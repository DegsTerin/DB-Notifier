// Module purpose: Verifies durable command-transport replay, fencing, terminal-only states, bounded retries and transactional SQLite failure handling without execution.
using DBNotifier.Application.Synchronization;
using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.UnitTests;

/// <summary>Protects the isolated v2 command-transport protocol and its zero-execution invariant.</summary>
public sealed class CommandTransportSafetyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 20, 30, 0, TimeSpan.Zero);
    private const string AgentVersion = "1.0.0-sandbox";
    private const string ProviderId = "fixture-provider";
    private const string ProviderVersion = "1.0.0-fixture";

    /// <summary>Proves exact Server replay, monotonic sequence refusal and terminal-only acknowledgement states.</summary>
    [Fact]
    public async Task ServerJournalReplaysExactlyAndNeverCreatesExecutionAttempts()
    {
        await using ServerFixture fixture = await ServerFixture.StartAsync();
        ServerCommandTransportSandboxStore store = new(fixture.Factory);
        CommandTransportPollRequest poll = Poll(fixture.AgentId, 1);

        CommandTransportPollResponse first = await store.PollAsync(poll, Now, CancellationToken.None);
        CommandTransportPollResponse replay = await store.PollAsync(poll, Now.AddSeconds(1), CancellationToken.None);
        Assert.Equal(CommandTransportCodec.Serialize(first), CommandTransportCodec.Serialize(replay));
        Assert.Equal(first.MessageId, replay.MessageId);
        Assert.Equal(2, first.Commands.Count);
        Assert.All(first.Commands, command => Assert.Equal(CommandExecutionPolicy.Never, command.ExecutionPolicy));

        CommandTransportException conflict = await Assert.ThrowsAsync<CommandTransportException>(async () =>
            await store.PollAsync(poll with { MessageId = Guid.NewGuid() }, Now, CancellationToken.None));
        Assert.Equal(CommandTransportFailureKind.Conflict, conflict.Kind);
        CommandTransportException gap = await Assert.ThrowsAsync<CommandTransportException>(async () =>
            await store.PollAsync(Poll(fixture.AgentId, 3), Now, CancellationToken.None));
        Assert.Equal(CommandTransportFailureKind.Gap, gap.Kind);
        Assert.Equal(2, gap.ExpectedSequence);

        CommandTransportAcknowledgementRequest acknowledgement = new(
            Guid.NewGuid(),
            CommandTransportProtocol.CurrentSchemaVersion,
            fixture.AgentId,
            2,
            Now,
            Now,
            [
                new(first.Commands[0].CommandId, first.Commands[0].IdempotencyKey, Now,
                    CommandTransportDisposition.Accepted),
                new(first.Commands[1].CommandId, first.Commands[1].IdempotencyKey, Now,
                    CommandTransportDisposition.Unsupported, "command.capability_unsupported"),
            ]);
        CommandTransportAcknowledgementResponse accepted = await store.AcknowledgeAsync(
            acknowledgement,
            Now,
            CancellationToken.None);
        CommandTransportAcknowledgementResponse acceptedReplay = await store.AcknowledgeAsync(
            acknowledgement,
            Now.AddSeconds(1),
            CancellationToken.None);
        Assert.Equal(CommandTransportCodec.Serialize(accepted), CommandTransportCodec.Serialize(acceptedReplay));

        await using ServerDbContext verification = fixture.CreateContext();
        string[] states = await verification.AdministrativeCommands
            .OrderBy(row => row.IdempotencyKey)
            .Select(row => row.State)
            .ToArrayAsync();
        Assert.Contains("Acknowledged", states);
        Assert.Contains("Unsupported", states);
        Assert.Contains("Expired", states);
        Assert.Contains("Pending", states);
        Assert.DoesNotContain("Running", states);
        Assert.DoesNotContain("Succeeded", states);
        Assert.Empty(await verification.CommandAttempts.ToArrayAsync());
        Assert.Equal(2, await verification.CommandTransportJournal.CountAsync());
        Assert.Equal(2, (await verification.CommandTransportCursors.SingleAsync()).HighestAcceptedSequence);
    }

    /// <summary>Proves Agent pending identity survives logical restart and inbox outcomes never imply execution.</summary>
    [Fact]
    public async Task AgentOutboxPersistsExactPollAndAcknowledgementAcrossStoreRestart()
    {
        await using AgentFixture fixture = await AgentFixture.StartAsync();
        AgentCommandTransportSandboxStore firstStore = new(fixture.Factory);
        long fence = Assert.IsType<long>(await firstStore.TryAcquireLeaseAsync(
            fixture.AgentId,
            "owner:first",
            Now,
            CommandTransportProtocol.LeaseDuration,
            CancellationToken.None));
        CommandTransportPendingMessage poll = await firstStore.GetOrPreparePollAsync(
            fixture.AgentId,
            fence,
            AgentVersion,
            Versions(),
            3,
            Now,
            CancellationToken.None);
        CommandTransportPendingMessage exactReplay = await firstStore.GetOrPreparePollAsync(
            fixture.AgentId,
            fence,
            AgentVersion,
            Versions(),
            3,
            Now.AddSeconds(1),
            CancellationToken.None);
        Assert.Equal(poll, exactReplay);
        await firstStore.ReleaseLeaseAsync(
            fixture.AgentId,
            "owner:first",
            fence,
            Now,
            CancellationToken.None);

        AgentCommandTransportSandboxStore restartedStore = new(fixture.Factory);
        long restartedFence = Assert.IsType<long>(await restartedStore.TryAcquireLeaseAsync(
            fixture.AgentId,
            "owner:restarted",
            Now.AddSeconds(1),
            CommandTransportProtocol.LeaseDuration,
            CancellationToken.None));
        CommandTransportPendingMessage afterRestart = await restartedStore.GetOrPreparePollAsync(
            fixture.AgentId,
            restartedFence,
            AgentVersion,
            Versions(),
            3,
            Now.AddSeconds(1),
            CancellationToken.None);
        Assert.Equal(poll.MessageId, afterRestart.MessageId);
        Assert.Equal(poll.PayloadSha256, afterRestart.PayloadSha256);

        CommandTransportPollRequest request = CommandTransportCodec.Deserialize<CommandTransportPollRequest>(
            poll.PayloadJson);
        CommandTransportEnvelope duplicate = Envelope("duplicate", Now.AddMinutes(5));
        CommandTransportPollResponse duplicateResponse = new(
            Guid.NewGuid(),
            CommandTransportProtocol.CurrentSchemaVersion,
            fixture.AgentId,
            request.Sequence,
            request.MessageId,
            Now,
            Now,
            [duplicate, duplicate]);
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await restartedStore.AcceptPollResponseAsync(
                fixture.AgentId,
                restartedFence,
                duplicateResponse,
                Now,
                CancellationToken.None));
        CommandTransportPollResponse response = new(
            Guid.NewGuid(),
            CommandTransportProtocol.CurrentSchemaVersion,
            fixture.AgentId,
            request.Sequence,
            request.MessageId,
            Now,
            Now,
            [
                Envelope("received", Now.AddMinutes(5)),
                Envelope("unsupported", Now.AddMinutes(5)),
                Envelope("expired", Now.AddSeconds(-1)),
            ]);
        CommandTransportPollCommit commit = await restartedStore.AcceptPollResponseAsync(
            fixture.AgentId,
            restartedFence,
            response,
            Now,
            CancellationToken.None);
        CommandTransportPendingMessage pendingAcknowledgement = Assert.IsType<CommandTransportPendingMessage>(
            commit.PendingAcknowledgement);
        Assert.Equal(2, pendingAcknowledgement.Sequence);

        CommandTransportAcknowledgementRequest acknowledgement =
            CommandTransportCodec.Deserialize<CommandTransportAcknowledgementRequest>(
                pendingAcknowledgement.PayloadJson);
        CommandTransportAcknowledgementResponse acknowledgementResponse = new(
            Guid.NewGuid(),
            CommandTransportProtocol.CurrentSchemaVersion,
            fixture.AgentId,
            acknowledgement.Sequence,
            acknowledgement.MessageId,
            Now,
            Now,
            acknowledgement.Items.Select(item => new CommandTransportAcknowledgementResult(
                item.CommandId,
                item.Disposition,
                item.ReasonCode)).ToArray());
        Assert.Equal(3, await restartedStore.AcceptAcknowledgementResponseAsync(
            fixture.AgentId,
            restartedFence,
            acknowledgementResponse,
            Now,
            CancellationToken.None));

        await using AgentDbContext verification = fixture.CreateContext();
        string[] states = await verification.InboxCommands.OrderBy(row => row.IdempotencyKey)
            .Select(row => row.State).ToArrayAsync();
        Assert.Equal(["Expired", "Acknowledged", "Unsupported"], states);
        Assert.All(await verification.InboxCommands.ToArrayAsync(), row =>
        {
            Assert.Null(row.CompletedAt);
            Assert.Null(row.ResultJson);
        });
        AgentCommandTransportStateRow transportState = await verification.CommandTransportStates.SingleAsync();
        Assert.Null(transportState.PendingMessageId);
        Assert.Equal(3, transportState.NextSequence);
    }

    /// <summary>Proves a deterministic SQLite fault rolls back inbox and acknowledgement preparation together.</summary>
    [Fact]
    public async Task AgentFaultBeforeAcknowledgementCommitPreservesPendingPollAndEmptyInbox()
    {
        SingleFaultInjector faults = new(AgentCommandTransportFaultPoint.BeforeAcknowledgementPreparationCommit);
        await using AgentFixture fixture = await AgentFixture.StartAsync();
        AgentCommandTransportSandboxStore store = new(fixture.Factory, faults);
        long fence = Assert.IsType<long>(await store.TryAcquireLeaseAsync(
            fixture.AgentId,
            "owner:fault",
            Now,
            CommandTransportProtocol.LeaseDuration,
            CancellationToken.None));
        CommandTransportPendingMessage pending = await store.GetOrPreparePollAsync(
            fixture.AgentId,
            fence,
            AgentVersion,
            Versions(),
            1,
            Now,
            CancellationToken.None);
        CommandTransportPollRequest request = CommandTransportCodec.Deserialize<CommandTransportPollRequest>(
            pending.PayloadJson);
        CommandTransportPollResponse response = new(
            Guid.NewGuid(),
            CommandTransportProtocol.CurrentSchemaVersion,
            fixture.AgentId,
            request.Sequence,
            request.MessageId,
            Now,
            Now,
            [Envelope("received", Now.AddMinutes(5))]);

        await Assert.ThrowsAsync<IOException>(async () => await store.AcceptPollResponseAsync(
            fixture.AgentId,
            fence,
            response,
            Now,
            CancellationToken.None));
        await using AgentDbContext verification = fixture.CreateContext();
        Assert.Empty(await verification.InboxCommands.ToArrayAsync());
        AgentCommandTransportStateRow state = await verification.CommandTransportStates.SingleAsync();
        Assert.Equal(pending.MessageId, state.PendingMessageId);
        Assert.Equal("Poll", state.PendingMessageKind);
    }

    /// <summary>Proves two bounded retries reuse one durable identity and cancellation leaves it pending.</summary>
    [Fact]
    public async Task CoordinatorRetriesExactMessageAndPreservesPendingStateOnCancellation()
    {
        await using AgentFixture fixture = await AgentFixture.StartAsync();
        AgentCommandTransportSandboxStore store = new(fixture.Factory);
        RecordingClient client = new(fixture.AgentId, failuresBeforeSuccess: 2);
        RecordingDelay delay = new();
        CommandTransportSandboxCoordinator coordinator = new(
            fixture.AgentId,
            "owner:retry",
            AgentVersion,
            Versions(),
            store,
            client,
            new FixedTimeProvider(Now),
            delay,
            CommandTransportRetryPolicy.SandboxDefault);

        CommandTransportCycleResult result = await coordinator.RunOnceAsync(1);
        Assert.Equal(3, result.AttemptCount);
        Assert.Equal(3, client.PollRequests.Count);
        Assert.Single(client.PollRequests.Select(item => item.MessageId).Distinct());
        Assert.Equal([TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(250)], delay.Delays);

        using CancellationTokenSource cancellation = new();
        CancellingClient cancellingClient = new(cancellation);
        CommandTransportSandboxCoordinator cancelled = new(
            fixture.AgentId,
            "owner:cancelled",
            AgentVersion,
            Versions(),
            store,
            cancellingClient,
            new FixedTimeProvider(Now.AddSeconds(1)),
            delay,
            CommandTransportRetryPolicy.SandboxDefault);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await cancelled.RunOnceAsync(1, cancellation.Token));
        Assert.Single(cancellingClient.PollRequests);
        await using AgentDbContext verification = fixture.CreateContext();
        AgentCommandTransportStateRow pending = await verification.CommandTransportStates.SingleAsync();
        Assert.Equal(2, pending.PendingSequence);
        Assert.Equal(cancellingClient.PollRequests[0].MessageId, pending.PendingMessageId);
        Assert.Equal("Poll", pending.PendingMessageKind);
        Assert.Equal(1, pending.PendingAttemptCount);
        Assert.Null(pending.LeaseOwner);
    }

    /// <summary>Proves an expired lease advances fencing and permanently rejects the former owner.</summary>
    [Fact]
    public async Task ExpiredLeaseAdvancesFenceAndRejectsStaleOwner()
    {
        await using AgentFixture fixture = await AgentFixture.StartAsync();
        AgentCommandTransportSandboxStore store = new(fixture.Factory);
        long first = Assert.IsType<long>(await store.TryAcquireLeaseAsync(
            fixture.AgentId,
            "owner:first",
            Now,
            TimeSpan.FromSeconds(1),
            CancellationToken.None));
        long second = Assert.IsType<long>(await store.TryAcquireLeaseAsync(
            fixture.AgentId,
            "owner:second",
            Now.AddSeconds(2),
            TimeSpan.FromSeconds(1),
            CancellationToken.None));
        Assert.True(second > first);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await store.GetOrPreparePollAsync(
            fixture.AgentId,
            first,
            AgentVersion,
            Versions(),
            1,
            Now.AddSeconds(2),
            CancellationToken.None));
    }

    private static CommandTransportPollRequest Poll(Guid agentId, long sequence) => new(
        Guid.NewGuid(),
        CommandTransportProtocol.CurrentSchemaVersion,
        agentId,
        sequence,
        Now,
        Now,
        AgentVersion,
        Versions(),
        CommandTransportProtocol.MaximumBatchSize);

    private static Dictionary<string, string> Versions() => new(StringComparer.Ordinal)
    {
        [ProviderId] = ProviderVersion,
    };

    private static CommandTransportEnvelope Envelope(string suffix, DateTimeOffset expiresAt) => new(
        Guid.NewGuid(),
        $"sandbox:{suffix}:{Guid.NewGuid():N}",
        Guid.NewGuid(),
        ProviderId,
        $"sandbox.command.{suffix}.v1",
        "{}",
        Now.AddMinutes(-1),
        expiresAt,
        AgentVersion,
        ProviderVersion,
        CommandExecutionPolicy.Never);

    private sealed class SingleFaultInjector(AgentCommandTransportFaultPoint point)
        : IAgentCommandTransportFaultInjector
    {
        private int armed = 1;

        /// <inheritdoc />
        public ValueTask InjectAsync(
            AgentCommandTransportFaultPoint current,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (current == point && Interlocked.Exchange(ref armed, 0) == 1)
            {
                throw new IOException("command.transport_test_fault");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingDelay : ICommandTransportDelay
    {
        public List<TimeSpan> Delays { get; } = [];

        /// <inheritdoc />
        public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Delays.Add(delay);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingClient(Guid agentId, int failuresBeforeSuccess)
        : ICommandTransportSandboxClient
    {
        private int failures = failuresBeforeSuccess;

        public List<CommandTransportPollRequest> PollRequests { get; } = [];

        /// <inheritdoc />
        public ValueTask<CommandTransportPollResponse> PollAsync(
            CommandTransportPollRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PollRequests.Add(request);
            if (Interlocked.Decrement(ref failures) >= 0)
            {
                throw new IOException("command.transport_response_lost");
            }

            return ValueTask.FromResult(new CommandTransportPollResponse(
                Guid.NewGuid(),
                CommandTransportProtocol.CurrentSchemaVersion,
                agentId,
                request.Sequence,
                request.MessageId,
                Now,
                Now,
                []));
        }

        /// <inheritdoc />
        public ValueTask<CommandTransportAcknowledgementResponse> AcknowledgeAsync(
            CommandTransportAcknowledgementRequest request,
            CancellationToken cancellationToken) => throw new InvalidOperationException("No acknowledgement expected.");
    }

    private sealed class CancellingClient(CancellationTokenSource source) : ICommandTransportSandboxClient
    {
        public List<CommandTransportPollRequest> PollRequests { get; } = [];

        /// <inheritdoc />
        public ValueTask<CommandTransportPollResponse> PollAsync(
            CommandTransportPollRequest request,
            CancellationToken cancellationToken)
        {
            PollRequests.Add(request);
            source.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Cancellation was not observed by the sandbox transport.");
        }

        /// <inheritdoc />
        public ValueTask<CommandTransportAcknowledgementResponse> AcknowledgeAsync(
            CommandTransportAcknowledgementRequest request,
            CancellationToken cancellationToken) => throw new InvalidOperationException("No acknowledgement expected.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class AgentFixture(
        SqliteConnection keeper,
        DbContextOptions<AgentDbContext> options,
        Guid agentId) : IAsyncDisposable
    {
        public Guid AgentId { get; } = agentId;

        public AgentFactory Factory { get; } = new(options);

        public static async Task<AgentFixture> StartAsync()
        {
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = $"CommandTransportAgent{Guid.NewGuid():N}",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true,
            }.ToString();
            SqliteConnection keeper = new(connectionString);
            await keeper.OpenAsync();
            DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
                .UseSqlite(connectionString).Options;
            Guid agentId = Guid.NewGuid();
            await using AgentDbContext setup = new(options);
            await setup.Database.EnsureCreatedAsync();
            setup.Registrations.Add(new AgentRegistrationRow
            {
                AgentId = agentId,
                InstallationId = $"installation:{agentId:N}",
                Environment = "sandbox",
                IdentityCertificateReference = $"sandbox-identity:{agentId:N}",
                CertificateThumbprint = new string('A', 64),
                CertificateNotAfter = Now.AddHours(1),
                IdentityState = "Active",
                CreatedAt = Now,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            await setup.SaveChangesAsync();
            return new AgentFixture(keeper, options, agentId);
        }

        public AgentDbContext CreateContext() => new(options);

        public async ValueTask DisposeAsync() => await keeper.DisposeAsync();
    }

    private sealed class ServerFixture(
        SqliteConnection keeper,
        DbContextOptions<ServerDbContext> options,
        Guid agentId) : IAsyncDisposable
    {
        public Guid AgentId { get; } = agentId;

        public ServerFactory Factory { get; } = new(options);

        public static async Task<ServerFixture> StartAsync()
        {
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = $"CommandTransportServer{Guid.NewGuid():N}",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true,
            }.ToString();
            SqliteConnection keeper = new(connectionString);
            await keeper.OpenAsync();
            DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
                .UseSqlite(connectionString).Options;
            Guid agentId = Guid.NewGuid();
            Guid instanceId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            await using ServerDbContext setup = new(options);
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(new RegisteredAgentRow
            {
                AgentId = agentId,
                InstallationId = $"agent-{agentId:N}",
                DisplayName = "Sandbox Agent",
                Environment = "sandbox",
                Platform = "test",
                AgentVersion = AgentVersion,
                CertificateThumbprint = new string('A', 64),
                State = "Active",
                EnrolledAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.Instances.Add(new DatabaseInstanceRow
            {
                InstanceId = instanceId,
                DisplayName = "Synthetic instance",
                ProviderType = ProviderId,
                Environment = "sandbox",
                EndpointJson = "{}",
                AssignedAgentId = agentId,
                TagsJson = "[]",
                IntervalSeconds = 60,
                TimeoutSeconds = 5,
                RetryCount = 0,
                Enabled = true,
                CreatedAt = Now,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.Users.Add(new PlatformUserRow
            {
                UserId = userId,
                SubjectId = "sandbox:fixture-owner",
                DisplayName = "Fixture owner",
                State = "Active",
                CreatedAt = Now,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.AdministrativeCommands.AddRange(
                Command("01-received", "sandbox.command.received.v1", Now.AddMinutes(5)),
                Command("02-unsupported", "sandbox.command.unsupported.v1", Now.AddMinutes(5)),
                Command("03-expired", "sandbox.command.expired.v1", Now.AddSeconds(-1)),
                Command("04-nonsandbox-blocked", "blocked.namespace.fixture.v1", Now.AddMinutes(5)));
            await setup.SaveChangesAsync();
            return new ServerFixture(keeper, options, agentId);

            AdministrativeCommandRow Command(string key, string capability, DateTimeOffset expiry) => new()
            {
                CommandId = Guid.NewGuid(),
                IdempotencyKey = key,
                InstanceId = instanceId,
                AssignedAgentId = agentId,
                CapabilityId = capability,
                TypedParametersJson = "{}",
                RequestedByUserId = userId,
                RequestedAt = Now.AddMinutes(-1),
                Reason = "Synthetic transport fixture",
                ExpiresAt = expiry,
                AuthorizationSnapshotReference = "sandbox:non-executable",
                ExpectedAgentVersion = AgentVersion,
                ExpectedProviderVersion = ProviderVersion,
                State = "Pending",
                ConcurrencyToken = Guid.NewGuid(),
            };
        }

        public ServerDbContext CreateContext() => new(options);

        public async ValueTask DisposeAsync() => await keeper.DisposeAsync();
    }

    private sealed class AgentFactory(DbContextOptions<AgentDbContext> options)
        : IDbContextFactory<AgentDbContext>
    {
        public AgentDbContext CreateDbContext() => new(options);
    }

    private sealed class ServerFactory(DbContextOptions<ServerDbContext> options)
        : IDbContextFactory<ServerDbContext>
    {
        public ServerDbContext CreateDbContext() => new(options);
    }
}
