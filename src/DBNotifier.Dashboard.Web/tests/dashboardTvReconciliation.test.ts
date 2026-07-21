/** Module purpose: Verifies bounded Dashboard TV sandbox reads, activation guards and serial reconciliation behaviour. */
import assert from "node:assert/strict";
import test from "node:test";
import {
  DashboardTvHttpSnapshotReader,
  DashboardTvReadError,
  DashboardTvReconciliationCoordinator,
  dashboardTvMaximumHintsPerPeriodicWindow,
  dashboardTvReconciliationMilliseconds,
  dashboardTvRequestTimeoutMilliseconds,
  isDashboardTvSandboxEnabled,
  type DashboardTvReadResult,
  type DashboardTvReconciliationView,
  type DashboardTvSnapshotReader,
  type DashboardTvTimer,
} from "../src/dashboardTvReconciliation.ts";

const entityTag = `"sha256-${"a".repeat(64)}"`;
const snapshotBody = {
  schemaVersion: "dashboard-tv.v1",
  generatedAt: "2020-07-18T12:00:00.000Z",
  items: [{
    instanceId: "88d74662-18a2-4f08-bc7a-7d61e68a3137",
    displayName: "Finance sandbox",
    providerType: "postgresql",
    supportLabel: "Fixture",
    environment: "Sandbox",
    locationLabel: "Local process",
    status: "healthy" as const,
    observedAt: "2020-07-18T11:59:58.000Z",
    receivedAt: "2020-07-18T11:59:59.000Z",
    latencyMilliseconds: 24,
    enabled: true,
  }],
};

test("sandbox activation requires the exact flag and an HTTPS loopback origin", () => {
  assert.equal(isDashboardTvSandboxEnabled(
    { VITE_DB_NOTIFIER_TV_SANDBOX: "local-test" },
    { protocol: "https:", hostname: "localhost", origin: "https://localhost:4173" },
  ), true);
  assert.equal(isDashboardTvSandboxEnabled(
    { VITE_DB_NOTIFIER_TV_SANDBOX: "local-test" },
    { protocol: "http:", hostname: "localhost", origin: "http://localhost:4173" },
  ), false);
  assert.equal(isDashboardTvSandboxEnabled(
    { VITE_DB_NOTIFIER_TV_SANDBOX: "local-test" },
    { protocol: "https:", hostname: "dashboard.example", origin: "https://dashboard.example" },
  ), false);
  assert.equal(isDashboardTvSandboxEnabled(
    {},
    { protocol: "https:", hostname: "localhost", origin: "https://localhost:4173" },
  ), false);
});

test("HTTP reader accepts one bounded snapshot and sends its ETag on reconciliation", async () => {
  const requests: RequestInit[] = [];
  const reader = new DashboardTvHttpSnapshotReader(async (_input, init) => {
    requests.push(init ?? {});
    return new Response(JSON.stringify(snapshotBody), {
      status: 200,
      headers: { ETag: entityTag, "DBN-Snapshot-Schema": "dashboard-tv.v1", "Content-Type": "application/json; charset=utf-8" },
    });
  });

  const result = await reader.read(entityTag, new AbortController().signal);

  assert.equal(result.disposition, "modified");
  assert.equal(result.snapshot?.schemaVersion, "inventory.v1");
  assert.equal(result.snapshot?.items[0].displayName, "Finance sandbox");
  assert.equal(new Headers(requests[0].headers).get("If-None-Match"), entityTag);
  assert.equal(new Headers(requests[0].headers).get("X-DBN-TV-Test-Human"), "dashboard-tv-local-test");
});

test("HTTP reader preserves the browser-global receiver for a captured Fetch operation", async () => {
  let observedReceiver: unknown;
  const receiverAwareFetch = async function (this: unknown): Promise<Response> {
    observedReceiver = this;
    return new Response(JSON.stringify(snapshotBody), {
      status: 200,
      headers: { ETag: entityTag, "DBN-Snapshot-Schema": "dashboard-tv.v1", "Content-Type": "application/json" },
    });
  };
  const reader = new DashboardTvHttpSnapshotReader(receiverAwareFetch as typeof fetch);

  await reader.read(undefined, new AbortController().signal);

  assert.equal(observedReceiver, globalThis);
});

test("HTTP reader rejects unknown fields, future evidence and weak ETags", async () => {
  const incompatible = { ...snapshotBody, unexpected: true };
  const reader = new DashboardTvHttpSnapshotReader(async () => new Response(JSON.stringify(incompatible), {
    status: 200,
    headers: { ETag: `W/${entityTag}`, "DBN-Snapshot-Schema": "dashboard-tv.v1", "Content-Type": "application/json" },
  }));

  await assert.rejects(
    reader.read(undefined, new AbortController().signal),
    (error: unknown) => error instanceof DashboardTvReadError && error.state === "incompatible",
  );
});

test("HTTP reader classifies denied, incompatible and oversized responses without parsing them", async () => {
  for (const [status, expected] of [[403, "denied"], [426, "incompatible"]] as const) {
    const reader = new DashboardTvHttpSnapshotReader(async () => new Response(null, { status }));
    await assert.rejects(
      reader.read(undefined, new AbortController().signal),
      (error: unknown) => error instanceof DashboardTvReadError && error.state === expected,
    );
  }

  const oversized = new DashboardTvHttpSnapshotReader(async () => new Response("{}", {
    status: 200,
    headers: {
      ETag: entityTag,
      "DBN-Snapshot-Schema": "dashboard-tv.v1",
      "Content-Type": "application/json",
      "Content-Length": String(512 * 1024 + 1),
    },
  }));
  await assert.rejects(
    oversized.read(undefined, new AbortController().signal),
    (error: unknown) => error instanceof DashboardTvReadError && error.state === "incompatible",
  );
});

test("HTTP reader accepts exactly N streamed bytes and refuses N plus one without Content-Length", async () => {
  const encoded = new TextEncoder().encode(JSON.stringify(snapshotBody));
  const exactBody = new Uint8Array(512 * 1024);
  exactBody.set(encoded);
  exactBody.fill(0x20, encoded.length);
  const createResponse = (body: Uint8Array) => new Response(new ReadableStream<Uint8Array>({
    start(controller) {
      controller.enqueue(body.subarray(0, 17));
      controller.enqueue(body.subarray(17));
      controller.close();
    },
  }), {
    status: 200,
    headers: {
      ETag: entityTag,
      "DBN-Snapshot-Schema": "dashboard-tv.v1",
      "Content-Type": "application/json",
    },
  });

  const exact = new DashboardTvHttpSnapshotReader(async () => createResponse(exactBody));
  assert.equal((await exact.read(undefined, new AbortController().signal)).disposition, "modified");

  const oversizedBody = new Uint8Array(exactBody.length + 1);
  oversizedBody.set(exactBody);
  oversizedBody[oversizedBody.length - 1] = 0x20;
  const oversized = new DashboardTvHttpSnapshotReader(async () => createResponse(oversizedBody));
  await assert.rejects(
    oversized.read(undefined, new AbortController().signal),
    (error: unknown) => error instanceof DashboardTvReadError && error.state === "incompatible",
  );
});

test("HTTP reader bounds a stalled response without weakening caller cancellation", async () => {
  let observedSignal: AbortSignal | undefined;
  const reader = new DashboardTvHttpSnapshotReader(
    async (_input, init) => new Promise<Response>((_resolve, reject) => {
      observedSignal = init?.signal as AbortSignal;
      observedSignal.addEventListener(
        "abort",
        () => reject(new DOMException("The local sandbox request was aborted.", "AbortError")),
        { once: true },
      );
    }),
    5,
  );

  await assert.rejects(
    reader.read(undefined, new AbortController().signal),
    (error: unknown) => error instanceof DashboardTvReadError && error.state === "offline",
  );
  assert.equal(observedSignal?.aborted, true);
  assert.equal(dashboardTvRequestTimeoutMilliseconds, 10_000);
});

test("coordinator reads immediately, never overlaps and preserves evidence across 304", async () => {
  const first = deferred<DashboardTvReadResult>();
  const second = deferred<DashboardTvReadResult>();
  const reader = new QueueReader([first.promise, second.promise]);
  const timer = new ManualTimer();
  const views: DashboardTvReconciliationView[] = [];
  const acceptedAt = new Date("2026-07-18T12:00:01.000Z");
  const coordinator = new DashboardTvReconciliationCoordinator(reader, (view) => views.push(view), timer, () => acceptedAt);

  coordinator.start();
  assert.equal(reader.callCount, 1);
  assert.equal(timer.pendingCount, 0);
  coordinator.retry();
  assert.equal(reader.callCount, 1);

  first.resolve({ disposition: "modified", snapshot: inventorySnapshot(), entityTag });
  await settle();
  assert.equal(timer.lastDelay, dashboardTvReconciliationMilliseconds);
  assert.equal(timer.pendingCount, 1);
  timer.runNext();
  assert.equal(reader.callCount, 2);
  coordinator.retry();
  assert.equal(reader.callCount, 2);

  second.resolve({ disposition: "notModified", entityTag });
  await settle();
  assert.equal(views.at(-1)?.snapshot?.generatedAt, "2020-07-18T12:00:00.000Z");
  assert.equal(views.at(-1)?.lastSuccessfulAt, acceptedAt);
  coordinator.stop();
});

test("coordinator bounds and coalesces hints without postponing its periodic deadline", async () => {
  const initial = deferred<DashboardTvReadResult>();
  const firstHint = deferred<DashboardTvReadResult>();
  const coalescedHint = deferred<DashboardTvReadResult>();
  const periodic = deferred<DashboardTvReadResult>();
  const reader = new QueueReader([initial.promise, firstHint.promise, coalescedHint.promise, periodic.promise]);
  const timer = new ManualTimer();
  const coordinator = new DashboardTvReconciliationCoordinator(reader, () => {}, timer);

  coordinator.start();
  initial.resolve({ disposition: "modified", snapshot: inventorySnapshot(), entityTag });
  await settle();
  assert.equal(timer.pendingCount, 1);

  coordinator.hint();
  coordinator.hint();
  coordinator.hint();
  assert.equal(reader.callCount, 2);
  assert.equal(timer.pendingCount, 1);
  timer.runNext();
  firstHint.resolve({ disposition: "notModified", entityTag });
  await settle();
  assert.equal(reader.callCount, 3);

  coalescedHint.resolve({ disposition: "notModified", entityTag });
  await settle();
  assert.equal(reader.callCount, 4);
  periodic.resolve({ disposition: "notModified", entityTag });
  await settle();

  assert.equal(timer.pendingCount, 1);
  assert.equal(dashboardTvMaximumHintsPerPeriodicWindow, 2);
  coordinator.stop();
});

test("coordinator preserves the last valid snapshot after an offline failure", async () => {
  const failure = deferred<DashboardTvReadResult>();
  const reader = new QueueReader([
    Promise.resolve({ disposition: "modified", snapshot: inventorySnapshot(), entityTag }),
    failure.promise,
  ]);
  const timer = new ManualTimer();
  const views: DashboardTvReconciliationView[] = [];
  const coordinator = new DashboardTvReconciliationCoordinator(reader, (view) => views.push(view), timer);

  coordinator.start();
  await settle();
  timer.runNext();
  failure.reject(new DashboardTvReadError("offline"));
  await settle();

  assert.equal(views.at(-1)?.state, "offline");
  assert.equal(views.at(-1)?.snapshot?.items.length, 1);
  coordinator.stop();
});

test("stopping a TV session aborts and ignores its late completion", async () => {
  const pending = deferred<DashboardTvReadResult>();
  const reader = new QueueReader([pending.promise]);
  const views: DashboardTvReconciliationView[] = [];
  const coordinator = new DashboardTvReconciliationCoordinator(reader, (view) => views.push(view), new ManualTimer());

  coordinator.start();
  coordinator.stop();
  pending.resolve({ disposition: "modified", snapshot: inventorySnapshot(), entityTag });
  await settle();

  assert.equal(reader.lastSignal?.aborted, true);
  assert.deepEqual(views.map((view) => view.state), ["loading"]);
});

function inventorySnapshot() {
  return {
    schemaVersion: "inventory.v1" as const,
    generatedAt: snapshotBody.generatedAt,
    items: snapshotBody.items,
  };
}

class QueueReader implements DashboardTvSnapshotReader {
  public callCount = 0;
  public lastSignal: AbortSignal | undefined;
  private readonly results: Array<Promise<DashboardTvReadResult>>;

  public constructor(results: Array<Promise<DashboardTvReadResult>>) {
    this.results = results;
  }

  public read(_entityTag: string | undefined, signal: AbortSignal): Promise<DashboardTvReadResult> {
    this.lastSignal = signal;
    const result = this.results[this.callCount];
    this.callCount += 1;
    if (!result) throw new Error("No queued read result.");
    return result;
  }
}

class ManualTimer implements DashboardTvTimer {
  private nextHandle = 1;
  private readonly callbacks = new Map<number, () => void>();
  public lastDelay: number | undefined;

  public get pendingCount(): number { return this.callbacks.size; }

  public schedule(callback: () => void, delayMilliseconds: number): number {
    const handle = this.nextHandle;
    this.nextHandle += 1;
    this.lastDelay = delayMilliseconds;
    this.callbacks.set(handle, callback);
    return handle;
  }

  public cancel(handle: number): void { this.callbacks.delete(handle); }

  public runNext(): void {
    const entry = this.callbacks.entries().next().value as [number, () => void] | undefined;
    assert.ok(entry);
    this.callbacks.delete(entry[0]);
    entry[1]();
  }
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (reason?: unknown) => void;
  const promise = new Promise<T>((resolvePromise, rejectPromise) => {
    resolve = resolvePromise;
    reject = rejectPromise;
  });
  return { promise, resolve, reject };
}

async function settle(): Promise<void> {
  await Promise.resolve();
  await new Promise((resolve) => setImmediate(resolve));
}
