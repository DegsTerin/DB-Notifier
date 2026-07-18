/** Module purpose: Verifies the bounded local provider-icon registry, safe fallback and categorical cycling contracts. */
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import test from "node:test";
import {
  providerIconRegistry,
  resolveProviderCategorySlot,
  resolveProviderIconAssets,
  type ProviderIconAssets,
} from "../src/providerIconRegistry.ts";

const expectedRegistry: Readonly<Record<string, ProviderIconAssets>> = {
  cassandra: { lightPath: "/provider-icons/cassandra-light.svg", darkPath: "/provider-icons/cassandra-dark.svg" },
  dynamodb: { lightPath: "/provider-icons/dynamodb-light.svg", darkPath: "/provider-icons/dynamodb-dark.svg" },
  elasticsearch: { lightPath: "/provider-icons/elasticsearch-light.svg", darkPath: "/provider-icons/elasticsearch-dark.svg" },
  firebase: { lightPath: "/provider-icons/firebase-light.svg", darkPath: "/provider-icons/firebase-dark.svg" },
  mongodb: { lightPath: "/provider-icons/mongodb-light.svg", darkPath: "/provider-icons/mongodb-dark.svg" },
  mysql: { lightPath: "/provider-icons/mysql-light.svg", darkPath: "/provider-icons/mysql-dark.svg" },
  planetscale: { lightPath: "/provider-icons/planetscale-light.svg", darkPath: "/provider-icons/planetscale-dark.svg" },
  postgresql: { lightPath: "/provider-icons/postgresql-light.svg", darkPath: "/provider-icons/postgresql-dark.svg" },
  redis: { lightPath: "/provider-icons/redis-light.svg", darkPath: "/provider-icons/redis-dark.svg" },
  sqlite: { lightPath: "/provider-icons/sqlite-light.svg", darkPath: "/provider-icons/sqlite-dark.svg" },
  supabase: { lightPath: "/provider-icons/supabase-light.svg", darkPath: "/provider-icons/supabase-dark.svg" },
};
const canonicalManifest = JSON.parse(
  readFileSync(new URL("../../../design-system/provider-icons/manifest.json", import.meta.url), "utf8"),
) as { readonly icons: readonly { readonly providerType: string }[] };

test("provider registry exposes only exact identifiers and literal local theme variants", () => {
  assert.deepEqual(providerIconRegistry, expectedRegistry);
  assert.deepEqual(Object.keys(providerIconRegistry), canonicalManifest.icons.map((icon) => icon.providerType));
  for (const [providerType, expected] of Object.entries(expectedRegistry)) {
    assert.deepEqual(resolveProviderIconAssets(providerType), expected);
    for (const path of [expected.lightPath, expected.darkPath]) {
      assert.match(path, /^\/provider-icons\/[a-z]+-(?:light|dark)\.svg$/);
      assert.doesNotMatch(path, /^(?:https?:)?\/\//);
    }
  }
});

test("unknown or path-like provider values fail safely to the generic fallback", () => {
  for (const providerType of ["mongo", "sql-server", "PostgreSQL", "../postgresql", "https://example.test/icon.svg", "__proto__", ""]) {
    assert.equal(resolveProviderIconAssets(providerType), undefined);
  }
});

test("every registered path resolves to a vendored SVG asset", () => {
  for (const assets of Object.values(providerIconRegistry)) {
    for (const path of [assets.lightPath, assets.darkPath]) {
      const assetUrl = new URL(`../public${path}`, import.meta.url);
      assert.equal(existsSync(fileURLToPath(assetUrl)), true, path);
      assert.match(readFileSync(assetUrl, "utf8"), /<svg\b/i, path);
    }
  }
});

test("provider category slots cycle without producing unstyled classes", () => {
  assert.deepEqual(Array.from({ length: 12 }, (_, index) => resolveProviderCategorySlot(index)), [1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2]);
  assert.equal(resolveProviderCategorySlot(-1), 1);
  assert.equal(resolveProviderCategorySlot(Number.NaN), 1);
  assert.equal(resolveProviderCategorySlot(1.5), 1);
});

test("ProviderIcon keeps registered images decorative and supplies local failure and forced-colour fallbacks", () => {
  const component = readFileSync(new URL("../src/ProviderIcon.tsx", import.meta.url), "utf8");
  const css = readFileSync(new URL("../src/styles.css", import.meta.url), "utf8");

  assert.match(component, /<img src=\{source\} alt=""/);
  assert.match(component, /onError=\{\(\) => setFailed\(true\)\}/);
  assert.match(component, /provider-icon-forced-colours/);
  assert.match(component, /resolveProviderIconAssets\(providerType\)/);
  assert.doesNotMatch(component, /https?:\/\//);
  assert.doesNotMatch(component, /\/provider-icons\/\$\{/);
  assert.match(css, /:root\[data-theme="dark"\] \.provider-icon-variant-light \{ display: none; \}/);
  assert.match(css, /html\[data-theme\] \.provider-icon \.provider-icon-variant \{ display: none; \}/);
  assert.match(css, /@media \(forced-colors: active\)[\s\S]*\.provider-icon-variant \{ display: none; \}[\s\S]*\.provider-icon-forced-colours \{ display: block; \}/);
});

test("provider artwork supplements visible identifiers across every provider-bearing Dashboard surface", () => {
  const app = readFileSync(new URL("../src/App.tsx", import.meta.url), "utf8");

  assert.match(app, /function ProviderIdentity[\s\S]*<ProviderIcon providerType=\{providerType\}[\s\S]*<code>\{providerType\}<\/code>/);
  assert.match(app, /overview-instance-row[\s\S]*<ProviderIcon providerType=\{item\.providerType\}/);
  assert.match(app, /function ProviderChart[\s\S]*<ProviderIcon providerType=\{provider\}/);
  assert.match(app, /provider-catalogue[\s\S]*<ProviderIcon providerType=\{item\.providerType\}/);
  assert.match(app, /function TimelineRow[\s\S]*<ProviderIdentity providerType=\{event\.providerType\}/);
  assert.match(app, /function AlertsView[\s\S]*<ProviderIdentity providerType=\{alert\.providerType\}/);
  assert.match(app, /function ConfigurationView[\s\S]*<ProviderIdentity providerType=\{snapshot\.providerType\}/);
  assert.match(app, /function InventoryTableRow[\s\S]*<ProviderIdentity providerType=\{item\.providerType\}/);
  assert.match(app, /function InventoryMobileCard[\s\S]*<ProviderIdentity providerType=\{item\.providerType\}/);
});
