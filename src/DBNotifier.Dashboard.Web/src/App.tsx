/** Module purpose: Implements App for the provider-neutral DB-Notifier Dashboard without direct database access. */
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import {
  buildDemonstrationSnapshot,
  buildConfigurationSnapshot,
  buildTimelineAlertSnapshot,
  filterInventory,
  filterTimeline,
  type AlertItem,
  type ActionPreview,
  type EventSeverity,
  type HealthStatus,
  type InventoryItem,
  type InventoryState,
  type TimelineEventItem,
  isStale,
  previewAction,
  staleAfterMilliseconds,
  summarizeInventory,
} from "./presentation";
import { ThemeSelector } from "./ThemeSelector";
import { LanguageSelector } from "./LanguageSelector";
import { TvModeButton } from "./TvModeButton";
import { useLocalisation } from "./LocalisationProvider";
import type { MessageKey } from "./generated/localisation";
import { formatSystemDateTime, formatSystemTime } from "./systemDateTime";

const stateOptions: ReadonlyArray<{ value: InventoryState; labelKey: MessageKey }> = [
  { value: "ready", labelKey: "Scenario.Ready" },
  { value: "loading", labelKey: "Scenario.Loading" },
  { value: "empty", labelKey: "Scenario.Empty" },
  { value: "offline", labelKey: "Scenario.Offline" },
  { value: "error", labelKey: "Scenario.Error" },
  { value: "denied", labelKey: "Scenario.Denied" },
  { value: "maintenance", labelKey: "Scenario.Maintenance" },
];

const stateMessages: Record<Exclude<InventoryState, "ready">, { symbol: string; titleKey: MessageKey; messageKey: MessageKey; retry: boolean }> = {
  loading: { symbol: "…", titleKey: "Operational.Loading.Title", messageKey: "Operational.Loading.Message", retry: false },
  empty: { symbol: "○", titleKey: "Operational.Empty.Title", messageKey: "Operational.Empty.Message", retry: false },
  offline: { symbol: "↯", titleKey: "Operational.Offline.Title", messageKey: "Operational.Offline.Message", retry: true },
  error: { symbol: "!", titleKey: "Operational.Error.Title", messageKey: "Operational.Error.Message", retry: true },
  denied: { symbol: "⊘", titleKey: "Operational.Denied.Title", messageKey: "Operational.Denied.Message", retry: false },
  maintenance: { symbol: "◆", titleKey: "Operational.Maintenance.Title", messageKey: "Operational.Maintenance.Message", retry: false },
};

type DashboardView = "overview" | "inventory" | "history" | "alerts" | "configuration";
const viewCopy: Record<DashboardView, { eyebrowKey: MessageKey; titleKey: MessageKey; descriptionKey: MessageKey }> = {
  overview: { eyebrowKey: "View.Overview.Eyebrow", titleKey: "View.Overview.Title", descriptionKey: "View.Overview.Description" },
  inventory: { eyebrowKey: "View.Inventory.Eyebrow", titleKey: "View.Inventory.Title", descriptionKey: "View.Inventory.Description" },
  history: { eyebrowKey: "View.History.Eyebrow", titleKey: "View.History.Title", descriptionKey: "View.History.Description" },
  alerts: { eyebrowKey: "View.Alerts.Eyebrow", titleKey: "View.Alerts.Title", descriptionKey: "View.Alerts.Description" },
  configuration: { eyebrowKey: "View.Configuration.Eyebrow", titleKey: "View.Configuration.Title", descriptionKey: "View.Configuration.Description" },
};

type AppIconName = "database" | "overview" | "inventory" | "history" | "alerts" | "configuration" | "healthy" | "degraded" | "critical" | "stale";

/**
 * Renders the small outlined icon set owned by the DB-Notifier shell and operational summaries.
 * @param name - Stable semantic icon name; it does not encode provider identity.
 * @param className - Optional presentation class applied without changing accessible meaning.
 * @returns A decorative SVG hidden from assistive technology because adjacent text carries the label.
 */
function AppIcon({ name, className = "" }: { name: AppIconName; className?: string }) {
  let content: ReactNode;
  switch (name) {
    case "database":
      content = <><ellipse cx="12" cy="5" rx="7.5" ry="3" /><path d="M4.5 5v7c0 1.7 3.4 3 7.5 3s7.5-1.3 7.5-3V5" /><path d="M4.5 12v7c0 1.7 3.4 3 7.5 3s7.5-1.3 7.5-3v-7" /></>;
      break;
    case "overview":
      content = <><path d="m4 11 8-7 8 7" /><path d="M6.5 10v9h11v-9" /><path d="M10 19v-5h4v5" /></>;
      break;
    case "inventory":
      content = <><rect x="4" y="4" width="6" height="6" rx="1" /><rect x="14" y="4" width="6" height="6" rx="1" /><rect x="4" y="14" width="6" height="6" rx="1" /><rect x="14" y="14" width="6" height="6" rx="1" /></>;
      break;
    case "history":
    case "stale":
      content = <><circle cx="12" cy="12" r="8" /><path d="M12 7.5V12l3 2" /></>;
      break;
    case "alerts":
    case "degraded":
      content = <><path d="M12 3.5 21 19H3Z" /><path d="M12 9v4" /><path d="M12 16.25h.01" /></>;
      break;
    case "configuration":
      content = <><path d="M4 7h10M18 7h2M4 17h2M10 17h10" /><circle cx="16" cy="7" r="2" /><circle cx="8" cy="17" r="2" /></>;
      break;
    case "healthy":
      content = <><circle cx="12" cy="12" r="8" /><path d="m8.5 12 2.25 2.25L15.75 9" /></>;
      break;
    case "critical":
      content = <><circle cx="12" cy="12" r="8" /><path d="M12 7.75v5.5" /><path d="M12 16.5h.01" /></>;
      break;
  }
  return <svg className={`app-icon ${className}`.trim()} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false">{content}</svg>;
}

const statusPresentation: Record<HealthStatus, { symbol: string; labelKey: MessageKey; className: string }> = {
  healthy: { symbol: "●", labelKey: "Status.Healthy", className: "healthy" },
  degraded: { symbol: "▲", labelKey: "Status.Degraded", className: "degraded" },
  unavailable: { symbol: "■", labelKey: "Status.Unavailable", className: "critical" },
  authFailed: { symbol: "■", labelKey: "Status.AuthFailed", className: "critical" },
  timeout: { symbol: "■", labelKey: "Status.Timeout", className: "critical" },
  maintenance: { symbol: "◆", labelKey: "Status.Maintenance", className: "maintenance" },
  unknown: { symbol: "○", labelKey: "Status.Unknown", className: "unknown" },
};

function StatusBadge({ item, now }: { item: InventoryItem; now: Date }) {
  const { t } = useLocalisation();
  if (isStale(item, now)) {
    return <span className="status-badge stale"><span aria-hidden="true">◷</span> {t("Status.Stale")}</span>;
  }
  const presentation = statusPresentation[item.status];
  return <span className={`status-badge ${presentation.className}`}><span aria-hidden="true">{presentation.symbol}</span> {t(presentation.labelKey)}</span>;
}

/**
 * Renders the provider-neutral Dashboard shell and deterministic STATE-05 feature views.
 * @returns The accessible application shell without external integration or administrative execution.
 */
export function App() {
  const { locale, t } = useLocalisation();
  const [view, setView] = useState<DashboardView>(() =>
    window.location.hash === "#inventory" ? "inventory" : window.location.hash === "#history" ? "history" : window.location.hash === "#alerts" ? "alerts" : window.location.hash === "#configuration" ? "configuration" : "overview");
  const [state, setState] = useState<InventoryState>("ready");
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<"all" | HealthStatus | "stale">("all");
  const [tvMode, setTvMode] = useState(false);
  const [snapshotTime] = useState(() => new Date());
  const [now, setNow] = useState(snapshotTime);
  const snapshot = useMemo(() => buildDemonstrationSnapshot(snapshotTime, locale), [snapshotTime, locale]);
  const timelineSnapshot = useMemo(() => buildTimelineAlertSnapshot(snapshotTime, locale), [snapshotTime, locale]);
  const configurationSnapshot = useMemo(() => buildConfigurationSnapshot(locale), [locale]);
  const summary = useMemo(() => summarizeInventory(snapshot, now), [snapshot, now]);
  const filteredItems = useMemo(
    () => filterInventory(snapshot.items, query, statusFilter, now, locale),
    [snapshot.items, query, statusFilter, now, locale],
  );
  const copy = viewCopy[view];
  const navigate = useCallback((nextView: DashboardView) => {
    window.history.replaceState(null, "", `#${nextView}`);
    setView(nextView);
  }, []);

  useEffect(() => {
    // TV mode updates the visible system-local clock each second; standard views only need bounded freshness recalculation.
    const interval = window.setInterval(() => setNow(new Date()), tvMode ? 1_000 : 30_000);
    return () => window.clearInterval(interval);
  }, [tvMode]);

  /** Enters the factual fleet view for TV presentation and restores only the presentation shell on exit. */
  const handleTvModeChange = useCallback((active: boolean) => {
    setTvMode(active);
    if (active) {
      navigate("overview");
      setState("ready");
      setQuery("");
      setStatusFilter("all");
      setNow(new Date());
    }
  }, [navigate]);

  return (
    <div className={`app-shell ${tvMode ? "tv-mode" : ""}`}>
      <a className="skip-link" href="#main-content">{t("Navigation.Skip")}</a>
      <header className="topbar">
        <div className="brand-lockup" aria-label="DB Notifier">
          <span className="brand-mark"><img src="/dbnotifier-icon.svg?v=1.3.3" alt="" /></span>
          <span><strong>DB Notifier</strong><small>{t("Brand.Subtitle")}</small></span>
        </div>
        {tvMode && <div className="tv-mode-status"><span aria-hidden="true" /><strong>{t("TV.Active")}</strong><time dateTime={now.toISOString()}>{formatSystemDateTime(now, locale)}</time></div>}
        <div className="topbar-controls">
          <LanguageSelector />
          <ThemeSelector />
          <TvModeButton active={tvMode} onActiveChange={handleTvModeChange} />
          <div className="demo-badge"><span aria-hidden="true" />{t("Demo.Badge")}</div>
        </div>
      </header>

      <div className="page-layout">
        <aside className="sidebar" aria-label={t("Navigation.Label")}>
          <div className="sidebar-primary">
            <p className="sidebar-section-label">{t("Navigation.Label")}</p>
            <nav>
              <button type="button" className={`nav-item ${view === "overview" ? "active" : ""}`} aria-current={view === "overview" ? "page" : undefined} onClick={() => navigate("overview")}><AppIcon name="overview" /><span className="nav-label">{t("Navigation.Overview")}</span></button>
              <button type="button" className={`nav-item ${view === "inventory" ? "active" : ""}`} aria-current={view === "inventory" ? "page" : undefined} onClick={() => navigate("inventory")}><AppIcon name="inventory" /><span className="nav-label">{t("Navigation.Inventory")}</span></button>
              <button type="button" className={`nav-item ${view === "history" ? "active" : ""}`} aria-current={view === "history" ? "page" : undefined} onClick={() => navigate("history")}><AppIcon name="history" /><span className="nav-label">{t("Navigation.History")}</span></button>
              <button type="button" className={`nav-item ${view === "alerts" ? "active" : ""}`} aria-current={view === "alerts" ? "page" : undefined} onClick={() => navigate("alerts")}><AppIcon name="alerts" /><span className="nav-label">{t("Navigation.Alerts")}</span><span className="nav-count" aria-label={t("Overview.ActiveAlertCount", timelineSnapshot.alerts.filter((alert) => alert.state === "active").length)}>{timelineSnapshot.alerts.filter((alert) => alert.state === "active").length}</span></button>
              <button type="button" className={`nav-item ${view === "configuration" ? "active" : ""}`} aria-current={view === "configuration" ? "page" : undefined} onClick={() => navigate("configuration")}><AppIcon name="configuration" /><span className="nav-label">{t("Navigation.Configuration")}</span></button>
            </nav>
          </div>
          <div className="sidebar-note"><strong>{t("Sidebar.State")}</strong><span>{t("Sidebar.ReadOnly")}</span></div>
        </aside>

        <main id="main-content" tabIndex={-1}>
          <section className="page-heading" aria-labelledby="page-title">
            <div>
              <p className="eyebrow">{t(copy.eyebrowKey)}</p>
              <h1 id="page-title">{t(copy.titleKey)}</h1>
              <p>{t(copy.descriptionKey)}</p>
            </div>
            <label className="scenario-control">
              <span>{t("Scenario.Label")}</span>
              <select value={state} onChange={(event) => setState(event.target.value as InventoryState)}>
                {stateOptions.map((option) => <option key={option.value} value={option.value}>{t(option.labelKey)}</option>)}
              </select>
            </label>
          </section>

          {state === "ready" && view === "overview" ? (
            <OverviewView
              items={snapshot.items}
              alerts={timelineSnapshot.alerts}
              now={now}
              generatedAt={snapshot.generatedAt}
              summary={summary}
              onNavigate={navigate}
            />
          ) : state === "ready" && view === "inventory" ? (
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
          ) : state === "ready" && view === "configuration" ? (
            <ConfigurationView snapshot={configurationSnapshot} />
          ) : (
            <OperationalState state={state as Exclude<InventoryState, "ready">} onRetry={() => setState("ready")} />
          )}
        </main>
      </div>

      <footer>
        {t("Footer.Disclaimer")}
      </footer>
    </div>
  );
}

/**
 * Composes the operational overview from the same deterministic inventory and alert adapters used by the detailed views.
 * @param props - Current local snapshot, freshness clock and safe navigation callback.
 * @returns A dense read-only overview without external monitoring or administrative execution.
 */
function OverviewView({
  items,
  alerts,
  now,
  generatedAt,
  summary,
  onNavigate,
}: {
  items: readonly InventoryItem[];
  alerts: readonly AlertItem[];
  now: Date;
  generatedAt: string;
  summary: ReturnType<typeof summarizeInventory>;
  onNavigate: (view: DashboardView) => void;
}) {
  const { locale, t } = useLocalisation();
  const metrics = [
    { label: t("Inventory.Total"), value: summary.total, icon: "database" as const, className: "" },
    { label: t("Inventory.Healthy"), value: summary.healthy, icon: "healthy" as const, className: "healthy" },
    { label: t("Inventory.Degraded"), value: summary.degraded, icon: "degraded" as const, className: "degraded" },
    { label: t("Inventory.Attention"), value: summary.attentionRequired, icon: "critical" as const, className: "critical" },
  ];

  return <section className="overview-layout" aria-label={t("View.Overview.Title")}>
    <p className="updated-at">{t("Inventory.UpdatedAt", formatSystemDateTime(generatedAt, locale), staleAfterMilliseconds / 60_000)}</p>
    <div className="summary-grid overview-summary">
      {metrics.map((metric) => <article className={`summary-card ${metric.className}`.trim()} key={metric.label}>
        <span className="summary-label"><span className="metric-icon"><AppIcon name={metric.icon} /></span><span>{metric.label}</span></span>
        <strong>{metric.value}</strong>
      </article>)}
    </div>

    <div className="overview-grid">
      <section className="overview-panel overview-fleet" aria-labelledby="overview-fleet-title">
        <div className="overview-panel-heading"><div><h2 id="overview-fleet-title">{t("Overview.FleetStatus")}</h2><p>{t("Overview.FleetDescription")}</p></div><button type="button" onClick={() => onNavigate("inventory")}>{t("Overview.ViewInventory")}</button></div>
        <div className="overview-instance-list">
          {items.map((item, index) => <article className="overview-instance-row" key={item.instanceId}>
            <span className={`overview-status-dot ${isStale(item, now) ? "stale" : statusPresentation[item.status].className}`} aria-hidden="true" />
            <div className="overview-instance-name"><strong>{item.displayName}</strong><code>{item.providerType}</code></div>
            <StatusBadge item={item} now={now} />
            <span className="overview-latency">{item.latencyMilliseconds === null ? "—" : `${item.latencyMilliseconds} ms`}</span>
            <svg className={`sparkline sparkline-${index + 1}`} viewBox="0 0 92 28" preserveAspectRatio="none" aria-hidden="true" focusable="false"><polyline points={index === 0 ? "0,20 10,18 20,21 30,12 40,15 50,7 60,13 70,9 80,17 92,8" : index === 1 ? "0,18 10,16 20,19 30,10 40,14 50,9 60,20 70,13 80,15 92,6" : index === 2 ? "0,9 10,14 20,8 30,20 40,12 50,22 60,17 70,24 80,18 92,25" : "0,18 12,18 24,18 36,18 48,18 60,18 72,18 84,18 92,18"} /></svg>
          </article>)}
        </div>
      </section>

      <section className="overview-panel overview-alerts" aria-labelledby="overview-alerts-title">
        <div className="overview-panel-heading"><div><h2 id="overview-alerts-title">{t("Overview.RecentAlerts")}</h2><p>{t("Overview.AlertDescription")}</p></div></div>
        <div className="overview-alert-list">
          {alerts.map((alert) => <article key={alert.alertId}>
            <SeverityBadge severity={alert.severity} />
            <div><strong>{alert.instanceName}</strong><span>{alert.ruleName}</span></div>
            <time dateTime={alert.updatedAt}>{formatSystemTime(alert.updatedAt, locale)}</time>
          </article>)}
        </div>
        <button className="overview-panel-action" type="button" onClick={() => onNavigate("alerts")}>{t("Overview.ViewAlerts")}</button>
      </section>

      <section className="overview-panel overview-trend" aria-labelledby="overview-trend-title">
        <div className="overview-panel-heading"><div><h2 id="overview-trend-title">{t("Overview.SampleTrend")}</h2><p>{t("Overview.DemonstrationChart")}</p></div><span className="read-only-label">{t("Demo.Badge")}</span></div>
        <div className="trend-chart">
          <div className="trend-axis" aria-hidden="true"><span>100</span><span>50</span><span>0 ms</span></div>
          <svg viewBox="0 0 640 150" preserveAspectRatio="none" role="img" aria-label={t("Overview.TrendAccessibleLabel")}>
            <path className="chart-grid" d="M0 25H640M0 75H640M0 125H640" />
            <polyline className="trend-primary" points="0,118 40,102 80,109 120,82 160,91 200,64 240,76 280,55 320,70 360,48 400,61 440,44 480,54 520,34 560,47 600,29 640,38" />
            <polyline className="trend-secondary" points="0,92 40,88 80,74 120,81 160,68 200,76 240,59 280,66 320,49 360,58 400,45 440,53 480,42 520,49 560,36 600,43 640,30" />
          </svg>
        </div>
      </section>

      <section className="overview-panel overview-providers" aria-labelledby="overview-providers-title">
        <div className="overview-panel-heading"><div><h2 id="overview-providers-title">{t("Overview.ProviderFixture")}</h2><p>{t("Overview.ProviderDescription")}</p></div></div>
        <div className="provider-figure">
          <svg className="provider-ring" viewBox="0 0 44 44" role="img" aria-label={t("Overview.ProviderAccessibleLabel", items.length)}>
            <circle className="provider-ring-track" cx="22" cy="22" r="15.9" />
            {items.map((item, index) => <circle key={item.instanceId} className={`provider-ring-segment segment-${index + 1}`} cx="22" cy="22" r="15.9" strokeDasharray="24 76" strokeDashoffset={25 - index * 25} />)}
          </svg>
          <ul>{items.map((item, index) => <li key={item.instanceId}><span className={`provider-key segment-${index + 1}`} aria-hidden="true" /><code>{item.providerType}</code><strong>1</strong></li>)}</ul>
        </div>
      </section>
    </div>
  </section>;
}

const severityPresentation: Record<EventSeverity, { symbol: string; labelKey: MessageKey; className: string }> = {
  information: { symbol: "●", labelKey: "Severity.Information", className: "information" },
  warning: { symbol: "▲", labelKey: "Severity.Warning", className: "degraded" },
  critical: { symbol: "■", labelKey: "Severity.Critical", className: "critical" },
};

function SeverityBadge({ severity }: { severity: EventSeverity }) {
  const { t } = useLocalisation();
  const item = severityPresentation[severity];
  return <span className={`status-badge ${item.className}`}><span aria-hidden="true">{item.symbol}</span> {t(item.labelKey)}</span>;
}

function HistoryView({ snapshot }: { snapshot: ReturnType<typeof buildTimelineAlertSnapshot> }) {
  const { locale, t } = useLocalisation();
  const [query, setQuery] = useState("");
  const [severity, setSeverity] = useState<"all" | EventSeverity>("all");
  const events = useMemo(() => filterTimeline(snapshot.events, query, severity, locale), [snapshot.events, query, severity, locale]);
  return <section className="inventory-panel timeline-panel" aria-labelledby="history-title">
    <div className="panel-header"><div><h2 id="history-title">{t("History.Title")}</h2><p aria-live="polite">{t("History.Count", events.length, snapshot.events.length)}</p></div><span className="read-only-label">history-alerts.v1</span></div>
    <div className="filters" role="search"><label><span>{t("History.Search")}</span><input type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder={t("History.SearchPlaceholder")} /></label><label><span>{t("History.Severity")}</span><select value={severity} onChange={(event) => setSeverity(event.target.value as "all" | EventSeverity)}><option value="all">{t("History.AllSeverities")}</option><option value="information">{t("Severity.Information")}</option><option value="warning">{t("Severity.Warning")}</option><option value="critical">{t("Severity.Critical")}</option></select></label></div>
    {events.length === 0 ? <FilteredEmpty message={t("History.FilteredEmpty")} /> : <ol className="timeline-list">{events.map((event) => <TimelineRow key={event.eventId} event={event} />)}</ol>}
  </section>;
}

function TimelineRow({ event }: { event: TimelineEventItem }) {
  const { locale } = useLocalisation();
  return <li className={`timeline-item ${event.severity}`}><div className="timeline-marker" aria-hidden="true" /><article><div className="timeline-heading"><div><h3>{event.eventType}</h3><p>{event.instanceName} · <code>{event.providerType}</code></p></div><SeverityBadge severity={event.severity} /></div><p className="timeline-summary">{event.summary}</p><time dateTime={event.occurredAt}>{formatSystemDateTime(event.occurredAt, locale)}</time></article></li>;
}

function AlertsView({ alerts, generatedAt }: { alerts: readonly AlertItem[]; generatedAt: string }) {
  const { locale, t } = useLocalisation();
  const active = alerts.filter((alert) => alert.state === "active").length;
  const critical = alerts.filter((alert) => alert.severity === "critical" && alert.state !== "resolved").length;
  return <section aria-labelledby="alerts-title"><p className="updated-at">{t("Alerts.UpdatedAt", formatSystemDateTime(generatedAt, locale))}</p><div className="summary-grid alert-summary"><article className="summary-card critical"><span><i aria-hidden="true">■</i> {t("Alerts.CriticalOpen")}</span><strong>{critical}</strong></article><article className="summary-card degraded"><span><i aria-hidden="true">▲</i> {t("Alerts.Active")}</span><strong>{active}</strong></article><article className="summary-card"><span><i aria-hidden="true">✓</i> {t("Alerts.TotalVisible")}</span><strong>{alerts.length}</strong></article></div><div className="inventory-panel"><div className="panel-header"><div><h2 id="alerts-title">{t("Alerts.Title")}</h2><p>{t("Alerts.Description")}</p></div><span className="read-only-label">{t("Common.ReadOnly")}</span></div><div className="alert-list">{alerts.map((alert) => <article className="alert-card" key={alert.alertId}><div className="alert-card-heading"><SeverityBadge severity={alert.severity} /><span className="alert-state">{t(alert.state === "active" ? "AlertState.Active" : alert.state === "acknowledged" ? "AlertState.Acknowledged" : alert.state === "silenced" ? "AlertState.Silenced" : "AlertState.Resolved")}</span></div><h3>{alert.ruleName}</h3><p>{alert.summary}</p><dl><div><dt>{t("Common.Instance")}</dt><dd>{alert.instanceName}</dd></div><div><dt>{t("Common.Provider")}</dt><dd><code>{alert.providerType}</code></dd></div><div><dt>{t("Common.Updated")}</dt><dd><time dateTime={alert.updatedAt}>{formatSystemDateTime(alert.updatedAt, locale)}</time></dd></div></dl></article>)}</div></div></section>;
}

const previewCopy: Record<ActionPreview, { symbol: string; titleKey: MessageKey; messageKey: MessageKey }> = {
  confirmationRequired: { symbol: "?", titleKey: "Preview.Confirmation.Title", messageKey: "Preview.Confirmation.Message" },
  denied: { symbol: "⊘", titleKey: "Preview.Denied.Title", messageKey: "Preview.Denied.Message" },
  unsupported: { symbol: "—", titleKey: "Preview.Unsupported.Title", messageKey: "Preview.Unsupported.Message" },
  unavailable: { symbol: "↯", titleKey: "Preview.Unavailable.Title", messageKey: "Preview.Unavailable.Message" },
  unknown: { symbol: "?", titleKey: "Preview.Unknown.Title", messageKey: "Preview.Unknown.Message" },
};

/**
 * Presents non-secret configuration and capability decisions without dispatching operations.
 * The native modal dialogue contains keyboard focus and restores it to the invoking control when closed.
 */
function ConfigurationView({ snapshot }: { snapshot: ReturnType<typeof buildConfigurationSnapshot> }) {
  const { t } = useLocalisation();
  const [authorized, setAuthorized] = useState(true);
  const [preview, setPreview] = useState<ActionPreview | null>(null);
  const dialogRef = useRef<HTMLDialogElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  const openerRef = useRef<HTMLButtonElement | null>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!preview || !dialog) return undefined;

    // showModal provides platform-level background inertness; explicit focus handling makes entry and restoration deterministic.
    if (!dialog.open) dialog.showModal();
    closeButtonRef.current?.focus();

    /** Contains keyboard traversal and handles the modal dismissal shortcut. */
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        setPreview(null);
        return;
      }
      if (event.key !== "Tab") return;

      const focusable = [...dialog.querySelectorAll<HTMLElement>("button:not(:disabled), [href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex]:not([tabindex='-1'])")];
      if (focusable.length === 0) {
        event.preventDefault();
        dialog.focus();
        return;
      }

      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };

    dialog.addEventListener("keydown", handleKeyDown);
    return () => {
      dialog.removeEventListener("keydown", handleKeyDown);
      if (dialog.open) dialog.close();
      openerRef.current?.focus();
    };
  }, [preview]);

  /** Opens a capability decision while retaining its invoking control for focus restoration. */
  const openPreview = (capabilityId: string, opener: HTMLButtonElement) => {
    openerRef.current = opener;
    setPreview(previewAction(snapshot, capabilityId, authorized));
  };
  /** Opens the non-executable confirmation example from the supplied control. */
  const openConfirmationExample = (opener: HTMLButtonElement) => {
    openerRef.current = opener;
    setPreview("confirmationRequired");
  };
  /** Closes the current preview; the effect cleanup restores focus safely. */
  const closePreview = () => setPreview(null);

  return <section className="configuration-layout" aria-labelledby="configuration-title">
    <div className="inventory-panel configuration-panel"><div className="panel-header"><div><h2 id="configuration-title">{snapshot.instanceName}</h2><p><code>{snapshot.providerType}</code> · configuration-capabilities.v1</p></div><span className="read-only-label">{t("Common.NotPersisted")}</span></div><div className="configuration-fields">{snapshot.fields.map((field) => <article key={field.key}><span>{field.label}</span><strong>{field.safeValue}</strong><p>{field.description}</p></article>)}</div></div>
    <div className="inventory-panel capability-panel"><div className="panel-header"><div><h2>{t("Configuration.AdminTitle")}</h2><p>{t("Configuration.AdminDescription")}</p></div><label className="permission-toggle"><span>{t("Configuration.PermissionScenario")}</span><select value={authorized ? "authorized" : "denied"} onChange={(event) => setAuthorized(event.target.value === "authorized")}><option value="authorized">{t("Configuration.Authorized")}</option><option value="denied">{t("Configuration.Denied")}</option></select></label></div><div className="capability-list">{snapshot.capabilities.map((capability) => <article key={capability.capabilityId}><div><h3>{capability.displayName}</h3><code>{capability.capabilityId}</code></div><span className={`capability-state ${capability.state}`}>{capability.state === "unsupported" ? t("Configuration.Unsupported") : capability.state}</span><p>{capability.reasonCode}</p><button type="button" onClick={(event) => openPreview(capability.capabilityId, event.currentTarget)}>{t("Configuration.ReviewDecision")}</button></article>)}</div><div className="confirmation-example"><div><strong>{t("Configuration.ConfirmationTitle")}</strong><p>{t("Configuration.ConfirmationDescription")}</p></div><button type="button" onClick={(event) => openConfirmationExample(event.currentTarget)}>{t("Configuration.ViewConfirmation")}</button></div></div>
    {preview && <dialog ref={dialogRef} className="confirmation-dialog" role="dialog" aria-modal="true" aria-labelledby="preview-title" onCancel={(event) => { event.preventDefault(); closePreview(); }}><span className="state-symbol" aria-hidden="true">{previewCopy[preview].symbol}</span><h2 id="preview-title">{t(previewCopy[preview].titleKey)}</h2><p>{t(previewCopy[preview].messageKey)}</p><div className="dialog-actions"><button ref={closeButtonRef} type="button" onClick={closePreview}>{t("Dialog.Close")}</button><button type="button" disabled>{t("Dialog.ExecuteDisabled")}</button></div></dialog>}
  </section>;
}

function FilteredEmpty({ message }: { message: string }) {
  const { t } = useLocalisation();
  return <div className="filtered-empty"><span aria-hidden="true">⌕</span><strong>{t("FilteredEmpty.Title")}</strong><p>{message}</p></div>;
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
  const { locale, t } = useLocalisation();
  const cards: ReadonlyArray<{ label: string; value: number; icon: AppIconName; className: string }> = [
    { label: t("Inventory.Total"), value: summary.total, icon: "database", className: "total" },
    { label: t("Inventory.Healthy"), value: summary.healthy, icon: "healthy", className: "healthy" },
    { label: t("Inventory.Degraded"), value: summary.degraded, icon: "degraded", className: "degraded" },
    { label: t("Inventory.Attention"), value: summary.attentionRequired, icon: "critical", className: "critical" },
    { label: t("Inventory.Stale"), value: summary.stale, icon: "stale", className: "stale" },
  ];

  return (
    <section id="inventory" aria-label={t("Inventory.Caption")}>
      <p className="updated-at">{t("Inventory.UpdatedAt", formatSystemDateTime(generatedAt, locale), staleAfterMilliseconds / 60_000)}</p>
      <div className="summary-grid">
        {cards.map((card) => (
          <article className={`summary-card ${card.className}`} key={card.label}>
            <span className="summary-label"><span className="metric-icon"><AppIcon name={card.icon} /></span><span>{card.label}</span></span>
            <strong>{card.value}</strong>
          </article>
        ))}
      </div>

      <section className="inventory-panel" aria-labelledby="inventory-title">
        <div className="panel-header">
          <div><h2 id="inventory-title">{t("Inventory.Title")}</h2><p aria-live="polite">{t("Inventory.Count", items.length, summary.total)}</p></div>
          <span className="read-only-label">{t("Common.ReadOnly")}</span>
        </div>
        <div className="filters" role="search">
          <label><span>{t("Inventory.Search")}</span><input type="search" value={query} onChange={(event) => onQueryChange(event.target.value)} placeholder={t("Inventory.SearchPlaceholder")} /></label>
          <label><span>{t("Common.Status")}</span><select value={statusFilter} onChange={(event) => onStatusChange(event.target.value as "all" | HealthStatus | "stale")}>
            <option value="all">{t("Common.All")}</option><option value="healthy">{t("Status.Healthy")}</option><option value="degraded">{t("Status.Degraded")}</option><option value="timeout">{t("Status.Timeout")}</option><option value="unknown">{t("Status.Unknown")}</option><option value="stale">{t("Status.Stale")}</option>
          </select></label>
        </div>
        {items.length === 0 ? (
          <FilteredEmpty message={t("Inventory.FilteredEmpty")} />
        ) : (
          <>
            <div className="table-wrap">
              <table>
                <caption className="sr-only">{t("Inventory.Caption")}</caption>
                <thead><tr><th scope="col">{t("Common.Instance")}</th><th scope="col">{t("Common.Provider")}</th><th scope="col">{t("Common.Support")}</th><th scope="col">{t("Common.Environment")}</th><th scope="col">{t("Common.Status")}</th><th scope="col">{t("Common.ObservedAt")}</th><th scope="col">{t("Common.Latency")}</th></tr></thead>
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
  const { locale } = useLocalisation();
  return <tr><th scope="row"><strong>{item.displayName}</strong><small>{item.locationLabel}</small></th><td><code>{item.providerType}</code></td><td>{item.supportLabel}</td><td>{item.environment}</td><td><StatusBadge item={item} now={now} /></td><td><time dateTime={item.observedAt}>{formatSystemDateTime(item.observedAt, locale)}</time></td><td>{item.latencyMilliseconds === null ? "—" : `${item.latencyMilliseconds} ms`}</td></tr>;
}

function InventoryMobileCard({ item, now }: { item: InventoryItem; now: Date }) {
  const { locale, t } = useLocalisation();
  return <article className="instance-card"><div className="instance-card-heading"><div><h3>{item.displayName}</h3><p>{item.locationLabel}</p></div><StatusBadge item={item} now={now} /></div><dl><div><dt>{t("Common.Provider")}</dt><dd><code>{item.providerType}</code></dd></div><div><dt>{t("Common.Support")}</dt><dd>{item.supportLabel}</dd></div><div><dt>{t("Common.Environment")}</dt><dd>{item.environment}</dd></div><div><dt>{t("Common.ObservedAt")}</dt><dd><time dateTime={item.observedAt}>{formatSystemDateTime(item.observedAt, locale)}</time></dd></div><div><dt>{t("Common.Latency")}</dt><dd>{item.latencyMilliseconds === null ? "—" : `${item.latencyMilliseconds} ms`}</dd></div></dl></article>;
}

function OperationalState({ state, onRetry }: { state: Exclude<InventoryState, "ready">; onRetry: () => void }) {
  const { t } = useLocalisation();
  const content = stateMessages[state];
  return <section className="operational-state" role={state === "error" || state === "denied" ? "alert" : "status"} aria-live="polite" aria-busy={state === "loading"}>
    {state === "loading" && <span className="loading-line" aria-hidden="true" />}
    <span className="state-symbol" aria-hidden="true">{content.symbol}</span><h2>{t(content.titleKey)}</h2><p>{t(content.messageKey)}</p>
    {content.retry && <button type="button" onClick={onRetry}>{t("Operational.Retry")}</button>}
  </section>;
}
