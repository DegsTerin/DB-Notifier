using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;

namespace DBNotifier.Desktop.Wpf;

public partial class MainWindow : Window
{
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);
    private readonly ObservableCollection<InventoryRow> rows = [];
    private readonly ObservableCollection<TimelineRow> timelineRows = [];
    private readonly ObservableCollection<AlertRow> alertRows = [];
    private readonly ObservableCollection<CapabilityRow> capabilityRows = [];
    private readonly InventorySnapshot snapshot;
    private readonly TimelineAlertSnapshot timelineSnapshot;
    private readonly ConfigurationCapabilitySnapshot configurationSnapshot;

    public MainWindow()
    {
        InitializeComponent();
        snapshot = CreateDemonstrationSnapshot(TimeProvider.System.GetUtcNow());
        timelineSnapshot = CreateTimelineSnapshot(TimeProvider.System.GetUtcNow());
        configurationSnapshot = CreateConfigurationSnapshot();
        InventoryGrid.ItemsSource = rows;
        HistoryGrid.ItemsSource = timelineRows;
        AlertsGrid.ItemsSource = alertRows;
        ConfigurationGrid.ItemsSource = configurationSnapshot.Fields;
        CapabilityGrid.ItemsSource = capabilityRows;
        PresentReadyState();
    }

    private void ViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsInitialized && ScenarioSelector.SelectedIndex == 0)
        {
            PresentReadyState();
        }
    }

    private void ScenarioSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || ScenarioSelector.SelectedItem is not ComboBoxItem selected)
        {
            return;
        }

        InventorySurfaceState state = Enum.TryParse(
            selected.Tag?.ToString(),
            ignoreCase: false,
            out InventorySurfaceState parsed)
            ? parsed
            : InventorySurfaceState.Error;

        if (state == InventorySurfaceState.Ready)
        {
            PresentReadyState();
            return;
        }

        PresentNonReadyState(state);
    }

    private void RetryButtonClick(object sender, RoutedEventArgs e)
    {
        ScenarioSelector.SelectedIndex = 0;
        ScenarioSelector.Focus();
    }

    private void ReviewCapabilityClick(object sender, RoutedEventArgs e)
    {
        if (CapabilityGrid.SelectedItem is not CapabilityRow selected)
        {
            System.Windows.MessageBox.Show(this, "Selecione uma capability para revisar.", "DB-Notifier", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        bool authorized = PermissionSelector.SelectedItem is ComboBoxItem permission &&
            string.Equals(permission.Tag?.ToString(), "Authorized", StringComparison.Ordinal);
        ShowPreview(configurationSnapshot.Preview(selected.CapabilityId, authorized));
    }

    private void PreviewConfirmationClick(object sender, RoutedEventArgs e) =>
        ShowPreview(ActionPreviewDisposition.ConfirmationRequired);

    private void ShowPreview(ActionPreviewDisposition disposition)
    {
        (string title, string message) = disposition switch
        {
            ActionPreviewDisposition.ConfirmationRequired => ("Confirmação obrigatória", "Uma operação suportada exigiria motivo, expiração e confirmação final. Executar permanece desabilitado."),
            ActionPreviewDisposition.Denied => ("Permissão negada", "A identidade demonstrativa não possui a permissão específica no escopo."),
            ActionPreviewDisposition.Unsupported => ("Operação não suportada", "O provider não declara esta capability. Nenhuma tentativa foi realizada."),
            ActionPreviewDisposition.Unavailable => ("Operação indisponível", "Os pré-requisitos da capability não estão disponíveis."),
            _ => ("Capability desconhecida", "O estado falha fechado e nenhuma ação é disponibilizada."),
        };
        System.Windows.MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void PresentReadyState()
    {
        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        bool showHistory = ViewSelector.SelectedItem is ComboBoxItem selectedView &&
            string.Equals(selectedView.Tag?.ToString(), "HistoryAlerts", StringComparison.Ordinal);
        if (showHistory)
        {
            PresentHistoryAndAlerts(now);
            return;
        }
        bool showConfiguration = ViewSelector.SelectedItem is ComboBoxItem configurationView &&
            string.Equals(configurationView.Tag?.ToString(), "Configuration", StringComparison.Ordinal);
        if (showConfiguration)
        {
            PresentConfiguration(now);
            return;
        }

        InventoryStatusSummary summary = snapshot.Summarize(now, StaleAfter);
        rows.Clear();
        foreach (InstanceInventoryItem item in snapshot.Items)
        {
            rows.Add(InventoryRow.From(item, now, StaleAfter));
        }

        UpdatedAtText.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Atualizado em {now:yyyy-MM-dd HH:mm:ss} UTC · stale após 5 min");
        TotalCountText.Text = summary.Total.ToString(CultureInfo.InvariantCulture);
        HealthyCountText.Text = summary.Healthy.ToString(CultureInfo.InvariantCulture);
        DegradedCountText.Text = summary.Degraded.ToString(CultureInfo.InvariantCulture);
        AttentionCountText.Text = summary.AttentionRequired.ToString(CultureInfo.InvariantCulture);
        StaleCountText.Text = summary.Stale.ToString(CultureInfo.InvariantCulture);
        ReadySurface.Visibility = Visibility.Visible;
        HistoryAlertSurface.Visibility = Visibility.Collapsed;
        ConfigurationSurface.Visibility = Visibility.Collapsed;
        StateSurface.Visibility = Visibility.Collapsed;
    }

    private void PresentHistoryAndAlerts(DateTimeOffset now)
    {
        timelineRows.Clear();
        foreach (TimelineEventItem item in timelineSnapshot.Events)
        {
            timelineRows.Add(TimelineRow.From(item));
        }

        alertRows.Clear();
        foreach (AlertPresentationItem item in timelineSnapshot.Alerts)
        {
            alertRows.Add(AlertRow.From(item));
        }

        AlertStatusSummary summary = timelineSnapshot.Summarize();
        AlertSummaryText.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"{summary.Active} ativo(s) · {summary.UnresolvedCritical} crítico(s) aberto(s)");
        UpdatedAtText.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Atualizado em {now:yyyy-MM-dd HH:mm:ss} UTC · adapters locais");
        ReadySurface.Visibility = Visibility.Collapsed;
        HistoryAlertSurface.Visibility = Visibility.Visible;
        ConfigurationSurface.Visibility = Visibility.Collapsed;
        StateSurface.Visibility = Visibility.Collapsed;
    }

    private void PresentConfiguration(DateTimeOffset now)
    {
        capabilityRows.Clear();
        foreach (CapabilityPresentation capability in configurationSnapshot.Capabilities)
        {
            capabilityRows.Add(CapabilityRow.From(capability));
        }
        if (capabilityRows.Count > 0)
        {
            CapabilityGrid.SelectedIndex = 0;
        }
        UpdatedAtText.Text = string.Create(CultureInfo.InvariantCulture, $"Prévia local em {now:yyyy-MM-dd HH:mm:ss} UTC · nenhuma mutation");
        ReadySurface.Visibility = Visibility.Collapsed;
        HistoryAlertSurface.Visibility = Visibility.Collapsed;
        ConfigurationSurface.Visibility = Visibility.Visible;
        StateSurface.Visibility = Visibility.Collapsed;
    }

    private void PresentNonReadyState(InventorySurfaceState state)
    {
        (string symbol, string title, string message, bool loading, bool retry) = state switch
        {
            InventorySurfaceState.Loading =>
                ("…", "Carregando conteúdo", "Preparando a visão local sem iniciar conexões externas.", true, false),
            InventorySurfaceState.Empty =>
                ("○", "Nenhum dado disponível", "A visão autorizada está vazia. Cadastros e integrações serão tratados em fases próprias.", false, false),
            InventorySurfaceState.Offline =>
                ("↯", "Agent offline", "Os últimos dados conhecidos não podem ser atualizados. Nenhum status antigo é apresentado como saudável.", false, true),
            InventorySurfaceState.Denied =>
                ("⊘", "Acesso negado", "Sua identidade não possui instances.read para este escopo. Nenhum detalhe protegido foi exibido.", false, false),
            InventorySurfaceState.Maintenance =>
                ("◆", "Janela de manutenção", "A visão está em manutenção planejada. Dados anteriores permanecem identificados como não atuais.", false, false),
            _ =>
                ("!", "Não foi possível carregar", "A visão permanece indisponível. Tente novamente; nenhum comando administrativo foi executado.", false, true),
        };

        ReadySurface.Visibility = Visibility.Collapsed;
        HistoryAlertSurface.Visibility = Visibility.Collapsed;
        ConfigurationSurface.Visibility = Visibility.Collapsed;
        StateSurface.Visibility = Visibility.Visible;
        LoadingIndicator.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        StateSymbolText.Text = symbol;
        StateTitleText.Text = title;
        StateMessageText.Text = message;
        RetryButton.Visibility = retry ? Visibility.Visible : Visibility.Collapsed;
        UpdatedAtText.Text = "Estado demonstrativo · nenhuma conexão externa";
    }

    private static InventorySnapshot CreateDemonstrationSnapshot(DateTimeOffset now) =>
        new(
            InventorySnapshot.CurrentSchemaVersion,
            now,
            [
                CreateItem("00000000-0000-0000-0000-000000000001", "Financeiro principal", "postgresql", "Implementado · não homologado", "Produção", "Datacenter SP", HealthStatus.Healthy, now.AddSeconds(-38), 24),
                CreateItem("00000000-0000-0000-0000-000000000002", "Pedidos regional", "mysql", "Planejado · não implementado", "Produção", "Cloud privado", HealthStatus.Degraded, now.AddMinutes(-2), 86),
                CreateItem("00000000-0000-0000-0000-000000000003", "Analytics", "sql-server", "Planejado · não implementado", "Homologação", "Azure", HealthStatus.Timeout, now.AddMinutes(-3), null),
                CreateItem("00000000-0000-0000-0000-000000000004", "Catálogo", "mongodb", "Planejado · não implementado", "Desenvolvimento", "Linux local", HealthStatus.Unknown, now.AddMinutes(-9), null),
            ]);

    private static TimelineAlertSnapshot CreateTimelineSnapshot(DateTimeOffset now) =>
        new(
            TimelineAlertSnapshot.CurrentSchemaVersion,
            now,
            [
                CreateEvent("00000000-0000-0000-0001-000000000001", "Financeiro principal", "postgresql", "Recovered", EventSeverity.Information, "Conectividade recuperada após uma tentativa.", now.AddMinutes(-2)),
                CreateEvent("00000000-0000-0000-0001-000000000002", "Pedidos regional", "mysql", "Degraded", EventSeverity.Warning, "Latência acima do limite demonstrativo.", now.AddMinutes(-7)),
                CreateEvent("00000000-0000-0000-0001-000000000003", "Analytics", "sql-server", "Timeout", EventSeverity.Critical, "Timeout normalizado; nenhuma ação automática executada.", now.AddMinutes(-14)),
                CreateEvent("00000000-0000-0000-0001-000000000004", "Catálogo", "mongodb", "MaintenanceStarted", EventSeverity.Information, "Janela de manutenção demonstrativa iniciada.", now.AddMinutes(-24)),
            ],
            [
                CreateAlert("00000000-0000-0000-0002-000000000001", "Analytics", "sql-server", EventSeverity.Critical, AlertPresentationState.Active, "Timeout contínuo", "Três timeouts na janela demonstrativa.", now.AddMinutes(-3)),
                CreateAlert("00000000-0000-0000-0002-000000000002", "Pedidos regional", "mysql", EventSeverity.Warning, AlertPresentationState.Acknowledged, "Latência elevada", "Alerta reconhecido somente na fixture local.", now.AddMinutes(-8)),
                CreateAlert("00000000-0000-0000-0002-000000000003", "Catálogo", "mongodb", EventSeverity.Information, AlertPresentationState.Silenced, "Manutenção programada", "Silenciamento demonstrativo; nenhum canal foi contatado.", now.AddMinutes(-24)),
            ]);

    private static ConfigurationCapabilitySnapshot CreateConfigurationSnapshot() =>
        new(
            ConfigurationCapabilitySnapshot.CurrentSchemaVersion,
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "Financeiro principal",
            "postgresql",
            [
                new("monitoring.interval", "Intervalo de monitoramento", "60 segundos", "Política demonstrativa; não persistida."),
                new("monitoring.timeout", "Timeout", "5 segundos", "Limite demonstrativo do probe."),
                new("monitoring.retry", "Tentativas", "3", "Retry limitado com backoff."),
                new("credential.reference", "Credencial", "Referência protegida", "Identificador e segredo não são exibidos."),
            ],
            [
                new("service.start", "Start", CapabilityPresentationState.Unsupported, "provider.control_unsupported", true),
                new("service.stop", "Stop", CapabilityPresentationState.Unsupported, "provider.control_unsupported", true),
                new("service.restart", "Restart", CapabilityPresentationState.Unsupported, "provider.control_unsupported", true),
            ]);

    private static TimelineEventItem CreateEvent(string id, string name, string provider, string type, EventSeverity severity, string summary, DateTimeOffset occurredAt) =>
        new(Guid.Parse(id), Guid.Parse(id.Replace("0001-", "0000-")), name, provider, type, severity, summary, occurredAt, occurredAt);

    private static AlertPresentationItem CreateAlert(string id, string name, string provider, EventSeverity severity, AlertPresentationState state, string rule, string summary, DateTimeOffset updatedAt) =>
        new(Guid.Parse(id), Guid.Parse(id.Replace("0002-", "0000-")), name, provider, severity, state, rule, summary, updatedAt.AddMinutes(-10), updatedAt);

    private static InstanceInventoryItem CreateItem(
        string instanceId,
        string name,
        string provider,
        string support,
        string environment,
        string location,
        HealthStatus status,
        DateTimeOffset receivedAt,
        double? latencyMilliseconds) =>
        new(
            Guid.Parse(instanceId),
            name,
            provider,
            support,
            environment,
            location,
            status,
            receivedAt.AddSeconds(-1),
            receivedAt,
            latencyMilliseconds is null ? null : TimeSpan.FromMilliseconds(latencyMilliseconds.Value),
            true);

    private sealed record InventoryRow(
        string DisplayName,
        string ProviderType,
        string SupportLabel,
        string Environment,
        string StatusLabel,
        string ObservedAtLabel,
        string LatencyLabel)
    {
        public static InventoryRow From(
            InstanceInventoryItem item,
            DateTimeOffset now,
            TimeSpan staleAfter)
        {
            bool stale = item.IsStale(now, staleAfter);
            string status = stale ? "◷ Desatualizado" : item.Status switch
            {
                HealthStatus.Healthy => "● Saudável",
                HealthStatus.Degraded => "▲ Degradado",
                HealthStatus.Unavailable => "■ Indisponível",
                HealthStatus.AuthFailed => "■ Falha de autenticação",
                HealthStatus.Timeout => "■ Timeout",
                HealthStatus.Maintenance => "◆ Manutenção",
                _ => "○ Desconhecido",
            };

            return new InventoryRow(
                item.DisplayName,
                item.ProviderType,
                item.SupportLabel,
                item.Environment,
                status,
                item.ObservedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                item.Latency is null
                    ? "—"
                    : string.Create(CultureInfo.InvariantCulture, $"{item.Latency.Value.TotalMilliseconds:0} ms"));
        }
    }

    private sealed record TimelineRow(string OccurredAtLabel, string SeverityLabel, string EventType, string InstanceName, string ProviderType, string Summary)
    {
        public static TimelineRow From(TimelineEventItem item) => new(
            item.OccurredAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            item.Severity switch { EventSeverity.Critical => "■ Crítico", EventSeverity.Warning => "▲ Aviso", _ => "● Informativo" },
            item.EventType, item.InstanceName, item.ProviderType, item.Summary);
    }

    private sealed record AlertRow(string SeverityLabel, string StateLabel, string RuleName, string InstanceName, string ProviderType, string Summary)
    {
        public static AlertRow From(AlertPresentationItem item) => new(
            item.Severity switch { EventSeverity.Critical => "■ Crítico", EventSeverity.Warning => "▲ Aviso", _ => "● Informativo" },
            item.State switch { AlertPresentationState.Active => "Ativo", AlertPresentationState.Acknowledged => "Reconhecido", AlertPresentationState.Silenced => "Silenciado", _ => "Resolvido" },
            item.RuleName, item.InstanceName, item.ProviderType, item.Summary);
    }

    private sealed record CapabilityRow(string CapabilityId, string DisplayName, string StateLabel, string ReasonCode)
    {
        public static CapabilityRow From(CapabilityPresentation item) => new(
            item.CapabilityId,
            item.DisplayName,
            item.State switch
            {
                CapabilityPresentationState.Supported => "Suportado",
                CapabilityPresentationState.Unsupported => "— Não suportado",
                CapabilityPresentationState.Unavailable => "↯ Indisponível",
                _ => "? Desconhecido",
            },
            item.ReasonCode);
    }
}
