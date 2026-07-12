# PgNotifier to DB-Notifier Migration Plan

## Status

Proposed in `STATE-00 DISCOVERY_MIGRATION`. Approval of this plan and the target baseline authorizes only the transition to `STATE-01 PROJECT_SETUP`; it does not pre-approve later architecture, implementation, deployment, or administrative actions.

## Migration principles

- Evolve incrementally and keep a runnable PostgreSQL monitoring path at every product milestone.
- Separate observation from administration and credentials from secret material.
- Place engine behavior behind provider contracts; never add engine conditionals to the core.
- Make health evidence explicit: provider, method, timestamps, latency, confidence/quality, and stale state.
- Preserve existing PgNotifier configuration as input until migration is verified; never overwrite it in place.
- Use side-by-side artifacts and reversible cutovers until the new Desktop/Agent path is homologated.
- Announce only the provider/platform combinations proven by tests and homologation.
- Keep the provider catalog open to every database engine while implementing and homologating one bounded provider slice at a time.

## Proposed technical baseline for the gate

The baseline proposed by the project vision is suitable for project setup:

- .NET 10 LTS/C# solution for Domain, Application, provider abstractions, PostgreSQL provider, infrastructure, Agent Worker, ASP.NET Core API, and WPF Desktop.
- React/TypeScript for a later Web Dashboard.
- SQLite for authorized local Agent state and outbox.
- PostgreSQL for later central persistence.
- Versioned HTTPS Agent/API contracts and SignalR for non-authoritative real-time updates.

This gate accepts only the direction and scaffold boundaries. Technology-specific commitments, secret storage, protocol compatibility, ORM, retention, signing, and provider capabilities require proposed/accepted ADRs in `STATE-02`.

## Target boundaries

```text
Legacy PgNotifier JSON
        |
Configuration migrator -- backup + validation + report
        |
DBNotifier.Application <---- DBNotifier.Provider.Abstractions
        ^                              ^
        |                              |
Agent/Desktop/API adapters      PostgreSQL provider
        |
SQLite local state/outbox (after data-model gate)
```

Domain owns canonical status and events. Application owns use cases and ports. Providers own engine-specific validation, probing, capabilities, error normalization, and approved administrative adapters. UI and transport consume Application contracts and do not authorize operations.

## Incremental milestones

### M0 — Discovery closure (`STATE-00`)

Deliverables:

- Verified legacy inventory and gaps.
- Compatibility baseline.
- Incremental plan, rollback strategy, risks, and gate evidence.
- Human decision on the baseline and permission to enter project setup.

Exit: automatic discovery checks are recorded and the Human Gate is `APROVADO` or `APROVADO COM RESSALVAS`.

### M1 — Reproducible scaffold (`STATE-01`)

Deliverables:

- Git worktree decision/initialization performed only with user authorization if still absent.
- `DBNotifier.sln` with empty projects matching approved boundaries.
- Pinned SDK/tool versions, repository-wide formatting/analyzers, deterministic restore/build, unit-test baseline, secret scan, dependency scan, and local CI definition.
- Configuration layering with safe sample values and no domain behavior.
- Legacy Pester suite retained as a separate characterization check.

Exit: clean bootstrap, build, lint/static analysis, tests, no secret, no premature business rules, and approved Human Gate.

### M2 — Architecture and contracts (`STATE-02`)

Deliverables:

- Accepted ADRs for stack/migration, vault, Agent identity, protocol/versioning, persistence/migrations, retention, update/signing, and provider capability policy.
- Threat model covering SSRF, command injection, Agent impersonation, replay, and secret exposure.
- Canonical health/event/error/capability contracts.
- PostgreSQL capability matrix plus explicit `Unsupported` results.
- Versioned legacy configuration migration contract.

Exit: dependency rules, offline behavior, compatibility, rollback, and threat scenarios pass their gates.

### M3 — Internal data model (`STATE-03`)

Deliverables:

- Schemas for instance catalog, Agent state, samples, events, incidents, outbox, commands, RBAC, and audit.
- UTC timestamps, concurrency, idempotency, indexes, retention, and rollback-tested non-production migrations.
- Opaque credential references only.

Exit: SQLite/local and PostgreSQL/central responsibilities are unambiguous and migrations are reversible in sandbox.

### M4 — PostgreSQL vertical slice (`STATE-04`)

Deliverables:

- Provider-neutral Domain/Application implementation.
- PostgreSQL provider matching characterized `pg_isready`, timeout, remote endpoint, and discovery behavior.
- TCP-only results emitted as `Degraded`, never `Healthy`.
- Local Agent scheduler, canonical samples/events, bounded retry, and isolation between instances.
- Configuration migration CLI/service with dry-run, backup, validation report, and idempotent rerun.
- Administrative command path only after RBAC, reason, expiry, idempotency, audit, and post-probe exist.

Exit: the PostgreSQL compatibility suite and negative security tests pass; no other engine is advertised.

Implementation note (2026-07-12): the backend remediation increment implements the versioned configuration migration service/CLI, typed Windows `pg_isready` discovery and deterministic negative fixtures. This note records implementation only; PostgreSQL homologation and public support remain `None`/`No`.

### M5 — Desktop transition (`STATE-05`)

Deliverables:

- Production WPF Tray/Desktop bound to Application contracts rather than mocks.
- Empty, loading, offline, error, stale, maintenance, unsupported, and denied states.
- Keyboard/accessibility support and status not communicated by color alone.
- PgNotifier and DB-Notifier side-by-side configuration choice during the compatibility window.

Exit: representative Windows walkthrough passes and the legacy UI can remain as rollback until integration succeeds.

### M6 — Agent/API/Dashboard integration (`STATE-06`)

Deliverables:

- Authenticated versioned Agent/API communication, outbox reconciliation, deduplication, ordering rules, heartbeat, and revocation.
- Web Dashboard reading authorized API data and displaying observed/received/stale timestamps.
- Sandbox E2E for disconnect/reconnect, duplicates, expired commands, and incompatible versions.

Exit: local monitoring survives API outage and reconciliation does not duplicate events or commands.

### M7 — Homologation and provider expansion (`STATE-07`)

Deliverables:

- PostgreSQL engine/platform/role matrix with functional, security, recovery, load, and accessibility evidence.
- One subsequent provider at a time, prioritizing MySQL/MariaDB, SQL Server, Oracle, MongoDB, SAP HANA, SQLite and other widely adopted engines, each with its own capability matrix, fixtures/environment, licensing review, and limitations.
- Open provider/plugin onboarding for additional relational, NoSQL, distributed, embedded, cloud-managed and future engines without changes to the core domain.

Exit: only proven provider/platform combinations are marked supported.

### M8 — Controlled release (`STATE-08`)

Deliverables:

- Signed packages, release notes, SBOM where applicable, externalized secrets, update channel, backup/restore, rollout and rollback runbooks.
- Explicitly authorized target and real health checks.
- Config migration report and a documented legacy uninstall/decommission choice after the rollback window.

Exit: production release Human Gate; no automatic deployment follows from this plan.

## Configuration compatibility contract

The migration tool must:

1. Accept the legacy JSON path explicitly or discover only documented PgNotifier locations.
2. Parse without executing embedded values or shell text.
3. Preserve the original file and create a timestamped backup before any target write.
4. Map `name`, `serviceName`, `hostName`, `port`, `postgresExe`, `enabled`, notification preference, retry policy, and administrative opt-in to typed target fields.
5. Add `providerType = PostgreSql` only for imported PgNotifier entries.
6. Convert passwords/connection strings, if ever encountered, into a blocked remediation item; never copy them into ordinary configuration.
7. Mark TCP fallback as degraded capability and local service control as a separate optional capability.
8. Validate duplicate identity, host, port range, timeout/retry bounds, paths, and unsupported fields.
9. Produce a sanitized dry-run report with migrated, defaulted, rejected, and manual-action items.
10. Be idempotent and support an explicit rollback that removes only generated target artifacts.

## Naming and upgrade compatibility

- New namespaces, projects, packages, executables, service names, log roots, and user-facing product text use `DBNotifier`/`DB-Notifier` from M1 onward.
- Existing `PgNotifier` paths and identifiers remain untouched while serving the rollback path.
- The first compatible release reads/imports PgNotifier configuration but writes only DB-Notifier configuration.
- Installer product codes and service identities must avoid accidental in-place replacement before the upgrade ADR defines it.
- Legacy names may remain in migration code, tests, documentation, and compatibility telemetry with an explicit deprecation reason.

## Verification strategy

Create a characterization suite before extraction with fixtures for:

- Empty, partial, invalid, duplicate, local, and remote configuration.
- `pg_isready` success, rejected connection, no response, timeout, missing executable, and special-character arguments.
- TCP success/failure with the expected degraded semantics.
- Service missing/stopped/running/PID-changed and access-denied cases using fakes; no real service mutation in ordinary tests.
- Log fallback, reload behavior, cancellation, retry boundaries, and per-instance fault isolation.
- Migration dry-run, backup, idempotency, secret rejection, and rollback.

Integration and E2E checks use disposable/sandbox databases and explicit credentials. Administrative tests require a disposable service adapter or a separately authorized environment.

## Rollback by milestone

| Change | Rollback |
|---|---|
| Scaffold/contracts | Remove only new DB-Notifier projects; legacy paths remain unchanged |
| Configuration import | Restore/select the untouched PgNotifier source; remove generated DB-Notifier config after checking the migration manifest |
| Desktop cutover | Stop DB-Notifier Desktop/Agent and relaunch PgNotifier with its original config |
| Local data schema | Restore the versioned local backup using the migration-specific procedure; do not touch monitored databases |
| Agent/API protocol | Roll Agent/API to a declared compatible version pair and replay only idempotent outbox messages |
| Package rollout | Uninstall/disable the DB-Notifier artifact and reactivate the signed prior artifact; preserve logs and audit |

Every milestone must define owner, trigger, tested target version, RTO, RPO, and evidence before release. No rollback step may alter a monitored database as a side effect.

## Main decisions deferred to ADRs

- Windows vault implementation and enterprise secret-manager integration.
- Agent provisioning, identity, rotation, and revocation mechanism.
- Exact Agent/API contract format and compatibility window.
- ORM, migration tooling, and telemetry retention/aggregation.
- Desktop/Agent signing, update channel, and installer transition.
- PostgreSQL discovery adapter and safe administrative adapter.
- Provider-specific support/licensing matrix for MySQL/MariaDB, SQL Server, Oracle, and MongoDB.

## Gate recommendation

Recommend `STATE-01 PROJECT_SETUP` after an explicit Human Gate approves:

- The incremental, non-big-bang strategy.
- The mandatory .NET 10 LTS/WPF/ASP.NET Core plus React baseline for all active projects.
- PostgreSQL as the only first provider.
- Side-by-side configuration migration and rollback.
- Deferral of functional code until its owning state.

The Human Gate remains pending until a validator records name, date, decision, reservations, and sanitized evidence.
