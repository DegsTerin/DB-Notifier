export const inventorySchemaVersion = "inventory.v1" as const;
export const staleAfterMilliseconds = 5 * 60 * 1000;

export type InventoryState = "ready" | "loading" | "empty" | "offline" | "error" | "denied";
export type HealthStatus =
  | "healthy"
  | "degraded"
  | "unavailable"
  | "authFailed"
  | "timeout"
  | "maintenance"
  | "unknown";

export interface InventoryItem {
  instanceId: string;
  displayName: string;
  providerType: string;
  supportLabel: string;
  environment: string;
  locationLabel: string;
  status: HealthStatus;
  observedAt: string;
  receivedAt: string;
  latencyMilliseconds: number | null;
  enabled: boolean;
}

export interface InventorySnapshot {
  schemaVersion: typeof inventorySchemaVersion;
  generatedAt: string;
  items: readonly InventoryItem[];
}

export interface InventorySummary {
  total: number;
  healthy: number;
  degraded: number;
  attentionRequired: number;
  stale: number;
}

export function isStale(item: InventoryItem, now: Date): boolean {
  return now.getTime() - Date.parse(item.receivedAt) > staleAfterMilliseconds;
}

export function summarizeInventory(snapshot: InventorySnapshot, now: Date): InventorySummary {
  return snapshot.items.reduce<InventorySummary>(
    (summary, item) => {
      const stale = isStale(item, now);
      summary.total += 1;
      summary.stale += stale ? 1 : 0;
      summary.healthy += item.status === "healthy" && !stale ? 1 : 0;
      summary.degraded += item.status === "degraded" && !stale ? 1 : 0;
      summary.attentionRequired += ["unavailable", "authFailed", "timeout"].includes(item.status) ? 1 : 0;
      return summary;
    },
    { total: 0, healthy: 0, degraded: 0, attentionRequired: 0, stale: 0 },
  );
}

export function filterInventory(
  items: readonly InventoryItem[],
  query: string,
  status: "all" | HealthStatus | "stale",
  now: Date,
): readonly InventoryItem[] {
  const normalizedQuery = query.trim().toLocaleLowerCase("pt-BR");
  return items.filter((item) => {
    const matchesQuery =
      normalizedQuery.length === 0 ||
      [item.displayName, item.providerType, item.environment, item.locationLabel]
        .join(" ")
        .toLocaleLowerCase("pt-BR")
        .includes(normalizedQuery);
    const matchesStatus = status === "all" || (status === "stale" ? isStale(item, now) : item.status === status);
    return matchesQuery && matchesStatus;
  });
}

export function buildDemonstrationSnapshot(now: Date): InventorySnapshot {
  const isoAt = (offsetMilliseconds: number) => new Date(now.getTime() - offsetMilliseconds).toISOString();
  const item = (
    instanceId: string,
    displayName: string,
    providerType: string,
    supportLabel: string,
    environment: string,
    locationLabel: string,
    status: HealthStatus,
    ageMilliseconds: number,
    latencyMilliseconds: number | null,
  ): InventoryItem => ({
    instanceId,
    displayName,
    providerType,
    supportLabel,
    environment,
    locationLabel,
    status,
    observedAt: isoAt(ageMilliseconds + 1_000),
    receivedAt: isoAt(ageMilliseconds),
    latencyMilliseconds,
    enabled: true,
  });

  return {
    schemaVersion: inventorySchemaVersion,
    generatedAt: now.toISOString(),
    items: [
      item("demo-001", "Financeiro principal", "postgresql", "Implementado · não homologado", "Produção", "Datacenter SP", "healthy", 38_000, 24),
      item("demo-002", "Pedidos regional", "mysql", "Planejado · não implementado", "Produção", "Cloud privado", "degraded", 120_000, 86),
      item("demo-003", "Analytics", "sql-server", "Planejado · não implementado", "Homologação", "Azure", "timeout", 180_000, null),
      item("demo-004", "Catálogo", "mongodb", "Planejado · não implementado", "Desenvolvimento", "Linux local", "unknown", 540_000, null),
    ],
  };
}
