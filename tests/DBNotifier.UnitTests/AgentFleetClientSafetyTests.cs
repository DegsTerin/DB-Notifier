// Module purpose: Verifies the fail-closed Agent-side activation guard and independently verifiable assignment digest.
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DBNotifier.Agent.Worker;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Infrastructure.AgentFleet;
using DBNotifier.Persistence.Agent.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.UnitTests;

public sealed class AgentFleetClientSafetyTests
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void OrdinaryWorkerCannotEnableTheSandboxOnlyAgentFleetClient()
    {
        AgentFleetClientOptions defaults = new();
        defaults.ValidateForStartup();
        Assert.False(defaults.Enabled);

        AgentFleetClientOptions enabled = new() { Enabled = true };
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(enabled.ValidateForStartup);
        Assert.Equal("agent_fleet.sandbox_only", exception.Message);
    }

    [Fact]
    public void AssignmentDigestRejectsBodyOrEntityTagTampering()
    {
        Guid agentId = Guid.NewGuid();
        DateTimeOffset updatedAt = new(2026, 7, 18, 3, 0, 0, TimeSpan.Zero);
        AgentReadOnlyAssignment assignment = CreateAssignment("Primary fixture", updatedAt);
        AgentReadOnlyAssignment[] assignments = [assignment];
        string version = AgentAssignmentVersion.Compute(agentId, assignments);
        AgentAssignmentSnapshot sourceSnapshot = new(
            AgentFleetProtocol.CurrentSchemaVersion,
            agentId,
            version,
            updatedAt,
            assignments);
        string wireJson = JsonSerializer.Serialize(sourceSnapshot);
        AgentAssignmentSnapshot snapshot = JsonSerializer.Deserialize<AgentAssignmentSnapshot>(wireJson) ??
            throw new InvalidOperationException("Assignment fixture did not round-trip.");

        Assert.True(AgentAssignmentVersion.TryValidate(
            snapshot,
            agentId,
            "test",
            $"\"{version}\"",
            out string? validError));
        Assert.Null(validError);

        AgentAssignmentSnapshot alteredBody = snapshot with
        {
            Assignments = [assignment with { DisplayName = "Altered fixture" }],
        };
        Assert.False(AgentAssignmentVersion.TryValidate(
            alteredBody,
            agentId,
            "test",
            $"\"{version}\"",
            out string? bodyError));
        Assert.Equal("assignments.digest_mismatch", bodyError);

        Assert.False(AgentAssignmentVersion.TryValidate(
            snapshot,
            agentId,
            "test",
            $"W/\"{version}\"",
            out string? entityTagError));
        Assert.Equal("assignments.snapshot_invalid", entityTagError);
    }

    [Fact]
    public async Task HeartbeatAcknowledgementRequiresTheExactPendingEnvelope()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (AgentDbContext context = new(options))
        {
            await context.Database.MigrateAsync();
        }

        AgentFleetLocalStore store = new(
            new TestContextFactory(options),
            AgentAssignmentValidationFixture.Create("fixture"));
        DateTimeOffset now = new(2026, 7, 18, 3, 0, 0, TimeSpan.Zero);
        AgentLocalRegistration registration = new(
            Guid.NewGuid(),
            "installation:safety-fixture",
            "test",
            "e2e:identity-reference",
            new string('A', 64),
            now.AddHours(1),
            now,
            AgentLocalIdentityState.Active,
            null);
        await store.SaveEnrollmentAsync(registration, CancellationToken.None);
        AgentFleetOperationLease lease = Assert.IsType<AgentFleetOperationLease>(
            await store.TryAcquireOperationLeaseAsync(
                registration.AgentId,
                "sandbox:safety-fixture",
                AgentFleetOperationKind.Heartbeat,
                now,
                TimeSpan.FromMinutes(1),
                CancellationToken.None));
        PendingAgentHeartbeat pending = await store.GetOrCreatePendingHeartbeatAsync(
            registration,
            "0.1.0",
            new AgentHeartbeatQueueEvidence(0, null),
            now,
            lease,
            CancellationToken.None);
        AgentHeartbeatRequest altered = pending.Request with { AgentVersion = "0.1.1" };
        AgentHeartbeatOutcome receipt = new(
            AgentHeartbeatDisposition.Accepted,
            now.AddSeconds(1),
            pending.Request.Sequence,
            0,
            null);

        InvalidOperationException refusal = await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.AcknowledgeHeartbeatAsync(
                altered,
                receipt,
                now.AddSeconds(1),
                lease,
                CancellationToken.None).AsTask());
        Assert.Equal("heartbeat.pending_conflict", refusal.Message);
        PendingAgentHeartbeat retained = await store.GetOrCreatePendingHeartbeatAsync(
            registration,
            "0.1.0",
            new AgentHeartbeatQueueEvidence(0, null),
            now.AddSeconds(2),
            lease,
            CancellationToken.None);
        Assert.Equal(pending.Request, retained.Request);

        await store.AcknowledgeHeartbeatAsync(
            pending.Request,
            receipt,
            now.AddSeconds(2),
            lease,
            CancellationToken.None);
        PendingAgentHeartbeat next = await store.GetOrCreatePendingHeartbeatAsync(
            registration,
            "0.1.0",
            new AgentHeartbeatQueueEvidence(0, null),
            now.AddSeconds(3),
            lease,
            CancellationToken.None);
        Assert.Equal(2, next.Request.Sequence);
    }

    /// <summary>Proves missing, duplicated, future, partial and extended responses fail protocol validation.</summary>
    [Fact]
    public async Task HttpTransportRejectsProtocolAndPayloadAmbiguity()
    {
        DateTimeOffset now = new(2026, 7, 18, 7, 0, 0, TimeSpan.Zero);
        Guid agentId = Guid.NewGuid();
        AgentHeartbeatRequest request = new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            agentId,
            1,
            now,
            now,
            "1.0.0-sandbox",
            AgentFleetProtocol.CurrentProtocolVersion,
            AgentFleetProtocol.CurrentProtocolVersion,
            0,
            null,
            now);
        AgentHeartbeatOutcome validOutcome = new(
            AgentHeartbeatDisposition.Accepted,
            now,
            1,
            0,
            null);
        string validJson = JsonSerializer.Serialize(validOutcome, WebJsonOptions);
        JsonObject extended = JsonNode.Parse(validJson)?.AsObject() ?? throw new InvalidOperationException();
        extended["unexpectedField"] = true;
        Func<HttpResponseMessage>[] responses =
        [
            () => JsonResponse(validJson, headers: HeaderMode.Missing),
            () => JsonResponse(validJson, headers: HeaderMode.Duplicated),
            () => JsonResponse(validJson, headers: HeaderMode.Future),
            () => JsonResponse("{", headers: HeaderMode.Valid),
            () => JsonResponse(extended.ToJsonString(), headers: HeaderMode.Valid),
        ];

        foreach (Func<HttpResponseMessage> response in responses)
        {
            using HttpClient client = new(new SingleResponseHandler(response));
            HttpAgentFleetClientTransport transport = new(
                new Uri("https://127.0.0.1:4443/"),
                "1.0.0-sandbox",
                _ => client);
            AgentFleetTransportResult<AgentHeartbeatOutcome> result = await transport.SendHeartbeatAsync(
                "sandbox-identity:test",
                request,
                CancellationToken.None);

            Assert.Equal(AgentFleetTransportDisposition.InvalidResponse, result.Disposition);
            Assert.Equal("protocol.response_invalid", result.ErrorCode);
        }
    }

    /// <summary>Proves only bounded delta Retry-After is admitted for transient scheduling.</summary>
    [Fact]
    public async Task HttpTransportBoundsRetryAfterAndResponseSize()
    {
        DateTimeOffset now = new(2026, 7, 18, 7, 10, 0, TimeSpan.Zero);
        AgentHeartbeatRequest request = new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            Guid.NewGuid(),
            1,
            now,
            now,
            "1.0.0-sandbox",
            1,
            1,
            0,
            null,
            now);
        using HttpClient retryClient = new(new SingleResponseHandler(() =>
        {
            HttpResponseMessage response = new(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(
                    "{\"code\":\"sandbox.unavailable\",\"retryable\":true}",
                    Encoding.UTF8,
                    "application/problem+json"),
            };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                TimeSpan.FromSeconds(2));
            return response;
        }));
        HttpAgentFleetClientTransport retryTransport = new(
            new Uri("https://127.0.0.1:4443/"),
            "1.0.0-sandbox",
            _ => retryClient);

        AgentFleetTransportResult<AgentHeartbeatOutcome> transient = await retryTransport.SendHeartbeatAsync(
            "sandbox-identity:test",
            request,
            CancellationToken.None);
        Assert.Equal(AgentFleetTransportDisposition.TransientFailure, transient.Disposition);
        Assert.Equal(TimeSpan.FromSeconds(2), transient.RetryAfter);

        using HttpClient terminalClient = new(new SingleResponseHandler(() => new HttpResponseMessage(
            HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent(
                "{\"code\":\"assignments.unavailable\",\"retryable\":false}",
                Encoding.UTF8,
                "application/problem+json"),
        }));
        HttpAgentFleetClientTransport terminalTransport = new(
            new Uri("https://127.0.0.1:4443/"),
            "1.0.0-sandbox",
            _ => terminalClient);
        AgentFleetTransportResult<AgentHeartbeatOutcome> terminal = await terminalTransport.SendHeartbeatAsync(
            "sandbox-identity:test",
            request,
            CancellationToken.None);
        Assert.Equal(AgentFleetTransportDisposition.InvalidResponse, terminal.Disposition);
        Assert.Equal("assignments.unavailable", terminal.ErrorCode);
        Assert.Null(terminal.RetryAfter);

        using HttpClient oversizedClient = new(new SingleResponseHandler(() =>
        {
            HttpResponseMessage response = JsonResponse(
                new string('A', (128 * 1024) + 1),
                headers: HeaderMode.Valid);
            return response;
        }));
        HttpAgentFleetClientTransport oversizedTransport = new(
            new Uri("https://127.0.0.1:4443/"),
            "1.0.0-sandbox",
            _ => oversizedClient);
        AgentFleetTransportResult<AgentHeartbeatOutcome> oversized = await oversizedTransport.SendHeartbeatAsync(
            "sandbox-identity:test",
            request,
            CancellationToken.None);
        Assert.Equal(AgentFleetTransportDisposition.InvalidResponse, oversized.Disposition);
        Assert.Equal("protocol.response_too_large", oversized.ErrorCode);
    }

    /// <summary>Proves a 304 with an ETag different from the local LKG is refused without replacing that LKG.</summary>
    [Fact]
    public async Task AssignmentNotModifiedRequiresTheExactLocalStrongEntityTag()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (AgentDbContext context = new(options))
        {
            await context.Database.MigrateAsync();
        }

        AgentFleetLocalStore store = new(
            new TestContextFactory(options),
            AgentAssignmentValidationFixture.Create("fixture"));
        DateTimeOffset now = new(2026, 7, 18, 7, 20, 0, TimeSpan.Zero);
        AgentLocalRegistration registration = new(
            Guid.NewGuid(),
            "installation:etag-fixture",
            "test",
            "sandbox-identity:etag-fixture",
            new string('B', 64),
            now.AddHours(1),
            now,
            AgentLocalIdentityState.Active,
            null);
        await store.SaveEnrollmentAsync(registration, CancellationToken.None);
        AgentFleetOperationLease lease = Assert.IsType<AgentFleetOperationLease>(
            await store.TryAcquireOperationLeaseAsync(
                registration.AgentId,
                "sandbox:etag-owner",
                AgentFleetOperationKind.AssignmentReconciliation,
                now,
                TimeSpan.FromMinutes(1),
                CancellationToken.None));
        AgentReadOnlyAssignment assignment = CreateAssignment("LKG fixture", now);
        string version = AgentAssignmentVersion.Compute(registration.AgentId, [assignment]);
        AgentAssignmentSnapshot snapshot = new(
            AgentFleetProtocol.CurrentSchemaVersion,
            registration.AgentId,
            version,
            now,
            [assignment]);
        await store.ApplyAssignmentsAsync(snapshot, $"\"{version}\"", now, lease, CancellationToken.None);
        NotModifiedTransport transport = new(new string('C', 64));
        AgentFleetClientCoordinator coordinator = new(
            new UnusedIdentityStore(),
            transport,
            store,
            new FixedTimeProvider(now.AddSeconds(1)),
            TimeSpan.FromMinutes(5));

        AgentFleetClientResult result = await coordinator.ReconcileAssignmentsOnceAsync(
            "1.0.0-sandbox",
            lease,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("assignments.response_invalid", result.Code);
        Assert.Equal(version, (await store.GetAssignmentStateAsync(
            registration.AgentId,
            CancellationToken.None)).Version);
    }

    private static HttpResponseMessage JsonResponse(string json, HeaderMode headers)
    {
        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        if (headers != HeaderMode.Missing)
        {
            string protocol = headers == HeaderMode.Future ? "2" : "1";
            response.Headers.TryAddWithoutValidation(
                "DBN-Protocol-Version",
                headers == HeaderMode.Duplicated ? ["1", "1"] : [protocol]);
            response.Headers.TryAddWithoutValidation("DBN-Protocol-Minimum", "1");
            response.Headers.TryAddWithoutValidation("DBN-Protocol-Maximum", protocol);
        }

        return response;
    }

    private static AgentReadOnlyAssignment CreateAssignment(string displayName, DateTimeOffset updatedAt)
    {
        using JsonDocument endpoint = JsonDocument.Parse("{ \"host\" : \"sandbox.invalid\", \"port\" : 5432 }");
        using JsonDocument tags = JsonDocument.Parse("[ \"sandbox\", \"read-only\" ]");
        return new AgentReadOnlyAssignment(
            Guid.Parse("2C16FE85-93B8-4785-9E9D-B245D669EFD8"),
            displayName,
            "fixture",
            "test",
            endpoint.RootElement.Clone(),
            AgentAssignmentValidationFixture.MonitoringReference,
            tags.RootElement.Clone(),
            30,
            5,
            1,
            updatedAt);
    }

    private sealed class TestContextFactory(DbContextOptions<AgentDbContext> options)
        : IDbContextFactory<AgentDbContext>
    {
        /// <inheritdoc />
        public AgentDbContext CreateDbContext() => new(options);
    }

    private sealed class SingleResponseHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HttpResponseMessage response = responseFactory();
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }

    private sealed class NotModifiedTransport(string version) : IAgentFleetClientTransport
    {
        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentEnrollmentOutcome>> EnrolAsync(
            string token,
            AgentEnrollmentRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentHeartbeatOutcome>> SendHeartbeatAsync(
            string identityReference,
            AgentHeartbeatRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <inheritdoc />
        public ValueTask<AgentFleetTransportResult<AgentAssignmentSnapshot>> GetAssignmentsAsync(
            string identityReference,
            Guid agentId,
            string agentVersion,
            string? currentVersion,
            CancellationToken cancellationToken) => ValueTask.FromResult(
                new AgentFleetTransportResult<AgentAssignmentSnapshot>(
                    AgentFleetTransportDisposition.NotModified,
                    null,
                    $"\"{version}\"",
                    null));
    }

    private sealed class UnusedIdentityStore : IAgentEnrollmentIdentityStore
    {
        /// <inheritdoc />
        public ValueTask<AgentEnrollmentKeyMaterial> BeginAsync(
            string installationId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <inheritdoc />
        public ValueTask<AgentIdentityCompletion> CompleteAsync(
            Guid operationId,
            ReadOnlyMemory<byte> certificateDer,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <inheritdoc />
        public ValueTask AbortAsync(Guid operationId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

        /// <inheritdoc />
        public ValueTask RemoveAsync(string identityReference, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }

    private enum HeaderMode
    {
        Missing,
        Valid,
        Duplicated,
        Future,
    }
}
