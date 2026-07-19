// Module purpose: Exposes a fail-closed read-only Dashboard TV snapshot only inside the explicitly enabled local sandbox.
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using DBNotifier.Application.Presentation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DBNotifier.Server.Api;

/// <summary>Registers the local-only Dashboard TV snapshot fixture and its authenticated read-only endpoint.</summary>
public static class DashboardTvSandboxEndpointRouteBuilderExtensions
{
    /// <summary>Gets the exact host-environment name required by the sandbox endpoint.</summary>
    public const string EnvironmentName = "DashboardTvSandbox";

    /// <summary>Gets the configuration section that controls explicit local activation.</summary>
    public const string ConfigurationSection = "DashboardTvSandbox";

    /// <summary>Gets the fixed, versioned read-only route consumed by the Dashboard adapter.</summary>
    public const string SnapshotRoute = "/api/v1/dashboard/tv-snapshot";

    /// <summary>Gets the bounded header used only to create an ephemeral local test subject.</summary>
    public const string TestSubjectHeader = "X-DBN-TV-Test-Human";

    /// <summary>Gets the dedicated authentication scheme that cannot authorise ordinary human routes.</summary>
    public const string TestAuthenticationScheme = "DashboardTvSandboxHuman";

    /// <summary>Gets the dedicated read policy used only by the snapshot route.</summary>
    public const string ReadPolicy = "DashboardTvSandboxRead";

    /// <summary>Registers the immutable fixture only when both sandbox guards are satisfied.</summary>
    /// <param name="services">Application service collection.</param>
    /// <param name="environment">Current host environment.</param>
    /// <param name="configuration">Current local configuration.</param>
    /// <returns><see langword="true"/> only when endpoint mapping is authorised for this process.</returns>
    public static bool AddDashboardTvSandbox(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);

        bool explicitlyEnabled = configuration.GetValue<bool>($"{ConfigurationSection}:Enabled");
        if (!environment.IsEnvironment(EnvironmentName) || !explicitlyEnabled)
        {
            return false;
        }

        services.TryAddSingleton<DashboardTvSandboxSnapshotSource>();
        services.TryAddSingleton<IDashboardTvSnapshotSource>(services =>
            services.GetRequiredService<DashboardTvSandboxSnapshotSource>());
        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, DashboardTvSandboxAuthenticationHandler>(
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

    /// <summary>Maps the authenticated endpoint when registration returned an authorised sandbox decision.</summary>
    /// <param name="endpoints">Endpoint route builder.</param>
    /// <param name="enabled">Exact activation result returned during service registration.</param>
    /// <returns>The unchanged route builder.</returns>
    public static IEndpointRouteBuilder MapDashboardTvSandboxEndpoint(
        this IEndpointRouteBuilder endpoints,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        if (!enabled)
        {
            return endpoints;
        }

        endpoints.MapGet(
                SnapshotRoute,
                (HttpContext context, IDashboardTvSnapshotSource source, TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                    ReadSnapshotAsync(context, source, timeProvider, cancellationToken))
            .RequireAuthorization(ReadPolicy)
            .RequireRateLimiting("HumanApiRateLimit");
        return endpoints;
    }

    private static async Task<IResult> ReadSnapshotAsync(
        HttpContext context,
        IDashboardTvSnapshotSource source,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        DashboardTvSnapshot snapshot;
        try
        {
            snapshot = await source.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Dashboard TV sandbox projection unavailable",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "dashboard_tv.projection_unavailable",
                });
        }

        if (!DashboardTvSnapshotValidator.TryValidate(snapshot, timeProvider.GetUtcNow(), out string errorCode))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Dashboard TV sandbox snapshot unavailable",
                extensions: new Dictionary<string, object?> { ["code"] = errorCode });
        }

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonSerializerOptions.Web);
        string entityTag = $"\"sha256-{Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant()}\"";
        context.Response.Headers.ETag = entityTag;
        context.Response.Headers["DBN-Snapshot-Schema"] = DashboardTvSnapshotContract.CurrentSchemaVersion;
        context.Response.Headers.CacheControl = "private, no-cache";

        if (context.Request.Headers.TryGetValue("If-None-Match", out Microsoft.Extensions.Primitives.StringValues candidates))
        {
            string[] values = candidates
                .SelectMany(value => value?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? [])
                .ToArray();
            if (values.Length == 0 || values.Any(value => !IsStrongEntityTag(value)))
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid conditional snapshot request",
                    extensions: new Dictionary<string, object?> { ["code"] = "dashboard_tv.etag_invalid" });
            }

            if (values.Contains(entityTag, StringComparer.Ordinal))
            {
                return TypedResults.StatusCode(StatusCodes.Status304NotModified);
            }
        }

        return TypedResults.Bytes(body, "application/json; charset=utf-8");
    }

    private static bool IsStrongEntityTag(string value) =>
        value.Length >= 2 && value[0] == '"' && value[^1] == '"' && !value.StartsWith("W/", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Creates a bounded test subject only for an exact HTTPS loopback sandbox request.</summary>
internal sealed class DashboardTvSandboxAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IHostEnvironment environment;

    /// <summary>Initialises the test-only handler from ordinary framework services and the current host guard.</summary>
    /// <param name="options">Framework authentication options.</param>
    /// <param name="logger">Framework logger factory.</param>
    /// <param name="encoder">Framework URL encoder.</param>
    /// <param name="environment">Host environment that must match the exact sandbox name.</param>
    public DashboardTvSandboxAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHostEnvironment environment)
        : base(options, logger, encoder)
    {
        this.environment = environment;
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        bool loopback = Context.Connection.RemoteIpAddress is not null &&
            System.Net.IPAddress.IsLoopback(Context.Connection.RemoteIpAddress);
        if (!environment.IsEnvironment(DashboardTvSandboxEndpointRouteBuilderExtensions.EnvironmentName) ||
            !Request.IsHttps ||
            !loopback ||
            !Request.Headers.TryGetValue(
                DashboardTvSandboxEndpointRouteBuilderExtensions.TestSubjectHeader,
                out Microsoft.Extensions.Primitives.StringValues subject) ||
            subject.Count != 1 ||
            string.IsNullOrWhiteSpace(subject[0]) ||
            subject[0]!.Length > 200)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        ClaimsIdentity identity = new([new Claim("sub", subject[0]!)], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}

/// <summary>Owns the deterministic, immutable, non-operational snapshot used only by the local sandbox.</summary>
public sealed class DashboardTvSandboxSnapshotSource : IDashboardTvSnapshotSource
{
    /// <summary>Initialises one immutable snapshot from the process clock without contacting an Agent or provider.</summary>
    /// <param name="timeProvider">Clock used once to anchor deterministic relative fixture timestamps.</param>
    public DashboardTvSandboxSnapshotSource(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        DateTimeOffset generatedAt = timeProvider.GetUtcNow();
        Snapshot = new DashboardTvSnapshot(
            DashboardTvSnapshotContract.CurrentSchemaVersion,
            FormatUtc(generatedAt),
            [
                CreateItem("88d74662-18a2-4f08-bc7a-7d61e68a3137", "Finance sandbox", "postgresql", "Implemented fixture", "Sandbox", "Local process", "healthy", generatedAt.AddSeconds(-38), generatedAt.AddSeconds(-37), 24),
                CreateItem("cb282539-d615-4ac0-98ff-dd6da6de125d", "Orders sandbox", "mysql", "Planned fixture", "Sandbox", "Local process", "degraded", generatedAt.AddMinutes(-2), generatedAt.AddMinutes(-2).AddSeconds(1), 86),
            ]);
    }

    /// <summary>Gets the immutable snapshot for the lifetime of the temporary sandbox process.</summary>
    public DashboardTvSnapshot Snapshot { get; }

    /// <inheritdoc />
    public ValueTask<DashboardTvSnapshot> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Snapshot);
    }

    private static DashboardTvInventoryItem CreateItem(
        string instanceId,
        string displayName,
        string providerType,
        string supportLabel,
        string environment,
        string locationLabel,
        string status,
        DateTimeOffset observedAt,
        DateTimeOffset receivedAt,
        int latencyMilliseconds) =>
        new(
            Guid.Parse(instanceId),
            displayName,
            providerType,
            supportLabel,
            environment,
            locationLabel,
            status,
            FormatUtc(observedAt),
            FormatUtc(receivedAt),
            latencyMilliseconds,
            true);

    private static string FormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture);
}
