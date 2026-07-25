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

    /// <summary>Verifies the scheduler-robust PF-OBS-1 protocol was frozen before resumed measurement.</summary>
    [Fact]
    public void CorrectedPhysicalProtocolIsFrozenBeforeResumedCampaign()
    {
        string protocol = Read(
            "docs",
            "STATE-06-MOD-12-PF-OBS-1-Physical-Measurement-Protocol-v2.md");
        string runner = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R5MeasurementReadinessRunner.cs");
        string pilot = Read("scripts", "run-pf-obs1-pilot.ps1");

        Assert.Contains("`pfobs1-physical-measurement-2.0.0`", protocol, StringComparison.Ordinal);
        Assert.Contains(ExpectedV2Digest, protocol, StringComparison.Ordinal);
        Assert.Contains("five fixed sequential groups of six", protocol, StringComparison.Ordinal);
        Assert.Contains("Two complete consecutive campaigns", protocol, StringComparison.Ordinal);
        Assert.Contains("No measured sample may be removed", protocol, StringComparison.Ordinal);
        Assert.Contains("RequiredConsecutiveCampaigns = 2", runner, StringComparison.Ordinal);
        Assert.Contains("FixedGroupMedianDistribution", runner, StringComparison.Ordinal);
        Assert.Contains("workingSetPeak <= empiricalMemoryLimit", runner, StringComparison.Ordinal);
        Assert.Contains("$requiredConsecutivePhysicalCampaigns = 2", pilot, StringComparison.Ordinal);
        Assert.Contains("no replacement run is permitted", pilot, StringComparison.Ordinal);
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
