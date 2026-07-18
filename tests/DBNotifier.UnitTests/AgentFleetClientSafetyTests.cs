// Module purpose: Verifies the fail-closed Agent-side activation guard and independently verifiable assignment digest.
using System.Text.Json;
using DBNotifier.Agent.Worker;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Persistence.Agent.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.UnitTests;

public sealed class AgentFleetClientSafetyTests
{
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

        AgentFleetLocalStore store = new(new TestContextFactory(options));
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
        PendingAgentHeartbeat pending = await store.GetOrCreatePendingHeartbeatAsync(
            registration,
            "0.1.0",
            new AgentHeartbeatQueueEvidence(0, null),
            now,
            CancellationToken.None);
        AgentHeartbeatRequest altered = pending.Request with { AgentVersion = "0.1.1" };
        AgentHeartbeatOutcome receipt = new(
            AgentHeartbeatDisposition.Accepted,
            now.AddSeconds(1),
            pending.Request.Sequence,
            0,
            null);

        InvalidOperationException refusal = await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.AcknowledgeHeartbeatAsync(altered, receipt, CancellationToken.None).AsTask());
        Assert.Equal("heartbeat.pending_conflict", refusal.Message);
        PendingAgentHeartbeat retained = await store.GetOrCreatePendingHeartbeatAsync(
            registration,
            "0.1.0",
            new AgentHeartbeatQueueEvidence(0, null),
            now.AddSeconds(2),
            CancellationToken.None);
        Assert.Equal(pending.Request, retained.Request);

        await store.AcknowledgeHeartbeatAsync(pending.Request, receipt, CancellationToken.None);
        PendingAgentHeartbeat next = await store.GetOrCreatePendingHeartbeatAsync(
            registration,
            "0.1.0",
            new AgentHeartbeatQueueEvidence(0, null),
            now.AddSeconds(3),
            CancellationToken.None);
        Assert.Equal(2, next.Request.Sequence);
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
            "fixture:monitoring-only",
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
}
