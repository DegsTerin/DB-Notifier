/**
 * Generates deterministic React and WPF localisation adapters from the shared canonical XML catalogues.
 * The generator validates schema, culture, unique keys, placeholder parity and cross-locale key parity.
 */
import { createHash } from "node:crypto";
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const schemaVersion = "dbnotifier.localisation.v1";
const sources = new Map([
  ["pt-BR", join(root, "localisation/messages.pt-BR.xml")],
  ["en-GB", join(root, "localisation/messages.en-GB.xml")],
]);
const outputs = {
  typescript: join(root, "src/DBNotifier.Dashboard.Web/src/generated/localisation.ts"),
  ptBR: join(root, "src/DBNotifier.Desktop.Wpf/Generated/Localisation.pt-BR.xaml"),
  enGB: join(root, "src/DBNotifier.Desktop.Wpf/Generated/Localisation.en-GB.xaml"),
};

/** Decodes the narrow XML entity set accepted in message text. */
function decodeXml(value) {
  return value.replaceAll("&lt;", "<").replaceAll("&gt;", ">").replaceAll("&quot;", '"')
    .replaceAll("&apos;", "'").replaceAll("&amp;", "&");
}

/** Encodes message text for safe XAML element content. */
function encodeXml(value) {
  return value.replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;");
}

/** Normalises platform line endings before validation and provenance hashing. */
function normaliseLineEndings(value) {
  return value.replace(/\r\n?/g, "\n");
}

/** Reads and validates one canonical catalogue without relying on an external XML package. */
function readCatalogue(culture, path) {
  const source = normaliseLineEndings(readFileSync(path, "utf8"));
  const rootMatch = source.match(/<localisation\s+schemaVersion="([^"]+)"\s+culture="([^"]+)">/);
  if (!rootMatch || rootMatch[1] !== schemaVersion || rootMatch[2] !== culture) {
    throw new Error(`${path}: invalid localisation envelope.`);
  }
  const messages = new Map();
  for (const match of source.matchAll(/<message\s+key="([A-Za-z][A-Za-z0-9.]*)">([^<]*)<\/message>/g)) {
    if (messages.has(match[1])) throw new Error(`${path}: duplicate message key ${match[1]}.`);
    messages.set(match[1], decodeXml(match[2]));
  }
  if (messages.size === 0) throw new Error(`${path}: no messages found.`);
  const unparsed = source.replace(/<message\s+key="[A-Za-z][A-Za-z0-9.]*">[^<]*<\/message>/g, "");
  if (unparsed.includes("<message")) throw new Error(`${path}: invalid message element.`);
  return { source, messages };
}

/** Returns the ordered placeholder indexes used by a message. */
function placeholders(value) {
  return [...value.matchAll(/\{(\d+)\}/g)].map((match) => Number(match[1])).sort((left, right) => left - right);
}

/** Validates that every locale exposes the same ordered keys and formatting placeholders. */
function validateParity(catalogues) {
  const [referenceCulture, reference] = catalogues.entries().next().value;
  const referenceKeys = [...reference.messages.keys()].sort();
  for (const [culture, catalogue] of catalogues) {
    const keys = [...catalogue.messages.keys()].sort();
    if (JSON.stringify(keys) !== JSON.stringify(referenceKeys)) {
      throw new Error(`${culture}: message keys differ from ${referenceCulture}.`);
    }
    for (const key of keys) {
      if (JSON.stringify(placeholders(catalogue.messages.get(key))) !==
          JSON.stringify(placeholders(reference.messages.get(key)))) {
        throw new Error(`${culture}: placeholder mismatch for ${key}.`);
      }
    }
  }
}

/** Generates the typed React adapter while preserving deterministic key ordering. */
function generateTypeScript(catalogues, checksum) {
  const blocks = [...catalogues].map(([culture, catalogue]) => {
    const identifier = culture === "pt-BR" ? "ptBRMessages" : "enGBMessages";
    const entries = [...catalogue.messages].sort(([left], [right]) => left.localeCompare(right))
      .map(([key, value]) => `  ${JSON.stringify(key)}: ${JSON.stringify(value)},`)
      .join("\n");
    return `const ${identifier} = {\n${entries}\n} as const;`;
  }).join("\n\n");
  return `/** Generated from ${schemaVersion}; source sha256:${checksum}. Do not edit directly. */\n${blocks}\n\nexport const localisationMessages = {\n  "pt-BR": ptBRMessages,\n  "en-GB": enGBMessages,\n} as const;\n\nexport type SupportedLocale = keyof typeof localisationMessages;\nexport type MessageKey = keyof typeof ptBRMessages;\n`;
}

/** Generates one WPF ResourceDictionary using stable DynamicResource keys. */
function generateXaml(culture, catalogue, checksum) {
  const entries = [...catalogue.messages].sort(([left], [right]) => left.localeCompare(right))
    .map(([key, value]) => `  <sys:String x:Key="${key}">${encodeXml(value)}</sys:String>`)
    .join("\n");
  return `<!-- Generated from ${schemaVersion}; culture:${culture}; source sha256:${checksum}. Do not edit directly. -->\n<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"\n                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"\n                    xmlns:sys="clr-namespace:System;assembly=System.Runtime">\n${entries}\n</ResourceDictionary>\n`;
}

const catalogues = new Map([...sources].map(([culture, path]) => [culture, readCatalogue(culture, path)]));
validateParity(catalogues);
const checksum = createHash("sha256").update([...catalogues.values()].map((item) => item.source).join("\n")).digest("hex");
const generated = new Map([
  [outputs.typescript, generateTypeScript(catalogues, checksum)],
  [outputs.ptBR, generateXaml("pt-BR", catalogues.get("pt-BR"), checksum)],
  [outputs.enGB, generateXaml("en-GB", catalogues.get("en-GB"), checksum)],
]);
const verify = process.argv.includes("--verify");
for (const [path, content] of generated) {
  if (verify) {
    if (normaliseLineEndings(readFileSync(path, "utf8")) !== content) {
      throw new Error(`${path}: generated localisation adapter has drifted.`);
    }
  } else {
    writeFileSync(path, content, "utf8");
  }
}
process.stdout.write(`Localisation ${verify ? "verified" : "generated"} from ${schemaVersion}.\n`);
