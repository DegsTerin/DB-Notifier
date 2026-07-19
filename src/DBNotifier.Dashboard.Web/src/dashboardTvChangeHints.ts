/** Module purpose: Adapts authenticated best-effort SignalR hints to bounded authoritative Dashboard TV re-reads in the exact local sandbox. */
import {
  HubConnectionBuilder,
  HttpTransportType,
  LogLevel,
  type HubConnection,
  type IRetryPolicy,
  type RetryContext,
} from "@microsoft/signalr";

export const dashboardTvChangeHintSchemaVersion = "dashboard-tv-change-hint.v1" as const;
export const dashboardTvChangeHintMaximumReconnectAttempts = 4;
export const dashboardTvChangeHintMaximumReconnectElapsedMilliseconds = 15_000;
const hubRoute = "/api/v1/dashboard/tv-change-hints";
const sessionRoute = "/api/v1/dashboard/tv-change-hints/session";
const clientMethod = "DashboardTvChanged";
const testSubjectHeader = "X-DBN-TV-Test-Human";
const testSubject = "dashboard-tv-local-test";
const reconnectDelaysMilliseconds = [0, 1_000, 2_000, 5_000] as const;

export interface DashboardTvChangeHint {
  schemaVersion: typeof dashboardTvChangeHintSchemaVersion;
  projectionRevision: string;
}

/** Supplies bounded retry delays and terminates after either the attempt or elapsed-time budget is exhausted. */
export class DashboardTvChangeHintRetryPolicy implements IRetryPolicy {
  /** Returns the next bounded delay, or null when reconnection must stop and polling must continue alone. */
  public nextRetryDelayInMilliseconds(retryContext: RetryContext): number | null {
    if (retryContext.previousRetryCount >= dashboardTvChangeHintMaximumReconnectAttempts ||
        retryContext.elapsedMilliseconds >= dashboardTvChangeHintMaximumReconnectElapsedMilliseconds) {
      return null;
    }
    return reconnectDelaysMilliseconds[retryContext.previousRetryCount] ?? null;
  }
}

/** Owns one ephemeral authenticated hint session without treating SignalR as an authoritative data source. */
export class DashboardTvSignalRChangeHintSource {
  private connection: HubConnection | undefined;
  private sessionIssued = false;
  private activeGeneration = 0;
  private readonly fetchImplementation: typeof fetch;
  private readonly buildConnection: () => HubConnection;

  /** Creates one same-origin source with injectable transport boundaries for deterministic tests. */
  public constructor(
    fetchImplementation: typeof fetch = fetch,
    buildConnection: () => HubConnection = createConnection,
  ) {
    this.fetchImplementation = fetchImplementation;
    this.buildConnection = buildConnection;
  }

  /** Creates the host-only session and starts the bounded hub connection for one TV generation. */
  public async start(onHint: () => void, onIncompatible: () => void, signal: AbortSignal): Promise<void> {
    const generation = ++this.activeGeneration;
    const abort = () => { void this.stop(); };
    if (signal.aborted) return;
    signal.addEventListener("abort", abort, { once: true });
    try {
      const response = await this.fetchImplementation.call(globalThis, sessionRoute, {
        method: "POST",
        credentials: "same-origin",
        cache: "no-store",
        redirect: "error",
        headers: { [testSubjectHeader]: testSubject },
        signal,
      });
      if (!response.ok || response.status !== 204 ||
          response.headers.get("DBN-Change-Hint-Schema") !== dashboardTvChangeHintSchemaVersion) {
        onIncompatible();
        return;
      }
      this.sessionIssued = true;
      if (signal.aborted || generation !== this.activeGeneration) return;

      const connection = this.buildConnection();
      this.connection = connection;
      connection.on(clientMethod, (candidate: unknown) => {
        if (generation !== this.activeGeneration) return;
        if (!isDashboardTvChangeHint(candidate)) {
          onIncompatible();
          void this.stop();
          return;
        }
        onHint();
      });
      await connection.start();
    } catch {
      // Hint transport failure is deliberately silent because periodic HTTPS reconciliation remains authoritative.
      await this.stop();
    }
  }

  /** Stops the hub, revokes its ephemeral cookie and fences every late callback from the old generation. */
  public async stop(): Promise<void> {
    this.activeGeneration += 1;
    const connection = this.connection;
    this.connection = undefined;
    const revokeSession = this.sessionIssued;
    this.sessionIssued = false;
    if (connection) {
      connection.off(clientMethod);
      try { await connection.stop(); } catch { /* Polling remains authoritative after bounded cleanup failure. */ }
    }
    if (!revokeSession) return;
    try {
      await this.fetchImplementation.call(globalThis, sessionRoute, {
        method: "DELETE",
        credentials: "same-origin",
        cache: "no-store",
        redirect: "error",
      });
    } catch {
      // The server-side proof is short-lived and process-local when best-effort browser cleanup cannot complete.
    }
  }
}

/** Validates the exact minimal versioned hint contract and rejects surplus or ambiguous fields. */
export function isDashboardTvChangeHint(value: unknown): value is DashboardTvChangeHint {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
  const record = value as Record<string, unknown>;
  const keys = Object.keys(record).sort();
  return keys.length === 2 && keys[0] === "projectionRevision" && keys[1] === "schemaVersion" &&
    record.schemaVersion === dashboardTvChangeHintSchemaVersion &&
    typeof record.projectionRevision === "string" && /^sha256-[0-9a-f]{64}$/.test(record.projectionRevision);
}

/** Builds the official Microsoft client with same-origin credentials and bounded automatic reconnection only. */
function createConnection(): HubConnection {
  const connection = new HubConnectionBuilder()
    .withUrl(hubRoute, {
      transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
      withCredentials: true,
    })
    .configureLogging(LogLevel.None)
    .withAutomaticReconnect(new DashboardTvChangeHintRetryPolicy())
    .build();
  connection.serverTimeoutInMilliseconds = 30_000;
  connection.keepAliveIntervalInMilliseconds = 10_000;
  return connection;
}
