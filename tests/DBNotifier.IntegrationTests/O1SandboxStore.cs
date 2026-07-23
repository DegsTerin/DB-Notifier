// Module purpose: Implements the O1 sandbox's atomic local checkpoint, continuity witness, quarantine and multiprocess fencing without operational persistence.
using System.Text;
using System.Text.Json;

namespace DBNotifier.IntegrationTests;

/// <summary>Represents a deterministic synthetic crash at one authorised local commit boundary.</summary>
internal sealed class O1InjectedCrashException : Exception
{
    /// <summary>Initialises a synthetic crash without untrusted detail.</summary>
    /// <param name="stage">Stable injected stage.</param>
    internal O1InjectedCrashException(O1CommitFault stage)
        : base($"o1_sandbox.injected_crash:{stage}")
    {
    }
}

/// <summary>Owns one temporary, digest-protected and atomic O1 checkpoint store under an exact sandbox root.</summary>
internal sealed class O1SandboxStore
{
    private const int MaximumConsumedNonces = 64;
    private const int MaximumAuditIntents = 64;
    private const int MaximumCorpusHeads = 32;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly string rootPath;
    private readonly string statePath;
    private readonly string witnessPath;
    private readonly string quarantinePath;
    private readonly string installationPath;
    private readonly string lockPath;

    /// <summary>Initialises one store under a canonical temporary root owned by the caller.</summary>
    /// <param name="rootPath">Exact sandbox root.</param>
    internal O1SandboxStore(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        this.rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(this.rootPath);
        statePath = Path.Combine(this.rootPath, "checkpoint.state.json");
        witnessPath = Path.Combine(this.rootPath, "checkpoint.witness.json");
        quarantinePath = Path.Combine(this.rootPath, "checkpoint.quarantine.json");
        installationPath = Path.Combine(this.rootPath, "installation.marker.json");
        lockPath = Path.Combine(this.rootPath, "checkpoint.lock");
    }

    /// <summary>Gets the exact state path for synthetic rollback and crash fixtures.</summary>
    internal string StatePath => statePath;

    /// <summary>Loads and authenticates the complete state, detecting rollback, split view and missing continuity.</summary>
    /// <param name="cancellationToken">Cancellation observed before filesystem work.</param>
    /// <returns>The complete current state or a quarantined detached representation.</returns>
    internal async Task<O1CheckpointState> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DeleteOwnedTemporaryFiles();

        bool quarantineExists = File.Exists(quarantinePath);
        bool witnessExists = File.Exists(witnessPath);
        bool installationExists = File.Exists(installationPath);
        PersistedQuarantine? existingQuarantine = await TryReadJsonAsync<PersistedQuarantine>(
            quarantinePath,
            cancellationToken);
        PersistedWitness? witness = await TryReadJsonAsync<PersistedWitness>(
            witnessPath,
            cancellationToken);
        PersistedInstallation? installation = await TryReadJsonAsync<PersistedInstallation>(
            installationPath,
            cancellationToken);
        if ((quarantineExists && existingQuarantine is null) ||
            (witnessExists && witness is null) ||
            (installationExists && installation is null))
        {
            return await QuarantineAsync(
                new O1CheckpointState(),
                "trust.continuity_unproved",
                cancellationToken);
        }
        if (!File.Exists(statePath))
        {
            if (witness is not null || installation is not null || existingQuarantine is not null)
            {
                return await QuarantineAsync(
                    new O1CheckpointState(),
                    existingQuarantine?.Code ?? "trust.continuity_unproved",
                    cancellationToken);
            }
            return new O1CheckpointState();
        }

        O1CheckpointState? state = await TryReadEnvelopeAsync(statePath, cancellationToken);
        if (state is null)
        {
            return await QuarantineAsync(
                new O1CheckpointState(),
                "trust.checkpoint_corrupt",
                cancellationToken);
        }

        if (existingQuarantine is not null)
        {
            state.Quarantined = true;
            state.QuarantineCode = existingQuarantine.Code;
            return state;
        }

        string stateDigest = O1CanonicalCryptography.Digest(state);
        if (witness is not null)
        {
            bool lower = state.RecoveryEpochHighWater < witness.RecoveryEpoch ||
                (state.RecoveryEpochHighWater == witness.RecoveryEpoch &&
                 state.Generation < witness.Generation) ||
                state.ContextRevision < witness.ContextRevision;
            bool divergent = state.ContextRevision == witness.ContextRevision &&
                !string.Equals(stateDigest, witness.StateDigest, StringComparison.Ordinal);
            if (lower || divergent)
            {
                return await QuarantineAsync(
                    state,
                    lower ? "trust.rollback" : "trust.split_view",
                    cancellationToken);
            }
        }

        if (witness is null ||
            state.ContextRevision > witness.ContextRevision ||
            !string.Equals(stateDigest, witness.StateDigest, StringComparison.Ordinal))
        {
            await WriteAtomicJsonAsync(
                witnessPath,
                new PersistedWitness(
                    state.RecoveryEpochHighWater,
                    state.Generation,
                    state.ContextRevision,
                    stateDigest),
                cancellationToken);
        }
        if (installation is null)
        {
            await WriteAtomicJsonAsync(
                installationPath,
                new PersistedInstallation(state.DomainId),
                cancellationToken);
        }
        else if (!string.Equals(installation.DomainId, state.DomainId, StringComparison.Ordinal))
        {
            return await QuarantineAsync(
                state,
                "trust.continuity_unproved",
                cancellationToken);
        }

        return state;
    }

    /// <summary>Commits checkpoint, corpus heads and audit intent as one digest-protected atomic state replacement.</summary>
    /// <param name="expectedContextRevision">Revision read before candidate construction.</param>
    /// <param name="candidate">Complete detached candidate state.</param>
    /// <param name="fault">Optional deterministic crash boundary.</param>
    /// <param name="cancellationToken">Cancellation observed before commit publication.</param>
    /// <returns>The committed detached state.</returns>
    internal async Task<O1CheckpointState> CommitAsync(
        long expectedContextRevision,
        O1CheckpointState candidate,
        O1CommitFault fault = O1CommitFault.None,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ValidateCandidate(candidate, expectedContextRevision);
        cancellationToken.ThrowIfCancellationRequested();

        await using FileStream owner = await AcquireLockAsync(cancellationToken);
        O1CheckpointState current = await LoadAsync(cancellationToken);
        if (current.ContextRevision != expectedContextRevision)
        {
            throw new InvalidOperationException("o1_sandbox.checkpoint_conflict");
        }

        string temporaryPath = Path.Combine(rootPath, $"checkpoint.{Guid.NewGuid():N}.tmp");
        PersistedEnvelope envelope = CreateEnvelope(candidate);
        byte[] encoded = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
        try
        {
            await WriteDurableFileAsync(temporaryPath, encoded, cancellationToken);
            if (fault == O1CommitFault.BeforeReplace)
            {
                throw new O1InjectedCrashException(fault);
            }

            File.Move(temporaryPath, statePath, overwrite: true);
            if (fault == O1CommitFault.AfterReplaceBeforeWitness)
            {
                throw new O1InjectedCrashException(fault);
            }

            string stateDigest = O1CanonicalCryptography.Digest(candidate);
            await WriteAtomicJsonAsync(
                witnessPath,
                new PersistedWitness(
                    candidate.RecoveryEpochHighWater,
                    candidate.Generation,
                    candidate.ContextRevision,
                    stateDigest),
                cancellationToken);
            if (!File.Exists(installationPath))
            {
                await WriteAtomicJsonAsync(
                    installationPath,
                    new PersistedInstallation(candidate.DomainId),
                    cancellationToken);
            }
            if (File.Exists(quarantinePath))
            {
                File.Delete(quarantinePath);
            }
            return candidate.Clone();
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>Persists a sanitised quarantine marker and returns a quarantined detached state.</summary>
    /// <param name="state">Last complete state that could be read.</param>
    /// <param name="code">Stable quarantine reason.</param>
    /// <param name="cancellationToken">Cancellation observed before the marker write.</param>
    /// <returns>Quarantined detached state.</returns>
    private async Task<O1CheckpointState> QuarantineAsync(
        O1CheckpointState state,
        string code,
        CancellationToken cancellationToken)
    {
        state.Quarantined = true;
        state.QuarantineCode = code;
        await WriteAtomicJsonAsync(
            quarantinePath,
            new PersistedQuarantine(code),
            cancellationToken);
        return state;
    }

    /// <summary>Acquires the exact local store lock with a bounded retry loop and cooperative cancellation.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An exclusive lock stream owned by the caller.</returns>
    private async Task<FileStream> AcquireLockAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);
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
                await Task.Delay(20, cancellationToken);
            }
        }
    }

    /// <summary>Validates finite candidate invariants before any temporary file is created.</summary>
    /// <param name="candidate">Complete candidate.</param>
    /// <param name="expectedContextRevision">Expected predecessor revision.</param>
    private static void ValidateCandidate(O1CheckpointState candidate, long expectedContextRevision)
    {
        if (candidate.SchemaVersion != 1 ||
            candidate.ContextRevision != checked(expectedContextRevision + 1) ||
            candidate.ConsumedApprovalNonces.Count > MaximumConsumedNonces ||
            candidate.ConsumedApprovalNonces.Count !=
                candidate.ConsumedApprovalNonces.Distinct(StringComparer.Ordinal).Count() ||
            candidate.AuditIntents.Count > MaximumAuditIntents ||
            candidate.CorpusHeads.Count > MaximumCorpusHeads ||
            candidate.AuditIntents.Any(
                intent => string.IsNullOrWhiteSpace(intent.Code) ||
                    intent.Code.Length > 96 ||
                    string.IsNullOrWhiteSpace(intent.CorrelationId) ||
                    intent.CorrelationId.Length > 96))
        {
            throw new InvalidOperationException("o1_sandbox.checkpoint_invalid");
        }
    }

    /// <summary>Creates a digest-protected envelope over the complete serialised state.</summary>
    /// <param name="state">Complete state.</param>
    /// <returns>Digest-protected persistence envelope.</returns>
    private static PersistedEnvelope CreateEnvelope(O1CheckpointState state)
    {
        string payload = JsonSerializer.Serialize(state, JsonOptions);
        return new PersistedEnvelope(
            payload,
            O1CanonicalCryptography.TextDigest(payload));
    }

    /// <summary>Reads and authenticates one complete state envelope without reflecting parse errors.</summary>
    /// <param name="path">Exact state path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>State when valid; otherwise <see langword="null"/>.</returns>
    private static async Task<O1CheckpointState?> TryReadEnvelopeAsync(
        string path,
        CancellationToken cancellationToken)
    {
        PersistedEnvelope? envelope = await TryReadJsonAsync<PersistedEnvelope>(
            path,
            cancellationToken);
        if (envelope is null ||
            !string.Equals(
                O1CanonicalCryptography.TextDigest(envelope.Payload),
                envelope.PayloadDigest,
                StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<O1CheckpointState>(envelope.Payload, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Reads one bounded JSON metadata file and converts malformed content to absence.</summary>
    /// <typeparam name="T">Metadata type.</typeparam>
    /// <param name="path">Exact file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed metadata or <see langword="null"/>.</returns>
    private static async Task<T?> TryReadJsonAsync<T>(
        string path,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!File.Exists(path) || new FileInfo(path).Length > 1024 * 1024)
        {
            return null;
        }
        try
        {
            await using FileStream stream = new(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            return await JsonSerializer.DeserializeAsync<T>(
                stream,
                JsonOptions,
                cancellationToken);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            JsonException)
        {
            return null;
        }
    }

    /// <summary>Writes one small metadata object through a durable same-directory atomic replacement.</summary>
    /// <typeparam name="T">Metadata type.</typeparam>
    /// <param name="path">Exact destination.</param>
    /// <param name="value">Bounded metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private static async Task WriteAtomicJsonAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await WriteDurableFileAsync(
                temporaryPath,
                JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions),
                cancellationToken);
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

    /// <summary>Writes and flushes one exact byte sequence before it can be atomically published.</summary>
    /// <param name="path">Exact temporary path.</param>
    /// <param name="bytes">Bounded bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private static async Task WriteDurableFileAsync(
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

    /// <summary>Removes only incomplete temporary files owned by this exact sandbox store.</summary>
    private void DeleteOwnedTemporaryFiles()
    {
        foreach (string path in Directory.EnumerateFiles(rootPath, "checkpoint.*.tmp"))
        {
            File.Delete(path);
        }
        foreach (string path in Directory.EnumerateFiles(rootPath, "*.json.*.tmp"))
        {
            File.Delete(path);
        }
    }

    private sealed record PersistedEnvelope(string Payload, string PayloadDigest);

    private sealed record PersistedWitness(
        long RecoveryEpoch,
        long Generation,
        long ContextRevision,
        string StateDigest);

    private sealed record PersistedQuarantine(string Code);

    private sealed record PersistedInstallation(string DomainId);
}

/// <summary>Provides the exact opt-in multiprocess fence operation exposed by the existing test-only host.</summary>
public static class O1SandboxProcess
{
    private const string ActivationMarker = "o1-durable-trust-resource-sandbox";

    /// <summary>Validates the exact marker and holds one exclusive coordinator fence until the fixture releases it.</summary>
    /// <param name="args">Exact test-only activation, operation, root, ready, release and timeout arguments.</param>
    /// <returns>Zero on a bounded complete hold, two for invalid activation, or four when another owner holds the fence.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (!TryReadOptions(
                args,
                out string? root,
                out string? ready,
                out string? release,
                out TimeSpan timeout))
        {
            Console.Error.WriteLine("o1_sandbox.failed:activation_invalid");
            return 2;
        }

        Directory.CreateDirectory(root!);
        string fencePath = Path.Combine(root!, "coordinator.fence.lock");
        FileStream? fence = null;
        try
        {
            try
            {
                fence = new FileStream(
                    fencePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    1,
                    FileOptions.Asynchronous | FileOptions.WriteThrough);
            }
            catch (IOException)
            {
                Console.Error.WriteLine("o1_sandbox.failed:fence_unavailable");
                return 4;
            }

            await File.WriteAllTextAsync(
                ready!,
                "ready",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            using CancellationTokenSource budget = new(timeout);
            while (!File.Exists(release))
            {
                await Task.Delay(20, budget.Token);
            }
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("o1_sandbox.failed:deadline");
            return 3;
        }
        finally
        {
            if (fence is not null)
            {
                await fence.DisposeAsync();
            }
        }
    }

    /// <summary>Accepts only the exact O1 marker and canonical temporary paths below one owned root.</summary>
    /// <param name="args">Untrusted arguments.</param>
    /// <param name="root">Validated root.</param>
    /// <param name="ready">Validated ready marker.</param>
    /// <param name="release">Validated release marker.</param>
    /// <param name="timeout">Validated bounded timeout.</param>
    /// <returns><see langword="true"/> only for the exact opt-in contract.</returns>
    private static bool TryReadOptions(
        string[] args,
        out string? root,
        out string? ready,
        out string? release,
        out TimeSpan timeout)
    {
        root = null;
        ready = null;
        release = null;
        timeout = TimeSpan.Zero;
        if (args.Length != 12 ||
            args[0] != "--activation" ||
            args[1] != ActivationMarker ||
            args[2] != "--operation" ||
            args[3] != "hold-fence" ||
            args[4] != "--root" ||
            args[6] != "--ready" ||
            args[8] != "--release" ||
            args[10] != "--timeout-ms" ||
            !int.TryParse(args[11], out int timeoutMilliseconds) ||
            timeoutMilliseconds is < 100 or > 30_000)
        {
            return false;
        }

        try
        {
            string candidateRoot = Path.GetFullPath(args[5]);
            string temporaryRoot = Path.GetFullPath(Path.GetTempPath());
            if (!candidateRoot.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(candidateRoot).StartsWith("DBNotifier-O1-", StringComparison.Ordinal))
            {
                return false;
            }
            string candidateReady = Path.GetFullPath(args[7]);
            string candidateRelease = Path.GetFullPath(args[9]);
            if (!IsDirectChild(candidateRoot, candidateReady) ||
                !IsDirectChild(candidateRoot, candidateRelease))
            {
                return false;
            }
            root = candidateRoot;
            ready = candidateReady;
            release = candidateRelease;
            timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
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

    /// <summary>Confirms that one marker path is an exact direct child of the owned sandbox root.</summary>
    /// <param name="root">Canonical root.</param>
    /// <param name="candidate">Canonical marker path.</param>
    /// <returns><see langword="true"/> only for an exact direct child.</returns>
    private static bool IsDirectChild(string root, string candidate) =>
        string.Equals(
            Path.GetDirectoryName(candidate),
            root,
            StringComparison.OrdinalIgnoreCase);
}
