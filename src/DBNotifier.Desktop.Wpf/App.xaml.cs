// Module purpose: Implements App xaml for the Windows desktop shell without controlling database services implicitly.
using System.Windows;

namespace DBNotifier.Desktop.Wpf;

public partial class App : System.Windows.Application, IDisposable
{
    private TrayApplicationController? trayController;
    private DesktopLocalisationService? localisation;
    private DesktopThemeService? theme;

    /// <summary>Loads safe language and theme preferences before constructing any visible desktop surface.</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DesktopUiPreferenceStore preferences = new();
        localisation = new DesktopLocalisationService(this, preferences);
        theme = new DesktopThemeService(this, preferences);
        localisation.Initialise();
        theme.Initialise();
        MainWindow window = new(localisation, theme);
        MainWindow = window;
        trayController = new TrayApplicationController(window, this, localisation);
        window.Show();
    }

    /// <summary>Releases tray and theme-observation resources when the WPF application exits.</summary>
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
