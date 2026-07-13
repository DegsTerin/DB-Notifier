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
  assert.equal(translate("pt-BR", "Inventory.Count", 2, 4), "2 de 4 itens visíveis");
  assert.equal(translate("en-GB", "Inventory.Count", 2, 4), "2 of 4 items visible");
  assert.match(translate("en-GB", "Configuration.AdminDescription"), /authorisation/);
  assert.match(translate("en-GB", "Preview.Unknown.Message"), /fails closed/);
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
