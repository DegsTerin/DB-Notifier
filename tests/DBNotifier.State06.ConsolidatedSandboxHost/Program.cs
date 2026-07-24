// Module purpose: Validates exact local activation and delegates bounded STATE-06 or O1 evidence runs to test-only integration compositions.
using DBNotifier.IntegrationTests;

namespace DBNotifier.State06.ConsolidatedSandboxHost;

/// <summary>Provides the process boundary for one temporary, loopback-only and deliberately non-operational run.</summary>
internal static class Program
{
    private const string ActivationMarker = "state06-consolidated-e2e-sandbox";
    private const string HumanRemediationActivationMarker = "state06-final-human-samples-remediation";
    private const string HumanQualityGateSample = "quality-gate";

    /// <summary>Validates arguments, applies the fifteen-minute budget and runs the isolated test composition.</summary>
    /// <param name="args">Exact activation marker, already-built Dashboard root and runner-owned correlation identifier.</param>
    /// <returns>Zero after complete cleanup, two for invalid activation, or three for a sanitised harness failure.</returns>
    public static async Task<int> Main(string[] args)
    {
        if (args.Length >= 2 &&
            string.Equals(args[0], "--activation", StringComparison.Ordinal) &&
            string.Equals(args[1], "o1-durable-trust-resource-sandbox", StringComparison.Ordinal))
        {
            return await O1SandboxProcess.RunAsync(args);
        }

        if (args.Length >= 2 &&
            string.Equals(args[0], "--activation", StringComparison.Ordinal) &&
            string.Equals(
                args[1],
                "o2a-canonical-observation-pipeline-sandbox",
                StringComparison.Ordinal))
        {
            return await O2ASandboxProcess.RunAsync(args);
        }

        if (args.Length >= 2 &&
            string.Equals(args[0], "--activation", StringComparison.Ordinal) &&
            string.Equals(
                args[1],
                "o2b-durable-pipeline-sandbox",
                StringComparison.Ordinal))
        {
            return await O2BSandboxProcess.RunAsync(args);
        }

        if (args.Length >= 2 &&
            string.Equals(args[0], "--activation", StringComparison.Ordinal) &&
            string.Equals(
                args[1],
                "o3a-governed-corpus-sandbox",
                StringComparison.Ordinal))
        {
            return await O3ASandboxProcess.RunAsync(args);
        }

        if (args.Length >= 2 &&
            string.Equals(args[0], "--activation", StringComparison.Ordinal) &&
            string.Equals(
                args[1],
                "o3b-governed-offline-evaluation-sandbox",
                StringComparison.Ordinal))
        {
            return await O3BSandboxProcess.RunAsync(args);
        }

        if (!TryReadOptions(
                args,
                out string? dashboardRoot,
                out Guid runId,
                out bool humanRemediationMode,
                out string? humanReviewSample))
        {
            Console.Error.WriteLine("state06_consolidated_host.failed:activation_invalid");
            return 2;
        }

        using CancellationTokenSource budget = new(TimeSpan.FromMinutes(15));
        try
        {
            return humanRemediationMode
                ? await AgentFleetApiEndToEndTests.RunState06FinalHumanSamplesRemediationHostAsync(
                    dashboardRoot!,
                    humanReviewSample!,
                    runId,
                    budget.Token)
                : await AgentFleetApiEndToEndTests.RunState06ConsolidatedSandboxHostAsync(
                    dashboardRoot!,
                    runId,
                    budget.Token);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"state06_consolidated_host.failed:{exception.GetType().Name}");
            return 3;
        }
    }

    /// <summary>Accepts one of the two exact test-only markers and the canonical Dashboard output only.</summary>
    /// <param name="args">Untrusted process arguments.</param>
    /// <param name="dashboardRoot">Validated canonical Dashboard output root.</param>
    /// <param name="runId">Validated version-four correlation identifier created by the owning runner.</param>
    /// <param name="humanRemediationMode">Whether the final-human-sample evidence mode was selected.</param>
    /// <param name="humanReviewSample">Exact quality-gate or single-sample selector.</param>
    /// <returns><see langword="true"/> only for the exact local activation contract.</returns>
    private static bool TryReadOptions(
        string[] args,
        out string? dashboardRoot,
        out Guid runId,
        out bool humanRemediationMode,
        out string? humanReviewSample)
    {
        dashboardRoot = null;
        runId = Guid.Empty;
        humanRemediationMode = false;
        humanReviewSample = null;
        if (args.Length is not (6 or 8) ||
            !string.Equals(args[0], "--activation", StringComparison.Ordinal) ||
            !string.Equals(args[2], "--dashboard-root", StringComparison.Ordinal) ||
            !string.Equals(args[4], "--run-id", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(args[3]) ||
            !Guid.TryParseExact(args[5], "D", out runId) ||
            args[5][14] != '4' ||
            !"89abAB".Contains(args[5][19]) ||
            (!string.Equals(args[1], ActivationMarker, StringComparison.Ordinal) &&
             !string.Equals(args[1], HumanRemediationActivationMarker, StringComparison.Ordinal)))
        {
            return false;
        }

        humanRemediationMode = string.Equals(
            args[1],
            HumanRemediationActivationMarker,
            StringComparison.Ordinal);
        if (args.Length == 8)
        {
            if (!humanRemediationMode ||
                !string.Equals(args[6], "--sample", StringComparison.Ordinal) ||
                args[7] is not ("S06-HG-001" or "S06-HG-006"))
            {
                return false;
            }
            humanReviewSample = args[7];
        }
        else if (humanRemediationMode)
        {
            humanReviewSample = HumanQualityGateSample;
        }

        DirectoryInfo? repository = new(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "DBNotifier.sln")))
        {
            repository = repository.Parent;
        }
        if (repository is null)
        {
            return false;
        }

        string expected = Path.GetFullPath(Path.Combine(
            repository.FullName,
            "src",
            "DBNotifier.Dashboard.Web",
            "dist"));
        string candidate;
        try
        {
            candidate = Path.GetFullPath(args[3]);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        if (!string.Equals(candidate, expected, StringComparison.OrdinalIgnoreCase) ||
            !Directory.Exists(candidate) ||
            !File.Exists(Path.Combine(candidate, "index.html")))
        {
            return false;
        }

        dashboardRoot = candidate;
        return true;
    }
}
