// Module purpose: Implements Agent Retention Worker for the opt-in Agent runtime while preserving fail-closed defaults.
using DBNotifier.Application.Operations;
using DBNotifier.Persistence.Agent.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DBNotifier.Agent.Worker;

/// <summary>Defines opt-in, dry-run-by-default retention policy for local Agent state.</summary>
public sealed class AgentRetentionOptions
{
    /// <summary>Gets the configuration section containing local retention options.</summary>
    public const string SectionName = "DBNotifier:Retention";

    /// <summary>Gets or sets whether the retention worker may initialise and execute.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets whether eligible rows may be deleted instead of reported only.</summary>
    public bool ApplyChanges { get; set; }

    /// <summary>Gets or sets the bounded retention cadence in minutes.</summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>Gets or sets the maximum rows processed per retained data class.</summary>
    public int MaximumRowsPerClass { get; set; } = 1000;

    /// <summary>Validates enabled retention bounds before the host is built.</summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when enabled cadence or batch size lies outside the local policy.
    /// </exception>
    public void ValidateForStartup()
    {
        if (Enabled && (IntervalMinutes is < 1 or > 1440 || MaximumRowsPerClass is < 1 or > 5000))
        {
            throw new InvalidOperationException("Agent retention cadence or batch size is outside policy.");
        }
    }
}

/// <summary>
/// Applies or previews bounded local retention only after the opt-in configuration and store are valid.
/// </summary>
/// <param name="options">Opt-in retention policy.</param>
/// <param name="initializer">Lazy owner of the Agent SQLite schema.</param>
/// <param name="store">Bounded local retention store.</param>
/// <param name="timeProvider">Clock used for retention cut-offs and cadence.</param>
/// <param name="logger">Sanitised worker logger.</param>
public sealed partial class AgentRetentionWorker(
    AgentRetentionOptions options,
    AgentStoreInitializer initializer,
    IAgentRetentionStore store,
    TimeProvider timeProvider,
    ILogger<AgentRetentionWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            LogDisabled(logger);
            return;
        }

        options.ValidateForStartup();
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
