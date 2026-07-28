// Module purpose: Proves D7-R1 identity, real JSON summary counts, typed validation, isolation and bounded classifications.
using System.Text;
using System.Text.Json;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Protects the independently identified D7-R1 correction without running physical work.</summary>
public sealed class O5R5FirstByteColdRepeatabilityRerunTests
{
    /// <summary>Verifies R1 identity, dependencies, predecessor hashes and non-authorising semantics.</summary>
    [Fact]
    public void ProtocolIdentityAndPredecessorsAreStable()
    {
        Assert.Equal(O5R5D7R1Protocol.Digest, O5R5D7R1Protocol.CalculateDigest());
        Assert.Equal(3, O5R5D7R1Protocol.PredecessorReportDigests.Count);
        Assert.Equal(3, O5R5D7R1Protocol.UnobservedRuns);
        Assert.Equal(2, O5R5D7R1Protocol.ExternalRuns);
        Assert.Equal(2, O5R5D7R1Protocol.ExternalFailureGate);
        Assert.Equal(O5R5D7Protocol.Digest, "7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF");
        Assert.Equal(O5R5D7Protocol.V3Digest, O5R5MeasurementProtocol.CreateFrozen().Digest);
        Assert.Equal(O5R5D7Protocol.D6Digest, O5R5D6Protocol.Digest);
    }

    /// <summary>Proves a complete envelope serialises both numeric counts and survives the production round trip.</summary>
    [Fact]
    public void CompleteEnvelopeSerialisesExactSummaryCountsAndRoundTrips()
    {
        O5R5D7R1Envelope envelope = O5R5D7R1Evidence.Finalise(
            CreateCompleteMeasured(),
            externalObservation: null);

        byte[] bytes = O5R5D7R1EvidenceWriter.Serialise(envelope);
        using JsonDocument document = JsonDocument.Parse(bytes);
        JsonElement root = document.RootElement;
        O5R5D7R1Envelope roundTrip = O5R5D7R1EvidenceWriter.Deserialise(bytes);

        Assert.Equal(JsonValueKind.Number, root.GetProperty("expectedSummaryCount").ValueKind);
        Assert.Equal(JsonValueKind.Number, root.GetProperty("completedSummaryCount").ValueKind);
        Assert.Equal(1, root.GetProperty("expectedSummaryCount").GetInt32());
        Assert.Equal(1, root.GetProperty("completedSummaryCount").GetInt32());
        Assert.Equal(35, roundTrip.CompletedSampleCount);
        Assert.True(roundTrip.Complete);
        Assert.False(roundTrip.Authorising);
        Assert.NotNull(roundTrip.Measured?.Summary);
    }

    /// <summary>Proves a blocked envelope serialises expected one, completed zero and cannot appear complete.</summary>
    [Fact]
    public void IncompleteEnvelopeSerialisesZeroCompletedSummaryAndRoundTrips()
    {
        O5R5D7R1Envelope envelope = O5R5D7R1Evidence.Blocked(
            O5R5D7Arm.Unobserved,
            1,
            measured: null,
            "o5r5d7r1.test.blocked");

        byte[] bytes = O5R5D7R1EvidenceWriter.Serialise(envelope);
        using JsonDocument document = JsonDocument.Parse(bytes);
        O5R5D7R1Envelope roundTrip = O5R5D7R1EvidenceWriter.Deserialise(bytes);

        Assert.Equal(1, document.RootElement.GetProperty("expectedSummaryCount").GetInt32());
        Assert.Equal(0, document.RootElement.GetProperty("completedSummaryCount").GetInt32());
        Assert.False(roundTrip.Complete);
        Assert.False(roundTrip.SummaryFailed);
        Assert.Null(roundTrip.Measured);
    }

    /// <summary>Proves a failed conditional external attempt can publish before snapshots exist.</summary>
    [Fact]
    public void BlockedExternalEnvelopeCanRetainTheAttemptWithoutInventingObservation()
    {
        O5R5D7R1Envelope envelope = O5R5D7R1Evidence.Blocked(
            O5R5D7Arm.External,
            1,
            measured: null,
            "o5r5d7r1.test.blocked");

        O5R5D7R1Envelope roundTrip = O5R5D7R1EvidenceWriter.Deserialise(
            O5R5D7R1EvidenceWriter.Serialise(envelope));

        Assert.False(roundTrip.Complete);
        Assert.Equal(O5R5D7Arm.External, roundTrip.Arm);
        Assert.Null(roundTrip.ExternalObservation);
    }

    /// <summary>Rejects missing, duplicate, mistyped, excessive and semantically inconsistent counts.</summary>
    [Fact]
    public void ProductionReaderRejectsEveryMalformedSummaryCountShape()
    {
        string valid = Encoding.UTF8.GetString(
            O5R5D7R1EvidenceWriter.Serialise(
                O5R5D7R1Evidence.Finalise(CreateCompleteMeasured(), null)));
        string[] malformed =
        [
            valid.Replace(
                "\"expectedSummaryCount\": 1",
                "\"removedSummaryCount\": 1",
                StringComparison.Ordinal),
            valid.Replace(
                "\"expectedSummaryCount\": 1,",
                "\"expectedSummaryCount\": 1,\n  \"expectedSummaryCount\": 1,",
                StringComparison.Ordinal),
            valid.Replace(
                "\"expectedSummaryCount\": 1",
                "\"expectedSummaryCount\": \"1\"",
                StringComparison.Ordinal),
            valid.Replace(
                "\"completedSummaryCount\": 1",
                "\"completedSummaryCount\": -1",
                StringComparison.Ordinal),
            valid.Replace(
                "\"completedSummaryCount\": 1",
                "\"completedSummaryCount\": 2",
                StringComparison.Ordinal),
            valid.Replace(
                "\"completedSummaryCount\": 1",
                "\"completedSummaryCount\": 0",
                StringComparison.Ordinal),
        ];

        Assert.All(
            malformed,
            json =>
                Assert.ThrowsAny<Exception>(
                    () => O5R5D7R1EvidenceWriter.Deserialise(Encoding.UTF8.GetBytes(json))));
    }

    /// <summary>Verifies exact R1-U and conditional R1-E classifications and run cardinalities.</summary>
    [Fact]
    public void ClassificationsRequireExactNewRunCounts()
    {
        Assert.Equal(
            "D7-R1.NOT_REPRODUCED",
            O5R5D7R1Evidence.ClassifyUnobserved([false, false, false]));
        Assert.Equal(
            "D7-R1.INTERMITTENT",
            O5R5D7R1Evidence.ClassifyUnobserved([true, false, false]));
        Assert.Equal(
            "D7-R1.REPEATED",
            O5R5D7R1Evidence.ClassifyUnobserved([true, false, true]));
        Assert.Equal(
            "D7-R1.REPEATED_CONSECUTIVELY",
            O5R5D7R1Evidence.ClassifyUnobserved([true, true, true]));
        Assert.Equal(
            "D7-R1.EXTERNAL_NOT_REPRODUCED",
            O5R5D7R1Evidence.ClassifyExternal([false, false]));
        Assert.Equal(
            "D7-R1.EXTERNAL_MIXED",
            O5R5D7R1Evidence.ClassifyExternal([true, false]));
        Assert.Equal(
            "D7-R1.EXTERNAL_REPRODUCED",
            O5R5D7R1Evidence.ClassifyExternal([true, true]));
        Assert.Throws<InvalidOperationException>(
            () => O5R5D7R1Evidence.ClassifyUnobserved([false, false]));
        Assert.Throws<InvalidOperationException>(
            () => O5R5D7R1Evidence.ClassifyExternal([false]));
    }

    /// <summary>Verifies the R1 writer uses a distinct strict root and atomic final publication.</summary>
    [Fact]
    public async Task FinalWriterUsesDistinctStrictRootAndAtomicPublication()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"DBNotifier-PF-OBS-1-D7-R1-{Guid.NewGuid():N}");
        string destination = Path.Combine(root, "unobserved-run-1.json");
        O5R5D7R1Envelope envelope = O5R5D7R1Evidence.Blocked(
            O5R5D7Arm.Unobserved,
            1,
            measured: null,
            "o5r5d7r1.test.blocked");

        try
        {
            Assert.Equal(
                Path.GetFullPath(destination),
                O5R5D7R1EvidenceWriter.ValidateFinalDestination(
                    destination,
                    O5R5D7Arm.Unobserved,
                    1));
            Assert.Throws<InvalidOperationException>(
                () => O5R5D7R1EvidenceWriter.ValidateFinalDestination(
                    Path.Combine(
                        Path.GetTempPath(),
                        $"DBNotifier-PF-OBS-1-D7-{Guid.NewGuid():N}",
                        "unobserved-run-1.json"),
                    O5R5D7Arm.Unobserved,
                    1));

            await O5R5D7R1EvidenceWriter.WriteFinalAsync(
                destination,
                envelope,
                CancellationToken.None);

            Assert.True(File.Exists(destination));
            Assert.False(File.Exists(destination + ".tmp"));
            Assert.Equal(envelope, O5R5D7R1EvidenceWriter.ReadFinal(destination));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    /// <summary>Verifies exact marker-gated arguments reject malformed, excessive and out-of-range input.</summary>
    [Fact]
    public void ProcessArgumentsAreExactAndBounded()
    {
        string[] valid =
        [
            "--activation",
            O5R5D7R1SupervisorProcess.ActivationMarker,
            "--output",
            "test",
            "--arm",
            "Unobserved",
            "--run",
            "1",
            "--sdk",
            "10.0.302",
            "--installed-memory",
            "1",
        ];

        Assert.True(
            O5R5D7R1Arguments.TryParse(
                valid,
                O5R5D7R1SupervisorProcess.ActivationMarker,
                out O5R5D7R1Arguments? parsed));
        Assert.NotNull(parsed);
        Assert.False(
            O5R5D7R1Arguments.TryParse(
                [.. valid, "unexpected"],
                O5R5D7R1SupervisorProcess.ActivationMarker,
                out _));
        Assert.False(
            O5R5D7R1Arguments.TryParse(
                valid,
                O5R5D7R1MeasuredProcess.ActivationMarker,
                out _));
        string[] outOfRange = [.. valid];
        outOfRange[7] = "4";
        Assert.False(
            O5R5D7R1Arguments.TryParse(
                outOfRange,
                O5R5D7R1SupervisorProcess.ActivationMarker,
                out _));
    }

    /// <summary>Builds a complete synthetic D7 report for schema validation without physical capture.</summary>
    /// <returns>A structurally valid complete measured report with one original summary.</returns>
    private static O5R5D7MeasuredReport CreateCompleteMeasured()
    {
        List<O5R5MeasurementSample> samples = new(O5R5D7Protocol.ExpectedSampleCount);
        for (int index = 0; index < O5R5D7Protocol.ExpectedSampleCount; index++)
        {
            bool warmUp = index < O5R5MeasurementProtocol.WarmUpRepetitions;
            int repetition = warmUp
                ? index + 1
                : index - O5R5MeasurementProtocol.WarmUpRepetitions + 1;
            samples.Add(
                new O5R5MeasurementSample(
                    O5R5D7Protocol.V3Digest,
                    O5R5MeasurementPhase.FirstByte,
                    O5R5Temperature.Cold,
                    warmUp,
                    repetition,
                    ElapsedMilliseconds: 0.1d,
                    CpuMilliseconds: 0d,
                    HeapPeakDeltaBytes: 0,
                    WorkingSetPeakDeltaBytes: 0,
                    AllocationPeakBytes: 0,
                    CumulativeAllocatedBytes: 0,
                    DeclaredWorkUnits: 1,
                    AccountedMemoryBytes: 1,
                    ElapsedMicrosecondsPerWorkUnit: 100d,
                    CpuMicrosecondsPerWorkUnit: 0d,
                    FirstObservationMilliseconds: 0.01d,
                    RepeatabilityWindowMilliseconds: 0.1d,
                    Passed: true));
        }

        O5R5MeasurementReadinessRunner runner = new(
            O5R5MeasurementReadinessRunner.Marker,
            O5R5MeasurementProtocol.CreateFrozen(),
            new UnusedMeasurementSource());
        O5R5MeasurementSummary summary = runner.Summarise(samples);
        return new O5R5D7MeasuredReport(
            O5R5D7Protocol.Version,
            O5R5D7Protocol.Digest,
            O5R5D7Protocol.V3Digest,
            O5R5D7Protocol.D6Digest,
            "None",
            O5R5D7Arm.Unobserved,
            1,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddSeconds(1),
            new O5R5PhysicalEnvironment(
                "synthetic-os",
                "X64",
                "X64",
                ".NET synthetic",
                "10.0.302",
                1,
                1,
                1),
            O5R5D7Protocol.ExpectedSampleCount,
            samples.Count,
            O5R5D7Protocol.ExpectedSummaryCount,
            1,
            "o5r5d7.group.complete",
            Complete: true,
            samples,
            summary,
            summary.Failures);
    }

    /// <summary>Provides no metrics because the schema regression never executes a physical sample.</summary>
    private sealed class UnusedMeasurementSource : IO5R5MeasurementSource
    {
        /// <inheritdoc />
        public long Frequency => 1;

        /// <inheritdoc />
        public O5R5MetricSnapshot Capture() =>
            throw new InvalidOperationException("o5r5d7r1.test.source_unused");
    }
}
