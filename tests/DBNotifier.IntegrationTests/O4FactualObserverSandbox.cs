// Module purpose: Builds and serves the factual O4 Observer projection only inside the exact local test-only sandbox.
using System.Collections.ObjectModel;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBNotifier.Application.AIOps;
using DBNotifier.Application.Synchronization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DBNotifier.IntegrationTests;

/// <summary>Identifies the bounded freshness state of one projected Observer result.</summary>
internal enum O4ObserverFreshness
{
    Current,
    Stale,
    Unknown,
}

/// <summary>Retains the exact O2 and O3 content addresses needed to verify one projected result.</summary>
internal sealed record O4ObserverTrace(
    string O2EnvelopeDigest,
    string O3ResultDigest,
    string PolicyDigest,
    string CorpusManifestDigest,
    long CorpusRevision,
    string EvaluationId);

/// <summary>Exposes one deterministic threshold result without adding recommendation or execution authority.</summary>
internal sealed record O4ObserverSignal(
    string SignalId,
    string RuleId,
    string RuleVersion,
    string MetricKey,
    string Unit,
    string Disposition,
    string Severity,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset? ObservedAtUtc,
    DateTimeOffset? ValidUntilUtc,
    O4ObserverFreshness Freshness,
    double? ObservedValue,
    double Threshold,
    IReadOnlyList<Guid> EvidenceIds,
    IReadOnlyList<string> Limitations);

/// <summary>Describes whether a complete deterministic forecast is available without fabricating a prediction.</summary>
internal sealed record O4ObserverForecastAvailability(
    string State,
    string Code,
    IReadOnlyList<string> Limitations);

/// <summary>Contains the complete factual, synthetic and non-authorising Observer view model.</summary>
internal sealed record O4ObserverProjectionPayload(
    string SchemaVersion,
    string ProjectionId,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset ValidUntilUtc,
    bool ProcessingCompleted,
    bool Synthetic,
    bool Operational,
    bool ProductionRepresentative,
    bool IsAuthorising,
    bool Revoked,
    bool Superseded,
    string ActivationState,
    string SourceLabel,
    O4ObserverTrace Trace,
    IReadOnlyList<O4ObserverSignal> Signals,
    O4ObserverForecastAvailability Forecasts,
    IReadOnlyList<string> Limitations);

/// <summary>Content-addresses one exact projection so the API can reject any in-memory alteration.</summary>
internal sealed record O4ObserverProjectionPackage(
    O4ObserverProjectionPayload Payload,
    string ProjectionDigest);

/// <summary>Returns one bounded verification result without echoing projection content.</summary>
internal sealed record O4ObserverProjectionVerification(bool Accepted, string Code);

/// <summary>Builds O4 exclusively from a complete accepted O2 report and an authenticated accepted O3 result.</summary>
internal static class O4ObserverProjectionFactory
{
    internal const string ActivationMarker = "o4-factual-observer-projection-sandbox";
    internal const string PfObs1ActivationMarker = "pf-obs-1-postgresql-observer-local-test";
    internal const string SchemaVersion = "o4.observer.projection.v1";
    internal static readonly Guid AgentId = Guid.Parse("a4a10000-0000-4000-8000-000000000001");
    internal static readonly Guid InstanceId = Guid.Parse("a4a20000-0000-4000-8000-000000000001");
    private static readonly JsonSerializerOptions PfObs1JsonOptions =
        new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyList<string> ForecastLimitations = Array.AsReadOnly(new[]
    {
        "o4.limitation.forecast_unavailable",
        "o4.limitation.no_holdout_inference",
    });
    private static readonly IReadOnlyList<string> ProjectionLimitations = Array.AsReadOnly(new[]
    {
        "o4.limitation.synthetic_not_production",
        "o4.limitation.non_authorising",
        "o4.limitation.physical_accessibility_not_tested",
    });

    /// <summary>
    /// Runs the accepted synthetic O2 and O3 paths and projects their immutable outputs without retaining raw corpus
    /// members or introducing a second analysis path.
    /// </summary>
    /// <param name="rootPath">Caller-owned temporary root used only by the accepted O2 trust sandbox.</param>
    /// <param name="projectedAtUtc">Trusted projection instant used solely for projection freshness.</param>
    /// <param name="cancellationToken">Cancellation propagated through the bounded O2 pipeline.</param>
    /// <returns>A content-addressed projection plus its exact expected provenance.</returns>
    internal static async Task<O4ObserverProjectionPackage> CreateAsync(
        string rootPath,
        DateTimeOffset projectedAtUtc,
        CancellationToken cancellationToken = default)
    {
        O2AFixedTimeProvider o2Clock = new(O1SyntheticTrustFixture.NowUtc);
        using O2ACanonicalObservationPipeline pipeline =
            await O2ACanonicalObservationPipeline.CreateAsync(
                rootPath,
                AgentId,
                InstanceId,
                o2Clock,
                cancellationToken);
        ObservationBatchIngestor ingestor = new(pipeline, o2Clock);
        ObservationSyncMessage message = O2ASyntheticObservation.Create(
            AgentId,
            InstanceId,
            1,
            o2Clock.GetUtcNow(),
            messageId: Guid.Parse("a4a30000-0000-4000-8000-000000000001"),
            observationId: Guid.Parse("a4a40000-0000-4000-8000-000000000001"));
        ObservationBatchResult ingestion = await ingestor
            .HandleAsync(new ObservationBatchRequest(AgentId, [message]), cancellationToken)
            .ConfigureAwait(false);
        O2AProcessingOutcome o2 = pipeline.Outcomes.SingleOrDefault() ??
            throw new InvalidOperationException("o4.source.o2_missing");
        ObserverAnalysisReport report = pipeline.PublishedReports.SingleOrDefault() ??
            throw new InvalidOperationException("o4.source.o2_report_missing");
        if (ingestion.HighestContiguousSequence != 1 ||
            !o2.Published ||
            !report.IsComplete ||
            report.IsAuthorising ||
            report.Capabilities.ActivationState != ObserverActivationState.None)
        {
            throw new InvalidOperationException("o4.source.o2_not_accepted");
        }

        using O3ASyntheticCorpusAuthority corpusAuthority = new();
        using O3BSyntheticPolicyAuthority policyAuthority = new();
        O3ACorpusPackage corpus = corpusAuthority.Build();
        O3ACorpusVerification corpusVerification = O3ACorpusVerifier.Verify(
            corpus,
            O3ASyntheticCorpusAuthority.NowUtc);
        O3ACorpusHead corpusHead = corpusVerification.Head ??
            throw new InvalidOperationException("o4.source.o3_corpus_missing");
        O3BPartitionGate gate = new(corpus);
        DateTimeOffset frozenAt = O3ASyntheticCorpusAuthority.NowUtc.AddMinutes(1);
        O3BFrozenPolicyPackage policy = policyAuthority.Freeze(corpus, corpusHead, gate, frozenAt);
        DateTimeOffset evaluatedAt = frozenAt.AddMinutes(1);
        O3BEvaluationOutcome evaluation = O3BOfflineEvaluator.Evaluate(
            corpus,
            corpusHead,
            policy,
            policyAuthority,
            gate,
            new O3BEvaluationBudget(9, 36, 36_864, evaluatedAt.AddMinutes(1)),
            evaluatedAt,
            cancellationToken: cancellationToken);
        O3BEvaluationPackage o3 = evaluation.Package ??
            throw new InvalidOperationException("o4.source.o3_result_missing");
        if (evaluation.Disposition != O3BEvaluationDisposition.Accepted ||
            !o3.Payload.ProcessingCompleted ||
            !o3.Payload.SyntheticApproved ||
            o3.Payload.ProductionRepresentative ||
            o3.Payload.IsAuthorising)
        {
            throw new InvalidOperationException("o4.source.o3_not_accepted");
        }

        ObserverThresholdResult threshold = report.ThresholdResults.Single();
        O4ObserverSignal signal = new(
            O1CanonicalCryptography.TextDigest($"{threshold.AnalysisId:D}:{threshold.RuleId}"),
            threshold.RuleId,
            threshold.RuleVersion,
            threshold.MetricKey,
            threshold.Unit,
            threshold.Disposition.ToString(),
            threshold.PolicySeverity.ToString(),
            threshold.EvaluatedAt,
            threshold.ObservedAt,
            threshold.ValidUntil,
            ClassifyFreshness(threshold.ValidUntil, projectedAtUtc),
            threshold.ObservedValue,
            threshold.Threshold,
            Array.AsReadOnly(threshold.EvidenceIds.ToArray()),
            Array.AsReadOnly(threshold.LimitationCodes.ToArray()));
        O4ObserverTrace trace = new(
            o2.EnvelopeDigest,
            o3.ResultDigest,
            policy.PolicyDigest,
            corpusHead.ManifestDigest,
            corpusHead.Revision,
            o3.Payload.EvaluationId);
        O4ObserverProjectionPayload payload = new(
            SchemaVersion,
            O1CanonicalCryptography.TextDigest(
                $"{o2.EnvelopeDigest}:{o3.ResultDigest}:{projectedAtUtc:O}"),
            projectedAtUtc,
            projectedAtUtc.AddMinutes(5),
            true,
            true,
            false,
            false,
            false,
            false,
            false,
            ObserverActivationState.None.ToString(),
            "synthetic-local-o2-o3-sandbox",
            trace,
            Array.AsReadOnly(new[] { signal }),
            new O4ObserverForecastAvailability(
                "Unknown",
                "o4.forecast.complete_result_unavailable",
                ForecastLimitations),
            ProjectionLimitations);
        return new(payload, O1CanonicalCryptography.Digest(payload));
    }

    /// <summary>Projects authenticated PF-OBS-1 laboratory evidence without retaining its ephemeral connection material.</summary>
    /// <param name="evidencePath">Exact caller-owned temporary evidence file produced by the functional pilot.</param>
    /// <param name="projectedAtUtc">Trusted local projection instant.</param>
    /// <returns>A complete factual Observer package that remains synthetic, non-operational and non-authorising.</returns>
    internal static O4ObserverProjectionPackage CreateFromPfObs1(
        string evidencePath,
        DateTimeOffset projectedAtUtc)
    {
        PfObs1PilotEvidence evidence = JsonSerializer.Deserialize<PfObs1PilotEvidence>(
                File.ReadAllText(evidencePath),
                PfObs1JsonOptions)
            ?? throw new InvalidOperationException("pfobs1.projection.evidence_missing");
        string expectedDigest = PfObs1CorpusEvaluation.Digest(
            JsonSerializer.Serialize(evidence with { EvidenceDigest = string.Empty }));
        if (evidence.SchemaVersion != PfObs1PilotCampaign.SchemaVersion ||
            evidence.CellId != PfObs1PilotCampaign.CellId ||
            evidence.ActivationState != ObserverActivationState.None.ToString() ||
            !evidence.SyntheticLaboratory ||
            evidence.ProductionRepresentative ||
            !evidence.ReadOnly ||
            evidence.IsAuthorising ||
            evidence.Provider != "postgresql" ||
            evidence.ProviderSemanticVersion != "16" ||
            evidence.CorpusCount != PfObs1CorpusEvaluation.SampleCount ||
            evidence.Holdout.Accuracy < 1d ||
            evidence.EvidenceDigest != expectedDigest)
        {
            throw new InvalidOperationException("pfobs1.projection.evidence_refused");
        }

        double threshold = evidence.Holdout.ThresholdMilliseconds;
        double observed = evidence.LatestDurationMilliseconds;
        string disposition = observed >= threshold ? "Detected" : "NotDetected";
        byte[] evidenceIdBytes = Convert.FromHexString(evidence.EvidenceDigest[..32]);
        O4ObserverSignal signal = new(
            PfObs1CorpusEvaluation.Digest($"{evidence.EvidenceDigest}:signal"),
            "pfobs1.postgresql.duration-threshold",
            "1.0.0",
            "database.probe.duration.degraded",
            "milliseconds",
            disposition,
            "Warning",
            evidence.GeneratedAtUtc,
            evidence.GeneratedAtUtc,
            evidence.GeneratedAtUtc.AddMinutes(5),
            projectedAtUtc <= evidence.GeneratedAtUtc.AddMinutes(5)
                ? O4ObserverFreshness.Current
                : O4ObserverFreshness.Stale,
            observed,
            threshold,
            Array.AsReadOnly(new[] { new Guid(evidenceIdBytes) }),
            Array.Empty<string>());
        O4ObserverProjectionPayload payload = new(
            SchemaVersion,
            PfObs1CorpusEvaluation.Digest($"{evidence.EvidenceDigest}:{projectedAtUtc:O}"),
            projectedAtUtc,
            projectedAtUtc.AddMinutes(5),
            true,
            true,
            false,
            false,
            false,
            false,
            false,
            ObserverActivationState.None.ToString(),
            "postgresql-16-loopback-laboratory",
            new O4ObserverTrace(
                evidence.O2EnvelopeDigest,
                evidence.Mod12ReportDigest,
                evidence.Holdout.PolicyDigest,
                evidence.CorpusManifestDigest,
                1,
                evidence.EvidenceDigest),
            Array.AsReadOnly(new[] { signal }),
            new O4ObserverForecastAvailability(
                "Unknown",
                "o4.forecast.complete_result_unavailable",
                ForecastLimitations),
            ProjectionLimitations);
        return new(payload, O1CanonicalCryptography.Digest(payload));
    }

    /// <summary>Classifies a source result conservatively at the projection instant.</summary>
    /// <param name="validUntilUtc">Source validity boundary, when known.</param>
    /// <param name="projectedAtUtc">Trusted projection instant.</param>
    /// <returns>Current only inside the source validity window, otherwise Stale or Unknown.</returns>
    private static O4ObserverFreshness ClassifyFreshness(
        DateTimeOffset? validUntilUtc,
        DateTimeOffset projectedAtUtc) =>
        validUntilUtc is null
            ? O4ObserverFreshness.Unknown
            : projectedAtUtc <= validUntilUtc.Value
                ? O4ObserverFreshness.Current
                : O4ObserverFreshness.Stale;
}

/// <summary>Verifies completeness, integrity, provenance and non-authority before an API response is created.</summary>
internal static class O4ObserverProjectionVerifier
{
    /// <summary>Validates one candidate against the exact accepted O2/O3 provenance and current publication instant.</summary>
    /// <param name="candidate">Untrusted in-memory projection package.</param>
    /// <param name="expected">Original accepted projection package.</param>
    /// <param name="nowUtc">Trusted API publication instant.</param>
    /// <returns>A stable accepted or fail-closed verification code.</returns>
    internal static O4ObserverProjectionVerification Verify(
        O4ObserverProjectionPackage candidate,
        O4ObserverProjectionPackage expected,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(expected);
        O4ObserverProjectionPayload payload = candidate.Payload;
        bool valid =
            payload.SchemaVersion == O4ObserverProjectionFactory.SchemaVersion &&
            payload.ProcessingCompleted &&
            payload.Synthetic &&
            !payload.Operational &&
            !payload.ProductionRepresentative &&
            !payload.IsAuthorising &&
            !payload.Revoked &&
            !payload.Superseded &&
            payload.ActivationState == ObserverActivationState.None.ToString() &&
            payload.GeneratedAtUtc <= nowUtc.AddMinutes(1) &&
            payload.ValidUntilUtc > nowUtc &&
            payload.ValidUntilUtc <= payload.GeneratedAtUtc.AddMinutes(5) &&
            payload.Signals.Count is > 0 and <= 32 &&
            payload.Signals.All(IsCompleteSignal) &&
            payload.Forecasts.State == "Unknown" &&
            payload.Forecasts.Limitations.Count > 0 &&
            payload.Limitations.Count > 0 &&
            candidate.ProjectionDigest == O1CanonicalCryptography.Digest(payload) &&
            candidate.ProjectionDigest == expected.ProjectionDigest &&
            payload.Trace == expected.Payload.Trace &&
            payload.Trace.O2EnvelopeDigest == expected.Payload.Trace.O2EnvelopeDigest &&
            payload.Trace.O3ResultDigest == expected.Payload.Trace.O3ResultDigest &&
            payload.Trace.PolicyDigest == expected.Payload.Trace.PolicyDigest &&
            payload.Trace.CorpusManifestDigest == expected.Payload.Trace.CorpusManifestDigest &&
            payload.Trace.CorpusRevision == expected.Payload.Trace.CorpusRevision;
        return valid
            ? new(true, "o4.projection.accepted")
            : new(false, "o4.projection.failed_closed");
    }

    /// <summary>Checks one signal for bounded identifiers, evidence and explicit uncertainty.</summary>
    private static bool IsCompleteSignal(O4ObserverSignal signal) =>
        !string.IsNullOrWhiteSpace(signal.SignalId) &&
        !string.IsNullOrWhiteSpace(signal.RuleId) &&
        !string.IsNullOrWhiteSpace(signal.RuleVersion) &&
        !string.IsNullOrWhiteSpace(signal.MetricKey) &&
        !string.IsNullOrWhiteSpace(signal.Unit) &&
        !string.IsNullOrWhiteSpace(signal.Disposition) &&
        !string.IsNullOrWhiteSpace(signal.Severity) &&
        signal.EvidenceIds.Count is > 0 and <= 64 &&
        signal.EvidenceIds.All(id => id != Guid.Empty) &&
        signal.Limitations.Count <= 32;
}

/// <summary>Runs the HTTPS loopback API and dedicated built Dashboard only under the exact O4 marker.</summary>
public static class O4SandboxProcess
{
    private const string ProjectionRoute = "/api/v1/observer-sandbox/projection";
    private const string TestSubjectHeader = "X-DBN-O4-Test-Subject";
    private const string TestSubject = "o4-local-observer-review";
    private const string AuthenticationScheme = "O4ObserverSandbox";
    private const string ReadPolicy = "O4ObserverSandboxRead";

    /// <summary>Starts one bounded host after exact activation and waits for owning-process shutdown.</summary>
    /// <param name="args">Exact marker, Dashboard root and version-four run identifier.</param>
    /// <returns>Zero after clean shutdown, two for rejected activation or three for a bounded sandbox failure.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (!TryReadOptions(args, out string? dashboardRoot, out Guid runId, out string? pilotEvidence))
        {
            Console.Error.WriteLine("o4_sandbox.failed:activation_invalid");
            return 2;
        }

        string temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            $"DBNotifier-O4-{runId:N}");
        try
        {
            Directory.CreateDirectory(temporaryRoot);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            O4ObserverProjectionPackage projection = pilotEvidence is null
                ? await O4ObserverProjectionFactory.CreateAsync(temporaryRoot, now)
                : O4ObserverProjectionFactory.CreateFromPfObs1(pilotEvidence, now);
            using O4LoopbackCertificateLease certificate = O4LoopbackCertificateLease.Create();
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = [],
                EnvironmentName = "O4ObserverSandbox",
            });
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(kestrel =>
                kestrel.Listen(IPAddress.Loopback, 0, listener => listener.UseHttps(certificate.Certificate)));
            builder.Services.AddSingleton(projection);
            builder.Services.ConfigureHttpJsonOptions(options =>
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, O4SandboxAuthenticationHandler>(
                    AuthenticationScheme,
                    _ => { });
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy(ReadPolicy, policy =>
                {
                    policy.AddAuthenticationSchemes(AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("sub", TestSubject);
                });

            WebApplication application = builder.Build();
            PhysicalFileProvider dashboardFiles = new(dashboardRoot!);
            application.Lifetime.ApplicationStopped.Register(dashboardFiles.Dispose);
            application.UseAuthentication();
            application.UseAuthorization();
            application.MapGet(
                    ProjectionRoute,
                    (O4ObserverProjectionPackage current) =>
                        ReadProjection(current, current, DateTimeOffset.UtcNow))
                .RequireAuthorization(ReadPolicy);
            application.UseDefaultFiles(new DefaultFilesOptions { FileProvider = dashboardFiles });
            application.UseStaticFiles(new StaticFileOptions { FileProvider = dashboardFiles });
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
                    Path.Combine(dashboardRoot!, "index.html"),
                    context.RequestAborted);
            });

            await application.StartAsync();
            string address = application.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()!
                .Addresses
                .Single(value => value.StartsWith("https://127.0.0.1:", StringComparison.Ordinal));
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                marker = "DBNOTIFIER_O4_OBSERVER_SANDBOX_READY",
                baseAddress = address,
                spkiPin = certificate.SubjectPublicKeyInfoPin,
            }));
            await application.WaitForShutdownAsync();
            await application.DisposeAsync();
            return 0;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"o4_sandbox.failed:{exception.GetType().Name}");
            return 3;
        }
        finally
        {
            DeleteTemporaryRoot(temporaryRoot);
        }
    }

    /// <summary>Creates a read-only response only after the package passes exact in-memory verification.</summary>
    internal static IResult ReadProjection(
        O4ObserverProjectionPackage candidate,
        O4ObserverProjectionPackage expected,
        DateTimeOffset nowUtc)
    {
        O4ObserverProjectionVerification verification =
            O4ObserverProjectionVerifier.Verify(candidate, expected, nowUtc);
        return verification.Accepted
            ? Results.Json(candidate, statusCode: StatusCodes.Status200OK)
            : Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Observer sandbox projection unavailable",
                extensions: new Dictionary<string, object?> { ["code"] = verification.Code });
    }

    /// <summary>Accepts only an exact O4/PF-OBS-1 marker, canonical Dashboard output and bounded temporary evidence.</summary>
    private static bool TryReadOptions(
        string[] args,
        out string? dashboardRoot,
        out Guid runId,
        out string? pilotEvidence)
    {
        dashboardRoot = null;
        runId = Guid.Empty;
        pilotEvidence = null;
        bool pilot = args.Length == 8 &&
            args[0] == "--activation" &&
            args[1] == O4ObserverProjectionFactory.PfObs1ActivationMarker;
        bool o4 = args.Length == 6 &&
            args[0] == "--activation" &&
            args[1] == O4ObserverProjectionFactory.ActivationMarker;
        if ((!pilot && !o4) ||
            args[2] != "--dashboard-root" ||
            args[4] != "--run-id" ||
            !Guid.TryParseExact(args[5], "D", out runId) ||
            args[5][14] != '4' ||
            !"89abAB".Contains(args[5][19]))
        {
            return false;
        }

        try
        {
            DirectoryInfo directory = new(Path.GetFullPath(args[3]));
            if (!directory.Exists ||
                directory.Name != "dist" ||
                directory.Parent?.Name != "DBNotifier.Dashboard.Web" ||
                directory.Attributes.HasFlag(FileAttributes.ReparsePoint) ||
                !File.Exists(Path.Combine(directory.FullName, "index.html")) ||
                !Directory.Exists(Path.Combine(directory.FullName, "assets")))
            {
                return false;
            }
            dashboardRoot = directory.FullName;
            if (pilot)
            {
                if (args[6] != "--pilot-evidence")
                {
                    return false;
                }
                string evidence = Path.GetFullPath(args[7]);
                string temporary = Path.GetFullPath(Path.GetTempPath());
                string? parent = Path.GetDirectoryName(evidence);
                if (parent is null ||
                    !parent.StartsWith(temporary, StringComparison.OrdinalIgnoreCase) ||
                    !Path.GetFileName(parent).StartsWith("DBNotifier-PF-OBS-1-", StringComparison.Ordinal) ||
                    Path.GetFileName(evidence) != "pf-obs-1-evidence.json" ||
                    !File.Exists(evidence))
                {
                    return false;
                }
                pilotEvidence = evidence;
            }
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            NotSupportedException or
            PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>Deletes only the GUID-bound O4 directory beneath the operating-system temporary root.</summary>
    private static void DeleteTemporaryRoot(string root)
    {
        string candidate = Path.GetFullPath(root);
        string temporary = Path.GetFullPath(Path.GetTempPath());
        if (!candidate.StartsWith(temporary, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(candidate).StartsWith("DBNotifier-O4-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o4.cleanup.scope_invalid");
        }
        if (Directory.Exists(candidate))
        {
            Directory.Delete(candidate, recursive: true);
        }
    }

    /// <summary>Authenticates only the fixed O4 subject on HTTPS loopback under the exact sandbox environment.</summary>
    private sealed class O4SandboxAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHostEnvironment environment)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        /// <inheritdoc />
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            bool loopback = Context.Connection.RemoteIpAddress is not null &&
                IPAddress.IsLoopback(Context.Connection.RemoteIpAddress);
            string subject = Request.Headers[TestSubjectHeader].SingleOrDefault() ?? "";
            if (!environment.IsEnvironment("O4ObserverSandbox") ||
                !Request.IsHttps ||
                !loopback ||
                !string.Equals(subject, TestSubject, StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
            ClaimsIdentity identity = new([new Claim("sub", subject)], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}

/// <summary>Owns one ephemeral loopback certificate and the non-secret Chromium SPKI pin for the O4 process.</summary>
internal sealed class O4LoopbackCertificateLease : IDisposable
{
    private readonly ECDsa key;

    private O4LoopbackCertificateLease(
        X509Certificate2 certificate,
        ECDsa key,
        string subjectPublicKeyInfoPin)
    {
        Certificate = certificate;
        this.key = key;
        SubjectPublicKeyInfoPin = subjectPublicKeyInfoPin;
    }

    internal X509Certificate2 Certificate { get; }
    internal string SubjectPublicKeyInfoPin { get; }

    /// <summary>Creates one short-lived certificate without persisting private material.</summary>
    internal static O4LoopbackCertificateLease Create()
    {
        ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest request = new("CN=localhost", key, HashAlgorithmName.SHA256);
        SubjectAlternativeNameBuilder names = new();
        names.AddDnsName("localhost");
        names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], true));
        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            using X509Certificate2 ephemeral =
                request.CreateSelfSigned(now.AddMinutes(-1), now.AddMinutes(15));
            string password = Guid.NewGuid().ToString("N");
            byte[] pkcs12 = ephemeral.Export(X509ContentType.Pkcs12, password);
            try
            {
                X509Certificate2 transport = X509CertificateLoader.LoadPkcs12(
                    pkcs12,
                    password,
                    X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
                using ECDsa publicKey = transport.GetECDsaPublicKey() ??
                    throw new CryptographicException(
                        "The loopback certificate did not expose an ECDSA public key.");
                byte[] subjectPublicKeyInfo = publicKey.ExportSubjectPublicKeyInfo();
                try
                {
                    string pin = Convert.ToBase64String(SHA256.HashData(subjectPublicKeyInfo));
                    return new(transport, key, pin);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(subjectPublicKeyInfo);
                }
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

    /// <summary>Disposes the ephemeral certificate and private key.</summary>
    public void Dispose()
    {
        Certificate.Dispose();
        key.Dispose();
    }
}
