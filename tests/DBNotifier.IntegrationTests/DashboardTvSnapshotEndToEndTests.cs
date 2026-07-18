// Module purpose: Proves the authenticated Dashboard TV snapshot protocol over a temporary HTTPS loopback sandbox.
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Application.Presentation;
using DBNotifier.Server.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DBNotifier.IntegrationTests;

public sealed class DashboardTvSnapshotEndToEndTests
{
    [Fact]
    public async Task AuthenticatedSnapshotSupportsStrongEtagAndNotModified()
    {
        await using DashboardTvSandbox sandbox = await DashboardTvSandbox.StartAsync();
        using HttpClient client = sandbox.CreateClient(authenticated: true);

        using HttpResponseMessage first = await client.GetAsync(
            DashboardTvSandboxEndpointRouteBuilderExtensions.SnapshotRoute);
        string body = await first.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(DashboardTvSnapshotContract.CurrentSchemaVersion, first.Headers.GetValues("DBN-Snapshot-Schema").Single());
        Assert.Matches("^\\\"sha256-[0-9a-f]{64}\\\"$", first.Headers.ETag?.ToString());
        Assert.Contains("\"schemaVersion\":\"dashboard-tv.v1\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);

        using HttpRequestMessage conditional = new(HttpMethod.Get, DashboardTvSandboxEndpointRouteBuilderExtensions.SnapshotRoute);
        conditional.Headers.IfNoneMatch.Add(first.Headers.ETag!);
        using HttpResponseMessage second = await client.SendAsync(conditional);

        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
        Assert.Equal(first.Headers.ETag, second.Headers.ETag);
        Assert.Empty(await second.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task SnapshotDeniesAnUnauthenticatedCaller()
    {
        await using DashboardTvSandbox sandbox = await DashboardTvSandbox.StartAsync();
        using HttpClient client = sandbox.CreateClient(authenticated: false);

        using HttpResponseMessage response = await client.GetAsync(
            DashboardTvSandboxEndpointRouteBuilderExtensions.SnapshotRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SnapshotRejectsAMalformedConditionalTag()
    {
        await using DashboardTvSandbox sandbox = await DashboardTvSandbox.StartAsync();
        using HttpClient client = sandbox.CreateClient(authenticated: true);
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            DashboardTvSandboxEndpointRouteBuilderExtensions.SnapshotRoute);
        request.Headers.TryAddWithoutValidation("If-None-Match", "W/\"weak-sandbox-tag\"");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("dashboard_tv.etag_invalid", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private sealed class DashboardTvSandbox : IAsyncDisposable
    {
        private readonly WebApplication application;
        private readonly X509Certificate2 certificate;
        private readonly ECDsa certificateKey;

        private DashboardTvSandbox(
            WebApplication application,
            X509Certificate2 certificate,
            ECDsa certificateKey,
            Uri baseAddress)
        {
            this.application = application;
            this.certificate = certificate;
            this.certificateKey = certificateKey;
            BaseAddress = baseAddress;
        }

        public Uri BaseAddress { get; }

        public static async Task<DashboardTvSandbox> StartAsync()
        {
            (X509Certificate2 certificate, ECDsa certificateKey) = CreateLoopbackCertificate();
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(DashboardTvSandboxEndpointRouteBuilderExtensions).Assembly.FullName,
                EnvironmentName = DashboardTvSandboxEndpointRouteBuilderExtensions.EnvironmentName,
            });
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DashboardTvSandboxEndpointRouteBuilderExtensions.ConfigurationSection}:Enabled"] = bool.TrueString,
            });
            builder.WebHost.ConfigureKestrel(options =>
                options.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.UseHttps(certificate)));
            builder.Services.AddSingleton(TimeProvider.System);
            bool enabled = builder.Services.AddDashboardTvSandbox(builder.Environment, builder.Configuration);
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
            application.MapDashboardTvSandboxEndpoint(enabled);
            try
            {
                await application.StartAsync();
                IServer server = application.Services.GetRequiredService<IServer>();
                Uri baseAddress = new(server.Features.Get<IServerAddressesFeature>()?.Addresses.Single() ??
                    throw new InvalidOperationException("The local sandbox did not expose one loopback address."));
                return new DashboardTvSandbox(application, certificate, certificateKey, baseAddress);
            }
            catch
            {
                await application.DisposeAsync();
                certificate.Dispose();
                certificateKey.Dispose();
                throw;
            }
        }

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
                client.DefaultRequestHeaders.Add(
                    DashboardTvSandboxEndpointRouteBuilderExtensions.TestSubjectHeader,
                    "local-tv-reviewer");
            }
            return client;
        }

        public async ValueTask DisposeAsync()
        {
            await application.StopAsync();
            await application.DisposeAsync();
            certificate.Dispose();
            certificateKey.Dispose();
        }

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
            DateTimeOffset now = DateTimeOffset.UtcNow;
            try
            {
                using X509Certificate2 ephemeral = request.CreateSelfSigned(now.AddMinutes(-1), now.AddMinutes(10));
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
    }

}
