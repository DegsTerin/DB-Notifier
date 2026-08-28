// Module purpose: Verifies serial read-only desktop fleet acquisition, structural validation and last-known evidence retention.
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;

namespace DBNotifier.UnitTests;

/// <summary>Protects the provider-neutral application boundary used by the Windows notification-area client.</summary>
public sealed class DesktopFleetReconciliationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);

    /// <summary>Verifies that a valid current-schema snapshot establishes one coherent accepted frame.</summary>
    [Fact]
    public async Task ValidSnapshotIsAcceptedAndSummarisedAtOneClockInstant()
    {
        AdjustableTimeProvider clock = new(Now);
        InventorySnapshot snapshot = CreateSnapshot(Now, CreateItem(Guid.NewGuid(), HealthStatus.Healthy, Now));
        QueueSnapshotSource source = new(Accept(snapshot));
        DesktopFleetReconciliationCoordinator coordinator = new(source, StaleAfter, clock);

        DesktopFleetReconciliationFrame frame = await coordinator.ReconcileAsync(DesktopFleetRefreshTrigger.Initial);

        Assert.Same(snapshot.Items[0], frame.Snapshot!.Items[0]);
        Assert.NotSame(snapshot.Items, frame.Snapshot.Items);
        Assert.Equal(DesktopFleetReadDisposition.Accepted, frame.Disposition);
        Assert.Equal(Now, frame.EvaluatedAt);
        Assert.Equal(TrayAggregateState.Healthy, frame.Summary.State);
        Assert.False(frame.RetainedLastAcceptedSnapshot);
    }

    /// <summary>Verifies that overlapping callers receive Busy while the first acquisition retains sole ownership.</summary>
    [Fact]
    public async Task OverlappingRefreshIsRejectedBySingleFlightBoundary()
    {
        AdjustableTimeProvider clock = new(Now);
        BlockingSnapshotSource source = new(Accept(CreateSnapshot(
            Now,
            CreateItem(Guid.NewGuid(), HealthStatus.Healthy, Now))));
        DesktopFleetReconciliationCoordinator coordinator = new(source, StaleAfter, clock);

        Task<DesktopFleetReconciliationFrame> first = coordinator
            .ReconcileAsync(DesktopFleetRefreshTrigger.Periodic)
            .AsTask();
        await source.WaitUntilEnteredAsync();

        DesktopFleetReconciliationFrame overlapping = await coordinator.ReconcileAsync(
            DesktopFleetRefreshTrigger.Manual);
        source.Release();
        DesktopFleetReconciliationFrame accepted = await first;

        Assert.Equal(DesktopFleetReadDisposition.Busy, overlapping.Disposition);
        Assert.Null(overlapping.Snapshot);
        Assert.Equal(DesktopFleetReadDisposition.Accepted, accepted.Disposition);
        Assert.Equal(1, source.ReadCount);
    }

    /// <summary>Verifies that an offline read retains accepted evidence while its freshness ages to Unknown.</summary>
    [Fact]
    public async Task OfflineReadRetainsLastAcceptedSnapshotAndAgesFreshness()
    {
        AdjustableTimeProvider clock = new(Now);
        InventorySnapshot snapshot = CreateSnapshot(
            Now,
            CreateItem(Guid.NewGuid(), HealthStatus.Healthy, Now));
        QueueSnapshotSource source = new(
            Accept(snapshot),
            new(DesktopFleetReadDisposition.Offline, null, "source.offline"));
        DesktopFleetReconciliationCoordinator coordinator = new(source, StaleAfter, clock);
        await coordinator.ReconcileAsync(DesktopFleetRefreshTrigger.Initial);
        clock.Advance(TimeSpan.FromMinutes(6));

        DesktopFleetReconciliationFrame frame = await coordinator.ReconcileAsync(DesktopFleetRefreshTrigger.Periodic);

        Assert.Equal(DesktopFleetReadDisposition.Offline, frame.Disposition);
        Assert.True(frame.RetainedLastAcceptedSnapshot);
        Assert.NotNull(frame.Snapshot);
        Assert.Equal(TrayAggregateState.Unknown, frame.Summary.State);
        Assert.Equal(1, frame.Summary.UnknownCount);
    }

    /// <summary>Verifies that a structurally invalid accepted candidate cannot replace prior accepted evidence.</summary>
    [Fact]
    public async Task InvalidCandidateIsIncompatibleAndCannotReplacePriorSnapshot()
    {
        AdjustableTimeProvider clock = new(Now);
        Guid identifier = Guid.NewGuid();
        InventorySnapshot initial = CreateSnapshot(Now, CreateItem(identifier, HealthStatus.Healthy, Now));
        InventorySnapshot duplicate = CreateSnapshot(
            Now,
            CreateItem(identifier, HealthStatus.Timeout, Now),
            CreateItem(identifier, HealthStatus.Timeout, Now));
        QueueSnapshotSource source = new(Accept(initial), Accept(duplicate));
        DesktopFleetReconciliationCoordinator coordinator = new(source, StaleAfter, clock);
        DesktopFleetReconciliationFrame accepted = await coordinator.ReconcileAsync(DesktopFleetRefreshTrigger.Initial);

        DesktopFleetReconciliationFrame rejected = await coordinator.ReconcileAsync(DesktopFleetRefreshTrigger.Manual);

        Assert.Equal(DesktopFleetReadDisposition.Incompatible, rejected.Disposition);
        Assert.True(rejected.RetainedLastAcceptedSnapshot);
        Assert.Equal(accepted.Snapshot, rejected.Snapshot);
        Assert.Equal(TrayAggregateState.Healthy, rejected.Summary.State);
        Assert.Equal("snapshot.invalid", rejected.ReasonCode);
    }

    /// <summary>Verifies that source exceptions fail closed without leaking exception text or manufacturing evidence.</summary>
    [Fact]
    public async Task InitialSourceFailureProducesUnknownFrameWithoutSnapshot()
    {
        AdjustableTimeProvider clock = new(Now);
        DesktopFleetReconciliationCoordinator coordinator = new(new ThrowingSnapshotSource(), StaleAfter, clock);

        DesktopFleetReconciliationFrame frame = await coordinator.ReconcileAsync(DesktopFleetRefreshTrigger.Initial);

        Assert.Equal(DesktopFleetReadDisposition.Failed, frame.Disposition);
        Assert.Equal("source.failed", frame.ReasonCode);
        Assert.Null(frame.Snapshot);
        Assert.Equal(TrayAggregateState.Unknown, frame.Summary.State);
        Assert.False(frame.RetainedLastAcceptedSnapshot);
    }

    /// <summary>Creates one accepted source result for a test-owned provider-neutral snapshot.</summary>
    private static DesktopFleetSnapshotReadResult Accept(InventorySnapshot snapshot) =>
        new(DesktopFleetReadDisposition.Accepted, snapshot, "source.accepted");

    /// <summary>Creates one current-schema snapshot containing the supplied immutable items.</summary>
    private static InventorySnapshot CreateSnapshot(
        DateTimeOffset generatedAt,
        params InstanceInventoryItem[] items) =>
        new(InventorySnapshot.CurrentSchemaVersion, generatedAt, items);

    /// <summary>Creates one structurally valid provider-neutral inventory item.</summary>
    private static InstanceInventoryItem CreateItem(
        Guid identifier,
        HealthStatus status,
        DateTimeOffset receivedAt) =>
        new(
            identifier,
            "Database instance",
            "sample-provider",
            "Fixture only",
            "Test",
            "Local fixture",
            status,
            receivedAt.AddSeconds(-1),
            receivedAt,
            TimeSpan.FromMilliseconds(12),
            true);

    /// <summary>Provides a deterministic test clock whose UTC instant advances only when requested.</summary>
    private sealed class AdjustableTimeProvider(DateTimeOffset initialValue) : TimeProvider
    {
        private DateTimeOffset current = initialValue;

        /// <summary>Returns the current test-owned UTC instant.</summary>
        public override DateTimeOffset GetUtcNow() => current;

        /// <summary>Advances the test-owned instant by a positive or negative duration.</summary>
        internal void Advance(TimeSpan duration) => current += duration;
    }

    /// <summary>Returns queued source results in deterministic order.</summary>
    private sealed class QueueSnapshotSource(params DesktopFleetSnapshotReadResult[] results) : IDesktopFleetSnapshotSource
    {
        private readonly Queue<DesktopFleetSnapshotReadResult> results = new(results);

        /// <summary>Returns the next queued result without external activity.</summary>
        public ValueTask<DesktopFleetSnapshotReadResult> ReadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(results.Dequeue());
        }
    }

    /// <summary>Holds one acquisition open so the test can exercise the non-overlapping boundary.</summary>
    private sealed class BlockingSnapshotSource(DesktopFleetSnapshotReadResult result) : IDesktopFleetSnapshotSource
    {
        private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Gets the number of source invocations observed by the test.</summary>
        internal int ReadCount { get; private set; }

        /// <summary>Blocks the sole accepted acquisition until the test releases it.</summary>
        public async ValueTask<DesktopFleetSnapshotReadResult> ReadAsync(CancellationToken cancellationToken)
        {
            ReadCount++;
            entered.SetResult();
            await released.Task.WaitAsync(cancellationToken);
            return result;
        }

        /// <summary>Waits until the first source invocation owns the acquisition boundary.</summary>
        internal Task WaitUntilEnteredAsync() => entered.Task;

        /// <summary>Allows the blocked source invocation to return its accepted result.</summary>
        internal void Release() => released.SetResult();
    }

    /// <summary>Throws a sensitive-looking diagnostic to prove the coordinator replaces it with a stable reason code.</summary>
    private sealed class ThrowingSnapshotSource : IDesktopFleetSnapshotSource
    {
        /// <summary>Fails without returning a snapshot.</summary>
        public ValueTask<DesktopFleetSnapshotReadResult> ReadAsync(CancellationToken cancellationToken) =>
            ValueTask.FromException<DesktopFleetSnapshotReadResult>(
                new InvalidOperationException("provider-secret-diagnostic"));
    }
}
