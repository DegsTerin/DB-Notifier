using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DBNotifier.UnitTests;

public sealed class PersistenceModelTests
{
    [Fact]
    public async Task AgentMigrationCreatesConstraintsAndRollsBack()
    {
        SqliteConnectionStringBuilder connectionString = new() { DataSource = ":memory:" };
        await using SqliteConnection connection = new(connectionString.ToString());
        await connection.OpenAsync();

        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connection)
            .Options;

        await using AgentDbContext context = new(options);
        await context.Database.MigrateAsync();

        string[] appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(2, appliedMigrations.Length);

        context.InstanceAssignments.Add(new AgentInstanceAssignmentRow
        {
            InstanceId = Guid.NewGuid(),
            DisplayName = "Invalid interval fixture",
            ProviderType = "postgresql",
            EndpointJson = "{}",
            IntervalSeconds = 0,
            TimeoutSeconds = 5,
            RetryCount = 0,
            Enabled = true,
            PolicyVersion = "test",
            UpdatedAt = DateTimeOffset.UtcNow,
            ConcurrencyToken = Guid.NewGuid(),
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        context.HealthObservations.Add(new AgentHealthObservationRow
        {
            ObservationId = Guid.NewGuid(),
            InstanceId = Guid.NewGuid(),
            ProviderType = "postgresql",
            ProviderVersion = "test",
            Status = "InvalidStatus",
            Method = "fixture",
            EvidenceLevel = "Unknown",
            ObservedAt = DateTimeOffset.UtcNow,
            DurationMilliseconds = 0,
            AttemptCount = 1,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        context.OutboxMessages.AddRange(
            new AgentOutboxMessageRow
            {
                MessageId = Guid.NewGuid(),
                Sequence = 42,
                MessageType = "fixture",
                SchemaVersion = 1,
                PayloadJson = "{}",
                OccurredAt = now,
                CreatedAt = now,
                AvailableAt = now,
            },
            new AgentOutboxMessageRow
            {
                MessageId = Guid.NewGuid(),
                Sequence = 42,
                MessageType = "fixture",
                SchemaVersion = 1,
                PayloadJson = "{}",
                OccurredAt = now,
                CreatedAt = now,
                AvailableAt = now,
            });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        IMigrator migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(appliedMigrations[0]);
        Assert.Single(await context.Database.GetAppliedMigrationsAsync());

        await migrator.MigrateAsync(Migration.InitialDatabase);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'instance_assignments'";
        long tableCount = (long)(await command.ExecuteScalarAsync() ?? -1L);
        Assert.Equal(0L, tableCount);
    }

    [Fact]
    public void ServerMigrationScriptUsesPostgreSqlJsonAndAppendOnlyAudit()
    {
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseNpgsql()
            .Options;

        using ServerDbContext context = new(options);
        IMigrator migrator = context.Database.GetService<IMigrator>();
        string migrationScript = migrator.GenerateScript();

        Assert.Contains("jsonb", migrationScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("audit_entries_append_only", migrationScript, StringComparison.Ordinal);
        Assert.Contains("prevent_audit_entry_mutation", migrationScript, StringComparison.Ordinal);
        Assert.Contains("administrative_commands", migrationScript, StringComparison.Ordinal);
        Assert.Contains("health_samples", migrationScript, StringComparison.Ordinal);

        string[] migrations = context.Database.GetMigrations().ToArray();
        Assert.Equal(2, migrations.Length);
        string rollbackScript = migrator.GenerateScript(migrations[1], migrations[0]);
        Assert.Contains("DROP CONSTRAINT", rollbackScript, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PersistenceModelsContainReferencesButNoSecretValueColumns()
    {
        DbContextOptions<AgentDbContext> agentOptions = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite()
            .Options;
        DbContextOptions<ServerDbContext> serverOptions = new DbContextOptionsBuilder<ServerDbContext>()
            .UseNpgsql()
            .Options;

        using AgentDbContext agentContext = new(agentOptions);
        using ServerDbContext serverContext = new(serverOptions);

        string[] propertyNames = agentContext.Model.GetEntityTypes()
            .Concat(serverContext.Model.GetEntityTypes())
            .SelectMany(entity => entity.GetProperties())
            .Select(property => property.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Contains(propertyNames, name => name.EndsWith("CredentialReference", StringComparison.Ordinal));
        Assert.DoesNotContain(propertyNames, name =>
            name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("SecretValue", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("PrivateKey", StringComparison.OrdinalIgnoreCase));
    }
}
