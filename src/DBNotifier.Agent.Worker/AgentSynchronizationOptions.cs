// Module purpose: Implements Agent Synchronization Options for the opt-in Agent runtime while preserving fail-closed defaults.
namespace DBNotifier.Agent.Worker;

public sealed class AgentSynchronizationOptions
{
    public const string SectionName = "DBNotifier:Synchronization";

    public bool Enabled { get; set; }

    public bool CommandPollingEnabled { get; set; }

    public string? ServerBaseAddress { get; set; }

    public string? ClientCertificateThumbprint { get; set; }

    public string AgentVersion { get; set; } = "0.1.0";

    public int DispatchIntervalSeconds { get; set; } = 10;

    public int MaximumBatchSize { get; set; } = 50;

    public int CommandPollIntervalSeconds { get; set; } = 15;

    public Dictionary<string, string> ProviderVersions { get; set; } = new(StringComparer.Ordinal);

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
