# O5-R5 — Physical HM-01, HM-02 and HM-03 Measurement Campaign Report

## Disposition

- Date: 2026-07-24.
- Authorised baseline: `505f098e710aceadf78548d20a0a139a2d211df4`.
- Automatic result: `BLOCKED`.
- `HM-01`: `NOT TESTED`.
- `HM-02`: `NOT TESTED`.
- `HM-03`: `NOT TESTED`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.
- Source, test, executable configuration and dependency changes: none.
- Restore, download, installation, external access, provider, database, corpus and credential:
  not used.

The authorised campaign stopped before starting a workload. The approved fail-closed rule requires
an immediate stop when the profiler is unavailable, the environment is not reproducible, an SLO
cannot be applied or headroom cannot be decided. Three independent prerequisites were absent:

1. no existing O5-R5 runner exposes the physical boundaries and maximum admitted load required by
   `HM-01`–`HM-03`;
2. the frozen O5-R3 SLOs apply to synthetic incident phases, not to the physical first-byte, idle,
   cancellation, control-update, memory-headroom or CPU-per-work-unit measurements;
3. the installed tooling cannot produce and analyse the required .NET 10 heap, allocation and
   phase-labelled evidence without a new runner, installation or both.

Executing the ordinary test suites repeatedly would therefore have produced test-host timings
rather than the declared measurements. Those numbers would not be comparable, attributable to
the required phases or suitable for a headroom decision.

## Declared environment observed

The candidate host declaration from O5-R1 was reproducible at the inventory boundary:

| Dimension | Observed value |
|---|---|
| Operating system | Windows 11 Enterprise `10.0.26200`, x64 |
| Processor capacity | 4 physical cores, 8 logical processors |
| Installed memory | approximately 15.8 GiB |
| .NET SDK selected by the repository | `10.0.301` |
| .NET host runtime | `10.0.10`, x64 |
| Repository baseline | exact and clean |
| Project runtime before assessment | zero process and zero owned listener |

This inventory does not prove workload isolation or measurement reproducibility. Those properties
require the missing bounded runner and preregistered measurement protocol.

## Prerequisite assessment

| Requirement | Result | Evidence |
|---|---|---|
| Existing O5-R5/HM runner | `BLOCKED` | No `O5R5`, `O5-R5`, `HM-01`, `HM-02` or `HM-03` executable reference exists under `src/`, `tests/` or `scripts/`. |
| Maximum admitted workload | `BLOCKED` | Deterministic resource contracts exist, but no existing runner materialises and labels the physical parse, cryptography, sort, analysis, first-byte, idle and control-update phases. |
| Applicable physical SLOs | `BLOCKED` | O5-R3 freezes detection `1,000 ms`, containment `3,000 ms`, recovery `8,000 ms` and closure `15,000 ms` for synthetic incident timelines. It does not freeze thresholds for the HM measurements. |
| Memory headroom rule | `BLOCKED` | No approved percentage or absolute headroom threshold maps empirical heap, working set and allocation peak to the versioned accounted-memory formula. |
| CPU/work calibration rule | `BLOCKED` | No approved per-phase CPU/elapsed threshold maps parse, cryptography, sort and analysis to deterministic work units. |
| .NET physical profiler chain | `BLOCKED` | `dotnet-counters`, `dotnet-trace`, `dotnet-gcdump`, PerfView and Windows Performance Analyzer are not installed. |
| Windows Performance Recorder | `INSUFFICIENT` | WPR and its .NET profile are available, but no supported analyser or phase-labelled runner is available. Capturing an unanalysable ETL would not meet the gate. |
| Generic Windows counters | `INSUFFICIENT` | Generic CLR counter sets exist, but without a .NET 10 scenario runner they cannot attribute measurements to the required phases or decide allocation peak and headroom. |

## HM disposition

### HM-01 — latency and control liveness

`NOT TESTED`. No authorised existing runner can timestamp real first-byte, inter-read idle,
cancellation observation and control-update scheduling/commit while holding the exact maximum
admitted evaluation load. No latency distribution, percentile, worst case or SLO comparison was
created.

### HM-02 — empirical memory

`NOT TESTED`. The deterministic accounted-memory formula remains valid as an admission contract,
but no runner and profiler chain can currently correlate heap, working set and allocation peak
with cold/warm scenarios. No headroom value was inferred.

### HM-03 — CPU and deterministic work

`NOT TESTED`. The product continues to account deterministic work units, but no existing runner
labels parse, cryptography, sort and analysis boundaries for physical CPU and elapsed-time
measurement. No time-derived admission rule or calibration was introduced.

## Preserved safety properties

- `ActivationState=None` remained unchanged.
- No Observer evaluation or publication was started.
- No source, configuration, test, fixture, dependency, package or lockfile changed.
- No profiler trace, workload, build or test suite was started after the stop condition.
- No provider, database, corpus, credential, telemetry, LLM, recommendation, command or automation
  was used.
- No result was presented as a physical pass, production evidence or provider homologation.

## Required remediation before repeating O5-R5

A separately authorised bounded remediation lot must:

1. preregister physical thresholds and a conservative memory-headroom rule that are distinct from
   the synthetic O5-R3 incident SLOs;
2. add or approve one test-only runner that exposes the exact HM phase boundaries, maximum admitted
   load, deterministic work units and cold/warm protocol;
3. use an approved local measurement chain capable of heap, allocation, working-set, CPU and
   elapsed-time attribution without external access;
4. prove that the runner cannot enter normal composition, publish Observer output or alter
   `ActivationState`;
5. define repetitions, warm-up, outlier handling, percentiles, variance, stop conditions and
   sanitised output before observing results.

Only after that remediation is automatically and humanly accepted may O5-R5 be repeated.

## Cleanup

The assessment started no project runtime, profiler, trace or workload and created no temporary
measurement root. Final cleanup confirmed zero project process, listener, dedicated profile and
O5-R5 measurement artefact.

## Next decision

O5-R5 is `BLOCKED`, not failed and not complete. The next eligible step is a concise proposal for a
separately authorised O5-R5-A measurement-readiness remediation lot. O5-R6, operational data,
provider/database access, `OBSERVER` activation and lifecycle transition remain unauthorised.
