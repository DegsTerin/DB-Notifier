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
    private readonly InventorySnapshot snapshot;

    public MainWindow()
    {
        InitializeComponent();
        snapshot = CreateDemonstrationSnapshot(TimeProvider.System.GetUtcNow());
        InventoryGrid.ItemsSource = rows;
        PresentReadyState();
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

    private void PresentReadyState()
    {
        DateTimeOffset now = TimeProvider.System.GetUtcNow();
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
        StateSurface.Visibility = Visibility.Collapsed;
    }

    private void PresentNonReadyState(InventorySurfaceState state)
    {
        (string symbol, string title, string message, bool loading, bool retry) = state switch
        {
            InventorySurfaceState.Loading =>
                ("…", "Carregando inventário", "Preparando a visão local sem iniciar conexões externas.", true, false),
            InventorySurfaceState.Empty =>
                ("○", "Nenhuma instância disponível", "O inventário autorizado está vazio. Cadastros e integrações serão tratados em fases próprias.", false, false),
            InventorySurfaceState.Offline =>
                ("↯", "Agent offline", "Os últimos dados conhecidos não podem ser atualizados. Nenhum status antigo é apresentado como saudável.", false, true),
            InventorySurfaceState.Denied =>
                ("⊘", "Acesso negado", "Sua identidade não possui instances.read para este escopo. Nenhum detalhe protegido foi exibido.", false, false),
            _ =>
                ("!", "Não foi possível carregar", "O inventário permanece indisponível. Tente novamente; nenhum comando administrativo foi executado.", false, true),
        };

        ReadySurface.Visibility = Visibility.Collapsed;
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
}
