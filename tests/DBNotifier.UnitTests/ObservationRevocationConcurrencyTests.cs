// Module purpose: Verifies deterministic ordering between authoritative observation ingestion and principal Agent revocation.
using System.Reflection;
using DBNotifier.Application.Access;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Application.Synchronization;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DBNotifier.UnitTests;

/// <summary>
/// Exercises both sides of the Agent identity transaction fence with isolated SQLite contexts. The tests pause before
/// either transaction's first write so database write contention cannot substitute for the shared ordering boundary.
/// </summary>
public sealed class ObservationRevocationConcurrencyTests
{
    private const string SubjectId = "oidc:r-egress-fixture";
    private static readonly DateTimeOffset Now = new(2026, 7, 25, 18, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Proves an ingestion transaction that has already fenced an active Agent must finish before revocation can commit.
    /// </summary>
    [Fact]
    public async Task IngestionFirstCommitsBeforeConcurrentRevocation()
    {
        await using ConcurrencyFixture fixture = await ConcurrencyFixture.CreateAsync(
            PauseTarget.ObservationCursorCreation);
        Task<ObservationItemResult> ingestion = fixture.IngestAsync().AsTask();
        await fixture.Pause.WaitUntilReachedAsync();

        Task<AgentRevocationOutcome> revocation = fixture.RevokeAsync().AsTask();
        try
        {
            fixture.AssertCompetingStoreIsFenced(revocation);
            Assert.Equal("Active", await fixture.ReadAgentStateAsync());
        }
        finally
        {
            await fixture.ReleaseAndDrainAsync(ingestion, revocation);
        }

        ObservationItemResult ingestionResult = await ingestion.WaitAsync(TimeSpan.FromSeconds(10));
        AgentRevocationOutcome revocationResult = await revocation.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(ObservationIngestionDisposition.Accepted, ingestionResult.Disposition);
        Assert.Equal(AgentRevocationDisposition.Revoked, revocationResult.Disposition);
        await fixture.AssertAcceptedThenRevokedAsync();
    }

    /// <summary>
    /// Proves a revocation transaction that owns the fence commits before later ingestion evaluates Agent authority.
    /// </summary>
    [Fact]
    public async Task RevocationFirstRejectsConcurrentIngestionWithoutAcceptedEffects()
    {
        await using ConcurrencyFixture fixture = await ConcurrencyFixture.CreateAsync(
            PauseTarget.PrincipalRevocation);
        Task<AgentRevocationOutcome> revocation = fixture.RevokeAsync().AsTask();
        await fixture.Pause.WaitUntilReachedAsync();

        Task<ObservationItemResult> ingestion = fixture.IngestAsync().AsTask();
        try
        {
            fixture.AssertCompetingStoreIsFenced(ingestion);
            Assert.Equal("Active", await fixture.ReadAgentStateAsync());
        }
        finally
        {
            await fixture.ReleaseAndDrainAsync(revocation, ingestion);
        }

        AgentRevocationOutcome revocationResult = await revocation.WaitAsync(TimeSpan.FromSeconds(10));
        ObservationItemResult ingestionResult = await ingestion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(AgentRevocationDisposition.Revoked, revocationResult.Disposition);
        Assert.Equal(ObservationIngestionDisposition.Rejected, ingestionResult.Disposition);
        Assert.Equal("agent.not_active", ingestionResult.ErrorCode);
        await fixture.AssertRevokedBeforeRejectionAsync();
    }

    /// <summary>Proves a revocation timestamp fails closed even if a drifted row still carries the active label.</summary>
    [Fact]
    public async Task RevocationTimestampPreventsIngestionWhenStateLabelIsStillActive()
    {
        await using ConcurrencyFixture fixture = await ConcurrencyFixture.CreateAsync(
            PauseTarget.ObservationCursorCreation,
            armPause: false);
        await fixture.SetRevocationTimestampOnlyAsync();

        ObservationItemResult result = await fixture.IngestAsync();

        Assert.Equal(ObservationIngestionDisposition.Rejected, result.Disposition);
        Assert.Equal("agent.not_active", result.ErrorCode);
        await fixture.AssertDriftedIdentityRejectedAsync();
    }

    /// <summary>
    /// Proves rejecting a missing prefix after revocation can reconcile only a successor accepted before revocation.
    /// </summary>
    [Fact]
    public async Task RevokedGapConsumptionReconcilesOnlyPreviouslyAcceptedSuccessor()
    {
        await using ConcurrencyFixture fixture = await ConcurrencyFixture.CreateAsync(
            PauseTarget.ObservationCursorCreation,
            armPause: false);
        ObservationSyncMessage successor = fixture.CreateMessage(sequence: 2);

        ObservationItemResult successorResult = await fixture.IngestAsync(successor);
        Assert.Equal(ObservationIngestionDisposition.Accepted, successorResult.Disposition);
        await fixture.AssertSuccessorBufferedBehindGapAsync(successor);

        AgentRevocationOutcome revocationResult = await fixture.RevokeAsync();
        Assert.Equal(AgentRevocationDisposition.Revoked, revocationResult.Disposition);

        ObservationSyncMessage rejectedGap = fixture.CreateMessage(sequence: 1);
        ObservationItemResult gapResult = await fixture.IngestAsync(rejectedGap);

        Assert.Equal(ObservationIngestionDisposition.Rejected, gapResult.Disposition);
        Assert.Equal("agent.not_active", gapResult.ErrorCode);
        await fixture.AssertRejectedGapReleasedOnlyAcceptedSuccessorAsync(successor, rejectedGap);
    }

    /// <summary>
    /// Proves post-failure classification preserves a buffered accepted replay when revocation is already durable.
    /// </summary>
    [Fact]
    public async Task PersistenceFailurePreservesAcceptedBufferedReplayAfterRevocation()
    {
        await using ConcurrencyFixture fixture = await ConcurrencyFixture.CreateAsync(
            PauseTarget.ObservationCursorCreation,
            armPause: false);
        ObservationSyncMessage accepted = fixture.CreateMessage(sequence: 2);
        fixture.ArmAcceptedReplayAfterPersistenceFailure(accepted);

        ObservationItemResult result = await fixture.IngestAsync(accepted);

        Assert.Equal(ObservationIngestionDisposition.Duplicate, result.Disposition);
        Assert.Null(result.ErrorCode);
        await fixture.AssertAcceptedReplayWonInactiveClassificationAsync(accepted);
    }

    /// <summary>Owns one shared in-memory Server database and the two stores participating in the race.</summary>
    private sealed class ConcurrencyFixture : IAsyncDisposable
    {
        private readonly SqliteConnection anchorConnection;
        private readonly DbContextOptions<ServerDbContext> options;
        private readonly AgentFleetStore agentFleetStore;
        private readonly ServerObservationIngestionStore ingestionStore;
        private readonly ServerContextFactory contextFactory;

        /// <summary>Initialises the isolated fixture after its schema and authority rows have been seeded.</summary>
        /// <param name="anchorConnection">Connection retaining the named in-memory SQLite database.</param>
        /// <param name="options">Context options shared by independently opened test contexts.</param>
        /// <param name="pause">One-shot interceptor controlling the selected pre-write boundary.</param>
        /// <param name="agentId">Synthetic Agent protected by the transaction fence.</param>
        /// <param name="instanceId">Synthetic instance assigned to the Agent.</param>
        private ConcurrencyFixture(
            SqliteConnection anchorConnection,
            DbContextOptions<ServerDbContext> options,
            PauseBeforeFirstWriteInterceptor pause,
            Guid agentId,
            Guid instanceId)
        {
            this.anchorConnection = anchorConnection;
            this.options = options;
            Pause = pause;
            AgentId = agentId;
            InstanceId = instanceId;
            contextFactory = new ServerContextFactory(options);
            agentFleetStore = new AgentFleetStore(contextFactory, AgentAssignmentValidationFixture.Create());
            ingestionStore = new ServerObservationIngestionStore(contextFactory);
        }

        /// <summary>Gets the one-shot pre-write pause used by the current interleaving.</summary>
        internal PauseBeforeFirstWriteInterceptor Pause { get; }

        /// <summary>Gets the synthetic Agent identifier.</summary>
        internal Guid AgentId { get; }

        /// <summary>Gets the synthetic assigned instance identifier.</summary>
        internal Guid InstanceId { get; }

        /// <summary>Creates and seeds one independently named shared-memory SQLite fixture.</summary>
        /// <param name="target">Transaction write boundary that should pause exactly once.</param>
        /// <param name="armPause">Whether to arm the selected boundary after setup.</param>
        /// <returns>A ready fixture whose pause matches the requested armed state.</returns>
        internal static async Task<ConcurrencyFixture> CreateAsync(PauseTarget target, bool armPause = true)
        {
            string connectionString =
                $"Data Source=db-notifier-r-egress-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            SqliteConnection anchor = new(connectionString);
            await anchor.OpenAsync();
            PauseBeforeFirstWriteInterceptor pause = new(target);
            DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
                .UseSqlite(connectionString)
                .AddInterceptors(pause)
                .Options;
            Guid agentId = Guid.NewGuid();
            Guid instanceId = Guid.NewGuid();

            try
            {
                await using ServerDbContext setup = new(options);
                await setup.Database.EnsureCreatedAsync();
                SeedAuthority(setup, agentId, instanceId);
                await setup.SaveChangesAsync();
                if (armPause)
                {
                    pause.Arm();
                }
                return new ConcurrencyFixture(anchor, options, pause, agentId, instanceId);
            }
            catch
            {
                await anchor.DisposeAsync();
                throw;
            }
        }

        /// <summary>Starts the exact valid observation used by both race interleavings.</summary>
        /// <returns>The authoritative ingestion result.</returns>
        internal ValueTask<ObservationItemResult> IngestAsync() =>
            IngestAsync(CreateMessage(sequence: 1));

        /// <summary>Starts authoritative ingestion for one exact fixture-owned message.</summary>
        /// <param name="message">Observation whose durable outcome should be decided.</param>
        /// <returns>The authoritative ingestion result.</returns>
        internal ValueTask<ObservationItemResult> IngestAsync(ObservationSyncMessage message) =>
            ingestionStore.IngestAsync(message, Now, CancellationToken.None);

        /// <summary>Creates one valid fixture-owned observation at the requested stream sequence.</summary>
        /// <param name="sequence">Positive Agent stream position.</param>
        /// <returns>A new authenticated healthy observation with unique identities.</returns>
        internal ObservationSyncMessage CreateMessage(long sequence) =>
            Message(AgentId, InstanceId, sequence);

        /// <summary>Starts human-authorised principal revocation for the fixture Agent.</summary>
        /// <returns>The committed or refused revocation outcome.</returns>
        internal ValueTask<AgentRevocationOutcome> RevokeAsync() =>
            agentFleetStore.RevokeAgentAsync(
                SubjectId,
                AgentId,
                PlatformPermissions.AgentsRevoke,
                "security-review",
                Now.AddSeconds(1),
                CancellationToken.None);

        /// <summary>Reads the currently committed Agent lifecycle state from an independent context.</summary>
        /// <returns>The exact stored Agent state.</returns>
        internal async Task<string> ReadAgentStateAsync()
        {
            await using ServerDbContext context = new(options);
            return await context.Agents
                .AsNoTracking()
                .Where(row => row.AgentId == AgentId)
                .Select(row => row.State)
                .SingleAsync();
        }

        /// <summary>Confirms the contender stopped at the shared gate before it could create a persistence context.</summary>
        /// <param name="operation">Competing store operation invoked synchronously up to its first incomplete await.</param>
        internal void AssertCompetingStoreIsFenced(Task operation)
        {
            Assert.False(operation.IsCompleted);
            Assert.Equal(1, contextFactory.CreatedContextCount);
        }

        /// <summary>Releases the one-shot pause and observes all operations before fixture resources can be disposed.</summary>
        /// <param name="operations">Owner and contender tasks that may still be using the shared database.</param>
        /// <returns>A task that completes after every operation has settled or the bounded cleanup wait expires.</returns>
        internal async Task ReleaseAndDrainAsync(params Task[] operations)
        {
            Pause.Release();
            try
            {
                await Task.WhenAll(operations).WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch
            {
                // Direct result awaits retain the normal assertion path. During an earlier assertion failure,
                // observing cleanup failures here prevents abandoned database work from racing fixture disposal.
            }
        }

        /// <summary>
        /// Arranges a failed initial cursor write followed by durable acceptance and revocation before reclassification.
        /// </summary>
        /// <param name="message">Exact message accepted by the simulated competing transaction.</param>
        internal void ArmAcceptedReplayAfterPersistenceFailure(ObservationSyncMessage message)
        {
            Pause.ArmFailure();
            contextFactory.BeforeContextCreated = ordinal =>
            {
                if (ordinal == 2)
                {
                    CommitAcceptedReplayThenRevocation(message);
                }
            };
        }

        /// <summary>Creates a deliberately inconsistent active label with a durable revocation timestamp.</summary>
        /// <returns>A task that completes after the drifted identity row has committed.</returns>
        internal async Task SetRevocationTimestampOnlyAsync()
        {
            await using ServerDbContext context = new(options);
            RegisteredAgentRow agent = await context.Agents.SingleAsync(row => row.AgentId == AgentId);
            agent.RevokedAt = Now.AddSeconds(1);
            agent.ConcurrencyToken = Guid.NewGuid();
            await context.SaveChangesAsync();
        }

        /// <summary>Verifies the ingestion-first order retained one accepted effect set before principal revocation.</summary>
        /// <returns>A task that completes after every durable effect has been checked.</returns>
        internal async Task AssertAcceptedThenRevokedAsync()
        {
            await using ServerDbContext context = new(options);
            Assert.Equal("Revoked", await context.Agents
                .Where(row => row.AgentId == AgentId)
                .Select(row => row.State)
                .SingleAsync());
            Assert.Equal(1, await context.HealthSamples.CountAsync());
            Assert.Equal(1, await context.AgentObservationCursors
                .Where(row => row.AgentId == AgentId)
                .Select(row => row.HighestContiguousSequence)
                .SingleAsync());
            Assert.Equal(1, await context.InstanceObservationStates.CountAsync());
            Assert.Equal(1, await context.Events.CountAsync());
            Assert.Equal(1, await context.OutboxMessages.CountAsync());
            Assert.Equal(0, await context.NotificationDeliveries.CountAsync());
            Assert.Equal(1, await context.AuditEntries.CountAsync(row => row.Action == "agent.revoke"));
            Assert.Equal(0, await context.RejectedObservationSequences.CountAsync());
        }

        /// <summary>Verifies revocation-first rejection consumed only its sequence and created no accepted health effects.</summary>
        /// <returns>A task that completes after every durable effect has been checked.</returns>
        internal async Task AssertRevokedBeforeRejectionAsync()
        {
            await using ServerDbContext context = new(options);
            Assert.Equal("Revoked", await context.Agents
                .Where(row => row.AgentId == AgentId)
                .Select(row => row.State)
                .SingleAsync());
            Assert.Equal(1, await context.AgentObservationCursors
                .Where(row => row.AgentId == AgentId)
                .Select(row => row.HighestContiguousSequence)
                .SingleAsync());
            Assert.Equal(0, await context.HealthSamples.CountAsync());
            Assert.Equal(0, await context.InstanceObservationStates.CountAsync());
            Assert.Equal(0, await context.Events.CountAsync());
            Assert.Equal(0, await context.OutboxMessages.CountAsync());
            Assert.Equal(0, await context.NotificationDeliveries.CountAsync());
            Assert.Equal(1, await context.AuditEntries.CountAsync(row => row.Action == "agent.revoke"));
            RejectedObservationSequenceRow ledger = await context.RejectedObservationSequences
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(1, ledger.Sequence);
            Assert.Equal("agent.not_active", ledger.ErrorCode);
        }

        /// <summary>Verifies a drifted revoked identity consumed only its rejected sequence without accepted effects.</summary>
        /// <returns>A task that completes after every durable effect has been checked.</returns>
        internal async Task AssertDriftedIdentityRejectedAsync()
        {
            await using ServerDbContext context = new(options);
            RegisteredAgentRow agent = await context.Agents
                .AsNoTracking()
                .SingleAsync(row => row.AgentId == AgentId);
            Assert.Equal("Active", agent.State);
            Assert.NotNull(agent.RevokedAt);
            Assert.Equal(1, await context.AgentObservationCursors
                .Where(row => row.AgentId == AgentId)
                .Select(row => row.HighestContiguousSequence)
                .SingleAsync());
            Assert.Equal(0, await context.HealthSamples.CountAsync());
            Assert.Equal(0, await context.InstanceObservationStates.CountAsync());
            Assert.Equal(0, await context.Events.CountAsync());
            Assert.Equal(0, await context.OutboxMessages.CountAsync());
            Assert.Equal(0, await context.NotificationDeliveries.CountAsync());
            Assert.Equal(0, await context.AuditEntries.CountAsync());
            RejectedObservationSequenceRow ledger = await context.RejectedObservationSequences
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(1, ledger.Sequence);
            Assert.Equal("agent.not_active", ledger.ErrorCode);
        }

        /// <summary>Verifies a sequence-two sample is durable but unprojected while sequence one remains unresolved.</summary>
        /// <param name="successor">Accepted sequence-two message buffered behind the gap.</param>
        /// <returns>A task that completes after the pre-revocation evidence has been checked.</returns>
        internal async Task AssertSuccessorBufferedBehindGapAsync(ObservationSyncMessage successor)
        {
            await using ServerDbContext context = new(options);
            HealthSampleRow sample = await context.HealthSamples.AsNoTracking().SingleAsync();
            Assert.Equal(successor.ObservationId, sample.ObservationId);
            Assert.Equal(2, sample.Sequence);
            Assert.Equal(0, await context.AgentObservationCursors
                .Where(row => row.AgentId == AgentId)
                .Select(row => row.HighestContiguousSequence)
                .SingleAsync());
            Assert.Equal(0, await context.InstanceObservationStates.CountAsync());
            Assert.Equal(0, await context.Events.CountAsync());
            Assert.Equal(0, await context.OutboxMessages.CountAsync());
            Assert.Equal(0, await context.NotificationDeliveries.CountAsync());
            Assert.Equal(0, await context.RejectedObservationSequences.CountAsync());
        }

        /// <summary>
        /// Verifies post-revocation gap consumption projects only the successor that was already accepted.
        /// </summary>
        /// <param name="successor">Sequence-two observation accepted before principal revocation.</param>
        /// <param name="rejectedGap">Sequence-one observation refused after principal revocation.</param>
        /// <returns>A task that completes after accepted and rejected provenance have been distinguished.</returns>
        internal async Task AssertRejectedGapReleasedOnlyAcceptedSuccessorAsync(
            ObservationSyncMessage successor,
            ObservationSyncMessage rejectedGap)
        {
            await using ServerDbContext context = new(options);
            Assert.Equal("Revoked", await context.Agents
                .Where(row => row.AgentId == AgentId)
                .Select(row => row.State)
                .SingleAsync());
            Assert.Equal(2, await context.AgentObservationCursors
                .Where(row => row.AgentId == AgentId)
                .Select(row => row.HighestContiguousSequence)
                .SingleAsync());

            HealthSampleRow sample = await context.HealthSamples.AsNoTracking().SingleAsync();
            Assert.Equal(successor.ObservationId, sample.ObservationId);
            Assert.Equal(2, sample.Sequence);
            Assert.NotEqual(rejectedGap.ObservationId, sample.ObservationId);

            InstanceObservationStateRow state = await context.InstanceObservationStates.AsNoTracking().SingleAsync();
            Assert.Equal(successor.ObservationId, state.ObservationId);
            Assert.Equal(2, state.LastProcessedSequence);

            EventRecordRow canonicalEvent = await context.Events.AsNoTracking().SingleAsync();
            Assert.Equal(successor.ObservationId, canonicalEvent.SourceObservationId);
            Assert.NotEqual(rejectedGap.ObservationId, canonicalEvent.SourceObservationId);
            Assert.Equal(1, await context.OutboxMessages.CountAsync());
            Assert.Equal(0, await context.NotificationDeliveries.CountAsync());
            Assert.Equal(1, await context.AuditEntries.CountAsync(row => row.Action == "agent.revoke"));
            RejectedObservationSequenceRow ledger = await context.RejectedObservationSequences
                .AsNoTracking()
                .SingleAsync();
            Assert.Equal(rejectedGap.MessageId, ledger.MessageId);
            Assert.Equal(1, ledger.Sequence);
            Assert.Equal("agent.not_active", ledger.ErrorCode);
        }

        /// <summary>Verifies replay classification preserved the accepted outcome instead of consuming a rejection.</summary>
        /// <param name="accepted">Message durably accepted before the simulated revocation.</param>
        /// <returns>A task that completes after the replay and authority evidence have been checked.</returns>
        internal async Task AssertAcceptedReplayWonInactiveClassificationAsync(ObservationSyncMessage accepted)
        {
            await using ServerDbContext context = new(options);
            RegisteredAgentRow agent = await context.Agents.AsNoTracking().SingleAsync(
                row => row.AgentId == AgentId);
            Assert.Equal("Revoked", agent.State);
            Assert.NotNull(agent.RevokedAt);
            HealthSampleRow sample = await context.HealthSamples.AsNoTracking().SingleAsync();
            Assert.Equal(accepted.MessageId, sample.MessageId);
            Assert.Equal(accepted.ObservationId, sample.ObservationId);
            Assert.Equal(0, await context.AgentObservationCursors
                .Where(row => row.AgentId == AgentId)
                .Select(row => row.HighestContiguousSequence)
                .SingleAsync());
            Assert.Equal(0, await context.RejectedObservationSequences.CountAsync());
        }

        /// <summary>Releases any pending pause and closes the anchor retaining the in-memory database.</summary>
        /// <returns>A task that completes after the anchor connection has closed.</returns>
        public async ValueTask DisposeAsync()
        {
            Pause.Release();
            await anchorConnection.DisposeAsync();
        }

        /// <summary>Seeds one active Agent, one assignment and the minimum global revocation permission.</summary>
        /// <param name="context">Setup context that owns all uncommitted seed rows.</param>
        /// <param name="agentId">Synthetic Agent identifier.</param>
        /// <param name="instanceId">Synthetic assigned-instance identifier.</param>
        private static void SeedAuthority(ServerDbContext context, Guid agentId, Guid instanceId)
        {
            Guid userId = Guid.NewGuid();
            Guid roleId = Guid.NewGuid();
            Guid permissionId = Guid.NewGuid();
            context.Agents.Add(new RegisteredAgentRow
            {
                AgentId = agentId,
                InstallationId = $"installation:{agentId:N}",
                DisplayName = "R-EGRESS Agent",
                Environment = "test",
                Platform = "windows-x64",
                AgentVersion = "1.0.0-test",
                CertificateThumbprint = Convert.ToHexString(agentId.ToByteArray()),
                State = "Active",
                EnrolledAt = Now.AddMinutes(-5),
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Instances.Add(new DatabaseInstanceRow
            {
                InstanceId = instanceId,
                DisplayName = "R-EGRESS instance",
                ProviderType = "postgresql",
                Environment = "test",
                EndpointJson = "{}",
                AssignedAgentId = agentId,
                TagsJson = "[]",
                IntervalSeconds = 60,
                TimeoutSeconds = 5,
                RetryCount = 1,
                Enabled = true,
                CreatedAt = Now.AddMinutes(-5),
                UpdatedAt = Now.AddMinutes(-5),
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Users.Add(new PlatformUserRow
            {
                UserId = userId,
                SubjectId = SubjectId,
                DisplayName = "R-EGRESS operator",
                State = "Active",
                CreatedAt = Now.AddMinutes(-10),
                UpdatedAt = Now.AddMinutes(-10),
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Roles.Add(new RoleRow
            {
                RoleId = roleId,
                Name = "R-EGRESS revocation role",
                Description = "Synthetic local revocation authority.",
                IsSystem = false,
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Permissions.Add(new PermissionRow
            {
                PermissionId = permissionId,
                Code = PlatformPermissions.AgentsRevoke,
                Description = "Revoke one Agent.",
            });
            context.RolePermissions.Add(new RolePermissionRow
            {
                RoleId = roleId,
                PermissionId = permissionId,
            });
            context.RoleAssignments.Add(new RoleAssignmentRow
            {
                RoleAssignmentId = Guid.NewGuid(),
                UserId = userId,
                RoleId = roleId,
                ScopeType = "Global",
                ScopeValue = "*",
                GrantedByUserId = userId,
                GrantedAt = Now.AddMinutes(-5),
                ExpiresAt = Now.AddHours(1),
            });
        }

        /// <summary>Creates the valid provider-neutral observation shared by the race regressions.</summary>
        /// <param name="agentId">Agent that owns the observation stream.</param>
        /// <param name="instanceId">Instance assigned to the Agent.</param>
        /// <param name="sequence">Positive Agent stream position.</param>
        /// <returns>An authenticated healthy observation at the requested sequence.</returns>
        private static ObservationSyncMessage Message(Guid agentId, Guid instanceId, long sequence) =>
            new(
                Guid.NewGuid(),
                1,
                sequence,
                Guid.NewGuid(),
                instanceId,
                agentId,
                "postgresql",
                "fixture",
                "Healthy",
                "fixture",
                "ProviderAuthenticated",
                1,
                Now,
                10,
                null,
                null,
                []);

        /// <summary>
        /// Simulates a competing accepted commit followed by principal revocation between the failed attempt and its
        /// fresh classification context.
        /// </summary>
        /// <param name="message">Exact accepted message whose replay identity must win classification.</param>
        private void CommitAcceptedReplayThenRevocation(ObservationSyncMessage message)
        {
            using ServerDbContext context = new(options);
            context.HealthSamples.Add(new HealthSampleRow
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
                ReceivedAt = Now,
                DurationMilliseconds = message.DurationMilliseconds,
                AttemptCount = message.AttemptCount,
                ErrorCode = message.ErrorCode,
                RedactedDetailsJson = "{}",
                PayloadHash = ProductionPayloadHash(message),
            });
            context.AgentObservationCursors.Add(new AgentObservationCursorRow
            {
                AgentId = AgentId,
                HighestContiguousSequence = 0,
                RejectionLedgerStartSequence = 1,
                UpdatedAt = Now,
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.SaveChanges();

            RegisteredAgentRow agent = context.Agents.Single(row => row.AgentId == AgentId);
            agent.State = "Revoked";
            agent.RevokedAt = Now.AddSeconds(1);
            agent.ConcurrencyToken = Guid.NewGuid();
            context.SaveChanges();
        }

        /// <summary>Invokes the canonical private hash implementation so the simulated replay is byte-for-byte exact.</summary>
        /// <param name="message">Message whose replay-significant fields are hashed.</param>
        /// <returns>The production hexadecimal SHA-256 payload hash.</returns>
        private static string ProductionPayloadHash(ObservationSyncMessage message)
        {
            MethodInfo method = typeof(ServerObservationIngestionStore).GetMethod(
                "ComputePayloadHash",
                BindingFlags.NonPublic | BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(ServerObservationIngestionStore).FullName,
                    "ComputePayloadHash");
            return (string)(method.Invoke(null, [message]) ??
                throw new InvalidOperationException("The production payload hash returned no value."));
        }
    }

    /// <summary>Identifies the exact first-write boundary paused by a concurrency regression.</summary>
    internal enum PauseTarget
    {
        ObservationCursorCreation,
        PrincipalRevocation,
    }

    /// <summary>Pauses exactly one selected transaction after its reads and before its first persistence write.</summary>
    /// <param name="target">Entity transition that identifies the owning transaction.</param>
    internal sealed class PauseBeforeFirstWriteInterceptor(PauseTarget target) : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource reached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int armed;
        private int failInsteadOfPause;

        /// <summary>Arms the interceptor after schema creation and seed persistence have completed.</summary>
        internal void Arm() => Interlocked.Exchange(ref armed, 1);

        /// <summary>Arms one synthetic persistence failure at the selected first-write boundary.</summary>
        internal void ArmFailure()
        {
            Interlocked.Exchange(ref failInsteadOfPause, 1);
            Interlocked.Exchange(ref armed, 1);
        }

        /// <summary>Waits for the selected transaction to reach its pre-write boundary.</summary>
        /// <returns>A task that fails if the expected boundary is not reached promptly.</returns>
        internal Task WaitUntilReachedAsync() => reached.Task.WaitAsync(TimeSpan.FromSeconds(10));

        /// <summary>Releases the paused transaction; repeated calls are harmless.</summary>
        internal void Release() => released.TrySetResult();

        /// <inheritdoc />
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref armed, 0, 1) == 1 &&
                MatchesTarget(eventData.Context))
            {
                if (Interlocked.Exchange(ref failInsteadOfPause, 0) == 1)
                {
                    throw new DbUpdateException("Synthetic R-EGRESS persistence failure.");
                }

                reached.TrySetResult();
                await released.Task.WaitAsync(cancellationToken);
            }

            return result;
        }

        /// <summary>Matches only the first write unique to the selected transaction.</summary>
        /// <param name="context">Context whose pending tracked changes are being saved.</param>
        /// <returns><see langword="true"/> when the selected observation or revocation boundary was reached.</returns>
        private bool MatchesTarget(DbContext? context) =>
            target switch
            {
                PauseTarget.ObservationCursorCreation => context?.ChangeTracker
                    .Entries<AgentObservationCursorRow>()
                    .Any(entry => entry.State == EntityState.Added) == true,
                PauseTarget.PrincipalRevocation => context?.ChangeTracker
                    .Entries<RegisteredAgentRow>()
                    .Any(entry => entry.State == EntityState.Modified) == true,
                _ => false,
            };
    }

    /// <summary>Creates independent Server contexts over the fixture's shared in-memory database.</summary>
    /// <param name="options">Provider and interceptor options for every created context.</param>
    private sealed class ServerContextFactory(DbContextOptions<ServerDbContext> options)
        : IDbContextFactory<ServerDbContext>
    {
        private int createdContextCount;

        /// <summary>Gets or sets a test-only callback invoked before each numbered store context is returned.</summary>
        internal Action<int>? BeforeContextCreated { get; set; }

        /// <summary>Gets the number of contexts requested by the two stores participating in the interleaving.</summary>
        internal int CreatedContextCount => Volatile.Read(ref createdContextCount);

        /// <inheritdoc />
        public ServerDbContext CreateDbContext()
        {
            int ordinal = Interlocked.Increment(ref createdContextCount);
            BeforeContextCreated?.Invoke(ordinal);
            return new(options);
        }
    }
}
