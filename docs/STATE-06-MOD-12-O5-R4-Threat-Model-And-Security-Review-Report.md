# O5-R4 — Observer Activation-Scope Threat Model and Security Review

## Disposition

- Date: 2026-07-24.
- Authorised baseline: `1f9e90874ae580e571a9d657dc8099f217ded7e0`.
- Automatic result: `APPROVED`.
- Human Gate O5-R4: `PENDING`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.
- Critical findings: `0`.
- High findings: `0`.
- Medium findings: `3`, each assigned to a functional owner, deadline and fail-closed risk
  decision.
- Low findings: `0`.
- Unknown trust boundaries: `0`.
- Source, test, executable configuration and dependency changes: none.
- Real data, provider, database, corpus, credential, secret and external access: not used.

This review models the future activatable Observer scope without creating an activation path. It
uses the accepted O1–O5-R3 contracts and their existing synthetic harnesses to review control,
telemetry, corpus, evaluation, API/UI and observability together. The model is realistic enough to
identify the future boundaries and required controls, but it does not represent an implemented or
operational Observer composition.

## Severity rule

A critical or high finding, or any unknown trust boundary, blocks O5-R4 immediately. A medium or
low finding is admissible only when it has a functional owner, a deadline tied to a later gate and
a risk decision that prevents activation until the control is proved. Missing future operational
components are assessed in the current pre-activation context: attempting activation before their
deadline would elevate the condition and fail the later gate.

## Assets

| ID | Asset | Required protection |
|---|---|---|
| `A-01` | activation scope, purpose, environment, revision and expiry | authenticated dual approval, one-use consumption and exact scope binding |
| `A-02` | trust bundle, root/revocation heads, checkpoint, witness and audit intent | role separation, integrity, monotonic continuity, fencing and quarantine |
| `A-03` | canonical observation envelope and source sequence | bounded schema, provenance, freshness, idempotency and no provider-native secret payload |
| `A-04` | corpus manifest, exact membership, partitions and withdrawal state | independent authority, content addressing, disjointness, anti-rollback and irreversible withdrawal |
| `A-05` | frozen policy, holdout access and evaluation result | freeze-before-use, one-use holdout, complete metrics and no feedback |
| `A-06` | factual Observer projection and trace identifiers | authenticated read-only delivery, integrity, freshness, uncertainty and no write authority |
| `A-07` | SLI measurements, diagnostic codes, alert mappings and runbooks | allow-listed codes, bounded cardinality/retention and sanitised evidence |
| `A-08` | resource reservations, deadlines, cancellation and monotonic fences | checked admission, reserved control capacity, quiescence and stale-publication refusal |

## Identities and responsibilities

| ID | Identity or role | Permitted responsibility | Explicit denial |
|---|---|---|---|
| `I-01` | policy and root governance roles | approve bounded trust and recovery decisions | cannot evaluate telemetry or execute commands |
| `I-02` | distinct attestation and signing roles | authenticate exact synthetic artefacts | cannot invent policy or reuse another role's key |
| `I-03` | host trust/resource coordinator | validate, reserve, fence and commit complete state | cannot choose a divergent branch or publish partial output |
| `I-04` | synthetic Agent and Server source identities | produce and ingest canonical test observations | cannot grant activation or recommendation authority |
| `I-05` | corpus authority and steward roles | approve exact membership, partitions and withdrawal | cannot train, recommend or execute |
| `I-06` | O5-R2 approval roles A and B | approve one exact bounded synthetic admission | cannot activate normal composition |
| `I-07` | O4 fixed sandbox subject | read one HTTPS loopback projection | cannot write, command or access an external origin |
| `I-08` | Observer control/security owner roles and incident commander | own synthetic alerts, escalation and runbooks | are not named operational people or on-call contacts |

All implemented keys, subjects and approvals used by this review are synthetic and test-only.
Operational identities, custody and contacts remain absent and are recorded below as readiness
findings.

## Trust boundaries and data flows

| Boundary | Flow | Required decision |
|---|---|---|
| `B-01` | external governance → future activation authority | exact, authenticated, dual, scoped and one-use; unavailable in normal composition |
| `B-02` | untrusted bundle/source → O1 trust and resource coordinator | authenticate before semantic trust; bound bytes/work/time; quarantine continuity damage |
| `B-03` | synthetic Agent → Server O2 ingress | canonical provider-neutral envelope, sequence, freshness, idempotency and revocation |
| `B-04` | O1 accepted context → O2 pipeline → deterministic MOD-12 | complete context only; deadline/cancellation/fence revalidation before publication |
| `B-05` | corpus authority → O3-A corpus → O3-B frozen evaluation | exact membership, disjoint partitions, freeze before holdout and no feedback |
| `B-06` | accepted O2/O3 results → O4 projection/API/UI | digest-bound trace, HTTPS loopback, authenticated read, no writes or action controls |
| `B-07` | O5-R2 diagnostics → O5-R3 observability | allow-listed code only, closed labels, bounded records/series and non-executable runbook |
| `B-08` | Observer output → recommendation/command/executor boundary | hard separation: no recommendation, plan, command, automation or executor authority |
| `B-09` | evaluation capacity ↔ control-plane capacity | control reservation is independent; narrowing update cancels and fences stale work |
| `B-10` | normal composition ↔ exact test-only sandboxes | exact markers and architecture gates; normal composition remains dormant in `None` |

```text
Synthetic governance and trust ──> O1 trust/resource admission
                                      │
Synthetic Agent ──> O2 ingress ───────┴──> deterministic MOD-12 result
Synthetic corpus authority ──> O3-A ──> O3-B frozen evaluation
Accepted O2 + O3 ────────────────────> O4 read-only projection/API/UI
O5-R2 control diagnostics ───────────> O5-R3 bounded observability
All Observer outputs ──X recommendation ──X command ──X execution
Normal composition: DormantObserverControlPlane + unavailable authority + None
```

## Adversarial review matrix

| Threat | Control reviewed | Existing evidence | Result |
|---|---|---|---|
| opt-in abuse or scope expansion | exact marker, two distinct roles, bounded scope/purpose/revision/expiry and unavailable normal authority | O5-R2 scope/approval tests and normal-control unit tests | passed |
| approval replay or identity/key confusion | one-use nonce, distinct synthetic role keys, exact signature and role checks | O1 cryptographic-role and O5-R2 replay tests | passed |
| downgrade, stale context or supersession | exact schema/revision, freshness, revocation, deadline and publication-time context check | O1/O2-A/O2-B deterministic refusal tests | passed |
| rollback, gap or split view | authenticated predecessor/witness continuity, fencing and quarantine without branch choice | O1, O2-B, O3-A, O3-B and O5-R2 continuity tests | passed |
| corpus poisoning, leakage or substitution | content-addressed manifest, exact membership, disjoint partitions, quantitative criteria and frozen holdout | O3-A/O3-B poisoning, mutation, leakage and tamper tests | passed |
| secret or topology leakage/exfiltration | provider-neutral envelopes, stable refusal codes, bounded evidence and secret canary scan | O2 contracts, O5-R3 canary test and repository secret scan | passed |
| resource/cardinality exhaustion | reservation before work, serial capacity, bounded records/series/retention and fail-closed saturation | O1, O2-B and O5-R3 capacity tests | passed |
| control starvation or stale publication | reserved control capacity, kill switch, cancellation and monotonic fencing | O1 control-lane, O5-R2 kill-switch and O5-R3 saturation exercises | passed |
| API authentication or write bypass | fixed sandbox subject, HTTPS loopback and GET-only endpoint | unauthenticated read `401`; write `404/405`; external origins `0` | passed |
| UI action escalation | read-only presentation and no action controls | O4 browser matrix `28/28`; action-control scan negative | passed |
| recommendation, command or execution escalation | immutable Observer report authority flags and absent executor dependency | Observer unit tests, architecture isolation and browser audit | passed |
| approximate marker or normal-build leakage | exact markers and zero normal route/reference | O1–O5-R3 architecture isolation tests | passed |
| diagnostic tamper or unsafe cardinality | closed incident/code catalogue and fixed refusal outcomes | O5-R3 governance, canary, retention and capacity tests | passed |

## Findings and risk decisions

### `O5-R4-M01` — Named operational accountability is not assigned

- Severity: `MEDIUM` in the pre-activation readiness context.
- Evidence: O5-R3 has functional owner and escalation roles, while O5-R1 explicitly records named
  data, governance, security, incident and homologation owners as pending.
- Functional owner: `role.observer-security-owner`.
- Deadline: before O5-R6 corpus admission and O5-R8 homologation; named incident/on-call ownership
  must also exist before O5-R9.
- Risk decision: accepted only as a hard dependency blocker while `ActivationState=None`; no alert
  is operational and no activation may proceed.

### `O5-R4-M02` — Operational activation identity and custody are unavailable

- Severity: `MEDIUM` in the pre-activation readiness context.
- Evidence: normal composition deliberately resolves `UnavailableObserverActivationAuthority`;
  every usable approval/key remains synthetic and test-only.
- Functional owner: `role.observer-security-owner`.
- Deadline: before O5-R9 integrated rehearsal and any repeated O5 activation gate.
- Risk decision: retain the unavailable authority and reject all normal admissions. No temporary,
  fallback or locally invented operational credential is permitted.

### `O5-R4-M03` — Independent operational continuity/reconciliation technology is not selected

- Severity: `MEDIUM` in the pre-activation readiness context.
- Evidence: O1 proves local synthetic checkpoint/witness continuity, while ADR-0007 records that a
  local checkpoint and audit cannot alone prove silent full restore or fleet-wide split view.
- Functional owner: `role.observer-security-owner`, with the future checkpoint owner and
  independent auditor as required participants.
- Deadline: technology and recovery procedure must be selected and proved before O5-R9 and before
  any `None → Observer` decision.
- Risk decision: no operational trust state may be admitted; continuity uncertainty quarantines
  rather than selecting a branch.

No finding is silently accepted beyond these deadlines. Each condition remains a later gate
blocker, not an implementation claim.

## Automated evidence

All checks ran locally and offline with existing dependencies and harnesses.

| Check | Result |
|---|---|
| shutdown preflight | baseline exact, clean worktree and zero DB-Notifier runtime/listener |
| complete Release solution build | passed, zero warnings and zero errors |
| focused Observer/control unit tests | `38/38 PASSED` |
| focused O1–O5-R3 integration security tests | `69/69 PASSED` |
| focused O1–O5-R3 architecture isolation tests | `27/27 PASSED` |
| O4 browser/API security and accessibility matrix | `28/28 PASSED` |
| unauthenticated O4 read | denied with `401` |
| O4 write attempt | denied with `404/405` |
| O4 external browser origins | `0` |
| O4 activation state | `None` |
| secret scan | passed |
| dependency and lockfile diff | none |

The first attempt launched three build-producing test commands concurrently and encountered only
compiler output-file locks before any test ran. Build servers were shut down, one clean offline
build passed, and all focused suites then passed with `--no-build`. This was a tooling-contention
event, not a product or security finding.

## Preserved limitations

- This is a local synthetic review, not a penetration test of an operational deployment.
- No operational activation authority, identity provider, key custody, witness, telemetry source,
  alert sink, on-call integration or incident platform exists.
- Corpus remains synthetic and non-representative of production.
- PostgreSQL remains only a candidate cell with `Homologation=None` and public support `No`.
- Physical measurements HM-01–HM-03 remain unexecuted.
- O5-R5 and every later lot remain unauthorised.
- No source, test, configuration or dependency was changed and no defect was corrected.

## Cleanup

The O4 runner closed its exact Chrome and sandbox-host trees and removed its GUID-owned profile and
host roots. Final cleanup confirmed zero DB-Notifier process, listener, dedicated browser/profile,
O1–O5 temporary root and coverage root. `ActivationState=None` remained unchanged.

## Next decision

O5-R4 is automatically `APPROVED` with the three bounded medium readiness findings above. Its Human
Gate remains pending. The next eligible step is a separate Human Gate O5-R4 reviewing this report,
the risk decisions and deadlines. O5-R5, operational data, `OBSERVER` activation and lifecycle
transition remain unauthorised.
