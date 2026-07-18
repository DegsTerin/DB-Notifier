// Module purpose: Orchestrates bounded sandbox-only Agent Fleet retries under one expiring local fencing lease without enabling the ordinary Worker.
namespace DBNotifier.Application.AgentFleet;

/// <summary>Defines bounded retry and lease limits for one sandbox resilience operation.</summary>
/// <param name="MaximumAttempts">Maximum total attempts, including the first call.</param>
/// <param name="InitialDelay">Initial exponential delay.</param>
/// <param name="MaximumDelay">Maximum delay admitted before jitter and Retry-After.</param>
/// <param name="MaximumElapsed">Maximum cumulative scheduled delay.</param>
/// <param name="LeaseDuration">Local fencing lease duration, which must exceed the elapsed budget.</param>
/// <param name="MaximumJitterRatio">Maximum proportional jitter in the inclusive range zero to one half.</param>
public sealed record AgentFleetSandboxRetryPolicy(
    int MaximumAttempts,
    TimeSpan InitialDelay,
    TimeSpan MaximumDelay,
    TimeSpan MaximumElapsed,
    TimeSpan LeaseDuration,
    double MaximumJitterRatio);

/// <summary>Provides cancellable sandbox delays without coupling tests to wall-clock waiting.</summary>
public interface IAgentFleetSandboxDelay
{
    /// <summary>Waits or advances a controlled test clock for one bounded delay.</summary>
    /// <param name="delay">Validated non-negative delay.</param>
    /// <param name="cancellationToken">Cancellation that must interrupt the delay.</param>
    ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

/// <summary>Provides deterministic jitter samples for retry scheduling.</summary>
public interface IAgentFleetSandboxJitter
{
    /// <summary>Returns the next finite sample in the inclusive range zero to one.</summary>
    /// <returns>Deterministic unit-interval sample.</returns>
    double NextUnitInterval();
}

/// <summary>Summarises one fenced sandbox operation without exposing transport or identity material.</summary>
/// <param name="Result">Final sanitised Agent Fleet result.</param>
/// <param name="Attempts">Number of coordinator calls made.</param>
/// <param name="FenceToken">Monotonic local fence used by every mutation.</param>
/// <param name="ScheduledDelays">Exact bounded delays requested before retries.</param>
public sealed record AgentFleetSandboxResilienceResult(
    AgentFleetClientResult Result,
    int Attempts,
    long? FenceToken,
    IReadOnlyList<TimeSpan> ScheduledDelays);

/// <summary>
/// Runs heartbeat or assignment reconciliation only when an expiring SQLite lease proves one current local owner.
/// It is not registered by the ordinary Worker and cannot enrol, activate providers, or execute commands.
/// </summary>
/// <param name="coordinator">One-shot Agent Fleet client coordinator.</param>
/// <param name="localStore">Durable local store that owns lease and fencing validation.</param>
/// <param name="timeProvider">Controlled sandbox clock.</param>
/// <param name="delay">Cancellable delay boundary.</param>
/// <param name="jitter">Deterministic jitter source.</param>
/// <param name="policy">Bounded retry and lease policy.</param>
public sealed class AgentFleetSandboxResilienceCoordinator(
    AgentFleetClientCoordinator coordinator,
    IAgentFleetLocalStore localStore,
    TimeProvider timeProvider,
    IAgentFleetSandboxDelay delay,
    IAgentFleetSandboxJitter jitter,
    AgentFleetSandboxRetryPolicy policy)
{
    private readonly AgentFleetSandboxRetryPolicy boundedPolicy = ValidatePolicy(policy);

    /// <summary>Delivers one heartbeat under an exact local fence and retries transient results only.</summary>
    /// <param name="ownerId">Bounded unique identifier for the temporary sandbox process.</param>
    /// <param name="agentVersion">Bounded Agent version.</param>
    /// <param name="cancellationToken">Cancellation for lease, transport, delay and persistence.</param>
    /// <returns>Final result, attempt count, fence and exact scheduled delays.</returns>
    public ValueTask<AgentFleetSandboxResilienceResult> SendHeartbeatAsync(
        string ownerId,
        string agentVersion,
        CancellationToken cancellationToken) => RunAsync(
            ownerId,
            AgentFleetOperationKind.Heartbeat,
            (lease, token) => coordinator.SendHeartbeatOnceAsync(agentVersion, lease, token),
            cancellationToken);

    /// <summary>Reconciles one assignment snapshot under an exact local fence and retries transient results only.</summary>
    /// <param name="ownerId">Bounded unique identifier for the temporary sandbox process.</param>
    /// <param name="agentVersion">Bounded Agent version.</param>
    /// <param name="cancellationToken">Cancellation for lease, transport, delay and persistence.</param>
    /// <returns>Final result, attempt count, fence and exact scheduled delays.</returns>
    public ValueTask<AgentFleetSandboxResilienceResult> ReconcileAssignmentsAsync(
        string ownerId,
        string agentVersion,
        CancellationToken cancellationToken) => RunAsync(
            ownerId,
            AgentFleetOperationKind.AssignmentReconciliation,
            (lease, token) => coordinator.ReconcileAssignmentsOnceAsync(agentVersion, lease, token),
            cancellationToken);

    /// <summary>Runs one operation under a single fence and a bounded transient-only retry budget.</summary>
    /// <param name="ownerId">Validated sandbox owner.</param>
    /// <param name="kind">Exact operation protected by the lease.</param>
    /// <param name="operation">One-shot operation that must use the supplied lease.</param>
    /// <param name="cancellationToken">Cancellation for acquisition, transport and delay.</param>
    /// <returns>Final bounded result and deterministic scheduling evidence.</returns>
    private async ValueTask<AgentFleetSandboxResilienceResult> RunAsync(
        string ownerId,
        AgentFleetOperationKind kind,
        Func<AgentFleetOperationLease, CancellationToken, ValueTask<AgentFleetClientResult>> operation,
        CancellationToken cancellationToken)
    {
        ValidateOwner(ownerId);
        AgentLocalRegistration? registration = await localStore
            .GetRegistrationAsync(cancellationToken)
            .ConfigureAwait(false);
        if (registration is null)
        {
            return new AgentFleetSandboxResilienceResult(
                new AgentFleetClientResult(false, AgentLocalIdentityState.NotEnrolled, "agent.not_enrolled"),
                0,
                null,
                []);
        }

        AgentFleetOperationLease? lease = await localStore.TryAcquireOperationLeaseAsync(
            registration.AgentId,
            ownerId,
            kind,
            timeProvider.GetUtcNow(),
            boundedPolicy.LeaseDuration,
            cancellationToken).ConfigureAwait(false);
        if (lease is null)
        {
            return new AgentFleetSandboxResilienceResult(
                new AgentFleetClientResult(false, AgentLocalIdentityState.Conflict, "agent_fleet.operation_busy"),
                0,
                null,
                []);
        }

        List<TimeSpan> scheduledDelays = [];
        try
        {
            AgentFleetClientResult result = new(false, AgentLocalIdentityState.Conflict, "agent_fleet.not_attempted");
            TimeSpan elapsed = TimeSpan.Zero;
            for (int attempt = 1; attempt <= boundedPolicy.MaximumAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result = await operation(lease, cancellationToken).ConfigureAwait(false);
                if (!result.Retryable)
                {
                    return new AgentFleetSandboxResilienceResult(result, attempt, lease.FenceToken, scheduledDelays);
                }

                if (attempt == boundedPolicy.MaximumAttempts)
                {
                    return new AgentFleetSandboxResilienceResult(
                        result with { Code = "agent_fleet.retry_budget_exhausted", Retryable = false },
                        attempt,
                        lease.FenceToken,
                        scheduledDelays);
                }

                TimeSpan nextDelay = CalculateDelay(attempt, result.RetryAfter);
                if (nextDelay < TimeSpan.Zero || elapsed + nextDelay > boundedPolicy.MaximumElapsed ||
                    timeProvider.GetUtcNow() + nextDelay >= lease.ExpiresAt)
                {
                    return new AgentFleetSandboxResilienceResult(
                        result with { Code = "agent_fleet.retry_budget_exhausted", Retryable = false },
                        attempt,
                        lease.FenceToken,
                        scheduledDelays);
                }

                scheduledDelays.Add(nextDelay);
                await delay.DelayAsync(nextDelay, cancellationToken).ConfigureAwait(false);
                elapsed += nextDelay;
            }

            throw new InvalidOperationException("agent_fleet.retry_loop_invalid");
        }
        finally
        {
            await localStore.ReleaseOperationLeaseAsync(lease, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>Calculates one bounded exponential delay with deterministic symmetric jitter.</summary>
    /// <param name="completedAttempt">One-based number of the completed failed attempt.</param>
    /// <param name="retryAfter">Optional server-supplied upper bound.</param>
    /// <returns>Non-negative delay admitted by policy.</returns>
    private TimeSpan CalculateDelay(int completedAttempt, TimeSpan? retryAfter)
    {
        double exponent = Math.Pow(2, completedAttempt - 1);
        double baseMilliseconds = Math.Min(
            boundedPolicy.MaximumDelay.TotalMilliseconds,
            boundedPolicy.InitialDelay.TotalMilliseconds * exponent);
        double sample = jitter.NextUnitInterval();
        if (!double.IsFinite(sample) || sample is < 0 or > 1)
        {
            throw new InvalidOperationException("agent_fleet.jitter_invalid");
        }

        double multiplier = 1 + ((sample * 2) - 1) * boundedPolicy.MaximumJitterRatio;
        TimeSpan candidate = TimeSpan.FromMilliseconds(baseMilliseconds * multiplier);
        return retryAfter is { } ceiling && ceiling >= TimeSpan.Zero && ceiling < candidate
            ? ceiling
            : candidate;
    }

    /// <summary>Refuses unbounded attempts, delay, elapsed time, lease duration or jitter.</summary>
    /// <param name="value">Candidate sandbox policy.</param>
    /// <returns>The validated immutable policy.</returns>
    private static AgentFleetSandboxRetryPolicy ValidatePolicy(AgentFleetSandboxRetryPolicy value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.MaximumAttempts is < 1 or > 8 ||
            value.InitialDelay < TimeSpan.Zero || value.InitialDelay > TimeSpan.FromMinutes(1) ||
            value.MaximumDelay < value.InitialDelay || value.MaximumDelay > TimeSpan.FromMinutes(5) ||
            value.MaximumElapsed < TimeSpan.Zero || value.MaximumElapsed > TimeSpan.FromMinutes(15) ||
            value.LeaseDuration <= value.MaximumElapsed || value.LeaseDuration > TimeSpan.FromMinutes(30) ||
            !double.IsFinite(value.MaximumJitterRatio) || value.MaximumJitterRatio is < 0 or > 0.5)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Sandbox retry policy is outside bounds.");
        }

        return value;
    }

    /// <summary>Validates one bounded owner identifier suitable for SQLite and sanitised evidence.</summary>
    /// <param name="ownerId">Candidate process-local owner.</param>
    private static void ValidateOwner(string ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId) || ownerId.Length is < 8 or > 160 ||
            ownerId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or ':' or '-')))
        {
            throw new ArgumentException("Sandbox operation owner is outside policy.", nameof(ownerId));
        }
    }
}
