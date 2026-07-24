// Module purpose: Persists authenticated O2-B sandbox continuity below an exact caller-owned temporary root.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DBNotifier.Application.Synchronization;

namespace DBNotifier.IntegrationTests;

/// <summary>Identifies the exact crash boundary injected into one atomic O2-B store commit.</summary>
internal enum O2BCommitFault
{
    None,
    BeforeReplace,
    AfterReplaceBeforeWitness,
}

/// <summary>Represents an intentional synthetic process interruption at a durable commit boundary.</summary>
internal sealed class O2BInjectedCrashException(O2BCommitFault fault)
    : Exception($"o2b.store.injected_crash.{fault}")
{
    /// <summary>Gets the exact fault boundary that interrupted the synthetic commit.</summary>
    internal O2BCommitFault Fault { get; } = fault;
}

/// <summary>Represents one stable fail-closed O2-B store refusal.</summary>
internal sealed class O2BStoreRefusalException(string code) : Exception(code)
{
    /// <summary>Gets the sanitised refusal code.</summary>
    internal string Code { get; } = code;
}

/// <summary>Retains one bounded idempotency identity and canonical digest.</summary>
internal sealed record O2BDigestRecord(Guid Id, long Sequence, string Digest);

/// <summary>Retains one accepted but not yet contiguous synthetic observation.</summary>
internal sealed record O2BPendingObservation(ObservationSyncMessage Message, DateTimeOffset ReceivedAtUtc);

/// <summary>Retains one bounded downstream disposition without operational payload.</summary>
internal sealed record O2BDurableOutcomeRecord(
    long Sequence,
    string Code,
    bool Published,
    string EnvelopeDigest,
    string ContextRevision);

/// <summary>Retains the durable, non-authorising publication boundary used for exactly-once replay proof.</summary>
internal sealed record O2BDurablePublicationRecord(
    long Sequence,
    Guid AnalysisId,
    Guid InstanceId,
    string Code,
    string EnvelopeDigest,
    string ContextRevision,
    DateTimeOffset CompletedAtUtc,
    bool Authorising);

/// <summary>Retains one sanitised observability counter.</summary>
internal sealed record O2BCounterRecord(string Code, long Count);

/// <summary>Contains the complete bounded O2-B continuity state committed as one authenticated unit.</summary>
internal sealed record O2BDurablePipelinePayload(
    Guid AgentId,
    Guid InstanceId,
    Guid AuthorisationScopeId,
    long WriterFence,
    long HighestContiguousSequence,
    string ContextRevision,
    IReadOnlyList<O2BDigestRecord> MessageDigests,
    IReadOnlyList<O2BDigestRecord> ObservationDigests,
    IReadOnlyList<O2BPendingObservation> Pending,
    IReadOnlyList<O2BDurableOutcomeRecord> Outcomes,
    IReadOnlyList<O2BDurablePublicationRecord> Publications,
    long TotalPublications,
    IReadOnlyList<O2BCounterRecord> Counters);

/// <summary>Describes one authenticated snapshot and its monotonic durable head.</summary>
internal sealed record O2BStoreSnapshot(
    long Generation,
    string StateDigest,
    O2BDurablePipelinePayload Payload,
    bool Quarantined,
    string? QuarantineCode);

/// <summary>
/// Implements an atomic authenticated sandbox ledger with a separate monotonic witness, rollback quarantine and
/// session fencing. The store accepts only exact O2-B temporary roots and never persists private key material.
/// </summary>
internal sealed class O2BDurablePipelineStore
{
    internal const int MaximumPending = 8;
    internal const int MaximumDigests = 64;
    internal const int MaximumOutcomes = 32;
    internal const int MaximumPublications = 32;
    internal const int MaximumCounters = 32;
    private const int MaximumStateBytes = 1024 * 1024;
    private const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string markerPath;
    private readonly string witnessPath;
    private readonly string statePath;
    private readonly string quarantinePath;
    private readonly string lockPath;

    /// <summary>Initialises an exact temporary O2-B store and rejects roots outside the operating-system temp directory.</summary>
    /// <param name="rootPath">Caller-owned path whose leaf starts with <c>DBNotifier-O2B-</c>.</param>
    internal O2BDurablePipelineStore(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        RootPath = ValidateRoot(rootPath);
        markerPath = Path.Combine(RootPath, "pipeline.marker");
        witnessPath = Path.Combine(RootPath, "pipeline.witness.json");
        statePath = Path.Combine(RootPath, "pipeline.state.json");
        quarantinePath = Path.Combine(RootPath, "pipeline.quarantine");
        lockPath = Path.Combine(RootPath, "pipeline.lock");
    }

    /// <summary>Gets the canonical caller-owned sandbox root.</summary>
    internal string RootPath { get; }

    /// <summary>Gets the state path for authorised synthetic corruption and rollback tests.</summary>
    internal string StatePath => statePath;

    /// <summary>Gets the witness path for authorised missing-state and rollback tests.</summary>
    internal string WitnessPath => witnessPath;

    /// <summary>
    /// Opens or creates the ledger and durably rotates its writer fence before returning the session snapshot.
    /// </summary>
    /// <param name="agentId">Exact synthetic stream identity.</param>
    /// <param name="instanceId">Exact synthetic instance identity.</param>
    /// <param name="scopeId">Exact synthetic authorisation scope.</param>
    /// <param name="cancellationToken">Cancellation observed before each filesystem boundary.</param>
    /// <returns>The current authenticated snapshot with a newly committed writer fence.</returns>
    internal async Task<O2BStoreSnapshot> OpenSessionAsync(
        Guid agentId,
        Guid instanceId,
        Guid scopeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(instanceId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(scopeId, Guid.Empty);
        Directory.CreateDirectory(RootPath);
        await using FileStream processLock = await AcquireLockAsync(cancellationToken).ConfigureAwait(false);
        O2BStoreSnapshot? current = await LoadCoreAsync(cancellationToken).ConfigureAwait(false);
        if (current?.Quarantined == true)
        {
            return current;
        }

        if (current is null)
        {
            if (Directory.EnumerateFileSystemEntries(RootPath)
                .Any(path => !string.Equals(path, lockPath, StringComparison.OrdinalIgnoreCase)))
            {
                return await QuarantineAsync("o2b.store.continuity_missing", null, cancellationToken)
                    .ConfigureAwait(false);
            }

            O2BDurablePipelinePayload initial = new(
                agentId,
                instanceId,
                scopeId,
                1,
                0,
                "o2a-context-1",
                [],
                [],
                [],
                [],
                [],
                0,
                [new O2BCounterRecord("o2b.store.initialised", 1)]);
            File.WriteAllText(markerPath, "o2b.durable-pipeline.v1", Encoding.UTF8);
            return await WriteCommitAsync(
                    1,
                    string.Empty,
                    initial,
                    O2BCommitFault.None,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        ValidateIdentity(current.Payload, agentId, instanceId, scopeId);
        O2BDurablePipelinePayload rotated = current.Payload with
        {
            WriterFence = checked(current.Payload.WriterFence + 1),
            Counters = Increment(current.Payload.Counters, "o2b.store.session_opened"),
        };
        return await WriteCommitAsync(
                checked(current.Generation + 1),
                current.StateDigest,
                rotated,
                O2BCommitFault.None,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Loads the current authenticated state without rotating a writer fence.</summary>
    /// <param name="cancellationToken">Cancellation observed before file access.</param>
    /// <returns>The current snapshot, or null when the root has never been initialised.</returns>
    internal async Task<O2BStoreSnapshot?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(RootPath))
        {
            return null;
        }
        await using FileStream processLock = await AcquireLockAsync(cancellationToken).ConfigureAwait(false);
        return await LoadCoreAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Commits one complete replacement after checking the caller's generation, state digest and writer fence.
    /// </summary>
    /// <param name="expected">Snapshot from which the caller derived the replacement.</param>
    /// <param name="replacement">Complete bounded replacement payload.</param>
    /// <param name="fault">Optional synthetic crash boundary.</param>
    /// <param name="cancellationToken">Cancellation observed before durable boundaries.</param>
    /// <returns>The new committed snapshot.</returns>
    internal async Task<O2BStoreSnapshot> CommitAsync(
        O2BStoreSnapshot expected,
        O2BDurablePipelinePayload replacement,
        O2BCommitFault fault = O2BCommitFault.None,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(replacement);
        await using FileStream processLock = await AcquireLockAsync(cancellationToken).ConfigureAwait(false);
        O2BStoreSnapshot current = await LoadCoreAsync(cancellationToken).ConfigureAwait(false) ??
            throw new O2BStoreRefusalException("o2b.store.continuity_missing");
        if (current.Quarantined)
        {
            throw new O2BStoreRefusalException(current.QuarantineCode ?? "o2b.store.quarantined");
        }
        if (current.Payload.WriterFence != expected.Payload.WriterFence)
        {
            throw new O2BStoreRefusalException("o2b.store.stale_fence");
        }
        if (current.Generation != expected.Generation ||
            !CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(current.StateDigest),
                Convert.FromHexString(expected.StateDigest)))
        {
            throw new O2BStoreRefusalException("o2b.store.stale_snapshot");
        }
        if (replacement.WriterFence != current.Payload.WriterFence)
        {
            throw new O2BStoreRefusalException("o2b.store.fence_mismatch");
        }
        ValidateIdentity(
            replacement,
            current.Payload.AgentId,
            current.Payload.InstanceId,
            current.Payload.AuthorisationScopeId);
        return await WriteCommitAsync(
                checked(current.Generation + 1),
                current.StateDigest,
                replacement,
                fault,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Atomically commits state then witness, allowing exact crash injection between those durable steps.</summary>
    private async Task<O2BStoreSnapshot> WriteCommitAsync(
        long generation,
        string predecessorDigest,
        O2BDurablePipelinePayload payload,
        O2BCommitFault fault,
        CancellationToken cancellationToken)
    {
        ValidatePayload(payload);
        byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        if (payloadBytes.Length > MaximumStateBytes)
        {
            throw new O2BStoreRefusalException("o2b.store.state_too_large");
        }
        string payloadDigest = Digest(payloadBytes);
        O2BStateEnvelope unsigned = new(
            SchemaVersion,
            generation,
            predecessorDigest,
            payloadDigest,
            Convert.ToBase64String(payloadBytes),
            string.Empty);
        string stateDigest = Digest(JsonSerializer.SerializeToUtf8Bytes(unsigned, JsonOptions));
        O2BStateEnvelope envelope = unsigned with { StateDigest = stateDigest };
        if (fault == O2BCommitFault.BeforeReplace)
        {
            throw new O2BInjectedCrashException(fault);
        }
        await AtomicWriteAsync(statePath, JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions), cancellationToken)
            .ConfigureAwait(false);
        if (fault == O2BCommitFault.AfterReplaceBeforeWitness)
        {
            throw new O2BInjectedCrashException(fault);
        }
        O2BWitness witness = new(SchemaVersion, generation, stateDigest);
        await AtomicWriteAsync(
                witnessPath,
                JsonSerializer.SerializeToUtf8Bytes(witness, JsonOptions),
                cancellationToken)
            .ConfigureAwait(false);
        return new O2BStoreSnapshot(generation, stateDigest, payload, false, null);
    }

    /// <summary>Loads and authenticates state and witness, repairing only one provable direct-successor crash.</summary>
    private async Task<O2BStoreSnapshot?> LoadCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(quarantinePath))
        {
            string code = await File.ReadAllTextAsync(quarantinePath, cancellationToken).ConfigureAwait(false);
            return QuarantinedSnapshot(string.IsNullOrWhiteSpace(code) ? "o2b.store.quarantined" : code.Trim());
        }
        bool any = File.Exists(markerPath) || File.Exists(statePath) || File.Exists(witnessPath);
        if (!any)
        {
            return null;
        }
        if (!File.Exists(markerPath) || !File.Exists(statePath) || !File.Exists(witnessPath))
        {
            return await QuarantineAsync("o2b.store.continuity_missing", null, cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            O2BStateEnvelope state = await ReadBoundedAsync<O2BStateEnvelope>(statePath, cancellationToken)
                .ConfigureAwait(false);
            O2BWitness witness = await ReadBoundedAsync<O2BWitness>(witnessPath, cancellationToken)
                .ConfigureAwait(false);
            O2BDurablePipelinePayload payload = ValidateEnvelope(state);
            if (state.Generation == witness.Generation &&
                string.Equals(state.StateDigest, witness.StateDigest, StringComparison.Ordinal))
            {
                return new O2BStoreSnapshot(state.Generation, state.StateDigest, payload, false, null);
            }
            if (state.Generation == checked(witness.Generation + 1) &&
                string.Equals(state.PredecessorDigest, witness.StateDigest, StringComparison.Ordinal))
            {
                await AtomicWriteAsync(
                        witnessPath,
                        JsonSerializer.SerializeToUtf8Bytes(
                            new O2BWitness(SchemaVersion, state.Generation, state.StateDigest),
                            JsonOptions),
                        cancellationToken)
                    .ConfigureAwait(false);
                return new O2BStoreSnapshot(state.Generation, state.StateDigest, payload, false, null);
            }
            string code = state.Generation < witness.Generation
                ? "o2b.store.rollback_detected"
                : "o2b.store.divergence_detected";
            return await QuarantineAsync(code, payload, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is JsonException or
            FormatException or
            InvalidDataException or
            IOException)
        {
            return await QuarantineAsync("o2b.store.corrupt", null, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Validates the authenticated envelope and returns its complete payload.</summary>
    private static O2BDurablePipelinePayload ValidateEnvelope(O2BStateEnvelope state)
    {
        if (state.SchemaVersion != SchemaVersion ||
            state.Generation <= 0 ||
            string.IsNullOrWhiteSpace(state.PayloadDigest) ||
            string.IsNullOrWhiteSpace(state.PayloadBase64) ||
            string.IsNullOrWhiteSpace(state.StateDigest))
        {
            throw new InvalidDataException("o2b.store.envelope_invalid");
        }
        O2BStateEnvelope unsigned = state with { StateDigest = string.Empty };
        string expectedStateDigest = Digest(JsonSerializer.SerializeToUtf8Bytes(unsigned, JsonOptions));
        if (!FixedEquals(expectedStateDigest, state.StateDigest))
        {
            throw new InvalidDataException("o2b.store.state_digest_invalid");
        }
        byte[] payloadBytes = Convert.FromBase64String(state.PayloadBase64);
        if (payloadBytes.Length > MaximumStateBytes || !FixedEquals(Digest(payloadBytes), state.PayloadDigest))
        {
            throw new InvalidDataException("o2b.store.payload_digest_invalid");
        }
        O2BDurablePipelinePayload payload = JsonSerializer.Deserialize<O2BDurablePipelinePayload>(
                payloadBytes,
                JsonOptions)
            ?? throw new InvalidDataException("o2b.store.payload_invalid");
        ValidatePayload(payload);
        return payload;
    }

    /// <summary>Rejects unbounded or structurally inconsistent state before it can be committed or restored.</summary>
    private static void ValidatePayload(O2BDurablePipelinePayload payload)
    {
        if (payload.AgentId == Guid.Empty ||
            payload.InstanceId == Guid.Empty ||
            payload.AuthorisationScopeId == Guid.Empty ||
            payload.WriterFence <= 0 ||
            payload.HighestContiguousSequence < 0 ||
            string.IsNullOrWhiteSpace(payload.ContextRevision) ||
            payload.MessageDigests.Count > MaximumDigests ||
            payload.ObservationDigests.Count > MaximumDigests ||
            payload.Pending.Count > MaximumPending ||
            payload.Outcomes.Count > MaximumOutcomes ||
            payload.Publications.Count > MaximumPublications ||
            payload.Counters.Count > MaximumCounters ||
            payload.TotalPublications < payload.Publications.Count ||
            payload.Publications.Any(publication => publication.Authorising) ||
            payload.Pending.Any(item => item.Message.Sequence <= payload.HighestContiguousSequence) ||
            payload.Pending.Select(item => item.Message.Sequence).Distinct().Count() != payload.Pending.Count)
        {
            throw new InvalidDataException("o2b.store.payload_invalid");
        }
    }

    /// <summary>Ensures a reopened stream cannot be rebound to another synthetic identity or scope.</summary>
    private static void ValidateIdentity(
        O2BDurablePipelinePayload payload,
        Guid agentId,
        Guid instanceId,
        Guid scopeId)
    {
        if (payload.AgentId != agentId ||
            payload.InstanceId != instanceId ||
            payload.AuthorisationScopeId != scopeId)
        {
            throw new O2BStoreRefusalException("o2b.store.identity_mismatch");
        }
    }

    /// <summary>Persists a stable quarantine code and returns a safe empty diagnostic view.</summary>
    private async Task<O2BStoreSnapshot> QuarantineAsync(
        string code,
        O2BDurablePipelinePayload? payload,
        CancellationToken cancellationToken)
    {
        await AtomicWriteAsync(quarantinePath, Encoding.UTF8.GetBytes(code), cancellationToken)
            .ConfigureAwait(false);
        return new O2BStoreSnapshot(
            0,
            string.Empty,
            payload ?? EmptyPayload(),
            true,
            code);
    }

    /// <summary>Returns an empty non-authorising payload for a quarantined diagnostic snapshot.</summary>
    private static O2BDurablePipelinePayload EmptyPayload() =>
        new(Guid.Empty, Guid.Empty, Guid.Empty, 0, 0, string.Empty, [], [], [], [], [], 0, []);

    /// <summary>Returns a quarantined snapshot without exposing stored content.</summary>
    private static O2BStoreSnapshot QuarantinedSnapshot(string code) =>
        new(0, string.Empty, EmptyPayload(), true, code);

    /// <summary>Reads one bounded JSON document and rejects empty, oversized or incomplete content.</summary>
    private static async Task<T> ReadBoundedAsync<T>(string path, CancellationToken cancellationToken)
    {
        FileInfo info = new(path);
        if (!info.Exists || info.Length <= 0 || info.Length > MaximumStateBytes)
        {
            throw new InvalidDataException("o2b.store.document_size_invalid");
        }
        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(bytes, JsonOptions) ??
            throw new InvalidDataException("o2b.store.document_invalid");
    }

    /// <summary>Writes and flushes a same-directory temporary file before atomically replacing the target.</summary>
    private static async Task AtomicWriteAsync(
        string targetPath,
        byte[] content,
        CancellationToken cancellationToken)
    {
        string temporaryPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.WriteThrough | FileOptions.Asynchronous))
            {
                await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>Acquires one bounded exclusive commit lock without retaining ownership for the session lifetime.</summary>
    private async Task<FileStream> AcquireLockAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(RootPath);
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    1,
                    FileOptions.DeleteOnClose);
            }
            catch (IOException) when (DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Increments one stable code while retaining a bounded alphabetically ordered counter set.</summary>
    internal static IReadOnlyList<O2BCounterRecord> Increment(
        IReadOnlyList<O2BCounterRecord> counters,
        string code)
    {
        Dictionary<string, long> values = counters.ToDictionary(item => item.Code, item => item.Count);
        values[code] = checked(values.GetValueOrDefault(code) + 1);
        return values
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Take(MaximumCounters)
            .Select(item => new O2BCounterRecord(item.Key, item.Value))
            .ToArray();
    }

    /// <summary>Computes one uppercase SHA-256 digest for authenticated local continuity.</summary>
    private static string Digest(ReadOnlySpan<byte> content) =>
        Convert.ToHexString(SHA256.HashData(content));

    /// <summary>Compares two encoded digests in constant time.</summary>
    private static bool FixedEquals(string left, string right)
    {
        byte[] leftBytes = Convert.FromHexString(left);
        byte[] rightBytes = Convert.FromHexString(right);
        return leftBytes.Length == rightBytes.Length &&
            CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    /// <summary>Accepts only a direct child of the operating-system temp directory with the exact O2-B prefix.</summary>
    private static string ValidateRoot(string rootPath)
    {
        string canonical = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
        string temporary = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        if (!string.Equals(Path.GetDirectoryName(canonical), temporary, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(canonical).StartsWith("DBNotifier-O2B-", StringComparison.Ordinal))
        {
            throw new ArgumentException("o2b.store.root_invalid", nameof(rootPath));
        }
        return canonical;
    }

    /// <summary>Contains one complete authenticated state document.</summary>
    private sealed record O2BStateEnvelope(
        int SchemaVersion,
        long Generation,
        string PredecessorDigest,
        string PayloadDigest,
        string PayloadBase64,
        string StateDigest);

    /// <summary>Contains the last durable monotonic state head independently from the state document.</summary>
    private sealed record O2BWitness(int SchemaVersion, long Generation, string StateDigest);
}
