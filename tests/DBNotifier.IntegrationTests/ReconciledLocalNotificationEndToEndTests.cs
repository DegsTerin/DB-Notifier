// Module purpose: Proves committed synthetic transition reconciliation over temporary authenticated HTTPS and an isolated local ledger.
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using DBNotifier.Application.Presentation;
using DBNotifier.Infrastructure.Presentation;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Server.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DBNotifier.IntegrationTests;

public sealed class ReconciledLocalNotificationEndToEndTests
{
    [Fact]
    public async Task SilentBaselineThenCommittedTransitionIsDeliveredOnceAcrossConsumerRestart()
    {
        await using NotificationSandbox sandbox = await NotificationSandbox.StartAsync();
        string stateDirectory = Path.Combine(Path.GetTempPath(), $"dbn-notification-e2e-{Guid.NewGuid():N}");
        try
        {
            using HttpClient firstClient = sandbox.CreateClient(authenticated: true);
            HttpReconciledNotificationTransitionReader firstReader = new(firstClient, TimeProvider.System);
            FileReconciledNotificationLedger firstLedger = new(stateDirectory);
            RecordingSink sink = new();
            using (ReconciledNotificationCoordinator coordinator = new(firstReader, firstLedger, sink, TimeProvider.System))
            {
                ReconciledNotificationCycleResult baseline = await coordinator.RunOnceAsync(new(true, false));
                Assert.Equal(ReconciledNotificationCycleDisposition.BaselineEstablished, baseline.Disposition);
                Assert.Empty(sink.Requests);

                await sandbox.CommitTransitionAsync();
                ReconciledNotificationCycleResult delivered = await coordinator.RunOnceAsync(new(true, false));
                Assert.Equal(ReconciledNotificationCycleDisposition.Completed, delivered.Disposition);
                Assert.Equal(1, delivered.Accepted);
            }
            await firstLedger.DisposeAsync();

            using HttpClient restartedClient = sandbox.CreateClient(authenticated: true);
            HttpReconciledNotificationTransitionReader restartedReader = new(restartedClient, TimeProvider.System);
            FileReconciledNotificationLedger restartedLedger = new(stateDirectory);
            using (ReconciledNotificationCoordinator restarted = new(restartedReader, restartedLedger, sink, TimeProvider.System))
            {
                ReconciledNotificationCycleResult repeated = await restarted.RunOnceAsync(new(true, false));

                Assert.Equal(ReconciledNotificationCycleDisposition.NoChanges, repeated.Disposition);
                ReconciledNotificationDeliveryRequest request = Assert.Single(sink.Requests);
                Assert.Equal("Healthy", request.Transition.PreviousStatus);
                Assert.Equal("Unavailable", request.Transition.CurrentStatus);
                Assert.Equal(ReconciledNotificationTransitionContract.SyntheticSourceKind, request.Transition.SourceKind);
            }
            await restartedLedger.DisposeAsync();
        }
        finally
        {
            if (Directory.Exists(stateDirectory)) Directory.Delete(stateDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task EndpointDeniesMissingTestIdentity()
    {
        await using NotificationSandbox sandbox = await NotificationSandbox.StartAsync();
        using HttpClient client = sandbox.CreateClient(authenticated: false);

        using HttpResponseMessage response = await client.GetAsync(
            $"{ReconciledLocalNotificationSandboxEndpointExtensions.TransitionRoute}?baseline=true");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Proves the ledger ownership fence rejects a second operating-system process.</summary>
    [Fact]
    public async Task LedgerOwnershipIsExclusiveAcrossProcesses()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dbn-notification-multiprocess-{Guid.NewGuid():N}");
        string readyPath = Path.Combine(directory, "holder.ready");
        string releasePath = Path.Combine(directory, "holder.release");
        Directory.CreateDirectory(directory);
        Process? holder = null;
        try
        {
            string projectPath = Path.Combine(
                RepositoryRoot(),
                "tests",
                "DBNotifier.IntegrationTests",
                "DBNotifier.IntegrationTests.csproj");
            ProcessStartInfo startInfo = new("dotnet")
            {
                WorkingDirectory = RepositoryRoot(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("test");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("Release");
            startInfo.ArgumentList.Add("--no-build");
            startInfo.ArgumentList.Add("--no-restore");
            startInfo.ArgumentList.Add("--filter");
            startInfo.ArgumentList.Add($"FullyQualifiedName={typeof(ReconciledLocalNotificationEndToEndTests).FullName}.LedgerLockHolder");
            startInfo.Environment["DBN_NOTIFICATION_LEDGER_HOLDER_DIRECTORY"] = directory;
            startInfo.Environment["DBN_NOTIFICATION_LEDGER_HOLDER_READY"] = readyPath;
            startInfo.Environment["DBN_NOTIFICATION_LEDGER_HOLDER_RELEASE"] = releasePath;
            holder = Process.Start(startInfo) ?? throw new InvalidOperationException("The ledger holder process did not start.");

            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
            while (!File.Exists(readyPath))
            {
                if (holder.HasExited)
                {
                    string error = await holder.StandardError.ReadToEndAsync(timeout.Token);
                    throw new InvalidOperationException($"The ledger holder exited before readiness: {error}");
                }
                await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token);
            }

            Assert.Throws<IOException>(() => new FileReconciledNotificationLedger(directory));
            await File.WriteAllTextAsync(releasePath, "release", timeout.Token);
            await holder.WaitForExitAsync(timeout.Token);
            Assert.Equal(0, holder.ExitCode);
        }
        finally
        {
            if (holder is { HasExited: false })
            {
                await File.WriteAllTextAsync(releasePath, "release");
                using CancellationTokenSource shutdown = new(TimeSpan.FromSeconds(5));
                try
                {
                    await holder.WaitForExitAsync(shutdown.Token);
                }
                catch (OperationCanceledException)
                {
                    holder.Kill(entireProcessTree: true);
                    await holder.WaitForExitAsync();
                }
            }
            holder?.Dispose();
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Holds the file ledger only when invoked by the bounded multiprocess parent test.</summary>
    [Fact]
    public async Task LedgerLockHolder()
    {
        string? directory = Environment.GetEnvironmentVariable("DBN_NOTIFICATION_LEDGER_HOLDER_DIRECTORY");
        string? readyPath = Environment.GetEnvironmentVariable("DBN_NOTIFICATION_LEDGER_HOLDER_READY");
        string? releasePath = Environment.GetEnvironmentVariable("DBN_NOTIFICATION_LEDGER_HOLDER_RELEASE");
        if (directory is null || readyPath is null || releasePath is null)
        {
            return;
        }

        await using FileReconciledNotificationLedger ledger = new(directory);
        await File.WriteAllTextAsync(readyPath, "ready");
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(15));
        while (!File.Exists(releasePath))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token);
        }
    }

    private sealed class RecordingSink : IReconciledNotificationSink
    {
        public List<ReconciledNotificationDeliveryRequest> Requests { get; } = [];

        public ValueTask<ReconciledNotificationDeliveryResult> DeliverAsync(
            ReconciledNotificationDeliveryRequest request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return ValueTask.FromResult(new ReconciledNotificationDeliveryResult(
                ReconciledNotificationDeliveryDisposition.Accepted,
                "delivery.test_accepted"));
        }
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DBNotifier.sln")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new DirectoryNotFoundException("The repository root could not be resolved.");
    }

    private sealed class NotificationSandbox : IAsyncDisposable
    {
        private readonly WebApplication application;
        private readonly X509Certificate2 certificate;
        private readonly ECDsa certificateKey;
        private readonly SqliteConnection connection;
        private readonly Guid agentId;
        private readonly Guid instanceId;

        private NotificationSandbox(
            WebApplication application,
            X509Certificate2 certificate,
            ECDsa certificateKey,
            SqliteConnection connection,
            Uri baseAddress,
            Guid agentId,
            Guid instanceId)
        {
            this.application = application;
            this.certificate = certificate;
            this.certificateKey = certificateKey;
            this.connection = connection;
            this.agentId = agentId;
            this.instanceId = instanceId;
            BaseAddress = baseAddress;
        }

        public Uri BaseAddress { get; }

        public static async Task<NotificationSandbox> StartAsync()
        {
            (X509Certificate2 certificate, ECDsa key) = CreateCertificate();
            SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(ReconciledLocalNotificationSandboxEndpointExtensions).Assembly.FullName,
                EnvironmentName = DashboardTvSandboxEndpointRouteBuilderExtensions.EnvironmentName,
            });
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DashboardTvSandboxEndpointRouteBuilderExtensions.ConfigurationSection}:Enabled"] = bool.TrueString,
                [$"{ReconciledLocalNotificationSandboxEndpointExtensions.ConfigurationSection}:Enabled"] = bool.TrueString,
            });
            builder.WebHost.ConfigureKestrel(options =>
                options.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(certificate)));
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddDbContextFactory<ServerDbContext>(options => options.UseSqlite(connection));
            bool dashboardEnabled = builder.Services.AddDashboardTvSandbox(builder.Environment, builder.Configuration);
            bool notificationsEnabled = builder.Services.AddReconciledLocalNotificationSandbox(
                builder.Environment,
                builder.Configuration);
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
            application.MapDashboardTvSandboxEndpoint(dashboardEnabled);
            application.MapReconciledLocalNotificationSandbox(notificationsEnabled);
            Guid agentId = Guid.NewGuid();
            Guid instanceId = Guid.NewGuid();
            try
            {
                await using (AsyncServiceScope scope = application.Services.CreateAsyncScope())
                {
                    IDbContextFactory<ServerDbContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ServerDbContext>>();
                    await using ServerDbContext context = await factory.CreateDbContextAsync();
                    await context.Database.EnsureCreatedAsync();
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    context.Agents.Add(new RegisteredAgentRow
                    {
                        AgentId = agentId,
                        InstallationId = $"test-{agentId:N}",
                        DisplayName = "Synthetic test Agent",
                        Environment = "Sandbox",
                        Platform = "Windows",
                        AgentVersion = "1.0.0-test",
                        CertificateThumbprint = new string('A', 64),
                        State = "Active",
                        EnrolledAt = now,
                        LastSeenAt = now,
                        ConcurrencyToken = Guid.NewGuid(),
                    });
                    context.Instances.Add(new DatabaseInstanceRow
                    {
                        InstanceId = instanceId,
                        DisplayName = "Synthetic finance database",
                        ProviderType = "synthetic-test",
                        Environment = "Sandbox",
                        EndpointJson = "{}",
                        AssignedAgentId = agentId,
                        TagsJson = "[]",
                        IntervalSeconds = 30,
                        TimeoutSeconds = 5,
                        RetryCount = 0,
                        Enabled = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                        ConcurrencyToken = Guid.NewGuid(),
                    });
                    context.AgentObservationCursors.Add(new AgentObservationCursorRow
                    {
                        AgentId = agentId,
                        HighestContiguousSequence = 1,
                        RejectionLedgerStartSequence = 1,
                        UpdatedAt = now,
                        ConcurrencyToken = Guid.NewGuid(),
                    });
                    await context.SaveChangesAsync();
                }
                await application.StartAsync();
                IServer server = application.Services.GetRequiredService<IServer>();
                Uri baseAddress = new(server.Features.Get<IServerAddressesFeature>()!.Addresses.Single());
                return new(application, certificate, key, connection, baseAddress, agentId, instanceId);
            }
            catch
            {
                await application.DisposeAsync();
                await connection.DisposeAsync();
                certificate.Dispose();
                key.Dispose();
                throw;
            }
        }

        public HttpClient CreateClient(bool authenticated)
        {
            string thumbprint = certificate.Thumbprint;
            HttpClientHandler handler = new()
            {
                ServerCertificateCustomValidationCallback = (_, presented, _, _) =>
                    string.Equals(presented?.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase),
            };
            HttpClient client = new(handler) { BaseAddress = BaseAddress };
            if (authenticated)
            {
                client.DefaultRequestHeaders.Add(
                    DashboardTvSandboxEndpointRouteBuilderExtensions.TestSubjectHeader,
                    "local-notification-reviewer");
            }
            return client;
        }

        public async Task CommitTransitionAsync()
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            IDbContextFactory<ServerDbContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ServerDbContext>>();
            await using ServerDbContext context = await factory.CreateDbContextAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Guid observationId = Guid.NewGuid();
            context.HealthSamples.Add(new HealthSampleRow
            {
                ObservationId = observationId,
                InstanceId = instanceId,
                AgentId = agentId,
                MessageId = Guid.NewGuid(),
                Sequence = 2,
                ProviderType = "synthetic-test",
                ProviderVersion = "1.0.0-test",
                Status = "Unavailable",
                Method = "synthetic",
                EvidenceLevel = "Synthetic",
                ObservedAt = now.AddMilliseconds(-100),
                ReceivedAt = now,
                DurationMilliseconds = 1,
                AttemptCount = 1,
                PayloadHash = new string('B', 64),
            });
            context.Events.Add(new EventRecordRow
            {
                EventId = Guid.NewGuid(),
                InstanceId = instanceId,
                AgentId = agentId,
                SourceObservationId = observationId,
                CorrelationId = Guid.NewGuid(),
                EventType = "Disconnected",
                Severity = "Error",
                ObservedAt = now.AddMilliseconds(-100),
                ReceivedAt = now,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    schemaVersion = "canonical-event-details.v1",
                    previousStatus = "Healthy",
                    currentStatus = "Unavailable",
                }, JsonSerializerOptions.Web),
            });
            AgentObservationCursorRow cursor = await context.AgentObservationCursors.SingleAsync(row => row.AgentId == agentId);
            cursor.HighestContiguousSequence = 2;
            cursor.UpdatedAt = now;
            cursor.ConcurrencyToken = Guid.NewGuid();
            await context.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await application.StopAsync();
            await application.DisposeAsync();
            await connection.DisposeAsync();
            certificate.Dispose();
            certificateKey.Dispose();
        }

        private static (X509Certificate2 Certificate, ECDsa Key) CreateCertificate()
        {
            ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CertificateRequest request = new("CN=localhost", key, HashAlgorithmName.SHA256);
            SubjectAlternativeNameBuilder names = new();
            names.AddDnsName("localhost");
            names.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(names.Build());
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], true));
            DateTimeOffset now = DateTimeOffset.UtcNow;
            using X509Certificate2 ephemeral = request.CreateSelfSigned(now.AddMinutes(-1), now.AddMinutes(10));
            string password = Guid.NewGuid().ToString("N");
            byte[] pkcs12 = ephemeral.Export(X509ContentType.Pkcs12, password);
            try
            {
                return (X509CertificateLoader.LoadPkcs12(
                    pkcs12,
                    password,
                    X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable), key);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pkcs12);
            }
        }
    }
}
