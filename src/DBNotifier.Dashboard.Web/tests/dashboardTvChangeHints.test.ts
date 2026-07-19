/** Module purpose: Verifies the exact Dashboard TV hint contract and bounded SignalR reconnect policy without opening a runtime. */
import assert from "node:assert/strict";
import test from "node:test";
import {
  DashboardTvChangeHintRetryPolicy,
  dashboardTvChangeHintMaximumReconnectAttempts,
  dashboardTvChangeHintMaximumReconnectElapsedMilliseconds,
  isDashboardTvChangeHint,
} from "../src/dashboardTvChangeHints.ts";

const revision = `sha256-${"a".repeat(64)}`;

test("change hint accepts only the exact versioned opaque revision contract", () => {
  assert.equal(isDashboardTvChangeHint({
    schemaVersion: "dashboard-tv-change-hint.v1",
    projectionRevision: revision,
  }), true);
  assert.equal(isDashboardTvChangeHint({
    schemaVersion: "dashboard-tv-change-hint.v1",
    projectionRevision: revision,
    payload: {},
  }), false);
  assert.equal(isDashboardTvChangeHint({
    schemaVersion: "dashboard-tv-change-hint.v2",
    projectionRevision: revision,
  }), false);
  assert.equal(isDashboardTvChangeHint({
    schemaVersion: "dashboard-tv-change-hint.v1",
    projectionRevision: `sha256-${"A".repeat(64)}`,
  }), false);
});

test("reconnect policy stops at both the attempt and elapsed-time budgets", () => {
  const policy = new DashboardTvChangeHintRetryPolicy();
  const failure = new Error("synthetic local disconnect");
  const next = (previousRetryCount: number, elapsedMilliseconds: number) =>
    policy.nextRetryDelayInMilliseconds({ previousRetryCount, elapsedMilliseconds, retryReason: failure });

  assert.deepEqual([next(0, 0), next(1, 1), next(2, 1_001), next(3, 3_001)], [0, 1_000, 2_000, 5_000]);
  assert.equal(next(dashboardTvChangeHintMaximumReconnectAttempts, 8_001), null);
  assert.equal(next(1, dashboardTvChangeHintMaximumReconnectElapsedMilliseconds), null);
});
