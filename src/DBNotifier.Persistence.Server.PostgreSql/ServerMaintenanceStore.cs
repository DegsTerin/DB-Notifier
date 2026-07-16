// Module purpose: Applies bounded maintenance policies to central PostgreSQL persistence without accessing monitored databases.
using DBNotifier.Application.Operations;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Server.PostgreSql;

/// <summary>
/// Owns bounded central retention queries and future delivery-store state transitions. Raw observations remain
/// authoritative and are retained until an aggregate-before-delete contract is durably implemented.
/// </summary>
/// <param name="contextFactory">Factory for isolated central PostgreSQL persistence contexts.</param>
public sealed class ServerMaintenanceStore(
    IDbContextFactory<ServerDbContext> contextFactory) :
    IServerRetentionStore,
    IServerOutboxStore,
    INotificationDeliveryStore
{
    private static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromMinutes(5);

    /// <summary>Evaluates or applies one bounded central retention cycle while preserving all raw observations.</summary>
    /// <param name="request">UTC cut-off instant, per-class row bound and explicit apply flag.</param>
    /// <param name="cancellationToken">Cancellation propagated from the maintenance worker.</param>
    /// <returns>Counts of eligible or deleted rows for each central retention class.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the per-class row bound is outside policy.</exception>
    public async ValueTask<RetentionExecutionResult> ExecuteAsync(
        RetentionExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRetention(request);
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        DateTimeOffset heartbeatCutoff = request.Now.AddDays(-90);
        DateTimeOffset deliveryCutoff = request.Now.AddMonths(-12);
        DateTimeOffset outboxCutoff = request.Now.AddDays(-7);
        // Raw observations remain authoritative until the aggregate-before-delete contract has a
        // durable implementation. Keeping them is the fail-closed retention outcome and the Agent
        // cursor remains independently durable when that later deletion path is introduced.
        HealthSampleRow[] observations = [];
        AgentHeartbeatRow[] heartbeats = await context.AgentHeartbeats
            .FromSqlInterpolated($$"""
                SELECT * FROM agent_heartbeats
                WHERE received_at < {{heartbeatCutoff}}
                ORDER BY received_at
                LIMIT {{request.MaximumRowsPerClass}}
                """)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        NotificationDeliveryRow[] deliveries = await context.NotificationDeliveries
            .FromSqlInterpolated($$"""
                SELECT * FROM notification_deliveries
                WHERE delivered_at IS NOT NULL AND delivered_at < {{deliveryCutoff}}
                ORDER BY delivered_at
                LIMIT {{request.MaximumRowsPerClass}}
                """)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        ServerOutboxMessageRow[] outboxTombstones = await context.OutboxMessages
            .FromSqlInterpolated($$"""
                SELECT * FROM outbox_messages
                WHERE published_at IS NOT NULL AND published_at < {{outboxCutoff}}
                ORDER BY published_at
                LIMIT {{request.MaximumRowsPerClass}}
                """)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        if (request.ApplyChanges)
        {
            context.HealthSamples.RemoveRange(observations);
            context.AgentHeartbeats.RemoveRange(heartbeats);
            context.NotificationDeliveries.RemoveRange(deliveries);
            context.OutboxMessages.RemoveRange(outboxTombstones);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new RetentionExecutionResult(
            observations.Length,
            0,
            0,
            heartbeats.Length,
            deliveries.Length,
            outboxTombstones.Length);
    }

    /// <summary>Reads a bounded ordered set of unpublished central outbox messages.</summary>
    /// <param name="now">UTC instant used to exclude messages not yet available.</param>
    /// <param name="maximumCount">Maximum number of messages between one and 100.</param>
    /// <param name="cancellationToken">Cancellation propagated from a future delivery cycle.</param>
    /// <returns>Unpublished messages in deterministic creation order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maximumCount"/> is outside policy.</exception>
    public async ValueTask<IReadOnlyList<ServerOutboxEnvelope>> GetPendingAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        ValidateBatchSize(maximumCount);
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        ServerOutboxMessageRow[] rows = await context.OutboxMessages
            .FromSqlInterpolated($$"""
                SELECT * FROM outbox_messages
                WHERE published_at IS NULL AND available_at <= {{now}}
                ORDER BY created_at, message_id
                LIMIT {{maximumCount}}
                """)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows
            .Select(row => new ServerOutboxEnvelope(
                row.MessageId,
                row.MessageType,
                row.SchemaVersion,
                row.PayloadJson,
                row.OccurredAt,
                row.AttemptCount))
            .ToArray();
    }

    /// <inheritdoc />
    async ValueTask IServerOutboxStore.ApplyResultsAsync(
        IReadOnlyList<DeliveryResult> results,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (results.Count == 0)
        {
            return;
        }

        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        Dictionary<Guid, DeliveryResult> byId = results
            .GroupBy(result => result.ItemId)
            .ToDictionary(group => group.Key, group => group.First());
        Guid[] ids = byId.Keys.ToArray();
        ServerOutboxMessageRow[] rows = await context.OutboxMessages
            .Where(row => ids.Contains(row.MessageId) && row.PublishedAt == null)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (ServerOutboxMessageRow row in rows)
        {
            DeliveryResult result = byId[row.MessageId];
            row.AttemptCount = checked(row.AttemptCount + 1);
            if (result.Disposition == DeliveryDisposition.Delivered)
            {
                row.PublishedAt = now;
            }
            else
            {
                row.AvailableAt = now.Add(RetryDelay(row.AttemptCount));
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async ValueTask<IReadOnlyList<NotificationEnvelope>> INotificationDeliveryStore.GetPendingAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        ValidateBatchSize(maximumCount);
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        int candidateLimit = maximumCount * 4;
        NotificationDeliveryRow[] deliveryCandidates = await context.NotificationDeliveries
            .FromSqlInterpolated($$"""
                SELECT * FROM notification_deliveries
                WHERE state = 'Pending'
                ORDER BY created_at, notification_delivery_id
                LIMIT {{candidateLimit}}
                """)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        Guid[] channelIds = deliveryCandidates.Select(row => row.NotificationChannelId).Distinct().ToArray();
        Guid[] eventIds = deliveryCandidates.Select(row => row.EventId).Distinct().ToArray();
        Dictionary<Guid, NotificationChannelRow> channels = await context.NotificationChannels
            .AsNoTracking()
            .Where(row => channelIds.Contains(row.NotificationChannelId) && row.Enabled)
            .ToDictionaryAsync(row => row.NotificationChannelId, cancellationToken)
            .ConfigureAwait(false);
        Dictionary<Guid, EventRecordRow> events = await context.Events
            .AsNoTracking()
            .Where(row => eventIds.Contains(row.EventId))
            .ToDictionaryAsync(row => row.EventId, cancellationToken)
            .ConfigureAwait(false);
        return deliveryCandidates
            .Where(delivery => delivery.LastAttemptAt is null ||
                delivery.LastAttemptAt.Value.Add(RetryDelay(delivery.AttemptCount)) <= now)
            .Where(delivery => channels.ContainsKey(delivery.NotificationChannelId) &&
                events.ContainsKey(delivery.EventId))
            .Take(maximumCount)
            .Select(delivery => new NotificationEnvelope(
                delivery.NotificationDeliveryId,
                delivery.NotificationChannelId,
                delivery.EventId,
                channels[delivery.NotificationChannelId].ChannelType,
                channels[delivery.NotificationChannelId].NonSecretConfigurationJson,
                events[delivery.EventId].EventType,
                events[delivery.EventId].Severity,
                events[delivery.EventId].DetailsJson,
                delivery.AttemptCount))
            .ToArray();
    }

    /// <inheritdoc />
    async ValueTask INotificationDeliveryStore.ApplyResultsAsync(
        IReadOnlyList<DeliveryResult> results,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (results.Count == 0)
        {
            return;
        }

        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        Dictionary<Guid, DeliveryResult> byId = results
            .GroupBy(result => result.ItemId)
            .ToDictionary(group => group.Key, group => group.First());
        Guid[] ids = byId.Keys.ToArray();
        NotificationDeliveryRow[] rows = await context.NotificationDeliveries
            .Where(row => ids.Contains(row.NotificationDeliveryId) && row.State == "Pending")
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (NotificationDeliveryRow row in rows)
        {
            DeliveryResult result = byId[row.NotificationDeliveryId];
            row.AttemptCount = checked(row.AttemptCount + 1);
            row.LastAttemptAt = now;
            if (result.Disposition == DeliveryDisposition.Delivered)
            {
                row.State = "Delivered";
                row.DeliveredAt = now;
                row.ErrorCode = null;
            }
            else
            {
                row.ErrorCode = BoundErrorCode(result.ErrorCode);
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Calculates a bounded exponential retry delay from a one-based delivery attempt count.</summary>
    private static TimeSpan RetryDelay(int attemptCount) =>
        TimeSpan.FromSeconds(Math.Min(
            Math.Pow(2, Math.Min(Math.Max(attemptCount, 1), 8)),
            MaximumRetryDelay.TotalSeconds));

    /// <summary>Normalises a retryable delivery code to the central persistence length boundary.</summary>
    private static string BoundErrorCode(string? errorCode) =>
        string.IsNullOrWhiteSpace(errorCode)
            ? "delivery.retryable"
            : errorCode[..Math.Min(errorCode.Length, 100)];

    /// <summary>Enforces the shared central delivery batch bound.</summary>
    private static void ValidateBatchSize(int maximumCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumCount, 100);
    }

    /// <summary>Enforces the central retention request and per-class row bounds.</summary>
    private static void ValidateRetention(RetentionExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.MaximumRowsPerClass, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.MaximumRowsPerClass, 5000);
    }
}
