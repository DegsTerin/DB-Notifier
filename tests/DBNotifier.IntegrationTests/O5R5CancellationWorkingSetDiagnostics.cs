// Module purpose: Isolates PF-OBS-1-D4 Cancellation/Cold resource growth without changing the frozen V3 campaign protocol.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DBNotifier.IntegrationTests;

/// <summary>Identifies one cumulative cancellation implementation used by the isolated D4 comparison.</summary>
internal enum O5R5D4CancellationStrategy
{
    AsyncDelay = 0,
    WaitHandle = 1,
}

/// <summary>Identifies one predeclared resource checkpoint in the D4 cancellation decomposition.</summary>
internal enum O5R5D4DiagnosticStage
{
    Baseline = 0,
    Materialisation = 1,
    Checkpoint = 2,
    LinkedToken = 3,
    CancelAfter = 4,
    Observation = 5,
    Disposal = 6,
}

/// <summary>Contains the D4 protocol identity and immutable diagnostic limits.</summary>
internal static class O5R5D4DiagnosticProtocol
{
    internal const string Version = "pfobs1-d4-cancellation-diagnostic-1.0.0";
    internal const string Digest = "FBA236A73DCBA87BA7247394082080AF9EB72A7D8AFE8CD3667C44A24EA5EDB7";
    internal const int RetainedRepetitions = 35;
    internal const int PreparationRepetitions = 1;
    internal const long WorkingSetLimitBytes = 786_432;
    internal const int InvocationDeadlineMilliseconds = 100;
    internal const string CanonicalStatement =
        "pfobs1-d4-cancellation-diagnostic|1.0.0|repetitions=35|warmup=1|" +
        "stages=baseline,materialisation,checkpoint,linked-token,cancel-after,observation,disposal|" +
        "metrics=working-set,private-memory,gc-heap,gc-committed,threads,handles,allocated," +
        "cts-active,timer-active,wait-active|limit=786432|deadline-ms=100|" +
        "strategies=async-delay,wait-handle|samples=all";

    /// <summary>Calculates the exact SHA-256 identity of the predeclared canonical statement.</summary>
    /// <returns>An upper-case hexadecimal SHA-256 digest.</returns>
    internal static string CalculateDigest() =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalStatement)));
}

/// <summary>Represents sanitised D4-owned cancellation resources at one checkpoint.</summary>
/// <param name="LinkedTokenSources">Active linked token sources owned by the invocation.</param>
/// <param name="CancellationTimers">Active scheduled cancellation timers owned by the invocation.</param>
/// <param name="Waits">Active cancellation waits owned by the invocation.</param>
internal readonly record struct O5R5D4OwnedResources(
    int LinkedTokenSources,
    int CancellationTimers,
    int Waits);

/// <summary>Captures built-in process and GC counters without host identity or payload.</summary>
/// <param name="WorkingSetBytes">Current process working set.</param>
/// <param name="PrivateMemoryBytes">Current process private memory.</param>
/// <param name="GcHeapBytes">Current managed heap size.</param>
/// <param name="GcCommittedBytes">Current GC committed bytes.</param>
/// <param name="ThreadCount">Current managed ThreadPool thread count.</param>
/// <param name="HandleCount">Current process handle count.</param>
/// <param name="TotalAllocatedBytes">Process-wide cumulative managed allocation.</param>
internal readonly record struct O5R5D4ProcessMetrics(
    long WorkingSetBytes,
    long PrivateMemoryBytes,
    long GcHeapBytes,
    long GcCommittedBytes,
    int ThreadCount,
    int HandleCount,
    long TotalAllocatedBytes);

/// <summary>Retains one complete predeclared D4 stage observation.</summary>
/// <param name="Stage">Exact stage label.</param>
/// <param name="Metrics">Sanitised process and GC metrics.</param>
/// <param name="OwnedResources">D4-owned cancellation resources.</param>
internal sealed record O5R5D4StageObservation(
    O5R5D4DiagnosticStage Stage,
    O5R5D4ProcessMetrics Metrics,
    O5R5D4OwnedResources OwnedResources);

/// <summary>Retains every stage and gate result for one D4 diagnostic invocation.</summary>
/// <param name="Repetition">One-based retained repetition.</param>
/// <param name="ElapsedMilliseconds">Complete cancellation observation duration.</param>
/// <param name="CancellationObserved">Whether the inner ten-millisecond cancellation was observed.</param>
/// <param name="PeakWorkingSetDeltaBytes">Peak working-set delta from the invocation baseline.</param>
/// <param name="PeakPrivateMemoryDeltaBytes">Peak private-memory delta from the invocation baseline.</param>
/// <param name="PeakGcCommittedDeltaBytes">Peak GC committed-memory delta from the invocation baseline.</param>
/// <param name="PeakThreadDelta">Peak process thread-count delta from the invocation baseline.</param>
/// <param name="PeakHandleDelta">Peak process handle-count delta from the invocation baseline.</param>
/// <param name="Passed">Whether the immutable D4 bounds and disposal rules passed.</param>
/// <param name="Observations">All seven stage observations in predeclared order.</param>
internal sealed record O5R5D4DiagnosticSample(
    int Repetition,
    double ElapsedMilliseconds,
    bool CancellationObserved,
    long PeakWorkingSetDeltaBytes,
    long PeakPrivateMemoryDeltaBytes,
    long PeakGcCommittedDeltaBytes,
    int PeakThreadDelta,
    int PeakHandleDelta,
    bool Passed,
    IReadOnlyList<O5R5D4StageObservation> Observations);

/// <summary>Reports one isolated, complete and non-authorising D4 strategy execution.</summary>
/// <param name="ProtocolVersion">Frozen D4 diagnostic protocol version.</param>
/// <param name="ProtocolDigest">Frozen D4 diagnostic protocol digest.</param>
/// <param name="ActivationState">Lifecycle state, which remains None.</param>
/// <param name="Strategy">Exact isolated comparison strategy.</param>
/// <param name="Code">Sanitised terminal diagnostic code.</param>
/// <param name="Passed">Whether every retained sample passed the immutable D4 limits.</param>
/// <param name="Samples">All 35 retained samples.</param>
internal sealed record O5R5D4DiagnosticReport(
    string ProtocolVersion,
    string ProtocolDigest,
    string ActivationState,
    O5R5D4CancellationStrategy Strategy,
    string Code,
    bool Passed,
    IReadOnlyList<O5R5D4DiagnosticSample> Samples);

/// <summary>Captures D4-only process detail through built-in .NET and Windows APIs.</summary>
internal sealed class O5R5D4ProcessMetricSource : IDisposable
{
    private readonly Process process = Process.GetCurrentProcess();

    /// <summary>Captures one complete metric set or fails before diagnostic evidence is emitted.</summary>
    /// <returns>Sanitised process and GC metrics.</returns>
    internal O5R5D4ProcessMetrics Capture()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("o5r5d4.windows_required");
        }

        ProcessMemoryCounters counters = new()
        {
            Size = checked((uint)Marshal.SizeOf<ProcessMemoryCounters>()),
        };
        if (!GetProcessMemoryInfo(process.Handle, ref counters, counters.Size) ||
            !GetProcessHandleCount(process.Handle, out uint handleCount))
        {
            throw new InvalidOperationException("o5r5d4.process_metric_unavailable");
        }

        GCMemoryInfo memory = GC.GetGCMemoryInfo();
        return new O5R5D4ProcessMetrics(
            checked((long)counters.WorkingSetSize.ToUInt64()),
            checked((long)counters.PrivateUsage.ToUInt64()),
            memory.HeapSizeBytes,
            memory.TotalCommittedBytes,
            ThreadPool.ThreadCount,
            checked((int)handleCount),
            GC.GetTotalAllocatedBytes(precise: false));
    }

    /// <summary>Releases the cached process handle after the isolated diagnostic.</summary>
    public void Dispose() => process.Dispose();

    /// <summary>Native process-memory structure used without allocating per checkpoint.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessMemoryCounters
    {
        internal uint Size;
        internal uint PageFaultCount;
        internal nuint PeakWorkingSetSize;
        internal nuint WorkingSetSize;
        internal nuint QuotaPeakPagedPoolUsage;
        internal nuint QuotaPagedPoolUsage;
        internal nuint QuotaPeakNonPagedPoolUsage;
        internal nuint QuotaNonPagedPoolUsage;
        internal nuint PagefileUsage;
        internal nuint PeakPagefileUsage;
        internal nuint PrivateUsage;
    }

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessMemoryInfo(
        IntPtr processHandle,
        ref ProcessMemoryCounters counters,
        uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessHandleCount(IntPtr processHandle, out uint handleCount);

}

/// <summary>
/// Runs the exact post-D3 cancellation path or the bounded wait-handle candidate while retaining
/// every predeclared process, GC and owned-resource checkpoint.
/// </summary>
internal sealed class O5R5D4CancellationDiagnosticRunner
{
    private static readonly TimeSpan CancellationDuration = TimeSpan.FromMilliseconds(10);
    private readonly O5R5CancellationInputBuffer input;
    private readonly O5R5D4ProcessMetricSource metrics;
    private int observableSink;

    /// <summary>Initialises one isolated diagnostic over the exact D3 input size.</summary>
    /// <param name="metrics">D4-only process metric source.</param>
    internal O5R5D4CancellationDiagnosticRunner(O5R5D4ProcessMetricSource metrics)
    {
        this.metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        byte[] source = GC.AllocateUninitializedArray<byte>(
            checked((int)O1ResourceEnvelope.Fixture().MaximumInputBytes));
        for (int index = 0; index < source.Length; index++)
        {
            source[index] = (byte)(index % 251);
        }
        input = new O5R5CancellationInputBuffer(source);
    }

    /// <summary>Runs one non-retained preparation and exactly 35 retained diagnostic invocations.</summary>
    /// <param name="strategy">Exact isolated cancellation observation strategy.</param>
    /// <param name="cancellationToken">Whole-process diagnostic cancellation.</param>
    /// <returns>A complete report containing every sample, including failures.</returns>
    internal async Task<O5R5D4DiagnosticReport> RunAsync(
        O5R5D4CancellationStrategy strategy,
        CancellationToken cancellationToken)
    {
        for (int repetition = 0;
             repetition < O5R5D4DiagnosticProtocol.PreparationRepetitions;
             repetition++)
        {
            await RunInvocationAsync(strategy, 0, retain: false, cancellationToken)
                .ConfigureAwait(false);
        }

        List<O5R5D4DiagnosticSample> samples =
            new(O5R5D4DiagnosticProtocol.RetainedRepetitions);
        for (int repetition = 1;
             repetition <= O5R5D4DiagnosticProtocol.RetainedRepetitions;
             repetition++)
        {
            O5R5D4DiagnosticSample? sample = await RunInvocationAsync(
                    strategy,
                    repetition,
                    retain: true,
                    cancellationToken)
                .ConfigureAwait(false);
            samples.Add(sample!);
        }

        bool passed = samples.All(sample => sample.Passed);
        return new O5R5D4DiagnosticReport(
            O5R5D4DiagnosticProtocol.Version,
            O5R5D4DiagnosticProtocol.Digest,
            "None",
            strategy,
            passed ? "o5r5d4.diagnostic.accepted" : "o5r5d4.diagnostic.limit_exceeded",
            passed,
            samples.AsReadOnly());
    }

    /// <summary>Executes one decomposed cancellation invocation without selecting its result.</summary>
    /// <param name="strategy">Exact cancellation observation strategy.</param>
    /// <param name="repetition">One-based retained repetition, or zero for preparation.</param>
    /// <param name="retain">Whether to materialise the seven stage observations.</param>
    /// <param name="cancellationToken">Whole-process diagnostic cancellation.</param>
    /// <returns>A complete retained sample, or <see langword="null"/> for preparation.</returns>
    private async Task<O5R5D4DiagnosticSample?> RunInvocationAsync(
        O5R5D4CancellationStrategy strategy,
        int repetition,
        bool retain,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<O5R5D4StageObservation>? observations = retain ? new(7) : null;
        O5R5D4OwnedResources owned = default;
        Capture(O5R5D4DiagnosticStage.Baseline, owned, observations);
        ReadOnlyMemory<byte> materialised = input.Materialise(O5R5Temperature.Cold);
        Capture(O5R5D4DiagnosticStage.Materialisation, owned, observations);

        // The checkpoint is deliberately represented by the existing bounded campaign source.
        using O5R5DotNetMeasurementSource campaignSource = new();
        O5R5MeasurementContext context = new(campaignSource);
        context.CaptureCheckpoint();
        Capture(O5R5D4DiagnosticStage.Checkpoint, owned, observations);

        CancellationTokenSource? inner = null;
        Stopwatch elapsed = new();
        bool observed = false;
        try
        {
            inner = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            owned = owned with { LinkedTokenSources = 1 };
            Capture(O5R5D4DiagnosticStage.LinkedToken, owned, observations);

            inner.CancelAfter(CancellationDuration);
            owned = owned with { CancellationTimers = 1 };
            Capture(O5R5D4DiagnosticStage.CancelAfter, owned, observations);

            owned = owned with { Waits = 1 };
            elapsed.Start();
            observed = strategy switch
            {
                O5R5D4CancellationStrategy.AsyncDelay =>
                    await ObserveThroughAsyncDelayAsync(inner, cancellationToken)
                        .ConfigureAwait(false),
                O5R5D4CancellationStrategy.WaitHandle =>
                    ObserveThroughWaitHandle(inner, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(strategy)),
            };
            elapsed.Stop();
            owned = owned with { CancellationTimers = 0, Waits = 0 };
            Capture(O5R5D4DiagnosticStage.Observation, owned, observations);
            Volatile.Write(ref observableSink, materialised.Span[0]);
        }
        finally
        {
            inner?.Dispose();
            owned = default;
            Capture(O5R5D4DiagnosticStage.Disposal, owned, observations);
            if (elapsed.IsRunning)
            {
                elapsed.Stop();
            }
        }

        if (!retain)
        {
            return null;
        }

        O5R5D4StageObservation baseline = observations![0];
        long peakWorkingSet = PeakDelta(
            observations,
            baseline.Metrics.WorkingSetBytes,
            observation => observation.Metrics.WorkingSetBytes);
        long peakPrivateMemory = PeakDelta(
            observations,
            baseline.Metrics.PrivateMemoryBytes,
            observation => observation.Metrics.PrivateMemoryBytes);
        long peakGcCommitted = PeakDelta(
            observations,
            baseline.Metrics.GcCommittedBytes,
            observation => observation.Metrics.GcCommittedBytes);
        int peakThreads = checked((int)PeakDelta(
            observations,
            baseline.Metrics.ThreadCount,
            observation => observation.Metrics.ThreadCount));
        int peakHandles = checked((int)PeakDelta(
            observations,
            baseline.Metrics.HandleCount,
            observation => observation.Metrics.HandleCount));
        bool resourcesReleased =
            observations[^1].OwnedResources == default;
        bool passed =
            observed &&
            elapsed.Elapsed.TotalMilliseconds <=
                O5R5D4DiagnosticProtocol.InvocationDeadlineMilliseconds &&
            peakWorkingSet <= O5R5D4DiagnosticProtocol.WorkingSetLimitBytes &&
            resourcesReleased;
        return new O5R5D4DiagnosticSample(
            repetition,
            elapsed.Elapsed.TotalMilliseconds,
            observed,
            peakWorkingSet,
            peakPrivateMemory,
            peakGcCommitted,
            peakThreads,
            peakHandles,
            passed,
            observations.AsReadOnly());
    }

    /// <summary>Reproduces the exact post-D3 asynchronous cancellation observation.</summary>
    /// <param name="inner">Linked source with the ten-millisecond cancellation scheduled.</param>
    /// <param name="outer">Whole diagnostic process cancellation.</param>
    /// <returns><see langword="true"/> only when inner cancellation is observed first.</returns>
    private static async Task<bool> ObserveThroughAsyncDelayAsync(
        CancellationTokenSource inner,
        CancellationToken outer)
    {
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, inner.Token).ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException) when (
            inner.IsCancellationRequested &&
            !outer.IsCancellationRequested)
        {
            return true;
        }
    }

    /// <summary>Observes the same linked cancellation without an asynchronous delay continuation.</summary>
    /// <param name="inner">Linked source with the ten-millisecond cancellation scheduled.</param>
    /// <param name="outer">Whole diagnostic process cancellation.</param>
    /// <returns><see langword="true"/> only when inner cancellation is observed first.</returns>
    internal static bool ObserveThroughWaitHandle(
        CancellationTokenSource inner,
        CancellationToken outer)
    {
        bool signalled = inner.Token.WaitHandle.WaitOne(
            O5R5D4DiagnosticProtocol.InvocationDeadlineMilliseconds);
        outer.ThrowIfCancellationRequested();
        return signalled && inner.IsCancellationRequested;
    }

    /// <summary>Captures one stage only when the invocation is retained.</summary>
    /// <param name="stage">Exact predeclared stage.</param>
    /// <param name="owned">Current D4-owned resource state.</param>
    /// <param name="observations">Retained destination, or null during preparation.</param>
    private void Capture(
        O5R5D4DiagnosticStage stage,
        O5R5D4OwnedResources owned,
        List<O5R5D4StageObservation>? observations)
    {
        O5R5D4ProcessMetrics snapshot = metrics.Capture();
        if (observations is not null)
        {
            observations.Add(new O5R5D4StageObservation(stage, snapshot, owned));
        }
    }

    /// <summary>Returns the non-negative peak delta for one observed metric.</summary>
    /// <param name="observations">All retained stage observations.</param>
    /// <param name="baseline">Invocation baseline value.</param>
    /// <param name="selector">Metric selector.</param>
    /// <returns>Maximum non-negative delta from baseline.</returns>
    private static long PeakDelta(
        IReadOnlyList<O5R5D4StageObservation> observations,
        long baseline,
        Func<O5R5D4StageObservation, long> selector) =>
        Math.Max(0, observations.Max(selector) - baseline);
}

/// <summary>Persists complete D4 diagnostic evidence atomically inside one exact temporary root.</summary>
internal static class O5R5D4DiagnosticEvidenceWriter
{
    private const string RootPrefix = "DBNotifier-PF-OBS-1-D4-";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>Validates a bounded D4 evidence destination inside a direct temporary child.</summary>
    /// <param name="path">Candidate JSON destination.</param>
    /// <returns>Canonical validated destination.</returns>
    internal static string ValidateDestination(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("o5r5d4.output.required");
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
            Path.GetFileName(destination) is not ("async-delay.json" or "wait-handle.json"))
        {
            throw new InvalidOperationException("o5r5d4.output.path_refused");
        }

        return destination;
    }

    /// <summary>Writes one complete report through a write-through temporary sibling and atomic move.</summary>
    /// <param name="destination">Validated exact destination.</param>
    /// <param name="report">Complete diagnostic report.</param>
    /// <param name="cancellationToken">Cancellation checked before durable publication.</param>
    internal static async Task WriteAsync(
        string destination,
        O5R5D4DiagnosticReport report,
        CancellationToken cancellationToken)
    {
        string canonical = ValidateDestination(destination);
        Directory.CreateDirectory(Path.GetDirectoryName(canonical)!);
        string temporary = canonical + ".tmp";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(
            report,
            SerializerOptions);
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

/// <summary>Exposes the sole marker-gated D4 diagnostic process entrypoint.</summary>
public static class O5R5D4CancellationDiagnosticProcess
{
    internal const string ActivationMarker = "pf-obs-1-d4-cancellation-diagnostic-test-only";
    private static readonly TimeSpan ProcessDeadline = TimeSpan.FromSeconds(30);

    /// <summary>Validates exact arguments and retains one isolated strategy report.</summary>
    /// <param name="args">Exact activation, output and strategy arguments.</param>
    /// <returns>Zero after complete evidence, two for invalid activation or three for failure.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 6 ||
            args[0] != "--activation" ||
            args[1] != ActivationMarker ||
            args[2] != "--output" ||
            args[4] != "--strategy" ||
            !Enum.TryParse(args[5], ignoreCase: true, out O5R5D4CancellationStrategy strategy))
        {
            Console.Error.WriteLine("o5r5d4.activation_invalid");
            return 2;
        }

        try
        {
            string destination = O5R5D4DiagnosticEvidenceWriter.ValidateDestination(args[3]);
            using O5R5D4ProcessMetricSource source = new();
            O5R5D4CancellationDiagnosticRunner runner = new(source);
            using CancellationTokenSource deadline = new(ProcessDeadline);
            O5R5D4DiagnosticReport report = await runner.RunAsync(strategy, deadline.Token)
                .ConfigureAwait(false);
            await O5R5D4DiagnosticEvidenceWriter.WriteAsync(
                    destination,
                    report,
                    CancellationToken.None)
                .ConfigureAwait(false);
            Console.WriteLine(report.Code);
            return 0;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"o5r5d4.failed:{exception.GetType().Name}");
            return 3;
        }
    }
}
