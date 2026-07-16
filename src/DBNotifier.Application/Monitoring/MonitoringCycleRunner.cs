// Module purpose: Coordinates bounded provider-neutral monitoring cycles without depending on persistence, providers or user interfaces.
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Application.Monitoring;

/// <summary>Describes one provider-neutral monitoring assignment due for execution by an Agent.</summary>
/// <param name="InstanceId">Non-empty database-instance identifier.</param>
/// <param name="Endpoint">Typed non-secret provider endpoint.</param>
/// <param name="MonitoringCredentialReference">Optional opaque reference to a least-privilege monitoring credential.</param>
/// <param name="Policy">Bounded probe timeout, attempts and back-off policy.</param>
public sealed record MonitoringAssignment(
    Guid InstanceId,
    ProviderEndpoint Endpoint,
    CredentialReference? MonitoringCredentialReference,
    ProbePolicy Policy);

/// <summary>Supplies a bounded, fairness-ordered set of due monitoring assignments.</summary>
public interface IMonitoringAssignmentSource
{
    /// <summary>Reads assignments whose monitoring cadence is due at the supplied instant.</summary>
    /// <param name="now">Authoritative UTC instant for cadence evaluation.</param>
    /// <param name="cancellationToken">Cancellation propagated from the Agent worker.</param>
    /// <returns>A bounded provider-neutral assignment list.</returns>
    ValueTask<IReadOnlyList<MonitoringAssignment>> GetDueAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <summary>Owns durable, provider-neutral persistence of completed health observations.</summary>
public interface IHealthObservationSink
{
    /// <summary>Persists one observation without exposing its provider implementation to Application.</summary>
    /// <param name="observation">Validated health observation to persist.</param>
    /// <param name="cancellationToken">Cancellation propagated from the monitoring cycle.</param>
    /// <returns>A task that completes after durable persistence.</returns>
    ValueTask PersistAsync(HealthObservation observation, CancellationToken cancellationToken);
}

/// <summary>Identifies one assignment that did not complete successfully within a monitoring cycle.</summary>
/// <param name="InstanceId">Database-instance identifier of the unsuccessful assignment.</param>
/// <param name="Code">Stable, non-secret failure code.</param>
public sealed record MonitoringCycleFailure(Guid InstanceId, string Code);

/// <summary>Summarises one bounded monitoring cycle without exposing provider-native diagnostics.</summary>
/// <param name="DueCount">Number of assignments selected for the cycle.</param>
/// <param name="PersistedCount">Number of observations durably persisted.</param>
/// <param name="Failures">Stable failure outcomes for unsuccessful assignments.</param>
public sealed record MonitoringCycleResult(
    int DueCount,
    int PersistedCount,
    IReadOnlyList<MonitoringCycleFailure> Failures);

/// <summary>
/// Executes due provider-neutral probes with bounded worker concurrency, a global deadline and per-assignment
/// failure isolation, delegating durable persistence through the observation sink boundary.
/// </summary>
/// <param name="agentId">Non-empty Agent identity attached to each probe.</param>
/// <param name="assignmentSource">Source of the bounded, fairness-ordered due assignments.</param>
/// <param name="probeHandler">Application handler that resolves providers and monitoring credentials safely.</param>
/// <param name="observationSink">Durable observation boundary; its implementation owns write serialisation.</param>
/// <param name="timeProvider">Clock used to query due assignments at one explicit UTC instant.</param>
/// <param name="maximumConcurrency">Worker count between one and 32.</param>
/// <param name="cycleDeadline">Optional positive deadline no greater than 30 minutes.</param>
public sealed class MonitoringCycleRunner(
    Guid agentId,
    IMonitoringAssignmentSource assignmentSource,
    ProbeInstanceHandler probeHandler,
    IHealthObservationSink observationSink,
    TimeProvider timeProvider,
    int maximumConcurrency = 4,
    TimeSpan? cycleDeadline = null)
{
    private static readonly TimeSpan DefaultCycleDeadline = TimeSpan.FromMinutes(5);

    /// <summary>Runs one bounded monitoring cycle without allowing one target failure to abort unrelated targets.</summary>
    /// <param name="cancellationToken">Caller cancellation, distinct from the internal cycle deadline.</param>
    /// <returns>Due and persisted counts plus one stable failure code for each unsuccessful assignment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the Agent identity, concurrency or deadline is outside policy.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the caller cancels the cycle.</exception>
    public async ValueTask<MonitoringCycleResult> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumConcurrency, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumConcurrency, 32);
        TimeSpan effectiveDeadline = cycleDeadline ?? DefaultCycleDeadline;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(effectiveDeadline, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(effectiveDeadline, TimeSpan.FromMinutes(30));

        IReadOnlyList<MonitoringAssignment> assignments = await assignmentSource.GetDueAsync(
            timeProvider.GetUtcNow(),
            cancellationToken).ConfigureAwait(false);

        string?[] failureCodes = new string?[assignments.Count];
        bool[] completed = new bool[assignments.Count];
        int persistedCount = 0;
        int nextIndex = -1;
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(effectiveDeadline);

        async Task RunWorkerAsync()
        {
            while (!deadline.IsCancellationRequested)
            {
                int index = Interlocked.Increment(ref nextIndex);
                if (index >= assignments.Count)
                {
                    return;
                }

                MonitoringAssignment assignment = assignments[index];
                try
                {
                    HealthObservation observation = await probeHandler.ExecuteAsync(
                        new ProbeInstanceCommand(
                            assignment.InstanceId,
                            agentId,
                            assignment.Endpoint,
                            assignment.MonitoringCredentialReference,
                            assignment.Policy),
                        deadline.Token).ConfigureAwait(false);

                    await observationSink.PersistAsync(observation, deadline.Token).ConfigureAwait(false);
                    Interlocked.Increment(ref persistedCount);
                    completed[index] = true;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException) when (deadline.IsCancellationRequested)
                {
                    failureCodes[index] = "monitoring.cycle_deadline_exceeded";
                    completed[index] = true;
                    return;
                }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                {
                    failureCodes[index] = "monitoring.assignment_failed";
                    completed[index] = true;
                }
            }
        }

        int workerCount = Math.Min(maximumConcurrency, assignments.Count);
        await Task.WhenAll(Enumerable.Range(0, workerCount).Select(_ => RunWorkerAsync())).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (deadline.IsCancellationRequested)
        {
            for (int index = 0; index < failureCodes.Length; index++)
            {
                if (!completed[index])
                {
                    failureCodes[index] = "monitoring.cycle_deadline_exceeded";
                }
            }
        }

        MonitoringCycleFailure[] failures = assignments
            .Select((assignment, index) => failureCodes[index] is { } code
                ? new MonitoringCycleFailure(assignment.InstanceId, code)
                : null)
            .OfType<MonitoringCycleFailure>()
            .ToArray();

        return new MonitoringCycleResult(assignments.Count, persistedCount, failures);
    }
}
