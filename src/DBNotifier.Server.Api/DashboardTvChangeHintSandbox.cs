// Module purpose: Composes the authenticated local-only SignalR hint boundary without changing normal API registration or authoritative snapshot semantics.
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using DBNotifier.Application.Presentation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace DBNotifier.Server.Api;

/// <summary>Registers and maps the authenticated best-effort change-hint channel only for an exact local sandbox host.</summary>
public static class DashboardTvChangeHintSandboxEndpointRouteBuilderExtensions
{
    /// <summary>Gets the configuration section that independently guards SignalR sandbox activation.</summary>
    public const string ConfigurationSection = "DashboardTvSignalRSandbox";

    /// <summary>Gets the versioned SignalR hub route.</summary>
    public const string HubRoute = "/api/v1/dashboard/tv-change-hints";

    /// <summary>Gets the same-origin route that issues and revokes one ephemeral test session.</summary>
    public const string SessionRoute = "/api/v1/dashboard/tv-change-hints/session";

    /// <summary>Gets the fixed client method name used for one minimal hint.</summary>
    public const string ClientMethod = "DashboardTvChanged";

    /// <summary>Gets the dedicated cookie authentication scheme used only by the hint hub.</summary>
    public const string TestAuthenticationScheme = "DashboardTvSignalRSandboxHuman";

    /// <summary>Gets the dedicated read-only policy used by the hint hub and session revocation route.</summary>
    public const string ReadPolicy = "DashboardTvSignalRSandboxRead";

    /// <summary>Gets the host-only cookie name that is never readable from Dashboard JavaScript.</summary>
    public const string SessionCookieName = "__Host-DBN-TV-Sandbox";

    /// <summary>Registers SignalR, bounded session state and authentication only when both sandbox guards are exact.</summary>
    /// <param name="services">Application service collection.</param>
    /// <param name="environment">Current host environment.</param>
    /// <param name="configuration">Current local configuration.</param>
    /// <returns><see langword="true"/> only when the caller may map the test-only channel.</returns>
    public static bool AddDashboardTvChangeHintSandbox(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);

        bool snapshotEnabled = configuration.GetValue<bool>(
            $"{DashboardTvSandboxEndpointRouteBuilderExtensions.ConfigurationSection}:Enabled");
        bool hintsEnabled = configuration.GetValue<bool>($"{ConfigurationSection}:Enabled");
        if (!environment.IsEnvironment(DashboardTvSandboxEndpointRouteBuilderExtensions.EnvironmentName) ||
            !snapshotEnabled ||
            !hintsEnabled)
        {
            return false;
        }

        services.AddSingleton<DashboardTvSignalRSandboxSessionStore>();
        services.AddSingleton<DashboardTvChangeHintSandboxEvidence>();
        services.AddSingleton<IDashboardTvChangeHintPublisher, DashboardTvSignalRChangeHintPublisher>();
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = false;
            options.MaximumParallelInvocationsPerClient = 1;
            options.MaximumReceiveMessageSize = 1_024;
            options.HandshakeTimeout = TimeSpan.FromSeconds(5);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.KeepAliveInterval = TimeSpan.FromSeconds(10);
        });
        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, DashboardTvSignalRSandboxAuthenticationHandler>(
                TestAuthenticationScheme,
                _ => { });
        services.AddAuthorizationBuilder()
            .AddPolicy(ReadPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(TestAuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("sub");
            });
        return true;
    }

    /// <summary>Maps session bootstrap/revocation and the hub only after exact service registration succeeded.</summary>
    /// <param name="endpoints">Endpoint route builder.</param>
    /// <param name="enabled">Exact activation result returned during service registration.</param>
    /// <returns>The unchanged route builder.</returns>
    public static IEndpointRouteBuilder MapDashboardTvChangeHintSandbox(
        this IEndpointRouteBuilder endpoints,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        if (!enabled)
        {
            return endpoints;
        }

        endpoints.MapPost(SessionRoute, IssueSession)
            .RequireAuthorization(DashboardTvSandboxEndpointRouteBuilderExtensions.ReadPolicy)
            .RequireRateLimiting("HumanApiRateLimit");
        endpoints.MapDelete(SessionRoute, RevokeSession)
            .RequireAuthorization(ReadPolicy)
            .RequireRateLimiting("HumanApiRateLimit");
        endpoints.MapHub<DashboardTvChangeHintHub>(HubRoute)
            .RequireAuthorization(ReadPolicy)
            .RequireRateLimiting("HumanApiRateLimit");
        return endpoints;
    }

    /// <summary>Exchanges the existing bounded test identity for one host-only ephemeral hub cookie.</summary>
    /// <param name="context">Exact same-origin HTTPS loopback request.</param>
    /// <param name="sessions">Bounded in-memory proof store.</param>
    /// <returns>No content after issuance, or an unauthorised result on any boundary failure.</returns>
    private static IResult IssueSession(
        HttpContext context,
        DashboardTvSignalRSandboxSessionStore sessions)
    {
        if (!DashboardTvSignalRSandboxOrigin.IsExact(context) ||
            context.User.FindFirstValue("sub") is not { Length: > 0 and <= 200 } subject ||
            !sessions.TryIssue(subject, out string token, out DateTimeOffset expiresAt))
        {
            return TypedResults.Unauthorized();
        }

        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers["DBN-Change-Hint-Schema"] = DashboardTvChangeHintContract.CurrentSchemaVersion;
        context.Response.Cookies.Append(SessionCookieName, token, CreateCookieOptions(expiresAt));
        return TypedResults.NoContent();
    }

    /// <summary>Revokes the presented ephemeral proof and clears its host-only cookie without existence disclosure.</summary>
    /// <param name="context">Exact same-origin HTTPS loopback request.</param>
    /// <param name="sessions">Bounded in-memory proof store.</param>
    /// <returns>No content after fail-closed revocation, or an unauthorised result for malformed boundaries.</returns>
    private static IResult RevokeSession(
        HttpContext context,
        DashboardTvSignalRSandboxSessionStore sessions)
    {
        if (!DashboardTvSignalRSandboxOrigin.IsExact(context) ||
            !DashboardTvSignalRSandboxCookie.TryReadExact(context.Request, out string token))
        {
            return TypedResults.Unauthorized();
        }

        sessions.Revoke(token);
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Cookies.Delete(SessionCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });
        return TypedResults.NoContent();
    }

    /// <summary>Creates fixed host-only cookie metadata that cannot be read by Dashboard JavaScript.</summary>
    /// <param name="expiresAt">Server-derived UTC expiry.</param>
    /// <returns>Strict secure cookie options for the local sandbox session.</returns>
    private static CookieOptions CreateCookieOptions(DateTimeOffset expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        Expires = expiresAt,
        MaxAge = DashboardTvSignalRSandboxSessionStore.SessionLifetime,
        IsEssential = false,
    };
}

/// <summary>Accepts no client methods and records only bounded connection counts for the local test harness.</summary>
public sealed class DashboardTvChangeHintHub : Hub
{
    private readonly DashboardTvChangeHintSandboxEvidence evidence;

    /// <summary>Initialises a transient hub instance with sanitised sandbox evidence.</summary>
    /// <param name="evidence">Process-local connection counter.</param>
    public DashboardTvChangeHintHub(DashboardTvChangeHintSandboxEvidence evidence)
    {
        this.evidence = evidence;
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync().ConfigureAwait(false);
        evidence.Connected();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        evidence.Disconnected();
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}

/// <summary>Publishes one validated best-effort hint without persisting or interpreting projection data.</summary>
public interface IDashboardTvChangeHintPublisher
{
    /// <summary>Publishes one opaque projection revision to currently authenticated sandbox clients.</summary>
    /// <param name="projectionRevision">Bounded lower-case SHA-256 revision.</param>
    /// <param name="cancellationToken">Cancellation for the local publication attempt.</param>
    /// <returns>A task that completes after the in-process hub accepted the send request.</returns>
    ValueTask PublishAsync(string projectionRevision, CancellationToken cancellationToken);
}

/// <summary>Uses the typed SignalR hub context only inside an explicitly registered sandbox process.</summary>
public sealed class DashboardTvSignalRChangeHintPublisher : IDashboardTvChangeHintPublisher
{
    private readonly IHubContext<DashboardTvChangeHintHub> hub;
    private readonly DashboardTvChangeHintSandboxEvidence evidence;

    /// <summary>Initialises the publisher from sandbox-only services.</summary>
    /// <param name="hub">SignalR context for authenticated connected clients.</param>
    /// <param name="evidence">Sanitised process-local counters.</param>
    public DashboardTvSignalRChangeHintPublisher(
        IHubContext<DashboardTvChangeHintHub> hub,
        DashboardTvChangeHintSandboxEvidence evidence)
    {
        this.hub = hub;
        this.evidence = evidence;
    }

    /// <inheritdoc />
    public async ValueTask PublishAsync(string projectionRevision, CancellationToken cancellationToken)
    {
        DashboardTvChangeHint hint = new(
            DashboardTvChangeHintContract.CurrentSchemaVersion,
            projectionRevision);
        if (!DashboardTvChangeHintContract.IsValid(hint))
        {
            throw new ArgumentException("The projection revision is incompatible with the bounded hint contract.", nameof(projectionRevision));
        }

        await hub.Clients.All
            .SendAsync(
                DashboardTvChangeHintSandboxEndpointRouteBuilderExtensions.ClientMethod,
                hint,
                cancellationToken)
            .ConfigureAwait(false);
        evidence.Published();
    }
}

/// <summary>Owns short-lived test-session proof while retaining only hashes of issued random tokens.</summary>
public sealed class DashboardTvSignalRSandboxSessionStore
{
    /// <summary>Gets the fixed lifetime of one browser-only sandbox session.</summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(5);

    private const int MaximumSessions = 4;
    private const int TokenBytes = 32;
    private const int EncodedTokenLength = 43;
    private readonly object gate = new();
    private readonly List<SessionProof> sessions = [];
    private readonly TimeProvider timeProvider;

    /// <summary>Initialises the bounded in-memory store with the host clock.</summary>
    /// <param name="timeProvider">Clock used for deterministic expiry checks.</param>
    public DashboardTvSignalRSandboxSessionStore(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    /// <summary>Issues one random raw token while retaining only its digest and bounded subject.</summary>
    /// <param name="subject">Authenticated read-only test subject.</param>
    /// <param name="token">Raw token returned only to the Set-Cookie boundary.</param>
    /// <param name="expiresAt">UTC expiry used by both server validation and cookie metadata.</param>
    /// <returns><see langword="true"/> when the bounded store admitted the session.</returns>
    public bool TryIssue(string subject, out string token, out DateTimeOffset expiresAt)
    {
        token = string.Empty;
        expiresAt = default;
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 200)
        {
            return false;
        }

        byte[] raw = RandomNumberGenerator.GetBytes(TokenBytes);
        byte[] digest = SHA256.HashData(raw);
        try
        {
            lock (gate)
            {
                DateTimeOffset now = timeProvider.GetUtcNow();
                RemoveExpiredAndSubject(now, subject);
                if (sessions.Count >= MaximumSessions)
                {
                    return false;
                }

                expiresAt = now.Add(SessionLifetime);
                sessions.Add(new SessionProof(digest.ToArray(), subject, expiresAt));
                token = WebEncoders.Base64UrlEncode(raw);
                return true;
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(raw);
            CryptographicOperations.ZeroMemory(digest);
        }
    }

    /// <summary>Validates one exact token using fixed-time digest comparisons and returns its bounded subject.</summary>
    /// <param name="token">Untrusted cookie value.</param>
    /// <param name="subject">Bound subject when the proof is current.</param>
    /// <returns><see langword="true"/> only for one non-expired issued proof.</returns>
    public bool TryValidate(string token, out string subject)
    {
        subject = string.Empty;
        if (!TryHashToken(token, out byte[] digest))
        {
            return false;
        }

        try
        {
            lock (gate)
            {
                DateTimeOffset now = timeProvider.GetUtcNow();
                RemoveExpiredAndSubject(now, subject: null);
                SessionProof? match = sessions.FirstOrDefault(candidate =>
                    CryptographicOperations.FixedTimeEquals(candidate.Digest, digest));
                if (match is null)
                {
                    return false;
                }

                subject = match.Subject;
                return true;
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(digest);
        }
    }

    /// <summary>Revokes one exact proof without revealing whether it previously existed.</summary>
    /// <param name="token">Untrusted cookie value.</param>
    public void Revoke(string token)
    {
        if (!TryHashToken(token, out byte[] digest))
        {
            return;
        }

        try
        {
            lock (gate)
            {
                for (int index = sessions.Count - 1; index >= 0; index--)
                {
                    if (!CryptographicOperations.FixedTimeEquals(sessions[index].Digest, digest))
                    {
                        continue;
                    }

                    CryptographicOperations.ZeroMemory(sessions[index].Digest);
                    sessions.RemoveAt(index);
                }
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(digest);
        }
    }

    /// <summary>Decodes one exact Base64URL token and hashes it without retaining raw material.</summary>
    /// <param name="token">Untrusted cookie value.</param>
    /// <param name="digest">SHA-256 digest when decoding succeeds.</param>
    /// <returns><see langword="true"/> only for the exact token shape and byte length.</returns>
    private static bool TryHashToken(string token, out byte[] digest)
    {
        digest = [];
        if (token is null || token.Length != EncodedTokenLength)
        {
            return false;
        }

        byte[] raw;
        try
        {
            raw = WebEncoders.Base64UrlDecode(token);
        }
        catch (FormatException)
        {
            return false;
        }

        try
        {
            if (raw.Length != TokenBytes)
            {
                return false;
            }

            digest = SHA256.HashData(raw);
            return true;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(raw);
        }
    }

    /// <summary>Removes expired entries and any previous proof for an explicitly re-enrolled subject.</summary>
    /// <param name="now">Current UTC expiry boundary.</param>
    /// <param name="subject">Optional subject whose previous proof must be replaced.</param>
    private void RemoveExpiredAndSubject(DateTimeOffset now, string? subject)
    {
        for (int index = sessions.Count - 1; index >= 0; index--)
        {
            SessionProof candidate = sessions[index];
            if (candidate.ExpiresAt > now &&
                (subject is null || !string.Equals(candidate.Subject, subject, StringComparison.Ordinal)))
            {
                continue;
            }

            CryptographicOperations.ZeroMemory(candidate.Digest);
            sessions.RemoveAt(index);
        }
    }

    /// <summary>Retains only a token digest, bounded subject and expiry inside the temporary process.</summary>
    /// <param name="Digest">Mutable digest buffer that is zeroed on removal.</param>
    /// <param name="Subject">Bounded test subject.</param>
    /// <param name="ExpiresAt">UTC proof expiry.</param>
    private sealed record SessionProof(byte[] Digest, string Subject, DateTimeOffset ExpiresAt);
}

/// <summary>Authenticates the exact host-only cookie only on same-origin HTTPS loopback requests.</summary>
public sealed class DashboardTvSignalRSandboxAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IHostEnvironment environment;
    private readonly DashboardTvSignalRSandboxSessionStore sessions;
    private readonly DashboardTvChangeHintSandboxEvidence evidence;

    /// <summary>Initialises the test-only cookie handler from ordinary framework services.</summary>
    /// <param name="options">Framework authentication options.</param>
    /// <param name="logger">Framework logger factory.</param>
    /// <param name="encoder">Framework URL encoder.</param>
    /// <param name="environment">Host environment that must match the sandbox exactly.</param>
    /// <param name="sessions">Bounded process-local test-session store.</param>
    /// <param name="evidence">Sanitised authentication counters.</param>
    public DashboardTvSignalRSandboxAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHostEnvironment environment,
        DashboardTvSignalRSandboxSessionStore sessions,
        DashboardTvChangeHintSandboxEvidence evidence)
        : base(options, logger, encoder)
    {
        this.environment = environment;
        this.sessions = sessions;
        this.evidence = evidence;
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!environment.IsEnvironment(DashboardTvSandboxEndpointRouteBuilderExtensions.EnvironmentName) ||
            !DashboardTvSignalRSandboxOrigin.IsExact(Context) ||
            !DashboardTvSignalRSandboxCookie.TryReadExact(Request, out string token) ||
            !sessions.TryValidate(token, out string subject))
        {
            evidence.AuthenticationDenied();
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        ClaimsIdentity identity = new([new Claim("sub", subject)], Scheme.Name);
        evidence.AuthenticationAccepted();
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}

/// <summary>Tracks only bounded aggregate counts needed to prove local connection and publication cleanup.</summary>
public sealed class DashboardTvChangeHintSandboxEvidence
{
    private int activeConnections;
    private int maximumConnections;
    private int acceptedAuthentications;
    private int deniedAuthentications;
    private int publishedHints;
    private int totalConnections;

    /// <summary>Records one authenticated connection without retaining connection or subject identifiers.</summary>
    public void Connected()
    {
        Interlocked.Increment(ref totalConnections);
        int active = Interlocked.Increment(ref activeConnections);
        int observed;
        do
        {
            observed = Volatile.Read(ref maximumConnections);
            if (active <= observed)
            {
                break;
            }
        }
        while (Interlocked.CompareExchange(ref maximumConnections, active, observed) != observed);
    }

    /// <summary>Records one completed connection while preventing a negative cleanup counter.</summary>
    public void Disconnected()
    {
        int active = Interlocked.Decrement(ref activeConnections);
        if (active < 0)
        {
            Interlocked.Exchange(ref activeConnections, 0);
        }
    }

    /// <summary>Records one accepted test-cookie authentication.</summary>
    public void AuthenticationAccepted() => Interlocked.Increment(ref acceptedAuthentications);

    /// <summary>Records one denied test-cookie authentication.</summary>
    public void AuthenticationDenied() => Interlocked.Increment(ref deniedAuthentications);

    /// <summary>Records one best-effort hint publication request.</summary>
    public void Published() => Interlocked.Increment(ref publishedHints);

    /// <summary>Returns a sanitised immutable snapshot containing no subject, connection, cookie or payload.</summary>
    /// <returns>Current bounded aggregate evidence.</returns>
    public DashboardTvChangeHintSandboxEvidenceSnapshot Snapshot() => new(
        Volatile.Read(ref activeConnections),
        Volatile.Read(ref maximumConnections),
        Volatile.Read(ref totalConnections),
        Volatile.Read(ref acceptedAuthentications),
        Volatile.Read(ref deniedAuthentications),
        Volatile.Read(ref publishedHints));
}

/// <summary>Contains sanitised aggregate counters for one temporary SignalR sandbox process.</summary>
/// <param name="ActiveConnections">Currently authenticated hub connections.</param>
/// <param name="MaximumConnections">Maximum concurrent authenticated hub connections.</param>
/// <param name="TotalConnections">Total authenticated hub connections observed.</param>
/// <param name="AcceptedAuthentications">Accepted cookie authentication evaluations.</param>
/// <param name="DeniedAuthentications">Denied cookie authentication evaluations.</param>
/// <param name="PublishedHints">Best-effort hint publication attempts accepted by the hub context.</param>
public sealed record DashboardTvChangeHintSandboxEvidenceSnapshot(
    int ActiveConnections,
    int MaximumConnections,
    int TotalConnections,
    int AcceptedAuthentications,
    int DeniedAuthentications,
    int PublishedHints);

/// <summary>Validates an exact same-origin HTTPS request over an IP-literal loopback boundary.</summary>
internal static class DashboardTvSignalRSandboxOrigin
{
    /// <summary>Returns whether scheme, remote endpoint, host and Origin all identify the same loopback origin.</summary>
    /// <param name="context">Current untrusted HTTP request context.</param>
    /// <returns><see langword="true"/> only for an exact same-origin HTTPS loopback request.</returns>
    public static bool IsExact(HttpContext context)
    {
        bool remoteLoopback = context.Connection.RemoteIpAddress is not null &&
            IPAddress.IsLoopback(context.Connection.RemoteIpAddress);
        if (!context.Request.IsHttps ||
            !remoteLoopback ||
            !IPAddress.TryParse(context.Request.Host.Host, out IPAddress? hostAddress) ||
            !IPAddress.IsLoopback(hostAddress) ||
            !context.Request.Headers.TryGetValue("Origin", out Microsoft.Extensions.Primitives.StringValues origins) ||
            origins.Count != 1 ||
            !Uri.TryCreate(origins[0], UriKind.Absolute, out Uri? origin) ||
            origin.Scheme != Uri.UriSchemeHttps ||
            !IPAddress.TryParse(origin.Host, out IPAddress? originAddress) ||
            !IPAddress.IsLoopback(originAddress) ||
            origin.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(origin.Query) ||
            !string.IsNullOrEmpty(origin.Fragment))
        {
            return false;
        }

        int requestPort = context.Request.Host.Port ?? 443;
        return origin.Port == requestPort && originAddress.Equals(hostAddress);
    }
}

/// <summary>Parses one exact host-only sandbox cookie without accepting duplicates or oversized headers.</summary>
internal static class DashboardTvSignalRSandboxCookie
{
    /// <summary>Reads one raw token from an exact cookie segment for subsequent cryptographic validation.</summary>
    /// <param name="request">Current untrusted HTTP request.</param>
    /// <param name="token">Bounded encoded token when exactly one cookie is present.</param>
    /// <returns><see langword="true"/> only when cookie framing is exact and bounded.</returns>
    public static bool TryReadExact(HttpRequest request, out string token)
    {
        token = string.Empty;
        Microsoft.Extensions.Primitives.StringValues cookieHeaders = request.Headers.Cookie;
        if (cookieHeaders.Count == 0 || cookieHeaders.Sum(value => value?.Length ?? 0) > 4_096)
        {
            return false;
        }

        string prefix = DashboardTvChangeHintSandboxEndpointRouteBuilderExtensions.SessionCookieName + "=";
        string[] candidates = cookieHeaders
            .SelectMany(value => value?.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? [])
            .Where(value => value.StartsWith(prefix, StringComparison.Ordinal))
            .Select(value => value[prefix.Length..])
            .ToArray();
        if (candidates.Length != 1)
        {
            return false;
        }

        token = candidates[0];
        return token.Length == 43;
    }
}
