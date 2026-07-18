// Module purpose: Defines the sandbox-only Agent Fleet client activation guard while preserving an unavailable production runtime.
namespace DBNotifier.Agent.Worker;

/// <summary>
/// Owns the explicit Agent Fleet client flag. The current increment supplies only E2E identity material, so any
/// attempt to enable the ordinary Worker composition is rejected before store or network initialisation.
/// </summary>
public sealed class AgentFleetClientOptions
{
    /// <summary>Gets the configuration section owned by the Agent Fleet client guard.</summary>
    public const string SectionName = "DBNotifier:AgentFleetClient";

    /// <summary>Gets or sets whether an operational Agent Fleet client was requested.</summary>
    public bool Enabled { get; set; }

    /// <summary>Rejects runtime activation because only the local E2E composition owns identity material.</summary>
    /// <exception cref="InvalidOperationException">Thrown whenever operational activation is requested.</exception>
    public void ValidateForStartup()
    {
        if (Enabled)
        {
            throw new InvalidOperationException("agent_fleet.sandbox_only");
        }
    }
}
