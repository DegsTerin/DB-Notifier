// Module purpose: Implements Agent Worker Options for the opt-in Agent runtime while preserving fail-closed defaults.
namespace DBNotifier.Agent.Worker;

public sealed class AgentWorkerOptions
{
    public const string SectionName = "DBNotifier:Agent";

    public bool MonitoringEnabled { get; set; }

    public Guid AgentId { get; set; }

    public string? DatabasePath { get; set; }

    public int CycleIntervalSeconds { get; set; } = 5;

    public static string ResolveDatabasePath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            if (!Path.IsPathFullyQualified(configuredPath))
            {
                throw new InvalidOperationException("Configured Agent database path must be fully qualified.");
            }

            return configuredPath;
        }

        string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localData))
        {
            throw new InvalidOperationException("Local application data directory is unavailable.");
        }

        return Path.Combine(localData, "DB-Notifier", "agent.db");
    }
}
