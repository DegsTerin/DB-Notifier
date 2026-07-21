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
    Ambiguous,
}

public sealed record DeliveryResult(
    Guid ItemId,
    DeliveryDisposition Disposition,
    string? ErrorCode = null);

/// <summary>Identifies one database-owned delivery attempt and the fence required to mutate it.</summary>
/// <param name="ItemId">Durable outbox or notification identifier.</param>
/// <param name="OwnerId">Unique worker identity that owns the current lease.</param>
/// <param name="FenceToken">Positive monotonic fence assigned by central persistence.</param>
/// <param name="LeaseExpiresAt">Database-clock expiry of the current ownership lease.</param>
/// <param name="AttemptNumber">One-based durable attempt number assigned before any hand-off.</param>
public sealed record DeliveryOwnership(
    Guid ItemId,
    Guid OwnerId,
    long FenceToken,
    DateTimeOffset LeaseExpiresAt,
    int AttemptNumber);

/// <summary>Requests a bounded atomic claim for one uniquely identified delivery worker.</summary>
/// <param name="OwnerId">Unique non-empty worker identity.</param>
/// <param name="MaximumCount">Maximum number of items to claim.</param>
/// <param name="LeaseDuration">Positive ownership interval interpreted against the database clock.</param>
public sealed record DeliveryClaimRequest(Guid OwnerId, int MaximumCount, TimeSpan LeaseDuration);

/// <summary>Combines one fenced ownership token with the typed outcome to persist.</summary>
/// <param name="Ownership">Exact ownership returned by the claim operation.</param>
/// <param name="Disposition">Confirmed, retryable or ambiguous hand-off outcome.</param>
/// <param name="ErrorCode">Optional stable non-secret diagnostic code.</param>
public sealed record DeliveryCompletion(
    DeliveryOwnership Ownership,
    DeliveryDisposition Disposition,
    string? ErrorCode = null);

/// <summary>Summarises durable completion transitions and rejected stale ownership attempts.</summary>
/// <param name="Delivered">Items durably confirmed as delivered.</param>
/// <param name="RetryScheduled">Items returned to a bounded retry schedule.</param>
/// <param name="DeadLettered">Items retained after exhausting the attempt budget.</param>
/// <param name="Ambiguous">Items retained without automatic replay after an uncertain hand-off.</param>
/// <param name="Rejected">Results rejected because their owner or fence was stale.</param>
public sealed record DeliveryCompletionSummary(
    int Delivered,
    int RetryScheduled,
    int DeadLettered,
    int Ambiguous,
    int Rejected);

/// <summary>Defines invariant bounds shared by provider-neutral runners and the central ownership store.</summary>
public static class DeliveryOwnershipPolicy
{
    /// <summary>Gets the maximum durable hand-off attempts before dead-letter retention.</summary>
    public const int MaximumAttempts = 5;

    /// <summary>Gets the minimum accepted lease duration.</summary>
    public static TimeSpan MinimumLeaseDuration { get; } = TimeSpan.FromSeconds(1);

    /// <summary>Gets the maximum accepted lease duration.</summary>
    public static TimeSpan MaximumLeaseDuration { get; } = TimeSpan.FromMinutes(5);

    /// <summary>Validates one claim request before persistence work begins.</summary>
    /// <param name="request">Claim owner, batch bound and lease interval.</param>
    /// <exception cref="ArgumentException">Thrown when the owner identity is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when batch or lease bounds are invalid.</exception>
    public static void Validate(DeliveryClaimRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfEqual(request.OwnerId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.MaximumCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.MaximumCount, 100);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.LeaseDuration, MinimumLeaseDuration);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.LeaseDuration, MaximumLeaseDuration);
    }
}

public sealed record ServerOutboxEnvelope(
    Guid MessageId,
    string MessageType,
    int SchemaVersion,
    string PayloadJson,
    DateTimeOffset OccurredAt,
    string IdempotencyKey,
    DeliveryOwnership Ownership);

public interface IServerOutboxStore
{
    /// <summary>Atomically claims due outbox messages under database-clock leases.</summary>
    /// <param name="request">Bounded owner and lease request.</param>
    /// <param name="cancellationToken">Cancellation propagated to central persistence.</param>
    /// <returns>Messages owned by the exact returned fences.</returns>
    ValueTask<IReadOnlyList<ServerOutboxEnvelope>> ClaimAsync(
        DeliveryClaimRequest request,
        CancellationToken cancellationToken);

    /// <summary>Durably marks that an external hand-off is about to begin for one exact fence.</summary>
    /// <param name="ownership">Claim identity that must still be current and unexpired.</param>
    /// <param name="cancellationToken">Cancellation propagated to central persistence.</param>
    /// <returns><see langword="true"/> only when the hand-off marker was accepted.</returns>
    ValueTask<bool> BeginHandoffAsync(
        DeliveryOwnership ownership,
        CancellationToken cancellationToken);

    /// <summary>Applies outcomes only for exact current fences and retains terminal evidence.</summary>
    /// <param name="completions">Bounded completion set produced by one runner cycle.</param>
    /// <param name="cancellationToken">Cancellation propagated to central persistence.</param>
    /// <returns>Durable transition counts including rejected stale fences.</returns>
    ValueTask<DeliveryCompletionSummary> CompleteAsync(
        IReadOnlyList<DeliveryCompletion> completions,
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
    string IdempotencyKey,
    DeliveryOwnership Ownership);

public interface INotificationDeliveryStore
{
    /// <summary>Atomically claims due notification deliveries whose exact binding remains eligible.</summary>
    /// <param name="request">Bounded owner and lease request.</param>
    /// <param name="cancellationToken">Cancellation propagated to central persistence.</param>
    /// <returns>Notifications owned by the exact returned fences.</returns>
    ValueTask<IReadOnlyList<NotificationEnvelope>> ClaimAsync(
        DeliveryClaimRequest request,
        CancellationToken cancellationToken);

    /// <summary>Durably marks that a channel hand-off is about to begin for one exact fence.</summary>
    /// <param name="ownership">Claim identity that must still be current and unexpired.</param>
    /// <param name="cancellationToken">Cancellation propagated to central persistence.</param>
    /// <returns><see langword="true"/> only when the hand-off marker was accepted.</returns>
    ValueTask<bool> BeginHandoffAsync(
        DeliveryOwnership ownership,
        CancellationToken cancellationToken);

    /// <summary>Applies outcomes only for exact current fences and retains terminal evidence.</summary>
    /// <param name="completions">Bounded completion set produced by one runner cycle.</param>
    /// <param name="cancellationToken">Cancellation propagated to central persistence.</param>
    /// <returns>Durable transition counts including rejected stale fences.</returns>
    ValueTask<DeliveryCompletionSummary> CompleteAsync(
        IReadOnlyList<DeliveryCompletion> completions,
        CancellationToken cancellationToken);
}

public interface INotificationChannelAdapter
{
    string ChannelType { get; }

    ValueTask<DeliveryResult> DeliverAsync(
        NotificationEnvelope notification,
        CancellationToken cancellationToken);
}

/// <summary>Summarises one bounded claimed-delivery cycle.</summary>
/// <param name="Selected">Items atomically claimed for the worker.</param>
/// <param name="Delivered">Items durably confirmed as delivered.</param>
/// <param name="Retryable">Items durably scheduled for retry.</param>
/// <param name="DeadLettered">Items retained after exhausting the attempt budget.</param>
/// <param name="Ambiguous">Items retained without replay after uncertain hand-off.</param>
/// <param name="LostOwnership">Hand-offs or results rejected under a stale fence.</param>
public sealed record DeliveryCycleResult(
    int Selected,
    int Delivered,
    int Retryable,
    int DeadLettered = 0,
    int Ambiguous = 0,
    int LostOwnership = 0);

public sealed class ServerOutboxDeliveryRunner(
    IServerOutboxStore store,
    IServerMessagePublisher publisher,
    Guid workerId,
    TimeSpan leaseDuration)
{
    /// <summary>Claims and hands off one bounded outbox batch under exact durable fences.</summary>
    /// <param name="maximumCount">Maximum number of items between one and 100.</param>
    /// <param name="cancellationToken">Caller cancellation propagated through claim and hand-off.</param>
    /// <returns>Durable outcomes observed for the claimed batch.</returns>
    public async ValueTask<DeliveryCycleResult> RunOnceAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        DeliveryClaimRequest request = new(workerId, maximumCount, leaseDuration);
        DeliveryOwnershipPolicy.Validate(request);
        IReadOnlyList<ServerOutboxEnvelope> claimed = await store
            .ClaimAsync(request, cancellationToken)
            .ConfigureAwait(false);
        List<DeliveryCompletion> completions = new(claimed.Count);
        int lostOwnership = 0;
        foreach (ServerOutboxEnvelope message in claimed)
        {
            if (!await store.BeginHandoffAsync(message.Ownership, cancellationToken).ConfigureAwait(false))
            {
                lostOwnership++;
                continue;
            }

            try
            {
                DeliveryResult result = await publisher
                    .PublishAsync(message, cancellationToken)
                    .ConfigureAwait(false);
                completions.Add(result.ItemId == message.MessageId
                    ? new DeliveryCompletion(message.Ownership, result.Disposition, result.ErrorCode)
                    : new DeliveryCompletion(
                        message.Ownership,
                        DeliveryDisposition.Ambiguous,
                        "server_outbox.result_mismatch"));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The durable hand-off marker makes later lease expiry ambiguous; never convert cancellation
                // after that boundary into an automatic retry.
                throw;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                completions.Add(new DeliveryCompletion(
                    message.Ownership,
                    DeliveryDisposition.Ambiguous,
                    "server_outbox.publisher_unavailable"));
            }
        }

        DeliveryCompletionSummary summary = completions.Count == 0
            ? new DeliveryCompletionSummary(0, 0, 0, 0, 0)
            : await store.CompleteAsync(completions, cancellationToken).ConfigureAwait(false);
        return new DeliveryCycleResult(
            claimed.Count,
            summary.Delivered,
            summary.RetryScheduled,
            summary.DeadLettered,
            summary.Ambiguous,
            lostOwnership + summary.Rejected);
    }
}

public sealed class NotificationDeliveryRunner
{
    private readonly INotificationDeliveryStore store;
    private readonly Dictionary<string, INotificationChannelAdapter> adapters;
    private readonly Guid workerId;
    private readonly TimeSpan leaseDuration;

    public NotificationDeliveryRunner(
        INotificationDeliveryStore store,
        IEnumerable<INotificationChannelAdapter> adapters,
        Guid workerId,
        TimeSpan leaseDuration)
    {
        this.store = store;
        this.workerId = workerId;
        this.leaseDuration = leaseDuration;
        this.adapters = adapters.ToDictionary(adapter => adapter.ChannelType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Claims and hands off one bounded notification batch under exact durable fences.</summary>
    /// <param name="maximumCount">Maximum number of items between one and 100.</param>
    /// <param name="cancellationToken">Caller cancellation propagated through claim and hand-off.</param>
    /// <returns>Durable outcomes observed for the claimed batch.</returns>
    public async ValueTask<DeliveryCycleResult> RunOnceAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        DeliveryClaimRequest request = new(workerId, maximumCount, leaseDuration);
        DeliveryOwnershipPolicy.Validate(request);
        IReadOnlyList<NotificationEnvelope> claimed = await store
            .ClaimAsync(request, cancellationToken)
            .ConfigureAwait(false);
        List<DeliveryCompletion> completions = new(claimed.Count);
        int lostOwnership = 0;
        foreach (NotificationEnvelope notification in claimed)
        {
            if (!adapters.TryGetValue(notification.ChannelType, out INotificationChannelAdapter? adapter))
            {
                completions.Add(new DeliveryCompletion(
                    notification.Ownership,
                    DeliveryDisposition.Retryable,
                    "notification.adapter_unavailable"));
                continue;
            }

            if (!await store.BeginHandoffAsync(notification.Ownership, cancellationToken).ConfigureAwait(false))
            {
                lostOwnership++;
                continue;
            }

            try
            {
                DeliveryResult result = await adapter
                    .DeliverAsync(notification, cancellationToken)
                    .ConfigureAwait(false);
                completions.Add(result.ItemId == notification.NotificationDeliveryId
                    ? new DeliveryCompletion(notification.Ownership, result.Disposition, result.ErrorCode)
                    : new DeliveryCompletion(
                        notification.Ownership,
                        DeliveryDisposition.Ambiguous,
                        "notification.result_mismatch"));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The durable hand-off marker makes later lease expiry ambiguous; never convert cancellation
                // after that boundary into an automatic retry.
                throw;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                completions.Add(new DeliveryCompletion(
                    notification.Ownership,
                    DeliveryDisposition.Ambiguous,
                    "notification.adapter_unavailable"));
            }
        }

        DeliveryCompletionSummary summary = completions.Count == 0
            ? new DeliveryCompletionSummary(0, 0, 0, 0, 0)
            : await store.CompleteAsync(completions, cancellationToken).ConfigureAwait(false);
        return new DeliveryCycleResult(
            claimed.Count,
            summary.Delivered,
            summary.RetryScheduled,
            summary.DeadLettered,
            summary.Ambiguous,
            lostOwnership + summary.Rejected);
    }
}
