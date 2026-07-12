using System.ComponentModel;
using System.Drawing;
using System.Windows;
using DBNotifier.Application.Presentation;
using Forms = System.Windows.Forms;

namespace DBNotifier.Desktop.Wpf;

internal sealed class TrayApplicationController : IDisposable
{
    private readonly MainWindow window;
    private readonly System.Windows.Application application;
    private readonly Forms.NotifyIcon notifyIcon;
    private bool exiting;
    private bool firstHide = true;

    public TrayApplicationController(MainWindow window, System.Windows.Application application)
    {
        this.window = window;
        this.application = application;
        Forms.ToolStripMenuItem openItem = new("Abrir DB-Notifier");
        openItem.Click += (_, _) => Apply(TrayWindowIntent.Show);
        Forms.ToolStripMenuItem statusItem = new("Demonstração local · sem dados externos")
        {
            Enabled = false,
        };
        Forms.ToolStripMenuItem exitItem = new("Sair");
        exitItem.Click += (_, _) => Apply(TrayWindowIntent.Exit);
        Forms.ContextMenuStrip menu = new();
        menu.Items.AddRange([openItem, statusItem, new Forms.ToolStripSeparator(), exitItem]);
        notifyIcon = new Forms.NotifyIcon
        {
            Text = "DB-Notifier · demonstração local",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true,
        };
        notifyIcon.DoubleClick += (_, _) => Apply(TrayWindowIntent.Show);
        window.StateChanged += WindowStateChanged;
        window.Closing += WindowClosing;
    }

    public void Dispose()
    {
        window.StateChanged -= WindowStateChanged;
        window.Closing -= WindowClosing;
        notifyIcon.Visible = false;
        notifyIcon.ContextMenuStrip?.Dispose();
        notifyIcon.Dispose();
    }

    private void WindowStateChanged(object? sender, EventArgs e)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            Apply(TrayWindowIntent.Minimize);
        }
    }

    private void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (exiting)
        {
            return;
        }

        e.Cancel = true;
        Apply(TrayWindowIntent.CloseRequest);
    }

    private void Apply(TrayWindowIntent intent)
    {
        switch (TrayPresentationPolicy.Resolve(intent))
        {
            case TrayWindowAction.HideToTray:
                window.Hide();
                if (firstHide)
                {
                    notifyIcon.ShowBalloonTip(
                        3000,
                        "DB-Notifier continua disponível",
                        "A janela foi recolhida. Nenhum banco ou serviço foi controlado.",
                        Forms.ToolTipIcon.Info);
                    firstHide = false;
                }
                break;
            case TrayWindowAction.ShowAndActivate:
                window.Show();
                window.WindowState = WindowState.Normal;
                window.Activate();
                break;
            case TrayWindowAction.ExitApplication:
                exiting = true;
                notifyIcon.Visible = false;
                window.Close();
                application.Shutdown();
                break;
        }
    }
}
