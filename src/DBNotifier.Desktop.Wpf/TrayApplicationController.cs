// Module purpose: Coordinates the localised Windows tray lifecycle without controlling database or operating-system services.
using System.ComponentModel;
using System.Drawing;
using System.IO;
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
    private readonly Icon applicationIcon;
    private readonly Forms.NotifyIcon notifyIcon;
    private readonly TrayFlyoutWindow flyout;
    private bool exiting;
    private bool firstHide = true;

    /// <summary>Initialises tray controls and subscribes to window and language state changes.</summary>
    public TrayApplicationController(MainWindow window, System.Windows.Application application, DesktopLocalisationService localisation)
    {
        this.window = window;
        this.application = application;
        this.localisation = localisation;
        flyout = new TrayFlyoutWindow(localisation, ShowView, () => Apply(TrayWindowIntent.Exit));
        applicationIcon = LoadApplicationIcon();
        notifyIcon = new Forms.NotifyIcon
        {
            Icon = applicationIcon,
            Visible = true,
        };
        notifyIcon.MouseUp += NotifyIconMouseUp;
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
        notifyIcon.MouseUp -= NotifyIconMouseUp;
        notifyIcon.Visible = false;
        flyout.CloseForApplicationExit();
        notifyIcon.Dispose();
        applicationIcon.Dispose();
    }

    /// <summary>Loads an independent icon instance from the WPF resource shared by the executable and window.</summary>
    /// <returns>A disposable Windows icon that remains valid after the resource stream closes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the packaged DB-Notifier icon resource is unavailable.</exception>
    private static Icon LoadApplicationIcon()
    {
        Uri resourceUri = new("pack://application:,,,/Assets/DBNotifier.ico", UriKind.Absolute);
        System.Windows.Resources.StreamResourceInfo resource = System.Windows.Application.GetResourceStream(resourceUri)
            ?? throw new InvalidOperationException("The packaged DB-Notifier icon resource is unavailable.");
        using Stream stream = resource.Stream;
        using Icon source = new(stream);
        return (Icon)source.Clone();
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
    private void LanguageChanged(object? sender, EventArgs e)
    {
        RefreshText();
        flyout.RefreshPresentation();
    }

    /// <summary>Updates all tray-visible strings from the generated localisation dictionary.</summary>
    private void RefreshText()
    {
        notifyIcon.Text = localisation.Text("Tray.Tooltip");
    }

    /// <summary>Toggles the accessible WPF fleet flyout after primary or secondary notification-area activation.</summary>
    private void NotifyIconMouseUp(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button is Forms.MouseButtons.Left or Forms.MouseButtons.Right)
        {
            ToggleFlyout();
        }
    }

    /// <summary>Shows or dismisses the single transient notification-area surface.</summary>
    private void ToggleFlyout()
    {
        if (flyout.IsVisible)
        {
            flyout.Hide();
            return;
        }

        flyout.ShowNearNotificationArea();
    }

    /// <summary>Restores the desktop window and selects one safe read-only destination.</summary>
    private void ShowView(DesktopView view)
    {
        Apply(TrayWindowIntent.Show);
        window.ShowView(view);
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
