// Module purpose: Presents reconciled read-only inventory plus local demonstration history, alerts and capability decisions through the localised WPF shell.
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;

namespace DBNotifier.Desktop.Wpf;

/// <summary>Identifies the safe read-only desktop destinations exposed by the notification-area flyout.</summary>
internal enum DesktopView
{
    Overview,
    Inventory,
    Alerts,
    Performance,
    History,
    Configuration,
    Providers,
    Settings,
}

/// <summary>Preserves native card rendering while exposing its arranged bounds only to raw diagnostic automation.</summary>
internal sealed class AuditableCardBorder : Border
{
    /// <summary>Creates the raw-only peer used to compare contained chart geometry with the actual card boundary.</summary>
    /// <returns>A diagnostic peer excluded from assistive-technology control and content views.</returns>
    protected override AutomationPeer OnCreateAutomationPeer() => new AuditableCardBorderAutomationPeer(this);

    /// <summary>Keeps card geometry measurable without adding a duplicate readable group around its existing content.</summary>
    private sealed class AuditableCardBorderAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>Initialises the peer for one rendered card boundary.</summary>
        /// <param name="owner">Card border whose arranged rectangle is audited.</param>
        public AuditableCardBorderAutomationPeer(AuditableCardBorder owner)
            : base(owner)
        {
        }

        /// <summary>Classifies the diagnostic geometry as a non-interactive group.</summary>
        /// <returns>The standard group automation control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        /// <summary>Excludes the duplicate card boundary from the control view.</summary>
        /// <returns>False because the existing card content carries the accessible information.</returns>
        protected override bool IsControlElementCore() => false;

        /// <summary>Excludes the duplicate card boundary from the content view.</summary>
        /// <returns>False because the existing card content carries the accessible information.</returns>
        protected override bool IsContentElementCore() => false;
    }
}

/// <summary>
/// Presents reconciled read-only inventory and isolated demonstration data without connecting to databases or executing commands.
/// Localised presentation values are rebuilt whenever the user changes the supported interface language;
/// theme changes remain isolated to generated semantic resources owned by the desktop theme service.
/// Responsive route templates preserve every factual field while switching between desktop tables and compact stacked records.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DesktopLocalisationService localisation;
    private readonly DesktopThemeService theme;
    private readonly DesktopMotionService motion;
    private readonly ProviderVisualIdentityPolicy providerVisualIdentityPolicy;
    private readonly DesktopDemonstrationEvidence evidence;
    private readonly ObservableCollection<InventoryRow> rows = [];
    private readonly ObservableCollection<TimelineRow> timelineRows = [];
    private readonly ObservableCollection<AlertRow> alertRows = [];
    private readonly ObservableCollection<ProviderDistributionItem> providerRows = [];
    private readonly ObservableCollection<CapabilityRow> capabilityRows = [];
    private InventorySnapshot snapshot = null!;
    private TimelineAlertSnapshot timelineSnapshot = null!;
    private ConfigurationCapabilitySnapshot configurationSnapshot = null!;
    private DesktopView currentView = DesktopView.Overview;
    private TrayAggregateState aggregateState;
    private IDisposable? windowIconLease;
    private bool nativeIconsInitialised;
    private bool? compactLayoutApplied;

    /// <summary>Initialises the desktop surface with one accepted inventory snapshot and isolated local fixtures.</summary>
    /// <param name="localisation">Desktop localisation owner shared with the application and tray controller.</param>
    /// <param name="theme">Desktop theme owner shared with the application.</param>
    /// <param name="motion">Read-only adapter for the current Windows reduced-motion preference.</param>
    /// <param name="providerVisualIdentityPolicy">Local resolver for decorative provider logos and neutral fallbacks.</param>
    /// <param name="evidence">Immutable locale-independent evidence shared with Tray presentation.</param>
    /// <param name="initialSnapshot">Initial snapshot accepted by the application reconciliation boundary.</param>
    /// <param name="aggregateState">Initial provider-neutral fleet state shared by every product-mark surface.</param>
    /// <param name="accessibilityReviewMode">Optional isolated geometry used only for a bounded accessibility review.</param>
    internal MainWindow(
        DesktopLocalisationService localisation,
        DesktopThemeService theme,
        DesktopMotionService motion,
        ProviderVisualIdentityPolicy providerVisualIdentityPolicy,
        DesktopDemonstrationEvidence evidence,
        InventorySnapshot initialSnapshot,
        TrayAggregateState aggregateState,
        DesktopAccessibilityReviewMode accessibilityReviewMode)
    {
        this.localisation = localisation;
        this.theme = theme;
        this.motion = motion;
        this.providerVisualIdentityPolicy = providerVisualIdentityPolicy;
        this.evidence = evidence;
        snapshot = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));
        this.aggregateState = aggregateState;
        InitializeComponent();
        ConfigureAccessibilityReview(accessibilityReviewMode);
        SourceInitialized += MainWindowSourceInitialized;
        UpdatePreferenceButtons();
        UpdateNavigationState();
        UpdateResponsiveLayout();
        OverviewInventoryList.ItemsSource = rows;
        OverviewAlertList.ItemsSource = alertRows;
        InventoryGrid.ItemsSource = rows;
        HistoryGrid.ItemsSource = timelineRows;
        AlertsGrid.ItemsSource = alertRows;
        ProviderCatalogueList.ItemsSource = providerRows;
        CapabilityGrid.ItemsSource = capabilityRows;
        localisation.LanguageChanged += LocalisationLanguageChanged;
        theme.ThemeChanged += ThemeChanged;
        motion.MotionPreferenceChanged += MotionPreferenceChanged;
        providerVisualIdentityPolicy.VisualIdentityChanged += ProviderVisualIdentityChanged;
        Closed += (_, _) =>
        {
            SourceInitialized -= MainWindowSourceInitialized;
            windowIconLease?.Dispose();
            localisation.LanguageChanged -= LocalisationLanguageChanged;
            theme.ThemeChanged -= ThemeChanged;
            motion.MotionPreferenceChanged -= MotionPreferenceChanged;
            providerVisualIdentityPolicy.VisualIdentityChanged -= ProviderVisualIdentityChanged;
        };
        RebuildLocalisedData();
        ApplyMotionPreference();
        PresentReadyState();
    }

    /// <summary>Applies a static ComboBox transition when Windows requests reduced motion.</summary>
    private void ApplyMotionPreference()
    {
        ScenarioSelector.ApplyTemplate();
        if (ScenarioSelector.Template.FindName("PART_Popup", ScenarioSelector) is Popup popup)
        {
            popup.PopupAnimation = motion.ReducedMotionActive ? PopupAnimation.None : PopupAnimation.Fade;
        }
    }

    /// <summary>Re-applies the existing operating-system motion preference without changing it.</summary>
    /// <param name="sender">Shared Windows motion adapter.</param>
    /// <param name="e">Preference-change event data.</param>
    private void MotionPreferenceChanged(object? sender, EventArgs e) => ApplyMotionPreference();

    /// <summary>Applies bounded review-only geometry without replacing or extending the normal demonstration fixture.</summary>
    /// <param name="mode">Validated review mode resolved from an exact local command-line switch.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported review mode reaches the desktop boundary.</exception>
    private void ConfigureAccessibilityReview(DesktopAccessibilityReviewMode mode)
    {
        ScenarioSelector.MaxDropDownHeight = mode switch
        {
            DesktopAccessibilityReviewMode.Disabled => ScenarioSelector.MaxDropDownHeight,
            DesktopAccessibilityReviewMode.ComboBoxOverflow => 128,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
    }

    /// <summary>Assigns independent native title and taskbar icon sizes once the WPF window handle exists.</summary>
    /// <param name="sender">Owning window instance.</param>
    /// <param name="e">Source-initialisation event data.</param>
    private void MainWindowSourceInitialized(object? sender, EventArgs e)
    {
        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        Icon = BrandStatusIconPolicy.LoadImageSource(aggregateState, 32, dpi);
        BrandStatusImage.Source = BrandStatusIconPolicy.LoadImageSource(aggregateState, 40, dpi);
        windowIconLease = BrandStatusIconPolicy.ApplyNativeWindowIcons(this, aggregateState);
        nativeIconsInitialised = true;
        NativeWindowThemePolicy.Apply(this, theme);
    }

    /// <summary>Shows a validated read-only destination requested by the notification-area flyout.</summary>
    /// <param name="view">Known desktop destination; no administrative operation is performed.</param>
    internal void ShowView(DesktopView view)
    {
        currentView = Enum.IsDefined(view) ? view : throw new ArgumentOutOfRangeException(nameof(view));
        ScenarioSelector.SelectedIndex = 0;
        DesktopContentScrollViewer.ScrollToTop();
        UpdateNavigationState();
        PresentReadyState();
    }

    /// <summary>Atomically applies one accepted or retained inventory snapshot and its aggregate presentation state.</summary>
    /// <param name="reconciledSnapshot">Snapshot validated or retained by the application reconciliation boundary.</param>
    /// <param name="evaluatedAt">UTC instant shared by detailed freshness and aggregate evaluation.</param>
    /// <param name="state">Aggregate state derived from <paramref name="reconciledSnapshot"/> at the same instant.</param>
    internal void ApplyReconciledInventory(
        InventorySnapshot reconciledSnapshot,
        DateTimeOffset evaluatedAt,
        TrayAggregateState state)
    {
        snapshot = reconciledSnapshot ?? throw new ArgumentNullException(nameof(reconciledSnapshot));
        RebuildInventoryDerivedData();
        UpdateAggregateState(state);
        if (ScenarioSelector.SelectedItem is ComboBoxItem selected &&
            Enum.TryParse(selected.Tag?.ToString(), false, out InventorySurfaceState surfaceState) &&
            surfaceState == InventorySurfaceState.Ready)
        {
            PresentReadyState(evaluatedAt);
        }
    }

    /// <summary>Updates WPF image and native window icon roles only when reconciled fleet state changes.</summary>
    /// <param name="state">Current provider-neutral aggregate derived from shared evidence.</param>
    private void UpdateAggregateState(TrayAggregateState state)
    {
        if (state == aggregateState)
        {
            return;
        }

        aggregateState = state;
        if (!nativeIconsInitialised)
        {
            return;
        }

        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        Icon = BrandStatusIconPolicy.LoadImageSource(state, 32, dpi);
        BrandStatusImage.Source = BrandStatusIconPolicy.LoadImageSource(state, 40, dpi);
        IDisposable replacement = BrandStatusIconPolicy.ApplyNativeWindowIcons(this, state);
        windowIconLease?.Dispose();
        windowIconLease = replacement;
    }

    /// <summary>Selects one of the eight read-only desktop destinations exposed by the shared product navigation.</summary>
    /// <param name="sender">Navigation button whose tag contains a validated <see cref="DesktopView"/> value.</param>
    /// <param name="e">Button activation event data.</param>
    private void NavigationButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && Enum.TryParse(button.Tag?.ToString(), false, out DesktopView view))
        {
            ShowView(view);
        }
    }

    /// <summary>Cycles to the other supported interface language without accepting arbitrary locale input.</summary>
    /// <param name="sender">The single language preference button.</param>
    /// <param name="e">Button activation event data.</param>
    private void LanguagePreferenceButtonClick(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        localisation.SetLanguage(localisation.CurrentLanguage == InterfaceLanguage.BritishEnglish
            ? InterfaceLanguage.BrazilianPortuguese
            : InterfaceLanguage.BritishEnglish);
    }

    /// <summary>Cycles Light and Dark while preserving the validated theme preference contract.</summary>
    /// <param name="sender">The single theme preference button.</param>
    /// <param name="e">Button activation event data.</param>
    private void ThemePreferenceButtonClick(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        ThemePreference next = theme.CurrentPreference == ThemePreference.Light
            ? ThemePreference.Dark
            : ThemePreference.Light;
        theme.SetPreference(next);
    }

    /// <summary>Opens the local alerts surface without delivering or acknowledging an external notification.</summary>
    /// <param name="sender">The notification button in the desktop TopBar.</param>
    /// <param name="e">Button activation event data.</param>
    private void NotificationButtonClick(object sender, RoutedEventArgs e) => ShowView(DesktopView.Alerts);

    /// <summary>Opens the detailed local alert route from the Overview without acknowledging or delivering anything.</summary>
    /// <param name="sender">Overview navigation affordance.</param>
    /// <param name="e">Button activation event data.</param>
    private void OverviewViewAlertsClick(object sender, RoutedEventArgs e) => ShowView(DesktopView.Alerts);

    /// <summary>Opens the local interface preferences surface without changing operational configuration.</summary>
    /// <param name="sender">The preferences button in the desktop TopBar.</param>
    /// <param name="e">Button activation event data.</param>
    private void SettingsButtonClick(object sender, RoutedEventArgs e) => ShowView(DesktopView.Settings);

    /// <summary>Refreshes both icon controls, tooltips and accessible names from current validated preferences.</summary>
    private void UpdatePreferenceButtons()
    {
        InterfaceLanguage nextLanguage = localisation.CurrentLanguage == InterfaceLanguage.BritishEnglish
            ? InterfaceLanguage.BrazilianPortuguese
            : InterfaceLanguage.BritishEnglish;
        string languageLabel = Text("Language.Toggle", LanguageLabel(localisation.CurrentLanguage), LanguageLabel(nextLanguage));
        LanguagePreferenceButton.ToolTip = languageLabel;
        System.Windows.Automation.AutomationProperties.SetName(LanguagePreferenceButton, languageLabel);
        System.Windows.Automation.AutomationProperties.SetHelpText(LanguagePreferenceButton, languageLabel);

        ThemePreference nextTheme = theme.CurrentPreference == ThemePreference.Light
            ? ThemePreference.Dark
            : ThemePreference.Light;
        string themeLabel = Text("Theme.Toggle", ThemeLabel(theme.CurrentPreference), ThemeLabel(nextTheme));
        ThemePreferenceButton.ToolTip = themeLabel;
        System.Windows.Automation.AutomationProperties.SetName(ThemePreferenceButton, themeLabel);
        System.Windows.Automation.AutomationProperties.SetHelpText(ThemePreferenceButton, themeLabel);
        LightThemeIcon.Visibility = theme.CurrentPreference == ThemePreference.Light ? Visibility.Visible : Visibility.Collapsed;
        DarkThemeIcon.Visibility = theme.CurrentPreference == ThemePreference.Dark ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Maps a supported interface language to its current generated display label.</summary>
    /// <param name="language">Validated current or next interface language.</param>
    /// <returns>The localised native language label.</returns>
    private string LanguageLabel(InterfaceLanguage language) => Text(language == InterfaceLanguage.BritishEnglish ? "Language.EnGb" : "Language.PtBr");

    /// <summary>Maps a validated theme preference to its current generated display label.</summary>
    /// <param name="preference">Validated current or next theme preference.</param>
    /// <returns>The localised theme label.</returns>
    private string ThemeLabel(ThemePreference preference) => Text(preference == ThemePreference.Dark ? "Theme.Dark" : "Theme.Light");

    /// <summary>Refreshes the theme icon and accessible state after explicit theme or High Contrast changes.</summary>
    /// <param name="sender">Theme service that applied the new effective state.</param>
    /// <param name="e">Theme-change event data.</param>
    private void ThemeChanged(object? sender, EventArgs e)
    {
        NativeWindowThemePolicy.Apply(this, theme);
        UpdatePreferenceButtons();
        UpdateNavigationState();
        if (currentView == DesktopView.Settings)
        {
            PresentReadyState();
        }
    }

    /// <summary>Rebuilds fixture-derived labels after the generated resource dictionary changes.</summary>
    private void LocalisationLanguageChanged(object? sender, EventArgs e)
    {
        UpdatePreferenceButtons();
        UpdateNavigationState();
        RebuildLocalisedData();
        if (ScenarioSelector.SelectedItem is ComboBoxItem selected &&
            Enum.TryParse(selected.Tag?.ToString(), false, out InventorySurfaceState state) &&
            state != InventorySurfaceState.Ready)
        {
            PresentNonReadyState(state);
        }
        // The tray controller applies the newly localised reconciled snapshot after this synchronous resource update.
    }

    /// <summary>Presents the selected accessible operational state without external activity.</summary>
    private void ScenarioSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || ScenarioSelector.SelectedItem is not ComboBoxItem selected)
        {
            return;
        }

        InventorySurfaceState state = Enum.TryParse(selected.Tag?.ToString(), false, out InventorySurfaceState parsed)
            ? parsed
            : InventorySurfaceState.Error;
        if (state == InventorySurfaceState.Ready)
        {
            PresentReadyState();
            return;
        }
        PresentNonReadyState(state);
    }

    /// <summary>Returns keyboard focus and presentation to the loaded demonstration state.</summary>
    private void RetryButtonClick(object sender, RoutedEventArgs e)
    {
        ScenarioSelector.SelectedIndex = 0;
        ScenarioSelector.Focus();
    }

    /// <summary>Reviews the selected capability through the fail-closed application preview contract.</summary>
    private void ReviewCapabilityClick(object sender, RoutedEventArgs e)
    {
        if (CapabilityGrid.SelectedItem is not CapabilityRow selected)
        {
            ShowOwnedDialog("Configuration.AdminTitle", "Configuration.SelectCapability", sender as UIElement);
            return;
        }

        bool authorised = PermissionSelector.SelectedItem is ComboBoxItem permission &&
            string.Equals(permission.Tag?.ToString(), "Authorized", StringComparison.Ordinal);
        ShowPreview(configurationSnapshot.Preview(selected.CapabilityId, authorised), sender as UIElement);
    }

    /// <summary>Shows the safe confirmation UX example without enabling execution.</summary>
    private void PreviewConfirmationClick(object sender, RoutedEventArgs e) =>
        ShowPreview(ActionPreviewDisposition.ConfirmationRequired, sender as UIElement);

    /// <summary>Maps a canonical preview disposition to generated localised dialog content.</summary>
    /// <param name="disposition">Fail-closed application outcome to present.</param>
    /// <param name="opener">Control that regains focus after modal dismissal.</param>
    private void ShowPreview(ActionPreviewDisposition disposition, UIElement? opener)
    {
        (string titleKey, string messageKey) = disposition switch
        {
            ActionPreviewDisposition.ConfirmationRequired => ("Preview.Confirmation.Title", "Preview.Confirmation.Message"),
            ActionPreviewDisposition.Denied => ("Preview.Denied.Title", "Preview.Denied.Message"),
            ActionPreviewDisposition.Unsupported => ("Preview.Unsupported.Title", "Preview.Unsupported.Message"),
            ActionPreviewDisposition.Unavailable => ("Preview.Unavailable.Title", "Preview.Unavailable.Message"),
            _ => ("Preview.Unknown.Title", "Preview.Unknown.Message"),
        };
        ShowOwnedDialog(titleKey, messageKey, opener);
    }

    /// <summary>Shows an owned theme-aware dialogue and restores focus to its invoking control after dismissal.</summary>
    /// <param name="titleKey">Canonical generated title resource key.</param>
    /// <param name="messageKey">Canonical generated description resource key.</param>
    /// <param name="opener">Control that regains focus after modal dismissal.</param>
    private void ShowOwnedDialog(string titleKey, string messageKey, UIElement? opener)
    {
        CapabilityPreviewDialog dialog = new(theme, Text(titleKey), Text(messageKey))
        {
            Owner = this,
        };
        dialog.ShowDialog();
        opener?.Focus();
    }

    /// <summary>Presents the selected shared navigation destination from deterministic local demonstration data.</summary>
    /// <param name="evaluatedAt">Optional shared reconciliation instant; omission reads the current clock without changing evidence timestamps.</param>
    private void PresentReadyState(DateTimeOffset? evaluatedAt = null)
    {
        DateTimeOffset now = evaluatedAt ?? TimeProvider.System.GetUtcNow();
        UpdateViewHeading();
        switch (currentView)
        {
            case DesktopView.Overview:
                PresentOverview(now);
                return;
            case DesktopView.Alerts:
            case DesktopView.History:
                PresentHistoryAndAlerts(currentView);
                return;
            case DesktopView.Performance:
                UpdatedAtText.Text = Text("Wpf.UpdatedFromAdapters", FormatUtc(evidence.GeneratedAt));
                ShowOnly(PerformanceSurface);
                return;
            case DesktopView.Configuration:
                PresentConfiguration();
                return;
            case DesktopView.Providers:
                UpdatedAtText.Text = Text("Wpf.UpdatedFromAdapters", FormatUtc(evidence.GeneratedAt));
                ShowOnly(ProvidersSurface);
                return;
            case DesktopView.Settings:
                PresentSettings();
                return;
        }

        InventoryStatusSummary summary = snapshot.Summarize(now, DesktopDemonstrationEvidence.StaleAfter);
        Replace(rows, snapshot.Items.Select((item, index) => InventoryRow.From(
            item,
            index,
            now,
            DesktopDemonstrationEvidence.StaleAfter,
            localisation,
            providerVisualIdentityPolicy)));
        UpdatedAtText.Text = Text("Wpf.UpdatedAt", FormatUtc(snapshot.GeneratedAt));
        TotalCountText.Text = summary.Total.ToString(localisation.Culture);
        HealthyCountText.Text = summary.Healthy.ToString(localisation.Culture);
        DegradedCountText.Text = summary.Degraded.ToString(localisation.Culture);
        AttentionCountText.Text = summary.AttentionRequired.ToString(localisation.Culture);
        StaleCountText.Text = summary.Stale.ToString(localisation.Culture);
        DisabledCountText.Text = summary.Disabled.ToString(localisation.Culture);
        ShowOnly(ReadySurface);
    }

    /// <summary>Composes the read-only operational overview from the same local inventory and alert fixtures as detailed views.</summary>
    /// <param name="now">Current UTC clock used only for freshness evaluation and the local snapshot label.</param>
    private void PresentOverview(DateTimeOffset now)
    {
        InventoryStatusSummary summary = snapshot.Summarize(now, DesktopDemonstrationEvidence.StaleAfter);
        Replace(rows, snapshot.Items.Select((item, index) => InventoryRow.From(
            item,
            index,
            now,
            DesktopDemonstrationEvidence.StaleAfter,
            localisation,
            providerVisualIdentityPolicy)));
        Replace(alertRows, timelineSnapshot.Alerts.Select(item => AlertRow.From(
            item,
            localisation,
            providerVisualIdentityPolicy)));
        UpdatedAtText.Text = Text("Wpf.UpdatedFromAdapters", FormatUtc(snapshot.GeneratedAt));
        OverviewTotalCountText.Text = summary.Total.ToString(localisation.Culture);
        OverviewHealthyCountText.Text = summary.Healthy.ToString(localisation.Culture);
        OverviewDegradedCountText.Text = summary.Warning.ToString(localisation.Culture);
        OverviewAttentionCountText.Text = summary.AttentionRequired.ToString(localisation.Culture);
        OverviewDisabledCountText.Text = Text("Inventory.DisabledCount", summary.Disabled);
        ShowOnly(OverviewSurface);
    }

    /// <summary>Presents local event and alert fixtures using localised severity and state labels.</summary>
    private void PresentHistoryAndAlerts(DesktopView view)
    {
        Replace(timelineRows, timelineSnapshot.Events.Select(item => TimelineRow.From(
            item,
            localisation,
            providerVisualIdentityPolicy)));
        Replace(alertRows, timelineSnapshot.Alerts.Select(item => AlertRow.From(
            item,
            localisation,
            providerVisualIdentityPolicy)));
        AlertStatusSummary summary = timelineSnapshot.Summarize();
        AlertSummaryText.Text = Text("Wpf.AlertSummary", summary.Active, summary.UnresolvedCritical);
        HistorySummaryText.Text = Text("History.Count", timelineRows.Count, timelineRows.Count);
        AlertCriticalCountText.Text = summary.UnresolvedCritical.ToString(localisation.Culture);
        AlertActiveCountText.Text = summary.Active.ToString(localisation.Culture);
        AlertTotalCountText.Text = alertRows.Count.ToString(localisation.Culture);
        HistoryPanel.Visibility = view == DesktopView.History ? Visibility.Visible : Visibility.Collapsed;
        AlertsPanel.Visibility = view == DesktopView.Alerts ? Visibility.Visible : Visibility.Collapsed;
        AlertsPanel.Margin = view == DesktopView.Alerts ? new Thickness(0) : new Thickness(0, 18, 0, 0);
        UpdatedAtText.Text = Text("Wpf.UpdatedFromAdapters", FormatUtc(timelineSnapshot.GeneratedAt));
        ShowOnly(HistoryAlertSurface);
    }

    /// <summary>Presents safe configuration fields and unsupported capability decisions.</summary>
    private void PresentConfiguration()
    {
        Replace(capabilityRows, configurationSnapshot.Capabilities.Select(item => CapabilityRow.From(item, localisation)));
        ConfigurationGrid.ItemsSource = configurationSnapshot.Fields;
        ConfigurationInstanceText.Text = configurationSnapshot.InstanceName;
        ConfigurationProviderIdentity.Identity = providerVisualIdentityPolicy.Resolve(configurationSnapshot.ProviderType);
        CapabilityGrid.SelectedIndex = capabilityRows.Count > 0 ? 0 : -1;
        UpdatedAtText.Text = Text("Wpf.LocalPreview", FormatUtc(evidence.GeneratedAt));
        ShowOnly(ConfigurationSurface);
    }

    /// <summary>Presents local interface preferences and the factual future-integration status of Windows notifications.</summary>
    private void PresentSettings()
    {
        SettingsLanguageText.Text = LanguageLabel(localisation.CurrentLanguage);
        SettingsThemeText.Text = ThemeLabel(theme.CurrentPreference);
        UpdatedAtText.Text = Text("Wpf.LocalPreview", FormatUtc(evidence.GeneratedAt));
        ShowOnly(SettingsSurface);
    }

    /// <summary>Presents a complete, non-colour-only state with bounded retry visibility.</summary>
    private void PresentNonReadyState(InventorySurfaceState state)
    {
        (string symbol, string titleKey, string messageKey, bool loading, bool retry) = state switch
        {
            InventorySurfaceState.Loading => ("…", "Operational.Loading.Title", "Operational.Loading.Message", true, false),
            InventorySurfaceState.Empty => ("○", "Operational.Empty.Title", "Operational.Empty.Message", false, false),
            InventorySurfaceState.Offline => ("↯", "Operational.Offline.Title", "Operational.Offline.Message", false, true),
            InventorySurfaceState.Denied => ("⊘", "Operational.Denied.Title", "Operational.Denied.Message", false, false),
            InventorySurfaceState.Maintenance => ("◆", "Operational.Maintenance.Title", "Operational.Maintenance.Message", false, false),
            _ => ("!", "Operational.Error.Title", "Operational.Error.Message", false, true),
        };
        ShowOnly(StateSurface);
        LoadingIndicator.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        StateSymbolText.Text = symbol;
        StateTitleText.Text = Text(titleKey);
        StateMessageText.Text = Text(messageKey);
        RetryButton.Visibility = retry ? Visibility.Visible : Visibility.Collapsed;
        UpdatedAtText.Text = Text("Wpf.DemonstrationState");
        AutomationProperties.SetName(StateMessageText, $"{StateTitleText.Text}. {StateMessageText.Text}");
        RaiseStateSurfaceLiveRegionChanged();
    }

    /// <summary>Raises the explicit UI Automation event required for the polite operational-state live region.</summary>
    private void RaiseStateSurfaceLiveRegionChanged()
    {
        StateSurface.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            AutomationPeer? peer = UIElementAutomationPeer.FromElement(StateMessageText) ??
                UIElementAutomationPeer.CreatePeerForElement(StateMessageText);
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }));
    }

    /// <summary>Recreates local fixtures whose visible values depend on the active language.</summary>
    private void RebuildLocalisedData()
    {
        timelineSnapshot = CreateTimelineSnapshot(evidence.GeneratedAt);
        configurationSnapshot = CreateConfigurationSnapshot();
        RebuildInventoryDerivedData();
        ConfigurationGrid.ItemsSource = configurationSnapshot.Fields;
        RefreshGridHeaders();
    }

    /// <summary>Rebuilds only presentation collections derived from the current reconciled inventory snapshot.</summary>
    private void RebuildInventoryDerivedData()
    {
        Replace(providerRows, snapshot.Items
            .GroupBy(item => item.ProviderType, StringComparer.Ordinal)
            .Select(group => new ProviderDistributionItem(
                providerVisualIdentityPolicy.Resolve(group.Key),
                group.Count(),
                group.First().SupportLabel)));
        RefreshProviderCharts();
    }

    /// <summary>Re-resolves only decorative provider identities after theme or High Contrast changes.</summary>
    /// <param name="sender">Local provider-identity policy that observed the effective presentation change.</param>
    /// <param name="e">Identity-change event data.</param>
    private void ProviderVisualIdentityChanged(object? sender, EventArgs e)
    {
        Replace(rows, rows.Select(row => row with
        {
            ProviderIdentity = providerVisualIdentityPolicy.Resolve(row.ProviderType),
        }).ToArray());
        Replace(timelineRows, timelineRows.Select(row => row with
        {
            ProviderIdentity = providerVisualIdentityPolicy.Resolve(row.ProviderType),
        }).ToArray());
        Replace(alertRows, alertRows.Select(row => row with
        {
            ProviderIdentity = providerVisualIdentityPolicy.Resolve(row.ProviderType),
        }).ToArray());
        Replace(providerRows, providerRows.Select(row => row with
        {
            ProviderIdentity = providerVisualIdentityPolicy.Resolve(row.ProviderIdentity.ProviderType),
        }).ToArray());
        RefreshProviderCharts();

        if (configurationSnapshot is not null)
        {
            ConfigurationProviderIdentity.Identity = providerVisualIdentityPolicy.Resolve(configurationSnapshot.ProviderType);
        }
    }

    /// <summary>Atomically supplies both provider charts with the current visible count and identity snapshot.</summary>
    private void RefreshProviderCharts()
    {
        ProviderDistributionItem[] items = providerRows.ToArray();
        OverviewProviderDistribution.Items = items;
        ProvidersDistribution.Items = items;
    }

    /// <summary>Refreshes detached DataGrid column headers that do not inherit WPF dynamic-resource invalidation.</summary>
    private void RefreshGridHeaders()
    {
        SetHeaders(InventoryGrid, "Common.Instance", "Common.Provider", "Common.Support", "Common.Environment", "Common.Status", "Common.ObservedAtUtc", "Common.Latency", "View.Inventory.Title");
        SetHeaders(HistoryGrid, "Common.TimeUtc", "Common.Severity", "Common.Event", "Common.Instance", "Common.Provider", "Common.Summary", "History.Title");
        SetHeaders(AlertsGrid, "Common.Severity", "Common.State", "Common.Rule", "Common.Instance", "Common.Provider", "Common.Summary", "Common.Updated", "Alerts.Title");
        SetHeaders(ConfigurationGrid, "Common.Field", "Common.SafeValue", "Common.Description", "Wpf.ConfigurationTitle");
        SetHeaders(CapabilityGrid, "Common.Action", "Common.Capability", "Common.State", "Common.Reason", "Configuration.AdminTitle");
    }

    /// <summary>Assigns canonical translated labels to one grid while preserving its fixed column contract.</summary>
    private void SetHeaders(System.Windows.Controls.DataGrid grid, params string[] keys)
    {
        if (grid.Columns.Count != keys.Length)
        {
            throw new InvalidOperationException($"Localised header count does not match {grid.Name}.");
        }
        for (int index = 0; index < keys.Length; index++) grid.Columns[index].Header = Text(keys[index]);
    }

    /// <summary>Shows one content surface and collapses the remaining mutually exclusive surfaces.</summary>
    private void ShowOnly(FrameworkElement selected)
    {
        FrameworkElement[] surfaces = [OverviewSurface, ReadySurface, HistoryAlertSurface, PerformanceSurface, ConfigurationSurface, ProvidersSurface, SettingsSurface, StateSurface];
        foreach (FrameworkElement surface in surfaces)
        {
            surface.Visibility = ReferenceEquals(surface, selected) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>Updates the shared page heading from the current desktop destination without duplicating localised strings.</summary>
    private void UpdateViewHeading()
    {
        string key = currentView switch
        {
            DesktopView.Overview => "Overview",
            DesktopView.Inventory => "Inventory",
            DesktopView.Alerts => "Alerts",
            DesktopView.Performance => "Performance",
            DesktopView.History => "History",
            DesktopView.Configuration => "Configuration",
            DesktopView.Providers => "Providers",
            DesktopView.Settings => "Settings",
            _ => throw new ArgumentOutOfRangeException(nameof(currentView)),
        };
        ViewEyebrowText.Text = Text($"View.{key}.Eyebrow");
        ViewTitleText.Text = Text($"View.{key}.Title");
        ViewDescriptionText.Text = Text($"View.{key}.Description");
        Title = $"DB Notifier — {ViewTitleText.Text}";
    }

    /// <summary>Synchronises the eight navigation buttons with the current route and neutral cross-platform navigation semantics.</summary>
    private void UpdateNavigationState()
    {
        System.Windows.Controls.Button[] buttons = [OverviewNavigationButton, InventoryNavigationButton, AlertsNavigationButton, PerformanceNavigationButton, HistoryNavigationButton, ConfigurationNavigationButton, ProvidersNavigationButton, SettingsNavigationButton];
        foreach (System.Windows.Controls.Button button in buttons)
        {
            bool active = Enum.TryParse(button.Tag?.ToString(), false, out DesktopView view) && view == currentView;
            button.SetResourceReference(BackgroundProperty, active ? "ColourSelectionBackgroundBrush" : "ColourSurfaceSubtleBrush");
            button.SetResourceReference(ForegroundProperty, active ? "ColourSelectionForegroundBrush" : "ColourTextPrimaryBrush");
            button.SetResourceReference(BorderBrushProperty, active ? "ColourActionPrimaryBackgroundBrush" : "ColourSurfaceSubtleBrush");
            AutomationProperties.SetItemStatus(button, active ? Text("Navigation.Current") : string.Empty);
        }
    }

    /// <summary>Reflows all eight routes when the native window enters or leaves its compact width.</summary>
    /// <param name="sender">The resized desktop window.</param>
    /// <param name="e">The new and previous native layout sizes.</param>
    private void WindowSizeChanged(object sender, SizeChangedEventArgs e) => UpdateResponsiveLayout();

    /// <summary>Applies the WPF compact layout without changing route, data or accessibility semantics.</summary>
    private void UpdateResponsiveLayout()
    {
        double availableWidth = ActualWidth > 0 ? ActualWidth : Width;
        // The fixed native navigation rail leaves materially less room than the outer window width suggests.
        bool compact = availableWidth < 1100;
        bool compactInventorySummary = availableWidth < 1240;
        OverviewSummaryGrid.Columns = compact ? 2 : 4;
        InventorySummaryGrid.Columns = compact ? 2 : compactInventorySummary ? 3 : 6;
        AlertSummaryGrid.Columns = compact ? 1 : 3;
        DesktopContentRoot.Margin = compact ? new Thickness(20, 22, 20, 22) : new Thickness(32, 28, 32, 28);
        SidebarStateCard.Visibility = ActualHeight > 0 && ActualHeight < 700 ? Visibility.Collapsed : Visibility.Visible;
        Grid.SetRow(DesktopScenarioPanel, compact ? 1 : 0);
        Grid.SetColumn(DesktopScenarioPanel, compact ? 0 : 1);
        Grid.SetColumnSpan(DesktopScenarioPanel, compact ? 2 : 1);
        DesktopScenarioPanel.Margin = compact ? new Thickness(0, 16, 0, 0) : new Thickness(24, 0, 0, 0);
        DesktopScenarioPanel.HorizontalAlignment = compact ? System.Windows.HorizontalAlignment.Left : System.Windows.HorizontalAlignment.Right;
        ArrangeAdaptivePair(OverviewFleetPanel, OverviewAlertsPanel, compact);
        ArrangeAdaptivePair(OverviewPerformancePanel, OverviewProvidersPanel, compact);
        ArrangeAdaptivePair(SettingsPreferencePanel, SettingsNotificationsPanel, compact);
        ArrangeConfigurationHeaders(compact);
        ConfigureResponsiveGrid(InventoryGrid, 7, 48, compact);
        ConfigureResponsiveGrid(HistoryGrid, 6, 46, compact);
        ConfigureResponsiveGrid(AlertsGrid, 7, 52, compact);
        ConfigureResponsiveGrid(ConfigurationGrid, 3, 46, compact);
        ConfigureResponsiveGrid(CapabilityGrid, 4, 46, compact);

        if (compactLayoutApplied != compact)
        {
            OverviewInventoryList.ItemTemplate = (DataTemplate)FindResource(compact
                ? "OverviewInventoryCompactRowTemplate"
                : "OverviewInventoryDesktopRowTemplate");
            ProviderCatalogueList.ItemsPanel = (ItemsPanelTemplate)FindResource(compact
                ? "ProviderCatalogueCompactPanel"
                : "ProviderCatalogueDesktopPanel");
            compactLayoutApplied = compact;
        }
    }

    /// <summary>Places configuration metadata, permission controls and safe review actions without horizontal page overflow.</summary>
    /// <param name="compact">Whether header controls must follow their associated explanatory text.</param>
    private void ArrangeConfigurationHeaders(bool compact)
    {
        Grid.SetRow(ConfigurationNotPersistedText, compact ? 1 : 0);
        Grid.SetColumn(ConfigurationNotPersistedText, compact ? 0 : 1);
        Grid.SetColumnSpan(ConfigurationNotPersistedText, compact ? 2 : 1);
        ConfigurationNotPersistedText.HorizontalAlignment = compact ? System.Windows.HorizontalAlignment.Left : System.Windows.HorizontalAlignment.Right;
        ConfigurationNotPersistedText.Margin = compact ? new Thickness(0, 8, 0, 0) : new Thickness(0);

        Grid.SetRow(PermissionSelector, compact ? 1 : 0);
        Grid.SetColumn(PermissionSelector, compact ? 0 : 1);
        Grid.SetColumnSpan(PermissionSelector, compact ? 2 : 1);
        PermissionSelector.HorizontalAlignment = compact ? System.Windows.HorizontalAlignment.Left : System.Windows.HorizontalAlignment.Right;
        PermissionSelector.Margin = compact ? new Thickness(0, 12, 0, 0) : new Thickness(0);

        CapabilityActionsPanel.HorizontalAlignment = compact ? System.Windows.HorizontalAlignment.Stretch : System.Windows.HorizontalAlignment.Right;
        ReviewCapabilityButton.Margin = compact ? new Thickness(0, 0, 10, 8) : new Thickness(0, 0, 10, 0);
        PreviewConfirmationButton.Margin = compact ? new Thickness(0, 0, 0, 8) : new Thickness(0);
    }

    /// <summary>Switches one read-only table between desktop columns and a complete stacked compact record.</summary>
    /// <param name="grid">The route table whose item and automation identity remain unchanged.</param>
    /// <param name="desktopColumnCount">Number of desktop columns preceding the single compact template column.</param>
    /// <param name="minimumRowHeight">Minimum touch and reading target retained while wrapped content grows vertically.</param>
    /// <param name="compact">Whether the complete compact record must be shown.</param>
    /// <exception cref="InvalidOperationException">Thrown when the XAML column contract no longer contains one compact column after the desktop columns.</exception>
    private static void ConfigureResponsiveGrid(System.Windows.Controls.DataGrid grid, int desktopColumnCount, double minimumRowHeight, bool compact)
    {
        if (grid.Columns.Count != desktopColumnCount + 1)
        {
            throw new InvalidOperationException($"Responsive column count does not match {grid.Name}.");
        }

        for (int index = 0; index < desktopColumnCount; index++)
        {
            grid.Columns[index].Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        }

        grid.Columns[desktopColumnCount].Visibility = compact ? Visibility.Visible : Visibility.Collapsed;
        grid.HeadersVisibility = compact ? DataGridHeadersVisibility.None : DataGridHeadersVisibility.Column;
        grid.RowHeight = double.NaN;
        grid.MinRowHeight = minimumRowHeight;
        ScrollViewer.SetHorizontalScrollBarVisibility(grid, compact
            ? ScrollBarVisibility.Disabled
            : ScrollBarVisibility.Auto);
    }

    /// <summary>Places a related pair side by side at comfortable widths and in one readable column when compact.</summary>
    /// <param name="primary">The larger or first panel in reading order.</param>
    /// <param name="secondary">The supporting panel that follows the primary panel.</param>
    /// <param name="compact">Whether both panels must span the available content width.</param>
    private static void ArrangeAdaptivePair(FrameworkElement primary, FrameworkElement secondary, bool compact)
    {
        Grid.SetRow(primary, 0);
        Grid.SetColumn(primary, 0);
        Grid.SetColumnSpan(primary, compact ? 3 : 1);
        Grid.SetRow(secondary, compact ? 1 : 0);
        Grid.SetColumn(secondary, compact ? 0 : 2);
        Grid.SetColumnSpan(secondary, compact ? 3 : 1);
        secondary.Margin = compact ? new Thickness(0, 16, 0, 0) : new Thickness(0);
    }

    /// <summary>Replaces observable rows as one local presentation update.</summary>
    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (T item in items) target.Add(item);
    }

    /// <summary>Formats UTC timestamps in a stable sortable shape under the selected culture.</summary>
    private string FormatUtc(DateTimeOffset value) => DesktopDateTimePresentation.FormatUtc(value, localisation.Culture);

    /// <summary>Resolves one canonical generated resource through the desktop localisation owner.</summary>
    private string Text(string key, params object[] values) => localisation.Text(key, values);

    /// <summary>Creates local event and alert fixtures without delivery, acknowledgement or mutation.</summary>
    private TimelineAlertSnapshot CreateTimelineSnapshot(DateTimeOffset now) => new(
        TimelineAlertSnapshot.CurrentSchemaVersion,
        now,
        [
            CreateEvent("00000000-0000-0000-0001-000000000001", Text("Sample.Instance.Finance"), "postgresql", "Recovered", EventSeverity.Information, Text("Sample.Event.Recovered"), now.AddMinutes(-2)),
            CreateEvent("00000000-0000-0000-0001-000000000002", Text("Sample.Instance.Orders"), "mysql", "Degraded", EventSeverity.Warning, Text("Sample.Event.Degraded"), now.AddMinutes(-7)),
            CreateEvent("00000000-0000-0000-0001-000000000003", Text("Sample.Instance.Analytics"), "sql-server", "Timeout", EventSeverity.Critical, Text("Sample.Event.Timeout"), now.AddMinutes(-14)),
            CreateEvent("00000000-0000-0000-0001-000000000004", Text("Sample.Instance.Catalogue"), "mongodb", "MaintenanceStarted", EventSeverity.Information, Text("Sample.Event.Maintenance"), now.AddMinutes(-24)),
        ],
        [
            CreateAlert("00000000-0000-0000-0002-000000000001", Text("Sample.Instance.Analytics"), "sql-server", EventSeverity.Critical, AlertPresentationState.Active, Text("Sample.Alert.TimeoutRule"), Text("Sample.Alert.TimeoutSummary"), now.AddMinutes(-3)),
            CreateAlert("00000000-0000-0000-0002-000000000002", Text("Sample.Instance.Orders"), "mysql", EventSeverity.Warning, AlertPresentationState.Acknowledged, Text("Sample.Alert.LatencyRule"), Text("Sample.Alert.LatencySummary"), now.AddMinutes(-8)),
            CreateAlert("00000000-0000-0000-0002-000000000003", Text("Sample.Instance.Catalogue"), "mongodb", EventSeverity.Information, AlertPresentationState.Silenced, Text("Sample.Alert.MaintenanceRule"), Text("Sample.Alert.MaintenanceSummary"), now.AddMinutes(-24)),
        ]);

    /// <summary>Creates non-secret fields and unavailable administrative controls for UX review.</summary>
    private ConfigurationCapabilitySnapshot CreateConfigurationSnapshot() => new(
        ConfigurationCapabilitySnapshot.CurrentSchemaVersion,
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        Text("Sample.Instance.Finance"),
        "postgresql",
        [
            new("monitoring.interval", Text("Sample.Config.Interval.Label"), Text("Sample.Config.Interval.Value"), Text("Sample.Config.Interval.Description")),
            new("monitoring.timeout", Text("Sample.Config.Timeout.Label"), Text("Sample.Config.Timeout.Value"), Text("Sample.Config.Timeout.Description")),
            new("monitoring.retry", Text("Sample.Config.Retry.Label"), Text("Sample.Config.Retry.Value"), Text("Sample.Config.Retry.Description")),
            new("credential.reference", Text("Sample.Config.Credential.Label"), Text("Sample.Config.Credential.Value"), Text("Sample.Config.Credential.Description")),
        ],
        [
            new("service.start", Text("Action.Start"), CapabilityPresentationState.Unsupported, "provider.control_unsupported", true),
            new("service.stop", Text("Action.Stop"), CapabilityPresentationState.Unsupported, "provider.control_unsupported", true),
            new("service.restart", Text("Action.Restart"), CapabilityPresentationState.Unsupported, "provider.control_unsupported", true),
        ]);

    /// <summary>Creates a canonical event fixture with deterministic identifiers.</summary>
    private static TimelineEventItem CreateEvent(string id, string name, string provider, string type, EventSeverity severity, string summary, DateTimeOffset occurredAt) =>
        new(Guid.Parse(id), Guid.Parse(id.Replace("0001-", "0000-")), name, provider, type, severity, summary, occurredAt, occurredAt);

    /// <summary>Creates a canonical alert fixture with deterministic identifiers.</summary>
    private static AlertPresentationItem CreateAlert(string id, string name, string provider, EventSeverity severity, AlertPresentationState state, string rule, string summary, DateTimeOffset updatedAt) =>
        new(Guid.Parse(id), Guid.Parse(id.Replace("0002-", "0000-")), name, provider, severity, state, rule, summary, updatedAt.AddMinutes(-10), updatedAt);

    /// <summary>Represents one localised inventory grid row.</summary>
    private sealed record InventoryRow(
        string InstanceId,
        string DisplayName,
        string ProviderType,
        ProviderVisualIdentity ProviderIdentity,
        string SupportLabel,
        string Environment,
        string StatusLabel,
        OperationalTone StatusTone,
        SemanticIconKind StatusIcon,
        OperationalTone SparklineTone,
        PointCollection SparklinePoints,
        string ObservedAtLabel,
        string LatencyLabel)
    {
        /// <summary>Maps a canonical inventory item to non-colour-only localised presentation.</summary>
        /// <param name="item">Canonical provider-neutral inventory evidence.</param>
        /// <param name="index">Stable fixture position used only to select deterministic visual sparkline geometry.</param>
        /// <param name="now">UTC evaluation instant used for freshness classification.</param>
        /// <param name="staleAfter">Maximum evidence age before the row is explicitly stale.</param>
        /// <param name="localisation">Desktop localisation owner for visible labels.</param>
        /// <param name="providerVisualIdentityPolicy">Local decorative provider-identity resolver.</param>
        /// <returns>A localised row that keeps provider identity separate from operational health.</returns>
        public static InventoryRow From(
            InstanceInventoryItem item,
            int index,
            DateTimeOffset now,
            TimeSpan staleAfter,
            DesktopLocalisationService localisation,
            ProviderVisualIdentityPolicy providerVisualIdentityPolicy)
        {
            EvidenceFreshness freshness = item.GetFreshness(now, staleAfter);
            string status = !item.Enabled
                ? localisation.Text("Status.Disabled")
                : freshness == EvidenceFreshness.Stale
                ? localisation.Text("Status.Stale")
                : freshness == EvidenceFreshness.Unknown
                    ? localisation.Text("Status.Unknown")
                    : item.Status switch
                    {
                        HealthStatus.Healthy => localisation.Text("Status.Healthy"),
                        HealthStatus.Degraded => localisation.Text("Status.Degraded"),
                        HealthStatus.Unavailable => localisation.Text("Status.Unavailable"),
                        HealthStatus.AuthFailed => localisation.Text("Status.AuthFailed"),
                        HealthStatus.Timeout => localisation.Text("Status.Timeout"),
                        HealthStatus.Maintenance => localisation.Text("Status.Maintenance"),
                        _ => localisation.Text("Status.Unknown"),
                    };
            OperationalTone healthTone = item.Status switch
            {
                HealthStatus.Healthy => OperationalTone.Healthy,
                HealthStatus.Degraded => OperationalTone.Degraded,
                HealthStatus.Unavailable or HealthStatus.AuthFailed or HealthStatus.Timeout => OperationalTone.Critical,
                HealthStatus.Maintenance => OperationalTone.Maintenance,
                _ => OperationalTone.Neutral,
            };
            OperationalTone statusTone = !item.Enabled || freshness is EvidenceFreshness.Stale or EvidenceFreshness.Unknown
                ? OperationalTone.Neutral
                : healthTone;
            SemanticIconKind statusIcon = !item.Enabled
                ? SemanticIconKind.Disabled
                : freshness == EvidenceFreshness.Stale
                    ? SemanticIconKind.Stale
                    : freshness == EvidenceFreshness.Unknown
                        ? SemanticIconKind.Unknown
                        : item.Status switch
                        {
                            HealthStatus.Healthy => SemanticIconKind.Healthy,
                            HealthStatus.Degraded => SemanticIconKind.Degraded,
                            HealthStatus.Unavailable or HealthStatus.AuthFailed or HealthStatus.Timeout => SemanticIconKind.Critical,
                            HealthStatus.Maintenance => SemanticIconKind.Settings,
                            _ => SemanticIconKind.Unknown,
                        };
            string latency = item.Latency is null ? "—" : string.Create(CultureInfo.InvariantCulture, $"{item.Latency.Value.TotalMilliseconds:0} ms");
            string observedAt = freshness == EvidenceFreshness.Unknown
                ? localisation.Text("Status.Unknown")
                : DesktopDateTimePresentation.FormatUtc(item.ObservedAt, localisation.Culture);
            return new(
                item.InstanceId.ToString("D", CultureInfo.InvariantCulture),
                item.DisplayName,
                item.ProviderType,
                providerVisualIdentityPolicy.Resolve(item.ProviderType),
                item.SupportLabel,
                item.Environment,
                status,
                statusTone,
                statusIcon,
                item.Enabled ? healthTone : OperationalTone.Neutral,
                CreateSparkline(index, item.Enabled),
                observedAt,
                latency);
        }

        /// <summary>Creates one bounded deterministic sparkline without introducing a telemetry or data contract.</summary>
        /// <param name="index">Stable fixture position used to select an existing visual-series shape.</param>
        /// <param name="enabled">Whether the instance may display variation or must remain a flat disabled line.</param>
        /// <returns>A frozen 92-by-28 point collection safe for reuse by the read-only row template.</returns>
        private static PointCollection CreateSparkline(int index, bool enabled)
        {
            string points = !enabled
                ? "0,15 92,15"
                : (index % 3) switch
                {
                    0 => "0,20 12,18 24,21 36,14 48,16 60,10 72,14 82,11 92,16",
                    1 => "0,17 12,15 24,18 36,11 48,14 60,9 72,18 82,13 92,15",
                    _ => "0,10 12,14 24,9 36,17 48,12 60,19 72,15 82,20 92,16",
                };
            PointCollection collection = PointCollection.Parse(points);
            collection.Freeze();
            return collection;
        }
    }

    /// <summary>Represents one localised event timeline row.</summary>
    private sealed record TimelineRow(
        string OccurredAtLabel,
        string SeverityLabel,
        OperationalTone Tone,
        SemanticIconKind IconKind,
        string EventType,
        string InstanceName,
        string ProviderType,
        ProviderVisualIdentity ProviderIdentity,
        string Summary)
    {
        /// <summary>Maps canonical event severity without altering its provider-neutral event type.</summary>
        /// <param name="item">Canonical provider-neutral event evidence.</param>
        /// <param name="localisation">Desktop localisation owner for visible labels.</param>
        /// <param name="providerVisualIdentityPolicy">Local decorative provider-identity resolver.</param>
        /// <returns>A localised event row with a visible stable provider identifier.</returns>
        public static TimelineRow From(
            TimelineEventItem item,
            DesktopLocalisationService localisation,
            ProviderVisualIdentityPolicy providerVisualIdentityPolicy) => new(
            item.OccurredAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", localisation.Culture),
            item.Severity switch
            {
                EventSeverity.Critical => localisation.Text("Severity.Critical"),
                EventSeverity.Warning => localisation.Text("Severity.Warning"),
                EventSeverity.Information => localisation.Text("Severity.Information"),
                _ => localisation.Text("Status.Unknown"),
            },
            item.Severity switch
            {
                EventSeverity.Critical => OperationalTone.Critical,
                EventSeverity.Warning => OperationalTone.Degraded,
                EventSeverity.Information => OperationalTone.Information,
                _ => OperationalTone.Neutral,
            },
            item.Severity switch
            {
                EventSeverity.Critical => SemanticIconKind.Critical,
                EventSeverity.Warning => SemanticIconKind.Degraded,
                EventSeverity.Information => SemanticIconKind.Notification,
                _ => SemanticIconKind.Unknown,
            },
            item.EventType,
            item.InstanceName,
            item.ProviderType,
            providerVisualIdentityPolicy.Resolve(item.ProviderType),
            item.Summary);
    }

    /// <summary>Represents one localised alert grid row.</summary>
    private sealed record AlertRow(
        string SeverityLabel,
        OperationalTone Tone,
        SemanticIconKind IconKind,
        string StateLabel,
        string RuleName,
        string InstanceName,
        string ProviderType,
        ProviderVisualIdentity ProviderIdentity,
        string Summary,
        string UpdatedAtLabel)
    {
        /// <summary>Maps canonical alert state and severity to localised display labels.</summary>
        /// <param name="item">Canonical provider-neutral alert evidence.</param>
        /// <param name="localisation">Desktop localisation owner for visible labels.</param>
        /// <param name="providerVisualIdentityPolicy">Local decorative provider-identity resolver.</param>
        /// <returns>A localised alert row with provider identity distinct from alert severity.</returns>
        public static AlertRow From(
            AlertPresentationItem item,
            DesktopLocalisationService localisation,
            ProviderVisualIdentityPolicy providerVisualIdentityPolicy) => new(
            item.Severity switch
            {
                EventSeverity.Critical => localisation.Text("Severity.Critical"),
                EventSeverity.Warning => localisation.Text("Severity.Warning"),
                EventSeverity.Information => localisation.Text("Severity.Information"),
                _ => localisation.Text("Status.Unknown"),
            },
            item.Severity switch
            {
                EventSeverity.Critical => OperationalTone.Critical,
                EventSeverity.Warning => OperationalTone.Degraded,
                EventSeverity.Information => OperationalTone.Information,
                _ => OperationalTone.Neutral,
            },
            item.Severity switch
            {
                EventSeverity.Critical => SemanticIconKind.Critical,
                EventSeverity.Warning => SemanticIconKind.Degraded,
                EventSeverity.Information => SemanticIconKind.Notification,
                _ => SemanticIconKind.Unknown,
            },
            item.State switch
            {
                AlertPresentationState.Active => localisation.Text("AlertState.Active"),
                AlertPresentationState.Acknowledged => localisation.Text("AlertState.Acknowledged"),
                AlertPresentationState.Silenced => localisation.Text("AlertState.Silenced"),
                AlertPresentationState.Resolved => localisation.Text("AlertState.Resolved"),
                _ => localisation.Text("Status.Unknown"),
            },
            item.RuleName,
            item.InstanceName,
            item.ProviderType,
            providerVisualIdentityPolicy.Resolve(item.ProviderType),
            item.Summary,
            $"{DesktopDateTimePresentation.FormatUtc(item.UpdatedAt, localisation.Culture)} UTC");
    }

    /// <summary>Represents one fail-closed capability decision row.</summary>
    private sealed record CapabilityRow(string CapabilityId, string DisplayName, string StateLabel, string ReasonCode)
    {
        /// <summary>Maps canonical capability states without implying unavailable support.</summary>
        public static CapabilityRow From(CapabilityPresentation item, DesktopLocalisationService localisation) => new(
            item.CapabilityId, item.DisplayName,
            item.State switch
            {
                CapabilityPresentationState.Unsupported => localisation.Text("Configuration.Unsupported"),
                CapabilityPresentationState.Unavailable => $"↯ {localisation.Text("Status.Unavailable")}",
                CapabilityPresentationState.Supported => localisation.Text("Common.Support"),
                _ => $"? {localisation.Text("Status.Unknown")}",
            }, item.ReasonCode);
    }
}
