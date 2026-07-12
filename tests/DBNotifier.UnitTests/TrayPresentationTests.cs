using DBNotifier.Application.Presentation;

namespace DBNotifier.UnitTests;

public sealed class TrayPresentationTests
{
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
