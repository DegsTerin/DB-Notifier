// Module purpose: Verifies D7 identity, exact V3 membership, classifications, integrity arithmetic and bounded evidence publication.
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Protects the frozen D7 diagnostic without running a physical measurement child.</summary>
public sealed class O5R5FirstByteColdRepeatabilityDiagnosticTests
{
    /// <summary>Verifies the proposal identity, dependencies, arm sizes and inactive boundary.</summary>
    [Fact]
    public void ProtocolIdentityIsStable()
    {
        Assert.Equal(O5R5D7Protocol.Digest, O5R5D7Protocol.CalculateDigest());
        Assert.Equal(O5R5D7Protocol.V3Digest, O5R5MeasurementProtocol.CreateFrozen().Digest);
        Assert.Equal(O5R5D7Protocol.D6Digest, O5R5D6Protocol.Digest);
        Assert.Equal(3, O5R5D7Protocol.UnobservedRuns);
        Assert.Equal(2, O5R5D7Protocol.ExternalRuns);
        Assert.Equal(2, O5R5D7Protocol.ExternalFailureGate);
        Assert.Equal(35, O5R5D7Protocol.ExpectedSampleCount);
        Assert.Equal(1, O5R5D7Protocol.ExpectedSummaryCount);
        Assert.Equal(0.20d, O5R5D7Protocol.CoefficientLimit);
        Assert.False(O5R5D7FinalReport.IsAuthorising);
        Assert.Contains(
            typeof(O5R5D7MeasuredReport).GetProperties(),
            property => property.Name == nameof(O5R5D7MeasuredReport.ExpectedSummaryCount));
        Assert.Contains(
            typeof(O5R5D7MeasuredReport).GetProperties(),
            property => property.Name == nameof(O5R5D7MeasuredReport.CompletedSummaryCount));
    }

    /// <summary>Verifies D7 selects only the original five warm-ups and thirty measured cold samples.</summary>
    [Fact]
    public void ExactGroupPreservesOriginalOrder()
    {
        O5R5D7MeasuredRunner runner = new(new UnusedMeasurementSource());

        IReadOnlyList<O5R5MeasurementScenario> selected = runner.SelectExactGroup();

        Assert.Equal(35, selected.Count);
        Assert.All(selected, item => Assert.Equal(O5R5MeasurementPhase.FirstByte, item.Phase));
        Assert.All(selected, item => Assert.Equal(O5R5Temperature.Cold, item.Temperature));
        Assert.Equal(
            Enumerable.Range(1, 5),
            selected.Where(item => item.IsWarmUp).Select(item => item.Repetition));
        Assert.Equal(
            Enumerable.Range(1, 30),
            selected.Where(item => !item.IsWarmUp).Select(item => item.Repetition));
    }

    /// <summary>Verifies the independent five-by-six calculation retains every value in fixed order.</summary>
    [Fact]
    public void IntegrityRecomputationUsesFiveFixedGroups()
    {
        int[] groupOrder = [5, 1, 4, 2, 3];
        double[] values = groupOrder
            .SelectMany(value => Enumerable.Repeat((double)value, 6))
            .ToArray();

        (double[] medians, double coefficient) = O5R5D7Integrity.RecomputeValues(values);

        Assert.Equal([5d, 1d, 4d, 2d, 3d], medians);
        Assert.Equal(Math.Sqrt(2d) / 3d, coefficient, precision: 12);
    }

    /// <summary>Verifies every preregistered unobserved and external classification.</summary>
    [Fact]
    public void ClassificationsUseExactRunCounts()
    {
        Assert.Equal("D7.NOT_REPRODUCED", O5R5D7Integrity.ClassifyUnobserved([false, false, false]));
        Assert.Equal("D7.INTERMITTENT", O5R5D7Integrity.ClassifyUnobserved([true, false, false]));
        Assert.Equal("D7.REPEATED", O5R5D7Integrity.ClassifyUnobserved([true, false, true]));
        Assert.Equal(
            "D7.REPEATED_CONSECUTIVELY",
            O5R5D7Integrity.ClassifyUnobserved([true, true, true]));
        Assert.Equal("D7.EXTERNAL_NOT_REPRODUCED", O5R5D7Integrity.ClassifyExternal([false, false]));
        Assert.Equal("D7.EXTERNAL_MIXED", O5R5D7Integrity.ClassifyExternal([true, false]));
        Assert.Equal("D7.EXTERNAL_REPRODUCED", O5R5D7Integrity.ClassifyExternal([true, true]));
        Assert.Throws<InvalidOperationException>(
            () => O5R5D7Integrity.ClassifyUnobserved([true, false]));
        Assert.Throws<InvalidOperationException>(
            () => O5R5D7Integrity.ClassifyExternal([true]));
    }

    /// <summary>Verifies final evidence is restricted to an exact direct D7 temporary child.</summary>
    [Fact]
    public void EvidenceDestinationIsStrictlyBounded()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"DBNotifier-PF-OBS-1-D7-{Guid.NewGuid():N}");
        string valid = Path.Combine(root, "unobserved-run-1.json");

        Assert.Equal(
            Path.GetFullPath(valid),
            O5R5D7EvidenceWriter.ValidateFinalDestination(valid, O5R5D7Arm.Unobserved, 1));
        Assert.Equal(
            Path.ChangeExtension(valid, ".measured.json"),
            O5R5D7EvidenceWriter.MeasuredDestination(valid));
        Assert.Throws<InvalidOperationException>(
            () => O5R5D7EvidenceWriter.ValidateFinalDestination(
                Path.Combine(root, "wrong.json"),
                O5R5D7Arm.Unobserved,
                1));
        Assert.Throws<InvalidOperationException>(
            () => O5R5D7EvidenceWriter.ValidateFinalDestination(
                Path.Combine(root, "nested", "unobserved-run-1.json"),
                O5R5D7Arm.Unobserved,
                1));
    }

    /// <summary>Verifies the atomic final writer closes its stream and removes its temporary sibling.</summary>
    [Fact]
    public async Task FinalWriterPublishesAfterClosingTheStream()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"DBNotifier-PF-OBS-1-D7-{Guid.NewGuid():N}");
        string destination = Path.Combine(root, "unobserved-run-1.json");
        O5R5D7FinalReport report = new(
            O5R5D7Protocol.Version,
            O5R5D7Protocol.Digest,
            O5R5D7Protocol.V3Digest,
            O5R5D7Protocol.D6Digest,
            "None",
            O5R5D7Arm.Unobserved,
            1,
            "o5r5d7.test",
            false,
            false,
            null,
            null,
            null);

        try
        {
            await O5R5D7EvidenceWriter.WriteFinalAsync(
                destination,
                report,
                CancellationToken.None);

            Assert.True(File.Exists(destination));
            Assert.False(File.Exists(destination + ".tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    /// <summary>Verifies the exact process argument grammar rejects malformed or excessive input.</summary>
    [Fact]
    public void ProcessArgumentsAreExactAndBounded()
    {
        string[] valid =
        [
            "--activation",
            O5R5D7SupervisorProcess.ActivationMarker,
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
            O5R5D7Arguments.TryParse(
                valid,
                O5R5D7SupervisorProcess.ActivationMarker,
                out O5R5D7Arguments? parsed));
        Assert.NotNull(parsed);
        Assert.False(
            O5R5D7Arguments.TryParse(
                [.. valid, "unexpected"],
                O5R5D7SupervisorProcess.ActivationMarker,
                out _));
        Assert.False(
            O5R5D7Arguments.TryParse(
                valid,
                O5R5D7MeasuredProcess.ActivationMarker,
                out _));
    }

    /// <summary>Provides no metrics because structural tests must remain non-physical.</summary>
    private sealed class UnusedMeasurementSource : IO5R5MeasurementSource
    {
        /// <inheritdoc />
        public long Frequency => 1;

        /// <inheritdoc />
        public O5R5MetricSnapshot Capture() =>
            throw new InvalidOperationException("o5r5d7.test.source_unused");
    }
}
