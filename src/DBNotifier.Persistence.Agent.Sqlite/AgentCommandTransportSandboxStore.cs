// Module purpose: Persists one fenced execution-ineligible command-transport stream in Agent SQLite, including exact pending replay and atomic inbox acknowledgement preparation.
using System.Data;
using System.Text;
using System.Text.Json;
using DBNotifier.Application.Synchronization;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Agent.Sqlite;

/// <summary>Identifies transaction boundaries where deterministic tests may inject a local SQLite failure.</summary>
public enum AgentCommandTransportFaultPoint
{
    /// <summary>Occurs after preparing a poll but before its transaction commits.</summary>
    BeforePollPreparationCommit,

    /// <summary>Occurs after inbox changes and acknowledgement preparation but before commit.</summary>
    BeforeAcknowledgementPreparationCommit,

    /// <summary>Occurs after applying an acknowledgement response but before commit.</summary>
    BeforeAcknowledgementCompletionCommit,
}

/// <summary>Provides deterministic local fault injection without affecting normal store construction.</summary>
public interface IAgentCommandTransportFaultInjector
{
    /// <summary>Injects a configured failure at the exact transaction boundary.</summary>
    ValueTask InjectAsync(AgentCommandTransportFaultPoint point, CancellationToken cancellationToken);
}

/// <summary>Implements durable pending-message replay, isolated receipt persistence and cross-process fencing for the sandbox.</summary>
/// <param name="contextFactory">Creates contexts for the dedicated sandbox receipt database only.</param>
/// <param name="faultInjector">Optional deterministic local fault boundary used exclusively by tests.</param>
internal sealed class AgentCommandTransportSandboxStore(
    IDbContextFactory<AgentCommandTransportSandboxDbContext> contextFactory,
    IAgentCommandTransportFaultInjector? faultInjector = null) : ICommandTransportAgentStore
{
    /// <inheritdoc />
    public async ValueTask<long?> TryAcquireLeaseAsync(
        Guid agentId,
        string ownerId,
        DateTimeOffset now,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        ValidateAgentAndOwner(agentId, ownerId);
        if (now.Offset != TimeSpan.Zero || duration <= TimeSpan.Zero || duration > CommandTransportProtocol.LeaseDuration)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        await using AgentCommandTransportSandboxDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken).ConfigureAwait(false);
        AgentCommandTransportSandboxStateRow? state = await context.TransportStates
            .SingleOrDefaultAsync(row => row.AgentId == agentId, cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            state = new AgentCommandTransportSandboxStateRow
            {
                AgentId = agentId,
                NextSequence = 1,
                NextFence = 2,
                LeaseOwner = ownerId,
                LeaseFence = 1,
                LeaseExpiresAt = now.Add(duration),
                ConcurrencyToken = Guid.NewGuid(),
            };
            context.TransportStates.Add(state);
        }
        else
        {
            bool owned = string.Equals(state.LeaseOwner, ownerId, StringComparison.Ordinal) &&
                state.LeaseExpiresAt > now;
            if (!owned && state.LeaseExpiresAt > now)
            {
                return null;
            }

            long fence = owned && state.LeaseFence is not null
                ? state.LeaseFence.Value
                : state.NextFence;
            if (!owned)
            {
                state.NextFence = checked(fence + 1);
            }

            state.LeaseOwner = ownerId;
            state.LeaseFence = fence;
            state.LeaseExpiresAt = now.Add(duration);
            state.ConcurrencyToken = Guid.NewGuid();
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return state.LeaseFence;
    }

    /// <inheritdoc />
    public async ValueTask<CommandTransportPendingMessage> GetOrPreparePollAsync(
        Guid agentId,
        long fenceToken,
        string agentVersion,
        IReadOnlyDictionary<string, string> providerVersions,
        int maximumCount,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ValidatePollInputs(agentId, fenceToken, agentVersion, providerVersions, maximumCount, now);
        await using AgentCommandTransportSandboxDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken).ConfigureAwait(false);
        AgentCommandTransportSandboxStateRow state = await GetFencedStateAsync(
            context,
            agentId,
            fenceToken,
            now,
            cancellationToken).ConfigureAwait(false);
        if (state.PendingMessageId is not null)
        {
            return ToPending(state);
        }

        SortedDictionary<string, string> orderedVersions = new(StringComparer.Ordinal);
        foreach ((string providerId, string providerVersion) in providerVersions)
        {
            orderedVersions.Add(providerId, providerVersion);
        }
        CommandTransportPollRequest request = new(
            Guid.NewGuid(),
            CommandTransportProtocol.CurrentSchemaVersion,
            agentId,
            state.NextSequence,
            now,
            now,
            agentVersion,
            orderedVersions,
            maximumCount);
        string payload = CommandTransportCodec.Serialize(request);
        EnsureBoundedPayload(payload);
        SetPending(state, request.MessageId, request.Sequence, CommandTransportMessageKind.Poll, payload);
        state.NextSequence = checked(state.NextSequence + 1);
        state.ConcurrencyToken = Guid.NewGuid();
        await InjectAsync(AgentCommandTransportFaultPoint.BeforePollPreparationCommit, cancellationToken)
            .ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return ToPending(state);
    }

    /// <inheritdoc />
    public async ValueTask RecordAttemptAsync(
        Guid agentId,
        long fenceToken,
        Guid messageId,
        DateTimeOffset attemptedAt,
        CancellationToken cancellationToken)
    {
        if (messageId == Guid.Empty || attemptedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("command.transport_attempt_invalid", nameof(messageId));
        }

        await using AgentCommandTransportSandboxDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        AgentCommandTransportSandboxStateRow state = await GetFencedStateAsync(
            context,
            agentId,
            fenceToken,
            attemptedAt,
            cancellationToken).ConfigureAwait(false);
        if (state.PendingMessageId != messageId || state.PendingAttemptCount >= 1_000_000)
        {
            throw new InvalidOperationException("command.transport_pending_mismatch");
        }

        state.PendingAttemptCount++;
        state.PendingLastAttemptAt = attemptedAt;
        state.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<CommandTransportPollCommit> AcceptPollResponseAsync(
        Guid agentId,
        long fenceToken,
        CommandTransportPollResponse response,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        await using AgentCommandTransportSandboxDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken).ConfigureAwait(false);
        AgentCommandTransportSandboxStateRow state = await GetFencedStateAsync(
            context,
            agentId,
            fenceToken,
            receivedAt,
            cancellationToken).ConfigureAwait(false);
        CommandTransportPollRequest request = RequirePending<CommandTransportPollRequest>(
            state,
            CommandTransportMessageKind.Poll);
        ValidatePollResponse(request, response, receivedAt);

        List<CommandTransportAcknowledgement> acknowledgements = new(response.Commands.Count);
        foreach (CommandTransportEnvelope command in response.Commands)
        {
            ValidateEnvelope(command, request, receivedAt);
            AgentCommandTransportSandboxReceiptRow? existing = await context.Receipts
                .SingleOrDefaultAsync(row => row.CommandId == command.CommandId ||
                    row.IdempotencyKey == command.IdempotencyKey, cancellationToken).ConfigureAwait(false);
            CommandTransportDisposition disposition = Classify(command, receivedAt);
            string stateName = ToLocalState(disposition);
            if (existing is not null)
            {
                if (!Matches(existing, command))
                {
                    throw new CommandTransportException(
                        CommandTransportFailureKind.Conflict,
                        "command.transport_inbox_conflict");
                }

                disposition = existing.State switch
                {
                    "ReceiptExpired" => CommandTransportDisposition.Expired,
                    "ReceiptUnsupported" => CommandTransportDisposition.Unsupported,
                    "ReceiptRejected" => CommandTransportDisposition.Rejected,
                    _ => CommandTransportDisposition.Accepted,
                };
            }
            else
            {
                context.Receipts.Add(new AgentCommandTransportSandboxReceiptRow
                {
                    CommandId = command.CommandId,
                    IdempotencyKey = command.IdempotencyKey,
                    InstanceId = command.InstanceId,
                    ProviderId = command.ProviderId,
                    CapabilityId = command.CapabilityId,
                    TypedParametersJson = command.TypedParametersJson,
                    State = stateName,
                    RequestedAt = command.RequestedAt,
                    ExpiresAt = command.ExpiresAt,
                    ExpectedAgentVersion = command.ExpectedAgentVersion,
                    ExpectedProviderVersion = command.ExpectedProviderVersion,
                    ExecutionPolicy = CommandExecutionPolicy.Never.ToString(),
                    ConcurrencyToken = Guid.NewGuid(),
                });
            }

            acknowledgements.Add(new(
                command.CommandId,
                command.IdempotencyKey,
                receivedAt,
                disposition,
                disposition switch
                {
                    CommandTransportDisposition.Expired => "command.expired",
                    CommandTransportDisposition.Unsupported => "command.capability_unsupported",
                    CommandTransportDisposition.Rejected => "command.rejected",
                    _ => null,
                }));
        }

        CommandTransportPendingMessage? pendingAcknowledgement = null;
        if (acknowledgements.Count == 0)
        {
            ClearPending(state);
        }
        else
        {
            CommandTransportAcknowledgementRequest acknowledgement = new(
                Guid.NewGuid(),
                CommandTransportProtocol.CurrentSchemaVersion,
                agentId,
                state.NextSequence,
                receivedAt,
                receivedAt,
                acknowledgements);
            string payload = CommandTransportCodec.Serialize(acknowledgement);
            EnsureBoundedPayload(payload);
            SetPending(
                state,
                acknowledgement.MessageId,
                acknowledgement.Sequence,
                CommandTransportMessageKind.Acknowledgement,
                payload);
            state.NextSequence = checked(state.NextSequence + 1);
            pendingAcknowledgement = ToPending(state);
        }

        state.ConcurrencyToken = Guid.NewGuid();
        await InjectAsync(AgentCommandTransportFaultPoint.BeforeAcknowledgementPreparationCommit, cancellationToken)
            .ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new(response.Commands.Count, pendingAcknowledgement);
    }

    /// <inheritdoc />
    public async ValueTask<int> AcceptAcknowledgementResponseAsync(
        Guid agentId,
        long fenceToken,
        CommandTransportAcknowledgementResponse response,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        await using AgentCommandTransportSandboxDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken).ConfigureAwait(false);
        AgentCommandTransportSandboxStateRow state = await GetFencedStateAsync(
            context,
            agentId,
            fenceToken,
            receivedAt,
            cancellationToken).ConfigureAwait(false);
        CommandTransportAcknowledgementRequest request = RequirePending<CommandTransportAcknowledgementRequest>(
            state,
            CommandTransportMessageKind.Acknowledgement);
        ValidateAcknowledgementResponse(request, response, receivedAt);

        Dictionary<Guid, CommandTransportAcknowledgement> expected = request.Items.ToDictionary(item => item.CommandId);
        foreach (CommandTransportAcknowledgementResult result in response.Items)
        {
            if (!expected.TryGetValue(result.CommandId, out CommandTransportAcknowledgement? sent))
            {
                throw new CommandTransportException(
                    CommandTransportFailureKind.Conflict,
                    "command.transport_ack_result_unknown");
            }

            if (!AcknowledgementResultMatches(sent.Disposition, result.Disposition))
            {
                throw new CommandTransportException(
                    CommandTransportFailureKind.Conflict,
                    "command.transport_ack_result_conflict");
            }

            AgentCommandTransportSandboxReceiptRow command = await context.Receipts
                .SingleAsync(row => row.CommandId == result.CommandId, cancellationToken).ConfigureAwait(false);
            EnsureNever(command);
            command.State = result.Disposition switch
            {
                CommandTransportDisposition.Accepted or CommandTransportDisposition.Duplicate
                    when sent.Disposition == CommandTransportDisposition.Accepted => "ReceiptAcknowledged",
                CommandTransportDisposition.Expired => "ReceiptExpired",
                CommandTransportDisposition.Unsupported => "ReceiptUnsupported",
                _ => "ReceiptRejected",
            };
            if (command.State == "ReceiptAcknowledged")
            {
                command.AcknowledgedAt ??= receivedAt;
            }

            command.ConcurrencyToken = Guid.NewGuid();
        }

        ClearPending(state);
        state.ConcurrencyToken = Guid.NewGuid();
        await InjectAsync(AgentCommandTransportFaultPoint.BeforeAcknowledgementCompletionCommit, cancellationToken)
            .ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return response.Items.Count;
    }

    /// <inheritdoc />
    public async ValueTask ReleaseLeaseAsync(
        Guid agentId,
        string ownerId,
        long fenceToken,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ValidateAgentAndOwner(agentId, ownerId);
        await using AgentCommandTransportSandboxDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        AgentCommandTransportSandboxStateRow? state = await context.TransportStates
            .SingleOrDefaultAsync(row => row.AgentId == agentId, cancellationToken).ConfigureAwait(false);
        if (state is null || state.LeaseFence != fenceToken ||
            !string.Equals(state.LeaseOwner, ownerId, StringComparison.Ordinal))
        {
            return;
        }

        state.LeaseOwner = null;
        state.LeaseFence = null;
        state.LeaseExpiresAt = null;
        state.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask InjectAsync(
        AgentCommandTransportFaultPoint point,
        CancellationToken cancellationToken)
    {
        if (faultInjector is not null)
        {
            await faultInjector.InjectAsync(point, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<AgentCommandTransportSandboxStateRow> GetFencedStateAsync(
        AgentCommandTransportSandboxDbContext context,
        Guid agentId,
        long fenceToken,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        AgentCommandTransportSandboxStateRow? state = await context.TransportStates
            .SingleOrDefaultAsync(row => row.AgentId == agentId, cancellationToken).ConfigureAwait(false);
        if (state is null || fenceToken < 1 || state.LeaseFence != fenceToken || state.LeaseExpiresAt <= now)
        {
            throw new InvalidOperationException("command.transport_fence_stale");
        }

        return state;
    }

    private static T RequirePending<T>(
        AgentCommandTransportSandboxStateRow state,
        CommandTransportMessageKind expectedKind)
    {
        CommandTransportPendingMessage pending = ToPending(state);
        if (pending.Kind != expectedKind ||
            !string.Equals(CommandTransportCodec.ComputeSha256(pending.PayloadJson), pending.PayloadSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException("command.transport_pending_integrity_invalid");
        }

        return CommandTransportCodec.Deserialize<T>(pending.PayloadJson);
    }

    private static CommandTransportPendingMessage ToPending(AgentCommandTransportSandboxStateRow state)
    {
        if (state.PendingMessageId is null || state.PendingSequence is null ||
            !Enum.TryParse(state.PendingMessageKind, out CommandTransportMessageKind kind) ||
            string.IsNullOrWhiteSpace(state.PendingPayloadJson) ||
            string.IsNullOrWhiteSpace(state.PendingPayloadSha256))
        {
            throw new InvalidOperationException("command.transport_pending_missing");
        }

        return new(
            state.PendingMessageId.Value,
            state.PendingSequence.Value,
            kind,
            state.PendingPayloadJson,
            state.PendingPayloadSha256,
            state.PendingAttemptCount);
    }

    private static void SetPending(
        AgentCommandTransportSandboxStateRow state,
        Guid messageId,
        long sequence,
        CommandTransportMessageKind kind,
        string payload)
    {
        state.PendingMessageId = messageId;
        state.PendingSequence = sequence;
        state.PendingMessageKind = kind.ToString();
        state.PendingPayloadJson = payload;
        state.PendingPayloadSha256 = CommandTransportCodec.ComputeSha256(payload);
        state.PendingAttemptCount = 0;
        state.PendingLastAttemptAt = null;
    }

    private static void ClearPending(AgentCommandTransportSandboxStateRow state)
    {
        state.PendingMessageId = null;
        state.PendingSequence = null;
        state.PendingMessageKind = null;
        state.PendingPayloadJson = null;
        state.PendingPayloadSha256 = null;
        state.PendingAttemptCount = 0;
        state.PendingLastAttemptAt = null;
    }

    private static CommandTransportDisposition Classify(CommandTransportEnvelope command, DateTimeOffset receivedAt) =>
        command.ExpiresAt <= receivedAt
            ? CommandTransportDisposition.Expired
            : command.CapabilityId.EndsWith("unsupported.v1", StringComparison.Ordinal)
                ? CommandTransportDisposition.Unsupported
                : CommandTransportDisposition.Accepted;

    private static bool AcknowledgementResultMatches(
        CommandTransportDisposition sent,
        CommandTransportDisposition received) => sent switch
        {
            CommandTransportDisposition.Accepted => received is
                CommandTransportDisposition.Accepted or
                CommandTransportDisposition.Duplicate or
                CommandTransportDisposition.Expired,
            CommandTransportDisposition.Expired => received == CommandTransportDisposition.Expired,
            CommandTransportDisposition.Unsupported => received == CommandTransportDisposition.Unsupported,
            CommandTransportDisposition.Rejected => received == CommandTransportDisposition.Rejected,
            _ => false,
        };

    private static string ToLocalState(CommandTransportDisposition disposition) => disposition switch
    {
        CommandTransportDisposition.Expired => "ReceiptExpired",
        CommandTransportDisposition.Unsupported => "ReceiptUnsupported",
        CommandTransportDisposition.Rejected => "ReceiptRejected",
        _ => "ReceiptPending",
    };

    private static void ValidatePollResponse(
        CommandTransportPollRequest request,
        CommandTransportPollResponse response,
        DateTimeOffset receivedAt)
    {
        string json = CommandTransportCodec.Serialize(response);
        if (response.MessageId == Guid.Empty || response.SchemaVersion != CommandTransportProtocol.CurrentSchemaVersion ||
            response.AgentId != request.AgentId || response.Sequence != request.Sequence ||
            response.InReplyToMessageId != request.MessageId || response.OccurredAt.Offset != TimeSpan.Zero ||
            response.SentAt.Offset != TimeSpan.Zero || response.SentAt < response.OccurredAt ||
            response.SentAt > receivedAt.AddMinutes(1) || response.Commands is null ||
            response.Commands.Count > request.MaximumCount || Encoding.UTF8.GetByteCount(json) > CommandTransportProtocol.MaximumHttpBodyBytes)
        {
            throw new InvalidDataException("command.transport_poll_response_invalid");
        }

        if (response.Commands.Select(item => item.CommandId).Distinct().Count() != response.Commands.Count ||
            response.Commands.Select(item => item.IdempotencyKey).Distinct(StringComparer.Ordinal).Count() !=
                response.Commands.Count)
        {
            throw new InvalidDataException("command.transport_poll_response_duplicate");
        }
    }

    private static void ValidateAcknowledgementResponse(
        CommandTransportAcknowledgementRequest request,
        CommandTransportAcknowledgementResponse response,
        DateTimeOffset receivedAt)
    {
        string json = CommandTransportCodec.Serialize(response);
        if (response.MessageId == Guid.Empty || response.SchemaVersion != CommandTransportProtocol.CurrentSchemaVersion ||
            response.AgentId != request.AgentId || response.Sequence != request.Sequence ||
            response.InReplyToMessageId != request.MessageId || response.OccurredAt.Offset != TimeSpan.Zero ||
            response.SentAt.Offset != TimeSpan.Zero || response.SentAt < response.OccurredAt ||
            response.SentAt > receivedAt.AddMinutes(1) || response.Items is null ||
            response.Items.Count != request.Items.Count ||
            response.Items.Select(item => item.CommandId).Distinct().Count() != response.Items.Count ||
            response.Items.Any(item => item.ReasonCode?.Length > 100) ||
            Encoding.UTF8.GetByteCount(json) > CommandTransportProtocol.MaximumHttpBodyBytes)
        {
            throw new InvalidDataException("command.transport_ack_response_invalid");
        }
    }

    private static void ValidateEnvelope(
        CommandTransportEnvelope command,
        CommandTransportPollRequest request,
        DateTimeOffset receivedAt)
    {
        if (command.CommandId == Guid.Empty || command.InstanceId == Guid.Empty ||
            command.ExecutionPolicy != CommandExecutionPolicy.Never ||
            string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 200 ||
            string.IsNullOrWhiteSpace(command.ProviderId) || command.ProviderId.Length > 64 ||
            string.IsNullOrWhiteSpace(command.CapabilityId) || command.CapabilityId.Length > 100 ||
            !command.CapabilityId.StartsWith(CommandTransportProtocol.SyntheticCapabilityPrefix, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(command.TypedParametersJson) ||
            Encoding.UTF8.GetByteCount(command.TypedParametersJson) > CommandTransportProtocol.MaximumTypedParametersBytes ||
            !IsJsonObject(command.TypedParametersJson) ||
            command.RequestedAt.Offset != TimeSpan.Zero || command.ExpiresAt.Offset != TimeSpan.Zero ||
            command.ExpiresAt <= command.RequestedAt || command.RequestedAt > receivedAt.AddMinutes(1) ||
            !string.Equals(command.ExpectedAgentVersion, request.AgentVersion, StringComparison.Ordinal) ||
            !request.ProviderVersions.TryGetValue(command.ProviderId, out string? providerVersion) ||
            !string.Equals(command.ExpectedProviderVersion, providerVersion, StringComparison.Ordinal))
        {
            throw new InvalidDataException("command.transport_envelope_invalid");
        }
    }

    private static bool Matches(AgentCommandTransportSandboxReceiptRow row, CommandTransportEnvelope command) =>
        row.CommandId == command.CommandId &&
        string.Equals(row.IdempotencyKey, command.IdempotencyKey, StringComparison.Ordinal) &&
        row.InstanceId == command.InstanceId &&
        string.Equals(row.ProviderId, command.ProviderId, StringComparison.Ordinal) &&
        string.Equals(row.CapabilityId, command.CapabilityId, StringComparison.Ordinal) &&
        string.Equals(row.TypedParametersJson, command.TypedParametersJson, StringComparison.Ordinal) &&
        row.RequestedAt == command.RequestedAt && row.ExpiresAt == command.ExpiresAt &&
        string.Equals(row.ExpectedAgentVersion, command.ExpectedAgentVersion, StringComparison.Ordinal) &&
        string.Equals(row.ExpectedProviderVersion, command.ExpectedProviderVersion, StringComparison.Ordinal) &&
        string.Equals(row.ExecutionPolicy, CommandExecutionPolicy.Never.ToString(), StringComparison.Ordinal);

    /// <summary>Rejects rehydrated receipt data whose durable policy no longer proves execution ineligibility.</summary>
    /// <param name="row">Receipt row loaded from the isolated sandbox database.</param>
    /// <exception cref="InvalidDataException">Thrown when the immutable policy is not exactly <c>Never</c>.</exception>
    private static void EnsureNever(AgentCommandTransportSandboxReceiptRow row)
    {
        if (!string.Equals(row.ExecutionPolicy, CommandExecutionPolicy.Never.ToString(), StringComparison.Ordinal))
        {
            throw new InvalidDataException("command.transport_execution_policy_invalid");
        }
    }

    private static bool IsJsonObject(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void ValidatePollInputs(
        Guid agentId,
        long fenceToken,
        string agentVersion,
        IReadOnlyDictionary<string, string> providerVersions,
        int maximumCount,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(providerVersions);
        if (agentId == Guid.Empty || fenceToken < 1 || string.IsNullOrWhiteSpace(agentVersion) ||
            agentVersion.Length > 64 || maximumCount is < 1 or > CommandTransportProtocol.MaximumBatchSize ||
            now.Offset != TimeSpan.Zero || providerVersions.Count > CommandTransportProtocol.MaximumProviderVersions ||
            providerVersions.Any(item => string.IsNullOrWhiteSpace(item.Key) || item.Key.Length > 64 ||
                string.IsNullOrWhiteSpace(item.Value) || item.Value.Length > 64))
        {
            throw new ArgumentException("command.transport_poll_input_invalid");
        }
    }

    private static void ValidateAgentAndOwner(Guid agentId, string ownerId)
    {
        if (agentId == Guid.Empty || string.IsNullOrWhiteSpace(ownerId) || ownerId.Length > 160)
        {
            throw new ArgumentException("command.transport_owner_invalid");
        }
    }

    private static void EnsureBoundedPayload(string payload)
    {
        if (Encoding.UTF8.GetByteCount(payload) > CommandTransportProtocol.MaximumHttpBodyBytes)
        {
            throw new InvalidOperationException("command.transport_payload_too_large");
        }
    }
}
