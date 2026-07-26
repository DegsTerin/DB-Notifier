// Module purpose: Resolves PostgreSQL network destinations through the provider-neutral local egress authority.
using System.Net;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Providers.PostgreSql;

/// <summary>
/// Separates the policy-approved IP used for a physical connection from the original hostname retained for TLS.
/// A Unix-domain socket remains local and therefore has no approved network address.
/// </summary>
/// <param name="Endpoint">Endpoint whose host is either the approved IP literal or the original local socket path.</param>
/// <param name="OriginalHost">Original hostname retained only for TLS server-identity verification.</param>
/// <param name="ApprovedAddress">Approved physical IP, or <see langword="null"/> for a local socket or denial.</param>
/// <param name="FailureCode">Sanitised denial code, or <see langword="null"/> when the destination is usable.</param>
internal sealed record PostgreSqlNetworkResolution(
    PostgreSqlEndpoint Endpoint,
    string OriginalHost,
    IPAddress? ApprovedAddress,
    string? FailureCode)
{
    /// <summary>Gets whether the endpoint may be used without any additional name resolution.</summary>
    internal bool IsApproved => FailureCode is null;

    /// <summary>Gets whether the approved endpoint is an existing Unix-domain socket path.</summary>
    internal bool IsLocalSocket => Endpoint.Host.StartsWith('/');
}

/// <summary>
/// Applies the fixed provider-monitoring policy immediately before PostgreSQL utilities or clients connect.
/// </summary>
internal static class PostgreSqlNetworkEgress
{
    /// <summary>Resolves and authorises one endpoint without opening a process, socket or provider connection.</summary>
    /// <param name="authorizer">Immutable local destination authority.</param>
    /// <param name="endpoint">Validated PostgreSQL endpoint containing no secret material.</param>
    /// <param name="cancellationToken">Caller cancellation propagated to bounded resolution.</param>
    /// <returns>An approved IP-pinned endpoint, the unchanged local socket, or one sanitised denial.</returns>
    internal static async ValueTask<PostgreSqlNetworkResolution> ResolveAsync(
        INetworkEgressAuthorizer authorizer,
        PostgreSqlEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorizer);
        ArgumentNullException.ThrowIfNull(endpoint);
        if (endpoint.Host.StartsWith('/'))
        {
            return new(endpoint, endpoint.Host, null, null);
        }

        NetworkEgressResolution resolution = await authorizer
            .ResolveAndAuthoriseAsync(
                new NetworkEgressRequest(
                    NetworkEgressPolicyIds.ProviderMonitoring,
                    endpoint.Host,
                    endpoint.Port),
                cancellationToken)
            .ConfigureAwait(false);
        if (!resolution.IsApproved)
        {
            return new(
                endpoint,
                endpoint.Host,
                null,
                resolution.FailureCode ?? NetworkEgressFailureCodes.DestinationInvalid);
        }

        IPAddress approvedAddress = resolution.Addresses[0];
        return new(
            endpoint with { Host = approvedAddress.ToString() },
            endpoint.Host,
            approvedAddress,
            null);
    }
}
