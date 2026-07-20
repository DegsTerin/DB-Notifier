/**
 * Module purpose: Audits the final-human-sample remediation through one dedicated loopback browser while retaining only bounded, sanitised evidence.
 * Browser transport remains online throughout; Agent loss/recovery and visual truth are driven only by exact authenticated test controls.
 */

const cdpEndpoint = requireLoopbackUrl("DBNOTIFIER_STATE06_CDP_ENDPOINT", "http:");
const dashboardUrl = requireLoopbackUrl("DBNOTIFIER_STATE06_URL", "https:");
const runId = requireRunId("DBNOTIFIER_STATE06_RUN_ID");
const humanHeader = "X-DBN-TV-Test-Human";
const humanSubject = "sandbox-tv-reviewer";
const runHeader = "X-DBN-State06-Consolidated-Run";
const evidenceRoute = "/__dbnotifier-state06-consolidated/evidence";
const evidencePageRoute = "/__dbnotifier-state06-human-remediation/evidence-page";

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

/** Reads one public version-four run identifier. */
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

/** Emits one sanitised progress marker. */
function reportStage(message) {
  console.error(`[state06-final-human-remediation] ${message}`);
}

/** Connects to the single dedicated page and records only origin-level transport counts. */
async function connectToPage() {
  const targets = await fetch(`${cdpEndpoint}/json/list`).then((response) => response.json());
  const target = targets.find((candidate) => candidate.type === "page" && candidate.url.startsWith(dashboardUrl));
  if (!target?.webSocketDebuggerUrl) throw new Error("The dedicated remediation page was not exposed by CDP.");
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

/** Evaluates one serialisable expression in the dedicated page. */
async function evaluate(call, expression) {
  const result = await call("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true });
  if (result.exceptionDetails) throw new Error(result.exceptionDetails.exception?.description ?? result.exceptionDetails.text);
  return result.result.value;
}

/** Polls one browser condition within a fixed deadline. */
async function waitFor(call, predicate, description, timeoutMilliseconds = 15_000) {
  const deadline = Date.now() + timeoutMilliseconds;
  while (Date.now() < deadline) {
    try {
      if (await evaluate(call, `Boolean(${predicate})`)) return;
    } catch {
      // Same-origin frame navigation may briefly replace its execution context.
    }
    await new Promise((resolve) => setTimeout(resolve, 50));
  }
  throw new Error(`Timed out waiting for ${description}.`);
}

/** Performs one authenticated test-only control while browser transport remains available. */
async function control(call, route, method = "GET") {
  const result = await evaluate(call, `fetch(${JSON.stringify(route)}, {
    method: ${JSON.stringify(method)},
    headers: {
      ${JSON.stringify(humanHeader)}: ${JSON.stringify(humanSubject)},
      ${JSON.stringify(runHeader)}: ${JSON.stringify(runId)},
    },
    cache: "no-store",
    credentials: "same-origin",
  }).then(async (response) => ({ ok: response.ok, status: response.status, body: await response.text() }))`);
  if (!result.ok) throw new Error(`${route} failed with local status ${result.status}.`);
  return result.body.length === 0 ? null : JSON.parse(result.body);
}

/** Waits for one sanitised evidence state. */
async function waitForEvidence(call, predicate, description, timeoutMilliseconds = 20_000) {
  const deadline = Date.now() + timeoutMilliseconds;
  while (Date.now() < deadline) {
    const evidence = await control(call, evidenceRoute);
    if (predicate(evidence)) return evidence;
    await new Promise((resolve) => setTimeout(resolve, 250));
  }
  throw new Error(`Timed out waiting for ${description}.`);
}

/** Reads only visible factual text from the same-origin Dashboard iframe. */
async function readDashboard(call) {
  return evaluate(call, `(() => {
    const frame = document.querySelector("iframe")?.contentDocument;
    return {
      ready: frame?.readyState === "complete",
      tvActive: frame?.documentElement?.dataset.tvMode === "true",
      control: frame?.querySelector(".tv-mode-button")?.dataset.tvModeControl ?? null,
      badge: frame?.querySelector(".demo-badge")?.textContent?.trim() ?? null,
      names: [...(frame?.querySelectorAll(".overview-instance-name strong") ?? [])].map((element) => element.textContent?.trim()),
      statuses: [...(frame?.querySelectorAll(".status-badge") ?? [])].map((element) => element.textContent?.trim()),
      footer: frame?.querySelector("footer")?.textContent?.trim() ?? "",
    };
  })()`);
}

/** Enters TV mode inside the bounded same-origin frame. */
async function enterTv(call) {
  await evaluate(call, `document.querySelector("iframe")?.contentDocument?.querySelector(".tv-mode-button")?.click(); true`);
  await waitFor(
    call,
    `document.querySelector("iframe")?.contentDocument?.querySelector(".demo-badge")?.textContent?.trim() === "Snapshot do sandbox local"`,
    "the immediate authoritative sandbox snapshot");
  const view = await readDashboard(call);
  assertEvidence(view.tvActive && view.control === "exit", "Dashboard TV did not remain active after its immediate read.");
  return view;
}

/** Exits TV mode and waits until the frame restores its session-only control. */
async function exitTv(call) {
  if ((await readDashboard(call)).tvActive) {
    await evaluate(call, `document.querySelector("iframe")?.contentDocument?.querySelector(".tv-mode-button")?.click(); true`);
    await waitFor(
      call,
      `document.querySelector("iframe")?.contentDocument?.querySelector(".tv-mode-button")?.dataset.tvModeControl === "enter"`,
      "Dashboard TV cancellation");
  }
}

const connection = await connectToPage();
const { call, network } = connection;
try {
  await call("Page.enable");
  await call("Runtime.enable");
  await call("Network.enable");
  await call("Network.setExtraHTTPHeaders", {
    headers: { [humanHeader]: humanSubject, [runHeader]: runId },
  });

  reportStage("opening the isolated test-only evidence surface");
  await call("Page.navigate", { url: `${dashboardUrl}${evidencePageRoute}` });
  await waitFor(call, `document.querySelector("#agent-evidence-title") !== null`, "the evidence surface");
  await waitFor(
    call,
    `document.querySelector("iframe")?.contentDocument?.querySelector(".tv-mode-button")?.dataset.tvModeControl === "enter"`,
    "the same-origin Dashboard frame");
  let evidence = await waitForEvidence(
    call,
    (value) => value.humanRemediationMode && value.stage === "agent-transport-ready",
    "the initial Agent transport stage");
  assertEvidence(evidence.humanAgentTransportState === "available", "The initial Agent transport was not explicitly available.");
  assertEvidence(evidence.humanPendingObservations === 0 && evidence.humanServerObservationSamples === 1, "The initial durable counts were not exact.");
  let view = await enterTv(call);
  assertEvidence(view.names.length === 1 && view.names[0] === "Sandbox Assignment", "The initial correlated assignment was not visible.");
  assertEvidence(view.statuses.some((value) => value?.includes("Degradado")), "The initial valid Degraded snapshot was not visible.");

  reportStage("proving Agent-only transport loss with one preserved observation");
  await control(call, "/__dbnotifier-state06-human-remediation/agent-loss", "POST");
  evidence = await waitForEvidence(
    call,
    (value) => value.stage === "agent-observation-pending" && value.humanPendingObservations === 1,
    "one pending Agent observation");
  assertEvidence(evidence.humanAgentTransportState === "unavailable", "Agent loss was not distinct from browser availability.");
  assertEvidence(evidence.humanServerObservationSamples === 1, "The unavailable Agent unexpectedly advanced Server evidence.");
  await waitFor(call, `document.querySelector("#page-health")?.textContent === "Browser → API disponível"`, "continued browser/API availability");
  view = await readDashboard(call);
  assertEvidence(view.statuses.some((value) => value?.includes("Degradado")), "Agent loss discarded or replaced the last valid Dashboard fact.");

  reportStage("proving recovery and exactly-once replay");
  await control(call, "/__dbnotifier-state06-human-remediation/agent-recovery", "POST");
  evidence = await waitForEvidence(
    call,
    (value) => value.stage === "agent-replay-accepted-once" && value.humanReplayAcceptedOnce,
    "the exactly-once Agent replay");
  assertEvidence(evidence.humanAgentTransportState === "recovered", "The Agent transport did not reach its recovered state.");
  assertEvidence(evidence.humanPendingObservations === 0 && evidence.humanServerObservationSamples === 2, "Replay counts did not prove one acknowledgement and one new Server sample.");
  await exitTv(call);
  view = await enterTv(call);
  assertEvidence(view.statuses.some((value) => value?.includes("Indisponível")), "The recovered authoritative Unavailable snapshot was not visible.");

  reportStage("proving current Unknown, stale and planned support truth");
  await control(call, "/__dbnotifier-state06-human-remediation/visual-truth", "POST");
  evidence = await waitForEvidence(
    call,
    (value) => value.stage === "visual-truth-ready" && value.humanVisualTruthEnabled,
    "the bounded visual-truth fixture");
  await exitTv(call);
  view = await enterTv(call);
  assertEvidence(view.names.length === 2, "The visual-truth snapshot did not expose exactly two bounded fixtures.");
  assertEvidence(view.statuses.some((value) => value?.includes("Desconhecido")), "Current Unknown was not textually visible.");
  assertEvidence(view.statuses.some((value) => value?.includes("Desatualizado")), "Stale evidence was not textually visible.");
  assertEvidence(view.footer.includes("Providers planejados não representam suporte público ou homologação"), "Planned support was not distinguished from public support.");
  await waitFor(call, `document.body.innerText.includes("Planejado — não implementado") && document.body.innerText.includes("Sandbox local sintético")`, "the companion support and origin truth");

  reportStage("fencing completion and cleanup hand-off");
  await control(call, "/__dbnotifier-state06-human-remediation/complete", "POST");
  evidence = await waitForEvidence(call, (value) => value.finalised && value.stage === "completed", "terminal remediation evidence");
  assertEvidence(evidence.commandAttempts === 0, "The remediation created a forbidden command attempt.");
  assertEvidence(evidence.maximumSnapshotConcurrency === 1 && evidence.activeSnapshotRequests === 0, "Snapshot reads exceeded the serial budget.");
  assertEvidence(evidence.failure === null, "A terminal diagnostic remained after successful remediation evidence.");
  assertEvidence(network.externalOrigins.size === 0, `The dedicated browser contacted an external origin: ${[...network.externalOrigins].join(", ")}`);

  await exitTv(call);
  await control(call, "/__dbnotifier-state06-consolidated/shutdown", "POST");
  const version = await fetch(`${cdpEndpoint}/json/version`).then((response) => response.json());
  console.log(JSON.stringify({
    result: "passed",
    browser: version.Browser,
    humanRemediationMode: true,
    agentLossObserved: evidence.humanLossObserved,
    agentReplayAcceptedOnce: evidence.humanReplayAcceptedOnce,
    pendingObservations: evidence.humanPendingObservations,
    serverObservationSamples: evidence.humanServerObservationSamples,
    unknownAndStaleVisible: evidence.humanVisualTruthEnabled,
    commandAttempts: evidence.commandAttempts,
    maximumSnapshotConcurrency: evidence.maximumSnapshotConcurrency,
    observedHttpRequests: network.requests,
    observedWebSockets: network.webSockets,
    observedExternalHttpRequests: 0,
    operationalData: false,
  }));
} finally {
  try {
    await exitTv(call);
  } catch {
    // The dedicated frame may already be closing after a failed assertion.
  }
  connection.close();
}
