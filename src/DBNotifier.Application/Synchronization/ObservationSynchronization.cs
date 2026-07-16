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

public sealed class AgentOutboxDispatchRunner(
    Guid agentId,
    IAgentOutboxStore outboxStore,
    IObservationBatchTransport transport,
    TimeProvider timeProvider)
{
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
        Dictionary<Guid, ObservationItemResult> returned = response.Items
            .OfType<ObservationItemResult>()
            .GroupBy(result => result.MessageId)
            .ToDictionary(group => group.Key, group => group.First());
        List<ObservationItemResult> completeResults = new(pending.Count);
        foreach (AgentOutboxEnvelope message in pending)
        {
            completeResults.Add(returned.TryGetValue(message.MessageId, out ObservationItemResult? result)
                ? result
                : new ObservationItemResult(
                    message.MessageId,
                    ObservationIngestionDisposition.Retryable,
                    "sync.response_missing"));
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
    private static readonly TimeSpan MaximumFutureObservationSkew = TimeSpan.FromMinutes(5);

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
            message.ObservedAt - receivedAt > MaximumFutureObservationSkew)
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

public sealed record CanonicalEventCandidate(string EventType, string Severity);

public static class ObservationEventDeriver
{
    public static CanonicalEventCandidate? Derive(HealthStatus? previous, HealthStatus current)
    {
        if (previous == current)
        {
            return null;
        }

        return current switch
        {
            HealthStatus.Healthy when previous is null => new("Connected", "Info"),
            HealthStatus.Healthy => new("Recovered", "Info"),
            HealthStatus.Unavailable => new("Disconnected", "Error"),
            HealthStatus.Timeout => new("Timeout", "Error"),
            HealthStatus.AuthFailed => new("AuthenticationFailed", "Error"),
            HealthStatus.Degraded => new("Degraded", "Warning"),
            _ => null,
        };
    }
}
