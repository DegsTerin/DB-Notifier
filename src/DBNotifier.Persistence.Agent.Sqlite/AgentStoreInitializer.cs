// Module purpose: Implements Agent Store Initializer for the Agent-local SQLite boundary without exposing monitored database secrets.
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Agent.Sqlite;

public sealed class AgentStoreInitializer(
    IDbContextFactory<AgentDbContext> contextFactory,
    string databasePath) : IDisposable
{
    private readonly SemaphoreSlim initializationGate = new(1, 1);
    private bool initialized;

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref initialized))
        {
            return;
        }

        await initializationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (initialized)
            {
                return;
            }

            await InitializeCoreAsync(cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref initialized, true);
        }
        finally
        {
            initializationGate.Release();
        }
    }

    private async ValueTask InitializeCoreAsync(CancellationToken cancellationToken)
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

    public void Dispose() => initializationGate.Dispose();
}
