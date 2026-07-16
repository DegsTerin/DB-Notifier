/** Module purpose: Verifies the Dashboard theme contract and deterministic generated token adapters. */
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { runInNewContext } from "node:vm";
import test from "node:test";
import {
  applyTheme,
  parseThemePreference,
  persistThemePreference,
  readThemePreference,
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

/** Executes the pre-paint bootstrap against isolated browser substitutes. */
function executeBootstrap(storedValue: string | null, storageFails = false, language = "pt-BR") {
  const dataset: Record<string, string> = {};
  const bootstrap = readFileSync(new URL("../public/theme-bootstrap.js", import.meta.url), "utf8");
  const localStorage = {
    getItem(key: string) {
      if (storageFails) throw new Error("storage denied");
      return key.includes("language") ? language : storedValue;
    },
  };
  runInNewContext(bootstrap, {
    window: { localStorage },
    document: { documentElement: { dataset } },
  });
  return dataset;
}

test("theme preference parsing accepts only Light and Dark and safely retires System", () => {
  assert.equal(parseThemePreference("light"), "light");
  assert.equal(parseThemePreference("dark"), "dark");
  assert.equal(parseThemePreference("system"), "light");
  assert.equal(parseThemePreference("unexpected"), "light");
  assert.equal(parseThemePreference(null), "light");
  assert.equal(themePreferenceStorageKey, "dbnotifier.theme.preference.v1");
  assert.equal(themePreferenceSchemaVersion, "dbnotifier.ui-preferences.v1");
});

test("explicit theme resolution preserves Light and Dark", () => {
  assert.equal(resolveEffectiveTheme("light"), "light");
  assert.equal(resolveEffectiveTheme("dark"), "dark");
});

test("theme storage and root application fail safely to an explicit Light preference", () => {
  const values = new Map<string, string>();
  const storage = {
    getItem: (key: string) => values.get(key) ?? null,
    setItem: (key: string, value: string) => { values.set(key, value); },
  };
  const root = { dataset: {} as DOMStringMap };

  assert.equal(readThemePreference(storage), "light");
  assert.equal(persistThemePreference(storage, "dark"), true);
  assert.equal(readThemePreference(storage), "dark");
  assert.equal(applyTheme(root, "dark"), "dark");
  assert.equal(root.dataset.themePreference, "dark");
  assert.equal(root.dataset.theme, "dark");

  const failingStorage = {
    getItem: () => { throw new Error("denied"); },
    setItem: () => { throw new Error("denied"); },
  };
  assert.equal(readThemePreference(failingStorage), "light");
  assert.equal(persistThemePreference(failingStorage, "light"), false);
});

test("pre-paint bootstrap resolves valid, invalid and unavailable preferences before the application module", () => {
  assert.deepEqual(executeBootstrap("dark"), { themePreference: "dark", theme: "dark", languagePreference: "pt-BR" });
  assert.deepEqual(executeBootstrap("invalid", false, "en-GB"), { themePreference: "light", theme: "light", languagePreference: "en-GB" });
  assert.deepEqual(executeBootstrap(null, true), { themePreference: "light", theme: "light", languagePreference: "pt-BR" });

  const html = readFileSync(new URL("../index.html", import.meta.url), "utf8");
  assert.ok(html.indexOf("/theme-bootstrap.js") < html.indexOf("/src/main.tsx"));
});

test("runtime wiring imports generated tokens and synchronises explicit theme changes without raw feature colours", () => {
  const main = readFileSync(new URL("../src/main.tsx", import.meta.url), "utf8");
  const selector = readFileSync(new URL("../src/ThemeSelector.tsx", import.meta.url), "utf8");
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");
  assert.ok(main.indexOf("generated/design-tokens.css") < main.indexOf("styles.css"));
  assert.match(selector, /addEventListener\("storage"/);
  assert.doesNotMatch(selector, /matchMedia|darkColourSchemeMediaQuery|Theme\.System/);
  assert.doesNotMatch(css, /#[0-9a-f]{3,8}\b|rgba?\(|hsla?\(/i);
  for (const declaration of css.matchAll(/box-shadow\s*:\s*([^;]+);/gi)) {
    assert.match(declaration[1], /^var\(--db-elevation-/, declaration[0]);
  }
  for (const declaration of css.matchAll(/(?:margin(?:-[a-z]+)?|padding(?:-[a-z]+)?|gap)\s*:\s*([^;]+);/gi)) {
    assert.doesNotMatch(declaration[1], /-?\d*\.?\d+(?:px|rem)\b/i, declaration[0]);
  }
  for (const declaration of css.matchAll(/border-radius\s*:\s*([^;]+);/gi)) {
    assert.match(declaration[1], /var\(--db-radius-|50%/, declaration[0]);
  }
});

test("both interfaces expose one accessible icon button per global preference in the upper-right topbar", () => {
  const app = readFileSync(new URL("../src/App.tsx", import.meta.url), "utf8");
  const languageSelector = readFileSync(new URL("../src/LanguageSelector.tsx", import.meta.url), "utf8");
  const themeSelector = readFileSync(new URL("../src/ThemeSelector.tsx", import.meta.url), "utf8");
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");
  const desktopXaml = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/MainWindow.xaml", import.meta.url), "utf8");
  const desktopTheme = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/DesktopThemeService.cs", import.meta.url), "utf8");
  const desktopPreferences = readFileSync(new URL("../../DBNotifier.Desktop.Wpf/DesktopUiPreferenceStore.cs", import.meta.url), "utf8");

  assert.ok(app.indexOf("<LanguageSelector />") < app.indexOf("<ThemeSelector />"));
  assert.equal((languageSelector.match(/<button\b/g) ?? []).length, 1);
  assert.match(languageSelector, /className="preference-icon-button language-selector"/);
  assert.match(languageSelector, /<LanguageIcon \/>/);
  assert.match(languageSelector, /M2 5h12/);
  assert.doesNotMatch(languageSelector, /<circle|<Ellipse/);
  assert.match(languageSelector, /aria-label=\{accessibleLabel\}/);
  assert.match(languageSelector, /data-locale=\{locale\}/);
  assert.doesNotMatch(languageSelector, /role="group"|aria-pressed=/);
  assert.doesNotMatch(languageSelector, /<select|<input/i);
  assert.equal((themeSelector.match(/<button\b/g) ?? []).length, 1);
  assert.match(themeSelector, /className="preference-icon-button theme-selector"/);
  assert.match(themeSelector, /<ThemeIcon preference=\{preference\} \/>/);
  assert.match(themeSelector, /aria-label=\{accessibleLabel\}/);
  assert.match(themeSelector, /data-preference=\{preference\}/);
  assert.doesNotMatch(themeSelector, /role="group"|aria-pressed=/);
  assert.doesNotMatch(themeSelector, /<fieldset|<input/i);
  assert.match(css, /\.topbar-controls\s*\{[^}]*margin-left:\s*auto;[^}]*justify-content:\s*flex-end;/s);
  assert.match(css, /\.preference-icon-button\s*\{[^}]*width:\s*var\(--db-control-height-comfortable\);[^}]*display:\s*grid;/s);
  assert.doesNotMatch(css, /button[^{}]*\{[^}]*min-height:\s*(?:34px|var\(--db-control-height-compact\))/s);

  for (const automationId of ["LanguagePreferenceButton", "ThemePreferenceButton"]) {
    assert.match(desktopXaml, new RegExp(`<Button x:Name="${automationId}"`));
  }
  for (const icon of ["LightThemeIcon", "DarkThemeIcon"]) {
    assert.match(desktopXaml, new RegExp(`x:Name="${icon}"`));
  }
  assert.doesNotMatch(desktopXaml, /SystemThemeIcon/);
  assert.doesNotMatch(desktopXaml, /PortugueseLanguageButton|BritishEnglishLanguageButton|TopbarPreferenceRadio/);
  assert.match(desktopXaml, /<WrapPanel Grid.Column="1" HorizontalAlignment="Right"/);
  assert.match(desktopXaml, /AutomationProperties.Name="\{DynamicResource Language.Label\}"/);
  assert.match(desktopXaml, /AutomationProperties.Name="\{DynamicResource Theme.Label\}"/);
  assert.doesNotMatch(desktopTheme, /AppsUseLightTheme|ThemePreference\.System/);
  assert.match(desktopTheme, /SystemParameters\.HighContrast/);
  assert.match(desktopTheme, /DesignTokens\.\{resolved\}\.xaml/);
  assert.match(desktopPreferences, /Current with \{ Language = language \}/);
  assert.match(desktopPreferences, /Current with \{ Theme = theme \}/);
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
      ["colour.chrome.foreground", "colour.chrome.background"],
      ["colour.chrome.muted", "colour.chrome.background"],
      ["colour.chrome.selected.foreground", "colour.chrome.selected.background"],
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
    for (let category = 1; category <= 5; category += 1) {
      const foregroundKey = `colour.data.category.${category}`;
      const foreground = resolveColour(semantic[foregroundKey], core);
      const background = resolveColour(semantic["colour.surface.raised"], core);
      assert.ok(contrastRatio(foreground, background) >= 4.5, `${theme}: ${foregroundKey} on colour.surface.raised`);
    }
  }
});

test("provider presentation uses only neutral categorical tokens and locally defined generic geometry", () => {
  const core = readFileSync(new URL("../../../design-system/tokens/core.tokens.json", import.meta.url), "utf8");
  const components = readFileSync(new URL("../../../design-system/tokens/components.tokens.json", import.meta.url), "utf8");
  const providerIcon = readFileSync(new URL("../src/ProviderIcon.tsx", import.meta.url), "utf8");
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");

  assert.doesNotMatch(core, /palette\.provider\./);
  assert.doesNotMatch(components, /component\.provider\./);
  assert.doesNotMatch(providerIcon, /providerType ===|provider-\$\{providerType\}/);
  assert.match(providerIcon, /<ellipse cx="16" cy="8" rx="10" ry="4"\/>/);
  assert.match(css, /provider-ring-segment\.segment-5[^{]*\{[^}]*--db-colour-data-category-5/s);
  assert.doesNotMatch(css, /provider-ring-segment[^}]*--db-(?:component|colour)-status/s);
});
