// Module purpose: Provides bounded loopback DNS, PKI, HTTPS and OIDC fixtures for the marker-gated R-NET homologation tests.
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using DBNotifier.Infrastructure.Http;
using DBNotifier.Infrastructure.Security;
using DBNotifier.Provider.Abstractions;
using DBNotifier.Server.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Owns the exact opt-in marker that keeps the local homologation campaign inert in normal test runs.</summary>
internal static class RNetLocalHomologationActivation
{
    private const string MarkerVariable = "DBNOTIFIER_RNET_LOCAL_HOMOLOGATION";
    private const string MarkerValue = "local-test";

    /// <summary>Checks for the exact non-secret marker without accepting aliases or peripheral whitespace.</summary>
    /// <returns><see langword="true"/> only for the explicitly authorised local campaign marker.</returns>
    internal static bool IsActive() =>
        string.Equals(
            Environment.GetEnvironmentVariable(MarkerVariable),
            MarkerValue,
            StringComparison.Ordinal);
}

/// <summary>Provides immutable validated paths to runner-owned synthetic certificate material.</summary>
internal sealed class RNetFixturePaths
{
    private const string FixtureRootVariable = "DBNOTIFIER_RNET_FIXTURE_ROOT";
    private const long MaximumPublicCertificateBytes = 128 * 1024;
    private const long MaximumPrivateKeyBytes = 128 * 1024;
    internal const string IdpHost = "rnet-idp.localhost";

    /// <summary>Initialises an already validated fixture path set.</summary>
    /// <param name="fixtureRoot">Canonical absolute runner-owned fixture directory.</param>
    private RNetFixturePaths(string fixtureRoot)
    {
        RootCertificate = RequiredFile(
            fixtureRoot,
            "root-ca.crt",
            MaximumPublicCertificateBytes);
        RootCertificateRevocationList = RequiredFile(
            fixtureRoot,
            "root-ca.crl",
            MaximumPublicCertificateBytes);
        MissingCrlRootCertificate = RequiredFile(
            fixtureRoot,
            "missing-crl-root-ca.crt",
            MaximumPublicCertificateBytes);
        ValidCertificate = RequiredFile(
            fixtureRoot,
            "idp-server.crt",
            MaximumPublicCertificateBytes);
        ValidPrivateKey = RequiredFile(
            fixtureRoot,
            "idp-server.key",
            MaximumPrivateKeyBytes);
        WrongNameCertificate = RequiredFile(
            fixtureRoot,
            "wrong-san.crt",
            MaximumPublicCertificateBytes);
        WrongNamePrivateKey = RequiredFile(
            fixtureRoot,
            "wrong-san.key",
            MaximumPrivateKeyBytes);
        WrongUsageCertificate = RequiredFile(
            fixtureRoot,
            "wrong-eku.crt",
            MaximumPublicCertificateBytes);
        WrongUsagePrivateKey = RequiredFile(
            fixtureRoot,
            "wrong-eku.key",
            MaximumPrivateKeyBytes);
        RevokedCertificate = RequiredFile(
            fixtureRoot,
            "revoked.crt",
            MaximumPublicCertificateBytes);
        RevokedPrivateKey = RequiredFile(
            fixtureRoot,
            "revoked.key",
            MaximumPrivateKeyBytes);
        UntrustedCertificate = RequiredFile(
            fixtureRoot,
            "untrusted.crt",
            MaximumPublicCertificateBytes);
        UntrustedPrivateKey = RequiredFile(
            fixtureRoot,
            "untrusted.key",
            MaximumPrivateKeyBytes);
        MissingCrlCertificate = RequiredFile(
            fixtureRoot,
            "missing-crl.crt",
            MaximumPublicCertificateBytes);
        MissingCrlPrivateKey = RequiredFile(
            fixtureRoot,
            "missing-crl.key",
            MaximumPrivateKeyBytes);
    }

    /// <summary>Gets the public trusted root certificate path.</summary>
    internal string RootCertificate { get; }

    /// <summary>Gets the public cached revocation-list evidence path.</summary>
    internal string RootCertificateRevocationList { get; }

    /// <summary>Gets the separately trusted root whose revocation material is deliberately absent.</summary>
    internal string MissingCrlRootCertificate { get; }

    /// <summary>Gets the trusted, non-revoked IdP server certificate path.</summary>
    internal string ValidCertificate { get; }

    /// <summary>Gets the private key consumed only by the valid IdP loopback listener.</summary>
    internal string ValidPrivateKey { get; }

    /// <summary>Gets the trusted certificate with a deliberately wrong DNS identity.</summary>
    internal string WrongNameCertificate { get; }

    /// <summary>Gets the private key consumed only by the wrong-name loopback listener.</summary>
    internal string WrongNamePrivateKey { get; }

    /// <summary>Gets the trusted certificate with a deliberately wrong extended-key use.</summary>
    internal string WrongUsageCertificate { get; }

    /// <summary>Gets the private key consumed only by the wrong-use loopback listener.</summary>
    internal string WrongUsagePrivateKey { get; }

    /// <summary>Gets the trusted certificate whose serial is present in the cached revocation list.</summary>
    internal string RevokedCertificate { get; }

    /// <summary>Gets the private key consumed only by the revoked-certificate loopback listener.</summary>
    internal string RevokedPrivateKey { get; }

    /// <summary>Gets the certificate whose issuing root is deliberately absent from system trust.</summary>
    internal string UntrustedCertificate { get; }

    /// <summary>Gets the private key consumed only by the untrusted-certificate loopback listener.</summary>
    internal string UntrustedPrivateKey { get; }

    /// <summary>Gets the certificate whose otherwise trusted issuer has no cached revocation evidence.</summary>
    internal string MissingCrlCertificate { get; }

    /// <summary>Gets the private key consumed only by the missing-CRL loopback listener.</summary>
    internal string MissingCrlPrivateKey { get; }

    /// <summary>Loads and validates the exact runner-owned fixture directory and expected bounded files.</summary>
    /// <returns>A canonical immutable path set.</returns>
    /// <exception cref="InvalidOperationException">Thrown with a stable code when any fixture boundary is absent.</exception>
    internal static RNetFixturePaths LoadRequired()
    {
        string? suppliedRoot = Environment.GetEnvironmentVariable(FixtureRootVariable);
        if (string.IsNullOrWhiteSpace(suppliedRoot) ||
            !Path.IsPathFullyQualified(suppliedRoot) ||
            !string.Equals(suppliedRoot, suppliedRoot.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("rnet.fixture_root_invalid");
        }

        string canonicalRoot;
        try
        {
            canonicalRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(suppliedRoot));
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException("rnet.fixture_root_invalid");
        }

        if (!Directory.Exists(canonicalRoot))
        {
            throw new InvalidOperationException("rnet.fixture_root_missing");
        }

        return new RNetFixturePaths(canonicalRoot);
    }

    /// <summary>Loads one public certificate without importing or retaining private key material.</summary>
    /// <param name="path">Previously validated public certificate path.</param>
    /// <returns>An independently disposable public certificate.</returns>
    /// <exception cref="InvalidOperationException">Thrown with a stable code when the material cannot be parsed.</exception>
    internal static X509Certificate2 LoadPublicCertificate(string path)
    {
        try
        {
            return X509CertificateLoader.LoadCertificateFromFile(path);
        }
        catch (Exception exception) when (
            exception is CryptographicException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException("rnet.public_certificate_invalid");
        }
    }

    /// <summary>Validates one expected filename beneath the canonical fixture root and enforces a byte ceiling.</summary>
    /// <param name="fixtureRoot">Canonical fixture root.</param>
    /// <param name="fileName">Exact trusted fixture filename.</param>
    /// <param name="maximumBytes">Maximum accepted file length.</param>
    /// <returns>The canonical contained file path.</returns>
    /// <exception cref="InvalidOperationException">Thrown with a stable code for missing or unbounded material.</exception>
    private static string RequiredFile(
        string fixtureRoot,
        string fileName,
        long maximumBytes)
    {
        string path = Path.GetFullPath(Path.Combine(fixtureRoot, fileName));
        string? parent = Path.GetDirectoryName(path);
        if (!string.Equals(parent, fixtureRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("rnet.fixture_path_invalid");
        }

        try
        {
            FileInfo information = new(path);
            if (!information.Exists || information.Length is < 1 || information.Length > maximumBytes)
            {
                throw new InvalidOperationException("rnet.fixture_file_invalid");
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new InvalidOperationException("rnet.fixture_file_invalid");
        }

        return path;
    }
}

/// <summary>Creates exact loopback policies and controlled resolver results for DNS boundary tests.</summary>
internal static class RNetNetworkFixtures
{
    /// <summary>Compiles one human-identity policy for an exact port and positive CIDR set.</summary>
    /// <param name="port">Exact fixture listener port.</param>
    /// <param name="allowedCidrs">Canonical loopback networks admitted by the test.</param>
    /// <returns>An immutable production policy set.</returns>
    internal static NetworkEgressPolicySet CreatePolicy(
        int port,
        params string[] allowedCidrs) =>
        CreatePolicy([port], allowedCidrs);

    /// <summary>Compiles one human-identity policy for exact ports and a positive CIDR set.</summary>
    /// <param name="ports">Exact fixture listener ports.</param>
    /// <param name="allowedCidrs">Canonical loopback networks admitted by the test.</param>
    /// <returns>An immutable production policy set.</returns>
    internal static NetworkEgressPolicySet CreatePolicy(
        IReadOnlyList<int> ports,
        params string[] allowedCidrs)
    {
        NetworkEgressOptions options = new();
        options.Policies[NetworkEgressPolicyIds.HumanIdentity] = new NetworkEgressPolicyOptions
        {
            AllowedCidrs = [.. allowedCidrs],
            AllowedPorts = [.. ports],
            DnsTimeoutSeconds = 2,
            MaximumResolvedAddresses = 8,
        };
        return NetworkEgressPolicySet.Compile(options);
    }
}

/// <summary>Returns a bounded sequence of controlled DNS answer sets and never queries a host resolver.</summary>
internal sealed class ControlledDnsResolver : IDnsResolver
{
    private readonly ConcurrentQueue<IReadOnlyList<IPAddress>> responses;
    private int callCount;

    /// <summary>Initialises the exact response sequence consumed one admission at a time.</summary>
    /// <param name="responses">Non-empty answer sets supplied in deterministic order.</param>
    internal ControlledDnsResolver(params IReadOnlyList<IPAddress>[] responses)
    {
        if (responses.Length == 0)
        {
            throw new ArgumentException("At least one controlled DNS response is required.", nameof(responses));
        }
        this.responses = new ConcurrentQueue<IReadOnlyList<IPAddress>>(responses);
    }

    /// <summary>Gets the number of bounded resolution attempts.</summary>
    internal int CallCount => Volatile.Read(ref callCount);

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<IPAddress>> ResolveAsync(
        string host,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref callCount);
        if (!responses.TryDequeue(out IReadOnlyList<IPAddress>? response))
        {
            throw new InvalidOperationException("rnet.controlled_dns_exhausted");
        }
        return ValueTask.FromResult(response);
    }
}

/// <summary>Returns loopback for a bounded number of fresh OIDC sockets without depending on connection pooling.</summary>
internal sealed class BoundedLoopbackDnsResolver : IDnsResolver
{
    private const int MaximumCalls = 8;
    private int callCount;

    /// <summary>Gets the completed bounded resolution count.</summary>
    internal int CallCount => Volatile.Read(ref callCount);

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<IPAddress>> ResolveAsync(
        string host,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int current = Interlocked.Increment(ref callCount);
        if (current > MaximumCalls)
        {
            throw new InvalidOperationException("rnet.loopback_dns_limit_exceeded");
        }
        return ValueTask.FromResult<IReadOnlyList<IPAddress>>([IPAddress.Loopback]);
    }

    /// <summary>Proves that the owning request used DNS admission without exceeding its physical socket envelope.</summary>
    internal void AssertBoundedUse()
    {
        Assert.InRange(CallCount, 1, MaximumCalls);
    }
}

/// <summary>Records only policy-approved address values and returns inert streams without opening sockets.</summary>
internal sealed class RecordingNetworkStreamConnector : INetworkStreamConnector
{
    private readonly List<IPAddress> addresses = [];

    /// <summary>Gets the number of admitted connection attempts.</summary>
    internal int CallCount => addresses.Count;

    /// <summary>Gets defensive snapshots of every admitted address.</summary>
    internal IReadOnlyList<IPAddress> Addresses =>
        addresses.Select(address => new IPAddress(address.GetAddressBytes())).ToArray();

    /// <inheritdoc />
    public ValueTask<Stream> ConnectAsync(
        IPAddress address,
        int port,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        addresses.Add(new IPAddress(address.GetAddressBytes()));
        return ValueTask.FromResult<Stream>(new MemoryStream());
    }
}

/// <summary>Stops and disposes a local web fixture while attempting every cleanup action after a non-fatal failure.</summary>
internal static class RNetFixtureCleanup
{
    /// <summary>Attempts bounded stop and asynchronous disposal without abandoning the second action.</summary>
    /// <param name="application">Fixture application whose listener must be released.</param>
    /// <returns><see langword="true"/> only when both cleanup actions completed.</returns>
    internal static async ValueTask<bool> TryStopAndDisposeAsync(WebApplication application)
    {
        bool succeeded = true;
        try
        {
            using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(10));
            await application.StopAsync(deadline.Token);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            succeeded = false;
        }

        try
        {
            await application.DisposeAsync();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            succeeded = false;
        }
        return succeeded;
    }
}

/// <summary>Contains a chain result and its combined platform status flags without retaining certificate material.</summary>
/// <param name="Succeeded">Whether the complete product policy accepted the chain.</param>
/// <param name="StatusFlags">Combined flags returned by the local operating-system chain engine.</param>
/// <param name="EndsAtExpectedRoot">Whether the built chain terminates at the exact process-local trust anchor.</param>
internal sealed record RNetChainResult(
    bool Succeeded,
    X509ChainStatusFlags StatusFlags,
    bool EndsAtExpectedRoot)
{
    /// <summary>Checks whether at least one permitted platform flag is present.</summary>
    /// <param name="expected">Permitted flags for this fail-closed case.</param>
    /// <returns><see langword="true"/> when any expected flag is present.</returns>
    internal bool HasAny(params X509ChainStatusFlags[] expected) =>
        expected.Any(flag => StatusFlags.HasFlag(flag));
}

/// <summary>Owns one exact public root used only through process-local custom trust in the R-NET laboratory.</summary>
internal sealed class RNetCustomRootTrustPolicyFactory : IDisposable
{
    private X509Certificate2? root;

    /// <summary>Initialises the factory after validating one public certificate-authority fixture.</summary>
    /// <param name="root">Owned public certificate-authority anchor.</param>
    private RNetCustomRootTrustPolicyFactory(X509Certificate2 root)
    {
        this.root = root;
    }

    /// <summary>Loads one exact public root without adding it to an operating-system trust store.</summary>
    /// <param name="certificatePath">Validated runner-owned public root path.</param>
    /// <returns>A disposable policy factory owning only public certificate material.</returns>
    internal static RNetCustomRootTrustPolicyFactory Load(string certificatePath)
    {
        X509Certificate2 certificate =
            RNetFixturePaths.LoadPublicCertificate(certificatePath);
        try
        {
            Assert.False(certificate.HasPrivateKey);
            X509BasicConstraintsExtension constraints = Assert.Single(
                certificate.Extensions.OfType<X509BasicConstraintsExtension>());
            Assert.True(constraints.CertificateAuthority);
            return new RNetCustomRootTrustPolicyFactory(certificate);
        }
        catch
        {
            certificate.Dispose();
            throw;
        }
    }

    /// <summary>Creates one fresh offline server policy anchored only to the exact synthetic public root.</summary>
    /// <returns>A custom-root policy retaining every production revocation, download and EKU control.</returns>
    internal X509ChainPolicy CreateServerAuthentication()
    {
        X509Certificate2 certificate = root ??
            throw new ObjectDisposedException(nameof(RNetCustomRootTrustPolicyFactory));
        X509ChainPolicy policy =
            OfflineCertificateChainPolicy.CreateServerAuthentication();
        RNetPkiAssertions.AssertSystemOfflinePolicy(policy);
        policy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        policy.CustomTrustStore.Add(certificate);
        RNetPkiAssertions.AssertFixtureOfflinePolicy(policy, certificate);
        return policy;
    }

    /// <summary>Checks whether one chain element is the exact process-local anchor.</summary>
    /// <param name="certificate">Terminal chain element returned by the operating-system chain engine.</param>
    /// <returns><see langword="true"/> only for a byte-for-byte public-root match.</returns>
    internal bool IsExpectedRoot(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        X509Certificate2 expected = root ??
            throw new ObjectDisposedException(nameof(RNetCustomRootTrustPolicyFactory));
        return certificate.RawDataMemory.Span.SequenceEqual(expected.RawDataMemory.Span);
    }

    /// <summary>Releases the process-local public anchor without changing any operating-system store.</summary>
    public void Dispose()
    {
        X509Certificate2? certificate = Interlocked.Exchange(ref root, null);
        certificate?.Dispose();
    }
}

/// <summary>Runs custom-root chain checks and real loopback TLS handshakes through the production HTTP boundary.</summary>
internal static class RNetPkiAssertions
{
    /// <summary>Checks whether the exact synthetic public root is absent from the current user's root store.</summary>
    /// <param name="certificate">Synthetic public root that must remain process-local.</param>
    /// <returns><see langword="true"/> only when no exact raw-certificate match exists.</returns>
    internal static bool IsAbsentFromCurrentUserRoot(X509Certificate2 certificate)
    {
        using X509Store store = new(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
        X509Certificate2Collection matches = store.Certificates.Find(
            X509FindType.FindByThumbprint,
            certificate.Thumbprint,
            validOnly: false);
        try
        {
            return matches.All(candidate =>
                !candidate.HasPrivateKey &&
                !candidate.RawDataMemory.Span.SequenceEqual(certificate.RawDataMemory.Span));
        }
        finally
        {
            foreach (X509Certificate2 candidate in matches)
            {
                candidate.Dispose();
            }
        }
    }

    /// <summary>Builds a chain with the production controls and one process-local custom trust anchor.</summary>
    /// <param name="certificate">Synthetic runner-owned server leaf.</param>
    /// <param name="trustPolicyFactory">Exact process-local public-root owner.</param>
    /// <returns>The accepted state and combined platform chain flags.</returns>
    internal static RNetChainResult BuildOfflineServerChain(
        X509Certificate2 certificate,
        RNetCustomRootTrustPolicyFactory trustPolicyFactory)
    {
        using X509Chain chain = new();
        chain.ChainPolicy = trustPolicyFactory.CreateServerAuthentication();
        bool succeeded = chain.Build(certificate);
        X509ChainStatusFlags flags = chain.ChainStatus.Aggregate(
            X509ChainStatusFlags.NoError,
            static (current, status) => current | status.Status);
        bool endsAtExpectedRoot =
            chain.ChainElements.Count > 0 &&
            trustPolicyFactory.IsExpectedRoot(
                chain.ChainElements[^1].Certificate);
        return new RNetChainResult(succeeded, flags, endsAtExpectedRoot);
    }

    /// <summary>Runs one loopback HTTPS request expected to pass name, use, trust and offline revocation checks.</summary>
    /// <param name="certificatePath">Validated public server certificate path.</param>
    /// <param name="privateKeyPath">Validated private key path consumed only by Kestrel.</param>
    /// <param name="trustPolicyFactory">Exact process-local public-root owner.</param>
    /// <returns>A task completing after the local response and policy evidence are verified.</returns>
    internal static async Task AssertHttpsAcceptedAsync(
        string certificatePath,
        string privateKeyPath,
        RNetCustomRootTrustPolicyFactory trustPolicyFactory)
    {
        await using RNetLoopbackHttpsServer server = await RNetLoopbackHttpsServer.StartAsync(
            certificatePath,
            privateKeyPath,
            RNetFixturePaths.IdpHost);
        using SocketsHttpHandler handler = CreateHandler(
            server.Port,
            trustPolicyFactory);
        Assert.NotNull(handler.SslOptions.CertificateChainPolicy);
        using HttpClient client = new(handler)
        {
            Timeout = TimeSpan.FromSeconds(10),
        };

        using HttpResponseMessage response = await client.GetAsync(server.Address);
        string payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("rnet.local.accepted", payload);
        Assert.Equal(1, server.RequestCount);
    }

    /// <summary>Runs one loopback HTTPS request expected to fail before an HTTP request reaches the fixture.</summary>
    /// <param name="certificatePath">Validated negative server certificate path.</param>
    /// <param name="privateKeyPath">Validated private key path consumed only by Kestrel.</param>
    /// <param name="trustPolicyFactory">Exact process-local public-root owner.</param>
    /// <returns>A task completing after fail-closed TLS evidence is verified.</returns>
    internal static async Task AssertHttpsRejectedAsync(
        string certificatePath,
        string privateKeyPath,
        RNetCustomRootTrustPolicyFactory trustPolicyFactory)
    {
        await using RNetLoopbackHttpsServer server = await RNetLoopbackHttpsServer.StartAsync(
            certificatePath,
            privateKeyPath,
            RNetFixturePaths.IdpHost);
        using SocketsHttpHandler handler = CreateHandler(
            server.Port,
            trustPolicyFactory);
        Assert.NotNull(handler.SslOptions.CertificateChainPolicy);
        using HttpClient client = new(handler)
        {
            Timeout = TimeSpan.FromSeconds(10),
        };

        HttpRequestException exception = await Xunit.Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetAsync(server.Address));

        Assert.DoesNotContain(".key", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, server.RequestCount);
    }

    /// <summary>Proves that Kestrel itself refuses a certificate whose EKU excludes server authentication.</summary>
    /// <param name="certificatePath">Validated wrong-use server certificate path.</param>
    /// <param name="privateKeyPath">Validated private key path consumed only by the attempted listener.</param>
    /// <returns>A task completing after the listener fails before binding.</returns>
    internal static async Task AssertHttpsServerCertificateRejectedAsync(
        string certificatePath,
        string privateKeyPath)
    {
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => RNetLoopbackHttpsServer.StartAsync(
                certificatePath,
                privateKeyPath,
                RNetFixturePaths.IdpHost));

        Assert.Contains(
            "1.3.6.1.5.5.7.3.1",
            exception.Message,
            StringComparison.Ordinal);
        Assert.DoesNotContain(".key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Creates a production handler whose DNS answer and physical socket remain loopback-only.</summary>
    /// <param name="port">Exact ephemeral Kestrel port.</param>
    /// <param name="trustPolicyFactory">Exact process-local public-root owner.</param>
    /// <returns>A policy-bound handler owned by the caller.</returns>
    private static SocketsHttpHandler CreateHandler(
        int port,
        RNetCustomRootTrustPolicyFactory trustPolicyFactory)
    {
        NetworkEgressPolicySet policies = RNetNetworkFixtures.CreatePolicy(port, "127.0.0.0/8");
        ControlledDnsResolver resolver = new([IPAddress.Loopback]);
        NetworkEgressAuthorizer authorizer = new(policies, resolver);
        NetworkBoundHttpMessageHandlerFactory factory = new(
            authorizer,
            new SystemNetworkStreamConnector(),
            trustPolicyFactory.CreateServerAuthentication);
        return factory.Create(NetworkEgressPolicyIds.HumanIdentity);
    }

    /// <summary>Checks the complete production no-download offline policy with operating-system trust.</summary>
    /// <param name="policy">Policy created by the production boundary.</param>
    internal static void AssertSystemOfflinePolicy(X509ChainPolicy? policy)
    {
        Assert.NotNull(policy);
        Assert.True(policy.DisableCertificateDownloads);
        Assert.Equal(X509RevocationMode.Offline, policy.RevocationMode);
        Assert.Equal(X509RevocationFlag.EntireChain, policy.RevocationFlag);
        Assert.Equal(X509VerificationFlags.NoFlag, policy.VerificationFlags);
        Assert.Equal(X509ChainTrustMode.System, policy.TrustMode);
        Assert.Empty(policy.CustomTrustStore);
        Assert.Contains(
            policy.ApplicationPolicy.Cast<Oid>(),
            usage => usage.Value == "1.3.6.1.5.5.7.3.1");
    }

    /// <summary>Checks that local trust changes only the anchor and retains every production TLS control.</summary>
    /// <param name="policy">Process-local test policy.</param>
    /// <param name="expectedRoot">Exact public root permitted by this fixture.</param>
    internal static void AssertFixtureOfflinePolicy(
        X509ChainPolicy? policy,
        X509Certificate2 expectedRoot)
    {
        Assert.NotNull(policy);
        Assert.True(policy.DisableCertificateDownloads);
        Assert.Equal(X509RevocationMode.Offline, policy.RevocationMode);
        Assert.Equal(X509RevocationFlag.EntireChain, policy.RevocationFlag);
        Assert.Equal(X509VerificationFlags.NoFlag, policy.VerificationFlags);
        Assert.Equal(X509ChainTrustMode.CustomRootTrust, policy.TrustMode);
        X509Certificate2 actualRoot = Assert.Single(policy.CustomTrustStore);
        Assert.False(actualRoot.HasPrivateKey);
        Assert.True(
            actualRoot.RawDataMemory.Span.SequenceEqual(
                expectedRoot.RawDataMemory.Span));
        Assert.Contains(
            policy.ApplicationPolicy.Cast<Oid>(),
            usage => usage.Value == "1.3.6.1.5.5.7.3.1");
    }
}

/// <summary>Normalises one synthetic PEM pair into a disposable Windows key context suitable for local Kestrel.</summary>
internal static class RNetServerCertificateLoader
{
    /// <summary>Loads, normalises and detaches one PEM pair without retaining exported PKCS#12 bytes.</summary>
    /// <param name="certificatePath">Validated public certificate path.</param>
    /// <param name="privateKeyPath">Validated private key path consumed only during import.</param>
    /// <returns>An independently disposable certificate suitable for Kestrel.</returns>
    /// <exception cref="InvalidOperationException">Thrown with a stable code when the pair cannot be imported.</exception>
    internal static X509Certificate2 Load(
        string certificatePath,
        string privateKeyPath)
    {
        byte[]? pkcs12 = null;
        byte[]? passwordBytes = null;
        try
        {
            using X509Certificate2 pemCertificate =
                X509Certificate2.CreateFromPemFile(certificatePath, privateKeyPath);
            if (!pemCertificate.HasPrivateKey)
            {
                throw new CryptographicException("rnet.fixture_private_key_missing");
            }

            passwordBytes = RandomNumberGenerator.GetBytes(24);
            string password = Convert.ToHexString(passwordBytes);
            pkcs12 = pemCertificate.Export(X509ContentType.Pkcs12, password);
            X509Certificate2 imported = X509CertificateLoader.LoadPkcs12(
                pkcs12,
                password,
                X509KeyStorageFlags.UserKeySet |
                    X509KeyStorageFlags.Exportable);
            if (!imported.HasPrivateKey)
            {
                imported.Dispose();
                throw new CryptographicException("rnet.fixture_private_key_missing");
            }
            return imported;
        }
        catch (Exception exception) when (
            exception is CryptographicException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException("rnet.server_certificate_invalid");
        }
        finally
        {
            if (pkcs12 is not null)
            {
                CryptographicOperations.ZeroMemory(pkcs12);
            }
            if (passwordBytes is not null)
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }
    }
}

/// <summary>Hosts one certificate on one loopback-only Kestrel listener and counts completed HTTP requests.</summary>
internal sealed class RNetLoopbackHttpsServer : IAsyncDisposable
{
    private readonly WebApplication application;
    private readonly X509Certificate2 certificate;
    private int requestCount;

    /// <summary>Initialises one started listener and its owned certificate.</summary>
    /// <param name="application">Started loopback-only application.</param>
    /// <param name="certificate">Certificate whose private key is used only by Kestrel.</param>
    /// <param name="address">TLS URI retaining the expected certificate hostname.</param>
    private RNetLoopbackHttpsServer(
        WebApplication application,
        X509Certificate2 certificate,
        Uri address)
    {
        this.application = application;
        this.certificate = certificate;
        Address = address;
    }

    /// <summary>Gets the TLS URI whose hostname is validated by the client.</summary>
    internal Uri Address { get; }

    /// <summary>Gets the exact loopback listener port.</summary>
    internal int Port => Address.Port;

    /// <summary>Gets the number of HTTP requests that passed the TLS boundary.</summary>
    internal int RequestCount => Volatile.Read(ref requestCount);

    /// <summary>Starts one HTTPS listener on an ephemeral loopback port using the supplied PEM pair.</summary>
    /// <param name="certificatePath">Validated public certificate path.</param>
    /// <param name="privateKeyPath">Validated private key path consumed only by Kestrel.</param>
    /// <param name="tlsHost">Exact hostname retained for SNI and certificate-name validation.</param>
    /// <returns>A started bounded listener.</returns>
    /// <exception cref="InvalidOperationException">Thrown with a stable code for invalid material or listener state.</exception>
    internal static async Task<RNetLoopbackHttpsServer> StartAsync(
        string certificatePath,
        string privateKeyPath,
        string tlsHost)
    {
        X509Certificate2 certificate = RNetServerCertificateLoader.Load(
            certificatePath,
            privateKeyPath);
        WebApplication? application = null;
        try
        {
            WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(
                new WebApplicationOptions
                {
                    EnvironmentName = Environments.Production,
                });
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options =>
                options.Listen(
                    IPAddress.Loopback,
                    0,
                    listener => listener.UseHttps(new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = certificate,
                    })));
            application = builder.Build();
            RNetLoopbackHttpsServer? fixture = null;
            application.MapGet(
                "/",
                () =>
                {
                    Interlocked.Increment(ref fixture!.requestCount);
                    return Results.Text("rnet.local.accepted", "text/plain");
                });
            await application.StartAsync();
            Uri listenAddress = ReadSingleLoopbackAddress(application);
            Uri publicAddress = new UriBuilder(
                Uri.UriSchemeHttps,
                tlsHost,
                listenAddress.Port).Uri;
            fixture = new RNetLoopbackHttpsServer(application, certificate, publicAddress);
            return fixture;
        }
        catch
        {
            bool cleaned = application is null ||
                await RNetFixtureCleanup.TryStopAndDisposeAsync(application);
            certificate.Dispose();
            if (!cleaned)
            {
                throw new InvalidOperationException("rnet.https_listener_cleanup_failed");
            }
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        bool cleaned = await RNetFixtureCleanup.TryStopAndDisposeAsync(application);
        certificate.Dispose();
        if (!cleaned)
        {
            throw new InvalidOperationException("rnet.https_listener_cleanup_failed");
        }
    }

    /// <summary>Reads and validates the sole started Kestrel address without retaining its literal host text.</summary>
    /// <param name="application">Started fixture application.</param>
    /// <returns>The loopback HTTPS listener URI.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the listener shape is not exact.</exception>
    private static Uri ReadSingleLoopbackAddress(WebApplication application)
    {
        IServer server = application.Services.GetRequiredService<IServer>();
        string[] addresses = server.Features
            .Get<IServerAddressesFeature>()?
            .Addresses
            .ToArray() ?? [];
        if (addresses.Length != 1 ||
            !Uri.TryCreate(addresses[0], UriKind.Absolute, out Uri? address) ||
            address.Scheme != Uri.UriSchemeHttps ||
            !IPAddress.TryParse(address.Host, out IPAddress? parsed) ||
            !IPAddress.IsLoopback(parsed))
        {
            throw new InvalidOperationException("rnet.loopback_listener_invalid");
        }
        return address;
    }
}

/// <summary>Names the exact IdP response modes used to exercise the production OIDC boundary.</summary>
internal enum RNetIdentityProviderScenario
{
    Valid = 0,
    Redirect = 1,
    CrossOriginSigningKeys = 2,
}

/// <summary>Captures the observable HTTP result of one protected local relying-party request.</summary>
/// <param name="StatusCode">Status returned after JwtBearer authentication and authorisation.</param>
/// <param name="Subject">Authenticated subject returned only for an accepted request.</param>
internal sealed record RNetJwtAuthenticationResult(
    HttpStatusCode StatusCode,
    string? Subject);

/// <summary>
/// Hosts one ephemeral loopback relying party so the test observes real JwtBearer middleware status codes.
/// </summary>
internal sealed class RNetLocalJwtConsumer : IAsyncDisposable
{
    private readonly WebApplication application;
    private readonly JwtBearerOptions jwtOptions;
    private readonly BoundedLoopbackDnsResolver resolver;
    private readonly Uri address;

    /// <summary>Initialises one started relying party and its configured OIDC backchannel.</summary>
    /// <param name="application">Started loopback-only HTTP application.</param>
    /// <param name="jwtOptions">Configured options whose network handler is disposed with the fixture.</param>
    /// <param name="resolver">Bounded repeatable loopback resolver used by fresh OIDC sockets.</param>
    /// <param name="address">Exact loopback protected-resource address.</param>
    private RNetLocalJwtConsumer(
        WebApplication application,
        JwtBearerOptions jwtOptions,
        BoundedLoopbackDnsResolver resolver,
        Uri address)
    {
        this.application = application;
        this.jwtOptions = jwtOptions;
        this.resolver = resolver;
        this.address = address;
    }

    /// <summary>Starts a fresh relying party with the production human OIDC configuration and metadata cache.</summary>
    /// <param name="issuer">Canonical local HTTPS IdP authority.</param>
    /// <param name="crossOriginSinkPort">Second admitted port that remains blocked by the IdP origin wrapper.</param>
    /// <param name="trustPolicyFactory">Exact process-local public-root owner for the synthetic IdP.</param>
    /// <returns>A started loopback-only consumer.</returns>
    internal static async Task<RNetLocalJwtConsumer> StartAsync(
        string issuer,
        int crossOriginSinkPort,
        RNetCustomRootTrustPolicyFactory trustPolicyFactory)
    {
        Uri authority = new(issuer);
        NetworkEgressPolicySet policies = RNetNetworkFixtures.CreatePolicy(
            [authority.Port, crossOriginSinkPort],
            "127.0.0.0/8");
        BoundedLoopbackDnsResolver resolver = new();
        NetworkEgressAuthorizer authorizer = new(policies, resolver);
        NetworkBoundHttpMessageHandlerFactory handlerFactory = new(
            authorizer,
            new SystemNetworkStreamConnector(),
            trustPolicyFactory.CreateServerAuthentication);
        Dictionary<string, string?> settings = new(StringComparer.Ordinal)
        {
            ["HumanAuthentication:Authority"] = issuer,
            ["HumanAuthentication:Audience"] = RNetLocalIdentityProvider.Audience,
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(
            new WebApplicationOptions
            {
                EnvironmentName = Environments.Production,
            });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options =>
            options.Listen(IPAddress.Loopback, 0));
        JwtBearerOptions? capturedOptions = null;
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = HumanAuthenticationDefaults.Scheme;
                options.DefaultChallengeScheme = HumanAuthenticationDefaults.Scheme;
            })
            .AddJwtBearer(HumanAuthenticationDefaults.Scheme, options =>
            {
                capturedOptions = options;
                bool configured = HumanOidcNetworkSecurity.Configure(
                    options,
                    configuration,
                    policies,
                    handlerFactory);
                if (!configured)
                {
                    throw new InvalidOperationException("rnet.identity_configuration_absent");
                }
            });
        builder.Services.AddAuthorization();
        WebApplication application = builder.Build();
        application.UseAuthentication();
        application.UseAuthorization();
        application.MapGet(
                "/protected",
                (HttpContext context) =>
                    Results.Text(context.User.FindFirst("sub")?.Value ?? string.Empty, "text/plain"))
            .RequireAuthorization();

        try
        {
            await application.StartAsync();
            Uri listenAddress = ReadSingleLoopbackAddress(application);
            IOptionsMonitor<JwtBearerOptions> options =
                application.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            JwtBearerOptions configuredOptions = options.Get(HumanAuthenticationDefaults.Scheme);
            return new RNetLocalJwtConsumer(
                application,
                configuredOptions,
                resolver,
                new Uri(listenAddress, "/protected"));
        }
        catch
        {
            bool cleaned = await RNetFixtureCleanup.TryStopAndDisposeAsync(application);
            try
            {
                capturedOptions?.BackchannelHttpHandler?.Dispose();
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                cleaned = false;
            }
            if (!cleaned)
            {
                throw new InvalidOperationException("rnet.jwt_consumer_cleanup_failed");
            }
            throw;
        }
    }

    /// <summary>Sends one in-memory bearer value to the local protected resource and records only status and subject.</summary>
    /// <param name="token">Compact synthetic JWT retained only in the request header.</param>
    /// <returns>The observable middleware status and accepted subject, if any.</returns>
    internal async Task<RNetJwtAuthenticationResult> RequestAsync(string token)
    {
        using SocketsHttpHandler handler = new()
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            Credentials = null,
            MaxConnectionsPerServer = 1,
        };
        using HttpClient client = new(handler)
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
        using HttpRequestMessage request = new(HttpMethod.Get, address);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage response = await client.SendAsync(request);
        string? subject = response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadAsStringAsync()
            : null;
        resolver.AssertBoundedUse();
        return new RNetJwtAuthenticationResult(response.StatusCode, subject);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        bool cleaned = await RNetFixtureCleanup.TryStopAndDisposeAsync(application);
        try
        {
            jwtOptions.BackchannelHttpHandler?.Dispose();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            cleaned = false;
        }
        if (!cleaned)
        {
            throw new InvalidOperationException("rnet.jwt_consumer_cleanup_failed");
        }
    }

    /// <summary>Reads and validates the sole started unencrypted loopback listener used only by this test fixture.</summary>
    /// <param name="application">Started fixture application.</param>
    /// <returns>The loopback HTTP listener URI.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the listener shape is not exact.</exception>
    private static Uri ReadSingleLoopbackAddress(WebApplication application)
    {
        IServer server = application.Services.GetRequiredService<IServer>();
        string[] addresses = server.Features
            .Get<IServerAddressesFeature>()?
            .Addresses
            .ToArray() ?? [];
        if (addresses.Length != 1 ||
            !Uri.TryCreate(addresses[0], UriKind.Absolute, out Uri? address) ||
            address.Scheme != Uri.UriSchemeHttp ||
            !IPAddress.TryParse(address.Host, out IPAddress? parsed) ||
            !IPAddress.IsLoopback(parsed))
        {
            throw new InvalidOperationException("rnet.jwt_consumer_listener_invalid");
        }
        return address;
    }
}

/// <summary>
/// Hosts one loopback HTTPS IdP with an ephemeral signing key and invokes the production JWT bearer configuration.
/// </summary>
internal sealed class RNetLocalIdentityProvider : IAsyncDisposable
{
    internal const string Audience = "db-notifier-rnet-local";
    internal const string Subject = "rnet-local-operator";
    private const string SigningAlgorithm = SecurityAlgorithms.RsaSha256;
    private readonly WebApplication application;
    private readonly X509Certificate2 certificate;
    private readonly RNetLoopbackHttpsServer crossOriginSink;
    private readonly RNetCustomRootTrustPolicyFactory trustPolicyFactory;
    private readonly RSA signingKey;
    private readonly string keyId;
    private int activeRequests;
    private int discoveryRequestCount;
    private int maximumConcurrentRequestCount;
    private int redirectTargetRequestCount;
    private int scenario;
    private int signingKeyRequestCount;

    /// <summary>Initialises one started IdP and its synthetic cryptographic material.</summary>
    /// <param name="application">Started loopback-only HTTPS application.</param>
    /// <param name="certificate">Transport certificate whose key is owned by Kestrel.</param>
    /// <param name="crossOriginSink">Reachable allowed-port sink that must never receive a cross-origin JWKS request.</param>
    /// <param name="trustPolicyFactory">Borrowed process-local public-root owner retained by the calling test.</param>
    /// <param name="signingKey">Ephemeral JWT signing key retained only in memory.</param>
    /// <param name="keyId">Bounded non-secret public signing-key identifier.</param>
    /// <param name="issuer">Canonical HTTPS issuer retaining the expected TLS hostname.</param>
    private RNetLocalIdentityProvider(
        WebApplication application,
        X509Certificate2 certificate,
        RNetLoopbackHttpsServer crossOriginSink,
        RNetCustomRootTrustPolicyFactory trustPolicyFactory,
        RSA signingKey,
        string keyId,
        string issuer)
    {
        this.application = application;
        this.certificate = certificate;
        this.crossOriginSink = crossOriginSink;
        this.trustPolicyFactory = trustPolicyFactory;
        this.signingKey = signingKey;
        this.keyId = keyId;
        Issuer = issuer;
    }

    /// <summary>Gets the canonical local HTTPS issuer.</summary>
    internal string Issuer { get; }

    /// <summary>Gets the completed discovery requests for the current scenario.</summary>
    internal int DiscoveryRequestCount => Volatile.Read(ref discoveryRequestCount);

    /// <summary>Gets signing-key requests that reached the admitted same-origin endpoint.</summary>
    internal int SigningKeyRequestCount => Volatile.Read(ref signingKeyRequestCount);

    /// <summary>Gets redirect-target requests; a compliant client always leaves this at zero.</summary>
    internal int RedirectTargetRequestCount => Volatile.Read(ref redirectTargetRequestCount);

    /// <summary>Gets requests reaching the separately admitted valid-TLS cross-origin sink.</summary>
    internal int CrossOriginSinkRequestCount => crossOriginSink.RequestCount;

    /// <summary>Gets the greatest concurrent IdP request count observed during the campaign.</summary>
    internal int MaximumConcurrentRequestCount => Volatile.Read(ref maximumConcurrentRequestCount);

    /// <summary>Starts one loopback-only HTTPS IdP using the runner-provided trusted PEM pair.</summary>
    /// <param name="certificatePath">Validated public transport certificate path.</param>
    /// <param name="privateKeyPath">Validated private key path consumed only by Kestrel.</param>
    /// <param name="trustPolicyFactory">Exact process-local public-root owner retained by the calling test.</param>
    /// <returns>A started local identity provider.</returns>
    internal static async Task<RNetLocalIdentityProvider> StartAsync(
        string certificatePath,
        string privateKeyPath,
        RNetCustomRootTrustPolicyFactory trustPolicyFactory)
    {
        RNetLoopbackHttpsServer crossOriginSink = await RNetLoopbackHttpsServer.StartAsync(
            certificatePath,
            privateKeyPath,
            RNetFixturePaths.IdpHost);
        X509Certificate2? certificate = null;
        RSA? signingKey = null;
        WebApplication? application = null;
        try
        {
            certificate = RNetServerCertificateLoader.Load(certificatePath, privateKeyPath);
            signingKey = RSA.Create(2048);
            string keyId = $"rnet-{Guid.NewGuid():N}";
            WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(
                new WebApplicationOptions
                {
                    EnvironmentName = Environments.Production,
                });
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options =>
                options.Listen(
                    IPAddress.Loopback,
                    0,
                    listener => listener.UseHttps(new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = certificate,
                    })));
            application = builder.Build();
            RNetLocalIdentityProvider? fixture = null;
            application.MapGet(
                "/.well-known/openid-configuration",
                (HttpContext context) => fixture!.WriteDiscoveryAsync(context));
            application.MapGet(
                "/keys",
                (HttpContext context) => fixture!.WriteSigningKeysAsync(context));
            application.MapGet(
                "/redirected-discovery",
                (HttpContext context) => fixture!.WriteUnexpectedRedirectTargetAsync(context));
            await application.StartAsync();
            Uri listenAddress = ReadSingleLoopbackAddress(application);
            string issuer = new UriBuilder(
                Uri.UriSchemeHttps,
                RNetFixturePaths.IdpHost,
                listenAddress.Port).Uri.AbsoluteUri.TrimEnd('/');
            fixture = new RNetLocalIdentityProvider(
                application,
                certificate,
                crossOriginSink,
                trustPolicyFactory,
                signingKey,
                keyId,
                issuer);
            return fixture;
        }
        catch
        {
            bool cleaned = application is null ||
                await RNetFixtureCleanup.TryStopAndDisposeAsync(application);
            try
            {
                await crossOriginSink.DisposeAsync();
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                cleaned = false;
            }
            certificate?.Dispose();
            signingKey?.Dispose();
            if (!cleaned)
            {
                throw new InvalidOperationException("rnet.identity_fixture_cleanup_failed");
            }
            throw;
        }
    }

    /// <summary>Resets bounded counters and selects one exact response mode after prior requests have completed.</summary>
    /// <param name="selected">Response mode for the next independent authentication provider.</param>
    internal void BeginScenario(RNetIdentityProviderScenario selected)
    {
        if (!Enum.IsDefined(selected) || Volatile.Read(ref activeRequests) != 0)
        {
            throw new InvalidOperationException("rnet.identity_scenario_invalid");
        }

        Interlocked.Exchange(ref discoveryRequestCount, 0);
        Interlocked.Exchange(ref signingKeyRequestCount, 0);
        Interlocked.Exchange(ref redirectTargetRequestCount, 0);
        Volatile.Write(ref scenario, (int)selected);
    }

    /// <summary>Creates one bounded valid-shape JWT using the fixture's advertised signing key.</summary>
    /// <param name="issuer">Issuer claim placed in the token.</param>
    /// <param name="audience">Audience claim placed in the token.</param>
    /// <returns>A compact signed JWT retained only by the caller.</returns>
    internal string CreateToken(string issuer, string audience) =>
        CreateToken(signingKey, keyId, issuer, audience);

    /// <summary>Creates a token whose public key is deliberately absent from the advertised JWKS.</summary>
    /// <param name="issuer">Issuer claim placed in the token.</param>
    /// <param name="audience">Audience claim placed in the token.</param>
    /// <returns>A compact token with an invalid signature for this IdP.</returns>
    internal string CreateTokenWithUntrustedKey(string issuer, string audience)
    {
        using RSA untrustedKey = RSA.Create(2048);
        return CreateToken(untrustedKey, keyId, issuer, audience);
    }

    /// <summary>Requests one protected resource through a fresh production JwtBearer metadata cache.</summary>
    /// <param name="token">Compact JWT supplied only through an in-memory request header.</param>
    /// <returns>The observable HTTP result after discovery, JWKS, authentication and authorisation.</returns>
    internal async Task<RNetJwtAuthenticationResult> RequestProtectedResourceAsync(string token)
    {
        await using RNetLocalJwtConsumer consumer = await RNetLocalJwtConsumer.StartAsync(
            Issuer,
            crossOriginSink.Port,
            trustPolicyFactory);
        return await consumer.RequestAsync(token);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        bool cleaned = await RNetFixtureCleanup.TryStopAndDisposeAsync(application);
        try
        {
            await crossOriginSink.DisposeAsync();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            cleaned = false;
        }
        signingKey.Dispose();
        certificate.Dispose();
        if (!cleaned)
        {
            throw new InvalidOperationException("rnet.identity_listener_cleanup_failed");
        }
    }

    /// <summary>Writes valid, redirecting or cross-origin discovery without exposing fixture paths.</summary>
    /// <param name="context">Current local HTTPS request.</param>
    /// <returns>A task completing after the bounded JSON or redirect response.</returns>
    private async Task WriteDiscoveryAsync(HttpContext context)
    {
        using RequestAdmission admission = EnterRequest();
        Interlocked.Increment(ref discoveryRequestCount);
        RNetIdentityProviderScenario selected =
            (RNetIdentityProviderScenario)Volatile.Read(ref scenario);
        if (selected == RNetIdentityProviderScenario.Redirect)
        {
            context.Response.StatusCode = StatusCodes.Status302Found;
            context.Response.Headers.Location = $"{Issuer}/redirected-discovery";
            return;
        }

        string keyEndpoint = selected == RNetIdentityProviderScenario.CrossOriginSigningKeys
            ? crossOriginSink.Address.AbsoluteUri
            : $"{Issuer}/keys";
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new
            {
                issuer = Issuer,
                jwks_uri = keyEndpoint,
                id_token_signing_alg_values_supported = new[] { SigningAlgorithm },
            },
            cancellationToken: context.RequestAborted);
    }

    /// <summary>Writes the single public RSA signing key advertised by the valid local IdP.</summary>
    /// <param name="context">Current local HTTPS request.</param>
    /// <returns>A task completing after the bounded public JWKS response.</returns>
    private async Task WriteSigningKeysAsync(HttpContext context)
    {
        using RequestAdmission admission = EnterRequest();
        Interlocked.Increment(ref signingKeyRequestCount);
        RSAParameters parameters = signingKey.ExportParameters(includePrivateParameters: false);
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/jwk-set+json";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new
            {
                keys = new[]
                {
                    new
                    {
                        kty = "RSA",
                        use = "sig",
                        kid = keyId,
                        alg = SigningAlgorithm,
                        n = Base64UrlEncoder.Encode(parameters.Modulus),
                        e = Base64UrlEncoder.Encode(parameters.Exponent),
                    },
                },
            },
            cancellationToken: context.RequestAborted);
    }

    /// <summary>Records any unexpected redirect follow so the test can prove it remained unreachable.</summary>
    /// <param name="context">Current local HTTPS request.</param>
    /// <returns>A completed task after the refusal response.</returns>
    private Task WriteUnexpectedRedirectTargetAsync(HttpContext context)
    {
        using RequestAdmission admission = EnterRequest();
        Interlocked.Increment(ref redirectTargetRequestCount);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return Task.CompletedTask;
    }

    /// <summary>Creates one compact JWT with bounded lifetime and an exact key identifier.</summary>
    /// <param name="key">RSA signing key retained only in memory.</param>
    /// <param name="kid">Public key identifier placed in the protected header.</param>
    /// <param name="issuer">Issuer claim.</param>
    /// <param name="audience">Audience claim.</param>
    /// <returns>A compact signed token.</returns>
    private static string CreateToken(
        RSA key,
        string kid,
        string issuer,
        string audience)
    {
        DateTime now = DateTime.UtcNow;
        RsaSecurityKey securityKey = new(key)
        {
            KeyId = kid,
        };
        JwtSecurityToken token = new(
            issuer,
            audience,
            [new Claim("sub", Subject), new Claim("name", "R-NET Local Operator")],
            now.AddMinutes(-1),
            now.AddMinutes(5),
            new SigningCredentials(securityKey, SigningAlgorithm));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Tracks bounded concurrent requests so overlapping metadata work cannot be mistaken for serial proof.</summary>
    /// <returns>A scope that decrements the active count exactly once.</returns>
    private RequestAdmission EnterRequest()
    {
        int active = Interlocked.Increment(ref activeRequests);
        int currentMaximum;
        do
        {
            currentMaximum = Volatile.Read(ref maximumConcurrentRequestCount);
            if (active <= currentMaximum)
            {
                break;
            }
        }
        while (Interlocked.CompareExchange(
            ref maximumConcurrentRequestCount,
            active,
            currentMaximum) != currentMaximum);
        return new RequestAdmission(this);
    }

    /// <summary>Reads and validates the sole started Kestrel address.</summary>
    /// <param name="application">Started fixture application.</param>
    /// <returns>The loopback HTTPS listener URI.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the listener shape is not exact.</exception>
    private static Uri ReadSingleLoopbackAddress(WebApplication application)
    {
        IServer server = application.Services.GetRequiredService<IServer>();
        string[] addresses = server.Features
            .Get<IServerAddressesFeature>()?
            .Addresses
            .ToArray() ?? [];
        if (addresses.Length != 1 ||
            !Uri.TryCreate(addresses[0], UriKind.Absolute, out Uri? address) ||
            address.Scheme != Uri.UriSchemeHttps ||
            !IPAddress.TryParse(address.Host, out IPAddress? parsed) ||
            !IPAddress.IsLoopback(parsed))
        {
            throw new InvalidOperationException("rnet.identity_listener_invalid");
        }
        return address;
    }

    /// <summary>Decrements one request admission exactly once.</summary>
    private sealed class RequestAdmission : IDisposable
    {
        private RNetLocalIdentityProvider? owner;

        /// <summary>Initialises one active request scope.</summary>
        /// <param name="owner">Fixture whose active count is decremented on disposal.</param>
        internal RequestAdmission(RNetLocalIdentityProvider owner)
        {
            this.owner = owner;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            RNetLocalIdentityProvider? current = Interlocked.Exchange(ref owner, null);
            if (current is not null)
            {
                Interlocked.Decrement(ref current.activeRequests);
            }
        }
    }
}
