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

## MySQL Notifier conceptual benchmark

### Clean-room product decision

On 2026-07-14, the product owner explicitly selected option 1: a clean implementation inspired functionally by MySQL Notifier while preserving DB Notifier's MIT licence and independent identity. This decision is a provenance and implementation constraint, not permission to copy or derive from the reference repository. Public behavioural documentation may be converted into provider-neutral DB Notifier requirements; Oracle/MySQL source, binaries, artwork, logos, trade dress, product copy and vendor-specific architecture must not enter this repository. Every implementation claim must be supported by DB Notifier-owned code, tests and evidence.

The product owner clarified on 2026-07-14 that the Windows WPF client is primarily a Windows notification-area notifier. The benchmark is the interaction model documented by Oracle for MySQL Notifier 1.1, not its source code, artwork, vendor identity or security architecture. Oracle's archived [MySQL Notifier manual](https://downloads.mysql.com/docs/mysql-notifier-en.pdf) and [release notes](https://downloads.mysql.com/docs/mysql-notifier-relnotes-en.pdf) establish the following reference behaviours:

- the application resides in the Microsoft Windows taskbar notification area and may start with Windows;
- activating its icon opens the main status menu, with each monitored server and its current state;
- the icon reflects aggregate monitored status and status changes can produce Windows notifications;
- monitored services/instances can be managed separately, refreshed manually and checked on a configurable interval;
- larger management tools are secondary destinations opened from the notifier;
- the final documented series is under Oracle Lifetime Sustaining Support, so it is a historical benchmark rather than a current dependency.

DB Notifier adopts the tray-first hierarchy, rapid fleet scan, aggregate-state concept, change-only notification intent and secondary-management navigation. It deliberately replaces MySQL-specific service assumptions with provider-neutral Application contracts, canonical health states, explicit freshness and Agent/API ownership. Normal WPF startup remains in the notification area; the full WPF shell is a secondary local drill-down, while the Web Dashboard owns the complete responsive fleet experience.

DB Notifier does not adopt automatic service addition by name, direct remote WMI/DCOM callbacks, automatic firewall changes, vendor connection-file coupling or unconditional Start/Stop/Restart. Discovery must be typed and authorised. Remote monitoring follows the accepted outbound Agent/API architecture. Administrative actions remain unavailable until capability, identity, permission, confirmation, replay protection, audit, post-probe and exact homologation are proved.

Starting automatically at Windows sign-in is not implemented by this `STATE-05` increment. It requires an explicit user preference and the signed installer/startup-registration contract owned by `STATE-08`; normal process startup being tray-first must not be confused with automatic operating-system registration.

### Complete public-function coverage matrix

“All MySQL Notifier functionality” means that every distinct user outcome publicly documented in Oracle's [MySQL Notifier 1.1 reference manual](https://downloads.mysql.com/docs/mysql-notifier-en.pdf) and [release notes through 1.1.8](https://downloads.mysql.com/docs/mysql-notifier-relnotes-en.pdf) has a stable record below. The baseline was re-audited against both publications on 2026-07-16. Release-note bug entries are not copied as a vendor defect catalogue; where they establish a durable user outcome, that outcome is captured by an `MN-Q*` quality record.

Disposition and maturity are separate facts. `ADOPT` preserves a safe portable outcome, `ADAPT` provides the outcome through DB-Notifier's provider-neutral and least-privilege architecture, and `REJECT/REPLACE` permanently refuses the documented mechanism while naming the supported alternative. A future owning phase is a scheduled requirement, never an implementation, support or homologation claim.

| ID | Publicly documented outcome and source | DB-Notifier clean-room disposition | Current evidence | Owning phase and remaining exit condition |
|---|---|---|---|---|
| `MN-001` | Windows notification-area residency and activation of a compact status menu (manual 1.1 §§1.1-1.2) | `ADOPT`: the notification-area-first Windows hierarchy is the product behaviour. | Normal WPF startup is hidden; one icon activation opens the compact fleet flyout and the full shell remains secondary. | `S05-HG-011` was approved on 2026-07-15; the complete `STATE-05` Human Gate remains pending. |
| `MN-002` | Per-item status in the main menu, contextual actions and an empty-inventory state (manual 1.1 §1.1) | `ADAPT`: show provider-neutral state, freshness and only applicable actions. Safe navigation, Settings and Exit remain discoverable when the fleet is empty rather than reproducing the reference menu-hiding rule. | The flyout presents four labelled demonstration instances and safe local destinations. | `STATE-06`: bind the list and action applicability to authorised reconciled state, including empty/loading/offline/error/stale conditions. |
| `MN-003` | Aggregate Tray status and per-item participation in that aggregate (manual 1.1 §§1.1-1.3) | `ADAPT`: canonical Healthy/Warning/Critical/Unknown precedence replaces the binary vendor model; stale evidence is Unknown. | The transparent canonical mark has semantic bell variants selected from a provider-neutral demonstration summary; text remains the non-colour evidence. | `STATE-06`: bind aggregation and persisted inclusion policy to reconciled Agent/API observations. |
| `MN-004` | Opt-in notifications for newly discovered items and factual status changes (manual 1.1 §§1.1-1.2) | `ADAPT`: suppress initial snapshots, classify canonical transitions and deliver each notification according to its own factual meaning. | The semantic selector and modern/fallback Windows publisher exist only for local close-to-Tray availability confirmation; this is not a state-change channel. | `STATE-06`: implement serialised delivery, deduplication, acknowledgement, quiet policy, audit and failure evidence against authorised state. |
| `MN-005` | Monitoring of local and remote database instances (manual 1.1 §§1.1, 2) | `ADAPT`: an outbound Agent near the target replaces direct Dashboard access and vendor-specific reverse callbacks. | Canonical topology and Agent/API contracts represent local, remote, datacentre, hybrid and cloud targets. | `STATE-06`: integrate authorised state; `STATE-07`: homologate each provider/version/platform/topology independently. |
| `MN-006` | Automatic discovery, addition and removal of recognised services, including configurable filtering (manual 1.1 §§1.1-1.3; release notes 1.1.2 and 1.1.6) | `ADAPT`: provider-owned typed discovery and explicit catalogue policy replace substring matching and blind auto-add. Removal is reconciliation, never destructive service control. | Typed PostgreSQL discovery and negative fixtures exist; no automatic catalogue mutation is active. | `STATE-06`: reconcile candidates with approval/policy and audit; `STATE-07`: homologate discovery per provider/platform. |
| `MN-007` | Add, edit, remove, list and filter monitored services/instances (manual 1.1 §1.3; release notes 1.1.8) | `ADAPT`: one provider-neutral catalogue workflow validates typed endpoints and exposes only authorised candidates. Arbitrary Windows services are not accepted merely by name. | Catalogue/API contracts exist; `STATE-05` surfaces use read-only fixtures. | `STATE-06`: implement authorised CRUD, filter, validation, conflict handling and audit without direct UI-to-database access. |
| `MN-008` | Per-item notification, aggregate-icon and monitoring-interval preferences (manual 1.1 §1.3) | `ADAPT`: store independent, versioned policies with bounded durations and safe defaults. | Aggregate and notification policies are provider-neutral; operational preferences are not persisted or active. | `STATE-06`: implement validation, persistence, reset/migration and application to authorised state. |
| `MN-009` | Configurable periodic status checks and immediate manual refresh (manual 1.1 §§1.2-1.3; release notes 1.1.4 and 1.1.7) | `ADOPT`: use bounded, cancellable, non-overlapping polling plus an explicit authoritative re-read. | Timing/reconciliation contracts are documented; current UI refresh changes only demonstration time/freshness. | `STATE-06`: prove cadence, cancellation, no overlapping requests and preserved timestamps/error/stale truth. |
| `MN-010` | Start, Stop and Restart from the item menu (manual 1.1 §1.1) | `ADAPT`: each operation is a distinct capability requiring separate identity, exact RBAC, reason, confirmation, replay protection, idempotency, audit and post-probe. | All three capabilities are explicitly `Unsupported`; command and authorisation contracts exist, but no executor or attempt exists. | `STATE-07`: implement and homologate each exact provider/topology/operation before enabling any UI action. |
| `MN-011` | Create, edit and test a connection with unique-name and required-field validation (manual 1.1 §1.3; release notes 1.1.5 and 1.1.7) | `ADAPT`: provider-owned typed forms and tests use non-secret configuration plus opaque credential references. | Provider endpoint validation and PostgreSQL fake tests exist; no integrated editor or MySQL provider exists. | `STATE-07`: implement provider-owned forms/tests against disposable targets, with accessible validation and no secret disclosure. |
| `MN-012` | Standard TCP and encrypted connection modes with explicit TLS verification choices (manual 1.1 §1.3) | `ADAPT`: provider capabilities may expose secure transport modes; verified identity is preferred and insecure downgrade is denied by default. Certificates and keys remain vault references. | Provider SDK can declare transport prerequisites; only the unhomologated PostgreSQL slice has an authenticated driver adapter. | `STATE-07`: implement and homologate exact modes for each provider/version; transport reachability alone never becomes Healthy. |
| `MN-013` | Strong-password authentication for MySQL 8.0 `caching_sha2_password` (release notes 1.1.8) | `ADOPT`: the future MySQL provider must use a maintained driver and advertise only proved authentication modes. | No MySQL provider or compatibility evidence exists. | `STATE-07`: implement provider-owned fixtures and disposable-target evidence for exact MySQL versions and TLS/authentication combinations. |
| `MN-014` | Creation of SSH-tunnel connections with key/password fallback (manual 1.1 §1.3; release notes 1.1.8) | `REJECT/REPLACE`: DB-Notifier does not create unapproved tunnels, persist key/password material or silently fall back between credentials. | No tunnel builder or secret-bearing connection document exists. | Use an approved pre-existing network path or an outbound Agent near the target. Any managed tunnel requires a separate accepted security ADR and Human Gate. |
| `MN-015` | Direct remote Windows service monitoring through WMI/DCOM callbacks and firewall/permission configuration (manual 1.1 §2; release notes 1.1.0) | `REJECT/REPLACE`: reverse callbacks, anonymous DCOM access and firewall mutation are permanently replaced by authenticated outbound Agent/API communication. | No WMI/DCOM dependency, reverse callback or firewall mutation exists. | Permanent architectural replacement; a remote administrative adapter still requires exact `STATE-07` homologation. |
| `MN-016` | Context-sensitive shortcuts to instance management, SQL editing, installer and utilities (manual 1.1 §§1.1-1.2; release notes 1.1.5-1.1.8) | `ADAPT`: prefer owned Overview, Configuration, history and logs. Optional external tools require provider-owned allow-listed descriptors, provenance, safe arguments and capability/topology predicates. | The flyout opens owned DB-Notifier destinations; arbitrary vendor executables are not launched. | `STATE-07`: propose and test optional descriptors; absence or incompatibility must remain visibly unavailable. |
| `MN-017` | Import/migration of MySQL Workbench connections, deferral, and dynamic reaction to Workbench install/uninstall or connection-file changes (manual 1.1 §§1.2-1.3; release notes 1.1.5-1.1.7) | `REJECT/REPLACE`: there is no direct vendor-file coupling. A future provider import must be explicit, versioned, deduplicating, secret-safe, reversible and independently consented to. | ConfigMigrator handles only the owned PgNotifier legacy contract with dry-run, backup, atomic writes and sanitised reports. | A MySQL/Workbench importer requires a separate authorised increment and Human Gate; ordinary monitoring does not watch vendor files. |
| `MN-018` | General, notification and connection options with Apply, Cancel and Reset-to-default semantics (manual 1.1 §1.2; release notes 1.1.8) | `ADAPT`: staged edits commit atomically only after validation; Cancel has no side effect and reset uses versioned safe defaults. | Language/theme persistence is implemented; operational and provider connection settings are not active. | `STATE-06`: operational preferences and conflict handling; `STATE-07`: provider-owned connection options; tests must cover commit/cancel/reset and migration. |
| `MN-019` | Optional colourful status icons (manual 1.1 §1.2; release notes 1.0.3 and 1.1.3) | `ADAPT`: semantic variants are mandatory and always paired with text/shape; DB-Notifier does not offer an ambiguous colour-only or unlabelled mode. | One canonical accessible Healthy/Warning/Critical/Unknown family is generated for Web/WPF/Tray. | Human accessibility confirmation remains part of `STATE-05`; factual live selection belongs to `STATE-06`. |
| `MN-020` | Launch at Windows sign-in with a persisted preference (manual 1.1 §§1.1-1.2; release notes 1.1.4 and 1.1.8) | `ADOPT`: explicit opt-in, reversible registration and uninstall cleanup are required; notification-area-first process startup is a separate fact. | Not registered in `STATE-05`. | `STATE-08`: signed-installer-owned registration, stable executable path, rollback and cleanup evidence. |
| `MN-021` | Manual/periodic product update checks and notification of an available update (manual 1.1 §§1.1-1.2; release notes 1.1.6 and 1.1.8) | `ADAPT`: use only a signed DB-Notifier update channel with an explicit policy; never invoke MySQL Installer. | No update check or update notification is active. | `STATE-08`: signed metadata/package validation, interval policy, SBOM, rollout, rollback and observable failure evidence. |
| `MN-022` | About, troubleshooting, sanitised logs and explicit application exit (manual 1.1 §§1.1-1.4; release notes 1.0.3) | `ADOPT/ADAPT`: retain owned product/version/support information, bounded diagnostics without stack traces or secrets, and an unambiguous Exit. | Logs navigation and explicit Exit exist; full operational diagnostics and packaged About/support data do not. | Operational log integration begins in `STATE-06`; packaged product/support evidence completes in `STATE-08`. |
| `MN-023` | Install, upgrade and uninstall without corrupting configuration or leaving an orphan process (manual overview; release notes 1.0.3, 1.1.4 and 1.1.5) | `ADAPT`: signed, side-by-side, migration-aware packaging preserves owned configuration, rollback and audit. | The PgNotifier ConfigMigrator is safe in its tested scope; target installer/update/uninstall flows are not proved. | `STATE-08`: package, migration, running-process hand-off, uninstall cleanup, rollback and recovery evidence. |
| `MN-024` | Monitor other recognised MySQL Windows services such as MySQL Router (release notes 1.1.8) | `ADAPT`: each provider may declare typed discoverable service kinds; a name containing an engine string is not proof. | Only typed PostgreSQL discovery exists and has no homologation. | `STATE-07`: provider-owned discovery/health fixtures and exact service-kind homologation, including a future MySQL provider. |
| `MN-025` | Distinguish supported, managed and unknown connection/driver types (release notes 1.1.6) | `ADAPT`: provider and capability registration is open; unknown or incompatible types return `Unsupported`, `Unavailable` or `Unknown` rather than being guessed or silently executed. | Canonical capability states and provider package verification exist; no MySQL Fabric or equivalent provider exists. | `STATE-06`: integrated presentation; `STATE-07`: provider-specific mapping and compatibility evidence. |

The release notes also establish durable quality outcomes that cut across the capabilities above:

| ID | Publicly documented quality outcome | DB-Notifier clean-room disposition | Current evidence | Owning phase and remaining exit condition |
|---|---|---|---|---|
| `MN-Q01` | External additions, removals and status changes reconcile without stale entries, duplicates or restart-only visibility (release notes 1.0.3-1.1.6) | `ADAPT`: reconciliation is idempotent, ordered and source-aware; disappearance becomes an explicit state/event and never deletes history silently. | Outbox, sequence, idempotency and canonical transition contracts exist; UIs still consume fixtures. | `STATE-06`: E2E add/change/remove/duplicate/reorder/disconnect scenarios with preserved freshness and history. |
| `MN-Q02` | Missing, dead or corrupt service/configuration inputs do not crash the notifier (release notes 1.1.4-1.1.7) | `ADAPT`: validate at trust boundaries, quarantine invalid input, retain last-known factual state and expose a sanitised remediation path. Deleting configuration is not the default recovery mechanism. | ConfigMigrator rejects invalid/secret-bearing input and writes atomically; provider/runtime external-file recovery is not integrated. | `STATE-06`: sandbox recovery/error-state E2E; `STATE-07`: provider/platform negative fixtures; `STATE-08`: packaged configuration recovery. |
| `MN-Q03` | Cancelled dialogs or denied external launches perform no action; runtime failures are logged without exposing raw stack traces (release notes 1.0.3, 1.1.5 and 1.1.8) | `ADOPT/ADAPT`: cancellation and refusal are side-effect free; failures are typed, bounded, sanitised and auditable. | Fail-closed authorisation, unsupported-action previews and sanitised migration reports exist; integrated settings/tool-launch flows do not. | Prove negative paths in each owning phase before enabling the corresponding function. |
| `MN-Q04` | Persisted options and connection changes survive restart/upgrade without corruption, while incompatible legacy data is migrated explicitly (release notes 1.1.4, 1.1.7 and 1.1.8) | `ADAPT`: versioned schemas, atomic writes, backup, validation, idempotent migration and rollback replace ad hoc vendor XML edits. | UI preferences and PgNotifier configuration migration are versioned/tested in their current scopes. | `STATE-06`: operational settings; `STATE-07`: provider settings; `STATE-08`: upgrade/rollback and uninstall evidence. |

These 29 records are the exhaustive clean-room coverage baseline for the public manual and feature-bearing release-note outcomes reviewed above. A future row is an accepted requirement only. A rejected mechanism cannot be reintroduced merely to obtain visual or behavioural similarity, and a capability cannot be marked supported until its exact provider/version/platform/topology combination is implemented and homologated.

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
- Provider-neutral notification-area flyout with factual fleet summary, non-colour-only states, local navigation and explicit unavailability for administrative operations not yet integrated or homologated.
- Notification-area-first startup; the compact flyout is the primary Windows surface and the full WPF shell is a secondary drill-down destination.
- Local Windows `AppNotificationManager` confirmation for every explicit `CloseRequest`, with an application-owned transparent identity asset and a fail-safe legacy notification-area fallback when the Windows App Runtime or modern notification path is unavailable. This confirmation proves neither visible delivery nor provider support.
- Empty, loading, offline, error, stale, maintenance, unsupported, and denied states.
- Keyboard/accessibility support and status not communicated by color alone.
- PgNotifier and DB-Notifier side-by-side configuration choice during the compatibility window.

Exit: representative Windows walkthrough passes and the legacy UI can remain as rollback until integration succeeds.

### M6 — Agent/API/Dashboard integration (`STATE-06`)

Deliverables:

- Authenticated versioned Agent/API communication, outbox reconciliation, deduplication, ordering rules, heartbeat, and revocation.
- Web Dashboard reading authorized API data and displaying observed/received/stale timestamps.
- WPF Tray reading authorised API/Agent presentation state, emitting opt-in notifications only for factual canonical changes, suppressing initial snapshots, deduplicating repeated evidence and opening integrated history/log destinations without exposing provider-native secrets.
- TV mode performing an immediate authorised API read on entry and a non-overlapping authoritative refresh every 30 seconds while active; authenticated SignalR hints may request an earlier read but never replace periodic reconciliation.
- Sandbox E2E for disconnect/reconnect, duplicates, expired commands, and incompatible versions.

Exit: local monitoring survives API outage and reconciliation does not duplicate events or commands.

### M7 — Homologation and provider expansion (`STATE-07`)

Deliverables:

- PostgreSQL engine/platform/role matrix with functional, security, recovery, load, and accessibility evidence.
- Tray administrative actions enabled only for each exact homologated service-control capability after privilege, confirmation, idempotency, audit and post-action probe evidence passes.
- One subsequent provider at a time, prioritizing MySQL/MariaDB, SQL Server, Oracle, MongoDB, SAP HANA, SQLite and other widely adopted engines, each with its own capability matrix, fixtures/environment, licensing review, and limitations.
- Open provider/plugin onboarding for additional relational, NoSQL, distributed, embedded, cloud-managed and future engines without changes to the core domain.

Exit: only proven provider/platform combinations are marked supported.

### M8 — Controlled release (`STATE-08`)

Deliverables:

- Signed packages, release notes, SBOM where applicable, externalized secrets, update channel, backup/restore, rollout and rollback runbooks.
- Installer detection and, where required, installation of the architecture-matched Windows App Runtime 2.2 dependency; a stable signed Desktop path for unpackaged notification identity; rollback that restores the compatible runtime/application pair; and uninstall cleanup for DB Notifier-owned notification registration without touching unrelated applications.
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
