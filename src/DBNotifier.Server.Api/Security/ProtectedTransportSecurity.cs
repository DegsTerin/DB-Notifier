// Module purpose: Rejects plaintext transport before protected API endpoints perform authentication or bind payloads.
using Microsoft.AspNetCore.Authorization;

namespace DBNotifier.Server.Api.Security;

/// <summary>
/// Marks an endpoint whose payload or credential requires HTTPS even when the endpoint deliberately allows
/// anonymous ASP.NET Core access, such as one-time Agent enrollment.
/// </summary>
public sealed class ProtectedTransportRequirement
{
    /// <summary>Gets the shared immutable endpoint marker.</summary>
    public static ProtectedTransportRequirement Instance { get; } = new();

    /// <summary>Prevents marker instances outside this assembly so endpoint metadata remains canonical.</summary>
    private ProtectedTransportRequirement()
    {
    }
}

/// <summary>
/// Enforces HTTPS for every endpoint carrying ASP.NET Core authorisation metadata or an explicit protected
/// transport marker. Health endpoints without protected metadata remain available to local hosting probes.
/// </summary>
/// <param name="next">Next component in the routed HTTP pipeline.</param>
public sealed class ProtectedTransportMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Rejects a protected plaintext request or invokes the next middleware for an HTTPS or public request.
    /// </summary>
    /// <param name="context">The current HTTP request and response context.</param>
    /// <returns>A task that completes after rejection or downstream processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Endpoint? endpoint = context.GetEndpoint();
        bool protectedEndpoint = endpoint?.Metadata.GetMetadata<IAuthorizeData>() is not null ||
            endpoint?.Metadata.GetMetadata<ProtectedTransportRequirement>() is not null;
        if (protectedEndpoint && !context.Request.IsHttps)
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            context.Response.ContentType = "application/problem+json";
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsJsonAsync(
                new
                {
                    type = "about:blank",
                    title = "HTTPS transport required",
                    status = StatusCodes.Status426UpgradeRequired,
                    code = "transport.https_required",
                    retryable = false,
                },
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
