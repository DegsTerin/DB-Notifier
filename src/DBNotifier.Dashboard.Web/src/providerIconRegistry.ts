/**
 * Module purpose: Declares the local provider-icon catalogue used by the Dashboard presentation boundary.
 * Paths are fixed application assets; unknown provider identifiers remain valid and use the generic glyph in the owning component.
 */

/** Theme-specific local assets for one exact provider identifier. */
export interface ProviderIconAssets {
  readonly lightPath: string;
  readonly darkPath: string;
}

/** The five provider-neutral categorical slots defined by the Design System. */
export type ProviderCategorySlot = 1 | 2 | 3 | 4 | 5;

/**
 * Exact presentation identifiers with locally vendored Light and Dark variants.
 * Literal paths prevent provider data from becoming part of a URL or filesystem path.
 */
export const providerIconRegistry = Object.freeze({
  cassandra: Object.freeze({ lightPath: "/provider-icons/cassandra-light.svg", darkPath: "/provider-icons/cassandra-dark.svg" }),
  dynamodb: Object.freeze({ lightPath: "/provider-icons/dynamodb-light.svg", darkPath: "/provider-icons/dynamodb-dark.svg" }),
  elasticsearch: Object.freeze({ lightPath: "/provider-icons/elasticsearch-light.svg", darkPath: "/provider-icons/elasticsearch-dark.svg" }),
  firebase: Object.freeze({ lightPath: "/provider-icons/firebase-light.svg", darkPath: "/provider-icons/firebase-dark.svg" }),
  mongodb: Object.freeze({ lightPath: "/provider-icons/mongodb-light.svg", darkPath: "/provider-icons/mongodb-dark.svg" }),
  mysql: Object.freeze({ lightPath: "/provider-icons/mysql-light.svg", darkPath: "/provider-icons/mysql-dark.svg" }),
  planetscale: Object.freeze({ lightPath: "/provider-icons/planetscale-light.svg", darkPath: "/provider-icons/planetscale-dark.svg" }),
  postgresql: Object.freeze({ lightPath: "/provider-icons/postgresql-light.svg", darkPath: "/provider-icons/postgresql-dark.svg" }),
  redis: Object.freeze({ lightPath: "/provider-icons/redis-light.svg", darkPath: "/provider-icons/redis-dark.svg" }),
  sqlite: Object.freeze({ lightPath: "/provider-icons/sqlite-light.svg", darkPath: "/provider-icons/sqlite-dark.svg" }),
  supabase: Object.freeze({ lightPath: "/provider-icons/supabase-light.svg", darkPath: "/provider-icons/supabase-dark.svg" }),
} satisfies Readonly<Record<string, ProviderIconAssets>>);

export type RegisteredProviderIconId = keyof typeof providerIconRegistry;

/**
 * Resolves fixed local assets for an exact provider identifier.
 * @param providerType - Stable provider identifier received from the presentation contract.
 * @returns The registered Light and Dark paths, or undefined when the provider must use the generic fallback.
 */
export function resolveProviderIconAssets(providerType: string): ProviderIconAssets | undefined {
  if (!Object.prototype.hasOwnProperty.call(providerIconRegistry, providerType)) return undefined;
  return providerIconRegistry[providerType as RegisteredProviderIconId];
}

/**
 * Maps an ordinal provider position onto the bounded Design System category palette.
 * @param index - Zero-based position in the visible provider dataset.
 * @returns A stable slot from one to five; invalid positions fail safely to the first neutral category.
 */
export function resolveProviderCategorySlot(index: number): ProviderCategorySlot {
  if (!Number.isSafeInteger(index) || index < 0) return 1;
  return ((index % 5) + 1) as ProviderCategorySlot;
}
