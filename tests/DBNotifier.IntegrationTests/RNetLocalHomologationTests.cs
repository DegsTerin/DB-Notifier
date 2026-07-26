// Module purpose: Runs the marker-gated local R-NET DNS, PKI and OIDC homologation matrix without contacting external infrastructure.
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Infrastructure.Http;
using DBNotifier.Infrastructure.Security;
using DBNotifier.Provider.Abstractions;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>
/// Proves the directly affected R-NET boundaries against controlled loopback fixtures while ordinary integration
/// runs remain inert. The owning runner supplies synthetic public PKI evidence without changing system root trust.
/// </summary>
public sealed class RNetLocalHomologationTests
{
    private const int DnsFixturePort = 443;

    /// <summary>
    /// Verifies the real localhost resolver, atomic mixed-answer refusal and fresh admission after a controlled DNS
    /// answer changes. No test answer leaves the local process or reaches a socket before positive admission.
    /// </summary>
    /// <returns>A task completing after every DNS admission and pinning assertion.</returns>
    [Fact]
    [Trait("Category", "RNetLocalHomologation")]
    public async Task LocalDnsProvesResolverAdmissionMixedAnswersAndRebindingResistance()
    {
        if (!RNetLocalHomologationActivation.IsActive())
        {
            return;
        }

        NetworkEgressPolicySet loopbackPolicy = RNetNetworkFixtures.CreatePolicy(
            DnsFixturePort,
            "127.0.0.0/8",
            "::1/128");
        NetworkEgressAuthorizer systemAuthorizer = new(
            loopbackPolicy,
            new SystemDnsResolver());

        NetworkEgressResolution localhost = await systemAuthorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest(
                NetworkEgressPolicyIds.HumanIdentity,
                "localhost",
                DnsFixturePort),
            CancellationToken.None);

        Assert.True(localhost.IsApproved);
        Assert.NotEmpty(localhost.Addresses);
        Assert.All(localhost.Addresses, address => Assert.True(IPAddress.IsLoopback(address)));

        ControlledDnsResolver mixedResolver = new(
            [
                IPAddress.Loopback,
                IPAddress.Parse("169.254.169.254"),
            ]);
        NetworkEgressAuthorizer mixedAuthorizer = new(loopbackPolicy, mixedResolver);

        NetworkEgressResolution mixed = await mixedAuthorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest(
                NetworkEgressPolicyIds.HumanIdentity,
                RNetFixturePaths.IdpHost,
                DnsFixturePort),
            CancellationToken.None);

        Assert.False(mixed.IsApproved);
        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, mixed.FailureCode);
        Assert.Empty(mixed.Addresses);
        Assert.Equal(1, mixedResolver.CallCount);

        ControlledDnsResolver changingResolver = new(
            [IPAddress.Loopback],
            [IPAddress.Parse("169.254.169.254")],
            [IPAddress.Loopback]);
        NetworkEgressAuthorizer changingAuthorizer = new(loopbackPolicy, changingResolver);
        RecordingNetworkStreamConnector connector = new();
        NetworkBoundHttpMessageHandlerFactory handlerFactory = new(
            changingAuthorizer,
            connector);
        DnsEndPoint endpoint = new(RNetFixturePaths.IdpHost, DnsFixturePort);

        await using Stream first = await handlerFactory.ConnectPinnedAsync(
            NetworkEgressPolicyIds.HumanIdentity,
            endpoint,
            CancellationToken.None);
        HttpRequestException refused = await Assert.ThrowsAsync<HttpRequestException>(
            () => handlerFactory.ConnectPinnedAsync(
                    NetworkEgressPolicyIds.HumanIdentity,
                    endpoint,
                    CancellationToken.None)
                .AsTask());
        await using Stream recovered = await handlerFactory.ConnectPinnedAsync(
            NetworkEgressPolicyIds.HumanIdentity,
            endpoint,
            CancellationToken.None);

        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, refused.Message);
        Assert.Equal(3, changingResolver.CallCount);
        Assert.Equal(2, connector.CallCount);
        Assert.All(connector.Addresses, address => Assert.Equal(IPAddress.Loopback, address));
    }

    /// <summary>
    /// Verifies process-local custom trust, cached offline revocation, exact server EKU and TLS hostname checks
    /// using only runner-owned synthetic certificates on loopback listeners.
    /// </summary>
    /// <returns>A task completing after the positive and all fail-closed certificate cases.</returns>
    [Fact]
    [Trait("Category", "RNetLocalHomologation")]
    public async Task LocalPkiProvesOfflineTrustRevocationNameAndUsageBoundaries()
    {
        if (!RNetLocalHomologationActivation.IsActive())
        {
            return;
        }

        RNetFixturePaths paths = RNetFixturePaths.LoadRequired();
        using X509Certificate2 trustedRoot =
            RNetFixturePaths.LoadPublicCertificate(paths.RootCertificate);
        using X509Certificate2 missingCrlRoot =
            RNetFixturePaths.LoadPublicCertificate(paths.MissingCrlRootCertificate);
        using X509Certificate2 valid =
            RNetFixturePaths.LoadPublicCertificate(paths.ValidCertificate);
        using X509Certificate2 wrongName =
            RNetFixturePaths.LoadPublicCertificate(paths.WrongNameCertificate);
        using X509Certificate2 wrongUsage =
            RNetFixturePaths.LoadPublicCertificate(paths.WrongUsageCertificate);
        using X509Certificate2 revoked =
            RNetFixturePaths.LoadPublicCertificate(paths.RevokedCertificate);
        using X509Certificate2 untrusted =
            RNetFixturePaths.LoadPublicCertificate(paths.UntrustedCertificate);
        using X509Certificate2 missingCrl =
            RNetFixturePaths.LoadPublicCertificate(paths.MissingCrlCertificate);
        using RNetCustomRootTrustPolicyFactory trustedPolicyFactory =
            RNetCustomRootTrustPolicyFactory.Load(paths.RootCertificate);
        using RNetCustomRootTrustPolicyFactory missingCrlPolicyFactory =
            RNetCustomRootTrustPolicyFactory.Load(paths.MissingCrlRootCertificate);

        Assert.False(trustedRoot.HasPrivateKey);
        Assert.False(missingCrlRoot.HasPrivateKey);
        Assert.True(RNetPkiAssertions.IsAbsentFromCurrentUserRoot(trustedRoot));
        Assert.True(RNetPkiAssertions.IsAbsentFromCurrentUserRoot(missingCrlRoot));
        Assert.True(File.Exists(paths.RootCertificateRevocationList));

        RNetChainResult trustedResult =
            RNetPkiAssertions.BuildOfflineServerChain(valid, trustedPolicyFactory);
        RNetChainResult wrongNameResult =
            RNetPkiAssertions.BuildOfflineServerChain(wrongName, trustedPolicyFactory);
        RNetChainResult wrongUsageResult =
            RNetPkiAssertions.BuildOfflineServerChain(wrongUsage, trustedPolicyFactory);
        RNetChainResult revokedResult =
            RNetPkiAssertions.BuildOfflineServerChain(revoked, trustedPolicyFactory);
        RNetChainResult untrustedResult =
            RNetPkiAssertions.BuildOfflineServerChain(untrusted, trustedPolicyFactory);
        RNetChainResult missingCrlResult =
            RNetPkiAssertions.BuildOfflineServerChain(missingCrl, missingCrlPolicyFactory);

        Assert.True(trustedResult.Succeeded);
        Assert.True(trustedResult.EndsAtExpectedRoot);
        Assert.True(wrongNameResult.Succeeded);
        Assert.True(wrongNameResult.EndsAtExpectedRoot);
        Assert.False(wrongUsageResult.Succeeded);
        Assert.True(wrongUsageResult.EndsAtExpectedRoot);
        Assert.True(wrongUsageResult.HasAny(X509ChainStatusFlags.NotValidForUsage));
        Assert.False(revokedResult.Succeeded);
        Assert.True(revokedResult.EndsAtExpectedRoot);
        Assert.True(revokedResult.HasAny(X509ChainStatusFlags.Revoked));
        Assert.False(untrustedResult.Succeeded);
        Assert.False(untrustedResult.EndsAtExpectedRoot);
        Assert.True(untrustedResult.HasAny(
            X509ChainStatusFlags.UntrustedRoot,
            X509ChainStatusFlags.PartialChain));
        Assert.False(missingCrlResult.Succeeded);
        Assert.True(missingCrlResult.EndsAtExpectedRoot);
        Assert.True(missingCrlResult.HasAny(
            X509ChainStatusFlags.RevocationStatusUnknown,
            X509ChainStatusFlags.OfflineRevocation));

        await RNetPkiAssertions.AssertHttpsAcceptedAsync(
            paths.ValidCertificate,
            paths.ValidPrivateKey,
            trustedPolicyFactory);
        await RNetPkiAssertions.AssertHttpsRejectedAsync(
            paths.WrongNameCertificate,
            paths.WrongNamePrivateKey,
            trustedPolicyFactory);
        await RNetPkiAssertions.AssertHttpsServerCertificateRejectedAsync(
            paths.WrongUsageCertificate,
            paths.WrongUsagePrivateKey);
        await RNetPkiAssertions.AssertHttpsRejectedAsync(
            paths.RevokedCertificate,
            paths.RevokedPrivateKey,
            trustedPolicyFactory);
        await RNetPkiAssertions.AssertHttpsRejectedAsync(
            paths.UntrustedCertificate,
            paths.UntrustedPrivateKey,
            trustedPolicyFactory);
        await RNetPkiAssertions.AssertHttpsRejectedAsync(
            paths.MissingCrlCertificate,
            paths.MissingCrlPrivateKey,
            missingCrlPolicyFactory);
    }

    /// <summary>
    /// Verifies actual local HTTPS discovery, JWKS retrieval and JWT validation, then refuses redirects,
    /// cross-origin signing keys and tokens with an invalid issuer, audience or signing key.
    /// </summary>
    /// <returns>A task completing after the complete local human-identity matrix.</returns>
    [Fact]
    [Trait("Category", "RNetLocalHomologation")]
    public async Task LocalHttpsIdentityProviderProvesDiscoveryJwksAndJwtBoundaries()
    {
        if (!RNetLocalHomologationActivation.IsActive())
        {
            return;
        }

        RNetFixturePaths paths = RNetFixturePaths.LoadRequired();
        using RNetCustomRootTrustPolicyFactory trustPolicyFactory =
            RNetCustomRootTrustPolicyFactory.Load(paths.RootCertificate);
        await using RNetLocalIdentityProvider identityProvider =
            await RNetLocalIdentityProvider.StartAsync(
                paths.ValidCertificate,
                paths.ValidPrivateKey,
                trustPolicyFactory);

        string validToken = identityProvider.CreateToken(
            identityProvider.Issuer,
            RNetLocalIdentityProvider.Audience);

        identityProvider.BeginScenario(RNetIdentityProviderScenario.Valid);
        RNetJwtAuthenticationResult accepted =
            await identityProvider.RequestProtectedResourceAsync(validToken);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(
            RNetLocalIdentityProvider.Subject,
            accepted.Subject);
        Assert.True(identityProvider.DiscoveryRequestCount >= 1);
        Assert.True(identityProvider.SigningKeyRequestCount >= 1);

        identityProvider.BeginScenario(RNetIdentityProviderScenario.Redirect);
        RNetJwtAuthenticationResult redirected =
            await identityProvider.RequestProtectedResourceAsync(validToken);
        Assert.Equal(HttpStatusCode.Unauthorized, redirected.StatusCode);
        Assert.True(identityProvider.DiscoveryRequestCount >= 1);
        Assert.Equal(0, identityProvider.RedirectTargetRequestCount);
        Assert.Equal(0, identityProvider.SigningKeyRequestCount);

        identityProvider.BeginScenario(RNetIdentityProviderScenario.CrossOriginSigningKeys);
        RNetJwtAuthenticationResult crossOrigin =
            await identityProvider.RequestProtectedResourceAsync(validToken);
        Assert.Equal(HttpStatusCode.Unauthorized, crossOrigin.StatusCode);
        Assert.True(identityProvider.DiscoveryRequestCount >= 1);
        Assert.Equal(0, identityProvider.SigningKeyRequestCount);
        Assert.Equal(0, identityProvider.CrossOriginSinkRequestCount);

        identityProvider.BeginScenario(RNetIdentityProviderScenario.Valid);
        RNetJwtAuthenticationResult wrongIssuer =
            await identityProvider.RequestProtectedResourceAsync(
            identityProvider.CreateToken(
                "https://invalid-issuer.localhost",
                RNetLocalIdentityProvider.Audience));
        Assert.Equal(HttpStatusCode.Unauthorized, wrongIssuer.StatusCode);

        identityProvider.BeginScenario(RNetIdentityProviderScenario.Valid);
        RNetJwtAuthenticationResult wrongAudience =
            await identityProvider.RequestProtectedResourceAsync(
            identityProvider.CreateToken(identityProvider.Issuer, "invalid-audience"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrongAudience.StatusCode);

        identityProvider.BeginScenario(RNetIdentityProviderScenario.Valid);
        RNetJwtAuthenticationResult wrongKey =
            await identityProvider.RequestProtectedResourceAsync(
            identityProvider.CreateTokenWithUntrustedKey(
                identityProvider.Issuer,
                RNetLocalIdentityProvider.Audience));
        Assert.Equal(HttpStatusCode.Unauthorized, wrongKey.StatusCode);

        Assert.True(identityProvider.MaximumConcurrentRequestCount <= 1);
    }
}
