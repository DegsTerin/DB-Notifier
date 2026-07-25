# PF-OBS-1-D1 — Failed Physical Evidence Retention Report

## Decision

`PF-OBS-1-D1` is automatically `APPROVED` within its local, test-only scope.
`ActivationState=None` remains unchanged. This result does not approve the physical campaign,
activate Observer or authorise a lifecycle transition.

## Cause corrected

The physical process already wrote a complete temporary report for either disposition. The pilot
wrapper checked the non-zero process exit before copying that report and then removed the owned
temporary root in cleanup. A failed campaign therefore retained only its generic terminal code.

## Implemented boundary

- Every absolute or repeatability breach now produces an allow-listed diagnostic containing only
  phase, cold/warm class, warm-up membership, repetition, metric, observed value, inclusive limit
  and unit.
- A campaign report aggregates those bounded diagnostics and preserves the exact first failing
  code without payload, path, user, machine identity, credential or database content.
- The writer validates protocol identity, `ActivationState=None`, membership, finite values,
  threshold direction, completeness and bounded cardinality before writing.
- Evidence is written through a write-through temporary file, flushed to disk and atomically
  moved over the exact destination. Corrupt, incomplete, linked, oversized and invalid evidence
  fails closed.
- The PF-OBS-1 wrapper now captures the physical exit code, validates and atomically commits the
  report to `artifacts/pf-obs-1/<run-id>/` before interpreting a failure and before removing the
  physical temporary root.
- A failed first campaign cannot be replaced. A passing first report and any failed or passing
  second report are retained under the same run identifier.

The frozen physical protocol remains version `pfobs1-physical-measurement-2.0.0` with SHA-256
`53F40F7DC72548EB488FFF729823BF0DCFD64EDD085314022C8E746CC45B5D71`. No SLO, workload,
sample count, resource limit or approval criterion changed.

## Automatic evidence

| Check | Result |
|---|---|
| Focused measurement and evidence tests | `13/13` passed |
| O5-R5 isolation and retention architecture tests | `9/9` passed |
| Complete integration suite | `109/109` passed |
| Complete architecture suite | `88/88` passed |
| Consolidated sandbox host Release build | passed, zero warnings and errors |
| PowerShell parser | passed |
| Changed-file .NET format verification | passed |
| Protocol v2 digest regression | passed |
| Corrupt JSON refusal | passed |
| Structurally incomplete report refusal | passed |
| Injected write failure and temporary cleanup | passed |
| Exact phase/sample/metric/value/limit round trip | passed |
| Normal composition references | zero |
| Code documentation, Markdown links and secret scan | passed |

No physical host counters or PostgreSQL runtime were used by the D1 automatic tests.

## Next authorised action

After this report and its focused commit, exactly one PF-OBS-1 resumption may run with the frozen
protocol v2. Any absolute, integrity, security, isolation or reproducibility failure must stop the
campaign, retain the exact report and prohibit a replacement attempt.
