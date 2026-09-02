/**
 * Validates the exact runner-to-Node evidence-directory contract for the STATE-05 Dashboard audit.
 * The PowerShell runner remains the sole authority for temporary-root creation and cleanup.
 */
import { posix, win32 } from "node:path";

const runnerLeafPattern = /^DBNotifier-Dashboard-Runner-[0-9a-f]{32}$/;
const runnerLeafPatternIgnoreCase = /^DBNotifier-Dashboard-Runner-[0-9a-f]{32}$/i;

/**
 * Resolves the exact evidence child owned by the PowerShell runner.
 *
 * @param {Record<string, string | undefined>} environment Environment values passed to the Node audit.
 * @param {string} platform Node platform identifier used to select path and case semantics.
 * @returns {string} Absolute evidence-directory path authorised by the runner.
 * @throws {Error} When either path is absent, relative, has an invalid runner identity or is not the exact child.
 */
export function resolveState05AuditEvidenceRoot(environment, platform = process.platform) {
  const configuredRunnerRoot = environment.DBNOTIFIER_AUDIT_RUNNER_ROOT;
  if (!configuredRunnerRoot) {
    throw new Error("The runner-owned STATE-05 root is required.");
  }

  const configuredEvidenceRoot = environment.DBNOTIFIER_AUDIT_EVIDENCE_ROOT;
  if (!configuredEvidenceRoot) {
    throw new Error("The runner-owned STATE-05 evidence root is required.");
  }

  const pathApi = platform === "win32" ? win32 : posix;
  if (!pathApi.isAbsolute(configuredRunnerRoot) || !pathApi.isAbsolute(configuredEvidenceRoot)) {
    throw new Error("The runner-owned STATE-05 paths must be absolute.");
  }

  const runnerRoot = pathApi.resolve(configuredRunnerRoot);
  const evidenceRoot = pathApi.resolve(configuredEvidenceRoot);
  const runnerLeaf = pathApi.basename(runnerRoot);
  const hasValidIdentity = platform === "win32"
    ? runnerLeafPatternIgnoreCase.test(runnerLeaf)
    : runnerLeafPattern.test(runnerLeaf);
  if (!hasValidIdentity) {
    throw new Error("The runner-owned STATE-05 root identity is invalid.");
  }

  const expectedEvidenceRoot = pathApi.join(runnerRoot, "evidence");
  const comparableEvidenceRoot = platform === "win32" ? evidenceRoot.toLowerCase() : evidenceRoot;
  const comparableExpectedRoot = platform === "win32" ? expectedEvidenceRoot.toLowerCase() : expectedEvidenceRoot;
  if (comparableEvidenceRoot !== comparableExpectedRoot) {
    throw new Error("The STATE-05 evidence root must be the exact runner-owned child.");
  }

  return evidenceRoot;
}
