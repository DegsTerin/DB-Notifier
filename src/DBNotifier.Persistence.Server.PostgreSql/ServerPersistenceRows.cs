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

/// <summary>Stores one immutable accepted Agent heartbeat and the evidence required for exact replay handling.</summary>
public sealed class AgentHeartbeatRow
{
    /// <summary>Gets or sets the durable heartbeat identifier.</summary>
    public Guid HeartbeatId { get; set; }

    /// <summary>Gets or sets the Agent that owns the heartbeat sequence.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Gets or sets the Agent-provided idempotency identifier.</summary>
    public Guid MessageId { get; set; }

    /// <summary>Gets or sets the monotonic per-Agent sequence.</summary>
    public long Sequence { get; set; }

    /// <summary>Gets or sets the Agent version reported by the accepted heartbeat.</summary>
    public required string AgentVersion { get; set; }

    /// <summary>Gets or sets the minimum protocol major supported by the Agent.</summary>
    public int ProtocolMinimum { get; set; }

    /// <summary>Gets or sets the maximum protocol major supported by the Agent.</summary>
    public int ProtocolMaximum { get; set; }

    /// <summary>Gets or sets the bounded local queue depth reported by the Agent.</summary>
    public long QueueDepth { get; set; }

    /// <summary>Gets or sets the oldest queued-item instant when the queue is non-empty.</summary>
    public DateTimeOffset? OldestQueuedAt { get; set; }

    /// <summary>Gets or sets the Agent clock instant used only for skew estimation.</summary>
    public DateTimeOffset AgentTime { get; set; }

    /// <summary>Gets or sets the trusted server receipt instant.</summary>
    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>Gets or sets the canonical SHA-256 payload digest, or null for legacy rows that cannot prove replay.</summary>
    public string? PayloadSha256 { get; set; }

    /// <summary>Gets or sets the cursor value immediately before this heartbeat was accepted.</summary>
    public long? PreviousAcceptedSequence { get; set; }

    /// <summary>Gets or sets whether this accepted heartbeat exposed a sequence gap.</summary>
    public bool GapDetected { get; set; }
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
/// Stores the durable contiguous stream-resolution checkpoint for one enrolled Agent.
/// </summary>
/// <remarks>
/// The checkpoint is independent of raw observation retention so reconciliation never infers progress
/// from a potentially pruned history.
/// </remarks>
public sealed class AgentObservationCursorRow
{
    /// <summary>Gets or sets the enrolled Agent that owns the sequence stream.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Gets or sets the highest sequence resolved by an accepted observation or consumed terminal rejection.</summary>
    public long HighestContiguousSequence { get; set; }

    /// <summary>Gets or sets the UTC instant of the latest contiguous resolution.</summary>
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

/// <summary>
/// Persists the explicit provider-neutral route from one alert rule to one notification channel for one
/// deployment environment. Instance scope remains owned by the referenced rule.
/// </summary>
public sealed class AlertRuleChannelBindingRow
{
    /// <summary>Gets or sets the durable binding identifier used as delivery provenance.</summary>
    public Guid AlertRuleChannelBindingId { get; set; }

    /// <summary>Gets or sets the alert rule selected by this route.</summary>
    public Guid AlertRuleId { get; set; }

    /// <summary>Gets or sets the notification channel selected by this route.</summary>
    public Guid NotificationChannelId { get; set; }

    /// <summary>Gets or sets the exact instance environment admitted by this route.</summary>
    public required string Environment { get; set; }

    /// <summary>Gets or sets whether the route may create a pending delivery.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the UTC creation instant.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC update instant.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the optimistic concurrency token.</summary>
    public Guid ConcurrencyToken { get; set; }
}

/// <summary>
/// Persists one notification delivery with exact event-and-binding provenance. Historical deliveries without
/// provable provenance remain retained in the quarantined state and cannot become pending.
/// </summary>
public sealed class NotificationDeliveryRow
{
    /// <summary>Gets or sets the delivery identifier.</summary>
    public Guid NotificationDeliveryId { get; set; }

    /// <summary>Gets or sets the channel copied from the proven binding.</summary>
    public Guid NotificationChannelId { get; set; }

    /// <summary>Gets or sets the canonical event that triggered this delivery.</summary>
    public Guid EventId { get; set; }

    /// <summary>Gets or sets the proven binding, or null only for retained historical rows.</summary>
    public Guid? AlertRuleChannelBindingId { get; set; }

    /// <summary>Gets or sets the deterministic event-and-binding idempotency key.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Gets or sets the durable delivery state.</summary>
    public required string State { get; set; }

    /// <summary>Gets or sets the bounded delivery attempt count.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Gets or sets the UTC creation instant.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the most recent UTC attempt instant.</summary>
    public DateTimeOffset? LastAttemptAt { get; set; }

    /// <summary>Gets or sets the next database-clock instant at which a known retry may be claimed.</summary>
    public DateTimeOffset? AvailableAt { get; set; }

    /// <summary>Gets or sets the unique worker that currently owns the delivery lease.</summary>
    public Guid? LeaseOwnerId { get; set; }

    /// <summary>Gets or sets the monotonic fence retained across lease release and reclaim.</summary>
    public long LeaseFence { get; set; }

    /// <summary>Gets or sets the database-clock expiry of the active lease.</summary>
    public DateTimeOffset? LeaseExpiresAt { get; set; }

    /// <summary>Gets or sets when a potentially side-effecting adapter hand-off began.</summary>
    public DateTimeOffset? HandoffStartedAt { get; set; }

    /// <summary>Gets or sets the confirmed UTC delivery instant.</summary>
    public DateTimeOffset? DeliveredAt { get; set; }

    /// <summary>Gets or sets when the bounded attempt budget was exhausted.</summary>
    public DateTimeOffset? DeadLetteredAt { get; set; }

    /// <summary>Gets or sets when an unconfirmed side effect was retained without replay.</summary>
    public DateTimeOffset? AmbiguousAt { get; set; }

    /// <summary>Gets or sets a stable non-secret error or quarantine reason code.</summary>
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

/// <summary>Persists the highest contiguous command-transport sequence accepted for one Agent.</summary>
public sealed class ServerCommandTransportCursorRow
{
    /// <summary>Gets or sets the owning Agent identifier.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Gets or sets the highest request sequence committed by the sandbox protocol.</summary>
    public long HighestAcceptedSequence { get; set; }

    /// <summary>Gets or sets the trusted UTC cursor update instant.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the optimistic concurrency token.</summary>
    public Guid ConcurrencyToken { get; set; }
}

/// <summary>Persists exact request identity, digest and stable response for safe command-transport replay.</summary>
public sealed class ServerCommandTransportJournalRow
{
    /// <summary>Gets or sets the immutable request message identifier.</summary>
    public Guid MessageId { get; set; }

    /// <summary>Gets or sets the authenticated Agent identifier.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Gets or sets the exact per-Agent request sequence.</summary>
    public long Sequence { get; set; }

    /// <summary>Gets or sets the closed request message type.</summary>
    public required string MessageType { get; set; }

    /// <summary>Gets or sets the SHA-256 digest of the canonical request JSON.</summary>
    public required string RequestPayloadSha256 { get; set; }

    /// <summary>Gets or sets the stable response message identifier.</summary>
    public Guid ResponseMessageId { get; set; }

    /// <summary>Gets or sets the exact bounded response JSON returned for every exact replay.</summary>
    public required string ResponsePayloadJson { get; set; }

    /// <summary>Gets or sets the trusted first-receipt UTC instant.</summary>
    public DateTimeOffset ReceivedAt { get; set; }
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

    /// <summary>Gets or sets the unique worker that currently owns the outbox lease.</summary>
    public Guid? LeaseOwnerId { get; set; }

    /// <summary>Gets or sets the monotonic fence retained across lease release and reclaim.</summary>
    public long LeaseFence { get; set; }

    /// <summary>Gets or sets the database-clock expiry of the active lease.</summary>
    public DateTimeOffset? LeaseExpiresAt { get; set; }

    /// <summary>Gets or sets when a potentially side-effecting publisher hand-off began.</summary>
    public DateTimeOffset? HandoffStartedAt { get; set; }

    /// <summary>Gets or sets when the bounded attempt budget was exhausted.</summary>
    public DateTimeOffset? DeadLetteredAt { get; set; }

    /// <summary>Gets or sets when an unconfirmed side effect was retained without replay.</summary>
    public DateTimeOffset? AmbiguousAt { get; set; }

    /// <summary>Gets or sets a stable non-secret delivery failure code.</summary>
    public string? ErrorCode { get; set; }
}
