# DB-Notifier Continuous-Improvement Backlog

- Snapshot date: `2026-08-30`
- Corpus version: `9.1.0`
- Controller schema: `1`
- Lifecycle: `STATE-06 INTEGRATION` (unchanged)
- Authority: [`../governance/Continuous-Improvement.md`](../governance/Continuous-Improvement.md)

## Status contract

This file is a factual queue snapshot, not an authority or live execution
ledger. New observations receive a stable rule ID and controller-derived
fingerprint. A row is updated only from validated evidence; prior failures
remain in the append-only state history.

Allowed statuses are `DETECTED`, `TRIAGED`, `CANDIDATE`, `QUARANTINED`,
`EXTERNAL_PREREQUISITE`, `BLOCKED_BY_HIGHER_AUTHORITY`, `OBSERVING` and
`CLOSED`. Severity is `P0` through `P3`. Ordering is severity, then immutable
first-observed order. A duplicate records its canonical fingerprint instead of
creating another row.

## Current queue

| Key | Stable fingerprint | Severity | Status | Owner role | Factual disposition |
|---|---|---:|---|---|---|
| `DBN-CI-0001` | `54be61fe06e66578c777c337dedfbdde5c85e4974ecef8e8b4e3fcc82b18ff8f` | P1 | `CANDIDATE` | `IMPLEMENTER` | Corpus `9.1.0`, controller schema `1`, recursive strict JSON, recoverable pending append, authorised-root and candidate containment, full additive risk, factual candidate-domain causal receipts, structured P0-P3 review evidence, bound metrics and Git-proven promotion/rollback are materialised. The latest successor recomputes every execution key from declared improvement, baseline, scope and acceptance inputs during append, recovery, admission and full replay; it also owns the exact 28-path official governance matrix, including the 11 prior false `STANDARD` paths, while retaining non-authority product controls as `STANDARD`. Behavioural `140`, development-flow `112`, complete verifier `160`, five-file AST and `1007` links in `233` files pass. Reviews `P0=0`, `P1=5`, `P2=2`, `P3=0`, then `P0=0`, `P1=3`, `P2=0`, `P3=0`, and finally `P0=0`, `P1=2`, `P2=0`, `P3=0` remain preserved while new independent re-review and promotion remain pending. |
| `DBN-CI-0002` | `82c38485a9ed8be5077b23dca9fd37a20213b10df1fd2cad6558053d81bba858` | P1 | `CLOSED` | `REVIEWER` | The initial autonomous-delivery review preserved P0=0, P1=3, P2=1, P3=0; its successor review passed with zero P0/P1. Both results remain in the state history. |
| `DBN-CI-0003` | `0613fd8b9a3c9397fddba98d2e90b692a1f5f3b31f0835ed8e5e5302bacfa7d2` | P1 | `QUARANTINED` | `ROOT_CAUSE_ANALYST` | The principal-worktree canonical verifier is blocked by protected predecessor matrix drift. This governance candidate neither retries nor modifies that WIP; exact clean-candidate validation is isolated. |
| `DBN-CI-0004` | `f70f91c883c33be6a3d949dc3dbbdc3d8116d06f6f0e3695f3ca61a0fda5dd07` | P2 | `EXTERNAL_PREREQUISITE` | `VERIFIER` | Entry `Doctor` found product dependencies unrestored in the fresh isolated worktree. Governance-only tests remain executable; product validation is not relabelled as run or passed. |

## Admission rules

- `DBN-CI-0001` may advance only with the exact candidate path set, clean
  index, preserved baseline, behavioural pass, complete policy pass and
   independent zero-P0/P1 review. Caller-selected ledger roots, duplicate JSON,
   non-factual causal hashes and review summaries not projected from individual
   review events cannot satisfy admission. A recovered receipt cannot satisfy a
  different requested event, and a failed gate requires a disjoint verifier
   plus factual applicable old/new results with at least one `FAIL`. Every
   execution identity must replay from its declared four inputs, and every row
   in the exact official 28-path matrix must retain derived `HIGH` governance.
- `DBN-CI-0003` cannot be retried from this candidate. Its successor requires
  separate authority over the protected predecessor WIP and a distinct causal
  delta.
- `DBN-CI-0004` does not block governance-only validation. It blocks claims
  about restored product build, product tests or runtime readiness.
- A row cannot become `CLOSED` from a mutable dashboard, manual relabelling,
  duplicate suppression, open observation window or absent evidence digest.

## Next bounded event

Dispatch the mechanically frozen exact 17-path `DBN-CI-0001` candidate to
independent read-only successor review. Promotion, observation and rollback
custody remain outside the implementation writer role.
