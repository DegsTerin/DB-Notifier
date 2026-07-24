/** Module purpose: Presents the authenticated O4 synthetic projection as a factual, read-only dedicated Dashboard. */
import { useEffect, useMemo, useState } from "react";
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

const limitationKeys: Readonly<Record<string, ObserverMessageKey>> = {
  "o4.limitation.forecast_unavailable": "Limitation.ForecastUnavailable",
  "o4.limitation.no_holdout_inference": "Limitation.NoHoldoutInference",
  "o4.limitation.synthetic_not_production": "Limitation.SyntheticNotProduction",
  "o4.limitation.non_authorising": "Limitation.NonAuthorising",
  "o4.limitation.physical_accessibility_not_tested": "Limitation.PhysicalAccessibilityNotTested",
};

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
          <strong>{ot("ActivationNone")}</strong>
          <span>{ot("ReadOnly")}</span>
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
    <span className="observer-state-icon" aria-hidden="true">{alert ? "!" : "…"}</span>
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
  return <div className="observer-grid">
    <section className="observer-panel observer-signals" aria-labelledby="observer-signals-title">
      <SectionHeader
        id="observer-signals-title"
        title={ot("CurrentSignals")}
        description={ot("CurrentSignalsDescription")}
        badge={formatSystemDateTime(generatedAt, locale)}
      />
      <div className="observer-signal-list">
        {payload.signals.map((signal) =>
          <SignalCard key={signal.signalId} signal={signal} />)}
      </div>
    </section>

    <section className="observer-panel" aria-labelledby="observer-forecast-title">
      <SectionHeader
        id="observer-forecast-title"
        title={ot("Forecasts")}
        description={ot("ForecastsDescription")}
        badge={ot("ForecastUnknown")}
      />
      <div className="observer-forecast-state">
        <span aria-hidden="true">?</span>
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
        title={ot("Evidence")}
        description={ot("EvidenceDescription")}
      />
      <dl>
        <TraceValue label={ot("Policy")} value={payload.trace.policyDigest} />
        <TraceValue label={ot("Corpus")} value={payload.trace.corpusManifestDigest} />
        <TraceValue label={ot("Result")} value={payload.trace.o3ResultDigest} />
        <TraceValue label={ot("Source")} value={payload.sourceLabel} />
      </dl>
    </section>

    <section className="observer-panel observer-limitations" aria-labelledby="observer-limitations-title">
      <SectionHeader
        id="observer-limitations-title"
        title={ot("Limitations")}
        description={ot("ReadOnly")}
      />
      <LimitationList values={payload.limitations} />
    </section>
  </div>;
}

/** Displays one complete deterministic signal with factual value, policy and evidence boundaries. */
function SignalCard({ signal }: { readonly signal: ObserverSignal }) {
  const { locale } = useLocalisation();
  const ot = (key: ObserverMessageKey, ...values: readonly (string | number)[]) =>
    observerText(locale, key, ...values);
  const dispositionKey = signal.disposition as ObserverMessageKey;
  const freshnessKey = signal.freshness as ObserverMessageKey;
  return <article className={`observer-signal-card ${signal.freshness.toLowerCase()}`}>
    <div className="observer-signal-heading">
      <div><span className="observer-metric-label">{ot("Metric")}</span><h3>{signal.metricKey}</h3></div>
      <span className={`observer-state-pill ${signal.freshness.toLowerCase()}`}>
        <span aria-hidden="true">{signal.freshness === "Current" ? "●" : signal.freshness === "Stale" ? "◷" : "?"}</span>
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
}: {
  readonly id: string;
  readonly title: string;
  readonly description: string;
  readonly badge?: string;
}) {
  return <header className="observer-panel-heading">
    <div><h2 id={id}>{title}</h2><p>{description}</p></div>
    {badge && <span className="read-only-label">{badge}</span>}
  </header>;
}

/** Renders one label/value fact in the signal grid. */
function Fact({ label, value }: { readonly label: string; readonly value: string }) {
  return <div><dt>{label}</dt><dd>{value}</dd></div>;
}

/** Renders a full trace value visually compact while preserving its complete accessible text. */
function TraceValue({ label, value }: { readonly label: string; readonly value: string }) {
  return <div><dt>{label}</dt><dd><code title={value}>{value}</code></dd></div>;
}

/** Resolves only known stable limitation codes to canonical localised text. */
function LimitationList({ values }: { readonly values: readonly string[] }) {
  const { locale } = useLocalisation();
  return <ul className="observer-limitation-list">
    {values.map((value) =>
      <li key={value}>{observerText(locale, limitationKeys[value] ?? "Unknown")}</li>)}
  </ul>;
}
