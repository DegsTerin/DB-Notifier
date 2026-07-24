// Module purpose: Proves O2-A remains an exact opt-in test sandbox and cannot enter normal DB-Notifier composition.
using DBNotifier.Application.AIOps;

namespace DBNotifier.Architecture.Tests;

/// <summary>
/// Protects ActivationState.None, the exact O2-A marker and the absence of canonical-pipeline references from product
/// source and normal composition.
/// </summary>
public sealed class O2ASandboxIsolationTests
{
    /// <summary>Verifies that no product source or project names any O2-A marker, pipeline or process bridge.</summary>
    [Fact]
    public void NormalCompositionContainsNoO2ASandboxReference()
    {
        string sourceRoot = Path.Combine(RepositoryRoot(), "src");
        string[] forbidden =
        [
            "o2a-canonical-observation-pipeline-sandbox",
            "O2ACanonicalObservationPipeline",
            "O2ASandboxProcess",
            "O2ALoopbackObservationTransport",
        ];
        string[] violations = Directory
            .EnumerateFiles(sourceRoot, "*.*", SearchOption.AllDirectories)
            .Where(
                path => Path.GetExtension(path) is ".cs" or ".csproj" &&
                    !path.Contains(
                        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                        StringComparison.OrdinalIgnoreCase) &&
                    !path.Contains(
                        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                        StringComparison.OrdinalIgnoreCase))
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

    /// <summary>Verifies that the existing test host and process entry point both demand the exact O2-A marker.</summary>
    [Fact]
    public void ExistingSandboxHostRequiresTheExactO2AMarker()
    {
        string host = Read(
            "tests",
            "DBNotifier.State06.ConsolidatedSandboxHost",
            "Program.cs");
        string process = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O2ACanonicalObservationPipeline.cs");

        Assert.Contains(
            "\"o2a-canonical-observation-pipeline-sandbox\"",
            host,
            StringComparison.Ordinal);
        Assert.Contains("return await O2ASandboxProcess.RunAsync(args);", host, StringComparison.Ordinal);
        Assert.Contains(
            "private const string ActivationMarker = \"o2a-canonical-observation-pipeline-sandbox\";",
            process,
            StringComparison.Ordinal);
        Assert.Contains("args[1] != ActivationMarker", process, StringComparison.Ordinal);
        Assert.Contains("args.Length != 6", process, StringComparison.Ordinal);
    }

    /// <summary>Verifies that None remains the only declared activation state after adding the O2-A sandbox.</summary>
    [Fact]
    public void ObserverActivationStateStillDeclaresOnlyNone()
    {
        Assert.Equal(["None"], Enum.GetNames<ObserverActivationState>());

        string observer = Read(
            "src",
            "DBNotifier.Application",
            "AIOps",
            "ObserverAnalysisService.cs");
        Assert.Contains(
            "public ObserverActivationState ActivationState { get; } = ObserverActivationState.None;",
            observer,
            StringComparison.Ordinal);
        Assert.DoesNotContain("O2A", observer, StringComparison.Ordinal);
    }

    /// <summary>Verifies that O2-A changes neither package declarations nor product-project references.</summary>
    [Fact]
    public void O2ASandboxUsesOnlyExistingTestProjectReferences()
    {
        string project = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "DBNotifier.IntegrationTests.csproj");
        Assert.DoesNotContain("O2A", project, StringComparison.Ordinal);

        string[] normalReferences = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot(), "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(
                path => File.ReadAllText(path).Contains(
                    "DBNotifier.IntegrationTests",
                    StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(RepositoryRoot(), path))
            .ToArray();
        Assert.Empty(normalReferences);
    }

    /// <summary>Reads one exact repository file.</summary>
    private static string Read(params string[] path) =>
        File.ReadAllText(Path.Combine([RepositoryRoot(), .. path]));

    /// <summary>Finds the repository root from the architecture-test output directory.</summary>
    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DBNotifier.sln")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
