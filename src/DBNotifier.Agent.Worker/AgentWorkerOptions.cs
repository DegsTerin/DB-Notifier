// Module purpose: Implements Agent Worker Options for the opt-in Agent runtime while preserving fail-closed defaults.
namespace DBNotifier.Agent.Worker;

/// <summary>Defines fail-closed monitoring and local persistence options for the Agent process.</summary>
public sealed class AgentWorkerOptions
{
    /// <summary>Gets the configuration section containing Agent process options.</summary>
    public const string SectionName = "DBNotifier:Agent";

    /// <summary>Gets or sets whether the monitoring worker may initialise and execute.</summary>
    public bool MonitoringEnabled { get; set; }

    /// <summary>Gets or sets the exact Agent identity shared by monitoring and synchronisation.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Gets or sets the optional fully qualified Agent SQLite path.</summary>
    public string? DatabasePath { get; set; }

    /// <summary>Gets or sets the bounded monitoring cadence in seconds.</summary>
    public int CycleIntervalSeconds { get; set; } = 5;

    /// <summary>Validates options that can activate monitoring or synchronisation before the host is built.</summary>
    /// <param name="synchronisationEnabled">Whether the independent outbox synchronisation path is enabled.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an enabled path has no Agent identity or monitoring cadence is outside policy.
    /// </exception>
    public void ValidateForStartup(bool synchronisationEnabled)
    {
        if ((MonitoringEnabled || synchronisationEnabled) && AgentId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A non-empty AgentId is required when monitoring or synchronisation is enabled.");
        }

        if (MonitoringEnabled && CycleIntervalSeconds is < 1 or > 300)
        {
            throw new InvalidOperationException("Agent cycle interval must be between 1 and 300 seconds.");
        }
    }

    /// <summary>Resolves the configured Agent database path without creating a file or directory.</summary>
    /// <param name="configuredPath">Optional fully qualified path supplied by configuration.</param>
    /// <returns>The configured path or the current-user DB-Notifier default.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a configured path is relative or the current-user data directory is unavailable.
    /// </exception>
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
