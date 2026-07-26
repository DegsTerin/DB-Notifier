// Module purpose: Applies bounded OIDC egress and offline inbound client-certificate trust to the Server API.
using System.Net;
using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using DBNotifier.Infrastructure.Http;
using DBNotifier.Infrastructure.Security;
using DBNotifier.Provider.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DBNotifier.Server.Api.Security;

/// <summary>
/// Configures human OIDC only from a complete local setting and constrains every metadata or signing-key request to
/// the configured HTTPS origin and the independently compiled human-identity egress policy.
/// </summary>
internal static class HumanOidcNetworkSecurity
{
    private const int MaximumAudienceLength = 300;
    private static readonly TimeSpan BackchannelTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Applies fail-closed JWT and OIDC backchannel options for the configured human identity boundary.</summary>
    /// <param name="options">JWT bearer options owned by the human authentication scheme.</param>
    /// <param name="configuration">Host configuration containing only non-secret authority and audience identifiers.</param>
    /// <param name="policySet">Immutable egress policies compiled before authentication composition.</param>
    /// <param name="handlerFactory">Factory that resolves, authorises and pins every physical backchannel connection.</param>
    /// <returns><see langword="true"/> when complete OIDC configuration was applied; otherwise false.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown with a sanitised message when configuration or its required network policy is invalid.
    /// </exception>
    internal static bool Configure(
        JwtBearerOptions options,
        IConfiguration configuration,
        NetworkEgressPolicySet policySet,
        NetworkBoundHttpMessageHandlerFactory handlerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(policySet);
        ArgumentNullException.ThrowIfNull(handlerFactory);

        options.RequireHttpsMetadata = true;
        options.IncludeErrorDetails = false;
        options.MapInboundClaims = false;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role",
        };

        string? authorityValue = configuration["HumanAuthentication:Authority"];
        string? audienceValue = configuration["HumanAuthentication:Audience"];
        bool hasAuthority = !string.IsNullOrWhiteSpace(authorityValue);
        bool hasAudience = !string.IsNullOrWhiteSpace(audienceValue);
        if (!hasAuthority && !hasAudience)
        {
            return false;
        }
        if (!hasAuthority || !hasAudience ||
            !TryValidateAuthority(authorityValue!, out Uri? authority) ||
            !IsValidAudience(audienceValue!))
        {
            throw InvalidConfiguration();
        }

        policySet.Require(NetworkEgressPolicyIds.HumanIdentity, authority.Port);
        string canonicalAuthority = authority.AbsoluteUri.TrimEnd('/');
        string audience = audienceValue!;
        options.Authority = canonicalAuthority;
        options.Audience = audience;
        options.BackchannelTimeout = BackchannelTimeout;
        options.BackchannelHttpHandler = new OidcBackchannelPolicyHandler(
            authority,
            handlerFactory.Create(NetworkEgressPolicyIds.HumanIdentity));
        options.TokenValidationParameters.ValidIssuer = canonicalAuthority;
        options.TokenValidationParameters.ValidAudience = audience;
        return true;
    }

    /// <summary>Validates one bounded HTTPS authority without embedded credentials or redirection data.</summary>
    /// <param name="value">Configured authority text.</param>
    /// <param name="authority">Validated absolute authority.</param>
    /// <returns><see langword="true"/> only when the exact authority is structurally safe.</returns>
    private static bool TryValidateAuthority(string value, out Uri authority)
    {
        authority = null!;
        if (value.Length > 2048 ||
            value != value.Trim() ||
            !Uri.TryCreate(value, UriKind.Absolute, out Uri? candidate) ||
            !string.Equals(candidate.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            candidate.HostNameType == UriHostNameType.Unknown ||
            !string.IsNullOrEmpty(candidate.UserInfo) ||
            !string.IsNullOrEmpty(candidate.Query) ||
            !string.IsNullOrEmpty(candidate.Fragment) ||
            candidate.Port is < 1 or > 65535)
        {
            return false;
        }

        authority = candidate;
        return true;
    }

    /// <summary>Checks the exact bounded audience identifier consumed by token validation.</summary>
    /// <param name="value">Configured audience.</param>
    /// <returns><see langword="true"/> only for bounded, trimmed, control-free text.</returns>
    private static bool IsValidAudience(string value) =>
        value.Length is > 0 and <= MaximumAudienceLength &&
        value == value.Trim() &&
        !value.Any(char.IsControl);

    /// <summary>Creates one sanitised configuration exception without echoing authority or audience values.</summary>
    /// <returns>A stable non-secret failure.</returns>
    private static InvalidOperationException InvalidConfiguration() =>
        new("Human identity configuration is outside the local security policy.");
}

/// <summary>
/// Restricts OIDC discovery and signing-key responses to one HTTPS origin and an exact bounded JSON payload.
/// The inner handler owns DNS revalidation, IP pinning, TLS policy, redirects, proxies and ambient credentials.
/// </summary>
internal sealed class OidcBackchannelPolicyHandler : DelegatingHandler
{
    private const int MaximumResponseBytes = 512 * 1024;
    private static readonly HashSet<string> JsonMediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "application/jwk-set+json",
    };
    private readonly string scheme;
    private readonly string idnHost;
    private readonly int port;

    /// <summary>Creates a bounded backchannel for one exact configured authority origin.</summary>
    /// <param name="authority">Validated absolute HTTPS authority.</param>
    /// <param name="innerHandler">Policy-bound direct HTTP handler.</param>
    internal OidcBackchannelPolicyHandler(
        Uri authority,
        HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentNullException.ThrowIfNull(innerHandler);
        scheme = authority.Scheme;
        idnHost = authority.IdnHost;
        port = authority.Port;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Uri? requestUri = request.RequestUri;
        if (request.Method != HttpMethod.Get ||
            requestUri is null ||
            !requestUri.IsAbsoluteUri ||
            !string.Equals(requestUri.Scheme, scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(requestUri.IdnHost, idnHost, StringComparison.OrdinalIgnoreCase) ||
            requestUri.Port != port ||
            !string.IsNullOrEmpty(requestUri.UserInfo) ||
            !string.IsNullOrEmpty(requestUri.Fragment))
        {
            throw PolicyFailure();
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        try
        {
            if ((int)response.StatusCode is >= 300 and < 400 ||
                response.Content is null ||
                response.Content.Headers.ContentEncoding.Count != 0)
            {
                throw PolicyFailure();
            }

            byte[] content;
            try
            {
                content = await BoundedHttpJsonReader
                    .ReadBytesAsync(
                        response.Content,
                        MaximumResponseBytes,
                        JsonMediaTypes,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (BoundedHttpJsonException)
            {
                throw PolicyFailure();
            }

            HttpContent original = response.Content;
            ByteArrayContent replacement = new(content);
            foreach ((string name, IEnumerable<string> values) in original.Headers)
            {
                replacement.Headers.TryAddWithoutValidation(name, values);
            }
            response.Content = replacement;
            original.Dispose();
            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    /// <summary>Creates a sanitised backchannel refusal that never echoes a URI or response field.</summary>
    /// <returns>A stable HTTP boundary failure.</returns>
    private static HttpRequestException PolicyFailure() =>
        new("Human identity backchannel traffic is outside the local security policy.");
}

/// <summary>Applies the shared offline, no-download client-certificate policy to Kestrel TLS handshakes.</summary>
internal static class InboundAgentTlsSecurity
{
    /// <summary>Replaces ambient client-certificate chain settings for one inbound TLS connection.</summary>
    /// <param name="options">Per-connection server authentication options supplied by Kestrel.</param>
    internal static void Apply(SslServerAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.CertificateRevocationCheckMode = X509RevocationMode.Offline;
        options.CertificateChainPolicy = OfflineCertificateChainPolicy.CreateClientAuthentication();
    }
}

/// <summary>
/// Authenticates only a TLS-approved Agent leaf with explicit client-authentication and signing extensions, then
/// preserves the existing active-enrolment, revocation, route-binding and sanitised audit decisions.
/// </summary>
internal sealed class AgentCertificateAuthenticationHandler :
    AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Initialises the handler from ordinary framework authentication services.</summary>
    /// <param name="options">Authentication scheme options.</param>
    /// <param name="logger">Logger factory.</param>
    /// <param name="encoder">URL encoder.</param>
    public AgentCertificateAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        X509Certificate2? certificate = await Context.Connection
            .GetClientCertificateAsync(Context.RequestAborted)
            .ConfigureAwait(false);
        if (certificate is null)
        {
            return AuthenticateResult.NoResult();
        }

        if (!AgentClientCertificateProfile.IsValid(certificate))
        {
            await WriteAuditAsync("Failed", "authentication.agent_certificate_invalid").ConfigureAwait(false);
            return AuthenticateResult.Fail("The Agent certificate profile is invalid.");
        }

        try
        {
            AgentCertificateIdentityValidator validator = Context.RequestServices
                .GetRequiredService<AgentCertificateIdentityValidator>();
            Guid? agentId = await validator
                .ValidateAsync(certificate, Context.RequestAborted)
                .ConfigureAwait(false);
            if (agentId is null)
            {
                await WriteAuditAsync("Denied", "authentication.agent_not_enrolled").ConfigureAwait(false);
                return AuthenticateResult.Fail("The Agent certificate is not enrolled or active.");
            }

            ClaimsIdentity identity = new(
                [new Claim(AgentIdentityClaimTypes.AgentId, agentId.Value.ToString("D"))],
                Scheme.Name);
            ClaimsPrincipal principal = new(identity);
            await WriteAuditAsync(
                "Succeeded",
                "authentication.agent_certificate_valid",
                agentId.Value.ToString("D")).ConfigureAwait(false);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
        catch (OperationCanceledException) when (Context.RequestAborted.IsCancellationRequested)
        {
            return AuthenticateResult.Fail("Agent certificate validation was cancelled.");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            await WriteAuditAsync("Unknown", "authentication.agent_validation_unavailable").ConfigureAwait(false);
            return AuthenticateResult.Fail("Agent certificate validation is unavailable.");
        }
    }

    /// <summary>Writes one bounded Agent authentication result through the existing best-effort audit boundary.</summary>
    /// <param name="outcome">Canonical authentication outcome.</param>
    /// <param name="code">Stable non-secret reason code.</param>
    /// <param name="actorId">Resolved Agent identifier, or unresolved before identity proof.</param>
    /// <returns>A task completing after the bounded audit attempt.</returns>
    private async ValueTask WriteAuditAsync(
        string outcome,
        string code,
        string actorId = "unresolved")
    {
        AuthenticationAuditWriter audit = Context.RequestServices.GetRequiredService<AuthenticationAuditWriter>();
        await audit.TryWriteAsync(
            new AuthenticationAuditEvent("Agent", actorId, Scheme.Name, outcome, code),
            Context.RequestAborted).ConfigureAwait(false);
    }
}

/// <summary>Validates the explicit leaf-certificate profile not implied by general operating-system trust.</summary>
internal static class AgentClientCertificateProfile
{
    private const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";

    /// <summary>Requires a non-CA leaf with explicit ClientAuth EKU and digital-signature key usage.</summary>
    /// <param name="certificate">TLS-approved client certificate.</param>
    /// <returns><see langword="true"/> only when every required leaf extension is present and exact.</returns>
    internal static bool IsValid(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        X509BasicConstraintsExtension[] constraints = certificate.Extensions
            .OfType<X509BasicConstraintsExtension>()
            .ToArray();
        X509KeyUsageExtension[] keyUsages = certificate.Extensions
            .OfType<X509KeyUsageExtension>()
            .ToArray();
        X509EnhancedKeyUsageExtension[] enhancedUsages = certificate.Extensions
            .OfType<X509EnhancedKeyUsageExtension>()
            .ToArray();
        return constraints is [{ CertificateAuthority: false }] &&
            keyUsages is [var keyUsage] &&
            keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature) &&
            enhancedUsages is [var enhancedUsage] &&
            enhancedUsage.EnhancedKeyUsages
                .OfType<Oid>()
                .Any(usage => string.Equals(
                    usage.Value,
                    ClientAuthenticationOid,
                    StringComparison.Ordinal));
    }
}
