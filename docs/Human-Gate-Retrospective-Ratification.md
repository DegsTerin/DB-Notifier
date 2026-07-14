# Retrospective Human Gate Ratification

## Status

`CONCLUÍDA — STATE-00 A STATE-04 RATIFICADOS`

This document owns the retrospective ratification of the contested Human Gate records for `STATE-00` through `STATE-04`. It is an addendum: the original reports and transition log remain historical evidence and are not rewritten.

The technical workspace remains positioned at `STATE-05 FRONTEND_IMPLEMENTATION`, but lifecycle progression is on hold. No entry below may be marked approved from an automated result, a bundled answer, a general request to continue or an agent inference.

## Reason for ratification

On 2026-07-13, validator Bruno stated that he was not certain the previous Human Gates had been approved correctly and that he had only written “approved”. He then agreed to correct the resulting governance divergence and perform complete retrospective ratification.

The audit found:

- `STATE-00` records “Aprovado” while also recording that repeated critical samples were not declared.
- `STATE-01` records “Sim” while the clean onboarding sample was not independently repeated.
- `STATE-02` interprets one “Sim” as acceptance of six ADRs, gate closure and transition.
- `STATE-03` states an approval without preserving an explicit validator response or detailed observed sample.
- `STATE-04` states that deterministic samples were reviewed, which the validator can no longer confirm as an informed Human Gate.

These findings contest the sufficiency of the approval evidence; they do not erase implementation, automatic test evidence or the original conversation history.

## Ratification rules

1. Present and decide one state at a time.
2. Review the named automatic report and the material limitations below.
3. Repeat the human sample where it remains meaningful and safe, or record it as not repeated with a reason.
4. Choose `APROVADO`, `APROVADO COM RESSALVAS` or `REPROVADO` for that state only.
5. Record validator, date, justification, accepted reservations and the exact confirmation sentence.
6. A later state cannot be ratified as authority for an earlier state; ordering remains `STATE-00` → `STATE-04`.
7. Ratification does not authorise a database/service mutation, production migration, deployment, provider support claim or later-state implementation.

## Current automatic revalidation baseline

Observed locally on 2026-07-13 from baseline commit `70830b6` plus the scoped governance/CI remediation worktree:

| Check | Current result |
|---|---|
| Git tracked scope | Worktree inspected before edits; unrelated user changes absent |
| Git object/ref integrity | Invalid long Codex checkpoint ref backed up outside the repository and removed; `git show-ref` and `git fsck --full` exit `0` |
| .NET | SDK `10.0.301`; locked restore; Release build with 0 warnings/errors; 128 unit/model/provider/presentation + 5 architecture tests |
| Static formatting | `dotnet format --verify-no-changes --no-restore` approved before this documentation increment |
| Dashboard | clean `npm ci`; 23 tests; typecheck/build; brand/token/localisation drift gates; 0 npm vulnerabilities |
| Dependencies | no NuGet direct/transitive vulnerabilities reported |
| Legacy | 10/10 Pester and validation-only canonical bundle approved |
| Web runtime matrix | 44 current locale/theme/viewport samples; no global overflow or unnamed interactive control; modal and TV samples approved |
| Fail-closed runtime | API liveness `200`; unauthenticated human route `401`; Agent route without certificate `403`; disabled Agent remained alive without enabling monitoring |
| External systems | Not contacted: database, IdP, certificate enrollment, vault, notification channel, remote network, service control, installer or deployment |

The remediation baseline was re-run after the CI/governance changes. The current formal automatic `STATE-05` result is recorded in [`STATE-05-Frontend-Implementation-Final-Reaudit.md`](STATE-05-Frontend-Implementation-Final-Reaudit.md); it does not decide any ratification entry below.

## STATE-00 DISCOVERY_MIGRATION

### Evidence to review

- [`STATE-00-Discovery-Report.md`](STATE-00-Discovery-Report.md)
- [`Legacy-Inventory.md`](Legacy-Inventory.md)
- [`Legacy-Migration-Plan.md`](Legacy-Migration-Plan.md)

### Material facts and limitations

- The legacy PowerShell PostgreSQL monitor was inventoried without a real database or service action.
- The original workspace was not a Git repository and the referenced build script was absent.
- The legacy suite passed 8 tests at discovery; later compatibility work now passes 10 tests and bundle validation.
- The migration plan is incremental, keeps rollback paths and does not claim provider homologation.
- Original critical samples were not declared as repeated by the validator.

### Human review sample

- Confirm the inventory distinguishes functional legacy code from WPF/Python prototypes.
- Confirm TCP-only evidence must be degraded, not healthy.
- Confirm service control and real infrastructure were not authorised by discovery.
- Confirm the proposed migration/rollback strategy was acceptable for entering setup.

### Ratification record

- Validator/date: `Bruno — 2026-07-13`
- Samples reviewed now (document review; no runtime execution): inventory separation between functional legacy code and WPF/Python prototypes; degraded meaning of TCP-only evidence; absence of service control or real-infrastructure action during discovery; incremental migration, rollback preservation and no provider-homologation claim.
- Reservations accepted: approval is limited to discovery and planning. It does not validate real-environment operation and does not replace later integration, infrastructure, provider or operational testing.
- Decision: `APROVADO`
- Justification/evidence: the validator explicitly reviewed all four named samples, accepted the discovery/migration direction and retained the stated scope limitations.
- Confirmation received: `Ratifico a decisão acima exclusivamente para STATE-00 DISCOVERY_MIGRATION.`

## STATE-01 PROJECT_SETUP

### Evidence to review

- [`STATE-01-Setup-Report.md`](STATE-01-Setup-Report.md)
- [`Development.md`](Development.md)
- [`Legacy-Compatibility.md`](Legacy-Compatibility.md)

### Material facts and limitations

- Git, solution boundaries, central build settings, CI, safe configuration and compatibility shims were introduced.
- The historical report began on .NET 8 and was superseded by the mandatory .NET 10 baseline in `STATE-02`; all active targets are now .NET 10.
- Clean restore/build/tests, Dashboard build and legacy compatibility had automatic evidence.
- Remote CI was defined but not proven by an external run.
- The validator did not independently repeat the clean onboarding sample recorded by the report.

### Human review sample

- Review the repository layout and onboarding commands.
- Confirm active .NET projects use .NET 10 and legacy entry points delegate without overwriting legacy configuration.
- Confirm setup introduced no provider/domain behaviour prematurely and no deployment.

### Ratification record

- Validator/date: `Bruno — 2026-07-13`
- Samples reviewed now (document review): solution structure and project boundaries; current .NET 10 targets; legacy delegation without overwriting legacy configuration; absence of premature provider behaviour or deployment; remote CI remains unproved.
- Onboarding sample: `NÃO REPETIDA` — the validator did not execute restore, build, tests or validation commands and therefore did not independently confirm the report results.
- Reservations accepted: ratification relies exclusively on the documentation and presented evidence; onboarding was not repeated by the validator; remote CI execution remains without independent proof.
- Decision: `APROVADO COM RESSALVAS`
- Justification/evidence: the validator explicitly accepted the setup structure and boundaries while retaining all three evidence limitations above.
- Confirmation received: `Ratifico a decisão acima exclusivamente para STATE-01 PROJECT_SETUP.`

## STATE-02 ARCHITECTURE

### Evidence to review

- [`STATE-02-Architecture-Report.md`](STATE-02-Architecture-Report.md)
- [`architecture/README.md`](architecture/README.md)
- ADR-0001 through ADR-0006, canonical contracts, protocol, threat model, provider matrix and AIOps guardrails under [`architecture/`](architecture/)

### Material facts and limitations

- .NET 10, WPF/React, provider-neutral dependencies, outbound mTLS Agent/API, SQLite/PostgreSQL ownership and versioned contracts were accepted architecturally.
- Monitoring and administration use separate identities/capabilities; commands require RBAC, expiry, idempotency, audit and post-probe.
- WiX or an equivalent reproducible signed MSI direction was accepted, but the exact toolchain/signing service remains unimplemented.
- Threat, hybrid/offline, replay, update and AIOps scenarios were architecture walkthroughs, not penetration tests or operational proof.
- A single “Sim” was historically interpreted as acceptance of all six ADRs and transition.

### Human review sample

- Decide whether each ADR-0001 through ADR-0006 remains acceptable; record any exception separately.
- Walk through standalone/offline, hybrid outbound-only, credential separation, command replay/timeout and update rollback.
- Confirm AIOps remains roadmap-only and initially `OBSERVER` without executor access.

### Ratification record

- Validator/date: `Bruno — 2026-07-13`
- ADR decisions:
  - `ADR-0001`: `ACEITO` — .NET 10 LTS, incremental migration and legacy-reference preservation remain appropriate.
  - `ADR-0002`: `ACEITO` — secret references, separate identities, vaults and mTLS remain appropriate; real provisioning, complete rotation and operational validation remain future work.
  - `ADR-0003`: `ACEITO` — outbound HTTPS/mTLS, versioned contracts, idempotency, durable queues and offline/replay handling remain appropriate; operational validation remains future integration work.
  - `ADR-0004`: `ACEITO` — Agent SQLite/central PostgreSQL separation, controlled migrations, explicit retention and ownership isolation remain appropriate; retention remains architectural guidance until operational validation.
  - `ADR-0005`: `ACEITO COM RESSALVAS` — reproducible MSI, signing and secure update direction accepted; WiX/equivalent, signing service and operational release flow remain unimplemented.
  - `ADR-0006`: `ACEITO` — declared capabilities, monitoring/administration separation, RBAC, audit and post-probe remain appropriate; no provider or administrative command is homologated by this decision.
- Samples reviewed now (architectural/document walkthrough): standalone/offline, outbound-only hybrid, credential separation, replay/timeout with `UnknownOutcome`, update/rollback and AIOps initially restricted to `OBSERVER` without executor access.
- Samples not repeated: no operational execution, penetration test, provider homologation, real update test or infrastructure validation was performed.
- Reservations accepted: no operational provider homologation; ADR-0005 release pipeline remains future work; ADR-0002 provisioning/rotation/mTLS require future validation; ADR-0006 does not authorise real administrative control without capability/provider-specific homologation.
- Decision: `APROVADO COM RESSALVAS`
- Justification/evidence: the validator decided all six ADRs individually, completed the documented scenario walkthrough and explicitly retained the implementation and operational-proof limits.
- Confirmation received: `Ratifico a decisão acima exclusivamente para STATE-02 ARCHITECTURE.`

## STATE-03 DATABASE_MODELING

### Evidence to review

- [`STATE-03-Database-Modeling-Report.md`](STATE-03-Database-Modeling-Report.md)
- [`data/README.md`](data/README.md)
- [`data/Logical-Model.md`](data/Logical-Model.md)
- [`data/Migration-Runbook.md`](data/Migration-Runbook.md)
- [`data/Retention-And-Deletion.md`](data/Retention-And-Deletion.md)

### Material facts and limitations

- Agent SQLite and Server PostgreSQL models are isolated and use opaque credential references.
- Constraints, idempotency, concurrency, retention selectors and audit immutability are modelled and tested.
- SQLite migrations/rollback were exercised ephemerally; PostgreSQL migration scripts were generated/reviewed offline.
- No PostgreSQL migration, backup/PITR recovery, legal-hold integration or production deletion was executed.
- The historical report states approval but does not preserve a detailed explicit validator response.

### Human review sample

- Review storage ownership and confirm monitored databases are never DB-Notifier persistence targets.
- Inspect representative Up/Down operations, audit trigger and rollback rules.
- Confirm retention values are design defaults and do not authorise production deletion.

### Ratification record

- Validator/date: `Bruno — 2026-07-13`
- Samples reviewed now:
  - Storage ownership: `APROVADO` — Agent SQLite and central Server/API PostgreSQL remain isolated; monitored databases are not internal persistence; credentials remain opaque vault references.
  - Model and invariants: `APROVADO` — application IDs, UTC timestamps, concurrency tokens, idempotency indexes, separate inbox/outbox/checkpoints, database-level audit protection and non-secret persisted configuration were reviewed.
  - Migration `Up`/`Down`: `APROVADO COM RESSALVAS` — reviewed migrations create only DB-Notifier structures and offer coherent non-production reversal; no real PostgreSQL execution occurred and production rollback cannot rely on `Down` alone.
  - Audit trigger: `APROVADO` — PostgreSQL initial migration models append-only protection against `UPDATE`/`DELETE`, with ordered trigger/function removal during `Down`.
  - Retention and rollback: `APROVADO COM RESSALVAS` — retention remains a design default; schema application does not enable deletion; bounded workers remain disabled/dry-run by default; legal hold, real backup/PITR and production deletion remain unvalidated.
- Samples not repeated: real PostgreSQL; production migration; real backup/PITR; complete operational restore; legal-hold integration; production deletion; complete real-environment rollback.
- Reservations accepted: approval is limited to the architectural model, reviewed migrations and presented non-production tests; production data operation requires future release, backup, authorisation and recovery gates; PostgreSQL migrations were reviewed offline without disposable-instance homologation; retention/deletion is designed capability, not operational authorisation.
- Decision: `APROVADO COM RESSALVAS`
- Justification/evidence: the validator reviewed all named ownership, invariant, migration, audit and retention samples and explicitly retained every unexecuted production/recovery limitation.
- Confirmation received: `Ratifico a decisão acima exclusivamente para STATE-03 DATABASE_MODELING.`

## STATE-04 BACKEND_IMPLEMENTATION

### Evidence to review

- [`STATE-04-Backend-Implementation-Report.md`](STATE-04-Backend-Implementation-Report.md)
- [`STATE-04-Backend-Implementation-Audit.md`](STATE-04-Backend-Implementation-Audit.md)
- [`STATE-04-Backend-Implementation-Reaudit.md`](STATE-04-Backend-Implementation-Reaudit.md)
- [`architecture/Provider-Capability-Matrix.md`](architecture/Provider-Capability-Matrix.md)

### Material facts and limitations

- The first automatic closure audit failed because the migrator, discovery fixtures and capability documentation were incomplete; remediation subsequently passed automatic checks.
- Provider-neutral contracts, PostgreSQL discovery/readiness/authenticated probes, Agent outbox, server ingestion, RBAC, command create/poll/ack and maintenance runners exist.
- Start/Stop/Restart remain unsupported; command execution, attempts, post-probe and `UnknownOutcome` E2E do not exist.
- No real PostgreSQL, credential, certificate enrollment, IdP/MFA, vault lookup, notification channel or provider package activation was exercised.
- PostgreSQL implementation is not homologation and public support remains `No`.
- The historical Human Gate says deterministic samples were reviewed; the validator now contests whether that was an informed approval.

### Human review sample

- Review representative readiness/transport/authentication mappings and confirm TCP success is never healthy.
- Review negative human/Agent authorisation and exact route/scope/version checks.
- Review ConfigMigrator dry-run/apply/idempotency/rollback and secret rejection using sanitised fixtures.
- Confirm the accepted limitations above and that no administrative operation/provider support was approved.

### Ratification record

- Validator/date: `Bruno — 2026-07-13`
- Samples reviewed now:
  - Audit and remediation: `APROVADO` — configuration migration, typed discovery, negative fixtures, security and state documentation remained consistent after the original findings.
  - PostgreSQL provider and states: `APROVADO` — typed validation/readiness, transport-limited TCP fallback, canonical classification, no shell execution and no unsupported administrative capability claim were reviewed.
  - Human/Agent authorisation: `APROVADO` — deterministic tests cover RBAC, authenticated identity, permission scope, expiry, fail-closed denial and audit.
  - Idempotency and delivery: `APROVADO` — command delivery, outbox, Agent/Server synchronisation, replay, sequence conflict, retry and invalid responses have defined tested behaviour.
  - ConfigMigrator: `APROVADO` — dry-run, field validation, secret blocking, backup, atomic writing, idempotency and guarded rollback were reviewed.
  - Capabilities and public support: `APROVADO` — the matrix distinguishes implementation, support, homologation and roadmap without claiming unsupported capability.
- Samples not repeated: real PostgreSQL, real certificates, IdP, vault, external notifications, administrative execution, external integrations and real-environment homologation/infrastructure validation were not executed.
- Reservations accepted: PostgreSQL homologation remains `None` and public support `No`; administrative capabilities remain unsupported in this increment; real integrations, certificates, external identity, vault, notifications and operational execution remain assigned to later authorised phases; approval is limited to the available `STATE-04` scope/evidence.
- Decision: `APROVADO`
- Justification/evidence: the validator reviewed all six named deterministic evidence groups, explicitly preserved every unexecuted infrastructure/operation boundary and approved only `STATE-04 BACKEND_IMPLEMENTATION`.
- Confirmation received: `Ratifico a decisão acima exclusivamente para STATE-04 BACKEND_IMPLEMENTATION.`

## Overall result

- `STATE-00`: `APROVADO` — retrospectively ratified by Bruno on 2026-07-13 with the recorded scope limitations
- `STATE-01`: `APROVADO COM RESSALVAS` — retrospectively ratified by Bruno on 2026-07-13; onboarding not repeated and remote CI not independently proved
- `STATE-02`: `APROVADO COM RESSALVAS` — retrospectively ratified by Bruno on 2026-07-13; all ADRs accepted, with ADR-0005 and the recorded operational limits reserved
- `STATE-03`: `APROVADO COM RESSALVAS` — retrospectively ratified by Bruno on 2026-07-13; no real PostgreSQL, production migration/deletion, PITR/restore, legal hold or operational rollback
- `STATE-04`: `APROVADO` — retrospectively ratified by Bruno on 2026-07-13 with the recorded homologation, support and external-integration limits
- Lifecycle progression: `EM ESPERA`
- Retrospective ratification: `CONCLUÍDA`
- Next Human Gate: `STATE-05 FRONTEND_IMPLEMENTATION`

All five independent retrospective records are complete. The project resumes only the `STATE-05` closure workflow: its automatic re-audit is approved in scope, but the named human visual/accessibility samples and the exclusive `STATE-05` decision remain pending. `STATE-06` is not authorised.
