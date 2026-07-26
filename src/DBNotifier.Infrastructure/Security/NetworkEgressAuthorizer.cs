// Module purpose: Revalidates DNS and returns only explicitly allowed pinned addresses under one immutable local policy.
using System.Net;
using System.Net.Sockets;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Infrastructure.Security;

/// <summary>
/// Resolves each destination at admission time, rejects mixed or prohibited answers and never connects by hostname.
/// </summary>
public sealed class NetworkEgressAuthorizer : INetworkEgressAuthorizer
{
    private readonly NetworkEgressPolicySet policySet;
    private readonly IDnsResolver dnsResolver;

    /// <summary>Creates the production authoriser over the operating-system DNS resolver.</summary>
    /// <param name="policySet">Immutable local policy snapshot compiled during host composition.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policySet"/> is null.</exception>
    public NetworkEgressAuthorizer(NetworkEgressPolicySet policySet)
        : this(policySet, new SystemDnsResolver())
    {
    }

    /// <summary>Creates an authoriser with an injected resolver for deterministic local validation.</summary>
    /// <param name="policySet">Immutable local policy snapshot.</param>
    /// <param name="dnsResolver">Resolver that supplies untrusted address answers.</param>
    internal NetworkEgressAuthorizer(
        NetworkEgressPolicySet policySet,
        IDnsResolver dnsResolver)
    {
        ArgumentNullException.ThrowIfNull(policySet);
        ArgumentNullException.ThrowIfNull(dnsResolver);
        this.policySet = policySet;
        this.dnsResolver = dnsResolver;
    }

    /// <inheritdoc />
    public async ValueTask<NetworkEgressResolution> ResolveAndAuthoriseAsync(
        NetworkEgressRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (!policySet.TryGet(request.PolicyId, out CompiledNetworkEgressPolicy? policy))
        {
            return NetworkEgressResolution.Denied(NetworkEgressFailureCodes.PolicyUnavailable);
        }
        if (request.Port is < 1 or > 65535 || !IsValidHost(request.Host))
        {
            return NetworkEgressResolution.Denied(NetworkEgressFailureCodes.DestinationInvalid);
        }
        if (!policy.AllowedPorts.Contains(request.Port))
        {
            return NetworkEgressResolution.Denied(NetworkEgressFailureCodes.PortDenied);
        }

        IReadOnlyList<IPAddress> untrustedAddresses;
        if (IPAddress.TryParse(request.Host, out IPAddress? literal))
        {
            untrustedAddresses = [literal];
        }
        else
        {
            using CancellationTokenSource deadline =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(policy.DnsTimeout);
            try
            {
                untrustedAddresses = await dnsResolver
                    .ResolveAsync(request.Host, deadline.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested)
            {
                return NetworkEgressResolution.Denied(NetworkEgressFailureCodes.DnsTimeout);
            }
            catch (Exception exception) when (
                exception is SocketException or ArgumentException or InvalidOperationException)
            {
                return NetworkEgressResolution.Denied(NetworkEgressFailureCodes.DnsFailed);
            }
        }

        if (untrustedAddresses is null ||
            untrustedAddresses.Count is < 1 ||
            untrustedAddresses.Count > policy.MaximumResolvedAddresses ||
            untrustedAddresses.Any(static address => address is null))
        {
            return NetworkEgressResolution.Denied(NetworkEgressFailureCodes.DnsAnswerLimit);
        }

        List<IPAddress> approved = new(untrustedAddresses.Count);
        HashSet<IPAddress> distinct = [];
        foreach (IPAddress untrustedAddress in untrustedAddresses)
        {
            IPAddress normalisedAddress = NetworkAddressRules.Normalize(untrustedAddress);
            if (NetworkAddressRules.IsHardDenied(normalisedAddress))
            {
                return NetworkEgressResolution.Denied(NetworkEgressFailureCodes.AddressDenied);
            }

            IPAddress address = Snapshot(normalisedAddress);
            if (!distinct.Add(address))
            {
                continue;
            }
            if (policy.DeniedCidrs.Any(cidr => cidr.Contains(address)) ||
                !policy.AllowedCidrs.Any(cidr => cidr.Contains(address)))
            {
                return NetworkEgressResolution.Denied(NetworkEgressFailureCodes.AddressDenied);
            }
            approved.Add(address);
        }

        return approved.Count == 0
            ? NetworkEgressResolution.Denied(NetworkEgressFailureCodes.DnsAnswerLimit)
            : NetworkEgressResolution.Approved(approved);
    }

    /// <summary>Accepts only bounded IP literals or unambiguous DNS names before resolver work.</summary>
    /// <param name="host">Untrusted endpoint host.</param>
    /// <returns><see langword="true"/> when the host can enter policy evaluation.</returns>
    private static bool IsValidHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host) || host.Length > 253 || host != host.Trim() ||
            host.Any(char.IsControl))
        {
            return false;
        }
        if (IPAddress.TryParse(host, out IPAddress? address))
        {
            return address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6 &&
                (address.AddressFamily != AddressFamily.InterNetworkV6 || address.ScopeId == 0);
        }

        return !host.EndsWith('.') && Uri.CheckHostName(host) == UriHostNameType.Dns;
    }

    /// <summary>Copies an address so later resolver mutation cannot alter a completed decision.</summary>
    /// <param name="address">Normalised resolver address.</param>
    /// <returns>An independent address value.</returns>
    private static IPAddress Snapshot(IPAddress address) => new(address.GetAddressBytes());
}
