// Module purpose: Verifies Maintenance Delivery Tests behaviour and protects the documented project contract.
using DBNotifier.Application.Operations;
using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Server.Api;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.UnitTests;

public sealed class MaintenanceDeliveryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ServerDeliveryDefaultsAreDisabledAndEnablingWithoutLeaseIsRejected()
    {
        ServerOperationsOptions defaults = new();

        Assert.False(defaults.ServerOutboxEnabled);
        Assert.False(defaults.NotificationDeliveryEnabled);
        defaults.ValidateForStartup();

        ServerOperationsOptions enabled = new() { ServerOutboxEnabled = true };
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(enabled.ValidateForStartup);
        Assert.Equal("server.delivery_durable_lease_unavailable", exception.Message);

        ServerOperationsOptions notifications = new() { NotificationDeliveryEnabled = true };
        exception = Assert.Throws<InvalidOperationException>(notifications.ValidateForStartup);
        Assert.Equal("server.delivery_durable_lease_unavailable", exception.Message);
    }

    [Fact]
    public async Task PublisherFailureAfterHandoffBecomesAmbiguousInsteadOfRetryable()
    {
        Guid itemId = Guid.NewGuid();
        FakeOutboxOwnershipStore store = new(itemId);
        ServerOutboxDeliveryRunner runner = new(
            store,
            new ThrowingPublisher(),
            Guid.NewGuid(),
            TimeSpan.FromSeconds(30));

        DeliveryCycleResult result = await runner.RunOnceAsync(1);

        Assert.Equal(new DeliveryCycleResult(1, 0, 0, 0, 1, 0), result);
        Assert.Single(store.Handoffs);
    }

    [Fact]
    public async Task AgentRetentionSupportsDryRunAndDeletesOnlyEligibleBoundedRows()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (AgentDbContext setup = new(options))
        {
            await setup.Database.MigrateAsync();
            setup.HealthObservations.AddRange(
                AgentObservation(Now.AddDays(-8)),
                AgentObservation(Now.AddDays(-1)));
            setup.OutboxMessages.AddRange(
                AgentOutbox(1, Now.AddDays(-2)),
                AgentOutbox(2, null));
            setup.InboxCommands.AddRange(
                InboxCommand("Succeeded", Now.AddDays(-31)),
                InboxCommand("Running", null));
            await setup.SaveChangesAsync();
        }

        AgentRetentionStore store = new(new AgentContextFactory(options));
        RetentionExecutionResult dryRun = await store.ExecuteAsync(
            new RetentionExecutionRequest(Now, 100, ApplyChanges: false),
            CancellationToken.None);
        await using (AgentDbContext afterDryRun = new(options))
        {
            Assert.Equal(2, await afterDryRun.HealthObservations.CountAsync());
            Assert.Equal(2, await afterDryRun.OutboxMessages.CountAsync());
            Assert.Equal(2, await afterDryRun.InboxCommands.CountAsync());
        }

        RetentionExecutionResult applied = await store.ExecuteAsync(
            new RetentionExecutionRequest(Now, 100, ApplyChanges: true),
            CancellationToken.None);

        Assert.Equal(dryRun, applied);
        Assert.Equal(1, applied.Observations);
        Assert.Equal(1, applied.AgentOutboxTombstones);
        Assert.Equal(1, applied.AgentInboxCommands);
        await using AgentDbContext verification = new(options);
        Assert.Equal(1, await verification.HealthObservations.CountAsync());
        Assert.Equal(1, await verification.OutboxMessages.CountAsync());
        Assert.Equal(1, await verification.InboxCommands.CountAsync());
        Assert.Null((await verification.OutboxMessages.SingleAsync()).AcknowledgedAt);
    }

    [Fact]
    public async Task ServerRetentionPreservesObservationsAndDurableCursorUntilAggregatesExist()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        HealthSampleRow referenced = ServerObservation(instanceId, agentId, Now.AddDays(-31));
        HealthSampleRow disposable = ServerObservation(instanceId, agentId, Now.AddDays(-31));
        EventRecordRow eventRow = Event(referenced.ObservationId, instanceId, agentId);
        NotificationChannelRow channel = Channel();
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            setup.HealthSamples.AddRange(referenced, disposable);
            setup.AgentObservationCursors.Add(new AgentObservationCursorRow
            {
                AgentId = agentId,
                HighestContiguousSequence = 2,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.Events.Add(eventRow);
            setup.AgentHeartbeats.Add(Heartbeat(agentId, Now.AddDays(-91)));
            setup.NotificationChannels.Add(channel);
            setup.NotificationDeliveries.Add(Delivery(channel.NotificationChannelId, eventRow.EventId, Now.AddMonths(-13)));
            setup.OutboxMessages.AddRange(
                ServerOutbox(Now.AddDays(-8), Now.AddDays(-8)),
                ServerOutbox(Now.AddDays(-8), null));
            setup.AuditEntries.Add(Audit());
            await setup.SaveChangesAsync();
        }

        ServerMaintenanceStore store = new(new ServerContextFactory(options));
        RetentionExecutionResult result = await store.ExecuteAsync(
            new RetentionExecutionRequest(Now, 100, ApplyChanges: true),
            CancellationToken.None);

        Assert.Equal(0, result.Observations);
        Assert.Equal(1, result.Heartbeats);
        Assert.Equal(1, result.NotificationDeliveries);
        Assert.Equal(1, result.ServerOutboxTombstones);
        await using ServerDbContext verification = new(options);
        Assert.Equal(2, await verification.HealthSamples.CountAsync());
        Assert.Equal(2, (await verification.AgentObservationCursors.SingleAsync()).HighestContiguousSequence);
        Assert.Empty(await verification.AgentHeartbeats.ToArrayAsync());
        Assert.Empty(await verification.NotificationDeliveries.ToArrayAsync());
        Assert.Null((await verification.OutboxMessages.SingleAsync()).PublishedAt);
        Assert.Single(await verification.AuditEntries.ToArrayAsync());
    }

    [Fact]
    public async Task ServerOutboxDeliveryPersistsPublishedAndRetryableOutcomes()
    {
        Guid ownerId = Guid.NewGuid();
        Guid deliveredId = Guid.NewGuid();
        FakeOutboxOwnershipStore store = new(deliveredId, Guid.NewGuid());
        ServerOutboxDeliveryRunner runner = new(
            store,
            new SplitPublisher(deliveredId),
            ownerId,
            TimeSpan.FromSeconds(30));

        DeliveryCycleResult result = await runner.RunOnceAsync(10);

        Assert.Equal(new DeliveryCycleResult(2, 1, 1), result);
        Assert.Equal(2, store.Handoffs.Count);
        Assert.All(store.Claimed, message =>
            Assert.Equal($"server-outbox:{message.MessageId:N}", message.IdempotencyKey));
    }

    [Fact]
    public async Task NotificationDeliveryBacksOffWithoutAdapterThenDeliversOnce()
    {
        Guid ownerId = Guid.NewGuid();
        Guid deliveryId = Guid.NewGuid();
        FakeNotificationOwnershipStore store = new(deliveryId, "fixture");
        NotificationDeliveryRunner unavailable = new(
            store,
            [],
            ownerId,
            TimeSpan.FromSeconds(30));
        DeliveryCycleResult first = await unavailable.RunOnceAsync(10);
        NotificationDeliveryRunner available = new(
            store,
            [new SuccessfulChannelAdapter("fixture")],
            ownerId,
            TimeSpan.FromSeconds(30));
        DeliveryCycleResult second = await available.RunOnceAsync(10);

        Assert.Equal(new DeliveryCycleResult(1, 0, 1), first);
        Assert.Equal(new DeliveryCycleResult(1, 1, 0), second);
        Assert.Single(store.Handoffs);
        Assert.Equal("notification:fixture", store.LastIdempotencyKey);
    }

    private static AgentHealthObservationRow AgentObservation(DateTimeOffset createdAt) => new()
    {
        ObservationId = Guid.NewGuid(),
        InstanceId = Guid.NewGuid(),
        ProviderType = "postgresql",
        ProviderVersion = "fixture",
        Status = "Healthy",
        Method = "fixture",
        EvidenceLevel = "ProviderAuthenticated",
        ObservedAt = createdAt,
        DurationMilliseconds = 1,
        AttemptCount = 1,
        CreatedAt = createdAt,
    };

    private static AgentOutboxMessageRow AgentOutbox(long sequence, DateTimeOffset? acknowledgedAt) => new()
    {
        MessageId = Guid.NewGuid(),
        Sequence = sequence,
        MessageType = "fixture.v1",
        SchemaVersion = 1,
        PayloadJson = "{}",
        OccurredAt = Now,
        CreatedAt = Now,
        AvailableAt = Now,
        AcknowledgedAt = acknowledgedAt,
    };

    private static AgentInboxCommandRow InboxCommand(string state, DateTimeOffset? completedAt) => new()
    {
        CommandId = Guid.NewGuid(),
        IdempotencyKey = $"fixture:{Guid.NewGuid():N}",
        InstanceId = Guid.NewGuid(),
        ProviderId = "fixture",
        CapabilityId = "fixture.capability",
        TypedParametersJson = "{}",
        State = state,
        RequestedAt = Now.AddDays(-40),
        ExpiresAt = Now.AddDays(-39),
        ExpectedAgentVersion = "fixture",
        ExpectedProviderVersion = "fixture",
        CompletedAt = completedAt,
        ConcurrencyToken = Guid.NewGuid(),
    };

    private static RegisteredAgentRow Agent(Guid agentId) => new()
    {
        AgentId = agentId,
        InstallationId = $"fixture-{agentId:N}",
        DisplayName = "Fixture Agent",
        Environment = "test",
        Platform = "fixture",
        AgentVersion = "fixture",
        CertificateThumbprint = $"FIXTURE-{agentId:N}",
        State = "Active",
        EnrolledAt = Now,
        ConcurrencyToken = Guid.NewGuid(),
    };

    private static DatabaseInstanceRow Instance(Guid instanceId, Guid agentId) => new()
    {
        InstanceId = instanceId,
        DisplayName = "Fixture Instance",
        ProviderType = "postgresql",
        Environment = "test",
        EndpointJson = "{}",
        AssignedAgentId = agentId,
        TagsJson = "[]",
        IntervalSeconds = 60,
        TimeoutSeconds = 5,
        RetryCount = 1,
        Enabled = true,
        CreatedAt = Now,
        UpdatedAt = Now,
        ConcurrencyToken = Guid.NewGuid(),
    };

    private static HealthSampleRow ServerObservation(Guid instanceId, Guid agentId, DateTimeOffset receivedAt) => new()
    {
        ObservationId = Guid.NewGuid(),
        InstanceId = instanceId,
        AgentId = agentId,
        MessageId = Guid.NewGuid(),
        Sequence = Random.Shared.NextInt64(1, long.MaxValue),
        ProviderType = "postgresql",
        ProviderVersion = "fixture",
        Status = "Healthy",
        Method = "fixture",
        EvidenceLevel = "ProviderAuthenticated",
        ObservedAt = receivedAt,
        ReceivedAt = receivedAt,
        DurationMilliseconds = 1,
    };

    private static AgentHeartbeatRow Heartbeat(Guid agentId, DateTimeOffset receivedAt) => new()
    {
        HeartbeatId = Guid.NewGuid(),
        AgentId = agentId,
        MessageId = Guid.NewGuid(),
        Sequence = 1,
        AgentVersion = "fixture",
        ProtocolMinimum = 1,
        ProtocolMaximum = 1,
        QueueDepth = 0,
        AgentTime = receivedAt,
        ReceivedAt = receivedAt,
    };

    private static EventRecordRow Event(Guid? observationId, Guid? instanceId, Guid? agentId) => new()
    {
        EventId = Guid.NewGuid(),
        InstanceId = instanceId,
        AgentId = agentId,
        SourceObservationId = observationId,
        CorrelationId = Guid.NewGuid(),
        EventType = "Fixture",
        Severity = "Info",
        ObservedAt = Now,
        ReceivedAt = Now,
        DetailsJson = "{}",
    };

    private static NotificationChannelRow Channel() => new()
    {
        NotificationChannelId = Guid.NewGuid(),
        Name = "Fixture Channel",
        ChannelType = "fixture",
        NonSecretConfigurationJson = "{}",
        Enabled = true,
        CreatedAt = Now,
        UpdatedAt = Now,
        ConcurrencyToken = Guid.NewGuid(),
    };

    private static NotificationDeliveryRow Delivery(
        Guid channelId,
        Guid eventId,
        DateTimeOffset? deliveredAt,
        Guid? bindingId = null) => new()
        {
            NotificationDeliveryId = Guid.NewGuid(),
            NotificationChannelId = channelId,
            EventId = eventId,
            AlertRuleChannelBindingId = bindingId,
            IdempotencyKey = bindingId is Guid provenBindingId
                ? $"notification:{eventId:N}:{provenBindingId:N}"
                : null,
            State = deliveredAt is null ? "Pending" : "Delivered",
            AttemptCount = deliveredAt is null ? 0 : 1,
            CreatedAt = deliveredAt ?? Now,
            LastAttemptAt = deliveredAt,
            DeliveredAt = deliveredAt,
        };

    private static ServerOutboxMessageRow ServerOutbox(DateTimeOffset createdAt, DateTimeOffset? publishedAt) => new()
    {
        MessageId = Guid.NewGuid(),
        MessageType = "fixture.v1",
        SchemaVersion = 1,
        PayloadJson = "{}",
        OccurredAt = createdAt,
        CreatedAt = createdAt,
        AvailableAt = createdAt,
        PublishedAt = publishedAt,
    };

    private static AuditEntryRow Audit() => new()
    {
        AuditEntryId = Guid.NewGuid(),
        OccurredAt = Now.AddYears(-3),
        ActorType = "Human",
        ActorId = "fixture",
        Action = "fixture",
        TargetType = "Fixture",
        TargetId = "fixture",
        Outcome = "Succeeded",
        CorrelationId = Guid.NewGuid(),
        DetailsJson = "{}",
    };

    private sealed class AgentContextFactory(DbContextOptions<AgentDbContext> options) : IDbContextFactory<AgentDbContext>
    {
        public AgentDbContext CreateDbContext() => new(options);
    }

    private sealed class ServerContextFactory(DbContextOptions<ServerDbContext> options) : IDbContextFactory<ServerDbContext>
    {
        public ServerDbContext CreateDbContext() => new(options);
    }

    private sealed class SplitPublisher(Guid deliveredId) : IServerMessagePublisher
    {
        public ValueTask<DeliveryResult> PublishAsync(
            ServerOutboxEnvelope message,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new DeliveryResult(
                message.MessageId,
                message.MessageId == deliveredId ? DeliveryDisposition.Delivered : DeliveryDisposition.Retryable,
                message.MessageId == deliveredId ? null : "fixture.retry"));
    }

    private sealed class SuccessfulChannelAdapter(string channelType) : INotificationChannelAdapter
    {
        public string ChannelType { get; } = channelType;

        public ValueTask<DeliveryResult> DeliverAsync(
            NotificationEnvelope notification,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new DeliveryResult(
                notification.NotificationDeliveryId,
                DeliveryDisposition.Delivered));
    }

    private sealed class FakeOutboxOwnershipStore(params Guid[] itemIds) : IServerOutboxStore
    {
        public List<ServerOutboxEnvelope> Claimed { get; } = [];

        public List<DeliveryOwnership> Handoffs { get; } = [];

        public ValueTask<IReadOnlyList<ServerOutboxEnvelope>> ClaimAsync(
            DeliveryClaimRequest request,
            CancellationToken cancellationToken)
        {
            Claimed.Clear();
            Claimed.AddRange(itemIds.Select((itemId, index) => new ServerOutboxEnvelope(
                itemId,
                "fixture.v1",
                1,
                "{}",
                Now,
                $"server-outbox:{itemId:N}",
                new DeliveryOwnership(
                    itemId,
                    request.OwnerId,
                    index + 1,
                    Now.Add(request.LeaseDuration),
                    1))));
            return ValueTask.FromResult<IReadOnlyList<ServerOutboxEnvelope>>(Claimed);
        }

        public ValueTask<bool> BeginHandoffAsync(
            DeliveryOwnership ownership,
            CancellationToken cancellationToken)
        {
            Handoffs.Add(ownership);
            return ValueTask.FromResult(true);
        }

        public ValueTask<DeliveryCompletionSummary> CompleteAsync(
            IReadOnlyList<DeliveryCompletion> completions,
            CancellationToken cancellationToken)
        {
            int delivered = completions.Count(item => item.Disposition == DeliveryDisposition.Delivered);
            int ambiguous = completions.Count(item => item.Disposition == DeliveryDisposition.Ambiguous);
            return ValueTask.FromResult(new DeliveryCompletionSummary(
                delivered,
                completions.Count - delivered - ambiguous,
                0,
                ambiguous,
                0));
        }
    }

    private sealed class FakeNotificationOwnershipStore(Guid itemId, string channelType) : INotificationDeliveryStore
    {
        private int attemptNumber;

        public List<DeliveryOwnership> Handoffs { get; } = [];

        public string? LastIdempotencyKey { get; private set; }

        public ValueTask<IReadOnlyList<NotificationEnvelope>> ClaimAsync(
            DeliveryClaimRequest request,
            CancellationToken cancellationToken)
        {
            DeliveryOwnership ownership = new(
                itemId,
                request.OwnerId,
                ++attemptNumber,
                Now.Add(request.LeaseDuration),
                attemptNumber);
            LastIdempotencyKey = "notification:fixture";
            return ValueTask.FromResult<IReadOnlyList<NotificationEnvelope>>(
            [
                new NotificationEnvelope(
                    itemId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    channelType,
                    "{}",
                    "Fixture",
                    "Info",
                    "{}",
                    LastIdempotencyKey,
                    ownership),
            ]);
        }

        public ValueTask<bool> BeginHandoffAsync(
            DeliveryOwnership ownership,
            CancellationToken cancellationToken)
        {
            Handoffs.Add(ownership);
            return ValueTask.FromResult(true);
        }

        public ValueTask<DeliveryCompletionSummary> CompleteAsync(
            IReadOnlyList<DeliveryCompletion> completions,
            CancellationToken cancellationToken)
        {
            DeliveryCompletion completion = Assert.Single(completions);
            return ValueTask.FromResult(completion.Disposition == DeliveryDisposition.Delivered
                ? new DeliveryCompletionSummary(1, 0, 0, 0, 0)
                : new DeliveryCompletionSummary(0, 1, 0, 0, 0));
        }
    }

    private sealed class ThrowingPublisher : IServerMessagePublisher
    {
        public ValueTask<DeliveryResult> PublishAsync(
            ServerOutboxEnvelope message,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Synthetic post-handoff failure.");
    }
}
