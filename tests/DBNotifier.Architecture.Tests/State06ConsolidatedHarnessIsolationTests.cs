// Module purpose: Guards the test-only activation, dependency and toolchain boundaries of the consolidated STATE-06 remediation harness.
using System.Xml.Linq;

namespace DBNotifier.Architecture.Tests;

/// <summary>Prevents the correlated evidence host from entering normal product composition or acquiring packages.</summary>
public sealed class State06ConsolidatedHarnessIsolationTests
{
    /// <summary>Confirms normal Server and Agent startup contain no consolidated test-host activation.</summary>
    [Fact]
    public void NormalRuntimeDoesNotComposeTheConsolidatedHarness()
    {
        string server = Read("src", "DBNotifier.Server.Api", "Program.cs");
        string agent = Read("src", "DBNotifier.Agent.Worker", "Program.cs");

        Assert.DoesNotContain("State06.ConsolidatedSandboxHost", server, StringComparison.Ordinal);
        Assert.DoesNotContain("state06-consolidated-e2e-sandbox", server, StringComparison.Ordinal);
        Assert.DoesNotContain("state06-final-human-samples-remediation", server, StringComparison.Ordinal);
        Assert.DoesNotContain("State06.ConsolidatedSandboxHost", agent, StringComparison.Ordinal);
        Assert.DoesNotContain("state06-consolidated-e2e-sandbox", agent, StringComparison.Ordinal);
        Assert.DoesNotContain("state06-final-human-samples-remediation", agent, StringComparison.Ordinal);
    }

    /// <summary>Confirms the dedicated executable is test-only and adds no package dependency.</summary>
    [Fact]
    public void ConsolidatedHostReferencesOnlyTheExistingIntegrationHarness()
    {
        string projectPath = Path.Combine(
            RepositoryRoot(),
            "tests",
            "DBNotifier.State06.ConsolidatedSandboxHost",
            "DBNotifier.State06.ConsolidatedSandboxHost.csproj");
        XDocument project = XDocument.Load(projectPath);
        XElement[] packageReferences = project.Descendants("PackageReference").ToArray();
        XElement[] projectReferences = project.Descendants("ProjectReference").ToArray();

        Assert.Empty(packageReferences);
        XElement reference = Assert.Single(projectReferences);
        Assert.Equal(
            "..\\DBNotifier.IntegrationTests\\DBNotifier.IntegrationTests.csproj",
            reference.Attribute("Include")?.Value);
    }

    /// <summary>Confirms modern browser runners refuse pre-PowerShell-7 execution before their bodies run.</summary>
    [Fact]
    public void ModernBrowserRunnersDeclarePowerShellSeven()
    {
        foreach (string runner in new[]
        {
            "run-state06-dashboard-tv-browser-e2e.ps1",
            "run-state06-consolidated-e2e.ps1",
            "run-state06-final-human-review.ps1",
        })
        {
            string contents = Read("scripts", runner);
            Assert.StartsWith(
                "# Module purpose:",
                contents,
                StringComparison.Ordinal);
            Assert.Contains("#Requires -Version 7.0", contents, StringComparison.Ordinal);
        }
    }

    /// <summary>Confirms the runner uses an exact opt-in and contains no installation or download command.</summary>
    [Fact]
    public void ConsolidatedRunnerIsExactAndOffline()
    {
        string runner = Read("scripts", "run-state06-consolidated-e2e.ps1");

        Assert.Contains("state06-consolidated-e2e-sandbox", runner, StringComparison.Ordinal);
        Assert.Contains("state06-final-human-samples-remediation", runner, StringComparison.Ordinal);
        Assert.Contains("--no-restore", runner, StringComparison.Ordinal);
        Assert.DoesNotContain("npm install", runner, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("npm ci", runner, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Start-BitsTransfer", runner, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Confirms terminal evidence is allow-listed and revocation scenarios do not use the wall clock as a gate.</summary>
    [Fact]
    public void ConsolidatedRevocationDiagnosticsAreSanitisedAndDeterministic()
    {
        string harness = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "AgentFleetApiEndToEndTests.ConsolidatedHarness.cs");
        string scenarios = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "AgentFleetApiEndToEndTests.ConsolidatedRevocation.cs");
        string auditor = Read("scripts", "audit-state06-consolidated-e2e.mjs");

        Assert.Contains("ConsolidatedHarnessFailureEvidence", harness, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", harness, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.StackTrace", harness, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow - owner.Now", harness, StringComparison.Ordinal);
        Assert.DoesNotContain("body.slice", auditor, StringComparison.Ordinal);
        Assert.Contains("sanitiseProblem", auditor, StringComparison.Ordinal);
        foreach (string scenario in new[] { "R1", "R2", "R3", "R4", "R5", "R6", "R7" })
        {
            Assert.Contains($"\"{scenario}\"", scenarios, StringComparison.Ordinal);
        }
    }

    /// <summary>Confirms the blocked-sample evidence remains test-only, offline and distinct from browser offline.</summary>
    [Fact]
    public void FinalHumanSampleRemediationIsIsolatedSanitisedAndFactuallyDistinct()
    {
        string harness = Read(
            "tests",
            "DBNotifier.IntegrationTests",
            "AgentFleetApiEndToEndTests.ConsolidatedHarness.cs");
        string auditor = Read("scripts", "audit-state06-final-human-samples-remediation.mjs");
        string presenter = Read("scripts", "present-state06-final-human-review.mjs");
        string reviewRunner = Read("scripts", "run-state06-final-human-review.ps1");
        string server = Read("src", "DBNotifier.Server.Api", "Program.cs");
        string dashboard = Read("src", "DBNotifier.Dashboard.Web", "src", "App.tsx");

        Assert.Contains("ConsolidatedHumanSampleSnapshotSource", harness, StringComparison.Ordinal);
        Assert.Contains("agent-observation-pending", harness, StringComparison.Ordinal);
        Assert.Contains("Evidência do harness — não é estado operacional", harness, StringComparison.Ordinal);
        Assert.Contains("unknown", harness, StringComparison.Ordinal);
        Assert.Contains("fixture-stale", harness, StringComparison.Ordinal);
        Assert.Contains("HumanEvidenceRefreshMilliseconds = 2100", harness, StringComparison.Ordinal);
        Assert.Contains("Browser → API temporariamente limitada pelo sandbox", harness, StringComparison.Ordinal);
        Assert.Contains("Browser → API: acesso de teste negado", harness, StringComparison.Ordinal);
        Assert.DoesNotContain("setInterval", harness, StringComparison.Ordinal);
        Assert.Contains("durationGateMilliseconds = 180_000", auditor, StringComparison.Ordinal);
        Assert.DoesNotContain("Network.emulateNetworkConditions", auditor, StringComparison.Ordinal);
        Assert.Contains("S06-HG-001", reviewRunner, StringComparison.Ordinal);
        Assert.Contains("S06-HG-006", reviewRunner, StringComparison.Ordinal);
        Assert.Contains("--sample", reviewRunner, StringComparison.Ordinal);
        Assert.DoesNotContain("--headless", reviewRunner, StringComparison.Ordinal);
        Assert.Contains("humanDecisionRecorded: false", presenter, StringComparison.Ordinal);
        Assert.Contains("qualityGateAutomation", presenter, StringComparison.Ordinal);
        Assert.DoesNotContain("state06-final-human-samples-remediation", server, StringComparison.Ordinal);
        Assert.DoesNotContain("state06-final-human-samples-remediation", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("npm install", auditor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", auditor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("npm install", presenter, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", presenter, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Start-BitsTransfer", reviewRunner, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Reads one repository-relative UTF-8 source file.</summary>
    /// <param name="path">Path segments beneath the repository root.</param>
    /// <returns>Complete source text.</returns>
    private static string Read(params string[] path) => File.ReadAllText(
        Path.Combine([RepositoryRoot(), .. path]));

    /// <summary>Finds the repository root independently of the current test working directory.</summary>
    /// <returns>Directory containing the solution and instruction file.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the test output is outside the repository.</exception>
    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               (!File.Exists(Path.Combine(directory.FullName, "DBNotifier.sln")) ||
                !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ??
            throw new DirectoryNotFoundException("The DB-Notifier repository root could not be resolved.");
    }
}
