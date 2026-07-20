/**
 * Module purpose: Presents exactly one final STATE-06 human sample in a dedicated visible browser while retaining bounded, sanitised evidence.
 * The presenter injects only ephemeral test headers, never advances a normal review automatically and owns no product or operational control.
 */

const cdpEndpoint = requireLoopbackUrl("DBNOTIFIER_STATE06_CDP_ENDPOINT", "http:");
const dashboardUrl = requireLoopbackUrl("DBNOTIFIER_STATE06_URL", "https:");
const runId = requireRunId("DBNOTIFIER_STATE06_RUN_ID");
const sample = requireSample("DBNOTIFIER_STATE06_REVIEW_SAMPLE");
const qualityGateAutomation = process.env.DBNOTIFIER_STATE06_REVIEW_AUTOMATION === "true";
const humanHeader = "X-DBN-TV-Test-Human";
const humanSubject = "sandbox-tv-reviewer";
const runHeader = "X-DBN-State06-Consolidated-Run";
const evidencePageRoute = "/__dbnotifier-state06-human-remediation/evidence-page";
const shutdownRoute = "/__dbnotifier-state06-consolidated/shutdown";
const reviewBudgetMilliseconds = 12 * 60 * 1000;

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

/** Accepts only one of the two explicitly authorised final samples. */
function requireSample(name) {
  const value = process.env[name];
  if (!["S06-HG-001", "S06-HG-006"].includes(value)) throw new Error(`${name} is not an authorised sample.`);
  return value;
}

/** Throws one bounded factual assertion. */
function assertEvidence(condition, message) {
  if (!condition) throw new Error(message);
}

/** Connects to the single dedicated page and records only origin-level network evidence. */
async function connectToPage() {
  const targets = await fetch(`${cdpEndpoint}/json/list`).then((response) => response.json());
  const target = targets.find((candidate) => candidate.type === "page" && candidate.url.startsWith(dashboardUrl));
  if (!target?.webSocketDebuggerUrl) throw new Error("The dedicated review page was not exposed by CDP.");
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

/** Waits for one browser condition within an explicit deadline. */
async function waitFor(call, predicate, description, timeoutMilliseconds = 20_000) {
  const deadline = Date.now() + timeoutMilliseconds;
  while (Date.now() < deadline) {
    try {
      if (await evaluate(call, `Boolean(${predicate})`)) return;
    } catch {
      // Same-origin navigation may briefly replace the execution context.
    }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  throw new Error(`Timed out waiting for ${description}.`);
}

/** Clicks one visible, enabled and stage-gated laboratory control. */
async function clickControl(call, id) {
  const clicked = await evaluate(call, `(() => {
    const button = document.getElementById(${JSON.stringify(id)});
    if (!button || button.hidden || button.disabled) return false;
    button.click();
    return true;
  })()`);
  assertEvidence(clicked, `The test-only control ${id} was not eligible.`);
}

/** Reads the visible Dashboard facts from the same-origin frame. */
async function readDashboard(call) {
  return evaluate(call, `(() => {
    const frame = document.querySelector("iframe")?.contentDocument;
    return {
      tvActive: frame?.documentElement?.dataset.tvMode === "true",
      names: [...(frame?.querySelectorAll(".overview-instance-name strong") ?? [])].map((element) => element.textContent?.trim()),
      statuses: [...(frame?.querySelectorAll(".status-badge") ?? [])].map((element) => element.textContent?.trim()),
      footer: frame?.querySelector("footer")?.textContent?.trim() ?? "",
    };
  })()`);
}

/** Enters TV mode and waits for the immediate authoritative sandbox read. */
async function enterTv(call) {
  const view = await readDashboard(call);
  if (!view.tvActive) {
    await evaluate(call, `document.querySelector("iframe")?.contentDocument?.querySelector(".tv-mode-button")?.click(); true`);
  }
  await waitFor(
    call,
    `document.querySelector("iframe")?.contentDocument?.querySelector(".demo-badge")?.textContent?.trim() === "Snapshot do sandbox local"`,
    "the immediate Dashboard TV snapshot");
}

/** Exits TV mode when the frame is still available. */
async function exitTv(call) {
  if ((await readDashboard(call)).tvActive) {
    await evaluate(call, `document.querySelector("iframe")?.contentDocument?.querySelector(".tv-mode-button")?.click(); true`);
    await waitFor(
      call,
      `document.querySelector("iframe")?.contentDocument?.querySelector(".tv-mode-button")?.dataset.tvModeControl === "enter"`,
      "Dashboard TV cancellation");
  }
}

/** Performs the deterministic runner smoke without recording or requesting a human decision. */
async function runQualityGateAutomation(call) {
  if (sample === "S06-HG-001") {
    await clickControl(call, "control-loss");
    await waitFor(call, `document.documentElement.dataset.evidenceStage === "agent-observation-pending"`, "the pending Agent barrier");
    assertEvidence(
      await evaluate(call, `document.documentElement.dataset.agentTransport === "unavailable"`),
      "The Agent transport did not become unavailable.");
    await clickControl(call, "control-recovery");
    await waitFor(call, `document.documentElement.dataset.evidenceStage === "agent-replay-accepted-once"`, "the recovered Agent barrier");
    await clickControl(call, "control-complete");
  } else {
    await clickControl(call, "control-visual");
    await waitFor(call, `document.documentElement.dataset.evidenceStage === "visual-truth-ready"`, "the visual-truth barrier");
    await exitTv(call);
    await enterTv(call);
    const view = await readDashboard(call);
    assertEvidence(view.statuses.some((value) => value?.includes("Desconhecido")), "Current Unknown was not visible.");
    assertEvidence(view.statuses.some((value) => value?.includes("Desatualizado")), "Stale evidence was not visible.");
    assertEvidence(!await evaluate(call, `document.documentElement.dataset.sampleExpired === "true"`), "The Unknown fixture expired during the automated smoke.");
    await clickControl(call, "control-complete");
  }
  await waitFor(call, `document.documentElement.dataset.finalised === "true"`, "the selected sample completion");
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
  await call("Page.navigate", { url: `${dashboardUrl}${evidencePageRoute}` });
  await waitFor(call, `document.querySelector("#agent-evidence-title") !== null`, "the evidence surface");
  await waitFor(
    call,
    `document.documentElement.dataset.reviewSample === ${JSON.stringify(sample)} && document.documentElement.dataset.evidenceStage === "agent-transport-ready"`,
    "the exact initial sample barrier");
  await enterTv(call);

  console.log(JSON.stringify({
    marker: "DBNOTIFIER_STATE06_HUMAN_REVIEW_READY",
    sample,
    automatedQualityGate: qualityGateAutomation,
    humanDecisionRecorded: false,
    operationalData: false,
  }));

  if (qualityGateAutomation) {
    await runQualityGateAutomation(call);
  } else {
    await waitFor(
      call,
      `document.documentElement.dataset.finalised === "true" || document.documentElement.dataset.sampleExpired === "true"`,
      "the explicit review completion",
      reviewBudgetMilliseconds);
    const expired = await evaluate(call, `document.documentElement.dataset.sampleExpired === "true"`);
    assertEvidence(!expired, "The selected human sample expired before completion.");
  }

  assertEvidence(network.externalOrigins.size === 0, `The dedicated review contacted an external origin: ${[...network.externalOrigins].join(", ")}`);
  await exitTv(call);
  const shutdown = await evaluate(call, `fetch(${JSON.stringify(shutdownRoute)}, {
    method: "POST",
    cache: "no-store",
    credentials: "same-origin"
  }).then((response) => response.status)`);
  assertEvidence(shutdown === 204, `The local review shutdown returned ${shutdown}.`);
  console.log(JSON.stringify({
    result: "completed",
    sample,
    automatedQualityGate: qualityGateAutomation,
    humanDecisionRecorded: false,
    observedExternalHttpRequests: 0,
    operationalData: false,
  }));
} finally {
  try {
    await exitTv(call);
  } catch {
    // The dedicated frame may already be closing after a bounded failure.
  }
  connection.close();
}
