// Module purpose: Defines bounded Agent synchronisation settings while preserving opt-in, fail-closed runtime behaviour.
namespace DBNotifier.Agent.Worker;

/// <summary>
/// Defines bounded, opt-in Agent synchronisation settings and validates transport and protocol prerequisites
/// without storing certificate material or enabling command execution.
/// </summary>
public sealed class AgentSynchronizationOptions
{
    /// <summary>Gets the configuration-section path owned by Agent synchronisation.</summary>
    public const string SectionName = "DBNotifier:Synchronization";

    /// <summary>Gets or sets whether observation dispatch is explicitly enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets whether the unavailable durable command-polling protocol was requested.</summary>
    public bool CommandPollingEnabled { get; set; }

    /// <summary>Gets or sets the absolute HTTPS base address of the authorised server.</summary>
    public string? ServerBaseAddress { get; set; }

    /// <summary>Gets or sets the non-secret thumbprint identifying the Agent client certificate.</summary>
    public string? ClientCertificateThumbprint { get; set; }

    /// <summary>Gets or sets the bounded Agent version reported by the synchronisation protocol.</summary>
    public string AgentVersion { get; set; } = "0.1.0";

    /// <summary>Gets or sets the observation-dispatch cadence in seconds.</summary>
    public int DispatchIntervalSeconds { get; set; } = 10;

    /// <summary>Gets or sets the maximum number of observations sent in one batch.</summary>
    public int MaximumBatchSize { get; set; } = 50;

    /// <summary>Gets or sets the future command-polling cadence in seconds.</summary>
    public int CommandPollIntervalSeconds { get; set; } = 15;

    /// <summary>Gets or sets the bounded provider-version evidence reported by the Agent.</summary>
    public Dictionary<string, string> ProviderVersions { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Refuses command polling until durable request identifiers, sequences and acknowledgement replay
    /// are implemented across the Agent and server protocol.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when command polling is enabled without its durable protocol contract.
    /// </exception>
    public void ValidateCommandPollingForStartup()
    {
        if (Enabled && CommandPollingEnabled)
        {
            throw new InvalidOperationException("command.polling_durable_protocol_unavailable");
        }
    }

    /// <summary>Validates the HTTPS endpoint, certificate reference, versions, cadence and batch bounds.</summary>
    /// <returns>A canonical absolute HTTPS base address ending in a slash.</returns>
    /// <exception cref="InvalidOperationException">Thrown when any synchronisation prerequisite is outside policy.</exception>
    public Uri ValidateAndGetServerBaseAddress()
    {
        if (!Uri.TryCreate(ServerBaseAddress, UriKind.Absolute, out Uri? address) ||
            !string.Equals(address.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(address.UserInfo) ||
            !string.IsNullOrEmpty(address.Query) ||
            !string.IsNullOrEmpty(address.Fragment))
        {
            throw new InvalidOperationException(
                "Agent synchronization requires an absolute HTTPS server address without user info, query, or fragment.");
        }

        if (string.IsNullOrWhiteSpace(ClientCertificateThumbprint))
        {
            throw new InvalidOperationException("Agent synchronization requires a client certificate thumbprint.");
        }

        if (string.IsNullOrWhiteSpace(AgentVersion) || AgentVersion.Length > 64)
        {
            throw new InvalidOperationException("Agent synchronization requires a bounded Agent version.");
        }

        if (DispatchIntervalSeconds is < 1 or > 300 || MaximumBatchSize is < 1 or > 100 ||
            CommandPollIntervalSeconds is < 1 or > 300 ||
            ProviderVersions.Any(item => string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value)))
        {
            throw new InvalidOperationException("Agent synchronization cadence or batch size is outside policy.");
        }

        return new Uri(address.AbsoluteUri.TrimEnd('/') + '/', UriKind.Absolute);
    }
}
