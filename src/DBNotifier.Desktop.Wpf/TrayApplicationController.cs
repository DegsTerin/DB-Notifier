// Module purpose: Coordinates the localised Windows tray lifecycle without controlling database or operating-system services.
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;
using Forms = System.Windows.Forms;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Maps window intents to notification-area presentation, localises visible labels and owns the bounded semantic-icon lease
/// used while Windows displays a notification. It never controls a database or operating-system service.
/// </summary>
internal sealed class TrayApplicationController : IDisposable
{
    private readonly MainWindow window;
    private readonly System.Windows.Application application;
    private readonly DesktopLocalisationService localisation;
    private readonly Icon applicationIcon;
    private readonly Icon availabilityNotificationIcon;
    private readonly Forms.NotifyIcon notifyIcon;
    private readonly DispatcherTimer notificationIconRestoreTimer;
    private readonly WindowsAppNotificationPublisher? appNotificationPublisher;
    private readonly TrayFlyoutWindow flyout;
    private readonly TrayFleetSummary fleetSummary;
    private bool exiting;
    private TrayNotificationIconLeaseState notificationIconLeaseState = TrayNotificationIconLeaseState.Aggregate;

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
        (applicationIcon, availabilityNotificationIcon, notifyIcon) = CreateNotificationAreaResources(fleetSummary.State);
        notificationIconRestoreTimer = new DispatcherTimer
        {
            Interval = TrayNotificationIconLeasePolicy.FallbackDelay,
        };
        notificationIconRestoreTimer.Tick += NotificationIconRestoreTimerTick;
        notifyIcon.MouseClick += NotifyIconMouseClick;
        notifyIcon.DoubleClick += (_, _) => Apply(TrayWindowIntent.Show);
        notifyIcon.BalloonTipShown += NotifyIconBalloonTipShown;
        appNotificationPublisher = WindowsAppNotificationPublisher.TryCreate(
            application.Dispatcher,
            () => Apply(TrayWindowIntent.Show));
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
        notifyIcon.BalloonTipShown -= NotifyIconBalloonTipShown;
        RestoreAggregateIconAfterNotificationCapture(TrayNotificationIconLeaseSignal.Disposed);
        notificationIconRestoreTimer.Tick -= NotificationIconRestoreTimerTick;
        notificationIconRestoreTimer.Stop();
        appNotificationPublisher?.Dispose();
        notifyIcon.Visible = false;
        flyout.CloseForApplicationExit();
        notifyIcon.Dispose();
        availabilityNotificationIcon.Dispose();
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

    /// <summary>Creates the semantic Tray icon and policy-selected availability source at the native Windows small-icon metric as one exception-safe resource set.</summary>
    /// <param name="state">Provider-neutral aggregate used only to select the state-bearing Tray icon.</param>
    /// <returns>The two owned icon frames and configured notification-area component.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when Windows reports an invalid native icon metric.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a packaged semantic icon resource is unavailable.</exception>
    private static (Icon ApplicationIcon, Icon AvailabilityNotificationIcon, Forms.NotifyIcon NotifyIcon) CreateNotificationAreaResources(
        TrayAggregateState state)
    {
        Icon? applicationIcon = null;
        Icon? availabilityNotificationIcon = null;
        Forms.NotifyIcon? notifyIcon = null;

        try
        {
            applicationIcon = BrandStatusIconPolicy.LoadWindowsIcon(
                state,
                Forms.SystemInformation.SmallIconSize.Width);
            availabilityNotificationIcon = BrandStatusIconPolicy.LoadWindowsIcon(
                TrayNotificationPresentationPolicy.ResolveIconState(TrayNotificationMeaning.AvailabilityOrRecovery),
                Forms.SystemInformation.SmallIconSize.Width);
            notifyIcon = new Forms.NotifyIcon();
            notifyIcon.Icon = applicationIcon;
            notifyIcon.Visible = true;
            return (applicationIcon, availabilityNotificationIcon, notifyIcon);
        }
        catch
        {
            if (notifyIcon is not null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }

            availabilityNotificationIcon?.Dispose();
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

    /// <summary>Restores the factual aggregate icon after Windows reports that the availability notification was displayed.</summary>
    /// <param name="sender">NotifyIcon that reported the visible notification.</param>
    /// <param name="e">Event metadata supplied by Windows Forms.</param>
    private void NotifyIconBalloonTipShown(object? sender, EventArgs e)
    {
        RestoreAggregateIconAfterNotificationCapture(TrayNotificationIconLeaseSignal.BalloonShown);
    }

    /// <summary>Restores the factual aggregate icon when Windows suppresses the visible-notification callback.</summary>
    /// <param name="sender">Dispatcher timer that bounds the temporary availability state.</param>
    /// <param name="e">Timer event metadata.</param>
    private void NotificationIconRestoreTimerTick(object? sender, EventArgs e)
    {
        RestoreAggregateIconAfterNotificationCapture(TrayNotificationIconLeaseSignal.FallbackElapsed);
    }

    /// <summary>Ends the bounded best-effort availability-icon lease without changing the aggregate fleet summary.</summary>
    /// <param name="signal">Lifecycle signal that requires the factual aggregate icon to resume.</param>
    private void RestoreAggregateIconAfterNotificationCapture(TrayNotificationIconLeaseSignal signal)
    {
        if (notificationIconLeaseState != TrayNotificationIconLeaseState.NotificationMeaning)
        {
            notificationIconRestoreTimer.Stop();
            return;
        }

        notifyIcon.Icon = applicationIcon;
        notificationIconLeaseState = TrayNotificationIconLeasePolicy.Resolve(notificationIconLeaseState, signal);
        notificationIconRestoreTimer.Stop();
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
                if (TrayPresentationPolicy.ShouldRequestAvailabilityConfirmation(intent))
                {
                    ShowCloseToTrayNotification();
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

    /// <summary>Requests one fresh close-to-Tray confirmation through a bounded best-effort green source.</summary>
    private void ShowCloseToTrayNotification()
    {
        string title = localisation.Text("Tray.BalloonTitle");
        string message = localisation.Text("Tray.BalloonMessage");

        // Each explicit close requests a fresh confirmation with an explicit transparent DB Notifier identity asset.
        // Platform acceptance is not proof that Windows displayed the card because Focus Assist and notification policy remain authoritative.
        if (appNotificationPublisher?.TryPublishAvailability(title, message) == true) return;

        ShowLegacyCloseToTrayNotification(title, message);
    }

    /// <summary>Uses the bounded notification-area fallback when the modern Windows publisher is unavailable or rejects delivery.</summary>
    /// <param name="title">Localised title displayed by the Windows notification surface.</param>
    /// <param name="message">Localised factual message confirming that the application remains available.</param>
    private void ShowLegacyCloseToTrayNotification(string title, string message)
    {
        // The displayed callback is a best-effort capture point; the bounded fallback limits the temporary Tray exception.
        try
        {
            notificationIconRestoreTimer.Stop();
            notifyIcon.Icon = availabilityNotificationIcon;
            notificationIconLeaseState = TrayNotificationIconLeasePolicy.Resolve(
                notificationIconLeaseState,
                TrayNotificationIconLeaseSignal.Begin);
            notificationIconRestoreTimer.Start();
            notifyIcon.ShowBalloonTip(
                3000,
                title,
                message,
                Forms.ToolTipIcon.None);
        }
        catch
        {
            RestoreAggregateIconAfterNotificationCapture(TrayNotificationIconLeaseSignal.DeliveryFailed);
        }
    }
}
