// Module purpose: Implements Server Observation Ingestion Store for central PostgreSQL persistence with transactional and authorisation boundaries.
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using DBNotifier.Application.Synchronization;
using DBNotifier.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Server.PostgreSql;

/// <summary>
/// Persists authenticated Agent observations and reconciles each Agent's contiguous sequence into canonical
/// instance state, events and outbox records under serialisable and in-process concurrency boundaries.
/// </summary>
/// <param name="contextFactory">Factory for isolated central persistence contexts.</param>
public sealed class ServerObservationIngestionStore(
    IDbContextFactory<ServerDbContext> contextFactory) : IObservationIngestionStore
{
    private const int ReconciliationBatchSize = 1000;
    private const int MaximumReconciliationBatches = 10;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> AgentGates = new();

    /// <inheritdoc />
    public async ValueTask<ObservationItemResult> IngestAsync(
        ObservationSyncMessage message,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        // One in-process writer per Agent avoids competing cursor creation and preserves deterministic
        // sequence reconciliation. The serialisable transaction and PostgreSQL row lock provide the
        // corresponding cross-process boundary.
        SemaphoreSlim gate = AgentGates.GetOrAdd(message.AgentId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await IngestCoreAsync(message, receivedAt, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            return await ClassifyAfterPersistenceFailureAsync(message, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Persists and reconciles one validated message inside a serialisable central transaction.</summary>
    private async ValueTask<ObservationItemResult> IngestCoreAsync(
        ObservationSyncMessage message,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);

        bool activeAgent = await context.Agents
            .AnyAsync(row => row.AgentId == message.AgentId && row.State == "Active", cancellationToken)
            .ConfigureAwait(false);
        if (!activeAgent)
        {
            return Rejected(message.MessageId, "agent.not_active");
        }

        string? assignedProvider = await context.Instances
            .Where(
                row => row.InstanceId == message.InstanceId &&
                    row.AssignedAgentId == message.AgentId &&
                    row.Enabled && row.ArchivedAt == null)
            .Select(row => row.ProviderType)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (assignedProvider is null)
        {
            return Rejected(message.MessageId, "observation.instance_not_assigned");
        }

        if (!string.Equals(assignedProvider, message.ProviderType, StringComparison.Ordinal))
        {
            return Rejected(message.MessageId, "observation.provider_mismatch");
        }

        ObservationItemResult? existingResult = await ClassifyExistingAsync(
            context,
            message,
            cancellationToken).ConfigureAwait(false);
        if (existingResult is not null)
        {
            if (existingResult.Disposition == ObservationIngestionDisposition.Duplicate)
            {
                AgentObservationCursorRow existingCursor = await GetOrCreateCursorAsync(
                    context,
                    message.AgentId,
                    receivedAt,
                    cancellationToken).ConfigureAwait(false);
                bool reconciliationComplete = await ReconcileContiguousAsync(
                    context,
                    existingCursor,
                    receivedAt,
                    cancellationToken).ConfigureAwait(false);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                if (!reconciliationComplete)
                {
                    return Retryable(message.MessageId, "ingestion.reconciliation_limit_reached");
                }
            }

            return existingResult;
        }

        AgentObservationCursorRow cursor = await GetOrCreateCursorAsync(
            context,
            message.AgentId,
            receivedAt,
            cancellationToken).ConfigureAwait(false);
        context.HealthSamples.Add(ToHealthSample(message, receivedAt));

        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            bool reconciliationComplete = await ReconcileContiguousAsync(
                context,
                cursor,
                receivedAt,
                cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return reconciliationComplete
                ? new ObservationItemResult(message.MessageId, ObservationIngestionDisposition.Accepted)
                : Retryable(message.MessageId, "ingestion.reconciliation_limit_reached");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return await ClassifyAfterPersistenceFailureAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Classifies a persistence race as a proved replay conflict or a retryable unavailable outcome.</summary>
    private async ValueTask<ObservationItemResult> ClassifyAfterPersistenceFailureAsync(
        ObservationSyncMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await using ServerDbContext verification = await contextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);
            ObservationItemResult? concurrentResult = await ClassifyExistingAsync(
                verification,
                message,
                cancellationToken).ConfigureAwait(false);
            return concurrentResult ?? Retryable(message.MessageId, "ingestion.persistence_failed");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is DbException or InvalidOperationException)
        {
            return Retryable(message.MessageId, "ingestion.persistence_unavailable");
        }
    }

    /// <summary>Compares identifiers and payload hashes so only exact replay is accepted as a duplicate.</summary>
    private static async ValueTask<ObservationItemResult?> ClassifyExistingAsync(
        ServerDbContext context,
        ObservationSyncMessage message,
        CancellationToken cancellationToken)
    {
        HealthSampleRow[] existing = await context.HealthSamples
            .AsNoTracking()
            .Where(row =>
                row.MessageId == message.MessageId ||
                row.ObservationId == message.ObservationId ||
                (row.AgentId == message.AgentId && row.Sequence == message.Sequence))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        if (existing.Length == 0)
        {
            return null;
        }

        string payloadHash = ComputePayloadHash(message);
        if (existing.Length == 1 &&
            (existing[0].MessageId == message.MessageId || existing[0].ObservationId == message.ObservationId) &&
            string.Equals(existing[0].PayloadHash, payloadHash, StringComparison.Ordinal))
        {
            return new ObservationItemResult(message.MessageId, ObservationIngestionDisposition.Duplicate);
        }

        if (existing.Any(row => row.MessageId == message.MessageId || row.ObservationId == message.ObservationId))
        {
            // A matching id with a different or unprovable legacy payload is terminal. Retrying it as
            // a duplicate could silently accept tampered content.
            return Rejected(message.MessageId, "observation.idempotency_conflict");
        }

        return Rejected(message.MessageId, "observation.sequence_conflict");
    }

    /// <summary>Reads the durable highest contiguous sequence accepted for one Agent.</summary>
    /// <param name="agentId">Authenticated Agent whose acknowledgement cursor is requested.</param>
    /// <param name="cancellationToken">Cancellation propagated from the synchronisation request.</param>
    /// <returns>The highest reconciled sequence, or zero when the Agent has no cursor.</returns>
    public async ValueTask<long> GetHighestContiguousSequenceAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        return await context.AgentObservationCursors
            .AsNoTracking()
            .Where(row => row.AgentId == agentId)
            .Select(row => row.HighestContiguousSequence)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Maps one validated message to its sanitised immutable raw-sample row.</summary>
    private static HealthSampleRow ToHealthSample(ObservationSyncMessage message, DateTimeOffset receivedAt) =>
        new()
        {
            ObservationId = message.ObservationId,
            InstanceId = message.InstanceId,
            AgentId = message.AgentId,
            MessageId = message.MessageId,
            Sequence = message.Sequence,
            ProviderType = message.ProviderType,
            ProviderVersion = message.ProviderVersion,
            Status = message.Status,
            Method = message.Method,
            EvidenceLevel = message.EvidenceLevel,
            ObservedAt = message.ObservedAt,
            ReceivedAt = receivedAt,
            DurationMilliseconds = message.DurationMilliseconds,
            AttemptCount = message.AttemptCount,
            ErrorCode = message.ErrorCode,
            RedactedDetailsJson = JsonSerializer.Serialize(
                new { message.SafeErrorMessage, message.Limitations },
                SerializerOptions),
            PayloadHash = ComputePayloadHash(message),
        };

    /// <summary>Derives one canonical event and bounded pending channel records from reconciled state change.</summary>
    private static void AddEventAndAlertDeliveries(
        ServerDbContext context,
        HealthSampleRow sample,
        CanonicalEventCandidate candidate,
        IReadOnlyList<AlertRuleRow> rules,
        IReadOnlyList<Guid> channelIds)
    {
        Guid eventId = Guid.NewGuid();
        EventRecordRow eventRow = new()
        {
            EventId = eventId,
            InstanceId = sample.InstanceId,
            AgentId = sample.AgentId,
            SourceObservationId = sample.ObservationId,
            CorrelationId = sample.MessageId,
            EventType = candidate.EventType,
            Severity = candidate.Severity,
            ObservedAt = sample.ObservedAt,
            ReceivedAt = sample.ReceivedAt,
            DetailsJson = JsonSerializer.Serialize(
                new { previousStateChanged = true, currentStatus = sample.Status },
                SerializerOptions),
        };
        context.Events.Add(eventRow);
        context.OutboxMessages.Add(new ServerOutboxMessageRow
        {
            MessageId = eventId,
            MessageType = "canonical.event.v1",
            SchemaVersion = 1,
            PayloadJson = JsonSerializer.Serialize(new
            {
                eventRow.EventId,
                eventRow.EventType,
                eventRow.Severity,
                eventRow.InstanceId,
                eventRow.AgentId,
                eventRow.SourceObservationId,
                eventRow.CorrelationId,
                eventRow.ObservedAt,
                eventRow.ReceivedAt,
            }, SerializerOptions),
            OccurredAt = sample.ObservedAt,
            CreatedAt = sample.ReceivedAt,
            AvailableAt = sample.ReceivedAt,
            AttemptCount = 0,
        });

        if (!rules.Any(rule => Matches(rule, candidate.EventType, sample.InstanceId)))
        {
            return;
        }

        context.NotificationDeliveries.AddRange(channelIds.Select(channelId => new NotificationDeliveryRow
        {
            NotificationDeliveryId = Guid.NewGuid(),
            NotificationChannelId = channelId,
            EventId = eventId,
            State = "Pending",
            AttemptCount = 0,
            CreatedAt = sample.ReceivedAt,
        }));
    }

    /// <summary>Locks or creates the durable per-Agent reconciliation cursor inside the active transaction.</summary>
    private static async ValueTask<AgentObservationCursorRow> GetOrCreateCursorAsync(
        ServerDbContext context,
        Guid agentId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        AgentObservationCursorRow? cursor;
        if (string.Equals(context.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
        {
            cursor = await context.AgentObservationCursors
                .FromSqlInterpolated($"SELECT * FROM agent_observation_cursors WHERE agent_id = {agentId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            cursor = await context.AgentObservationCursors
                .SingleOrDefaultAsync(row => row.AgentId == agentId, cancellationToken)
                .ConfigureAwait(false);
        }

        if (cursor is not null)
        {
            return cursor;
        }

        cursor = new AgentObservationCursorRow
        {
            AgentId = agentId,
            HighestContiguousSequence = 0,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid(),
        };
        context.AgentObservationCursors.Add(cursor);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return cursor;
    }

    /// <summary>
    /// Advances one Agent cursor in bounded batches and derives state only for instances still actively assigned
    /// to that Agent, preventing delayed observations from a former owner from mutating current truth.
    /// </summary>
    /// <param name="context">Central persistence context participating in the serialisable transaction.</param>
    /// <param name="cursor">Locked durable cursor for the authenticated Agent.</param>
    /// <param name="reconciledAt">Authoritative UTC reconciliation instant.</param>
    /// <param name="cancellationToken">Cancellation propagated from the ingestion request.</param>
    /// <returns><see langword="true"/> when no bounded continuation is required.</returns>
    private static async ValueTask<bool> ReconcileContiguousAsync(
        ServerDbContext context,
        AgentObservationCursorRow cursor,
        DateTimeOffset reconciledAt,
        CancellationToken cancellationToken)
    {
        Dictionary<Guid, InstanceObservationStateRow> states = [];
        AlertRuleRow[] rules = await context.AlertRules
            .AsNoTracking()
            .Where(row => row.Enabled && row.ArchivedAt == null && row.RuleType == "CanonicalEvent")
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        Guid[] channelIds = await context.NotificationChannels
            .AsNoTracking()
            .Where(row => row.Enabled)
            .Select(row => row.NotificationChannelId)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        for (int batch = 0; batch < MaximumReconciliationBatches; batch++)
        {
            HealthSampleRow[] candidates = await context.HealthSamples
                .Where(row => row.AgentId == cursor.AgentId && row.Sequence > cursor.HighestContiguousSequence)
                .OrderBy(row => row.Sequence)
                .Take(ReconciliationBatchSize)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
            List<HealthSampleRow> contiguous = [];
            long expected = cursor.HighestContiguousSequence + 1;
            foreach (HealthSampleRow candidate in candidates)
            {
                if (candidate.Sequence != expected)
                {
                    break;
                }

                contiguous.Add(candidate);
                expected++;
            }

            if (contiguous.Count == 0)
            {
                return true;
            }

            Guid[] batchInstanceIds = contiguous
                .Select(row => row.InstanceId)
                .Distinct()
                .ToArray();
            HashSet<Guid> currentlyOwnedInstanceIds = (await context.Instances
                .AsNoTracking()
                .Where(row =>
                    batchInstanceIds.Contains(row.InstanceId) &&
                    row.AssignedAgentId == cursor.AgentId &&
                    row.Enabled &&
                    row.ArchivedAt == null)
                .Select(row => row.InstanceId)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false))
                .ToHashSet();
            Guid[] unknownInstanceIds = currentlyOwnedInstanceIds
                .Where(instanceId => !states.ContainsKey(instanceId))
                .ToArray();
            InstanceObservationStateRow[] loadedStates = await context.InstanceObservationStates
                .Where(row => unknownInstanceIds.Contains(row.InstanceId))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (InstanceObservationStateRow loadedState in loadedStates)
            {
                states.Add(loadedState.InstanceId, loadedState);
            }

            foreach (HealthSampleRow sample in contiguous)
            {
                if (!currentlyOwnedInstanceIds.Contains(sample.InstanceId))
                {
                    // Retain sequence continuity without allowing a former owner to mutate the
                    // current instance state or derive events after reassignment or archival.
                    cursor.HighestContiguousSequence = sample.Sequence;
                    continue;
                }

                states.TryGetValue(sample.InstanceId, out InstanceObservationStateRow? state);
                HealthStatus? previous = state is null
                    ? null
                    : Enum.Parse<HealthStatus>(state.Status, ignoreCase: false);
                HealthStatus current = Enum.Parse<HealthStatus>(sample.Status, ignoreCase: false);
                CanonicalEventCandidate? candidate = ObservationEventDeriver.Derive(previous, current);
                if (candidate is not null)
                {
                    AddEventAndAlertDeliveries(context, sample, candidate, rules, channelIds);
                }

                if (state is null)
                {
                    state = new InstanceObservationStateRow
                    {
                        InstanceId = sample.InstanceId,
                        AgentId = sample.AgentId,
                        LastProcessedSequence = sample.Sequence,
                        ObservationId = sample.ObservationId,
                        Status = sample.Status,
                        ObservedAt = sample.ObservedAt,
                        ReceivedAt = sample.ReceivedAt,
                        ConcurrencyToken = Guid.NewGuid(),
                    };
                    context.InstanceObservationStates.Add(state);
                    states.Add(sample.InstanceId, state);
                }
                else
                {
                    state.AgentId = sample.AgentId;
                    state.LastProcessedSequence = sample.Sequence;
                    state.ObservationId = sample.ObservationId;
                    state.Status = sample.Status;
                    state.ObservedAt = sample.ObservedAt;
                    state.ReceivedAt = sample.ReceivedAt;
                    state.ConcurrencyToken = Guid.NewGuid();
                }

                cursor.HighestContiguousSequence = sample.Sequence;
            }

            cursor.UpdatedAt = reconciledAt;
            cursor.ConcurrencyToken = Guid.NewGuid();
            if (contiguous.Count < ReconciliationBatchSize)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Computes a deterministic SHA-256 hash over every replay-significant payload field.</summary>
    private static string ComputePayloadHash(ObservationSyncMessage message)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            message.SchemaVersion,
            message.Sequence,
            message.ObservationId,
            message.InstanceId,
            message.AgentId,
            message.ProviderType,
            message.ProviderVersion,
            message.Status,
            message.Method,
            message.EvidenceLevel,
            message.AttemptCount,
            message.ObservedAt,
            message.DurationMilliseconds,
            message.ErrorCode,
            message.SafeErrorMessage,
            message.Limitations,
        }, SerializerOptions);
        return Convert.ToHexString(SHA256.HashData(payload));
    }

    private static bool Matches(AlertRuleRow rule, string eventType, Guid instanceId)
    {
        try
        {
            using JsonDocument configuration = JsonDocument.Parse(rule.ConfigurationJson);
            if (!configuration.RootElement.TryGetProperty("eventTypes", out JsonElement eventTypes) ||
                eventTypes.ValueKind != JsonValueKind.Array ||
                !eventTypes.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.String &&
                    string.Equals(item.GetString(), eventType, StringComparison.Ordinal)))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(rule.InstanceScopeJson))
            {
                return true;
            }

            using JsonDocument scope = JsonDocument.Parse(rule.InstanceScopeJson);
            return scope.RootElement.TryGetProperty("instanceIds", out JsonElement instanceIds) &&
                instanceIds.ValueKind == JsonValueKind.Array &&
                instanceIds.EnumerateArray().Any(item =>
                    item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out Guid value) &&
                    value == instanceId);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static ObservationItemResult Rejected(Guid messageId, string errorCode) =>
        new(messageId, ObservationIngestionDisposition.Rejected, errorCode);

    /// <summary>Creates a stable retryable ingestion result without provider or persistence details.</summary>
    private static ObservationItemResult Retryable(Guid messageId, string errorCode) =>
        new(messageId, ObservationIngestionDisposition.Retryable, errorCode);
}
