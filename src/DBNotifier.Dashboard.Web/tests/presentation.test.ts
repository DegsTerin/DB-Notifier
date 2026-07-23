/** Module purpose: Verifies presentation test behaviour and protects the documented project contract. */
import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { inflateSync } from "node:zlib";
import {
  buildDemonstrationSnapshot,
  buildTimelineAlertSnapshot,
  buildConfigurationSnapshot,
  classifyEvidenceFreshness,
  filterInventory,
  filterTimeline,
  isStale,
  normalizeAlertState,
  normalizeCapabilityState,
  normalizeEventSeverity,
  previewAction,
  staleAfterMilliseconds,
  summarizeFleetAggregate,
  summarizeInventory,
} from "../src/presentation.ts";
import {
  designSystemVersion,
  replaceSemanticFavicon,
  semanticBrandAssets,
  semanticFaviconId,
} from "../src/semanticBrand.ts";

const now = new Date("2026-07-12T15:00:00.000Z");

type IcoFrame = {
  size: number;
  dib: Buffer;
  pixels: Buffer;
  mask: Buffer;
};

/** Parses uncompressed 32-bit DIB frames from one deterministic generated ICO asset. */
function readIcoFrames(icon: Buffer): IcoFrame[] {
  const frameCount = icon.readUInt16LE(4);
  return Array.from({ length: frameCount }, (_, entry) => {
    const entryOffset = 6 + entry * 16;
    const size = icon[entryOffset] === 0 ? 256 : icon[entryOffset];
    const dataLength = icon.readUInt32LE(entryOffset + 8);
    const dataOffset = icon.readUInt32LE(entryOffset + 12);
    const dib = icon.subarray(dataOffset, dataOffset + dataLength);
    const pixelLength = size * size * 4;
    const maskLength = Math.ceil(size / 32) * 4 * size;
    return {
      size,
      dib,
      pixels: dib.subarray(40, 40 + pixelLength),
      mask: dib.subarray(40 + pixelLength, 40 + pixelLength + maskLength),
    };
  });
}

/** Extracts frame alpha bytes so semantic colours can be proved to share one silhouette. */
function alphaMask(frame: IcoFrame): Buffer {
  const alpha = Buffer.alloc(frame.size * frame.size);
  for (let pixel = 0; pixel < alpha.length; pixel += 1) alpha[pixel] = frame.pixels[pixel * 4 + 3];
  return alpha;
}

type NormalisedRasterGeometry = {
  left: number;
  right: number;
  top: number;
  bottom: number;
  centroidX: number;
  centroidY: number;
};

/** Measures one visible colour layer in top-down normalised coordinates for cross-size silhouette regression. */
function normalisedRasterGeometry(
  frame: IcoFrame,
  includesPixel: (red: number, green: number, blue: number, alpha: number) => boolean,
): NormalisedRasterGeometry {
  let minimumX = frame.size;
  let maximumX = -1;
  let minimumY = frame.size;
  let maximumY = -1;
  let weightedX = 0;
  let weightedY = 0;
  let totalWeight = 0;
  for (let storedY = 0; storedY < frame.size; storedY += 1) {
    const visualY = frame.size - 1 - storedY;
    for (let x = 0; x < frame.size; x += 1) {
      const offset = (storedY * frame.size + x) * 4;
      const blue = frame.pixels[offset];
      const green = frame.pixels[offset + 1];
      const red = frame.pixels[offset + 2];
      const alpha = frame.pixels[offset + 3];
      if (!includesPixel(red, green, blue, alpha)) continue;
      const weight = alpha / 255;
      minimumX = Math.min(minimumX, x);
      maximumX = Math.max(maximumX, x);
      minimumY = Math.min(minimumY, visualY);
      maximumY = Math.max(maximumY, visualY);
      weightedX += (x + 0.5) * weight;
      weightedY += (visualY + 0.5) * weight;
      totalWeight += weight;
    }
  }
  assert.ok(totalWeight > 0, `${frame.size} px frame lost a required canonical colour layer.`);
  return {
    left: minimumX / frame.size,
    right: (maximumX + 1) / frame.size,
    top: minimumY / frame.size,
    bottom: (maximumY + 1) / frame.size,
    centroidX: weightedX / totalWeight / frame.size,
    centroidY: weightedY / totalWeight / frame.size,
  };
}

/** Parses the deterministic non-interlaced RGBA PNG emitted for Windows notification attribution. */
function readRgbaPng(image: Buffer): { width: number; height: number; pixels: Buffer } {
  assert.deepEqual([...image.subarray(0, 8)], [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
  let offset = 8;
  let width = 0;
  let height = 0;
  const compressed: Buffer[] = [];
  while (offset < image.length) {
    const length = image.readUInt32BE(offset);
    const type = image.toString("ascii", offset + 4, offset + 8);
    const payload = image.subarray(offset + 8, offset + 8 + length);
    if (type === "IHDR") {
      width = payload.readUInt32BE(0);
      height = payload.readUInt32BE(4);
      assert.deepEqual([...payload.subarray(8, 13)], [8, 6, 0, 0, 0]);
    } else if (type === "IDAT") {
      compressed.push(payload);
    } else if (type === "IEND") {
      break;
    }
    offset += 12 + length;
  }

  assert.ok(width > 0 && height > 0);
  const scanlines = inflateSync(Buffer.concat(compressed));
  const pixels = Buffer.alloc(width * height * 4);
  for (let row = 0; row < height; row += 1) {
    const rowOffset = row * (1 + width * 4);
    assert.equal(scanlines[rowOffset], 0, "The generated notification PNG must use deterministic unfiltered rows.");
    scanlines.copy(pixels, row * width * 4, rowOffset + 1, rowOffset + 1 + width * 4);
  }
  return { width, height, pixels };
}

test("summary does not report stale data as freshly healthy", () => {
  const snapshot = buildDemonstrationSnapshot(now);
  const summary = summarizeInventory(snapshot, now);

  assert.deepEqual(summary, { total: 4, healthy: 1, degraded: 1, warning: 1, attentionRequired: 1, stale: 0, disabled: 1 });
});

test("summary excludes stale status classes and counts current maintenance as warning", () => {
  const template = buildDemonstrationSnapshot(now).items[0];
  const staleAt = new Date(now.getTime() - staleAfterMilliseconds - 1).toISOString();
  const snapshot = {
    schemaVersion: "inventory.v1" as const,
    generatedAt: now.toISOString(),
    items: [
      { ...template, instanceId: "healthy", status: "healthy" as const, observedAt: staleAt, receivedAt: staleAt },
      { ...template, instanceId: "degraded", status: "degraded" as const, observedAt: staleAt, receivedAt: staleAt },
      { ...template, instanceId: "critical", status: "timeout" as const, observedAt: staleAt, receivedAt: staleAt },
      { ...template, instanceId: "maintenance", status: "maintenance" as const, observedAt: now.toISOString(), receivedAt: now.toISOString() },
    ],
  };

  assert.deepEqual(summarizeInventory(snapshot, now), {
    total: 4, healthy: 0, degraded: 0, warning: 1, attentionRequired: 0, stale: 3, disabled: 0,
  });
});

test("disabled inventory remains visible without contributing to current health", () => {
  const template = buildDemonstrationSnapshot(now).items[0];
  const snapshot = {
    schemaVersion: "inventory.v1" as const,
    generatedAt: now.toISOString(),
    items: [
      { ...template, instanceId: "disabled-critical", status: "timeout" as const, enabled: false },
      { ...template, instanceId: "enabled-healthy", enabled: true },
    ],
  };

  assert.deepEqual(summarizeInventory(snapshot, now), {
    total: 2, healthy: 1, degraded: 0, warning: 0, attentionRequired: 0, stale: 0, disabled: 1,
  });
  assert.equal(summarizeFleetAggregate(snapshot, now), "healthy");
  assert.equal(filterInventory(snapshot.items, "", "disabled", now).length, 1);
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

test("invalid, future and unrecognised evidence fails safely to unknown", () => {
  const template = buildDemonstrationSnapshot(now).items[0];
  const aggregate = (item: typeof template) => summarizeFleetAggregate({
    schemaVersion: "inventory.v1",
    generatedAt: now.toISOString(),
    items: [item],
  }, now);
  const future = new Date(now.getTime() + 1).toISOString();

  assert.equal(classifyEvidenceFreshness({ ...template, observedAt: "invalid", receivedAt: "invalid" }, now), "unknown");
  assert.equal(classifyEvidenceFreshness({ ...template, observedAt: future, receivedAt: future }, now), "unknown");
  assert.equal(aggregate({ ...template, observedAt: "invalid", receivedAt: "invalid" }), "unknown");
  assert.equal(aggregate({ ...template, status: "unrecognised" as typeof template.status }), "unknown");
  assert.equal(normalizeEventSeverity("unrecognised"), "unknown");
  assert.equal(normalizeAlertState("unrecognised"), "unknown");
  assert.equal(normalizeCapabilityState("unrecognised"), "unknown");
});

test("semantic brand replacement keeps header and favicon on the same aggregate without reusing the old node", () => {
  for (const state of ["healthy", "warning", "critical", "unknown"] as const) {
    const stateAssets = semanticBrandAssets(state);
    assert.equal(stateAssets.iconPath, `/dbnotifier-icon.${state}.svg?v=2.6.13-${state}`);
    assert.equal(stateAssets.faviconPath, `/dbnotifier-favicon.${state}.ico?v=2.6.13-${state}`);
  }

  const assets = semanticBrandAssets("critical");
  let replacement: HTMLLinkElement | undefined;
  let appended = false;
  const current = {
    replaceWith(node: HTMLLinkElement) {
      replacement = node;
    },
  };
  const ownerDocument = {
    createElement(tagName: string) {
      assert.equal(tagName, "link");
      return {
        dataset: {},
        setAttribute(name: string, value: string) {
          if (name === "sizes") this.sizesValue = value;
        },
      };
    },
    getElementById(id: string) {
      assert.equal(id, semanticFaviconId);
      return current;
    },
    querySelector() {
      return undefined;
    },
    querySelectorAll() {
      return replacement ? [replacement] : [];
    },
    head: {
      append() {
        appended = true;
      },
    },
  } as unknown as Document;

  replaceSemanticFavicon(ownerDocument, assets.faviconPath, "critical");

  assert.equal(designSystemVersion, "2.6.13");
  assert.equal(assets.iconPath, "/dbnotifier-icon.critical.svg?v=2.6.13-critical");
  assert.equal(assets.faviconPath, "/dbnotifier-favicon.critical.ico?v=2.6.13-critical");
  assert.equal(replacement?.id, semanticFaviconId);
  assert.equal(replacement?.href, assets.faviconPath);
  assert.equal(replacement?.dataset.aggregateState, "critical");
  assert.equal(appended, false);

  let legacyReplacement: HTMLLinkElement | undefined;
  let staleCandidateRemoved = false;
  const legacyCandidate = {
    replaceWith(node: HTMLLinkElement) {
      legacyReplacement = node;
    },
  };
  const staleCandidate = {
    remove() {
      staleCandidateRemoved = true;
    },
  };
  const legacyDocument = {
    createElement() {
      return {
        dataset: {},
        setAttribute() {},
      };
    },
    getElementById() {
      return undefined;
    },
    querySelector(selector: string) {
      assert.equal(selector, 'link[rel~="icon"]');
      return legacyCandidate;
    },
    querySelectorAll(selector: string) {
      assert.equal(selector, 'link[rel~="icon"]');
      return legacyReplacement ? [legacyReplacement, staleCandidate] : [staleCandidate];
    },
    head: {
      append() {
        assert.fail("The legacy favicon should be replaced instead of duplicated");
      },
    },
  } as unknown as Document;

  replaceSemanticFavicon(legacyDocument, assets.faviconPath, "critical");

  assert.equal(legacyReplacement?.id, semanticFaviconId);
  assert.equal(staleCandidateRemoved, true);
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
  assert.match(css, /forced-colors: active/);
  assert.match(css, /\.nav-item\.active \{ color: HighlightText; background: Highlight; forced-color-adjust: none; \}/);
  assert.match(css, /\.nav-item\.active:focus-visible \{ outline-color: CanvasText; \}/);
  assert.match(css, /\.nav-item\.active \.nav-count \{ color: Highlight; background: HighlightText; border: 1px solid HighlightText; \}/);
  assert.match(css, /\.overview-kpis \.summary-card:last-child \{ border-inline-end: 1px solid var\(--db-component-card-border\); \}/);
  assert.match(css, /\.status-badge, \.metric-icon, \.demo-badge, \.read-only-label \{/);
  assert.match(css, /border: 1px solid CanvasText/);
  assert.match(css, /\.loading-line::after \{ background: Highlight; \}/);
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

test("TV mode keeps Fullscreen, factual source context and independently aged evidence", () => {
  const app = readFileSync(new URL("../src/App.tsx", import.meta.url), "utf8");
  const control = readFileSync(new URL("../src/TvModeButton.tsx", import.meta.url), "utf8");
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");
  const audit = readFileSync(new URL("../../../scripts/audit-state05-dashboard.mjs", import.meta.url), "utf8");
  const auditGate = readFileSync(new URL("../../../scripts/run-state05-dashboard-audit.ps1", import.meta.url), "utf8");

  assert.match(app, /<TvModeButton active=\{tvMode\} onActiveChange=\{handleTvModeChange\}/);
  assert.match(app, /tvMode && <TvModeStatus authoritativeSandbox=\{authoritativeSandbox\} \/>/);
  assert.match(app, /function TvModeStatus\(\{ authoritativeSandbox \}[\s\S]*setInterval\(\(\) => setClock\(new Date\(\)\), 1_000\)/);
  assert.match(app, /TV\.SourceSandbox/);
  assert.match(app, /setInterval\(\(\) => setNow\(new Date\(\)\), 30_000\)/);
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
  for (const width of [320, 390, 768, 1920]) assert.match(audit, new RegExp(`width: ${width}`));
  assert.match(audit, /documentScrollWidth: root\.scrollWidth/);
  assert.match(audit, /appScrollWidth: app\?\.scrollWidth/);
  assert.match(auditGate, /documentScrollWidth -gt \$_\.documentClientWidth/);
  assert.match(auditGate, /'320,390,768,1920'/);
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

test("performance charts surrender intrinsic SVG height before their responsive card can clip", () => {
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");
  const browserAudit = readFileSync(new URL("../../../scripts/audit-state05-dashboard.mjs", import.meta.url), "utf8");
  const browserGate = readFileSync(new URL("../../../scripts/run-state05-dashboard-audit.ps1", import.meta.url), "utf8");

  assert.match(css, /\.trend-plot \{[^}]*min-width: 0;[^}]*min-height: 0;[^}]*grid-template-rows: minmax\(0, 1fr\) auto;/s);
  assert.match(css, /\.trend-chart svg \{[^}]*width: 100%;[^}]*height: 100%;[^}]*min-height: 0;[^}]*overflow: visible;/s);
  assert.match(browserAudit, /async function auditPerformanceChart\(call\)/);
  assert.match(browserAudit, /overview-200-percent-browser-zoom-1280x900/);
  assert.match(browserAudit, /performance-400-percent-browser-zoom-1280x900/);
  assert.match(browserGate, /A performance chart was clipped or overflowed its owning card in the 100\/200\/400-percent reflow matrix\./);
  assert.match(browserGate, /TV mode clipped the performance chart at a required layout width\./);
  const authoritativeAudit = readFileSync(new URL("../../../scripts/audit-state06-dashboard-tv-browser.mjs", import.meta.url), "utf8");
  assert.match(authoritativeAudit, /performanceChartCount: document\.querySelectorAll\("\.trend-chart"\)\.length/);
  assert.match(authoritativeAudit, /view\.performanceChartCount === 0 && Boolean\(view\.sourceTruth\)/);
});

test("overview status text remains contained and separated from latency and sparklines during panel reflow", () => {
  const app = readFileSync(new URL("../src/App.tsx", import.meta.url), "utf8");
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");
  const browserAudit = readFileSync(new URL("../../../scripts/audit-state05-dashboard.mjs", import.meta.url), "utf8");
  const browserGate = readFileSync(new URL("../../../scripts/run-state05-dashboard-audit.ps1", import.meta.url), "utf8");
  const authoritativeAudit = readFileSync(new URL("../../../scripts/audit-state06-dashboard-tv-browser.mjs", import.meta.url), "utf8");

  assert.match(app, /className="overview-status-region"><StatusBadge/);
  assert.match(app, /className="status-badge-label"/);
  assert.match(css, /\.overview-fleet \{ container: overview-fleet \/ inline-size; \}/);
  assert.match(css, /\.overview-status-region \.status-badge \{[^}]*max-width: 100%;[^}]*white-space: normal;/);
  assert.match(css, /@container overview-fleet \(max-width: 767px\)/);
  assert.match(css, /@container overview-fleet \(max-width: 430px\)/);
  assert.match(browserAudit, /async function auditOverviewStatusLayout\(call\)/);
  assert.match(browserAudit, /document\.createRange\(\)/);
  assert.match(browserAudit, /getClientRects\(\)/);
  assert.match(browserAudit, /statusSparklineIntersectionArea/);
  assert.match(browserAudit, /sparklineExpected/);
  assert.match(browserAudit, /latencyVisible/);
  assert.match(browserAudit, /visualOrderValid/);
  assert.match(browserAudit, /layoutWidth = Math\.floor\(dimensions\.width \/ zoomFactor\)/);
  assert.match(browserAudit, /allStatusBoundariesVisible/);
  assert.match(browserAudit, /allStatusesUseSystemColours/);
  assert.match(browserAudit, /overview-human-review-1180x760/);
  assert.match(browserAudit, /overview-compact-820x620/);
  assert.match(browserGate, /An Overview status escaped its region, lost text, overflowed, or intersected latency or sparkline evidence\./);
  assert.match(browserGate, /The focal forced-colour Overview status matrix was incomplete, inactive, duplicated, or lost system-colour boundaries or containment\./);
  assert.match(browserGate, /\.overviewStatus\.sparklineCount -ne 4/);
  assert.match(browserGate, /consecutiveAbsentChecks -ge 3/);
  assert.match(authoritativeAudit, /async function readOverviewStatusLayout\(call\)/);
  assert.match(authoritativeAudit, /const identityVisible = Boolean\(/);
  assert.match(authoritativeAudit, /const pillVisible = Boolean\(/);
  assert.match(authoritativeAudit, /const labelVisible = Boolean\(/);
  assert.match(authoritativeAudit, /sparklineExpected/);
  assert.match(authoritativeAudit, /view\.overviewStatus\.allValid && view\.overviewStatus\.sparklineCount === 0/);
});

test("alert summary uses its full row without inheriting empty inventory columns", () => {
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");

  assert.match(css, /\.alert-summary \{ width: 100%; max-width: none; grid-template-columns: repeat\(3, minmax\(150px, 1fr\)\); \}/);
  assert.match(css, /@media \(max-width: 1100px\)[\s\S]*?\.summary-grid \{ grid-template-columns: repeat\(3, minmax\(0, 1fr\)\); \}[\s\S]*?\.alert-summary \{ grid-template-columns: repeat\(3, minmax\(0, 1fr\)\); \}/);
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
  const semanticBrand = readFileSync(new URL("../src/semanticBrand.ts", import.meta.url), "utf8");
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
  const desktopEvidence = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/DesktopDemonstrationEvidence.cs", import.meta.url), "utf8");
  const flyoutXaml = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/TrayFlyoutWindow.xaml", import.meta.url), "utf8");
  const flyoutCode = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/TrayFlyoutWindow.xaml.cs", import.meta.url), "utf8");
  const brandStatusPolicy = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/BrandStatusIconPolicy.cs", import.meta.url), "utf8");
  const trayController = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/TrayApplicationController.cs", import.meta.url), "utf8");
  const installer = readFileSync(new URL("../../../packaging/inno/DBNotifier.iss", import.meta.url), "utf8");
  const compatibilityBuild = readFileSync(new URL("../../../build/build.ps1", import.meta.url), "utf8");
  const brandGenerator = readFileSync(new URL("../../../scripts/generate-brand-assets.mjs", import.meta.url), "utf8");
  const notificationAsset = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/NotificationAssets/DBNotifier.Availability.png", import.meta.url));
  const semanticNotificationAssets = ["Healthy", "Warning", "Critical", "Unknown"].map((state) =>
    readFileSync(new URL(`../../DBNotifier.Desktop.Wpf/NotificationAssets/DBNotifier.${state}.png`, import.meta.url)));
  const notificationPublisher = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/WindowsAppNotificationPublisher.cs", import.meta.url), "utf8");

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
  const semanticWindowsIcons = [healthyIcon, warningIcon, criticalIcon, unknownIcon];
  const expectedSmallAccents = [
    { red: 72, green: 199, blue: 95 },
    { red: 250, green: 184, blue: 42 },
    { red: 198, green: 40, blue: 40 },
    { red: 148, green: 163, blue: 184 },
  ];
  const referenceFrames = readIcoFrames(unknownIcon);
  for (const [stateIndex, icon] of semanticWindowsIcons.entries()) {
    assert.deepEqual([...icon.subarray(0, 6)], [0, 0, 1, 0, 9, 0]);
    const frames = readIcoFrames(icon);
    assert.deepEqual(frames.map((frame) => frame.size), [16, 20, 24, 32, 40, 48, 64, 128, 256]);
    frames.forEach((frame, index) => assert.deepEqual(alphaMask(frame), alphaMask(referenceFrames[index])));

    const largestFrame = frames[frames.length - 1];
    let transparentPixels = 0;
    for (let pixel = 0; pixel < 256 * 256; pixel += 1) {
      if (largestFrame.pixels[pixel * 4 + 3] === 0) transparentPixels += 1;
    }
    assert.ok(transparentPixels > 256 * 256 * 0.5);
    const transparentInteriorX = 100;
    const transparentInteriorY = 80;
    const storedInteriorRow = 255 - transparentInteriorY;
    assert.equal(largestFrame.pixels[(storedInteriorRow * 256 + transparentInteriorX) * 4 + 3], 0);

    const smallestFrame = frames[0];
    let strongSmallPixels = 0;
    let transparentSmallPixels = 0;
    let partialSmallPixels = 0;
    let visibleBluePixels = 0;
    let visibleAccentPixels = 0;
    for (let pixel = 0; pixel < 16 * 16; pixel += 1) {
      const offset = pixel * 4;
      const alpha = smallestFrame.pixels[pixel * 4 + 3];
      const blue = smallestFrame.pixels[offset];
      const green = smallestFrame.pixels[offset + 1];
      const red = smallestFrame.pixels[offset + 2];
      if (alpha >= 128) strongSmallPixels += 1;
      if (alpha === 0) transparentSmallPixels += 1;
      if (alpha > 0 && alpha < 255) partialSmallPixels += 1;
      if (red === 0 && green === 120 && blue === 212 && alpha > 0) visibleBluePixels += 1;
      const expectedAccent = expectedSmallAccents[stateIndex];
      if (red === expectedAccent.red && green === expectedAccent.green && blue === expectedAccent.blue && alpha > 0) {
        visibleAccentPixels += 1;
      }
    }
    assert.ok(strongSmallPixels >= 40);
    assert.ok(transparentSmallPixels > 16 * 16 * 0.5);
    assert.ok(partialSmallPixels > 0);
    assert.ok(visibleBluePixels > 0);
    assert.ok(visibleAccentPixels > 0);
    const storedTopInteriorRow = 15 - 2;
    const storedBodyInteriorRow = 15 - 9;
    assert.equal(smallestFrame.pixels[(storedTopInteriorRow * 16 + 5) * 4 + 3], 0);
    assert.equal(smallestFrame.pixels[(storedBodyInteriorRow * 16 + 5) * 4 + 3], 0);

    for (const frame of frames) {
      let partialAlphaPixels = 0;
      for (let pixel = 0; pixel < frame.size * frame.size; pixel += 1) {
        const alpha = frame.pixels[pixel * 4 + 3];
        const row = Math.floor(pixel / frame.size);
        const column = pixel % frame.size;
        const maskStride = Math.ceil(frame.size / 32) * 4;
        const maskBit = frame.mask[row * maskStride + Math.floor(column / 8)] & (0x80 >> (column % 8));
        assert.equal(maskBit !== 0, alpha === 0, `${frame.size} px legacy mask disagrees with BGRA transparency.`);
        if (alpha > 0 && alpha < 255) partialAlphaPixels += 1;
      }
      assert.ok(partialAlphaPixels > 0);
    }
  }
  assert.notDeepEqual(healthyIcon, warningIcon);
  assert.notDeepEqual(warningIcon, criticalIcon);
  assert.notDeepEqual(criticalIcon, unknownIcon);
  const healthyNotificationFrame = readIcoFrames(healthyIcon).find((frame) => frame.size === 16);
  assert.ok(healthyNotificationFrame);
  let hasCanonicalGreenBellPixel = false;
  for (let pixel = 0; pixel < healthyNotificationFrame.size * healthyNotificationFrame.size; pixel += 1) {
    const offset = pixel * 4;
    const blue = healthyNotificationFrame.pixels[offset];
    const green = healthyNotificationFrame.pixels[offset + 1];
    const red = healthyNotificationFrame.pixels[offset + 2];
    const alpha = healthyNotificationFrame.pixels[offset + 3];
    if (red === 72 && green === 199 && blue === 95 && alpha > 0) hasCanonicalGreenBellPixel = true;
  }
  assert.ok(hasCanonicalGreenBellPixel, "The native small availability source lost the canonical Healthy green bell.");
  const notificationPng = readRgbaPng(notificationAsset);
  assert.deepEqual({ width: notificationPng.width, height: notificationPng.height }, { width: 64, height: 64 });
  const healthyNotificationAssetFrame = readIcoFrames(healthyIcon).find((frame) => frame.size === 64);
  assert.ok(healthyNotificationAssetFrame);
  let notificationTransparent = 0;
  let notificationBlue = 0;
  let notificationGreen = 0;
  let notificationPartial = 0;
  for (let pixel = 0; pixel < 64 * 64; pixel += 1) {
    const offset = pixel * 4;
    const red = notificationPng.pixels[offset];
    const green = notificationPng.pixels[offset + 1];
    const blue = notificationPng.pixels[offset + 2];
    const alpha = notificationPng.pixels[offset + 3];
    if (alpha === 0) notificationTransparent += 1;
    if (alpha > 0 && alpha < 255) notificationPartial += 1;
    if (red === 0 && green === 120 && blue === 212 && alpha === 255) notificationBlue += 1;
    if (red === 72 && green === 199 && blue === 95 && alpha === 255) notificationGreen += 1;
    const x = pixel % 64;
    const y = Math.floor(pixel / 64);
    const icoOffset = ((63 - y) * 64 + x) * 4;
    assert.deepEqual(
      [red, green, blue, alpha],
      [
        healthyNotificationAssetFrame.pixels[icoOffset + 2],
        healthyNotificationAssetFrame.pixels[icoOffset + 1],
        healthyNotificationAssetFrame.pixels[icoOffset],
        healthyNotificationAssetFrame.pixels[icoOffset + 3],
      ],
      `The notification PNG diverged from the canonical Healthy 64 px frame at ${x},${y}.`,
    );
  }
  assert.ok(notificationTransparent > 64 * 64 * 0.5);
  assert.ok(notificationPartial > 0);
  assert.ok(notificationBlue > 0);
  assert.ok(notificationGreen > 0);
  assert.deepEqual(notificationAsset, semanticNotificationAssets[0]);
  semanticNotificationAssets.forEach((asset, stateIndex) => {
    const png = readRgbaPng(asset);
    const matchingFrame = readIcoFrames(semanticWindowsIcons[stateIndex]).find((frame) => frame.size === 64);
    assert.ok(matchingFrame);
    let transparentPixels = 0;
    let partialPixels = 0;
    for (let pixel = 0; pixel < 64 * 64; pixel += 1) {
      const offset = pixel * 4;
      const x = pixel % 64;
      const y = Math.floor(pixel / 64);
      const icoOffset = ((63 - y) * 64 + x) * 4;
      if (png.pixels[offset + 3] === 0) transparentPixels += 1;
      if (png.pixels[offset + 3] > 0 && png.pixels[offset + 3] < 255) partialPixels += 1;
      assert.deepEqual(
        [...png.pixels.subarray(offset, offset + 4)],
        [
          matchingFrame.pixels[icoOffset + 2],
          matchingFrame.pixels[icoOffset + 1],
          matchingFrame.pixels[icoOffset],
          matchingFrame.pixels[icoOffset + 3],
        ],
        `The ${stateIndex} semantic notification PNG diverged from its canonical 64 px ICO frame at ${x},${y}.`,
      );
    }
    assert.ok(transparentPixels > 64 * 64 * 0.5);
    assert.ok(partialPixels > 0);
  });
  assert.deepEqual(favicon, unknownFavicon);
  const semanticFavicons = [healthyFavicon, warningFavicon, criticalFavicon, unknownFavicon];
  for (const favicon of semanticFavicons) {
    assert.deepEqual([...favicon.subarray(0, 6)], [0, 0, 1, 0, 4, 0]);
  }
  semanticFavicons.forEach((faviconAsset, stateIndex) => {
    const faviconFrames = readIcoFrames(faviconAsset);
    const windowsFrames = readIcoFrames(semanticWindowsIcons[stateIndex]);
    assert.deepEqual(faviconFrames.map((frame) => frame.size), [16, 20, 24, 32]);
    faviconFrames.forEach((frame, frameIndex) => assert.deepEqual(frame.dib, windowsFrames[frameIndex].dib));
  });
  assert.notDeepEqual(healthyFavicon, warningFavicon);
  assert.notDeepEqual(warningFavicon, criticalFavicon);
  assert.notDeepEqual(criticalFavicon, unknownFavicon);

  const criticalFrames = readIcoFrames(criticalIcon);
  const proportionalLayers = [
    { name: "complete mark", includesPixel: (_red: number, _green: number, _blue: number, alpha: number) => alpha > 0 },
    { name: "database", includesPixel: (red: number, green: number, blue: number, alpha: number) => red === 0 && green === 120 && blue === 212 && alpha > 0 },
    { name: "bell", includesPixel: (red: number, green: number, blue: number, alpha: number) => red === 198 && green === 40 && blue === 40 && alpha > 0 },
  ];
  const proportionalReference = criticalFrames[criticalFrames.length - 1];
  for (const layer of proportionalLayers) {
    const reference = normalisedRasterGeometry(proportionalReference, layer.includesPixel);
    for (const frame of criticalFrames) {
      const actual = normalisedRasterGeometry(frame, layer.includesPixel);
      const rasterTolerance = 1.5 / frame.size + 1 / proportionalReference.size;
      for (const property of Object.keys(reference) as Array<keyof NormalisedRasterGeometry>) {
        assert.ok(
          Math.abs(actual[property] - reference[property]) <= rasterTolerance,
          `${frame.size} px ${layer.name} ${property} diverged from the canonical proportional silhouette.`,
        );
      }
    }
  }

  for (const frame of criticalFrames) {
    let straightBlueEdge = false;
    let straightRedEdge = false;
    for (let pixel = 0; pixel < frame.size * frame.size; pixel += 1) {
      const offset = pixel * 4;
      const alpha = frame.pixels[offset + 3];
      if (alpha === 0 || alpha === 255) continue;
      const blue = frame.pixels[offset];
      const green = frame.pixels[offset + 1];
      const red = frame.pixels[offset + 2];
      if (red === 0 && green === 120 && blue === 212) straightBlueEdge = true;
      if (red === 198 && green === 40 && blue === 40) straightRedEdge = true;
    }
    assert.ok(straightBlueEdge, `${frame.size} px frame lost the canonical blue on a partially covered edge.`);
    assert.ok(straightRedEdge, `${frame.size} px frame lost the canonical Critical red on a partially covered edge.`);
  }

  assert.doesNotMatch(dashboardSvg.toString("utf8"), /<rect/);
  assert.match(dashboardSvg.toString("utf8"), /fill="none" stroke="#0078D4"/);
  assert.doesNotMatch(dashboardSvg.toString("utf8"), /#0F2940|#F8FAFC|#55B4FF/);
  assert.match(brandGenerator, /const markGeometry = Object\.freeze/);
  assert.doesNotMatch(brandGenerator, /microGlyph16|renderMicroBitmap|renderOpticalCanonicalBitmap/);
  assert.match(brandGenerator, /const \{ database, bell \} = markGeometry;/);
  assert.match(brandGenerator, /for \(const centreY of \[database\.seamCentreY, database\.bottomCentreY\]\)/);
  assert.match(brandGenerator, /const supersampling = 4;/);
  assert.match(brandGenerator, /notificationPngs:[\s\S]*Availability:[\s\S]*Healthy:[\s\S]*Warning:[\s\S]*Critical:[\s\S]*Unknown:/);
  assert.match(brandGenerator, /const visibleSamples = coverage\[coverageOffset\] \+ coverage\[coverageOffset \+ 1\]/);
  assert.match(brandGenerator, /pixels\[offset \+ 3\] = Math\.round\(\(visibleSamples \/ samples\) \* 255\)/);
  assert.match(brandGenerator, /const coverageBySize = new Map\(iconSizes\.map\(\(size\) => \[size, renderCoverage\(size\)\]\)\)/);
  assert.doesNotMatch(brandGenerator, /includeMiddleSeam|databaseStrokeRadius = Math\.max/);
  assert.match(brandGenerator, /strokeWidth:\s*3\.5/);
  assert.match(html, /id="dbnotifier-favicon"[^>]+dbnotifier-favicon\.unknown\.ico\?v=2\.6\.13-unknown/);
  assert.match(app, /summarizeFleetAggregate\(snapshot, presentationNow\)/);
  assert.match(app, /useLayoutEffect\(\(\) =>/);
  assert.match(app, /replaceSemanticFavicon\(document, brandAssets\.faviconPath, aggregateState\)/);
  assert.match(semanticBrand, /querySelector<HTMLLinkElement>\('link\[rel~="icon"\]'\)/);
  assert.match(semanticBrand, /replacement\.dataset\.aggregateState = state/);
  assert.match(semanticBrand, /current\.replaceWith\(replacement\)/);
  assert.match(semanticBrand, /if \(candidate !== replacement\) candidate\.remove\(\)/);
  assert.match(app, /<img src=\{brandAssets\.iconPath\} alt=""/);
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
  assert.match(desktopCode, /DpiScale dpi = VisualTreeHelper\.GetDpi\(this\)/);
  assert.match(desktopCode, /Icon = BrandStatusIconPolicy\.LoadImageSource\(aggregateState, 32, dpi\)/);
  assert.match(desktopCode, /BrandStatusImage\.Source = BrandStatusIconPolicy\.LoadImageSource\(aggregateState, 40, dpi\)/);
  assert.match(desktopCode, /BrandStatusIconPolicy\.ApplyNativeWindowIcons\(this, aggregateState\)/);
  assert.match(desktopApp, /DesktopDemonstrationEvidence\.Create\(generatedAt\)/);
  assert.match(desktopApp, /evidence\.Summarise\(generatedAt\)/);
  assert.match(desktopApp, /new\(\s*localisation,\s*theme,\s*motion,\s*providerVisualIdentityPolicy,\s*evidence,\s*fleetSummary\.State,\s*accessibilityReviewMode\)/);
  assert.match(desktopEvidence, /TrayFleetPresentationPolicy\.Summarise\(CreateInventorySnapshot/);
  assert.match(desktopEvidence, /GeneratedAt = generatedAt/);
  assert.match(flyoutXaml, /x:Name="BrandStatusImage"/);
  assert.match(flyoutCode, /DpiScale dpi = VisualTreeHelper\.GetDpi\(this\)/);
  assert.match(flyoutCode, /BrandStatusIconPolicy\.LoadImageSource\(fleetSummary\.State, 32, dpi\)/);
  assert.match(trayController, /Icon = applicationIcon/);
  assert.match(trayController, /CreateNotificationAreaResources\(initialSummary\.State\)/);
  assert.match(trayController, /BrandStatusIconPolicy\.LoadWindowsIcon\([\s\S]*state,[\s\S]*Forms\.SystemInformation\.SmallIconSize\.Width\)/);
  assert.match(trayController, /BrandStatusIconPolicy\.LoadWindowsIcon\([\s\S]*TrayNotificationPresentationPolicy\.ResolveIconState\(request\.Meaning\),[\s\S]*Forms\.SystemInformation\.SmallIconSize\.Width\)/);
  assert.doesNotMatch(trayController, /notifyIcon\.BalloonTipShown \+=/);
  assert.doesNotMatch(trayController, /notifyIcon\.BalloonTipClosed \+=/);
  assert.match(trayController, /notifyIcon\.BalloonTipClicked \+= NotifyIconBalloonTipClicked/);
  assert.match(trayController, /TrayPresentationPolicy\.ShouldRequestAvailabilityConfirmation\(intent\)/);
  assert.match(trayController, /ShowCloseToTrayNotification\(\)/);
  assert.match(trayController, /WindowsAppNotificationPublisher\.TryCreate/);
  assert.match(trayController, /appNotificationPublisher\?\.TryPublishAvailability\(title, message\)/);
  assert.match(trayController, /instanceStates = evidence\.CaptureInstanceStates\(evidence\.GeneratedAt\)/);
  assert.match(trayController, /TrayInstanceStateChangePolicy\.DetectChanges\([\s\S]*instanceStates,[\s\S]*nextInstanceStates\)/);
  assert.match(trayController, /instanceStates = nextInstanceStates;[\s\S]*PublishDemonstrationStatusChanges\(changes\)/);
  assert.match(trayController, /TryPublishDemonstrationStatusChange\([\s\S]*change\.InstanceId,[\s\S]*title,[\s\S]*message,[\s\S]*meaning/);
  assert.match(trayController, /QueueLegacyNotification\(title, message, meaning\)/);
  assert.match(trayController, /MaximumLegacyNotificationQueueLength = 16/);
  assert.match(trayController, /legacyNotificationQueue\.Enqueue/);
  assert.match(trayController, /legacyNotificationInFlight/);
  assert.match(trayController, /Stopwatch\.GetElapsedTime\(notificationIconLeaseStartedTimestamp\)/);
  assert.match(trayController, /Stopwatch\.GetElapsedTime\(legacyNotificationStartedTimestamp\)/);
  assert.doesNotMatch(trayController, /firstHide|ShowFirstHideNotification/);
  assert.match(trayController, /try[\s\S]*notificationIconRestoreTimer\.Stop\(\);[\s\S]*notifyIcon\.Icon = notificationMeaningIcon;[\s\S]*TrayNotificationIconLeaseSignal\.Begin[\s\S]*legacyNotificationAdvanceTimer\.Start\(\);[\s\S]*ShowBalloonTip\([\s\S]*Forms\.ToolTipIcon\.None/);
  assert.equal((trayController.match(/\bShowBalloonTip\s*\(/g) ?? []).length, 1);
  assert.match(trayController, /TrayNotificationIconLeaseSignal\.FallbackElapsed/);
  assert.match(trayController, /TrayNotificationIconLeaseSignal\.DeliveryFailed/);
  assert.match(trayController, /TrayNotificationIconLeaseSignal\.Disposed/);
  assert.match(trayController, /RestoreAggregateIconAfterNotificationCapture\(TrayNotificationIconLeaseSignal signal\)[\s\S]*notifyIcon\.Icon = applicationIcon;[\s\S]*TrayNotificationIconLeasePolicy\.Resolve[\s\S]*notificationIconRestoreTimer\.Stop\(\);/);
  assert.match(trayController, /CompleteLegacyNotification\(TrayNotificationIconLeaseSignal signal\)[\s\S]*legacyNotificationInFlight = false;[\s\S]*TryShowNextLegacyNotification\(\)/);
  assert.match(trayController, /NotifyIconBalloonTipClicked[\s\S]*Apply\(TrayWindowIntent\.Show\)/);
  assert.doesNotMatch(trayController, /notificationIconRestorePending/);
  assert.match(brandStatusPolicy, /IconBitmapDecoder/);
  assert.match(brandStatusPolicy, /Math\.Ceiling\(targetDipSize \* Math\.Max\(dpi\.DpiScaleX, dpi\.DpiScaleY\)\)/);
  assert.match(brandStatusPolicy, /candidate\.PixelWidth >= targetPixelSize/);
  assert.match(brandStatusPolicy, /OrderByDescending\(candidate => candidate\.PixelWidth\)/);
  assert.doesNotMatch(brandStatusPolicy, /BitmapImage image = new\(\)/);
  assert.match(brandStatusPolicy, /SendMessage\(windowHandle, WmSetIcon, IconSmall2/);
  assert.match(brandStatusPolicy, /SendMessage\(windowHandle, WmSetIcon, IconBig/);
  assert.match(brandStatusPolicy, /TrayAggregateState\.Healthy => "DBNotifier\.Healthy\.ico"/);
  assert.match(brandStatusPolicy, /TrayAggregateState\.Warning => "DBNotifier\.Warning\.ico"/);
  assert.match(brandStatusPolicy, /TrayAggregateState\.Critical => "DBNotifier\.Critical\.ico"/);
  assert.match(brandStatusPolicy, /TrayAggregateState\.Unknown => "DBNotifier\.Unknown\.ico"/);
  assert.doesNotMatch(trayController, /SystemIcons\.Application/);
  assert.match(notificationPublisher, /AppNotificationManager\.IsSupported\(\)/);
  assert.match(notificationPublisher, /runtime-initialisation-failed/);
  assert.match(notificationPublisher, /manager\.Register\(DisplayName, new Uri\(iconPath\)\)/);
  assert.match(notificationPublisher, /DBNotifier\.Availability\.png/);
  assert.match(notificationPublisher, /TryPublishDemonstrationStatusChange/);
  assert.match(notificationPublisher, /SetAppLogoOverride\(new Uri\(iconPath\), AppNotificationImageCrop\.Default, title\)/);
  assert.match(notificationPublisher, /DBNotifier\.Healthy\.png/);
  assert.match(notificationPublisher, /DBNotifier\.Warning\.png/);
  assert.match(notificationPublisher, /DBNotifier\.Critical\.png/);
  assert.match(notificationPublisher, /DBNotifier\.Unknown\.png/);
  assert.match(notificationPublisher, /AvailabilityGroup = "local-avail"/);
  assert.match(notificationPublisher, /DemonstrationStatusGroup = "local-demo"/);
  assert.match(notificationPublisher, /WindowsNotificationIdentifierMaximumLength = 16/);
  assert.match(notificationPublisher, /Guid\.NewGuid\(\)\.ToString\("N"\)\[\.\.4\]/);
  assert.match(notificationPublisher, /instanceId\.ToString\("N"\)\[\^3\.\.\]/);
  assert.match(notificationPublisher, /\$"d\{demonstrationStatusSession\}\{instanceSuffix\}\{sequence:x8\}"/);
  assert.match(notificationPublisher, /CreateDemonstrationStatusTag\(instanceId\)/);
  assert.match(notificationPublisher, /Interlocked\.Increment\(ref demonstrationStatusSequence\)/);
  assert.match(notificationPublisher, /tag\.Length <= WindowsNotificationIdentifierMaximumLength/);
  assert.match(notificationPublisher, /DemonstrationStatusGroup/);
  assert.match(notificationPublisher, /\.MuteAudio\(\)/);
  assert.match(notificationPublisher, /manager\.Show\(notification\)/);
  assert.match(notificationPublisher, /notification\.SuppressDisplay = false/);
  assert.match(desktopProject, /<PackageReference Include="Microsoft\.WindowsAppSDK" \/>/);
  for (const asset of ["Availability", "Healthy", "Warning", "Critical", "Unknown"]) {
    assert.match(desktopProject, new RegExp(`NotificationAssets\\\\DBNotifier\\.${asset}\\.png`));
  }
  assert.match(installer, /SetupIconFile=.*DBNotifier\.ico/);
  assert.match(installer, /#define AppDisplayName "DB Notifier"/);
  assert.match(installer, /AppName=\{#AppDisplayName\}/);
  assert.match(installer, /DefaultDirName=\{autopf\}\\\{#AppName\}/);
  assert.match(compatibilityBuild, /Compatibility packaging remains unavailable in R5/);
  assert.doesNotMatch(compatibilityBuild, /-IconFile \$iconPath/);
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
  assert.match(controller, /new TrayFlyoutWindow\(\s*localisation,\s*providerVisualIdentityPolicy,\s*evidence,\s*initialSummary,\s*ShowView/);
  assert.match(controller, /evidence\.Summarise\(evaluatedAt\)/);
  assert.match(controller, /Interval = TimeSpan\.FromSeconds\(30\)/);
  assert.match(controller, /window\.RefreshOperationalEvidence\(evaluatedAt, next\.State\)/);
  assert.match(controller, /Tray\.TooltipSummary/);
  assert.match(controller, /notifyIcon\.MouseClick \+= NotifyIconMouseClick/);
  assert.match(controller, /Forms\.MouseButtons\.Left or Forms\.MouseButtons\.Right/);
  assert.doesNotMatch(controller, /notifyIcon\.DoubleClick/);
  assert.match(flyoutCode, /FormatUtc\(evidence\.GeneratedAt\)/);
  assert.doesNotMatch(flyoutCode, /TimeProvider\.System\.GetUtcNow/);
  assert.match(controller, /Forms\.ToolTipIcon\.None/);
  assert.doesNotMatch(controller, /Forms\.ToolTipIcon\.Info/);
  assert.doesNotMatch(controller, /ContextMenuStrip|ContextMenuOpening/);
});

test("timeline filters severity without provider-specific branches", () => {
  const snapshot = buildTimelineAlertSnapshot(now);
  assert.equal(filterTimeline(snapshot.events, "", "critical").length, 1);
  assert.equal(filterTimeline(snapshot.events, "mysql", "all").length, 1);
  assert.equal(snapshot.alerts.filter((alert) => alert.state === "active").length, 1);
});

test("stale policy changes only after the five-minute boundary", () => {
  const boundary = new Date(now.getTime() - staleAfterMilliseconds).toISOString();
  const item = {
    ...buildDemonstrationSnapshot(now).items[0],
    observedAt: boundary,
    receivedAt: boundary,
  };

  assert.equal(isStale(item, now), false);
  assert.equal(isStale(item, new Date(now.getTime() + 1)), true);
});

test("filter searches provider-neutral fields and stale state", () => {
  const snapshot = buildDemonstrationSnapshot(now);

  assert.equal(filterInventory(snapshot.items, "azure", "all", now).length, 1);
  assert.equal(filterInventory(snapshot.items, "", "stale", now).length, 0);
  assert.equal(filterInventory(snapshot.items, "", "disabled", now).length, 1);
  assert.equal(filterInventory(snapshot.items, "mysql", "healthy", now).length, 0);
});

test("filter maps future and reversed evidence to Unknown instead of its raw health status", () => {
  const healthy = buildDemonstrationSnapshot(now).items[0];
  const future = {
    ...healthy,
    instanceId: "future-evidence",
    observedAt: new Date(now.getTime() + 1_000).toISOString(),
    receivedAt: new Date(now.getTime() + 2_000).toISOString(),
  };
  const reversed = {
    ...healthy,
    instanceId: "reversed-evidence",
    observedAt: now.toISOString(),
    receivedAt: new Date(now.getTime() - 1_000).toISOString(),
  };

  assert.equal(filterInventory([future, reversed], "", "healthy", now).length, 0);
  assert.equal(filterInventory([future, reversed], "", "unknown", now).length, 2);
  assert.equal(filterInventory([future, reversed], "", "stale", now).length, 0);
});

test("route titles, complete identifiers and provider registries retain one canonical presentation contract", () => {
  const app = readFileSync(new URL("../src/App.tsx", import.meta.url), "utf8");
  const localisationProvider = readFileSync(new URL("../src/LocalisationProvider.tsx", import.meta.url), "utf8");
  const providerRegistry = readFileSync(new URL("../src/providerIconRegistry.ts", import.meta.url), "utf8");
  const providerGenerator = readFileSync(new URL("../../../scripts/generate-provider-icon-assets.mjs", import.meta.url), "utf8");

  assert.match(app, /document\.title = `DB Notifier — \$\{t\(copy\.titleKey\)\}`/);
  assert.doesNotMatch(localisationProvider, /document\.title/);
  assert.equal((app.match(/className="machine-id"/g) ?? []).length, 2);
  assert.match(providerRegistry, /from "\.\/generated\/providerIconRegistry\.ts"/);
  assert.match(providerGenerator, /design-system["', ]+, "provider-icons"/);
  assert.match(providerGenerator, /generateRegistries\(\)/);
  assert.match(providerGenerator, /verifyRegistries\(\)/);
});
