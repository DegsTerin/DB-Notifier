// Module purpose: Applies bounded retention policies to central PostgreSQL persistence without accessing monitored databases.
using DBNotifier.Application.Operations;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Server.PostgreSql;

/// <summary>
/// Owns bounded central retention queries. Raw observations remain authoritative and are retained until an
/// aggregate-before-delete contract is durably implemented.
/// </summary>
/// <param name="contextFactory">Factory for isolated central PostgreSQL persistence contexts.</param>
public sealed class ServerMaintenanceStore(IDbContextFactory<ServerDbContext> contextFactory) : IServerRetentionStore
{
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

    /// <summary>Enforces the central retention request and per-class row bounds.</summary>
    private static void ValidateRetention(RetentionExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.MaximumRowsPerClass, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.MaximumRowsPerClass, 5000);
    }
}
