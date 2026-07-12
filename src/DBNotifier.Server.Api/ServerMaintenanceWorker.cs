using DBNotifier.Application.Operations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DBNotifier.Server.Api;

public sealed class ServerOperationsOptions
{
    public const string SectionName = "DBNotifier:Operations";

    public bool RetentionEnabled { get; set; }

    public bool RetentionApplyChanges { get; set; }

    public bool ServerOutboxEnabled { get; set; }

    public bool NotificationDeliveryEnabled { get; set; }

    public int IntervalSeconds { get; set; } = 30;

    public int MaximumBatchSize { get; set; } = 50;
}

public sealed class UnavailableServerMessagePublisher : IServerMessagePublisher
{
    public ValueTask<DeliveryResult> PublishAsync(
        ServerOutboxEnvelope message,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(new DeliveryResult(
            message.MessageId,
            DeliveryDisposition.Retryable,
            "server_outbox.publisher_not_configured"));
}

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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.RetentionEnabled && !options.ServerOutboxEnabled && !options.NotificationDeliveryEnabled)
        {
            LogDisabled(logger);
            return;
        }

        Validate();
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

    private void Validate()
    {
        if (options.IntervalSeconds is < 1 or > 3600 || options.MaximumBatchSize is < 1 or > 100)
        {
            throw new InvalidOperationException("Server operations cadence or batch size is outside policy.");
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
