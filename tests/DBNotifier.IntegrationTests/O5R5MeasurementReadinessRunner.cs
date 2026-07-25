// Module purpose: Defines the frozen O5-R5-A protocol and its isolated, non-authorising physical-measurement runner.
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DBNotifier.IntegrationTests;

/// <summary>Identifies the exact liveness and deterministic-compute phases admitted by O5-R5-A.</summary>
internal enum O5R5MeasurementPhase
{
    FirstByte = 1,
    Idle = 2,
    Cancellation = 3,
    ControlUpdate = 4,
    Parse = 5,
    Cryptography = 6,
    Sort = 7,
    Analysis = 8,
}

/// <summary>Classifies a measured sample as a cold or warm physical run.</summary>
internal enum O5R5Temperature
{
    Cold = 1,
    Warm = 2,
}

/// <summary>Classifies whether the runner produced one complete sample or failed closed.</summary>
internal enum O5R5MeasurementDisposition
{
    Accepted = 1,
    Refused = 2,
}

/// <summary>Defines one immutable phase threshold frozen before any physical result exists.</summary>
/// <param name="Phase">Exact phase governed by the limit.</param>
/// <param name="MaximumElapsed">Inclusive absolute elapsed-time limit.</param>
/// <param name="MeasuresWorkRate">Whether CPU and elapsed cost per deterministic work unit apply.</param>
internal sealed record O5R5PhaseLimit(
    O5R5MeasurementPhase Phase,
    TimeSpan MaximumElapsed,
    bool MeasuresWorkRate);

/// <summary>
/// Owns the content-addressed O5-R5-A protocol, finite resource envelope and preregistered
/// statistical rules.
/// </summary>
internal sealed class O5R5MeasurementProtocol
{
    internal const string Version = "pfobs1-physical-measurement-2.0.0";
    internal const int WarmUpRepetitions = 5;
    internal const int MeasuredRepetitions = 30;
    internal const int RepeatabilityGroupCount = 5;
    internal const int SamplesPerRepeatabilityGroup = 6;
    internal const int RequiredConsecutiveCampaigns = 2;
    internal const int MaximumCheckpoints = 64;
    internal const int Percentile50 = 50;
    internal const int Percentile95 = 95;
    internal const int Percentile99 = 99;
    internal const int MemoryHeadroomNumerator = 3;
    internal const int MemoryHeadroomDenominator = 2;
    internal const double MaximumCoefficientOfVariation = 0.20d;
    internal const double MaximumElapsedMicrosecondsPerWorkUnit = 100d;
    internal const double MaximumCpuMicrosecondsPerWorkUnit = 100d;
    internal static readonly TimeSpan SchedulingAllowance = TimeSpan.FromMilliseconds(250);

    private readonly ReadOnlyDictionary<O5R5MeasurementPhase, O5R5PhaseLimit> phaseLimits;

    /// <summary>Initialises and validates the immutable preregistered protocol.</summary>
    /// <param name="envelope">Accepted O1 resource envelope reproduced without enlargement.</param>
    /// <param name="limits">Complete exact phase-limit catalogue.</param>
    /// <exception cref="InvalidOperationException">Thrown when the protocol is incomplete or unsafe.</exception>
    internal O5R5MeasurementProtocol(
        O1ResourceEnvelope envelope,
        IReadOnlyCollection<O5R5PhaseLimit> limits)
    {
        Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        ArgumentNullException.ThrowIfNull(limits);
        O5R5PhaseLimit?[] bounded = limits.Take(Enum.GetValues<O5R5MeasurementPhase>().Length + 1).ToArray();
        if (bounded.Length != Enum.GetValues<O5R5MeasurementPhase>().Length ||
            bounded.Any(limit => limit is null) ||
            bounded.Select(limit => limit!.Phase).Distinct().Count() != bounded.Length)
        {
            throw new InvalidOperationException("o5r5a.protocol.phase_catalogue_invalid");
        }

        phaseLimits = new ReadOnlyDictionary<O5R5MeasurementPhase, O5R5PhaseLimit>(
            bounded.ToDictionary(limit => limit!.Phase, limit => limit!));
        Validate();
        CanonicalRepresentation = BuildCanonicalRepresentation();
        Digest = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalRepresentation)));
    }

    /// <summary>Gets the unchanged finite O1 resource envelope.</summary>
    internal O1ResourceEnvelope Envelope { get; }

    /// <summary>Gets every phase threshold in enum order.</summary>
    internal IReadOnlyDictionary<O5R5MeasurementPhase, O5R5PhaseLimit> PhaseLimits => phaseLimits;

    /// <summary>Gets the stable canonical representation used for content addressing.</summary>
    internal string CanonicalRepresentation { get; }

    /// <summary>Gets the uppercase SHA-256 digest of the complete canonical protocol.</summary>
    internal string Digest { get; }

    /// <summary>Creates the sole frozen O5-R5-A protocol from already accepted O1 and O5-R3 limits.</summary>
    /// <returns>A complete content-addressed protocol.</returns>
    internal static O5R5MeasurementProtocol CreateFrozen()
    {
        O1ResourceEnvelope envelope = O1ResourceEnvelope.Fixture();
        TimeSpan containment = TimeSpan.FromMilliseconds(
            O5R3ObservabilityCatalog
                .CreateDefault()
                .SloFor(O5R3ExercisePhase.Containment)
                .MaximumElapsedMilliseconds);
        return new O5R5MeasurementProtocol(
            envelope,
            new O5R5PhaseLimit[]
            {
                new(
                    O5R5MeasurementPhase.FirstByte,
                    envelope.FirstByteDuration + SchedulingAllowance,
                    false),
                new(
                    O5R5MeasurementPhase.Idle,
                    envelope.IdleDuration + SchedulingAllowance,
                    false),
                new(O5R5MeasurementPhase.Cancellation, containment, false),
                new(O5R5MeasurementPhase.ControlUpdate, containment, false),
                new(O5R5MeasurementPhase.Parse, envelope.TotalDuration, true),
                new(O5R5MeasurementPhase.Cryptography, envelope.TotalDuration, true),
                new(O5R5MeasurementPhase.Sort, envelope.TotalDuration, true),
                new(O5R5MeasurementPhase.Analysis, envelope.TotalDuration, true),
            });
    }

    /// <summary>Gets the exact threshold for one allow-listed phase.</summary>
    /// <param name="phase">Exact measurement phase.</param>
    /// <returns>The immutable preregistered phase limit.</returns>
    internal O5R5PhaseLimit LimitFor(O5R5MeasurementPhase phase) =>
        phaseLimits.TryGetValue(phase, out O5R5PhaseLimit? limit)
            ? limit
            : throw new InvalidOperationException("o5r5a.protocol.phase_unknown");

    /// <summary>Calculates the fixed 50% empirical memory ceiling with checked, upward-rounded arithmetic.</summary>
    /// <param name="accountedMemoryBytes">Declared deterministic accounted-memory reservation.</param>
    /// <returns>The inclusive empirical byte ceiling.</returns>
    internal long EmpiricalMemoryLimit(long accountedMemoryBytes)
    {
        if (accountedMemoryBytes < 1 || accountedMemoryBytes > Envelope.MaximumAccountedMemoryBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(accountedMemoryBytes));
        }

        return checked(
            ((accountedMemoryBytes * MemoryHeadroomNumerator) +
                (MemoryHeadroomDenominator - 1)) /
            MemoryHeadroomDenominator);
    }

    /// <summary>Validates every frozen structural, resource and statistical invariant.</summary>
    /// <exception cref="InvalidOperationException">Thrown when any invariant is missing or enlarged.</exception>
    private void Validate()
    {
        if (Envelope != O1ResourceEnvelope.Fixture() ||
            Envelope.MaximumParallelism != 1 ||
            Envelope.QueueEnabled ||
            WarmUpRepetitions != 5 ||
            MeasuredRepetitions != 30 ||
            RepeatabilityGroupCount != 5 ||
            SamplesPerRepeatabilityGroup != 6 ||
            RepeatabilityGroupCount * SamplesPerRepeatabilityGroup != MeasuredRepetitions ||
            RequiredConsecutiveCampaigns != 2 ||
            MaximumCheckpoints != 64 ||
            MaximumElapsedMicrosecondsPerWorkUnit <= 0d ||
            MaximumCpuMicrosecondsPerWorkUnit <= 0d)
        {
            throw new InvalidOperationException("o5r5a.protocol.envelope_invalid");
        }

        foreach (O5R5MeasurementPhase phase in Enum.GetValues<O5R5MeasurementPhase>())
        {
            O5R5PhaseLimit limit = LimitFor(phase);
            if (limit.MaximumElapsed <= TimeSpan.Zero ||
                limit.MaximumElapsed > Envelope.TotalDuration ||
                limit.MeasuresWorkRate != IsComputePhase(phase))
            {
                throw new InvalidOperationException("o5r5a.protocol.phase_limit_invalid");
            }
        }
    }

    /// <summary>Builds the stable line-oriented canonical protocol representation.</summary>
    /// <returns>Canonical UTF-8 text whose digest freezes every material limit.</returns>
    private string BuildCanonicalRepresentation()
    {
        List<string> lines =
        [
            $"version={Version}",
            $"warm_up_repetitions={WarmUpRepetitions}",
            $"measured_repetitions={MeasuredRepetitions}",
            "repeatability_method=fixed-sequential-groups-all-samples-median",
            $"repeatability_group_count={RepeatabilityGroupCount}",
            $"samples_per_repeatability_group={SamplesPerRepeatabilityGroup}",
            $"required_consecutive_campaigns={RequiredConsecutiveCampaigns}",
            $"maximum_checkpoints={MaximumCheckpoints}",
            $"percentiles={Percentile50},{Percentile95},{Percentile99},100",
            $"maximum_coefficient_of_variation={MaximumCoefficientOfVariation.ToString("F2", CultureInfo.InvariantCulture)}",
            $"memory_headroom={MemoryHeadroomNumerator}/{MemoryHeadroomDenominator}",
            $"maximum_elapsed_microseconds_per_work_unit={MaximumElapsedMicrosecondsPerWorkUnit:F0}",
            $"maximum_cpu_microseconds_per_work_unit={MaximumCpuMicrosecondsPerWorkUnit:F0}",
            $"maximum_input_bytes={Envelope.MaximumInputBytes}",
            $"maximum_structure_depth={Envelope.MaximumStructureDepth}",
            $"maximum_items={Envelope.MaximumItems}",
            $"maximum_accounted_memory_bytes={Envelope.MaximumAccountedMemoryBytes}",
            $"maximum_work_units={Envelope.MaximumWorkUnits}",
            $"maximum_results={Envelope.MaximumResults}",
            $"maximum_output_bytes={Envelope.MaximumOutputBytes}",
            $"maximum_control_metadata_entries={Envelope.MaximumControlMetadataEntries}",
            $"total_duration_ticks={Envelope.TotalDuration.Ticks}",
            $"first_byte_duration_ticks={Envelope.FirstByteDuration.Ticks}",
            $"idle_duration_ticks={Envelope.IdleDuration.Ticks}",
            $"maximum_parallelism={Envelope.MaximumParallelism}",
            $"queue_enabled={Envelope.QueueEnabled.ToString().ToLowerInvariant()}",
        ];
        lines.AddRange(
            Enum.GetValues<O5R5MeasurementPhase>()
                .Select(
                    phase =>
                    {
                        O5R5PhaseLimit limit = LimitFor(phase);
                        return string.Join(
                            '|',
                            $"phase={PhaseCode(phase)}",
                            $"maximum_elapsed_ticks={limit.MaximumElapsed.Ticks}",
                            $"measures_work_rate={limit.MeasuresWorkRate.ToString().ToLowerInvariant()}");
                    }));
        return string.Join('\n', lines);
    }

    /// <summary>Returns whether a phase participates in deterministic CPU/work calibration.</summary>
    /// <param name="phase">Candidate phase.</param>
    /// <returns><see langword="true"/> only for parse, cryptography, sort and analysis.</returns>
    internal static bool IsComputePhase(O5R5MeasurementPhase phase) =>
        phase is
            O5R5MeasurementPhase.Parse or
            O5R5MeasurementPhase.Cryptography or
            O5R5MeasurementPhase.Sort or
            O5R5MeasurementPhase.Analysis;

    /// <summary>Maps a phase to its stable sanitised output code.</summary>
    /// <param name="phase">Exact phase.</param>
    /// <returns>Stable lower-case phase code.</returns>
    internal static string PhaseCode(O5R5MeasurementPhase phase) =>
        phase switch
        {
            O5R5MeasurementPhase.FirstByte => "first-byte",
            O5R5MeasurementPhase.Idle => "idle",
            O5R5MeasurementPhase.Cancellation => "cancellation",
            O5R5MeasurementPhase.ControlUpdate => "control-update",
            O5R5MeasurementPhase.Parse => "parse",
            O5R5MeasurementPhase.Cryptography => "cryptography",
            O5R5MeasurementPhase.Sort => "sort",
            O5R5MeasurementPhase.Analysis => "analysis",
            _ => throw new ArgumentOutOfRangeException(nameof(phase)),
        };
}

/// <summary>Captures one monotonic and resource-counter snapshot without payload or machine identity.</summary>
/// <param name="MonotonicTimestamp">Timestamp expressed in the source frequency.</param>
/// <param name="HeapBytes">Current managed heap size.</param>
/// <param name="TotalAllocatedBytes">Process-wide cumulative managed allocation.</param>
/// <param name="WorkingSetBytes">Current process working set.</param>
/// <param name="CpuTime">Current total process CPU time.</param>
internal sealed record O5R5MetricSnapshot(
    long MonotonicTimestamp,
    long HeapBytes,
    long TotalAllocatedBytes,
    long WorkingSetBytes,
    TimeSpan CpuTime);

/// <summary>Supplies monotonic time and built-in .NET/Windows resource counters to the isolated runner.</summary>
internal interface IO5R5MeasurementSource
{
    /// <summary>Gets monotonic timestamp ticks per second.</summary>
    long Frequency { get; }

    /// <summary>Captures one complete resource snapshot or throws before a sample can be published.</summary>
    /// <returns>One sanitised metric snapshot.</returns>
    O5R5MetricSnapshot Capture();
}

/// <summary>Uses only built-in .NET and Windows process APIs for a later separately authorised physical campaign.</summary>
internal sealed class O5R5DotNetMeasurementSource : IO5R5MeasurementSource, IDisposable
{
    private readonly Process process = Process.GetCurrentProcess();

    /// <inheritdoc />
    public long Frequency => Stopwatch.Frequency;

    /// <inheritdoc />
    public O5R5MetricSnapshot Capture()
    {
        long timestamp = Stopwatch.GetTimestamp();
        GCMemoryInfo memory = GC.GetGCMemoryInfo();
        long workingSet = Environment.WorkingSet;
        TimeSpan cpu = process.TotalProcessorTime;
        long allocated = GC.GetTotalAllocatedBytes(precise: false);
        return new O5R5MetricSnapshot(
            timestamp,
            memory.HeapSizeBytes,
            allocated,
            workingSet,
            cpu);
    }

    /// <summary>Releases the single cached operating-system process handle after the physical campaign.</summary>
    public void Dispose() => process.Dispose();
}

/// <summary>Allows an authorised scenario to request bounded intermediate allocation and memory checkpoints.</summary>
internal sealed class O5R5MeasurementContext
{
    private readonly IO5R5MeasurementSource source;
    private readonly List<O5R5MetricSnapshot> checkpoints = [];

    /// <summary>Initialises a bounded context over the exact runner measurement source.</summary>
    /// <param name="source">Source shared with the owning runner.</param>
    internal O5R5MeasurementContext(IO5R5MeasurementSource source) =>
        this.source = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>Gets the captured intermediate checkpoints.</summary>
    internal IReadOnlyList<O5R5MetricSnapshot> Checkpoints => checkpoints;

    /// <summary>Captures one intermediate resource checkpoint and refuses unbounded sampling.</summary>
    /// <exception cref="InvalidOperationException">Thrown after the frozen checkpoint capacity is reached.</exception>
    internal void CaptureCheckpoint()
    {
        if (checkpoints.Count >= O5R5MeasurementProtocol.MaximumCheckpoints)
        {
            throw new InvalidOperationException("o5r5a.measurement.checkpoint_capacity");
        }

        checkpoints.Add(source.Capture());
    }
}

/// <summary>Describes one exact synthetic or future physical phase invocation.</summary>
/// <param name="Phase">Exact phase label.</param>
/// <param name="Temperature">Cold or warm classification.</param>
/// <param name="IsWarmUp">Whether the sample is excluded from statistics.</param>
/// <param name="Repetition">One-based repetition within the applicable class.</param>
/// <param name="DeclaredWorkUnits">Deterministic work units declared before execution.</param>
/// <param name="AccountedMemoryBytes">Accounted-memory reservation declared before execution.</param>
/// <param name="Operation">Bounded operation that may capture intermediate checkpoints.</param>
internal sealed record O5R5MeasurementScenario(
    O5R5MeasurementPhase Phase,
    O5R5Temperature Temperature,
    bool IsWarmUp,
    int Repetition,
    long DeclaredWorkUnits,
    long AccountedMemoryBytes,
    Func<O5R5MeasurementContext, CancellationToken, ValueTask> Operation);

/// <summary>Reports one complete phase-labelled sample without granting evaluation or publication authority.</summary>
/// <param name="ProtocolDigest">Exact frozen protocol digest.</param>
/// <param name="Phase">Measured phase.</param>
/// <param name="Temperature">Cold or warm classification.</param>
/// <param name="IsWarmUp">Whether the sample is excluded from statistics.</param>
/// <param name="Repetition">One-based repetition.</param>
/// <param name="ElapsedMilliseconds">Monotonic phase duration.</param>
/// <param name="CpuMilliseconds">Process CPU-time delta.</param>
/// <param name="HeapPeakDeltaBytes">Peak managed-heap delta from baseline.</param>
/// <param name="WorkingSetPeakDeltaBytes">Peak working-set delta from baseline.</param>
/// <param name="AllocationPeakBytes">Largest allocation delta between bounded checkpoints.</param>
/// <param name="CumulativeAllocatedBytes">Total allocation delta across the phase.</param>
/// <param name="DeclaredWorkUnits">Predeclared deterministic work units.</param>
/// <param name="AccountedMemoryBytes">Predeclared accounted-memory reservation.</param>
/// <param name="ElapsedMicrosecondsPerWorkUnit">Elapsed work cost for compute phases.</param>
/// <param name="CpuMicrosecondsPerWorkUnit">CPU work cost for compute phases.</param>
/// <param name="Passed">Whether every applicable preregistered threshold passed.</param>
internal sealed record O5R5MeasurementSample(
    string ProtocolDigest,
    O5R5MeasurementPhase Phase,
    O5R5Temperature Temperature,
    bool IsWarmUp,
    int Repetition,
    double ElapsedMilliseconds,
    double CpuMilliseconds,
    long HeapPeakDeltaBytes,
    long WorkingSetPeakDeltaBytes,
    long AllocationPeakBytes,
    long CumulativeAllocatedBytes,
    long DeclaredWorkUnits,
    long AccountedMemoryBytes,
    double? ElapsedMicrosecondsPerWorkUnit,
    double? CpuMicrosecondsPerWorkUnit,
    bool Passed);

/// <summary>Returns a typed non-authorising runner outcome and optional complete sample.</summary>
/// <param name="Disposition">Accepted or refused disposition.</param>
/// <param name="Code">Stable sanitised diagnostic code.</param>
/// <param name="Sample">Complete sample only after safe measurement.</param>
internal sealed record O5R5MeasurementResult(
    O5R5MeasurementDisposition Disposition,
    string Code,
    O5R5MeasurementSample? Sample)
{
    /// <summary>Gets a value that is always false because O5-R5-A cannot evaluate Observer evidence.</summary>
    internal static bool MayEvaluate => false;

    /// <summary>Gets a value that is always false because O5-R5-A cannot publish Observer output.</summary>
    internal static bool MayPublish => false;
}

/// <summary>Summarises one retained metric distribution without dropping outliers.</summary>
/// <param name="P50">Nearest-rank fiftieth percentile.</param>
/// <param name="P95">Nearest-rank ninety-fifth percentile.</param>
/// <param name="P99">Nearest-rank ninety-ninth percentile.</param>
/// <param name="Maximum">Worst observed value.</param>
/// <param name="Mean">Arithmetic mean.</param>
/// <param name="PopulationStandardDeviation">Population standard deviation.</param>
/// <param name="CoefficientOfVariation">Standard deviation divided by the non-zero mean.</param>
internal sealed record O5R5MetricDistribution(
    double P50,
    double P95,
    double P99,
    double Maximum,
    double Mean,
    double PopulationStandardDeviation,
    double CoefficientOfVariation);

/// <summary>Reports one exact phase/temperature batch after warm-up separation and completeness checks.</summary>
/// <param name="Code">Stable accepted or failed summary code.</param>
/// <param name="Phase">Single exact phase represented by the batch.</param>
/// <param name="Temperature">Single exact temperature represented by the batch.</param>
/// <param name="WarmUpCount">Exact excluded warm-up count.</param>
/// <param name="MeasuredCount">Exact retained measured count.</param>
/// <param name="Elapsed">Elapsed-time distribution.</param>
/// <param name="Cpu">CPU-time distribution.</param>
/// <param name="HeapPeak">Managed-heap peak-delta distribution.</param>
/// <param name="WorkingSetPeak">Working-set peak-delta distribution.</param>
/// <param name="AllocationPeak">Checkpoint allocation-peak distribution.</param>
/// <param name="ElapsedPerWorkUnit">Elapsed work-cost distribution for compute phases.</param>
/// <param name="CpuPerWorkUnit">CPU work-cost distribution for compute phases.</param>
/// <param name="RepeatabilityElapsed">Distribution of five predeclared sequential-group medians.</param>
/// <param name="Passed">Whether completeness, thresholds and variance all passed.</param>
internal sealed record O5R5MeasurementSummary(
    string Code,
    O5R5MeasurementPhase Phase,
    O5R5Temperature Temperature,
    int WarmUpCount,
    int MeasuredCount,
    O5R5MetricDistribution Elapsed,
    O5R5MetricDistribution Cpu,
    O5R5MetricDistribution HeapPeak,
    O5R5MetricDistribution WorkingSetPeak,
    O5R5MetricDistribution AllocationPeak,
    O5R5MetricDistribution? ElapsedPerWorkUnit,
    O5R5MetricDistribution? CpuPerWorkUnit,
    O5R5MetricDistribution RepeatabilityElapsed,
    bool Passed)
{
    /// <summary>Gets a value that is always false because statistical evidence cannot authorise activation.</summary>
    internal static bool IsAuthorising => false;
}

/// <summary>
/// Executes one serial, marker-gated phase sample and calculates only preregistered, sanitised
/// physical metrics.
/// </summary>
internal sealed class O5R5MeasurementReadinessRunner
{
    internal const string Marker = "DBNOTIFIER_O5_R5_A_TEST_ONLY";

    private readonly O5R5MeasurementProtocol protocol;
    private readonly IO5R5MeasurementSource source;
    private int active;

    /// <summary>Initialises the runner only for the exact O5-R5-A test marker and frozen protocol.</summary>
    /// <param name="marker">Exact test-only opt-in marker.</param>
    /// <param name="protocol">Frozen content-addressed protocol.</param>
    /// <param name="source">Synthetic source now or built-in .NET source in a later authorised campaign.</param>
    /// <exception cref="InvalidOperationException">Thrown when marker or protocol identity differs.</exception>
    internal O5R5MeasurementReadinessRunner(
        string marker,
        O5R5MeasurementProtocol protocol,
        IO5R5MeasurementSource source)
    {
        if (!string.Equals(marker, Marker, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o5r5a.runner.marker_required");
        }

        this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        O5R5MeasurementProtocol frozen = O5R5MeasurementProtocol.CreateFrozen();
        if (!string.Equals(protocol.Digest, frozen.Digest, StringComparison.Ordinal) ||
            !string.Equals(protocol.CanonicalRepresentation, frozen.CanonicalRepresentation, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o5r5a.runner.protocol_mismatch");
        }
    }

    /// <summary>Runs one bounded sample or returns a stable refusal without partial evidence.</summary>
    /// <param name="scenario">Predeclared exact phase and resource demand.</param>
    /// <param name="cancellationToken">Cancellation observed before and after every measurement boundary.</param>
    /// <returns>A complete sample or typed refusal.</returns>
    internal async ValueTask<O5R5MeasurementResult> RunAsync(
        O5R5MeasurementScenario scenario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (Interlocked.CompareExchange(ref active, 1, 0) != 0)
        {
            return Refused("o5r5a.runner.saturated");
        }

        try
        {
            string? invalid = ValidateScenario(scenario);
            if (invalid is not null)
            {
                return Refused(invalid);
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return Refused("o5r5a.runner.cancelled");
            }

            O5R5MetricSnapshot before;
            try
            {
                before = source.Capture();
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                return Refused("o5r5a.measurement.source_unavailable");
            }

            O5R5MeasurementContext context = new(source);
            try
            {
                await scenario.Operation(context, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Refused("o5r5a.runner.cancelled");
            }
            catch (InvalidOperationException exception)
                when (string.Equals(
                    exception.Message,
                    "o5r5a.measurement.checkpoint_capacity",
                    StringComparison.Ordinal))
            {
                return Refused(exception.Message);
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                return Refused("o5r5a.measurement.operation_failed");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Refused("o5r5a.runner.cancelled");
            }

            O5R5MetricSnapshot after;
            try
            {
                after = source.Capture();
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                return Refused("o5r5a.measurement.source_unavailable");
            }

            return BuildResult(scenario, before, context.Checkpoints, after);
        }
        finally
        {
            Volatile.Write(ref active, 0);
        }
    }

    /// <summary>Summarises one complete phase/temperature batch with no outlier removal.</summary>
    /// <param name="samples">Exactly five warm-ups and thirty retained measurements.</param>
    /// <returns>A validated metric summary.</returns>
    /// <exception cref="InvalidOperationException">Thrown for incomplete, mixed or duplicate batches.</exception>
    internal O5R5MeasurementSummary Summarise(IReadOnlyCollection<O5R5MeasurementSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        O5R5MeasurementSample?[] bounded = samples
            .Take(O5R5MeasurementProtocol.WarmUpRepetitions +
                O5R5MeasurementProtocol.MeasuredRepetitions +
                1)
            .ToArray();
        if (bounded.Length !=
                O5R5MeasurementProtocol.WarmUpRepetitions +
                O5R5MeasurementProtocol.MeasuredRepetitions ||
            bounded.Any(sample => sample is null))
        {
            throw new InvalidOperationException("o5r5a.summary.sample_count_invalid");
        }

        O5R5MeasurementSample[] copied = bounded.Select(sample => sample!).ToArray();
        O5R5MeasurementPhase phase = copied[0].Phase;
        O5R5Temperature temperature = copied[0].Temperature;
        if (copied.Any(
                sample =>
                    sample.Phase != phase ||
                    sample.Temperature != temperature ||
                    !string.Equals(sample.ProtocolDigest, protocol.Digest, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("o5r5a.summary.batch_mixed");
        }

        O5R5MeasurementSample[] warmUps = copied.Where(sample => sample.IsWarmUp).ToArray();
        O5R5MeasurementSample[] measured = copied.Where(sample => !sample.IsWarmUp).ToArray();
        ValidateSequence(warmUps, O5R5MeasurementProtocol.WarmUpRepetitions);
        ValidateSequence(measured, O5R5MeasurementProtocol.MeasuredRepetitions);

        O5R5MetricDistribution elapsed = Distribution(measured.Select(sample => sample.ElapsedMilliseconds));
        O5R5MetricDistribution cpu = Distribution(measured.Select(sample => sample.CpuMilliseconds));
        O5R5MetricDistribution heap = Distribution(measured.Select(sample => (double)sample.HeapPeakDeltaBytes));
        O5R5MetricDistribution workingSet = Distribution(
            measured.Select(sample => (double)sample.WorkingSetPeakDeltaBytes));
        O5R5MetricDistribution allocation = Distribution(
            measured.Select(sample => (double)sample.AllocationPeakBytes));
        O5R5MetricDistribution? elapsedPerWork = O5R5MeasurementProtocol.IsComputePhase(phase)
            ? Distribution(measured.Select(sample => sample.ElapsedMicrosecondsPerWorkUnit!.Value))
            : null;
        O5R5MetricDistribution? cpuPerWork = O5R5MeasurementProtocol.IsComputePhase(phase)
            ? Distribution(measured.Select(sample => sample.CpuMicrosecondsPerWorkUnit!.Value))
            : null;
        O5R5MetricDistribution repeatabilityElapsed = FixedGroupMedianDistribution(
            measured.Select(sample => sample.ElapsedMilliseconds).ToArray());

        // Every raw sample remains subject to its absolute gate and in the evidence. Relative repeatability
        // alone uses five fixed sequential-group medians, preventing one Windows scheduling delay from
        // dominating a sub-millisecond workload without choosing or deleting any observation.
        bool passed = measured.All(sample => sample.Passed) &&
            repeatabilityElapsed.CoefficientOfVariation <=
            O5R5MeasurementProtocol.MaximumCoefficientOfVariation;

        return new O5R5MeasurementSummary(
            passed ? "o5r5a.summary.accepted" : "o5r5a.summary.failed",
            phase,
            temperature,
            warmUps.Length,
            measured.Length,
            elapsed,
            cpu,
            heap,
            workingSet,
            allocation,
            elapsedPerWork,
            cpuPerWork,
            repeatabilityElapsed,
            passed);
    }

    /// <summary>Builds a complete sample only after validating monotonicity and every finite metric.</summary>
    /// <param name="scenario">Predeclared scenario.</param>
    /// <param name="before">Initial source snapshot.</param>
    /// <param name="checkpoints">Bounded intermediate snapshots.</param>
    /// <param name="after">Terminal source snapshot.</param>
    /// <returns>A complete result or fail-closed metric refusal.</returns>
    private O5R5MeasurementResult BuildResult(
        O5R5MeasurementScenario scenario,
        O5R5MetricSnapshot before,
        IReadOnlyList<O5R5MetricSnapshot> checkpoints,
        O5R5MetricSnapshot after)
    {
        O5R5MetricSnapshot[] snapshots = [before, .. checkpoints, after];
        if (source.Frequency <= 0 ||
            snapshots.Any(
                snapshot =>
                    snapshot.MonotonicTimestamp < 0 ||
                    snapshot.HeapBytes < 0 ||
                    snapshot.TotalAllocatedBytes < 0 ||
                    snapshot.WorkingSetBytes < 0 ||
                    snapshot.CpuTime < TimeSpan.Zero))
        {
            return Refused("o5r5a.measurement.metric_invalid");
        }

        for (int index = 1; index < snapshots.Length; index++)
        {
            if (snapshots[index].MonotonicTimestamp < snapshots[index - 1].MonotonicTimestamp ||
                snapshots[index].TotalAllocatedBytes < snapshots[index - 1].TotalAllocatedBytes ||
                snapshots[index].CpuTime < snapshots[index - 1].CpuTime)
            {
                return Refused("o5r5a.measurement.metric_rollback");
            }
        }

        long elapsedTicks = after.MonotonicTimestamp - before.MonotonicTimestamp;
        double elapsedMilliseconds = (elapsedTicks * 1_000d) / source.Frequency;
        double elapsedMicroseconds = (elapsedTicks * 1_000_000d) / source.Frequency;
        double cpuMilliseconds = (after.CpuTime - before.CpuTime).TotalMilliseconds;
        long heapPeak = snapshots.Max(snapshot => snapshot.HeapBytes) - before.HeapBytes;
        long workingSetPeak = snapshots.Max(snapshot => snapshot.WorkingSetBytes) - before.WorkingSetBytes;
        long cumulativeAllocated = after.TotalAllocatedBytes - before.TotalAllocatedBytes;
        long allocationPeak = 0;
        for (int index = 1; index < snapshots.Length; index++)
        {
            allocationPeak = Math.Max(
                allocationPeak,
                snapshots[index].TotalAllocatedBytes - snapshots[index - 1].TotalAllocatedBytes);
        }

        double? elapsedPerWork = O5R5MeasurementProtocol.IsComputePhase(scenario.Phase)
            ? elapsedMicroseconds / scenario.DeclaredWorkUnits
            : null;
        double? cpuPerWork = O5R5MeasurementProtocol.IsComputePhase(scenario.Phase)
            ? ((after.CpuTime - before.CpuTime).Ticks / 10d) / scenario.DeclaredWorkUnits
            : null;
        double[] finite =
        [
            elapsedMilliseconds,
            elapsedMicroseconds,
            cpuMilliseconds,
            elapsedPerWork ?? 0d,
            cpuPerWork ?? 0d,
        ];
        if (finite.Any(value => !double.IsFinite(value) || value < 0d))
        {
            return Refused("o5r5a.measurement.metric_invalid");
        }

        O5R5PhaseLimit limit = protocol.LimitFor(scenario.Phase);
        long empiricalMemoryLimit = protocol.EmpiricalMemoryLimit(scenario.AccountedMemoryBytes);
        bool passed = elapsedMilliseconds <= limit.MaximumElapsed.TotalMilliseconds &&
            heapPeak <= empiricalMemoryLimit &&
            workingSetPeak <= empiricalMemoryLimit &&
            allocationPeak <= empiricalMemoryLimit &&
            (!limit.MeasuresWorkRate ||
                (elapsedPerWork <= O5R5MeasurementProtocol.MaximumElapsedMicrosecondsPerWorkUnit &&
                    cpuPerWork <= O5R5MeasurementProtocol.MaximumCpuMicrosecondsPerWorkUnit));
        O5R5MeasurementSample sample = new(
            protocol.Digest,
            scenario.Phase,
            scenario.Temperature,
            scenario.IsWarmUp,
            scenario.Repetition,
            elapsedMilliseconds,
            cpuMilliseconds,
            heapPeak,
            workingSetPeak,
            allocationPeak,
            cumulativeAllocated,
            scenario.DeclaredWorkUnits,
            scenario.AccountedMemoryBytes,
            elapsedPerWork,
            cpuPerWork,
            passed);
        return new O5R5MeasurementResult(
            O5R5MeasurementDisposition.Accepted,
            passed
                ? "o5r5a.measurement.accepted"
                : "o5r5a.measurement.threshold_exceeded",
            sample);
    }

    /// <summary>Validates phase, sequence and deterministic resource bounds before source capture.</summary>
    /// <param name="scenario">Candidate scenario.</param>
    /// <returns>Stable refusal code or <see langword="null"/>.</returns>
    private string? ValidateScenario(O5R5MeasurementScenario scenario)
    {
        if (!Enum.IsDefined(scenario.Phase) ||
            !Enum.IsDefined(scenario.Temperature) ||
            scenario.Operation is null)
        {
            return "o5r5a.scenario.invalid";
        }

        int maximumRepetition = scenario.IsWarmUp
            ? O5R5MeasurementProtocol.WarmUpRepetitions
            : O5R5MeasurementProtocol.MeasuredRepetitions;
        if (scenario.Repetition < 1 || scenario.Repetition > maximumRepetition)
        {
            return "o5r5a.scenario.repetition_invalid";
        }
        if (scenario.DeclaredWorkUnits < 1 ||
            scenario.DeclaredWorkUnits > protocol.Envelope.MaximumWorkUnits)
        {
            return "o5r5a.scenario.work_refused";
        }
        if (scenario.AccountedMemoryBytes < 1 ||
            scenario.AccountedMemoryBytes > protocol.Envelope.MaximumAccountedMemoryBytes)
        {
            return "o5r5a.scenario.memory_refused";
        }

        return null;
    }

    /// <summary>Validates exact one-based repetition membership without duplicates.</summary>
    /// <param name="samples">Warm-up or retained samples.</param>
    /// <param name="expectedCount">Exact required count.</param>
    /// <exception cref="InvalidOperationException">Thrown for missing or duplicate repetitions.</exception>
    private static void ValidateSequence(
        O5R5MeasurementSample[] samples,
        int expectedCount)
    {
        if (samples.Length != expectedCount ||
            !samples.Select(sample => sample.Repetition).Order().SequenceEqual(Enumerable.Range(1, expectedCount)))
        {
            throw new InvalidOperationException("o5r5a.summary.sequence_invalid");
        }
    }

    /// <summary>Calculates nearest-rank percentiles and population variance without removing any sample.</summary>
    /// <param name="values">Complete retained metric values.</param>
    /// <returns>One finite immutable distribution.</returns>
    private static O5R5MetricDistribution Distribution(
        IEnumerable<double> values,
        int expectedCount = O5R5MeasurementProtocol.MeasuredRepetitions)
    {
        double[] sorted = values.Order().ToArray();
        if (sorted.Length != expectedCount ||
            sorted.Any(value => !double.IsFinite(value) || value < 0d))
        {
            throw new InvalidOperationException("o5r5a.summary.metric_invalid");
        }

        double mean = sorted.Average();
        double variance = sorted.Select(value => Math.Pow(value - mean, 2d)).Average();
        double standardDeviation = Math.Sqrt(variance);
        double coefficient = mean == 0d ? 0d : standardDeviation / mean;
        return new O5R5MetricDistribution(
            Percentile(sorted, O5R5MeasurementProtocol.Percentile50),
            Percentile(sorted, O5R5MeasurementProtocol.Percentile95),
            Percentile(sorted, O5R5MeasurementProtocol.Percentile99),
            sorted[^1],
            mean,
            standardDeviation,
            coefficient);
    }

    /// <summary>
    /// Calculates repeatability from all thirty samples in five fixed sequential groups without
    /// dropping, reordering or choosing observations after execution.
    /// </summary>
    /// <param name="values">Exact retained elapsed measurements in repetition order.</param>
    /// <returns>A distribution over the five fixed group medians.</returns>
    private static O5R5MetricDistribution FixedGroupMedianDistribution(double[] values)
    {
        if (values.Length != O5R5MeasurementProtocol.MeasuredRepetitions)
        {
            throw new InvalidOperationException("o5r5a.summary.repeatability_membership_invalid");
        }

        double[] medians = values
            .Chunk(O5R5MeasurementProtocol.SamplesPerRepeatabilityGroup)
            .Select(Median)
            .ToArray();
        if (medians.Length != O5R5MeasurementProtocol.RepeatabilityGroupCount)
        {
            throw new InvalidOperationException("o5r5a.summary.repeatability_group_invalid");
        }

        return Distribution(medians, O5R5MeasurementProtocol.RepeatabilityGroupCount);
    }

    /// <summary>Calculates the finite median of one exact repeatability group.</summary>
    /// <param name="values">One fixed group of six elapsed measurements.</param>
    /// <returns>The arithmetic midpoint of the two central ordered values.</returns>
    private static double Median(double[] values)
    {
        if (values.Length != O5R5MeasurementProtocol.SamplesPerRepeatabilityGroup ||
            values.Any(value => !double.IsFinite(value) || value < 0d))
        {
            throw new InvalidOperationException("o5r5a.summary.repeatability_group_invalid");
        }

        double[] ordered = [.. values.Order()];
        int upper = ordered.Length / 2;
        return (ordered[upper - 1] + ordered[upper]) / 2d;
    }

    /// <summary>Calculates one nearest-rank percentile over a sorted non-empty distribution.</summary>
    /// <param name="sorted">Ascending values.</param>
    /// <param name="percentile">Inclusive percentile from one to one hundred.</param>
    /// <returns>The nearest-rank value.</returns>
    private static double Percentile(double[] sorted, int percentile)
    {
        int rank = Math.Max(1, (int)Math.Ceiling((percentile / 100d) * sorted.Length));
        return sorted[rank - 1];
    }

    /// <summary>Creates one stable refusal with no partial sample.</summary>
    /// <param name="code">Sanitised diagnostic code.</param>
    /// <returns>A non-authorising refusal.</returns>
    private static O5R5MeasurementResult Refused(string code) =>
        new(O5R5MeasurementDisposition.Refused, code, null);
}
