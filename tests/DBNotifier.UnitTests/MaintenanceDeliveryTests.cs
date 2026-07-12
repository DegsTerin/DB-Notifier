using DBNotifier.Application.Operations;
using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.UnitTests;

public sealed class MaintenanceDeliveryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 18, 0, 0, TimeSpan.Zero);

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
    public async Task ServerRetentionPreservesReferencedAndUnpublishedEvidence()
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

        Assert.Equal(1, result.Observations);
        Assert.Equal(1, result.Heartbeats);
        Assert.Equal(1, result.NotificationDeliveries);
        Assert.Equal(1, result.ServerOutboxTombstones);
        await using ServerDbContext verification = new(options);
        Assert.Equal(referenced.ObservationId, (await verification.HealthSamples.SingleAsync()).ObservationId);
        Assert.Empty(await verification.AgentHeartbeats.ToArrayAsync());
        Assert.Empty(await verification.NotificationDeliveries.ToArrayAsync());
        Assert.Null((await verification.OutboxMessages.SingleAsync()).PublishedAt);
        Assert.Single(await verification.AuditEntries.ToArrayAsync());
    }

    [Fact]
    public async Task ServerOutboxDeliveryPersistsPublishedAndRetryableOutcomes()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        ServerOutboxMessageRow first = ServerOutbox(Now, null);
        ServerOutboxMessageRow second = ServerOutbox(Now.AddSeconds(-1), null);
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.OutboxMessages.AddRange(first, second);
            await setup.SaveChangesAsync();
        }

        ServerMaintenanceStore store = new(new ServerContextFactory(options));
        ServerOutboxDeliveryRunner runner = new(store, new SplitPublisher(first.MessageId), new FixedTimeProvider(Now));

        DeliveryCycleResult result = await runner.RunOnceAsync(10);

        Assert.Equal(new DeliveryCycleResult(2, 1, 1), result);
        await using ServerDbContext verification = new(options);
        ServerOutboxMessageRow delivered = await verification.OutboxMessages.SingleAsync(
            row => row.MessageId == first.MessageId);
        ServerOutboxMessageRow retryable = await verification.OutboxMessages.SingleAsync(
            row => row.MessageId == second.MessageId);
        Assert.Equal(Now, delivered.PublishedAt);
        Assert.Null(retryable.PublishedAt);
        Assert.True(retryable.AvailableAt > Now);
        ServerOutboxMessageRow[] rows = [delivered, retryable];
        Assert.All(rows, row => Assert.Equal(1, row.AttemptCount));
    }

    [Fact]
    public async Task NotificationDeliveryBacksOffWithoutAdapterThenDeliversOnce()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        EventRecordRow eventRow = Event(null, null, null);
        NotificationChannelRow channel = Channel();
        NotificationDeliveryRow delivery = Delivery(channel.NotificationChannelId, eventRow.EventId, null);
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Events.Add(eventRow);
            setup.NotificationChannels.Add(channel);
            setup.NotificationDeliveries.Add(delivery);
            await setup.SaveChangesAsync();
        }

        ServerMaintenanceStore store = new(new ServerContextFactory(options));
        NotificationDeliveryRunner unavailable = new(store, [], new FixedTimeProvider(Now));
        DeliveryCycleResult first = await unavailable.RunOnceAsync(10);
        NotificationDeliveryRunner available = new(
            store,
            [new SuccessfulChannelAdapter(channel.ChannelType)],
            new FixedTimeProvider(Now.AddSeconds(3)));
        DeliveryCycleResult second = await available.RunOnceAsync(10);

        Assert.Equal(new DeliveryCycleResult(1, 0, 1), first);
        Assert.Equal(new DeliveryCycleResult(1, 1, 0), second);
        await using ServerDbContext verification = new(options);
        NotificationDeliveryRow stored = await verification.NotificationDeliveries.SingleAsync();
        Assert.Equal("Delivered", stored.State);
        Assert.Equal(2, stored.AttemptCount);
        Assert.Equal(Now.AddSeconds(3), stored.DeliveredAt);
        Assert.Null(stored.ErrorCode);
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
        CapabilityId = "fixture.capability",
        TypedParametersJson = "{}",
        State = state,
        RequestedAt = Now.AddDays(-40),
        ExpiresAt = Now.AddDays(-39),
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
        DateTimeOffset? deliveredAt) => new()
        {
            NotificationDeliveryId = Guid.NewGuid(),
            NotificationChannelId = channelId,
            EventId = eventId,
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

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
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
}
