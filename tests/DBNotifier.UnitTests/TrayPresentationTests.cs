// Module purpose: Verifies Tray Presentation Tests behaviour and protects the documented project contract.
using DBNotifier.Application.Presentation;

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
}
