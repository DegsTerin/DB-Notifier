/** Module purpose: Reads and validates the exact O4 read-only projection from the dedicated HTTPS loopback sandbox. */

export const observerProjectionSchemaVersion = "o4.observer.projection.v1" as const;
const projectionRoute = "/api/v1/observer-sandbox/projection";
const testSubjectHeader = "X-DBN-O4-Test-Subject";
const testSubject = "o4-local-observer-review";
const maximumBodyBytes = 256 * 1024;
const maximumTextLength = 512;
const maximumSignals = 32;
const maximumEvidence = 64;
const maximumLimitations = 32;
const requestTimeoutMilliseconds = 10_000;
const allowedProjectionLimitations = new Set([
  "o4.limitation.synthetic_not_production",
  "o4.limitation.non_authorising",
  "o4.limitation.physical_accessibility_not_tested",
]);
const allowedForecastLimitations = new Set([
  "o4.limitation.forecast_unavailable",
  "o4.limitation.no_holdout_inference",
]);

type ObserverFreshness = "Current" | "Stale" | "Unknown";

export interface ObserverTrace {
  readonly o2EnvelopeDigest: string;
  readonly o3ResultDigest: string;
  readonly policyDigest: string;
  readonly corpusManifestDigest: string;
  readonly corpusRevision: number;
  readonly evaluationId: string;
}

export interface ObserverSignal {
  readonly signalId: string;
  readonly ruleId: string;
  readonly ruleVersion: string;
  readonly metricKey: string;
  readonly unit: string;
  readonly disposition: "Detected" | "NotDetected" | "InsufficientEvidence";
  readonly severity: string;
  readonly evaluatedAtUtc: string;
  readonly observedAtUtc: string | null;
  readonly validUntilUtc: string | null;
  readonly freshness: ObserverFreshness;
  readonly observedValue: number | null;
  readonly threshold: number;
  readonly evidenceIds: readonly string[];
  readonly limitations: readonly string[];
}

export interface ObserverProjection {
  readonly schemaVersion: typeof observerProjectionSchemaVersion;
  readonly projectionId: string;
  readonly generatedAtUtc: string;
  readonly validUntilUtc: string;
  readonly processingCompleted: true;
  readonly synthetic: true;
  readonly operational: false;
  readonly productionRepresentative: false;
  readonly isAuthorising: false;
  readonly revoked: false;
  readonly superseded: false;
  readonly activationState: "None";
  readonly sourceLabel: string;
  readonly trace: ObserverTrace;
  readonly signals: readonly ObserverSignal[];
  readonly forecasts: {
    readonly state: "Unknown";
    readonly code: string;
    readonly limitations: readonly string[];
  };
  readonly limitations: readonly string[];
}

export interface ObserverProjectionPackage {
  readonly payload: ObserverProjection;
  readonly projectionDigest: string;
}

type LocationBoundary = Pick<Location, "protocol" | "hostname" | "origin">;

/** Returns true only for the exact build flag on an HTTPS loopback origin. */
export function isObserverSandboxEnabled(
  environment: { readonly VITE_DB_NOTIFIER_OBSERVER_SANDBOX?: unknown },
  location: LocationBoundary,
): boolean {
  const loopback = ["localhost", "127.0.0.1", "[::1]"].includes(location.hostname);
  return environment.VITE_DB_NOTIFIER_OBSERVER_SANDBOX === "local-test" &&
    location.protocol === "https:" &&
    location.origin.startsWith("https://") &&
    loopback;
}

/** Reads one authenticated, bounded and same-origin projection without accepting a configurable endpoint. */
export async function readObserverProjection(
  signal: AbortSignal,
  fetchImplementation: typeof fetch = fetch,
): Promise<ObserverProjectionPackage> {
  const requestController = new AbortController();
  const abortFromCaller = () => requestController.abort();
  if (signal.aborted) requestController.abort();
  else signal.addEventListener("abort", abortFromCaller, { once: true });
  const timeout = globalThis.setTimeout(() => requestController.abort(), requestTimeoutMilliseconds);
  try {
    const response = await fetchImplementation.call(globalThis, projectionRoute, {
      method: "GET",
      credentials: "same-origin",
      cache: "no-store",
      redirect: "error",
      headers: new Headers({
        Accept: "application/json",
        [testSubjectHeader]: testSubject,
      }),
      signal: requestController.signal,
    });
    if (!response.ok || !isJsonContentType(response.headers.get("Content-Type"))) {
      throw new Error("observer.projection.unavailable");
    }
    const declaredLength = response.headers.get("Content-Length");
    if (declaredLength !== null &&
        (!/^\d+$/.test(declaredLength) || Number(declaredLength) > maximumBodyBytes)) {
      throw new Error("observer.projection.incompatible");
    }
    const body = await readBoundedBody(response, maximumBodyBytes);
    let value: unknown;
    try {
      value = JSON.parse(body);
    } catch {
      throw new Error("observer.projection.incompatible");
    }
    return validateObserverProjectionPackage(value, new Date());
  } finally {
    globalThis.clearTimeout(timeout);
    signal.removeEventListener("abort", abortFromCaller);
  }
}

/** Validates every field needed by the Observer UI and rejects unknown authority or incomplete content. */
export function validateObserverProjectionPackage(
  value: unknown,
  acceptedAt: Date,
): ObserverProjectionPackage {
  const root = record(value);
  const payload = record(root.payload);
  const trace = record(payload.trace);
  const forecasts = record(payload.forecasts);
  const generatedAt = utcDate(payload.generatedAtUtc);
  const validUntil = utcDate(payload.validUntilUtc);
  if (payload.schemaVersion !== observerProjectionSchemaVersion ||
      payload.processingCompleted !== true ||
      payload.synthetic !== true ||
      payload.operational !== false ||
      payload.productionRepresentative !== false ||
      payload.isAuthorising !== false ||
      payload.revoked !== false ||
      payload.superseded !== false ||
      payload.activationState !== "None" ||
      validUntil <= acceptedAt ||
      generatedAt > new Date(acceptedAt.getTime() + 60_000) ||
      validUntil.getTime() - generatedAt.getTime() > 5 * 60_000 ||
      !digest(root.projectionDigest)) {
    throw new Error("observer.projection.failed_closed");
  }
  const signals = array(payload.signals, 1, maximumSignals).map(signal);
  const limitations = textArray(payload.limitations, 1, maximumLimitations);
  const forecastLimitations = textArray(forecasts.limitations, 1, maximumLimitations);
  if (forecasts.state !== "Unknown" ||
      forecasts.code !== "o4.forecast.complete_result_unavailable" ||
      !limitations.every((value) => allowedProjectionLimitations.has(value)) ||
      !forecastLimitations.every((value) => allowedForecastLimitations.has(value))) {
    throw new Error("observer.projection.failed_closed");
  }
  return {
    projectionDigest: root.projectionDigest as string,
    payload: {
      schemaVersion: observerProjectionSchemaVersion,
      projectionId: text(payload.projectionId),
      generatedAtUtc: generatedAt.toISOString(),
      validUntilUtc: validUntil.toISOString(),
      processingCompleted: true,
      synthetic: true,
      operational: false,
      productionRepresentative: false,
      isAuthorising: false,
      revoked: false,
      superseded: false,
      activationState: "None",
      sourceLabel: text(payload.sourceLabel),
      trace: {
        o2EnvelopeDigest: digestText(trace.o2EnvelopeDigest),
        o3ResultDigest: digestText(trace.o3ResultDigest),
        policyDigest: digestText(trace.policyDigest),
        corpusManifestDigest: digestText(trace.corpusManifestDigest),
        corpusRevision: integer(trace.corpusRevision, 1, Number.MAX_SAFE_INTEGER),
        evaluationId: digestText(trace.evaluationId),
      },
      signals,
      forecasts: {
        state: "Unknown",
        code: text(forecasts.code),
        limitations: forecastLimitations,
      },
      limitations,
    },
  };
}

/** Converts one untrusted signal to a bounded immutable view model. */
function signal(value: unknown): ObserverSignal {
  const item = record(value);
  const disposition = item.disposition;
  const freshness = item.freshness;
  if (!["Detected", "NotDetected", "InsufficientEvidence"].includes(String(disposition)) ||
      !["Current", "Stale", "Unknown"].includes(String(freshness))) {
    throw new Error("observer.projection.failed_closed");
  }
  return {
    signalId: digestText(item.signalId),
    ruleId: text(item.ruleId),
    ruleVersion: text(item.ruleVersion),
    metricKey: text(item.metricKey),
    unit: text(item.unit),
    disposition: disposition as ObserverSignal["disposition"],
    severity: text(item.severity),
    evaluatedAtUtc: utcDate(item.evaluatedAtUtc).toISOString(),
    observedAtUtc: nullableUtc(item.observedAtUtc),
    validUntilUtc: nullableUtc(item.validUntilUtc),
    freshness: freshness as ObserverFreshness,
    observedValue: nullableFinite(item.observedValue),
    threshold: finite(item.threshold),
    evidenceIds: array(item.evidenceIds, 1, maximumEvidence).map(guid),
    limitations: textArray(item.limitations, 0, maximumLimitations),
  };
}

/** Reads a response body while cancelling before it can exceed the declared contract. */
async function readBoundedBody(response: Response, maximumBytes: number): Promise<string> {
  const reader = response.body?.getReader();
  if (!reader) throw new Error("observer.projection.incompatible");
  const chunks: Uint8Array[] = [];
  let total = 0;
  try {
    while (true) {
      const next = await reader.read();
      if (next.done) break;
      total += next.value.byteLength;
      if (total > maximumBytes) throw new Error("observer.projection.incompatible");
      chunks.push(next.value);
    }
  } finally {
    reader.releaseLock();
  }
  const bytes = new Uint8Array(total);
  let offset = 0;
  for (const chunk of chunks) {
    bytes.set(chunk, offset);
    offset += chunk.byteLength;
  }
  return new TextDecoder("utf-8", { fatal: true }).decode(bytes);
}

function record(value: unknown): Record<string, unknown> {
  if (value === null || typeof value !== "object" || Array.isArray(value)) {
    throw new Error("observer.projection.failed_closed");
  }
  return value as Record<string, unknown>;
}

function array(value: unknown, minimum: number, maximum: number): readonly unknown[] {
  if (!Array.isArray(value) || value.length < minimum || value.length > maximum) {
    throw new Error("observer.projection.failed_closed");
  }
  return value;
}

function text(value: unknown): string {
  if (typeof value !== "string" ||
      value.length < 1 ||
      value.length > maximumTextLength ||
      value.trim() !== value ||
      /[\u0000-\u001F\u007F\u202A-\u202E\u2066-\u2069]/u.test(value)) {
    throw new Error("observer.projection.failed_closed");
  }
  return value;
}

function textArray(value: unknown, minimum: number, maximum: number): readonly string[] {
  return array(value, minimum, maximum).map(text);
}

function digest(value: unknown): value is string {
  return typeof value === "string" && /^[a-fA-F0-9]{64}$/u.test(value);
}

function digestText(value: unknown): string {
  if (!digest(value)) throw new Error("observer.projection.failed_closed");
  return value;
}

function guid(value: unknown): string {
  if (typeof value !== "string" ||
      !/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/iu.test(value)) {
    throw new Error("observer.projection.failed_closed");
  }
  return value;
}

function utcDate(value: unknown): Date {
  if (typeof value !== "string" || !/(?:Z|\+00:00)$/u.test(value)) {
    throw new Error("observer.projection.failed_closed");
  }
  const parsed = new Date(value);
  if (!Number.isFinite(parsed.getTime())) throw new Error("observer.projection.failed_closed");
  return parsed;
}

function nullableUtc(value: unknown): string | null {
  return value === null ? null : utcDate(value).toISOString();
}

function finite(value: unknown): number {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    throw new Error("observer.projection.failed_closed");
  }
  return value;
}

function nullableFinite(value: unknown): number | null {
  return value === null ? null : finite(value);
}

function integer(value: unknown, minimum: number, maximum: number): number {
  if (typeof value !== "number" || !Number.isSafeInteger(value) || value < minimum || value > maximum) {
    throw new Error("observer.projection.failed_closed");
  }
  return value;
}

function isJsonContentType(value: string | null): boolean {
  return value?.split(";", 1)[0]?.trim().toLowerCase() === "application/json";
}
