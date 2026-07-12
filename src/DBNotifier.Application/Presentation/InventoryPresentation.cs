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
    public bool IsStale(DateTimeOffset now, TimeSpan staleAfter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(staleAfter, TimeSpan.Zero);
        return now - ReceivedAt > staleAfter;
    }
}

public sealed record InventorySnapshot(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<InstanceInventoryItem> Items)
{
    public const string CurrentSchemaVersion = "inventory.v1";

    public InventoryStatusSummary Summarize(DateTimeOffset now, TimeSpan staleAfter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(staleAfter, TimeSpan.Zero);
        IReadOnlyList<InstanceInventoryItem> items = Items ?? [];
        return new InventoryStatusSummary(
            items.Count,
            items.Count(item => item.Status == HealthStatus.Healthy && !item.IsStale(now, staleAfter)),
            items.Count(item => item.Status == HealthStatus.Degraded && !item.IsStale(now, staleAfter)),
            items.Count(item => item.Status is HealthStatus.Unavailable or HealthStatus.AuthFailed or HealthStatus.Timeout),
            items.Count(item => item.IsStale(now, staleAfter)));
    }
}

public sealed record InventoryStatusSummary(
    int Total,
    int Healthy,
    int Degraded,
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
