/**
 * Verifies that the active Node.js and npm executables satisfy DB-Notifier's bounded stable ranges.
 * The gate validates manifest and lock metadata without installing or updating toolchain components.
 */
import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const canonicalPolicy = Object.freeze({
  nodeRange: ">=24.18.0 <25.0.0",
  npmRange: ">=11.16.0 <12.0.0",
  nvmSelector: "24",
});

/**
 * Parses one stable canonical major.minor.patch version.
 *
 * @param {string} value Candidate version text.
 * @param {string} context Sanitised description used in an error.
 * @returns {readonly [number, number, number]} Comparable numeric components.
 * @throws {Error} When the value is abbreviated, non-canonical, prerelease or numerically unsafe.
 */
function parseStableVersion(value, context) {
  const match = /^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$/.exec(value);
  if (match === null) {
    throw new Error(`${context} must be a stable canonical major.minor.patch version.`);
  }
  const components = match.slice(1).map(Number);
  if (components.some((component) => !Number.isSafeInteger(component))) {
    throw new Error(`${context} contains a numerically unsafe version component.`);
  }
  return /** @type {const} */ ([components[0], components[1], components[2]]);
}

/**
 * Parses the canonical inclusive-lower and exclusive-upper range syntax.
 *
 * @param {string} value Range text in the form >=x.y.z <x.y.z.
 * @param {string} context Sanitised description used in an error.
 * @returns {{ lower: readonly [number, number, number], upper: readonly [number, number, number] }} Parsed bounds.
 * @throws {Error} When the syntax or bound ordering is invalid.
 */
function parseCompatibleRange(value, context) {
  const match = /^>=(\S+) <(\S+)$/.exec(value);
  if (match === null) {
    throw new Error(`${context} must use the canonical '>=minimum <upper-bound' syntax.`);
  }
  const lower = parseStableVersion(match[1], `${context} lower bound`);
  const upper = parseStableVersion(match[2], `${context} upper bound`);
  if (compareVersions(lower, upper) >= 0) {
    throw new Error(`${context} must have an exclusive upper bound greater than its lower bound.`);
  }
  return { lower, upper };
}

/**
 * Compares two parsed stable versions without locale-dependent string ordering.
 *
 * @param {readonly [number, number, number]} left Left-hand version.
 * @param {readonly [number, number, number]} right Right-hand version.
 * @returns {-1 | 0 | 1} Ordering indicator.
 */
function compareVersions(left, right) {
  for (let index = 0; index < left.length; index += 1) {
    if (left[index] < right[index]) return -1;
    if (left[index] > right[index]) return 1;
  }
  return 0;
}

/**
 * Fails closed when an observed tool version falls outside its compatible range.
 *
 * @param {string} version Observed stable version.
 * @param {string} range Declared compatible range.
 * @param {string} toolName Sanitised tool name.
 * @returns {void}
 * @throws {Error} When the version is malformed or incompatible.
 */
function assertCompatibleVersion(version, range, toolName) {
  const candidate = parseStableVersion(version, `${toolName} observed version`);
  const policy = parseCompatibleRange(range, `${toolName} compatibility range`);
  if (compareVersions(candidate, policy.lower) < 0 || compareVersions(candidate, policy.upper) >= 0) {
    throw new Error(`${toolName} ${version} is outside the compatible range '${range}'.`);
  }
}

const root = resolve(fileURLToPath(new URL("..", import.meta.url)));
const dashboardRoot = resolve(root, "src", "DBNotifier.Dashboard.Web");
const manifest = JSON.parse(readFileSync(resolve(dashboardRoot, "package.json"), "utf8"));
const lock = JSON.parse(readFileSync(resolve(dashboardRoot, "package-lock.json"), "utf8"));
const nvmSelector = readFileSync(resolve(root, ".nvmrc"), "utf8").trim();
const nodeRange = manifest.engines?.node;
const npmRange = manifest.engines?.npm;
const rootLockEngines = lock.packages?.[""]?.engines;

if (nodeRange !== canonicalPolicy.nodeRange || npmRange !== canonicalPolicy.npmRange) {
  throw new Error("The Dashboard manifest diverges from the canonical compatible toolchain ranges.");
}
if (Object.hasOwn(manifest, "packageManager")) {
  throw new Error("The Dashboard manifest must not encode an exact packageManager version.");
}
if (
  manifest.devEngines?.runtime?.name !== "node" ||
  manifest.devEngines.runtime.version !== nodeRange ||
  manifest.devEngines.runtime.onFail !== "error" ||
  manifest.devEngines?.packageManager?.name !== "npm" ||
  manifest.devEngines.packageManager.version !== npmRange ||
  manifest.devEngines.packageManager.onFail !== "error"
) {
  throw new Error("The Dashboard devEngines contract must fail closed with the declared compatible ranges.");
}
if (nvmSelector !== canonicalPolicy.nvmSelector) {
  throw new Error(".nvmrc must select the compatible Node.js major release line.");
}
if (rootLockEngines?.node !== nodeRange || rootLockEngines?.npm !== npmRange) {
  throw new Error("The package-lock.json root engine metadata diverges from package.json.");
}

const observedNode = process.versions.node;
const npmCliPath = process.env.npm_execpath;
if (typeof npmCliPath !== "string" || npmCliPath.length === 0) {
  throw new Error("Run this gate through 'npm run toolchain:verify' so the active npm CLI can be identified without shell resolution.");
}
const observedNpm = execFileSync(process.execPath, [npmCliPath, "--version"], { encoding: "utf8" }).trim();
assertCompatibleVersion(observedNode, nodeRange, "Node.js");
assertCompatibleVersion(observedNpm, npmRange, "npm");

process.stdout.write(
  `Node.js ${observedNode} satisfies '${nodeRange}'; npm ${observedNpm} satisfies '${npmRange}'.\n`,
);
