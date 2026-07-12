using System.Security.Claims;
using DBNotifier.Application.Access;

namespace DBNotifier.Server.Api.Security;

public static class HumanAuthenticationDefaults
{
    public const string Scheme = "HumanBearer";
}

public sealed class HumanActorResolver
{
    public HumanActor? Resolve(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        string? subjectId = principal.FindFirstValue("sub");
        return principal.Identity?.IsAuthenticated == true &&
            !string.IsNullOrWhiteSpace(subjectId) && subjectId.Length <= 300
            ? new HumanActor(subjectId)
            : null;
    }
}
