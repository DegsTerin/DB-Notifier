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
        "60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A";

    /// <summary>Proves every physical threshold and statistical rule was frozen before synthetic execution.</summary>
    [Fact]
    public void FrozenProtocolHasExactEnvelopeThresholdsAndDigest()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        Assert.Equal(ExpectedProtocolDigest, protocol.Digest);
        Assert.Equal("pfobs1-physical-measurement-3.0.0", O5R5MeasurementProtocol.Version);
        Assert.Equal(O1ResourceEnvelope.Fixture(), protocol.Envelope);
        Assert.Equal(5, O5R5MeasurementProtocol.WarmUpRepetitions);
        Assert.Equal(30, O5R5MeasurementProtocol.MeasuredRepetitions);
        Assert.Equal(5, O5R5MeasurementProtocol.RepeatabilityGroupCount);
        Assert.Equal(6, O5R5MeasurementProtocol.SamplesPerRepeatabilityGroup);
        Assert.Equal(2, O5R5MeasurementProtocol.RequiredConsecutiveCampaigns);
        Assert.Equal(100_000, O5R5MeasurementProtocol.FirstByteRepeatabilityWorkUnits);
        Assert.Equal(64, O5R5MeasurementProtocol.MaximumCheckpoints);
        Assert.Equal(0.20d, O5R5MeasurementProtocol.MaximumCoefficientOfVariation);
        Assert.Equal(100d, O5R5MeasurementProtocol.MaximumElapsedMicrosecondsPerWorkUnit);
        Assert.Equal(100d, O5R5MeasurementProtocol.MaximumCpuMicrosecondsPerWorkUnit);
        Assert.Equal(786_432, protocol.EmpiricalMemoryLimit(524_288));
        Assert.Equal(8, protocol.PhaseLimits.Count);
        Assert.Equal(
            TimeSpan.FromMilliseconds(2_250),
            protocol.LimitFor(O5R5MeasurementPhase.FirstByte).MaximumElapsed);
        Assert.True(protocol.LimitFor(O5R5MeasurementPhase.FirstByte).MeasuresWorkRate);
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
            SyntheticMeasurementSource source = phase == O5R5MeasurementPhase.FirstByte
                ? FirstByteTriplet(
                    frequency: 1_000,
                    checkpoint: Snapshot(1, 100, 100, 100, TimeSpan.Zero),
                    after: Snapshot(2, 101, 101, 101, TimeSpan.FromMilliseconds(1)))
                : Pair(
                    frequency: 1_000,
                    after: Snapshot(1, 101, 101, 101, TimeSpan.FromMilliseconds(1)));
            O5R5MeasurementReadinessRunner runner = Runner(protocol, source);

            O5R5MeasurementResult result = await runner.RunAsync(
                Scenario(
                    phase,
                    declaredWorkUnits: phase == O5R5MeasurementPhase.FirstByte
                        ? O5R5MeasurementProtocol.FirstByteRepeatabilityWorkUnits
                        : 10));

            Assert.Equal(O5R5MeasurementDisposition.Accepted, result.Disposition);
            Assert.Equal("o5r5a.measurement.accepted", result.Code);
            Assert.NotNull(result.Sample);
            Assert.Equal(phase, result.Sample.Phase);
            Assert.Equal(ExpectedProtocolDigest, result.Sample.ProtocolDigest);
            Assert.True(result.Sample.Passed);
            Assert.False(O5R5MeasurementResult.MayEvaluate);
            Assert.False(O5R5MeasurementResult.MayPublish);
            Assert.Equal(phase == O5R5MeasurementPhase.FirstByte ? 3 : 2, source.CaptureCount);
            if (O5R5MeasurementProtocol.MeasuresDeterministicWorkRate(phase))
            {
                double expectedElapsed = phase == O5R5MeasurementPhase.FirstByte ? 0.01d : 100d;
                double expectedCpu = phase == O5R5MeasurementPhase.FirstByte ? 0.01d : 100d;
                Assert.Equal(expectedElapsed, result.Sample.ElapsedMicrosecondsPerWorkUnit);
                Assert.Equal(expectedCpu, result.Sample.CpuMicrosecondsPerWorkUnit);
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
            .RunAsync(Scenario(O5R5MeasurementPhase.Idle));
        O5R5MeasurementResult elapsedAboveLimit = await Runner(
                protocol,
                Pair(
                    1_000,
                    Snapshot(2_251, 100, 100, 100, TimeSpan.Zero)))
            .RunAsync(Scenario(O5R5MeasurementPhase.Idle));

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
            .RunAsync(Scenario(O5R5MeasurementPhase.Idle, accountedMemoryBytes: reserved));
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
            .RunAsync(Scenario(O5R5MeasurementPhase.Idle, accountedMemoryBytes: reserved));

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
        Assert.Equal("o5r5d1.threshold.elapsed", elapsedAboveLimit.Code);
        Assert.Equal("o5r5d1.threshold.heap-peak", memoryAboveLimit.Code);
        Assert.Equal("o5r5d1.threshold.elapsed-per-work-unit", workAboveLimit.Code);
        O5R5ThresholdFailure elapsedFailure = Assert.Single(elapsedAboveLimit.Failures);
        Assert.Equal(O5R5MeasurementPhase.Idle, elapsedFailure.Phase);
        Assert.Equal(O5R5Temperature.Warm, elapsedFailure.Temperature);
        Assert.False(elapsedFailure.IsWarmUp);
        Assert.Equal(1, elapsedFailure.Repetition);
        Assert.Equal(O5R5ThresholdMetric.ElapsedTime, elapsedFailure.Metric);
        Assert.Equal(2_251d, elapsedFailure.Observed);
        Assert.Equal(2_250d, elapsedFailure.InclusiveLimit);
        Assert.Equal(O5R5ThresholdUnit.Milliseconds, elapsedFailure.Unit);
        Assert.Equal(
            [
                O5R5ThresholdMetric.ManagedHeapPeak,
                O5R5ThresholdMetric.WorkingSetPeak,
                O5R5ThresholdMetric.AllocationPeak,
            ],
            memoryAboveLimit.Failures.Select(failure => failure.Metric));
        Assert.Equal(
            [
                O5R5ThresholdMetric.ElapsedPerWorkUnit,
                O5R5ThresholdMetric.CpuPerWorkUnit,
            ],
            workAboveLimit.Failures.Select(failure => failure.Metric));
    }

    /// <summary>
    /// Proves the unchanged Cancellation working-set ceiling accepts the inclusive limit and
    /// rejects one additional byte without publishing partial authority.
    /// </summary>
    [Fact]
    public async Task CancellationWorkingSetBoundaryRemainsFailClosedAtPlusOne()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        long limit = protocol.EmpiricalMemoryLimit(
            protocol.Envelope.MaximumAccountedMemoryBytes);
        O5R5MeasurementResult accepted = await Runner(
                protocol,
                Pair(
                    1_000,
                    Snapshot(
                        1,
                        100,
                        100,
                        100 + limit,
                        TimeSpan.Zero)))
            .RunAsync(Scenario(O5R5MeasurementPhase.Cancellation));
        O5R5MeasurementResult refused = await Runner(
                protocol,
                Pair(
                    1_000,
                    Snapshot(
                        1,
                        100,
                        100,
                        100 + limit + 1,
                        TimeSpan.Zero)))
            .RunAsync(Scenario(O5R5MeasurementPhase.Cancellation));

        Assert.True(accepted.Sample!.Passed);
        Assert.False(refused.Sample!.Passed);
        O5R5ThresholdFailure failure = Assert.Single(refused.Failures);
        Assert.Equal("o5r5d1.threshold.working-set-peak", failure.Code);
        Assert.Equal(O5R5MeasurementPhase.Cancellation, failure.Phase);
        Assert.Equal(O5R5ThresholdMetric.WorkingSetPeak, failure.Metric);
        Assert.Equal(limit + 1d, failure.Observed);
        Assert.Equal(limit, failure.InclusiveLimit);
        Assert.False(O5R5MeasurementResult.MayEvaluate);
        Assert.False(O5R5MeasurementResult.MayPublish);
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
                O5R5MeasurementPhase.Parse,
                declaredWorkUnits: protocol.Envelope.MaximumWorkUnits - 1));
        O5R5MeasurementResult limit = await Runner(protocol, limitSource).RunAsync(
            Scenario(
                O5R5MeasurementPhase.Parse,
                declaredWorkUnits: protocol.Envelope.MaximumWorkUnits));
        O5R5MeasurementResult above = await Runner(protocol, aboveSource).RunAsync(
            Scenario(
                O5R5MeasurementPhase.Parse,
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
            .RunAsync(Scenario(O5R5MeasurementPhase.Idle));
        O5R5MeasurementResult unavailable = await Runner(
                protocol,
                new ThrowingMeasurementSource())
            .RunAsync(Scenario(O5R5MeasurementPhase.Idle));

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
        O5R5ThresholdFailure repeatabilityFailure = Assert.Single(failed.Failures);
        Assert.Equal(
            "o5r5d1.threshold.repeatability-coefficient",
            repeatabilityFailure.Code);
        Assert.Equal(O5R5ThresholdMetric.RepeatabilityCoefficient, repeatabilityFailure.Metric);
        Assert.Null(repeatabilityFailure.IsWarmUp);
        Assert.Null(repeatabilityFailure.Repetition);
        Assert.Equal(O5R5ThresholdUnit.Ratio, repeatabilityFailure.Unit);
        Assert.Equal(1_000d, failed.Elapsed.Maximum);
        Assert.True(
            failed.RepeatabilityElapsed.CoefficientOfVariation >
            O5R5MeasurementProtocol.MaximumCoefficientOfVariation);
        Assert.Equal("o5r5a.summary.sample_count_invalid", missing.Message);
        Assert.Equal("o5r5a.summary.sequence_invalid", duplicated.Message);
    }

    /// <summary>
    /// Proves the retained v2 FirstByte/Cold failure came from applying relative repeatability to
    /// the sub-millisecond whole operation, while v3 retains that absolute latency separately and
    /// evaluates every predeclared fixed-window observation.
    /// </summary>
    [Fact]
    public void FirstByteV3SeparatesAbsoluteLatencyFromFixedWindowRepeatability()
    {
        double[] retainedV2ElapsedMilliseconds =
        [
            0.0658d, 0.0679d, 0.0960d, 0.0343d, 0.0604d, 0.0382d,
            0.0354d, 0.0337d, 0.0322d, 0.0327d, 0.0420d, 0.0344d,
            0.0316d, 0.0343d, 0.0340d, 0.0331d, 0.0437d, 0.0390d,
            0.0381d, 0.0320d, 0.0323d, 0.0313d, 0.0344d, 0.0350d,
            0.0362d, 0.0395d, 0.0326d, 0.0324d, 0.0336d, 0.0343d,
        ];
        double[] retainedGroupMedians = FixedGroupMedians(retainedV2ElapsedMilliseconds);
        double retainedCoefficient = CoefficientOfVariation(retainedGroupMedians);
        Assert.All(retainedV2ElapsedMilliseconds, value => Assert.InRange(value, 0d, 0.1d));
        Assert.Equal(
            [0.0631d, 0.03405d, 0.03415d, 0.03335d, 0.03395d],
            retainedGroupMedians,
            (expected, actual) => Math.Abs(expected - actual) < 0.0000000001d);
        Assert.Equal(0.2943936135230859d, retainedCoefficient, precision: 15);
        Assert.True(
            retainedGroupMedians[0] >
            retainedGroupMedians.Skip(1).Max() * 1.8d);
        Assert.True(retainedCoefficient > O5R5MeasurementProtocol.MaximumCoefficientOfVariation);

        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        O5R5MeasurementReadinessRunner runner = Runner(protocol, Pair(1_000));
        O5R5MeasurementSummary separated = runner.Summarise(
            FirstByteSamples(
                protocol.Digest,
                repetition => retainedV2ElapsedMilliseconds[repetition - 1],
                _ => 1d));
        O5R5MeasurementSummary unstableWindow = runner.Summarise(
            FirstByteSamples(
                protocol.Digest,
                repetition => retainedV2ElapsedMilliseconds[repetition - 1],
                repetition => ((repetition - 1) / 6) % 2 == 0 ? 1d : 10d));

        Assert.True(separated.Passed);
        Assert.Equal(O5R5RepeatabilityBasis.FirstByteFixedWindowElapsed, separated.RepeatabilityBasis);
        Assert.NotNull(separated.FirstObservation);
        Assert.True(separated.FirstObservation.CoefficientOfVariation > 0.3d);
        Assert.Equal(0d, separated.RepeatabilityElapsed.CoefficientOfVariation);
        Assert.Equal(1.096d, separated.Elapsed.Maximum, precision: 3);
        Assert.False(unstableWindow.Passed);
        Assert.Equal(
            "o5r5d1.threshold.repeatability-coefficient",
            Assert.Single(unstableWindow.Failures).Code);
    }

    /// <summary>Proves FirstByte v3 refuses an absent or ambiguous first-observation checkpoint.</summary>
    [Fact]
    public async Task FirstByteRequiresExactlyOneFirstObservationCheckpoint()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        SyntheticMeasurementSource wrongWindowSource = new(1_000);
        O5R5MeasurementResult wrongWindow = await Runner(protocol, wrongWindowSource).RunAsync(
            Scenario(
                O5R5MeasurementPhase.FirstByte,
                declaredWorkUnits:
                    O5R5MeasurementProtocol.FirstByteRepeatabilityWorkUnits - 1));
        O5R5MeasurementResult missing = await Runner(protocol, Pair(1_000)).RunAsync(
            Scenario(
                O5R5MeasurementPhase.FirstByte,
                operation: (_, _) => ValueTask.CompletedTask));
        SyntheticMeasurementSource multipleSource = new(
            1_000,
            Snapshot(0, 100, 100, 100, TimeSpan.Zero),
            Snapshot(1, 100, 100, 100, TimeSpan.Zero),
            Snapshot(2, 100, 100, 100, TimeSpan.Zero),
            Snapshot(3, 100, 100, 100, TimeSpan.Zero));
        O5R5MeasurementResult multiple = await Runner(protocol, multipleSource).RunAsync(
            Scenario(
                O5R5MeasurementPhase.FirstByte,
                operation: (context, _) =>
                {
                    context.CaptureCheckpoint();
                    context.CaptureCheckpoint();
                    return ValueTask.CompletedTask;
                }));

        Assert.Equal("o5r5d2.first-byte.work_window_invalid", wrongWindow.Code);
        Assert.Equal(0, wrongWindowSource.CaptureCount);
        Assert.Equal("o5r5d2.first-byte.checkpoint_invalid", missing.Code);
        Assert.Null(missing.Sample);
        Assert.Equal("o5r5d2.first-byte.checkpoint_invalid", multiple.Code);
        Assert.Null(multiple.Sample);
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
        long? declaredWorkUnits = null,
        long accountedMemoryBytes = 524_288,
        Func<O5R5MeasurementContext, CancellationToken, ValueTask>? operation = null) =>
        new(
            phase,
            O5R5Temperature.Warm,
            false,
            1,
            declaredWorkUnits ??
                (phase == O5R5MeasurementPhase.FirstByte
                    ? O5R5MeasurementProtocol.FirstByteRepeatabilityWorkUnits
                    : 1),
            accountedMemoryBytes,
            operation ??
                (phase == O5R5MeasurementPhase.FirstByte
                    ? (context, _) =>
                    {
                        context.CaptureCheckpoint();
                        return ValueTask.CompletedTask;
                    }
    : (_, _) => ValueTask.CompletedTask));

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

    /// <summary>Creates the exact baseline, first-observation and terminal sequence required by FirstByte v3.</summary>
    /// <param name="frequency">Synthetic monotonic frequency.</param>
    /// <param name="checkpoint">Snapshot captured immediately after the first observation.</param>
    /// <param name="after">Terminal snapshot after the fixed work window.</param>
    /// <returns>A three-capture synthetic source.</returns>
    private static SyntheticMeasurementSource FirstByteTriplet(
        long frequency,
        O5R5MetricSnapshot checkpoint,
        O5R5MetricSnapshot after) =>
        new(
            frequency,
            Snapshot(0, 100, 100, 100, TimeSpan.Zero),
            checkpoint,
            after);

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

    /// <summary>Creates a complete FirstByte v3 batch without dropping or replacing any observation.</summary>
    /// <param name="digest">Frozen v3 digest.</param>
    /// <param name="firstObservation">Absolute first-observation latency per measured repetition.</param>
    /// <param name="fixedWindow">Fixed-work-window duration per measured repetition.</param>
    /// <returns>Five warm-ups and thirty measured FirstByte samples.</returns>
    private static List<O5R5MeasurementSample> FirstByteSamples(
        string digest,
        Func<int, double> firstObservation,
        Func<int, double> fixedWindow)
    {
        List<O5R5MeasurementSample> samples = [];
        for (int repetition = 1; repetition <= O5R5MeasurementProtocol.WarmUpRepetitions; repetition++)
        {
            samples.Add(FirstByteSample(digest, true, repetition, 0.05d, 1d));
        }

        for (int repetition = 1; repetition <= O5R5MeasurementProtocol.MeasuredRepetitions; repetition++)
        {
            samples.Add(
                FirstByteSample(
                    digest,
                    false,
                    repetition,
                    firstObservation(repetition),
                    fixedWindow(repetition)));
        }

        return samples;
    }

    /// <summary>Creates one structurally complete FirstByte v3 sample.</summary>
    /// <param name="digest">Frozen v3 digest.</param>
    /// <param name="warmUp">Warm-up membership.</param>
    /// <param name="repetition">Exact one-based repetition.</param>
    /// <param name="firstObservation">Absolute first-observation latency.</param>
    /// <param name="fixedWindow">Post-observation fixed-window duration.</param>
    /// <returns>One complete non-authorising sample.</returns>
    private static O5R5MeasurementSample FirstByteSample(
        string digest,
        bool warmUp,
        int repetition,
        double firstObservation,
        double fixedWindow) =>
        new(
            digest,
            O5R5MeasurementPhase.FirstByte,
            O5R5Temperature.Cold,
            warmUp,
            repetition,
            firstObservation + fixedWindow,
            0d,
            1,
            1,
            1,
            1,
            O5R5MeasurementProtocol.FirstByteRepeatabilityWorkUnits,
            524_288,
            fixedWindow * 1_000d / O5R5MeasurementProtocol.FirstByteRepeatabilityWorkUnits,
            0d,
            firstObservation,
            fixedWindow,
            true);

    /// <summary>Calculates the five predeclared sequential-group medians used by v2 and v3.</summary>
    /// <param name="values">Exactly thirty raw values in execution order.</param>
    /// <returns>Five medians without sample selection.</returns>
    private static double[] FixedGroupMedians(double[] values) =>
        values
            .Chunk(O5R5MeasurementProtocol.SamplesPerRepeatabilityGroup)
            .Select(
                group =>
                {
                    double[] ordered = group.Order().ToArray();
                    return (ordered[2] + ordered[3]) / 2d;
                })
            .ToArray();

    /// <summary>Calculates the population coefficient of variation for finite positive evidence.</summary>
    /// <param name="values">Complete finite values.</param>
    /// <returns>Population standard deviation divided by the arithmetic mean.</returns>
    private static double CoefficientOfVariation(double[] values)
    {
        double mean = values.Average();
        double variance = values.Select(value => Math.Pow(value - mean, 2d)).Average();
        return Math.Sqrt(variance) / mean;
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
            null,
            null,
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
