// Module purpose: Defines the bounded, versioned read-only snapshot contract used by the local Dashboard TV sandbox.
namespace DBNotifier.Application.Presentation;

/// <summary>Defines the current Dashboard TV snapshot protocol and its defensive resource limits.</summary>
public static class DashboardTvSnapshotContract
{
    /// <summary>Gets the only schema version accepted by this restricted increment.</summary>
    public const string CurrentSchemaVersion = "dashboard-tv.v1";

    /// <summary>Gets the maximum number of inventory items accepted in one snapshot.</summary>
    public const int MaximumItemCount = 500;

    /// <summary>Gets the maximum length accepted for a human-readable field.</summary>
    public const int MaximumTextLength = 200;

    /// <summary>Gets the maximum non-secret latency represented by the contract.</summary>
    public const int MaximumLatencyMilliseconds = 3_600_000;
}

/// <summary>Reads one bounded Dashboard TV snapshot without exposing persistence or transport details.</summary>
public interface IDashboardTvSnapshotSource
{
    /// <summary>Reads the current authoritative snapshot for one explicitly authorised composition.</summary>
    /// <param name="cancellationToken">Cancellation propagated from the read-only API request.</param>
    /// <returns>A complete immutable snapshot that will be validated before transport.</returns>
    ValueTask<DashboardTvSnapshot> ReadAsync(CancellationToken cancellationToken);
}

/// <summary>Contains one immutable, provider-neutral item in a Dashboard TV snapshot.</summary>
/// <param name="InstanceId">Stable non-secret instance identifier.</param>
/// <param name="DisplayName">Human-readable instance label.</param>
/// <param name="ProviderType">Stable provider identifier without provider-native details.</param>
/// <param name="SupportLabel">Factual support label.</param>
/// <param name="Environment">Human-readable environment label.</param>
/// <param name="LocationLabel">Non-secret location label.</param>
/// <param name="Status">Canonical provider-neutral health-status identifier.</param>
/// <param name="ObservedAt">UTC evidence observation instant in round-trip format.</param>
/// <param name="ReceivedAt">UTC authoritative receipt instant in round-trip format.</param>
/// <param name="LatencyMilliseconds">Optional bounded probe latency.</param>
/// <param name="Enabled">Whether the item is enabled in the immutable fixture.</param>
public sealed record DashboardTvInventoryItem(
    Guid InstanceId,
    string DisplayName,
    string ProviderType,
    string SupportLabel,
    string Environment,
    string LocationLabel,
    string Status,
    string ObservedAt,
    string ReceivedAt,
    int? LatencyMilliseconds,
    bool Enabled);

/// <summary>Contains one bounded immutable snapshot returned by the local read-only sandbox endpoint.</summary>
/// <param name="SchemaVersion">Versioned protocol identifier.</param>
/// <param name="GeneratedAt">UTC generation instant in round-trip format.</param>
/// <param name="Items">Immutable provider-neutral inventory items.</param>
public sealed record DashboardTvSnapshot(
    string SchemaVersion,
    string GeneratedAt,
    IReadOnlyList<DashboardTvInventoryItem> Items);

/// <summary>Validates local fixture snapshots before they cross the API trust boundary.</summary>
public static class DashboardTvSnapshotValidator
{
    private static readonly HashSet<string> AllowedStatuses =
    [
        "healthy", "degraded", "unavailable", "authFailed", "timeout", "maintenance", "unknown",
    ];

    /// <summary>Validates schema, bounds, uniqueness and UTC timestamp ordering.</summary>
    /// <param name="snapshot">Snapshot to validate.</param>
    /// <param name="now">Current UTC instant used to reject future evidence.</param>
    /// <param name="errorCode">Stable non-secret failure code, or an empty string when valid.</param>
    /// <returns><see langword="true"/> only when the complete snapshot is safe to expose.</returns>
    public static bool TryValidate(DashboardTvSnapshot? snapshot, DateTimeOffset now, out string errorCode)
    {
        if (snapshot is null ||
            !string.Equals(snapshot.SchemaVersion, DashboardTvSnapshotContract.CurrentSchemaVersion, StringComparison.Ordinal) ||
            !TryParseUtc(snapshot.GeneratedAt, out DateTimeOffset generatedAt) ||
            generatedAt > now ||
            snapshot.Items is null ||
            snapshot.Items.Count > DashboardTvSnapshotContract.MaximumItemCount)
        {
            errorCode = "dashboard_tv.snapshot_invalid";
            return false;
        }

        HashSet<Guid> instanceIds = [];
        foreach (DashboardTvInventoryItem item in snapshot.Items)
        {
            if (item.InstanceId == Guid.Empty ||
                !instanceIds.Add(item.InstanceId) ||
                !IsBoundedText(item.DisplayName) ||
                !IsBoundedText(item.ProviderType) ||
                !IsBoundedText(item.SupportLabel) ||
                !IsBoundedText(item.Environment) ||
                !IsBoundedText(item.LocationLabel) ||
                !AllowedStatuses.Contains(item.Status) ||
                !TryParseUtc(item.ObservedAt, out DateTimeOffset observedAt) ||
                !TryParseUtc(item.ReceivedAt, out DateTimeOffset receivedAt) ||
                observedAt > receivedAt ||
                receivedAt > generatedAt ||
                item.LatencyMilliseconds is < 0 or > DashboardTvSnapshotContract.MaximumLatencyMilliseconds)
            {
                errorCode = "dashboard_tv.item_invalid";
                return false;
            }
        }

        errorCode = string.Empty;
        return true;
    }

    private static bool IsBoundedText(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= DashboardTvSnapshotContract.MaximumTextLength;

    private static bool TryParseUtc(string? value, out DateTimeOffset parsed) =>
        DateTimeOffset.TryParse(
            value,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out parsed) && parsed.Offset == TimeSpan.Zero;
}
