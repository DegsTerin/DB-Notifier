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

“All MySQL Notifier functionality” means that every behaviour publicly documented in the reference manual is accounted for below. It does not mean copying its implementation or preserving vendor coupling. DB Notifier adopts the user outcome when it is safe, adapts it to the provider-neutral Agent/API architecture when necessary, and explicitly rejects mechanisms that weaken security or contradict accepted ADRs.

| Public reference capability | DB Notifier clean-room disposition | Current evidence | Owning phase and remaining exit condition |
|---|---|---|---|
| Notification-area residency and one-click fleet menu | `ADOPT` | Normal WPF startup is hidden; one icon activation opens the compact fleet flyout and the full shell is secondary. | `STATE-05`: human acceptance in `S05-HG-011` remains pending. |
| Tray icon reflects aggregate status | `ADAPT` | Transparent database mark has Healthy/Warning/Critical/Unknown bell variants selected from the provider-neutral summary. Current selection is explicitly demonstration-only. | `STATE-06`: bind icon replacement to reconciled authorised Agent/API state. |
| Status-change and newly discovered-item notifications | `ADAPT` | Pure opt-in/change-only policy suppresses the initial snapshot; no delivery channel is active. | `STATE-06`: implement Windows delivery, deduplication, acknowledgement, quiet policy and audit evidence. |
| Separate local and remote monitoring | `ADAPT` | Canonical topology and outbound Agent architecture represent both without direct Dashboard/database access. | `STATE-06`: integrate authorised state; `STATE-07`: homologate each provider/version/topology independently. |
| Automatic local-service discovery and removal | `ADAPT` | Typed PostgreSQL discovery exists; blind name-filter auto-add is prohibited. | `STATE-06`: reconcile typed candidates and require explicit policy/approval before catalogue mutation. |
| Add, edit and remove monitored services/instances | `ADAPT` | Provider-neutral catalogue and configuration contracts exist; STATE-05 surfaces are read-only fixtures. | `STATE-06`: authorised catalogue workflows, validation, audit and conflict handling. |
| Per-item participation in notifications and aggregate icon | `ADAPT` | Aggregate and notification policies are independent of provider identity. | `STATE-06`: persist validated preferences and apply them to authorised state. |
| Configurable status interval and manual refresh | `ADOPT` | Timing/reconciliation contracts are documented; current UI refreshes only demonstration time/freshness. | `STATE-06`: bounded non-overlapping polling, immediate manual re-read and freshness/error evidence. |
| Start, Stop and Restart | `ADAPT` | Commands are explicitly `Unsupported`; RBAC, expiry, idempotency and audit contracts exist. | `STATE-07`: implement and homologate each exact provider/topology capability with confirmation, reason and post-probe. |
| Connection creation and connection test | `ADAPT` | Typed non-secret provider configuration and opaque credential references are architectural contracts. | `STATE-07`: implement provider-owned forms/tests against disposable targets without exposing secrets. |
| TCP and verified TLS connection modes | `ADOPT` | Provider SDK and transport architecture allow provider-owned secure connection modes. | `STATE-07`: implement and homologate each provider-specific mode; insecure downgrade remains denied by default. |
| SSH tunnel creation and credential fallback | `REJECT/REPLACE` | DB Notifier never opens an unapproved tunnel or stores password/key material. | Use an approved pre-existing network path or deploy an outbound Agent near the target; any future managed tunnel needs a separate accepted security ADR. |
| Direct remote WMI/DCOM callbacks and firewall mutation | `REJECT/REPLACE` | No reverse callback, WMI/DCOM dependency or firewall mutation is present. | Replaced permanently by outbound authenticated Agent/API communication. |
| Manage Instance, SQL Editor, Installer and Utilities shortcuts | `ADAPT` | Flyout opens DB Notifier Overview, Configuration and logs; arbitrary vendor executables are not launched. | `STATE-07`: optional provider-owned, allow-listed external-tool descriptors may be proposed with provenance and safe arguments. |
| MySQL Workbench connection import/migration | `REJECT/REPLACE` | No vendor configuration coupling exists; ConfigMigrator handles only the owned PgNotifier legacy contract with sanitised reports. | A future provider import requires a separate versioned, secret-safe migrator and Human Gate. |
| General, notification and connection options with defaults | `ADAPT` | Language/theme persistence exists; monitoring and notification settings are not yet active. | `STATE-06`: versioned operational preferences and safe reset; provider connection options remain provider-owned in `STATE-07`. |
| Colourful-status-icon preference | `ADAPT` | Semantic bell colours are mandatory and paired with text/shape; colour is never the only status evidence. | No unsafe monochrome ambiguity is introduced; accessibility confirmation remains in the Human Gate. |
| Launch at Windows startup | `ADOPT` | Not registered in STATE-05. | `STATE-08`: explicit opt-in, reversible signed-installer registration and uninstall cleanup. |
| Check for product updates | `ADAPT` | No unsigned or vendor-installer update check is active. | `STATE-08`: signed update channel, SBOM, rollout, rollback and release evidence. |
| About, troubleshooting, logs and explicit exit | `ADOPT/ADAPT` | Logs navigation and explicit Exit exist; diagnostics remain sanitised. | Complete product/version/about and packaged support evidence in `STATE-08`; operational log integration begins in `STATE-06`. |

This matrix is the exhaustive clean-room coverage baseline for the reference manual's user-visible capabilities. A row marked future is an accepted requirement, not a claim that the function already works. A rejected mechanism cannot be reintroduced merely to obtain visual or behavioural similarity.

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
- Empty, loading, offline, error, stale, maintenance, unsupported, and denied states.
- Keyboard/accessibility support and status not communicated by color alone.
- PgNotifier and DB-Notifier side-by-side configuration choice during the compatibility window.

Exit: representative Windows walkthrough passes and the legacy UI can remain as rollback until integration succeeds.

### M6 — Agent/API/Dashboard integration (`STATE-06`)

Deliverables:

- Authenticated versioned Agent/API communication, outbox reconciliation, deduplication, ordering rules, heartbeat, and revocation.
- Web Dashboard reading authorized API data and displaying observed/received/stale timestamps.
- WPF Tray reading authorised API/Agent presentation state, emitting notifications only for canonical changes and opening integrated history/log destinations without exposing provider-native secrets.
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
