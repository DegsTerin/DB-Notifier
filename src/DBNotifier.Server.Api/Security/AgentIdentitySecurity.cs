// Module purpose: Implements Agent Identity Security for the authorised server API without direct monitored-database access.
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Server.Api.Security;

/// <summary>Defines the private claim names created only after successful Agent certificate validation.</summary>
public static class AgentIdentityClaimTypes
{
    /// <summary>Identifies the enrolled Agent bound to the authenticated client certificate.</summary>
    public const string AgentId = "dbnotifier:agent_id";
}

/// <summary>
/// Resolves a presented client certificate through normalised certificate metadata and an independently active
/// Agent record. Revoked, superseded, inactive or out-of-validity certificates fail closed.
/// </summary>
/// <param name="contextFactory">Factory for isolated central-persistence reads.</param>
/// <param name="timeProvider">Trusted server clock used for certificate metadata validity checks.</param>
public sealed class AgentCertificateIdentityValidator(
    IDbContextFactory<ServerDbContext> contextFactory,
    TimeProvider timeProvider)
{
    /// <summary>Resolves an active Agent from one presented client certificate.</summary>
    /// <param name="certificate">Certificate already subjected to the ASP.NET Core chain checks.</param>
    /// <param name="cancellationToken">Cancellation signal for the persistence query.</param>
    /// <returns>The exact active Agent identifier, or <see langword="null"/> when trust cannot be proved.</returns>
    public async ValueTask<Guid?> ValidateAsync(
        X509Certificate2 certificate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        string? thumbprint = certificate.Thumbprint;
        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            return null;
        }

        string canonicalThumbprint = NormaliseThumbprint(thumbprint);
        DateTimeOffset now = timeProvider.GetUtcNow();
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        CertificateIdentityProjection? identity = await context.AgentCertificates
            .AsNoTracking()
            .Where(certificateRow =>
                certificateRow.Thumbprint == canonicalThumbprint &&
                certificateRow.State == "Active" &&
                certificateRow.RevokedAt == null)
            .Join(
                context.Agents.AsNoTracking().Where(agent => agent.State == "Active" && agent.RevokedAt == null),
                certificateRow => certificateRow.AgentId,
                agent => agent.AgentId,
                (certificateRow, agent) => new CertificateIdentityProjection(
                    agent.AgentId,
                    certificateRow.PublicKeySha256,
                    certificateRow.NotBefore,
                    certificateRow.NotAfter))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return identity is not null &&
            (identity.NotBefore is null || identity.NotBefore <= now) &&
            (identity.NotAfter is null || identity.NotAfter > now) &&
            MatchesStoredPublicKey(certificate, identity.PublicKeySha256)
                ? identity.AgentId
                : null;
    }

    /// <summary>Checks non-legacy certificate evidence against the exact presented subject public key.</summary>
    /// <param name="certificate">Presented client certificate.</param>
    /// <param name="expectedSha256">Stored uppercase SHA-256 digest, or null for a legacy row.</param>
    /// <returns><see langword="true"/> only when legacy compatibility applies or the digest matches exactly.</returns>
    private static bool MatchesStoredPublicKey(X509Certificate2 certificate, string? expectedSha256)
    {
        if (expectedSha256 is null)
        {
            return true;
        }

        using ECDsa? publicKey = certificate.GetECDsaPublicKey();
        if (publicKey is null)
        {
            return false;
        }

        byte[] expectedDigest;
        try
        {
            expectedDigest = Convert.FromHexString(expectedSha256);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] subjectPublicKey = publicKey.ExportSubjectPublicKeyInfo();
        byte[] actualDigest = SHA256.HashData(subjectPublicKey);
        try
        {
            return CryptographicOperations.FixedTimeEquals(expectedDigest, actualDigest);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(expectedDigest);
            CryptographicOperations.ZeroMemory(subjectPublicKey);
            CryptographicOperations.ZeroMemory(actualDigest);
        }
    }

    /// <summary>Normalises a framework thumbprint for the exact canonical persistence comparison.</summary>
    /// <param name="thumbprint">Framework-provided hexadecimal thumbprint.</param>
    /// <returns>Uppercase hexadecimal text without presentation separators.</returns>
    private static string NormaliseThumbprint(string thumbprint) =>
        thumbprint
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(":", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

    /// <summary>Projects the only certificate facts requiring provider-independent UTC comparison.</summary>
    /// <param name="AgentId">Resolved active Agent identifier.</param>
    /// <param name="PublicKeySha256">Optional public-key digest; null is restricted to legacy backfill.</param>
    /// <param name="NotBefore">Optional certificate validity start.</param>
    /// <param name="NotAfter">Optional certificate validity end.</param>
    private sealed record CertificateIdentityProjection(
        Guid AgentId,
        string? PublicKeySha256,
        DateTimeOffset? NotBefore,
        DateTimeOffset? NotAfter);
}

/// <summary>Requires the authenticated Agent claim to match the Agent identifier in the selected route.</summary>
public sealed class AgentRouteRequirement : IAuthorizationRequirement;

/// <summary>Enforces exact Agent claim-to-route binding after certificate authentication.</summary>
public sealed class AgentRouteAuthorizationHandler : AuthorizationHandler<AgentRouteRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AgentRouteRequirement requirement)
    {
        if (context.Resource is HttpContext httpContext &&
            Guid.TryParse(context.User.FindFirstValue(AgentIdentityClaimTypes.AgentId), out Guid identityAgentId) &&
            Guid.TryParse(httpContext.Request.RouteValues["agentId"]?.ToString(), out Guid routeAgentId) &&
            identityAgentId == routeAgentId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
