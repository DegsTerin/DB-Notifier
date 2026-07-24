/** Module purpose: Presents the authenticated O4 synthetic projection as a factual, read-only dedicated Dashboard. */
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { LanguageSelector } from "./LanguageSelector";
import { ThemeSelector } from "./ThemeSelector";
import { useLocalisation } from "./LocalisationProvider";
import { formatSystemDateTime } from "./systemDateTime";
import {
  observerText,
  type ObserverMessageKey,
} from "./observerSandboxMessages";
import {
  readObserverProjection,
  type ObserverProjectionPackage,
  type ObserverSignal,
} from "./observerSandbox";
import "./observerSandbox.css";

type ObserverViewState =
  | { readonly state: "loading" }
  | { readonly state: "ready"; readonly projection: ObserverProjectionPackage }
  | { readonly state: "unavailable" };

type ObserverIconName = "activation" | "evidence" | "forecast" | "limitations" | "signal" | "stale";

const limitationKeys: Readonly<Record<string, ObserverMessageKey>> = {
  "o4.limitation.forecast_unavailable": "Limitation.ForecastUnavailable",
  "o4.limitation.no_holdout_inference": "Limitation.NoHoldoutInference",
  "o4.limitation.synthetic_not_production": "Limitation.SyntheticNotProduction",
  "o4.limitation.non_authorising": "Limitation.NonAuthorising",
  "o4.limitation.physical_accessibility_not_tested": "Limitation.PhysicalAccessibilityNotTested",
};

const metricKeys: Readonly<Record<string, ObserverMessageKey>> = {
  "database.probe.duration.degraded": "Metric.DurationDegraded",
};

/**
 * Renders the compact outlined icon family owned by the isolated Observer composition.
 * @param name - Stable semantic meaning represented next to authoritative visible text.
 * @param className - Optional styling hook that does not change the accessible outcome.
 * @returns A decorative code-native SVG hidden from assistive technology.
 */
function ObserverIcon({
  name,
  className = "",
}: {
  readonly name: ObserverIconName;
  readonly className?: string;
}) {
  let content: ReactNode;
  switch (name) {
    case "activation":
      content = <><path d="M12 3.5 19 6v5.2c0 4.2-2.8 7.5-7 9.3-4.2-1.8-7-5.1-7-9.3V6Z" /><path d="M9 12h6" /></>;
      break;
    case "evidence":
      content = <><path d="M8.5 7.5 6 10a3.5 3.5 0 0 0 5 5l2.5-2.5" /><path d="m15.5 16.5 2.5-2.5a3.5 3.5 0 0 0-5-5l-2.5 2.5" /></>;
      break;
    case "forecast":
      content = <><path d="M4 17 9 12l3 3 7-8" /><path d="M15 7h4v4" /><circle cx="12" cy="12" r="9" /></>;
      break;
    case "limitations":
      content = <><path d="M12 3.5 21 19H3Z" /><path d="M12 9v4" /><path d="M12 16.25h.01" /></>;
      break;
    case "signal":
      content = <><path d="M3 12h4l2-5 4 10 2-5h6" /><circle cx="12" cy="12" r="9" /></>;
      break;
    case "stale":
      content = <><circle cx="12" cy="12" r="9" /><path d="M12 7.5V12l3 2" /></>;
      break;
  }
  return <svg
    className={`observer-icon ${className}`.trim()}
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.75"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
    focusable="false"
  >{content}</svg>;
}

/** Renders the isolated Observer shell and performs exactly one bounded read on mount. */
export function ObserverSandboxApp() {
  const { locale, t } = useLocalisation();
  const ot = (key: ObserverMessageKey, ...values: readonly (string | number)[]) =>
    observerText(locale, key, ...values);
  const [view, setView] = useState<ObserverViewState>({ state: "loading" });

  useEffect(() => {
    const controller = new AbortController();
    void readObserverProjection(controller.signal)
      .then((projection) => setView({ state: "ready", projection }))
      .catch(() => {
        if (!controller.signal.aborted) setView({ state: "unavailable" });
      });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    document.title = `DB Notifier — ${ot("Title")}`;
  }, [locale]);

  return <div className="observer-shell">
    <a className="skip-link" href="#observer-main">{t("Navigation.Skip")}</a>
    <header className="topbar observer-topbar">
      <div className="brand-lockup" role="img" aria-label="DB Notifier">
        <span className="brand-mark"><img src="/dbnotifier-icon.unknown.svg" alt="" /></span>
        <span><strong className="brand-wordmark"><span>DB</span><span>Notifier</span></strong><small>{t("Brand.Subtitle")}</small></span>
      </div>
      <div className="topbar-controls">
        <LanguageSelector />
        <ThemeSelector />
        <span className="observer-synthetic-badge">{ot("SyntheticBadge")}</span>
      </div>
    </header>
    <main id="observer-main" className="observer-main" tabIndex={-1}>
      <header className="observer-heading">
        <div>
          <p className="eyebrow">{ot("Eyebrow")}</p>
          <h1>{ot("Title")}</h1>
          <p>{ot("Description")}</p>
        </div>
        <div className="observer-activation" role="status">
          <span className="observer-activation-icon"><ObserverIcon name="activation" /></span>
          <span className="observer-activation-copy">
            <strong>{ot("ActivationNone")}</strong>
            <span>{ot("ReadOnly")}</span>
          </span>
          <span className="observer-mobile-synthetic">{ot("SyntheticBadge")}</span>
        </div>
      </header>
      {view.state === "loading" && <ObserverState title={ot("Loading")} />}
      {view.state === "unavailable" &&
        <ObserverState
          title={ot("UnavailableTitle")}
          message={ot("UnavailableMessage")}
          alert
        />}
      {view.state === "ready" &&
        <ObserverProjectionView projection={view.projection} />}
    </main>
    <footer className="observer-footer">
      <span>{ot("NoExternalData")}</span>
      <strong>{ot("SyntheticApprovalWarning")}</strong>
    </footer>
  </div>;
}

/** Presents loading and fail-closed states without exposing transport details. */
function ObserverState({
  title,
  message,
  alert = false,
}: {
  readonly title: string;
  readonly message?: string;
  readonly alert?: boolean;
}) {
  return <section className="observer-state" role={alert ? "alert" : "status"} aria-live="polite">
    <span className="observer-state-icon"><ObserverIcon name={alert ? "limitations" : "signal"} /></span>
    <div><h2>{title}</h2>{message && <p>{message}</p>}</div>
  </section>;
}

/** Renders all verified projection sections while keeping trace digests compact but fully accessible. */
function ObserverProjectionView({ projection }: { readonly projection: ObserverProjectionPackage }) {
  const { locale } = useLocalisation();
  const ot = (key: ObserverMessageKey, ...values: readonly (string | number)[]) =>
    observerText(locale, key, ...values);
  const payload = projection.payload;
  const generatedAt = useMemo(() => new Date(payload.generatedAtUtc), [payload.generatedAtUtc]);
  return <>
    <section className="observer-summary" aria-label={ot("ProjectionSummary")}>
      <ObserverSummaryCard
        icon="stale"
        label={ot("Summary.CurrentEvidence")}
        value={ot((payload.signals[0]?.freshness as ObserverMessageKey | undefined) ?? "Unknown")}
        detail={ot("Summary.SignalCount", payload.signals.length)}
        emphasis="stale"
      />
      <ObserverSummaryCard
        icon="forecast"
        label={ot("Summary.Forecast")}
        value={ot("ForecastUnknown")}
        detail={ot("Summary.ForecastDetail")}
      />
      <ObserverSummaryCard
        icon="activation"
        label={ot("Summary.Activation")}
        value="None"
        detail={ot("Summary.ReadOnly")}
      />
    </section>
    <div className="observer-grid">
      <section className="observer-panel observer-signals" aria-labelledby="observer-signals-title">
        <SectionHeader
          id="observer-signals-title"
          icon="signal"
          title={ot("CurrentSignals")}
          description={ot("CurrentSignalsDescription")}
          badge={formatSystemDateTime(generatedAt, locale)}
        />
        <div className="observer-signal-list">
          {payload.signals.map((signal) =>
            <SignalCard key={signal.signalId} signal={signal} />)}
        </div>
      </section>

      <section className="observer-panel observer-forecasts" aria-labelledby="observer-forecast-title">
        <SectionHeader
          id="observer-forecast-title"
          icon="forecast"
          title={ot("Forecasts")}
          description={ot("ForecastsDescription")}
          badge={ot("ForecastUnknown")}
        />
        <div className="observer-forecast-state">
          <span><ObserverIcon name="forecast" /></span>
          <div>
            <strong>{ot("ForecastUnknown")}</strong>
            <p>{ot("ForecastUnavailable")}</p>
          </div>
        </div>
        <LimitationList values={payload.forecasts.limitations} />
      </section>

      <section className="observer-panel observer-trace" aria-labelledby="observer-evidence-title">
        <SectionHeader
          id="observer-evidence-title"
          icon="evidence"
          title={ot("Evidence")}
          description={ot("EvidenceDescription")}
        />
        <dl>
          <TraceValue index={1} label={ot("Policy")} value={payload.trace.policyDigest} detailsLabel={ot("FullIdentifier")} />
          <TraceValue index={2} label={ot("Corpus")} value={payload.trace.corpusManifestDigest} detailsLabel={ot("FullIdentifier")} />
          <TraceValue index={3} label={ot("Result")} value={payload.trace.o3ResultDigest} detailsLabel={ot("FullIdentifier")} />
          <TraceValue index={4} label={ot("Source")} value={payload.sourceLabel} detailsLabel={ot("FullIdentifier")} />
        </dl>
      </section>

      <section className="observer-panel observer-limitations" aria-labelledby="observer-limitations-title">
        <SectionHeader
          id="observer-limitations-title"
          icon="limitations"
          title={ot("Limitations")}
          description={ot("ReadOnly")}
        />
        <LimitationList values={payload.limitations} />
      </section>
    </div>
  </>;
}

/** Displays one complete deterministic signal with factual value, policy and evidence boundaries. */
function SignalCard({ signal }: { readonly signal: ObserverSignal }) {
  const { locale } = useLocalisation();
  const ot = (key: ObserverMessageKey, ...values: readonly (string | number)[]) =>
    observerText(locale, key, ...values);
  const dispositionKey = signal.disposition as ObserverMessageKey;
  const freshnessKey = signal.freshness as ObserverMessageKey;
  const metricLabelKey = metricKeys[signal.metricKey];
  return <article className={`observer-signal-card ${signal.freshness.toLowerCase()}`}>
    <div className="observer-signal-heading">
      <div className="observer-metric">
        <span className="observer-metric-icon"><ObserverIcon name="signal" /></span>
        <span>
          <span className="observer-metric-label">{ot("Metric")}</span>
          <h3>{metricLabelKey ? ot(metricLabelKey) : signal.metricKey}</h3>
          {metricLabelKey && <code>{signal.metricKey}</code>}
        </span>
      </div>
      <span className={`observer-state-pill ${signal.freshness.toLowerCase()}`}>
        <ObserverIcon name={signal.freshness === "Stale" ? "stale" : "signal"} />
        {ot(freshnessKey)}
      </span>
    </div>
    <p className="observer-disposition"><strong>{ot(dispositionKey)}</strong><span>{signal.ruleId} · {signal.ruleVersion}</span></p>
    <dl className="observer-signal-facts">
      <Fact label={ot("ObservedValue")} value={signal.observedValue === null ? ot("Unknown") : `${signal.observedValue} ${signal.unit}`} />
      <Fact label={ot("Threshold")} value={`${signal.threshold} ${signal.unit}`} />
      <Fact label={ot("EvaluatedAt")} value={formatSystemDateTime(new Date(signal.evaluatedAtUtc), locale)} />
      <Fact label={ot("ValidUntil")} value={signal.validUntilUtc ? formatSystemDateTime(new Date(signal.validUntilUtc), locale) : ot("Unknown")} />
    </dl>
    <div className="observer-evidence-count">{ot("EvidenceCount", signal.evidenceIds.length)}</div>
  </article>;
}

/** Renders one consistent section heading and optional factual badge. */
function SectionHeader({
  id,
  title,
  description,
  badge,
  icon,
}: {
  readonly id: string;
  readonly title: string;
  readonly description: string;
  readonly badge?: string;
  readonly icon: ObserverIconName;
}) {
  return <header className="observer-panel-heading">
    <div className="observer-section-title">
      <span className="observer-section-icon"><ObserverIcon name={icon} /></span>
      <span><h2 id={id}>{title}</h2><p>{description}</p></span>
    </div>
    {badge && <span className="read-only-label">{badge}</span>}
  </header>;
}

/** Presents one primary projection fact in the scannable summary band. */
function ObserverSummaryCard({
  icon,
  label,
  value,
  detail,
  emphasis = "neutral",
}: {
  readonly icon: ObserverIconName;
  readonly label: string;
  readonly value: string;
  readonly detail: string;
  readonly emphasis?: "neutral" | "stale";
}) {
  return <article className={`observer-summary-card ${emphasis}`}>
    <span className="observer-summary-icon"><ObserverIcon name={icon} /></span>
    <span className="observer-summary-copy">
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{detail}</small>
    </span>
  </article>;
}

/** Renders one label/value fact in the signal grid. */
function Fact({ label, value }: { readonly label: string; readonly value: string }) {
  return <div><dt>{label}</dt><dd>{value}</dd></div>;
}

/** Renders one ordered trace value compactly while retaining a keyboard-accessible complete identifier. */
function TraceValue({
  index,
  label,
  value,
  detailsLabel,
}: {
  readonly index: number;
  readonly label: string;
  readonly value: string;
  readonly detailsLabel: string;
}) {
  const compact = value.length > 40 ? `${value.slice(0, 10)}…${value.slice(-8)}` : value;
  return <div className="observer-trace-entry" data-step={index}>
    <dt>{label}</dt>
    <dd>
      <code className="observer-trace-compact" title={value}>{compact}</code>
      {compact !== value &&
        <details>
          <summary>{detailsLabel}</summary>
          <code>{value}</code>
        </details>}
    </dd>
  </div>;
}

/** Resolves only known stable limitation codes to canonical localised text. */
function LimitationList({ values }: { readonly values: readonly string[] }) {
  const { locale } = useLocalisation();
  return <ul className="observer-limitation-list">
    {values.map((value) =>
      <li key={value}>{observerText(locale, limitationKeys[value] ?? "Unknown")}</li>)}
  </ul>;
}
