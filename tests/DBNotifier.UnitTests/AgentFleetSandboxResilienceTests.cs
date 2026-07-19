// Module purpose: Verifies sandbox-only Agent Fleet retry, cancellation, fencing and deterministic SQLite fault behaviour without operational activation.
using System.Text.Json;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Persistence.Agent.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DBNotifier.UnitTests;

/// <summary>Exercises the local resilience boundary with migrated ephemeral SQLite stores and typed fake transport.</summary>
public sealed class AgentFleetSandboxResilienceTests
{
    /// <summary>Proves bounded transient retry, Retry-After ceiling, exact replay and lease release.</summary>
    [Fact]
    public async Task TransientHeartbeatRetriesUseDeterministicBudgetAndExactReplay()
    {
        DateTimeOffset now = new(2026, 7, 18, 5, 30, 0, TimeSpan.Zero);
        MutableTimeProvider clock = new(now);
        await using ResilienceFixture fixture = await ResilienceFixture.StartAsync(clock);
        RecordingHeartbeatTransport transport = new(clock, transientFailures: 2, firstRetryAfter: TimeSpan.FromMilliseconds(200));
        AgentFleetClientCoordinator oneShot = new(
            new UnusedIdentityStore(),
            transport,
            fixture.Store,
            clock,
            TimeSpan.FromMinutes(5));
        AdvancingDelay delay = new(clock);
        AgentFleetSandboxResilienceCoordinator resilience = new(
            oneShot,
            fixture.Store,
            clock,
            delay,
            new FixedJitter(0.5),
            new AgentFleetSandboxRetryPolicy(
                3,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                0));

        AgentFleetSandboxResilienceResult outcome = await resilience.SendHeartbeatAsync(
            "sandbox:retry-owner",
            "1.0.0-sandbox",
            CancellationToken.None);

        Assert.True(outcome.Result.Succeeded);
        Assert.Equal(3, outcome.Attempts);
        Assert.Equal(new[] { TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(2) }, outcome.ScheduledDelays);
        Assert.Equal(outcome.ScheduledDelays, delay.Delays);
        Assert.Equal(3, transport.Requests.Count);
        Assert.Single(transport.Requests.Select(request => request.MessageId).Distinct());
        Assert.Single(transport.Requests.Select(request => request.Sequence).Distinct());
        AgentFleetStateRow state = await fixture.ReadFleetStateAsync();
        Assert.Equal(2, state.NextHeartbeatSequence);
        Assert.Null(state.PendingHeartbeatPayloadJson);
        Assert.Null(state.OperationLeaseOwner);
        Assert.Equal(2, state.NextOperationFence);
    }

    /// <summary>Proves cancellation interrupts a scheduled retry and the exact lease is still released.</summary>
    [Fact]
    public async Task CancellationDuringBackoffReleasesTheFence()
    {
        DateTimeOffset now = new(2026, 7, 18, 5, 40, 0, TimeSpan.Zero);
        MutableTimeProvider clock = new(now);
        await using ResilienceFixture fixture = await ResilienceFixture.StartAsync(clock);
        RecordingHeartbeatTransport transport = new(clock, transientFailures: int.MaxValue, firstRetryAfter: null);
        AgentFleetClientCoordinator oneShot = new(
            new UnusedIdentityStore(),
            transport,
            fixture.Store,
            clock,
            TimeSpan.FromMinutes(5));
        using CancellationTokenSource cancellation = new();
        AgentFleetSandboxResilienceCoordinator resilience = new(
            oneShot,
            fixture.Store,
            clock,
            new CancellingDelay(cancellation),
            new FixedJitter(0.5),
            new AgentFleetSandboxRetryPolicy(
                3,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(10),
                0));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resilience
            .SendHeartbeatAsync("sandbox:cancel-owner", "1.0.0-sandbox", cancellation.Token)
            .AsTask());

        AgentFleetOperationLease? replacement = await fixture.Store.TryAcquireOperationLeaseAsync(
            fixture.Registration.AgentId,
            "sandbox:replacement-owner",
            AgentFleetOperationKind.Heartbeat,
            clock.GetUtcNow(),
            TimeSpan.FromSeconds(5),
            CancellationToken.None);
        Assert.NotNull(replacement);
        Assert.True(replacement.FenceToken >= 2);
    }

    /// <summary>Proves one owner wins and an expired owner cannot clear or commit through the newer fence.</summary>
    [Fact]
    public async Task ExpiredOwnerCannotCommitOrReleaseANewerFence()
    {
        DateTimeOffset now = new(2026, 7, 18, 5, 50, 0, TimeSpan.Zero);
        MutableTimeProvider clock = new(now);
        await using ResilienceFixture fixture = await ResilienceFixture.StartAsync(clock);
        Task<AgentFleetOperationLease?> firstTask = fixture.Store.TryAcquireOperationLeaseAsync(
            fixture.Registration.AgentId,
            "sandbox:owner-a",
            AgentFleetOperationKind.Heartbeat,
            now,
            TimeSpan.FromSeconds(2),
            CancellationToken.None).AsTask();
        Task<AgentFleetOperationLease?> secondTask = fixture.Store.TryAcquireOperationLeaseAsync(
            fixture.Registration.AgentId,
            "sandbox:owner-b",
            AgentFleetOperationKind.Heartbeat,
            now,
            TimeSpan.FromSeconds(2),
            CancellationToken.None).AsTask();
        AgentFleetOperationLease?[] competing = await Task.WhenAll(firstTask, secondTask);
        AgentFleetOperationLease first = Assert.Single(competing, lease => lease is not null)!;

        PendingAgentHeartbeat pending = await fixture.Store.GetOrCreatePendingHeartbeatAsync(
            fixture.Registration,
            "1.0.0-sandbox",
            new AgentHeartbeatQueueEvidence(0, null),
            now,
            first,
            CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(3));
        AgentFleetOperationLease second = Assert.IsType<AgentFleetOperationLease>(
            await fixture.Store.TryAcquireOperationLeaseAsync(
                fixture.Registration.AgentId,
                "sandbox:owner-c",
                AgentFleetOperationKind.Heartbeat,
                clock.GetUtcNow(),
                TimeSpan.FromSeconds(5),
                CancellationToken.None));
        Assert.True(second.FenceToken > first.FenceToken);
        AgentHeartbeatOutcome receipt = new(
            AgentHeartbeatDisposition.Duplicate,
            clock.GetUtcNow(),
            pending.Request.Sequence,
            0,
            null);

        InvalidOperationException lost = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Store
            .AcknowledgeHeartbeatAsync(
                pending.Request,
                receipt,
                clock.GetUtcNow(),
                first,
                CancellationToken.None).AsTask());
        Assert.Equal("agent_fleet.lease_lost", lost.Message);
        await fixture.Store.ReleaseOperationLeaseAsync(first, CancellationToken.None);
        AgentFleetStateRow retained = await fixture.ReadFleetStateAsync();
        Assert.Equal(second.OwnerId, retained.OperationLeaseOwner);
        Assert.Equal(second.FenceToken, retained.OperationLeaseFence);

        await fixture.Store.AcknowledgeHeartbeatAsync(
            pending.Request,
            receipt,
            clock.GetUtcNow(),
            second,
            CancellationToken.None);
        Assert.Null((await fixture.ReadFleetStateAsync()).PendingHeartbeatPayloadJson);
    }

    /// <summary>Proves pre-commit failure rolls back and post-commit failure leaves recoverable durable evidence.</summary>
    [Fact]
    public async Task AssignmentFaultInjectionPreservesAtomicLastKnownValidState()
    {
        DateTimeOffset now = new(2026, 7, 18, 6, 0, 0, TimeSpan.Zero);
        MutableTimeProvider clock = new(now);
        SingleFaultInjector faults = new();
        await using ResilienceFixture fixture = await ResilienceFixture.StartAsync(clock, faults);
        AgentFleetOperationLease lease = Assert.IsType<AgentFleetOperationLease>(
            await fixture.Store.TryAcquireOperationLeaseAsync(
                fixture.Registration.AgentId,
                "sandbox:assignment-owner",
                AgentFleetOperationKind.AssignmentReconciliation,
                now,
                TimeSpan.FromMinutes(5),
                CancellationToken.None));
        AgentAssignmentSnapshot first = CreateSnapshot(fixture.Registration.AgentId, now, "first");
        await fixture.Store.ApplyAssignmentsAsync(
            first,
            $"\"{first.Version}\"",
            now,
            lease,
            CancellationToken.None);

        AgentAssignmentSnapshot second = CreateSnapshot(fixture.Registration.AgentId, now.AddSeconds(1), "second");
        faults.FailNext(AgentFleetLocalStoreFaultPoint.BeforeAssignmentCommit);
        await Assert.ThrowsAsync<IOException>(() => fixture.Store.ApplyAssignmentsAsync(
            second,
            $"\"{second.Version}\"",
            now.AddSeconds(1),
            lease,
            CancellationToken.None).AsTask());
        Assert.Equal(first.Version, (await fixture.Store.GetAssignmentStateAsync(
            fixture.Registration.AgentId,
            CancellationToken.None)).Version);
        Assert.Equal("first", await fixture.ReadAssignmentDisplayNameAsync());

        faults.FailNext(AgentFleetLocalStoreFaultPoint.AfterAssignmentCommit);
        await Assert.ThrowsAsync<IOException>(() => fixture.Store.ApplyAssignmentsAsync(
            second,
            $"\"{second.Version}\"",
            now.AddSeconds(2),
            lease,
            CancellationToken.None).AsTask());
        Assert.Equal(second.Version, (await fixture.Store.GetAssignmentStateAsync(
            fixture.Registration.AgentId,
            CancellationToken.None)).Version);
        Assert.Equal("second", await fixture.ReadAssignmentDisplayNameAsync());
    }

    /// <summary>Proves lease failures distinguish no commit from an abandoned, expiring committed fence.</summary>
    [Fact]
    public async Task LeaseFaultInjectionPreservesMonotonicRecovery()
    {
        DateTimeOffset now = new(2026, 7, 18, 6, 5, 0, TimeSpan.Zero);
        MutableTimeProvider clock = new(now);
        SingleFaultInjector faults = new();
        await using ResilienceFixture fixture = await ResilienceFixture.StartAsync(clock, faults);
        faults.FailNext(AgentFleetLocalStoreFaultPoint.BeforeLeaseAcquire);
        await Assert.ThrowsAsync<IOException>(() => fixture.Store.TryAcquireOperationLeaseAsync(
            fixture.Registration.AgentId,
            "sandbox:lease-before",
            AgentFleetOperationKind.Heartbeat,
            now,
            TimeSpan.FromSeconds(5),
            CancellationToken.None).AsTask());
        AgentFleetStateRow before = await fixture.ReadFleetStateAsync();
        Assert.Equal(1, before.NextOperationFence);
        Assert.Null(before.OperationLeaseOwner);

        faults.FailNext(AgentFleetLocalStoreFaultPoint.AfterLeaseAcquire);
        await Assert.ThrowsAsync<IOException>(() => fixture.Store.TryAcquireOperationLeaseAsync(
            fixture.Registration.AgentId,
            "sandbox:lease-after",
            AgentFleetOperationKind.Heartbeat,
            now,
            TimeSpan.FromSeconds(5),
            CancellationToken.None).AsTask());
        AgentFleetStateRow committed = await fixture.ReadFleetStateAsync();
        Assert.Equal(2, committed.NextOperationFence);
        Assert.Equal("sandbox:lease-after", committed.OperationLeaseOwner);
        clock.Advance(TimeSpan.FromSeconds(6));
        AgentFleetOperationLease recovered = Assert.IsType<AgentFleetOperationLease>(
            await fixture.Store.TryAcquireOperationLeaseAsync(
                fixture.Registration.AgentId,
                "sandbox:lease-recovery",
                AgentFleetOperationKind.Heartbeat,
                clock.GetUtcNow(),
                TimeSpan.FromSeconds(5),
                CancellationToken.None));
        Assert.Equal(2, recovered.FenceToken);
    }

    /// <summary>Proves heartbeat pre-commit faults roll back and post-commit faults preserve replay evidence.</summary>
    [Fact]
    public async Task HeartbeatFaultInjectionPreservesExactCommitEvidence()
    {
        DateTimeOffset now = new(2026, 7, 18, 6, 7, 0, TimeSpan.Zero);
        MutableTimeProvider clock = new(now);
        SingleFaultInjector faults = new();
        await using ResilienceFixture fixture = await ResilienceFixture.StartAsync(clock, faults);
        AgentFleetOperationLease lease = Assert.IsType<AgentFleetOperationLease>(
            await fixture.Store.TryAcquireOperationLeaseAsync(
                fixture.Registration.AgentId,
                "sandbox:heartbeat-faults",
                AgentFleetOperationKind.Heartbeat,
                now,
                TimeSpan.FromMinutes(1),
                CancellationToken.None));
        AgentHeartbeatQueueEvidence queue = new(0, null);
        faults.FailNext(AgentFleetLocalStoreFaultPoint.BeforeHeartbeatPendingCommit);
        await Assert.ThrowsAsync<IOException>(() => fixture.Store.GetOrCreatePendingHeartbeatAsync(
            fixture.Registration,
            "1.0.0-sandbox",
            queue,
            now,
            lease,
            CancellationToken.None).AsTask());
        Assert.Null((await fixture.ReadFleetStateAsync()).PendingHeartbeatPayloadJson);

        faults.FailNext(AgentFleetLocalStoreFaultPoint.AfterHeartbeatPendingCommit);
        await Assert.ThrowsAsync<IOException>(() => fixture.Store.GetOrCreatePendingHeartbeatAsync(
            fixture.Registration,
            "1.0.0-sandbox",
            queue,
            now,
            lease,
            CancellationToken.None).AsTask());
        PendingAgentHeartbeat pending = await fixture.Store.GetOrCreatePendingHeartbeatAsync(
            fixture.Registration,
            "1.0.0-sandbox",
            queue,
            now.AddSeconds(1),
            lease,
            CancellationToken.None);
        AgentHeartbeatOutcome receipt = new(
            AgentHeartbeatDisposition.Accepted,
            now.AddSeconds(1),
            pending.Request.Sequence,
            0,
            null);
        faults.FailNext(AgentFleetLocalStoreFaultPoint.BeforeHeartbeatAcknowledgementCommit);
        await Assert.ThrowsAsync<IOException>(() => fixture.Store.AcknowledgeHeartbeatAsync(
            pending.Request,
            receipt,
            now.AddSeconds(1),
            lease,
            CancellationToken.None).AsTask());
        Assert.NotNull((await fixture.ReadFleetStateAsync()).PendingHeartbeatPayloadJson);

        faults.FailNext(AgentFleetLocalStoreFaultPoint.AfterHeartbeatAcknowledgementCommit);
        await Assert.ThrowsAsync<IOException>(() => fixture.Store.AcknowledgeHeartbeatAsync(
            pending.Request,
            receipt,
            now.AddSeconds(2),
            lease,
            CancellationToken.None).AsTask());
        AgentFleetStateRow acknowledged = await fixture.ReadFleetStateAsync();
        Assert.Null(acknowledged.PendingHeartbeatPayloadJson);
        Assert.Equal(2, acknowledged.NextHeartbeatSequence);
    }

    /// <summary>Proves terminal and malformed protocol results never enter the retry schedule.</summary>
    /// <param name="disposition">Typed transport classification returned on the first call.</param>
    /// <param name="expectedState">Fail-closed local identity classification.</param>
    [Theory]
    [InlineData(AgentFleetTransportDisposition.Denied, AgentLocalIdentityState.RevokedOrDenied)]
    [InlineData(AgentFleetTransportDisposition.Incompatible, AgentLocalIdentityState.Incompatible)]
    [InlineData(AgentFleetTransportDisposition.InvalidResponse, AgentLocalIdentityState.Conflict)]
    public async Task TerminalProtocolResultsNeverRetry(
        AgentFleetTransportDisposition disposition,
        AgentLocalIdentityState expectedState)
    {
        DateTimeOffset now = new(2026, 7, 18, 6, 10, 0, TimeSpan.Zero);
        MutableTimeProvider clock = new(now);
        await using ResilienceFixture fixture = await ResilienceFixture.StartAsync(clock);
        FixedHeartbeatTransport transport = new(disposition);
        AdvancingDelay delay = new(clock);
        AgentFleetSandboxResilienceCoordinator resilience = CreateResilience(
            fixture,
            clock,
            transport,
            delay,
            maximumAttempts: 4);

        AgentFleetSandboxResilienceResult outcome = await resilience.SendHeartbeatAsync(
            $"sandbox:terminal-{disposition}",
            "1.0.0-sandbox",
            CancellationToken.None);

        Assert.False(outcome.Result.Succeeded);
        Assert.False(outcome.Result.Retryable);
        Assert.Equal(expectedState, outcome.Result.State);
        Assert.Equal(1, outcome.Attempts);
        Assert.Empty(outcome.ScheduledDelays);
        Assert.Empty(delay.Delays);
        Assert.Equal(1, transport.Calls);
        Assert.Equal(expectedState, (await fixture.Store.GetRegistrationAsync(CancellationToken.None))?.State);
    }

    /// <summary>Proves an expired local certificate is quarantined before transport and remains non-retryable.</summary>
    [Fact]
    public async Task ExpiredIdentityStopsBeforeTransportWithoutRetry()
    {
        DateTimeOffset enrolledAt = new(2026, 7, 18, 6, 20, 0, TimeSpan.Zero);
        MutableTimeProvider clock = new(enrolledAt);
        await using ResilienceFixture fixture = await ResilienceFixture.StartAsync(clock);
        clock.Advance(TimeSpan.FromHours(2));
        FixedHeartbeatTransport transport = new(AgentFleetTransportDisposition.TransientFailure);
        AdvancingDelay delay = new(clock);
        AgentFleetSandboxResilienceCoordinator resilience = CreateResilience(
            fixture,
            clock,
            transport,
            delay,
            maximumAttempts: 4);

        AgentFleetSandboxResilienceResult outcome = await resilience.SendHeartbeatAsync(
            "sandbox:expired-owner",
            "1.0.0-sandbox",
            CancellationToken.None);

        Assert.False(outcome.Result.Succeeded);
        Assert.Equal(AgentLocalIdentityState.Expired, outcome.Result.State);
        Assert.Equal("agent.certificate_expired", outcome.Result.Code);
        Assert.Equal(1, outcome.Attempts);
        Assert.Equal(0, transport.Calls);
        Assert.Empty(delay.Delays);
    }

    /// <summary>Proves exhaustion converts the last transient response into a terminal bounded result.</summary>
    [Fact]
    public async Task AttemptBudgetExhaustionCannotEscapeAsRetryable()
    {
        DateTimeOffset now = new(2026, 7, 18, 6, 30, 0, TimeSpan.Zero);
        MutableTimeProvider clock = new(now);
        await using ResilienceFixture fixture = await ResilienceFixture.StartAsync(clock);
        FixedHeartbeatTransport transport = new(AgentFleetTransportDisposition.TransientFailure);
        AdvancingDelay delay = new(clock);
        AgentFleetSandboxResilienceCoordinator resilience = CreateResilience(
            fixture,
            clock,
            transport,
            delay,
            maximumAttempts: 2);

        AgentFleetSandboxResilienceResult outcome = await resilience.SendHeartbeatAsync(
            "sandbox:budget-owner",
            "1.0.0-sandbox",
            CancellationToken.None);

        Assert.False(outcome.Result.Succeeded);
        Assert.False(outcome.Result.Retryable);
        Assert.Equal("agent_fleet.retry_budget_exhausted", outcome.Result.Code);
        Assert.Equal(2, outcome.Attempts);
        Assert.Single(delay.Delays);
    }

    /// <summary>Proves a real SQLite write lock refuses lease acquisition and does not consume a fence.</summary>
    [Fact]
    public async Task SqliteBusyLockRefusesAcquisitionWithoutAdvancingFence()
    {
        DateTimeOffset now = new(2026, 7, 18, 6, 40, 0, TimeSpan.Zero);
        MutableTimeProvider clock = new(now);
        await using ResilienceFixture fixture = await ResilienceFixture.StartAsync(clock);
        await using AgentDbContext blocker = fixture.CreateContext();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlRawAsync(
            "UPDATE agent_fleet_state SET last_error_code = 'sandbox.locked' WHERE agent_id = {0}",
            fixture.Registration.AgentId);

        AgentFleetOperationLease? refused = await fixture.Store.TryAcquireOperationLeaseAsync(
            fixture.Registration.AgentId,
            "sandbox:locked-owner",
            AgentFleetOperationKind.Heartbeat,
            now,
            TimeSpan.FromSeconds(5),
            CancellationToken.None);

        Assert.Null(refused);
        await transaction.RollbackAsync();
        AgentFleetOperationLease acquired = Assert.IsType<AgentFleetOperationLease>(
            await fixture.Store.TryAcquireOperationLeaseAsync(
                fixture.Registration.AgentId,
                "sandbox:after-lock-owner",
                AgentFleetOperationKind.Heartbeat,
                now,
                TimeSpan.FromSeconds(5),
                CancellationToken.None));
        Assert.Equal(1, acquired.FenceToken);
    }

    /// <summary>Proves the sandbox guard refuses corrupt, future and incomplete stores without recreating them.</summary>
    [Fact]
    public async Task SandboxDatabaseGuardFailsClosedWithoutRepairOrRecreation()
    {
        string root = Path.Combine(Path.GetTempPath(), $"dbnotifier-agent-fleet-guard-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string corruptPath = Path.Combine(root, "corrupt.db");
        byte[] corruptBytes = "not-a-sqlite-database"u8.ToArray();
        await File.WriteAllBytesAsync(corruptPath, corruptBytes);
        try
        {
            ContextFactory corruptFactory = new(CreateFileOptions(corruptPath, SqliteOpenMode.ReadWrite));
            await Assert.ThrowsAsync<SqliteException>(() => new AgentFleetSandboxDatabaseGuard(corruptFactory)
                .ValidateAsync(CancellationToken.None).AsTask());
            Assert.Equal(corruptBytes, await File.ReadAllBytesAsync(corruptPath));

            string schemaPath = Path.Combine(root, "schema.db");
            DbContextOptions<AgentDbContext> createOptions = CreateFileOptions(
                schemaPath,
                SqliteOpenMode.ReadWriteCreate);
            await using (AgentDbContext context = new(createOptions))
            {
                await context.Database.MigrateAsync();
            }

            ContextFactory schemaFactory = new(CreateFileOptions(schemaPath, SqliteOpenMode.ReadWrite));
            await new AgentFleetSandboxDatabaseGuard(schemaFactory).ValidateAsync(CancellationToken.None);
            await using (AgentDbContext context = new(CreateFileOptions(schemaPath, SqliteOpenMode.ReadWrite)))
            {
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({0}, {1})",
                    "99999999999999_FutureSandboxSchema",
                    "10.0.9");
            }

            InvalidOperationException future = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new AgentFleetSandboxDatabaseGuard(schemaFactory).ValidateAsync(CancellationToken.None).AsTask());
            Assert.Equal("agent_fleet.sandbox_schema_incompatible", future.Message);
            await using (AgentDbContext context = new(CreateFileOptions(schemaPath, SqliteOpenMode.ReadWrite)))
            {
                await context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM __EFMigrationsHistory WHERE MigrationId = {0}",
                    "99999999999999_FutureSandboxSchema");
                string last = context.Database.GetMigrations().Last();
                await context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM __EFMigrationsHistory WHERE MigrationId = {0}",
                    last);
            }

            InvalidOperationException incomplete = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new AgentFleetSandboxDatabaseGuard(schemaFactory).ValidateAsync(CancellationToken.None).AsTask());
            Assert.Equal("agent_fleet.sandbox_schema_incompatible", incomplete.Message);
            Assert.True(File.Exists(schemaPath));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    /// <summary>Proves migration rollback refuses an active lease, then succeeds after exact release and reapplies.</summary>
    [Fact]
    public async Task ResilienceMigrationRoundTripGuardsAnActiveLease()
    {
        DateTimeOffset now = new(2026, 7, 18, 6, 50, 0, TimeSpan.Zero);
        MutableTimeProvider clock = new(now);
        await using ResilienceFixture fixture = await ResilienceFixture.StartAsync(clock);
        AgentFleetOperationLease lease = Assert.IsType<AgentFleetOperationLease>(
            await fixture.Store.TryAcquireOperationLeaseAsync(
                fixture.Registration.AgentId,
                "sandbox:migration-owner",
                AgentFleetOperationKind.Heartbeat,
                now,
                TimeSpan.FromMinutes(1),
                CancellationToken.None));
        await using AgentDbContext context = fixture.CreateContext();
        string[] migrations = context.Database.GetMigrations().ToArray();
        Assert.True(migrations.Length >= 2);
        IMigrator migrator = context.Database.GetService<IMigrator>();

        int resilienceIndex = Array.FindIndex(
            migrations,
            migration => migration.EndsWith("HardenAgentFleetSandboxResilience", StringComparison.Ordinal));
        Assert.True(resilienceIndex > 0);
        string beforeResilience = migrations[resilienceIndex - 1];
        await Assert.ThrowsAsync<SqliteException>(() => migrator
            .MigrateAsync(beforeResilience, CancellationToken.None));
        await fixture.Store.ReleaseOperationLeaseAsync(lease, CancellationToken.None);
        await migrator.MigrateAsync(beforeResilience, CancellationToken.None);
        Assert.Equal(beforeResilience, Assert.Single(await context.Database.GetAppliedMigrationsAsync(),
            migration => migration == beforeResilience));
        await migrator.MigrateAsync(migrations[^1], CancellationToken.None);
        Assert.Equal(migrations, (await context.Database.GetAppliedMigrationsAsync()).ToArray());
    }

    private static AgentFleetSandboxResilienceCoordinator CreateResilience(
        ResilienceFixture fixture,
        MutableTimeProvider clock,
        IAgentFleetClientTransport transport,
        IAgentFleetSandboxDelay delay,
        int maximumAttempts)
    {
        AgentFleetClientCoordinator oneShot = new(
            new UnusedIdentityStore(),
            transport,
            fixture.Store,
            clock,
            TimeSpan.FromMinutes(5));
        return new AgentFleetSandboxResilienceCoordinator(
            oneShot,
            fixture.Store,
            clock,
            delay,
            new FixedJitter(0.5),
            new AgentFleetSandboxRetryPolicy(
                maximumAttempts,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                0));
    }

    private static DbContextOptions<AgentDbContext> CreateFileOptions(string path, SqliteOpenMode mode)
    {
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            ForeignKeys = true,
            DefaultTimeout = 1,
            Pooling = false,
        }.ToString();
        return new DbContextOptionsBuilder<AgentDbContext>().UseSqlite(connectionString).Options;
    }

    private static AgentAssignmentSnapshot CreateSnapshot(Guid agentId, DateTimeOffset now, string displayName)
    {
        using JsonDocument endpoint = JsonDocument.Parse("{\"host\":\"sandbox.invalid\",\"port\":5432}");
        using JsonDocument tags = JsonDocument.Parse("[\"sandbox\",\"resilience\"]");
        AgentReadOnlyAssignment assignment = new(
            Guid.NewGuid(),
            displayName,
            "postgresql",
            "sandbox",
            endpoint.RootElement.Clone(),
            "credential-ref:sandbox-monitor",
            tags.RootElement.Clone(),
            30,
            5,
            1,
            now);
        string version = AgentAssignmentVersion.Compute(agentId, [assignment]);
        return new AgentAssignmentSnapshot(
            AgentFleetProtocol.CurrentSchemaVersion,
            agentId,
            version,
            now,
            [assignment]);
    }

    private sealed class ResilienceFixture : IAsyncDisposable
    {
        private readonly SqliteConnection keeper;
        private readonly DbContextOptions<AgentDbContext> options;

        private ResilienceFixture(
            SqliteConnection keeper,
            DbContextOptions<AgentDbContext> options,
            AgentFleetLocalStore store,
            AgentLocalRegistration registration)
        {
            this.keeper = keeper;
            this.options = options;
            Store = store;
            Registration = registration;
        }

        /// <summary>Gets the real SQLite local store under test.</summary>
        public AgentFleetLocalStore Store { get; }

        /// <summary>Gets the sole test registration.</summary>
        public AgentLocalRegistration Registration { get; }

        /// <summary>Creates one migrated named in-memory store with optional deterministic faults.</summary>
        /// <param name="clock">Controlled UTC clock.</param>
        /// <param name="faultInjector">Optional test-only fault injector.</param>
        /// <returns>Initialised disposable fixture.</returns>
        public static async Task<ResilienceFixture> StartAsync(
            TimeProvider clock,
            IAgentFleetLocalStoreFaultInjector? faultInjector = null)
        {
            DateTimeOffset now = clock.GetUtcNow();
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = $"DBNotifierResilience{Guid.NewGuid():N}",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true,
                DefaultTimeout = 1,
            }.ToString();
            SqliteConnection keeper = new(connectionString);
            await keeper.OpenAsync();
            DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
                .UseSqlite(connectionString)
                .Options;
            ContextFactory factory = new(options);
            await using (AgentDbContext context = new(options))
            {
                await context.Database.MigrateAsync();
            }

            AgentFleetLocalStore store = new(factory, faultInjector);
            AgentLocalRegistration registration = new(
                Guid.NewGuid(),
                $"installation:{Guid.NewGuid():N}",
                "sandbox",
                $"sandbox-identity:{Guid.NewGuid():N}",
                new string('A', 64),
                now.AddHours(1),
                now,
                AgentLocalIdentityState.Active,
                null);
            await store.SaveEnrollmentAsync(registration, CancellationToken.None);
            return new ResilienceFixture(keeper, options, store, registration);
        }

        /// <summary>Reads exact durable fleet state for fencing and replay assertions.</summary>
        /// <returns>Untracked durable state row.</returns>
        public async Task<AgentFleetStateRow> ReadFleetStateAsync()
        {
            await using AgentDbContext context = new(options);
            return await context.AgentFleetStates.AsNoTracking().SingleAsync();
        }

        /// <summary>Reads the sole assignment display name after an atomic replacement.</summary>
        /// <returns>Persisted display name.</returns>
        public async Task<string> ReadAssignmentDisplayNameAsync()
        {
            await using AgentDbContext context = new(options);
            return await context.InstanceAssignments.AsNoTracking().Select(row => row.DisplayName).SingleAsync();
        }

        /// <summary>Creates one context sharing the migrated fixture database.</summary>
        /// <returns>Fresh context for coordinated lock and migration tests.</returns>
        public AgentDbContext CreateContext() => new(options);

        /// <inheritdoc />
        public ValueTask DisposeAsync() => keeper.DisposeAsync();
    }

    private sealed class RecordingHeartbeatTransport(
        TimeProvider clock,
        int transientFailures,
        TimeSpan? firstRetryAfter) : IAgentFleetClientTransport
    {
        private int remainingFailures = transientFailures;

        /// <summary>Gets exact heartbeat requests sent through the fake boundary.</summary>
        public List<AgentHeartbeatRequest> Requests { get; } = [];

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentEnrollmentOutcome>> EnrolAsync(
            string token,
            AgentEnrollmentRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentHeartbeatOutcome>> SendHeartbeatAsync(
            string identityReference,
            AgentHeartbeatRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            if (remainingFailures-- > 0)
            {
                TimeSpan? retryAfter = Requests.Count == 1 ? firstRetryAfter : null;
                return ValueTask.FromResult(new AgentFleetTransportResult<AgentHeartbeatOutcome>(
                    AgentFleetTransportDisposition.TransientFailure,
                    null,
                    null,
                    "transport.sandbox_unavailable",
                    retryAfter));
            }

            return ValueTask.FromResult(new AgentFleetTransportResult<AgentHeartbeatOutcome>(
                AgentFleetTransportDisposition.Succeeded,
                new AgentHeartbeatOutcome(
                    AgentHeartbeatDisposition.Accepted,
                    clock.GetUtcNow(),
                    request.Sequence,
                    0,
                    null),
                null,
                null));
        }

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentAssignmentSnapshot>> GetAssignmentsAsync(
            string identityReference,
            Guid agentId,
            string agentVersion,
            string? currentVersion,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FixedHeartbeatTransport(AgentFleetTransportDisposition disposition)
        : IAgentFleetClientTransport
    {
        /// <summary>Gets the number of heartbeat calls observed at the transport boundary.</summary>
        public int Calls { get; private set; }

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentEnrollmentOutcome>> EnrolAsync(
            string token,
            AgentEnrollmentRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentHeartbeatOutcome>> SendHeartbeatAsync(
            string identityReference,
            AgentHeartbeatRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(new AgentFleetTransportResult<AgentHeartbeatOutcome>(
                disposition,
                null,
                null,
                $"protocol.sandbox_{disposition.ToString().ToLowerInvariant()}"));
        }

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentAssignmentSnapshot>> GetAssignmentsAsync(
            string identityReference,
            Guid agentId,
            string agentVersion,
            string? currentVersion,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class UnusedIdentityStore : IAgentEnrollmentIdentityStore
    {
        /// <inheritdoc />
        public ValueTask<AgentEnrollmentKeyMaterial> BeginAsync(
            string installationId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <inheritdoc />
        public ValueTask<AgentIdentityCompletion> CompleteAsync(
            Guid operationId,
            ReadOnlyMemory<byte> certificateDer,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <inheritdoc />
        public ValueTask AbortAsync(Guid operationId, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        /// <inheritdoc />
        public ValueTask RemoveAsync(string identityReference, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;

        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => current;

        /// <summary>Advances the controlled clock by one non-negative duration.</summary>
        /// <param name="duration">Amount to add.</param>
        public void Advance(TimeSpan duration)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
            current += duration;
        }
    }

    private sealed class AdvancingDelay(MutableTimeProvider clock) : IAgentFleetSandboxDelay
    {
        /// <summary>Gets exact delays requested by the resilience coordinator.</summary>
        public List<TimeSpan> Delays { get; } = [];

        /// <inheritdoc />
        public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Delays.Add(delay);
            clock.Advance(delay);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CancellingDelay(CancellationTokenSource cancellation) : IAgentFleetSandboxDelay
    {
        /// <inheritdoc />
        public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            cancellation.Cancel();
            return new ValueTask(Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
        }
    }

    private sealed class FixedJitter(double sample) : IAgentFleetSandboxJitter
    {
        /// <inheritdoc />
        public double NextUnitInterval() => sample;
    }

    private sealed class SingleFaultInjector : IAgentFleetLocalStoreFaultInjector
    {
        private AgentFleetLocalStoreFaultPoint? next;

        /// <summary>Configures the next matching boundary to simulate an unavailable local device.</summary>
        /// <param name="point">Exact boundary that will fail once.</param>
        public void FailNext(AgentFleetLocalStoreFaultPoint point) => next = point;

        /// <inheritdoc />
        public ValueTask InjectAsync(AgentFleetLocalStoreFaultPoint point, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (next == point)
            {
                next = null;
                throw new IOException("agent_fleet.sandbox_disk_full_simulated");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class ContextFactory(DbContextOptions<AgentDbContext> options)
        : IDbContextFactory<AgentDbContext>
    {
        /// <inheritdoc />
        public AgentDbContext CreateDbContext() => new(options);

        /// <inheritdoc />
        public Task<AgentDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentDbContext(options));
    }
}
