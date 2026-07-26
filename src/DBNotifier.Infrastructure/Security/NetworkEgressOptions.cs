// Module purpose: Defines bounded configuration inputs that compile into immutable local network-egress policies.
namespace DBNotifier.Infrastructure.Security;

/// <summary>
/// Owns the named network-egress policies bound once during host composition.
/// Mutable configuration values must never be consumed directly after compilation.
/// </summary>
public sealed class NetworkEgressOptions
{
    /// <summary>Gets the configuration section containing all local network-egress policies.</summary>
    public const string SectionName = "DBNotifier:NetworkEgress";

    /// <summary>Gets or sets the bounded policy inputs keyed by trusted application-selected identifier.</summary>
    public Dictionary<string, NetworkEgressPolicyOptions> Policies { get; set; } =
        new(StringComparer.Ordinal);
}

/// <summary>Defines one bounded positive allowlist, overriding denylist and resolver resource envelope.</summary>
public sealed class NetworkEgressPolicyOptions
{
    /// <summary>Gets or sets the canonical CIDRs that may receive connections under this policy.</summary>
    public List<string> AllowedCidrs { get; set; } = [];

    /// <summary>Gets or sets additional canonical CIDRs that take precedence over the allowlist.</summary>
    public List<string> DeniedCidrs { get; set; } = [];

    /// <summary>Gets or sets the exact destination ports authorised by this policy.</summary>
    public List<int> AllowedPorts { get; set; } = [];

    /// <summary>Gets or sets the DNS deadline in whole seconds, between one and ten.</summary>
    public int DnsTimeoutSeconds { get; set; } = 5;

    /// <summary>Gets or sets the maximum number of distinct DNS answers accepted, between one and 32.</summary>
    public int MaximumResolvedAddresses { get; set; } = 16;
}
