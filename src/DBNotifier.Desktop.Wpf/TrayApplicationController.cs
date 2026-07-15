// Module purpose: Coordinates the localised Windows tray lifecycle without controlling database or operating-system services.
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;
using Forms = System.Windows.Forms;

namespace DBNotifier.Desktop.Wpf;

/// <summary>Maps window intents to tray presentation actions and updates its labels with the active interface language.</summary>
internal sealed class TrayApplicationController : IDisposable
{
    private readonly MainWindow window;
    private readonly System.Windows.Application application;
    private readonly DesktopLocalisationService localisation;
    private readonly Icon applicationIcon;
    private readonly Icon notificationIcon;
    private readonly Forms.NotifyIcon notifyIcon;
    private readonly TrayFlyoutWindow flyout;
    private readonly TrayFleetSummary fleetSummary;
    private bool exiting;
    private bool firstHide = true;

    /// <summary>Initialises tray controls from the shared fleet summary and subscribes to window and language state changes.</summary>
    /// <param name="window">Secondary WPF shell controlled by notification-area intents.</param>
    /// <param name="application">Owning WPF application lifecycle.</param>
    /// <param name="localisation">Localisation owner used by Tray text and the flyout.</param>
    /// <param name="fleetSummary">Provider-neutral state shared by every operational product-mark surface.</param>
    public TrayApplicationController(
        MainWindow window,
        System.Windows.Application application,
        DesktopLocalisationService localisation,
        TrayFleetSummary fleetSummary)
    {
        this.window = window;
        this.application = application;
        this.localisation = localisation;
        this.fleetSummary = fleetSummary;
        flyout = new TrayFlyoutWindow(localisation, fleetSummary, ShowView, () => Apply(TrayWindowIntent.Exit));
        (applicationIcon, notificationIcon, notifyIcon) = CreateNotificationAreaResources(fleetSummary.State);
        notifyIcon.MouseClick += NotifyIconMouseClick;
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
        notifyIcon.MouseClick -= NotifyIconMouseClick;
        notifyIcon.Visible = false;
        flyout.CloseForApplicationExit();
        notifyIcon.Dispose();
        notificationIcon.Dispose();
        applicationIcon.Dispose();
    }

    /// <summary>Creates the deterministic STATE-05 fleet summary without reading an Agent, API or monitored database.</summary>
    /// <returns>The provider-neutral summary used by the local Tray demonstration.</returns>
    internal static TrayFleetSummary CreateDemonstrationSummary() => TrayFleetPresentationPolicy.Summarise(
    [
        new(HealthStatus.Healthy, IsStale: false),
        new(HealthStatus.Degraded, IsStale: false),
        new(HealthStatus.Timeout, IsStale: false),
        new(HealthStatus.Unknown, IsStale: true),
    ]);

    /// <summary>Creates the small Tray icon, larger notification source and native NotifyIcon as one exception-safe resource set.</summary>
    /// <param name="state">Provider-neutral aggregate used to select both semantic icon frames.</param>
    /// <returns>The two owned icon frames and configured notification-area component.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when Windows reports an invalid native icon metric.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a packaged semantic icon resource is unavailable.</exception>
    private static (Icon ApplicationIcon, Icon NotificationIcon, Forms.NotifyIcon NotifyIcon) CreateNotificationAreaResources(
        TrayAggregateState state)
    {
        Icon? applicationIcon = null;
        Icon? notificationIcon = null;
        Forms.NotifyIcon? notifyIcon = null;

        try
        {
            applicationIcon = BrandStatusIconPolicy.LoadWindowsIcon(
                state,
                Forms.SystemInformation.SmallIconSize.Width);
            notificationIcon = BrandStatusIconPolicy.LoadWindowsIcon(
                state,
                Forms.SystemInformation.IconSize.Width);
            notifyIcon = new Forms.NotifyIcon();
            notifyIcon.Icon = applicationIcon;
            notifyIcon.Visible = true;
            return (applicationIcon, notificationIcon, notifyIcon);
        }
        catch
        {
            if (notifyIcon is not null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }

            notificationIcon?.Dispose();
            applicationIcon?.Dispose();
            throw;
        }
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
        string aggregateLabel = localisation.Text($"Tray.Aggregate.{fleetSummary.State}");
        notifyIcon.Text = localisation.Text("Tray.TooltipSummary", aggregateLabel);
    }

    /// <summary>Toggles the accessible WPF fleet flyout after the Windows shell reports a complete primary or secondary click.</summary>
    private void NotifyIconMouseClick(object? sender, Forms.MouseEventArgs e)
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
                    ShowFirstHideNotification();
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

    /// <summary>Dispatches the local hide confirmation with the larger frame of the same semantic mark, then restores the Tray metric.</summary>
    private void ShowFirstHideNotification()
    {
        // WinForms exposes only stock balloon glyphs. Supplying the larger semantic NotifyIcon frame during the synchronous Shell call
        // gives Windows the best canonical source available without replacing the approved MouseClick lifecycle or claiming Shell ownership.
        notifyIcon.Icon = notificationIcon;
        try
        {
            notifyIcon.ShowBalloonTip(
                3000,
                localisation.Text("Tray.BalloonTitle"),
                localisation.Text("Tray.BalloonMessage"),
                Forms.ToolTipIcon.None);
        }
        finally
        {
            notifyIcon.Icon = applicationIcon;
        }
    }
}
