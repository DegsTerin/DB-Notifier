// Module purpose: Validates exact local activation and delegates one bounded consolidated STATE-06 evidence run to the test-only integration composition.
using DBNotifier.IntegrationTests;

namespace DBNotifier.State06.ConsolidatedSandboxHost;

/// <summary>Provides the process boundary for one temporary, loopback-only and deliberately non-operational run.</summary>
internal static class Program
{
    private const string ActivationMarker = "state06-consolidated-e2e-sandbox";

    /// <summary>Validates arguments, applies the fifteen-minute budget and runs the isolated test composition.</summary>
    /// <param name="args">Exact activation marker and already-built Dashboard root.</param>
    /// <returns>Zero after complete cleanup, two for invalid activation, or three for a sanitised harness failure.</returns>
    public static async Task<int> Main(string[] args)
    {
        if (!TryReadDashboardRoot(args, out string? dashboardRoot))
        {
            Console.Error.WriteLine("state06_consolidated_host.failed:activation_invalid");
            return 2;
        }

        using CancellationTokenSource budget = new(TimeSpan.FromMinutes(15));
        try
        {
            return await AgentFleetApiEndToEndTests.RunState06ConsolidatedSandboxHostAsync(
                dashboardRoot!,
                budget.Token);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"state06_consolidated_host.failed:{exception.GetType().Name}");
            return 3;
        }
    }

    /// <summary>Accepts one exact marker and the canonical repository Dashboard output only.</summary>
    /// <param name="args">Untrusted process arguments.</param>
    /// <param name="dashboardRoot">Validated canonical Dashboard output root.</param>
    /// <returns><see langword="true"/> only for the exact local activation contract.</returns>
    private static bool TryReadDashboardRoot(string[] args, out string? dashboardRoot)
    {
        dashboardRoot = null;
        if (args.Length != 4 ||
            !string.Equals(args[0], "--activation", StringComparison.Ordinal) ||
            !string.Equals(args[1], ActivationMarker, StringComparison.Ordinal) ||
            !string.Equals(args[2], "--dashboard-root", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(args[3]))
        {
            return false;
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
