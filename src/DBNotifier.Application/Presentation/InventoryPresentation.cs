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
