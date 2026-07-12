# STATE-03 Database Modeling Report

## Outcome

Automatic audit result: **APPROVED**. The internal DB-Notifier data model, provider-specific migrations, retention policy, and recovery guidance passed the `STATE-03` Human Gate on 2026-07-11. The gate authorizes transition to `STATE-04`; it does not authorize a production migration, deployment, or real administrative action.

## Delivered scope

- Agent local model in an isolated SQLite assembly with registration, authorized assignments, health observations, durable outbox, command inbox, and checkpoints.
- Server model in an isolated PostgreSQL assembly with inventory, Agent fleet, monitoring history, incidents, alerting, commands, RBAC, audit, and server outbox.
- Two migrations per provider: initial schema followed by canonical state constraints.
- Keys, foreign keys, delete behavior, check constraints, indexes, uniqueness, idempotency, and optimistic concurrency modeled explicitly.
- Credential material represented only by opaque monitoring, administration, certificate, or channel references.
- PostgreSQL `jsonb` used only for non-secret structured payloads/configuration.
- Append-only PostgreSQL audit protection through an update/delete rejection trigger.
- Retention defaults and deletion safeguards for central and local data.
- Migration generation, review, rollback, backup, and recovery runbook.

## Provider separation

| Runtime | Persistence assembly | Provider carried |
|---|---|---|
| Agent Worker | `DBNotifier.Persistence.Agent.Sqlite` | SQLite only |
| Server API | `DBNotifier.Persistence.Server.PostgreSql` | PostgreSQL only |
| Shared Infrastructure | `DBNotifier.Infrastructure` | EF Core relational conventions; no concrete database provider |

The solution contains 12 .NET projects. All active targets remain `net10.0` or `net10.0-windows`, selected by .NET SDK `10.0.301`.

## Migration chain

| Store | Initial | Current |
|---|---|---|
| Agent SQLite | `20260712005330_InitialAgentSchema` | `20260712005606_AddAgentStateConstraints` |
| Server PostgreSQL | `20260712005336_InitialServerSchema` | `20260712005611_AddServerStateConstraints` |

The UTC-derived migration identifiers fall on 2026-07-12 while this report uses the project timezone date, 2026-07-11.

## Verification evidence

Executed from the repository root with the workspace-local .NET SDK 10.0.301:

| Check | Result |
|---|---|
| Forced dependency restore and locked restore | Approved for all 12 projects |
| Release build with warnings as errors | Approved; 0 warnings, 0 errors |
| .NET tests | Approved; 5/5 (4 unit/model + 1 architecture) |
| SQLite initial-to-latest migration | Approved against an ephemeral in-memory database |
| SQLite constraints/idempotency | Invalid interval, status, and duplicate sequence rejected |
| SQLite rollback | Latest to previous to zero approved; modeled table removed |
| PostgreSQL migration generation | Approved offline; two migrations discovered |
| PostgreSQL idempotent script | Approved; 813 lines with `jsonb`, state constraints, and append-only audit trigger |
| PostgreSQL rollback generation | Approved offline; latest constraint migration emits `DROP CONSTRAINT` |
| Secret-column model test | Approved; credential references present and secret-value fields absent |
| `dotnet format --verify-no-changes` | Approved |
| NuGet direct/transitive vulnerability audit | Approved; no vulnerable packages reported |
| `git diff --check` | Approved |

The first SQLite dependency resolution selected a vulnerable transitive native library. The issue was corrected rather than suppressed by centrally pinning `SQLitePCLRaw.lib.e_sqlite3` `3.53.3`; the final audit reports no vulnerable package in any project.

## Limits and residual risks

- No migration was applied to PostgreSQL, production, or a monitored database. PostgreSQL SQL was generated and inspected offline only.
- Retention workers, aggregation tables, backup/PITR automation, and privileged audit archival belong to later implementation/operations states.
- Authentication provider, vault integration, mTLS enrollment, RBAC enforcement, and command execution are not implemented by this schema.
- Schema acceptance does not advertise a new functional provider; the characterized PowerShell PostgreSQL monitor remains the only functional monitoring behavior.
- A disposable PostgreSQL execution/restore exercise remains for later integration/homologation after explicit authorization.

## Human Gate walkthrough

Review samples:

1. Confirm Agent SQLite and Server PostgreSQL ownership and provider isolation.
2. Read the logical model, credential-reference invariants, and retention/deletion safeguards.
3. Inspect the initial migrations and state-constraint migrations, including the append-only audit trigger and all `Down` paths.
4. Confirm that monitored databases are never DB-Notifier persistence targets.
5. Confirm that the listed later-phase limitations remain unauthorized.

Human Gate decision: **APPROVED** by Bruno on 2026-07-11.

Accepted reservations: no PostgreSQL/production migration was executed; retention workers, vault/mTLS, authentication, RBAC enforcement, providers, and command execution remain later-phase implementation/integration work.
