# PF-OBS-1-D4 — Cancellation/Cold Diagnostic Protocol

## Purpose

This protocol isolates the residual process working-set growth observed in
`Cancellation/Cold` after PF-OBS-1-D3. It is diagnostic evidence only. It is
not a PF-OBS-1 physical campaign, does not change the frozen V3 protocol and
cannot authorise Observer activation.

`ActivationState=None` remains immutable.

## Frozen diagnostic identity

| Field | Value |
|---|---|
| Version | `pfobs1-d4-cancellation-diagnostic-1.0.0` |
| SHA-256 | `FBA236A73DCBA87BA7247394082080AF9EB72A7D8AFE8CD3667C44A24EA5EDB7` |
| Retained repetitions | `35` per isolated strategy |
| Non-retained preparation | exactly `1` invocation per isolated strategy |
| Per-invocation deadline | `100 ms` |
| Inclusive working-set limit | `786,432 bytes` |
| Input | exact synthetic `65,536`-byte D3 buffer |

The digest is calculated over this exact UTF-8 canonical statement:

```text
pfobs1-d4-cancellation-diagnostic|1.0.0|repetitions=35|warmup=1|stages=baseline,materialisation,checkpoint,linked-token,cancel-after,observation,disposal|metrics=working-set,private-memory,gc-heap,gc-committed,threads,handles,allocated,cts-active,timer-active,wait-active|limit=786432|deadline-ms=100|strategies=async-delay,wait-handle|samples=all
```

## Isolation and comparison

Each strategy runs in a fresh marker-gated process so cold process resources
cannot be transferred between strategies:

1. `async-delay` reproduces the post-D3 cancellation path with a linked token,
   `CancelAfter` and an infinite `Task.Delay`.
2. `wait-handle` preserves the same linked token, ten-millisecond
   `CancelAfter`, outer cancellation and deadline, but observes cancellation
   through the token wait handle without an asynchronous delay continuation.

The second strategy is a remediation candidate, not a pre-approved result.
No favourable execution may be selected and no retained sample may be
removed, replaced or hidden.

## Frozen stages and metrics

Every retained invocation records these stages in this order:

1. baseline;
2. materialisation;
3. checkpoint;
4. linked-token creation;
5. `CancelAfter` scheduling;
6. cancellation observation;
7. disposal.

Every stage records only sanitised counters:

- process working set;
- process private memory;
- managed heap size;
- GC committed bytes;
- managed ThreadPool thread count;
- process handle count;
- cumulative managed allocation;
- active D4-owned linked token sources;
- active D4-owned cancellation timers;
- active D4-owned waits.

No payload, machine name, user name, process identifier or topology is
retained.

## Predeclared diagnosis rules

A cause may be attributed only when all retained samples are present and:

- the stage introducing the residual growth is repeatable;
- working-set growth is corroborated by private memory, GC committed memory,
  thread count or handle count;
- D4-owned resource counters return to zero after disposal;
- the comparison changes only the cancellation observation mechanism;
- the evidence distinguishes observation from inference.

The remediation candidate passes only when all 35 retained samples remain at
or below `786,432 bytes`, every cancellation is observed within `100 ms`,
all owned resources return to zero and no sample is discarded.

## Prohibitions

The diagnostic must not use forced garbage collection, `EmptyWorkingSet`,
priority or affinity changes, favourable sample selection, hidden samples,
reduced workloads, a changed V3 threshold, PostgreSQL, external access,
normal composition, Observer activation or a lifecycle transition.

If the evidence cannot prove a cause, or if the correction requires a V3
methodology change, PF-OBS-1-D4 stops without a new campaign.
