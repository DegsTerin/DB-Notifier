// Module purpose: Proves O5-R3 observability remains test-only and cannot alter normal Observer composition or activation.
using DBNotifier.Application.AIOps;
using Xunit;

namespace DBNotifier.Architecture.Tests;

/// <summary>Protects the exact O5-R3 marker, dependency isolation and immutable inactive Observer state.</summary>
public sealed class O5R3SandboxIsolationTests
{
    /// <summary>Verifies normal product source contains no O5-R3 marker, catalogue, alert or runbook reference.</summary>
    [Fact]
    public void NormalCompositionContainsNoO5R3Reference()
    {
        string sourceRoot = Path.Combine(RepositoryRoot(), "src");
        string[] forbidden =
        [
            "DBNOTIFIER_O5_R3_TEST_ONLY",
            "O5R3Observability",
            "o5r3.alert.",
            "o5r3.runbook.",
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

    /// <summary>Verifies the sandbox uses the exact opt-in marker and no project or dependency change.</summary>
    [Fact]
    public void SandboxUsesExactMarkerAndExistingIntegrationBoundary()
    {
        string sandbox = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R3ObservabilitySandbox.cs");
        string project = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "DBNotifier.IntegrationTests.csproj");

        Assert.Contains(
            "internal const string Marker = \"DBNOTIFIER_O5_R3_TEST_ONLY\";",
            sandbox,
            StringComparison.Ordinal);
        Assert.Contains(
            "throw new InvalidOperationException(\"o5r3.sandbox.marker_required\");",
            sandbox,
            StringComparison.Ordinal);
        Assert.DoesNotContain("O5R3", project, StringComparison.Ordinal);
        Assert.DoesNotContain("PackageReference Include=\"", sandbox, StringComparison.Ordinal);
    }

    /// <summary>Verifies every O5-R3 source diagnostic is an exact code already emitted by the accepted O5-R2 sandbox.</summary>
    [Fact]
    public void CatalogueUsesOnlyAcceptedO5R2DiagnosticCodes()
    {
        string o5r2 = string.Concat(
            Read("tests", "DBNotifier.IntegrationTests", "O5R2ControlPlaneSandbox.cs"),
            Read("tests", "DBNotifier.IntegrationTests", "O5R2ControlPlaneStore.cs"));
        string o5r3 = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O5R3ObservabilitySandbox.cs");
        string[] expectedDiagnostics =
        [
            "o5r2.control.approval_capacity_exhausted",
            "o5r2.store.corrupt",
            "o5r2.control.deadline_expired",
            "o5r2.store.split_view",
            "o5r2.control.kill_switch_engaged",
            "o5r2.store.rollback",
        ];

        Assert.All(expectedDiagnostics, code =>
        {
            Assert.Contains($"\"{code}\"", o5r2, StringComparison.Ordinal);
            Assert.Contains($"\"{code}\"", o5r3, StringComparison.Ordinal);
        });
    }

    /// <summary>Verifies None remains the sole activation state and normal composition remains dormant and unavailable.</summary>
    [Fact]
    public void ActivationAndNormalCompositionRemainInactive()
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
        Assert.DoesNotContain("O5R3", composition, StringComparison.Ordinal);
    }

    /// <summary>Reads one exact repository file.</summary>
    /// <param name="path">Repository-relative path components.</param>
    /// <returns>The complete UTF-8 file content.</returns>
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
