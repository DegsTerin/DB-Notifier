// Module purpose: Keeps legacy command routes visibly unavailable without resolving persistence or delivery services.
using DBNotifier.Server.Api.Security;

namespace DBNotifier.Server.Api;

/// <summary>
/// Maps stable fail-closed tombstones for the legacy v1 command surface while command authorisation,
/// transport and execution remain outside the operational composition.
/// </summary>
public static class CommandSurfaceContainmentEndpointExtensions
{
    /// <summary>Gets the stable non-sensitive code returned by every contained command route.</summary>
    public const string UnavailableCode = "command.surface_unavailable";

    /// <summary>
    /// Maps authenticated tombstones that reject command creation, polling and acknowledgement before
    /// request-body binding, persistence access or command-service resolution can occur.
    /// </summary>
    /// <param name="endpoints">Route builder owned by the normal Server API composition.</param>
    /// <returns>The same route builder for further composition.</returns>
    public static IEndpointRouteBuilder MapContainedCommandSurface(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/instances/{instanceId:guid}/commands", Unavailable)
            .RequireAuthorization(ApiSecurityDefaults.HumanApiPolicy)
            .RequireRateLimiting("HumanApiRateLimit")
            .WithName("ContainedAdministrativeCommandCreation");
        endpoints.MapPost("/api/v1/agents/{agentId:guid}/commands:poll", Unavailable)
            .RequireAuthorization(ApiSecurityDefaults.AgentApiPolicy)
            .RequireRateLimiting("AgentApiRateLimit")
            .WithName("ContainedCommandPolling");
        endpoints.MapPost("/api/v1/agents/{agentId:guid}/commands:ack", Unavailable)
            .RequireAuthorization(ApiSecurityDefaults.AgentApiPolicy)
            .RequireRateLimiting("AgentApiRateLimit")
            .WithName("ContainedCommandAcknowledgement");
        return endpoints;
    }

    /// <summary>Returns the shared non-retryable problem without reading the request body or resolving services.</summary>
    /// <param name="context">Current request context used only to set response cache policy.</param>
    /// <returns>A stable HTTP 503 problem result with no command or persistence state.</returns>
    private static IResult Unavailable(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Json(
            new CommandSurfaceProblem(UnavailableCode, StatusCodes.Status503ServiceUnavailable, false),
            statusCode: StatusCodes.Status503ServiceUnavailable,
            contentType: "application/problem+json");
    }

    /// <summary>Defines the complete non-sensitive response contract for a contained command route.</summary>
    /// <param name="Code">Stable machine-readable containment code.</param>
    /// <param name="Status">HTTP status repeated in the problem body.</param>
    /// <param name="Retryable">Always false because v1 cannot be activated by retry.</param>
    private sealed record CommandSurfaceProblem(string Code, int Status, bool Retryable);
}
