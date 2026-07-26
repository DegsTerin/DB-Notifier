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
   Use a disposable PostgreSQL laboratory only when it is separately authorised, pinned, isolated, bounded and
   completely removed after the test.
9. Run locked restore, build, tests, format, and dependency vulnerability audit.

## Current migration chain

The current source chain contains six Agent SQLite migrations and nine Server PostgreSQL migrations:

- Agent SQLite: `InitialAgentSchema` → `AddAgentStateConstraints` → `AddCommandCompatibilityEnvelope` →
  `IntegrateAgentFleetClientState` → `HardenAgentFleetSandboxResilience` →
  `AddCommandTransportSafetySandbox`.
- Server PostgreSQL: `InitialServerSchema` → `AddServerStateConstraints` →
  `EnforceAgentObservationSequence` → `HardenObservationReconciliation` →
  `IntegrateAgentFleetIdentity` → `AddCommandTransportSafetySandbox` →
  `AddExplicitAlertRouting` → `AddDurableDeliveryOwnership` →
  `AddRejectedObservationSequenceLedger`.

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

`HardenAgentFleetSandboxResilience` adds the next monotonic operation fence and the all-or-none
owner/kind/fence/expiry lease tuple to `agent_fleet_state`. Existing rows start safely at fence one with no
owner. Its `Down` guard refuses while any lease tuple remains, because removing fencing under an active or
abandoned owner could allow an old process to confirm work. Local tests exercised clean `Up`, real
`BUSY/LOCKED` refusal, active-lease `Down` refusal, exact release, `Down` to the previous schema and `Up`
again. A separate read-only sandbox guard runs SQLite `quick_check(1)` and requires the exact ordered known
migration list before the child harness uses an existing file; corrupt, future and incomplete stores are
preserved and refused rather than recreated. No migration was applied outside ephemeral local fixtures.

`IntegrateAgentFleetIdentity` adds normalised enrollment-token, Agent-certificate and heartbeat-cursor
tables; canonical heartbeat digest/gap evidence; uniqueness guards; conservative legacy backfills; and a
fail-closed `Down` guard. The guard refuses to remove normalised identity evidence while an active Agent
enrolled by this feature could remain trusted through the legacy certificate pointer. Its generated SQL was
inspected, but `Up`, backfill, serialisable races and `Down` were not executed against PostgreSQL. The local
Agent Fleet E2E used SQLite `EnsureCreated`, which is not migration evidence. Migration source and offline
scripts are not evidence that any real PostgreSQL store has been migrated.

`AddRejectedObservationSequenceLedger` is the ninth Server PostgreSQL migration. It adds
`rejection_ledger_start_sequence` to each Agent observation cursor and creates
`rejected_observation_sequences`, keyed by Agent and sequence and restricted to the owning cursor. Existing
cursors are backfilled to `highest_contiguous_sequence + 1`, so no historical message identity or rejection
reason is invented; new cursors start at sequence one. The migration refuses the cutover when a legacy cursor
is already at the `bigint` maximum. Its `Down` operation refuses to discard any retained rejection row with
`ingestion.rejection_ledger_downgrade_blocked`; an empty ledger can be rolled back only with a compatible,
stopped runtime and explicit authority.

On 2026-07-26, the migration matrix passed `1/1` against a loopback-only disposable PostgreSQL 16 Alpine
container pinned to
`sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb`. The test proved the legacy
cursor backfill, durable rejection replay with the original reason, protected `Down`, rollback after explicit
fixture-ledger removal, cutover-overflow refusal and reapplication. The runner used a temporary random
credential, a bounded tmpfs data directory, no image pull, an exact ownership label and complete owned-container
cleanup. This is disposable local migration evidence only; no existing, operational or monitored PostgreSQL
database was migrated.

## Rollback rules

- Rollback target must be compatible with application/protocol and data written since upgrade.
- Back up the internal store and migration history before any non-ephemeral rollback.
- Never use rollback to alter a monitored database.
- Prefer forward-fix when Down would lose accepted data; record the decision and new migration.
- SQLite rollback tests use an in-memory ephemeral database and validate latest→previous→zero.
- PostgreSQL Down scripts are reviewed/generated offline by default. Only the
  `AddRejectedObservationSequenceLedger` latest→previous paths described above have physical disposable-lab
  evidence; other PostgreSQL rollback paths remain unproved unless their owning report says otherwise.
