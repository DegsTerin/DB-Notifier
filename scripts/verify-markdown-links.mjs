/**
 * Verifies repository-local Markdown links without contacting external services.
 * Generated output and dependency directories remain outside the documentation corpus.
 */
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(fileURLToPath(new URL("..", import.meta.url)));
const ignoredDirectories = new Set([".git", ".dotnet", "bin", "dist", "node_modules", "obj"]);

/** Returns every Markdown file that belongs to the hand-written repository corpus. */
function walk(directory) {
  return readdirSync(directory).flatMap((name) => {
    if (ignoredDirectories.has(name)) return [];
    const path = join(directory, name);
    return statSync(path).isDirectory() ? walk(path) : path.toLowerCase().endsWith(".md") ? [path] : [];
  });
}

/** Converts a Markdown destination into a local path, or null when no file check applies. */
function resolveLocalDestination(sourcePath, rawDestination) {
  const destination = rawDestination.trim().replace(/^<|>$/g, "").split("#", 1)[0];
  if (!destination || destination.startsWith("#") || /^[a-z][a-z0-9+.-]*:/i.test(destination)) return null;
  const decoded = decodeURIComponent(destination);
  return resolve(decoded.startsWith("/") ? root : dirname(sourcePath), decoded.replace(/^\//, ""));
}

const failures = [];
let checkedFiles = 0;
let checkedLinks = 0;
for (const sourcePath of walk(root)) {
  checkedFiles += 1;
  const source = readFileSync(sourcePath, "utf8");
  const linkPattern = /!?\[[^\]]*\]\((<[^>]+>|[^\s)]+)(?:\s+(?:"[^"]*"|'[^']*'))?\)/g;
  for (const match of source.matchAll(linkPattern)) {
    let destination;
    try {
      destination = resolveLocalDestination(sourcePath, match[1]);
    } catch (error) {
      failures.push(`${relative(root, sourcePath)}: invalid encoded destination ${match[1]} (${error.message})`);
      continue;
    }
    if (!destination) continue;
    checkedLinks += 1;
    if (!existsSync(destination)) {
      failures.push(`${relative(root, sourcePath)}: missing ${match[1]}`);
    }
  }
}

if (failures.length > 0) {
  process.stderr.write(`${failures.join("\n")}\nMarkdown link gate failed: ${failures.length} issue(s).\n`);
  process.exit(1);
}
process.stdout.write(`Markdown link gate passed for ${checkedLinks} local links in ${checkedFiles} files.\n`);
