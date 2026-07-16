/**
 * Generates the provider-neutral DB-Notifier database mark for Web and Windows surfaces.
 * The implementation uses only Node.js primitives so CI can verify the SVG and ICO outputs cross-platform.
 */
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { deflateSync } from "node:zlib";

const root = resolve(fileURLToPath(new URL("..", import.meta.url)));
const transparent = [0, 0, 0, 0];
const databaseStrokeHex = "#0078D4";
const statusAccentHex = {
  Healthy: "#48C75F",
  Warning: "#FAB82A",
  Critical: "#C62828",
  Unknown: "#94A3B8",
};

/** Converts one canonical hexadecimal colour to the straight-alpha RGBA channels used by ICO generation. */
function hexToRgba(hex) {
  const value = Number.parseInt(hex.slice(1), 16);
  return [(value >> 16) & 0xff, (value >> 8) & 0xff, value & 0xff, 255];
}

const databaseStroke = hexToRgba(databaseStrokeHex);
const statusAccents = Object.fromEntries(
  Object.entries(statusAccentHex).map(([state, colour]) => [state, hexToRgba(colour)]),
);
const markGeometry = Object.freeze({
  database: Object.freeze({
    centreX: 25,
    topCentreY: 10,
    radiusX: 20,
    radiusY: 6.5,
    seamCentreY: 25,
    bottomCentreY: 41,
    strokeWidth: 3.5,
  }),
  bell: Object.freeze({
    centreX: 47,
    domeJoinY: 42,
    bodyBottomY: 49,
    flareBottomY: 54,
    bodyHalfWidth: 7,
    flareHalfWidth: 11,
    clapperCentreY: 57.5,
    clapperRadius: 2.5,
  }),
});
/** Defines the 16 px optical master: D is the blue database, S the semantic bell and dots remain transparent. */
const microGlyph16 = Object.freeze([
  "................",
  "...DDDDD........",
  ".DD.....DD......",
  ".D.......D......",
  ".DD.....DD......",
  ".D.DDDDD.D......",
  ".D.......D......",
  ".D.......D......",
  ".D.......D......",
  ".D.......D.SS...",
  ".D.......SSSS...",
  ".D.......SSSS...",
  ".DD.....SSSSS...",
  "...DDDD.SSSSS...",
  "..........SS....",
  "................",
]);
const iconSizes = [16, 20, 24, 32, 40, 48, 64, 128, 256];
const faviconSizes = [16, 20, 24, 32];
const outputs = {
  designSystemSvg: join(root, "design-system/assets/dbnotifier-database.svg"),
  dashboardSvgs: {
    Default: join(root, "src/DBNotifier.Dashboard.Web/public/dbnotifier-icon.svg"),
    Healthy: join(root, "src/DBNotifier.Dashboard.Web/public/dbnotifier-icon.healthy.svg"),
    Warning: join(root, "src/DBNotifier.Dashboard.Web/public/dbnotifier-icon.warning.svg"),
    Critical: join(root, "src/DBNotifier.Dashboard.Web/public/dbnotifier-icon.critical.svg"),
    Unknown: join(root, "src/DBNotifier.Dashboard.Web/public/dbnotifier-icon.unknown.svg"),
  },
  dashboardFavicons: {
    Default: join(root, "src/DBNotifier.Dashboard.Web/public/dbnotifier-favicon.ico"),
    Healthy: join(root, "src/DBNotifier.Dashboard.Web/public/dbnotifier-favicon.healthy.ico"),
    Warning: join(root, "src/DBNotifier.Dashboard.Web/public/dbnotifier-favicon.warning.ico"),
    Critical: join(root, "src/DBNotifier.Dashboard.Web/public/dbnotifier-favicon.critical.ico"),
    Unknown: join(root, "src/DBNotifier.Dashboard.Web/public/dbnotifier-favicon.unknown.ico"),
  },
  windowsIcons: {
    Default: join(root, "src/DBNotifier.Desktop.Wpf/Assets/DBNotifier.ico"),
    Healthy: join(root, "src/DBNotifier.Desktop.Wpf/Assets/DBNotifier.Healthy.ico"),
    Warning: join(root, "src/DBNotifier.Desktop.Wpf/Assets/DBNotifier.Warning.ico"),
    Critical: join(root, "src/DBNotifier.Desktop.Wpf/Assets/DBNotifier.Critical.ico"),
    Unknown: join(root, "src/DBNotifier.Desktop.Wpf/Assets/DBNotifier.Unknown.ico"),
  },
  notificationAvailabilityPng: join(
    root,
    "src/DBNotifier.Desktop.Wpf/NotificationAssets/DBNotifier.Availability.png",
  ),
};

/** Formats one canonical coordinate without exposing floating-point noise in generated SVG markup. */
function formatCoordinate(value) {
  return Number(value.toFixed(3));
}

/** Builds the lower-half ellipse commands shared by the database seam and lower boundary. */
function lowerHalfEllipseCommands(database, centreY) {
  const kappa = 0.5522847498;
  const databaseRight = database.centreX + database.radiusX;
  const verticalControl = database.radiusY * kappa;
  const horizontalControl = database.radiusX * kappa;
  return `C${database.centreX - database.radiusX} ${formatCoordinate(centreY + verticalControl)} ` +
    `${formatCoordinate(database.centreX - horizontalControl)} ${formatCoordinate(centreY + database.radiusY)} ` +
    `${database.centreX} ${formatCoordinate(centreY + database.radiusY)}` +
    `S${databaseRight} ${formatCoordinate(centreY + verticalControl)} ${databaseRight} ${centreY}`;
}

/**
 * Builds the transparent accessible vector master used by browser surfaces and Design System documentation.
 * @param {string} accent Semantic bell colour expressed as a CSS hexadecimal value.
 * @returns {string} Complete SVG markup with one clear outline and no opaque background.
 */
function buildSvg(accent) {
  const { database, bell } = markGeometry;
  const databaseLeft = database.centreX - database.radiusX;
  const bellLeft = bell.centreX - bell.bodyHalfWidth;
  const flareLeft = bell.centreX - bell.flareHalfWidth;
  const bottomCurve = lowerHalfEllipseCommands(database, database.bottomCentreY);
  const seamCurve = lowerHalfEllipseCommands(database, database.seamCentreY);
  return `<!-- Generated by scripts/generate-brand-assets.mjs; do not edit directly. -->
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">
  <g fill="none" stroke="${databaseStrokeHex}" stroke-width="${database.strokeWidth}" stroke-linecap="round" stroke-linejoin="round">
    <ellipse cx="${database.centreX}" cy="${database.topCentreY}" rx="${database.radiusX}" ry="${database.radiusY}"/>
    <path d="M${databaseLeft} ${database.topCentreY}V${database.bottomCentreY}${bottomCurve}V${database.topCentreY}"/>
    <path d="M${databaseLeft} ${database.seamCentreY}${seamCurve}"/>
  </g>
  <path d="M${bellLeft} ${bell.bodyBottomY}V${bell.domeJoinY}a${bell.bodyHalfWidth} ${bell.bodyHalfWidth} 0 0 1 ${bell.bodyHalfWidth * 2} 0v${bell.bodyBottomY - bell.domeJoinY}l${bell.flareHalfWidth - bell.bodyHalfWidth} ${bell.flareBottomY - bell.bodyBottomY}H${flareLeft}Z" fill="${accent}"/>
  <circle cx="${bell.centreX}" cy="${bell.clapperCentreY}" r="${bell.clapperRadius}" fill="${accent}"/>
</svg>
`;
}

/** Calculates the shortest distance from a point to a finite line segment. */
function distanceToSegment(x, y, x1, y1, x2, y2) {
  const dx = x2 - x1;
  const dy = y2 - y1;
  const denominator = dx * dx + dy * dy;
  const projection = denominator === 0 ? 0 : Math.max(0, Math.min(1, ((x - x1) * dx + (y - y1) * dy) / denominator));
  return Math.hypot(x - (x1 + projection * dx), y - (y1 + projection * dy));
}

/**
 * Determines whether a point overlaps the database-cylinder outline at a requested raster weight.
 * @param {number} x Horizontal coordinate in the canonical 64-unit canvas.
 * @param {number} y Vertical coordinate in the canonical 64-unit canvas.
 * @returns {boolean} True when the point belongs to the requested outline.
 */
function insideDatabaseStroke(x, y) {
  const { database } = markGeometry;
  const strokeRadius = database.strokeWidth / 2;
  const databaseLeft = database.centreX - database.radiusX;
  const databaseRight = database.centreX + database.radiusX;
  let previousX = databaseRight;
  let previousY = database.topCentreY;
  for (let step = 1; step <= 64; step += 1) {
    const angle = (Math.PI * 2 * step) / 64;
    const currentX = database.centreX + database.radiusX * Math.cos(angle);
    const currentY = database.topCentreY + database.radiusY * Math.sin(angle);
    if (distanceToSegment(x, y, previousX, previousY, currentX, currentY) <= strokeRadius) return true;
    previousX = currentX;
    previousY = currentY;
  }
  if (distanceToSegment(x, y, databaseLeft, database.topCentreY, databaseLeft, database.bottomCentreY) <= strokeRadius ||
      distanceToSegment(x, y, databaseRight, database.topCentreY, databaseRight, database.bottomCentreY) <= strokeRadius) return true;
  for (const centreY of [database.seamCentreY, database.bottomCentreY]) {
    previousX = databaseLeft;
    let previousY = centreY;
    for (let step = 1; step <= 32; step += 1) {
      const angle = Math.PI - (Math.PI * step) / 32;
      const currentX = database.centreX + database.radiusX * Math.cos(angle);
      const currentY = centreY + database.radiusY * Math.sin(angle);
      if (distanceToSegment(x, y, previousX, previousY, currentX, currentY) <= strokeRadius) return true;
      previousX = currentX;
      previousY = currentY;
    }
  }
  return false;
}

/** Returns whether a point lies within the lower-right notification bell accent. */
function insideBellFill(x, y) {
  const { bell } = markGeometry;
  const domeTopY = bell.domeJoinY - bell.bodyHalfWidth;
  if (Math.hypot(x - bell.centreX, y - bell.clapperCentreY) <= bell.clapperRadius) return true;
  if (y >= bell.bodyBottomY && y <= bell.flareBottomY) {
    const expansion = ((y - bell.bodyBottomY) * (bell.flareHalfWidth - bell.bodyHalfWidth)) /
      (bell.flareBottomY - bell.bodyBottomY);
    return x >= bell.centreX - bell.bodyHalfWidth - expansion &&
      x <= bell.centreX + bell.bodyHalfWidth + expansion;
  }
  if (y >= bell.domeJoinY && y <= bell.bodyBottomY &&
      Math.abs(x - bell.centreX) <= bell.bodyHalfWidth) return true;
  const domeHalfWidth = bell.bodyHalfWidth * Math.sqrt(
    Math.max(0, 1 - ((y - bell.domeJoinY) / bell.bodyHalfWidth) ** 2),
  );
  return y >= domeTopY && y <= bell.domeJoinY && Math.abs(x - bell.centreX) <= domeHalfWidth;
}

/**
 * Classifies one supersampled point using the single canonical shell-mark geometry.
 * @param {number} x Horizontal coordinate in the canonical 64-unit canvas.
 * @param {number} y Vertical coordinate in the canonical 64-unit canvas.
 * @returns {number} Index of the foremost semantic drawing layer at this point.
 */
function sampleLayer(x, y) {
  if (insideBellFill(x, y)) return 0;
  if (insideDatabaseStroke(x, y)) return 1;
  return 2;
}

/**
 * Precomputes supersampled layer coverage once for every semantic status variant.
 * @param {number} size Output bitmap edge length in pixels.
 * @returns {Buffer} Three-layer coverage counts for every output pixel.
 */
function renderCoverage(size) {
  const supersampling = 4;
  const sampleWeight = 16 / (supersampling ** 2);
  const coverage = Buffer.alloc(size * size * 3);
  for (let y = 0; y < size; y += 1) {
    for (let x = 0; x < size; x += 1) {
      for (let sampleY = 0; sampleY < supersampling; sampleY += 1) {
        for (let sampleX = 0; sampleX < supersampling; sampleX += 1) {
          const layer = sampleLayer(
            ((x + (sampleX + 0.5) / supersampling) / size) * 64,
            ((y + (sampleY + 0.5) / supersampling) / size) * 64,
          );
          coverage[(y * size + x) * 3 + layer] += sampleWeight;
        }
      }
    }
  }
  return coverage;
}

/**
 * Renders the optically corrected 16 px grid at one native small-icon size with binary alpha.
 * @param {number} size Native 16, 20 or 24 px output size selected by the Windows scaling metric.
 * @param {number[]} accent Semantic bell colour as RGBA channels.
 * @returns {Buffer} Top-down straight-alpha BGRA bitmap bytes for a small ICO frame.
 */
function renderMicroBitmap(size, accent) {
  if (![16, 20, 24].includes(size)) throw new Error(`Unsupported micro-glyph size ${size}.`);
  const pixels = Buffer.alloc(size * size * 4);
  for (let y = 0; y < size; y += 1) {
    const sourceY = Math.min(15, Math.floor(((y + 0.5) * 16) / size));
    const row = microGlyph16[sourceY];
    if (row.length !== 16) throw new Error(`Invalid 16 px micro-glyph row width at row ${y}.`);
    for (let x = 0; x < size; x += 1) {
      const sourceX = Math.min(15, Math.floor(((x + 0.5) * 16) / size));
      const layer = row[sourceX];
      if (layer === ".") continue;
      const colour = layer === "S" ? accent : databaseStroke;
      const offset = (y * size + x) * 4;
      pixels[offset] = colour[2];
      pixels[offset + 1] = colour[1];
      pixels[offset + 2] = colour[0];
      pixels[offset + 3] = 255;
    }
  }
  return pixels;
}

/** Calculates the PNG CRC-32 value without introducing a generator dependency. */
function crc32(bytes) {
  let value = 0xffffffff;
  for (const byte of bytes) {
    value ^= byte;
    for (let bit = 0; bit < 8; bit += 1) {
      value = (value >>> 1) ^ (value & 1 ? 0xedb88320 : 0);
    }
  }
  return (value ^ 0xffffffff) >>> 0;
}

/** Builds one length-prefixed PNG chunk with its required type-and-payload checksum. */
function buildPngChunk(type, payload) {
  const typeBytes = Buffer.from(type, "ascii");
  const chunk = Buffer.alloc(12 + payload.length);
  chunk.writeUInt32BE(payload.length, 0);
  typeBytes.copy(chunk, 4);
  payload.copy(chunk, 8);
  chunk.writeUInt32BE(crc32(Buffer.concat([typeBytes, payload])), 8 + payload.length);
  return chunk;
}

/**
 * Builds the explicit Windows app-notification identity asset from the native canonical 64 px raster.
 * @param {number[]} accent Application-availability bell colour as RGBA channels.
 * @param {Buffer} coverage Canonical 64 px layer coverage shared with the matching ICO frame.
 * @returns {Buffer} Complete transparent 64 px RGBA PNG bytes.
 */
function buildNotificationAvailabilityPng(accent, coverage) {
  const size = 64;
  const source = renderBitmap(size, accent, coverage);
  const scanlines = Buffer.alloc(size * (1 + size * 4));
  for (let y = 0; y < size; y += 1) {
    const rowOffset = y * (1 + size * 4);
    scanlines[rowOffset] = 0;
    for (let x = 0; x < size; x += 1) {
      const sourceOffset = (y * size + x) * 4;
      const targetOffset = rowOffset + 1 + x * 4;
      scanlines[targetOffset] = source[sourceOffset + 2];
      scanlines[targetOffset + 1] = source[sourceOffset + 1];
      scanlines[targetOffset + 2] = source[sourceOffset];
      scanlines[targetOffset + 3] = source[sourceOffset + 3];
    }
  }

  const header = Buffer.alloc(13);
  header.writeUInt32BE(size, 0);
  header.writeUInt32BE(size, 4);
  header[8] = 8;
  header[9] = 6;
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    buildPngChunk("IHDR", header),
    buildPngChunk("IDAT", deflateSync(scanlines)),
    buildPngChunk("IEND", Buffer.alloc(0)),
  ]);
}

/**
 * Renders one square straight-alpha BGRA bitmap from shared layer coverage.
 * @param {number} size Output bitmap edge length in pixels.
 * @param {number[]} accent Semantic bell colour as RGBA channels.
 * @param {Buffer | undefined} coverage Precomputed three-layer supersampling coverage for canonical frames.
 * @returns {Buffer} Straight-alpha BGRA bitmap bytes for the ICO payload.
 */
function renderBitmap(size, accent, coverage) {
  if (size <= 24) return renderMicroBitmap(size, accent);
  if (!coverage) throw new Error(`Missing canonical coverage for ${size} px frame.`);

  const samples = 16;
  const pixels = Buffer.alloc(size * size * 4);
  const colours = [accent, databaseStroke];
  for (let y = 0; y < size; y += 1) {
    for (let x = 0; x < size; x += 1) {
      const coverageOffset = (y * size + x) * 3;
      const visibleSamples = coverage[coverageOffset] + coverage[coverageOffset + 1];
      const offset = (y * size + x) * 4;
      if (visibleSamples === 0) {
        pixels.set(transparent, offset);
        continue;
      }

      const colourTotals = [0, 0, 0];
      for (let layer = 0; layer < colours.length; layer += 1) {
        const count = coverage[coverageOffset + layer];
        for (let channel = 0; channel < colourTotals.length; channel += 1) {
          colourTotals[channel] += colours[layer][channel] * count;
        }
      }
      pixels[offset] = Math.round(colourTotals[2] / visibleSamples);
      pixels[offset + 1] = Math.round(colourTotals[1] / visibleSamples);
      pixels[offset + 2] = Math.round(colourTotals[0] / visibleSamples);
      pixels[offset + 3] = Math.round((visibleSamples / samples) * 255);
    }
  }
  return pixels;
}

/**
 * Builds the legacy ICO AND mask for consumers that do not honour 32-bit alpha consistently.
 * Fully transparent pixels are masked out; visible and partially covered pixels remain governed by their BGRA alpha.
 */
function buildTransparencyMask(size, bottomUpPixels) {
  const maskStride = Math.ceil(size / 32) * 4;
  const mask = Buffer.alloc(maskStride * size);
  for (let row = 0; row < size; row += 1) {
    for (let column = 0; column < size; column += 1) {
      const alpha = bottomUpPixels[(row * size + column) * 4 + 3];
      if (alpha !== 0) continue;
      const maskOffset = row * maskStride + Math.floor(column / 8);
      mask[maskOffset] |= 0x80 >> (column % 8);
    }
  }
  return mask;
}

/** Wraps a rendered BGRA bitmap in the Windows device-independent bitmap structure used by ICO files. */
function buildDib(size, accent, coverage) {
  const pixels = renderBitmap(size, accent, coverage);
  const header = Buffer.alloc(40);
  header.writeUInt32LE(40, 0);
  header.writeInt32LE(size, 4);
  header.writeInt32LE(size * 2, 8);
  header.writeUInt16LE(1, 12);
  header.writeUInt16LE(32, 14);
  header.writeUInt32LE(pixels.length, 20);
  const bottomUpPixels = Buffer.alloc(pixels.length);
  const rowBytes = size * 4;
  for (let row = 0; row < size; row += 1) {
    pixels.copy(bottomUpPixels, row * rowBytes, (size - row - 1) * rowBytes, (size - row) * rowBytes);
  }
  const mask = buildTransparencyMask(size, bottomUpPixels);
  return Buffer.concat([header, bottomUpPixels, mask]);
}

/**
 * Builds a multi-resolution icon for Windows shell surfaces or the browser favicon.
 * @param {number[]} accent Semantic bell colour as RGBA channels.
 * @param {Map<number, Buffer>} coverageBySize Shared resolution-specific layer coverage.
 * @param {number[]} sizes Ordered icon resolutions to include in the ICO directory.
 * @returns {Buffer} Complete multi-resolution ICO bytes.
 */
function buildIco(accent, coverageBySize, sizes = iconSizes) {
  const images = sizes.map((size) => buildDib(size, accent, coverageBySize.get(size)));
  const header = Buffer.alloc(6 + sizes.length * 16);
  header.writeUInt16LE(0, 0);
  header.writeUInt16LE(1, 2);
  header.writeUInt16LE(sizes.length, 4);
  let offset = header.length;
  sizes.forEach((size, index) => {
    const entry = 6 + index * 16;
    header.writeUInt8(size === 256 ? 0 : size, entry);
    header.writeUInt8(size === 256 ? 0 : size, entry + 1);
    header.writeUInt16LE(1, entry + 4);
    header.writeUInt16LE(32, entry + 6);
    header.writeUInt32LE(images[index].length, entry + 8);
    header.writeUInt32LE(offset, entry + 12);
    offset += images[index].length;
  });
  return Buffer.concat([header, ...images]);
}

/** Writes generated bytes or fails closed when verify mode detects drift. */
function processOutput(path, bytes, verify) {
  if (verify) {
    if (!existsSync(path) || !readFileSync(path).equals(bytes)) throw new Error(`Generated brand asset is missing or stale: ${path}`);
    return;
  }
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, bytes);
}

const verify = process.argv.includes("--verify");
const coverageBySize = new Map(iconSizes.filter((size) => size >= 32).map((size) => [size, renderCoverage(size)]));
const defaultSvg = Buffer.from(buildSvg(statusAccentHex.Unknown), "utf8");
processOutput(outputs.designSystemSvg, defaultSvg, verify);
processOutput(outputs.dashboardSvgs.Default, defaultSvg, verify);
processOutput(outputs.dashboardFavicons.Default, buildIco(statusAccents.Unknown, coverageBySize, faviconSizes), verify);
processOutput(outputs.windowsIcons.Default, buildIco(statusAccents.Unknown, coverageBySize), verify);
processOutput(
  outputs.notificationAvailabilityPng,
  buildNotificationAvailabilityPng(statusAccents.Healthy, coverageBySize.get(64)),
  verify,
);
for (const [state, accent] of Object.entries(statusAccents)) {
  processOutput(outputs.dashboardSvgs[state], Buffer.from(buildSvg(statusAccentHex[state]), "utf8"), verify);
  processOutput(outputs.dashboardFavicons[state], buildIco(accent, coverageBySize, faviconSizes), verify);
  processOutput(outputs.windowsIcons[state], buildIco(accent, coverageBySize), verify);
}
console.log(verify ? "DB-Notifier brand assets verified." : "DB-Notifier brand assets generated.");
