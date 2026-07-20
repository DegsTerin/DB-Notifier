// Module purpose: Verifies fail-closed cursors, silent baseline, durable deduplication, policy and local ledger fencing.
using System.Text.Json;
using DBNotifier.Application.Presentation;
using DBNotifier.Infrastructure.Presentation;
using Xunit;

namespace DBNotifier.UnitTests;

public sealed class ReconciledLocalNotificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CursorRoundTripsAndRejectsRollback()
    {
        Guid first = Guid.Parse("10000000-0000-0000-0000-000000000001");
        string prior = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [first] = 4 });
        string next = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [first] = 5 });

        Assert.True(ReconciledNotificationCursorCodec.TryDecode(next, out IReadOnlyDictionary<Guid, long>? decoded));
        Assert.Equal(5, decoded[first]);
        Assert.True(ReconciledNotificationCursorCodec.Dominates(next, prior));
        Assert.False(ReconciledNotificationCursorCodec.Dominates(prior, next));
        Assert.False(ReconciledNotificationCursorCodec.TryDecode(next + "=", out _));
    }

    [Fact]
    public async Task DisabledPolicyPerformsNoInputOrStorageWork()
    {
        FakeReader reader = new();
        FakeLedger ledger = new();
        FakeSink sink = new();
        using ReconciledNotificationCoordinator coordinator = new(reader, ledger, sink, new FixedClock(Now));

        ReconciledNotificationCycleResult result = await coordinator.RunOnceAsync(new(false, false));

        Assert.Equal(ReconciledNotificationCycleDisposition.Disabled, result.Disposition);
        Assert.Equal(0, reader.ReadCount);
        Assert.Equal(0, ledger.ReadCount);
        Assert.Empty(sink.Requests);
    }

    [Fact]
    public async Task BaselineIsSilentThenOneTransitionIsDurablyDeduplicatedAcrossRestart()
    {
        Guid agentId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        string headOne = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 1 });
        string headTwo = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 2 });
        ReconciledNotificationTransition transition = CreateTransition();
        FakeReader reader = new()
        {
            Baseline = Page(ReconciledNotificationTransitionContract.EmptyCursor, headOne),
            Continuation = Page(headOne, headTwo, transition),
        };
        FakeLedger ledger = new();
        FakeSink sink = new();
        using (ReconciledNotificationCoordinator first = new(reader, ledger, sink, new FixedClock(Now)))
        {
            Assert.Equal(ReconciledNotificationCycleDisposition.BaselineEstablished,
                (await first.RunOnceAsync(new(true, false))).Disposition);
            Assert.Empty(sink.Requests);
            Assert.Equal(ReconciledNotificationCycleDisposition.Completed,
                (await first.RunOnceAsync(new(true, false))).Disposition);
        }

        reader.Continuation = Page(headTwo, headTwo);
        using ReconciledNotificationCoordinator restarted = new(reader, ledger, sink, new FixedClock(Now));
        Assert.Equal(ReconciledNotificationCycleDisposition.NoChanges,
            (await restarted.RunOnceAsync(new(true, false))).Disposition);
        Assert.Single(sink.Requests);
        Assert.Equal(headTwo, ledger.State.Cursor);
        Assert.Equal(ReconciledNotificationLedgerDisposition.Accepted, Assert.Single(ledger.State.Entries).Disposition);
    }

    [Theory]
    [InlineData(true, "current")]
    [InlineData(false, "stale")]
    public async Task QuietOrNonCurrentTransitionIsSuppressedWithoutDelivery(bool quiet, string freshness)
    {
        Guid agentId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        string from = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 1 });
        string next = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 2 });
        FakeReader reader = new() { Continuation = Page(from, next, CreateTransition(freshness)) };
        FakeLedger ledger = new() { State = ReconciledNotificationLedgerState.Empty with { Cursor = from } };
        FakeSink sink = new();
        using ReconciledNotificationCoordinator coordinator = new(reader, ledger, sink, new FixedClock(Now));

        ReconciledNotificationCycleResult result = await coordinator.RunOnceAsync(new(true, quiet));

        Assert.Equal(ReconciledNotificationCycleDisposition.Completed, result.Disposition);
        Assert.Equal(1, result.Suppressed);
        Assert.Empty(sink.Requests);
        Assert.Equal(ReconciledNotificationLedgerDisposition.Suppressed, Assert.Single(ledger.State.Entries).Disposition);
    }

    [Fact]
    public async Task FileLedgerRejectsASecondOwnerAndPersistsOneAtomicRevision()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dbn-notification-ledger-{Guid.NewGuid():N}");
        try
        {
            FileReconciledNotificationLedger first = new(directory);
            Assert.Throws<IOException>(() => new FileReconciledNotificationLedger(directory));
            ReconciledNotificationLedgerState replacement = ReconciledNotificationLedgerState.Empty with
            {
                Revision = 1,
                Cursor = ReconciledNotificationTransitionContract.EmptyCursor,
            };
            Assert.True(await first.TryReplaceAsync(0, replacement, CancellationToken.None));
            await first.DisposeAsync();

            FileReconciledNotificationLedger reopened = new(directory);
            Assert.Equal(1, (await reopened.ReadAsync(CancellationToken.None)).Revision);
            await reopened.DisposeAsync();
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Proves pre-R1 acceptance is migrated as terminal uncertainty rather than trusted or replayed.</summary>
    [Fact]
    public async Task LegacyAcceptedEntryMigratesConservatively()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dbn-notification-ledger-migration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            ReconciledNotificationTransition transition = CreateTransition();
            byte[] legacy = JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = "reconciled-local-notification-ledger.v1",
                revision = 4,
                cursor = ReconciledNotificationTransitionContract.EmptyCursor,
                entries = new[]
                {
                    new
                    {
                        eventId = transition.EventId,
                        contentSha256 = ReconciledNotificationIdentity.CreateContentSha256(transition),
                        windowsTag = ReconciledNotificationIdentity.CreateWindowsTag(transition.EventId),
                        disposition = (int)ReconciledNotificationLedgerDisposition.Accepted,
                        attemptCount = 1,
                        firstSeenAt = Now.AddSeconds(-10),
                        lastDecidedAt = Now.AddSeconds(-5),
                        resultCode = "delivery.local_platform_accepted",
                    },
                },
            }, JsonSerializerOptions.Web);
            await File.WriteAllBytesAsync(
                Path.Combine(directory, "reconciled-notification-ledger.v1.json"),
                legacy);

            await using FileReconciledNotificationLedger ledger = new(directory);
            ReconciledNotificationLedgerState migrated = await ledger.ReadAsync(CancellationToken.None);

            Assert.Equal(ReconciledNotificationLedgerState.CurrentSchemaVersion, migrated.SchemaVersion);
            Assert.Equal(2, migrated.NextQueueSequence);
            ReconciledNotificationLedgerEntry entry = Assert.Single(migrated.Entries);
            Assert.Equal(ReconciledNotificationLedgerDisposition.Rejected, entry.Disposition);
            Assert.Equal("migration.pre_r1_acceptance_uncertain", entry.ResultCode);
            Assert.True(await ledger.TryReplaceAsync(
                migrated.Revision,
                migrated with { Revision = migrated.Revision + 1 },
                CancellationToken.None));
            Assert.True(File.Exists(Path.Combine(directory, "reconciled-notification-ledger.v2.json")));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Proves an existing v2 ledger still fences a concurrently running pre-R1 owner.</summary>
    [Fact]
    public async Task VersionTwoLedgerRetainsTheLegacyOwnershipFence()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dbn-notification-ledger-legacy-fence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            await using (FileReconciledNotificationLedger initial = new(directory))
            {
                ReconciledNotificationLedgerState state = await initial.ReadAsync(CancellationToken.None);
                Assert.True(await initial.TryReplaceAsync(
                    state.Revision,
                    state with { Revision = state.Revision + 1 },
                    CancellationToken.None));
            }

            using (FileStream legacyOwner = new(
                Path.Combine(directory, "reconciled-notification-ledger.v1.lock"),
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None))
            {
                Assert.Throws<IOException>(() => new FileReconciledNotificationLedger(directory));
            }

            await using FileReconciledNotificationLedger reopened = new(directory);
            Assert.Equal(1, (await reopened.ReadAsync(CancellationToken.None)).Revision);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Proves the complete bounded page is durable before its first platform hand-off begins.</summary>
    [Fact]
    public async Task BurstIsDurablyQueuedBeforeFirstBoundaryCall()
    {
        Guid agentId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        string from = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 1 });
        string next = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 4 });
        ReconciledNotificationTransition[] transitions =
        [
            CreateTransition(eventId: Guid.Parse("61000000-0000-0000-0000-000000000001")),
            CreateTransition(eventId: Guid.Parse("61000000-0000-0000-0000-000000000002")),
            CreateTransition(eventId: Guid.Parse("61000000-0000-0000-0000-000000000003")),
        ];
        FakeReader reader = new() { Continuation = Page(from, next, transitions) };
        FakeLedger ledger = new() { State = ReconciledNotificationLedgerState.Empty with { Cursor = from } };
        int boundaryCalls = 0;
        FakeSink sink = new()
        {
            BeforeDelivery = () =>
            {
                if (boundaryCalls++ == 0)
                {
                    Assert.Equal(
                        3,
                        ledger.State.Entries.Count(entry => entry.Disposition is
                            ReconciledNotificationLedgerDisposition.Queued or
                            ReconciledNotificationLedgerDisposition.Attempting));
                }
            },
        };
        using ReconciledNotificationCoordinator coordinator = new(reader, ledger, sink, new FixedClock(Now));

        ReconciledNotificationCycleResult result = await coordinator.RunOnceAsync(new(true, false));

        Assert.Equal(ReconciledNotificationCycleDisposition.Completed, result.Disposition);
        Assert.Equal(3, result.Accepted);
        Assert.Equal(transitions.Select(item => item.EventId), sink.Requests.Select(item => item.EventId));
        Assert.Contains(ledger.History, state => state.Entries.Count(entry =>
            entry.Disposition == ReconciledNotificationLedgerDisposition.Queued) == 3);
    }

    /// <summary>Proves retryable delivery observes backoff and becomes a factual rejection after its budget.</summary>
    [Fact]
    public async Task RetryableBoundaryHonoursBackoffThenRejectsAfterBudget()
    {
        Guid agentId = Guid.Parse("62000000-0000-0000-0000-000000000001");
        string from = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 1 });
        string next = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 2 });
        FakeReader reader = new() { Continuation = Page(from, next, CreateTransition()) };
        FakeLedger ledger = new() { State = ReconciledNotificationLedgerState.Empty with { Cursor = from } };
        FakeSink sink = new();
        sink.Outcomes.Enqueue(ReconciledNotificationDeliveryDisposition.Retryable);
        sink.Outcomes.Enqueue(ReconciledNotificationDeliveryDisposition.Retryable);
        MutableClock clock = new(Now);
        using ReconciledNotificationCoordinator coordinator = new(reader, ledger, sink, clock);

        Assert.Equal(ReconciledNotificationCycleDisposition.Retryable,
            (await coordinator.RunOnceAsync(new(true, false))).Disposition);
        Assert.Equal(ReconciledNotificationCycleDisposition.Retryable,
            (await coordinator.RunOnceAsync(new(true, false))).Disposition);
        Assert.Single(sink.Requests);

        clock.Advance(TimeSpan.FromSeconds(5));
        ReconciledNotificationCycleResult exhausted = await coordinator.RunOnceAsync(new(true, false));

        Assert.Equal(ReconciledNotificationCycleDisposition.Completed, exhausted.Disposition);
        Assert.Equal(1, exhausted.FailedTerminal);
        Assert.Equal(2, sink.Requests.Count);
        Assert.Equal(ReconciledNotificationLedgerDisposition.Rejected, Assert.Single(ledger.State.Entries).Disposition);
        Assert.Equal(next, ledger.State.Cursor);
    }

    /// <summary>Proves a non-cooperative sink cannot hold the delivery cycle beyond its enforced deadline.</summary>
    [Fact]
    public async Task NonCooperativeBoundaryBecomesTerminalDeadlineUncertainty()
    {
        Guid agentId = Guid.Parse("62500000-0000-0000-0000-000000000001");
        string from = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 1 });
        string next = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 2 });
        FakeReader reader = new() { Continuation = Page(from, next, CreateTransition()) };
        FakeLedger ledger = new() { State = ReconciledNotificationLedgerState.Empty with { Cursor = from } };
        TaskCompletionSource<ReconciledNotificationDeliveryResult> blocked = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        FakeSink sink = new()
        {
            Delivery = (_, _) => new ValueTask<ReconciledNotificationDeliveryResult>(blocked.Task),
        };
        using ReconciledNotificationCoordinator coordinator = new(
            reader,
            ledger,
            sink,
            new ImmediateDeadlineClock(Now));

        ReconciledNotificationCycleResult result = await coordinator.RunOnceAsync(new(true, false));

        Assert.Equal(ReconciledNotificationCycleDisposition.Completed, result.Disposition);
        Assert.Equal(1, result.FailedTerminal);
        ReconciledNotificationLedgerEntry rejected = Assert.Single(ledger.State.Entries);
        Assert.Equal(ReconciledNotificationLedgerDisposition.Rejected, rejected.Disposition);
        Assert.Equal("delivery.deadline_uncertain", rejected.ResultCode);
        Assert.Equal(next, ledger.State.Cursor);
    }

    /// <summary>Proves a queued durable item resumes after restart without a second queue admission.</summary>
    [Fact]
    public async Task QueuedEntryResumesAcrossCoordinatorRestart()
    {
        Guid agentId = Guid.Parse("63000000-0000-0000-0000-000000000001");
        string from = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 1 });
        string next = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 2 });
        ReconciledNotificationTransition transition = CreateTransition();
        FakeReader reader = new() { Continuation = Page(from, next, transition) };
        FakeLedger ledger = new()
        {
            State = ReconciledNotificationLedgerState.Empty with
            {
                Cursor = from,
                NextQueueSequence = 2,
                Entries = [CreateEntry(transition, ReconciledNotificationLedgerDisposition.Queued, 0, 1)],
            },
        };
        FakeSink sink = new();
        using ReconciledNotificationCoordinator coordinator = new(reader, ledger, sink, new FixedClock(Now));

        ReconciledNotificationCycleResult result = await coordinator.RunOnceAsync(new(true, false));

        Assert.Equal(ReconciledNotificationCycleDisposition.Completed, result.Disposition);
        Assert.Single(sink.Requests);
        Assert.Equal(ReconciledNotificationLedgerDisposition.Accepted, Assert.Single(ledger.State.Entries).Disposition);
    }

    /// <summary>Proves an interrupted non-transactional hand-off is terminally classified without replay.</summary>
    [Fact]
    public async Task InterruptedAttemptIsRejectedWithoutRedelivery()
    {
        Guid agentId = Guid.Parse("64000000-0000-0000-0000-000000000001");
        string from = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 1 });
        string next = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 2 });
        ReconciledNotificationTransition transition = CreateTransition();
        FakeReader reader = new() { Continuation = Page(from, next, transition) };
        FakeLedger ledger = new()
        {
            State = ReconciledNotificationLedgerState.Empty with
            {
                Cursor = from,
                NextQueueSequence = 2,
                Entries = [CreateEntry(transition, ReconciledNotificationLedgerDisposition.Attempting, 1, 1)],
            },
        };
        FakeSink sink = new();
        using ReconciledNotificationCoordinator coordinator = new(reader, ledger, sink, new FixedClock(Now));

        ReconciledNotificationCycleResult result = await coordinator.RunOnceAsync(new(true, false));

        Assert.Equal(ReconciledNotificationCycleDisposition.Completed, result.Disposition);
        Assert.Equal(1, result.FailedTerminal);
        Assert.Empty(sink.Requests);
        ReconciledNotificationLedgerEntry rejected = Assert.Single(ledger.State.Entries);
        Assert.Equal(ReconciledNotificationLedgerDisposition.Rejected, rejected.Disposition);
        Assert.Equal("delivery.uncertain_after_restart", rejected.ResultCode);
    }

    /// <summary>Proves replay order comes from the durable queue rather than a reordered server page.</summary>
    [Fact]
    public async Task ReorderedReplayUsesDurableQueueOrder()
    {
        Guid agentId = Guid.Parse("65000000-0000-0000-0000-000000000001");
        string from = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 1 });
        string next = ReconciledNotificationCursorCodec.Encode(new Dictionary<Guid, long> { [agentId] = 3 });
        ReconciledNotificationTransition first = CreateTransition(
            eventId: Guid.Parse("65100000-0000-0000-0000-000000000001"));
        ReconciledNotificationTransition second = CreateTransition(
            eventId: Guid.Parse("65100000-0000-0000-0000-000000000002"));
        FakeReader reader = new() { Continuation = Page(from, next, second, first) };
        FakeLedger ledger = new()
        {
            State = ReconciledNotificationLedgerState.Empty with
            {
                Cursor = from,
                NextQueueSequence = 3,
                Entries =
                [
                    CreateEntry(first, ReconciledNotificationLedgerDisposition.Queued, 0, 1),
                    CreateEntry(second, ReconciledNotificationLedgerDisposition.Queued, 0, 2),
                ],
            },
        };
        FakeSink sink = new();
        using ReconciledNotificationCoordinator coordinator = new(reader, ledger, sink, new FixedClock(Now));

        await coordinator.RunOnceAsync(new(true, false));

        Assert.Equal([first.EventId, second.EventId], sink.Requests.Select(item => item.EventId));
    }

    private static ReconciledNotificationTransitionPage Page(
        string from,
        string next,
        params ReconciledNotificationTransition[] items) => new(
            ReconciledNotificationTransitionContract.CurrentSchemaVersion,
            from,
            next,
            false,
            items);

    private static ReconciledNotificationTransition CreateTransition(
        string freshness = "current",
        Guid? eventId = null) => new(
        eventId ?? Guid.Parse("40000000-0000-0000-0000-000000000001"),
        Guid.Parse("50000000-0000-0000-0000-000000000001"),
        "Synthetic finance database",
        "Disconnected",
        "Error",
        "Healthy",
        "Unavailable",
        Now.AddSeconds(-2),
        Now.AddSeconds(-1),
        freshness,
        ReconciledNotificationTransitionContract.SyntheticSourceKind);

    private static ReconciledNotificationLedgerEntry CreateEntry(
        ReconciledNotificationTransition transition,
        ReconciledNotificationLedgerDisposition disposition,
        int attemptCount,
        long queueSequence) => new(
        transition.EventId,
        ReconciledNotificationIdentity.CreateContentSha256(transition),
        ReconciledNotificationIdentity.CreateWindowsTag(transition.EventId),
        disposition,
        attemptCount,
        queueSequence,
        Now.AddSeconds(-10),
        Now.AddSeconds(-5),
        disposition == ReconciledNotificationLedgerDisposition.Queued ? "delivery.queued" : "delivery.attempting");

    private sealed class FakeReader : IReconciledNotificationTransitionReader
    {
        public ReconciledNotificationTransitionPage Baseline { get; set; } =
            Page(ReconciledNotificationTransitionContract.EmptyCursor, ReconciledNotificationTransitionContract.EmptyCursor);
        public ReconciledNotificationTransitionPage Continuation { get; set; } =
            Page(ReconciledNotificationTransitionContract.EmptyCursor, ReconciledNotificationTransitionContract.EmptyCursor);
        public int ReadCount { get; private set; }

        public ValueTask<ReconciledNotificationReadResult> ReadBaselineAsync(CancellationToken cancellationToken)
        {
            ReadCount++;
            return ValueTask.FromResult(new ReconciledNotificationReadResult(
                ReconciledNotificationReadDisposition.Accepted, Baseline, null));
        }

        public ValueTask<ReconciledNotificationReadResult> ReadAfterAsync(string cursor, CancellationToken cancellationToken)
        {
            ReadCount++;
            return ValueTask.FromResult(new ReconciledNotificationReadResult(
                ReconciledNotificationReadDisposition.Accepted, Continuation, null));
        }
    }

    private sealed class FakeLedger : IReconciledNotificationLedger
    {
        public ReconciledNotificationLedgerState State { get; set; } = ReconciledNotificationLedgerState.Empty;
        public List<ReconciledNotificationLedgerState> History { get; } = [];
        public int ReadCount { get; private set; }

        public ValueTask<ReconciledNotificationLedgerState> ReadAsync(CancellationToken cancellationToken)
        {
            ReadCount++;
            return ValueTask.FromResult(State);
        }

        public ValueTask<bool> TryReplaceAsync(
            long expectedRevision,
            ReconciledNotificationLedgerState replacement,
            CancellationToken cancellationToken)
        {
            if (State.Revision != expectedRevision) return ValueTask.FromResult(false);
            State = replacement;
            History.Add(replacement with { Entries = replacement.Entries.ToArray() });
            return ValueTask.FromResult(true);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeSink : IReconciledNotificationSink
    {
        public List<ReconciledNotificationDeliveryRequest> Requests { get; } = [];
        public Queue<ReconciledNotificationDeliveryDisposition> Outcomes { get; } = new();
        public Action? BeforeDelivery { get; init; }
        public Func<ReconciledNotificationDeliveryRequest, CancellationToken,
            ValueTask<ReconciledNotificationDeliveryResult>>? Delivery
        { get; init; }

        public ValueTask<ReconciledNotificationDeliveryResult> DeliverAsync(
            ReconciledNotificationDeliveryRequest request,
            CancellationToken cancellationToken)
        {
            BeforeDelivery?.Invoke();
            Requests.Add(request);
            if (Delivery is not null)
            {
                return Delivery(request, cancellationToken);
            }
            return ValueTask.FromResult(new ReconciledNotificationDeliveryResult(
                Outcomes.TryDequeue(out ReconciledNotificationDeliveryDisposition outcome)
                    ? outcome
                    : ReconciledNotificationDeliveryDisposition.Accepted,
                "delivery.test_result"));
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;

        public override DateTimeOffset GetUtcNow() => current;

        public void Advance(TimeSpan duration) => current += duration;
    }

    /// <summary>Provides an immediate timer solely to exercise bounded waits without slowing the unit suite.</summary>
    private sealed class ImmediateDeadlineClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period) => new ImmediateTimer(callback, state);
    }

    /// <summary>Fires one deadline callback asynchronously and supports the timer disposal contract.</summary>
    private sealed class ImmediateTimer : ITimer
    {
        private readonly CancellationTokenSource disposal = new();

        public ImmediateTimer(TimerCallback callback, object? state)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (!disposal.IsCancellationRequested)
                {
                    callback(state);
                }
            });
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => false;

        public void Dispose() => disposal.Cancel();

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
