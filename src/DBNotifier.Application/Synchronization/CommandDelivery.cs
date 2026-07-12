// Module purpose: Defines Command Delivery application behaviour without depending on concrete providers or user interfaces.
namespace DBNotifier.Application.Synchronization;

public sealed record CommandPollRequest(
    Guid MessageId,
    int SchemaVersion,
    Guid AgentId,
    long Sequence,
    DateTimeOffset OccurredAt,
    DateTimeOffset SentAt,
    string AgentVersion,
    IReadOnlyDictionary<string, string> ProviderVersions,
    int MaximumCount);

public sealed record CommandEnvelope(
    Guid CommandId,
    string IdempotencyKey,
    Guid InstanceId,
    string ProviderId,
    string CapabilityId,
    string TypedParametersJson,
    DateTimeOffset RequestedAt,
    DateTimeOffset ExpiresAt,
    string ExpectedAgentVersion,
    string ExpectedProviderVersion);

public sealed record CommandPollResponse(
    Guid MessageId,
    int SchemaVersion,
    Guid AgentId,
    long Sequence,
    DateTimeOffset OccurredAt,
    DateTimeOffset SentAt,
    IReadOnlyList<CommandEnvelope> Commands);

public sealed record CommandAcknowledgement(
    Guid CommandId,
    string IdempotencyKey,
    DateTimeOffset AcknowledgedAt);

public sealed record CommandAcknowledgementRequest(
    Guid MessageId,
    int SchemaVersion,
    Guid AgentId,
    long Sequence,
    DateTimeOffset OccurredAt,
    DateTimeOffset SentAt,
    IReadOnlyList<CommandAcknowledgement> Items);

public enum CommandAcknowledgementDisposition
{
    Accepted,
    Duplicate,
    Rejected,
}

public sealed record CommandAcknowledgementResult(
    Guid CommandId,
    CommandAcknowledgementDisposition Disposition,
    string? ErrorCode = null);

public sealed record CommandAcknowledgementResponse(
    IReadOnlyList<CommandAcknowledgementResult> Items);

public interface IServerCommandDeliveryStore
{
    ValueTask<IReadOnlyList<CommandEnvelope>> PollAsync(
        CommandPollRequest request,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<CommandAcknowledgementResult>> AcknowledgeAsync(
        CommandAcknowledgementRequest request,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);
}

public interface IAgentCommandInboxStore
{
    ValueTask<long> NextSequenceAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<CommandAcknowledgement>> ReceiveAsync(
        IReadOnlyList<CommandEnvelope> commands,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);

    ValueTask MarkAcknowledgedAsync(
        IReadOnlyList<CommandAcknowledgementResult> results,
        DateTimeOffset acknowledgedAt,
        CancellationToken cancellationToken);
}

public interface ICommandDeliveryTransport
{
    ValueTask<CommandPollResponse> PollAsync(CommandPollRequest request, CancellationToken cancellationToken);

    ValueTask<CommandAcknowledgementResponse> AcknowledgeAsync(
        CommandAcknowledgementRequest request,
        CancellationToken cancellationToken);
}

public sealed record CommandDeliveryCycleResult(int DeliveredCount, int AcknowledgedCount);

public sealed class CommandDeliveryRunner(
    Guid agentId,
    string agentVersion,
    IReadOnlyDictionary<string, string> providerVersions,
    IAgentCommandInboxStore inboxStore,
    ICommandDeliveryTransport transport,
    TimeProvider timeProvider)
{
    public async ValueTask<CommandDeliveryCycleResult> RunOnceAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentVersion);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumCount, 100);
        DateTimeOffset now = timeProvider.GetUtcNow();
        long pollSequence = await inboxStore.NextSequenceAsync(cancellationToken).ConfigureAwait(false);
        CommandPollResponse response = await transport.PollAsync(new CommandPollRequest(
            Guid.NewGuid(), 1, agentId, pollSequence, now, timeProvider.GetUtcNow(), agentVersion,
            providerVersions, maximumCount), cancellationToken).ConfigureAwait(false);
        if (response.MessageId == Guid.Empty || response.SchemaVersion != 1 || response.AgentId != agentId ||
            response.Sequence != pollSequence || response.Commands is null ||
            response.Commands.Count > maximumCount || response.Commands.Any(command =>
                !string.Equals(command.ExpectedAgentVersion, agentVersion, StringComparison.Ordinal) ||
                !providerVersions.TryGetValue(command.ProviderId, out string? providerVersion) ||
                !string.Equals(command.ExpectedProviderVersion, providerVersion, StringComparison.Ordinal)))
        {
            throw new InvalidDataException("The command poll response is invalid.");
        }

        IReadOnlyList<CommandAcknowledgement> acknowledgements = await inboxStore
            .ReceiveAsync(response.Commands, timeProvider.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);
        if (acknowledgements.Count == 0)
        {
            return new(response.Commands.Count, 0);
        }

        long ackSequence = await inboxStore.NextSequenceAsync(cancellationToken).ConfigureAwait(false);
        DateTimeOffset ackNow = timeProvider.GetUtcNow();
        CommandAcknowledgementResponse acknowledged = await transport.AcknowledgeAsync(
            new CommandAcknowledgementRequest(Guid.NewGuid(), 1, agentId, ackSequence, ackNow,
                timeProvider.GetUtcNow(), acknowledgements), cancellationToken).ConfigureAwait(false);
        HashSet<Guid> expectedIds = acknowledgements.Select(item => item.CommandId).ToHashSet();
        if (acknowledged.Items is null || acknowledged.Items.Count > acknowledgements.Count ||
            acknowledged.Items.Select(item => item.CommandId).Distinct().Count() != acknowledged.Items.Count ||
            acknowledged.Items.Any(item => !expectedIds.Contains(item.CommandId)))
        {
            throw new InvalidDataException("The command acknowledgement response is invalid.");
        }

        await inboxStore.MarkAcknowledgedAsync(acknowledged.Items, timeProvider.GetUtcNow(), cancellationToken)
            .ConfigureAwait(false);
        return new(response.Commands.Count, acknowledged.Items.Count(item =>
            item.Disposition is CommandAcknowledgementDisposition.Accepted or CommandAcknowledgementDisposition.Duplicate));
    }
}
