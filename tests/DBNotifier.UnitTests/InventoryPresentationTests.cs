// Module purpose: Verifies Inventory Presentation Tests behaviour and protects the documented project contract.
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
        Assert.Equal(1, summary.Warning);
        Assert.Equal(1, summary.AttentionRequired);
        Assert.Equal(1, summary.Stale);
    }

    [Fact]
    public void SummaryExcludesEveryStaleHealthClassAndIncludesCurrentMaintenanceAsWarning()
    {
        InventorySnapshot snapshot = new(
            InventorySnapshot.CurrentSchemaVersion,
            Now,
            [
                CreateItem(HealthStatus.Healthy, Now.AddMinutes(-8)),
                CreateItem(HealthStatus.Degraded, Now.AddMinutes(-8)),
                CreateItem(HealthStatus.Timeout, Now.AddMinutes(-8)),
                CreateItem(HealthStatus.Maintenance, Now.AddMinutes(-2)),
            ]);

        InventoryStatusSummary summary = snapshot.Summarize(Now, TimeSpan.FromMinutes(5));

        Assert.Equal(0, summary.Healthy);
        Assert.Equal(0, summary.Degraded);
        Assert.Equal(1, summary.Warning);
        Assert.Equal(0, summary.AttentionRequired);
        Assert.Equal(3, summary.Stale);
    }

    /// <summary>Verifies that disabled evidence remains inventory truth without contributing to health or stale conclusions.</summary>
    [Fact]
    public void SummarySeparatesDisabledItemsFromCurrentHealthAndFreshness()
    {
        InventorySnapshot snapshot = new(
            InventorySnapshot.CurrentSchemaVersion,
            Now,
            [
                CreateItem(HealthStatus.Healthy, Now) with { Enabled = false },
                CreateItem(HealthStatus.Timeout, Now.AddMinutes(-10)) with { Enabled = false },
                CreateItem(HealthStatus.Healthy, Now),
            ]);

        InventoryStatusSummary summary = snapshot.Summarize(Now, TimeSpan.FromMinutes(5));
        TrayFleetSummary tray = TrayFleetPresentationPolicy.Summarise(snapshot, Now, TimeSpan.FromMinutes(5));

        Assert.Equal(3, summary.Total);
        Assert.Equal(1, summary.Healthy);
        Assert.Equal(0, summary.AttentionRequired);
        Assert.Equal(0, summary.Stale);
        Assert.Equal(2, summary.Disabled);
        Assert.Equal(TrayAggregateState.Healthy, tray.State);
        Assert.Equal(3, tray.TotalCount);
        Assert.Equal(2, tray.DisabledCount);
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

    [Fact]
    public void FutureOrReversedEvidenceFailsToUnknownFreshness()
    {
        InstanceInventoryItem future = CreateItem(HealthStatus.Healthy, Now.AddSeconds(1));
        InstanceInventoryItem reversed = CreateItem(HealthStatus.Healthy, Now) with
        {
            ObservedAt = Now.AddSeconds(1),
        };

        Assert.Equal(EvidenceFreshness.Unknown, future.GetFreshness(Now, TimeSpan.FromMinutes(5)));
        Assert.Equal(EvidenceFreshness.Unknown, reversed.GetFreshness(Now, TimeSpan.FromMinutes(5)));
        Assert.False(future.IsStale(Now, TimeSpan.FromMinutes(5)));
    }

    /// <summary>Verifies that Tray and detailed inventory age the same immutable evidence without moving its generated instant.</summary>
    [Fact]
    public void FleetSummaryAgesTheImmutableInventorySnapshotToUnknown()
    {
        InventorySnapshot snapshot = new(
            InventorySnapshot.CurrentSchemaVersion,
            Now,
            [
                CreateItem(HealthStatus.Healthy, Now.AddMinutes(-1)),
                CreateItem(HealthStatus.Degraded, Now.AddMinutes(-2)),
                CreateItem(HealthStatus.Timeout, Now.AddMinutes(-3)),
            ]);

        TrayFleetSummary initial = TrayFleetPresentationPolicy.Summarise(snapshot, Now, TimeSpan.FromMinutes(5));
        TrayFleetSummary aged = TrayFleetPresentationPolicy.Summarise(snapshot, Now.AddMinutes(10), TimeSpan.FromMinutes(5));

        Assert.Equal(TrayAggregateState.Critical, initial.State);
        Assert.Equal(1, initial.HealthyCount);
        Assert.Equal(1, initial.WarningCount);
        Assert.Equal(1, initial.CriticalCount);
        Assert.Equal(TrayAggregateState.Unknown, aged.State);
        Assert.Equal(3, aged.UnknownCount);
        Assert.Equal(Now, snapshot.GeneratedAt);
    }

    [Fact]
    public void AlertSummaryKeepsResolvedCriticalOutOfUnresolvedCount()
    {
        TimelineAlertSnapshot snapshot = new(
            TimelineAlertSnapshot.CurrentSchemaVersion,
            Now,
            [],
            [
                CreateAlert(EventSeverity.Critical, AlertPresentationState.Active),
                CreateAlert(EventSeverity.Warning, AlertPresentationState.Acknowledged),
                CreateAlert(EventSeverity.Information, AlertPresentationState.Silenced),
                CreateAlert(EventSeverity.Critical, AlertPresentationState.Resolved),
            ]);

        Assert.Equal(new AlertStatusSummary(4, 1, 1, 1, 1), snapshot.Summarize());
    }

    [Fact]
    public void TimelineStaleBoundaryMatchesInventoryPolicy()
    {
        TimelineEventItem item = new(
            Guid.NewGuid(), Guid.NewGuid(), "Instance", "sample-provider", "Degraded",
            EventSeverity.Warning, "Safe summary", Now.AddMinutes(-5), Now.AddMinutes(-5));

        Assert.False(item.IsStale(Now, TimeSpan.FromMinutes(5)));
        Assert.True(item.IsStale(Now.AddTicks(1), TimeSpan.FromMinutes(5)));
    }

    [Theory]
    [InlineData("service.start", true, ActionPreviewDisposition.ConfirmationRequired)]
    [InlineData("service.start", false, ActionPreviewDisposition.Denied)]
    [InlineData("service.stop", true, ActionPreviewDisposition.Unsupported)]
    [InlineData("service.restart", true, ActionPreviewDisposition.Unavailable)]
    [InlineData("missing", true, ActionPreviewDisposition.Unknown)]
    public void CapabilityPreviewFailsClosedWithoutExecuting(
        string capabilityId,
        bool authorized,
        ActionPreviewDisposition expected)
    {
        ConfigurationCapabilitySnapshot snapshot = new(
            ConfigurationCapabilitySnapshot.CurrentSchemaVersion,
            Guid.NewGuid(),
            "Fixture",
            "sample-provider",
            [],
            [
                new("service.start", "Start", CapabilityPresentationState.Supported, "fixture.confirm", true),
                new("service.stop", "Stop", CapabilityPresentationState.Unsupported, "provider.unsupported", true),
                new("service.restart", "Restart", CapabilityPresentationState.Unavailable, "agent.offline", true),
            ]);

        Assert.Equal(expected, snapshot.Preview(capabilityId, authorized));
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

    private static AlertPresentationItem CreateAlert(
        EventSeverity severity,
        AlertPresentationState state) =>
        new(
            Guid.NewGuid(), Guid.NewGuid(), "Instance", "sample-provider", severity, state,
            "Rule", "Safe summary", Now.AddMinutes(-2), Now.AddMinutes(-1));
}
