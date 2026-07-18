/**
 * Generates and verifies provider identity assets for the Web and WPF presentation boundaries.
 * Source SVGs remain pinned and local; ordinary builds and product runtime never use network access.
 */
import { spawnSync } from "node:child_process";
import { createHash } from "node:crypto";
import {
  existsSync,
  mkdirSync,
  mkdtempSync,
  readFileSync,
  readdirSync,
  rmSync,
  writeFileSync,
} from "node:fs";
import { tmpdir } from "node:os";
import { basename, dirname, isAbsolute, join, relative, resolve, sep } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { inflateSync } from "node:zlib";

const root = resolve(fileURLToPath(new URL("..", import.meta.url)));
const providerRoot = join(root, "design-system", "provider-icons");
const sourceAssetRoot = join(providerRoot, "skill-icons", "icons");
const manifestPath = join(providerRoot, "manifest.json");
const generatedInventoryPath = join(providerRoot, "generated-assets.json");
const dashboardAssetRoot = join(root, "src", "DBNotifier.Dashboard.Web", "public", "provider-icons");
const desktopAssetRoot = join(root, "src", "DBNotifier.Desktop.Wpf", "Assets", "ProviderIcons");
const verifyOnly = process.argv.includes("--verify");
const generate = process.argv.includes("--generate");

if (verifyOnly === generate) {
  throw new Error("Specify exactly one of --generate or --verify.");
}

const manifest = JSON.parse(readFileSync(manifestPath, "utf8"));

/** Returns the lowercase SHA-256 digest for one byte buffer. */
function sha256(bytes) {
  return createHash("sha256").update(bytes).digest("hex");
}

/** Returns the lowercase Git blob SHA-1 for one exact byte buffer. */
function gitBlobSha(bytes) {
  return createHash("sha1").update(`blob ${bytes.length}\0`).update(bytes).digest("hex");
}

/**
 * Calculates the unsigned CRC-32 used by PNG chunks.
 * @param {Buffer} bytes Chunk type and data bytes.
 * @returns {number} Unsigned CRC-32 value.
 */
function crc32(bytes) {
  let crc = 0xffffffff;
  for (const byte of bytes) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit += 1) {
      crc = (crc >>> 1) ^ (crc & 1 ? 0xedb88320 : 0);
    }
  }
  return (crc ^ 0xffffffff) >>> 0;
}

/**
 * Validates a bounded, non-interlaced, eight-bit RGBA PNG and its decoded scanlines.
 * @param {Buffer} bytes Candidate PNG bytes.
 * @param {number | undefined} expectedWidth Required width when validating a generated derivative.
 * @param {number | undefined} expectedHeight Required height when validating a generated derivative.
 * @returns {{ width: number, height: number }} Validated physical dimensions.
 * @throws {Error} When chunks, checksums, encoding, dimensions or scanlines are invalid.
 */
function validateRgbaPng(bytes, expectedWidth, expectedHeight) {
  const signature = "89504e470d0a1a0a";
  if (bytes.length < 45 || bytes.subarray(0, 8).toString("hex") !== signature) {
    throw new Error("PNG signature is invalid.");
  }

  let offset = 8;
  let width = 0;
  let height = 0;
  let seenHeader = false;
  let seenImageData = false;
  let imageDataEnded = false;
  let seenEnd = false;
  const imageDataChunks = [];
  while (offset < bytes.length) {
    if (offset + 12 > bytes.length) {
      throw new Error("PNG chunk header is truncated.");
    }

    const length = bytes.readUInt32BE(offset);
    const typeStart = offset + 4;
    const dataStart = typeStart + 4;
    const dataEnd = dataStart + length;
    const chunkEnd = dataEnd + 4;
    if (chunkEnd > bytes.length) {
      throw new Error("PNG chunk data is truncated.");
    }

    const type = bytes.subarray(typeStart, dataStart).toString("ascii");
    if (!/^[A-Za-z]{4}$/.test(type) || crc32(bytes.subarray(typeStart, dataEnd)) !== bytes.readUInt32BE(dataEnd)) {
      throw new Error("PNG chunk type or checksum is invalid.");
    }

    if (!seenHeader) {
      if (type !== "IHDR" || length !== 13) {
        throw new Error("PNG IHDR must be the first chunk.");
      }
      width = bytes.readUInt32BE(dataStart);
      height = bytes.readUInt32BE(dataStart + 4);
      const isReviewedEncoding = bytes[dataStart + 8] === 8 &&
        bytes[dataStart + 9] === 6 &&
        bytes[dataStart + 10] === 0 &&
        bytes[dataStart + 11] === 0 &&
        bytes[dataStart + 12] === 0;
      if (width === 0 || height === 0 || width > 4096 || height > 4096 || width * height > 4_194_304 || !isReviewedEncoding) {
        throw new Error("PNG dimensions or pixel encoding are outside the reviewed bounds.");
      }
      seenHeader = true;
    } else if (type === "IHDR") {
      throw new Error("PNG contains more than one IHDR chunk.");
    }

    if (type === "IDAT") {
      if (imageDataEnded || seenEnd) {
        throw new Error("PNG IDAT chunks are not contiguous.");
      }
      seenImageData = true;
      imageDataChunks.push(bytes.subarray(dataStart, dataEnd));
    } else if (seenImageData && type !== "IEND") {
      imageDataEnded = true;
    }

    if (type === "IEND") {
      if (length !== 0 || !seenImageData || chunkEnd !== bytes.length) {
        throw new Error("PNG IEND chunk is invalid.");
      }
      seenEnd = true;
    }
    offset = chunkEnd;
  }

  if (!seenHeader || !seenImageData || !seenEnd ||
      (expectedWidth !== undefined && width !== expectedWidth) ||
      (expectedHeight !== undefined && height !== expectedHeight)) {
    throw new Error("PNG structure or dimensions do not match the expected output.");
  }

  const expectedScanlineLength = 1 + width * 4;
  const expectedDecodedLength = expectedScanlineLength * height;
  const decoded = inflateSync(Buffer.concat(imageDataChunks), { maxOutputLength: expectedDecodedLength });
  if (decoded.length !== expectedDecodedLength) {
    throw new Error("PNG decoded scanline length is invalid.");
  }
  for (let row = 0; row < height; row += 1) {
    if (decoded[row * expectedScanlineLength] > 4) {
      throw new Error("PNG scanline uses an invalid filter method.");
    }
  }

  return { width, height };
}

/**
 * Accepts only bounded, canonical base64 PNG data used by a reviewed pinned SVG.
 * @param {string} value Complete SVG data reference.
 * @returns {boolean} Whether the reference decodes to a plausible local PNG resource.
 */
function isSafeEmbeddedPngReference(value) {
  const prefix = "data:image/png;base64,";
  if (!value.startsWith(prefix)) {
    return false;
  }

  const encoded = value.slice(prefix.length);
  const canonicalBase64 = /^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/;
  if (encoded.length === 0 || encoded.length > 512 * 1024 || !canonicalBase64.test(encoded)) {
    return false;
  }

  const decoded = Buffer.from(encoded, "base64");
  if (decoded.toString("base64") !== encoded) {
    return false;
  }
  try {
    validateRgbaPng(decoded, undefined, undefined);
    return true;
  } catch {
    return false;
  }
}

/**
 * Resolves a reviewed relative path and rejects traversal outside its owning root.
 * @param {string} owningRoot Directory that owns the expected file.
 * @param {string} candidate Relative path declared by the trusted manifest.
 * @returns {string} Validated absolute path.
 */
function resolveWithin(owningRoot, candidate) {
  const absolute = resolve(owningRoot, candidate);
  const relation = relative(owningRoot, absolute);
  if (relation === "" || (!relation.startsWith(`..${sep}`) && relation !== ".." && !isAbsolute(relation))) {
    return absolute;
  }

  throw new Error(`Path escapes its owning directory: ${candidate}`);
}

/**
 * Applies the deliberately narrow static-SVG allowlist used before local rendering.
 * @param {string} markup Candidate UTF-8 SVG markup.
 * @returns {boolean} Whether the markup uses only the reviewed static subset.
 */
function isSafeSvgMarkup(markup) {
  const forbidden = [
    /<script\b/i,
    /<foreignObject\b/i,
    /<(?:animate(?:Color|Motion|Transform)?|audio|discard|embed|iframe|object|set|video)\b/i,
    /<!DOCTYPE\b/i,
    /<!ENTITY\b/i,
    /<\?/,
    /<style\b/i,
    /@import\b/i,
    /&/,
    /\\/,
    /<\/?[^\s>/:]+:[^\s>/:]+/,
    /\sxml:base\s*=/i,
    /\s(?:[A-Za-z_][A-Za-z0-9_.-]*:)?on[a-z]+\s*=/i,
    /\s(?:[A-Za-z_][A-Za-z0-9_.-]*:)?ping\s*=/i,
    /\s(?:[A-Za-z_][A-Za-z0-9_.-]*:)?src(?:doc|set)?\s*=/i,
  ];
  const referenceDeclarations = [...markup.matchAll(/\b(?:href|xlink:href)\s*=/gi)];
  const references = [...markup.matchAll(/\b(?:href|xlink:href)\s*=\s*["']([^"']+)["']/gi)]
    .map((match) => match[1]);
  const cssReferenceDeclarations = [...markup.matchAll(/\burl\s*\(/gi)];
  const cssReferences = [...markup.matchAll(/\burl\s*\(\s*(?:["']([^"']+)["']|([^\s"')][^)]*?))\s*\)/gi)]
    .map((match) => (match[1] ?? match[2]).trim());
  const styleDeclarations = [...markup.matchAll(/\bstyle\s*=/gi)];
  const styles = [...markup.matchAll(/\bstyle\s*=\s*["']([^"']*)["']/gi)].map((match) => match[1]);
  const namespaceDeclarations = [...markup.matchAll(/\bxmlns(?:\:[A-Za-z_][A-Za-z0-9_.-]*)?\s*=/gi)];
  const namespaces = [...markup.matchAll(/\bxmlns(?:\:([A-Za-z_][A-Za-z0-9_.-]*))?\s*=\s*["']([^"']+)["']/gi)]
    .map((match) => ({ prefix: match[1] ?? "", value: match[2] }));
  const prefixedAttributes = [...markup.matchAll(/\s([^\s=/>]+:[^\s=/>]+)\s*=/g)]
    .map((match) => match[1]);
  const elements = [...markup.matchAll(/<\/?\s*([^\s>/]+)/g)].map((match) => match[1]);
  const allowedElements = new Set([
    "clipPath",
    "defs",
    "g",
    "image",
    "linearGradient",
    "mask",
    "path",
    "pattern",
    "rect",
    "stop",
    "svg",
    "use",
  ]);
  const rootElement = markup.match(/^\uFEFF?\s*<svg(?:\s[^>]*|)>/i)?.[0];
  const safeFragment = /^#[A-Za-z_][A-Za-z0-9_.:-]*$/;
  const referencesAreSafe = references.every((value) => safeFragment.test(value) || isSafeEmbeddedPngReference(value));
  const cssReferencesAreSafe = cssReferences.every((value) => safeFragment.test(value));
  const stylesAreSafe = styles.every((value) => value === "mask-type:alpha");
  const namespacesAreSafe = namespaces.every(({ prefix, value }) =>
    (prefix === "" && value === "http://www.w3.org/2000/svg") ||
    (prefix === "xlink" && value === "http://www.w3.org/1999/xlink"));
  const prefixedAttributesAreSafe = prefixedAttributes.every((name) => name === "xmlns:xlink" || name === "xlink:href");
  const elementsAreSafe = elements.length > 0 && elements.every((name) => allowedElements.has(name));
  return Boolean(rootElement) &&
    /\bxmlns=["']http:\/\/www\.w3\.org\/2000\/svg["']/i.test(rootElement) &&
    !forbidden.some((pattern) => pattern.test(markup)) &&
    referenceDeclarations.length === references.length &&
    cssReferenceDeclarations.length === cssReferences.length &&
    styleDeclarations.length === styles.length &&
    namespaceDeclarations.length === namespaces.length &&
    referencesAreSafe &&
    cssReferencesAreSafe &&
    stylesAreSafe &&
    namespacesAreSafe &&
    prefixedAttributesAreSafe &&
    elementsAreSafe;
}

/** Proves that reviewed active-content and external-reference regressions remain rejected. */
function verifySvgSanitiserPolicy() {
  const accepted = [
    '<svg xmlns="http://www.w3.org/2000/svg"><path id="shape" fill="url(#paint)"/></svg>',
    '<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink"><use xlink:href="#shape" style="mask-type:alpha"/></svg>',
  ];
  const rejected = [
    '<svg xmlns="http://www.w3.org/2000/svg"><script>alert(1)</script></svg>',
    '<svg xmlns="http://www.w3.org/2000/svg" xmlns:svg="http://www.w3.org/2000/svg"><svg:script/></svg>',
    '<svg xmlns="http://www.w3.org/2000/svg" xmlns:é="http://www.w3.org/2000/svg"><é:script/></svg>',
    '<svg xmlns="http://www.w3.org/2000/svg"><g xmlns="http://www.w3.org/1999/xhtml"><img src="https://example.invalid/x"/></g></svg>',
    '<svg xmlns="http://www.w3.org/2000/svg"><path style="mask-image:image-set(https://example.invalid/x 1x)"/></svg>',
    String.raw`<svg xmlns="http://www.w3.org/2000/svg"><path fill="\75\72\6c(https://example.invalid/x)"/></svg>`,
    '<svg xmlns="http://www.w3.org/2000/svg"><path fill="&#x75;rl(https://example.invalid/x)"/></svg>',
    '<svg xmlns="http://www.w3.org/2000/svg"><set href="#image" attributeName="href" to="https://example.invalid/x"/></svg>',
    '<svg xmlns="http://www.w3.org/2000/svg"><image href="data:image/png;base64,AAAA"/></svg>',
    '<svg xmlns="http://www.w3.org/2000/svg"><image href="https://example.invalid/x"/></svg>',
    '<svg xmlns="http://www.w3.org/2000/svg"><a href="#shape" ping="https://example.invalid/x"><path id="shape"/></a></svg>',
    '<svg xmlns="http://www.w3.org/2000/svg"><path fill="url(https://example.invalid/x)"/></svg>',
  ];
  if (accepted.some((markup) => !isSafeSvgMarkup(markup)) || rejected.some((markup) => isSafeSvgMarkup(markup))) {
    throw new Error("Provider icon SVG sanitiser policy self-test failed.");
  }
}

/**
 * Validates one pinned SVG source before it can be copied or rasterised.
 * @param {object} variant Reviewed manifest variant.
 * @returns {{ sourcePath: string, bytes: Buffer }} Validated local source.
 */
function readValidatedSvg(variant) {
  const sourcePath = resolveWithin(providerRoot, variant.sourcePath);
  const bytes = readFileSync(sourcePath);
  if (sha256(bytes) !== variant.sourceSha256) {
    throw new Error(`Pinned source hash mismatch: ${variant.sourcePath}`);
  }
  if (gitBlobSha(bytes) !== variant.upstreamBlobSha) {
    throw new Error(`Pinned source does not match its declared upstream Git blob: ${variant.sourcePath}`);
  }

  if (!isSafeSvgMarkup(bytes.toString("utf8"))) {
    throw new Error(`Unsafe or invalid SVG source: ${variant.sourcePath}`);
  }

  return { sourcePath, bytes };
}

/** Validates manifest structure, identifiers, provenance and source hashes. */
function validateManifest() {
  if (manifest.schemaVersion !== 1 || manifest.source?.runtimeNetworkAccess !== false) {
    throw new Error("Provider icon manifest schema or network policy is invalid.");
  }

  const raster = manifest.desktopRaster;
  if (raster?.engine !== "Google Chrome Headless" ||
      !/^\d+\.\d+\.\d+\.\d+$/.test(raster.version) ||
      !Number.isInteger(raster.width) || raster.width < 16 || raster.width > 512 ||
      !Number.isInteger(raster.height) || raster.height < 16 || raster.height > 512 ||
      !Array.isArray(manifest.icons) || manifest.icons.length === 0) {
    throw new Error("Provider icon rasteriser or icon catalogue is invalid.");
  }

  if (!/^[0-9a-f]{40}$/.test(manifest.source.revision) ||
      !/^[0-9a-f]{40}$/.test(manifest.source.licenseUpstreamBlobSha) ||
      manifest.source.license !== "MIT") {
    throw new Error("Provider icon provenance is incomplete.");
  }

  const licenceBytes = readFileSync(resolveWithin(providerRoot, manifest.source.licensePath));
  if (sha256(licenceBytes) !== manifest.source.licenseSha256 ||
      gitBlobSha(licenceBytes) !== manifest.source.licenseUpstreamBlobSha) {
    throw new Error("Skill Icons licence hashes do not match the declared upstream bytes.");
  }

  const providerTypes = new Set();
  for (const icon of manifest.icons) {
    if (!/^[a-z0-9][a-z0-9._-]{0,63}$/.test(icon.providerType) ||
        typeof icon.displayName !== "string" || icon.displayName.length === 0 || icon.displayName.length > 100 ||
        providerTypes.has(icon.providerType)) {
      throw new Error(`Invalid or duplicate providerType: ${icon.providerType}`);
    }

    providerTypes.add(icon.providerType);
    for (const variantName of ["light", "dark"]) {
      const variant = icon[variantName];
      if (!variant ||
          !/^[0-9a-f]{40}$/.test(variant.upstreamBlobSha) ||
          !/^[0-9a-f]{64}$/.test(variant.sourceSha256) ||
          !/^icons\/[A-Za-z0-9][A-Za-z0-9._-]*\.svg$/.test(variant.upstreamPath) ||
          variant.sourcePath !== `skill-icons/${variant.upstreamPath}`) {
        throw new Error(`Incomplete ${variantName} provenance for ${icon.providerType}.`);
      }
      readValidatedSvg(variant);
    }
  }
}

/**
 * Locates an explicitly selected or standard Windows Chromium executable without launching a user browser profile.
 * @returns {string} Existing executable path used only by the offline generation action.
 */
function findChromium() {
  const candidates = [
    process.env.DBNOTIFIER_CHROMIUM_PATH,
    process.platform === "win32" ? "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe" : undefined,
  ].filter(Boolean);
  const executable = candidates.find((candidate) => existsSync(candidate));
  if (!executable) {
    throw new Error("A reviewed Chromium executable is required only for --generate.");
  }
  return executable;
}

/**
 * Reads the executable product identity without opening Chrome or attaching to a browser profile.
 * @param {string} executable Validated Chromium executable path.
 * @returns {{ productName: string, version: string }} Product name and four-part version.
 */
function readChromiumIdentity(executable) {
  if (process.platform === "win32") {
    const result = spawnSync(
      "powershell.exe",
      [
        "-NoProfile",
        "-NonInteractive",
        "-Command",
        "$information = (Get-Item -LiteralPath $env:DBNOTIFIER_CHROMIUM_PATH).VersionInfo; " +
          "@{ productName = $information.ProductName; version = $information.ProductVersion } | ConvertTo-Json -Compress",
      ],
      { encoding: "utf8", env: { ...process.env, DBNOTIFIER_CHROMIUM_PATH: executable }, windowsHide: true },
    );
    if (result.status !== 0) {
      throw new Error(`Could not read Chrome identity: ${result.stderr.trim()}`);
    }
    return JSON.parse(result.stdout.trim());
  }

  const result = spawnSync(executable, ["--version"], { encoding: "utf8" });
  const output = `${result.stdout} ${result.stderr}`.trim();
  const version = output.match(/\d+\.\d+\.\d+\.\d+/)?.[0];
  if (result.status !== 0 || !version || !/^Google Chrome\b/i.test(output)) {
    throw new Error("Could not prove the pinned Google Chrome identity.");
  }
  return { productName: "Google Chrome", version };
}

/**
 * Confirms that a generated PNG is a 64-bit-signature image with the declared physical dimensions.
 * @param {Buffer} bytes Generated PNG bytes.
 */
function validatePng(bytes) {
  validateRgbaPng(bytes, manifest.desktopRaster.width, manifest.desktopRaster.height);
}

/**
 * Rasterises one SVG in an isolated headless profile and removes only that owned temporary directory.
 * @param {string} executable Pinned Chromium executable.
 * @param {string} sourcePath Validated local SVG path.
 * @returns {Buffer} Generated transparent PNG bytes.
 */
function rasteriseSvg(executable, sourcePath) {
  const profile = mkdtempSync(join(tmpdir(), "dbnotifier-provider-icon-"));
  const outputPath = join(profile, "icon.png");
  const renderPath = join(profile, "render.html");
  try {
    const sourceUrl = pathToFileURL(sourcePath).href.replaceAll("&", "&amp;").replaceAll('"', "&quot;");
    const renderDocument = `<!doctype html><html><head><meta charset="utf-8"><style>` +
      `html,body{width:${manifest.desktopRaster.width}px;height:${manifest.desktopRaster.height}px;margin:0;overflow:hidden;background:transparent}` +
      `img{display:block;width:100%;height:100%}` +
      `</style></head><body><img src="${sourceUrl}" alt=""></body></html>`;
    writeFileSync(renderPath, renderDocument);
    const result = spawnSync(executable, [
      "--headless=new",
      "--disable-gpu",
      "--disable-extensions",
      "--hide-scrollbars",
      "--no-default-browser-check",
      "--no-first-run",
      "--force-device-scale-factor=1",
      "--default-background-color=00000000",
      `--window-size=${manifest.desktopRaster.width},${manifest.desktopRaster.height}`,
      `--user-data-dir=${profile}`,
      `--screenshot=${outputPath}`,
      pathToFileURL(renderPath).href,
    ], { encoding: "utf8", timeout: 30_000, windowsHide: true });
    if (result.status !== 0 || !existsSync(outputPath)) {
      throw new Error(`Chromium rasterisation failed: ${result.stderr.trim()}`);
    }
    const bytes = readFileSync(outputPath);
    validatePng(bytes);
    return bytes;
  } finally {
    rmSync(profile, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 });
  }
}

/**
 * Writes one generated file after creating only its expected owning directory.
 * @param {string} outputPath Reviewed generated output path.
 * @param {Buffer|string} content Deterministic generated content.
 */
function writeGeneratedFile(outputPath, content) {
  mkdirSync(dirname(outputPath), { recursive: true });
  writeFileSync(outputPath, content);
}

/** Generates local Web mirrors, WPF PNGs, licence copy and their verification inventory. */
function generateAssets() {
  const executable = findChromium();
  const detected = readChromiumIdentity(executable);
  if (detected.productName !== "Google Chrome" || detected.version !== manifest.desktopRaster.version) {
    throw new Error(
      `Rasteriser ${detected.productName} ${detected.version} does not match pinned Google Chrome ${manifest.desktopRaster.version}.`,
    );
  }

  const rasterCache = new Map();
  const assets = [];
  for (const icon of manifest.icons) {
    for (const variantName of ["light", "dark"]) {
      const variant = icon[variantName];
      const source = readValidatedSvg(variant);
      const webName = `${icon.providerType}-${variantName}.svg`;
      const desktopName = `${icon.providerType}-${variantName}.png`;
      const webPath = join(dashboardAssetRoot, webName);
      const desktopPath = join(desktopAssetRoot, desktopName);
      writeGeneratedFile(webPath, source.bytes);

      let png = rasterCache.get(variant.sourceSha256);
      if (!png) {
        png = rasteriseSvg(executable, source.sourcePath);
        rasterCache.set(variant.sourceSha256, png);
      }
      writeGeneratedFile(desktopPath, png);
      assets.push({
        providerType: icon.providerType,
        variant: variantName,
        webPath: `src/DBNotifier.Dashboard.Web/public/provider-icons/${webName}`,
        webSha256: sha256(source.bytes),
        desktopPath: `src/DBNotifier.Desktop.Wpf/Assets/ProviderIcons/${desktopName}`,
        desktopSha256: sha256(png),
      });
    }
  }

  const licenceBytes = readFileSync(resolveWithin(providerRoot, manifest.source.licensePath));
  const webLicencePath = join(dashboardAssetRoot, "SkillIcons.LICENSE.txt");
  writeGeneratedFile(webLicencePath, licenceBytes);
  const inventory = {
    generatedBy: "scripts/generate-provider-icon-assets.mjs",
    sourceRevision: manifest.source.revision,
    rasteriser: manifest.desktopRaster,
    assets,
    licence: {
      webPath: "src/DBNotifier.Dashboard.Web/public/provider-icons/SkillIcons.LICENSE.txt",
      sha256: sha256(licenceBytes),
    },
  };
  writeGeneratedFile(generatedInventoryPath, `${JSON.stringify(inventory, null, 2)}\n`);
}

/**
 * Verifies one generated file against its recorded SHA-256 without changing the workspace.
 * @param {string} relativePath Repository-relative generated path.
 * @param {string} expectedHash Recorded lowercase SHA-256.
 */
function verifyGeneratedFile(relativePath, expectedHash) {
  const outputPath = resolveWithin(root, relativePath);
  if (!existsSync(outputPath) || sha256(readFileSync(outputPath)) !== expectedHash) {
    throw new Error(`Generated provider icon drift: ${relativePath}`);
  }
}

/**
 * Verifies that one generated directory contains exactly the current manifest-owned files.
 * @param {string} directory Reviewed generated directory.
 * @param {Set<string>} expectedNames Expected direct child file names.
 */
function verifyDirectoryContents(directory, expectedNames) {
  const entries = readdirSync(directory, { withFileTypes: true });
  const nonFiles = entries.filter((entry) => !entry.isFile()).map((entry) => entry.name);
  const actualNames = entries.filter((entry) => entry.isFile()).map((entry) => entry.name);
  const unexpected = actualNames.filter((name) => !expectedNames.has(name));
  const missing = [...expectedNames].filter((name) => !actualNames.includes(name));
  if (nonFiles.length > 0 || unexpected.length > 0 || missing.length > 0) {
    throw new Error(
      `Generated provider icon directory drift in ${relative(root, directory)}: ` +
      `non-files [${nonFiles.join(", ")}], unexpected [${unexpected.join(", ")}], missing [${missing.join(", ")}].`,
    );
  }
}

/** Verifies every checked-in derivative and its relationship to the pinned source manifest. */
function verifyAssets() {
  const inventory = JSON.parse(readFileSync(generatedInventoryPath, "utf8"));
  const rasterKeys = ["engine", "height", "version", "width"];
  const inventoryRasterKeys = inventory.rasteriser && typeof inventory.rasteriser === "object"
    ? Object.keys(inventory.rasteriser).sort()
    : [];
  const rasteriserMatches = inventoryRasterKeys.length === rasterKeys.length &&
    rasterKeys.every((key) => inventoryRasterKeys.includes(key) && inventory.rasteriser[key] === manifest.desktopRaster[key]);
  if (inventory.generatedBy !== "scripts/generate-provider-icon-assets.mjs" ||
      inventory.sourceRevision !== manifest.source.revision ||
      !rasteriserMatches ||
      !Array.isArray(inventory.assets)) {
    throw new Error("Generated inventory provenance does not match the source manifest.");
  }

  const expectedCount = manifest.icons.length * 2;
  if (inventory.assets.length !== expectedCount) {
    throw new Error(`Expected ${expectedCount} generated provider icon variants.`);
  }

  const expectedVariants = new Map(manifest.icons.flatMap((icon) => [
    [`${icon.providerType}:light`, icon.light],
    [`${icon.providerType}:dark`, icon.dark],
  ]));
  for (const asset of inventory.assets) {
    const key = `${asset.providerType}:${asset.variant}`;
    const sourceVariant = expectedVariants.get(key);
    if (!sourceVariant) {
      throw new Error(`Unexpected or duplicate generated provider icon: ${key}`);
    }
    expectedVariants.delete(key);
    const expectedWebPath = `src/DBNotifier.Dashboard.Web/public/provider-icons/${asset.providerType}-${asset.variant}.svg`;
    const expectedDesktopPath = `src/DBNotifier.Desktop.Wpf/Assets/ProviderIcons/${asset.providerType}-${asset.variant}.png`;
    if (asset.webPath !== expectedWebPath || asset.desktopPath !== expectedDesktopPath) {
      throw new Error(`Generated provider icon path is not canonical: ${key}`);
    }
    if (asset.webSha256 !== sourceVariant.sourceSha256) {
      throw new Error(`Web mirror does not match its pinned source: ${key}`);
    }
    if (!/^[0-9a-f]{64}$/.test(asset.webSha256) || !/^[0-9a-f]{64}$/.test(asset.desktopSha256)) {
      throw new Error(`Generated provider icon hash is invalid: ${key}`);
    }
    verifyGeneratedFile(asset.webPath, asset.webSha256);
    verifyGeneratedFile(asset.desktopPath, asset.desktopSha256);
    validatePng(readFileSync(resolveWithin(root, asset.desktopPath)));
  }
  if (expectedVariants.size > 0) {
    throw new Error(`Missing generated provider icons: ${[...expectedVariants.keys()].join(", ")}`);
  }
  if (inventory.licence.webPath !== "src/DBNotifier.Dashboard.Web/public/provider-icons/SkillIcons.LICENSE.txt" ||
      inventory.licence.sha256 !== manifest.source.licenseSha256) {
    throw new Error("Generated Skill Icons licence path or hash is not canonical.");
  }
  verifyGeneratedFile(inventory.licence.webPath, inventory.licence.sha256);

  const expectedWebNames = new Set(inventory.assets.map((asset) => basename(asset.webPath)));
  expectedWebNames.add(basename(inventory.licence.webPath));
  const expectedDesktopNames = new Set(inventory.assets.map((asset) => basename(asset.desktopPath)));
  const expectedSourceNames = new Set(manifest.icons.flatMap((icon) =>
    [basename(icon.light.sourcePath), basename(icon.dark.sourcePath)]));
  verifyDirectoryContents(sourceAssetRoot, expectedSourceNames);
  verifyDirectoryContents(dashboardAssetRoot, expectedWebNames);
  verifyDirectoryContents(desktopAssetRoot, expectedDesktopNames);
}

verifySvgSanitiserPolicy();
validateManifest();
if (generate) {
  generateAssets();
}
verifyAssets();
console.log(`Provider icon assets ${generate ? "generated and verified" : "verified"}: ${manifest.icons.length} identities, ${manifest.icons.length * 2} theme variants.`);
