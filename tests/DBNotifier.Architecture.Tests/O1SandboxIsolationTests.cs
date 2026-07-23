// Module purpose: Proves the O1 implementation remains an exact opt-in test sandbox and cannot enter normal DB-Notifier composition.

namespace DBNotifier.Architecture.Tests;

/// <summary>Protects ActivationState.None and the absence of O1 sandbox references from every normal product composition.</summary>
public sealed class O1SandboxIsolationTests
{
    /// <summary>Verifies that no product source or project can name the O1 marker, coordinator, store or process bridge.</summary>
    [Fact]
    public void NormalCompositionContainsNoO1SandboxReference()
    {
        string sourceRoot = Path.Combine(RepositoryRoot(), "src");
        string[] forbidden =
        [
            "o1-durable-trust-resource-sandbox",
            "O1TrustCoordinator",
            "O1SandboxStore",
            "O1SandboxProcess",
        ];
        string[] violations = Directory
            .EnumerateFiles(sourceRoot, "*.*", SearchOption.AllDirectories)
            .Where(
                path => Path.GetExtension(path) is ".cs" or ".csproj" &&
                    !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
                    !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
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

    /// <summary>Verifies that the existing test-only host exposes the exact marker and no broader prefix or normal fallback.</summary>
    [Fact]
    public void ExistingSandboxHostRequiresTheExactO1Marker()
    {
        string host = Read(
            "tests",
            "DBNotifier.State06.ConsolidatedSandboxHost",
            "Program.cs");
        string process = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O1SandboxStore.cs");

        Assert.Contains(
            "string.Equals(args[1], \"o1-durable-trust-resource-sandbox\", StringComparison.Ordinal)",
            host,
            StringComparison.Ordinal);
        Assert.Contains("return await O1SandboxProcess.RunAsync(args);", host, StringComparison.Ordinal);
        Assert.Contains(
            "private const string ActivationMarker = \"o1-durable-trust-resource-sandbox\";",
            process,
            StringComparison.Ordinal);
        Assert.Contains("args[1] != ActivationMarker", process, StringComparison.Ordinal);
    }

    /// <summary>Verifies the application mode remains immutable None and has no O1 runtime registration.</summary>
    [Fact]
    public void ObserverActivationStateRemainsNone()
    {
        string observer = Read(
            "src",
            "DBNotifier.Application",
            "AIOps",
            "ObserverAnalysisService.cs");
        Assert.Contains(
            "public ObserverActivationState ActivationState { get; } = ObserverActivationState.None;",
            observer,
            StringComparison.Ordinal);
        Assert.DoesNotContain("O1", observer, StringComparison.Ordinal);
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
