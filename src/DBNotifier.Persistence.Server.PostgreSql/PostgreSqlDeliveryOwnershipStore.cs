// Module purpose: Owns PostgreSQL-specific atomic delivery claims, leases, fences and terminal evidence.
using System.Data;
using DBNotifier.Application.Operations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace DBNotifier.Persistence.Server.PostgreSql;

/// <summary>
/// Implements durable ownership for Server outbox messages and bound notification deliveries. Provider-specific
/// concurrency SQL remains isolated here; no method performs an external side effect or accesses a monitored database.
/// </summary>
/// <param name="contextFactory">Factory for central PostgreSQL contexts.</param>
public sealed class PostgreSqlDeliveryOwnershipStore(IDbContextFactory<ServerDbContext> contextFactory) :
    IServerOutboxStore,
    INotificationDeliveryStore
{
    private const int CommandTimeoutSeconds = 10;

    /// <inheritdoc />
    async ValueTask<IReadOnlyList<ServerOutboxEnvelope>> IServerOutboxStore.ClaimAsync(
        DeliveryClaimRequest request,
        CancellationToken cancellationToken)
    {
        DeliveryOwnershipPolicy.Validate(request);
        await using ServerDbContext context = await CreatePostgreSqlContextAsync(cancellationToken).ConfigureAwait(false);
        NpgsqlConnection connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
        await using NpgsqlTransaction transaction = await connection
            .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            .ConfigureAwait(false);

        await ReconcileExpiredOutboxAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
        await using NpgsqlCommand command = new(OutboxClaimSql, connection, transaction)
        {
            CommandTimeout = CommandTimeoutSeconds,
        };
        AddClaimParameters(command, request);
        List<ServerOutboxEnvelope> claimed = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            Guid itemId = reader.GetGuid(0);
            Guid ownerId = reader.GetGuid(6);
            claimed.Add(new ServerOutboxEnvelope(
                itemId,
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                $"server-outbox:{itemId:N}",
                new DeliveryOwnership(
                    itemId,
                    ownerId,
                    reader.GetInt64(7),
                    reader.GetFieldValue<DateTimeOffset>(8),
                    reader.GetInt32(5))));
        }

        await reader.DisposeAsync().ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return claimed;
    }

    /// <inheritdoc />
    async ValueTask<IReadOnlyList<NotificationEnvelope>> INotificationDeliveryStore.ClaimAsync(
        DeliveryClaimRequest request,
        CancellationToken cancellationToken)
    {
        DeliveryOwnershipPolicy.Validate(request);
        await using ServerDbContext context = await CreatePostgreSqlContextAsync(cancellationToken).ConfigureAwait(false);
        NpgsqlConnection connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
        await using NpgsqlTransaction transaction = await connection
            .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            .ConfigureAwait(false);

        await ReconcileExpiredNotificationsAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
        await using NpgsqlCommand command = new(NotificationClaimSql, connection, transaction)
        {
            CommandTimeout = CommandTimeoutSeconds,
        };
        AddClaimParameters(command, request);
        List<NotificationEnvelope> claimed = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            Guid itemId = reader.GetGuid(0);
            Guid ownerId = reader.GetGuid(10);
            claimed.Add(new NotificationEnvelope(
                itemId,
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                new DeliveryOwnership(
                    itemId,
                    ownerId,
                    reader.GetInt64(11),
                    reader.GetFieldValue<DateTimeOffset>(12),
                    reader.GetInt32(9))));
        }

        await reader.DisposeAsync().ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return claimed;
    }

    /// <inheritdoc />
    ValueTask<bool> IServerOutboxStore.BeginHandoffAsync(
        DeliveryOwnership ownership,
        CancellationToken cancellationToken) =>
        BeginHandoffAsync(OutboxBeginHandoffSql, ownership, cancellationToken);

    /// <inheritdoc />
    ValueTask<bool> INotificationDeliveryStore.BeginHandoffAsync(
        DeliveryOwnership ownership,
        CancellationToken cancellationToken) =>
        BeginHandoffAsync(NotificationBeginHandoffSql, ownership, cancellationToken);

    /// <inheritdoc />
    ValueTask<DeliveryCompletionSummary> IServerOutboxStore.CompleteAsync(
        IReadOnlyList<DeliveryCompletion> completions,
        CancellationToken cancellationToken) =>
        CompleteAsync(OutboxCompletionSql, completions, cancellationToken);

    /// <inheritdoc />
    ValueTask<DeliveryCompletionSummary> INotificationDeliveryStore.CompleteAsync(
        IReadOnlyList<DeliveryCompletion> completions,
        CancellationToken cancellationToken) =>
        CompleteAsync(NotificationCompletionSql, completions, cancellationToken);

    /// <summary>Marks one exact unexpired claim immediately before its potentially side-effecting call.</summary>
    private async ValueTask<bool> BeginHandoffAsync(
        string sql,
        DeliveryOwnership ownership,
        CancellationToken cancellationToken)
    {
        ValidateOwnership(ownership);
        await using ServerDbContext context = await CreatePostgreSqlContextAsync(cancellationToken).ConfigureAwait(false);
        NpgsqlConnection connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
        await using NpgsqlCommand command = new(sql, connection)
        {
            CommandTimeout = CommandTimeoutSeconds,
        };
        AddOwnershipParameters(command, ownership);
        object? applied = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return applied is not null;
    }

    /// <summary>Applies a bounded completion batch in one transaction and rejects every stale fence.</summary>
    private async ValueTask<DeliveryCompletionSummary> CompleteAsync(
        string sql,
        IReadOnlyList<DeliveryCompletion> completions,
        CancellationToken cancellationToken)
    {
        ValidateCompletions(completions);
        await using ServerDbContext context = await CreatePostgreSqlContextAsync(cancellationToken).ConfigureAwait(false);
        NpgsqlConnection connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken).ConfigureAwait(false);
        await using NpgsqlTransaction transaction = await connection
            .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            .ConfigureAwait(false);
        int delivered = 0;
        int retryScheduled = 0;
        int deadLettered = 0;
        int ambiguous = 0;
        int rejected = 0;
        foreach (DeliveryCompletion completion in completions)
        {
            await using NpgsqlCommand command = new(sql, connection, transaction)
            {
                CommandTimeout = CommandTimeoutSeconds,
            };
            AddOwnershipParameters(command, completion.Ownership);
            command.Parameters.AddWithValue("disposition", NpgsqlDbType.Text, completion.Disposition.ToString());
            command.Parameters.AddWithValue("error_code", NpgsqlDbType.Text, BoundErrorCode(completion));
            command.Parameters.AddWithValue(
                "maximum_attempts",
                NpgsqlDbType.Integer,
                DeliveryOwnershipPolicy.MaximumAttempts);
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            switch (result as string)
            {
                case "Delivered":
                    delivered++;
                    break;
                case "Pending":
                    retryScheduled++;
                    break;
                case "DeadLettered":
                    deadLettered++;
                    break;
                case "Ambiguous":
                    ambiguous++;
                    break;
                default:
                    rejected++;
                    break;
            }
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new DeliveryCompletionSummary(delivered, retryScheduled, deadLettered, ambiguous, rejected);
    }

    /// <summary>Converts expired post-hand-off leases to ambiguous and exhausted pre-hand-off leases to dead-letter.</summary>
    private static async ValueTask ReconcileExpiredOutboxAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = new(OutboxReconciliationSql, connection, transaction)
        {
            CommandTimeout = CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue(
            "maximum_attempts",
            NpgsqlDbType.Integer,
            DeliveryOwnershipPolicy.MaximumAttempts);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Converts expired post-hand-off leases to ambiguous and exhausted pre-hand-off leases to dead-letter.</summary>
    private static async ValueTask ReconcileExpiredNotificationsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = new(NotificationReconciliationSql, connection, transaction)
        {
            CommandTimeout = CommandTimeoutSeconds,
        };
        command.Parameters.AddWithValue(
            "maximum_attempts",
            NpgsqlDbType.Integer,
            DeliveryOwnershipPolicy.MaximumAttempts);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a central context and refuses every provider except Npgsql before SQL execution.</summary>
    private async ValueTask<ServerDbContext> CreatePostgreSqlContextAsync(CancellationToken cancellationToken)
    {
        ServerDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (context.Database.GetDbConnection() is not NpgsqlConnection)
        {
            await context.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException("server.delivery_postgresql_required");
        }

        return context;
    }

    /// <summary>Opens the central connection only when the context factory did not already open it.</summary>
    private static async ValueTask EnsureOpenAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Adds bounded claim parameters without interpolating worker-controlled values into SQL.</summary>
    private static void AddClaimParameters(NpgsqlCommand command, DeliveryClaimRequest request)
    {
        command.Parameters.AddWithValue("owner_id", NpgsqlDbType.Uuid, request.OwnerId);
        command.Parameters.AddWithValue("lease_duration", NpgsqlDbType.Interval, request.LeaseDuration);
        command.Parameters.AddWithValue("maximum_count", NpgsqlDbType.Integer, request.MaximumCount);
        command.Parameters.AddWithValue(
            "maximum_attempts",
            NpgsqlDbType.Integer,
            DeliveryOwnershipPolicy.MaximumAttempts);
    }

    /// <summary>Adds exact owner and fence values used by hand-off and completion mutations.</summary>
    private static void AddOwnershipParameters(NpgsqlCommand command, DeliveryOwnership ownership)
    {
        command.Parameters.AddWithValue("item_id", NpgsqlDbType.Uuid, ownership.ItemId);
        command.Parameters.AddWithValue("owner_id", NpgsqlDbType.Uuid, ownership.OwnerId);
        command.Parameters.AddWithValue("fence_token", NpgsqlDbType.Bigint, ownership.FenceToken);
    }

    /// <summary>Validates one ownership token before contacting central persistence.</summary>
    private static void ValidateOwnership(DeliveryOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentOutOfRangeException.ThrowIfEqual(ownership.ItemId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(ownership.OwnerId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfLessThan(ownership.FenceToken, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(ownership.AttemptNumber, 1);
    }

    /// <summary>Validates a bounded completion set and rejects duplicate item identities.</summary>
    private static void ValidateCompletions(IReadOnlyList<DeliveryCompletion> completions)
    {
        ArgumentNullException.ThrowIfNull(completions);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(completions.Count, 100);
        HashSet<Guid> itemIds = [];
        foreach (DeliveryCompletion completion in completions)
        {
            ArgumentNullException.ThrowIfNull(completion);
            ValidateOwnership(completion.Ownership);
            if (!itemIds.Add(completion.Ownership.ItemId))
            {
                throw new ArgumentException("delivery.completion_duplicate", nameof(completions));
            }
        }
    }

    /// <summary>Normalises error evidence without exposing exception messages or unbounded adapter content.</summary>
    private static string BoundErrorCode(DeliveryCompletion completion)
    {
        string fallback = completion.Disposition switch
        {
            DeliveryDisposition.Ambiguous => "delivery.handoff_outcome_ambiguous",
            DeliveryDisposition.Retryable => "delivery.retryable",
            _ => "delivery.none",
        };
        string code = string.IsNullOrWhiteSpace(completion.ErrorCode) ? fallback : completion.ErrorCode;
        return code[..Math.Min(code.Length, 100)];
    }

    private const string OutboxReconciliationSql = """
        UPDATE outbox_messages
        SET ambiguous_at = clock_timestamp(),
            error_code = 'delivery.handoff_outcome_ambiguous',
            lease_owner_id = NULL,
            lease_expires_at = NULL,
            handoff_started_at = NULL
        WHERE published_at IS NULL
          AND dead_lettered_at IS NULL
          AND ambiguous_at IS NULL
          AND lease_expires_at <= clock_timestamp()
          AND handoff_started_at IS NOT NULL;

        UPDATE outbox_messages
        SET dead_lettered_at = clock_timestamp(),
            error_code = 'delivery.attempts_exhausted',
            lease_owner_id = NULL,
            lease_expires_at = NULL,
            handoff_started_at = NULL
        WHERE published_at IS NULL
          AND dead_lettered_at IS NULL
          AND ambiguous_at IS NULL
          AND attempt_count >= @maximum_attempts
          AND (lease_owner_id IS NULL OR lease_expires_at <= clock_timestamp())
          AND handoff_started_at IS NULL;
        """;

    private const string NotificationReconciliationSql = """
        UPDATE notification_deliveries
        SET state = 'Ambiguous',
            ambiguous_at = clock_timestamp(),
            error_code = 'delivery.handoff_outcome_ambiguous',
            lease_owner_id = NULL,
            lease_expires_at = NULL,
            handoff_started_at = NULL
        WHERE state = 'Delivering'
          AND lease_expires_at <= clock_timestamp()
          AND handoff_started_at IS NOT NULL;

        UPDATE notification_deliveries
        SET state = 'DeadLettered',
            dead_lettered_at = clock_timestamp(),
            error_code = 'delivery.attempts_exhausted',
            lease_owner_id = NULL,
            lease_expires_at = NULL,
            handoff_started_at = NULL
        WHERE state IN ('Pending','Delivering')
          AND attempt_count >= @maximum_attempts
          AND (lease_owner_id IS NULL OR lease_expires_at <= clock_timestamp())
          AND handoff_started_at IS NULL;
        """;

    private const string OutboxClaimSql = """
        WITH candidates AS (
            SELECT message_id
            FROM outbox_messages
            WHERE published_at IS NULL
              AND dead_lettered_at IS NULL
              AND ambiguous_at IS NULL
              AND available_at <= clock_timestamp()
              AND attempt_count < @maximum_attempts
              AND (lease_owner_id IS NULL OR lease_expires_at <= clock_timestamp())
              AND handoff_started_at IS NULL
            ORDER BY available_at, created_at, message_id
            FOR UPDATE SKIP LOCKED
            LIMIT @maximum_count
        ), claimed AS (
            UPDATE outbox_messages AS target
            SET lease_owner_id = @owner_id,
                lease_fence = target.lease_fence + 1,
                lease_expires_at = clock_timestamp() + @lease_duration,
                handoff_started_at = NULL,
                attempt_count = target.attempt_count + 1
            FROM candidates
            WHERE target.message_id = candidates.message_id
            RETURNING target.*
        )
        SELECT message_id, message_type, schema_version, payload_json::text, occurred_at,
               attempt_count, lease_owner_id, lease_fence, lease_expires_at
        FROM claimed
        ORDER BY available_at, created_at, message_id;
        """;

    private const string NotificationClaimSql = """
        WITH candidates AS (
            SELECT delivery.notification_delivery_id
            FROM notification_deliveries AS delivery
            INNER JOIN alert_rule_channel_bindings AS binding
                ON binding.alert_rule_channel_binding_id = delivery.alert_rule_channel_binding_id
               AND binding.notification_channel_id = delivery.notification_channel_id
               AND binding.enabled = TRUE
            INNER JOIN alert_rules AS rule
                ON rule.alert_rule_id = binding.alert_rule_id
               AND rule.enabled = TRUE
               AND rule.archived_at IS NULL
               AND rule.rule_type = 'CanonicalEvent'
            INNER JOIN notification_channels AS channel
                ON channel.notification_channel_id = delivery.notification_channel_id
               AND channel.enabled = TRUE
            INNER JOIN events AS event_record
                ON event_record.event_id = delivery.event_id
            INNER JOIN database_instances AS instance
                ON instance.instance_id = event_record.instance_id
               AND instance.environment = binding.environment
               AND instance.enabled = TRUE
               AND instance.archived_at IS NULL
            WHERE (delivery.state = 'Pending' OR
                   (delivery.state = 'Delivering' AND delivery.lease_expires_at <= clock_timestamp()))
              AND (delivery.available_at IS NULL OR delivery.available_at <= clock_timestamp())
              AND delivery.attempt_count < @maximum_attempts
              AND delivery.handoff_started_at IS NULL
              AND delivery.idempotency_key IS NOT NULL
              AND jsonb_typeof(rule.configuration_json -> 'eventTypes') = 'array'
              AND (rule.configuration_json -> 'eventTypes') ? event_record.event_type
              AND (rule.instance_scope_json IS NULL OR
                   (jsonb_typeof(rule.instance_scope_json -> 'instanceIds') = 'array' AND
                    (rule.instance_scope_json -> 'instanceIds') ? instance.instance_id::text))
            ORDER BY COALESCE(delivery.available_at, delivery.created_at),
                     delivery.created_at,
                     delivery.notification_delivery_id
            FOR UPDATE OF delivery SKIP LOCKED
            LIMIT @maximum_count
        ), claimed AS (
            UPDATE notification_deliveries AS target
            SET state = 'Delivering',
                lease_owner_id = @owner_id,
                lease_fence = target.lease_fence + 1,
                lease_expires_at = clock_timestamp() + @lease_duration,
                handoff_started_at = NULL,
                last_attempt_at = clock_timestamp(),
                attempt_count = target.attempt_count + 1
            FROM candidates
            WHERE target.notification_delivery_id = candidates.notification_delivery_id
            RETURNING target.*
        )
        SELECT claimed.notification_delivery_id,
               claimed.notification_channel_id,
               claimed.event_id,
               channel.channel_type,
               channel.non_secret_configuration_json::text,
               event_record.event_type,
               event_record.severity,
               event_record.details_json::text,
               claimed.idempotency_key,
               claimed.attempt_count,
               claimed.lease_owner_id,
               claimed.lease_fence,
               claimed.lease_expires_at
        FROM claimed
        INNER JOIN notification_channels AS channel
            ON channel.notification_channel_id = claimed.notification_channel_id
        INNER JOIN events AS event_record
            ON event_record.event_id = claimed.event_id
        ORDER BY COALESCE(claimed.available_at, claimed.created_at),
                 claimed.created_at,
                 claimed.notification_delivery_id;
        """;

    private const string OutboxBeginHandoffSql = """
        UPDATE outbox_messages
        SET handoff_started_at = COALESCE(handoff_started_at, clock_timestamp())
        WHERE message_id = @item_id
          AND lease_owner_id = @owner_id
          AND lease_fence = @fence_token
          AND lease_expires_at > clock_timestamp()
          AND published_at IS NULL
          AND dead_lettered_at IS NULL
          AND ambiguous_at IS NULL
        RETURNING 1;
        """;

    private const string NotificationBeginHandoffSql = """
        UPDATE notification_deliveries
        SET handoff_started_at = COALESCE(handoff_started_at, clock_timestamp())
        WHERE notification_delivery_id = @item_id
          AND lease_owner_id = @owner_id
          AND lease_fence = @fence_token
          AND lease_expires_at > clock_timestamp()
          AND state = 'Delivering'
        RETURNING 1;
        """;

    private const string OutboxCompletionSql = """
        UPDATE outbox_messages
        SET published_at = CASE WHEN @disposition = 'Delivered' THEN clock_timestamp() ELSE published_at END,
            dead_lettered_at = CASE
                WHEN @disposition = 'Retryable' AND attempt_count >= @maximum_attempts THEN clock_timestamp()
                ELSE dead_lettered_at
            END,
            ambiguous_at = CASE WHEN @disposition = 'Ambiguous' THEN clock_timestamp() ELSE ambiguous_at END,
            available_at = CASE
                WHEN @disposition = 'Retryable' AND attempt_count < @maximum_attempts
                    THEN clock_timestamp() +
                         (power(2, LEAST(attempt_count, 8))::integer * interval '1 second')
                ELSE available_at
            END,
            error_code = CASE
                WHEN @disposition = 'Delivered' THEN NULL
                WHEN @disposition = 'Retryable' AND attempt_count >= @maximum_attempts
                    THEN 'delivery.attempts_exhausted'
                ELSE @error_code
            END,
            lease_owner_id = NULL,
            lease_expires_at = NULL,
            handoff_started_at = NULL
        WHERE message_id = @item_id
          AND lease_owner_id = @owner_id
          AND lease_fence = @fence_token
          AND lease_expires_at > clock_timestamp()
          AND published_at IS NULL
          AND dead_lettered_at IS NULL
          AND ambiguous_at IS NULL
          AND (@disposition = 'Retryable' OR handoff_started_at IS NOT NULL)
        RETURNING CASE
            WHEN published_at IS NOT NULL THEN 'Delivered'
            WHEN dead_lettered_at IS NOT NULL THEN 'DeadLettered'
            WHEN ambiguous_at IS NOT NULL THEN 'Ambiguous'
            ELSE 'Pending'
        END;
        """;

    private const string NotificationCompletionSql = """
        UPDATE notification_deliveries
        SET state = CASE
                WHEN @disposition = 'Delivered' THEN 'Delivered'
                WHEN @disposition = 'Ambiguous' THEN 'Ambiguous'
                WHEN attempt_count >= @maximum_attempts THEN 'DeadLettered'
                ELSE 'Pending'
            END,
            delivered_at = CASE WHEN @disposition = 'Delivered' THEN clock_timestamp() ELSE delivered_at END,
            dead_lettered_at = CASE
                WHEN @disposition = 'Retryable' AND attempt_count >= @maximum_attempts THEN clock_timestamp()
                ELSE dead_lettered_at
            END,
            ambiguous_at = CASE WHEN @disposition = 'Ambiguous' THEN clock_timestamp() ELSE ambiguous_at END,
            available_at = CASE
                WHEN @disposition = 'Retryable' AND attempt_count < @maximum_attempts
                    THEN clock_timestamp() +
                         (power(2, LEAST(attempt_count, 8))::integer * interval '1 second')
                ELSE available_at
            END,
            error_code = CASE
                WHEN @disposition = 'Delivered' THEN NULL
                WHEN @disposition = 'Retryable' AND attempt_count >= @maximum_attempts
                    THEN 'delivery.attempts_exhausted'
                ELSE @error_code
            END,
            lease_owner_id = NULL,
            lease_expires_at = NULL,
            handoff_started_at = NULL
        WHERE notification_delivery_id = @item_id
          AND lease_owner_id = @owner_id
          AND lease_fence = @fence_token
          AND lease_expires_at > clock_timestamp()
          AND state = 'Delivering'
          AND (@disposition = 'Retryable' OR handoff_started_at IS NOT NULL)
        RETURNING state;
        """;
}
