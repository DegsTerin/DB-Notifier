// Module purpose: Implements the authenticated, crash-consistent and temporary O5-R2 sandbox store without operational persistence.
using System.Security.Cryptography;
using System.Text.Json;

namespace DBNotifier.IntegrationTests;

/// <summary>Names deterministic synthetic crash boundaries in the O5-R2 atomic commit sequence.</summary>
internal enum O5R2CommitFault
{
    /// <summary>No fault is injected.</summary>
    None,

    /// <summary>The process fails after the complete temporary file is durable but before replacement.</summary>
    BeforeReplace,

    /// <summary>The process fails after state replacement but before witness advancement.</summary>
    AfterReplaceBeforeWitness,
}

/// <summary>Represents an authorised synthetic crash without exposing filesystem content.</summary>
internal sealed class O5R2InjectedCrashException(O5R2CommitFault fault)
    : Exception($"o5r2_sandbox.injected_crash:{fault}");

/// <summary>Represents the complete non-activating O5-R2 checkpoint persisted by the synthetic store.</summary>
internal sealed record O5R2Checkpoint(
    string ActivationState,
    long Generation,
    long Fence,
    bool KillSwitchEngaged,
    bool Quarantined,
    string Code,
    string? ActiveContextRevision,
    IReadOnlyList<Guid> ConsumedApprovalIds)
{
    /// <summary>Creates the only safe initial checkpoint.</summary>
    /// <returns>An inactive empty checkpoint.</returns>
    internal static O5R2Checkpoint Initial() =>
        new("None", 0, 0, false, false, "o5r2.control.none", null, []);
}

/// <summary>
/// Owns one authenticated temporary checkpoint and a separate monotonic witness, accepting recovery only as the old
/// or new complete state and quarantining rollback, gaps, split views and corruption.
/// </summary>
internal sealed class O5R2ControlPlaneStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly string rootPath;
    private readonly string statePath;
    private readonly string witnessPath;
    private readonly string quarantinePath;
    private readonly string lockPath;
    private readonly byte[] authenticationKey;

    /// <summary>Initialises one store under an exact caller-owned temporary root.</summary>
    /// <param name="rootPath">Exact temporary sandbox root.</param>
    /// <param name="authenticationKey">Synthetic state-authentication key distinct from approval keys.</param>
    internal O5R2ControlPlaneStore(string rootPath, ReadOnlySpan<byte> authenticationKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        if (authenticationKey.Length < 32)
        {
            throw new ArgumentOutOfRangeException(nameof(authenticationKey));
        }

        this.rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(this.rootPath);
        statePath = Path.Combine(this.rootPath, "observer-control.state");
        witnessPath = Path.Combine(this.rootPath, "observer-control.witness");
        quarantinePath = Path.Combine(this.rootPath, "observer-control.quarantine");
        lockPath = Path.Combine(this.rootPath, "observer-control.lock");
        this.authenticationKey = authenticationKey.ToArray();
    }

    /// <summary>Gets the authenticated state path for authorised fault fixtures.</summary>
    internal string StatePath => statePath;

    /// <summary>Gets the authenticated witness path for authorised continuity fixtures.</summary>
    internal string WitnessPath => witnessPath;

    /// <summary>Loads a complete checkpoint and advances a lagging witness only after authenticating the new state.</summary>
    /// <param name="cancellationToken">Cancellation observed before filesystem work.</param>
    /// <returns>The complete state or a durable quarantined state.</returns>
    internal async Task<O5R2Checkpoint> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DeleteTemporaryFiles();
        O5R2Quarantine? quarantine = await ReadEnvelopeAsync<O5R2Quarantine>(
            quarantinePath,
            cancellationToken);
        if (File.Exists(quarantinePath) && quarantine is null)
        {
            return await QuarantineAsync(O5R2Checkpoint.Initial(), "o5r2.store.corrupt", cancellationToken);
        }

        if (!File.Exists(statePath))
        {
            return File.Exists(witnessPath) || quarantine is not null
                ? await QuarantineAsync(
                    O5R2Checkpoint.Initial(),
                    quarantine?.Code ?? "o5r2.store.continuity_unproved",
                    cancellationToken)
                : O5R2Checkpoint.Initial();
        }

        O5R2Checkpoint? state = await ReadEnvelopeAsync<O5R2Checkpoint>(statePath, cancellationToken);
        if (!IsValidState(state))
        {
            return await QuarantineAsync(O5R2Checkpoint.Initial(), "o5r2.store.corrupt", cancellationToken);
        }
        O5R2Checkpoint validState = state!;

        if (quarantine is not null)
        {
            return validState with { Quarantined = true, Code = quarantine.Code, ActiveContextRevision = null };
        }

        O5R2Witness? witness = await ReadEnvelopeAsync<O5R2Witness>(witnessPath, cancellationToken);
        if (File.Exists(witnessPath) && witness is null)
        {
            return await QuarantineAsync(state!, "o5r2.store.corrupt", cancellationToken);
        }

        string digest = Digest(validState);
        if (witness is not null)
        {
            if (validState.Generation < witness.Generation || validState.Fence < witness.Fence)
            {
                return await QuarantineAsync(validState, "o5r2.store.rollback", cancellationToken);
            }

            if (validState.Generation == witness.Generation &&
                !string.Equals(digest, witness.StateDigest, StringComparison.Ordinal))
            {
                return await QuarantineAsync(validState, "o5r2.store.split_view", cancellationToken);
            }

            if (validState.Generation > witness.Generation + 1)
            {
                return await QuarantineAsync(validState, "o5r2.store.gap", cancellationToken);
            }
        }

        if (witness is null || validState.Generation > witness.Generation)
        {
            await WriteAtomicEnvelopeAsync(
                witnessPath,
                new O5R2Witness(validState.Generation, validState.Fence, digest),
                cancellationToken);
        }

        return validState;
    }

    /// <summary>Commits exactly one monotonic complete replacement and its continuity witness.</summary>
    /// <param name="expectedGeneration">Generation observed before candidate construction.</param>
    /// <param name="candidate">Complete next checkpoint.</param>
    /// <param name="fault">Optional synthetic crash boundary.</param>
    /// <param name="cancellationToken">Cancellation observed before publication.</param>
    /// <returns>The committed complete checkpoint.</returns>
    internal async Task<O5R2Checkpoint> CommitAsync(
        long expectedGeneration,
        O5R2Checkpoint candidate,
        O5R2CommitFault fault = O5R2CommitFault.None,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ValidateCandidate(candidate, expectedGeneration);
        cancellationToken.ThrowIfCancellationRequested();

        await using FileStream owner = await AcquireLockAsync(cancellationToken);
        O5R2Checkpoint current = await LoadAsync(cancellationToken);
        if (current.Quarantined || current.Generation != expectedGeneration)
        {
            throw new InvalidOperationException(
                current.Quarantined ? current.Code : "o5r2.store.conflict");
        }

        string temporaryPath = Path.Combine(rootPath, $"observer-control.{Guid.NewGuid():N}.tmp");
        try
        {
            await WriteDurableAsync(temporaryPath, EncodeEnvelope(candidate), cancellationToken);
            if (fault == O5R2CommitFault.BeforeReplace)
            {
                throw new O5R2InjectedCrashException(fault);
            }

            File.Move(temporaryPath, statePath, overwrite: true);
            if (fault == O5R2CommitFault.AfterReplaceBeforeWitness)
            {
                throw new O5R2InjectedCrashException(fault);
            }

            await WriteAtomicEnvelopeAsync(
                witnessPath,
                new O5R2Witness(candidate.Generation, candidate.Fence, Digest(candidate)),
                cancellationToken);
            if (File.Exists(quarantinePath))
            {
                File.Delete(quarantinePath);
            }

            return candidate;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>Writes an authenticated alternative state for authorised rollback, gap and split-view fixtures.</summary>
    /// <param name="state">Complete synthetic state to place without updating the witness.</param>
    /// <param name="cancellationToken">Cancellation observed before replacement.</param>
    internal Task ReplaceStateForTestAsync(
        O5R2Checkpoint state,
        CancellationToken cancellationToken = default) =>
        WriteAtomicEnvelopeAsync(statePath, state, cancellationToken);

    /// <summary>Persists a fail-closed quarantine marker authenticated by the state key.</summary>
    /// <param name="state">Last complete safe state.</param>
    /// <param name="code">Stable sanitised reason.</param>
    /// <param name="cancellationToken">Cancellation observed before marker publication.</param>
    /// <returns>The detached quarantined state.</returns>
    private async Task<O5R2Checkpoint> QuarantineAsync(
        O5R2Checkpoint state,
        string code,
        CancellationToken cancellationToken)
    {
        await WriteAtomicEnvelopeAsync(quarantinePath, new O5R2Quarantine(code), cancellationToken);
        return state with
        {
            ActivationState = "None",
            Quarantined = true,
            Code = code,
            ActiveContextRevision = null,
        };
    }

    /// <summary>Validates monotonicity and bounded one-use state before filesystem allocation.</summary>
    /// <param name="candidate">Candidate checkpoint.</param>
    /// <param name="expectedGeneration">Generation read by the caller.</param>
    private static void ValidateCandidate(O5R2Checkpoint candidate, long expectedGeneration)
    {
        if (!IsValidState(candidate) ||
            candidate.Generation != expectedGeneration + 1 ||
            candidate.Fence < candidate.Generation ||
            candidate.ConsumedApprovalIds.Count > 64 ||
            candidate.ConsumedApprovalIds.Distinct().Count() != candidate.ConsumedApprovalIds.Count)
        {
            throw new ArgumentException("The O5-R2 checkpoint is not a complete monotonic candidate.", nameof(candidate));
        }
    }

    /// <summary>Checks the only allowed activation value and finite bounded checkpoint shape.</summary>
    /// <param name="state">Decoded checkpoint.</param>
    /// <returns><see langword="true"/> only for a complete inactive checkpoint.</returns>
    private static bool IsValidState(O5R2Checkpoint? state) =>
        state is not null &&
        string.Equals(state.ActivationState, "None", StringComparison.Ordinal) &&
        state.Generation >= 0 &&
        state.Fence >= 0 &&
        state.ConsumedApprovalIds is not null &&
        state.ConsumedApprovalIds.Count <= 64;

    /// <summary>Acquires the exact store lock with a bounded cancellation-aware retry.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exclusive lock stream.</returns>
    private async Task<FileStream> AcquireLockAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(3);
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
                    FileOptions.Asynchronous | FileOptions.WriteThrough);
            }
            catch (IOException) when (DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(10, cancellationToken);
            }
        }
    }

    /// <summary>Writes an authenticated JSON envelope by durable temporary replacement.</summary>
    /// <typeparam name="T">Serialisable bounded payload type.</typeparam>
    /// <param name="path">Exact owned destination.</param>
    /// <param name="value">Complete value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task WriteAtomicEnvelopeAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        string temporaryPath = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            await WriteDurableAsync(temporaryPath, EncodeEnvelope(value), cancellationToken);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>Reads and authenticates one bounded local envelope before deserialisation.</summary>
    /// <typeparam name="T">Expected payload type.</typeparam>
    /// <param name="path">Exact owned path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Decoded value, or <see langword="null"/> when authentication or shape fails.</returns>
    private async Task<T?> ReadEnvelopeAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            FileInfo file = new(path);
            if (file.Length <= 0 || file.Length > 65_536)
            {
                return default;
            }

            byte[] encoded = await File.ReadAllBytesAsync(path, cancellationToken);
            PersistedEnvelope? envelope = JsonSerializer.Deserialize<PersistedEnvelope>(encoded, JsonOptions);
            if (envelope is null ||
                string.IsNullOrWhiteSpace(envelope.Payload) ||
                string.IsNullOrWhiteSpace(envelope.AuthenticationTag))
            {
                return default;
            }

            byte[] payload = Convert.FromBase64String(envelope.Payload);
            byte[] suppliedTag = Convert.FromHexString(envelope.AuthenticationTag);
            byte[] expectedTag = HMACSHA256.HashData(authenticationKey, payload);
            if (!CryptographicOperations.FixedTimeEquals(suppliedTag, expectedTag))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(payload, JsonOptions);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or FormatException)
        {
            return default;
        }
    }

    /// <summary>Encodes one canonical payload and authenticates it with the synthetic state key.</summary>
    /// <typeparam name="T">Serialisable bounded payload type.</typeparam>
    /// <param name="value">Complete payload.</param>
    /// <returns>Authenticated envelope bytes.</returns>
    private byte[] EncodeEnvelope<T>(T value)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        return JsonSerializer.SerializeToUtf8Bytes(
            new PersistedEnvelope(
                Convert.ToBase64String(payload),
                Convert.ToHexString(HMACSHA256.HashData(authenticationKey, payload))),
            JsonOptions);
    }

    /// <summary>Computes the content identity used by the separate monotonic witness.</summary>
    /// <param name="state">Complete checkpoint.</param>
    /// <returns>Upper-case SHA-256 digest.</returns>
    private static string Digest(O5R2Checkpoint state) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(state, JsonOptions)));

    /// <summary>Writes and flushes one complete temporary file.</summary>
    /// <param name="path">Exact temporary path.</param>
    /// <param name="bytes">Bounded encoded content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private static async Task WriteDurableAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        stream.Flush(flushToDisk: true);
    }

    /// <summary>Removes only abandoned temporary files owned by the exact sandbox root.</summary>
    private void DeleteTemporaryFiles()
    {
        foreach (string path in Directory.EnumerateFiles(rootPath, "*.tmp", SearchOption.TopDirectoryOnly))
        {
            File.Delete(path);
        }
    }

    private sealed record PersistedEnvelope(string Payload, string AuthenticationTag);

    private sealed record O5R2Witness(long Generation, long Fence, string StateDigest);

    private sealed record O5R2Quarantine(string Code);
}
