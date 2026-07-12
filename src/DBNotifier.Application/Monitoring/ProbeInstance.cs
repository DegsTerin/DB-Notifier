// Module purpose: Defines Probe Instance application behaviour without depending on concrete providers or user interfaces.
using System.Diagnostics;
using DBNotifier.Application.Security;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Application.Monitoring;

public sealed record ProbePolicy(
    TimeSpan Timeout,
    int MaxAttempts,
    TimeSpan InitialBackoff,
    TimeSpan MaximumBackoff);

public sealed record ProbeInstanceCommand(
    Guid InstanceId,
    Guid AgentId,
    ProviderEndpoint Endpoint,
    CredentialReference? MonitoringCredentialReference,
    ProbePolicy Policy);

public sealed class ProbeInstanceHandler(
    IProviderRegistry providerRegistry,
    ICredentialVault credentialVault,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan MaximumOperationDuration = TimeSpan.FromMinutes(5);

    public async ValueTask<HealthObservation> ExecuteAsync(
        ProbeInstanceCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);

        DateTimeOffset observedAt = timeProvider.GetUtcNow();
        if (!providerRegistry.TryResolve(command.Endpoint.ProviderType, out IDatabaseProvider provider))
        {
            return CreateUnknownProviderObservation(command, observedAt);
        }

        ProviderValidationResult validation = provider.ValidateEndpoint(command.Endpoint);
        if (!validation.IsValid)
        {
            ProviderValidationError firstError = validation.Errors[0];
            return CreateObservation(
                command,
                provider,
                observedAt,
                HealthStatus.Unknown,
                "configuration-validation",
                TimeSpan.Zero,
                EvidenceLevel.Unknown,
                1,
                ["endpoint-invalid"],
                new NormalizedError(
                    firstError.Code,
                    ErrorCategory.Configuration,
                    Retryability.AfterConfigurationChange,
                    firstError.SafeMessage));
        }

        IProviderCredential? credential = await ResolveCredentialAsync(command, cancellationToken).ConfigureAwait(false);
        if (command.MonitoringCredentialReference is not null && credential is null)
        {
            return CreateObservation(
                command,
                provider,
                observedAt,
                HealthStatus.Unknown,
                "credential-vault",
                TimeSpan.Zero,
                EvidenceLevel.Unknown,
                1,
                ["credential-unavailable"],
                new NormalizedError(
                    "credential.unavailable",
                    ErrorCategory.Resource,
                    Retryability.Backoff,
                    "The monitoring credential could not be resolved."));
        }

        if (credential?.ExpiresAt is DateTimeOffset expiresAt && expiresAt <= observedAt)
        {
            credential.Dispose();
            return CreateObservation(
                command,
                provider,
                observedAt,
                HealthStatus.AuthFailed,
                "credential-vault",
                TimeSpan.Zero,
                EvidenceLevel.Unknown,
                1,
                ["credential-expired"],
                new NormalizedError(
                    "credential.expired",
                    ErrorCategory.Authentication,
                    Retryability.AfterConfigurationChange,
                    "The monitoring credential has expired."));
        }

        using (credential)
        {
            return await ExecuteWithRetryAsync(command, provider, credential, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<HealthObservation> ExecuteWithRetryAsync(
        ProbeInstanceCommand command,
        IDatabaseProvider provider,
        IProviderCredential? credential,
        CancellationToken cancellationToken)
    {
        TimeSpan backoff = command.Policy.InitialBackoff;
        for (int attempt = 1; attempt <= command.Policy.MaxAttempts; attempt++)
        {
            DateTimeOffset observedAt = timeProvider.GetUtcNow();
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                ProviderProbeResult result = await provider.ProbeAsync(
                    new ProviderProbeRequest(command.Endpoint, credential, command.Policy.Timeout, attempt),
                    cancellationToken).ConfigureAwait(false);

                if (attempt < command.Policy.MaxAttempts && result.Error?.Retryability == Retryability.Backoff)
                {
                    await DelayAsync(backoff, cancellationToken).ConfigureAwait(false);
                    backoff = NextBackoff(backoff, command.Policy.MaximumBackoff);
                    continue;
                }

                return CreateObservation(
                    command,
                    provider,
                    observedAt,
                    result.Status,
                    result.Method,
                    result.Duration,
                    result.EvidenceLevel,
                    attempt,
                    result.Limitations,
                    result.Error);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                return CreateObservation(
                    command,
                    provider,
                    observedAt,
                    HealthStatus.Unknown,
                    "provider-boundary",
                    stopwatch.Elapsed,
                    EvidenceLevel.Unknown,
                    attempt,
                    ["provider-failure-redacted"],
                    new NormalizedError(
                        "provider.unhandled_failure",
                        ErrorCategory.Internal,
                        Retryability.Unknown,
                        "The provider failed without a safe normalized result."));
            }
        }

        throw new UnreachableException();
    }

    private async ValueTask<IProviderCredential?> ResolveCredentialAsync(
        ProbeInstanceCommand command,
        CancellationToken cancellationToken)
    {
        if (command.MonitoringCredentialReference is null)
        {
            return null;
        }

        try
        {
            return await credentialVault.ResolveAsync(
                command.MonitoringCredentialReference,
                cancellationToken).ConfigureAwait(false);
        }
        catch (CredentialUnavailableException)
        {
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return null;
        }
    }

    private async ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    private static TimeSpan NextBackoff(TimeSpan current, TimeSpan maximum)
    {
        long doubledTicks = current.Ticks > maximum.Ticks / 2 ? maximum.Ticks : current.Ticks * 2;
        return TimeSpan.FromTicks(Math.Min(doubledTicks, maximum.Ticks));
    }

    private static void Validate(ProbeInstanceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfEqual(command.InstanceId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(command.AgentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(command.Policy.Timeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(command.Policy.Timeout, MaximumOperationDuration);
        ArgumentOutOfRangeException.ThrowIfLessThan(command.Policy.MaxAttempts, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(command.Policy.MaxAttempts, 10);
        ArgumentOutOfRangeException.ThrowIfLessThan(command.Policy.InitialBackoff, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(command.Policy.MaximumBackoff, command.Policy.InitialBackoff);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(command.Policy.MaximumBackoff, MaximumOperationDuration);

        if (command.MonitoringCredentialReference is not null &&
            command.MonitoringCredentialReference.Purpose != CredentialPurpose.Monitoring)
        {
            throw new ArgumentException(
                "Only a monitoring credential reference can be used for a health probe.",
                nameof(command));
        }
    }

    private static HealthObservation CreateObservation(
        ProbeInstanceCommand command,
        IDatabaseProvider provider,
        DateTimeOffset observedAt,
        HealthStatus status,
        string method,
        TimeSpan duration,
        EvidenceLevel evidenceLevel,
        int attempt,
        IReadOnlyList<string> limitations,
        NormalizedError? error) =>
        new(
            Guid.NewGuid(),
            command.InstanceId,
            command.AgentId,
            provider.ProviderType,
            provider.Version,
            status,
            method,
            observedAt,
            duration,
            new ObservationQuality(evidenceLevel, attempt, limitations),
            error);

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
