// Module purpose: Reconciles bounded provider-neutral desktop inventory snapshots without owning providers, transports or administrative actions.
namespace DBNotifier.Application.Presentation;

/// <summary>Classifies the factual outcome of one read-only desktop fleet snapshot acquisition.</summary>
public enum DesktopFleetReadDisposition
{
    /// <summary>The source returned a structurally valid snapshot that may replace the prior accepted snapshot.</summary>
    Accepted,

    /// <summary>The source cannot currently be reached; any retained evidence must continue to age.</summary>
    Offline,

    /// <summary>The current identity is not authorised to read the source.</summary>
    Denied,

    /// <summary>The source or returned snapshot is incompatible with the current presentation contract.</summary>
    Incompatible,

    /// <summary>The source failed without exposing provider-native or secret-bearing diagnostics.</summary>
    Failed,

    /// <summary>Another acquisition already owns the single-flight boundary.</summary>
    Busy,
}

/// <summary>Identifies why a read-only desktop fleet reconciliation was requested.</summary>
public enum DesktopFleetRefreshTrigger
{
    /// <summary>The notification-area application is establishing its first presentation frame.</summary>
    Initial,

    /// <summary>The bounded periodic timer requested a fresh snapshot.</summary>
    Periodic,

    /// <summary>The user explicitly requested a read-only refresh.</summary>
    Manual,

    /// <summary>Visible localised labels must be rebuilt without changing operational evidence.</summary>
    Localisation,
}

/// <summary>Contains one source-owned acquisition outcome before application validation and presentation.</summary>
/// <param name="Disposition">Factual read outcome reported by the source.</param>
/// <param name="Snapshot">Candidate provider-neutral snapshot, present only for an accepted outcome.</param>
/// <param name="ReasonCode">Stable non-secret reason code suitable for diagnostics and presentation routing.</param>
public sealed record DesktopFleetSnapshotReadResult(
    DesktopFleetReadDisposition Disposition,
    InventorySnapshot? Snapshot,
    string ReasonCode);

/// <summary>Reads one bounded provider-neutral inventory snapshot without mutating monitored systems.</summary>
public interface IDesktopFleetSnapshotSource
{
    /// <summary>Acquires the latest available inventory snapshot through the source's authorised read-only boundary.</summary>
    /// <param name="cancellationToken">Cancellation requested by the owning desktop lifecycle.</param>
    /// <returns>A factual source outcome containing no secret-bearing diagnostic text.</returns>
    /// <exception cref="OperationCanceledException">Propagated when the caller cancels the acquisition.</exception>
    ValueTask<DesktopFleetSnapshotReadResult> ReadAsync(CancellationToken cancellationToken);
}

/// <summary>Contains one atomically derived desktop fleet presentation frame.</summary>
/// <param name="Snapshot">Latest accepted snapshot, or null when no source read has ever been accepted.</param>
/// <param name="Summary">Aggregate state evaluated from <paramref name="Snapshot"/> at <paramref name="EvaluatedAt"/>.</param>
/// <param name="EvaluatedAt">UTC instant shared by snapshot freshness and aggregate evaluation.</param>
/// <param name="Disposition">Factual outcome of the acquisition attempt that produced this frame.</param>
/// <param name="Trigger">Reason the owning desktop surface requested reconciliation.</param>
/// <param name="RetainedLastAcceptedSnapshot">Whether a failed acquisition retained prior accepted evidence.</param>
/// <param name="ReasonCode">Stable non-secret outcome code.</param>
public sealed record DesktopFleetReconciliationFrame(
    InventorySnapshot? Snapshot,
    TrayFleetSummary Summary,
    DateTimeOffset EvaluatedAt,
    DesktopFleetReadDisposition Disposition,
    DesktopFleetRefreshTrigger Trigger,
    bool RetainedLastAcceptedSnapshot,
    string ReasonCode);

/// <summary>
/// Serialises read-only fleet acquisition, validates candidates and publishes only coherent frames derived from one clock instant.
/// Failed reads retain the last accepted immutable snapshot while freshness continues to age towards Unknown.
/// </summary>
public sealed class DesktopFleetReconciliationCoordinator
{
    /// <summary>Maximum number of items accepted in one notification-area presentation snapshot.</summary>
    public const int MaximumSnapshotItems = 256;

    private readonly IDesktopFleetSnapshotSource source;
    private readonly TimeSpan staleAfter;
    private readonly TimeProvider timeProvider;
    private InventorySnapshot? lastAcceptedSnapshot;
    private int acquisitionInProgress;

    /// <summary>Initialises a single-flight reconciliation boundary for one read-only snapshot source.</summary>
    /// <param name="source">Authorised source that returns provider-neutral snapshot candidates.</param>
    /// <param name="staleAfter">Strictly positive maximum evidence age before current conclusions fail closed.</param>
    /// <param name="timeProvider">Clock used to give every field in a frame one evaluation instant.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="timeProvider"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="staleAfter"/> is not positive.</exception>
    public DesktopFleetReconciliationCoordinator(
        IDesktopFleetSnapshotSource source,
        TimeSpan staleAfter,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(staleAfter, TimeSpan.Zero);
        this.source = source;
        this.staleAfter = staleAfter;
        this.timeProvider = timeProvider;
    }

    /// <summary>Attempts one non-overlapping acquisition and atomically derives its presentation frame.</summary>
    /// <param name="trigger">Validated reason for requesting the refresh.</param>
    /// <param name="cancellationToken">Cancellation requested by the owning desktop lifecycle.</param>
    /// <returns>The newly accepted snapshot or a frame that safely retains and re-ages the prior accepted snapshot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="trigger"/> is not recognised.</exception>
    /// <exception cref="OperationCanceledException">Propagated when the caller cancels an acquired single-flight operation.</exception>
    public async ValueTask<DesktopFleetReconciliationFrame> ReconcileAsync(
        DesktopFleetRefreshTrigger trigger,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(trigger))
        {
            throw new ArgumentOutOfRangeException(nameof(trigger));
        }

        DateTimeOffset evaluatedAt = timeProvider.GetUtcNow();
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref acquisitionInProgress, 1, 0) != 0)
        {
            return CreateFrame(
                lastAcceptedSnapshot,
                evaluatedAt,
                DesktopFleetReadDisposition.Busy,
                trigger,
                retained: lastAcceptedSnapshot is not null,
                "reconciliation.busy");
        }

        try
        {
            DesktopFleetSnapshotReadResult result;
            try
            {
                result = await source.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return CreateFrame(
                    lastAcceptedSnapshot,
                    timeProvider.GetUtcNow(),
                    DesktopFleetReadDisposition.Failed,
                    trigger,
                    retained: lastAcceptedSnapshot is not null,
                    "source.failed");
            }

            evaluatedAt = timeProvider.GetUtcNow();
            if (result is null)
            {
                return CreateFrame(
                    lastAcceptedSnapshot,
                    evaluatedAt,
                    DesktopFleetReadDisposition.Failed,
                    trigger,
                    retained: lastAcceptedSnapshot is not null,
                    "source.result-missing");
            }

            if (result.Disposition == DesktopFleetReadDisposition.Accepted &&
                TryValidateAndCopy(result.Snapshot, evaluatedAt, out InventorySnapshot? accepted))
            {
                lastAcceptedSnapshot = accepted;
                return CreateFrame(
                    accepted,
                    evaluatedAt,
                    DesktopFleetReadDisposition.Accepted,
                    trigger,
                    retained: false,
                    NormaliseReasonCode(result.ReasonCode, "source.accepted"));
            }

            DesktopFleetReadDisposition disposition = result.Disposition == DesktopFleetReadDisposition.Accepted
                ? DesktopFleetReadDisposition.Incompatible
                : NormaliseFailureDisposition(result.Disposition);
            string fallbackReason = disposition == DesktopFleetReadDisposition.Incompatible
                ? "snapshot.invalid"
                : $"source.{disposition.ToString().ToLowerInvariant()}";
            string reasonCode = result.Disposition == DesktopFleetReadDisposition.Accepted
                ? fallbackReason
                : NormaliseReasonCode(result.ReasonCode, fallbackReason);
            return CreateFrame(
                lastAcceptedSnapshot,
                evaluatedAt,
                disposition,
                trigger,
                retained: lastAcceptedSnapshot is not null,
                reasonCode);
        }
        finally
        {
            Volatile.Write(ref acquisitionInProgress, 0);
        }
    }

    /// <summary>Creates one coherent aggregate from the retained snapshot and the supplied evaluation instant.</summary>
    private DesktopFleetReconciliationFrame CreateFrame(
        InventorySnapshot? snapshot,
        DateTimeOffset evaluatedAt,
        DesktopFleetReadDisposition disposition,
        DesktopFleetRefreshTrigger trigger,
        bool retained,
        string reasonCode)
    {
        TrayFleetSummary summary = snapshot is null
            ? new TrayFleetSummary(TrayAggregateState.Unknown, 0, 0, 0, 0, 0)
            : TrayFleetPresentationPolicy.Summarise(snapshot, evaluatedAt, staleAfter);
        return new(snapshot, summary, evaluatedAt, disposition, trigger, retained, reasonCode);
    }

    /// <summary>Accepts only the current bounded structural contract and copies its collection before retention.</summary>
    private static bool TryValidateAndCopy(
        InventorySnapshot? candidate,
        DateTimeOffset evaluatedAt,
        out InventorySnapshot? accepted)
    {
        accepted = null;
        if (candidate is null ||
            !string.Equals(candidate.SchemaVersion, InventorySnapshot.CurrentSchemaVersion, StringComparison.Ordinal) ||
            candidate.GeneratedAt > evaluatedAt ||
            candidate.Items is null ||
            candidate.Items.Count > MaximumSnapshotItems)
        {
            return false;
        }

        HashSet<Guid> identifiers = [];
        foreach (InstanceInventoryItem? item in candidate.Items)
        {
            if (item is null ||
                item.InstanceId == Guid.Empty ||
                !identifiers.Add(item.InstanceId) ||
                IsMissingOrOversized(item.DisplayName, 160) ||
                IsMissingOrOversized(item.ProviderType, 96) ||
                IsMissingOrOversized(item.SupportLabel, 160) ||
                IsMissingOrOversized(item.Environment, 160) ||
                IsMissingOrOversized(item.LocationLabel, 240) ||
                (item.Latency.HasValue &&
                    (item.Latency.Value < TimeSpan.Zero || item.Latency.Value > TimeSpan.FromDays(1))))
            {
                return false;
            }
        }

        accepted = candidate with { Items = candidate.Items.ToArray() };
        return true;
    }

    /// <summary>Rejects absent or unexpectedly large display values at the application trust boundary.</summary>
    private static bool IsMissingOrOversized(string? value, int maximumLength) =>
        string.IsNullOrWhiteSpace(value) || value.Length > maximumLength;

    /// <summary>Restricts source failures to dispositions that cannot replace accepted evidence.</summary>
    private static DesktopFleetReadDisposition NormaliseFailureDisposition(DesktopFleetReadDisposition disposition) =>
        disposition switch
        {
            DesktopFleetReadDisposition.Offline => DesktopFleetReadDisposition.Offline,
            DesktopFleetReadDisposition.Denied => DesktopFleetReadDisposition.Denied,
            DesktopFleetReadDisposition.Incompatible => DesktopFleetReadDisposition.Incompatible,
            DesktopFleetReadDisposition.Failed => DesktopFleetReadDisposition.Failed,
            _ => DesktopFleetReadDisposition.Failed,
        };

    /// <summary>Retains only a bounded stable reason code and otherwise uses a coordinator-owned fallback.</summary>
    private static string NormaliseReasonCode(string? reasonCode, string fallback) =>
        !string.IsNullOrWhiteSpace(reasonCode) && reasonCode.Length <= 96 &&
        reasonCode.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_')
            ? reasonCode
            : fallback;
}
