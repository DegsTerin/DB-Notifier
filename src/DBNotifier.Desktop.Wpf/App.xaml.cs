// Module purpose: Initialises the WPF desktop application without controlling database services implicitly.
using System.Windows;
using DBNotifier.Application.Presentation;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Owns the notification-area-first WPF lifecycle and shared local demonstration evidence without controlling
/// database services, external infrastructure or administrative capabilities.
/// </summary>
public partial class App : System.Windows.Application, IDisposable
{
    private TrayApplicationController? trayController;
    private DesktopLocalisationService? localisation;
    private DesktopThemeService? theme;
    private ProviderVisualIdentityPolicy? providerVisualIdentityPolicy;

    /// <summary>Loads safe preferences and starts in the notification area with only explicitly requested local review modes enabled.</summary>
    /// <param name="e">Startup arguments; exact desktop, notification-transition and accessibility review switches remain independent and opt-in.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DesktopUiPreferenceStore preferences = new();
        localisation = new DesktopLocalisationService(this, preferences);
        theme = new DesktopThemeService(this, preferences);
        localisation.Initialise();
        theme.Initialise();
        providerVisualIdentityPolicy = new ProviderVisualIdentityPolicy(theme);
        DateTimeOffset generatedAt = TimeProvider.System.GetUtcNow();
        DesktopDemonstrationEvidence evidence = DesktopDemonstrationEvidence.Create(generatedAt);
        TrayFleetSummary fleetSummary = evidence.Summarise(generatedAt);
        DesktopAccessibilityReviewMode accessibilityReviewMode = DesktopAccessibilityReviewPolicy.Resolve(e.Args);
        MainWindow window = new(
            localisation,
            theme,
            providerVisualIdentityPolicy,
            evidence,
            fleetSummary.State,
            accessibilityReviewMode);
        MainWindow = window;
        TrayNotificationValidationMode notificationValidationMode = TrayNotificationValidationPolicy.Resolve(e.Args);
        trayController = new TrayApplicationController(
            window,
            this,
            localisation,
            providerVisualIdentityPolicy,
            evidence,
            fleetSummary,
            notificationValidationMode);
        if (TrayStartupPolicy.Resolve(e.Args) == TrayStartupMode.ShowDesktop)
        {
            window.Show();
        }
    }

    /// <summary>Releases tray and theme-observation resources when the WPF application exits.</summary>
    /// <param name="e">Framework exit event metadata.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        Dispose();
        base.OnExit(e);
    }

    /// <summary>Releases owned presentation resources without controlling external services.</summary>
    public void Dispose()
    {
        trayController?.Dispose();
        trayController = null;
        providerVisualIdentityPolicy?.Dispose();
        providerVisualIdentityPolicy = null;
        theme?.Dispose();
        theme = null;
        GC.SuppressFinalize(this);
    }
}
