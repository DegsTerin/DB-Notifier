using DBNotifier.Application.Operations;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Agent.Sqlite;

public sealed class AgentRetentionStore(
    IDbContextFactory<AgentDbContext> contextFactory) : IAgentRetentionStore
{
    public async ValueTask<RetentionExecutionResult> ExecuteAsync(
        RetentionExecutionRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        DateTimeOffset observationCutoff = request.Now.AddDays(-7);
        DateTimeOffset outboxCutoff = request.Now.AddHours(-24);
        DateTimeOffset commandCutoff = request.Now.AddDays(-30);
        AgentHealthObservationRow[] observations = await context.HealthObservations
            .FromSqlInterpolated($$"""
                SELECT * FROM health_observations
                WHERE created_at < {{observationCutoff}}
                ORDER BY created_at
                LIMIT {{request.MaximumRowsPerClass}}
                """)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        AgentOutboxMessageRow[] outboxTombstones = await context.OutboxMessages
            .FromSqlInterpolated($$"""
                SELECT * FROM outbox_messages
                WHERE acknowledged_at IS NOT NULL AND acknowledged_at < {{outboxCutoff}}
                ORDER BY acknowledged_at
                LIMIT {{request.MaximumRowsPerClass}}
                """)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        AgentInboxCommandRow[] commands = await context.InboxCommands
            .FromSqlInterpolated($$"""
                SELECT * FROM inbox_commands
                WHERE completed_at IS NOT NULL AND completed_at < {{commandCutoff}}
                  AND state IN ('Succeeded','Failed','Cancelled','Expired','Rejected','UnknownOutcome')
                ORDER BY completed_at
                LIMIT {{request.MaximumRowsPerClass}}
                """)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        if (request.ApplyChanges)
        {
            context.HealthObservations.RemoveRange(observations);
            context.OutboxMessages.RemoveRange(outboxTombstones);
            context.InboxCommands.RemoveRange(commands);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new RetentionExecutionResult(
            observations.Length,
            outboxTombstones.Length,
            commands.Length,
            0,
            0,
            0);
    }

    private static void Validate(RetentionExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.MaximumRowsPerClass, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.MaximumRowsPerClass, 5000);
    }
}
