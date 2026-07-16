// Module purpose: Owns endpoint-routed authentication defaults so routing selects the correct human or Agent identity before quotas.
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DBNotifier.Server.Api.Security;

/// <summary>
/// Defines canonical API policy names and selects the authentication scheme declared by routed endpoint metadata.
/// </summary>
public static class ApiSecurityDefaults
{
    /// <summary>Composite scheme evaluated after endpoint routing and before rate limiting.</summary>
    public const string RoutedAuthenticationScheme = "DBNotifierEndpointAuthentication";

    /// <summary>No-op scheme used by public and unrecognised endpoints so bearer or certificate handlers cannot create unauthorised work.</summary>
    public const string PublicEndpointAuthenticationScheme = "DBNotifierPublicEndpoint";

    /// <summary>Policy for authenticated human API callers.</summary>
    public const string HumanApiPolicy = "HumanApi";

    /// <summary>Policy for authenticated Agent API callers whose route identity must match.</summary>
    public const string AgentApiPolicy = "AgentApi";

    /// <summary>Policy for authenticated Agent observation ingestion.</summary>
    public const string AgentObservationIngestionPolicy = "AgentObservationIngestion";

    /// <summary>Selects certificate authentication for Agent policies, bearer authentication for the human policy and no-op authentication otherwise.</summary>
    /// <param name="context">HTTP context whose endpoint metadata was populated by routing.</param>
    /// <returns>The exact certificate, human bearer or public no-op authentication scheme.</returns>
    public static string SelectAuthenticationScheme(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Endpoint? endpoint = context.GetEndpoint();
        if (endpoint is null || endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return PublicEndpointAuthenticationScheme;
        }

        bool humanPolicy = false;
        foreach (IAuthorizeData authorisation in endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>())
        {
            if (authorisation.Policy is AgentApiPolicy or AgentObservationIngestionPolicy)
            {
                return CertificateAuthenticationDefaults.AuthenticationScheme;
            }

            humanPolicy |= authorisation.Policy == HumanApiPolicy;
        }

        return humanPolicy ? HumanAuthenticationDefaults.Scheme : PublicEndpointAuthenticationScheme;
    }
}

/// <summary>Returns no identity for public endpoints and provides a bounded unauthorised challenge for unknown protected metadata.</summary>
internal sealed class PublicEndpointAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Initialises the no-op handler from ordinary framework authentication services.</summary>
    /// <param name="options">Framework authentication-scheme options.</param>
    /// <param name="logger">Framework logger factory.</param>
    /// <param name="encoder">Framework URL encoder.</param>
    public PublicEndpointAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());

    /// <inheritdoc />
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
}
