# PF-OBS-1-D4 — Residual Cancellation/Cold Working-Set Report

## Decision

PF-OBS-1-D4 is `BLOCKED`.

The residual post-D3 working-set cause was not proved. The frozen diagnostic
completed, but an isolated `Cancellation/Cold` process did not reproduce the
`2,916,352`-byte campaign spike and the comparison did not justify changing
the V3 workload.

No physical campaign, workload correction, PostgreSQL or Observer runtime
was executed. `ActivationState=None` remains unchanged.

## Inputs retained before diagnosis

The post-D3 campaign evidence remains authoritative for the failure:

| Field | Value |
|---|---:|
| Phase | `Cancellation/Cold` |
| Failed sample | measured repetition `13` |
| Working-set peak delta | `2,916,352 bytes` |
| Inclusive limit | `786,432 bytes` |
| Managed heap delta | `0 bytes` |
| Managed allocation | `0 bytes` |
| Private-memory, GC committed, thread and handle attribution | not recorded |

The failure proves process-wide growth after earlier phase history. It does
not identify ownership of the pages.

## Frozen D4 diagnostic

The
[D4 diagnostic protocol](STATE-06-MOD-12-PF-OBS-1-D4-Cancellation-Diagnostic-Protocol.md)
was frozen before execution as
`pfobs1-d4-cancellation-diagnostic-1.0.0`, SHA-256
`FBA236A73DCBA87BA7247394082080AF9EB72A7D8AFE8CD3667C44A24EA5EDB7`.

Each strategy used:

- one non-retained preparation;
- 35 retained invocations;
- the exact 65,536-byte D3 synthetic input;
- seven ordered checkpoints;
- the unchanged inclusive limit of `786,432 bytes`;
- complete sample retention and a 100-millisecond cancellation bound.

## Instrumentation corrections

Two complete 35+35 comparisons were rejected as diagnostic evidence:

1. `Process.Threads` allocated about `8,200` managed bytes at repeated
   checkpoints and introduced its own page-in.
2. the replacement `Toolhelp` thread enumeration remained intrusive enough
   to let the ten-millisecond cancellation fire while the observer was
   collecting a checkpoint.

All 140 rejected samples were preserved. They were not selected, removed or
used to approve a remediation. The final source uses non-allocating
ThreadPool count, native working-set/private-memory/handle counters and
built-in GC counters.

## Final isolated comparison

| Metric | Existing `Task.Delay` path | Wait-handle candidate |
|---|---:|---:|
| Retained samples | `35/35` | `35/35` |
| Failed samples | `0` | `0` |
| Maximum working-set delta | `516,096 bytes` | `385,024 bytes` |
| Maximum private-memory delta | `217,088 bytes` | `208,896 bytes` |
| Maximum GC committed delta | `0 bytes` | `0 bytes` |
| Maximum ThreadPool thread delta | `1` | `0` |
| Maximum handle delta | `2` | `1` |
| Maximum observation latency | `20.4751 ms` | `23.5356 ms` |
| D4-owned resource residue | `0/35` | `0/35` |
| Isolated D4 limit | passed | passed |

Final retained evidence:

| Strategy | Local retained path | Size | SHA-256 |
|---|---|---:|---|
| Existing async delay | `artifacts/pf-obs-1-d4/final-8e7bd60a5b5d4a6da3ed4b079b22f0a0/async-delay.json` | `132,134` | `51680E6663F58A9E9A4D3F65BE9B95321A526DA79E0DC3A360AED52496412366` |
| Wait-handle candidate | `artifacts/pf-obs-1-d4/final-8e7bd60a5b5d4a6da3ed4b079b22f0a0/wait-handle.json` | `132,127` | `56D2B0343F8C4C590FE024F45015684F27C1E638A7ACF6EC848EB052F1DF9BE6` |

The wait-handle result is numerically smaller in this isolated run, but both
strategies pass and neither reproduces the historical spike. This is
insufficient to attribute the `2,916,352`-byte process-wide growth to
`Task.Delay`, linked tokens, `CancelAfter`, the timer queue or disposal.

## Implementation disposition

The D4 diagnostic instrumentation and marker-gated evidence writer remain
test-only. The physical V3 runner and workload remain unchanged:

- protocol `pfobs1-physical-measurement-3.0.0`;
- SHA-256
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`;
- existing linked token, `CancelAfter` and `Task.Delay` path;
- inclusive working-set limit `786,432 bytes`;
- two pre-existing preconditioning collections only.

No forced collection was added. `EmptyWorkingSet`, affinity, priority
changes, favourable selection and hidden samples are absent.

## Validation

| Check | Result |
|---|---|
| Focal integration tests: D4, runner and driver | `22/22` passed |
| Complete integration suite | `118/118` passed |
| Focal architecture isolation tests | `12/12` passed |
| Complete architecture suite | `91/91` passed |
| Release build of the dedicated sandbox host | passed, zero warnings/errors |
| Changed-file format gate at warning severity | passed |
| Documentation gate | passed, `380` files |
| Markdown links | passed, `813` links in `191` files |
| Current-worktree secret scan | passed |
| Targeted coverage execution | `4/4` tests passed; the configured collector excludes the test assembly that owns D4, so no numeric D4 class rate was produced |
| D4 references in normal `src/` composition | `0` |
| Protocol V3 changed | no |
| Dependencies or lockfiles changed | no |
| Physical campaign executed | no |

## Validation incident

The command named `verify-dotnet-lockfiles.ps1` was invoked as a lockfile
check, but the script internally executed a locked restore for the 18
solution projects. This restore was outside the D4 authorisation.

The command reported that the repository state remained unchanged and the
subsequent diff confirms no dependency, project or lockfile change. No
download was reported. However, the execution cannot prove that configured
NuGet sources were not consulted for metadata, so this report does not
convert the incident into an authorised or offline action.

No further restore was executed.

## Cleanup

- all four exact D4/coverage temporary roots were removed;
- zero D4 diagnostic process remained;
- zero DB-Notifier product process or listener remained;
- seven reusable MSBuild nodes started by the repository builds were
  identified by exact command line and stopped by verified process identity;
- the ignored retained archive contains the six complete reports and no
  temporary sibling;
- no browser, PostgreSQL, Docker, provider, database or external runtime was
  started.

## Consequence

D4 cannot be declared complete because its mandatory causal proof was not
met. The correct fail-closed disposition is `BLOCKED`, without speculative
remediation.

Any next diagnostic must be separately authorised and must reproduce the
exact process history preceding `Cancellation/Cold` without executing or
substituting an HM-01–HM-03 campaign. Any future physical campaign also
requires separate explicit authorisation.
