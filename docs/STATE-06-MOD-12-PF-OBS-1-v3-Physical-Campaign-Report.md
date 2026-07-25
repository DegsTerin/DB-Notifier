# PF-OBS-1-V3 — Physical Campaign Report

## Decision

PF-OBS-1-V3 is `FAILED`. Execution stopped during the first of the two
authorised consecutive physical campaigns. The second campaign was not
started, and no replacement or third campaign was executed.

`ActivationState=None` remains unchanged.

## Authorised baseline and protocol

| Field | Value |
|---|---|
| Baseline | `2f5c128e226a18cfef4f93323a66f6ebfb46e2a2` |
| Protocol | `pfobs1-physical-measurement-3.0.0` |
| Protocol SHA-256 | `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A` |
| Runner | Existing marker-gated test-only host |
| Physical campaigns authorised | Exactly two consecutive campaigns |
| Physical campaigns started | One |
| Physical campaigns completed | Zero |
| Replacement or third campaign | None |

The existing Release binary was used without restore, download, build,
implementation or configuration change.

## Exact gate failure

| Field | Retained value |
|---|---|
| Diagnostic | `o5r5d1.threshold.working-set-peak` |
| Phase | `Cancellation` |
| Temperature | `Cold` |
| Sample | measured repetition `13` |
| Metric | `WorkingSetPeak` |
| Observed | `2,998,272 bytes` |
| Inclusive limit | `786,432 bytes` |
| Excess | `2,211,840 bytes` |
| Completed samples | `158/560` |
| Completed summaries | `4/16` |

The failure is an absolute physical resource gate. The runner returned exit
code `3`, retained the complete failed report and stopped before any further
phase or campaign.

## Results reached before the stop

| Phase | Temperature | Result | Repeatability coefficient | Relevant maxima |
|---|---|---|---|---|
| FirstByte | Cold | Passed | `0.1823489689706405` | first observation `0.0519 ms`; elapsed `0.1745 ms`; working set `69,632 bytes`; allocation `68,576 bytes` |
| FirstByte | Warm | Passed | `0.11150855833045359` | first observation `0.0256 ms`; elapsed `0.1384 ms`; working set `0 bytes`; allocation `0 bytes` |
| Idle | Cold | Passed | `0.0034114134455135634` | elapsed `15.1682 ms`; working set `286,720 bytes`; allocation `69,584 bytes` |
| Idle | Warm | Passed | `0.005534449151655676` | elapsed `15.2351 ms`; working set `495,616 bytes`; allocation `8,200 bytes` |

The v2 FirstByte repeatability failure did not recur in these two completed
FirstByte summaries. This observation does not prove campaign
reproducibility because the first campaign was incomplete and the second
campaign was prohibited after the absolute failure.

## HM-01–HM-03 classification

| Measurement | Classification | Basis |
|---|---|---|
| HM-01 latency, cancellation and control update | `BLOCKED` | FirstByte and Idle completed, but the Cancellation matrix stopped at the absolute working-set failure before HM-01 was complete. |
| HM-02 heap, working set and allocation | `FAILED` | `Cancellation/Cold` exceeded the frozen working-set limit by `2,211,840 bytes`. |
| HM-03 CPU and time per unit of work | `NOT TESTED` | Parse, cryptography, ordering and analysis phases were not reached. |
| Two-campaign reproducibility | `FAILED` | The first campaign failed; the mandatory stop rule prohibited the second campaign. |

## Retained evidence

- Run identifier:
  `e4b39e453cf3486c93c6ed8682260c5e`.
- Local path:
  `artifacts/pf-obs-1/e4b39e453cf3486c93c6ed8682260c5e/hm-01-03-run-1.json`.
- Size: `123,245 bytes`.
- SHA-256:
  `B12B8C096DE79F619B59A8F21EA3191A12FEEAE6439F729265B5CEA1AF80C1DE`.
- Structural state: complete failed report, `158` raw samples, four
  summaries, one allow-listed diagnostic and no temporary sibling.
- Environment: Microsoft Windows `10.0.26200`, .NET runtime `10.0.9`,
  SDK `10.0.301`, x64 process, eight logical processors and
  `16,963,534,848` installed-memory bytes.

The evidence was written through, flushed, hash-verified and atomically moved
to its retained project-owned location before cleanup. It is intentionally
ignored by Git and contains no database payload, credential, secret, user
name, machine name or production topology.

## Execution integrity and cleanup

One initial orchestration command was rejected by the local execution policy
before the runner started. A subsequent audit found no output, temporary root
or product process from that rejected attempt, so it was not counted as a
campaign.

The final cleanup audit found:

- zero DB-Notifier-owned process or listener;
- zero physical temporary root;
- zero temporary evidence file;
- exactly one retained evidence file;
- no second-campaign evidence;
- a clean source worktree before this factual documentation update.

No PostgreSQL process, container, provider, database, corpus, credential,
network access or Observer runtime was used.

## Consequence

PF-OBS-1-V3 remains failed at the unchanged absolute physical working-set
gate. The PostgreSQL laboratory, pipeline, corpus, calibration, holdout,
resilience and human demonstration are not eligible to proceed from this
result.

Diagnosis or remediation of the `Cancellation/Cold` working-set peak requires
separate explicit authorisation. A later physical campaign would also require
separate authority and must not reclassify or replace this retained failure.
Observer activation and lifecycle transition remain prohibited.
