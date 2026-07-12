// Module purpose: Implements Agent Retention Worker for the opt-in Agent runtime while preserving fail-closed defaults.
using DBNotifier.Application.Operations;
using DBNotifier.Persistence.Agent.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DBNotifier.Agent.Worker;

public sealed class AgentRetentionOptions
{
    public const string SectionName = "DBNotifier:Retention";

    public bool Enabled { get; set; }

    public bool ApplyChanges { get; set; }

    public int IntervalMinutes { get; set; } = 60;

    public int MaximumRowsPerClass { get; set; } = 1000;
}

public sealed partial class AgentRetentionWorker(
    AgentRetentionOptions options,
    AgentStoreInitializer initializer,
    IAgentRetentionStore store,
    TimeProvider timeProvider,
    ILogger<AgentRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            LogDisabled(logger);
            return;
        }

        Validate();
        await initializer.InitializeAsync(stoppingToken).ConfigureAwait(false);
        TimeSpan interval = TimeSpan.FromMinutes(options.IntervalMinutes);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                RetentionExecutionResult result = await store.ExecuteAsync(
                    new RetentionExecutionRequest(
                        timeProvider.GetUtcNow(),
                        options.MaximumRowsPerClass,
                        options.ApplyChanges),
                    stoppingToken).ConfigureAwait(false);
                LogCompleted(
                    logger,
                    options.ApplyChanges,
                    result.Observations,
                    result.AgentOutboxTombstones,
                    result.AgentInboxCommands);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                LogFailure(logger, exception.GetType().Name);
            }

            await Task.Delay(interval, timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }

    private void Validate()
    {
        if (options.IntervalMinutes is < 1 or > 1440 || options.MaximumRowsPerClass is < 1 or > 5000)
        {
            throw new InvalidOperationException("Agent retention cadence or batch size is outside policy.");
        }
    }

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "Agent retention is disabled by configuration")]
    private static partial void LogDisabled(ILogger logger);

    [LoggerMessage(
        EventId = 1202,
        Level = LogLevel.Information,
        Message = "Agent retention completed: applied={Applied}, observations={Observations}, outboxTombstones={OutboxTombstones}, inboxCommands={InboxCommands}")]
    private static partial void LogCompleted(
        ILogger logger,
        bool applied,
        int observations,
        int outboxTombstones,
        int inboxCommands);

    [LoggerMessage(
        EventId = 1203,
        Level = LogLevel.Warning,
        Message = "Agent retention failed safely: errorType={ErrorType}")]
    private static partial void LogFailure(ILogger logger, string errorType);
}
