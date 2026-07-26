// Module purpose: Validates the R-SEQ rejection-ledger migration only against an explicitly activated disposable PostgreSQL lab.
using DBNotifier.Application.Synchronization;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>
/// Proves cutover backfill, durable replay, guarded rollback, overflow refusal and reapplication without touching an
/// operational or monitored database.
/// </summary>
public sealed class RSeqPostgreSqlMigrationTests
{
    private const string ActivationVariable = "DBNOTIFIER_RSEQ_POSTGRESQL_LAB";
    private const string ActivationValue = "local-test";
    private const string ConnectionVariable = "DBNOTIFIER_RSEQ_POSTGRESQL_CONNECTION";
    private static readonly DateTimeOffset Now = new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Runs the complete migration matrix only when the exact disposable-lab marker is present.</summary>
    /// <returns>A task that completes after forward, guarded rollback and reapplication evidence is verified.</returns>
    [Fact]
    public async Task DisposablePostgreSqlProvesRejectedObservationLedgerMigration()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(ActivationVariable),
                ActivationValue,
                StringComparison.Ordinal))
        {
            return;
        }

        string connectionString = Environment.GetEnvironmentVariable(ConnectionVariable) ??
            throw new InvalidOperationException("rseq.postgresql_connection_missing");
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseNpgsql(connectionString)
            .EnableSensitiveDataLogging(false)
            .Options;
        string[] migrations;
        await using (ServerDbContext context = new(options))
        {
            Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
            migrations = context.Database.GetMigrations().ToArray();
        }

        Assert.Equal(9, migrations.Length);
        string previousMigration = migrations[^2];
        string ledgerMigration = migrations[^1];
        await MigrateAsync(options, previousMigration);

        Guid agentId = Guid.NewGuid();
        await SeedLegacyCursorAsync(connectionString, agentId, highestContiguousSequence: 7);
        await MigrateAsync(options, ledgerMigration);

        await using (ServerDbContext verification = new(options))
        {
            AgentObservationCursorRow cursor = await verification.AgentObservationCursors
                .AsNoTracking()
                .SingleAsync(row => row.AgentId == agentId);
            Assert.Equal(7, cursor.HighestContiguousSequence);
            Assert.Equal(8, cursor.RejectionLedgerStartSequence);
            Assert.Empty(await verification.RejectedObservationSequences.ToArrayAsync());
        }

        Guid messageId = Guid.NewGuid();
        ServerObservationIngestionStore store = new(new ServerContextFactory(options));
        ObservationItemResult first = await store.ConsumeRejectedAsync(
            agentId,
            messageId,
            8,
            "fixture.postgresql_rejection",
            Now,
            CancellationToken.None);
        ObservationItemResult replay = await store.ConsumeRejectedAsync(
            agentId,
            messageId,
            8,
            "fixture.changed_reason",
            Now.AddSeconds(1),
            CancellationToken.None);
        Assert.Equal(ObservationIngestionDisposition.Rejected, first.Disposition);
        Assert.Equal("fixture.postgresql_rejection", first.ErrorCode);
        Assert.Equal("fixture.postgresql_rejection", replay.ErrorCode);

        await using (ServerDbContext verification = new(options))
        {
            RejectedObservationSequenceRow ledger = await verification.RejectedObservationSequences
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(agentId, ledger.AgentId);
            Assert.Equal(8, ledger.Sequence);
            Assert.Equal(messageId, ledger.MessageId);
            Assert.Equal(Now, ledger.ConsumedAt);
            Assert.Equal(8, (await verification.AgentObservationCursors.SingleAsync()).HighestContiguousSequence);
        }

        PostgresException blockedRollback = await Assert.ThrowsAsync<PostgresException>(
            () => MigrateAsync(options, previousMigration));
        Assert.Equal(PostgresErrorCodes.RaiseException, blockedRollback.SqlState);
        Assert.Contains(
            "ingestion.rejection_ledger_downgrade_blocked",
            blockedRollback.MessageText,
            StringComparison.Ordinal);
        await DeleteFixtureLedgerAsync(connectionString);
        await MigrateAsync(options, previousMigration);
        Assert.False(await RelationExistsAsync(connectionString, "rejected_observation_sequences"));
        Assert.False(await ColumnExistsAsync(
            connectionString,
            "agent_observation_cursors",
            "rejection_ledger_start_sequence"));

        await SetCursorAsync(connectionString, agentId, long.MaxValue);
        PostgresException overflow = await Assert.ThrowsAsync<PostgresException>(
            () => MigrateAsync(options, ledgerMigration));
        Assert.Equal(PostgresErrorCodes.RaiseException, overflow.SqlState);
        Assert.Contains(
            "ingestion.rejection_ledger_cutover_overflow",
            overflow.MessageText,
            StringComparison.Ordinal);
        Assert.False(await ColumnExistsAsync(
            connectionString,
            "agent_observation_cursors",
            "rejection_ledger_start_sequence"));

        await SetCursorAsync(connectionString, agentId, 8);
        await MigrateAsync(options, ledgerMigration);
        await using (ServerDbContext finalVerification = new(options))
        {
            AgentObservationCursorRow cursor = await finalVerification.AgentObservationCursors
                .AsNoTracking()
                .SingleAsync(row => row.AgentId == agentId);
            Assert.Equal(8, cursor.HighestContiguousSequence);
            Assert.Equal(9, cursor.RejectionLedgerStartSequence);
            Assert.Equal(9, (await finalVerification.Database.GetAppliedMigrationsAsync()).Count());
            Assert.Empty(await finalVerification.RejectedObservationSequences.ToArrayAsync());
        }
    }

    /// <summary>Applies one exact migration target through a fresh context.</summary>
    /// <param name="options">Disposable PostgreSQL context options.</param>
    /// <param name="targetMigration">Exact compiled migration identifier.</param>
    /// <returns>A task that completes after the migration transaction commits.</returns>
    private static async Task MigrateAsync(
        DbContextOptions<ServerDbContext> options,
        string targetMigration)
    {
        await using ServerDbContext context = new(options);
        await context.Database.GetService<IMigrator>().MigrateAsync(targetMigration);
    }

    /// <summary>Seeds one pre-ledger Agent and cursor using only columns present in the preceding schema.</summary>
    /// <param name="connectionString">Synthetic disposable-lab connection string.</param>
    /// <param name="agentId">Fixture Agent identifier.</param>
    /// <param name="highestContiguousSequence">Legacy high-water mark to backfill.</param>
    /// <returns>A task that completes after both rows commit.</returns>
    private static async Task SeedLegacyCursorAsync(
        string connectionString,
        Guid agentId,
        long highestContiguousSequence)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agents (
                agent_id, installation_id, display_name, environment, platform, agent_version,
                certificate_thumbprint, state, enrolled_at, concurrency_token
            )
            VALUES (
                @agent_id, @installation_id, @display_name, @environment, @platform, @agent_version,
                @certificate_thumbprint, 'Active', @enrolled_at, @concurrency_token
            );
            INSERT INTO agent_observation_cursors (
                agent_id, highest_contiguous_sequence, updated_at, concurrency_token
            )
            VALUES (@agent_id, @highest_contiguous_sequence, @updated_at, @cursor_token);
            """;
        command.Parameters.AddWithValue("agent_id", agentId);
        command.Parameters.AddWithValue("installation_id", $"rseq:{agentId:N}");
        command.Parameters.AddWithValue("display_name", "R-SEQ migration fixture");
        command.Parameters.AddWithValue("environment", "local-test");
        command.Parameters.AddWithValue("platform", "fixture");
        command.Parameters.AddWithValue("agent_version", "1.0.0-test");
        command.Parameters.AddWithValue("certificate_thumbprint", Convert.ToHexString(agentId.ToByteArray()));
        command.Parameters.AddWithValue("enrolled_at", Now);
        command.Parameters.AddWithValue("concurrency_token", Guid.NewGuid());
        command.Parameters.AddWithValue("highest_contiguous_sequence", highestContiguousSequence);
        command.Parameters.AddWithValue("updated_at", Now);
        command.Parameters.AddWithValue("cursor_token", Guid.NewGuid());
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Removes only synthetic ledger rows so the guarded Down path can be exercised explicitly.</summary>
    /// <param name="connectionString">Synthetic disposable-lab connection string.</param>
    /// <returns>A task that completes after the fixture table is empty.</returns>
    private static async Task DeleteFixtureLedgerAsync(string connectionString)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new("DELETE FROM rejected_observation_sequences", connection);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    /// <summary>Sets the synthetic cursor high-water mark while the preceding schema is active.</summary>
    /// <param name="connectionString">Synthetic disposable-lab connection string.</param>
    /// <param name="agentId">Fixture Agent identifier.</param>
    /// <param name="sequence">High-water value used for migration validation.</param>
    /// <returns>A task that completes after the exact row is updated.</returns>
    private static async Task SetCursorAsync(string connectionString, Guid agentId, long sequence)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            "UPDATE agent_observation_cursors SET highest_contiguous_sequence = @sequence WHERE agent_id = @agent_id",
            connection);
        command.Parameters.AddWithValue("sequence", sequence);
        command.Parameters.AddWithValue("agent_id", agentId);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    /// <summary>Checks whether one exact relation exists in the disposable public schema.</summary>
    /// <param name="connectionString">Synthetic disposable-lab connection string.</param>
    /// <param name="relationName">Exact non-secret relation name.</param>
    /// <returns><see langword="true"/> when PostgreSQL resolves the relation.</returns>
    private static async Task<bool> RelationExistsAsync(string connectionString, string relationName)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new("SELECT to_regclass('public.' || @relation_name) IS NOT NULL", connection);
        command.Parameters.AddWithValue("relation_name", relationName);
        return (bool)(await command.ExecuteScalarAsync() ??
            throw new InvalidOperationException("rseq.relation_probe_missing"));
    }

    /// <summary>Checks whether one exact column exists in the disposable public schema.</summary>
    /// <param name="connectionString">Synthetic disposable-lab connection string.</param>
    /// <param name="tableName">Exact non-secret table name.</param>
    /// <param name="columnName">Exact non-secret column name.</param>
    /// <returns><see langword="true"/> when the exact column exists.</returns>
    private static async Task<bool> ColumnExistsAsync(
        string connectionString,
        string tableName,
        string columnName)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = @table_name AND column_name = @column_name
            )
            """,
            connection);
        command.Parameters.AddWithValue("table_name", tableName);
        command.Parameters.AddWithValue("column_name", columnName);
        return (bool)(await command.ExecuteScalarAsync() ??
            throw new InvalidOperationException("rseq.column_probe_missing"));
    }

    /// <summary>Creates independent contexts for the production ingestion store within the disposable lab.</summary>
    /// <param name="options">PostgreSQL options shared by all contexts.</param>
    private sealed class ServerContextFactory(DbContextOptions<ServerDbContext> options)
        : IDbContextFactory<ServerDbContext>
    {
        /// <inheritdoc />
        public ServerDbContext CreateDbContext() => new(options);
    }
}
