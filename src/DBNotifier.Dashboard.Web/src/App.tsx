import { useMemo, useState } from "react";
import {
  buildDemonstrationSnapshot,
  buildTimelineAlertSnapshot,
  filterInventory,
  filterTimeline,
  type AlertItem,
  type EventSeverity,
  type HealthStatus,
  type InventoryItem,
  type InventoryState,
  type TimelineEventItem,
  isStale,
  staleAfterMilliseconds,
  summarizeInventory,
} from "./presentation";

const stateOptions: ReadonlyArray<{ value: InventoryState; label: string }> = [
  { value: "ready", label: "Conteúdo carregado" },
  { value: "loading", label: "Carregando" },
  { value: "empty", label: "Sem dados" },
  { value: "offline", label: "Agent offline" },
  { value: "error", label: "Erro ao carregar" },
  { value: "denied", label: "Permissão negada" },
  { value: "maintenance", label: "Em manutenção" },
];

const stateMessages: Record<Exclude<InventoryState, "ready">, { symbol: string; title: string; message: string; retry: boolean }> = {
  loading: { symbol: "…", title: "Carregando conteúdo", message: "Preparando a visão local sem iniciar conexões externas.", retry: false },
  empty: { symbol: "○", title: "Nenhum dado disponível", message: "A visão autorizada está vazia. Cadastros e integrações serão tratados em fases próprias.", retry: false },
  offline: { symbol: "↯", title: "Agent offline", message: "Os últimos dados conhecidos não podem ser atualizados. Nenhum status antigo é apresentado como saudável.", retry: true },
  error: { symbol: "!", title: "Não foi possível carregar", message: "A visão permanece indisponível. Nenhum comando administrativo foi executado.", retry: true },
  denied: { symbol: "⊘", title: "Acesso negado", message: "Sua identidade não possui instances.read para este escopo. Nenhum detalhe protegido foi exibido.", retry: false },
  maintenance: { symbol: "◆", title: "Janela de manutenção", message: "A visão está em manutenção planejada. Dados anteriores permanecem identificados como não atuais.", retry: false },
};

type DashboardView = "inventory" | "history" | "alerts";
const viewCopy: Record<DashboardView, { eyebrow: string; title: string; description: string }> = {
  inventory: { eyebrow: "Visão da frota", title: "Inventário de instâncias", description: "Estado operacional, atualização e suporte declarado por provider." },
  history: { eyebrow: "Eventos canônicos", title: "Histórico operacional", description: "Timeline somente leitura com severidade, origem e timestamps UTC." },
  alerts: { eyebrow: "Atenção operacional", title: "Alertas", description: "Estado de alertas e regras demonstrativas sem entrega externa ou mutation." },
};

const statusPresentation: Record<HealthStatus, { symbol: string; label: string; className: string }> = {
  healthy: { symbol: "●", label: "Saudável", className: "healthy" },
  degraded: { symbol: "▲", label: "Degradado", className: "degraded" },
  unavailable: { symbol: "■", label: "Indisponível", className: "critical" },
  authFailed: { symbol: "■", label: "Falha de autenticação", className: "critical" },
  timeout: { symbol: "■", label: "Timeout", className: "critical" },
  maintenance: { symbol: "◆", label: "Manutenção", className: "maintenance" },
  unknown: { symbol: "○", label: "Desconhecido", className: "unknown" },
};

function formatUtc(value: string): string {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "medium",
    timeZone: "UTC",
  }).format(new Date(value));
}

function StatusBadge({ item, now }: { item: InventoryItem; now: Date }) {
  if (isStale(item, now)) {
    return <span className="status-badge stale"><span aria-hidden="true">◷</span> Desatualizado</span>;
  }
  const presentation = statusPresentation[item.status];
  return <span className={`status-badge ${presentation.className}`}><span aria-hidden="true">{presentation.symbol}</span> {presentation.label}</span>;
}

export function App() {
  const [view, setView] = useState<DashboardView>(() =>
    window.location.hash === "#history" ? "history" : window.location.hash === "#alerts" ? "alerts" : "inventory");
  const [state, setState] = useState<InventoryState>("ready");
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<"all" | HealthStatus | "stale">("all");
  const [now] = useState(() => new Date());
  const snapshot = useMemo(() => buildDemonstrationSnapshot(now), [now]);
  const timelineSnapshot = useMemo(() => buildTimelineAlertSnapshot(now), [now]);
  const summary = useMemo(() => summarizeInventory(snapshot, now), [snapshot, now]);
  const filteredItems = useMemo(
    () => filterInventory(snapshot.items, query, statusFilter, now),
    [snapshot.items, query, statusFilter, now],
  );
  const copy = viewCopy[view];
  const navigate = (nextView: DashboardView) => {
    window.history.replaceState(null, "", nextView === "inventory" ? "#inventory" : `#${nextView}`);
    setView(nextView);
  };

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main-content">Pular para o conteúdo</a>
      <header className="topbar">
        <div className="brand-lockup" aria-label="DB-Notifier">
          <span className="brand-mark" aria-hidden="true"><i /><i /><i /></span>
          <span><strong>DB-NOTIFIER</strong><small>Operations console</small></span>
        </div>
        <div className="demo-badge">Dados de demonstração</div>
      </header>

      <div className="page-layout">
        <aside className="sidebar" aria-label="Navegação principal">
          <nav>
            <button type="button" className={`nav-item ${view === "inventory" ? "active" : ""}`} aria-current={view === "inventory" ? "page" : undefined} onClick={() => navigate("inventory")}><span aria-hidden="true">▦</span> Inventário</button>
            <button type="button" className={`nav-item ${view === "history" ? "active" : ""}`} aria-current={view === "history" ? "page" : undefined} onClick={() => navigate("history")}><span aria-hidden="true">◷</span> Histórico</button>
            <button type="button" className={`nav-item ${view === "alerts" ? "active" : ""}`} aria-current={view === "alerts" ? "page" : undefined} onClick={() => navigate("alerts")}><span aria-hidden="true">△</span> Alertas</button>
            <span className="nav-item unavailable" aria-disabled="true"><span aria-hidden="true">⚙</span> Configuração <small>Próximo incremento</small></span>
          </nav>
          <div className="sidebar-note"><strong>STATE-05</strong><span>Interface somente leitura</span></div>
        </aside>

        <main id="main-content" tabIndex={-1}>
          <section className="page-heading" aria-labelledby="page-title">
            <div>
              <p className="eyebrow">{copy.eyebrow}</p>
              <h1 id="page-title">{copy.title}</h1>
              <p>{copy.description}</p>
            </div>
            <label className="scenario-control">
              <span>Cenário acessível</span>
              <select value={state} onChange={(event) => setState(event.target.value as InventoryState)}>
                {stateOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
              </select>
            </label>
          </section>

          {state === "ready" && view === "inventory" ? (
            <ReadyInventory
              items={filteredItems}
              now={now}
              generatedAt={snapshot.generatedAt}
              summary={summary}
              query={query}
              statusFilter={statusFilter}
              onQueryChange={setQuery}
              onStatusChange={setStatusFilter}
            />
          ) : state === "ready" && view === "history" ? (
            <HistoryView snapshot={timelineSnapshot} />
          ) : state === "ready" && view === "alerts" ? (
            <AlertsView alerts={timelineSnapshot.alerts} generatedAt={timelineSnapshot.generatedAt} />
          ) : (
            <OperationalState state={state as Exclude<InventoryState, "ready">} onRetry={() => setState("ready")} />
          )}
        </main>
      </div>

      <footer>
        Nenhuma conexão externa é realizada nesta tela. Providers planejados não representam suporte público ou homologação.
      </footer>
    </div>
  );
}

const severityPresentation: Record<EventSeverity, { symbol: string; label: string; className: string }> = {
  information: { symbol: "●", label: "Informativo", className: "information" },
  warning: { symbol: "▲", label: "Aviso", className: "degraded" },
  critical: { symbol: "■", label: "Crítico", className: "critical" },
};

function SeverityBadge({ severity }: { severity: EventSeverity }) {
  const item = severityPresentation[severity];
  return <span className={`status-badge ${item.className}`}><span aria-hidden="true">{item.symbol}</span> {item.label}</span>;
}

function HistoryView({ snapshot }: { snapshot: ReturnType<typeof buildTimelineAlertSnapshot> }) {
  const [query, setQuery] = useState("");
  const [severity, setSeverity] = useState<"all" | EventSeverity>("all");
  const events = useMemo(() => filterTimeline(snapshot.events, query, severity), [snapshot.events, query, severity]);
  return <section className="inventory-panel timeline-panel" aria-labelledby="history-title">
    <div className="panel-header"><div><h2 id="history-title">Timeline de eventos</h2><p aria-live="polite">{events.length} de {snapshot.events.length} eventos visíveis</p></div><span className="read-only-label">history-alerts.v1</span></div>
    <div className="filters" role="search"><label><span>Buscar eventos</span><input type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Instância, provider, evento..." /></label><label><span>Severidade</span><select value={severity} onChange={(event) => setSeverity(event.target.value as "all" | EventSeverity)}><option value="all">Todas</option><option value="information">Informativo</option><option value="warning">Aviso</option><option value="critical">Crítico</option></select></label></div>
    {events.length === 0 ? <FilteredEmpty message="Ajuste a busca ou o filtro de severidade." /> : <ol className="timeline-list">{events.map((event) => <TimelineRow key={event.eventId} event={event} />)}</ol>}
  </section>;
}

function TimelineRow({ event }: { event: TimelineEventItem }) {
  return <li className={`timeline-item ${event.severity}`}><div className="timeline-marker" aria-hidden="true" /><article><div className="timeline-heading"><div><h3>{event.eventType}</h3><p>{event.instanceName} · <code>{event.providerType}</code></p></div><SeverityBadge severity={event.severity} /></div><p className="timeline-summary">{event.summary}</p><time dateTime={event.occurredAt}>{formatUtc(event.occurredAt)} UTC</time></article></li>;
}

function AlertsView({ alerts, generatedAt }: { alerts: readonly AlertItem[]; generatedAt: string }) {
  const active = alerts.filter((alert) => alert.state === "active").length;
  const critical = alerts.filter((alert) => alert.severity === "critical" && alert.state !== "resolved").length;
  return <section aria-labelledby="alerts-title"><p className="updated-at">Atualizado em <time dateTime={generatedAt}>{formatUtc(generatedAt)} UTC</time></p><div className="summary-grid alert-summary"><article className="summary-card critical"><span><i aria-hidden="true">■</i> Críticos abertos</span><strong>{critical}</strong></article><article className="summary-card degraded"><span><i aria-hidden="true">▲</i> Ativos</span><strong>{active}</strong></article><article className="summary-card"><span><i aria-hidden="true">✓</i> Total visível</span><strong>{alerts.length}</strong></article></div><div className="inventory-panel"><div className="panel-header"><div><h2 id="alerts-title">Alertas demonstrativos</h2><p>Reconhecer e silenciar não estão habilitados neste incremento.</p></div><span className="read-only-label">Somente leitura</span></div><div className="alert-list">{alerts.map((alert) => <article className="alert-card" key={alert.alertId}><div className="alert-card-heading"><SeverityBadge severity={alert.severity} /><span className="alert-state">{alert.state === "active" ? "Ativo" : alert.state === "acknowledged" ? "Reconhecido" : alert.state === "silenced" ? "Silenciado" : "Resolvido"}</span></div><h3>{alert.ruleName}</h3><p>{alert.summary}</p><dl><div><dt>Instância</dt><dd>{alert.instanceName}</dd></div><div><dt>Provider</dt><dd><code>{alert.providerType}</code></dd></div><div><dt>Atualizado</dt><dd><time dateTime={alert.updatedAt}>{formatUtc(alert.updatedAt)} UTC</time></dd></div></dl></article>)}</div></div></section>;
}

function FilteredEmpty({ message }: { message: string }) {
  return <div className="filtered-empty"><span aria-hidden="true">⌕</span><strong>Nenhum resultado</strong><p>{message}</p></div>;
}

function ReadyInventory({
  items,
  now,
  generatedAt,
  summary,
  query,
  statusFilter,
  onQueryChange,
  onStatusChange,
}: {
  items: readonly InventoryItem[];
  now: Date;
  generatedAt: string;
  summary: ReturnType<typeof summarizeInventory>;
  query: string;
  statusFilter: "all" | HealthStatus | "stale";
  onQueryChange: (value: string) => void;
  onStatusChange: (value: "all" | HealthStatus | "stale") => void;
}) {
  const cards = [
    { label: "Total", value: summary.total, symbol: "▦", className: "total" },
    { label: "Saudáveis", value: summary.healthy, symbol: "●", className: "healthy" },
    { label: "Degradadas", value: summary.degraded, symbol: "▲", className: "degraded" },
    { label: "Atenção", value: summary.attentionRequired, symbol: "■", className: "critical" },
    { label: "Desatualizadas", value: summary.stale, symbol: "◷", className: "stale" },
  ];

  return (
    <section id="inventory" aria-label="Resumo e inventário">
      <p className="updated-at">Atualizado em <time dateTime={generatedAt}>{formatUtc(generatedAt)} UTC</time> · stale após {staleAfterMilliseconds / 60_000} min</p>
      <div className="summary-grid">
        {cards.map((card) => (
          <article className={`summary-card ${card.className}`} key={card.label}>
            <span><i aria-hidden="true">{card.symbol}</i> {card.label}</span>
            <strong>{card.value}</strong>
          </article>
        ))}
      </div>

      <section className="inventory-panel" aria-labelledby="inventory-title">
        <div className="panel-header">
          <div><h2 id="inventory-title">Instâncias monitoradas</h2><p aria-live="polite">{items.length} de {summary.total} itens visíveis</p></div>
          <span className="read-only-label">Somente leitura</span>
        </div>
        <div className="filters" role="search">
          <label><span>Buscar</span><input type="search" value={query} onChange={(event) => onQueryChange(event.target.value)} placeholder="Nome, provider, ambiente..." /></label>
          <label><span>Status</span><select value={statusFilter} onChange={(event) => onStatusChange(event.target.value as "all" | HealthStatus | "stale")}>
            <option value="all">Todos</option><option value="healthy">Saudável</option><option value="degraded">Degradado</option><option value="timeout">Timeout</option><option value="unknown">Desconhecido</option><option value="stale">Desatualizado</option>
          </select></label>
        </div>
        {items.length === 0 ? (
          <FilteredEmpty message="Ajuste a busca ou o filtro de status." />
        ) : (
          <>
            <div className="table-wrap">
              <table>
                <caption className="sr-only">Inventário demonstrativo de instâncias de banco de dados</caption>
                <thead><tr><th scope="col">Instância</th><th scope="col">Provider</th><th scope="col">Suporte</th><th scope="col">Ambiente</th><th scope="col">Status</th><th scope="col">Observado em</th><th scope="col">Latência</th></tr></thead>
                <tbody>{items.map((item) => <InventoryTableRow key={item.instanceId} item={item} now={now} />)}</tbody>
              </table>
            </div>
            <div className="mobile-list">{items.map((item) => <InventoryMobileCard key={item.instanceId} item={item} now={now} />)}</div>
          </>
        )}
      </section>
    </section>
  );
}

function InventoryTableRow({ item, now }: { item: InventoryItem; now: Date }) {
  return <tr><th scope="row"><strong>{item.displayName}</strong><small>{item.locationLabel}</small></th><td><code>{item.providerType}</code></td><td>{item.supportLabel}</td><td>{item.environment}</td><td><StatusBadge item={item} now={now} /></td><td><time dateTime={item.observedAt}>{formatUtc(item.observedAt)} UTC</time></td><td>{item.latencyMilliseconds === null ? "—" : `${item.latencyMilliseconds} ms`}</td></tr>;
}

function InventoryMobileCard({ item, now }: { item: InventoryItem; now: Date }) {
  return <article className="instance-card"><div className="instance-card-heading"><div><h3>{item.displayName}</h3><p>{item.locationLabel}</p></div><StatusBadge item={item} now={now} /></div><dl><div><dt>Provider</dt><dd><code>{item.providerType}</code></dd></div><div><dt>Suporte</dt><dd>{item.supportLabel}</dd></div><div><dt>Ambiente</dt><dd>{item.environment}</dd></div><div><dt>Observado em</dt><dd><time dateTime={item.observedAt}>{formatUtc(item.observedAt)} UTC</time></dd></div><div><dt>Latência</dt><dd>{item.latencyMilliseconds === null ? "—" : `${item.latencyMilliseconds} ms`}</dd></div></dl></article>;
}

function OperationalState({ state, onRetry }: { state: Exclude<InventoryState, "ready">; onRetry: () => void }) {
  const content = stateMessages[state];
  return <section className="operational-state" role={state === "error" || state === "denied" ? "alert" : "status"} aria-live="polite" aria-busy={state === "loading"}>
    {state === "loading" && <span className="loading-line" aria-hidden="true" />}
    <span className="state-symbol" aria-hidden="true">{content.symbol}</span><h2>{content.title}</h2><p>{content.message}</p>
    {content.retry && <button type="button" onClick={onRetry}>Tentar novamente</button>}
  </section>;
}
