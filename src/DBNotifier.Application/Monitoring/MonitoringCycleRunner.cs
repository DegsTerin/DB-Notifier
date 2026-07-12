// Module purpose: Defines Monitoring Cycle Runner application behaviour without depending on concrete providers or user interfaces.
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Application.Monitoring;

public sealed record MonitoringAssignment(
    Guid InstanceId,
    ProviderEndpoint Endpoint,
    CredentialReference? MonitoringCredentialReference,
    ProbePolicy Policy);

public interface IMonitoringAssignmentSource
{
    ValueTask<IReadOnlyList<MonitoringAssignment>> GetDueAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public interface IHealthObservationSink
{
    ValueTask PersistAsync(HealthObservation observation, CancellationToken cancellationToken);
}

public sealed record MonitoringCycleFailure(Guid InstanceId, string Code);

public sealed record MonitoringCycleResult(
    int DueCount,
    int PersistedCount,
    IReadOnlyList<MonitoringCycleFailure> Failures);

public sealed class MonitoringCycleRunner(
    Guid agentId,
    IMonitoringAssignmentSource assignmentSource,
    ProbeInstanceHandler probeHandler,
    IHealthObservationSink observationSink,
    TimeProvider timeProvider)
{
    public async ValueTask<MonitoringCycleResult> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);

        IReadOnlyList<MonitoringAssignment> assignments = await assignmentSource.GetDueAsync(
            timeProvider.GetUtcNow(),
            cancellationToken).ConfigureAwait(false);

        List<MonitoringCycleFailure> failures = [];
        int persistedCount = 0;
        foreach (MonitoringAssignment assignment in assignments)
        {
            try
            {
                HealthObservation observation = await probeHandler.ExecuteAsync(
                    new ProbeInstanceCommand(
                        assignment.InstanceId,
                        agentId,
                        assignment.Endpoint,
                        assignment.MonitoringCredentialReference,
                        assignment.Policy),
                    cancellationToken).ConfigureAwait(false);

                await observationSink.PersistAsync(observation, cancellationToken).ConfigureAwait(false);
                persistedCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                failures.Add(new MonitoringCycleFailure(assignment.InstanceId, "monitoring.assignment_failed"));
            }
        }

        return new MonitoringCycleResult(assignments.Count, persistedCount, failures);
    }
}
