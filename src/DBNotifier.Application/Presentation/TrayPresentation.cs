// Module purpose: Defines Tray Presentation application behaviour without depending on concrete providers or user interfaces.
namespace DBNotifier.Application.Presentation;

public enum TrayWindowIntent
{
    Minimize,
    CloseRequest,
    Show,
    Exit,
}

public enum TrayWindowAction
{
    HideToTray,
    ShowAndActivate,
    ExitApplication,
}

public static class TrayPresentationPolicy
{
    public static TrayWindowAction Resolve(TrayWindowIntent intent) => intent switch
    {
        TrayWindowIntent.Minimize or TrayWindowIntent.CloseRequest => TrayWindowAction.HideToTray,
        TrayWindowIntent.Show => TrayWindowAction.ShowAndActivate,
        TrayWindowIntent.Exit => TrayWindowAction.ExitApplication,
        _ => throw new ArgumentOutOfRangeException(nameof(intent)),
    };
}

public sealed record TrayStatusPresentation(
    string StatusLabel,
    string FreshnessLabel,
    string SupportLabel,
    bool UsesExternalData);
