// Module purpose: Proves the frozen O5-R5-A runner contract with synthetic counters and no physical campaign.
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>
/// Verifies preregistration, phase labelling, deterministic boundaries, serial admission and
/// fail-closed statistics without invoking the physical measurement source.
/// </summary>
public sealed class O5R5MeasurementReadinessRunnerTests
{
    private const string ExpectedProtocolDigest =
        "53F40F7DC72548EB488FFF729823BF0DCFD64EDD085314022C8E746CC45B5D71";

    /// <summary>Proves every physical threshold and statistical rule was frozen before synthetic execution.</summary>
    [Fact]
    public void FrozenProtocolHasExactEnvelopeThresholdsAndDigest()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        Assert.Equal(ExpectedProtocolDigest, protocol.Digest);
        Assert.Equal("pfobs1-physical-measurement-2.0.0", O5R5MeasurementProtocol.Version);
        Assert.Equal(O1ResourceEnvelope.Fixture(), protocol.Envelope);
        Assert.Equal(5, O5R5MeasurementProtocol.WarmUpRepetitions);
        Assert.Equal(30, O5R5MeasurementProtocol.MeasuredRepetitions);
        Assert.Equal(5, O5R5MeasurementProtocol.RepeatabilityGroupCount);
        Assert.Equal(6, O5R5MeasurementProtocol.SamplesPerRepeatabilityGroup);
        Assert.Equal(2, O5R5MeasurementProtocol.RequiredConsecutiveCampaigns);
        Assert.Equal(64, O5R5MeasurementProtocol.MaximumCheckpoints);
        Assert.Equal(0.20d, O5R5MeasurementProtocol.MaximumCoefficientOfVariation);
        Assert.Equal(100d, O5R5MeasurementProtocol.MaximumElapsedMicrosecondsPerWorkUnit);
        Assert.Equal(100d, O5R5MeasurementProtocol.MaximumCpuMicrosecondsPerWorkUnit);
        Assert.Equal(786_432, protocol.EmpiricalMemoryLimit(524_288));
        Assert.Equal(8, protocol.PhaseLimits.Count);
        Assert.Equal(
            TimeSpan.FromMilliseconds(2_250),
            protocol.LimitFor(O5R5MeasurementPhase.FirstByte).MaximumElapsed);
        Assert.Equal(
            TimeSpan.FromMilliseconds(2_250),
            protocol.LimitFor(O5R5MeasurementPhase.Idle).MaximumElapsed);
        Assert.Equal(
            TimeSpan.FromMilliseconds(3_000),
            protocol.LimitFor(O5R5MeasurementPhase.Cancellation).MaximumElapsed);
        Assert.Equal(
            TimeSpan.FromMilliseconds(3_000),
            protocol.LimitFor(O5R5MeasurementPhase.ControlUpdate).MaximumElapsed);
        Assert.All(
            new[]
            {
                O5R5MeasurementPhase.Parse,
                O5R5MeasurementPhase.Cryptography,
                O5R5MeasurementPhase.Sort,
                O5R5MeasurementPhase.Analysis,
            },
            phase =>
            {
                O5R5PhaseLimit limit = protocol.LimitFor(phase);
                Assert.Equal(TimeSpan.FromSeconds(10), limit.MaximumElapsed);
                Assert.True(limit.MeasuresWorkRate);
            });
    }

    /// <summary>Proves all eight allow-listed phases return complete, labelled and non-authorising samples.</summary>
    [Fact]
    public async Task EveryPhaseProducesOneCompleteSyntheticSample()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();

        foreach (O5R5MeasurementPhase phase in Enum.GetValues<O5R5MeasurementPhase>())
        {
            SyntheticMeasurementSource source = Pair(
                frequency: 1_000,
                after: Snapshot(1, 101, 101, 101, TimeSpan.FromMilliseconds(1)));
            O5R5MeasurementReadinessRunner runner = Runner(protocol, source);

            O5R5MeasurementResult result = await runner.RunAsync(
                Scenario(phase, declaredWorkUnits: 10));

            Assert.Equal(O5R5MeasurementDisposition.Accepted, result.Disposition);
            Assert.Equal("o5r5a.measurement.accepted", result.Code);
            Assert.NotNull(result.Sample);
            Assert.Equal(phase, result.Sample.Phase);
            Assert.Equal(ExpectedProtocolDigest, result.Sample.ProtocolDigest);
            Assert.True(result.Sample.Passed);
            Assert.False(O5R5MeasurementResult.MayEvaluate);
            Assert.False(O5R5MeasurementResult.MayPublish);
            Assert.Equal(2, source.CaptureCount);
            if (O5R5MeasurementProtocol.IsComputePhase(phase))
            {
                Assert.Equal(100d, result.Sample.ElapsedMicrosecondsPerWorkUnit);
                Assert.Equal(100d, result.Sample.CpuMicrosecondsPerWorkUnit);
            }
            else
            {
                Assert.Null(result.Sample.ElapsedMicrosecondsPerWorkUnit);
                Assert.Null(result.Sample.CpuMicrosecondsPerWorkUnit);
            }
        }
    }

    /// <summary>Proves inclusive elapsed, memory and work-cost boundaries accept N and reject N plus one.</summary>
    [Fact]
    public async Task InclusiveMetricBoundariesFailClosedAtPlusOne()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        O5R5MeasurementResult elapsedAtLimit = await Runner(
                protocol,
                Pair(
                    1_000,
                    Snapshot(2_250, 100, 100, 100, TimeSpan.Zero)))
            .RunAsync(Scenario(O5R5MeasurementPhase.FirstByte));
        O5R5MeasurementResult elapsedAboveLimit = await Runner(
                protocol,
                Pair(
                    1_000,
                    Snapshot(2_251, 100, 100, 100, TimeSpan.Zero)))
            .RunAsync(Scenario(O5R5MeasurementPhase.FirstByte));

        const long reserved = 524_288;
        const long empiricalLimit = 786_432;
        O5R5MeasurementResult memoryAtLimit = await Runner(
                protocol,
                Pair(
                    1_000,
                    Snapshot(
                        1,
                        100 + empiricalLimit,
                        100 + empiricalLimit,
                        100 + empiricalLimit,
                        TimeSpan.Zero)))
            .RunAsync(Scenario(O5R5MeasurementPhase.FirstByte, accountedMemoryBytes: reserved));
        O5R5MeasurementResult memoryAboveLimit = await Runner(
                protocol,
                Pair(
                    1_000,
                    Snapshot(
                        1,
                        100 + empiricalLimit + 1,
                        100 + empiricalLimit + 1,
                        100 + empiricalLimit + 1,
                        TimeSpan.Zero)))
            .RunAsync(Scenario(O5R5MeasurementPhase.FirstByte, accountedMemoryBytes: reserved));

        O5R5MeasurementResult workAtLimit = await Runner(
                protocol,
                Pair(
                    1_000_000,
                    Snapshot(100, 100, 100, 100, TimeSpan.FromTicks(1_000))))
            .RunAsync(Scenario(O5R5MeasurementPhase.Parse, declaredWorkUnits: 1));
        O5R5MeasurementResult workAboveLimit = await Runner(
                protocol,
                Pair(
                    1_000_000,
                    Snapshot(101, 100, 100, 100, TimeSpan.FromTicks(1_010))))
            .RunAsync(Scenario(O5R5MeasurementPhase.Parse, declaredWorkUnits: 1));

        Assert.True(elapsedAtLimit.Sample!.Passed);
        Assert.False(elapsedAboveLimit.Sample!.Passed);
        Assert.True(memoryAtLimit.Sample!.Passed);
        Assert.False(memoryAboveLimit.Sample!.Passed);
        Assert.True(workAtLimit.Sample!.Passed);
        Assert.False(workAboveLimit.Sample!.Passed);
        Assert.Equal("o5r5a.measurement.threshold_exceeded", elapsedAboveLimit.Code);
        Assert.Equal("o5r5a.measurement.threshold_exceeded", memoryAboveLimit.Code);
        Assert.Equal("o5r5a.measurement.threshold_exceeded", workAboveLimit.Code);
    }

    /// <summary>Proves deterministic work admission accepts maximum minus one and maximum, then refuses maximum plus one before capture.</summary>
    [Fact]
    public async Task DeterministicWorkAdmissionFailsBeforeExecutionAtMaximumPlusOne()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        SyntheticMeasurementSource belowSource = Pair(
            1_000,
            Snapshot(1, 100, 100, 100, TimeSpan.Zero));
        SyntheticMeasurementSource limitSource = Pair(
            1_000,
            Snapshot(1, 100, 100, 100, TimeSpan.Zero));
        SyntheticMeasurementSource aboveSource = new(1_000);
        int aboveExecutions = 0;

        O5R5MeasurementResult below = await Runner(protocol, belowSource).RunAsync(
            Scenario(
                O5R5MeasurementPhase.FirstByte,
                declaredWorkUnits: protocol.Envelope.MaximumWorkUnits - 1));
        O5R5MeasurementResult limit = await Runner(protocol, limitSource).RunAsync(
            Scenario(
                O5R5MeasurementPhase.FirstByte,
                declaredWorkUnits: protocol.Envelope.MaximumWorkUnits));
        O5R5MeasurementResult above = await Runner(protocol, aboveSource).RunAsync(
            Scenario(
                O5R5MeasurementPhase.FirstByte,
                declaredWorkUnits: protocol.Envelope.MaximumWorkUnits + 1,
                operation: (_, _) =>
                {
                    aboveExecutions++;
                    return ValueTask.CompletedTask;
                }));

        Assert.True(below.Sample!.Passed);
        Assert.True(limit.Sample!.Passed);
        Assert.Equal(O5R5MeasurementDisposition.Refused, above.Disposition);
        Assert.Equal("o5r5a.scenario.work_refused", above.Code);
        Assert.Equal(0, aboveSource.CaptureCount);
        Assert.Equal(0, aboveExecutions);
    }

    /// <summary>Proves cancellation, serial saturation and checkpoint exhaustion retain no partial sample.</summary>
    [Fact]
    public async Task CancellationSaturationAndCheckpointCapacityFailClosed()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        SyntheticMeasurementSource blockingSource = new(
            1_000,
            Snapshot(0, 100, 100, 100, TimeSpan.Zero));
        O5R5MeasurementReadinessRunner runner = Runner(protocol, blockingSource);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource cancellation = new();
        Task<O5R5MeasurementResult> active = runner.RunAsync(
                Scenario(
                    O5R5MeasurementPhase.Cancellation,
                    operation: async (_, token) =>
                    {
                        entered.SetResult();
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    }),
                cancellation.Token)
            .AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        O5R5MeasurementResult saturated = await runner.RunAsync(
            Scenario(O5R5MeasurementPhase.ControlUpdate));
        cancellation.Cancel();
        O5R5MeasurementResult cancelled = await active.WaitAsync(TimeSpan.FromSeconds(5));

        SyntheticMeasurementSource checkpointSource = new(
            1_000,
            Enumerable
                .Range(0, O5R5MeasurementProtocol.MaximumCheckpoints + 1)
                .Select(index => Snapshot(index, 100, 100 + index, 100, TimeSpan.Zero))
                .ToArray());
        O5R5MeasurementResult checkpointCapacity = await Runner(protocol, checkpointSource).RunAsync(
            Scenario(
                O5R5MeasurementPhase.Parse,
                operation: (context, _) =>
                {
                    for (int index = 0; index <= O5R5MeasurementProtocol.MaximumCheckpoints; index++)
                    {
                        context.CaptureCheckpoint();
                    }

                    return ValueTask.CompletedTask;
                }));

        Assert.Equal("o5r5a.runner.saturated", saturated.Code);
        Assert.Null(saturated.Sample);
        Assert.Equal("o5r5a.runner.cancelled", cancelled.Code);
        Assert.Null(cancelled.Sample);
        Assert.Equal("o5r5a.measurement.checkpoint_capacity", checkpointCapacity.Code);
        Assert.Null(checkpointCapacity.Sample);
        Assert.False(O5R5MeasurementResult.MayEvaluate);
        Assert.False(O5R5MeasurementResult.MayPublish);
    }

    /// <summary>Proves protocol mismatch, counter rollback and source failure return stable fail-closed outcomes.</summary>
    [Fact]
    public async Task ProtocolAndCounterIntegrityFailuresFailClosed()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        O5R5PhaseLimit[] alteredLimits = protocol.PhaseLimits.Values
            .Select(
                limit => limit.Phase == O5R5MeasurementPhase.FirstByte
                    ? limit with { MaximumElapsed = limit.MaximumElapsed - TimeSpan.FromMilliseconds(1) }
                    : limit)
            .ToArray();
        O5R5MeasurementProtocol altered = new(protocol.Envelope, alteredLimits);

        InvalidOperationException markerFailure = Assert.Throws<InvalidOperationException>(
            () => new O5R5MeasurementReadinessRunner("wrong", protocol, Pair(1_000)));
        InvalidOperationException protocolFailure = Assert.Throws<InvalidOperationException>(
            () => new O5R5MeasurementReadinessRunner(
                O5R5MeasurementReadinessRunner.Marker,
                altered,
                Pair(1_000)));
        O5R5MeasurementResult rollback = await Runner(
                protocol,
                new SyntheticMeasurementSource(
                    1_000,
                    Snapshot(1, 100, 100, 100, TimeSpan.Zero),
                    Snapshot(2, 100, 99, 100, TimeSpan.Zero)))
            .RunAsync(Scenario(O5R5MeasurementPhase.FirstByte));
        O5R5MeasurementResult unavailable = await Runner(
                protocol,
                new ThrowingMeasurementSource())
            .RunAsync(Scenario(O5R5MeasurementPhase.FirstByte));

        Assert.Equal("o5r5a.runner.marker_required", markerFailure.Message);
        Assert.Equal("o5r5a.runner.protocol_mismatch", protocolFailure.Message);
        Assert.Equal("o5r5a.measurement.metric_rollback", rollback.Code);
        Assert.Null(rollback.Sample);
        Assert.Equal("o5r5a.measurement.source_unavailable", unavailable.Code);
        Assert.Null(unavailable.Sample);
    }

    /// <summary>Proves fixed groups retain all outliers while isolated scheduler noise cannot dominate repeatability.</summary>
    [Fact]
    public void SummaryRequiresExactSequencesAndRetainsOutliers()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        O5R5MeasurementReadinessRunner runner = Runner(protocol, Pair(1_000));
        List<O5R5MeasurementSample> stable = Samples(protocol.Digest, _ => 10d);
        List<O5R5MeasurementSample> isolatedOutlier = Samples(
            protocol.Digest,
            repetition => repetition == O5R5MeasurementProtocol.MeasuredRepetitions
                ? 1_000d
                : 10d);
        List<O5R5MeasurementSample> variable = Samples(
            protocol.Digest,
            repetition =>
                ((repetition - 1) / O5R5MeasurementProtocol.SamplesPerRepeatabilityGroup) % 2 == 0
                    ? 10d
                    : 1_000d);

        O5R5MeasurementSummary accepted = runner.Summarise(stable);
        O5R5MeasurementSummary robust = runner.Summarise(isolatedOutlier);
        O5R5MeasurementSummary failed = runner.Summarise(variable);
        InvalidOperationException missing = Assert.Throws<InvalidOperationException>(
            () => runner.Summarise(stable.Skip(1).ToArray()));
        List<O5R5MeasurementSample> duplicate = [.. stable];
        duplicate[^1] = duplicate[^2];
        InvalidOperationException duplicated = Assert.Throws<InvalidOperationException>(
            () => runner.Summarise(duplicate));

        Assert.True(accepted.Passed);
        Assert.Equal("o5r5a.summary.accepted", accepted.Code);
        Assert.True(robust.Passed);
        Assert.Equal(1_000d, robust.Elapsed.Maximum);
        Assert.Equal(10d, robust.RepeatabilityElapsed.Maximum);
        Assert.False(O5R5MeasurementSummary.IsAuthorising);
        Assert.False(failed.Passed);
        Assert.Equal("o5r5a.summary.failed", failed.Code);
        Assert.Equal(1_000d, failed.Elapsed.Maximum);
        Assert.True(
            failed.RepeatabilityElapsed.CoefficientOfVariation >
            O5R5MeasurementProtocol.MaximumCoefficientOfVariation);
        Assert.Equal("o5r5a.summary.sample_count_invalid", missing.Message);
        Assert.Equal("o5r5a.summary.sequence_invalid", duplicated.Message);
    }

    /// <summary>Creates the exact marker-gated runner used by synthetic tests.</summary>
    /// <param name="protocol">Frozen protocol.</param>
    /// <param name="source">Synthetic measurement source.</param>
    /// <returns>An isolated runner.</returns>
    private static O5R5MeasurementReadinessRunner Runner(
        O5R5MeasurementProtocol protocol,
        IO5R5MeasurementSource source) =>
        new(O5R5MeasurementReadinessRunner.Marker, protocol, source);

    /// <summary>Creates one valid scenario with optional bounded overrides.</summary>
    /// <param name="phase">Exact phase.</param>
    /// <param name="declaredWorkUnits">Predeclared deterministic work.</param>
    /// <param name="accountedMemoryBytes">Predeclared memory reservation.</param>
    /// <param name="operation">Optional synthetic operation.</param>
    /// <returns>A valid first measured warm-temperature scenario.</returns>
    private static O5R5MeasurementScenario Scenario(
        O5R5MeasurementPhase phase,
        long declaredWorkUnits = 1,
        long accountedMemoryBytes = 524_288,
        Func<O5R5MeasurementContext, CancellationToken, ValueTask>? operation = null) =>
        new(
            phase,
            O5R5Temperature.Warm,
            false,
            1,
            declaredWorkUnits,
            accountedMemoryBytes,
            operation ?? ((_, _) => ValueTask.CompletedTask));

    /// <summary>Creates a source whose baseline is fixed and whose terminal snapshot is supplied.</summary>
    /// <param name="frequency">Synthetic monotonic frequency.</param>
    /// <param name="after">Terminal snapshot.</param>
    /// <returns>A two-capture synthetic source.</returns>
    private static SyntheticMeasurementSource Pair(
        long frequency,
        O5R5MetricSnapshot? after = null) =>
        new(
            frequency,
            Snapshot(0, 100, 100, 100, TimeSpan.Zero),
            after ?? Snapshot(1, 100, 100, 100, TimeSpan.Zero));

    /// <summary>Creates one synthetic metric snapshot.</summary>
    /// <param name="timestamp">Monotonic timestamp.</param>
    /// <param name="heap">Managed heap bytes.</param>
    /// <param name="allocated">Cumulative allocation bytes.</param>
    /// <param name="workingSet">Working-set bytes.</param>
    /// <param name="cpu">Cumulative CPU time.</param>
    /// <returns>A complete synthetic snapshot.</returns>
    private static O5R5MetricSnapshot Snapshot(
        long timestamp,
        long heap,
        long allocated,
        long workingSet,
        TimeSpan cpu) =>
        new(timestamp, heap, allocated, workingSet, cpu);

    /// <summary>Creates one exact summary batch from a predeclared elapsed-time sequence.</summary>
    /// <param name="digest">Frozen protocol digest.</param>
    /// <param name="elapsedForRepetition">Deterministic elapsed value for each measured repetition.</param>
    /// <returns>Five warm-ups and thirty measured samples.</returns>
    private static List<O5R5MeasurementSample> Samples(
        string digest,
        Func<int, double> elapsedForRepetition)
    {
        List<O5R5MeasurementSample> samples = [];
        for (int repetition = 1; repetition <= O5R5MeasurementProtocol.WarmUpRepetitions; repetition++)
        {
            samples.Add(Sample(digest, true, repetition, 10d));
        }

        for (int repetition = 1; repetition <= O5R5MeasurementProtocol.MeasuredRepetitions; repetition++)
        {
            samples.Add(
                Sample(
                    digest,
                    false,
                    repetition,
                    elapsedForRepetition(repetition)));
        }

        return samples;
    }

    /// <summary>Creates one complete accepted synthetic summary sample.</summary>
    /// <param name="digest">Frozen digest.</param>
    /// <param name="warmUp">Warm-up membership.</param>
    /// <param name="repetition">Exact repetition.</param>
    /// <param name="elapsed">Elapsed milliseconds.</param>
    /// <returns>One phase-labelled sample.</returns>
    private static O5R5MeasurementSample Sample(
        string digest,
        bool warmUp,
        int repetition,
        double elapsed) =>
        new(
            digest,
            O5R5MeasurementPhase.Parse,
            O5R5Temperature.Warm,
            warmUp,
            repetition,
            elapsed,
            5d,
            1,
            1,
            1,
            1,
            10,
            524_288,
            10d,
            10d,
            true);

    /// <summary>Returns queued synthetic counter snapshots and records capture count.</summary>
    private sealed class SyntheticMeasurementSource : IO5R5MeasurementSource
    {
        private readonly Queue<O5R5MetricSnapshot> snapshots;

        /// <summary>Initialises a finite synthetic source.</summary>
        /// <param name="frequency">Positive synthetic timestamp frequency.</param>
        /// <param name="snapshots">Exact queued snapshots.</param>
        internal SyntheticMeasurementSource(long frequency, params O5R5MetricSnapshot[] snapshots)
        {
            Frequency = frequency;
            this.snapshots = new Queue<O5R5MetricSnapshot>(snapshots);
        }

        /// <inheritdoc />
        public long Frequency { get; }

        /// <summary>Gets the number of requested synthetic captures.</summary>
        internal int CaptureCount { get; private set; }

        /// <inheritdoc />
        public O5R5MetricSnapshot Capture()
        {
            CaptureCount++;
            return snapshots.Count > 0
                ? snapshots.Dequeue()
                : throw new InvalidOperationException("synthetic counter queue exhausted");
        }
    }

    /// <summary>Represents an unavailable counter source without exposing a host error.</summary>
    private sealed class ThrowingMeasurementSource : IO5R5MeasurementSource
    {
        /// <inheritdoc />
        public long Frequency => 1_000;

        /// <inheritdoc />
        public O5R5MetricSnapshot Capture() =>
            throw new InvalidOperationException("synthetic source unavailable");
    }
}
