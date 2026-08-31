# DB-Notifier Continuous Improvement

- Status: normative thematic authority
- Revision: `1.0.0`
- Corpus version introduced: `9.1.0`
- Controller schema: `1`
- Project: `DB-Notifier`

## Purpose and authority

This document owns the DB-Notifier continuous-improvement control loop. It
specialises, but does not replace, [`Governance.md`](Governance.md),
[`Quality-Gates.md`](Quality-Gates.md) or
[`Conversation-Coordination-Prompt.md`](Conversation-Coordination-Prompt.md).
The factual queue is
[`../state/Continuous-Improvement-Backlog.md`](../state/Continuous-Improvement-Backlog.md)
and the executable reference is
[`../../scripts/continuous-improvement.ps1`](../../scripts/continuous-improvement.ps1).

The loop may autonomously find, triage, implement, verify, promote, observe and
route local improvements within an already authorised objective. It does not
create product requirements, widen a write set, access a protected boundary,
waive clean-room or licence constraints, supply credentials, authorise an
external action, change lifecycle state, publish, deploy, push or perform an
unsafe destructive operation.

Historical Human Gates, failures and decisions remain immutable evidence. New
engineering decisions use objective Agent Gates without owner-mediated
copy-and-paste or routine approval.

## Event-driven bounded loop

The controller consumes one already-recorded event set and returns at most one
next action. It never polls, sleeps, recursively invokes itself, edits product
code, starts a runtime or retries a tool call. New work begins only when a new
immutable event exists.

The maximum ledger size and attempt budget are explicit inputs. Defaults are
`512` events per ledger and `3` distinct candidate attempts per execution key;
both fail closed when exceeded. Every invocation is finite and independently
auditable.

The canonical event path is:

```text
FINDING_DETECTED -> TRIAGED -> ATTEMPT_STARTED -> REVIEW_RECORDED
-> REVIEW_RECORDED -> GATE_PASSED -> PROMOTED
-> OBSERVATION_PASSED -> CLOSED
```

Failure paths are explicit:

```text
ATTEMPT_STARTED -> GATE_FAILED -> causal analysis -> distinct successor attempt
GATE_FAILED -> QUARANTINED
PROMOTED -> OBSERVATION_FAILED -> ROLLBACK_REQUIRED -> ROLLED_BACK -> CLOSED
```

`EXTERNAL_PREREQUISITE_RECORDED` and `AUTHORITY_BLOCKED` are terminal routing
events, not substitutes for a failed gate. `DUPLICATE_SUPPRESSED` records an
observation without changing the canonical finding state.

## Stable identities and idempotency

A finding fingerprint is lowercase SHA-256 over a versioned canonical payload
containing:

- the stable uppercase rule ID;
- the normalised root-cause statement;
- the sorted, unique, case-normalised repository-relative affected paths.

Presentation noise, slash direction, path order and case do not create a new
finding. A different root cause or affected path set does.

An execution key binds the finding fingerprint to the exact baseline, execution
scope digest and acceptance digest. Every event carrying that identity stores
all three fields. Append admission, pending recovery, attempt admission and
full-ledger replay independently recompute the key from the event's declared
improvement, baseline and input digests; an arbitrary but well-formed SHA-256,
a missing input or identity drift fails with `EXECUTION_KEY_IDENTITY_INVALID`
or `EXECUTION_KEY_DRIFT`. Exact pending replay remains idempotent because the
recovered event and the requested canonical event must have the same event hash.
A dispatch key binds the execution key to the exact route, source and
destination. The same logical dispatch reuses the same key; an uncertain
receipt is reconciled before any retry.

## Append-only ledger

The ledger is strict UTF-8 without BOM, one compact JSON object per
LF-terminated line. BOM-based encoding detection is prohibited and fails
closed. Before object conversion, the parser recursively rejects duplicate
property names in ledger events, pending journals and embedded pending events;
last-property-wins conversion is never an integrity rule.
Each event has a monotonically increasing sequence, canonical UUID and UTC
timestamp, exact baseline, actor and role, result, reason code, evidence
digests, the preceding event hash and its own SHA-256 hash. Property order is
part of schema `1`.

An append accepts only an explicit `.jsonl` regular-file path physically
contained below the exact `AuthorisedRoot`; every existing root, parent, file
and deterministic sidecar component contains no reparse point. The command-line
controller confines that root to the repository-ignored
`.dotnet/continuous-improvement/` evidence directory; behavioural tests use an
explicitly owned disposable root. Under one
exclusive ledger lock, it validates the entire bounded chain, constructs one
next event, writes and flushes a content-addressed `.pending.new` journal, then
atomically renames it to `.pending` before changing the ledger. The journal
binds the exact ledger path, prior length and digest, complete event bytes,
event hash and its own integrity hash.

Every append or explicit recovery reconciles a pending journal before any new
event. A valid journal applies a missing event once, removes an already-applied
journal without duplication, or replaces a hash-matched truncated event prefix
with the complete event. A corrupt, mismatched, unbounded, reparse-backed or
unexpected path fails closed and remains available for quarantine evidence.
After locked recovery, the append returns the recovered receipt only when the
canonical requested event replays to that exact event and predecessor. A
distinct requested event is constructed and appended next under the same lock;
recovery never reports event A as successful completion of requested event B.
Ordinary reads never repair state: they return `PENDING_APPEND_RECOVERY_REQUIRED`
until explicit locked recovery succeeds. Cleanup targets only the exact
validated sidecar after the complete ledger and hash chain validate.

Lock collision records `MUTABLE_RESOURCE_COLLISION`; it is never resolved by
last-write-wins. A modified, reordered, deleted, duplicated, malformed, CRLF,
BOM-bearing or unjournalled partial event invalidates the ledger.

The committed repository does not contain a live operational ledger. A task
must place its ledger in an explicitly owned, non-product evidence location
inside its authorised worktree or disposable test root. `LedgerPath` cannot
select or create a sibling of that authorised root. Ledgers can contain
only sanitised digests and canonical identities, never secrets or raw external
payloads. Every ledger command requires an explicit `-LedgerPath`; there is no
implicit repository write location.

## Specialised roles and separation

Use the smallest useful set of these roles:

- `SCOUT`: records observations and stable fingerprints;
- `ROOT_CAUSE_ANALYST`: establishes a causal hypothesis and delta;
- `IMPLEMENTER`: owns one exact isolated candidate;
- `VERIFIER`: executes deterministic mechanical checks;
- `REVIEWER`: performs semantic P0-P3 review;
- `INTEGRATOR`: promotes or restores an exact revision;
- `OBSERVER`: closes the post-promotion observation window.

Implementers and reviewers are disjoint for the same candidate. Each reviewer
records its own `REVIEW_RECORDED` event, exact result, SHA evidence and
controller-derived review receipt; a caller-supplied reviewer list or receipt
is not evidence. A governance or high-risk change requires at least two such
independent passing receipts. Every review structurally records P0-P3 counts
and findings. P2/P3 findings require a candidate-, reviewer- and
execution-bound disposition decision plus SHA evidence; P0/P1 findings cannot
be disposed and remain blocking. A gate summary is recomputed from the factual
review events, and promotion requires zero P0 and zero P1. Its verifier and integrator are disjoint from its
implementers and reviewers, and its observer is also disjoint from the exact
promotion integrator. One agent may perform multiple sequential roles only
across different candidates where no independence claim is made.

Verifier disjointness applies identically to `GATE_FAILED` and `GATE_PASSED`.
`GATE_FAILED` preserves literal `FAIL` and at least one applicable failed old or
new check. A governance gate retains both old and new results as `PASS` or
`FAIL` and rejects `PASS`/`PASS`; a non-governance failure retains
`NOT_APPLICABLE` for the old gate and `FAIL` for the new gate.

## Attempt admission and quarantine

Every `ATTEMPT_STARTED` binds one execution key, one exact candidate digest and
a controller-derived candidate-scope digest. Canonical candidate paths are
stored in every candidate event and replayed with the classification; a
re-hashed event cannot omit a path-derived domain. Candidate paths and canonical
additional risk domains derive `governanceChange`, `riskClass` and the complete
risk-domain set. Paths under security, persistence, migration, architecture,
compatibility, release, external-operation, destructive-operation,
rights/licensing or governance boundaries are high risk. Caller additions can
raise but never lower derived risk; legacy `ContractRiskFacts`, caller-supplied
classification, `false` and `STANDARD` are not authority. The first
execution key remains immutable for the finding chain and cannot be changed to
reset identity or budget. The same candidate digest can never be
attempted twice, even when a caller changes its label, timestamp or dispatch
route.

At corpus `9.1.0`, the controller-owned minimum governed-path matrix contains
exactly these 28 repository-relative paths. Every row derives `riskClass=HIGH`,
`governanceChange=true` and `GOVERNANCE_POLICY`; generic domain rules may raise
additional paths but cannot lower any row:

| Official authority, state or development-control path |
|---|
| `AGENTS.md` |
| `PLANS.md` |
| `prompts/Start-Here.md` |
| `prompts/foundation/Prompt-New-Project.md` |
| `prompts/foundation/Solution-Architecture-Document.md` |
| `prompts/foundation/AIOps-And-AI-Module.md` |
| `prompts/governance/Continuous-Improvement.md` |
| `prompts/governance/Conversation-Coordination-Prompt.md` |
| `prompts/governance/Governance.md` |
| `prompts/governance/Language-Policy.md` |
| `prompts/governance/Lifecycle.md` |
| `prompts/governance/Quality-Gates.md` |
| `prompts/governance/Security-And-Access.md` |
| `prompts/operations/Operational-Playbooks.md` |
| `prompts/state/Current-State.md` |
| `prompts/state/Continuous-Improvement-Backlog.md` |
| `prompts/state/State-Transition-Log.md` |
| `prompts/system/AI-Software-Engineering-Master-Prompt.md` |
| `prompts/system/Prompt-System-Change-Log.md` |
| `prompts/templates/Templates.md` |
| `docs/Code-Documentation-Standards.md` |
| `docs/design/DB-Notifier-Design-System.md` |
| `scripts/development.ps1` |
| `scripts/ci.ps1` |
| `scripts/continuous-improvement.ps1` |
| `scripts/verify-development-flow.ps1` |
| `tests/DBNotifier.ContinuousImprovement.Tests.ps1` |
| `tests/DBNotifier.DevelopmentFlow.Tests.ps1` |

Ordinary non-authority product paths remain `STANDARD` when no independent
security, persistence, migration, architecture, compatibility, release,
external-operation, destructive-operation or rights/licensing domain applies.
The behavioural suite verifies all 28 rows, marks the 11 formerly missed rows
as explicit counterexamples and retains a non-authority product control group.

An initial attempt is admitted only when the exact latest event for its finding
is `TRIAGED`. After an attempt, a successor is admitted only when the exact
latest event is its `GATE_FAILED`, its candidate digest differs and a new
controller-derived causal-delta receipt replays. That receipt binds the failed
gate event and attempt, execution key, failed and successor candidates,
changed-fact domain and digest, and the canonical retry decision. Schema `1`
admits a candidate-domain change only when the changed-fact digest is the exact
successor candidate digest; dependency, environment and control-plane claims
remain quarantined until a separately governed factual receipt event exists.
An arbitrary SHA, raw digest, arbitrary GUID, missing or reused delta,
same-candidate retry, latest `ATTEMPT_STARTED` or exhausted attempt budget
routes to `QUARANTINED`. Changing whitespace, regenerating evidence or
repeating the same command is not a causal delta.

## Exact candidate and meta-gate

Before a gate, the verifier proves the exact worktree root, exact baseline,
empty index when required, complete non-ignored changed-path set and a digest
over every expected regular file. Any extra or missing path records
`SCOPE_OVERLAP`; a different root records `ISOLATION_FAILURE`; a different HEAD
records `BASELINE_DRIFT`; casing that differs from Git or a path whose physical
resolution escapes the root or crosses a reparse point fails before hashing.
Ignored or protected overlays are not candidate
inputs and must not be opened merely to prove exclusion.

For a governance change, the unchanged baseline validator is the old gate and
the candidate validator plus new behavioural regressions is the new gate. Both
must preserve literal `PASS`, and two independent reviewers must return zero
P0 and zero P1. A new gate cannot redefine an old failure as success.

## Promotion, observation and rollback

Promotion resolves the supplied references in the exact repository and binds
the passing execution key and candidate digest to a real candidate Git commit
and tree, a different real last-known-good (`LKG`) commit and tree, the exact
promotion preimage commit and tree, one promotion UUID and evidence digests.
The baseline, `LKG` and preimage must identify the same commit and tree; opaque
40-character strings are not revisions. Only an independent `INTEGRATOR`
records `PROMOTED`.

The `LKG` does not advance on promotion. An independent `OBSERVER` closes the
exact promotion window using the same promotion UUID, execution key, candidate
digest, candidate revision and prior `LKG`. Only `OBSERVATION_PASSED` permits a
later governed advancement of `LKG`.

`OBSERVATION_FAILED` records literal `FAIL` and routes the unchanged promotion
identity and exact prior `LKG` through `ROLLBACK_REQUIRED`. `ROLLED_BACK`
resolves a real rollback commit and tree, requires that tree and revision to be
the prior Git-proven `LKG`, and preserves the same identities, an independent
integrator, a passing post-rollback gate and evidence. Rollback is a governed
compensating action; destructive reset and loss of protected work remain
prohibited.

## Anti-gaming metrics

Metrics are recomputed only from a fully validated ledger. The metrics envelope
and every rate bind the profile and version, validated-ledger source and head
hash, exact sequence/timestamp window, direction and unit. Every rate also
exposes an integer numerator, integer denominator and a ratio; a zero denominator
produces `null`, never zero or one.

- First-pass rate counts only unique findings whose first gate result exists;
  unfinished findings are excluded from its denominator.
- Escape rate counts failed, closed observation windows over all closed
  observation windows; unobserved promotions are excluded.
- Observation coverage counts closed observation windows over promotions.
- Duplicate rate counts suppressed duplicate observations over canonical
  findings plus suppressed duplicates.
- Raw counts retain findings, suppressions, attempts, quarantines, promotions,
  observed promotions and rollbacks.

Mutable dashboards, caller-supplied aggregates, deleted findings, relabelled
retries and open observation windows cannot become evidence or improve a rate.

## Required gates and stop conditions

Before every applicable technical action, execute the canonical shutdown
preflight with PowerShell 7 and stop on a non-zero result. A candidate requires:

1. stable finding, execution and dispatch identities;
2. one writer and an exact write set;
3. behavioural controller regressions;
4. PowerShell AST and explicit-link validation;
5. the old and new policy gates for governance changes;
6. independent P0-P3 review;
7. exact candidate proof before promotion;
8. an observation window and exact rollback record after promotion.

Stop on `AUTHORITY_MISMATCH`, `BASELINE_DRIFT`, `SCOPE_OVERLAP`,
`ISOLATION_FAILURE`, `MUTABLE_RESOURCE_COLLISION`, `GATE_FAILURE`,
`SAME_CANDIDATE_RETRY`, `EXECUTION_KEY_DRIFT`, `CAUSAL_DELTA_REQUIRED`,
`EXECUTION_KEY_IDENTITY_INVALID`,
`CALLER_SUPPLIED_CAUSAL_DELTA`, `CALLER_SUPPLIED_CLASSIFICATION`,
`PHANTOM_REVIEWER_REJECTED`, `OPAQUE_REVISION_REJECTED`,
`PENDING_APPEND_RECOVERY_REQUIRED`, `PENDING_APPEND_CORRUPT`,
`DUPLICATE_JSON_PROPERTY`, `PATH_OUTSIDE_AUTHORISED_ROOT`,
`CANDIDATE_PATH_CASING_MISMATCH`, `REVIEW_DISPOSITION_REQUIRED`,
`UTF8_BOM_FORBIDDEN`, `ATTEMPT_PREDECESSOR_INVALID`,
`ATTEMPT_BUDGET_EXHAUSTED`, `EXTERNAL_PREREQUISITE` or
`BLOCKED_BY_HIGHER_AUTHORITY`. Preserve the first factual failure and continue
only independent safe work.

## Validation

The behavioural contract is executable through
[`../../tests/DBNotifier.ContinuousImprovement.Tests.ps1`](../../tests/DBNotifier.ContinuousImprovement.Tests.ps1).
The canonical policy gate invokes it once. Policy changes update the corpus
changelog, factual backlog and current state only after their evidence exists;
they never rewrite historical failures.
