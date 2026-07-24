// Module purpose: Proves O3-A remains an exact opt-in test sandbox absent from every normal DB-Notifier composition.
using DBNotifier.Application.AIOps;
using Xunit;

namespace DBNotifier.Architecture.Tests;

/// <summary>Protects ActivationState.None, exact O3-A activation and dependency isolation.</summary>
public sealed class O3ASandboxIsolationTests
{
    /// <summary>Verifies product source and projects contain no O3-A marker, verifier, authority or process bridge.</summary>
    [Fact]
    public void NormalCompositionContainsNoO3ASandboxReference()
    {
        string sourceRoot = Path.Combine(RepositoryRoot(), "src");
        string[] forbidden =
        [
            "o3a-governed-corpus-sandbox",
            "O3ACorpusVerifier",
            "O3ASyntheticCorpusAuthority",
            "O3ASandboxProcess",
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

    /// <summary>Verifies the existing host and bridge both require the exact O3-A marker and bounded arguments.</summary>
    [Fact]
    public void ExistingSandboxHostRequiresExactO3AMarker()
    {
        string host = Read("tests", "DBNotifier.State06.ConsolidatedSandboxHost", "Program.cs");
        string process = Read("tests", "DBNotifier.IntegrationTests", "O3AGovernedCorpusSandbox.cs");

        Assert.Contains("\"o3a-governed-corpus-sandbox\"", host, StringComparison.Ordinal);
        Assert.Contains("return await O3ASandboxProcess.RunAsync(args);", host, StringComparison.Ordinal);
        Assert.Contains(
            "internal const string ActivationMarker = \"o3a-governed-corpus-sandbox\";",
            process,
            StringComparison.Ordinal);
        Assert.Contains("args.Length != 4", process, StringComparison.Ordinal);
    }

    /// <summary>Verifies None remains the sole activation state after the O3-A sandbox is added.</summary>
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
        Assert.DoesNotContain("O3A", observer, StringComparison.Ordinal);
    }

    /// <summary>Verifies O3-A introduces no package declaration or normal product dependency on tests.</summary>
    [Fact]
    public void O3ASandboxUsesOnlyExistingTestDependencies()
    {
        string project = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "DBNotifier.IntegrationTests.csproj");
        Assert.DoesNotContain("O3A", project, StringComparison.Ordinal);
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
