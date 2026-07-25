# PF-OBS-1 — Frozen Physical Measurement Protocol v2

## Authority and boundary

- Protocol version: `pfobs1-physical-measurement-2.0.0`.
- Canonical SHA-256:
  `53F40F7DC72548EB488FFF729823BF0DCFD64EDD085314022C8E746CC45B5D71`.
- Lifecycle: `STATE-06 INTEGRATION`.
- MOD-12 activation: `ActivationState=None`.
- Scope: local, synthetic, test-only HM-01–HM-03 evidence for PF-OBS-1.
- Provider, database and corpus observations are outside the physical measurement process.
- Observer publication, recommendation, command, automation and lifecycle authority are prohibited.

This version is frozen before any resumed physical measurement. It corrects only the repeatability
method that was too sensitive to Windows scheduler noise in the earlier checkpoint. Every absolute
SLO, resource ceiling, security boundary and deterministic admission limit remains unchanged.

## Unchanged finite envelope and absolute gates

| Dimension | Inclusive limit |
|---|---:|
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

The liveness limits remain `2,250 ms` for first-byte and idle and `3,000 ms` for cancellation and
control update. Parse, cryptography, sort and analysis remain bounded by `10,000 ms`, `100
microseconds per work unit` of monotonic elapsed time and `100 microseconds per work unit` of CPU.
Every raw measured sample must satisfy every applicable absolute limit.

## Scheduler-robust repeatability method

For every phase and each `Cold`/`Warm` temperature:

1. execute five warm-up samples and retain them outside the measured distribution;
2. execute exactly thirty measured samples in stable sequence;
3. preserve all raw samples, raw P50, P95, P99, maximum, mean, population standard deviation and
   raw coefficient of variation;
4. divide the thirty measured elapsed values into five fixed sequential groups of six;
5. calculate the median of each group without sorting or regrouping the campaign sequence;
6. calculate the coefficient of variation of the five group medians;
7. require that repeatability coefficient to remain at or below the unchanged `0.20` threshold;
8. require every raw sample and every absolute worst case to pass its unchanged absolute gates.

No measured sample may be removed, hidden, replaced, trimmed, winsorised, reordered or selected
after observing results. A failed campaign cannot be substituted by a favourable rerun. Grouping,
group size, median statistic and threshold are content-addressed in the protocol digest.

The group-median statistic reduces the influence of isolated scheduler dispatch jitter on relative
variation when the underlying phase duration is very small. It does not excuse an absolute SLO,
memory, allocation, CPU, work-unit, deadline or cancellation failure.

## Reproducibility rule

Two complete consecutive campaigns must pass the exact same protocol, environment declaration,
phase order, workloads and gates. Both complete reports must be retained in the temporary
PF-OBS-1 evidence root. Any failed, incomplete or differently configured campaign fails the
reproducibility gate; execution stops and no third run may be selected as a replacement.

The campaign records operating-system description, architectures, .NET runtime and SDK, logical
processor count, installed memory and monotonic clock frequency without machine name, user name,
serial number, path, payload, credential or unauthorised topology.

## Fail-closed disposition

The protocol refuses or fails the campaign for a mismatched version or digest, an invalid phase or
sequence, missing or additional samples, non-finite or backward-moving counters, a resource
declaration outside the accepted envelope, any absolute-gate breach, a repeatability coefficient
above `0.20`, concurrent ownership, deadline, cancellation before a complete sample or incomplete
evidence.

Only complete passing evidence may allow PF-OBS-1 to continue to its disposable PostgreSQL
laboratory. Passing physical evidence remains non-authorising and cannot change
`ActivationState=None`.
