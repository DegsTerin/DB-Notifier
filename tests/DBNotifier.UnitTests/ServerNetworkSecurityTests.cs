// Module purpose: Verifies fail-closed OIDC backchannel and inbound Agent TLS policy without real network access.
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DBNotifier.Infrastructure.Http;
using DBNotifier.Infrastructure.Security;
using DBNotifier.Provider.Abstractions;
using DBNotifier.Server.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;

namespace DBNotifier.UnitTests;

/// <summary>Protects the Server's human-identity egress and inbound Agent certificate boundaries.</summary>
public sealed class ServerNetworkSecurityTests
{
    private static readonly Uri Authority = new("https://identity.example/tenant");

    /// <summary>Proves that absent human identity configuration creates no OIDC backchannel.</summary>
    [Fact]
    public void AbsentOidcConfigurationRemainsDormant()
    {
        JwtBearerOptions options = new();

        bool configured = HumanOidcNetworkSecurity.Configure(
            options,
            Configuration(),
            Policies(),
            HandlerFactory());

        Assert.False(configured);
        Assert.Null(options.Authority);
        Assert.Null(options.BackchannelHttpHandler);
        Assert.False(options.IncludeErrorDetails);
        Assert.True(options.RequireHttpsMetadata);
    }

    /// <summary>Proves that a partial or structurally invalid OIDC setting fails closed.</summary>
    /// <param name="authority">Configured authority value.</param>
    /// <param name="audience">Configured audience value.</param>
    [Theory]
    [InlineData("https://identity.example", null)]
    [InlineData(null, "db-notifier")]
    [InlineData("http://identity.example", "db-notifier")]
    [InlineData("https://user@identity.example", "db-notifier")]
    [InlineData("https://identity.example?redirect=https://elsewhere.example", "db-notifier")]
    [InlineData("https://identity.example", " audience")]
    public void PartialOrInvalidOidcConfigurationIsRejected(
        string? authority,
        string? audience)
    {
        JwtBearerOptions options = new();

        Assert.Throws<InvalidOperationException>(() => HumanOidcNetworkSecurity.Configure(
            options,
            Configuration(authority, audience),
            Policies(),
            HandlerFactory()));
    }

    /// <summary>Proves that configured OIDC cannot rely on a missing or wrong-port network policy.</summary>
    /// <param name="allowedPort">Port admitted by the compiled policy, or null for no policy.</param>
    /// <param name="expectedCode">Sanitised policy failure expected from startup configuration.</param>
    [Theory]
    [InlineData(null, NetworkEgressFailureCodes.PolicyUnavailable)]
    [InlineData(8443, NetworkEgressFailureCodes.PortDenied)]
    public void ConfiguredOidcRequiresExactNetworkPolicy(
        int? allowedPort,
        string expectedCode)
    {
        JwtBearerOptions options = new();
        NetworkEgressPolicySet policies = allowedPort is null
            ? Policies()
            : Policies(allowedPort.Value);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => HumanOidcNetworkSecurity.Configure(
                options,
                Configuration(Authority.AbsoluteUri, "db-notifier"),
                policies,
                HandlerFactory()));

        Assert.Equal(expectedCode, exception.Message);
    }

    /// <summary>Proves that complete configuration uses exact issuer, audience and bounded policy-bound backchannel.</summary>
    [Fact]
    public void CompleteOidcConfigurationUsesPinnedBackchannel()
    {
        JwtBearerOptions options = new();

        bool configured = HumanOidcNetworkSecurity.Configure(
            options,
            Configuration(Authority.AbsoluteUri, "db-notifier"),
            Policies(443),
            HandlerFactory());

        Assert.True(configured);
        Assert.Equal(Authority.AbsoluteUri.TrimEnd('/'), options.Authority);
        Assert.Equal("db-notifier", options.Audience);
        Assert.Equal(TimeSpan.FromSeconds(10), options.BackchannelTimeout);
        Assert.IsType<OidcBackchannelPolicyHandler>(options.BackchannelHttpHandler);
        Assert.Equal(options.Authority, options.TokenValidationParameters.ValidIssuer);
        Assert.Equal(options.Audience, options.TokenValidationParameters.ValidAudience);
        options.BackchannelHttpHandler.Dispose();
    }

    /// <summary>Proves that a same-origin JSON response is buffered under the exact response ceiling.</summary>
    [Fact]
    public async Task OidcBackchannelAcceptsBoundedSameOriginJson()
    {
        StubHttpHandler inner = new(_ => JsonResponse("{}"));
        using OidcBackchannelPolicyHandler handler = new(Authority, inner);
        using HttpMessageInvoker invoker = new(handler, disposeHandler: false);
        using HttpRequestMessage request = new(HttpMethod.Get, "https://identity.example/signing-keys");

        using HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);
        string payload = await response.Content.ReadAsStringAsync();

        Assert.Equal("{}", payload);
        Assert.Equal(1, inner.CallCount);
    }

    /// <summary>Proves that metadata cannot redirect or nominate a signing-key endpoint on another origin.</summary>
    /// <param name="requestUri">URI sent through the policy wrapper.</param>
    /// <param name="redirect">Whether the admitted origin returns a redirect response.</param>
    [Theory]
    [InlineData("https://elsewhere.example/keys", false)]
    [InlineData("https://identity.example/keys", true)]
    public async Task OidcBackchannelRejectsCrossOriginAndRedirect(
        string requestUri,
        bool redirect)
    {
        StubHttpHandler inner = new(_ => redirect
            ? new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                Headers = { Location = new Uri("https://elsewhere.example/keys") },
            }
            : JsonResponse("{}"));
        using OidcBackchannelPolicyHandler handler = new(Authority, inner);
        using HttpMessageInvoker invoker = new(handler, disposeHandler: false);
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(redirect ? 1 : 0, inner.CallCount);
    }

    /// <summary>Proves that untrusted content type and an actual body beyond the ceiling are rejected.</summary>
    /// <param name="oversized">Whether the response uses an unknown-length body above the exact byte ceiling.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OidcBackchannelRejectsInvalidContent(bool oversized)
    {
        StubHttpHandler inner = new(_ =>
        {
            HttpContent content = oversized
                ? new UnknownLengthContent(new byte[(512 * 1024) + 1], "application/json")
                : new StringContent("{}", Encoding.UTF8, "text/plain");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });
        using OidcBackchannelPolicyHandler handler = new(Authority, inner);
        using HttpMessageInvoker invoker = new(handler, disposeHandler: false);
        using HttpRequestMessage request = new(HttpMethod.Get, "https://identity.example/metadata");

        await Assert.ThrowsAsync<HttpRequestException>(
            () => invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(1, inner.CallCount);
    }

    /// <summary>Proves that Kestrel uses explicit offline revocation and no certificate downloads for Agent TLS.</summary>
    [Fact]
    public void InboundAgentTlsUsesOfflineNoDownloadPolicy()
    {
        SslServerAuthenticationOptions options = new();

        InboundAgentTlsSecurity.Apply(options);

        Assert.Equal(X509RevocationMode.Offline, options.CertificateRevocationCheckMode);
        Assert.NotNull(options.CertificateChainPolicy);
        Assert.True(options.CertificateChainPolicy.DisableCertificateDownloads);
        Assert.Equal(X509RevocationMode.Offline, options.CertificateChainPolicy.RevocationMode);
        Assert.Equal(X509RevocationFlag.EntireChain, options.CertificateChainPolicy.RevocationFlag);
        Assert.Equal(X509VerificationFlags.NoFlag, options.CertificateChainPolicy.VerificationFlags);
        Assert.Contains(
            options.CertificateChainPolicy.ApplicationPolicy.Cast<Oid>(),
            usage => usage.Value == "1.3.6.1.5.5.7.3.2");
    }

    /// <summary>Proves that Agent leaves require non-CA, digital-signature and explicit ClientAuth extensions.</summary>
    /// <param name="certificateAuthority">Whether the leaf is incorrectly marked as a CA.</param>
    /// <param name="includeDigitalSignature">Whether digital-signature key usage is present.</param>
    /// <param name="enhancedKeyUsageOid">Optional exact EKU placed on the certificate.</param>
    /// <param name="expected">Expected profile decision.</param>
    [Theory]
    [InlineData(false, true, "1.3.6.1.5.5.7.3.2", true)]
    [InlineData(true, true, "1.3.6.1.5.5.7.3.2", false)]
    [InlineData(false, false, "1.3.6.1.5.5.7.3.2", false)]
    [InlineData(false, true, null, false)]
    [InlineData(false, true, "1.3.6.1.5.5.7.3.1", false)]
    public void AgentCertificateRequiresExactLeafProfile(
        bool certificateAuthority,
        bool includeDigitalSignature,
        string? enhancedKeyUsageOid,
        bool expected)
    {
        using X509Certificate2 certificate = CreateCertificate(
            certificateAuthority,
            includeDigitalSignature,
            enhancedKeyUsageOid);

        Assert.Equal(expected, AgentClientCertificateProfile.IsValid(certificate));
    }

    private static IConfiguration Configuration(
        string? authority = null,
        string? audience = null)
    {
        Dictionary<string, string?> values = [];
        if (authority is not null)
        {
            values["HumanAuthentication:Authority"] = authority;
        }
        if (audience is not null)
        {
            values["HumanAuthentication:Audience"] = audience;
        }
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static NetworkEgressPolicySet Policies(int? allowedPort = null)
    {
        NetworkEgressOptions options = new();
        if (allowedPort is not null)
        {
            options.Policies[NetworkEgressPolicyIds.HumanIdentity] = new NetworkEgressPolicyOptions
            {
                AllowedCidrs = ["203.0.113.0/24"],
                AllowedPorts = [allowedPort.Value],
            };
        }
        return NetworkEgressPolicySet.Compile(options);
    }

    private static NetworkBoundHttpMessageHandlerFactory HandlerFactory() =>
        new(new NeverCalledAuthorizer());

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private static X509Certificate2 CreateCertificate(
        bool certificateAuthority,
        bool includeDigitalSignature,
        string? enhancedKeyUsageOid)
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest request = new(
            "CN=DB-Notifier Agent Network Fixture",
            key,
            HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(certificateAuthority, false, 0, true));
        if (includeDigitalSignature)
        {
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        }
        if (enhancedKeyUsageOid is not null)
        {
            OidCollection usages = new();
            usages.Add(new Oid(enhancedKeyUsageOid));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(usages, false));
        }
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddHours(1));
    }

    private sealed class NeverCalledAuthorizer : INetworkEgressAuthorizer
    {
        /// <inheritdoc />
        public ValueTask<NetworkEgressResolution> ResolveAndAuthoriseAsync(
            NetworkEgressRequest request,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The configuration test must not perform network admission.");
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) :
        HttpMessageHandler
    {
        internal int CallCount { get; private set; }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class UnknownLengthContent : HttpContent
    {
        private readonly byte[] content;

        internal UnknownLengthContent(byte[] content, string mediaType)
        {
            this.content = content;
            Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
        }

        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context) =>
            stream.WriteAsync(content).AsTask();

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
