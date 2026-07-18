// Module purpose: Validates an existing Agent SQLite sandbox before resilience tests without creating, migrating or repairing it.
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Agent.Sqlite;

/// <summary>
/// Proves that an existing test-only Agent database is readable, internally consistent and at the exact known
/// migration level. It is not registered by the ordinary Worker and never repairs or recreates a refused store.
/// </summary>
/// <param name="contextFactory">Factory configured with read/write-existing SQLite mode for the fixture file.</param>
public sealed class AgentFleetSandboxDatabaseGuard(IDbContextFactory<AgentDbContext> contextFactory)
{
    /// <summary>Validates SQLite integrity and the exact ordered migration history without changing the store.</summary>
    /// <param name="cancellationToken">Cancellation propagated to every database read.</param>
    /// <returns>A task that completes only when the existing store is safe for the sandbox harness.</returns>
    /// <exception cref="InvalidOperationException">The integrity result or migration history is not exact.</exception>
    /// <exception cref="SqliteException">The file is missing, unreadable, corrupt or locked.</exception>
    public async ValueTask ValidateAsync(CancellationToken cancellationToken)
    {
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA quick_check(1);";
            object? integrity = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (!string.Equals(Convert.ToString(integrity, System.Globalization.CultureInfo.InvariantCulture),
                    "ok",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("agent_fleet.sandbox_store_corrupt");
            }

            string[] known = context.Database.GetMigrations().ToArray();
            string[] applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)
                    .ConfigureAwait(false))
                .ToArray();
            if (!known.SequenceEqual(applied, StringComparer.Ordinal))
            {
                throw new InvalidOperationException("agent_fleet.sandbox_schema_incompatible");
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }
}
