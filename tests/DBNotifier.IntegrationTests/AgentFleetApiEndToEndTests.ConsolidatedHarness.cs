// Module purpose: Composes one explicitly gated STATE-06 sandbox run that correlates Agent, API, browser, notification and non-executable command evidence.
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Application.Presentation;
using DBNotifier.Application.Synchronization;
using DBNotifier.Infrastructure.AgentFleet;
using DBNotifier.Infrastructure.Presentation;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Server.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DBNotifier.IntegrationTests;

public sealed partial class AgentFleetApiEndToEndTests
{
    private const string ConsolidatedEvidenceRoute = "/__dbnotifier-state06-consolidated/evidence";
    private const string ConsolidatedAdvanceRoute = "/__dbnotifier-state06-consolidated/advance";
    private const string ConsolidatedFinaliseRoute = "/__dbnotifier-state06-consolidated/finalise";
    private const string ConsolidatedShutdownRoute = "/__dbnotifier-state06-consolidated/shutdown";
    private const string ConsolidatedRunHeader = "X-DBN-State06-Consolidated-Run";

    /// <summary>
    /// Starts one local-only consolidated host, prepares the first correlated observation and waits for the
    /// authenticated browser auditor to advance, finalise and stop the run.
    /// </summary>
    /// <param name="dashboardRoot">Absolute root of the already-built local Dashboard.</param>
    /// <param name="cancellationToken">Cancellation bounding the temporary host lifetime.</param>
    /// <returns>Zero after complete disposal, or an exception for the owning executable to sanitise.</returns>
    public static async Task<int> RunState06ConsolidatedSandboxHostAsync(
        string dashboardRoot,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dashboardRoot);
        string fullDashboardRoot = Path.GetFullPath(dashboardRoot);
        if (!Directory.Exists(fullDashboardRoot) ||
            !File.Exists(Path.Combine(fullDashboardRoot, "index.html")))
        {
            throw new ArgumentException("The consolidated Dashboard root is unavailable.", nameof(dashboardRoot));
        }

        ConsolidatedHarnessState state = new(Guid.NewGuid());
        AgentFleetSandbox? sandbox = null;
        try
        {
            sandbox = await AgentFleetSandbox.StartAsync(
                enableObservationPipeline: true,
                enableCommandTransport: true,
                consolidatedHarness: state,
                dashboardRoot: fullDashboardRoot);
            await state.InitialiseAsync(sandbox, cancellationToken);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                marker = "DBNOTIFIER_STATE06_CONSOLIDATED_SANDBOX_READY",
                baseAddress = sandbox.BrowserBaseAddress.AbsoluteUri,
                spkiPin = sandbox.ServerSubjectPublicKeyInfoPin,
                runId = state.RunId,
            }, JsonOptions));
            await sandbox.WaitForShutdownAsync(cancellationToken);
            return 0;
        }
        finally
        {
            await state.DisposeAsync();
            if (sandbox is not null)
            {
                await sandbox.DisposeAsync();
            }
        }
    }

    /// <summary>Maps test-only controls after every ordinary sandbox guard has succeeded.</summary>
    /// <param name="application">Exact loopback application owned by the integration fixture.</param>
    /// <param name="state">Single correlated run state.</param>
    private static void MapConsolidatedHarnessEndpoints(
        WebApplication application,
        ConsolidatedHarnessState state)
    {
        application.MapGet(ConsolidatedEvidenceRoute, (HttpContext context) =>
            state.IsExactRun(context)
                ? Results.Json(state.Snapshot(
                    application.Services.GetRequiredService<DashboardTvChangeHintSandboxEvidence>().Snapshot()))
                : Results.Unauthorized())
            .RequireAuthorization(DashboardTvSandboxEndpointRouteBuilderExtensions.ReadPolicy)
            .RequireRateLimiting("HumanApiRateLimit");

        Func<HttpContext, Task<IResult>> advance = context =>
            ExecuteControlAsync(context, state, state.AdvanceAsync);
        application.MapPost(ConsolidatedAdvanceRoute, advance)
            .RequireAuthorization(DashboardTvSandboxEndpointRouteBuilderExtensions.ReadPolicy)
            .RequireRateLimiting("HumanApiRateLimit");

        Func<HttpContext, Task<IResult>> finalise = context =>
            ExecuteControlAsync(context, state, state.FinaliseAsync);
        application.MapPost(ConsolidatedFinaliseRoute, finalise)
            .RequireAuthorization(DashboardTvSandboxEndpointRouteBuilderExtensions.ReadPolicy)
            .RequireRateLimiting("HumanApiRateLimit");

        application.MapPost(ConsolidatedShutdownRoute, (HttpContext context) =>
        {
            if (!state.IsExactRun(context) || !state.IsFinalised)
            {
                return Results.Unauthorized();
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                application.Lifetime.StopApplication();
            });
            return Results.NoContent();
        })
            .RequireAuthorization(DashboardTvSandboxEndpointRouteBuilderExtensions.ReadPolicy)
            .RequireRateLimiting("HumanApiRateLimit");
    }

    /// <summary>Executes one serial state transition and returns only sanitised evidence or a typed failure.</summary>
    /// <param name="context">Authenticated HTTPS loopback request.</param>
    /// <param name="state">Owning run state.</param>
    /// <param name="operation">Bounded state transition.</param>
    /// <returns>No content after success, unauthorised, or a sanitised problem response.</returns>
    private static async Task<IResult> ExecuteControlAsync(
        HttpContext context,
        ConsolidatedHarnessState state,
        Func<CancellationToken, Task> operation)
    {
        if (!state.IsExactRun(context))
        {
            return Results.Unauthorized();
        }

        try
        {
            await operation(context.RequestAborted);
            return Results.NoContent();
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            ConsolidatedHarnessFailureEvidence failure = state.RecordFailure(exception);
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Consolidated sandbox operation failed",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "state06.consolidated_operation_failed",
                    ["stage"] = state.Stage,
                    ["failure"] = failure,
                });
        }
    }

    /// <summary>Owns one correlated identity, two temporary stores and every bounded transition of the harness.</summary>
    private sealed class ConsolidatedHarnessState(Guid runId) : IAsyncDisposable
    {
        private const int MaximumSnapshotEvidence = 32;
        private readonly object evidenceGate = new();
        private readonly SemaphoreSlim operationGate = new(1, 1);
        private readonly List<ConsolidatedSnapshotRequestEvidence> snapshotRequests = [];
        private readonly ConsolidatedRecordingSink notificationSink = new();
        private AgentFleetSandbox? sandbox;
        private AgentFileSandbox? local;
        private SandboxAgentIdentityStore? identities;
        private CountingAgentFleetClientTransport? fleetTransportEvidence;
        private AgentFleetClientCoordinator? fleetCoordinator;
        private AgentLocalRegistration? registration;
        private CommandTransportFixtureIds? commandFixtures;
        private byte[]? pkcs12;
        private string? identityPassword;
        private HttpClient? notificationClient;
        private FileReconciledNotificationLedger? notificationLedger;
        private ReconciledNotificationCoordinator? notificationCoordinator;
        private Guid instanceId;
        private int activeSnapshotRequests;
        private int maximumSnapshotConcurrency;
        private int observationSamples;
        private int commandJournalEntries;
        private int commandAttempts;
        private bool initialReplayObserved;
        private bool notificationRestartDeduplicated;
        private bool heartbeatDeniedAfterRevocation;
        private bool assignmentsDeniedAfterRevocation;
        private bool observationDeniedAfterRevocation;
        private bool commandDeniedAfterRevocation;
        private bool centralRevocationCommitted;
        private int revokedCertificateCount;
        private bool heartbeatServerDeniedAfterRevocation;
        private bool assignmentsServerDeniedAfterRevocation;
        private bool heartbeatTransportAttemptedAfterRevocation;
        private bool assignmentsTransportAttemptedAfterRevocation;
        private bool assignmentLastKnownGoodPreserved;
        private bool commandProcessesExitedBeforeRevocation;
        private long? heartbeatFenceAfterRevocation;
        private long? assignmentsFenceAfterRevocation;
        private bool initialised;
        private bool advanced;
        private bool finalised;
        private bool disposed;
        private string stage = "created";
        private ConsolidatedOperationDiagnostic diagnostic = ConsolidatedOperationDiagnostic.Empty("created");
        private ConsolidatedHarnessFailureEvidence? failure;

        /// <summary>Gets the public correlation identifier for this fixture run.</summary>
        public string RunId => runId.ToString("D");

        /// <summary>Gets whether the terminal command and revocation checks completed.</summary>
        public bool IsFinalised => Volatile.Read(ref finalised);

        /// <summary>Gets the current non-secret orchestration stage.</summary>
        public string Stage
        {
            get
            {
                lock (evidenceGate)
                {
                    return stage;
                }
            }
        }

        /// <summary>Prepares enrollment, heartbeat, assignment, offline replay and notification baseline.</summary>
        /// <param name="owner">Started server sandbox using the shared Server SQLite.</param>
        /// <param name="cancellationToken">Cancellation bounding initial local setup.</param>
        /// <returns>A task completing after the first authoritative snapshot is available.</returns>
        public async Task InitialiseAsync(AgentFleetSandbox owner, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(owner);
            await operationGate.WaitAsync(cancellationToken);
            try
            {
                if (initialised)
                {
                    throw new InvalidOperationException("The consolidated sandbox was already initialised.");
                }

                SetStage("initialising");
                sandbox = owner;
                local = await AgentFileSandbox.StartConsolidatedAsync();
                identities = new SandboxAgentIdentityStore(owner);
                string installationId = $"installation:{Guid.NewGuid():N}";
                AgentEnrollmentRequest tokenBinding = new(
                    Guid.NewGuid(),
                    AgentFleetProtocol.CurrentSchemaVersion,
                    installationId,
                    "STATE-06 Consolidated Sandbox Agent",
                    EnvironmentName,
                    PlatformName,
                    AgentVersion,
                    ScopeName,
                    owner.Now,
                    owner.Now,
                    "test-only-public-csr-placeholder");
                string token = await owner.SeedEnrollmentTokenAsync(tokenBinding);
                HttpAgentFleetClientTransport fleetTransport = new(
                    owner.BaseAddress,
                    AgentVersion,
                    identities.GetClient);
                fleetTransportEvidence = new CountingAgentFleetClientTransport(fleetTransport);
                fleetCoordinator = new AgentFleetClientCoordinator(
                    identities,
                    fleetTransportEvidence,
                    local.Store,
                    owner.SandboxTimeProvider,
                    TimeSpan.FromMinutes(5));
                AgentFleetClientResult enrollment = await fleetCoordinator.EnrolForTestAsync(
                    new AgentEnrollmentBootstrap(
                        token,
                        installationId,
                        "STATE-06 Consolidated Sandbox Agent",
                        EnvironmentName,
                        PlatformName,
                        AgentVersion,
                        ScopeName),
                    cancellationToken);
                Assert.True(enrollment.Succeeded);
                registration = Assert.IsType<AgentLocalRegistration>(
                    await local.Store.GetRegistrationAsync(cancellationToken));
                instanceId = await owner.SeedAssignmentsAndHumanAccessAsync(
                    registration.AgentId,
                    omitMonitoringCredential: true);
                Assert.True((await RunAssignmentsOnceAsync(fleetCoordinator, local.Store, owner.Now)).Succeeded);
                Assert.True((await RunHeartbeatOnceAsync(fleetCoordinator, local.Store, owner.Now)).Succeeded);

                (pkcs12, identityPassword) = identities.ExportIdentityPackage(registration.IdentityReference);
                Uri unavailable = GetUnusedLoopbackHttpsAddress();
                ObservationSandboxChildResult offline = await RunObservationSandboxHostAsync(
                    local,
                    owner,
                    registration,
                    instanceId,
                    pkcs12,
                    identityPassword,
                    unavailable,
                    owner.Now,
                    nameof(DBNotifier.Domain.HealthStatus.Degraded),
                    "normal");
                Assert.Equal(new ObservationSandboxChildResult(1, 1, 0, 1), offline);
                owner.Advance(TimeSpan.FromSeconds(3));
                ObservationSandboxChildResult replay = await RunObservationSandboxHostAsync(
                    local,
                    owner,
                    registration,
                    instanceId,
                    pkcs12,
                    identityPassword,
                    owner.BaseAddress,
                    owner.Now,
                    "none",
                    "normal");
                Assert.Equal(new ObservationSandboxChildResult(0, 1, 1, 0), replay);
                initialReplayObserved = true;
                observationSamples = 1;

                using HttpClient dashboard = owner.CreateDashboardTvClient();
                (DashboardTvSnapshot snapshot, _) = await ReadDashboardSnapshotAsync(
                    dashboard,
                    cancellationToken);
                DashboardTvInventoryItem item = Assert.Single(snapshot.Items);
                Assert.Equal(instanceId, item.InstanceId);
                Assert.Equal("degraded", item.Status);

                await CreateNotificationConsumerAsync();
                ReconciledNotificationCycleResult baseline = await notificationCoordinator!.RunOnceAsync(
                    new ReconciledNotificationSandboxPolicy(true, false),
                    cancellationToken);
                Assert.Equal(ReconciledNotificationCycleDisposition.BaselineEstablished, baseline.Disposition);
                Assert.Empty(notificationSink.Requests);
                commandFixtures = await owner.SeedCommandTransportFixturesAsync(
                    registration.AgentId,
                    instanceId);
                initialised = true;
                SetStage("ready");
            }
            finally
            {
                operationGate.Release();
            }
        }

        /// <summary>Commits the second observation, notifies once and publishes only its opaque revision.</summary>
        /// <param name="cancellationToken">Cancellation bounding the local transition.</param>
        /// <returns>A task completing after hint publication and notification restart evidence.</returns>
        public async Task AdvanceAsync(CancellationToken cancellationToken)
        {
            await operationGate.WaitAsync(cancellationToken);
            try
            {
                RequireState(initialised && !advanced && !finalised, "The consolidated advance stage is unavailable.");
                SetStage("advancing-observation");
                AgentFleetSandbox owner = Require(sandbox);
                AgentFileSandbox agent = Require(local);
                AgentLocalRegistration enrolled = Require(registration);
                byte[] identityPackage = Require(pkcs12);
                string password = Require(identityPassword);
                owner.Advance(TimeSpan.FromSeconds(31));
                ObservationSandboxChildResult lost = await RunObservationSandboxHostAsync(
                    agent,
                    owner,
                    enrolled,
                    instanceId,
                    identityPackage,
                    password,
                    owner.BaseAddress,
                    owner.Now,
                    nameof(DBNotifier.Domain.HealthStatus.Unavailable),
                    "drop-accepted-response");
                Assert.Equal(new ObservationSandboxChildResult(1, 1, 0, 1), lost);
                SetStage("advancing-observation-replaying");
                owner.Advance(TimeSpan.FromSeconds(3));
                ObservationSandboxChildResult replay = await RunObservationSandboxHostAsync(
                    agent,
                    owner,
                    enrolled,
                    instanceId,
                    identityPackage,
                    password,
                    owner.BaseAddress,
                    owner.Now,
                    "none",
                    "normal");
                Assert.Equal(new ObservationSandboxChildResult(0, 1, 1, 0), replay);
                observationSamples = 2;

                SetStage("advancing-notification");
                ReconciledNotificationCycleResult delivered = await notificationCoordinator!.RunOnceAsync(
                    new ReconciledNotificationSandboxPolicy(true, false),
                    cancellationToken);
                Assert.Equal(ReconciledNotificationCycleDisposition.Completed, delivered.Disposition);
                Assert.Equal(1, delivered.Accepted);
                Assert.Single(notificationSink.Requests);
                SetStage("advancing-notification-restart");
                await RestartNotificationConsumerAsync(cancellationToken);

                SetStage("advancing-signalr-hint");
                IDashboardTvSnapshotSource source = owner.Services.GetRequiredService<IDashboardTvSnapshotSource>();
                DashboardTvSnapshot snapshot = await source.ReadAsync(cancellationToken);
                DashboardTvInventoryItem item = Assert.Single(snapshot.Items);
                Assert.Equal("unavailable", item.Status);
                byte[] body = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonOptions);
                string revision = $"sha256-{Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant()}";
                IDashboardTvChangeHintPublisher publisher = owner.Services
                    .GetRequiredService<IDashboardTvChangeHintPublisher>();
                await publisher.PublishAsync(revision, cancellationToken);
                advanced = true;
                SetStage("advanced");
            }
            finally
            {
                operationGate.Release();
            }
        }

        /// <summary>Exercises command replay, revokes the same Agent and proves every later Agent path fails closed.</summary>
        /// <param name="cancellationToken">Cancellation bounding the terminal local checks.</param>
        /// <returns>A task completing after durable isolation is inspected.</returns>
        public async Task FinaliseAsync(CancellationToken cancellationToken)
        {
            await operationGate.WaitAsync(cancellationToken);
            try
            {
                RequireState(initialised && advanced && !finalised, "The consolidated finalisation stage is unavailable.");
                SetStage("finalising-command-and-revocation");
                AgentFleetSandbox owner = Require(sandbox);
                AgentFileSandbox agent = Require(local);
                SandboxAgentIdentityStore identityStore = Require(identities);
                AgentFleetClientCoordinator coordinator = Require(fleetCoordinator);
                AgentLocalRegistration enrolled = Require(registration);
                CommandTransportFixtureIds fixtures = Require(commandFixtures);
                byte[] identityPackage = Require(pkcs12);
                string password = Require(identityPassword);
                CountingAgentFleetClientTransport transportEvidence = Require(fleetTransportEvidence);

                using X509Certificate2 identity = X509CertificateLoader.LoadPkcs12(
                    identityPackage,
                    password,
                    X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
                using HttpClient directClient = owner.CreateClient(identity);
                SetStage("finalising-command-version-negative");
                using HttpResponseMessage incompatible = await SendCommandTransportPollAsync(
                    directClient,
                    enrolled.AgentId,
                    owner.Now,
                    1,
                    1,
                    4,
                    cancellationToken);
                ObserveDiagnostic(
                    httpStatusCode: (int)incompatible.StatusCode,
                    transportAttempted: true);
                Assert.Equal(HttpStatusCode.UpgradeRequired, incompatible.StatusCode);
                SetStage("finalising-command-gap-negative");
                using HttpResponseMessage gap = await SendCommandTransportPollAsync(
                    directClient,
                    enrolled.AgentId,
                    owner.Now,
                    2,
                    CommandTransportProtocol.CurrentSchemaVersion,
                    4,
                    cancellationToken);
                ObserveDiagnostic(
                    httpStatusCode: (int)gap.StatusCode,
                    transportAttempted: true);
                Assert.Equal(HttpStatusCode.Conflict, gap.StatusCode);

                SetStage("finalising-command-lost-poll");
                CommandTransportProcessResult lostPoll = await RunCommandTransportHostAsync(
                    agent,
                    owner,
                    enrolled,
                    identityPackage,
                    password,
                    owner.Now,
                    "lose-poll");
                Assert.Equal(3, lostPoll.ExitCode);
                await owner.AssertCommandTransportJournalAsync(enrolled.AgentId, 1, 1);
                SetStage("finalising-command-lost-acknowledgement");
                CommandTransportProcessResult lostAcknowledgement = await RunCommandTransportHostAsync(
                    agent,
                    owner,
                    enrolled,
                    identityPackage,
                    password,
                    owner.Now,
                    "lose-ack");
                Assert.Equal(3, lostAcknowledgement.ExitCode);
                await owner.AssertCommandTransportJournalAsync(enrolled.AgentId, 2, 2);
                SetStage("finalising-command-replay");
                CommandTransportProcessResult replay = await RunCommandTransportHostAsync(
                    agent,
                    owner,
                    enrolled,
                    identityPackage,
                    password,
                    owner.Now,
                    "none");
                Assert.Equal(0, replay.ExitCode);
                await agent.AssertCommandTransportCompletedAsync(enrolled.AgentId, fixtures);
                commandProcessesExitedBeforeRevocation = true;

                AgentAssignmentLocalState lastKnownGood = await agent.Store.GetAssignmentStateAsync(
                    enrolled.AgentId,
                    cancellationToken);
                SetStage("finalising-revocation-request");
                AgentRevocationHttpEvidence revocation = await owner.RevokeAgentForDiagnosticAsync(
                    enrolled.AgentId,
                    cancellationToken);
                ObserveDiagnostic(
                    httpStatusCode: (int)revocation.StatusCode,
                    disposition: revocation.Outcome?.Disposition.ToString(),
                    errorCode: revocation.Outcome?.ErrorCode);
                Assert.Equal(HttpStatusCode.OK, revocation.StatusCode);
                Assert.Equal(
                    AgentRevocationDisposition.Revoked,
                    Assert.IsType<AgentRevocationOutcome>(revocation.Outcome).Disposition);

                SetStage("finalising-revocation-commit-verification");
                ConsolidatedCentralRevocationEvidence central = await owner.ReadCentralRevocationEvidenceAsync(
                    enrolled.AgentId,
                    cancellationToken);
                centralRevocationCommitted = central.AgentState == "Revoked" &&
                    central.RevokedAt is not null &&
                    central.CertificateCount > 0 &&
                    central.RevokedCertificateCount == central.CertificateCount;
                revokedCertificateCount = central.RevokedCertificateCount;
                ObserveDiagnostic(
                    centralAgentState: central.AgentState,
                    centralCertificateState: central.AllCertificatesRevoked ? "Revoked" : "Mixed",
                    expectedCount: central.CertificateCount,
                    actualCount: central.RevokedCertificateCount);
                Assert.True(centralRevocationCommitted);

                SetStage("finalising-heartbeat-server-denial");
                using HttpResponseMessage directHeartbeatResponse = await SendAgentJsonAsync(
                    directClient,
                    HttpMethod.Post,
                    $"/api/v1/agents/{enrolled.AgentId:D}/heartbeats",
                    CreateHeartbeat(enrolled.AgentId, 2, owner.Now),
                    cancellationToken);
                heartbeatServerDeniedAfterRevocation = directHeartbeatResponse.StatusCode is
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
                ObserveDiagnostic(
                    httpStatusCode: (int)directHeartbeatResponse.StatusCode,
                    transportAttempted: true);
                Assert.True(heartbeatServerDeniedAfterRevocation);

                SetStage("finalising-assignments-server-denial");
                using HttpRequestMessage directAssignmentsRequest = CreateAgentRequest(
                    HttpMethod.Get,
                    $"/api/v1/agents/{enrolled.AgentId:D}/assignments");
                using HttpResponseMessage directAssignmentsResponse = await directClient.SendAsync(
                    directAssignmentsRequest,
                    cancellationToken);
                assignmentsServerDeniedAfterRevocation = directAssignmentsResponse.StatusCode is
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
                ObserveDiagnostic(
                    httpStatusCode: (int)directAssignmentsResponse.StatusCode,
                    transportAttempted: true);
                Assert.True(assignmentsServerDeniedAfterRevocation);

                SetStage("finalising-heartbeat-local-quarantine");
                int heartbeatCallsBefore = transportEvidence.HeartbeatCalls;
                AgentFleetSandboxResilienceResult heartbeat = await CreateOneShotResilience(
                        coordinator,
                        agent.Store,
                        owner.Now)
                    .SendHeartbeatAsync(
                        $"sandbox:consolidated:heartbeat:{Guid.NewGuid():N}",
                        AgentVersion,
                        cancellationToken);
                heartbeatTransportAttemptedAfterRevocation = transportEvidence.HeartbeatCalls > heartbeatCallsBefore;
                heartbeatFenceAfterRevocation = heartbeat.FenceToken;
                heartbeatDeniedAfterRevocation = !heartbeat.Result.Succeeded &&
                    heartbeat.Result.State == AgentLocalIdentityState.RevokedOrDenied;
                ObserveDiagnostic(
                    disposition: heartbeat.Result.State.ToString(),
                    errorCode: heartbeat.Result.Code,
                    attempts: heartbeat.Attempts,
                    fenceToken: heartbeat.FenceToken,
                    localIdentityState: heartbeat.Result.State.ToString(),
                    transportAttempted: heartbeatTransportAttemptedAfterRevocation);
                Assert.True(heartbeatDeniedAfterRevocation);
                Assert.True(heartbeatTransportAttemptedAfterRevocation);
                Assert.Equal(1, heartbeat.Attempts);
                Assert.NotNull(heartbeat.FenceToken);

                SetStage("finalising-assignments-local-quarantine");
                int assignmentCallsBefore = transportEvidence.AssignmentCalls;
                AgentFleetSandboxResilienceResult assignments = await CreateOneShotResilience(
                        coordinator,
                        agent.Store,
                        owner.Now)
                    .ReconcileAssignmentsAsync(
                        $"sandbox:consolidated:assignments:{Guid.NewGuid():N}",
                        AgentVersion,
                        cancellationToken);
                assignmentsTransportAttemptedAfterRevocation = transportEvidence.AssignmentCalls > assignmentCallsBefore;
                assignmentsFenceAfterRevocation = assignments.FenceToken;
                assignmentsDeniedAfterRevocation = !assignments.Result.Succeeded &&
                    assignments.Result.State == AgentLocalIdentityState.RevokedOrDenied;
                AgentAssignmentLocalState preserved = await agent.Store.GetAssignmentStateAsync(
                    enrolled.AgentId,
                    cancellationToken);
                assignmentLastKnownGoodPreserved = string.Equals(
                    lastKnownGood.Version,
                    preserved.Version,
                    StringComparison.Ordinal);
                ObserveDiagnostic(
                    disposition: assignments.Result.State.ToString(),
                    errorCode: assignments.Result.Code,
                    attempts: assignments.Attempts,
                    fenceToken: assignments.FenceToken,
                    localIdentityState: assignments.Result.State.ToString(),
                    transportAttempted: assignmentsTransportAttemptedAfterRevocation);
                Assert.True(heartbeatDeniedAfterRevocation);
                Assert.True(assignmentsDeniedAfterRevocation);
                Assert.False(assignmentsTransportAttemptedAfterRevocation);
                Assert.True(assignmentLastKnownGoodPreserved);
                Assert.Equal(1, assignments.Attempts);
                Assert.NotNull(assignments.FenceToken);

                SetStage("finalising-observation-denial");
                using HttpRequestMessage deniedObservation = CreateObservationRequest(
                    enrolled.AgentId,
                    instanceId,
                    owner.Now,
                    "1",
                    AgentVersion);
                using HttpResponseMessage deniedObservationResponse = await directClient.SendAsync(
                    deniedObservation,
                    cancellationToken);
                observationDeniedAfterRevocation = deniedObservationResponse.StatusCode is
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
                ObserveDiagnostic(
                    httpStatusCode: (int)deniedObservationResponse.StatusCode,
                    transportAttempted: true);
                Assert.True(observationDeniedAfterRevocation);
                SetStage("finalising-command-server-denial");
                using HttpResponseMessage deniedCommandResponse = await SendCommandTransportPollAsync(
                    directClient,
                    enrolled.AgentId,
                    owner.Now,
                    3,
                    CommandTransportProtocol.CurrentSchemaVersion,
                    4,
                    cancellationToken);
                bool commandServerDenied = deniedCommandResponse.StatusCode is
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
                ObserveDiagnostic(
                    httpStatusCode: (int)deniedCommandResponse.StatusCode,
                    transportAttempted: true);
                Assert.True(commandServerDenied);

                SetStage("finalising-command-local-quarantine");
                CommandTransportProcessResult deniedCommand = await RunCommandTransportHostAsync(
                    agent,
                    owner,
                    enrolled,
                    identityPackage,
                    password,
                    owner.Now,
                    "none");
                commandDeniedAfterRevocation = deniedCommand.ExitCode == 3;
                ObserveDiagnostic(
                    disposition: commandDeniedAfterRevocation ? "Denied" : "Unexpected",
                    attempts: 1,
                    transportAttempted: false);
                Assert.True(commandDeniedAfterRevocation);

                SetStage("finalising-durable-counts");
                await using AsyncServiceScope scope = owner.Services.CreateAsyncScope();
                ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
                RegisteredAgentRow row = await context.Agents.AsNoTracking()
                    .SingleAsync(value => value.AgentId == enrolled.AgentId, cancellationToken);
                Assert.Equal("Revoked", row.State);
                observationSamples = await context.HealthSamples.CountAsync(cancellationToken);
                commandJournalEntries = await context.CommandTransportJournal.CountAsync(cancellationToken);
                commandAttempts = await context.CommandAttempts.CountAsync(cancellationToken);
                Assert.Equal(2, observationSamples);
                Assert.Equal(2, commandJournalEntries);
                Assert.Equal(0, commandAttempts);
                Assert.Single(notificationSink.Requests);
                Assert.NotNull(identityStore.GetClient(enrolled.IdentityReference));
                finalised = true;
                SetStage("finalised");
            }
            finally
            {
                operationGate.Release();
            }
        }

        /// <summary>Records one real snapshot request without retaining headers, bodies or subject identifiers.</summary>
        /// <param name="context">Current loopback request.</param>
        /// <param name="next">Remaining immutable endpoint pipeline.</param>
        /// <returns>A task completing after evidence counters are updated.</returns>
        public async Task RecordSnapshotRequestAsync(HttpContext context, RequestDelegate next)
        {
            if (!string.Equals(
                    context.Request.Path.Value,
                    DashboardTvSandboxEndpointRouteBuilderExtensions.SnapshotRoute,
                    StringComparison.Ordinal) ||
                !initialised)
            {
                await next(context);
                return;
            }

            DateTimeOffset startedAt = DateTimeOffset.UtcNow;
            int active = Interlocked.Increment(ref activeSnapshotRequests);
            UpdateMaximum(ref maximumSnapshotConcurrency, active);
            bool conditional = context.Request.Headers.ContainsKey("If-None-Match");
            bool cancelled = false;
            try
            {
                await next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                cancelled = true;
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref activeSnapshotRequests);
                lock (evidenceGate)
                {
                    if (snapshotRequests.Count < MaximumSnapshotEvidence)
                    {
                        snapshotRequests.Add(new ConsolidatedSnapshotRequestEvidence(
                            startedAt,
                            DateTimeOffset.UtcNow,
                            context.Response.StatusCode,
                            conditional,
                            cancelled));
                    }
                    else
                    {
                        stage = "snapshot-evidence-budget-exceeded";
                    }
                }
            }
        }

        /// <summary>Checks HTTPS loopback, exact run correlation and a single bounded public header.</summary>
        /// <param name="context">Current authenticated request.</param>
        /// <returns><see langword="true"/> only for this run.</returns>
        public bool IsExactRun(HttpContext context) =>
            context.Request.IsHttps &&
            context.Connection.RemoteIpAddress is not null &&
            IPAddress.IsLoopback(context.Connection.RemoteIpAddress) &&
            context.Request.Headers.TryGetValue(ConsolidatedRunHeader, out var values) &&
            values.Count == 1 &&
            string.Equals(values[0], RunId, StringComparison.Ordinal);

        /// <summary>Builds one immutable sanitised evidence response.</summary>
        /// <param name="hints">Aggregate SignalR counters containing no connection identifier.</param>
        /// <returns>Current correlated evidence without secrets or payloads.</returns>
        public ConsolidatedHarnessEvidence Snapshot(DashboardTvChangeHintSandboxEvidenceSnapshot hints)
        {
            lock (evidenceGate)
            {
                return new ConsolidatedHarnessEvidence(
                    RunId,
                    registration?.AgentId.ToString("D"),
                    instanceId == Guid.Empty ? null : instanceId.ToString("D"),
                    stage,
                    initialised,
                    advanced,
                    finalised,
                    initialReplayObserved,
                    observationSamples,
                    notificationSink.Requests.Length,
                    notificationRestartDeduplicated,
                    commandJournalEntries,
                    commandAttempts,
                    heartbeatDeniedAfterRevocation,
                    assignmentsDeniedAfterRevocation,
                    observationDeniedAfterRevocation,
                    commandDeniedAfterRevocation,
                    centralRevocationCommitted,
                    revokedCertificateCount,
                    heartbeatServerDeniedAfterRevocation,
                    assignmentsServerDeniedAfterRevocation,
                    heartbeatTransportAttemptedAfterRevocation,
                    assignmentsTransportAttemptedAfterRevocation,
                    assignmentLastKnownGoodPreserved,
                    commandProcessesExitedBeforeRevocation,
                    heartbeatFenceAfterRevocation,
                    assignmentsFenceAfterRevocation,
                    Volatile.Read(ref activeSnapshotRequests),
                    Volatile.Read(ref maximumSnapshotConcurrency),
                    snapshotRequests.ToArray(),
                    hints,
                    failure);
            }
        }

        /// <summary>Freezes one sanitised failure envelope without retaining exception text or stack data.</summary>
        /// <param name="exception">Failure whose type alone contributes to the closed category.</param>
        /// <returns>Bounded failure evidence safe for the loopback response.</returns>
        public ConsolidatedHarnessFailureEvidence RecordFailure(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            lock (evidenceGate)
            {
                failure ??= ConsolidatedHarnessFailureEvidence.Create(stage, diagnostic, exception);
                return failure;
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            notificationCoordinator?.Dispose();
            if (notificationLedger is not null)
            {
                await notificationLedger.DisposeAsync();
            }
            notificationClient?.Dispose();
            if (identities is not null)
            {
                await identities.DisposeAsync();
            }
            if (local is not null)
            {
                await local.DisposeAsync();
            }
            if (pkcs12 is not null)
            {
                CryptographicOperations.ZeroMemory(pkcs12);
            }
            identityPassword = null;
            operationGate.Dispose();
        }

        /// <summary>Creates the first or restarted notification reader over the same Server projection and ledger.</summary>
        private async Task CreateNotificationConsumerAsync()
        {
            AgentFleetSandbox owner = Require(sandbox);
            AgentFileSandbox agent = Require(local);
            notificationClient = owner.CreateDashboardTvClient();
            HttpReconciledNotificationTransitionReader reader = new(
                notificationClient,
                owner.SandboxTimeProvider);
            notificationLedger = new FileReconciledNotificationLedger(
                Path.Combine(agent.RootPath, "notification-ledger"));
            notificationCoordinator = new ReconciledNotificationCoordinator(
                reader,
                notificationLedger,
                notificationSink,
                owner.SandboxTimeProvider);
            await Task.CompletedTask;
        }

        /// <summary>Reopens the same durable cursor and proves the committed transition is not delivered twice.</summary>
        /// <param name="cancellationToken">Cancellation bounding the replay read.</param>
        private async Task RestartNotificationConsumerAsync(CancellationToken cancellationToken)
        {
            notificationCoordinator?.Dispose();
            if (notificationLedger is not null)
            {
                await notificationLedger.DisposeAsync();
            }
            notificationClient?.Dispose();
            notificationCoordinator = null;
            notificationLedger = null;
            notificationClient = null;
            await CreateNotificationConsumerAsync();
            ReconciledNotificationCycleResult repeated = await notificationCoordinator!.RunOnceAsync(
                new ReconciledNotificationSandboxPolicy(true, false),
                cancellationToken);
            Assert.Equal(ReconciledNotificationCycleDisposition.NoChanges, repeated.Disposition);
            Assert.Single(notificationSink.Requests);
            notificationRestartDeduplicated = true;
        }

        /// <summary>Updates the textual stage under the evidence lock.</summary>
        /// <param name="value">Bounded non-secret stage identifier.</param>
        public void SetStage(string value)
        {
            lock (evidenceGate)
            {
                stage = value;
                diagnostic = ConsolidatedOperationDiagnostic.Empty(value);
            }
        }

        /// <summary>Updates only the bounded typed facts associated with the current test boundary.</summary>
        private void ObserveDiagnostic(
            int? httpStatusCode = null,
            string? disposition = null,
            string? errorCode = null,
            int? attempts = null,
            long? fenceToken = null,
            string? centralAgentState = null,
            string? centralCertificateState = null,
            string? localIdentityState = null,
            bool? transportAttempted = null,
            int? expectedCount = null,
            int? actualCount = null)
        {
            lock (evidenceGate)
            {
                diagnostic = new ConsolidatedOperationDiagnostic(
                    diagnostic.Boundary,
                    httpStatusCode,
                    disposition,
                    errorCode,
                    attempts,
                    fenceToken,
                    centralAgentState,
                    centralCertificateState,
                    localIdentityState,
                    transportAttempted,
                    expectedCount,
                    actualCount);
            }
        }

        /// <summary>Fails closed when one ordered harness stage is invoked out of sequence.</summary>
        /// <param name="condition">Required state predicate.</param>
        /// <param name="message">Sanitised failure text.</param>
        private static void RequireState(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>Returns a required harness reference without exposing its contents.</summary>
        /// <typeparam name="T">Required reference type.</typeparam>
        /// <param name="value">Potentially absent state.</param>
        /// <returns>The available reference.</returns>
        private static T Require<T>(T? value) where T : class =>
            value ?? throw new InvalidOperationException("The consolidated harness state is incomplete.");

        /// <summary>Updates one concurrent maximum without a lock.</summary>
        /// <param name="maximum">Maximum field.</param>
        /// <param name="candidate">New candidate value.</param>
        private static void UpdateMaximum(ref int maximum, int candidate)
        {
            int observed;
            do
            {
                observed = Volatile.Read(ref maximum);
                if (candidate <= observed)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref maximum, candidate, observed) != observed);
        }
    }

    /// <summary>Accepts notification requests only into bounded in-memory test evidence.</summary>
    private sealed class ConsolidatedRecordingSink : IReconciledNotificationSink
    {
        private const int MaximumRequests = 2;
        private readonly List<ReconciledNotificationDeliveryRequest> requests = [];

        /// <summary>Gets a stable snapshot of accepted synthetic requests.</summary>
        public ReconciledNotificationDeliveryRequest[] Requests
        {
            get
            {
                lock (requests)
                {
                    return requests.ToArray();
                }
            }
        }

        /// <inheritdoc />
        public ValueTask<ReconciledNotificationDeliveryResult> DeliverAsync(
            ReconciledNotificationDeliveryRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (requests)
            {
                if (requests.Count >= MaximumRequests)
                {
                    return ValueTask.FromResult(new ReconciledNotificationDeliveryResult(
                        ReconciledNotificationDeliveryDisposition.FailedTerminal,
                        "delivery.test_budget_exceeded"));
                }
                requests.Add(request);
            }

            return ValueTask.FromResult(new ReconciledNotificationDeliveryResult(
                ReconciledNotificationDeliveryDisposition.Accepted,
                "delivery.test_accepted"));
        }
    }

    /// <summary>Contains one sanitised snapshot-request timing record.</summary>
    /// <param name="StartedAt">Real UTC request start.</param>
    /// <param name="CompletedAt">Real UTC request completion.</param>
    /// <param name="StatusCode">Local HTTP response status.</param>
    /// <param name="HadConditionalTag">Whether the browser sent `If-None-Match`.</param>
    /// <param name="Cancelled">Whether the request was cancelled by its browser session.</param>
    private sealed record ConsolidatedSnapshotRequestEvidence(
        DateTimeOffset StartedAt,
        DateTimeOffset CompletedAt,
        int StatusCode,
        bool HadConditionalTag,
        bool Cancelled);

    /// <summary>Contains only public identifiers, counters and dispositions for one consolidated run.</summary>
    private sealed record ConsolidatedHarnessEvidence(
        string RunId,
        string? AgentId,
        string? InstanceId,
        string Stage,
        bool Initialised,
        bool Advanced,
        bool Finalised,
        bool InitialReplayObserved,
        int ObservationSamples,
        int NotificationDeliveries,
        bool NotificationRestartDeduplicated,
        int CommandJournalEntries,
        int CommandAttempts,
        bool HeartbeatDeniedAfterRevocation,
        bool AssignmentsDeniedAfterRevocation,
        bool ObservationDeniedAfterRevocation,
        bool CommandDeniedAfterRevocation,
        bool CentralRevocationCommitted,
        int RevokedCertificateCount,
        bool HeartbeatServerDeniedAfterRevocation,
        bool AssignmentsServerDeniedAfterRevocation,
        bool HeartbeatTransportAttemptedAfterRevocation,
        bool AssignmentsTransportAttemptedAfterRevocation,
        bool AssignmentLastKnownGoodPreserved,
        bool CommandProcessesExitedBeforeRevocation,
        long? HeartbeatFenceAfterRevocation,
        long? AssignmentsFenceAfterRevocation,
        int ActiveSnapshotRequests,
        int MaximumSnapshotConcurrency,
        IReadOnlyList<ConsolidatedSnapshotRequestEvidence> SnapshotRequests,
        DashboardTvChangeHintSandboxEvidenceSnapshot SignalR,
        ConsolidatedHarnessFailureEvidence? Failure);

    /// <summary>Contains the current bounded diagnostic context before it becomes terminal failure evidence.</summary>
    private sealed record ConsolidatedOperationDiagnostic(
        string Boundary,
        int? HttpStatusCode,
        string? Disposition,
        string? ErrorCode,
        int? Attempts,
        long? FenceToken,
        string? CentralAgentState,
        string? CentralCertificateState,
        string? LocalIdentityState,
        bool? TransportAttempted,
        int? ExpectedCount,
        int? ActualCount)
    {
        /// <summary>Creates an empty diagnostic for one stable non-secret boundary.</summary>
        public static ConsolidatedOperationDiagnostic Empty(string boundary) => new(
            boundary,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    /// <summary>Exposes only whitelisted failure facts and never exception messages, paths, headers or identity material.</summary>
    private sealed record ConsolidatedHarnessFailureEvidence(
        string Stage,
        string Boundary,
        string Category,
        int? HttpStatusCode,
        string? Disposition,
        string? ErrorCode,
        int? Attempts,
        long? FenceToken,
        string? CentralAgentState,
        string? CentralCertificateState,
        string? LocalIdentityState,
        bool? TransportAttempted,
        int? ExpectedCount,
        int? ActualCount)
    {
        /// <summary>Maps one exception type and the current typed context to a closed sanitised category.</summary>
        public static ConsolidatedHarnessFailureEvidence Create(
            string stage,
            ConsolidatedOperationDiagnostic diagnostic,
            Exception exception)
        {
            Type exceptionType = exception.GetType();
            string typeName = exceptionType.Name;
            string category = exception is OperationCanceledException
                ? "budget-exceeded"
                : string.Equals(diagnostic.ErrorCode, "agent_fleet.operation_busy", StringComparison.Ordinal)
                    ? "lease-busy"
                    : typeName is "DbUpdateConcurrencyException" or "DbUpdateException" or "SqliteException"
                        ? "persistence-conflict"
                        : exceptionType.Namespace?.StartsWith("Xunit", StringComparison.Ordinal) == true
                            ? "assertion-mismatch"
                            : "unexpected";
            return new ConsolidatedHarnessFailureEvidence(
                stage,
                diagnostic.Boundary,
                category,
                diagnostic.HttpStatusCode,
                diagnostic.Disposition,
                diagnostic.ErrorCode,
                diagnostic.Attempts,
                diagnostic.FenceToken,
                diagnostic.CentralAgentState,
                diagnostic.CentralCertificateState,
                diagnostic.LocalIdentityState,
                diagnostic.TransportAttempted,
                diagnostic.ExpectedCount,
                diagnostic.ActualCount);
        }
    }

    /// <summary>Records whether each Agent Fleet transport boundary was actually called by the local coordinator.</summary>
    private sealed class CountingAgentFleetClientTransport(IAgentFleetClientTransport inner)
        : IAgentFleetClientTransport
    {
        private int heartbeatCalls;
        private int assignmentCalls;

        /// <summary>Gets the number of heartbeat calls delegated to HTTPS.</summary>
        public int HeartbeatCalls => Volatile.Read(ref heartbeatCalls);

        /// <summary>Gets the number of assignment calls delegated to HTTPS.</summary>
        public int AssignmentCalls => Volatile.Read(ref assignmentCalls);

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentEnrollmentOutcome>> EnrolAsync(
            string enrollmentToken,
            AgentEnrollmentRequest request,
            CancellationToken cancellationToken) => inner.EnrolAsync(enrollmentToken, request, cancellationToken);

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentHeartbeatOutcome>> SendHeartbeatAsync(
            string identityReference,
            AgentHeartbeatRequest request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref heartbeatCalls);
            return inner.SendHeartbeatAsync(identityReference, request, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentAssignmentSnapshot>> GetAssignmentsAsync(
            string identityReference,
            Guid agentId,
            string agentVersion,
            string? entityTag,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref assignmentCalls);
            return inner.GetAssignmentsAsync(identityReference, agentId, agentVersion, entityTag, cancellationToken);
        }
    }

    /// <summary>Captures the typed revocation response without retaining body, headers or identity material.</summary>
    private sealed record AgentRevocationHttpEvidence(
        HttpStatusCode StatusCode,
        AgentRevocationOutcome? Outcome);

    /// <summary>Captures bounded durable central revocation facts for one synthetic Agent.</summary>
    private sealed record ConsolidatedCentralRevocationEvidence(
        string AgentState,
        DateTimeOffset? RevokedAt,
        int CertificateCount,
        int RevokedCertificateCount)
    {
        /// <summary>Gets whether every bounded certificate row is durably revoked.</summary>
        public bool AllCertificatesRevoked => CertificateCount > 0 && RevokedCertificateCount == CertificateCount;
    }
}
