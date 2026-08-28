// Module purpose: Defines the bounded provider-neutral Desktop Fleet API wire contract and its conversion into presentation evidence.
using DBNotifier.Domain;

namespace DBNotifier.Application.Presentation;

/// <summary>Defines the exact Desktop Fleet read protocol admitted by the Tray integration boundary.</summary>
public static class DesktopFleetApiContract
{
    /// <summary>Gets the only wire schema accepted by this implementation lot.</summary>
    public const string CurrentSchemaVersion = "desktop-fleet.v1";

    /// <summary>Gets the exact relative route exposed by the human-authorised Server API.</summary>
    public const string SnapshotRoute = "/api/v1/desktop/fleet-snapshot";

    /// <summary>Gets the maximum response size admitted before JSON materialisation.</summary>
    public const int MaximumResponseBytes = 512 * 1024;
}

/// <summary>Contains one bounded provider-neutral observation transported to a Desktop Fleet client.</summary>
/// <param name="InstanceId">Stable non-secret database-instance identifier.</param>
/// <param name="DisplayName">Human-readable database-instance label.</param>
/// <param name="ProviderType">Stable open provider identifier.</param>
/// <param name="SupportLabel">Factual evidence classification without a support or homologation claim.</param>
/// <param name="Environment">Human-readable environment label.</param>
/// <param name="LocationLabel">Non-secret Agent placement label.</param>
/// <param name="Status">Canonical provider-neutral health-status wire identifier.</param>
/// <param name="ObservedAt">UTC instant reported by the observation source.</param>
/// <param name="ReceivedAt">Authoritative UTC Server receipt instant.</param>
/// <param name="LatencyMilliseconds">Optional bounded observation duration.</param>
/// <param name="Enabled">Whether monitoring remains enabled for the instance.</param>
public sealed record DesktopFleetApiItem(
    Guid InstanceId,
    string DisplayName,
    string ProviderType,
    string SupportLabel,
    string Environment,
    string LocationLabel,
    string Status,
    DateTimeOffset ObservedAt,
    DateTimeOffset ReceivedAt,
    long? LatencyMilliseconds,
    bool Enabled);

/// <summary>Contains one complete immutable Desktop Fleet read response.</summary>
/// <param name="SchemaVersion">Exact versioned wire schema.</param>
/// <param name="GeneratedAt">UTC instant at which the authorised projection was generated.</param>
/// <param name="Items">Bounded latest observations visible to the authenticated human actor.</param>
public sealed record DesktopFleetApiSnapshot(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<DesktopFleetApiItem> Items);

/// <summary>Converts untrusted Desktop Fleet wire values into the canonical presentation snapshot without provider branching.</summary>
public static class DesktopFleetApiSnapshotMapper
{
    /// <summary>Maps an admitted API response to one immutable inventory snapshot.</summary>
    /// <param name="source">Untrusted response material produced outside the Application boundary.</param>
    /// <param name="receivedAt">Current UTC client instant used to reject future Server evidence.</param>
    /// <param name="snapshot">Mapped snapshot when the complete response is valid; otherwise null.</param>
    /// <returns><see langword="true"/> only when every field satisfies the current contract.</returns>
    public static bool TryMap(
        DesktopFleetApiSnapshot? source,
        DateTimeOffset receivedAt,
        out InventorySnapshot? snapshot)
    {
        snapshot = null;
        if (source is null ||
            !string.Equals(source.SchemaVersion, DesktopFleetApiContract.CurrentSchemaVersion, StringComparison.Ordinal) ||
            source.GeneratedAt.Offset != TimeSpan.Zero ||
            source.GeneratedAt > receivedAt ||
            source.Items is null ||
            source.Items.Count > DesktopFleetReconciliationCoordinator.MaximumSnapshotItems)
        {
            return false;
        }

        HashSet<Guid> identifiers = [];
        List<InstanceInventoryItem> items = new(source.Items.Count);
        foreach (DesktopFleetApiItem? item in source.Items)
        {
            if (item is null ||
                item.InstanceId == Guid.Empty ||
                !identifiers.Add(item.InstanceId) ||
                !IsBoundedText(item.DisplayName, 160) ||
                !IsBoundedText(item.ProviderType, 96) ||
                !IsBoundedText(item.SupportLabel, 160) ||
                !IsBoundedText(item.Environment, 160) ||
                !IsBoundedText(item.LocationLabel, 240) ||
                !TryMapStatus(item.Status, out HealthStatus status) ||
                item.ObservedAt.Offset != TimeSpan.Zero ||
                item.ReceivedAt.Offset != TimeSpan.Zero ||
                item.ObservedAt > item.ReceivedAt ||
                item.ReceivedAt > source.GeneratedAt ||
                item.LatencyMilliseconds is < 0 or > 86_400_000)
            {
                return false;
            }

            items.Add(new InstanceInventoryItem(
                item.InstanceId,
                item.DisplayName,
                item.ProviderType,
                item.SupportLabel,
                item.Environment,
                item.LocationLabel,
                status,
                item.ObservedAt,
                item.ReceivedAt,
                item.LatencyMilliseconds is long latency ? TimeSpan.FromMilliseconds(latency) : null,
                item.Enabled));
        }

        snapshot = new InventorySnapshot(
            InventorySnapshot.CurrentSchemaVersion,
            source.GeneratedAt,
            items.ToArray());
        return true;
    }

    /// <summary>Maps one exact provider-neutral status identifier without accepting aliases or provider-native values.</summary>
    private static bool TryMapStatus(string? value, out HealthStatus status)
    {
        status = value switch
        {
            "healthy" => HealthStatus.Healthy,
            "degraded" => HealthStatus.Degraded,
            "unavailable" => HealthStatus.Unavailable,
            "authFailed" => HealthStatus.AuthFailed,
            "timeout" => HealthStatus.Timeout,
            "maintenance" => HealthStatus.Maintenance,
            "unknown" => HealthStatus.Unknown,
            _ => (HealthStatus)(-1),
        };
        return Enum.IsDefined(status);
    }

    /// <summary>Rejects missing, peripheral-whitespace, control-bearing or oversized wire text.</summary>
    private static bool IsBoundedText(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= maximumLength &&
        string.Equals(value, value.Trim(), StringComparison.Ordinal) &&
        !value.Any(character =>
            char.IsControl(character) ||
            char.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.Format);
}
