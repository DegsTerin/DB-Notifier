# O5-R5-A — Frozen Physical Measurement Protocol

## Authority and boundary

- Protocol version: `o5r5a-physical-measurement-1.0.0`.
- Canonical SHA-256:
  `266B7A952DF1A46BEE4577894D0A9206D92917AC661E0E17F9052DE1EB415DD7`.
- Lifecycle: `STATE-06 INTEGRATION`.
- MOD-12 activation: `ActivationState=None`.
- Scope: local, synthetic, test-only readiness for a later separately authorised O5-R5 physical
  campaign.
- Physical results: prohibited in O5-R5-A.
- Provider, database, corpus, credential, external access and operational publication: prohibited.

This protocol is frozen before any O5-R5 physical result exists. It defines how the future
campaign must collect and judge evidence; it does not itself measure the host, approve headroom or
change deterministic admission limits.

## Exact marker and isolation

The only runner marker is `DBNOTIFIER_O5_R5_A_TEST_ONLY`. The runner belongs exclusively to the
existing integration-test assembly. Normal product source, Server composition and
`ObserverActivationState` must contain no O5-R5-A reference or activation path.

The runner:

- admits one sample at a time and has no queue;
- retains no global result state;
- returns only phase-labelled, sanitised in-memory records;
- never publishes an Observer result;
- exposes no recommendation, command, automation or provider capability;
- refuses missing, altered or additional markers.

## Frozen environment envelope

| Dimension | Frozen value |
|---|---:|
| Maximum input bytes | `65,536` |
| Maximum accounted-memory bytes | `524,288` |
| Maximum deterministic work units | `100,000` |
| Maximum structure depth | `16` |
| Maximum items | `256` |
| Maximum result count | `64` |
| Maximum output bytes | `65,536` |
| Maximum parallel samples | `1` |
| Queue | disabled |
| Total phase deadline | `10,000 ms` |
| Maximum measurement checkpoints | `64` |

These values reproduce the accepted O1 synthetic resource envelope. A future campaign may measure
within the envelope but may not enlarge it.

## HM-01 liveness thresholds

| Phase | Inclusive physical threshold | Source of the preregistered limit |
|---|---:|---|
| first-byte | `2,250 ms` | O1 first-byte bound `2,000 ms` plus fixed `250 ms` scheduling allowance |
| idle | `2,250 ms` | O1 idle bound `2,000 ms` plus fixed `250 ms` scheduling allowance |
| cancellation | `3,000 ms` | accepted O5-R3 containment SLO |
| control update | `3,000 ms` | accepted O5-R3 containment SLO |

The scheduling allowance is fixed before measurement and is not recalculated from results.
First-byte and idle must still honour the underlying `2,000 ms` logical deadline; the additional
allowance applies only to observation and scheduling overhead in the physical evidence.

Every liveness scenario must run while the test-only data-plane reservation is at its maximum
declared envelope. Control update must use the separately reserved bounded control lane.

## HM-02 memory and allocation thresholds

The runner records deltas from the pre-phase baseline for:

- managed heap;
- working set;
- allocation between consecutive bounded checkpoints.

The inclusive empirical limit for each metric is:

```text
ceil(accounted-memory-bytes × 3 / 2)
```

At the maximum accounted-memory declaration, the limit is `786,432 bytes`. This is a fixed 50%
headroom ceiling. Negative heap or working-set movement is retained as a zero delta; allocated
bytes and CPU time must never move backwards. Cumulative allocation is reported separately but
does not replace peak checkpoint allocation.

Any metric above the ceiling fails the sample. O5-R5 may approve the headroom only if every
measured sample stays within the ceiling; it may not increase the ceiling after seeing results.

## HM-03 work-cost thresholds

The compute phases are exactly:

- parse;
- cryptography;
- sort;
- analysis.

For each phase the runner records deterministic work units, CPU time and monotonic elapsed time.
The inclusive preregistered limits are:

| Metric | Limit |
|---|---:|
| elapsed cost | `100 microseconds per work unit` |
| CPU cost | `100 microseconds per work unit` |
| absolute phase duration | `10,000 ms` |

The future campaign must exercise declared work-unit points without changing the work model.
Admission at `maximum − 1` and `maximum` must succeed when every other dimension is valid;
`maximum + 1` must fail before the operation executes.

Time remains evidence only. A fast run cannot authorise work above the deterministic unit limit,
and a slow run cannot silently redefine a work unit.

## Repetition and statistical protocol

For every phase and each `Cold`/`Warm` temperature:

1. execute `5` warm-up samples, excluded from statistics but retained in the sanitised evidence;
2. execute `30` measured samples;
3. retain every measured sample; do not trim, winsorise or discard outliers;
4. report P50, P95, P99, maximum, arithmetic mean, population standard deviation and coefficient
   of variation;
5. use nearest-rank percentiles;
6. require coefficient of variation at or below `0.20`;
7. require every measured sample and the reported worst case to meet all applicable thresholds.

Clock frequency, OS, architecture, .NET SDK/runtime, processor count and installed memory must be
captured once as sanitised campaign metadata. Machine name, user name, serial number, paths,
payloads and topology are forbidden.

## Fail-closed rules

The runner refuses the sample or campaign when:

- the marker, protocol version or protocol digest differs;
- the phase, temperature, repetition or resource declaration is invalid;
- a deterministic resource dimension is above its accepted maximum;
- monotonic time, allocated bytes or CPU time moves backwards;
- a metric is negative, non-finite, incomplete or cannot be attributed to one exact phase;
- cancellation occurs before a complete sample exists;
- another sample already owns the serial runner;
- more than `64` checkpoints are requested;
- a warm-up enters the measured distribution;
- fewer or more than the exact required samples are supplied;
- a phase or temperature is absent, mixed or duplicated;
- any threshold, worst case or variance criterion fails.

Failure produces a stable diagnostic code and no authorising result.

## Physical campaign gate

O5-R5-A may compile and test this protocol only with synthetic metric sources. It may not invoke
the physical metric source or execute a physical workload. The later O5-R5 repetition requires:

- automatic and human acceptance of O5-R5-A;
- a separate execution authorisation;
- the exact frozen protocol version and digest;
- an exact clean baseline and shutdown preflight;
- complete cleanup and a separate Human Gate.

The protocol digest above was calculated from the runner's invariant, line-oriented canonical
representation before any synthetic runner test was executed.
