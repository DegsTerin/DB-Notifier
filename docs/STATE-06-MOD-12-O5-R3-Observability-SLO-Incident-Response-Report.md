# O5-R3 — Sanitised Observability, SLO and Incident Response Sandbox Report

## Disposition

- Date: 2026-07-24.
- Authorised baseline: `cdfdce9d3961b2ca64f4f009c2b822a13ced7f47`.
- Automatic result: `APPROVED`.
- Human Gate O5-R3: `PENDING`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.
- Provider, database, corpus, credential and telemetry: not used.
- External integration, alert delivery, restore, download, push and deploy: not performed.

O5-R3 adds a closed observability and incident-exercise boundary only to the exact
`DBNOTIFIER_O5_R3_TEST_ONLY` integration sandbox. It maps six already accepted O5-R2 diagnostic
codes to bounded alerts, functional owner roles, one escalation role and non-executable runbooks.
It measures synthetic detection, containment, recovery and closure against numeric SLOs frozen
before each exercise.

No O5-R3 code or reference was added to normal product composition. The normal control plane
remains dormant, its activation authority remains unavailable and `None` remains the only
activation state.

## Frozen catalogue

- Catalogue version: `o5r3-observability-1.0.0`.
- Maximum retained exercise records: `24`.
- Maximum metric series: `24`.
- Retention: `15 minutes`.
- Labels are selected only from closed enums and allow-listed codes; caller-defined labels are not
  admitted.

| SLI | SLO |
|---|---:|
| `o5r3.sli.detection.elapsed_ms` | at most `1,000 ms` |
| `o5r3.sli.containment.elapsed_ms` | at most `3,000 ms` |
| `o5r3.sli.recovery.elapsed_ms` | at most `8,000 ms` |
| `o5r3.sli.closure.elapsed_ms` | at most `15,000 ms` |

## Alert governance

| Synthetic incident | Accepted O5-R2 diagnostic | O5-R3 alert | Functional owner | Escalation | Runbook |
|---|---|---|---|---|---|
| saturation | `o5r2.control.approval_capacity_exhausted` | `o5r3.alert.control_saturation` | `role.observer-control-owner` | `role.incident-commander` | `o5r3.runbook.saturation` |
| corruption | `o5r2.store.corrupt` | `o5r3.alert.state_corruption` | `role.observer-security-owner` | `role.incident-commander` | `o5r3.runbook.corruption` |
| stale | `o5r2.control.deadline_expired` | `o5r3.alert.stale_context` | `role.observer-control-owner` | `role.incident-commander` | `o5r3.runbook.stale` |
| split view | `o5r2.store.split_view` | `o5r3.alert.split_view` | `role.observer-security-owner` | `role.incident-commander` | `o5r3.runbook.split_view` |
| kill switch | `o5r2.control.kill_switch_engaged` | `o5r3.alert.kill_switch_engaged` | `role.observer-control-owner` | `role.incident-commander` | `o5r3.runbook.kill_switch` |
| rollback | `o5r2.store.rollback` | `o5r3.alert.rollback_detected` | `role.observer-security-owner` | `role.incident-commander` | `o5r3.runbook.rollback` |

Each runbook contains five stable, non-executable action identifiers. The steps preserve sanitised
evidence, verify inactive activation, prove the applicable containment, escalate to the functional
role and close only after control or recovery evidence. They do not contain commands or grant
operational authority.

The owner values above are functional roles, not named people or operational on-call assignments.
The material owner nominations left pending by O5-R1 therefore remain pending and continue to
block O5-R6 and O5-R8.

## Synthetic exercise results

All six authorised incident classes used deterministic in-memory timelines and completed within
the predeclared thresholds:

| Phase | Measured value | SLO | Result |
|---|---:|---:|---|
| detection | `250 ms` | `1,000 ms` | passed |
| containment | `1,000 ms` | `3,000 ms` | passed |
| recovery | `3,000 ms` | `8,000 ms` | passed |
| closure | `6,000 ms` | `15,000 ms` | passed |

The exercise also proved that:

- an unmeasurable SLO, missing functional owner or missing runbook blocks catalogue construction;
- an unknown incident or a diagnostic outside the allow-list fails closed;
- payload-shaped input containing secret, SQL and topology canaries is neither retained nor
  reflected in evidence;
- invalid phase ordering, an exceeded SLO, an expired record, cancellation and capacity exhaustion
  retain no unauthorised evidence;
- retention pruning, record capacity and metric cardinality remain bounded;
- every accepted result remains non-authorising, with `MayEvaluate=false` and
  `MayPublish=false`.

## Automated evidence

All checks ran locally and offline without restore, download or dependency changes.

| Check | Result |
|---|---|
| shutdown preflight | baseline exact, clean worktree and zero DB-Notifier runtime/listener/root |
| focused O5-R3 integration tests | `8/8 PASSED` |
| focused O5-R3 architecture tests | `4/4 PASSED` |
| complete unit suite | `401/401 PASSED` |
| complete integration suite | `91/91 PASSED` |
| complete architecture suite | `79/79 PASSED` |
| complete WPF suite | `10/10 PASSED` |
| complete Release solution build | passed, zero warnings and zero errors |
| proportional .NET coverage | lines `82.01%`, branches `53.92%`, ten required components present |
| source documentation gate | passed for `369` files |

## Preserved limitations

- This is a synthetic in-memory test sandbox, not operational observability.
- No log sink, metrics backend, paging channel, alert delivery or external incident system exists.
- Functional roles are defined, but named accountable people and real escalation contacts are not.
- The exercises prove only the frozen synthetic timing matrix, not operational SLO attainment.
- PostgreSQL remains a candidate cell only, with `Homologation=None` and public support `No`.
- O5 remains blocked by O5-R4 and later prerequisites, including security review, physical
  measurements, representative corpus, operational calibration, exact provider homologation and
  integrated rehearsal.
- No LLM, recommendation, command, automation, activation or lifecycle transition was added.

## Cleanup

The O5-R3 implementation allocates only in-memory synthetic records. Final cleanup confirmed zero
DB-Notifier process, listener, dedicated browser/profile, coverage root and O5-R3 temporary root.
Normal composition remained dormant throughout validation.

## Next decision

The automatic O5-R3 result is `APPROVED`, but the increment remains humanly pending. The next
eligible step is a separate Human Gate O5-R3 reviewing this report and its limitations. O5-R4,
operational observability, `OBSERVER` activation and lifecycle transition remain unauthorised.
