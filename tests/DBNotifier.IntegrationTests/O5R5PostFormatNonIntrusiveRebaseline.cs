// Module purpose: Repeats the exact V3 prefix after host reformatting without adding instrumentation inside the measured process.
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DBNotifier.IntegrationTests;

/// <summary>Defines the immutable D6 identity, exact prefix and conditional external-observation gate.</summary>
internal static class O5R5D6Protocol
{
    internal const string Version = "pfobs1-d6-post-format-non-intrusive-rebaseline-1.0.0";
    internal const string Digest = "5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845";
    internal const string V3Digest = "60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A";
    internal const int Runs = 2;
    internal const int ExpectedSampleCount = 158;
    internal const int ExpectedSummaryCount = 4;
    internal const long WorkingSetLimitBytes = 786_432;
    internal const string CanonicalStatement =
        "pfobs1-d6-post-format-non-intrusive-rebaseline|1.0.0|" +
        "v3=60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A|" +
        "runs=2|arm=unobserved|precondition=v3-exact|" +
        "prefix=first-byte-cold-5-30,first-byte-warm-5-30,idle-cold-5-30," +
        "idle-warm-5-30,cancellation-cold-5-13|summaries=original-four|" +
        "intraprocess-extra-captures=0|working-set-limit=786432|samples=all|" +
        "forced-gc=v3-two-existing-only|activation=none|" +
        "conditional=external-after-two-reproductions";

    /// <summary>Calculates the SHA-256 identity of the exact preregistered statement.</summary>
    /// <returns>An upper-case hexadecimal SHA-256 digest.</returns>
    internal static string CalculateDigest() =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalStatement)));
}

/// <summary>Reports one complete or fail-closed unobserved D6 prefix without granting activation authority.</summary>
/// <param name="ProtocolVersion">Frozen D6 protocol version.</param>
/// <param name="ProtocolDigest">Frozen D6 protocol digest.</param>
/// <param name="V3ProtocolDigest">Unchanged physical V3 protocol digest.</param>
/// <param name="ActivationState">MOD-12 activation state, which remains None.</param>
/// <param name="Run">One-based fresh-process run.</param>
/// <param name="StartedAtUtc">Instant immediately before the first retained sample.</param>
/// <param name="CompletedAtUtc">Instant after completion or the first fail-closed stop.</param>
/// <param name="Environment">Sanitised physical environment declaration.</param>
/// <param name="ExpectedSampleCount">Exact preregistered prefix size.</param>
/// <param name="CompletedSampleCount">Number of complete retained samples.</param>
/// <param name="Code">Stable sanitised disposition code.</param>
/// <param name="Complete">Whether the exact prefix and four summaries completed.</param>
/// <param name="WorkingSetExcessReproduced">Whether Cancellation/Cold exceeded its unchanged limit.</param>
/// <param name="Samples">Complete unchanged V3 samples in original order.</param>
/// <param name="Summaries">Original V3 summaries reached before the prefix stop.</param>
/// <param name="Failures">Bounded unchanged V3 threshold failures.</param>
internal sealed record O5R5D6Report(
    string ProtocolVersion,
    string ProtocolDigest,
    string V3ProtocolDigest,
    string ActivationState,
    int Run,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    O5R5PhysicalEnvironment Environment,
    int ExpectedSampleCount,
    int CompletedSampleCount,
    string Code,
    bool Complete,
    bool WorkingSetExcessReproduced,
    IReadOnlyList<O5R5MeasurementSample> Samples,
    IReadOnlyList<O5R5MeasurementSummary> Summaries,
    IReadOnlyList<O5R5ThresholdFailure> Failures)
{
    /// <summary>Gets a value that is always false because D6 evidence cannot authorise activation.</summary>
    internal static bool IsAuthorising => false;
}

/// <summary>Executes only the first 158 V3 scenarios and their four original summaries.</summary>
internal sealed class O5R5D6Runner
{
    private readonly O5R5MeasurementReadinessRunner runner;
    private readonly O5R5PhysicalCampaignDriver driver;

    /// <summary>Initialises D6 over the unchanged physical source after all process-boundary checks pass.</summary>
    /// <param name="source">Existing V3 measurement source.</param>
    internal O5R5D6Runner(IO5R5MeasurementSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        if (protocol.Digest != O5R5D6Protocol.V3Digest ||
            O5R5D6Protocol.CalculateDigest() != O5R5D6Protocol.Digest)
        {
            throw new InvalidOperationException("o5r5d6.protocol_mismatch");
        }

        runner = new O5R5MeasurementReadinessRunner(
            O5R5MeasurementReadinessRunner.Marker,
            protocol,
            source);
        driver = new O5R5PhysicalCampaignDriver(protocol, source);
    }

    /// <summary>Returns the exact original V3 prefix without reordering or substitution.</summary>
    /// <returns>The first 158 physical scenarios in their original order.</returns>
    internal IReadOnlyList<O5R5MeasurementScenario> SelectExactPrefix() =>
        driver.CreateScenarios().Take(O5R5D6Protocol.ExpectedSampleCount).ToArray();

    /// <summary>Runs one fresh-process unobserved prefix and retains every complete result.</summary>
    /// <param name="run">One-based run index.</param>
    /// <param name="environment">Sanitised current environment declaration.</param>
    /// <param name="cancellationToken">Bounded process deadline.</param>
    /// <returns>A complete or fail-closed non-authorising report.</returns>
    internal async Task<O5R5D6Report> RunAsync(
        int run,
        O5R5PhysicalEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (run is < 1 or > O5R5D6Protocol.Runs)
        {
            throw new ArgumentOutOfRangeException(nameof(run));
        }
        ArgumentNullException.ThrowIfNull(environment);

        await driver.PreconditionAsync(cancellationToken).ConfigureAwait(false);
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
        List<O5R5MeasurementSample> samples = new(O5R5D6Protocol.ExpectedSampleCount);
        List<O5R5MeasurementSummary> summaries = new(O5R5D6Protocol.ExpectedSummaryCount);
        List<O5R5ThresholdFailure> failures = new(6);

        foreach (O5R5MeasurementScenario scenario in SelectExactPrefix())
        {
            O5R5MeasurementResult result = await runner
                .RunAsync(scenario, cancellationToken)
                .ConfigureAwait(false);
            failures.AddRange(result.Failures);
            if (result.Disposition != O5R5MeasurementDisposition.Accepted ||
                result.Sample is null)
            {
                return Report(
                    run,
                    startedAtUtc,
                    environment,
                    result.Code,
                    samples,
                    summaries,
                    failures);
            }

            samples.Add(result.Sample);
            if (!result.Sample.Passed)
            {
                return Report(
                    run,
                    startedAtUtc,
                    environment,
                    result.Code,
                    samples,
                    summaries,
                    failures);
            }

            if (!scenario.IsWarmUp &&
                scenario.Repetition == O5R5MeasurementProtocol.MeasuredRepetitions)
            {
                int batchSize =
                    O5R5MeasurementProtocol.WarmUpRepetitions +
                    O5R5MeasurementProtocol.MeasuredRepetitions;
                O5R5MeasurementSummary summary = runner.Summarise(
                    samples.TakeLast(batchSize).ToArray());
                summaries.Add(summary);
                failures.AddRange(summary.Failures);
                if (!summary.Passed)
                {
                    return Report(
                        run,
                        startedAtUtc,
                        environment,
                        summary.Failures[0].Code,
                        samples,
                        summaries,
                        failures);
                }
            }
        }

        return Report(
            run,
            startedAtUtc,
            environment,
            "o5r5d6.prefix.complete",
            samples,
            summaries,
            failures);
    }

    /// <summary>Creates one immutable report and classifies only the unchanged working-set threshold.</summary>
    /// <param name="run">One-based fresh-process run.</param>
    /// <param name="startedAtUtc">Instant immediately before the first retained sample.</param>
    /// <param name="environment">Sanitised physical environment.</param>
    /// <param name="code">Stable result code.</param>
    /// <param name="samples">Complete retained V3 samples.</param>
    /// <param name="summaries">Original summaries reached before stopping.</param>
    /// <param name="failures">Unchanged V3 failures reached before stopping.</param>
    /// <returns>A non-authorising D6 report.</returns>
    private static O5R5D6Report Report(
        int run,
        DateTimeOffset startedAtUtc,
        O5R5PhysicalEnvironment environment,
        string code,
        List<O5R5MeasurementSample> samples,
        List<O5R5MeasurementSummary> summaries,
        List<O5R5ThresholdFailure> failures)
    {
        bool reproduced = failures.Any(
            failure =>
                failure.Phase == O5R5MeasurementPhase.Cancellation &&
                failure.Temperature == O5R5Temperature.Cold &&
                failure.Metric == O5R5ThresholdMetric.WorkingSetPeak &&
                failure.Observed > O5R5D6Protocol.WorkingSetLimitBytes);
        bool complete =
            samples.Count == O5R5D6Protocol.ExpectedSampleCount &&
            summaries.Count == O5R5D6Protocol.ExpectedSummaryCount;
        return new O5R5D6Report(
            O5R5D6Protocol.Version,
            O5R5D6Protocol.Digest,
            O5R5D6Protocol.V3Digest,
            "None",
            run,
            startedAtUtc,
            DateTimeOffset.UtcNow,
            environment,
            O5R5D6Protocol.ExpectedSampleCount,
            samples.Count,
            code,
            complete,
            reproduced,
            samples.ToArray(),
            summaries.ToArray(),
            failures.ToArray());
    }
}

/// <summary>Writes complete D6 reports only beneath one direct runner-owned temporary child.</summary>
internal static class O5R5D6EvidenceWriter
{
    private const string RootPrefix = "DBNotifier-PF-OBS-1-D6-";
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Validates the exact direct temporary destination for one unobserved run.</summary>
    /// <param name="path">Candidate output path.</param>
    /// <param name="run">One-based run index.</param>
    /// <returns>The canonical validated destination.</returns>
    internal static string ValidateDestination(string? path, int run)
    {
        if (string.IsNullOrWhiteSpace(path) || run is < 1 or > O5R5D6Protocol.Runs)
        {
            throw new InvalidOperationException("o5r5d6.output.required");
        }

        string destination = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(destination);
        string temporaryRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        string expectedName = $"unobserved-run-{run}.json";
        if (directory is null ||
            !string.Equals(
                Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(directory)),
                temporaryRoot,
                StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(directory).StartsWith(RootPrefix, StringComparison.Ordinal) ||
            !string.Equals(Path.GetFileName(destination), expectedName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o5r5d6.output.path_refused");
        }

        return destination;
    }

    /// <summary>Durably writes one complete report through a write-through sibling and atomic move.</summary>
    /// <param name="destination">Validated destination.</param>
    /// <param name="report">Complete D6 report.</param>
    /// <param name="cancellationToken">Cancellation checked before publication.</param>
    internal static async Task WriteAsync(
        string destination,
        O5R5D6Report report,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        string canonical = ValidateDestination(destination, report.Run);
        Directory.CreateDirectory(Path.GetDirectoryName(canonical)!);
        string temporary = canonical + ".tmp";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(report, Options);
        try
        {
            await using (FileStream stream = new(
                             temporary,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             16_384,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, canonical, overwrite: false);
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

/// <summary>Exposes the sole marker-gated D6 fresh-process entrypoint.</summary>
public static class O5R5D6Process
{
    internal const string ActivationMarker = "pf-obs-1-d6-post-format-non-intrusive-test-only";
    private static readonly TimeSpan ProcessDeadline = TimeSpan.FromMinutes(5);

    /// <summary>Validates exact arguments, runs one unobserved prefix and writes sanitised evidence.</summary>
    /// <param name="args">Activation, destination, run and sanitised environment arguments.</param>
    /// <returns>Zero after a complete diagnostic, two for invalid activation or three for failure.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 10 ||
            args[0] != "--activation" ||
            args[1] != ActivationMarker ||
            args[2] != "--output" ||
            args[4] != "--run" ||
            !int.TryParse(args[5], NumberStyles.None, CultureInfo.InvariantCulture, out int run) ||
            args[6] != "--sdk" ||
            args[8] != "--installed-memory" ||
            !long.TryParse(
                args[9],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long installedMemory) ||
            installedMemory < 1)
        {
            Console.Error.WriteLine("o5r5d6.activation_invalid");
            return 2;
        }

        try
        {
            string destination = O5R5D6EvidenceWriter.ValidateDestination(args[3], run);
            string sdk = RequiredBoundedText(args[7]);
            O5R5PhysicalEnvironment environment = new(
                RuntimeInformation.OSDescription,
                RuntimeInformation.OSArchitecture.ToString(),
                RuntimeInformation.ProcessArchitecture.ToString(),
                RuntimeInformation.FrameworkDescription,
                sdk,
                Environment.ProcessorCount,
                installedMemory,
                Stopwatch.Frequency);
            using O5R5DotNetMeasurementSource source = new();
            O5R5D6Runner runner = new(source);
            using CancellationTokenSource deadline = new(ProcessDeadline);
            O5R5D6Report report = await runner
                .RunAsync(run, environment, deadline.Token)
                .ConfigureAwait(false);
            await O5R5D6EvidenceWriter
                .WriteAsync(destination, report, CancellationToken.None)
                .ConfigureAwait(false);
            if (!report.Complete ||
                report.CompletedSampleCount != O5R5D6Protocol.ExpectedSampleCount ||
                report.Summaries.Count != O5R5D6Protocol.ExpectedSummaryCount)
            {
                Console.Error.WriteLine(report.Code);
                return 3;
            }

            Console.WriteLine("o5r5d6.prefix.complete");
            return 0;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"o5r5d6.failed:{exception.GetType().Name}");
            return 3;
        }
    }

    /// <summary>Validates one bounded non-secret SDK declaration.</summary>
    /// <param name="value">Candidate SDK version.</param>
    /// <returns>The exact validated value.</returns>
    private static string RequiredBoundedText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > 32 ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o5r5d6.environment.sdk_required");
        }

        return value;
    }
}
