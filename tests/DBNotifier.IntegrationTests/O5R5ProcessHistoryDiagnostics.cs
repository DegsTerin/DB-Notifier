// Module purpose: Reproduces bounded V3 process-history prefixes and attributes residual Cancellation/Cold resource growth without changing the physical protocol.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DBNotifier.IntegrationTests;

/// <summary>Identifies one preregistered D5 history comparison.</summary>
internal enum O5R5D5HistoryVariant
{
    ExactPrefix = 0,
    WithoutFirstByte = 1,
    WithoutIdle = 2,
    CancellationOnly = 3,
}

/// <summary>Contains the frozen identity, matrix and immutable D5 diagnostic boundaries.</summary>
internal static class O5R5D5DiagnosticProtocol
{
    internal const string Version = "pfobs1-d5-process-history-diagnostic-1.0.0";
    internal const string Digest = "82606BF31085214607C8CBE401C0F4050D9465523B9B01F89FFE45731012FFDA";
    internal const string V3Digest = "60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A";
    internal const int RunsPerVariant = 2;
    internal const int ExactPrefixSampleCount = 158;
    internal const int ReducedPrefixSampleCount = 88;
    internal const int CancellationOnlySampleCount = 18;
    internal const int CancellationMeasuredRepetitions = 13;
    internal const long WorkingSetLimitBytes = 786_432;
    internal const string CanonicalStatement =
        "pfobs1-d5-process-history-diagnostic|1.0.0|" +
        "v3=60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A|" +
        "runs=2|variants=exact-prefix,without-first-byte,without-idle,cancellation-only|" +
        "precondition=v3-exact|" +
        "prefix=first-byte-cold-5-30,first-byte-warm-5-30,idle-cold-5-30," +
        "idle-warm-5-30,cancellation-cold-5-13|" +
        "metrics=working-set,private-memory,gc-heap,gc-committed,os-threads,threadpool," +
        "handles,allocated,committed-private,committed-mapped,committed-image," +
        "committed-unknown|limit=786432|samples=all|forced-gc=v3-two-existing-only";

    /// <summary>Calculates the exact SHA-256 identity of the preregistered statement.</summary>
    /// <returns>An upper-case hexadecimal SHA-256 digest.</returns>
    internal static string CalculateDigest() =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalStatement)));

    /// <summary>Returns the exact retained sample count for one comparison variant.</summary>
    /// <param name="variant">Preregistered history comparison.</param>
    /// <returns>The exact number of complete samples that must be retained.</returns>
    internal static int ExpectedSampleCount(O5R5D5HistoryVariant variant) =>
        variant switch
        {
            O5R5D5HistoryVariant.ExactPrefix => ExactPrefixSampleCount,
            O5R5D5HistoryVariant.WithoutFirstByte => ReducedPrefixSampleCount,
            O5R5D5HistoryVariant.WithoutIdle => ReducedPrefixSampleCount,
            O5R5D5HistoryVariant.CancellationOnly => CancellationOnlySampleCount,
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };
}

/// <summary>Captures bounded process, GC, thread and committed-region counters without host identity.</summary>
/// <param name="WorkingSetBytes">Current physical working set.</param>
/// <param name="PrivateMemoryBytes">Current private commit reported by the process.</param>
/// <param name="GcHeapBytes">Current managed heap size.</param>
/// <param name="GcCommittedBytes">Current GC committed bytes.</param>
/// <param name="OsThreadCount">Current operating-system thread count for this process.</param>
/// <param name="ThreadPoolThreadCount">Current managed ThreadPool thread count.</param>
/// <param name="HandleCount">Current operating-system handle count.</param>
/// <param name="TotalAllocatedBytes">Process-wide cumulative managed allocation.</param>
/// <param name="CommittedPrivateBytes">Committed private virtual-memory regions.</param>
/// <param name="CommittedMappedBytes">Committed mapped virtual-memory regions.</param>
/// <param name="CommittedImageBytes">Committed image virtual-memory regions.</param>
/// <param name="CommittedUnknownBytes">Committed regions with an unrecognised type.</param>
internal sealed record O5R5D5ProcessMetrics(
    long WorkingSetBytes,
    long PrivateMemoryBytes,
    long GcHeapBytes,
    long GcCommittedBytes,
    int OsThreadCount,
    int ThreadPoolThreadCount,
    int HandleCount,
    long TotalAllocatedBytes,
    long CommittedPrivateBytes,
    long CommittedMappedBytes,
    long CommittedImageBytes,
    long CommittedUnknownBytes);

/// <summary>Retains one complete sample and the surrounding process-state observations.</summary>
/// <param name="Sequence">One-based position in the preregistered history.</param>
/// <param name="Sample">Complete unchanged V3 measurement sample.</param>
/// <param name="Before">Process state immediately before the sample.</param>
/// <param name="After">Process state immediately after the sample.</param>
internal sealed record O5R5D5SampleObservation(
    int Sequence,
    O5R5MeasurementSample Sample,
    O5R5D5ProcessMetrics Before,
    O5R5D5ProcessMetrics After);

/// <summary>Reports one complete fresh-process D5 comparison without authorising Observer activity.</summary>
/// <param name="ProtocolVersion">Frozen D5 protocol version.</param>
/// <param name="ProtocolDigest">Frozen D5 protocol digest.</param>
/// <param name="V3ProtocolDigest">Unchanged V3 physical protocol digest.</param>
/// <param name="ActivationState">Observer activation state, which remains None.</param>
/// <param name="Variant">Preregistered history comparison.</param>
/// <param name="Run">One-based fresh-process run index.</param>
/// <param name="ExpectedSampleCount">Exact expected prefix size.</param>
/// <param name="CompletedSampleCount">Complete retained sample count.</param>
/// <param name="Code">Stable sanitised disposition.</param>
/// <param name="Complete">Whether the exact prefix completed without missing evidence.</param>
/// <param name="WorkingSetExcessReproduced">Whether Cancellation/Cold exceeded its unchanged limit.</param>
/// <param name="BeforePrecondition">Process state before the exact V3 precondition.</param>
/// <param name="AfterPrecondition">Process state after the exact V3 precondition.</param>
/// <param name="Samples">Every complete prefix sample with surrounding resource state.</param>
internal sealed record O5R5D5DiagnosticReport(
    string ProtocolVersion,
    string ProtocolDigest,
    string V3ProtocolDigest,
    string ActivationState,
    O5R5D5HistoryVariant Variant,
    int Run,
    int ExpectedSampleCount,
    int CompletedSampleCount,
    string Code,
    bool Complete,
    bool WorkingSetExcessReproduced,
    O5R5D5ProcessMetrics BeforePrecondition,
    O5R5D5ProcessMetrics AfterPrecondition,
    IReadOnlyList<O5R5D5SampleObservation> Samples)
{
    /// <summary>Gets a value that is always false because D5 evidence cannot authorise activation.</summary>
    internal static bool IsAuthorising => false;
}

/// <summary>Reads D5 process state through built-in .NET and Windows APIs only.</summary>
internal sealed class O5R5D5ProcessMetricSource : IDisposable
{
    private const uint MemCommit = 0x1000;
    private const uint MemPrivate = 0x20000;
    private const uint MemMapped = 0x40000;
    private const uint MemImage = 0x1000000;
    private const uint ThreadSnapshot = 0x00000004;
    private static readonly nint InvalidHandle = new(-1);
    private readonly Process process = Process.GetCurrentProcess();

    /// <summary>Captures one complete metric set or fails closed before evidence publication.</summary>
    /// <returns>Sanitised process, GC, thread, handle and committed-region counters.</returns>
    internal O5R5D5ProcessMetrics Capture()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("o5r5d5.windows_required");
        }

        D5ProcessMemoryCounters counters = new()
        {
            Size = checked((uint)Marshal.SizeOf<D5ProcessMemoryCounters>()),
        };
        if (!GetProcessMemoryInfo(process.Handle, ref counters, counters.Size) ||
            !GetProcessHandleCount(process.Handle, out uint handleCount))
        {
            throw new InvalidOperationException("o5r5d5.process_metric_unavailable");
        }

        (long privateBytes, long mappedBytes, long imageBytes, long unknownBytes) =
            CaptureCommittedRegions();
        GCMemoryInfo memory = GC.GetGCMemoryInfo();
        return new O5R5D5ProcessMetrics(
            checked((long)counters.WorkingSetSize.ToUInt64()),
            checked((long)counters.PrivateUsage.ToUInt64()),
            memory.HeapSizeBytes,
            memory.TotalCommittedBytes,
            CaptureOsThreadCount(),
            ThreadPool.ThreadCount,
            checked((int)handleCount),
            GC.GetTotalAllocatedBytes(precise: false),
            privateBytes,
            mappedBytes,
            imageBytes,
            unknownBytes);
    }

    /// <summary>Releases the cached process handle after one diagnostic process.</summary>
    public void Dispose() => process.Dispose();

    /// <summary>Enumerates committed regions and groups their sizes by the Windows region type.</summary>
    /// <returns>Committed private, mapped, image and unknown byte totals.</returns>
    private (long Private, long Mapped, long Image, long Unknown) CaptureCommittedRegions()
    {
        ulong address = 0;
        long privateBytes = 0;
        long mappedBytes = 0;
        long imageBytes = 0;
        long unknownBytes = 0;
        int structureSize = Marshal.SizeOf<D5MemoryBasicInformation>();
        while (true)
        {
            nuint result = VirtualQueryEx(
                process.Handle,
                new nint(unchecked((long)address)),
                out D5MemoryBasicInformation information,
                checked((nuint)structureSize));
            if (result == 0)
            {
                break;
            }

            ulong regionSize = information.RegionSize.ToUInt64();
            if (information.State == MemCommit)
            {
                long boundedSize = checked((long)regionSize);
                switch (information.Type)
                {
                    case MemPrivate:
                        privateBytes = checked(privateBytes + boundedSize);
                        break;
                    case MemMapped:
                        mappedBytes = checked(mappedBytes + boundedSize);
                        break;
                    case MemImage:
                        imageBytes = checked(imageBytes + boundedSize);
                        break;
                    default:
                        unknownBytes = checked(unknownBytes + boundedSize);
                        break;
                }
            }

            ulong next = checked(information.BaseAddress.ToUInt64() + regionSize);
            if (next <= address)
            {
                throw new InvalidOperationException("o5r5d5.memory_region_invalid");
            }
            address = next;
        }

        return (privateBytes, mappedBytes, imageBytes, unknownBytes);
    }

    /// <summary>Counts operating-system threads owned by this exact diagnostic process.</summary>
    /// <returns>The bounded current thread count.</returns>
    private static int CaptureOsThreadCount()
    {
        nint snapshot = CreateToolhelp32Snapshot(ThreadSnapshot, 0);
        if (snapshot == InvalidHandle)
        {
            throw new InvalidOperationException("o5r5d5.thread_metric_unavailable");
        }

        try
        {
            D5ThreadEntry entry = new()
            {
                Size = checked((uint)Marshal.SizeOf<D5ThreadEntry>()),
            };
            int count = 0;
            if (!Thread32First(snapshot, ref entry))
            {
                throw new InvalidOperationException("o5r5d5.thread_metric_unavailable");
            }

            do
            {
                if (entry.OwnerProcessId == checked((uint)Environment.ProcessId))
                {
                    count = checked(count + 1);
                }
                entry.Size = checked((uint)Marshal.SizeOf<D5ThreadEntry>());
            }
            while (Thread32Next(snapshot, ref entry));

            return count;
        }
        finally
        {
            _ = CloseHandle(snapshot);
        }
    }

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessMemoryInfo(
        nint processHandle,
        ref D5ProcessMemoryCounters counters,
        uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessHandleCount(nint processHandle, out uint handleCount);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nuint VirtualQueryEx(
        nint processHandle,
        nint address,
        out D5MemoryBasicInformation information,
        nuint length);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Thread32First(nint snapshot, ref D5ThreadEntry entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Thread32Next(nint snapshot, ref D5ThreadEntry entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct D5ProcessMemoryCounters
    {
        internal uint Size;
        internal uint PageFaultCount;
        internal UIntPtr PeakWorkingSetSize;
        internal UIntPtr WorkingSetSize;
        internal UIntPtr QuotaPeakPagedPoolUsage;
        internal UIntPtr QuotaPagedPoolUsage;
        internal UIntPtr QuotaPeakNonPagedPoolUsage;
        internal UIntPtr QuotaNonPagedPoolUsage;
        internal UIntPtr PagefileUsage;
        internal UIntPtr PeakPagefileUsage;
        internal UIntPtr PrivateUsage;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D5MemoryBasicInformation
    {
        internal UIntPtr BaseAddress;
        internal UIntPtr AllocationBase;
        internal uint AllocationProtect;
        internal ushort PartitionId;
        internal UIntPtr RegionSize;
        internal uint State;
        internal uint Protect;
        internal uint Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D5ThreadEntry
    {
        internal uint Size;
        internal uint Usage;
        internal uint ThreadId;
        internal uint OwnerProcessId;
        internal int BasePriority;
        internal int DeltaPriority;
        internal uint Flags;
    }
}

/// <summary>Executes one preregistered process-history comparison using unchanged V3 scenarios.</summary>
internal sealed class O5R5D5DiagnosticRunner
{
    private readonly O5R5PhysicalCampaignDriver driver;
    private readonly O5R5MeasurementReadinessRunner runner;
    private readonly O5R5D5ProcessMetricSource processMetrics;

    /// <summary>Initialises the D5 runner over the unchanged frozen V3 driver and physical source.</summary>
    /// <param name="measurementSource">Built-in source shared with the V3 scenarios.</param>
    /// <param name="processMetrics">D5-only process-state source.</param>
    internal O5R5D5DiagnosticRunner(
        IO5R5MeasurementSource measurementSource,
        O5R5D5ProcessMetricSource processMetrics)
    {
        ArgumentNullException.ThrowIfNull(measurementSource);
        this.processMetrics = processMetrics ?? throw new ArgumentNullException(nameof(processMetrics));
        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        driver = new O5R5PhysicalCampaignDriver(protocol, measurementSource);
        runner = new O5R5MeasurementReadinessRunner(
            O5R5MeasurementReadinessRunner.Marker,
            protocol,
            measurementSource);
    }

    /// <summary>Runs the exact precondition and one complete selected process-history prefix.</summary>
    /// <param name="variant">Preregistered history comparison.</param>
    /// <param name="run">One-based fresh-process run index.</param>
    /// <param name="cancellationToken">Bounded process deadline.</param>
    /// <returns>Complete non-authorising D5 evidence, including failed V3 samples.</returns>
    internal async Task<O5R5D5DiagnosticReport> RunAsync(
        O5R5D5HistoryVariant variant,
        int run,
        CancellationToken cancellationToken)
    {
        if (run is < 1 or > O5R5D5DiagnosticProtocol.RunsPerVariant)
        {
            throw new ArgumentOutOfRangeException(nameof(run));
        }

        O5R5D5ProcessMetrics beforePrecondition = processMetrics.Capture();
        await driver.PreconditionAsync(cancellationToken).ConfigureAwait(false);
        O5R5D5ProcessMetrics afterPrecondition = processMetrics.Capture();
        IReadOnlyList<O5R5MeasurementScenario> scenarios = SelectScenarios(
            driver.CreateScenarios(),
            variant);
        List<O5R5D5SampleObservation> observations = new(scenarios.Count);

        foreach (O5R5MeasurementScenario scenario in scenarios)
        {
            cancellationToken.ThrowIfCancellationRequested();
            O5R5D5ProcessMetrics before = processMetrics.Capture();
            O5R5MeasurementResult result = await runner
                .RunAsync(scenario, cancellationToken)
                .ConfigureAwait(false);
            O5R5D5ProcessMetrics after = processMetrics.Capture();
            if (result.Disposition != O5R5MeasurementDisposition.Accepted ||
                result.Sample is null)
            {
                throw new InvalidOperationException("o5r5d5.sample_incomplete");
            }

            observations.Add(
                new O5R5D5SampleObservation(
                    observations.Count + 1,
                    result.Sample,
                    before,
                    after));
        }

        int expected = O5R5D5DiagnosticProtocol.ExpectedSampleCount(variant);
        bool complete = observations.Count == expected;
        bool reproduced = observations.Any(
            observation =>
                observation.Sample.Phase == O5R5MeasurementPhase.Cancellation &&
                observation.Sample.Temperature == O5R5Temperature.Cold &&
                observation.Sample.WorkingSetPeakDeltaBytes >
                    O5R5D5DiagnosticProtocol.WorkingSetLimitBytes);
        return new O5R5D5DiagnosticReport(
            O5R5D5DiagnosticProtocol.Version,
            O5R5D5DiagnosticProtocol.Digest,
            O5R5D5DiagnosticProtocol.V3Digest,
            "None",
            variant,
            run,
            expected,
            observations.Count,
            complete ? "o5r5d5.history.complete" : "o5r5d5.history.incomplete",
            complete,
            reproduced,
            beforePrecondition,
            afterPrecondition,
            observations.AsReadOnly());
    }

    /// <summary>Selects one stable prefix without reordering or replacing retained V3 samples.</summary>
    /// <param name="allScenarios">Exact stable V3 scenario matrix.</param>
    /// <param name="variant">Preregistered history comparison.</param>
    /// <returns>The exact selected prefix in original V3 order.</returns>
    internal static IReadOnlyList<O5R5MeasurementScenario> SelectScenarios(
        IReadOnlyList<O5R5MeasurementScenario> allScenarios,
        O5R5D5HistoryVariant variant)
    {
        ArgumentNullException.ThrowIfNull(allScenarios);
        List<O5R5MeasurementScenario> selected = [];
        foreach (O5R5MeasurementScenario scenario in allScenarios)
        {
            bool includeHistory = scenario.Phase switch
            {
                O5R5MeasurementPhase.FirstByte =>
                    variant is O5R5D5HistoryVariant.ExactPrefix or
                        O5R5D5HistoryVariant.WithoutIdle,
                O5R5MeasurementPhase.Idle =>
                    variant is O5R5D5HistoryVariant.ExactPrefix or
                        O5R5D5HistoryVariant.WithoutFirstByte,
                O5R5MeasurementPhase.Cancellation =>
                    scenario.Temperature == O5R5Temperature.Cold &&
                    (scenario.IsWarmUp ||
                        scenario.Repetition <=
                            O5R5D5DiagnosticProtocol.CancellationMeasuredRepetitions),
                _ => false,
            };
            if (includeHistory)
            {
                selected.Add(scenario);
            }
        }

        int expected = O5R5D5DiagnosticProtocol.ExpectedSampleCount(variant);
        if (selected.Count != expected)
        {
            throw new InvalidOperationException("o5r5d5.prefix_invalid");
        }

        return selected.AsReadOnly();
    }
}

/// <summary>Persists one complete D5 fresh-process report atomically in an exact temporary root.</summary>
internal static class O5R5D5EvidenceWriter
{
    private const string RootPrefix = "DBNotifier-PF-OBS-1-D5-";
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Validates an exact D5 destination below one direct project-owned temporary child.</summary>
    /// <param name="path">Candidate report path.</param>
    /// <param name="variant">Expected history variant.</param>
    /// <param name="run">Expected one-based run index.</param>
    /// <returns>The canonical validated destination.</returns>
    internal static string ValidateDestination(
        string? path,
        O5R5D5HistoryVariant variant,
        int run)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            run is < 1 or > O5R5D5DiagnosticProtocol.RunsPerVariant)
        {
            throw new InvalidOperationException("o5r5d5.output.required");
        }

        string destination = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(destination);
        string temporaryRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        string expectedName = $"{VariantCode(variant)}-run-{run}.json";
        if (directory is null ||
            !string.Equals(
                Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(directory)),
                temporaryRoot,
                StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(directory).StartsWith(RootPrefix, StringComparison.Ordinal) ||
            !string.Equals(Path.GetFileName(destination), expectedName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o5r5d5.output.path_refused");
        }

        return destination;
    }

    /// <summary>Writes complete D5 evidence through a write-through sibling and atomic move.</summary>
    /// <param name="destination">Validated destination.</param>
    /// <param name="report">Complete report.</param>
    /// <param name="cancellationToken">Cancellation checked before durable publication.</param>
    internal static async Task WriteAsync(
        string destination,
        O5R5D5DiagnosticReport report,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        string canonical = ValidateDestination(destination, report.Variant, report.Run);
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

    /// <summary>Maps one allow-listed variant to its stable file code.</summary>
    /// <param name="variant">History comparison.</param>
    /// <returns>Stable lower-case file code.</returns>
    internal static string VariantCode(O5R5D5HistoryVariant variant) =>
        variant switch
        {
            O5R5D5HistoryVariant.ExactPrefix => "exact-prefix",
            O5R5D5HistoryVariant.WithoutFirstByte => "without-first-byte",
            O5R5D5HistoryVariant.WithoutIdle => "without-idle",
            O5R5D5HistoryVariant.CancellationOnly => "cancellation-only",
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };
}

/// <summary>Exposes the sole marker-gated D5 fresh-process diagnostic entrypoint.</summary>
public static class O5R5D5ProcessHistoryDiagnosticProcess
{
    internal const string ActivationMarker = "pf-obs-1-d5-process-history-diagnostic-test-only";
    private static readonly TimeSpan ProcessDeadline = TimeSpan.FromMinutes(5);

    /// <summary>Validates exact arguments, executes one comparison and atomically retains all samples.</summary>
    /// <param name="args">Exact activation, output, variant and run arguments.</param>
    /// <returns>Zero after complete evidence, two for invalid activation or three for failure.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 8 ||
            args[0] != "--activation" ||
            args[1] != ActivationMarker ||
            args[2] != "--output" ||
            args[4] != "--variant" ||
            !Enum.TryParse(args[5], ignoreCase: true, out O5R5D5HistoryVariant variant) ||
            args[6] != "--run" ||
            !int.TryParse(args[7], out int run))
        {
            Console.Error.WriteLine("o5r5d5.activation_invalid");
            return 2;
        }

        try
        {
            string destination = O5R5D5EvidenceWriter.ValidateDestination(args[3], variant, run);
            using O5R5DotNetMeasurementSource measurementSource = new();
            using O5R5D5ProcessMetricSource processMetrics = new();
            O5R5D5DiagnosticRunner runner = new(measurementSource, processMetrics);
            using CancellationTokenSource deadline = new(ProcessDeadline);
            O5R5D5DiagnosticReport report = await runner
                .RunAsync(variant, run, deadline.Token)
                .ConfigureAwait(false);
            await O5R5D5EvidenceWriter
                .WriteAsync(destination, report, CancellationToken.None)
                .ConfigureAwait(false);
            Console.WriteLine(report.Code);
            return report.Complete ? 0 : 3;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"o5r5d5.failed:{exception.GetType().Name}");
            return 3;
        }
    }
}
