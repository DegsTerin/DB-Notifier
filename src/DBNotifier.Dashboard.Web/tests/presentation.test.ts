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
  summarizeFleetAggregate,
  summarizeInventory,
} from "../src/presentation.ts";

const now = new Date("2026-07-12T15:00:00.000Z");

test("summary does not report stale data as freshly healthy", () => {
  const snapshot = buildDemonstrationSnapshot(now);
  const summary = summarizeInventory(snapshot, now);

  assert.deepEqual(summary, { total: 4, healthy: 1, degraded: 1, attentionRequired: 1, stale: 1 });
});

test("fleet aggregate drives every semantic product-mark state with Tray precedence", () => {
  const template = buildDemonstrationSnapshot(now).items[0];
  const snapshot = (status: typeof template.status, receivedAt = now.toISOString()) => ({
    schemaVersion: "inventory.v1" as const,
    generatedAt: now.toISOString(),
    items: [{ ...template, status, receivedAt }],
  });

  assert.equal(summarizeFleetAggregate(snapshot("healthy"), now), "healthy");
  assert.equal(summarizeFleetAggregate(snapshot("degraded"), now), "warning");
  assert.equal(summarizeFleetAggregate(snapshot("timeout"), now), "critical");
  assert.equal(summarizeFleetAggregate(snapshot("healthy", new Date(now.getTime() - staleAfterMilliseconds - 1).toISOString()), now), "unknown");
  assert.equal(summarizeFleetAggregate({ ...snapshot("healthy"), items: [] }, now), "unknown");
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
  const healthySvg = readFileSync(new URL("../public/dbnotifier-icon.healthy.svg", import.meta.url));
  const warningSvg = readFileSync(new URL("../public/dbnotifier-icon.warning.svg", import.meta.url));
  const criticalSvg = readFileSync(new URL("../public/dbnotifier-icon.critical.svg", import.meta.url));
  const unknownSvg = readFileSync(new URL("../public/dbnotifier-icon.unknown.svg", import.meta.url));
  const designSystemSvg = readFileSync(new URL("../../../design-system/assets/dbnotifier-database.svg", import.meta.url));
  const windowsIcon = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/Assets/DBNotifier.ico", import.meta.url));
  const healthyIcon = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/Assets/DBNotifier.Healthy.ico", import.meta.url));
  const warningIcon = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/Assets/DBNotifier.Warning.ico", import.meta.url));
  const criticalIcon = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/Assets/DBNotifier.Critical.ico", import.meta.url));
  const unknownIcon = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/Assets/DBNotifier.Unknown.ico", import.meta.url));
  const favicon = readFileSync(new URL("../public/dbnotifier-favicon.ico", import.meta.url));
  const healthyFavicon = readFileSync(new URL("../public/dbnotifier-favicon.healthy.ico", import.meta.url));
  const warningFavicon = readFileSync(new URL("../public/dbnotifier-favicon.warning.ico", import.meta.url));
  const criticalFavicon = readFileSync(new URL("../public/dbnotifier-favicon.critical.ico", import.meta.url));
  const unknownFavicon = readFileSync(new URL("../public/dbnotifier-favicon.unknown.ico", import.meta.url));
  const desktopProject = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/DBNotifier.Desktop.Wpf.csproj", import.meta.url), "utf8");
  const desktopXaml = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/MainWindow.xaml", import.meta.url), "utf8");
  const desktopCode = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/MainWindow.xaml.cs", import.meta.url), "utf8");
  const desktopApp = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/App.xaml.cs", import.meta.url), "utf8");
  const flyoutXaml = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/TrayFlyoutWindow.xaml", import.meta.url), "utf8");
  const flyoutCode = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/TrayFlyoutWindow.xaml.cs", import.meta.url), "utf8");
  const brandStatusPolicy = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/BrandStatusIconPolicy.cs", import.meta.url), "utf8");
  const trayController = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/TrayApplicationController.cs", import.meta.url), "utf8");
  const installer = readFileSync(new URL("../../../packaging/inno/DBNotifier.iss", import.meta.url), "utf8");
  const compatibilityBuild = readFileSync(new URL("../../../build/build.ps1", import.meta.url), "utf8");
  const brandGenerator = readFileSync(new URL("../../../scripts/generate-brand-assets.mjs", import.meta.url), "utf8");

  assert.deepEqual(dashboardSvg, designSystemSvg);
  assert.deepEqual(dashboardSvg, unknownSvg);
  assert.match(healthySvg.toString("utf8"), /#48C75F/);
  assert.match(warningSvg.toString("utf8"), /#FAB82A/);
  assert.match(criticalSvg.toString("utf8"), /#C62828/);
  assert.match(unknownSvg.toString("utf8"), /#94A3B8/);
  assert.notDeepEqual(healthySvg, warningSvg);
  assert.notDeepEqual(warningSvg, criticalSvg);
  assert.notDeepEqual(criticalSvg, unknownSvg);
  assert.deepEqual([...windowsIcon.subarray(0, 6)], [0, 0, 1, 0, 9, 0]);
  assert.deepEqual(windowsIcon, unknownIcon);
  for (const icon of [healthyIcon, warningIcon, criticalIcon, unknownIcon]) {
    assert.deepEqual([...icon.subarray(0, 6)], [0, 0, 1, 0, 9, 0]);
    const largestEntry = 6 + 8 * 16;
    const bitmapOffset = icon.readUInt32LE(largestEntry + 12) + 40;
    let transparentPixels = 0;
    for (let pixel = 0; pixel < 256 * 256; pixel += 1) {
      if (icon[bitmapOffset + pixel * 4 + 3] === 0) transparentPixels += 1;
    }
    assert.ok(transparentPixels > 256 * 256 * 0.5);
    const transparentInteriorX = 100;
    const transparentInteriorY = 80;
    const storedInteriorRow = 255 - transparentInteriorY;
    assert.equal(icon[bitmapOffset + (storedInteriorRow * 256 + transparentInteriorX) * 4 + 3], 0);

    const smallestBitmapOffset = icon.readUInt32LE(6 + 12) + 40;
    let opaqueSmallPixels = 0;
    for (let pixel = 0; pixel < 16 * 16; pixel += 1) {
      if (icon[smallestBitmapOffset + pixel * 4 + 3] >= 128) opaqueSmallPixels += 1;
    }
    assert.ok(opaqueSmallPixels >= 90);

    for (const [entry, size] of [16, 20, 24, 32].entries()) {
      const entryOffset = 6 + entry * 16;
      const smallBitmapOffset = icon.readUInt32LE(entryOffset + 12) + 40;
      let partialAlphaPixels = 0;
      for (let pixel = 0; pixel < size * size; pixel += 1) {
        const alpha = icon[smallBitmapOffset + pixel * 4 + 3];
        if (alpha > 0 && alpha < 255) partialAlphaPixels += 1;
      }
      assert.equal(partialAlphaPixels, 0);
    }
  }
  assert.notDeepEqual(healthyIcon, warningIcon);
  assert.notDeepEqual(warningIcon, criticalIcon);
  assert.notDeepEqual(criticalIcon, unknownIcon);
  assert.deepEqual(favicon, unknownFavicon);
  for (const favicon of [healthyFavicon, warningFavicon, criticalFavicon, unknownFavicon]) {
    assert.deepEqual([...favicon.subarray(0, 6)], [0, 0, 1, 0, 4, 0]);
  }
  assert.notDeepEqual(healthyFavicon, warningFavicon);
  assert.notDeepEqual(warningFavicon, criticalFavicon);
  assert.notDeepEqual(criticalFavicon, unknownFavicon);
  assert.doesNotMatch(dashboardSvg.toString("utf8"), /<rect/);
  assert.match(dashboardSvg.toString("utf8"), /fill="none" stroke="#0078D4"/);
  assert.doesNotMatch(dashboardSvg.toString("utf8"), /#0F2940|#F8FAFC|#55B4FF/);
  assert.match(brandGenerator, /const includeMiddleSeam = size >= 24;/);
  assert.match(brandGenerator, /Math\.max\(1\.3, 48 \/ size\)/);
  assert.match(brandGenerator, /const supersampling = size <= 32 \? 1 : size <= 48 \? 2 : 4;/);
  assert.match(brandGenerator, /const colours = \[accent, databaseStroke, transparent\];/);
  assert.match(html, /rel="icon"[^>]+dbnotifier-favicon\.unknown\.ico\?v=2\.6\.5/);
  assert.match(app, /summarizeFleetAggregate\(snapshot, now\)/);
  assert.match(app, /const brandIconPath = `\/dbnotifier-icon\.\$\{aggregateState\}\.svg\?v=2\.6\.5`/);
  assert.match(app, /const brandFaviconPath = `\/dbnotifier-favicon\.\$\{aggregateState\}\.ico\?v=2\.6\.5`/);
  assert.match(app, /favicon\.href = brandFaviconPath/);
  assert.match(app, /<img src=\{brandIconPath\} alt=""/);
  assert.match(html, /<title>DB Notifier — Visão geral<\/title>/);
  assert.match(app, /aria-label="DB Notifier"/);
  assert.match(app, /className="brand-wordmark"/);
  assert.match(desktopXaml, /Text="DB"[^>]+ComponentBrandWordmarkAccentBrush/);
  assert.match(desktopXaml, /Text="Notifier"[^>]+ComponentShellChromeForegroundBrush/);
  assert.match(desktopProject, /<ApplicationIcon>Assets\\DBNotifier\.ico<\/ApplicationIcon>/);
  assert.match(desktopProject, /<AssemblyTitle>DB Notifier<\/AssemblyTitle>/);
  assert.match(desktopProject, /<Product>DB Notifier<\/Product>/);
  assert.doesNotMatch(desktopXaml, /Icon="Assets\/DBNotifier\.ico"/);
  assert.match(desktopXaml, /x:Name="BrandStatusImage"/);
  assert.match(desktopCode, /Icon = BrandStatusIconPolicy\.LoadImageSource\(aggregateState, 32\)/);
  assert.match(desktopCode, /BrandStatusImage\.Source = BrandStatusIconPolicy\.LoadImageSource\(aggregateState, 40\)/);
  assert.match(desktopCode, /BrandStatusIconPolicy\.ApplyNativeWindowIcons\(this, aggregateState\)/);
  assert.match(desktopApp, /CreateDemonstrationSummary\(\)/);
  assert.match(desktopApp, /new\(localisation, theme, fleetSummary\.State\)/);
  assert.match(flyoutXaml, /x:Name="BrandStatusImage"/);
  assert.match(flyoutCode, /BrandStatusIconPolicy\.LoadImageSource\(fleetSummary\.State, 32\)/);
  assert.match(trayController, /Icon = applicationIcon/);
  assert.match(trayController, /BrandStatusIconPolicy\.LoadWindowsIcon\([\s\S]*fleetSummary\.State,[\s\S]*Forms\.SystemInformation\.SmallIconSize\.Width\)/);
  assert.match(brandStatusPolicy, /IconBitmapDecoder/);
  assert.match(brandStatusPolicy, /Math\.Abs\(candidate\.PixelWidth - targetPixelSize\)/);
  assert.doesNotMatch(brandStatusPolicy, /BitmapImage image = new\(\)/);
  assert.match(brandStatusPolicy, /SendMessage\(windowHandle, WmSetIcon, IconSmall2/);
  assert.match(brandStatusPolicy, /SendMessage\(windowHandle, WmSetIcon, IconBig/);
  assert.match(brandStatusPolicy, /TrayAggregateState\.Healthy => "DBNotifier\.Healthy\.ico"/);
  assert.match(brandStatusPolicy, /TrayAggregateState\.Warning => "DBNotifier\.Warning\.ico"/);
  assert.match(brandStatusPolicy, /TrayAggregateState\.Critical => "DBNotifier\.Critical\.ico"/);
  assert.match(brandStatusPolicy, /TrayAggregateState\.Unknown => "DBNotifier\.Unknown\.ico"/);
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
  assert.match(flyout, /x:Name="AggregateText"/);
  assert.match(flyoutCode, /localisation\.Text\("Tray\.LocalSnapshot"/);
  assert.match(flyoutCode, /localisation\.Text\("Tray\.AggregateSummary"/);
  assert.doesNotMatch(flyout, /Click="OpenInventoryClick"/);
  assert.match(flyout, /Click="OpenOverviewClick"/);
  assert.match(flyout, /Click="OpenHistoryAlertsClick"/);
  assert.match(flyout, /Click="OpenConfigurationClick"/);
  assert.match(flyout, /DynamicResource Tray\.RestartUnavailable/);
  assert.match(flyout, /Grid\.Column="2"/);
  assert.doesNotMatch(flyout, /Click="Restart/);
  assert.match(controller, /new TrayFlyoutWindow\(localisation, fleetSummary, ShowView/);
  assert.match(controller, /TrayFleetPresentationPolicy\.Summarise/);
  assert.match(controller, /Tray\.TooltipSummary/);
  assert.match(controller, /notifyIcon\.MouseClick \+= NotifyIconMouseClick/);
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
