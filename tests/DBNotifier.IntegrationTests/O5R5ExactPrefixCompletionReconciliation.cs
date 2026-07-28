// Module purpose: Reconciles exact V3 prefix completion in two isolated D8 processes without adding measurement or observation.
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

/// <summary>Defines the immutable D8 identity, predecessor dependencies and exact two-run boundary.</summary>
internal static class O5R5D8Protocol
{
    internal const string Version = "pfobs1-d8-exact-prefix-completion-reconciliation-1.0.0";
    internal const string Digest = "208DA70A8D638E50E2951DECDA83414B9E1F7CF4AFF957D09C1EAA2A8B1B8814";
    internal const string V3Digest = "60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A";
    internal const string D5Digest = "82606BF31085214607C8CBE401C0F4050D9465523B9B01F89FFE45731012FFDA";
    internal const string D6Digest = "5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845";
    internal const string D7Digest = "7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF";
    internal const string D7R1Digest = "4FD5E92E4674BF93DF102DCFC564614E7D417C400292324A76E45011AE39F5C5";
    internal const int Runs = 2;
    internal const int ExpectedSampleCount = 158;
    internal const int ExpectedSummaryCount = 4;
    internal const int GroupSampleCount = 35;
    internal const int FirstTargetSampleCount = 141;
    internal const long WorkingSetLimitBytes = 786_432;
    internal const double RepeatabilityLimit = 0.20d;
    internal const string CanonicalStatement =
        "pfobs1-d8-exact-prefix-completion-reconciliation|1.0.0|" +
        "v3=60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A|" +
        "d5=82606BF31085214607C8CBE401C0F4050D9465523B9B01F89FFE45731012FFDA|" +
        "d5-report=2CC5AD651A40AAC82CC6EFBB02456F9F1CEC99C4E4153E861E14447EBA2A5418|" +
        "d5-human=334C91A9E2D733478039AFCCCC263823AEAF9507174C7C44C112F94C2B7D4A34|" +
        "d6=5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845|" +
        "d6-report=9FD674AA32F47FB5BCBC37CC90616CAE066BE3299C5278DDDF6D3A50465AAEDD|" +
        "d7=7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF|" +
        "d7-report=B916B42409210E269241CE6187053E4411DE57502A7DD06B934455F46F428D2A|" +
        "d7-r1=4FD5E92E4674BF93DF102DCFC564614E7D417C400292324A76E45011AE39F5C5|" +
        "d7-r1-report=4686BCA6C94BE60708BE439F697060EED56523AED81CED26C1B8C84705A0F820|" +
        "d7-r1-human=D0FE791D85136CFE44776465DEC093122C909F5C7E07A957A23F5E1504A5C1D9|" +
        "runs=2|arm=unobserved|precondition=v3-exact|" +
        "prefix=first-byte-cold-5-30-summary,first-byte-warm-5-30-summary," +
        "idle-cold-5-30-summary,idle-warm-5-30-summary,cancellation-cold-5-13|" +
        "expected-samples=158|expected-summaries=4|" +
        "target-stop=cancellation-cold-working-set-excess-through-measured-13|" +
        "target-eligible=complete-prefix-or-exact-target-stop|" +
        "valid-early-stop-run2=continue|invalid-report-run2=prohibited|" +
        "working-set-limit=786432|repeatability-limit=0.20|" +
        "intraprocess-extra-captures=0|external=0|d5-controls=0|" +
        "historical-runs-replaced=0|forced-gc=v3-two-existing-only|activation=none";

    /// <summary>Calculates the SHA-256 identity of the exact preregistered D8 statement.</summary>
    /// <returns>An upper-case hexadecimal SHA-256 digest.</returns>
    internal static string CalculateDigest() =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalStatement)));
}

/// <summary>Identifies the independently validated stopping boundary of one D8 attempt.</summary>
internal enum O5R5D8Disposition
{
    Blocked = 1,
    CompletePrefix = 2,
    TargetStop = 3,
    EarlyGateStop = 4,
    OtherV3Stop = 5,
}

/// <summary>Retains one D8 attempt with explicit counts, binary identity and non-authorising semantics.</summary>
/// <param name="ProtocolVersion">Frozen D8 protocol version.</param>
/// <param name="ProtocolDigest">Frozen D8 proposal digest.</param>
/// <param name="V3ProtocolDigest">Unchanged V3 protocol digest.</param>
/// <param name="D5ProtocolDigest">Frozen D5 protocol digest.</param>
/// <param name="D6ProtocolDigest">Frozen D6 protocol digest.</param>
/// <param name="D7ProtocolDigest">Frozen D7 proposal digest.</param>
/// <param name="D7R1ProtocolDigest">Frozen D7-R1 proposal digest.</param>
/// <param name="ActivationState">MOD-12 activation state, which remains None.</param>
/// <param name="HostBinarySha256">Exact sandbox-host binary used by this attempt.</param>
/// <param name="Run">One-based fixed D8 attempt.</param>
/// <param name="Code">Stable bounded D8 disposition code.</param>
/// <param name="Disposition">Independently validated stopping boundary.</param>
/// <param name="Admissible">Whether this attempt may enter the D8 decision tree.</param>
/// <param name="Complete">Whether all 158 samples completed without a failure.</param>
/// <param name="TargetEligible">Whether the exact prefix completed or the exact target failure stopped it.</param>
/// <param name="WorkingSetExcess">Whether the exact Cancellation/Cold target failure was retained.</param>
/// <param name="Authorising">Always false because D8 cannot authorise activation.</param>
/// <param name="ExpectedSampleCount">Frozen expected sample count of 158.</param>
/// <param name="CompletedSampleCount">Number of retained samples.</param>
/// <param name="ExpectedSummaryCount">Frozen expected summary count of four.</param>
/// <param name="CompletedSummaryCount">Number of retained original V3 summaries.</param>
/// <param name="Measured">Nested unchanged D6 prefix report when independently admissible.</param>
internal sealed record O5R5D8Envelope(
    string ProtocolVersion,
    string ProtocolDigest,
    string V3ProtocolDigest,
    string D5ProtocolDigest,
    string D6ProtocolDigest,
    string D7ProtocolDigest,
    string D7R1ProtocolDigest,
    string ActivationState,
    string HostBinarySha256,
    int Run,
    string Code,
    O5R5D8Disposition Disposition,
    bool Admissible,
    bool Complete,
    bool TargetEligible,
    bool WorkingSetExcess,
    bool Authorising,
    int ExpectedSampleCount,
    int CompletedSampleCount,
    int ExpectedSummaryCount,
    int CompletedSummaryCount,
    O5R5D6Report? Measured);

/// <summary>Independently validates D8 prefix membership, stopping boundaries and classifications.</summary>
internal static class O5R5D8Evidence
{
    private const string WorkingSetFailureCode = "o5r5d1.threshold.working-set-peak";
    private const string RepeatabilityFailureCode = "o5r5d1.threshold.repeatability-coefficient";

    /// <summary>Finalises one unchanged D6 measured report as a distinct D8 attempt.</summary>
    /// <param name="measured">Measured exact-prefix report from the fresh D8 child.</param>
    /// <param name="hostBinarySha256">SHA-256 identity of the frozen sandbox host.</param>
    /// <returns>An admissible D8 envelope with one exact stopping disposition.</returns>
    internal static O5R5D8Envelope Finalise(O5R5D6Report measured, string hostBinarySha256)
    {
        ArgumentNullException.ThrowIfNull(measured);
        ValidateMeasuredPrefix(measured);

        O5R5D8Disposition disposition;
        string code;
        if (IsCompletePrefix(measured))
        {
            disposition = O5R5D8Disposition.CompletePrefix;
            code = "o5r5d8.prefix.complete";
        }
        else if (IsExactTargetStop(measured))
        {
            disposition = O5R5D8Disposition.TargetStop;
            code = "o5r5d8.target_stop";
        }
        else if (IsExactEarlyGateStop(measured))
        {
            disposition = O5R5D8Disposition.EarlyGateStop;
            code = "o5r5d8.early_repeatability_stop";
        }
        else if (IsOtherExactV3Stop(measured))
        {
            disposition = O5R5D8Disposition.OtherV3Stop;
            code = "o5r5d8.other_v3_stop";
        }
        else
        {
            throw new InvalidOperationException("o5r5d8.measured.boundary_invalid");
        }

        O5R5D8Envelope envelope = Create(
            measured.Run,
            hostBinarySha256,
            code,
            disposition,
            admissible: true,
            measured);
        Validate(envelope);
        return envelope;
    }

    /// <summary>Creates one retained contract-invalid attempt without interpreting V3 results.</summary>
    /// <param name="run">One-based fixed D8 attempt.</param>
    /// <param name="hostBinarySha256">SHA-256 identity of the selected sandbox host.</param>
    /// <returns>A non-admissible, non-authorising D8 envelope.</returns>
    internal static O5R5D8Envelope Blocked(int run, string hostBinarySha256)
    {
        O5R5D8Envelope envelope = Create(
            run,
            hostBinarySha256,
            "o5r5d8.supervisor.failed",
            O5R5D8Disposition.Blocked,
            admissible: false,
            measured: null);
        Validate(envelope);
        return envelope;
    }

    /// <summary>Validates every frozen identity, root count and disposition invariant.</summary>
    /// <param name="envelope">Typed D8 envelope to validate.</param>
    internal static void Validate(O5R5D8Envelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ValidateRun(envelope.Run);
        if (envelope.ProtocolVersion != O5R5D8Protocol.Version ||
            envelope.ProtocolDigest != O5R5D8Protocol.Digest ||
            envelope.V3ProtocolDigest != O5R5D8Protocol.V3Digest ||
            envelope.D5ProtocolDigest != O5R5D8Protocol.D5Digest ||
            envelope.D6ProtocolDigest != O5R5D8Protocol.D6Digest ||
            envelope.D7ProtocolDigest != O5R5D8Protocol.D7Digest ||
            envelope.D7R1ProtocolDigest != O5R5D8Protocol.D7R1Digest ||
            envelope.ActivationState != "None" ||
            envelope.Authorising ||
            !IsSha256(envelope.HostBinarySha256) ||
            envelope.ExpectedSampleCount != O5R5D8Protocol.ExpectedSampleCount ||
            envelope.ExpectedSummaryCount != O5R5D8Protocol.ExpectedSummaryCount ||
            envelope.CompletedSampleCount is < 0 or > O5R5D8Protocol.ExpectedSampleCount ||
            envelope.CompletedSummaryCount is < 0 or > O5R5D8Protocol.ExpectedSummaryCount)
        {
            throw new InvalidOperationException("o5r5d8.evidence.identity_invalid");
        }

        int measuredSamples = envelope.Measured?.Samples.Count ?? 0;
        int measuredSummaries = envelope.Measured?.Summaries.Count ?? 0;
        if (envelope.CompletedSampleCount != measuredSamples ||
            envelope.CompletedSummaryCount != measuredSummaries)
        {
            throw new InvalidOperationException("o5r5d8.evidence.count_invalid");
        }

        if (!envelope.Admissible)
        {
            if (envelope.Disposition != O5R5D8Disposition.Blocked ||
                envelope.Measured is not null ||
                envelope.Complete ||
                envelope.TargetEligible ||
                envelope.WorkingSetExcess)
            {
                throw new InvalidOperationException("o5r5d8.evidence.blocked_invalid");
            }
            return;
        }

        if (envelope.Measured is null)
        {
            throw new InvalidOperationException("o5r5d8.evidence.measured_missing");
        }
        ValidateMeasuredPrefix(envelope.Measured);
        bool complete = IsCompletePrefix(envelope.Measured);
        bool targetStop = IsExactTargetStop(envelope.Measured);
        bool earlyStop = IsExactEarlyGateStop(envelope.Measured);
        bool otherStop = IsOtherExactV3Stop(envelope.Measured);
        O5R5D8Disposition expectedDisposition = complete
            ? O5R5D8Disposition.CompletePrefix
            : targetStop
                ? O5R5D8Disposition.TargetStop
                : earlyStop
                    ? O5R5D8Disposition.EarlyGateStop
                    : otherStop
                        ? O5R5D8Disposition.OtherV3Stop
                        : throw new InvalidOperationException("o5r5d8.evidence.boundary_invalid");
        if (envelope.Disposition != expectedDisposition ||
            envelope.Complete != complete ||
            envelope.TargetEligible != (complete || targetStop) ||
            envelope.WorkingSetExcess != targetStop ||
            envelope.Code != CodeFor(expectedDisposition))
        {
            throw new InvalidOperationException("o5r5d8.evidence.disposition_invalid");
        }
    }

    /// <summary>Classifies the fixed D8 attempts without selecting or replacing a run.</summary>
    /// <param name="attempts">One blocked first attempt or exactly two admissible ordered attempts.</param>
    /// <returns>The exact frozen D8 decision-tree classification.</returns>
    internal static string Classify(IReadOnlyList<O5R5D8Envelope> attempts)
    {
        ArgumentNullException.ThrowIfNull(attempts);
        if (attempts.Count is < 1 or > O5R5D8Protocol.Runs ||
            attempts.Select(item => item.Run).Distinct().Count() != attempts.Count ||
            attempts.Where((item, index) => item.Run != index + 1).Any())
        {
            throw new InvalidOperationException("o5r5d8.classification.cardinality_invalid");
        }
        foreach (O5R5D8Envelope attempt in attempts)
        {
            Validate(attempt);
        }
        if (attempts.Any(item => !item.Admissible))
        {
            return "D8.BLOCKED_EVIDENCE";
        }
        if (attempts.Count != O5R5D8Protocol.Runs)
        {
            throw new InvalidOperationException("o5r5d8.classification.second_attempt_missing");
        }
        if (attempts.Any(item => item.Disposition == O5R5D8Disposition.OtherV3Stop))
        {
            return "D8.PREFIX_BLOCKED_OTHER_V3";
        }

        O5R5D8Envelope[] early = attempts
            .Where(item => item.Disposition == O5R5D8Disposition.EarlyGateStop)
            .ToArray();
        if (early.Length == 1)
        {
            return "D8.EARLY_GATE_INTERMITTENT";
        }
        if (early.Length == 2)
        {
            bool samePhase =
                early[0].Measured!.Summaries[^1].Phase ==
                early[1].Measured!.Summaries[^1].Phase &&
                early[0].Measured!.Summaries[^1].Temperature ==
                early[1].Measured!.Summaries[^1].Temperature;
            return samePhase
                ? "D8.EARLY_GATE_REPEATED"
                : "D8.EARLY_GATE_DIVERGENT";
        }

        if (attempts.Any(item => !item.TargetEligible))
        {
            throw new InvalidOperationException("o5r5d8.classification.target_invalid");
        }
        return attempts.Count(item => item.WorkingSetExcess) switch
        {
            0 => "D8.WORKING_SET_NOT_REPRODUCED",
            1 => "D8.WORKING_SET_INTERMITTENT",
            2 => "D8.WORKING_SET_REPEATED",
            _ => throw new InvalidOperationException("o5r5d8.classification.invalid"),
        };
    }

    /// <summary>Validates one-based D8 attempt identity.</summary>
    /// <param name="run">Candidate attempt number.</param>
    internal static void ValidateRun(int run)
    {
        if (run is < 1 or > O5R5D8Protocol.Runs)
        {
            throw new ArgumentOutOfRangeException(nameof(run));
        }
    }

    /// <summary>Validates the nested D6 identity and exact contiguous V3 prefix membership.</summary>
    /// <param name="measured">Candidate measured report.</param>
    private static void ValidateMeasuredPrefix(O5R5D6Report measured)
    {
        ValidateRun(measured.Run);
        if (measured.ProtocolVersion != O5R5D6Protocol.Version ||
            measured.ProtocolDigest != O5R5D8Protocol.D6Digest ||
            measured.V3ProtocolDigest != O5R5D8Protocol.V3Digest ||
            measured.ActivationState != "None" ||
            measured.ExpectedSampleCount != O5R5D8Protocol.ExpectedSampleCount ||
            measured.CompletedSampleCount != measured.Samples.Count ||
            measured.Samples.Count > O5R5D8Protocol.ExpectedSampleCount ||
            measured.Summaries.Count > O5R5D8Protocol.ExpectedSummaryCount ||
            measured.StartedAtUtc > measured.CompletedAtUtc)
        {
            throw new InvalidOperationException("o5r5d8.measured.identity_invalid");
        }

        O5R5D6Runner selector = new(new UnusedMeasurementSource());
        IReadOnlyList<O5R5MeasurementScenario> expected = selector.SelectExactPrefix();
        for (int index = 0; index < measured.Samples.Count; index++)
        {
            O5R5MeasurementSample sample = measured.Samples[index];
            O5R5MeasurementScenario scenario = expected[index];
            if (sample.ProtocolDigest != O5R5D8Protocol.V3Digest ||
                sample.Phase != scenario.Phase ||
                sample.Temperature != scenario.Temperature ||
                sample.IsWarmUp != scenario.IsWarmUp ||
                sample.Repetition != scenario.Repetition ||
                sample.DeclaredWorkUnits != scenario.DeclaredWorkUnits ||
                sample.AccountedMemoryBytes != scenario.AccountedMemoryBytes)
            {
                throw new InvalidOperationException("o5r5d8.measured.prefix_invalid");
            }
        }

        O5R5MeasurementPhase[] phases =
        [
            O5R5MeasurementPhase.FirstByte,
            O5R5MeasurementPhase.FirstByte,
            O5R5MeasurementPhase.Idle,
            O5R5MeasurementPhase.Idle,
        ];
        O5R5Temperature[] temperatures =
        [
            O5R5Temperature.Cold,
            O5R5Temperature.Warm,
            O5R5Temperature.Cold,
            O5R5Temperature.Warm,
        ];
        for (int index = 0; index < measured.Summaries.Count; index++)
        {
            O5R5MeasurementSummary summary = measured.Summaries[index];
            if (summary.Phase != phases[index] ||
                summary.Temperature != temperatures[index] ||
                summary.WarmUpCount != O5R5MeasurementProtocol.WarmUpRepetitions ||
                summary.MeasuredCount != O5R5MeasurementProtocol.MeasuredRepetitions ||
                measured.Samples.Count < ((index + 1) * O5R5D8Protocol.GroupSampleCount))
            {
                throw new InvalidOperationException("o5r5d8.measured.summary_order_invalid");
            }
        }
    }

    /// <summary>Determines whether the entire exact prefix passed with four summaries.</summary>
    /// <param name="measured">Validated measured report.</param>
    /// <returns>True only for a failure-free 158/4 prefix.</returns>
    private static bool IsCompletePrefix(O5R5D6Report measured) =>
        measured.Samples.Count == O5R5D8Protocol.ExpectedSampleCount &&
        measured.Summaries.Count == O5R5D8Protocol.ExpectedSummaryCount &&
        measured.Failures.Count == 0 &&
        measured.Samples.All(item => item.Passed) &&
        measured.Summaries.All(item => item.Passed) &&
        measured.Complete &&
        !measured.WorkingSetExcessReproduced &&
        measured.Code == "o5r5d6.prefix.complete";

    /// <summary>Determines whether the last retained sample is the exact working-set target stop.</summary>
    /// <param name="measured">Validated measured report.</param>
    /// <returns>True only for the exact Cancellation/Cold working-set failure boundary.</returns>
    private static bool IsExactTargetStop(O5R5D6Report measured)
    {
        if (measured.Samples.Count is < O5R5D8Protocol.FirstTargetSampleCount or
            > O5R5D8Protocol.ExpectedSampleCount ||
            measured.Summaries.Count != O5R5D8Protocol.ExpectedSummaryCount ||
            measured.Failures.Count != 1 ||
            measured.Samples.Take(measured.Samples.Count - 1).Any(item => !item.Passed) ||
            measured.Summaries.Any(item => !item.Passed))
        {
            return false;
        }

        O5R5MeasurementSample sample = measured.Samples[^1];
        O5R5ThresholdFailure failure = measured.Failures[0];
        return !sample.Passed &&
            sample.Phase == O5R5MeasurementPhase.Cancellation &&
            sample.Temperature == O5R5Temperature.Cold &&
            failure.Code == WorkingSetFailureCode &&
            failure.Phase == sample.Phase &&
            failure.Temperature == sample.Temperature &&
            failure.IsWarmUp == sample.IsWarmUp &&
            failure.Repetition == sample.Repetition &&
            failure.Metric == O5R5ThresholdMetric.WorkingSetPeak &&
            failure.Unit == O5R5ThresholdUnit.Bytes &&
            failure.InclusiveLimit == O5R5D8Protocol.WorkingSetLimitBytes &&
            failure.Observed == sample.WorkingSetPeakDeltaBytes &&
            failure.Observed > O5R5D8Protocol.WorkingSetLimitBytes &&
            measured.WorkingSetExcessReproduced &&
            measured.Code == WorkingSetFailureCode;
    }

    /// <summary>Determines whether one of the four original summaries stopped at repeatability only.</summary>
    /// <param name="measured">Validated measured report.</param>
    /// <returns>True only for an exact pre-target repeatability summary failure.</returns>
    private static bool IsExactEarlyGateStop(O5R5D6Report measured)
    {
        if (measured.Samples.Count is not (35 or 70 or 105 or 140) ||
            measured.Summaries.Count != measured.Samples.Count / O5R5D8Protocol.GroupSampleCount ||
            measured.Failures.Count != 1 ||
            measured.Samples.Any(item => !item.Passed) ||
            measured.Summaries.Take(measured.Summaries.Count - 1).Any(item => !item.Passed))
        {
            return false;
        }

        O5R5MeasurementSummary summary = measured.Summaries[^1];
        O5R5ThresholdFailure failure = measured.Failures[0];
        return !summary.Passed &&
            summary.Failures.Count == 1 &&
            summary.Failures[0] == failure &&
            failure.Code == RepeatabilityFailureCode &&
            failure.Phase == summary.Phase &&
            failure.Temperature == summary.Temperature &&
            failure.IsWarmUp is null &&
            failure.Repetition is null &&
            failure.Metric == O5R5ThresholdMetric.RepeatabilityCoefficient &&
            failure.Unit == O5R5ThresholdUnit.Ratio &&
            failure.InclusiveLimit == O5R5D8Protocol.RepeatabilityLimit &&
            failure.Observed == summary.RepeatabilityElapsed.CoefficientOfVariation &&
            failure.Observed > O5R5D8Protocol.RepeatabilityLimit &&
            !measured.Complete &&
            !measured.WorkingSetExcessReproduced &&
            measured.Code == RepeatabilityFailureCode;
    }

    /// <summary>Recognises another exact V3 threshold stop without interpreting its cause.</summary>
    /// <param name="measured">Validated measured report.</param>
    /// <returns>True when failures align with the final sample or summary boundary.</returns>
    private static bool IsOtherExactV3Stop(O5R5D6Report measured)
    {
        if (measured.Failures.Count < 1 ||
            measured.WorkingSetExcessReproduced ||
            measured.Code != measured.Failures[0].Code)
        {
            return false;
        }

        if (measured.Samples.Count > 0 && !measured.Samples[^1].Passed)
        {
            O5R5MeasurementSample sample = measured.Samples[^1];
            int expectedSummaries = Math.Min(
                (measured.Samples.Count - 1) / O5R5D8Protocol.GroupSampleCount,
                O5R5D8Protocol.ExpectedSummaryCount);
            return measured.Summaries.Count == expectedSummaries &&
                measured.Samples.Take(measured.Samples.Count - 1).All(item => item.Passed) &&
                measured.Summaries.All(item => item.Passed) &&
                measured.Failures.All(
                    failure =>
                        failure.Phase == sample.Phase &&
                        failure.Temperature == sample.Temperature &&
                        failure.IsWarmUp == sample.IsWarmUp &&
                        failure.Repetition == sample.Repetition &&
                        failure.Observed > failure.InclusiveLimit);
        }

        if (measured.Summaries.Count > 0 && !measured.Summaries[^1].Passed)
        {
            O5R5MeasurementSummary summary = measured.Summaries[^1];
            return measured.Samples.Count ==
                    measured.Summaries.Count * O5R5D8Protocol.GroupSampleCount &&
                measured.Samples.All(item => item.Passed) &&
                measured.Summaries.Take(measured.Summaries.Count - 1).All(item => item.Passed) &&
                summary.Failures.SequenceEqual(measured.Failures) &&
                measured.Failures.All(
                    failure =>
                        failure.Phase == summary.Phase &&
                        failure.Temperature == summary.Temperature &&
                        failure.Observed > failure.InclusiveLimit);
        }

        return false;
    }

    /// <summary>Creates one root envelope with derived counts and non-authorising flags.</summary>
    private static O5R5D8Envelope Create(
        int run,
        string hostBinarySha256,
        string code,
        O5R5D8Disposition disposition,
        bool admissible,
        O5R5D6Report? measured) =>
        new(
            O5R5D8Protocol.Version,
            O5R5D8Protocol.Digest,
            O5R5D8Protocol.V3Digest,
            O5R5D8Protocol.D5Digest,
            O5R5D8Protocol.D6Digest,
            O5R5D8Protocol.D7Digest,
            O5R5D8Protocol.D7R1Digest,
            "None",
            hostBinarySha256,
            run,
            code,
            disposition,
            admissible,
            disposition == O5R5D8Disposition.CompletePrefix,
            disposition is O5R5D8Disposition.CompletePrefix or O5R5D8Disposition.TargetStop,
            disposition == O5R5D8Disposition.TargetStop,
            Authorising: false,
            O5R5D8Protocol.ExpectedSampleCount,
            measured?.Samples.Count ?? 0,
            O5R5D8Protocol.ExpectedSummaryCount,
            measured?.Summaries.Count ?? 0,
            measured);

    /// <summary>Maps one validated disposition to its exact retained code.</summary>
    private static string CodeFor(O5R5D8Disposition disposition) =>
        disposition switch
        {
            O5R5D8Disposition.CompletePrefix => "o5r5d8.prefix.complete",
            O5R5D8Disposition.TargetStop => "o5r5d8.target_stop",
            O5R5D8Disposition.EarlyGateStop => "o5r5d8.early_repeatability_stop",
            O5R5D8Disposition.OtherV3Stop => "o5r5d8.other_v3_stop",
            _ => "o5r5d8.supervisor.failed",
        };

    /// <summary>Checks an upper-case hexadecimal SHA-256 value without accepting alternate encodings.</summary>
    private static bool IsSha256(string value) =>
        value.Length == 64 &&
        value.All(character => character is >= '0' and <= '9' or >= 'A' and <= 'F');

    /// <summary>Provides no metrics because independent prefix validation must not execute a workload.</summary>
    private sealed class UnusedMeasurementSource : IO5R5MeasurementSource
    {
        /// <inheritdoc />
        public long Frequency => 1;

        /// <inheritdoc />
        public O5R5MetricSnapshot Capture() =>
            throw new InvalidOperationException("o5r5d8.validation.source_unused");
    }
}

/// <summary>Restricts, validates and atomically publishes measured and final D8 evidence.</summary>
internal static class O5R5D8EvidenceWriter
{
    private const string RootPrefix = "DBNotifier-PF-OBS-1-D8-";
    private const int MaximumEvidenceBytes = 4_000_000;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Validates one exact final destination below a direct D8 temporary child.</summary>
    /// <param name="path">Candidate final path.</param>
    /// <param name="run">Expected one-based D8 attempt.</param>
    /// <returns>The canonical validated destination.</returns>
    internal static string ValidateFinalDestination(string? path, int run)
    {
        O5R5D8Evidence.ValidateRun(run);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("o5r5d8.output.required");
        }

        string destination = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(destination);
        string temporaryRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        if (directory is null ||
            !string.Equals(
                Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(directory)),
                temporaryRoot,
                StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(directory).StartsWith(RootPrefix, StringComparison.Ordinal) ||
            !string.Equals(
                Path.GetFileName(destination),
                $"unobserved-run-{run}.json",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o5r5d8.output.path_refused");
        }
        return destination;
    }

    /// <summary>Returns the supervisor-owned intermediate measured sibling.</summary>
    /// <param name="finalDestination">Validated final D8 destination.</param>
    /// <returns>The exact unretained measured sibling path.</returns>
    internal static string MeasuredDestination(string finalDestination) =>
        Path.ChangeExtension(finalDestination, ".measured.json");

    /// <summary>Serialises one validated final D8 envelope.</summary>
    /// <param name="envelope">Admissible or blocked D8 evidence.</param>
    /// <returns>Formatted UTF-8 JSON bytes.</returns>
    internal static byte[] Serialise(O5R5D8Envelope envelope)
    {
        O5R5D8Evidence.Validate(envelope);
        return JsonSerializer.SerializeToUtf8Bytes(envelope, Options);
    }

    /// <summary>Deserialises root count fields and validates the full D8 contract.</summary>
    /// <param name="bytes">Bounded UTF-8 JSON evidence.</param>
    /// <returns>The validated typed D8 envelope.</returns>
    internal static O5R5D8Envelope Deserialise(ReadOnlySpan<byte> bytes)
    {
        ValidateSize(bytes);
        ValidateRawCounts(bytes);
        O5R5D8Envelope envelope =
            JsonSerializer.Deserialize<O5R5D8Envelope>(bytes, Options) ??
            throw new InvalidOperationException("o5r5d8.evidence.invalid");
        O5R5D8Evidence.Validate(envelope);
        return envelope;
    }

    /// <summary>Reads one final D8 report through raw and typed validation.</summary>
    /// <param name="destination">Exact final D8 path.</param>
    /// <returns>The validated typed D8 envelope.</returns>
    internal static O5R5D8Envelope ReadFinal(string destination) =>
        Deserialise(File.ReadAllBytes(destination));

    /// <summary>Writes one unchanged measured D6 report to the private intermediate sibling.</summary>
    /// <param name="destination">Supervisor-derived measured path.</param>
    /// <param name="report">Complete or stopped D6 prefix report.</param>
    /// <param name="cancellationToken">Cancellation requested before publication.</param>
    internal static Task WriteMeasuredAsync(
        string destination,
        O5R5D6Report report,
        CancellationToken cancellationToken) =>
        WriteAtomicallyAsync(
            destination,
            JsonSerializer.SerializeToUtf8Bytes(report, Options),
            cancellationToken);

    /// <summary>Reads one bounded unchanged measured D6 report.</summary>
    /// <param name="destination">Supervisor-derived intermediate path.</param>
    /// <returns>The typed measured report.</returns>
    internal static O5R5D6Report ReadMeasured(string destination)
    {
        byte[] bytes = File.ReadAllBytes(destination);
        ValidateSize(bytes);
        return JsonSerializer.Deserialize<O5R5D6Report>(bytes, Options) ??
            throw new InvalidOperationException("o5r5d8.measured.invalid");
    }

    /// <summary>Atomically publishes one final D8 report after exact semantic validation.</summary>
    /// <param name="destination">Validated final D8 path.</param>
    /// <param name="envelope">Admissible or fail-closed evidence.</param>
    /// <param name="cancellationToken">Cancellation requested before publication.</param>
    internal static Task WriteFinalAsync(
        string destination,
        O5R5D8Envelope envelope,
        CancellationToken cancellationToken) =>
        WriteAtomicallyAsync(destination, Serialise(envelope), cancellationToken);

    /// <summary>Durably writes through a write-through sibling followed by an atomic move.</summary>
    private static async Task WriteAtomicallyAsync(
        string destination,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
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

    /// <summary>Rejects evidence outside the fixed bounded JSON size.</summary>
    private static void ValidateSize(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is < 1 or > MaximumEvidenceBytes)
        {
            throw new InvalidOperationException("o5r5d8.evidence.size_invalid");
        }
    }

    /// <summary>Rejects missing, duplicate, mistyped, negative, excessive or inconsistent root counts.</summary>
    private static void ValidateRawCounts(ReadOnlySpan<byte> bytes)
    {
        Dictionary<string, int> occurrences = new(StringComparer.Ordinal);
        Dictionary<string, int> values = new(StringComparer.Ordinal);
        string[] names =
        [
            "expectedSampleCount",
            "completedSampleCount",
            "expectedSummaryCount",
            "completedSummaryCount",
        ];
        Utf8JsonReader reader = new(
            bytes,
            new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 128,
            });
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName ||
                reader.CurrentDepth != 1)
            {
                continue;
            }
            string? name = null;
            foreach (string candidate in names)
            {
                if (reader.ValueTextEquals(candidate))
                {
                    name = candidate;
                    break;
                }
            }
            if (name is null)
            {
                continue;
            }
            if (!reader.Read() ||
                reader.TokenType != JsonTokenType.Number ||
                !reader.TryGetInt32(out int value))
            {
                throw new InvalidOperationException("o5r5d8.evidence.count_type_invalid");
            }
            occurrences[name] = occurrences.GetValueOrDefault(name) + 1;
            values[name] = value;
        }

        if (names.Any(name => occurrences.GetValueOrDefault(name) != 1) ||
            values["expectedSampleCount"] != O5R5D8Protocol.ExpectedSampleCount ||
            values["expectedSummaryCount"] != O5R5D8Protocol.ExpectedSummaryCount ||
            values["completedSampleCount"] is < 0 or > O5R5D8Protocol.ExpectedSampleCount ||
            values["completedSummaryCount"] is < 0 or > O5R5D8Protocol.ExpectedSummaryCount)
        {
            throw new InvalidOperationException("o5r5d8.evidence.count_invalid");
        }
    }
}

/// <summary>Exposes the marker-gated D8 measured child over the unchanged D6 prefix runner.</summary>
public static class O5R5D8MeasuredProcess
{
    internal const string ActivationMarker = "pf-obs-1-d8-measured-test-only";
    private static readonly TimeSpan ProcessDeadline = TimeSpan.FromMinutes(5);

    /// <summary>Runs one exact prefix and publishes only the private measured sibling.</summary>
    /// <param name="args">Exact activation, output, run and sanitised environment arguments.</param>
    /// <returns>Zero after measured publication, two for invalid activation or three for failure.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (!O5R5D8Arguments.TryParse(args, ActivationMarker, out O5R5D8Arguments? parsed))
        {
            Console.Error.WriteLine("o5r5d8.measured.activation_invalid");
            return 2;
        }

        try
        {
            string final = O5R5D8EvidenceWriter.ValidateFinalDestination(parsed.Output, parsed.Run);
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
            O5R5D6Runner runner = new(source);
            using CancellationTokenSource deadline = new(ProcessDeadline);
            O5R5D6Report measured = await runner
                .RunAsync(parsed.Run, environment, deadline.Token)
                .ConfigureAwait(false);
            await O5R5D8EvidenceWriter
                .WriteMeasuredAsync(
                    O5R5D8EvidenceWriter.MeasuredDestination(final),
                    measured,
                    CancellationToken.None)
                .ConfigureAwait(false);
            Console.WriteLine(measured.Code);
            return 0;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"o5r5d8.measured.failed:{exception.GetType().Name}");
            return 3;
        }
    }
}

/// <summary>Exposes the sole D8 fresh-child launcher and independent final publication boundary.</summary>
public static class O5R5D8SupervisorProcess
{
    internal const string ActivationMarker = "pf-obs-1-d8-supervisor-test-only";
    private static readonly TimeSpan ProcessDeadline = TimeSpan.FromMinutes(5);

    /// <summary>Runs one fresh D8 child, validates its exact boundary and publishes final evidence.</summary>
    /// <param name="args">Exact activation, output, run and sanitised environment arguments.</param>
    /// <returns>Zero for admissible evidence, two for invalid activation or three for blocked evidence.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (!O5R5D8Arguments.TryParse(args, ActivationMarker, out O5R5D8Arguments? parsed))
        {
            Console.Error.WriteLine("o5r5d8.supervisor.activation_invalid");
            return 2;
        }

        string? finalDestination = null;
        string? measuredDestination = null;
        string hostBinarySha256 = new('0', 64);
        try
        {
            finalDestination = O5R5D8EvidenceWriter.ValidateFinalDestination(
                parsed.Output,
                parsed.Run);
            measuredDestination = O5R5D8EvidenceWriter.MeasuredDestination(finalDestination);
            if (File.Exists(finalDestination) || File.Exists(measuredDestination))
            {
                throw new InvalidOperationException("o5r5d8.output.exists");
            }

            string entryAssembly = Assembly.GetEntryAssembly()?.Location ??
                throw new InvalidOperationException("o5r5d8.supervisor.entry_missing");
            hostBinarySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(entryAssembly)));
            using Process child = StartMeasuredChild(parsed, entryAssembly);
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

            if (child.ExitCode != 0 || !File.Exists(measuredDestination))
            {
                throw new InvalidOperationException("o5r5d8.measured.missing_or_failed");
            }
            O5R5D6Report measured = O5R5D8EvidenceWriter.ReadMeasured(measuredDestination);
            O5R5D8Envelope envelope = O5R5D8Evidence.Finalise(measured, hostBinarySha256);
            await O5R5D8EvidenceWriter
                .WriteFinalAsync(finalDestination, envelope, CancellationToken.None)
                .ConfigureAwait(false);
            Console.WriteLine(envelope.Code);
            return 0;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (finalDestination is not null && !File.Exists(finalDestination))
            {
                try
                {
                    O5R5D8Envelope blocked = O5R5D8Evidence.Blocked(
                        parsed.Run,
                        hostBinarySha256);
                    await O5R5D8EvidenceWriter
                        .WriteFinalAsync(finalDestination, blocked, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (Exception publicationException) when (
                    publicationException is not OutOfMemoryException)
                {
                    Console.Error.WriteLine(
                        $"o5r5d8.publication.failed:{publicationException.GetType().Name}");
                }
            }
            Console.Error.WriteLine($"o5r5d8.supervisor.failed:{exception.GetType().Name}");
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

    /// <summary>Starts the current sandbox host as one exact fresh unobserved D8 child.</summary>
    /// <param name="parsed">Validated D8 process arguments.</param>
    /// <param name="entryAssembly">Exact sandbox-host assembly already hashed by the supervisor.</param>
    /// <returns>The newly started measured child.</returns>
    private static Process StartMeasuredChild(O5R5D8Arguments parsed, string entryAssembly)
    {
        string processPath = Environment.ProcessPath ??
            throw new InvalidOperationException("o5r5d8.supervisor.process_path_missing");
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
        start.ArgumentList.Add(O5R5D8MeasuredProcess.ActivationMarker);
        start.ArgumentList.Add("--output");
        start.ArgumentList.Add(parsed.Output);
        start.ArgumentList.Add("--run");
        start.ArgumentList.Add(parsed.Run.ToString(CultureInfo.InvariantCulture));
        start.ArgumentList.Add("--sdk");
        start.ArgumentList.Add(parsed.Sdk);
        start.ArgumentList.Add("--installed-memory");
        start.ArgumentList.Add(parsed.InstalledMemory.ToString(CultureInfo.InvariantCulture));
        return Process.Start(start) ??
            throw new InvalidOperationException("o5r5d8.supervisor.start_failed");
    }
}

/// <summary>Represents exact validated D8 process arguments.</summary>
internal sealed record O5R5D8Arguments(
    string Output,
    int Run,
    string Sdk,
    long InstalledMemory)
{
    /// <summary>Parses one exact marker-gated D8 argument sequence.</summary>
    /// <param name="args">Complete process argument sequence.</param>
    /// <param name="marker">Exact marker expected by the selected D8 entrypoint.</param>
    /// <param name="parsed">Validated bounded arguments when parsing succeeds.</param>
    /// <returns>True only when every argument and value is exact and bounded.</returns>
    internal static bool TryParse(
        string[] args,
        string marker,
        [NotNullWhen(true)] out O5R5D8Arguments? parsed)
    {
        parsed = null;
        if (args.Length != 10 ||
            args[0] != "--activation" ||
            args[1] != marker ||
            args[2] != "--output" ||
            args[4] != "--run" ||
            !int.TryParse(args[5], NumberStyles.None, CultureInfo.InvariantCulture, out int run) ||
            args[6] != "--sdk" ||
            string.IsNullOrWhiteSpace(args[7]) ||
            args[7].Length > 32 ||
            args[7] != args[7].Trim() ||
            args[8] != "--installed-memory" ||
            !long.TryParse(
                args[9],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long installedMemory) ||
            installedMemory < 1)
        {
            return false;
        }
        try
        {
            O5R5D8Evidence.ValidateRun(run);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
        parsed = new O5R5D8Arguments(args[3], run, args[7], installedMemory);
        return true;
    }
}
