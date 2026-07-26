// Module purpose: Serialises Agent identity decisions that must remain valid until their owning central transaction commits.
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Server.PostgreSql;

/// <summary>
/// Provides one in-process gate per Agent and a PostgreSQL row lock so observation ingestion and principal revocation
/// have a single, fail-closed transaction order. The gate supports local SQLite validation; the database lock is the
/// cross-process authority boundary for the central PostgreSQL store.
/// </summary>
internal static class AgentIdentityTransactionFence
{
    private static readonly ConcurrentDictionary<Guid, AgentGateEntry> AgentGates = new();

    /// <summary>Enters the process-local portion of the identity transaction fence for one exact Agent.</summary>
    /// <param name="agentId">Agent whose identity-dependent transaction is about to begin.</param>
    /// <param name="cancellationToken">Cancellation observed while waiting for the bounded owner.</param>
    /// <returns>A lease that releases the exact gate once the owning transaction has completed or failed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="agentId"/> is empty.</exception>
    internal static async ValueTask<IDisposable> EnterAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        AgentGateEntry entry;
        while (true)
        {
            entry = AgentGates.GetOrAdd(agentId, static _ => new AgentGateEntry());
            lock (entry.ReferenceLock)
            {
                if (AgentGates.TryGetValue(agentId, out AgentGateEntry? current) &&
                    ReferenceEquals(entry, current))
                {
                    checked
                    {
                        entry.ReferenceCount++;
                    }
                    break;
                }
            }
        }

        try
        {
            await entry.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new AgentIdentityTransactionLease(agentId, entry);
        }
        catch
        {
            ReleaseReference(agentId, entry);
            throw;
        }
    }

    /// <summary>
    /// Loads the tracked Agent identity and locks its PostgreSQL row against principal revocation until the current
    /// transaction ends. The exact SQLite test provider relies on the process-local lease and its serialisable
    /// transaction; any other provider fails closed.
    /// </summary>
    /// <param name="context">Central persistence context with an active transaction.</param>
    /// <param name="agentId">Exact Agent identity to load and fence.</param>
    /// <param name="cancellationToken">Cancellation propagated from the owning operation.</param>
    /// <returns>The tracked identity row, or <see langword="null"/> when the Agent is unknown.</returns>
    internal static async ValueTask<RegisteredAgentRow?> LockIdentityAsync(
        ServerDbContext context,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);

        if (string.Equals(
                context.Database.ProviderName,
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                StringComparison.Ordinal))
        {
            RegisteredAgentRow[] rows = await context.Agents
                .FromSqlInterpolated($"SELECT * FROM agents WHERE agent_id = {agentId} FOR NO KEY UPDATE")
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
            return rows.SingleOrDefault();
        }

        if (string.Equals(
                context.Database.ProviderName,
                "Microsoft.EntityFrameworkCore.Sqlite",
                StringComparison.Ordinal))
        {
            return await context.Agents
                .SingleOrDefaultAsync(row => row.AgentId == agentId, cancellationToken)
                .ConfigureAwait(false);
        }

        throw new NotSupportedException(
            "The Agent identity transaction fence supports only central PostgreSQL and the local SQLite test provider.");
    }

    /// <summary>Releases one retained gate reference and removes an idle Agent entry without an ABA race.</summary>
    /// <param name="agentId">Agent key that selected the entry.</param>
    /// <param name="entry">Exact retained gate entry.</param>
    private static void ReleaseReference(Guid agentId, AgentGateEntry entry)
    {
        lock (entry.ReferenceLock)
        {
            entry.ReferenceCount--;
            if (entry.ReferenceCount == 0 &&
                AgentGates.TryGetValue(agentId, out AgentGateEntry? current) &&
                ReferenceEquals(entry, current))
            {
                AgentGates.TryRemove(agentId, out _);
            }
        }
    }

    /// <summary>Stores one semaphore and its lock-protected number of owners or waiters.</summary>
    private sealed class AgentGateEntry
    {
        /// <summary>Gets the semaphore that serialises identity-dependent transactions.</summary>
        internal SemaphoreSlim Gate { get; } = new(1, 1);

        /// <summary>Gets the monitor protecting reference retention and dictionary removal.</summary>
        internal object ReferenceLock { get; } = new();

        /// <summary>Gets or sets the owners and waiters retaining this exact entry.</summary>
        internal int ReferenceCount { get; set; }
    }

    /// <summary>Owns one exact process-local Agent identity gate and releases it at most once.</summary>
    /// <param name="agentId">Agent key that selected the gate.</param>
    /// <param name="entry">Retained gate entry whose semaphore has been acquired.</param>
    private sealed class AgentIdentityTransactionLease(Guid agentId, AgentGateEntry entry) : IDisposable
    {
        private AgentGateEntry? ownedEntry = entry;

        /// <summary>Releases the owned Agent gate and its retained dictionary reference; repeated disposal is harmless.</summary>
        public void Dispose()
        {
            AgentGateEntry? released = Interlocked.Exchange(ref ownedEntry, null);
            if (released is null)
            {
                return;
            }

            released.Gate.Release();
            ReleaseReference(agentId, released);
        }
    }
}
