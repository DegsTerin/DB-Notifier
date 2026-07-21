/** Module purpose: Implements presentation for the provider-neutral DB-Notifier Dashboard without direct database access. */
import type { SupportedLocale } from "./generated/localisation";
import { translate } from "./localisation.ts";

export const inventorySchemaVersion = "inventory.v1" as const;
export const staleAfterMilliseconds = 5 * 60 * 1000;

export type InventoryState = "ready" | "loading" | "empty" | "offline" | "error" | "denied" | "maintenance";
export type EventSeverity = "information" | "warning" | "critical";
export type AlertState = "active" | "acknowledged" | "silenced" | "resolved";
export type CapabilityState = "supported" | "unsupported" | "unavailable" | "unknown";
export type ActionPreview = "confirmationRequired" | "denied" | "unsupported" | "unavailable" | "unknown";
export type FleetAggregateState = "healthy" | "warning" | "critical" | "unknown";
export type EvidenceFreshness = "current" | "stale" | "unknown";
export type InventoryFilterState = "all" | HealthStatus | "stale" | "disabled";
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
  warning: number;
  attentionRequired: number;
  stale: number;
  disabled: number;
}

export interface TimelineEventItem {
  eventId: string; instanceId: string; instanceName: string; providerType: string;
  eventType: string; severity: EventSeverity; summary: string; occurredAt: string; receivedAt: string;
}

export interface AlertItem {
  alertId: string; instanceId: string; instanceName: string; providerType: string;
  severity: EventSeverity; state: AlertState; ruleName: string; summary: string; openedAt: string; updatedAt: string;
}

export interface TimelineAlertSnapshot {
  schemaVersion: "history-alerts.v1"; generatedAt: string;
  events: readonly TimelineEventItem[]; alerts: readonly AlertItem[];
}

export interface ConfigurationCapabilitySnapshot {
  schemaVersion: "configuration-capabilities.v1"; instanceId: string; instanceName: string; providerType: string;
  fields: readonly { key: string; label: string; safeValue: string; description: string }[];
  capabilities: readonly { capabilityId: string; displayName: string; state: CapabilityState; reasonCode: string; requiresConfirmation: boolean }[];
}

/**
 * Classifies temporal evidence without allowing invalid or future timestamps to imply current health.
 * @param item - Inventory item whose observation and receipt timestamps came from a presentation boundary.
 * @param now - Clock instant used for deterministic freshness evaluation.
 * @returns Current, stale or Unknown; invalid ordering and future evidence always fail to Unknown.
 */
export function classifyEvidenceFreshness(item: InventoryItem, now: Date): EvidenceFreshness {
  const nowMilliseconds = now.getTime();
  const observedMilliseconds = Date.parse(item.observedAt);
  const receivedMilliseconds = Date.parse(item.receivedAt);
  if (![nowMilliseconds, observedMilliseconds, receivedMilliseconds].every(Number.isFinite) ||
      observedMilliseconds > receivedMilliseconds ||
      observedMilliseconds > nowMilliseconds ||
      receivedMilliseconds > nowMilliseconds) {
    return "unknown";
  }
  return nowMilliseconds - receivedMilliseconds > staleAfterMilliseconds ? "stale" : "current";
}

/**
 * Returns whether valid evidence has exceeded the configured freshness threshold.
 * @param item - Inventory evidence whose timestamp ordering is validated before age is considered.
 * @param now - Clock instant used for deterministic freshness evaluation.
 * @returns True only for valid stale evidence; invalid or future evidence remains Unknown.
 */
export function isStale(item: InventoryItem, now: Date): boolean {
  return classifyEvidenceFreshness(item, now) === "stale";
}

/**
 * Returns a recognised primitive health status, failing untrusted runtime values safely to Unknown.
 * @param status - Untrusted status value received at the presentation boundary.
 * @returns One canonical health-status string.
 */
export function normalizeHealthStatus(status: unknown): HealthStatus {
  const candidate = String(status);
  return ["healthy", "degraded", "unavailable", "authFailed", "timeout", "maintenance", "unknown"].includes(candidate)
    ? candidate as HealthStatus
    : "unknown";
}

/** Returns a recognised event severity without allowing an unknown runtime value to imply Information. */
export function normalizeEventSeverity(severity: unknown): EventSeverity | "unknown" {
  return severity === "information" || severity === "warning" || severity === "critical" ? severity : "unknown";
}

/** Returns a recognised alert state without allowing an unknown runtime value to imply Resolved. */
export function normalizeAlertState(state: unknown): AlertState | "unknown" {
  return state === "active" || state === "acknowledged" || state === "silenced" || state === "resolved" ? state : "unknown";
}

/** Returns a recognised capability state without exposing an untrusted machine value as interface copy. */
export function normalizeCapabilityState(state: unknown): CapabilityState {
  return state === "supported" || state === "unsupported" || state === "unavailable" || state === "unknown" ? state : "unknown";
}

/**
 * Summarises only current evidence into health KPIs while retaining stale counts independently.
 * @param snapshot - Immutable inventory evidence to classify.
 * @param now - Clock instant used for deterministic freshness evaluation.
 * @returns Coherent KPI counts that never report stale or invalid evidence as current health.
 */
export function summarizeInventory(snapshot: InventorySnapshot, now: Date): InventorySummary {
  return snapshot.items.reduce<InventorySummary>(
    (summary, item) => {
      const freshness = classifyEvidenceFreshness(item, now);
      const current = freshness === "current";
      const status = normalizeHealthStatus(item.status);
      summary.total += 1;
      if (!item.enabled) {
        summary.disabled += 1;
        return summary;
      }
      summary.stale += freshness === "stale" ? 1 : 0;
      summary.healthy += status === "healthy" && current ? 1 : 0;
      summary.degraded += status === "degraded" && current ? 1 : 0;
      summary.warning += ["degraded", "maintenance"].includes(status) && current ? 1 : 0;
      summary.attentionRequired += ["unavailable", "authFailed", "timeout"].includes(status) && current ? 1 : 0;
      return summary;
    },
    { total: 0, healthy: 0, degraded: 0, warning: 0, attentionRequired: 0, stale: 0, disabled: 0 },
  );
}

/**
 * Reduces provider-neutral health and freshness evidence to the semantic state used by every product-mark surface.
 * @param snapshot - Current inventory evidence without provider-specific branches.
 * @param now - Clock instant used to classify stale observations consistently with the visible inventory.
 * @returns Critical, Warning, Unknown or Healthy using the same precedence as the Windows Tray policy.
 */
export function summarizeFleetAggregate(snapshot: InventorySnapshot, now: Date): FleetAggregateState {
  let warning = false;
  let critical = false;
  let unknown = false;
  let enabledCount = 0;

  for (const item of snapshot.items) {
    if (!item.enabled) continue;
    enabledCount += 1;
    if (classifyEvidenceFreshness(item, now) !== "current") {
      unknown = true;
      continue;
    }
    const status = normalizeHealthStatus(item.status);
    if (["unavailable", "authFailed", "timeout"].includes(status)) critical = true;
    else if (["degraded", "maintenance"].includes(status)) warning = true;
    else if (status === "unknown") unknown = true;
  }

  if (critical) return "critical";
  if (warning) return "warning";
  if (unknown || enabledCount === 0) return "unknown";
  return "healthy";
}

export function filterInventory(
  items: readonly InventoryItem[],
  query: string,
  status: InventoryFilterState,
  now: Date,
  locale: SupportedLocale = "pt-BR",
): readonly InventoryItem[] {
  const normalizedQuery = query.trim().toLocaleLowerCase(locale);
  return items.filter((item) => {
    const matchesQuery =
      normalizedQuery.length === 0 ||
      [item.displayName, item.providerType, item.environment, item.locationLabel]
        .join(" ")
        .toLocaleLowerCase(locale)
        .includes(normalizedQuery);
    const freshness = classifyEvidenceFreshness(item, now);
    const effectiveStatus = freshness === "unknown"
      ? "unknown"
      : freshness === "current"
        ? normalizeHealthStatus(item.status)
        : null;
    const matchesStatus = status === "all" ||
      (status === "disabled"
        ? !item.enabled
        : item.enabled && (status === "stale" ? freshness === "stale" : effectiveStatus === status));
    return matchesQuery && matchesStatus;
  });
}

export function buildDemonstrationSnapshot(now: Date, locale: SupportedLocale = "pt-BR"): InventorySnapshot {
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
    enabled = true,
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
    enabled,
  });

  return {
    schemaVersion: inventorySchemaVersion,
    generatedAt: now.toISOString(),
    items: [
      item("demo-001", translate(locale, "Sample.Instance.Finance"), "postgresql", translate(locale, "Sample.Support.Implemented"), translate(locale, "Sample.Environment.Production"), translate(locale, "Sample.Location.Datacentre"), "healthy", 38_000, 24),
      item("demo-002", translate(locale, "Sample.Instance.Orders"), "mysql", translate(locale, "Sample.Support.Planned"), translate(locale, "Sample.Environment.Production"), translate(locale, "Sample.Location.PrivateCloud"), "degraded", 120_000, 86),
      item("demo-003", translate(locale, "Sample.Instance.Analytics"), "sql-server", translate(locale, "Sample.Support.Planned"), translate(locale, "Sample.Environment.Validation"), "Azure", "timeout", 180_000, null),
      item("demo-004", translate(locale, "Sample.Instance.Catalogue"), "mongodb", translate(locale, "Sample.Support.Planned"), translate(locale, "Sample.Environment.Development"), translate(locale, "Sample.Location.LocalLinux"), "unknown", 540_000, null, false),
    ],
  };
}

export function buildTimelineAlertSnapshot(now: Date, locale: SupportedLocale = "pt-BR"): TimelineAlertSnapshot {
  const at = (minutes: number) => new Date(now.getTime() - minutes * 60_000).toISOString();
  return {
    schemaVersion: "history-alerts.v1",
    generatedAt: now.toISOString(),
    events: [
      { eventId: "event-001", instanceId: "demo-001", instanceName: translate(locale, "Sample.Instance.Finance"), providerType: "postgresql", eventType: "Recovered", severity: "information", summary: translate(locale, "Sample.Event.Recovered"), occurredAt: at(2), receivedAt: at(2) },
      { eventId: "event-002", instanceId: "demo-002", instanceName: translate(locale, "Sample.Instance.Orders"), providerType: "mysql", eventType: "Degraded", severity: "warning", summary: translate(locale, "Sample.Event.Degraded"), occurredAt: at(7), receivedAt: at(7) },
      { eventId: "event-003", instanceId: "demo-003", instanceName: translate(locale, "Sample.Instance.Analytics"), providerType: "sql-server", eventType: "Timeout", severity: "critical", summary: translate(locale, "Sample.Event.Timeout"), occurredAt: at(14), receivedAt: at(14) },
      { eventId: "event-004", instanceId: "demo-004", instanceName: translate(locale, "Sample.Instance.Catalogue"), providerType: "mongodb", eventType: "MaintenanceStarted", severity: "information", summary: translate(locale, "Sample.Event.Maintenance"), occurredAt: at(24), receivedAt: at(24) },
    ],
    alerts: [
      { alertId: "alert-001", instanceId: "demo-003", instanceName: translate(locale, "Sample.Instance.Analytics"), providerType: "sql-server", severity: "critical", state: "active", ruleName: translate(locale, "Sample.Alert.TimeoutRule"), summary: translate(locale, "Sample.Alert.TimeoutSummary"), openedAt: at(16), updatedAt: at(3) },
      { alertId: "alert-002", instanceId: "demo-002", instanceName: translate(locale, "Sample.Instance.Orders"), providerType: "mysql", severity: "warning", state: "acknowledged", ruleName: translate(locale, "Sample.Alert.LatencyRule"), summary: translate(locale, "Sample.Alert.LatencySummary"), openedAt: at(22), updatedAt: at(8) },
      { alertId: "alert-003", instanceId: "demo-001", instanceName: translate(locale, "Sample.Instance.Finance"), providerType: "postgresql", severity: "information", state: "resolved", ruleName: translate(locale, "Overview.Restarted"), summary: translate(locale, "Sample.Event.Recovered"), openedAt: at(26), updatedAt: at(24) },
    ],
  };
}

export function filterTimeline(
  events: readonly TimelineEventItem[], query: string, severity: "all" | EventSeverity, locale: SupportedLocale = "pt-BR",
): readonly TimelineEventItem[] {
  const normalized = query.trim().toLocaleLowerCase(locale);
  return events.filter((event) =>
    (severity === "all" || event.severity === severity) &&
    (normalized.length === 0 || [event.instanceName, event.providerType, event.eventType, event.summary]
      .join(" ").toLocaleLowerCase(locale).includes(normalized)));
}

export function buildConfigurationSnapshot(locale: SupportedLocale = "pt-BR"): ConfigurationCapabilitySnapshot {
  return {
    schemaVersion: "configuration-capabilities.v1",
    instanceId: "demo-001", instanceName: translate(locale, "Sample.Instance.Finance"), providerType: "postgresql",
    fields: [
      { key: "monitoring.interval", label: translate(locale, "Sample.Config.Interval.Label"), safeValue: translate(locale, "Sample.Config.Interval.Value"), description: translate(locale, "Sample.Config.Interval.Description") },
      { key: "monitoring.timeout", label: translate(locale, "Sample.Config.Timeout.Label"), safeValue: translate(locale, "Sample.Config.Timeout.Value"), description: translate(locale, "Sample.Config.Timeout.Description") },
      { key: "monitoring.retry", label: translate(locale, "Sample.Config.Retry.Label"), safeValue: translate(locale, "Sample.Config.Retry.Value"), description: translate(locale, "Sample.Config.Retry.Description") },
      { key: "credential.reference", label: translate(locale, "Sample.Config.Credential.Label"), safeValue: translate(locale, "Sample.Config.Credential.Value"), description: translate(locale, "Sample.Config.Credential.Description") },
    ],
    capabilities: [
      { capabilityId: "service.start", displayName: translate(locale, "Action.Start"), state: "unsupported", reasonCode: "provider.control_unsupported", requiresConfirmation: true },
      { capabilityId: "service.stop", displayName: translate(locale, "Action.Stop"), state: "unsupported", reasonCode: "provider.control_unsupported", requiresConfirmation: true },
      { capabilityId: "service.restart", displayName: translate(locale, "Action.Restart"), state: "unsupported", reasonCode: "provider.control_unsupported", requiresConfirmation: true },
    ],
  };
}

export function previewAction(
  snapshot: ConfigurationCapabilitySnapshot, capabilityId: string, authorized: boolean,
): ActionPreview {
  const capability = snapshot.capabilities.find((item) => item.capabilityId === capabilityId);
  if (!capability) return "unknown";
  if (!authorized) return "denied";
  if (capability.state === "supported" && capability.requiresConfirmation) return "confirmationRequired";
  if (capability.state === "unsupported" || capability.state === "unavailable") return capability.state;
  return "unknown";
}
