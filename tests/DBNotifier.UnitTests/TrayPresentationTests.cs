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

    [Theory]
    [InlineData(TrayWindowIntent.Minimize, TrayWindowAction.HideToTray)]
    [InlineData(TrayWindowIntent.CloseRequest, TrayWindowAction.HideToTray)]
    [InlineData(TrayWindowIntent.Show, TrayWindowAction.ShowAndActivate)]
    [InlineData(TrayWindowIntent.Exit, TrayWindowAction.ExitApplication)]
    public void WindowIntentMapsToExplicitTrayAction(
        TrayWindowIntent intent,
        TrayWindowAction expected)
    {
        Assert.Equal(expected, TrayPresentationPolicy.Resolve(intent));
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
