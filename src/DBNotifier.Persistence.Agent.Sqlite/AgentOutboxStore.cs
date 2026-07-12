// Module purpose: Implements Agent Outbox Store for the Agent-local SQLite boundary without exposing monitored database secrets.
using DBNotifier.Application.Synchronization;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Agent.Sqlite;

public sealed class AgentOutboxStore(IDbContextFactory<AgentDbContext> contextFactory) : IAgentOutboxStore
{
    private static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromMinutes(5);

    public async ValueTask<IReadOnlyList<AgentOutboxEnvelope>> GetPendingAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumCount, 100);

        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        AgentOutboxMessageRow[] candidates = await context.OutboxMessages
            .AsNoTracking()
            .Where(row => row.AcknowledgedAt == null)
            .OrderBy(row => row.Sequence)
            .Take(maximumCount)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return candidates
            .TakeWhile(row => row.AvailableAt <= now)
            .Select(row => new AgentOutboxEnvelope(
                row.MessageId,
                row.Sequence,
                row.MessageType,
                row.SchemaVersion,
                row.PayloadJson,
                row.OccurredAt,
                row.AttemptCount))
            .ToArray();
    }

    public async ValueTask ApplyResultsAsync(
        IReadOnlyList<ObservationItemResult> results,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (results.Count == 0)
        {
            return;
        }

        Guid[] ids = results.Select(result => result.MessageId).Distinct().ToArray();
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        AgentOutboxMessageRow[] rows = await context.OutboxMessages
            .Where(row => ids.Contains(row.MessageId) && row.AcknowledgedAt == null)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        Dictionary<Guid, ObservationItemResult> resultById = results
            .GroupBy(result => result.MessageId)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (AgentOutboxMessageRow row in rows)
        {
            ObservationItemResult result = resultById[row.MessageId];
            row.AttemptCount = checked(row.AttemptCount + 1);
            if (result.Disposition is ObservationIngestionDisposition.Accepted or
                ObservationIngestionDisposition.Duplicate or
                ObservationIngestionDisposition.Rejected)
            {
                row.AcknowledgedAt = now;
                continue;
            }

            TimeSpan backoff = TimeSpan.FromSeconds(Math.Min(
                Math.Pow(2, Math.Min(row.AttemptCount, 8)),
                MaximumRetryDelay.TotalSeconds));
            DateTimeOffset calculatedRetry = now.Add(backoff);
            DateTimeOffset maximumRetry = now.Add(MaximumRetryDelay);
            row.AvailableAt = result.RetryAfter is { } requested && requested > calculatedRetry
                ? (requested < maximumRetry ? requested : maximumRetry)
                : calculatedRetry;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
