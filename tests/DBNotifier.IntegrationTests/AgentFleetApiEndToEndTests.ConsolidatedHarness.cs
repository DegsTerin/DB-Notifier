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
    private const string HumanEvidencePageRoute = "/__dbnotifier-state06-human-remediation/evidence-page";
    private const string HumanAgentLossRoute = "/__dbnotifier-state06-human-remediation/agent-loss";
    private const string HumanAgentRecoveryRoute = "/__dbnotifier-state06-human-remediation/agent-recovery";
    private const string HumanVisualTruthRoute = "/__dbnotifier-state06-human-remediation/visual-truth";
    private const string HumanCompleteRoute = "/__dbnotifier-state06-human-remediation/complete";
    private const string ConsolidatedRunHeader = "X-DBN-State06-Consolidated-Run";
    private const string ConsolidatedHarnessRateLimitPolicy = "ConsolidatedHarnessRateLimit";
    private const string HumanQualityGateSample = "quality-gate";
    private const string HumanSample001 = "S06-HG-001";
    private const string HumanSample006 = "S06-HG-006";

    /// <summary>
    /// Starts one local-only consolidated host, prepares the first correlated observation and waits for the
    /// authenticated browser auditor to advance, finalise and stop the run.
    /// </summary>
    /// <param name="dashboardRoot">Absolute root of the already-built local Dashboard.</param>
    /// <param name="runId">Runner-owned version-four correlation identifier.</param>
    /// <param name="cancellationToken">Cancellation bounding the temporary host lifetime.</param>
    /// <returns>Zero after complete disposal, or an exception for the owning executable to sanitise.</returns>
    public static async Task<int> RunState06ConsolidatedSandboxHostAsync(
        string dashboardRoot,
        Guid runId,
        CancellationToken cancellationToken) =>
        await RunState06ConsolidatedSandboxHostCoreAsync(
            dashboardRoot,
            humanRemediationMode: false,
            humanReviewSample: null,
            runId,
            cancellationToken);

    /// <summary>
    /// Starts the exact test-only mode that makes the two blocked final human samples observable without changing
    /// product composition or pre-filling a human decision.
    /// </summary>
    /// <param name="dashboardRoot">Absolute root of the already-built local Dashboard.</param>
    /// <param name="runId">Runner-owned version-four correlation identifier.</param>
    /// <param name="cancellationToken">Cancellation bounding the temporary host lifetime.</param>
    /// <returns>Zero after complete disposal, or an exception for the owning executable to sanitise.</returns>
    public static async Task<int> RunState06FinalHumanSamplesRemediationHostAsync(
        string dashboardRoot,
        Guid runId,
        CancellationToken cancellationToken) =>
        await RunState06FinalHumanSamplesRemediationHostAsync(
            dashboardRoot,
            HumanQualityGateSample,
            runId,
            cancellationToken);

    /// <summary>
    /// Starts one exact automatic or single-sample test-only presentation mode without changing product composition.
    /// </summary>
    /// <param name="dashboardRoot">Absolute root of the already-built local Dashboard.</param>
    /// <param name="humanReviewSample">Exact quality-gate or single-sample selector.</param>
    /// <param name="runId">Runner-owned version-four correlation identifier.</param>
    /// <param name="cancellationToken">Cancellation bounding the temporary host lifetime.</param>
    /// <returns>Zero after complete disposal, or an exception for the owning executable to sanitise.</returns>
    public static async Task<int> RunState06FinalHumanSamplesRemediationHostAsync(
        string dashboardRoot,
        string humanReviewSample,
        Guid runId,
        CancellationToken cancellationToken) =>
        await RunState06ConsolidatedSandboxHostCoreAsync(
            dashboardRoot,
            humanRemediationMode: true,
            humanReviewSample,
            runId,
            cancellationToken);

    /// <summary>Runs one exact consolidated host mode behind the shared local process boundary.</summary>
    /// <param name="dashboardRoot">Absolute root of the already-built local Dashboard.</param>
    /// <param name="humanRemediationMode">Whether only the final-human-sample evidence controls are exposed.</param>
    /// <param name="humanReviewSample">Exact quality-gate or single-sample selector when remediation mode is enabled.</param>
    /// <param name="runId">Runner-owned version-four correlation identifier.</param>
    /// <param name="cancellationToken">Cancellation bounding the temporary host lifetime.</param>
    /// <returns>Zero after complete disposal.</returns>
    private static async Task<int> RunState06ConsolidatedSandboxHostCoreAsync(
        string dashboardRoot,
        bool humanRemediationMode,
        string? humanReviewSample,
        Guid runId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dashboardRoot);
        string fullDashboardRoot = Path.GetFullPath(dashboardRoot);
        if (!Directory.Exists(fullDashboardRoot) ||
            !File.Exists(Path.Combine(fullDashboardRoot, "index.html")))
        {
            throw new ArgumentException("The consolidated Dashboard root is unavailable.", nameof(dashboardRoot));
        }

        ConsolidatedHarnessState state = new(runId, humanRemediationMode, humanReviewSample);
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
                mode = humanRemediationMode ? "final-human-samples-remediation" : "consolidated",
                humanReviewSample = state.HumanReviewSample,
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
            .RequireRateLimiting(ConsolidatedHarnessRateLimitPolicy);

        if (state.HumanRemediationMode)
        {
            application.MapGet(HumanEvidencePageRoute, (HttpContext context) =>
            {
                if (!state.IsExactRun(context))
                {
                    return Results.Unauthorized();
                }

                context.Response.Headers["Content-Security-Policy"] =
                    "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; " +
                    "connect-src 'self'; frame-src 'self'; base-uri 'none'; form-action 'none'";
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["Referrer-Policy"] = "no-referrer";
                return Results.Content(
                    ConsolidatedHarnessState.BuildHumanEvidencePage(),
                    "text/html; charset=utf-8");
            })
                .RequireAuthorization(DashboardTvSandboxEndpointRouteBuilderExtensions.ReadPolicy)
                .RequireRateLimiting(ConsolidatedHarnessRateLimitPolicy);

            MapHumanControl(application, HumanAgentLossRoute, state, state.BeginHumanAgentLossAsync);
            MapHumanControl(application, HumanAgentRecoveryRoute, state, state.RecoverHumanAgentAsync);
            MapHumanControl(application, HumanVisualTruthRoute, state, state.EnableHumanVisualTruthAsync);
            MapHumanControl(application, HumanCompleteRoute, state, state.CompleteHumanRemediationAsync);
        }
        else
        {
            Func<HttpContext, Task<IResult>> advance = context =>
                ExecuteControlAsync(context, state, state.AdvanceAsync);
            application.MapPost(ConsolidatedAdvanceRoute, advance)
                .RequireAuthorization(DashboardTvSandboxEndpointRouteBuilderExtensions.ReadPolicy)
                .RequireRateLimiting(ConsolidatedHarnessRateLimitPolicy);

            Func<HttpContext, Task<IResult>> finalise = context =>
                ExecuteControlAsync(context, state, state.FinaliseAsync);
            application.MapPost(ConsolidatedFinaliseRoute, finalise)
                .RequireAuthorization(DashboardTvSandboxEndpointRouteBuilderExtensions.ReadPolicy)
                .RequireRateLimiting(ConsolidatedHarnessRateLimitPolicy);
        }

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
            .RequireRateLimiting(ConsolidatedHarnessRateLimitPolicy);
    }

    /// <summary>Maps one exact, authenticated and serial test-only remediation control.</summary>
    /// <param name="application">Loopback-only application owned by the fixture.</param>
    /// <param name="route">Exact non-operational route.</param>
    /// <param name="state">Owning correlated evidence state.</param>
    /// <param name="operation">Bounded state transition.</param>
    private static void MapHumanControl(
        WebApplication application,
        string route,
        ConsolidatedHarnessState state,
        Func<CancellationToken, Task> operation)
    {
        Func<HttpContext, Task<IResult>> control = context =>
            ExecuteControlAsync(context, state, operation);
        application.MapPost(route, control)
            .RequireAuthorization(DashboardTvSandboxEndpointRouteBuilderExtensions.ReadPolicy)
            .RequireRateLimiting(ConsolidatedHarnessRateLimitPolicy);
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
    private sealed class ConsolidatedHarnessState : IAsyncDisposable
    {
        private const int MaximumSnapshotEvidence = 32;
        private const int MaximumHumanEvidenceRequestHistory = 512;
        private const int HumanEvidenceRefreshMilliseconds = 2100;
        private readonly Guid runId;
        private readonly bool humanRemediationMode;
        private readonly string humanReviewSample;
        private readonly object evidenceGate = new();
        private readonly SemaphoreSlim operationGate = new(1, 1);
        private readonly List<ConsolidatedSnapshotRequestEvidence> snapshotRequests = [];
        private readonly List<DateTimeOffset> humanEvidenceRequestStarts = [];
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
        private int humanPendingObservations;
        private int humanServerObservationSamples;
        private int activeHumanEvidenceRequests;
        private int maximumHumanEvidenceConcurrency;
        private int humanEvidenceRateLimitedResponses;
        private bool initialReplayObserved;
        private bool humanLossObserved;
        private bool humanReplayAcceptedOnce;
        private bool humanVisualTruthEnabled;
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
        private string humanAgentTransportState = "not-started";
        private ConsolidatedOperationDiagnostic diagnostic = ConsolidatedOperationDiagnostic.Empty("created");
        private ConsolidatedHarnessFailureEvidence? failure;
        private DashboardTvSnapshot? humanVisualTruthSnapshot;
        private DateTimeOffset? humanVisualTruthExpiresAt;

        /// <summary>Creates one isolated evidence state with an optional final-human-sample control surface.</summary>
        /// <param name="runId">Public version-four correlation identifier.</param>
        /// <param name="humanRemediationMode">Whether command controls are replaced by test-only human evidence controls.</param>
        /// <param name="humanReviewSample">Exact quality-gate or single-sample selector.</param>
        public ConsolidatedHarnessState(
            Guid runId,
            bool humanRemediationMode = false,
            string? humanReviewSample = null)
        {
            if (runId == Guid.Empty)
            {
                throw new ArgumentException("The consolidated run identifier is required.", nameof(runId));
            }

            if (!humanRemediationMode && humanReviewSample is not null)
            {
                throw new ArgumentException(
                    "A human review sample requires the exact remediation mode.",
                    nameof(humanReviewSample));
            }

            this.runId = runId;
            this.humanRemediationMode = humanRemediationMode;
            this.humanReviewSample = humanRemediationMode
                ? ValidateHumanReviewSample(humanReviewSample ?? HumanQualityGateSample)
                : "none";
        }

        /// <summary>Gets the public correlation identifier for this fixture run.</summary>
        public string RunId => runId.ToString("D");

        /// <summary>Gets whether this run exposes only the final-human-sample remediation controls.</summary>
        public bool HumanRemediationMode => humanRemediationMode;

        /// <summary>Gets the exact automatic or single-sample presentation selector.</summary>
        public string HumanReviewSample => humanReviewSample;

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
                local = await AgentFileSandbox.StartConsolidatedAsync(runId);
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
                if (humanRemediationMode)
                {
                    ObservationSandboxChildResult initial = await RunObservationSandboxHostAsync(
                        local,
                        owner,
                        registration,
                        instanceId,
                        pkcs12,
                        identityPassword,
                        owner.BaseAddress,
                        owner.Now,
                        nameof(DBNotifier.Domain.HealthStatus.Degraded),
                        "normal");
                    Assert.Equal(new ObservationSandboxChildResult(1, 1, 1, 0), initial);
                    observationSamples = await CountServerObservationSamplesAsync(cancellationToken);
                    Assert.Equal(1, observationSamples);
                    humanServerObservationSamples = observationSamples;

                    using HttpClient initialDashboard = owner.CreateDashboardTvClient();
                    (DashboardTvSnapshot initialSnapshot, _) = await ReadDashboardSnapshotAsync(
                        initialDashboard,
                        cancellationToken);
                    DashboardTvInventoryItem initialItem = Assert.Single(initialSnapshot.Items);
                    Assert.Equal(instanceId, initialItem.InstanceId);
                    Assert.Equal("degraded", initialItem.Status);

                    humanAgentTransportState = "available";
                    initialised = true;
                    SetStage("agent-transport-ready");
                    return;
                }

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

        /// <summary>Creates one pending synthetic observation while the browser/API path remains online.</summary>
        /// <param name="cancellationToken">Cancellation bounding the child process and durable checks.</param>
        /// <returns>A task completing only after one pending local observation and zero new Server sample are proved.</returns>
        public async Task BeginHumanAgentLossAsync(CancellationToken cancellationToken)
        {
            await operationGate.WaitAsync(cancellationToken);
            try
            {
                RequireState(
                    humanRemediationMode &&
                    humanReviewSample is HumanQualityGateSample or HumanSample001 &&
                    initialised &&
                    !finalised,
                    "The human Agent-loss stage is unavailable.");
                if (humanLossObserved)
                {
                    return;
                }

                SetStage("agent-transport-unavailable");
                AgentFleetSandbox owner = Require(sandbox);
                AgentFileSandbox agent = Require(local);
                AgentLocalRegistration enrolled = Require(registration);
                owner.Advance(TimeSpan.FromSeconds(31));
                ObservationSandboxChildResult offline = await RunObservationSandboxHostAsync(
                    agent,
                    owner,
                    enrolled,
                    instanceId,
                    Require(pkcs12),
                    Require(identityPassword),
                    GetUnusedLoopbackHttpsAddress(),
                    owner.Now,
                    nameof(DBNotifier.Domain.HealthStatus.Unavailable),
                    "normal");
                Assert.Equal(new ObservationSandboxChildResult(1, 1, 0, 1), offline);

                HumanLocalObservationEvidence localEvidence = await agent.ReadHumanObservationEvidenceAsync();
                Assert.Equal(2, localEvidence.ObservationCount);
                Assert.Equal(1, localEvidence.PendingCount);
                Assert.Equal(1, localEvidence.AcknowledgedCount);
                int serverCount = await CountServerObservationSamplesAsync(cancellationToken);
                Assert.Equal(1, serverCount);

                humanPendingObservations = localEvidence.PendingCount;
                humanServerObservationSamples = serverCount;
                humanAgentTransportState = "unavailable";
                humanLossObserved = true;
                SetStage("agent-observation-pending");
            }
            finally
            {
                operationGate.Release();
            }
        }

        /// <summary>Restores only Agent-to-API loopback transport and proves the pending observation is accepted once.</summary>
        /// <param name="cancellationToken">Cancellation bounding replay and durable checks.</param>
        /// <returns>A task completing after local acknowledgement and one new Server sample are proved.</returns>
        public async Task RecoverHumanAgentAsync(CancellationToken cancellationToken)
        {
            await operationGate.WaitAsync(cancellationToken);
            try
            {
                RequireState(
                    humanRemediationMode &&
                    humanReviewSample is HumanQualityGateSample or HumanSample001 &&
                    initialised &&
                    humanLossObserved &&
                    !finalised,
                    "The human Agent-recovery stage is unavailable.");
                if (humanReplayAcceptedOnce)
                {
                    return;
                }

                SetStage("agent-transport-recovered");
                AgentFleetSandbox owner = Require(sandbox);
                AgentFileSandbox agent = Require(local);
                owner.Advance(TimeSpan.FromSeconds(3));
                ObservationSandboxChildResult replay = await RunObservationSandboxHostAsync(
                    agent,
                    owner,
                    Require(registration),
                    instanceId,
                    Require(pkcs12),
                    Require(identityPassword),
                    owner.BaseAddress,
                    owner.Now,
                    "none",
                    "normal");
                Assert.Equal(new ObservationSandboxChildResult(0, 1, 1, 0), replay);

                HumanLocalObservationEvidence localEvidence = await agent.ReadHumanObservationEvidenceAsync();
                Assert.Equal(2, localEvidence.ObservationCount);
                Assert.Equal(0, localEvidence.PendingCount);
                Assert.Equal(2, localEvidence.AcknowledgedCount);
                int serverCount = await CountServerObservationSamplesAsync(cancellationToken);
                Assert.Equal(2, serverCount);

                using HttpClient dashboard = owner.CreateDashboardTvClient();
                (DashboardTvSnapshot snapshot, _) = await ReadDashboardSnapshotAsync(
                    dashboard,
                    cancellationToken);
                DashboardTvInventoryItem item = Assert.Single(snapshot.Items);
                Assert.Equal("unavailable", item.Status);

                humanPendingObservations = 0;
                humanServerObservationSamples = serverCount;
                observationSamples = serverCount;
                humanAgentTransportState = "recovered";
                humanReplayAcceptedOnce = true;
                initialReplayObserved = true;
                SetStage("agent-replay-accepted-once");
            }
            finally
            {
                operationGate.Release();
            }
        }

        /// <summary>Activates the bounded `unknown`/stale visual fixture without changing normal Dashboard data.</summary>
        /// <param name="cancellationToken">Cancellation checked before the immutable snapshot is published.</param>
        /// <returns>A completed task after the fixture is validated and fenced.</returns>
        public async Task EnableHumanVisualTruthAsync(CancellationToken cancellationToken)
        {
            await operationGate.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                RequireState(
                    humanRemediationMode &&
                    !finalised &&
                    ((humanReviewSample == HumanQualityGateSample && humanReplayAcceptedOnce) ||
                     (humanReviewSample == HumanSample006 && initialised && !humanLossObserved)),
                    "The human visual-truth stage is unavailable.");
                if (humanVisualTruthEnabled)
                {
                    return;
                }

                AgentFleetSandbox owner = Require(sandbox);
                DashboardTvSnapshot snapshot = CreateHumanVisualTruthSnapshot(owner.Now);
                Assert.True(DashboardTvSnapshotValidator.TryValidate(snapshot, owner.Now, out string errorCode), errorCode);
                lock (evidenceGate)
                {
                    humanVisualTruthSnapshot = snapshot;
                    humanVisualTruthEnabled = true;
                    humanVisualTruthExpiresAt = owner.Now.AddMinutes(5);
                }
                SetStage("visual-truth-ready");
            }
            finally
            {
                operationGate.Release();
            }
        }

        /// <summary>Closes the remediation state only after both blocked-sample evidences are ready.</summary>
        /// <param name="cancellationToken">Cancellation bounding the final zero-execution assertion.</param>
        /// <returns>A task completing after the terminal state is fenced.</returns>
        public async Task CompleteHumanRemediationAsync(CancellationToken cancellationToken)
        {
            await operationGate.WaitAsync(cancellationToken);
            try
            {
                RequireState(
                    humanRemediationMode &&
                    ((humanReviewSample == HumanQualityGateSample && humanReplayAcceptedOnce && humanVisualTruthEnabled) ||
                     (humanReviewSample == HumanSample001 && humanReplayAcceptedOnce) ||
                     (humanReviewSample == HumanSample006 && humanVisualTruthEnabled)),
                    "The human remediation cannot complete before the selected evidence is ready.");
                if (finalised)
                {
                    return;
                }

                await using AsyncServiceScope scope = Require(sandbox).Services.CreateAsyncScope();
                ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
                Assert.Equal(0, await context.CommandAttempts.CountAsync(cancellationToken));
                commandAttempts = 0;
                finalised = true;
                SetStage("completed");
            }
            finally
            {
                operationGate.Release();
            }
        }

        /// <summary>Returns the test-only visual snapshot only after its exact stage is enabled.</summary>
        /// <param name="fallback">Existing synthetic observation source used before visual-truth activation.</param>
        /// <param name="cancellationToken">Cancellation forwarded to the existing source.</param>
        /// <returns>The immutable visual fixture or the ordinary correlated synthetic projection.</returns>
        public ValueTask<DashboardTvSnapshot> ReadHumanSnapshotAsync(
            DashboardTvSyntheticObservationSnapshotSource fallback,
            CancellationToken cancellationToken)
        {
            lock (evidenceGate)
            {
                if (humanRemediationMode && humanVisualTruthEnabled && humanVisualTruthSnapshot is not null)
                {
                    return ValueTask.FromResult(humanVisualTruthSnapshot);
                }
            }

            return fallback.ReadAsync(cancellationToken);
        }

        /// <summary>Builds one valid two-item fixture that keeps current Unknown separate from stale evidence.</summary>
        /// <param name="now">Controlled Server instant anchoring the immutable snapshot.</param>
        /// <returns>Versioned synthetic Dashboard TV snapshot.</returns>
        public static DashboardTvSnapshot CreateHumanVisualTruthSnapshot(DateTimeOffset now)
        {
            DateTimeOffset current = now.ToUniversalTime();
            DateTimeOffset stale = current.AddMinutes(-6);
            const string support = "Planejado - não implementado / Planned - not implemented";
            return new DashboardTvSnapshot(
                DashboardTvSnapshotContract.CurrentSchemaVersion,
                FormatHumanSnapshotInstant(current),
                [
                    new DashboardTvInventoryItem(
                        Guid.Parse("06000000-0000-4000-8000-000000000001"),
                        "Unknown atual - fixture sintética",
                        "fixture-unknown",
                        support,
                        "test-only",
                        "Sandbox local sintético",
                        "unknown",
                        FormatHumanSnapshotInstant(current),
                        FormatHumanSnapshotInstant(current),
                        null,
                        true),
                    new DashboardTvInventoryItem(
                        Guid.Parse("06000000-0000-4000-8000-000000000002"),
                        "Evidência desatualizada - fixture sintética",
                        "fixture-stale",
                        support,
                        "test-only",
                        "Sandbox local sintético",
                        "degraded",
                        FormatHumanSnapshotInstant(stale),
                        FormatHumanSnapshotInstant(stale),
                        25,
                        true),
                ]);
        }

        /// <summary>Builds the self-contained local evidence page without external resources or product controls.</summary>
        /// <returns>Static HTML whose script updates only text content from the sanitised evidence endpoint.</returns>
        public static string BuildHumanEvidencePage()
        {
            return $$"""
                <!doctype html>
                <html lang="pt-BR">
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>DB Notifier — evidência test-only do Agent</title>
                  <style>
                    :root { color-scheme: light dark; font-family: "Segoe UI", sans-serif; }
                    body { margin: 0; background: Canvas; color: CanvasText; }
                    header { padding: 16px 24px; border-bottom: 2px solid Highlight; }
                    header strong { display: block; font-size: 1.1rem; }
                    header span { display: block; margin-top: 4px; }
                    main { display: grid; grid-template-columns: minmax(280px, 360px) 1fr; gap: 16px; padding: 16px; }
                    section { border: 1px solid GrayText; border-radius: 8px; padding: 16px; }
                    dl { display: grid; gap: 12px; margin: 16px 0; }
                    dt { font-weight: 700; }
                    dd { margin: 3px 0 0; overflow-wrap: anywhere; }
                    .controls { display: grid; gap: 8px; margin-top: 20px; }
                    button { min-height: 44px; padding: 8px 12px; font: inherit; }
                    button:focus-visible { outline: 3px solid Highlight; outline-offset: 2px; }
                    [hidden] { display: none !important; }
                    iframe { width: 100%; min-height: 720px; border: 1px solid GrayText; border-radius: 8px; background: Canvas; }
                    .truth { font-weight: 700; }
                    @media (max-width: 900px) { main { grid-template-columns: 1fr; } iframe { min-height: 640px; } }
                  </style>
                </head>
                <body>
                  <header>
                    <strong>Evidência do harness — não é estado operacional</strong>
                    <span>Somente dados sintéticos locais; esta página não existe na composição normal.</span>
                  </header>
                  <main>
                    <section aria-labelledby="agent-evidence-title">
                      <h1 id="agent-evidence-title">Transporte do Agent — test-only</h1>
                      <p id="page-health" class="truth">Browser → API disponível</p>
                      <dl>
                        <div><dt>Etapa</dt><dd id="stage">A carregar</dd></div>
                        <div><dt>Agent → API</dt><dd id="agent-transport">A carregar</dd></div>
                        <div><dt>Observações pendentes</dt><dd id="pending">A carregar</dd></div>
                        <div><dt>Amostras aceites pelo Server</dt><dd id="server-samples">A carregar</dd></div>
                        <div><dt>Replay único</dt><dd id="replay">A carregar</dd></div>
                        <div><dt>Verdade visual</dt><dd id="visual-truth">A carregar</dd></div>
                        <div><dt>Última leitura válida</dt><dd id="last-success">Ainda não concluída</dd></div>
                        <div><dt>Expiração de unknown</dt><dd id="visual-expiry">Não ativada</dd></div>
                        <div><dt>Leituras de evidência</dt><dd id="evidence-requests">0</dd></div>
                        <div><dt>Suporte</dt><dd>Planejado — não implementado; sem homologação.</dd></div>
                        <div><dt>Origem</dt><dd>Sandbox local sintético; sem dados externos.</dd></div>
                      </dl>
                      <div class="controls" aria-label="Controles do laboratório test-only">
                        <button id="control-loss" type="button" hidden>Apresentar perda sintética do Agent</button>
                        <button id="control-recovery" type="button" hidden>Apresentar recuperação sintética do Agent</button>
                        <button id="control-visual" type="button" hidden>Apresentar unknown e stale</button>
                        <button id="control-complete" type="button" hidden>Encerrar amostra e limpar laboratório</button>
                        <p id="control-status" role="status">Nenhum controle executado.</p>
                      </div>
                    </section>
                    <section aria-labelledby="dashboard-title">
                      <h2 id="dashboard-title">Dashboard TV sandbox</h2>
                      <iframe title="Dashboard TV sandbox test-only" src="/#overview"></iframe>
                    </section>
                  </main>
                  <script>
                    (() => {
                      "use strict";
                      const evidenceRoute = {{JsonSerializer.Serialize(ConsolidatedEvidenceRoute)}};
                      const refreshDelay = {{HumanEvidenceRefreshMilliseconds}};
                      const requestDeadline = 5000;
                      const controls = {
                        "control-loss": {{JsonSerializer.Serialize(HumanAgentLossRoute)}},
                        "control-recovery": {{JsonSerializer.Serialize(HumanAgentRecoveryRoute)}},
                        "control-visual": {{JsonSerializer.Serialize(HumanVisualTruthRoute)}},
                        "control-complete": {{JsonSerializer.Serialize(HumanCompleteRoute)}}
                      };
                      let timer = null;
                      let inFlight = false;
                      let requestFence = 0;
                      let appliedFence = 0;
                      let stopped = false;
                      const set = (id, value) => { document.getElementById(id).textContent = String(value); };
                      const setHealth = (state, text) => {
                        set("page-health", text);
                        document.documentElement.dataset.browserApiState = state;
                      };
                      const schedule = (delay = refreshDelay) => {
                        if (stopped) return;
                        window.clearTimeout(timer);
                        timer = window.setTimeout(refresh, delay);
                      };
                      const retryDelay = (response) => {
                        const seconds = Number(response.headers.get("Retry-After"));
                        return Number.isFinite(seconds) && seconds > 0
                          ? Math.max(refreshDelay, seconds * 1000)
                          : refreshDelay;
                      };
                      const expose = (name, value) => {
                        document.documentElement.dataset[name] = String(value);
                      };
                      const showControl = (id, visible, enabled) => {
                        const button = document.getElementById(id);
                        button.hidden = !visible;
                        button.disabled = !enabled;
                      };
                      const updateControls = (evidence) => {
                        const sample001 = evidence.humanReviewSample === "S06-HG-001";
                        const sample006 = evidence.humanReviewSample === "S06-HG-006";
                        showControl("control-loss", sample001, evidence.stage === "agent-transport-ready");
                        showControl("control-recovery", sample001, evidence.stage === "agent-observation-pending");
                        showControl("control-visual", sample006, evidence.stage === "agent-transport-ready");
                        showControl(
                          "control-complete",
                          sample001 || sample006,
                          (sample001 && evidence.stage === "agent-replay-accepted-once") ||
                          (sample006 && evidence.stage === "visual-truth-ready"));
                      };
                      const updateExpiry = (value) => {
                        if (!value) {
                          set("visual-expiry", "Não ativada");
                          expose("sampleExpired", false);
                          return;
                        }
                        const remaining = Date.parse(value) - Date.now();
                        if (remaining <= 0) {
                          set("visual-expiry", "EXPIRADA — reinicie a amostra");
                          expose("sampleExpired", true);
                          return;
                        }
                        set("visual-expiry", `${Math.ceil(remaining / 1000)} segundos restantes`);
                        expose("sampleExpired", false);
                      };
                      const applyEvidence = (evidence) => {
                        set("stage", evidence.stage);
                        set("agent-transport", evidence.humanAgentTransportState);
                        set("pending", evidence.humanPendingObservations);
                        set("server-samples", evidence.humanServerObservationSamples);
                        set("replay", evidence.humanReplayAcceptedOnce ? "Aceite exatamente uma vez" : "Ainda não concluído");
                        set("visual-truth", evidence.humanVisualTruthEnabled ? "Unknown e stale disponíveis" : "Ainda não ativada");
                        set("evidence-requests", evidence.humanEvidenceRequests);
                        updateExpiry(evidence.humanVisualTruthExpiresAt);
                        updateControls(evidence);
                        expose("humanRemediationMode", evidence.humanRemediationMode);
                        expose("evidenceStage", evidence.stage);
                        expose("reviewSample", evidence.humanReviewSample);
                        expose("agentTransport", evidence.humanAgentTransportState);
                        expose("pendingObservations", evidence.humanPendingObservations);
                        expose("serverSamples", evidence.humanServerObservationSamples);
                        expose("lossObserved", evidence.humanLossObserved);
                        expose("replayAcceptedOnce", evidence.humanReplayAcceptedOnce);
                        expose("visualTruthEnabled", evidence.humanVisualTruthEnabled);
                        expose("finalised", evidence.finalised);
                        expose("evidenceRequests", evidence.humanEvidenceRequests);
                        expose("rateLimitedResponses", evidence.humanEvidenceRateLimitedResponses);
                        expose("activeEvidenceRequests", evidence.activeHumanEvidenceRequests);
                        expose("maximumEvidenceConcurrency", evidence.maximumHumanEvidenceConcurrency);
                        expose("maximumEvidenceRequestsPerMinute", evidence.maximumHumanEvidenceRequestsPerMinute);
                        expose("activeSnapshotRequests", evidence.activeSnapshotRequests);
                        expose("maximumSnapshotConcurrency", evidence.maximumSnapshotConcurrency);
                        expose("commandAttempts", evidence.commandAttempts);
                        expose("failurePresent", evidence.failure !== null);
                      };
                      const refresh = async () => {
                        if (inFlight || stopped) return;
                        inFlight = true;
                        const fence = ++requestFence;
                        const controller = new AbortController();
                        const deadline = window.setTimeout(() => controller.abort(), requestDeadline);
                        let nextDelay = refreshDelay;
                        try {
                          const response = await fetch(evidenceRoute, {
                            cache: "no-store",
                            credentials: "same-origin",
                            signal: controller.signal
                          });
                          if (fence < appliedFence) return;
                          if (response.status === 429) {
                            setHealth("limited", "Browser → API temporariamente limitada pelo sandbox");
                            nextDelay = retryDelay(response);
                            return;
                          }
                          if (response.status === 401 || response.status === 403) {
                            setHealth("denied", "Browser → API: acesso de teste negado");
                            return;
                          }
                          if (!response.ok) {
                            setHealth("unavailable", "Browser → API indisponível");
                            return;
                          }
                          const evidence = await response.json();
                          if (fence < appliedFence) return;
                          appliedFence = fence;
                          applyEvidence(evidence);
                          setHealth("available", "Browser → API disponível");
                          set("last-success", new Date().toLocaleTimeString("pt-BR", { hour12: false }));
                        } catch {
                          if (fence >= appliedFence) {
                            setHealth("unavailable", "Browser → API indisponível");
                          }
                        } finally {
                          window.clearTimeout(deadline);
                          inFlight = false;
                          schedule(nextDelay);
                        }
                      };
                      const invokeControl = async (button) => {
                        const route = controls[button.id];
                        if (!route || button.disabled) return;
                        document.querySelectorAll("button").forEach((candidate) => { candidate.disabled = true; });
                        set("control-status", "Transição test-only em andamento.");
                        try {
                          const response = await fetch(route, {
                            method: "POST",
                            cache: "no-store",
                            credentials: "same-origin"
                          });
                          if (!response.ok) {
                            set("control-status", `Transição recusada pelo laboratório (${response.status}).`);
                            return;
                          }
                          set("control-status", "Transição test-only concluída; aguardando evidência atualizada.");
                          schedule(0);
                        } catch {
                          set("control-status", "Transição test-only indisponível.");
                        }
                      };
                      document.querySelectorAll("button").forEach((button) => {
                        button.addEventListener("click", () => invokeControl(button));
                      });
                      window.addEventListener("pagehide", () => {
                        stopped = true;
                        window.clearTimeout(timer);
                      }, { once: true });
                      refresh();
                    })();
                  </script>
                </body>
                </html>
                """;
        }

        /// <summary>Counts only the synthetic Server samples owned by this isolated run.</summary>
        /// <param name="cancellationToken">Cancellation bounding the read.</param>
        /// <returns>Exact sample count.</returns>
        private async Task<int> CountServerObservationSamplesAsync(CancellationToken cancellationToken)
        {
            await using AsyncServiceScope scope = Require(sandbox).Services.CreateAsyncScope();
            ServerDbContext context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            return await context.HealthSamples.CountAsync(cancellationToken);
        }

        /// <summary>Formats one controlled instant for the Dashboard TV wire contract.</summary>
        /// <param name="value">UTC or offset-aware instant.</param>
        /// <returns>Canonical UTC millisecond text.</returns>
        private static string FormatHumanSnapshotInstant(DateTimeOffset value) =>
            value.UtcDateTime.ToString(
                "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                CultureInfo.InvariantCulture);

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

        /// <summary>Records bounded request-rate and response evidence for the test-only human evidence endpoint.</summary>
        /// <param name="context">Current loopback request.</param>
        /// <param name="next">Rate limiter and endpoint pipeline.</param>
        /// <returns>A task completing after counters observe the final response.</returns>
        public async Task RecordHumanEvidenceRequestAsync(HttpContext context, RequestDelegate next)
        {
            if (!humanRemediationMode ||
                !string.Equals(context.Request.Path.Value, ConsolidatedEvidenceRoute, StringComparison.Ordinal))
            {
                await next(context);
                return;
            }

            DateTimeOffset startedAt = DateTimeOffset.UtcNow;
            int active = Interlocked.Increment(ref activeHumanEvidenceRequests);
            UpdateMaximum(ref maximumHumanEvidenceConcurrency, active);
            lock (evidenceGate)
            {
                if (humanEvidenceRequestStarts.Count >= MaximumHumanEvidenceRequestHistory)
                {
                    stage = "human-evidence-request-budget-exceeded";
                }
                else
                {
                    humanEvidenceRequestStarts.Add(startedAt);
                }
            }

            try
            {
                await next(context);
            }
            finally
            {
                if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
                {
                    Interlocked.Increment(ref humanEvidenceRateLimitedResponses);
                }
                Interlocked.Decrement(ref activeHumanEvidenceRequests);
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
                    humanRemediationMode,
                    humanReviewSample,
                    humanAgentTransportState,
                    humanPendingObservations,
                    humanServerObservationSamples,
                    humanLossObserved,
                    humanReplayAcceptedOnce,
                    humanVisualTruthEnabled,
                    humanVisualTruthExpiresAt is null
                        ? null
                        : FormatHumanSnapshotInstant(humanVisualTruthExpiresAt.Value),
                    humanEvidenceRequestStarts.Count,
                    Volatile.Read(ref humanEvidenceRateLimitedResponses),
                    Volatile.Read(ref activeHumanEvidenceRequests),
                    Volatile.Read(ref maximumHumanEvidenceConcurrency),
                    CalculateMaximumHumanEvidenceRequestsPerMinute(),
                    failure);
            }
        }

        /// <summary>Calculates the largest bounded half-open sixty-second request window.</summary>
        /// <returns>Maximum evidence requests observed in any sixty-second interval.</returns>
        private int CalculateMaximumHumanEvidenceRequestsPerMinute()
        {
            int maximum = 0;
            int start = 0;
            for (int end = 0; end < humanEvidenceRequestStarts.Count; end++)
            {
                while (humanEvidenceRequestStarts[end] - humanEvidenceRequestStarts[start] >= TimeSpan.FromMinutes(1))
                {
                    start++;
                }
                maximum = Math.Max(maximum, end - start + 1);
            }
            return maximum;
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

        /// <summary>Accepts only the automatic gate or one of the two still-blocked human samples.</summary>
        /// <param name="value">Untrusted sample selector from the local process boundary.</param>
        /// <returns>The exact validated selector.</returns>
        private static string ValidateHumanReviewSample(string value) => value switch
        {
            HumanQualityGateSample => HumanQualityGateSample,
            HumanSample001 => HumanSample001,
            HumanSample006 => HumanSample006,
            _ => throw new ArgumentOutOfRangeException(
                nameof(value),
                "The human review sample selector is not authorised."),
        };

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

    /// <summary>
    /// Delegates to the existing synthetic observation projection until the exact human visual-truth stage publishes
    /// its immutable test-only snapshot.
    /// </summary>
    private sealed class ConsolidatedHumanSampleSnapshotSource(
        DashboardTvSyntheticObservationSnapshotSource fallback,
        ConsolidatedHarnessState state) : IDashboardTvSnapshotSource
    {
        /// <inheritdoc />
        public ValueTask<DashboardTvSnapshot> ReadAsync(CancellationToken cancellationToken) =>
            state.ReadHumanSnapshotAsync(fallback, cancellationToken);
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
        bool HumanRemediationMode,
        string HumanReviewSample,
        string HumanAgentTransportState,
        int HumanPendingObservations,
        int HumanServerObservationSamples,
        bool HumanLossObserved,
        bool HumanReplayAcceptedOnce,
        bool HumanVisualTruthEnabled,
        string? HumanVisualTruthExpiresAt,
        int HumanEvidenceRequests,
        int HumanEvidenceRateLimitedResponses,
        int ActiveHumanEvidenceRequests,
        int MaximumHumanEvidenceConcurrency,
        int MaximumHumanEvidenceRequestsPerMinute,
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
