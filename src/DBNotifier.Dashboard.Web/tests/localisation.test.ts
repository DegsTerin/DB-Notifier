/** Module purpose: Verifies shared pt-BR/en-GB localisation generation, persistence and translated fixtures. */
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import test from "node:test";
import { localisationMessages } from "../src/generated/localisation.ts";
import {
  defaultLocale,
  languagePreferenceStorageKey,
  parseLocale,
  persistLocale,
  readLocale,
  translate,
} from "../src/localisation.ts";
import { buildConfigurationSnapshot, buildDemonstrationSnapshot, buildTimelineAlertSnapshot } from "../src/presentation.ts";

test("canonical localisation adapters are current and expose exact locale parity", () => {
  const generator = new URL("../../../scripts/generate-localisation.mjs", import.meta.url);
  const result = spawnSync(process.execPath, [fileURLToPath(generator), "--verify"], { encoding: "utf8" });
  assert.equal(result.status, 0, result.stderr);
  assert.deepEqual(Object.keys(localisationMessages["pt-BR"]), Object.keys(localisationMessages["en-GB"]));
  assert.ok(Object.keys(localisationMessages["pt-BR"]).length >= 150);
});

test("language persistence accepts only supported BCP 47 values and fails safely", () => {
  const values = new Map<string, string>();
  const storage = {
    getItem: (key: string) => values.get(key) ?? null,
    setItem: (key: string, value: string) => { values.set(key, value); },
  };
  assert.equal(defaultLocale, "pt-BR");
  assert.equal(parseLocale("en-GB"), "en-GB");
  assert.equal(parseLocale("en-US"), "pt-BR");
  assert.equal(persistLocale(storage, "en-GB"), true);
  assert.equal(values.get(languagePreferenceStorageKey), "en-GB");
  assert.equal(readLocale(storage), "en-GB");
  assert.equal(readLocale({ getItem: () => { throw new Error("denied"); }, setItem: () => undefined }), "pt-BR");
});

test("translated messages preserve placeholders and British operational terminology", () => {
  assert.equal(translate("pt-BR", "App.Title"), "DB Notifier — Visão geral");
  assert.equal(translate("en-GB", "App.Title"), "DB Notifier — Overview");
  assert.equal(translate("pt-BR", "Theme.Toggle", "Sistema", "Claro"), "Tema: Sistema. Alternar para Claro");
  assert.equal(translate("en-GB", "Language.Toggle", "English (UK)", "Português (Brasil)"), "Language: English (UK). Switch to Português (Brasil)");
  assert.equal(translate("pt-BR", "TV.Enter"), "Ativar modo TV");
  assert.equal(translate("en-GB", "TV.Active"), "TV mode · demonstration");
  assert.equal(translate("pt-BR", "Inventory.Count", 2, 4), "2 de 4 itens visíveis");
  assert.equal(translate("en-GB", "Inventory.Count", 2, 4), "2 of 4 items visible");
  assert.match(translate("en-GB", "Configuration.AdminDescription"), /authorisation/);
  assert.match(translate("en-GB", "Preview.Unknown.Message"), /fails closed/);
});

test("operational configuration and interface preferences remain unambiguous in both locales", () => {
  assert.equal(translate("pt-BR", "Navigation.Configuration"), "Configuração operacional");
  assert.equal(translate("pt-BR", "Navigation.Settings"), "Preferências");
  assert.equal(translate("pt-BR", "View.Configuration.Title"), "Configuração operacional");
  assert.equal(translate("pt-BR", "View.Settings.Title"), "Preferências");
  assert.equal(translate("pt-BR", "TopBar.Settings"), "Abrir preferências");
  assert.equal(translate("pt-BR", "Tray.OpenConfiguration"), "Abrir configuração operacional");
  assert.equal(translate("en-GB", "Navigation.Configuration"), "Operational configuration");
  assert.equal(translate("en-GB", "Navigation.Settings"), "Preferences");
  assert.equal(translate("en-GB", "View.Configuration.Title"), "Operational configuration");
  assert.equal(translate("en-GB", "View.Settings.Title"), "Preferences");
  assert.equal(translate("en-GB", "TopBar.Settings"), "Open preferences");
  assert.equal(translate("en-GB", "Tray.OpenConfiguration"), "Open operational configuration");
});

test("demonstration fixtures localise visible copy without changing provider identifiers", () => {
  const now = new Date("2026-07-13T12:00:00.000Z");
  const inventory = buildDemonstrationSnapshot(now, "en-GB");
  const timeline = buildTimelineAlertSnapshot(now, "en-GB");
  const configuration = buildConfigurationSnapshot("en-GB");
  assert.equal(inventory.items[0].displayName, "Primary finance");
  assert.equal(inventory.items[0].providerType, "postgresql");
  assert.equal(timeline.alerts[0].ruleName, "Continuous timeout");
  assert.equal(configuration.fields[0].label, "Monitoring interval");
  assert.equal(configuration.capabilities[0].displayName, "Start");
});
