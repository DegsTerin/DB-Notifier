import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { contrastRatio, dashboardTextContrastPairs } from "../src/accessibility.ts";
import {
  buildDemonstrationSnapshot,
  buildTimelineAlertSnapshot,
  buildConfigurationSnapshot,
  filterInventory,
  filterTimeline,
  isStale,
  previewAction,
  staleAfterMilliseconds,
  summarizeInventory,
} from "../src/presentation.ts";

const now = new Date("2026-07-12T15:00:00.000Z");

test("summary does not report stale data as freshly healthy", () => {
  const snapshot = buildDemonstrationSnapshot(now);
  const summary = summarizeInventory(snapshot, now);

  assert.deepEqual(summary, { total: 4, healthy: 1, degraded: 1, attentionRequired: 1, stale: 1 });
});

test("administrative preview distinguishes denied, unsupported and unknown", () => {
  const snapshot = buildConfigurationSnapshot();
  assert.equal(previewAction(snapshot, "service.start", true), "unsupported");
  assert.equal(previewAction(snapshot, "service.start", false), "denied");
  assert.equal(previewAction(snapshot, "missing", true), "unknown");
});

test("dashboard text palette meets WCAG AA normal-text contrast", () => {
  for (const [foreground, background] of dashboardTextContrastPairs) {
    assert.ok(contrastRatio(foreground, background) >= 4.5, `${foreground} on ${background}`);
  }
});

test("semantic and motion accessibility guards remain in source", () => {
  const app = readFileSync(new URL("../src/App.tsx", import.meta.url), "utf8");
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");
  const html = readFileSync(new URL("../index.html", import.meta.url), "utf8");
  assert.match(html, /lang="pt-BR"/);
  assert.match(app, /className="skip-link"/);
  assert.match(app, /<main id="main-content"/);
  assert.match(app, /aria-modal="true"/);
  assert.match(css, /prefers-reduced-motion: reduce/);
  assert.match(css, /:focus-visible/);
});

test("timeline filters severity without provider-specific branches", () => {
  const snapshot = buildTimelineAlertSnapshot(now);
  assert.equal(filterTimeline(snapshot.events, "", "critical").length, 1);
  assert.equal(filterTimeline(snapshot.events, "mysql", "all").length, 1);
  assert.equal(snapshot.alerts.filter((alert) => alert.state === "active").length, 1);
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
