# PF-OBS-1 — Frozen Physical Measurement Protocol v3

## Authority and boundary

- Protocol version: `pfobs1-physical-measurement-3.0.0`.
- Canonical SHA-256:
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
- Lifecycle: `STATE-06 INTEGRATION`.
- MOD-12 activation: `ActivationState=None`.
- Scope: future local, synthetic, test-only HM-01–HM-03 evidence for PF-OBS-1.
- Physical execution under this version is prohibited by PF-OBS-1-D2.
- Provider, database, corpus, Observer publication, recommendation, command, automation and
  lifecycle authority remain outside this protocol.

Version 3 changes only how the `FirstByte` phase separates its absolute liveness observation from
its relative repeatability input. It is frozen before any future physical measurement.

## Evidence-based cause

The retained failed v2 report has SHA-256
`4BC4DF3F436B137B909F3A034190D5FF6446A46EAD2FBBCAFEF0B67509B0153C`. Its thirty measured
`FirstByte/Cold` whole-operation durations ranged from `0.0313 ms` to `0.0960 ms`. The five
predeclared group medians were `0.06310`, `0.03405`, `0.03415`, `0.03335` and `0.03395 ms`,
producing coefficient `0.2943936135230859`, above the unchanged `0.20` limit.

The v2 repeatability input was the elapsed duration of a complete single-byte operation. At less
than one tenth of a millisecond, that value combined the requested first observation with fixed
invocation and measurement cost. The first group median was more than `1.8` times the largest of
the other four group medians. Therefore the retained evidence proves that whole-operation
relative variation was dominated by the initial fixed-cost region rather than a sufficiently
sized deterministic work window.

The retained report cannot identify a more specific host cause such as scheduler dispatch, JIT,
page-in or another operating-system effect. Version 3 does not claim one.

## Unchanged absolute and resource gates

All v2 absolute limits remain unchanged:

| Dimension | Inclusive limit |
|---|---:|
| FirstByte total phase elapsed | `2,250 ms` |
| First observation elapsed | `2,250 ms` |
| Idle elapsed | `2,250 ms` |
| Cancellation and control update elapsed | `3,000 ms` |
| Parse, cryptography, sort and analysis elapsed | `10,000 ms` |
| Input bytes | `65,536` |
| Structure depth | `16` |
| Items | `256` |
| Accounted memory | `524,288 bytes` |
| Empirical heap, working-set and checkpoint-allocation peak | `786,432 bytes` |
| Deterministic work units | `100,000` |
| Results | `64` |
| Output bytes | `65,536` |
| Control metadata entries | `64` |
| Parallel samples | `1` |
| Queue | disabled |
| Total phase deadline | `10,000 ms` |
| Measurement checkpoints | `64` |
| Elapsed work cost | `100 microseconds per work unit` |
| CPU work cost | `100 microseconds per work unit` |

The separate first-observation gate and the work-cost gates on the new fixed window are additional
fail-closed checks. They do not enlarge any v2 allowance.

## Predeclared FirstByte measurement

Every `FirstByte` sample must execute this exact sequence:

1. capture the complete baseline counters;
2. perform one bounded read and prove that exactly one byte was observed;
3. capture exactly one checkpoint immediately after that first observation;
4. perform exactly `100,000` additional deterministic byte observations;
5. capture the complete terminal counters;
6. retain total elapsed, first-observation elapsed and fixed-window elapsed as separate raw fields;
7. retain total and fixed-window CPU, heap, working-set, allocation and work-cost evidence;
8. apply the unchanged `2,250 ms` absolute gate to both total elapsed and first observation;
9. apply unchanged memory, CPU, work, deadline and cancellation gates;
10. refuse the sample if the checkpoint is absent, duplicated, reordered or backward-moving.

The first-observation latency is the factual absolute liveness metric. The elapsed time from the
first-observation checkpoint through the terminal snapshot is the only `FirstByte` input to the
relative repeatability statistic.

## Repeatability and raw-evidence preservation

For each phase and temperature, five warm-ups and thirty measured samples remain mandatory.
Repeatability continues to use five fixed sequential groups of six and the coefficient of
variation of their medians, with the unchanged inclusive limit `0.20`.

For `FirstByte`, those group medians use the fixed-window elapsed field. All other phases continue
to use total elapsed. Raw P50, P95, P99, maximum, mean, population standard deviation and
coefficient remain available for total elapsed and first-observation latency.

No sample, group or raw field may be removed, selected, replaced, trimmed, winsorised, reordered,
hidden or substituted after observing a result. Variation in first-observation latency therefore
remains visible even when it is not the relative-repeatability input.

## Reproducibility and fail-closed rules

Two complete consecutive future campaigns remain mandatory. A failed or incomplete campaign
stops execution and cannot be replaced by a favourable third run.

The protocol refuses a mismatched version or digest, any `FirstByte` work declaration other than
exactly `100,000`, an absent or ambiguous first-observation checkpoint, non-finite or
backward-moving counters, an absolute or resource breach, repeatability above `0.20`, concurrent
ownership, deadline, cancellation or incomplete evidence.

Passing this protocol would remain non-authorising. It cannot activate Observer or change
`ActivationState=None`.
