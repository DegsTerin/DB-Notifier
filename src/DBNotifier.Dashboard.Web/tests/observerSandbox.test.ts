/** Module purpose: Verifies O4 browser activation, strict projection validation and fail-closed adversarial handling. */
import assert from "node:assert/strict";
import test from "node:test";
import {
  isObserverSandboxEnabled,
  observerProjectionSchemaVersion,
  validateObserverProjectionPackage,
} from "../src/observerSandbox.ts";
import {
  observerMessageCatalogue,
  observerText,
} from "../src/observerSandboxMessages.ts";

const now = new Date("2026-07-24T12:00:00.000Z");
const digest = "a".repeat(64);
const guid = "a4a40000-0000-4000-8000-000000000001";

/** Builds one complete package using only bounded synthetic values. */
function packageFixture() {
  return {
    projectionDigest: digest,
    payload: {
      schemaVersion: observerProjectionSchemaVersion,
      projectionId: digest,
      generatedAtUtc: "2026-07-24T11:59:30.000+00:00",
      validUntilUtc: "2026-07-24T12:04:30.000+00:00",
      processingCompleted: true,
      synthetic: true,
      operational: false,
      productionRepresentative: false,
      isAuthorising: false,
      revoked: false,
      superseded: false,
      activationState: "None",
      sourceLabel: "synthetic-local-o2-o3-sandbox",
      trace: {
        o2EnvelopeDigest: digest,
        o3ResultDigest: digest,
        policyDigest: digest,
        corpusManifestDigest: digest,
        corpusRevision: 1,
        evaluationId: digest,
      },
      signals: [{
        signalId: digest,
        ruleId: "o2a.duration-threshold",
        ruleVersion: "1.0.0",
        metricKey: "observer.health.duration.degraded",
        unit: "milliseconds",
        disposition: "Detected",
        severity: "Warning",
        evaluatedAtUtc: "2026-07-23T01:00:00.000Z",
        observedAtUtc: "2026-07-23T01:00:00.000Z",
        validUntilUtc: "2026-07-23T01:05:00.000Z",
        freshness: "Stale",
        observedValue: 100,
        threshold: 50,
        evidenceIds: [guid],
        limitations: [],
      }],
      forecasts: {
        state: "Unknown",
        code: "o4.forecast.complete_result_unavailable",
        limitations: ["o4.limitation.forecast_unavailable"],
      },
      limitations: ["o4.limitation.synthetic_not_production"],
    },
  };
}

test("observer sandbox requires exact flag and HTTPS loopback", () => {
  const location = { protocol: "https:", hostname: "127.0.0.1", origin: "https://127.0.0.1:4443" };
  assert.equal(isObserverSandboxEnabled({ VITE_DB_NOTIFIER_OBSERVER_SANDBOX: "local-test" }, location), true);
  assert.equal(isObserverSandboxEnabled({}, location), false);
  assert.equal(
    isObserverSandboxEnabled(
      { VITE_DB_NOTIFIER_OBSERVER_SANDBOX: "local-test" },
      { ...location, protocol: "http:", origin: "http://127.0.0.1:4443" },
    ),
    false,
  );
  assert.equal(
    isObserverSandboxEnabled(
      { VITE_DB_NOTIFIER_OBSERVER_SANDBOX: "local-test" },
      { ...location, hostname: "example.test", origin: "https://example.test" },
    ),
    false,
  );
});

test("Observer sandbox messages retain exact locale parity and bounded placeholders", () => {
  const ptBR = observerMessageCatalogue("pt-BR");
  const enGB = observerMessageCatalogue("en-GB");
  assert.deepEqual(Object.keys(ptBR), Object.keys(enGB));
  assert.equal(observerText("pt-BR", "EvidenceCount", 2), "2 evidência(s) vinculada(s)");
  assert.equal(observerText("en-GB", "EvidenceCount", 2), "2 linked evidence item(s)");
  for (const value of [...Object.values(ptBR), ...Object.values(enGB)]) {
    assert.equal(/[\u0000-\u001F\u007F\u202A-\u202E\u2066-\u2069]/u.test(value), false);
  }
});

test("complete synthetic package retains traceability and non-authority", () => {
  const parsed = validateObserverProjectionPackage(packageFixture(), now);
  assert.equal(parsed.payload.activationState, "None");
  assert.equal(parsed.payload.isAuthorising, false);
  assert.equal(parsed.payload.productionRepresentative, false);
  assert.equal(parsed.payload.signals[0].freshness, "Stale");
  assert.equal(parsed.payload.forecasts.state, "Unknown");
  assert.equal(parsed.payload.trace.policyDigest, digest);
  assert.equal(parsed.payload.trace.corpusManifestDigest, digest);
  assert.equal(parsed.payload.generatedAtUtc, "2026-07-24T11:59:30.000Z");
});

test("incomplete, expired, revoked, superseded, future and mismatched shapes fail closed", () => {
  const mutations: Array<(candidate: ReturnType<typeof packageFixture>) => void> = [
    (candidate) => { candidate.payload.processingCompleted = false; },
    (candidate) => { candidate.payload.validUntilUtc = now.toISOString(); },
    (candidate) => { candidate.payload.revoked = true; },
    (candidate) => { candidate.payload.superseded = true; },
    (candidate) => { candidate.payload.generatedAtUtc = "2026-07-24T12:02:00.000Z"; },
    (candidate) => { candidate.payload.activationState = "Observer"; },
    (candidate) => { candidate.payload.signals = []; },
    (candidate) => { candidate.payload.forecasts.state = "Available"; },
    (candidate) => { candidate.payload.trace.policyDigest = "not-a-digest"; },
    (candidate) => { candidate.payload.signals[0].evidenceIds = []; },
  ];
  for (const mutate of mutations) {
    const candidate = packageFixture();
    mutate(candidate);
    assert.throws(
      () => validateObserverProjectionPackage(candidate, now),
      /observer\.projection\.failed_closed/,
    );
  }
});

test("hostile text and non-finite numeric values fail closed", () => {
  const control = packageFixture();
  control.payload.sourceLabel = "synthetic\u0000source";
  assert.throws(() => validateObserverProjectionPackage(control, now));

  const bidi = packageFixture();
  bidi.payload.signals[0].ruleId = "safe\u202Eunsafe";
  assert.throws(() => validateObserverProjectionPackage(bidi, now));

  const nonFinite = packageFixture();
  nonFinite.payload.signals[0].threshold = Number.NaN;
  assert.throws(() => validateObserverProjectionPackage(nonFinite, now));
});
