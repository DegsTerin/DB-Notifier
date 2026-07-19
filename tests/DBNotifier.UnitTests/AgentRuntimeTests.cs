// Module purpose: Verifies Agent Runtime Tests behaviour and protects the documented project contract.
using System.Text.Json;
using DBNotifier.Agent.Worker;
using DBNotifier.Domain;
using DBNotifier.Persistence.Agent.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DBNotifier.UnitTests;

public sealed class AgentRuntimeTests
{
    [Fact]
    public async Task AssignmentSourceReturnsOnlyDueAndValidAssignments()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connection)
            .Options;
        TestContextFactory factory = new(options);
        await using (AgentDbContext context = new(options))
        {
            await context.Database.MigrateAsync();
            Guid dueId = Guid.NewGuid();
            Guid recentId = Guid.NewGuid();
            context.InstanceAssignments.AddRange(
                Assignment(dueId, "{\"host\":\"localhost\",\"port\":5432}", CredentialJson()),
                Assignment(recentId, "{\"host\":\"localhost\",\"port\":5432}", null),
                Assignment(Guid.NewGuid(), "{\"password\":\"blocked\"}", null));
            context.HealthObservations.Add(Observation(recentId, DateTimeOffset.UtcNow));
            await context.SaveChangesAsync();
        }

        AgentMonitoringAssignmentSource source = new(factory, NullLogger<AgentMonitoringAssignmentSource>.Instance);
        IReadOnlyList<DBNotifier.Application.Monitoring.MonitoringAssignment> due =
            await source.GetDueAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        DBNotifier.Application.Monitoring.MonitoringAssignment assignment = Assert.Single(due);
        Assert.Equal("postgresql", assignment.Endpoint.ProviderType.Value);
        Assert.Equal("5432", assignment.Endpoint.Properties["port"]);
        Assert.Equal(CredentialPurpose.Monitoring, assignment.MonitoringCredentialReference?.Purpose);
        Assert.Equal("windows-credential-manager", assignment.MonitoringCredentialReference?.VaultProvider);
    }

    [Fact]
    public async Task AssignmentSourceRotatesDueWorkToPreventDeadlineStarvation()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connection)
            .Options;
        TestContextFactory factory = new(options);
        await using (AgentDbContext context = new(options))
        {
            await context.Database.MigrateAsync();
            context.InstanceAssignments.AddRange(
                Assignment(Guid.Parse("11111111-1111-1111-1111-111111111111"), "{\"host\":\"localhost\",\"port\":5432}", null),
                Assignment(Guid.Parse("22222222-2222-2222-2222-222222222222"), "{\"host\":\"localhost\",\"port\":5432}", null));
            await context.SaveChangesAsync();
        }

        AgentMonitoringAssignmentSource source = new(factory, NullLogger<AgentMonitoringAssignmentSource>.Instance);
        IReadOnlyList<DBNotifier.Application.Monitoring.MonitoringAssignment> first =
            await source.GetDueAsync(DateTimeOffset.UtcNow, CancellationToken.None);
        IReadOnlyList<DBNotifier.Application.Monitoring.MonitoringAssignment> second =
            await source.GetDueAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(2, first.Count);
        Assert.Equal(2, second.Count);
        Assert.Equal(first[0].InstanceId, second[1].InstanceId);
        Assert.Equal(first[1].InstanceId, second[0].InstanceId);
    }

    [Fact]
    public async Task AssignmentSourceRejectsConfiguredWorkAboveBound()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connection)
            .Options;
        TestContextFactory factory = new(options);
        await using (AgentDbContext context = new(options))
        {
            await context.Database.MigrateAsync();
            context.InstanceAssignments.AddRange(
                Assignment(Guid.NewGuid(), "{\"host\":\"localhost\",\"port\":5432}", null),
                Assignment(Guid.NewGuid(), "{\"host\":\"localhost\",\"port\":5432}", null));
            await context.SaveChangesAsync();
        }

        AgentMonitoringAssignmentSource source = new(
            factory,
            NullLogger<AgentMonitoringAssignmentSource>.Instance,
            maximumAssignments: 1);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await source.GetDueAsync(DateTimeOffset.UtcNow, CancellationToken.None));
        Assert.Equal("assignment.maximum_exceeded", exception.Message);
    }

    [Fact]
    public async Task StoreInitializerCreatesSchemaOnlyWhenInvoked()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"db-notifier-{Guid.NewGuid():N}");
        string databasePath = Path.Combine(directory, "agent.db");
        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false }.ToString())
            .Options;
        TestContextFactory factory = new(options);
        AgentStoreInitializer initializer = new(factory, databasePath);

        try
        {
            Assert.False(File.Exists(databasePath));
            await initializer.InitializeAsync(CancellationToken.None);
            Assert.True(File.Exists(databasePath));

            await using AgentDbContext verification = new(options);
            Assert.Equal(6, (await verification.Database.GetAppliedMigrationsAsync()).Count());
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void AgentDatabasePathMustBeAbsoluteWhenConfigured()
    {
        Assert.Throws<InvalidOperationException>(() => AgentWorkerOptions.ResolveDatabasePath("relative/agent.db"));
    }

    private static AgentInstanceAssignmentRow Assignment(
        Guid instanceId,
        string endpointJson,
        string? credentialReference) =>
        new()
        {
            InstanceId = instanceId,
            DisplayName = "Fixture",
            ProviderType = "postgresql",
            EndpointJson = endpointJson,
            MonitoringCredentialReference = credentialReference,
            IntervalSeconds = 60,
            TimeoutSeconds = 5,
            RetryCount = 2,
            Enabled = true,
            PolicyVersion = "test",
            UpdatedAt = DateTimeOffset.UtcNow,
            ConcurrencyToken = Guid.NewGuid(),
        };

    private static AgentHealthObservationRow Observation(Guid instanceId, DateTimeOffset observedAt) =>
        new()
        {
            ObservationId = Guid.NewGuid(),
            InstanceId = instanceId,
            ProviderType = "postgresql",
            ProviderVersion = "test",
            Status = "Healthy",
            Method = "fixture",
            EvidenceLevel = "ProviderReadiness",
            ObservedAt = observedAt,
            DurationMilliseconds = 1,
            AttemptCount = 1,
            CreatedAt = observedAt,
        };

    private static string CredentialJson() => JsonSerializer.Serialize(new
    {
        referenceId = Guid.NewGuid(),
        vaultProvider = "windows-credential-manager",
        locator = "DB-Notifier/test",
    });

    private sealed class TestContextFactory(DbContextOptions<AgentDbContext> options) : IDbContextFactory<AgentDbContext>
    {
        public AgentDbContext CreateDbContext() => new(options);
    }
}
