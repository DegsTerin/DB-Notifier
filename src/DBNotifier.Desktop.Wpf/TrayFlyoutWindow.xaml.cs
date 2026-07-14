// Module purpose: Positions and coordinates the notification-area fleet flyout while limiting its actions to local desktop navigation and exit.
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Presents a localised fleet summary near the Windows notification area and delegates only validated desktop navigation.
/// It deliberately contains no provider connection, service-control or configuration-mutation behaviour.
/// </summary>
internal sealed partial class TrayFlyoutWindow : Window
{
    private readonly DesktopLocalisationService localisation;
    private readonly Action<DesktopView> openView;
    private readonly Action exitApplication;
    private readonly DispatcherTimer activationTimer;
    private bool allowClose;
    private bool dismissOnDeactivation;

    /// <summary>Initialises the flyout with safe callbacks owned by the tray lifecycle controller.</summary>
    /// <param name="localisation">Generated localisation resource owner.</param>
    /// <param name="openView">Callback that opens one validated read-only desktop destination.</param>
    /// <param name="exitApplication">Callback that explicitly exits DB Notifier.</param>
    internal TrayFlyoutWindow(DesktopLocalisationService localisation, Action<DesktopView> openView, Action exitApplication)
    {
        this.localisation = localisation;
        this.openView = openView;
        this.exitApplication = exitApplication;
        activationTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
        {
            Interval = TimeSpan.FromMilliseconds(120),
        };
        activationTimer.Tick += CompleteActivation;
        InitializeComponent();
    }

    /// <summary>Shows and positions the flyout inside the working area nearest the notification icon.</summary>
    internal void ShowNearNotificationArea()
    {
        SnapshotText.Text = localisation.Text("Tray.LocalSnapshot", FormatUtc(TimeProvider.System.GetUtcNow()));
        dismissOnDeactivation = false;
        activationTimer.Stop();
        Show();
        UpdateLayout();

        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        System.Drawing.Rectangle workingArea = Forms.Screen.FromPoint(Forms.Cursor.Position).WorkingArea;
        Left = (workingArea.Right / dpi.DpiScaleX) - ActualWidth - 12;
        Top = (workingArea.Bottom / dpi.DpiScaleY) - ActualHeight - 12;
        Activate();
        Focus();
        activationTimer.Start();
    }

    /// <summary>Refreshes code-created presentation text after a supported language change.</summary>
    internal void RefreshPresentation()
    {
        if (IsVisible)
        {
            SnapshotText.Text = localisation.Text("Tray.LocalSnapshot", FormatUtc(TimeProvider.System.GetUtcNow()));
        }
    }

    /// <summary>Closes the reusable flyout during application disposal.</summary>
    internal void CloseForApplicationExit()
    {
        activationTimer.Stop();
        allowClose = true;
        Close();
    }

    /// <summary>Restores activation after the Windows overflow panel finishes dismissing, then enables focus-loss dismissal.</summary>
    private void CompleteActivation(object? sender, EventArgs e)
    {
        activationTimer.Stop();
        if (!IsVisible)
        {
            return;
        }

        Activate();
        Focus();
        dismissOnDeactivation = true;
    }

    /// <summary>Formats an exact UTC timestamp without implying that an external source was read.</summary>
    private string FormatUtc(DateTimeOffset value) => value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", localisation.Culture);

    /// <summary>Opens the composed operational overview and hides the transient flyout.</summary>
    private void OpenOverviewClick(object sender, RoutedEventArgs e) => Open(DesktopView.Overview);

    /// <summary>Opens the local history and alerts demonstration view and hides the transient flyout.</summary>
    private void OpenHistoryAlertsClick(object sender, RoutedEventArgs e) => Open(DesktopView.HistoryAlerts);

    /// <summary>Opens the non-secret configuration demonstration view and hides the transient flyout.</summary>
    private void OpenConfigurationClick(object sender, RoutedEventArgs e) => Open(DesktopView.Configuration);

    /// <summary>Delegates one validated local navigation request.</summary>
    private void Open(DesktopView view)
    {
        Hide();
        openView(view);
    }

    /// <summary>Requests explicit DB Notifier exit without controlling any external process.</summary>
    private void ExitClick(object sender, RoutedEventArgs e)
    {
        Hide();
        exitApplication();
    }

    /// <summary>Hides the transient surface when focus moves to another application.</summary>
    private void WindowDeactivated(object? sender, EventArgs e)
    {
        if (dismissOnDeactivation)
        {
            Hide();
        }
    }

    /// <summary>Provides the expected Escape dismissal without closing the reusable window.</summary>
    private void WindowPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    /// <summary>Converts incidental close requests to dismissal until application disposal authorises closure.</summary>
    private void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (!allowClose)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
