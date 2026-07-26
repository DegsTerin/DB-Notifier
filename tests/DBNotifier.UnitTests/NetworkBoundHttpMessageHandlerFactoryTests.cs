// Module purpose: Verifies pinned HTTP connection seams and no-download TLS configuration without opening a real socket.
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Infrastructure.Http;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.UnitTests;

/// <summary>Protects the HTTP egress boundary using only injected authorisation and stream fixtures.</summary>
public sealed class NetworkBoundHttpMessageHandlerFactoryTests
{
    [Fact]
    public void HandlerDisablesAmbientEgressAndUsesOfflineServerValidation()
    {
        NetworkBoundHttpMessageHandlerFactory factory = new(
            new StaticAuthorizer(NetworkEgressResolution.Approved([IPAddress.Loopback])));

        using SocketsHttpHandler handler = factory.Create(NetworkEgressPolicyIds.AgentSynchronization);

        Assert.False(handler.AllowAutoRedirect);
        Assert.False(handler.UseProxy);
        Assert.False(handler.UseCookies);
        Assert.False(handler.PreAuthenticate);
        Assert.Null(handler.Credentials);
        Assert.Equal(DecompressionMethods.None, handler.AutomaticDecompression);
        Assert.NotNull(handler.ConnectCallback);
        X509ChainPolicy policy = Assert.IsType<X509ChainPolicy>(handler.SslOptions.CertificateChainPolicy);
        Assert.True(policy.DisableCertificateDownloads);
        Assert.Equal(X509RevocationMode.Offline, policy.RevocationMode);
        Assert.Equal(X509RevocationFlag.EntireChain, policy.RevocationFlag);
        Assert.Equal(X509VerificationFlags.NoFlag, policy.VerificationFlags);
        Assert.Equal(X509ChainTrustMode.System, policy.TrustMode);
        Assert.Empty(policy.CustomTrustStore);
        Assert.Contains(policy.ApplicationPolicy.Cast<Oid>(), oid => oid.Value == "1.3.6.1.5.5.7.3.1");
        Assert.Null(handler.SslOptions.RemoteCertificateValidationCallback);
    }

    [Fact]
    public void OptionalClientIdentityUsesAnOfflineCertificateContext()
    {
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new(
            "CN=DBNotifier-Network-Egress-Test",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(5));
        NetworkBoundHttpMessageHandlerFactory factory = new(
            new StaticAuthorizer(NetworkEgressResolution.Approved([IPAddress.Loopback])));

        using SocketsHttpHandler handler = factory.Create(
            NetworkEgressPolicyIds.AgentSynchronization,
            certificate);

        Assert.NotNull(handler.SslOptions.ClientCertificateContext);
        Assert.Null(handler.SslOptions.ClientCertificates);
    }

    [Fact]
    public async Task PhysicalConnectionsAreReauthorisedAndUseOnlyPinnedAddresses()
    {
        SequenceAuthorizer authorizer = new(
            NetworkEgressResolution.Approved([IPAddress.Parse("203.0.113.8")]),
            NetworkEgressResolution.Approved([IPAddress.Parse("203.0.113.9")]));
        RecordingConnector connector = new();
        NetworkBoundHttpMessageHandlerFactory factory = new(authorizer, connector);
        DnsEndPoint endpoint = new("service.example.test", 443);

        await using Stream first = await factory.ConnectPinnedAsync(
            NetworkEgressPolicyIds.AgentSynchronization,
            endpoint,
            CancellationToken.None);
        await using Stream second = await factory.ConnectPinnedAsync(
            NetworkEgressPolicyIds.AgentSynchronization,
            endpoint,
            CancellationToken.None);

        Assert.Equal(2, authorizer.CallCount);
        Assert.All(authorizer.Requests, request =>
        {
            Assert.Equal("service.example.test", request.Host);
            Assert.Equal(443, request.Port);
        });
        Assert.Equal(
            [IPAddress.Parse("203.0.113.8"), IPAddress.Parse("203.0.113.9")],
            connector.Addresses);
    }

    [Fact]
    public async Task DeniedResolutionNeverReachesTheSocketConnector()
    {
        RecordingConnector connector = new();
        NetworkBoundHttpMessageHandlerFactory factory = new(
            new StaticAuthorizer(NetworkEgressResolution.Denied(NetworkEgressFailureCodes.AddressDenied)),
            connector);

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await factory.ConnectPinnedAsync(
                NetworkEgressPolicyIds.HumanIdentity,
                new DnsEndPoint("identity.example.test", 443),
                CancellationToken.None));

        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, exception.Message);
        Assert.Empty(connector.Addresses);
    }

    [Fact]
    public async Task ConnectorTriesOnlyTheApprovedSetAndReturnsTheFirstSuccessfulStream()
    {
        IPAddress firstAddress = IPAddress.Parse("203.0.113.8");
        IPAddress secondAddress = IPAddress.Parse("203.0.113.9");
        SequencedConnector connector = new(firstAddress, secondAddress);
        NetworkBoundHttpMessageHandlerFactory factory = new(
            new StaticAuthorizer(NetworkEgressResolution.Approved([firstAddress, secondAddress])),
            connector);

        await using Stream result = await factory.ConnectPinnedAsync(
            NetworkEgressPolicyIds.AgentSynchronization,
            new DnsEndPoint("service.example.test", 443),
            CancellationToken.None);

        Assert.IsType<MemoryStream>(result);
        Assert.Equal([firstAddress, secondAddress], connector.Addresses);
    }

    [Fact]
    public async Task ExhaustedApprovedAddressesReturnOnlyTheSanitisedConnectionCode()
    {
        NetworkBoundHttpMessageHandlerFactory factory = new(
            new StaticAuthorizer(NetworkEgressResolution.Approved(
                [IPAddress.Parse("203.0.113.8"), IPAddress.Parse("203.0.113.9")])),
            new FailingConnector());

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await factory.ConnectPinnedAsync(
                NetworkEgressPolicyIds.AgentSynchronization,
                new DnsEndPoint("secret-host.example.test", 443),
                CancellationToken.None));

        Assert.Equal(NetworkEgressFailureCodes.ConnectionFailed, exception.Message);
        Assert.DoesNotContain("secret-host", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OfflinePoliciesAreFreshAndRequireTheCorrectExtendedKeyUse()
    {
        X509ChainPolicy server = OfflineCertificateChainPolicy.CreateServerAuthentication();
        X509ChainPolicy anotherServer = OfflineCertificateChainPolicy.CreateServerAuthentication();
        X509ChainPolicy client = OfflineCertificateChainPolicy.CreateClientAuthentication();

        Assert.NotSame(server, anotherServer);
        Assert.All([server, anotherServer, client], policy =>
        {
            Assert.True(policy.DisableCertificateDownloads);
            Assert.Equal(X509RevocationMode.Offline, policy.RevocationMode);
            Assert.Equal(X509RevocationFlag.EntireChain, policy.RevocationFlag);
            Assert.Equal(X509VerificationFlags.NoFlag, policy.VerificationFlags);
            Assert.Equal(X509ChainTrustMode.System, policy.TrustMode);
            Assert.Empty(policy.CustomTrustStore);
        });
        Assert.Contains(server.ApplicationPolicy.Cast<Oid>(), oid => oid.Value == "1.3.6.1.5.5.7.3.1");
        Assert.Contains(client.ApplicationPolicy.Cast<Oid>(), oid => oid.Value == "1.3.6.1.5.5.7.3.2");
    }

    [Fact]
    public void ApprovedResultDeepCopiesSuppliedAddresses()
    {
        IPAddress supplied = IPAddress.Parse("fe80::1");

        NetworkEgressResolution result = NetworkEgressResolution.Approved([supplied]);
        supplied.ScopeId = 42;
        IPAddress firstRead = Assert.Single(result.Addresses);
        firstRead.ScopeId = 84;

        Assert.Equal(0, Assert.Single(result.Addresses).ScopeId);
    }

    [Fact]
    public void DeniedResultRejectsAnUnrecognisedOrPotentiallySensitiveCode()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => NetworkEgressResolution.Denied("network.secret-host.example.test"));

        Assert.DoesNotContain("secret-host", exception.Message, StringComparison.Ordinal);
    }

    private sealed class StaticAuthorizer(NetworkEgressResolution result) : INetworkEgressAuthorizer
    {
        public ValueTask<NetworkEgressResolution> ResolveAndAuthoriseAsync(
            NetworkEgressRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class SequenceAuthorizer(params NetworkEgressResolution[] results) : INetworkEgressAuthorizer
    {
        private readonly Queue<NetworkEgressResolution> results = new(results);

        internal int CallCount { get; private set; }

        internal List<NetworkEgressRequest> Requests { get; } = [];

        public ValueTask<NetworkEgressResolution> ResolveAndAuthoriseAsync(
            NetworkEgressRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            Requests.Add(request);
            return ValueTask.FromResult(results.Dequeue());
        }
    }

    private sealed class RecordingConnector : INetworkStreamConnector
    {
        internal List<IPAddress> Addresses { get; } = [];

        public ValueTask<Stream> ConnectAsync(
            IPAddress address,
            int port,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Addresses.Add(address);
            return ValueTask.FromResult<Stream>(new MemoryStream());
        }
    }

    private sealed class SequencedConnector(
        IPAddress expectedFirst,
        IPAddress expectedSecond) : INetworkStreamConnector
    {
        internal List<IPAddress> Addresses { get; } = [];

        public ValueTask<Stream> ConnectAsync(
            IPAddress address,
            int port,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Addresses.Add(address);
            if (address.Equals(expectedFirst))
            {
                throw new SocketException((int)SocketError.ConnectionRefused);
            }
            Assert.Equal(expectedSecond, address);
            return ValueTask.FromResult<Stream>(new MemoryStream());
        }
    }

    private sealed class FailingConnector : INetworkStreamConnector
    {
        public ValueTask<Stream> ConnectAsync(
            IPAddress address,
            int port,
            CancellationToken cancellationToken) =>
            throw new SocketException((int)SocketError.ConnectionRefused);
    }
}
