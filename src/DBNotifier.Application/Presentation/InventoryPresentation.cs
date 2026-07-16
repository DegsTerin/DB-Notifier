// Module purpose: Defines provider-neutral inventory and capability presentation contracts without depending on a user interface.
using DBNotifier.Domain;

namespace DBNotifier.Application.Presentation;

public enum InventorySurfaceState
{
    Loading,
    Ready,
    Empty,
    Offline,
    Error,
    Denied,
    Maintenance,
}

/// <summary>Classifies whether timestamped presentation evidence can support a current operational conclusion.</summary>
public enum EvidenceFreshness
{
    /// <summary>The timestamp ordering is valid and the receipt remains inside the freshness window.</summary>
    Current,

    /// <summary>The timestamp ordering is valid but the receipt has exceeded the freshness window.</summary>
    Stale,

    /// <summary>The timestamps are invalid for a current conclusion, including evidence reported from the future.</summary>
    Unknown,
}

/// <summary>Contains one immutable inventory item and the evidence timestamps needed for factual freshness evaluation.</summary>
/// <param name="InstanceId">Stable database-instance identifier.</param>
/// <param name="DisplayName">User-facing instance name.</param>
/// <param name="ProviderType">Stable provider identifier without provider-native state.</param>
/// <param name="SupportLabel">Factual support or lifecycle label.</param>
/// <param name="Environment">User-facing environment label.</param>
/// <param name="LocationLabel">Non-secret location label.</param>
/// <param name="Status">Provider-neutral health status reported by the evidence.</param>
/// <param name="ObservedAt">UTC instant reported by the observation source.</param>
/// <param name="ReceivedAt">Authoritative UTC receipt instant.</param>
/// <param name="Latency">Optional bounded probe latency.</param>
/// <param name="Enabled">Whether monitoring is enabled for the instance.</param>
public sealed record InstanceInventoryItem(
    Guid InstanceId,
    string DisplayName,
    string ProviderType,
    string SupportLabel,
    string Environment,
    string LocationLabel,
    HealthStatus Status,
    DateTimeOffset ObservedAt,
    DateTimeOffset ReceivedAt,
    TimeSpan? Latency,
    bool Enabled)
{
    /// <summary>Classifies timestamp ordering and age without allowing future evidence to appear current.</summary>
    /// <param name="now">Current UTC instant used for evaluation.</param>
    /// <param name="staleAfter">Strictly positive maximum current-evidence age.</param>
    /// <returns>Current, stale or Unknown according to the canonical presentation policy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="staleAfter"/> is not positive.</exception>
    public EvidenceFreshness GetFreshness(DateTimeOffset now, TimeSpan staleAfter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(staleAfter, TimeSpan.Zero);
        if (ObservedAt > ReceivedAt || ObservedAt > now || ReceivedAt > now)
        {
            return EvidenceFreshness.Unknown;
        }

        return now - ReceivedAt > staleAfter ? EvidenceFreshness.Stale : EvidenceFreshness.Current;
    }

    /// <summary>Returns whether valid evidence has exceeded the configured freshness threshold.</summary>
    /// <param name="now">Current UTC instant used for evaluation.</param>
    /// <param name="staleAfter">Strictly positive maximum current-evidence age.</param>
    /// <returns><see langword="true"/> only for valid stale evidence; invalid or future evidence is Unknown.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="staleAfter"/> is not positive.</exception>
    public bool IsStale(DateTimeOffset now, TimeSpan staleAfter)
    {
        return GetFreshness(now, staleAfter) == EvidenceFreshness.Stale;
    }
}

/// <summary>Contains one immutable set of provider-neutral inventory evidence and its generation instant.</summary>
/// <param name="SchemaVersion">Versioned presentation schema identifier.</param>
/// <param name="GeneratedAt">UTC instant at which the snapshot was generated.</param>
/// <param name="Items">Bounded inventory items whose timestamps remain immutable while freshness is re-evaluated.</param>
public sealed record InventorySnapshot(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<InstanceInventoryItem> Items)
{
    public const string CurrentSchemaVersion = "inventory.v1";

    /// <summary>Summarises only current evidence into health KPIs while retaining stale counts separately.</summary>
    /// <param name="now">Current UTC instant used only to evaluate evidence freshness.</param>
    /// <param name="staleAfter">Strictly positive maximum current-evidence age.</param>
    /// <returns>Coherent counts in which stale or invalid evidence cannot imply a current health outcome.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="staleAfter"/> is not positive.</exception>
    public InventoryStatusSummary Summarize(DateTimeOffset now, TimeSpan staleAfter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(staleAfter, TimeSpan.Zero);
        IReadOnlyList<InstanceInventoryItem> items = Items ?? [];
        bool IsCurrent(InstanceInventoryItem item) => item.GetFreshness(now, staleAfter) == EvidenceFreshness.Current;
        return new InventoryStatusSummary(
            items.Count,
            items.Count(item => item.Status == HealthStatus.Healthy && IsCurrent(item)),
            items.Count(item => item.Status == HealthStatus.Degraded && IsCurrent(item)),
            items.Count(item => (item.Status is HealthStatus.Degraded or HealthStatus.Maintenance) && IsCurrent(item)),
            items.Count(item => (item.Status is HealthStatus.Unavailable or HealthStatus.AuthFailed or HealthStatus.Timeout) && IsCurrent(item)),
            items.Count(item => item.IsStale(now, staleAfter)));
    }
}

/// <summary>Contains current-evidence inventory KPI counts and the independent stale total.</summary>
/// <param name="Total">Total number of inventory items regardless of freshness.</param>
/// <param name="Healthy">Current healthy evidence count.</param>
/// <param name="Degraded">Current degraded evidence count.</param>
/// <param name="Warning">Current degraded or maintenance evidence count.</param>
/// <param name="AttentionRequired">Current unavailable, authentication-failed or timeout evidence count.</param>
/// <param name="Stale">Valid evidence count older than the configured freshness threshold.</param>
public sealed record InventoryStatusSummary(
    int Total,
    int Healthy,
    int Degraded,
    int Warning,
    int AttentionRequired,
    int Stale);

public enum EventSeverity
{
    Information,
    Warning,
    Critical,
}

public enum AlertPresentationState
{
    Active,
    Acknowledged,
    Silenced,
    Resolved,
}

public sealed record TimelineEventItem(
    Guid EventId,
    Guid InstanceId,
    string InstanceName,
    string ProviderType,
    string EventType,
    EventSeverity Severity,
    string Summary,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt)
{
    public bool IsStale(DateTimeOffset now, TimeSpan staleAfter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(staleAfter, TimeSpan.Zero);
        return now - ReceivedAt > staleAfter;
    }
}

public sealed record AlertPresentationItem(
    Guid AlertId,
    Guid InstanceId,
    string InstanceName,
    string ProviderType,
    EventSeverity Severity,
    AlertPresentationState State,
    string RuleName,
    string Summary,
    DateTimeOffset OpenedAt,
    DateTimeOffset UpdatedAt);

public sealed record TimelineAlertSnapshot(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<TimelineEventItem> Events,
    IReadOnlyList<AlertPresentationItem> Alerts)
{
    public const string CurrentSchemaVersion = "history-alerts.v1";

    public AlertStatusSummary Summarize() =>
        new(
            Alerts.Count,
            Alerts.Count(alert => alert.State == AlertPresentationState.Active),
            Alerts.Count(alert => alert.State == AlertPresentationState.Acknowledged),
            Alerts.Count(alert => alert.State == AlertPresentationState.Silenced),
            Alerts.Count(alert => alert.Severity == EventSeverity.Critical && alert.State != AlertPresentationState.Resolved));
}

public sealed record AlertStatusSummary(
    int Total,
    int Active,
    int Acknowledged,
    int Silenced,
    int UnresolvedCritical);

public enum CapabilityPresentationState
{
    Supported,
    Unsupported,
    Unavailable,
    Unknown,
}

public enum ActionPreviewDisposition
{
    ConfirmationRequired,
    Denied,
    Unsupported,
    Unavailable,
    Unknown,
}

public sealed record ConfigurationFieldPresentation(
    string Key,
    string Label,
    string SafeValue,
    string Description);

public sealed record CapabilityPresentation(
    string CapabilityId,
    string DisplayName,
    CapabilityPresentationState State,
    string ReasonCode,
    bool RequiresConfirmation);

public sealed record ConfigurationCapabilitySnapshot(
    string SchemaVersion,
    Guid InstanceId,
    string InstanceName,
    string ProviderType,
    IReadOnlyList<ConfigurationFieldPresentation> Fields,
    IReadOnlyList<CapabilityPresentation> Capabilities)
{
    public const string CurrentSchemaVersion = "configuration-capabilities.v1";

    public ActionPreviewDisposition Preview(string capabilityId, bool authorized)
    {
        CapabilityPresentation? capability = Capabilities.FirstOrDefault(item =>
            string.Equals(item.CapabilityId, capabilityId, StringComparison.Ordinal));
        if (capability is null)
        {
            return ActionPreviewDisposition.Unknown;
        }

        if (!authorized)
        {
            return ActionPreviewDisposition.Denied;
        }

        return capability.State switch
        {
            CapabilityPresentationState.Supported when capability.RequiresConfirmation =>
                ActionPreviewDisposition.ConfirmationRequired,
            CapabilityPresentationState.Unsupported => ActionPreviewDisposition.Unsupported,
            CapabilityPresentationState.Unavailable => ActionPreviewDisposition.Unavailable,
            _ => ActionPreviewDisposition.Unknown,
        };
    }
}
