/** Module purpose: Implements the bounded local-sandbox adapter and serial Dashboard TV snapshot reconciliation lifecycle. */
import { inventorySchemaVersion, type HealthStatus, type InventorySnapshot } from "./presentation.ts";

export const dashboardTvSchemaVersion = "dashboard-tv.v1" as const;
export const dashboardTvReconciliationMilliseconds = 30_000;
const maximumBodyBytes = 512 * 1024;
const maximumItems = 500;
const maximumTextLength = 200;
const maximumLatencyMilliseconds = 3_600_000;
const snapshotRoute = "/api/v1/dashboard/tv-snapshot";
const testSubjectHeader = "X-DBN-TV-Test-Human";
const testSubject = "dashboard-tv-local-test";
const allowedStatuses: readonly HealthStatus[] = [
  "healthy", "degraded", "unavailable", "authFailed", "timeout", "maintenance", "unknown",
];

export type DashboardTvConnectionState = "loading" | "ready" | "offline" | "error" | "denied" | "incompatible";

export interface DashboardTvReconciliationView {
  state: DashboardTvConnectionState;
  snapshot?: InventorySnapshot;
  entityTag?: string;
  lastSuccessfulAt?: Date;
}

export interface DashboardTvReadResult {
  disposition: "modified" | "notModified";
  snapshot?: InventorySnapshot;
  entityTag: string;
}

export interface DashboardTvSnapshotReader {
  read(entityTag: string | undefined, signal: AbortSignal): Promise<DashboardTvReadResult>;
}

export interface DashboardTvTimer {
  schedule(callback: () => void, delayMilliseconds: number): number;
  cancel(handle: number): void;
}

type LocationBoundary = Pick<Location, "protocol" | "hostname" | "origin">;

/** Classifies one adapter failure without exposing raw transport or response details to the interface. */
export class DashboardTvReadError extends Error {
  public readonly state: Exclude<DashboardTvConnectionState, "loading" | "ready">;

  /** Creates one bounded semantic failure for the reconciliation coordinator. */
  public constructor(state: Exclude<DashboardTvConnectionState, "loading" | "ready">) {
    super(`Dashboard TV snapshot read failed: ${state}`);
    this.name = "DashboardTvReadError";
    this.state = state;
  }
}

/**
 * Returns whether the Dashboard adapter may exist for this exact browser session.
 * @param environment - Build-time values supplied by Vite without a runtime token or external endpoint.
 * @param location - Current browser origin, which must be HTTPS loopback.
 * @returns True only for the exact local-test flag on an HTTPS loopback origin.
 */
export function isDashboardTvSandboxEnabled(
  environment: { readonly VITE_DB_NOTIFIER_TV_SANDBOX?: unknown },
  location: LocationBoundary,
): boolean {
  const loopback = location.hostname === "localhost" ||
    location.hostname === "127.0.0.1" ||
    location.hostname === "[::1]";
  return environment.VITE_DB_NOTIFIER_TV_SANDBOX === "local-test" &&
    location.protocol === "https:" &&
    loopback &&
    location.origin.startsWith("https://");
}

/** Reads and validates the fixed same-origin snapshot route without accepting a configurable endpoint or credential. */
export class DashboardTvHttpSnapshotReader implements DashboardTvSnapshotReader {
  private readonly fetchImplementation: typeof fetch;

  /** Creates a same-origin reader after the caller has passed the exact sandbox activation guard. */
  public constructor(fetchImplementation: typeof fetch = fetch) {
    this.fetchImplementation = fetchImplementation;
  }

  /**
   * Reads one bounded snapshot or a factual 304 response.
   * @param entityTag - Previously accepted strong ETag, when available.
   * @param signal - Session cancellation signal propagated to Fetch.
   * @returns A validated snapshot result that contains no provider-native data.
   * @throws DashboardTvReadError when authentication, compatibility, transport or validation fails.
   */
  public async read(entityTag: string | undefined, signal: AbortSignal): Promise<DashboardTvReadResult> {
    const headers = new Headers({ Accept: "application/json", [testSubjectHeader]: testSubject });
    if (entityTag) headers.set("If-None-Match", entityTag);

    let response: Response;
    try {
      response = await this.fetchImplementation(snapshotRoute, {
        method: "GET",
        credentials: "same-origin",
        cache: "no-store",
        redirect: "error",
        headers,
        signal,
      });
    } catch (error) {
      if (signal.aborted) throw error;
      throw new DashboardTvReadError("offline");
    }

    if (response.status === 401 || response.status === 403) throw new DashboardTvReadError("denied");
    if (response.status === 304) {
      const returnedEntityTag = requireStrongEntityTag(response.headers.get("ETag"));
      if (!entityTag || returnedEntityTag !== entityTag) throw new DashboardTvReadError("incompatible");
      return { disposition: "notModified", entityTag: returnedEntityTag };
    }
    if (!response.ok) throw new DashboardTvReadError(response.status === 406 || response.status === 426 ? "incompatible" : "error");
    if (response.headers.get("DBN-Snapshot-Schema") !== dashboardTvSchemaVersion) {
      throw new DashboardTvReadError("incompatible");
    }

    const returnedEntityTag = requireStrongEntityTag(response.headers.get("ETag"));
    const contentLength = Number(response.headers.get("Content-Length"));
    if (Number.isFinite(contentLength) && contentLength > maximumBodyBytes) throw new DashboardTvReadError("incompatible");
    const body = await response.text();
    if (new TextEncoder().encode(body).byteLength > maximumBodyBytes) throw new DashboardTvReadError("incompatible");

    let value: unknown;
    try {
      value = JSON.parse(body);
    } catch {
      throw new DashboardTvReadError("incompatible");
    }
    return {
      disposition: "modified",
      entityTag: returnedEntityTag,
      snapshot: validateDashboardTvSnapshot(value, new Date()),
    };
  }
}

/**
 * Owns one TV session, ensuring immediate then serial 30-second reads with cancellation and stale-session rejection.
 * A failed read never discards a previously accepted immutable snapshot.
 */
export class DashboardTvReconciliationCoordinator {
  private generation = 0;
  private active = false;
  private inFlight = false;
  private timerHandle: number | undefined;
  private abortController: AbortController | undefined;
  private current: DashboardTvReconciliationView = { state: "loading" };
  private readonly reader: DashboardTvSnapshotReader;
  private readonly publish: (view: DashboardTvReconciliationView) => void;
  private readonly timer: DashboardTvTimer;
  private readonly now: () => Date;

  /** Creates a coordinator with injectable time and scheduling boundaries for deterministic tests. */
  public constructor(
    reader: DashboardTvSnapshotReader,
    publish: (view: DashboardTvReconciliationView) => void,
    timer: DashboardTvTimer = browserTimer,
    now: () => Date = () => new Date(),
  ) {
    this.reader = reader;
    this.publish = publish;
    this.timer = timer;
    this.now = now;
  }

  /** Starts a new session and performs its first read immediately. */
  public start(): void {
    this.stop();
    this.active = true;
    this.generation += 1;
    this.current = { state: "loading" };
    this.publish(this.current);
    void this.reconcile(this.generation);
  }

  /** Cancels the active request and timer without publishing any late completion. */
  public stop(): void {
    this.active = false;
    this.generation += 1;
    this.abortController?.abort();
    this.abortController = undefined;
    if (this.timerHandle !== undefined) this.timer.cancel(this.timerHandle);
    this.timerHandle = undefined;
    this.inFlight = false;
  }

  /** Requests an immediate retry while coalescing with any already active read. */
  public retry(): void {
    if (!this.active || this.inFlight) return;
    if (this.timerHandle !== undefined) this.timer.cancel(this.timerHandle);
    this.timerHandle = undefined;
    void this.reconcile(this.generation);
  }

  private async reconcile(generation: number): Promise<void> {
    if (!this.active || this.inFlight || generation !== this.generation) return;
    this.inFlight = true;
    const controller = new AbortController();
    this.abortController = controller;
    try {
      const result = await this.reader.read(this.current.entityTag, controller.signal);
      if (!this.active || generation !== this.generation || controller.signal.aborted) return;
      if (result.disposition === "notModified" && !this.current.snapshot) {
        throw new DashboardTvReadError("incompatible");
      }
      this.current = {
        state: "ready",
        snapshot: result.snapshot ?? this.current.snapshot,
        entityTag: result.entityTag,
        lastSuccessfulAt: result.disposition === "modified" ? this.now() : this.current.lastSuccessfulAt,
      };
      this.publish(this.current);
    } catch (error) {
      if (!this.active || generation !== this.generation || controller.signal.aborted) return;
      this.current = {
        ...this.current,
        state: error instanceof DashboardTvReadError ? error.state : "error",
      };
      this.publish(this.current);
    } finally {
      if (generation === this.generation) {
        this.inFlight = false;
        this.abortController = undefined;
        if (this.active) {
          this.timerHandle = this.timer.schedule(
            () => {
              this.timerHandle = undefined;
              void this.reconcile(generation);
            },
            dashboardTvReconciliationMilliseconds,
          );
        }
      }
    }
  }
}

const browserTimer: DashboardTvTimer = {
  schedule: (callback, delayMilliseconds) => window.setTimeout(callback, delayMilliseconds),
  cancel: (handle) => window.clearTimeout(handle),
};

function requireStrongEntityTag(value: string | null): string {
  if (!value || !/^"sha256-[0-9a-f]{64}"$/.test(value)) throw new DashboardTvReadError("incompatible");
  return value;
}

function validateDashboardTvSnapshot(value: unknown, now: Date): InventorySnapshot {
  if (!isRecord(value) || !hasExactKeys(value, ["schemaVersion", "generatedAt", "items"]) ||
      value.schemaVersion !== dashboardTvSchemaVersion || !Array.isArray(value.items) || value.items.length > maximumItems) {
    throw new DashboardTvReadError("incompatible");
  }
  const generatedAt = parseUtcInstant(value.generatedAt, now.getTime());
  const identifiers = new Set<string>();
  const items = value.items.map((candidate) => {
    if (!isRecord(candidate) || !hasExactKeys(candidate, [
      "instanceId", "displayName", "providerType", "supportLabel", "environment", "locationLabel",
      "status", "observedAt", "receivedAt", "latencyMilliseconds", "enabled",
    ])) throw new DashboardTvReadError("incompatible");
    const latency = candidate.latencyMilliseconds;
    if (typeof candidate.instanceId !== "string" || !isUuid(candidate.instanceId) || identifiers.has(candidate.instanceId) ||
        !isBoundedText(candidate.displayName) || !isBoundedText(candidate.providerType) ||
        !isBoundedText(candidate.supportLabel) || !isBoundedText(candidate.environment) ||
        !isBoundedText(candidate.locationLabel) || !allowedStatuses.includes(candidate.status as HealthStatus) ||
        typeof candidate.enabled !== "boolean" ||
        !(latency === null ||
          (typeof latency === "number" && Number.isInteger(latency) && latency >= 0 && latency <= maximumLatencyMilliseconds))) {
      throw new DashboardTvReadError("incompatible");
    }
    identifiers.add(candidate.instanceId);
    const observedAt = parseUtcInstant(candidate.observedAt, generatedAt);
    const receivedAt = parseUtcInstant(candidate.receivedAt, generatedAt);
    if (observedAt > receivedAt) throw new DashboardTvReadError("incompatible");
    return {
      instanceId: candidate.instanceId,
      displayName: candidate.displayName,
      providerType: candidate.providerType,
      supportLabel: candidate.supportLabel,
      environment: candidate.environment,
      locationLabel: candidate.locationLabel,
      status: candidate.status as HealthStatus,
      observedAt: candidate.observedAt as string,
      receivedAt: candidate.receivedAt as string,
      latencyMilliseconds: candidate.latencyMilliseconds as number | null,
      enabled: candidate.enabled,
    };
  });
  return { schemaVersion: inventorySchemaVersion, generatedAt: value.generatedAt as string, items };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function hasExactKeys(value: Record<string, unknown>, expected: readonly string[]): boolean {
  const keys = Object.keys(value).sort();
  return keys.length === expected.length && [...expected].sort().every((key, index) => key === keys[index]);
}

function isBoundedText(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0 && value.length <= maximumTextLength;
}

function isUuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}

function parseUtcInstant(value: unknown, maximumMilliseconds: number): number {
  if (typeof value !== "string" || !/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/.test(value)) {
    throw new DashboardTvReadError("incompatible");
  }
  const parsed = Date.parse(value);
  if (!Number.isFinite(parsed) || parsed > maximumMilliseconds) throw new DashboardTvReadError("incompatible");
  return parsed;
}
