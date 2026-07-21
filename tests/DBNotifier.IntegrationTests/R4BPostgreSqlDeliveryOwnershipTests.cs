// Module purpose: Proves R4-B delivery ownership only against the explicitly activated disposable PostgreSQL lab.
using DBNotifier.Application.Operations;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>
/// Exercises atomic claim, skip-locked progress, lease reclaim, stale fencing, ambiguity and dead-letter retention
/// for both central delivery streams without registering an operational publisher or channel adapter.
/// </summary>
public sealed class R4BPostgreSqlDeliveryOwnershipTests
{
    private const string ActivationVariable = "DBNOTIFIER_R4B_POSTGRESQL_LAB";
    private const string ActivationValue = "local-test";
    private const string ConnectionVariable = "DBNOTIFIER_R4B_POSTGRESQL_CONNECTION";

    /// <summary>Runs the complete bounded PostgreSQL ownership matrix only under the exact lab marker.</summary>
    [Fact]
    public async Task DisposablePostgreSqlProvesDurableOwnershipForBothDeliveryStreams()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(ActivationVariable),
                ActivationValue,
                StringComparison.Ordinal))
        {
            return;
        }

        string connectionString = Environment.GetEnvironmentVariable(ConnectionVariable) ??
            throw new InvalidOperationException("r4b.postgresql_connection_missing");
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseNpgsql(connectionString)
            .EnableSensitiveDataLogging(false)
            .Options;
        await using (ServerDbContext migrationContext = new(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        ServerContextFactory factory = new(options);
        PostgreSqlDeliveryOwnershipStore ownershipStore = new(factory);
        await ProveOutboxOwnershipAsync(options, ownershipStore, connectionString);
        await ProveNotificationOwnershipAsync(options, ownershipStore, connectionString);
    }

    /// <summary>Proves concurrency, reclaim, fencing, ambiguity and dead-letter retention for Server outbox work.</summary>
    private static async Task ProveOutboxOwnershipAsync(
        DbContextOptions<ServerDbContext> options,
        PostgreSqlDeliveryOwnershipStore store,
        string connectionString)
    {
        IServerOutboxStore outboxStore = store;
        Guid ownerA = Guid.NewGuid();
        Guid ownerB = Guid.NewGuid();
        Guid[] concurrentIds;
        await using (ServerDbContext setup = new(options))
        {
            ServerOutboxMessageRow[] rows = Enumerable.Range(0, 6)
                .Select(index => OutboxMessage(index))
                .ToArray();
            concurrentIds = rows.Select(row => row.MessageId).ToArray();
            setup.OutboxMessages.AddRange(rows);
            await setup.SaveChangesAsync();
        }

        Task<IReadOnlyList<ServerOutboxEnvelope>> firstClaim = outboxStore
            .ClaimAsync(new DeliveryClaimRequest(ownerA, 3, TimeSpan.FromSeconds(10)), CancellationToken.None)
            .AsTask();
        Task<IReadOnlyList<ServerOutboxEnvelope>> secondClaim = outboxStore
            .ClaimAsync(new DeliveryClaimRequest(ownerB, 3, TimeSpan.FromSeconds(10)), CancellationToken.None)
            .AsTask();
        await Task.WhenAll(firstClaim, secondClaim);
        ServerOutboxEnvelope[] concurrentClaims = [.. firstClaim.Result, .. secondClaim.Result];
        Assert.Equal(6, concurrentClaims.Length);
        Assert.Equal(6, concurrentClaims.Select(item => item.MessageId).Distinct().Count());
        Assert.Equal(concurrentIds.Order(), concurrentClaims.Select(item => item.MessageId).Order());
        Assert.All(concurrentClaims, item =>
        {
            Assert.Equal($"server-outbox:{item.MessageId:N}", item.IdempotencyKey);
            Assert.True(item.Ownership.FenceToken > 0);
            Assert.Equal(1, item.Ownership.AttemptNumber);
        });
        await CompleteKnownRetriesAsync(outboxStore, firstClaim.Result);
        await CompleteKnownRetriesAsync(outboxStore, secondClaim.Result);

        ServerOutboxMessageRow locked = OutboxMessage(100);
        ServerOutboxMessageRow progress = OutboxMessage(101);
        await AddOutboxAsync(options, locked, progress);
        await using (NpgsqlConnection lockConnection = new(connectionString))
        {
            await lockConnection.OpenAsync();
            await using NpgsqlTransaction lockTransaction = await lockConnection.BeginTransactionAsync();
            await LockRowAsync(
                lockConnection,
                lockTransaction,
                "SELECT message_id FROM outbox_messages WHERE message_id = @item_id FOR UPDATE",
                locked.MessageId);
            IReadOnlyList<ServerOutboxEnvelope> skipLocked = await outboxStore.ClaimAsync(
                new DeliveryClaimRequest(ownerA, 1, TimeSpan.FromSeconds(10)),
                CancellationToken.None);
            Assert.Equal(progress.MessageId, Assert.Single(skipLocked).MessageId);
            await CompleteDeliveredAsync(outboxStore, skipLocked);
            await lockTransaction.RollbackAsync();
        }

        await MarkOutboxPublishedAsync(options, locked.MessageId);
        ServerOutboxMessageRow reclaimable = OutboxMessage(200);
        await AddOutboxAsync(options, reclaimable);
        ServerOutboxEnvelope original = Assert.Single(await outboxStore.ClaimAsync(
            new DeliveryClaimRequest(ownerA, 1, TimeSpan.FromSeconds(10)),
            CancellationToken.None));
        await ExpireOutboxLeaseAsync(options, reclaimable.MessageId);
        ServerOutboxEnvelope reclaimed = Assert.Single(await outboxStore.ClaimAsync(
            new DeliveryClaimRequest(ownerB, 1, TimeSpan.FromSeconds(10)),
            CancellationToken.None));
        Assert.Equal(original.MessageId, reclaimed.MessageId);
        Assert.True(reclaimed.Ownership.FenceToken > original.Ownership.FenceToken);
        Assert.False(await outboxStore.BeginHandoffAsync(original.Ownership, CancellationToken.None));
        DeliveryCompletionSummary stale = await outboxStore.CompleteAsync(
            [new DeliveryCompletion(original.Ownership, DeliveryDisposition.Retryable, "fixture.stale")],
            CancellationToken.None);
        Assert.Equal(1, stale.Rejected);
        Assert.True(await outboxStore.BeginHandoffAsync(reclaimed.Ownership, CancellationToken.None));
        await CompleteDeliveredAsync(outboxStore, [reclaimed]);

        ServerOutboxMessageRow uncertain = OutboxMessage(300);
        await AddOutboxAsync(options, uncertain);
        ServerOutboxEnvelope uncertainClaim = Assert.Single(await outboxStore.ClaimAsync(
            new DeliveryClaimRequest(ownerA, 1, TimeSpan.FromSeconds(10)),
            CancellationToken.None));
        Assert.True(await outboxStore.BeginHandoffAsync(uncertainClaim.Ownership, CancellationToken.None));
        SyntheticSideEffectRecorder recorder = new();
        Assert.True(recorder.Accept(uncertainClaim.IdempotencyKey));
        Assert.False(recorder.Accept(uncertainClaim.IdempotencyKey));
        await ExpireOutboxLeaseAsync(options, uncertain.MessageId);
        Assert.Empty(await outboxStore.ClaimAsync(
            new DeliveryClaimRequest(ownerB, 100, TimeSpan.FromSeconds(10)),
            CancellationToken.None));
        await using (ServerDbContext verification = new(options))
        {
            ServerOutboxMessageRow stored = await verification.OutboxMessages.SingleAsync(
                row => row.MessageId == uncertain.MessageId);
            Assert.NotNull(stored.AmbiguousAt);
            Assert.Null(stored.PublishedAt);
            Assert.Null(stored.DeadLetteredAt);
            Assert.Equal("delivery.handoff_outcome_ambiguous", stored.ErrorCode);
        }

        ServerOutboxMessageRow cancelled = OutboxMessage(350);
        await AddOutboxAsync(options, cancelled);
        using CancellationTokenSource cancellation = new();
        CancellingOutboxPublisher cancellingPublisher = new(cancellation);
        ServerOutboxDeliveryRunner cancellingRunner = new(
            outboxStore,
            cancellingPublisher,
            ownerA,
            TimeSpan.FromSeconds(10));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await cancellingRunner.RunOnceAsync(1, cancellation.Token));
        Assert.Equal($"server-outbox:{cancelled.MessageId:N}", cancellingPublisher.IdempotencyKey);
        await ExpireOutboxLeaseAsync(options, cancelled.MessageId);
        Assert.Empty(await outboxStore.ClaimAsync(
            new DeliveryClaimRequest(ownerB, 100, TimeSpan.FromSeconds(10)),
            CancellationToken.None));
        await using (ServerDbContext verification = new(options))
        {
            ServerOutboxMessageRow stored = await verification.OutboxMessages.SingleAsync(
                row => row.MessageId == cancelled.MessageId);
            Assert.NotNull(stored.AmbiguousAt);
            Assert.Equal("delivery.handoff_outcome_ambiguous", stored.ErrorCode);
        }

        ServerOutboxMessageRow exhausted = OutboxMessage(400);
        exhausted.AttemptCount = DeliveryOwnershipPolicy.MaximumAttempts - 1;
        await AddOutboxAsync(options, exhausted);
        ServerOutboxEnvelope finalAttempt = Assert.Single(await outboxStore.ClaimAsync(
            new DeliveryClaimRequest(ownerA, 1, TimeSpan.FromSeconds(10)),
            CancellationToken.None));
        DeliveryCompletionSummary deadLetter = await outboxStore.CompleteAsync(
            [new DeliveryCompletion(finalAttempt.Ownership, DeliveryDisposition.Retryable, "fixture.retry")],
            CancellationToken.None);
        Assert.Equal(1, deadLetter.DeadLettered);
        await using (ServerDbContext verification = new(options))
        {
            ServerOutboxMessageRow stored = await verification.OutboxMessages.SingleAsync(
                row => row.MessageId == exhausted.MessageId);
            Assert.NotNull(stored.DeadLetteredAt);
            Assert.Equal("delivery.attempts_exhausted", stored.ErrorCode);
        }
    }

    /// <summary>Proves the same ownership guarantees while retaining exact R4-A rule and channel binding.</summary>
    private static async Task ProveNotificationOwnershipAsync(
        DbContextOptions<ServerDbContext> options,
        PostgreSqlDeliveryOwnershipStore store,
        string connectionString)
    {
        INotificationDeliveryStore notificationStore = store;
        NotificationFixture fixture = await CreateNotificationFixtureAsync(options);
        Guid ownerA = Guid.NewGuid();
        Guid ownerB = Guid.NewGuid();
        NotificationDeliveryRow[] concurrent = await AddNotificationsAsync(options, fixture, 6, 0);
        Task<IReadOnlyList<NotificationEnvelope>> firstClaim = notificationStore
            .ClaimAsync(new DeliveryClaimRequest(ownerA, 3, TimeSpan.FromSeconds(10)), CancellationToken.None)
            .AsTask();
        Task<IReadOnlyList<NotificationEnvelope>> secondClaim = notificationStore
            .ClaimAsync(new DeliveryClaimRequest(ownerB, 3, TimeSpan.FromSeconds(10)), CancellationToken.None)
            .AsTask();
        await Task.WhenAll(firstClaim, secondClaim);
        NotificationEnvelope[] concurrentClaims = [.. firstClaim.Result, .. secondClaim.Result];
        Assert.Equal(6, concurrentClaims.Length);
        Assert.Equal(6, concurrentClaims.Select(item => item.NotificationDeliveryId).Distinct().Count());
        Assert.Equal(
            concurrent.Select(item => item.NotificationDeliveryId).Order(),
            concurrentClaims.Select(item => item.NotificationDeliveryId).Order());
        Assert.All(concurrentClaims, item => Assert.StartsWith("notification:", item.IdempotencyKey));
        await CompleteKnownRetriesAsync(notificationStore, firstClaim.Result);
        await CompleteKnownRetriesAsync(notificationStore, secondClaim.Result);

        NotificationDeliveryRow[] headOfLine = await AddNotificationsAsync(options, fixture, 2, 100);
        await using (NpgsqlConnection lockConnection = new(connectionString))
        {
            await lockConnection.OpenAsync();
            await using NpgsqlTransaction lockTransaction = await lockConnection.BeginTransactionAsync();
            await LockRowAsync(
                lockConnection,
                lockTransaction,
                "SELECT notification_delivery_id FROM notification_deliveries " +
                "WHERE notification_delivery_id = @item_id FOR UPDATE",
                headOfLine[0].NotificationDeliveryId);
            NotificationEnvelope selected = Assert.Single(await notificationStore.ClaimAsync(
                new DeliveryClaimRequest(ownerA, 1, TimeSpan.FromSeconds(10)),
                CancellationToken.None));
            Assert.Equal(headOfLine[1].NotificationDeliveryId, selected.NotificationDeliveryId);
            Assert.True(await notificationStore.BeginHandoffAsync(selected.Ownership, CancellationToken.None));
            await CompleteDeliveredAsync(notificationStore, [selected]);
            await lockTransaction.RollbackAsync();
        }

        await CancelNotificationAsync(options, headOfLine[0].NotificationDeliveryId);
        NotificationDeliveryRow reclaimable = Assert.Single(await AddNotificationsAsync(options, fixture, 1, 200));
        NotificationEnvelope original = Assert.Single(await notificationStore.ClaimAsync(
            new DeliveryClaimRequest(ownerA, 1, TimeSpan.FromSeconds(10)),
            CancellationToken.None));
        await ExpireNotificationLeaseAsync(options, reclaimable.NotificationDeliveryId);
        NotificationEnvelope reclaimed = Assert.Single(await notificationStore.ClaimAsync(
            new DeliveryClaimRequest(ownerB, 1, TimeSpan.FromSeconds(10)),
            CancellationToken.None));
        Assert.True(reclaimed.Ownership.FenceToken > original.Ownership.FenceToken);
        Assert.False(await notificationStore.BeginHandoffAsync(original.Ownership, CancellationToken.None));
        DeliveryCompletionSummary stale = await notificationStore.CompleteAsync(
            [new DeliveryCompletion(original.Ownership, DeliveryDisposition.Retryable, "fixture.stale")],
            CancellationToken.None);
        Assert.Equal(1, stale.Rejected);
        Assert.True(await notificationStore.BeginHandoffAsync(reclaimed.Ownership, CancellationToken.None));
        await CompleteDeliveredAsync(notificationStore, [reclaimed]);

        NotificationDeliveryRow uncertain = Assert.Single(await AddNotificationsAsync(options, fixture, 1, 300));
        NotificationEnvelope uncertainClaim = Assert.Single(await notificationStore.ClaimAsync(
            new DeliveryClaimRequest(ownerA, 1, TimeSpan.FromSeconds(10)),
            CancellationToken.None));
        Assert.True(await notificationStore.BeginHandoffAsync(uncertainClaim.Ownership, CancellationToken.None));
        SyntheticSideEffectRecorder recorder = new();
        Assert.True(recorder.Accept(uncertainClaim.IdempotencyKey));
        Assert.False(recorder.Accept(uncertainClaim.IdempotencyKey));
        await ExpireNotificationLeaseAsync(options, uncertain.NotificationDeliveryId);
        Assert.Empty(await notificationStore.ClaimAsync(
            new DeliveryClaimRequest(ownerB, 100, TimeSpan.FromSeconds(10)),
            CancellationToken.None));
        await using (ServerDbContext verification = new(options))
        {
            NotificationDeliveryRow stored = await verification.NotificationDeliveries.SingleAsync(
                row => row.NotificationDeliveryId == uncertain.NotificationDeliveryId);
            Assert.Equal("Ambiguous", stored.State);
            Assert.NotNull(stored.AmbiguousAt);
            Assert.Equal("delivery.handoff_outcome_ambiguous", stored.ErrorCode);
        }

        NotificationDeliveryRow exhausted = Assert.Single(await AddNotificationsAsync(options, fixture, 1, 400));
        await SetNotificationAttemptCountAsync(
            options,
            exhausted.NotificationDeliveryId,
            DeliveryOwnershipPolicy.MaximumAttempts - 1);
        NotificationEnvelope finalAttempt = Assert.Single(await notificationStore.ClaimAsync(
            new DeliveryClaimRequest(ownerA, 1, TimeSpan.FromSeconds(10)),
            CancellationToken.None));
        DeliveryCompletionSummary deadLetter = await notificationStore.CompleteAsync(
            [new DeliveryCompletion(finalAttempt.Ownership, DeliveryDisposition.Retryable, "fixture.retry")],
            CancellationToken.None);
        Assert.Equal(1, deadLetter.DeadLettered);
        await using (ServerDbContext verification = new(options))
        {
            NotificationDeliveryRow stored = await verification.NotificationDeliveries.SingleAsync(
                row => row.NotificationDeliveryId == exhausted.NotificationDeliveryId);
            Assert.Equal("DeadLettered", stored.State);
            Assert.NotNull(stored.DeadLetteredAt);
            Assert.Equal("delivery.attempts_exhausted", stored.ErrorCode);
        }
    }

    /// <summary>Persists known retry outcomes for one claimed outbox set without beginning an external hand-off.</summary>
    private static async Task CompleteKnownRetriesAsync(
        IServerOutboxStore store,
        IReadOnlyList<ServerOutboxEnvelope> claimed)
    {
        DeliveryCompletionSummary summary = await store.CompleteAsync(
            claimed.Select(item => new DeliveryCompletion(
                item.Ownership,
                DeliveryDisposition.Retryable,
                "fixture.retry")).ToArray(),
            CancellationToken.None);
        Assert.Equal(claimed.Count, summary.RetryScheduled);
    }

    /// <summary>Persists known retry outcomes for one claimed notification set without invoking an adapter.</summary>
    private static async Task CompleteKnownRetriesAsync(
        INotificationDeliveryStore store,
        IReadOnlyList<NotificationEnvelope> claimed)
    {
        DeliveryCompletionSummary summary = await store.CompleteAsync(
            claimed.Select(item => new DeliveryCompletion(
                item.Ownership,
                DeliveryDisposition.Retryable,
                "fixture.retry")).ToArray(),
            CancellationToken.None);
        Assert.Equal(claimed.Count, summary.RetryScheduled);
    }

    /// <summary>Begins and confirms delivery for one exact outbox ownership set.</summary>
    private static async Task CompleteDeliveredAsync(
        IServerOutboxStore store,
        IReadOnlyList<ServerOutboxEnvelope> claimed)
    {
        foreach (ServerOutboxEnvelope item in claimed)
        {
            Assert.True(await store.BeginHandoffAsync(item.Ownership, CancellationToken.None));
        }

        DeliveryCompletionSummary summary = await store.CompleteAsync(
            claimed.Select(item => new DeliveryCompletion(
                item.Ownership,
                DeliveryDisposition.Delivered)).ToArray(),
            CancellationToken.None);
        Assert.Equal(claimed.Count, summary.Delivered);
    }

    /// <summary>Confirms delivery for notification ownership whose hand-off marker is already durable.</summary>
    private static async Task CompleteDeliveredAsync(
        INotificationDeliveryStore store,
        IReadOnlyList<NotificationEnvelope> claimed)
    {
        DeliveryCompletionSummary summary = await store.CompleteAsync(
            claimed.Select(item => new DeliveryCompletion(
                item.Ownership,
                DeliveryDisposition.Delivered)).ToArray(),
            CancellationToken.None);
        Assert.Equal(claimed.Count, summary.Delivered);
    }

    /// <summary>Adds isolated due outbox fixtures to the disposable central store.</summary>
    private static async Task AddOutboxAsync(
        DbContextOptions<ServerDbContext> options,
        params ServerOutboxMessageRow[] rows)
    {
        await using ServerDbContext context = new(options);
        context.OutboxMessages.AddRange(rows);
        await context.SaveChangesAsync();
    }

    /// <summary>Marks one unclaimed head-of-line fixture terminal after its lock test completes.</summary>
    private static async Task MarkOutboxPublishedAsync(
        DbContextOptions<ServerDbContext> options,
        Guid itemId)
    {
        await using ServerDbContext context = new(options);
        ServerOutboxMessageRow row = await context.OutboxMessages.SingleAsync(item => item.MessageId == itemId);
        row.PublishedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
    }

    /// <summary>Moves only one fixture lease into the past to emulate a pre-hand-off worker crash.</summary>
    private static async Task ExpireOutboxLeaseAsync(
        DbContextOptions<ServerDbContext> options,
        Guid itemId)
    {
        await using ServerDbContext context = new(options);
        ServerOutboxMessageRow row = await context.OutboxMessages.SingleAsync(item => item.MessageId == itemId);
        row.LeaseExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        await context.SaveChangesAsync();
    }

    /// <summary>Creates the exact enabled R4-A rule, channel, binding, instance and Agent fixture.</summary>
    private static async Task<NotificationFixture> CreateNotificationFixtureAsync(
        DbContextOptions<ServerDbContext> options)
    {
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        Guid ruleId = Guid.NewGuid();
        Guid channelId = Guid.NewGuid();
        Guid bindingId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await using ServerDbContext context = new(options);
        context.Agents.Add(new RegisteredAgentRow
        {
            AgentId = agentId,
            InstallationId = $"r4b-{agentId:N}",
            DisplayName = "R4-B synthetic Agent",
            Environment = "r4b-local",
            Platform = "synthetic",
            AgentVersion = "fixture",
            CertificateThumbprint = $"R4B-{agentId:N}",
            State = "Active",
            EnrolledAt = now,
            ConcurrencyToken = Guid.NewGuid(),
        });
        context.Instances.Add(new DatabaseInstanceRow
        {
            InstanceId = instanceId,
            DisplayName = "R4-B synthetic instance",
            ProviderType = "synthetic",
            Environment = "r4b-local",
            EndpointJson = "{}",
            AssignedAgentId = agentId,
            TagsJson = "[]",
            IntervalSeconds = 60,
            TimeoutSeconds = 5,
            RetryCount = 0,
            Enabled = true,
            CreatedAt = now,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid(),
        });
        context.AlertRules.Add(new AlertRuleRow
        {
            AlertRuleId = ruleId,
            Name = "R4-B exact event rule",
            RuleType = "CanonicalEvent",
            ConfigurationJson = "{\"eventTypes\":[\"Fixture\"]}",
            Enabled = true,
            CreatedAt = now,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid(),
        });
        context.NotificationChannels.Add(new NotificationChannelRow
        {
            NotificationChannelId = channelId,
            Name = "R4-B synthetic channel",
            ChannelType = "r4b-synthetic",
            NonSecretConfigurationJson = "{}",
            Enabled = true,
            CreatedAt = now,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid(),
        });
        context.AlertRuleChannelBindings.Add(new AlertRuleChannelBindingRow
        {
            AlertRuleChannelBindingId = bindingId,
            AlertRuleId = ruleId,
            NotificationChannelId = channelId,
            Environment = "r4b-local",
            Enabled = true,
            CreatedAt = now,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid(),
        });
        await context.SaveChangesAsync();
        return new NotificationFixture(agentId, instanceId, channelId, bindingId);
    }

    /// <summary>Adds exact bound notification fixtures and returns their durable rows.</summary>
    private static async Task<NotificationDeliveryRow[]> AddNotificationsAsync(
        DbContextOptions<ServerDbContext> options,
        NotificationFixture fixture,
        int count,
        int sequenceOffset)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<NotificationDeliveryRow> deliveries = [];
        await using ServerDbContext context = new(options);
        for (int index = 0; index < count; index++)
        {
            EventRecordRow eventRow = new()
            {
                EventId = Guid.NewGuid(),
                InstanceId = fixture.InstanceId,
                AgentId = fixture.AgentId,
                CorrelationId = Guid.NewGuid(),
                EventType = "Fixture",
                Severity = "Info",
                ObservedAt = now.AddMilliseconds(sequenceOffset + index),
                ReceivedAt = now.AddMilliseconds(sequenceOffset + index),
                DetailsJson = "{}",
            };
            NotificationDeliveryRow delivery = new()
            {
                NotificationDeliveryId = Guid.NewGuid(),
                NotificationChannelId = fixture.ChannelId,
                EventId = eventRow.EventId,
                AlertRuleChannelBindingId = fixture.BindingId,
                IdempotencyKey = $"notification:{eventRow.EventId:N}:{fixture.BindingId:N}",
                State = "Pending",
                CreatedAt = eventRow.ReceivedAt,
            };
            context.Events.Add(eventRow);
            context.NotificationDeliveries.Add(delivery);
            deliveries.Add(delivery);
        }

        await context.SaveChangesAsync();
        return deliveries.ToArray();
    }

    /// <summary>Moves only one notification fixture lease into the past.</summary>
    private static async Task ExpireNotificationLeaseAsync(
        DbContextOptions<ServerDbContext> options,
        Guid itemId)
    {
        await using ServerDbContext context = new(options);
        NotificationDeliveryRow row = await context.NotificationDeliveries.SingleAsync(
            item => item.NotificationDeliveryId == itemId);
        row.LeaseExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        await context.SaveChangesAsync();
    }

    /// <summary>Removes one head-of-line fixture from eligibility after its lock is released.</summary>
    private static async Task CancelNotificationAsync(
        DbContextOptions<ServerDbContext> options,
        Guid itemId)
    {
        await using ServerDbContext context = new(options);
        NotificationDeliveryRow row = await context.NotificationDeliveries.SingleAsync(
            item => item.NotificationDeliveryId == itemId);
        row.State = "Cancelled";
        await context.SaveChangesAsync();
    }

    /// <summary>Positions one fixture at its final allowed attempt without changing operational data.</summary>
    private static async Task SetNotificationAttemptCountAsync(
        DbContextOptions<ServerDbContext> options,
        Guid itemId,
        int attemptCount)
    {
        await using ServerDbContext context = new(options);
        NotificationDeliveryRow row = await context.NotificationDeliveries.SingleAsync(
            item => item.NotificationDeliveryId == itemId);
        row.AttemptCount = attemptCount;
        await context.SaveChangesAsync();
    }

    /// <summary>Holds one exact fixture row lock while another worker proves skip-locked progress.</summary>
    private static async Task LockRowAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        Guid itemId)
    {
        await using NpgsqlCommand command = new(sql, connection, transaction);
        command.Parameters.AddWithValue("item_id", NpgsqlDbType.Uuid, itemId);
        Assert.Equal(itemId, await command.ExecuteScalarAsync());
    }

    private static ServerOutboxMessageRow OutboxMessage(int sequence)
    {
        DateTimeOffset instant = DateTimeOffset.UtcNow.AddMinutes(-1).AddMilliseconds(sequence);
        return new ServerOutboxMessageRow
        {
            MessageId = Guid.NewGuid(),
            MessageType = "r4b.fixture.v1",
            SchemaVersion = 1,
            PayloadJson = "{}",
            OccurredAt = instant,
            CreatedAt = instant,
            AvailableAt = instant,
        };
    }

    private sealed class ServerContextFactory(DbContextOptions<ServerDbContext> options) :
        IDbContextFactory<ServerDbContext>
    {
        public ServerDbContext CreateDbContext() => new(options);
    }

    private sealed class SyntheticSideEffectRecorder
    {
        private readonly HashSet<string> accepted = new(StringComparer.Ordinal);

        public bool Accept(string idempotencyKey) => accepted.Add(idempotencyKey);
    }

    /// <summary>Cancels one synthetic publisher call after receiving its durable idempotency identity.</summary>
    /// <param name="cancellation">Token source controlled exclusively by the current laboratory case.</param>
    private sealed class CancellingOutboxPublisher(CancellationTokenSource cancellation) : IServerMessagePublisher
    {
        /// <summary>Gets the idempotency identity observed before synthetic cancellation.</summary>
        public string? IdempotencyKey { get; private set; }

        /// <inheritdoc />
        public ValueTask<DeliveryResult> PublishAsync(
            ServerOutboxEnvelope message,
            CancellationToken cancellationToken)
        {
            IdempotencyKey = message.IdempotencyKey;
            cancellation.Cancel();
            return ValueTask.FromCanceled<DeliveryResult>(cancellationToken);
        }
    }

    private sealed record NotificationFixture(Guid AgentId, Guid InstanceId, Guid ChannelId, Guid BindingId);
}
