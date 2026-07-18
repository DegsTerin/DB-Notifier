// Module purpose: Persists Agent-side identity metadata, exact heartbeat replay and atomic last-known-valid assignments in local SQLite.
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBNotifier.Application.AgentFleet;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Agent.Sqlite;

/// <summary>
/// Implements the Agent Fleet local-store port without persisting tokens, private keys, certificate bodies,
/// resolved credentials or provider results.
/// </summary>
/// <param name="contextFactory">Factory for short-lived Agent SQLite contexts.</param>
/// <param name="faultInjector">Optional deterministic test-only fault injector.</param>
public sealed class AgentFleetLocalStore(
    IDbContextFactory<AgentDbContext> contextFactory,
    IAgentFleetLocalStoreFaultInjector? faultInjector = null) : IAgentFleetLocalStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 32,
    };

    /// <inheritdoc />
    public async ValueTask<AgentLocalRegistration?> GetRegistrationAsync(CancellationToken cancellationToken)
    {
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        AgentRegistrationRow[] rows = await context.Registrations.AsNoTracking()
            .OrderBy(row => row.AgentId)
            .Take(2)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
        if (rows.Length > 1)
        {
            throw new InvalidOperationException("agent.local_identity_conflict");
        }

        return rows.Length == 0 ? null : ToRegistration(rows[0]);
    }

    /// <inheritdoc />
    public async ValueTask SaveEnrollmentAsync(
        AgentLocalRegistration registration,
        CancellationToken cancellationToken)
    {
        ValidateRegistration(registration);
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        if (await context.Registrations.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("agent.local_identity_conflict");
        }

        DateTimeOffset now = registration.EnrolledAt;
        context.Registrations.Add(new AgentRegistrationRow
        {
            AgentId = registration.AgentId,
            InstallationId = registration.InstallationId,
            Environment = registration.Environment,
            IdentityCertificateReference = registration.IdentityReference,
            CertificateThumbprint = registration.CertificateThumbprint,
            CertificateNotAfter = registration.CertificateNotAfter,
            IdentityState = registration.State.ToString(),
            ActiveConfigurationVersion = null,
            CreatedAt = now,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid(),
        });
        context.AgentFleetStates.Add(new AgentFleetStateRow
        {
            AgentId = registration.AgentId,
            NextHeartbeatSequence = 1,
            NextOperationFence = 1,
            ConcurrencyToken = Guid.NewGuid(),
        });
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<AgentFleetOperationLease?> TryAcquireOperationLeaseAsync(
        Guid agentId,
        string ownerId,
        AgentFleetOperationKind kind,
        DateTimeOffset now,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        ValidateLeaseRequest(agentId, ownerId, kind, now, duration);
        await InjectAsync(AgentFleetLocalStoreFaultPoint.BeforeLeaseAcquire, cancellationToken).ConfigureAwait(false);
        DateTimeOffset expiresAt = now + duration;
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        int updated;
        try
        {
            updated = await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE agent_fleet_state
                SET operation_lease_owner = {ownerId},
                    operation_lease_kind = {kind.ToString()},
                    operation_lease_fence = next_operation_fence,
                    operation_lease_expires_at = {expiresAt},
                    next_operation_fence = next_operation_fence + 1,
                    concurrency_token = {Guid.NewGuid()}
                WHERE agent_id = {agentId}
                  AND next_operation_fence < {long.MaxValue}
                  AND (operation_lease_owner IS NULL OR operation_lease_expires_at <= {now});
                """,
                cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode is 5 or 6)
        {
            // SQLite BUSY/LOCKED is a bounded acquisition refusal, never permission to run without a fence.
            return null;
        }
        if (updated == 0)
        {
            return null;
        }

        var lease = await context.AgentFleetStates.AsNoTracking()
            .Where(row => row.AgentId == agentId)
            .Select(row => new
            {
                row.OperationLeaseOwner,
                row.OperationLeaseKind,
                row.OperationLeaseFence,
                row.OperationLeaseExpiresAt,
            })
            .SingleAsync(cancellationToken).ConfigureAwait(false);
        if (!string.Equals(lease.OperationLeaseOwner, ownerId, StringComparison.Ordinal) ||
            !string.Equals(lease.OperationLeaseKind, kind.ToString(), StringComparison.Ordinal) ||
            lease.OperationLeaseExpiresAt != expiresAt)
        {
            throw new InvalidOperationException("agent_fleet.lease_invalid");
        }
        await InjectAsync(AgentFleetLocalStoreFaultPoint.AfterLeaseAcquire, cancellationToken).ConfigureAwait(false);
        return new AgentFleetOperationLease(
            agentId,
            ownerId,
            kind,
            lease.OperationLeaseFence ?? throw new InvalidOperationException("agent_fleet.lease_invalid"),
            expiresAt);
    }

    /// <inheritdoc />
    public async ValueTask ReleaseOperationLeaseAsync(
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken)
    {
        ValidateLease(lease);
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE agent_fleet_state
            SET operation_lease_owner = NULL,
                operation_lease_kind = NULL,
                operation_lease_fence = NULL,
                operation_lease_expires_at = NULL,
                concurrency_token = {Guid.NewGuid()}
            WHERE agent_id = {lease.AgentId}
              AND operation_lease_owner = {lease.OwnerId}
              AND operation_lease_kind = {lease.Kind.ToString()}
              AND operation_lease_fence = {lease.FenceToken};
            """,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<AgentHeartbeatQueueEvidence> GetQueueEvidenceAsync(CancellationToken cancellationToken)
    {
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        long count = await context.OutboxMessages.AsNoTracking()
            .LongCountAsync(row => row.AcknowledgedAt == null, cancellationToken).ConfigureAwait(false);
        DateTimeOffset? oldest = count == 0
            ? null
            : await context.OutboxMessages.AsNoTracking()
                .Where(row => row.AcknowledgedAt == null)
                .MinAsync(row => (DateTimeOffset?)row.OccurredAt, cancellationToken).ConfigureAwait(false);
        if (count > 1_000_000)
        {
            throw new InvalidOperationException("heartbeat.queue_limit_exceeded");
        }

        return new AgentHeartbeatQueueEvidence(count, oldest);
    }

    /// <inheritdoc />
    public async ValueTask<PendingAgentHeartbeat> GetOrCreatePendingHeartbeatAsync(
        AgentLocalRegistration registration,
        string agentVersion,
        AgentHeartbeatQueueEvidence queueEvidence,
        DateTimeOffset now,
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken)
    {
        ValidateRegistration(registration);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentVersion);
        if (queueEvidence.Depth < 0 ||
            (queueEvidence.Depth == 0) != (queueEvidence.OldestQueuedAt is null) ||
            now == default || now.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Heartbeat evidence is outside policy.", nameof(queueEvidence));
        }

        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        AgentFleetStateRow state = await context.AgentFleetStates
            .SingleAsync(row => row.AgentId == registration.AgentId, cancellationToken).ConfigureAwait(false);
        EnsureLease(state, lease, AgentFleetOperationKind.Heartbeat, now);
        if (state.PendingHeartbeatPayloadJson is not null)
        {
            AgentHeartbeatRequest pending = JsonSerializer.Deserialize<AgentHeartbeatRequest>(
                state.PendingHeartbeatPayloadJson,
                JsonOptions) ?? throw new InvalidOperationException("heartbeat.pending_invalid");
            if (state.PendingHeartbeatMessageId != pending.MessageId ||
                state.PendingHeartbeatSequence != pending.Sequence || pending.AgentId != registration.AgentId)
            {
                throw new InvalidOperationException("heartbeat.pending_conflict");
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new PendingAgentHeartbeat(pending, false);
        }

        if (state.NextHeartbeatSequence < 1)
        {
            throw new InvalidOperationException("heartbeat.sequence_invalid");
        }

        AgentHeartbeatRequest request = new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            registration.AgentId,
            state.NextHeartbeatSequence,
            now,
            now,
            agentVersion,
            AgentFleetProtocol.CurrentProtocolVersion,
            AgentFleetProtocol.CurrentProtocolVersion,
            queueEvidence.Depth,
            queueEvidence.OldestQueuedAt,
            now);
        state.PendingHeartbeatMessageId = request.MessageId;
        state.PendingHeartbeatSequence = request.Sequence;
        state.PendingHeartbeatPayloadJson = JsonSerializer.Serialize(request, JsonOptions);
        state.LastAttemptAt = now;
        state.ConcurrencyToken = Guid.NewGuid();
        await InjectAsync(AgentFleetLocalStoreFaultPoint.BeforeHeartbeatPendingCommit, cancellationToken)
            .ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        await InjectAsync(AgentFleetLocalStoreFaultPoint.AfterHeartbeatPendingCommit, cancellationToken)
            .ConfigureAwait(false);
        return new PendingAgentHeartbeat(request, true);
    }

    /// <inheritdoc />
    public async ValueTask AcknowledgeHeartbeatAsync(
        AgentHeartbeatRequest request,
        AgentHeartbeatOutcome outcome,
        DateTimeOffset acknowledgedAt,
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(outcome);
        if (outcome.Disposition is not (AgentHeartbeatDisposition.Accepted or
                AgentHeartbeatDisposition.Duplicate or AgentHeartbeatDisposition.AcceptedWithGap) ||
            outcome.AcceptedAt is null || outcome.HighestAcceptedSequence != request.Sequence ||
            acknowledgedAt == default || acknowledgedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Heartbeat outcome cannot acknowledge the pending envelope.", nameof(outcome));
        }

        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        AgentFleetStateRow state = await context.AgentFleetStates
            .SingleAsync(row => row.AgentId == request.AgentId, cancellationToken).ConfigureAwait(false);
        EnsureLease(state, lease, AgentFleetOperationKind.Heartbeat, acknowledgedAt);
        AgentRegistrationRow registration = await context.Registrations
            .SingleAsync(row => row.AgentId == request.AgentId, cancellationToken).ConfigureAwait(false);
        if (state.PendingHeartbeatMessageId != request.MessageId ||
            state.PendingHeartbeatSequence != request.Sequence ||
            !string.Equals(
                state.PendingHeartbeatPayloadJson,
                JsonSerializer.Serialize(request, JsonOptions),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("heartbeat.pending_conflict");
        }

        state.NextHeartbeatSequence = checked(outcome.HighestAcceptedSequence + 1);
        state.PendingHeartbeatMessageId = null;
        state.PendingHeartbeatSequence = null;
        state.PendingHeartbeatPayloadJson = null;
        state.LastHeartbeatAcceptedAt = outcome.AcceptedAt;
        state.LastErrorCode = null;
        state.ConcurrencyToken = Guid.NewGuid();
        registration.IdentityState = AgentLocalIdentityState.Active.ToString();
        registration.UpdatedAt = outcome.AcceptedAt.Value;
        registration.ConcurrencyToken = Guid.NewGuid();
        await InjectAsync(AgentFleetLocalStoreFaultPoint.BeforeHeartbeatAcknowledgementCommit, cancellationToken)
            .ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        await InjectAsync(AgentFleetLocalStoreFaultPoint.AfterHeartbeatAcknowledgementCommit, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask SetIdentityStateAsync(
        Guid agentId,
        AgentLocalIdentityState state,
        string errorCode,
        DateTimeOffset now,
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken)
    {
        ValidateStateChange(agentId, state, errorCode, now);
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        AgentRegistrationRow registration = await context.Registrations
            .SingleAsync(row => row.AgentId == agentId, cancellationToken).ConfigureAwait(false);
        AgentFleetStateRow fleet = await context.AgentFleetStates
            .SingleAsync(row => row.AgentId == agentId, cancellationToken).ConfigureAwait(false);
        EnsureLease(fleet, lease, lease.Kind, now);
        registration.IdentityState = state.ToString();
        registration.UpdatedAt = now;
        registration.ConcurrencyToken = Guid.NewGuid();
        fleet.LastAttemptAt = now;
        fleet.LastErrorCode = errorCode;
        fleet.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<AgentAssignmentLocalState> GetAssignmentStateAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var result = await context.Registrations.AsNoTracking()
            .Where(row => row.AgentId == agentId)
            .Join(
                context.AgentFleetStates.AsNoTracking(),
                registration => registration.AgentId,
                fleet => fleet.AgentId,
                (registration, fleet) => new
                {
                    registration.ActiveConfigurationVersion,
                    fleet.AssignmentEntityTag,
                    fleet.AssignmentGeneratedAt,
                    fleet.AssignmentLastSucceededAt,
                    fleet.LastErrorCode,
                })
            .SingleAsync(cancellationToken).ConfigureAwait(false);
        return new AgentAssignmentLocalState(
            result.ActiveConfigurationVersion,
            result.AssignmentEntityTag,
            result.AssignmentGeneratedAt,
            result.AssignmentLastSucceededAt,
            result.LastErrorCode);
    }

    /// <inheritdoc />
    public async ValueTask ApplyAssignmentsAsync(
        AgentAssignmentSnapshot snapshot,
        string entityTag,
        DateTimeOffset appliedAt,
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (appliedAt == default || appliedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Assignment snapshot is outside local-store policy.", nameof(snapshot));
        }

        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        AgentRegistrationRow registration = await context.Registrations
            .SingleAsync(row => row.AgentId == snapshot.AgentId, cancellationToken).ConfigureAwait(false);
        AgentFleetStateRow fleet = await context.AgentFleetStates
            .SingleAsync(row => row.AgentId == snapshot.AgentId, cancellationToken).ConfigureAwait(false);
        EnsureLease(fleet, lease, AgentFleetOperationKind.AssignmentReconciliation, appliedAt);
        if (!AgentAssignmentVersion.TryValidate(
                snapshot,
                snapshot.AgentId,
                registration.Environment,
                entityTag,
                out _))
        {
            throw new InvalidOperationException("assignments.snapshot_invalid");
        }

        await context.InstanceAssignments.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        context.InstanceAssignments.AddRange(snapshot.Assignments.Select(assignment => new AgentInstanceAssignmentRow
        {
            InstanceId = assignment.InstanceId,
            DisplayName = assignment.DisplayName,
            ProviderType = assignment.ProviderType,
            EndpointJson = assignment.Endpoint.GetRawText(),
            MonitoringCredentialReference = assignment.MonitoringCredentialReference,
            AdministrativeCredentialReference = null,
            TagsJson = assignment.Tags.GetRawText(),
            IntervalSeconds = assignment.IntervalSeconds,
            TimeoutSeconds = assignment.TimeoutSeconds,
            RetryCount = assignment.RetryCount,
            Enabled = true,
            PolicyVersion = snapshot.Version,
            UpdatedAt = assignment.UpdatedAt,
            ConcurrencyToken = Guid.NewGuid(),
        }));
        registration.ActiveConfigurationVersion = snapshot.Version;
        registration.IdentityState = AgentLocalIdentityState.Active.ToString();
        registration.UpdatedAt = appliedAt;
        registration.ConcurrencyToken = Guid.NewGuid();
        fleet.AssignmentEntityTag = entityTag;
        fleet.AssignmentGeneratedAt = snapshot.GeneratedAt;
        fleet.AssignmentLastSucceededAt = appliedAt;
        fleet.LastAttemptAt = appliedAt;
        fleet.LastErrorCode = null;
        fleet.ConcurrencyToken = Guid.NewGuid();
        await InjectAsync(AgentFleetLocalStoreFaultPoint.BeforeAssignmentCommit, cancellationToken)
            .ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        await InjectAsync(AgentFleetLocalStoreFaultPoint.AfterAssignmentCommit, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask RecordAssignmentsNotModifiedAsync(
        Guid agentId,
        string version,
        string entityTag,
        DateTimeOffset checkedAt,
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken)
    {
        if (agentId == Guid.Empty || !AgentAssignmentVersion.IsUpperHexDigest(version) ||
            entityTag != $"\"{version}\"" || checkedAt == default || checkedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Not-modified assignment evidence is outside policy.");
        }

        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        AgentRegistrationRow registration = await context.Registrations
            .SingleAsync(row => row.AgentId == agentId, cancellationToken).ConfigureAwait(false);
        AgentFleetStateRow fleet = await context.AgentFleetStates
            .SingleAsync(row => row.AgentId == agentId, cancellationToken).ConfigureAwait(false);
        EnsureLease(fleet, lease, AgentFleetOperationKind.AssignmentReconciliation, checkedAt);
        if (!string.Equals(registration.ActiveConfigurationVersion, version, StringComparison.Ordinal) ||
            !string.Equals(fleet.AssignmentEntityTag, entityTag, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("assignments.not_modified_conflict");
        }

        registration.IdentityState = AgentLocalIdentityState.Active.ToString();
        registration.UpdatedAt = checkedAt;
        registration.ConcurrencyToken = Guid.NewGuid();
        fleet.AssignmentLastSucceededAt = checkedAt;
        fleet.LastAttemptAt = checkedAt;
        fleet.LastErrorCode = null;
        fleet.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask RecordAssignmentFailureAsync(
        Guid agentId,
        AgentLocalIdentityState state,
        string errorCode,
        DateTimeOffset failedAt,
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken) =>
        SetIdentityStateAsync(agentId, state, errorCode, failedAt, lease, cancellationToken);

    /// <summary>Invokes the optional deterministic test fault boundary without affecting ordinary composition.</summary>
    /// <param name="point">Exact persistence boundary.</param>
    /// <param name="cancellationToken">Cancellation propagated to the fixture hook.</param>
    /// <returns>Completed work when no injector is supplied.</returns>
    private ValueTask InjectAsync(AgentFleetLocalStoreFaultPoint point, CancellationToken cancellationToken) =>
        faultInjector?.InjectAsync(point, cancellationToken) ?? ValueTask.CompletedTask;

    /// <summary>Refuses a mutation unless the row still carries the caller's exact unexpired fence.</summary>
    /// <param name="state">Tracked Agent Fleet state row.</param>
    /// <param name="lease">Caller-provided lease.</param>
    /// <param name="expectedKind">Operation required by the mutation.</param>
    /// <param name="now">Controlled UTC mutation instant.</param>
    private static void EnsureLease(
        AgentFleetStateRow state,
        AgentFleetOperationLease lease,
        AgentFleetOperationKind expectedKind,
        DateTimeOffset now)
    {
        ValidateLease(lease);
        if (state.AgentId != lease.AgentId || lease.Kind != expectedKind ||
            !string.Equals(state.OperationLeaseOwner, lease.OwnerId, StringComparison.Ordinal) ||
            !string.Equals(state.OperationLeaseKind, lease.Kind.ToString(), StringComparison.Ordinal) ||
            state.OperationLeaseFence != lease.FenceToken || state.OperationLeaseExpiresAt != lease.ExpiresAt ||
            lease.ExpiresAt <= now)
        {
            throw new InvalidOperationException("agent_fleet.lease_lost");
        }
    }

    /// <summary>Validates bounded lease acquisition inputs before SQLite access.</summary>
    /// <param name="agentId">Exact Agent identifier.</param>
    /// <param name="ownerId">Bounded sandbox owner.</param>
    /// <param name="kind">Protected operation.</param>
    /// <param name="now">Controlled UTC acquisition instant.</param>
    /// <param name="duration">Bounded lease duration.</param>
    private static void ValidateLeaseRequest(
        Guid agentId,
        string ownerId,
        AgentFleetOperationKind kind,
        DateTimeOffset now,
        TimeSpan duration)
    {
        if (agentId == Guid.Empty || string.IsNullOrWhiteSpace(ownerId) || ownerId.Length is < 8 or > 160 ||
            ownerId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or ':' or '-')) ||
            !Enum.IsDefined(kind) || now == default || now.Offset != TimeSpan.Zero ||
            duration < TimeSpan.FromSeconds(1) || duration > TimeSpan.FromMinutes(30))
        {
            throw new ArgumentException("Agent Fleet operation lease request is outside policy.");
        }
    }

    /// <summary>Validates a caller-provided lease before it influences a mutation or release.</summary>
    /// <param name="lease">Lease to validate.</param>
    private static void ValidateLease(AgentFleetOperationLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        if (lease.AgentId == Guid.Empty || string.IsNullOrWhiteSpace(lease.OwnerId) || lease.FenceToken < 1 ||
            !Enum.IsDefined(lease.Kind) || lease.ExpiresAt == default || lease.ExpiresAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Agent Fleet operation lease is outside policy.", nameof(lease));
        }
    }

    private static AgentLocalRegistration ToRegistration(AgentRegistrationRow row)
    {
        if (!Enum.TryParse(row.IdentityState, ignoreCase: false, out AgentLocalIdentityState state))
        {
            throw new InvalidOperationException("agent.identity_state_invalid");
        }

        return new AgentLocalRegistration(
            row.AgentId,
            row.InstallationId,
            row.Environment,
            row.IdentityCertificateReference,
            row.CertificateThumbprint,
            row.CertificateNotAfter,
            row.CreatedAt,
            state,
            row.ActiveConfigurationVersion);
    }

    private static void ValidateRegistration(AgentLocalRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        if (registration.AgentId == Guid.Empty || string.IsNullOrWhiteSpace(registration.InstallationId) ||
            string.IsNullOrWhiteSpace(registration.Environment) || string.IsNullOrWhiteSpace(registration.IdentityReference) ||
            string.IsNullOrWhiteSpace(registration.CertificateThumbprint) ||
            registration.CertificateNotAfter == default || registration.CertificateNotAfter.Offset != TimeSpan.Zero ||
            registration.EnrolledAt == default || registration.EnrolledAt.Offset != TimeSpan.Zero ||
            registration.CertificateNotAfter <= registration.EnrolledAt)
        {
            throw new ArgumentException("Agent registration is outside policy.", nameof(registration));
        }
    }

    private static void ValidateStateChange(
        Guid agentId,
        AgentLocalIdentityState state,
        string errorCode,
        DateTimeOffset now)
    {
        if (agentId == Guid.Empty || state == AgentLocalIdentityState.NotEnrolled ||
            string.IsNullOrWhiteSpace(errorCode) || errorCode.Length > 100 ||
            now == default || now.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Agent identity state change is outside policy.");
        }
    }
}
