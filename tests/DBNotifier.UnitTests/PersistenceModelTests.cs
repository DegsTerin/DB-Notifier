// Module purpose: Verifies Persistence Model Tests behaviour and protects the documented project contract.
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
    public async Task ServerRoutingSchemaQuarantinesUnprovenWorkOnEphemeralSqlite()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid channelId = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();
        await using ServerDbContext context = new(options);
        await context.Database.EnsureCreatedAsync();
        context.NotificationChannels.Add(new NotificationChannelRow
        {
            NotificationChannelId = channelId,
            Name = "Fixture channel",
            ChannelType = "fixture",
            NonSecretConfigurationJson = "{}",
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ConcurrencyToken = Guid.NewGuid(),
        });
        context.Events.Add(new EventRecordRow
        {
            EventId = eventId,
            CorrelationId = Guid.NewGuid(),
            EventType = "Fixture",
            Severity = "Info",
            ObservedAt = DateTimeOffset.UtcNow,
            ReceivedAt = DateTimeOffset.UtcNow,
            DetailsJson = "{}",
        });
        await context.SaveChangesAsync();
        context.NotificationDeliveries.Add(new NotificationDeliveryRow
        {
            NotificationDeliveryId = Guid.NewGuid(),
            NotificationChannelId = channelId,
            EventId = eventId,
            State = "Pending",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        context.NotificationDeliveries.Add(new NotificationDeliveryRow
        {
            NotificationDeliveryId = Guid.NewGuid(),
            NotificationChannelId = channelId,
            EventId = eventId,
            State = "Quarantined",
            ErrorCode = "notification.binding_unproven",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await context.SaveChangesAsync();
        Assert.Equal("Quarantined", (await context.NotificationDeliveries.SingleAsync()).State);

        Guid ruleId = Guid.NewGuid();
        Guid boundChannelId = Guid.NewGuid();
        Guid bindingId = Guid.NewGuid();
        context.AlertRules.Add(new AlertRuleRow
        {
            AlertRuleId = ruleId,
            Name = "Fixture rule",
            RuleType = "CanonicalEvent",
            ConfigurationJson = "{\"eventTypes\":[\"Fixture\"]}",
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ConcurrencyToken = Guid.NewGuid(),
        });
        context.NotificationChannels.Add(new NotificationChannelRow
        {
            NotificationChannelId = boundChannelId,
            Name = "Bound fixture channel",
            ChannelType = "fixture",
            NonSecretConfigurationJson = "{}",
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ConcurrencyToken = Guid.NewGuid(),
        });
        context.AlertRuleChannelBindings.Add(new AlertRuleChannelBindingRow
        {
            AlertRuleChannelBindingId = bindingId,
            AlertRuleId = ruleId,
            NotificationChannelId = boundChannelId,
            Environment = "test",
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ConcurrencyToken = Guid.NewGuid(),
        });
        await context.SaveChangesAsync();
        context.NotificationDeliveries.Add(new NotificationDeliveryRow
        {
            NotificationDeliveryId = Guid.NewGuid(),
            NotificationChannelId = channelId,
            EventId = eventId,
            AlertRuleChannelBindingId = bindingId,
            IdempotencyKey = $"notification:{eventId:N}:{bindingId:N}",
            State = "Pending",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

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
        Assert.Equal(6, appliedMigrations.Length);

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
    public async Task AgentFleetMigrationRefusesRollbackUntilLocalIdentityIsRemoved()
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
        Assert.Equal(6, appliedMigrations.Length);

        Guid agentId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        context.Registrations.Add(new AgentRegistrationRow
        {
            AgentId = agentId,
            InstallationId = "migration-guard-fixture",
            Environment = "test",
            IdentityCertificateReference = "e2e:identity-reference",
            CertificateThumbprint = new string('A', 64),
            CertificateNotAfter = now.AddHours(1),
            IdentityState = "Active",
            CreatedAt = now,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid(),
        });
        await context.SaveChangesAsync();

        IMigrator migrator = context.Database.GetService<IMigrator>();
        SqliteException refusal = await Assert.ThrowsAsync<SqliteException>(
            () => migrator.MigrateAsync(appliedMigrations[2]));
        Assert.Equal(19, refusal.SqliteErrorCode);
        Assert.Equal(4, (await context.Database.GetAppliedMigrationsAsync()).Count());

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM agent_fleet_state WHERE agent_id = {agentId}");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM agent_registration WHERE agent_id = {agentId}");
        await migrator.MigrateAsync(appliedMigrations[2]);
        Assert.Equal(3, (await context.Database.GetAppliedMigrationsAsync()).Count());

        await migrator.MigrateAsync();
        Assert.Equal(6, (await context.Database.GetAppliedMigrationsAsync()).Count());
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
        Assert.Contains("agent_observation_cursors", migrationScript, StringComparison.Ordinal);
        Assert.Contains("DEFAULT 1", migrationScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MIN(sequence) <> 1", migrationScript, StringComparison.Ordinal);
        Assert.Contains("COUNT(*) <> MAX(sequence)", migrationScript, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO agent_observation_cursors", migrationScript, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO instance_observation_states", migrationScript, StringComparison.Ordinal);
        Assert.Contains("instance_row.assigned_agent_id = sample.agent_id", migrationScript, StringComparison.Ordinal);
        Assert.Contains("agent_enrollment_tokens", migrationScript, StringComparison.Ordinal);
        Assert.Contains("agent_certificates", migrationScript, StringComparison.Ordinal);
        Assert.Contains("agent_heartbeat_cursors", migrationScript, StringComparison.Ordinal);
        Assert.Contains("legacy.agent_revoked", migrationScript, StringComparison.Ordinal);
        Assert.Contains("duplicate per-Agent sequences", migrationScript, StringComparison.Ordinal);
        Assert.Contains("duplicate canonical thumbprints", migrationScript, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO agent_certificates", migrationScript, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO agent_heartbeat_cursors", migrationScript, StringComparison.Ordinal);
        Assert.Contains("LAG(sequence, 1, 0)", migrationScript, StringComparison.Ordinal);
        Assert.Matches(
            @"ORDER BY\s+sample\.instance_id,\s+sample\.sequence DESC,\s+sample\.received_at DESC",
            migrationScript);

        string[] migrations = context.Database.GetMigrations().ToArray();
        Assert.Equal(7, migrations.Length);
        Assert.Contains("alert_rule_channel_bindings", migrationScript, StringComparison.Ordinal);
        Assert.Contains("notification.binding_unproven", migrationScript, StringComparison.Ordinal);
        Assert.Contains("SET state = 'Quarantined'", migrationScript, StringComparison.Ordinal);
        Assert.Contains("ck_delivery_pending_provenance", migrationScript, StringComparison.Ordinal);
        Assert.Contains("AK_alert_rule_channel_bindings_alert_rule_channel_binding_id", migrationScript, StringComparison.Ordinal);
        string explicitRoutingRollback = migrator.GenerateScript(migrations[6], migrations[5]);
        Assert.Contains("notification.explicit_binding_downgrade_blocked", explicitRoutingRollback, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE notification_deliveries", explicitRoutingRollback, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", explicitRoutingRollback, StringComparison.OrdinalIgnoreCase);
        string commandTransportRollback = migrator.GenerateScript(migrations[5], migrations[4]);
        Assert.Contains("command_transport_journal", commandTransportRollback, StringComparison.Ordinal);
        Assert.Contains("agent_command_transport_cursors", commandTransportRollback, StringComparison.Ordinal);
        Assert.Contains("'Rejected','UnknownOutcome'", commandTransportRollback, StringComparison.Ordinal);
        string agentFleetRollback = migrator.GenerateScript(migrations[4], migrations[3]);
        Assert.Contains("DROP TABLE", agentFleetRollback, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("agent_certificates", agentFleetRollback, StringComparison.Ordinal);
        Assert.Contains("agent_enrollment_tokens", agentFleetRollback, StringComparison.Ordinal);
        Assert.Contains("agent_heartbeat_cursors", agentFleetRollback, StringComparison.Ordinal);
        Assert.Contains(
            "Active Agents enrolled by Agent Fleet would remain trusted after downgrade",
            agentFleetRollback,
            StringComparison.Ordinal);
        Assert.Contains("DROP COLUMN", agentFleetRollback, StringComparison.OrdinalIgnoreCase);

        string reconciliationRollback = migrator.GenerateScript(migrations[3], migrations[2]);
        Assert.Contains("agent_observation_cursors", reconciliationRollback, StringComparison.Ordinal);
        Assert.Contains("DROP COLUMN", reconciliationRollback, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("SecretHash", propertyNames);
        Assert.DoesNotContain(propertyNames, name =>
            name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("SecretValue", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("PrivateKey", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("TokenValue", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("CertificateDer", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("CertificateBody", StringComparison.OrdinalIgnoreCase));
    }
}
