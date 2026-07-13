// Module purpose: Presents local demonstration inventory, history, alerts and capability decisions through the localised WPF shell.
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Presents read-only demonstration data and accessible operational states without connecting to databases or executing commands.
/// Localised presentation values are rebuilt whenever the user changes the supported interface language;
/// theme changes remain isolated to generated semantic resources owned by the desktop theme service.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);
    private readonly DesktopLocalisationService localisation;
    private readonly DesktopThemeService theme;
    private readonly ObservableCollection<InventoryRow> rows = [];
    private readonly ObservableCollection<TimelineRow> timelineRows = [];
    private readonly ObservableCollection<AlertRow> alertRows = [];
    private readonly ObservableCollection<CapabilityRow> capabilityRows = [];
    private InventorySnapshot snapshot = null!;
    private TimelineAlertSnapshot timelineSnapshot = null!;
    private ConfigurationCapabilitySnapshot configurationSnapshot = null!;

    /// <summary>Initialises the local demonstration surface with the already loaded language and theme preferences.</summary>
    /// <param name="localisation">Desktop localisation owner shared with the application and tray controller.</param>
    /// <param name="theme">Desktop theme owner shared with the application.</param>
    internal MainWindow(DesktopLocalisationService localisation, DesktopThemeService theme)
    {
        this.localisation = localisation;
        this.theme = theme;
        InitializeComponent();
        UpdatePreferenceButtons();
        InventoryGrid.ItemsSource = rows;
        HistoryGrid.ItemsSource = timelineRows;
        AlertsGrid.ItemsSource = alertRows;
        CapabilityGrid.ItemsSource = capabilityRows;
        localisation.LanguageChanged += LocalisationLanguageChanged;
        theme.ThemeChanged += ThemeChanged;
        Closed += (_, _) =>
        {
            localisation.LanguageChanged -= LocalisationLanguageChanged;
            theme.ThemeChanged -= ThemeChanged;
        };
        RebuildLocalisedData();
        PresentReadyState();
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

    /// <summary>Cycles System, Light and Dark while preserving the validated theme preference contract.</summary>
    /// <param name="sender">The single theme preference button.</param>
    /// <param name="e">Button activation event data.</param>
    private void ThemePreferenceButtonClick(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        ThemePreference next = theme.CurrentPreference switch
        {
            ThemePreference.System => ThemePreference.Light,
            ThemePreference.Light => ThemePreference.Dark,
            _ => ThemePreference.System,
        };
        theme.SetPreference(next);
    }

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

        ThemePreference nextTheme = theme.CurrentPreference switch
        {
            ThemePreference.System => ThemePreference.Light,
            ThemePreference.Light => ThemePreference.Dark,
            _ => ThemePreference.System,
        };
        string themeLabel = Text("Theme.Toggle", ThemeLabel(theme.CurrentPreference), ThemeLabel(nextTheme));
        ThemePreferenceButton.ToolTip = themeLabel;
        System.Windows.Automation.AutomationProperties.SetName(ThemePreferenceButton, themeLabel);
        System.Windows.Automation.AutomationProperties.SetHelpText(ThemePreferenceButton, themeLabel);
        SystemThemeIcon.Visibility = theme.CurrentPreference == ThemePreference.System ? Visibility.Visible : Visibility.Collapsed;
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
    private string ThemeLabel(ThemePreference preference) => Text(preference switch
    {
        ThemePreference.Light => "Theme.Light",
        ThemePreference.Dark => "Theme.Dark",
        _ => "Theme.System",
    });

    /// <summary>Refreshes the theme icon and accessible state after explicit or system-derived theme changes.</summary>
    /// <param name="sender">Theme service that applied the new effective state.</param>
    /// <param name="e">Theme-change event data.</param>
    private void ThemeChanged(object? sender, EventArgs e) => UpdatePreferenceButtons();

    /// <summary>Rebuilds fixture-derived labels after the generated resource dictionary changes.</summary>
    private void LocalisationLanguageChanged(object? sender, EventArgs e)
    {
        UpdatePreferenceButtons();
        RebuildLocalisedData();
        if (ScenarioSelector.SelectedItem is ComboBoxItem selected &&
            Enum.TryParse(selected.Tag?.ToString(), false, out InventorySurfaceState state) &&
            state != InventorySurfaceState.Ready)
        {
            PresentNonReadyState(state);
        }
        else
        {
            PresentReadyState();
        }
    }

    /// <summary>Refreshes the selected ready view when navigation changes.</summary>
    private void ViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsInitialized && ScenarioSelector.SelectedIndex == 0)
        {
            PresentReadyState();
        }
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
            System.Windows.MessageBox.Show(this, Text("Configuration.SelectCapability"), "DB Notifier", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        bool authorised = PermissionSelector.SelectedItem is ComboBoxItem permission &&
            string.Equals(permission.Tag?.ToString(), "Authorized", StringComparison.Ordinal);
        ShowPreview(configurationSnapshot.Preview(selected.CapabilityId, authorised));
    }

    /// <summary>Shows the safe confirmation UX example without enabling execution.</summary>
    private void PreviewConfirmationClick(object sender, RoutedEventArgs e) =>
        ShowPreview(ActionPreviewDisposition.ConfirmationRequired);

    /// <summary>Maps a canonical preview disposition to generated localised dialog content.</summary>
    private void ShowPreview(ActionPreviewDisposition disposition)
    {
        (string titleKey, string messageKey) = disposition switch
        {
            ActionPreviewDisposition.ConfirmationRequired => ("Preview.Confirmation.Title", "Preview.Confirmation.Message"),
            ActionPreviewDisposition.Denied => ("Preview.Denied.Title", "Preview.Denied.Message"),
            ActionPreviewDisposition.Unsupported => ("Preview.Unsupported.Title", "Preview.Unsupported.Message"),
            ActionPreviewDisposition.Unavailable => ("Preview.Unavailable.Title", "Preview.Unavailable.Message"),
            _ => ("Preview.Unknown.Title", "Preview.Unknown.Message"),
        };
        System.Windows.MessageBox.Show(this, Text(messageKey), Text(titleKey), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>Presents inventory, history and alerts, or configuration according to the selected view.</summary>
    private void PresentReadyState()
    {
        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        string? view = (ViewSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (view == "HistoryAlerts")
        {
            PresentHistoryAndAlerts(now);
            return;
        }
        if (view == "Configuration")
        {
            PresentConfiguration(now);
            return;
        }

        InventoryStatusSummary summary = snapshot.Summarize(now, StaleAfter);
        Replace(rows, snapshot.Items.Select(item => InventoryRow.From(item, now, StaleAfter, localisation)));
        UpdatedAtText.Text = Text("Wpf.UpdatedAt", FormatUtc(now));
        TotalCountText.Text = summary.Total.ToString(localisation.Culture);
        HealthyCountText.Text = summary.Healthy.ToString(localisation.Culture);
        DegradedCountText.Text = summary.Degraded.ToString(localisation.Culture);
        AttentionCountText.Text = summary.AttentionRequired.ToString(localisation.Culture);
        StaleCountText.Text = summary.Stale.ToString(localisation.Culture);
        ShowOnly(ReadySurface);
    }

    /// <summary>Presents local event and alert fixtures using localised severity and state labels.</summary>
    private void PresentHistoryAndAlerts(DateTimeOffset now)
    {
        Replace(timelineRows, timelineSnapshot.Events.Select(item => TimelineRow.From(item, localisation)));
        Replace(alertRows, timelineSnapshot.Alerts.Select(item => AlertRow.From(item, localisation)));
        AlertStatusSummary summary = timelineSnapshot.Summarize();
        AlertSummaryText.Text = Text("Wpf.AlertSummary", summary.Active, summary.UnresolvedCritical);
        UpdatedAtText.Text = Text("Wpf.UpdatedFromAdapters", FormatUtc(now));
        ShowOnly(HistoryAlertSurface);
    }

    /// <summary>Presents safe configuration fields and unsupported capability decisions.</summary>
    private void PresentConfiguration(DateTimeOffset now)
    {
        Replace(capabilityRows, configurationSnapshot.Capabilities.Select(item => CapabilityRow.From(item, localisation)));
        ConfigurationGrid.ItemsSource = configurationSnapshot.Fields;
        ConfigurationInstanceText.Text = $"{configurationSnapshot.InstanceName} · {configurationSnapshot.ProviderType}";
        CapabilityGrid.SelectedIndex = capabilityRows.Count > 0 ? 0 : -1;
        UpdatedAtText.Text = Text("Wpf.LocalPreview", FormatUtc(now));
        ShowOnly(ConfigurationSurface);
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
    }

    /// <summary>Recreates all local fixtures whose visible values depend on the active language.</summary>
    private void RebuildLocalisedData()
    {
        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        snapshot = CreateDemonstrationSnapshot(now);
        timelineSnapshot = CreateTimelineSnapshot(now);
        configurationSnapshot = CreateConfigurationSnapshot();
        ConfigurationGrid.ItemsSource = configurationSnapshot.Fields;
        RefreshGridHeaders();
    }

    /// <summary>Refreshes detached DataGrid column headers that do not inherit WPF dynamic-resource invalidation.</summary>
    private void RefreshGridHeaders()
    {
        SetHeaders(InventoryGrid, "Common.Instance", "Common.Provider", "Common.Support", "Common.Environment", "Common.Status", "Common.ObservedAt", "Common.Latency");
        SetHeaders(HistoryGrid, "Common.TimeUtc", "Common.Severity", "Common.Event", "Common.Instance", "Common.Provider", "Common.Summary");
        SetHeaders(AlertsGrid, "Common.Severity", "Common.State", "Common.Rule", "Common.Instance", "Common.Provider", "Common.Summary");
        SetHeaders(ConfigurationGrid, "Common.Field", "Common.SafeValue", "Common.Description");
        SetHeaders(CapabilityGrid, "Common.Action", "Common.Capability", "Common.State", "Common.Reason");
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
        FrameworkElement[] surfaces = [ReadySurface, HistoryAlertSurface, ConfigurationSurface, StateSurface];
        foreach (FrameworkElement surface in surfaces)
        {
            surface.Visibility = ReferenceEquals(surface, selected) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>Replaces observable rows as one local presentation update.</summary>
    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (T item in items) target.Add(item);
    }

    /// <summary>Formats UTC timestamps in a stable sortable shape under the selected culture.</summary>
    private string FormatUtc(DateTimeOffset value) => value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", localisation.Culture);

    /// <summary>Resolves one canonical generated resource through the desktop localisation owner.</summary>
    private string Text(string key, params object[] values) => localisation.Text(key, values);

    /// <summary>Creates the provider-neutral inventory demonstration fixture with localised visible fields.</summary>
    private InventorySnapshot CreateDemonstrationSnapshot(DateTimeOffset now) => new(
        InventorySnapshot.CurrentSchemaVersion,
        now,
        [
            CreateItem("00000000-0000-0000-0000-000000000001", Text("Sample.Instance.Finance"), "postgresql", Text("Sample.Support.Implemented"), Text("Sample.Environment.Production"), Text("Sample.Location.Datacentre"), HealthStatus.Healthy, now.AddSeconds(-38), 24),
            CreateItem("00000000-0000-0000-0000-000000000002", Text("Sample.Instance.Orders"), "mysql", Text("Sample.Support.Planned"), Text("Sample.Environment.Production"), Text("Sample.Location.PrivateCloud"), HealthStatus.Degraded, now.AddMinutes(-2), 86),
            CreateItem("00000000-0000-0000-0000-000000000003", Text("Sample.Instance.Analytics"), "sql-server", Text("Sample.Support.Planned"), Text("Sample.Environment.Validation"), "Azure", HealthStatus.Timeout, now.AddMinutes(-3), null),
            CreateItem("00000000-0000-0000-0000-000000000004", Text("Sample.Instance.Catalogue"), "mongodb", Text("Sample.Support.Planned"), Text("Sample.Environment.Development"), Text("Sample.Location.LocalLinux"), HealthStatus.Unknown, now.AddMinutes(-9), null),
        ]);

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

    /// <summary>Creates one inventory fixture without embedding credentials or native provider details.</summary>
    private static InstanceInventoryItem CreateItem(string id, string name, string provider, string support, string environment, string location, HealthStatus status, DateTimeOffset receivedAt, double? latencyMilliseconds) =>
        new(Guid.Parse(id), name, provider, support, environment, location, status, receivedAt.AddSeconds(-1), receivedAt,
            latencyMilliseconds is null ? null : TimeSpan.FromMilliseconds(latencyMilliseconds.Value), true);

    /// <summary>Represents one localised inventory grid row.</summary>
    private sealed record InventoryRow(string DisplayName, string ProviderType, string SupportLabel, string Environment, string StatusLabel, string ObservedAtLabel, string LatencyLabel)
    {
        /// <summary>Maps a canonical inventory item to non-colour-only localised presentation.</summary>
        public static InventoryRow From(InstanceInventoryItem item, DateTimeOffset now, TimeSpan staleAfter, DesktopLocalisationService localisation)
        {
            string status = item.IsStale(now, staleAfter) ? $"◷ {localisation.Text("Status.Stale")}" : item.Status switch
            {
                HealthStatus.Healthy => $"● {localisation.Text("Status.Healthy")}",
                HealthStatus.Degraded => $"▲ {localisation.Text("Status.Degraded")}",
                HealthStatus.Unavailable => $"■ {localisation.Text("Status.Unavailable")}",
                HealthStatus.AuthFailed => $"■ {localisation.Text("Status.AuthFailed")}",
                HealthStatus.Timeout => $"■ {localisation.Text("Status.Timeout")}",
                HealthStatus.Maintenance => $"◆ {localisation.Text("Status.Maintenance")}",
                _ => $"○ {localisation.Text("Status.Unknown")}",
            };
            string latency = item.Latency is null ? "—" : string.Create(CultureInfo.InvariantCulture, $"{item.Latency.Value.TotalMilliseconds:0} ms");
            return new(item.DisplayName, item.ProviderType, item.SupportLabel, item.Environment, status,
                item.ObservedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", localisation.Culture), latency);
        }
    }

    /// <summary>Represents one localised event timeline row.</summary>
    private sealed record TimelineRow(string OccurredAtLabel, string SeverityLabel, string EventType, string InstanceName, string ProviderType, string Summary)
    {
        /// <summary>Maps canonical event severity without altering its provider-neutral event type.</summary>
        public static TimelineRow From(TimelineEventItem item, DesktopLocalisationService localisation) => new(
            item.OccurredAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", localisation.Culture),
            item.Severity switch
            {
                EventSeverity.Critical => $"■ {localisation.Text("Severity.Critical")}",
                EventSeverity.Warning => $"▲ {localisation.Text("Severity.Warning")}",
                _ => $"● {localisation.Text("Severity.Information")}",
            }, item.EventType, item.InstanceName, item.ProviderType, item.Summary);
    }

    /// <summary>Represents one localised alert grid row.</summary>
    private sealed record AlertRow(string SeverityLabel, string StateLabel, string RuleName, string InstanceName, string ProviderType, string Summary)
    {
        /// <summary>Maps canonical alert state and severity to localised display labels.</summary>
        public static AlertRow From(AlertPresentationItem item, DesktopLocalisationService localisation) => new(
            item.Severity switch
            {
                EventSeverity.Critical => $"■ {localisation.Text("Severity.Critical")}",
                EventSeverity.Warning => $"▲ {localisation.Text("Severity.Warning")}",
                _ => $"● {localisation.Text("Severity.Information")}",
            },
            item.State switch
            {
                AlertPresentationState.Active => localisation.Text("AlertState.Active"),
                AlertPresentationState.Acknowledged => localisation.Text("AlertState.Acknowledged"),
                AlertPresentationState.Silenced => localisation.Text("AlertState.Silenced"),
                _ => localisation.Text("AlertState.Resolved"),
            }, item.RuleName, item.InstanceName, item.ProviderType, item.Summary);
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
