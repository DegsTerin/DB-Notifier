using System.Diagnostics;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Application.Monitoring;

public sealed record ProbeInstanceCommand(
    Guid InstanceId,
    Guid AgentId,
    ProviderEndpoint Endpoint,
    CredentialReference? MonitoringCredentialReference,
    TimeSpan Timeout);

public sealed class ProbeInstanceHandler(IProviderRegistry providerRegistry, TimeProvider timeProvider)
{
    public async ValueTask<HealthObservation> ExecuteAsync(
        ProbeInstanceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfEqual(command.InstanceId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(command.AgentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(command.Timeout, TimeSpan.Zero);

        DateTimeOffset observedAt = timeProvider.GetUtcNow();
        if (!providerRegistry.TryResolve(command.Endpoint.ProviderType, out IDatabaseProvider provider))
        {
            return CreateUnknownProviderObservation(command, observedAt);
        }

        ProviderValidationResult validation = provider.ValidateEndpoint(command.Endpoint);
        if (!validation.IsValid)
        {
            ProviderValidationError firstError = validation.Errors[0];
            return new HealthObservation(
                Guid.NewGuid(),
                command.InstanceId,
                command.AgentId,
                command.Endpoint.ProviderType,
                provider.Version,
                HealthStatus.Unknown,
                "configuration-validation",
                observedAt,
                TimeSpan.Zero,
                new ObservationQuality(EvidenceLevel.Unknown, 1, ["endpoint-invalid"]),
                new NormalizedError(
                    firstError.Code,
                    ErrorCategory.Configuration,
                    Retryability.AfterConfigurationChange,
                    firstError.SafeMessage));
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            ProviderProbeResult result = await provider.ProbeAsync(
                new ProviderProbeRequest(
                    command.Endpoint,
                    command.MonitoringCredentialReference,
                    command.Timeout,
                    1),
                cancellationToken).ConfigureAwait(false);

            return new HealthObservation(
                Guid.NewGuid(),
                command.InstanceId,
                command.AgentId,
                provider.ProviderType,
                provider.Version,
                result.Status,
                result.Method,
                observedAt,
                result.Duration,
                new ObservationQuality(result.EvidenceLevel, 1, result.Limitations),
                result.Error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return new HealthObservation(
                Guid.NewGuid(),
                command.InstanceId,
                command.AgentId,
                provider.ProviderType,
                provider.Version,
                HealthStatus.Unknown,
                "provider-boundary",
                observedAt,
                stopwatch.Elapsed,
                new ObservationQuality(EvidenceLevel.Unknown, 1, ["provider-failure-redacted"]),
                new NormalizedError(
                    "provider.unhandled_failure",
                    ErrorCategory.Internal,
                    Retryability.Unknown,
                    "The provider failed without a safe normalized result."));
        }
    }

    private static HealthObservation CreateUnknownProviderObservation(
        ProbeInstanceCommand command,
        DateTimeOffset observedAt) =>
        new(
            Guid.NewGuid(),
            command.InstanceId,
            command.AgentId,
            command.Endpoint.ProviderType,
            "unregistered",
            HealthStatus.Unknown,
            "provider-registry",
            observedAt,
            TimeSpan.Zero,
            new ObservationQuality(EvidenceLevel.Unknown, 1, ["provider-not-registered"]),
            new NormalizedError(
                "provider.not_registered",
                ErrorCategory.Compatibility,
                Retryability.AfterConfigurationChange,
                "No provider is registered for the configured provider type."));
}
