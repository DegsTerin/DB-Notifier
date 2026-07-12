using DBNotifier.Application.Synchronization;
using DBNotifier.Persistence.Agent.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DBNotifier.Agent.Worker;

public sealed partial class AgentCommandPollingWorker(
    AgentWorkerOptions agentOptions,
    AgentSynchronizationOptions synchronizationOptions,
    AgentStoreInitializer storeInitializer,
    IAgentCommandInboxStore inboxStore,
    ICommandDeliveryTransport transport,
    TimeProvider timeProvider,
    ILogger<AgentCommandPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!synchronizationOptions.Enabled || !synchronizationOptions.CommandPollingEnabled)
        {
            LogDisabled(logger);
            return;
        }

        _ = synchronizationOptions.ValidateAndGetServerBaseAddress();
        await storeInitializer.InitializeAsync(stoppingToken).ConfigureAwait(false);
        CommandDeliveryRunner runner = new(agentOptions.AgentId, synchronizationOptions.AgentVersion,
            synchronizationOptions.ProviderVersions, inboxStore, transport, timeProvider);
        TimeSpan interval = TimeSpan.FromSeconds(synchronizationOptions.CommandPollIntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CommandDeliveryCycleResult result = await runner
                    .RunOnceAsync(synchronizationOptions.MaximumBatchSize, stoppingToken).ConfigureAwait(false);
                if (result.DeliveredCount > 0)
                {
                    LogCompleted(logger, result.DeliveredCount, result.AcknowledgedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                LogFailed(logger, exception.GetType().Name);
            }

            await Task.Delay(interval, timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "Administrative command polling is disabled")]
    private static partial void LogDisabled(ILogger logger);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Information,
        Message = "Command delivery completed without execution: delivered={Delivered}, acknowledged={Acknowledged}")]
    private static partial void LogCompleted(ILogger logger, int delivered, int acknowledged);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Warning,
        Message = "Command polling failed safely: errorType={ErrorType}")]
    private static partial void LogFailed(ILogger logger, string errorType);
}
