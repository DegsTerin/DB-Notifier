// Module purpose: Proves the human-authorised Desktop Fleet read protocol over an isolated local HTTPS Server composition.
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using DBNotifier.Application.Access;
using DBNotifier.Application.Presentation;
using DBNotifier.Server.Api;
using DBNotifier.Server.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Exercises endpoint authentication, protocol negotiation and bounded provider-neutral response transport.</summary>
public sealed class DesktopFleetApiEndToEndTests
{
    [Fact]
    public async Task AuthenticatedHumanReadsVersionedProviderNeutralSnapshot()
    {
        await using DesktopFleetApiSandbox sandbox = await DesktopFleetApiSandbox.StartAsync();
        using HttpClient client = sandbox.CreateClient(authenticated: true);
        using HttpRequestMessage request = CreateRequest();

        using HttpResponseMessage response = await client.SendAsync(request);
        DesktopFleetApiSnapshot? snapshot = await response.Content.ReadFromJsonAsync<DesktopFleetApiSnapshot>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("1", Assert.Single(response.Headers.GetValues("DBN-Protocol-Version")));
        Assert.Equal(
            DesktopFleetApiContract.CurrentSchemaVersion,
            Assert.Single(response.Headers.GetValues("DBN-Message-Schema")));
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        DesktopFleetApiItem item = Assert.Single(Assert.IsType<DesktopFleetApiSnapshot>(snapshot).Items);
        Assert.Equal("provider.fixture", item.ProviderType);
        Assert.Equal("degraded", item.Status);
    }

    [Fact]
    public async Task SnapshotDeniesCallerWithoutHumanIdentity()
    {
        await using DesktopFleetApiSandbox sandbox = await DesktopFleetApiSandbox.StartAsync();
        using HttpClient client = sandbox.CreateClient(authenticated: false);
        using HttpRequestMessage request = CreateRequest();

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SnapshotRejectsUnsupportedMessageSchemaBeforeStoreRead()
    {
        await using DesktopFleetApiSandbox sandbox = await DesktopFleetApiSandbox.StartAsync();
        using HttpClient client = sandbox.CreateClient(authenticated: true);
        using HttpRequestMessage request = CreateRequest(schema: "desktop-fleet.v2");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.UpgradeRequired, response.StatusCode);
        Assert.Equal(0, sandbox.Store.ReadCount);
    }

    /// <summary>Creates one exact versioned Desktop Fleet GET request.</summary>
    private static HttpRequestMessage CreateRequest(string? schema = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, DesktopFleetApiContract.SnapshotRoute);
        request.Headers.TryAddWithoutValidation("DBN-Protocol-Version", "1");
        request.Headers.TryAddWithoutValidation(
            "DBN-Message-Schema",
            schema ?? DesktopFleetApiContract.CurrentSchemaVersion);
        return request;
    }

    /// <summary>Owns a temporary HTTPS endpoint, deterministic identity and in-memory application store.</summary>
    private sealed class DesktopFleetApiSandbox : IAsyncDisposable
    {
        private const string TestAuthenticationScheme = "DesktopFleetTestHuman";
        private const string TestSubjectHeader = "DBN-Test-Human-Subject";
        private static readonly DateTimeOffset Now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);
        private readonly WebApplication application;
        private readonly X509Certificate2 certificate;
        private readonly ECDsa certificateKey;

        private DesktopFleetApiSandbox(
            WebApplication application,
            X509Certificate2 certificate,
            ECDsa certificateKey,
            Uri baseAddress,
            RecordingStore store)
        {
            this.application = application;
            this.certificate = certificate;
            this.certificateKey = certificateKey;
            BaseAddress = baseAddress;
            Store = store;
        }

        public Uri BaseAddress { get; }

        public RecordingStore Store { get; }

        /// <summary>Starts one local test-only HTTPS composition with no provider, external identity or external service.</summary>
        public static async Task<DesktopFleetApiSandbox> StartAsync()
        {
            (X509Certificate2 certificate, ECDsa certificateKey) = CreateLoopbackCertificate();
            RecordingStore store = new(Now);
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(DesktopFleetEndpointRouteBuilderExtensions).Assembly.FullName,
                EnvironmentName = "DesktopFleetApiSandbox",
            });
            builder.WebHost.ConfigureKestrel(options =>
                options.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.UseHttps(certificate)));
            builder.Services.AddSingleton<IAuthorizedOperationsStore>(store);
            builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
            builder.Services.AddSingleton<AuthorizedOperationsService>();
            builder.Services.AddSingleton<HumanActorResolver>();
            builder.Services
                .AddAuthentication(TestAuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestHumanAuthenticationHandler>(
                    TestAuthenticationScheme,
                    _ => { });
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy(ApiSecurityDefaults.HumanApiPolicy, policy =>
                {
                    policy.AddAuthenticationSchemes(TestAuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("sub");
                });
            builder.Services.AddRateLimiter(options =>
                options.AddFixedWindowLimiter("HumanApiRateLimit", limiter =>
                {
                    limiter.PermitLimit = 20;
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.QueueLimit = 0;
                }));

            WebApplication application = builder.Build();
            application.UseRouting();
            application.UseAuthentication();
            application.UseRateLimiter();
            application.UseAuthorization();
            application.MapDesktopFleetEndpoint();
            try
            {
                await application.StartAsync();
                IServer server = application.Services.GetRequiredService<IServer>();
                Uri baseAddress = new(server.Features.Get<IServerAddressesFeature>()?.Addresses.Single() ??
                    throw new InvalidOperationException("The Desktop Fleet sandbox did not expose one loopback address."));
                return new DesktopFleetApiSandbox(application, certificate, certificateKey, baseAddress, store);
            }
            catch
            {
                await application.DisposeAsync();
                certificate.Dispose();
                certificateKey.Dispose();
                throw;
            }
        }

        /// <summary>Creates one certificate-pinned local client with optional test-only human identity.</summary>
        public HttpClient CreateClient(bool authenticated)
        {
            string expectedThumbprint = certificate.Thumbprint;
            HttpClientHandler handler = new()
            {
                ServerCertificateCustomValidationCallback = (_, presented, _, _) =>
                    string.Equals(presented?.Thumbprint, expectedThumbprint, StringComparison.OrdinalIgnoreCase),
            };
            HttpClient client = new(handler) { BaseAddress = BaseAddress };
            if (authenticated)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(TestSubjectHeader, "desktop-operator");
            }

            return client;
        }

        /// <summary>Stops the isolated endpoint and releases its temporary certificate resources.</summary>
        public async ValueTask DisposeAsync()
        {
            await application.StopAsync();
            await application.DisposeAsync();
            certificate.Dispose();
            certificateKey.Dispose();
        }

        /// <summary>Creates one short-lived local TLS certificate whose private key never leaves this test process.</summary>
        private static (X509Certificate2 Certificate, ECDsa Key) CreateLoopbackCertificate()
        {
            ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CertificateRequest request = new("CN=localhost", key, HashAlgorithmName.SHA256);
            SubjectAlternativeNameBuilder names = new();
            names.AddDnsName("localhost");
            names.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(names.Build());
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.1")],
                true));
            try
            {
                using X509Certificate2 ephemeral = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddMinutes(-1),
                    DateTimeOffset.UtcNow.AddMinutes(10));
                string password = Guid.NewGuid().ToString("N");
                byte[] pkcs12 = ephemeral.Export(X509ContentType.Pkcs12, password);
                try
                {
                    X509Certificate2 transport = X509CertificateLoader.LoadPkcs12(
                        pkcs12,
                        password,
                        X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
                    return (transport, key);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(pkcs12);
                }
            }
            catch
            {
                key.Dispose();
                throw;
            }
        }

        /// <summary>Authenticates only the exact test header inside the isolated local composition.</summary>
        private sealed class TestHumanAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            public TestHumanAuthenticationHandler(
                IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger,
                UrlEncoder encoder)
                : base(options, logger, encoder)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                if (!Request.Headers.TryGetValue(TestSubjectHeader, out var values) ||
                    values.Count != 1 || values[0] != "desktop-operator")
                {
                    return Task.FromResult(AuthenticateResult.NoResult());
                }

                ClaimsIdentity identity = new([new Claim("sub", "desktop-operator")], Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
            }
        }
    }

    /// <summary>Returns one deterministic authorised snapshot and records whether the endpoint reached application work.</summary>
    private sealed class RecordingStore(DateTimeOffset now) : IAuthorizedOperationsStore
    {
        public int ReadCount { get; private set; }

        public ValueTask<DesktopFleetApiSnapshot> GetAuthorizedDesktopFleetSnapshotAsync(
            string subjectId,
            string permissionCode,
            DateTimeOffset generatedAt,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            return ValueTask.FromResult(new DesktopFleetApiSnapshot(
                DesktopFleetApiContract.CurrentSchemaVersion,
                generatedAt,
                [new DesktopFleetApiItem(
                    Guid.Parse("00000000-0000-0000-0000-000000000301"),
                    "Orders",
                    "provider.fixture",
                    "Provider-readiness evidence",
                    "production",
                    "Remote Agent",
                    "degraded",
                    now.AddSeconds(-2),
                    now.AddSeconds(-1),
                    125,
                    true)]));
        }

        public ValueTask<IReadOnlyList<AuthorizedInstance>> GetAuthorizedInstancesAsync(
            string subjectId,
            string permissionCode,
            DateTimeOffset generatedAt,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<AuthorizedInstance>>([]);

        public ValueTask<CommandCreationResult> CreateCommandAsync(
            string subjectId,
            Guid instanceId,
            string permissionCode,
            ValidatedCommandRequest request,
            DateTimeOffset generatedAt,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new CommandCreationResult(CommandCreationDisposition.Denied, null));

        public ValueTask AuditCommandRejectionAsync(
            string subjectId,
            Guid instanceId,
            string errorCode,
            DateTimeOffset generatedAt,
            CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public ValueTask<AuthorizedAuditPage> QueryAuditAsync(
            string subjectId,
            string permissionCode,
            AuditQuery query,
            DateTimeOffset generatedAt,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new AuthorizedAuditPage(false, [], null, generatedAt));
    }

    /// <summary>Provides the deterministic UTC projection instant to the local API composition.</summary>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
