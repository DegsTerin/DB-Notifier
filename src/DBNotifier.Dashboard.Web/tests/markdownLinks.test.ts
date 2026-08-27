/** Module purpose: Proves Markdown and SQLite inventories respect Git ignore boundaries without reading protected roots. */
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import {
  copyFileSync,
  mkdirSync,
  mkdtempSync,
  rmSync,
  writeFileSync,
} from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const repositoryRoot = resolve(fileURLToPath(new URL("../../../", import.meta.url)));
const markdownVerifier = join(repositoryRoot, "scripts", "verify-markdown-links.mjs");
const repositoryIgnore = join(repositoryRoot, ".gitignore");

/**
 * Runs one shell-free child command with bounded captured output.
 * @param command Exact executable name or path.
 * @param arguments_ Ordered arguments passed without shell interpretation.
 * @param workingDirectory Isolated directory that owns the child operation.
 * @returns The exit status and bounded standard streams captured by Node.
 * @throws {Error} When the child process cannot be created.
 */
function run(command: string, arguments_: readonly string[], workingDirectory: string) {
  const result = spawnSync(command, arguments_, {
    cwd: workingDirectory,
    encoding: "utf8",
    maxBuffer: 1024 * 1024,
    shell: false,
    windowsHide: true,
  });
  if (result.error) throw result.error;
  return result;
}

/**
 * Creates an isolated Git repository with deterministic case-insensitive ignore matching.
 * @param root Exact temporary root that owns the new repository.
 * @returns Nothing; assertions fail closed when Git initialisation is unavailable.
 */
function initialiseRepository(root: string) {
  assert.equal(run("git", ["init", "--quiet"], root).status, 0);
  assert.equal(run("git", ["config", "core.ignorecase", "true"], root).status, 0);
}

/**
 * Removes only the exact temporary root created by the current test.
 * @param root Exact temporary directory selected for deletion.
 * @param expectedPrefix Trusted prefix returned to the test's `mkdtempSync` call.
 * @returns Nothing; an ownership mismatch fails before recursive removal.
 */
function removeTemporaryRoot(root: string, expectedPrefix: string) {
  assert.ok(root.startsWith(expectedPrefix));
  rmSync(root, { recursive: true, force: true });
}

test("the SQLite source project remains visible while runtime state stays ignored", () => {
  const temporaryPrefix = join(tmpdir(), "dbnotifier-ignore-boundary-");
  const temporaryRoot = mkdtempSync(temporaryPrefix);
  try {
    initialiseRepository(temporaryRoot);
    copyFileSync(repositoryIgnore, join(temporaryRoot, ".gitignore"));

    const sourceProbe = run(
      "git",
      ["check-ignore", "--no-index", "--quiet", "--", "src/DBNotifier.Persistence.Agent.Sqlite/FutureMigration.cs"],
      temporaryRoot,
    );
    assert.equal(sourceProbe.status, 1, "The canonical SQLite source tree must not be ignored.");

    for (const runtimeName of [
      "state.db",
      "state.db-journal",
      "state.db-shm",
      "state.db-wal",
      "state.sqlite",
      "state.sqlite-journal",
      "state.sqlite-shm",
      "state.sqlite-wal",
      "state.sqlite3",
      "state.sqlite3-journal",
      "state.sqlite3-shm",
      "state.sqlite3-wal",
    ]) {
      const runtimePath = `src/DBNotifier.Persistence.Agent.Sqlite/${runtimeName}`;
      assert.equal(
        run("git", ["check-ignore", "--no-index", "--quiet", "--", runtimePath], temporaryRoot).status,
        0,
        `${runtimePath} must remain ignored runtime state.`,
      );
    }
  } finally {
    removeTemporaryRoot(temporaryRoot, temporaryPrefix);
  }
});

test("the Markdown gate checks Git-owned new files without traversing ignored protected roots", () => {
  const temporaryPrefix = join(tmpdir(), "dbnotifier-markdown-inventory-");
  const temporaryRoot = mkdtempSync(temporaryPrefix);
  try {
    initialiseRepository(temporaryRoot);
    mkdirSync(join(temporaryRoot, "scripts"));
    mkdirSync(join(temporaryRoot, "docs"));
    mkdirSync(join(temporaryRoot, "mysql-notifier-1.1.8-src"));
    mkdirSync(join(temporaryRoot, "ignored-external"));
    copyFileSync(markdownVerifier, join(temporaryRoot, "scripts", "verify-markdown-links.mjs"));
    writeFileSync(
      join(temporaryRoot, ".gitignore"),
      "mysql-notifier-*-src/\nignored-external/\n",
      "utf8",
    );
    writeFileSync(join(temporaryRoot, "README.md"), "[Guide](docs/guide.md)\n", "utf8");
    writeFileSync(join(temporaryRoot, "docs", "guide.md"), "# Guide\n", "utf8");
    writeFileSync(join(temporaryRoot, "notes.md"), "# New note\n", "utf8");
    writeFileSync(
      join(temporaryRoot, "mysql-notifier-1.1.8-src", "sentinel.md"),
      "[Must not be read](missing-protected.md)\n",
      "utf8",
    );
    writeFileSync(
      join(temporaryRoot, "ignored-external", "sentinel.md"),
      "[Must not be read](missing-external.md)\n",
      "utf8",
    );
    assert.equal(
      run("git", ["add", "--", ".gitignore", "README.md", "docs/guide.md"], temporaryRoot).status,
      0,
    );

    const passingResult = run(
      process.execPath,
      [join(temporaryRoot, "scripts", "verify-markdown-links.mjs")],
      temporaryRoot,
    );
    assert.equal(passingResult.status, 0, passingResult.stderr);
    assert.match(passingResult.stdout, /passed for 1 local links in 3 files/);
    assert.doesNotMatch(passingResult.stdout + passingResult.stderr, /sentinel|mysql-notifier|ignored-external/);

    writeFileSync(join(temporaryRoot, "notes.md"), "[Missing](missing.md)\n", "utf8");
    const failingResult = run(
      process.execPath,
      [join(temporaryRoot, "scripts", "verify-markdown-links.mjs")],
      temporaryRoot,
    );
    assert.equal(failingResult.status, 1);
    assert.match(failingResult.stderr, /notes[.]md: destination is absent from the non-ignored Git-owned inventory missing[.]md/);
    assert.doesNotMatch(failingResult.stderr, /missing-protected|missing-external/);

    writeFileSync(
      join(temporaryRoot, "notes.md"),
      "[Protected](mysql-notifier-1.1.8-src/sentinel.md)\n",
      "utf8",
    );
    const protectedDestinationResult = run(
      process.execPath,
      [join(temporaryRoot, "scripts", "verify-markdown-links.mjs")],
      temporaryRoot,
    );
    assert.equal(protectedDestinationResult.status, 1);
    assert.match(protectedDestinationResult.stderr, /absent from the non-ignored Git-owned inventory/);

    writeFileSync(join(temporaryRoot, "notes.md"), "[Outside](../../outside.md)\n", "utf8");
    const outsideResult = run(
      process.execPath,
      [join(temporaryRoot, "scripts", "verify-markdown-links.mjs")],
      temporaryRoot,
    );
    assert.equal(outsideResult.status, 1);
    assert.match(outsideResult.stderr, /outside the repository boundary/);

    writeFileSync(join(temporaryRoot, "notes.md"), "[UNC](//server/share/readme.md)\n", "utf8");
    const uncResult = run(
      process.execPath,
      [join(temporaryRoot, "scripts", "verify-markdown-links.mjs")],
      temporaryRoot,
    );
    assert.equal(uncResult.status, 1);
    assert.match(uncResult.stderr, /UNC or backslash Markdown destinations are outside the repository boundary/);

    for (const [destination, expectedFailure] of [
      ["C:\\outside.md", /Windows drive Markdown destinations are outside the repository boundary/],
      ["C:/outside.md", /Windows drive Markdown destinations are outside the repository boundary/],
      ["C%3A%2Foutside.md", /Windows drive Markdown destinations are outside the repository boundary/],
      ["docs\\guide.md", /UNC or backslash Markdown destinations are outside the repository boundary/],
      ["docs%5Cguide.md", /UNC or backslash Markdown destinations are outside the repository boundary/],
    ] as const) {
      writeFileSync(join(temporaryRoot, "notes.md"), `[Rejected](${destination})\n`, "utf8");
      const rejectedDestinationResult = run(
        process.execPath,
        [join(temporaryRoot, "scripts", "verify-markdown-links.mjs")],
        temporaryRoot,
      );
      assert.equal(rejectedDestinationResult.status, 1);
      assert.match(rejectedDestinationResult.stderr, expectedFailure);
    }

    writeFileSync(
      join(temporaryRoot, "linked.md"),
      "[Must not be read](mysql-notifier-1.1.8-src/sentinel.md)\n",
      "utf8",
    );
    const linkedBlob = run("git", ["hash-object", "-w", "--", "linked.md"], temporaryRoot).stdout.trim();
    assert.match(linkedBlob, /^[0-9a-f]{40,64}$/);
    assert.equal(
      run(
        "git",
        ["update-index", "--add", "--cacheinfo", `120000,${linkedBlob},linked.md`],
        temporaryRoot,
      ).status,
      0,
    );
    writeFileSync(join(temporaryRoot, "notes.md"), "[Linked](linked.md)\n", "utf8");
    const symbolicSourceResult = run(
      process.execPath,
      [join(temporaryRoot, "scripts", "verify-markdown-links.mjs")],
      temporaryRoot,
    );
    assert.equal(symbolicSourceResult.status, 1);
    assert.match(symbolicSourceResult.stderr, /linked[.]md: Markdown source must not be a tracked symbolic link/);
    assert.match(symbolicSourceResult.stderr, /notes[.]md: destination must not be a tracked symbolic link linked[.]md/);
    assert.doesNotMatch(symbolicSourceResult.stderr, /sentinel/);
  } finally {
    removeTemporaryRoot(temporaryRoot, temporaryPrefix);
  }
});
