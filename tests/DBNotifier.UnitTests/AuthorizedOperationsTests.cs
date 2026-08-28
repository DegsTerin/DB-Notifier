// Module purpose: Verifies Authorized Operations Tests behaviour and protects the documented project contract.
using System.Security.Claims;
using System.Text.Json;
using DBNotifier.Application.Access;
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Server.Api.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.UnitTests;

public sealed class AuthorizedOperationsTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);
    private const string SubjectId = "oidc|operator-1";

    [Fact]
    public async Task CatalogReturnsOnlyInstancesCoveredByPermissionScope()
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        DbContextOptions<ServerDbContext> options = Options(connection);
        Guid allowedId = Guid.NewGuid();
        Guid deniedId = Guid.NewGuid();
        await SeedAsync(options, allowedId, "production", PlatformPermissions.InstancesRead, "Environment", "production");
        await using (ServerDbContext setup = new(options))
        {
            setup.Instances.Add(Instance(deniedId, Guid.NewGuid(), "development"));
            setup.Agents.Add(Agent(setup.Instances.Local.Single(row => row.InstanceId == deniedId).AssignedAgentId!.Value));
            await setup.SaveChangesAsync();
        }

        AuthorizedOperationsService service = Service(options);

        IReadOnlyList<AuthorizedInstance> result = await service.GetCatalogAsync(new HumanActor(SubjectId));

        AuthorizedInstance instance = Assert.Single(result);
        Assert.Equal(allowedId, instance.InstanceId);
        Assert.Equal("production", instance.Environment);
    }

    [Fact]
    public async Task DesktopFleetReturnsOnlyCoherentLatestObservationsWithinPermissionScope()
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        DbContextOptions<ServerDbContext> options = Options(connection);
        Guid allowedId = Guid.NewGuid();
        Guid deniedId = Guid.NewGuid();
        await SeedAsync(options, allowedId, "production", PlatformPermissions.InstancesRead, "Environment", "production");
        await using (ServerDbContext setup = new(options))
        {
            Guid deniedAgentId = Guid.NewGuid();
            setup.Agents.Add(Agent(deniedAgentId));
            setup.Instances.Add(Instance(deniedId, deniedAgentId, "development"));
            await setup.SaveChangesAsync();
        }
        await AddObservationAsync(options, allowedId, HealthStatus.Degraded, EvidenceLevel.ProviderReadiness);
        await AddObservationAsync(options, deniedId, HealthStatus.Healthy, EvidenceLevel.ProviderAuthenticated);

        DesktopFleetApiSnapshot result = await Service(options)
            .GetDesktopFleetSnapshotAsync(new HumanActor(SubjectId));

        Assert.Equal(DesktopFleetApiContract.CurrentSchemaVersion, result.SchemaVersion);
        Assert.Equal(Now, result.GeneratedAt);
        DesktopFleetApiItem item = Assert.Single(result.Items);
        Assert.Equal(allowedId, item.InstanceId);
        Assert.Equal("degraded", item.Status);
        Assert.Equal("Provider-readiness evidence", item.SupportLabel);
        Assert.Equal("Fixture Agent", item.LocationLabel);
        Assert.Equal(175, item.LatencyMilliseconds);
    }

    [Fact]
    public async Task DesktopFleetRejectsInconsistentObservationEvidence()
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        DbContextOptions<ServerDbContext> options = Options(connection);
        Guid instanceId = Guid.NewGuid();
        await SeedAsync(options, instanceId, "production", PlatformPermissions.InstancesRead, "Global", "*");
        await AddObservationAsync(options, instanceId, HealthStatus.Healthy, EvidenceLevel.ProviderAuthenticated);
        await using (ServerDbContext mutation = new(options))
        {
            HealthSampleRow sample = await mutation.HealthSamples.SingleAsync();
            sample.Status = nameof(HealthStatus.Timeout);
            await mutation.SaveChangesAsync();
        }

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await Service(options).GetDesktopFleetSnapshotAsync(new HumanActor(SubjectId)));

        Assert.Equal("desktop_fleet.projection_evidence_invalid", exception.Message);
    }

    [Fact]
    public async Task CommandCreationIsPendingAuditedIdempotentAndDoesNotDispatch()
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        DbContextOptions<ServerDbContext> options = Options(connection);
        Guid instanceId = Guid.NewGuid();
        await SeedAsync(
            options,
            instanceId,
            "production",
            PlatformPermissions.CommandsCreate,
            "Instance",
            instanceId.ToString("D"));
        AuthorizedOperationsService service = Service(options);
        CreateAdministrativeCommandRequest request = CommandRequest("command:test:0001");

        CommandCreationResult created = await service.CreateCommandAsync(
            new HumanActor(SubjectId),
            instanceId,
            request);
        CommandCreationResult duplicate = await service.CreateCommandAsync(
            new HumanActor(SubjectId),
            instanceId,
            request);

        Assert.Equal(CommandCreationDisposition.Created, created.Disposition);
        Assert.Equal(CommandCreationDisposition.Duplicate, duplicate.Disposition);
        Assert.Equal(created.Command?.CommandId, duplicate.Command?.CommandId);
        Assert.Equal("Pending", created.Command?.State);
        await using ServerDbContext verification = new(options);
        AdministrativeCommandRow command = Assert.Single(await verification.AdministrativeCommands.ToArrayAsync());
        Assert.StartsWith("role-assignment:", command.AuthorizationSnapshotReference, StringComparison.Ordinal);
        Assert.Equal(2, await verification.AuditEntries.CountAsync());
        Assert.Empty(await verification.CommandAttempts.ToArrayAsync());
        Assert.Empty(await verification.OutboxMessages.ToArrayAsync());
    }

    [Fact]
    public async Task ReusedIdempotencyKeyWithDifferentRequestIsConflict()
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        DbContextOptions<ServerDbContext> options = Options(connection);
        Guid instanceId = Guid.NewGuid();
        await SeedAsync(
            options,
            instanceId,
            "production",
            PlatformPermissions.CommandsCreate,
            "Global",
            "*");
        AuthorizedOperationsService service = Service(options);
        CreateAdministrativeCommandRequest request = CommandRequest("command:test:0002");
        _ = await service.CreateCommandAsync(new HumanActor(SubjectId), instanceId, request);
        CreateAdministrativeCommandRequest changed = request with { Reason = "A different approved operational reason" };

        CommandCreationResult result = await service.CreateCommandAsync(
            new HumanActor(SubjectId),
            instanceId,
            changed);

        Assert.Equal(CommandCreationDisposition.IdempotencyConflict, result.Disposition);
        Assert.Equal("command.idempotency_conflict", result.ErrorCode);
        await using ServerDbContext verification = new(options);
        Assert.Equal(1, await verification.AdministrativeCommands.CountAsync());
        Assert.Equal(2, await verification.AuditEntries.CountAsync());
    }

    [Fact]
    public async Task MissingPermissionDeniesCommandAndWritesAudit()
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        DbContextOptions<ServerDbContext> options = Options(connection);
        Guid instanceId = Guid.NewGuid();
        await SeedAsync(
            options,
            instanceId,
            "production",
            PlatformPermissions.InstancesRead,
            "Global",
            "*");

        CommandCreationResult result = await Service(options).CreateCommandAsync(
            new HumanActor(SubjectId),
            instanceId,
            CommandRequest("command:test:0003"));

        Assert.Equal(CommandCreationDisposition.Denied, result.Disposition);
        await using ServerDbContext verification = new(options);
        Assert.Empty(await verification.AdministrativeCommands.ToArrayAsync());
        AuditEntryRow audit = Assert.Single(await verification.AuditEntries.ToArrayAsync());
        Assert.Equal("Denied", audit.Outcome);
        Assert.DoesNotContain("command:test:0003", audit.DetailsJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExpiredRoleAssignmentDoesNotAuthorizeCommand()
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        DbContextOptions<ServerDbContext> options = Options(connection);
        Guid instanceId = Guid.NewGuid();
        await SeedAsync(
            options,
            instanceId,
            "production",
            PlatformPermissions.CommandsCreate,
            "Global",
            "*");
        await using (ServerDbContext setup = new(options))
        {
            RoleAssignmentRow assignment = await setup.RoleAssignments.SingleAsync();
            assignment.ExpiresAt = Now.AddSeconds(-1);
            await setup.SaveChangesAsync();
        }

        CommandCreationResult result = await Service(options).CreateCommandAsync(
            new HumanActor(SubjectId),
            instanceId,
            CommandRequest("command:test:expired"));

        Assert.Equal(CommandCreationDisposition.Denied, result.Disposition);
        await using ServerDbContext verification = new(options);
        Assert.Empty(await verification.AdministrativeCommands.ToArrayAsync());
    }

    [Fact]
    public async Task UnsupportedCapabilityCannotCreateCommand()
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        DbContextOptions<ServerDbContext> options = Options(connection);
        Guid instanceId = Guid.NewGuid();
        await SeedAsync(
            options,
            instanceId,
            "production",
            PlatformPermissions.CommandsCreate,
            "Global",
            "*",
            capabilityState: "Unsupported");

        CommandCreationResult result = await Service(options).CreateCommandAsync(
            new HumanActor(SubjectId),
            instanceId,
            CommandRequest("command:test:0004"));

        Assert.Equal(CommandCreationDisposition.CapabilityUnavailable, result.Disposition);
        await using ServerDbContext verification = new(options);
        Assert.Empty(await verification.AdministrativeCommands.ToArrayAsync());
        Assert.Equal("Denied", Assert.Single(await verification.AuditEntries.ToArrayAsync()).Outcome);
    }

    [Fact]
    public async Task SecretOrFreeFormParameterNamesAreRejectedBeforePersistence()
    {
        RecordingAuthorizedStore store = new();
        AuthorizedOperationsService service = new(store, new FixedTimeProvider(Now));
        CreateAdministrativeCommandRequest request = CommandRequest("command:test:0005") with
        {
            TypedParameters = JsonSerializer.SerializeToElement(new
            {
                values = new object[] { new object[] { new { password = "not-a-real-secret" } } },
            }),
        };

        CommandCreationResult result = await service.CreateCommandAsync(
            new HumanActor(SubjectId),
            Guid.NewGuid(),
            request);

        Assert.Equal(CommandCreationDisposition.Invalid, result.Disposition);
        Assert.Equal(0, store.CreateCalls);
        Assert.Equal(1, store.AuditCalls);
    }

    [Fact]
    public void HumanActorResolverRequiresAuthenticatedSubjectClaim()
    {
        HumanActorResolver resolver = new();
        ClaimsPrincipal valid = new(new ClaimsIdentity([new Claim("sub", SubjectId)], "fixture"));
        ClaimsPrincipal missingSubject = new(new ClaimsIdentity([], "fixture"));

        Assert.Equal(SubjectId, resolver.Resolve(valid)?.SubjectId);
        Assert.Null(resolver.Resolve(missingSubject));
        Assert.Null(resolver.Resolve(new ClaimsPrincipal()));
    }

    [Fact]
    public async Task AuditQueryRequiresGlobalPermissionAndWritesReadAudit()
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        DbContextOptions<ServerDbContext> options = Options(connection);
        await SeedAsync(
            options,
            Guid.NewGuid(),
            "production",
            PlatformPermissions.AuditRead,
            "Global",
            "*");
        await using (ServerDbContext setup = new(options))
        {
            setup.AuditEntries.AddRange(
                AuditEntry("fixture.one", Now.AddMinutes(-3)),
                AuditEntry("fixture.two", Now.AddMinutes(-2)),
                AuditEntry("fixture.three", Now.AddMinutes(-1)));
            await setup.SaveChangesAsync();
        }

        AuthorizedAuditPage page = await Service(options).QueryAuditAsync(
            new HumanActor(SubjectId),
            new AuditQuery(PageSize: 2));

        Assert.True(page.Authorized);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.NextOffset);
        Assert.Equal(Now, page.SnapshotAt);
        Assert.Equal("fixture.three", page.Items[0].Action);
        await using ServerDbContext verification = new(options);
        Assert.Equal(4, await verification.AuditEntries.CountAsync());
        Assert.Contains(await verification.AuditEntries.ToArrayAsync(), row => row.Action == "audit.read");
    }

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static DbContextOptions<ServerDbContext> Options(SqliteConnection connection) =>
        new DbContextOptionsBuilder<ServerDbContext>().UseSqlite(connection).Options;

    private static AuthorizedOperationsService Service(DbContextOptions<ServerDbContext> options) =>
        new(
            new AuthorizedOperationsStore(new TestServerContextFactory(options)),
            new FixedTimeProvider(Now));

    private static async Task SeedAsync(
        DbContextOptions<ServerDbContext> options,
        Guid instanceId,
        string environment,
        string permissionCode,
        string scopeType,
        string scopeValue,
        string capabilityState = "Supported")
    {
        Guid userId = Guid.NewGuid();
        Guid roleId = Guid.NewGuid();
        Guid permissionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await using ServerDbContext context = new(options);
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(new PlatformUserRow
        {
            UserId = userId,
            SubjectId = SubjectId,
            DisplayName = "Fixture Operator",
            State = "Active",
            CreatedAt = Now,
            UpdatedAt = Now,
            ConcurrencyToken = Guid.NewGuid(),
        });
        context.Roles.Add(new RoleRow
        {
            RoleId = roleId,
            Name = $"Fixture-{Guid.NewGuid():N}",
            Description = "Fixture role",
            IsSystem = false,
            ConcurrencyToken = Guid.NewGuid(),
        });
        context.Permissions.Add(new PermissionRow
        {
            PermissionId = permissionId,
            Code = permissionCode,
            Description = "Fixture permission",
        });
        context.RolePermissions.Add(new RolePermissionRow { RoleId = roleId, PermissionId = permissionId });
        context.RoleAssignments.Add(new RoleAssignmentRow
        {
            RoleAssignmentId = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            ScopeType = scopeType,
            ScopeValue = scopeValue,
            GrantedByUserId = userId,
            GrantedAt = Now.AddMinutes(-1),
        });
        context.Agents.Add(Agent(agentId));
        context.Instances.Add(Instance(instanceId, agentId, environment));
        context.AgentCapabilities.Add(new AgentCapabilityRow
        {
            AgentCapabilityId = Guid.NewGuid(),
            AgentId = agentId,
            CapabilityId = "service.restart",
            ProviderType = "postgresql",
            ProviderVersion = "fixture-provider",
            Platform = "fixture",
            State = capabilityState,
            ObservedAt = Now,
        });
        await context.SaveChangesAsync();
    }

    private static RegisteredAgentRow Agent(Guid agentId) => new()
    {
        AgentId = agentId,
        InstallationId = $"fixture-{agentId:N}",
        DisplayName = "Fixture Agent",
        Environment = "test",
        Platform = "fixture",
        AgentVersion = "fixture-agent",
        CertificateThumbprint = $"FIXTURE-{agentId:N}",
        State = "Active",
        EnrolledAt = Now,
        ConcurrencyToken = Guid.NewGuid(),
    };

    private static async Task AddObservationAsync(
        DbContextOptions<ServerDbContext> options,
        Guid instanceId,
        HealthStatus status,
        EvidenceLevel evidenceLevel)
    {
        await using ServerDbContext context = new(options);
        DatabaseInstanceRow instance = await context.Instances.SingleAsync(row => row.InstanceId == instanceId);
        Guid agentId = instance.AssignedAgentId!.Value;
        Guid observationId = Guid.NewGuid();
        DateTimeOffset observedAt = Now.AddSeconds(-2);
        DateTimeOffset receivedAt = Now.AddSeconds(-1);
        context.HealthSamples.Add(new HealthSampleRow
        {
            ObservationId = observationId,
            InstanceId = instanceId,
            AgentId = agentId,
            MessageId = Guid.NewGuid(),
            Sequence = 1,
            ProviderType = instance.ProviderType,
            ProviderVersion = "fixture-provider",
            Status = status.ToString(),
            Method = "fixture-read",
            EvidenceLevel = evidenceLevel.ToString(),
            ObservedAt = observedAt,
            ReceivedAt = receivedAt,
            DurationMilliseconds = 175,
            AttemptCount = 1,
        });
        context.InstanceObservationStates.Add(new InstanceObservationStateRow
        {
            InstanceId = instanceId,
            AgentId = agentId,
            LastProcessedSequence = 1,
            ObservationId = observationId,
            Status = status.ToString(),
            ObservedAt = observedAt,
            ReceivedAt = receivedAt,
            ConcurrencyToken = Guid.NewGuid(),
        });
        await context.SaveChangesAsync();
    }

    private static DatabaseInstanceRow Instance(Guid instanceId, Guid agentId, string environment) => new()
    {
        InstanceId = instanceId,
        DisplayName = $"Fixture {environment}",
        ProviderType = "postgresql",
        Environment = environment,
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

    private static CreateAdministrativeCommandRequest CommandRequest(string idempotencyKey) =>
        new(
            idempotencyKey,
            "service.restart",
            JsonSerializer.SerializeToElement(new { }),
            "Approved fixture restart reason",
            Now.AddMinutes(5),
            "fixture-agent",
            "fixture-provider");

    private static AuditEntryRow AuditEntry(string action, DateTimeOffset occurredAt) => new()
    {
        AuditEntryId = Guid.NewGuid(),
        OccurredAt = occurredAt,
        ActorType = "Human",
        ActorId = "fixture-actor",
        Action = action,
        TargetType = "Fixture",
        TargetId = "fixture",
        Outcome = "Succeeded",
        CorrelationId = Guid.NewGuid(),
        DetailsJson = "{}",
    };

    private sealed class TestServerContextFactory(DbContextOptions<ServerDbContext> options) : IDbContextFactory<ServerDbContext>
    {
        public ServerDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingAuthorizedStore : IAuthorizedOperationsStore
    {
        public int CreateCalls { get; private set; }

        public int AuditCalls { get; private set; }

        public ValueTask<IReadOnlyList<AuthorizedInstance>> GetAuthorizedInstancesAsync(
            string subjectId,
            string permissionCode,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<AuthorizedInstance>>([]);

        public ValueTask<DesktopFleetApiSnapshot> GetAuthorizedDesktopFleetSnapshotAsync(
            string subjectId,
            string permissionCode,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new DesktopFleetApiSnapshot(
                DesktopFleetApiContract.CurrentSchemaVersion,
                now,
                []));

        public ValueTask<CommandCreationResult> CreateCommandAsync(
            string subjectId,
            Guid instanceId,
            string permissionCode,
            ValidatedCommandRequest request,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            CreateCalls++;
            return ValueTask.FromResult(new CommandCreationResult(CommandCreationDisposition.Created, null));
        }

        public ValueTask AuditCommandRejectionAsync(
            string subjectId,
            Guid instanceId,
            string errorCode,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            AuditCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask<AuthorizedAuditPage> QueryAuditAsync(
            string subjectId,
            string permissionCode,
            AuditQuery query,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new AuthorizedAuditPage(false, [], null, now));
    }
}
