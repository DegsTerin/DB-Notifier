// Module purpose: Positions and coordinates the notification-area fleet flyout while limiting its actions to local desktop navigation and exit.
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;
using Forms = System.Windows.Forms;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Presents a localised fleet summary near the Windows notification area and delegates only validated desktop navigation.
/// It deliberately contains no provider connection, service-control or configuration-mutation behaviour.
/// </summary>
internal sealed partial class TrayFlyoutWindow : Window
{
    private readonly DesktopLocalisationService localisation;
    private readonly DesktopDemonstrationEvidence evidence;
    private readonly Action<DesktopView> openView;
    private readonly Action exitApplication;
    private readonly DispatcherTimer activationTimer;
    private bool allowClose;
    private bool dismissOnDeactivation;
    private TrayFleetSummary fleetSummary;

    /// <summary>Initialises the flyout with safe callbacks owned by the tray lifecycle controller.</summary>
    /// <param name="localisation">Generated localisation resource owner.</param>
    /// <param name="evidence">Immutable locale-independent evidence shared with the desktop shell.</param>
    /// <param name="initialSummary">Aggregate evaluated at the evidence creation instant.</param>
    /// <param name="openView">Callback that opens one validated read-only desktop destination.</param>
    /// <param name="exitApplication">Callback that explicitly exits DB Notifier.</param>
    internal TrayFlyoutWindow(
        DesktopLocalisationService localisation,
        DesktopDemonstrationEvidence evidence,
        TrayFleetSummary initialSummary,
        Action<DesktopView> openView,
        Action exitApplication)
    {
        this.localisation = localisation;
        this.evidence = evidence;
        fleetSummary = initialSummary;
        this.openView = openView;
        this.exitApplication = exitApplication;
        activationTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
        {
            Interval = TimeSpan.FromMilliseconds(120),
        };
        activationTimer.Tick += CompleteActivation;
        InitializeComponent();
        RefreshPresentation(evidence.GeneratedAt, initialSummary);
    }

    /// <summary>Shows and positions the flyout inside the working area nearest the notification icon.</summary>
    internal void ShowNearNotificationArea()
    {
        dismissOnDeactivation = false;
        activationTimer.Stop();
        Show();
        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        BrandStatusImage.Source = BrandStatusIconPolicy.LoadImageSource(fleetSummary.State, 32, dpi);
        UpdateLayout();

        System.Drawing.Rectangle workingArea = Forms.Screen.FromPoint(Forms.Cursor.Position).WorkingArea;
        Left = (workingArea.Right / dpi.DpiScaleX) - ActualWidth - 12;
        Top = (workingArea.Bottom / dpi.DpiScaleY) - ActualHeight - 12;
        Activate();
        Focus();
        activationTimer.Start();
    }

    /// <summary>Refreshes localised fleet text and item states from one shared freshness evaluation.</summary>
    /// <param name="evaluatedAt">UTC instant used only to classify the immutable item evidence.</param>
    /// <param name="summary">Aggregate derived from the same evidence and evaluation instant.</param>
    internal void RefreshPresentation(DateTimeOffset evaluatedAt, TrayFleetSummary summary)
    {
        fleetSummary = summary;
        string aggregateLabel = localisation.Text($"Tray.Aggregate.{fleetSummary.State}");
        AggregateText.Text = localisation.Text("Tray.AggregateSummary", aggregateLabel);
        SnapshotText.Text = localisation.Text("Tray.LocalSnapshot", FormatUtc(evidence.GeneratedAt));
        RefreshInstanceStates(evaluatedAt);
    }

    /// <summary>Aligns every flyout row with the canonical freshness and provider-neutral status policies.</summary>
    /// <param name="evaluatedAt">UTC instant used only to classify item evidence.</param>
    private void RefreshInstanceStates(DateTimeOffset evaluatedAt)
    {
        InstanceInventoryItem[] items = evidence.CreateInventorySnapshot(localisation).Items.ToArray();
        Shape[] glyphs = [FinanceStatusGlyph, OrdersStatusGlyph, AnalyticsStatusGlyph, CatalogueStatusGlyph];
        System.Windows.Controls.TextBlock[] labels = [FinanceStatusText, OrdersStatusText, AnalyticsStatusText, CatalogueStatusText];
        for (int index = 0; index < Math.Min(items.Length, labels.Length); index++)
        {
            InstanceInventoryItem item = items[index];
            EvidenceFreshness freshness = item.GetFreshness(evaluatedAt, DesktopDemonstrationEvidence.StaleAfter);
            (string labelKey, string brushKey) = freshness switch
            {
                EvidenceFreshness.Stale => ("Status.Stale", "ComponentStatusNeutralForegroundBrush"),
                EvidenceFreshness.Unknown => ("Status.Unknown", "ComponentStatusNeutralForegroundBrush"),
                _ => item.Status switch
                {
                    HealthStatus.Healthy => ("Status.Healthy", "ComponentStatusHealthyForegroundBrush"),
                    HealthStatus.Degraded or HealthStatus.Maintenance => ($"Status.{item.Status}", "ComponentStatusDegradedForegroundBrush"),
                    HealthStatus.Unavailable or HealthStatus.AuthFailed or HealthStatus.Timeout => ($"Status.{item.Status}", "ComponentStatusCriticalForegroundBrush"),
                    _ => ("Status.Unknown", "ComponentStatusNeutralForegroundBrush"),
                },
            };
            labels[index].Text = localisation.Text(labelKey);
            labels[index].SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, brushKey);
            glyphs[index].SetResourceReference(Shape.FillProperty, brushKey);
            glyphs[index].SetResourceReference(Shape.StrokeProperty, brushKey);
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
    private void OpenHistoryAlertsClick(object sender, RoutedEventArgs e) => Open(DesktopView.Alerts);

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
