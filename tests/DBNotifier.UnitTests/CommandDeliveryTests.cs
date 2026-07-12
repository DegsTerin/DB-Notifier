using DBNotifier.Application.Synchronization;
using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.UnitTests;

public sealed class CommandDeliveryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ServerDeliversOnlyCompatibleUnexpiredCommandsAndAcknowledgesIdempotently()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>().UseSqlite(connection).Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        AdministrativeCommandRow valid = Command(agentId, instanceId, userId, Now.AddMinutes(5), "command:1");
        AdministrativeCommandRow expired = Command(agentId, instanceId, userId, Now.AddSeconds(-1), "command:2");
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId));
            setup.Instances.Add(Instance(instanceId, agentId));
            setup.Users.Add(User(userId));
            setup.AdministrativeCommands.AddRange(valid, expired);
            await setup.SaveChangesAsync();
        }

        ServerCommandDeliveryStore store = new(new ServerFactory(options));
        CommandPollRequest poll = new(Guid.NewGuid(), 1, agentId, 1, Now, Now, "0.1.0",
            new Dictionary<string, string> { ["postgresql"] = "0.1.0" }, 10);
        CommandEnvelope delivered = Assert.Single(await store.PollAsync(poll, Now, CancellationToken.None));
        CommandAcknowledgementRequest ack = new(Guid.NewGuid(), 1, agentId, 2, Now, Now,
            [new(delivered.CommandId, delivered.IdempotencyKey, Now)]);
        Assert.Equal(CommandAcknowledgementDisposition.Accepted,
            Assert.Single(await store.AcknowledgeAsync(ack, Now, CancellationToken.None)).Disposition);
        Assert.Equal(CommandAcknowledgementDisposition.Duplicate,
            Assert.Single(await store.AcknowledgeAsync(ack, Now, CancellationToken.None)).Disposition);

        await using ServerDbContext verification = new(options);
        Assert.Equal("Acknowledged", (await verification.AdministrativeCommands.FindAsync(valid.CommandId))!.State);
        Assert.Equal("Expired", (await verification.AdministrativeCommands.FindAsync(expired.CommandId))!.State);
        Assert.Empty(await verification.CommandAttempts.ToArrayAsync());
    }

    [Fact]
    public async Task ServerFailsClosedForVersionMismatchAndWrongAgentAcknowledgement()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>().UseSqlite(connection).Options;
        Guid agentId = Guid.NewGuid();
        Guid instanceId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        AdministrativeCommandRow command = Command(agentId, instanceId, userId, Now.AddMinutes(5), "command:3");
        await using (ServerDbContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Agents.Add(Agent(agentId)); setup.Instances.Add(Instance(instanceId, agentId));
            setup.Users.Add(User(userId)); setup.AdministrativeCommands.Add(command); await setup.SaveChangesAsync();
        }

        ServerCommandDeliveryStore store = new(new ServerFactory(options));
        CommandPollRequest incompatible = new(Guid.NewGuid(), 1, agentId, 1, Now, Now, "9.0.0",
            new Dictionary<string, string> { ["postgresql"] = "0.1.0" }, 10);
        Assert.Empty(await store.PollAsync(incompatible, Now, CancellationToken.None));
        CommandAcknowledgementRequest wrongAgent = new(Guid.NewGuid(), 1, Guid.NewGuid(), 2, Now, Now,
            [new(command.CommandId, command.IdempotencyKey, Now)]);
        Assert.Equal(CommandAcknowledgementDisposition.Rejected,
            Assert.Single(await store.AcknowledgeAsync(wrongAgent, Now, CancellationToken.None)).Disposition);
    }

    [Fact]
    public async Task AgentInboxAcceptsExactReplayButRejectsConflictingReplayWithoutExecution()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>().UseSqlite(connection).Options;
        await using (AgentDbContext setup = new(options)) { await setup.Database.EnsureCreatedAsync(); }
        AgentCommandInboxStore store = new(new AgentFactory(options));
        CommandEnvelope command = new(Guid.NewGuid(), "command:4", Guid.NewGuid(), "postgresql", "database.restart", "{}",
            Now, Now.AddMinutes(5), "0.1.0", "0.1.0");

        Assert.Single(await store.ReceiveAsync([command], Now, CancellationToken.None));
        Assert.Single(await store.ReceiveAsync([command], Now.AddSeconds(1), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await store.ReceiveAsync([command with { TypedParametersJson = "{\"changed\":true}" }], Now, CancellationToken.None));
        await store.MarkAcknowledgedAsync(
            [new(command.CommandId, CommandAcknowledgementDisposition.Accepted)], Now, CancellationToken.None);

        await using AgentDbContext verification = new(options);
        AgentInboxCommandRow saved = await verification.InboxCommands.SingleAsync();
        Assert.Equal("Acknowledged", saved.State);
        Assert.Null(saved.CompletedAt);
        Assert.Null(saved.ResultJson);
    }

    private static AdministrativeCommandRow Command(Guid agent, Guid instance, Guid user, DateTimeOffset expiry, string key) => new()
    {
        CommandId = Guid.NewGuid(),
        IdempotencyKey = key,
        InstanceId = instance,
        AssignedAgentId = agent,
        CapabilityId = "database.restart",
        TypedParametersJson = "{}",
        RequestedByUserId = user,
        RequestedAt = Now.AddMinutes(-1),
        Reason = "Approved test",
        ExpiresAt = expiry,
        AuthorizationSnapshotReference = "audit:test",
        ExpectedAgentVersion = "0.1.0",
        ExpectedProviderVersion = "0.1.0",
        State = "Pending",
        ConcurrencyToken = Guid.NewGuid()
    };
    private static RegisteredAgentRow Agent(Guid id) => new() { AgentId = id, InstallationId = $"agent-{id:N}", DisplayName = "Agent", Environment = "test", Platform = "test", AgentVersion = "0.1.0", CertificateThumbprint = $"CERT-{id:N}", State = "Active", EnrolledAt = Now, ConcurrencyToken = Guid.NewGuid() };
    private static DatabaseInstanceRow Instance(Guid id, Guid agent) => new() { InstanceId = id, DisplayName = "Instance", ProviderType = "postgresql", Environment = "test", EndpointJson = "{}", AssignedAgentId = agent, TagsJson = "[]", IntervalSeconds = 60, TimeoutSeconds = 5, RetryCount = 1, Enabled = true, CreatedAt = Now, UpdatedAt = Now, ConcurrencyToken = Guid.NewGuid() };
    private static PlatformUserRow User(Guid id) => new() { UserId = id, SubjectId = $"user-{id:N}", DisplayName = "User", State = "Active", CreatedAt = Now, UpdatedAt = Now, ConcurrencyToken = Guid.NewGuid() };
    private sealed class ServerFactory(DbContextOptions<ServerDbContext> options) : IDbContextFactory<ServerDbContext> { public ServerDbContext CreateDbContext() => new(options); }
    private sealed class AgentFactory(DbContextOptions<AgentDbContext> options) : IDbContextFactory<AgentDbContext> { public AgentDbContext CreateDbContext() => new(options); }
}
