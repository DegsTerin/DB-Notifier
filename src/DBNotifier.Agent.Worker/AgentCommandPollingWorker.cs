// Module purpose: Implements Agent Command Polling Worker for the opt-in Agent runtime while preserving fail-closed defaults.
using DBNotifier.Application.Synchronization;
using DBNotifier.Persistence.Agent.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DBNotifier.Agent.Worker;

/// <summary>
/// Hosts opt-in command polling but refuses startup whenever the required durable polling and acknowledgement
/// protocol is unavailable; it never executes an administrative command itself.
/// </summary>
/// <param name="agentOptions">Validated identity and runtime options for the local Agent.</param>
/// <param name="synchronizationOptions">Fail-closed synchronisation and command-polling options.</param>
/// <param name="storeInitializer">Initialiser for the authorised Agent-local SQLite store.</param>
/// <param name="inboxStore">Durable local inbox boundary used only after protocol validation.</param>
/// <param name="transport">Authenticated command-delivery transport boundary.</param>
/// <param name="timeProvider">Clock used for bounded polling cadence.</param>
/// <param name="logger">Structured logger that receives no command payload or credential material.</param>
public sealed partial class AgentCommandPollingWorker(
    AgentWorkerOptions agentOptions,
    AgentSynchronizationOptions synchronizationOptions,
    AgentStoreInitializer storeInitializer,
    IAgentCommandInboxStore inboxStore,
    ICommandDeliveryTransport transport,
    TimeProvider timeProvider,
    ILogger<AgentCommandPollingWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!synchronizationOptions.Enabled || !synchronizationOptions.CommandPollingEnabled)
        {
            LogDisabled(logger);
            return;
        }

        synchronizationOptions.ValidateCommandPollingForStartup();
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
