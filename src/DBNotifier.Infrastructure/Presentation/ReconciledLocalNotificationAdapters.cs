// Module purpose: Implements bounded HTTPS reads and an isolated atomic file ledger for the local notification sandbox.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBNotifier.Application.Presentation;

namespace DBNotifier.Infrastructure.Presentation;

/// <summary>Reads versioned transition pages from an explicitly supplied HTTPS loopback sandbox.</summary>
public sealed class HttpReconciledNotificationTransitionReader : IReconciledNotificationTransitionReader
{
    private const int MaximumResponseBytes = 65_536;
    private const string TransitionPath = "/api/v1/dashboard/reconciled-notification-transitions";
    private const string SchemaHeader = "DBN-Reconciled-Notification-Schema";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 12,
    };
    private readonly HttpClient client;
    private readonly TimeProvider timeProvider;

    /// <summary>Initialises the reader after verifying its immutable HTTPS loopback boundary.</summary>
    /// <param name="client">Dedicated sandbox client with a pinned test certificate policy.</param>
    /// <param name="timeProvider">Trusted clock used to validate freshness claims.</param>
    /// <exception cref="ArgumentException">Thrown when the base address is not an exact HTTPS loopback origin.</exception>
    public HttpReconciledNotificationTransitionReader(HttpClient client, TimeProvider timeProvider)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        Uri? address = client.BaseAddress;
        if (address is null || !string.Equals(address.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) ||
            !IPAddress.TryParse(address.Host, out IPAddress? ipAddress) || !IPAddress.IsLoopback(ipAddress) ||
            !string.IsNullOrEmpty(address.UserInfo) || !string.IsNullOrEmpty(address.Query) ||
            !string.IsNullOrEmpty(address.Fragment))
        {
            throw new ArgumentException("The reconciled notification reader requires an exact HTTPS loopback base address.", nameof(client));
        }
    }

    /// <inheritdoc />
    public ValueTask<ReconciledNotificationReadResult> ReadBaselineAsync(CancellationToken cancellationToken) =>
        ReadAsync($"{TransitionPath}?baseline=true", ReconciledNotificationTransitionContract.EmptyCursor, true, cancellationToken);

    /// <inheritdoc />
    public ValueTask<ReconciledNotificationReadResult> ReadAfterAsync(
        string cursor,
        CancellationToken cancellationToken)
    {
        if (!ReconciledNotificationCursorCodec.TryDecode(cursor, out _))
        {
            return ValueTask.FromResult(new ReconciledNotificationReadResult(
                ReconciledNotificationReadDisposition.Incompatible,
                null,
                "reconciled_notification.cursor_invalid"));
        }
        return ReadAsync($"{TransitionPath}?cursor={Uri.EscapeDataString(cursor)}", cursor, false, cancellationToken);
    }

    private async ValueTask<ReconciledNotificationReadResult> ReadAsync(
        string path,
        string requestedCursor,
        bool baseline,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await client.GetAsync(
                path,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return new(ReconciledNotificationReadDisposition.Denied, null, "reconciled_notification.identity_denied");
            }
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.UpgradeRequired)
            {
                return new(ReconciledNotificationReadDisposition.Incompatible, null, "reconciled_notification.endpoint_incompatible");
            }
            if (!response.IsSuccessStatusCode)
            {
                return new(ReconciledNotificationReadDisposition.Retryable, null, "reconciled_notification.read_unavailable");
            }
            if (!response.Headers.TryGetValues(SchemaHeader, out IEnumerable<string>? schemas) ||
                !schemas.SequenceEqual([ReconciledNotificationTransitionContract.CurrentSchemaVersion], StringComparer.Ordinal) ||
                !string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(response.Content.Headers.ContentType?.CharSet, "utf-8", StringComparison.OrdinalIgnoreCase) ||
                response.Content.Headers.ContentLength > MaximumResponseBytes)
            {
                return new(ReconciledNotificationReadDisposition.Incompatible, null, "reconciled_notification.response_invalid");
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using MemoryStream bounded = new();
            byte[] buffer = new byte[8_192];
            while (true)
            {
                int read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }
                if (bounded.Length + read > MaximumResponseBytes)
                {
                    return new(ReconciledNotificationReadDisposition.Incompatible, null, "reconciled_notification.response_too_large");
                }
                await bounded.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
            bounded.Position = 0;
            ReconciledNotificationTransitionPage? page = await JsonSerializer.DeserializeAsync<ReconciledNotificationTransitionPage>(
                bounded,
                SerializerOptions,
                cancellationToken).ConfigureAwait(false);
            if (!ReconciledNotificationTransitionValidator.TryValidate(
                    page,
                    requestedCursor,
                    baseline,
                    timeProvider.GetUtcNow(),
                    out string errorCode))
            {
                return new(ReconciledNotificationReadDisposition.Incompatible, null, errorCode);
            }
            return new(ReconciledNotificationReadDisposition.Accepted, page, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException)
        {
            return new(ReconciledNotificationReadDisposition.Retryable, null, "reconciled_notification.read_unavailable");
        }
    }
}

/// <summary>Owns one exclusive, bounded and atomically replaced local sandbox ledger.</summary>
public sealed class FileReconciledNotificationLedger : IReconciledNotificationLedger
{
    private const int MaximumFileBytes = 262_144;
    private const int MaximumEntries = 256;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 12,
    };
    private readonly string ledgerPath;
    private readonly string temporaryPath;
    private readonly FileStream ownershipLock;
    private ReconciledNotificationLedgerState state;
    private bool disposed;

    /// <summary>Creates or opens one ledger and acquires an exclusive process fence for its lifetime.</summary>
    /// <param name="directoryPath">Dedicated sandbox directory selected and constrained by the composition root.</param>
    /// <exception cref="InvalidDataException">Thrown when existing state is malformed or incompatible.</exception>
    /// <exception cref="IOException">Thrown when another process already owns the same ledger.</exception>
    public FileReconciledNotificationLedger(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        string fullDirectory = Path.GetFullPath(directoryPath);
        Directory.CreateDirectory(fullDirectory);
        ledgerPath = Path.Combine(fullDirectory, "reconciled-notification-ledger.v1.json");
        temporaryPath = Path.Combine(fullDirectory, "reconciled-notification-ledger.v1.tmp");
        ownershipLock = new FileStream(
            Path.Combine(fullDirectory, "reconciled-notification-ledger.v1.lock"),
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None,
            1,
            FileOptions.WriteThrough);
        try
        {
            state = Load(ledgerPath);
        }
        catch
        {
            ownershipLock.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public ValueTask<ReconciledNotificationLedgerState> ReadAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Clone(state));
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryReplaceAsync(
        long expectedRevision,
        ReconciledNotificationLedgerState replacement,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(replacement);
        cancellationToken.ThrowIfCancellationRequested();
        if (state.Revision != expectedRevision)
        {
            return false;
        }
        Validate(replacement);
        if (replacement.Revision != checked(expectedRevision + 1))
        {
            throw new InvalidDataException("The local notification ledger revision is not the exact successor.");
        }

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(replacement, SerializerOptions);
        if (payload.Length > MaximumFileBytes)
        {
            throw new InvalidDataException("The local notification ledger exceeds its storage budget.");
        }
        await using (FileStream output = new(
            temporaryPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            8_192,
            FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
            await output.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            output.Flush(flushToDisk: true);
        }
        cancellationToken.ThrowIfCancellationRequested();
        File.Move(temporaryPath, ledgerPath, overwrite: true);
        state = Clone(replacement);
        return true;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!disposed)
        {
            disposed = true;
            ownershipLock.Dispose();
        }
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private static ReconciledNotificationLedgerState Load(string path)
    {
        if (!File.Exists(path))
        {
            return Clone(ReconciledNotificationLedgerState.Empty);
        }
        FileInfo info = new(path);
        if (info.Length <= 0 || info.Length > MaximumFileBytes)
        {
            throw new InvalidDataException("The local notification ledger has an invalid size.");
        }
        try
        {
            ReconciledNotificationLedgerState? loaded = JsonSerializer.Deserialize<ReconciledNotificationLedgerState>(
                File.ReadAllBytes(path),
                SerializerOptions);
            Validate(loaded);
            return Clone(loaded!);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The local notification ledger is malformed.", exception);
        }
    }

    private static void Validate(ReconciledNotificationLedgerState? candidate)
    {
        if (candidate is null ||
            !string.Equals(candidate.SchemaVersion, ReconciledNotificationLedgerState.CurrentSchemaVersion, StringComparison.Ordinal) ||
            candidate.Revision < 0 || candidate.Entries is null || candidate.Entries.Count > MaximumEntries ||
            (candidate.Cursor is not null && !ReconciledNotificationCursorCodec.TryDecode(candidate.Cursor, out _)))
        {
            throw new InvalidDataException("The local notification ledger contract is invalid.");
        }
        HashSet<Guid> ids = [];
        HashSet<string> tags = new(StringComparer.Ordinal);
        foreach (ReconciledNotificationLedgerEntry entry in candidate.Entries)
        {
            if (entry.EventId == Guid.Empty || !ids.Add(entry.EventId) ||
                entry.ContentSha256.Length != 64 || entry.ContentSha256.Any(character => !Uri.IsHexDigit(character)) ||
                entry.WindowsTag.Length != 16 || !tags.Add(entry.WindowsTag) ||
                entry.WindowsTag.Any(character => !(character is >= '0' and <= '9' or >= 'a' and <= 'f')) ||
                entry.AttemptCount is < 0 or > 2 || entry.FirstSeenAt.Offset != TimeSpan.Zero ||
                entry.LastDecidedAt.Offset != TimeSpan.Zero || entry.LastDecidedAt < entry.FirstSeenAt ||
                string.IsNullOrWhiteSpace(entry.ResultCode) || entry.ResultCode.Length > 100)
            {
                throw new InvalidDataException("A local notification ledger entry is invalid.");
            }
        }
    }

    private static ReconciledNotificationLedgerState Clone(ReconciledNotificationLedgerState source) =>
        source with { Entries = source.Entries.ToArray() };
}
