/**
 * Generated from design-system/provider-icons/manifest.json. Do not edit manually.
 * The registry contains presentation assets only and makes no provider-support claim.
 */

/** Theme-specific local assets for one exact provider identifier. */
export interface ProviderIconAssets {
  readonly lightPath: string;
  readonly darkPath: string;
}

/** Exact provider identifiers and their fixed local presentation assets. */
export const providerIconRegistry = Object.freeze({
  "cassandra": Object.freeze({ lightPath: "/provider-icons/cassandra-light.svg", darkPath: "/provider-icons/cassandra-dark.svg" }),
  "dynamodb": Object.freeze({ lightPath: "/provider-icons/dynamodb-light.svg", darkPath: "/provider-icons/dynamodb-dark.svg" }),
  "elasticsearch": Object.freeze({ lightPath: "/provider-icons/elasticsearch-light.svg", darkPath: "/provider-icons/elasticsearch-dark.svg" }),
  "firebase": Object.freeze({ lightPath: "/provider-icons/firebase-light.svg", darkPath: "/provider-icons/firebase-dark.svg" }),
  "mongodb": Object.freeze({ lightPath: "/provider-icons/mongodb-light.svg", darkPath: "/provider-icons/mongodb-dark.svg" }),
  "mysql": Object.freeze({ lightPath: "/provider-icons/mysql-light.svg", darkPath: "/provider-icons/mysql-dark.svg" }),
  "planetscale": Object.freeze({ lightPath: "/provider-icons/planetscale-light.svg", darkPath: "/provider-icons/planetscale-dark.svg" }),
  "postgresql": Object.freeze({ lightPath: "/provider-icons/postgresql-light.svg", darkPath: "/provider-icons/postgresql-dark.svg" }),
  "redis": Object.freeze({ lightPath: "/provider-icons/redis-light.svg", darkPath: "/provider-icons/redis-dark.svg" }),
  "sqlite": Object.freeze({ lightPath: "/provider-icons/sqlite-light.svg", darkPath: "/provider-icons/sqlite-dark.svg" }),
  "supabase": Object.freeze({ lightPath: "/provider-icons/supabase-light.svg", darkPath: "/provider-icons/supabase-dark.svg" }),
} satisfies Readonly<Record<string, ProviderIconAssets>>);

export type RegisteredProviderIconId = keyof typeof providerIconRegistry;
