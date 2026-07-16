// Module purpose: Verifies Tray Presentation Tests behaviour and protects the documented project contract.
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;

namespace DBNotifier.UnitTests;

public sealed class TrayPresentationTests
{
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
}
