// Module purpose: Maps the explicitly composed HTTPS-only command-transport safety sandbox endpoints without registering execution or normal runtime activation.
using System.Globalization;
using DBNotifier.Application.Synchronization;
using DBNotifier.Server.Api.Security;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;

namespace DBNotifier.Server.Api;

/// <summary>Maps the isolated v2 poll and acknowledgement surface only when a sandbox host calls this extension.</summary>
public static class CommandTransportSafetySandboxEndpointExtensions
{
    private const string ProtocolHeader = "DBN-Protocol-Version";
    private const string SchemaHeader = "DBN-Message-Schema";
    private const string AgentVersionHeader = "DBN-Agent-Version";

    /// <summary>Maps the two execution-ineligible command-transport routes into an explicit local sandbox composition.</summary>
    /// <param name="endpoints">Route builder owned by the temporary sandbox host.</param>
    /// <returns>The same route builder for further composition.</returns>
    public static IEndpointRouteBuilder MapCommandTransportSafetySandboxEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost(
                "/api/v2/sandbox/agents/{agentId:guid}/commands:poll",
                PollAsync)
            .RequireAuthorization(ApiSecurityDefaults.AgentApiPolicy)
            .RequireRateLimiting("AgentApiRateLimit")
            .WithMetadata(new RequestBodySizeMetadata(CommandTransportProtocol.MaximumHttpBodyBytes))
            .WithName("CommandTransportSafetySandboxPoll");
        endpoints.MapPost(
                "/api/v2/sandbox/agents/{agentId:guid}/commands:ack",
                AcknowledgeAsync)
            .RequireAuthorization(ApiSecurityDefaults.AgentApiPolicy)
            .RequireRateLimiting("AgentApiRateLimit")
            .WithMetadata(new RequestBodySizeMetadata(CommandTransportProtocol.MaximumHttpBodyBytes))
            .WithName("CommandTransportSafetySandboxAcknowledgement");
        return endpoints;
    }

    private static async Task<IResult> PollAsync(
        Guid agentId,
        CommandTransportPollRequest request,
        ICommandTransportServerStore store,
        TimeProvider timeProvider,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        ApplyResponseHeaders(context);
        if (!HeadersMatch(context.Request, request.SchemaVersion) || request.AgentId != agentId)
        {
            return request.AgentId == agentId
                ? Problem(StatusCodes.Status426UpgradeRequired, "command.transport_version_unsupported", null, false)
                : Problem(StatusCodes.Status400BadRequest, "command.transport_agent_mismatch", null, false);
        }

        try
        {
            CommandTransportPollResponse response = await store.PollAsync(
                request,
                timeProvider.GetUtcNow(),
                cancellationToken).ConfigureAwait(false);
            return Results.Json(response);
        }
        catch (CommandTransportException exception)
        {
            return ToProblem(exception);
        }
    }

    private static async Task<IResult> AcknowledgeAsync(
        Guid agentId,
        CommandTransportAcknowledgementRequest request,
        ICommandTransportServerStore store,
        TimeProvider timeProvider,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        ApplyResponseHeaders(context);
        if (!HeadersMatch(context.Request, request.SchemaVersion) || request.AgentId != agentId)
        {
            return request.AgentId == agentId
                ? Problem(StatusCodes.Status426UpgradeRequired, "command.transport_version_unsupported", null, false)
                : Problem(StatusCodes.Status400BadRequest, "command.transport_agent_mismatch", null, false);
        }

        try
        {
            CommandTransportAcknowledgementResponse response = await store.AcknowledgeAsync(
                request,
                timeProvider.GetUtcNow(),
                cancellationToken).ConfigureAwait(false);
            return Results.Json(response);
        }
        catch (CommandTransportException exception)
        {
            return ToProblem(exception);
        }
    }

    private static IResult ToProblem(CommandTransportException exception) => exception.Kind switch
    {
        CommandTransportFailureKind.AgentInactive =>
            Problem(StatusCodes.Status403Forbidden, exception.Code, exception.ExpectedSequence, false),
        CommandTransportFailureKind.Busy =>
            Problem(StatusCodes.Status429TooManyRequests, exception.Code, exception.ExpectedSequence, true),
        CommandTransportFailureKind.Conflict or CommandTransportFailureKind.Gap or
            CommandTransportFailureKind.Reordered =>
            Problem(StatusCodes.Status409Conflict, exception.Code, exception.ExpectedSequence, false),
        _ => Problem(StatusCodes.Status400BadRequest, exception.Code, exception.ExpectedSequence, false),
    };

    private static IResult Problem(int statusCode, string code, long? expectedSequence, bool retryable) =>
        Results.Json(
            new CommandTransportProblem(
                CommandTransportProtocol.CurrentSchemaVersion,
                code,
                expectedSequence,
                retryable),
            statusCode: statusCode,
            contentType: "application/problem+json");

    private static bool HeadersMatch(HttpRequest request, int schemaVersion) =>
        schemaVersion == CommandTransportProtocol.CurrentSchemaVersion &&
        HasSingleHeader(
            request,
            ProtocolHeader,
            CommandTransportProtocol.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture)) &&
        HasSingleHeader(
            request,
            SchemaHeader,
            CommandTransportProtocol.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture)) &&
        request.Headers.TryGetValue(AgentVersionHeader, out var agentVersion) &&
        agentVersion.Count == 1 && !string.IsNullOrWhiteSpace(agentVersion[0]) && agentVersion[0]!.Length <= 64;

    private static bool HasSingleHeader(HttpRequest request, string name, string expected) =>
        request.Headers.TryGetValue(name, out var values) && values.Count == 1 && values[0] == expected;

    private static void ApplyResponseHeaders(HttpContext context)
    {
        string version = CommandTransportProtocol.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers[ProtocolHeader] = version;
        context.Response.Headers[SchemaHeader] = version;
        context.Response.Headers.CacheControl = "no-store";
    }

    private sealed class RequestBodySizeMetadata(long maxRequestBodySize) : IRequestSizeLimitMetadata
    {
        /// <inheritdoc />
        public long? MaxRequestBodySize { get; } = maxRequestBodySize;
    }
}
