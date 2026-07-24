// Module purpose: Proves O4 remains an exact opt-in UI/API sandbox absent from normal DB-Notifier composition.
using DBNotifier.Application.AIOps;
using Xunit;

namespace DBNotifier.Architecture.Tests;

/// <summary>Protects ActivationState.None, normal routing and the exact O4 process bridge.</summary>
public sealed class O4SandboxIsolationTests
{
    /// <summary>Verifies the normal Server, App routing and Dashboard route registry contain no O4 activation.</summary>
    [Fact]
    public void NormalCompositionContainsNoObserverActivationOrRoute()
    {
        string server = Read("src", "DBNotifier.Server.Api", "Program.cs");
        string app = Read("src", "DBNotifier.Dashboard.Web", "src", "App.tsx");
        string routes = Read("src", "DBNotifier.Dashboard.Web", "src", "routing.ts");

        Assert.DoesNotContain("o4-factual-observer-projection-sandbox", server, StringComparison.Ordinal);
        Assert.DoesNotContain("O4SandboxProcess", server, StringComparison.Ordinal);
        Assert.DoesNotContain("ObserverSandboxApp", app, StringComparison.Ordinal);
        Assert.DoesNotContain("\"observer\"", routes, StringComparison.Ordinal);
        Assert.Equal(["None"], Enum.GetNames<ObserverActivationState>());
    }

    /// <summary>Verifies the existing test host delegates only the exact O4 marker to the isolated process.</summary>
    [Fact]
    public void ExistingSandboxHostRequiresExactO4Marker()
    {
        string host = Read(
            "tests",
            "DBNotifier.State06.ConsolidatedSandboxHost",
            "Program.cs");
        string process = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "O4FactualObserverSandbox.cs");

        Assert.Contains(
            "\"o4-factual-observer-projection-sandbox\"",
            host,
            StringComparison.Ordinal);
        Assert.Contains(
            "return await O4SandboxProcess.RunAsync(args);",
            host,
            StringComparison.Ordinal);
        Assert.Contains(
            "internal const string ActivationMarker = \"o4-factual-observer-projection-sandbox\";",
            process,
            StringComparison.Ordinal);
        Assert.Contains("args.Length != 6", process, StringComparison.Ordinal);
    }

    /// <summary>Verifies the dedicated UI requires the exact build flag and the normal projects do not depend on tests.</summary>
    [Fact]
    public void DashboardGuardAndProjectDependenciesRemainFailClosed()
    {
        string main = Read(
            "src",
            "DBNotifier.Dashboard.Web",
            "src",
            "main.tsx");
        string adapter = Read(
            "src",
            "DBNotifier.Dashboard.Web",
            "src",
            "observerSandbox.ts");
        Assert.Contains(
            "environment.VITE_DB_NOTIFIER_OBSERVER_SANDBOX === \"local-test\"",
            adapter,
            StringComparison.Ordinal);
        Assert.Contains("location.protocol === \"https:\"", adapter, StringComparison.Ordinal);
        Assert.Contains(
            "import.meta.env.VITE_DB_NOTIFIER_OBSERVER_SANDBOX === \"local-test\"",
            main,
            StringComparison.Ordinal);
        Assert.Contains(
            "import(\"./ObserverSandboxApp\")",
            main,
            StringComparison.Ordinal);
        Assert.Contains(
            "import(\"./observerSandbox\")",
            main,
            StringComparison.Ordinal);
        Assert.Contains(
            "isObserverSandboxEnabled(import.meta.env, window.location)",
            main,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "import { ObserverSandboxApp }",
            main,
            StringComparison.Ordinal);

        string[] productReferences = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot(), "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("DBNotifier.IntegrationTests", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(RepositoryRoot(), path))
            .ToArray();
        Assert.Empty(productReferences);
    }

    /// <summary>Verifies the isolated Observer view retains its factual hierarchy and early responsive stacking.</summary>
    [Fact]
    public void ObserverPresentationRetainsOrganisedReadOnlyHierarchy()
    {
        string app = Read(
            "src",
            "DBNotifier.Dashboard.Web",
            "src",
            "ObserverSandboxApp.tsx");
        string styles = Read(
            "src",
            "DBNotifier.Dashboard.Web",
            "src",
            "observerSandbox.css");

        Assert.Contains("className=\"observer-summary\"", app, StringComparison.Ordinal);
        Assert.Contains("ObserverSummaryCard", app, StringComparison.Ordinal);
        Assert.Contains("detailsLabel={ot(\"FullIdentifier\")}", app, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 1120px)", styles, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: 1fr;", styles, StringComparison.Ordinal);
        Assert.DoesNotContain(".observer-signals { grid-row: span 2; }", styles, StringComparison.Ordinal);
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
        return directory?.FullName ??
            throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
