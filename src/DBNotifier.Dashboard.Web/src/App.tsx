import { useMemo, useState } from "react";
import {
  buildDemonstrationSnapshot,
  filterInventory,
  type HealthStatus,
  type InventoryItem,
  type InventoryState,
  isStale,
  staleAfterMilliseconds,
  summarizeInventory,
} from "./presentation";

const stateOptions: ReadonlyArray<{ value: InventoryState; label: string }> = [
  { value: "ready", label: "Inventário carregado" },
  { value: "loading", label: "Carregando" },
  { value: "empty", label: "Inventário vazio" },
  { value: "offline", label: "Agent offline" },
  { value: "error", label: "Erro ao carregar" },
  { value: "denied", label: "Permissão negada" },
];

const stateMessages: Record<Exclude<InventoryState, "ready">, { symbol: string; title: string; message: string; retry: boolean }> = {
  loading: { symbol: "…", title: "Carregando inventário", message: "Preparando a visão local sem iniciar conexões externas.", retry: false },
  empty: { symbol: "○", title: "Nenhuma instância disponível", message: "O inventário autorizado está vazio. Cadastros e integrações serão tratados em fases próprias.", retry: false },
  offline: { symbol: "↯", title: "Agent offline", message: "Os últimos dados conhecidos não podem ser atualizados. Nenhum status antigo é apresentado como saudável.", retry: true },
  error: { symbol: "!", title: "Não foi possível carregar", message: "O inventário permanece indisponível. Nenhum comando administrativo foi executado.", retry: true },
  denied: { symbol: "⊘", title: "Acesso negado", message: "Sua identidade não possui instances.read para este escopo. Nenhum detalhe protegido foi exibido.", retry: false },
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
  const [state, setState] = useState<InventoryState>("ready");
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<"all" | HealthStatus | "stale">("all");
  const [now] = useState(() => new Date());
  const snapshot = useMemo(() => buildDemonstrationSnapshot(now), [now]);
  const summary = useMemo(() => summarizeInventory(snapshot, now), [snapshot, now]);
  const filteredItems = useMemo(
    () => filterInventory(snapshot.items, query, statusFilter, now),
    [snapshot.items, query, statusFilter, now],
  );

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
            <a className="nav-item active" href="#inventory" aria-current="page"><span aria-hidden="true">▦</span> Inventário</a>
            <span className="nav-item unavailable" aria-disabled="true"><span aria-hidden="true">◷</span> Histórico <small>Próximo incremento</small></span>
            <span className="nav-item unavailable" aria-disabled="true"><span aria-hidden="true">△</span> Alertas <small>Próximo incremento</small></span>
            <span className="nav-item unavailable" aria-disabled="true"><span aria-hidden="true">⚙</span> Configuração <small>Próximo incremento</small></span>
          </nav>
          <div className="sidebar-note"><strong>STATE-05</strong><span>Interface somente leitura</span></div>
        </aside>

        <main id="main-content" tabIndex={-1}>
          <section className="page-heading" aria-labelledby="page-title">
            <div>
              <p className="eyebrow">Visão da frota</p>
              <h1 id="page-title">Inventário de instâncias</h1>
              <p>Estado operacional, atualização e suporte declarado por provider.</p>
            </div>
            <label className="scenario-control">
              <span>Cenário acessível</span>
              <select value={state} onChange={(event) => setState(event.target.value as InventoryState)}>
                {stateOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
              </select>
            </label>
          </section>

          {state === "ready" ? (
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
          ) : (
            <OperationalState state={state} onRetry={() => setState("ready")} />
          )}
        </main>
      </div>

      <footer>
        Nenhuma conexão externa é realizada nesta tela. Providers planejados não representam suporte público ou homologação.
      </footer>
    </div>
  );
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
          <div className="filtered-empty"><span aria-hidden="true">⌕</span><strong>Nenhum resultado</strong><p>Ajuste a busca ou o filtro de status.</p></div>
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
