// Module purpose: Implements the disposable PostgreSQL process orchestration and durable checks for the R-EGRESS/R-FENCE laboratory.
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DBNotifier.Application.Access;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace DBNotifier.IntegrationTests;

public sealed partial class REgressPostgreSqlMultiprocessFenceTests
{
    /// <summary>Owns the disposable PostgreSQL orchestration and all exact child-process cleanup.</summary>
    private sealed partial class PostgreSqlFenceLab
    {
        private const string ApplicationPrefix = "dbn-ref-";
        private const string DotNetHostVariable = "DBNOTIFIER_R_EGRESS_POSTGRESQL_DOTNET_HOST";
        private static readonly TimeSpan ChildTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan CoordinationTimeout = TimeSpan.FromSeconds(10);
        private static readonly string[] PersistenceRetryableErrorCodes =
            ["ingestion.persistence_failed", "ingestion.persistence_unavailable"];
        private static readonly string[] TerminalReplayDispositions = ["Accepted", "Duplicate"];
        private readonly string baseConnectionString;
        private readonly DbContextOptions<ServerDbContext> contextOptions;
        private readonly string dotnetPath;
        private readonly string hostPath;
        private readonly DateTimeOffset now;
        private readonly string runToken;
        private readonly List<SandboxChild> children = [];
        private readonly SemaphoreSlim laboratoryDisposalGate = new(1, 1);
        private int sessionOrdinal;
        private bool laboratoryDisposed;

        /// <summary>Initialises one laboratory after its exact schema and global synthetic authority exist.</summary>
        /// <param name="baseConnectionString">Validated loopback-only disposable PostgreSQL connection.</param>
        /// <param name="contextOptions">Parent-process EF options with pooling disabled.</param>
        /// <param name="hostPath">Built test-only child host assembly.</param>
        /// <param name="now">Fixed UTC fixture instant.</param>
        /// <param name="runToken">Short non-secret session-label token.</param>
        private PostgreSqlFenceLab(
            string baseConnectionString,
            DbContextOptions<ServerDbContext> contextOptions,
            string dotnetPath,
            string hostPath,
            DateTimeOffset now,
            string runToken)
        {
            this.baseConnectionString = baseConnectionString;
            this.contextOptions = contextOptions;
            this.dotnetPath = dotnetPath;
            this.hostPath = hostPath;
            this.now = now;
            this.runToken = runToken;
        }

        /// <summary>Validates the local connection, applies migrations only to the disposable volume and seeds RBAC.</summary>
        /// <param name="connectionString">Runner-provided ephemeral connection string.</param>
        /// <param name="now">Fixed UTC fixture instant.</param>
        /// <param name="runToken">Runner-owned non-secret token for exact process and session identity.</param>
        /// <returns>An initialised laboratory.</returns>
        internal static async Task<PostgreSqlFenceLab> CreateAsync(
            string connectionString,
            DateTimeOffset now,
            string runToken)
        {
            string parentConnection = BuildConnectionString(
                connectionString,
                $"{ApplicationPrefix}{runToken}-parent");
            DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
                .UseNpgsql(parentConnection)
                .EnableSensitiveDataLogging(false)
                .Options;
            string dotnetPath = ResolveDotNetPath();
            string hostPath = ResolveHostPath();
            PostgreSqlFenceLab lab = new(
                parentConnection,
                options,
                dotnetPath,
                hostPath,
                now,
                runToken);
            await using ServerDbContext migration = new(options);
            await migration.Database.MigrateAsync().ConfigureAwait(false);
            await lab.SeedGlobalAuthorityAsync().ConfigureAwait(false);
            return lab;
        }

        /// <summary>Runs the ingestion-first order and verifies the accepted observation precedes durable revocation.</summary>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <returns>A task completing after process and database verification.</returns>
        internal async Task ProveIngestionFirstAsync(LabEvidence evidence)
        {
            AgentFixture fixture = await SeedAgentAsync("mp01").ConfigureAwait(false);
            await using SandboxChild ingestion = await StartChildAsync(
                "mp01-ingest",
                "ingest",
                "identity-lock",
                fixture,
                sequenceStart: 1,
                count: 1).ConfigureAwait(false);
            await ingestion.StartOperationAsync().ConfigureAwait(false);
            SandboxSignal ingestionBarrier = await ingestion
                .ReadSignalAsync("barrier")
                .ConfigureAwait(false);
            Assert.Equal("identity-lock", ingestionBarrier.Boundary);

            await using SandboxChild revocation = await StartChildAsync(
                "mp01-revoke",
                "revoke",
                "none",
                fixture,
                sequenceStart: 1,
                count: 1).ConfigureAwait(false);
            try
            {
                await revocation.StartOperationAsync().ConfigureAwait(false);
                BlockingPair pair = await WaitForBlockingAsync(
                    revocation.ApplicationName,
                    ingestion.ApplicationName).ConfigureAwait(false);
                Assert.Equal(ingestionBarrier.BackendPid, pair.BlockerBackendPid);
                await AssertNoCommittedEffectsAsync(fixture).ConfigureAwait(false);
                evidence.BlockingObservationCount++;
            }
            finally
            {
                await ingestion.TrySendCommandAsync("release").ConfigureAwait(false);
            }

            SandboxOperationResult[] results = await Task.WhenAll(
                ingestion.WaitForResultAsync(),
                revocation.WaitForResultAsync()).ConfigureAwait(false);
            AssertDistinctProcesses(results);
            SandboxItemResult accepted = Assert.Single(results[0].Items);
            Assert.Equal("Accepted", accepted.Disposition);
            Assert.Equal("Revoked", Assert.Single(results[1].Items).Disposition);
            AddIngestionEvidence(evidence, results[0]);
            AddSerializationEvidence(evidence, results[1]);
            await AssertIngestionFirstMatrixAsync(fixture).ConfigureAwait(false);
            evidence.ScenarioCount++;
        }

        /// <summary>Runs the revocation-first blocked order and verifies that the observation has no accepted effects.</summary>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <returns>A task completing after process and database verification.</returns>
        internal async Task ProveRevocationFirstAsync(LabEvidence evidence)
        {
            (SandboxOperationResult ingestion, SandboxOperationResult revocation, AgentFixture fixture) =
                await ExecuteRevocationFirstAsync("mp02", evidence).ConfigureAwait(false);
            SandboxItemResult rejected = Assert.Single(ingestion.Items);
            Assert.Equal("Rejected", rejected.Disposition);
            Assert.Equal("agent.not_active", rejected.ErrorCode);
            Assert.Equal("Revoked", Assert.Single(revocation.Items).Disposition);
            await AssertRevocationFirstMatrixAsync(fixture).ConfigureAwait(false);
            evidence.ScenarioCount++;
        }

        /// <summary>Repeats the blocked revocation-first order and requires real SQLSTATE 40001 reclassification.</summary>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <returns>A task completing after PostgreSQL serialization evidence is observed.</returns>
        internal async Task ProveSerializationReclassificationAsync(LabEvidence evidence)
        {
            (SandboxOperationResult ingestion, SandboxOperationResult revocation, AgentFixture fixture) =
                await ExecuteRevocationFirstAsync("mp03", evidence).ConfigureAwait(false);
            Assert.True(
                ingestion.SerializationFailureCount > 0,
                "The blocked serializable PostgreSQL ingestion did not expose SQLSTATE 40001.");
            Assert.Equal("Rejected", Assert.Single(ingestion.Items).Disposition);
            Assert.Equal("Revoked", Assert.Single(revocation.Items).Disposition);
            await AssertRevocationFirstMatrixAsync(fixture).ConfigureAwait(false);
            evidence.ScenarioCount++;
        }

        /// <summary>Proves both crash lock release and cancellable waiter cleanup as separate scenarios.</summary>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <returns>A task completing after both recovery paths and durable checks.</returns>
        internal async Task ProveCrashAndCancellationAsync(LabEvidence evidence)
        {
            await ProveCrashReleaseAsync(evidence).ConfigureAwait(false);
            evidence.ScenarioCount++;
            await ProveCancellationReleaseAsync(evidence).ConfigureAwait(false);
            evidence.ScenarioCount++;
        }

        /// <summary>Proves a held identity row for one Agent cannot delay another Agent.</summary>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <returns>A task completing after independent progress is durable.</returns>
        internal async Task ProveDistinctAgentProgressAsync(LabEvidence evidence)
        {
            AgentFixture heldFixture = await SeedAgentAsync("mp06-held").ConfigureAwait(false);
            AgentFixture independentFixture = await SeedAgentAsync("mp06-free").ConfigureAwait(false);
            await using SandboxChild holder = await StartChildAsync(
                "mp06-held",
                "ingest",
                "identity-lock",
                heldFixture,
                1,
                1).ConfigureAwait(false);
            await holder.StartOperationAsync().ConfigureAwait(false);
            await holder.ReadSignalAsync("barrier").ConfigureAwait(false);

            await using SandboxChild independent = await StartChildAsync(
                "mp06-free",
                "ingest",
                "none",
                independentFixture,
                1,
                1).ConfigureAwait(false);
            SandboxOperationResult independentResult;
            try
            {
                await independent.StartOperationAsync().ConfigureAwait(false);
                independentResult = await independent.WaitForResultAsync().ConfigureAwait(false);
                SandboxItemResult independentItem = Assert.Single(independentResult.Items);
                Assert.True(
                    string.Equals(independentItem.Disposition, "Accepted", StringComparison.Ordinal),
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Independent Agent result was {independentItem.Disposition}:{independentItem.ErrorCode ?? "none"}; SQLSTATE 40001 count was {independentResult.SerializationFailureCount}."));
            }
            finally
            {
                await holder.TrySendCommandAsync("release").ConfigureAwait(false);
            }

            SandboxOperationResult holderResult = await holder.WaitForResultAsync().ConfigureAwait(false);
            Assert.NotEqual(holderResult.ProcessId, independentResult.ProcessId);
            SandboxItemResult holderItem = Assert.Single(holderResult.Items);
            if (string.Equals(holderItem.Disposition, "Retryable", StringComparison.Ordinal))
            {
                Assert.Contains(
                    holderItem.ErrorCode,
                    PersistenceRetryableErrorCodes);
                Assert.True(holderItem.SerializationFailureCount > 0);
                AddExpectedRetryableEvidence(evidence, holderResult);

                await using SandboxChild retry = await StartChildAsync(
                    "mp06-held-convergence",
                    "ingest",
                    "none",
                    heldFixture,
                    1,
                    1).ConfigureAwait(false);
                await retry.StartOperationAsync().ConfigureAwait(false);
                SandboxOperationResult retryResult = await retry.WaitForResultAsync().ConfigureAwait(false);
                Assert.Equal("Accepted", Assert.Single(retryResult.Items).Disposition);
                evidence.ConvergenceOperationCount++;
                AddIngestionEvidence(evidence, retryResult);
            }
            else
            {
                Assert.Equal("Accepted", holderItem.Disposition);
                AddIngestionEvidence(evidence, holderResult);
            }

            AddIngestionEvidence(evidence, independentResult);
            await AssertActiveAcceptedMatrixAsync(heldFixture, 1, 1).ConfigureAwait(false);
            await AssertActiveAcceptedMatrixAsync(independentFixture, 1, 1).ConfigureAwait(false);
            evidence.ScenarioCount++;
        }

        /// <summary>Characterises four processes replaying the same twenty-five messages for one Agent.</summary>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <returns>Same-Agent latency, blocking and throughput evidence.</returns>
        internal async Task<LoadProfileSummary> CharacteriseSameAgentLoadAsync(LabEvidence evidence)
        {
            AgentFixture fixture = await SeedAgentAsync("mp07").ConfigureAwait(false);
            DatabaseCounters before = await ReadDatabaseCountersAsync().ConfigureAwait(false);
            List<SandboxChild> workers = [];
            Guid sharedMessage = Guid.NewGuid();
            Guid sharedObservation = Guid.NewGuid();
            try
            {
                for (int index = 0; index < 4; index++)
                {
                    workers.Add(await StartChildAsync(
                        $"mp07-load-{index}",
                        "load",
                        index == 0 ? "identity-lock" : "none",
                        fixture with
                        {
                            MessageId = sharedMessage,
                            ObservationId = sharedObservation,
                        },
                        1,
                        25).ConfigureAwait(false));
                }

                Stopwatch wall = Stopwatch.StartNew();
                await workers[0].StartOperationAsync().ConfigureAwait(false);
                await workers[0].ReadSignalAsync("barrier").ConfigureAwait(false);
                for (int index = 1; index < workers.Count; index++)
                {
                    await workers[index].StartOperationAsync().ConfigureAwait(false);
                }

                int maximumBlocked = await WaitForBlockedCountAsync(
                    $"{ApplicationPrefix}{runToken}-mp07-load-",
                    3).ConfigureAwait(false);
                await AssertNoCommittedEffectsAsync(fixture).ConfigureAwait(false);
                evidence.BlockingObservationCount++;
                await workers[0].TrySendCommandAsync("release").ConfigureAwait(false);
                SandboxOperationResult[] results = await Task.WhenAll(
                    workers.Select(worker => worker.WaitForResultAsync())).ConfigureAwait(false);
                wall.Stop();

                Assert.Equal(4, results.Select(result => result.ProcessId).Distinct().Count());
                SandboxItemResult[] items = results.SelectMany(result => result.Items).ToArray();
                Assert.Equal(100, items.Length);
                Assert.Equal(25, items.Count(item => item.Disposition == "Accepted"));
                Assert.Equal(75, items.Count(item => item.Disposition == "Duplicate"));
                Assert.DoesNotContain(items, item =>
                    item.Disposition is not ("Accepted" or "Duplicate"));
                foreach (SandboxOperationResult result in results)
                {
                    AddIngestionEvidence(evidence, result, duplicateIsExpected: true);
                }

                await AssertActiveAcceptedMatrixAsync(fixture, 25, 25).ConfigureAwait(false);
                DatabaseCounters after = await ReadDatabaseCountersAsync().ConfigureAwait(false);
                LoadProfileSummary profile = CreateLoadSummary(
                    results,
                    maximumBlocked,
                    checked(after.Deadlocks - before.Deadlocks),
                    wall.Elapsed);
                Assert.Equal(0, profile.DeadlockCount);
                evidence.ScenarioCount++;
                return profile;
            }
            finally
            {
                await DisposeEveryChildAsync(workers).ConfigureAwait(false);
            }
        }

        /// <summary>Characterises four independent Agents while one target process races a principal revocation.</summary>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <returns>Distinct-Agent latency, blocking and throughput evidence.</returns>
        internal async Task<LoadProfileSummary> CharacteriseDistinctAgentLoadAsync(LabEvidence evidence)
        {
            AgentFixture[] fixtures = await Task.WhenAll(
                Enumerable.Range(0, 4).Select(index => SeedAgentAsync($"mp08-{index}"))).ConfigureAwait(false);
            DatabaseCounters before = await ReadDatabaseCountersAsync().ConfigureAwait(false);
            List<SandboxChild> workers = [];
            SandboxChild? revocation = null;
            try
            {
                for (int index = 0; index < fixtures.Length; index++)
                {
                    workers.Add(await StartChildAsync(
                        $"mp08-load-{index}",
                        "load",
                        index == 0 ? "identity-lock" : "none",
                        fixtures[index],
                        1,
                        25).ConfigureAwait(false));
                }

                revocation = await StartChildAsync(
                    "mp08-revoke",
                    "revoke",
                    "none",
                    fixtures[0],
                    1,
                    1).ConfigureAwait(false);
                Stopwatch wall = Stopwatch.StartNew();
                await workers[0].StartOperationAsync().ConfigureAwait(false);
                await workers[0].ReadSignalAsync("barrier").ConfigureAwait(false);
                for (int index = 1; index < workers.Count; index++)
                {
                    await workers[index].StartOperationAsync().ConfigureAwait(false);
                }
                await revocation.StartOperationAsync().ConfigureAwait(false);

                BlockingPair pair = await WaitForBlockingAsync(
                    revocation.ApplicationName,
                    workers[0].ApplicationName).ConfigureAwait(false);
                Assert.True(pair.BlockedBackendPid > 0);
                int maximumBlocked = await WaitForBlockedCountAsync(
                    $"{ApplicationPrefix}{runToken}-mp08-",
                    1).ConfigureAwait(false);
                await AssertNoCommittedEffectsAsync(fixtures[0]).ConfigureAwait(false);
                evidence.BlockingObservationCount++;
                await workers[0].TrySendCommandAsync("release").ConfigureAwait(false);

                SandboxOperationResult[] loadResults = await Task.WhenAll(
                    workers.Select(worker => worker.WaitForResultAsync())).ConfigureAwait(false);
                SandboxOperationResult revocationResult = await revocation
                    .WaitForResultAsync()
                    .ConfigureAwait(false);
                wall.Stop();
                Assert.Equal("Revoked", Assert.Single(revocationResult.Items).Disposition);
                SandboxItemResult[] items = loadResults.SelectMany(result => result.Items).ToArray();
                Assert.Equal(100, items.Length);
                int targetAccepted = ValidateRevokedTargetLoadResult(loadResults[0]);
                for (int index = 1; index < loadResults.Length; index++)
                {
                    ValidateActiveLoadResult(loadResults[index]);
                }

                for (int index = 0; index < loadResults.Length; index++)
                {
                    AddIngestionEvidence(
                        evidence,
                        loadResults[index],
                        retryableIsExpected: true);
                    await ConvergeRetryableItemsAsync(
                        $"mp08-convergence-{index}",
                        fixtures[index],
                        loadResults[index],
                        revoked: index == 0,
                        evidence).ConfigureAwait(false);
                }
                AddSerializationEvidence(evidence, revocationResult);

                int targetRejected = 25 - targetAccepted;
                evidence.RevocationCompletionPosition = targetAccepted;
                await AssertRevokedLoadMatrixAsync(
                    fixtures[0],
                    targetAccepted,
                    targetRejected).ConfigureAwait(false);
                for (int index = 1; index < fixtures.Length; index++)
                {
                    await AssertActiveAcceptedMatrixAsync(fixtures[index], 25, 25).ConfigureAwait(false);
                }

                DatabaseCounters after = await ReadDatabaseCountersAsync().ConfigureAwait(false);
                LoadProfileSummary profile = CreateLoadSummary(
                    loadResults,
                    maximumBlocked,
                    checked(after.Deadlocks - before.Deadlocks),
                    wall.Elapsed);
                Assert.Equal(0, profile.DeadlockCount);
                evidence.ScenarioCount++;
                return profile;
            }
            finally
            {
                IEnumerable<SandboxChild> owned = revocation is null
                    ? workers
                    : workers.Prepend(revocation);
                await DisposeEveryChildAsync(owned).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Validates the measured revoked-target profile as an accepted prefix followed only by inactive rejection or
        /// a bounded SSI failure and its consequent rejection gap.
        /// </summary>
        /// <param name="result">Target worker's original twenty-five-operation result.</param>
        /// <returns>The number of accepted prefix positions committed before revocation.</returns>
        private static int ValidateRevokedTargetLoadResult(SandboxOperationResult result)
        {
            Assert.Equal(
                Enumerable.Range(1, 25).Select(value => (long)value).ToArray(),
                result.Items.Select(item => item.Sequence).ToArray());
            int acceptedPrefix = 0;
            bool nonAcceptedObserved = false;
            bool persistenceGapObserved = false;
            foreach (SandboxItemResult item in result.Items)
            {
                Assert.True(
                    item.Disposition is "Accepted" or "Rejected" or "Retryable",
                    $"Unexpected revoked-target disposition: {item.Disposition}.");
                if (item.Disposition == "Accepted")
                {
                    Assert.False(nonAcceptedObserved);
                    acceptedPrefix++;
                    continue;
                }

                nonAcceptedObserved = true;
                if (item.Disposition == "Rejected")
                {
                    Assert.Equal("agent.not_active", item.ErrorCode);
                    continue;
                }

                if (IsPersistenceRetryable(item))
                {
                    persistenceGapObserved = true;
                    continue;
                }

                Assert.True(persistenceGapObserved);
                Assert.Equal("ingestion.rejection_gap", item.ErrorCode);
            }

            if (result.Items.Any(IsPersistenceRetryable))
            {
                Assert.All(
                    result.Items.Where(IsPersistenceRetryable),
                    item => Assert.True(item.SerializationFailureCount > 0));
            }

            Assert.True(nonAcceptedObserved);
            Assert.InRange(acceptedPrefix, 0, 24);
            return acceptedPrefix;
        }

        /// <summary>Validates one measured active-Agent profile without accepting rejection or unknown retry causes.</summary>
        /// <param name="result">Active worker's original twenty-five-operation result.</param>
        private static void ValidateActiveLoadResult(SandboxOperationResult result)
        {
            Assert.Equal(
                Enumerable.Range(1, 25).Select(value => (long)value).ToArray(),
                result.Items.Select(item => item.Sequence).ToArray());
            Assert.All(
                result.Items,
                item => Assert.True(
                    item.Disposition == "Accepted" || IsPersistenceRetryable(item),
                    $"Unexpected active-Agent result: {item.Disposition}:{item.ErrorCode ?? "none"}."));
            Assert.Contains(result.Items, item => item.Disposition == "Accepted");
            if (result.Items.Any(IsPersistenceRetryable))
            {
                Assert.All(
                    result.Items.Where(IsPersistenceRetryable),
                    item => Assert.True(item.SerializationFailureCount > 0));
            }
        }

        /// <summary>
        /// Replays only measured retryable positions with their exact identifiers and timestamps, serially and for at
        /// most three attempts, so convergence cannot change the original load profile.
        /// </summary>
        /// <param name="labelPrefix">Stable process-label prefix for the exact fixture.</param>
        /// <param name="fixture">Agent and original message-identity seeds.</param>
        /// <param name="initialResult">Measured worker result containing possible retryable positions.</param>
        /// <param name="revoked">Whether convergence must terminate in inactive-Agent rejection.</param>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <returns>A task completing only after every retryable position reaches a terminal durable outcome.</returns>
        private async Task ConvergeRetryableItemsAsync(
            string labelPrefix,
            AgentFixture fixture,
            SandboxOperationResult initialResult,
            bool revoked,
            LabEvidence evidence)
        {
            foreach (SandboxItemResult original in initialResult.Items
                         .Where(item => item.Disposition == "Retryable")
                         .OrderBy(item => item.Sequence))
            {
                int offset = checked((int)(original.Sequence - 1));
                Assert.InRange(offset, 0, 24);
                AgentFixture exactFixture = fixture with
                {
                    MessageId = DeriveGuid(fixture.MessageId, offset),
                    ObservationId = DeriveGuid(fixture.ObservationId, offset),
                };
                bool converged = false;
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    await using SandboxChild retry = await StartChildAsync(
                        $"{labelPrefix}-{original.Sequence}-{attempt}",
                        "ingest",
                        "none",
                        exactFixture,
                        original.Sequence,
                        1,
                        now.AddMilliseconds(offset)).ConfigureAwait(false);
                    await retry.StartOperationAsync().ConfigureAwait(false);
                    SandboxOperationResult retryResult = await retry.WaitForResultAsync().ConfigureAwait(false);
                    SandboxItemResult retryItem = Assert.Single(retryResult.Items);
                    evidence.ConvergenceOperationCount++;
                    if (retryItem.Disposition == "Retryable")
                    {
                        AddExpectedRetryableEvidence(evidence, retryResult);
                        continue;
                    }

                    if (revoked)
                    {
                        Assert.Equal("Rejected", retryItem.Disposition);
                        Assert.Equal("agent.not_active", retryItem.ErrorCode);
                        AddIngestionEvidence(evidence, retryResult);
                    }
                    else
                    {
                        Assert.Contains(retryItem.Disposition, TerminalReplayDispositions);
                        AddIngestionEvidence(evidence, retryResult, duplicateIsExpected: true);
                    }

                    converged = true;
                    break;
                }

                Assert.True(
                    converged,
                    $"Retryable sequence {original.Sequence} did not converge within the bounded attempts.");
            }
        }

        /// <summary>Identifies the narrow persistence retry outcomes that require process-level SQLSTATE evidence.</summary>
        /// <param name="item">Measured child item.</param>
        /// <returns><see langword="true"/> only for an allowlisted persistence retry outcome.</returns>
        private static bool IsPersistenceRetryable(SandboxItemResult item) =>
            item.Disposition == "Retryable" &&
            item.SerializationFailureCount > 0 &&
            item.ErrorCode is { } errorCode &&
            PersistenceRetryableErrorCodes.Contains(errorCode, StringComparer.Ordinal);

        /// <summary>Derives the exact bounded message identifier used by the child host for a load offset.</summary>
        /// <param name="seed">Original non-empty fixture seed.</param>
        /// <param name="offset">Zero-based load offset from zero through twenty-four.</param>
        /// <returns>The exact identifier used in the original measured operation.</returns>
        private static Guid DeriveGuid(Guid seed, int offset)
        {
            Assert.InRange(offset, 0, 24);
            if (offset == 0)
            {
                return seed;
            }

            byte[] bytes = seed.ToByteArray();
            uint suffix = BitConverter.ToUInt32(bytes, 12);
            BitConverter.GetBytes(unchecked(suffix + (uint)offset)).CopyTo(bytes, 12);
            return new Guid(bytes);
        }

        /// <summary>Reads dedicated database transaction/deadlock counters.</summary>
        /// <returns>Current counters for the disposable database.</returns>
        internal async Task<DatabaseCounters> ReadDatabaseCountersAsync()
        {
            await using NpgsqlConnection connection = await OpenControlConnectionAsync("stats").ConfigureAwait(false);
            await using NpgsqlCommand command = new(
                """
                SELECT xact_commit, xact_rollback, deadlocks
                FROM pg_stat_database
                WHERE datname = current_database()
                """,
                connection);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            Assert.True(await reader.ReadAsync().ConfigureAwait(false));
            return new DatabaseCounters(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2));
        }

        /// <summary>Counts child-labelled PostgreSQL sessions after every child is expected to have exited.</summary>
        /// <returns>Number of residual child sessions.</returns>
        internal async Task<int> CountResidualSessionsAsync()
        {
            await DisposeEveryChildAsync(children.ToArray()).ConfigureAwait(false);

            await using NpgsqlConnection connection = await OpenControlConnectionAsync("residue").ConfigureAwait(false);
            await using NpgsqlCommand command = new(
                """
                SELECT count(*)::integer
                FROM pg_stat_activity
                WHERE application_name LIKE @prefix
                  AND application_name <> current_setting('application_name')
                """,
                connection);
            command.Parameters.AddWithValue("prefix", NpgsqlDbType.Text, $"{ApplicationPrefix}{runToken}-%");
            return Convert.ToInt32(
                await command.ExecuteScalarAsync().ConfigureAwait(false),
                CultureInfo.InvariantCulture);
        }

        /// <summary>Disposes every child idempotently without touching Docker or unrelated processes.</summary>
        /// <returns>A task completing after exact child cleanup.</returns>
        public async ValueTask DisposeAsync()
        {
            await laboratoryDisposalGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (laboratoryDisposed)
                {
                    return;
                }

                await DisposeEveryChildAsync(children.ToArray()).ConfigureAwait(false);
                laboratoryDisposed = true;
            }
            finally
            {
                laboratoryDisposalGate.Release();
            }
        }

        /// <summary>
        /// Attempts exact cleanup for every supplied child before reporting a bounded aggregate failure, allowing the
        /// laboratory owner to retry any child whose disposal did not prove operating-system exit.
        /// </summary>
        /// <param name="ownedChildren">Exact child objects already admitted by this laboratory instance.</param>
        /// <returns>A task completing only after every cleanup attempt has run.</returns>
        private static async Task DisposeEveryChildAsync(IEnumerable<SandboxChild> ownedChildren)
        {
            int failureCount = 0;
            foreach (SandboxChild child in ownedChildren)
            {
                try
                {
                    await child.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception exception) when (
                    exception is not StackOverflowException and not OutOfMemoryException)
                {
                    failureCount++;
                }
            }

            if (failureCount > 0)
            {
                throw new InvalidOperationException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"r_egress_fence.children_cleanup_failed:{failureCount}"));
            }
        }

        /// <summary>Runs one revocation-first order with a real blocked PostgreSQL backend.</summary>
        /// <param name="scenario">Stable scenario label used only for local process/session identities.</param>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <returns>The two typed process results and their seeded Agent fixture.</returns>
        private async Task<(
            SandboxOperationResult Ingestion,
            SandboxOperationResult Revocation,
            AgentFixture Fixture)> ExecuteRevocationFirstAsync(
                string scenario,
                LabEvidence evidence)
        {
            AgentFixture fixture = await SeedAgentAsync(scenario).ConfigureAwait(false);
            await using SandboxChild revocation = await StartChildAsync(
                $"{scenario}-revoke",
                "revoke",
                "agent-update",
                fixture,
                1,
                1).ConfigureAwait(false);
            await revocation.StartOperationAsync().ConfigureAwait(false);
            SandboxSignal revocationBarrier = await revocation
                .ReadSignalAsync("barrier")
                .ConfigureAwait(false);
            Assert.Equal("agent-update", revocationBarrier.Boundary);

            await using SandboxChild ingestion = await StartChildAsync(
                $"{scenario}-ingest",
                "ingest",
                "none",
                fixture,
                1,
                1).ConfigureAwait(false);
            try
            {
                await ingestion.StartOperationAsync().ConfigureAwait(false);
                BlockingPair pair = await WaitForBlockingAsync(
                    ingestion.ApplicationName,
                    revocation.ApplicationName).ConfigureAwait(false);
                Assert.Equal(revocationBarrier.BackendPid, pair.BlockerBackendPid);
                await AssertNoCommittedEffectsAsync(fixture).ConfigureAwait(false);
                evidence.BlockingObservationCount++;
            }
            finally
            {
                await revocation.TrySendCommandAsync("release").ConfigureAwait(false);
            }

            SandboxOperationResult[] results = await Task.WhenAll(
                ingestion.WaitForResultAsync(),
                revocation.WaitForResultAsync()).ConfigureAwait(false);
            AssertDistinctProcesses(results);
            AddIngestionEvidence(evidence, results[0]);
            AddSerializationEvidence(evidence, results[1]);
            return (results[0], results[1], fixture);
        }

        /// <summary>Proves an exact killed lock owner releases its PostgreSQL transaction for a replacement process.</summary>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <returns>A task completing after replacement acceptance and durable identity checks.</returns>
        private async Task ProveCrashReleaseAsync(LabEvidence evidence)
        {
            AgentFixture fixture = await SeedAgentAsync("mp04").ConfigureAwait(false);
            AgentFixture replacementFixture = fixture with
            {
                MessageId = Guid.NewGuid(),
                ObservationId = Guid.NewGuid(),
            };
            await using SandboxChild holder = await StartChildAsync(
                "mp04-holder",
                "ingest",
                "identity-lock",
                fixture,
                1,
                1).ConfigureAwait(false);
            await holder.StartOperationAsync().ConfigureAwait(false);
            SandboxSignal barrier = await holder.ReadSignalAsync("barrier").ConfigureAwait(false);
            Assert.Equal("identity-lock", barrier.Boundary);

            await using SandboxChild replacement = await StartChildAsync(
                "mp04-replacement",
                "ingest",
                "none",
                replacementFixture,
                1,
                1).ConfigureAwait(false);
            await replacement.StartOperationAsync().ConfigureAwait(false);
            BlockingPair pair = await WaitForBlockingAsync(
                replacement.ApplicationName,
                holder.ApplicationName).ConfigureAwait(false);
            Assert.Equal(barrier.BackendPid, pair.BlockerBackendPid);
            await AssertNoCommittedEffectsAsync(fixture).ConfigureAwait(false);
            evidence.BlockingObservationCount++;
            await holder.KillAsync().ConfigureAwait(false);

            SandboxOperationResult result = await replacement.WaitForResultAsync().ConfigureAwait(false);
            Assert.Equal("Accepted", Assert.Single(result.Items).Disposition);
            Assert.NotEqual(holder.ProcessId, result.ProcessId);
            AddIngestionEvidence(evidence, result);
            await AssertActiveAcceptedMatrixAsync(fixture, 1, 1).ConfigureAwait(false);
            await AssertAcceptedMessageIdentitiesAsync(
                fixture,
                [replacementFixture.MessageId],
                [fixture.MessageId]).ConfigureAwait(false);
        }

        /// <summary>Proves cancellation removes a blocked waiter while its lock owner and a successor remain usable.</summary>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <returns>A task completing after cancellation, release, successor progress and durable checks.</returns>
        private async Task ProveCancellationReleaseAsync(LabEvidence evidence)
        {
            AgentFixture fixture = await SeedAgentAsync("mp05").ConfigureAwait(false);
            AgentFixture cancelledFixture = fixture with
            {
                MessageId = Guid.NewGuid(),
                ObservationId = Guid.NewGuid(),
            };
            AgentFixture successorFixture = fixture with
            {
                MessageId = Guid.NewGuid(),
                ObservationId = Guid.NewGuid(),
            };
            await using SandboxChild holder = await StartChildAsync(
                "mp05-holder",
                "ingest",
                "identity-lock",
                fixture,
                1,
                1).ConfigureAwait(false);
            await holder.StartOperationAsync().ConfigureAwait(false);
            SandboxSignal barrier = await holder.ReadSignalAsync("barrier").ConfigureAwait(false);

            await using SandboxChild cancelled = await StartChildAsync(
                "mp05-cancelled",
                "ingest-cancellable",
                "none",
                cancelledFixture,
                2,
                1).ConfigureAwait(false);
            await cancelled.StartOperationAsync().ConfigureAwait(false);
            await cancelled.ReadSignalAsync("started").ConfigureAwait(false);
            BlockingPair pair = await WaitForBlockingAsync(
                cancelled.ApplicationName,
                holder.ApplicationName).ConfigureAwait(false);
            Assert.Equal(barrier.BackendPid, pair.BlockerBackendPid);
            await AssertNoCommittedEffectsAsync(fixture).ConfigureAwait(false);
            evidence.BlockingObservationCount++;
            await cancelled.TrySendCommandAsync("cancel").ConfigureAwait(false);
            SandboxOperationResult cancelledResult = await cancelled
                .WaitForResultAsync()
                .ConfigureAwait(false);
            Assert.True(cancelledResult.Cancelled);
            Assert.Empty(cancelledResult.Items);
            AddSerializationEvidence(evidence, cancelledResult);

            await holder.TrySendCommandAsync("release").ConfigureAwait(false);
            SandboxOperationResult holderResult = await holder.WaitForResultAsync().ConfigureAwait(false);
            Assert.Equal("Accepted", Assert.Single(holderResult.Items).Disposition);
            AddIngestionEvidence(evidence, holderResult);

            await using SandboxChild successor = await StartChildAsync(
                "mp05-successor",
                "ingest",
                "none",
                successorFixture,
                2,
                1).ConfigureAwait(false);
            await successor.StartOperationAsync().ConfigureAwait(false);
            SandboxOperationResult successorResult = await successor
                .WaitForResultAsync()
                .ConfigureAwait(false);
            Assert.Equal("Accepted", Assert.Single(successorResult.Items).Disposition);
            AssertDistinctProcesses(holderResult, cancelledResult, successorResult);
            AddIngestionEvidence(evidence, successorResult);
            await AssertActiveAcceptedMatrixAsync(fixture, 2, 2).ConfigureAwait(false);
            await AssertAcceptedMessageIdentitiesAsync(
                fixture,
                [fixture.MessageId, successorFixture.MessageId],
                [cancelledFixture.MessageId]).ConfigureAwait(false);
        }

        /// <summary>Seeds the single active operator, role and global revocation permission shared by the laboratory.</summary>
        /// <returns>A task completing after the synthetic authority is durable.</returns>
        private async Task SeedGlobalAuthorityAsync()
        {
            Guid userId = Guid.NewGuid();
            Guid roleId = Guid.NewGuid();
            Guid permissionId = Guid.NewGuid();
            await using ServerDbContext context = new(contextOptions);
            context.Users.Add(new PlatformUserRow
            {
                UserId = userId,
                SubjectId = SubjectId,
                DisplayName = "R-EGRESS fence operator",
                State = "Active",
                CreatedAt = now.AddMinutes(-10),
                UpdatedAt = now.AddMinutes(-10),
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Roles.Add(new RoleRow
            {
                RoleId = roleId,
                Name = "R-EGRESS fence revocation role",
                Description = "Synthetic local PostgreSQL fence authority.",
                IsSystem = false,
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Permissions.Add(new PermissionRow
            {
                PermissionId = permissionId,
                Code = PlatformPermissions.AgentsRevoke,
                Description = "Revoke one synthetic Agent.",
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
                GrantedAt = now.AddMinutes(-5),
                ExpiresAt = now.AddHours(1),
            });
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>Seeds one independent active Agent and one enabled PostgreSQL assignment.</summary>
        /// <param name="suffix">Stable scenario suffix used only in non-secret fixture labels.</param>
        /// <returns>The identities and deterministic message seeds owned by the fixture.</returns>
        private async Task<AgentFixture> SeedAgentAsync(string suffix)
        {
            Guid agentId = Guid.NewGuid();
            Guid instanceId = Guid.NewGuid();
            AgentFixture fixture = new(
                agentId,
                instanceId,
                Guid.NewGuid(),
                Guid.NewGuid());
            await using ServerDbContext context = new(contextOptions);
            context.Agents.Add(new RegisteredAgentRow
            {
                AgentId = agentId,
                InstallationId = $"installation:r-egress:{agentId:N}",
                DisplayName = $"R-EGRESS {suffix} Agent",
                Environment = "test",
                Platform = "windows-x64",
                AgentVersion = "1.0.0-test",
                CertificateThumbprint = Convert.ToHexString(agentId.ToByteArray()),
                State = "Active",
                EnrolledAt = now.AddMinutes(-5),
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Instances.Add(new DatabaseInstanceRow
            {
                InstanceId = instanceId,
                DisplayName = $"R-EGRESS {suffix} instance",
                ProviderType = "postgresql",
                Environment = "test",
                EndpointJson = "{}",
                AssignedAgentId = agentId,
                TagsJson = "[]",
                IntervalSeconds = 60,
                TimeoutSeconds = 5,
                RetryCount = 1,
                Enabled = true,
                CreatedAt = now.AddMinutes(-5),
                UpdatedAt = now.AddMinutes(-5),
                ConcurrencyToken = Guid.NewGuid(),
            });
            await context.SaveChangesAsync().ConfigureAwait(false);
            return fixture;
        }

        /// <summary>Starts one exact child, transfers its secret by private pipe and waits for readiness.</summary>
        /// <param name="label">Stable local process/session label.</param>
        /// <param name="operation">Exact kebab-case child operation.</param>
        /// <param name="barrier">Exact kebab-case barrier.</param>
        /// <param name="fixture">Agent, assignment and message identities.</param>
        /// <param name="sequenceStart">First positive sequence.</param>
        /// <param name="count">Bounded number of operations.</param>
        /// <param name="operationNow">Optional exact receipt and observation base instant for replay convergence.</param>
        /// <returns>A ready, parent-owned child process.</returns>
        private async Task<SandboxChild> StartChildAsync(
            string label,
            string operation,
            string barrier,
            AgentFixture fixture,
            long sequenceStart,
            int count,
            DateTimeOffset? operationNow = null)
        {
            int ordinal = Interlocked.Increment(ref sessionOrdinal);
            string applicationName = $"{ApplicationPrefix}{runToken}-{label}-{ordinal}";
            string pipeName = $"DBNotifier-REgress-{runToken}-{Guid.NewGuid():N}";
            NamedPipeServerStream pipe = new(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            ProcessStartInfo startInfo = new(dotnetPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.Environment.Remove(ActivationVariable);
            startInfo.Environment.Remove(ConnectionVariable);
            startInfo.Environment.Remove(SummaryVariable);
            startInfo.Environment.Remove(DotNetHostVariable);
            startInfo.Environment.Remove(RunTokenVariable);
            startInfo.ArgumentList.Add(hostPath);
            startInfo.ArgumentList.Add("--sandbox-r-egress-fence");
            AddArgument(startInfo, "--operation", operation);
            AddArgument(startInfo, "--barrier", barrier);
            AddArgument(startInfo, "--control-pipe", pipeName);
            AddArgument(startInfo, "--application-name", applicationName);
            AddArgument(startInfo, "--agent-id", fixture.AgentId.ToString("D"));
            AddArgument(startInfo, "--instance-id", fixture.InstanceId.ToString("D"));
            AddArgument(startInfo, "--message-id", fixture.MessageId.ToString("D"));
            AddArgument(startInfo, "--observation-id", fixture.ObservationId.ToString("D"));
            AddArgument(
                startInfo,
                "--sequence-start",
                sequenceStart.ToString(CultureInfo.InvariantCulture));
            AddArgument(startInfo, "--count", count.ToString(CultureInfo.InvariantCulture));
            AddArgument(
                startInfo,
                "--utc-now",
                (operationNow ?? now).ToString("O", CultureInfo.InvariantCulture));

            Process process = new() { StartInfo = startInfo };
            SandboxChild? child = null;
            try
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException("r_egress_fence.child_start_failed");
                }

                child = new SandboxChild(process, pipe, applicationName, operation);
                using CancellationTokenSource timeout = new(CoordinationTimeout);
                await pipe.WaitForConnectionAsync(timeout.Token).ConfigureAwait(false);
                string connection = BuildConnectionString(baseConnectionString, applicationName);
                await child.WriteSecretAsync(connection, timeout.Token).ConfigureAwait(false);
                SandboxSignal ready = await child.ReadSignalAsync("ready", timeout.Token).ConfigureAwait(false);
                Assert.Equal(process.Id, ready.ProcessId);
                children.Add(child);
                return child;
            }
            catch
            {
                if (child is not null)
                {
                    await child.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    await pipe.DisposeAsync().ConfigureAwait(false);
                    process.Dispose();
                }
                throw;
            }
        }

        /// <summary>Waits until PostgreSQL proves one exact child backend is blocked by another.</summary>
        /// <param name="blockedApplication">Expected blocked session label.</param>
        /// <param name="blockerApplication">Expected lock-owning session label.</param>
        /// <returns>The exact PostgreSQL backend pair.</returns>
        private async Task<BlockingPair> WaitForBlockingAsync(
            string blockedApplication,
            string blockerApplication)
        {
            await using NpgsqlConnection connection = await OpenControlConnectionAsync("blocking")
                .ConfigureAwait(false);
            await using NpgsqlCommand command = new(
                """
                SELECT blocked.pid, blocker.pid
                FROM pg_stat_activity AS blocked
                JOIN LATERAL unnest(pg_blocking_pids(blocked.pid)) AS blocked_by(blocker_pid) ON true
                JOIN pg_stat_activity AS blocker ON blocker.pid = blocked_by.blocker_pid
                WHERE blocked.application_name = @blocked
                  AND blocker.application_name = @blocker
                  AND cardinality(pg_blocking_pids(blocked.pid)) = 1
                """,
                connection);
            command.Parameters.AddWithValue("blocked", NpgsqlDbType.Text, blockedApplication);
            command.Parameters.AddWithValue("blocker", NpgsqlDbType.Text, blockerApplication);
            using CancellationTokenSource timeout = new(CoordinationTimeout);
            try
            {
                while (true)
                {
                    await using NpgsqlDataReader reader = await command
                        .ExecuteReaderAsync(timeout.Token)
                        .ConfigureAwait(false);
                    if (await reader.ReadAsync(timeout.Token).ConfigureAwait(false))
                    {
                        BlockingPair pair = new(reader.GetInt32(0), reader.GetInt32(1));
                        Assert.False(await reader.ReadAsync(timeout.Token).ConfigureAwait(false));
                        return pair;
                    }

                    await reader.DisposeAsync().ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                throw new TimeoutException("r_egress_fence.blocking_not_observed");
            }
        }

        /// <summary>Measures the maximum current laboratory waiter count after a required minimum is proved.</summary>
        /// <param name="applicationPrefix">Exact child-session prefix.</param>
        /// <param name="minimum">Minimum blocking cardinality required before release.</param>
        /// <returns>The maximum observed blocked-session count.</returns>
        private async Task<int> WaitForBlockedCountAsync(string applicationPrefix, int minimum)
        {
            await using NpgsqlConnection connection = await OpenControlConnectionAsync("blocking-count")
                .ConfigureAwait(false);
            await using NpgsqlCommand command = new(
                """
                SELECT count(*)::integer
                FROM pg_stat_activity
                WHERE application_name LIKE @prefix
                  AND cardinality(pg_blocking_pids(pid)) > 0
                """,
                connection);
            command.Parameters.AddWithValue("prefix", NpgsqlDbType.Text, $"{applicationPrefix}%");
            using CancellationTokenSource timeout = new(CoordinationTimeout);
            int maximum = 0;
            int samplesAfterMinimum = 0;
            try
            {
                while (samplesAfterMinimum < 5)
                {
                    int current = Convert.ToInt32(
                        await command.ExecuteScalarAsync(timeout.Token).ConfigureAwait(false),
                        CultureInfo.InvariantCulture);
                    maximum = Math.Max(maximum, current);
                    samplesAfterMinimum = maximum >= minimum
                        ? samplesAfterMinimum + 1
                        : 0;
                    await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token).ConfigureAwait(false);
                }

                return maximum;
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                throw new TimeoutException("r_egress_fence.blocked_count_not_observed");
            }
        }

        /// <summary>Opens one bounded, non-pooled loopback PostgreSQL control session.</summary>
        /// <param name="role">Stable non-secret control-session role.</param>
        /// <returns>An open connection owned by the caller.</returns>
        private async Task<NpgsqlConnection> OpenControlConnectionAsync(string role)
        {
            int ordinal = Interlocked.Increment(ref sessionOrdinal);
            string applicationName = $"{ApplicationPrefix}{runToken}-{role}-{ordinal}";
            NpgsqlConnection connection = new(BuildConnectionString(baseConnectionString, applicationName));
            using CancellationTokenSource timeout = new(CoordinationTimeout);
            try
            {
                await connection.OpenAsync(timeout.Token).ConfigureAwait(false);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>Proves a blocked contender has no externally committed Agent-scoped effects before release.</summary>
        /// <param name="fixture">Agent and assignment whose lock order is currently held.</param>
        /// <returns>A task completing after the pre-release durable snapshot is checked.</returns>
        private async Task AssertNoCommittedEffectsAsync(AgentFixture fixture)
        {
            await using ServerDbContext context = new(contextOptions);
            await AssertAgentStateAsync(context, fixture.AgentId, "Active").ConfigureAwait(false);
            Assert.Equal(0, await context.HealthSamples.CountAsync(
                row => row.AgentId == fixture.AgentId).ConfigureAwait(false));
            Assert.Equal(0, await context.AgentObservationCursors.CountAsync(
                row => row.AgentId == fixture.AgentId).ConfigureAwait(false));
            Assert.Equal(0, await context.RejectedObservationSequences.CountAsync(
                row => row.AgentId == fixture.AgentId).ConfigureAwait(false));
            await AssertAcceptedEffectsAsync(context, fixture, 0, 0).ConfigureAwait(false);
            Assert.Equal(0, await CountRevocationAuditsAsync(context, fixture.AgentId).ConfigureAwait(false));
        }

        /// <summary>Verifies the complete accepted-then-revoked durable effect matrix for one fixture.</summary>
        /// <param name="fixture">Scenario-owned Agent and assignment.</param>
        /// <returns>A task completing after every fixture-scoped effect is checked.</returns>
        private async Task AssertIngestionFirstMatrixAsync(AgentFixture fixture)
        {
            await using ServerDbContext context = new(contextOptions);
            await AssertAgentStateAsync(context, fixture.AgentId, "Revoked").ConfigureAwait(false);
            Assert.Equal(1, await context.HealthSamples.CountAsync(
                row => row.AgentId == fixture.AgentId).ConfigureAwait(false));
            Assert.Equal(1, await ReadCursorAsync(context, fixture.AgentId).ConfigureAwait(false));
            Assert.Equal(0, await context.RejectedObservationSequences.CountAsync(
                row => row.AgentId == fixture.AgentId).ConfigureAwait(false));
            await AssertAcceptedEffectsAsync(context, fixture, 1, 1).ConfigureAwait(false);
            Assert.Equal(1, await CountRevocationAuditsAsync(context, fixture.AgentId).ConfigureAwait(false));
        }

        /// <summary>Verifies the complete revoked-before-rejection durable effect matrix for one fixture.</summary>
        /// <param name="fixture">Scenario-owned Agent, assignment and message identity.</param>
        /// <returns>A task completing after every accepted and rejected effect boundary is checked.</returns>
        private async Task AssertRevocationFirstMatrixAsync(AgentFixture fixture)
        {
            await using ServerDbContext context = new(contextOptions);
            await AssertAgentStateAsync(context, fixture.AgentId, "Revoked").ConfigureAwait(false);
            Assert.Equal(0, await context.HealthSamples.CountAsync(
                row => row.AgentId == fixture.AgentId).ConfigureAwait(false));
            Assert.Equal(1, await ReadCursorAsync(context, fixture.AgentId).ConfigureAwait(false));
            RejectedObservationSequenceRow ledger = await context.RejectedObservationSequences
                .AsNoTracking()
                .SingleAsync(row => row.AgentId == fixture.AgentId)
                .ConfigureAwait(false);
            Assert.Equal(1, ledger.Sequence);
            Assert.Equal(fixture.MessageId, ledger.MessageId);
            Assert.Equal("agent.not_active", ledger.ErrorCode);
            await AssertAcceptedEffectsAsync(context, fixture, 0, 0).ConfigureAwait(false);
            Assert.Equal(1, await CountRevocationAuditsAsync(context, fixture.AgentId).ConfigureAwait(false));
        }

        /// <summary>Verifies one active Agent has exactly the requested contiguous accepted durable effects.</summary>
        /// <param name="fixture">Scenario-owned Agent and assignment.</param>
        /// <param name="sampleCount">Expected raw accepted samples.</param>
        /// <param name="highestSequence">Expected contiguous cursor and state position.</param>
        /// <returns>A task completing after exact sequence and effect verification.</returns>
        private async Task AssertActiveAcceptedMatrixAsync(
            AgentFixture fixture,
            int sampleCount,
            long highestSequence)
        {
            await using ServerDbContext context = new(contextOptions);
            await AssertAgentStateAsync(context, fixture.AgentId, "Active").ConfigureAwait(false);
            long[] sequences = await context.HealthSamples
                .AsNoTracking()
                .Where(row => row.AgentId == fixture.AgentId)
                .OrderBy(row => row.Sequence)
                .Select(row => row.Sequence)
                .ToArrayAsync()
                .ConfigureAwait(false);
            Assert.Equal(
                Enumerable.Range(1, sampleCount).Select(value => (long)value).ToArray(),
                sequences);
            Assert.Equal(highestSequence, await ReadCursorAsync(context, fixture.AgentId).ConfigureAwait(false));
            Assert.Equal(0, await context.RejectedObservationSequences.CountAsync(
                row => row.AgentId == fixture.AgentId).ConfigureAwait(false));
            await AssertAcceptedEffectsAsync(context, fixture, sampleCount > 0 ? 1 : 0, highestSequence)
                .ConfigureAwait(false);
            Assert.Equal(0, await CountRevocationAuditsAsync(context, fixture.AgentId).ConfigureAwait(false));
        }

        /// <summary>Verifies a raced target retained only its accepted prefix and consumed the rejected suffix.</summary>
        /// <param name="fixture">Target Agent and assignment.</param>
        /// <param name="acceptedCount">Accepted prefix length.</param>
        /// <param name="rejectedCount">Rejected suffix length.</param>
        /// <returns>A task completing after exact accepted/rejected provenance checks.</returns>
        private async Task AssertRevokedLoadMatrixAsync(
            AgentFixture fixture,
            int acceptedCount,
            int rejectedCount)
        {
            Assert.InRange(acceptedCount, 0, 24);
            Assert.Equal(25, acceptedCount + rejectedCount);
            await using ServerDbContext context = new(contextOptions);
            await AssertAgentStateAsync(context, fixture.AgentId, "Revoked").ConfigureAwait(false);
            long[] acceptedSequences = await context.HealthSamples
                .AsNoTracking()
                .Where(row => row.AgentId == fixture.AgentId)
                .OrderBy(row => row.Sequence)
                .Select(row => row.Sequence)
                .ToArrayAsync()
                .ConfigureAwait(false);
            Assert.Equal(
                Enumerable.Range(1, acceptedCount).Select(value => (long)value).ToArray(),
                acceptedSequences);
            RejectedObservationSequenceRow[] rejected = await context.RejectedObservationSequences
                .AsNoTracking()
                .Where(row => row.AgentId == fixture.AgentId)
                .OrderBy(row => row.Sequence)
                .ToArrayAsync()
                .ConfigureAwait(false);
            Assert.Equal(rejectedCount, rejected.Length);
            Assert.Equal(
                Enumerable.Range(acceptedCount + 1, rejectedCount)
                    .Select(value => (long)value)
                    .ToArray(),
                rejected.Select(row => row.Sequence).ToArray());
            Assert.All(rejected, row => Assert.Equal("agent.not_active", row.ErrorCode));
            Assert.Equal(25, await ReadCursorAsync(context, fixture.AgentId).ConfigureAwait(false));
            await AssertAcceptedEffectsAsync(
                context,
                fixture,
                acceptedCount > 0 ? 1 : 0,
                acceptedCount).ConfigureAwait(false);
            Assert.Equal(1, await CountRevocationAuditsAsync(context, fixture.AgentId).ConfigureAwait(false));
        }

        /// <summary>Verifies state, event, outbox and notification effects scoped to one assignment.</summary>
        /// <param name="context">Open verification context.</param>
        /// <param name="fixture">Scenario-owned Agent and assignment.</param>
        /// <param name="expectedEventCount">Expected canonical event cardinality.</param>
        /// <param name="expectedStateSequence">Expected state sequence, or zero when no state may exist.</param>
        /// <returns>A task completing after all downstream effect checks.</returns>
        private static async Task AssertAcceptedEffectsAsync(
            ServerDbContext context,
            AgentFixture fixture,
            int expectedEventCount,
            long expectedStateSequence)
        {
            InstanceObservationStateRow[] states = await context.InstanceObservationStates
                .AsNoTracking()
                .Where(row => row.AgentId == fixture.AgentId && row.InstanceId == fixture.InstanceId)
                .ToArrayAsync()
                .ConfigureAwait(false);
            if (expectedStateSequence == 0)
            {
                Assert.Empty(states);
            }
            else
            {
                InstanceObservationStateRow state = Assert.Single(states);
                Assert.Equal(expectedStateSequence, state.LastProcessedSequence);
                Assert.Equal("Healthy", state.Status);
            }

            Guid[] eventIds = await context.Events
                .AsNoTracking()
                .Where(row => row.AgentId == fixture.AgentId && row.InstanceId == fixture.InstanceId)
                .Select(row => row.EventId)
                .ToArrayAsync()
                .ConfigureAwait(false);
            Assert.Equal(expectedEventCount, eventIds.Length);
            Assert.Equal(expectedEventCount, await context.OutboxMessages.CountAsync(
                row => eventIds.Contains(row.MessageId)).ConfigureAwait(false));
            Assert.Equal(0, await context.NotificationDeliveries.CountAsync(
                row => eventIds.Contains(row.EventId)).ConfigureAwait(false));
        }

        /// <summary>Verifies exact accepted message identities and excludes interrupted/cancelled identities.</summary>
        /// <param name="fixture">Scenario-owned Agent.</param>
        /// <param name="expected">Exact accepted message identifiers.</param>
        /// <param name="excluded">Identifiers that must not have durable samples.</param>
        /// <returns>A task completing after identity verification.</returns>
        private async Task AssertAcceptedMessageIdentitiesAsync(
            AgentFixture fixture,
            IReadOnlyCollection<Guid> expected,
            IReadOnlyCollection<Guid> excluded)
        {
            await using ServerDbContext context = new(contextOptions);
            Guid[] accepted = await context.HealthSamples
                .AsNoTracking()
                .Where(row => row.AgentId == fixture.AgentId)
                .OrderBy(row => row.Sequence)
                .Select(row => row.MessageId)
                .ToArrayAsync()
                .ConfigureAwait(false);
            Assert.Equal(expected, accepted);
            Assert.DoesNotContain(accepted, excluded.Contains);
        }

        /// <summary>Checks one exact Agent state and its monotonic revocation timestamp contract.</summary>
        /// <param name="context">Open verification context.</param>
        /// <param name="agentId">Fixture Agent identity.</param>
        /// <param name="expectedState">Expected Active or Revoked state.</param>
        /// <returns>A task completing after the identity row is checked.</returns>
        private static async Task AssertAgentStateAsync(
            ServerDbContext context,
            Guid agentId,
            string expectedState)
        {
            RegisteredAgentRow agent = await context.Agents
                .AsNoTracking()
                .SingleAsync(row => row.AgentId == agentId)
                .ConfigureAwait(false);
            Assert.Equal(expectedState, agent.State);
            if (string.Equals(expectedState, "Revoked", StringComparison.Ordinal))
            {
                Assert.NotNull(agent.RevokedAt);
            }
            else
            {
                Assert.Null(agent.RevokedAt);
            }
        }

        /// <summary>Reads one required fixture cursor.</summary>
        /// <param name="context">Open verification context.</param>
        /// <param name="agentId">Fixture Agent identity.</param>
        /// <returns>The exact highest contiguous sequence.</returns>
        private static Task<long> ReadCursorAsync(ServerDbContext context, Guid agentId) =>
            context.AgentObservationCursors
                .AsNoTracking()
                .Where(row => row.AgentId == agentId)
                .Select(row => row.HighestContiguousSequence)
                .SingleAsync();

        /// <summary>Counts only successful principal revocation audits for one exact Agent.</summary>
        /// <param name="context">Open verification context.</param>
        /// <param name="agentId">Fixture Agent identity.</param>
        /// <returns>Matching durable audit cardinality.</returns>
        private static Task<int> CountRevocationAuditsAsync(ServerDbContext context, Guid agentId)
        {
            string target = agentId.ToString("D");
            return context.AuditEntries.CountAsync(row =>
                row.Action == "agent.revoke" &&
                row.TargetType == "Agent" &&
                row.TargetId == target &&
                row.Outcome == "Succeeded");
        }

        /// <summary>Creates one closed latency and throughput profile from completed child results.</summary>
        /// <param name="results">Completed load-process results.</param>
        /// <param name="maximumBlocked">Maximum observed blocked-session cardinality.</param>
        /// <param name="deadlocks">PostgreSQL deadlock delta.</param>
        /// <param name="wallElapsed">Parent-observed profile duration.</param>
        /// <returns>Sanitised numeric load profile.</returns>
        private static LoadProfileSummary CreateLoadSummary(
            IReadOnlyCollection<SandboxOperationResult> results,
            int maximumBlocked,
            long deadlocks,
            TimeSpan wallElapsed)
        {
            SandboxItemResult[] items = results.SelectMany(result => result.Items).ToArray();
            Assert.Equal(100, items.Length);
            double[] latencies = results
                .SelectMany(result => result.Items.Select(item =>
                {
                    Assert.True(result.StopwatchFrequency > 0);
                    Assert.True(item.CompletedTimestamp >= item.StartedTimestamp);
                    return (item.CompletedTimestamp - item.StartedTimestamp) *
                        1000d /
                        result.StopwatchFrequency;
                }))
                .Order()
                .ToArray();
            Assert.True(wallElapsed > TimeSpan.Zero);
            LatencySummary distribution = new(
                Round(NearestRank(latencies, 0.50)),
                Round(NearestRank(latencies, 0.95)),
                Round(NearestRank(latencies, 0.99)),
                Round(latencies[^1]));
            return new LoadProfileSummary(
                items.Length,
                results.Sum(result => result.SerializationFailureCount),
                items.Count(item => item.Disposition == "Retryable"),
                deadlocks,
                maximumBlocked,
                distribution,
                Round(items.Length / wallElapsed.TotalSeconds));
        }

        /// <summary>Returns one nearest-rank percentile from a non-empty sorted sample.</summary>
        /// <param name="sorted">Ascending finite sample.</param>
        /// <param name="percentile">Fraction in the closed-open interval above zero and no greater than one.</param>
        /// <returns>The selected observed value.</returns>
        private static double NearestRank(double[] sorted, double percentile)
        {
            Assert.NotEmpty(sorted);
            int rank = checked((int)Math.Ceiling(percentile * sorted.Length));
            return sorted[Math.Clamp(rank - 1, 0, sorted.Length - 1)];
        }

        /// <summary>Rounds a sanitised measurement to four fractional digits.</summary>
        /// <param name="value">Finite non-negative value.</param>
        /// <returns>Midpoint-away-from-zero rounded value.</returns>
        private static double Round(double value)
        {
            Assert.True(double.IsFinite(value) && value >= 0);
            return Math.Round(value, 4, MidpointRounding.AwayFromZero);
        }

        /// <summary>Adds one ingestion result while rejecting any unapproved disposition or malformed timing.</summary>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <param name="result">Typed child result.</param>
        /// <param name="duplicateIsExpected">Whether exact replay is part of the scenario contract.</param>
        /// <param name="retryableIsExpected">Whether a separately validated bounded SSI retry is expected.</param>
        private static void AddIngestionEvidence(
            LabEvidence evidence,
            SandboxOperationResult result,
            bool duplicateIsExpected = false,
            bool retryableIsExpected = false)
        {
            AddSerializationEvidence(evidence, result);
            foreach (SandboxItemResult item in result.Items)
            {
                if (item.CompletedTimestamp < item.StartedTimestamp)
                {
                    evidence.UnclassifiedFailureCount++;
                    continue;
                }

                switch (item.Disposition)
                {
                    case "Accepted":
                        evidence.AcceptedCount++;
                        break;
                    case "Rejected":
                        evidence.RejectedCount++;
                        break;
                    case "Duplicate" when duplicateIsExpected:
                        break;
                    case "Retryable" when retryableIsExpected:
                        evidence.RetryableCount++;
                        break;
                    default:
                        evidence.UnclassifiedFailureCount++;
                        break;
                }
            }
        }

        /// <summary>
        /// Records retryable items only when the child also proved a PostgreSQL serialisation failure and every
        /// canonical error belongs to the narrow persistence allowlist.
        /// </summary>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <param name="result">Typed child result containing only expected retryable items.</param>
        private static void AddExpectedRetryableEvidence(
            LabEvidence evidence,
            SandboxOperationResult result)
        {
            AddSerializationEvidence(evidence, result);
            Assert.True(result.SerializationFailureCount > 0);
            Assert.NotEmpty(result.Items);
            foreach (SandboxItemResult item in result.Items)
            {
                Assert.Equal("Retryable", item.Disposition);
                Assert.True(item.SerializationFailureCount > 0);
                Assert.Contains(
                    item.ErrorCode,
                    PersistenceRetryableErrorCodes);
                evidence.RetryableCount++;
            }
        }

        /// <summary>Adds bounded SQLSTATE evidence shared by ingestion and revocation process results.</summary>
        /// <param name="evidence">Closed summary accumulator.</param>
        /// <param name="result">Typed child result.</param>
        private static void AddSerializationEvidence(
            LabEvidence evidence,
            SandboxOperationResult result)
        {
            Assert.True(result.ProcessId > 0);
            Assert.True(result.SerializationFailureCount >= 0);
            Assert.True(result.StopwatchFrequency > 0);
            Assert.True(result.CompletedTimestamp >= result.StartedTimestamp);
            evidence.SerializationFailureCount = checked(
                evidence.SerializationFailureCount + result.SerializationFailureCount);
        }

        /// <summary>Requires every completed operation to originate in its own non-parent operating-system process.</summary>
        /// <param name="results">Results whose process identities must be distinct.</param>
        private static void AssertDistinctProcesses(params SandboxOperationResult[] results)
        {
            Assert.Equal(results.Length, results.Select(result => result.ProcessId).Distinct().Count());
            Assert.DoesNotContain(results, result => result.ProcessId == Environment.ProcessId);
        }

        /// <summary>Rebuilds one local-only non-pooled connection from a narrow runner-provided source.</summary>
        /// <param name="connectionString">Ephemeral runner-provided secret.</param>
        /// <param name="applicationName">Exact bounded session label.</param>
        /// <returns>A safe connection string retaining only the disposable database identity and secret.</returns>
        private static string BuildConnectionString(string connectionString, string applicationName)
        {
            NpgsqlConnectionStringBuilder source;
            try
            {
                source = new NpgsqlConnectionStringBuilder(connectionString);
            }
            catch (ArgumentException)
            {
                throw new InvalidDataException("r_egress_fence.connection_invalid");
            }

            if (!IPAddress.TryParse(source.Host, out IPAddress? address) ||
                !IPAddress.IsLoopback(address) ||
                source.Port is < 1 or > 65535 ||
                !string.Equals(source.Database, "dbnotifier_r_egress_fence", StringComparison.Ordinal) ||
                !string.Equals(source.Username, "dbnotifier_r_egress_fence", StringComparison.Ordinal) ||
                source.Password is null || source.Password.Length is < 16 or > 256 ||
                source.Pooling ||
                source.SslMode != SslMode.Disable ||
                source.IncludeErrorDetail ||
                source.LogParameters ||
                source.PersistSecurityInfo ||
                string.IsNullOrWhiteSpace(applicationName) ||
                applicationName.Length > 63)
            {
                throw new InvalidDataException("r_egress_fence.connection_invalid");
            }

            return new NpgsqlConnectionStringBuilder
            {
                Host = address.ToString(),
                Port = source.Port,
                Database = "dbnotifier_r_egress_fence",
                Username = "dbnotifier_r_egress_fence",
                Password = source.Password,
                ApplicationName = applicationName,
                SslMode = SslMode.Disable,
                Pooling = false,
                Enlist = false,
                Multiplexing = false,
                NoResetOnClose = false,
                PersistSecurityInfo = false,
                IncludeErrorDetail = false,
                LogParameters = false,
                Timeout = 5,
                CommandTimeout = 15,
                CancellationTimeout = 2000,
            }.ConnectionString;
        }

        /// <summary>Resolves only the exact runner-provided .NET host used to launch the current test command.</summary>
        /// <returns>Validated absolute dotnet executable path.</returns>
        private static string ResolveDotNetPath()
        {
            string candidate = Environment.GetEnvironmentVariable(DotNetHostVariable) ??
                throw new InvalidOperationException("r_egress_fence.dotnet_host_missing");
            string path = Path.GetFullPath(candidate);
            if (!Path.IsPathFullyQualified(candidate) ||
                !string.Equals(Path.GetFileName(path), "dotnet.exe", StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(path))
            {
                throw new InvalidDataException("r_egress_fence.dotnet_host_invalid");
            }

            return path;
        }

        /// <summary>Resolves the built child host from its owning project and active configuration.</summary>
        /// <returns>Absolute test-only host assembly path.</returns>
        private static string ResolveHostPath()
        {
            DirectoryInfo? root = new(AppContext.BaseDirectory);
            while (root is not null && !File.Exists(Path.Combine(root.FullName, "DBNotifier.sln")))
            {
                root = root.Parent;
            }

            string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? string.Empty;
            if (root is null || string.IsNullOrWhiteSpace(configuration))
            {
                throw new FileNotFoundException("The concurrency host workspace or configuration was not found.");
            }

            string path = Path.Combine(
                root.FullName,
                "tests",
                "DBNotifier.ServerConcurrency.SandboxHost",
                "bin",
                configuration,
                "net10.0",
                "DBNotifier.ServerConcurrency.SandboxHost.dll");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The built concurrency host was not found.", path);
            }

            return path;
        }

        /// <summary>Adds one exact command-line name/value pair without shell parsing.</summary>
        /// <param name="startInfo">Child start configuration.</param>
        /// <param name="name">Exact option name.</param>
        /// <param name="value">Bounded non-secret value.</param>
        private static void AddArgument(ProcessStartInfo startInfo, string name, string value)
        {
            startInfo.ArgumentList.Add(name);
            startInfo.ArgumentList.Add(value);
        }

        /// <summary>Owns one exact child process and its current-user-only parent pipe.</summary>
        private sealed class SandboxChild : IAsyncDisposable
        {
            private const int MaximumSecretBytes = 4096;
            private const int MaximumControlBytes = 1024;
            private const int MaximumResultBytes = 65536;
            private static readonly UTF8Encoding StrictUtf8 = new(false, true);
            private readonly Process process;
            private readonly NamedPipeServerStream pipe;
            private readonly string expectedOperation;
            private readonly int processId;
            private readonly SemaphoreSlim resourceGate = new(1, 1);
            private bool resourcesDisposed;
            private int resultRead;

            /// <summary>Initialises parent ownership after the process has started.</summary>
            /// <param name="process">Exact child process.</param>
            /// <param name="pipe">Connected-or-connecting private control pipe.</param>
            /// <param name="applicationName">Exact PostgreSQL session label.</param>
            /// <param name="expectedOperation">Exact operation selected on the command line.</param>
            internal SandboxChild(
                Process process,
                NamedPipeServerStream pipe,
                string applicationName,
                string expectedOperation)
            {
                this.process = process;
                this.pipe = pipe;
                this.expectedOperation = expectedOperation;
                processId = process.Id;
                ApplicationName = applicationName;
            }

            /// <summary>Gets the exact child operating-system process identifier.</summary>
            internal int ProcessId => processId;

            /// <summary>Gets the exact PostgreSQL application label supplied to this child.</summary>
            internal string ApplicationName { get; }

            /// <summary>Transfers the ephemeral connection once without placing it on the command line.</summary>
            /// <param name="connectionString">Validated loopback-only disposable connection secret.</param>
            /// <param name="cancellationToken">Bounded coordination cancellation.</param>
            /// <returns>A task completing after the secret frame is flushed.</returns>
            internal Task WriteSecretAsync(
                string connectionString,
                CancellationToken cancellationToken) =>
                WriteFrameAsync(
                    StrictUtf8.GetBytes(connectionString),
                    MaximumSecretBytes,
                    cancellationToken);

            /// <summary>Sends the exact start command after the child has reported readiness.</summary>
            /// <returns>A task completing after the bounded command frame is flushed.</returns>
            internal Task StartOperationAsync() => WriteCommandAsync("start");

            /// <summary>Sends a best-effort cleanup/release command without masking the owning assertion.</summary>
            /// <param name="command">Exact release, cancel or start command.</param>
            /// <returns>A task completing after delivery or a proved closed endpoint.</returns>
            internal async Task TrySendCommandAsync(string command)
            {
                try
                {
                    await WriteCommandAsync(command).ConfigureAwait(false);
                }
                catch (Exception exception) when (
                    exception is IOException or ObjectDisposedException or InvalidOperationException or TimeoutException)
                {
                    // The result wait or exact process cleanup remains the authoritative failure/cleanup boundary.
                }
            }

            /// <summary>Reads one strict child coordination signal within the default bounded deadline.</summary>
            /// <param name="expected">Exact expected signal name.</param>
            /// <returns>The validated signal.</returns>
            internal Task<SandboxSignal> ReadSignalAsync(string expected) =>
                ReadSignalAsync(expected, CancellationToken.None);

            /// <summary>Reads one strict child coordination signal within a caller-owned deadline.</summary>
            /// <param name="expected">Exact expected signal name.</param>
            /// <param name="cancellationToken">Caller-owned bounded cancellation.</param>
            /// <returns>The validated signal.</returns>
            internal async Task<SandboxSignal> ReadSignalAsync(
                string expected,
                CancellationToken cancellationToken)
            {
                using CancellationTokenSource timeout = CreateTimeout(
                    CoordinationTimeout,
                    cancellationToken);
                byte[] payload;
                try
                {
                    payload = await ReadFrameAsync(MaximumControlBytes, timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException("r_egress_fence.child_signal_timeout");
                }

                try
                {
                    using JsonDocument document = JsonDocument.Parse(
                        payload,
                        new JsonDocumentOptions
                        {
                            AllowTrailingCommas = false,
                            CommentHandling = JsonCommentHandling.Disallow,
                            MaxDepth = 3,
                        });
                    AssertExactProperties(
                        document.RootElement,
                        "signal",
                        "processId",
                        "backendPid",
                        "boundary");
                    SandboxSignal signal = JsonSerializer.Deserialize<SandboxSignal>(
                        document.RootElement.GetRawText(),
                        JsonOptions) ??
                        throw new InvalidDataException("r_egress_fence.child_signal_invalid");
                    if (!string.Equals(signal.Signal, expected, StringComparison.Ordinal) ||
                        signal.ProcessId != processId ||
                        (signal.BackendPid is not null && signal.BackendPid < 1))
                    {
                        throw new InvalidDataException("r_egress_fence.child_signal_invalid");
                    }

                    return signal;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(payload);
                }
            }

            /// <summary>Waits for one successful child exit and parses its single strict sanitised result.</summary>
            /// <returns>The validated child result.</returns>
            internal async Task<SandboxOperationResult> WaitForResultAsync()
            {
                if (Interlocked.Exchange(ref resultRead, 1) != 0)
                {
                    throw new InvalidOperationException("r_egress_fence.child_result_already_read");
                }

                using CancellationTokenSource timeout = new(ChildTimeout);
                Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(timeout.Token);
                Task<string> standardError = process.StandardError.ReadToEndAsync(timeout.Token);
                try
                {
                    await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    await KillAsync().ConfigureAwait(false);
                    throw new TimeoutException("r_egress_fence.child_exit_timeout");
                }

                string output;
                string error;
                try
                {
                    output = await standardOutput.ConfigureAwait(false);
                    error = await standardError.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    await KillAsync().ConfigureAwait(false);
                    throw new TimeoutException("r_egress_fence.child_output_timeout");
                }

                try
                {
                    if (process.ExitCode != 0)
                    {
                        string failureCode = ParseSanitisedFailure(
                            process.ExitCode,
                            output,
                            error);
                        throw new InvalidOperationException(failureCode);
                    }
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        throw new InvalidDataException("r_egress_fence.child_stderr_invalid");
                    }
                    string json = output.Trim();
                    if (json.Length is < 2 or > MaximumResultBytes)
                    {
                        throw new InvalidDataException("r_egress_fence.child_result_invalid");
                    }

                    SandboxOperationResult result = ParseResult(json);
                    if (result.ProcessId != processId ||
                        !string.Equals(result.Operation, expectedOperation, StringComparison.Ordinal) ||
                        result.SerializationFailureCount < 0 ||
                        result.StopwatchFrequency < 1 ||
                        result.CompletedTimestamp < result.StartedTimestamp ||
                        result.Items.Count > 25)
                    {
                        throw new InvalidDataException("r_egress_fence.child_result_invalid");
                    }

                    return result;
                }
                finally
                {
                    await DisposeResourcesAsync(killRunningProcess: false).ConfigureAwait(false);
                }
            }

            /// <summary>Validates the host's single closed failure line and reconstructs only its safe code.</summary>
            /// <param name="exitCode">Observed child process exit code.</param>
            /// <param name="standardOutput">Redirected child standard output.</param>
            /// <param name="standardError">Redirected child standard error.</param>
            /// <returns>A reconstructed bounded failure code containing no raw process output.</returns>
            private static string ParseSanitisedFailure(
                int exitCode,
                string standardOutput,
                string standardError)
            {
                if (exitCode != 2 ||
                    !string.IsNullOrWhiteSpace(standardOutput) ||
                    standardError.Length is < 1 or > 256)
                {
                    throw new InvalidDataException("r_egress_fence.child_failure_output_invalid");
                }

                Match match = Regex.Match(
                    standardError,
                    """
                    \Ar_egress_fence_host[.]failed:(argument-validation|control-pipe|operation):([A-Za-z][A-Za-z0-9]{0,127}):(none|[0-9A-Z]{5})\r?\n?\z
                    """,
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(100));
                if (!match.Success)
                {
                    throw new InvalidDataException("r_egress_fence.child_failure_output_invalid");
                }

                return string.Create(
                    CultureInfo.InvariantCulture,
                    $"r_egress_fence.child_failed:{match.Groups[1].Value}:{match.Groups[2].Value}:{match.Groups[3].Value}");
            }

            /// <summary>Kills only this exact child process tree and waits for bounded operating-system release.</summary>
            /// <returns>A task completing after exact child exit and resource disposal.</returns>
            internal async Task KillAsync()
            {
                await DisposeResourcesAsync(killRunningProcess: true).ConfigureAwait(false);
            }

            /// <summary>Stops only an unfinished owned child and disposes its private endpoint idempotently.</summary>
            /// <returns>A task completing after exact cleanup.</returns>
            public async ValueTask DisposeAsync()
            {
                await DisposeResourcesAsync(killRunningProcess: true).ConfigureAwait(false);
            }

            /// <summary>Sends one allowlisted parent command as a strict bounded JSON frame.</summary>
            /// <param name="command">Exact command name.</param>
            /// <returns>A task completing after flush.</returns>
            private async Task WriteCommandAsync(string command)
            {
                if (command is not ("start" or "release" or "cancel"))
                {
                    throw new ArgumentException("r_egress_fence.command_invalid", nameof(command));
                }

                using CancellationTokenSource timeout = new(CoordinationTimeout);
                byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
                    new SandboxCommand(command),
                    JsonOptions);
                try
                {
                    await WriteFrameAsync(
                        payload,
                        MaximumControlBytes,
                        timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    throw new TimeoutException("r_egress_fence.child_command_timeout");
                }
            }

            /// <summary>Parses one strict result and validates each bounded item schema.</summary>
            /// <param name="json">Single child stdout JSON object.</param>
            /// <returns>The typed result.</returns>
            private static SandboxOperationResult ParseResult(string json)
            {
                using JsonDocument document = JsonDocument.Parse(
                    json,
                    new JsonDocumentOptions
                    {
                        AllowTrailingCommas = false,
                        CommentHandling = JsonCommentHandling.Disallow,
                        MaxDepth = 4,
                    });
                AssertExactProperties(
                    document.RootElement,
                    "processId",
                    "operation",
                    "cancelled",
                    "serializationFailureCount",
                    "startedTimestamp",
                    "completedTimestamp",
                    "stopwatchFrequency",
                    "items");
                JsonElement items = document.RootElement.GetProperty("items");
                if (items.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidDataException("r_egress_fence.child_result_invalid");
                }

                foreach (JsonElement item in items.EnumerateArray())
                {
                    AssertExactProperties(
                        item,
                        "sequence",
                        "disposition",
                        "errorCode",
                        "serializationFailureCount",
                        "startedTimestamp",
                        "completedTimestamp");
                }

                SandboxOperationResult result = JsonSerializer.Deserialize<SandboxOperationResult>(
                    document.RootElement.GetRawText(),
                    JsonOptions) ??
                    throw new InvalidDataException("r_egress_fence.child_result_invalid");
                foreach (SandboxItemResult item in result.Items)
                {
                    if (item.Sequence < 0 ||
                        item.SerializationFailureCount < 0 ||
                        item.CompletedTimestamp < item.StartedTimestamp ||
                        string.IsNullOrWhiteSpace(item.Disposition) ||
                        item.Disposition.Length > 64 ||
                        (item.ErrorCode?.Length ?? 0) > 100)
                    {
                        throw new InvalidDataException("r_egress_fence.child_result_invalid");
                    }
                }
                if (result.Items.Sum(item => item.SerializationFailureCount) !=
                    result.SerializationFailureCount)
                {
                    throw new InvalidDataException("r_egress_fence.child_result_invalid");
                }

                return result;
            }

            /// <summary>Requires an object to contain exactly the named case-sensitive properties.</summary>
            /// <param name="element">Candidate JSON object.</param>
            /// <param name="expected">Exact property names.</param>
            private static void AssertExactProperties(JsonElement element, params string[] expected)
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException("r_egress_fence.child_json_invalid");
                }

                string[] actual = element.EnumerateObject()
                    .Select(property => property.Name)
                    .Order(StringComparer.Ordinal)
                    .ToArray();
                string[] orderedExpected = expected.Order(StringComparer.Ordinal).ToArray();
                if (!actual.SequenceEqual(orderedExpected, StringComparer.Ordinal))
                {
                    throw new InvalidDataException("r_egress_fence.child_json_invalid");
                }
            }

            /// <summary>Reads one bounded network-order length-prefixed frame.</summary>
            /// <param name="maximumLength">Maximum admitted payload length.</param>
            /// <param name="cancellationToken">Bounded pipe cancellation.</param>
            /// <returns>The caller-owned payload buffer.</returns>
            private async Task<byte[]> ReadFrameAsync(
                int maximumLength,
                CancellationToken cancellationToken)
            {
                byte[] length = new byte[sizeof(int)];
                await pipe.ReadExactlyAsync(length, cancellationToken).ConfigureAwait(false);
                int count = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(length));
                if (count is < 1 || count > maximumLength)
                {
                    throw new InvalidDataException("r_egress_fence.frame_invalid");
                }

                byte[] payload = new byte[count];
                try
                {
                    await pipe.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);
                    return payload;
                }
                catch
                {
                    CryptographicOperations.ZeroMemory(payload);
                    throw;
                }
            }

            /// <summary>Writes one bounded network-order length-prefixed frame and clears its payload.</summary>
            /// <param name="payload">Caller-owned payload cleared after the write attempt.</param>
            /// <param name="maximumLength">Maximum permitted payload length.</param>
            /// <param name="cancellationToken">Bounded pipe cancellation.</param>
            /// <returns>A task completing after flush.</returns>
            private async Task WriteFrameAsync(
                byte[] payload,
                int maximumLength,
                CancellationToken cancellationToken)
            {
                try
                {
                    if (payload.Length is < 1 || payload.Length > maximumLength)
                    {
                        throw new InvalidDataException("r_egress_fence.frame_invalid");
                    }

                    byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));
                    await pipe.WriteAsync(length, cancellationToken).ConfigureAwait(false);
                    await pipe.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                    await pipe.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(payload);
                }
            }

            /// <summary>Creates a linked deadline that never weakens an earlier caller cancellation.</summary>
            /// <param name="cancellationToken">Optional caller cancellation.</param>
            /// <param name="duration">Maximum permitted operation duration.</param>
            /// <returns>A caller-owned linked token source.</returns>
            private static CancellationTokenSource CreateTimeout(
                TimeSpan duration,
                CancellationToken cancellationToken)
            {
                CancellationTokenSource timeout = CancellationTokenSource
                    .CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(duration);
                return timeout;
            }

            /// <summary>Disposes exact process/pipe resources once, optionally terminating an unfinished child first.</summary>
            /// <param name="killRunningProcess">Whether an unfinished owned process must be killed.</param>
            /// <returns>A task completing after exact resource release.</returns>
            private async Task DisposeResourcesAsync(bool killRunningProcess)
            {
                await resourceGate.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (resourcesDisposed)
                    {
                        return;
                    }

                    if (killRunningProcess && !process.HasExited)
                    {
                        try
                        {
                            process.Kill(entireProcessTree: true);
                        }
                        catch (InvalidOperationException) when (process.HasExited)
                        {
                            // The exact owned process exited between state inspection and termination.
                        }

                        using CancellationTokenSource timeout = new(CoordinationTimeout);
                        try
                        {
                            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                        {
                            throw new TimeoutException("r_egress_fence.child_cleanup_timeout");
                        }
                    }

                    if (!process.HasExited)
                    {
                        throw new InvalidOperationException("r_egress_fence.child_exit_unproved");
                    }

                    await pipe.DisposeAsync().ConfigureAwait(false);
                    process.Dispose();
                    resourcesDisposed = true;
                }
                finally
                {
                    resourceGate.Release();
                }
            }
        }

        /// <summary>Captures one independent Agent, assignment and deterministic child message seeds.</summary>
        /// <param name="AgentId">Synthetic Agent identity.</param>
        /// <param name="InstanceId">Assigned database instance.</param>
        /// <param name="MessageId">First message-identity seed.</param>
        /// <param name="ObservationId">First observation-identity seed.</param>
        private sealed record AgentFixture(
            Guid AgentId,
            Guid InstanceId,
            Guid MessageId,
            Guid ObservationId);

        /// <summary>Captures one exact PostgreSQL blocking relation.</summary>
        /// <param name="BlockedBackendPid">Blocked backend process identifier.</param>
        /// <param name="BlockerBackendPid">Lock-owning backend process identifier.</param>
        private sealed record BlockingPair(int BlockedBackendPid, int BlockerBackendPid);

        /// <summary>Represents one strict child coordination signal.</summary>
        /// <param name="Signal">Stable signal name.</param>
        /// <param name="ProcessId">Exact child operating-system process identifier.</param>
        /// <param name="BackendPid">PostgreSQL backend process identifier when applicable.</param>
        /// <param name="Boundary">Selected barrier when applicable.</param>
        private sealed record SandboxSignal(
            string Signal,
            int ProcessId,
            int? BackendPid,
            string? Boundary);

        /// <summary>Represents one strict parent coordination command.</summary>
        /// <param name="Command">Exact allowlisted command.</param>
        private sealed record SandboxCommand(string Command);

        /// <summary>Represents one complete sanitised child-process operation.</summary>
        /// <param name="ProcessId">Exact child operating-system process identifier.</param>
        /// <param name="Operation">Stable operation name.</param>
        /// <param name="Cancelled">Whether deliberate cancellation ended the operation.</param>
        /// <param name="SerializationFailureCount">Observed SQLSTATE 40001 count.</param>
        /// <param name="StartedTimestamp">Monotonic operation-start timestamp.</param>
        /// <param name="CompletedTimestamp">Monotonic operation-completion timestamp.</param>
        /// <param name="StopwatchFrequency">Monotonic ticks per second.</param>
        /// <param name="Items">Bounded typed item outcomes.</param>
        private sealed record SandboxOperationResult(
            int ProcessId,
            string Operation,
            bool Cancelled,
            int SerializationFailureCount,
            long StartedTimestamp,
            long CompletedTimestamp,
            long StopwatchFrequency,
            IReadOnlyList<SandboxItemResult> Items);

        /// <summary>Represents one bounded observation or revocation outcome.</summary>
        /// <param name="Sequence">Positive observation sequence, or zero for revocation.</param>
        /// <param name="Disposition">Typed production result name.</param>
        /// <param name="ErrorCode">Stable sanitised error code.</param>
        /// <param name="SerializationFailureCount">SQLSTATE 40001 failures attributed to this exact item.</param>
        /// <param name="StartedTimestamp">Monotonic item-start timestamp.</param>
        /// <param name="CompletedTimestamp">Monotonic item-completion timestamp.</param>
        private sealed record SandboxItemResult(
            long Sequence,
            string Disposition,
            string? ErrorCode,
            int SerializationFailureCount,
            long StartedTimestamp,
            long CompletedTimestamp);
    }
}
