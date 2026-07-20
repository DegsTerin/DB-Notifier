// Module purpose: Proves deterministic STATE-06 revocation finalisation interleavings and sanitised failure evidence without operational composition.
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Application.Presentation;
using DBNotifier.Infrastructure.AgentFleet;
using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Server.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DBNotifier.IntegrationTests;

public sealed partial class AgentFleetApiEndToEndTests
{
    private static readonly string[] ConsolidatedRevocationScenarios = ["R1", "R2", "R3", "R4", "R5", "R6", "R7"];

    /// <summary>
    /// Executes the seven authorised revocation orders with explicit barriers, controlled time, bounded cancellation
    /// and durable assertions against the same synthetic Agent within each scenario.
    /// </summary>
    [Fact]
    public async Task ConsolidatedRevocationInterleavingsR1ThroughR7RemainDeterministicAndFailClosed()
    {
        using CancellationTokenSource totalBudget = new(TimeSpan.FromMinutes(4));
        foreach (string scenario in ConsolidatedRevocationScenarios)
        {
            using CancellationTokenSource scenarioBudget = CancellationTokenSource.CreateLinkedTokenSource(
                totalBudget.Token);
            scenarioBudget.CancelAfter(TimeSpan.FromSeconds(30));
            await RunConsolidatedRevocationScenarioAsync(scenario, scenarioBudget.Token);
        }
    }

    /// <summary>Confirms exception text and secret-shaped values cannot enter the public diagnostic envelope.</summary>
    [Fact]
    public void ConsolidatedFailureEvidenceSuppressesExceptionTextAndSensitiveFields()
    {
        const string forbidden = "token=fixture-secret C:\\private\\identity.pfx thumbprint=ABC123";
        ConsolidatedHarnessState state = new(Guid.NewGuid());
        state.SetStage("finalising-revocation-commit-verification");

        ConsolidatedHarnessFailureEvidence evidence = state.RecordFailure(new InvalidOperationException(forbidden));
        string serialised = JsonSerializer.Serialize(evidence, JsonOptions);

        Assert.Equal("unexpected", evidence.Category);
        Assert.DoesNotContain(forbidden, serialised, StringComparison.Ordinal);
        Assert.DoesNotContain("fixture-secret", serialised, StringComparison.Ordinal);
        Assert.DoesNotContain("identity.pfx", serialised, StringComparison.Ordinal);
        Assert.DoesNotContain("thumbprint", serialised, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", serialised, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Runs one exact R1-R7 scenario and proves central denial remains distinct from local quarantine.</summary>
    /// <param name="scenario">Stable authorised scenario identifier.</param>
    /// <param name="cancellationToken">Thirty-second per-scenario budget.</param>
    private static async Task RunConsolidatedRevocationScenarioAsync(
        string scenario,
        CancellationToken cancellationToken)
    {
        ConsolidatedSnapshotResponseBarrier? snapshotBarrier = scenario == "R3"
            ? new ConsolidatedSnapshotResponseBarrier()
            : null;
        await using AgentFleetSandbox sandbox = await AgentFleetSandbox.StartAsync(
            enableObservationPipeline: true,
            enableCommandTransport: scenario == "R5",
            enableRevocationMatrixHints: scenario == "R4",
            snapshotResponseBarrier: snapshotBarrier);
        await using AgentFileSandbox local = await AgentFileSandbox.StartConsolidatedAsync(Guid.NewGuid());
        await using SandboxAgentIdentityStore identities = new(sandbox);
        string installationId = $"installation:{Guid.NewGuid():N}";
        AgentEnrollmentRequest tokenBinding = new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            installationId,
            $"STATE-06 revocation matrix {scenario}",
            EnvironmentName,
            PlatformName,
            AgentVersion,
            ScopeName,
            sandbox.Now,
            sandbox.Now,
            "test-only-public-csr-placeholder");
        string token = await sandbox.SeedEnrollmentTokenAsync(tokenBinding);
        CountingAgentFleetClientTransport transport = new(new HttpAgentFleetClientTransport(
            sandbox.BaseAddress,
            AgentVersion,
            identities.GetClient));
        AgentFleetClientCoordinator coordinator = new(
            identities,
            transport,
            local.Store,
            sandbox.SandboxTimeProvider,
            TimeSpan.FromMinutes(5));
        AgentFleetClientResult enrollment = await coordinator.EnrolForTestAsync(
            new AgentEnrollmentBootstrap(
                token,
                installationId,
                $"STATE-06 revocation matrix {scenario}",
                EnvironmentName,
                PlatformName,
                AgentVersion,
                ScopeName),
            cancellationToken);
        Assert.True(enrollment.Succeeded);
        AgentLocalRegistration registration = Assert.IsType<AgentLocalRegistration>(
            await local.Store.GetRegistrationAsync(cancellationToken));
        Guid instanceId = await sandbox.SeedAssignmentsAndHumanAccessAsync(
            registration.AgentId,
            omitMonitoringCredential: true);
        AgentFleetSandboxResilienceResult assignments = await CreateOneShotResilience(
                coordinator,
                local.Store,
                sandbox.Now)
            .ReconcileAssignmentsAsync(
                $"sandbox:matrix:{scenario}:assignments",
                AgentVersion,
                cancellationToken);
        AgentFleetSandboxResilienceResult heartbeat = await CreateOneShotResilience(
                coordinator,
                local.Store,
                sandbox.Now)
            .SendHeartbeatAsync(
                $"sandbox:matrix:{scenario}:heartbeat",
                AgentVersion,
                cancellationToken);
        Assert.True(assignments.Result.Succeeded);
        Assert.True(heartbeat.Result.Succeeded);
        AgentAssignmentLocalState lastKnownGood = await local.Store.GetAssignmentStateAsync(
            registration.AgentId,
            cancellationToken);
        HttpClient agentClient = identities.GetClient(registration.IdentityReference);

        AgentRevocationHttpEvidence? revocation = null;
        switch (scenario)
        {
            case "R1":
                break;
            case "R2":
                using (HttpClient dashboard = sandbox.CreateDashboardTvClient())
                {
                    (DashboardTvSnapshot snapshot, _) = await ReadDashboardSnapshotAsync(
                        dashboard,
                        cancellationToken);
                    Assert.Empty(snapshot.Items);
                }
                break;
            case "R3":
                using (HttpClient dashboard = sandbox.CreateDashboardTvClient())
                {
                    snapshotBarrier!.Arm();
                    Task<HttpResponseMessage> heldSnapshot = dashboard.GetAsync(
                        DashboardTvSandboxEndpointRouteBuilderExtensions.SnapshotRoute,
                        cancellationToken);
                    try
                    {
                        await snapshotBarrier.WaitUntilHeldAsync(cancellationToken);
                        revocation = await sandbox.RevokeAgentAsync(registration.AgentId, cancellationToken);
                    }
                    finally
                    {
                        snapshotBarrier.Release();
                    }

                    using HttpResponseMessage snapshotResponse = await heldSnapshot;
                    Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
                }
                break;
            case "R4":
                revocation = await sandbox.RevokeAgentAsync(registration.AgentId, cancellationToken);
                using (HttpClient dashboard = sandbox.CreateDashboardTvClient())
                {
                    (DashboardTvSnapshot snapshot, _) = await ReadDashboardSnapshotAsync(
                        dashboard,
                        cancellationToken);
                    Assert.Empty(snapshot.Items);
                }
                await sandbox.Services.GetRequiredService<IDashboardTvChangeHintPublisher>()
                    .PublishAsync($"sha256-{new string('a', 64)}", cancellationToken);
                Assert.Equal(
                    1,
                    sandbox.Services.GetRequiredService<DashboardTvChangeHintSandboxEvidence>()
                        .Snapshot()
                        .PublishedHints);
                break;
            case "R5":
                {
                    CommandTransportFixtureIds fixtures = await sandbox.SeedCommandTransportFixturesAsync(
                        registration.AgentId,
                        instanceId);
                    (byte[] identityPackage, string password) = identities.ExportIdentityPackage(
                        registration.IdentityReference);
                    try
                    {
                        CommandTransportProcessResult process = await RunCommandTransportHostAsync(
                            local,
                            sandbox,
                            registration,
                            identityPackage,
                            password,
                            sandbox.Now,
                            "none");
                        Assert.Equal(0, process.ExitCode);
                        await local.AssertCommandTransportCompletedAsync(registration.AgentId, fixtures);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(identityPackage);
                    }
                    break;
                }
            case "R6":
                sandbox.Advance(TimeSpan.FromSeconds(45));
                break;
            case "R7":
                await ProveNewerAgentFenceAsync(sandbox, local, coordinator, registration, cancellationToken);
                break;
            default:
                throw new InvalidOperationException("The revocation scenario is outside the authorised matrix.");
        }

        revocation ??= await sandbox.RevokeAgentAsync(registration.AgentId, cancellationToken);
        Assert.Equal(
            AgentRevocationDisposition.Revoked,
            Assert.IsType<AgentRevocationOutcome>(revocation.Outcome).Disposition);
        await ProveRevocationFinalisationAsync(
            sandbox,
            local,
            coordinator,
            transport,
            registration,
            agentClient,
            lastKnownGood,
            cancellationToken);
    }

    /// <summary>Proves an expired owner cannot clear or write through the next monotonic Agent-local fence.</summary>
    private static async Task ProveNewerAgentFenceAsync(
        AgentFleetSandbox sandbox,
        AgentFileSandbox local,
        AgentFleetClientCoordinator coordinator,
        AgentLocalRegistration registration,
        CancellationToken cancellationToken)
    {
        AgentFleetOperationLease older = Assert.IsType<AgentFleetOperationLease>(
            await local.Store.TryAcquireOperationLeaseAsync(
                registration.AgentId,
                "sandbox:matrix:r7:older",
                AgentFleetOperationKind.Heartbeat,
                sandbox.Now,
                TimeSpan.FromSeconds(30),
                cancellationToken));
        AgentFleetSandboxResilienceResult busy = await CreateOneShotResilience(
                coordinator,
                local.Store,
                sandbox.Now)
            .SendHeartbeatAsync(
                "sandbox:matrix:r7:contender",
                AgentVersion,
                cancellationToken);
        Assert.Equal(0, busy.Attempts);
        Assert.Null(busy.FenceToken);
        Assert.Equal("agent_fleet.operation_busy", busy.Result.Code);

        sandbox.Advance(TimeSpan.FromSeconds(31));
        AgentFleetOperationLease current = Assert.IsType<AgentFleetOperationLease>(
            await local.Store.TryAcquireOperationLeaseAsync(
                registration.AgentId,
                "sandbox:matrix:r7:current",
                AgentFleetOperationKind.Heartbeat,
                sandbox.Now,
                TimeSpan.FromSeconds(30),
                cancellationToken));
        Assert.True(current.FenceToken > older.FenceToken);
        await local.Store.ReleaseOperationLeaseAsync(older, cancellationToken);
        AgentFleetStateRow fenced = await local.ReadFleetStateAsync(registration.AgentId);
        Assert.Equal(current.FenceToken, fenced.OperationLeaseFence);
        Assert.Equal(current.OwnerId, fenced.OperationLeaseOwner);
        await local.Store.ReleaseOperationLeaseAsync(current, cancellationToken);
    }

    /// <summary>Proves durable central revocation, direct server denial and later local quarantine as separate facts.</summary>
    private static async Task ProveRevocationFinalisationAsync(
        AgentFleetSandbox sandbox,
        AgentFileSandbox local,
        AgentFleetClientCoordinator coordinator,
        CountingAgentFleetClientTransport transport,
        AgentLocalRegistration registration,
        HttpClient agentClient,
        AgentAssignmentLocalState lastKnownGood,
        CancellationToken cancellationToken)
    {
        ConsolidatedCentralRevocationEvidence central = await sandbox.ReadCentralRevocationEvidenceAsync(
            registration.AgentId,
            cancellationToken);
        Assert.Equal("Revoked", central.AgentState);
        Assert.NotNull(central.RevokedAt);
        Assert.True(central.AllCertificatesRevoked);

        using HttpResponseMessage directHeartbeat = await SendAgentJsonAsync(
            agentClient,
            HttpMethod.Post,
            $"/api/v1/agents/{registration.AgentId:D}/heartbeats",
            CreateHeartbeat(registration.AgentId, 2, sandbox.Now),
            cancellationToken);
        Assert.Contains(
            directHeartbeat.StatusCode,
            new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
        using HttpRequestMessage assignmentRequest = CreateAgentRequest(
            HttpMethod.Get,
            $"/api/v1/agents/{registration.AgentId:D}/assignments");
        using HttpResponseMessage directAssignments = await agentClient.SendAsync(
            assignmentRequest,
            cancellationToken);
        Assert.Contains(
            directAssignments.StatusCode,
            new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });

        int heartbeatCalls = transport.HeartbeatCalls;
        AgentFleetSandboxResilienceResult heartbeat = await CreateOneShotResilience(
                coordinator,
                local.Store,
                sandbox.Now)
            .SendHeartbeatAsync(
                $"sandbox:matrix:final-heartbeat:{Guid.NewGuid():N}",
                AgentVersion,
                cancellationToken);
        Assert.Equal(heartbeatCalls + 1, transport.HeartbeatCalls);
        Assert.False(heartbeat.Result.Succeeded);
        Assert.Equal(AgentLocalIdentityState.RevokedOrDenied, heartbeat.Result.State);
        Assert.NotNull(heartbeat.FenceToken);

        int assignmentCalls = transport.AssignmentCalls;
        AgentFleetSandboxResilienceResult assignments = await CreateOneShotResilience(
                coordinator,
                local.Store,
                sandbox.Now)
            .ReconcileAssignmentsAsync(
                $"sandbox:matrix:final-assignments:{Guid.NewGuid():N}",
                AgentVersion,
                cancellationToken);
        Assert.Equal(assignmentCalls, transport.AssignmentCalls);
        Assert.False(assignments.Result.Succeeded);
        Assert.Equal(AgentLocalIdentityState.RevokedOrDenied, assignments.Result.State);
        Assert.Equal("agent.identity_unavailable", assignments.Result.Code);
        AgentAssignmentLocalState preserved = await local.Store.GetAssignmentStateAsync(
            registration.AgentId,
            cancellationToken);
        Assert.Equal(lastKnownGood.Version, preserved.Version);
        AgentFleetStateRow localState = await local.ReadFleetStateAsync(registration.AgentId);
        Assert.Null(localState.OperationLeaseOwner);
        Assert.Null(localState.OperationLeaseFence);
        Assert.Null(localState.OperationLeaseExpiresAt);

        await using AsyncServiceScope scope = sandbox.Services.CreateAsyncScope();
        ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
        Assert.Equal(0, await context.CommandAttempts.CountAsync(cancellationToken));
        Assert.Equal(0, await context.HealthSamples.CountAsync(cancellationToken));
        Assert.Equal(0, await context.Events.CountAsync(cancellationToken));
        Assert.Equal(0, await context.OutboxMessages.CountAsync(cancellationToken));
        Assert.Equal(0, await context.NotificationDeliveries.CountAsync(cancellationToken));
    }

    /// <summary>Holds one completed authoritative snapshot response until the test releases its deterministic gate.</summary>
    private sealed class ConsolidatedSnapshotResponseBarrier
    {
        private readonly TaskCompletionSource<bool> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int armed;

        /// <summary>Arms exactly one snapshot response.</summary>
        public void Arm() => Interlocked.Exchange(ref armed, 1);

        /// <summary>Waits until the selected snapshot has completed its authoritative read but not left middleware.</summary>
        public Task<bool> WaitUntilHeldAsync(CancellationToken cancellationToken) =>
            held.Task.WaitAsync(cancellationToken);

        /// <summary>Releases the held response; repeated cleanup calls are harmless.</summary>
        public void Release() => released.TrySetResult(true);

        /// <summary>Applies the response barrier only to the first armed canonical snapshot request.</summary>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            bool selected = string.Equals(
                    context.Request.Path.Value,
                    DashboardTvSandboxEndpointRouteBuilderExtensions.SnapshotRoute,
                    StringComparison.Ordinal) &&
                Interlocked.Exchange(ref armed, 0) == 1;
            await next(context);
            if (!selected)
            {
                return;
            }

            held.TrySetResult(true);
            await released.Task.WaitAsync(context.RequestAborted);
        }
    }
}
