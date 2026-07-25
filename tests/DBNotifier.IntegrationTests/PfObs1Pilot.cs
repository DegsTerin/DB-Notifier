// Module purpose: Runs the exact PF-OBS-1 PostgreSQL laboratory through the read-only O2/MOD-12 path and produces authenticated pilot evidence.
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBNotifier.Application.AIOps;
using DBNotifier.Application.Synchronization;
using DBNotifier.Domain;
using Npgsql;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Classifies one laboratory-only corpus sample without granting production representativeness.</summary>
internal enum PfObs1Partition
{
    Development,
    Calibration,
    Holdout,
}

/// <summary>Contains one bounded PostgreSQL laboratory measurement and its predeclared expected class.</summary>
/// <param name="Id">Stable content-derived sample identifier.</param>
/// <param name="Partition">Immutable development, calibration or holdout partition.</param>
/// <param name="DurationMilliseconds">Observed round-trip duration for the fixed read-only query.</param>
/// <param name="ExpectedDegraded">Whether the query fixture deliberately included bounded server delay.</param>
internal sealed record PfObs1CorpusSample(
    string Id,
    PfObs1Partition Partition,
    double DurationMilliseconds,
    bool ExpectedDegraded);

/// <summary>Records the frozen threshold and holdout metrics derived exclusively from the local PostgreSQL laboratory.</summary>
/// <param name="ThresholdMilliseconds">Threshold frozen before holdout access.</param>
/// <param name="PolicyDigest">Content address of the frozen policy.</param>
/// <param name="HoldoutCount">Number of immutable holdout samples.</param>
/// <param name="TruePositive">Correct degraded classifications.</param>
/// <param name="TrueNegative">Correct normal classifications.</param>
/// <param name="FalsePositive">Normal samples incorrectly classified as degraded.</param>
/// <param name="FalseNegative">Degraded samples incorrectly classified as normal.</param>
/// <param name="Accuracy">Finite holdout accuracy in the closed interval zero through one.</param>
internal sealed record PfObs1HoldoutResult(
    double ThresholdMilliseconds,
    string PolicyDigest,
    int HoldoutCount,
    int TruePositive,
    int TrueNegative,
    int FalsePositive,
    int FalseNegative,
    double Accuracy);

/// <summary>
/// Contains sanitised, content-addressed evidence for the single local PostgreSQL 16 pilot cell.
/// No connection string, password, certificate, SQL text or host identity is retained.
/// </summary>
internal sealed record PfObs1PilotEvidence(
    string SchemaVersion,
    string CellId,
    string ActivationState,
    bool SyntheticLaboratory,
    bool ProductionRepresentative,
    bool ReadOnly,
    bool IsAuthorising,
    string Provider,
    string ProviderSemanticVersion,
    string ImageDigest,
    string TlsCertificateDigest,
    DateTimeOffset GeneratedAtUtc,
    double LatestDurationMilliseconds,
    string LatestHealthStatus,
    string O2EnvelopeDigest,
    string Mod12ReportDigest,
    string CorpusManifestDigest,
    int CorpusCount,
    PfObs1HoldoutResult Holdout,
    IReadOnlyList<string> ValidationCodes,
    string ResponsibleHuman,
    string EvidenceDigest);

/// <summary>Collects bounded read-only PostgreSQL measurements over hostname-verified laboratory TLS.</summary>
internal sealed class PfObs1PostgreSqlCollector
{
    private const int CommandTimeoutSeconds = 5;
    private readonly string connectionString;

    /// <summary>Initialises the collector from a caller-owned ephemeral connection string.</summary>
    /// <param name="connectionString">Synthetic laboratory connection string, retained only in memory.</param>
    internal PfObs1PostgreSqlCollector(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        NpgsqlConnectionStringBuilder parsed = new(connectionString);
        if (parsed.Host != "localhost" ||
            parsed.Port is < 1 or > 65535 ||
            parsed.SslMode != SslMode.VerifyFull ||
            string.IsNullOrWhiteSpace(parsed.RootCertificate) ||
            parsed.Pooling)
        {
            throw new InvalidOperationException("pfobs1.postgresql.boundary_refused");
        }
        this.connectionString = parsed.ConnectionString;
    }

    /// <summary>Executes one fixed read-only query, optionally including a bounded server-side delay fixture.</summary>
    /// <param name="delayed">Whether to include the sixty-millisecond laboratory delay.</param>
    /// <param name="cancellationToken">Cancellation propagated to connection and query operations.</param>
    /// <returns>The measured round-trip duration.</returns>
    internal async Task<TimeSpan> MeasureAsync(bool delayed, CancellationToken cancellationToken)
    {
        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString);
        await using NpgsqlConnection connection =
            await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        const string normalQuery = "SELECT count(*) FROM pf_obs1_samples";
        const string delayedQuery =
            "SELECT count(*) FROM pf_obs1_samples CROSS JOIN LATERAL (SELECT pg_sleep(0.060)) AS delay";
        await using NpgsqlCommand command = new(delayed ? delayedQuery : normalQuery, connection)
        {
            CommandTimeout = CommandTimeoutSeconds,
        };
        Stopwatch stopwatch = Stopwatch.StartNew();
        object? value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        if (Convert.ToInt64(value, CultureInfo.InvariantCulture) != 32)
        {
            throw new InvalidOperationException("pfobs1.postgresql.fixture_invalid");
        }
        return stopwatch.Elapsed;
    }
}

/// <summary>Builds immutable laboratory partitions, freezes calibration and evaluates holdout exactly once.</summary>
internal static class PfObs1CorpusEvaluation
{
    internal const int SampleCount = 36;

    /// <summary>Creates disjoint partitions in stable order from completed live laboratory measurements.</summary>
    /// <param name="measurements">Exact normal/degraded alternating measurements.</param>
    /// <returns>Immutable content-addressed laboratory samples.</returns>
    internal static IReadOnlyList<PfObs1CorpusSample> Partition(
        IReadOnlyList<(double DurationMilliseconds, bool ExpectedDegraded)> measurements)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        if (measurements.Count != SampleCount ||
            measurements.Any(item => !double.IsFinite(item.DurationMilliseconds) ||
                                     item.DurationMilliseconds < 0d))
        {
            throw new InvalidOperationException("pfobs1.corpus.incomplete");
        }

        return measurements.Select((item, index) =>
        {
            PfObs1Partition partition = index < 12
                ? PfObs1Partition.Development
                : index < 24
                    ? PfObs1Partition.Calibration
                    : PfObs1Partition.Holdout;
            string id = Digest($"{index}|{partition}|{item.DurationMilliseconds:F6}|{item.ExpectedDegraded}");
            return new PfObs1CorpusSample(id, partition, item.DurationMilliseconds, item.ExpectedDegraded);
        }).ToArray();
    }

    /// <summary>Freezes a threshold from development/calibration before exposing any holdout value.</summary>
    /// <param name="corpus">Complete disjoint laboratory corpus.</param>
    /// <returns>Finite policy threshold and content address.</returns>
    internal static (double Threshold, string Digest) Freeze(IReadOnlyList<PfObs1CorpusSample> corpus)
    {
        ValidateMembership(corpus);
        PfObs1CorpusSample[] calibration = corpus
            .Where(item => item.Partition != PfObs1Partition.Holdout)
            .ToArray();
        double maximumNormal = calibration
            .Where(item => !item.ExpectedDegraded)
            .Max(item => item.DurationMilliseconds);
        double minimumDegraded = calibration
            .Where(item => item.ExpectedDegraded)
            .Min(item => item.DurationMilliseconds);
        if (maximumNormal >= minimumDegraded)
        {
            throw new InvalidOperationException("pfobs1.calibration.not_separable");
        }
        double threshold = maximumNormal + ((minimumDegraded - maximumNormal) / 2d);
        return (threshold, Digest($"pfobs1.policy.v1|{threshold:F6}|{ManifestDigest(corpus)}"));
    }

    /// <summary>Evaluates immutable holdout against an already frozen policy without feedback.</summary>
    /// <param name="corpus">Complete disjoint laboratory corpus.</param>
    /// <param name="threshold">Previously frozen finite threshold.</param>
    /// <param name="policyDigest">Previously frozen policy digest.</param>
    /// <returns>Complete segmented binary classification metrics.</returns>
    internal static PfObs1HoldoutResult Evaluate(
        IReadOnlyList<PfObs1CorpusSample> corpus,
        double threshold,
        string policyDigest)
    {
        ValidateMembership(corpus);
        if (!double.IsFinite(threshold) || threshold <= 0d || !IsDigest(policyDigest))
        {
            throw new InvalidOperationException("pfobs1.holdout.policy_invalid");
        }
        PfObs1CorpusSample[] holdout = corpus
            .Where(item => item.Partition == PfObs1Partition.Holdout)
            .ToArray();
        int truePositive = holdout.Count(item => item.ExpectedDegraded && item.DurationMilliseconds >= threshold);
        int trueNegative = holdout.Count(item => !item.ExpectedDegraded && item.DurationMilliseconds < threshold);
        int falsePositive = holdout.Count(item => !item.ExpectedDegraded && item.DurationMilliseconds >= threshold);
        int falseNegative = holdout.Count(item => item.ExpectedDegraded && item.DurationMilliseconds < threshold);
        double accuracy = (truePositive + trueNegative) / (double)holdout.Length;
        return new(
            threshold,
            policyDigest,
            holdout.Length,
            truePositive,
            trueNegative,
            falsePositive,
            falseNegative,
            accuracy);
    }

    /// <summary>Returns the content address of exact ordered membership and content.</summary>
    internal static string ManifestDigest(IReadOnlyList<PfObs1CorpusSample> corpus) =>
        Digest(JsonSerializer.Serialize(corpus));

    /// <summary>Computes one uppercase SHA-256 content address.</summary>
    internal static string Digest(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    /// <summary>Checks whether a value is an uppercase or lowercase SHA-256 hexadecimal digest.</summary>
    internal static bool IsDigest(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);

    private static void ValidateMembership(IReadOnlyList<PfObs1CorpusSample> corpus)
    {
        ArgumentNullException.ThrowIfNull(corpus);
        if (corpus.Count != SampleCount ||
            corpus.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != SampleCount ||
            Enum.GetValues<PfObs1Partition>().Any(partition =>
                corpus.Count(item => item.Partition == partition) != 12))
        {
            throw new InvalidOperationException("pfobs1.corpus.membership_invalid");
        }
    }
}

/// <summary>Coordinates the functional laboratory path and persists only sanitised authenticated evidence.</summary>
internal static class PfObs1PilotCampaign
{
    internal const string MarkerVariable = "DBNOTIFIER_PF_OBS1_TEST_ONLY";
    internal const string ConnectionVariable = "DBNOTIFIER_PF_OBS1_CONNECTION";
    internal const string OutputVariable = "DBNOTIFIER_PF_OBS1_OUTPUT";
    internal const string ImageDigestVariable = "DBNOTIFIER_PF_OBS1_IMAGE_DIGEST";
    internal const string CertificateDigestVariable = "DBNOTIFIER_PF_OBS1_CERTIFICATE_DIGEST";
    internal const string Marker = "pf-obs-1-postgresql-observer-local-test";
    internal const string CellId = "OBS-PILOT-PG16-LOCAL-001";
    internal const string SchemaVersion = "pf-obs-1.evidence.v1";
    private static readonly Guid AgentId = Guid.Parse("bf010000-0000-4000-8000-000000000001");
    private static readonly Guid InstanceId = Guid.Parse("bf020000-0000-4000-8000-000000000001");

    /// <summary>Runs live collection, Agent/Server/MOD-12 ingestion, calibration and holdout.</summary>
    /// <param name="connectionString">Ephemeral TLS laboratory connection string.</param>
    /// <param name="imageDigest">Exact official image digest.</param>
    /// <param name="certificateDigest">Public laboratory TLS certificate digest.</param>
    /// <param name="temporaryRoot">Caller-owned exact campaign root.</param>
    /// <param name="cancellationToken">Whole-campaign cancellation.</param>
    /// <returns>Complete non-authorising content-addressed pilot evidence.</returns>
    internal static async Task<PfObs1PilotEvidence> RunAsync(
        string connectionString,
        string imageDigest,
        string certificateDigest,
        string temporaryRoot,
        CancellationToken cancellationToken)
    {
        if (!PfObs1CorpusEvaluation.IsDigest(imageDigest) ||
            !PfObs1CorpusEvaluation.IsDigest(certificateDigest))
        {
            throw new InvalidOperationException("pfobs1.provenance.invalid");
        }

        PfObs1PostgreSqlCollector collector = new(connectionString);
        List<(double DurationMilliseconds, bool ExpectedDegraded)> measurements =
            new(PfObs1CorpusEvaluation.SampleCount);
        for (int index = 0; index < PfObs1CorpusEvaluation.SampleCount; index++)
        {
            bool delayed = index % 2 == 1;
            TimeSpan duration = await collector.MeasureAsync(delayed, cancellationToken).ConfigureAwait(false);
            measurements.Add((duration.TotalMilliseconds, delayed));
        }

        IReadOnlyList<PfObs1CorpusSample> corpus = PfObs1CorpusEvaluation.Partition(measurements);
        (double threshold, string policyDigest) = PfObs1CorpusEvaluation.Freeze(corpus);
        PfObs1HoldoutResult holdout = PfObs1CorpusEvaluation.Evaluate(corpus, threshold, policyDigest);
        if (holdout.Accuracy < 1d)
        {
            throw new InvalidOperationException("pfobs1.holdout.slo_failed");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        O2AFixedTimeProvider clock = new(now);
        string o2Root = Path.Combine(temporaryRoot, "o2");
        using O2ACanonicalObservationPipeline pipeline =
            await O2ACanonicalObservationPipeline.CreateAsync(
                o2Root,
                AgentId,
                InstanceId,
                clock,
                cancellationToken);
        ObservationBatchIngestor server = new(pipeline, clock);
        O2AAgentOutboxStore outbox = new();
        double latestDuration = measurements[^1].DurationMilliseconds;
        outbox.Add(new ObservationSyncMessage(
            Guid.NewGuid(),
            1,
            1,
            Guid.NewGuid(),
            InstanceId,
            AgentId,
            "postgresql",
            "16",
            HealthStatus.Degraded.ToString(),
            "npgsql-read-only-laboratory",
            EvidenceLevel.ProviderAuthenticated.ToString(),
            1,
            now,
            checked((long)Math.Ceiling(latestDuration)),
            null,
            null,
            ["synthetic-laboratory-only"]));
        AgentOutboxDispatchResult dispatch = await new AgentOutboxDispatchRunner(
                AgentId,
                outbox,
                new O2ALoopbackObservationTransport(server),
                clock)
            .RunOnceAsync(1, cancellationToken)
            .ConfigureAwait(false);
        ObserverAnalysisReport report = pipeline.PublishedReports.SingleOrDefault() ??
            throw new InvalidOperationException("pfobs1.mod12.report_missing");
        if (dispatch.AcknowledgedCount != 1 ||
            outbox.PendingCount != 0 ||
            !report.IsComplete ||
            report.IsAuthorising ||
            report.Capabilities.ActivationState != ObserverActivationState.None)
        {
            throw new InvalidOperationException("pfobs1.pipeline.incomplete");
        }

        string o2Digest = pipeline.Outcomes.Single().EnvelopeDigest;
        string reportDigest = PfObs1CorpusEvaluation.Digest(JsonSerializer.Serialize(report));
        string corpusDigest = PfObs1CorpusEvaluation.ManifestDigest(corpus);
        string[] validationCodes =
        [
            "pfobs1.postgresql.tls_verified",
            "pfobs1.agent_server.accepted",
            "pfobs1.mod12.complete_non_authorising",
            "pfobs1.corpus.disjoint",
            "pfobs1.holdout.accepted",
            "pfobs1.activation.none",
        ];
        PfObs1PilotEvidence unsigned = new(
            SchemaVersion,
            CellId,
            ObserverActivationState.None.ToString(),
            true,
            false,
            true,
            false,
            "postgresql",
            "16",
            imageDigest,
            certificateDigest,
            now,
            latestDuration,
            HealthStatus.Degraded.ToString(),
            o2Digest,
            reportDigest,
            corpusDigest,
            corpus.Count,
            holdout,
            validationCodes,
            "Bruno",
            string.Empty);
        return unsigned with
        {
            EvidenceDigest = PfObs1CorpusEvaluation.Digest(JsonSerializer.Serialize(unsigned)),
        };
    }
}

/// <summary>Exposes the exact PF-OBS-1 campaign only through explicit environment opt-in.</summary>
public sealed class PfObs1PilotEntryPointTests
{
    private static readonly TimeSpan CampaignDeadline = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions EvidenceJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Runs the functional pilot only when the exact marker and temporary destination are present.</summary>
    [Fact]
    [Trait("Category", "PfObs1Pilot")]
    public async Task RunFunctionalPostgreSqlObserverPilotWhenExplicitlyAuthorised()
    {
        string? marker = Environment.GetEnvironmentVariable(PfObs1PilotCampaign.MarkerVariable);
        if (marker is null)
        {
            return;
        }
        Assert.Equal(PfObs1PilotCampaign.Marker, marker);
        string connection = Required(PfObs1PilotCampaign.ConnectionVariable, 4096);
        string imageDigest = Required(PfObs1PilotCampaign.ImageDigestVariable, 64);
        string certificateDigest = Required(PfObs1PilotCampaign.CertificateDigestVariable, 64);
        string output = ValidateOutput(Required(PfObs1PilotCampaign.OutputVariable, 1024));
        string root = Path.GetDirectoryName(output)!;
        using CancellationTokenSource deadline = new(CampaignDeadline);
        PfObs1PilotEvidence evidence = await PfObs1PilotCampaign.RunAsync(
            connection,
            imageDigest,
            certificateDigest,
            root,
            deadline.Token);
        await WriteAtomicAsync(output, evidence);
        Assert.Equal("None", evidence.ActivationState);
        Assert.False(evidence.IsAuthorising);
        Assert.Equal(1d, evidence.Holdout.Accuracy);
    }

    private static string Required(string variable, int maximumLength)
    {
        string? value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > maximumLength ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("pfobs1.environment.invalid");
        }
        return value;
    }

    private static string ValidateOutput(string candidate)
    {
        string full = Path.GetFullPath(candidate);
        string temporary = Path.GetFullPath(Path.GetTempPath());
        string? directory = Path.GetDirectoryName(full);
        if (directory is null ||
            !directory.StartsWith(temporary, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(directory).StartsWith("DBNotifier-PF-OBS-1-", StringComparison.Ordinal) ||
            Path.GetFileName(full) != "pf-obs-1-evidence.json")
        {
            throw new InvalidOperationException("pfobs1.output.refused");
        }
        return full;
    }

    private static async Task WriteAtomicAsync(string output, PfObs1PilotEvidence evidence)
    {
        string temporary = output + ".tmp";
        try
        {
            await File.WriteAllTextAsync(
                temporary,
                JsonSerializer.Serialize(evidence, EvidenceJsonOptions),
                new UTF8Encoding(false));
            File.Move(temporary, output, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
