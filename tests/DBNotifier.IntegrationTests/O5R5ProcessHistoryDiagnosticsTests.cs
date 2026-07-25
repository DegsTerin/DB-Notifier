// Module purpose: Verifies the frozen D5 process-history matrix, evidence boundary and non-authorising diagnostic contract.
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Protects the exact D5 comparison matrix without executing a physical diagnostic campaign.</summary>
public sealed class O5R5ProcessHistoryDiagnosticsTests
{
    /// <summary>Verifies the canonical statement retains its preregistered identity and unchanged V3 boundary.</summary>
    [Fact]
    public void ProtocolIdentityIsStable()
    {
        Assert.Equal(O5R5D5DiagnosticProtocol.Digest, O5R5D5DiagnosticProtocol.CalculateDigest());
        Assert.Equal(
            O5R5D5DiagnosticProtocol.V3Digest,
            O5R5MeasurementProtocol.CreateFrozen().Digest);
        Assert.Contains("forced-gc=v3-two-existing-only", O5R5D5DiagnosticProtocol.CanonicalStatement);
        Assert.Equal(2, O5R5D5DiagnosticProtocol.RunsPerVariant);
        Assert.False(O5R5D5DiagnosticReport.IsAuthorising);
    }

    /// <summary>Verifies every comparison selects only the preregistered V3 prefix in stable order.</summary>
    [Theory]
    [InlineData(0, 158, 70, 70)]
    [InlineData(1, 88, 0, 70)]
    [InlineData(2, 88, 70, 0)]
    [InlineData(3, 18, 0, 0)]
    public void MatrixSelectionIsExact(
        int variantCode,
        int expected,
        int firstByte,
        int idle)
    {
        O5R5D5HistoryVariant variant = (O5R5D5HistoryVariant)variantCode;
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        UnusedMeasurementSource source = new();
        O5R5PhysicalCampaignDriver driver = new(protocol, source);

        IReadOnlyList<O5R5MeasurementScenario> selected =
            O5R5D5DiagnosticRunner.SelectScenarios(driver.CreateScenarios(), variant);

        Assert.Equal(expected, selected.Count);
        Assert.Equal(firstByte, selected.Count(item => item.Phase == O5R5MeasurementPhase.FirstByte));
        Assert.Equal(idle, selected.Count(item => item.Phase == O5R5MeasurementPhase.Idle));
        O5R5MeasurementScenario[] cancellation = selected
            .Where(item => item.Phase == O5R5MeasurementPhase.Cancellation)
            .ToArray();
        Assert.Equal(18, cancellation.Length);
        Assert.All(cancellation, item => Assert.Equal(O5R5Temperature.Cold, item.Temperature));
        Assert.Equal(5, cancellation.Count(item => item.IsWarmUp));
        Assert.Equal(13, cancellation.Count(item => !item.IsWarmUp));
        Assert.Equal(
            Enumerable.Range(1, 13),
            cancellation.Where(item => !item.IsWarmUp).Select(item => item.Repetition));
    }

    /// <summary>Verifies evidence paths are confined to exact direct temporary children and file names.</summary>
    [Fact]
    public void EvidenceDestinationIsStrictlyBounded()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"DBNotifier-PF-OBS-1-D5-{Guid.NewGuid():N}");
        string valid = Path.Combine(root, "exact-prefix-run-1.json");
        string accepted = O5R5D5EvidenceWriter.ValidateDestination(
            valid,
            O5R5D5HistoryVariant.ExactPrefix,
            1);

        Assert.Equal(Path.GetFullPath(valid), accepted);
        Assert.Throws<InvalidOperationException>(
            () => O5R5D5EvidenceWriter.ValidateDestination(
                Path.Combine(root, "wrong.json"),
                O5R5D5HistoryVariant.ExactPrefix,
                1));
        Assert.Throws<InvalidOperationException>(
            () => O5R5D5EvidenceWriter.ValidateDestination(
                Path.Combine(root, "nested", "exact-prefix-run-1.json"),
                O5R5D5HistoryVariant.ExactPrefix,
                1));
    }

    /// <summary>Supplies an intentionally unusable source because matrix selection performs no measurement.</summary>
    private sealed class UnusedMeasurementSource : IO5R5MeasurementSource
    {
        /// <inheritdoc />
        public long Frequency => 1;

        /// <inheritdoc />
        public O5R5MetricSnapshot Capture() =>
            throw new InvalidOperationException("o5r5d5.test.source_unused");
    }
}
