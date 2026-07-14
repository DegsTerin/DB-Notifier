// Module purpose: Defines Tray Presentation application behaviour without depending on concrete providers or user interfaces.
namespace DBNotifier.Application.Presentation;

/// <summary>Identifies whether the Windows client starts in its normal notification-area mode or an explicit desktop-review mode.</summary>
public enum TrayStartupMode
{
    /// <summary>Starts only the notification-area controller and keeps the secondary desktop shell hidden.</summary>
    NotificationArea,

    /// <summary>Shows the secondary desktop shell for a bounded development or accessibility review.</summary>
    ShowDesktop,
}

/// <summary>Resolves the bounded command-line override used by development and accessibility audits.</summary>
public static class TrayStartupPolicy
{
    /// <summary>Returns notification-area-first startup unless the exact desktop-review switch is present.</summary>
    /// <param name="arguments">Command-line arguments supplied to the Windows client.</param>
    /// <returns>The safe startup mode; unknown arguments do not make the desktop visible.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    public static TrayStartupMode Resolve(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return arguments.Any(argument => string.Equals(argument, "--show-desktop", StringComparison.OrdinalIgnoreCase))
            ? TrayStartupMode.ShowDesktop
            : TrayStartupMode.NotificationArea;
    }
}

/// <summary>Identifies a user or operating-system request affecting a tray-owned desktop window.</summary>
public enum TrayWindowIntent
{
    /// <summary>Requests that the visible window be minimised.</summary>
    Minimize,

    /// <summary>Requests that the visible window be closed without exiting the notification-area client.</summary>
    CloseRequest,

    /// <summary>Requests that the secondary desktop window be shown and activated.</summary>
    Show,

    /// <summary>Requests an explicit application exit.</summary>
    Exit,
}

/// <summary>Identifies the safe presentation action selected for a tray-owned desktop window.</summary>
public enum TrayWindowAction
{
    /// <summary>Hides the window while preserving the notification-area client.</summary>
    HideToTray,

    /// <summary>Shows and activates the secondary desktop window.</summary>
    ShowAndActivate,

    /// <summary>Terminates the notification-area client after an explicit exit request.</summary>
    ExitApplication,
}

/// <summary>Maps window intentions to safe notification-area-first presentation actions.</summary>
public static class TrayPresentationPolicy
{
    /// <summary>Resolves the presentation action for a supported tray-owned window intention.</summary>
    /// <param name="intent">The window intention to resolve.</param>
    /// <returns>The safe presentation action associated with <paramref name="intent"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="intent"/> is not recognised.</exception>
    public static TrayWindowAction Resolve(TrayWindowIntent intent) => intent switch
    {
        TrayWindowIntent.Minimize or TrayWindowIntent.CloseRequest => TrayWindowAction.HideToTray,
        TrayWindowIntent.Show => TrayWindowAction.ShowAndActivate,
        TrayWindowIntent.Exit => TrayWindowAction.ExitApplication,
        _ => throw new ArgumentOutOfRangeException(nameof(intent)),
    };
}

/// <summary>Provides provider-neutral status, freshness and support text for the compact tray surface.</summary>
/// <param name="StatusLabel">The canonical user-facing status label.</param>
/// <param name="FreshnessLabel">The factual user-facing freshness label.</param>
/// <param name="SupportLabel">The factual capability or support label.</param>
/// <param name="UsesExternalData">Whether the presentation is backed by an authorised external data source.</param>
public sealed record TrayStatusPresentation(
    string StatusLabel,
    string FreshnessLabel,
    string SupportLabel,
    bool UsesExternalData);
