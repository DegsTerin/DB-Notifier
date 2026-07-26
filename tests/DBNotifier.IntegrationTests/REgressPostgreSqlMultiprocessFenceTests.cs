// Module purpose: Orchestrates the marker-gated multiprocess PostgreSQL laboratory for observation ingestion and Agent revocation.
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Net;
using System.Text;
using System.Text.Json;
using DBNotifier.Application.Access;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>
/// Proves the cross-process PostgreSQL identity fence, bounded cancellation/crash recovery, Agent isolation and
/// characterised same-Agent and distinct-Agent contention without composing an operational Server runtime.
/// </summary>
public sealed partial class REgressPostgreSqlMultiprocessFenceTests
{
    private const string ActivationVariable = "DBNOTIFIER_R_EGRESS_POSTGRESQL_LAB";
    private const string ActivationValue = "local-test";
    private const string ConnectionVariable = "DBNOTIFIER_R_EGRESS_POSTGRESQL_CONNECTION";
    private const string SummaryVariable = "DBNOTIFIER_R_EGRESS_POSTGRESQL_SUMMARY";
    private const string RunTokenVariable = "DBNOTIFIER_R_EGRESS_POSTGRESQL_RUN_TOKEN";
    private const string SubjectId = "r-egress-fence-operator";
    private static readonly DateTimeOffset Now = new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Executes every authorised process ordering and writes one closed, sanitised summary for the owning runner.
    /// The test remains inert unless the exact disposable-laboratory marker is present.
    /// </summary>
    [Fact]
    public async Task DisposablePostgreSqlProvesMultiprocessFenceAndBoundedLoad()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(ActivationVariable),
                ActivationValue,
                StringComparison.Ordinal))
        {
            return;
        }

        string connectionString = Environment.GetEnvironmentVariable(ConnectionVariable) ??
            throw new InvalidOperationException("r_egress_fence.connection_missing");
        string summaryPath = ValidateSummaryPath(
            Environment.GetEnvironmentVariable(SummaryVariable) ??
            throw new InvalidOperationException("r_egress_fence.summary_path_missing"));
        string runToken = ValidateRunToken(
            Environment.GetEnvironmentVariable(RunTokenVariable) ??
            throw new InvalidOperationException("r_egress_fence.run_token_missing"));
        LabEvidence evidence = new();
        try
        {
            await using PostgreSqlFenceLab lab = await PostgreSqlFenceLab
                .CreateAsync(connectionString, Now, runToken);
            DatabaseCounters before = await lab.ReadDatabaseCountersAsync();

            await lab.ProveIngestionFirstAsync(evidence);
            await lab.ProveRevocationFirstAsync(evidence);
            await lab.ProveSerializationReclassificationAsync(evidence);
            await lab.ProveCrashAndCancellationAsync(evidence);
            await lab.ProveDistinctAgentProgressAsync(evidence);
            evidence.SameAgentLoad = await lab.CharacteriseSameAgentLoadAsync(evidence);
            evidence.DistinctAgentLoad = await lab.CharacteriseDistinctAgentLoadAsync(evidence);

            DatabaseCounters after = await lab.ReadDatabaseCountersAsync();
            evidence.DeadlockCount = checked(after.Deadlocks - before.Deadlocks);
            evidence.ResidualResourceCount = await lab.CountResidualSessionsAsync();
            await WriteSummaryAsync(summaryPath, evidence.ToSummary());

            Assert.Equal(8, evidence.ScenarioCount);
            Assert.Equal(evidence.RetryableCount, evidence.ConvergenceOperationCount);
            Assert.Equal(0, evidence.DeadlockCount);
            Assert.Equal(0, evidence.UnclassifiedFailureCount);
            Assert.Equal(0, evidence.ResidualResourceCount);
        }
        catch
        {
            evidence.UnclassifiedFailureCount++;
            await WriteSummaryAsync(summaryPath, evidence.ToSummary());
            throw;
        }
    }

    /// <summary>Validates the runner-owned non-secret token embedded in every child process and session identity.</summary>
    /// <param name="value">Candidate lowercase hexadecimal token.</param>
    /// <returns>The exact twelve-character token.</returns>
    private static string ValidateRunToken(string value)
    {
        if (value.Length != 12 ||
            value.Any(character =>
                character is not (>= '0' and <= '9') and
                not (>= 'a' and <= 'f')))
        {
            throw new InvalidDataException("r_egress_fence.run_token_invalid");
        }

        return value;
    }

    /// <summary>Validates the exact runner-owned destination used for the sanitised summary.</summary>
    /// <param name="path">Candidate process-scoped summary path.</param>
    /// <returns>Canonical validated path.</returns>
    private static string ValidateSummaryPath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        DirectoryInfo? parent = Directory.GetParent(fullPath);
        if (!Path.IsPathFullyQualified(path) ||
            !string.Equals(
                Path.GetFileName(fullPath),
                "r-egress-postgresql-fence-summary.json",
                StringComparison.Ordinal) ||
            parent is null ||
            !parent.Name.StartsWith("DBNotifier-R-Egress-Fence-", StringComparison.Ordinal) ||
            !parent.Exists)
        {
            throw new InvalidDataException("r_egress_fence.summary_path_invalid");
        }

        return fullPath;
    }

    /// <summary>Writes the closed summary atomically without retaining connection or credential material.</summary>
    /// <param name="path">Validated runner-owned final path.</param>
    /// <param name="summary">Sanitised evidence object.</param>
    /// <returns>A task completing after the atomic replacement.</returns>
    private static async Task WriteSummaryAsync(string path, LabSummary summary)
    {
        string temporaryPath = path + ".tmp";
        string json = JsonSerializer.Serialize(summary, JsonOptions);
        await File.WriteAllTextAsync(temporaryPath, json, new UTF8Encoding(false)).ConfigureAwait(false);
        File.Move(temporaryPath, path, overwrite: true);
    }

    /// <summary>Accumulates closed numeric evidence while scenarios execute.</summary>
    private sealed class LabEvidence
    {
        internal int ScenarioCount { get; set; }
        internal int BlockingObservationCount { get; set; }
        internal int SerializationFailureCount { get; set; }
        internal int AcceptedCount { get; set; }
        internal int RejectedCount { get; set; }
        internal int RetryableCount { get; set; }
        internal int ConvergenceOperationCount { get; set; }
        internal long DeadlockCount { get; set; }
        internal int UnclassifiedFailureCount { get; set; }
        internal int RevocationCompletionPosition { get; set; }
        internal int ResidualResourceCount { get; set; }
        internal LoadProfileSummary SameAgentLoad { get; set; } = LoadProfileSummary.Empty;
        internal LoadProfileSummary DistinctAgentLoad { get; set; } = LoadProfileSummary.Empty;

        /// <summary>Creates the exact closed summary schema consumed by the runner.</summary>
        /// <returns>Sanitised numeric summary.</returns>
        internal LabSummary ToSummary() =>
            new(
                2,
                ScenarioCount,
                BlockingObservationCount,
                SerializationFailureCount,
                AcceptedCount,
                RejectedCount,
                RetryableCount,
                ConvergenceOperationCount,
                DeadlockCount,
                UnclassifiedFailureCount,
                SameAgentLoad,
                DistinctAgentLoad,
                RevocationCompletionPosition,
                ResidualResourceCount);
    }

    /// <summary>Owns the disposable PostgreSQL orchestration and all exact child-process cleanup.</summary>
    private sealed partial class PostgreSqlFenceLab : IAsyncDisposable;

    /// <summary>Captures PostgreSQL database counters surrounding one bounded profile.</summary>
    /// <param name="Commits">Committed transactions.</param>
    /// <param name="Rollbacks">Rolled-back transactions.</param>
    /// <param name="Deadlocks">Detected deadlocks.</param>
    private sealed record DatabaseCounters(long Commits, long Rollbacks, long Deadlocks);

    /// <summary>Defines one closed load-profile measurement.</summary>
    /// <param name="OperationCount">Retained operation count.</param>
    /// <param name="SerializationFailureCount">Observed SQLSTATE 40001 count.</param>
    /// <param name="RetryableCount">Retryable results retained inside the original one-hundred-operation profile.</param>
    /// <param name="DeadlockCount">PostgreSQL deadlock delta.</param>
    /// <param name="MaximumBlockedCount">Maximum simultaneously blocked laboratory sessions.</param>
    /// <param name="LatencyMilliseconds">Nearest-rank latency distribution.</param>
    /// <param name="ThroughputPerSecond">Observed completed operations per wall-clock second.</param>
    private sealed record LoadProfileSummary(
        int OperationCount,
        int SerializationFailureCount,
        int RetryableCount,
        long DeadlockCount,
        int MaximumBlockedCount,
        LatencySummary LatencyMilliseconds,
        double ThroughputPerSecond)
    {
        internal static LoadProfileSummary Empty { get; } =
            new(0, 0, 0, 0, 0, new LatencySummary(0, 0, 0, 0), 0);
    }

    /// <summary>Defines nearest-rank latency percentiles and maximum in milliseconds.</summary>
    /// <param name="P50">Fiftieth percentile.</param>
    /// <param name="P95">Ninety-fifth percentile.</param>
    /// <param name="P99">Ninety-ninth percentile.</param>
    /// <param name="Max">Maximum retained latency.</param>
    private sealed record LatencySummary(double P50, double P95, double P99, double Max);

    /// <summary>Defines the complete deterministic summary schema printed by the owning runner.</summary>
    private sealed record LabSummary(
        int SchemaVersion,
        int ScenarioCount,
        int BlockingObservationCount,
        int SerializationFailureCount,
        int AcceptedCount,
        int RejectedCount,
        int RetryableCount,
        int ConvergenceOperationCount,
        long DeadlockCount,
        int UnclassifiedFailureCount,
        LoadProfileSummary SameAgentLoad,
        LoadProfileSummary DistinctAgentLoad,
        int RevocationCompletionPosition,
        int ResidualResourceCount);
}
