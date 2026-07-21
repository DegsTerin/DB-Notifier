// Module purpose: Verifies independent Server and Agent refusal of unsafe assignment content before transmission or persistence.
using System.Text.Json;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.UnitTests;

/// <summary>Exercises the canonical non-secret assignment policy at both durable trust boundaries.</summary>
public sealed class AgentAssignmentValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 18, 0, 0, TimeSpan.Zero);

    /// <summary>Proves secret-like fields, nesting, unknown providers and non-monitoring references fail closed.</summary>
    [Fact]
    public void ValidatorRejectsUnsafeOrUnknownAssignmentMaterial()
    {
        IAgentAssignmentValidator validator = AgentAssignmentValidationFixture.Create("fixture");
        Assert.True(validator.TryValidate(CreateAssignment(), "test", out string? validCode));
        Assert.Null(validCode);

        Assert.False(validator.TryValidate(
            CreateAssignment(endpointJson: "{\"host\":\"sandbox.invalid\",\"port\":5432,\"password\":\"redacted\"}"),
            "test",
            out string? secretFieldCode));
        Assert.Equal("assignments.secret_like_field_rejected", secretFieldCode);

        Assert.False(validator.TryValidate(
            CreateAssignment(tagsJson: "{\"metadata\":{\"connectionString\":\"redacted\"}}"),
            "test",
            out string? nestedSecretCode));
        Assert.Equal("assignments.secret_like_field_rejected", nestedSecretCode);

        Assert.False(validator.TryValidate(
            CreateAssignment(endpointJson: "{\"host\":\"sandbox.invalid\",\"port\":5432,\"unknown\":true}"),
            "test",
            out string? unknownPropertyCode));
        Assert.Equal("assignments.provider_configuration_invalid", unknownPropertyCode);

        Assert.False(validator.TryValidate(
            CreateAssignment(providerType: "unknown-provider"),
            "test",
            out string? unknownProviderCode));
        Assert.Equal("assignments.provider_configuration_invalid", unknownProviderCode);

        string administrationReference = AgentAssignmentValidationFixture.MonitoringReference
            .Replace("\"Monitoring\"", "\"Administration\"", StringComparison.Ordinal);
        Assert.False(validator.TryValidate(
            CreateAssignment(credentialReference: administrationReference),
            "test",
            out string? purposeCode));
        Assert.Equal("assignments.credential_reference_invalid", purposeCode);

        string unknownCredentialField = AgentAssignmentValidationFixture.MonitoringReference
            .Replace("}", ",\"unexpected\":true}", StringComparison.Ordinal);
        Assert.False(validator.TryValidate(
            CreateAssignment(credentialReference: unknownCredentialField),
            "test",
            out string? credentialSchemaCode));
        Assert.Equal("assignments.credential_reference_invalid", credentialSchemaCode);
    }

    /// <summary>Proves the Server refuses an unsafe persisted row before constructing a transmissible snapshot.</summary>
    [Fact]
    public async Task ServerRefusesUnsafeStoredAssignmentBeforeSerialisation()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        await using (ServerDbContext context = new(options))
        {
            await context.Database.EnsureCreatedAsync();
            Guid agentId = Guid.NewGuid();
            context.Agents.Add(new RegisteredAgentRow
            {
                AgentId = agentId,
                InstallationId = "installation:assignment-validator",
                DisplayName = "Assignment validator fixture",
                Environment = "test",
                Platform = "windows-x64",
                AgentVersion = "1.0.0-test",
                CertificateThumbprint = new string('A', 64),
                State = "Active",
                EnrolledAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Instances.Add(CreateServerRow(agentId));
            await context.SaveChangesAsync();
        }

        Guid expectedAgentId;
        await using (ServerDbContext context = new(options))
        {
            expectedAgentId = await context.Agents.Select(row => row.AgentId).SingleAsync();
        }
        AgentFleetStore store = new(
            new ServerContextFactory(options),
            AgentAssignmentValidationFixture.Create("fixture"));
        AgentAssignmentOutcome outcome = await store.GetAssignmentsAsync(
            expectedAgentId,
            "1.0.0-test",
            null,
            Now,
            CancellationToken.None);

        Assert.Equal(AgentAssignmentDisposition.InvalidStoredConfiguration, outcome.Disposition);
        Assert.Null(outcome.Snapshot);
        Assert.Equal("assignments.invalid_stored_configuration", outcome.ErrorCode);
    }

    /// <summary>Proves an unsafe replacement is refused before SQLite deletion and leaves the LKG unchanged.</summary>
    [Fact]
    public async Task AgentRefusalPreservesLastKnownValidAssignments()
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
            new AgentContextFactory(options),
            AgentAssignmentValidationFixture.Create("fixture"));
        Guid agentId = Guid.NewGuid();
        AgentLocalRegistration registration = new(
            agentId,
            "installation:lkg-validator",
            "test",
            "sandbox-identity:lkg-validator",
            new string('B', 64),
            Now.AddHours(1),
            Now,
            AgentLocalIdentityState.Active,
            null);
        await store.SaveEnrollmentAsync(registration, CancellationToken.None);
        AgentFleetOperationLease lease = Assert.IsType<AgentFleetOperationLease>(
            await store.TryAcquireOperationLeaseAsync(
                agentId,
                "sandbox:assignment-validator",
                AgentFleetOperationKind.AssignmentReconciliation,
                Now,
                TimeSpan.FromMinutes(1),
                CancellationToken.None));
        AgentAssignmentSnapshot valid = CreateSnapshot(agentId, CreateAssignment(displayName: "Last known valid"));
        Assert.True(await store.ApplyAssignmentsAsync(valid, $"\"{valid.Version}\"", Now, lease, CancellationToken.None));

        AgentReadOnlyAssignment unsafeAssignment = CreateAssignment(
            displayName: "Rejected replacement",
            tagsJson: "{\"nested\":{\"token\":\"redacted\"}}");
        AgentAssignmentSnapshot unsafeSnapshot = CreateSnapshot(agentId, unsafeAssignment);
        Assert.False(await store.ApplyAssignmentsAsync(
            unsafeSnapshot,
            $"\"{unsafeSnapshot.Version}\"",
            Now.AddSeconds(1),
            lease,
            CancellationToken.None));

        await using AgentDbContext verification = new(options);
        AgentInstanceAssignmentRow retained = await verification.InstanceAssignments.AsNoTracking().SingleAsync();
        Assert.Equal("Last known valid", retained.DisplayName);
        Assert.Equal(valid.Version, (await store.GetAssignmentStateAsync(agentId, CancellationToken.None)).Version);
    }

    private static AgentReadOnlyAssignment CreateAssignment(
        string displayName = "Safe fixture",
        string providerType = "fixture",
        string endpointJson = "{\"host\":\"sandbox.invalid\",\"port\":5432}",
        string tagsJson = "{\"purpose\":\"test\"}",
        string? credentialReference = AgentAssignmentValidationFixture.MonitoringReference)
    {
        using JsonDocument endpoint = JsonDocument.Parse(endpointJson);
        using JsonDocument tags = JsonDocument.Parse(tagsJson);
        return new AgentReadOnlyAssignment(
            Guid.NewGuid(),
            displayName,
            providerType,
            "test",
            endpoint.RootElement.Clone(),
            credentialReference,
            tags.RootElement.Clone(),
            30,
            5,
            1,
            Now);
    }

    private static AgentAssignmentSnapshot CreateSnapshot(Guid agentId, AgentReadOnlyAssignment assignment)
    {
        string version = AgentAssignmentVersion.Compute(agentId, [assignment]);
        return new AgentAssignmentSnapshot(
            AgentFleetProtocol.CurrentSchemaVersion,
            agentId,
            version,
            Now,
            [assignment]);
    }

    private static DatabaseInstanceRow CreateServerRow(Guid agentId) => new()
    {
        InstanceId = Guid.NewGuid(),
        DisplayName = "Unsafe server fixture",
        ProviderType = "fixture",
        Environment = "test",
        EndpointJson = "{\"host\":\"sandbox.invalid\",\"port\":5432,\"privateKey\":\"redacted\"}",
        MonitoringCredentialReference = AgentAssignmentValidationFixture.MonitoringReference,
        AssignedAgentId = agentId,
        TagsJson = "{}",
        IntervalSeconds = 30,
        TimeoutSeconds = 5,
        RetryCount = 1,
        Enabled = true,
        CreatedAt = Now,
        UpdatedAt = Now,
        ConcurrencyToken = Guid.NewGuid(),
    };

    private sealed class AgentContextFactory(DbContextOptions<AgentDbContext> options)
        : IDbContextFactory<AgentDbContext>
    {
        /// <inheritdoc />
        public AgentDbContext CreateDbContext() => new(options);
    }

    private sealed class ServerContextFactory(DbContextOptions<ServerDbContext> options)
        : IDbContextFactory<ServerDbContext>
    {
        /// <inheritdoc />
        public ServerDbContext CreateDbContext() => new(options);
    }
}
