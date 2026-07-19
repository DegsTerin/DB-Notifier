// Module purpose: Proves the SignalR change-hint composition remains opt-in, sandbox-only and absent from normal API startup.
using System.Text.Json;

namespace DBNotifier.Architecture.Tests;

/// <summary>Protects the source-level activation boundary around the optional Dashboard TV SignalR client and hub.</summary>
public sealed class DashboardTvSignalRIsolationTests
{
    [Fact]
    public void NormalApiDoesNotRegisterOrMapTheSignalRSandbox()
    {
        string root = RepositoryRoot();
        string normalApi = File.ReadAllText(Path.Combine(root, "src", "DBNotifier.Server.Api", "Program.cs"));
        string browserHost = File.ReadAllText(Path.Combine(root, "tests", "DBNotifier.DashboardTv.BrowserSandboxHost", "Program.cs"));

        Assert.DoesNotContain("AddDashboardTvChangeHintSandbox", normalApi, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDashboardTvChangeHintSandbox", normalApi, StringComparison.Ordinal);
        Assert.Contains("AddDashboardTvChangeHintSandbox", browserHost, StringComparison.Ordinal);
        Assert.Contains("MapDashboardTvChangeHintSandbox", browserHost, StringComparison.Ordinal);
    }

    [Fact]
    public void OfficialClientIsExactlyPinnedAndLoadedOnlyByTheGuardedTvComposition()
    {
        string root = RepositoryRoot();
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src",
            "DBNotifier.Dashboard.Web",
            "package.json")));
        string application = File.ReadAllText(Path.Combine(root, "src", "DBNotifier.Dashboard.Web", "src", "App.tsx"));

        Assert.Equal("10.0.0", manifest.RootElement.GetProperty("dependencies").GetProperty("@microsoft/signalr").GetString());
        Assert.Contains("isDashboardTvSandboxEnabled", application, StringComparison.Ordinal);
        Assert.Contains("import(\"./dashboardTvChangeHints\")", application, StringComparison.Ordinal);
        Assert.DoesNotContain("from \"./dashboardTvChangeHints\"", application, StringComparison.Ordinal);
    }

    /// <summary>Finds the repository root without depending on the test command's working directory.</summary>
    /// <returns>The directory containing both the solution and repository instruction file.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the compiled test output is outside the repository.</exception>
    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               (!File.Exists(Path.Combine(directory.FullName, "DBNotifier.sln")) ||
                !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("The DB-Notifier repository root could not be resolved.");
    }
}
