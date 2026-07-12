using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Agent.Sqlite;

public sealed class AgentStoreInitializer(
    IDbContextFactory<AgentDbContext> contextFactory,
    string databasePath)
{
    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        if (!Path.IsPathFullyQualified(databasePath))
        {
            throw new InvalidOperationException("Agent database path must be fully qualified.");
        }

        string? directory = Path.GetDirectoryName(databasePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Agent database directory is invalid.");
        }

        Directory.CreateDirectory(directory);
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
