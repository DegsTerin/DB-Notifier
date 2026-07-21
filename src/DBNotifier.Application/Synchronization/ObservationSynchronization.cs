// Module purpose: Defines observation synchronisation contracts without depending on concrete providers, transports or user interfaces.
using DBNotifier.Domain;

namespace DBNotifier.Application.Synchronization;

public enum ObservationIngestionDisposition
{
    Accepted,
    Duplicate,
    Rejected,
    Retryable,
}

public sealed record ObservationSyncMessage(
    Guid MessageId,
    int SchemaVersion,
    long Sequence,
    Guid ObservationId,
    Guid InstanceId,
    Guid AgentId,
    string ProviderType,
    string ProviderVersion,
    string Status,
    string Method,
    string EvidenceLevel,
    int AttemptCount,
    DateTimeOffset ObservedAt,
    long DurationMilliseconds,
    string? ErrorCode,
    string? SafeErrorMessage,
    IReadOnlyList<string> Limitations);

public sealed record ObservationBatchRequest(
    Guid AgentId,
    IReadOnlyList<ObservationSyncMessage> Items);

public sealed record ObservationItemResult(
    Guid MessageId,
    ObservationIngestionDisposition Disposition,
    string? ErrorCode = null,
    DateTimeOffset? RetryAfter = null);

public sealed record ObservationBatchResult(
    IReadOnlyList<ObservationItemResult> Items,
    long HighestContiguousSequence);

public sealed record AgentOutboxEnvelope(
    Guid MessageId,
    long Sequence,
    string MessageType,
    int SchemaVersion,
    string PayloadJson,
    DateTimeOffset OccurredAt,
    int AttemptCount);

public sealed record AgentOutboxDispatchResult(
    int PendingCount,
    int AcknowledgedCount,
    int RetryableCount);

public interface IAgentOutboxStore
{
    ValueTask<IReadOnlyList<AgentOutboxEnvelope>> GetPendingAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken);

    ValueTask ApplyResultsAsync(
        IReadOnlyList<ObservationItemResult> results,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public interface IObservationBatchTransport
{
    ValueTask<ObservationBatchResult> SendAsync(
        Guid agentId,
        IReadOnlyList<AgentOutboxEnvelope> messages,
        CancellationToken cancellationToken);
}

public interface IObservationIngestionStore
{
    ValueTask<ObservationItemResult> IngestAsync(
        ObservationSyncMessage message,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);

    ValueTask<long> GetHighestContiguousSequenceAsync(
        Guid agentId,
        CancellationToken cancellationToken);
}

/// <summary>Dispatches one bounded local Agent outbox batch and applies only unambiguous response classifications.</summary>
/// <param name="agentId">Exact enrolled Agent whose stream is being dispatched.</param>
/// <param name="outboxStore">Durable local outbox boundary.</param>
/// <param name="transport">Authenticated observation batch transport.</param>
/// <param name="timeProvider">Trusted local clock used for leases and result timestamps.</param>
public sealed class AgentOutboxDispatchRunner(
    Guid agentId,
    IAgentOutboxStore outboxStore,
    IObservationBatchTransport transport,
    TimeProvider timeProvider)
{
    /// <summary>Dispatches at most one bounded batch and records terminal or retryable outcomes in the local store.</summary>
    /// <param name="maximumBatchSize">Maximum number of pending envelopes, from one through one hundred.</param>
    /// <param name="cancellationToken">Token propagated through local persistence and transport operations.</param>
    /// <returns>Counts of selected, acknowledged and retryable envelopes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for an empty Agent identifier or invalid batch size.</exception>
    public async ValueTask<AgentOutboxDispatchResult> RunOnceAsync(
        int maximumBatchSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumBatchSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumBatchSize, 100);

        DateTimeOffset now = timeProvider.GetUtcNow();
        IReadOnlyList<AgentOutboxEnvelope> pending = await outboxStore
            .GetPendingAsync(now, maximumBatchSize, cancellationToken)
            .ConfigureAwait(false);
        if (pending.Count == 0)
        {
            return new AgentOutboxDispatchResult(0, 0, 0);
        }

        ObservationBatchResult response = await transport
            .SendAsync(agentId, pending, cancellationToken)
            .ConfigureAwait(false);
        List<ObservationItemResult> completeResults = new(pending.Count);
        if (!TryIndexResponse(response, pending, out Dictionary<Guid, ObservationItemResult>? returned))
        {
            completeResults.AddRange(pending.Select(message => new ObservationItemResult(
                message.MessageId,
                ObservationIngestionDisposition.Retryable,
                "sync.response_invalid")));
        }
        else
        {
            foreach (AgentOutboxEnvelope message in pending)
            {
                completeResults.Add(returned.TryGetValue(message.MessageId, out ObservationItemResult? result)
                    ? result
                    : new ObservationItemResult(
                    message.MessageId,
                    ObservationIngestionDisposition.Retryable,
                    "sync.response_missing"));
            }
        }

        DateTimeOffset completedAt = timeProvider.GetUtcNow();
        await outboxStore.ApplyResultsAsync(completeResults, completedAt, cancellationToken).ConfigureAwait(false);
        int acknowledged = completeResults.Count(result =>
            result.Disposition is ObservationIngestionDisposition.Accepted or
                ObservationIngestionDisposition.Duplicate or
                ObservationIngestionDisposition.Rejected);
        return new AgentOutboxDispatchResult(
            pending.Count,
            acknowledged,
            completeResults.Count - acknowledged);
    }

    /// <summary>Indexes only an unambiguous response whose identifiers are a unique subset of the pending batch.</summary>
    private static bool TryIndexResponse(
        ObservationBatchResult response,
        IReadOnlyList<AgentOutboxEnvelope> pending,
        out Dictionary<Guid, ObservationItemResult> indexed)
    {
        indexed = [];
        if (response.Items is null || response.HighestContiguousSequence < 0)
        {
            return false;
        }

        HashSet<Guid> expected = pending.Select(message => message.MessageId).ToHashSet();
        foreach (ObservationItemResult? result in response.Items)
        {
            if (result is null || !expected.Contains(result.MessageId) ||
                !Enum.IsDefined(result.Disposition) || !indexed.TryAdd(result.MessageId, result))
            {
                indexed.Clear();
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Validates bounded Agent observation batches before delegating each canonical message to the durable ingestion store.
/// Future evidence beyond the clock-skew policy is rejected terminally, while old offline backlog remains admissible.
/// </summary>
/// <param name="store">Durable server-side observation ingestion boundary.</param>
/// <param name="timeProvider">Authoritative server clock used to timestamp receipt and enforce future skew.</param>
public sealed class ObservationBatchIngestor(
    IObservationIngestionStore store,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Validates and ingests one ordered, bounded observation batch without normalising untrusted evidence timestamps.
    /// </summary>
    /// <param name="request">Authenticated Agent envelope containing between one and 100 observations.</param>
    /// <param name="cancellationToken">Cancellation propagated from the API request.</param>
    /// <returns>One terminal or retryable disposition for each supplied observation.</returns>
    public async ValueTask<ObservationBatchResult> HandleAsync(
        ObservationBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfEqual(request.AgentId, Guid.Empty);
        if (request.Items is null || request.Items.Count is < 1 or > 100)
        {
            throw new ArgumentException("Observation batches must contain between 1 and 100 items.", nameof(request));
        }

        DateTimeOffset receivedAt = timeProvider.GetUtcNow();
        List<ObservationItemResult> results = new(request.Items.Count);
        HashSet<Guid> batchMessageIds = [];
        foreach (ObservationSyncMessage? message in request.Items)
        {
            if (message is null)
            {
                results.Add(new ObservationItemResult(
                    Guid.Empty,
                    ObservationIngestionDisposition.Rejected,
                    "observation.payload_invalid"));
                continue;
            }

            string? validationError = Validate(request.AgentId, message, receivedAt, batchMessageIds);
            if (validationError is not null)
            {
                results.Add(new ObservationItemResult(
                    message.MessageId,
                    ObservationIngestionDisposition.Rejected,
                    validationError));
                continue;
            }

            results.Add(await store.IngestAsync(message, receivedAt, cancellationToken).ConfigureAwait(false));
        }

        long highest = await store
            .GetHighestContiguousSequenceAsync(request.AgentId, cancellationToken)
            .ConfigureAwait(false);
        return new ObservationBatchResult(results, highest);
    }

    /// <summary>Applies canonical envelope, timestamp, status, evidence and diagnostic bounds before persistence.</summary>
    private static string? Validate(
        Guid routeAgentId,
        ObservationSyncMessage message,
        DateTimeOffset receivedAt,
        HashSet<Guid> batchMessageIds)
    {
        if (message.MessageId == Guid.Empty || !batchMessageIds.Add(message.MessageId))
        {
            return "observation.message_id_invalid";
        }

        if (message.AgentId != routeAgentId || message.ObservationId == Guid.Empty ||
            message.InstanceId == Guid.Empty || message.Sequence < 1 || message.SchemaVersion != 1)
        {
            return "observation.envelope_invalid";
        }

        if (message.ObservedAt.Offset == TimeSpan.Zero &&
            message.ObservedAt - receivedAt > AgentFleet.AgentFleetProtocol.MaximumFutureClockSkew)
        {
            return "observation.observed_at_future";
        }

        if (!ProviderType.TryParse(message.ProviderType, out ProviderType providerType) ||
            providerType.Value != message.ProviderType ||
            !Enum.TryParse(message.Status, ignoreCase: false, out HealthStatus status) ||
            !Enum.IsDefined(status) ||
            !Enum.TryParse(message.EvidenceLevel, ignoreCase: false, out EvidenceLevel evidenceLevel) ||
            !Enum.IsDefined(evidenceLevel) ||
            !IsValidEvidence(status, evidenceLevel) ||
            message.ObservedAt.Offset != TimeSpan.Zero || message.ObservedAt == default ||
            message.AttemptCount is < 1 or > 10 ||
            message.DurationMilliseconds is < 0 or > 300_000 ||
            string.IsNullOrWhiteSpace(message.ProviderVersion) || message.ProviderVersion.Length > 64 ||
            string.IsNullOrWhiteSpace(message.Method) || message.Method.Length > 64 ||
            message.Limitations is null || message.Limitations.Count > 20 ||
            message.Limitations.Any(item => string.IsNullOrWhiteSpace(item) || item.Length > 200) ||
            message.ErrorCode?.Length > 100 || message.SafeErrorMessage?.Length > 1000 ||
            (message.ErrorCode is null && message.SafeErrorMessage is not null) ||
            (status == HealthStatus.Healthy && message.ErrorCode is not null))
        {
            return "observation.payload_invalid";
        }

        return null;
    }

    /// <summary>Prevents a healthy state from being asserted using transport-only or otherwise unproved evidence.</summary>
    private static bool IsValidEvidence(HealthStatus status, EvidenceLevel evidenceLevel) =>
        status != HealthStatus.Healthy ||
        evidenceLevel is EvidenceLevel.ProviderAuthenticated or EvidenceLevel.ProviderReadiness;
}

/// <summary>Captures one exact state transition before it is persisted as a canonical event.</summary>
/// <param name="EventType">Canonical event classification.</param>
/// <param name="Severity">Canonical event severity.</param>
/// <param name="PreviousStatus">Status before reconciliation, or null for an initial observation.</param>
/// <param name="CurrentStatus">Status after reconciliation.</param>
public sealed record CanonicalEventCandidate(
    string EventType,
    string Severity,
    HealthStatus? PreviousStatus,
    HealthStatus CurrentStatus);

/// <summary>Derives canonical event metadata from an exact provider-neutral state transition.</summary>
public static class ObservationEventDeriver
{
    /// <summary>Returns the canonical event for a state change, or null when no event is warranted.</summary>
    /// <param name="previous">Previously reconciled status, when one exists.</param>
    /// <param name="current">New reconciled status.</param>
    /// <returns>An exact transition candidate, or null for unchanged or non-notifiable states.</returns>
    public static CanonicalEventCandidate? Derive(HealthStatus? previous, HealthStatus current)
    {
        if (previous == current)
        {
            return null;
        }

        return current switch
        {
            HealthStatus.Healthy when previous is null => new("Connected", "Info", previous, current),
            HealthStatus.Healthy => new("Recovered", "Info", previous, current),
            HealthStatus.Unavailable => new("Disconnected", "Error", previous, current),
            HealthStatus.Timeout => new("Timeout", "Error", previous, current),
            HealthStatus.AuthFailed => new("AuthenticationFailed", "Error", previous, current),
            HealthStatus.Degraded => new("Degraded", "Warning", previous, current),
            _ => null,
        };
    }
}
