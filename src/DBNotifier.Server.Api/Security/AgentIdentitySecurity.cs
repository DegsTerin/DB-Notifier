// Module purpose: Implements Agent Identity Security for the authorised server API without direct monitored-database access.
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Server.Api.Security;

public static class AgentIdentityClaimTypes
{
    public const string AgentId = "dbnotifier:agent_id";
}

public sealed class AgentCertificateIdentityValidator(
    IDbContextFactory<ServerDbContext> contextFactory)
{
    public async ValueTask<Guid?> ValidateAsync(
        X509Certificate2 certificate,
        CancellationToken cancellationToken)
    {
        string? thumbprint = certificate.Thumbprint;
        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            return null;
        }

        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        return await context.Agents
            .AsNoTracking()
            .Where(row =>
                row.CertificateThumbprint == thumbprint &&
                row.State == "Active" &&
                row.RevokedAt == null)
            .Select(row => (Guid?)row.AgentId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class AgentRouteRequirement : IAuthorizationRequirement;

public sealed class AgentRouteAuthorizationHandler : AuthorizationHandler<AgentRouteRequirement>
{
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
