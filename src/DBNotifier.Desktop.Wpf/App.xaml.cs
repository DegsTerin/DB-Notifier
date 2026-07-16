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

    /// <summary>Loads safe preferences and starts in the notification area unless an explicit audit switch requests the desktop.</summary>
    /// <param name="e">Startup arguments; only <c>--show-desktop</c> changes the tray-first default.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DesktopUiPreferenceStore preferences = new();
        localisation = new DesktopLocalisationService(this, preferences);
        theme = new DesktopThemeService(this, preferences);
        localisation.Initialise();
        theme.Initialise();
        DateTimeOffset generatedAt = TimeProvider.System.GetUtcNow();
        DesktopDemonstrationEvidence evidence = DesktopDemonstrationEvidence.Create(generatedAt);
        TrayFleetSummary fleetSummary = evidence.Summarise(generatedAt);
        MainWindow window = new(localisation, theme, evidence, fleetSummary.State);
        MainWindow = window;
        trayController = new TrayApplicationController(window, this, localisation, evidence, fleetSummary);
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
        theme?.Dispose();
        theme = null;
        GC.SuppressFinalize(this);
    }
}
