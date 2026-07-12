using System.Windows;

namespace DBNotifier.Desktop.Wpf;

public partial class App : System.Windows.Application, IDisposable
{
    private TrayApplicationController? trayController;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        MainWindow window = new();
        MainWindow = window;
        trayController = new TrayApplicationController(window, this);
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Dispose();
        base.OnExit(e);
    }

    public void Dispose()
    {
        trayController?.Dispose();
        trayController = null;
        GC.SuppressFinalize(this);
    }
}
