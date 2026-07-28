// Module purpose: Supervises fresh D7 children, performs bounded external snapshots and publishes integrity-checked final evidence.
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DBNotifier.IntegrationTests;

/// <summary>Validates measured D7 evidence and creates the final non-authorising report.</summary>
internal static class O5R5D7EvidenceValidator
{
    /// <summary>Validates one child report, recomputes integrity and binds optional external evidence.</summary>
    /// <param name="measured">Measured child report.</param>
    /// <param name="externalObservation">External evidence for D7-E, otherwise null.</param>
    /// <returns>A complete or fail-closed final report.</returns>
    internal static O5R5D7FinalReport Finalise(
        O5R5D7MeasuredReport measured,
        O5R5D7ExternalObservation? externalObservation)
    {
        ArgumentNullException.ThrowIfNull(measured);
        ValidateIdentity(measured);
        if (!measured.Complete || measured.Summary is null)
        {
            return Report(
                measured,
                "o5r5d7.evidence.incomplete",
                complete: false,
                summaryFailed: false,
                integrity: null,
                externalObservation);
        }

        ValidateSequence(measured.Samples);
        O5R5D7IntegrityResult integrity = O5R5D7Integrity.Recompute(measured);
        if (!integrity.ExactSummaryMatch)
        {
            return Report(
                measured,
                "o5r5d7.contract_mismatch",
                complete: false,
                summaryFailed: false,
                integrity,
                externalObservation);
        }

        bool summaryFailed = IsExactRepeatabilityFailure(measured.Summary, measured.Failures);
        if (!measured.Summary.Passed && !summaryFailed)
        {
            return Report(
                measured,
                "o5r5d7.summary.unexpected_failure",
                complete: false,
                summaryFailed: false,
                integrity,
                externalObservation);
        }

        if ((measured.Arm == O5R5D7Arm.External) != (externalObservation is not null))
        {
            return Report(
                measured,
                "o5r5d7.external.shape_invalid",
                complete: false,
                summaryFailed,
                integrity,
                externalObservation);
        }

        return Report(
            measured,
            summaryFailed
                ? "o5r5d1.threshold.repeatability-coefficient"
                : "o5r5d7.report.complete",
            complete: true,
            summaryFailed,
            integrity,
            externalObservation);
    }

    /// <summary>Returns true only for the exact allow-listed V3 summary failure.</summary>
    /// <param name="summary">Original V3 summary.</param>
    /// <param name="failures">Complete measured failure collection.</param>
    /// <returns>True only for the single frozen FirstByte/Cold repeatability failure.</returns>
    internal static bool IsExactRepeatabilityFailure(
        O5R5MeasurementSummary summary,
        IReadOnlyList<O5R5ThresholdFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(failures);
        if (summary.Passed || summary.Failures.Count != 1 || failures.Count != 1)
        {
            return false;
        }

        O5R5ThresholdFailure failure = summary.Failures[0];
        return ReferenceEquals(failure, failures[0]) || Equivalent(failure, failures[0])
            ? failure.Code == "o5r5d1.threshold.repeatability-coefficient" &&
                failure.Phase == O5R5MeasurementPhase.FirstByte &&
                failure.Temperature == O5R5Temperature.Cold &&
                failure.IsWarmUp is null &&
                failure.Repetition is null &&
                failure.Metric == O5R5ThresholdMetric.RepeatabilityCoefficient &&
                failure.Observed > O5R5D7Protocol.CoefficientLimit &&
                failure.InclusiveLimit == O5R5D7Protocol.CoefficientLimit &&
                failure.Unit == O5R5ThresholdUnit.Ratio
            : false;
    }

    /// <summary>Validates content-addressed identity and bounded run membership.</summary>
    /// <param name="measured">Measured child report to validate.</param>
    private static void ValidateIdentity(O5R5D7MeasuredReport measured)
    {
        O5R5D7MeasuredRunner.ValidateRun(measured.Arm, measured.Run);
        if (measured.ProtocolVersion != O5R5D7Protocol.Version ||
            measured.ProtocolDigest != O5R5D7Protocol.Digest ||
            measured.V3ProtocolDigest != O5R5D7Protocol.V3Digest ||
            measured.D6ProtocolDigest != O5R5D7Protocol.D6Digest ||
            measured.ActivationState != "None" ||
            measured.ExpectedSampleCount != O5R5D7Protocol.ExpectedSampleCount ||
            measured.CompletedSampleCount != measured.Samples.Count ||
            measured.ExpectedSummaryCount != O5R5D7Protocol.ExpectedSummaryCount ||
            measured.CompletedSummaryCount != (measured.Summary is null ? 0 : 1))
        {
            throw new InvalidOperationException("o5r5d7.evidence.identity_invalid");
        }
    }

    /// <summary>Validates exact warm-up and measured sample order.</summary>
    /// <param name="samples">Original V3 samples in execution order.</param>
    private static void ValidateSequence(IReadOnlyList<O5R5MeasurementSample> samples)
    {
        if (samples.Count != O5R5D7Protocol.ExpectedSampleCount ||
            samples.Any(
                sample =>
                    sample.Phase != O5R5MeasurementPhase.FirstByte ||
                    sample.Temperature != O5R5Temperature.Cold))
        {
            throw new InvalidOperationException("o5r5d7.evidence.sequence_invalid");
        }

        for (int index = 0; index < samples.Count; index++)
        {
            bool warmUp = index < O5R5MeasurementProtocol.WarmUpRepetitions;
            int repetition = warmUp
                ? index + 1
                : index - O5R5MeasurementProtocol.WarmUpRepetitions + 1;
            if (samples[index].IsWarmUp != warmUp ||
                samples[index].Repetition != repetition)
            {
                throw new InvalidOperationException("o5r5d7.evidence.sequence_invalid");
            }
        }
    }

    /// <summary>Compares two deserialised bounded failures value by value.</summary>
    /// <param name="left">First bounded failure.</param>
    /// <param name="right">Second bounded failure.</param>
    /// <returns>True when every frozen failure field is equal.</returns>
    private static bool Equivalent(O5R5ThresholdFailure left, O5R5ThresholdFailure right) =>
        left.Code == right.Code &&
        left.Phase == right.Phase &&
        left.Temperature == right.Temperature &&
        left.IsWarmUp == right.IsWarmUp &&
        left.Repetition == right.Repetition &&
        left.Metric == right.Metric &&
        left.Observed == right.Observed &&
        left.InclusiveLimit == right.InclusiveLimit &&
        left.Unit == right.Unit;

    /// <summary>Creates the immutable final report.</summary>
    /// <param name="measured">Validated measured child report.</param>
    /// <param name="code">Stable bounded final outcome code.</param>
    /// <param name="complete">Whether the report satisfies the D7 contract.</param>
    /// <param name="summaryFailed">Whether the exact V3 repeatability failure occurred.</param>
    /// <param name="integrity">Independent recomputation when evidence is complete.</param>
    /// <param name="externalObservation">External evidence for D7-E, otherwise null.</param>
    /// <returns>An immutable non-authorising final report.</returns>
    private static O5R5D7FinalReport Report(
        O5R5D7MeasuredReport measured,
        string code,
        bool complete,
        bool summaryFailed,
        O5R5D7IntegrityResult? integrity,
        O5R5D7ExternalObservation? externalObservation) =>
        new(
            O5R5D7Protocol.Version,
            O5R5D7Protocol.Digest,
            O5R5D7Protocol.V3Digest,
            O5R5D7Protocol.D6Digest,
            "None",
            measured.Arm,
            measured.Run,
            code,
            complete,
            summaryFailed,
            measured,
            integrity,
            externalObservation);
}

/// <summary>Captures the proposal allow-list from outside the measured D7 child.</summary>
internal static class O5R5D7ExternalCounters
{
    /// <summary>Captures one system snapshot without polling or host identity.</summary>
    /// <returns>Available values and stable unavailable counter names.</returns>
    internal static O5R5D7SystemSnapshot CaptureSystem()
    {
        List<string> unavailable = [];
        ulong? idle = null;
        ulong? kernel = null;
        ulong? user = null;
        if (OperatingSystem.IsWindows() &&
            GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime))
        {
            idle = idleTime.Value;
            kernel = kernelTime.Value;
            user = userTime.Value;
        }
        else
        {
            unavailable.Add("system-processor-times");
        }

        uint? memoryLoad = null;
        ulong? availableMemory = null;
        MemoryStatusEx memory = new()
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>(),
        };
        if (OperatingSystem.IsWindows() && GlobalMemoryStatusEx(ref memory))
        {
            memoryLoad = memory.MemoryLoad;
            availableMemory = memory.AvailablePhysical;
        }
        else
        {
            unavailable.Add("system-physical-memory");
        }

        return new O5R5D7SystemSnapshot(
            idle,
            kernel,
            user,
            memoryLoad,
            availableMemory,
            unavailable);
    }

    /// <summary>Captures final child counters from its retained process handle without polling.</summary>
    /// <param name="process">Exited measured child retained by the supervisor.</param>
    /// <returns>Available values and stable unavailable counter names.</returns>
    internal static O5R5D7ProcessSnapshot CaptureProcess(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        List<string> unavailable = [];
        long? userTicks = TryRead(() => process.UserProcessorTime.Ticks, "process-user-cpu", unavailable);
        long? kernelTicks = TryRead(
            () => process.PrivilegedProcessorTime.Ticks,
            "process-kernel-cpu",
            unavailable);
        long? peakWorkingSet = TryRead(
            () => process.PeakWorkingSet64,
            "process-peak-working-set",
            unavailable);
        int? handles = TryRead(() => process.HandleCount, "process-handle-count", unavailable);
        int? threads = TryRead(() => process.Threads.Count, "process-thread-count", unavailable);

        ulong? cycles = null;
        uint? pageFaults = null;
        try
        {
            if (OperatingSystem.IsWindows() &&
                QueryProcessCycleTime(process.Handle, out ulong cycleCount))
            {
                cycles = cycleCount;
            }
            else
            {
                unavailable.Add("process-cycle-count");
            }

            ProcessMemoryCounters counters = new()
            {
                Size = (uint)Marshal.SizeOf<ProcessMemoryCounters>(),
            };
            if (OperatingSystem.IsWindows() &&
                GetProcessMemoryInfo(process.Handle, ref counters, counters.Size))
            {
                pageFaults = counters.PageFaultCount;
            }
            else
            {
                unavailable.Add("process-page-fault-count");
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
                System.ComponentModel.Win32Exception or
                NotSupportedException)
        {
            if (cycles is null && !unavailable.Contains("process-cycle-count", StringComparer.Ordinal))
            {
                unavailable.Add("process-cycle-count");
            }
            if (pageFaults is null &&
                !unavailable.Contains("process-page-fault-count", StringComparer.Ordinal))
            {
                unavailable.Add("process-page-fault-count");
            }
        }

        return new O5R5D7ProcessSnapshot(
            userTicks,
            kernelTicks,
            cycles,
            peakWorkingSet,
            pageFaults,
            handles,
            threads,
            unavailable);
    }

    /// <summary>Reads one managed process counter and records an explicit unavailable value.</summary>
    /// <typeparam name="T">Value-type counter result.</typeparam>
    /// <param name="read">Bounded managed counter accessor.</param>
    /// <param name="code">Stable unavailable counter name.</param>
    /// <param name="unavailable">Collection receiving unavailable counter names.</param>
    /// <returns>The available counter value, otherwise null.</returns>
    private static T? TryRead<T>(Func<T> read, string code, List<string> unavailable)
        where T : struct
    {
        try
        {
            return read();
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
                System.ComponentModel.Win32Exception or
                NotSupportedException)
        {
            unavailable.Add(code);
            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct FileTime
    {
        private readonly uint low;
        private readonly uint high;

        internal ulong Value => ((ulong)high << 32) | low;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        internal uint Length;
        internal uint MemoryLoad;
        internal ulong TotalPhysical;
        internal ulong AvailablePhysical;
        internal ulong TotalPageFile;
        internal ulong AvailablePageFile;
        internal ulong TotalVirtual;
        internal ulong AvailableVirtual;
        internal ulong AvailableExtendedVirtual;
    }

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
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(
        out FileTime idleTime,
        out FileTime kernelTime,
        out FileTime userTime);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryProcessCycleTime(IntPtr processHandle, out ulong cycleTime);

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessMemoryInfo(
        IntPtr processHandle,
        ref ProcessMemoryCounters counters,
        uint size);
}

/// <summary>Exposes the marker-gated D7 supervisor and sole child-launch boundary.</summary>
public static class O5R5D7SupervisorProcess
{
    internal const string ActivationMarker = "pf-obs-1-d7-supervisor-test-only";
    private static readonly TimeSpan ProcessDeadline = TimeSpan.FromMinutes(5);

    /// <summary>Runs one fresh child, validates integrity and publishes final evidence.</summary>
    /// <param name="args">Activation, output, arm, run and sanitised environment arguments.</param>
    /// <returns>Zero for complete evidence, two for invalid activation or three for failure.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (!O5R5D7Arguments.TryParse(args, ActivationMarker, out O5R5D7Arguments? parsed))
        {
            Console.Error.WriteLine("o5r5d7.supervisor.activation_invalid");
            return 2;
        }

        string? measuredDestination = null;
        try
        {
            string finalDestination = O5R5D7EvidenceWriter.ValidateFinalDestination(
                parsed.Output,
                parsed.Arm,
                parsed.Run);
            measuredDestination = O5R5D7EvidenceWriter.MeasuredDestination(finalDestination);
            if (File.Exists(measuredDestination) || File.Exists(finalDestination))
            {
                throw new InvalidOperationException("o5r5d7.output.exists");
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
                throw new InvalidOperationException("o5r5d7.measured.missing");
            }

            O5R5D7MeasuredReport measured = O5R5D7EvidenceWriter.ReadMeasured(measuredDestination);
            O5R5D7FinalReport report = O5R5D7EvidenceValidator.Finalise(measured, external);
            await O5R5D7EvidenceWriter
                .WriteFinalAsync(finalDestination, report, CancellationToken.None)
                .ConfigureAwait(false);
            Console.WriteLine(report.Code);
            return child.ExitCode == 0 && report.Complete ? 0 : 3;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"o5r5d7.supervisor.failed:{exception.GetType().Name}");
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

    /// <summary>Starts the current sandbox host as one exact measured child.</summary>
    /// <param name="parsed">Validated supervisor arguments.</param>
    /// <returns>The newly started measured child process.</returns>
    private static Process StartMeasuredChild(O5R5D7Arguments parsed)
    {
        string processPath = Environment.ProcessPath ??
            throw new InvalidOperationException("o5r5d7.supervisor.process_path_missing");
        string entryAssembly = Assembly.GetEntryAssembly()?.Location ??
            throw new InvalidOperationException("o5r5d7.supervisor.entry_missing");
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
        start.ArgumentList.Add(O5R5D7MeasuredProcess.ActivationMarker);
        start.ArgumentList.Add("--output");
        start.ArgumentList.Add(parsed.Output);
        start.ArgumentList.Add("--arm");
        start.ArgumentList.Add(parsed.Arm.ToString());
        start.ArgumentList.Add("--run");
        start.ArgumentList.Add(parsed.Run.ToString(System.Globalization.CultureInfo.InvariantCulture));
        start.ArgumentList.Add("--sdk");
        start.ArgumentList.Add(parsed.Sdk);
        start.ArgumentList.Add("--installed-memory");
        start.ArgumentList.Add(
            parsed.InstalledMemory.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Process.Start(start) ??
            throw new InvalidOperationException("o5r5d7.supervisor.start_failed");
    }
}
