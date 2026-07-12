using DBNotifier.Application.Presentation;
using DBNotifier.Domain;

namespace DBNotifier.UnitTests;

public sealed class InventoryPresentationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SummaryKeepsStaleHealthyOutOfFreshHealthyCount()
    {
        InventorySnapshot snapshot = new(
            InventorySnapshot.CurrentSchemaVersion,
            Now,
            [
                CreateItem(HealthStatus.Healthy, Now.AddMinutes(-1)),
                CreateItem(HealthStatus.Healthy, Now.AddMinutes(-8)),
                CreateItem(HealthStatus.Degraded, Now.AddMinutes(-2)),
                CreateItem(HealthStatus.Timeout, Now.AddMinutes(-3)),
            ]);

        InventoryStatusSummary summary = snapshot.Summarize(Now, TimeSpan.FromMinutes(5));

        Assert.Equal(4, summary.Total);
        Assert.Equal(1, summary.Healthy);
        Assert.Equal(1, summary.Degraded);
        Assert.Equal(1, summary.AttentionRequired);
        Assert.Equal(1, summary.Stale);
    }

    [Fact]
    public void StaleBoundaryIsNotPremature()
    {
        InstanceInventoryItem item = CreateItem(HealthStatus.Healthy, Now.AddMinutes(-5));

        Assert.False(item.IsStale(Now, TimeSpan.FromMinutes(5)));
        Assert.True(item.IsStale(Now.AddTicks(1), TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void NonPositiveStalePolicyIsRejected()
    {
        InstanceInventoryItem item = CreateItem(HealthStatus.Unknown, Now);

        Assert.Throws<ArgumentOutOfRangeException>(() => item.IsStale(Now, TimeSpan.Zero));
    }

    private static InstanceInventoryItem CreateItem(HealthStatus status, DateTimeOffset receivedAt) =>
        new(
            Guid.NewGuid(),
            "Database instance",
            "sample-provider",
            "Fixture only",
            "Test",
            "Local fixture",
            status,
            receivedAt.AddSeconds(-1),
            receivedAt,
            TimeSpan.FromMilliseconds(12),
            true);
}
