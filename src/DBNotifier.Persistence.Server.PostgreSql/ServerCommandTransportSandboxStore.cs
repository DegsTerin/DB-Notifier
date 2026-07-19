// Module purpose: Commits one serial execution-ineligible command-transport stream with exact durable response replay and no dependency on command execution.
using System.Data;
using System.Text;
using DBNotifier.Application.Synchronization;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Server.PostgreSql;

/// <summary>Implements the isolated v2 Server journal, monotonic cursor and terminal-only fixture handling.</summary>
public sealed class ServerCommandTransportSandboxStore(
    IDbContextFactory<ServerDbContext> contextFactory) : ICommandTransportServerStore
{
    private static readonly SemaphoreSlim BoundedGate = new(1, 1);

    /// <inheritdoc />
    public async ValueTask<CommandTransportPollResponse> PollAsync(
        CommandTransportPollRequest request,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        ValidatePoll(request, receivedAt);
        return await ExecuteAsync(
            request.AgentId,
            request.MessageId,
            request.Sequence,
            CommandTransportProtocol.PollMessageType,
            CommandTransportCodec.Serialize(request),
            receivedAt,
            async (context, token) =>
            {
                AdministrativeCommandRow[] candidates = await context.AdministrativeCommands
                    .Where(row => row.AssignedAgentId == request.AgentId &&
                        row.ExpectedAgentVersion == request.AgentVersion &&
                        (row.State == "Pending" || row.State == "Available"))
                    .OrderBy(row => row.IdempotencyKey)
                    .Take(request.MaximumCount * 2)
                    .ToArrayAsync(token).ConfigureAwait(false);
                candidates = [.. candidates.OrderBy(row => row.RequestedAt)];
                List<CommandTransportEnvelope> deliverable = new(request.MaximumCount);
                foreach (AdministrativeCommandRow command in candidates)
                {
                    if (command.ExpiresAt <= receivedAt)
                    {
                        command.State = "Expired";
                        command.ConcurrencyToken = Guid.NewGuid();
                        continue;
                    }

                    if (!command.CapabilityId.StartsWith(
                        CommandTransportProtocol.SyntheticCapabilityPrefix,
                        StringComparison.Ordinal) ||
                        Encoding.UTF8.GetByteCount(command.TypedParametersJson) >
                            CommandTransportProtocol.MaximumTypedParametersBytes ||
                        !IsJsonObject(command.TypedParametersJson))
                    {
                        continue;
                    }

                    DatabaseInstanceRow? instance = await context.Instances.AsNoTracking()
                        .SingleOrDefaultAsync(row => row.InstanceId == command.InstanceId, token)
                        .ConfigureAwait(false);
                    bool compatible = instance is not null &&
                        request.ProviderVersions.TryGetValue(instance.ProviderType, out string? version) &&
                        string.Equals(version, command.ExpectedProviderVersion, StringComparison.Ordinal);
                    if (!compatible)
                    {
                        continue;
                    }

                    command.State = "Available";
                    command.ConcurrencyToken = Guid.NewGuid();
                    deliverable.Add(new CommandTransportEnvelope(
                        command.CommandId,
                        command.IdempotencyKey,
                        command.InstanceId,
                        instance!.ProviderType,
                        command.CapabilityId,
                        command.TypedParametersJson,
                        command.RequestedAt,
                        command.ExpiresAt,
                        command.ExpectedAgentVersion,
                        command.ExpectedProviderVersion,
                        CommandExecutionPolicy.Never));
                    if (deliverable.Count == request.MaximumCount)
                    {
                        break;
                    }
                }

                return new CommandTransportPollResponse(
                    Guid.NewGuid(),
                    CommandTransportProtocol.CurrentSchemaVersion,
                    request.AgentId,
                    request.Sequence,
                    request.MessageId,
                    receivedAt,
                    receivedAt,
                    deliverable);
            },
            CommandTransportCodec.Deserialize<CommandTransportPollResponse>,
            response => response.MessageId,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<CommandTransportAcknowledgementResponse> AcknowledgeAsync(
        CommandTransportAcknowledgementRequest request,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        ValidateAcknowledgement(request, receivedAt);
        return await ExecuteAsync(
            request.AgentId,
            request.MessageId,
            request.Sequence,
            CommandTransportProtocol.AcknowledgementMessageType,
            CommandTransportCodec.Serialize(request),
            receivedAt,
            async (context, token) =>
            {
                List<CommandTransportAcknowledgementResult> results = new(request.Items.Count);
                foreach (CommandTransportAcknowledgement item in request.Items)
                {
                    AdministrativeCommandRow? command = await context.AdministrativeCommands
                        .SingleOrDefaultAsync(row => row.CommandId == item.CommandId &&
                            row.AssignedAgentId == request.AgentId, token).ConfigureAwait(false);
                    if (command is null ||
                        !string.Equals(command.IdempotencyKey, item.IdempotencyKey, StringComparison.Ordinal) ||
                        !command.CapabilityId.StartsWith(
                            CommandTransportProtocol.SyntheticCapabilityPrefix,
                            StringComparison.Ordinal))
                    {
                        results.Add(new(item.CommandId, CommandTransportDisposition.Rejected, "command.ack_unknown"));
                        continue;
                    }

                    if (command.State == "Acknowledged" && item.Disposition == CommandTransportDisposition.Accepted)
                    {
                        results.Add(new(command.CommandId, CommandTransportDisposition.Duplicate));
                        continue;
                    }

                    CommandTransportDisposition disposition = item.Disposition;
                    if (command.ExpiresAt <= receivedAt || disposition == CommandTransportDisposition.Expired)
                    {
                        command.State = "Expired";
                        disposition = CommandTransportDisposition.Expired;
                    }
                    else if (disposition == CommandTransportDisposition.Unsupported)
                    {
                        command.State = "Unsupported";
                    }
                    else if (disposition == CommandTransportDisposition.Rejected)
                    {
                        command.State = "Rejected";
                    }
                    else if (disposition == CommandTransportDisposition.Accepted && command.State == "Available")
                    {
                        command.State = "Acknowledged";
                    }
                    else
                    {
                        disposition = CommandTransportDisposition.Rejected;
                        command.State = "Rejected";
                    }

                    command.ConcurrencyToken = Guid.NewGuid();
                    results.Add(new(command.CommandId, disposition, item.ReasonCode));
                }

                return new CommandTransportAcknowledgementResponse(
                    Guid.NewGuid(),
                    CommandTransportProtocol.CurrentSchemaVersion,
                    request.AgentId,
                    request.Sequence,
                    request.MessageId,
                    receivedAt,
                    receivedAt,
                    results);
            },
            CommandTransportCodec.Deserialize<CommandTransportAcknowledgementResponse>,
            response => response.MessageId,
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<TResponse> ExecuteAsync<TResponse>(
        Guid agentId,
        Guid messageId,
        long sequence,
        string messageType,
        string requestJson,
        DateTimeOffset receivedAt,
        Func<ServerDbContext, CancellationToken, Task<TResponse>> createResponse,
        Func<string, TResponse> readResponse,
        Func<TResponse, Guid> responseId,
        CancellationToken cancellationToken)
    {
        if (!await BoundedGate.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false))
        {
            throw new CommandTransportException(
                CommandTransportFailureKind.Busy,
                "command.transport_backpressure");
        }

        try
        {
            string requestHash = CommandTransportCodec.ComputeSha256(requestJson);
            await using ServerDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken).ConfigureAwait(false);
            RegisteredAgentRow? agent = await context.Agents
                .SingleOrDefaultAsync(row => row.AgentId == agentId, cancellationToken).ConfigureAwait(false);
            if (agent is null || agent.State != "Active" || agent.RevokedAt is not null)
            {
                throw new CommandTransportException(
                    CommandTransportFailureKind.AgentInactive,
                    "agent.identity_inactive");
            }

            ServerCommandTransportJournalRow? byMessage = await context.CommandTransportJournal
                .AsNoTracking()
                .SingleOrDefaultAsync(row => row.MessageId == messageId, cancellationToken).ConfigureAwait(false);
            ServerCommandTransportJournalRow? bySequence = await context.CommandTransportJournal
                .AsNoTracking()
                .SingleOrDefaultAsync(row => row.AgentId == agentId && row.Sequence == sequence, cancellationToken)
                .ConfigureAwait(false);
            ServerCommandTransportJournalRow? replay = byMessage ?? bySequence;
            if (replay is not null)
            {
                if (replay.MessageId != messageId || replay.AgentId != agentId || replay.Sequence != sequence ||
                    !string.Equals(replay.MessageType, messageType, StringComparison.Ordinal) ||
                    !string.Equals(replay.RequestPayloadSha256, requestHash, StringComparison.Ordinal))
                {
                    throw new CommandTransportException(
                        CommandTransportFailureKind.Conflict,
                        "command.transport_replay_conflict");
                }

                return readResponse(replay.ResponsePayloadJson);
            }

            ServerCommandTransportCursorRow? cursor = await context.CommandTransportCursors
                .SingleOrDefaultAsync(row => row.AgentId == agentId, cancellationToken).ConfigureAwait(false);
            long expected = checked((cursor?.HighestAcceptedSequence ?? 0) + 1);
            if (sequence != expected)
            {
                throw new CommandTransportException(
                    sequence > expected ? CommandTransportFailureKind.Gap : CommandTransportFailureKind.Reordered,
                    sequence > expected ? "command.transport_sequence_gap" : "command.transport_sequence_reordered",
                    expected);
            }

            TResponse response = await createResponse(context, cancellationToken).ConfigureAwait(false);
            string responseJson = CommandTransportCodec.Serialize(response);
            if (Encoding.UTF8.GetByteCount(responseJson) > CommandTransportProtocol.MaximumHttpBodyBytes)
            {
                throw new CommandTransportException(
                    CommandTransportFailureKind.Invalid,
                    "command.transport_response_too_large");
            }

            if (cursor is null)
            {
                cursor = new ServerCommandTransportCursorRow
                {
                    AgentId = agentId,
                    HighestAcceptedSequence = sequence,
                    UpdatedAt = receivedAt,
                    ConcurrencyToken = Guid.NewGuid(),
                };
                context.CommandTransportCursors.Add(cursor);
            }
            else
            {
                cursor.HighestAcceptedSequence = sequence;
                cursor.UpdatedAt = receivedAt;
                cursor.ConcurrencyToken = Guid.NewGuid();
            }

            context.CommandTransportJournal.Add(new ServerCommandTransportJournalRow
            {
                MessageId = messageId,
                AgentId = agentId,
                Sequence = sequence,
                MessageType = messageType,
                RequestPayloadSha256 = requestHash,
                ResponseMessageId = responseId(response),
                ResponsePayloadJson = responseJson,
                ReceivedAt = receivedAt,
            });
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
        finally
        {
            BoundedGate.Release();
        }
    }

    private static void ValidatePoll(CommandTransportPollRequest request, DateTimeOffset receivedAt)
    {
        ArgumentNullException.ThrowIfNull(request);
        string json = CommandTransportCodec.Serialize(request);
        if (request.MessageId == Guid.Empty || request.SchemaVersion != CommandTransportProtocol.CurrentSchemaVersion ||
            request.AgentId == Guid.Empty || request.Sequence < 1 || request.OccurredAt.Offset != TimeSpan.Zero ||
            request.SentAt.Offset != TimeSpan.Zero || request.SentAt < request.OccurredAt ||
            request.SentAt > receivedAt.AddMinutes(1) || string.IsNullOrWhiteSpace(request.AgentVersion) ||
            request.AgentVersion.Length > 64 || request.ProviderVersions is null ||
            request.ProviderVersions.Count > CommandTransportProtocol.MaximumProviderVersions ||
            request.ProviderVersions.Any(item => string.IsNullOrWhiteSpace(item.Key) || item.Key.Length > 64 ||
                string.IsNullOrWhiteSpace(item.Value) || item.Value.Length > 64) ||
            request.MaximumCount is < 1 or > CommandTransportProtocol.MaximumBatchSize ||
            Encoding.UTF8.GetByteCount(json) > CommandTransportProtocol.MaximumHttpBodyBytes)
        {
            throw new CommandTransportException(CommandTransportFailureKind.Invalid, "command.transport_poll_invalid");
        }
    }

    private static void ValidateAcknowledgement(
        CommandTransportAcknowledgementRequest request,
        DateTimeOffset receivedAt)
    {
        ArgumentNullException.ThrowIfNull(request);
        string json = CommandTransportCodec.Serialize(request);
        if (request.MessageId == Guid.Empty || request.SchemaVersion != CommandTransportProtocol.CurrentSchemaVersion ||
            request.AgentId == Guid.Empty || request.Sequence < 1 || request.OccurredAt.Offset != TimeSpan.Zero ||
            request.SentAt.Offset != TimeSpan.Zero || request.SentAt < request.OccurredAt ||
            request.SentAt > receivedAt.AddMinutes(1) || request.Items is null ||
            request.Items.Count is < 1 or > CommandTransportProtocol.MaximumBatchSize ||
            request.Items.Select(item => item.CommandId).Distinct().Count() != request.Items.Count ||
            request.Items.Any(item => item.CommandId == Guid.Empty ||
                string.IsNullOrWhiteSpace(item.IdempotencyKey) || item.IdempotencyKey.Length > 200 ||
                item.AcknowledgedAt.Offset != TimeSpan.Zero || item.AcknowledgedAt > receivedAt.AddMinutes(1) ||
                item.ReasonCode?.Length > 100 ||
                item.Disposition is CommandTransportDisposition.Duplicate) ||
            Encoding.UTF8.GetByteCount(json) > CommandTransportProtocol.MaximumHttpBodyBytes)
        {
            throw new CommandTransportException(CommandTransportFailureKind.Invalid, "command.transport_ack_invalid");
        }
    }

    private static bool IsJsonObject(string json)
    {
        try
        {
            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json);
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
