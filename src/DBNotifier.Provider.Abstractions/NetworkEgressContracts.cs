// Module purpose: Defines provider-neutral network-egress admission contracts without owning DNS or socket behaviour.
using System.Collections.ObjectModel;
using System.Net;

namespace DBNotifier.Provider.Abstractions;

/// <summary>Provides stable identifiers for the independently configured outbound-network boundaries.</summary>
public static class NetworkEgressPolicyIds
{
    /// <summary>Gets the policy used when an Agent synchronises observations with its authorised Server.</summary>
    public const string AgentSynchronization = "agent-synchronization";

    /// <summary>Gets the policy used when a provider probes a configured database endpoint.</summary>
    public const string ProviderMonitoring = "provider-monitoring";

    /// <summary>Gets the policy used when the Server retrieves human-identity metadata and signing keys.</summary>
    public const string HumanIdentity = "human-identity";

    /// <summary>Gets the policy used when the Server connects to its own central database.</summary>
    public const string ServerDatabase = "server-database";
}

/// <summary>Provides sanitised failure codes that never disclose a hostname, address or resolver exception.</summary>
public static class NetworkEgressFailureCodes
{
    /// <summary>Gets the code returned when the caller names no compiled local policy.</summary>
    public const string PolicyUnavailable = "network.policy_unavailable";

    /// <summary>Gets the code returned when a hostname, policy identifier or port is invalid.</summary>
    public const string DestinationInvalid = "network.destination_invalid";

    /// <summary>Gets the code returned when the local policy does not authorise the requested port.</summary>
    public const string PortDenied = "network.port_denied";

    /// <summary>Gets the code returned when bounded DNS resolution exceeds its deadline.</summary>
    public const string DnsTimeout = "network.dns_timeout";

    /// <summary>Gets the code returned when DNS resolution fails without caller cancellation.</summary>
    public const string DnsFailed = "network.dns_failed";

    /// <summary>Gets the code returned when DNS produces no usable address or exceeds the configured bound.</summary>
    public const string DnsAnswerLimit = "network.dns_answer_limit";

    /// <summary>Gets the code returned when any resolved address is prohibited or not explicitly allowed.</summary>
    public const string AddressDenied = "network.address_denied";

    /// <summary>Gets the code used when no approved address accepts a bounded socket connection.</summary>
    public const string ConnectionFailed = "network.connect_failed";
}

/// <summary>
/// Describes one destination that must be resolved and authorised under a caller-selected local policy.
/// </summary>
/// <param name="PolicyId">Stable policy identifier selected by trusted application composition.</param>
/// <param name="Host">DNS name or IP literal supplied by the owning endpoint contract.</param>
/// <param name="Port">Destination port between one and 65,535.</param>
public sealed record NetworkEgressRequest(
    string PolicyId,
    string Host,
    int Port);

/// <summary>
/// Contains either a bounded immutable set of approved IP addresses or one sanitised denial code.
/// </summary>
public sealed class NetworkEgressResolution
{
    private static readonly HashSet<string> KnownFailureCodes = new(StringComparer.Ordinal)
    {
        NetworkEgressFailureCodes.PolicyUnavailable,
        NetworkEgressFailureCodes.DestinationInvalid,
        NetworkEgressFailureCodes.PortDenied,
        NetworkEgressFailureCodes.DnsTimeout,
        NetworkEgressFailureCodes.DnsFailed,
        NetworkEgressFailureCodes.DnsAnswerLimit,
        NetworkEgressFailureCodes.AddressDenied,
        NetworkEgressFailureCodes.ConnectionFailed,
    };
    private readonly IPAddress[] addresses;

    /// <summary>Creates one already validated resolution snapshot.</summary>
    /// <param name="addresses">Defensive approved-address snapshot or an empty denied set.</param>
    /// <param name="failureCode">Sanitised denial code, or null for approval.</param>
    private NetworkEgressResolution(
        IPAddress[] addresses,
        string? failureCode)
    {
        this.addresses = addresses;
        FailureCode = failureCode;
    }

    /// <summary>Gets whether the destination was fully resolved and every returned address passed policy.</summary>
    public bool IsApproved => FailureCode is null;

    /// <summary>Gets a defensive copy of the approved addresses; denied resolutions expose an empty collection.</summary>
    public IReadOnlyList<IPAddress> Addresses =>
        new ReadOnlyCollection<IPAddress>(addresses.Select(CloneAddress).ToArray());

    /// <summary>Gets the sanitised failure code, or <see langword="null"/> for an approved resolution.</summary>
    public string? FailureCode { get; }

    /// <summary>Creates an approved result by defensively copying a non-empty address set.</summary>
    /// <param name="addresses">Policy-approved addresses that callers must use directly for connection.</param>
    /// <returns>An immutable approved resolution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="addresses"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the address set is empty or contains null entries.</exception>
    public static NetworkEgressResolution Approved(IEnumerable<IPAddress> addresses)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        IPAddress[] supplied = addresses.ToArray();
        if (supplied.Length == 0 || supplied.Any(static address => address is null))
        {
            throw new ArgumentException("An approved network-egress result requires at least one address.", nameof(addresses));
        }

        IPAddress[] snapshot = supplied.Select(CloneAddress).ToArray();
        return new(snapshot, null);
    }

    /// <summary>Creates a denied result containing only a stable sanitised failure code.</summary>
    /// <param name="failureCode">Stable non-secret code from <see cref="NetworkEgressFailureCodes"/>.</param>
    /// <returns>An immutable denied resolution.</returns>
    /// <exception cref="ArgumentException">Thrown when the failure code is absent or unbounded.</exception>
    public static NetworkEgressResolution Denied(string failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode) ||
            failureCode.Length > 100 ||
            !KnownFailureCodes.Contains(failureCode))
        {
            throw new ArgumentException("A recognised network-egress failure code is required.", nameof(failureCode));
        }

        return new(Array.Empty<IPAddress>(), failureCode);
    }

    /// <summary>Copies one address without retaining a mutable caller-owned IPv6 scope.</summary>
    /// <param name="address">Caller-supplied address.</param>
    /// <returns>An independent address instance.</returns>
    private static IPAddress CloneAddress(IPAddress address) =>
        address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
            ? new IPAddress(address.GetAddressBytes(), address.ScopeId)
            : new IPAddress(address.GetAddressBytes());
}

/// <summary>
/// Resolves a destination under one immutable local policy and returns only addresses safe for direct connection.
/// </summary>
public interface INetworkEgressAuthorizer
{
    /// <summary>
    /// Resolves and authorises one destination without connecting to it or disclosing resolver diagnostics.
    /// </summary>
    /// <param name="request">Trusted policy identifier plus the untrusted endpoint host and port.</param>
    /// <param name="cancellationToken">Caller cancellation propagated independently of the DNS deadline.</param>
    /// <returns>An approved pinned-address set or one sanitised denial.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the caller cancels the operation.</exception>
    ValueTask<NetworkEgressResolution> ResolveAndAuthoriseAsync(
        NetworkEgressRequest request,
        CancellationToken cancellationToken);
}
