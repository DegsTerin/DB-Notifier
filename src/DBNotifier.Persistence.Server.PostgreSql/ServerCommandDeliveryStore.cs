// Module purpose: Implements Server Command Delivery Store for central PostgreSQL persistence with transactional and authorisation boundaries.
using DBNotifier.Application.Synchronization;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Server.PostgreSql;

public sealed class ServerCommandDeliveryStore(IDbContextFactory<ServerDbContext> contextFactory)
    : IServerCommandDeliveryStore
{
    public async ValueTask<IReadOnlyList<CommandEnvelope>> PollAsync(
        CommandPollRequest request,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        Validate(request);
        await using ServerDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        AdministrativeCommandRow[] candidates = await context.AdministrativeCommands
            .Where(row => row.AssignedAgentId == request.AgentId &&
                row.ExpectedAgentVersion == request.AgentVersion &&
                (row.State == "Pending" || row.State == "Available"))
            .Take(request.MaximumCount * 2)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        candidates = [.. candidates.OrderBy(row => row.RequestedAt)];
        List<CommandEnvelope> deliverable = new(request.MaximumCount);
        foreach (AdministrativeCommandRow command in candidates)
        {
            if (command.ExpiresAt <= receivedAt)
            {
                command.State = "Expired";
                command.ConcurrencyToken = Guid.NewGuid();
                continue;
            }

            DatabaseInstanceRow? instance = await context.Instances
                .AsNoTracking()
                .SingleOrDefaultAsync(row => row.InstanceId == command.InstanceId, cancellationToken)
                .ConfigureAwait(false);
            bool providerCompatible = instance is not null &&
                request.ProviderVersions.TryGetValue(instance.ProviderType, out string? providerVersion) &&
                string.Equals(providerVersion, command.ExpectedProviderVersion, StringComparison.Ordinal);
            if (!string.Equals(request.AgentVersion, command.ExpectedAgentVersion, StringComparison.Ordinal) ||
                !providerCompatible)
            {
                continue;
            }

            command.State = "Available";
            command.ConcurrencyToken = Guid.NewGuid();
            deliverable.Add(ToEnvelope(command, instance!.ProviderType));
            if (deliverable.Count == request.MaximumCount)
            {
                break;
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return deliverable;
    }

    public async ValueTask<IReadOnlyList<CommandAcknowledgementResult>> AcknowledgeAsync(
        CommandAcknowledgementRequest request,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        Validate(request);
        await using ServerDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        List<CommandAcknowledgementResult> results = new(request.Items.Count);
        foreach (CommandAcknowledgement acknowledgement in request.Items)
        {
            AdministrativeCommandRow? command = await context.AdministrativeCommands
                .SingleOrDefaultAsync(row => row.CommandId == acknowledgement.CommandId &&
                    row.AssignedAgentId == request.AgentId, cancellationToken)
                .ConfigureAwait(false);
            if (command is null || !string.Equals(command.IdempotencyKey, acknowledgement.IdempotencyKey, StringComparison.Ordinal))
            {
                results.Add(new(acknowledgement.CommandId, CommandAcknowledgementDisposition.Rejected, "command.ack_unknown"));
                continue;
            }

            if (command.State == "Acknowledged")
            {
                results.Add(new(command.CommandId, CommandAcknowledgementDisposition.Duplicate));
                continue;
            }

            if (command.ExpiresAt <= receivedAt)
            {
                command.State = "Expired";
                command.ConcurrencyToken = Guid.NewGuid();
                results.Add(new(command.CommandId, CommandAcknowledgementDisposition.Rejected, "command.expired"));
                continue;
            }

            if (command.State != "Available")
            {
                results.Add(new(command.CommandId, CommandAcknowledgementDisposition.Rejected, "command.ack_state_invalid"));
                continue;
            }

            command.State = "Acknowledged";
            command.ConcurrencyToken = Guid.NewGuid();
            results.Add(new(command.CommandId, CommandAcknowledgementDisposition.Accepted));
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return results;
    }

    private static void Validate(CommandPollRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MessageId == Guid.Empty || request.AgentId == Guid.Empty || request.SchemaVersion != 1 ||
            request.Sequence < 0 || request.MaximumCount is < 1 or > 100 ||
            string.IsNullOrWhiteSpace(request.AgentVersion) || request.AgentVersion.Length > 64 ||
            request.ProviderVersions is null || request.ProviderVersions.Count > 128 ||
            request.ProviderVersions.Any(item => string.IsNullOrWhiteSpace(item.Key) || item.Key.Length > 64 ||
                string.IsNullOrWhiteSpace(item.Value) || item.Value.Length > 64))
        {
            throw new ArgumentException("The command poll request is invalid.", nameof(request));
        }
    }

    private static void Validate(CommandAcknowledgementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MessageId == Guid.Empty || request.AgentId == Guid.Empty || request.SchemaVersion != 1 ||
            request.Sequence < 0 || request.Items is null || request.Items.Count is < 1 or > 100 ||
            request.Items.Any(item => item.CommandId == Guid.Empty || string.IsNullOrWhiteSpace(item.IdempotencyKey) ||
                item.IdempotencyKey.Length > 200))
        {
            throw new ArgumentException("The command acknowledgement request is invalid.", nameof(request));
        }
    }

    private static CommandEnvelope ToEnvelope(AdministrativeCommandRow command, string providerId) => new(
        command.CommandId,
        command.IdempotencyKey,
        command.InstanceId,
        providerId,
        command.CapabilityId,
        command.TypedParametersJson,
        command.RequestedAt,
        command.ExpiresAt,
        command.ExpectedAgentVersion,
        command.ExpectedProviderVersion);
}
