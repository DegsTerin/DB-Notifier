// Module purpose: Exposes the versioned, bounded Agent Fleet HTTP surface without enabling providers, commands or external certificate issuance.
using System.Globalization;
using DBNotifier.Application.Access;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Server.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Primitives;

namespace DBNotifier.Server.Api;

/// <summary>
/// Maps the authorised Agent Fleet enrollment, heartbeat, read-only assignment, catalogue and revocation endpoints.
/// The extension is public so the same production route surface can be exercised by an isolated local E2E host.
/// </summary>
public static class AgentFleetEndpointRouteBuilderExtensions
{
    /// <summary>Names the dedicated pre-binding quota applied to one-time Agent enrollment.</summary>
    public const string EnrollmentRateLimitPolicy = "AgentEnrollmentRateLimit";

    private const string EnrollmentAuthorisationScheme = "DBN-Enrollment";
    private const string ProtocolVersionHeader = "DBN-Protocol-Version";
    private const string ProtocolMinimumHeader = "DBN-Protocol-Minimum";
    private const string ProtocolMaximumHeader = "DBN-Protocol-Maximum";
    private const string AgentVersionHeader = "DBN-Agent-Version";
    private const string MessageSchemaHeader = "DBN-Message-Schema";
    private const string ServerVersionHeader = "DBN-Server-Version";
    private const string CorrelationIdHeader = "DBN-Correlation-Id";
    private static readonly string ServerVersion =
        typeof(AgentFleetEndpointRouteBuilderExtensions).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

    /// <summary>Maps every endpoint in the restricted Agent Fleet integration slice.</summary>
    /// <param name="endpoints">Route builder owned by the local API host.</param>
    /// <returns>The same route builder for further composition.</returns>
    public static IEndpointRouteBuilder MapAgentFleetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/v1/agents/enroll", EnrolAgentAsync)
            .AllowAnonymous()
            .WithMetadata(ProtectedTransportRequirement.Instance)
            .RequireRateLimiting(EnrollmentRateLimitPolicy)
            .WithName("AgentFleetEnroll");

        endpoints.MapPost("/api/v1/agents/{agentId:guid}/heartbeats", RecordHeartbeatAsync)
            .RequireAuthorization(ApiSecurityDefaults.AgentApiPolicy)
            .RequireRateLimiting("AgentApiRateLimit")
            .WithName("AgentFleetHeartbeat");

        endpoints.MapGet("/api/v1/agents/{agentId:guid}/assignments", GetAssignmentsAsync)
            .RequireAuthorization(ApiSecurityDefaults.AgentApiPolicy)
            .RequireRateLimiting("AgentApiRateLimit")
            .WithName("AgentFleetAssignments");

        endpoints.MapGet("/api/v1/agents", GetCatalogueAsync)
            .RequireAuthorization(ApiSecurityDefaults.HumanApiPolicy)
            .RequireRateLimiting("HumanApiRateLimit")
            .WithName("AgentFleetCatalogue");

        endpoints.MapPost("/api/v1/agents/{agentId:guid}:revoke", RevokeAgentAsync)
            .RequireAuthorization(ApiSecurityDefaults.HumanApiPolicy)
            .RequireRateLimiting("HumanApiRateLimit")
            .WithName("AgentFleetRevoke");

        return endpoints;
    }

    /// <summary>Handles one HTTPS-only enrollment attempt without authenticating the one-time token as a reusable identity.</summary>
    /// <param name="request">Bounded non-secret enrollment body.</param>
    /// <param name="service">Application-level Agent Fleet coordinator.</param>
    /// <param name="context">Current HTTP context containing the one-time authorisation header.</param>
    /// <param name="cancellationToken">Request cancellation signal.</param>
    /// <returns>A safe enrollment result or generic fail-closed problem.</returns>
    private static async Task<IResult> EnrolAgentAsync(
        AgentEnrollmentRequest request,
        AgentFleetService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        ApplyVersionResponseHeaders(context);
        if (!TryReadAgentProtocolHeaders(context.Request, request.SchemaVersion, out string? agentVersion) ||
            !string.Equals(agentVersion, request.AgentVersion, StringComparison.Ordinal))
        {
            return ProtocolProblem(context);
        }

        string? enrollmentToken = ReadEnrollmentToken(context.Request);
        AgentEnrollmentOutcome outcome;
        try
        {
            outcome = await service
                .EnrollAsync(enrollmentToken, request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            return Problem(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid enrollment request",
                "enrollment.request_invalid",
                false);
        }

        return outcome.Disposition switch
        {
            AgentEnrollmentDisposition.Enrolled when outcome.AgentId is not null &&
                !outcome.CertificateDer.IsEmpty && outcome.CertificateNotAfter is not null => Results.Ok(outcome),
            AgentEnrollmentDisposition.Invalid => Problem(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid enrollment request",
                "enrollment.request_invalid",
                false),
            AgentEnrollmentDisposition.IssuerUnavailable => Problem(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "Agent enrollment is unavailable",
                "enrollment.unavailable",
                false),
            AgentEnrollmentDisposition.Denied or AgentEnrollmentDisposition.Conflict => Problem(
                context,
                StatusCodes.Status403Forbidden,
                "Agent enrollment denied",
                "enrollment.denied",
                false),
            _ => Problem(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "Agent enrollment is unavailable",
                "enrollment.unavailable",
                false),
        };
    }

    /// <summary>Records one mTLS route-bound heartbeat through the durable Application boundary.</summary>
    /// <param name="agentId">Agent identifier bound by the route authorisation handler.</param>
    /// <param name="request">Versioned heartbeat body.</param>
    /// <param name="service">Application-level Agent Fleet coordinator.</param>
    /// <param name="context">Current authenticated HTTP context.</param>
    /// <param name="cancellationToken">Request cancellation signal.</param>
    /// <returns>The durable acceptance result or a typed fail-closed problem.</returns>
    private static async Task<IResult> RecordHeartbeatAsync(
        Guid agentId,
        AgentHeartbeatRequest request,
        AgentFleetService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        ApplyVersionResponseHeaders(context);
        if (request.AgentId != agentId ||
            !TryReadAgentProtocolHeaders(context.Request, request.SchemaVersion, out string? agentVersion) ||
            !string.Equals(agentVersion, request.AgentVersion, StringComparison.Ordinal))
        {
            return request.AgentId == agentId
                ? ProtocolProblem(context)
                : Problem(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Invalid heartbeat request",
                    "heartbeat.agent_mismatch",
                    false);
        }

        AgentHeartbeatOutcome outcome;
        try
        {
            outcome = await service.RecordHeartbeatAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            return Problem(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid heartbeat request",
                "heartbeat.request_invalid",
                false);
        }

        return outcome.Disposition switch
        {
            AgentHeartbeatDisposition.Accepted or
            AgentHeartbeatDisposition.AcceptedWithGap or
            AgentHeartbeatDisposition.Duplicate => Results.Ok(outcome),
            AgentHeartbeatDisposition.Incompatible => ProtocolProblem(context),
            AgentHeartbeatDisposition.Conflict => Problem(
                context,
                StatusCodes.Status409Conflict,
                "Heartbeat conflicts with durable history",
                "heartbeat.conflict",
                false),
            AgentHeartbeatDisposition.AgentInactive => Problem(
                context,
                StatusCodes.Status403Forbidden,
                "Agent identity is inactive",
                "agent.identity_inactive",
                false),
            _ => Problem(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid heartbeat request",
                "heartbeat.request_invalid",
                false),
        };
    }

    /// <summary>Returns one complete mTLS route-bound read-only assignment snapshot with strong ETag reconciliation.</summary>
    /// <param name="agentId">Agent identifier bound by the route authorisation handler.</param>
    /// <param name="service">Application-level Agent Fleet coordinator.</param>
    /// <param name="context">Current authenticated HTTP context.</param>
    /// <param name="cancellationToken">Request cancellation signal.</param>
    /// <returns>A complete snapshot, HTTP 304, or a fail-closed problem without partial configuration.</returns>
    private static async Task<IResult> GetAssignmentsAsync(
        Guid agentId,
        AgentFleetService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        ApplyVersionResponseHeaders(context);
        if (!TryReadAgentProtocolHeaders(
                context.Request,
                AgentFleetProtocol.CurrentSchemaVersion,
                out string? agentVersion))
        {
            return ProtocolProblem(context);
        }

        if (!TryReadConditionalVersion(context.Request, out string? afterVersion))
        {
            return Problem(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid assignment condition",
                "assignments.condition_invalid",
                false);
        }

        AgentAssignmentOutcome outcome;
        try
        {
            outcome = await service
                .GetAssignmentsAsync(agentId, agentVersion!, afterVersion, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            return Problem(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid assignment request",
                "assignments.request_invalid",
                false);
        }

        if (outcome.Disposition is AgentAssignmentDisposition.Available or AgentAssignmentDisposition.NotModified &&
            !IsUpperHexDigest(outcome.CurrentVersion))
        {
            return AssignmentUnavailableProblem(context);
        }

        if (outcome.Disposition == AgentAssignmentDisposition.NotModified)
        {
            SetEntityTag(context, outcome.CurrentVersion!);
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        if (outcome.Disposition == AgentAssignmentDisposition.Available)
        {
            if (outcome.Snapshot is null ||
                !string.Equals(outcome.Snapshot.Version, outcome.CurrentVersion, StringComparison.Ordinal) ||
                outcome.Snapshot.SchemaVersion != AgentFleetProtocol.CurrentSchemaVersion ||
                outcome.Snapshot.AgentId != agentId ||
                outcome.Snapshot.Assignments.Count > AgentFleetProtocol.MaximumAssignments)
            {
                return AssignmentUnavailableProblem(context);
            }

            SetEntityTag(context, outcome.CurrentVersion!);
            return Results.Ok(outcome.Snapshot);
        }

        return outcome.Disposition switch
        {
            AgentAssignmentDisposition.AgentInactive => Problem(
                context,
                StatusCodes.Status403Forbidden,
                "Agent identity is inactive",
                "agent.identity_inactive",
                false),
            AgentAssignmentDisposition.LimitExceeded or
            AgentAssignmentDisposition.InvalidStoredConfiguration => AssignmentUnavailableProblem(context),
            AgentAssignmentDisposition.TemporarilyUnavailable => AssignmentUnavailableProblem(context, retryable: true),
            _ => AssignmentUnavailableProblem(context),
        };
    }

    /// <summary>Returns the RBAC-scoped human Agent catalogue without certificate or enrollment evidence.</summary>
    /// <param name="service">Application-level Agent Fleet coordinator.</param>
    /// <param name="actorResolver">Resolver for the authenticated human subject.</param>
    /// <param name="context">Current authenticated HTTP context.</param>
    /// <param name="cancellationToken">Request cancellation signal.</param>
    /// <returns>The safe scoped catalogue or an authorisation refusal.</returns>
    private static async Task<IResult> GetCatalogueAsync(
        AgentFleetService service,
        HumanActorResolver actorResolver,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        ApplyVersionResponseHeaders(context);
        HumanActor? actor = actorResolver.Resolve(context.User);
        if (actor is null)
        {
            return Results.Unauthorized();
        }

        AgentFleetCatalogue catalogue = await service
            .GetCatalogueAsync(actor, cancellationToken)
            .ConfigureAwait(false);
        if (!catalogue.Authorised)
        {
            return Problem(
                context,
                StatusCodes.Status403Forbidden,
                "Agent catalogue access denied",
                "authorization.denied",
                false);
        }

        return catalogue.Complete
            ? Results.Ok(catalogue)
            : Problem(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "Agent catalogue exceeds the bounded response envelope",
                catalogue.ErrorCode ?? "agent.catalogue_unavailable",
                false);
    }

    /// <summary>Revokes one Agent and its active certificates through exact human RBAC.</summary>
    /// <param name="agentId">Target Agent identifier.</param>
    /// <param name="request">Bounded machine-readable revocation reason.</param>
    /// <param name="service">Application-level Agent Fleet coordinator.</param>
    /// <param name="actorResolver">Resolver for the authenticated human subject.</param>
    /// <param name="context">Current authenticated HTTP context.</param>
    /// <param name="cancellationToken">Request cancellation signal.</param>
    /// <returns>The monotonic revocation result or a generic fail-closed problem.</returns>
    private static async Task<IResult> RevokeAgentAsync(
        Guid agentId,
        AgentRevocationRequest request,
        AgentFleetService service,
        HumanActorResolver actorResolver,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        ApplyVersionResponseHeaders(context);
        HumanActor? actor = actorResolver.Resolve(context.User);
        if (actor is null)
        {
            return Results.Unauthorized();
        }

        AgentRevocationOutcome outcome;
        try
        {
            outcome = await service
                .RevokeAsync(actor, agentId, request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            return Problem(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid Agent revocation request",
                "agent.revocation_invalid",
                false);
        }

        return outcome.Disposition switch
        {
            AgentRevocationDisposition.Revoked or AgentRevocationDisposition.AlreadyRevoked => Results.Ok(outcome),
            AgentRevocationDisposition.CertificatesReconciling => Results.Accepted(value: outcome),
            AgentRevocationDisposition.Denied => Problem(
                context,
                StatusCodes.Status403Forbidden,
                "Agent revocation denied",
                "authorization.denied",
                false),
            AgentRevocationDisposition.Conflict => Problem(
                context,
                StatusCodes.Status409Conflict,
                "Agent revocation could not be committed",
                "agent.revocation_conflict",
                false),
            _ => Problem(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "Agent revocation is unavailable",
                "agent.revocation_unavailable",
                false),
        };
    }

    /// <summary>Reads the exact one-time enrollment scheme without logging or reflecting its parameter.</summary>
    /// <param name="request">Request carrying the one-time credential.</param>
    /// <returns>The token parameter for Application validation, or <see langword="null"/>.</returns>
    private static string? ReadEnrollmentToken(HttpRequest request)
    {
        StringValues values = request.Headers.Authorization;
        if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
        {
            return null;
        }

        string header = values[0]!;
        int separator = header.IndexOf(' ');
        if (separator <= 0 || separator == header.Length - 1 ||
            !string.Equals(header[..separator], EnrollmentAuthorisationScheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string parameter = header[(separator + 1)..];
        return parameter.Length <= 128 && parameter == parameter.Trim() && !parameter.Any(char.IsWhiteSpace)
            ? parameter
            : null;
    }

    /// <summary>Validates the exact protocol, schema and bounded Agent-version request headers.</summary>
    /// <param name="request">Request headers to inspect.</param>
    /// <param name="schemaVersion">Schema required by the selected endpoint.</param>
    /// <param name="agentVersion">Validated Agent version when successful.</param>
    /// <returns><see langword="true"/> only for the supported protocol generation.</returns>
    private static bool TryReadAgentProtocolHeaders(
        HttpRequest request,
        int schemaVersion,
        out string? agentVersion)
    {
        agentVersion = null;
        if (schemaVersion != AgentFleetProtocol.CurrentSchemaVersion ||
            !HasSingleHeader(request, ProtocolVersionHeader, AgentFleetProtocol.CurrentProtocolVersion.ToString(CultureInfo.InvariantCulture)) ||
            !HasSingleHeader(request, MessageSchemaHeader, schemaVersion.ToString(CultureInfo.InvariantCulture)) ||
            !request.Headers.TryGetValue(AgentVersionHeader, out StringValues agentVersions) ||
            agentVersions.Count != 1 || !IsStableAgentVersion(agentVersions[0]))
        {
            return false;
        }

        agentVersion = agentVersions[0];
        return true;
    }

    /// <summary>Checks one request header for exactly one expected value.</summary>
    /// <param name="request">Request carrying the header.</param>
    /// <param name="name">Header name.</param>
    /// <param name="expected">Exact expected value.</param>
    /// <returns><see langword="true"/> only for one exact value.</returns>
    private static bool HasSingleHeader(HttpRequest request, string name, string expected) =>
        request.Headers.TryGetValue(name, out StringValues values) &&
        values.Count == 1 && string.Equals(values[0], expected, StringComparison.Ordinal);

    /// <summary>Reads one optional strong ETag from the query or <c>If-None-Match</c> header.</summary>
    /// <param name="request">Assignment request.</param>
    /// <param name="conditionalVersion">Validated uppercase digest, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the condition is absent or unambiguous and valid.</returns>
    private static bool TryReadConditionalVersion(HttpRequest request, out string? conditionalVersion)
    {
        conditionalVersion = null;
        StringValues queryVersions = request.Query["afterVersion"];
        if (queryVersions.Count > 1)
        {
            return false;
        }

        string? queryVersion = queryVersions.Count == 1 ? queryVersions[0] : null;
        if (queryVersion is not null && !IsUpperHexDigest(queryVersion))
        {
            return false;
        }

        StringValues entityTags = request.Headers.IfNoneMatch;
        if (entityTags.Count == 0)
        {
            conditionalVersion = queryVersion;
            return true;
        }

        if (entityTags.Count != 1 || string.IsNullOrEmpty(entityTags[0]))
        {
            return false;
        }

        string entityTag = entityTags[0]!;
        if (entityTag.Length != 66 || entityTag[0] != '"' || entityTag[^1] != '"')
        {
            return false;
        }

        string tagVersion = entityTag[1..^1];
        if (!IsUpperHexDigest(tagVersion) ||
            (queryVersion is not null && !string.Equals(queryVersion, tagVersion, StringComparison.Ordinal)))
        {
            return false;
        }

        conditionalVersion = tagVersion;
        return true;
    }

    /// <summary>Adds version negotiation and correlation evidence to a handled Agent Fleet response.</summary>
    /// <param name="context">Current HTTP context.</param>
    private static void ApplyVersionResponseHeaders(HttpContext context)
    {
        string protocol = AgentFleetProtocol.CurrentProtocolVersion.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers[ProtocolVersionHeader] = protocol;
        context.Response.Headers[ProtocolMinimumHeader] = protocol;
        context.Response.Headers[ProtocolMaximumHeader] = protocol;
        context.Response.Headers[MessageSchemaHeader] =
            AgentFleetProtocol.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers[ServerVersionHeader] = ServerVersion;
        context.Response.Headers[CorrelationIdHeader] = context.TraceIdentifier;
        context.Response.Headers.CacheControl = "no-store";
    }

    /// <summary>Sets one strong ETag after its digest has been independently validated.</summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="version">Uppercase SHA-256 digest.</param>
    private static void SetEntityTag(HttpContext context, string version) =>
        context.Response.Headers.ETag = $"\"{version}\"";

    /// <summary>Creates the canonical unsupported-protocol response without accepting state.</summary>
    /// <param name="context">Current HTTP context.</param>
    /// <returns>HTTP 426 Problem Details.</returns>
    private static IResult ProtocolProblem(HttpContext context) => Problem(
        context,
        StatusCodes.Status426UpgradeRequired,
        "Unsupported DB-Notifier protocol version",
        "protocol.version_unsupported",
        false);

    /// <summary>Creates one generic response when a complete safe assignment snapshot cannot be proved.</summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="retryable">Whether a transient consistency conflict permits bounded retry.</param>
    /// <returns>HTTP 503 Problem Details.</returns>
    private static IResult AssignmentUnavailableProblem(HttpContext context, bool retryable = false) => Problem(
        context,
        StatusCodes.Status503ServiceUnavailable,
        "Agent assignments are unavailable",
        "assignments.unavailable",
        retryable);

    /// <summary>Creates bounded Problem Details with correlation and retry semantics.</summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="statusCode">HTTP status.</param>
    /// <param name="title">Safe summary.</param>
    /// <param name="code">Stable machine-readable code.</param>
    /// <param name="retryable">Whether the same unchanged request may be retried.</param>
    /// <returns>Bounded Problem Details result.</returns>
    private static IResult Problem(
        HttpContext context,
        int statusCode,
        string title,
        string code,
        bool retryable) => Results.Problem(
            statusCode: statusCode,
            title: title,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
                ["correlationId"] = context.TraceIdentifier,
                ["retryable"] = retryable,
            });

    /// <summary>Checks the bounded stable Agent-version alphabet accepted by the Application contract.</summary>
    /// <param name="value">Candidate Agent version.</param>
    /// <returns><see langword="true"/> for one bounded stable value.</returns>
    private static bool IsStableAgentVersion(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 64 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');

    /// <summary>Checks one uppercase SHA-256 hexadecimal digest.</summary>
    /// <param name="value">Candidate digest.</param>
    /// <returns><see langword="true"/> for an exact uppercase digest.</returns>
    private static bool IsUpperHexDigest(string? value) =>
        value is { Length: 64 } &&
        value.All(character => char.IsAsciiDigit(character) || character is >= 'A' and <= 'F');
}

/// <summary>
/// Refuses certificate issuance in the production composition until an approved issuer and trust distribution
/// mechanism are configured by a separately authorised increment.
/// </summary>
public sealed class UnavailableAgentCertificateIssuer : IAgentCertificateIssuer
{
    /// <inheritdoc />
    public ValueTask<AgentCertificateIssueResult> IssueAsync(
        AgentCertificateIssueRequest request,
        DateTimeOffset issuedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new AgentCertificateIssueResult(
            AgentCertificateIssueDisposition.Unavailable,
            ReadOnlyMemory<byte>.Empty,
            null,
            null,
            ReadOnlyMemory<byte>.Empty,
            null,
            null));
    }
}
