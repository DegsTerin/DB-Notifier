// Module purpose: Verifies PostgreSQL egress pinning and offline TLS integration without DNS, sockets or databases.
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Application.Security;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;
using DBNotifier.Providers.PostgreSql;

namespace DBNotifier.UnitTests;

/// <summary>
/// Exercises each provider boundary with deterministic authorisation and connection seams so a hostname can never
/// be resolved implicitly after local policy admission.
/// </summary>
public sealed class ProviderNetworkEgressIntegrationTests
{
    private static readonly IPAddress ApprovedAddress = IPAddress.Parse("203.0.113.17");
    private const string OriginalHost = "database.example.test";

    /// <summary>Proves provider resolution pins the approved IP while retaining the original hostname only for TLS.</summary>
    [Fact]
    public async Task ProviderResolutionPinsApprovedAddressAndPreservesTlsHost()
    {
        RecordingAuthorizer authorizer = ApprovedAuthorizer();
        PostgreSqlNetworkResolution resolution = await PostgreSqlNetworkEgress.ResolveAsync(
            authorizer,
            Endpoint(),
            CancellationToken.None);

        Assert.True(resolution.IsApproved);
        Assert.Equal(ApprovedAddress, resolution.ApprovedAddress);
        Assert.Equal(ApprovedAddress.ToString(), resolution.Endpoint.Host);
        Assert.Equal(OriginalHost, resolution.OriginalHost);
        Assert.Equal(NetworkEgressPolicyIds.ProviderMonitoring, authorizer.Request?.PolicyId);
        Assert.Equal(OriginalHost, authorizer.Request?.Host);
        Assert.Equal(5432, authorizer.Request?.Port);

        ProcessStartInfo startInfo = PostgreSqlReadinessExecutor.CreateStartInfo(
            resolution.Endpoint with { PgIsReadyPath = "pg_isready" },
            TimeSpan.FromSeconds(2));
        Assert.Equal(ApprovedAddress.ToString(), startInfo.ArgumentList[1]);
    }

    /// <summary>Proves a denied readiness destination opens no process and maps to a sanitised configuration result.</summary>
    [Fact]
    public async Task ReadinessDenialMapsToUnknownConfigurationWithoutProcess()
    {
        RecordingAuthorizer authorizer = DeniedAuthorizer(NetworkEgressFailureCodes.AddressDenied);
        PostgreSqlReadinessExecutor executor = new(
            new FoundDiscovery(),
            new UnexpectedTransportProbe(),
            authorizer);
        PostgreSqlDatabaseProvider provider = new(
            executor,
            new UnexpectedAuthenticatedExecutor());

        ProviderProbeResult result = await provider.ProbeAsync(
            new ProviderProbeRequest(ProviderEndpoint(), null, TimeSpan.FromSeconds(2), 1),
            CancellationToken.None);

        Assert.Equal(HealthStatus.Unknown, result.Status);
        Assert.Equal(EvidenceLevel.Unknown, result.EvidenceLevel);
        Assert.Equal(ErrorCategory.Configuration, result.Error?.Category);
        Assert.Equal(Retryability.AfterConfigurationChange, result.Error?.Retryability);
        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, result.Error?.Code);
        Assert.DoesNotContain(OriginalHost, result.Error?.SafeMessage ?? string.Empty, StringComparison.Ordinal);
    }

    /// <summary>Proves the TCP fallback receives only the approved IP and never the original hostname.</summary>
    [Fact]
    public async Task TcpFallbackConnectsOnlyToApprovedAddress()
    {
        RecordingAuthorizer authorizer = ApprovedAuthorizer();
        RecordingTcpConnector connector = new();
        TcpPostgreSqlTransportProbe probe = new(authorizer, connector);

        PostgreSqlTransportResult result = await probe.ProbeAsync(
            Endpoint(),
            TimeSpan.FromSeconds(2),
            CancellationToken.None);

        Assert.Equal(PostgreSqlTransportState.Reachable, result.State);
        Assert.Equal(ApprovedAddress, connector.Address);
        Assert.Equal(5432, connector.Port);
        Assert.Equal(1, connector.CallCount);
    }

    /// <summary>Proves a denied TCP fallback opens no socket and retains the sanitised configuration failure.</summary>
    [Fact]
    public async Task TcpFallbackDenialMapsToUnknownConfigurationWithoutSocket()
    {
        RecordingTcpConnector connector = new();
        RecordingAuthorizer authorizer = DeniedAuthorizer(NetworkEgressFailureCodes.AddressDenied);
        TcpPostgreSqlTransportProbe transport = new(authorizer, connector);
        PostgreSqlReadinessExecutor executor = new(
            new NotFoundDiscovery(),
            transport,
            authorizer);
        PostgreSqlDatabaseProvider provider = new(
            executor,
            new UnexpectedAuthenticatedExecutor());

        ProviderProbeResult result = await provider.ProbeAsync(
            new ProviderProbeRequest(ProviderEndpoint(), null, TimeSpan.FromSeconds(2), 1),
            CancellationToken.None);

        Assert.Equal(HealthStatus.Unknown, result.Status);
        Assert.Equal(EvidenceLevel.Unknown, result.EvidenceLevel);
        Assert.Equal(ErrorCategory.Configuration, result.Error?.Category);
        Assert.Equal(Retryability.AfterConfigurationChange, result.Error?.Retryability);
        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, result.Error?.Code);
        Assert.Equal(0, connector.CallCount);
    }

    /// <summary>Proves an authenticated denial is returned before password materialisation or provider connection.</summary>
    [Fact]
    public async Task AuthenticatedDenialMapsToUnknownConfigurationWithoutConnection()
    {
        NpgsqlAuthenticatedExecutor executor = new(
            DeniedAuthorizer(NetworkEgressFailureCodes.PortDenied));
        PostgreSqlDatabaseProvider provider = new(
            new UnexpectedReadinessExecutor(),
            executor);
        using ProviderCredentialLease credential = new("monitor", "fixture-secret".AsSpan());

        ProviderProbeResult result = await provider.ProbeAsync(
            new ProviderProbeRequest(ProviderEndpoint(), credential, TimeSpan.FromSeconds(2), 1),
            CancellationToken.None);

        Assert.Equal(HealthStatus.Unknown, result.Status);
        Assert.Equal(EvidenceLevel.Unknown, result.EvidenceLevel);
        Assert.Equal(ErrorCategory.Configuration, result.Error?.Category);
        Assert.Equal(Retryability.AfterConfigurationChange, result.Error?.Retryability);
        Assert.Equal(NetworkEgressFailureCodes.PortDenied, result.Error?.Code);
        Assert.DoesNotContain("fixture-secret", result.Error?.SafeMessage ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(OriginalHost, result.Error?.SafeMessage ?? string.Empty, StringComparison.Ordinal);
    }

    /// <summary>Proves Npgsql retains the original SNI hostname with offline, download-disabled chain validation.</summary>
    [Fact]
    public void NpgsqlTlsOptionsPreserveHostAndForbidCertificateDownloads()
    {
        SslClientAuthenticationOptions options = new();

        NpgsqlAuthenticatedExecutor.ConfigureSslOptions(options, OriginalHost);

        Assert.Equal(OriginalHost, options.TargetHost);
        Assert.Equal(X509RevocationMode.Offline, options.CertificateRevocationCheckMode);
        X509ChainPolicy policy = Assert.IsType<X509ChainPolicy>(options.CertificateChainPolicy);
        Assert.True(policy.DisableCertificateDownloads);
        Assert.Equal(X509RevocationMode.Offline, policy.RevocationMode);
        Assert.Equal(X509RevocationFlag.EntireChain, policy.RevocationFlag);
        Assert.Equal(X509VerificationFlags.NoFlag, policy.VerificationFlags);
        Assert.Equal(X509ChainTrustMode.System, policy.TrustMode);
        Assert.Empty(policy.CustomTrustStore);
        Assert.Contains(
            policy.ApplicationPolicy.Cast<System.Security.Cryptography.Oid>(),
            oid => oid.Value == "1.3.6.1.5.5.7.3.1");
        Assert.Null(options.RemoteCertificateValidationCallback);
    }

    /// <summary>Proves an existing Unix-domain socket path remains local and never invokes network authorisation.</summary>
    [Fact]
    public async Task UnixSocketEndpointBypassesNetworkResolutionWithoutMutation()
    {
        UnexpectedAuthorizer authorizer = new();
        PostgreSqlEndpoint endpoint = Endpoint("/var/run/postgresql");

        PostgreSqlNetworkResolution resolution = await PostgreSqlNetworkEgress.ResolveAsync(
            authorizer,
            endpoint,
            CancellationToken.None);

        Assert.True(resolution.IsApproved);
        Assert.True(resolution.IsLocalSocket);
        Assert.Null(resolution.ApprovedAddress);
        Assert.Equal(endpoint, resolution.Endpoint);
        Assert.Equal(0, authorizer.CallCount);
    }

    /// <summary>Creates one policy-approved authoriser returning the deterministic documentation address.</summary>
    /// <returns>An authoriser that records its single expected request.</returns>
    private static RecordingAuthorizer ApprovedAuthorizer() =>
        new(NetworkEgressResolution.Approved([ApprovedAddress]));

    /// <summary>Creates one policy-denied authoriser with the supplied sanitised code.</summary>
    /// <param name="failureCode">Stable network-egress denial code.</param>
    /// <returns>An authoriser that records its single expected request.</returns>
    private static RecordingAuthorizer DeniedAuthorizer(string failureCode) =>
        new(NetworkEgressResolution.Denied(failureCode));

    /// <summary>Creates a validated provider-specific endpoint without secret material.</summary>
    /// <param name="host">Hostname or Unix-domain socket path.</param>
    /// <returns>A PostgreSQL endpoint using the normal verified-TLS contract.</returns>
    private static PostgreSqlEndpoint Endpoint(string host = OriginalHost) =>
        new(host, 5432, "postgres", "pg_isready", null, "verify-full");

    /// <summary>Creates the provider-neutral representation consumed by the public provider adapter.</summary>
    /// <returns>A PostgreSQL endpoint containing only the original hostname and port.</returns>
    private static ProviderEndpoint ProviderEndpoint() =>
        new(
            ProviderType.Parse("postgresql"),
            [
                new KeyValuePair<string, string>("host", OriginalHost),
                new KeyValuePair<string, string>("port", "5432"),
            ]);

    /// <summary>Returns one preselected resolution and records the exact request without DNS.</summary>
    private sealed class RecordingAuthorizer(NetworkEgressResolution resolution) : INetworkEgressAuthorizer
    {
        /// <summary>Gets the exact request passed by the provider boundary.</summary>
        internal NetworkEgressRequest? Request { get; private set; }

        /// <inheritdoc />
        public ValueTask<NetworkEgressResolution> ResolveAndAuthoriseAsync(
            NetworkEgressRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return ValueTask.FromResult(resolution);
        }
    }

    /// <summary>Fails if a local Unix-domain socket attempts to invoke network authorisation.</summary>
    private sealed class UnexpectedAuthorizer : INetworkEgressAuthorizer
    {
        /// <summary>Gets the number of unexpected network-authorisation attempts.</summary>
        internal int CallCount { get; private set; }

        /// <inheritdoc />
        public ValueTask<NetworkEgressResolution> ResolveAndAuthoriseAsync(
            NetworkEgressRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            throw new InvalidOperationException("A Unix-domain socket attempted network authorisation.");
        }
    }

    /// <summary>Records the already approved address without opening a real socket.</summary>
    private sealed class RecordingTcpConnector : IPostgreSqlTcpConnector
    {
        /// <summary>Gets the approved address supplied to the connector.</summary>
        internal IPAddress? Address { get; private set; }

        /// <summary>Gets the validated destination port supplied to the connector.</summary>
        internal int Port { get; private set; }

        /// <summary>Gets the number of synthetic connection attempts.</summary>
        internal int CallCount { get; private set; }

        /// <inheritdoc />
        public ValueTask ConnectAsync(
            IPAddress address,
            int port,
            CancellationToken cancellationToken)
        {
            Address = address;
            Port = port;
            CallCount++;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Returns a discovered utility path without opening a process.</summary>
    private sealed class FoundDiscovery : IPostgreSqlExecutableDiscovery
    {
        /// <inheritdoc />
        public PostgreSqlExecutableDiscoveryResult Resolve(PostgreSqlExecutableDiscoveryRequest request) =>
            new(PostgreSqlExecutableDiscoveryState.Found, "pg_isready", "fixture");
    }

    /// <summary>Returns no utility path so the readiness executor must use its transport-only fallback.</summary>
    private sealed class NotFoundDiscovery : IPostgreSqlExecutableDiscovery
    {
        /// <inheritdoc />
        public PostgreSqlExecutableDiscoveryResult Resolve(PostgreSqlExecutableDiscoveryRequest request) =>
            new(PostgreSqlExecutableDiscoveryState.NotFound, null, "fixture");
    }

    /// <summary>Fails if a denied readiness request reaches the TCP fallback.</summary>
    private sealed class UnexpectedTransportProbe : IPostgreSqlTransportProbe
    {
        /// <inheritdoc />
        public ValueTask<PostgreSqlTransportResult> ProbeAsync(
            PostgreSqlEndpoint endpoint,
            TimeSpan timeout,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("A denied readiness request reached the TCP fallback.");
    }

    /// <summary>Fails if an unauthenticated readiness test invokes the authenticated executor.</summary>
    private sealed class UnexpectedAuthenticatedExecutor : IPostgreSqlAuthenticatedExecutor
    {
        /// <inheritdoc />
        public ValueTask<PostgreSqlAuthenticatedResult> ExecuteAsync(
            PostgreSqlEndpoint endpoint,
            IProviderCredential credential,
            TimeSpan timeout,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("An unauthenticated test invoked the authenticated executor.");
    }

    /// <summary>Fails if an authenticated test invokes the readiness executor.</summary>
    private sealed class UnexpectedReadinessExecutor : IPostgreSqlReadinessExecutor
    {
        /// <inheritdoc />
        public ValueTask<PostgreSqlReadinessResult> ExecuteAsync(
            PostgreSqlEndpoint endpoint,
            TimeSpan timeout,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("An authenticated test invoked the readiness executor.");
    }
}
