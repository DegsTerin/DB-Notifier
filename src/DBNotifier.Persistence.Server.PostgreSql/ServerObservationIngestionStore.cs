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
/// Persists authenticated Agent observations, consumes identifiable terminal rejections and reconciles each Agent's
/// contiguous stream into canonical instance state, events and outbox records under serialisable and in-process
/// concurrency boundaries.
/// </summary>
/// <param name="contextFactory">Factory for isolated central persistence contexts.</param>
public sealed class ServerObservationIngestionStore(
    IDbContextFactory<ServerDbContext> contextFactory) :
    IObservationIngestionStore,
    IRejectedObservationSequenceStore
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

    /// <inheritdoc />
    public async ValueTask<ObservationItemResult> ConsumeRejectedAsync(
        Guid agentId,
        Guid messageId,
        long sequence,
        string errorCode,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(messageId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfLessThan(sequence, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        if (errorCode.Length > 100)
        {
            throw new ArgumentException("Rejected observation error codes cannot exceed 100 characters.", nameof(errorCode));
        }

        SemaphoreSlim gate = AgentGates.GetOrAdd(agentId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ConsumeRejectedCoreAsync(
                agentId,
                messageId,
                sequence,
                errorCode,
                receivedAt,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            return Retryable(messageId, "ingestion.persistence_unavailable");
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Consumes one rejected sequence inside a serialisable transaction when the Agent is durably known.</summary>
    private async ValueTask<ObservationItemResult> ConsumeRejectedCoreAsync(
        Guid agentId,
        Guid messageId,
        long sequence,
        string errorCode,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);

        bool knownAgent = await context.Agents
            .AnyAsync(row => row.AgentId == agentId, cancellationToken)
            .ConfigureAwait(false);
        if (!knownAgent)
        {
            return Retryable(messageId, "ingestion.rejection_agent_unknown");
        }

        return await ConsumeRejectedInTransactionAsync(
            context,
            transaction,
            agentId,
            messageId,
            sequence,
            errorCode,
            receivedAt,
            cancellationToken).ConfigureAwait(false);
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

        string? agentState = await context.Agents
            .Where(row => row.AgentId == message.AgentId)
            .Select(row => row.State)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (agentState is null)
        {
            return Rejected(message.MessageId, "agent.not_active");
        }
        if (!string.Equals(agentState, "Active", StringComparison.Ordinal))
        {
            return await ConsumeRejectedInTransactionAsync(
                context,
                transaction,
                message.AgentId,
                message.MessageId,
                message.Sequence,
                "agent.not_active",
                receivedAt,
                cancellationToken).ConfigureAwait(false);
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
            return await ConsumeRejectedInTransactionAsync(
                context,
                transaction,
                message.AgentId,
                message.MessageId,
                message.Sequence,
                "observation.instance_not_assigned",
                receivedAt,
                cancellationToken).ConfigureAwait(false);
        }

        if (!string.Equals(assignedProvider, message.ProviderType, StringComparison.Ordinal))
        {
            return await ConsumeRejectedInTransactionAsync(
                context,
                transaction,
                message.AgentId,
                message.MessageId,
                message.Sequence,
                "observation.provider_mismatch",
                receivedAt,
                cancellationToken).ConfigureAwait(false);
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

                return existingResult;
            }

            return await ConsumeRejectedInTransactionAsync(
                context,
                transaction,
                message.AgentId,
                message.MessageId,
                message.Sequence,
                existingResult.ErrorCode ?? "observation.conflict",
                receivedAt,
                cancellationToken).ConfigureAwait(false);
        }

        AgentObservationCursorRow cursor = await GetOrCreateCursorAsync(
            context,
            message.AgentId,
            receivedAt,
            cancellationToken).ConfigureAwait(false);
        if (message.Sequence <= cursor.HighestContiguousSequence)
        {
            return Rejected(message.MessageId, "observation.sequence_conflict");
        }

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

    /// <summary>
    /// Resolves stored contiguous samples before consuming exactly the next rejected stream position in the same
    /// transaction, without creating raw health evidence for the rejection.
    /// </summary>
    /// <param name="context">Central persistence context participating in the serialisable transaction.</param>
    /// <param name="transaction">Transaction that owns the cursor lock and commit.</param>
    /// <param name="agentId">Durably known Agent whose stream position is being resolved.</param>
    /// <param name="messageId">Rejected message identifier returned to the Agent.</param>
    /// <param name="sequence">Positive rejected stream sequence.</param>
    /// <param name="errorCode">Stable terminal reason returned after durable consumption.</param>
    /// <param name="receivedAt">Authoritative Server receipt instant.</param>
    /// <param name="cancellationToken">Cancellation propagated from the ingestion request.</param>
    /// <returns>A terminal rejection after durable consumption, or a retryable result while a lower gap remains.</returns>
    private static async ValueTask<ObservationItemResult> ConsumeRejectedInTransactionAsync(
        ServerDbContext context,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        Guid agentId,
        Guid messageId,
        long sequence,
        string errorCode,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        AgentObservationCursorRow cursor = await GetOrCreateCursorAsync(
            context,
            agentId,
            receivedAt,
            cancellationToken).ConfigureAwait(false);
        bool reconciliationComplete = await ReconcileContiguousAsync(
            context,
            cursor,
            receivedAt,
            cancellationToken).ConfigureAwait(false);
        if (!reconciliationComplete)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Retryable(messageId, "ingestion.reconciliation_limit_reached");
        }

        if (sequence <= cursor.HighestContiguousSequence)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Rejected(messageId, errorCode);
        }

        long nextSequence = cursor.HighestContiguousSequence + 1;
        if (sequence != nextSequence)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Retryable(messageId, "ingestion.rejection_gap");
        }

        cursor.HighestContiguousSequence = sequence;
        cursor.UpdatedAt = receivedAt;
        cursor.ConcurrencyToken = Guid.NewGuid();
        reconciliationComplete = await ReconcileContiguousAsync(
            context,
            cursor,
            receivedAt,
            cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return reconciliationComplete
            ? Rejected(messageId, errorCode)
            : Retryable(messageId, "ingestion.reconciliation_limit_reached");
    }

    /// <summary>Reads the highest Agent sequence durably resolved as an accepted observation or terminal rejection.</summary>
    /// <param name="agentId">Authenticated Agent whose acknowledgement cursor is requested.</param>
    /// <param name="cancellationToken">Cancellation propagated from the synchronisation request.</param>
    /// <returns>The highest contiguously resolved sequence, or zero when the Agent has no cursor.</returns>
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

    /// <summary>
    /// Derives one canonical event and only those pending deliveries whose active rule, channel, environment and
    /// instance scope are all proven by an explicit durable binding.
    /// </summary>
    private static void AddEventAndAlertDeliveries(
        ServerDbContext context,
        HealthSampleRow sample,
        CanonicalEventCandidate candidate,
        string instanceEnvironment,
        Dictionary<Guid, AlertRuleRow> rules,
        IReadOnlyList<AlertRuleChannelBindingRow> bindings,
        HashSet<Guid> enabledChannelIds)
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
                new
                {
                    schemaVersion = "canonical-event-details.v1",
                    previousStatus = candidate.PreviousStatus?.ToString(),
                    currentStatus = candidate.CurrentStatus.ToString(),
                },
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

        IEnumerable<AlertRuleChannelBindingRow> provenBindings = bindings.Where(binding =>
            string.Equals(binding.Environment, instanceEnvironment, StringComparison.Ordinal) &&
            enabledChannelIds.Contains(binding.NotificationChannelId) &&
            rules.TryGetValue(binding.AlertRuleId, out AlertRuleRow? rule) &&
            AlertRoutingPolicy.Matches(rule, candidate.EventType, sample.InstanceId));
        context.NotificationDeliveries.AddRange(provenBindings.Select(binding => new NotificationDeliveryRow
        {
            NotificationDeliveryId = Guid.NewGuid(),
            NotificationChannelId = binding.NotificationChannelId,
            EventId = eventId,
            AlertRuleChannelBindingId = binding.AlertRuleChannelBindingId,
            IdempotencyKey = DeliveryIdempotencyKey(eventId, binding.AlertRuleChannelBindingId),
            State = "Pending",
            AttemptCount = 0,
            CreatedAt = sample.ReceivedAt,
        }));
    }

    /// <summary>Locks or creates the durable per-Agent stream-resolution cursor inside the active transaction.</summary>
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
        Dictionary<Guid, AlertRuleRow> rules = await context.AlertRules
            .AsNoTracking()
            .Where(row => row.Enabled && row.ArchivedAt == null && row.RuleType == "CanonicalEvent")
            .ToDictionaryAsync(row => row.AlertRuleId, cancellationToken)
            .ConfigureAwait(false);
        AlertRuleChannelBindingRow[] bindings = await context.AlertRuleChannelBindings
            .AsNoTracking()
            .Where(row => row.Enabled)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        HashSet<Guid> enabledChannelIds = (await context.NotificationChannels
            .AsNoTracking()
            .Where(row => row.Enabled)
            .Select(row => row.NotificationChannelId)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false))
            .ToHashSet();

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
            Dictionary<Guid, string> currentlyOwnedInstances = await context.Instances
                .AsNoTracking()
                .Where(row =>
                    batchInstanceIds.Contains(row.InstanceId) &&
                    row.AssignedAgentId == cursor.AgentId &&
                    row.Enabled &&
                    row.ArchivedAt == null)
                .ToDictionaryAsync(row => row.InstanceId, row => row.Environment, cancellationToken)
                .ConfigureAwait(false);
            Guid[] unknownInstanceIds = currentlyOwnedInstances.Keys
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
                if (!currentlyOwnedInstances.TryGetValue(sample.InstanceId, out string? instanceEnvironment))
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
                    AddEventAndAlertDeliveries(
                        context,
                        sample,
                        candidate,
                        instanceEnvironment,
                        rules,
                        bindings,
                        enabledChannelIds);
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

    /// <summary>Builds the bounded deterministic idempotency identity for one event and proven binding.</summary>
    private static string DeliveryIdempotencyKey(Guid eventId, Guid bindingId) =>
        $"notification:{eventId:N}:{bindingId:N}";

    private static ObservationItemResult Rejected(Guid messageId, string errorCode) =>
        new(messageId, ObservationIngestionDisposition.Rejected, errorCode);

    /// <summary>Creates a stable retryable ingestion result without provider or persistence details.</summary>
    private static ObservationItemResult Retryable(Guid messageId, string errorCode) =>
        new(messageId, ObservationIngestionDisposition.Retryable, errorCode);
}
