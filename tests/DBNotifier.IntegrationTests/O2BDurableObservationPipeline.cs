// Module purpose: Composes durable O2-B continuity, bounded admission and sanitised observability around O2-A.
using System.Collections.Concurrent;
using System.Text.Json;
using DBNotifier.Application.AIOps;
using DBNotifier.Application.Synchronization;

namespace DBNotifier.IntegrationTests;

/// <summary>Exposes only stable codes, counters and bounded cardinalities from the O2-B sandbox.</summary>
internal sealed record O2BObservabilitySnapshot(
    IReadOnlyList<O2BCounterRecord> Counters,
    long HighestContiguousSequence,
    int PendingCount,
    int RetainedOutcomeCount,
    int RetainedPublicationCount,
    long TotalPublications,
    bool Quarantined,
    string? QuarantineCode);

/// <summary>
/// Wraps one fresh O2-A in-memory analysis session with an authenticated durable ledger. Publication becomes visible
/// only after its complete, non-authorising record has crossed the atomic O2-B commit boundary.
/// </summary>
internal sealed class O2BDurableObservationPipeline : IObservationIngestionStore, IAsyncDisposable
{
    internal const string ActivationMarker = "o2b-durable-pipeline-sandbox";
    private static readonly Guid ScopeId = Guid.Parse("a2a00000-0000-4000-8000-000000000001");
    private readonly SemaphoreSlim admissionGate = new(1, 1);
    private readonly ConcurrentDictionary<string, long> transientCounters = new(StringComparer.Ordinal);
    private readonly O2BDurablePipelineStore store;
    private readonly O2ACanonicalObservationPipeline? inner;
    private readonly string? sessionRoot;
    private readonly TimeProvider timeProvider;
    private O2BStoreSnapshot snapshot;
    private bool faulted;
    private bool disposed;

    /// <summary>Initialises one already opened durable session.</summary>
    private O2BDurableObservationPipeline(
        O2BDurablePipelineStore store,
        O2BStoreSnapshot snapshot,
        O2ACanonicalObservationPipeline? inner,
        string? sessionRoot,
        TimeProvider timeProvider,
        Guid agentId,
        Guid instanceId)
    {
        this.store = store;
        this.snapshot = snapshot;
        this.inner = inner;
        this.sessionRoot = sessionRoot;
        this.timeProvider = timeProvider;
        AgentId = agentId;
        InstanceId = instanceId;
    }

    /// <summary>Gets the exact synthetic Agent stream owned by this sandbox.</summary>
    internal Guid AgentId { get; }

    /// <summary>Gets the exact synthetic instance owned by this sandbox.</summary>
    internal Guid InstanceId { get; }

    /// <summary>Gets retained durable non-authorising publications.</summary>
    internal IReadOnlyList<O2BDurablePublicationRecord> Publications => snapshot.Payload.Publications;

    /// <summary>Gets the total durable publication count independent of bounded retained detail.</summary>
    internal long TotalPublications => snapshot.Payload.TotalPublications;

    /// <summary>Gets the current authenticated store generation for crash and rollback evidence.</summary>
    internal long Generation => snapshot.Generation;

    /// <summary>Gets the durable monotonic O1 context revision retained across O2-B sessions.</summary>
    internal string DurableContextRevision => snapshot.Payload.ContextRevision;

    /// <summary>Gets the current sanitised quarantine code, if continuity failed closed.</summary>
    internal string? QuarantineCode => snapshot.QuarantineCode;

    /// <summary>Gets the O1 resource release code so O2-B can prove every completed lease became quiescent.</summary>
    internal string? LastResourceReleaseCode => inner?.LastResourceReleaseCode;

    /// <summary>Gets or sets the exact fault injected into the next admission commit.</summary>
    internal O2BCommitFault NextAdmissionFault { get; set; }

    /// <summary>Gets or sets the exact fault injected into the next completion commit.</summary>
    internal O2BCommitFault NextCompletionFault { get; set; }

    /// <summary>Gets or sets a bounded test-only hook after durable admission and before O2-A processing.</summary>
    internal Func<CancellationToken, Task>? BeforeProcessingAsync { get; set; }

    /// <summary>Gets or sets the next deterministic O2-A deadline.</summary>
    internal DateTimeOffset? NextDeadlineUtc
    {
        get => inner?.NextDeadlineUtc;
        set
        {
            EnsureActive();
            inner!.NextDeadlineUtc = value;
        }
    }

    /// <summary>Gets or sets the next deterministic O2-A resource request.</summary>
    internal O1ResourceRequest? NextResourceRequest
    {
        get => inner?.NextResourceRequest;
        set
        {
            EnsureActive();
            inner!.NextResourceRequest = value;
        }
    }

    /// <summary>Gets or sets the exact O2-A pre-publication hook.</summary>
    internal Func<CancellationToken, Task>? BeforePublicationAsync
    {
        get => inner?.BeforePublicationAsync;
        set
        {
            EnsureActive();
            inner!.BeforePublicationAsync = value;
        }
    }

    /// <summary>Advances the exact synthetic O1 trust context through its reserved control lane.</summary>
    /// <param name="cancellationToken">Cancellation propagated through the monotonic context update.</param>
    internal Task AdvanceTrustAsync(CancellationToken cancellationToken = default)
    {
        EnsureActive();
        return inner!.AdvanceTrustAsync(cancellationToken);
    }

    /// <summary>
    /// Opens one exact temporary O2-B root, rotates its writer fence and resumes any contiguous durable pending work.
    /// </summary>
    /// <param name="rootPath">Direct operating-system temporary child with the O2-B prefix.</param>
    /// <param name="agentId">Synthetic Agent stream identity.</param>
    /// <param name="instanceId">Synthetic instance identity.</param>
    /// <param name="timeProvider">Deterministic sandbox clock.</param>
    /// <param name="cancellationToken">Cancellation propagated through open and recovery.</param>
    /// <returns>An active pipeline or a quarantined fail-closed diagnostic instance.</returns>
    internal static async Task<O2BDurableObservationPipeline> CreateAsync(
        string rootPath,
        Guid agentId,
        Guid instanceId,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        O2BDurablePipelineStore store = new(rootPath);
        O2BStoreSnapshot snapshot = await store
            .OpenSessionAsync(agentId, instanceId, ScopeId, cancellationToken)
            .ConfigureAwait(false);
        if (snapshot.Quarantined)
        {
            return new O2BDurableObservationPipeline(
                store,
                snapshot,
                null,
                null,
                timeProvider,
                agentId,
                instanceId);
        }

        string sessionRoot = Path.Combine(
            store.RootPath,
            "sessions",
            $"session-{snapshot.Payload.WriterFence}");
        Directory.CreateDirectory(sessionRoot);
        O2ACanonicalObservationPipeline inner = await O2ACanonicalObservationPipeline
            .CreateAsync(sessionRoot, agentId, instanceId, timeProvider, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            inner.RestoreDurableBaseline(
                snapshot.Payload.HighestContiguousSequence,
                snapshot.Payload.MessageDigests
                    .Where(item => item.Sequence <= snapshot.Payload.HighestContiguousSequence)
                    .ToDictionary(item => item.Id, item => item.Digest),
                snapshot.Payload.ObservationDigests
                    .Where(item => item.Sequence <= snapshot.Payload.HighestContiguousSequence)
                    .ToDictionary(item => item.Id, item => item.Digest));
            await inner.AlignDurableContextAsync(snapshot.Payload.ContextRevision, cancellationToken)
                .ConfigureAwait(false);
            O2BDurableObservationPipeline pipeline = new(
                store,
                snapshot,
                inner,
                sessionRoot,
                timeProvider,
                agentId,
                instanceId);
            await pipeline.ProcessContiguousAsync(cancellationToken).ConfigureAwait(false);
            return pipeline;
        }
        catch
        {
            inner.Dispose();
            DeleteSession(sessionRoot);
            throw;
        }
    }

    /// <summary>
    /// Durably admits one bounded observation, processes contiguous work and commits complete publication exactly once.
    /// </summary>
    /// <param name="message">Production-contract message containing only authorised synthetic fixture values.</param>
    /// <param name="receivedAt">Authoritative synthetic receipt instant.</param>
    /// <param name="cancellationToken">Cancellation propagated through admission and analysis.</param>
    /// <returns>Accepted, duplicate, retryable saturation or stable fail-closed refusal.</returns>
    public async ValueTask<ObservationItemResult> IngestAsync(
        ObservationSyncMessage message,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!await admissionGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            transientCounters.AddOrUpdate("o2b.backpressure.busy", 1, (_, count) => checked(count + 1));
            return Retryable(message.MessageId, "o2b.backpressure.busy");
        }
        try
        {
            if (snapshot.Quarantined)
            {
                return Rejected(message.MessageId, snapshot.QuarantineCode ?? "o2b.store.quarantined");
            }
            if (faulted)
            {
                return Retryable(message.MessageId, "o2b.pipeline.restart_required");
            }
            if (message.AgentId != AgentId || message.InstanceId != InstanceId)
            {
                return Rejected(message.MessageId, "o2b.pipeline.identity_mismatch");
            }

            string digest = O1CanonicalCryptography.Digest(message);
            O2BDigestRecord? existingMessage = snapshot.Payload.MessageDigests
                .SingleOrDefault(item => item.Id == message.MessageId);
            if (existingMessage is not null)
            {
                if (!string.Equals(existingMessage.Digest, digest, StringComparison.Ordinal))
                {
                    return Rejected(message.MessageId, "o2b.idempotency_conflict");
                }
                if (snapshot.Payload.Pending.Any(item => item.Message.MessageId == message.MessageId))
                {
                    await ProcessContiguousAsync(cancellationToken).ConfigureAwait(false);
                }
                return Duplicate(message.MessageId);
            }
            O2BDigestRecord? existingObservation = snapshot.Payload.ObservationDigests
                .SingleOrDefault(item => item.Id == message.ObservationId);
            if (existingObservation is not null)
            {
                return string.Equals(existingObservation.Digest, digest, StringComparison.Ordinal)
                    ? Duplicate(message.MessageId)
                    : Rejected(message.MessageId, "o2b.observation_conflict");
            }
            if (message.Sequence <= snapshot.Payload.HighestContiguousSequence ||
                snapshot.Payload.Pending.Any(item => item.Message.Sequence == message.Sequence))
            {
                return Rejected(message.MessageId, "o2b.sequence_conflict");
            }
            if (snapshot.Payload.Pending.Count >= O2BDurablePipelineStore.MaximumPending)
            {
                snapshot = await store.CommitAsync(
                        snapshot,
                        snapshot.Payload with
                        {
                            Counters = O2BDurablePipelineStore.Increment(
                                snapshot.Payload.Counters,
                                "o2b.backpressure.saturated"),
                        },
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return Retryable(message.MessageId, "o2b.backpressure.saturated");
            }

            O2BDurablePipelinePayload admitted = snapshot.Payload with
            {
                MessageDigests = RetainDigests(
                    [.. snapshot.Payload.MessageDigests, new(message.MessageId, message.Sequence, digest)],
                    snapshot.Payload.Pending.Select(item => item.Message.MessageId).Append(message.MessageId)),
                ObservationDigests = RetainDigests(
                    [.. snapshot.Payload.ObservationDigests, new(message.ObservationId, message.Sequence, digest)],
                    snapshot.Payload.Pending.Select(item => item.Message.ObservationId).Append(message.ObservationId)),
                Pending = snapshot.Payload.Pending
                    .Append(new O2BPendingObservation(message, receivedAt))
                    .OrderBy(item => item.Message.Sequence)
                    .ToArray(),
                Counters = O2BDurablePipelineStore.Increment(
                    snapshot.Payload.Counters,
                    "o2b.pipeline.admitted"),
            };
            O2BCommitFault admissionFault = NextAdmissionFault;
            NextAdmissionFault = O2BCommitFault.None;
            try
            {
                snapshot = await store
                    .CommitAsync(snapshot, admitted, admissionFault, cancellationToken)
                    .ConfigureAwait(false);
                Func<CancellationToken, Task>? hook = BeforeProcessingAsync;
                BeforeProcessingAsync = null;
                if (hook is not null)
                {
                    await hook(cancellationToken).ConfigureAwait(false);
                }
                await ProcessContiguousAsync(cancellationToken).ConfigureAwait(false);
                return Accepted(message.MessageId);
            }
            catch (O2BInjectedCrashException)
            {
                faulted = true;
                throw;
            }
            catch (OperationCanceledException)
            {
                faulted = true;
                throw;
            }
        }
        finally
        {
            admissionGate.Release();
        }
    }

    /// <summary>Returns the authenticated highest contiguous sequence for the exact synthetic Agent only.</summary>
    /// <param name="agentId">Requested Agent stream identity.</param>
    /// <param name="cancellationToken">Cancellation checked before reading the snapshot.</param>
    /// <returns>Zero for another Agent or the durable high-water mark for the owned Agent.</returns>
    public ValueTask<long> GetHighestContiguousSequenceAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(
            agentId == AgentId ? snapshot.Payload.HighestContiguousSequence : 0L);
    }

    /// <summary>Returns sanitised codes and bounded counters without identities, provider values or payload content.</summary>
    internal O2BObservabilitySnapshot Observe()
    {
        Dictionary<string, long> counters = snapshot.Payload.Counters
            .ToDictionary(item => item.Code, item => item.Count, StringComparer.Ordinal);
        foreach ((string code, long count) in transientCounters)
        {
            counters[code] = checked(counters.GetValueOrDefault(code) + count);
        }
        return new O2BObservabilitySnapshot(
            counters.OrderBy(item => item.Key, StringComparer.Ordinal)
                .Select(item => new O2BCounterRecord(item.Key, item.Value))
                .ToArray(),
            snapshot.Payload.HighestContiguousSequence,
            snapshot.Payload.Pending.Count,
            snapshot.Payload.Outcomes.Count,
            snapshot.Payload.Publications.Count,
            snapshot.Payload.TotalPublications,
            snapshot.Quarantined,
            snapshot.QuarantineCode);
    }

    /// <summary>Processes durable contiguous items in order and commits each outcome and publication atomically.</summary>
    private async Task ProcessContiguousAsync(CancellationToken cancellationToken)
    {
        EnsureActive();
        while (true)
        {
            long nextSequence = checked(snapshot.Payload.HighestContiguousSequence + 1);
            O2BPendingObservation? pending = snapshot.Payload.Pending
                .SingleOrDefault(item => item.Message.Sequence == nextSequence);
            if (pending is null)
            {
                return;
            }

            int outcomeCount = inner!.Outcomes.Count;
            int reportCount = inner.PublishedReports.Count;
            ObservationItemResult result = await inner
                .IngestAsync(pending.Message, pending.ReceivedAtUtc, cancellationToken)
                .ConfigureAwait(false);
            if (result.Disposition is not ObservationIngestionDisposition.Accepted ||
                inner.Outcomes.Count != outcomeCount + 1)
            {
                faulted = true;
                throw new InvalidOperationException("o2b.pipeline.inner_refused");
            }
            O2AProcessingOutcome outcome = inner.Outcomes[outcomeCount];
            ObserverAnalysisReport? report = inner.PublishedReports.Count == reportCount + 1
                ? inner.PublishedReports[reportCount]
                : null;
            if (outcome.Published != (report is not null))
            {
                faulted = true;
                throw new InvalidOperationException("o2b.pipeline.publication_inconsistent");
            }

            IReadOnlyList<O2BDurablePublicationRecord> publications = snapshot.Payload.Publications;
            long totalPublications = snapshot.Payload.TotalPublications;
            if (report is not null)
            {
                O2BDurablePublicationRecord publication = new(
                    outcome.Sequence,
                    report.AnalysisId,
                    report.InstanceId,
                    report.Code,
                    outcome.EnvelopeDigest,
                    outcome.ContextRevision,
                    report.AnalysedAt,
                    report.IsAuthorising);
                publications = Retain([.. publications, publication], O2BDurablePipelineStore.MaximumPublications);
                totalPublications = checked(totalPublications + 1);
            }

            O2BDurablePipelinePayload completed = snapshot.Payload with
            {
                HighestContiguousSequence = outcome.Sequence,
                ContextRevision = await inner
                    .GetCurrentContextRevisionAsync(cancellationToken)
                    .ConfigureAwait(false),
                Pending = snapshot.Payload.Pending
                    .Where(item => item.Message.Sequence != outcome.Sequence)
                    .ToArray(),
                Outcomes = Retain(
                    [
                        .. snapshot.Payload.Outcomes,
                        new O2BDurableOutcomeRecord(
                            outcome.Sequence,
                            outcome.Code,
                            outcome.Published,
                            outcome.EnvelopeDigest,
                            outcome.ContextRevision),
                    ],
                    O2BDurablePipelineStore.MaximumOutcomes),
                Publications = publications,
                TotalPublications = totalPublications,
                Counters = O2BDurablePipelineStore.Increment(
                    snapshot.Payload.Counters,
                    report is null ? "o2b.pipeline.completed_no_publication" : "o2b.pipeline.published"),
            };
            O2BCommitFault completionFault = NextCompletionFault;
            NextCompletionFault = O2BCommitFault.None;
            try
            {
                snapshot = await store
                    .CommitAsync(snapshot, completed, completionFault, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (O2BInjectedCrashException)
            {
                faulted = true;
                throw;
            }
        }
    }

    /// <summary>Retains all pending identities and the newest completed identities within the fixed bound.</summary>
    private static O2BDigestRecord[] RetainDigests(
        IReadOnlyList<O2BDigestRecord> values,
        IEnumerable<Guid> pendingIds)
    {
        HashSet<Guid> required = pendingIds.ToHashSet();
        O2BDigestRecord[] requiredValues = values.Where(item => required.Contains(item.Id)).ToArray();
        O2BDigestRecord[] completed = values
            .Where(item => !required.Contains(item.Id))
            .OrderByDescending(item => item.Sequence)
            .Take(O2BDurablePipelineStore.MaximumDigests - requiredValues.Length)
            .ToArray();
        return requiredValues
            .Concat(completed)
            .OrderBy(item => item.Sequence)
            .ToArray();
    }

    /// <summary>Retains the newest bounded detail records while preserving their original order.</summary>
    private static T[] Retain<T>(IReadOnlyList<T> values, int maximum) =>
        values.Skip(Math.Max(0, values.Count - maximum)).ToArray();

    /// <summary>Rejects access after quarantine, synthetic crash or disposal.</summary>
    private void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (snapshot.Quarantined)
        {
            throw new O2BStoreRefusalException(snapshot.QuarantineCode ?? "o2b.store.quarantined");
        }
        if (faulted)
        {
            throw new O2BStoreRefusalException("o2b.pipeline.restart_required");
        }
    }

    /// <summary>Creates one stable accepted disposition.</summary>
    private static ObservationItemResult Accepted(Guid messageId) =>
        new(messageId, ObservationIngestionDisposition.Accepted);

    /// <summary>Creates one stable exact-duplicate disposition.</summary>
    private static ObservationItemResult Duplicate(Guid messageId) =>
        new(messageId, ObservationIngestionDisposition.Duplicate);

    /// <summary>Creates one stable terminal refusal.</summary>
    private static ObservationItemResult Rejected(Guid messageId, string code) =>
        new(messageId, ObservationIngestionDisposition.Rejected, code);

    /// <summary>Creates one stable bounded backpressure disposition.</summary>
    private static ObservationItemResult Retryable(Guid messageId, string code) =>
        new(messageId, ObservationIngestionDisposition.Retryable, code);

    /// <summary>Disposes the in-memory O2-A session and deletes only its exact session directory.</summary>
    public ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return ValueTask.CompletedTask;
        }
        disposed = true;
        inner?.Dispose();
        admissionGate.Dispose();
        if (sessionRoot is not null)
        {
            DeleteSession(sessionRoot);
        }
        return ValueTask.CompletedTask;
    }

    /// <summary>Deletes only an exact O2-B session child after the owning in-memory pipeline has stopped.</summary>
    private static void DeleteSession(string path)
    {
        DirectoryInfo session = new(path);
        if (session.Exists &&
            session.Parent?.Name == "sessions" &&
            session.Name.StartsWith("session-", StringComparison.Ordinal))
        {
            session.Delete(recursive: true);
            if (!session.Parent.EnumerateFileSystemInfos().Any())
            {
                session.Parent.Delete();
            }
        }
    }
}

/// <summary>Exposes one bounded cross-process restart proof through the exact existing test-only host.</summary>
public static class O2BSandboxProcess
{
    private const string ActivationMarker = "o2b-durable-pipeline-sandbox";

    /// <summary>
    /// Opens the same ledger twice, replays one fixed message and emits aggregate non-authorising evidence only.
    /// </summary>
    /// <param name="args">Exact activation, operation and root arguments.</param>
    /// <returns>Zero for durable exactly-once recovery, two for invalid activation or three for failure.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (!TryReadOptions(args, out string? root))
        {
            Console.Error.WriteLine("o2b_sandbox.failed:activation_invalid");
            return 2;
        }
        Guid agentId = Guid.Parse("b2b10000-0000-4000-8000-000000000001");
        Guid instanceId = Guid.Parse("b2b20000-0000-4000-8000-000000000001");
        ObservationSyncMessage message = O2ASyntheticObservation.Create(
            agentId,
            instanceId,
            1,
            O1SyntheticTrustFixture.NowUtc,
            messageId: Guid.Parse("b2b30000-0000-4000-8000-000000000001"),
            observationId: Guid.Parse("b2b40000-0000-4000-8000-000000000001"));
        O2AFixedTimeProvider clock = new(O1SyntheticTrustFixture.NowUtc);
        try
        {
            await using (O2BDurableObservationPipeline first =
                await O2BDurableObservationPipeline.CreateAsync(root!, agentId, instanceId, clock))
            {
                await first.IngestAsync(message, clock.GetUtcNow(), CancellationToken.None);
            }
            await using O2BDurableObservationPipeline reopened =
                await O2BDurableObservationPipeline.CreateAsync(root!, agentId, instanceId, clock);
            ObservationItemResult replay = await reopened
                .IngestAsync(message, clock.GetUtcNow(), CancellationToken.None);
            O2BDurablePublicationRecord? publication = reopened.Publications.SingleOrDefault();
            if (replay.Disposition != ObservationIngestionDisposition.Duplicate ||
                reopened.TotalPublications != 1 ||
                publication is null ||
                publication.Authorising)
            {
                Console.Error.WriteLine("o2b_sandbox.failed:reference_incomplete");
                return 3;
            }
            Console.WriteLine(
                JsonSerializer.Serialize(
                    new
                    {
                        highest = await reopened.GetHighestContiguousSequenceAsync(
                            agentId,
                            CancellationToken.None),
                        totalPublications = reopened.TotalPublications,
                        activationState = ObserverActivationState.None.ToString(),
                        authorising = publication.Authorising,
                    }));
            return 0;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"o2b_sandbox.failed:{exception.GetType().Name}");
            return 3;
        }
    }

    /// <summary>Accepts only the exact marker, reference operation and direct O2-B temporary root.</summary>
    private static bool TryReadOptions(string[] args, out string? root)
    {
        root = null;
        if (args.Length != 6 ||
            args[0] != "--activation" ||
            args[1] != ActivationMarker ||
            args[2] != "--operation" ||
            args[3] != "reference" ||
            args[4] != "--root")
        {
            return false;
        }
        try
        {
            string candidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(args[5]));
            string temporary = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
            if (!string.Equals(Path.GetDirectoryName(candidate), temporary, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(candidate).StartsWith("DBNotifier-O2B-", StringComparison.Ordinal))
            {
                return false;
            }
            root = candidate;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            NotSupportedException or
            PathTooLongException)
        {
            return false;
        }
    }
}
