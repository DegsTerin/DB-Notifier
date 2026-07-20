// Module purpose: Validates exact local activation and delegates one bounded consolidated STATE-06 evidence run to the test-only integration composition.
using DBNotifier.IntegrationTests;

namespace DBNotifier.State06.ConsolidatedSandboxHost;

/// <summary>Provides the process boundary for one temporary, loopback-only and deliberately non-operational run.</summary>
internal static class Program
{
    private const string ActivationMarker = "state06-consolidated-e2e-sandbox";
    private const string HumanRemediationActivationMarker = "state06-final-human-samples-remediation";
    private const string HumanQualityGateSample = "quality-gate";

    /// <summary>Validates arguments, applies the fifteen-minute budget and runs the isolated test composition.</summary>
    /// <param name="args">Exact activation marker and already-built Dashboard root.</param>
    /// <returns>Zero after complete cleanup, two for invalid activation, or three for a sanitised harness failure.</returns>
    public static async Task<int> Main(string[] args)
    {
        if (!TryReadOptions(
                args,
                out string? dashboardRoot,
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
                    budget.Token)
                : await AgentFleetApiEndToEndTests.RunState06ConsolidatedSandboxHostAsync(
                    dashboardRoot!,
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
    /// <param name="humanRemediationMode">Whether the final-human-sample evidence mode was selected.</param>
    /// <param name="humanReviewSample">Exact quality-gate or single-sample selector.</param>
    /// <returns><see langword="true"/> only for the exact local activation contract.</returns>
    private static bool TryReadOptions(
        string[] args,
        out string? dashboardRoot,
        out bool humanRemediationMode,
        out string? humanReviewSample)
    {
        dashboardRoot = null;
        humanRemediationMode = false;
        humanReviewSample = null;
        if (args.Length is not (4 or 6) ||
            !string.Equals(args[0], "--activation", StringComparison.Ordinal) ||
            !string.Equals(args[2], "--dashboard-root", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(args[3]) ||
            (!string.Equals(args[1], ActivationMarker, StringComparison.Ordinal) &&
             !string.Equals(args[1], HumanRemediationActivationMarker, StringComparison.Ordinal)))
        {
            return false;
        }

        humanRemediationMode = string.Equals(
            args[1],
            HumanRemediationActivationMarker,
            StringComparison.Ordinal);
        if (args.Length == 6)
        {
            if (!humanRemediationMode ||
                !string.Equals(args[4], "--sample", StringComparison.Ordinal) ||
                args[5] is not ("S06-HG-001" or "S06-HG-006"))
            {
                return false;
            }
            humanReviewSample = args[5];
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
