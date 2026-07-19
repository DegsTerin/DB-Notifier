// Module purpose: Hosts the built Dashboard and deterministic API fixture on one temporary HTTPS loopback origin for browser-only E2E evidence.
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using DBNotifier.Server.Api;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.FileProviders;

namespace DBNotifier.DashboardTv.BrowserSandboxHost;

/// <summary>Starts an exact opt-in sandbox host and exposes no operational DB-Notifier capability.</summary>
internal static class Program
{
    private const string EvidenceRoute = "/__dbnotifier-browser-e2e/evidence";
    private const string ScenarioHeader = "X-DBN-TV-Browser-Scenario";
    private const string EvidenceSubject = "dashboard-tv-local-test";

    /// <summary>Validates local-only arguments, starts the bounded HTTPS host and waits for runner cancellation.</summary>
    /// <param name="args">Exact activation and built Dashboard root arguments supplied by the local runner.</param>
    /// <returns>Zero after a clean host shutdown, or a non-zero code when activation fails closed.</returns>
    public static async Task<int> Main(string[] args)
    {
        if (!BrowserSandboxOptions.TryParse(args, out BrowserSandboxOptions? options, out string error))
        {
            Console.Error.WriteLine(error);
            return 2;
        }
        BrowserSandboxOptions validatedOptions = options ??
            throw new InvalidOperationException("Successful sandbox option parsing returned no options.");

        using LoopbackCertificateLease certificate = LoopbackCertificateLease.Create();
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [],
            ApplicationName = typeof(DashboardTvSandboxEndpointRouteBuilderExtensions).Assembly.FullName,
            EnvironmentName = DashboardTvSandboxEndpointRouteBuilderExtensions.EnvironmentName,
        });
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{DashboardTvSandboxEndpointRouteBuilderExtensions.ConfigurationSection}:Enabled"] = bool.TrueString,
        });
        builder.WebHost.ConfigureKestrel(kestrel =>
            kestrel.Listen(IPAddress.Loopback, 0, listener => listener.UseHttps(certificate.Certificate)));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<BrowserScenarioEvidenceStore>();
        bool endpointEnabled = builder.Services.AddDashboardTvSandbox(builder.Environment, builder.Configuration);
        builder.Services.AddRateLimiter(rateLimiting =>
            rateLimiting.AddFixedWindowLimiter("HumanApiRateLimit", limiter =>
            {
                limiter.PermitLimit = 120;
                limiter.Window = TimeSpan.FromMinutes(5);
                limiter.QueueLimit = 0;
            }));

        WebApplication application = builder.Build();
        PhysicalFileProvider dashboardFiles = new(validatedOptions.DashboardRoot);
        application.Lifetime.ApplicationStopped.Register(dashboardFiles.Dispose);
        application.UseRouting();
        application.UseAuthentication();
        application.UseRateLimiter();
        application.UseAuthorization();
        application.Use((context, next) => HandleScenarioAsync(context, next, application.Services.GetRequiredService<BrowserScenarioEvidenceStore>()));
        application.MapDashboardTvSandboxEndpoint(endpointEnabled);
        application.MapGet(EvidenceRoute, (HttpContext context, BrowserScenarioEvidenceStore evidence) => ReadEvidence(context, evidence));
        application.UseDefaultFiles(new DefaultFilesOptions { FileProvider = dashboardFiles });
        application.UseStaticFiles(new StaticFileOptions { FileProvider = dashboardFiles });
        application.MapFallback(async context =>
        {
            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(validatedOptions.IndexPath, context.RequestAborted);
        });

        try
        {
            await application.StartAsync();
            string address = application.Urls.Single(url => url.StartsWith("https://127.0.0.1:", StringComparison.Ordinal));
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                marker = "DBNOTIFIER_DASHBOARD_TV_BROWSER_SANDBOX_READY",
                baseAddress = address,
                spkiPin = certificate.SubjectPublicKeyInfoPin,
            }));
            await application.WaitForShutdownAsync();
            return 0;
        }
        finally
        {
            await application.DisposeAsync();
        }
    }

    /// <summary>Records every snapshot request and injects only the bounded failure selected by the isolated browser runner.</summary>
    /// <param name="context">Current loopback request.</param>
    /// <param name="next">Remaining immutable snapshot endpoint pipeline.</param>
    /// <param name="evidence">In-memory evidence recorder for this process only.</param>
    /// <returns>A task that completes after the selected response and evidence record finish.</returns>
    private static async Task HandleScenarioAsync(
        HttpContext context,
        Func<Task> next,
        BrowserScenarioEvidenceStore evidence)
    {
        if (!context.Request.Path.Equals(DashboardTvSandboxEndpointRouteBuilderExtensions.SnapshotRoute))
        {
            await next();
            return;
        }

        string scenario = context.Request.Headers[ScenarioHeader].SingleOrDefault() ?? "";
        if (!BrowserScenarioEvidenceStore.IsKnownScenario(scenario))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        BrowserScenarioRequest request = evidence.Begin(
            scenario,
            context.Request.Headers.ContainsKey("If-None-Match"));
        bool cancelled = false;
        try
        {
            switch (scenario, request.Sequence)
            {
                case ("preserve-denied", >= 2):
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    break;
                case ("preserve-incompatible", >= 2):
                    context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
                    break;
                case ("preserve-malformed", >= 2):
                    SetSyntheticSuccessHeaders(context.Response);
                    await context.Response.WriteAsync("{", context.RequestAborted);
                    break;
                case ("preserve-oversized", >= 2):
                    SetSyntheticSuccessHeaders(context.Response);
                    string oversized = new('x', (512 * 1024) + 1);
                    context.Response.ContentLength = oversized.Length;
                    await context.Response.WriteAsync(oversized, context.RequestAborted);
                    break;
                case ("preserve-error", >= 2):
                case ("error-recovery", 2):
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    break;
                case ("timeout-recovery", 2):
                    await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted);
                    break;
                case ("late-fence", 1):
                    await Task.Delay(TimeSpan.FromMilliseconds(1_200));
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("late response deliberately ignored by the browser session");
                    break;
                case ("slow-serial", _):
                    await Task.Delay(TimeSpan.FromMilliseconds(350), context.RequestAborted);
                    await next();
                    break;
                default:
                    await next();
                    break;
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            cancelled = true;
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499;
            }
        }
        finally
        {
            evidence.Complete(request, context.Response.StatusCode, cancelled || context.RequestAborted.IsCancellationRequested);
        }
    }

    /// <summary>Returns sanitised request timing and status evidence only to the exact HTTPS loopback test subject.</summary>
    /// <param name="context">Current evidence request.</param>
    /// <param name="evidence">In-memory scenario evidence.</param>
    /// <returns>A bounded result containing no request body, token, certificate or private material.</returns>
    private static IResult ReadEvidence(HttpContext context, BrowserScenarioEvidenceStore evidence)
    {
        bool loopback = context.Connection.RemoteIpAddress is not null && IPAddress.IsLoopback(context.Connection.RemoteIpAddress);
        string subject = context.Request.Headers[DashboardTvSandboxEndpointRouteBuilderExtensions.TestSubjectHeader].SingleOrDefault() ?? "";
        string scenario = context.Request.Query["scenario"].SingleOrDefault() ?? "";
        if (!context.Request.IsHttps || !loopback || !string.Equals(subject, EvidenceSubject, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        if (!BrowserScenarioEvidenceStore.IsKnownScenario(scenario))
        {
            return Results.BadRequest();
        }

        return Results.Json(evidence.Snapshot(scenario));
    }

    /// <summary>Adds valid protocol headers to one deliberately malformed or oversized local response.</summary>
    /// <param name="response">Response that will remain semantically invalid at a later validation boundary.</param>
    private static void SetSyntheticSuccessHeaders(HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "application/json; charset=utf-8";
        response.Headers.ETag = $"\"sha256-{new string('b', 64)}\"";
        response.Headers["DBN-Snapshot-Schema"] = "dashboard-tv.v1";
        response.Headers.CacheControl = "private, no-cache";
    }
}

/// <summary>Owns exact startup options and rejects arbitrary roots or activation values before any listener exists.</summary>
internal sealed class BrowserSandboxOptions
{
    private const string ActivationValue = "local-test";

    private BrowserSandboxOptions(string dashboardRoot)
    {
        DashboardRoot = dashboardRoot;
        IndexPath = Path.Combine(dashboardRoot, "index.html");
    }

    /// <summary>Gets the validated Vite distribution directory served by the temporary host.</summary>
    public string DashboardRoot { get; }

    /// <summary>Gets the validated single-page application entry point.</summary>
    public string IndexPath { get; }

    /// <summary>Parses the exact activation and Dashboard root arguments without accepting additional switches.</summary>
    /// <param name="args">Raw process arguments.</param>
    /// <param name="options">Validated options when parsing succeeds.</param>
    /// <param name="error">Sanitised failure reason when parsing fails.</param>
    /// <returns><see langword="true"/> only for an exact local-test activation and safe built Dashboard root.</returns>
    public static bool TryParse(string[] args, out BrowserSandboxOptions? options, out string error)
    {
        options = null;
        error = "Dashboard TV browser sandbox activation was rejected.";
        if (args.Length != 4 || args[0] != "--activation" || args[1] != "local-test" || args[2] != "--dashboard-root")
        {
            return false;
        }

        string root;
        try
        {
            root = Path.GetFullPath(args[3]);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        DirectoryInfo directory = new(root);
        DirectoryInfo? parent = directory.Parent;
        if (!directory.Exists ||
            !string.Equals(directory.Name, "dist", StringComparison.Ordinal) ||
            !string.Equals(parent?.Name, "DBNotifier.Dashboard.Web", StringComparison.Ordinal) ||
            directory.Attributes.HasFlag(FileAttributes.ReparsePoint) ||
            parent!.Attributes.HasFlag(FileAttributes.ReparsePoint) ||
            !File.Exists(Path.Combine(root, "index.html")) ||
            !Directory.Exists(Path.Combine(root, "assets")))
        {
            return false;
        }

        options = new BrowserSandboxOptions(root);
        error = "";
        return true;
    }
}

/// <summary>Owns one ephemeral P-256 loopback certificate and its non-secret Chromium SPKI pin.</summary>
internal sealed class LoopbackCertificateLease : IDisposable
{
    private readonly ECDsa key;

    private LoopbackCertificateLease(X509Certificate2 certificate, ECDsa key, string subjectPublicKeyInfoPin)
    {
        Certificate = certificate;
        this.key = key;
        SubjectPublicKeyInfoPin = subjectPublicKeyInfoPin;
    }

    /// <summary>Gets the in-process certificate used by the temporary Kestrel listener.</summary>
    public X509Certificate2 Certificate { get; }

    /// <summary>Gets the SHA-256 SPKI pin used to trust only this generated certificate in the dedicated browser.</summary>
    public string SubjectPublicKeyInfoPin { get; }

    /// <summary>Creates a short-lived P-256 certificate for localhost and 127.0.0.1 without persisting private material.</summary>
    /// <returns>A disposable certificate and key lease.</returns>
    public static LoopbackCertificateLease Create()
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

        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            using X509Certificate2 ephemeral = request.CreateSelfSigned(now.AddMinutes(-1), now.AddMinutes(15));
            string password = Guid.NewGuid().ToString("N");
            byte[] pkcs12 = ephemeral.Export(X509ContentType.Pkcs12, password);
            try
            {
                X509Certificate2 transport = X509CertificateLoader.LoadPkcs12(
                    pkcs12,
                    password,
                    X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
                using ECDsa publicKey = transport.GetECDsaPublicKey() ??
                    throw new CryptographicException("The loopback certificate did not expose an ECDSA public key.");
                byte[] subjectPublicKeyInfo = publicKey.ExportSubjectPublicKeyInfo();
                try
                {
                    string pin = Convert.ToBase64String(SHA256.HashData(subjectPublicKeyInfo));
                    return new LoopbackCertificateLease(transport, key, pin);
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

    /// <summary>Disposes the temporary certificate and its private key.</summary>
    public void Dispose()
    {
        Certificate.Dispose();
        key.Dispose();
    }
}

/// <summary>Records bounded, sanitised timing evidence for known browser scenarios in memory only.</summary>
internal sealed class BrowserScenarioEvidenceStore
{
    private static readonly HashSet<string> KnownScenarios = new(StringComparer.Ordinal)
    {
        "authoritative",
        "preserve-denied",
        "preserve-incompatible",
        "preserve-malformed",
        "preserve-oversized",
        "preserve-error",
        "error-recovery",
        "timeout-recovery",
        "offline-recovery",
        "late-fence",
        "authoritative-after-fence",
        "slow-serial",
    };

    private readonly ConcurrentDictionary<string, BrowserScenarioState> states = new(StringComparer.Ordinal);

    /// <summary>Returns whether a scenario belongs to the fixed local evidence matrix.</summary>
    /// <param name="scenario">Untrusted scenario header or query value.</param>
    /// <returns><see langword="true"/> only for a fixed scenario identifier.</returns>
    public static bool IsKnownScenario(string scenario) => KnownScenarios.Contains(scenario);

    /// <summary>Begins one request record and updates the per-scenario concurrency watermark.</summary>
    /// <param name="scenario">Validated scenario identifier.</param>
    /// <param name="hadConditionalTag">Whether the browser sent an ETag condition.</param>
    /// <returns>The mutable request record owned by this request only.</returns>
    public BrowserScenarioRequest Begin(string scenario, bool hadConditionalTag) =>
        states.GetOrAdd(scenario, _ => new BrowserScenarioState()).Begin(scenario, hadConditionalTag);

    /// <summary>Completes one request record with status and cancellation truth.</summary>
    /// <param name="request">Record returned by <see cref="Begin"/>.</param>
    /// <param name="statusCode">Final local HTTP status when available.</param>
    /// <param name="cancelled">Whether the request cancellation token was observed.</param>
    public void Complete(BrowserScenarioRequest request, int statusCode, bool cancelled) =>
        states[request.Scenario].Complete(request, statusCode, cancelled);

    /// <summary>Creates an immutable sanitised snapshot for the evidence endpoint.</summary>
    /// <param name="scenario">Validated scenario identifier.</param>
    /// <returns>Current count, concurrency watermark and completed request records.</returns>
    public BrowserScenarioEvidence Snapshot(string scenario) =>
        states.GetOrAdd(scenario, _ => new BrowserScenarioState()).Snapshot(scenario);
}

/// <summary>Serialises mutation of one scenario's request list while retaining real concurrent request evidence.</summary>
internal sealed class BrowserScenarioState
{
    private readonly object gate = new();
    private readonly List<BrowserScenarioRequest> requests = [];
    private int active;
    private int maximumConcurrency;

    /// <summary>Creates and registers one request record.</summary>
    /// <param name="scenario">Fixed scenario identifier.</param>
    /// <param name="hadConditionalTag">Whether an ETag condition was present.</param>
    /// <returns>The new in-flight record.</returns>
    public BrowserScenarioRequest Begin(string scenario, bool hadConditionalTag)
    {
        lock (gate)
        {
            active += 1;
            maximumConcurrency = Math.Max(maximumConcurrency, active);
            BrowserScenarioRequest request = new(scenario, requests.Count + 1, hadConditionalTag, DateTimeOffset.UtcNow);
            requests.Add(request);
            return request;
        }
    }

    /// <summary>Completes a previously registered record exactly once.</summary>
    /// <param name="request">Owned request record.</param>
    /// <param name="statusCode">Final local response status.</param>
    /// <param name="cancelled">Whether cancellation reached the host.</param>
    public void Complete(BrowserScenarioRequest request, int statusCode, bool cancelled)
    {
        lock (gate)
        {
            request.Complete(statusCode, cancelled, DateTimeOffset.UtcNow);
            active = Math.Max(0, active - 1);
        }
    }

    /// <summary>Copies completed evidence without exposing mutable records to the response serialiser.</summary>
    /// <param name="scenario">Fixed scenario identifier.</param>
    /// <returns>A bounded evidence value for one scenario.</returns>
    public BrowserScenarioEvidence Snapshot(string scenario)
    {
        lock (gate)
        {
            return new BrowserScenarioEvidence(
                scenario,
                requests.Count,
                active,
                maximumConcurrency,
                requests.Select(request => request.Copy()).ToArray());
        }
    }
}

/// <summary>Represents one sanitised local snapshot request without headers, bodies or identity material.</summary>
internal sealed class BrowserScenarioRequest
{
    /// <summary>Creates one in-flight evidence record.</summary>
    /// <param name="scenario">Fixed scenario identifier.</param>
    /// <param name="sequence">One-based request sequence for this scenario.</param>
    /// <param name="hadConditionalTag">Whether an ETag condition was present.</param>
    /// <param name="startedAt">Observed UTC start time.</param>
    public BrowserScenarioRequest(string scenario, int sequence, bool hadConditionalTag, DateTimeOffset startedAt)
    {
        Scenario = scenario;
        Sequence = sequence;
        HadConditionalTag = hadConditionalTag;
        StartedAt = startedAt;
    }

    /// <summary>Gets the fixed scenario identifier.</summary>
    public string Scenario { get; }

    /// <summary>Gets the one-based request order.</summary>
    public int Sequence { get; }

    /// <summary>Gets whether the request carried an ETag condition.</summary>
    public bool HadConditionalTag { get; }

    /// <summary>Gets the UTC request start time.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>Gets the UTC completion time after the record completes.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Gets the final response status when the host observed one.</summary>
    public int? StatusCode { get; private set; }

    /// <summary>Gets whether cancellation reached the host.</summary>
    public bool Cancelled { get; private set; }

    /// <summary>Completes the mutable in-process record.</summary>
    /// <param name="statusCode">Final response status.</param>
    /// <param name="cancelled">Cancellation truth.</param>
    /// <param name="completedAt">Observed UTC completion time.</param>
    public void Complete(int statusCode, bool cancelled, DateTimeOffset completedAt)
    {
        StatusCode = statusCode;
        Cancelled = cancelled;
        CompletedAt = completedAt;
    }

    /// <summary>Copies this record for JSON serialisation without sharing mutable state.</summary>
    /// <returns>An immutable record copy.</returns>
    public BrowserScenarioRequestEvidence Copy() =>
        new(Sequence, HadConditionalTag, StartedAt, CompletedAt, StatusCode, Cancelled);
}

/// <summary>Contains immutable, sanitised evidence for one scenario.</summary>
internal sealed record BrowserScenarioEvidence(
    string Scenario,
    int RequestCount,
    int ActiveRequests,
    int MaximumConcurrency,
    IReadOnlyList<BrowserScenarioRequestEvidence> Requests);

/// <summary>Contains immutable timing and status evidence for one local request.</summary>
internal sealed record BrowserScenarioRequestEvidence(
    int Sequence,
    bool HadConditionalTag,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int? StatusCode,
    bool Cancelled);
