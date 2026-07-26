// Module purpose: Verifies Synchronization Tests behaviour and protects the documented project contract.
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DBNotifier.Agent.Worker;
using DBNotifier.Application.Synchronization;
using DBNotifier.Domain;
using DBNotifier.Infrastructure.Synchronization;
using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Server.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DBNotifier.UnitTests;

public sealed class SynchronizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 11, 18, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task DispatchRunnerAcknowledgesTerminalResultsAndBacksOffRetryableItems()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (AgentDbContext setup = new(options))
        {
            await setup.Database.MigrateAsync();
            setup.OutboxMessages.AddRange(Outbox(1), Outbox(2));
            await setup.SaveChangesAsync();
        }

        TestAgentContextFactory factory = new(options);
        AgentOutboxStore store = new(factory);
        RecordingTransport transport = new();
        AgentOutboxDispatchRunner runner = new(Guid.NewGuid(), store, transport, new FixedTimeProvider(Now));

        AgentOutboxDispatchResult result = await runner.RunOnceAsync(50);

        Assert.Equal(2, result.PendingCount);
        Assert.Equal(1, result.AcknowledgedCount);
        Assert.Equal(1, result.RetryableCount);
        await using AgentDbContext verification = new(options);
        AgentOutboxMessageRow[] rows = await verification.OutboxMessages.OrderBy(row => row.Sequence).ToArrayAsync();
        Assert.Equal(Now, rows[0].AcknowledgedAt);
        Assert.Null(rows[1].AcknowledgedAt);
        Assert.True(rows[1].AvailableAt > Now);
        Assert.All(rows, row => Assert.Equal(1, row.AttemptCount));
    }

    [Fact]
    public async Task OutboxBlocksLaterSequenceUntilHeadIsDueAndRetainsRejectedTombstone()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connection)
            .Options;
        AgentOutboxMessageRow head = Outbox(1);
        head.AvailableAt = Now.AddMinutes(1);
        AgentOutboxMessageRow next = Outbox(2);
        await using (AgentDbContext setup = new(options))
        {
            await setup.Database.MigrateAsync();
            setup.OutboxMessages.AddRange(head, next);
            await setup.SaveChangesAsync();
        }

        AgentOutboxStore store = new(new TestAgentContextFactory(options));

        Assert.Empty(await store.GetPendingAsync(Now, 50, CancellationToken.None));
        await store.ApplyResultsAsync(
            [new ObservationItemResult(head.MessageId, ObservationIngestionDisposition.Rejected)],
            Now,
            CancellationToken.None);
        AgentOutboxEnvelope released = Assert.Single(
            await store.GetPendingAsync(Now, 50, CancellationToken.None));

        Assert.Equal(next.MessageId, released.MessageId);
        await using AgentDbContext verification = new(options);
        AgentOutboxMessageRow tombstone = await verification.OutboxMessages.SingleAsync(
            row => row.MessageId == head.MessageId);
        Assert.Equal(Now, tombstone.AcknowledgedAt);
    }

    [Fact]
    public async Task DispatchRunnerRetriesItemMissingFromServerResponse()
    {
        AgentOutboxMessageRow row = Outbox(1);
        InMemoryOutboxStore store = new(row);
        AgentOutboxDispatchRunner runner = new(
            Guid.NewGuid(),
            store,
            new EmptyResponseTransport(),
            new FixedTimeProvider(Now));

        AgentOutboxDispatchResult result = await runner.RunOnceAsync(50);

        Assert.Equal(1, result.RetryableCount);
        ObservationItemResult applied = Assert.Single(store.AppliedResults);
        Assert.Equal(row.MessageId, applied.MessageId);
        Assert.Equal(ObservationIngestionDisposition.Retryable, applied.Disposition);
        Assert.Equal("sync.response_missing", applied.ErrorCode);
    }

    [Fact]
    public async Task DispatchRunnerRetriesEntireAmbiguousServerResponse()
    {
        AgentOutboxMessageRow row = Outbox(1);
        InMemoryOutboxStore store = new(row);
        AgentOutboxDispatchRunner runner = new(
            Guid.NewGuid(),
            store,
            new AmbiguousResponseTransport(),
            new FixedTimeProvider(Now));

        AgentOutboxDispatchResult result = await runner.RunOnceAsync(50);

        Assert.Equal(1, result.RetryableCount);
        ObservationItemResult applied = Assert.Single(store.AppliedResults);
        Assert.Equal(row.MessageId, applied.MessageId);
        Assert.Equal(ObservationIngestionDisposition.Retryable, applied.Disposition);
        Assert.Equal("sync.response_invalid", applied.ErrorCode);
    }

    /// <summary>Proves that a rejection without a covering Server high-water mark remains retryable locally.</summary>
    [Fact]
    public async Task DispatchRunnerRetriesRejectedItemWithoutContiguousServerProof()
    {
        AgentOutboxMessageRow row = Outbox(1);
        InMemoryOutboxStore store = new(row);
        AgentOutboxDispatchRunner runner = new(
            Guid.NewGuid(),
            store,
            new RejectedResponseTransport(0),
            new FixedTimeProvider(Now));

        AgentOutboxDispatchResult result = await runner.RunOnceAsync(50);

        Assert.Equal(0, result.AcknowledgedCount);
        Assert.Equal(1, result.RetryableCount);
        ObservationItemResult applied = Assert.Single(store.AppliedResults);
        Assert.Equal(ObservationIngestionDisposition.Retryable, applied.Disposition);
        Assert.Equal("sync.rejection_unconfirmed", applied.ErrorCode);
    }

    /// <summary>Proves that a rejection becomes terminal only after the Server high-water mark covers its sequence.</summary>
    [Fact]
    public async Task DispatchRunnerAcknowledgesRejectedItemAfterContiguousServerProof()
    {
        AgentOutboxMessageRow row = Outbox(1);
        InMemoryOutboxStore store = new(row);
        AgentOutboxDispatchRunner runner = new(
            Guid.NewGuid(),
            store,
            new RejectedResponseTransport(1),
            new FixedTimeProvider(Now));

        AgentOutboxDispatchResult result = await runner.RunOnceAsync(50);

        Assert.Equal(1, result.AcknowledgedCount);
        Assert.Equal(0, result.RetryableCount);
        ObservationItemResult applied = Assert.Single(store.AppliedResults);
        Assert.Equal(ObservationIngestionDisposition.Rejected, applied.Disposition);
        Assert.Equal("observation.payload_invalid", applied.ErrorCode);
    }

    [Fact]
    public async Task ServerIngestionIsIdempotentAndCreatesCanonicalEventsAndAlertDeliveries()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        Guid ruleId = Guid.NewGuid();
        Guid boundChannelId = Guid.NewGuid();
        Guid outOfScopeRuleId = Guid.NewGuid();
        Guid outOfScopeChannelId = Guid.NewGuid();
        Guid wrongEnvironmentChannelId = Guid.NewGuid();
        Guid unboundChannelId = Guid.NewGuid();
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            setup.AlertRules.Add(new AlertRuleRow
            {
                AlertRuleId = ruleId,
                Name = "Connectivity events",
                InstanceScopeJson = $"{{\"instanceIds\":[\"{instanceId:D}\"]}}",
                RuleType = "CanonicalEvent",
                ConfigurationJson = "{\"eventTypes\":[\"Connected\",\"Timeout\"]}",
                Enabled = true,
                CreatedAt = Now,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.NotificationChannels.Add(new NotificationChannelRow
            {
                NotificationChannelId = boundChannelId,
                Name = "Fixture channel",
                ChannelType = "test",
                NonSecretConfigurationJson = "{}",
                Enabled = true,
                CreatedAt = Now,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.NotificationChannels.Add(new NotificationChannelRow
            {
                NotificationChannelId = outOfScopeChannelId,
                Name = "Out-of-scope fixture channel",
                ChannelType = "test",
                NonSecretConfigurationJson = "{}",
                Enabled = true,
                CreatedAt = Now,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.NotificationChannels.AddRange(
                new NotificationChannelRow
                {
                    NotificationChannelId = wrongEnvironmentChannelId,
                    Name = "Wrong-environment fixture channel",
                    ChannelType = "test",
                    NonSecretConfigurationJson = "{}",
                    Enabled = true,
                    CreatedAt = Now,
                    UpdatedAt = Now,
                    ConcurrencyToken = Guid.NewGuid(),
                },
                new NotificationChannelRow
                {
                    NotificationChannelId = unboundChannelId,
                    Name = "Unbound fixture channel",
                    ChannelType = "test",
                    NonSecretConfigurationJson = "{}",
                    Enabled = true,
                    CreatedAt = Now,
                    UpdatedAt = Now,
                    ConcurrencyToken = Guid.NewGuid(),
                });
            setup.AlertRules.Add(new AlertRuleRow
            {
                AlertRuleId = outOfScopeRuleId,
                Name = "Other instance connectivity events",
                InstanceScopeJson = $"{{\"instanceIds\":[\"{Guid.NewGuid():D}\"]}}",
                RuleType = "CanonicalEvent",
                ConfigurationJson = "{\"eventTypes\":[\"Connected\",\"Timeout\"]}",
                Enabled = true,
                CreatedAt = Now,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.AlertRuleChannelBindings.Add(new AlertRuleChannelBindingRow
            {
                AlertRuleChannelBindingId = Guid.NewGuid(),
                AlertRuleId = ruleId,
                NotificationChannelId = boundChannelId,
                Environment = "test",
                Enabled = true,
                CreatedAt = Now,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.AlertRuleChannelBindings.Add(new AlertRuleChannelBindingRow
            {
                AlertRuleChannelBindingId = Guid.NewGuid(),
                AlertRuleId = ruleId,
                NotificationChannelId = wrongEnvironmentChannelId,
                Environment = "production",
                Enabled = true,
                CreatedAt = Now,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.AlertRuleChannelBindings.Add(new AlertRuleChannelBindingRow
            {
                AlertRuleChannelBindingId = Guid.NewGuid(),
                AlertRuleId = outOfScopeRuleId,
                NotificationChannelId = outOfScopeChannelId,
                Environment = "test",
                Enabled = true,
                CreatedAt = Now,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationSyncMessage healthy = Message(agentId, instanceId, 1, "Healthy");
        ObservationItemResult accepted = await store.IngestAsync(healthy, Now, CancellationToken.None);
        ObservationItemResult duplicate = await store.IngestAsync(healthy, Now.AddSeconds(1), CancellationToken.None);
        ObservationItemResult timeout = await store.IngestAsync(
            Message(agentId, instanceId, 2, "Timeout"),
            Now.AddSeconds(2),
            CancellationToken.None);
        ObservationItemResult unchangedTimeout = await store.IngestAsync(
            Message(agentId, instanceId, 3, "Timeout"),
            Now.AddSeconds(3),
            CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Accepted, accepted.Disposition);
        Assert.Equal(ObservationIngestionDisposition.Duplicate, duplicate.Disposition);
        Assert.Equal(ObservationIngestionDisposition.Accepted, timeout.Disposition);
        Assert.Equal(ObservationIngestionDisposition.Accepted, unchangedTimeout.Disposition);
        Assert.Equal(3, await store.GetHighestContiguousSequenceAsync(agentId, CancellationToken.None));
        await using ServerDbContext verification = new(options);
        Assert.Equal(3, await verification.HealthSamples.CountAsync());
        string[] eventTypes = (await verification.Events.Select(row => row.EventType).ToArrayAsync())
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Connected", "Timeout"], eventTypes);
        Assert.Equal(2, await verification.NotificationDeliveries.CountAsync());
        Assert.All(
            await verification.NotificationDeliveries.ToArrayAsync(),
            delivery =>
            {
                Assert.Equal(boundChannelId, delivery.NotificationChannelId);
                Assert.NotNull(delivery.AlertRuleChannelBindingId);
                Assert.StartsWith("notification:", delivery.IdempotencyKey, StringComparison.Ordinal);
            });
        Assert.Equal(2, await verification.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task ServerIngestionRejectsConflictingAgentSequenceWithoutRetryLoop()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationItemResult accepted = await store.IngestAsync(
            Message(agentId, instanceId, 1, "Healthy"),
            Now,
            CancellationToken.None);
        ObservationItemResult conflict = await store.IngestAsync(
            Message(agentId, instanceId, 1, "Timeout"),
            Now.AddSeconds(1),
            CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Accepted, accepted.Disposition);
        Assert.Equal(ObservationIngestionDisposition.Rejected, conflict.Disposition);
        Assert.Equal("observation.sequence_conflict", conflict.ErrorCode);
        await using ServerDbContext verification = new(options);
        Assert.Equal(1, await verification.HealthSamples.CountAsync());
    }

    [Fact]
    public async Task ServerIngestionRejectsAlteredReplayWithSameIdentifiers()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationSyncMessage original = Message(agentId, instanceId, 1, "Healthy");
        Assert.Equal(
            ObservationIngestionDisposition.Accepted,
            (await store.IngestAsync(original, Now, CancellationToken.None)).Disposition);

        ObservationItemResult altered = await store.IngestAsync(
            original with { Status = "Timeout", ErrorCode = "probe.timeout", SafeErrorMessage = "Probe timed out." },
            Now.AddSeconds(1),
            CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Rejected, altered.Disposition);
        Assert.Equal("observation.idempotency_conflict", altered.ErrorCode);
        await using ServerDbContext verification = new(options);
        Assert.Equal("Healthy", (await verification.HealthSamples.SingleAsync()).Status);
    }

    [Fact]
    public async Task ServerIngestionRejectsInactiveAgentBeforeInstanceEvaluation()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationItemResult result = await store.IngestAsync(
            Message(Guid.NewGuid(), Guid.NewGuid(), 1, "Healthy"),
            Now,
            CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Rejected, result.Disposition);
        Assert.Equal("agent.not_active", result.ErrorCode);
    }

    [Fact]
    public async Task ServerIngestionRejectsInstanceNotAssignedToAgent()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationItemResult result = await store.IngestAsync(
            Message(agentId, Guid.NewGuid(), 1, "Healthy"),
            Now,
            CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Rejected, result.Disposition);
        Assert.Equal("observation.instance_not_assigned", result.ErrorCode);
    }

    [Fact]
    public async Task BatchIngestorRejectsAgentMismatchBeforePersistence()
    {
        Guid routeAgentId = Guid.NewGuid();
        RecordingIngestionStore store = new();
        ObservationBatchIngestor ingestor = new(store, new FixedTimeProvider(Now));

        ObservationBatchResult result = await ingestor.HandleAsync(new ObservationBatchRequest(
            routeAgentId,
            [Message(Guid.NewGuid(), Guid.NewGuid(), 1, "Healthy")]));

        ObservationItemResult item = Assert.Single(result.Items);
        Assert.Equal(ObservationIngestionDisposition.Rejected, item.Disposition);
        Assert.Equal("observation.envelope_invalid", item.ErrorCode);
        Assert.Equal(0, store.IngestCalls);
    }

    [Theory]
    [InlineData("999", "ProviderAuthenticated")]
    [InlineData("Healthy", "999")]
    [InlineData("Healthy", "TransportOnly")]
    public async Task BatchIngestorRejectsUndefinedOrContradictoryHealthEvidence(
        string status,
        string evidenceLevel)
    {
        Guid agentId = Guid.NewGuid();
        RecordingIngestionStore store = new();
        ObservationBatchIngestor ingestor = new(store, new FixedTimeProvider(Now));
        ObservationSyncMessage message = Message(agentId, Guid.NewGuid(), 1, status) with
        {
            EvidenceLevel = evidenceLevel,
        };

        ObservationBatchResult result = await ingestor.HandleAsync(new ObservationBatchRequest(agentId, [message]));

        ObservationItemResult rejected = Assert.Single(result.Items);
        Assert.Equal(ObservationIngestionDisposition.Rejected, rejected.Disposition);
        Assert.Equal("observation.payload_invalid", rejected.ErrorCode);
        Assert.Equal(0, store.IngestCalls);
    }

    [Fact]
    public async Task BatchIngestorAcceptsObservationAtFutureSkewBoundary()
    {
        Guid agentId = Guid.NewGuid();
        RecordingIngestionStore store = new();
        ObservationBatchIngestor ingestor = new(store, new FixedTimeProvider(Now));
        ObservationSyncMessage message = Message(agentId, Guid.NewGuid(), 1, "Healthy") with
        {
            ObservedAt = Now.AddMinutes(5),
        };

        ObservationBatchResult result = await ingestor.HandleAsync(new ObservationBatchRequest(agentId, [message]));

        Assert.Equal(ObservationIngestionDisposition.Accepted, Assert.Single(result.Items).Disposition);
        Assert.Equal(1, store.IngestCalls);
    }

    [Fact]
    public async Task BatchIngestorAcceptsOldBacklogWithoutAgeCutoff()
    {
        Guid agentId = Guid.NewGuid();
        RecordingIngestionStore store = new();
        ObservationBatchIngestor ingestor = new(store, new FixedTimeProvider(Now));
        ObservationSyncMessage message = Message(agentId, Guid.NewGuid(), 1, "Healthy") with
        {
            ObservedAt = Now.AddYears(-10),
        };

        ObservationBatchResult result = await ingestor.HandleAsync(new ObservationBatchRequest(agentId, [message]));

        Assert.Equal(ObservationIngestionDisposition.Accepted, Assert.Single(result.Items).Disposition);
        Assert.Equal(1, store.IngestCalls);
    }

    [Fact]
    public async Task BatchIngestorTerminallyRejectsObservationFarInFuture()
    {
        Guid agentId = Guid.NewGuid();
        RecordingIngestionStore store = new();
        ObservationBatchIngestor ingestor = new(store, new FixedTimeProvider(Now));
        ObservationSyncMessage message = Message(agentId, Guid.NewGuid(), 1, "Healthy") with
        {
            ObservedAt = Now.AddYears(10),
        };

        ObservationBatchResult result = await ingestor.HandleAsync(new ObservationBatchRequest(agentId, [message]));

        ObservationItemResult rejected = Assert.Single(result.Items);
        Assert.Equal(ObservationIngestionDisposition.Rejected, rejected.Disposition);
        Assert.Equal("observation.observed_at_future", rejected.ErrorCode);
        Assert.Equal(0, store.IngestCalls);
    }

    /// <summary>
    /// Proves that a terminal validation rejection closes its stream slot, releases a stored successor and remains
    /// idempotent without creating health evidence.
    /// </summary>
    [Fact]
    public async Task RejectedValidationSequenceReleasesStoredSuccessorWithoutHealthEvidence()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationBatchIngestor ingestor = new(store, new FixedTimeProvider(Now));
        Assert.Equal(
            ObservationIngestionDisposition.Accepted,
            (await store.IngestAsync(
                Message(agentId, instanceId, 2, "Healthy"),
                Now,
                CancellationToken.None)).Disposition);
        ObservationSyncMessage rejectedMessage = Message(agentId, instanceId, 1, "Healthy") with
        {
            AttemptCount = 0,
        };

        ObservationBatchResult rejected = await ingestor.HandleAsync(
            new ObservationBatchRequest(agentId, [rejectedMessage]));
        ObservationBatchResult replay = await ingestor.HandleAsync(
            new ObservationBatchRequest(agentId, [rejectedMessage]));
        ObservationBatchResult retroactive = await ingestor.HandleAsync(
            new ObservationBatchRequest(agentId, [Message(agentId, instanceId, 1, "Timeout")]));

        ObservationItemResult rejectedItem = Assert.Single(rejected.Items);
        Assert.Equal(ObservationIngestionDisposition.Rejected, rejectedItem.Disposition);
        Assert.Equal("observation.payload_invalid", rejectedItem.ErrorCode);
        Assert.Equal(2, rejected.HighestContiguousSequence);
        Assert.Equal(ObservationIngestionDisposition.Rejected, Assert.Single(replay.Items).Disposition);
        Assert.Equal(2, replay.HighestContiguousSequence);
        ObservationItemResult retroactiveItem = Assert.Single(retroactive.Items);
        Assert.Equal(ObservationIngestionDisposition.Rejected, retroactiveItem.Disposition);
        Assert.Equal("observation.sequence_conflict", retroactiveItem.ErrorCode);
        Assert.Equal(2, retroactive.HighestContiguousSequence);

        await using ServerDbContext verification = new(options);
        HealthSampleRow sample = await verification.HealthSamples.SingleAsync();
        Assert.Equal(2, sample.Sequence);
        RejectedObservationSequenceRow ledger = await verification.RejectedObservationSequences.SingleAsync();
        Assert.Equal(agentId, ledger.AgentId);
        Assert.Equal(1, ledger.Sequence);
        Assert.Equal(rejectedMessage.MessageId, ledger.MessageId);
        Assert.Equal("observation.payload_invalid", ledger.ErrorCode);
        Assert.Equal(Now, ledger.ConsumedAt);
        InstanceObservationStateRow state = await verification.InstanceObservationStates.SingleAsync();
        Assert.Equal(2, state.LastProcessedSequence);
        Assert.Equal("Connected", (await verification.Events.SingleAsync()).EventType);
        Assert.Single(await verification.OutboxMessages.ToArrayAsync());
        Assert.Empty(await verification.NotificationDeliveries.ToArrayAsync());
    }

    /// <summary>
    /// Proves the durable rejection ledger owns the original reason, detects a conflicting slot identity and records
    /// a repeated message identifier as a separate terminally resolved stream position.
    /// </summary>
    [Fact]
    public async Task RejectedSequenceLedgerPreservesOriginalReasonAndConflictEvidence()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationItemResult first = await store.ConsumeRejectedAsync(
            agentId,
            messageId,
            1,
            "fixture.original_rejection",
            Now,
            CancellationToken.None);
        ObservationItemResult replay = await store.ConsumeRejectedAsync(
            agentId,
            messageId,
            1,
            "fixture.changed_rejection",
            Now.AddSeconds(1),
            CancellationToken.None);
        ObservationItemResult conflictingSlot = await store.ConsumeRejectedAsync(
            agentId,
            Guid.NewGuid(),
            1,
            "fixture.conflicting_slot",
            Now.AddSeconds(2),
            CancellationToken.None);
        ObservationItemResult repeatedMessage = await store.ConsumeRejectedAsync(
            agentId,
            messageId,
            2,
            "fixture.second_slot",
            Now.AddSeconds(3),
            CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Rejected, first.Disposition);
        Assert.Equal("fixture.original_rejection", first.ErrorCode);
        Assert.Equal("fixture.original_rejection", replay.ErrorCode);
        Assert.Equal("observation.sequence_conflict", conflictingSlot.ErrorCode);
        Assert.Equal("observation.idempotency_conflict", repeatedMessage.ErrorCode);
        Assert.Equal(2, await store.GetHighestContiguousSequenceAsync(agentId, CancellationToken.None));

        await using ServerDbContext verification = new(options);
        RejectedObservationSequenceRow[] ledger = await verification.RejectedObservationSequences
            .OrderBy(row => row.Sequence)
            .ToArrayAsync();
        Assert.Equal(2, ledger.Length);
        Assert.All(ledger, row => Assert.Equal(messageId, row.MessageId));
        Assert.Equal("fixture.original_rejection", ledger[0].ErrorCode);
        Assert.Equal(Now, ledger[0].ConsumedAt);
        Assert.Equal("observation.idempotency_conflict", ledger[1].ErrorCode);
        Assert.Equal(Now.AddSeconds(3), ledger[1].ConsumedAt);
        Assert.Empty(await verification.HealthSamples.ToArrayAsync());
        Assert.Empty(await verification.InstanceObservationStates.ToArrayAsync());
        Assert.Empty(await verification.Events.ToArrayAsync());
        Assert.Empty(await verification.OutboxMessages.ToArrayAsync());
        Assert.Empty(await verification.NotificationDeliveries.ToArrayAsync());
    }

    /// <summary>Distinguishes a factual pre-ledger cursor from missing evidence after the durable cutover.</summary>
    /// <param name="ledgerStart">Inclusive per-Agent ledger cutover stored with the cursor.</param>
    /// <param name="expectedDisposition">Expected fail-closed replay classification.</param>
    /// <param name="expectedErrorCode">Expected stable replay code.</param>
    [Theory]
    [InlineData(2L, ObservationIngestionDisposition.Rejected, "observation.sequence_conflict")]
    [InlineData(1L, ObservationIngestionDisposition.Retryable, "ingestion.rejection_ledger_inconsistent")]
    public async Task CoveredSequenceWithoutEvidenceHonoursLedgerCutover(
        long ledgerStart,
        ObservationIngestionDisposition expectedDisposition,
        string expectedErrorCode)
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        ObservationSyncMessage acceptedReplay = Message(agentId, instanceId, 1, "Healthy");
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            setup.AgentObservationCursors.Add(new AgentObservationCursorRow
            {
                AgentId = agentId,
                HighestContiguousSequence = 1,
                RejectionLedgerStartSequence = ledgerStart,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationItemResult result = await store.ConsumeRejectedAsync(
            agentId,
            Guid.NewGuid(),
            1,
            "fixture.rejection",
            Now.AddSeconds(1),
            CancellationToken.None);
        ObservationItemResult acceptedResult = await store.IngestAsync(
            acceptedReplay,
            Now.AddSeconds(1),
            CancellationToken.None);

        Assert.Equal(expectedDisposition, result.Disposition);
        Assert.Equal(expectedErrorCode, result.ErrorCode);
        Assert.Equal(expectedDisposition, acceptedResult.Disposition);
        Assert.Equal(expectedErrorCode, acceptedResult.ErrorCode);
        await using ServerDbContext verification = new(options);
        Assert.Empty(await verification.RejectedObservationSequences.ToArrayAsync());
        Assert.Empty(await verification.HealthSamples.ToArrayAsync());
        Assert.Equal(1, (await verification.AgentObservationCursors.SingleAsync()).HighestContiguousSequence);
    }

    /// <summary>Fails closed when a rejection-ledger row exists ahead of the durable cursor.</summary>
    [Fact]
    public async Task RejectionLedgerEvidenceAheadOfCursorIsInconsistent()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        ObservationSyncMessage message = Message(agentId, instanceId, 1, "Healthy");
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            setup.AgentObservationCursors.Add(new AgentObservationCursorRow
            {
                AgentId = agentId,
                HighestContiguousSequence = 0,
                RejectionLedgerStartSequence = 1,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.RejectedObservationSequences.Add(new RejectedObservationSequenceRow
            {
                AgentId = agentId,
                Sequence = 1,
                MessageId = message.MessageId,
                ErrorCode = "fixture.impossible_rejection",
                ConsumedAt = Now,
            });
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationItemResult acceptedPath = await store.IngestAsync(
            message,
            Now.AddSeconds(1),
            CancellationToken.None);
        ObservationItemResult rejectedPath = await store.ConsumeRejectedAsync(
            agentId,
            message.MessageId,
            1,
            "fixture.rejection",
            Now.AddSeconds(1),
            CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Retryable, acceptedPath.Disposition);
        Assert.Equal("ingestion.rejection_ledger_inconsistent", acceptedPath.ErrorCode);
        Assert.Equal(ObservationIngestionDisposition.Retryable, rejectedPath.Disposition);
        Assert.Equal("ingestion.rejection_ledger_inconsistent", rejectedPath.ErrorCode);
        await using ServerDbContext verification = new(options);
        Assert.Empty(await verification.HealthSamples.ToArrayAsync());
        Assert.Equal(0, (await verification.AgentObservationCursors.SingleAsync()).HighestContiguousSequence);
    }

    /// <summary>Refuses the final signed 64-bit slot because no safe successor or ledger cutover can represent it.</summary>
    [Fact]
    public async Task MaximumSequenceFailsClosedWithoutDurableEffects()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        ObservationSyncMessage message = Message(agentId, instanceId, 1, "Healthy") with
        {
            Sequence = long.MaxValue,
        };
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            setup.AgentObservationCursors.Add(new AgentObservationCursorRow
            {
                AgentId = agentId,
                HighestContiguousSequence = long.MaxValue - 1,
                RejectionLedgerStartSequence = 1,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationItemResult acceptedPath = await store.IngestAsync(
            message,
            Now.AddSeconds(1),
            CancellationToken.None);
        ObservationItemResult rejectedPath = await store.ConsumeRejectedAsync(
            agentId,
            message.MessageId,
            long.MaxValue,
            "fixture.rejection",
            Now.AddSeconds(1),
            CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Retryable, acceptedPath.Disposition);
        Assert.Equal("ingestion.sequence_exhausted", acceptedPath.ErrorCode);
        Assert.Equal(ObservationIngestionDisposition.Retryable, rejectedPath.Disposition);
        Assert.Equal("ingestion.sequence_exhausted", rejectedPath.ErrorCode);
        await using ServerDbContext verification = new(options);
        Assert.Empty(await verification.HealthSamples.ToArrayAsync());
        Assert.Empty(await verification.RejectedObservationSequences.ToArrayAsync());
        Assert.Equal(
            long.MaxValue - 1,
            (await verification.AgentObservationCursors.SingleAsync()).HighestContiguousSequence);
    }

    /// <summary>Fails closed when accepted and rejected evidence coexist for the same Agent stream position.</summary>
    [Fact]
    public async Task AcceptedAndRejectedEvidenceForOneSlotIsInconsistent()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        ObservationSyncMessage accepted = Message(agentId, instanceId, 1, "Healthy");
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            setup.AgentObservationCursors.Add(new AgentObservationCursorRow
            {
                AgentId = agentId,
                HighestContiguousSequence = 1,
                RejectionLedgerStartSequence = 1,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.HealthSamples.Add(StoredSample(accepted, Now));
            setup.RejectedObservationSequences.Add(new RejectedObservationSequenceRow
            {
                AgentId = agentId,
                Sequence = 1,
                MessageId = accepted.MessageId,
                ErrorCode = "fixture.impossible_rejection",
                ConsumedAt = Now,
            });
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationItemResult rejectedPath = await store.ConsumeRejectedAsync(
            agentId,
            accepted.MessageId,
            1,
            "fixture.rejection",
            Now.AddSeconds(1),
            CancellationToken.None);
        ObservationItemResult acceptedPath = await store.IngestAsync(
            accepted,
            Now.AddSeconds(1),
            CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Retryable, rejectedPath.Disposition);
        Assert.Equal("ingestion.rejection_ledger_inconsistent", rejectedPath.ErrorCode);
        Assert.Equal(ObservationIngestionDisposition.Retryable, acceptedPath.Disposition);
        Assert.Equal("ingestion.rejection_ledger_inconsistent", acceptedPath.ErrorCode);
        await using ServerDbContext verification = new(options);
        Assert.Single(await verification.HealthSamples.ToArrayAsync());
        Assert.Single(await verification.RejectedObservationSequences.ToArrayAsync());
        Assert.Empty(await verification.InstanceObservationStates.ToArrayAsync());
        Assert.Empty(await verification.Events.ToArrayAsync());
    }

    /// <summary>
    /// Proves that a rejected sequence above a lower gap remains retryable until the missing prefix is reconciled.
    /// </summary>
    [Fact]
    public async Task RejectedSequenceAboveGapRemainsRetryableUntilPrefixArrives()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationBatchIngestor ingestor = new(store, new FixedTimeProvider(Now));
        ObservationSyncMessage rejectedMessage = Message(agentId, instanceId, 2, "Healthy") with
        {
            AttemptCount = 0,
        };

        ObservationBatchResult beforePrefix = await ingestor.HandleAsync(
            new ObservationBatchRequest(agentId, [rejectedMessage]));
        ObservationBatchResult prefix = await ingestor.HandleAsync(
            new ObservationBatchRequest(agentId, [Message(agentId, instanceId, 1, "Healthy")]));
        ObservationBatchResult afterPrefix = await ingestor.HandleAsync(
            new ObservationBatchRequest(agentId, [rejectedMessage]));

        ObservationItemResult beforePrefixItem = Assert.Single(beforePrefix.Items);
        Assert.Equal(ObservationIngestionDisposition.Retryable, beforePrefixItem.Disposition);
        Assert.Equal("ingestion.rejection_gap", beforePrefixItem.ErrorCode);
        Assert.Equal(0, beforePrefix.HighestContiguousSequence);
        Assert.Equal(ObservationIngestionDisposition.Accepted, Assert.Single(prefix.Items).Disposition);
        Assert.Equal(1, prefix.HighestContiguousSequence);
        ObservationItemResult afterPrefixItem = Assert.Single(afterPrefix.Items);
        Assert.Equal(ObservationIngestionDisposition.Rejected, afterPrefixItem.Disposition);
        Assert.Equal("observation.payload_invalid", afterPrefixItem.ErrorCode);
        Assert.Equal(2, afterPrefix.HighestContiguousSequence);

        await using ServerDbContext verification = new(options);
        HealthSampleRow sample = await verification.HealthSamples.SingleAsync();
        Assert.Equal(1, sample.Sequence);
        RejectedObservationSequenceRow ledger = await verification.RejectedObservationSequences.SingleAsync();
        Assert.Equal(2, ledger.Sequence);
        Assert.Equal(rejectedMessage.MessageId, ledger.MessageId);
        Assert.Equal("observation.payload_invalid", ledger.ErrorCode);
        Assert.Equal(1, (await verification.InstanceObservationStates.SingleAsync()).LastProcessedSequence);
        Assert.Single(await verification.Events.ToArrayAsync());
    }

    /// <summary>Proves that a provider-mismatch rejection releases an already stored valid successor.</summary>
    [Fact]
    public async Task ProviderMismatchRejectionReleasesStoredSuccessor()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        Assert.Equal(
            ObservationIngestionDisposition.Accepted,
            (await store.IngestAsync(
                Message(agentId, instanceId, 2, "Healthy"),
                Now,
                CancellationToken.None)).Disposition);

        ObservationItemResult rejection = await store.IngestAsync(
            Message(agentId, instanceId, 1, "Healthy") with { ProviderType = "mysql" },
            Now.AddSeconds(1),
            CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Rejected, rejection.Disposition);
        Assert.Equal("observation.provider_mismatch", rejection.ErrorCode);
        Assert.Equal(2, await store.GetHighestContiguousSequenceAsync(agentId, CancellationToken.None));
        await using ServerDbContext verification = new(options);
        HealthSampleRow sample = await verification.HealthSamples.SingleAsync();
        Assert.Equal(2, sample.Sequence);
        RejectedObservationSequenceRow ledger = await verification.RejectedObservationSequences.SingleAsync();
        Assert.Equal(1, ledger.Sequence);
        Assert.Equal(rejection.MessageId, ledger.MessageId);
        Assert.Equal("observation.provider_mismatch", ledger.ErrorCode);
        Assert.Equal(2, (await verification.InstanceObservationStates.SingleAsync()).LastProcessedSequence);
        Assert.Single(await verification.Events.ToArrayAsync());
    }

    [Fact]
    public async Task ServerReconcilesGappedObservationsInSequenceOrder()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));

        ObservationItemResult second = await store.IngestAsync(
            Message(agentId, instanceId, 2, "Healthy"), Now, CancellationToken.None);
        await using (ServerDbContext gapVerification = new(options))
        {
            Assert.Empty(await gapVerification.Events.ToArrayAsync());
            Assert.Equal(0, await store.GetHighestContiguousSequenceAsync(agentId, CancellationToken.None));
        }

        ObservationItemResult first = await store.IngestAsync(
            Message(agentId, instanceId, 1, "Timeout"), Now.AddSeconds(1), CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Accepted, second.Disposition);
        Assert.Equal(ObservationIngestionDisposition.Accepted, first.Disposition);
        Assert.Equal(2, await store.GetHighestContiguousSequenceAsync(agentId, CancellationToken.None));
        await using ServerDbContext verification = new(options);
        EventRecordRow[] events = await verification.Events.ToArrayAsync();
        Assert.Equal(
            ["Timeout", "Recovered"],
            events.OrderBy(row => row.ObservedAt).Select(row => row.EventType).ToArray());
        InstanceObservationStateRow state = await verification.InstanceObservationStates.SingleAsync();
        Assert.Equal("Healthy", state.Status);
        Assert.Equal(2, state.LastProcessedSequence);
    }

    [Fact]
    public async Task ServerReconciliationSkipsPendingSampleAfterInstanceReassignment()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentAId = Guid.NewGuid();
        Guid agentBId = Guid.NewGuid();
        Guid reassignedInstanceId = Guid.NewGuid();
        Guid agentAInstanceId = Guid.NewGuid();
        RegisteredAgentRow agentB = Agent(agentBId);
        agentB.InstallationId = "fixture-agent-b";
        agentB.CertificateThumbprint = "FIXTURE-B";
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.AddRange(Agent(agentAId), agentB);
            setup.Instances.AddRange(
                Instance(reassignedInstanceId, agentAId),
                Instance(agentAInstanceId, agentAId));
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationSyncMessage pendingFromAgentA = Message(agentAId, reassignedInstanceId, 2, "Timeout");
        ObservationItemResult pending = await store.IngestAsync(
            pendingFromAgentA,
            Now,
            CancellationToken.None);
        Assert.Equal(ObservationIngestionDisposition.Accepted, pending.Disposition);
        Assert.Equal(0, await store.GetHighestContiguousSequenceAsync(agentAId, CancellationToken.None));

        await using (ServerDbContext reassign = new(options))
        {
            DatabaseInstanceRow instance = await reassign.Instances.SingleAsync(
                row => row.InstanceId == reassignedInstanceId);
            instance.AssignedAgentId = agentBId;
            instance.ConcurrencyToken = Guid.NewGuid();
            await reassign.SaveChangesAsync();
        }

        ObservationSyncMessage currentFromAgentB = Message(agentBId, reassignedInstanceId, 1, "Healthy");
        Assert.Equal(
            ObservationIngestionDisposition.Accepted,
            (await store.IngestAsync(currentFromAgentB, Now.AddSeconds(1), CancellationToken.None)).Disposition);

        ObservationSyncMessage closesAgentAGap = Message(agentAId, agentAInstanceId, 1, "Unknown");
        Assert.Equal(
            ObservationIngestionDisposition.Accepted,
            (await store.IngestAsync(closesAgentAGap, Now.AddSeconds(2), CancellationToken.None)).Disposition);

        Assert.Equal(2, await store.GetHighestContiguousSequenceAsync(agentAId, CancellationToken.None));
        await using ServerDbContext verification = new(options);
        InstanceObservationStateRow state = await verification.InstanceObservationStates.SingleAsync(
            row => row.InstanceId == reassignedInstanceId);
        Assert.Equal(agentBId, state.AgentId);
        Assert.Equal(currentFromAgentB.ObservationId, state.ObservationId);
        Assert.Equal("Healthy", state.Status);
        EventRecordRow[] events = await verification.Events.ToArrayAsync();
        EventRecordRow canonicalEvent = Assert.Single(events);
        Assert.Equal(agentBId, canonicalEvent.AgentId);
        Assert.Equal(currentFromAgentB.ObservationId, canonicalEvent.SourceObservationId);
        Assert.DoesNotContain(
            events,
            row => row.SourceObservationId == pendingFromAgentA.ObservationId);
        Assert.Single(await verification.OutboxMessages.ToArrayAsync());
    }

    [Fact]
    public async Task ServerReconciliationContinuesAcrossBoundedQueryBatches()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            setup.HealthSamples.AddRange(Enumerable.Range(1, 1001).Select(sequence =>
                StoredSample(Message(agentId, instanceId, sequence, "Healthy"), Now)));
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        ObservationItemResult result = await store.IngestAsync(
            Message(agentId, instanceId, 1002, "Healthy"),
            Now.AddMinutes(1),
            CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Accepted, result.Disposition);
        Assert.Equal(1002, await store.GetHighestContiguousSequenceAsync(agentId, CancellationToken.None));
        await using ServerDbContext verification = new(options);
        Assert.Equal("Connected", (await verification.Events.SingleAsync()).EventType);
        Assert.Equal(1002, (await verification.InstanceObservationStates.SingleAsync()).LastProcessedSequence);
    }

    [Fact]
    public async Task ConcurrentAgentIngestionSerialisesCursorAndEmitsEachTransitionOnce()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            await setup.SaveChangesAsync();
        }

        ServerObservationIngestionStore store = new(new TestServerContextFactory(options));
        Task<ObservationItemResult> first = store
            .IngestAsync(Message(agentId, instanceId, 1, "Healthy"), Now, CancellationToken.None)
            .AsTask();
        Task<ObservationItemResult> second = store
            .IngestAsync(Message(agentId, instanceId, 2, "Timeout"), Now.AddSeconds(1), CancellationToken.None)
            .AsTask();

        ObservationItemResult[] results = await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.Equal(ObservationIngestionDisposition.Accepted, result.Disposition));
        Assert.Equal(2, await store.GetHighestContiguousSequenceAsync(agentId, CancellationToken.None));
        await using ServerDbContext verification = new(options);
        Assert.Equal(2, await verification.HealthSamples.CountAsync());
        EventRecordRow[] events = await verification.Events.ToArrayAsync();
        Assert.Equal(
            ["Connected", "Timeout"],
            events.OrderBy(row => row.ObservedAt).Select(row => row.EventType).ToArray());
    }

    [Fact]
    public async Task ProtectedTransportRejectsAuthorisedPlaintextRequest()
    {
        bool nextCalled = false;
        DBNotifier.Server.Api.Security.ProtectedTransportMiddleware middleware = new(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        DefaultHttpContext context = new();
        context.Request.Scheme = "http";
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute()),
            "protected-fixture"));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status426UpgradeRequired, context.Response.StatusCode);
        Assert.False(nextCalled);
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body);
        Assert.Contains("transport.https_required", await reader.ReadToEndAsync(), StringComparison.Ordinal);
    }

    /// <summary>Verifies that anonymous enrollment metadata cannot bypass the mandatory HTTPS boundary.</summary>
    [Fact]
    public async Task ProtectedTransportRejectsMarkedAnonymousPlaintextRequest()
    {
        bool nextCalled = false;
        ProtectedTransportMiddleware middleware = new(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        DefaultHttpContext context = new();
        context.Request.Scheme = "http";
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(
                new AllowAnonymousAttribute(),
                ProtectedTransportRequirement.Instance),
            "anonymous-enrollment-fixture"));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status426UpgradeRequired, context.Response.StatusCode);
        Assert.False(nextCalled);
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body);
        Assert.Contains("transport.https_required", await reader.ReadToEndAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProtectedTransportSeesAuthorisationMetadataAfterEndpointRouting()
    {
        await using ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddRouting()
            .AddSingleton(new DiagnosticListener("DBNotifier.UnitTests.Routing"))
            .BuildServiceProvider();
        ApplicationBuilder application = new(services);
        application.UseRouting();
        application.UseMiddleware<ProtectedTransportMiddleware>();
        application.UseEndpoints(endpoints =>
            endpoints.MapGet("/protected", static () => "unexpected").RequireAuthorization());
        RequestDelegate pipeline = application.Build();
        DefaultHttpContext context = new()
        {
            RequestServices = services,
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/protected";
        context.Request.Scheme = "http";
        context.Response.Body = new MemoryStream();

        await pipeline(context);

        Assert.NotNull(context.GetEndpoint());
        Assert.NotNull(context.GetEndpoint()!.Metadata.GetMetadata<IAuthorizeData>());
        Assert.Equal(StatusCodes.Status426UpgradeRequired, context.Response.StatusCode);
    }

    [Fact]
    public async Task HttpsPipelineChallengesMissingHumanAndAgentCredentialsWithCanonicalStatuses()
    {
        await using ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddRouting()
            .AddSingleton(new DiagnosticListener("DBNotifier.UnitTests.Authentication"))
            .AddAuthentication(ApiSecurityDefaults.RoutedAuthenticationScheme)
            .AddPolicyScheme(
                ApiSecurityDefaults.RoutedAuthenticationScheme,
                ApiSecurityDefaults.RoutedAuthenticationScheme,
                options => options.ForwardDefaultSelector = ApiSecurityDefaults.SelectAuthenticationScheme)
            .AddCertificate()
            .AddJwtBearer(HumanAuthenticationDefaults.Scheme)
            .Services
            .AddSingleton<IAuthorizationHandler, AgentRouteAuthorizationHandler>()
            .AddAuthorizationBuilder()
            .AddPolicy(ApiSecurityDefaults.AgentApiPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(CertificateAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new AgentRouteRequirement());
            })
            .AddPolicy(ApiSecurityDefaults.HumanApiPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(HumanAuthenticationDefaults.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("sub");
            })
            .Services
            .BuildServiceProvider();
        ApplicationBuilder application = new(services);
        application.UseRouting();
        application.UseMiddleware<ProtectedTransportMiddleware>();
        application.UseAuthentication();
        application.UseAuthorization();
        application.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/human", static () => "unexpected").RequireAuthorization(ApiSecurityDefaults.HumanApiPolicy);
            endpoints.MapGet("/agent/{agentId:guid}", static () => "unexpected").RequireAuthorization(ApiSecurityDefaults.AgentApiPolicy);
        });
        RequestDelegate pipeline = application.Build();

        DefaultHttpContext humanContext = HttpsContext(services, "/human");
        await pipeline(humanContext);
        DefaultHttpContext agentContext = HttpsContext(
            services,
            $"/agent/{Guid.NewGuid():D}");
        await pipeline(agentContext);

        Assert.Equal(StatusCodes.Status401Unauthorized, humanContext.Response.StatusCode);
        Assert.Equal(StatusCodes.Status403Forbidden, agentContext.Response.StatusCode);
    }

    [Fact]
    public async Task RoutedAgentAuthenticationPopulatesIdentityBeforeRateLimitingPosition()
    {
        await using ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddRouting()
            .AddSingleton(new DiagnosticListener("DBNotifier.UnitTests.AgentAuthentication"))
            .AddAuthentication(ApiSecurityDefaults.RoutedAuthenticationScheme)
            .AddPolicyScheme(
                ApiSecurityDefaults.RoutedAuthenticationScheme,
                ApiSecurityDefaults.RoutedAuthenticationScheme,
                options => options.ForwardDefaultSelector = ApiSecurityDefaults.SelectAuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, AgentFixtureAuthenticationHandler>(
                CertificateAuthenticationDefaults.AuthenticationScheme,
                _ => { })
            .Services
            .AddSingleton<IAuthorizationHandler, AgentRouteAuthorizationHandler>()
            .AddAuthorizationBuilder()
            .AddPolicy(ApiSecurityDefaults.AgentApiPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(CertificateAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new AgentRouteRequirement());
            })
            .Services
            .BuildServiceProvider();
        bool agentIdentityVisibleAtQuotaPosition = false;
        ApplicationBuilder application = new(services);
        application.UseRouting();
        application.UseAuthentication();
        application.Use(async (context, next) =>
        {
            agentIdentityVisibleAtQuotaPosition = context.User.FindFirst(AgentIdentityClaimTypes.AgentId) is not null;
            await next(context);
        });
        application.UseAuthorization();
        application.UseEndpoints(endpoints => endpoints
            .MapGet("/agent/{agentId:guid}", static () => "ok")
            .RequireAuthorization(ApiSecurityDefaults.AgentApiPolicy));
        RequestDelegate pipeline = application.Build();
        DefaultHttpContext context = HttpsContext(services, $"/agent/{Guid.NewGuid():D}");

        await pipeline(context);

        Assert.True(agentIdentityVisibleAtQuotaPosition);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task PublicLivenessDoesNotInvokeBearerAuthenticationForInvalidHeader()
    {
        await using ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddRouting()
            .AddSingleton(new DiagnosticListener("DBNotifier.UnitTests.PublicAuthentication"))
            .AddAuthentication(ApiSecurityDefaults.RoutedAuthenticationScheme)
            .AddPolicyScheme(
                ApiSecurityDefaults.RoutedAuthenticationScheme,
                ApiSecurityDefaults.RoutedAuthenticationScheme,
                options => options.ForwardDefaultSelector = ApiSecurityDefaults.SelectAuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, AgentFixtureAuthenticationHandler>(
                ApiSecurityDefaults.PublicEndpointAuthenticationScheme,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, UnexpectedAuthenticationHandler>(
                HumanAuthenticationDefaults.Scheme,
                _ => { })
            .Services
            .BuildServiceProvider();
        ApplicationBuilder application = new(services);
        application.UseRouting();
        application.UseAuthentication();
        application.UseEndpoints(endpoints => endpoints.MapGet("/health/live", static () => "alive"));
        RequestDelegate pipeline = application.Build();
        DefaultHttpContext context = HttpsContext(services, "/health/live");
        context.Request.Headers.Authorization = "Bearer invalid-fixture";

        await pipeline(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task AuthenticationAuditPersistsOnlyBoundedSanitisedEvidence()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        using AuthenticationAuditGate gate = new(10);
        AuthenticationAuditWriter writer = new(
            new TestServerContextFactory(options),
            new FixedTimeProvider(Now),
            gate,
            NullLogger<AuthenticationAuditWriter>.Instance);
        await writer.TryWriteAsync(
            new AuthenticationAuditEvent(
                "Agent",
                "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                "Certificate",
                "Succeeded",
                "authentication.agent_certificate_valid"),
            CancellationToken.None);

        await using ServerDbContext verification = new(options);
        AuditEntryRow row = await verification.AuditEntries.SingleAsync();
        Assert.Equal("authentication.validate", row.Action);
        Assert.Equal("AuthenticationScheme", row.TargetType);
        Assert.Equal("Certificate", row.TargetId);
        Assert.Equal("{\"code\":\"authentication.agent_certificate_valid\"}", row.DetailsJson);
    }

    [Fact]
    public async Task AuthenticationAuditRateLimitSuppressesDatabaseWorkAfterQuota()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        using AuthenticationAuditGate gate = new(1, new FixedTimeProvider(Now));
        Assert.Equal(AuthenticationAuditAdmission.Accepted, gate.Acquire());
        Assert.Equal(AuthenticationAuditAdmission.SuppressedAndReport, gate.Acquire());
        Assert.Equal(AuthenticationAuditAdmission.Suppressed, gate.Acquire());
        AuthenticationAuditWriter writer = new(
            new TestServerContextFactory(options),
            new FixedTimeProvider(Now),
            gate,
            NullLogger<AuthenticationAuditWriter>.Instance);
        await writer.TryWriteAsync(
            new AuthenticationAuditEvent(
                "Human",
                "unresolved",
                "Bearer",
                "Failed",
                "authentication.human_token_invalid"),
            CancellationToken.None);

        await using ServerDbContext verification = new(options);
        Assert.Empty(await verification.AuditEntries.ToArrayAsync());
    }

    [Fact]
    public async Task AgentRouteAuthorizationDeniesDifferentAgentIdentity()
    {
        Guid identityAgentId = Guid.NewGuid();
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues["agentId"] = Guid.NewGuid().ToString("D");
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [new Claim(AgentIdentityClaimTypes.AgentId, identityAgentId.ToString("D"))],
            "fixture"));
        AuthorizationHandlerContext authorizationContext = new(
            [new AgentRouteRequirement()],
            principal,
            httpContext);

        await new AgentRouteAuthorizationHandler().HandleAsync(authorizationContext);

        Assert.False(authorizationContext.HasSucceeded);
    }

    [Fact]
    public async Task AgentRouteAuthorizationAllowsOnlyMatchingAuthenticatedIdentity()
    {
        Guid agentId = Guid.NewGuid();
        DefaultHttpContext httpContext = new();
        httpContext.Request.RouteValues["agentId"] = agentId.ToString("D");
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [new Claim(AgentIdentityClaimTypes.AgentId, agentId.ToString("D"))],
            "fixture"));
        AuthorizationHandlerContext authorizationContext = new(
            [new AgentRouteRequirement()],
            principal,
            httpContext);

        await new AgentRouteAuthorizationHandler().HandleAsync(authorizationContext);

        Assert.True(authorizationContext.HasSucceeded);
    }

    [Fact]
    public async Task HttpTransportUsesVersionedHttpsAgentRouteAndProtocolHeaders()
    {
        Guid agentId = Guid.NewGuid();
        AgentOutboxMessageRow row = Outbox(1);
        AgentOutboxEnvelope envelope = new(
            row.MessageId,
            row.Sequence,
            row.MessageType,
            row.SchemaVersion,
            row.PayloadJson,
            row.OccurredAt,
            row.AttemptCount);
        RecordingHttpHandler handler = new(envelope.MessageId);
        using HttpClient httpClient = new(handler);
        HttpObservationBatchTransport transport = new(
            httpClient,
            new Uri("https://server.example.test/root/"),
            "0.1.0");

        ObservationBatchResult result = await transport.SendAsync(agentId, [envelope], CancellationToken.None);

        Assert.Equal(ObservationIngestionDisposition.Accepted, Assert.Single(result.Items).Disposition);
        Assert.Equal($"https://server.example.test/root/api/v1/agents/{agentId:D}/observations:batch", handler.RequestUri);
        Assert.Equal("1", handler.ProtocolVersion);
    }

    [Fact]
    public async Task HttpTransportRetriesSuccessfulResponseWithInvalidContract()
    {
        AgentOutboxMessageRow row = Outbox(1);
        using HttpClient httpClient = new(new InvalidJsonHttpHandler());
        HttpObservationBatchTransport transport = new(
            httpClient,
            new Uri("https://server.example.test/"),
            "0.1.0");

        ObservationBatchResult result = await transport.SendAsync(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            [Envelope(row)],
            CancellationToken.None);

        ObservationItemResult item = Assert.Single(result.Items);
        Assert.Equal(ObservationIngestionDisposition.Retryable, item.Disposition);
        Assert.Equal("sync.response_invalid", item.ErrorCode);
    }

    [Fact]
    public async Task HttpTransportRetriesSuccessfulResponseWithDuplicateMessageResults()
    {
        AgentOutboxMessageRow row = Outbox(1);
        using HttpClient httpClient = new(new DuplicateResultHttpHandler(row.MessageId));
        HttpObservationBatchTransport transport = new(
            httpClient,
            new Uri("https://server.example.test/"),
            "0.1.0");

        ObservationBatchResult result = await transport.SendAsync(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            [Envelope(row)],
            CancellationToken.None);

        ObservationItemResult item = Assert.Single(result.Items);
        Assert.Equal(ObservationIngestionDisposition.Retryable, item.Disposition);
        Assert.Equal("sync.response_invalid", item.ErrorCode);
    }

    [Fact]
    public async Task HttpTransportMapsClientTimeoutToRetryableOutcome()
    {
        AgentOutboxMessageRow row = Outbox(1);
        using HttpClient httpClient = new(new TimeoutHttpHandler());
        HttpObservationBatchTransport transport = new(
            httpClient,
            new Uri("https://server.example.test/"),
            "0.1.0");

        ObservationBatchResult result = await transport.SendAsync(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            [Envelope(row)],
            CancellationToken.None);

        ObservationItemResult item = Assert.Single(result.Items);
        Assert.Equal(ObservationIngestionDisposition.Retryable, item.Disposition);
        Assert.Equal("sync.transport_timeout", item.ErrorCode);
    }

    /// <summary>Proves every redirect is retained as retryable and never becomes a terminal acknowledgement.</summary>
    /// <param name="statusCode">Redirect status returned by the fixed Server endpoint.</param>
    [Theory]
    [InlineData(HttpStatusCode.MovedPermanently)]
    [InlineData(HttpStatusCode.Found)]
    [InlineData(HttpStatusCode.SeeOther)]
    [InlineData(HttpStatusCode.TemporaryRedirect)]
    [InlineData(HttpStatusCode.PermanentRedirect)]
    public async Task HttpTransportRefusesRedirectsAsRetryable(HttpStatusCode statusCode)
    {
        AgentOutboxMessageRow row = Outbox(1);
        using HttpClient httpClient = new(new StatusHttpHandler(statusCode));
        HttpObservationBatchTransport transport = new(
            httpClient,
            new Uri("https://server.example.test/"),
            "0.1.0");

        ObservationBatchResult result = await transport.SendAsync(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            [Envelope(row)],
            CancellationToken.None);

        ObservationItemResult item = Assert.Single(result.Items);
        Assert.Equal(ObservationIngestionDisposition.Retryable, item.Disposition);
        Assert.Equal("sync.redirect_refused", item.ErrorCode);
        Assert.Equal(0, result.HighestContiguousSequence);
    }

    /// <summary>Proves that a local unsupported payload cannot acknowledge its sequence without Server cursor proof.</summary>
    [Fact]
    public async Task DispatchRunnerRetainsLocallyRejectedHttpPayload()
    {
        AgentOutboxMessageRow row = Outbox(1);
        row.MessageType = "unsupported.observation.v1";
        InMemoryOutboxStore store = new(row);
        using HttpClient httpClient = new(new InvalidJsonHttpHandler());
        HttpObservationBatchTransport transport = new(
            httpClient,
            new Uri("https://server.example.test/"),
            "0.1.0");
        AgentOutboxDispatchRunner runner = new(
            Guid.NewGuid(),
            store,
            transport,
            new FixedTimeProvider(Now));

        AgentOutboxDispatchResult result = await runner.RunOnceAsync(50);

        Assert.Equal(0, result.AcknowledgedCount);
        Assert.Equal(1, result.RetryableCount);
        ObservationItemResult applied = Assert.Single(store.AppliedResults);
        Assert.Equal(ObservationIngestionDisposition.Retryable, applied.Disposition);
        Assert.Equal("sync.rejection_unconfirmed", applied.ErrorCode);
    }

    /// <summary>Proves that a request-level HTTP rejection cannot acknowledge observations absent Server cursor proof.</summary>
    [Fact]
    public async Task DispatchRunnerRetainsRequestLevelHttpRejection()
    {
        AgentOutboxMessageRow row = Outbox(1);
        InMemoryOutboxStore store = new(row);
        using HttpClient httpClient = new(new StatusHttpHandler(HttpStatusCode.BadRequest));
        HttpObservationBatchTransport transport = new(
            httpClient,
            new Uri("https://server.example.test/"),
            "0.1.0");
        AgentOutboxDispatchRunner runner = new(
            Guid.NewGuid(),
            store,
            transport,
            new FixedTimeProvider(Now));

        AgentOutboxDispatchResult result = await runner.RunOnceAsync(50);

        Assert.Equal(0, result.AcknowledgedCount);
        Assert.Equal(1, result.RetryableCount);
        ObservationItemResult applied = Assert.Single(store.AppliedResults);
        Assert.Equal(ObservationIngestionDisposition.Retryable, applied.Disposition);
        Assert.Equal("sync.rejection_unconfirmed", applied.ErrorCode);
    }

    [Fact]
    public void SynchronizationConfigurationRejectsNonHttpsServer()
    {
        AgentSynchronizationOptions options = new()
        {
            Enabled = true,
            ServerBaseAddress = "http://server.example.test",
            ClientCertificateThumbprint = "ABC123",
        };

        Assert.Throws<InvalidOperationException>(options.ValidateAndGetServerBaseAddress);
    }

    [Fact]
    public void CommandPollingDefaultsDisabledAndEnablingWithoutDurableProtocolIsRejected()
    {
        AgentSynchronizationOptions defaults = new();

        Assert.False(defaults.Enabled);
        Assert.False(defaults.CommandPollingEnabled);
        defaults.ValidateCommandPollingForStartup();

        AgentSynchronizationOptions enabled = new()
        {
            Enabled = true,
            CommandPollingEnabled = true,
        };
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            enabled.ValidateCommandPollingForStartup);
        Assert.Equal("command.polling_durable_protocol_unavailable", exception.Message);
    }

    /// <summary>Proves the command flag is rejected independently of observation synchronisation.</summary>
    /// <param name="synchronisationEnabled">Whether observation dispatch is enabled.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CommandPollingFlagCannotBeEnabledByAnySynchronisationCombination(bool synchronisationEnabled)
    {
        AgentSynchronizationOptions options = new()
        {
            Enabled = synchronisationEnabled,
            CommandPollingEnabled = true,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            options.ValidateCommandPollingForStartup);

        Assert.Equal("command.polling_durable_protocol_unavailable", exception.Message);
    }

    [Fact]
    public void HttpTransportRejectsNonHttpsBaseAddressEvenWhenConstructedDirectly()
    {
        using HttpClient httpClient = new(new RecordingHttpHandler(Guid.NewGuid()));

        Assert.Throws<ArgumentException>(() => new HttpObservationBatchTransport(
            httpClient,
            new Uri("http://server.example.test/"),
            "0.1.0"));
    }

    [Theory]
    [InlineData(null, HealthStatus.Healthy, "Connected", "Info")]
    [InlineData(HealthStatus.Timeout, HealthStatus.Healthy, "Recovered", "Info")]
    [InlineData(HealthStatus.Healthy, HealthStatus.Unavailable, "Disconnected", "Error")]
    [InlineData(HealthStatus.Healthy, HealthStatus.Timeout, "Timeout", "Error")]
    [InlineData(HealthStatus.Healthy, HealthStatus.AuthFailed, "AuthenticationFailed", "Error")]
    [InlineData(HealthStatus.Healthy, HealthStatus.Degraded, "Degraded", "Warning")]
    public void EventDeriverMapsCanonicalHealthTransitions(
        HealthStatus? previous,
        HealthStatus current,
        string eventType,
        string severity)
    {
        CanonicalEventCandidate candidate = Assert.IsType<CanonicalEventCandidate>(
            ObservationEventDeriver.Derive(previous, current));

        Assert.Equal(eventType, candidate.EventType);
        Assert.Equal(severity, candidate.Severity);
        Assert.Equal(previous, candidate.PreviousStatus);
        Assert.Equal(current, candidate.CurrentStatus);
    }

    private static AgentOutboxMessageRow Outbox(long sequence)
    {
        Guid messageId = Guid.NewGuid();
        Guid agentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid instanceId = Guid.NewGuid();
        return new AgentOutboxMessageRow
        {
            MessageId = messageId,
            Sequence = sequence,
            MessageType = "health.observation.v1",
            SchemaVersion = 1,
            PayloadJson = JsonSerializer.Serialize(new
            {
                messageId,
                schemaVersion = 1,
                observationId = Guid.NewGuid(),
                instanceId,
                agentId,
                providerType = "postgresql",
                providerVersion = "fixture",
                status = "Healthy",
                method = "fixture",
                evidenceLevel = "ProviderAuthenticated",
                attemptCount = 1,
                observedAt = Now,
                durationMilliseconds = 10,
                limitations = Array.Empty<string>(),
            }, SerializerOptions),
            OccurredAt = Now,
            CreatedAt = Now,
            AvailableAt = Now,
            AttemptCount = 0,
        };
    }

    private static ObservationSyncMessage Message(Guid agentId, Guid instanceId, long sequence, string status) =>
        new(
            Guid.NewGuid(),
            1,
            sequence,
            Guid.NewGuid(),
            instanceId,
            agentId,
            "postgresql",
            "fixture",
            status,
            "fixture",
            "ProviderAuthenticated",
            1,
            Now.AddSeconds(sequence),
            10,
            null,
            null,
            []);

    private static HealthSampleRow StoredSample(ObservationSyncMessage message, DateTimeOffset receivedAt) => new()
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
        AttemptCount = message.AttemptCount,
    };

    private static AgentOutboxEnvelope Envelope(AgentOutboxMessageRow row) =>
        new(
            row.MessageId,
            row.Sequence,
            row.MessageType,
            row.SchemaVersion,
            row.PayloadJson,
            row.OccurredAt,
            row.AttemptCount);

    /// <summary>Creates an in-memory HTTPS request without a token or client certificate.</summary>
    /// <param name="services">Pipeline service provider used by authentication and authorisation middleware.</param>
    /// <param name="path">Protected route to exercise.</param>
    /// <returns>A fresh HTTPS context with an isolated response body.</returns>
    private static DefaultHttpContext HttpsContext(IServiceProvider services, string path)
    {
        DefaultHttpContext context = new()
        {
            RequestServices = services,
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Request.Scheme = "https";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static RegisteredAgentRow Agent(Guid agentId) => new()
    {
        AgentId = agentId,
        InstallationId = "fixture-agent",
        DisplayName = "Fixture Agent",
        Environment = "test",
        Platform = "test",
        AgentVersion = "0.1.0",
        CertificateThumbprint = "FIXTURE",
        State = "Active",
        EnrolledAt = Now,
        ConcurrencyToken = Guid.NewGuid(),
    };

    private static DatabaseInstanceRow Instance(Guid instanceId, Guid agentId) => new()
    {
        InstanceId = instanceId,
        DisplayName = "Fixture instance",
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

    private sealed class TestAgentContextFactory(DbContextOptions<AgentDbContext> options) : IDbContextFactory<AgentDbContext>
    {
        public AgentDbContext CreateDbContext() => new(options);
    }

    private sealed class TestServerContextFactory(DbContextOptions<ServerDbContext> options) : IDbContextFactory<ServerDbContext>
    {
        public ServerDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>Authenticates the routed Agent fixture without creating or loading a client certificate.</summary>
    private sealed class AgentFixtureAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        /// <summary>Initialises the in-memory Agent authentication handler from standard framework services.</summary>
        public AgentFixtureAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        /// <inheritdoc />
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? routeValue = Context.Request.RouteValues["agentId"]?.ToString();
            if (!Guid.TryParse(routeValue, out Guid agentId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            ClaimsIdentity identity = new(
                [new Claim(AgentIdentityClaimTypes.AgentId, agentId.ToString("D"))],
                Scheme.Name);
            AuthenticationTicket ticket = new(new ClaimsPrincipal(identity), Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>Fails the test if a protected identity handler runs for an explicitly public endpoint.</summary>
    private sealed class UnexpectedAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        /// <summary>Initialises the guard handler from standard framework services.</summary>
        public UnexpectedAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        /// <inheritdoc />
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            throw new InvalidOperationException("A public endpoint invoked the human bearer handler.");
    }

    private sealed class RecordingTransport : IObservationBatchTransport
    {
        public ValueTask<ObservationBatchResult> SendAsync(
            Guid agentId,
            IReadOnlyList<AgentOutboxEnvelope> messages,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new ObservationBatchResult(
            [
                new(messages[0].MessageId, ObservationIngestionDisposition.Accepted),
                new(messages[1].MessageId, ObservationIngestionDisposition.Retryable),
            ],
            1));
    }

    private sealed class EmptyResponseTransport : IObservationBatchTransport
    {
        public ValueTask<ObservationBatchResult> SendAsync(
            Guid agentId,
            IReadOnlyList<AgentOutboxEnvelope> messages,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new ObservationBatchResult([], 0));
    }

    /// <summary>Returns one terminal rejection with a caller-selected Server high-water mark.</summary>
    private sealed class RejectedResponseTransport(long highestContiguousSequence) : IObservationBatchTransport
    {
        /// <inheritdoc />
        public ValueTask<ObservationBatchResult> SendAsync(
            Guid agentId,
            IReadOnlyList<AgentOutboxEnvelope> messages,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new ObservationBatchResult(
                [
                    new(
                        messages[0].MessageId,
                        ObservationIngestionDisposition.Rejected,
                        "observation.payload_invalid"),
                ],
                highestContiguousSequence));
    }

    /// <summary>Returns contradictory duplicate results to exercise the dispatcher's fail-closed response boundary.</summary>
    private sealed class AmbiguousResponseTransport : IObservationBatchTransport
    {
        public ValueTask<ObservationBatchResult> SendAsync(
            Guid agentId,
            IReadOnlyList<AgentOutboxEnvelope> messages,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new ObservationBatchResult(
            [
                new(messages[0].MessageId, ObservationIngestionDisposition.Accepted),
                new(messages[0].MessageId, ObservationIngestionDisposition.Retryable),
            ],
            1));
    }

    private sealed class InMemoryOutboxStore(AgentOutboxMessageRow row) : IAgentOutboxStore
    {
        public IReadOnlyList<ObservationItemResult> AppliedResults { get; private set; } = [];

        public ValueTask<IReadOnlyList<AgentOutboxEnvelope>> GetPendingAsync(
            DateTimeOffset now,
            int maximumCount,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<AgentOutboxEnvelope>>([Envelope(row)]);

        public ValueTask ApplyResultsAsync(
            IReadOnlyList<ObservationItemResult> results,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            AppliedResults = results;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingIngestionStore : IObservationIngestionStore
    {
        public int IngestCalls { get; private set; }

        public ValueTask<ObservationItemResult> IngestAsync(
            ObservationSyncMessage message,
            DateTimeOffset receivedAt,
            CancellationToken cancellationToken)
        {
            IngestCalls++;
            return ValueTask.FromResult(new ObservationItemResult(
                message.MessageId,
                ObservationIngestionDisposition.Accepted));
        }

        public ValueTask<long> GetHighestContiguousSequenceAsync(Guid agentId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(0L);
    }

    private sealed class RecordingHttpHandler(Guid messageId) : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }

        public string? ProtocolVersion { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.AbsoluteUri;
            ProtocolVersion = request.Headers.GetValues("DBN-Protocol-Version").Single();
            _ = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new ObservationBatchResult(
                        [new(messageId, ObservationIngestionDisposition.Accepted)],
                        1),
                        SerializerOptions),
                    Encoding.UTF8,
                    "application/json"),
            };
        }
    }

    private sealed class InvalidJsonHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{not-json", Encoding.UTF8, "application/json"),
            });
    }

    /// <summary>Returns a syntactically valid but contradictory duplicate-result HTTP contract.</summary>
    private sealed class DuplicateResultHttpHandler(Guid messageId) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new ObservationBatchResult(
                    [
                        new(messageId, ObservationIngestionDisposition.Accepted),
                        new(messageId, ObservationIngestionDisposition.Retryable),
                    ],
                    1),
                    SerializerOptions),
                    Encoding.UTF8,
                    "application/json"),
            });
    }

    private sealed class TimeoutHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("Fixture timeout."));
    }

    /// <summary>Returns one caller-selected non-success HTTP status without an ingestion response body.</summary>
    private sealed class StatusHttpHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
