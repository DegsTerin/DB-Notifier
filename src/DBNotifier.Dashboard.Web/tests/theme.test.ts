/** Module purpose: Verifies the Dashboard theme contract and deterministic generated token adapters. */
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import test from "node:test";
import {
  parseThemePreference,
  resolveEffectiveTheme,
  themePreferenceSchemaVersion,
  themePreferenceStorageKey,
} from "../src/theme.ts";
import { contrastRatio } from "../src/accessibility.ts";

type Token = { type: string; value: string | number };

/** Reads one canonical token set for contract-level validation without importing JSON modules. */
function readTokens(relativePath: string): Record<string, Token> {
  const document = JSON.parse(readFileSync(new URL(relativePath, import.meta.url), "utf8")) as { tokens: Record<string, Token> };
  return document.tokens;
}

/** Resolves a semantic colour alias against the canonical core palette. */
function resolveColour(token: Token, core: Record<string, Token>): string {
  const match = String(token.value).match(/^\{([^}]+)\}$/);
  return String(match ? core[match[1]].value : token.value);
}

test("theme preference parsing fails safely to system", () => {
  assert.equal(parseThemePreference("light"), "light");
  assert.equal(parseThemePreference("dark"), "dark");
  assert.equal(parseThemePreference("system"), "system");
  assert.equal(parseThemePreference("unexpected"), "system");
  assert.equal(parseThemePreference(null), "system");
  assert.equal(themePreferenceStorageKey, "dbnotifier.theme.preference.v1");
  assert.equal(themePreferenceSchemaVersion, "dbnotifier.ui-preferences.v1");
});

test("system resolution follows the current platform request only for system preference", () => {
  assert.equal(resolveEffectiveTheme("system", false), "light");
  assert.equal(resolveEffectiveTheme("system", true), "dark");
  assert.equal(resolveEffectiveTheme("light", true), "light");
  assert.equal(resolveEffectiveTheme("dark", false), "dark");
});

test("generated CSS and WPF token adapters are current and contain both semantic themes", () => {
  const generator = new URL("../../../scripts/generate-design-tokens.mjs", import.meta.url);
  const result = spawnSync(process.execPath, [fileURLToPath(generator), "--verify"], { encoding: "utf8" });
  assert.equal(result.status, 0, result.stderr);

  const css = readFileSync(new URL("../src/generated/design-tokens.css", import.meta.url), "utf8");
  const lightXaml = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/Generated/DesignTokens.Light.xaml", import.meta.url), "utf8");
  const darkXaml = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/Generated/DesignTokens.Dark.xaml", import.meta.url), "utf8");
  assert.match(css, /:root\[data-theme="light"\]/);
  assert.match(css, /:root\[data-theme="dark"\]/);
  assert.match(lightXaml, /ColourCanvasBrush/);
  assert.match(darkXaml, /ColourCanvasBrush/);
});

test("canonical Light and Dark semantic text pairs meet WCAG AA", () => {
  const core = readTokens("../../../design-system/tokens/core.tokens.json");
  for (const theme of ["light", "dark"]) {
    const semantic = readTokens(`../../../design-system/tokens/semantic.${theme}.tokens.json`);
    const pairs = [
      ["colour.text.primary", "colour.surface.default"],
      ["colour.text.secondary", "colour.surface.default"],
      ["colour.action.primary.foreground", "colour.action.primary.background"],
      ["colour.status.healthy.foreground", "colour.status.healthy.background"],
      ["colour.status.degraded.foreground", "colour.status.degraded.background"],
      ["colour.status.critical.foreground", "colour.status.critical.background"],
      ["colour.status.information.foreground", "colour.status.information.background"],
      ["colour.status.maintenance.foreground", "colour.status.maintenance.background"],
      ["colour.status.neutral.foreground", "colour.status.neutral.background"],
    ];
    for (const [foregroundKey, backgroundKey] of pairs) {
      const foreground = resolveColour(semantic[foregroundKey], core);
      const background = resolveColour(semantic[backgroundKey], core);
      assert.ok(contrastRatio(foreground, background) >= 4.5, `${theme}: ${foregroundKey} on ${backgroundKey}`);
    }
  }
});
