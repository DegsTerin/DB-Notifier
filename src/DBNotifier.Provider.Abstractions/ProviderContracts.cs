// Module purpose: Defines Provider Contracts as an engine-neutral provider contract shared by Application and adapters.
using DBNotifier.Domain;

namespace DBNotifier.Provider.Abstractions;

public enum CapabilityState
{
    Supported,
    Unsupported,
    Unavailable,
    Unknown,
}

public sealed record ProviderCapability(
    string CapabilityId,
    CapabilityState State,
    string Platform,
    string ReasonCode);

public sealed record ProviderValidationError(string Code, string Field, string SafeMessage);

public sealed class ProviderValidationResult
{
    private ProviderValidationResult(IReadOnlyList<ProviderValidationError> errors)
    {
        Errors = errors;
    }

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<ProviderValidationError> Errors { get; }

    public static ProviderValidationResult Valid { get; } = new([]);

    public static ProviderValidationResult Invalid(params ProviderValidationError[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Length == 0)
        {
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        }

        return new(errors);
    }
}

public sealed record ProviderProbeRequest(
    ProviderEndpoint Endpoint,
    IProviderCredential? MonitoringCredential,
    TimeSpan Timeout,
    int AttemptNumber);

public interface IProviderCredential : IDisposable
{
    string? UserName { get; }

    ReadOnlyMemory<char> Secret { get; }

    DateTimeOffset? ExpiresAt { get; }
}

public sealed record ProviderProbeResult(
    HealthStatus Status,
    EvidenceLevel EvidenceLevel,
    string Method,
    TimeSpan Duration,
    NormalizedError? Error,
    IReadOnlyList<string> Limitations);

public interface IDatabaseProvider
{
    ProviderType ProviderType { get; }

    string Version { get; }

    IReadOnlyList<ProviderCapability> Capabilities { get; }

    ProviderValidationResult ValidateEndpoint(ProviderEndpoint endpoint);

    ValueTask<ProviderProbeResult> ProbeAsync(ProviderProbeRequest request, CancellationToken cancellationToken);
}

public interface IProviderRegistry
{
    IReadOnlyCollection<IDatabaseProvider> Providers { get; }

    bool TryResolve(ProviderType providerType, out IDatabaseProvider provider);
}
