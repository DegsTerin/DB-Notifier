// Module purpose: Defines the bounded read-only transition, ledger and delivery orchestration used only by the authorised local notification sandbox.
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DBNotifier.Application.Presentation;

/// <summary>Defines the exact protocol and resource envelope for reconciled local notification transitions.</summary>
public static class ReconciledNotificationTransitionContract
{
    /// <summary>Gets the only transition schema accepted by this sandbox increment.</summary>
    public const string CurrentSchemaVersion = "reconciled-local-notification-transition.v1";

    /// <summary>Gets the only evidence source accepted by this sandbox increment.</summary>
    public const string SyntheticSourceKind = "synthetic-sandbox";

    /// <summary>Gets the maximum number of transitions materialised in one page and delivery queue.</summary>
    public const int MaximumItemCount = 16;

    /// <summary>Gets the maximum number of Agent sequence heads encoded by one sandbox cursor.</summary>
    public const int MaximumCursorAgents = 32;

    /// <summary>Gets the maximum accepted cursor length after base64url encoding.</summary>
    public const int MaximumCursorLength = 1_024;

    /// <summary>Gets the maximum accepted human-readable field length.</summary>
    public const int MaximumTextLength = 200;

    /// <summary>Gets the current-evidence window used by the isolated transition projection.</summary>
    public static TimeSpan CurrentEvidenceLifetime { get; } = TimeSpan.FromMinutes(2);

    /// <summary>Gets the canonical empty cursor used before a baseline has observed any Agent sequence.</summary>
    public static string EmptyCursor { get; } = ReconciledNotificationCursorCodec.Encode(
        new Dictionary<Guid, long>());
}

/// <summary>Represents one immutable transition derived from a committed canonical event.</summary>
/// <param name="EventId">Stable canonical event identity used for durable deduplication.</param>
/// <param name="InstanceId">Stable provider-neutral instance identity.</param>
/// <param name="DisplayName">Bounded synthetic instance label suitable for local presentation.</param>
/// <param name="EventType">Canonical event classification.</param>
/// <param name="Severity">Canonical event severity.</param>
/// <param name="PreviousStatus">Exact canonical status before reconciliation.</param>
/// <param name="CurrentStatus">Exact canonical status after reconciliation.</param>
/// <param name="ObservedAt">UTC instant reported by the synthetic observation.</param>
/// <param name="ReceivedAt">UTC instant assigned by the server boundary.</param>
/// <param name="Freshness">Factual current, stale or unknown evidence classification.</param>
/// <param name="SourceKind">Exact source marker; only synthetic sandbox evidence is accepted.</param>
public sealed record ReconciledNotificationTransition(
    Guid EventId,
    Guid InstanceId,
    string DisplayName,
    string EventType,
    string Severity,
    string PreviousStatus,
    string CurrentStatus,
    DateTimeOffset ObservedAt,
    DateTimeOffset ReceivedAt,
    string Freshness,
    string SourceKind);

/// <summary>Contains one bounded cursor page of committed reconciled transitions.</summary>
/// <param name="SchemaVersion">Exact protocol identifier.</param>
/// <param name="FromCursor">Cursor supplied by the caller, or the empty cursor for a baseline.</param>
/// <param name="NextCursor">Monotonic cursor after every transition classified by this page.</param>
/// <param name="HasMore">Whether another bounded page remains after <paramref name="NextCursor"/>.</param>
/// <param name="Items">Validated transitions in deterministic Agent/sequence order.</param>
public sealed record ReconciledNotificationTransitionPage(
    string SchemaVersion,
    string FromCursor,
    string NextCursor,
    bool HasMore,
    IReadOnlyList<ReconciledNotificationTransition> Items);

/// <summary>Describes a baseline or continuation read without exposing persistence implementation details.</summary>
/// <param name="Baseline">Whether the caller requests a silent head cursor rather than historical transitions.</param>
/// <param name="Cursor">Previously confirmed cursor for a continuation read.</param>
public sealed record ReconciledNotificationTransitionQuery(bool Baseline, string? Cursor);

/// <summary>Reads committed synthetic transitions for the explicitly enabled local server sandbox.</summary>
public interface IReconciledNotificationTransitionSource
{
    /// <summary>Reads one bounded baseline or continuation page.</summary>
    /// <param name="query">Validated read mode and optional continuation cursor.</param>
    /// <param name="cancellationToken">Cancellation propagated from the read-only request.</param>
    /// <returns>A complete page that will be validated before transport.</returns>
    ValueTask<ReconciledNotificationTransitionPage> ReadAsync(
        ReconciledNotificationTransitionQuery query,
        CancellationToken cancellationToken);
}

/// <summary>Classifies a bounded transition read without throwing transport details into the coordinator.</summary>
public enum ReconciledNotificationReadDisposition
{
    /// <summary>A complete validated page was returned.</summary>
    Accepted,

    /// <summary>The sandbox identity was absent or denied.</summary>
    Denied,

    /// <summary>The server or payload contract was incompatible.</summary>
    Incompatible,

    /// <summary>A temporary transport or projection failure permits a later bounded retry.</summary>
    Retryable,
}

/// <summary>Contains the factual outcome of one baseline or continuation read.</summary>
/// <param name="Disposition">Stable read classification.</param>
/// <param name="Page">Validated page only when the disposition is accepted.</param>
/// <param name="ErrorCode">Sanitised non-secret reason code.</param>
public sealed record ReconciledNotificationReadResult(
    ReconciledNotificationReadDisposition Disposition,
    ReconciledNotificationTransitionPage? Page,
    string? ErrorCode);

/// <summary>Reads transition pages through a bounded transport owned outside the Application layer.</summary>
public interface IReconciledNotificationTransitionReader
{
    /// <summary>Reads a silent baseline head.</summary>
    /// <param name="cancellationToken">Cancellation for the bounded request.</param>
    /// <returns>The classified read result.</returns>
    ValueTask<ReconciledNotificationReadResult> ReadBaselineAsync(CancellationToken cancellationToken);

    /// <summary>Reads transitions strictly after a previously confirmed cursor.</summary>
    /// <param name="cursor">Exact cursor stored by the local ledger.</param>
    /// <param name="cancellationToken">Cancellation for the bounded request.</param>
    /// <returns>The classified read result.</returns>
    ValueTask<ReconciledNotificationReadResult> ReadAfterAsync(
        string cursor,
        CancellationToken cancellationToken);
}

/// <summary>Classifies the local platform hand-off without claiming that Windows displayed a notification.</summary>
public enum ReconciledNotificationDeliveryDisposition
{
    /// <summary>The primary Windows API or safe local fallback accepted the request.</summary>
    Accepted,

    /// <summary>A bounded later retry may be attempted.</summary>
    Retryable,

    /// <summary>The request failed terminally and must not be repeated automatically.</summary>
    FailedTerminal,
}

/// <summary>Contains localised, sanitised content for one event-specific Windows request.</summary>
/// <param name="EventId">Stable canonical event identity.</param>
/// <param name="WindowsTag">Deterministic bounded Windows replacement tag.</param>
/// <param name="Transition">Validated factual transition.</param>
public sealed record ReconciledNotificationDeliveryRequest(
    Guid EventId,
    string WindowsTag,
    ReconciledNotificationTransition Transition);

/// <summary>Contains the sanitised result returned by a local platform adapter.</summary>
/// <param name="Disposition">Whether the hand-off was accepted, retryable or terminal.</param>
/// <param name="ErrorCode">Stable non-secret result code.</param>
public sealed record ReconciledNotificationDeliveryResult(
    ReconciledNotificationDeliveryDisposition Disposition,
    string ErrorCode);

/// <summary>Hands one validated transition to the Windows-specific presentation boundary.</summary>
public interface IReconciledNotificationSink
{
    /// <summary>Attempts one local platform hand-off without performing any administrative action.</summary>
    /// <param name="request">Validated event identity and transition content.</param>
    /// <param name="cancellationToken">Cancellation observed before platform hand-off.</param>
    /// <returns>A factual request result; acceptance is not proof of visible display.</returns>
    ValueTask<ReconciledNotificationDeliveryResult> DeliverAsync(
        ReconciledNotificationDeliveryRequest request,
        CancellationToken cancellationToken);
}

/// <summary>Classifies one durable local event decision.</summary>
public enum ReconciledNotificationLedgerDisposition
{
    /// <summary>The intent was committed before calling the non-transactional Windows boundary.</summary>
    Attempting = 0,

    /// <summary>The local platform boundary accepted the hand-off request.</summary>
    Accepted = 1,

    /// <summary>Policy suppressed the event without a platform request.</summary>
    Suppressed = 2,

    /// <summary>A bounded later retry remains permitted.</summary>
    Retryable = 3,

    /// <summary>The event was rejected terminally or became uncertain and will not be repeated automatically.</summary>
    Rejected = 4,

    /// <summary>The validated transition is durably queued and has not reached the Windows boundary.</summary>
    Queued = 5,
}

/// <summary>Represents one durable deduplication and delivery decision.</summary>
/// <param name="EventId">Stable canonical event identity.</param>
/// <param name="ContentSha256">Hash of the complete validated transition content.</param>
/// <param name="WindowsTag">Deterministic bounded Windows replacement tag.</param>
/// <param name="Disposition">Current durable delivery decision.</param>
/// <param name="AttemptCount">Number of platform hand-offs begun.</param>
/// <param name="QueueSequence">Monotonic local order assigned before any delivery in the page begins.</param>
/// <param name="FirstSeenAt">UTC instant when the transition first entered the ledger.</param>
/// <param name="LastDecidedAt">UTC instant of the most recent durable decision.</param>
/// <param name="ResultCode">Sanitised non-secret outcome code.</param>
public sealed record ReconciledNotificationLedgerEntry(
    Guid EventId,
    string ContentSha256,
    string WindowsTag,
    ReconciledNotificationLedgerDisposition Disposition,
    int AttemptCount,
    long QueueSequence,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastDecidedAt,
    string ResultCode);

/// <summary>Contains the complete bounded durable state owned by one isolated sandbox consumer.</summary>
/// <param name="SchemaVersion">Exact local ledger schema identifier.</param>
/// <param name="Revision">Optimistic revision advanced on every atomic replacement.</param>
/// <param name="Cursor">Last fully classified server cursor, or null before the silent baseline.</param>
/// <param name="NextQueueSequence">Next positive sequence reserved for a newly queued transition.</param>
/// <param name="Entries">Bounded recent decisions used for deduplication and collision detection.</param>
public sealed record ReconciledNotificationLedgerState(
    string SchemaVersion,
    long Revision,
    string? Cursor,
    long NextQueueSequence,
    IReadOnlyList<ReconciledNotificationLedgerEntry> Entries)
{
    /// <summary>Gets the exact local ledger schema version.</summary>
    public const string CurrentSchemaVersion = "reconciled-local-notification-ledger.v2";

    /// <summary>Gets a new empty ledger that still requires a silent baseline.</summary>
    public static ReconciledNotificationLedgerState Empty { get; } = new(
        CurrentSchemaVersion,
        0,
        null,
        1,
        []);
}

/// <summary>Persists one isolated, versioned ledger behind an atomic optimistic boundary.</summary>
public interface IReconciledNotificationLedger : IAsyncDisposable
{
    /// <summary>Reads the complete current ledger state.</summary>
    /// <param name="cancellationToken">Cancellation for local storage access.</param>
    /// <returns>The validated durable state.</returns>
    ValueTask<ReconciledNotificationLedgerState> ReadAsync(CancellationToken cancellationToken);

    /// <summary>Atomically replaces the ledger only when its revision still matches the caller's snapshot.</summary>
    /// <param name="expectedRevision">Revision read before constructing the replacement.</param>
    /// <param name="replacement">Complete bounded replacement with the next revision.</param>
    /// <param name="cancellationToken">Cancellation observed before the atomic replacement.</param>
    /// <returns>True only when the replacement was committed.</returns>
    ValueTask<bool> TryReplaceAsync(
        long expectedRevision,
        ReconciledNotificationLedgerState replacement,
        CancellationToken cancellationToken);
}

/// <summary>Defines the explicit local opt-in and quiet decision supplied by the sandbox composition.</summary>
/// <param name="Enabled">Whether the isolated consumer is explicitly enabled; false is the default.</param>
/// <param name="Quiet">Whether valid new transitions are durably suppressed without delayed replay.</param>
public sealed record ReconciledNotificationSandboxPolicy(bool Enabled, bool Quiet);

/// <summary>Classifies the outcome of one bounded reconciliation cycle.</summary>
public enum ReconciledNotificationCycleDisposition
{
    /// <summary>The explicit opt-in is off and no read or ledger mutation occurred.</summary>
    Disabled,

    /// <summary>A silent baseline was durably established without notification.</summary>
    BaselineEstablished,

    /// <summary>No transition remained after the confirmed cursor.</summary>
    NoChanges,

    /// <summary>The bounded page was classified and its cursor committed.</summary>
    Completed,

    /// <summary>A temporary read or delivery failure permits a later bounded cycle.</summary>
    Retryable,

    /// <summary>The test identity was denied without revealing transitions.</summary>
    Denied,

    /// <summary>The contract was incompatible and failed closed.</summary>
    Incompatible,

    /// <summary>Cursor, hash, tag or optimistic state conflicted and failed closed.</summary>
    Conflict,
}

/// <summary>Summarises one bounded cycle without exposing notification content or storage details.</summary>
/// <param name="Disposition">Overall cycle classification.</param>
/// <param name="Accepted">Number of local platform hand-offs accepted.</param>
/// <param name="Suppressed">Number of transitions suppressed by quiet or freshness policy.</param>
/// <param name="FailedTerminal">Number of terminal or uncertain transitions.</param>
/// <param name="ErrorCode">Optional sanitised reason code.</param>
public sealed record ReconciledNotificationCycleResult(
    ReconciledNotificationCycleDisposition Disposition,
    int Accepted,
    int Suppressed,
    int FailedTerminal,
    string? ErrorCode = null);

/// <summary>Encodes and compares bounded per-Agent reconciliation heads as one canonical opaque wire cursor.</summary>
public static class ReconciledNotificationCursorCodec
{
    private const byte FormatVersion = 1;
    private const int HeaderLength = 3;
    private const int EntryLength = 24;

    /// <summary>Encodes non-negative Agent sequence heads in deterministic identifier order.</summary>
    /// <param name="heads">Bounded Agent identifiers and their highest classified sequences.</param>
    /// <returns>A canonical unpadded base64url cursor.</returns>
    /// <exception cref="ArgumentException">Thrown when an identifier or sequence is invalid or the cursor exceeds its contract.</exception>
    public static string Encode(IReadOnlyDictionary<Guid, long> heads)
    {
        ArgumentNullException.ThrowIfNull(heads);
        if (heads.Count > ReconciledNotificationTransitionContract.MaximumCursorAgents ||
            heads.Any(item => item.Key == Guid.Empty || item.Value < 0))
        {
            throw new ArgumentException("A reconciled notification cursor contains invalid or excessive Agent heads.", nameof(heads));
        }

        KeyValuePair<Guid, long>[] ordered = heads
            .OrderBy(item => item.Key.ToString("N"), StringComparer.Ordinal)
            .ToArray();
        byte[] buffer = new byte[HeaderLength + (ordered.Length * EntryLength)];
        buffer[0] = FormatVersion;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(1, 2), checked((ushort)ordered.Length));
        int offset = HeaderLength;
        foreach ((Guid agentId, long sequence) in ordered)
        {
            if (!agentId.TryWriteBytes(buffer.AsSpan(offset, 16), bigEndian: true, out int written) || written != 16)
            {
                throw new InvalidOperationException("The runtime could not encode a canonical Agent identifier.");
            }
            BinaryPrimitives.WriteInt64BigEndian(buffer.AsSpan(offset + 16, 8), sequence);
            offset += EntryLength;
        }

        return Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Decodes an exact canonical cursor and rejects malformed, unsorted or excessive content.</summary>
    /// <param name="cursor">Untrusted wire cursor.</param>
    /// <param name="heads">Decoded Agent sequence heads when valid.</param>
    /// <returns>True only for the canonical representation accepted by <see cref="Encode"/>.</returns>
    public static bool TryDecode(string? cursor, out IReadOnlyDictionary<Guid, long> heads)
    {
        heads = new Dictionary<Guid, long>();
        if (string.IsNullOrWhiteSpace(cursor) ||
            cursor.Length > ReconciledNotificationTransitionContract.MaximumCursorLength ||
            cursor.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')))
        {
            return false;
        }

        try
        {
            string base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 += new string('=', (4 - (base64.Length % 4)) % 4);
            byte[] buffer = Convert.FromBase64String(base64);
            if (buffer.Length < HeaderLength || buffer[0] != FormatVersion)
            {
                return false;
            }

            int count = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(1, 2));
            if (count > ReconciledNotificationTransitionContract.MaximumCursorAgents ||
                buffer.Length != HeaderLength + (count * EntryLength))
            {
                return false;
            }

            Dictionary<Guid, long> decoded = new(count);
            string? previousId = null;
            int offset = HeaderLength;
            for (int index = 0; index < count; index++)
            {
                Guid agentId = new(buffer.AsSpan(offset, 16), bigEndian: true);
                long sequence = BinaryPrimitives.ReadInt64BigEndian(buffer.AsSpan(offset + 16, 8));
                string currentId = agentId.ToString("N");
                if (agentId == Guid.Empty || sequence < 0 ||
                    (previousId is not null && string.CompareOrdinal(previousId, currentId) >= 0) ||
                    !decoded.TryAdd(agentId, sequence))
                {
                    return false;
                }
                previousId = currentId;
                offset += EntryLength;
            }

            if (!string.Equals(Encode(decoded), cursor, StringComparison.Ordinal))
            {
                return false;
            }
            heads = decoded;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>Determines whether a next cursor preserves or advances every previously confirmed Agent sequence.</summary>
    /// <param name="next">Candidate next cursor.</param>
    /// <param name="previous">Previously confirmed cursor.</param>
    /// <returns>True only when both are valid and no Agent sequence rolls back or disappears.</returns>
    public static bool Dominates(string next, string previous)
    {
        if (!TryDecode(next, out IReadOnlyDictionary<Guid, long>? nextHeads) ||
            !TryDecode(previous, out IReadOnlyDictionary<Guid, long>? previousHeads))
        {
            return false;
        }

        return previousHeads.All(item =>
            nextHeads.TryGetValue(item.Key, out long sequence) && sequence >= item.Value);
    }
}

/// <summary>Validates every field before a transition page can influence local notification state.</summary>
public static class ReconciledNotificationTransitionValidator
{
    private static readonly HashSet<string> AllowedStatuses =
    [
        "Healthy", "Degraded", "Unavailable", "AuthFailed", "Timeout", "Maintenance", "Unknown",
    ];

    private static readonly Dictionary<string, (string CurrentStatus, string Severity)> EventMappings =
        new(StringComparer.Ordinal)
        {
            ["Connected"] = ("Healthy", "Info"),
            ["Recovered"] = ("Healthy", "Info"),
            ["Disconnected"] = ("Unavailable", "Error"),
            ["Timeout"] = ("Timeout", "Error"),
            ["AuthenticationFailed"] = ("AuthFailed", "Error"),
            ["Degraded"] = ("Degraded", "Warning"),
        };

    /// <summary>Validates schema, cursor monotonicity, bounds, uniqueness, source and factual transition semantics.</summary>
    /// <param name="page">Untrusted page returned by a transport or source.</param>
    /// <param name="requestedCursor">Exact cursor supplied to the read, or the empty cursor for a baseline.</param>
    /// <param name="baseline">Whether the page is required to contain no historical transitions.</param>
    /// <param name="now">Trusted UTC instant used to reject future evidence and false current freshness.</param>
    /// <param name="errorCode">Stable non-secret validation failure code.</param>
    /// <returns>True only when the entire page is safe to process.</returns>
    public static bool TryValidate(
        ReconciledNotificationTransitionPage? page,
        string requestedCursor,
        bool baseline,
        DateTimeOffset now,
        out string errorCode)
    {
        if (page is null ||
            !string.Equals(page.SchemaVersion, ReconciledNotificationTransitionContract.CurrentSchemaVersion, StringComparison.Ordinal) ||
            !string.Equals(page.FromCursor, requestedCursor, StringComparison.Ordinal) ||
            !ReconciledNotificationCursorCodec.TryDecode(page.FromCursor, out _) ||
            !ReconciledNotificationCursorCodec.TryDecode(page.NextCursor, out _) ||
            !ReconciledNotificationCursorCodec.Dominates(page.NextCursor, page.FromCursor) ||
            page.Items is null ||
            page.Items.Count > ReconciledNotificationTransitionContract.MaximumItemCount ||
            (page.HasMore && string.Equals(page.NextCursor, page.FromCursor, StringComparison.Ordinal)) ||
            (baseline && (page.Items.Count != 0 || page.HasMore)))
        {
            errorCode = "reconciled_notification.page_invalid";
            return false;
        }

        HashSet<Guid> eventIds = [];
        HashSet<string> contentHashes = new(StringComparer.Ordinal);
        foreach (ReconciledNotificationTransition transition in page.Items)
        {
            if (transition.EventId == Guid.Empty || transition.InstanceId == Guid.Empty ||
                !eventIds.Add(transition.EventId) ||
                !IsBoundedText(transition.DisplayName) ||
                !EventMappings.TryGetValue(transition.EventType, out (string CurrentStatus, string Severity) mapping) ||
                !string.Equals(mapping.Severity, transition.Severity, StringComparison.Ordinal) ||
                !AllowedStatuses.Contains(transition.PreviousStatus) ||
                !AllowedStatuses.Contains(transition.CurrentStatus) ||
                !string.Equals(mapping.CurrentStatus, transition.CurrentStatus, StringComparison.Ordinal) ||
                string.Equals(transition.PreviousStatus, transition.CurrentStatus, StringComparison.Ordinal) ||
                transition.ObservedAt.Offset != TimeSpan.Zero || transition.ReceivedAt.Offset != TimeSpan.Zero ||
                transition.ObservedAt > transition.ReceivedAt || transition.ReceivedAt > now ||
                transition.Freshness is not ("current" or "stale" or "unknown") ||
                (transition.Freshness == "current" && now - transition.ReceivedAt > ReconciledNotificationTransitionContract.CurrentEvidenceLifetime) ||
                !string.Equals(
                    transition.SourceKind,
                    ReconciledNotificationTransitionContract.SyntheticSourceKind,
                    StringComparison.Ordinal) ||
                !contentHashes.Add(ReconciledNotificationIdentity.CreateContentSha256(transition)))
            {
                errorCode = "reconciled_notification.transition_invalid";
                return false;
            }
        }

        errorCode = string.Empty;
        return true;
    }

    private static bool IsBoundedText(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= ReconciledNotificationTransitionContract.MaximumTextLength &&
        value.All(character => !char.IsControl(character));
}

/// <summary>Creates stable content and Windows identities without exposing transition text in the ledger.</summary>
public static class ReconciledNotificationIdentity
{
    /// <summary>Creates a canonical SHA-256 digest across every presentation-relevant transition field.</summary>
    /// <param name="transition">Validated transition.</param>
    /// <returns>Uppercase hexadecimal SHA-256 digest.</returns>
    public static string CreateContentSha256(ReconciledNotificationTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        string canonical = string.Join('\n',
            transition.EventId.ToString("D"),
            transition.InstanceId.ToString("D"),
            transition.DisplayName,
            transition.EventType,
            transition.Severity,
            transition.PreviousStatus,
            transition.CurrentStatus,
            transition.ObservedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            transition.ReceivedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            transition.Freshness,
            transition.SourceKind);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    /// <summary>Creates a deterministic sixteen-character Windows replacement tag from the canonical event identity.</summary>
    /// <param name="eventId">Stable non-empty canonical event identity.</param>
    /// <returns>Lowercase hexadecimal tag within the Windows identifier limit.</returns>
    /// <exception cref="ArgumentException">Thrown when the event identity is empty.</exception>
    public static string CreateWindowsTag(Guid eventId)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("A reconciled notification requires a stable event identifier.", nameof(eventId));
        }

        Span<byte> identifier = stackalloc byte[16];
        eventId.TryWriteBytes(identifier);
        Span<byte> digest = stackalloc byte[32];
        SHA256.HashData(identifier, digest);
        return Convert.ToHexString(digest[..8]).ToLowerInvariant();
    }
}

/// <summary>Coordinates one serial bounded transition read, durable decision and local platform hand-off.</summary>
public sealed class ReconciledNotificationCoordinator : IDisposable
{
    private const int MaximumLedgerEntries = 256;
    private const int MaximumDeliveryAttempts = 2;
    private static readonly TimeSpan DeliveryAttemptTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RetryBackoff = TimeSpan.FromSeconds(5);
    private readonly IReconciledNotificationTransitionReader reader;
    private readonly IReconciledNotificationLedger ledger;
    private readonly IReconciledNotificationSink sink;
    private readonly TimeProvider timeProvider;
    private readonly SemaphoreSlim cycleGate = new(1, 1);
    private bool disposed;

    /// <summary>Initialises one coordinator around explicitly supplied sandbox boundaries.</summary>
    /// <param name="reader">Bounded read-only transition transport.</param>
    /// <param name="ledger">Exclusive durable local state.</param>
    /// <param name="sink">Windows-specific local presentation boundary.</param>
    /// <param name="timeProvider">Trusted clock used for freshness and durable decisions.</param>
    public ReconciledNotificationCoordinator(
        IReconciledNotificationTransitionReader reader,
        IReconciledNotificationLedger ledger,
        IReconciledNotificationSink sink,
        TimeProvider timeProvider)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Runs one non-overlapping cycle under explicit opt-in and quiet policy.</summary>
    /// <param name="policy">Explicit local sandbox opt-in and quiet decision.</param>
    /// <param name="cancellationToken">Cancellation propagated through read, ledger and delivery boundaries.</param>
    /// <returns>A sanitised cycle summary.</returns>
    public async ValueTask<ReconciledNotificationCycleResult> RunOnceAsync(
        ReconciledNotificationSandboxPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(policy);
        if (!policy.Enabled)
        {
            return new(ReconciledNotificationCycleDisposition.Disabled, 0, 0, 0);
        }

        if (!await cycleGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return new(ReconciledNotificationCycleDisposition.Retryable, 0, 0, 0, "reconciled_notification.cycle_busy");
        }

        try
        {
            ReconciledNotificationLedgerState state = await ledger.ReadAsync(cancellationToken).ConfigureAwait(false);
            return state.Cursor is null
                ? await EstablishBaselineAsync(state, cancellationToken).ConfigureAwait(false)
                : await ProcessPageAsync(state, policy, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            cycleGate.Release();
        }
    }

    private async ValueTask<ReconciledNotificationCycleResult> EstablishBaselineAsync(
        ReconciledNotificationLedgerState state,
        CancellationToken cancellationToken)
    {
        ReconciledNotificationReadResult read = await reader.ReadBaselineAsync(cancellationToken).ConfigureAwait(false);
        ReconciledNotificationCycleResult? failure = ClassifyReadFailure(read);
        if (failure is not null)
        {
            return failure;
        }

        ReconciledNotificationTransitionPage page = read.Page!;
        if (!ReconciledNotificationTransitionValidator.TryValidate(
                page,
                ReconciledNotificationTransitionContract.EmptyCursor,
                baseline: true,
                timeProvider.GetUtcNow(),
                out string errorCode))
        {
            return new(ReconciledNotificationCycleDisposition.Incompatible, 0, 0, 0, errorCode);
        }

        ReconciledNotificationLedgerState replacement = state with
        {
            Revision = checked(state.Revision + 1),
            Cursor = page.NextCursor,
        };
        bool saved = await ledger.TryReplaceAsync(state.Revision, replacement, cancellationToken).ConfigureAwait(false);
        return saved
            ? new(ReconciledNotificationCycleDisposition.BaselineEstablished, 0, 0, 0)
            : new(ReconciledNotificationCycleDisposition.Conflict, 0, 0, 0, "reconciled_notification.ledger_conflict");
    }

    private async ValueTask<ReconciledNotificationCycleResult> ProcessPageAsync(
        ReconciledNotificationLedgerState initialState,
        ReconciledNotificationSandboxPolicy policy,
        CancellationToken cancellationToken)
    {
        ReconciledNotificationReadResult read = await reader
            .ReadAfterAsync(initialState.Cursor!, cancellationToken)
            .ConfigureAwait(false);
        ReconciledNotificationCycleResult? failure = ClassifyReadFailure(read);
        if (failure is not null)
        {
            return failure;
        }

        ReconciledNotificationTransitionPage page = read.Page!;
        if (!ReconciledNotificationTransitionValidator.TryValidate(
                page,
                initialState.Cursor!,
                baseline: false,
                timeProvider.GetUtcNow(),
                out string errorCode))
        {
            return new(ReconciledNotificationCycleDisposition.Incompatible, 0, 0, 0, errorCode);
        }

        ReconciledNotificationLedgerState state = initialState;
        int accepted = 0;
        int suppressed = 0;
        int failedTerminal = 0;
        List<ReconciledNotificationLedgerEntry> stagedEntries = state.Entries.ToList();
        long nextQueueSequence = state.NextQueueSequence;
        foreach (ReconciledNotificationTransition transition in page.Items)
        {
            string contentHash = ReconciledNotificationIdentity.CreateContentSha256(transition);
            string windowsTag = ReconciledNotificationIdentity.CreateWindowsTag(transition.EventId);
            ReconciledNotificationLedgerEntry? existing = stagedEntries.SingleOrDefault(
                entry => entry.EventId == transition.EventId);
            if (existing is not null && !string.Equals(existing.ContentSha256, contentHash, StringComparison.Ordinal))
            {
                return new(ReconciledNotificationCycleDisposition.Conflict, accepted, suppressed, failedTerminal,
                    "reconciled_notification.event_content_conflict");
            }
            if (stagedEntries.Any(entry => entry.EventId != transition.EventId &&
                string.Equals(entry.WindowsTag, windowsTag, StringComparison.Ordinal)))
            {
                return new(ReconciledNotificationCycleDisposition.Conflict, accepted, suppressed, failedTerminal,
                    "reconciled_notification.windows_tag_collision");
            }
            if (existing is not null)
            {
                continue;
            }

            DateTimeOffset now = timeProvider.GetUtcNow();
            bool suppress = policy.Quiet || transition.Freshness is not "current";
            stagedEntries.Add(new(
                transition.EventId,
                contentHash,
                windowsTag,
                suppress
                    ? ReconciledNotificationLedgerDisposition.Suppressed
                    : ReconciledNotificationLedgerDisposition.Queued,
                0,
                nextQueueSequence,
                now,
                now,
                suppress ? (policy.Quiet ? "policy.quiet" : "policy.not_current") : "delivery.queued"));
            nextQueueSequence = checked(nextQueueSequence + 1);
            if (suppress)
            {
                suppressed++;
            }
        }

        int pendingCount = stagedEntries.Count(entry => entry.Disposition is
            ReconciledNotificationLedgerDisposition.Queued or
            ReconciledNotificationLedgerDisposition.Attempting or
            ReconciledNotificationLedgerDisposition.Retryable);
        if (pendingCount > MaximumLedgerEntries)
        {
            return new(ReconciledNotificationCycleDisposition.Retryable, accepted, suppressed, failedTerminal,
                "reconciled_notification.ledger_capacity_exceeded");
        }
        if (nextQueueSequence != state.NextQueueSequence)
        {
            ReconciledNotificationLedgerState staged = state with
            {
                Revision = checked(state.Revision + 1),
                NextQueueSequence = nextQueueSequence,
                Entries = Prune(stagedEntries),
            };
            if (!await ledger.TryReplaceAsync(state.Revision, staged, cancellationToken).ConfigureAwait(false))
            {
                return new(ReconciledNotificationCycleDisposition.Conflict, accepted, suppressed, failedTerminal,
                    "reconciled_notification.ledger_conflict");
            }
            state = staged;
        }

        HashSet<Guid> pageEventIds = page.Items.Select(item => item.EventId).ToHashSet();
        if (state.Entries.Any(entry =>
            entry.Disposition is (
                ReconciledNotificationLedgerDisposition.Queued or
                ReconciledNotificationLedgerDisposition.Attempting or
                ReconciledNotificationLedgerDisposition.Retryable) &&
            !pageEventIds.Contains(entry.EventId)))
        {
            return new(ReconciledNotificationCycleDisposition.Conflict, accepted, suppressed, failedTerminal,
                "reconciled_notification.pending_transition_missing");
        }

        ReconciledNotificationTransition[] orderedTransitions = page.Items
            .OrderBy(transition => state.Entries.Single(entry => entry.EventId == transition.EventId).QueueSequence)
            .ToArray();
        foreach (ReconciledNotificationTransition transition in orderedTransitions)
        {
            ReconciledNotificationLedgerEntry existing = state.Entries.Single(entry => entry.EventId == transition.EventId);
            if (existing.Disposition == ReconciledNotificationLedgerDisposition.Attempting)
            {
                ReconciledNotificationLedgerState? recovered = await ReplaceEntryAsync(
                    state,
                    existing with
                    {
                        Disposition = ReconciledNotificationLedgerDisposition.Rejected,
                        LastDecidedAt = timeProvider.GetUtcNow(),
                        ResultCode = "delivery.uncertain_after_restart",
                    },
                    cancellationToken).ConfigureAwait(false);
                if (recovered is null)
                {
                    return new(ReconciledNotificationCycleDisposition.Conflict, accepted, suppressed, failedTerminal,
                        "reconciled_notification.ledger_conflict");
                }
                state = recovered;
                failedTerminal++;
                continue;
            }
            if (existing.Disposition is
                ReconciledNotificationLedgerDisposition.Accepted or
                ReconciledNotificationLedgerDisposition.Suppressed or
                ReconciledNotificationLedgerDisposition.Rejected)
            {
                continue;
            }

            DateTimeOffset now = timeProvider.GetUtcNow();
            if (policy.Quiet || transition.Freshness is not "current")
            {
                ReconciledNotificationLedgerState? saved = await ReplaceEntryAsync(
                    state,
                    existing with
                    {
                        Disposition = ReconciledNotificationLedgerDisposition.Suppressed,
                        LastDecidedAt = now,
                        ResultCode = policy.Quiet ? "policy.quiet" : "policy.not_current",
                    },
                    cancellationToken).ConfigureAwait(false);
                if (saved is null)
                {
                    return new(ReconciledNotificationCycleDisposition.Conflict, accepted, suppressed, failedTerminal,
                        "reconciled_notification.ledger_conflict");
                }
                state = saved;
                suppressed++;
                continue;
            }
            if (existing.Disposition == ReconciledNotificationLedgerDisposition.Retryable &&
                now - existing.LastDecidedAt < RetryBackoff)
            {
                return new(ReconciledNotificationCycleDisposition.Retryable, accepted, suppressed, failedTerminal,
                    "delivery.backoff_pending");
            }

            int attemptCount = checked(existing.AttemptCount + 1);
            if (attemptCount > MaximumDeliveryAttempts)
            {
                ReconciledNotificationLedgerState? exhausted = await ReplaceEntryAsync(
                    state,
                    existing with
                    {
                        Disposition = ReconciledNotificationLedgerDisposition.Rejected,
                        LastDecidedAt = now,
                        ResultCode = "delivery.retry_budget_exhausted",
                    },
                    cancellationToken).ConfigureAwait(false);
                if (exhausted is null)
                {
                    return new(ReconciledNotificationCycleDisposition.Conflict, accepted, suppressed, failedTerminal,
                        "reconciled_notification.ledger_conflict");
                }
                state = exhausted;
                failedTerminal++;
                continue;
            }

            ReconciledNotificationLedgerEntry attempting = existing with
            {
                Disposition = ReconciledNotificationLedgerDisposition.Attempting,
                AttemptCount = attemptCount,
                LastDecidedAt = now,
                ResultCode = "delivery.attempting",
            };
            ReconciledNotificationLedgerState? attemptSaved = await ReplaceEntryAsync(
                state,
                attempting,
                cancellationToken).ConfigureAwait(false);
            if (attemptSaved is null)
            {
                return new(ReconciledNotificationCycleDisposition.Conflict, accepted, suppressed, failedTerminal,
                    "reconciled_notification.ledger_conflict");
            }
            state = attemptSaved;

            ReconciledNotificationDeliveryResult delivery = await DeliverWithDeadlineAsync(
                new ReconciledNotificationDeliveryRequest(transition.EventId, existing.WindowsTag, transition),
                cancellationToken).ConfigureAwait(false);
            ReconciledNotificationLedgerDisposition disposition = delivery.Disposition switch
            {
                ReconciledNotificationDeliveryDisposition.Accepted => ReconciledNotificationLedgerDisposition.Accepted,
                ReconciledNotificationDeliveryDisposition.Retryable when attemptCount < MaximumDeliveryAttempts =>
                    ReconciledNotificationLedgerDisposition.Retryable,
                _ => ReconciledNotificationLedgerDisposition.Rejected,
            };
            ReconciledNotificationLedgerEntry decided = attempting with
            {
                Disposition = disposition,
                LastDecidedAt = timeProvider.GetUtcNow(),
                ResultCode = delivery.ErrorCode,
            };
            ReconciledNotificationLedgerState? decisionSaved = await ReplaceEntryAsync(
                state,
                decided,
                cancellationToken).ConfigureAwait(false);
            if (decisionSaved is null)
            {
                return new(ReconciledNotificationCycleDisposition.Conflict, accepted, suppressed, failedTerminal,
                    "reconciled_notification.ledger_conflict");
            }
            state = decisionSaved;

            if (disposition == ReconciledNotificationLedgerDisposition.Accepted)
            {
                accepted++;
            }
            else if (disposition == ReconciledNotificationLedgerDisposition.Retryable)
            {
                return new(ReconciledNotificationCycleDisposition.Retryable, accepted, suppressed, failedTerminal,
                    delivery.ErrorCode);
            }
            else
            {
                failedTerminal++;
            }
        }

        ReconciledNotificationLedgerState finalState = state with
        {
            Revision = checked(state.Revision + 1),
            Cursor = page.NextCursor,
            Entries = Prune(state.Entries),
        };
        if (!await ledger.TryReplaceAsync(state.Revision, finalState, cancellationToken).ConfigureAwait(false))
        {
            return new(ReconciledNotificationCycleDisposition.Conflict, accepted, suppressed, failedTerminal,
                "reconciled_notification.ledger_conflict");
        }

        ReconciledNotificationCycleDisposition completed = page.Items.Count == 0 && !page.HasMore
            ? ReconciledNotificationCycleDisposition.NoChanges
            : ReconciledNotificationCycleDisposition.Completed;
        return new(completed, accepted, suppressed, failedTerminal);
    }

    private async ValueTask<ReconciledNotificationDeliveryResult> DeliverWithDeadlineAsync(
        ReconciledNotificationDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(DeliveryAttemptTimeout);
        try
        {
            return await sink
                .DeliverAsync(request, deadline.Token)
                .AsTask()
                .WaitAsync(DeliveryAttemptTimeout, timeProvider, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            exception is OperationCanceledException or TimeoutException)
        {
            deadline.Cancel();
            return new(ReconciledNotificationDeliveryDisposition.FailedTerminal, "delivery.deadline_uncertain");
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            return new(ReconciledNotificationDeliveryDisposition.FailedTerminal, "delivery.boundary_uncertain");
        }
    }

    private async ValueTask<ReconciledNotificationLedgerState?> ReplaceEntryAsync(
        ReconciledNotificationLedgerState state,
        ReconciledNotificationLedgerEntry replacement,
        CancellationToken cancellationToken)
    {
        List<ReconciledNotificationLedgerEntry> entries = state.Entries
            .Where(entry => entry.EventId != replacement.EventId)
            .Append(replacement)
            .OrderBy(entry => entry.FirstSeenAt)
            .ThenBy(entry => entry.EventId)
            .ToList();
        ReconciledNotificationLedgerState next = state with
        {
            Revision = checked(state.Revision + 1),
            Entries = Prune(entries),
        };
        return await ledger.TryReplaceAsync(state.Revision, next, cancellationToken).ConfigureAwait(false)
            ? next
            : null;
    }

    private static ReconciledNotificationLedgerEntry[] Prune(
        IReadOnlyList<ReconciledNotificationLedgerEntry> entries)
    {
        if (entries.Count <= MaximumLedgerEntries)
        {
            return entries.ToArray();
        }

        ReconciledNotificationLedgerEntry[] pending = entries
            .Where(entry => entry.Disposition is
                ReconciledNotificationLedgerDisposition.Queued or
                ReconciledNotificationLedgerDisposition.Attempting or
                ReconciledNotificationLedgerDisposition.Retryable)
            .OrderBy(entry => entry.FirstSeenAt)
            .ThenBy(entry => entry.EventId)
            .ToArray();
        if (pending.Length > MaximumLedgerEntries)
        {
            throw new InvalidDataException("The durable local notification queue reached its bounded capacity.");
        }

        return pending
            .Concat(entries
                .Where(entry => entry.Disposition is not (
                    ReconciledNotificationLedgerDisposition.Queued or
                    ReconciledNotificationLedgerDisposition.Attempting or
                    ReconciledNotificationLedgerDisposition.Retryable))
                .OrderByDescending(entry => entry.LastDecidedAt)
                .ThenByDescending(entry => entry.EventId)
                .Take(MaximumLedgerEntries - pending.Length))
            .OrderBy(entry => entry.FirstSeenAt)
            .ThenBy(entry => entry.EventId)
            .ToArray();
    }

    private static ReconciledNotificationCycleResult? ClassifyReadFailure(
        ReconciledNotificationReadResult read) => read.Disposition switch
        {
            ReconciledNotificationReadDisposition.Accepted when read.Page is not null => null,
            ReconciledNotificationReadDisposition.Denied =>
                new(ReconciledNotificationCycleDisposition.Denied, 0, 0, 0, read.ErrorCode),
            ReconciledNotificationReadDisposition.Incompatible =>
                new(ReconciledNotificationCycleDisposition.Incompatible, 0, 0, 0, read.ErrorCode),
            _ => new(ReconciledNotificationCycleDisposition.Retryable, 0, 0, 0,
                read.ErrorCode ?? "reconciled_notification.read_unavailable"),
        };

    /// <summary>Releases the serial cycle gate owned by this coordinator.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        cycleGate.Dispose();
        GC.SuppressFinalize(this);
    }
}
