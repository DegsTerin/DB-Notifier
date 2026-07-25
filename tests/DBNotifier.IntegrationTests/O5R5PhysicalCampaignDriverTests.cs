// Module purpose: Validates the O5-R5-B physical campaign matrix, bounded workloads and fail-closed evidence path without host measurement.
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Proves O5-R5-B remains exact, bounded, non-authorising and isolated before physical execution.</summary>
public sealed class O5R5PhysicalCampaignDriverTests
{
    private const string ExpectedDigest =
        "60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A";

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
            if (scenario.Phase == O5R5MeasurementPhase.FirstByte)
            {
                Assert.Single(context.Checkpoints);
            }
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

    /// <summary>
    /// Proves Cancellation/Cold preserves the exact input workload while reusing one bounded
    /// serial buffer without per-sample managed allocation.
    /// </summary>
    [Fact]
    public void CancellationColdInputIsCompleteFreshAndAllocationStable()
    {
        byte[] source = Enumerable
            .Range(0, checked((int)O1ResourceEnvelope.Fixture().MaximumInputBytes))
            .Select(index => (byte)(index % 251))
            .ToArray();
        long cloneBefore = GC.GetAllocatedBytesForCurrentThread();
        byte[] formerPerSampleClone = (byte[])source.Clone();
        long formerCloneAllocation = GC.GetAllocatedBytesForCurrentThread() - cloneBefore;
        O5R5CancellationInputBuffer buffer = new(source);
        ReadOnlyMemory<byte> first = buffer.Materialise(O5R5Temperature.Cold);

        long before = GC.GetAllocatedBytesForCurrentThread();
        ReadOnlyMemory<byte> last = default;
        for (int repetition = 0;
             repetition <
                O5R5MeasurementProtocol.WarmUpRepetitions +
                O5R5MeasurementProtocol.MeasuredRepetitions;
             repetition++)
        {
            last = buffer.Materialise(O5R5Temperature.Cold);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.InRange(formerCloneAllocation, source.Length, source.Length + 64);
        GC.KeepAlive(formerPerSampleClone);
        Assert.Equal(0, allocated);
        Assert.Equal(O1ResourceEnvelope.Fixture().MaximumInputBytes, last.Length);
        Assert.True(first.Equals(last));
        Assert.True(source.AsSpan().SequenceEqual(last.Span));
        Assert.True(source.AsSpan().SequenceEqual(buffer.Materialise(O5R5Temperature.Warm).Span));
    }

    /// <summary>
    /// Proves the full frozen Cancellation/Cold batch still observes inner cancellation once per
    /// sample and remains bounded under synthetic counters.
    /// </summary>
    [Fact]
    public async Task CancellationColdBatchPreservesCancellationAndCheckpointContract()
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        O5R5PhysicalCampaignDriver driver = new(protocol, new UnusedSyntheticSource());
        O5R5MeasurementScenario[] cancellation = driver
            .CreateScenarios()
            .Where(
                scenario =>
                    scenario.Phase == O5R5MeasurementPhase.Cancellation &&
                    scenario.Temperature == O5R5Temperature.Cold)
            .ToArray();

        Assert.Equal(
            O5R5MeasurementProtocol.WarmUpRepetitions +
                O5R5MeasurementProtocol.MeasuredRepetitions,
            cancellation.Length);
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
        foreach (O5R5MeasurementScenario scenario in cancellation)
        {
            O5R5MeasurementContext context = new(new CheckpointSyntheticSource());
            await scenario.Operation(context, deadline.Token);
            Assert.Single(context.Checkpoints);
        }
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
            O5R5PhysicalCampaignReport roundTrip =
                await O5R5PhysicalEvidenceWriter.ReadValidatedAsync(
                    destination,
                    CancellationToken.None);

            string evidence = await File.ReadAllTextAsync(destination);
            Assert.Equal(report.ProtocolVersion, roundTrip.ProtocolVersion);
            Assert.Equal(report.ProtocolDigest, roundTrip.ProtocolDigest);
            Assert.Equal(report.ActivationState, roundTrip.ActivationState);
            Assert.Equal(report.Code, roundTrip.Code);
            Assert.Equal(report.Passed, roundTrip.Passed);
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

    /// <summary>Proves corrupt and structurally incomplete evidence fail closed during recovery.</summary>
    [Fact]
    public async Task EvidenceReaderRejectsCorruptionAndIncompleteReports()
    {
        string root = NewEvidenceRoot();
        string destination = Path.Combine(root, "o5-r5-physical-campaign.json");
        try
        {
            await File.WriteAllTextAsync(destination, "{");
            InvalidOperationException corrupt = await Assert.ThrowsAsync<InvalidOperationException>(
                () => O5R5PhysicalEvidenceWriter.ReadValidatedAsync(
                    destination,
                    CancellationToken.None));
            Assert.Equal("o5r5d1.evidence.corrupt", corrupt.Message);

            await File.WriteAllTextAsync(destination, "{}");
            InvalidOperationException incomplete =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => O5R5PhysicalEvidenceWriter.ReadValidatedAsync(
                        destination,
                        CancellationToken.None));
            Assert.Equal("o5r5d1.evidence.incomplete", incomplete.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Proves a failed temporary write leaves neither destination nor temporary residue.</summary>
    [Fact]
    public async Task EvidenceWriterCleansTemporaryFileAfterWriteFailure()
    {
        string root = NewEvidenceRoot();
        string destination = Path.Combine(root, "o5-r5-physical-campaign.json");
        FailingEvidenceFileOperations operations = new();
        try
        {
            await Assert.ThrowsAsync<IOException>(
                () => O5R5PhysicalEvidenceWriter.WriteAsync(
                    destination,
                    Report(),
                    operations,
                    CancellationToken.None));

            Assert.True(operations.DeleteCalled);
            Assert.False(operations.TemporaryExists);
            Assert.False(File.Exists(destination));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Proves a failed campaign retains exact phase, sample, metric, value and limit.</summary>
    [Fact]
    public async Task EvidenceWriterRetainsSanitisedThresholdDiagnostic()
    {
        string root = NewEvidenceRoot();
        string destination = Path.Combine(root, "o5-r5-physical-campaign.json");
        try
        {
            O5R5ThresholdFailure failure = new(
                "o5r5d1.threshold.working-set-peak",
                O5R5MeasurementPhase.FirstByte,
                O5R5Temperature.Cold,
                false,
                1,
                O5R5ThresholdMetric.WorkingSetPeak,
                786_433d,
                786_432d,
                O5R5ThresholdUnit.Bytes);
            O5R5MeasurementSample sample = new(
                ExpectedDigest,
                O5R5MeasurementPhase.FirstByte,
                O5R5Temperature.Cold,
                false,
                1,
                1d,
                1d,
                0,
                786_433,
                0,
                0,
                O5R5MeasurementProtocol.FirstByteRepeatabilityWorkUnits,
                524_288,
                0.0075d,
                0d,
                0.25d,
                0.75d,
                false);
            O5R5PhysicalCampaignReport report = new(
                O5R5MeasurementProtocol.Version,
                ExpectedDigest,
                "None",
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch,
                SyntheticEnvironment(),
                failure.Code,
                false,
                560,
                1,
                [sample],
                [],
                [failure]);

            await O5R5PhysicalEvidenceWriter.WriteAsync(
                destination,
                report,
                CancellationToken.None);
            O5R5PhysicalCampaignReport retained =
                await O5R5PhysicalEvidenceWriter.ReadValidatedAsync(
                    destination,
                    CancellationToken.None);

            O5R5ThresholdFailure actual = Assert.Single(retained.Failures);
            Assert.Equal(O5R5MeasurementPhase.FirstByte, actual.Phase);
            Assert.Equal(O5R5Temperature.Cold, actual.Temperature);
            Assert.False(actual.IsWarmUp);
            Assert.Equal(1, actual.Repetition);
            Assert.Equal(O5R5ThresholdMetric.WorkingSetPeak, actual.Metric);
            Assert.Equal(786_433d, actual.Observed);
            Assert.Equal(786_432d, actual.InclusiveLimit);
            Assert.Equal(O5R5ThresholdUnit.Bytes, actual.Unit);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
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
            SyntheticEnvironment(),
            "o5r5b.synthetic",
            false,
            560,
            0,
            [],
            [],
            []);

    /// <summary>Creates one bounded synthetic environment declaration.</summary>
    /// <returns>A host-neutral environment without identity.</returns>
    private static O5R5PhysicalEnvironment SyntheticEnvironment() =>
        new(
            "synthetic",
            "synthetic",
            "synthetic",
            ".NET synthetic",
            "10.0.0",
            1,
            1,
            1);

    /// <summary>Creates one exact project-owned temporary evidence root.</summary>
    /// <returns>A newly created absolute root.</returns>
    private static string NewEvidenceRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"DBNotifier-O5-R5-Physical-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

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

    /// <summary>Injects one deterministic write failure and records temporary cleanup.</summary>
    private sealed class FailingEvidenceFileOperations : IO5R5EvidenceFileOperations
    {
        /// <summary>Gets whether the synthetic temporary path currently exists.</summary>
        internal bool TemporaryExists { get; private set; }

        /// <summary>Gets whether cleanup attempted to delete the temporary path.</summary>
        internal bool DeleteCalled { get; private set; }

        /// <inheritdoc />
        public Task WriteAllBytesAsync(
            string path,
            byte[] bytes,
            CancellationToken cancellationToken)
        {
            TemporaryExists = true;
            throw new IOException("synthetic write failure");
        }

        /// <inheritdoc />
        public bool Exists(string path) => TemporaryExists;

        /// <inheritdoc />
        public void Delete(string path)
        {
            DeleteCalled = true;
            TemporaryExists = false;
        }

        /// <inheritdoc />
        public void Move(string source, string destination) =>
            throw new InvalidOperationException("move must not run after a failed write");
    }
}
