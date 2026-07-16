/**
 * Verifies that the active Node.js and npm executables exactly match the repository manifest.
 * The gate reports versions only and never installs or updates toolchain components.
 */
import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(fileURLToPath(new URL("..", import.meta.url)));
const manifestPath = resolve(root, "src", "DBNotifier.Dashboard.Web", "package.json");
const manifest = JSON.parse(readFileSync(manifestPath, "utf8"));
const expectedNode = manifest.engines?.node;
const expectedNpm = manifest.engines?.npm;

if (typeof expectedNode !== "string" || typeof expectedNpm !== "string" || manifest.packageManager !== `npm@${expectedNpm}`) {
  throw new Error("The Dashboard manifest must declare exact Node.js and npm engine versions.");
}

const observedNode = process.versions.node;
const npmCliPath = process.env.npm_execpath;
if (typeof npmCliPath !== "string" || npmCliPath.length === 0) {
  throw new Error("Run this gate through 'npm run toolchain:verify' so the active npm CLI can be identified without shell resolution.");
}
const observedNpm = execFileSync(process.execPath, [npmCliPath, "--version"], { encoding: "utf8" }).trim();
if (observedNode !== expectedNode || observedNpm !== expectedNpm) {
  throw new Error(`Toolchain mismatch: required Node.js ${expectedNode}/npm ${expectedNpm}, observed ${observedNode}/${observedNpm}.`);
}

process.stdout.write(`Node.js ${observedNode} and npm ${observedNpm} toolchain verified.\n`);
