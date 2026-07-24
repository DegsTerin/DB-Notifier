/**
 * Module purpose: Audits the dedicated O4 Observer UI through Chromium CDP without external requests or retained images.
 * The runner checks factual copy, API denial, accessibility, themes, locales, forced colours and bounded reflow.
 */

const cdpEndpoint = requireLoopbackUrl("DBNOTIFIER_O4_CDP_ENDPOINT", "http:");
const observerUrl = requireLoopbackUrl("DBNOTIFIER_O4_URL", "https:");

/** Reads one required local endpoint without accepting an external origin. */
function requireLoopbackUrl(name, protocol) {
  const value = new URL(process.env[name] ?? "");
  if (value.protocol !== protocol || !["127.0.0.1", "localhost", "[::1]"].includes(value.hostname)) {
    throw new Error(`${name} must use ${protocol}// on loopback.`);
  }
  return value.toString().replace(/\/$/u, "");
}

/** Throws a bounded assertion without serialising page or API content. */
function assertEvidence(condition, message) {
  if (!condition) throw new Error(message);
}

/** Opens the dedicated page target and returns one correlated CDP command function. */
async function connect() {
  const targets = await fetch(`${cdpEndpoint}/json/list`).then((response) => response.json());
  const target = targets.find((candidate) =>
    candidate.type === "page" && candidate.url.startsWith(observerUrl));
  if (!target?.webSocketDebuggerUrl) throw new Error("The dedicated O4 page target was not found.");
  const socket = new WebSocket(target.webSocketDebuggerUrl);
  const pending = new Map();
  const network = { requests: 0, externalOrigins: new Set() };
  let nextId = 0;
  await new Promise((resolve, reject) => {
    socket.addEventListener("open", resolve, { once: true });
    socket.addEventListener("error", reject, { once: true });
  });
  socket.addEventListener("message", (event) => {
    const message = JSON.parse(event.data);
    if (!message.id) {
      if (message.method === "Network.requestWillBeSent") {
        const request = new URL(message.params.request.url);
        if (request.protocol === "http:" || request.protocol === "https:") {
          network.requests += 1;
          if (!["127.0.0.1", "localhost", "[::1]"].includes(request.hostname)) {
            network.externalOrigins.add(request.origin);
          }
        }
      }
      return;
    }
    const waiter = pending.get(message.id);
    if (!waiter) return;
    pending.delete(message.id);
    if (message.error) waiter.reject(new Error(message.error.message));
    else waiter.resolve(message.result);
  });
  const call = (method, params = {}) => new Promise((resolve, reject) => {
    const id = ++nextId;
    pending.set(id, { resolve, reject });
    socket.send(JSON.stringify({ id, method, params }));
  });
  return { call, network, close: () => socket.close() };
}

/** Evaluates one serialisable browser expression. */
async function evaluate(call, expression) {
  const result = await call("Runtime.evaluate", {
    expression,
    returnByValue: true,
    awaitPromise: true,
  });
  if (result.exceptionDetails) throw new Error(result.exceptionDetails.text);
  return result.result.value;
}

/** Waits boundedly for one page predicate. */
async function waitFor(call, predicate, description) {
  const deadline = Date.now() + 10_000;
  while (Date.now() < deadline) {
    if (await evaluate(call, `Boolean(${predicate})`)) return;
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
  throw new Error(`Timed out waiting for ${description}.`);
}

/** Reads sanitised layout facts without returning user or projection identifiers. */
async function layoutEvidence(call) {
  return evaluate(call, `(() => {
    const root = document.documentElement;
    const main = document.querySelector(".observer-main");
    const grid = document.querySelector(".observer-grid");
    const panels = [...document.querySelectorAll(".observer-panel")];
    const panelLefts = [...new Set(panels.map((panel) =>
      Math.round(panel.getBoundingClientRect().left)))];
    const traceDetails = [...document.querySelectorAll(".observer-trace details")];
    const allContained = panels.every((panel) => {
      const rect = panel.getBoundingClientRect();
      return rect.width > 0 && rect.height > 0 && panel.scrollWidth <= panel.clientWidth + 1;
    });
    const visibleText = (selector) => {
      const element = document.querySelector(selector);
      if (!element) return false;
      const style = getComputedStyle(element);
      const rect = element.getBoundingClientRect();
      return rect.width > 0 && rect.height > 0 && style.visibility !== "hidden" && style.display !== "none";
    };
    return {
      theme: root.dataset.theme,
      language: root.lang,
      panelCount: panels.length,
      summaryCount: document.querySelectorAll(".observer-summary-card").length,
      signalCount: document.querySelectorAll(".observer-signal-card").length,
      traceCount: document.querySelectorAll(".observer-trace dd").length,
      fullTraceCount: traceDetails.length,
      fullTraceAvailable: traceDetails.every((item) =>
        (item.querySelector("code")?.textContent?.length ?? 0) === 64),
      limitationCount: document.querySelectorAll(".observer-limitation-list li").length,
      panelColumnCount: panelLefts.length,
      gridVisible: Boolean(grid && grid.getBoundingClientRect().width > 0),
      horizontalOverflow: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
      mainOverflow: Boolean(main && main.scrollWidth > main.clientWidth + 1),
      allContained,
      stateVisible: visibleText(".observer-state-pill"),
      activationVisible: visibleText(".observer-activation"),
      syntheticVisible: /sintétic|synthetic/iu.test(document.body.innerText),
      noneVisible: document.body.innerText.includes("ActivationState=None"),
      unknownForecastVisible: document.body.innerText.includes("Desconhecida") || document.body.innerText.includes("Unknown"),
      prohibitedActionText: /\\b(recommend|recomend|execut|command|comando)\\b/iu.test(
        [...document.querySelectorAll("button")].map((item) => item.textContent ?? "").join(" ")),
    };
  })()`);
}

const session = await connect();
const { call, network } = session;
let screenshotCount = 0;
try {
  await call("Runtime.enable");
  await call("Page.enable");
  await call("Network.enable");
  await call("Accessibility.enable");
  try {
    await waitFor(call, 'document.querySelector(".observer-grid")', "validated Observer projection");
  } catch (error) {
    const diagnostic = await evaluate(call, `(async () => {
      const response = await fetch("/api/v1/observer-sandbox/projection", {
        method: "GET",
        cache: "no-store",
        headers: { "X-DBN-O4-Test-Subject": "o4-local-observer-review" },
      });
      const body = await response.json().catch(() => null);
      return {
        title: document.title,
        observerState: Boolean(document.querySelector(".observer-state")),
        unavailable: document.body.innerText.includes("indisponível") || document.body.innerText.includes("unavailable"),
        origin: location.origin,
        apiStatus: response.status,
        contentType: response.headers.get("Content-Type"),
        rootKeys: body && typeof body === "object" ? Object.keys(body) : [],
        payloadKeys: body?.payload && typeof body.payload === "object" ? Object.keys(body.payload) : [],
        generatedAtUtc: body?.payload?.generatedAtUtc ?? null,
        validUntilUtc: body?.payload?.validUntilUtc ?? null,
        observedAtUtc: body?.payload?.signals?.[0]?.observedAtUtc ?? null,
        signal: body?.payload?.signals?.[0] ? {
          signalIdLength: body.payload.signals[0].signalId?.length ?? null,
          disposition: body.payload.signals[0].disposition ?? null,
          freshness: body.payload.signals[0].freshness ?? null,
          evidenceCount: body.payload.signals[0].evidenceIds?.length ?? null,
          evidenceShapes: body.payload.signals[0].evidenceIds?.map((value) =>
            /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/iu.test(value)) ?? [],
          limitationCount: body.payload.signals[0].limitations?.length ?? null,
        } : null,
        traceLengths: body?.payload?.trace && typeof body.payload.trace === "object"
          ? Object.fromEntries(Object.entries(body.payload.trace).map(([key, value]) =>
            [key, typeof value === "string" ? value.length : value]))
          : {},
        forecastState: body?.payload?.forecasts?.state ?? null,
        forecastCode: body?.payload?.forecasts?.code ?? null,
        forecastLimitations: body?.payload?.forecasts?.limitations ?? [],
        projectionLimitations: body?.payload?.limitations ?? [],
        nowUtc: new Date().toISOString(),
      };
    })()`);
    console.error(JSON.stringify({ diagnostic }));
    throw error;
  }

  const deniedStatus = await evaluate(call, `fetch("/api/v1/observer-sandbox/projection", {
    method: "GET", cache: "no-store",
  }).then((response) => response.status)`);
  const writeStatus = await evaluate(call, `fetch("/api/v1/observer-sandbox/projection", {
    method: "POST",
    headers: { "X-DBN-O4-Test-Subject": "o4-local-observer-review" },
  }).then((response) => response.status)`);
  assertEvidence(deniedStatus === 401, "The projection API accepted an unauthenticated read.");
  assertEvidence(
    writeStatus === 404 || writeStatus === 405,
    "The projection API exposed a write method.",
  );

  const matrix = [
    { width: 1440, height: 900, scale: 1 },
    { width: 1180, height: 760, scale: 1 },
    { width: 1024, height: 768, scale: 1 },
    { width: 820, height: 620, scale: 1 },
    { width: 390, height: 844, scale: 1 },
    { width: 1440, height: 900, scale: 2 },
    { width: 1440, height: 900, scale: 4 },
  ];
  for (const locale of ["pt-BR", "en-GB"]) {
    for (const theme of ["light", "dark"]) {
      await evaluate(call, `localStorage.setItem("dbnotifier.language.preference.v1", ${JSON.stringify(locale)});
        localStorage.setItem("dbnotifier.theme.preference.v1", ${JSON.stringify(theme)});
        location.reload(); true`);
      await waitFor(call, 'document.querySelector(".observer-grid")', `${locale}/${theme} projection`);
      for (const sample of matrix) {
        await call("Emulation.setDeviceMetricsOverride", {
          width: Math.floor(sample.width / sample.scale),
          height: Math.floor(sample.height / sample.scale),
          deviceScaleFactor: 1,
          mobile: false,
        });
        const evidence = await layoutEvidence(call);
        const effectiveWidth = Math.floor(sample.width / sample.scale);
        assertEvidence(evidence.panelCount === 4, "A factual Observer panel is missing.");
        assertEvidence(evidence.summaryCount === 3, "The factual Observer summary is incomplete.");
        assertEvidence(evidence.signalCount >= 1, "No complete signal is visible.");
        assertEvidence(evidence.traceCount === 4, "Result-policy-corpus traceability is incomplete.");
        assertEvidence(
          evidence.fullTraceCount === 3 && evidence.fullTraceAvailable,
          "Compact technical identifiers do not retain their complete trace values.",
        );
        assertEvidence(evidence.limitationCount >= 5, "Uncertainty or limitations are incomplete.");
        assertEvidence(evidence.gridVisible, "The organised Observer panel grid is hidden.");
        assertEvidence(
          effectiveWidth <= 1120
            ? evidence.panelColumnCount === 1
            : evidence.panelColumnCount === 2,
          "Observer panels did not use the expected balanced or stacked composition.",
        );
        assertEvidence(!evidence.horizontalOverflow && !evidence.mainOverflow, "Observer page overflowed horizontally.");
        assertEvidence(evidence.allContained, "Observer panel content is clipped.");
        assertEvidence(evidence.stateVisible && evidence.activationVisible, "Freshness or activation truth is hidden.");
        assertEvidence(evidence.syntheticVisible && evidence.noneVisible, "Synthetic or inactive truth is hidden.");
        assertEvidence(evidence.unknownForecastVisible, "Forecast uncertainty is hidden.");
        assertEvidence(!evidence.prohibitedActionText, "The Observer surface exposed an action control.");
        const screenshot = await call("Page.captureScreenshot", { format: "png", fromSurface: true });
        assertEvidence(typeof screenshot.data === "string" && screenshot.data.length > 100, "Sanitised capture failed.");
        screenshotCount += 1;
      }
    }
  }

  await call("Emulation.setEmulatedMedia", {
    media: "screen",
    features: [{ name: "forced-colors", value: "active" }],
  });
  const forcedColours = await evaluate(call, `(() => {
    const panel = document.querySelector(".observer-panel");
    const pill = document.querySelector(".observer-state-pill");
    return {
      active: matchMedia("(forced-colors: active)").matches,
      panelBorder: panel ? getComputedStyle(panel).borderStyle : "none",
      pillBorder: pill ? getComputedStyle(pill).borderStyle : "none",
    };
  })()`);
  assertEvidence(
    forcedColours.active &&
      forcedColours.panelBorder !== "none" &&
      forcedColours.pillBorder !== "none",
    "Forced-colour boundaries were not retained.",
  );

  const accessibility = await call("Accessibility.getFullAXTree");
  const roles = accessibility.nodes.map((node) => node.role?.value).filter(Boolean);
  const names = accessibility.nodes.map((node) => node.name?.value).filter(Boolean);
  assertEvidence(roles.includes("main") && roles.includes("heading"), "Required semantic regions are missing.");
  assertEvidence(names.includes("Observer"), "The Observer heading is absent from the accessibility tree.");
  await call("Input.dispatchKeyEvent", { type: "keyDown", key: "Tab", code: "Tab", windowsVirtualKeyCode: 9 });
  await call("Input.dispatchKeyEvent", { type: "keyUp", key: "Tab", code: "Tab", windowsVirtualKeyCode: 9 });
  const focusVisible = await evaluate(call, `Boolean(document.activeElement && document.activeElement !== document.body)`);
  assertEvidence(focusVisible, "Keyboard focus did not enter the interface.");
  assertEvidence(network.externalOrigins.size === 0, "The O4 browser contacted an external origin.");

  console.log(JSON.stringify({
    result: "approved",
    matrixSamples: 28,
    screenshotsSanitisedInMemory: screenshotCount,
    forcedColours: "modelled",
    accessibilityTree: "verified",
    authenticatedReadDeniedWithoutSubject: true,
    writeMethodDenied: true,
    externalOrigins: 0,
    activationState: "None",
  }));
} finally {
  session.close();
}
