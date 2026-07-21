// Module purpose: Implements Agent Monitoring Worker for the opt-in Agent runtime while preserving fail-closed defaults.
using DBNotifier.Application.Monitoring;
using DBNotifier.Persistence.Agent.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DBNotifier.Agent.Worker;

public sealed partial class AgentMonitoringWorker(
    AgentWorkerOptions options,
    AgentStoreInitializer storeInitializer,
    IMonitoringAssignmentSource assignmentSource,
    ProbeInstanceHandler probeHandler,
    IHealthObservationSink observationSink,
    TimeProvider timeProvider,
    ILogger<AgentMonitoringWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.MonitoringEnabled)
        {
            LogMonitoringDisabled(logger);
            return;
        }

        ValidateOptions();
        await storeInitializer.InitializeAsync(stoppingToken).ConfigureAwait(false);
        MonitoringCycleRunner runner = new(
            options.AgentId,
            assignmentSource,
            probeHandler,
            observationSink,
            timeProvider);
        TimeSpan cycleInterval = TimeSpan.FromSeconds(options.CycleIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTimeOffset startedAt = timeProvider.GetUtcNow();
            MonitoringCycleResult result = await runner.RunOnceAsync(stoppingToken).ConfigureAwait(false);
            TimeSpan duration = timeProvider.GetUtcNow() - startedAt;
            if (logger.IsEnabled(LogLevel.Information))
            {
                LogCycleCompleted(
                    logger,
                    result.DueCount,
                    result.PersistedCount,
                    result.Failures.Count,
                    duration);
            }
            foreach (MonitoringCycleFailure failure in result.Failures)
            {
                LogAssignmentFailure(logger, failure.InstanceId, failure.Code);
            }
            if (result.CycleFailureCode is { } cycleFailureCode)
            {
                LogCycleFailure(logger, cycleFailureCode);
            }

            await Task.Delay(cycleInterval, timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }

    private void ValidateOptions()
    {
        if (options.AgentId == Guid.Empty)
        {
            throw new InvalidOperationException("A non-empty AgentId is required when monitoring is enabled.");
        }

        if (options.CycleIntervalSeconds is < 1 or > 300)
        {
            throw new InvalidOperationException("Agent cycle interval must be between 1 and 300 seconds.");
        }
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Agent monitoring is disabled by configuration")]
    private static partial void LogMonitoringDisabled(ILogger logger);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Monitoring cycle completed: due={DueCount}, persisted={PersistedCount}, failures={FailureCount}, duration={Duration}",
        SkipEnabledCheck = true)]
    private static partial void LogCycleCompleted(
        ILogger logger,
        int dueCount,
        int persistedCount,
        int failureCount,
        TimeSpan duration);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Monitoring assignment failed: instanceId={InstanceId}, code={ErrorCode}")]
    private static partial void LogAssignmentFailure(ILogger logger, Guid instanceId, string errorCode);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Warning,
        Message = "Monitoring cycle source failed: code={ErrorCode}")]
    private static partial void LogCycleFailure(ILogger logger, string errorCode);
}
