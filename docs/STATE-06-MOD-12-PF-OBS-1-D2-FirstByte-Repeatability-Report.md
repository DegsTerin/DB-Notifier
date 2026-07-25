# PF-OBS-1-D2 — FirstByte Repeatability Diagnosis and Remediation Report

## Decision

`PF-OBS-1-D2` is automatically `APPROVED` within its local, test-only implementation scope.
No physical campaign was executed and no new host measurement was produced.
`ActivationState=None` remains unchanged.

This result freezes a candidate methodology for a separately authorised future campaign. It does
not approve PF-OBS-1, authorise PostgreSQL, activate Observer or permit a lifecycle transition.

## Evidence analysed

D2 read only the locally retained failed v2 report:

| Field | Value |
|---|---|
| Evidence | `artifacts/pf-obs-1/4e020a2451e1453a81f65070ed203167/hm-01-03-run-1.json` |
| SHA-256 | `4BC4DF3F436B137B909F3A034190D5FF6446A46EAD2FBBCAFEF0B67509B0153C` |
| Protocol | `pfobs1-physical-measurement-2.0.0` |
| Phase | `FirstByte/Cold` |
| Measured samples | `30` |
| Whole-operation range | `0.0313–0.0960 ms` |
| Fixed group medians | `0.06310`, `0.03405`, `0.03415`, `0.03335`, `0.03395 ms` |
| Repeatability coefficient | `0.2943936135230859` |
| Inclusive limit | `0.20` |

No retained value was removed, reordered, substituted or hidden.

## Proven cause and limitation

Version 2 used the complete elapsed duration of one single-byte operation as both the absolute
FirstByte latency and the input to a relative repeatability statistic. Every retained value was
below `0.1 ms`; the first group median was more than `1.8` times the largest of the remaining
group medians. The relative result was therefore dominated by the initial fixed-cost region of a
very small operation rather than by a predeclared deterministic work window.

The retained counters do not distinguish JIT, page-in, scheduler dispatch or another specific
host mechanism. D2 deliberately makes no more specific causal claim.

## Remediation

The [frozen protocol v3](STATE-06-MOD-12-PF-OBS-1-Physical-Measurement-Protocol-v3.md) separates:

- absolute FirstByte liveness, measured from baseline to the first observed byte; and
- repeatability, measured only across exactly `100,000` additional deterministic byte
  observations after that first observation.

The runner requires exactly one first-observation checkpoint and refuses any FirstByte work
declaration other than `100,000`. The sample and summary retain total elapsed, first-observation
elapsed, fixed-window elapsed, CPU, memory, allocation and work-rate evidence. The evidence
writer rejects an incomplete, non-finite, inconsistent or phase-incompatible metric shape.

The five warm-ups, thirty measured samples, five fixed sequential groups of six, coefficient limit
`0.20`, two-consecutive-campaign rule and every absolute/resource/security limit remain
unchanged. FirstByte now also receives the existing elapsed and CPU work-cost gates, making
admission more restrictive rather than relaxing a limit.

## Automatic evidence

| Check | Result |
|---|---|
| Protocol v3 version | `pfobs1-physical-measurement-3.0.0` |
| Protocol v3 SHA-256 | `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A` |
| Focused integration tests | `16/16` passed |
| Focused architecture tests | `10/10` passed |
| Complete integration suite | `111/111` passed |
| Complete architecture suite | `89/89` passed |
| Release solution build | passed with zero warnings and errors |
| Changed-file .NET format verification | passed |
| Code documentation gate | passed for `378` source files |
| Markdown links | passed for `797` links in `184` files |
| Secret scan | passed |
| PowerShell parser | passed |
| Physical marker during tests | absent |
| Physical campaign or PostgreSQL | not executed |
| Normal-composition O5-R5 reference | zero |
| Activation state | `None` |

Synthetic tests reproduce the retained v2 group medians and failed coefficient, prove that
first-observation variation remains visible after separation, prove stable fixed-window evidence
passes, prove unstable fixed-window evidence still fails, and refuse absent, duplicate or
wrong-sized FirstByte windows.

## Cleanup and next decision

No DB-Notifier runtime, physical campaign, PostgreSQL resource or project-owned listener was
started. Build and test outputs are ordinary ignored artefacts. The retained failed v2 report
remains unchanged and continues to be the only retained physical evidence in this scope.

Acceptance of D2 requires a separate Human Gate. Even after acceptance, any new physical campaign
requires another explicit authorisation; no future campaign is authorised by this report.
