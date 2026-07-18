// Module purpose: Defines Authorized Operations application behaviour without depending on concrete providers or user interfaces.
using System.Text.Json;

namespace DBNotifier.Application.Access;

/// <summary>Defines stable server-side permission codes without granting any permission by declaration alone.</summary>
public static class PlatformPermissions
{
    /// <summary>Reads database-instance catalogue entries within the actor's scope.</summary>
    public const string InstancesRead = "instances.read";

    /// <summary>Reads safe Agent Fleet facts within the actor's scope.</summary>
    public const string AgentsRead = "agents.read";

    /// <summary>Revokes an Agent identity and its active certificates within the actor's scope.</summary>
    public const string AgentsRevoke = "agents.revoke";

    /// <summary>Creates an authorised administrative command receipt without executing it.</summary>
    public const string CommandsCreate = "commands.create";

    /// <summary>Reads append-only audit evidence under global audit scope.</summary>
    public const string AuditRead = "audit.read";
}

public sealed record HumanActor(string SubjectId);

public sealed record AuthorizedInstance(
    Guid InstanceId,
    string DisplayName,
    string ProviderType,
    string Environment,
    Guid? AssignedAgentId,
    bool Enabled);

public sealed record CreateAdministrativeCommandRequest(
    string IdempotencyKey,
    string CapabilityId,
    JsonElement TypedParameters,
    string Reason,
    DateTimeOffset ExpiresAt,
    string ExpectedAgentVersion,
    string ExpectedProviderVersion);

public sealed record AdministrativeCommandReceipt(
    Guid CommandId,
    Guid InstanceId,
    string CapabilityId,
    string State,
    DateTimeOffset RequestedAt,
    DateTimeOffset ExpiresAt);

public sealed record AuditQuery(
    int Offset = 0,
    int PageSize = 50,
    DateTimeOffset? SnapshotAt = null,
    string? ActorId = null,
    string? Action = null,
    string? Outcome = null);

public sealed record AuditEntryView(
    Guid AuditEntryId,
    DateTimeOffset OccurredAt,
    string ActorType,
    string ActorId,
    string Action,
    string TargetType,
    string TargetId,
    string Outcome,
    Guid CorrelationId,
    string DetailsJson);

public sealed record AuthorizedAuditPage(
    bool Authorized,
    IReadOnlyList<AuditEntryView> Items,
    int? NextOffset,
    DateTimeOffset SnapshotAt);

public enum CommandCreationDisposition
{
    Created,
    Duplicate,
    Denied,
    IdempotencyConflict,
    CapabilityUnavailable,
    Invalid,
}

public sealed record CommandCreationResult(
    CommandCreationDisposition Disposition,
    AdministrativeCommandReceipt? Command,
    string? ErrorCode = null);

public interface IAuthorizedOperationsStore
{
    ValueTask<IReadOnlyList<AuthorizedInstance>> GetAuthorizedInstancesAsync(
        string subjectId,
        string permissionCode,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    ValueTask<CommandCreationResult> CreateCommandAsync(
        string subjectId,
        Guid instanceId,
        string permissionCode,
        ValidatedCommandRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    ValueTask AuditCommandRejectionAsync(
        string subjectId,
        Guid instanceId,
        string errorCode,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    ValueTask<AuthorizedAuditPage> QueryAuditAsync(
        string subjectId,
        string permissionCode,
        AuditQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record ValidatedCommandRequest(
    string IdempotencyKey,
    string CapabilityId,
    string TypedParametersJson,
    string Reason,
    DateTimeOffset ExpiresAt,
    string ExpectedAgentVersion,
    string ExpectedProviderVersion);

public sealed class AuthorizedOperationsService(
    IAuthorizedOperationsStore store,
    TimeProvider timeProvider)
{
    private static readonly string[] ForbiddenParameterFragments =
    [
        "password",
        "secret",
        "token",
        "connectionstring",
        "privatekey",
        "sql",
        "query",
        "script",
        "shell",
        "command",
        "arguments",
        "executable",
    ];

    public ValueTask<IReadOnlyList<AuthorizedInstance>> GetCatalogAsync(
        HumanActor actor,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return store.GetAuthorizedInstancesAsync(
            actor.SubjectId,
            PlatformPermissions.InstancesRead,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }

    public async ValueTask<CommandCreationResult> CreateCommandAsync(
        HumanActor actor,
        Guid instanceId,
        CreateAdministrativeCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentOutOfRangeException.ThrowIfEqual(instanceId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(request);
        DateTimeOffset now = timeProvider.GetUtcNow();
        ValidatedCommandRequest? validated = ValidateCommand(request, now);
        if (validated is null)
        {
            await store.AuditCommandRejectionAsync(
                actor.SubjectId,
                instanceId,
                "command.request_invalid",
                now,
                cancellationToken).ConfigureAwait(false);
            return new CommandCreationResult(
                CommandCreationDisposition.Invalid,
                null,
                "command.request_invalid");
        }

        return await store.CreateCommandAsync(
            actor.SubjectId,
            instanceId,
            PlatformPermissions.CommandsCreate,
            validated,
            now,
            cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<AuthorizedAuditPage> QueryAuditAsync(
        HumanActor actor,
        AuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(query);
        DateTimeOffset now = timeProvider.GetUtcNow();
        if (query.Offset is < 0 or > 10000 || query.PageSize is < 1 or > 100 ||
            query.SnapshotAt > now ||
            query.ActorId?.Length > 300 || query.Action?.Length > 150 ||
            query.Outcome?.Length > 32)
        {
            throw new ArgumentException("Audit query is outside policy.", nameof(query));
        }

        return store.QueryAuditAsync(
            actor.SubjectId,
            PlatformPermissions.AuditRead,
            query,
            now,
            cancellationToken);
    }

    private static void ValidateActor(HumanActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor.SubjectId);
        if (actor.SubjectId.Length > 300)
        {
            throw new ArgumentException("Human subject identifier is outside policy.", nameof(actor));
        }
    }

    private static ValidatedCommandRequest? ValidateCommand(
        CreateAdministrativeCommandRequest request,
        DateTimeOffset now)
    {
        if (!IsStableIdentifier(request.IdempotencyKey, 8, 200) ||
            !IsStableIdentifier(request.CapabilityId, 3, 100) ||
            string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length is < 10 or > 1000 ||
            request.ExpiresAt < now.AddMinutes(1) || request.ExpiresAt > now.AddMinutes(15) ||
            string.IsNullOrWhiteSpace(request.ExpectedAgentVersion) || request.ExpectedAgentVersion.Length > 64 ||
            string.IsNullOrWhiteSpace(request.ExpectedProviderVersion) || request.ExpectedProviderVersion.Length > 64 ||
            request.TypedParameters.ValueKind != JsonValueKind.Object ||
            ContainsForbiddenParameter(request.TypedParameters))
        {
            return null;
        }

        string parametersJson = request.TypedParameters.GetRawText();
        if (parametersJson.Length > 8192)
        {
            return null;
        }

        return new ValidatedCommandRequest(
            request.IdempotencyKey,
            request.CapabilityId,
            parametersJson,
            request.Reason.Trim(),
            request.ExpiresAt,
            request.ExpectedAgentVersion.Trim(),
            request.ExpectedProviderVersion.Trim());
    }

    private static bool IsStableIdentifier(string? value, int minimumLength, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < minimumLength || value.Length > maximumLength)
        {
            return false;
        }

        return value.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');
    }

    private static bool ContainsForbiddenParameter(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Any(ContainsForbiddenParameter);
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (ForbiddenParameterFragments.Any(fragment =>
                    property.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (ContainsForbiddenParameter(property.Value))
            {
                return true;
            }
        }

        return false;
    }
}
