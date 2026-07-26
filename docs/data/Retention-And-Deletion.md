# Retention and Deletion Policy

## Authority

These values are accepted design defaults from ADR-0004 for schema, index, capacity, and test design. They do not authorize production deletion. Environment policy, legal hold, backup, and Human Gate requirements can extend them.

## Central PostgreSQL

| Data | Design default | Deletion/aggregation rule |
|---|---:|---|
| Raw `health_samples` | 30 days | aggregate first; delete bounded time partitions/batches; preserve incident/event links |
| Hourly aggregates (future table) | 13 months | created before raw deletion; version aggregation algorithm |
| `agent_heartbeats` | 90 days | retain fleet availability summaries separately if introduced |
| `events` and `incidents` | 24 months | never delete an open incident; preserve correlations |
| `administrative_commands` and attempts | 24 months minimum | retain longer when audit/legal policy requires |
| `audit_entries` | 24 months minimum | append-only; protected from ordinary retention worker deletion until a separately privileged archival process exists |
| `notification_deliveries` | 12 months | preserve delivery evidence referenced by active incident/audit policy |
| server `outbox_messages` | published + 7-day tombstone | unpublished rows never expire silently |
| `rejected_observation_sequences` | lifetime of the Agent observation stream/cursor | no ordinary time-based deletion; preserve rejected-slot identity and reason until a separately authorised durable compaction exists |
| catalog/RBAC/configuration | active + explicit archive policy | restrict deletion while evidence references it |

## Agent SQLite

| Data | Design default | Bound |
|---|---:|---|
| local health observations | 7 days | also constrained by configured maximum rows/bytes |
| acknowledged outbox | 24-hour tombstone | only after the Server contiguous cursor covers the accepted, duplicate or terminally rejected sequence |
| unacknowledged outbox | until acknowledged | hard age/size limit emits explicit overflow/degraded event; never silently reports healthy |
| terminal inbox commands | 30 days | preserve idempotency key/result through maximum server retry window |
| assignments/checkpoints/registration | while active | replace atomically; keep last-known-valid config |

## Deletion safety

- Retention worker uses bounded, idempotent batches with metrics for selected/deleted/failed rows.
- Deletion checks legal hold, incident/command/audit references, backup policy, and aggregate completion.
- Time comparison uses server UTC for central data and explicit creation/ack times locally.
- A failed retention batch is retryable and never blocks monitoring ingestion indefinitely.
- Retention never touches monitored databases or external vault secrets.
- Audit deletion/archival uses a separate privileged procedure; the application runtime role cannot update/delete audit rows.
- Raw accepted samples at or after an Agent's rejection-ledger cutover cannot be deleted until a durable accepted-slot
  tombstone or equivalent resolution ledger has been implemented. Otherwise a legitimate accepted slot would become
  indistinguishable from missing post-cutover evidence.

## Implemented runtime controls

- `AgentRetentionStore` and `ServerMaintenanceStore` select each retention class with parameterized time cutoffs and a maximum of 5,000 rows per class/cycle.
- Agent and Server hosted workers are disabled by default; enabling retention still defaults to dry-run until `ApplyChanges` is explicitly set.
- `ServerMaintenanceStore` currently preserves every central raw observation until the aggregate-before-delete
  contract exists and never selects `rejected_observation_sequences`. Audit entries are also never selected.
- Unacknowledged Agent outbox and unpublished Server outbox rows are never selected as tombstones.
- This implementation does not evaluate organization-specific legal holds or backup completion. Production apply remains unauthorized until those integrations and the applicable Human Gate exist.

## Recovery objectives for modeling

- Agent SQLite: rebuildable from central config plus retained local outbox; target RPO is the acknowledged Server
  cursor of contiguously resolved slots, not the last displayed health. A local or request-level rejection without
  that cursor proof remains unacknowledged.
- Central PostgreSQL: backup/PITR design must preserve catalog, commands, RBAC, audit, and outbox consistency.
- Central PostgreSQL recovery must preserve each observation cursor, its rejection-ledger cutover and every retained
  rejected-slot row atomically. A covered post-cutover slot with neither accepted nor rejected evidence fails closed
  as retryable inconsistency rather than being acknowledged.
- Restore tests in later phases validate schema version, migration history, idempotency records, and replay behavior before service resumes.
