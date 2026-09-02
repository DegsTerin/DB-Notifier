/**
 * Audits the built STATE-05 Dashboard through Chrome DevTools Protocol.
 * The script records viewport, semantic-brand, accessibility-tree, keyboard and modal-focus evidence without mutating product state.
 */
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { resolveState05AuditEvidenceRoot } from "./state05-audit-evidence-root.mjs";

const endpoint = process.env.DBNOTIFIER_AUDIT_CDP_ENDPOINT ?? "http://127.0.0.1:9224";
const dashboardUrl = process.env.DBNOTIFIER_AUDIT_DASHBOARD_URL ?? "http://127.0.0.1:4173/";
const requestedLocale = process.env.DBNOTIFIER_AUDIT_LOCALE;
const locale = requestedLocale === "en-GB" ? "en-GB" : "pt-BR";
const requestedTheme = process.env.DBNOTIFIER_AUDIT_THEME;
const theme = requestedTheme === "dark" ? "dark" : "light";
const browserProduct = process.env.DBNOTIFIER_AUDIT_BROWSER_PRODUCT;
const browserVersion = process.env.DBNOTIFIER_AUDIT_BROWSER_VERSION;
if (!browserProduct || !browserVersion) throw new Error("Browser product and version provenance are required.");
const evidenceRoot = resolveState05AuditEvidenceRoot(process.env);
const evidenceDirectory = join(evidenceRoot, locale, theme);
mkdirSync(evidenceDirectory, { recursive: true });

/** Opens the current Dashboard target and returns a minimal request-response CDP client. */
async function connect() {
  const targets = await fetch(`${endpoint}/json/list`).then((response) => response.json());
  const target = targets.find((item) => item.type === "page" && item.url.startsWith(dashboardUrl));
  if (!target) throw new Error("The Dashboard Chrome target is unavailable.");

  const socket = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((resolve, reject) => {
    socket.addEventListener("open", resolve, { once: true });
    socket.addEventListener("error", reject, { once: true });
  });

  let sequence = 0;
  const pending = new Map();
  socket.addEventListener("message", (event) => {
    const message = JSON.parse(event.data);
    if (!message.id || !pending.has(message.id)) return;
    const { resolve: complete, reject, timer } = pending.get(message.id);
    pending.delete(message.id);
    clearTimeout(timer);
    if (message.error) reject(new Error(message.error.message));
    else complete(message.result);
  });

  socket.addEventListener("close", () => {
    for (const { reject, timer } of pending.values()) {
      clearTimeout(timer);
      reject(new Error("The dedicated browser closed with a CDP command pending."));
    }
    pending.clear();
  });

  /** Sends one protocol command and rejects it when Chrome exceeds the per-command deadline. */
  function call(method, params = {}) {
    const id = ++sequence;
    socket.send(JSON.stringify({ id, method, params }));
    return new Promise((resolveCommand, reject) => {
      const timer = setTimeout(() => {
        pending.delete(id);
        reject(new Error(`CDP command exceeded its deadline: ${method}`));
      }, 15_000);
      pending.set(id, { resolve: resolveCommand, reject, timer });
    });
  }

  return { socket, call };
}

/** Waits for React and layout work to settle after navigation or emulation changes. */
function settle(milliseconds = 350) {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}

/** Waits for one serialisable page condition without allowing navigation or layout work to hang the audit. */
async function waitForPage(call, expression, description, timeoutMilliseconds = 8_000) {
  const deadline = Date.now() + timeoutMilliseconds;
  while (Date.now() < deadline) {
    try {
      const result = await call("Runtime.evaluate", { expression: `Boolean(${expression})`, returnByValue: true });
      if (result.result.value) return;
    } catch {
      // A reload may transiently replace the execution context before the route becomes stable.
    }
    await settle(50);
  }
  throw new Error(`Timed out waiting for ${description}.`);
}

/** Evaluates a serialisable expression in the page and returns its value. */
async function evaluate(call, expression) {
  const result = await call("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true });
  if (result.exceptionDetails) throw new Error(result.exceptionDetails.text);
  return result.result.value;
}

/** Captures the loaded header mark and single favicon candidate so browser evidence cannot drift from the visible aggregate. */
async function auditSemanticBrand(call) {
  return evaluate(call, `(() => {
    const candidates = [...document.querySelectorAll('link[rel~="icon"]')];
    const active = candidates.find((candidate) => candidate.id === "dbnotifier-favicon");
    const headerMark = document.querySelector(".brand-mark img");
    return {
      candidateCount: candidates.length,
      href: active?.getAttribute("href") ?? null,
      aggregateState: active?.dataset.aggregateState ?? null,
      headerSrc: headerMark?.getAttribute("src") ?? null,
      headerComplete: headerMark?.complete ?? false,
      headerNaturalWidth: headerMark?.naturalWidth ?? 0,
      headerNaturalHeight: headerMark?.naturalHeight ?? 0,
    };
  })()`);
}

/** Measures whether every visible performance-chart layer remains vertically contained by its owning card. */
async function auditPerformanceChart(call) {
  return evaluate(call, `(() => {
    const chart = document.querySelector(".trend-chart");
    if (!chart) return null;
    const panel = chart.closest(".overview-panel");
    const axis = chart.querySelector(".trend-axis");
    const plot = chart.querySelector(".trend-plot");
    const graphic = chart.querySelector("svg");
    const times = chart.querySelector(".trend-times");
    const rect = (element) => element?.getBoundingClientRect() ?? null;
    const panelRect = rect(panel);
    const chartRect = rect(chart);
    const axisRect = rect(axis);
    const plotRect = rect(plot);
    const graphicRect = rect(graphic);
    const timesRect = rect(times);
    const containsVertically = (outer, inner) => Boolean(outer && inner && inner.top >= outer.top - 1 && inner.bottom <= outer.bottom + 1);
    const panelContainsChart = containsVertically(panelRect, chartRect);
    const chartContainsAxis = containsVertically(chartRect, axisRect);
    const chartContainsPlot = containsVertically(chartRect, plotRect);
    const plotContainsGraphic = containsVertically(plotRect, graphicRect);
    const plotContainsTimes = containsVertically(plotRect, timesRect);
    const panelContentOverflow = panel ? panel.scrollHeight > panel.clientHeight + 1 : true;
    const chartContentOverflow = chart.scrollHeight > chart.clientHeight + 1;
    const plotContentOverflow = plot ? plot.scrollHeight > plot.clientHeight + 1 : true;
    return {
      panelContainsChart,
      chartContainsAxis,
      chartContainsPlot,
      plotContainsGraphic,
      plotContainsTimes,
      panelContentOverflow,
      chartContentOverflow,
      plotContentOverflow,
      clipped: !panelContainsChart || !chartContainsAxis || !chartContainsPlot || !plotContainsGraphic || !plotContainsTimes || panelContentOverflow || chartContentOverflow || plotContentOverflow,
      panelHeight: panel?.clientHeight ?? null,
      panelScrollHeight: panel?.scrollHeight ?? null,
      chartHeight: chart.clientHeight,
      chartScrollHeight: chart.scrollHeight,
      plotHeight: plot?.clientHeight ?? null,
      plotScrollHeight: plot?.scrollHeight ?? null,
    };
  })()`);
}

/** Measures status containment and separation from latency and sparkline evidence in every Overview row. */
async function auditOverviewStatusLayout(call) {
  return evaluate(call, `(() => {
    const tolerance = 1;
    const rect = (element) => element?.getBoundingClientRect() ?? null;
    const contains = (outer, inner) => Boolean(
      outer && inner &&
      inner.left >= outer.left - tolerance && inner.right <= outer.right + tolerance &&
      inner.top >= outer.top - tolerance && inner.bottom <= outer.bottom + tolerance);
    const intersectionArea = (first, second) => {
      if (!first || !second) return 0;
      return Math.max(0, Math.min(first.right, second.right) - Math.max(first.left, second.left)) *
        Math.max(0, Math.min(first.bottom, second.bottom) - Math.max(first.top, second.top));
    };
    const follows = (first, second) => Boolean(
      first && second && (
        second.top >= first.bottom - tolerance ||
        (Math.min(first.bottom, second.bottom) - Math.max(first.top, second.top) > 0 && second.left >= first.right - tolerance)));
    const container = document.querySelector(".overview-fleet");
    const rows = [...document.querySelectorAll(".overview-instance-row")].map((row, index) => {
      const identity = row.querySelector(".overview-instance-name");
      const region = row.querySelector(".overview-status-region");
      const pill = region?.querySelector(".status-badge");
      const label = pill?.querySelector(".status-badge-label");
      const latency = row.querySelector(".overview-latency");
      const sparkline = row.querySelector(".sparkline");
      const rowRect = rect(row);
      const identityRect = rect(identity);
      const regionRect = rect(region);
      const pillRect = rect(pill);
      const latencyRect = rect(latency);
      const sparklineRect = rect(sparkline);
      const labelRange = label ? document.createRange() : null;
      labelRange?.selectNodeContents(label);
      const labelFragments = labelRange ? [...labelRange.getClientRects()] : [];
      labelRange?.detach();
      const pillStyle = pill ? getComputedStyle(pill) : null;
      const labelStyle = label ? getComputedStyle(label) : null;
      const identityStyle = identity ? getComputedStyle(identity) : null;
      const latencyStyle = latency ? getComputedStyle(latency) : null;
      const sparklineStyle = sparkline ? getComputedStyle(sparkline) : null;
      const labelText = label?.textContent?.trim() ?? "";
      const labelContained = labelFragments.length > 0 && labelFragments.every((fragment) => contains(pillRect, fragment));
      const pillVisible = Boolean(
        pillRect && pillRect.width > 0 && pillRect.height > 0 && pillStyle &&
        pillStyle.display !== "none" && pillStyle.visibility !== "hidden" && Number(pillStyle.opacity) > 0);
      const labelVisible = Boolean(
        labelText && labelStyle && labelStyle.display !== "none" &&
        labelStyle.visibility !== "hidden" && Number(labelStyle.opacity) > 0);
      const identityVisible = Boolean(
        identityRect && identityRect.width > 0 && identityRect.height > 0 && identityStyle &&
        identityStyle.display !== "none" && identityStyle.visibility !== "hidden" && Number(identityStyle.opacity) > 0);
      const latencyVisible = Boolean(
        latencyRect && latencyRect.width > 0 && latencyRect.height > 0 && latencyStyle &&
        latencyStyle.display !== "none" && latencyStyle.visibility !== "hidden" && Number(latencyStyle.opacity) > 0);
      const sparklineExpected = !row.classList.contains("no-sparkline");
      const sparklineVisible = Boolean(
        sparklineRect && sparklineRect.width > 0 && sparklineRect.height > 0 && sparklineStyle &&
        sparklineStyle.display !== "none" && sparklineStyle.visibility !== "hidden" && Number(sparklineStyle.opacity) > 0);
      const visualOrderValid = follows(identityRect, pillRect) && follows(pillRect, latencyRect) &&
        (!sparklineExpected || follows(latencyRect, sparklineRect));
      const pillOverflow = Boolean(pill && (pill.scrollWidth > pill.clientWidth + tolerance || pill.scrollHeight > pill.clientHeight + tolerance));
      const rowOverflow = row.scrollWidth > row.clientWidth + tolerance;
      const valid = Boolean(
        rowRect && identityRect && regionRect && pillRect && latencyRect &&
        rowRect.width > 0 && rowRect.height > 0 && identityVisible && pillVisible && labelVisible && latencyVisible && visualOrderValid &&
        labelContained && !pillOverflow && !rowOverflow &&
        contains(rowRect, identityRect) && contains(rowRect, regionRect) && contains(regionRect, pillRect) && contains(rowRect, latencyRect) &&
        (sparklineExpected
          ? sparklineVisible && contains(rowRect, sparklineRect) && sparkline.clientWidth > 0 && sparkline.scrollWidth <= sparkline.clientWidth + tolerance
          : !sparkline) &&
        intersectionArea(pillRect, latencyRect) === 0 && intersectionArea(pillRect, sparklineRect) === 0 &&
        intersectionArea(latencyRect, sparklineRect) === 0);
      return {
        index,
        statusClass: pill?.className ?? null,
        statusText: labelText,
        labelFragmentCount: labelFragments.length,
        labelContained,
        identityVisible,
        identityContainedByRow: contains(rowRect, identityRect),
        pillContainedByRegion: contains(regionRect, pillRect),
        regionContainedByRow: contains(rowRect, regionRect),
        latencyContainedByRow: contains(rowRect, latencyRect),
        latencyVisible,
        sparklineExpected,
        sparklinePresent: Boolean(sparkline),
        sparklineVisible: sparklineExpected ? sparklineVisible : true,
        visualOrderValid,
        sparklineContainedByRow: sparkline ? contains(rowRect, sparklineRect) : true,
        statusLatencyIntersectionArea: intersectionArea(pillRect, latencyRect),
        statusSparklineIntersectionArea: intersectionArea(pillRect, sparklineRect),
        latencySparklineIntersectionArea: intersectionArea(latencyRect, sparklineRect),
        pillOverflow,
        rowOverflow,
        pillVisible,
        labelVisible,
        gridTemplateAreas: getComputedStyle(row).gridTemplateAreas,
        valid,
      };
    });
    return {
      containerWidth: container?.getBoundingClientRect().width ?? 0,
      containerOverflow: Boolean(container && container.scrollWidth > container.clientWidth + tolerance),
      rowCount: rows.length,
      disabledCount: rows.filter((row) => row.statusClass?.split(/\\s+/).includes("disabled")).length,
      sparklineCount: rows.filter((row) => row.sparklinePresent).length,
      allValid: Boolean(container && container.getBoundingClientRect().width > 0 && container.scrollWidth <= container.clientWidth + tolerance) &&
        rows.length > 0 && rows.every((row) => row.valid),
      rows,
    };
  })()`);
}

/** Captures layout and screenshot evidence for one route and viewport. */
async function captureViewport(call, name, width, height, hash = "inventory", pageScaleFactor = 1) {
  await call("Emulation.setDeviceMetricsOverride", { width, height, deviceScaleFactor: 1, mobile: false });
  await call("Emulation.setPageScaleFactor", { pageScaleFactor });
  await call("Page.navigate", { url: `${dashboardUrl}#${hash}` });
  await waitForPage(call, `document.readyState === "complete" && location.hash === "#${hash}"`, `${hash} navigation`);
  await call("Page.reload", { ignoreCache: true });
  await waitForPage(
    call,
    `document.readyState === "complete" && location.hash === "#${hash}" && innerWidth === ${width} && document.querySelector("main h1") !== null`,
    `${name} route and layout`);
  await evaluate(call, "new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve(true))))");
  await evaluate(call, "window.scrollTo(0, 0); true");

  const layout = await evaluate(call, `(() => {
    const root = document.documentElement;
    const main = document.querySelector("main")?.getBoundingClientRect();
    const primaryPanel = document.querySelector(".inventory-panel")?.getBoundingClientRect();
    const topbar = document.querySelector(".topbar")?.getBoundingClientRect();
    const brand = document.querySelector(".brand-lockup")?.getBoundingClientRect();
    const topbarControls = document.querySelector(".topbar-controls")?.getBoundingClientRect();
    const alertList = document.querySelector(".alert-list");
    const alertSummary = document.querySelector(".alert-summary");
    const capabilityList = document.querySelector(".capability-list");
    const overviewGrid = document.querySelector(".overview-grid");
    const overviewSummary = document.querySelector(".overview-summary");
    const measureCardCollection = (list, selector) => {
      if (!list) return null;
      const cards = [...list.querySelectorAll(selector)];
      return {
        columns: getComputedStyle(list).gridTemplateColumns.split(" ").filter(Boolean).length,
        minimumCardWidth: cards.length ? Math.round(Math.min(...cards.map((card) => card.getBoundingClientRect().width))) : null,
        contentOverflow: cards.some((card) => card.scrollWidth > card.clientWidth + 1),
      };
    };
    const offenders = [...document.querySelectorAll("body *")]
      .filter((element) => element.scrollWidth > element.clientWidth + 1)
      .filter((element) => !["auto", "scroll"].includes(getComputedStyle(element).overflowX))
      .slice(0, 12)
      .map((element) => ({ tag: element.tagName, className: element.className, clientWidth: element.clientWidth, scrollWidth: element.scrollWidth }));
    return {
      route: location.hash,
      innerWidth,
      innerHeight,
      documentClientWidth: root.clientWidth,
      documentScrollWidth: root.scrollWidth,
      horizontalOverflow: root.scrollWidth > root.clientWidth + 1,
      mainRightGap: main ? Math.round(innerWidth - main.right) : null,
      primaryContentRightGap: primaryPanel ? Math.round(innerWidth - primaryPanel.right) : null,
      topbarSingleRow: brand && topbarControls ? Math.abs((brand.top + brand.height / 2) - (topbarControls.top + topbarControls.height / 2)) <= 2 : null,
      topbarControlsContained: topbar && topbarControls ? topbarControls.left >= topbar.left && topbarControls.right <= topbar.right : null,
      alertSummary: alertSummary ? {
        columns: getComputedStyle(alertSummary).gridTemplateColumns.split(" ").filter(Boolean).length,
        cardCount: alertSummary.querySelectorAll(":scope > .summary-card").length,
        parentRightGap: Math.round(alertSummary.parentElement.getBoundingClientRect().right - alertSummary.getBoundingClientRect().right),
      } : null,
      alertCards: measureCardCollection(alertList, ".alert-card"),
      capabilityCards: measureCardCollection(capabilityList, ":scope > article"),
      overview: overviewGrid ? {
        instanceRows: document.querySelectorAll(".overview-instance-row").length,
        alertRows: document.querySelectorAll(".overview-alert-list article").length,
        summaryColumns: overviewSummary ? getComputedStyle(overviewSummary).gridTemplateColumns.split(" ").length : 0,
        contentOverflow: overviewGrid.scrollWidth > overviewGrid.clientWidth + 1,
      } : null,
      offenders,
      heading: document.querySelector("h1")?.textContent?.trim(),
    };
  })()`);
  layout.performanceChart = await auditPerformanceChart(call);
  layout.overviewStatus = hash === "overview" ? await auditOverviewStatusLayout(call) : null;
  if (hash === "alerts") await evaluate(call, 'document.querySelector(".inventory-panel")?.scrollIntoView({ block: "start" }); true');
  if (hash === "configuration") await evaluate(call, 'document.querySelector(".capability-panel")?.scrollIntoView({ block: "start" }); true');
  await settle(80);
  const screenshot = await call("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
  const screenshotPath = join(evidenceDirectory, `${name}.png`);
  writeFileSync(screenshotPath, Buffer.from(screenshot.data, "base64"));
  return { name, width, height, hash, pageScaleFactor, screenshotPath, layout };
}

/** Dispatches a trusted pointer activation to the centre of one visible element. */
async function clickElement(call, selector) {
  const point = await evaluate(call, `(() => {
    const rect = document.querySelector(${JSON.stringify(selector)})?.getBoundingClientRect();
    return rect ? { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 } : null;
  })()`);
  if (!point) throw new Error(`Visible audit control unavailable: ${selector}`);
  await call("Input.dispatchMouseEvent", { type: "mousePressed", x: point.x, y: point.y, button: "left", clickCount: 1 });
  await call("Input.dispatchMouseEvent", { type: "mouseReleased", x: point.x, y: point.y, button: "left", clickCount: 1 });
}

/** Exercises the dedicated TV layout, native Fullscreen request, compact reflow and persistent visible exit control. */
async function auditTvMode(call) {
  await call("Emulation.setDeviceMetricsOverride", { width: 1920, height: 1080, deviceScaleFactor: 1, mobile: false });
  await call("Page.navigate", { url: `${dashboardUrl}#overview` });
  await settle(80);
  await call("Page.reload", { ignoreCache: true });
  await settle();
  await clickElement(call, ".tv-mode-button");
  await settle(300);
  const active = await evaluate(call, `(() => ({
    tvMode: document.documentElement.dataset.tvMode,
    appClass: document.querySelector(".app-shell")?.className,
    nativeFullscreen: Boolean(document.fullscreenElement),
    buttonState: document.querySelector(".tv-mode-button")?.dataset.tvModeControl,
    buttonName: document.querySelector(".tv-mode-button")?.getAttribute("aria-label"),
    sidebarDisplay: getComputedStyle(document.querySelector(".sidebar")).display,
    scenarioDisplay: getComputedStyle(document.querySelector(".scenario-control")).display,
    overviewDisplay: getComputedStyle(document.querySelector(".overview-grid")).display,
    overviewInstanceRows: document.querySelectorAll(".overview-instance-row").length,
    demoDisplay: getComputedStyle(document.querySelector(".demo-badge")).display,
    languageDisplay: getComputedStyle(document.querySelector(".language-selector")).display,
    themeDisplay: getComputedStyle(document.querySelector(".theme-selector")).display,
    systemTimeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    clockText: document.querySelector(".tv-mode-status time")?.textContent?.trim(),
    statusText: document.querySelector(".tv-mode-status")?.textContent?.trim().replace(/\s+/g, " "),
  }))()`);
  active.overviewStatus = await auditOverviewStatusLayout(call);
  const screenshot = await call("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
  const screenshotPath = join(evidenceDirectory, "tv-mode-overview-1920x1080.png");
  writeFileSync(screenshotPath, Buffer.from(screenshot.data, "base64"));
  await clickElement(call, ".tv-mode-button");
  await settle(250);
  const restored = await evaluate(call, `(() => ({
    tvMode: document.documentElement.dataset.tvMode ?? null,
    nativeFullscreen: Boolean(document.fullscreenElement),
    buttonState: document.querySelector(".tv-mode-button")?.dataset.tvModeControl,
    sidebarDisplay: getComputedStyle(document.querySelector(".sidebar")).display,
  }))()`);
  const layouts = [];
  let unavailableFullscreen = null;
  for (const dimensions of [
    { width: 320, height: 900 },
    { width: 390, height: 900 },
    { width: 768, height: 1024 },
    { width: 1920, height: 1080 },
  ]) {
    await call("Emulation.setDeviceMetricsOverride", { ...dimensions, deviceScaleFactor: 1, mobile: false });
    await call("Page.navigate", { url: `${dashboardUrl}#overview` });
    await settle(80);
    await call("Page.reload", { ignoreCache: true });
    await settle();
    await evaluate(call, `(() => {
      Object.defineProperty(document.documentElement, "requestFullscreen", { value: undefined, configurable: true });
      return true;
    })()`);
    await clickElement(call, ".tv-mode-button");
    await settle(150);
    const layout = await evaluate(call, `(() => {
      const root = document.documentElement;
      const app = document.querySelector(".app-shell");
      const topbar = document.querySelector(".topbar")?.getBoundingClientRect();
      const controls = document.querySelector(".topbar-controls")?.getBoundingClientRect();
      const overview = document.querySelector(".overview-grid");
      return {
        width: innerWidth,
        height: innerHeight,
        documentClientWidth: root.clientWidth,
        documentScrollWidth: root.scrollWidth,
        appClientWidth: app?.clientWidth ?? null,
        appScrollWidth: app?.scrollWidth ?? null,
        horizontalOverflow: root.scrollWidth > root.clientWidth,
        appOverflow: app ? app.scrollWidth > app.clientWidth : null,
        topbarControlsContained: topbar && controls
          ? controls.left >= topbar.left && controls.right <= topbar.right
          : null,
        topbarControlsWithinViewport: controls
          ? controls.left >= 0 && controls.right <= root.clientWidth
          : null,
        overviewColumns: overview
          ? getComputedStyle(overview).gridTemplateColumns.split(" ").filter(Boolean).length
          : null,
        overviewOverflow: overview ? overview.scrollWidth > overview.clientWidth : null,
      };
    })()`);
    layout.performanceChart = await auditPerformanceChart(call);
    layout.overviewStatus = await auditOverviewStatusLayout(call);
    layouts.push(layout);
    if (!unavailableFullscreen) {
      unavailableFullscreen = await evaluate(call, `(() => ({
        tvMode: document.documentElement.dataset.tvMode,
        nativeFullscreen: Boolean(document.fullscreenElement),
        buttonState: document.querySelector(".tv-mode-button")?.dataset.tvModeControl,
        announcement: document.querySelector(".tv-mode-button + [role=status]")?.textContent,
      }))()`);
    }
    await clickElement(call, ".tv-mode-button");
    await settle(100);
  }
  await call("Page.reload", { ignoreCache: true });
  await settle();
  return { active, restored, unavailableFullscreen, layouts, screenshotPath };
}

/** Records the focus sequence produced by native Tab navigation. */
async function auditKeyboard(call) {
  await call("Emulation.setDeviceMetricsOverride", { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
  await call("Emulation.setPageScaleFactor", { pageScaleFactor: 1 });
  await call("Page.navigate", { url: `${dashboardUrl}#inventory` });
  await settle(80);
  await call("Page.reload", { ignoreCache: true });
  await settle();
  await evaluate(call, "document.activeElement?.blur(); document.body.focus(); true");
  const sequence = [];
  for (let index = 0; index < 12; index += 1) {
    await call("Input.dispatchKeyEvent", { type: "keyDown", key: "Tab", code: "Tab", windowsVirtualKeyCode: 9 });
    await call("Input.dispatchKeyEvent", { type: "keyUp", key: "Tab", code: "Tab", windowsVirtualKeyCode: 9 });
    sequence.push(await evaluate(call, `(() => {
      const element = document.activeElement;
      const rect = element?.getBoundingClientRect();
      return { tag: element?.tagName, text: element?.textContent?.trim().replace(/\\s+/g, " ").slice(0, 90), id: element?.id, className: element?.className, visible: Boolean(rect && rect.width > 0 && rect.height > 0) };
    })()`));
  }
  return sequence;
}

/** Exercises both compact preference buttons and restores their initial locale and theme. */
async function auditPreferenceCycles(call) {
  await call("Emulation.setDeviceMetricsOverride", { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
  await call("Page.navigate", { url: `${dashboardUrl}#inventory` });
  await settle(80);
  await call("Page.reload", { ignoreCache: true });
  await settle();
  return evaluate(call, `(() => new Promise(async (resolve) => {
    const pause = () => new Promise((done) => setTimeout(done, 80));
    const readLanguage = () => {
      const button = document.querySelector(".language-selector");
      return { locale: button?.dataset.locale, name: button?.getAttribute("aria-label") };
    };
    const readTheme = () => {
      const button = document.querySelector(".theme-selector");
      return { preference: button?.dataset.preference, name: button?.getAttribute("aria-label") };
    };
    const language = [readLanguage()];
    for (let index = 0; index < 2; index += 1) {
      document.querySelector(".language-selector")?.click();
      await pause();
      language.push(readLanguage());
    }
    const theme = [readTheme()];
    for (let index = 0; index < 2; index += 1) {
      document.querySelector(".theme-selector")?.click();
      await pause();
      theme.push(readTheme());
    }
    resolve({ language, theme });
  }))()`);
}

/** Inspects accessible roles and flags unnamed interactive controls. */
async function auditAccessibilityTree(call) {
  const { nodes } = await call("Accessibility.getFullAXTree");
  const interactiveRoles = new Set(["button", "link", "combobox", "textbox", "searchbox"]);
  const exposed = nodes.filter((node) => !node.ignored);
  return {
    exposedNodeCount: exposed.length,
    roles: [...new Set(exposed.map((node) => node.role?.value).filter(Boolean))].sort(),
    unnamedInteractive: exposed
      .filter((node) => interactiveRoles.has(node.role?.value) && !node.name?.value)
      .map((node) => ({ role: node.role?.value, backendDOMNodeId: node.backendDOMNodeId })),
  };
}

/** Exercises every Dashboard destination and authorised zoom under browser forced colours, including navigation and KPI boundary evidence. */
async function auditForcedColours(call) {
  const routes = ["overview", "inventory", "alerts", "performance", "history", "configuration", "providers", "settings"];
  const zoomPercentages = [100, 200, 400];
  const samples = [];
  await call("Emulation.setEmulatedMedia", {
    features: [
      { name: "forced-colors", value: "active" },
      { name: "prefers-reduced-motion", value: "reduce" },
    ],
  });
  await call("Emulation.setDeviceMetricsOverride", { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
  for (const zoomPercent of zoomPercentages) {
    await call("Emulation.setPageScaleFactor", { pageScaleFactor: zoomPercent / 100 });
    for (const route of routes) {
      await call("Page.navigate", { url: `${dashboardUrl}#${route}` });
      await settle(80);
      await call("Page.reload", { ignoreCache: true });
      await settle();
      const surface = await evaluate(call, `(() => {
        const probe = document.createElement("div");
        const selectedProbe = document.createElement("div");
        probe.style.cssText = "position:fixed;left:-10000px;color:CanvasText;background:Canvas;border:1px solid Highlight";
        selectedProbe.style.cssText = "position:fixed;left:-10000px;color:HighlightText;background:Highlight";
        document.body.append(probe, selectedProbe);
        const probeStyle = getComputedStyle(probe);
        const selectedProbeStyle = getComputedStyle(selectedProbe);
        const system = {
          canvasText: probeStyle.color,
          canvas: probeStyle.backgroundColor,
          highlight: selectedProbeStyle.backgroundColor,
          highlightText: selectedProbeStyle.color,
        };
        probe.remove();
        selectedProbe.remove();
        const focusTarget = document.querySelector("button:not(:disabled), a[href], input:not(:disabled), select:not(:disabled)");
        focusTarget?.focus();
        const focusStyle = focusTarget ? getComputedStyle(focusTarget) : null;
        const focusVisible = Boolean(focusStyle && focusStyle.outlineStyle !== "none" && parseFloat(focusStyle.outlineWidth) >= 2);
        const activeNav = document.querySelector(".nav-item.active");
        activeNav?.focus();
        const activeNavStyle = activeNav ? getComputedStyle(activeNav) : null;
        const activeNavLabel = activeNav?.querySelector(".nav-label");
        const activeNavLabelStyle = activeNavLabel ? getComputedStyle(activeNavLabel) : null;
        const activeNavLabelRect = activeNavLabel?.getBoundingClientRect();
        const activeNavCount = activeNav?.querySelector(".nav-count");
        const activeNavCountStyle = activeNavCount ? getComputedStyle(activeNavCount) : null;
        const metricCards = [...document.querySelectorAll(".overview-kpis .summary-card")].map((card) => {
          const style = getComputedStyle(card);
          const boundaryVisible = (width, lineStyle) => parseFloat(width) >= 1 && lineStyle !== "none";
          const boundaries = {
            top: boundaryVisible(style.borderTopWidth, style.borderTopStyle),
            right: boundaryVisible(style.borderRightWidth, style.borderRightStyle),
            bottom: boundaryVisible(style.borderBottomWidth, style.borderBottomStyle),
            left: boundaryVisible(style.borderLeftWidth, style.borderLeftStyle),
          };
          return {
            label: card.querySelector(".summary-label")?.textContent?.trim() ?? null,
            boundaries,
            allBoundariesVisible: Object.values(boundaries).every(Boolean),
          };
        });
        const status = document.querySelector(".status-badge");
        const statusStyle = status ? getComputedStyle(status) : null;
        const root = document.documentElement;
        return {
          route: location.hash.slice(1),
          zoomPercent: ${zoomPercent},
          active: matchMedia("(forced-colors: active)").matches,
          horizontalOverflow: root.scrollWidth > root.clientWidth + 1,
          system,
          bodyUsesSystemCanvas: getComputedStyle(document.body).backgroundColor === system.canvas,
          bodyUsesSystemText: getComputedStyle(document.body).color === system.canvasText,
          activeNavigationUsesHighlight: activeNavStyle?.backgroundColor === system.highlight,
          activeNavigationTextUsesHighlightText: activeNavStyle?.color === system.highlightText,
          activeNavigationResistsRemapping: activeNavStyle?.forcedColorAdjust === "none",
          activeNavigationLabelVisible: Boolean(
            activeNavLabel?.textContent?.trim() &&
            activeNavLabelRect && activeNavLabelRect.width > 0 && activeNavLabelRect.height > 0 &&
            activeNavLabelStyle?.display !== "none" && activeNavLabelStyle?.visibility !== "hidden" &&
            Number(activeNavLabelStyle?.opacity ?? 0) > 0 && activeNavLabelStyle?.color === system.highlightText
          ),
          activeNavigationCountUsesSystemColours: !activeNavCountStyle || (
            activeNavCountStyle.color === system.highlight &&
            activeNavCountStyle.backgroundColor === system.highlightText &&
            activeNavCountStyle.borderTopStyle !== "none" && parseFloat(activeNavCountStyle.borderTopWidth) >= 1
          ),
          activeNavigationText: activeNavStyle?.color ?? null,
          activeNavigationFocusVisible: Boolean(activeNavStyle && activeNavStyle.outlineStyle !== "none" && parseFloat(activeNavStyle.outlineWidth) >= 2),
          focusVisible,
          metricCards,
          statusBoundaryVisible: statusStyle ? statusStyle.borderTopStyle !== "none" && parseFloat(statusStyle.borderTopWidth) >= 1 : true,
          mainName: document.querySelector("main")?.getAttribute("aria-label") ?? document.querySelector("h1")?.textContent?.trim() ?? null,
          currentNavigationCount: document.querySelectorAll('[aria-current="page"]').length,
        };
      })()`);
      surface.performanceChart = await auditPerformanceChart(call);
      surface.overviewStatus = route === "overview" ? await auditOverviewStatusLayout(call) : null;
      const tree = await auditAccessibilityTree(call);
      samples.push({ ...surface, accessibilityTree: tree });
      if (route === "overview" && zoomPercent === 100) {
        const screenshot = await call("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
        writeFileSync(join(evidenceDirectory, "forced-colours-overview-1440x1000.png"), Buffer.from(screenshot.data, "base64"));
      }
    }
  }
  await call("Emulation.setPageScaleFactor", { pageScaleFactor: 1 });
  await call("Emulation.setEmulatedMedia", { features: [] });
  return samples;
}

/** Records focal forced-colour status geometry at desktop and compact physical widths with zoom-equivalent CSS viewports. */
async function auditForcedColourOverviewStatus(call) {
  const samples = [];
  const focalViewports = [
    { width: 1180, height: 760, zoomPercent: 100 },
    { width: 820, height: 620, zoomPercent: 100 },
    { width: 1180, height: 760, zoomPercent: 200 },
    { width: 820, height: 620, zoomPercent: 200 },
    { width: 1280, height: 900, zoomPercent: 400 },
    { width: 1440, height: 1000, zoomPercent: 400 },
  ];
  await call("Emulation.setEmulatedMedia", {
    features: [
      { name: "forced-colors", value: "active" },
      { name: "prefers-reduced-motion", value: "reduce" },
    ],
  });
  for (const dimensions of focalViewports) {
    const { zoomPercent } = dimensions;
    const zoomFactor = zoomPercent / 100;
    await call("Emulation.setPageScaleFactor", { pageScaleFactor: 1 });
    const layoutWidth = Math.floor(dimensions.width / zoomFactor);
    const layoutHeight = Math.floor(dimensions.height / zoomFactor);
    await call("Emulation.setDeviceMetricsOverride", { width: layoutWidth, height: layoutHeight, deviceScaleFactor: zoomFactor, mobile: false });
    await call("Page.navigate", { url: `${dashboardUrl}#overview` });
    await waitForPage(call, 'document.readyState === "complete" && location.hash === "#overview"', "forced-colour Overview navigation");
    await call("Page.reload", { ignoreCache: true });
    await waitForPage(call, `document.readyState === "complete" && innerWidth === ${layoutWidth} && document.querySelectorAll(".overview-instance-row").length > 0`, "forced-colour Overview status layout");
    await evaluate(call, "new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve(true))))");
    const forcedColourPresentation = await evaluate(call, `(() => {
        const probe = document.createElement("div");
        probe.style.cssText = "position:fixed;left:-10000px;color:CanvasText;background:Canvas";
        document.body.append(probe);
        const probeStyle = getComputedStyle(probe);
        const system = { canvasText: probeStyle.color, canvas: probeStyle.backgroundColor };
        probe.remove();
        const boundaryVisible = (width, lineStyle) => parseFloat(width) >= 1 && lineStyle !== "none";
        const statuses = [...document.querySelectorAll(".overview-instance-row .status-badge")].map((status) => {
          const style = getComputedStyle(status);
          const boundaries = {
            top: boundaryVisible(style.borderTopWidth, style.borderTopStyle),
            right: boundaryVisible(style.borderRightWidth, style.borderRightStyle),
            bottom: boundaryVisible(style.borderBottomWidth, style.borderBottomStyle),
            left: boundaryVisible(style.borderLeftWidth, style.borderLeftStyle),
          };
          return {
            boundaries,
            allBoundariesVisible: Object.values(boundaries).every(Boolean),
            usesSystemText: style.color === system.canvasText,
            usesSystemCanvas: style.backgroundColor === system.canvas,
          };
        });
        return {
          active: matchMedia("(forced-colors: active)").matches,
          horizontalOverflow: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
          system,
          statusCount: statuses.length,
          allStatusBoundariesVisible: statuses.length > 0 && statuses.every((status) => status.allBoundariesVisible),
          allStatusesUseSystemColours: statuses.length > 0 && statuses.every((status) => status.usesSystemText && status.usesSystemCanvas),
          statuses,
        };
    })()`);
    samples.push({
      ...dimensions,
      zoomFactor,
      layoutWidth,
      layoutHeight,
      innerWidth: await evaluate(call, "innerWidth"),
      forcedColourPresentation,
      statusLayout: await auditOverviewStatusLayout(call),
    });
  }
  await call("Emulation.setPageScaleFactor", { pageScaleFactor: 1 });
  await call("Emulation.setEmulatedMedia", { features: [] });
  await call("Emulation.setDeviceMetricsOverride", { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
  return samples;
}

/** Exercises each declared operational state and verifies reduced-motion rendering. */
async function auditOperationalStates(call) {
  await call("Page.navigate", { url: `${dashboardUrl}#inventory` });
  await settle(80);
  await call("Page.reload", { ignoreCache: true });
  await settle();
  const states = {};
  for (const state of ["loading", "empty", "offline", "error", "denied", "maintenance"]) {
    states[state] = await evaluate(call, `(() => {
      const select = document.querySelector(".scenario-control select");
      const setter = Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, "value").set;
      setter.call(select, ${JSON.stringify(state)});
      select.dispatchEvent(new Event("change", { bubbles: true }));
      return new Promise((resolve) => setTimeout(() => {
        const surface = document.querySelector(".operational-state");
        resolve({
          title: surface?.querySelector("h2")?.textContent,
          role: surface?.getAttribute("role"),
          ariaBusy: surface?.getAttribute("aria-busy"),
          retryVisible: Boolean(surface?.querySelector("button")),
        });
      }, 80));
    })()`);
  }

  await call("Emulation.setEmulatedMedia", { features: [{ name: "prefers-reduced-motion", value: "reduce" }] });
  const reducedMotionAnimation = await evaluate(call, `(() => {
    const select = document.querySelector(".scenario-control select");
    const setter = Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, "value").set;
    setter.call(select, "loading");
    select.dispatchEvent(new Event("change", { bubbles: true }));
    return new Promise((resolve) => setTimeout(() => resolve(getComputedStyle(document.querySelector(".loading-line"), "::after").animationName), 80));
  })()`);
  await call("Emulation.setEmulatedMedia", { features: [] });
  return { states, reducedMotionAnimation };
}

/** Exercises the confirmation dialogue and checks focus containment and Escape behaviour. */
async function auditModal(call) {
  await call("Emulation.setDeviceMetricsOverride", { width: 390, height: 844, deviceScaleFactor: 1, mobile: false });
  await call("Page.navigate", { url: `${dashboardUrl}#configuration` });
  await settle(80);
  await call("Page.reload", { ignoreCache: true });
  await settle();
  await evaluate(call, `(() => {
    const select = document.querySelector(".scenario-control select");
    const setter = Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, "value").set;
    setter.call(select, "ready");
    select.dispatchEvent(new Event("change", { bubbles: true }));
    return true;
  })()`);
  await settle(100);
  await evaluate(call, `(() => {
    const button = document.querySelector(".confirmation-example button");
    button?.click();
    return Boolean(button);
  })()`);
  await settle(100);

  const initial = await evaluate(call, `(() => {
    const dialog = document.querySelector('[role="dialog"]');
    return { open: Boolean(dialog), focusInside: Boolean(dialog?.contains(document.activeElement)), activeText: document.activeElement?.textContent?.trim() };
  })()`);
  const screenshot = await call("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
  const screenshotPath = join(evidenceDirectory, "configuration-modal-mobile-390x844.png");
  writeFileSync(screenshotPath, Buffer.from(screenshot.data, "base64"));
  const tabSequence = [];
  for (let index = 0; index < 4; index += 1) {
    await call("Input.dispatchKeyEvent", { type: "keyDown", key: "Tab", code: "Tab", windowsVirtualKeyCode: 9 });
    await call("Input.dispatchKeyEvent", { type: "keyUp", key: "Tab", code: "Tab", windowsVirtualKeyCode: 9 });
    tabSequence.push(await evaluate(call, `(() => {
      const dialog = document.querySelector('[role="dialog"]');
      return { focusInside: Boolean(dialog?.contains(document.activeElement)), activeText: document.activeElement?.textContent?.trim().replace(/\\s+/g, " ").slice(0, 90) };
    })()`));
  }
  await call("Input.dispatchKeyEvent", { type: "keyDown", key: "Tab", code: "Tab", modifiers: 8, windowsVirtualKeyCode: 9 });
  await call("Input.dispatchKeyEvent", { type: "keyUp", key: "Tab", code: "Tab", modifiers: 8, windowsVirtualKeyCode: 9 });
  const reverseTabInside = await evaluate(call, "Boolean(document.querySelector('[role=dialog]')?.contains(document.activeElement))");
  await call("Input.dispatchKeyEvent", { type: "keyDown", key: "Escape", code: "Escape", windowsVirtualKeyCode: 27 });
  await call("Input.dispatchKeyEvent", { type: "keyUp", key: "Escape", code: "Escape", windowsVirtualKeyCode: 27 });
  const afterEscape = await evaluate(call, `(() => ({
    closesWithEscape: !document.querySelector('[role="dialog"]'),
    focusRestored: document.activeElement === document.querySelector(".confirmation-example button"),
    activeText: document.activeElement?.textContent?.trim(),
  }))()`);
  return { initial, tabSequence, reverseTabInside, screenshotPath, ...afterEscape };
}

/** Runs the complete browser audit and writes a machine-readable evidence bundle. */
async function main() {
  const { socket, call } = await connect();
  await call("Page.enable");
  await call("Runtime.enable");
  await call("Accessibility.enable");
  await call("Page.navigate", { url: dashboardUrl });
  await settle();
  await evaluate(call, `
    localStorage.setItem("dbnotifier.language.preference.v1", ${JSON.stringify(locale)});
    localStorage.setItem("dbnotifier.theme.preference.v1", ${JSON.stringify(theme)});
    true
  `);
  await call("Page.reload", { ignoreCache: true });
  await settle();

  const viewports = [];
  for (const [name, width, height, hash = "inventory", pageScaleFactor = 1] of [
    ["overview-ultrawide-1920x1080", 1920, 1080, "overview"],
    ["overview-desktop-1440x1000", 1440, 1000, "overview"],
    ["overview-human-review-1180x760", 1180, 760, "overview"],
    ["overview-compact-820x620", 820, 620, "overview"],
    ["overview-200-percent-reflow-equivalent-640x800", 640, 800, "overview"],
    ["overview-200-percent-browser-zoom-1280x900", 1280, 900, "overview", 2],
    ["overview-400-percent-browser-zoom-1280x900", 1280, 900, "overview", 4],
    ["overview-mobile-390x844", 390, 844, "overview"],
    ["overview-minimum-320x568", 320, 568, "overview"],
    ["inventory-ultrawide-1920x1080", 1920, 1080],
    ["inventory-desktop-1440x1000", 1440, 1000],
    ["inventory-narrow-desktop-960x1040", 960, 1040],
    ["inventory-laptop-1024x768", 1024, 768],
    ["inventory-tablet-768x1024", 768, 1024],
    ["inventory-mobile-390x844", 390, 844],
    ["inventory-minimum-320x568", 320, 568],
    ["inventory-200-percent-reflow-equivalent-640", 640, 800],
    ["history-desktop-1440x1000", 1440, 1000, "history"],
    ["alerts-narrow-desktop-960x1040", 960, 1040, "alerts"],
    ["alerts-mobile-390x844", 390, 844, "alerts"],
    ["alerts-minimum-320x568", 320, 568, "alerts"],
    ["performance-desktop-1440x1000", 1440, 1000, "performance"],
    ["performance-200-percent-reflow-equivalent-640x800", 640, 800, "performance"],
    ["performance-200-percent-browser-zoom-1280x900", 1280, 900, "performance", 2],
    ["performance-400-percent-browser-zoom-1280x900", 1280, 900, "performance", 4],
    ["performance-minimum-320x568", 320, 568, "performance"],
    ["configuration-mobile-390x844", 390, 844, "configuration"],
    ["configuration-minimum-320x568", 320, 568, "configuration"],
    ["providers-desktop-1440x1000", 1440, 1000, "providers"],
    ["providers-minimum-320x568", 320, 568, "providers"],
    ["settings-desktop-1440x1000", 1440, 1000, "settings"],
    ["settings-minimum-320x568", 320, 568, "settings"],
  ]) viewports.push(await captureViewport(call, name, width, height, hash, pageScaleFactor));

  await call("Page.navigate", { url: `${dashboardUrl}#history` });
  await settle(80);
  await call("Page.reload", { ignoreCache: true });
  await settle();
  const duplicateHistorySearchRegions = await evaluate(call, "document.querySelectorAll('[role=search]').length");
  const report = {
    generatedAt: new Date().toISOString(),
    browser: { product: browserProduct, version: browserVersion },
    locale,
    theme,
    viewports,
    tvMode: await auditTvMode(call),
    keyboard: await auditKeyboard(call),
    preferenceCycles: await auditPreferenceCycles(call),
    semanticBrand: await auditSemanticBrand(call),
    accessibilityTree: await auditAccessibilityTree(call),
    forcedColours: await auditForcedColours(call),
    forcedColourOverviewStatus: await auditForcedColourOverviewStatus(call),
    operationalStates: await auditOperationalStates(call),
    modal: await auditModal(call),
    duplicateHistorySearchRegions,
  };
  const reportPath = join(evidenceDirectory, "dashboard-audit.json");
  writeFileSync(reportPath, `${JSON.stringify(report, null, 2)}\n`);
  socket.close();
  process.stdout.write(`${JSON.stringify({ reportPath, ...report }, null, 2)}\n`);
}

await main();
