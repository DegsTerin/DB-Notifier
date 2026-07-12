// Module purpose: Defines Health Observation domain semantics independently of providers, persistence and presentation.
namespace DBNotifier.Domain;

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unavailable,
    AuthFailed,
    Timeout,
    Maintenance,
    Unknown,
}

public enum EvidenceLevel
{
    ProviderAuthenticated,
    ProviderReadiness,
    TransportOnly,
    Synthetic,
    Unknown,
}

public enum ErrorCategory
{
    Configuration,
    Network,
    Timeout,
    Authentication,
    Authorization,
    Provider,
    Platform,
    Resource,
    Compatibility,
    Internal,
}

public enum Retryability
{
    Never,
    Backoff,
    AfterConfigurationChange,
    Unknown,
}

public sealed record NormalizedError(
    string Code,
    ErrorCategory Category,
    Retryability Retryability,
    string SafeMessage);

public sealed record ObservationQuality(
    EvidenceLevel EvidenceLevel,
    int AttemptCount,
    IReadOnlyList<string> Limitations);

public sealed class HealthObservation
{
    public HealthObservation(
        Guid observationId,
        Guid instanceId,
        Guid agentId,
        ProviderType providerType,
        string providerVersion,
        HealthStatus status,
        string method,
        DateTimeOffset observedAt,
        TimeSpan duration,
        ObservationQuality quality,
        NormalizedError? error = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(observationId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(instanceId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        if (!ProviderType.TryParse(providerType.Value, out ProviderType canonicalProviderType) ||
            canonicalProviderType != providerType)
        {
            throw new ArgumentException("Provider type must be canonical.", nameof(providerType));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(providerVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(quality);
        ArgumentOutOfRangeException.ThrowIfLessThan(quality.AttemptCount, 1);

        ObservationId = observationId;
        InstanceId = instanceId;
        AgentId = agentId;
        ProviderType = canonicalProviderType;
        ProviderVersion = providerVersion;
        Status = status;
        Method = method;
        ObservedAt = observedAt;
        Duration = duration;
        Quality = quality;
        Error = error;
    }

    public Guid ObservationId { get; }

    public Guid InstanceId { get; }

    public Guid AgentId { get; }

    public ProviderType ProviderType { get; }

    public string ProviderVersion { get; }

    public HealthStatus Status { get; }

    public string Method { get; }

    public DateTimeOffset ObservedAt { get; }

    public TimeSpan Duration { get; }

    public ObservationQuality Quality { get; }

    public NormalizedError? Error { get; }
}
