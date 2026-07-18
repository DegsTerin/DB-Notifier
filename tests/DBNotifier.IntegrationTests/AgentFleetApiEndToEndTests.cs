// Module purpose: Exercises the production Agent Fleet routes over isolated loopback HTTPS with ephemeral identity material and SQLite state.
using System.Formats.Asn1;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.RateLimiting;
using DBNotifier.Application.Access;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Infrastructure.AgentFleet;
using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Server.Api;
using DBNotifier.Server.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>
/// Verifies enrollment, mTLS heartbeat, read-only fleet data and immediate revocation through a disposable local
/// HTTPS host. The fixture creates no external connection, provider, command, worker or durable certificate file.
/// </summary>
public sealed class AgentFleetApiEndToEndTests
{
    private const string AgentVersion = "1.0.0-sandbox";
    private const string EnvironmentName = "sandbox";
    private const string PlatformName = "windows-x64";
    private const string ScopeName = "fleet:sandbox";
    private const string HumanSubject = "oidc:sandbox-operator";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Proves the complete restricted flow and confirms that no operational command, observation or outbox work is
    /// created as a side effect.
    /// </summary>
    [Fact]
    public async Task EnrollmentHeartbeatAssignmentsCatalogueAndRevocationRemainLocalAndReadOnly()
    {
        await using AgentFleetSandbox sandbox = await AgentFleetSandbox.StartAsync();
        using ECDsa agentKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest signingRequest = new(
            "CN=DB-Notifier Sandbox Agent",
            agentKey,
            HashAlgorithmName.SHA256);
        byte[] certificateSigningRequest = signingRequest.CreateSigningRequest();
        string installationId = $"installation:{Guid.NewGuid():N}";
        AgentEnrollmentRequest enrollmentRequest = new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            installationId,
            "Sandbox Agent",
            EnvironmentName,
            PlatformName,
            AgentVersion,
            ScopeName,
            sandbox.Now.AddSeconds(-2),
            sandbox.Now.AddSeconds(-1),
            Convert.ToBase64String(certificateSigningRequest));

        string token = await sandbox.SeedEnrollmentTokenAsync(enrollmentRequest);
        using HttpClient enrollmentClient = sandbox.CreateClient();
        using HttpResponseMessage enrollmentResponse = await SendEnrollmentAsync(
            enrollmentClient,
            token,
            enrollmentRequest);
        string enrollmentJson = await enrollmentResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, enrollmentResponse.StatusCode);
        Assert.DoesNotContain(token, enrollmentJson, StringComparison.Ordinal);
        AgentEnrollmentOutcome enrollment = JsonSerializer.Deserialize<AgentEnrollmentOutcome>(
            enrollmentJson,
            JsonOptions) ?? throw new InvalidOperationException("Enrollment returned no typed outcome.");
        Assert.Equal(AgentEnrollmentDisposition.Enrolled, enrollment.Disposition);
        Guid agentId = Assert.IsType<Guid>(enrollment.AgentId);
        Assert.False(enrollment.CertificateDer.IsEmpty);

        using HttpResponseMessage replayResponse = await SendEnrollmentAsync(
            enrollmentClient,
            token,
            enrollmentRequest);
        string replayJson = await replayResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, replayResponse.StatusCode);
        Assert.Contains("enrollment.denied", replayJson, StringComparison.Ordinal);
        Assert.DoesNotContain(agentId.ToString("D"), replayJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(token, replayJson, StringComparison.Ordinal);

        using X509Certificate2 issuedPublicCertificate = X509CertificateLoader.LoadCertificate(
            enrollment.CertificateDer.Span);
        using ECDsa issuedPublicKey = issuedPublicCertificate.GetECDsaPublicKey()
            ?? throw new InvalidOperationException("Issued Agent certificate did not expose ECDSA public material.");
        Assert.Equal(
            agentKey.ExportSubjectPublicKeyInfo(),
            issuedPublicKey.ExportSubjectPublicKeyInfo());
        using X509Certificate2 ephemeralAgentCertificate = issuedPublicCertificate.CopyWithPrivateKey(agentKey);
        using X509Certificate2 agentCertificate = AgentFleetSandbox.CreateTransportCertificate(
            ephemeralAgentCertificate);
        await sandbox.SeedAssignmentsAndHumanAccessAsync(agentId);

        using HttpClient agentClient = sandbox.CreateClient(agentCertificate);
        AgentHeartbeatRequest firstHeartbeat = CreateHeartbeat(agentId, 1, sandbox.Now);
        using HttpResponseMessage firstHeartbeatResponse = await SendAgentJsonAsync(
            agentClient,
            HttpMethod.Post,
            $"/api/v1/agents/{agentId:D}/heartbeats",
            firstHeartbeat);
        Assert.Equal(HttpStatusCode.OK, firstHeartbeatResponse.StatusCode);
        AgentHeartbeatOutcome firstHeartbeatOutcome = await ReadRequiredJsonAsync<AgentHeartbeatOutcome>(
            firstHeartbeatResponse);
        Assert.Equal(AgentHeartbeatDisposition.Accepted, firstHeartbeatOutcome.Disposition);
        Assert.Equal(1, firstHeartbeatOutcome.HighestAcceptedSequence);

        using HttpResponseMessage duplicateHeartbeatResponse = await SendAgentJsonAsync(
            agentClient,
            HttpMethod.Post,
            $"/api/v1/agents/{agentId:D}/heartbeats",
            firstHeartbeat);
        AgentHeartbeatOutcome duplicateHeartbeat = await ReadRequiredJsonAsync<AgentHeartbeatOutcome>(
            duplicateHeartbeatResponse);
        Assert.Equal(HttpStatusCode.OK, duplicateHeartbeatResponse.StatusCode);
        Assert.Equal(AgentHeartbeatDisposition.Duplicate, duplicateHeartbeat.Disposition);

        AgentHeartbeatRequest conflictingHeartbeat = firstHeartbeat with { MessageId = Guid.NewGuid() };
        using HttpResponseMessage conflictResponse = await SendAgentJsonAsync(
            agentClient,
            HttpMethod.Post,
            $"/api/v1/agents/{agentId:D}/heartbeats",
            conflictingHeartbeat);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);

        AgentHeartbeatRequest gappedHeartbeat = CreateHeartbeat(agentId, 3, sandbox.Now) with
        {
            OccurredAt = sandbox.Now.AddSeconds(1),
            SentAt = sandbox.Now.AddSeconds(2),
            AgentTime = sandbox.Now.AddSeconds(2),
        };
        using HttpResponseMessage gapResponse = await SendAgentJsonAsync(
            agentClient,
            HttpMethod.Post,
            $"/api/v1/agents/{agentId:D}/heartbeats",
            gappedHeartbeat);
        AgentHeartbeatOutcome gapOutcome = await ReadRequiredJsonAsync<AgentHeartbeatOutcome>(gapResponse);
        Assert.Equal(HttpStatusCode.OK, gapResponse.StatusCode);
        Assert.Equal(AgentHeartbeatDisposition.AcceptedWithGap, gapOutcome.Disposition);
        Assert.Equal(3, gapOutcome.HighestAcceptedSequence);

        using HttpRequestMessage assignmentsRequest = CreateAgentRequest(
            HttpMethod.Get,
            $"/api/v1/agents/{agentId:D}/assignments");
        using HttpResponseMessage assignmentsResponse = await agentClient.SendAsync(assignmentsRequest);
        string assignmentsJson = await assignmentsResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, assignmentsResponse.StatusCode);
        Assert.NotNull(assignmentsResponse.Headers.ETag);
        Assert.DoesNotContain("administrativeCredentialReference", assignmentsJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fixture:administrative-omitted", assignmentsJson, StringComparison.Ordinal);
        AgentAssignmentSnapshot assignments = JsonSerializer.Deserialize<AgentAssignmentSnapshot>(
            assignmentsJson,
            JsonOptions) ?? throw new InvalidOperationException("Assignments returned no typed snapshot.");
        AgentReadOnlyAssignment assignment = Assert.Single(assignments.Assignments);
        Assert.Equal(agentId, assignments.AgentId);
        Assert.Equal("fixture-provider", assignment.ProviderType);
        Assert.Equal("fixture:monitoring-only", assignment.MonitoringCredentialReference);

        using HttpRequestMessage notModifiedRequest = CreateAgentRequest(
            HttpMethod.Get,
            $"/api/v1/agents/{agentId:D}/assignments");
        notModifiedRequest.Headers.IfNoneMatch.Add(assignmentsResponse.Headers.ETag!);
        using HttpResponseMessage notModifiedResponse = await agentClient.SendAsync(notModifiedRequest);
        Assert.Equal(HttpStatusCode.NotModified, notModifiedResponse.StatusCode);
        Assert.Empty(await notModifiedResponse.Content.ReadAsByteArrayAsync());

        using HttpClient unprivilegedHumanClient = sandbox.CreateHumanClient("oidc:sandbox-unprivileged");
        using HttpResponseMessage deniedCatalogueResponse = await unprivilegedHumanClient.GetAsync("/api/v1/agents");
        string deniedCatalogueJson = await deniedCatalogueResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, deniedCatalogueResponse.StatusCode);
        Assert.DoesNotContain(agentId.ToString("D"), deniedCatalogueJson, StringComparison.OrdinalIgnoreCase);

        using HttpResponseMessage deniedRevocationResponse = await unprivilegedHumanClient.PostAsJsonAsync(
            $"/api/v1/agents/{agentId:D}:revoke",
            new AgentRevocationRequest("sandbox-review-complete"),
            JsonOptions);
        string deniedRevocationJson = await deniedRevocationResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, deniedRevocationResponse.StatusCode);
        Assert.DoesNotContain(agentId.ToString("D"), deniedRevocationJson, StringComparison.OrdinalIgnoreCase);

        using HttpClient humanClient = sandbox.CreateHumanClient(HumanSubject);
        using HttpResponseMessage catalogueResponse = await humanClient.GetAsync("/api/v1/agents");
        string catalogueJson = await catalogueResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, catalogueResponse.StatusCode);
        Assert.DoesNotContain("installationId", catalogueJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("thumbprint", catalogueJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secretHash", catalogueJson, StringComparison.OrdinalIgnoreCase);
        AgentFleetCatalogue catalogue = JsonSerializer.Deserialize<AgentFleetCatalogue>(
            catalogueJson,
            JsonOptions) ?? throw new InvalidOperationException("Catalogue returned no typed result.");
        Assert.True(catalogue.Authorised);
        Assert.Equal(agentId, Assert.Single(catalogue.Agents).AgentId);

        using HttpResponseMessage revocationResponse = await humanClient.PostAsJsonAsync(
            $"/api/v1/agents/{agentId:D}:revoke",
            new AgentRevocationRequest("sandbox-review-complete"),
            JsonOptions);
        AgentRevocationOutcome revocation = await ReadRequiredJsonAsync<AgentRevocationOutcome>(revocationResponse);
        Assert.Equal(HttpStatusCode.OK, revocationResponse.StatusCode);
        Assert.Equal(AgentRevocationDisposition.Revoked, revocation.Disposition);

        using HttpResponseMessage repeatedRevocationResponse = await humanClient.PostAsJsonAsync(
            $"/api/v1/agents/{agentId:D}:revoke",
            new AgentRevocationRequest("sandbox-review-complete"),
            JsonOptions);
        AgentRevocationOutcome repeatedRevocation = await ReadRequiredJsonAsync<AgentRevocationOutcome>(
            repeatedRevocationResponse);
        Assert.Equal(HttpStatusCode.OK, repeatedRevocationResponse.StatusCode);
        Assert.Equal(AgentRevocationDisposition.AlreadyRevoked, repeatedRevocation.Disposition);
        Assert.Equal(revocation.RevokedAt, repeatedRevocation.RevokedAt);

        AgentHeartbeatRequest revokedHeartbeat = CreateHeartbeat(agentId, 4, sandbox.Now);
        using HttpResponseMessage revokedHeartbeatResponse = await SendAgentJsonAsync(
            agentClient,
            HttpMethod.Post,
            $"/api/v1/agents/{agentId:D}/heartbeats",
            revokedHeartbeat);
        Assert.Contains(
            revokedHeartbeatResponse.StatusCode,
            new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });

        using HttpRequestMessage revokedAssignmentsRequest = CreateAgentRequest(
            HttpMethod.Get,
            $"/api/v1/agents/{agentId:D}/assignments");
        using HttpResponseMessage revokedAssignmentsResponse = await agentClient.SendAsync(revokedAssignmentsRequest);
        Assert.Contains(
            revokedAssignmentsResponse.StatusCode,
            new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });

        await sandbox.AssertDurableIsolationAsync(agentId, revocation.RevokedAt);
        CryptographicOperations.ZeroMemory(certificateSigningRequest);
    }

    /// <summary>
    /// Proves the Agent-side coordinator against the real local HTTPS routes, including response loss, logical
    /// restart, content-digest validation, atomic LKG preservation and revocation quarantine.
    /// </summary>
    [Fact]
    public async Task AgentSideIdentityHeartbeatAndAssignmentsRemainSandboxedAndFailClosed()
    {
        await using AgentFleetSandbox sandbox = await AgentFleetSandbox.StartAsync();
        await using AgentLocalSandbox local = await AgentLocalSandbox.StartAsync();
        await using SandboxAgentIdentityStore identities = new(sandbox);
        string installationId = $"installation:{Guid.NewGuid():N}";
        AgentEnrollmentRequest tokenBinding = new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            installationId,
            "Sandbox Agent-side Client",
            EnvironmentName,
            PlatformName,
            AgentVersion,
            ScopeName,
            sandbox.Now,
            sandbox.Now,
            "test-only-public-csr-placeholder");
        string token = await sandbox.SeedEnrollmentTokenAsync(tokenBinding);
        HttpAgentFleetClientTransport httpTransport = new(
            sandbox.BaseAddress,
            AgentVersion,
            identities.GetClient);
        DropFirstHeartbeatResponseTransport lossyTransport = new(httpTransport);
        AgentFleetClientCoordinator coordinator = new(
            identities,
            lossyTransport,
            local.Store,
            new FixedTimeProvider(sandbox.Now),
            TimeSpan.FromMinutes(5));

        AgentFleetClientResult enrolled = await coordinator.EnrolForTestAsync(
            new AgentEnrollmentBootstrap(
                token,
                installationId,
                "Sandbox Agent-side Client",
                EnvironmentName,
                PlatformName,
                AgentVersion,
                ScopeName),
            CancellationToken.None);
        Assert.True(enrolled.Succeeded);
        Assert.Equal(AgentLocalIdentityState.Active, enrolled.State);
        AgentLocalRegistration registration = Assert.IsType<AgentLocalRegistration>(
            await local.Store.GetRegistrationAsync(CancellationToken.None));
        Assert.DoesNotContain(token, await local.ReadPersistedTextAsync(), StringComparison.Ordinal);
        await sandbox.SeedAssignmentsAndHumanAccessAsync(registration.AgentId);

        AgentFleetClientResult responseLost = await coordinator.SendHeartbeatOnceAsync(
            AgentVersion,
            CancellationToken.None);
        Assert.False(responseLost.Succeeded);
        Assert.Equal(AgentLocalIdentityState.Offline, responseLost.State);
        PendingAgentHeartbeat pendingAfterLoss = await local.ReadPendingHeartbeatAsync(registration.AgentId);

        AgentFleetClientCoordinator restartedCoordinator = new(
            identities,
            lossyTransport,
            local.Store,
            new FixedTimeProvider(sandbox.Now),
            TimeSpan.FromMinutes(5));
        AgentFleetClientResult replayed = await restartedCoordinator.SendHeartbeatOnceAsync(
            AgentVersion,
            CancellationToken.None);
        Assert.True(replayed.Succeeded);
        Assert.Equal("heartbeat.accepted", replayed.Code);
        Assert.Equal(1, pendingAfterLoss.Request.Sequence);

        AgentFleetClientResult nextHeartbeat = await restartedCoordinator.SendHeartbeatOnceAsync(
            AgentVersion,
            CancellationToken.None);
        Assert.True(nextHeartbeat.Succeeded);
        Assert.Equal(3, lossyTransport.HeartbeatCalls);

        AgentFleetClientResult applied = await restartedCoordinator.ReconcileAssignmentsOnceAsync(
            AgentVersion,
            CancellationToken.None);
        Assert.True(applied.Succeeded);
        Assert.Equal("assignments.applied", applied.Code);
        AgentAssignmentLocalState firstState = await local.Store.GetAssignmentStateAsync(
            registration.AgentId,
            CancellationToken.None);
        Assert.True(AgentAssignmentVersion.IsUpperHexDigest(firstState.Version));
        Assert.Equal(1, await local.CountAssignmentsAsync());
        Assert.All(await local.ReadAssignmentsAsync(), row =>
            Assert.Null(row.AdministrativeCredentialReference));

        AgentFleetClientResult unchanged = await restartedCoordinator.ReconcileAssignmentsOnceAsync(
            AgentVersion,
            CancellationToken.None);
        Assert.True(unchanged.Succeeded);
        Assert.Equal("assignments.not_modified", unchanged.Code);

        await sandbox.ReplaceAssignmentsAsync(registration.AgentId);
        TamperNextAssignmentTransport tamperedTransport = new(httpTransport);
        AgentFleetClientCoordinator tamperedCoordinator = new(
            identities,
            tamperedTransport,
            local.Store,
            new FixedTimeProvider(sandbox.Now),
            TimeSpan.FromMinutes(5));
        AgentFleetClientResult rejected = await tamperedCoordinator.ReconcileAssignmentsOnceAsync(
            AgentVersion,
            CancellationToken.None);
        Assert.False(rejected.Succeeded);
        Assert.Equal(AgentLocalIdentityState.Active, rejected.State);
        Assert.Equal("assignments.digest_mismatch", rejected.Code);
        Assert.Equal(firstState.Version, (await local.Store.GetAssignmentStateAsync(
            registration.AgentId,
            CancellationToken.None)).Version);
        Assert.Equal(1, await local.CountAssignmentsAsync());

        AgentFleetClientResult replaced = await restartedCoordinator.ReconcileAssignmentsOnceAsync(
            AgentVersion,
            CancellationToken.None);
        Assert.True(replaced.Succeeded);
        Assert.Equal(2, await local.CountAssignmentsAsync());
        Assert.NotEqual(firstState.Version, (await local.Store.GetAssignmentStateAsync(
            registration.AgentId,
            CancellationToken.None)).Version);

        await sandbox.RevokeAgentAsync(registration.AgentId);
        AgentFleetClientResult revoked = await restartedCoordinator.SendHeartbeatOnceAsync(
            AgentVersion,
            CancellationToken.None);
        Assert.False(revoked.Succeeded);
        Assert.Equal(AgentLocalIdentityState.RevokedOrDenied, revoked.State);
        AgentFleetClientResult quarantined = await restartedCoordinator.ReconcileAssignmentsOnceAsync(
            AgentVersion,
            CancellationToken.None);
        Assert.False(quarantined.Succeeded);
        Assert.Equal(AgentLocalIdentityState.RevokedOrDenied, quarantined.State);

        await local.AssertNoOperationalEffectsAsync();
        await sandbox.AssertClientIsolationAsync(registration.AgentId);
    }

    /// <summary>Creates the canonical version-one heartbeat for one enrolled Agent.</summary>
    /// <param name="agentId">Enrolled Agent identifier.</param>
    /// <param name="sequence">Monotonic heartbeat sequence.</param>
    /// <param name="now">Trusted sandbox time.</param>
    /// <returns>Bounded heartbeat request.</returns>
    private static AgentHeartbeatRequest CreateHeartbeat(Guid agentId, long sequence, DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            agentId,
            sequence,
            now.AddSeconds(-2),
            now.AddSeconds(-1),
            AgentVersion,
            AgentFleetProtocol.CurrentProtocolVersion,
            AgentFleetProtocol.CurrentProtocolVersion,
            0,
            null,
            now.AddSeconds(-1));

    /// <summary>Sends enrollment with the credential isolated in the authorisation header.</summary>
    /// <param name="client">Anonymous HTTPS client.</param>
    /// <param name="token">Ephemeral one-time token.</param>
    /// <param name="request">Non-secret enrollment body.</param>
    /// <returns>Server response.</returns>
    private static async Task<HttpResponseMessage> SendEnrollmentAsync(
        HttpClient client,
        string token,
        AgentEnrollmentRequest request)
    {
        using HttpRequestMessage message = new(HttpMethod.Post, "/api/v1/agents/enroll")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };
        message.Headers.TryAddWithoutValidation("Authorization", $"DBN-Enrollment {token}");
        AddAgentProtocolHeaders(message);
        return await client.SendAsync(message);
    }

    /// <summary>Sends one versioned Agent JSON request over mTLS.</summary>
    /// <typeparam name="T">Request body type.</typeparam>
    /// <param name="client">mTLS client.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Relative Agent route.</param>
    /// <param name="body">Versioned request body.</param>
    /// <returns>Server response.</returns>
    private static async Task<HttpResponseMessage> SendAgentJsonAsync<T>(
        HttpClient client,
        HttpMethod method,
        string path,
        T body)
    {
        using HttpRequestMessage message = CreateAgentRequest(method, path);
        message.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(message);
    }

    /// <summary>Creates one Agent request with the exact protocol, schema and version headers.</summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Relative Agent route.</param>
    /// <returns>Configured request.</returns>
    private static HttpRequestMessage CreateAgentRequest(HttpMethod method, string path)
    {
        HttpRequestMessage request = new(method, path);
        AddAgentProtocolHeaders(request);
        return request;
    }

    /// <summary>Adds the exact Agent Fleet version headers.</summary>
    /// <param name="request">Request to configure.</param>
    private static void AddAgentProtocolHeaders(HttpRequestMessage request)
    {
        request.Headers.Add(
            "DBN-Protocol-Version",
            AgentFleetProtocol.CurrentProtocolVersion.ToString(CultureInfo.InvariantCulture));
        request.Headers.Add(
            "DBN-Message-Schema",
            AgentFleetProtocol.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture));
        request.Headers.Add("DBN-Agent-Version", AgentVersion);
    }

    /// <summary>Reads a required JSON response after preserving its HTTP status for the caller's assertion.</summary>
    /// <typeparam name="T">Expected response contract.</typeparam>
    /// <param name="response">HTTP response.</param>
    /// <returns>Deserialised typed contract.</returns>
    private static async Task<T> ReadRequiredJsonAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(JsonOptions) ??
        throw new InvalidOperationException($"The sandbox response did not contain {typeof(T).Name}.");

    /// <summary>Owns one temporary Agent SQLite database and exposes only test assertions over non-secret state.</summary>
    private sealed class AgentLocalSandbox : IAsyncDisposable
    {
        private readonly SqliteConnection keeper;
        private readonly DbContextOptions<AgentDbContext> options;

        private AgentLocalSandbox(
            SqliteConnection keeper,
            DbContextOptions<AgentDbContext> options,
            AgentFleetLocalStore store)
        {
            this.keeper = keeper;
            this.options = options;
            Store = store;
        }

        /// <summary>Gets the real SQLite implementation used by the Agent-side coordinator.</summary>
        public AgentFleetLocalStore Store { get; }

        /// <summary>Creates the migrated named in-memory Agent store.</summary>
        /// <returns>Disposable local Agent sandbox.</returns>
        public static async Task<AgentLocalSandbox> StartAsync()
        {
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = $"DBNotifierAgentClient{Guid.NewGuid():N}",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true,
            }.ToString();
            SqliteConnection keeper = new(connectionString);
            await keeper.OpenAsync();
            DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
                .UseSqlite(connectionString)
                .Options;
            AgentContextFactory factory = new(options);
            await using AgentDbContext context = new(options);
            await context.Database.MigrateAsync();
            return new AgentLocalSandbox(keeper, options, new AgentFleetLocalStore(factory));
        }

        /// <summary>Reads the exact durable pending heartbeat for restart/replay assertions.</summary>
        /// <param name="agentId">Enrolled Agent identifier.</param>
        /// <returns>Pending typed heartbeat.</returns>
        public async Task<PendingAgentHeartbeat> ReadPendingHeartbeatAsync(Guid agentId)
        {
            await using AgentDbContext context = new(options);
            AgentFleetStateRow row = await context.AgentFleetStates.AsNoTracking()
                .SingleAsync(item => item.AgentId == agentId);
            AgentHeartbeatRequest request = JsonSerializer.Deserialize<AgentHeartbeatRequest>(
                row.PendingHeartbeatPayloadJson ?? throw new InvalidOperationException("No pending heartbeat exists."),
                JsonOptions) ?? throw new InvalidOperationException("Pending heartbeat could not be read.");
            return new PendingAgentHeartbeat(request, false);
        }

        /// <summary>Returns all persisted text used to prove that the one-time token was not stored.</summary>
        /// <returns>Concatenated non-secret text columns.</returns>
        public async Task<string> ReadPersistedTextAsync()
        {
            await using AgentDbContext context = new(options);
            string[] registration = await context.Registrations.AsNoTracking()
                .Select(row => row.InstallationId + row.Environment + row.IdentityCertificateReference +
                    row.CertificateThumbprint + row.IdentityState + row.ActiveConfigurationVersion)
                .ToArrayAsync();
            string[] fleet = await context.AgentFleetStates.AsNoTracking()
                .Select(row => (row.PendingHeartbeatPayloadJson ?? string.Empty) +
                    (row.AssignmentEntityTag ?? string.Empty) + (row.LastErrorCode ?? string.Empty))
                .ToArrayAsync();
            return string.Concat(registration.Concat(fleet));
        }

        /// <summary>Counts the currently active complete local assignment set.</summary>
        /// <returns>Assignment row count.</returns>
        public async Task<int> CountAssignmentsAsync()
        {
            await using AgentDbContext context = new(options);
            return await context.InstanceAssignments.CountAsync();
        }

        /// <summary>Reads local assignments for secret-purpose and replacement assertions.</summary>
        /// <returns>Complete local assignment rows.</returns>
        public async Task<AgentInstanceAssignmentRow[]> ReadAssignmentsAsync()
        {
            await using AgentDbContext context = new(options);
            return await context.InstanceAssignments.AsNoTracking().OrderBy(row => row.InstanceId).ToArrayAsync();
        }

        /// <summary>Proves that assignment reconciliation activated no monitoring, outbox or command path.</summary>
        public async Task AssertNoOperationalEffectsAsync()
        {
            await using AgentDbContext context = new(options);
            Assert.Equal(0, await context.HealthObservations.CountAsync());
            Assert.Equal(0, await context.OutboxMessages.CountAsync());
            Assert.Equal(0, await context.InboxCommands.CountAsync());
            Assert.All(await context.InstanceAssignments.AsNoTracking().ToArrayAsync(), row =>
                Assert.Null(row.AdministrativeCredentialReference));
            Assert.DoesNotContain(
                context.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()),
                property => property.Name.Contains("PrivateKey", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("EnrollmentToken", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("CertificateDer", StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync() => keeper.DisposeAsync();

        private sealed class AgentContextFactory(DbContextOptions<AgentDbContext> options)
            : IDbContextFactory<AgentDbContext>
        {
            /// <inheritdoc />
            public AgentDbContext CreateDbContext() => new(options);

            /// <inheritdoc />
            public Task<AgentDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(new AgentDbContext(options));
        }
    }

    /// <summary>Holds P-256 private identity material only in the E2E process and disposes every client/container.</summary>
    private sealed class SandboxAgentIdentityStore(AgentFleetSandbox sandbox)
        : IAgentEnrollmentIdentityStore, IAsyncDisposable
    {
        private readonly Dictionary<Guid, ECDsa> pending = [];
        private readonly Dictionary<string, (X509Certificate2 Certificate, HttpClient Client)> completed = [];
        private readonly HttpClient enrollmentClient = sandbox.CreateClient();

        /// <inheritdoc />
        public ValueTask<AgentEnrollmentKeyMaterial> BeginAsync(
            string installationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentException.ThrowIfNullOrWhiteSpace(installationId);
            ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            Guid operationId = Guid.NewGuid();
            pending.Add(operationId, key);
            CertificateRequest request = new(
                "CN=DB-Notifier Agent-side Sandbox",
                key,
                HashAlgorithmName.SHA256);
            byte[] csr = request.CreateSigningRequest();
            try
            {
                return ValueTask.FromResult(new AgentEnrollmentKeyMaterial(
                    operationId,
                    Convert.ToBase64String(csr)));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(csr);
            }
        }

        /// <inheritdoc />
        public ValueTask<AgentIdentityCompletion> CompleteAsync(
            Guid operationId,
            ReadOnlyMemory<byte> certificateDer,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!pending.Remove(operationId, out ECDsa? key))
            {
                throw new InvalidOperationException("The sandbox enrollment key is unavailable.");
            }

            using (key)
            using (X509Certificate2 publicCertificate = X509CertificateLoader.LoadCertificate(certificateDer.Span))
            using (ECDsa publicKey = publicCertificate.GetECDsaPublicKey() ??
                throw new CryptographicException("The issued certificate has no ECDSA key."))
            {
                if (!CryptographicOperations.FixedTimeEquals(
                        key.ExportSubjectPublicKeyInfo(),
                        publicKey.ExportSubjectPublicKeyInfo()))
                {
                    throw new CryptographicException("The issued certificate does not match the pending key.");
                }

                using X509Certificate2 combined = publicCertificate.CopyWithPrivateKey(key);
                X509Certificate2 transportCertificate = AgentFleetSandbox.CreateTransportCertificate(combined);
                string reference = $"sandbox-identity:{Guid.NewGuid():N}";
                HttpClient client = sandbox.CreateClient(transportCertificate);
                completed.Add(reference, (transportCertificate, client));
                return ValueTask.FromResult(new AgentIdentityCompletion(
                    reference,
                    AgentFleetSandbox.NormaliseThumbprint(transportCertificate.Thumbprint)));
            }
        }

        /// <inheritdoc />
        public ValueTask AbortAsync(Guid operationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pending.Remove(operationId, out ECDsa? key))
            {
                key.Dispose();
            }

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public ValueTask RemoveAsync(string identityReference, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (completed.Remove(identityReference, out var identity))
            {
                identity.Client.Dispose();
                identity.Certificate.Dispose();
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>Returns the anonymous enrollment client or the exact completed mTLS client.</summary>
        /// <param name="identityReference">Opaque identity reference, or null for enrollment.</param>
        /// <returns>Reusable sandbox HTTP client.</returns>
        public HttpClient GetClient(string? identityReference)
        {
            if (identityReference is null)
            {
                return enrollmentClient;
            }

            return completed.TryGetValue(identityReference, out var identity)
                ? identity.Client
                : throw new InvalidOperationException("The sandbox identity reference is unavailable.");
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            enrollmentClient.Dispose();
            foreach (ECDsa key in pending.Values)
            {
                key.Dispose();
            }

            foreach (var identity in completed.Values)
            {
                identity.Client.Dispose();
                identity.Certificate.Dispose();
            }

            pending.Clear();
            completed.Clear();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Simulates one lost heartbeat response after the real Server has already accepted the request.</summary>
    private sealed class DropFirstHeartbeatResponseTransport(IAgentFleetClientTransport inner)
        : IAgentFleetClientTransport
    {
        private bool drop = true;

        /// <summary>Gets the number of real heartbeat requests sent through the wrapper.</summary>
        public int HeartbeatCalls { get; private set; }

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentEnrollmentOutcome>> EnrolAsync(
            string token,
            AgentEnrollmentRequest request,
            CancellationToken cancellationToken) => inner.EnrolAsync(token, request, cancellationToken);

        /// <inheritdoc />
        public async ValueTask<AgentFleetTransportResult<AgentHeartbeatOutcome>> SendHeartbeatAsync(
            string identityReference,
            AgentHeartbeatRequest request,
            CancellationToken cancellationToken)
        {
            HeartbeatCalls++;
            AgentFleetTransportResult<AgentHeartbeatOutcome> result = await inner.SendHeartbeatAsync(
                identityReference,
                request,
                cancellationToken);
            if (drop)
            {
                drop = false;
                return new AgentFleetTransportResult<AgentHeartbeatOutcome>(
                    AgentFleetTransportDisposition.TransientFailure,
                    null,
                    null,
                    "transport.response_lost");
            }

            return result;
        }

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentAssignmentSnapshot>> GetAssignmentsAsync(
            string identityReference,
            Guid agentId,
            string agentVersion,
            string? currentVersion,
            CancellationToken cancellationToken) =>
            inner.GetAssignmentsAsync(identityReference, agentId, agentVersion, currentVersion, cancellationToken);
    }

    /// <summary>Mutates one authenticated assignment body after transport to prove client-side digest rejection.</summary>
    private sealed class TamperNextAssignmentTransport(IAgentFleetClientTransport inner)
        : IAgentFleetClientTransport
    {
        private bool tamper = true;

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentEnrollmentOutcome>> EnrolAsync(
            string token,
            AgentEnrollmentRequest request,
            CancellationToken cancellationToken) => inner.EnrolAsync(token, request, cancellationToken);

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentHeartbeatOutcome>> SendHeartbeatAsync(
            string identityReference,
            AgentHeartbeatRequest request,
            CancellationToken cancellationToken) => inner.SendHeartbeatAsync(identityReference, request, cancellationToken);

        /// <inheritdoc />
        public async ValueTask<AgentFleetTransportResult<AgentAssignmentSnapshot>> GetAssignmentsAsync(
            string identityReference,
            Guid agentId,
            string agentVersion,
            string? currentVersion,
            CancellationToken cancellationToken)
        {
            AgentFleetTransportResult<AgentAssignmentSnapshot> result = await inner.GetAssignmentsAsync(
                identityReference,
                agentId,
                agentVersion,
                currentVersion,
                cancellationToken);
            if (!tamper || result.Disposition != AgentFleetTransportDisposition.Succeeded ||
                result.Value is not { Assignments.Count: > 0 } snapshot)
            {
                return result;
            }

            tamper = false;
            AgentReadOnlyAssignment[] assignments = snapshot.Assignments.ToArray();
            assignments[0] = assignments[0] with { DisplayName = assignments[0].DisplayName + " tampered" };
            return result with { Value = snapshot with { Assignments = assignments } };
        }
    }

    /// <summary>Owns the disposable HTTPS server, ephemeral CA and shared in-memory SQLite database.</summary>
    private sealed class AgentFleetSandbox : IAsyncDisposable
    {
        private readonly SqliteConnection databaseKeeper;
        private readonly ECDsa rootKey;
        private readonly ECDsa serverKey;
        private readonly X509Certificate2 rootCertificate;
        private readonly X509Certificate2 serverCertificate;
        private readonly WebApplication application;
        private bool disposed;

        /// <summary>Initialises a fully composed but already started local sandbox.</summary>
        /// <param name="databaseKeeper">Connection keeping the named in-memory database alive.</param>
        /// <param name="rootKey">Ephemeral root private key.</param>
        /// <param name="serverKey">Ephemeral server private key.</param>
        /// <param name="rootCertificate">Ephemeral custom trust root.</param>
        /// <param name="serverCertificate">Ephemeral loopback server certificate.</param>
        /// <param name="application">Started local Web application.</param>
        /// <param name="baseAddress">Bound loopback HTTPS address.</param>
        /// <param name="now">Fixed trusted application time.</param>
        private AgentFleetSandbox(
            SqliteConnection databaseKeeper,
            ECDsa rootKey,
            ECDsa serverKey,
            X509Certificate2 rootCertificate,
            X509Certificate2 serverCertificate,
            WebApplication application,
            Uri baseAddress,
            DateTimeOffset now)
        {
            this.databaseKeeper = databaseKeeper;
            this.rootKey = rootKey;
            this.serverKey = serverKey;
            this.rootCertificate = rootCertificate;
            this.serverCertificate = serverCertificate;
            this.application = application;
            BaseAddress = baseAddress;
            Now = now;
        }

        /// <summary>Gets the exact HTTPS loopback address selected by Kestrel.</summary>
        public Uri BaseAddress { get; }

        /// <summary>Gets the fixed UTC clock used by the Application service.</summary>
        public DateTimeOffset Now { get; }

        /// <summary>Creates and starts the loopback-only sandbox.</summary>
        /// <returns>Started disposable sandbox.</returns>
        public static async Task<AgentFleetSandbox> StartAsync()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            ECDsa rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            ECDsa serverKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            X509Certificate2 rootCertificate = CreateRootCertificate(rootKey, now);
            X509Certificate2 serverCertificate = CreateServerCertificate(
                rootCertificate,
                serverKey,
                now);
            if (!serverCertificate.HasPrivateKey ||
                !BuildCustomChain(serverCertificate, rootCertificate, "1.3.6.1.5.5.7.3.1"))
            {
                throw new InvalidOperationException("The ephemeral loopback server certificate is not usable.");
            }

            string databaseName = $"DBNotifierAgentFleet{Guid.NewGuid():N}";
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databaseName,
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true,
            }.ToString();
            SqliteConnection databaseKeeper = new(connectionString);
            await databaseKeeper.OpenAsync();

            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(AgentFleetEndpointRouteBuilderExtensions).Assembly.FullName,
                EnvironmentName = "IntegrationTests",
            });
            builder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 1_048_576;
                options.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.UseHttps(
                    new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = serverCertificate,
                        ClientCertificateMode = ClientCertificateMode.AllowCertificate,
                        ClientCertificateValidation = (certificate, _, _) =>
                            BuildCustomChain(certificate, rootCertificate, "1.3.6.1.5.5.7.3.2"),
                    }));
            });
            builder.Services.AddProblemDetails();
            builder.Services.AddDbContextFactory<ServerDbContext>(options => options.UseSqlite(connectionString));
            builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(now));
            builder.Services.AddSingleton<IAgentCertificateIssuer>(
                new EphemeralAgentCertificateIssuer(rootCertificate));
            builder.Services.AddScoped<IAgentFleetStore, AgentFleetStore>();
            builder.Services.AddScoped<AgentFleetService>();
            builder.Services.AddScoped<AgentCertificateIdentityValidator>();
            builder.Services.AddSingleton<HumanActorResolver>();
            builder.Services.AddSingleton<IAuthorizationHandler, AgentRouteAuthorizationHandler>();
            AddAuthentication(builder.Services, rootCertificate);
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy(ApiSecurityDefaults.AgentApiPolicy, policy =>
                {
                    policy.AddAuthenticationSchemes(CertificateAuthenticationDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new AgentRouteRequirement());
                })
                .AddPolicy(ApiSecurityDefaults.HumanApiPolicy, policy =>
                {
                    policy.AddAuthenticationSchemes(HumanAuthenticationDefaults.Scheme);
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("sub");
                });
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                AddRatePolicy(options, AgentFleetEndpointRouteBuilderExtensions.EnrollmentRateLimitPolicy);
                AddRatePolicy(options, "AgentApiRateLimit");
                AddRatePolicy(options, "HumanApiRateLimit");
            });

            WebApplication application = builder.Build();
            application.UseRouting();
            application.UseMiddleware<ProtectedTransportMiddleware>();
            application.UseAuthentication();
            application.UseRateLimiter();
            application.UseAuthorization();
            application.MapAgentFleetEndpoints();

            try
            {
                await using (AsyncServiceScope scope = application.Services.CreateAsyncScope())
                {
                    ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
                    await context.Database.EnsureCreatedAsync();
                }

                await application.StartAsync();
                IServer server = application.Services.GetRequiredService<IServer>();
                string address = server.Features.Get<IServerAddressesFeature>()?.Addresses.Single() ??
                    throw new InvalidOperationException("Kestrel did not expose its bound loopback address.");
                Uri baseAddress = new(address);
                if (baseAddress.Scheme != Uri.UriSchemeHttps ||
                    !IPAddress.TryParse(baseAddress.Host, out IPAddress? addressValue) ||
                    !IPAddress.IsLoopback(addressValue))
                {
                    throw new InvalidOperationException("The integration host was not bound exclusively to HTTPS loopback.");
                }

                return new AgentFleetSandbox(
                    databaseKeeper,
                    rootKey,
                    serverKey,
                    rootCertificate,
                    serverCertificate,
                    application,
                    baseAddress,
                    now);
            }
            catch
            {
                await application.DisposeAsync();
                await databaseKeeper.DisposeAsync();
                serverCertificate.Dispose();
                rootCertificate.Dispose();
                serverKey.Dispose();
                rootKey.Dispose();
                throw;
            }
        }

        /// <summary>Seeds one salted enrollment proof and returns its ephemeral raw token only to the test caller.</summary>
        /// <param name="request">Enrollment request whose exact metadata is pre-authorised.</param>
        /// <returns>One-time token held only in process memory.</returns>
        public async Task<string> SeedEnrollmentTokenAsync(AgentEnrollmentRequest request)
        {
            Guid tokenId = Guid.NewGuid();
            byte[] tokenSecret = RandomNumberGenerator.GetBytes(AgentEnrollmentTokenProof.SecretLength);
            byte[] salt = RandomNumberGenerator.GetBytes(AgentEnrollmentTokenProof.SaltLength);
            byte[]? secretHash = null;
            try
            {
                string encodedSecret = Convert.ToBase64String(tokenSecret)
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');
                string token = $"{tokenId:N}.{encodedSecret}";
                secretHash = AgentEnrollmentTokenProof.ComputeSecretHash(token, salt);
                await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
                ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
                context.AgentEnrollmentTokens.Add(new AgentEnrollmentTokenRow
                {
                    EnrollmentTokenId = tokenId,
                    Salt = salt.ToArray(),
                    SecretHash = secretHash.ToArray(),
                    HashAlgorithm = AgentEnrollmentTokenProof.HashAlgorithm,
                    ExpectedInstallationId = request.InstallationId,
                    ExpectedEnvironment = request.Environment,
                    ExpectedPlatform = request.Platform,
                    Scope = request.RequestedScope,
                    State = "Active",
                    IssuedAt = Now.AddMinutes(-1),
                    ExpiresAt = Now.AddMinutes(10),
                    ConsumedAt = null,
                    RevokedAt = null,
                    ConsumedByAgentId = null,
                    ConcurrencyToken = Guid.NewGuid(),
                });
                await context.SaveChangesAsync();
                return token;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(tokenSecret);
                CryptographicOperations.ZeroMemory(salt);
                if (secretHash is not null)
                {
                    CryptographicOperations.ZeroMemory(secretHash);
                }
            }
        }

        /// <summary>Seeds one safe assignment and the minimal global read/revoke RBAC role.</summary>
        /// <param name="agentId">Enrolled Agent identifier.</param>
        public async Task SeedAssignmentsAndHumanAccessAsync(Guid agentId)
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            Guid userId = Guid.NewGuid();
            Guid roleId = Guid.NewGuid();
            Guid readPermissionId = Guid.NewGuid();
            Guid revokePermissionId = Guid.NewGuid();
            DateTimeOffset now = Now;
            context.Instances.AddRange(
                new DatabaseInstanceRow
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "Sandbox Assignment",
                    ProviderType = "fixture-provider",
                    Environment = EnvironmentName,
                    EndpointJson = "{\"host\":\"sandbox.invalid\",\"port\":5432}",
                    MonitoringCredentialReference = "fixture:monitoring-only",
                    AdministrativeCredentialReference = "fixture:administrative-omitted",
                    AssignedAgentId = agentId,
                    TagsJson = "{\"purpose\":\"integration-test\"}",
                    IntervalSeconds = 30,
                    TimeoutSeconds = 5,
                    RetryCount = 1,
                    Enabled = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    ArchivedAt = null,
                    ConcurrencyToken = Guid.NewGuid(),
                },
                new DatabaseInstanceRow
                {
                    InstanceId = Guid.NewGuid(),
                    DisplayName = "Disabled Sandbox Assignment",
                    ProviderType = "fixture-provider",
                    Environment = EnvironmentName,
                    EndpointJson = "{}",
                    MonitoringCredentialReference = null,
                    AdministrativeCredentialReference = "fixture:disabled-administrative-omitted",
                    AssignedAgentId = agentId,
                    TagsJson = "{}",
                    IntervalSeconds = 30,
                    TimeoutSeconds = 5,
                    RetryCount = 1,
                    Enabled = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                    ArchivedAt = null,
                    ConcurrencyToken = Guid.NewGuid(),
                });
            context.Users.Add(new PlatformUserRow
            {
                UserId = userId,
                SubjectId = HumanSubject,
                DisplayName = "Sandbox Operator",
                State = "Active",
                CreatedAt = now,
                UpdatedAt = now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Roles.Add(new RoleRow
            {
                RoleId = roleId,
                Name = "Sandbox Agent Fleet Reviewer",
                Description = "Local integration-test role only.",
                IsSystem = false,
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Permissions.AddRange(
                new PermissionRow
                {
                    PermissionId = readPermissionId,
                    Code = PlatformPermissions.AgentsRead,
                    Description = "Read safe Agent Fleet facts.",
                },
                new PermissionRow
                {
                    PermissionId = revokePermissionId,
                    Code = PlatformPermissions.AgentsRevoke,
                    Description = "Revoke one Agent identity.",
                });
            context.RolePermissions.AddRange(
                new RolePermissionRow { RoleId = roleId, PermissionId = readPermissionId },
                new RolePermissionRow { RoleId = roleId, PermissionId = revokePermissionId });
            context.RoleAssignments.Add(new RoleAssignmentRow
            {
                RoleAssignmentId = Guid.NewGuid(),
                UserId = userId,
                RoleId = roleId,
                ScopeType = "Global",
                ScopeValue = "*",
                GrantedByUserId = userId,
                GrantedAt = now,
                ExpiresAt = now.AddHours(1),
            });
            await context.SaveChangesAsync();
        }

        /// <summary>Replaces the complete server assignment projection with a deterministic second version.</summary>
        /// <param name="agentId">Enrolled Agent that owns the replacement snapshot.</param>
        public async Task ReplaceAssignmentsAsync(Guid agentId)
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            DatabaseInstanceRow[] previous = await context.Instances
                .Where(row => row.AssignedAgentId == agentId)
                .ToArrayAsync();
            context.Instances.RemoveRange(previous);
            DateTimeOffset updatedAt = Now.AddMinutes(1);
            context.Instances.AddRange(
                new DatabaseInstanceRow
                {
                    InstanceId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    DisplayName = "Replacement Assignment A",
                    ProviderType = "fixture-provider",
                    Environment = EnvironmentName,
                    EndpointJson = "{\"host\":\"replacement-a.invalid\",\"port\":5432}",
                    MonitoringCredentialReference = "fixture:monitoring-only",
                    AdministrativeCredentialReference = "fixture:administrative-never-sent",
                    AssignedAgentId = agentId,
                    TagsJson = "{\"revision\":2}",
                    IntervalSeconds = 45,
                    TimeoutSeconds = 6,
                    RetryCount = 1,
                    Enabled = true,
                    CreatedAt = Now,
                    UpdatedAt = updatedAt,
                    ArchivedAt = null,
                    ConcurrencyToken = Guid.NewGuid(),
                },
                new DatabaseInstanceRow
                {
                    InstanceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    DisplayName = "Replacement Assignment B",
                    ProviderType = "fixture-provider",
                    Environment = EnvironmentName,
                    EndpointJson = "{\"host\":\"replacement-b.invalid\",\"port\":5433}",
                    MonitoringCredentialReference = null,
                    AdministrativeCredentialReference = "fixture:administrative-never-sent",
                    AssignedAgentId = agentId,
                    TagsJson = "[\"sandbox\",\"replacement\"]",
                    IntervalSeconds = 60,
                    TimeoutSeconds = 7,
                    RetryCount = 2,
                    Enabled = true,
                    CreatedAt = Now,
                    UpdatedAt = updatedAt,
                    ArchivedAt = null,
                    ConcurrencyToken = Guid.NewGuid(),
                });
            await context.SaveChangesAsync();
        }

        /// <summary>Revokes one Agent through the real human-authorised endpoint.</summary>
        /// <param name="agentId">Agent to revoke.</param>
        public async Task RevokeAgentAsync(Guid agentId)
        {
            using HttpClient human = CreateHumanClient(HumanSubject);
            using HttpResponseMessage response = await human.PostAsJsonAsync(
                $"/api/v1/agents/{agentId:D}:revoke",
                new AgentRevocationRequest("sandbox-client-review-complete"),
                JsonOptions);
            AgentRevocationOutcome outcome = await ReadRequiredJsonAsync<AgentRevocationOutcome>(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(AgentRevocationDisposition.Revoked, outcome.Disposition);
        }

        /// <summary>Confirms Agent-side E2E server evidence without requiring the direct-route test's gap sequence.</summary>
        /// <param name="agentId">Enrolled and revoked Agent identifier.</param>
        public async Task AssertClientIsolationAsync(Guid agentId)
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            RegisteredAgentRow agent = await context.Agents.SingleAsync(row => row.AgentId == agentId);
            AgentHeartbeatCursorRow cursor = await context.AgentHeartbeatCursors.SingleAsync(
                row => row.AgentId == agentId);
            long[] sequences = await context.AgentHeartbeats
                .Where(row => row.AgentId == agentId)
                .OrderBy(row => row.Sequence)
                .Select(row => row.Sequence)
                .ToArrayAsync();
            Assert.Equal("Revoked", agent.State);
            Assert.Equal(2, cursor.HighestAcceptedSequence);
            Assert.Equal(new long[] { 1, 2 }, sequences);
            Assert.Equal(0, await context.AdministrativeCommands.CountAsync());
            Assert.Equal(0, await context.CommandAttempts.CountAsync());
            Assert.Equal(0, await context.HealthSamples.CountAsync());
            Assert.Equal(0, await context.Events.CountAsync());
            Assert.Equal(0, await context.OutboxMessages.CountAsync());
            Assert.Equal(0, await context.NotificationDeliveries.CountAsync());
        }

        /// <summary>Creates an HTTPS client optionally carrying one ephemeral Agent certificate.</summary>
        /// <param name="clientCertificate">Agent certificate with private key, or null for public enrollment.</param>
        /// <returns>Client pinned to the ephemeral server certificate.</returns>
        public HttpClient CreateClient(X509Certificate2? clientCertificate = null)
        {
            HttpClientHandler handler = new()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                    certificate is not null &&
                    string.Equals(
                        NormaliseThumbprint(certificate.Thumbprint),
                        NormaliseThumbprint(serverCertificate.Thumbprint),
                        StringComparison.Ordinal),
            };
            if (clientCertificate is not null)
            {
                handler.ClientCertificates.Add(clientCertificate);
            }

            return new HttpClient(handler, disposeHandler: true) { BaseAddress = BaseAddress };
        }

        /// <summary>Creates a test-only human client without granting any production authentication bypass.</summary>
        /// <param name="subjectId">Bounded human subject resolved by the test authentication handler.</param>
        /// <returns>HTTPS client carrying only the local test subject header.</returns>
        public HttpClient CreateHumanClient(string subjectId)
        {
            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add(TestHumanAuthenticationHandler.SubjectHeader, subjectId);
            return client;
        }

        /// <summary>
        /// Rehydrates an ephemeral test certificate into a non-persistent user key container required by Windows
        /// Schannel. Disposing the returned certificate releases that temporary container.
        /// </summary>
        /// <param name="certificate">Ephemeral certificate with private key.</param>
        /// <returns>Equivalent test-only transport certificate.</returns>
        public static X509Certificate2 CreateTransportCertificate(X509Certificate2 certificate)
        {
            string password = Guid.NewGuid().ToString("N");
            byte[] pkcs12 = certificate.Export(X509ContentType.Pkcs12, password);
            try
            {
                return X509CertificateLoader.LoadPkcs12(
                    pkcs12,
                    password,
                    X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pkcs12);
            }
        }

        /// <summary>Confirms durable replay, revocation and isolation evidence after the HTTP flow.</summary>
        /// <param name="agentId">Enrolled Agent identifier.</param>
        /// <param name="expectedRevokedAt">Monotonic revocation instant returned by the API.</param>
        public async Task AssertDurableIsolationAsync(Guid agentId, DateTimeOffset? expectedRevokedAt)
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            RegisteredAgentRow agent = await context.Agents.SingleAsync(row => row.AgentId == agentId);
            AgentCertificateRow certificate = await context.AgentCertificates.SingleAsync(row => row.AgentId == agentId);
            AgentEnrollmentTokenRow token = await context.AgentEnrollmentTokens.SingleAsync(
                row => row.ConsumedByAgentId == agentId);
            AgentHeartbeatCursorRow cursor = await context.AgentHeartbeatCursors.SingleAsync(
                row => row.AgentId == agentId);
            AgentHeartbeatRow[] heartbeats = await context.AgentHeartbeats
                .Where(row => row.AgentId == agentId)
                .OrderBy(row => row.Sequence)
                .ToArrayAsync();

            Assert.Equal("Revoked", agent.State);
            Assert.Equal(expectedRevokedAt, agent.RevokedAt);
            Assert.Equal("Revoked", certificate.State);
            Assert.Equal(expectedRevokedAt, certificate.RevokedAt);
            Assert.Equal("sandbox-review-complete", certificate.RevocationReasonCode);
            Assert.Equal("Consumed", token.State);
            Assert.NotNull(token.ConsumedAt);
            Assert.Equal(3, cursor.HighestAcceptedSequence);
            Assert.Equal(new long[] { 1, 3 }, heartbeats.Select(row => row.Sequence));
            Assert.False(heartbeats[0].GapDetected);
            Assert.True(heartbeats[1].GapDetected);
            Assert.All(heartbeats, row => Assert.Equal(64, row.PayloadSha256?.Length));
            Assert.Equal(0, await context.AdministrativeCommands.CountAsync());
            Assert.Equal(0, await context.CommandAttempts.CountAsync());
            Assert.Equal(0, await context.HealthSamples.CountAsync());
            Assert.Equal(0, await context.Events.CountAsync());
            Assert.Equal(0, await context.OutboxMessages.CountAsync());
            Assert.Equal(0, await context.NotificationDeliveries.CountAsync());
            Assert.Contains(
                await context.AuditEntries.Select(row => row.Action).ToArrayAsync(),
                action => action == "agent.enroll");
            Assert.Contains(
                await context.AuditEntries.Select(row => row.Action).ToArrayAsync(),
                action => action == "agent.revoke");
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            await application.StopAsync();
            await application.DisposeAsync();
            await databaseKeeper.DisposeAsync();
            serverCertificate.Dispose();
            rootCertificate.Dispose();
            serverKey.Dispose();
            rootKey.Dispose();
        }

        /// <summary>Registers endpoint-routed public, certificate and test-human authentication.</summary>
        /// <param name="services">Sandbox service collection.</param>
        /// <param name="rootCertificate">Ephemeral custom trust root.</param>
        private static void AddAuthentication(IServiceCollection services, X509Certificate2 rootCertificate)
        {
            services
                .AddAuthentication(ApiSecurityDefaults.RoutedAuthenticationScheme)
                .AddPolicyScheme(
                    ApiSecurityDefaults.RoutedAuthenticationScheme,
                    ApiSecurityDefaults.RoutedAuthenticationScheme,
                    options => options.ForwardDefaultSelector = ApiSecurityDefaults.SelectAuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, NoIdentityAuthenticationHandler>(
                    ApiSecurityDefaults.PublicEndpointAuthenticationScheme,
                    _ => { })
                .AddScheme<AuthenticationSchemeOptions, TestHumanAuthenticationHandler>(
                    HumanAuthenticationDefaults.Scheme,
                    _ => { })
                .AddCertificate(options =>
                {
                    options.AllowedCertificateTypes = CertificateTypes.Chained;
                    options.ValidateCertificateUse = true;
                    options.ValidateValidityPeriod = true;
                    options.RevocationMode = X509RevocationMode.NoCheck;
                    options.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
                    options.CustomTrustStore.Add(rootCertificate);
                    options.Events = new CertificateAuthenticationEvents
                    {
                        OnCertificateValidated = async context =>
                        {
                            AgentCertificateIdentityValidator validator = context.HttpContext.RequestServices
                                .GetRequiredService<AgentCertificateIdentityValidator>();
                            Guid? agentId = await validator.ValidateAsync(
                                context.ClientCertificate,
                                context.HttpContext.RequestAborted);
                            if (agentId is null)
                            {
                                context.Fail("The sandbox Agent certificate is not active.");
                                return;
                            }

                            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
                                [new Claim(AgentIdentityClaimTypes.AgentId, agentId.Value.ToString("D"))],
                                context.Scheme.Name));
                            context.Success();
                        },
                    };
                });
        }

        /// <summary>Adds a bounded fixed-window rate policy without queueing requests.</summary>
        /// <param name="options">Rate limiter options.</param>
        /// <param name="policyName">Endpoint policy name.</param>
        private static void AddRatePolicy(RateLimiterOptions options, string policyName) =>
            options.AddPolicy(policyName, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "loopback",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));

        /// <summary>Creates the ephemeral P-256 root used only by this sandbox instance.</summary>
        /// <param name="rootKey">Ephemeral root key.</param>
        /// <param name="now">Current UTC instant.</param>
        /// <returns>Self-signed CA certificate with private key.</returns>
        private static X509Certificate2 CreateRootCertificate(ECDsa rootKey, DateTimeOffset now)
        {
            CertificateRequest request = new(
                "CN=DB-Notifier Agent Fleet Sandbox Root",
                rootKey,
                HashAlgorithmName.SHA256);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                true));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            return request.CreateSelfSigned(now.AddHours(-1), now.AddHours(4));
        }

        /// <summary>Creates the ephemeral P-256 HTTPS server certificate for loopback only.</summary>
        /// <param name="rootCertificate">Ephemeral signing root.</param>
        /// <param name="serverKey">Ephemeral server key.</param>
        /// <param name="now">Current UTC instant.</param>
        /// <returns>Loopback server certificate with private key.</returns>
        private static X509Certificate2 CreateServerCertificate(
            X509Certificate2 rootCertificate,
            ECDsa serverKey,
            DateTimeOffset now)
        {
            CertificateRequest request = new(
                "CN=localhost",
                serverKey,
                HashAlgorithmName.SHA256);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            OidCollection usages = new();
            usages.Add(new Oid("1.3.6.1.5.5.7.3.1"));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(usages, false));
            SubjectAlternativeNameBuilder names = new();
            names.AddDnsName("localhost");
            names.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(names.Build());
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            byte[] serial = RandomNumberGenerator.GetBytes(16);
            try
            {
                using X509Certificate2 publicCertificate = request.Create(
                    rootCertificate,
                    now.AddMinutes(-5),
                    now.AddHours(2),
                    serial);
                using X509Certificate2 ephemeralCertificate = publicCertificate.CopyWithPrivateKey(serverKey);
                return CreateTransportCertificate(ephemeralCertificate);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(serial);
            }
        }

        /// <summary>Builds a custom-root chain for one exact application-purpose OID.</summary>
        /// <param name="certificate">Certificate to validate.</param>
        /// <param name="rootCertificate">Only admitted trust root.</param>
        /// <param name="applicationPolicyOid">Required server- or client-auth OID.</param>
        /// <returns><see langword="true"/> only for a valid chain to the ephemeral root.</returns>
        private static bool BuildCustomChain(
            X509Certificate2 certificate,
            X509Certificate2 rootCertificate,
            string applicationPolicyOid)
        {
            using X509Chain chain = new();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(rootCertificate);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            chain.ChainPolicy.ApplicationPolicy.Add(new Oid(applicationPolicyOid));
            return chain.Build(certificate);
        }

        /// <summary>Normalises a framework thumbprint for exact sandbox pinning.</summary>
        /// <param name="thumbprint">Framework thumbprint.</param>
        /// <returns>Uppercase hexadecimal text without separators.</returns>
        public static string NormaliseThumbprint(string thumbprint) =>
            thumbprint
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace(":", string.Empty, StringComparison.Ordinal)
                .ToUpperInvariant();
    }

    /// <summary>Issues P-256 client certificates from verified PKCS#10 requests using one ephemeral local CA.</summary>
    /// <param name="rootCertificate">Ephemeral signing root owned by the sandbox.</param>
    private sealed class EphemeralAgentCertificateIssuer(X509Certificate2 rootCertificate) : IAgentCertificateIssuer
    {
        /// <inheritdoc />
        public ValueTask<AgentCertificateIssueResult> IssueAsync(
            AgentCertificateIssueRequest request,
            DateTimeOffset issuedAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] signingRequestBytes = request.CertificateSigningRequestDer.ToArray();
            try
            {
                byte[] digest = SHA256.HashData(signingRequestBytes);
                try
                {
                    if (!CryptographicOperations.FixedTimeEquals(
                            digest,
                            request.CertificateSigningRequestSha256.Span))
                    {
                        return ValueTask.FromResult(InvalidIssueResult());
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(digest);
                }

                CertificateRequest parsed = CertificateRequest.LoadSigningRequest(
                    signingRequestBytes,
                    HashAlgorithmName.SHA256,
                    CertificateRequestLoadOptions.Default);
                if (!IsNistP256(parsed.PublicKey))
                {
                    return ValueTask.FromResult(InvalidIssueResult());
                }

                CertificateRequest controlled = new(
                    new X500DistinguishedName("CN=DB-Notifier Sandbox Agent"),
                    parsed.PublicKey,
                    HashAlgorithmName.SHA256);
                controlled.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
                controlled.CertificateExtensions.Add(new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature,
                    true));
                OidCollection usages = new();
                usages.Add(new Oid("1.3.6.1.5.5.7.3.2"));
                controlled.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(usages, false));
                controlled.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(controlled.PublicKey, false));
                byte[] serial = RandomNumberGenerator.GetBytes(16);
                try
                {
                    using X509Certificate2 certificate = controlled.Create(
                        rootCertificate,
                        issuedAt.AddMinutes(-1),
                        issuedAt.AddHours(1),
                        serial);
                    byte[] certificateDer = certificate.Export(X509ContentType.Cert);
                    using ECDsa publicKey = certificate.GetECDsaPublicKey()
                        ?? throw new CryptographicException("Issued certificate did not expose ECDSA public material.");
                    string publicKeySha256 = Convert.ToHexString(SHA256.HashData(
                        publicKey.ExportSubjectPublicKeyInfo()));
                    return ValueTask.FromResult(new AgentCertificateIssueResult(
                        AgentCertificateIssueDisposition.Issued,
                        certificateDer,
                        certificate.Thumbprint
                            .Replace(" ", string.Empty, StringComparison.Ordinal)
                            .ToUpperInvariant(),
                        publicKeySha256,
                        request.CertificateSigningRequestSha256,
                        new DateTimeOffset(certificate.NotBefore.ToUniversalTime()),
                        new DateTimeOffset(certificate.NotAfter.ToUniversalTime())));
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(serial);
                }
            }
            catch (CryptographicException)
            {
                return ValueTask.FromResult(InvalidIssueResult());
            }
            finally
            {
                CryptographicOperations.ZeroMemory(signingRequestBytes);
            }
        }

        /// <summary>Checks the exact named-curve OIDs of an ECDSA P-256 public key.</summary>
        /// <param name="publicKey">CSR public key.</param>
        /// <returns><see langword="true"/> only for id-ecPublicKey with prime256v1 parameters.</returns>
        private static bool IsNistP256(PublicKey publicKey)
        {
            if (publicKey.Oid?.Value != "1.2.840.10045.2.1")
            {
                return false;
            }

            byte[]? encodedParameters = publicKey.EncodedParameters?.RawData;
            if (encodedParameters is null)
            {
                return false;
            }

            try
            {
                AsnReader reader = new(encodedParameters, AsnEncodingRules.DER);
                string curveOid = reader.ReadObjectIdentifier();
                return !reader.HasData && curveOid == "1.2.840.10045.3.1.7";
            }
            catch (AsnContentException)
            {
                return false;
            }
        }

        /// <summary>Creates a certificate-free fail-closed issuer result.</summary>
        /// <returns>Invalid-request result.</returns>
        private static AgentCertificateIssueResult InvalidIssueResult() =>
            new(
                AgentCertificateIssueDisposition.InvalidRequest,
                ReadOnlyMemory<byte>.Empty,
                null,
                null,
                ReadOnlyMemory<byte>.Empty,
                null,
                null);
    }

    /// <summary>Supplies the exact fixed server instant to the local test composition.</summary>
    /// <param name="now">UTC instant returned throughout the test.</param>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>Authenticates a bounded test subject header only inside the integration-test assembly.</summary>
    private sealed class TestHumanAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        /// <summary>Names the test-only subject header.</summary>
        public const string SubjectHeader = "DBN-Test-Subject";

        /// <summary>Initialises the test handler from ordinary framework services.</summary>
        /// <param name="options">Authentication options.</param>
        /// <param name="logger">Logger factory.</param>
        /// <param name="encoder">URL encoder.</param>
        public TestHumanAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        /// <inheritdoc />
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? subject = Request.Headers[SubjectHeader].SingleOrDefault();
            if (subject is null || subject.Length is < 3 or > 300 || subject != subject.Trim())
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            ClaimsPrincipal principal = new(new ClaimsIdentity(
                [new Claim("sub", subject)],
                Scheme.Name));
            AuthenticationTicket ticket = new(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>Returns no identity for public enrollment while preserving routed authentication semantics.</summary>
    private sealed class NoIdentityAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        /// <summary>Initialises the no-op handler from ordinary framework services.</summary>
        /// <param name="options">Authentication options.</param>
        /// <param name="logger">Logger factory.</param>
        /// <param name="encoder">URL encoder.</param>
        public NoIdentityAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        /// <inheritdoc />
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }
}
