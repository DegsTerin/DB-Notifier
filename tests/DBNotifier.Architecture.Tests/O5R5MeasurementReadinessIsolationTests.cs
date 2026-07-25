// Module purpose: Proves O5-R5-A remains test-only, uses the frozen protocol and cannot run a physical campaign now.
using DBNotifier.Application.AIOps;
using Xunit;

namespace DBNotifier.Architecture.Tests;

/// <summary>
/// Protects normal-composition isolation, built-in counter provenance, synthetic-only validation
/// and the immutable inactive Observer state for O5-R5-A.
/// </summary>
public sealed class O5R5MeasurementReadinessIsolationTests
{
    private const string ExpectedDigest =
        "266B7A952DF1A46BEE4577894D0A9206D92917AC661E0E17F9052DE1EB415DD7";

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
            "process.WorkingSet64",
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

    /// <summary>Verifies O5-R5-A tests use only synthetic sources and cannot execute host measurements.</summary>
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
    }

    /// <summary>Verifies the preregistered protocol document records the exact version, digest and prohibition.</summary>
    [Fact]
    public void ProtocolWasFrozenBeforeSyntheticExecution()
    {
        string protocol = Read(
            "docs",
            "STATE-06-MOD-12-O5-R5A-Physical-Measurement-Protocol.md");

        Assert.Contains("`o5r5a-physical-measurement-1.0.0`", protocol, StringComparison.Ordinal);
        Assert.Contains(ExpectedDigest, protocol, StringComparison.Ordinal);
        Assert.Contains("Physical results: prohibited in O5-R5-A.", protocol, StringComparison.Ordinal);
        Assert.Contains("`2,250 ms`", protocol, StringComparison.Ordinal);
        Assert.Contains("`3,000 ms`", protocol, StringComparison.Ordinal);
        Assert.Contains("`786,432 bytes`", protocol, StringComparison.Ordinal);
        Assert.Contains("`100 microseconds per work unit`", protocol, StringComparison.Ordinal);
        Assert.Contains("execute `5` warm-up samples", protocol, StringComparison.Ordinal);
        Assert.Contains("execute `30` measured samples", protocol, StringComparison.Ordinal);
        Assert.Contains("at or below `0.20`", protocol, StringComparison.Ordinal);
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
