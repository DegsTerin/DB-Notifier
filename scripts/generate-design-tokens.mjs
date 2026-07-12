/**
 * Generates deterministic CSS and WPF resources from the canonical DB-Notifier design tokens.
 * Validation fails closed on schema, reference, parity or generated-file drift.
 */
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { createHash } from "node:crypto";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(fileURLToPath(new URL("..", import.meta.url)));
const schemaVersion = "dbnotifier.design-tokens.v1";
const allowedTypes = new Set(["color", "dimension", "duration", "fontFamily", "fontWeight", "shadow", "cubicBezier", "number"]);
const sourcePaths = {
  core: join(root, "design-system/tokens/core.tokens.json"),
  light: join(root, "design-system/tokens/semantic.light.tokens.json"),
  dark: join(root, "design-system/tokens/semantic.dark.tokens.json"),
  components: join(root, "design-system/tokens/components.tokens.json"),
};
const outputPaths = {
  css: join(root, "src/DBNotifier.Dashboard.Web/src/generated/design-tokens.css"),
  coreXaml: join(root, "src/DBNotifier.Desktop.Wpf/Generated/DesignTokens.Core.xaml"),
  lightXaml: join(root, "src/DBNotifier.Desktop.Wpf/Generated/DesignTokens.Light.xaml"),
  darkXaml: join(root, "src/DBNotifier.Desktop.Wpf/Generated/DesignTokens.Dark.xaml"),
};

/** Loads one strict JSON token set and reports its source path when parsing fails. */
function loadTokenSet(path) {
  try {
    return JSON.parse(readFileSync(path, "utf8"));
  } catch (error) {
    throw new Error(`Cannot parse ${path}: ${error.message}`);
  }
}

/** Validates the stable token envelope and every token's minimal type/value contract. */
function validateTokenSet(set, expectedName, path) {
  const envelopeKeys = Object.keys(set).sort();
  if (JSON.stringify(envelopeKeys) !== JSON.stringify(["$schema", "schemaVersion", "tokenSet", "tokens"].sort())) {
    throw new Error(`${path}: token envelope contains missing or unknown properties.`);
  }
  if (set.schemaVersion !== schemaVersion) throw new Error(`${path}: unsupported schemaVersion.`);
  if (set.tokenSet !== expectedName) throw new Error(`${path}: expected tokenSet ${expectedName}.`);
  if (!set.$schema || !existsSync(resolve(dirname(path), set.$schema))) throw new Error(`${path}: schema reference is unavailable.`);
  if (!set.tokens || typeof set.tokens !== "object" || Array.isArray(set.tokens)) throw new Error(`${path}: tokens must be an object.`);
  for (const [key, token] of Object.entries(set.tokens)) {
    if (!/^[a-z][a-z0-9]*(\.[a-z0-9]+)*$/.test(key)) throw new Error(`${path}: invalid token name ${key}.`);
    if (!token || typeof token !== "object" || !allowedTypes.has(token.type)) throw new Error(`${path}: invalid type for ${key}.`);
    if (JSON.stringify(Object.keys(token).sort()) !== JSON.stringify(["type", "value"])) throw new Error(`${path}: ${key} contains missing or unknown properties.`);
    if (typeof token.value !== "string" && typeof token.value !== "number") throw new Error(`${path}: invalid value for ${key}.`);
    if (token.type === "color" && typeof token.value === "string" && !/^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$|^\{[^}]+\}$/.test(token.value)) {
      throw new Error(`${path}: ${key} is not a six/eight-digit colour or alias.`);
    }
    if (token.type === "dimension" && typeof token.value === "string" && !/^\d+(\.\d+)?px$|^\{[^}]+\}$/.test(token.value)) {
      throw new Error(`${path}: ${key} is not a pixel dimension or alias.`);
    }
    if (token.type === "duration" && typeof token.value === "string" && !/^\d+ms$|^\{[^}]+\}$/.test(token.value)) {
      throw new Error(`${path}: ${key} is not a millisecond duration or alias.`);
    }
    if ((token.type === "fontWeight" || token.type === "number") && typeof token.value !== "number" && !/^\{[^}]+\}$/.test(token.value)) {
      throw new Error(`${path}: ${key} must be numeric or an alias.`);
    }
  }
}

/** Hashes the schema and canonical sources so generated adapters expose exact provenance. */
function calculateSourceChecksum() {
  const hash = createHash("sha256");
  const paths = [join(root, "design-system/schema/design-tokens.schema.json"), ...Object.values(sourcePaths)].sort();
  for (const path of paths) hash.update(readFileSync(path));
  return hash.digest("hex");
}

/** Ensures both semantic themes expose the same names and types. */
function validateThemeParity(light, dark) {
  const lightKeys = Object.keys(light).sort();
  const darkKeys = Object.keys(dark).sort();
  if (JSON.stringify(lightKeys) !== JSON.stringify(darkKeys)) throw new Error("Light and Dark semantic token names differ.");
  for (const key of lightKeys) {
    if (light[key].type !== dark[key].type) throw new Error(`Light and Dark types differ for ${key}.`);
  }
}

/** Resolves aliases recursively, rejecting missing, circular or type-incompatible references. */
function resolveTokens(core, semantic, components) {
  const combined = { ...core, ...semantic, ...components };
  const resolved = new Map();

  /** Resolves a single key while retaining the traversal stack for circular-reference evidence. */
  function resolveKey(key, stack = []) {
    if (resolved.has(key)) return resolved.get(key);
    const token = combined[key];
    if (!token) throw new Error(`Token reference ${key} does not exist.`);
    if (stack.includes(key)) throw new Error(`Circular token reference: ${[...stack, key].join(" -> ")}.`);
    const match = typeof token.value === "string" ? token.value.match(/^\{([^}]+)\}$/) : null;
    if (!match) {
      resolved.set(key, { type: token.type, value: token.value });
      return resolved.get(key);
    }
    const target = combined[match[1]];
    if (!target) throw new Error(`${key}: referenced token ${match[1]} does not exist.`);
    if (target.type !== token.type) throw new Error(`${key}: alias type ${token.type} does not match ${target.type}.`);
    const result = resolveKey(match[1], [...stack, key]);
    resolved.set(key, { type: token.type, value: result.value });
    return resolved.get(key);
  }

  for (const key of Object.keys(combined)) resolveKey(key);
  return Object.fromEntries([...resolved.entries()]);
}

/** Converts a dotted canonical token name into its CSS custom-property name. */
function cssName(key) {
  return `--db-${key.replaceAll(".", "-")}`;
}

/** Converts a dotted canonical token name into a stable PascalCase WPF resource key. */
function wpfName(key) {
  return key.split(".").map((part) => `${part[0].toUpperCase()}${part.slice(1)}`).join("");
}

/** Escapes text before insertion into generated XML content. */
function escapeXml(value) {
  return String(value).replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;");
}

/** Formats a millisecond duration as a WPF TimeSpan-compatible duration string. */
function wpfDuration(value) {
  const match = String(value).match(/^(\d+)ms$/);
  if (!match) throw new Error(`WPF duration must use milliseconds: ${value}.`);
  const milliseconds = Number(match[1]);
  return milliseconds === 0 ? "0:0:0" : `0:0:0.${String(milliseconds).padStart(3, "0")}`;
}

/** Converts canonical CSS RRGGBBAA colours to WPF AARRGGBB while preserving six-digit colours. */
function wpfColor(value) {
  const text = String(value);
  return text.length === 9 ? `#${text.slice(7, 9)}${text.slice(1, 7)}` : text;
}

/** Renders one WPF primitive or semantic resource with a stable type-specific suffix. */
function renderWpfResource(key, token, semantic) {
  const name = wpfName(key);
  const value = escapeXml(token.value);
  if (token.type === "color") {
    const colour = escapeXml(wpfColor(token.value));
    return semantic
      ? `  <SolidColorBrush x:Key="${name}Brush" Color="${colour}" />`
      : `  <Color x:Key="${name}Color">${colour}</Color>`;
  }
  if (token.type === "dimension") return `  <sys:Double x:Key="${name}">${Number.parseFloat(token.value)}</sys:Double>`;
  if (token.type === "duration") return `  <Duration x:Key="${name}Duration">${wpfDuration(token.value)}</Duration>`;
  if (token.type === "fontFamily") return `  <FontFamily x:Key="${name}">${value}</FontFamily>`;
  if (token.type === "fontWeight" || token.type === "number") return `  <sys:Double x:Key="${name}">${token.value}</sys:Double>`;
  return `  <sys:String x:Key="${name}">${value}</sys:String>`;
}

/** Produces the shared CSS primitive block and both resolved semantic theme blocks. */
function generateCss(core, light, dark, components, checksum) {
  const lightResolved = resolveTokens(core, light, components);
  const darkResolved = resolveTokens(core, dark, components);
  const lines = [
    `/* Generated from dbnotifier.design-tokens.v1; source sha256:${checksum}. Do not edit directly. */`,
    ":root {",
    ...Object.keys(core).sort().map((key) => `  ${cssName(key)}: ${core[key].value};`),
    "}",
    "",
    ':root[data-theme="light"] {',
    ...[...Object.keys(light), ...Object.keys(components)].sort().map((key) => `  ${cssName(key)}: ${lightResolved[key].value};`),
    "  color-scheme: light;",
    "}",
    "",
    ':root[data-theme="dark"] {',
    ...[...Object.keys(dark), ...Object.keys(components)].sort().map((key) => `  ${cssName(key)}: ${darkResolved[key].value};`),
    "  color-scheme: dark;",
    "}",
    "",
  ];
  return lines.join("\n");
}

/** Produces a well-formed WPF ResourceDictionary for core or resolved theme resources. */
function generateXaml(tokens, semantic, checksum) {
  return [
    `<!-- Generated from dbnotifier.design-tokens.v1; source sha256:${checksum}. Do not edit directly. -->`,
    '<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"',
    '                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"',
    '                    xmlns:sys="clr-namespace:System;assembly=System.Runtime">',
    ...Object.keys(tokens).sort().map((key) => renderWpfResource(key, tokens[key], semantic)),
    "</ResourceDictionary>",
    "",
  ].join("\n");
}

/** Compares generated content in verification mode or writes deterministic local output. */
function emit(path, content, verify) {
  if (verify) {
    if (!existsSync(path) || readFileSync(path, "utf8") !== content) throw new Error(`${path}: generated design tokens are stale.`);
    return;
  }
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, content, "utf8");
}

/** Validates every source set and emits or verifies all platform adapters. */
function main() {
  const verify = process.argv.includes("--verify");
  const sets = Object.fromEntries(Object.entries(sourcePaths).map(([name, path]) => [name, loadTokenSet(path)]));
  validateTokenSet(sets.core, "core", sourcePaths.core);
  validateTokenSet(sets.light, "semantic.light", sourcePaths.light);
  validateTokenSet(sets.dark, "semantic.dark", sourcePaths.dark);
  validateTokenSet(sets.components, "components", sourcePaths.components);
  validateThemeParity(sets.light.tokens, sets.dark.tokens);
  const checksum = calculateSourceChecksum();

  const lightResolved = resolveTokens(sets.core.tokens, sets.light.tokens, sets.components.tokens);
  const darkResolved = resolveTokens(sets.core.tokens, sets.dark.tokens, sets.components.tokens);
  const lightTheme = Object.fromEntries([...Object.keys(sets.light.tokens), ...Object.keys(sets.components.tokens)].map((key) => [key, lightResolved[key]]));
  const darkTheme = Object.fromEntries([...Object.keys(sets.dark.tokens), ...Object.keys(sets.components.tokens)].map((key) => [key, darkResolved[key]]));

  emit(outputPaths.css, generateCss(sets.core.tokens, sets.light.tokens, sets.dark.tokens, sets.components.tokens, checksum), verify);
  emit(outputPaths.coreXaml, generateXaml(sets.core.tokens, false, checksum), verify);
  emit(outputPaths.lightXaml, generateXaml(lightTheme, true, checksum), verify);
  emit(outputPaths.darkXaml, generateXaml(darkTheme, true, checksum), verify);
  process.stdout.write(`Design tokens ${verify ? "verified" : "generated"} from ${schemaVersion}.\n`);
}

try {
  main();
} catch (error) {
  process.stderr.write(`${error.message}\n`);
  process.exitCode = 1;
}
