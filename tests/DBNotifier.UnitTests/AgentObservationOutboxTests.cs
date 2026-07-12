using DBNotifier.Domain;
using DBNotifier.Persistence.Agent.Sqlite;
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

        AgentObservationOutboxSink sink = new(factory, TimeProvider.System);
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

    private sealed class TestContextFactory(DbContextOptions<AgentDbContext> options) : IDbContextFactory<AgentDbContext>
    {
        public AgentDbContext CreateDbContext() => new(options);
    }
}
