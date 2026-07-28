// Module purpose: Verifies D8 identity, exact prefix boundaries, schema, classification and isolated publication without physical execution.
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Protects the preregistered D8 contract and every admissible stopping boundary.</summary>
public sealed class O5R5ExactPrefixCompletionReconciliationTests
{
    private const string HostDigest = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    /// <summary>Verifies the frozen identity, dependencies, counts and permanently inactive boundary.</summary>
    [Fact]
    public void ProtocolIdentityIsStableAndNonAuthorising()
    {
        Assert.Equal(O5R5D8Protocol.Digest, O5R5D8Protocol.CalculateDigest());
        Assert.Equal(O5R5D8Protocol.V3Digest, O5R5MeasurementProtocol.CreateFrozen().Digest);
        Assert.Equal(O5R5D6Protocol.Digest, O5R5D8Protocol.D6Digest);
        Assert.Equal(2, O5R5D8Protocol.Runs);
        Assert.Equal(158, O5R5D8Protocol.ExpectedSampleCount);
        Assert.Equal(4, O5R5D8Protocol.ExpectedSummaryCount);
        Assert.Equal(786_432, O5R5D8Protocol.WorkingSetLimitBytes);
        Assert.Equal(0.20d, O5R5D8Protocol.RepeatabilityLimit);
        Assert.Contains("intraprocess-extra-captures=0", O5R5D8Protocol.CanonicalStatement);
        Assert.Contains("external=0", O5R5D8Protocol.CanonicalStatement);
        Assert.Contains("d5-controls=0", O5R5D8Protocol.CanonicalStatement);
    }

    /// <summary>Verifies all 158 scenarios and four summary boundaries preserve original V3 order.</summary>
    [Fact]
    public void ExactPrefixAndSummaryMembershipRemainUnchanged()
    {
        O5R5D6Runner selector = new(new UnusedMeasurementSource());

        IReadOnlyList<O5R5MeasurementScenario> scenarios = selector.SelectExactPrefix();
        O5R5D8Envelope envelope = O5R5D8Evidence.Finalise(
            CreateMeasured(1, TestBoundary.Complete),
            HostDigest);

        Assert.Equal(158, scenarios.Count);
        Assert.Equal(158, envelope.CompletedSampleCount);
        Assert.Equal(4, envelope.CompletedSummaryCount);
        Assert.Collection(
            envelope.Measured!.Summaries,
            item => Assert.Equal(
                (O5R5MeasurementPhase.FirstByte, O5R5Temperature.Cold),
                (item.Phase, item.Temperature)),
            item => Assert.Equal(
                (O5R5MeasurementPhase.FirstByte, O5R5Temperature.Warm),
                (item.Phase, item.Temperature)),
            item => Assert.Equal(
                (O5R5MeasurementPhase.Idle, O5R5Temperature.Cold),
                (item.Phase, item.Temperature)),
            item => Assert.Equal(
                (O5R5MeasurementPhase.Idle, O5R5Temperature.Warm),
                (item.Phase, item.Temperature)));
        Assert.Equal(
            Enumerable.Range(1, 13),
            scenarios
                .Where(
                    item =>
                        item.Phase == O5R5MeasurementPhase.Cancellation &&
                        !item.IsWarmUp)
                .Select(item => item.Repetition));
    }

    /// <summary>Verifies a complete report emits all four numeric count fields and round-trips exactly.</summary>
    [Fact]
    public void CompleteReportRoundTripsRealNumericCounts()
    {
        O5R5D8Envelope envelope = O5R5D8Evidence.Finalise(
            CreateMeasured(1, TestBoundary.Complete),
            HostDigest);

        byte[] bytes = O5R5D8EvidenceWriter.Serialise(envelope);
        using JsonDocument document = JsonDocument.Parse(bytes);
        JsonElement root = document.RootElement;
        O5R5D8Envelope retained = O5R5D8EvidenceWriter.Deserialise(bytes);

        Assert.Equal(JsonValueKind.Number, root.GetProperty("expectedSampleCount").ValueKind);
        Assert.Equal(158, root.GetProperty("expectedSampleCount").GetInt32());
        Assert.Equal(158, root.GetProperty("completedSampleCount").GetInt32());
        Assert.Equal(4, root.GetProperty("expectedSummaryCount").GetInt32());
        Assert.Equal(4, root.GetProperty("completedSummaryCount").GetInt32());
        Assert.True(retained.Admissible);
        Assert.True(retained.Complete);
        Assert.True(retained.TargetEligible);
        Assert.False(retained.Authorising);
        Assert.Equal(O5R5D8Disposition.CompletePrefix, retained.Disposition);
    }

    /// <summary>Verifies each of the four permitted pre-target summary stops is complete and count-consistent.</summary>
    /// <param name="completedGroups">Number of original V3 groups retained before the exact failure.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void EveryEarlySummaryBoundaryRoundTrips(int completedGroups)
    {
        O5R5D8Envelope envelope = O5R5D8Evidence.Finalise(
            CreateMeasured(1, TestBoundary.EarlyGate, completedGroups),
            HostDigest);

        O5R5D8Envelope retained = O5R5D8EvidenceWriter.Deserialise(
            O5R5D8EvidenceWriter.Serialise(envelope));

        Assert.True(retained.Admissible);
        Assert.False(retained.Complete);
        Assert.False(retained.TargetEligible);
        Assert.Equal(completedGroups * 35, retained.CompletedSampleCount);
        Assert.Equal(completedGroups, retained.CompletedSummaryCount);
        Assert.Equal(O5R5D8Disposition.EarlyGateStop, retained.Disposition);
        Assert.Single(retained.Measured!.Failures);
        Assert.Equal(
            O5R5ThresholdMetric.RepeatabilityCoefficient,
            retained.Measured.Failures[0].Metric);
        Assert.Equal(0.20d, retained.Measured.Failures[0].InclusiveLimit);
    }

    /// <summary>Verifies the exact target stop retains the crossing sample, four summaries and no later sample.</summary>
    [Fact]
    public void TargetStopRoundTripsTheCrossingCancellationSample()
    {
        O5R5D8Envelope envelope = O5R5D8Evidence.Finalise(
            CreateMeasured(1, TestBoundary.TargetStop),
            HostDigest);

        O5R5D8Envelope retained = O5R5D8EvidenceWriter.Deserialise(
            O5R5D8EvidenceWriter.Serialise(envelope));

        Assert.True(retained.Admissible);
        Assert.False(retained.Complete);
        Assert.True(retained.TargetEligible);
        Assert.True(retained.WorkingSetExcess);
        Assert.Equal(141, retained.CompletedSampleCount);
        Assert.Equal(4, retained.CompletedSummaryCount);
        O5R5MeasurementSample crossing = retained.Measured!.Samples[^1];
        Assert.Equal(O5R5MeasurementPhase.Cancellation, crossing.Phase);
        Assert.Equal(O5R5Temperature.Cold, crossing.Temperature);
        Assert.Equal(786_433, crossing.WorkingSetPeakDeltaBytes);
        Assert.Equal(O5R5D8Disposition.TargetStop, retained.Disposition);
    }

    /// <summary>Rejects every preregistered malformed root-count family and unknown fields.</summary>
    [Fact]
    public void RawSchemaRejectsMissingDuplicateMistypedAndInvalidCounts()
    {
        byte[] valid = O5R5D8EvidenceWriter.Serialise(
            O5R5D8Evidence.Finalise(CreateMeasured(1, TestBoundary.Complete), HostDigest));
        JsonObject root = JsonNode.Parse(valid)!.AsObject();

        AssertRejected(Mutate(root, node => node.Remove("expectedSummaryCount")));
        AssertRejected(
            Encoding.UTF8.GetBytes(
                Encoding.UTF8.GetString(valid).Replace(
                    "\"expectedSummaryCount\": 4,",
                    "\"expectedSummaryCount\": 4,\n  \"expectedSummaryCount\": 4,",
                    StringComparison.Ordinal)));
        AssertRejected(Mutate(root, node => node["completedSampleCount"] = "158"));
        AssertRejected(Mutate(root, node => node["completedSampleCount"] = -1));
        AssertRejected(Mutate(root, node => node["completedSummaryCount"] = 5));
        AssertRejected(Mutate(root, node => node["completedSampleCount"] = 157));
        AssertRejected(Mutate(root, node => node["unexpected"] = true));
    }

    /// <summary>Verifies all frozen decision-tree outcomes without accepting missing admissible attempts.</summary>
    [Fact]
    public void ClassificationFollowsTheFrozenDecisionTree()
    {
        Assert.Equal(
            "D8.BLOCKED_EVIDENCE",
            O5R5D8Evidence.Classify([O5R5D8Evidence.Blocked(1, HostDigest)]));
        Assert.Equal(
            "D8.WORKING_SET_NOT_REPRODUCED",
            O5R5D8Evidence.Classify(
            [
                Finalise(1, TestBoundary.Complete),
                Finalise(2, TestBoundary.Complete),
            ]));
        Assert.Equal(
            "D8.WORKING_SET_INTERMITTENT",
            O5R5D8Evidence.Classify(
            [
                Finalise(1, TestBoundary.TargetStop),
                Finalise(2, TestBoundary.Complete),
            ]));
        Assert.Equal(
            "D8.WORKING_SET_REPEATED",
            O5R5D8Evidence.Classify(
            [
                Finalise(1, TestBoundary.TargetStop),
                Finalise(2, TestBoundary.TargetStop),
            ]));
        Assert.Equal(
            "D8.EARLY_GATE_INTERMITTENT",
            O5R5D8Evidence.Classify(
            [
                Finalise(1, TestBoundary.EarlyGate, 1),
                Finalise(2, TestBoundary.Complete),
            ]));
        Assert.Equal(
            "D8.EARLY_GATE_REPEATED",
            O5R5D8Evidence.Classify(
            [
                Finalise(1, TestBoundary.EarlyGate, 1),
                Finalise(2, TestBoundary.EarlyGate, 1),
            ]));
        Assert.Equal(
            "D8.EARLY_GATE_DIVERGENT",
            O5R5D8Evidence.Classify(
            [
                Finalise(1, TestBoundary.EarlyGate, 1),
                Finalise(2, TestBoundary.EarlyGate, 2),
            ]));
        Assert.Equal(
            "D8.PREFIX_BLOCKED_OTHER_V3",
            O5R5D8Evidence.Classify(
            [
                Finalise(1, TestBoundary.OtherV3),
                Finalise(2, TestBoundary.Complete),
            ]));
        Assert.Throws<InvalidOperationException>(
            () => O5R5D8Evidence.Classify([Finalise(1, TestBoundary.Complete)]));
    }

    /// <summary>Verifies strict temporary destinations, atomic publication and temporary-sibling cleanup.</summary>
    [Fact]
    public async Task WriterRestrictsAndAtomicallyPublishesFinalEvidence()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"DBNotifier-PF-OBS-1-D8-{Guid.NewGuid():N}");
        string destination = Path.Combine(root, "unobserved-run-1.json");
        O5R5D8Envelope envelope = Finalise(1, TestBoundary.Complete);

        try
        {
            Assert.Equal(
                Path.GetFullPath(destination),
                O5R5D8EvidenceWriter.ValidateFinalDestination(destination, 1));
            Assert.Throws<InvalidOperationException>(
                () => O5R5D8EvidenceWriter.ValidateFinalDestination(
                    Path.Combine(root, "wrong.json"),
                    1));
            Assert.Throws<InvalidOperationException>(
                () => O5R5D8EvidenceWriter.ValidateFinalDestination(
                    Path.Combine(root, "nested", "unobserved-run-1.json"),
                    1));

            await O5R5D8EvidenceWriter.WriteFinalAsync(
                destination,
                envelope,
                CancellationToken.None);

            Assert.True(File.Exists(destination));
            Assert.False(File.Exists(destination + ".tmp"));
            Assert.False(File.Exists(O5R5D8EvidenceWriter.MeasuredDestination(destination)));
            O5R5D8Envelope retained = O5R5D8EvidenceWriter.ReadFinal(destination);
            Assert.Equal(
                O5R5D8EvidenceWriter.Serialise(envelope),
                O5R5D8EvidenceWriter.Serialise(retained));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    /// <summary>Verifies exact bounded D8 arguments and rejects extra, invalid or non-positive values.</summary>
    [Fact]
    public void ArgumentsAreExactAndBounded()
    {
        string[] valid =
        [
            "--activation",
            O5R5D8SupervisorProcess.ActivationMarker,
            "--output",
            Path.Combine(Path.GetTempPath(), "DBNotifier-PF-OBS-1-D8-test", "unobserved-run-1.json"),
            "--run",
            "1",
            "--sdk",
            "10.0.302",
            "--installed-memory",
            "1",
        ];

        Assert.True(
            O5R5D8Arguments.TryParse(
                valid,
                O5R5D8SupervisorProcess.ActivationMarker,
                out O5R5D8Arguments? parsed));
        Assert.Equal(1, parsed.Run);
        Assert.False(
            O5R5D8Arguments.TryParse(
                [.. valid, "extra"],
                O5R5D8SupervisorProcess.ActivationMarker,
                out _));
        string[] invalidRun = [.. valid];
        invalidRun[5] = "3";
        Assert.False(
            O5R5D8Arguments.TryParse(
                invalidRun,
                O5R5D8SupervisorProcess.ActivationMarker,
                out _));
        string[] invalidMemory = [.. valid];
        invalidMemory[9] = "0";
        Assert.False(
            O5R5D8Arguments.TryParse(
                invalidMemory,
                O5R5D8SupervisorProcess.ActivationMarker,
                out _));
    }

    /// <summary>Asserts that one malformed raw JSON document is rejected by the production reader.</summary>
    /// <param name="bytes">Malformed D8 JSON.</param>
    private static void AssertRejected(byte[] bytes) =>
        Assert.ThrowsAny<Exception>(() => O5R5D8EvidenceWriter.Deserialise(bytes));

    /// <summary>Clones and mutates one root JSON object without changing the shared valid baseline.</summary>
    /// <param name="source">Valid root object.</param>
    /// <param name="mutation">Single malformed mutation.</param>
    /// <returns>Malformed UTF-8 JSON bytes.</returns>
    private static byte[] Mutate(JsonObject source, Action<JsonObject> mutation)
    {
        JsonObject clone = JsonNode.Parse(source.ToJsonString())!.AsObject();
        mutation(clone);
        return Encoding.UTF8.GetBytes(clone.ToJsonString());
    }

    /// <summary>Creates and finalises one synthetic but contract-exact D8 boundary.</summary>
    private static O5R5D8Envelope Finalise(
        int run,
        TestBoundary boundary,
        int completedGroups = 0) =>
        O5R5D8Evidence.Finalise(
            CreateMeasured(run, boundary, completedGroups),
            HostDigest);

    /// <summary>Creates one unchanged D6 report shape without running the physical measurement source.</summary>
    private static O5R5D6Report CreateMeasured(
        int run,
        TestBoundary boundary,
        int completedGroups = 0)
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        O5R5D6Runner selector = new(new UnusedMeasurementSource());
        O5R5MeasurementScenario[] scenarios = selector.SelectExactPrefix().ToArray();
        int sampleCount = boundary switch
        {
            TestBoundary.Complete => 158,
            TestBoundary.TargetStop => 141,
            TestBoundary.EarlyGate => completedGroups * 35,
            TestBoundary.OtherV3 => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(boundary)),
        };
        if (boundary == TestBoundary.EarlyGate && completedGroups is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(completedGroups));
        }

        List<O5R5MeasurementSample> samples = scenarios
            .Take(sampleCount)
            .Select(CreatePassingSample)
            .ToList();
        O5R5MeasurementReadinessRunner summariser = new(
            O5R5MeasurementReadinessRunner.Marker,
            protocol,
            new UnusedMeasurementSource());
        int fullGroups = Math.Min(sampleCount / 35, 4);
        List<O5R5MeasurementSummary> summaries = Enumerable
            .Range(0, fullGroups)
            .Select(
                group => summariser.Summarise(
                    samples.Skip(group * 35).Take(35).ToArray()))
            .ToList();
        List<O5R5ThresholdFailure> failures = [];
        string code = "o5r5d6.prefix.complete";
        bool workingSetExcess = false;

        if (boundary == TestBoundary.TargetStop)
        {
            O5R5MeasurementSample crossing = samples[^1] with
            {
                WorkingSetPeakDeltaBytes = 786_433,
                Passed = false,
            };
            samples[^1] = crossing;
            O5R5ThresholdFailure failure = new(
                "o5r5d1.threshold.working-set-peak",
                crossing.Phase,
                crossing.Temperature,
                crossing.IsWarmUp,
                crossing.Repetition,
                O5R5ThresholdMetric.WorkingSetPeak,
                786_433,
                786_432,
                O5R5ThresholdUnit.Bytes);
            failures.Add(failure);
            code = failure.Code;
            workingSetExcess = true;
        }
        else if (boundary == TestBoundary.EarlyGate)
        {
            O5R5MeasurementSummary accepted = summaries[^1];
            O5R5MetricDistribution repeatability = accepted.RepeatabilityElapsed with
            {
                CoefficientOfVariation = 0.21d,
            };
            O5R5ThresholdFailure failure = new(
                "o5r5d1.threshold.repeatability-coefficient",
                accepted.Phase,
                accepted.Temperature,
                null,
                null,
                O5R5ThresholdMetric.RepeatabilityCoefficient,
                0.21d,
                0.20d,
                O5R5ThresholdUnit.Ratio);
            summaries[^1] = accepted with
            {
                Code = "o5r5a.summary.failed",
                RepeatabilityElapsed = repeatability,
                Failures = [failure],
                Passed = false,
            };
            failures.Add(failure);
            code = failure.Code;
        }
        else if (boundary == TestBoundary.OtherV3)
        {
            O5R5MeasurementSample stopped = samples[^1] with
            {
                ElapsedMilliseconds = 101d,
                Passed = false,
            };
            samples[^1] = stopped;
            O5R5ThresholdFailure failure = new(
                "o5r5d1.threshold.elapsed",
                stopped.Phase,
                stopped.Temperature,
                stopped.IsWarmUp,
                stopped.Repetition,
                O5R5ThresholdMetric.ElapsedTime,
                101d,
                100d,
                O5R5ThresholdUnit.Milliseconds);
            failures.Add(failure);
            code = failure.Code;
        }

        return new O5R5D6Report(
            O5R5D6Protocol.Version,
            O5R5D6Protocol.Digest,
            O5R5D6Protocol.V3Digest,
            "None",
            run,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddSeconds(1),
            TestEnvironment(),
            O5R5D8Protocol.ExpectedSampleCount,
            samples.Count,
            code,
            samples.Count == O5R5D8Protocol.ExpectedSampleCount &&
                summaries.Count == O5R5D8Protocol.ExpectedSummaryCount,
            workingSetExcess,
            samples,
            summaries,
            failures);
    }

    /// <summary>Creates one passing sample that preserves every scenario identity field.</summary>
    private static O5R5MeasurementSample CreatePassingSample(O5R5MeasurementScenario scenario)
    {
        bool firstByte = scenario.Phase == O5R5MeasurementPhase.FirstByte;
        bool workRate = O5R5MeasurementProtocol.MeasuresDeterministicWorkRate(scenario.Phase);
        return new O5R5MeasurementSample(
            O5R5D8Protocol.V3Digest,
            scenario.Phase,
            scenario.Temperature,
            scenario.IsWarmUp,
            scenario.Repetition,
            1d,
            0.5d,
            1,
            1,
            1,
            1,
            scenario.DeclaredWorkUnits,
            scenario.AccountedMemoryBytes,
            workRate ? 1d : null,
            workRate ? 1d : null,
            firstByte ? 0.5d : null,
            firstByte ? 0.5d : null,
            Passed: true);
    }

    /// <summary>Returns one fully sanitised fixed environment declaration for schema tests.</summary>
    private static O5R5PhysicalEnvironment TestEnvironment() =>
        new(
            "test-only",
            "X64",
            "X64",
            ".NET test-only",
            "10.0.302",
            1,
            1,
            1);

    /// <summary>Identifies synthetic structural boundaries used only by D8 contract regressions.</summary>
    private enum TestBoundary
    {
        Complete = 1,
        TargetStop = 2,
        EarlyGate = 3,
        OtherV3 = 4,
    }

    /// <summary>Provides no metrics because structural regressions must remain non-physical.</summary>
    private sealed class UnusedMeasurementSource : IO5R5MeasurementSource
    {
        /// <inheritdoc />
        public long Frequency => 1;

        /// <inheritdoc />
        public O5R5MetricSnapshot Capture() =>
            throw new InvalidOperationException("o5r5d8.test.source_unused");
    }
}
