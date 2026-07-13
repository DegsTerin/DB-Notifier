# Retrospective Human Gate Ratification

## Status

`PENDENTE`

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

- Validator/date: `PENDENTE`
- Samples repeated now: `PENDENTE`
- Reservations accepted: `PENDENTE`
- Decision: `PENDENTE`
- Justification/evidence: `PENDENTE`
- Required confirmation: `Ratifico a decisão acima exclusivamente para STATE-00 DISCOVERY_MIGRATION.`

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

- Validator/date: `PENDENTE`
- Samples repeated now: `PENDENTE`
- Reservations accepted: `PENDENTE`
- Decision: `PENDENTE`
- Justification/evidence: `PENDENTE`
- Required confirmation: `Ratifico a decisão acima exclusivamente para STATE-01 PROJECT_SETUP.`

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

- Validator/date: `PENDENTE`
- ADR decisions: `PENDENTE`
- Samples repeated now: `PENDENTE`
- Reservations accepted: `PENDENTE`
- Decision: `PENDENTE`
- Justification/evidence: `PENDENTE`
- Required confirmation: `Ratifico a decisão acima exclusivamente para STATE-02 ARCHITECTURE.`

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

- Validator/date: `PENDENTE`
- Samples repeated now: `PENDENTE`
- Reservations accepted: `PENDENTE`
- Decision: `PENDENTE`
- Justification/evidence: `PENDENTE`
- Required confirmation: `Ratifico a decisão acima exclusivamente para STATE-03 DATABASE_MODELING.`

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

- Validator/date: `PENDENTE`
- Samples repeated now: `PENDENTE`
- Reservations accepted: `PENDENTE`
- Decision: `PENDENTE`
- Justification/evidence: `PENDENTE`
- Required confirmation: `Ratifico a decisão acima exclusivamente para STATE-04 BACKEND_IMPLEMENTATION.`

## Overall result

- `STATE-00`: `PENDENTE`
- `STATE-01`: `PENDENTE`
- `STATE-02`: `PENDENTE`
- `STATE-03`: `PENDENTE`
- `STATE-04`: `PENDENTE`
- Lifecycle progression: `EM ESPERA`
- Next ratification decision to present: `STATE-00 DISCOVERY_MIGRATION`

Only after all five independent records are completed may the project resume the `STATE-05` closure workflow. That resumption still requires the current automatic re-audit and its own Human Gate.
