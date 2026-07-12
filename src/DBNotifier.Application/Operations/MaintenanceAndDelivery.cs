// Module purpose: Defines Maintenance And Delivery application behaviour without depending on concrete providers or user interfaces.
namespace DBNotifier.Application.Operations;

public sealed record RetentionExecutionRequest(
    DateTimeOffset Now,
    int MaximumRowsPerClass,
    bool ApplyChanges);

public sealed record RetentionExecutionResult(
    int Observations,
    int AgentOutboxTombstones,
    int AgentInboxCommands,
    int Heartbeats,
    int NotificationDeliveries,
    int ServerOutboxTombstones);

public interface IAgentRetentionStore
{
    ValueTask<RetentionExecutionResult> ExecuteAsync(
        RetentionExecutionRequest request,
        CancellationToken cancellationToken);
}

public interface IServerRetentionStore
{
    ValueTask<RetentionExecutionResult> ExecuteAsync(
        RetentionExecutionRequest request,
        CancellationToken cancellationToken);
}

public enum DeliveryDisposition
{
    Delivered,
    Retryable,
}

public sealed record DeliveryResult(
    Guid ItemId,
    DeliveryDisposition Disposition,
    string? ErrorCode = null);

public sealed record ServerOutboxEnvelope(
    Guid MessageId,
    string MessageType,
    int SchemaVersion,
    string PayloadJson,
    DateTimeOffset OccurredAt,
    int AttemptCount);

public interface IServerOutboxStore
{
    ValueTask<IReadOnlyList<ServerOutboxEnvelope>> GetPendingAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken);

    ValueTask ApplyResultsAsync(
        IReadOnlyList<DeliveryResult> results,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public interface IServerMessagePublisher
{
    ValueTask<DeliveryResult> PublishAsync(
        ServerOutboxEnvelope message,
        CancellationToken cancellationToken);
}

public sealed record NotificationEnvelope(
    Guid NotificationDeliveryId,
    Guid NotificationChannelId,
    Guid EventId,
    string ChannelType,
    string NonSecretConfigurationJson,
    string EventType,
    string Severity,
    string EventDetailsJson,
    int AttemptCount);

public interface INotificationDeliveryStore
{
    ValueTask<IReadOnlyList<NotificationEnvelope>> GetPendingAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken);

    ValueTask ApplyResultsAsync(
        IReadOnlyList<DeliveryResult> results,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public interface INotificationChannelAdapter
{
    string ChannelType { get; }

    ValueTask<DeliveryResult> DeliverAsync(
        NotificationEnvelope notification,
        CancellationToken cancellationToken);
}

public sealed record DeliveryCycleResult(int Selected, int Delivered, int Retryable);

public sealed class ServerOutboxDeliveryRunner(
    IServerOutboxStore store,
    IServerMessagePublisher publisher,
    TimeProvider timeProvider)
{
    public async ValueTask<DeliveryCycleResult> RunOnceAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        ValidateMaximumCount(maximumCount);
        DateTimeOffset now = timeProvider.GetUtcNow();
        IReadOnlyList<ServerOutboxEnvelope> pending = await store
            .GetPendingAsync(now, maximumCount, cancellationToken)
            .ConfigureAwait(false);
        List<DeliveryResult> results = new(pending.Count);
        foreach (ServerOutboxEnvelope message in pending)
        {
            try
            {
                DeliveryResult result = await publisher
                    .PublishAsync(message, cancellationToken)
                    .ConfigureAwait(false);
                results.Add(result.ItemId == message.MessageId
                    ? result
                    : new DeliveryResult(
                        message.MessageId,
                        DeliveryDisposition.Retryable,
                        "server_outbox.result_mismatch"));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                results.Add(new DeliveryResult(
                    message.MessageId,
                    DeliveryDisposition.Retryable,
                    "server_outbox.publisher_unavailable"));
            }
        }

        if (results.Count > 0)
        {
            await store.ApplyResultsAsync(results, timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        }

        return Summarize(results);
    }

    private static void ValidateMaximumCount(int maximumCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumCount, 100);
    }

    private static DeliveryCycleResult Summarize(List<DeliveryResult> results)
    {
        int delivered = results.Count(result => result.Disposition == DeliveryDisposition.Delivered);
        return new DeliveryCycleResult(results.Count, delivered, results.Count - delivered);
    }
}

public sealed class NotificationDeliveryRunner
{
    private readonly INotificationDeliveryStore store;
    private readonly Dictionary<string, INotificationChannelAdapter> adapters;
    private readonly TimeProvider timeProvider;

    public NotificationDeliveryRunner(
        INotificationDeliveryStore store,
        IEnumerable<INotificationChannelAdapter> adapters,
        TimeProvider timeProvider)
    {
        this.store = store;
        this.timeProvider = timeProvider;
        this.adapters = adapters.ToDictionary(adapter => adapter.ChannelType, StringComparer.OrdinalIgnoreCase);
    }

    public async ValueTask<DeliveryCycleResult> RunOnceAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumCount, 100);
        DateTimeOffset now = timeProvider.GetUtcNow();
        IReadOnlyList<NotificationEnvelope> pending = await store
            .GetPendingAsync(now, maximumCount, cancellationToken)
            .ConfigureAwait(false);
        List<DeliveryResult> results = new(pending.Count);
        foreach (NotificationEnvelope notification in pending)
        {
            if (!adapters.TryGetValue(notification.ChannelType, out INotificationChannelAdapter? adapter))
            {
                results.Add(new DeliveryResult(
                    notification.NotificationDeliveryId,
                    DeliveryDisposition.Retryable,
                    "notification.adapter_unavailable"));
                continue;
            }

            try
            {
                DeliveryResult result = await adapter
                    .DeliverAsync(notification, cancellationToken)
                    .ConfigureAwait(false);
                results.Add(result.ItemId == notification.NotificationDeliveryId
                    ? result
                    : new DeliveryResult(
                        notification.NotificationDeliveryId,
                        DeliveryDisposition.Retryable,
                        "notification.result_mismatch"));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                results.Add(new DeliveryResult(
                    notification.NotificationDeliveryId,
                    DeliveryDisposition.Retryable,
                    "notification.adapter_unavailable"));
            }
        }

        if (results.Count > 0)
        {
            await store.ApplyResultsAsync(results, timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        }

        int delivered = results.Count(result => result.Disposition == DeliveryDisposition.Delivered);
        return new DeliveryCycleResult(results.Count, delivered, results.Count - delivered);
    }
}
