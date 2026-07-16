// Module purpose: Coordinates bounded central maintenance while delivery paths remain explicitly fail-closed and external-action free.
using DBNotifier.Application.Operations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DBNotifier.Server.Api;

/// <summary>
/// Defines opt-in central maintenance settings and rejects delivery paths until their durable claim-and-lease
/// protocols are implemented; it grants no authority over monitored databases or external channels.
/// </summary>
public sealed class ServerOperationsOptions
{
    /// <summary>Gets the configuration-section path owned by central maintenance operations.</summary>
    public const string SectionName = "DBNotifier:Operations";

    /// <summary>Gets or sets whether bounded central retention inspection is enabled.</summary>
    public bool RetentionEnabled { get; set; }

    /// <summary>Gets or sets whether eligible central retention rows may be deleted.</summary>
    public bool RetentionApplyChanges { get; set; }

    /// <summary>Gets or sets whether the unavailable durable server-outbox delivery path was requested.</summary>
    public bool ServerOutboxEnabled { get; set; }

    /// <summary>Gets or sets whether the unavailable durable notification-delivery path was requested.</summary>
    public bool NotificationDeliveryEnabled { get; set; }

    /// <summary>Gets or sets the maintenance-cycle cadence in seconds.</summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>Gets or sets the maximum number of rows considered per maintenance class or delivery batch.</summary>
    public int MaximumBatchSize { get; set; } = 50;

    /// <summary>
    /// Validates enabled server operations before a background worker performs any persistence work.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when delivery is enabled without the required durable claim-and-lease contract or when
    /// cadence and batch settings are outside policy.
    /// </exception>
    public void ValidateForStartup()
    {
        if (ServerOutboxEnabled || NotificationDeliveryEnabled)
        {
            throw new InvalidOperationException("server.delivery_durable_lease_unavailable");
        }

        if (IntervalSeconds is < 1 or > 3600 || MaximumBatchSize is < 1 or > 100)
        {
            throw new InvalidOperationException("server.operations_policy_invalid");
        }
    }
}

/// <summary>
/// Provides a fail-closed publisher placeholder that performs no external action and returns a retryable outcome.
/// </summary>
public sealed class UnavailableServerMessagePublisher : IServerMessagePublisher
{
    /// <inheritdoc />
    public ValueTask<DeliveryResult> PublishAsync(
        ServerOutboxEnvelope message,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(new DeliveryResult(
            message.MessageId,
            DeliveryDisposition.Retryable,
            "server_outbox.publisher_not_configured"));
}

/// <summary>
/// Hosts bounded central retention cycles and refuses unavailable delivery protocols before entering its loop.
/// It accesses only DB-Notifier central persistence and never connects to a monitored database.
/// </summary>
/// <param name="options">Validated fail-closed maintenance and delivery settings.</param>
/// <param name="retentionStore">Central persistence boundary for bounded retention.</param>
/// <param name="outboxStore">Durable server-outbox boundary reserved for a future delivery protocol.</param>
/// <param name="notificationStore">Durable notification boundary reserved for future channel delivery.</param>
/// <param name="publisher">Fail-closed server-message publisher.</param>
/// <param name="adapters">Registered notification adapters; none are invoked while delivery remains blocked.</param>
/// <param name="timeProvider">Clock used for retention cut-offs and bounded cadence.</param>
/// <param name="logger">Structured logger that receives counts and stable error types only.</param>
public sealed partial class ServerMaintenanceWorker(
    ServerOperationsOptions options,
    IServerRetentionStore retentionStore,
    IServerOutboxStore outboxStore,
    INotificationDeliveryStore notificationStore,
    IServerMessagePublisher publisher,
    IEnumerable<INotificationChannelAdapter> adapters,
    TimeProvider timeProvider,
    ILogger<ServerMaintenanceWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.RetentionEnabled && !options.ServerOutboxEnabled && !options.NotificationDeliveryEnabled)
        {
            LogDisabled(logger);
            return;
        }

        options.ValidateForStartup();
        ServerOutboxDeliveryRunner outboxRunner = new(outboxStore, publisher, timeProvider);
        NotificationDeliveryRunner notificationRunner = new(notificationStore, adapters, timeProvider);
        TimeSpan interval = TimeSpan.FromSeconds(options.IntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.RetentionEnabled)
                {
                    RetentionExecutionResult retention = await retentionStore.ExecuteAsync(
                        new RetentionExecutionRequest(
                            timeProvider.GetUtcNow(),
                            options.MaximumBatchSize,
                            options.RetentionApplyChanges),
                        stoppingToken).ConfigureAwait(false);
                    LogRetention(
                        logger,
                        options.RetentionApplyChanges,
                        retention.Observations,
                        retention.Heartbeats,
                        retention.NotificationDeliveries,
                        retention.ServerOutboxTombstones);
                }

                if (options.ServerOutboxEnabled)
                {
                    DeliveryCycleResult outbox = await outboxRunner
                        .RunOnceAsync(options.MaximumBatchSize, stoppingToken)
                        .ConfigureAwait(false);
                    LogDelivery(logger, "server-outbox", outbox.Selected, outbox.Delivered, outbox.Retryable);
                }

                if (options.NotificationDeliveryEnabled)
                {
                    DeliveryCycleResult notifications = await notificationRunner
                        .RunOnceAsync(options.MaximumBatchSize, stoppingToken)
                        .ConfigureAwait(false);
                    LogDelivery(
                        logger,
                        "notifications",
                        notifications.Selected,
                        notifications.Delivered,
                        notifications.Retryable);
                }
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

    [LoggerMessage(EventId = 3101, Level = LogLevel.Information, Message = "Server maintenance and delivery are disabled by configuration")]
    private static partial void LogDisabled(ILogger logger);

    [LoggerMessage(
        EventId = 3102,
        Level = LogLevel.Information,
        Message = "Server retention completed: applied={Applied}, observations={Observations}, heartbeats={Heartbeats}, deliveries={Deliveries}, outboxTombstones={OutboxTombstones}")]
    private static partial void LogRetention(
        ILogger logger,
        bool applied,
        int observations,
        int heartbeats,
        int deliveries,
        int outboxTombstones);

    [LoggerMessage(
        EventId = 3103,
        Level = LogLevel.Information,
        Message = "Server delivery completed: stream={Stream}, selected={Selected}, delivered={Delivered}, retryable={Retryable}")]
    private static partial void LogDelivery(
        ILogger logger,
        string stream,
        int selected,
        int delivered,
        int retryable);

    [LoggerMessage(
        EventId = 3104,
        Level = LogLevel.Warning,
        Message = "Server maintenance cycle failed safely: errorType={ErrorType}")]
    private static partial void LogFailure(ILogger logger, string errorType);
}
