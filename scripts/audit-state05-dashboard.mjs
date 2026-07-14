/**
 * Audits the built STATE-05 Dashboard through Chrome DevTools Protocol.
 * The script records viewport, accessibility-tree, keyboard and modal-focus evidence without mutating product state.
 */
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { tmpdir } from "node:os";

const endpoint = process.env.DBNOTIFIER_AUDIT_CDP_ENDPOINT ?? "http://127.0.0.1:9224";
const dashboardUrl = "http://127.0.0.1:4173/";
const requestedLocale = process.env.DBNOTIFIER_AUDIT_LOCALE;
const locale = requestedLocale === "en-GB" ? "en-GB" : "pt-BR";
const requestedTheme = process.env.DBNOTIFIER_AUDIT_THEME;
const theme = requestedTheme === "dark" ? "dark" : "light";
const evidenceDirectory = join(tmpdir(), "DBNotifier-State05-Audit", locale, theme);
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
    const { resolve, reject } = pending.get(message.id);
    pending.delete(message.id);
    if (message.error) reject(new Error(message.error.message));
    else resolve(message.result);
  });

  /** Sends one protocol command and resolves only after Chrome returns its result. */
  function call(method, params = {}) {
    const id = ++sequence;
    socket.send(JSON.stringify({ id, method, params }));
    return new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
  }

  return { socket, call };
}

/** Waits for React and layout work to settle after navigation or emulation changes. */
function settle(milliseconds = 350) {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}

/** Evaluates a serialisable expression in the page and returns its value. */
async function evaluate(call, expression) {
  const result = await call("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true });
  if (result.exceptionDetails) throw new Error(result.exceptionDetails.text);
  return result.result.value;
}

/** Captures layout and screenshot evidence for one route and viewport. */
async function captureViewport(call, name, width, height, hash = "inventory", pageScaleFactor = 1) {
  await call("Emulation.setDeviceMetricsOverride", { width, height, deviceScaleFactor: 1, mobile: false });
  await call("Emulation.setPageScaleFactor", { pageScaleFactor });
  await call("Page.navigate", { url: `${dashboardUrl}#${hash}` });
  await settle(80);
  await call("Page.reload", { ignoreCache: true });
  await settle();
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
      offenders,
      heading: document.querySelector("h1")?.textContent?.trim(),
    };
  })()`);
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

/** Exercises the dedicated TV layout, native Fullscreen request and persistent visible exit control. */
async function auditTvMode(call) {
  await call("Emulation.setDeviceMetricsOverride", { width: 1920, height: 1080, deviceScaleFactor: 1, mobile: false });
  await call("Page.navigate", { url: `${dashboardUrl}#inventory` });
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
    filtersDisplay: getComputedStyle(document.querySelector(".inventory-panel .filters")).display,
    tableDisplay: getComputedStyle(document.querySelector(".table-wrap")).display,
    demoDisplay: getComputedStyle(document.querySelector(".demo-badge")).display,
    languageDisplay: getComputedStyle(document.querySelector(".language-selector")).display,
    themeDisplay: getComputedStyle(document.querySelector(".theme-selector")).display,
    systemTimeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    clockText: document.querySelector(".tv-mode-status time")?.textContent?.trim(),
    statusText: document.querySelector(".tv-mode-status")?.textContent?.trim().replace(/\s+/g, " "),
  }))()`);
  const screenshot = await call("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
  const screenshotPath = join(evidenceDirectory, "tv-mode-inventory-1920x1080.png");
  writeFileSync(screenshotPath, Buffer.from(screenshot.data, "base64"));
  await clickElement(call, ".tv-mode-button");
  await settle(250);
  const restored = await evaluate(call, `(() => ({
    tvMode: document.documentElement.dataset.tvMode ?? null,
    nativeFullscreen: Boolean(document.fullscreenElement),
    buttonState: document.querySelector(".tv-mode-button")?.dataset.tvModeControl,
    sidebarDisplay: getComputedStyle(document.querySelector(".sidebar")).display,
  }))()`);
  await evaluate(call, `(() => {
    Object.defineProperty(document.documentElement, "requestFullscreen", { value: undefined, configurable: true });
    return true;
  })()`);
  await clickElement(call, ".tv-mode-button");
  await settle(150);
  const unavailableFullscreen = await evaluate(call, `(() => ({
    tvMode: document.documentElement.dataset.tvMode,
    nativeFullscreen: Boolean(document.fullscreenElement),
    buttonState: document.querySelector(".tv-mode-button")?.dataset.tvModeControl,
    announcement: document.querySelector(".tv-mode-button + [role=status]")?.textContent,
  }))()`);
  await clickElement(call, ".tv-mode-button");
  await settle(100);
  await call("Page.reload", { ignoreCache: true });
  await settle();
  return { active, restored, unavailableFullscreen, screenshotPath };
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
    ["configuration-mobile-390x844", 390, 844, "configuration"],
    ["configuration-minimum-320x568", 320, 568, "configuration"],
  ]) viewports.push(await captureViewport(call, name, width, height, hash, pageScaleFactor));

  await call("Page.navigate", { url: `${dashboardUrl}#history` });
  await settle(80);
  await call("Page.reload", { ignoreCache: true });
  await settle();
  const duplicateHistorySearchRegions = await evaluate(call, "document.querySelectorAll('[role=search]').length");
  const report = {
    generatedAt: new Date().toISOString(),
    locale,
    theme,
    viewports,
    tvMode: await auditTvMode(call),
    keyboard: await auditKeyboard(call),
    preferenceCycles: await auditPreferenceCycles(call),
    accessibilityTree: await auditAccessibilityTree(call),
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
