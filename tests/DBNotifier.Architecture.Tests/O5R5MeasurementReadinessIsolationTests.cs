// Module purpose: Proves O5-R5-A/B remain test-only, use the frozen protocol and cannot enter normal composition.
using DBNotifier.Application.AIOps;
using Xunit;

namespace DBNotifier.Architecture.Tests;

/// <summary>
/// Protects normal-composition isolation, built-in counter provenance, synthetic-only validation
/// marker-gated physical execution and the immutable inactive Observer state for O5-R5-A/B.
/// </summary>
public sealed class O5R5MeasurementReadinessIsolationTests
{
    private const string ExpectedV1Digest =
        "266B7A952DF1A46BEE4577894D0A9206D92917AC661E0E17F9052DE1EB415DD7";
    private const string ExpectedV2Digest =
        "53F40F7DC72548EB488FFF729823BF0DCFD64EDD085314022C8E746CC45B5D71";
    private const string ExpectedV3Digest =
        "60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A";

    /// <summary>Verifies normal product projects contain no O5-R5-A marker, runner or measurement source.</summary>
    [Fact]
    public void NormalProductSourceContainsNoO5R5AReference()
    {
        string sourceRoot = Path.Combine(RepositoryRoot(), "src");
        string[] forbidden =
        [
            "DBNOTIFIER_O5_R5_A_TEST_ONLY",
            "O5R5MeasurementReadinessRunner",
            "O5R5DotNetMeasurementSource",
            "o5r5a.",
        ];
        string[] violations = Directory
            .EnumerateFiles(sourceRoot, "*.*", SearchOption.AllDirectories)
            .Where(
                path => Path.GetExtension(path) is ".cs" or ".csproj" &&
                    !IsBuildOutput(path))
            .Where(
                path =>
                {
                    string source = File.ReadAllText(path);
                    return forbidden.Any(value => source.Contains(value, StringComparison.Ordinal));
                })
            .Select(path => Path.GetRelativePath(RepositoryRoot(), path))
            .ToArray();

        Assert.Empty(violations);
    }

    /// <summary>Verifies the runner is isolated in the existing test project and introduces no dependency.</summary>
    [Fact]
    public void RunnerUsesExactMarkerAndExistingIntegrationBoundary()
    {
        string runner = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5MeasurementReadinessRunner.cs");
        string project = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "DBNotifier.IntegrationTests.csproj");

        Assert.Contains(
            "internal const string Marker = \"DBNOTIFIER_O5_R5_A_TEST_ONLY\";",
            runner,
            StringComparison.Ordinal);
        Assert.Contains("o5r5a.runner.protocol_mismatch", runner, StringComparison.Ordinal);
        Assert.Contains("o5r5a.runner.saturated", runner, StringComparison.Ordinal);
        Assert.Contains("internal static bool MayEvaluate => false;", runner, StringComparison.Ordinal);
        Assert.Contains("internal static bool MayPublish => false;", runner, StringComparison.Ordinal);
        Assert.DoesNotContain("O5R5", project, StringComparison.Ordinal);
        Assert.DoesNotContain("PackageReference Include=\"", runner, StringComparison.Ordinal);
    }

    /// <summary>Verifies the future physical source uses only authorised built-in .NET and Windows counters.</summary>
    [Fact]
    public void PhysicalSourceUsesOnlyAuthorisedBuiltInCounters()
    {
        string runner = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5MeasurementReadinessRunner.cs");
        string[] required =
        [
            "Stopwatch.GetTimestamp()",
            "Stopwatch.Frequency",
            "GC.GetGCMemoryInfo()",
            "GC.GetTotalAllocatedBytes(precise: false)",
            "Process.GetCurrentProcess()",
            "Environment.WorkingSet",
            "process.TotalProcessorTime",
        ];
        string[] forbidden =
        [
            "Process.Start(",
            "HttpClient",
            "Socket",
            "File.Write",
            "File.Append",
            "EventPipe",
            "PerformanceCounter",
            "dotnet-counters",
            "dotnet-trace",
        ];

        Assert.All(required, value => Assert.Contains(value, runner, StringComparison.Ordinal));
        Assert.All(forbidden, value => Assert.DoesNotContain(value, runner, StringComparison.Ordinal));
    }

    /// <summary>Verifies automatic O5-R5-A/B tests use only synthetic sources and cannot execute host measurements.</summary>
    [Fact]
    public void AutomaticTestsDoNotInstantiatePhysicalMeasurementSource()
    {
        string tests = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5MeasurementReadinessRunnerTests.cs");

        Assert.Contains("SyntheticMeasurementSource", tests, StringComparison.Ordinal);
        Assert.Contains("ThrowingMeasurementSource", tests, StringComparison.Ordinal);
        Assert.DoesNotContain("O5R5DotNetMeasurementSource", tests, StringComparison.Ordinal);
        Assert.DoesNotContain("Stopwatch.GetTimestamp", tests, StringComparison.Ordinal);
        Assert.DoesNotContain("Process.GetCurrentProcess", tests, StringComparison.Ordinal);

        string driverTests = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5PhysicalCampaignDriverTests.cs");
        Assert.Contains("UnusedSyntheticSource", driverTests, StringComparison.Ordinal);
        Assert.Contains("CheckpointSyntheticSource", driverTests, StringComparison.Ordinal);
        Assert.DoesNotContain("O5R5DotNetMeasurementSource", driverTests, StringComparison.Ordinal);
        Assert.DoesNotContain("Stopwatch.GetTimestamp", driverTests, StringComparison.Ordinal);
        Assert.DoesNotContain("Process.GetCurrentProcess", driverTests, StringComparison.Ordinal);
    }

    /// <summary>Verifies the sole physical source construction is guarded by the exact digest marker.</summary>
    [Fact]
    public void PhysicalEntryPointValidatesOptInBeforeSourceConstruction()
    {
        string entryPoint = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5PhysicalCampaignEntryPointTests.cs");
        int testBoundary = entryPoint.IndexOf(
            "public sealed class O5R5PhysicalCampaignEntryPointTests",
            StringComparison.Ordinal);
        Assert.True(testBoundary >= 0);
        string guardedEntryPoint = entryPoint[testBoundary..];
        int markerRead = guardedEntryPoint.IndexOf(
            "Environment.GetEnvironmentVariable(PhysicalMarkerVariable)",
            StringComparison.Ordinal);
        int inertReturn = guardedEntryPoint.IndexOf("if (marker is null)", StringComparison.Ordinal);
        int digestCheck = guardedEntryPoint.IndexOf(
            "Assert.Equal(ExactPhysicalMarker, marker);",
            StringComparison.Ordinal);
        int destinationCheck = guardedEntryPoint.IndexOf(
            "O5R5PhysicalEvidenceWriter.ValidateDestination",
            StringComparison.Ordinal);
        int physicalConstruction = guardedEntryPoint.IndexOf(
            "using O5R5DotNetMeasurementSource source = new();",
            StringComparison.Ordinal);

        Assert.True(markerRead >= 0);
        Assert.True(inertReturn > markerRead);
        Assert.True(digestCheck > inertReturn);
        Assert.True(destinationCheck > digestCheck);
        Assert.True(physicalConstruction > destinationCheck);
        Assert.Contains(
            "[Trait(\"Category\", \"O5R5Physical\")]",
            entryPoint,
            StringComparison.Ordinal);
        Assert.Equal(
            1,
            CountOccurrences(guardedEntryPoint, "using O5R5DotNetMeasurementSource source = new();"));
    }

    /// <summary>Verifies the preregistered protocol document records the exact version, digest and prohibition.</summary>
    [Fact]
    public void ProtocolWasFrozenBeforeSyntheticExecution()
    {
        string protocol = Read(
            "docs",
            "STATE-06-MOD-12-O5-R5A-Physical-Measurement-Protocol.md");

        Assert.Contains("`o5r5a-physical-measurement-1.0.0`", protocol, StringComparison.Ordinal);
        Assert.Contains(ExpectedV1Digest, protocol, StringComparison.Ordinal);
        Assert.Contains("Physical results: prohibited in O5-R5-A.", protocol, StringComparison.Ordinal);
        Assert.Contains("`2,250 ms`", protocol, StringComparison.Ordinal);
        Assert.Contains("`3,000 ms`", protocol, StringComparison.Ordinal);
        Assert.Contains("`786,432 bytes`", protocol, StringComparison.Ordinal);
        Assert.Contains("`100 microseconds per work unit`", protocol, StringComparison.Ordinal);
        Assert.Contains("execute `5` warm-up samples", protocol, StringComparison.Ordinal);
        Assert.Contains("execute `30` measured samples", protocol, StringComparison.Ordinal);
        Assert.Contains("at or below `0.20`", protocol, StringComparison.Ordinal);
    }

    /// <summary>Verifies the v2 protocol remains immutable historical evidence of the failed campaign.</summary>
    [Fact]
    public void V2PhysicalProtocolRemainsPreservedAsHistoricalEvidence()
    {
        string protocol = Read(
            "docs",
            "STATE-06-MOD-12-PF-OBS-1-Physical-Measurement-Protocol-v2.md");

        Assert.Contains("`pfobs1-physical-measurement-2.0.0`", protocol, StringComparison.Ordinal);
        Assert.Contains(ExpectedV2Digest, protocol, StringComparison.Ordinal);
        Assert.Contains("five fixed sequential groups of six", protocol, StringComparison.Ordinal);
        Assert.Contains("Two complete consecutive campaigns", protocol, StringComparison.Ordinal);
        Assert.Contains("No measured sample may be removed", protocol, StringComparison.Ordinal);
    }

    /// <summary>Verifies D2 froze FirstByte v3 before any future physical campaign could run.</summary>
    [Fact]
    public void D2FirstByteProtocolIsFrozenBeforeFutureMeasurement()
    {
        string protocol = Read(
            "docs",
            "STATE-06-MOD-12-PF-OBS-1-Physical-Measurement-Protocol-v3.md");
        string runner = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5MeasurementReadinessRunner.cs");
        string pilot = Read("scripts", "run-pf-obs1-pilot.ps1");

        Assert.Contains("`pfobs1-physical-measurement-3.0.0`", protocol, StringComparison.Ordinal);
        Assert.Contains(ExpectedV3Digest, protocol, StringComparison.Ordinal);
        Assert.Contains("100,000", protocol, StringComparison.Ordinal);
        Assert.Contains("first observation", protocol, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No sample", protocol, StringComparison.Ordinal);
        Assert.Contains("FirstByteRepeatabilityWorkUnits = 100_000", runner, StringComparison.Ordinal);
        Assert.Contains("FirstByteFixedWindowElapsed", runner, StringComparison.Ordinal);
        Assert.Contains("o5r5d2.first-byte.checkpoint_invalid", runner, StringComparison.Ordinal);
        Assert.Contains("RequiredConsecutiveCampaigns = 2", runner, StringComparison.Ordinal);
        Assert.Contains("FixedGroupMedianDistribution", runner, StringComparison.Ordinal);
        Assert.Contains(
            "O5R5ThresholdMetric.WorkingSetPeak",
            runner,
            StringComparison.Ordinal);
        Assert.Contains(
            "workingSetPeak,\n            empiricalMemoryLimit",
            runner.ReplaceLineEndings("\n"),
            StringComparison.Ordinal);
        Assert.Contains("$requiredConsecutivePhysicalCampaigns = 2", pilot, StringComparison.Ordinal);
        Assert.Contains("no replacement run is permitted", pilot, StringComparison.Ordinal);
        Assert.Contains(ExpectedV3Digest, pilot, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies D3 removes only repeated Cancellation/Cold input allocation while preserving the
    /// frozen protocol, exact input size, unchanged memory gate and existing preconditioning.
    /// </summary>
    [Fact]
    public void D3CancellationRemediationIsBoundedAndPreservesTheFrozenProtocol()
    {
        string driver = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5PhysicalCampaignDriver.cs");
        string runner = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5MeasurementReadinessRunner.cs");

        Assert.Contains("O5R5CancellationInputBuffer", driver, StringComparison.Ordinal);
        Assert.Contains(
            "source.AsSpan().CopyTo(coldInput);",
            driver,
            StringComparison.Ordinal);
        Assert.Contains(
            "coldInput = GC.AllocateUninitializedArray<byte>(source.Length);",
            driver,
            StringComparison.Ordinal);
        Assert.Contains(
            "ReadOnlyMemory<byte> input = cancellationInput.Materialise(temperature);",
            driver,
            StringComparison.Ordinal);
        Assert.Contains(
            "internal const string Version = \"pfobs1-physical-measurement-3.0.0\";",
            runner,
            StringComparison.Ordinal);
        Assert.Contains(
            "internal const int MemoryHeadroomNumerator = 3;",
            runner,
            StringComparison.Ordinal);
        Assert.Contains(
            "internal const int MemoryHeadroomDenominator = 2;",
            runner,
            StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(driver, "GC.Collect("));
        Assert.DoesNotContain("ArrayPool", driver, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies D4 remains test-only, preserves the V3 campaign and excludes prohibited
    /// working-set manipulation from its diagnostic implementation.
    /// </summary>
    [Fact]
    public void D4DiagnosticsAreIsolatedAndDoNotChangeTheFrozenCampaign()
    {
        string diagnostics = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5CancellationWorkingSetDiagnostics.cs");
        string driver = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5PhysicalCampaignDriver.cs");
        string runner = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5MeasurementReadinessRunner.cs");
        string protocol = Read(
            "docs",
            "STATE-06-MOD-12-PF-OBS-1-D4-Cancellation-Diagnostic-Protocol.md");
        string sourceTree = string.Join(
            "\n",
            Directory
                .EnumerateFiles(
                    Path.Combine(RepositoryRoot(), "src"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.Contains(
            "pf-obs-1-d4-cancellation-diagnostic-test-only",
            diagnostics,
            StringComparison.Ordinal);
        Assert.Contains(
            "FBA236A73DCBA87BA7247394082080AF9EB72A7D8AFE8CD3667C44A24EA5EDB7",
            diagnostics,
            StringComparison.Ordinal);
        Assert.Contains(
            "`FBA236A73DCBA87BA7247394082080AF9EB72A7D8AFE8CD3667C44A24EA5EDB7`",
            protocol,
            StringComparison.Ordinal);
        Assert.Contains(
            "internal const string Version = \"pfobs1-physical-measurement-3.0.0\";",
            runner,
            StringComparison.Ordinal);
        Assert.Contains(
            "await Task.Delay(Timeout.InfiniteTimeSpan, inner.Token)",
            driver,
            StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(driver, "GC.Collect("));
        Assert.DoesNotContain("EmptyWorkingSet", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain("ProcessorAffinity", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain("PriorityClass", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain("O5R5D4", sourceTree, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "pf-obs-1-d4-cancellation-diagnostic-test-only",
            sourceTree,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies D5 is marker-gated, preserves the two existing V3 collections and introduces no
    /// working-set manipulation or normal-composition reference.
    /// </summary>
    [Fact]
    public void D5DiagnosticsPreserveExactHistoryAndRemainIsolated()
    {
        string diagnostics = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5ProcessHistoryDiagnostics.cs");
        string driver = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5PhysicalCampaignDriver.cs");
        string protocol = Read(
            "docs",
            "STATE-06-MOD-12-PF-OBS-1-D5-Process-History-Diagnostic-Protocol.md");
        string sourceTree = string.Join(
            "\n",
            Directory
                .EnumerateFiles(
                    Path.Combine(RepositoryRoot(), "src"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.Contains(
            "pf-obs-1-d5-process-history-diagnostic-test-only",
            diagnostics,
            StringComparison.Ordinal);
        Assert.Contains(
            "82606BF31085214607C8CBE401C0F4050D9465523B9B01F89FFE45731012FFDA",
            diagnostics,
            StringComparison.Ordinal);
        Assert.Contains(
            "`82606BF31085214607C8CBE401C0F4050D9465523B9B01F89FFE45731012FFDA`",
            protocol,
            StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(driver, "GC.Collect("));
        Assert.Contains(
            "internal async Task PreconditionAsync",
            driver,
            StringComparison.Ordinal);
        Assert.DoesNotContain("GC.Collect(", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain("EmptyWorkingSet", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain("ProcessorAffinity", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain("PriorityClass", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain("O5R5D5", sourceTree, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "pf-obs-1-d5-process-history-diagnostic-test-only",
            sourceTree,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies D6 restores the original four summaries, adds no in-process diagnostic captures and
    /// remains absent from normal composition.
    /// </summary>
    [Fact]
    public void D6RebaselinePreservesTheUnobservedV3PrefixAndRemainsIsolated()
    {
        string diagnostics = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5PostFormatNonIntrusiveRebaseline.cs");
        string driver = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5PhysicalCampaignDriver.cs");
        string protocol = Read(
            "docs",
            "STATE-06-MOD-12-PF-OBS-1-D6-Post-Format-Non-Intrusive-Rebaseline-Protocol.md");
        string sourceTree = string.Join(
            "\n",
            Directory
                .EnumerateFiles(
                    Path.Combine(RepositoryRoot(), "src"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.Contains(
            "pf-obs-1-d6-post-format-non-intrusive-test-only",
            diagnostics,
            StringComparison.Ordinal);
        Assert.Contains(
            "5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845",
            diagnostics,
            StringComparison.Ordinal);
        Assert.Contains(
            "`5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845`",
            protocol,
            StringComparison.Ordinal);
        Assert.Contains(
            "driver.CreateScenarios().Take(O5R5D6Protocol.ExpectedSampleCount)",
            diagnostics,
            StringComparison.Ordinal);
        Assert.Contains("runner.Summarise(", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain("VirtualQueryEx", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProcessMemoryInfo", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain("CaptureCommittedRegions", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain("O5R5D6", sourceTree, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "pf-obs-1-d6-post-format-non-intrusive-test-only",
            sourceTree,
            StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(driver, "GC.Collect("));
    }

    /// <summary>
    /// Verifies D7 preserves the first V3 group, keeps external snapshots in the supervisor and
    /// remains absent from normal composition.
    /// </summary>
    [Fact]
    public void D7RepeatabilityDiagnosticPreservesV3AndSeparatesExternalObservation()
    {
        string measured = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5FirstByteColdRepeatabilityDiagnostic.cs");
        string supervisor = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5FirstByteColdRepeatabilitySupervisor.cs");
        string driver = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5PhysicalCampaignDriver.cs");
        string proposal = Read(
            "docs",
            "STATE-06-MOD-12-PF-OBS-1-D7-FirstByte-Cold-Repeatability-Diagnostic-Proposal.md");
        string sourceTree = string.Join(
            "\n",
            Directory
                .EnumerateFiles(
                    Path.Combine(RepositoryRoot(), "src"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.Contains("pf-obs-1-d7-measured-test-only", measured, StringComparison.Ordinal);
        Assert.Contains("pf-obs-1-d7-supervisor-test-only", supervisor, StringComparison.Ordinal);
        Assert.Contains(
            "7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF",
            measured,
            StringComparison.Ordinal);
        Assert.Contains(
            "`7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF`",
            proposal,
            StringComparison.Ordinal);
        Assert.Contains(
            "driver.CreateScenarios().Take(O5R5D7Protocol.ExpectedSampleCount)",
            measured,
            StringComparison.Ordinal);
        Assert.Contains("runner.Summarise(samples)", measured, StringComparison.Ordinal);
        Assert.DoesNotContain("GetSystemTimes", measured, StringComparison.Ordinal);
        Assert.DoesNotContain("GlobalMemoryStatusEx", measured, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProcessMemoryInfo", measured, StringComparison.Ordinal);
        Assert.Contains("GetSystemTimes", supervisor, StringComparison.Ordinal);
        Assert.Contains("GlobalMemoryStatusEx", supervisor, StringComparison.Ordinal);
        Assert.Contains("QueryProcessCycleTime", supervisor, StringComparison.Ordinal);
        Assert.Contains("GetProcessMemoryInfo", supervisor, StringComparison.Ordinal);
        Assert.DoesNotContain("Thread.Sleep", supervisor, StringComparison.Ordinal);
        Assert.DoesNotContain("PeriodicTimer", supervisor, StringComparison.Ordinal);
        Assert.DoesNotContain("EventPipe", supervisor, StringComparison.Ordinal);
        Assert.DoesNotContain("TraceEvent", supervisor, StringComparison.Ordinal);
        Assert.DoesNotContain("PriorityClass", supervisor, StringComparison.Ordinal);
        Assert.DoesNotContain("ProcessorAffinity", supervisor, StringComparison.Ordinal);
        Assert.DoesNotContain("PowerSetActiveScheme", supervisor, StringComparison.Ordinal);
        Assert.DoesNotContain("GC.Collect(", measured, StringComparison.Ordinal);
        Assert.DoesNotContain("GC.Collect(", supervisor, StringComparison.Ordinal);
        Assert.DoesNotContain("O5R5D7", sourceTree, StringComparison.Ordinal);
        Assert.DoesNotContain("pf-obs-1-d7", sourceTree, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(driver, "GC.Collect("));
    }

    /// <summary>
    /// Verifies R1 wraps the unchanged D7 algorithm, requires real serialised counts and remains
    /// absent from normal composition.
    /// </summary>
    [Fact]
    public void D7R1AddsOnlyDistinctTestEvidenceAroundTheFrozenD7Algorithm()
    {
        string rerun = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5FirstByteColdRepeatabilityRerun.cs");
        string tests = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5FirstByteColdRepeatabilityRerunTests.cs");
        string host = Read(
            "tests",
            "DBNotifier.State06.ConsolidatedSandboxHost",
            "Program.cs");
        string proposal = Read(
            "docs",
            "STATE-06-MOD-12-PF-OBS-1-D7-R1-Summary-Count-Contract-Rerun-Proposal.md");
        string sourceTree = string.Join(
            "\n",
            Directory
                .EnumerateFiles(
                    Path.Combine(RepositoryRoot(), "src"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.Contains("pf-obs-1-d7-r1-measured-test-only", host, StringComparison.Ordinal);
        Assert.Contains("pf-obs-1-d7-r1-supervisor-test-only", host, StringComparison.Ordinal);
        Assert.Contains(
            "4FD5E92E4674BF93DF102DCFC564614E7D417C400292324A76E45011AE39F5C5",
            rerun,
            StringComparison.Ordinal);
        Assert.Contains(
            "`4FD5E92E4674BF93DF102DCFC564614E7D417C400292324A76E45011AE39F5C5`",
            proposal,
            StringComparison.Ordinal);
        Assert.Contains("O5R5D7MeasuredRunner runner = new(source)", rerun, StringComparison.Ordinal);
        Assert.DoesNotContain("class O5R5D7MeasuredRunner", rerun, StringComparison.Ordinal);
        Assert.Contains("expectedSummaryCount", rerun, StringComparison.Ordinal);
        Assert.Contains("completedSummaryCount", rerun, StringComparison.Ordinal);
        Assert.Contains("JsonDocument.Parse(bytes)", tests, StringComparison.Ordinal);
        Assert.Contains("O5R5D7R1EvidenceWriter.Deserialise(bytes)", tests, StringComparison.Ordinal);
        Assert.DoesNotContain("GetSystemTimes", rerun, StringComparison.Ordinal);
        Assert.DoesNotContain("GlobalMemoryStatusEx", rerun, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProcessMemoryInfo", rerun, StringComparison.Ordinal);
        Assert.DoesNotContain("Thread.Sleep", rerun, StringComparison.Ordinal);
        Assert.DoesNotContain("PeriodicTimer", rerun, StringComparison.Ordinal);
        Assert.DoesNotContain("EventPipe", rerun, StringComparison.Ordinal);
        Assert.DoesNotContain("TraceEvent", rerun, StringComparison.Ordinal);
        Assert.DoesNotContain("PriorityClass", rerun, StringComparison.Ordinal);
        Assert.DoesNotContain("ProcessorAffinity", rerun, StringComparison.Ordinal);
        Assert.DoesNotContain("PowerSetActiveScheme", rerun, StringComparison.Ordinal);
        Assert.DoesNotContain("GC.Collect(", rerun, StringComparison.Ordinal);
        Assert.DoesNotContain("O5R5D7R1", sourceTree, StringComparison.Ordinal);
        Assert.DoesNotContain("pf-obs-1-d7-r1", sourceTree, StringComparison.Ordinal);
        Assert.Equal(
            "A9DA5EEB6768E28189FCF4E0B7A3897FFA46F295C579B870E261681A588F3CEC",
            FileDigest(
                "tests",
                "DBNotifier.IntegrationTests",
                "O5R5FirstByteColdRepeatabilityDiagnostic.cs"));
        Assert.Equal(
            "BC2134B417AD57286AF7A2E2D000779C77508BBAC2F204F227B16A2FED26EAC2",
            FileDigest(
                "tests",
                "DBNotifier.IntegrationTests",
                "O5R5PhysicalCampaignDriver.cs"));
    }

    /// <summary>
    /// Verifies D8 wraps the unchanged D6 prefix, adds no capture or observer and remains absent
    /// from normal composition.
    /// </summary>
    [Fact]
    public void D8ReconcilesTheExactPrefixWithoutChangingMeasurement()
    {
        string reconciliation = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5ExactPrefixCompletionReconciliation.cs");
        string host = Read(
            "tests",
            "DBNotifier.State06.ConsolidatedSandboxHost",
            "Program.cs");
        string driver = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5PhysicalCampaignDriver.cs");
        string proposal = Read(
            "docs",
            "STATE-06-MOD-12-PF-OBS-1-D8-Exact-Prefix-Completion-Reconciliation-Proposal.md");
        string sourceTree = string.Join(
            "\n",
            Directory
                .EnumerateFiles(
                    Path.Combine(RepositoryRoot(), "src"),
                    "*.cs",
                    SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.Contains("pf-obs-1-d8-measured-test-only", host, StringComparison.Ordinal);
        Assert.Contains("pf-obs-1-d8-supervisor-test-only", host, StringComparison.Ordinal);
        Assert.Contains(
            "208DA70A8D638E50E2951DECDA83414B9E1F7CF4AFF957D09C1EAA2A8B1B8814",
            reconciliation,
            StringComparison.Ordinal);
        Assert.Contains(
            "`208DA70A8D638E50E2951DECDA83414B9E1F7CF4AFF957D09C1EAA2A8B1B8814`",
            proposal,
            StringComparison.Ordinal);
        Assert.Contains("O5R5D6Runner runner = new(source)", reconciliation, StringComparison.Ordinal);
        Assert.DoesNotContain("class O5R5D6Runner", reconciliation, StringComparison.Ordinal);
        Assert.DoesNotContain("GetSystemTimes", reconciliation, StringComparison.Ordinal);
        Assert.DoesNotContain("GlobalMemoryStatusEx", reconciliation, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProcessMemoryInfo", reconciliation, StringComparison.Ordinal);
        Assert.DoesNotContain("Thread.Sleep", reconciliation, StringComparison.Ordinal);
        Assert.DoesNotContain("PeriodicTimer", reconciliation, StringComparison.Ordinal);
        Assert.DoesNotContain("EventPipe", reconciliation, StringComparison.Ordinal);
        Assert.DoesNotContain("TraceEvent", reconciliation, StringComparison.Ordinal);
        Assert.DoesNotContain("GC.Collect(", reconciliation, StringComparison.Ordinal);
        Assert.DoesNotContain("O5R5D8", sourceTree, StringComparison.Ordinal);
        Assert.DoesNotContain("pf-obs-1-d8", sourceTree, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(driver, "GC.Collect("));
        Assert.Equal(
            "A9DA5EEB6768E28189FCF4E0B7A3897FFA46F295C579B870E261681A588F3CEC",
            FileDigest(
                "tests",
                "DBNotifier.IntegrationTests",
                "O5R5FirstByteColdRepeatabilityDiagnostic.cs"));
        Assert.Equal(
            "BC2134B417AD57286AF7A2E2D000779C77508BBAC2F204F227B16A2FED26EAC2",
            FileDigest(
                "tests",
                "DBNotifier.IntegrationTests",
                "O5R5PhysicalCampaignDriver.cs"));
    }

    /// <summary>Verifies failed physical evidence is committed before exit handling and temporary cleanup.</summary>
    [Fact]
    public void FailedPhysicalEvidenceIsRetainedBeforeCleanup()
    {
        string pilot = Read("scripts", "run-pf-obs1-pilot.ps1");
        int exitCapture = pilot.IndexOf(
            "$physicalExitCode = $LASTEXITCODE",
            StringComparison.Ordinal);
        int evidenceCopy = pilot.IndexOf(
            "Copy-PhysicalEvidenceAtomically",
            exitCapture,
            StringComparison.Ordinal);
        int failureBranch = pilot.IndexOf(
            "if ($physicalExitCode -ne 0)",
            exitCapture,
            StringComparison.Ordinal);

        Assert.True(exitCapture >= 0);
        Assert.True(evidenceCopy > exitCapture);
        Assert.True(failureBranch > evidenceCopy);
        Assert.Contains(
            "$evidenceRoot = Join-Path $repositoryRoot 'artifacts\\pf-obs-1'",
            pilot,
            StringComparison.Ordinal);
        Assert.Contains("[IO.File]::Move($temporary, $fullDestination, $true)", pilot);
        Assert.Contains("$output.Flush($true)", pilot);
        Assert.Contains("PF-OBS-1-D1 refused incomplete physical evidence.", pilot);
        Assert.DoesNotContain(
            "Copy-Item -LiteralPath $physicalOutput",
            pilot,
            StringComparison.Ordinal);
    }

    /// <summary>Verifies None remains the only activation state and normal composition stays dormant.</summary>
    [Fact]
    public void ObserverActivationRemainsNoneAndNormalCompositionRemainsDormant()
    {
        Assert.Equal(["None"], Enum.GetNames<ObserverActivationState>());
        string composition = Read(
            "src",
            "DBNotifier.Server.Api",
            "ObserverControlPlaneComposition.cs");

        Assert.Contains(
            "TryAddSingleton<IObserverActivationAuthority, UnavailableObserverActivationAuthority>()",
            composition,
            StringComparison.Ordinal);
        Assert.Contains(
            "TryAddSingleton<IObserverControlPlane, DormantObserverControlPlane>()",
            composition,
            StringComparison.Ordinal);
        Assert.DoesNotContain("O5R5", composition, StringComparison.Ordinal);
    }

    /// <summary>Returns whether a candidate file is generated build output rather than source.</summary>
    /// <param name="path">Absolute candidate path.</param>
    /// <returns><see langword="true"/> for bin or obj descendants.</returns>
    private static bool IsBuildOutput(string path) =>
        path.Contains(
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase) ||
        path.Contains(
            $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>Reads one exact repository file.</summary>
    /// <param name="path">Repository-relative path components.</param>
    /// <returns>The complete file content.</returns>
    private static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine([RepositoryRoot(), .. path]));

    /// <summary>Calculates one repository file SHA-256 without altering the source.</summary>
    /// <param name="path">Repository-relative path components.</param>
    /// <returns>An upper-case hexadecimal SHA-256 digest.</returns>
    private static string FileDigest(params string[] path) =>
        Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                File.ReadAllBytes(Path.Combine([RepositoryRoot(), .. path]))));

    /// <summary>Counts exact non-overlapping source occurrences.</summary>
    /// <param name="source">Complete source text.</param>
    /// <param name="value">Exact value to count.</param>
    /// <returns>Number of exact occurrences.</returns>
    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    /// <summary>Finds the repository root from the architecture-test output directory.</summary>
    /// <returns>The absolute repository root.</returns>
    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DBNotifier.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ??
            throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
