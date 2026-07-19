// Module purpose: Verifies that the Dashboard TV sandbox projection exposes only bounded, reconciled synthetic observation evidence.
using System.Text.Json;
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Server.Api;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.UnitTests;

/// <summary>
/// Protects the fail-closed mapping between Server observation persistence and the read-only Dashboard TV contract.
/// </summary>
public sealed class DashboardTvSyntheticObservationSnapshotSourceTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ReceivedAt = ObservedAt.AddSeconds(1);

    /// <summary>Verifies valid synthetic evidence, stable reads and exclusion of persistence-only details.</summary>
    [Fact]
    public async Task ReadAsyncMapsOnlyTheSafeSyntheticProjectionAndRemainsStable()
    {
        await using ProjectionFixture fixture = await ProjectionFixture.CreateAsync();
        await fixture.SeedValidProjectionAsync();
        DashboardTvSyntheticObservationSnapshotSource source = fixture.CreateSource();

        DashboardTvSnapshot first = await source.ReadAsync(CancellationToken.None);
        DashboardTvSnapshot second = await source.ReadAsync(CancellationToken.None);
        string firstJson = JsonSerializer.Serialize(first, JsonSerializerOptions.Web);
        string secondJson = JsonSerializer.Serialize(second, JsonSerializerOptions.Web);

        Assert.Equal(DashboardTvSnapshotContract.CurrentSchemaVersion, first.SchemaVersion);
        Assert.Equal("2026-07-18T10:00:01.000Z", first.GeneratedAt);
        Assert.Equal(first.GeneratedAt, second.GeneratedAt);
        Assert.Equal(firstJson, secondJson);
        DashboardTvInventoryItem item = Assert.Single(first.Items);
        Assert.Equal(ProjectionFixture.InstanceId, item.InstanceId);
        Assert.Equal("Synthetic inventory item", item.DisplayName);
        Assert.Equal("fixture-provider", item.ProviderType);
        Assert.Equal("Synthetic sandbox evidence", item.SupportLabel);
        Assert.Equal("Sandbox", item.Environment);
        Assert.Equal("Local observation pipeline", item.LocationLabel);
        Assert.Equal("degraded", item.Status);
        Assert.Equal("2026-07-18T10:00:00.000Z", item.ObservedAt);
        Assert.Equal("2026-07-18T10:00:01.000Z", item.ReceivedAt);
        Assert.Equal(42, item.LatencyMilliseconds);
        Assert.True(item.Enabled);
        Assert.True(DashboardTvSnapshotValidator.TryValidate(first, ProjectionFixture.Now, out string errorCode));
        Assert.Empty(errorCode);

        Assert.DoesNotContain(ProjectionFixture.EndpointCanary, firstJson, StringComparison.Ordinal);
        Assert.DoesNotContain(ProjectionFixture.MonitoringReferenceCanary, firstJson, StringComparison.Ordinal);
        Assert.DoesNotContain(ProjectionFixture.AdministrativeReferenceCanary, firstJson, StringComparison.Ordinal);
        Assert.DoesNotContain(ProjectionFixture.TagsCanary, firstJson, StringComparison.Ordinal);
        Assert.DoesNotContain(ProjectionFixture.ErrorCanary, firstJson, StringComparison.Ordinal);
        Assert.DoesNotContain(ProjectionFixture.RedactedDetailsCanary, firstJson, StringComparison.Ordinal);
    }

    /// <summary>Verifies that an empty authoritative projection retains one stable timestamp and representation.</summary>
    [Fact]
    public async Task ReadAsyncReturnsAStableEmptySnapshot()
    {
        await using ProjectionFixture fixture = await ProjectionFixture.CreateAsync();
        DashboardTvSyntheticObservationSnapshotSource source = fixture.CreateSource();

        DashboardTvSnapshot first = await source.ReadAsync(CancellationToken.None);
        DashboardTvSnapshot second = await source.ReadAsync(CancellationToken.None);

        Assert.Empty(first.Items);
        Assert.Equal("2026-07-18T10:01:01.000Z", first.GeneratedAt);
        Assert.Equal(
            JsonSerializer.Serialize(first, JsonSerializerOptions.Web),
            JsonSerializer.Serialize(second, JsonSerializerOptions.Web));
    }

    /// <summary>Verifies that the item bound is enforced before any missing sample evidence is inspected.</summary>
    [Fact]
    public async Task ReadAsyncRejectsAnOversizedProjectionBeforeReadingSampleEvidence()
    {
        await using ProjectionFixture fixture = await ProjectionFixture.CreateAsync();
        await fixture.SeedOversizedProjectionWithoutSamplesAsync();
        DashboardTvSyntheticObservationSnapshotSource source = fixture.CreateSource();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => source.ReadAsync(CancellationToken.None).AsTask());

        Assert.Equal("dashboard_tv.synthetic_projection_limit_exceeded", exception.Message);
    }

    /// <summary>Verifies that state provenance cannot name a different Agent sequence than its source sample.</summary>
    [Fact]
    public async Task ReadAsyncRejectsAStateAndSampleSequenceMismatch()
    {
        await using ProjectionFixture fixture = await ProjectionFixture.CreateAsync();
        await fixture.SeedValidProjectionAsync(stateSequence: 2, sampleSequence: 1);
        DashboardTvSyntheticObservationSnapshotSource source = fixture.CreateSource();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => source.ReadAsync(CancellationToken.None).AsTask());

        Assert.Equal("dashboard_tv.synthetic_projection_evidence_invalid", exception.Message);
    }

    /// <summary>Verifies that operational and otherwise non-synthetic evidence never enters this sandbox projection.</summary>
    /// <param name="evidenceLevel">Stored non-synthetic evidence level to reject.</param>
    [Theory]
    [InlineData(nameof(EvidenceLevel.ProviderAuthenticated))]
    [InlineData(nameof(EvidenceLevel.ProviderReadiness))]
    [InlineData(nameof(EvidenceLevel.TransportOnly))]
    [InlineData(nameof(EvidenceLevel.Unknown))]
    public async Task ReadAsyncRejectsEveryNonSyntheticEvidenceLevel(string evidenceLevel)
    {
        await using ProjectionFixture fixture = await ProjectionFixture.CreateAsync();
        await fixture.SeedValidProjectionAsync(evidenceLevel: evidenceLevel);
        DashboardTvSyntheticObservationSnapshotSource source = fixture.CreateSource();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => source.ReadAsync(CancellationToken.None).AsTask());

        Assert.Equal("dashboard_tv.synthetic_projection_evidence_invalid", exception.Message);
    }

    /// <summary>Verifies that synthetic evidence can never project the healthy state prohibited at ingestion.</summary>
    [Fact]
    public async Task ReadAsyncRejectsSyntheticHealthyEvidence()
    {
        await using ProjectionFixture fixture = await ProjectionFixture.CreateAsync();
        await fixture.SeedValidProjectionAsync(status: nameof(HealthStatus.Healthy));
        DashboardTvSyntheticObservationSnapshotSource source = fixture.CreateSource();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => source.ReadAsync(CancellationToken.None).AsTask());

        Assert.Equal("dashboard_tv.synthetic_projection_evidence_invalid", exception.Message);
    }

    /// <summary>Owns one isolated in-memory Server database and deterministic observation fixture.</summary>
    private sealed class ProjectionFixture : IAsyncDisposable
    {
        public static readonly Guid AgentId = Guid.Parse("ff90886e-b2ec-425e-b44d-ea210a460f32");
        public static readonly Guid InstanceId = Guid.Parse("55666056-f350-46e9-bb40-ce9e70b6041b");
        public static readonly Guid ObservationId = Guid.Parse("f906a915-b1f1-4629-b992-6b041cc41136");
        public const string EndpointCanary = "endpoint-projection-canary";
        public const string MonitoringReferenceCanary = "monitoring-reference-canary";
        public const string AdministrativeReferenceCanary = "administrative-reference-canary";
        public const string TagsCanary = "tags-projection-canary";
        public const string ErrorCanary = "error-projection-canary";
        public const string RedactedDetailsCanary = "details-projection-canary";

        private readonly SqliteConnection connection;
        private readonly DbContextOptions<ServerDbContext> options;

        private ProjectionFixture(SqliteConnection connection, DbContextOptions<ServerDbContext> options)
        {
            this.connection = connection;
            this.options = options;
        }

        public static DateTimeOffset Now => ReceivedAt.AddMinutes(1);

        /// <summary>Creates the database and its schema while retaining the in-memory connection for the test lifetime.</summary>
        public static async Task<ProjectionFixture> CreateAsync()
        {
            SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
                .UseSqlite(connection)
                .Options;
            await using ServerDbContext context = new(options);
            await context.Database.EnsureCreatedAsync();
            return new ProjectionFixture(connection, options);
        }

        /// <summary>Creates the projection adapter against this fixture's context factory and deterministic clock.</summary>
        public DashboardTvSyntheticObservationSnapshotSource CreateSource() =>
            new(new TestServerContextFactory(options), new FixedTimeProvider(Now));

        /// <summary>Seeds one reconciled state and its source sample, with optional provenance mutations.</summary>
        public async Task SeedValidProjectionAsync(
            long stateSequence = 1,
            long sampleSequence = 1,
            string evidenceLevel = nameof(EvidenceLevel.Synthetic),
            string status = nameof(HealthStatus.Degraded))
        {
            await using ServerDbContext context = new(options);
            context.Agents.Add(CreateAgent());
            context.Instances.Add(CreateInstance(InstanceId, "Synthetic inventory item"));
            context.InstanceObservationStates.Add(CreateState(
                InstanceId,
                ObservationId,
                stateSequence,
                ReceivedAt,
                status));
            context.HealthSamples.Add(new HealthSampleRow
            {
                ObservationId = ObservationId,
                InstanceId = InstanceId,
                AgentId = AgentId,
                MessageId = Guid.Parse("94a7b660-cc48-4210-a438-dc47d2bad727"),
                Sequence = sampleSequence,
                ProviderType = "fixture-provider",
                ProviderVersion = "1.0-test",
                Status = status,
                Method = "synthetic-fixture",
                EvidenceLevel = evidenceLevel,
                ObservedAt = ObservedAt,
                ReceivedAt = ReceivedAt,
                DurationMilliseconds = 42,
                AttemptCount = 1,
                ErrorCode = ErrorCanary,
                RedactedDetailsJson = $"{{\"marker\":\"{RedactedDetailsCanary}\"}}",
                PayloadHash = new string('a', 64),
            });
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds one row beyond the public item limit and intentionally omits samples so the expected error proves
        /// that the bounded state query rejects the corpus before sample evidence is materialised.
        /// </summary>
        public async Task SeedOversizedProjectionWithoutSamplesAsync()
        {
            await using ServerDbContext context = new(options);
            context.Agents.Add(CreateAgent());
            for (int index = 0; index <= DashboardTvSnapshotContract.MaximumItemCount; index++)
            {
                Guid instanceId = CreateDeterministicGuid(index + 1);
                Guid observationId = CreateDeterministicGuid(index + 10_001);
                context.Instances.Add(CreateInstance(instanceId, $"Synthetic inventory item {index:D3}"));
                context.InstanceObservationStates.Add(CreateState(
                    instanceId,
                    observationId,
                    index + 1,
                    ReceivedAt.AddMilliseconds(index)));
            }

            await context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync() => await connection.DisposeAsync();

        /// <summary>Creates the enrolled sandbox Agent required by the persistence foreign keys.</summary>
        private static RegisteredAgentRow CreateAgent() => new()
        {
            AgentId = AgentId,
            InstallationId = "projection-fixture-agent",
            DisplayName = "Projection fixture Agent",
            Environment = "Sandbox",
            Platform = "test",
            AgentVersion = "1.0-test",
            CertificateThumbprint = new string('b', 64),
            State = "Active",
            EnrolledAt = ObservedAt.AddMinutes(-1),
            LastSeenAt = ReceivedAt,
            ConcurrencyToken = Guid.Parse("4aa36b7f-bfbb-42cc-b627-116d79cd07f5"),
        };

        /// <summary>Creates one enabled assigned instance with persistence-only canaries that must not be exposed.</summary>
        private static DatabaseInstanceRow CreateInstance(Guid instanceId, string displayName) => new()
        {
            InstanceId = instanceId,
            DisplayName = displayName,
            ProviderType = "fixture-provider",
            Environment = "Sandbox",
            EndpointJson = $"{{\"marker\":\"{EndpointCanary}\"}}",
            MonitoringCredentialReference = MonitoringReferenceCanary,
            AdministrativeCredentialReference = AdministrativeReferenceCanary,
            AssignedAgentId = AgentId,
            TagsJson = $"{{\"marker\":\"{TagsCanary}\"}}",
            IntervalSeconds = 30,
            TimeoutSeconds = 5,
            RetryCount = 0,
            Enabled = true,
            CreatedAt = ObservedAt.AddMinutes(-1),
            UpdatedAt = ReceivedAt,
            ConcurrencyToken = Guid.NewGuid(),
        };

        /// <summary>Creates one reconciled state row whose sample provenance is supplied separately.</summary>
        private static InstanceObservationStateRow CreateState(
            Guid instanceId,
            Guid observationId,
            long sequence,
            DateTimeOffset receivedAt,
            string status = nameof(HealthStatus.Degraded)) => new()
            {
                InstanceId = instanceId,
                AgentId = AgentId,
                LastProcessedSequence = sequence,
                ObservationId = observationId,
                Status = status,
                ObservedAt = ObservedAt,
                ReceivedAt = receivedAt,
                ConcurrencyToken = Guid.NewGuid(),
            };

        /// <summary>Creates stable non-empty identifiers without relying on shared process state.</summary>
        private static Guid CreateDeterministicGuid(int value)
        {
            Span<byte> bytes = stackalloc byte[16];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            bytes[15] = 1;
            return new Guid(bytes);
        }
    }

    /// <summary>Creates short-lived Server contexts over the fixture-owned open SQLite connection.</summary>
    private sealed class TestServerContextFactory(DbContextOptions<ServerDbContext> options)
        : IDbContextFactory<ServerDbContext>
    {
        /// <inheritdoc />
        public ServerDbContext CreateDbContext() => new(options);

        /// <inheritdoc />
        public Task<ServerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateDbContext());
        }
    }

    /// <summary>Provides one immutable UTC instant without consulting the host clock.</summary>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }
}
