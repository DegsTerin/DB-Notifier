// Module purpose: Creates no-redirect HTTP handlers whose physical sockets connect only to freshly authorised pinned IPs.
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Infrastructure.Http;

/// <summary>
/// Creates direct HTTP handlers that disable ambient credentials, proxies, cookies and redirects at the egress boundary.
/// </summary>
public sealed class NetworkBoundHttpMessageHandlerFactory
{
    private readonly INetworkEgressAuthorizer authorizer;
    private readonly INetworkStreamConnector connector;

    /// <summary>Creates the production factory with direct operating-system sockets.</summary>
    /// <param name="authorizer">Local destination authority invoked for every new physical connection.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="authorizer"/> is null.</exception>
    public NetworkBoundHttpMessageHandlerFactory(INetworkEgressAuthorizer authorizer)
        : this(authorizer, new SystemNetworkStreamConnector())
    {
    }

    /// <summary>Creates a factory with an injected connector for deterministic local tests.</summary>
    /// <param name="authorizer">Local destination authority.</param>
    /// <param name="connector">Connector that receives only policy-approved IP addresses.</param>
    internal NetworkBoundHttpMessageHandlerFactory(
        INetworkEgressAuthorizer authorizer,
        INetworkStreamConnector connector)
    {
        ArgumentNullException.ThrowIfNull(authorizer);
        ArgumentNullException.ThrowIfNull(connector);
        this.authorizer = authorizer;
        this.connector = connector;
    }

    /// <summary>Creates one independently disposable handler bound to an exact local egress policy.</summary>
    /// <param name="policyId">Trusted application-selected policy identifier.</param>
    /// <param name="clientCertificate">Optional Agent identity certificate used only by this handler.</param>
    /// <returns>A direct handler with DNS pinning and offline server-certificate validation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="policyId"/> is absent or unbounded.</exception>
    public SocketsHttpHandler Create(
        string policyId,
        X509Certificate2? clientCertificate = null)
    {
        if (string.IsNullOrWhiteSpace(policyId) || policyId.Length > 64)
        {
            throw new ArgumentException("A bounded network-egress policy identifier is required.", nameof(policyId));
        }

        SslClientAuthenticationOptions sslOptions = new()
        {
            CertificateChainPolicy = OfflineCertificateChainPolicy.CreateServerAuthentication(),
        };
        if (clientCertificate is not null)
        {
            sslOptions.ClientCertificateContext = SslStreamCertificateContext.Create(
                clientCertificate,
                new X509Certificate2Collection(),
                offline: true);
        }

        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            Credentials = null,
            PreAuthenticate = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 8,
            SslOptions = sslOptions,
            ConnectCallback = (context, cancellationToken) =>
                ConnectPinnedAsync(policyId, context.DnsEndPoint, cancellationToken),
        };
    }

    /// <summary>Resolves one physical connection and passes only approved addresses to the socket seam.</summary>
    /// <param name="policyId">Trusted application-selected policy identifier.</param>
    /// <param name="endpoint">Untrusted HTTP hostname and port supplied by the HTTP stack.</param>
    /// <param name="cancellationToken">Physical-connection cancellation.</param>
    /// <returns>A connected stream owned by the HTTP stack.</returns>
    /// <exception cref="HttpRequestException">Thrown with only a sanitised code when admission or connection fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the caller cancels the operation.</exception>
    internal async ValueTask<Stream> ConnectPinnedAsync(
        string policyId,
        DnsEndPoint endpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        NetworkEgressResolution resolution = await authorizer
            .ResolveAndAuthoriseAsync(
                new NetworkEgressRequest(policyId, endpoint.Host, endpoint.Port),
                cancellationToken)
            .ConfigureAwait(false);
        if (!resolution.IsApproved)
        {
            throw new HttpRequestException(resolution.FailureCode);
        }

        foreach (IPAddress address in resolution.Addresses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                Stream stream = await connector
                    .ConnectAsync(address, endpoint.Port, cancellationToken)
                    .ConfigureAwait(false);
                if (stream is not null)
                {
                    return stream;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is SocketException or IOException)
            {
                // A later policy-approved address may still accept the same bounded physical connection.
            }
        }

        throw new HttpRequestException(NetworkEgressFailureCodes.ConnectionFailed);
    }
}

internal interface INetworkStreamConnector
{
    /// <summary>Connects directly to one already authorised IP address without performing DNS.</summary>
    /// <param name="address">Approved pinned IP address.</param>
    /// <param name="port">Approved destination port.</param>
    /// <param name="cancellationToken">Physical-connection cancellation.</param>
    /// <returns>A stream that transfers ownership of the underlying connection to its caller.</returns>
    ValueTask<Stream> ConnectAsync(
        IPAddress address,
        int port,
        CancellationToken cancellationToken);
}

/// <summary>Creates one direct TCP socket and transfers its lifetime to a network stream.</summary>
internal sealed class SystemNetworkStreamConnector : INetworkStreamConnector
{
    /// <inheritdoc />
    public async ValueTask<Stream> ConnectAsync(
        IPAddress address,
        int port,
        CancellationToken cancellationToken)
    {
        Socket socket = new(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
        };
        try
        {
            await socket
                .ConnectAsync(new IPEndPoint(address, port), cancellationToken)
                .ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
