# PF-OBS-1 — Post-D1 Resumption Report

## Decision

The single authorised PF-OBS-1 resumption is `FAILED`. Execution stopped during the first
physical campaign. No replacement campaign, second physical campaign, live PostgreSQL pipeline,
corpus, calibration, holdout, resilience campaign or human sample was executed.

`ActivationState=None` remains unchanged.

## Exact gate failure

| Field | Retained value |
|---|---|
| Protocol | `pfobs1-physical-measurement-2.0.0` |
| Protocol SHA-256 | `53F40F7DC72548EB488FFF729823BF0DCFD64EDD085314022C8E746CC45B5D71` |
| Phase | `FirstByte` |
| Temperature | `Cold` |
| Sample classification | batch-level repeatability after 5 warm-ups and 30 measured samples |
| Metric | `RepeatabilityCoefficient` |
| Observed | `0.2943936135230859` |
| Inclusive limit | `0.20` |
| Diagnostic | `o5r5d1.threshold.repeatability-coefficient` |
| Completed samples | `35/560` |
| Completed summaries | `1/16` |

All thirty measured values remain in the retained report. Their elapsed-time distribution was
P50 `0.0343 ms`, P95 `0.0679 ms`, P99 `0.0960 ms` and maximum `0.0960 ms`. The five fixed
sequential-group medians produced the failed coefficient above. No sample was removed, reordered,
replaced or hidden, and no absolute threshold was relaxed.

## Retained evidence

- Local path:
  `artifacts/pf-obs-1/4e020a2451e1453a81f65070ed203167/hm-01-03-run-1.json`.
- Size: `25,208 bytes`.
- SHA-256: `4BC4DF3F436B137B909F3A034190D5FF6446A46EAD2FBBCAFEF0B67509B0153C`.
- Structural state: complete failed report, one bounded diagnostic, no temporary sibling.
- Environment: Windows `10.0.26200`, .NET runtime `10.0.9`, SDK `10.0.301`, x64 process,
  eight logical processors and `16,963,534,848` installed-memory bytes.

The evidence is intentionally ignored by Git and retained locally. It contains no database
payload, credential, secret, user name, machine name or authorised production topology.

## Cleanup

The final audit found zero project-owned process, active-state file, physical temporary root,
PF-OBS-1 container, network or volume. The retained JSON is evidence, not an active runtime
residue. The exact PostgreSQL image was already present locally, and no download or external
access occurred.

## Consequence

The D1 retention mechanism is automatically approved and worked as designed. PF-OBS-1 itself
remains failed at the unchanged physical repeatability gate. A further attempt, changed protocol,
threshold adjustment or progression to the PostgreSQL/Observer stages requires a new explicit
authorisation. Observer activation and lifecycle transition remain prohibited.

