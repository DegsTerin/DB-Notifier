// Module purpose: Implements Authorized Operations Store for central PostgreSQL persistence with transactional and authorisation boundaries.
using System.Text.Json;
using DBNotifier.Application.Access;
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Server.PostgreSql;

public sealed class AuthorizedOperationsStore(
    IDbContextFactory<ServerDbContext> contextFactory) : IAuthorizedOperationsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask AuditCommandRejectionAsync(
        string subjectId,
        Guid instanceId,
        string errorCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        AddAudit(
            context,
            subjectId,
            "command.create",
            instanceId,
            "Denied",
            Guid.NewGuid(),
            errorCode,
            now);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<AuthorizedAuditPage> QueryAuditAsync(
        string subjectId,
        string permissionCode,
        AuditQuery query,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        Guid? userId = await context.Users
            .AsNoTracking()
            .Where(row => row.SubjectId == subjectId && row.State == "Active")
            .Select(row => (Guid?)row.UserId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        AuthorizationScope[] scopes = userId is null
            ? []
            : await GetScopesAsync(
                context,
                userId.Value,
                permissionCode,
                now,
                cancellationToken).ConfigureAwait(false);
        bool authorized = scopes.Any(scope => scope.ScopeType == "Global" && scope.ScopeValue == "*");
        if (!authorized)
        {
            AddAudit(
                context,
                subjectId,
                "audit.read",
                Guid.Empty,
                "Denied",
                Guid.NewGuid(),
                "authorization.denied",
                now,
                targetType: "AuditLog");
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new AuthorizedAuditPage(false, [], null, query.SnapshotAt ?? now);
        }

        IQueryable<AuditEntryRow> entries = context.AuditEntries.AsNoTracking();
        DateTimeOffset snapshotAt = query.SnapshotAt ?? now;
        if (!string.IsNullOrWhiteSpace(query.ActorId))
        {
            entries = entries.Where(row => row.ActorId == query.ActorId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            entries = entries.Where(row => row.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.Outcome))
        {
            entries = entries.Where(row => row.Outcome == query.Outcome);
        }

        AuditEntryView[] items;
        if (string.Equals(
                context.Database.ProviderName,
                "Microsoft.EntityFrameworkCore.Sqlite",
                StringComparison.Ordinal))
        {
            AuditEntryRow[] testRows = await entries
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
            items = testRows
                .Where(row => row.OccurredAt < snapshotAt)
                .OrderByDescending(row => row.OccurredAt)
                .ThenByDescending(row => row.AuditEntryId)
                .Skip(query.Offset)
                .Take(query.PageSize + 1)
                .Select(ToAuditView)
                .ToArray();
        }
        else
        {
            items = await entries
                .Where(row => row.OccurredAt < snapshotAt)
                .OrderByDescending(row => row.OccurredAt)
                .ThenByDescending(row => row.AuditEntryId)
                .Skip(query.Offset)
                .Take(query.PageSize + 1)
                .Select(row => new AuditEntryView(
                    row.AuditEntryId,
                    row.OccurredAt,
                    row.ActorType,
                    row.ActorId,
                    row.Action,
                    row.TargetType,
                    row.TargetId,
                    row.Outcome,
                    row.CorrelationId,
                    row.DetailsJson))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        bool hasMore = items.Length > query.PageSize;
        AddAudit(
            context,
            subjectId,
            "audit.read",
            Guid.Empty,
            "Succeeded",
            Guid.NewGuid(),
            "audit.page_read",
            now,
            targetType: "AuditLog");
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new AuthorizedAuditPage(
            true,
            items.Take(query.PageSize).ToArray(),
            hasMore ? query.Offset + query.PageSize : null,
            snapshotAt);
    }

    public async ValueTask<IReadOnlyList<AuthorizedInstance>> GetAuthorizedInstancesAsync(
        string subjectId,
        string permissionCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        Guid? userId = await context.Users
            .AsNoTracking()
            .Where(row => row.SubjectId == subjectId && row.State == "Active")
            .Select(row => (Guid?)row.UserId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (userId is null)
        {
            return [];
        }

        AuthorizationScope[] scopes = await GetScopesAsync(
            context,
            userId.Value,
            permissionCode,
            now,
            cancellationToken).ConfigureAwait(false);
        if (scopes.Length == 0)
        {
            return [];
        }

        bool global = scopes.Any(scope => scope.ScopeType == "Global" && scope.ScopeValue == "*");
        string[] environments = scopes
            .Where(scope => scope.ScopeType == "Environment")
            .Select(scope => scope.ScopeValue)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Guid[] instanceIds = scopes
            .Where(scope => scope.ScopeType == "Instance" && Guid.TryParse(scope.ScopeValue, out _))
            .Select(scope => Guid.Parse(scope.ScopeValue))
            .Distinct()
            .ToArray();

        return await context.Instances
            .AsNoTracking()
            .Where(row => row.ArchivedAt == null &&
                (global || environments.Contains(row.Environment) || instanceIds.Contains(row.InstanceId)))
            .OrderBy(row => row.Environment)
            .ThenBy(row => row.DisplayName)
            .Select(row => new AuthorizedInstance(
                row.InstanceId,
                row.DisplayName,
                row.ProviderType,
                row.Environment,
                row.AssignedAgentId,
                row.Enabled))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<DesktopFleetApiSnapshot> GetAuthorizedDesktopFleetSnapshotAsync(
        string subjectId,
        string permissionCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        Guid? userId = await context.Users
            .AsNoTracking()
            .Where(row => row.SubjectId == subjectId && row.State == "Active")
            .Select(row => (Guid?)row.UserId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (userId is null)
        {
            return EmptyDesktopFleetSnapshot(now);
        }

        AuthorizationScope[] scopes = await GetScopesAsync(
            context,
            userId.Value,
            permissionCode,
            now,
            cancellationToken).ConfigureAwait(false);
        if (scopes.Length == 0)
        {
            return EmptyDesktopFleetSnapshot(now);
        }

        bool global = scopes.Any(scope => scope.ScopeType == "Global" && scope.ScopeValue == "*");
        string[] environments = scopes
            .Where(scope => scope.ScopeType == "Environment")
            .Select(scope => scope.ScopeValue)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Guid[] instanceIds = scopes
            .Where(scope => scope.ScopeType == "Instance" && Guid.TryParse(scope.ScopeValue, out _))
            .Select(scope => Guid.Parse(scope.ScopeValue))
            .Distinct()
            .ToArray();

        DesktopFleetProjectionRow[] rows = await (
                from instance in context.Instances.AsNoTracking()
                join state in context.InstanceObservationStates.AsNoTracking()
                    on instance.InstanceId equals state.InstanceId
                join sample in context.HealthSamples.AsNoTracking()
                    on state.ObservationId equals sample.ObservationId
                join agent in context.Agents.AsNoTracking()
                    on state.AgentId equals agent.AgentId
                where instance.ArchivedAt == null &&
                    instance.AssignedAgentId != null &&
                    instance.AssignedAgentId == state.AgentId &&
                    (global || environments.Contains(instance.Environment) || instanceIds.Contains(instance.InstanceId))
                orderby instance.Environment, instance.DisplayName, instance.InstanceId
                select new DesktopFleetProjectionRow(
                    instance.InstanceId,
                    instance.DisplayName,
                    instance.ProviderType,
                    instance.Environment,
                    instance.Enabled,
                    agent.DisplayName,
                    state.AgentId,
                    state.ObservationId,
                    state.LastProcessedSequence,
                    state.Status,
                    state.ObservedAt,
                    state.ReceivedAt,
                    sample.InstanceId,
                    sample.AgentId,
                    sample.Sequence,
                    sample.ProviderType,
                    sample.Status,
                    sample.EvidenceLevel,
                    sample.ObservedAt,
                    sample.ReceivedAt,
                    sample.DurationMilliseconds))
            .Take(DesktopFleetReconciliationCoordinator.MaximumSnapshotItems + 1)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        if (rows.Length > DesktopFleetReconciliationCoordinator.MaximumSnapshotItems)
        {
            throw new InvalidOperationException("desktop_fleet.projection_limit_exceeded");
        }

        DesktopFleetApiItem[] items = rows.Select(MapDesktopFleetItem).ToArray();
        return new DesktopFleetApiSnapshot(
            DesktopFleetApiContract.CurrentSchemaVersion,
            now.ToUniversalTime(),
            items);
    }

    public async ValueTask<CommandCreationResult> CreateCommandAsync(
        string subjectId,
        Guid instanceId,
        string permissionCode,
        ValidatedCommandRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        PlatformUserRow? user = await context.Users
            .SingleOrDefaultAsync(
                row => row.SubjectId == subjectId && row.State == "Active",
                cancellationToken)
            .ConfigureAwait(false);
        DatabaseInstanceRow? instance = await context.Instances
            .AsNoTracking()
            .SingleOrDefaultAsync(
                row => row.InstanceId == instanceId && row.Enabled && row.ArchivedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (user is null || instance?.AssignedAgentId is null)
        {
            return await DenyAsync(
                context,
                transaction,
                subjectId,
                instanceId,
                "authorization.denied",
                now,
                cancellationToken).ConfigureAwait(false);
        }

        AuthorizationScope[] scopes = await GetScopesAsync(
            context,
            user.UserId,
            permissionCode,
            now,
            cancellationToken).ConfigureAwait(false);
        AuthorizationScope? grantedScope = scopes.FirstOrDefault(scope => ScopeMatches(scope, instance));
        if (grantedScope is null)
        {
            return await DenyAsync(
                context,
                transaction,
                subjectId,
                instanceId,
                "authorization.denied",
                now,
                cancellationToken).ConfigureAwait(false);
        }

        AdministrativeCommandRow? existing = await context.AdministrativeCommands
            .AsNoTracking()
            .SingleOrDefaultAsync(row => row.IdempotencyKey == request.IdempotencyKey, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            CommandCreationDisposition disposition = Matches(existing, user.UserId, instanceId, request)
                ? CommandCreationDisposition.Duplicate
                : CommandCreationDisposition.IdempotencyConflict;
            AddAudit(
                context,
                subjectId,
                "command.create",
                instanceId,
                disposition == CommandCreationDisposition.Duplicate ? "Succeeded" : "Failed",
                existing.CommandId,
                disposition == CommandCreationDisposition.Duplicate
                    ? "command.duplicate"
                    : "command.idempotency_conflict",
                now);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CommandCreationResult(
                disposition,
                disposition == CommandCreationDisposition.Duplicate ? ToReceipt(existing) : null,
                disposition == CommandCreationDisposition.Duplicate ? null : "command.idempotency_conflict");
        }

        bool capabilityAvailable = await (
            from agent in context.Agents.AsNoTracking()
            join capability in context.AgentCapabilities.AsNoTracking() on agent.AgentId equals capability.AgentId
            where agent.AgentId == instance.AssignedAgentId.Value &&
                agent.State == "Active" && agent.RevokedAt == null &&
                agent.AgentVersion == request.ExpectedAgentVersion &&
                capability.CapabilityId == request.CapabilityId &&
                capability.ProviderType == instance.ProviderType &&
                capability.ProviderVersion == request.ExpectedProviderVersion &&
                capability.State == "Supported"
            select capability.AgentCapabilityId)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!capabilityAvailable)
        {
            AddAudit(
                context,
                subjectId,
                "command.create",
                instanceId,
                "Denied",
                Guid.NewGuid(),
                "command.capability_unavailable",
                now);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CommandCreationResult(
                CommandCreationDisposition.CapabilityUnavailable,
                null,
                "command.capability_unavailable");
        }

        AdministrativeCommandRow command = new()
        {
            CommandId = Guid.NewGuid(),
            IdempotencyKey = request.IdempotencyKey,
            InstanceId = instanceId,
            AssignedAgentId = instance.AssignedAgentId.Value,
            CapabilityId = request.CapabilityId,
            TypedParametersJson = request.TypedParametersJson,
            RequestedByUserId = user.UserId,
            RequestedAt = now,
            Reason = request.Reason,
            ExpiresAt = request.ExpiresAt,
            AuthorizationSnapshotReference = $"role-assignment:{grantedScope.RoleAssignmentId:D}",
            ExpectedAgentVersion = request.ExpectedAgentVersion,
            ExpectedProviderVersion = request.ExpectedProviderVersion,
            State = "Pending",
            ConcurrencyToken = Guid.NewGuid(),
        };
        context.AdministrativeCommands.Add(command);
        AddAudit(
            context,
            subjectId,
            "command.create",
            instanceId,
            "Succeeded",
            command.CommandId,
            "command.created_pending",
            now);

        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CommandCreationResult(
                CommandCreationDisposition.Created,
                ToReceipt(command));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            await using ServerDbContext verification = await contextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);
            AdministrativeCommandRow? concurrent = await verification.AdministrativeCommands
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    row => row.IdempotencyKey == request.IdempotencyKey,
                    cancellationToken)
                .ConfigureAwait(false);
            if (concurrent is null)
            {
                return new CommandCreationResult(
                    CommandCreationDisposition.Invalid,
                    null,
                    "command.persistence_failed");
            }

            bool duplicate = Matches(concurrent, user.UserId, instanceId, request);
            AddAudit(
                verification,
                subjectId,
                "command.create",
                instanceId,
                duplicate ? "Succeeded" : "Failed",
                concurrent.CommandId,
                duplicate ? "command.duplicate" : "command.idempotency_conflict",
                now);
            await verification.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new CommandCreationResult(
                duplicate ? CommandCreationDisposition.Duplicate : CommandCreationDisposition.IdempotencyConflict,
                duplicate ? ToReceipt(concurrent) : null,
                duplicate ? null : "command.idempotency_conflict");
        }
    }

    private static async ValueTask<AuthorizationScope[]> GetScopesAsync(
        ServerDbContext context,
        Guid userId,
        string permissionCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        AuthorizationScope[] assigned = await (
            from assignment in context.RoleAssignments.AsNoTracking()
            join rolePermission in context.RolePermissions.AsNoTracking()
                on assignment.RoleId equals rolePermission.RoleId
            join permission in context.Permissions.AsNoTracking()
                on rolePermission.PermissionId equals permission.PermissionId
            where assignment.UserId == userId && permission.Code == permissionCode
            select new AuthorizationScope(
                assignment.RoleAssignmentId,
                assignment.ScopeType,
                assignment.ScopeValue,
                assignment.ExpiresAt))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return assigned
            .Where(scope => scope.ExpiresAt is null || scope.ExpiresAt > now)
            .ToArray();
    }

    private static bool ScopeMatches(AuthorizationScope scope, DatabaseInstanceRow instance) =>
        (scope.ScopeType == "Global" && scope.ScopeValue == "*") ||
        (scope.ScopeType == "Environment" && scope.ScopeValue == instance.Environment) ||
        (scope.ScopeType == "Instance" &&
            Guid.TryParse(scope.ScopeValue, out Guid scopedInstanceId) &&
            scopedInstanceId == instance.InstanceId);

    private static bool Matches(
        AdministrativeCommandRow existing,
        Guid userId,
        Guid instanceId,
        ValidatedCommandRequest request) =>
        existing.RequestedByUserId == userId &&
        existing.InstanceId == instanceId &&
        existing.CapabilityId == request.CapabilityId &&
        existing.TypedParametersJson == request.TypedParametersJson &&
        existing.Reason == request.Reason &&
        existing.ExpiresAt == request.ExpiresAt &&
        existing.ExpectedAgentVersion == request.ExpectedAgentVersion &&
        existing.ExpectedProviderVersion == request.ExpectedProviderVersion;

    /// <summary>Creates an authorised empty read result without inventing observation evidence.</summary>
    private static DesktopFleetApiSnapshot EmptyDesktopFleetSnapshot(DateTimeOffset now) =>
        new(DesktopFleetApiContract.CurrentSchemaVersion, now.ToUniversalTime(), []);

    /// <summary>Maps only a fully coherent latest-observation row and rejects inconsistent persistence evidence.</summary>
    private static DesktopFleetApiItem MapDesktopFleetItem(DesktopFleetProjectionRow row)
    {
        if (row.InstanceId != row.SampleInstanceId ||
            row.AgentId != row.SampleAgentId ||
            row.ObservationId == Guid.Empty ||
            row.Sequence != row.SampleSequence ||
            !string.Equals(row.ProviderType, row.SampleProviderType, StringComparison.Ordinal) ||
            !string.Equals(row.Status, row.SampleStatus, StringComparison.Ordinal) ||
            row.ObservedAt != row.SampleObservedAt ||
            row.ReceivedAt != row.SampleReceivedAt ||
            row.ObservedAt > row.ReceivedAt ||
            row.DurationMilliseconds is < 0 or > 86_400_000 ||
            !TryMapStatus(row.Status, out string status) ||
            !TryMapEvidenceLevel(row.EvidenceLevel, out string evidenceLabel))
        {
            throw new InvalidOperationException("desktop_fleet.projection_evidence_invalid");
        }

        return new DesktopFleetApiItem(
            row.InstanceId,
            row.DisplayName,
            row.ProviderType,
            evidenceLabel,
            row.Environment,
            row.AgentDisplayName,
            status,
            row.ObservedAt.ToUniversalTime(),
            row.ReceivedAt.ToUniversalTime(),
            row.DurationMilliseconds,
            row.Enabled);
    }

    /// <summary>Maps the canonical stored domain status to the stable Desktop Fleet wire identifier.</summary>
    private static bool TryMapStatus(string value, out string status)
    {
        status = value switch
        {
            nameof(HealthStatus.Healthy) => "healthy",
            nameof(HealthStatus.Degraded) => "degraded",
            nameof(HealthStatus.Unavailable) => "unavailable",
            nameof(HealthStatus.AuthFailed) => "authFailed",
            nameof(HealthStatus.Timeout) => "timeout",
            nameof(HealthStatus.Maintenance) => "maintenance",
            nameof(HealthStatus.Unknown) => "unknown",
            _ => string.Empty,
        };
        return status.Length != 0;
    }

    /// <summary>Maps stored observation quality to a factual label without claiming provider support or homologation.</summary>
    private static bool TryMapEvidenceLevel(string value, out string label)
    {
        label = value switch
        {
            nameof(EvidenceLevel.ProviderAuthenticated) => "Provider-authenticated evidence",
            nameof(EvidenceLevel.ProviderReadiness) => "Provider-readiness evidence",
            nameof(EvidenceLevel.TransportOnly) => "Transport-only evidence",
            nameof(EvidenceLevel.Synthetic) => "Synthetic evidence",
            nameof(EvidenceLevel.Unknown) => "Unknown evidence",
            _ => string.Empty,
        };
        return label.Length != 0;
    }

    private static async ValueTask<CommandCreationResult> DenyAsync(
        ServerDbContext context,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        string subjectId,
        Guid instanceId,
        string errorCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        AddAudit(
            context,
            subjectId,
            "command.create",
            instanceId,
            "Denied",
            Guid.NewGuid(),
            errorCode,
            now);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new CommandCreationResult(CommandCreationDisposition.Denied, null, errorCode);
    }

    private static void AddAudit(
        ServerDbContext context,
        string subjectId,
        string action,
        Guid instanceId,
        string outcome,
        Guid correlationId,
        string code,
        DateTimeOffset now,
        string targetType = "DatabaseInstance") =>
        context.AuditEntries.Add(new AuditEntryRow
        {
            AuditEntryId = Guid.NewGuid(),
            OccurredAt = now,
            ActorType = "Human",
            ActorId = subjectId,
            Action = action,
            TargetType = targetType,
            TargetId = instanceId.ToString("D"),
            Outcome = outcome,
            CorrelationId = correlationId,
            DetailsJson = JsonSerializer.Serialize(new { code }, SerializerOptions),
        });

    private static AdministrativeCommandReceipt ToReceipt(AdministrativeCommandRow command) =>
        new(
            command.CommandId,
            command.InstanceId,
            command.CapabilityId,
            command.State,
            command.RequestedAt,
            command.ExpiresAt);

    private static AuditEntryView ToAuditView(AuditEntryRow row) =>
        new(
            row.AuditEntryId,
            row.OccurredAt,
            row.ActorType,
            row.ActorId,
            row.Action,
            row.TargetType,
            row.TargetId,
            row.Outcome,
            row.CorrelationId,
            row.DetailsJson);

    private sealed record AuthorizationScope(
        Guid RoleAssignmentId,
        string ScopeType,
        string ScopeValue,
        DateTimeOffset? ExpiresAt);

    /// <summary>Holds the exact database values required to prove one coherent Desktop Fleet item.</summary>
    private sealed record DesktopFleetProjectionRow(
        Guid InstanceId,
        string DisplayName,
        string ProviderType,
        string Environment,
        bool Enabled,
        string AgentDisplayName,
        Guid AgentId,
        Guid ObservationId,
        long Sequence,
        string Status,
        DateTimeOffset ObservedAt,
        DateTimeOffset ReceivedAt,
        Guid SampleInstanceId,
        Guid SampleAgentId,
        long SampleSequence,
        string SampleProviderType,
        string SampleStatus,
        string EvidenceLevel,
        DateTimeOffset SampleObservedAt,
        DateTimeOffset SampleReceivedAt,
        long DurationMilliseconds);
}
