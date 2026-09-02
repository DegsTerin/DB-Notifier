/**
 * Exercises the cross-process STATE-05 evidence-root contract without starting a browser.
 * The cases model hosted-runner path differences while retaining exact ownership checks.
 */
import assert from "node:assert/strict";
import test from "node:test";
import { win32 } from "node:path";
import { resolveState05AuditEvidenceRoot } from "../../../scripts/state05-audit-evidence-root.mjs";

const runId = "0123456789abcdef0123456789abcdef";

test("accepts the exact Windows child when TEMP authorities and path casing differ", () => {
  const runnerRoot = `D:\\a\\_temp\\DBNotifier-Dashboard-Runner-${runId}`;
  const evidenceRoot = `d:\\A\\_TEMP\\dbnotifier-dashboard-runner-${runId}\\EVIDENCE`;
  const resolved = resolveState05AuditEvidenceRoot({
    TEMP: "C:\\Windows\\Temp",
    TMP: "D:\\a\\_temp",
    DBNOTIFIER_AUDIT_RUNNER_ROOT: runnerRoot,
    DBNOTIFIER_AUDIT_EVIDENCE_ROOT: evidenceRoot,
  }, "win32");

  assert.equal(resolved.toLowerCase(), win32.resolve(evidenceRoot).toLowerCase());
});

test("rejects an evidence sibling outside the exact runner child", () => {
  assert.throws(
    () => resolveState05AuditEvidenceRoot({
      DBNOTIFIER_AUDIT_RUNNER_ROOT: `/tmp/DBNotifier-Dashboard-Runner-${runId}`,
      DBNOTIFIER_AUDIT_EVIDENCE_ROOT: `/tmp/DBNotifier-Dashboard-Runner-${runId}/reports`,
    }, "linux"),
    /exact runner-owned child/,
  );
});

test("rejects an external evidence directory", () => {
  assert.throws(
    () => resolveState05AuditEvidenceRoot({
      DBNOTIFIER_AUDIT_RUNNER_ROOT: `/tmp/DBNotifier-Dashboard-Runner-${runId}`,
      DBNOTIFIER_AUDIT_EVIDENCE_ROOT: "/tmp/unrelated/evidence",
    }, "linux"),
    /exact runner-owned child/,
  );
});

test("rejects absent runner and evidence roots with distinct diagnostics", () => {
  assert.throws(
    () => resolveState05AuditEvidenceRoot({}, "linux"),
    /runner-owned STATE-05 root is required/,
  );
  assert.throws(
    () => resolveState05AuditEvidenceRoot({
      DBNOTIFIER_AUDIT_RUNNER_ROOT: `/tmp/DBNotifier-Dashboard-Runner-${runId}`,
    }, "linux"),
    /runner-owned STATE-05 evidence root is required/,
  );
});

test("rejects a runner without the exact GUID identity", () => {
  assert.throws(
    () => resolveState05AuditEvidenceRoot({
      DBNOTIFIER_AUDIT_RUNNER_ROOT: "/tmp/DBNotifier-Dashboard-Runner-not-a-guid",
      DBNOTIFIER_AUDIT_EVIDENCE_ROOT: "/tmp/DBNotifier-Dashboard-Runner-not-a-guid/evidence",
    }, "linux"),
    /root identity is invalid/,
  );
});
