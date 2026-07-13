/** Module purpose: Verifies presentation test behaviour and protects the documented project contract. */
import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
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

test("modal and mobile regression guards remain in source", () => {
  const app = readFileSync(new URL("../src/App.tsx", import.meta.url), "utf8");
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");

  assert.match(app, /dialog\.showModal\(\)/);
  assert.match(app, /event\.key === "Escape"/);
  assert.match(app, /event\.shiftKey/);
  assert.match(app, /openerRef\.current\?\.focus\(\)/);
  assert.match(css, /\.app-shell \{ width: 100%; min-width: 0;/);
  assert.match(css, /\.sidebar nav \{[^}]*width: 100%;[^}]*min-width: 0;[^}]*display: flex;[^}]*overflow-x: auto;/s);
});

test("enterprise shell uses coherent icons and a consolidated responsive metric band", () => {
  const app = readFileSync(new URL("../src/App.tsx", import.meta.url), "utf8");
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");

  assert.match(app, /function AppIcon/);
  assert.match(app, /className="metric-icon"/);
  assert.equal((app.match(/className={`nav-item/g) ?? []).length, 4);
  assert.doesNotMatch(app, /className={`nav-item[^>]*}><span aria-hidden="true">/s);
  assert.match(css, /main \{[^}]*max-width: none;[^}]*justify-self: stretch;/s);
  assert.match(css, /\.summary-grid \{[^}]*overflow: hidden;[^}]*border:/s);
  assert.match(css, /\.summary-card \{[^}]*border-inline-end:/s);
});

test("provider-neutral database mark is shared by active Web and Windows surfaces", () => {
  const app = readFileSync(new URL("../src/App.tsx", import.meta.url), "utf8");
  const html = readFileSync(new URL("../index.html", import.meta.url), "utf8");
  const dashboardSvg = readFileSync(new URL("../public/dbnotifier-icon.svg", import.meta.url));
  const designSystemSvg = readFileSync(new URL("../../../design-system/assets/dbnotifier-database.svg", import.meta.url));
  const windowsIcon = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/Assets/DBNotifier.ico", import.meta.url));
  const desktopProject = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/DBNotifier.Desktop.Wpf.csproj", import.meta.url), "utf8");
  const desktopXaml = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/MainWindow.xaml", import.meta.url), "utf8");
  const trayController = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/TrayApplicationController.cs", import.meta.url), "utf8");
  const installer = readFileSync(new URL("../../../packaging/inno/DBNotifier.iss", import.meta.url), "utf8");
  const compatibilityBuild = readFileSync(new URL("../../../build/build.ps1", import.meta.url), "utf8");

  assert.deepEqual(dashboardSvg, designSystemSvg);
  assert.deepEqual([...windowsIcon.subarray(0, 6)], [0, 0, 1, 0, 9, 0]);
  assert.match(html, /rel="icon"[^>]+dbnotifier-icon\.svg/);
  assert.match(app, /<img src="\/dbnotifier-icon\.svg\?v=1\.3\.3" alt=""/);
  assert.match(html, /<title>DB Notifier — Inventário<\/title>/);
  assert.match(app, /aria-label="DB Notifier"/);
  assert.match(app, /<strong>DB Notifier<\/strong>/);
  assert.match(desktopXaml, /Text="DB Notifier"/);
  assert.match(desktopProject, /<ApplicationIcon>Assets\\DBNotifier\.ico<\/ApplicationIcon>/);
  assert.match(desktopXaml, /Icon="Assets\/DBNotifier\.ico"/);
  assert.match(trayController, /Icon = applicationIcon/);
  assert.doesNotMatch(trayController, /SystemIcons\.Application/);
  assert.match(installer, /SetupIconFile=.*DBNotifier\.ico/);
  assert.match(installer, /#define AppDisplayName "DB Notifier"/);
  assert.match(installer, /AppName=\{#AppDisplayName\}/);
  assert.match(installer, /DefaultDirName=\{autopf\}\\\{#AppName\}/);
  assert.match(compatibilityBuild, /-IconFile \$iconPath/);
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
