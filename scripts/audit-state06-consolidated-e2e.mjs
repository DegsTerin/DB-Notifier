/**
 * Module purpose: Audits one correlated STATE-06 local run through a dedicated browser without retaining payloads, identities or secret material.
 * The API snapshot remains authoritative; SignalR is only a change hint and notification delivery remains a bounded in-memory test sink.
 */

const cdpEndpoint = requireLoopbackUrl("DBNOTIFIER_STATE06_CDP_ENDPOINT", "http:");
const dashboardUrl = requireLoopbackUrl("DBNOTIFIER_STATE06_URL", "https:");
const runId = requireRunId("DBNOTIFIER_STATE06_RUN_ID");
const humanHeader = "X-DBN-TV-Test-Human";
const humanSubject = "sandbox-tv-reviewer";
const runHeader = "X-DBN-State06-Consolidated-Run";
const accelerateKey = "dbnotifier.state06.consolidated.accelerate";

/** Reads one required exact-loopback URL. */
function requireLoopbackUrl(name, protocol) {
  const raw = process.env[name];
  if (!raw) throw new Error(`${name} is required.`);
  const value = new URL(raw);
  if (value.protocol !== protocol || !["127.0.0.1", "localhost", "[::1]"].includes(value.hostname)) {
    throw new Error(`${name} must use ${protocol}// on loopback.`);
  }
  return value.toString().replace(/\/$/, "");
}

/** Reads one public per-run correlation identifier. */
function requireRunId(name) {
  const value = process.env[name];
  if (!value || !/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value)) {
    throw new Error(`${name} must be one version-four GUID.`);
  }
  return value;
}

/** Throws one bounded factual assertion. */
function assertEvidence(condition, message) {
  if (!condition) throw new Error(message);
}

/** Emits one sanitised progress marker to the runner's temporary diagnostic log. */
function reportStage(message) {
  console.error(`[state06-consolidated-e2e] ${message}`);
}

/** Reduces a failed loopback response to the explicit typed diagnostic allow-list. */
function sanitiseProblem(status, body) {
  let problem;
  try {
    problem = JSON.parse(body);
  } catch {
    return { status, code: "state06.invalid_problem_response" };
  }
  const failure = problem?.failure ?? {};
  const safeText = (value) => typeof value === "string" && /^[A-Za-z0-9_.:-]{1,96}$/.test(value)
    ? value
    : null;
  const safeInteger = (value) => Number.isSafeInteger(value) && value >= 0 ? value : null;
  return {
    status,
    code: safeText(problem?.code),
    stage: safeText(problem?.stage),
    boundary: safeText(failure.boundary),
    category: safeText(failure.category),
    httpStatusCode: safeInteger(failure.httpStatusCode),
    disposition: safeText(failure.disposition),
    errorCode: safeText(failure.errorCode),
    attempts: safeInteger(failure.attempts),
    fenceToken: safeInteger(failure.fenceToken),
    centralAgentState: safeText(failure.centralAgentState),
    centralCertificateState: safeText(failure.centralCertificateState),
    localIdentityState: safeText(failure.localIdentityState),
    transportAttempted: typeof failure.transportAttempted === "boolean" ? failure.transportAttempted : null,
    expectedCount: safeInteger(failure.expectedCount),
    actualCount: safeInteger(failure.actualCount),
  };
}

/** Connects to the single dedicated Dashboard page and records only transport aggregates. */
async function connectToPage() {
  const targets = await fetch(`${cdpEndpoint}/json/list`).then((response) => response.json());
  const target = targets.find((candidate) => candidate.type === "page" && candidate.url.startsWith(dashboardUrl));
  if (!target?.webSocketDebuggerUrl) throw new Error("The dedicated Dashboard page was not exposed by CDP.");
  const socket = new WebSocket(target.webSocketDebuggerUrl);
  const pending = new Map();
  const network = { requests: 0, webSockets: 0, externalOrigins: new Set() };
  let nextId = 0;
  await new Promise((resolve, reject) => {
    socket.addEventListener("open", resolve, { once: true });
    socket.addEventListener("error", reject, { once: true });
  });
  socket.addEventListener("message", (event) => {
    const message = JSON.parse(event.data);
    if (!message.id) {
      if (message.method === "Network.requestWillBeSent") {
        const url = new URL(message.params.request.url);
        if (url.protocol === "http:" || url.protocol === "https:") {
          network.requests += 1;
          if (!["127.0.0.1", "localhost", "[::1]"].includes(url.hostname)) network.externalOrigins.add(url.origin);
        }
      } else if (message.method === "Network.webSocketCreated") {
        const url = new URL(message.params.url);
        network.webSockets += 1;
        if (!["127.0.0.1", "localhost", "[::1]"].includes(url.hostname)) network.externalOrigins.add(url.origin);
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

/** Evaluates one serialisable expression in the dedicated browser page. */
async function evaluate(call, expression) {
  const result = await call("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true });
  if (result.exceptionDetails) throw new Error(result.exceptionDetails.exception?.description ?? result.exceptionDetails.text);
  return result.result.value;
}

/** Polls one browser condition within a fixed deadline. */
async function waitFor(call, predicate, description, timeoutMilliseconds = 10_000) {
  const deadline = Date.now() + timeoutMilliseconds;
  while (Date.now() < deadline) {
    try {
      if (await evaluate(call, `Boolean(${predicate})`)) return;
    } catch {
      // Navigation or deliberate offline mode may transiently replace the execution context.
    }
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
  throw new Error(`Timed out waiting for ${description}.`);
}

/** Reads the factual public Dashboard view without response bodies. */
async function readView(call) {
  return evaluate(call, `(() => ({
    tvActive: document.documentElement.dataset.tvMode === "true",
    control: document.querySelector(".tv-mode-button")?.dataset.tvModeControl ?? null,
    badge: document.querySelector(".demo-badge")?.textContent?.trim() ?? null,
    names: [...document.querySelectorAll(".overview-instance-name strong")].map((element) => element.textContent?.trim()),
    statuses: [...document.querySelectorAll(".status-badge")].map((element) => element.textContent?.trim()),
    timerDelays: [...(window.__dbNotifierConsolidatedTimerDelays ?? [])],
  }))()`);
}

/** Performs one authenticated test-only harness control through the same HTTPS origin. */
async function control(call, route, method = "GET") {
  const result = await evaluate(call, `fetch(${JSON.stringify(route)}, {
    method: ${JSON.stringify(method)},
    headers: {
      ${JSON.stringify(humanHeader)}: ${JSON.stringify(humanSubject)},
      ${JSON.stringify(runHeader)}: ${JSON.stringify(runId)},
    },
    cache: "no-store",
  }).then(async (response) => {
    const body = await response.text();
    return { ok: response.ok, status: response.status, body };
  })`);
  if (!result.ok) throw new Error(`${route} failed ${JSON.stringify(sanitiseProblem(result.status, result.body))}`);
  return result.body.length === 0 ? null : JSON.parse(result.body);
}

/** Waits for one host evidence predicate while the browser remains online. */
async function waitForEvidence(call, predicate, description, timeoutMilliseconds = 15_000) {
  const deadline = Date.now() + timeoutMilliseconds;
  while (Date.now() < deadline) {
    const evidence = await control(call, "/__dbnotifier-state06-consolidated/evidence");
    if (predicate(evidence)) return evidence;
    await new Promise((resolve) => setTimeout(resolve, 500));
  }
  throw new Error(`Timed out waiting for ${description}.`);
}

/** Enters Dashboard TV and waits for its immediate authoritative API snapshot. */
async function enterTv(call) {
  await evaluate(call, `document.querySelector(".tv-mode-button")?.click(); true`);
  await waitFor(call, `document.querySelector(".demo-badge")?.textContent?.trim() === "Local sandbox snapshot"`, "the immediate authoritative snapshot");
  const view = await readView(call);
  assertEvidence(view.tvActive && view.control === "exit", "TV mode did not remain active after its immediate read.");
  assertEvidence(view.names.length === 1 && view.names[0] === "Sandbox Assignment", "The browser did not render the single correlated synthetic assignment.");
  return view;
}

/** Exits Dashboard TV and waits until its owned work is cancelled. */
async function exitTv(call) {
  if ((await readView(call)).tvActive) {
    await evaluate(call, `document.querySelector(".tv-mode-button")?.click(); true`);
    await waitFor(call, `document.querySelector(".tv-mode-button")?.dataset.tvModeControl === "enter"`, "TV cancellation");
  }
}

const connection = await connectToPage();
const { call, network } = connection;
try {
  await call("Page.enable");
  await call("Runtime.enable");
  await call("Network.enable");
  await call("Page.addScriptToEvaluateOnNewDocument", {
    source: `(() => {
      const nativeSetTimeout = window.setTimeout.bind(window);
      window.__dbNotifierConsolidatedTimerDelays = [];
      window.setTimeout = (callback, delay, ...args) => {
        if (delay === 30000 || delay === 10000) window.__dbNotifierConsolidatedTimerDelays.push(delay);
        const accelerated = localStorage.getItem(${JSON.stringify(accelerateKey)}) === "true";
        const effectiveDelay = accelerated && delay === 30000 ? 650 : delay;
        return nativeSetTimeout(callback, effectiveDelay, ...args);
      };
    })();`,
  });

  reportStage("proving normal composition isolation");
  await call("Page.navigate", { url: `${dashboardUrl}/#overview` });
  try {
    await waitFor(call, `document.querySelector(".tv-mode-button") !== null`, "the standard Dashboard shell", 20_000);
  } catch (error) {
    const diagnostic = await evaluate(call, `({
      url: location.href,
      readyState: document.readyState,
      title: document.title,
      bodyText: document.body?.innerText?.slice(0, 240) ?? "",
      scripts: [...document.scripts].map((script) => script.src),
    })`);
    throw new Error(`${error.message} Sanitised page state: ${JSON.stringify(diagnostic)}`);
  }
  await evaluate(call, `localStorage.setItem("dbnotifier.language.preference.v1", "en-GB"); localStorage.setItem(${JSON.stringify(accelerateKey)}, "false"); true`);
  await call("Page.navigate", { url: `${dashboardUrl}/?run=${runId}#overview` });
  await waitFor(call, `document.readyState === "complete" && document.querySelector(".tv-mode-button")?.dataset.tvModeControl === "enter"`, "the isolated Dashboard shell");
  let view = await readView(call);
  assertEvidence(!view.tvActive && view.names.length === 4, "Normal composition did not retain its four-item demonstration baseline.");
  let evidence = await control(call, "/__dbnotifier-state06-consolidated/evidence");
  assertEvidence(evidence.runId === runId && evidence.initialised && evidence.initialReplayObserved, "The host did not correlate initial Agent replay evidence.");
  assertEvidence(evidence.snapshotRequests.length === 0, "Normal composition contacted the authoritative snapshot before TV entry.");

  reportStage("proving immediate read, SignalR hint, notification and independent cadence");
  view = await enterTv(call);
  assertEvidence(view.statuses.some((value) => value?.includes("Degraded")), "The initial canonical Degraded state was not displayed.");
  evidence = await waitForEvidence(call, (value) => value.snapshotRequests.length >= 1 && value.signalR.activeConnections === 1, "one snapshot and one authenticated SignalR connection");
  const firstCompletedAt = Date.parse(evidence.snapshotRequests[0].completedAt);
  await control(call, "/__dbnotifier-state06-consolidated/advance", "POST");
  const advanced = await waitForEvidence(call, (value) => value.advanced, "the committed observation advance");
  assertEvidence(advanced.advanced && advanced.observationSamples === 2, "The correlated observation transition did not commit exactly twice.");
  assertEvidence(advanced.notificationDeliveries === 1 && advanced.notificationRestartDeduplicated, "The local test sink did not prove one restart-safe notification.");
  await waitFor(call, `[...document.querySelectorAll(".status-badge")].some((element) => element.textContent?.includes("Unavailable"))`, "the hinted authoritative Unavailable state");
  evidence = await waitForEvidence(call, (value) => value.snapshotRequests.length >= 2 && value.signalR.publishedHints >= 1, "the hinted authoritative re-read");
  assertEvidence(evidence.snapshotRequests[1].hadConditionalTag, "The SignalR hint did not cause a conditional authoritative read.");
  await new Promise((resolve) => setTimeout(resolve, 29_500));
  evidence = await waitForEvidence(call, (value) => value.snapshotRequests.length >= 3, "the independent real 30-second reconciliation", 8_000);
  const periodicStartedAt = Date.parse(evidence.snapshotRequests[2].startedAt);
  assertEvidence(evidence.snapshotRequests[2].statusCode === 304 && evidence.snapshotRequests[2].hadConditionalTag, "The independent cadence did not retain ETag/304.");
  assertEvidence(periodicStartedAt - firstCompletedAt >= 29_500 && periodicStartedAt - firstCompletedAt <= 32_000, "The independent cadence fell outside its 30-second deadline.");
  assertEvidence(evidence.maximumSnapshotConcurrency === 1 && evidence.activeSnapshotRequests === 0, "Snapshot reconciliation exceeded its concurrency budget.");
  assertEvidence((await readView(call)).timerDelays.includes(30_000), "The browser did not request the canonical 30-second timer.");

  reportStage("proving offline preservation, recovery and fencing");
  await exitTv(call);
  await waitForEvidence(call, (value) => value.signalR.activeConnections === 0, "SignalR cleanup after TV exit");
  await evaluate(call, `localStorage.setItem(${JSON.stringify(accelerateKey)}, "true"); true`);
  await enterTv(call);
  await call("Network.emulateNetworkConditions", { offline: true, latency: 0, downloadThroughput: -1, uploadThroughput: -1 });
  await waitFor(call, `document.querySelector(".demo-badge")?.textContent?.trim() === "Local sandbox offline"`, "the bounded local offline state");
  view = await readView(call);
  assertEvidence(view.names.length === 1 && view.names[0] === "Sandbox Assignment", "Offline handling discarded the last factual snapshot.");
  await call("Network.emulateNetworkConditions", { offline: false, latency: 0, downloadThroughput: -1, uploadThroughput: -1 });
  await waitFor(call, `document.querySelector(".demo-badge")?.textContent?.trim() === "Local sandbox snapshot"`, "authoritative recovery after offline mode");
  evidence = await waitForEvidence(call, (value) => value.maximumSnapshotConcurrency === 1 && value.activeSnapshotRequests === 0, "serial recovery evidence");

  reportStage("proving non-executable command transport and revocation fence");
  await control(call, "/__dbnotifier-state06-consolidated/finalise", "POST");
  evidence = await waitForEvidence(call, (value) => value.finalised, "the final revocation and command evidence");
  assertEvidence(evidence.finalised && evidence.stage === "finalised", "The consolidated run did not reach its bounded terminal stage.");
  assertEvidence(evidence.commandJournalEntries === 2 && evidence.commandAttempts === 0, "Command transport did not remain deliberately non-executable.");
  assertEvidence(evidence.observationSamples === 2 && evidence.notificationDeliveries === 1, "Replay or restart duplicated correlated side effects.");
  assertEvidence(evidence.heartbeatDeniedAfterRevocation && evidence.assignmentsDeniedAfterRevocation && evidence.observationDeniedAfterRevocation && evidence.commandDeniedAfterRevocation, "Revocation did not fail closed across every Agent path.");
  assertEvidence(evidence.centralRevocationCommitted && evidence.revokedCertificateCount >= 1, "Central Agent and certificate revocation were not proved atomically.");
  assertEvidence(evidence.heartbeatServerDeniedAfterRevocation && evidence.assignmentsServerDeniedAfterRevocation, "Direct server denial was not proved separately from Agent quarantine.");
  assertEvidence(evidence.heartbeatTransportAttemptedAfterRevocation && !evidence.assignmentsTransportAttemptedAfterRevocation, "The harness did not distinguish the first server denial from later local quarantine.");
  assertEvidence(evidence.assignmentLastKnownGoodPreserved && evidence.commandProcessesExitedBeforeRevocation, "Local LKG or child-process ordering evidence was incomplete.");
  assertEvidence(Number.isSafeInteger(evidence.heartbeatFenceAfterRevocation) && Number.isSafeInteger(evidence.assignmentsFenceAfterRevocation) && evidence.assignmentsFenceAfterRevocation > evidence.heartbeatFenceAfterRevocation, "Agent-local fencing did not advance monotonically.");
  assertEvidence(evidence.failure === null, "A terminal diagnostic remained after successful finalisation.");
  assertEvidence(evidence.maximumSnapshotConcurrency === 1, "The final snapshot concurrency budget was exceeded.");
  await exitTv(call);
  evidence = await waitForEvidence(call, (value) => value.signalR.activeConnections === 0, "final SignalR cleanup");
  assertEvidence(network.externalOrigins.size === 0, `The dedicated browser contacted an external origin: ${[...network.externalOrigins].join(", ")}`);

  await control(call, "/__dbnotifier-state06-consolidated/shutdown", "POST");
  const version = await fetch(`${cdpEndpoint}/json/version`).then((response) => response.json());
  console.log(JSON.stringify({
    result: "passed",
    browser: version.Browser,
    correlatedRun: true,
    observationSamples: evidence.observationSamples,
    notificationDeliveries: evidence.notificationDeliveries,
    commandJournalEntries: evidence.commandJournalEntries,
    commandAttempts: evidence.commandAttempts,
    revokedCertificateCount: evidence.revokedCertificateCount,
    heartbeatFenceAfterRevocation: evidence.heartbeatFenceAfterRevocation,
    assignmentsFenceAfterRevocation: evidence.assignmentsFenceAfterRevocation,
    maximumSnapshotConcurrency: evidence.maximumSnapshotConcurrency,
    observedHttpRequests: network.requests,
    observedWebSockets: network.webSockets,
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
