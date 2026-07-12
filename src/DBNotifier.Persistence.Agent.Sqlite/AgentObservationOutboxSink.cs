// Module purpose: Implements Agent Observation Outbox Sink for the Agent-local SQLite boundary without exposing monitored database secrets.
using System.Text.Json;
using DBNotifier.Application.Monitoring;
using DBNotifier.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Agent.Sqlite;

public sealed class AgentObservationOutboxSink(
    IDbContextFactory<AgentDbContext> contextFactory,
    TimeProvider timeProvider) : IHealthObservationSink
{
    private const string OutboxSequenceStream = "outbox-sequence";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask PersistAsync(
        HealthObservation observation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observation);

        await using AgentDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        AgentCheckpointRow? checkpoint = await context.Checkpoints
            .SingleOrDefaultAsync(row => row.StreamName == OutboxSequenceStream, cancellationToken)
            .ConfigureAwait(false);

        DateTimeOffset createdAt = timeProvider.GetUtcNow();
        if (checkpoint is null)
        {
            checkpoint = new AgentCheckpointRow
            {
                StreamName = OutboxSequenceStream,
                Sequence = 0,
                UpdatedAt = createdAt,
            };
            context.Checkpoints.Add(checkpoint);
        }

        checkpoint.Sequence = checked(checkpoint.Sequence + 1);
        checkpoint.UpdatedAt = createdAt;

        context.HealthObservations.Add(ToRow(observation, createdAt));

        Guid messageId = Guid.NewGuid();
        context.OutboxMessages.Add(new AgentOutboxMessageRow
        {
            MessageId = messageId,
            Sequence = checkpoint.Sequence,
            MessageType = "health.observation.v1",
            SchemaVersion = 1,
            PayloadJson = SerializePayload(messageId, observation),
            OccurredAt = observation.ObservedAt,
            CreatedAt = createdAt,
            AvailableAt = createdAt,
            AttemptCount = 0,
        });

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static AgentHealthObservationRow ToRow(HealthObservation observation, DateTimeOffset createdAt) =>
        new()
        {
            ObservationId = observation.ObservationId,
            InstanceId = observation.InstanceId,
            ProviderType = observation.ProviderType.Value,
            ProviderVersion = observation.ProviderVersion,
            Status = observation.Status.ToString(),
            Method = observation.Method,
            EvidenceLevel = observation.Quality.EvidenceLevel.ToString(),
            ObservedAt = observation.ObservedAt,
            DurationMilliseconds = (long)observation.Duration.TotalMilliseconds,
            AttemptCount = observation.Quality.AttemptCount,
            ErrorCode = observation.Error?.Code,
            SafeErrorMessage = observation.Error?.SafeMessage,
            RedactedDetailsJson = JsonSerializer.Serialize(observation.Quality.Limitations, SerializerOptions),
            CreatedAt = createdAt,
        };

    private static string SerializePayload(Guid messageId, HealthObservation observation) =>
        JsonSerializer.Serialize(
            new ObservationOutboxPayload(
                messageId,
                1,
                observation.ObservationId,
                observation.InstanceId,
                observation.AgentId,
                observation.ProviderType.Value,
                observation.ProviderVersion,
                observation.Status.ToString(),
                observation.Method,
                observation.Quality.EvidenceLevel.ToString(),
                observation.Quality.AttemptCount,
                observation.ObservedAt,
                (long)observation.Duration.TotalMilliseconds,
                observation.Error?.Code,
                observation.Error?.SafeMessage,
                observation.Quality.Limitations),
            SerializerOptions);

    private sealed record ObservationOutboxPayload(
        Guid MessageId,
        int SchemaVersion,
        Guid ObservationId,
        Guid InstanceId,
        Guid AgentId,
        string ProviderType,
        string ProviderVersion,
        string Status,
        string Method,
        string EvidenceLevel,
        int AttemptCount,
        DateTimeOffset ObservedAt,
        long DurationMilliseconds,
        string? ErrorCode,
        string? SafeErrorMessage,
        IReadOnlyList<string> Limitations);
}
