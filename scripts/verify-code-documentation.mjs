/**
 * Project-wide documentation gate for hand-written, comment-capable sources.
 * Generated, immutable and strict-format exceptions are deliberately narrow.
 */
import { existsSync, readFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { basename, extname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(fileURLToPath(new URL("..", import.meta.url)));
const commentSyntax = new Map([
  [".cs", /^\s*(?:\/\/\/|\/\/|\/\*)/m], [".js", /^\s*(?:\/\/|\/\*|\*\/)/m],
  [".mjs", /^\s*(?:\/\/|\/\*|\*\/)/m], [".cjs", /^\s*(?:\/\/|\/\*|\*\/)/m], [".ts", /^\s*(?:\/\/|\/\*|\*\/)/m],
  [".tsx", /^\s*(?:\/\/|\/\*|\*\/)/m], [".css", /^\s*\/\*/m], [".html", /^\s*(?:<!doctype html>\s*)?<!--/im],
  [".xaml", /^\s*(?:<\?xml[^>]*>\s*)?<!--/m], [".xml", /^\s*(?:<\?xml[^>]*>\s*)?<!--/m], [".props", /^\s*<!--/m], [".csproj", /^\s*<!--/m],
  [".ps1", /^\s*#/m], [".psm1", /^\s*#/m], [".psd1", /^\s*#/m], [".sh", /^\s*#/m],
  [".py", /^\s*(?:\"\"\"|''')/m], [".yml", /^\s*#/m], [".yaml", /^\s*#/m],
  [".sql", /^\s*--/m], [".iss", /^\s*;/m],
  [".config", /^\s*(?:<\?xml[^>]*>\s*)?<!--/m],
]);
const ignoredDirectories = new Set([".git", ".dotnet", "bin", "dist", "node_modules", "obj"]);
const timestampedMigrations = new Set([
  "src/DBNotifier.Persistence.Agent.Sqlite/Migrations/20260712005330_InitialAgentSchema.cs",
  "src/DBNotifier.Persistence.Agent.Sqlite/Migrations/20260712005606_AddAgentStateConstraints.cs",
  "src/DBNotifier.Persistence.Agent.Sqlite/Migrations/20260712055252_AddCommandCompatibilityEnvelope.cs",
  "src/DBNotifier.Persistence.Server.PostgreSql/Migrations/20260712005336_InitialServerSchema.cs",
  "src/DBNotifier.Persistence.Server.PostgreSql/Migrations/20260712005611_AddServerStateConstraints.cs",
  "src/DBNotifier.Persistence.Server.PostgreSql/Migrations/20260712025130_EnforceAgentObservationSequence.cs",
  "src/DBNotifier.Persistence.Server.PostgreSql/Migrations/20260716142357_HardenObservationReconciliation.cs",
  "src/DBNotifier.Persistence.Server.PostgreSql/Migrations/20260718013515_IntegrateAgentFleetIdentity.cs",
]);

/** Returns tracked and non-ignored new files so a source root cannot silently escape the documentation gate before staging. */
function trackedFiles() {
  const output = execFileSync("git", ["ls-files", "--cached", "--others", "--exclude-standard", "-z"], { cwd: root });
  return output
    .toString("utf8")
    .split("\0")
    .filter(Boolean)
    .filter((path) => !path.split(/[\\/]/).some((part) => ignoredDirectories.has(part)))
    .map((path) => join(root, path));
}

/** Resolves comment syntax for ordinary extensions and exact extensionless configuration formats. */
function syntaxFor(path) {
  if (basename(path).toLowerCase() === ".editorconfig") return /^\s*#/m;
  return commentSyntax.get(extname(path).toLowerCase());
}

/** Tests only the opening source position, while retaining format flags such as case-insensitive HTML declarations. */
function hasModuleHeader(syntax, opening) {
  const headerSyntax = new RegExp(syntax.source, syntax.flags.replace("m", ""));
  return headerSyntax.test(opening);
}

/** Identifies generated or immutable C# files whose contents must not be rewritten. */
function isCSharpException(path) {
  const unixPath = relative(root, path).split(sep).join("/");
  return path.endsWith(".Designer.cs") || path.endsWith("ModelSnapshot.cs") || timestampedMigrations.has(unixPath);
}

const failures = [];
let checked = 0;
const csharpSyntax = commentSyntax.get(".cs");
if (hasModuleHeader(csharpSyntax, "using System;\n/// <inheritdoc />") ||
    !hasModuleHeader(csharpSyntax, "// Module purpose: Test fixture.\nusing System;")) {
  throw new Error("Documentation gate self-test failed: a later API comment must not substitute for a module header.");
}
for (const path of trackedFiles()) {
  if (!existsSync(path)) {
    failures.push(`${relative(root, path)}: tracked file is missing from the worktree`);
    continue;
  }
  const syntax = syntaxFor(path);
  if (!syntax || isCSharpException(path)) continue;
  checked += 1;
  const relativePath = relative(root, path).split(sep).join("/");
  const opening = readFileSync(path, "utf8").split(/\r?\n/).slice(0, 12).join("\n");
  if (!hasModuleHeader(syntax, opening)) failures.push(`${relativePath}: missing a module header before the first source construct`);
}

if (failures.length > 0) {
  process.stderr.write(`${failures.join("\n")}\nDocumentation gate failed: ${failures.length} issue(s).\n`);
  process.exit(1);
}
process.stdout.write(`Documentation gate passed for ${checked} comment-capable source files.\n`);
