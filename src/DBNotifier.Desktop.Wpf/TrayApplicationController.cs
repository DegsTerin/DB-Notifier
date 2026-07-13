// Module purpose: Coordinates the localised Windows tray lifecycle without controlling database or operating-system services.
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using DBNotifier.Application.Presentation;
using Forms = System.Windows.Forms;

namespace DBNotifier.Desktop.Wpf;

/// <summary>Maps window intents to tray presentation actions and updates its labels with the active interface language.</summary>
internal sealed class TrayApplicationController : IDisposable
{
    private readonly MainWindow window;
    private readonly System.Windows.Application application;
    private readonly DesktopLocalisationService localisation;
    private readonly Forms.NotifyIcon notifyIcon;
    private readonly Forms.ToolStripMenuItem openItem;
    private readonly Forms.ToolStripMenuItem statusItem;
    private readonly Forms.ToolStripMenuItem exitItem;
    private bool exiting;
    private bool firstHide = true;

    /// <summary>Initialises tray controls and subscribes to window and language state changes.</summary>
    public TrayApplicationController(MainWindow window, System.Windows.Application application, DesktopLocalisationService localisation)
    {
        this.window = window;
        this.application = application;
        this.localisation = localisation;
        openItem = new Forms.ToolStripMenuItem();
        openItem.Click += (_, _) => Apply(TrayWindowIntent.Show);
        statusItem = new Forms.ToolStripMenuItem { Enabled = false };
        exitItem = new Forms.ToolStripMenuItem();
        exitItem.Click += (_, _) => Apply(TrayWindowIntent.Exit);
        Forms.ContextMenuStrip menu = new();
        menu.Items.AddRange([openItem, statusItem, new Forms.ToolStripSeparator(), exitItem]);
        notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true,
        };
        notifyIcon.DoubleClick += (_, _) => Apply(TrayWindowIntent.Show);
        window.StateChanged += WindowStateChanged;
        window.Closing += WindowClosing;
        localisation.LanguageChanged += LanguageChanged;
        RefreshText();
    }

    /// <summary>Releases notification and event resources without changing external process state.</summary>
    public void Dispose()
    {
        window.StateChanged -= WindowStateChanged;
        window.Closing -= WindowClosing;
        localisation.LanguageChanged -= LanguageChanged;
        notifyIcon.Visible = false;
        notifyIcon.ContextMenuStrip?.Dispose();
        notifyIcon.Dispose();
    }

    /// <summary>Applies the minimise-to-tray policy when the WPF window is minimised.</summary>
    private void WindowStateChanged(object? sender, EventArgs e)
    {
        if (window.WindowState == WindowState.Minimized) Apply(TrayWindowIntent.Minimize);
    }

    /// <summary>Converts a normal close request to a tray action until explicit application exit.</summary>
    private void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (exiting) return;
        e.Cancel = true;
        Apply(TrayWindowIntent.CloseRequest);
    }

    /// <summary>Refreshes tray labels after an interface-language change.</summary>
    private void LanguageChanged(object? sender, EventArgs e) => RefreshText();

    /// <summary>Updates all tray-visible strings from the generated localisation dictionary.</summary>
    private void RefreshText()
    {
        openItem.Text = localisation.Text("Tray.Open");
        statusItem.Text = localisation.Text("Tray.Status");
        exitItem.Text = localisation.Text("Tray.Exit");
        notifyIcon.Text = localisation.Text("Tray.Tooltip");
    }

    /// <summary>Executes only the local window action resolved by the neutral tray policy.</summary>
    private void Apply(TrayWindowIntent intent)
    {
        switch (TrayPresentationPolicy.Resolve(intent))
        {
            case TrayWindowAction.HideToTray:
                window.Hide();
                if (firstHide)
                {
                    notifyIcon.ShowBalloonTip(3000, localisation.Text("Tray.BalloonTitle"), localisation.Text("Tray.BalloonMessage"), Forms.ToolTipIcon.Info);
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
