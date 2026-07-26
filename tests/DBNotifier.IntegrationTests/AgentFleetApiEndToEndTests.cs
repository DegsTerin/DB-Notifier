// Module purpose: Exercises the production Agent Fleet routes over isolated loopback HTTPS with ephemeral identity material and SQLite state.
using System.Data.Common;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Globalization;
using System.IO.Pipes;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.RateLimiting;
using DBNotifier.Application.Access;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Application.Presentation;
using DBNotifier.Application.Synchronization;
using DBNotifier.Domain;
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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>
/// Verifies enrollment, mTLS heartbeat, read-only fleet data and immediate revocation through a disposable local
/// HTTPS host. The fixture creates no external connection, operational provider, command, worker or durable
/// certificate file; its observation scenario uses only a private deterministic synthetic provider.
/// </summary>
public sealed partial class AgentFleetApiEndToEndTests
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
        foreach (string unsupportedProtocol in new[] { "0", "2" })
        {
            using HttpRequestMessage incompatibleRequest = CreateAgentRequest(
                HttpMethod.Get,
                $"/api/v1/agents/{agentId:D}/assignments");
            incompatibleRequest.Headers.Remove("DBN-Protocol-Version");
            incompatibleRequest.Headers.Add("DBN-Protocol-Version", unsupportedProtocol);
            using HttpResponseMessage incompatibleResponse = await agentClient.SendAsync(incompatibleRequest);
            Assert.Equal(HttpStatusCode.UpgradeRequired, incompatibleResponse.StatusCode);
        }

        using (HttpRequestMessage missingSchemaRequest = CreateAgentRequest(
            HttpMethod.Get,
            $"/api/v1/agents/{agentId:D}/assignments"))
        {
            missingSchemaRequest.Headers.Remove("DBN-Message-Schema");
            using HttpResponseMessage missingSchemaResponse = await agentClient.SendAsync(missingSchemaRequest);
            Assert.Equal(HttpStatusCode.UpgradeRequired, missingSchemaResponse.StatusCode);
        }

        AgentHeartbeatRequest unknownSchemaHeartbeat = CreateHeartbeat(agentId, 1, sandbox.Now) with
        {
            SchemaVersion = AgentFleetProtocol.CurrentSchemaVersion + 1,
        };
        using (HttpResponseMessage unknownSchemaResponse = await SendAgentJsonAsync(
            agentClient,
            HttpMethod.Post,
            $"/api/v1/agents/{agentId:D}/heartbeats",
            unknownSchemaHeartbeat))
        {
            Assert.Equal(HttpStatusCode.UpgradeRequired, unknownSchemaResponse.StatusCode);
        }

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

        AgentHeartbeatRequest reorderedHeartbeat = CreateHeartbeat(agentId, 2, sandbox.Now) with
        {
            OccurredAt = sandbox.Now.AddSeconds(3),
            SentAt = sandbox.Now.AddSeconds(3),
            AgentTime = sandbox.Now.AddSeconds(3),
        };
        using HttpResponseMessage reorderedResponse = await SendAgentJsonAsync(
            agentClient,
            HttpMethod.Post,
            $"/api/v1/agents/{agentId:D}/heartbeats",
            reorderedHeartbeat);
        Assert.Equal(HttpStatusCode.Conflict, reorderedResponse.StatusCode);

        AgentHeartbeatRequest reusedMessageId = reorderedHeartbeat with
        {
            MessageId = firstHeartbeat.MessageId,
            Sequence = 4,
        };
        using HttpResponseMessage reusedMessageResponse = await SendAgentJsonAsync(
            agentClient,
            HttpMethod.Post,
            $"/api/v1/agents/{agentId:D}/heartbeats",
            reusedMessageId);
        Assert.Equal(HttpStatusCode.Conflict, reusedMessageResponse.StatusCode);

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
        Assert.Equal(AgentAssignmentValidationFixture.MonitoringReference, assignment.MonitoringCredentialReference);

        using (HttpRequestMessage weakEntityTagRequest = CreateAgentRequest(
            HttpMethod.Get,
            $"/api/v1/agents/{agentId:D}/assignments"))
        {
            weakEntityTagRequest.Headers.IfNoneMatch.Add(
                new EntityTagHeaderValue($"\"{assignments.Version}\"", isWeak: true));
            using HttpResponseMessage weakEntityTagResponse = await agentClient.SendAsync(weakEntityTagRequest);
            Assert.Equal(HttpStatusCode.BadRequest, weakEntityTagResponse.StatusCode);
        }

        using (HttpRequestMessage divergentConditionRequest = CreateAgentRequest(
            HttpMethod.Get,
            $"/api/v1/agents/{agentId:D}/assignments?afterVersion={assignments.Version}"))
        {
            divergentConditionRequest.Headers.IfNoneMatch.Add(
                new EntityTagHeaderValue($"\"{new string('F', 64)}\""));
            using HttpResponseMessage divergentConditionResponse = await agentClient.SendAsync(divergentConditionRequest);
            Assert.Equal(HttpStatusCode.BadRequest, divergentConditionResponse.StatusCode);
        }

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

        AgentFleetClientResult responseLost = await RunHeartbeatOnceAsync(
            coordinator,
            local.Store,
            sandbox.Now);
        Assert.False(responseLost.Succeeded);
        Assert.Equal(AgentLocalIdentityState.Offline, responseLost.State);
        PendingAgentHeartbeat pendingAfterLoss = await local.ReadPendingHeartbeatAsync(registration.AgentId);

        AgentFleetClientCoordinator restartedCoordinator = new(
            identities,
            lossyTransport,
            local.Store,
            new FixedTimeProvider(sandbox.Now),
            TimeSpan.FromMinutes(5));
        AgentFleetClientResult replayed = await RunHeartbeatOnceAsync(
            restartedCoordinator,
            local.Store,
            sandbox.Now);
        Assert.True(replayed.Succeeded);
        Assert.Equal("heartbeat.accepted", replayed.Code);
        Assert.Equal(1, pendingAfterLoss.Request.Sequence);

        AgentFleetClientResult nextHeartbeat = await RunHeartbeatOnceAsync(
            restartedCoordinator,
            local.Store,
            sandbox.Now);
        Assert.True(nextHeartbeat.Succeeded);
        Assert.Equal(3, lossyTransport.HeartbeatCalls);

        AgentFleetClientResult applied = await RunAssignmentsOnceAsync(
            restartedCoordinator,
            local.Store,
            sandbox.Now);
        Assert.True(applied.Succeeded);
        Assert.Equal("assignments.applied", applied.Code);
        AgentAssignmentLocalState firstState = await local.Store.GetAssignmentStateAsync(
            registration.AgentId,
            CancellationToken.None);
        Assert.True(AgentAssignmentVersion.IsUpperHexDigest(firstState.Version));
        Assert.Equal(1, await local.CountAssignmentsAsync());
        Assert.All(await local.ReadAssignmentsAsync(), row =>
            Assert.Null(row.AdministrativeCredentialReference));

        AgentFleetClientResult unchanged = await RunAssignmentsOnceAsync(
            restartedCoordinator,
            local.Store,
            sandbox.Now);
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
        AgentFleetClientResult rejected = await RunAssignmentsOnceAsync(
            tamperedCoordinator,
            local.Store,
            sandbox.Now);
        Assert.False(rejected.Succeeded);
        Assert.Equal(AgentLocalIdentityState.Active, rejected.State);
        Assert.Equal("assignments.digest_mismatch", rejected.Code);
        Assert.Equal(firstState.Version, (await local.Store.GetAssignmentStateAsync(
            registration.AgentId,
            CancellationToken.None)).Version);
        Assert.Equal(1, await local.CountAssignmentsAsync());

        AgentFleetClientResult replaced = await RunAssignmentsOnceAsync(
            restartedCoordinator,
            local.Store,
            sandbox.Now);
        Assert.True(replaced.Succeeded);
        Assert.Equal(2, await local.CountAssignmentsAsync());
        Assert.NotEqual(firstState.Version, (await local.Store.GetAssignmentStateAsync(
            registration.AgentId,
            CancellationToken.None)).Version);

        await sandbox.RevokeAgentAsync(registration.AgentId);
        AgentFleetClientResult revoked = await RunHeartbeatOnceAsync(
            restartedCoordinator,
            local.Store,
            sandbox.Now);
        Assert.False(revoked.Succeeded);
        Assert.Equal(AgentLocalIdentityState.RevokedOrDenied, revoked.State);
        AgentFleetClientResult quarantined = await RunAssignmentsOnceAsync(
            restartedCoordinator,
            local.Store,
            sandbox.Now);
        Assert.False(quarantined.Succeeded);
        Assert.Equal(AgentLocalIdentityState.RevokedOrDenied, quarantined.State);

        await local.AssertNoOperationalEffectsAsync();
        await sandbox.AssertClientIsolationAsync(registration.AgentId);
    }

    /// <summary>Proves controlled certificate expiry is persisted and blocks transport across coordinator restart.</summary>
    [Fact]
    public async Task AgentSideCertificateExpiryRemainsFailClosedAfterLogicalRestart()
    {
        await using AgentFleetSandbox sandbox = await AgentFleetSandbox.StartAsync();
        await using AgentLocalSandbox local = await AgentLocalSandbox.StartAsync();
        await using SandboxAgentIdentityStore identities = new(sandbox);
        string installationId = $"installation:{Guid.NewGuid():N}";
        AgentEnrollmentRequest tokenBinding = new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            installationId,
            "Sandbox Expiry Client",
            EnvironmentName,
            PlatformName,
            AgentVersion,
            ScopeName,
            sandbox.Now,
            sandbox.Now,
            "test-only-public-csr-placeholder");
        string token = await sandbox.SeedEnrollmentTokenAsync(tokenBinding);
        HttpAgentFleetClientTransport enrollmentTransport = new(
            sandbox.BaseAddress,
            AgentVersion,
            identities.GetClient);
        AgentFleetClientCoordinator enrollmentCoordinator = new(
            identities,
            enrollmentTransport,
            local.Store,
            new FixedTimeProvider(sandbox.Now),
            TimeSpan.FromMinutes(5));
        AgentFleetClientResult enrolled = await enrollmentCoordinator.EnrolForTestAsync(
            new AgentEnrollmentBootstrap(
                token,
                installationId,
                "Sandbox Expiry Client",
                EnvironmentName,
                PlatformName,
                AgentVersion,
                ScopeName),
            CancellationToken.None);
        Assert.True(enrolled.Succeeded);
        AgentLocalRegistration registration = Assert.IsType<AgentLocalRegistration>(
            await local.Store.GetRegistrationAsync(CancellationToken.None));
        FailIfCalledTransport blockedTransport = new();
        DateTimeOffset expiredAt = registration.CertificateNotAfter.AddTicks(1);
        AgentFleetClientCoordinator expiredCoordinator = new(
            identities,
            blockedTransport,
            local.Store,
            new FixedTimeProvider(expiredAt),
            TimeSpan.FromMinutes(5));

        AgentFleetClientResult expired = await RunHeartbeatOnceAsync(
            expiredCoordinator,
            local.Store,
            expiredAt);
        Assert.False(expired.Succeeded);
        Assert.Equal(AgentLocalIdentityState.Expired, expired.State);
        AgentFleetClientCoordinator restarted = new(
            identities,
            blockedTransport,
            local.Store,
            new FixedTimeProvider(expiredAt.AddMinutes(1)),
            TimeSpan.FromMinutes(5));
        AgentFleetClientResult stillExpired = await RunAssignmentsOnceAsync(
            restarted,
            local.Store,
            expiredAt.AddMinutes(1));
        Assert.False(stillExpired.Succeeded);
        Assert.Equal(AgentLocalIdentityState.Expired, stillExpired.State);
        Assert.Equal(0, blockedTransport.Calls);
        await local.AssertNoOperationalEffectsAsync();
    }

    /// <summary>
    /// Proves an assignment authorised and committed before revocation can finish delivery afterwards as historical
    /// evidence, while every request whose authorisation occurs after revocation is refused.
    /// </summary>
    [Fact]
    public async Task AssignmentResponseAndRevocationRaceHasExplicitAuthorisationSemantics()
    {
        PauseAfterCommitInterceptor pause = new();
        await using AgentFleetSandbox sandbox = await AgentFleetSandbox.StartAsync(pause);
        using ECDsa agentKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest signingRequest = new(
            "CN=DB-Notifier Sandbox Race Agent",
            agentKey,
            HashAlgorithmName.SHA256);
        byte[] certificateSigningRequest = signingRequest.CreateSigningRequest();
        string installationId = $"installation:{Guid.NewGuid():N}";
        AgentEnrollmentRequest enrollmentRequest = new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            installationId,
            "Sandbox Race Agent",
            EnvironmentName,
            PlatformName,
            AgentVersion,
            ScopeName,
            sandbox.Now,
            sandbox.Now,
            Convert.ToBase64String(certificateSigningRequest));
        try
        {
            string token = await sandbox.SeedEnrollmentTokenAsync(enrollmentRequest);
            using HttpClient enrollmentClient = sandbox.CreateClient();
            using HttpResponseMessage enrollmentResponse = await SendEnrollmentAsync(
                enrollmentClient,
                token,
                enrollmentRequest);
            AgentEnrollmentOutcome enrollment = await ReadRequiredJsonAsync<AgentEnrollmentOutcome>(
                enrollmentResponse);
            Assert.Equal(HttpStatusCode.OK, enrollmentResponse.StatusCode);
            Guid agentId = Assert.IsType<Guid>(enrollment.AgentId);
            using X509Certificate2 publicCertificate = X509CertificateLoader.LoadCertificate(
                enrollment.CertificateDer.Span);
            using X509Certificate2 combinedCertificate = publicCertificate.CopyWithPrivateKey(agentKey);
            using X509Certificate2 transportCertificate = AgentFleetSandbox.CreateTransportCertificate(
                combinedCertificate);
            await sandbox.SeedAssignmentsAndHumanAccessAsync(agentId);
            using HttpClient agentClient = sandbox.CreateClient(transportCertificate);
            using HttpClient humanClient = sandbox.CreateHumanClient(HumanSubject);
            using HttpRequestMessage assignmentRequest = CreateAgentRequest(
                HttpMethod.Get,
                $"/api/v1/agents/{agentId:D}/assignments");

            pause.Arm();
            Task<HttpResponseMessage> delayedAssignment = agentClient.SendAsync(assignmentRequest);
            await pause.WaitUntilPausedAsync(CancellationToken.None);
            using HttpResponseMessage revocationResponse = await humanClient.PostAsJsonAsync(
                $"/api/v1/agents/{agentId:D}:revoke",
                new AgentRevocationRequest("sandbox-race"),
                JsonOptions);
            AgentRevocationOutcome revocation = await ReadRequiredJsonAsync<AgentRevocationOutcome>(
                revocationResponse);
            Assert.Equal(AgentRevocationDisposition.Revoked, revocation.Disposition);
            pause.Release();

            using HttpResponseMessage historicalResponse = await delayedAssignment;
            AgentAssignmentSnapshot historical = await ReadRequiredJsonAsync<AgentAssignmentSnapshot>(
                historicalResponse);
            Assert.Equal(HttpStatusCode.OK, historicalResponse.StatusCode);
            Assert.Equal(agentId, historical.AgentId);
            using HttpRequestMessage afterRevocationRequest = CreateAgentRequest(
                HttpMethod.Get,
                $"/api/v1/agents/{agentId:D}/assignments");
            using HttpResponseMessage afterRevocationResponse = await agentClient.SendAsync(afterRevocationRequest);
            Assert.Contains(
                afterRevocationResponse.StatusCode,
                new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
            await sandbox.AssertRaceIsolationAsync(agentId, revocation.RevokedAt);
        }
        finally
        {
            pause.Release();
            CryptographicOperations.ZeroMemory(certificateSigningRequest);
        }
    }

    /// <summary>
    /// Proves exact heartbeat replay after a real child-process termination, expired-lease takeover and private
    /// identity transfer through bounded local IPC rather than arguments, environment variables or SQLite.
    /// </summary>
    [Fact]
    public async Task HeartbeatReplaySurvivesRealProcessTerminationUnderANewerFence()
    {
        await using AgentFleetSandbox sandbox = await AgentFleetSandbox.StartAsync();
        await using AgentFileSandbox local = await AgentFileSandbox.StartAsync();
        await using SandboxAgentIdentityStore identities = new(sandbox);
        string installationId = $"installation:{Guid.NewGuid():N}";
        AgentEnrollmentRequest tokenBinding = new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            installationId,
            "Multiprocess Sandbox Agent",
            EnvironmentName,
            PlatformName,
            AgentVersion,
            ScopeName,
            sandbox.Now,
            sandbox.Now,
            "test-only-public-csr-placeholder");
        string token = await sandbox.SeedEnrollmentTokenAsync(tokenBinding);
        HttpAgentFleetClientTransport transport = new(
            sandbox.BaseAddress,
            AgentVersion,
            identities.GetClient);
        AgentFleetClientCoordinator coordinator = new(
            identities,
            transport,
            local.Store,
            new FixedTimeProvider(sandbox.Now),
            TimeSpan.FromMinutes(5));
        AgentFleetClientResult enrollment = await coordinator.EnrolForTestAsync(
            new AgentEnrollmentBootstrap(
                token,
                installationId,
                "Multiprocess Sandbox Agent",
                EnvironmentName,
                PlatformName,
                AgentVersion,
                ScopeName),
            CancellationToken.None);
        Assert.True(enrollment.Succeeded);
        AgentLocalRegistration registration = Assert.IsType<AgentLocalRegistration>(
            await local.Store.GetRegistrationAsync(CancellationToken.None));
        (byte[] pkcs12, string password) = identities.ExportIdentityPackage(registration.IdentityReference);
        Process? interrupted = null;
        try
        {
            string marker = Path.Combine(local.RootPath, "server-accepted.marker");
            interrupted = await StartPausedSandboxHostAsync(
                local,
                sandbox,
                registration,
                pkcs12,
                password,
                sandbox.Now,
                marker);
            Assert.True(File.Exists(marker));
            Assert.False(interrupted.HasExited);
            int interruptedProcessId = interrupted.Id;
            interrupted.Kill(entireProcessTree: true);
            await interrupted.WaitForExitAsync();
            Assert.NotEqual(Environment.ProcessId, interruptedProcessId);
            PendingAgentHeartbeat pending = await local.ReadPendingHeartbeatAsync(registration.AgentId);
            Assert.Equal(1, pending.Request.Sequence);

            SandboxChildResult replayed = await RunSandboxHostAsync(
                local,
                sandbox,
                registration,
                pkcs12,
                password,
                sandbox.Now.AddSeconds(6));
            Assert.True(replayed.Succeeded);
            Assert.Equal("heartbeat.accepted", replayed.Code);
            Assert.Equal(1, replayed.Attempts);
            Assert.Equal(2, replayed.FenceToken);
            AgentFleetStateRow durable = await local.ReadFleetStateAsync(registration.AgentId);
            Assert.Null(durable.PendingHeartbeatPayloadJson);
            Assert.Equal(2, durable.NextHeartbeatSequence);
            Assert.Equal(3, durable.NextOperationFence);
            Assert.Null(durable.OperationLeaseOwner);
            await local.AssertNoOperationalEffectsAsync();
            await sandbox.AssertMultiprocessReplayIsolationAsync(registration.AgentId);
        }
        finally
        {
            if (interrupted is { HasExited: false })
            {
                interrupted.Kill(entireProcessTree: true);
                await interrupted.WaitForExitAsync();
            }

            interrupted?.Dispose();
            CryptographicOperations.ZeroMemory(pkcs12);
        }
    }

    /// <summary>
    /// Proves the authorised synthetic observation path across real Agent process restarts, Agent SQLite/outbox,
    /// HTTPS/mTLS ingestion and the read-only Dashboard TV projection without enabling an operational runtime.
    /// </summary>
    [Fact]
    public async Task SyntheticObservationPipelineSurvivesOfflineReplayReorderStalenessAndRevocation()
    {
        ObservationIngestionPause ingestionPause = new(targetSequence: 5);
        await using AgentFleetSandbox sandbox = await AgentFleetSandbox.StartAsync(
            enableObservationPipeline: true,
            observationPause: ingestionPause);
        await using AgentFileSandbox local = await AgentFileSandbox.StartAsync();
        await using SandboxAgentIdentityStore identities = new(sandbox);
        string installationId = $"installation:{Guid.NewGuid():N}";
        AgentEnrollmentRequest tokenBinding = new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            installationId,
            "Synthetic Observation Sandbox Agent",
            EnvironmentName,
            PlatformName,
            AgentVersion,
            ScopeName,
            sandbox.Now,
            sandbox.Now,
            "test-only-public-csr-placeholder");
        string token = await sandbox.SeedEnrollmentTokenAsync(tokenBinding);
        HttpAgentFleetClientTransport fleetTransport = new(
            sandbox.BaseAddress,
            AgentVersion,
            identities.GetClient);
        AgentFleetClientCoordinator coordinator = new(
            identities,
            fleetTransport,
            local.Store,
            new FixedTimeProvider(sandbox.Now),
            TimeSpan.FromMinutes(5));
        AgentFleetClientResult enrollment = await coordinator.EnrolForTestAsync(
            new AgentEnrollmentBootstrap(
                token,
                installationId,
                "Synthetic Observation Sandbox Agent",
                EnvironmentName,
                PlatformName,
                AgentVersion,
                ScopeName),
            CancellationToken.None);
        Assert.True(enrollment.Succeeded);
        AgentLocalRegistration registration = Assert.IsType<AgentLocalRegistration>(
            await local.Store.GetRegistrationAsync(CancellationToken.None));
        Guid instanceId = await sandbox.SeedAssignmentsAndHumanAccessAsync(
            registration.AgentId,
            omitMonitoringCredential: true);
        AgentFleetClientResult assignments = await RunAssignmentsOnceAsync(
            coordinator,
            local.Store,
            sandbox.Now);
        Assert.True(assignments.Succeeded);
        Assert.Equal("assignments.applied", assignments.Code);

        (byte[] pkcs12, string password) = identities.ExportIdentityPackage(registration.IdentityReference);
        try
        {
            Uri unavailableAddress = GetUnusedLoopbackHttpsAddress();
            ObservationSandboxChildResult offline = await RunObservationSandboxHostAsync(
                local,
                sandbox,
                registration,
                instanceId,
                pkcs12,
                password,
                unavailableAddress,
                sandbox.Now,
                nameof(HealthStatus.Degraded),
                "normal");
            Assert.Equal(new ObservationSandboxChildResult(1, 1, 0, 1), offline);

            sandbox.Advance(TimeSpan.FromSeconds(3));
            ObservationSandboxChildResult reconnected = await RunObservationSandboxHostAsync(
                local,
                sandbox,
                registration,
                instanceId,
                pkcs12,
                password,
                sandbox.BaseAddress,
                sandbox.Now,
                "none",
                "normal");
            Assert.Equal(new ObservationSandboxChildResult(0, 1, 1, 0), reconnected);

            using HttpClient dashboardClient = sandbox.CreateDashboardTvClient();
            (DashboardTvSnapshot firstSnapshot, string firstEntityTag) = await ReadDashboardSnapshotAsync(
                dashboardClient);
            DashboardTvInventoryItem firstItem = Assert.Single(firstSnapshot.Items);
            Assert.Equal(instanceId, firstItem.InstanceId);
            Assert.Equal("degraded", firstItem.Status);
            Assert.Equal("Synthetic sandbox evidence", firstItem.SupportLabel);

            sandbox.Advance(TimeSpan.FromSeconds(31));
            ObservationSandboxChildResult responseLost = await RunObservationSandboxHostAsync(
                local,
                sandbox,
                registration,
                instanceId,
                pkcs12,
                password,
                sandbox.BaseAddress,
                sandbox.Now,
                nameof(HealthStatus.Unavailable),
                "drop-accepted-response");
            Assert.Equal(new ObservationSandboxChildResult(1, 1, 0, 1), responseLost);
            (DashboardTvSnapshot afterLoss, string lossEntityTag) = await ReadDashboardSnapshotAsync(dashboardClient);
            Assert.Equal("unavailable", Assert.Single(afterLoss.Items).Status);
            Assert.NotEqual(firstEntityTag, lossEntityTag);

            sandbox.Advance(TimeSpan.FromSeconds(3));
            ObservationSandboxChildResult replayed = await RunObservationSandboxHostAsync(
                local,
                sandbox,
                registration,
                instanceId,
                pkcs12,
                password,
                sandbox.BaseAddress,
                sandbox.Now,
                "none",
                "normal");
            Assert.Equal(new ObservationSandboxChildResult(0, 1, 1, 0), replayed);

            sandbox.Advance(TimeSpan.FromSeconds(62));
            ObservationSandboxChildResult reordered = await RunObservationSandboxHostAsync(
                local,
                sandbox,
                registration,
                instanceId,
                pkcs12,
                password,
                sandbox.BaseAddress,
                sandbox.Now.AddSeconds(-31),
                $"{nameof(HealthStatus.Degraded)},{nameof(HealthStatus.Unavailable)}",
                "reverse-batch");
            Assert.Equal(new ObservationSandboxChildResult(2, 2, 2, 0), reordered);
            (DashboardTvSnapshot reorderedSnapshot, string reconciledEntityTag) = await ReadDashboardSnapshotAsync(
                dashboardClient);
            DashboardTvInventoryItem reconciledItem = Assert.Single(reorderedSnapshot.Items);
            Assert.Equal("unavailable", reconciledItem.Status);
            Assert.NotEqual(lossEntityTag, reconciledEntityTag);

            HttpClient agentClient = identities.GetClient(registration.IdentityReference);
            foreach ((string schema, string version) in new[]
            {
                ("2", AgentVersion),
                ("1", "invalid version"),
            })
            {
                using HttpRequestMessage incompatible = CreateObservationRequest(
                    registration.AgentId,
                    instanceId,
                    sandbox.Now,
                    schema,
                    version);
                using HttpResponseMessage incompatibleResponse = await agentClient.SendAsync(incompatible);
                Assert.Equal(HttpStatusCode.UpgradeRequired, incompatibleResponse.StatusCode);
            }

            using (HttpRequestMessage missingProtocol = CreateObservationRequest(
                registration.AgentId,
                instanceId,
                sandbox.Now,
                "1",
                AgentVersion))
            {
                missingProtocol.Headers.Remove("DBN-Protocol-Version");
                using HttpResponseMessage response = await agentClient.SendAsync(missingProtocol);
                Assert.Equal(HttpStatusCode.UpgradeRequired, response.StatusCode);
            }

            using (HttpRequestMessage duplicateProtocol = CreateObservationRequest(
                registration.AgentId,
                instanceId,
                sandbox.Now,
                "1",
                AgentVersion))
            {
                duplicateProtocol.Headers.Remove("DBN-Protocol-Version");
                duplicateProtocol.Headers.TryAddWithoutValidation("DBN-Protocol-Version", ["1", "1"]);
                using HttpResponseMessage response = await agentClient.SendAsync(duplicateProtocol);
                Assert.Equal(HttpStatusCode.UpgradeRequired, response.StatusCode);
            }

            sandbox.Advance(TimeSpan.FromMinutes(6));
            DateTimeOffset receivedAt = DateTimeOffset.ParseExact(
                reconciledItem.ReceivedAt,
                "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal);
            Assert.True(sandbox.Now - receivedAt >= TimeSpan.FromMinutes(5));
            await AssertDashboardNotModifiedAsync(dashboardClient, reconciledEntityTag);

            Task<ObservationSandboxChildResult> deniedAttempt = RunObservationSandboxHostAsync(
                local,
                sandbox,
                registration,
                instanceId,
                pkcs12,
                password,
                sandbox.BaseAddress,
                sandbox.Now,
                nameof(HealthStatus.Degraded),
                "normal");
            using CancellationTokenSource revocationTimeout = new(TimeSpan.FromSeconds(20));
            try
            {
                await ingestionPause.WaitUntilPausedAsync(revocationTimeout.Token);
                await sandbox.RevokeAgentAsync(registration.AgentId);
            }
            catch
            {
                ingestionPause.Release();
                await deniedAttempt;
                throw;
            }
            finally
            {
                ingestionPause.Release();
            }

            ObservationSandboxChildResult denied = await deniedAttempt;
            Assert.Equal(new ObservationSandboxChildResult(1, 1, 1, 0), denied);
            ObservationItemResult revocationResult = await ingestionPause.WaitForResultAsync(
                revocationTimeout.Token);
            Assert.Equal(ObservationIngestionDisposition.Rejected, revocationResult.Disposition);
            Assert.Equal("agent.not_active", revocationResult.ErrorCode);
            await AssertDashboardNotModifiedAsync(dashboardClient, reconciledEntityTag);

            await local.AssertSyntheticObservationOutboxAsync();
            await sandbox.AssertSyntheticObservationPipelineAsync(registration.AgentId, instanceId);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pkcs12);
        }
    }

    /// <summary>Verifies that an identity pipe closed mid-frame fails closed without disclosing received material.</summary>
    [Fact]
    public async Task SandboxHostFailsClosedWhenIdentityPipeClosesMidFrame()
    {
        await using AgentFleetSandbox sandbox = await AgentFleetSandbox.StartAsync();
        await using AgentFileSandbox local = await AgentFileSandbox.StartAsync();
        DateTimeOffset now = sandbox.Now;
        AgentLocalRegistration registration = new(
            Guid.NewGuid(),
            $"installation:{Guid.NewGuid():N}",
            EnvironmentName,
            $"sandbox-identity:{Guid.NewGuid():N}",
            new string('a', 64),
            now.AddHours(1),
            now,
            AgentLocalIdentityState.Active,
            null);
        string pipeName = $"dbnotifier-truncated-identity-{Guid.NewGuid():N}";
        using NamedPipeServerStream pipe = CreateIdentityPipe(pipeName);
        ProcessStartInfo startInfo = CreateSandboxHostStartInfo(
            local,
            sandbox,
            registration,
            pipeName,
            $"sandbox:truncated-frame:{Guid.NewGuid():N}",
            now,
            null);
        using Process process = Process.Start(startInfo) ??
            throw new InvalidOperationException("Sandbox host did not start.");
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
        const string secretCanary = "partial-pkcs12-secret-canary";
        Task sender = SendTruncatedIdentityPackageAsync(pipe, secretCanary, timeout.Token);
        Task<string> output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        Task<string> error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await Task.WhenAll(sender, process.WaitForExitAsync(timeout.Token));
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            throw;
        }

        string standardOutput = await output;
        string standardError = await error;
        Assert.Equal(2, process.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(standardOutput), standardOutput);
        Assert.StartsWith("sandbox_host.failed:identity_pipe:EndOfStreamException:", standardError.Trim());
        Assert.DoesNotContain(secretCanary, standardError, StringComparison.Ordinal);
    }

    /// <summary>
    /// Proves durable poll and acknowledgement replay across fresh child processes, bounded response loss,
    /// protocol rejection and revocation while every fixture remains structurally execution-ineligible.
    /// </summary>
    [Fact]
    public async Task CommandTransportReplaysAcrossProcessesAndCannotCreateAnExecutionAttempt()
    {
        PauseAfterCommitInterceptor commitPause = new();
        await using AgentFleetSandbox sandbox = await AgentFleetSandbox.StartAsync(
            commitPause,
            enableCommandTransport: true);
        await using AgentFileSandbox local = await AgentFileSandbox.StartCommandTransportAsync();
        await using SandboxAgentIdentityStore identities = new(sandbox);
        string installationId = $"installation:{Guid.NewGuid():N}";
        AgentEnrollmentRequest tokenBinding = new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            installationId,
            "Command Transport Sandbox Agent",
            EnvironmentName,
            PlatformName,
            AgentVersion,
            ScopeName,
            sandbox.Now,
            sandbox.Now,
            "test-only-public-csr-placeholder");
        string token = await sandbox.SeedEnrollmentTokenAsync(tokenBinding);
        HttpAgentFleetClientTransport enrollmentTransport = new(
            sandbox.BaseAddress,
            AgentVersion,
            identities.GetClient);
        AgentFleetClientCoordinator enrollmentCoordinator = new(
            identities,
            enrollmentTransport,
            local.Store,
            new FixedTimeProvider(sandbox.Now),
            TimeSpan.FromMinutes(5));
        AgentFleetClientResult enrollment = await enrollmentCoordinator.EnrolForTestAsync(
            new AgentEnrollmentBootstrap(
                token,
                installationId,
                "Command Transport Sandbox Agent",
                EnvironmentName,
                PlatformName,
                AgentVersion,
                ScopeName),
            CancellationToken.None);
        Assert.True(enrollment.Succeeded);
        AgentLocalRegistration registration = Assert.IsType<AgentLocalRegistration>(
            await local.Store.GetRegistrationAsync(CancellationToken.None));
        Guid instanceId = await sandbox.SeedAssignmentsAndHumanAccessAsync(registration.AgentId);
        CommandTransportFixtureIds fixtures = await sandbox.SeedCommandTransportFixturesAsync(
            registration.AgentId,
            instanceId);
        (byte[] pkcs12, string password) = identities.ExportIdentityPackage(registration.IdentityReference);
        try
        {
            using X509Certificate2 identity = X509CertificateLoader.LoadPkcs12(
                pkcs12,
                password,
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
            using HttpClient directClient = sandbox.CreateClient(identity);
            using HttpResponseMessage incompatible = await SendCommandTransportPollAsync(
                directClient,
                registration.AgentId,
                sandbox.Now,
                sequence: 1,
                schemaVersion: 1,
                maximumCount: 4);
            Assert.Equal(HttpStatusCode.UpgradeRequired, incompatible.StatusCode);

            using HttpResponseMessage gap = await SendCommandTransportPollAsync(
                directClient,
                registration.AgentId,
                sandbox.Now,
                sequence: 2,
                schemaVersion: CommandTransportProtocol.CurrentSchemaVersion,
                maximumCount: 4);
            Assert.Equal(HttpStatusCode.Conflict, gap.StatusCode);
            CommandTransportProblem gapProblem = await ReadRequiredJsonAsync<CommandTransportProblem>(gap);
            Assert.Equal("command.transport_sequence_gap", gapProblem.Code);
            Assert.Equal(1, gapProblem.ExpectedSequence);

            using HttpResponseMessage oversized = await SendOversizedCommandTransportRequestAsync(
                directClient,
                registration.AgentId);
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, oversized.StatusCode);

            using HttpResponseMessage wrongRoute = await SendCommandTransportPollAsync(
                directClient,
                Guid.NewGuid(),
                sandbox.Now,
                sequence: 1,
                schemaVersion: CommandTransportProtocol.CurrentSchemaVersion,
                maximumCount: 4);
            Assert.Equal(HttpStatusCode.Forbidden, wrongRoute.StatusCode);

            CommandTransportProcessResult lostPoll = await RunCommandTransportHostAsync(
                local,
                sandbox,
                registration,
                pkcs12,
                password,
                sandbox.Now,
                "lose-poll");
            Assert.Equal(3, lostPoll.ExitCode);
            Assert.Contains("command_transport_host.failed:cycle:IOException", lostPoll.StandardError);
            await local.AssertCommandTransportPendingAsync(
                registration.AgentId,
                CommandTransportMessageKind.Poll,
                sequence: 1,
                attemptCount: 3,
                expectedInboxCount: 0);
            await sandbox.AssertCommandTransportJournalAsync(registration.AgentId, 1, 1);

            CommandTransportProcessResult lostAcknowledgement = await RunCommandTransportHostAsync(
                local,
                sandbox,
                registration,
                pkcs12,
                password,
                sandbox.Now,
                "lose-ack");
            Assert.Equal(3, lostAcknowledgement.ExitCode);
            Assert.Contains("command_transport_host.failed:cycle:IOException", lostAcknowledgement.StandardError);
            await local.AssertCommandTransportPendingAsync(
                registration.AgentId,
                CommandTransportMessageKind.Acknowledgement,
                sequence: 2,
                attemptCount: 3,
                expectedInboxCount: 2);
            await sandbox.AssertCommandTransportJournalAsync(registration.AgentId, 2, 2);

            CommandTransportProcessResult replayed = await RunCommandTransportHostAsync(
                local,
                sandbox,
                registration,
                pkcs12,
                password,
                sandbox.Now,
                "none");
            Assert.Equal(0, replayed.ExitCode);
            CommandTransportChildResult completed = JsonSerializer.Deserialize<CommandTransportChildResult>(
                replayed.StandardOutput.Trim(),
                JsonOptions) ?? throw new InvalidOperationException("Command transport child returned no result.");
            Assert.True(completed.Succeeded);
            Assert.Equal(0, completed.Delivered);
            Assert.Equal(2, completed.Acknowledged);
            Assert.Equal(1, completed.Attempts);
            await local.AssertCommandTransportCompletedAsync(registration.AgentId, fixtures);

            commitPause.Arm();
            Task<HttpResponseMessage> committedPoll = SendCommandTransportPollAsync(
                directClient,
                registration.AgentId,
                sandbox.Now,
                sequence: 3,
                schemaVersion: CommandTransportProtocol.CurrentSchemaVersion,
                maximumCount: 4);
            await commitPause.WaitUntilPausedAsync(CancellationToken.None);
            await sandbox.RevokeAgentAsync(registration.AgentId);
            commitPause.Release();
            using HttpResponseMessage historicalPoll = await committedPoll;
            Assert.Equal(HttpStatusCode.OK, historicalPoll.StatusCode);
            CommandTransportPollResponse historical = await ReadRequiredJsonAsync<CommandTransportPollResponse>(
                historicalPoll);
            Assert.Equal(3, historical.Sequence);
            Assert.Empty(historical.Commands);

            CommandTransportProcessResult revoked = await RunCommandTransportHostAsync(
                local,
                sandbox,
                registration,
                pkcs12,
                password,
                sandbox.Now,
                "none");
            Assert.Equal(3, revoked.ExitCode);
            Assert.Contains("command_transport_host.failed:cycle:CommandTransportException", revoked.StandardError);
            await local.AssertCommandTransportPendingAsync(
                registration.AgentId,
                CommandTransportMessageKind.Poll,
                sequence: 3,
                attemptCount: 1,
                expectedInboxCount: 2);
            await sandbox.AssertCommandTransportFinalIsolationAsync(registration.AgentId, fixtures);
        }
        finally
        {
            commitPause.Release();
            CryptographicOperations.ZeroMemory(pkcs12);
        }
    }

    private static async Task<HttpResponseMessage> SendCommandTransportPollAsync(
        HttpClient client,
        Guid routeAgentId,
        DateTimeOffset now,
        long sequence,
        int schemaVersion,
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        CommandTransportPollRequest body = new(
            Guid.NewGuid(),
            schemaVersion,
            routeAgentId,
            sequence,
            now,
            now,
            AgentVersion,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["fixture-provider"] = "sandbox-provider-v1",
            },
            maximumCount);
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            $"/api/v2/sandbox/agents/{routeAgentId:D}/commands:poll")
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        AddCommandTransportHeaders(request, schemaVersion);
        return await client.SendAsync(request, cancellationToken);
    }

    private static async Task<HttpResponseMessage> SendOversizedCommandTransportRequestAsync(
        HttpClient client,
        Guid agentId)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            $"/api/v2/sandbox/agents/{agentId:D}/commands:poll")
        {
            Content = new StringContent(
                new string('x', CommandTransportProtocol.MaximumHttpBodyBytes + 1),
                Encoding.UTF8,
                "application/json"),
        };
        AddCommandTransportHeaders(request, CommandTransportProtocol.CurrentSchemaVersion);
        return await client.SendAsync(request);
    }

    private static void AddCommandTransportHeaders(HttpRequestMessage request, int schemaVersion)
    {
        string version = schemaVersion.ToString(CultureInfo.InvariantCulture);
        request.Headers.Add("DBN-Protocol-Version", version);
        request.Headers.Add("DBN-Message-Schema", version);
        request.Headers.Add("DBN-Agent-Version", AgentVersion);
    }

    private static async Task<CommandTransportProcessResult> RunCommandTransportHostAsync(
        AgentFileSandbox local,
        AgentFleetSandbox sandbox,
        AgentLocalRegistration registration,
        byte[] pkcs12,
        string password,
        DateTimeOffset now,
        string fault)
    {
        string pipeName = $"dbnotifier-command-transport-{Guid.NewGuid():N}";
        using NamedPipeServerStream pipe = CreateIdentityPipe(pipeName);
        ProcessStartInfo startInfo = new("dotnet")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = local.RootPath,
        };
        startInfo.ArgumentList.Add(ResolveSandboxHostPath());
        startInfo.ArgumentList.Add("--sandbox-command-transport");
        AddArgument(startInfo, "--sandbox-root", local.RootPath);
        AddArgument(startInfo, "--database", local.DatabasePath);
        AddArgument(startInfo, "--base-address", sandbox.BaseAddress.AbsoluteUri);
        AddArgument(startInfo, "--identity-pipe", pipeName);
        AddArgument(startInfo, "--owner", $"sandbox:command:{Guid.NewGuid():N}");
        AddArgument(startInfo, "--agent-id", registration.AgentId.ToString("D"));
        AddArgument(startInfo, "--agent-version", AgentVersion);
        AddArgument(startInfo, "--provider-id", "fixture-provider");
        AddArgument(startInfo, "--provider-version", "sandbox-provider-v1");
        AddArgument(startInfo, "--maximum-count", "4");
        AddArgument(startInfo, "--utc-now", now.ToString("O", CultureInfo.InvariantCulture));
        AddArgument(startInfo, "--fault", fault);
        Assert.DoesNotContain(startInfo.ArgumentList, value =>
            value.Contains("PRIVATE", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("password", StringComparison.OrdinalIgnoreCase));

        using Process process = Process.Start(startInfo) ??
            throw new InvalidOperationException("Command transport sandbox host did not start.");
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
        Task sender = SendIdentityPackageAsync(pipe, pkcs12, password, sandbox.ServerThumbprint, timeout.Token);
        Task<string> output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        Task<string> error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await Task.WhenAll(sender, process.WaitForExitAsync(timeout.Token));
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            throw;
        }

        return new(process.ExitCode, await output, await error);
    }

    /// <summary>Creates a deliberately incompatible observation request whose body would otherwise be canonical.</summary>
    private static HttpRequestMessage CreateObservationRequest(
        Guid agentId,
        Guid instanceId,
        DateTimeOffset observedAt,
        string messageSchema,
        string agentVersion)
    {
        ObservationSyncMessage message = new(
            Guid.NewGuid(),
            1,
            5,
            Guid.NewGuid(),
            instanceId,
            agentId,
            "fixture-provider",
            "sandbox-synthetic-v1",
            nameof(HealthStatus.Degraded),
            "sandbox-synthetic",
            nameof(EvidenceLevel.Synthetic),
            1,
            observedAt,
            13,
            null,
            null,
            ["synthetic-no-external-data"]);
        HttpRequestMessage request = new(
            HttpMethod.Post,
            $"/api/v1/agents/{agentId:D}/observations:batch")
        {
            Content = JsonContent.Create(new ObservationBatchRequest(agentId, [message]), options: JsonOptions),
        };
        request.Headers.Add("DBN-Protocol-Version", "1");
        request.Headers.Add("DBN-Message-Schema", messageSchema);
        request.Headers.Add("DBN-Agent-Version", agentVersion);
        return request;
    }

    /// <summary>Reads one authenticated snapshot and requires an exact strong ETag.</summary>
    /// <param name="client">Authenticated Dashboard sandbox client.</param>
    /// <param name="cancellationToken">Cancellation bounding the authoritative read.</param>
    /// <returns>Validated snapshot and strong entity tag.</returns>
    private static async Task<(DashboardTvSnapshot Snapshot, string EntityTag)> ReadDashboardSnapshotAsync(
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await client.GetAsync(
            DashboardTvSandboxEndpointRouteBuilderExtensions.SnapshotRoute,
            cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        DashboardTvSnapshot snapshot = await ReadRequiredJsonAsync<DashboardTvSnapshot>(response);
        string entityTag = response.Headers.ETag?.Tag ??
            throw new InvalidOperationException("Dashboard TV snapshot returned no strong ETag.");
        Assert.False(response.Headers.ETag!.IsWeak);
        return (snapshot, entityTag);
    }

    /// <summary>Requires one unchanged authoritative snapshot to return 304 without replacing the last body.</summary>
    private static async Task AssertDashboardNotModifiedAsync(HttpClient client, string entityTag)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            DashboardTvSandboxEndpointRouteBuilderExtensions.SnapshotRoute);
        request.Headers.TryAddWithoutValidation("If-None-Match", entityTag);
        using HttpResponseMessage response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength ?? 0);
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

    /// <summary>Runs one heartbeat attempt under a fresh sandbox-only local fence.</summary>
    /// <param name="coordinator">One-shot Agent Fleet coordinator.</param>
    /// <param name="store">Real Agent SQLite store.</param>
    /// <param name="now">Controlled sandbox instant.</param>
    /// <returns>The one-shot sanitised result.</returns>
    private static async Task<AgentFleetClientResult> RunHeartbeatOnceAsync(
        AgentFleetClientCoordinator coordinator,
        AgentFleetLocalStore store,
        DateTimeOffset now)
    {
        AgentFleetSandboxResilienceCoordinator resilience = CreateOneShotResilience(coordinator, store, now);
        AgentFleetSandboxResilienceResult outcome = await resilience.SendHeartbeatAsync(
            $"sandbox:e2e:{Guid.NewGuid():N}",
            AgentVersion,
            CancellationToken.None);
        Assert.Equal(1, outcome.Attempts);
        Assert.NotNull(outcome.FenceToken);
        return outcome.Result;
    }

    /// <summary>Runs one assignment reconciliation attempt under a fresh sandbox-only local fence.</summary>
    /// <param name="coordinator">One-shot Agent Fleet coordinator.</param>
    /// <param name="store">Real Agent SQLite store.</param>
    /// <param name="now">Controlled sandbox instant.</param>
    /// <returns>The one-shot sanitised result.</returns>
    private static async Task<AgentFleetClientResult> RunAssignmentsOnceAsync(
        AgentFleetClientCoordinator coordinator,
        AgentFleetLocalStore store,
        DateTimeOffset now)
    {
        AgentFleetSandboxResilienceCoordinator resilience = CreateOneShotResilience(coordinator, store, now);
        AgentFleetSandboxResilienceResult outcome = await resilience.ReconcileAssignmentsAsync(
            $"sandbox:e2e:{Guid.NewGuid():N}",
            AgentVersion,
            CancellationToken.None);
        Assert.Equal(1, outcome.Attempts);
        Assert.NotNull(outcome.FenceToken);
        return outcome.Result;
    }

    /// <summary>Creates a one-attempt resilience boundary for the pre-existing end-to-end flow.</summary>
    /// <param name="coordinator">One-shot Agent Fleet coordinator.</param>
    /// <param name="store">Real Agent SQLite store.</param>
    /// <param name="now">Controlled sandbox instant.</param>
    /// <returns>Sandbox resilience coordinator with no retry delay.</returns>
    private static AgentFleetSandboxResilienceCoordinator CreateOneShotResilience(
        AgentFleetClientCoordinator coordinator,
        AgentFleetLocalStore store,
        DateTimeOffset now) => new(
            coordinator,
            store,
            new FixedTimeProvider(now),
            new RejectingDelay(),
            new FixedJitter(0.5),
            new AgentFleetSandboxRetryPolicy(
                1,
                TimeSpan.Zero,
                TimeSpan.Zero,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(1),
                0));

    /// <summary>Starts a child that pauses only after the real Server accepted its heartbeat.</summary>
    /// <param name="local">File-backed Agent sandbox.</param>
    /// <param name="sandbox">Loopback HTTPS Server sandbox.</param>
    /// <param name="registration">Exact local registration.</param>
    /// <param name="pkcs12">Private test identity package transferred only through bounded local IPC.</param>
    /// <param name="password">Ephemeral package password transferred only through bounded local IPC.</param>
    /// <param name="now">Controlled child-process clock.</param>
    /// <param name="marker">Fixture-owned non-secret response marker.</param>
    /// <returns>Running child process paused before local acknowledgement.</returns>
    private static async Task<Process> StartPausedSandboxHostAsync(
        AgentFileSandbox local,
        AgentFleetSandbox sandbox,
        AgentLocalRegistration registration,
        byte[] pkcs12,
        string password,
        DateTimeOffset now,
        string marker)
    {
        string pipeName = $"dbnotifier-agent-fleet-{Guid.NewGuid():N}";
        using NamedPipeServerStream pipe = CreateIdentityPipe(pipeName);
        ProcessStartInfo startInfo = CreateSandboxHostStartInfo(
            local,
            sandbox,
            registration,
            pipeName,
            $"sandbox:process-a:{Guid.NewGuid():N}",
            now,
            marker);
        Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Sandbox host did not start.");
        try
        {
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
            Task sender = SendIdentityPackageAsync(
                pipe,
                pkcs12,
                password,
                sandbox.ServerThumbprint,
                timeout.Token);
            while (!File.Exists(marker))
            {
                if (process.HasExited)
                {
                    string error = await process.StandardError.ReadToEndAsync(timeout.Token);
                    throw new InvalidOperationException($"Sandbox host exited before the marker: {error}");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(25), timeout.Token);
            }

            await sender;
            return process;
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            process.Dispose();
            throw;
        }
    }

    /// <summary>Runs a second child to replay and durably acknowledge the exact pending heartbeat.</summary>
    /// <param name="local">File-backed Agent sandbox.</param>
    /// <param name="sandbox">Loopback HTTPS Server sandbox.</param>
    /// <param name="registration">Exact local registration.</param>
    /// <param name="pkcs12">Private test identity package transferred only through bounded local IPC.</param>
    /// <param name="password">Ephemeral package password transferred only through bounded local IPC.</param>
    /// <param name="now">Controlled instant after the interrupted lease expires.</param>
    /// <returns>Sanitised child result.</returns>
    private static async Task<SandboxChildResult> RunSandboxHostAsync(
        AgentFileSandbox local,
        AgentFleetSandbox sandbox,
        AgentLocalRegistration registration,
        byte[] pkcs12,
        string password,
        DateTimeOffset now)
    {
        string pipeName = $"dbnotifier-agent-fleet-{Guid.NewGuid():N}";
        using NamedPipeServerStream pipe = CreateIdentityPipe(pipeName);
        ProcessStartInfo startInfo = CreateSandboxHostStartInfo(
            local,
            sandbox,
            registration,
            pipeName,
            $"sandbox:process-b:{Guid.NewGuid():N}",
            now,
            null);
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Sandbox host did not start.");
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(20));
        Task sender = SendIdentityPackageAsync(pipe, pkcs12, password, sandbox.ServerThumbprint, timeout.Token);
        Task<string> output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        Task<string> error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await Task.WhenAll(sender, process.WaitForExitAsync(timeout.Token));
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            throw;
        }

        string standardOutput = await output;
        string standardError = await error;
        Assert.True(process.HasExited);
        Assert.Equal(0, process.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(standardError), standardError);
        return JsonSerializer.Deserialize<SandboxChildResult>(standardOutput.Trim(), JsonOptions) ??
            throw new InvalidOperationException("Sandbox host returned no typed result.");
    }

    /// <summary>Runs one synthetic observation attempt in a fresh child process using only private local IPC.</summary>
    private static async Task<ObservationSandboxChildResult> RunObservationSandboxHostAsync(
        AgentFileSandbox local,
        AgentFleetSandbox sandbox,
        AgentLocalRegistration registration,
        Guid instanceId,
        byte[] pkcs12,
        string password,
        Uri baseAddress,
        DateTimeOffset now,
        string statuses,
        string dispatchMode)
    {
        string pipeName = $"dbnotifier-observation-{Guid.NewGuid():N}";
        using NamedPipeServerStream pipe = CreateIdentityPipe(pipeName);
        ProcessStartInfo startInfo = CreateObservationSandboxHostStartInfo(
            local,
            registration,
            instanceId,
            pipeName,
            baseAddress,
            now,
            statuses,
            dispatchMode);
        using Process process = Process.Start(startInfo) ??
            throw new InvalidOperationException("Observation sandbox host did not start.");
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        Task sender = SendIdentityPackageAsync(
            pipe,
            pkcs12,
            password,
            sandbox.ServerThumbprint,
            timeout.Token);
        Task<string> output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        Task<string> error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await Task.WhenAll(sender, process.WaitForExitAsync(timeout.Token));
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            throw;
        }

        string standardOutput = await output;
        string standardError = await error;
        Assert.True(process.HasExited);
        Assert.Equal(0, process.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(standardError), standardError);
        return JsonSerializer.Deserialize<ObservationSandboxChildResult>(standardOutput.Trim(), JsonOptions) ??
            throw new InvalidOperationException("Observation sandbox host returned no typed result.");
    }

    /// <summary>Creates the strict non-secret argument set for one observation child process.</summary>
    private static ProcessStartInfo CreateObservationSandboxHostStartInfo(
        AgentFileSandbox local,
        AgentLocalRegistration registration,
        Guid instanceId,
        string pipeName,
        Uri baseAddress,
        DateTimeOffset now,
        string statuses,
        string dispatchMode)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = local.RootPath,
        };
        startInfo.ArgumentList.Add(ResolveSandboxHostPath());
        startInfo.ArgumentList.Add("--sandbox-observation-pipeline");
        AddArgument(startInfo, "--sandbox-root", local.RootPath);
        AddArgument(startInfo, "--database", local.DatabasePath);
        AddArgument(startInfo, "--base-address", baseAddress.AbsoluteUri);
        AddArgument(startInfo, "--identity-reference", registration.IdentityReference);
        AddArgument(startInfo, "--identity-pipe", pipeName);
        AddArgument(startInfo, "--agent-version", AgentVersion);
        AddArgument(startInfo, "--utc-now", now.ToString("O", CultureInfo.InvariantCulture));
        AddArgument(startInfo, "--agent-id", registration.AgentId.ToString("D"));
        AddArgument(startInfo, "--instance-id", instanceId.ToString("D"));
        AddArgument(startInfo, "--provider-type", "fixture-provider");
        AddArgument(startInfo, "--statuses", statuses);
        AddArgument(startInfo, "--dispatch-mode", dispatchMode);
        Assert.DoesNotContain(startInfo.ArgumentList, value =>
            value.Contains("PRIVATE", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("password", StringComparison.OrdinalIgnoreCase));
        return startInfo;
    }

    /// <summary>Reserves and releases one loopback port so a bounded attempt observes an unavailable local listener.</summary>
    private static Uri GetUnusedLoopbackHttpsAddress()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return new Uri($"https://127.0.0.1:{port}/", UriKind.Absolute);
    }

    private static ProcessStartInfo CreateSandboxHostStartInfo(
        AgentFileSandbox local,
        AgentFleetSandbox sandbox,
        AgentLocalRegistration registration,
        string pipeName,
        string owner,
        DateTimeOffset now,
        string? marker)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = local.RootPath,
        };
        startInfo.ArgumentList.Add(ResolveSandboxHostPath());
        startInfo.ArgumentList.Add("--sandbox-agent-fleet");
        AddArgument(startInfo, "--sandbox-root", local.RootPath);
        AddArgument(startInfo, "--database", local.DatabasePath);
        AddArgument(startInfo, "--base-address", sandbox.BaseAddress.AbsoluteUri);
        AddArgument(startInfo, "--identity-reference", registration.IdentityReference);
        AddArgument(startInfo, "--identity-pipe", pipeName);
        AddArgument(startInfo, "--owner", owner);
        AddArgument(startInfo, "--agent-version", AgentVersion);
        AddArgument(startInfo, "--utc-now", now.ToString("O", CultureInfo.InvariantCulture));
        if (marker is not null)
        {
            AddArgument(startInfo, "--pause-after-response-marker", marker);
        }

        Assert.DoesNotContain(startInfo.ArgumentList, value => value.Contains("PRIVATE", StringComparison.OrdinalIgnoreCase));
        return startInfo;
    }

    /// <summary>Resolves the built harness from its owning project instead of an incomplete test-output copy.</summary>
    /// <returns>Absolute path to the sandbox host assembly for the active build configuration.</returns>
    private static string ResolveSandboxHostPath()
    {
        DirectoryInfo? candidate = new(AppContext.BaseDirectory);
        while (candidate is not null && !File.Exists(Path.Combine(candidate.FullName, "DBNotifier.sln")))
        {
            candidate = candidate.Parent;
        }

        string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? string.Empty;
        if (candidate is null || string.IsNullOrWhiteSpace(configuration))
        {
            throw new FileNotFoundException("The sandbox host workspace or build configuration was not found.");
        }

        string hostPath = Path.Combine(
            candidate.FullName,
            "tests",
            "DBNotifier.AgentFleet.SandboxHost",
            "bin",
            configuration,
            "net10.0",
            "DBNotifier.AgentFleet.SandboxHost.dll");
        if (!File.Exists(hostPath))
        {
            throw new FileNotFoundException("The built sandbox host was not found.", hostPath);
        }

        return hostPath;
    }

    private static void AddArgument(ProcessStartInfo startInfo, string name, string value)
    {
        startInfo.ArgumentList.Add(name);
        startInfo.ArgumentList.Add(value);
    }

    private static NamedPipeServerStream CreateIdentityPipe(string pipeName) => new(
        pipeName,
        PipeDirection.Out,
        1,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

    private static async Task SendIdentityPackageAsync(
        NamedPipeServerStream pipe,
        byte[] pkcs12,
        string password,
        string serverThumbprint,
        CancellationToken cancellationToken)
    {
        await pipe.WaitForConnectionAsync(cancellationToken);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] thumbprintBytes = Encoding.ASCII.GetBytes(serverThumbprint);
        try
        {
            await WriteFrameAsync(pipe, passwordBytes, cancellationToken);
            await WriteFrameAsync(pipe, pkcs12, cancellationToken);
            await WriteFrameAsync(pipe, thumbprintBytes, cancellationToken);
            await pipe.FlushAsync(cancellationToken);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(thumbprintBytes);
        }
    }

    /// <summary>Sends one complete password frame and an intentionally incomplete PKCS#12 frame, then disconnects.</summary>
    /// <param name="pipe">Fixture-owned local pipe connected to the child process.</param>
    /// <param name="secretCanary">Non-operational marker used to prove sanitised child diagnostics.</param>
    /// <param name="cancellationToken">Token bounding the local IPC operation.</param>
    private static async Task SendTruncatedIdentityPackageAsync(
        NamedPipeServerStream pipe,
        string secretCanary,
        CancellationToken cancellationToken)
    {
        await pipe.WaitForConnectionAsync(cancellationToken);
        byte[] passwordBytes = Encoding.UTF8.GetBytes("test-password-material-1234");
        byte[] partialPkcs12 = Encoding.UTF8.GetBytes(secretCanary);
        byte[] declaredLength = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(partialPkcs12.Length + 8));
        try
        {
            await WriteFrameAsync(pipe, passwordBytes, cancellationToken);
            await pipe.WriteAsync(declaredLength, cancellationToken);
            await pipe.WriteAsync(partialPkcs12, cancellationToken);
            await pipe.FlushAsync(cancellationToken);
            pipe.Disconnect();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(partialPkcs12);
            CryptographicOperations.ZeroMemory(declaredLength);
        }
    }

    private static async Task WriteFrameAsync(
        Stream stream,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));
        await stream.WriteAsync(length, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
    }

    private sealed record SandboxChildResult(
        bool Succeeded,
        string State,
        string Code,
        int Attempts,
        long FenceToken);

    private sealed record ObservationSandboxChildResult(
        int PersistedCount,
        int PendingCount,
        int AcknowledgedCount,
        int RetryableCount);

    /// <summary>Contains bounded durable counts for the final-human-sample Agent replay evidence.</summary>
    private sealed record HumanLocalObservationEvidence(
        int ObservationCount,
        int PendingCount,
        int AcknowledgedCount);

    private sealed record CommandTransportChildResult(
        bool Succeeded,
        int Delivered,
        int Acknowledged,
        int Attempts,
        long Fence);

    private sealed record CommandTransportProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);

    private sealed record CommandTransportFixtureIds(
        Guid AcceptedCommandId,
        Guid UnsupportedCommandId,
        Guid ExpiredCommandId,
        Guid IncompatibleCommandId,
        Guid NonSandboxControlCommandId);

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
        T body,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage message = CreateAgentRequest(method, path);
        message.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(message, cancellationToken);
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

    /// <summary>Owns one fixture-scoped file SQLite database used only for real child-process restart evidence.</summary>
    private sealed class AgentFileSandbox : IAsyncDisposable
    {
        private readonly DbContextOptions<AgentDbContext> options;
        private bool disposed;

        private AgentFileSandbox(
            string rootPath,
            string databasePath,
            DbContextOptions<AgentDbContext> options,
            AgentFleetLocalStore store)
        {
            RootPath = rootPath;
            DatabasePath = databasePath;
            this.options = options;
            Store = store;
        }

        /// <summary>Gets the validated fixture-owned temporary root.</summary>
        public string RootPath { get; }

        /// <summary>Gets the exact Agent SQLite path shared only by fixture-owned child processes.</summary>
        public string DatabasePath { get; }

        /// <summary>Gets the real SQLite local store used by the parent test.</summary>
        public AgentFleetLocalStore Store { get; }

        /// <summary>Creates and migrates one isolated file store under the operating-system temporary directory.</summary>
        /// <returns>Disposable file-backed Agent sandbox.</returns>
        public static async Task<AgentFileSandbox> StartAsync()
        {
            return await StartAsync("dbnotifier-agent-fleet-sandbox-");
        }

        /// <summary>Creates a file store whose strict name is admitted by the command-transport child harness.</summary>
        /// <returns>Disposable file-backed command-transport sandbox.</returns>
        public static async Task<AgentFileSandbox> StartCommandTransportAsync()
        {
            return await StartAsync("dbnotifier-command-transport-sandbox-");
        }

        /// <summary>Creates the single file store shared by every phase of the consolidated STATE-06 harness.</summary>
        /// <param name="runId">Correlation identifier used to give cleanup exact ownership.</param>
        /// <returns>Disposable file-backed Agent sandbox with an exact consolidated-test ownership prefix.</returns>
        public static async Task<AgentFileSandbox> StartConsolidatedAsync(Guid runId)
        {
            if (runId == Guid.Empty)
            {
                throw new ArgumentException("The consolidated run identifier is required.", nameof(runId));
            }
            return await StartAsync($"dbnotifier-state06-consolidated-sandbox-{runId:N}-");
        }

        private static async Task<AgentFileSandbox> StartAsync(string prefix)
        {
            DirectoryInfo root = Directory.CreateTempSubdirectory(prefix);
            string rootPath = root.FullName;
            string databasePath = Path.Combine(rootPath, "agent.db");
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true,
                DefaultTimeout = 1,
            }.ToString();
            DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
                .UseSqlite(connectionString)
                .Options;
            AgentContextFactory factory = new(options);
            await using (AgentDbContext context = new(options))
            {
                await context.Database.MigrateAsync();
            }

            return new AgentFileSandbox(
                rootPath,
                databasePath,
                options,
                new AgentFleetLocalStore(factory, AgentAssignmentValidationFixture.Create()));
        }

        /// <summary>Reads the exact durable pending heartbeat after the interrupted child exits.</summary>
        /// <param name="agentId">Exact local Agent registration.</param>
        /// <returns>Typed pending heartbeat.</returns>
        public async Task<PendingAgentHeartbeat> ReadPendingHeartbeatAsync(Guid agentId)
        {
            AgentFleetStateRow row = await ReadFleetStateAsync(agentId);
            AgentHeartbeatRequest request = JsonSerializer.Deserialize<AgentHeartbeatRequest>(
                row.PendingHeartbeatPayloadJson ?? throw new InvalidOperationException("No pending heartbeat exists."),
                JsonOptions) ?? throw new InvalidOperationException("Pending heartbeat could not be read.");
            return new PendingAgentHeartbeat(request, false);
        }

        /// <summary>Reads exact local lease and replay evidence after a child-process boundary.</summary>
        /// <param name="agentId">Exact local Agent registration.</param>
        /// <returns>Untracked Agent Fleet state.</returns>
        public async Task<AgentFleetStateRow> ReadFleetStateAsync(Guid agentId)
        {
            await using AgentDbContext context = new(options);
            return await context.AgentFleetStates.AsNoTracking()
                .SingleAsync(row => row.AgentId == agentId);
        }

        /// <summary>Confirms exact durable pending identity after a child process loses all bounded responses.</summary>
        public async Task AssertCommandTransportPendingAsync(
            Guid agentId,
            CommandTransportMessageKind kind,
            long sequence,
            int attemptCount,
            int expectedInboxCount)
        {
            await using AgentCommandTransportSandboxDbContext context = CreateCommandTransportContext();
            AgentCommandTransportSandboxStateRow state = await context.TransportStates
                .AsNoTracking()
                .SingleAsync(row => row.AgentId == agentId);
            Assert.Equal(kind.ToString(), state.PendingMessageKind);
            Assert.Equal(sequence, state.PendingSequence);
            Assert.NotEqual(Guid.Empty, state.PendingMessageId);
            Assert.False(string.IsNullOrWhiteSpace(state.PendingPayloadJson));
            Assert.Equal(
                CommandTransportCodec.ComputeSha256(state.PendingPayloadJson),
                state.PendingPayloadSha256);
            Assert.Equal(attemptCount, state.PendingAttemptCount);
            Assert.Null(state.LeaseOwner);
            Assert.Null(state.LeaseFence);
            Assert.Null(state.LeaseExpiresAt);
            Assert.Equal(expectedInboxCount, await context.Receipts.CountAsync());
            Assert.All(await context.Receipts.AsNoTracking().ToArrayAsync(), row =>
                Assert.Equal(CommandExecutionPolicy.Never.ToString(), row.ExecutionPolicy));
            await AssertNormalCommandInboxEmptyAsync();
        }

        /// <summary>Confirms terminal receipt-only inbox state after the exact acknowledgement replay succeeds.</summary>
        public async Task AssertCommandTransportCompletedAsync(
            Guid agentId,
            CommandTransportFixtureIds fixtures)
        {
            await using AgentCommandTransportSandboxDbContext context = CreateCommandTransportContext();
            AgentCommandTransportSandboxStateRow state = await context.TransportStates
                .AsNoTracking()
                .SingleAsync(row => row.AgentId == agentId);
            Assert.Equal(3, state.NextSequence);
            Assert.Null(state.PendingMessageId);
            Assert.Null(state.PendingSequence);
            Assert.Null(state.PendingMessageKind);
            Assert.Null(state.PendingPayloadJson);
            Assert.Null(state.PendingPayloadSha256);
            Assert.Equal(0, state.PendingAttemptCount);
            AgentCommandTransportSandboxReceiptRow accepted = await context.Receipts.AsNoTracking()
                .SingleAsync(row => row.CommandId == fixtures.AcceptedCommandId);
            AgentCommandTransportSandboxReceiptRow unsupported = await context.Receipts.AsNoTracking()
                .SingleAsync(row => row.CommandId == fixtures.UnsupportedCommandId);
            Assert.Equal("ReceiptAcknowledged", accepted.State);
            Assert.NotNull(accepted.AcknowledgedAt);
            Assert.Equal("ReceiptUnsupported", unsupported.State);
            Assert.Null(unsupported.AcknowledgedAt);
            Assert.All(new[] { accepted, unsupported }, row =>
            {
                Assert.StartsWith(CommandTransportProtocol.SyntheticCapabilityPrefix, row.CapabilityId);
                Assert.Equal(CommandExecutionPolicy.Never.ToString(), row.ExecutionPolicy);
            });
            await AssertNormalCommandInboxEmptyAsync();
        }

        /// <summary>Opens the dedicated receipt-only SQLite store created by the exact command sandbox marker.</summary>
        /// <returns>A caller-owned context that cannot address the normal Agent inbox.</returns>
        private AgentCommandTransportSandboxDbContext CreateCommandTransportContext()
        {
            string path = Path.Combine(RootPath, "command-transport-receipts.sqlite");
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWrite,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true,
                Pooling = false,
                DefaultTimeout = 1,
            }.ToString();
            DbContextOptions<AgentCommandTransportSandboxDbContext> sandboxOptions =
                new DbContextOptionsBuilder<AgentCommandTransportSandboxDbContext>()
                    .UseSqlite(connectionString)
                    .Options;
            return new AgentCommandTransportSandboxDbContext(sandboxOptions);
        }

        /// <summary>Proves the normal Agent database received neither command rows nor sandbox transport state.</summary>
        /// <returns>A task that completes after both normal tables are confirmed empty.</returns>
        private async Task AssertNormalCommandInboxEmptyAsync()
        {
            await using AgentDbContext context = new(options);
            Assert.Empty(await context.InboxCommands.AsNoTracking().ToArrayAsync());
            Assert.Empty(await context.CommandTransportStates.AsNoTracking().ToArrayAsync());
        }

        /// <summary>Proves that multiprocess heartbeat replay activated no operational data path.</summary>
        public async Task AssertNoOperationalEffectsAsync()
        {
            await using AgentDbContext context = new(options);
            Assert.Equal(0, await context.HealthObservations.CountAsync());
            Assert.Equal(0, await context.OutboxMessages.CountAsync());
            Assert.Equal(0, await context.InboxCommands.CountAsync());
            Assert.Equal(0, await context.InstanceAssignments.CountAsync());
        }

        /// <summary>Confirms the exact durable synthetic observations and terminal local outbox state across restarts.</summary>
        public async Task AssertSyntheticObservationOutboxAsync()
        {
            await using AgentDbContext context = new(options);
            AgentHealthObservationRow[] observations = await context.HealthObservations
                .AsNoTracking()
                .ToArrayAsync();
            AgentOutboxMessageRow[] messages = await context.OutboxMessages
                .AsNoTracking()
                .OrderBy(row => row.Sequence)
                .ToArrayAsync();
            Assert.Equal(5, observations.Length);
            Assert.Equal(new long[] { 1, 2, 3, 4, 5 }, messages.Select(row => row.Sequence));
            Assert.Collection(
                messages,
                row => Assert.Equal(2, row.AttemptCount),
                row => Assert.Equal(2, row.AttemptCount),
                row => Assert.Equal(1, row.AttemptCount),
                row => Assert.Equal(1, row.AttemptCount),
                row => Assert.Equal(1, row.AttemptCount));
            Assert.All(messages, row =>
            {
                Assert.NotNull(row.AcknowledgedAt);
                Assert.DoesNotContain("sandbox.invalid", row.PayloadJson, StringComparison.Ordinal);
                Assert.DoesNotContain("credential", row.PayloadJson, StringComparison.OrdinalIgnoreCase);
            });
            Assert.All(observations, row =>
            {
                Assert.Equal(nameof(EvidenceLevel.Synthetic), row.EvidenceLevel);
                Assert.Equal("fixture-provider", row.ProviderType);
            });
            Assert.Equal(0, await context.InboxCommands.CountAsync());
        }

        /// <summary>Reads only aggregate synthetic observation counts for the human-remediation evidence surface.</summary>
        /// <returns>Total local observations and exact pending/acknowledged outbox counts.</returns>
        public async Task<HumanLocalObservationEvidence> ReadHumanObservationEvidenceAsync()
        {
            await using AgentDbContext context = new(options);
            int observations = await context.HealthObservations.CountAsync();
            int pending = await context.OutboxMessages.CountAsync(row => row.AcknowledgedAt == null);
            int acknowledged = await context.OutboxMessages.CountAsync(row => row.AcknowledgedAt != null);
            return new HumanLocalObservationEvidence(observations, pending, acknowledged);
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            if (disposed)
            {
                return ValueTask.CompletedTask;
            }

            disposed = true;
            string fullRoot = Path.GetFullPath(RootPath);
            string tempRoot = Path.GetFullPath(Path.GetTempPath());
            string relative = Path.GetRelativePath(tempRoot, fullRoot);
            string rootName = Path.GetFileName(fullRoot);
            if (Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal) ||
                !(rootName.StartsWith("dbnotifier-agent-fleet-sandbox-", StringComparison.Ordinal) ||
                    rootName.StartsWith("dbnotifier-command-transport-sandbox-", StringComparison.Ordinal) ||
                    rootName.StartsWith("dbnotifier-state06-consolidated-sandbox-", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("The sandbox root cannot be safely removed.");
            }

            if (Directory.Exists(fullRoot))
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(fullRoot, recursive: true);
            }

            return ValueTask.CompletedTask;
        }

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
            return new AgentLocalSandbox(
                keeper,
                options,
                new AgentFleetLocalStore(factory, AgentAssignmentValidationFixture.Create()));
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

    /// <summary>
    /// Owns parent P-256 identity material, transfers child copies only through fixture IPC and disposes every
    /// client and certificate container.
    /// </summary>
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

        /// <summary>Exports one encrypted test package for immediate transfer through the fixture-owned IPC pipe.</summary>
        /// <param name="identityReference">Exact completed E2E identity reference.</param>
        /// <returns>PKCS#12 bytes and an ephemeral password returned only for bounded test-harness IPC transfer.</returns>
        public (byte[] Pkcs12, string Password) ExportIdentityPackage(string identityReference)
        {
            if (!completed.TryGetValue(identityReference, out var identity))
            {
                throw new InvalidOperationException("The sandbox identity reference is unavailable.");
            }

            string password = $"sandbox-{Guid.NewGuid():N}";
            return (identity.Certificate.Export(X509ContentType.Pkcs12, password), password);
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

    /// <summary>Fails if an expired or quarantined coordinator reaches any transport method.</summary>
    private sealed class FailIfCalledTransport : IAgentFleetClientTransport
    {
        /// <summary>Gets the number of forbidden calls observed.</summary>
        public int Calls { get; private set; }

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentEnrollmentOutcome>> EnrolAsync(
            string token,
            AgentEnrollmentRequest request,
            CancellationToken cancellationToken) => Reject<AgentEnrollmentOutcome>();

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentHeartbeatOutcome>> SendHeartbeatAsync(
            string identityReference,
            AgentHeartbeatRequest request,
            CancellationToken cancellationToken) => Reject<AgentHeartbeatOutcome>();

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentAssignmentSnapshot>> GetAssignmentsAsync(
            string identityReference,
            Guid agentId,
            string agentVersion,
            string? currentVersion,
            CancellationToken cancellationToken) => Reject<AgentAssignmentSnapshot>();

        private ValueTask<AgentFleetTransportResult<T>> Reject<T>()
        {
            Calls++;
            throw new InvalidOperationException("Expired identity reached transport.");
        }
    }

    /// <summary>Pauses one armed transaction after durable commit so revocation timing is deterministic.</summary>
    private sealed class PauseAfterCommitInterceptor : DbTransactionInterceptor
    {
        private readonly TaskCompletionSource paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int armed;

        /// <summary>Arms the next committed transaction as the exact assignment authorisation point.</summary>
        public void Arm() => Interlocked.Exchange(ref armed, 1);

        /// <summary>Waits until the armed transaction has committed but has not returned to the store.</summary>
        /// <param name="cancellationToken">Bounded test cancellation.</param>
        /// <returns>A task that completes at the post-commit boundary.</returns>
        public Task WaitUntilPausedAsync(CancellationToken cancellationToken) =>
            paused.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

        /// <summary>Releases the paused assignment path; repeated release is harmless.</summary>
        public void Release() => released.TrySetResult();

        /// <inheritdoc />
        public override async Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref armed, 0) == 1)
            {
                paused.TrySetResult();
                await released.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            await base.TransactionCommittedAsync(transaction, eventData, cancellationToken).ConfigureAwait(false);
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
        private readonly AdjustableTimeProvider clock;
        private bool disposed;

        /// <summary>Initialises a fully composed but already started local sandbox.</summary>
        /// <param name="databaseKeeper">Connection keeping the named in-memory database alive.</param>
        /// <param name="rootKey">Ephemeral root private key.</param>
        /// <param name="serverKey">Ephemeral server private key.</param>
        /// <param name="rootCertificate">Ephemeral custom trust root.</param>
        /// <param name="serverCertificate">Ephemeral loopback server certificate.</param>
        /// <param name="application">Started local Web application.</param>
        /// <param name="baseAddress">Bound loopback HTTPS address that requests the ephemeral Agent certificate.</param>
        /// <param name="browserBaseAddress">Bound loopback HTTPS address that never requests a browser-held certificate.</param>
        /// <param name="clock">Adjustable fixture clock that remains fixed unless the owning test advances it.</param>
        private AgentFleetSandbox(
            SqliteConnection databaseKeeper,
            ECDsa rootKey,
            ECDsa serverKey,
            X509Certificate2 rootCertificate,
            X509Certificate2 serverCertificate,
            WebApplication application,
            Uri baseAddress,
            Uri browserBaseAddress,
            AdjustableTimeProvider clock)
        {
            this.databaseKeeper = databaseKeeper;
            this.rootKey = rootKey;
            this.serverKey = serverKey;
            this.rootCertificate = rootCertificate;
            this.serverCertificate = serverCertificate;
            this.application = application;
            this.clock = clock;
            BaseAddress = baseAddress;
            BrowserBaseAddress = browserBaseAddress;
        }

        /// <summary>Gets the exact HTTPS loopback address selected by Kestrel.</summary>
        public Uri BaseAddress { get; }

        /// <summary>
        /// Gets the dedicated human/browser HTTPS loopback address. It differs only in the consolidated sandbox so
        /// Chromium is never prompted for an Agent certificate; endpoint policies still deny Agent routes without mTLS.
        /// </summary>
        public Uri BrowserBaseAddress { get; }

        /// <summary>Gets the current controlled UTC clock used by the Application service.</summary>
        public DateTimeOffset Now => clock.GetUtcNow();

        /// <summary>Gets the same adjustable test clock consumed by correlated readers.</summary>
        public TimeProvider SandboxTimeProvider => clock;

        /// <summary>Gets the sandbox-only service provider for the consolidated evidence coordinator.</summary>
        public IServiceProvider Services => application.Services;

        /// <summary>Gets the public loopback server thumbprint sent through bounded local IPC for exact pinning.</summary>
        public string ServerThumbprint => NormaliseThumbprint(serverCertificate.Thumbprint);

        /// <summary>Gets the public SPKI pin used only by the dedicated Chromium process.</summary>
        public string ServerSubjectPublicKeyInfoPin
        {
            get
            {
                using ECDsa publicKey = serverCertificate.GetECDsaPublicKey() ??
                    throw new CryptographicException("The sandbox server certificate has no ECDSA public key.");
                return Convert.ToBase64String(SHA256.HashData(publicKey.ExportSubjectPublicKeyInfo()));
            }
        }

        /// <summary>Waits until the exact temporary host receives its authenticated shutdown request.</summary>
        /// <param name="cancellationToken">Cancellation bounding the local harness lifetime.</param>
        /// <returns>A task completing after the application begins shutdown.</returns>
        public Task WaitForShutdownAsync(CancellationToken cancellationToken) =>
            application.WaitForShutdownAsync(cancellationToken);

        /// <summary>Creates and starts the loopback-only sandbox.</summary>
        /// <param name="interceptor">Optional fixture-owned coordination interceptor; never registered operationally.</param>
        /// <param name="enableObservationPipeline">Whether to compose the explicitly guarded synthetic observation and TV routes.</param>
        /// <param name="observationPause">Optional E2E-only barrier before one selected observation enters persistence.</param>
        /// <param name="enableCommandTransport">Whether to compose the execution-ineligible command transport routes.</param>
        /// <param name="consolidatedHarness">Optional owner of the explicitly gated consolidated test controls.</param>
        /// <param name="dashboardRoot">Built Dashboard root served only by the consolidated loopback host.</param>
        /// <param name="enableRevocationMatrixHints">Whether the revocation matrix composes the in-process hint publisher.</param>
        /// <param name="snapshotResponseBarrier">Optional test-only response barrier for deterministic snapshot overlap.</param>
        /// <returns>Started disposable sandbox.</returns>
        public static async Task<AgentFleetSandbox> StartAsync(
            IInterceptor? interceptor = null,
            bool enableObservationPipeline = false,
            ObservationIngestionPause? observationPause = null,
            bool enableCommandTransport = false,
            ConsolidatedHarnessState? consolidatedHarness = null,
            string? dashboardRoot = null,
            bool enableRevocationMatrixHints = false,
            ConsolidatedSnapshotResponseBarrier? snapshotResponseBarrier = null)
        {
            if (observationPause is not null && !enableObservationPipeline)
            {
                throw new ArgumentException(
                    "An observation pause requires the explicitly enabled observation pipeline.",
                    nameof(observationPause));
            }

            if (consolidatedHarness is not null &&
                (!enableObservationPipeline || !enableCommandTransport ||
                    string.IsNullOrWhiteSpace(dashboardRoot) ||
                    !Directory.Exists(dashboardRoot) ||
                    !File.Exists(Path.Combine(dashboardRoot, "index.html"))))
            {
                throw new ArgumentException(
                    "The consolidated harness requires both protocol paths and one built Dashboard root.",
                    nameof(consolidatedHarness));
            }

            if ((enableRevocationMatrixHints || snapshotResponseBarrier is not null) && !enableObservationPipeline)
            {
                throw new ArgumentException(
                    "Revocation-matrix Dashboard controls require the explicitly enabled observation pipeline.",
                    nameof(enableObservationPipeline));
            }

            DateTimeOffset now = consolidatedHarness is null
                ? DateTimeOffset.UtcNow
                : DateTimeOffset.UtcNow.AddMinutes(-1);
            AdjustableTimeProvider clock = new(now);
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
                EnvironmentName = enableObservationPipeline
                    ? DashboardTvSandboxEndpointRouteBuilderExtensions.EnvironmentName
                    : "IntegrationTests",
            });
            if (enableObservationPipeline)
            {
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{DashboardTvSandboxEndpointRouteBuilderExtensions.ConfigurationSection}:Enabled"] = "true",
                    [$"{DashboardTvChangeHintSandboxEndpointRouteBuilderExtensions.ConfigurationSection}:Enabled"] =
                        consolidatedHarness is not null || enableRevocationMatrixHints ? "true" : "false",
                    [$"{ReconciledLocalNotificationSandboxEndpointExtensions.ConfigurationSection}:Enabled"] =
                        consolidatedHarness is null ? "false" : "true",
                });
            }
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
                if (consolidatedHarness is not null)
                {
                    options.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.UseHttps(
                        new HttpsConnectionAdapterOptions
                        {
                            ServerCertificate = serverCertificate,
                            ClientCertificateMode = ClientCertificateMode.NoCertificate,
                        }));
                }
            });
            builder.Services.AddProblemDetails();
            builder.Services.AddDbContextFactory<ServerDbContext>(options =>
            {
                options.UseSqlite(connectionString);
                if (interceptor is not null)
                {
                    options.AddInterceptors(interceptor);
                }
            });
            builder.Services.AddSingleton<TimeProvider>(clock);
            builder.Services.AddSingleton<IAgentCertificateIssuer>(
                new EphemeralAgentCertificateIssuer(rootCertificate));
            builder.Services.AddSingleton<IAgentAssignmentValidator>(_ => AgentAssignmentValidationFixture.Create());
            builder.Services.AddScoped<IAgentFleetStore, AgentFleetStore>();
            builder.Services.AddScoped<AgentFleetService>();
            builder.Services.AddScoped<AgentCertificateIdentityValidator>();
            if (enableCommandTransport)
            {
                builder.Services.AddScoped<ICommandTransportServerStore, ServerCommandTransportSandboxStore>();
            }

            builder.Services.AddSingleton<HumanActorResolver>();
            builder.Services.AddSingleton<IAuthorizationHandler, AgentRouteAuthorizationHandler>();
            bool dashboardTvEnabled = false;
            bool dashboardHintsEnabled = false;
            bool notificationsEnabled = false;
            if (enableObservationPipeline)
            {
                builder.Services.AddScoped<ServerObservationIngestionStore>();
                builder.Services.AddScoped<IObservationIngestionStore>(services =>
                {
                    ServerObservationIngestionStore inner = services
                        .GetRequiredService<ServerObservationIngestionStore>();
                    return observationPause is null
                        ? inner
                        : new PausingObservationIngestionStore(inner, observationPause);
                });
                builder.Services.AddScoped<ObservationBatchIngestor>();
                dashboardTvEnabled = builder.Services.AddDashboardTvSandbox(
                    builder.Environment,
                    builder.Configuration);
                builder.Services.RemoveAll<IDashboardTvSnapshotSource>();
                builder.Services.AddSingleton<DashboardTvSyntheticObservationSnapshotSource>();
                if (consolidatedHarness?.HumanRemediationMode == true)
                {
                    builder.Services.AddSingleton<IDashboardTvSnapshotSource>(services =>
                        new ConsolidatedHumanSampleSnapshotSource(
                            services.GetRequiredService<DashboardTvSyntheticObservationSnapshotSource>(),
                            consolidatedHarness));
                }
                else
                {
                    builder.Services.AddSingleton<IDashboardTvSnapshotSource>(services =>
                        services.GetRequiredService<DashboardTvSyntheticObservationSnapshotSource>());
                }
                if (consolidatedHarness is not null || enableRevocationMatrixHints)
                {
                    dashboardHintsEnabled = builder.Services.AddDashboardTvChangeHintSandbox(
                        builder.Environment,
                        builder.Configuration);
                }
                if (consolidatedHarness is not null)
                {
                    notificationsEnabled = builder.Services.AddReconciledLocalNotificationSandbox(
                        builder.Environment,
                        builder.Configuration);
                    builder.Services.AddSingleton(consolidatedHarness);
                }
            }

            AddAuthentication(builder.Services, rootCertificate);
            Microsoft.AspNetCore.Authorization.AuthorizationBuilder authorisation = builder.Services
                .AddAuthorizationBuilder()
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
            if (enableObservationPipeline)
            {
                authorisation.AddPolicy(ApiSecurityDefaults.AgentObservationIngestionPolicy, policy =>
                {
                    policy.AddAuthenticationSchemes(CertificateAuthenticationDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new AgentRouteRequirement());
                });
            }
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                AddRatePolicy(options, AgentFleetEndpointRouteBuilderExtensions.EnrollmentRateLimitPolicy);
                AddRatePolicy(options, "AgentApiRateLimit");
                AddRatePolicy(options, "HumanApiRateLimit");
                if (consolidatedHarness is not null)
                {
                    AddConsolidatedHarnessRatePolicy(options, consolidatedHarness.RunId);
                }
            });

            WebApplication application = builder.Build();
            application.UseRouting();
            application.UseMiddleware<ProtectedTransportMiddleware>();
            application.UseAuthentication();
            if (consolidatedHarness?.HumanRemediationMode == true)
            {
                application.Use((context, next) =>
                    consolidatedHarness.RecordHumanEvidenceRequestAsync(context, next));
            }
            application.UseRateLimiter();
            application.UseAuthorization();
            if (snapshotResponseBarrier is not null)
            {
                application.Use((context, next) => snapshotResponseBarrier.InvokeAsync(context, next));
            }
            if (consolidatedHarness is not null)
            {
                PhysicalFileProvider dashboardFiles = new(Path.GetFullPath(dashboardRoot!));
                application.Lifetime.ApplicationStopped.Register(dashboardFiles.Dispose);
                application.UseDefaultFiles(new DefaultFilesOptions { FileProvider = dashboardFiles });
                application.UseStaticFiles(new StaticFileOptions { FileProvider = dashboardFiles });
                application.Use((context, next) => consolidatedHarness.RecordSnapshotRequestAsync(context, next));
            }
            application.MapAgentFleetEndpoints();
            if (enableCommandTransport)
            {
                application.MapCommandTransportSafetySandboxEndpoints();
            }

            if (enableObservationPipeline)
            {
                application.MapObservationIngestionEndpoint();
                application.MapDashboardTvSandboxEndpoint(dashboardTvEnabled);
                if (consolidatedHarness is not null || enableRevocationMatrixHints)
                {
                    application.MapDashboardTvChangeHintSandbox(dashboardHintsEnabled);
                }
                if (consolidatedHarness is not null)
                {
                    application.MapReconciledLocalNotificationSandbox(notificationsEnabled);
                    MapConsolidatedHarnessEndpoints(application, consolidatedHarness);
                    application.MapFallback(async context =>
                    {
                        if (!HttpMethods.IsGet(context.Request.Method) &&
                            !HttpMethods.IsHead(context.Request.Method))
                        {
                            context.Response.StatusCode = StatusCodes.Status404NotFound;
                            return;
                        }

                        context.Response.ContentType = "text/html; charset=utf-8";
                        await context.Response.SendFileAsync(
                            Path.Combine(Path.GetFullPath(dashboardRoot!), "index.html"),
                            context.RequestAborted);
                    });
                }
            }

            try
            {
                await using (AsyncServiceScope scope = application.Services.CreateAsyncScope())
                {
                    ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
                    await context.Database.EnsureCreatedAsync();
                }

                await application.StartAsync();
                IServer server = application.Services.GetRequiredService<IServer>();
                Uri[] addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses
                    .Select(value => new Uri(value))
                    .ToArray() ?? [];
                int expectedAddressCount = consolidatedHarness is null ? 1 : 2;
                if (addresses.Length != expectedAddressCount || addresses.Any(address =>
                    address.Scheme != Uri.UriSchemeHttps ||
                    !IPAddress.TryParse(address.Host, out IPAddress? addressValue) ||
                    !IPAddress.IsLoopback(addressValue)))
                {
                    throw new InvalidOperationException("The integration host was not bound exclusively to HTTPS loopback.");
                }

                Uri baseAddress = addresses[0];
                Uri browserBaseAddress = consolidatedHarness is null ? baseAddress : addresses[1];

                return new AgentFleetSandbox(
                    databaseKeeper,
                    rootKey,
                    serverKey,
                    rootCertificate,
                    serverCertificate,
                    application,
                    baseAddress,
                    browserBaseAddress,
                    clock);
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
        /// <param name="omitMonitoringCredential">Whether the observation E2E requires a credential-free synthetic assignment.</param>
        public async Task<Guid> SeedAssignmentsAndHumanAccessAsync(
            Guid agentId,
            bool omitMonitoringCredential = false)
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            Guid userId = Guid.NewGuid();
            Guid roleId = Guid.NewGuid();
            Guid readPermissionId = Guid.NewGuid();
            Guid revokePermissionId = Guid.NewGuid();
            DateTimeOffset now = Now;
            Guid activeInstanceId = Guid.NewGuid();
            context.Instances.AddRange(
                new DatabaseInstanceRow
                {
                    InstanceId = activeInstanceId,
                    DisplayName = "Sandbox Assignment",
                    ProviderType = "fixture-provider",
                    Environment = EnvironmentName,
                    EndpointJson = "{\"host\":\"sandbox.invalid\",\"port\":5432}",
                    MonitoringCredentialReference = omitMonitoringCredential
                        ? null
                        : AgentAssignmentValidationFixture.MonitoringReference,
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
            return activeInstanceId;
        }

        /// <summary>Seeds only synthetic non-executable command fixtures plus explicit negative controls.</summary>
        public async Task<CommandTransportFixtureIds> SeedCommandTransportFixturesAsync(
            Guid agentId,
            Guid instanceId)
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            Guid userId = await context.Users.Select(row => row.UserId).SingleAsync();
            DateTimeOffset now = Now;
            CommandTransportFixtureIds ids = new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid());
            context.AdministrativeCommands.AddRange(
                CreateCommand(ids.AcceptedCommandId, "sandbox.command.receipt.v1", "sandbox-provider-v1", now.AddMinutes(-5), now.AddMinutes(5)),
                CreateCommand(ids.UnsupportedCommandId, "sandbox.command.unsupported.v1", "sandbox-provider-v1", now.AddMinutes(-4), now.AddMinutes(5)),
                CreateCommand(ids.ExpiredCommandId, "sandbox.command.expired.v1", "sandbox-provider-v1", now.AddMinutes(-3), now.AddMinutes(-1)),
                CreateCommand(ids.IncompatibleCommandId, "sandbox.command.incompatible.v1", "other-provider-version", now.AddMinutes(-2), now.AddMinutes(5)),
                CreateCommand(ids.NonSandboxControlCommandId, "blocked.namespace.fixture.v1", "sandbox-provider-v1", now.AddMinutes(-1), now.AddMinutes(5)));
            await context.SaveChangesAsync();
            return ids;

            AdministrativeCommandRow CreateCommand(
                Guid commandId,
                string capabilityId,
                string providerVersion,
                DateTimeOffset requestedAt,
                DateTimeOffset expiresAt) => new()
                {
                    CommandId = commandId,
                    IdempotencyKey = $"sandbox-command-{commandId:N}",
                    InstanceId = instanceId,
                    AssignedAgentId = agentId,
                    CapabilityId = capabilityId,
                    TypedParametersJson = "{\"fixture\":\"receipt-only\"}",
                    RequestedByUserId = userId,
                    RequestedAt = requestedAt,
                    Reason = "Synthetic command-transport safety fixture; never executable.",
                    ExpiresAt = expiresAt,
                    AuthorizationSnapshotReference = "sandbox:test-only:non-authoritative",
                    ExpectedAgentVersion = AgentVersion,
                    ExpectedProviderVersion = providerVersion,
                    State = "Pending",
                    ConcurrencyToken = Guid.NewGuid(),
                };
        }

        /// <summary>Confirms exact server replay cardinality after one or more client-side response losses.</summary>
        public async Task AssertCommandTransportJournalAsync(Guid agentId, long sequence, int count)
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            ServerCommandTransportCursorRow cursor = await context.CommandTransportCursors.AsNoTracking()
                .SingleAsync(row => row.AgentId == agentId);
            Assert.Equal(sequence, cursor.HighestAcceptedSequence);
            Assert.Equal(count, await context.CommandTransportJournal.CountAsync(row => row.AgentId == agentId));
            Assert.Equal(0, await context.CommandAttempts.CountAsync());
        }

        /// <summary>Confirms revocation advanced no stream state and every fixture remained free of execution effects.</summary>
        public async Task AssertCommandTransportFinalIsolationAsync(
            Guid agentId,
            CommandTransportFixtureIds fixtures)
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            RegisteredAgentRow agent = await context.Agents.AsNoTracking()
                .SingleAsync(row => row.AgentId == agentId);
            ServerCommandTransportCursorRow cursor = await context.CommandTransportCursors.AsNoTracking()
                .SingleAsync(row => row.AgentId == agentId);
            ServerCommandTransportJournalRow[] journal = await context.CommandTransportJournal.AsNoTracking()
                .Where(row => row.AgentId == agentId)
                .OrderBy(row => row.Sequence)
                .ToArrayAsync();
            Assert.Equal("Revoked", agent.State);
            Assert.Equal(3, cursor.HighestAcceptedSequence);
            Assert.Equal(new long[] { 1, 2, 3 }, journal.Select(row => row.Sequence));
            Assert.Equal(
                new[]
                {
                    CommandTransportProtocol.PollMessageType,
                    CommandTransportProtocol.AcknowledgementMessageType,
                    CommandTransportProtocol.PollMessageType,
                },
                journal.Select(row => row.MessageType));
            Assert.Equal(3, journal.Select(row => row.MessageId).Distinct().Count());
            Assert.Equal(3, journal.Select(row => row.ResponseMessageId).Distinct().Count());
            Assert.All(journal, row => Assert.Equal(64, row.RequestPayloadSha256.Length));

            Dictionary<Guid, string> states = await context.AdministrativeCommands.AsNoTracking()
                .Where(row => row.AssignedAgentId == agentId)
                .ToDictionaryAsync(row => row.CommandId, row => row.State);
            Assert.Equal("Acknowledged", states[fixtures.AcceptedCommandId]);
            Assert.Equal("Unsupported", states[fixtures.UnsupportedCommandId]);
            Assert.Equal("Expired", states[fixtures.ExpiredCommandId]);
            Assert.Equal("Pending", states[fixtures.IncompatibleCommandId]);
            Assert.Equal("Pending", states[fixtures.NonSandboxControlCommandId]);
            Assert.Equal(0, await context.CommandAttempts.CountAsync());
            Assert.Equal(0, await context.HealthSamples.CountAsync());
            Assert.Equal(0, await context.Events.CountAsync());
            Assert.Equal(0, await context.OutboxMessages.CountAsync());
            Assert.Equal(0, await context.NotificationDeliveries.CountAsync());
        }

        /// <summary>Advances the controlled application clock without sleeping or changing any persisted state.</summary>
        /// <param name="duration">Positive bounded duration used by one deterministic E2E scenario.</param>
        public void Advance(TimeSpan duration) => clock.Advance(duration);

        /// <summary>Creates the dedicated read-only Dashboard TV sandbox client with its bounded test subject.</summary>
        /// <returns>HTTPS client pinned to the ephemeral server identity.</returns>
        public HttpClient CreateDashboardTvClient()
        {
            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add(
                DashboardTvSandboxEndpointRouteBuilderExtensions.TestSubjectHeader,
                "sandbox-tv-reviewer");
            return client;
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
                    MonitoringCredentialReference = AgentAssignmentValidationFixture.MonitoringReference,
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

        /// <summary>Revokes one Agent through the real human-authorised endpoint and returns bounded HTTP evidence.</summary>
        /// <param name="agentId">Agent to revoke.</param>
        /// <param name="cancellationToken">Cancellation bounding the local HTTPS request.</param>
        /// <returns>Typed status and revocation disposition without response headers or identity material.</returns>
        public async Task<AgentRevocationHttpEvidence> RevokeAgentAsync(
            Guid agentId,
            CancellationToken cancellationToken = default)
        {
            AgentRevocationHttpEvidence evidence = await RevokeAgentForDiagnosticAsync(
                agentId,
                cancellationToken);
            Assert.Equal(HttpStatusCode.OK, evidence.StatusCode);
            Assert.Equal(
                AgentRevocationDisposition.Revoked,
                Assert.IsType<AgentRevocationOutcome>(evidence.Outcome).Disposition);
            return evidence;
        }

        /// <summary>Attempts one revocation while preserving a non-success HTTP status for sanitised diagnosis.</summary>
        /// <param name="agentId">Agent to revoke.</param>
        /// <param name="cancellationToken">Cancellation bounding the local HTTPS request.</param>
        /// <returns>Status plus an outcome only when the endpoint returned its successful typed contract.</returns>
        public async Task<AgentRevocationHttpEvidence> RevokeAgentForDiagnosticAsync(
            Guid agentId,
            CancellationToken cancellationToken = default)
        {
            using HttpClient human = CreateHumanClient(HumanSubject);
            using HttpResponseMessage response = await human.PostAsJsonAsync(
                $"/api/v1/agents/{agentId:D}:revoke",
                new AgentRevocationRequest("sandbox-client-review-complete"),
                JsonOptions,
                cancellationToken);
            AgentRevocationOutcome? outcome = response.StatusCode == HttpStatusCode.OK
                ? await ReadRequiredJsonAsync<AgentRevocationOutcome>(response)
                : null;
            return new AgentRevocationHttpEvidence(response.StatusCode, outcome);
        }

        /// <summary>Reads the atomically committed Agent and certificate revocation state from Server SQLite.</summary>
        /// <param name="agentId">Synthetic Agent whose exact central state is required.</param>
        /// <param name="cancellationToken">Cancellation bounding the durable inspection.</param>
        /// <returns>Bounded canonical states and certificate counts.</returns>
        public async Task<ConsolidatedCentralRevocationEvidence> ReadCentralRevocationEvidenceAsync(
            Guid agentId,
            CancellationToken cancellationToken)
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            RegisteredAgentRow agent = await context.Agents.AsNoTracking()
                .SingleAsync(row => row.AgentId == agentId, cancellationToken);
            string[] certificateStates = await context.AgentCertificates.AsNoTracking()
                .Where(row => row.AgentId == agentId)
                .OrderBy(row => row.AgentCertificateId)
                .Select(row => row.State)
                .ToArrayAsync(cancellationToken);
            return new ConsolidatedCentralRevocationEvidence(
                agent.State,
                agent.RevokedAt,
                certificateStates.Length,
                certificateStates.Count(state => state == "Revoked"));
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

        /// <summary>Confirms one server-side heartbeat effect after two child processes replayed the same envelope.</summary>
        /// <param name="agentId">Exact enrolled Agent identifier.</param>
        public async Task AssertMultiprocessReplayIsolationAsync(Guid agentId)
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            AgentHeartbeatCursorRow cursor = await context.AgentHeartbeatCursors.SingleAsync(
                row => row.AgentId == agentId);
            AgentHeartbeatRow[] heartbeats = await context.AgentHeartbeats
                .Where(row => row.AgentId == agentId)
                .OrderBy(row => row.Sequence)
                .ToArrayAsync();
            Assert.Equal(1, cursor.HighestAcceptedSequence);
            AgentHeartbeatRow heartbeat = Assert.Single(heartbeats);
            Assert.Equal(1, heartbeat.Sequence);
            Assert.Equal(0, await context.AdministrativeCommands.CountAsync());
            Assert.Equal(0, await context.CommandAttempts.CountAsync());
            Assert.Equal(0, await context.HealthSamples.CountAsync());
            Assert.Equal(0, await context.Events.CountAsync());
            Assert.Equal(0, await context.OutboxMessages.CountAsync());
            Assert.Equal(0, await context.NotificationDeliveries.CountAsync());
        }

        /// <summary>
        /// Confirms contiguous stream resolution, retained accepted provenance and zero effects from the rejected fifth item.
        /// </summary>
        /// <param name="agentId">Revoked Agent whose first four observations were accepted and fifth sequence was consumed.</param>
        /// <param name="instanceId">Sole synthetic assignment projected to Dashboard TV.</param>
        public async Task AssertSyntheticObservationPipelineAsync(Guid agentId, Guid instanceId)
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            RegisteredAgentRow agent = await context.Agents.SingleAsync(row => row.AgentId == agentId);
            AgentObservationCursorRow cursor = await context.AgentObservationCursors
                .SingleAsync(row => row.AgentId == agentId);
            HealthSampleRow[] samples = await context.HealthSamples
                .AsNoTracking()
                .Where(row => row.AgentId == agentId)
                .OrderBy(row => row.Sequence)
                .ToArrayAsync();
            InstanceObservationStateRow state = await context.InstanceObservationStates
                .AsNoTracking()
                .SingleAsync(row => row.InstanceId == instanceId);

            Assert.Equal("Revoked", agent.State);
            Assert.Equal(5, cursor.HighestContiguousSequence);
            Assert.Equal(new long[] { 1, 2, 3, 4 }, samples.Select(row => row.Sequence));
            Assert.All(samples, sample =>
            {
                Assert.Equal(nameof(EvidenceLevel.Synthetic), sample.EvidenceLevel);
                Assert.Equal("fixture-provider", sample.ProviderType);
                Assert.Equal(instanceId, sample.InstanceId);
                Assert.Equal(64, sample.PayloadHash?.Length);
            });
            Assert.Equal(4, state.LastProcessedSequence);
            Assert.Equal(nameof(HealthStatus.Unavailable), state.Status);
            Assert.Equal(samples[^1].ObservationId, state.ObservationId);
            Assert.Equal(4, await context.Events.CountAsync());
            Assert.Equal(4, await context.OutboxMessages.CountAsync());
            Assert.Equal(0, await context.NotificationDeliveries.CountAsync());
            Assert.Equal(0, await context.AdministrativeCommands.CountAsync());
            Assert.Equal(0, await context.CommandAttempts.CountAsync());
        }

        /// <summary>Confirms the coordinated assignment race created revocation evidence and no operational effects.</summary>
        /// <param name="agentId">Enrolled and revoked race-fixture Agent.</param>
        /// <param name="expectedRevokedAt">Exact durable revocation instant.</param>
        public async Task AssertRaceIsolationAsync(Guid agentId, DateTimeOffset? expectedRevokedAt)
        {
            await using AsyncServiceScope scope = application.Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            RegisteredAgentRow agent = await context.Agents.SingleAsync(row => row.AgentId == agentId);
            AgentCertificateRow certificate = await context.AgentCertificates.SingleAsync(row => row.AgentId == agentId);
            Assert.Equal("Revoked", agent.State);
            Assert.Equal(expectedRevokedAt, agent.RevokedAt);
            Assert.Equal("Revoked", certificate.State);
            Assert.Equal("sandbox-race", certificate.RevocationReasonCode);
            Assert.Empty(await context.AgentHeartbeats.Where(row => row.AgentId == agentId).ToArrayAsync());
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

        /// <summary>Adds a test-only per-run budget so harness controls cannot consume the ordinary human API partition.</summary>
        /// <param name="options">Rate limiter options owned by the isolated integration fixture.</param>
        /// <param name="runId">Immutable owning run identifier; untrusted request headers cannot create extra partitions.</param>
        private static void AddConsolidatedHarnessRatePolicy(RateLimiterOptions options, string runId) =>
            options.AddPolicy(ConsolidatedHarnessRateLimitPolicy, _ =>
                RateLimitPartition.GetFixedWindowLimiter(
                    runId,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
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

    /// <summary>Supplies a deterministic Server clock that advances only when its owning E2E fixture requests it.</summary>
    /// <param name="now">Initial UTC instant.</param>
    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;

        /// <summary>Advances the fixture clock by one positive duration no greater than one hour.</summary>
        /// <param name="duration">Deterministic amount to add.</param>
        public void Advance(TimeSpan duration)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(duration, TimeSpan.FromHours(1));
            now = now.Add(duration);
        }
    }

    /// <summary>
    /// Coordinates one E2E-only pause after HTTP authentication and request validation but before persistence
    /// admission, allowing revocation to complete while the synchronisation request remains in flight.
    /// </summary>
    private sealed class ObservationIngestionPause(long targetSequence)
    {
        private readonly TaskCompletionSource<bool> paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<ObservationItemResult> result = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int interceptionCount;

        /// <summary>Pauses only the first ingestion carrying the selected positive Agent sequence.</summary>
        /// <param name="sequence">Canonical Agent sequence entering the store boundary.</param>
        /// <param name="cancellationToken">Request cancellation propagated by the sandbox endpoint.</param>
        public async ValueTask PauseIfSelectedAsync(long sequence, CancellationToken cancellationToken)
        {
            if (sequence != targetSequence || Interlocked.CompareExchange(ref interceptionCount, 1, 0) != 0)
            {
                return;
            }

            paused.TrySetResult(true);
            await released.Task.WaitAsync(cancellationToken);
        }

        /// <summary>Waits until the selected request has reached the pre-persistence barrier.</summary>
        /// <param name="cancellationToken">Token bounding the deterministic coordination wait.</param>
        /// <returns>A task whose successful result confirms that the barrier was reached.</returns>
        public Task<bool> WaitUntilPausedAsync(CancellationToken cancellationToken) =>
            paused.Task.WaitAsync(cancellationToken);

        /// <summary>Releases the selected request exactly once; repeated cleanup calls are harmless.</summary>
        public void Release() => released.TrySetResult(true);

        /// <summary>Records the terminal store result for the selected request.</summary>
        /// <param name="sequence">Canonical Agent sequence returned by the decorated store.</param>
        /// <param name="itemResult">Exact ingestion classification returned by the production store.</param>
        public void RecordResult(long sequence, ObservationItemResult itemResult)
        {
            if (sequence == targetSequence)
            {
                result.TrySetResult(itemResult);
            }
        }

        /// <summary>Waits for the production store classification of the selected request.</summary>
        /// <param name="cancellationToken">Token bounding the deterministic result wait.</param>
        /// <returns>The exact selected ingestion result.</returns>
        public Task<ObservationItemResult> WaitForResultAsync(CancellationToken cancellationToken) =>
            result.Task.WaitAsync(cancellationToken);
    }

    /// <summary>Applies the fixture-owned revocation barrier without changing the production ingestion store.</summary>
    private sealed class PausingObservationIngestionStore(
        IObservationIngestionStore inner,
        ObservationIngestionPause pause) :
        IObservationIngestionStore,
        IRejectedObservationSequenceStore
    {
        /// <inheritdoc />
        public async ValueTask<ObservationItemResult> IngestAsync(
            ObservationSyncMessage message,
            DateTimeOffset receivedAt,
            CancellationToken cancellationToken)
        {
            await pause.PauseIfSelectedAsync(message.Sequence, cancellationToken);
            ObservationItemResult result = await inner.IngestAsync(message, receivedAt, cancellationToken);
            pause.RecordResult(message.Sequence, result);
            return result;
        }

        /// <inheritdoc />
        public async ValueTask<ObservationItemResult> ConsumeRejectedAsync(
            Guid agentId,
            Guid messageId,
            long sequence,
            string errorCode,
            DateTimeOffset receivedAt,
            CancellationToken cancellationToken)
        {
            await pause.PauseIfSelectedAsync(sequence, cancellationToken);
            ObservationItemResult result = inner is IRejectedObservationSequenceStore rejectionStore
                ? await rejectionStore.ConsumeRejectedAsync(
                    agentId,
                    messageId,
                    sequence,
                    errorCode,
                    receivedAt,
                    cancellationToken)
                : new ObservationItemResult(
                    messageId,
                    ObservationIngestionDisposition.Retryable,
                    "ingestion.rejection_store_unavailable");
            pause.RecordResult(sequence, result);
            return result;
        }

        /// <inheritdoc />
        public ValueTask<long> GetHighestContiguousSequenceAsync(
            Guid agentId,
            CancellationToken cancellationToken) => inner.GetHighestContiguousSequenceAsync(
                agentId,
                cancellationToken);
    }

    /// <summary>Fails if a one-attempt E2E path unexpectedly schedules a retry.</summary>
    private sealed class RejectingDelay : IAgentFleetSandboxDelay
    {
        /// <inheritdoc />
        public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The one-attempt sandbox path must not schedule a delay.");
    }

    /// <summary>Returns one deterministic unit-interval jitter sample.</summary>
    /// <param name="sample">Fixed sample returned for every request.</param>
    private sealed class FixedJitter(double sample) : IAgentFleetSandboxJitter
    {
        /// <inheritdoc />
        public double NextUnitInterval() => sample;
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
