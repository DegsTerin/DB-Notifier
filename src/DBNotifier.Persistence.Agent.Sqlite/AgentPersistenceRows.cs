// Module purpose: Implements Agent Persistence Rows for the Agent-local SQLite boundary without exposing monitored database secrets.
namespace DBNotifier.Persistence.Agent.Sqlite;

/// <summary>Persists non-secret local Agent identity metadata and its fail-closed lifecycle state.</summary>
public sealed class AgentRegistrationRow
{
    public Guid AgentId { get; set; }
    public required string InstallationId { get; set; }
    public required string Environment { get; set; }
    public required string IdentityCertificateReference { get; set; }
    /// <summary>Gets or sets the canonical public certificate thumbprint.</summary>
    public string CertificateThumbprint { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC certificate expiry used for local fail-closed checks.</summary>
    public DateTimeOffset CertificateNotAfter { get; set; }

    /// <summary>Gets or sets the exact persisted <c>AgentLocalIdentityState</c> name.</summary>
    public string IdentityState { get; set; } = "NotEnrolled";
    public string? ActiveConfigurationVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

/// <summary>Persists one complete read-only local assignment projection without administrative material.</summary>
public sealed class AgentInstanceAssignmentRow
{
    public Guid InstanceId { get; set; }
    public required string DisplayName { get; set; }
    public required string ProviderType { get; set; }
    public required string EndpointJson { get; set; }
    public string? MonitoringCredentialReference { get; set; }
    public string? AdministrativeCredentialReference { get; set; }
    /// <summary>Gets or sets the validated non-secret assignment tags JSON.</summary>
    public string TagsJson { get; set; } = "[]";
    public int IntervalSeconds { get; set; }
    public int TimeoutSeconds { get; set; }
    public int RetryCount { get; set; }
    public bool Enabled { get; set; }
    public required string PolicyVersion { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class AgentHealthObservationRow
{
    public Guid ObservationId { get; set; }
    public Guid InstanceId { get; set; }
    public required string ProviderType { get; set; }
    public required string ProviderVersion { get; set; }
    public required string Status { get; set; }
    public required string Method { get; set; }
    public required string EvidenceLevel { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public long DurationMilliseconds { get; set; }
    public int AttemptCount { get; set; }
    public string? ErrorCode { get; set; }
    public string? SafeErrorMessage { get; set; }
    public string? RedactedDetailsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AgentOutboxMessageRow
{
    public Guid MessageId { get; set; }
    public long Sequence { get; set; }
    public required string MessageType { get; set; }
    public int SchemaVersion { get; set; }
    public required string PayloadJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
}

public sealed class AgentInboxCommandRow
{
    public Guid CommandId { get; set; }
    public required string IdempotencyKey { get; set; }
    public Guid InstanceId { get; set; }
    public required string ProviderId { get; set; }
    public required string CapabilityId { get; set; }
    public required string TypedParametersJson { get; set; }
    public required string State { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public required string ExpectedAgentVersion { get; set; }
    public required string ExpectedProviderVersion { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ResultJson { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class AgentCheckpointRow
{
    public required string StreamName { get; set; }
    public long Sequence { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Persists exact heartbeat replay evidence and last-known-valid assignment reconciliation metadata.</summary>
public sealed class AgentFleetStateRow
{
    /// <summary>Gets or sets the sole local Agent identifier and foreign key.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Gets or sets the next heartbeat sequence when no earlier envelope remains pending.</summary>
    public long NextHeartbeatSequence { get; set; }

    /// <summary>Gets or sets the message identifier of the exact pending heartbeat.</summary>
    public Guid? PendingHeartbeatMessageId { get; set; }

    /// <summary>Gets or sets the sequence of the exact pending heartbeat.</summary>
    public long? PendingHeartbeatSequence { get; set; }

    /// <summary>Gets or sets the bounded exact pending heartbeat JSON.</summary>
    public string? PendingHeartbeatPayloadJson { get; set; }

    /// <summary>Gets or sets the last durable Server heartbeat acceptance instant.</summary>
    public DateTimeOffset? LastHeartbeatAcceptedAt { get; set; }

    /// <summary>Gets or sets the strong ETag of the last-known-valid assignment snapshot.</summary>
    public string? AssignmentEntityTag { get; set; }

    /// <summary>Gets or sets the trusted Server generation instant of the active snapshot.</summary>
    public DateTimeOffset? AssignmentGeneratedAt { get; set; }

    /// <summary>Gets or sets the last successful local reconciliation instant.</summary>
    public DateTimeOffset? AssignmentLastSucceededAt { get; set; }

    /// <summary>Gets or sets the latest local heartbeat or assignment attempt instant.</summary>
    public DateTimeOffset? LastAttemptAt { get; set; }

    /// <summary>Gets or sets the latest sanitised failure code.</summary>
    public string? LastErrorCode { get; set; }

    /// <summary>Gets or sets the optimistic concurrency token.</summary>
    public Guid ConcurrencyToken { get; set; }
}
