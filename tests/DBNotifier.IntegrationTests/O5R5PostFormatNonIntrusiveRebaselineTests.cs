// Module purpose: Verifies the immutable D6 prefix, evidence boundary and non-authorising post-format rebaseline contract.
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Protects D6 structure without constructing the physical measurement source.</summary>
public sealed class O5R5PostFormatNonIntrusiveRebaselineTests
{
    /// <summary>Verifies the preregistered identity, V3 dependency and inactive boundary.</summary>
    [Fact]
    public void ProtocolIdentityIsStable()
    {
        Assert.Equal(O5R5D6Protocol.Digest, O5R5D6Protocol.CalculateDigest());
        Assert.Equal(O5R5D6Protocol.V3Digest, O5R5MeasurementProtocol.CreateFrozen().Digest);
        Assert.Equal(2, O5R5D6Protocol.Runs);
        Assert.Equal(158, O5R5D6Protocol.ExpectedSampleCount);
        Assert.Equal(4, O5R5D6Protocol.ExpectedSummaryCount);
        Assert.Contains("intraprocess-extra-captures=0", O5R5D6Protocol.CanonicalStatement);
        Assert.False(O5R5D6Report.IsAuthorising);
    }

    /// <summary>Verifies D6 selects the first 158 unchanged scenarios in original V3 order.</summary>
    [Fact]
    public void ExactPrefixPreservesOriginalOrder()
    {
        O5R5D6Runner runner = new(new UnusedMeasurementSource());

        IReadOnlyList<O5R5MeasurementScenario> selected = runner.SelectExactPrefix();

        Assert.Equal(158, selected.Count);
        Assert.Equal(70, selected.Count(item => item.Phase == O5R5MeasurementPhase.FirstByte));
        Assert.Equal(70, selected.Count(item => item.Phase == O5R5MeasurementPhase.Idle));
        O5R5MeasurementScenario[] cancellation = selected
            .Where(item => item.Phase == O5R5MeasurementPhase.Cancellation)
            .ToArray();
        Assert.Equal(18, cancellation.Length);
        Assert.All(cancellation, item => Assert.Equal(O5R5Temperature.Cold, item.Temperature));
        Assert.Equal(5, cancellation.Count(item => item.IsWarmUp));
        Assert.Equal(
            Enumerable.Range(1, 13),
            cancellation.Where(item => !item.IsWarmUp).Select(item => item.Repetition));
    }

    /// <summary>Verifies output is restricted to an exact direct temporary child and run name.</summary>
    [Fact]
    public void EvidenceDestinationIsStrictlyBounded()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"DBNotifier-PF-OBS-1-D6-{Guid.NewGuid():N}");
        string valid = Path.Combine(root, "unobserved-run-1.json");

        Assert.Equal(
            Path.GetFullPath(valid),
            O5R5D6EvidenceWriter.ValidateDestination(valid, 1));
        Assert.Throws<InvalidOperationException>(
            () => O5R5D6EvidenceWriter.ValidateDestination(
                Path.Combine(root, "wrong.json"),
                1));
        Assert.Throws<InvalidOperationException>(
            () => O5R5D6EvidenceWriter.ValidateDestination(
                Path.Combine(root, "nested", "unobserved-run-1.json"),
                1));
    }

    /// <summary>Verifies the atomic writer closes its stream before publishing and leaves no temporary sibling.</summary>
    [Fact]
    public async Task EvidenceWriterPublishesAfterClosingTheStream()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"DBNotifier-PF-OBS-1-D6-{Guid.NewGuid():N}");
        string destination = Path.Combine(root, "unobserved-run-1.json");
        O5R5D6Report report = new(
            O5R5D6Protocol.Version,
            O5R5D6Protocol.Digest,
            O5R5D6Protocol.V3Digest,
            "None",
            1,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            new O5R5PhysicalEnvironment(
                "test-only",
                "X64",
                "X64",
                ".NET test-only",
                "10.0.302",
                1,
                1,
                1),
            O5R5D6Protocol.ExpectedSampleCount,
            0,
            "o5r5d6.test",
            false,
            false,
            [],
            [],
            []);

        try
        {
            await O5R5D6EvidenceWriter.WriteAsync(
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

    /// <summary>Provides no metrics because structural tests must remain non-physical.</summary>
    private sealed class UnusedMeasurementSource : IO5R5MeasurementSource
    {
        /// <inheritdoc />
        public long Frequency => 1;

        /// <inheritdoc />
        public O5R5MetricSnapshot Capture() =>
            throw new InvalidOperationException("o5r5d6.test.source_unused");
    }
}
