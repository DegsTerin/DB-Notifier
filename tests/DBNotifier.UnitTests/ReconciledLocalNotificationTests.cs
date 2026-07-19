// Module purpose: Verifies fail-closed cursors, silent baseline, durable deduplication, policy and local ledger fencing.
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

    private static ReconciledNotificationTransitionPage Page(
        string from,
        string next,
        params ReconciledNotificationTransition[] items) => new(
            ReconciledNotificationTransitionContract.CurrentSchemaVersion,
            from,
            next,
            false,
            items);

    private static ReconciledNotificationTransition CreateTransition(string freshness = "current") => new(
        Guid.Parse("40000000-0000-0000-0000-000000000001"),
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
            return ValueTask.FromResult(true);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeSink : IReconciledNotificationSink
    {
        public List<ReconciledNotificationDeliveryRequest> Requests { get; } = [];

        public ValueTask<ReconciledNotificationDeliveryResult> DeliverAsync(
            ReconciledNotificationDeliveryRequest request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return ValueTask.FromResult(new ReconciledNotificationDeliveryResult(
                ReconciledNotificationDeliveryDisposition.Accepted,
                "delivery.accepted"));
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
