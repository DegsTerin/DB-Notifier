# O5-R5 — Physical Measurement Campaign Repetition Report

## Disposition

- Date: 2026-07-25.
- Authorised source baseline: `45ade378ef8e5468a45e9507a77f25229327b29a`.
- Execution baseline after the authorised Human Gate record:
  `5a63245`.
- Automatic result: `BLOCKED`.
- `HM-01`: `NOT TESTED`.
- `HM-02`: `NOT TESTED`.
- `HM-03`: `NOT TESTED`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.
- Source and configuration changes: none.
- Restore, installation, download, external access, provider, database and corpus: not used.

The repetition stopped before any physical workload or counter capture. The approved O5-R5-A
source contains a built-in physical counter source and a coordinator for one supplied operation,
but it contains no executable physical campaign entrypoint and no physical workload definitions
for the eight frozen phases.

Executing physical measurements would therefore require new test code, a driver or another
implementation change. The authorisation explicitly required the campaign to stop rather than
create that missing path.

## Executable inventory

| Check | Observed result |
|---|---|
| `O5R5DotNetMeasurementSource` definition | present once |
| Physical-source construction or instantiation | `0` |
| Existing O5-R5-A executable tests | `7`, all synthetic |
| Existing physical campaign test/entrypoint | absent |
| Physical first-byte/idle/control operation | absent |
| Physical parse/cryptography/sort/analysis workload | absent |
| Result sink for the frozen 5/30 campaign matrix | absent |

The existing runner accepts a delegated operation, so it can coordinate and judge a sample only
after a caller supplies that operation. The approved synthetic tests pass artificial snapshots and
never construct `O5R5DotNetMeasurementSource`. Test discovery confirmed the only seven executable
O5-R5-A cases are the previously accepted synthetic boundary and isolation tests.

Reflection, an ad-hoc shell workload or reinterpretation of ordinary test-host timing was rejected
because none would be the approved phase-labelled campaign.

## HM disposition

### HM-01 — latency and control liveness

`NOT TESTED`. There is no executable physical first-byte, idle, cancellation or control-update
scenario bound to the approved runner.

### HM-02 — heap, allocation and working set

`NOT TESTED`. The physical source was not instantiated and no workload boundary exists from which
to capture cold/warm memory evidence.

### HM-03 — CPU and deterministic work

`NOT TESTED`. No physical parse, cryptography, sort or analysis operation maps deterministic work
units to the runner.

## Preserved safety and cleanup

- No physical source was instantiated.
- No workload, counter capture, profiler, trace, test case or Observer publication ran.
- No product, test, configuration, dependency or lockfile changed.
- `ActivationState=None` and normal dormant composition remained unchanged.
- No process, listener, profile or temporary physical-campaign artefact remained.

## Required remediation

The next bounded remediation must add one marker-gated physical campaign driver in the existing
test assembly. It must:

1. instantiate the existing built-in physical source only under the exact test-only marker;
2. define bounded physical operations for all eight frozen phases;
3. execute the exact 5 warm-up and 30 measured samples for cold and warm classifications;
4. emit only sanitised phase-labelled evidence bound to the frozen protocol digest;
5. preserve serial admission, cancellation, fail-closed resource limits and zero normal-composition
   reference;
6. remain dormant unless a separately authorised physical campaign explicitly opts in.

This is an implementation change and requires separate authority. The blocked repetition does not
invalidate the synthetic O5-R5-A boundary evidence, but it shows that readiness was incomplete for
direct execution.

## Next decision

The next eligible step is a concise O5-R5-B proposal for the missing physical campaign driver.
O5-R6, data/provider/database access, Observer activation and lifecycle transition remain
unauthorised.
