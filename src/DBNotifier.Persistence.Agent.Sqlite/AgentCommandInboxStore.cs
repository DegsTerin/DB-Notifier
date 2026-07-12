// Module purpose: Implements Agent Command Inbox Store for the Agent-local SQLite boundary without exposing monitored database secrets.
using DBNotifier.Application.Synchronization;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Agent.Sqlite;

public sealed class AgentCommandInboxStore(IDbContextFactory<AgentDbContext> contextFactory)
    : IAgentCommandInboxStore
{
    public async ValueTask<long> NextSequenceAsync(CancellationToken cancellationToken)
    {
        await using AgentDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        AgentCheckpointRow? checkpoint = await context.Checkpoints
            .SingleOrDefaultAsync(row => row.StreamName == "command-protocol", cancellationToken)
            .ConfigureAwait(false);
        if (checkpoint is null)
        {
            checkpoint = new AgentCheckpointRow
            {
                StreamName = "command-protocol",
                Sequence = 1,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            context.Checkpoints.Add(checkpoint);
        }
        else
        {
            checkpoint.Sequence++;
            checkpoint.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return checkpoint.Sequence;
    }

    public async ValueTask<IReadOnlyList<CommandAcknowledgement>> ReceiveAsync(
        IReadOnlyList<CommandEnvelope> commands,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(commands);
        if (commands.Count > 100)
        {
            throw new ArgumentException("Command batches are limited to 100 items.", nameof(commands));
        }

        await using AgentDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        List<CommandAcknowledgement> acknowledgements = new(commands.Count);
        foreach (CommandEnvelope command in commands)
        {
            Validate(command);
            if (command.ExpiresAt <= receivedAt)
            {
                continue;
            }

            AgentInboxCommandRow? existing = await context.InboxCommands
                .SingleOrDefaultAsync(row => row.CommandId == command.CommandId ||
                    row.IdempotencyKey == command.IdempotencyKey, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                if (!Matches(existing, command))
                {
                    throw new InvalidOperationException("A conflicting command replay was rejected.");
                }

                acknowledgements.Add(new(command.CommandId, command.IdempotencyKey, receivedAt));
                continue;
            }

            context.InboxCommands.Add(new AgentInboxCommandRow
            {
                CommandId = command.CommandId,
                IdempotencyKey = command.IdempotencyKey,
                InstanceId = command.InstanceId,
                ProviderId = command.ProviderId,
                CapabilityId = command.CapabilityId,
                TypedParametersJson = command.TypedParametersJson,
                State = "Available",
                RequestedAt = command.RequestedAt,
                ExpiresAt = command.ExpiresAt,
                ExpectedAgentVersion = command.ExpectedAgentVersion,
                ExpectedProviderVersion = command.ExpectedProviderVersion,
                ConcurrencyToken = Guid.NewGuid(),
            });
            acknowledgements.Add(new(command.CommandId, command.IdempotencyKey, receivedAt));
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return acknowledgements;
    }

    public async ValueTask MarkAcknowledgedAsync(
        IReadOnlyList<CommandAcknowledgementResult> results,
        DateTimeOffset acknowledgedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(results);
        await using AgentDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (CommandAcknowledgementResult result in results.Where(result =>
            result.Disposition is CommandAcknowledgementDisposition.Accepted or CommandAcknowledgementDisposition.Duplicate))
        {
            AgentInboxCommandRow? command = await context.InboxCommands
                .SingleOrDefaultAsync(row => row.CommandId == result.CommandId, cancellationToken)
                .ConfigureAwait(false);
            if (command is not null && command.State is "Available" or "Acknowledged")
            {
                command.State = "Acknowledged";
                command.AcknowledgedAt ??= acknowledgedAt;
                command.ConcurrencyToken = Guid.NewGuid();
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void Validate(CommandEnvelope command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.CommandId == Guid.Empty || command.InstanceId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.IdempotencyKey) || string.IsNullOrWhiteSpace(command.ProviderId) ||
            string.IsNullOrWhiteSpace(command.CapabilityId) || string.IsNullOrWhiteSpace(command.ExpectedAgentVersion) ||
            string.IsNullOrWhiteSpace(command.ExpectedProviderVersion) ||
            string.IsNullOrWhiteSpace(command.TypedParametersJson) || command.IdempotencyKey.Length > 200 ||
            command.ProviderId.Length > 64 || command.CapabilityId.Length > 100 ||
            command.ExpectedAgentVersion.Length > 64 || command.ExpectedProviderVersion.Length > 64 ||
            command.TypedParametersJson.Length > 64 * 1024 || command.ExpiresAt <= command.RequestedAt)
        {
            throw new ArgumentException("The delivered command is invalid.", nameof(command));
        }
    }

    private static bool Matches(AgentInboxCommandRow existing, CommandEnvelope command) =>
        existing.CommandId == command.CommandId &&
        string.Equals(existing.IdempotencyKey, command.IdempotencyKey, StringComparison.Ordinal) &&
        existing.InstanceId == command.InstanceId &&
        string.Equals(existing.ProviderId, command.ProviderId, StringComparison.Ordinal) &&
        string.Equals(existing.CapabilityId, command.CapabilityId, StringComparison.Ordinal) &&
        string.Equals(existing.TypedParametersJson, command.TypedParametersJson, StringComparison.Ordinal) &&
        existing.RequestedAt == command.RequestedAt && existing.ExpiresAt == command.ExpiresAt &&
        string.Equals(existing.ExpectedAgentVersion, command.ExpectedAgentVersion, StringComparison.Ordinal) &&
        string.Equals(existing.ExpectedProviderVersion, command.ExpectedProviderVersion, StringComparison.Ordinal);
}
