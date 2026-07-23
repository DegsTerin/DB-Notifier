// Module purpose: Implements serial, queue-free and fenced O1 resource admission with a separately reserved bounded control lane.
namespace DBNotifier.IntegrationTests;

/// <summary>Represents one deterministic resource-admission outcome and its optional fenced lease.</summary>
internal sealed record O1ResourceAdmission(
    bool Accepted,
    string Code,
    O1ResourceLease? Lease);

/// <summary>Owns one serial data-plane reservation until the caller proves quiescence.</summary>
internal sealed class O1ResourceLease : IDisposable
{
    private readonly O1ResourceCoordinator owner;
    private bool quiescent;
    private bool disposed;

    /// <summary>Initialises one lease owned by an exact coordinator fence.</summary>
    /// <param name="owner">Owning coordinator.</param>
    /// <param name="fence">Monotonic fence.</param>
    internal O1ResourceLease(O1ResourceCoordinator owner, long fence)
    {
        this.owner = owner;
        Fence = fence;
    }

    /// <summary>Gets the monotonic fence that prevents an old lease from releasing newer capacity.</summary>
    internal long Fence { get; }

    /// <summary>Marks the bounded synthetic worker tree quiescent before capacity release.</summary>
    internal void MarkQuiescent() => quiescent = true;

    /// <summary>Releases only the matching active fence and records whether quiescence was proved.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        owner.Release(Fence, quiescent);
    }
}

/// <summary>Owns one independent bounded control-plane reservation.</summary>
internal sealed class O1ControlLease : IDisposable
{
    private readonly O1ResourceCoordinator owner;
    private bool disposed;

    /// <summary>Initialises the independent control lease.</summary>
    /// <param name="owner">Owning coordinator.</param>
    internal O1ControlLease(O1ResourceCoordinator owner) => this.owner = owner;

    /// <summary>Releases the control lane exactly once.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        owner.ReleaseControl();
    }
}

/// <summary>Coordinates one bounded serial O1 sandbox resource domain without queueing or fairness claims.</summary>
internal sealed class O1ResourceCoordinator
{
    private readonly object sync = new();
    private readonly O1ResourceEnvelope envelope;
    private readonly Func<DateTimeOffset> utcNow;
    private long nextFence;
    private long? activeFence;
    private bool controlActive;

    /// <summary>Initialises one coordinator from the finite envelope bound into the active trust context.</summary>
    /// <param name="envelope">Finite signed resource envelope.</param>
    /// <param name="utcNow">Deterministic UTC clock.</param>
    internal O1ResourceCoordinator(
        O1ResourceEnvelope envelope,
        Func<DateTimeOffset> utcNow)
    {
        this.envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        this.utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        if (envelope.MaximumParallelism != 1 || envelope.QueueEnabled)
        {
            throw new ArgumentException(
                "O1 starts with serial execution and a disabled queue.",
                nameof(envelope));
        }
    }

    /// <summary>Gets the stable outcome of the most recent data-plane release.</summary>
    internal string LastReleaseCode { get; private set; } = "resource.not_released";

    /// <summary>Attempts one atomic finite data-plane reservation without queueing.</summary>
    /// <param name="request">Exact declared resource demand.</param>
    /// <param name="absoluteDeadlineUtc">Deadline recorded before reservation.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>Accepted fenced lease or one stable fail-closed code.</returns>
    internal O1ResourceAdmission TryAdmit(
        O1ResourceRequest request,
        DateTimeOffset absoluteDeadlineUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested)
        {
            return new O1ResourceAdmission(false, "resource.cancelled", null);
        }
        if (utcNow() >= absoluteDeadlineUtc)
        {
            return new O1ResourceAdmission(false, "resource.deadline", null);
        }

        string? invalid = ValidateDeclaration(request);
        if (invalid is not null)
        {
            return new O1ResourceAdmission(false, invalid, null);
        }

        lock (sync)
        {
            if (activeFence.HasValue)
            {
                return new O1ResourceAdmission(false, "resource.capacity_global", null);
            }
            long fence;
            try
            {
                fence = checked(++nextFence);
            }
            catch (OverflowException)
            {
                return new O1ResourceAdmission(false, "resource.counter_exhausted", null);
            }
            activeFence = fence;
            LastReleaseCode = "resource.active";
            return new O1ResourceAdmission(
                true,
                "resource.accepted",
                new O1ResourceLease(this, fence));
        }
    }

    /// <summary>Attempts one reserved control-plane slot independently of saturated evaluation capacity.</summary>
    /// <param name="metadataEntries">Bounded active-head and audit metadata entries.</param>
    /// <param name="absoluteDeadlineUtc">Control-phase deadline.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>An independent control lease or <see langword="null"/> when refused.</returns>
    internal O1ControlLease? TryAdmitControl(
        int metadataEntries,
        DateTimeOffset absoluteDeadlineUtc,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested ||
            utcNow() >= absoluteDeadlineUtc ||
            metadataEntries is < 1 ||
            metadataEntries > envelope.MaximumControlMetadataEntries)
        {
            return null;
        }
        lock (sync)
        {
            if (controlActive)
            {
                return null;
            }
            controlActive = true;
            return new O1ControlLease(this);
        }
    }

    /// <summary>Releases a data lease only when its fence still owns capacity.</summary>
    /// <param name="fence">Lease fence.</param>
    /// <param name="quiescent">Whether the caller proved all synthetic work stopped.</param>
    internal void Release(long fence, bool quiescent)
    {
        lock (sync)
        {
            if (activeFence != fence)
            {
                LastReleaseCode = "resource.stale_fence";
                return;
            }
            activeFence = null;
            LastReleaseCode = quiescent
                ? "resource.released"
                : "resource.released_after_forced_fence";
        }
    }

    /// <summary>Releases the independent control slot.</summary>
    internal void ReleaseControl()
    {
        lock (sync)
        {
            controlActive = false;
        }
    }

    /// <summary>Validates every declared dimension with checked aggregate arithmetic before lease creation.</summary>
    /// <param name="request">Declared demand.</param>
    /// <returns>Stable failure code or <see langword="null"/>.</returns>
    private string? ValidateDeclaration(O1ResourceRequest request)
    {
        if (request.InputBytes < 0 ||
            request.StructureDepth < 0 ||
            request.Items < 0 ||
            request.AccountedMemoryBytes < 0 ||
            request.WorkUnits < 0 ||
            request.Results < 0 ||
            request.OutputBytes < 0)
        {
            return "resource.declaration_invalid";
        }
        if (request.InputBytes > envelope.MaximumInputBytes)
        {
            return "resource.input_bytes";
        }
        if (request.StructureDepth > envelope.MaximumStructureDepth)
        {
            return "resource.structure_depth";
        }
        if (request.Items > envelope.MaximumItems)
        {
            return "resource.items";
        }
        if (request.AccountedMemoryBytes > envelope.MaximumAccountedMemoryBytes)
        {
            return "resource.memory";
        }
        if (request.WorkUnits > envelope.MaximumWorkUnits)
        {
            return "resource.work";
        }
        if (request.Results > envelope.MaximumResults)
        {
            return "resource.results";
        }
        if (request.OutputBytes > envelope.MaximumOutputBytes)
        {
            return "resource.output_bytes";
        }

        try
        {
            _ = checked(request.InputBytes + request.AccountedMemoryBytes + request.OutputBytes);
            _ = checked(request.WorkUnits + request.Items + request.Results);
        }
        catch (OverflowException)
        {
            return "resource.arithmetic_overflow";
        }
        return null;
    }
}

/// <summary>Builds the complete 24 trust, 36 resource and eight corpus vector-to-code/test/owner map.</summary>
internal static class O1VectorCatalogue
{
    private static readonly System.Collections.ObjectModel.ReadOnlyCollection<O1VectorTrace> Traces = Build();

    /// <summary>Gets all 68 deterministic O1 vector mappings.</summary>
    internal static IReadOnlyList<O1VectorTrace> Entries => Traces;

    /// <summary>Builds every declared vector exactly once with a concrete component, test group and owner.</summary>
    /// <returns>Read-only complete vector map.</returns>
    private static System.Collections.ObjectModel.ReadOnlyCollection<O1VectorTrace> Build()
    {
        List<O1VectorTrace> entries = [];
        for (int index = 1; index <= 24; index++)
        {
            entries.Add(
                new O1VectorTrace(
                    $"TR-{index:00}",
                    index switch
                    {
                        <= 16 => nameof(O1TrustCoordinator),
                        <= 23 => nameof(O1SandboxStore),
                        _ => nameof(O1ResourceCoordinator),
                    },
                    index switch
                    {
                        <= 8 => nameof(O1TrustSandboxTests.TrustContinuityRejectsRollbackGapDivergenceAndReplay),
                        <= 16 => nameof(O1TrustSandboxTests.CryptographicRolesScopeAndDualControlFailClosed),
                        <= 23 => nameof(O1TrustSandboxTests.CrashRestartAndRecoveryPreserveAtomicContinuity),
                        _ => nameof(O1TrustSandboxTests.ControlCapacityRemainsIndependentAndStalePublicationFails),
                    },
                    index switch
                    {
                        <= 8 => "Host trust coordinator",
                        <= 16 => "Root and policy governance",
                        <= 23 => "Durable checkpoint owner",
                        _ => "Security reviewer",
                    }));
        }
        for (int index = 1; index <= 36; index++)
        {
            entries.Add(
                new O1VectorTrace(
                    $"RE-{index:00}",
                    nameof(O1ResourceCoordinator),
                    index switch
                    {
                        <= 12 => nameof(O1TrustSandboxTests.ResourceDimensionsRejectBeforeAllocation),
                        <= 24 => nameof(O1TrustSandboxTests.SerialCapacityCancellationDeadlineAndFencingAreDeterministic),
                        _ => nameof(O1TrustSandboxTests.ControlCapacityRemainsIndependentAndStalePublicationFails),
                    },
                    index is 20 or 21 or 25 or 32
                        ? "Evaluation owner"
                        : "Host resource coordinator"));
        }
        for (int index = 1; index <= 8; index++)
        {
            entries.Add(
                new O1VectorTrace(
                    $"CO-{index:00}",
                    nameof(O1CorpusVerifier),
                    nameof(O1TrustSandboxTests.CorpusManifestRequiresApprovalSignatureAndExactMembership),
                    index is 1 or 2 or 8
                        ? "Dataset owner and data governance"
                        : "Corpus attestation and checkpoint owners"));
        }
        return Array.AsReadOnly(entries.ToArray());
    }
}
