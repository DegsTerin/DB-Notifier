// Module purpose: Persists serialised Agent observations and outbox messages atomically in the local SQLite boundary.
using System.Text.Json;
using DBNotifier.Application.Monitoring;
using DBNotifier.Domain;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Agent.Sqlite;

/// <summary>
/// Persists one health observation and its ordered Agent outbox message atomically while serialising
/// the singleton sink's SQLite write transaction so concurrent probes cannot reuse a checkpoint sequence.
/// </summary>
/// <param name="contextFactory">Factory for isolated Agent-local persistence contexts.</param>
/// <param name="timeProvider">Clock used for persistence and delivery-availability timestamps.</param>
public sealed class AgentObservationOutboxSink(
    IDbContextFactory<AgentDbContext> contextFactory,
    TimeProvider timeProvider) : IHealthObservationSink, IDisposable
{
    private const string OutboxSequenceStream = "outbox-sequence";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim persistenceGate = new(1, 1);

    /// <summary>
    /// Atomically stores an observation and the next monotonic outbox message.
    /// </summary>
    /// <param name="observation">Validated provider-neutral observation to persist.</param>
    /// <param name="cancellationToken">Cancellation propagated from the monitoring cycle.</param>
    /// <returns>A task that completes only after the SQLite transaction commits.</returns>
    public async ValueTask PersistAsync(
        HealthObservation observation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observation);

        await persistenceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using AgentDbContext context = await contextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);
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
        finally
        {
            persistenceGate.Release();
        }
    }

    /// <summary>Releases the in-process persistence gate when the Agent host shuts down.</summary>
    public void Dispose() => persistenceGate.Dispose();

    /// <summary>Maps one provider-neutral observation to its sanitised Agent-local persistence row.</summary>
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

    /// <summary>Serialises the exact bounded observation envelope later sent through the Agent outbox.</summary>
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
