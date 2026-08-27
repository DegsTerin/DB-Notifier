/**
 * Module purpose: Verifies repository-local Markdown links from the Git-owned file inventory.
 * Ignored and protected roots are never traversed, and external services remain outside the gate.
 */
import { execFileSync } from "node:child_process";
import { lstatSync, readFileSync } from "node:fs";
import { dirname, isAbsolute, join, posix, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(fileURLToPath(new URL("..", import.meta.url)));

/**
 * Runs one bounded shell-free Git inventory operation.
 * @param {string[]} arguments_ Exact arguments passed to Git.
 * @returns {string} Unquoted NUL-delimited inventory output.
 * @throws {Error} When Git cannot provide a complete bounded inventory.
 */
function gitInventoryOutput(arguments_) {
  try {
    return execFileSync(
      "git",
      arguments_,
      {
        cwd: root,
        encoding: "utf8",
        maxBuffer: 4 * 1024 * 1024,
        windowsHide: true,
      },
    );
  } catch {
    throw new Error("Markdown link gate could not obtain the Git-owned repository inventory.");
  }
}

/**
 * Splits one Git `-z` result without interpreting path characters.
 * @param {string} output NUL-delimited Git output.
 * @returns {string[]} Exact non-empty Git path or stage records.
 */
function splitGitOutput(output) {
  return output
    .split("\0")
    .filter(Boolean);
}

/**
 * Converts an absolute candidate to one canonical repository-relative Git path.
 * @param {string} absolutePath Candidate path resolved without physical access.
 * @returns {string} Forward-slash path beneath the repository root.
 * @throws {Error} When the candidate escapes the repository boundary.
 */
function repositoryRelativePath(absolutePath) {
  const candidate = relative(root, absolutePath);
  if (candidate === ".." || candidate.startsWith(`..${sep}`) || isAbsolute(candidate)) {
    throw new Error("Markdown destination is outside the repository boundary.");
  }
  return candidate.split(sep).join("/");
}

/**
 * Inspects an owned worktree path one component at a time without accepting symbolic traversal.
 * @param {string} absolutePath Candidate already admitted by the Git-owned inventory.
 * @param {string} subject Stable diagnostic subject that does not expose host paths.
 * @returns {import("node:fs").Stats} Status of the final non-symbolic component.
 * @throws {Error} When a component is unavailable or symbolic.
 */
function lstatWithoutSymbolicTraversal(absolutePath, subject) {
  const relativePath = repositoryRelativePath(absolutePath);
  const components = relativePath ? relativePath.split("/") : [];
  let currentPath = root;
  let currentStatus;

  for (const component of [null, ...components]) {
    if (component !== null) currentPath = join(currentPath, component);
    try {
      currentStatus = lstatSync(currentPath);
    } catch {
      throw new Error(`${subject} is unavailable in the worktree.`);
    }
    if (currentStatus.isSymbolicLink()) {
      throw new Error(`${subject} crosses a symbolic worktree path.`);
    }
  }

  return currentStatus;
}

/**
 * Builds the non-ignored Git-owned file/directory boundary and tracked symlink set.
 * @returns {{ markdownSources: { absolutePath: string, relativePath: string }[], ownedPaths: Set<string>, trackedSymlinks: Set<string> }} Bounded inventory used before physical access.
 */
function gitOwnedInventory() {
  const ignoredTrackedPaths = new Set(splitGitOutput(gitInventoryOutput([
    "ls-files",
    "--cached",
    "--ignored",
    "--exclude-standard",
    "-z",
  ])));
  const inventoryEntries = splitGitOutput(gitInventoryOutput([
    "ls-files",
    "--cached",
    "--others",
    "--exclude-standard",
    "-z",
  ]))
    .filter((path) => !ignoredTrackedPaths.has(path))
    .map((path) => {
      const absolutePath = resolve(root, path);
      const relativePath = repositoryRelativePath(absolutePath);
      if (relativePath !== path) {
        throw new Error("Markdown link gate received a non-canonical Git inventory path.");
      }
      return { absolutePath, relativePath };
    });
  const trackedSymlinks = new Set(
    splitGitOutput(gitInventoryOutput(["ls-files", "--stage", "-z"]))
      .filter((record) => record.startsWith("120000 "))
      .map((record) => record.slice(record.indexOf("\t") + 1)),
  );
  const ownedPaths = new Set([""]);
  for (const { relativePath } of inventoryEntries) {
    ownedPaths.add(relativePath);
    let parent = posix.dirname(relativePath);
    while (parent !== ".") {
      ownedPaths.add(parent);
      parent = posix.dirname(parent);
    }
  }

  return {
    markdownSources: inventoryEntries.filter(({ relativePath }) => relativePath.toLowerCase().endsWith(".md")),
    ownedPaths,
    trackedSymlinks,
  };
}

/**
 * Converts a Markdown destination into one lexical local candidate.
 * @param {string} sourcePath Absolute path of the owning Markdown source.
 * @param {string} rawDestination Destination captured from Markdown syntax.
 * @returns {string | null} Absolute local candidate, or null when no local check applies.
 * @throws {Error} When decoding fails or the destination uses a rooted Windows or UNC form.
 */
function resolveLocalDestination(sourcePath, rawDestination) {
  const destination = rawDestination.trim().replace(/^<|>$/g, "").split("#", 1)[0];
  if (!destination || destination.startsWith("#")) return null;
  if (/^[a-z]:/i.test(destination)) {
    throw new Error("Windows drive Markdown destinations are outside the repository boundary.");
  }
  if (/^[a-z][a-z0-9+.-]*:/i.test(destination)) return null;
  const decoded = decodeURIComponent(destination);
  if (/^[a-z]:/i.test(decoded)) {
    throw new Error("Windows drive Markdown destinations are outside the repository boundary.");
  }
  if (decoded.startsWith("//") || decoded.includes("\\")) {
    throw new Error("UNC or backslash Markdown destinations are outside the repository boundary.");
  }
  return resolve(decoded.startsWith("/") ? root : dirname(sourcePath), decoded.replace(/^\//, ""));
}

const failures = [];
let checkedFiles = 0;
let checkedLinks = 0;
const inventory = gitOwnedInventory();
for (const { absolutePath: sourcePath, relativePath: relativeSourcePath } of inventory.markdownSources) {
  if (inventory.trackedSymlinks.has(relativeSourcePath)) {
    failures.push(`${relativeSourcePath}: Markdown source must not be a tracked symbolic link`);
    continue;
  }
  let sourceStatus;
  try {
    sourceStatus = lstatWithoutSymbolicTraversal(sourcePath, "Markdown source");
  } catch (error) {
    failures.push(`${relativeSourcePath}: ${error.message}`);
    continue;
  }
  if (sourceStatus.isSymbolicLink() || !sourceStatus.isFile()) {
    failures.push(`${relativeSourcePath}: Markdown source must be a regular non-symbolic file`);
    continue;
  }
  checkedFiles += 1;
  const source = readFileSync(sourcePath, "utf8");
  const linkPattern = /!?\[[^\]]*\]\((<[^>]+>|[^\s)]+)(?:\s+(?:"[^"]*"|'[^']*'))?\)/g;
  for (const match of source.matchAll(linkPattern)) {
    let destination;
    try {
      destination = resolveLocalDestination(sourcePath, match[1]);
    } catch (error) {
      failures.push(`${relativeSourcePath}: invalid encoded destination ${match[1]} (${error.message})`);
      continue;
    }
    if (!destination) continue;
    checkedLinks += 1;
    let relativeDestination;
    try {
      relativeDestination = repositoryRelativePath(destination);
    } catch (error) {
      failures.push(`${relativeSourcePath}: invalid local destination ${match[1]} (${error.message})`);
      continue;
    }
    if (!inventory.ownedPaths.has(relativeDestination)) {
      failures.push(`${relativeSourcePath}: destination is absent from the non-ignored Git-owned inventory ${match[1]}`);
      continue;
    }
    if (inventory.trackedSymlinks.has(relativeDestination)) {
      failures.push(`${relativeSourcePath}: destination must not be a tracked symbolic link ${match[1]}`);
      continue;
    }
    try {
      lstatWithoutSymbolicTraversal(destination, "Markdown destination");
    } catch (error) {
      failures.push(`${relativeSourcePath}: ${error.message} ${match[1]}`);
    }
  }
}

if (failures.length > 0) {
  process.stderr.write(`${failures.join("\n")}\nMarkdown link gate failed: ${failures.length} issue(s).\n`);
  process.exit(1);
}
process.stdout.write(`Markdown link gate passed for ${checkedLinks} local links in ${checkedFiles} files.\n`);
