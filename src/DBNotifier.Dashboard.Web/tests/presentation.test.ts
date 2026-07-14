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

test("enterprise shell uses coherent icons, complete navigation and separated overview KPIs", () => {
  const app = readFileSync(new URL("../src/App.tsx", import.meta.url), "utf8");
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");

  assert.match(app, /function AppIcon/);
  assert.match(app, /className="metric-icon"/);
  assert.equal((app.match(/className={`nav-item/g) ?? []).length, 8);
  assert.doesNotMatch(app, /className={`nav-item[^>]*}><span aria-hidden="true">/s);
  assert.match(app, /function OverviewView/);
  assert.match(app, /className="overview-grid"/);
  assert.match(app, /className="overview-instance-list"/);
  assert.match(app, /className="overview-alert-list"/);
  assert.match(app, /className="provider-ring"/);
  assert.match(css, /main \{[^}]*max-width: none;[^}]*justify-self: stretch;/s);
  assert.match(app, /TopBar\.Notifications/);
  assert.match(app, /TopBar\.Settings/);
  assert.match(app, /Navigation\.Performance/);
  assert.match(app, /Navigation\.Providers/);
  assert.match(css, /\.summary-grid \{[^}]*overflow: hidden;[^}]*border:/s);
  assert.match(css, /\.summary-grid\.overview-summary\s*\{[^}]*grid-template-columns:\s*repeat\(4,/s);
  assert.match(css, /\.summary-grid\.overview-kpis \{[^}]*gap:/s);
  assert.match(css, /\.overview-kpis \.summary-card \{[^}]*border:/s);
  assert.match(css, /\.overview-summary \.summary-card:last-child\s*\{\s*grid-column:\s*auto;/);
});

test("TV mode keeps a visible Fullscreen toggle and factual demonstration context", () => {
  const app = readFileSync(new URL("../src/App.tsx", import.meta.url), "utf8");
  const control = readFileSync(new URL("../src/TvModeButton.tsx", import.meta.url), "utf8");
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");

  assert.match(app, /<TvModeButton active=\{tvMode\} onActiveChange=\{handleTvModeChange\}/);
  assert.match(app, /tvMode && <div className="tv-mode-status"/);
  assert.match(app, /navigate\("overview"\)/);
  assert.match(control, /requestFullscreen\(\)/);
  assert.match(control, /typeof document\.documentElement\.requestFullscreen !== "function"/);
  assert.match(control, /TV\.FullscreenUnavailable/);
  assert.match(control, /exitFullscreen\(\)/);
  assert.match(control, /addEventListener\("fullscreenchange"/);
  assert.match(control, /aria-pressed=\{active\}/);
  assert.match(control, /data-tv-mode-control=\{active \? "exit" : "enter"\}/);
  assert.match(css, /\.app-shell\.tv-mode \.sidebar \{ display: none; \}/);
  assert.match(css, /\.app-shell\.tv-mode \.filters \{ display: none; \}/);
  assert.match(css, /\.app-shell\.tv-mode \.language-selector, \.app-shell\.tv-mode \.theme-selector \{ display: grid; \}/);
  assert.match(css, /\.app-shell\.tv-mode \.demo-badge \{ display: flex; \}/);
});

test("mobile topbar keeps brand and controls in one accessible row", () => {
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");

  assert.match(css, /@media \(max-width: 620px\) \{[^}]*\.topbar \{[^}]*flex-direction: row;[^}]*align-items: center;/s);
  assert.match(css, /@media \(max-width: 620px\)[\s\S]*?\.preference-icon-button \{[^}]*width: 44px;[^}]*height: 44px;/);
  assert.doesNotMatch(css, /@media \(max-width: 620px\) \{[^}]*\.topbar \{[^}]*flex-direction: column;/s);
});

test("compact alert and capability cards keep a readable single-column flow", () => {
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");

  assert.match(css, /@media \(max-width: 767px\)[\s\S]*?\.alert-list, \.capability-list \{ grid-template-columns: 1fr; \}/);
  assert.match(css, /\.alert-card dd \{[^}]*overflow-wrap: anywhere;/);
  assert.match(css, /\.capability-list article > code, \.capability-list article > p \{[^}]*overflow-wrap: anywhere;[^}]*white-space: normal;/);
});

test("alert summary uses its full row without inheriting empty inventory columns", () => {
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");

  assert.match(css, /\.alert-summary \{ width: 100%; max-width: none; grid-template-columns: repeat\(3, minmax\(150px, 1fr\)\); \}/);
  assert.match(css, /@media \(max-width: 1100px\)[\s\S]*?\.summary-grid \{ grid-template-columns: repeat\(5, minmax\(0, 1fr\)\); \}[\s\S]*?\.alert-summary \{ grid-template-columns: repeat\(3, minmax\(0, 1fr\)\); \}/);
});

test("crawler policy explicitly blocks crawler access to the operational console", () => {
  const robots = readFileSync(new URL("../public/robots.txt", import.meta.url), "utf8");

  assert.match(robots, /^# Module purpose:/);
  assert.match(robots, /^User-agent: \*$/m);
  assert.match(robots, /^Disallow: \/$/m);
});

test("desktop sidebar label uses the AA-safe secondary text token", () => {
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");

  assert.match(css, /\.sidebar-section-label \{[^}]*color: var\(--db-colour-text-secondary\);/);
  assert.doesNotMatch(css, /\.sidebar-section-label \{[^}]*color: var\(--db-colour-text-muted\);/);
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
  assert.match(app, /<img src="\/dbnotifier-icon\.svg\?v=1\.4\.0" alt=""/);
  assert.match(html, /<title>DB Notifier — Visão geral<\/title>/);
  assert.match(app, /aria-label="DB Notifier"/);
  assert.match(app, /className="brand-wordmark"/);
  assert.match(desktopXaml, /Text="DB"[^>]+ComponentBrandWordmarkAccentBrush/);
  assert.match(desktopXaml, /Text="Notifier"[^>]+ComponentShellChromeForegroundBrush/);
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

test("Tray flyout preserves operational scanning while administrative execution remains unavailable", () => {
  const flyout = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/TrayFlyoutWindow.xaml", import.meta.url), "utf8");
  const flyoutCode = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/TrayFlyoutWindow.xaml.cs", import.meta.url), "utf8");
  const controller = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/TrayApplicationController.cs", import.meta.url), "utf8");

  assert.equal((flyout.match(/DynamicResource Sample\.Instance\./g) ?? []).length, 4);
  assert.match(flyout, /DynamicResource Tray\.FleetSummary/);
  assert.match(flyoutCode, /localisation\.Text\("Tray\.LocalSnapshot"/);
  assert.doesNotMatch(flyout, /Click="OpenInventoryClick"/);
  assert.match(flyout, /Click="OpenOverviewClick"/);
  assert.match(flyout, /Click="OpenHistoryAlertsClick"/);
  assert.match(flyout, /Click="OpenConfigurationClick"/);
  assert.match(flyout, /DynamicResource Tray\.RestartUnavailable/);
  assert.match(flyout, /Grid\.Column="2"/);
  assert.doesNotMatch(flyout, /Click="Restart/);
  assert.match(controller, /new TrayFlyoutWindow\(localisation, ShowView/);
  assert.match(controller, /notifyIcon\.MouseUp \+= NotifyIconMouseUp/);
  assert.match(controller, /Forms\.MouseButtons\.Left or Forms\.MouseButtons\.Right/);
  assert.doesNotMatch(controller, /ContextMenuStrip|ContextMenuOpening/);
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
