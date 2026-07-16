// Module purpose: Verifies Tray Presentation Tests behaviour and protects the documented project contract.
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;

namespace DBNotifier.UnitTests;

/// <summary>Verifies provider-neutral notification-area presentation policies without invoking Windows delivery.</summary>
public sealed class TrayPresentationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 15, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies that normal startup remains hidden and only the exact review switch reveals the desktop shell.</summary>
    /// <param name="expected">The startup mode expected for the supplied arguments.</param>
    /// <param name="arguments">The command-line arguments under test.</param>
    [Theory]
    [InlineData(TrayStartupMode.NotificationArea)]
    [InlineData(TrayStartupMode.ShowDesktop, "--show-desktop")]
    [InlineData(TrayStartupMode.ShowDesktop, "--SHOW-DESKTOP")]
    [InlineData(TrayStartupMode.NotificationArea, "--unknown")]
    public void StartupDefaultsToNotificationAreaAndRequiresExactDesktopReviewSwitch(
        TrayStartupMode expected,
        params string[] arguments)
    {
        Assert.Equal(expected, TrayStartupPolicy.Resolve(arguments));
    }

    /// <summary>Verifies safe Tray action mapping and limits repeated availability confirmation to explicit close requests.</summary>
    /// <param name="intent">The window intention under test.</param>
    /// <param name="expected">The presentation action expected for the intention.</param>
    /// <param name="shouldRequestAvailabilityConfirmation">Whether the intention must request a fresh local confirmation.</param>
    [Theory]
    [InlineData(TrayWindowIntent.Minimize, TrayWindowAction.HideToTray, false)]
    [InlineData(TrayWindowIntent.CloseRequest, TrayWindowAction.HideToTray, true)]
    [InlineData(TrayWindowIntent.Show, TrayWindowAction.ShowAndActivate, false)]
    [InlineData(TrayWindowIntent.Exit, TrayWindowAction.ExitApplication, false)]
    public void WindowIntentMapsToExplicitTrayAction(
        TrayWindowIntent intent,
        TrayWindowAction expected,
        bool shouldRequestAvailabilityConfirmation)
    {
        Assert.Equal(expected, TrayPresentationPolicy.Resolve(intent));
        Assert.Equal(
            shouldRequestAvailabilityConfirmation,
            TrayPresentationPolicy.ShouldRequestAvailabilityConfirmation(intent));
    }

    /// <summary>Verifies that invalid window intentions cannot silently request a presentation action or notification.</summary>
    [Fact]
    public void InvalidWindowIntentIsRejectedByPresentationAndConfirmationPolicies()
    {
        TrayWindowIntent invalid = (TrayWindowIntent)int.MaxValue;

        Assert.Throws<ArgumentOutOfRangeException>(() => TrayPresentationPolicy.Resolve(invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => TrayPresentationPolicy.ShouldRequestAvailabilityConfirmation(invalid));
    }

    /// <summary>Verifies that confirmation is stateless and remains requested after the shell is shown and closed repeatedly.</summary>
    [Fact]
    public void EveryExplicitCloseRequestsANewAvailabilityConfirmation()
    {
        TrayWindowIntent[] repeatedSequence =
        [
            TrayWindowIntent.Show,
            TrayWindowIntent.CloseRequest,
            TrayWindowIntent.Show,
            TrayWindowIntent.CloseRequest,
        ];

        Assert.Equal(
            [false, true, false, true],
            repeatedSequence.Select(TrayPresentationPolicy.ShouldRequestAvailabilityConfirmation));
    }

    [Fact]
    public void DemonstrationStatusDoesNotClaimExternalData()
    {
        TrayStatusPresentation status = new(
            "Demonstração local",
            "Sem dados externos",
            "Nenhum provider homologado",
            UsesExternalData: false);

        Assert.False(status.UsesExternalData);
        Assert.Contains("externos", status.FreshnessLabel, StringComparison.Ordinal);
    }

    /// <summary>Verifies provider-neutral status mapping, freshness protection and severity precedence.</summary>
    [Fact]
    public void FleetSummaryClassifiesSignalsAndUsesCriticalPrecedence()
    {
        TrayFleetSummary summary = TrayFleetPresentationPolicy.Summarise(
        [
            new(HealthStatus.Healthy, IsStale: false),
            new(HealthStatus.Degraded, IsStale: false),
            new(HealthStatus.Timeout, IsStale: false),
            new(HealthStatus.Healthy, IsStale: true),
        ]);

        Assert.Equal(TrayAggregateState.Critical, summary.State);
        Assert.Equal(4, summary.TotalCount);
        Assert.Equal(1, summary.HealthyCount);
        Assert.Equal(1, summary.WarningCount);
        Assert.Equal(1, summary.CriticalCount);
        Assert.Equal(1, summary.UnknownCount);
    }

    /// <summary>Verifies that absence of evidence remains unknown rather than silently healthy.</summary>
    [Fact]
    public void EmptyFleetSummaryIsUnknown()
    {
        TrayFleetSummary summary = TrayFleetPresentationPolicy.Summarise([]);

        Assert.Equal(TrayAggregateState.Unknown, summary.State);
        Assert.Equal(0, summary.TotalCount);
    }

    /// <summary>Verifies that each notification meaning selects its corresponding semantic bell without inferring fleet health.</summary>
    /// <param name="meaning">The provider-neutral meaning conveyed by the notification.</param>
    /// <param name="expected">The shared icon state expected for that meaning.</param>
    [Theory]
    [InlineData(TrayNotificationMeaning.AvailabilityOrRecovery, TrayAggregateState.Healthy)]
    [InlineData(TrayNotificationMeaning.Warning, TrayAggregateState.Warning)]
    [InlineData(TrayNotificationMeaning.Critical, TrayAggregateState.Critical)]
    [InlineData(TrayNotificationMeaning.InformationalOrUnknown, TrayAggregateState.Unknown)]
    [InlineData((TrayNotificationMeaning)int.MaxValue, TrayAggregateState.Unknown)]
    public void NotificationMeaningSelectsCorrespondingSemanticIcon(
        TrayNotificationMeaning meaning,
        TrayAggregateState expected)
    {
        Assert.Equal(expected, TrayNotificationPresentationPolicy.ResolveIconState(meaning));
    }

    /// <summary>Verifies that current status and freshness select the meaning of the notification itself.</summary>
    [Fact]
    public void EffectiveStateSelectsNotificationMeaningAndStaleFailsToUnknown()
    {
        Guid instanceId = Guid.Parse("05000000-0000-0000-0000-000000000001");

        Assert.Equal(
            TrayNotificationMeaning.AvailabilityOrRecovery,
            TrayNotificationPresentationPolicy.ResolveMeaning(new(instanceId, HealthStatus.Healthy, EvidenceFreshness.Current)));
        Assert.Equal(
            TrayNotificationMeaning.Warning,
            TrayNotificationPresentationPolicy.ResolveMeaning(new(instanceId, HealthStatus.Degraded, EvidenceFreshness.Current)));
        Assert.Equal(
            TrayNotificationMeaning.Warning,
            TrayNotificationPresentationPolicy.ResolveMeaning(new(instanceId, HealthStatus.Maintenance, EvidenceFreshness.Current)));
        Assert.Equal(
            TrayNotificationMeaning.Critical,
            TrayNotificationPresentationPolicy.ResolveMeaning(new(instanceId, HealthStatus.Unavailable, EvidenceFreshness.Current)));
        Assert.Equal(
            TrayNotificationMeaning.Critical,
            TrayNotificationPresentationPolicy.ResolveMeaning(new(instanceId, HealthStatus.AuthFailed, EvidenceFreshness.Current)));
        Assert.Equal(
            TrayNotificationMeaning.Critical,
            TrayNotificationPresentationPolicy.ResolveMeaning(new(instanceId, HealthStatus.Timeout, EvidenceFreshness.Current)));
        Assert.Equal(
            TrayNotificationMeaning.InformationalOrUnknown,
            TrayNotificationPresentationPolicy.ResolveMeaning(new(instanceId, HealthStatus.Unknown, EvidenceFreshness.Current)));
        Assert.Equal(
            TrayNotificationMeaning.InformationalOrUnknown,
            TrayNotificationPresentationPolicy.ResolveMeaning(new(instanceId, HealthStatus.Healthy, EvidenceFreshness.Stale)));
        Assert.Equal(
            TrayNotificationMeaning.InformationalOrUnknown,
            TrayNotificationPresentationPolicy.ResolveMeaning(new(instanceId, HealthStatus.Healthy, (EvidenceFreshness)int.MaxValue)));
        Assert.Equal(
            TrayNotificationMeaning.InformationalOrUnknown,
            TrayNotificationPresentationPolicy.ResolveMeaning(new(instanceId, (HealthStatus)int.MaxValue, EvidenceFreshness.Current)));
    }

    /// <summary>Verifies that every completion path ends the bounded semantic-icon lease and invalid input fails to the aggregate.</summary>
    [Fact]
    public void NotificationIconLeaseIsBoundedAcrossShownFallbackFailureAndDisposal()
    {
        Assert.Equal(TimeSpan.FromSeconds(2), TrayNotificationIconLeasePolicy.FallbackDelay);

        TrayNotificationIconLeaseState active = TrayNotificationIconLeasePolicy.Resolve(
            TrayNotificationIconLeaseState.Aggregate,
            TrayNotificationIconLeaseSignal.Begin);

        Assert.Equal(TrayNotificationIconLeaseState.NotificationMeaning, active);
        Assert.Equal(
            TrayNotificationIconLeaseState.NotificationMeaning,
            TrayNotificationIconLeasePolicy.Resolve(active, TrayNotificationIconLeaseSignal.Begin));

        foreach (TrayNotificationIconLeaseSignal completion in new[]
        {
            TrayNotificationIconLeaseSignal.BalloonShown,
            TrayNotificationIconLeaseSignal.FallbackElapsed,
            TrayNotificationIconLeaseSignal.DeliveryFailed,
            TrayNotificationIconLeaseSignal.Disposed,
        })
        {
            Assert.Equal(
                TrayNotificationIconLeaseState.Aggregate,
                TrayNotificationIconLeasePolicy.Resolve(active, completion));
            Assert.Equal(
                TrayNotificationIconLeaseState.Aggregate,
                TrayNotificationIconLeasePolicy.Resolve(TrayNotificationIconLeaseState.Aggregate, completion));
        }

        Assert.Equal(
            TrayNotificationIconLeaseState.Aggregate,
            TrayNotificationIconLeasePolicy.Resolve(
                (TrayNotificationIconLeaseState)int.MaxValue,
                TrayNotificationIconLeaseSignal.Begin));
        Assert.Equal(
            TrayNotificationIconLeaseState.Aggregate,
            TrayNotificationIconLeasePolicy.Resolve(
                active,
                (TrayNotificationIconLeaseSignal)int.MaxValue));
    }

    /// <summary>Verifies that the first captured instance state establishes a silent baseline.</summary>
    [Fact]
    public void InitialEffectiveStateCaptureCreatesNoChanges()
    {
        InventorySnapshot snapshot = CreateSnapshot(
            CreateItem(Guid.Parse("10000000-0000-0000-0000-000000000001"), HealthStatus.Healthy, Now));
        IReadOnlyList<TrayInstanceEffectiveState> current = TrayInstanceStateChangePolicy.Capture(
            snapshot,
            Now,
            TimeSpan.FromMinutes(5));

        Assert.Empty(TrayInstanceStateChangePolicy.DetectChanges(null, current));
    }

    /// <summary>Verifies that repeated evaluation at the same instant does not invent an instance change.</summary>
    [Fact]
    public void SameEffectiveStateEvaluationInstantCreatesNoChanges()
    {
        InventorySnapshot snapshot = CreateSnapshot(
            CreateItem(Guid.Parse("20000000-0000-0000-0000-000000000001"), HealthStatus.Degraded, Now.AddMinutes(-2)));
        IReadOnlyList<TrayInstanceEffectiveState> previous = TrayInstanceStateChangePolicy.Capture(
            snapshot,
            Now,
            TimeSpan.FromMinutes(5));
        IReadOnlyList<TrayInstanceEffectiveState> current = TrayInstanceStateChangePolicy.Capture(
            snapshot,
            Now,
            TimeSpan.FromMinutes(5));

        Assert.Empty(TrayInstanceStateChangePolicy.DetectChanges(previous, current));
        TrayInstanceEffectiveState added = new(
            Guid.Parse("20000000-0000-0000-0000-000000000002"),
            HealthStatus.Healthy,
            EvidenceFreshness.Current);
        Assert.Empty(TrayInstanceStateChangePolicy.DetectChanges(previous, current.Append(added)));
        Assert.Empty(TrayInstanceStateChangePolicy.DetectChanges(current.Append(added), current));
        Assert.Throws<ArgumentException>(() => TrayInstanceStateChangePolicy.DetectChanges(
            previous.Append(previous[0]),
            current));
    }

    /// <summary>Verifies that simultaneous freshness changes remain individual, stable and independent of the fleet aggregate.</summary>
    [Fact]
    public void FreshnessChangesRemainIndividualWhenAggregateDoesNotChange()
    {
        Guid first = Guid.Parse("30000000-0000-0000-0000-000000000001");
        Guid second = Guid.Parse("30000000-0000-0000-0000-000000000002");
        Guid unchangedCritical = Guid.Parse("30000000-0000-0000-0000-000000000003");
        TimeSpan staleAfter = TimeSpan.FromMinutes(5);
        InventorySnapshot snapshot = CreateSnapshot(
            CreateItem(second, HealthStatus.Degraded, Now.Subtract(staleAfter)),
            CreateItem(unchangedCritical, HealthStatus.Timeout, Now),
            CreateItem(first, HealthStatus.Healthy, Now.Subtract(staleAfter)));

        IReadOnlyList<TrayInstanceEffectiveState> previous = TrayInstanceStateChangePolicy.Capture(snapshot, Now, staleAfter);
        IReadOnlyList<TrayInstanceEffectiveState> current = TrayInstanceStateChangePolicy.Capture(snapshot, Now.AddTicks(1), staleAfter);
        IReadOnlyList<TrayInstanceStateChange> changes = TrayInstanceStateChangePolicy.DetectChanges(previous, current);

        Assert.Equal(TrayAggregateState.Critical, TrayFleetPresentationPolicy.Summarise(snapshot, Now, staleAfter).State);
        Assert.Equal(TrayAggregateState.Critical, TrayFleetPresentationPolicy.Summarise(snapshot, Now.AddTicks(1), staleAfter).State);
        Assert.Equal([first, second], changes.Select(change => change.InstanceId));
        Assert.All(changes, change => Assert.Equal(EvidenceFreshness.Current, change.Previous.Freshness));
        Assert.All(changes, change => Assert.Equal(EvidenceFreshness.Stale, change.Current.Freshness));
    }

    /// <summary>Verifies that health status participates in effective state even when freshness remains unchanged.</summary>
    [Fact]
    public void HealthStatusChangeIsDetectedWithUnchangedFreshness()
    {
        Guid instanceId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        IReadOnlyList<TrayInstanceEffectiveState> previous = TrayInstanceStateChangePolicy.Capture(
            CreateSnapshot(CreateItem(instanceId, HealthStatus.Healthy, Now)),
            Now,
            TimeSpan.FromMinutes(5));
        IReadOnlyList<TrayInstanceEffectiveState> current = TrayInstanceStateChangePolicy.Capture(
            CreateSnapshot(CreateItem(instanceId, HealthStatus.Maintenance, Now)),
            Now,
            TimeSpan.FromMinutes(5));

        TrayInstanceStateChange change = Assert.Single(TrayInstanceStateChangePolicy.DetectChanges(previous, current));

        Assert.Equal(instanceId, change.InstanceId);
        Assert.Equal(HealthStatus.Healthy, change.Previous.Status);
        Assert.Equal(HealthStatus.Maintenance, change.Current.Status);
        Assert.Equal(EvidenceFreshness.Current, change.Previous.Freshness);
        Assert.Equal(EvidenceFreshness.Current, change.Current.Freshness);
    }

    /// <summary>Verifies that the STATE-05 age profile emits exactly three post-baseline changes without replaying the already stale item.</summary>
    [Fact]
    public void DemonstrationAgeProfileProducesThreeIndividualPostBaselineChanges()
    {
        Guid finance = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Guid orders = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Guid analytics = Guid.Parse("00000000-0000-0000-0000-000000000003");
        Guid catalogue = Guid.Parse("00000000-0000-0000-0000-000000000004");
        TimeSpan staleAfter = TimeSpan.FromMinutes(5);
        InventorySnapshot snapshot = CreateSnapshot(
            CreateItem(finance, HealthStatus.Healthy, Now.AddSeconds(-38)),
            CreateItem(orders, HealthStatus.Degraded, Now.AddMinutes(-2)),
            CreateItem(analytics, HealthStatus.Timeout, Now.AddMinutes(-3)),
            CreateItem(catalogue, HealthStatus.Unknown, Now.AddMinutes(-9)));
        IReadOnlyList<TrayInstanceEffectiveState> baseline = TrayInstanceStateChangePolicy.Capture(snapshot, Now, staleAfter);

        IReadOnlyList<TrayInstanceEffectiveState> afterAnalytics = TrayInstanceStateChangePolicy.Capture(
            snapshot,
            Now.AddMinutes(2).AddTicks(1),
            staleAfter);
        IReadOnlyList<TrayInstanceEffectiveState> afterOrders = TrayInstanceStateChangePolicy.Capture(
            snapshot,
            Now.AddMinutes(3).AddTicks(1),
            staleAfter);
        IReadOnlyList<TrayInstanceEffectiveState> afterFinance = TrayInstanceStateChangePolicy.Capture(
            snapshot,
            Now.AddMinutes(4).AddSeconds(22).AddTicks(1),
            staleAfter);

        Assert.Equal(
            [analytics],
            TrayInstanceStateChangePolicy.DetectChanges(baseline, afterAnalytics).Select(change => change.InstanceId));
        Assert.Equal(
            [orders],
            TrayInstanceStateChangePolicy.DetectChanges(afterAnalytics, afterOrders).Select(change => change.InstanceId));
        Assert.Equal(
            [finance],
            TrayInstanceStateChangePolicy.DetectChanges(afterOrders, afterFinance).Select(change => change.InstanceId));
        Assert.Empty(TrayInstanceStateChangePolicy.DetectChanges(afterFinance, afterFinance));
    }

    /// <summary>Verifies that future delivery is change-only, opt-in and suppressed for the initial snapshot.</summary>
    [Fact]
    public void NotificationPolicyRequiresOptInPriorStateAndMaterialChange()
    {
        TrayFleetSummary healthy = TrayFleetPresentationPolicy.Summarise([new(HealthStatus.Healthy, IsStale: false)]);
        TrayFleetSummary warning = TrayFleetPresentationPolicy.Summarise([new(HealthStatus.Degraded, IsStale: false)]);

        Assert.False(TrayFleetPresentationPolicy.ShouldNotify(null, healthy, notificationsEnabled: true));
        Assert.False(TrayFleetPresentationPolicy.ShouldNotify(healthy, warning, notificationsEnabled: false));
        Assert.False(TrayFleetPresentationPolicy.ShouldNotify(healthy, healthy, notificationsEnabled: true));
        Assert.True(TrayFleetPresentationPolicy.ShouldNotify(healthy, warning, notificationsEnabled: true));
    }

    /// <summary>Creates an immutable inventory snapshot for pure presentation-policy tests.</summary>
    /// <param name="items">Provider-neutral inventory items included in the snapshot.</param>
    /// <returns>A versioned inventory snapshot generated at the shared test instant.</returns>
    private static InventorySnapshot CreateSnapshot(params InstanceInventoryItem[] items) =>
        new(InventorySnapshot.CurrentSchemaVersion, Now, items);

    /// <summary>Creates one current or ageable inventory item without provider-specific evidence.</summary>
    /// <param name="instanceId">Stable identifier assigned to the test instance.</param>
    /// <param name="status">Provider-neutral health status assigned to the test evidence.</param>
    /// <param name="receivedAt">UTC instant used for both observation and authoritative receipt.</param>
    /// <returns>An enabled demonstration inventory item suitable for freshness evaluation.</returns>
    private static InstanceInventoryItem CreateItem(Guid instanceId, HealthStatus status, DateTimeOffset receivedAt) =>
        new(
            instanceId,
            $"Instance {instanceId:N}",
            "demonstration",
            "Not homologated",
            "Test",
            "Local",
            status,
            receivedAt,
            receivedAt,
            TimeSpan.FromMilliseconds(5),
            Enabled: true);
}
