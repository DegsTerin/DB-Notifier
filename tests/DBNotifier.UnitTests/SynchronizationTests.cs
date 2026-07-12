// Module purpose: Verifies Synchronization Tests behaviour and protects the documented project contract.
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DBNotifier.Agent.Worker;
using DBNotifier.Application.Synchronization;
using DBNotifier.Domain;
using DBNotifier.Infrastructure.Synchronization;
using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Server.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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
    public async Task ServerIngestionIsIdempotentAndCreatesCanonicalEventsAndAlertDeliveries()
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
            setup.AlertRules.Add(new AlertRuleRow
            {
                AlertRuleId = Guid.NewGuid(),
                Name = "Connectivity events",
                RuleType = "CanonicalEvent",
                ConfigurationJson = "{\"eventTypes\":[\"Connected\",\"Timeout\"]}",
                Enabled = true,
                CreatedAt = Now,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            setup.NotificationChannels.Add(new NotificationChannelRow
            {
                NotificationChannelId = Guid.NewGuid(),
                Name = "Fixture channel",
                ChannelType = "test",
                NonSecretConfigurationJson = "{}",
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

    private static AgentOutboxEnvelope Envelope(AgentOutboxMessageRow row) =>
        new(
            row.MessageId,
            row.Sequence,
            row.MessageType,
            row.SchemaVersion,
            row.PayloadJson,
            row.OccurredAt,
            row.AttemptCount);

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

    private sealed class TimeoutHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("Fixture timeout."));
    }
}
