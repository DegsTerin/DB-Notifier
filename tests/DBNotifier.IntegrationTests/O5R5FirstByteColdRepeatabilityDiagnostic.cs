// Module purpose: Executes the frozen D7 FirstByte/Cold group in an isolated child and validates non-authorising repeatability evidence.
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DBNotifier.IntegrationTests;

/// <summary>Identifies the two frozen D7 observation arms.</summary>
internal enum O5R5D7Arm
{
    Unobserved = 1,
    External = 2,
}

/// <summary>Defines the immutable D7 identity, exact group and conditional external gate.</summary>
internal static class O5R5D7Protocol
{
    internal const string Version = "pfobs1-d7-first-byte-cold-repeatability-diagnostic-1.0.0";
    internal const string Digest = "7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF";
    internal const string V3Digest = "60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A";
    internal const string D6Digest = "5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845";
    internal const int UnobservedRuns = 3;
    internal const int ExternalRuns = 2;
    internal const int ExternalFailureGate = 2;
    internal const int ExpectedSampleCount = 35;
    internal const int ExpectedSummaryCount = 1;
    internal const double CoefficientLimit = 0.20d;
    internal const string CanonicalStatement =
        "pfobs1-d7-first-byte-cold-repeatability-diagnostic|1.0.0|" +
        "v3=60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A|" +
        "d6=5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845|" +
        "unobserved-runs=3|external-runs=2|external-gate=unobserved-failures>=2|" +
        "precondition=v3-exact|scope=first-byte-cold-5-30-summary|" +
        "repeatability=five-fixed-sequential-groups-six-samples-median-cv|" +
        "coefficient-limit=0.20|samples=all|intraprocess-extra-captures=0|" +
        "external=supervisor-before-after-os-counters-only|" +
        "forced-gc=v3-two-existing-only|activation=none";

    /// <summary>Calculates the SHA-256 identity of the exact D7 statement.</summary>
    /// <returns>An upper-case hexadecimal SHA-256 digest.</returns>
    internal static string CalculateDigest() =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalStatement)));

    /// <summary>Returns the exact number of authorised runs for one arm.</summary>
    /// <param name="arm">Frozen observation arm.</param>
    /// <returns>Three for unobserved or two for external.</returns>
    internal static int RunsFor(O5R5D7Arm arm) =>
        arm switch
        {
            O5R5D7Arm.Unobserved => UnobservedRuns,
            O5R5D7Arm.External => ExternalRuns,
            _ => throw new ArgumentOutOfRangeException(nameof(arm)),
        };
}

/// <summary>Reports one complete or fail-closed measured child without external D7 counters.</summary>
/// <param name="ProtocolVersion">Frozen D7 version.</param>
/// <param name="ProtocolDigest">Frozen D7 digest.</param>
/// <param name="V3ProtocolDigest">Unchanged V3 digest.</param>
/// <param name="D6ProtocolDigest">Referenced D6 digest.</param>
/// <param name="ActivationState">MOD-12 activation state, which remains None.</param>
/// <param name="Arm">Unobserved or external arm selected by the supervisor.</param>
/// <param name="Run">One-based arm run.</param>
/// <param name="StartedAtUtc">Instant immediately before the first retained sample.</param>
/// <param name="CompletedAtUtc">Instant after the summary or fail-closed stop.</param>
/// <param name="Environment">Sanitised physical environment declaration.</param>
/// <param name="ExpectedSampleCount">Frozen group size.</param>
/// <param name="CompletedSampleCount">Number of complete retained samples.</param>
/// <param name="ExpectedSummaryCount">Frozen original-summary count.</param>
/// <param name="CompletedSummaryCount">Number of original summaries retained.</param>
/// <param name="Code">Stable bounded disposition code.</param>
/// <param name="Complete">Whether all 35 samples and the original summary exist.</param>
/// <param name="Samples">Unchanged V3 samples in original order.</param>
/// <param name="Summary">Original V3 summary, or null after an earlier stop.</param>
/// <param name="Failures">Unchanged bounded V3 failures.</param>
internal sealed record O5R5D7MeasuredReport(
    string ProtocolVersion,
    string ProtocolDigest,
    string V3ProtocolDigest,
    string D6ProtocolDigest,
    string ActivationState,
    O5R5D7Arm Arm,
    int Run,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    O5R5PhysicalEnvironment Environment,
    int ExpectedSampleCount,
    int CompletedSampleCount,
    int ExpectedSummaryCount,
    int CompletedSummaryCount,
    string Code,
    bool Complete,
    IReadOnlyList<O5R5MeasurementSample> Samples,
    O5R5MeasurementSummary? Summary,
    IReadOnlyList<O5R5ThresholdFailure> Failures);

/// <summary>Reports the independent fixed-group integrity recomputation.</summary>
/// <param name="GroupMediansMilliseconds">Five fixed sequential group medians.</param>
/// <param name="CoefficientOfVariation">Population coefficient over the five medians.</param>
/// <param name="ExactSummaryMatch">Whether the binary double equals the original V3 summary.</param>
internal sealed record O5R5D7IntegrityResult(
    IReadOnlyList<double> GroupMediansMilliseconds,
    double CoefficientOfVariation,
    bool ExactSummaryMatch);

/// <summary>Reports one supervisor-owned external before/after observation pair.</summary>
/// <param name="Before">System snapshot immediately before child launch.</param>
/// <param name="After">System snapshot immediately after child exit.</param>
/// <param name="Process">Process counters read after child exit from the retained handle.</param>
internal sealed record O5R5D7ExternalObservation(
    O5R5D7SystemSnapshot Before,
    O5R5D7SystemSnapshot After,
    O5R5D7ProcessSnapshot Process);

/// <summary>Reports sanitised system counters without host identity.</summary>
/// <param name="IdleTime100Nanoseconds">Cumulative system idle time.</param>
/// <param name="KernelTime100Nanoseconds">Cumulative system kernel time.</param>
/// <param name="UserTime100Nanoseconds">Cumulative system user time.</param>
/// <param name="MemoryLoadPercent">Current physical-memory load.</param>
/// <param name="AvailablePhysicalMemoryBytes">Current available physical memory.</param>
/// <param name="Unavailable">Allow-listed counters unavailable from the operating system.</param>
internal sealed record O5R5D7SystemSnapshot(
    ulong? IdleTime100Nanoseconds,
    ulong? KernelTime100Nanoseconds,
    ulong? UserTime100Nanoseconds,
    uint? MemoryLoadPercent,
    ulong? AvailablePhysicalMemoryBytes,
    IReadOnlyList<string> Unavailable);

/// <summary>Reports sanitised measured-process counters without retaining its identifier.</summary>
/// <param name="UserCpuTicks">Final user CPU time in TimeSpan ticks.</param>
/// <param name="KernelCpuTicks">Final kernel CPU time in TimeSpan ticks.</param>
/// <param name="CycleCount">Final processor-cycle count.</param>
/// <param name="PeakWorkingSetBytes">Final peak working set.</param>
/// <param name="PageFaultCount">Final page-fault count.</param>
/// <param name="HandleCount">Final handle count when available.</param>
/// <param name="ThreadCount">Final thread count when available.</param>
/// <param name="Unavailable">Allow-listed counters unavailable after process exit.</param>
internal sealed record O5R5D7ProcessSnapshot(
    long? UserCpuTicks,
    long? KernelCpuTicks,
    ulong? CycleCount,
    long? PeakWorkingSetBytes,
    uint? PageFaultCount,
    int? HandleCount,
    int? ThreadCount,
    IReadOnlyList<string> Unavailable);

/// <summary>Reports one final supervisor-validated, non-authorising D7 run.</summary>
/// <param name="ProtocolVersion">Frozen D7 version.</param>
/// <param name="ProtocolDigest">Frozen D7 digest.</param>
/// <param name="V3ProtocolDigest">Unchanged V3 digest.</param>
/// <param name="D6ProtocolDigest">Referenced D6 digest.</param>
/// <param name="ActivationState">MOD-12 activation state, which remains None.</param>
/// <param name="Arm">Unobserved or external arm.</param>
/// <param name="Run">One-based arm run.</param>
/// <param name="Code">Stable supervisor disposition.</param>
/// <param name="Complete">Whether measured and integrity evidence is complete.</param>
/// <param name="SummaryFailed">Whether only the exact repeatability summary failed.</param>
/// <param name="Measured">Complete or fail-closed child evidence.</param>
/// <param name="Integrity">Independent fixed-group recomputation, when available.</param>
/// <param name="ExternalObservation">External snapshots only for the external arm.</param>
internal sealed record O5R5D7FinalReport(
    string ProtocolVersion,
    string ProtocolDigest,
    string V3ProtocolDigest,
    string D6ProtocolDigest,
    string ActivationState,
    O5R5D7Arm Arm,
    int Run,
    string Code,
    bool Complete,
    bool SummaryFailed,
    O5R5D7MeasuredReport? Measured,
    O5R5D7IntegrityResult? Integrity,
    O5R5D7ExternalObservation? ExternalObservation)
{
    /// <summary>Gets a value that is always false because D7 cannot authorise activation.</summary>
    internal static bool IsAuthorising => false;
}

/// <summary>Executes the exact first V3 group and its original summary without D7 instrumentation.</summary>
internal sealed class O5R5D7MeasuredRunner
{
    private readonly O5R5MeasurementReadinessRunner runner;
    private readonly O5R5PhysicalCampaignDriver driver;

    /// <summary>Initialises the measured child over the unchanged V3 source.</summary>
    /// <param name="source">Existing physical measurement source.</param>
    internal O5R5D7MeasuredRunner(IO5R5MeasurementSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        if (protocol.Digest != O5R5D7Protocol.V3Digest ||
            O5R5D6Protocol.Digest != O5R5D7Protocol.D6Digest ||
            O5R5D7Protocol.CalculateDigest() != O5R5D7Protocol.Digest ||
            O5R5MeasurementProtocol.MaximumCoefficientOfVariation !=
                O5R5D7Protocol.CoefficientLimit)
        {
            throw new InvalidOperationException("o5r5d7.protocol_mismatch");
        }

        runner = new O5R5MeasurementReadinessRunner(
            O5R5MeasurementReadinessRunner.Marker,
            protocol,
            source);
        driver = new O5R5PhysicalCampaignDriver(protocol, source);
    }

    /// <summary>Returns the exact first 35 V3 scenarios without reordering.</summary>
    /// <returns>Five warm-ups and thirty measured FirstByte/Cold scenarios.</returns>
    internal IReadOnlyList<O5R5MeasurementScenario> SelectExactGroup() =>
        driver.CreateScenarios().Take(O5R5D7Protocol.ExpectedSampleCount).ToArray();

    /// <summary>Executes one measured child and retains the original V3 result.</summary>
    /// <param name="arm">Supervisor-selected arm.</param>
    /// <param name="run">One-based arm run.</param>
    /// <param name="environment">Sanitised current environment declaration.</param>
    /// <param name="cancellationToken">Bounded child deadline.</param>
    /// <returns>A complete group report or the first fail-closed result.</returns>
    internal async Task<O5R5D7MeasuredReport> RunAsync(
        O5R5D7Arm arm,
        int run,
        O5R5PhysicalEnvironment environment,
        CancellationToken cancellationToken)
    {
        ValidateRun(arm, run);
        ArgumentNullException.ThrowIfNull(environment);
        await driver.PreconditionAsync(cancellationToken).ConfigureAwait(false);

        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
        List<O5R5MeasurementSample> samples = new(O5R5D7Protocol.ExpectedSampleCount);
        List<O5R5ThresholdFailure> failures = new(4);
        foreach (O5R5MeasurementScenario scenario in SelectExactGroup())
        {
            O5R5MeasurementResult result = await runner
                .RunAsync(scenario, cancellationToken)
                .ConfigureAwait(false);
            failures.AddRange(result.Failures);
            if (result.Disposition != O5R5MeasurementDisposition.Accepted ||
                result.Sample is null)
            {
                return Report(arm, run, startedAtUtc, environment, result.Code, samples, null, failures);
            }

            samples.Add(result.Sample);
            if (!result.Sample.Passed)
            {
                return Report(arm, run, startedAtUtc, environment, result.Code, samples, null, failures);
            }
        }

        O5R5MeasurementSummary summary = runner.Summarise(samples);
        failures.AddRange(summary.Failures);
        string code = summary.Passed
            ? "o5r5d7.group.complete"
            : summary.Failures.Count > 0
                ? summary.Failures[0].Code
                : summary.Code;
        return Report(arm, run, startedAtUtc, environment, code, samples, summary, failures);
    }

    /// <summary>Validates the exact arm run range.</summary>
    /// <param name="arm">Frozen observation arm.</param>
    /// <param name="run">One-based run.</param>
    internal static void ValidateRun(O5R5D7Arm arm, int run)
    {
        if (!Enum.IsDefined(arm) || run < 1 || run > O5R5D7Protocol.RunsFor(arm))
        {
            throw new ArgumentOutOfRangeException(nameof(run));
        }
    }

    /// <summary>Creates one immutable child report without external D7 metrics.</summary>
    /// <param name="arm">Supervisor-selected observation arm.</param>
    /// <param name="run">One-based arm run.</param>
    /// <param name="startedAtUtc">Instant immediately before the first retained sample.</param>
    /// <param name="environment">Sanitised physical environment declaration.</param>
    /// <param name="code">Stable bounded outcome code.</param>
    /// <param name="samples">Retained original V3 samples.</param>
    /// <param name="summary">Original V3 summary when all samples completed.</param>
    /// <param name="failures">Retained original V3 failures.</param>
    /// <returns>An immutable measured child report.</returns>
    private static O5R5D7MeasuredReport Report(
        O5R5D7Arm arm,
        int run,
        DateTimeOffset startedAtUtc,
        O5R5PhysicalEnvironment environment,
        string code,
        List<O5R5MeasurementSample> samples,
        O5R5MeasurementSummary? summary,
        List<O5R5ThresholdFailure> failures) =>
        new(
            O5R5D7Protocol.Version,
            O5R5D7Protocol.Digest,
            O5R5D7Protocol.V3Digest,
            O5R5D7Protocol.D6Digest,
            "None",
            arm,
            run,
            startedAtUtc,
            DateTimeOffset.UtcNow,
            environment,
            O5R5D7Protocol.ExpectedSampleCount,
            samples.Count,
            O5R5D7Protocol.ExpectedSummaryCount,
            summary is null ? 0 : 1,
            code,
            samples.Count == O5R5D7Protocol.ExpectedSampleCount && summary is not null,
            samples.ToArray(),
            summary,
            failures.ToArray());
}

/// <summary>Recomputes fixed-group integrity and classifies complete arm outcomes.</summary>
internal static class O5R5D7Integrity
{
    /// <summary>Recomputes the five group medians and coefficient from complete measured evidence.</summary>
    /// <param name="report">Complete measured child report.</param>
    /// <returns>Independent integrity result with an exact binary summary comparison.</returns>
    internal static O5R5D7IntegrityResult Recompute(O5R5D7MeasuredReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (!report.Complete ||
            report.Summary is null ||
            report.Samples.Count != O5R5D7Protocol.ExpectedSampleCount)
        {
            throw new InvalidOperationException("o5r5d7.integrity.incomplete");
        }

        double[] values = report.Samples
            .Where(sample => !sample.IsWarmUp)
            .Select(
                sample => sample.RepeatabilityWindowMilliseconds ??
                    throw new InvalidOperationException("o5r5d7.integrity.metric_missing"))
            .ToArray();
        (double[] medians, double coefficient) = RecomputeValues(values);
        bool exact =
            BitConverter.DoubleToInt64Bits(coefficient) ==
            BitConverter.DoubleToInt64Bits(
                report.Summary.RepeatabilityElapsed.CoefficientOfVariation);
        return new O5R5D7IntegrityResult(medians, coefficient, exact);
    }

    /// <summary>Calculates the frozen five-by-six median coefficient for structural tests and reports.</summary>
    /// <param name="values">Exactly thirty finite non-negative values in repetition order.</param>
    /// <returns>Five medians and their population coefficient of variation.</returns>
    internal static (double[] Medians, double Coefficient) RecomputeValues(double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length != O5R5MeasurementProtocol.MeasuredRepetitions ||
            values.Any(value => !double.IsFinite(value) || value < 0d))
        {
            throw new InvalidOperationException("o5r5d7.integrity.membership_invalid");
        }

        double[] medians = values
            .Chunk(O5R5MeasurementProtocol.SamplesPerRepeatabilityGroup)
            .Select(
                group =>
                {
                    double[] ordered = [.. group.Order()];
                    int upper = ordered.Length / 2;
                    return (ordered[upper - 1] + ordered[upper]) / 2d;
                })
            .ToArray();
        double[] sorted = [.. medians.Order()];
        double mean = sorted.Average();
        double variance = sorted.Select(value => Math.Pow(value - mean, 2d)).Average();
        double deviation = Math.Sqrt(variance);
        return (medians, mean == 0d ? 0d : deviation / mean);
    }

    /// <summary>Classifies exactly three complete unobserved summary outcomes.</summary>
    /// <param name="summaryFailures">Three ordered failure flags.</param>
    /// <returns>Stable D7-U classification.</returns>
    internal static string ClassifyUnobserved(IReadOnlyList<bool> summaryFailures)
    {
        ArgumentNullException.ThrowIfNull(summaryFailures);
        if (summaryFailures.Count != O5R5D7Protocol.UnobservedRuns)
        {
            throw new InvalidOperationException("o5r5d7.classification.unobserved_incomplete");
        }

        return summaryFailures.Count(value => value) switch
        {
            0 => "D7.NOT_REPRODUCED",
            1 => "D7.INTERMITTENT",
            2 => "D7.REPEATED",
            3 => "D7.REPEATED_CONSECUTIVELY",
            _ => throw new InvalidOperationException("o5r5d7.classification.unobserved_invalid"),
        };
    }

    /// <summary>Classifies exactly two complete external summary outcomes.</summary>
    /// <param name="summaryFailures">Two ordered failure flags.</param>
    /// <returns>Stable descriptive D7-E classification.</returns>
    internal static string ClassifyExternal(IReadOnlyList<bool> summaryFailures)
    {
        ArgumentNullException.ThrowIfNull(summaryFailures);
        if (summaryFailures.Count != O5R5D7Protocol.ExternalRuns)
        {
            throw new InvalidOperationException("o5r5d7.classification.external_incomplete");
        }

        return summaryFailures.Count(value => value) switch
        {
            0 => "D7.EXTERNAL_NOT_REPRODUCED",
            1 => "D7.EXTERNAL_MIXED",
            2 => "D7.EXTERNAL_REPRODUCED",
            _ => throw new InvalidOperationException("o5r5d7.classification.external_invalid"),
        };
    }
}

/// <summary>Restricts and atomically publishes measured and final D7 evidence.</summary>
internal static class O5R5D7EvidenceWriter
{
    private const string RootPrefix = "DBNotifier-PF-OBS-1-D7-";
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Validates the exact final output below one direct D7 temporary child.</summary>
    /// <param name="path">Candidate final destination.</param>
    /// <param name="arm">Expected arm.</param>
    /// <param name="run">Expected one-based run.</param>
    /// <returns>The canonical validated destination.</returns>
    internal static string ValidateFinalDestination(string? path, O5R5D7Arm arm, int run)
    {
        O5R5D7MeasuredRunner.ValidateRun(arm, run);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("o5r5d7.output.required");
        }

        string destination = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(destination);
        string temporaryRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        string expectedName = $"{ArmCode(arm)}-run-{run}.json";
        if (directory is null ||
            !string.Equals(
                Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(directory)),
                temporaryRoot,
                StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(directory).StartsWith(RootPrefix, StringComparison.Ordinal) ||
            !string.Equals(Path.GetFileName(destination), expectedName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o5r5d7.output.path_refused");
        }

        return destination;
    }

    /// <summary>Returns the supervisor-owned measured sibling path.</summary>
    /// <param name="finalDestination">Validated final destination.</param>
    /// <returns>An exact sibling used only between supervisor and measured child.</returns>
    internal static string MeasuredDestination(string finalDestination) =>
        Path.ChangeExtension(finalDestination, ".measured.json");

    /// <summary>Atomically writes one measured child report.</summary>
    /// <param name="destination">Validated measured sibling destination.</param>
    /// <param name="report">Typed measured report.</param>
    /// <param name="cancellationToken">Cancellation requested before publication.</param>
    /// <returns>A task representing durable atomic publication.</returns>
    internal static Task WriteMeasuredAsync(
        string destination,
        O5R5D7MeasuredReport report,
        CancellationToken cancellationToken) =>
        WriteAsync(destination, report, cancellationToken);

    /// <summary>Atomically writes one final supervisor report.</summary>
    /// <param name="destination">Validated final destination.</param>
    /// <param name="report">Typed integrity-checked final report.</param>
    /// <param name="cancellationToken">Cancellation requested before publication.</param>
    /// <returns>A task representing durable atomic publication.</returns>
    internal static Task WriteFinalAsync(
        string destination,
        O5R5D7FinalReport report,
        CancellationToken cancellationToken) =>
        WriteAsync(destination, report, cancellationToken);

    /// <summary>Reads one measured sibling using the bounded typed schema.</summary>
    /// <param name="destination">Exact measured sibling.</param>
    /// <returns>The deserialised report.</returns>
    internal static O5R5D7MeasuredReport ReadMeasured(string destination)
    {
        byte[] bytes = File.ReadAllBytes(destination);
        if (bytes.Length is < 1 or > 1_000_000)
        {
            throw new InvalidOperationException("o5r5d7.evidence.size_invalid");
        }

        return JsonSerializer.Deserialize<O5R5D7MeasuredReport>(bytes, Options) ??
            throw new InvalidOperationException("o5r5d7.evidence.invalid");
    }

    /// <summary>Writes one typed report through a write-through sibling and atomic move.</summary>
    /// <typeparam name="T">Typed evidence schema.</typeparam>
    /// <param name="destination">Exact destination within the strict D7 temporary root.</param>
    /// <param name="report">Evidence value to serialise.</param>
    /// <param name="cancellationToken">Cancellation requested before publication.</param>
    /// <returns>A task representing durable atomic publication.</returns>
    private static async Task WriteAsync<T>(
        string destination,
        T report,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string temporary = destination + ".tmp";
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
            File.Move(temporary, destination, overwrite: false);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    /// <summary>Maps one arm to its stable file code.</summary>
    /// <param name="arm">Frozen observation arm.</param>
    /// <returns>The lower-case allow-listed file code.</returns>
    internal static string ArmCode(O5R5D7Arm arm) =>
        arm switch
        {
            O5R5D7Arm.Unobserved => "unobserved",
            O5R5D7Arm.External => "external",
            _ => throw new ArgumentOutOfRangeException(nameof(arm)),
        };
}

/// <summary>Exposes the marker-gated D7 measured child without supervisor counters.</summary>
public static class O5R5D7MeasuredProcess
{
    internal const string ActivationMarker = "pf-obs-1-d7-measured-test-only";
    private static readonly TimeSpan ProcessDeadline = TimeSpan.FromMinutes(5);

    /// <summary>Validates exact arguments, executes one group and publishes measured evidence.</summary>
    /// <param name="args">Activation, output, arm, run and sanitised environment arguments.</param>
    /// <returns>Zero for complete group evidence, two for invalid activation or three for failure.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (!O5R5D7Arguments.TryParse(args, ActivationMarker, out O5R5D7Arguments? parsed))
        {
            Console.Error.WriteLine("o5r5d7.measured.activation_invalid");
            return 2;
        }

        try
        {
            string final = O5R5D7EvidenceWriter.ValidateFinalDestination(
                parsed.Output,
                parsed.Arm,
                parsed.Run);
            string measured = O5R5D7EvidenceWriter.MeasuredDestination(final);

            O5R5PhysicalEnvironment environment = new(
                RuntimeInformation.OSDescription,
                RuntimeInformation.OSArchitecture.ToString(),
                RuntimeInformation.ProcessArchitecture.ToString(),
                RuntimeInformation.FrameworkDescription,
                parsed.Sdk,
                Environment.ProcessorCount,
                parsed.InstalledMemory,
                Stopwatch.Frequency);
            using O5R5DotNetMeasurementSource source = new();
            O5R5D7MeasuredRunner runner = new(source);
            using CancellationTokenSource deadline = new(ProcessDeadline);
            O5R5D7MeasuredReport report = await runner
                .RunAsync(parsed.Arm, parsed.Run, environment, deadline.Token)
                .ConfigureAwait(false);
            await O5R5D7EvidenceWriter
                .WriteMeasuredAsync(measured, report, CancellationToken.None)
                .ConfigureAwait(false);
            Console.WriteLine(report.Code);
            return report.Complete ? 0 : 3;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"o5r5d7.measured.failed:{exception.GetType().Name}");
            return 3;
        }
    }
}

/// <summary>Represents exact validated D7 process arguments.</summary>
internal sealed record O5R5D7Arguments(
    string Output,
    O5R5D7Arm Arm,
    int Run,
    string Sdk,
    long InstalledMemory)
{
    /// <summary>Parses one exact marker-gated D7 argument set.</summary>
    /// <param name="args">Complete process argument sequence.</param>
    /// <param name="marker">Exact marker expected by the selected entrypoint.</param>
    /// <param name="parsed">Validated bounded arguments when parsing succeeds.</param>
    /// <returns>True only when every argument and value is exact and bounded.</returns>
    internal static bool TryParse(
        string[] args,
        string marker,
        [NotNullWhen(true)] out O5R5D7Arguments? parsed)
    {
        parsed = null;
        if (args.Length != 12 ||
            args[0] != "--activation" ||
            args[1] != marker ||
            args[2] != "--output" ||
            args[4] != "--arm" ||
            !Enum.TryParse(args[5], ignoreCase: true, out O5R5D7Arm arm) ||
            args[6] != "--run" ||
            !int.TryParse(args[7], NumberStyles.None, CultureInfo.InvariantCulture, out int run) ||
            args[8] != "--sdk" ||
            string.IsNullOrWhiteSpace(args[9]) ||
            args[9].Length > 32 ||
            args[9] != args[9].Trim() ||
            args[10] != "--installed-memory" ||
            !long.TryParse(
                args[11],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long installedMemory) ||
            installedMemory < 1)
        {
            return false;
        }

        parsed = new O5R5D7Arguments(args[3], arm, run, args[9], installedMemory);
        return true;
    }
}
