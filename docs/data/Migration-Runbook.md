# Migration and Recovery Runbook

## Scope

These commands generate/validate non-production migrations only. Applying them to production or a monitored database is prohibited without a later authorised release/migration procedure.

## Tooling

```powershell
$dotnet = ".\.dotnet\dotnet.exe"
& $dotnet tool restore
& $dotnet restore .\DBNotifier.sln --locked-mode
```

The repository pins .NET 10 and `dotnet-ef` in `global.json` and `.config/dotnet-tools.json`.

## Add an Agent SQLite migration

```powershell
& $dotnet tool run dotnet-ef migrations add <MigrationName> `
  --context DBNotifier.Persistence.Agent.Sqlite.AgentDbContext `
  --project .\src\DBNotifier.Persistence.Agent.Sqlite\DBNotifier.Persistence.Agent.Sqlite.csproj `
  --startup-project .\src\DBNotifier.Persistence.Agent.Sqlite\DBNotifier.Persistence.Agent.Sqlite.csproj `
  --output-dir Migrations
```

## Add a Server PostgreSQL migration

```powershell
& $dotnet tool run dotnet-ef migrations add <MigrationName> `
  --context DBNotifier.Persistence.Server.PostgreSql.ServerDbContext `
  --project .\src\DBNotifier.Persistence.Server.PostgreSql\DBNotifier.Persistence.Server.PostgreSql.csproj `
  --startup-project .\src\DBNotifier.Persistence.Server.PostgreSql\DBNotifier.Persistence.Server.PostgreSql.csproj `
  --output-dir Migrations
```

Design-time factories select providers without storing connection strings. Migration generation does not contact a database.

## Review checklist

1. Build with warnings-as-errors and inspect generated Up/Down operations.
2. Confirm correct context/provider and no cross-store table.
3. Confirm IDs, UTC times, required fields, max lengths, FKs/delete behaviour, unique/idempotency constraints, status checks, and indexes.
4. Search for password, secret value, connection string, private key, token value, or provider-native sensitive content.
5. Review destructive operations explicitly; a generated drop/rename is never accepted blindly.
6. For custom SQL, verify provider syntax, transactional behaviour, idempotent cleanup in Down, and least privilege.
7. Generate scripts for initial→latest, previous→latest, latest→previous, and latest→zero where supported.
8. Run SQLite migration tests against an ephemeral database and PostgreSQL script/model tests without a real target.
9. Run locked restore, build, tests, format, and dependency vulnerability audit.

## Current migration chain

- Agent SQLite: `InitialAgentSchema` → `AddAgentStateConstraints` → `AddCommandCompatibilityEnvelope` →
  `IntegrateAgentFleetClientState`.
- Server PostgreSQL: `InitialServerSchema` → `AddServerStateConstraints` →
  `EnforceAgentObservationSequence` → `HardenObservationReconciliation` →
  `IntegrateAgentFleetIdentity`.

The server initial migration adds an append-only trigger for `audit_entries`; Down removes the trigger/function before dropping tables.
`EnforceAgentObservationSequence` makes each Agent observation sequence unique. `HardenObservationReconciliation`
adds a safely backfilled attempt count with a default of one, a nullable payload hash for new exact-replay
verification, and durable Agent cursor and per-instance state tables that do not depend on raw observation
retention. Before backfilling those checkpoints, the migration aborts unless each Agent history starts at
sequence one and its row count equals its maximum sequence; this fail-closed precondition prevents a gap
from being presented as contiguous. The cursor is then set to the proved maximum sequence and per-instance
state uses the greatest canonical sequence belonging to the instance's currently assigned Agent, with
receipt time and observation identifier only as deterministic tie-breakers. A legacy row without
a payload hash cannot prove an exact replay and therefore remains fail-closed as an idempotency conflict.

The `HardenObservationReconciliation` Down operation removes the durable cursor and instance-state tables,
their constraints and indexes, and the new health-sample columns. That rollback loses reconciliation
checkpoints and payload-hash evidence, so it requires an application/protocol compatibility review and an
internal-store backup.

`IntegrateAgentFleetClientState` adds public certificate metadata and a fail-closed identity state to the
local registration, assignment tags, and the one-to-one `agent_fleet_state` row used for exact pending
heartbeat replay and last-known-valid reconciliation evidence. Existing registrations are marked
`Conflict` during `Up` rather than silently adopted. Its custom `Down` guard refuses rollback while any
local registration exists, because removing identity/revocation state could make an older runtime trust a
row it can no longer classify. The sandbox migration tests exercised clean `Up`, refusal with a registration,
explicit removal of the test identity, `Down` to the preceding migration and `Up` again. This is local
SQLite evidence only; no operational store was touched.

`IntegrateAgentFleetIdentity` adds normalised enrollment-token, Agent-certificate and heartbeat-cursor
tables; canonical heartbeat digest/gap evidence; uniqueness guards; conservative legacy backfills; and a
fail-closed `Down` guard. The guard refuses to remove normalised identity evidence while an active Agent
enrolled by this feature could remain trusted through the legacy certificate pointer. Its generated SQL was
inspected, but `Up`, backfill, serialisable races and `Down` were not executed against PostgreSQL. The local
Agent Fleet E2E used SQLite `EnsureCreated`, which is not migration evidence. Migration source and offline
scripts are not evidence that any real PostgreSQL store has been migrated.

## Rollback rules

- Rollback target must be compatible with application/protocol and data written since upgrade.
- Back up the internal store and migration history before any non-ephemeral rollback.
- Never use rollback to alter a monitored database.
- Prefer forward-fix when Down would lose accepted data; record the decision and new migration.
- SQLite rollback tests use an in-memory ephemeral database and validate latest→previous→zero.
- PostgreSQL Down scripts are reviewed/generated offline; execution remains unproved and requires separate authority for a disposable PostgreSQL sandbox.
