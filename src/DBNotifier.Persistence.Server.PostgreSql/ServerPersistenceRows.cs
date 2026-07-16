// Module purpose: Implements Server Persistence Rows for central PostgreSQL persistence with transactional and authorisation boundaries.
namespace DBNotifier.Persistence.Server.PostgreSql;

public sealed class DatabaseInstanceRow
{
    public Guid InstanceId { get; set; }
    public required string DisplayName { get; set; }
    public required string ProviderType { get; set; }
    public required string Environment { get; set; }
    public required string EndpointJson { get; set; }
    public string? MonitoringCredentialReference { get; set; }
    public string? AdministrativeCredentialReference { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public required string TagsJson { get; set; }
    public int IntervalSeconds { get; set; }
    public int TimeoutSeconds { get; set; }
    public int RetryCount { get; set; }
    public bool Enabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class RegisteredAgentRow
{
    public Guid AgentId { get; set; }
    public required string InstallationId { get; set; }
    public required string DisplayName { get; set; }
    public required string Environment { get; set; }
    public required string Platform { get; set; }
    public required string AgentVersion { get; set; }
    public required string CertificateThumbprint { get; set; }
    public required string State { get; set; }
    public DateTimeOffset EnrolledAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class AgentCapabilityRow
{
    public Guid AgentCapabilityId { get; set; }
    public Guid AgentId { get; set; }
    public required string CapabilityId { get; set; }
    public required string ProviderType { get; set; }
    public required string ProviderVersion { get; set; }
    public required string Platform { get; set; }
    public required string State { get; set; }
    public string? ReasonCode { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
}

public sealed class AgentHeartbeatRow
{
    public Guid HeartbeatId { get; set; }
    public Guid AgentId { get; set; }
    public Guid MessageId { get; set; }
    public long Sequence { get; set; }
    public required string AgentVersion { get; set; }
    public int ProtocolMinimum { get; set; }
    public int ProtocolMaximum { get; set; }
    public long QueueDepth { get; set; }
    public DateTimeOffset? OldestQueuedAt { get; set; }
    public DateTimeOffset AgentTime { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}

/// <summary>Stores one immutable raw health observation received from an authenticated assigned Agent.</summary>
public sealed class HealthSampleRow
{
    public Guid ObservationId { get; set; }
    public Guid InstanceId { get; set; }
    public Guid AgentId { get; set; }
    public Guid MessageId { get; set; }
    public long Sequence { get; set; }
    public required string ProviderType { get; set; }
    public required string ProviderVersion { get; set; }
    public required string Status { get; set; }
    public required string Method { get; set; }
    public required string EvidenceLevel { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public long DurationMilliseconds { get; set; }

    /// <summary>Gets or sets the bounded one-based provider attempt count recorded with the sample.</summary>
    public int AttemptCount { get; set; } = 1;
    public string? ErrorCode { get; set; }
    public string? RedactedDetailsJson { get; set; }

    /// <summary>Gets or sets the SHA-256 payload hash used to prove exact idempotent replay.</summary>
    public string? PayloadHash { get; set; }
}

/// <summary>
/// Stores the durable contiguous observation checkpoint for one enrolled Agent.
/// </summary>
/// <remarks>
/// The checkpoint is independent of raw observation retention so reconciliation never infers progress
/// from a potentially pruned history.
/// </remarks>
public sealed class AgentObservationCursorRow
{
    /// <summary>Gets or sets the enrolled Agent that owns the sequence stream.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Gets or sets the highest sequence reconciled without a preceding gap.</summary>
    public long HighestContiguousSequence { get; set; }

    /// <summary>Gets or sets the UTC instant of the latest reconciliation.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the optimistic concurrency token for cursor updates.</summary>
    public Guid ConcurrencyToken { get; set; }
}

/// <summary>
/// Stores the last reconciled observation state for one database instance.
/// </summary>
/// <remarks>
/// The source observation identifier is retained as factual provenance but is deliberately not a
/// foreign key, allowing authorised raw-sample retention without losing the durable state baseline.
/// </remarks>
public sealed class InstanceObservationStateRow
{
    /// <summary>Gets or sets the database instance represented by this durable state baseline.</summary>
    public Guid InstanceId { get; set; }

    /// <summary>Gets or sets the Agent that owned the reconciled source observation.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Gets or sets the source Agent sequence that last changed this state.</summary>
    public long LastProcessedSequence { get; set; }

    /// <summary>Gets or sets the source observation identifier retained as non-foreign-key provenance.</summary>
    public Guid ObservationId { get; set; }

    /// <summary>Gets or sets the canonical health-status name.</summary>
    public required string Status { get; set; }

    /// <summary>Gets or sets the UTC instant reported by the source observation.</summary>
    public DateTimeOffset ObservedAt { get; set; }

    /// <summary>Gets or sets the authoritative UTC receipt instant assigned by the server.</summary>
    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>Gets or sets the optimistic concurrency token for state replacement.</summary>
    public Guid ConcurrencyToken { get; set; }
}

public sealed class EventRecordRow
{
    public Guid EventId { get; set; }
    public Guid? InstanceId { get; set; }
    public Guid? AgentId { get; set; }
    public Guid? SourceObservationId { get; set; }
    public Guid CorrelationId { get; set; }
    public required string EventType { get; set; }
    public required string Severity { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public required string DetailsJson { get; set; }
}

public sealed class IncidentRow
{
    public Guid IncidentId { get; set; }
    public Guid InstanceId { get; set; }
    public required string Status { get; set; }
    public required string Severity { get; set; }
    public required string Title { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedByUserId { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class MaintenanceWindowRow
{
    public Guid MaintenanceWindowId { get; set; }
    public Guid InstanceId { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class AlertRuleRow
{
    public Guid AlertRuleId { get; set; }
    public required string Name { get; set; }
    public string? InstanceScopeJson { get; set; }
    public required string RuleType { get; set; }
    public required string ConfigurationJson { get; set; }
    public bool Enabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class NotificationChannelRow
{
    public Guid NotificationChannelId { get; set; }
    public required string Name { get; set; }
    public required string ChannelType { get; set; }
    public required string NonSecretConfigurationJson { get; set; }
    public string? CredentialReference { get; set; }
    public bool Enabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class NotificationDeliveryRow
{
    public Guid NotificationDeliveryId { get; set; }
    public Guid NotificationChannelId { get; set; }
    public Guid EventId { get; set; }
    public required string State { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? ErrorCode { get; set; }
}

public sealed class AdministrativeCommandRow
{
    public Guid CommandId { get; set; }
    public required string IdempotencyKey { get; set; }
    public Guid InstanceId { get; set; }
    public Guid AssignedAgentId { get; set; }
    public required string CapabilityId { get; set; }
    public required string TypedParametersJson { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public required string AuthorizationSnapshotReference { get; set; }
    public required string ExpectedAgentVersion { get; set; }
    public required string ExpectedProviderVersion { get; set; }
    public required string State { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class CommandAttemptRow
{
    public Guid CommandAttemptId { get; set; }
    public Guid CommandId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public required string State { get; set; }
    public Guid? PostProbeObservationId { get; set; }
    public string? ErrorCode { get; set; }
    public string? DiagnosticReference { get; set; }
}

public sealed class PlatformUserRow
{
    public Guid UserId { get; set; }
    public required string SubjectId { get; set; }
    public required string DisplayName { get; set; }
    public required string State { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class RoleRow
{
    public Guid RoleId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsSystem { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class PermissionRow
{
    public Guid PermissionId { get; set; }
    public required string Code { get; set; }
    public required string Description { get; set; }
}

public sealed class RolePermissionRow
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

public sealed class RoleAssignmentRow
{
    public Guid RoleAssignmentId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public required string ScopeType { get; set; }
    public required string ScopeValue { get; set; }
    public Guid GrantedByUserId { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public sealed class AuditEntryRow
{
    public Guid AuditEntryId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public required string ActorType { get; set; }
    public required string ActorId { get; set; }
    public required string Action { get; set; }
    public required string TargetType { get; set; }
    public required string TargetId { get; set; }
    public required string Outcome { get; set; }
    public Guid CorrelationId { get; set; }
    public required string DetailsJson { get; set; }
}

public sealed class ServerOutboxMessageRow
{
    public Guid MessageId { get; set; }
    public required string MessageType { get; set; }
    public int SchemaVersion { get; set; }
    public required string PayloadJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
