// Module purpose: Implements Agent Outbox Dispatch Worker for the opt-in Agent runtime while preserving fail-closed defaults.
using DBNotifier.Application.Synchronization;
using DBNotifier.Persistence.Agent.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DBNotifier.Agent.Worker;

/// <summary>
/// Dispatches the durable Agent observation outbox only through the explicitly enabled synchronisation boundary.
/// </summary>
/// <param name="agentOptions">Agent identity and monitoring options.</param>
/// <param name="synchronizationOptions">Independent outbound synchronisation options.</param>
/// <param name="storeInitializer">Lazy owner of the Agent SQLite schema.</param>
/// <param name="outboxStore">Durable ordered observation source.</param>
/// <param name="transport">Authenticated bounded observation transport.</param>
/// <param name="timeProvider">Clock used for retry cadence.</param>
/// <param name="logger">Sanitised worker logger.</param>
public sealed partial class AgentOutboxDispatchWorker(
    AgentWorkerOptions agentOptions,
    AgentSynchronizationOptions synchronizationOptions,
    AgentStoreInitializer storeInitializer,
    IAgentOutboxStore outboxStore,
    IObservationBatchTransport transport,
    TimeProvider timeProvider,
    ILogger<AgentOutboxDispatchWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!synchronizationOptions.Enabled)
        {
            LogSynchronizationDisabled(logger);
            return;
        }

        agentOptions.ValidateForStartup(synchronisationEnabled: true);
        _ = synchronizationOptions.ValidateAndGetServerBaseAddress();
        await storeInitializer.InitializeAsync(stoppingToken).ConfigureAwait(false);
        AgentOutboxDispatchRunner runner = new(
            agentOptions.AgentId,
            outboxStore,
            transport,
            timeProvider);
        TimeSpan interval = TimeSpan.FromSeconds(synchronizationOptions.DispatchIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                AgentOutboxDispatchResult result = await runner
                    .RunOnceAsync(synchronizationOptions.MaximumBatchSize, stoppingToken)
                    .ConfigureAwait(false);
                if (result.PendingCount > 0)
                {
                    LogDispatchCompleted(
                        logger,
                        result.PendingCount,
                        result.AcknowledgedCount,
                        result.RetryableCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                LogDispatchFailed(logger, exception.GetType().Name);
            }

            await Task.Delay(interval, timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Agent synchronization is disabled by configuration")]
    private static partial void LogSynchronizationDisabled(ILogger logger);

    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Information,
        Message = "Outbox dispatch completed: pending={PendingCount}, acknowledged={AcknowledgedCount}, retryable={RetryableCount}")]
    private static partial void LogDispatchCompleted(
        ILogger logger,
        int pendingCount,
        int acknowledgedCount,
        int retryableCount);

    [LoggerMessage(
        EventId = 1103,
        Level = LogLevel.Warning,
        Message = "Outbox dispatch failed safely: errorType={ErrorType}")]
    private static partial void LogDispatchFailed(ILogger logger, string errorType);
}
