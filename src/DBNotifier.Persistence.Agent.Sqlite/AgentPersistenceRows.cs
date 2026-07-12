namespace DBNotifier.Persistence.Agent.Sqlite;

public sealed class AgentRegistrationRow
{
    public Guid AgentId { get; set; }
    public required string InstallationId { get; set; }
    public required string Environment { get; set; }
    public required string IdentityCertificateReference { get; set; }
    public string? ActiveConfigurationVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid ConcurrencyToken { get; set; }
}

public sealed class AgentInstanceAssignmentRow
{
    public Guid InstanceId { get; set; }
    public required string DisplayName { get; set; }
    public required string ProviderType { get; set; }
    public required string EndpointJson { get; set; }
    public string? MonitoringCredentialReference { get; set; }
    public string? AdministrativeCredentialReference { get; set; }
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
