// Module purpose: Verifies Agent Observation Outbox Tests behaviour and protects the documented project contract.
using DBNotifier.Application.Monitoring;
using DBNotifier.Application.Security;
using DBNotifier.Domain;
using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Provider.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.UnitTests;

public sealed class AgentObservationOutboxTests
{
    [Fact]
    public async Task ObservationAndOutboxPersistAtomicallyWithMonotonicSequence()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connection)
            .Options;
        TestContextFactory factory = new(options);
        await using (AgentDbContext migrationContext = new(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        using AgentObservationOutboxSink sink = new(factory, TimeProvider.System);
        HealthObservation first = Observation(Guid.NewGuid());
        HealthObservation second = Observation(Guid.NewGuid());

        await sink.PersistAsync(first, CancellationToken.None);
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await sink.PersistAsync(first, CancellationToken.None));
        await sink.PersistAsync(second, CancellationToken.None);

        await using AgentDbContext verification = new(options);
        AgentHealthObservationRow[] observations = await verification.HealthObservations.ToArrayAsync();
        AgentOutboxMessageRow[] messages = await verification.OutboxMessages
            .OrderBy(row => row.Sequence)
            .ToArrayAsync();
        AgentCheckpointRow checkpoint = await verification.Checkpoints.SingleAsync(row => row.StreamName == "outbox-sequence");

        Assert.Equal(2, observations.Length);
        Assert.Equal([1L, 2L], messages.Select(message => message.Sequence));
        Assert.Equal(2L, checkpoint.Sequence);
        Assert.All(messages, message =>
        {
            Assert.Equal("health.observation.v1", message.MessageType);
            Assert.DoesNotContain("password", message.PayloadJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("credential", message.PayloadJson, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ConcurrentMonitoringCycleSerialisesOutboxSequenceWithoutLosingObservations()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connection)
            .Options;
        TestContextFactory factory = new(options);
        await using (AgentDbContext migrationContext = new(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        ConcurrentProvider provider = new();
        ProbeInstanceHandler handler = new(
            new ProviderRegistry([provider]),
            new UnusedVault(),
            TimeProvider.System);
        ProviderEndpoint endpoint = new(
            provider.ProviderType,
            [new KeyValuePair<string, string>("host", "localhost")]);
        MonitoringAssignment[] assignments =
        [
            new(Guid.NewGuid(), endpoint, null, Policy()),
            new(Guid.NewGuid(), endpoint, null, Policy()),
        ];
        using AgentObservationOutboxSink sink = new(factory, TimeProvider.System);
        MonitoringCycleRunner runner = new(
            Guid.NewGuid(),
            new StaticAssignmentSource(assignments),
            handler,
            sink,
            TimeProvider.System,
            maximumConcurrency: 2,
            cycleDeadline: TimeSpan.FromSeconds(5));

        MonitoringCycleResult result = await runner.RunOnceAsync();

        Assert.Equal(2, result.DueCount);
        Assert.Equal(2, result.PersistedCount);
        Assert.Empty(result.Failures);
        await using AgentDbContext verification = new(options);
        Assert.Equal(2, await verification.HealthObservations.CountAsync());
        long[] sequences = await verification.OutboxMessages
            .OrderBy(row => row.Sequence)
            .Select(row => row.Sequence)
            .ToArrayAsync();
        Assert.Equal([1L, 2L], sequences);
    }

    private static HealthObservation Observation(Guid observationId) =>
        new(
            observationId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ProviderType.Parse("future-db"),
            "test",
            HealthStatus.Degraded,
            "fixture",
            DateTimeOffset.UtcNow,
            TimeSpan.FromMilliseconds(12),
            new ObservationQuality(EvidenceLevel.TransportOnly, 2, ["transport-only-evidence"]),
            new NormalizedError("fixture.safe", ErrorCategory.Network, Retryability.Backoff, "Safe fixture."));

    private static ProbePolicy Policy() =>
        new(TimeSpan.FromSeconds(1), 1, TimeSpan.Zero, TimeSpan.Zero);

    private sealed class ConcurrentProvider : IDatabaseProvider
    {
        private readonly TaskCompletionSource firstProbeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int probeCount;

        public ProviderType ProviderType { get; } = ProviderType.Parse("concurrent-outbox-fixture");

        public string Version => "test";

        public IReadOnlyList<ProviderCapability> Capabilities => [];

        public ProviderValidationResult ValidateEndpoint(ProviderEndpoint endpoint) => ProviderValidationResult.Valid;

        public async ValueTask<ProviderProbeResult> ProbeAsync(
            ProviderProbeRequest request,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref probeCount) == 1)
            {
                firstProbeEntered.TrySetResult();
                await Task.Yield();
            }
            else
            {
                await firstProbeEntered.Task.WaitAsync(cancellationToken);
            }

            return new ProviderProbeResult(
                HealthStatus.Healthy,
                EvidenceLevel.ProviderAuthenticated,
                "concurrent-fixture",
                TimeSpan.Zero,
                null,
                []);
        }
    }

    private sealed class StaticAssignmentSource(IReadOnlyList<MonitoringAssignment> assignments)
        : IMonitoringAssignmentSource
    {
        public ValueTask<IReadOnlyList<MonitoringAssignment>> GetDueAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken) => ValueTask.FromResult(assignments);
    }

    private sealed class UnusedVault : ICredentialVault
    {
        public ValueTask<IProviderCredential> ResolveAsync(
            CredentialReference reference,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Vault should not be called without a credential reference.");
    }

    private sealed class TestContextFactory(DbContextOptions<AgentDbContext> options) : IDbContextFactory<AgentDbContext>
    {
        public AgentDbContext CreateDbContext() => new(options);
    }
}
