import assert from "node:assert/strict";
import test from "node:test";
import {
  buildDemonstrationSnapshot,
  filterInventory,
  isStale,
  staleAfterMilliseconds,
  summarizeInventory,
} from "../src/presentation.ts";

const now = new Date("2026-07-12T15:00:00.000Z");

test("summary does not report stale data as freshly healthy", () => {
  const snapshot = buildDemonstrationSnapshot(now);
  const summary = summarizeInventory(snapshot, now);

  assert.deepEqual(summary, { total: 4, healthy: 1, degraded: 1, attentionRequired: 1, stale: 1 });
});

test("stale policy changes only after the five-minute boundary", () => {
  const item = {
    ...buildDemonstrationSnapshot(now).items[0],
    receivedAt: new Date(now.getTime() - staleAfterMilliseconds).toISOString(),
  };

  assert.equal(isStale(item, now), false);
  assert.equal(isStale(item, new Date(now.getTime() + 1)), true);
});

test("filter searches provider-neutral fields and stale state", () => {
  const snapshot = buildDemonstrationSnapshot(now);

  assert.equal(filterInventory(snapshot.items, "azure", "all", now).length, 1);
  assert.equal(filterInventory(snapshot.items, "", "stale", now).length, 1);
  assert.equal(filterInventory(snapshot.items, "mysql", "healthy", now).length, 0);
});
