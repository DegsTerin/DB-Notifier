// Module purpose: Defines the isolated O5-R5-B physical campaign driver, bounded workloads and sanitised evidence contract.
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DBNotifier.IntegrationTests;

/// <summary>Describes one sanitised host declaration supplied to the physical campaign.</summary>
/// <param name="OperatingSystem">Operating-system description without machine identity.</param>
/// <param name="OperatingSystemArchitecture">Operating-system architecture.</param>
/// <param name="ProcessArchitecture">Measurement-process architecture.</param>
/// <param name="DotNetRuntime">Selected .NET runtime description.</param>
/// <param name="DotNetSdk">Repository-selected .NET SDK version.</param>
/// <param name="ProcessorCount">Logical processor count visible to the process.</param>
/// <param name="InstalledMemoryBytes">Installed physical memory declared by the local wrapper.</param>
/// <param name="StopwatchFrequency">Monotonic-clock ticks per second.</param>
internal sealed record O5R5PhysicalEnvironment(
    string OperatingSystem,
    string OperatingSystemArchitecture,
    string ProcessArchitecture,
    string DotNetRuntime,
    string DotNetSdk,
    int ProcessorCount,
    long InstalledMemoryBytes,
    long StopwatchFrequency);

/// <summary>Reports the complete or fail-closed physical campaign without granting Observer authority.</summary>
/// <param name="ProtocolVersion">Frozen O5-R5-A protocol version.</param>
/// <param name="ProtocolDigest">Frozen O5-R5-A protocol digest.</param>
/// <param name="ActivationState">Lifecycle activation state, which remains None.</param>
/// <param name="StartedAtUtc">Campaign start instant.</param>
/// <param name="CompletedAtUtc">Campaign completion or stop instant.</param>
/// <param name="Environment">Sanitised physical host declaration.</param>
/// <param name="Code">Stable campaign disposition code.</param>
/// <param name="Passed">Whether every sample, summary and variance gate passed.</param>
/// <param name="ExpectedSampleCount">Exact preregistered sample count.</param>
/// <param name="CompletedSampleCount">Number of complete samples retained before completion or stop.</param>
/// <param name="Samples">Complete phase-labelled samples, including warm-ups.</param>
/// <param name="Summaries">Completed phase/temperature summaries.</param>
/// <param name="Failures">Bounded sanitised threshold diagnostics retained before cleanup.</param>
internal sealed record O5R5PhysicalCampaignReport(
    string ProtocolVersion,
    string ProtocolDigest,
    string ActivationState,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    O5R5PhysicalEnvironment Environment,
    string Code,
    bool Passed,
    int ExpectedSampleCount,
    int CompletedSampleCount,
    IReadOnlyList<O5R5MeasurementSample> Samples,
    IReadOnlyList<O5R5MeasurementSummary> Summaries,
    IReadOnlyList<O5R5ThresholdFailure> Failures)
{
    /// <summary>Gets a value that is always false because physical evidence cannot authorise activation.</summary>
    internal static bool IsAuthorising => false;
}

/// <summary>
/// Materialises the full synthetic cancellation input into one bounded buffer owned by the
/// serial test-only workload instead of allocating a new large object for every sample.
/// </summary>
internal sealed class O5R5CancellationInputBuffer
{
    private readonly byte[] source;
    private readonly byte[] coldInput;

    /// <summary>Initialises storage with the exact frozen input length before measurement starts.</summary>
    /// <param name="source">Immutable deterministic source bytes.</param>
    internal O5R5CancellationInputBuffer(byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Length != O1ResourceEnvelope.Fixture().MaximumInputBytes)
        {
            throw new InvalidOperationException("o5r5d3.cancellation.input_length_invalid");
        }

        this.source = source;
        coldInput = GC.AllocateUninitializedArray<byte>(source.Length);
    }

    /// <summary>
    /// Returns immutable warm bytes or freshly copies the same complete input into the dedicated
    /// cold buffer without allocating per sample.
    /// </summary>
    /// <param name="temperature">Cold or warm data-state classification.</param>
    /// <returns>The exact frozen input bytes in bounded reusable storage.</returns>
    internal ReadOnlyMemory<byte> Materialise(O5R5Temperature temperature)
    {
        if (temperature == O5R5Temperature.Warm)
        {
            return source;
        }
        if (temperature != O5R5Temperature.Cold)
        {
            throw new ArgumentOutOfRangeException(nameof(temperature));
        }

        source.AsSpan().CopyTo(coldInput);
        return coldInput;
    }
}

/// <summary>
/// Materialises the exact eight bounded physical workloads without provider, database, corpus,
/// network, publication or normal-composition authority.
/// </summary>
internal sealed class O5R5PhysicalWorkloadCatalogue
{
    private const int SortItemCount = 256;
    private const int ParseTokenTarget = 100_000;
    private const int CryptographyOperationTarget = 100_000;
    private const int ControlUpdateTarget = 100_000;
    private const int SortOperationTarget = 100_000;
    private const int AnalysisOperationTarget = 100_000;
    private static readonly TimeSpan IdleDuration = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan CancellationDuration = TimeSpan.FromMilliseconds(10);

    private readonly byte[] warmInput;
    private readonly int[] warmSortValues;
    private readonly double[] warmAnalysisValues;
    private readonly O5R5CancellationInputBuffer cancellationInput;
    private int controlVersion;
    private int observableSink;

    /// <summary>Initialises immutable warm inputs inside the frozen input and item limits.</summary>
    internal O5R5PhysicalWorkloadCatalogue()
    {
        warmInput = CreateInput();
        warmSortValues = CreateSortValues();
        warmAnalysisValues = CreateAnalysisValues();
        cancellationInput = new O5R5CancellationInputBuffer(warmInput);
    }

    /// <summary>Creates the bounded operation for one exact phase and temperature.</summary>
    /// <param name="phase">Frozen phase label.</param>
    /// <param name="temperature">Cold or warm data-state classification.</param>
    /// <returns>A bounded operation suitable for the accepted O5-R5-A runner.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for an unknown phase.</exception>
    internal Func<O5R5MeasurementContext, CancellationToken, ValueTask> Create(
        O5R5MeasurementPhase phase,
        O5R5Temperature temperature) =>
        phase switch
        {
            O5R5MeasurementPhase.FirstByte => (context, token) =>
                RunFirstByteAsync(context, temperature, token),
            O5R5MeasurementPhase.Idle => (context, token) =>
                RunIdleAsync(context, temperature, token),
            O5R5MeasurementPhase.Cancellation => (context, token) =>
                RunCancellationAsync(context, temperature, token),
            O5R5MeasurementPhase.ControlUpdate => (context, token) =>
                RunControlUpdateAsync(context, token),
            O5R5MeasurementPhase.Parse => (context, token) =>
                RunParseAsync(context, temperature, token),
            O5R5MeasurementPhase.Cryptography => (context, token) =>
                RunCryptographyAsync(context, temperature, token),
            O5R5MeasurementPhase.Sort => (context, token) =>
                RunSortAsync(context, temperature, token),
            O5R5MeasurementPhase.Analysis => (context, token) =>
                RunAnalysisAsync(context, temperature, token),
            _ => throw new ArgumentOutOfRangeException(nameof(phase)),
        };

    /// <summary>
    /// Records first-byte liveness, then executes the frozen observation window used only for
    /// repeatability and deterministic work-rate evidence.
    /// </summary>
    /// <param name="context">Bounded measurement checkpoint context.</param>
    /// <param name="temperature">Cold creates fresh data; warm reuses immutable prepared data.</param>
    /// <param name="cancellationToken">Cancellation checked before observable work.</param>
    /// <returns>A completed bounded operation.</returns>
    private ValueTask RunFirstByteAsync(
        O5R5MeasurementContext context,
        O5R5Temperature temperature,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] input = InputFor(temperature);
        int observed = input[0];
        Volatile.Write(ref observableSink, observed);
        context.CaptureCheckpoint();
        for (int unit = 0; unit < O5R5MeasurementProtocol.FirstByteRepeatabilityWorkUnits; unit++)
        {
            if ((unit & 1_023) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            observed ^= input[unit & (input.Length - 1)];
        }

        Volatile.Write(ref observableSink, observed);
        return ValueTask.CompletedTask;
    }

    /// <summary>Observes one bounded idle interval while retaining the declared maximum input.</summary>
    /// <param name="context">Bounded measurement checkpoint context.</param>
    /// <param name="temperature">Cold creates fresh data; warm reuses immutable prepared data.</param>
    /// <param name="cancellationToken">Outer campaign cancellation.</param>
    /// <returns>A bounded asynchronous wait.</returns>
    private async ValueTask RunIdleAsync(
        O5R5MeasurementContext context,
        O5R5Temperature temperature,
        CancellationToken cancellationToken)
    {
        byte[] input = InputFor(temperature);
        context.CaptureCheckpoint();
        await Task.Delay(IdleDuration, cancellationToken).ConfigureAwait(false);
        Volatile.Write(ref observableSink, input[^1]);
    }

    /// <summary>Measures a self-contained cancellation observation without cancelling the owning runner.</summary>
    /// <param name="context">Bounded measurement checkpoint context.</param>
    /// <param name="temperature">Cold creates fresh data; warm reuses immutable prepared data.</param>
    /// <param name="cancellationToken">Outer campaign cancellation.</param>
    /// <returns>A completed operation after the inner cancellation is observed.</returns>
    private async ValueTask RunCancellationAsync(
        O5R5MeasurementContext context,
        O5R5Temperature temperature,
        CancellationToken cancellationToken)
    {
        ReadOnlyMemory<byte> input = cancellationInput.Materialise(temperature);
        context.CaptureCheckpoint();
        using CancellationTokenSource inner = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        inner.CancelAfter(CancellationDuration);
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, inner.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            inner.IsCancellationRequested &&
            !cancellationToken.IsCancellationRequested)
        {
            Volatile.Write(ref observableSink, input.Span[0]);
        }
    }

    /// <summary>Applies exactly the maximum admitted number of isolated in-memory control updates.</summary>
    /// <param name="context">Bounded measurement checkpoint context.</param>
    /// <param name="cancellationToken">Cancellation checked at bounded intervals.</param>
    /// <returns>A completed bounded operation.</returns>
    private ValueTask RunControlUpdateAsync(
        O5R5MeasurementContext context,
        CancellationToken cancellationToken)
    {
        context.CaptureCheckpoint();
        for (int index = 0; index < ControlUpdateTarget; index++)
        {
            if ((index & 1_023) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            Interlocked.Increment(ref controlVersion);
        }

        Volatile.Write(ref observableSink, Volatile.Read(ref controlVersion));
        return ValueTask.CompletedTask;
    }

    /// <summary>Executes exactly one hundred thousand bounded UTF-8 JSON token reads.</summary>
    /// <param name="context">Bounded measurement checkpoint context.</param>
    /// <param name="temperature">Cold creates fresh data; warm reuses immutable prepared data.</param>
    /// <param name="cancellationToken">Cancellation checked between complete parse passes.</param>
    /// <returns>A completed bounded operation.</returns>
    private ValueTask RunParseAsync(
        O5R5MeasurementContext context,
        O5R5Temperature temperature,
        CancellationToken cancellationToken)
    {
        byte[] input = InputFor(temperature);
        context.CaptureCheckpoint();
        int completed = 0;
        int tokenKind = 0;
        while (completed < ParseTokenTarget)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Utf8JsonReader reader = new(input);
            while (completed < ParseTokenTarget && reader.Read())
            {
                tokenKind ^= (int)reader.TokenType;
                completed++;
            }
        }

        Volatile.Write(ref observableSink, tokenKind);
        return ValueTask.CompletedTask;
    }

    /// <summary>Executes one hundred thousand allocation-free SHA-256 block operations.</summary>
    /// <param name="context">Bounded measurement checkpoint context.</param>
    /// <param name="temperature">Cold creates fresh data; warm reuses immutable prepared data.</param>
    /// <param name="cancellationToken">Cancellation checked at bounded intervals.</param>
    /// <returns>A completed bounded operation.</returns>
    private ValueTask RunCryptographyAsync(
        O5R5MeasurementContext context,
        O5R5Temperature temperature,
        CancellationToken cancellationToken)
    {
        byte[] input = InputFor(temperature);
        context.CaptureCheckpoint();
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        ReadOnlySpan<byte> block = input.AsSpan(0, 64);
        for (int index = 0; index < CryptographyOperationTarget; index++)
        {
            if ((index & 1_023) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            SHA256.HashData(block, digest);
            block = input.AsSpan(digest[0] % (input.Length - 64), 64);
        }

        Volatile.Write(ref observableSink, digest[0]);
        return ValueTask.CompletedTask;
    }

    /// <summary>Executes exactly one hundred thousand deterministic compare-and-swap work units.</summary>
    /// <param name="context">Bounded measurement checkpoint context.</param>
    /// <param name="temperature">Cold creates fresh data; warm clones immutable prepared data.</param>
    /// <param name="cancellationToken">Cancellation checked at bounded intervals.</param>
    /// <returns>A completed bounded operation.</returns>
    private ValueTask RunSortAsync(
        O5R5MeasurementContext context,
        O5R5Temperature temperature,
        CancellationToken cancellationToken)
    {
        int[] values = temperature == O5R5Temperature.Cold
            ? CreateSortValues()
            : (int[])warmSortValues.Clone();
        context.CaptureCheckpoint();
        for (int unit = 0; unit < SortOperationTarget; unit++)
        {
            if ((unit & 1_023) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            int left = unit % (SortItemCount - 1);
            if (values[left] > values[left + 1])
            {
                (values[left], values[left + 1]) = (values[left + 1], values[left]);
            }
        }

        Volatile.Write(ref observableSink, values[0]);
        return ValueTask.CompletedTask;
    }

    /// <summary>Executes exactly one hundred thousand finite analysis accumulation work units.</summary>
    /// <param name="context">Bounded measurement checkpoint context.</param>
    /// <param name="temperature">Cold creates fresh data; warm reuses immutable prepared data.</param>
    /// <param name="cancellationToken">Cancellation checked at bounded intervals.</param>
    /// <returns>A completed bounded operation.</returns>
    private ValueTask RunAnalysisAsync(
        O5R5MeasurementContext context,
        O5R5Temperature temperature,
        CancellationToken cancellationToken)
    {
        double[] values = temperature == O5R5Temperature.Cold
            ? CreateAnalysisValues()
            : warmAnalysisValues;
        context.CaptureCheckpoint();
        double total = 0d;
        for (int unit = 0; unit < AnalysisOperationTarget; unit++)
        {
            if ((unit & 1_023) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            total += values[unit % values.Length];
        }

        Volatile.Write(ref observableSink, (int)total);
        return ValueTask.CompletedTask;
    }

    /// <summary>Returns fresh cold input or the immutable warm input.</summary>
    /// <param name="temperature">Cold or warm data-state classification.</param>
    /// <returns>A bounded UTF-8 JSON input.</returns>
    private byte[] InputFor(O5R5Temperature temperature) =>
        temperature == O5R5Temperature.Cold
            ? (byte[])warmInput.Clone()
            : warmInput;

    /// <summary>Creates a valid JSON input that occupies the exact frozen maximum input bytes.</summary>
    /// <returns>A deterministic 65,536-byte UTF-8 JSON document.</returns>
    private static byte[] CreateInput()
    {
        byte[] input = GC.AllocateUninitializedArray<byte>(65_536);
        input.AsSpan().Fill((byte)' ');
        ReadOnlySpan<byte> prefix = "{\"values\":[0,1,2,3],\"padding\":\""u8;
        ReadOnlySpan<byte> suffix = "\"}"u8;
        prefix.CopyTo(input);
        suffix.CopyTo(input.AsSpan(input.Length - suffix.Length));
        return input;
    }

    /// <summary>Creates a deterministic 256-item sort input.</summary>
    /// <returns>A bounded descending integer sequence.</returns>
    private static int[] CreateSortValues() =>
        Enumerable.Range(0, SortItemCount).Select(value => SortItemCount - value).ToArray();

    /// <summary>Creates a deterministic finite 256-item analysis input.</summary>
    /// <returns>A bounded provider-neutral numeric sequence.</returns>
    private static double[] CreateAnalysisValues() =>
        Enumerable.Range(1, SortItemCount).Select(value => value / 10d).ToArray();
}

/// <summary>
/// Coordinates the exact frozen sample matrix and stops at the first refused sample, exceeded
/// threshold or failed phase/temperature summary.
/// </summary>
internal sealed class O5R5PhysicalCampaignDriver
{
    internal const int ExpectedSampleCount =
        8 * 2 *
        (O5R5MeasurementProtocol.WarmUpRepetitions +
            O5R5MeasurementProtocol.MeasuredRepetitions);

    private readonly O5R5MeasurementProtocol protocol;
    private readonly O5R5MeasurementReadinessRunner runner;
    private readonly O5R5PhysicalWorkloadCatalogue workloads;
    private readonly IO5R5MeasurementSource source;

    /// <summary>Initialises the isolated campaign over one already-authorised measurement source.</summary>
    /// <param name="protocol">Exact frozen protocol.</param>
    /// <param name="source">Physical source only after entrypoint opt-in; synthetic source in tests.</param>
    internal O5R5PhysicalCampaignDriver(
        O5R5MeasurementProtocol protocol,
        IO5R5MeasurementSource source)
    {
        this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        runner = new O5R5MeasurementReadinessRunner(
            O5R5MeasurementReadinessRunner.Marker,
            protocol,
            this.source);
        workloads = new O5R5PhysicalWorkloadCatalogue();
    }

    /// <summary>Builds the exact eight-phase, two-temperature and 5/30 sample matrix.</summary>
    /// <returns>Five hundred and sixty phase-labelled bounded scenarios in stable order.</returns>
    internal IReadOnlyList<O5R5MeasurementScenario> CreateScenarios()
    {
        List<O5R5MeasurementScenario> scenarios = new(ExpectedSampleCount);
        foreach (O5R5MeasurementPhase phase in Enum.GetValues<O5R5MeasurementPhase>())
        {
            foreach (O5R5Temperature temperature in Enum.GetValues<O5R5Temperature>())
            {
                AddScenarios(scenarios, phase, temperature, isWarmUp: true);
                AddScenarios(scenarios, phase, temperature, isWarmUp: false);
            }
        }

        return scenarios.AsReadOnly();
    }

    /// <summary>Runs the exact physical campaign and returns complete or partial fail-closed evidence.</summary>
    /// <param name="environment">Sanitised host declaration captured before execution.</param>
    /// <param name="cancellationToken">Bounded whole-campaign cancellation.</param>
    /// <returns>A non-authorising physical campaign report.</returns>
    internal async Task<O5R5PhysicalCampaignReport> RunAsync(
        O5R5PhysicalEnvironment environment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(environment);
        await PreconditionAsync(cancellationToken).ConfigureAwait(false);
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
        List<O5R5MeasurementSample> samples = new(ExpectedSampleCount);
        List<O5R5MeasurementSummary> summaries = new(16);
        List<O5R5ThresholdFailure> failures = new(6);

        foreach (O5R5MeasurementScenario scenario in CreateScenarios())
        {
            O5R5MeasurementResult result = await runner
                .RunAsync(scenario, cancellationToken)
                .ConfigureAwait(false);
            failures.AddRange(result.Failures);
            if (result.Disposition != O5R5MeasurementDisposition.Accepted ||
                result.Sample is null)
            {
                return Report(
                    startedAtUtc,
                    environment,
                    result.Code,
                    false,
                    samples,
                    summaries,
                    failures);
            }

            samples.Add(result.Sample);
            if (!result.Sample.Passed)
            {
                return Report(
                    startedAtUtc,
                    environment,
                    result.Code,
                    false,
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
                        startedAtUtc,
                        environment,
                        summary.Failures[0].Code,
                        false,
                        samples,
                        summaries,
                        failures);
                }
            }
        }

        return Report(
            startedAtUtc,
            environment,
            "o5r5b.campaign.accepted",
            true,
            samples,
            summaries,
            failures);
    }

    /// <summary>Adds one exact warm-up or measured sequence to the stable campaign matrix.</summary>
    /// <param name="scenarios">Destination matrix.</param>
    /// <param name="phase">Exact phase.</param>
    /// <param name="temperature">Cold or warm data-state classification.</param>
    /// <param name="isWarmUp">Whether to add five warm-ups or thirty retained measurements.</param>
    private void AddScenarios(
        List<O5R5MeasurementScenario> scenarios,
        O5R5MeasurementPhase phase,
        O5R5Temperature temperature,
        bool isWarmUp)
    {
        int count = isWarmUp
            ? O5R5MeasurementProtocol.WarmUpRepetitions
            : O5R5MeasurementProtocol.MeasuredRepetitions;
        for (int repetition = 1; repetition <= count; repetition++)
        {
            scenarios.Add(
                new O5R5MeasurementScenario(
                    phase,
                    temperature,
                    isWarmUp,
                    repetition,
                    protocol.Envelope.MaximumWorkUnits,
                    protocol.Envelope.MaximumAccountedMemoryBytes,
                    workloads.Create(phase, temperature)));
        }
    }

    /// <summary>Creates one immutable campaign disposition without machine identity or payload.</summary>
    /// <param name="startedAtUtc">Campaign start instant.</param>
    /// <param name="environment">Sanitised host declaration.</param>
    /// <param name="code">Stable disposition code.</param>
    /// <param name="passed">Whether every gate passed.</param>
    /// <param name="samples">Complete retained samples.</param>
    /// <param name="summaries">Complete retained summaries.</param>
    /// <param name="failures">Bounded sanitised threshold diagnostics.</param>
    /// <returns>A non-authorising campaign report.</returns>
    private O5R5PhysicalCampaignReport Report(
        DateTimeOffset startedAtUtc,
        O5R5PhysicalEnvironment environment,
        string code,
        bool passed,
        List<O5R5MeasurementSample> samples,
        List<O5R5MeasurementSummary> summaries,
        List<O5R5ThresholdFailure> failures) =>
        new(
            O5R5MeasurementProtocol.Version,
            protocol.Digest,
            "None",
            startedAtUtc,
            DateTimeOffset.UtcNow,
            environment,
            code,
            passed,
            ExpectedSampleCount,
            samples.Count,
            samples.ToArray(),
            summaries.ToArray(),
            failures.ToArray());

    /// <summary>
    /// Materialises each code path once before the frozen warm-up and measured sequences so process-wide page-in
    /// and JIT activity are not incorrectly attributed to an individual bounded workload.
    /// </summary>
    /// <param name="cancellationToken">Whole-campaign cancellation checked during every preconditioning phase.</param>
    /// <returns>A task that completes before any retained physical sample begins.</returns>
    internal async Task PreconditionAsync(CancellationToken cancellationToken)
    {
        foreach (O5R5MeasurementPhase phase in Enum.GetValues<O5R5MeasurementPhase>())
        {
            foreach (O5R5Temperature temperature in Enum.GetValues<O5R5Temperature>())
            {
                O5R5MeasurementContext context = new(source);
                await workloads.Create(phase, temperature)(context, cancellationToken).ConfigureAwait(false);
            }
        }

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
    }
}

/// <summary>Abstracts the four filesystem operations required for atomic evidence persistence.</summary>
internal interface IO5R5EvidenceFileOperations
{
    /// <summary>Writes complete bytes to one temporary destination.</summary>
    Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken);

    /// <summary>Returns whether one exact path exists.</summary>
    bool Exists(string path);

    /// <summary>Deletes one exact temporary path.</summary>
    void Delete(string path);

    /// <summary>Atomically moves one completed temporary file over the destination.</summary>
    void Move(string source, string destination);
}

/// <summary>Uses built-in filesystem APIs for the production path of the test-only evidence writer.</summary>
internal sealed class O5R5EvidenceFileOperations : IO5R5EvidenceFileOperations
{
    /// <inheritdoc />
    public async Task WriteAllBytesAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 16_384,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        stream.Flush(flushToDisk: true);
    }

    /// <inheritdoc />
    public bool Exists(string path) => File.Exists(path);

    /// <inheritdoc />
    public void Delete(string path) => File.Delete(path);

    /// <inheritdoc />
    public void Move(string source, string destination) =>
        File.Move(source, destination, overwrite: true);
}

/// <summary>
/// Validates and writes one physical campaign report atomically inside an exact project-owned
/// temporary root.
/// </summary>
internal static class O5R5PhysicalEvidenceWriter
{
    private const string RootPrefix = "DBNotifier-O5-R5-Physical-";
    private const string EvidenceFileName = "o5-r5-physical-campaign.json";
    private const int MaximumEvidenceBytes = 4 * 1024 * 1024;
    private const int MaximumFailures = 7;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Validates the exact temporary evidence destination before any physical source is created.</summary>
    /// <param name="candidate">Absolute destination supplied by the authorised local wrapper.</param>
    /// <returns>The canonical destination.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the path leaves the exact temporary boundary.</exception>
    internal static string ValidateDestination(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) || !Path.IsPathFullyQualified(candidate))
        {
            throw new InvalidOperationException("o5r5b.output.path_required");
        }

        string fullPath = Path.GetFullPath(candidate);
        string? directory = Path.GetDirectoryName(fullPath);
        string? rootName = directory is null ? null : Path.GetFileName(directory);
        string temporaryRoot = Path.GetFullPath(Path.GetTempPath());
        if (directory is null ||
            !directory.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(rootName) ||
            !rootName.StartsWith(RootPrefix, StringComparison.Ordinal) ||
            !string.Equals(Path.GetFileName(fullPath), EvidenceFileName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o5r5b.output.path_refused");
        }

        DirectoryInfo information = new(directory);
        if (!information.Exists ||
            information.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException("o5r5b.output.root_refused");
        }

        return fullPath;
    }

    /// <summary>Atomically persists sanitised temporary evidence for later documentary extraction.</summary>
    /// <param name="destination">Validated exact temporary destination.</param>
    /// <param name="report">Complete or fail-closed campaign report.</param>
    /// <param name="cancellationToken">Cancellation honoured before commit.</param>
    /// <returns>A task that completes after atomic replacement.</returns>
    internal static async Task WriteAsync(
        string destination,
        O5R5PhysicalCampaignReport report,
        CancellationToken cancellationToken) =>
        await WriteAsync(
            destination,
            report,
            new O5R5EvidenceFileOperations(),
            cancellationToken).ConfigureAwait(false);

    /// <summary>Persists evidence through injectable operations so write failure cleanup is testable.</summary>
    /// <param name="destination">Validated exact temporary destination.</param>
    /// <param name="report">Complete or fail-closed campaign report.</param>
    /// <param name="operations">Bounded filesystem operations.</param>
    /// <param name="cancellationToken">Cancellation honoured before commit.</param>
    /// <returns>A task that completes only after an atomic commit.</returns>
    internal static async Task WriteAsync(
        string destination,
        O5R5PhysicalCampaignReport report,
        IO5R5EvidenceFileOperations operations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(operations);
        string validated = ValidateDestination(destination);
        ValidateReport(report);
        string temporary = validated + ".tmp";
        byte[] encoded = JsonSerializer.SerializeToUtf8Bytes(report, Options);
        if (encoded.Length is < 1 or > MaximumEvidenceBytes)
        {
            throw new InvalidOperationException("o5r5d1.evidence.size_invalid");
        }
        ValidateEncoded(encoded);
        try
        {
            await operations
                .WriteAllBytesAsync(temporary, encoded, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            operations.Move(temporary, validated);
        }
        finally
        {
            if (operations.Exists(temporary))
            {
                operations.Delete(temporary);
            }
        }
    }

    /// <summary>Reads and validates one persisted report without accepting corruption or partial JSON.</summary>
    /// <param name="destination">Exact project-owned temporary evidence path.</param>
    /// <param name="cancellationToken">Cancellation honoured while reading.</param>
    /// <returns>The complete validated non-authorising report.</returns>
    internal static async Task<O5R5PhysicalCampaignReport> ReadValidatedAsync(
        string destination,
        CancellationToken cancellationToken)
    {
        string validated = ValidateDestination(destination);
        FileInfo information = new(validated);
        if (!information.Exists ||
            information.Attributes.HasFlag(FileAttributes.ReparsePoint) ||
            information.Length is < 1 or > MaximumEvidenceBytes)
        {
            throw new InvalidOperationException("o5r5d1.evidence.file_invalid");
        }

        byte[] encoded = await File
            .ReadAllBytesAsync(validated, cancellationToken)
            .ConfigureAwait(false);
        return ValidateEncoded(encoded);
    }

    /// <summary>Deserialises bounded bytes and rejects corrupt or structurally incomplete evidence.</summary>
    /// <param name="encoded">Bounded candidate JSON bytes.</param>
    /// <returns>The complete validated report.</returns>
    private static O5R5PhysicalCampaignReport ValidateEncoded(byte[] encoded)
    {
        try
        {
            O5R5PhysicalCampaignReport? decoded =
                JsonSerializer.Deserialize<O5R5PhysicalCampaignReport>(encoded, Options);
            if (decoded is null)
            {
                throw new InvalidOperationException("o5r5d1.evidence.incomplete");
            }
            ValidateReport(decoded);
            return decoded;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("o5r5d1.evidence.corrupt", exception);
        }
    }

    /// <summary>Validates report identity, completeness and bounded diagnostic membership.</summary>
    /// <param name="report">Candidate deserialised report.</param>
    private static void ValidateReport(O5R5PhysicalCampaignReport report)
    {
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        if (!string.Equals(report.ProtocolVersion, O5R5MeasurementProtocol.Version, StringComparison.Ordinal) ||
            !string.Equals(report.ProtocolDigest, protocol.Digest, StringComparison.Ordinal) ||
            !string.Equals(report.ActivationState, "None", StringComparison.Ordinal) ||
            report.Environment is null ||
            report.Samples is null ||
            report.Summaries is null ||
            report.Failures is null ||
            report.CompletedAtUtc < report.StartedAtUtc ||
            report.ExpectedSampleCount != O5R5PhysicalCampaignDriver.ExpectedSampleCount ||
            report.CompletedSampleCount != report.Samples.Count ||
            report.CompletedSampleCount < 0 ||
            report.CompletedSampleCount > report.ExpectedSampleCount ||
            report.Summaries.Count > 16 ||
            report.Failures.Count > MaximumFailures ||
            string.IsNullOrWhiteSpace(report.Code) ||
            report.Code.Length > 128)
        {
            throw new InvalidOperationException("o5r5d1.evidence.incomplete");
        }

        if (report.Samples.Any(sample => !IsValidSample(sample, protocol)) ||
            report.Summaries.Any(summary => !IsValidSummary(summary)))
        {
            throw new InvalidOperationException("o5r5d1.evidence.membership_invalid");
        }

        foreach (O5R5ThresholdFailure failure in report.Failures)
        {
            ValidateFailure(failure);
        }

        bool thresholdDisposition =
            report.Code.StartsWith("o5r5d1.threshold.", StringComparison.Ordinal);
        if (report.Passed)
        {
            if (report.CompletedSampleCount != report.ExpectedSampleCount ||
                report.Summaries.Count != 16 ||
                report.Failures.Count != 0 ||
                report.Samples.Any(sample => !sample.Passed) ||
                report.Summaries.Any(summary => !summary.Passed))
            {
                throw new InvalidOperationException("o5r5d1.evidence.passing_report_invalid");
            }
        }
        else if (report.Failures.Count > 0)
        {
            if (!thresholdDisposition ||
                !string.Equals(report.Code, report.Failures[0].Code, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("o5r5d1.evidence.failure_disposition_invalid");
            }
        }
        else if (thresholdDisposition)
        {
            throw new InvalidOperationException("o5r5d1.evidence.failure_missing");
        }
    }

    /// <summary>Validates one complete sample and its phase-specific D2 metric shape.</summary>
    /// <param name="sample">Candidate sample.</param>
    /// <param name="protocol">Current frozen protocol.</param>
    /// <returns>True only for finite, bounded and structurally complete evidence.</returns>
    private static bool IsValidSample(
        O5R5MeasurementSample? sample,
        O5R5MeasurementProtocol protocol)
    {
        if (sample is null)
        {
            return false;
        }

        bool firstByte = sample.Phase == O5R5MeasurementPhase.FirstByte;
        bool measuresWorkRate =
            O5R5MeasurementProtocol.MeasuresDeterministicWorkRate(sample.Phase);
        double[] finite =
        [
            sample.ElapsedMilliseconds,
            sample.CpuMilliseconds,
            sample.ElapsedMicrosecondsPerWorkUnit ?? 0d,
            sample.CpuMicrosecondsPerWorkUnit ?? 0d,
            sample.FirstObservationMilliseconds ?? 0d,
            sample.RepeatabilityWindowMilliseconds ?? 0d,
        ];
        bool firstByteShape = firstByte
            ? sample.FirstObservationMilliseconds is not null &&
                sample.RepeatabilityWindowMilliseconds is not null &&
                Math.Abs(
                    sample.ElapsedMilliseconds -
                    sample.FirstObservationMilliseconds.Value -
                    sample.RepeatabilityWindowMilliseconds.Value) <= 0.000_001d
            : sample.FirstObservationMilliseconds is null &&
                sample.RepeatabilityWindowMilliseconds is null;
        bool workShape = measuresWorkRate
            ? sample.ElapsedMicrosecondsPerWorkUnit is not null &&
                sample.CpuMicrosecondsPerWorkUnit is not null
            : sample.ElapsedMicrosecondsPerWorkUnit is null &&
                sample.CpuMicrosecondsPerWorkUnit is null;
        return string.Equals(sample.ProtocolDigest, protocol.Digest, StringComparison.Ordinal) &&
            Enum.IsDefined(sample.Phase) &&
            Enum.IsDefined(sample.Temperature) &&
            sample.Repetition >= 1 &&
            sample.HeapPeakDeltaBytes >= 0 &&
            sample.WorkingSetPeakDeltaBytes >= 0 &&
            sample.AllocationPeakBytes >= 0 &&
            sample.CumulativeAllocatedBytes >= 0 &&
            sample.DeclaredWorkUnits >= 1 &&
            sample.DeclaredWorkUnits <= protocol.Envelope.MaximumWorkUnits &&
            (!firstByte ||
                sample.DeclaredWorkUnits ==
                    O5R5MeasurementProtocol.FirstByteRepeatabilityWorkUnits) &&
            sample.AccountedMemoryBytes >= 1 &&
            sample.AccountedMemoryBytes <= protocol.Envelope.MaximumAccountedMemoryBytes &&
            finite.All(value => double.IsFinite(value) && value >= 0d) &&
            firstByteShape &&
            workShape;
    }

    /// <summary>Validates one summary and the predeclared repeatability basis for its phase.</summary>
    /// <param name="summary">Candidate complete summary.</param>
    /// <returns>True only when FirstByte and other phases retain distinct metric shapes.</returns>
    private static bool IsValidSummary(O5R5MeasurementSummary? summary)
    {
        if (summary is null ||
            !Enum.IsDefined(summary.Phase) ||
            !Enum.IsDefined(summary.Temperature) ||
            !Enum.IsDefined(summary.RepeatabilityBasis))
        {
            return false;
        }

        return summary.Phase == O5R5MeasurementPhase.FirstByte
            ? summary.FirstObservation is not null &&
                summary.RepeatabilityBasis ==
                    O5R5RepeatabilityBasis.FirstByteFixedWindowElapsed
            : summary.FirstObservation is null &&
                summary.RepeatabilityBasis == O5R5RepeatabilityBasis.TotalElapsed;
    }

    /// <summary>Validates one allow-listed diagnostic without accepting arbitrary text or values.</summary>
    /// <param name="failure">Candidate bounded threshold diagnostic.</param>
    private static void ValidateFailure(O5R5ThresholdFailure failure)
    {
        if (failure is null ||
            !Enum.IsDefined(failure.Phase) ||
            !Enum.IsDefined(failure.Temperature) ||
            !Enum.IsDefined(failure.Metric) ||
            !Enum.IsDefined(failure.Unit) ||
            !double.IsFinite(failure.Observed) ||
            !double.IsFinite(failure.InclusiveLimit) ||
            failure.Observed <= failure.InclusiveLimit ||
            failure.InclusiveLimit < 0d ||
            !string.Equals(
                failure.Code,
                $"o5r5d1.threshold.{O5R5MeasurementReadinessRunner.MetricCode(failure.Metric)}",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o5r5d1.evidence.failure_invalid");
        }

        bool batchLevel = failure.Metric == O5R5ThresholdMetric.RepeatabilityCoefficient;
        bool membershipValid = batchLevel
            ? failure.IsWarmUp is null &&
                failure.Repetition is null &&
                failure.Unit == O5R5ThresholdUnit.Ratio
            : failure.IsWarmUp is not null &&
                failure.Repetition is >= 1 &&
                failure.Repetition <= (failure.IsWarmUp.Value
                    ? O5R5MeasurementProtocol.WarmUpRepetitions
                    : O5R5MeasurementProtocol.MeasuredRepetitions) &&
                UnitFor(failure.Metric) == failure.Unit;
        if (!membershipValid)
        {
            throw new InvalidOperationException("o5r5d1.evidence.failure_membership_invalid");
        }
    }

    /// <summary>Returns the only valid unit for one sample-level threshold metric.</summary>
    /// <param name="metric">Allow-listed sample-level metric.</param>
    /// <returns>Exact unit required in persisted evidence.</returns>
    private static O5R5ThresholdUnit UnitFor(O5R5ThresholdMetric metric) =>
        metric switch
        {
            O5R5ThresholdMetric.ElapsedTime or
            O5R5ThresholdMetric.FirstObservationTime =>
                O5R5ThresholdUnit.Milliseconds,
            O5R5ThresholdMetric.ManagedHeapPeak or
            O5R5ThresholdMetric.WorkingSetPeak or
            O5R5ThresholdMetric.AllocationPeak => O5R5ThresholdUnit.Bytes,
            O5R5ThresholdMetric.ElapsedPerWorkUnit or
            O5R5ThresholdMetric.CpuPerWorkUnit =>
                O5R5ThresholdUnit.MicrosecondsPerWorkUnit,
            _ => throw new ArgumentOutOfRangeException(nameof(metric)),
        };
}
