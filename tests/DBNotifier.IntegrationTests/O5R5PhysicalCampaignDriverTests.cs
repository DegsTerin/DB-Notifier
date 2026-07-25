// Module purpose: Validates the O5-R5-B physical campaign matrix, bounded workloads and fail-closed evidence path without host measurement.
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Proves O5-R5-B remains exact, bounded, non-authorising and isolated before physical execution.</summary>
public sealed class O5R5PhysicalCampaignDriverTests
{
    private const string ExpectedDigest =
        "53F40F7DC72548EB488FFF729823BF0DCFD64EDD085314022C8E746CC45B5D71";

    /// <summary>Proves the driver creates exactly eight phases, two temperatures and the frozen 5/30 sequences.</summary>
    [Fact]
    public void ScenarioMatrixIsExactAndUsesMaximumAcceptedReservations()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        O5R5PhysicalCampaignDriver driver = new(protocol, new UnusedSyntheticSource());

        IReadOnlyList<O5R5MeasurementScenario> scenarios = driver.CreateScenarios();

        Assert.Equal(O5R5PhysicalCampaignDriver.ExpectedSampleCount, scenarios.Count);
        Assert.Equal(560, scenarios.Count);
        Assert.Equal(ExpectedDigest, protocol.Digest);
        Assert.All(
            Enum.GetValues<O5R5MeasurementPhase>(),
            phase =>
                Assert.All(
                    Enum.GetValues<O5R5Temperature>(),
                    temperature =>
                    {
                        O5R5MeasurementScenario[] batch = scenarios
                            .Where(item => item.Phase == phase && item.Temperature == temperature)
                            .ToArray();
                        Assert.Equal(35, batch.Length);
                        Assert.Equal(5, batch.Count(item => item.IsWarmUp));
                        Assert.Equal(30, batch.Count(item => !item.IsWarmUp));
                        Assert.Equal(
                            Enumerable.Range(1, 5),
                            batch.Where(item => item.IsWarmUp).Select(item => item.Repetition));
                        Assert.Equal(
                            Enumerable.Range(1, 30),
                            batch.Where(item => !item.IsWarmUp).Select(item => item.Repetition));
                    }));
        Assert.All(
            scenarios,
            scenario =>
            {
                Assert.Equal(protocol.Envelope.MaximumWorkUnits, scenario.DeclaredWorkUnits);
                Assert.Equal(
                    protocol.Envelope.MaximumAccountedMemoryBytes,
                    scenario.AccountedMemoryBytes);
                Assert.NotNull(scenario.Operation);
            });
        Assert.False(O5R5PhysicalCampaignReport.IsAuthorising);
    }

    /// <summary>Proves every workload completes once under synthetic orchestration and honours cancellation.</summary>
    [Fact]
    public async Task WorkloadsAreBoundedAndCancellationAware()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        O5R5PhysicalCampaignDriver driver = new(protocol, new UnusedSyntheticSource());
        O5R5MeasurementScenario[] representatives = driver
            .CreateScenarios()
            .Where(item => item.IsWarmUp && item.Repetition == 1)
            .ToArray();

        Assert.Equal(16, representatives.Length);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        foreach (O5R5MeasurementScenario scenario in representatives)
        {
            O5R5MeasurementContext context = new(new CheckpointSyntheticSource());
            await scenario.Operation(context, timeout.Token);
            Assert.InRange(context.Checkpoints.Count, 1, O5R5MeasurementProtocol.MaximumCheckpoints);
        }

        O5R5MeasurementScenario cancellable = representatives.Single(
            item =>
                item.Phase == O5R5MeasurementPhase.Analysis &&
                item.Temperature == O5R5Temperature.Warm);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
            {
                using CancellationTokenSource cancelled = new();
                cancelled.Cancel();
                await cancellable.Operation(
                    new O5R5MeasurementContext(new CheckpointSyntheticSource()),
                    cancelled.Token);
            });
    }

    /// <summary>Proves the evidence writer accepts only the exact project-owned temporary boundary.</summary>
    [Fact]
    public async Task EvidenceWriterIsAtomicAndRefusesPathsOutsideExactTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"DBNotifier-O5-R5-Physical-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string destination = Path.Combine(root, "o5-r5-physical-campaign.json");
        try
        {
            O5R5PhysicalCampaignReport report = Report();

            await O5R5PhysicalEvidenceWriter.WriteAsync(destination, report, CancellationToken.None);

            string evidence = await File.ReadAllTextAsync(destination);
            Assert.Contains("\"activationState\": \"None\"", evidence, StringComparison.Ordinal);
            Assert.Contains("\"passed\": false", evidence, StringComparison.Ordinal);
            Assert.DoesNotContain(Environment.MachineName, evidence, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(Environment.UserName, evidence, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(destination + ".tmp"));
            InvalidOperationException outside = Assert.Throws<InvalidOperationException>(
                () => O5R5PhysicalEvidenceWriter.ValidateDestination(
                    Path.Combine(Path.GetTempPath(), "o5-r5-physical-campaign.json")));
            Assert.Equal("o5r5b.output.path_refused", outside.Message);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    /// <summary>Creates a minimal non-authorising report for evidence-writer validation.</summary>
    /// <returns>A sanitised incomplete campaign report.</returns>
    private static O5R5PhysicalCampaignReport Report() =>
        new(
            O5R5MeasurementProtocol.Version,
            ExpectedDigest,
            "None",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            new O5R5PhysicalEnvironment(
                "synthetic",
                "synthetic",
                "synthetic",
                ".NET synthetic",
                "10.0.0",
                1,
                1,
                1),
            "o5r5b.synthetic",
            false,
            560,
            0,
            [],
            []);

    /// <summary>Rejects accidental physical capture during matrix-only automatic tests.</summary>
    private sealed class UnusedSyntheticSource : IO5R5MeasurementSource
    {
        /// <inheritdoc />
        public long Frequency => 1;

        /// <inheritdoc />
        public O5R5MetricSnapshot Capture() =>
            throw new InvalidOperationException("automatic matrix validation must not capture");
    }

    /// <summary>Supplies a bounded deterministic checkpoint without reading host counters.</summary>
    private sealed class CheckpointSyntheticSource : IO5R5MeasurementSource
    {
        private long timestamp;

        /// <inheritdoc />
        public long Frequency => 1_000;

        /// <inheritdoc />
        public O5R5MetricSnapshot Capture() =>
            new(
                Interlocked.Increment(ref timestamp),
                1,
                timestamp,
                1,
                TimeSpan.FromTicks(timestamp));
    }
}
