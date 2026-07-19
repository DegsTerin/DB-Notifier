// Module purpose: Maps the authenticated, versioned Agent observation-ingestion boundary without coupling it to the Server entry point.
using DBNotifier.Application.Synchronization;
using DBNotifier.Server.Api.Security;

namespace DBNotifier.Server.Api;

/// <summary>Maps the canonical Agent observation batch endpoint for the Server and isolated integration hosts.</summary>
public static class ObservationIngestionEndpointRouteBuilderExtensions
{
    /// <summary>Gets the versioned Agent observation batch route.</summary>
    public const string BatchRoute = "/api/v1/agents/{agentId:guid}/observations:batch";

    /// <summary>Maps the mTLS-authorised and rate-limited observation ingestion endpoint.</summary>
    /// <param name="endpoints">Endpoint route builder owned by the current Server composition.</param>
    /// <returns>The unchanged route builder.</returns>
    public static IEndpointRouteBuilder MapObservationIngestionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost(BatchRoute, IngestAsync)
            .RequireAuthorization(ApiSecurityDefaults.AgentObservationIngestionPolicy)
            .RequireRateLimiting("AgentApiRateLimit")
            .WithName("AgentObservationBatchIngestion");
        return endpoints;
    }

    /// <summary>Validates protocol headers, route binding and the bounded batch before durable ingestion.</summary>
    private static async Task<IResult> IngestAsync(
        Guid agentId,
        ObservationBatchRequest request,
        ObservationBatchIngestor ingestor,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!HasExactVersionHeaders(httpContext))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status426UpgradeRequired,
                title: "Unsupported DB-Notifier protocol version",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "protocol.version_unsupported",
                    ["retryable"] = false,
                });
        }

        try
        {
            if (request.AgentId != agentId)
            {
                throw new ArgumentException("The observation batch Agent does not match the authorised route.");
            }

            ObservationBatchResult result = await ingestor
                .HandleAsync(request, cancellationToken)
                .ConfigureAwait(false);
            return Results.Ok(result);
        }
        catch (ArgumentException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid observation batch",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "observation.batch_invalid",
                    ["retryable"] = false,
                });
        }
    }

    /// <summary>Requires one exact protocol, message-schema and bounded Agent-version header.</summary>
    private static bool HasExactVersionHeaders(HttpContext context) =>
        context.Request.Headers.TryGetValue("DBN-Protocol-Version", out var protocol) &&
        protocol.Count == 1 && protocol[0] == "1" &&
        context.Request.Headers.TryGetValue("DBN-Message-Schema", out var schema) &&
        schema.Count == 1 && schema[0] == "1" &&
        context.Request.Headers.TryGetValue("DBN-Agent-Version", out var agentVersion) &&
        agentVersion.Count == 1 && agentVersion[0] is { } version &&
        IsStableAgentVersion(version);

    /// <summary>Accepts the same bounded transport-safe Agent-version alphabet used by the Agent Fleet routes.</summary>
    private static bool IsStableAgentVersion(string value) =>
        value.Length is >= 1 and <= 64 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');
}
