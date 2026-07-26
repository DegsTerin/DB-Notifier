// Module purpose: Compiles bounded CIDR rules and enforces immutable destination admission before any outbound connection.
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Infrastructure.Security;

/// <summary>
/// Holds a defensive immutable snapshot of every declared network-egress policy.
/// </summary>
public sealed class NetworkEgressPolicySet
{
    private const int MaximumPolicies = 16;
    private const int MaximumCidrsPerList = 64;
    private const int MaximumPorts = 64;
    private readonly IReadOnlyDictionary<string, CompiledNetworkEgressPolicy> policies;

    /// <summary>Creates a policy set from an already validated defensive snapshot.</summary>
    /// <param name="policies">Exact case-sensitive policies owned by the current host.</param>
    private NetworkEgressPolicySet(IReadOnlyDictionary<string, CompiledNetworkEgressPolicy> policies)
    {
        this.policies = policies;
    }

    /// <summary>Compiles and validates mutable configuration into one immutable policy snapshot.</summary>
    /// <param name="options">Configuration values bound once by the owning host.</param>
    /// <returns>A policy set that never observes subsequent mutation of <paramref name="options"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown with a sanitised code when any declared policy is invalid.</exception>
    public static NetworkEgressPolicySet Compile(NetworkEgressOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Policies is null || options.Policies.Count > MaximumPolicies)
        {
            throw Invalid(NetworkEgressFailureCodes.DestinationInvalid);
        }

        Dictionary<string, CompiledNetworkEgressPolicy> compiled = new(StringComparer.Ordinal);
        foreach ((string policyId, NetworkEgressPolicyOptions? policyOptions) in options.Policies)
        {
            if (!IsValidPolicyId(policyId) || policyOptions is null ||
                !compiled.TryAdd(policyId, CompilePolicy(policyOptions)))
            {
                throw Invalid(NetworkEgressFailureCodes.DestinationInvalid);
            }
        }

        return new(new ReadOnlyDictionary<string, CompiledNetworkEgressPolicy>(compiled));
    }

    /// <summary>Determines whether the immutable snapshot contains the exact policy identifier.</summary>
    /// <param name="policyId">Trusted application-selected policy identifier.</param>
    /// <returns><see langword="true"/> only when that exact policy was compiled.</returns>
    public bool Contains(string policyId) =>
        !string.IsNullOrWhiteSpace(policyId) && policies.ContainsKey(policyId);

    /// <summary>Requires an existing policy that explicitly permits the supplied destination port.</summary>
    /// <param name="policyId">Trusted application-selected policy identifier.</param>
    /// <param name="port">Known startup destination port.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown with a sanitised code when the policy is absent or the port is invalid or denied.
    /// </exception>
    public void Require(string policyId, int port)
    {
        if (!TryGet(policyId, out CompiledNetworkEgressPolicy? policy))
        {
            throw Invalid(NetworkEgressFailureCodes.PolicyUnavailable);
        }
        if (port is < 1 or > 65535)
        {
            throw Invalid(NetworkEgressFailureCodes.DestinationInvalid);
        }
        if (!policy.AllowedPorts.Contains(port))
        {
            throw Invalid(NetworkEgressFailureCodes.PortDenied);
        }
    }

    /// <summary>Looks up one exact trusted policy identifier without applying configuration fallbacks.</summary>
    /// <param name="policyId">Application-selected policy identifier.</param>
    /// <param name="policy">Compiled policy when found.</param>
    /// <returns><see langword="true"/> only for an exact compiled identifier.</returns>
    internal bool TryGet(string? policyId, out CompiledNetworkEgressPolicy policy)
    {
        if (!string.IsNullOrWhiteSpace(policyId) && policies.TryGetValue(policyId, out policy!))
        {
            return true;
        }

        policy = null!;
        return false;
    }

    /// <summary>Validates bounds and defensively compiles one declared policy.</summary>
    /// <param name="options">Mutable configuration values for one named policy.</param>
    /// <returns>An immutable policy containing parsed CIDRs, ports and DNS limits.</returns>
    private static CompiledNetworkEgressPolicy CompilePolicy(NetworkEgressPolicyOptions options)
    {
        if (options.AllowedCidrs is null || options.AllowedCidrs.Count is < 1 or > MaximumCidrsPerList ||
            options.DeniedCidrs is null || options.DeniedCidrs.Count > MaximumCidrsPerList ||
            options.AllowedPorts is null || options.AllowedPorts.Count is < 1 or > MaximumPorts ||
            options.DnsTimeoutSeconds is < 1 or > 10 ||
            options.MaximumResolvedAddresses is < 1 or > 32)
        {
            throw Invalid(NetworkEgressFailureCodes.DestinationInvalid);
        }

        NetworkCidr[] allowed = ParseDistinctCidrs(options.AllowedCidrs);
        NetworkCidr[] denied = ParseDistinctCidrs(options.DeniedCidrs);
        int[] ports = options.AllowedPorts.ToArray();
        if (ports.Any(static port => port is < 1 or > 65535) ||
            ports.Distinct().Count() != ports.Length)
        {
            throw Invalid(NetworkEgressFailureCodes.DestinationInvalid);
        }

        Array.Sort(ports);
        return new(
            allowed,
            denied,
            new HashSet<int>(ports),
            TimeSpan.FromSeconds(options.DnsTimeoutSeconds),
            options.MaximumResolvedAddresses);
    }

    /// <summary>Parses a bounded CIDR list and refuses aliases or duplicate canonical networks.</summary>
    /// <param name="values">CIDR strings supplied by local configuration.</param>
    /// <returns>A defensive array of parsed networks.</returns>
    private static NetworkCidr[] ParseDistinctCidrs(List<string> values)
    {
        NetworkCidr[] cidrs = new NetworkCidr[values.Count];
        HashSet<string> canonical = new(StringComparer.Ordinal);
        for (int index = 0; index < values.Count; index++)
        {
            if (!NetworkCidr.TryParse(values[index], out NetworkCidr cidr) ||
                !canonical.Add(cidr.CanonicalValue))
            {
                throw Invalid(NetworkEgressFailureCodes.DestinationInvalid);
            }
            cidrs[index] = cidr;
        }

        return cidrs;
    }

    /// <summary>Checks the stable lower-case identifier grammar used by trusted composition.</summary>
    /// <param name="value">Candidate policy identifier.</param>
    /// <returns><see langword="true"/> only for a bounded lower-case identifier.</returns>
    private static bool IsValidPolicyId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
        {
            return false;
        }

        return value.All(static character =>
            char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-');
    }

    /// <summary>Creates one startup-safe exception that contains no configuration value.</summary>
    /// <param name="code">Stable sanitised failure code.</param>
    /// <returns>An exception suitable for fail-closed host composition.</returns>
    private static InvalidOperationException Invalid(string code) => new(code);
}

/// <summary>Stores one validated immutable CIDR, port and resolver envelope.</summary>
/// <param name="allowedCidrs">Positive networks required for admission.</param>
/// <param name="deniedCidrs">Networks that override any positive match.</param>
/// <param name="allowedPorts">Exact permitted ports.</param>
/// <param name="dnsTimeout">Maximum duration of one DNS resolution.</param>
/// <param name="maximumResolvedAddresses">Maximum raw DNS answer count.</param>
internal sealed class CompiledNetworkEgressPolicy(
    IReadOnlyList<NetworkCidr> allowedCidrs,
    IReadOnlyList<NetworkCidr> deniedCidrs,
    IReadOnlySet<int> allowedPorts,
    TimeSpan dnsTimeout,
    int maximumResolvedAddresses)
{
    /// <summary>Gets the immutable positive networks.</summary>
    internal IReadOnlyList<NetworkCidr> AllowedCidrs { get; } = allowedCidrs;

    /// <summary>Gets the immutable overriding negative networks.</summary>
    internal IReadOnlyList<NetworkCidr> DeniedCidrs { get; } = deniedCidrs;

    /// <summary>Gets the immutable exact-port allowlist.</summary>
    internal IReadOnlySet<int> AllowedPorts { get; } = allowedPorts;

    /// <summary>Gets the bounded resolver deadline.</summary>
    internal TimeSpan DnsTimeout { get; } = dnsTimeout;

    /// <summary>Gets the maximum accepted raw address count.</summary>
    internal int MaximumResolvedAddresses { get; } = maximumResolvedAddresses;
}

/// <summary>Represents one canonical IPv4 or IPv6 network without retaining configuration text.</summary>
internal readonly struct NetworkCidr
{
    private readonly byte[] networkBytes;

    /// <summary>Creates a parsed network whose host bits were already proved to be zero.</summary>
    /// <param name="networkAddress">Canonical network address.</param>
    /// <param name="prefixLength">Validated family-specific prefix length.</param>
    private NetworkCidr(IPAddress networkAddress, int prefixLength)
    {
        AddressFamily = networkAddress.AddressFamily;
        networkBytes = networkAddress.GetAddressBytes();
        PrefixLength = prefixLength;
        CanonicalValue = string.Create(
            CultureInfo.InvariantCulture,
            $"{networkAddress}/{prefixLength}");
    }

    /// <summary>Gets the address family matched by this network.</summary>
    internal AddressFamily AddressFamily { get; }

    /// <summary>Gets the family-specific prefix length.</summary>
    internal int PrefixLength { get; }

    /// <summary>Gets a stable representation used only to reject duplicate rules.</summary>
    internal string CanonicalValue { get; }

    /// <summary>Parses one CIDR and rejects scoped, mapped-prefix or host-bit ambiguity.</summary>
    /// <param name="value">Local configuration value.</param>
    /// <param name="cidr">Parsed network when successful.</param>
    /// <returns><see langword="true"/> only for one canonical supported CIDR.</returns>
    internal static bool TryParse(string? value, out NetworkCidr cidr)
    {
        cidr = default;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 80 || value != value.Trim())
        {
            return false;
        }

        int separator = value.IndexOf('/');
        if (separator <= 0 || separator != value.LastIndexOf('/') ||
            !IPAddress.TryParse(value[..separator], out IPAddress? address) ||
            !int.TryParse(
                value[(separator + 1)..],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int prefixLength))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.ScopeId != 0)
        {
            return false;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            if (prefixLength is < 96 or > 128)
            {
                return false;
            }
            address = address.MapToIPv4();
            prefixLength -= 96;
        }

        int maximumPrefix = address.AddressFamily switch
        {
            AddressFamily.InterNetwork => 32,
            AddressFamily.InterNetworkV6 => 128,
            _ => -1,
        };
        if (prefixLength < 0 || prefixLength > maximumPrefix || !HasZeroHostBits(address, prefixLength))
        {
            return false;
        }

        cidr = new(address, prefixLength);
        return true;
    }

    /// <summary>Determines whether a normalised candidate belongs to this exact network.</summary>
    /// <param name="candidate">Candidate address returned by an untrusted resolver.</param>
    /// <returns><see langword="true"/> when every significant prefix bit matches.</returns>
    internal bool Contains(IPAddress candidate)
    {
        IPAddress address = NetworkAddressRules.Normalize(candidate);
        if (address.AddressFamily != AddressFamily)
        {
            return false;
        }

        byte[] candidateBytes = address.GetAddressBytes();
        int wholeBytes = PrefixLength / 8;
        for (int index = 0; index < wholeBytes; index++)
        {
            if (networkBytes[index] != candidateBytes[index])
            {
                return false;
            }
        }

        int remainingBits = PrefixLength % 8;
        if (remainingBits == 0)
        {
            return true;
        }

        int mask = 0xff << (8 - remainingBits);
        return (networkBytes[wholeBytes] & mask) == (candidateBytes[wholeBytes] & mask);
    }

    /// <summary>Checks that a configured network address contains no set bit after its prefix.</summary>
    /// <param name="address">Parsed IPv4 or IPv6 address.</param>
    /// <param name="prefixLength">Validated prefix length.</param>
    /// <returns><see langword="true"/> only when all host bits are zero.</returns>
    private static bool HasZeroHostBits(IPAddress address, int prefixLength)
    {
        byte[] bytes = address.GetAddressBytes();
        int wholeBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;
        if (remainingBits != 0)
        {
            int hostMask = 0xff >> remainingBits;
            if ((bytes[wholeBytes] & hostMask) != 0)
            {
                return false;
            }
            wholeBytes++;
        }

        for (int index = wholeBytes; index < bytes.Length; index++)
        {
            if (bytes[index] != 0)
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>Normalises addresses and enforces non-overridable unsafe-address exclusions.</summary>
internal static class NetworkAddressRules
{
    private static readonly IPAddress AlibabaMetadata = IPAddress.Parse("100.100.100.200");
    private static readonly IPAddress AzurePlatformAddress = IPAddress.Parse("168.63.129.16");
    private static readonly IPAddress AwsIpv6Metadata = IPAddress.Parse("fd00:ec2::254");

    /// <summary>Maps IPv4-mapped IPv6 answers into one IPv4 policy domain.</summary>
    /// <param name="address">Resolver or literal address.</param>
    /// <returns>The original address or its IPv4 representation.</returns>
    internal static IPAddress Normalize(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }

    /// <summary>Rejects addresses that no local allowlist may make safe for this product boundary.</summary>
    /// <param name="candidate">Normalised candidate address.</param>
    /// <returns><see langword="true"/> for unspecified, multicast, link-local, broadcast or metadata destinations.</returns>
    internal static bool IsHardDenied(IPAddress candidate)
    {
        IPAddress address = Normalize(candidate);
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = address.GetAddressBytes();
            return bytes[0] == 0 ||
                (bytes[0] == 169 && bytes[1] == 254) ||
                bytes[0] >= 224 ||
                address.Equals(AlibabaMetadata) ||
                address.Equals(AzurePlatformAddress);
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6 || address.ScopeId != 0)
        {
            return true;
        }

        return address.Equals(IPAddress.IPv6Any) ||
            address.IsIPv6Multicast ||
            address.IsIPv6LinkLocal ||
            address.Equals(AwsIpv6Metadata);
    }
}
