// Module purpose: Positions and coordinates the notification-area fleet flyout while limiting its actions to local desktop navigation and exit.
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
    private readonly ProviderVisualIdentityPolicy providerVisualIdentityPolicy;
    private readonly DesktopDemonstrationEvidence evidence;
    private readonly Action<DesktopView> openView;
    private readonly Action exitApplication;
    private readonly DispatcherTimer activationTimer;
    private bool allowClose;
    private bool dismissOnDeactivation;
    private bool compactLayout;
    private TrayFleetSummary fleetSummary;

    /// <summary>Initialises the flyout with safe callbacks owned by the tray lifecycle controller.</summary>
    /// <param name="localisation">Generated localisation resource owner.</param>
    /// <param name="providerVisualIdentityPolicy">Theme-aware resolver for decorative per-instance provider logos.</param>
    /// <param name="evidence">Immutable locale-independent evidence shared with the desktop shell.</param>
    /// <param name="initialSummary">Aggregate evaluated at the evidence creation instant.</param>
    /// <param name="openView">Callback that opens one validated read-only desktop destination.</param>
    /// <param name="exitApplication">Callback that explicitly exits DB Notifier.</param>
    /// <param name="secondaryNavigationEnabled">Whether the current composition permits opening the preference-bearing shell.</param>
    internal TrayFlyoutWindow(
        DesktopLocalisationService localisation,
        ProviderVisualIdentityPolicy providerVisualIdentityPolicy,
        DesktopDemonstrationEvidence evidence,
        TrayFleetSummary initialSummary,
        Action<DesktopView> openView,
        Action exitApplication,
        bool secondaryNavigationEnabled)
    {
        this.localisation = localisation;
        this.providerVisualIdentityPolicy = providerVisualIdentityPolicy;
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
        OpenOverviewButton.IsEnabled = secondaryNavigationEnabled;
        OpenConfigurationButton.IsEnabled = secondaryNavigationEnabled;
        OpenHistoryAlertsButton.IsEnabled = secondaryNavigationEnabled;
        providerVisualIdentityPolicy.VisualIdentityChanged += ProviderVisualIdentityChanged;
        Closed += TrayFlyoutWindowClosed;
        RefreshPresentation(evidence.GeneratedAt, initialSummary);
    }

    /// <summary>Shows and positions the flyout inside the working area nearest the notification icon.</summary>
    internal void ShowNearNotificationArea()
    {
        dismissOnDeactivation = false;
        activationTimer.Stop();
        Show();
        PositionWithinCurrentWorkArea();
        Activate();
        Focus();
        activationTimer.Start();
    }

    /// <summary>Sizes, reflows and positions the flyout inside the monitor work area using its current per-monitor DPI.</summary>
    private void PositionWithinCurrentWorkArea()
    {
        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        System.Drawing.Rectangle workingArea = Forms.Screen.FromPoint(Forms.Cursor.Position).WorkingArea;
        double availableWidth = Math.Max(320, workingArea.Width / dpi.DpiScaleX - 24);
        double availableHeight = Math.Max(320, workingArea.Height / dpi.DpiScaleY - 24);
        Width = Math.Min(560, availableWidth);
        MaxHeight = Math.Min(620, availableHeight);
        ApplyResponsiveLayout(Width < 480);
        RefreshBrandStatusImage(dpi);
        UpdateLayout();

        double workingLeft = workingArea.Left / dpi.DpiScaleX;
        double workingTop = workingArea.Top / dpi.DpiScaleY;
        double workingRight = workingArea.Right / dpi.DpiScaleX;
        double workingBottom = workingArea.Bottom / dpi.DpiScaleY;
        Left = Math.Max(workingLeft + 12, workingRight - ActualWidth - 12);
        Top = Math.Max(workingTop + 12, workingBottom - ActualHeight - 12);
    }

    /// <summary>Switches between side-by-side and stacked panels without hiding evidence or navigation.</summary>
    /// <param name="compact">Whether the available width requires a single-column body.</param>
    private void ApplyResponsiveLayout(bool compact)
    {
        if (compactLayout == compact)
        {
            return;
        }

        compactLayout = compact;
        FleetColumn.Width = new GridLength(1, GridUnitType.Star);
        ActionsColumn.Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        System.Windows.Controls.Grid.SetRow(ActionsPanel, compact ? 1 : 0);
        System.Windows.Controls.Grid.SetColumn(ActionsPanel, compact ? 0 : 1);
        FleetPanel.BorderThickness = compact ? new Thickness(0, 1, 0, 0) : new Thickness(0, 1, 1, 1);
        ActionsPanel.BorderThickness = new Thickness(0, compact ? 0 : 1, 0, 1);
    }

    /// <summary>Repositions after Windows moves the flyout between monitors with different DPI.</summary>
    /// <param name="sender">Flyout window.</param>
    /// <param name="e">Old and new per-monitor DPI values.</param>
    private void WindowDpiChanged(object sender, System.Windows.DpiChangedEventArgs e) =>
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(PositionWithinCurrentWorkArea));

    /// <summary>Re-evaluates bounded panel reflow when the work-area-constrained width changes.</summary>
    /// <param name="sender">Flyout window.</param>
    /// <param name="e">Previous and current rendered dimensions.</param>
    private void WindowSizeChanged(object sender, SizeChangedEventArgs e) => ApplyResponsiveLayout(e.NewSize.Width < 480);

    /// <summary>Refreshes localised fleet text and item states from one shared freshness evaluation.</summary>
    /// <param name="evaluatedAt">UTC instant used only to classify the immutable item evidence.</param>
    /// <param name="summary">Aggregate derived from the same evidence and evaluation instant.</param>
    internal void RefreshPresentation(DateTimeOffset evaluatedAt, TrayFleetSummary summary)
    {
        InstanceInventoryItem[] items = evidence.CreateInventorySnapshot(localisation).Items.ToArray();
        ApplyPresentation(
            summary,
            items.Select(item => new TrayFlyoutInstancePresentationState(
                item.InstanceId,
                item.Status,
                item.GetFreshness(evaluatedAt, DesktopDemonstrationEvidence.StaleAfter),
                item.Enabled)).ToArray());
    }

    /// <summary>Applies one coherent W02 frame without reading, persisting or publishing any operational state.</summary>
    /// <param name="frame">Bounded frame whose row states, aggregate and counts were derived together.</param>
    internal void RefreshReviewPresentation(TrayFlyoutPresentationFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ApplyPresentation(frame.Summary, frame.InstanceStates);
    }

    /// <summary>Updates aggregate text, brand mark and rows from one presentation transaction.</summary>
    /// <param name="summary">Aggregate and counts derived from the supplied row states.</param>
    /// <param name="states">Stable per-instance states belonging to the same evaluation frame.</param>
    private void ApplyPresentation(
        TrayFleetSummary summary,
        IReadOnlyList<TrayFlyoutInstancePresentationState> states)
    {
        fleetSummary = summary;
        string aggregateLabel = localisation.Text($"Tray.Aggregate.{fleetSummary.State}");
        AggregateText.Text =
            $"{localisation.Text("Tray.AggregateSummary", aggregateLabel)} · {localisation.Text("Inventory.DisabledCount", fleetSummary.DisabledCount)}";
        SnapshotText.Text = localisation.Text("Tray.LocalSnapshot", FormatUtc(evidence.GeneratedAt));
        RefreshBrandStatusImage(VisualTreeHelper.GetDpi(this));
        RefreshInstanceStates(states);
    }

    /// <summary>Aligns every flyout row with the canonical freshness and provider-neutral status policies.</summary>
    /// <param name="states">Stable row states already evaluated for the current presentation frame.</param>
    private void RefreshInstanceStates(IReadOnlyList<TrayFlyoutInstancePresentationState> states)
    {
        InstanceInventoryItem[] items = evidence.CreateInventorySnapshot(localisation).Items.ToArray();
        SemanticIcon[] glyphs = [FinanceStatusGlyph, OrdersStatusGlyph, AnalyticsStatusGlyph, CatalogueStatusGlyph];
        System.Windows.Controls.TextBlock[] labels = [FinanceStatusText, OrdersStatusText, AnalyticsStatusText, CatalogueStatusText];
        ProviderIdentityView[] providerIdentities =
            [FinanceProviderIdentity, OrdersProviderIdentity, AnalyticsProviderIdentity, CatalogueProviderIdentity];
        for (int index = 0; index < Math.Min(items.Length, labels.Length); index++)
        {
            InstanceInventoryItem item = items[index];
            providerIdentities[index].Identity = providerVisualIdentityPolicy.Resolve(item.ProviderType);
            TrayFlyoutInstancePresentationState state = states
                .SingleOrDefault(candidate => candidate.InstanceId == item.InstanceId)
                ?? new(item.InstanceId, HealthStatus.Unknown, EvidenceFreshness.Unknown, true);
            (string labelKey, string brushKey, SemanticIconKind iconKind) = ResolveInstancePresentation(state);
            labels[index].Text = localisation.Text(labelKey);
            labels[index].SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, brushKey);
            glyphs[index].Kind = iconKind;
            glyphs[index].SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, brushKey);
        }
    }

    /// <summary>Maps one row state to matching text, colour resource and code-native semantic geometry.</summary>
    /// <param name="state">Provider-neutral row state from the active frame.</param>
    /// <returns>A localised key, semantic brush key and icon kind that fail unknown evidence closed.</returns>
    private static (string LabelKey, string BrushKey, SemanticIconKind IconKind) ResolveInstancePresentation(
        TrayFlyoutInstancePresentationState state)
    {
        if (!state.Enabled)
        {
            return ("Status.Disabled", "ComponentStatusNeutralForegroundBrush", SemanticIconKind.Disabled);
        }

        return state.Freshness switch
        {
            EvidenceFreshness.Stale =>
                ("Status.Stale", "ComponentStatusNeutralForegroundBrush", SemanticIconKind.Stale),
            EvidenceFreshness.Unknown =>
                ("Status.Unknown", "ComponentStatusNeutralForegroundBrush", SemanticIconKind.Unknown),
            _ => state.Status switch
            {
                HealthStatus.Healthy =>
                    ("Status.Healthy", "ComponentStatusHealthyForegroundBrush", SemanticIconKind.Healthy),
                HealthStatus.Degraded or HealthStatus.Maintenance =>
                    ($"Status.{state.Status}", "ComponentStatusDegradedForegroundBrush", SemanticIconKind.Degraded),
                HealthStatus.Unavailable or HealthStatus.AuthFailed or HealthStatus.Timeout =>
                    ($"Status.{state.Status}", "ComponentStatusCriticalForegroundBrush", SemanticIconKind.Critical),
                _ => ("Status.Unknown", "ComponentStatusNeutralForegroundBrush", SemanticIconKind.Unknown),
            },
        };
    }

    /// <summary>Synchronises the flyout mark with the latest aggregate without changing notification delivery state.</summary>
    /// <param name="dpi">Effective DPI used to select the smallest non-upscaled canonical icon frame.</param>
    private void RefreshBrandStatusImage(DpiScale dpi) =>
        BrandStatusImage.Source = BrandStatusIconPolicy.LoadImageSource(fleetSummary.State, 32, dpi);

    /// <summary>Re-resolves decorative provider identities without re-evaluating immutable operational evidence.</summary>
    /// <param name="sender">Provider identity policy that observed an effective presentation change.</param>
    /// <param name="e">Identity-change event data.</param>
    private void ProviderVisualIdentityChanged(object? sender, EventArgs e)
    {
        InstanceInventoryItem[] items = evidence.CreateInventorySnapshot(localisation).Items.ToArray();
        ProviderIdentityView[] providerIdentities =
            [FinanceProviderIdentity, OrdersProviderIdentity, AnalyticsProviderIdentity, CatalogueProviderIdentity];
        for (int index = 0; index < Math.Min(items.Length, providerIdentities.Length); index++)
        {
            providerIdentities[index].Identity = providerVisualIdentityPolicy.Resolve(items[index].ProviderType);
        }
    }

    /// <summary>Releases provider-identity observation after the reusable flyout is closed for application exit.</summary>
    /// <param name="sender">Flyout that completed its final close.</param>
    /// <param name="e">Window close event data.</param>
    private void TrayFlyoutWindowClosed(object? sender, EventArgs e)
    {
        providerVisualIdentityPolicy.VisualIdentityChanged -= ProviderVisualIdentityChanged;
        Closed -= TrayFlyoutWindowClosed;
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
    private string FormatUtc(DateTimeOffset value) => DesktopDateTimePresentation.FormatUtc(value, localisation.Culture);

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
