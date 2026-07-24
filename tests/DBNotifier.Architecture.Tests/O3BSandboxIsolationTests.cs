// Module purpose: Proves O3-B remains an exact opt-in test sandbox absent from every normal DB-Notifier composition.
using DBNotifier.Application.AIOps;
using Xunit;

namespace DBNotifier.Architecture.Tests;

/// <summary>Protects ActivationState.None, exact O3-B activation and dependency isolation.</summary>
public sealed class O3BSandboxIsolationTests
{
    /// <summary>Verifies product source and projects contain no O3-B evaluator, policy, marker or process bridge.</summary>
    [Fact]
    public void NormalCompositionContainsNoO3BSandboxReference()
    {
        string sourceRoot = Path.Combine(RepositoryRoot(), "src");
        string[] forbidden =
        [
            "o3b-governed-offline-evaluation-sandbox",
            "O3BOfflineEvaluator",
            "O3BSyntheticPolicyAuthority",
            "O3BSandboxProcess",
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

    /// <summary>Verifies the existing host and bridge both require the exact O3-B marker and bounded arguments.</summary>
    [Fact]
    public void ExistingSandboxHostRequiresExactO3BMarker()
    {
        string host = Read("tests", "DBNotifier.State06.ConsolidatedSandboxHost", "Program.cs");
        string process = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O3BGovernedOfflineEvaluationSandbox.cs");

        Assert.Contains("\"o3b-governed-offline-evaluation-sandbox\"", host, StringComparison.Ordinal);
        Assert.Contains("return await O3BSandboxProcess.RunAsync(args);", host, StringComparison.Ordinal);
        Assert.Contains(
            "internal const string ActivationMarker = \"o3b-governed-offline-evaluation-sandbox\";",
            process,
            StringComparison.Ordinal);
        Assert.Contains("args.Length != 4", process, StringComparison.Ordinal);
    }

    /// <summary>Verifies None remains the sole activation state after the O3-B sandbox is added.</summary>
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
        Assert.DoesNotContain("O3B", observer, StringComparison.Ordinal);
    }

    /// <summary>Verifies O3-B introduces no package declaration or normal product dependency on tests.</summary>
    [Fact]
    public void O3BSandboxUsesOnlyExistingTestDependencies()
    {
        string project = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "DBNotifier.IntegrationTests.csproj");
        Assert.DoesNotContain("O3B", project, StringComparison.Ordinal);
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
