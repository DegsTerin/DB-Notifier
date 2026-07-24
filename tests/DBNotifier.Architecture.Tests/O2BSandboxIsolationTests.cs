// Module purpose: Proves O2-B remains an exact opt-in test sandbox with no normal DB-Notifier composition reference.
using DBNotifier.Application.AIOps;

namespace DBNotifier.Architecture.Tests;

/// <summary>Protects ActivationState.None, the exact O2-B marker and dependency isolation from product source.</summary>
public sealed class O2BSandboxIsolationTests
{
    /// <summary>Verifies that no product source or project names any O2-B store, pipeline, marker or process bridge.</summary>
    [Fact]
    public void NormalCompositionContainsNoO2BSandboxReference()
    {
        string sourceRoot = Path.Combine(RepositoryRoot(), "src");
        string[] forbidden =
        [
            "o2b-durable-pipeline-sandbox",
            "O2BDurableObservationPipeline",
            "O2BDurablePipelineStore",
            "O2BSandboxProcess",
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

    /// <summary>Verifies the existing host and process entry point both require the exact O2-B marker.</summary>
    [Fact]
    public void ExistingSandboxHostRequiresExactO2BMarker()
    {
        string host = Read("tests", "DBNotifier.State06.ConsolidatedSandboxHost", "Program.cs");
        string process = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O2BDurableObservationPipeline.cs");
        Assert.Contains("\"o2b-durable-pipeline-sandbox\"", host, StringComparison.Ordinal);
        Assert.Contains("return await O2BSandboxProcess.RunAsync(args);", host, StringComparison.Ordinal);
        Assert.Contains(
            "private const string ActivationMarker = \"o2b-durable-pipeline-sandbox\";",
            process,
            StringComparison.Ordinal);
        Assert.Contains("args[1] != ActivationMarker", process, StringComparison.Ordinal);
        Assert.Contains("args.Length != 6", process, StringComparison.Ordinal);
    }

    /// <summary>Verifies None remains the only activation state after adding the durable O2-B sandbox.</summary>
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
        Assert.DoesNotContain("O2B", observer, StringComparison.Ordinal);
    }

    /// <summary>Verifies O2-B adds neither package declarations nor product references to the test assembly.</summary>
    [Fact]
    public void O2BSandboxUsesOnlyExistingTestDependencies()
    {
        string project = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "DBNotifier.IntegrationTests.csproj");
        Assert.DoesNotContain("O2B", project, StringComparison.Ordinal);
        string[] normalReferences = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot(), "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("DBNotifier.IntegrationTests", StringComparison.Ordinal))
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
