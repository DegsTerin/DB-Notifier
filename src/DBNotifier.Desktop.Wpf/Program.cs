// Module purpose: Establishes process-wide Windows presentation policy before either WPF or notification-area controls are created.
using Forms = System.Windows.Forms;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Owns the Windows desktop entry point so Per-Monitor V2 awareness is selected before WPF windows or WinForms
/// notification-area resources exist.
/// </summary>
internal static class Program
{
    /// <summary>Starts the local desktop client in a Per-Monitor V2 context without requesting elevated privileges.</summary>
    [STAThread]
    private static void Main()
    {
        Forms.Application.SetHighDpiMode(Forms.HighDpiMode.PerMonitorV2);
        App application = new();
        application.InitializeComponent();
        application.Run();
    }
}
