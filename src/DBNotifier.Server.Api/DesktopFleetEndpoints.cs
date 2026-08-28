// Module purpose: Exposes the bounded human-authorised Desktop Fleet snapshot without provider, command or credential data.
using DBNotifier.Application.Access;
using DBNotifier.Application.Presentation;
using DBNotifier.Server.Api.Security;

namespace DBNotifier.Server.Api;

/// <summary>Maps the provider-neutral read-only Desktop Fleet endpoint for ordinary Server and isolated test hosts.</summary>
public static class DesktopFleetEndpointRouteBuilderExtensions
{
    /// <summary>Maps one human-authorised, rate-limited latest-observation snapshot read.</summary>
    /// <param name="endpoints">Route builder owned by the current Server composition.</param>
    /// <returns>The unchanged route builder.</returns>
    public static IEndpointRouteBuilder MapDesktopFleetEndpoint(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet(DesktopFleetApiContract.SnapshotRoute, ReadAsync)
            .RequireAuthorization(ApiSecurityDefaults.HumanApiPolicy)
            .RequireRateLimiting("HumanApiRateLimit")
            .WithName("DesktopFleetSnapshotRead");
        return endpoints;
    }

    /// <summary>Validates the exact protocol request and returns only actor-scoped provider-neutral observations.</summary>
    private static async Task<IResult> ReadAsync(
        AuthorizedOperationsService operations,
        HumanActorResolver actorResolver,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!HasExactVersionHeaders(httpContext))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status426UpgradeRequired,
                title: "Unsupported DB-Notifier Desktop Fleet protocol version",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "desktop_fleet.protocol_unsupported",
                    ["retryable"] = false,
                });
        }

        HumanActor? actor = actorResolver.Resolve(httpContext.User);
        if (actor is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            DesktopFleetApiSnapshot snapshot = await operations
                .GetDesktopFleetSnapshotAsync(actor, cancellationToken)
                .ConfigureAwait(false);
            if (!DesktopFleetApiSnapshotMapper.TryMap(snapshot, snapshot.GeneratedAt, out _))
            {
                throw new InvalidOperationException("desktop_fleet.snapshot_invalid");
            }

            httpContext.Response.Headers["DBN-Protocol-Version"] = "1";
            httpContext.Response.Headers["DBN-Message-Schema"] = DesktopFleetApiContract.CurrentSchemaVersion;
            httpContext.Response.Headers.CacheControl = "no-store";
            return Results.Ok(snapshot);
        }
        catch (InvalidOperationException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Desktop Fleet snapshot unavailable",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "desktop_fleet.snapshot_unavailable",
                    ["retryable"] = true,
                });
        }
    }

    /// <summary>Requires one exact protocol version and Desktop Fleet message schema.</summary>
    private static bool HasExactVersionHeaders(HttpContext context) =>
        context.Request.Headers.TryGetValue("DBN-Protocol-Version", out var protocol) &&
        protocol.Count == 1 && protocol[0] == "1" &&
        context.Request.Headers.TryGetValue("DBN-Message-Schema", out var schema) &&
        schema.Count == 1 && schema[0] == DesktopFleetApiContract.CurrentSchemaVersion;
}
