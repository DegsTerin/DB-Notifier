using System.Text.Json;
using DBNotifier.Application.Synchronization;
using DBNotifier.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Server.PostgreSql;

public sealed class ServerObservationIngestionStore(
    IDbContextFactory<ServerDbContext> contextFactory) : IObservationIngestionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<ObservationItemResult> IngestAsync(
        ObservationSyncMessage message,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        bool activeAgent = await context.Agents
            .AnyAsync(row => row.AgentId == message.AgentId && row.State == "Active", cancellationToken)
            .ConfigureAwait(false);
        if (!activeAgent)
        {
            return Rejected(message.MessageId, "agent.not_active");
        }

        bool assignedInstance = await context.Instances
            .AnyAsync(
                row => row.InstanceId == message.InstanceId &&
                    row.AssignedAgentId == message.AgentId &&
                    row.Enabled && row.ArchivedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (!assignedInstance)
        {
            return Rejected(message.MessageId, "observation.instance_not_assigned");
        }

        ObservationItemResult? existingResult = await ClassifyExistingAsync(
            context,
            message,
            cancellationToken).ConfigureAwait(false);
        if (existingResult is not null)
        {
            return existingResult;
        }

        HealthStatus currentStatus = Enum.Parse<HealthStatus>(message.Status, ignoreCase: false);
        string? previousStatusValue = await context.HealthSamples
            .Where(row => row.InstanceId == message.InstanceId)
            .OrderByDescending(row => row.Sequence)
            .Select(row => row.Status)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        HealthStatus? previousStatus = Enum.TryParse(previousStatusValue, out HealthStatus parsedPrevious)
            ? parsedPrevious
            : null;

        context.HealthSamples.Add(ToHealthSample(message, receivedAt));
        CanonicalEventCandidate? candidate = ObservationEventDeriver.Derive(previousStatus, currentStatus);
        if (candidate is not null)
        {
            await AddEventAndAlertDeliveriesAsync(
                context,
                message,
                candidate,
                receivedAt,
                cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new ObservationItemResult(message.MessageId, ObservationIngestionDisposition.Accepted);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            await using ServerDbContext verification = await contextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);
            ObservationItemResult? concurrentResult = await ClassifyExistingAsync(
                verification,
                message,
                cancellationToken).ConfigureAwait(false);
            return concurrentResult ?? new ObservationItemResult(
                message.MessageId,
                ObservationIngestionDisposition.Retryable,
                "ingestion.persistence_failed");
        }
    }

    private static async ValueTask<ObservationItemResult?> ClassifyExistingAsync(
        ServerDbContext context,
        ObservationSyncMessage message,
        CancellationToken cancellationToken)
    {
        var existing = await context.HealthSamples
            .AsNoTracking()
            .Where(row =>
                row.MessageId == message.MessageId ||
                row.ObservationId == message.ObservationId ||
                (row.AgentId == message.AgentId && row.Sequence == message.Sequence))
            .Select(row => new { row.MessageId, row.ObservationId, row.AgentId, row.Sequence })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return null;
        }

        return existing.MessageId == message.MessageId || existing.ObservationId == message.ObservationId
            ? new ObservationItemResult(message.MessageId, ObservationIngestionDisposition.Duplicate)
            : Rejected(message.MessageId, "observation.sequence_conflict");
    }

    public async ValueTask<long> GetHighestContiguousSequenceAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        long[] sequences = await context.HealthSamples
            .AsNoTracking()
            .Where(row => row.AgentId == agentId)
            .OrderBy(row => row.Sequence)
            .Select(row => row.Sequence)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        long contiguous = 0;
        foreach (long sequence in sequences)
        {
            if (sequence == contiguous + 1)
            {
                contiguous = sequence;
            }
            else if (sequence > contiguous + 1)
            {
                break;
            }
        }

        return contiguous;
    }

    private static HealthSampleRow ToHealthSample(ObservationSyncMessage message, DateTimeOffset receivedAt) =>
        new()
        {
            ObservationId = message.ObservationId,
            InstanceId = message.InstanceId,
            AgentId = message.AgentId,
            MessageId = message.MessageId,
            Sequence = message.Sequence,
            ProviderType = message.ProviderType,
            ProviderVersion = message.ProviderVersion,
            Status = message.Status,
            Method = message.Method,
            EvidenceLevel = message.EvidenceLevel,
            ObservedAt = message.ObservedAt,
            ReceivedAt = receivedAt,
            DurationMilliseconds = message.DurationMilliseconds,
            ErrorCode = message.ErrorCode,
            RedactedDetailsJson = JsonSerializer.Serialize(
                new { message.SafeErrorMessage, message.Limitations },
                SerializerOptions),
        };

    private static async ValueTask AddEventAndAlertDeliveriesAsync(
        ServerDbContext context,
        ObservationSyncMessage message,
        CanonicalEventCandidate candidate,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        Guid eventId = Guid.NewGuid();
        EventRecordRow eventRow = new()
        {
            EventId = eventId,
            InstanceId = message.InstanceId,
            AgentId = message.AgentId,
            SourceObservationId = message.ObservationId,
            CorrelationId = message.MessageId,
            EventType = candidate.EventType,
            Severity = candidate.Severity,
            ObservedAt = message.ObservedAt,
            ReceivedAt = receivedAt,
            DetailsJson = JsonSerializer.Serialize(
                new { previousStateChanged = true, currentStatus = message.Status },
                SerializerOptions),
        };
        context.Events.Add(eventRow);
        context.OutboxMessages.Add(new ServerOutboxMessageRow
        {
            MessageId = eventId,
            MessageType = "canonical.event.v1",
            SchemaVersion = 1,
            PayloadJson = JsonSerializer.Serialize(new
            {
                eventRow.EventId,
                eventRow.EventType,
                eventRow.Severity,
                eventRow.InstanceId,
                eventRow.AgentId,
                eventRow.SourceObservationId,
                eventRow.CorrelationId,
                eventRow.ObservedAt,
                eventRow.ReceivedAt,
            }, SerializerOptions),
            OccurredAt = message.ObservedAt,
            CreatedAt = receivedAt,
            AvailableAt = receivedAt,
            AttemptCount = 0,
        });

        AlertRuleRow[] rules = await context.AlertRules
            .AsNoTracking()
            .Where(row => row.Enabled && row.ArchivedAt == null && row.RuleType == "CanonicalEvent")
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!rules.Any(rule => Matches(rule, candidate.EventType, message.InstanceId)))
        {
            return;
        }

        Guid[] channelIds = await context.NotificationChannels
            .AsNoTracking()
            .Where(row => row.Enabled)
            .Select(row => row.NotificationChannelId)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        context.NotificationDeliveries.AddRange(channelIds.Select(channelId => new NotificationDeliveryRow
        {
            NotificationDeliveryId = Guid.NewGuid(),
            NotificationChannelId = channelId,
            EventId = eventId,
            State = "Pending",
            AttemptCount = 0,
            CreatedAt = receivedAt,
        }));
    }

    private static bool Matches(AlertRuleRow rule, string eventType, Guid instanceId)
    {
        try
        {
            using JsonDocument configuration = JsonDocument.Parse(rule.ConfigurationJson);
            if (!configuration.RootElement.TryGetProperty("eventTypes", out JsonElement eventTypes) ||
                eventTypes.ValueKind != JsonValueKind.Array ||
                !eventTypes.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.String &&
                    string.Equals(item.GetString(), eventType, StringComparison.Ordinal)))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(rule.InstanceScopeJson))
            {
                return true;
            }

            using JsonDocument scope = JsonDocument.Parse(rule.InstanceScopeJson);
            return scope.RootElement.TryGetProperty("instanceIds", out JsonElement instanceIds) &&
                instanceIds.ValueKind == JsonValueKind.Array &&
                instanceIds.EnumerateArray().Any(item =>
                    item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out Guid value) &&
                    value == instanceId);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static ObservationItemResult Rejected(Guid messageId, string errorCode) =>
        new(messageId, ObservationIngestionDisposition.Rejected, errorCode);
}
