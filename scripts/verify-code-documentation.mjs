/**
 * Project-wide documentation gate for hand-written, comment-capable sources.
 * Generated, immutable and strict-format exceptions are deliberately narrow.
 */
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { extname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(fileURLToPath(new URL("..", import.meta.url)));
const scannedRoots = [".github", "build", "packaging", "scripts", "src", "tests", "desktop-wpf", "pixel-ui", "tray-app"];
const commentSyntax = new Map([
  [".cs", /^\s*(?:\/\/\/|\/\/|\/\*)/m], [".ts", /^\s*(?:\/\/|\/\*|\*\/)/m],
  [".tsx", /^\s*(?:\/\/|\/\*|\*\/)/m], [".css", /^\s*\/\*/m], [".html", /^\s*<!--/m],
  [".xaml", /^\s*<!--/m], [".xml", /^\s*<!--/m], [".props", /^\s*<!--/m], [".csproj", /^\s*<!--/m],
  [".ps1", /^\s*#/m], [".psm1", /^\s*#/m], [".psd1", /^\s*#/m], [".sh", /^\s*#/m],
  [".py", /^\s*(?:\"\"\"|''')/m], [".yml", /^\s*#/m], [".yaml", /^\s*#/m],
  [".sql", /^\s*--/m], [".iss", /^\s*;/m],
]);
const ignoredDirectories = new Set([".git", ".dotnet", "bin", "dist", "node_modules", "obj"]);
const appliedMigrations = new Set([
  "src/DBNotifier.Persistence.Agent.Sqlite/Migrations/20260712005330_InitialAgentSchema.cs",
  "src/DBNotifier.Persistence.Agent.Sqlite/Migrations/20260712005606_AddAgentStateConstraints.cs",
  "src/DBNotifier.Persistence.Agent.Sqlite/Migrations/20260712055252_AddCommandCompatibilityEnvelope.cs",
  "src/DBNotifier.Persistence.Server.PostgreSql/Migrations/20260712005336_InitialServerSchema.cs",
  "src/DBNotifier.Persistence.Server.PostgreSql/Migrations/20260712005611_AddServerStateConstraints.cs",
  "src/DBNotifier.Persistence.Server.PostgreSql/Migrations/20260712025130_EnforceAgentObservationSequence.cs",
]);

/** Recursively yields files while preserving the narrow generated-directory exclusions. */
function walk(directory) {
  if (!existsSync(directory)) return [];
  return readdirSync(directory).flatMap((name) => {
    if (ignoredDirectories.has(name)) return [];
    const path = join(directory, name);
    return statSync(path).isDirectory() ? walk(path) : [path];
  });
}

/** Identifies generated or immutable C# files whose contents must not be rewritten. */
function isCSharpException(path) {
  const unixPath = relative(root, path).split(sep).join("/");
  return path.endsWith(".Designer.cs") || path.endsWith("ModelSnapshot.cs") || appliedMigrations.has(unixPath);
}

const failures = [];
let checked = 0;
for (const scannedRoot of scannedRoots) {
  for (const path of walk(join(root, scannedRoot))) {
    const syntax = commentSyntax.get(extname(path).toLowerCase());
    if (!syntax || isCSharpException(path)) continue;
    checked += 1;
    const relativePath = relative(root, path).split(sep).join("/");
    const opening = readFileSync(path, "utf8").split(/\r?\n/).slice(0, 12).join("\n");
    if (!syntax.test(opening)) failures.push(`${relativePath}: missing an early module comment`);
  }
}

if (failures.length > 0) {
  process.stderr.write(`${failures.join("\n")}\nDocumentation gate failed: ${failures.length} issue(s).\n`);
  process.exit(1);
}
process.stdout.write(`Documentation gate passed for ${checked} comment-capable source files.\n`);
