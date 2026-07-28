// Module purpose: Wraps the unchanged D7 measurement in the independently identified D7-R1 evidence contract.
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

/// <summary>Defines the immutable D7-R1 identity, predecessor evidence and bounded run counts.</summary>
internal static class O5R5D7R1Protocol
{
    internal const string Version = "pfobs1-d7-r1-summary-count-contract-rerun-1.0.0";
    internal const string Digest = "4FD5E92E4674BF93DF102DCFC564614E7D417C400292324A76E45011AE39F5C5";
    internal const int UnobservedRuns = 3;
    internal const int ExternalRuns = 2;
    internal const int ExternalFailureGate = 2;
    internal const string CanonicalStatement =
        "pfobs1-d7-r1-summary-count-contract-rerun|1.0.0|" +
        "d7=7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF|" +
        "v3=60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A|" +
        "d6=5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845|" +
        "blocked-execution=0939b6560d2c4556ad781be9d9e3b376|" +
        "blocked-report-sha256=" +
        "AB48D99C1688EB3FC163DBAD59D4E393739B04BF1C82AB59C9239A5B180E2C85," +
        "0087DFAA4E7E014A7C037E1887C879A4A9C5501F1228280962854ED62224066F," +
        "7E167AFEBBC01F8DF4551A12CFD6036F2CD2156A0D92EF73E3DD80C9212D00C2|" +
        "blocked-evidence=preserve-byte-for-byte-exclude-from-classification|" +
        "scope=first-byte-cold-5-30-summary|" +
        "summary-count-schema=expected-1-completed-1-iff-summary-retained|" +
        "schema-gate=typed-roundtrip-exact-json-fields-values-validator|" +
        "unobserved-runs=3|external-runs=2|" +
        "external-gate=valid-r1-unobserved-failures>=2|historical-runs=replaced-0|" +
        "intraprocess-extra-captures=0|" +
        "external=supervisor-before-after-os-counters-only|" +
        "v3-threshold=0.20|forced-gc=v3-two-existing-only|activation=none";

    internal static readonly IReadOnlyList<string> PredecessorReportDigests =
    [
        "AB48D99C1688EB3FC163DBAD59D4E393739B04BF1C82AB59C9239A5B180E2C85",
        "0087DFAA4E7E014A7C037E1887C879A4A9C5501F1228280962854ED62224066F",
        "7E167AFEBBC01F8DF4551A12CFD6036F2CD2156A0D92EF73E3DD80C9212D00C2",
    ];

    /// <summary>Calculates the SHA-256 identity of the exact R1 canonical statement.</summary>
    /// <returns>An upper-case hexadecimal SHA-256 digest.</returns>
    internal static string CalculateDigest() =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalStatement)));

    /// <summary>Returns the exact authorised run count for one R1 arm.</summary>
    /// <param name="arm">Inherited D7 observation arm.</param>
    /// <returns>Three for unobserved or two for external.</returns>
    internal static int RunsFor(O5R5D7Arm arm) =>
        arm switch
        {
            O5R5D7Arm.Unobserved => UnobservedRuns,
            O5R5D7Arm.External => ExternalRuns,
            _ => throw new ArgumentOutOfRangeException(nameof(arm)),
        };
}

/// <summary>Reports one independently identified and permanently non-authorising D7-R1 attempt.</summary>
/// <param name="ProtocolVersion">Frozen R1 proposal version.</param>
/// <param name="ProtocolDigest">Frozen R1 proposal digest.</param>
/// <param name="D7ProtocolDigest">Unchanged predecessor D7 digest.</param>
/// <param name="V3ProtocolDigest">Unchanged V3 digest.</param>
/// <param name="D6ProtocolDigest">Referenced D6 digest.</param>
/// <param name="ActivationState">MOD-12 activation state, which remains None.</param>
/// <param name="Arm">Unobserved or conditional external arm.</param>
/// <param name="Run">One-based run within the R1 arm.</param>
/// <param name="Code">Stable bounded R1 outcome code.</param>
/// <param name="Complete">Whether the R1 report is admissible for classification.</param>
/// <param name="SummaryFailed">Whether only the exact V3 repeatability summary failed.</param>
/// <param name="Authorising">Always false because R1 cannot authorise activation.</param>
/// <param name="ExpectedSampleCount">Frozen sample count of 35.</param>
/// <param name="CompletedSampleCount">Complete retained D7 samples.</param>
/// <param name="ExpectedSummaryCount">Frozen summary count of one.</param>
/// <param name="CompletedSummaryCount">One only when the original summary is retained.</param>
/// <param name="Measured">Nested unchanged D7 measured report when available.</param>
/// <param name="Integrity">Independent five-median integrity result when available.</param>
/// <param name="ExternalObservation">Supervisor-owned external evidence only for R1-E.</param>
internal sealed record O5R5D7R1Envelope(
    string ProtocolVersion,
    string ProtocolDigest,
    string D7ProtocolDigest,
    string V3ProtocolDigest,
    string D6ProtocolDigest,
    string ActivationState,
    O5R5D7Arm Arm,
    int Run,
    string Code,
    bool Complete,
    bool SummaryFailed,
    bool Authorising,
    int ExpectedSampleCount,
    int CompletedSampleCount,
    int ExpectedSummaryCount,
    int CompletedSummaryCount,
    O5R5D7MeasuredReport? Measured,
    O5R5D7IntegrityResult? Integrity,
    O5R5D7ExternalObservation? ExternalObservation);

/// <summary>Creates, validates and classifies the distinct D7-R1 evidence envelope.</summary>
internal static class O5R5D7R1Evidence
{
    /// <summary>Finalises one measured report through the unchanged D7 validator and R1 contract.</summary>
    /// <param name="measured">Unchanged D7 measured child report.</param>
    /// <param name="externalObservation">Supervisor evidence for R1-E, otherwise null.</param>
    /// <returns>A complete or fail-closed R1 envelope.</returns>
    internal static O5R5D7R1Envelope Finalise(
        O5R5D7MeasuredReport measured,
        O5R5D7ExternalObservation? externalObservation)
    {
        ArgumentNullException.ThrowIfNull(measured);
        O5R5D7FinalReport d7 = O5R5D7EvidenceValidator.Finalise(
            measured,
            externalObservation);
        O5R5D7R1Envelope envelope = Create(
            measured.Arm,
            measured.Run,
            d7.Code == "o5r5d7.report.complete"
                ? "o5r5d7r1.report.complete"
                : d7.Code,
            d7.Complete,
            d7.SummaryFailed,
            measured,
            d7.Integrity,
            d7.ExternalObservation);
        Validate(envelope);
        return envelope;
    }

    /// <summary>Creates a retained fail-closed attempt when complete D7 validation is unavailable.</summary>
    /// <param name="arm">Supervisor-selected arm.</param>
    /// <param name="run">One-based R1 run.</param>
    /// <param name="measured">Partial measured report when safely readable.</param>
    /// <param name="code">Stable bounded blocking code.</param>
    /// <returns>A non-admissible R1 envelope that preserves available counts.</returns>
    internal static O5R5D7R1Envelope Blocked(
        O5R5D7Arm arm,
        int run,
        O5R5D7MeasuredReport? measured,
        string code)
    {
        ValidateRun(arm, run);
        O5R5D7R1Envelope envelope = Create(
            arm,
            run,
            code,
            complete: false,
            summaryFailed: false,
            measured,
            integrity: null,
            externalObservation: null);
        Validate(envelope);
        return envelope;
    }

    /// <summary>Validates every frozen identity, count, shape and non-authorising boundary.</summary>
    /// <param name="envelope">Typed R1 envelope to validate.</param>
    internal static void Validate(O5R5D7R1Envelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ValidateRun(envelope.Arm, envelope.Run);
        if (envelope.ProtocolVersion != O5R5D7R1Protocol.Version ||
            envelope.ProtocolDigest != O5R5D7R1Protocol.Digest ||
            envelope.D7ProtocolDigest != O5R5D7Protocol.Digest ||
            envelope.V3ProtocolDigest != O5R5D7Protocol.V3Digest ||
            envelope.D6ProtocolDigest != O5R5D7Protocol.D6Digest ||
            envelope.ActivationState != "None" ||
            envelope.Authorising ||
            envelope.ExpectedSampleCount != O5R5D7Protocol.ExpectedSampleCount ||
            envelope.ExpectedSummaryCount != O5R5D7Protocol.ExpectedSummaryCount ||
            envelope.CompletedSampleCount is < 0 or > O5R5D7Protocol.ExpectedSampleCount ||
            envelope.CompletedSummaryCount is < 0 or > O5R5D7Protocol.ExpectedSummaryCount)
        {
            throw new InvalidOperationException("o5r5d7r1.evidence.identity_invalid");
        }

        int measuredSamples = envelope.Measured?.Samples.Count ?? 0;
        int measuredSummaries = envelope.Measured?.Summary is null ? 0 : 1;
        if (envelope.CompletedSampleCount != measuredSamples ||
            envelope.CompletedSummaryCount != measuredSummaries ||
            (envelope.Arm == O5R5D7Arm.Unobserved &&
                envelope.ExternalObservation is not null))
        {
            throw new InvalidOperationException("o5r5d7r1.evidence.shape_invalid");
        }

        if (!envelope.Complete)
        {
            if (envelope.SummaryFailed)
            {
                throw new InvalidOperationException("o5r5d7r1.evidence.disposition_invalid");
            }
            return;
        }

        if (envelope.Measured is null ||
            !envelope.Measured.Complete ||
            envelope.CompletedSampleCount != O5R5D7Protocol.ExpectedSampleCount ||
            envelope.CompletedSummaryCount != O5R5D7Protocol.ExpectedSummaryCount ||
            envelope.Integrity is null ||
            !envelope.Integrity.ExactSummaryMatch ||
            (envelope.Arm == O5R5D7Arm.External &&
                envelope.ExternalObservation is null))
        {
            throw new InvalidOperationException("o5r5d7r1.evidence.complete_invalid");
        }

        O5R5D7FinalReport d7 = O5R5D7EvidenceValidator.Finalise(
            envelope.Measured,
            envelope.ExternalObservation);
        string expectedCode = d7.Code == "o5r5d7.report.complete"
            ? "o5r5d7r1.report.complete"
            : d7.Code;
        if (!d7.Complete ||
            d7.SummaryFailed != envelope.SummaryFailed ||
            d7.Integrity is null ||
            !EquivalentIntegrity(d7.Integrity, envelope.Integrity) ||
            envelope.Code != expectedCode)
        {
            throw new InvalidOperationException("o5r5d7r1.evidence.validation_mismatch");
        }
    }

    /// <summary>Validates the exact R1 arm and one-based run range.</summary>
    /// <param name="arm">Inherited observation arm.</param>
    /// <param name="run">One-based R1 run.</param>
    internal static void ValidateRun(O5R5D7Arm arm, int run)
    {
        if (!Enum.IsDefined(arm) || run < 1 || run > O5R5D7R1Protocol.RunsFor(arm))
        {
            throw new ArgumentOutOfRangeException(nameof(run));
        }
    }

    /// <summary>Classifies exactly three new admissible R1-U reports.</summary>
    /// <param name="summaryFailures">Three ordered exact-failure flags.</param>
    /// <returns>The frozen R1-U classification.</returns>
    internal static string ClassifyUnobserved(IReadOnlyList<bool> summaryFailures)
    {
        ArgumentNullException.ThrowIfNull(summaryFailures);
        if (summaryFailures.Count != O5R5D7R1Protocol.UnobservedRuns)
        {
            throw new InvalidOperationException("o5r5d7r1.classification.unobserved_incomplete");
        }

        return summaryFailures.Count(value => value) switch
        {
            0 => "D7-R1.NOT_REPRODUCED",
            1 => "D7-R1.INTERMITTENT",
            2 => "D7-R1.REPEATED",
            3 => "D7-R1.REPEATED_CONSECUTIVELY",
            _ => throw new InvalidOperationException("o5r5d7r1.classification.unobserved_invalid"),
        };
    }

    /// <summary>Classifies exactly two new admissible R1-E reports.</summary>
    /// <param name="summaryFailures">Two ordered exact-failure flags.</param>
    /// <returns>The frozen descriptive R1-E classification.</returns>
    internal static string ClassifyExternal(IReadOnlyList<bool> summaryFailures)
    {
        ArgumentNullException.ThrowIfNull(summaryFailures);
        if (summaryFailures.Count != O5R5D7R1Protocol.ExternalRuns)
        {
            throw new InvalidOperationException("o5r5d7r1.classification.external_incomplete");
        }

        return summaryFailures.Count(value => value) switch
        {
            0 => "D7-R1.EXTERNAL_NOT_REPRODUCED",
            1 => "D7-R1.EXTERNAL_MIXED",
            2 => "D7-R1.EXTERNAL_REPRODUCED",
            _ => throw new InvalidOperationException("o5r5d7r1.classification.external_invalid"),
        };
    }

    /// <summary>Compares deserialised integrity values with exact binary double semantics.</summary>
    /// <param name="left">Freshly recomputed D7 integrity.</param>
    /// <param name="right">Integrity retained in the R1 envelope.</param>
    /// <returns>True when every median, coefficient and exact-match flag agrees.</returns>
    private static bool EquivalentIntegrity(
        O5R5D7IntegrityResult left,
        O5R5D7IntegrityResult right) =>
        left.ExactSummaryMatch == right.ExactSummaryMatch &&
        BitConverter.DoubleToInt64Bits(left.CoefficientOfVariation) ==
            BitConverter.DoubleToInt64Bits(right.CoefficientOfVariation) &&
        left.GroupMediansMilliseconds.Count == right.GroupMediansMilliseconds.Count &&
        left.GroupMediansMilliseconds
            .Zip(right.GroupMediansMilliseconds)
            .All(
                pair =>
                    BitConverter.DoubleToInt64Bits(pair.First) ==
                    BitConverter.DoubleToInt64Bits(pair.Second));

    /// <summary>Creates one envelope from available measured and supervisor-owned evidence.</summary>
    /// <param name="arm">Inherited observation arm.</param>
    /// <param name="run">One-based R1 run.</param>
    /// <param name="code">Stable bounded outcome code.</param>
    /// <param name="complete">Whether the attempt is admissible.</param>
    /// <param name="summaryFailed">Whether only the exact summary failure occurred.</param>
    /// <param name="measured">Measured D7 report when available.</param>
    /// <param name="integrity">Independent integrity result when available.</param>
    /// <param name="externalObservation">External evidence only for R1-E.</param>
    /// <returns>A distinct immutable R1 envelope.</returns>
    private static O5R5D7R1Envelope Create(
        O5R5D7Arm arm,
        int run,
        string code,
        bool complete,
        bool summaryFailed,
        O5R5D7MeasuredReport? measured,
        O5R5D7IntegrityResult? integrity,
        O5R5D7ExternalObservation? externalObservation) =>
        new(
            O5R5D7R1Protocol.Version,
            O5R5D7R1Protocol.Digest,
            O5R5D7Protocol.Digest,
            O5R5D7Protocol.V3Digest,
            O5R5D7Protocol.D6Digest,
            "None",
            arm,
            run,
            code,
            complete,
            summaryFailed,
            Authorising: false,
            O5R5D7Protocol.ExpectedSampleCount,
            measured?.Samples.Count ?? 0,
            O5R5D7Protocol.ExpectedSummaryCount,
            measured?.Summary is null ? 0 : 1,
            measured,
            integrity,
            externalObservation);
}

/// <summary>Restricts, validates and atomically publishes distinct D7-R1 evidence.</summary>
internal static class O5R5D7R1EvidenceWriter
{
    private const string RootPrefix = "DBNotifier-PF-OBS-1-D7-R1-";
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Validates an exact final destination below one direct R1 temporary child.</summary>
    /// <param name="path">Candidate final destination.</param>
    /// <param name="arm">Expected R1 arm.</param>
    /// <param name="run">Expected one-based R1 run.</param>
    /// <returns>The canonical validated destination.</returns>
    internal static string ValidateFinalDestination(string? path, O5R5D7Arm arm, int run)
    {
        O5R5D7R1Evidence.ValidateRun(arm, run);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("o5r5d7r1.output.required");
        }

        string destination = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(destination);
        string temporaryRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        string expectedName = $"{O5R5D7EvidenceWriter.ArmCode(arm)}-run-{run}.json";
        if (directory is null ||
            !string.Equals(
                Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(directory)),
                temporaryRoot,
                StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(directory).StartsWith(RootPrefix, StringComparison.Ordinal) ||
            !string.Equals(Path.GetFileName(destination), expectedName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o5r5d7r1.output.path_refused");
        }

        return destination;
    }

    /// <summary>Returns the supervisor-owned intermediate measured sibling.</summary>
    /// <param name="finalDestination">Validated final R1 destination.</param>
    /// <returns>The exact unretained measured sibling path.</returns>
    internal static string MeasuredDestination(string finalDestination) =>
        Path.ChangeExtension(finalDestination, ".measured.json");

    /// <summary>Serialises one R1 envelope using the production UTF-8 contract.</summary>
    /// <param name="envelope">Validated complete or blocked R1 evidence.</param>
    /// <returns>The formatted UTF-8 JSON bytes.</returns>
    internal static byte[] Serialise(O5R5D7R1Envelope envelope)
    {
        O5R5D7R1Evidence.Validate(envelope);
        return JsonSerializer.SerializeToUtf8Bytes(envelope, Options);
    }

    /// <summary>Deserialises and validates exact raw counts and typed R1 semantics.</summary>
    /// <param name="bytes">Bounded UTF-8 JSON evidence.</param>
    /// <returns>The validated typed R1 envelope.</returns>
    internal static O5R5D7R1Envelope Deserialise(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is < 1 or > 1_000_000)
        {
            throw new InvalidOperationException("o5r5d7r1.evidence.size_invalid");
        }

        ValidateRawSummaryCounts(bytes);
        O5R5D7R1Envelope envelope =
            JsonSerializer.Deserialize<O5R5D7R1Envelope>(bytes, Options) ??
            throw new InvalidOperationException("o5r5d7r1.evidence.invalid");
        O5R5D7R1Evidence.Validate(envelope);
        return envelope;
    }

    /// <summary>Reads one final R1 report through the production schema and semantic validator.</summary>
    /// <param name="destination">Exact final R1 destination.</param>
    /// <returns>The validated typed R1 envelope.</returns>
    internal static O5R5D7R1Envelope ReadFinal(string destination) =>
        Deserialise(File.ReadAllBytes(destination));

    /// <summary>Atomically writes one final R1 report after exact semantic validation.</summary>
    /// <param name="destination">Validated final R1 destination.</param>
    /// <param name="envelope">Complete or fail-closed R1 evidence.</param>
    /// <param name="cancellationToken">Cancellation requested before publication.</param>
    /// <returns>A task representing durable atomic publication.</returns>
    internal static async Task WriteFinalAsync(
        string destination,
        O5R5D7R1Envelope envelope,
        CancellationToken cancellationToken)
    {
        byte[] bytes = Serialise(envelope);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string temporary = destination + ".tmp";
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

    /// <summary>Rejects missing, duplicate, mistyped or out-of-range root summary-count fields.</summary>
    /// <param name="bytes">Raw UTF-8 R1 evidence.</param>
    private static void ValidateRawSummaryCounts(ReadOnlySpan<byte> bytes)
    {
        Utf8JsonReader reader = new(
            bytes,
            new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 128,
            });
        int expectedOccurrences = 0;
        int completedOccurrences = 0;
        int? expected = null;
        int? completed = null;
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 1)
            {
                continue;
            }

            bool isExpected = reader.ValueTextEquals("expectedSummaryCount"u8);
            bool isCompleted = reader.ValueTextEquals("completedSummaryCount"u8);
            if (!isExpected && !isCompleted)
            {
                continue;
            }
            if (!reader.Read() ||
                reader.TokenType != JsonTokenType.Number ||
                !reader.TryGetInt32(out int value))
            {
                throw new InvalidOperationException("o5r5d7r1.evidence.summary_count_type_invalid");
            }

            if (isExpected)
            {
                expectedOccurrences++;
                expected = value;
            }
            else
            {
                completedOccurrences++;
                completed = value;
            }
        }

        if (expectedOccurrences != 1 ||
            completedOccurrences != 1 ||
            expected != O5R5D7Protocol.ExpectedSummaryCount ||
            completed is < 0 or > O5R5D7Protocol.ExpectedSummaryCount)
        {
            throw new InvalidOperationException("o5r5d7r1.evidence.summary_count_invalid");
        }
    }
}

/// <summary>Exposes the marker-gated R1 measured child over the unchanged D7 runner.</summary>
public static class O5R5D7R1MeasuredProcess
{
    internal const string ActivationMarker = "pf-obs-1-d7-r1-measured-test-only";
    private static readonly TimeSpan ProcessDeadline = TimeSpan.FromMinutes(5);

    /// <summary>Executes one exact D7 group and publishes only the intermediate measured sibling.</summary>
    /// <param name="args">Exact activation, output, arm, run and sanitised environment arguments.</param>
    /// <returns>Zero for complete measured evidence, two for invalid activation or three for failure.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (!O5R5D7R1Arguments.TryParse(args, ActivationMarker, out O5R5D7R1Arguments? parsed))
        {
            Console.Error.WriteLine("o5r5d7r1.measured.activation_invalid");
            return 2;
        }

        try
        {
            string final = O5R5D7R1EvidenceWriter.ValidateFinalDestination(
                parsed.Output,
                parsed.Arm,
                parsed.Run);
            string measuredDestination = O5R5D7R1EvidenceWriter.MeasuredDestination(final);
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
            O5R5D7MeasuredReport measured = await runner
                .RunAsync(parsed.Arm, parsed.Run, environment, deadline.Token)
                .ConfigureAwait(false);
            await O5R5D7EvidenceWriter
                .WriteMeasuredAsync(measuredDestination, measured, CancellationToken.None)
                .ConfigureAwait(false);
            Console.WriteLine(measured.Code);
            return measured.Complete ? 0 : 3;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"o5r5d7r1.measured.failed:{exception.GetType().Name}");
            return 3;
        }
    }
}

/// <summary>Exposes the sole R1 fresh-child launcher and final evidence publication boundary.</summary>
public static class O5R5D7R1SupervisorProcess
{
    internal const string ActivationMarker = "pf-obs-1-d7-r1-supervisor-test-only";
    private static readonly TimeSpan ProcessDeadline = TimeSpan.FromMinutes(5);

    /// <summary>Runs one fresh R1 child, validates D7 and R1 semantics and publishes final evidence.</summary>
    /// <param name="args">Exact activation, output, arm, run and sanitised environment arguments.</param>
    /// <returns>Zero for admissible evidence, two for invalid activation or three for blocked evidence.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (!O5R5D7R1Arguments.TryParse(
                args,
                ActivationMarker,
                out O5R5D7R1Arguments? parsed))
        {
            Console.Error.WriteLine("o5r5d7r1.supervisor.activation_invalid");
            return 2;
        }

        string? finalDestination = null;
        string? measuredDestination = null;
        O5R5D7MeasuredReport? measured = null;
        try
        {
            finalDestination = O5R5D7R1EvidenceWriter.ValidateFinalDestination(
                parsed.Output,
                parsed.Arm,
                parsed.Run);
            measuredDestination = O5R5D7R1EvidenceWriter.MeasuredDestination(finalDestination);
            if (File.Exists(measuredDestination) || File.Exists(finalDestination))
            {
                throw new InvalidOperationException("o5r5d7r1.output.exists");
            }

            O5R5D7SystemSnapshot? before = parsed.Arm == O5R5D7Arm.External
                ? O5R5D7ExternalCounters.CaptureSystem()
                : null;
            using Process child = StartMeasuredChild(parsed);
            using CancellationTokenSource deadline = new(ProcessDeadline);
            try
            {
                await child.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (!child.HasExited)
                {
                    child.Kill(entireProcessTree: true);
                    await child.WaitForExitAsync().ConfigureAwait(false);
                }
                throw;
            }

            O5R5D7ExternalObservation? external = null;
            if (parsed.Arm == O5R5D7Arm.External)
            {
                O5R5D7ProcessSnapshot process = O5R5D7ExternalCounters.CaptureProcess(child);
                O5R5D7SystemSnapshot after = O5R5D7ExternalCounters.CaptureSystem();
                external = new O5R5D7ExternalObservation(before!, after, process);
            }

            if (!File.Exists(measuredDestination))
            {
                throw new InvalidOperationException("o5r5d7r1.measured.missing");
            }

            measured = O5R5D7EvidenceWriter.ReadMeasured(measuredDestination);
            O5R5D7R1Envelope envelope = O5R5D7R1Evidence.Finalise(measured, external);
            await O5R5D7R1EvidenceWriter
                .WriteFinalAsync(finalDestination, envelope, CancellationToken.None)
                .ConfigureAwait(false);
            Console.WriteLine(envelope.Code);
            return child.ExitCode == 0 && envelope.Complete ? 0 : 3;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (finalDestination is not null && !File.Exists(finalDestination))
            {
                try
                {
                    O5R5D7R1Envelope blocked = O5R5D7R1Evidence.Blocked(
                        parsed.Arm,
                        parsed.Run,
                        measured,
                        "o5r5d7r1.supervisor.failed");
                    await O5R5D7R1EvidenceWriter
                        .WriteFinalAsync(finalDestination, blocked, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (Exception publicationException) when (
                    publicationException is not OutOfMemoryException)
                {
                    Console.Error.WriteLine(
                        $"o5r5d7r1.publication.failed:{publicationException.GetType().Name}");
                }
            }
            Console.Error.WriteLine($"o5r5d7r1.supervisor.failed:{exception.GetType().Name}");
            return 3;
        }
        finally
        {
            if (measuredDestination is not null && File.Exists(measuredDestination))
            {
                File.Delete(measuredDestination);
            }
        }
    }

    /// <summary>Starts the current sandbox host as one exact fresh R1 measured child.</summary>
    /// <param name="parsed">Validated R1 process arguments.</param>
    /// <returns>The newly started measured child.</returns>
    private static Process StartMeasuredChild(O5R5D7R1Arguments parsed)
    {
        string processPath = Environment.ProcessPath ??
            throw new InvalidOperationException("o5r5d7r1.supervisor.process_path_missing");
        string entryAssembly = Assembly.GetEntryAssembly()?.Location ??
            throw new InvalidOperationException("o5r5d7r1.supervisor.entry_missing");
        ProcessStartInfo start = new()
        {
            FileName = processPath,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (string.Equals(
                Path.GetFileNameWithoutExtension(processPath),
                "dotnet",
                StringComparison.OrdinalIgnoreCase))
        {
            start.ArgumentList.Add(entryAssembly);
        }
        start.ArgumentList.Add("--activation");
        start.ArgumentList.Add(O5R5D7R1MeasuredProcess.ActivationMarker);
        start.ArgumentList.Add("--output");
        start.ArgumentList.Add(parsed.Output);
        start.ArgumentList.Add("--arm");
        start.ArgumentList.Add(parsed.Arm.ToString());
        start.ArgumentList.Add("--run");
        start.ArgumentList.Add(parsed.Run.ToString(CultureInfo.InvariantCulture));
        start.ArgumentList.Add("--sdk");
        start.ArgumentList.Add(parsed.Sdk);
        start.ArgumentList.Add("--installed-memory");
        start.ArgumentList.Add(parsed.InstalledMemory.ToString(CultureInfo.InvariantCulture));
        return Process.Start(start) ??
            throw new InvalidOperationException("o5r5d7r1.supervisor.start_failed");
    }
}

/// <summary>Represents exact validated D7-R1 process arguments.</summary>
internal sealed record O5R5D7R1Arguments(
    string Output,
    O5R5D7Arm Arm,
    int Run,
    string Sdk,
    long InstalledMemory)
{
    /// <summary>Parses one exact marker-gated R1 argument sequence.</summary>
    /// <param name="args">Complete process argument sequence.</param>
    /// <param name="marker">Exact marker expected by the selected R1 entrypoint.</param>
    /// <param name="parsed">Validated bounded arguments when parsing succeeds.</param>
    /// <returns>True only when every argument and value is exact and bounded.</returns>
    internal static bool TryParse(
        string[] args,
        string marker,
        [NotNullWhen(true)] out O5R5D7R1Arguments? parsed)
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

        try
        {
            O5R5D7R1Evidence.ValidateRun(arm, run);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }

        parsed = new O5R5D7R1Arguments(args[3], arm, run, args[9], installedMemory);
        return true;
    }
}
