# DB-Notifier Live Execution Plan

<!-- Purpose: Maintains the non-authorising execution ledger for the current broad engineering increment. -->

This file is the canonical live plan for the current broad or cross-cutting
increment. It records execution intent and evidence; it does not replace
`AGENTS.md`, the instruction corpus, an accepted ADR, `Current-State.md`, the
append-only history, a Quality Gate or a Human Gate.

## Control record

- Plan ID: `DEV-FLOW-01`
- Status: `PARTIAL`
- Created: `2026-08-27`
- Initial baseline: `main@f0f220c539fde685e2c500b4944ebca168aaec7d`
- Lifecycle state: `STATE-06 INTEGRATION`; unchanged by this plan
- Authority: Bruno's request to implement in DB-Notifier the development
  method and flow used by RAG-Challenge
- Execution mode: `SINGLE_OWNER`
- Writer: coordinating conversation only
- Independent reviewers: three read-only discovery lanes before implementation
  and three named read-only recheck lanes after implementation
- Factual-state owner: `prompts/state/Current-State.md`
- Historical owner: `prompts/state/State-Transition-Log.md`

## Closed task envelope

- Envelope ID and version: `DEV-FLOW-01/v1`
- Envelope status: `BLOCKED`
- Exact human authority: `Quero que implemente no DB-Notifier o mesmo método
  e fluxo de desenvolvimento do RAG-Challenge`
- Non-waivable limits: DB-Notifier security, clean-room, lifecycle, Quality
  Gate, Human Gate, Git and external-action controls remain in force.
- Owning state and lot: `STATE-06 INTEGRATION`; cross-cutting development-flow
  increment `DEV-FLOW-01`.
- Workspace and frozen baseline: `C:\Projects\DB-Notifier`, branch `main`,
  commit `f0f220c539fde685e2c500b4944ebca168aaec7d`.
- Initial repository state: index and tracked worktree clean; the untracked
  `mysql-notifier-1.1.8-src/` tree was present and was classified as protected,
  unread third-party material.
- Verifiable objective: provide one DB-native, documented and mechanically
  enforced method from preflight and planning through local feedback,
  canonical validation, evidence, review and hand-off.
- Frozen contracts and dependencies: the three references in the provenance
  table below; .NET SDK `10.0.302`, Node.js `24.18.0`, npm `11.16.0`, tracked
  NuGet lock files and the Dashboard npm lock file.
- Execution topology: `SINGLE_OWNER`. The coordinating conversation is the
  only writer; every delegated lane is read-only.
- Independent reviewers: `Dirac` reviews method equivalence, `Turing` reviews
  PowerShell and runner safety, and `Schrodinger` reviews governance and state
  reconciliation. They do not write or integrate their own findings.
- Expected evidence: deterministic policy-test counts, applicable repository
  check results, independent findings, a reconciled factual record and one
  focused local commit. Evidence must omit secrets, real host names and the
  contents of the protected third-party tree.
- Rollback strategy: before commit, reverse only this increment's owned diff;
  after commit, use a separately authorised focused revert. Never discard or
  rewrite pre-existing user work or append-only history.
- Final disposition: `PARTIAL`; it may be only `PASS`, `FAIL`, `BLOCKED`,
  `PARTIAL` or `NOT_RUN`.

### Artefact classification and exclusive writers

| Class | Paths or logical artefacts | Exclusive writer |
|---|---|---|
| `AUTHORITY` | `AGENTS.md`; routed governance, lifecycle, quality, coordination, playbook, template and master-prompt amendments | Coordinating conversation |
| `CURRENT_FACT` | `prompts/state/Current-State.md` | Coordinating conversation |
| `HISTORY` | `prompts/state/State-Transition-Log.md`; `prompts/system/Prompt-System-Change-Log.md` | Coordinating conversation, append-only where required |
| `PLAN` | `PLANS.md` | Coordinating conversation |
| `IMPLEMENTATION` | `.gitignore`, `.nvmrc`, `.github/workflows/ci.yml`, `scripts/`, `tests/`, `README.md`, `docs/Development.md` | Coordinating conversation |
| `EVIDENCE` | Sanitised command output and independent read-only review findings retained in the conversation and factual records | Coordinating conversation |
| `GENERATED` | Build, test, coverage and runtime outputs, if a gate can run | Owning checked-in runner; never edited manually |

### Mutable-resource matrix

| Resource | Owner | Readers or boundary |
|---|---|---|
| All tracked paths in this increment | Coordinating conversation | Review lanes are read-only |
| Git index, branch and final commit | Coordinating conversation | No branch, worktree, amend, rebase, merge, push or publication |
| Project processes, listeners and runtime ports | Canonical runner while active | None may exist before a technical action; no product runtime is intentionally left running |
| Runner diagnostics and temporary profiles | The runner that creates them | Bounded, sanitised and cleaned by the owning runner |
| NuGet and npm caches | Package managers during an authorised `Setup` or gate | Current implementation work does not mutate them |
| `mysql-notifier-1.1.8-src/` | Owner-protected external material; no project writer | No read, inventory, gate traversal or tracking |

### Stop conditions

| Stop code | Objective trigger |
|---|---|
| `AUTHORITY_MISMATCH` | Requested work would waive a protected DB-Notifier authority or require an ungranted external, lifecycle, ADR or Human Gate decision. |
| `BASELINE_DRIFT` | Branch, frozen commit, tracked user work or a frozen contract changes outside this increment before integration. |
| `SCOPE_OVERLAP` | A proposed edit reaches protected external material, product behaviour or another writer's path. |
| `DEPENDENCY_UNREADY` | An exact pinned toolchain, locked dependency or required input is unavailable. |
| `ISOLATION_FAILURE` | A project-owned process, listener, runtime, temporary profile or output cannot be identified and isolated. |
| `MUTABLE_RESOURCE_COLLISION` | More than one writer would touch a path, contract, branch, cache, runtime or evidence set. |
| `GATE_FAILURE` | An applicable check returns a factual failure; there is no automatic retry or in-place correction. |
| `EXTERNAL_AUTHORITY_REQUIRED` | Installation, remote CI, provider/database action, deployment, publication or other external mutation is required. |
| `HUMAN_DECISION_REQUIRED` | An ADR, Human Gate, activation choice or other formal human decision becomes necessary; none belongs to the current envelope. |

## Source provenance and adaptation boundary

| Source | Frozen reference | Use |
|---|---|---|
| RAG-Challenge public development flow | `main@2154b311b4ba41d62f462e3cdb37bc360ee32ca4` | `Doctor`, `Setup`, `Quick`, `Full`, deterministic `PlanOnly`, isolated child process and canonical CI delegation |
| RAG-Challenge governed method | `codex/pdf1-internal-governance@31ac04ba53f2e94b305d4ca08eb2c6d23aab9f9a` | Live plan, closed task envelope, ownership, stop conditions, evidence and independent review |
| DB-Notifier | `main@f0f220c539fde685e2c500b4944ebca168aaec7d` | Product authority, lifecycle, security, shutdown preflight, 14-field hand-off and technical commands |

The two RAG-Challenge references are intentionally separate because its public
working tree contained uncommitted work and its governed corpus is maintained
in a distinct clean worktree. No uncommitted RAG-Challenge content is an input
to this increment.

## Capability disposition

| Capability | Disposition | DB-Notifier owner |
|---|---|---|
| Authority, baseline, positive and negative scope before execution | `ADOPTED` | Current request and `Governance.md` own authority; Git and `Current-State.md` own facts; this plan only records the frozen envelope |
| Live, evidence-bearing execution plan | `ADAPTED` | `PLANS.md`; never an authority source |
| Closed task envelope, ownership and stop codes | `ADAPTED` | `Conversation-Coordination-Prompt.md` and `Templates.md` |
| `Doctor`, `Setup`, `Quick`, `Full` and deterministic `PlanOnly` | `ADAPTED` | `scripts/development.ps1` and `docs/Development.md` |
| Canonical aggregate repository gate | `ADAPTED` | `scripts/ci.ps1` and `Quality-Gates.md` |
| Independent review and serial integration | `ALREADY_GOVERNED` | DB-Notifier coordination authority |
| RAG lifecycle, corpus, evaluation, cloud and public/private projection | `REJECTED` | Outside DB-Notifier product and governance scope |
| TypeScript worktree orchestrator and `.codex/agents` catalogue | `DEFERRED` | Requires a separate ADR and authority |
| Compact RAG hand-off contract | `REJECTED` | DB-Notifier retains its 14-field contract |

## Positive scope

- Introduce this tracked live plan and its mechanical policy verification.
- Add a single DB-native development entry point under `scripts/`.
- Add a canonical aggregate gate and make CI delegate to it.
- Preserve the existing .NET, legacy, Dashboard, browser, sandbox, coverage,
  dependency, documentation and secret responsibilities.
- Add deterministic policy tests that perform no restore, build, product
  runtime or network access; one bounded disposable PowerShell helper exists
  solely to prove shutdown-preflight blocking and cleanup.
- Reconcile the owning governance, playbook, templates, development guide,
  current factual state, corpus changelog and append-only history.

## Negative scope and protected work

- No product behaviour, provider, database schema, migration, UI, packaging,
  deployment, release, activation, lifecycle transition, ADR or Human Gate.
- No external database, service, browser profile, credential, network tunnel
  or infrastructure mutation.
- No RAG-specific state, corpus, qrels, citation, Render, OCI or provider
  contract.
- No TypeScript orchestration service, worktree automation or parallel writer.
- The pre-existing untracked `mysql-notifier-1.1.8-src/` tree is protected
  third-party reference material. It must remain unread, unmodified and
  untracked, and is excluded from project inventory through `.gitignore`.

## Definition of Ready

- [x] Repository instructions and routed authorities read.
- [x] Shutdown preflight observed zero DB-Notifier-owned process or listener.
- [x] DB-Notifier baseline and protected work identified.
- [x] Public technical and governed normative RAG-Challenge references frozen.
- [x] Positive scope, negative scope, ownership and review lanes recorded.
- [x] Existing CI responsibilities and repository-native commands inventoried.
- [x] Versioned envelope, artefact classes, mutable resources, rollback and
  objective stop conditions recorded.

## Definition of Done

- [x] Governance and implementation describe one consistent workflow.
- [x] `PlanOnly` is exact, deterministic and side-effect free.
- [x] `Doctor` is read-only and reports actionable prerequisites.
- [x] `Setup` uses locked restores and detects lockfile drift.
- [x] `Quick` is explicitly `NON_GATE` and excludes integration, coverage,
  online advisory audits, runtime and Human Gates.
- [x] `Full` delegates exactly once to the canonical aggregate gate.
- [x] Online-only checks are explicitly `NOT_RUN` during offline execution.
- [x] CI delegates to the same checked-in gate and retains Linux Dashboard
  compatibility evidence.
- [x] Existing project checks applicable to the change have factual outcomes.
- [x] An independent post-change review has no unresolved `P0` or `P1` finding.
- [x] State, append-only history and corpus changelog reflect the observed result.
- [x] One focused local commit contains only the authorised increment.

## Findings

| ID | Severity | Finding | Disposition |
|---|---|---|---|
| `DF-001` | `P1` | No canonical live plan existed. | Implement in this increment. |
| `DF-002` | `P1` | Local development and CI commands had no shared aggregate entry point. | Implement in this increment. |
| `DF-003` | `P1` | The task envelope and workflow invariants were textual only. | Add templates and policy tests. |
| `DF-004` | `P1` | Unignored external source could be traversed by current repository gates. | Exclude it without reading or tracking it. |
| `DF-005` | `P2` | The current factual snapshot predates the repository baseline used here. | Reconcile without claiming product revalidation. |
| `DF-006` | `P2` | The RAG TypeScript orchestrator would introduce architecture and state not yet decided for DB-Notifier. | Defer to a separately authorised ADR. |
| `DF-007` | `P1` | A generic full test run could inherit physical-campaign markers, connection values or product configuration from the caller. | Run both entry points through a shell-free child and remove hazardous names/prefixes from its private environment without reading values or mutating the parent. |
| `DF-008` | `P1` | Case-sensitive `Stage` dispatch could accept lowercase `dashboard` and then execute `All`. | Dispatch the validated value case-insensitively and cover the source invariant. |
| `DF-009` | `P2` | Process-name-only ownership, an incomplete `Full` plan, workload update notification, and late secret scanning weakened the promised boundary. | Bind ownership to executable-path/command evidence, make the plan faithful, suppress offline workload notification and scan secrets before executable repository gates. |
| `DF-010` | `P2`/`P3` | Linux procfs could ignore a still-live unreadable process, and a 60-minute timeout lacked capacity evidence for gates made sequential. | Fail closed on still-present unreadable identities and bound the canonical job to 135 minutes using the former jobs' combined 125-minute envelopes plus orchestration margin. |

## Increment plan

| Increment | Scope | Status |
|---|---|---|
| `I1` | Protect external reference material and establish the live plan. | `COMPLETE` |
| `I2` | Implement shutdown assertion, development entry point, aggregate gate and policy tests. | `COMPLETE` |
| `I3` | Delegate CI and document the developer workflow. | `COMPLETE` |
| `I4` | Reconcile governance, templates and method ownership. | `COMPLETE` |
| `I5` | Validate, review independently, reconcile factual records and commit. | `COMPLETE` |

## Evidence log

| Date | Scope | Command or review | Result | Limitation |
|---|---|---|---|---|
| 2026-08-27 | Initial preflight | Process, command-line, parentage and listener inventory | `PASS` | No product runtime was started. |
| 2026-08-27 | Baseline | `git status`, `git rev-parse HEAD`, source provenance comparison | `PASS` | The protected external source tree was not inspected. |
| 2026-08-27 | Toolchain discovery | Local and system .NET/Node/npm version inspection | `BLOCKED` for full execution | Required .NET SDK `10.0.302`, Node `24.18.0` and npm `11.16.0` are not all available on the current host. |
| 2026-08-27 | Executable policy | Shutdown preflight, development-flow verifier and deterministic regression suite | `PASS`; `97` verifier assertions and `64` regression assertions | Policy evidence does not replace product build or runtime evidence. |
| 2026-08-27 | Existing script policy | Primary syntax `47` PowerShell/`14` Node; legacy syntax `27` PowerShell with `20` deliberate skips; runner-process `68`; syntax regressions `13` | `PASS` | No product compilation or runtime was inferred. |
| 2026-08-27 | Documentation and packaging diagnostics | Code documentation `437` files; Markdown `977` links in `227` files; bundle `-ValidateOnly` | `PASS` from the documented Dashboard working directory | An initial non-canonical npm invocation from the repository root returned `ENOENT`; it was an operator path error, not a gate result. |
| 2026-08-27 | Security and Git | Full available-history secret scan, `git diff --check` and `git fsck --full` | `PASS` | Unreachable dangling Git objects were informational; no integrity error was reported. |
| 2026-08-27 | `Doctor` | Isolated exact-prerequisite diagnostic | `BLOCKED`; `DEPENDENCY_UNREADY` | Repository layout, lock files and restored dependency readiness passed; exact .NET SDK `10.0.302` was absent. |
| 2026-08-27 | Canonical Dashboard offline diagnostic attempts | `scripts/ci.ps1 -Stage Dashboard -Offline` | `BLOCKED`; `DEPENDENCY_UNREADY` in every attempt | The latest attempt, after isolation hardening, passed preflight and the full available-history secret scan before Node.js `24.19.0` failed the `24.18.0` pin. No attempt produced or was converted to `PASS`. |
| 2026-08-27 | Independent review | Method, runner and governance lanes | `PASS` for integration review; `P0=0`, `P1=0`, no unresolved `P2`; two final method-review `P3` clarifications corrected | Review is static and does not replace the blocked executable gates. |

## Blockers and limitations

- The checked-in flow can be implemented and policy-tested locally, but
  `Doctor`, `Quick` and `Full` cannot be reported as passing until the exact
  pinned toolchains are available. The gate must report that fact rather than
  relaxing `global.json`, `.nvmrc` or `package.json`.
- Remote CI execution is not authorised by a local commit and is not evidence
  available in this increment.
- Product build, unit/integration tests, coverage, browser/runtime matrices and
  online dependency-advisory freshness remain `NOT_RUN` for this increment;
  policy and documentation diagnostics are not substitutes for them.

## Outcome and next action

The DB-native method and its implementation increments are complete, reviewed,
factually reconciled and bounded into the focused local commit that contains
this ledger. The technical disposition remains `PARTIAL`, not `PASS`, because
the pinned toolchains are unavailable and the product and runtime gates listed
above remain `NOT_RUN`. The next separately authorised interaction is isolated
toolchain provisioning followed by `Doctor`, `Quick` and one online `Full`
without relaxing repository pins.

## Change log

- `2026-08-27`: created from the authorised baseline after read-only DB-Notifier
  and RAG-Challenge audits; no lifecycle or product authority was inferred.
- `2026-08-27`: implementation and policy evidence recorded; the envelope was
  completed without converting the exact-toolchain blocker into a pass.
- `2026-08-27`: independent rechecks closed with zero unresolved `P0`, `P1` or
  `P2`; two final `P3` wording/help clarifications were corrected before the
  focused commit.
