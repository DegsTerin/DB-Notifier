// Module purpose: Coordinates the localised Windows tray lifecycle without controlling database or operating-system services.
using System.ComponentModel;
using System.Drawing;
using System.IO;
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
    private readonly Forms.NotifyIcon notifyIcon;
    private readonly TrayFlyoutWindow flyout;
    private readonly TrayFleetSummary fleetSummary;
    private bool exiting;
    private bool firstHide = true;

    /// <summary>Initialises tray controls and subscribes to window and language state changes.</summary>
    public TrayApplicationController(MainWindow window, System.Windows.Application application, DesktopLocalisationService localisation)
    {
        this.window = window;
        this.application = application;
        this.localisation = localisation;
        fleetSummary = CreateDemonstrationSummary();
        flyout = new TrayFlyoutWindow(localisation, fleetSummary, ShowView, () => Apply(TrayWindowIntent.Exit));
        applicationIcon = LoadApplicationIcon(fleetSummary.State);
        notifyIcon = new Forms.NotifyIcon
        {
            Icon = applicationIcon,
            Visible = true,
        };
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
        applicationIcon.Dispose();
    }

    /// <summary>Loads an independent transparent icon whose bell reflects the aggregate fleet state.</summary>
    /// <param name="state">The provider-neutral aggregate state used to select the semantic bell colour.</param>
    /// <returns>A disposable Windows icon that remains valid after the resource stream closes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the selected packaged DB Notifier icon resource is unavailable.</exception>
    private static Icon LoadApplicationIcon(TrayAggregateState state)
    {
        string assetName = state switch
        {
            TrayAggregateState.Healthy => "DBNotifier.Healthy.ico",
            TrayAggregateState.Warning => "DBNotifier.Warning.ico",
            TrayAggregateState.Critical => "DBNotifier.Critical.ico",
            TrayAggregateState.Unknown => "DBNotifier.Unknown.ico",
            _ => "DBNotifier.Unknown.ico",
        };
        Uri resourceUri = new($"pack://application:,,,/Assets/{assetName}", UriKind.Absolute);
        System.Windows.Resources.StreamResourceInfo resource = System.Windows.Application.GetResourceStream(resourceUri)
            ?? throw new InvalidOperationException("The selected packaged DB Notifier status icon resource is unavailable.");
        using Stream stream = resource.Stream;
        using Icon source = new(stream);
        return (Icon)source.Clone();
    }

    /// <summary>Creates the deterministic STATE-05 fleet summary without reading an Agent, API or monitored database.</summary>
    /// <returns>The provider-neutral summary used by the local Tray demonstration.</returns>
    private static TrayFleetSummary CreateDemonstrationSummary() => TrayFleetPresentationPolicy.Summarise(
    [
        new(HealthStatus.Healthy, IsStale: false),
        new(HealthStatus.Degraded, IsStale: false),
        new(HealthStatus.Timeout, IsStale: false),
        new(HealthStatus.Unknown, IsStale: true),
    ]);

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
