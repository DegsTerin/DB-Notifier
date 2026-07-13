// Module purpose: Implements App xaml for the Windows desktop shell without controlling database services implicitly.
using System.Windows;

namespace DBNotifier.Desktop.Wpf;

public partial class App : System.Windows.Application, IDisposable
{
    private TrayApplicationController? trayController;
    private DesktopLocalisationService? localisation;

    /// <summary>Loads the safe language preference before constructing any visible desktop surface.</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        localisation = new DesktopLocalisationService(this);
        localisation.Initialise();
        MainWindow window = new(localisation);
        MainWindow = window;
        trayController = new TrayApplicationController(window, this, localisation);
        window.Show();
    }

    /// <summary>Releases tray resources when the WPF application exits.</summary>
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
        GC.SuppressFinalize(this);
    }
}
