/**
 * Module purpose: Audits the exact local Dashboard TV browser composition through Chromium DevTools Protocol.
 * The audit uses only loopback, accelerates selected timers after one real 30-second cadence proof, and emits sanitised summary evidence.
 */

const cdpEndpoint = requireLoopbackUrl("DBNOTIFIER_DASHBOARD_TV_BROWSER_CDP_ENDPOINT", "http:");
const dashboardUrl = requireLoopbackUrl("DBNOTIFIER_DASHBOARD_TV_BROWSER_URL", "https:");
const scenarioHeader = "X-DBN-TV-Browser-Scenario";
const evidenceSubjectHeader = "X-DBN-TV-Test-Human";
const evidenceSubject = "dashboard-tv-local-test";
const acceleratedStorageKey = "dbnotifier.browser-e2e.accelerate-timers";
let navigationSequence = 0;

/** Reads one required loopback URL without allowing an external host or unexpected protocol. */
function requireLoopbackUrl(name, protocol) {
  const raw = process.env[name];
  if (!raw) throw new Error(`${name} is required.`);
  const value = new URL(raw);
  if (value.protocol !== protocol || !["127.0.0.1", "localhost", "[::1]"].includes(value.hostname)) {
    throw new Error(`${name} must use ${protocol}// on loopback.`);
  }
  return value.toString().replace(/\/$/, "");
}

/** Throws one bounded assertion error when browser evidence does not match the contract. */
function assertEvidence(condition, message) {
  if (!condition) throw new Error(message);
}

/** Emits one sanitised stage marker so a bounded local run can be diagnosed without response payloads. */
function reportStage(message) {
  console.error(`[dashboard-tv-browser-e2e] ${message}`);
}

/** Opens one CDP WebSocket and returns a correlated command function plus deterministic cleanup. */
async function connectToPage() {
  const targets = await fetch(`${cdpEndpoint}/json/list`).then((response) => response.json());
  const target = targets.find((candidate) => candidate.type === "page" && candidate.url.startsWith(dashboardUrl));
  if (!target?.webSocketDebuggerUrl) throw new Error("The dedicated Dashboard page was not exposed by CDP.");

  const socket = new WebSocket(target.webSocketDebuggerUrl);
  const pending = new Map();
  const networkEvidence = { observedHttpRequests: 0, observedWebSockets: 0, externalOrigins: new Set() };
  let nextId = 0;
  await new Promise((resolve, reject) => {
    socket.addEventListener("open", resolve, { once: true });
    socket.addEventListener("error", reject, { once: true });
  });
  socket.addEventListener("message", (event) => {
    const message = JSON.parse(event.data);
    if (!message.id) {
      if (message.method === "Network.requestWillBeSent") {
        const requestUrl = new URL(message.params.request.url);
        if (requestUrl.protocol === "http:" || requestUrl.protocol === "https:") {
          networkEvidence.observedHttpRequests += 1;
          if (!["127.0.0.1", "localhost", "[::1]"].includes(requestUrl.hostname)) {
            networkEvidence.externalOrigins.add(requestUrl.origin);
          }
        }
      } else if (message.method === "Network.webSocketCreated") {
        const socketUrl = new URL(message.params.url);
        networkEvidence.observedWebSockets += 1;
        if (!['127.0.0.1', 'localhost', '[::1]'].includes(socketUrl.hostname)) {
          networkEvidence.externalOrigins.add(socketUrl.origin);
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

  /** Sends one CDP command and resolves only its correlated response. */
  const call = (method, params = {}) => new Promise((resolve, reject) => {
    const id = ++nextId;
    pending.set(id, { resolve, reject });
    socket.send(JSON.stringify({ id, method, params }));
  });
  return { call, networkEvidence, close: () => socket.close() };
}

/** Evaluates a serialisable browser expression and returns its value. */
async function evaluate(call, expression) {
  const result = await call("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true });
  if (result.exceptionDetails) throw new Error(result.exceptionDetails.exception?.description ?? result.exceptionDetails.text);
  return result.result.value;
}

/** Waits for one browser predicate with a bounded timeout and navigation-tolerant polling. */
async function waitFor(call, predicate, description, timeoutMilliseconds = 8_000) {
  const deadline = Date.now() + timeoutMilliseconds;
  while (Date.now() < deadline) {
    try {
      if (await evaluate(call, `Boolean(${predicate})`)) return;
    } catch {
      // A navigation can replace the execution context between two bounded polls.
    }
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
  throw new Error(`Timed out waiting for ${description}.`);
}

/** Reads the current factual TV surface without capturing credentials or response bodies. */
async function readView(call) {
  return evaluate(call, `(() => ({
    tvActive: document.documentElement.dataset.tvMode === "true",
    control: document.querySelector(".tv-mode-button")?.dataset.tvModeControl ?? null,
    badge: document.querySelector(".demo-badge")?.textContent?.trim() ?? null,
    instanceNames: [...document.querySelectorAll(".overview-instance-name strong")].map((element) => element.textContent?.trim()),
    timerDelays: [...(window.__dbNotifierBrowserE2eTimerDelays ?? [])],
  }))()`);
}

/** Reads sanitised host evidence for one fixed scenario through the same HTTPS loopback origin. */
async function readHostEvidence(call, scenario) {
  return evaluate(call, `fetch(${JSON.stringify(`/__dbnotifier-browser-e2e/evidence?scenario=${encodeURIComponent(scenario)}`)}, {
    headers: { ${JSON.stringify(evidenceSubjectHeader)}: ${JSON.stringify(evidenceSubject)} },
    cache: "no-store",
  }).then(async (response) => {
    if (!response.ok) throw new Error("Evidence endpoint returned " + response.status);
    return response.json();
  })`);
}

/** Reads sanitised aggregate SignalR and synthetic projection evidence from the temporary host. */
async function readSignalREvidence(call) {
  return evaluate(call, `fetch("/__dbnotifier-browser-e2e/signalr-evidence", {
    headers: { ${JSON.stringify(evidenceSubjectHeader)}: ${JSON.stringify(evidenceSubject)} },
    cache: "no-store",
  }).then(async (response) => {
    if (!response.ok) throw new Error("SignalR evidence endpoint returned " + response.status);
    return response.json();
  })`);
}

/** Waits for one aggregate SignalR evidence predicate without exposing connection identifiers. */
async function waitForSignalREvidence(call, predicate, description, timeoutMilliseconds = 8_000) {
  const deadline = Date.now() + timeoutMilliseconds;
  while (Date.now() < deadline) {
    const evidence = await readSignalREvidence(call);
    if (predicate(evidence)) return evidence;
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
  throw new Error(`Timed out waiting for ${description}.`);
}

/** Publishes one fixed synthetic change after the authoritative local projection has committed it. */
async function publishSyntheticHint(call) {
  return evaluate(call, `fetch("/__dbnotifier-browser-e2e/publish-hint", {
    method: "POST",
    headers: { ${JSON.stringify(evidenceSubjectHeader)}: ${JSON.stringify(evidenceSubject)} },
    cache: "no-store",
  }).then(async (response) => {
    if (!response.ok) throw new Error("Synthetic hint endpoint returned " + response.status);
    return response.json();
  })`);
}

/** Waits until the host has observed the requested number of scenario reads. */
async function waitForRequestCount(call, scenario, expected, timeoutMilliseconds = 8_000) {
  const deadline = Date.now() + timeoutMilliseconds;
  while (Date.now() < deadline) {
    const evidence = await readHostEvidence(call, scenario);
    if (evidence.requestCount >= expected) return evidence;
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
  throw new Error(`Timed out waiting for ${expected} ${scenario} requests.`);
}

/** Navigates to a fresh Dashboard session with one fixed host scenario and optional controlled timer acceleration. */
async function prepareScenario(call, scenario, accelerateTimers) {
  await call("Network.setExtraHTTPHeaders", { headers: { [scenarioHeader]: scenario } });
  await evaluate(call, `localStorage.setItem("dbnotifier.language.preference.v1", "en-GB"); localStorage.setItem(${JSON.stringify(acceleratedStorageKey)}, ${JSON.stringify(accelerateTimers ? "true" : "false")}); true`);
  const navigationMarker = `browser-e2e-${++navigationSequence}`;
  await call("Page.navigate", { url: `${dashboardUrl}/?run=${navigationMarker}#overview` });
  await waitFor(
    call,
    `location.search === ${JSON.stringify(`?run=${navigationMarker}`)} && document.readyState === "complete" && document.querySelector(".tv-mode-button")?.dataset.tvModeControl === "enter"`,
    "a fresh standard Dashboard shell",
  );
}

/** Activates the TV session and waits for its first authoritative fixture snapshot. */
async function enterAndWaitForSnapshot(call, scenario) {
  await evaluate(call, `document.querySelector(".tv-mode-button")?.click(); true`);
  try {
    await waitFor(call, `document.querySelector(".demo-badge")?.textContent?.trim() === "Local sandbox snapshot"`, "the first authoritative browser snapshot");
  } catch (error) {
    const diagnostic = await readView(call);
    const hostDiagnostic = await readHostEvidence(call, scenario);
    const resourceDiagnostic = await evaluate(call, `[...performance.getEntriesByType("resource")]
      .filter((entry) => entry.name.includes("/api/v1/dashboard/tv-snapshot"))
      .map((entry) => ({ duration: entry.duration, transferSize: entry.transferSize }))`);
    throw new Error(`${error.message} Sanitised browser state: ${JSON.stringify(diagnostic)} Host evidence: ${JSON.stringify(hostDiagnostic)} Resource evidence: ${JSON.stringify(resourceDiagnostic)}`);
  }
  const view = await readView(call);
  assertEvidence(view.tvActive && view.control === "exit", "TV presentation did not remain active after the authoritative read.");
  assertEvidence(view.instanceNames.length === 2 && view.instanceNames.includes("Orders sandbox"), "The browser did not render the two authoritative sandbox items.");
  return view;
}

/** Exits the current TV session so no timer or request can leak into the next scenario. */
async function exitTv(call) {
  const current = await readView(call);
  if (current.tvActive) {
    await evaluate(call, `document.querySelector(".tv-mode-button")?.click(); true`);
    await waitFor(call, `document.querySelector(".tv-mode-button")?.dataset.tvModeControl === "enter"`, "TV session cancellation");
  }
}

/** Proves one failure label while retaining the previously accepted snapshot in the same session. */
async function auditPreservedFailure(call, scenario, expectedBadge, expectedStatus) {
  await prepareScenario(call, scenario, true);
  await enterAndWaitForSnapshot(call, scenario);
  await waitFor(call, `document.querySelector(".demo-badge")?.textContent?.trim() === ${JSON.stringify(expectedBadge)}`, `${scenario} factual source state`);
  const view = await readView(call);
  const evidence = await waitForRequestCount(call, scenario, 2);
  assertEvidence(view.instanceNames.length === 2 && view.instanceNames.includes("Orders sandbox"), `${scenario} discarded the last valid snapshot.`);
  assertEvidence(view.timerDelays.includes(30_000), `${scenario} did not request the canonical 30-second reconciliation delay.`);
  assertEvidence(evidence.maximumConcurrency === 1, `${scenario} overlapped snapshot reads.`);
  assertEvidence(evidence.requests[1].statusCode === expectedStatus, `${scenario} did not produce the expected local status.`);
  await exitTv(call);
  return { scenario, requestCount: evidence.requestCount, maximumConcurrency: evidence.maximumConcurrency };
}

const connection = await connectToPage();
const { call, networkEvidence } = connection;
const summaries = [];
try {
  reportStage("connected to the dedicated loopback browser");
  await call("Page.enable");
  await call("Runtime.enable");
  await call("Network.enable");
  await call("Page.addScriptToEvaluateOnNewDocument", {
    source: `(() => {
      const nativeSetTimeout = window.setTimeout.bind(window);
      window.__dbNotifierBrowserE2eTimerDelays = [];
      window.setTimeout = (callback, delay, ...args) => {
        if (delay === 30000 || delay === 10000) window.__dbNotifierBrowserE2eTimerDelays.push(delay);
        const accelerated = localStorage.getItem(${JSON.stringify(acceleratedStorageKey)}) === "true";
        const effectiveDelay = accelerated && delay === 30000 ? 600 : accelerated && delay === 10000 ? 700 : delay;
        return nativeSetTimeout(callback, effectiveDelay, ...args);
      };
      Object.defineProperty(document.documentElement, "requestFullscreen", {
        configurable: true,
        value: () => Promise.reject(new Error("Fullscreen is intentionally unavailable in the headless evidence browser.")),
      });
    })();`,
  });

  await call("Network.setExtraHTTPHeaders", { headers: { [scenarioHeader]: "authoritative" } });
  await waitFor(call, `document.querySelector(".tv-mode-button") !== null`, "the initial Dashboard shell");
  let standardView = await readView(call);
  assertEvidence(!standardView.tvActive && standardView.instanceNames.length === 4, "The normal Dashboard did not preserve its four-item demonstration baseline.");
  assertEvidence((await readHostEvidence(call, "authoritative")).requestCount === 0, "The standard Dashboard contacted the sandbox before TV entry.");

  reportStage("starting authenticated SignalR hint and independent real periodic deadline");
  await prepareScenario(call, "signalr-authoritative", false);
  await enterAndWaitForSnapshot(call, "signalr-authoritative");
  const initialHintEvidence = await waitForRequestCount(call, "signalr-authoritative", 1);
  await waitForSignalREvidence(call, (evidence) => evidence.hints.activeConnections === 1, "one authenticated local SignalR connection");
  const firstSignalRCompletedAt = Date.parse(initialHintEvidence.requests[0].completedAt);
  const published = await publishSyntheticHint(call);
  assertEvidence(/^sha256-[0-9a-f]{64}$/.test(published.projectionRevision), "The synthetic control did not return one opaque canonical revision.");
  let hintReadEvidence = await waitForRequestCount(call, "signalr-authoritative", 2);
  await waitFor(call, `[...document.querySelectorAll(".overview-instance-name strong")].some((element) => element.textContent?.trim() === "Finance sandbox updated")`, "the authoritative re-read triggered by a hint");
  assertEvidence(hintReadEvidence.requests[1].statusCode === 200 && hintReadEvidence.requests[1].hadConditionalTag, "The hint did not trigger a conditional authoritative HTTPS read.");
  const signalRCadenceEvidence = await waitForRequestCount(call, "signalr-authoritative", 3, 38_000);
  const signalRPeriodicStartedAt = Date.parse(signalRCadenceEvidence.requests[2].startedAt);
  assertEvidence(signalRCadenceEvidence.requests[2].statusCode === 304 && signalRCadenceEvidence.requests[2].hadConditionalTag, "The independent periodic read did not retain ETag/304 semantics after a hint.");
  assertEvidence(signalRPeriodicStartedAt - firstSignalRCompletedAt >= 29_500 && signalRPeriodicStartedAt - firstSignalRCompletedAt <= 32_000, "The SignalR hint postponed or advanced the independent 30-second authoritative deadline.");
  assertEvidence(signalRCadenceEvidence.maximumConcurrency === 1, "SignalR-triggered and periodic authoritative reads overlapped.");
  const activeSignalREvidence = await readSignalREvidence(call);
  assertEvidence(activeSignalREvidence.hints.maximumConnections === 1 && activeSignalREvidence.hints.publishedHints >= 1, "The host did not retain bounded aggregate SignalR evidence.");
  summaries.push({
    scenario: "authenticated-signalr-hint",
    requestCount: signalRCadenceEvidence.requestCount,
    maximumConcurrency: signalRCadenceEvidence.maximumConcurrency,
    periodicDelayMilliseconds: signalRPeriodicStartedAt - firstSignalRCompletedAt,
    publishedHints: activeSignalREvidence.hints.publishedHints,
  });
  await exitTv(call);
  await waitForSignalREvidence(call, (evidence) => evidence.hints.activeConnections === 0, "SignalR connection cleanup after TV exit");
  reportStage("authenticated hint, authoritative re-read and independent cadence passed");

  reportStage("starting the real 30-second authoritative cadence");
  await prepareScenario(call, "authoritative", false);
  await enterAndWaitForSnapshot(call, "authoritative");
  let cadenceEvidence = await waitForRequestCount(call, "authoritative", 2, 38_000);
  const firstCompletedAt = Date.parse(cadenceEvidence.requests[0].completedAt);
  const secondStartedAt = Date.parse(cadenceEvidence.requests[1].startedAt);
  assertEvidence(cadenceEvidence.requests[0].statusCode === 200, "The immediate browser read did not receive the authoritative 200 response.");
  assertEvidence(cadenceEvidence.requests[1].statusCode === 304 && cadenceEvidence.requests[1].hadConditionalTag, "The periodic browser read did not preserve strong ETag/304 semantics.");
  assertEvidence(secondStartedAt - firstCompletedAt >= 29_500, "The real browser reconciliation began before 30 seconds elapsed after completion.");
  assertEvidence(cadenceEvidence.maximumConcurrency === 1, "The real 30-second cadence overlapped browser reads.");
  let cadenceView = await readView(call);
  assertEvidence(cadenceView.instanceNames.length === 2 && cadenceView.timerDelays.includes(30_000), "The 304 response changed snapshot content or bypassed the canonical timer.");
  summaries.push({ scenario: "authoritative-cadence", requestCount: cadenceEvidence.requestCount, maximumConcurrency: cadenceEvidence.maximumConcurrency, minimumDelayMilliseconds: secondStartedAt - firstCompletedAt });
  await exitTv(call);
  reportStage("real cadence and ETag/304 passed");

  for (const failure of [
    ["preserve-denied", "Local sandbox access denied", 403],
    ["preserve-incompatible", "Incompatible local sandbox snapshot", 426],
    ["preserve-malformed", "Incompatible local sandbox snapshot", 200],
    ["preserve-oversized", "Incompatible local sandbox snapshot", 200],
    ["preserve-error", "Local sandbox unavailable", 503],
  ]) {
    reportStage(`starting ${failure[0]}`);
    summaries.push(await auditPreservedFailure(call, ...failure));
  }

  reportStage("starting error recovery");
  await prepareScenario(call, "error-recovery", true);
  await enterAndWaitForSnapshot(call, "error-recovery");
  await waitFor(call, `document.querySelector(".demo-badge")?.textContent?.trim() === "Local sandbox unavailable"`, "the temporary error state");
  assertEvidence((await readView(call)).instanceNames.length === 2, "Temporary error recovery lost the last valid snapshot.");
  await waitFor(call, `document.querySelector(".demo-badge")?.textContent?.trim() === "Local sandbox snapshot"`, "authoritative recovery after an error");
  let recoveryEvidence = await waitForRequestCount(call, "error-recovery", 3);
  assertEvidence(recoveryEvidence.requests[2].statusCode === 304 && recoveryEvidence.maximumConcurrency === 1, "Error recovery did not re-read authoritative state serially.");
  summaries.push({ scenario: "error-recovery", requestCount: recoveryEvidence.requestCount, maximumConcurrency: recoveryEvidence.maximumConcurrency });
  await exitTv(call);

  reportStage("starting timeout recovery");
  await prepareScenario(call, "timeout-recovery", true);
  await enterAndWaitForSnapshot(call, "timeout-recovery");
  await waitFor(call, `document.querySelector(".demo-badge")?.textContent?.trim() === "Local sandbox offline"`, "the bounded timeout state");
  assertEvidence((await readView(call)).instanceNames.length === 2, "Request timeout discarded the last valid snapshot.");
  await waitFor(call, `document.querySelector(".demo-badge")?.textContent?.trim() === "Local sandbox snapshot"`, "authoritative recovery after timeout");
  let timeoutEvidence = await waitForRequestCount(call, "timeout-recovery", 3);
  assertEvidence(timeoutEvidence.requests[1].cancelled && timeoutEvidence.maximumConcurrency === 1, "The stalled response was not cancelled before serial recovery.");
  assertEvidence((await readView(call)).timerDelays.includes(10_000), "The browser did not request the bounded ten-second response deadline.");
  summaries.push({ scenario: "timeout-recovery", requestCount: timeoutEvidence.requestCount, maximumConcurrency: timeoutEvidence.maximumConcurrency });
  await exitTv(call);

  reportStage("starting offline recovery");
  await prepareScenario(call, "offline-recovery", true);
  await enterAndWaitForSnapshot(call, "offline-recovery");
  const beforeOfflineSignalR = await waitForSignalREvidence(
    call,
    (evidence) => evidence.hints.activeConnections === 1,
    "the pre-offline SignalR connection",
  );
  await call("Network.emulateNetworkConditions", { offline: true, latency: 0, downloadThroughput: -1, uploadThroughput: -1 });
  await waitFor(call, `document.querySelector(".demo-badge")?.textContent?.trim() === "Local sandbox offline"`, "the interrupted loopback connection");
  assertEvidence((await readView(call)).instanceNames.length === 2, "Offline state discarded the last valid snapshot.");
  await call("Network.emulateNetworkConditions", { offline: false, latency: 0, downloadThroughput: -1, uploadThroughput: -1 });
  await waitFor(call, `document.querySelector(".demo-badge")?.textContent?.trim() === "Local sandbox snapshot"`, "recovery after loopback interruption");
  let offlineEvidence = await waitForRequestCount(call, "offline-recovery", 2);
  const afterOfflineSignalR = await waitForSignalREvidence(
    call,
    (evidence) => evidence.hints.activeConnections === 1 &&
      evidence.hints.totalConnections > beforeOfflineSignalR.hints.totalConnections,
    "bounded SignalR reconnection after loopback interruption",
  );
  assertEvidence(offlineEvidence.maximumConcurrency === 1, "Offline recovery overlapped authoritative reads.");
  assertEvidence(afterOfflineSignalR.hints.maximumConnections === 1, "SignalR reconnection overlapped authenticated connections.");
  summaries.push({
    scenario: "offline-recovery",
    requestCount: offlineEvidence.requestCount,
    maximumConcurrency: offlineEvidence.maximumConcurrency,
    signalRReconnectsObserved: afterOfflineSignalR.hints.totalConnections - beforeOfflineSignalR.hints.totalConnections,
  });
  await exitTv(call);

  reportStage("starting cancellation and late-session fencing");
  await prepareScenario(call, "late-fence", true);
  await evaluate(call, `document.querySelector(".tv-mode-button")?.click(); true`);
  await waitForRequestCount(call, "late-fence", 1);
  await exitTv(call);
  await call("Network.setExtraHTTPHeaders", { headers: { [scenarioHeader]: "authoritative-after-fence" } });
  await evaluate(call, `document.querySelector(".tv-mode-button")?.click(); true`);
  await waitFor(call, `document.querySelector(".demo-badge")?.textContent?.trim() === "Local sandbox snapshot"`, "the replacement TV session");
  await new Promise((resolve) => setTimeout(resolve, 1_400));
  let fencedView = await readView(call);
  let lateEvidence = await waitForRequestCount(call, "late-fence", 1);
  assertEvidence(fencedView.badge === "Local sandbox snapshot" && fencedView.instanceNames.length === 2, "A late completion overwrote the replacement TV session.");
  assertEvidence(lateEvidence.requests[0].cancelled, "Leaving TV mode did not propagate cancellation to the late request.");
  summaries.push({ scenario: "late-fence", requestCount: lateEvidence.requestCount, maximumConcurrency: lateEvidence.maximumConcurrency });
  await exitTv(call);

  reportStage("starting controlled page pause and serial resume");
  await prepareScenario(call, "slow-serial", true);
  await enterAndWaitForSnapshot(call, "slow-serial");
  await call("Page.setWebLifecycleState", { state: "frozen" });
  await new Promise((resolve) => setTimeout(resolve, 750));
  await call("Page.setWebLifecycleState", { state: "active" });
  let slowEvidence = await waitForRequestCount(call, "slow-serial", 2);
  assertEvidence(slowEvidence.maximumConcurrency === 1 && slowEvidence.activeRequests <= 1, "Page resume overlapped slow reconciliation work.");
  summaries.push({ scenario: "pause-resume-serial", requestCount: slowEvidence.requestCount, maximumConcurrency: slowEvidence.maximumConcurrency });
  await exitTv(call);

  await prepareScenario(call, "authoritative-after-fence", true);
  standardView = await readView(call);
  assertEvidence(!standardView.tvActive && standardView.control === "enter" && standardView.instanceNames.length === 4, "The final standard Dashboard did not return to its demonstration baseline.");

  const version = await fetch(`${cdpEndpoint}/json/version`).then((response) => response.json());
  assertEvidence(networkEvidence.externalOrigins.size === 0, `The evidence browser contacted an external HTTP origin: ${[...networkEvidence.externalOrigins].join(", ")}`);
  reportStage("all browser scenarios passed");
  console.log(JSON.stringify({
    result: "passed",
    browser: version.Browser,
    scenarios: summaries,
    observedHttpRequests: networkEvidence.observedHttpRequests,
    observedWebSockets: networkEvidence.observedWebSockets,
    observedExternalHttpRequests: 0,
    operationalData: false,
  }));
} finally {
  try {
    await call("Network.emulateNetworkConditions", { offline: false, latency: 0, downloadThroughput: -1, uploadThroughput: -1 });
  } catch {
    // The dedicated browser may already be closing after a failed assertion.
  }
  connection.close();
}
