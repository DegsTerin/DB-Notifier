# PF-OBS-1-D5 — Frozen Process-History Diagnostic Protocol

## Authority and boundary

- Version: `pfobs1-d5-process-history-diagnostic-1.0.0`.
- SHA-256:
  `82606BF31085214607C8CBE401C0F4050D9465523B9B01F89FFE45731012FFDA`.
- V3 dependency:
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
- Lifecycle: `STATE-06 INTEGRATION`.
- MOD-12 activation: `ActivationState=None`.
- Scope: synthetic, local, marker-gated attribution of the residual
  `Cancellation/Cold` working-set excess.
- Physical HM-01–HM-03 execution, PostgreSQL, the pilot runtime and Observer
  activation remain prohibited.

The protocol is frozen before D5 diagnostic results are produced. It does
not correct the V3 workload or change any V3 SLO.

## Canonical statement

```text
pfobs1-d5-process-history-diagnostic|1.0.0|v3=60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A|runs=2|variants=exact-prefix,without-first-byte,without-idle,cancellation-only|precondition=v3-exact|prefix=first-byte-cold-5-30,first-byte-warm-5-30,idle-cold-5-30,idle-warm-5-30,cancellation-cold-5-13|metrics=working-set,private-memory,gc-heap,gc-committed,os-threads,threadpool,handles,allocated,committed-private,committed-mapped,committed-image,committed-unknown|limit=786432|samples=all|forced-gc=v3-two-existing-only
```

## Frozen matrix

Each row executes twice in a fresh process. Every row first executes the
unchanged V3 precondition and then retains the selected scenarios in their
original V3 order.

| Variant | FirstByte | Idle | Cancellation/Cold | Samples per process |
|---|---:|---:|---:|---:|
| `ExactPrefix` | `70` | `70` | first `18` | `158` |
| `WithoutFirstByte` | `0` | `70` | first `18` | `88` |
| `WithoutIdle` | `70` | `0` | first `18` | `88` |
| `CancellationOnly` | `0` | `0` | first `18` | `18` |

The first 18 `Cancellation/Cold` samples are the five V3 warm-ups followed
by measured repetitions 1–13. This reaches the exact position of the
retained post-D3 failure without replacing or extending the history.

## Required instrumentation

Every retained sample includes its complete unchanged V3 measurement plus
before/after observations of:

- working set and private memory;
- managed heap and GC committed memory;
- operating-system and ThreadPool thread counts;
- handle count and cumulative managed allocation;
- committed private, mapped, image and unknown virtual-memory regions.

Instrumentation runs outside the V3 sample boundaries. If it prevents
reproduction or cannot collect a complete counter set, D5 is `BLOCKED`.

## Existing V3 forced collections

The exact V3 precondition contains two existing `GC.Collect` calls separated
by `GC.WaitForPendingFinalizers`. D5 preserves and executes those calls only
because they are mandatory parts of the exact historical prefix.

D5 must not add, remove, move, repeat or condition either call. Any other
forced collection, `EmptyWorkingSet`, priority change, affinity change,
sample selection or hidden result is prohibited.

## Attribution and stop rules

The unchanged inclusive working-set limit remains `786,432 bytes`.
Reproduction requires an excess in both `ExactPrefix` fresh-process runs.
Attribution additionally requires controlled variants to identify a
specific accumulated phase or resource category consistently across both
runs.

The result is `BLOCKED` when:

- the exact excess is not reproduced twice;
- control variants contradict one another;
- no resource/state category consistently explains the excess;
- instrumentation perturbs the measurement;
- any sample or required metric is missing; or
- additional tooling or authority would be required.

All complete samples are retained whether their V3 thresholds pass or fail.
No D5 result is authorising, and no new physical campaign follows
automatically.
