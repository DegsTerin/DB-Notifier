# DB-Notifier Data Model

## Scope

This pack defines the internal DB-Notifier storage model for `STATE-03 DATABASE_MODELING`. It does not alter or migrate any monitored database.

## Storage ownership

| Store | Owner | Purpose | Provider |
|---|---|---|---|
| Agent local store | DB-Notifier Agent | authorized assignments, last observations, inbox/outbox, checkpoints, offline recovery | SQLite |
| Central store | DB-Notifier Server/API | inventory, Agent fleet, history, incidents, alerting, commands, RBAC, audit, server outbox | PostgreSQL |
| Monitored database | customer/provider | monitored workload only | never used as DB-Notifier persistence |
| Secret vault | OS/corporate vault | resolved monitoring/admin secret material | referenced, never copied into these stores |

## Implementation map

- Agent SQLite model/context: `src/DBNotifier.Persistence.Agent.Sqlite/`.
- Server PostgreSQL model/context: `src/DBNotifier.Persistence.Server.PostgreSql/`.
- Shared relational conventions only: `src/DBNotifier.Infrastructure/Persistence/`.
- Provider-specific migrations are stored under each context's `Migrations/` folder.
- Tool version is pinned in `.config/dotnet-tools.json`.
- Tests are in `tests/DBNotifier.UnitTests/PersistenceModelTests.cs`.

## Documents

- [Logical model and invariants](Logical-Model.md)
- [Retention and deletion](Retention-And-Deletion.md)
- [Migration and recovery runbook](Migration-Runbook.md)

Architecture authority remains in [`../architecture/ADR-0004-Persistence-And-Retention.md`](../architecture/ADR-0004-Persistence-And-Retention.md) and the canonical contract remains in [`../architecture/Canonical-Contracts.md`](../architecture/Canonical-Contracts.md).

## Safety properties

- IDs are application-generated UUIDs.
- Times are UTC `DateTimeOffset` values.
- Mutable aggregates use explicit concurrency tokens.
- Idempotency/message identifiers have unique indexes.
- JSON columns are non-secret typed payload/config fragments; PostgreSQL uses `jsonb`.
- Credential fields are opaque references only.
- Audit entries are protected by a PostgreSQL update/delete trigger in the initial migration.
- Retention is a later worker responsibility and is not enabled by merely applying schema.
