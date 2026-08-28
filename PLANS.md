# DB-Notifier Live Execution Plan

<!-- Purpose: Maintains the non-authorising execution ledger for the current broad engineering increment. -->

This file is the canonical live plan for the current broad or cross-cutting
increment. It records execution intent and evidence; it does not replace
`AGENTS.md`, the instruction corpus, an accepted ADR, `Current-State.md`, the
append-only history, a Quality Gate or a Human Gate.

## Control record

- Plan ID: `S06-DFR-01`
- Status: `COMPLETE`
- Created: `2026-08-28`
- Initial baseline: `main@8e1fb1c4cee61b2bb2d1a67012d2a78db072ef47`
- Preserved predecessors: all first factual dispositions remain immutable;
  `GOV-2026-R1` remains `COMPLETE`,
  `AUD-2026-R1-R5-R2` remains `BLOCKED` by its rejected durable-capture
  wrapper before process creation,
  `AUD-2026-R1-R5-R1` remains `BLOCKED` by its unprovable sole focal result,
  while `AUD-2026-R1-R4` remains `BLOCKED` by its sole online `Full`
- Lifecycle state: `STATE-06 INTEGRATION`; unchanged by this plan
- Authority: Bruno's explicit clean-room authorisation to approximate the
  publicly documented MySQL Notifier Tray coverage and flows through the
  smallest coherent provider-neutral `STATE-06` read-only reconciliation lot,
  without consulting or adapting the protected external source tree
- Execution mode: `SEQUENTIAL_ONLY`
- Writer: coordinating conversation only
- Reviewer: the coordinating conversation performs frozen-diff, security,
  architectural and factual review; an independent semantic reviewer is not
  assigned under the current single-conversation authority
- Factual-state owner: `prompts/state/Current-State.md`
- Historical owner: `prompts/state/State-Transition-Log.md`

## Task envelopes

### Desktop Fleet read-only reconciliation `S06-DFR-01` — complete

- Envelope status: `COMPLETE`.
- Exact human authority: implement a strictly clean-room approximation of the
  publicly documented MySQL Notifier notification-area coverage and flows,
  preserving DB-Notifier's provider-neutral architecture, independent identity,
  existing authorities and `STATE-06 INTEGRATION` position. The protected
  external source tree must not be enumerated, searched, opened, read, moved,
  changed or used.
- Workspace and frozen baseline: `C:\Projects\DB-Notifier`, branch `main`,
  commit `8e1fb1c4cee61b2bb2d1a67012d2a78db072ef47`; index and tracked worktree
  were clean without enumerating untracked material.
- Initial shutdown evidence: the mandatory preflight returned `PASS`; no
  process was stopped, matching processes were `0` and owned listeners were
  `0`.
- Public behavioural references: Oracle's archived *MySQL Notifier Reference
  Manual* at `https://downloads.mysql.com/docs/mysql-notifier-en.pdf`, the
  official 1.1.8 release announcement at
  `https://dev.mysql.com/blog-archive/mysql-notifier-1-1-8/`, and the DB-Notifier
  Design System's already adopted clean-room Tray requirements. Public
  behaviour may inform requirements; proprietary implementation, artwork,
  wording, trade dress and vendor architecture are not inputs.
- Verifiable objective: introduce an Application-owned, bounded inventory
  snapshot read contract and validator; reconcile one accepted snapshot at a
  time through a `TimeProvider`; preserve the last accepted snapshot on a
  denied, offline, incompatible or failed read; update Tray, flyout and
  secondary WPF shell coherently; provide initial, manual and serial 30-second
  refresh paths; and retain the deterministic demonstration behind an isolated
  adapter with no operational source.
- Positive scope: this plan; one explicit clean-room functional-coverage matrix
  and factual report; `DBNotifier.Application` presentation contracts and
  coordinator; the minimum WPF demonstration adapter, composition, controller,
  flyout, shell and localised manual-refresh presentation; focused unit and
  architecture regressions; the owning Design System clarification; generated
  localisation adapters through their canonical XML source; factual current
  state and append-only history reconciliation; applicable local validation;
  and one focused local commit.
- Frozen read-only scope: provider implementations and registry behaviour;
  Domain health semantics; Agent, API, Server and Dashboard runtime behaviour;
  schemas, migrations, manifests, dependencies, lockfile identities, package
  versions, workflows, installer, update channel and notification-delivery
  contracts except deterministic generated localisation parity.
- Protected work and negative scope: no MySQL or MariaDB provider/driver,
  credential, connection editor, SSH tunnel, service discovery, name-filter
  auto-add, WMI/DCOM, firewall mutation, Start/Stop/Restart execution,
  authoritative change notification, persisted notification preference,
  external database, infrastructure, deploy, publication, push, Human Gate,
  activation, homologation or lifecycle transition. The protected external
  source tree and every item beneath it remain strictly inaccessible.
- Artefact classification and ownership: governing prompts and Design System
  are `AUTHORITY`; current state is `CURRENT_FACT`; the state log is append-only
  `HISTORY`; this file is `PLAN`; source, canonical localisation and tests are
  `IMPLEMENTATION`; generated localisation adapters are `GENERATED`; the
  coverage report is `EVIDENCE`. This coordinating conversation is the sole
  writer for every path, Git index and focused commit.
- Execution topology and mutable resources: `SINGLE_OWNER` and
  `SEQUENTIAL_ONLY`. Only one reconciliation read may run at a time; UI
  application occurs on the WPF Dispatcher; timer scheduling restarts after a
  completed cycle; every command owns and closes its child processes and
  outputs before the next stage.
- Definition of Ready: shutdown and exact baseline checks passed; tracked state
  is clean; the current composition, public references, Design System,
  Application boundary, WPF consumers, canonical localisation source, test
  owners, positive/negative scope and objective stop codes are identified.
- Definition of Done: the coverage matrix classifies every public behaviour in
  scope as adopted, safely adapted, rejected or scheduled; initial/manual/
  periodic reads share one validated coordinator; concurrent reads do not
  overlap; rejected reads retain only the last accepted snapshot and present
  factual source/freshness state; the normal WPF composition remains a labelled
  local demonstration with no external data; every changed behaviour has
  focused regression coverage; generated assets are regenerated from canonical
  sources; applicable focal checks and canonical workflow evidence are factual;
  one focused local commit exists; lifecycle remains unchanged.
- Stop rule: any protected-path exposure, baseline drift, scope overlap,
  licensing conflict, unready dependency, unisolated runtime, failed required
  gate or need for provider, administrative, notification, external or
  lifecycle authority stops the affected work without implicit widening,
  retry-based evidence replacement or speculative implementation.
- Objective stop codes: `AUTHORITY_MISMATCH`, `BASELINE_DRIFT`,
  `SCOPE_OVERLAP`, `DEPENDENCY_UNREADY`, `ISOLATION_FAILURE`,
  `MUTABLE_RESOURCE_COLLISION`, `GATE_FAILURE`, `EXTERNAL_AUTHORITY_REQUIRED`
  and `HUMAN_DECISION_REQUIRED` retain their governed meanings.
- Rollback strategy: before commit, reverse only this envelope's owned
  candidate under separately authorised recovery; after commit, preserve
  history and use a separately authorised focused revert. No database,
  provider, schema or external rollback applies.
- Implemented outcome: `DBNotifier.Application` owns the bounded snapshot source
  contract, validation, non-overlapping acquisition and last-known retention;
  WPF composes the local fixture only through an isolated adapter and applies
  initial, manual, localisation and 30-second periodic frames coherently. The
  flyout exposes read-only Refresh Status and factual source/retention text.
- Coverage decision: the clean-room matrix in
  `docs/STATE-06-Desktop-Fleet-Read-Only-Reconciliation-Report.md` classifies
  the public behaviours as adopted, safely adapted, rejected or scheduled.
  No protected implementation source, vendor asset, proprietary product copy or
  vendor-specific architecture was an input.
- Findings closed during validation: `CA1001` replaced the disposable
  `SemaphoreSlim` with an atomic guard; one invalid-candidate reason code was
  corrected to `snapshot.invalid`; and three stale cross-surface test
  expectations were reconciled to the new Application-owned boundary. The
  failed intermediate results remain factual and are not relabelled.
- Focal evidence: reconciliation `5/5`; architecture reconciliation `1/1`;
  affected legacy architecture `1/1`; Dashboard presentation `25/25`; WPF build
  zero warnings and zero errors.
- Development evidence: final `Quick` passed as `NON_GATE` with Release build,
  unit `533/533`, architecture `100/100`, Dashboard `74/74` and all included
  generation, documentation, localisation, Markdown and script checks.
- Canonical evidence: exactly one `Full` completed
  `DISPOSITION|PASS|stage=All`; unit `533/533`, architecture `100/100`,
  integration `168/168`, WPF `10/10`, line coverage `83.44%`, branch coverage
  `56.81%`, vulnerability/runtime/legacy/bundle/Web/browser/consolidated gates
  passed. The browser and consolidated harnesses used dedicated local test
  processes and local test data only.
- Final review disposition: `P0=0`, `P1=0`, `P2=0`, `P3=0` after frozen-scope,
  clean-room, licensing, architecture, security, localisation, accessibility,
  generated-artefact and factual-state review. The narrow local-source lot does
  not require a separate high-risk semantic reviewer.
- Delivery: this completed record is included in the single focused local
  commit required by repository policy. Its object ID is reported in the final
  hand-off rather than embedded here, avoiding a self-referential commit hash.
- Remaining mandatory items for `S06-DFR-01`: `0`. Agent/API binding, persisted
  preferences, provider integrations, authoritative notification delivery and
  administration remain separately authorised future work, not remainder of
  this target.
- Resulting state: `STATE-06 INTEGRATION` and lifecycle eligibility remain
  unchanged; no Human Gate, provider support, activation or external authority
  is inferred.

### Project-wide copy-box governance `GOV-2026-R1` — complete

- Envelope status: `COMPLETE`.
- Exact human authority: document throughout DB-Notifier the immediately prior
  owner preference that text intended for copying must be placed inside a box
  where it can be edited and copied. No adjacent product, runtime, lifecycle or
  external authority is inferred.
- Workspace and frozen baseline: `C:\Projects\DB-Notifier`, branch `main`,
  commit `0f63548365cf5fbb83cc5bff781fecab2f9bbc77`; index, tracked worktree and
  complete non-ignored untracked inventory were clean before this plan update.
- Initial shutdown evidence: the single mandatory canonical preflight returned
  `PASS`; exit code `0`; matching processes `0`, owned listeners `0`.
- Verifiable objective: make one project-wide presentation rule require every
  owner-facing payload explicitly intended for copying to appear by itself in
  a fenced Markdown code block labelled `text`, with labels and explanatory
  prose outside the block so copied content remains exact.
- Positive scope: this plan; the root cross-cutting instruction; the thematic
  conversation-coordination authority; its reusable template and Quality Gate;
  the instruction-corpus changelog; current factual state; append-only history;
  and the minimum development-flow policy assertion needed to keep the
  versioned corpus fail-closed. The existing development-flow test remains
  read-only unless a directly related expectation proves stale.
- Frozen read-only scope: every product source, product test, runtime runner,
  workflow, dependency, manifest, lockfile, graph, version or integrity outside
  the instruction-corpus policy version; every schema, migration, provider,
  executable contract, interface and generated artefact.
- Protected work and negative scope: all predecessor results and the ignored I6
  residue remain untouched. No database/provider, ordinary browser, product
  runtime, build, deploy, publication, push, Human Gate, ADR decision,
  activation, homologation or lifecycle transition is authorised.
- Artefact classification and ownership: `AGENTS.md` and the coordination and
  Quality Gate documents are `AUTHORITY`; the template is `AUTHORITY`; the
  changelog and append-only state log are `HISTORY`; current state is
  `CURRENT_FACT`; this file is `PLAN`; the policy verifier is
  `IMPLEMENTATION`. The coordinating conversation is the exclusive writer for
  every authorised path, the Git index and the focused commit.
- Execution topology and mutable resources: `SINGLE_OWNER` with one isolated
  read-only verifier-inspection lane. All writes, validation, integration and
  Git operations remain sequential in the coordinating conversation.
- Definition of Ready: shutdown preflight, exact branch, exact HEAD and clean
  complete non-ignored tree passed; the owning authority, dependent template,
  gate, version records and verifier were identified; scope, ownership and stop
  codes are explicit.
- Definition of Done: the project-wide rule covers every payload explicitly
  presented for copying; `Exact next message` uses exactly one `text` block and
  parallel payloads use one block per payload; no label or explanation enters a
  copy block; the 14 handoff fields and all closed enums remain unchanged; the
  corpus patch version, current state and append-only history agree; focused
  policy and documentary checks pass; one focused local commit exists; lifecycle
  and product state remain unchanged.
- Stop rule: any baseline drift, scope overlap, malformed policy, failed
  required check or inability to isolate the documentation-only increment stops
  further mutation and is recorded factually without widening scope.
- Objective stop codes: `AUTHORITY_MISMATCH`, `BASELINE_DRIFT`,
  `SCOPE_OVERLAP`, `DEPENDENCY_UNREADY`, `ISOLATION_FAILURE`,
  `MUTABLE_RESOURCE_COLLISION`, `GATE_FAILURE`, `EXTERNAL_AUTHORITY_REQUIRED`
  and `HUMAN_DECISION_REQUIRED` retain their governed meanings.
- Rollback strategy: before commit, reverse only this lot's documentary and
  policy-verifier candidate under separately authorised recovery; after commit,
  preserve history and use a separately authorised focused revert.
- Result: the project-wide presentation rule is materialised at corpus `6.7.1`
  and coordination revision `1.4.1`. The first two focal verifier invocations
  exited `1` because the new assertion did not yet normalise every ordinary
  Markdown line wrap inside the normative sentence; the rule itself was present
  throughout. The corrected assertion normalises whitespace before its ordinal
  comparison, and the next invocation passed `106` assertions. The code
  documentation gate passed `439` files, the Markdown link gate passed `990`
  local links in `227` files, and the secret scan passed the non-ignored
  worktree and available Git history. Two independent final reviews closed at
  `P0=0`, `P1=0`, `P2=0`, `P3=0`, confirmed the append-only prefix and required
  no test change. No product path changed.

### Authorised validation recovery `AUD-2026-R1-R5-R2` — blocked

- Envelope status: `BLOCKED` by `ISOLATION_FAILURE`.
- Exact human authority: start from clean
  `main@dcd3d24064e0709981e8ba0cff223de6a2c35565`; execute the canonical shutdown
  preflight exactly once and stop on drift, `FAIL` or `BLOCKED`; do not alter
  implementation or tests; execute the exact focal regression once with its
  complete first output and exit code retained durably; only after `PASS`, run
  `Doctor`, then `Quick`, then exactly one online `Full`; stop without retry,
  executable diagnosis or in-line correction on any failure; reconcile only
  this plan, current state and append-only history; create one focused local
  commit when applicable; do not use a real database/provider, ordinary
  browser, deploy, push, Human Gate, activation or lifecycle transition.
- Workspace and frozen baseline: `C:\Projects\DB-Notifier`, branch `main`, commit
  `dcd3d24064e0709981e8ba0cff223de6a2c35565`; index, tracked worktree and
  complete non-ignored untracked inventory were clean before this plan update.
- Initial shutdown evidence: the single authorised canonical preflight returned
  `PASS`; exit code `0`; matching processes `0`, owned listeners `0`.
- Verifiable objective: recover executable evidence for the already committed
  protected readiness lookup without changing the candidate, and preserve the
  first literal result of every authorised validation stage.
- Positive scope: this plan before validation; exact read-only hashes of the
  committed runner and focal test; one focal `dotnet test` invocation; generated
  ignored evidence only under
  `.dotnet/evidence/AUD-2026-R1-R5-R2/`; one `Doctor`; one `Quick`; exactly one
  online `Full` only if all preceding stages pass; factual reconciliation only
  in `PLANS.md`, `prompts/state/Current-State.md` and append-only
  `prompts/state/State-Transition-Log.md`; one focused local commit.
- Frozen read-only scope: `scripts/run-state06-consolidated-e2e.ps1`;
  `tests/DBNotifier.Architecture.Tests/State06ConsolidatedHarnessIsolationTests.cs`;
  every other source, test, script, workflow, configuration, manifest, lockfile,
  dependency graph, version, integrity value, contract, schema and migration.
- Protected work and negative scope: external or ignored material remains
  unread, unmodified and undeleted except for the exact new ignored evidence
  boundary owned by this lot. The historical I6 residue remains untouched. No
  product/provider runtime, operational database, ordinary browser, deployment,
  publication, push, Human Gate, activation, homologation or lifecycle
  transition is authorised.
- Artefact classification and ownership: generated output is `EVIDENCE`; this
  file is `PLAN`; current state is `CURRENT_FACT`; the transition log is
  append-only `HISTORY`. The coordinating conversation is the exclusive writer
  for the evidence boundary, three authorised documents, Git index and commit.
- Execution topology and mutable resources: `SEQUENTIAL_ONLY`. Each command
  exclusively owns its child processes and generated outputs. No parallel lane,
  shared runtime, alternative runner or retry is admitted.
- Definition of Ready: the initial preflight, exact branch, exact HEAD and clean
  complete non-ignored tree passed; the R1 candidate commit and historical first
  results remain immutable; authority, frozen paths, evidence boundary, ordered
  checks and stop codes are explicit.
- Definition of Done: runner and test remain byte-identical; focal test,
  `Doctor`, `Quick` and sole online `Full` return their first `PASS` in order;
  focal stdout/stderr, TRX and exit code are retained in the owned ignored
  evidence boundary; factual records are reconciled; one focused local commit
  exists; `STATE-06` remains unchanged.
- Stop rule: each authorised executable stage runs once. Any first non-zero
  exit, mechanical `FAIL`/`BLOCKED`, missing durable result or baseline drift
  stops every later executable stage without retry, alternative execution,
  executable diagnosis or in-line correction. Only factual reconciliation and
  a safely isolated focused commit may follow.
- Objective stop codes: `AUTHORITY_MISMATCH`, `BASELINE_DRIFT`,
  `SCOPE_OVERLAP`, `DEPENDENCY_UNREADY`, `ISOLATION_FAILURE`,
  `MUTABLE_RESOURCE_COLLISION`, `GATE_FAILURE`, `EXTERNAL_AUTHORITY_REQUIRED`
  and `HUMAN_DECISION_REQUIRED` retain their definitions below.
- Rollback strategy: no implementation or test rollback is applicable because
  both are frozen. Before commit, reverse only this lot's documentary candidate
  under separately authorised recovery; after commit, preserve history and use
  a separately authorised focused revert.
- Result: preflight and baseline are `PASS`. The first attempt to start the
  durable-capture wrapper was rejected by the local execution boundary before
  `CreateProcess`; no PowerShell or test process started, and the exact ignored
  evidence boundary remained absent. The focal test is `NOT_RUN`, and the stop
  rule leaves `Doctor`, `Quick` and `Full` `NOT_RUN`. No retry, alternative
  invocation, executable diagnosis or in-line correction followed.

### Authorised consolidated-readiness corrective attempt `AUD-2026-R1-R5-R1` — blocked

- Envelope status: `BLOCKED` by `ISOLATION_FAILURE`.
- Exact human authority: start from clean
  `main@34e5f3358491a1eb52b508c0170d6a9ac3169bc4`; execute the canonical shutdown
  preflight exactly once and stop without editing on drift, `FAIL` or `BLOCKED`;
  update this plan before implementation; correct only consolidated readiness
  parsing and its focal architecture regression; execute the focal test,
  `Doctor`, `Quick` and, conditionally, exactly one online `Full`; preserve every
  first factual result; reconcile only this plan, current state and append-only
  history; create one focused local commit; do not use a real database/provider,
  ordinary browser, deploy, push, Human Gate, activation or lifecycle transition.
- Workspace and frozen baseline: `C:\Projects\DB-Notifier`, branch `main`, commit
  `34e5f3358491a1eb52b508c0170d6a9ac3169bc4`; index, tracked worktree and
  complete non-ignored untracked inventory were clean before this plan update.
- Initial shutdown evidence: the single authorised canonical preflight returned
  `PASS`; matching processes `0`, owned listeners `0`.
- Verifiable objective: admit only the exact consolidated readiness record from
  a mixed stdout stream while safely ignoring non-JSON lines, JSON `null`,
  objects without `marker` and non-exact marker values under the StrictMode
  inherited from `scripts/ci.ps1`.
- Positive implementation scope: this plan;
  `scripts/run-state06-consolidated-e2e.ps1` readiness parsing only;
  `tests/DBNotifier.Architecture.Tests/State06ConsolidatedHarnessIsolationTests.cs`
  focal source-contract regression only; one focused test execution; one
  `Doctor`; one `Quick`; exactly one online `Full` only if all preceding stages
  pass; mandatory reconciliation limited to `prompts/state/Current-State.md`
  and append-only `prompts/state/State-Transition-Log.md`; one focused commit.
- Frozen read-only scope: `scripts/ci.ps1` and its inherited
  `Set-StrictMode -Version Latest`; the consolidated host, activation argument,
  exact literal `DBNOTIFIER_STATE06_CONSOLIDATED_SANDBOX_READY`, all readiness
  payload members and every adjacent runner; all other source, tests, workflow,
  configuration, version files, manifests, lockfiles, dependency graph,
  dependency versions, integrity values, contracts and architecture artefacts.
- Protected work and negative scope: external or ignored material remains
  unread, unmodified and undeleted. No product behaviour, host change, provider,
  operational database, ordinary browser, deploy, publication, push, Human
  Gate, activation, homologation or lifecycle transition is authorised.
- Artefact classification and ownership: the runner and architecture test are
  `IMPLEMENTATION`; this file is `PLAN`; current state is `CURRENT_FACT`; the
  transition log is append-only `HISTORY`. The coordinating conversation is the
  exclusive writer for every authorised path, the Git index and final commit.
- Execution topology and mutable resources: `SEQUENTIAL_ONLY`. Static candidate
  review finishes before executable validation; each authorised command then
  exclusively owns its bounded child processes, temporary resources and first
  result. No concurrent writer or runtime is admitted.
- Definition of Ready: the one authorised preflight passed; exact branch, HEAD
  and clean non-ignored tree passed; prior `AUD-2026-R1-R4` failure remains
  immutable; root cause, writable paths, regression, frozen contracts, negative
  scope, sequence and objective stop codes are explicit.
- Definition of Done: readiness parsing uses protected property lookup and an
  exact case-sensitive marker comparison; all four non-candidate forms are
  ignored without weakening StrictMode; the focal regression requires protected
  lookup and forbids direct `$candidate.marker`; host, literal and adjacent
  runners remain unchanged; focal test, `Doctor`, `Quick` and sole conditional
  online `Full` return their first `PASS`; factual records are reconciled; one
  focused commit exists; `STATE-06` remains unchanged.
- Stop rule: static defects may be corrected only before the first focal test.
  From the first focal test onwards, each authorised stage runs once; any first
  non-zero exit or mechanical `FAIL`/`BLOCKED` stops every later executable
  stage and prohibits retry, alternative execution, diagnosis execution or
  in-line correction. Only mandatory factual reconciliation and an isolable
  focused commit may follow a stopped stage.
- Objective stop codes: `AUTHORITY_MISMATCH`, `BASELINE_DRIFT`,
  `SCOPE_OVERLAP`, `DEPENDENCY_UNREADY`, `ISOLATION_FAILURE`,
  `MUTABLE_RESOURCE_COLLISION`, `GATE_FAILURE`, `EXTERNAL_AUTHORITY_REQUIRED`
  and `HUMAN_DECISION_REQUIRED` retain their definitions below.
- Rollback strategy: before commit, reverse only this lot's two implementation
  paths under separately authorised recovery; after commit, preserve history
  and use a separately authorised focused revert. Never discard predecessor or
  unrelated work.
- Result: the minimum parser and regression candidate are materialised. Static
  review found no defect, `git diff --check` passed, the runner contains zero
  direct `$candidate.marker` occurrence and frozen host/StrictMode/adjacent
  runner paths remain unchanged. The sole focal command built the candidate and
  reported one matching test file, but the execution channel did not retain its
  final verdict or exit code. Read-only recovery of that same execution found no
  matching process and no durable result artefact. The focal stage is therefore
  `BLOCKED` by `ISOLATION_FAILURE`; it was not retried, and `Doctor`, `Quick` and
  `Full` are `NOT_RUN`.

### Authorised documentary architecture decision `ARCH-2026-R1` — complete

- Envelope status: `COMPLETE`.
- Exact human authority: prepare a documentation-only proposed architecture
  decision for the named Web, API, data, cache/session, container, Linux,
  reverse-proxy and repository-automation technologies; update this live plan;
  perform applicable documentation checks once; create one focused local
  commit; do not implement, advance lifecycle, accept the ADR or take external
  action.
- Workspace and frozen baseline: `C:\Projects\DB-Notifier`, branch `main`,
  commit `0b09bd62863ad71fb1fcde48c6f47851b2ee0e69`; index, tracked worktree and
  non-ignored untracked inventory were clean before this plan update.
- Initial shutdown evidence: one canonical shutdown preflight returned `PASS`;
  matching processes `0`, owned listeners `0`.
- Verifiable objective: record one coherent proposed decision that retains the
  existing React/Vite and C#/.NET 10/ASP.NET Core baseline, PostgreSQL as the
  sole durable source of truth and REST as the primary API; makes Next.js,
  Redis and GraphQL conditional on objective criteria; and defines a future
  Docker/Linux/Nginx Web topology plus Git/GitHub/GitHub Actions delivery
  boundaries without claiming implementation or operational support.
- Positive documentary scope: this live plan;
  `docs/architecture/ADR-0009-Web-API-Data-And-Linux-Delivery-Topology.md` as a
  new `en-GB` artefact with status `proposed`; the discoverability entry in
  `docs/architecture/README.md`; static independent review; the development-flow
  policy verifier, code-documentation verifier, Markdown-link verifier,
  secret scan and `git diff --check`; one focused local commit.
- Frozen read-only scope: `AGENTS.md`, the instruction corpus, current state,
  append-only history, accepted ADRs, other proposed ADRs, architecture
  baseline, product reports, source, tests, scripts, workflows, configuration,
  manifests, lockfiles, schemas and migrations.
- Protected work and negative scope: protected external or ignored material
  remains unread, unmodified and undeleted. `AUD-2026-R1-R5-R1` and every path
  or artefact owned by it are protected read-only work and are neither
  incorporated nor altered by this envelope. No code, configuration,
  dependency, lockfile, schema, migration, runtime, build, product test,
  operational database, implementation, support claim, ADR acceptance, Human
  Gate, activation, lifecycle transition, deploy, publication, push or other
  external action is authorised. Subjects outside the exact named technology
  set remain outside the ADR.
- Artefact classification and ownership: `PLANS.md` is `PLAN`; the new ADR is
  `AUTHORITY` with proposal status; the architecture index is `AUTHORITY`. The
  coordinating conversation is the exclusive writer for all three paths. The
  independent reviewer is read-only and cannot accept the ADR or alter facts.
- Execution topology and mutable resources: `SEQUENTIAL_ONLY`; no product
  process, port, database, container, browser, workflow runner or external
  resource is started or changed. Git index and worktree remain under the
  coordinating conversation's exclusive custody through the final commit.
- Definition of Ready: shutdown preflight `PASS`; exact clean baseline frozen;
  authority, positive and negative scope, owners, checks, reviewer and stop
  codes explicit; ADR number `0009` confirmed unused; current aggregate-gate
  `FAIL` preserved as prior factual evidence rather than retried.
- Definition of Done: the plan is reconciled; ADR-0009 remains `proposed` and
  contains context, decision, alternatives, consequences, adoption criteria,
  failure behaviour, rollback, observability, acceptance conditions and
  negative scope; the architecture index labels it as proposed and
  non-authorising; independent review has no unresolved `P0` or `P1`; every
  applicable documentation check passes on its first execution; final diff and
  staged diff contain only the three authorised paths; one focused local commit
  exists; `STATE-06` remains unchanged.
- Rollback strategy: before commit, correct only in-scope documentary defects;
  after commit, preserve history and use a separately authorised focused revert
  if the proposal must be withdrawn. No product, data, schema or runtime
  rollback applies because none changes.
- Objective stop codes: `AUTHORITY_MISMATCH`, `BASELINE_DRIFT`,
  `SCOPE_OVERLAP`, `DEPENDENCY_UNREADY`, `ISOLATION_FAILURE`,
  `MUTABLE_RESOURCE_COLLISION`, `GATE_FAILURE`, `EXTERNAL_AUTHORITY_REQUIRED`
  and `HUMAN_DECISION_REQUIRED`. Any first applicable stop condition prevents
  later validation or correction beyond safe factual reconciliation and the
  mandatory focused commit of an isolable authorised state.
- Result: `COMPLETE`. The candidate was frozen after an initial independent
  review reported `P0=0`, `P1=0`, `P2=3`, `P3=0`; all three documentary
  findings were corrected, and the final read-only re-review reported `P0=0`,
  `P1=0`, `P2=0`, `P3=0`. The development-flow, code-documentation,
  Markdown-link and secret checks each passed on their first execution. The
  final whitespace and staged-scope checks and the focused local commit form
  the atomic closing boundary after this ledger freeze; their exact facts are
  reported from the final Git state in the owner hand-off.

### Authorised dependency corrective continuation `AUD-2026-R1-R4` — blocked

- Envelope status: `BLOCKED` by `GATE_FAILURE` from its sole online `Full`.
- Exact human authority: `Quero que implemente no DB-Notifier o mesmo método e
  fluxo de desenvolvimento do RAG-Challenge`.
- Workspace and frozen baseline: `C:\Projects\DB-Notifier`, branch `main`,
  commit `0f59408440dc1c5877f8d3de0de8859ef9bc7fed`; tracked worktree and index
  were clean before this plan update.
- Initial shutdown evidence: one canonical shutdown preflight returned `PASS`;
  matching processes `0`, owned listeners `0`.
- Verifiable objective: remove the sole known high-severity Dashboard
  dependency finding by moving the transitive `nanoid` lock entry from
  `3.3.16` to a non-vulnerable compatible patch selected by npm, without
  changing its owning dependency edge or any developer-toolchain range.
- Positive implementation scope:
  `src/DBNotifier.Dashboard.Web/package-lock.json`, generated through npm in
  package-lock-only mode; this plan and only the mandatory factual current-state
  and append-only history reconciliation; deterministic lockfile comparison;
  one focused dependency audit; `Doctor`; `Quick`; exactly one online `Full`
  only after every preceding stage passes; one focused local commit.
- Frozen read-only scope:
  `src/DBNotifier.Dashboard.Web/package.json` at SHA-256
  `5a137255c337ab1a159e797dd7187bcfdbcdb70ca75c73c0cf43faf5f3a917a8`,
  every other manifest and lockfile, source and test implementation, workflow,
  `global.json`, `.nvmrc`, dependency edges, developer-toolchain ranges and
  external contracts. The initial Dashboard lockfile SHA-256 is
  `adc835185b3676484274acc938487ee846599aadcc18e16b02d4b3fbe64ec5f0`.
- Protected work and negative scope: ignored or external protected material
  remains unread, unmodified and undeleted. Product components, backend,
  migrations, real database/provider use, ordinary browser use, deploy,
  publication, push, Human Gate, activation, homologation and lifecycle
  transition remain prohibited.
- Execution topology and ownership: `SEQUENTIAL_ONLY`; the coordinating
  conversation is the sole writer. npm exclusively owns the generated lockfile
  update, and each canonical check owns its bounded runtime and temporary
  resources.
- Definition of Ready: clean exact baseline and initial preflight proved;
  previous `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE` preserved; official
  advisory and patched-version evidence must be confirmed before the generated
  lockfile update; positive and negative scope frozen.
- Definition of Done: only the `nanoid` package identity fields required for
  the compatible patch change in the Dashboard lockfile; package manifest and
  dependency edges remain byte-identical; focused dependency audit, `Doctor`
  and `Quick` pass once and in order; exactly one online `Full` passes only if
  every prerequisite passed; factual records are reconciled; one focused local
  commit exists; `STATE-06` remains unchanged.
- Stop rule: the generated lockfile update runs once. Each authorised
  executable validation stage then runs once. Any first non-zero exit or
  mechanical `FAIL`/`BLOCKED` stops every later check and prohibits retry or
  in-line correction. A stopped result permits only mandatory factual
  reconciliation and the focused local commit of already authorised changes.
- Objective stop codes: `BASELINE_DRIFT`, `SCOPE_OVERLAP`,
  `ISOLATION_FAILURE`, `MUTABLE_RESOURCE_COLLISION`, `GATE_FAILURE`,
  `EXTERNAL_AUTHORITY_REQUIRED` and `HUMAN_DECISION_REQUIRED` retain their
  definitions below.
- Result: npm generated the exact compatible `nanoid 3.3.18` lock identity;
  the manifest hash and dependency edge remained unchanged. The focused audit,
  `Doctor` and `Quick` passed. The sole online `Full` confirmed zero npm
  vulnerabilities and passed the isolated Dashboard browser audit, then failed
  in the later STATE-06 consolidated E2E runner because one candidate object
  did not expose a `marker` property at
  `scripts/run-state06-consolidated-e2e.ps1:254`. The preserved result is
  `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`; no retry, executable
  diagnosis or in-line correction followed. Closing shutdown passed with zero
  matching process and zero owned listener.

### Authorised recovery and corrective continuation `AUD-2026-R1-R3-R1` — blocked

- Envelope status: `BLOCKED` by `GATE_FAILURE` from its sole online `Full`.
- Exact human authority: recover from the preserved first
  `AUD-2026-R1-R3` result
  `BLOCKED|shutdown-preflight|pid=8952|process=pwsh.exe` /
  `ISOLATION_FAILURE`; inspect only that PID's executable path, command line
  and parentage without reading environment values; terminate it only if its
  DB-Notifier ownership remains proved; execute exactly one new shutdown
  preflight; then, only after a clean exact
  `main@6ecc72f7a347a746d0153072580c527d7c679e81`, resume the bounded legacy SDK
  identity correction, focused checks, `Doctor`, `Quick` and at most one
  online `Full`, stopping without retry or in-line correction at the first
  `FAIL` or `BLOCKED`.
- Preserved first result: the initial `AUD-2026-R1-R3` shutdown assertion
  returned exit code `1` and
  `BLOCKED|shutdown-preflight|pid=8952|process=pwsh.exe`. No baseline check,
  edit, retry, termination or executable validation followed in that lot.
- Recovery evidence: PID `8952` was absent at the single authorised identity
  check, so no process was terminated. The single recovery shutdown preflight
  then returned `PASS`; matching processes `0`, owned listeners `0`.
- Workspace and recovery baseline: `C:\Projects\DB-Notifier`, branch `main`,
  commit `6ecc72f7a347a746d0153072580c527d7c679e81`; tracked worktree and index
  were clean before this plan update.
- Verifiable objective: preserve the exact compatible dotnet executable
  selected by the canonical gate across the Windows PowerShell legacy boundary
  and into every legacy vulnerability-verifier invocation.
- Positive implementation scope: `scripts/ci.ps1`,
  `scripts/run-legacy-tests.ps1`, `tests/DBNotifier.Legacy.Tests.ps1` and
  `tests/DBNotifier.DevelopmentFlow.Tests.ps1`; this plan and only the mandatory
  factual current-state and append-only history reconciliation; one focused
  local commit.
- Frozen read-only scope: `scripts/development.ps1`,
  `scripts/verify-nuget-vulnerabilities.ps1`, `global.json`, product
  implementation, workflow, versions, manifests, lockfiles, dependencies and
  external contracts.
- Protected work and negative scope: external protected material and ignored
  residue remain unread, unmodified and undeleted. Backend, migrations, real
  database/provider use, ordinary browser use, deploy, publication, push,
  Human Gate, activation and lifecycle transition remain prohibited.
- Execution topology and ownership: `SEQUENTIAL_ONLY`; the coordinating
  conversation is the sole writer for all four implementation/test paths and
  factual records. The canonical checks exclusively own their bounded runtime
  and temporary resources.
- Definition of Ready: recovery PID disposition recorded; recovery preflight
  passed; exact branch, commit and clean tracked state proved; previous
  `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE` preserved; positive and
  negative scope frozen.
- Definition of Done: the validated dotnet path is mandatory at every legacy
  boundary; focused policy regression and one legacy-runner execution pass;
  `Doctor` and `Quick` pass once and in order; exactly one online `Full` passes
  only if every prerequisite passed; factual records are reconciled; one
  focused local commit exists; `STATE-06` remains unchanged.
- Stop rule: each authorised executable stage runs once. Any first non-zero
  exit or mechanical `FAIL`/`BLOCKED` stops every later check and prohibits
  retry or in-line correction. A stopped result permits only mandatory factual
  reconciliation and the focused local commit of already authorised changes.
- Result: the recovery preflight, exact baseline, bounded implementation,
  focused policy check, corrected legacy runner, `Doctor` and `Quick` passed.
  The sole online `Full` proved the legacy correction, then failed the
  Dashboard dependency audit because `nanoid <3.3.18` has one high-severity
  advisory. The preserved result is
  `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`; no retry or in-line
  correction followed.

### Authorised corrective continuation `AUD-2026-R1-R1` — blocked

- Envelope status: `BLOCKED` by `GATE_FAILURE` from its sole online `Full`.
- Exact human authority: `AUTORIZO exclusivamente o lote corretivo
  AUD-2026-R1-R1 Plan Control Integrity no DB-Notifier, partindo da baseline
  limpa main@b60ef4d302e4c4dc3f0e474be27eaa4b8c6beb13. Execute primeiro o
  shutdown preflight e pare sem editar diante de qualquer drift rastreado.
  Corrija somente o control record de PLANS.md, renomeando “- Frozen
  baseline:” para a chave literal exigida “- Initial baseline:” e preservando
  o valor factual main@3762f71c116af206b911a086b836cef11cd1894d. Não altere
  a implementação já commitada, testes, workflow, versões, lockfiles,
  dependências ou contratos externos. Depois, execute uma única vez o check
  focal da política de desenvolvimento, Doctor e Quick; somente se todos
  passarem, execute exatamente uma vez o Full online. Preserve o primeiro
  resultado factual de cada etapa e pare diante de qualquer FAIL ou BLOCKED,
  sem retry ou correção em linha. Reconcilie somente a documentação factual
  obrigatória e faça um commit local focado. Permanecem proibidos material
  externo protegido, exclusão de resíduos ignorados, backend/migrations, banco
  ou provider real, navegador comum, deploy, push, Human Gate, ativação e
  transição de STATE.`
- Workspace and continuation baseline: `C:\Projects\DB-Notifier`, branch
  `main`, commit `b60ef4d302e4c4dc3f0e474be27eaa4b8c6beb13`; index and non-ignored
  worktree were clean before the one-line correction.
- Initial shutdown evidence: `PASS`; matching processes `0`, owned listeners
  `0`.
- Positive scope: rename only the control-record key `- Frozen baseline:` to
  `- Initial baseline:` while preserving its original factual value; execute
  the focused policy check, `Doctor`, `Quick` and one online `Full` in that
  order; reconcile this plan, current state and append-only history; create one
  focused local commit.
- Frozen read-only scope: every implementation/test file from
  `AUD-2026-R1`, workflow, version file, manifest, lockfile, dependency and
  external contract. No product or policy implementation was authorised.
- Protected work and negative scope: external protected material, ignored
  residue, backend, migrations, real database/provider, ordinary browser,
  deploy, push, Human Gate, activation and lifecycle transition remained
  prohibited.
- Execution topology: `SEQUENTIAL_ONLY`; the coordinating conversation is the
  only writer and the canonical gates own their bounded temporary/runtime
  resources.
- Stop rule: every authorised executable stage runs once. Any first non-zero
  exit or mechanical `FAIL`/`BLOCKED` stops all later validation and prohibits
  retry, diagnosis execution or in-line correction. Online `Full` may run at
  most once.
- Result: the one-line control correction, focused policy check, `Doctor` and
  `Quick` passed. The sole online `Full` returned `FAIL` at legacy
  compatibility because its child environment could not resolve the required
  .NET SDK `10.0.302`; `Failed=1`, `Pending=0`. No retry or correction
  followed.

### Authorised corrective lot `AUD-2026-R1` — blocked

- Envelope status: `BLOCKED` by `GATE_FAILURE` from its first `Quick`.
- Exact human authority: `AUTORIZO exclusivamente o lote AUD-2026-R1 Gate And
  Inventory Integrity no DB-Notifier. Parta da baseline
  main@3762f71c116af206b911a086b836cef11cd1894d; se houver drift rastreado,
  pare sem editar. Execute primeiro o shutdown preflight e atualize o PLANS.md
  antes da implementação. Corrija somente: (1) a regra de ignore que oculta
  novos arquivos em src/DBNotifier.Persistence.Agent.Sqlite, mantendo arquivos
  runtime SQLite ignorados; (2) o verificador Markdown para derivar seu corpus
  do inventário Git e provar por regressão que nunca atravessa
  mysql-notifier-*-src nem outra raiz ignorada protegida; e (3) o teste
  arquitetural obsoleto para validar a topologia consolidada atual do CI, sem
  restaurar jobs antigos. Adicione regressões focais, preserve en-GB nos
  artefatos técnicos e não altere versões, lockfiles, dependências ou contratos
  externos. Depois, execute checks focais, Doctor e Quick; somente se todos
  passarem, execute uma única vez o Full online. Preserve o primeiro resultado
  factual de cada etapa e pare diante de qualquer FAIL ou BLOCKED, sem retry ou
  correção em linha. Faça um commit local focado conforme as instruções do
  repositório e reconcilie apenas a documentação factual obrigatória.
  Permanecem proibidos: ler ou modificar o material externo protegido, excluir
  qualquer resíduo ignorado, alterar backend/migrations, usar banco ou provider
  real, navegador comum, deploy, push, Human Gate, ativação ou transição de
  STATE.`
- Workspace and frozen baseline: `C:\Projects\DB-Notifier`, branch `main`,
  commit `3762f71c116af206b911a086b836cef11cd1894d`; the initial tracked worktree
  and index were clean.
- Initial shutdown evidence: `PASS`; matching processes `0`, owned listeners
  `0`.
- Verifiable objective: restore trustworthy Git inventory and clean-room
  boundaries, then align the architecture regression with the current single
  canonical Windows gate without altering the workflow topology.
- Positive scope: `.gitignore`; `scripts/verify-markdown-links.mjs`;
  `tests/DBNotifier.Architecture.Tests/State06ConsolidatedHarnessIsolationTests.cs`;
  one focused deterministic regression under the existing Dashboard test
  boundary; this plan; mandatory current-state and append-only history
  reconciliation; focused checks; `Doctor`; `Quick`; exactly one
  online `Full` only after every preceding check passes; one focused local
  commit.
- Frozen read-only contracts: `.github/workflows/ci.yml`, `global.json`,
  `.nvmrc`, `Directory.Packages.props`, every project/package manifest and
  every dependency lockfile. Their versions, dependency graph and integrity
  values must not change.
- Protected work: `mysql-notifier-1.1.8-src/` and every other ignored external
  root remain unread, unmodified, untracked and outside every inventory or
  gate traversal. Ignored build, dependency, toolchain and provisioning
  residue must not be deleted.
- Negative scope: product behaviour, backend, migrations, providers, real
  databases, ordinary browser use, dependency or contract changes, cleanup,
  deploy, publication, push, Human Gate, activation, homologation and lifecycle
  transition.
- Execution topology: `SEQUENTIAL_ONLY`. The coordinating conversation is the
  only writer. One independent reviewer may inspect the frozen candidate diff
  read-only and must finish before executable validation begins.
- Stop rule: static review findings may be corrected before the first focused
  executable check. From that check onwards, each authorised stage runs once;
  any non-zero exit or mechanical `FAIL`/`BLOCKED` stops all later checks and
  prohibits retry or in-line correction. Online `Full` may run at most once.
- Objective stop codes: `BASELINE_DRIFT`, `SCOPE_OVERLAP`,
  `ISOLATION_FAILURE`, `MUTABLE_RESOURCE_COLLISION`, `GATE_FAILURE`,
  `EXTERNAL_AUTHORITY_REQUIRED` and `HUMAN_DECISION_REQUIRED` retain their
  definitions below.
- Rollback: before commit, reverse only this lot's owned diff if required by an
  authorised recovery; after commit, use a separately authorised focused
  revert. Never discard predecessor history or unrelated work.
- Acceptance: a hypothetical source beneath the canonical SQLite project is
  not ignored while real SQLite runtime files remain ignored; the Markdown
  gate uses Git inventory and a synthetic regression proves ignored protected
  roots are not traversed; the architecture test validates the current
  workflow contract; focused checks, `Doctor`, `Quick` and the sole online
  `Full` all return their first successful dispositions; factual records are
  reconciled and one focused local commit contains only this lot.

### Original envelope `DEV-FLOW-01/v1` — closed

- Envelope status: `PARTIAL`
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
- Historical frozen contracts and dependencies: the three references in the provenance
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
- Original-envelope disposition: `PARTIAL`. The preserved mechanical
  vocabulary remains `PASS`, `FAIL`, `BLOCKED`, `PARTIAL` or `NOT_RUN`;
  continuations `I6` and `I7` have their own dispositions below.

### Authorised continuation `I6` — closed

- Envelope ID and version: `DEV-FLOW-01/v2`.
- Envelope status: `BLOCKED` by `ISOLATION_FAILURE`.

- Exact authority (verbatim): `AUTORIZO exclusivamente provisionar, de forma local e isolada para o workspace DB-Notifier, o .NET SDK 10.0.302, Node.js 24.18.0 e npm 11.16.0, sem alterar global.json, .nvmrc, package.json ou lockfiles e sem instalar componentes de produto. Depois, execute o shutdown preflight, Doctor, Quick e uma única execução online de Full, preserve o primeiro resultado factual e pare diante de qualquer falha. Permanecem proibidos banco/provider real, navegador comum, deploy, push, Human Gate, ativação e transição de STATE.`
- Execution topology: `SEQUENTIAL_ONLY`; the coordinating conversation owns
  the local toolchain roots, process environment, caches, outputs, Git state
  and factual evidence.
- Positive scope: official toolchain metadata and archives, cryptographic hash
  verification, isolated installation below the ignored workspace `.dotnet/`
  root, a private process `PATH`, and the authorised canonical commands.
- Negative scope: product components, database or provider runtime, ordinary
  browser use, deploy, publication, push, Human Gate, activation, lifecycle
  transition, pin changes and lockfile changes.
- Protected work: the clean tracked baseline and the unread external
  `mysql-notifier-1.1.8-src/` tree remain outside toolchain discovery and gate
  traversal.
- Stop rule: preserve the first factual outcome and stop without retry or
  corrective continuation when provisioning, `Doctor`, `Quick` or `Full`
  returns a non-zero exit or a mechanical `FAIL`/`BLOCKED` disposition.
- Acceptance: exact versions are observed from the isolated executables,
  `Doctor` and `Quick` complete successfully, and the sole online `Full` run
  supplies the canonical disposition without prohibited external activity.

### Authorised continuation `I7` — preserved blocked

- Envelope ID and version: `DEV-FLOW-01/v3`.
- Envelope status: `BLOCKED` by `GATE_FAILURE`.

- Exact authority (verbatim): `Não fixar as versões, introduzir um intervalo de versões compatíveis, pois pode acontecer do computador atualizar automaticamente eles`.
- Inherited continuation: the immediately preceding unanswered sequence still
  owns shutdown preflight, `Doctor`, `Quick`, exactly one online `Full`, first
  factual-result preservation and stop-on-failure. I7 changes only the
  incompatible exact-pin constraint and the tracked metadata needed to express
  bounded ranges.
- Baseline: `main@e6416f6dac3f65d672f0247da88850881bae9120`, plus the
  uncommitted factual `I6` records in `PLANS.md`, `Current-State.md` and the
  append-only `State-Transition-Log.md`; those records are protected history.
- Compatible contracts: .NET SDK `>=10.0.302 <10.1.0` through
  `latestFeature` roll-forward with prereleases disabled, Node.js
  `>=24.18.0 <25.0.0`, and npm `>=11.16.0 <12.0.0`.
- Positive scope: the canonical manifests, the root lockfile's engine metadata,
  a shared dependency-free range policy, local/CI resolvers, deterministic
  policy tests, active development documentation and factual evidence.
- Negative scope: dependency graph or integrity changes, product components,
  real database/provider runtime, ordinary browser use, deploy, publication,
  push, Human Gate, activation, lifecycle transition and deletion of the
  protected ignored `I6` archive residue.
- Execution topology: `SEQUENTIAL_ONLY`; one coordinating writer owns every
  tracked path, process, cache, gate result and final commit. Independent lanes
  may review the frozen candidate diff read-only before execution.
- Stop rule: static findings may be corrected before executable validation;
  after executable validation begins, any non-zero command or mechanical
  `FAIL`/`BLOCKED` disposition ends the sequence without retry or correction.
  `Full` may be invoked online at most once.
- Acceptance: all manifests express the same compatible ranges, boundary tests
  prove accepted and rejected versions, CI selects current compatible releases,
  shutdown preflight, `Doctor` and `Quick` pass, and the sole online `Full`
  supplies its first canonical disposition without prohibited activity.

### Authorised corrective continuation `I7-R1` — preserved blocked

- Envelope ID and version: `DEV-FLOW-01/v4`.
- Envelope status: `BLOCKED` by `GATE_FAILURE`.
- Exact authority (verbatim): `AUTORIZO exclusivamente uma nova tentativa do lote I7-R1 no workspace DB-Notifier, começando por localizar de forma somente leitura o entry point canônico existente do shutdown preflight e executá-lo uma única vez. Se o preflight passar, corrija de forma mínima e testada a leitura da chave raiz vazia de package-lock.json no policy helper, sem alterar as faixas, o grafo, as versões ou as integridades das dependências e sem remover o resíduo ignorado de I6. Depois, execute Doctor, Quick e uma única execução online de Full, preserve o primeiro resultado factual de cada etapa e pare diante de qualquer falha. Permanecem proibidos banco/provider real, navegador comum, deploy, push, Human Gate, ativação e transição de STATE.`
- Frozen baseline: `main@fc7001240f87a3ee555b9e53cc98d8c0c57b4ce5`
  with a clean tracked worktree before the correction.
- Positive scope: read-only discovery of the canonical shutdown entry point;
  one preflight; the minimum parser correction in
  `scripts/toolchain-version-policy.ps1`; one focused regression in
  `tests/DBNotifier.DevelopmentFlow.Tests.ps1`; factual plan, state, history
  and changelog reconciliation; `Doctor`; `Quick`; exactly one online `Full`;
  and one focused local commit.
- Frozen protected files: `global.json`, Dashboard `package.json` and
  `package-lock.json` must retain their pre-correction SHA-256 values
  `6CD80ED6F7A93E76C20E47164E3BFEDFDDC1B42B33519B2CD2DEE6FEBFF4E836`,
  `5A137255C337AB1A159E797DD7187BCFDBCDB70CA75C73C0CF43FAF5F3A917A8`
  and `ADC835185B3676484274ACC938487EE846599AADCC18E16B02D4B3FBE64EC5F0`,
  respectively.
- Negative scope: compatible-range changes, dependency graph/version/integrity
  changes, deletion of ignored I6 residue, product components, real
  database/provider runtime, ordinary browser use, deploy, publication, push,
  Human Gate, activation, homologation and lifecycle transition.
- Execution topology: `SEQUENTIAL_ONLY`. The coordinating conversation is the
  only writer and owns the policy helper, regression, factual evidence, gate
  sequence and commit. Any review lane remains read-only.
- Stop rule: run each authorised executable stage once and preserve its first
  factual result. Any non-zero command or mechanical `FAIL`/`BLOCKED`
  disposition stops all later executable stages without retry or in-place
  correction. The online `Full` may be invoked at most once.
- Acceptance: the empty-name root package key is read without weakening
  required-property failures; protected hashes remain identical; the focused
  regression, `Doctor`, `Quick` and sole online `Full` each return their first
  successful disposition; no prohibited boundary is crossed.

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
| NuGet and npm caches | Package managers during an authorised setup or gate | Online checks may update cache metadata; dependency versions, graph and integrities remain immutable while I7 owns only root engine metadata |
| Workspace-local toolchains | Coordinating conversation under the ignored `.dotnet/` root | Official archives only; exact hashes and versions must be verified before use |
| `mysql-notifier-1.1.8-src/` | Owner-protected external material; no project writer | No read, inventory, gate traversal or tracking |

### Stop conditions

| Stop code | Objective trigger |
|---|---|
| `AUTHORITY_MISMATCH` | Requested work would waive a protected DB-Notifier authority or require an ungranted external, lifecycle, ADR or Human Gate decision. |
| `BASELINE_DRIFT` | Branch, frozen commit, tracked user work or a frozen contract changes outside this increment before integration. |
| `SCOPE_OVERLAP` | A proposed edit reaches protected external material, product behaviour or another writer's path. |
| `DEPENDENCY_UNREADY` | No stable toolchain satisfies a declared compatible range, or a locked dependency or required input is unavailable. |
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

- [x] `AUD-2026-R1-R1` authority is exact and limits the implementation to one
  control-record key in this plan.
- [x] Mandatory shutdown preflight passed with zero matching processes and
  zero owned listeners.
- [x] Branch `main`, continuation commit and clean non-ignored worktree match
  the authorised baseline.
- [x] Original initial-baseline value, frozen read-only artefacts, negative
  scope, exclusive writer, sequential stages and stop conditions are recorded.
- [x] `AUD-2026-R1` authority is exact, bounded and distinct from the preserved
  blocked predecessor envelopes.
- [x] Mandatory shutdown preflight passed with zero matching processes and
  zero owned listeners.
- [x] Branch `main`, frozen commit and clean tracked worktree match the
  authorised baseline.
- [x] Positive scope, negative scope, protected material, exclusive writer,
  read-only contracts and objective stop conditions are recorded.
- [x] The three root causes and their focused regression boundaries are known
  before implementation.
- [x] Repository instructions and routed authorities read.
- [x] Shutdown preflight observed zero DB-Notifier-owned process or listener.
- [x] DB-Notifier baseline and protected work identified.
- [x] Public technical and governed normative RAG-Challenge references frozen.
- [x] Positive scope, negative scope, ownership and review lanes recorded.
- [x] Existing CI responsibilities and repository-native commands inventoried.
- [x] Versioned envelope, artefact classes, mutable resources, rollback and
  objective stop conditions recorded.

## Definition of Done — original `I1`–`I5`

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

### Completion criteria — current `I7`

- [x] Every tracked toolchain representation expresses the same bounded stable
  compatibility contract without an exact developer-tool pin.
- [ ] Boundary regressions accept the lower limit and later compatible
  versions while rejecting older, upper-bound, prerelease and malformed values.
- [x] Three independent read-only reviews have no unresolved `P0` or `P1`.
- [ ] Shutdown preflight and `Doctor` complete successfully.
- [ ] `Quick` completes successfully as `NON_GATE` evidence.
- [ ] Exactly one online `Full` execution supplies its first factual canonical
  disposition; no retry or in-place correction follows a failure.
- [x] Plan, current state, append-only history and corpus changelog reflect the
  observed result without Human Gate, activation or lifecycle claims.
- [x] One focused local commit contains the authorised I6 records and I7
  increment without protected or unrelated work.

### Completion criteria — current `I7-R1`

- [x] Canonical shutdown entry point located read-only and executed once with
  zero matching processes and zero owned listeners.
- [x] The minimum parser correction handles the empty-name root package key
  while preserving fail-closed required-property checks.
- [x] The focused regression passes once and all three protected hashes remain
  byte-identical.
- [x] A new `Doctor` completes successfully.
- [ ] `Quick` completes successfully as `NON_GATE` evidence.
- [ ] Exactly one online `Full` supplies its first factual canonical
  disposition.
- [x] Plan, current state, append-only history and corpus changelog preserve
  every historical failure and record the I7-R1 outcome without lifecycle or
  Human Gate claims.
- [x] One focused local commit contains only I7-R1 and its factual evidence.

### Completion criteria — preserved `AUD-2026-R1`

- [x] The canonical SQLite source tree is visible to Git inventory while
  ordinary `.sqlite`, journal, shared-memory and WAL runtime files remain
  ignored.
- [x] The Markdown verifier obtains tracked and non-ignored new paths from Git,
  performs no physical repository walk and fails closed on inventory errors.
- [x] A deterministic synthetic regression proves that the verifier checks a
  non-ignored new Markdown file but never traverses the exact ignored external
  pattern or another ignored protected root.
- [x] The architecture regression validates one canonical Windows gate, one
  recursive sanitised failure-evidence upload and the supplemental Linux
  Dashboard job without restoring removed workflow jobs.
- [x] One independent read-only candidate review has no unresolved `P0` or
  `P1` before executable validation begins.
- [ ] Focused regressions, `Doctor` and `Quick` each return their first
  successful disposition. The focused checks and `Doctor` passed, but the
  first `Quick` failed the existing plan-control policy.
- [ ] Exactly one online `Full` returns its first canonical disposition; no
  retry or in-line correction follows a failure. `Full` remained `NOT_RUN`.
- [x] Frozen manifests and lockfiles remain unchanged, protected external
  material remains unread, and no ignored residue is deleted.
- [x] Plan, current state and append-only history record the
  factual result without product, Human Gate, activation or lifecycle claims.
- [x] One focused local commit contains only `AUD-2026-R1` and its factual
  evidence.

### Completion criteria — current `AUD-2026-R1-R1`

- [x] The control record contains the exact literal `- Initial baseline:` and
  preserves `main@3762f71c116af206b911a086b836cef11cd1894d`.
- [x] No implementation, test, workflow, version, manifest, lockfile,
  dependency or external contract changed.
- [x] The focused development-flow policy check returned its first `PASS` with
  `105` assertions.
- [x] `Doctor` returned its first `PASS` after its internal shutdown preflight.
- [x] `Quick` returned its first `PASS` as `NON_GATE` evidence.
- [ ] Exactly one online `Full` returns `PASS`. Its sole execution returned
  `FAIL` in legacy compatibility and cannot be retried in this lot.
- [x] Protected external material remained unread, no ignored residue was
  deleted and no prohibited product/external action occurred.
- [x] Plan, current state and append-only history record the factual first
  results without Human Gate, activation or lifecycle claims.
- [x] One focused local commit contains only `AUD-2026-R1-R1` and its factual
  evidence.

### Completion criteria — preserved `AUD-2026-R1-R3-R1`

- [x] The initial shutdown `BLOCKED` result and the previous canonical
  `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE` remain unchanged.
- [x] PID `8952` was inspected once, found absent and not terminated; the one
  recovery shutdown preflight and exact tracked baseline check passed.
- [x] The exact canonical dotnet host is propagated through `ci.ps1`, the
  legacy runner, Pester parameters and all seven vulnerability-verifier calls.
- [x] The focused policy check and corrected legacy runner returned their
  first `PASS`; `Doctor` and `Quick` then returned their first `PASS` in order.
- [ ] Exactly one online `Full` returns `PASS`. Its sole execution proved the
  legacy correction but returned `FAIL` at the Dashboard dependency audit and
  cannot be retried in this lot.
- [x] Protected external material remained unread, no ignored residue was
  deleted and frozen files, versions, lockfiles, dependencies and contracts
  were not modified.
- [x] Plan, current state and append-only history record the first factual
  results without Human Gate, activation or lifecycle claims.
- [x] One focused local commit contains only `AUD-2026-R1-R3-R1` and its
  factual evidence.

### Completion criteria — current `AUD-2026-R1-R5-R2`

- [x] The single canonical initial shutdown returned `PASS`, and branch, exact
  HEAD and the complete non-ignored tree matched the authorised clean baseline.
- [x] Runner and focal test remain byte-identical to the authorised baseline.
- [ ] The exact focal architecture regression returns its first `PASS`, with
  complete stdout/stderr, TRX and exit code retained durably.
- [ ] `Doctor` and `Quick` return their first `PASS` in order.
- [ ] Exactly one online `Full` returns its first `PASS`, only after every
  preceding stage passes; no retry or in-line correction follows a failure.
- [x] Plan, current state and append-only history record the first factual
  results without Human Gate, activation or lifecycle claims.
- [x] One focused local commit contains only `AUD-2026-R1-R5-R2` factual
  reconciliation.

### Completion criteria — preserved `AUD-2026-R1-R5-R1`

- [x] The single canonical initial shutdown returned `PASS`, and branch, exact
  HEAD and the complete non-ignored tree matched the authorised clean baseline.
- [ ] Readiness parsing safely ignores non-JSON lines, JSON `null`, objects
  without `marker` and non-exact marker values under inherited StrictMode.
- [x] The runner uses a protected property lookup and exact case-sensitive
  marker comparison without direct `$candidate.marker` access.
- [ ] The focal architecture regression requires the protected lookup, forbids
  direct member access and returns its first `PASS`.
- [ ] `Doctor` and `Quick` return their first `PASS` in order.
- [ ] Exactly one online `Full` returns its first `PASS`, only after every
  preceding stage passes; no retry or in-line correction follows a failure.
- [x] Host, canonical marker, adjacent runners, versions, dependencies,
  lockfiles, integrities and protected material remain unchanged.
- [x] Plan, current state and append-only history record the first factual
  results without Human Gate, activation or lifecycle claims.
- [x] One focused local commit contains only `AUD-2026-R1-R5-R1` and its
  mandatory factual evidence.

### Completion criteria — preserved `AUD-2026-R1-R4`

- [x] The canonical initial shutdown and exact clean baseline check passed.
- [x] Primary sources identify `nanoid 3.3.18` as the compatible patched
  package and npm generated its exact registry identity in package-lock-only
  mode.
- [x] Dashboard `package.json`, the `postcss` dependency edge, all other
  package entries and every developer-toolchain range remained unchanged.
- [x] The focused online audit returned `found 0 vulnerabilities`; `Doctor`
  and `Quick` then returned their first `PASS` in order.
- [ ] Exactly one online `Full` returns `PASS`. Its sole execution confirmed
  the npm correction but returned `FAIL` in the later STATE-06 consolidated
  E2E runner and cannot be retried in this lot.
- [x] Protected external material remained unread, no ignored residue was
  deleted and no prohibited product or external action occurred.
- [x] Plan, current state and append-only history record the first factual
  results without Human Gate, activation or lifecycle claims.
- [x] One focused local commit contains only `AUD-2026-R1-R4` and its factual
  evidence.

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
| `DF-011` | `P1` | I7's first .NET resolver silently skipped a discovered executable when `--version` failed, which could erase the first factual failure. | Resolved before execution: each entry point now resolves one host and stops immediately on command, format or range failure. |
| `DF-012` | `P2` | Automatic enumeration under the ignored `.dotnet/toolchains/` root could execute an untracked binary and required unimplemented multi-candidate selection coverage. | Resolved before execution: ignored toolchains are never enumerated; PATH or the explicit CI path owns the single host and `global.json` owns SDK roll-forward. |
| `DF-013` | `P2`/`P3` | The live ledger mixed historical and current envelopes, pre-completed I7 criteria, paraphrased exact authority and used one mixed-language term. | Resolved before execution by separating v1/v2/v3, preserving literal authority, adding pending I7 criteria and an append-only clarification, and correcting pt-BR prose. |
| `DF-014` | `P1` | The first I7 `Doctor` failed while parsing `package-lock.json`: `ConvertFrom-Json` rejected the root package key whose name is an empty string without `-AsHashtable`. | `RESOLVED` only under separately authorised I7-R1: hashtable parsing plus required empty-key lookup passed `94` focused assertions and the new `Doctor`. The historical I7 failure remains unchanged. |
| `DF-015` | `P1` | The first I7-R1 `Quick` failed in `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`: the expected substring `state06-consolidated-e2e:` was absent. | `OPEN`. The I7-R1 stop rule prohibits diagnosis, correction or retry; `Full` remains `NOT_RUN`. |
| `AUD-R1-001` | `P1` | `.gitignore` pattern `*.sqlite` also ignores new files beneath the canonical `DBNotifier.Persistence.Agent.Sqlite` source tree on the authorised Windows Git boundary. | `RESOLVED` in the candidate; the first focused regression passed both source visibility and twelve runtime-state ignore probes. |
| `AUD-R1-002` | `P1` | The Markdown gate walks the physical repository and can traverse ignored protected external roots instead of using the Git-owned corpus. | `RESOLVED` in the candidate; the focused synthetic regression passed and the live Git-owned corpus passed `981` links in `226` files. |
| `AUD-R1-003` | `P1` | The architecture test still asserts the removed pre-consolidation CI jobs and diagnostic uploads. | `RESOLVED` in the candidate; the exact focused architecture test passed `1/1` without changing the workflow. |
| `AUD-R1-004` | `P1` | The first static review found that destinations and symbolic source paths could still cross the repository or clean-room boundary before physical access. | Resolved before executable validation by inventory-admitting destinations, rejecting tracked symbolic entries and inspecting every worktree path component without symbolic traversal. |
| `AUD-R1-005` | `P2` | The corrected gate initially treated Windows drive paths as external schemes and interpreted relative backslashes differently across operating systems. | Resolved before executable validation by failing closed on raw and encoded drive, UNC and backslash forms, with focused regressions. |
| `AUD-R1-006` | `P1` | The `AUD-2026-R1` plan control used `- Frozen baseline:` instead of the policy-required literal `- Initial baseline:`. | `RESOLVED` only under separately authorised `AUD-2026-R1-R1`; the historical first `Quick FAIL` and its stop rule remain unchanged. |
| `AUD-R1-007` | `P1` | The first post-failure factual review found one newly inserted blank line inside the append-only historical prefix. | `RESOLVED` by removing only that uncommitted line; final review confirmed one append hunk at end of file and `P0=0`, `P1=0`, `P2=0`, `P3=0`. |
| `AUD-R1-R1-001` | `P1` | The policy-required control key `- Initial baseline:` was absent because the plan used `- Frozen baseline:`. | `RESOLVED` under separate authority by the exact one-line rename; the focused verifier passed `105` assertions and the first `Quick` passed. Historical `AUD-R1-006` remains preserved. |
| `AUD-R1-R1-002` | `P1` | The sole online `Full` legacy-compatibility stage could not resolve .NET SDK `10.0.302` while inventorying target frameworks for `DBNotifier.Domain.csproj`. | `RESOLVED` only under separately authorised `AUD-2026-R1-R3-R1` by exact host propagation. The historical `AUD-2026-R1-R1` `FAIL`, `Failed=1`, `Pending=0` and `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE` remain unchanged. |
| `AUD-R1-R3-R1-001` | `P1` | The sole online `Full` Dashboard dependency audit reported one high-severity advisory for `nanoid <3.3.18` (`GHSA-2v37-7h3g-55p8`). | `RESOLVED` only under separately authorised `AUD-2026-R1-R4`: npm generated the `3.3.18` lock identity, the focused audit and the next sole `Full` both reported zero vulnerabilities. The historical `AUD-2026-R1-R3-R1` `FAIL` remains unchanged. |
| `AUD-R1-R4-001` | `P1` | The sole online `Full` reached `scripts/run-state06-consolidated-e2e.ps1:254` and failed because one candidate object did not expose the accessed `marker` property. | `OPEN`; the aggregate gate returned `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`. Executable diagnosis, source/test correction and another gate attempt are outside this lot. |
| `AUD-R1-R5-R1-001` | `P1` | Under inherited StrictMode, direct `$candidate.marker` access turns a null or property-less parsed stdout candidate into a terminating runner error before the exact readiness record can be admitted. | `IMPLEMENTED_UNVERIFIED`: the frozen candidate uses null-guarded `PSObject.Properties['marker']` lookup, string-type admission and `-ceq`, but its sole focal execution has no recoverable verdict. The historical `AUD-2026-R1-R4` `FAIL` remains unchanged. |

## Increment plan

| Increment | Scope | Status |
|---|---|---|
| `I1` | Protect external reference material and establish the live plan. | `COMPLETE` |
| `I2` | Implement shutdown assertion, development entry point, aggregate gate and policy tests. | `COMPLETE` |
| `I3` | Delegate CI and document the developer workflow. | `COMPLETE` |
| `I4` | Reconcile governance, templates and method ownership. | `COMPLETE` |
| `I5` | Validate, review independently, reconcile factual records and commit. | `COMPLETE` |
| `I6` | Provision the exact isolated toolchains and execute preflight, `Doctor`, `Quick` and one online `Full`. | `BLOCKED` |
| `I7` | Replace exact toolchain pins with bounded compatible ranges and resume the preserved canonical sequence. | `BLOCKED` |
| `I7-R1` | Correct empty-name root-lockfile parsing and execute the newly authorised canonical sequence. | `BLOCKED` |
| `AUD-2026-R1` | Restore Git/clean-room inventory integrity and align the stale architecture regression before executing the authorised gate sequence. | `BLOCKED` |
| `AUD-2026-R1-R1` | Restore the mandatory initial-baseline plan control and resume the exact sequential gate attempt. | `BLOCKED` |
| `AUD-2026-R1-R3-R1` | Recover the stopped shutdown boundary, propagate exact legacy SDK identity and execute the separately authorised sequential gate attempt. | `BLOCKED` |
| `AUD-2026-R1-R4` | Update only the vulnerable transitive nanoid lock identity and execute the sequential gate attempt. | `BLOCKED` |
| `ARCH-2026-R1` | Formalise the proposed Web, API, data and Linux delivery topology without implementation or lifecycle progression. | `COMPLETE` |
| `AUD-2026-R1-R5-R1` | Guard consolidated readiness parsing under inherited StrictMode and execute the separately authorised sequential gate attempt. | `BLOCKED` |
| `AUD-2026-R1-R5-R2` | Recover the committed candidate's validation with durable first-result capture and no implementation or test change. | `BLOCKED` |

## Evidence log

| Date | Scope | Command or review | Result | Limitation |
|---|---|---|---|---|
| 2026-08-28 | `AUD-2026-R1-R5-R2` shutdown preflight | `scripts/assert-dbnotifier-shutdown.ps1` | `PASS`; exit code `0`; matching processes `0`, owned listeners `0` | Executed exactly once before baseline inspection; no product, provider, database or ordinary browser runtime was started. |
| 2026-08-28 | `AUD-2026-R1-R5-R2` baseline and envelope freeze | Branch, `HEAD`, index, tracked worktree and complete non-ignored untracked inventory | `PASS`; clean `main@dcd3d24064e0709981e8ba0cff223de6a2c35565` | Implementation, tests, historical first results and negative scope remain frozen. |
| 2026-08-28 | `AUD-2026-R1-R5-R2` frozen candidate identity | SHA-256 of the consolidated runner and focal architecture test | `PASS`; runner `F88CC8B090967CE666CA57C62BE8949D37B91C1797CF9D1D1B2E1E40F3FFBFF1`; test `0F2CD49EEB1D87C7986F6F60F793D8E4E8F2603F0793756C00029B0A614857BF` | `git status` after the stopped launch listed only `PLANS.md`; implementation and tests were not altered. |
| 2026-08-28 | `AUD-2026-R1-R5-R2` durable-capture wrapper launch | Attempt to start the sole focal command with sanitised console, TRX and exit-code capture | `BLOCKED`; `ISOLATION_FAILURE`; the local execution boundary rejected `CreateProcess` before any command ran | No PowerShell or test process was created, and `.dotnet/evidence/AUD-2026-R1-R5-R2/` remained absent. The wrapper was not corrected or retried. |
| 2026-08-28 | `AUD-2026-R1-R5-R2` authorised validation sequence | Exact focal test, `Doctor`, `Quick` and sole conditional online `Full` | `NOT_RUN` | The blocked wrapper launch activated the stop rule before the focal test; no later allowance was consumed. |
| 2026-08-28 | `AUD-2026-R1-R5-R1` shutdown preflight | `scripts/assert-dbnotifier-shutdown.ps1` | `PASS`; exit code `0`; matching processes `0`, owned listeners `0` | Executed exactly once before baseline inspection; no product, provider, database or ordinary browser runtime was started. |
| 2026-08-28 | `AUD-2026-R1-R5-R1` baseline and envelope freeze | Branch, `HEAD`, index, tracked worktree and complete non-ignored untracked inventory | `PASS`; clean `main@34e5f3358491a1eb52b508c0170d6a9ac3169bc4` | The prior aggregate `FAIL`, protected material and negative scope remain unchanged. |
| 2026-08-28 | `AUD-2026-R1-R5-R1` implementation and static candidate review | Protected readiness lookup, exact marker admission, focal source-contract regression, owned diff and frozen-path comparison | `PASS`; zero direct `$candidate.marker` occurrence, zero frozen-path diff, `git diff --check` exit code `0` | Static evidence precedes and does not replace the one authorised focal test or canonical sequence. |
| 2026-08-28 | `AUD-2026-R1-R5-R1` focal architecture regression | One `dotnet test` invocation filtered to `State06ConsolidatedHarnessIsolationTests.ConsolidatedRunnerIsExactAndOffline` | `BLOCKED`; `ISOLATION_FAILURE`; final verdict and exit code were not retained | The preserved stream proves candidate build and reports one matching test file only. Read-only same-execution recovery found no matching process and no durable TRX or log. The command was not retried, and no `PASS` or test `FAIL` is inferred. |
| 2026-08-28 | `AUD-2026-R1-R5-R1` canonical continuation | One `Doctor`, one `Quick` and the sole conditional online `Full` | `NOT_RUN` | The unprovable focal result activated the stop rule before `Doctor`; no later allowance was consumed. |
| 2026-08-28 | `ARCH-2026-R1` shutdown preflight | `scripts/assert-dbnotifier-shutdown.ps1` | `PASS`; matching processes `0`, owned listeners `0` | No product runtime, database, container, browser or external resource was started. |
| 2026-08-28 | `ARCH-2026-R1` baseline and scope freeze | Branch, `HEAD`, index, tracked worktree and complete non-ignored untracked inventory | `PASS`; initially clean `main@0b09bd62863ad71fb1fcde48c6f47851b2ee0e69`; candidate limited to the three authorised paths | Protected and ignored material was not inspected; `AUD-2026-R1-R5-R1` was neither incorporated nor altered. |
| 2026-08-28 | `ARCH-2026-R1` independent review and corrected re-review | Read-only architecture, factual, language, link and scope review | Initial `P0=0`, `P1=0`, `P2=3`, `P3=0`; final `P0=0`, `P1=0`, `P2=0`, `P3=0` | The reviewer changed no file, ran no canonical check and did not consult external links. |
| 2026-08-28 | `ARCH-2026-R1` deterministic documentary validation | Development-flow policy, code-documentation, Markdown-link and available-history secret verifiers | `PASS` on first execution; `105` policy assertions, `439` comment-capable source files and `990` local links in `227` Markdown files | No build, product test, runtime or external link request was performed; final Git whitespace and staged-scope evidence follows the ledger freeze. |
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
| 2026-08-27 | `I6` source provenance | Official .NET 10 release metadata and Node.js `24.18.0` signed-release checksum catalogue | `PASS`; the selected win-x64 archives were present and their published hashes were captured | Provenance evidence does not replace archive-integrity or executable-version checks. |
| 2026-08-27 | `I6` isolated provisioning | SHA-512/SHA-256 verification, isolated extraction and executable version checks | `PASS`; .NET SDK `10.0.302`, Node.js `24.18.0` and bundled npm `11.16.0` were observed under `.dotnet/toolchains/` | Toolchains remain local and ignored; repository pins and lockfiles were not changed. |
| 2026-08-27 | `I6` temporary-download cleanup | Remove the verified workspace-local `.dotnet/provisioning-i6/` directory after extraction | `BLOCKED`; `ISOLATION_FAILURE` because the execution boundary rejected the recursive removal before process creation | Two official archives remain ignored in that directory: `2` files, `334721515` bytes. No alternate deletion mechanism was attempted. |
| 2026-08-27 | `I6` canonical sequence | Shutdown preflight, `Doctor`, `Quick` and exactly one online `Full` | Preflight `PASS`; `Doctor`, `Quick` and `Full` are `NOT_RUN` | The owner-required stop-on-failure rule ended the sequence after cleanup was blocked; no canonical-gate attempt exists. |
| 2026-08-27 | `I7` primary-source review | Microsoft `global.json`, pinned setup-dotnet/setup-node v4 documentation and npm 11 package-manifest contract | `PASS` for policy design | `latestFeature` bounds .NET to the selected major/minor, setup actions accept stable channel/major selectors, and npm supports semver engines plus fail-closed `devEngines`. |
| 2026-08-27 | `I7` independent static review and recheck | PowerShell/range, Node/CI/lockfile and governance/history lanes | Initial `P1=1`, `P2=3`, `P3=1`; final `P0=0`, `P1=0`, `P2=0`, `P3=0` | Candidate-failure, ignored-binary execution, ledger, authority and language findings were corrected before executable validation. |
| 2026-08-27 | `I7` shutdown preflight | Process, executable-path, command-line, parentage and listener inventory | `PASS`; matching processes `0`, owned listeners `0` | No product runtime, database/provider or browser was started. |
| 2026-08-27 | First I7 `Doctor` | `scripts/development.ps1 Doctor` | `FAIL`; exit code `1` | Repository root, lock files and restored dependencies passed. Toolchain policy parsing failed with `The provided JSON includes a property whose name is an empty string, this is only supported using the -AsHashTable switch.` The result was not retried or corrected. |
| 2026-08-27 | Remaining I7 canonical sequence | `Quick` and exactly one online `Full` | `NOT_RUN` | The first `Doctor` failure activated the mandatory stop rule. The online `Full` allowance remains unused. |
| 2026-08-27 | I7 closing preflight | Process, executable-path, command-line, parentage and listener inventory after the stopped sequence | `PASS`; matching processes `0`, owned listeners `0` | No DB-Notifier-owned process or listener remained. |
| 2026-08-27 | `I7-R1` preflight discovery | `rg --files scripts` narrowed to shutdown/preflight names | `PASS`; canonical entry point `scripts/assert-dbnotifier-shutdown.ps1` | The preceding misspelled filename did not execute a preflight or mutate the repository; the owner separately authorised this new attempt. |
| 2026-08-27 | `I7-R1` shutdown preflight | `scripts/assert-dbnotifier-shutdown.ps1` | `PASS`; matching processes `0`, owned listeners `0` | Executed exactly once under the new authority; no product runtime was started. |
| 2026-08-27 | `I7-R1` baseline and protected hashes | `git status`, `git rev-parse HEAD`, SHA-256 of three protected manifests | `PASS`; `main@fc7001240f87a3ee555b9e53cc98d8c0c57b4ce5`, clean tracked worktree | The ignored I6 residue and protected external source were neither read nor changed. |
| 2026-08-27 | `I7-R1` parser correction | Hashtable JSON parsing and required-property support for npm's empty root key | `PASS` for the authorised implementation | Only the policy helper and focused regression changed; compatible ranges and protected manifests did not. |
| 2026-08-27 | `I7-R1` protected-hash recheck | SHA-256 comparison for `global.json`, Dashboard `package.json` and `package-lock.json` | `PASS`; all three hashes identical to the frozen values | Proves byte identity of the protected manifests, not dependency freshness. |
| 2026-08-27 | `I7-R1` focused regression | `tests/DBNotifier.DevelopmentFlow.Tests.ps1` | `PASS`; `94` assertions, exit code `0` | Executed once; proves the bounded development-flow policy only. |
| 2026-08-27 | `I7-R1 Doctor` | `scripts/development.ps1 Doctor` | `PASS`; exit code `0` | Internal preflight, repository root, compatible toolchains, lockfiles and restored dependencies all passed. |
| 2026-08-27 | `I7-R1 Quick` | `scripts/development.ps1 Quick` | `FAIL`; exit code `1`; explicitly `NON_GATE` | Release build passed with `0` warnings and `0` errors; unit tests passed `528/528`; architecture tests passed `99/100` and failed the exact `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources` assertion because `state06-consolidated-e2e:` was not found. No retry or diagnosis followed. |
| 2026-08-27 | `I7-R1 Full` | Exactly one online `scripts/development.ps1 Full` allowance | `NOT_RUN` | The first `Quick` failure activated the stop rule; the online `Full` allowance remains unused. |
| 2026-08-27 | `AUD-2026-R1` shutdown preflight and baseline | `scripts/assert-dbnotifier-shutdown.ps1`; `git rev-parse HEAD`; branch and full worktree status | `PASS`; matching processes `0`, owned listeners `0`; clean `main@3762f71c116af206b911a086b836cef11cd1894d` | No protected material, product runtime, browser, database/provider or external action was touched. |
| 2026-08-27 | `AUD-2026-R1` first independent candidate review | Read-only inspection of the authorised diff and frozen contracts; no executable operation | `P0=0`; `P1=1`; `P2=3`; `P3=1` | All findings were corrected before executable validation; the review did not read protected material. |
| 2026-08-27 | `AUD-2026-R1` corrected independent review | Read-only inspection after Git-boundary, regression and plan corrections; no executable operation | `P0=0`; `P1=0`; `P2=1`; `P3=0` | The remaining cross-platform path-classification finding was corrected before executable validation. |
| 2026-08-27 | `AUD-2026-R1` final independent re-review and candidate freeze | Read-only inspection of raw/encoded drive, UNC and backslash handling | `P0=0`; `P1=0`; `P2=0`; `P3=0` | Candidate frozen for the first focused executable check; later `FAIL` or `BLOCKED` prohibits retry and in-line correction. |
| 2026-08-27 | `AUD-2026-R1` focused Git-boundary regressions | `node --experimental-strip-types --test tests/markdownLinks.test.ts` | `PASS`; exit code `0`; `2/2` tests | Executed once; proves the bounded SQLite ignore and Markdown clean-room cases only. |
| 2026-08-27 | `AUD-2026-R1` focused live Markdown inventory | `node ../../scripts/verify-markdown-links.mjs` | `PASS`; exit code `0`; `981` local links in `226` files | Executed once against the non-ignored Git-owned corpus; no external link or protected root was followed. |
| 2026-08-27 | `AUD-2026-R1` focused architecture regression | Exact `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources` filter | `PASS`; exit code `0`; `1/1` test | Executed once; compiled on .NET 10 and did not mutate the frozen workflow. |
| 2026-08-27 | `AUD-2026-R1 Doctor` | `scripts/development.ps1 Doctor` | `PASS`; exit code `0` | Internal preflight reported zero matching processes/listeners; root, toolchains, lockfiles and restored dependencies passed. |
| 2026-08-27 | `AUD-2026-R1 Quick` | `scripts/development.ps1 Quick` | `FAIL`; exit code `1`; explicitly `NON_GATE` | Build passed with `0` warnings/errors; unit tests passed `528/528`; architecture tests `100/100`; Node tests `74/74`; asset, type, documentation and Markdown checks passed. Development-flow policy then failed because `PLANS.md` lacks required control `- Initial baseline:`. No retry or in-line correction followed. |
| 2026-08-27 | `AUD-2026-R1 Full` | Exactly one online `scripts/development.ps1 Full` allowance | `NOT_RUN` | The first `Quick` failure activated the stop rule; the online allowance remains unused and no canonical gate disposition exists for this lot. |
| 2026-08-27 | `AUD-2026-R1` closing shutdown | `scripts/assert-dbnotifier-shutdown.ps1` | `PASS`; exit code `0`; matching processes `0`, owned listeners `0` | Safe closure only; it was not a gate retry and started no product runtime. |
| 2026-08-27 | `AUD-2026-R1` first post-failure factual review | Read-only comparison of plan, current state, append-only history and preserved results | `P0=0`; `P1=1`; `P2=0`; `P3=0` | All execution facts were correct; one added blank line interrupted the byte-identical historical prefix. |
| 2026-08-27 | `AUD-2026-R1` final factual re-review | Read-only append-boundary inspection after removing only the uncommitted blank line | `P0=0`; `P1=0`; `P2=0`; `P3=0` | History differs from baseline only through the new EOF append; no executable validation was repeated. |
| 2026-08-27 | `AUD-2026-R1-R1` shutdown and continuation baseline | `scripts/assert-dbnotifier-shutdown.ps1`; branch, commit and full non-ignored worktree status | `PASS`; matching processes `0`, owned listeners `0`; clean `main@b60ef4d302e4c4dc3f0e474be27eaa4b8c6beb13` | Executed before editing; protected material and ignored residue were not inspected. |
| 2026-08-27 | `AUD-2026-R1-R1` exact control correction | Rename `- Frozen baseline:` to `- Initial baseline:` in the control record | `PASS` for the authorised one-line implementation | Original value `main@3762f71c116af206b911a086b836cef11cd1894d` was preserved; no other file changed before validation. |
| 2026-08-27 | `AUD-2026-R1-R1` focused policy check | `scripts/verify-development-flow.ps1` | `PASS`; exit code `0`; `105` assertions | Executed once; it proves the development-flow policy only. |
| 2026-08-27 | `AUD-2026-R1-R1 Doctor` | `scripts/development.ps1 Doctor` | `PASS`; exit code `0` | Internal shutdown preflight reported zero matching processes/listeners; root, toolchains, lockfiles and restored dependencies passed. |
| 2026-08-27 | `AUD-2026-R1-R1 Quick` | `scripts/development.ps1 Quick` | `PASS`; exit code `0`; explicitly `NON_GATE` | Executed once. Release build passed with `0` warnings/errors; unit `528/528`, architecture `100/100`, Node `74/74`, policy `105`, policy tests `94`, runner tests `68` and syntax tests `13` passed with their applicable asset/type/documentation/Markdown checks. |
| 2026-08-27 | `AUD-2026-R1-R1 Full` pre-failure evidence | Sole online `scripts/development.ps1 Full` execution | `PASS` through secret scan, policy, locked restore/build, .NET test, coverage, vulnerability and fail-closed runtime-audit stages | Restore covered `19` projects; build was `0` warnings/errors; architecture `100/100`, WPF `10/10`, unit `528/528`, integration `168/168`; coverage lines `83.41%`, branches `56.62%`, required components `10`; NuGet vulnerability gate passed `19` projects; runtime audit observed live `200`, protected endpoints `426`, disabled Agent workers, no command polling and no local-persistence initialisation. These partial passes do not replace the final disposition. |
| 2026-08-27 | `AUD-2026-R1-R1 Full` first and only disposition | Legacy compatibility within the same online `Full` | `FAIL`; exit code `1`; `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE` | Legacy script syntax passed `27` PowerShell scripts with `21` declared skips. The compatibility test then reported SDK `10.0.302` unavailable in its child environment while inventorying `DBNotifier.Domain.csproj`; `Failed=1`, `Pending=0`. No retry, diagnosis execution or correction followed. |
| 2026-08-27 | `AUD-2026-R1-R1` closing shutdown | `scripts/assert-dbnotifier-shutdown.ps1` | `PASS`; exit code `0`; matching processes `0`, owned listeners `0` | Safe closure only; it was not a gate retry and started no subsequent validation. |
| 2026-08-27 | `AUD-2026-R1-R1` independent factual review and recheck | Read-only review of plan, current state, EOF-only history append and historical-tense reconciliation | `P0=0`; `P1=0`; `P2=0`; `P3=0` | Confirmed only three factual documents changed, the historical prefix is byte-preserved, partial passes do not replace `Full FAIL`, and the original `AUD-2026-R1 Quick FAIL` remains unchanged. No executable validation occurred. |
| 2026-08-27 | First `AUD-2026-R1-R3` shutdown preflight | `scripts/assert-dbnotifier-shutdown.ps1` | `BLOCKED`; exit code `1`; `pid=8952`; `process=pwsh.exe`; `ISOLATION_FAILURE` | The lot stopped before baseline inspection or editing. No retry, process termination or executable validation followed under that authority. |
| 2026-08-27 | `AUD-2026-R1-R3-R1` PID recovery | Exact PID existence query without environment access | `PASS`; PID `8952` was absent | No process was terminated and no alternate identity was inspected. |
| 2026-08-27 | `AUD-2026-R1-R3-R1` recovery shutdown and baseline | One shutdown preflight; branch, HEAD and tracked status | `PASS`; matching processes `0`, owned listeners `0`; clean `main@6ecc72f7a347a746d0153072580c527d7c679e81` | Executed before the plan or implementation changed; protected material and ignored residue were not inspected. |
| 2026-08-27 | `AUD-2026-R1-R3-R1` legacy SDK identity correction | Exact host propagation through CI, Windows PowerShell runner, Pester parameters and seven verifier calls | `PASS` for the authorised implementation | Only the four authorised script/test files changed; `development.ps1`, the verifier, `global.json`, workflow, versions, lockfiles, dependencies and product code remained frozen. |
| 2026-08-27 | `AUD-2026-R1-R3-R1` focused policy check | `scripts/verify-development-flow.ps1` | `PASS`; exit code `0`; `105` assertions | Executed once; it proves the bounded development-flow policy only. |
| 2026-08-27 | `AUD-2026-R1-R3-R1` focused legacy runner | `scripts/run-legacy-tests.ps1` under Windows PowerShell 5.1 with the absolute PATH-resolved dotnet host | `PASS`; exit code `0`; tests `34`, skipped `1`, coverage `35.17%` (`338/961`) | Executed once; the single skip was the expected conditional `pg_isready` case. |
| 2026-08-27 | `AUD-2026-R1-R3-R1 Doctor` | `scripts/development.ps1 Doctor` | `PASS`; exit code `0` | Internal shutdown preflight, root, compatible toolchains, lockfiles and restored dependencies passed. |
| 2026-08-27 | `AUD-2026-R1-R3-R1 Quick` | `scripts/development.ps1 Quick` | `PASS`; exit code `0`; explicitly `NON_GATE` | Build was clean; unit `528/528`, architecture `100/100`, Node `74/74`, policy `105`, policy regressions `98`, runner `68` and syntax `13` passed with applicable asset, type, documentation and Markdown checks. |
| 2026-08-27 | `AUD-2026-R1-R3-R1 Full` pre-failure evidence | Sole online `scripts/development.ps1 Full` execution | `PASS` through corrected legacy compatibility, bundle validation, Dashboard tests and Dashboard production build | Earlier stages included two preflights, secret scan, policies, locked restore of `19` projects, build with zero warnings/errors, architecture `100/100`, WPF `10/10`, unit `528/528`, integration `168/168`, coverage `83.41%` lines/`56.62%` branches/`10` components, NuGet vulnerability coverage for `19` projects, fail-closed runtime audit, legacy `34` tests and Node `74/74`. Partial passes do not replace the final disposition. |
| 2026-08-27 | `AUD-2026-R1-R3-R1 Full` first and only disposition | Dashboard online dependency audit within the same `Full` | `FAIL`; exit code `1`; `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE` | `npm audit` reported one high-severity advisory for `nanoid <3.3.18`, `GHSA-2v37-7h3g-55p8`. No dependency change, retry, diagnosis execution or correction followed. |
| 2026-08-27 | `AUD-2026-R1-R3-R1` closing shutdown | `scripts/assert-dbnotifier-shutdown.ps1` | `PASS`; exit code `0`; matching processes `0`, owned listeners `0` | Safe closure only; it was not a gate retry and started no later validation. |
| 2026-08-27 | `AUD-2026-R1-R4` initial shutdown and baseline | `scripts/assert-dbnotifier-shutdown.ps1`; branch, commit, worktree and toolchain inspection | `PASS`; matching processes `0`, owned listeners `0`; clean `main@0f59408440dc1c5877f8d3de0de8859ef9bc7fed`; .NET `10.0.400`, Node `24.19.0`, npm `11.17.0` | All toolchains satisfied their bounded stable ranges; protected material and ignored residue were not inspected. |
| 2026-08-27 | `AUD-2026-R1-R4` advisory and package provenance | GitHub Advisory Database `GHSA-2v37-7h3g-55p8`; npm registry metadata for `nanoid@3.3.18` | `PASS`; `<3.3.18` affected, `3.3.18` patched; registry tarball and SHA-512 integrity matched the generated lock entry | Primary-source review proves the selected patch identity, not the aggregate repository gate. |
| 2026-08-27 | `AUD-2026-R1-R4` generated lockfile correction | `npm update nanoid --package-lock-only --ignore-scripts --no-audit --no-fund` | `PASS`; exit code `0`; only nanoid `version`, `resolved` and `integrity` changed from `3.3.16` to `3.3.18` | Dashboard `package.json` remained byte-identical at SHA-256 `5a137255c337ab1a159e797dd7187bcfdbcdb70ca75c73c0cf43faf5f3a917a8`; the `postcss` edge remained `^3.3.16`. No product component was installed. |
| 2026-08-27 | `AUD-2026-R1-R4` focused dependency audit | `npm audit --audit-level=high` | `PASS`; exit code `0`; `found 0 vulnerabilities` | Executed once before the canonical sequence; registry freshness is bounded to this observation. |
| 2026-08-27 | `AUD-2026-R1-R4 Doctor` | `scripts/development.ps1 Doctor` | `PASS`; exit code `0` | Internal shutdown preflight, root, compatible toolchains, lockfiles and restored dependencies passed. |
| 2026-08-27 | `AUD-2026-R1-R4 Quick` | `scripts/development.ps1 Quick` | `PASS`; exit code `0`; explicitly `NON_GATE` | Executed once. Build passed with zero warnings/errors; unit `528/528`, architecture `100/100`, Node `74/74`, policy `105`, policy regressions `98`, runner `68` and syntax `13` passed with applicable asset, type, documentation and Markdown checks. |
| 2026-08-27 | `AUD-2026-R1-R4 Full` pre-failure evidence | Sole online `scripts/development.ps1 Full` execution | `PASS` through the Dashboard vulnerability and isolated browser-audit stages | Earlier stages included two preflights, secret scan, policies, locked restore of `19` projects, build with zero warnings/errors, architecture `100/100`, WPF `10/10`, unit `528/528`, integration `168/168`, coverage `83.41%` lines/`56.62%` branches/`10` components, NuGet vulnerability coverage for `19` projects, fail-closed runtime audit, legacy `34` tests, Node `74/74`, Dashboard build and `found 0 vulnerabilities`. The isolated browser audit passed `128` viewport, `96` forced-colour and `24` focal zoom/reflow samples. Partial passes do not replace the final disposition. |
| 2026-08-27 | `AUD-2026-R1-R4 Full` first and only disposition | STATE-06 consolidated E2E runner within the same online `Full` | `FAIL`; exit code `1`; `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE` | At `scripts/run-state06-consolidated-e2e.ps1:254`, PowerShell reported `The property 'marker' cannot be found on this object.` No retry, executable diagnosis or in-line correction followed. |
| 2026-08-27 | `AUD-2026-R1-R4` closing shutdown | `scripts/assert-dbnotifier-shutdown.ps1` | `PASS`; exit code `0`; matching processes `0`, owned listeners `0` | Safe closure only; it was not a gate retry and started no later validation. |

## Blockers and limitations

- `AUD-2026-R1-R5-R2` is `BLOCKED` by `ISOLATION_FAILURE`. The local execution
  boundary rejected the durable-capture wrapper before `CreateProcess`; no
  PowerShell or test process started and the owned evidence boundary remained
  absent.
- The exact focal test, `Doctor`, `Quick` and online `Full` are all `NOT_RUN`.
  Reissuing or simplifying the invocation would be an unauthorised retry or
  in-line correction under this lot's explicit stop rule.
- `AUD-2026-R1-R5-R1` is `BLOCKED` by `ISOLATION_FAILURE`. Its sole focal test
  command built the candidate and reported one matching test file, but the
  execution channel did not retain the final verdict or exit code.
- Read-only recovery found no matching process and no durable result artefact.
  Repeating the focal command would violate the explicit no-retry boundary, so
  the candidate has static evidence only; `Doctor`, `Quick` and `Full` remain
  `NOT_RUN`.
- `AUD-2026-R1-R4` is `BLOCKED` by the first and only online `Full` result.
  The exact generated lockfile correction, focused audit, `Doctor` and `Quick`
  all passed before the canonical gate began.
- The sole `Full` proved the prior npm-advisory correction with
  `found 0 vulnerabilities` and passed the isolated Dashboard browser audit.
  The later STATE-06 consolidated E2E runner then accessed a missing `marker`
  property at `scripts/run-state06-consolidated-e2e.ps1:254`, forcing
  `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`.
- Executable diagnosis, runner/test correction and any retry require separate
  authority. No partial stage pass converts the aggregate result.
- `AUD-2026-R1-R3-R1` is `BLOCKED` by the first and only online `Full` result.
  The recovery, implementation, focused policy check, corrected legacy runner,
  `Doctor` and `Quick` all passed before the canonical gate began.
- The sole `Full` proved the exact legacy SDK correction and passed Dashboard
  tests and production build. The following `npm audit` reported one
  high-severity advisory for `nanoid <3.3.18`, forcing
  `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`.
- Dependency and lockfile changes were outside that historical lot. Its
  advisory was corrected only under separate `AUD-2026-R1-R4` authority, which
  does not rewrite the preserved historical `FAIL`.
- `AUD-2026-R1-R1` is `BLOCKED` by the first and only online `Full` result.
  The exact control rename, focused policy check, `Doctor` and `Quick` all
  passed before the canonical gate began.
- The sole `Full` passed every reported stage through the NuGet vulnerability
  gate and fail-closed runtime audit, including the required coverage floors.
  It then failed legacy compatibility because the child environment could not
  resolve .NET SDK `10.0.302` for MSBuild target-framework inventory;
  `Failed=1`, `Pending=0`.
- The preserved disposition is
  `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`. No retry, executable
  diagnosis, alternative environment or in-line correction is permitted in
  this envelope. Partial stage passes do not convert the aggregate result.
- `AUD-2026-R1` is `BLOCKED` by `GATE_FAILURE`. All three focused checks and
  `Doctor` passed, and the first `Quick` passed build, `528/528` unit tests,
  `100/100` architecture tests, `74/74` Node tests and its asset, type,
  documentation and Markdown checks. Its later development-flow policy check
  failed because `PLANS.md` did not then contain the required literal
  `- Initial baseline:`.
- The `AUD-2026-R1` stop rule preserves that first `Quick` result. The key was
  corrected only under separate `AUD-2026-R1-R1` authority; no retry or
  alternative `Quick` occurred in the historical lot. Its sole online `Full`
  is `NOT_RUN`, its allowance remains unused and no canonical aggregate-gate
  disposition was produced.
- I7-R1 is `BLOCKED` by `GATE_FAILURE`. Its first `Quick` exited `1` after the
  Release build and all `528` unit tests passed, because one of `100`
  architecture tests did not find the expected
  `state06-consolidated-e2e:` substring. No diagnosis, correction or retry is
  part of the current envelope.
- The I7-R1 parser correction itself passed `94` focused assertions and the
  new `Doctor`. The protected manifests remain byte-identical, so no range,
  dependency graph, version or integrity value changed.
- The I7-R1 stop rule leaves the sole online `Full` as `NOT_RUN`; its allowance
  remains unused and no canonical aggregate-gate disposition was produced.
- I7 is `BLOCKED` by `GATE_FAILURE`. Its first `Doctor` exited `1` because the
  PowerShell policy helper parses `package-lock.json` without the hashtable
  mode required for the root package key whose name is an empty string.
- The stop-on-failure rule preserves that first `Doctor` result and leaves
  `Quick` and the sole online `Full` as `NOT_RUN`. No retry or in-place
  correction is permitted in the current envelope.
- The compatible-range candidate is materialised and static review closed at
  `P0=0`, `P1=0`, `P2=0`, `P3=0`, but executable boundary regressions, product
  build, unit/integration tests, coverage, browser/runtime matrices and online
  dependency-advisory freshness have no new passing evidence.
- The historical I6 cleanup remains `BLOCKED` by `ISOLATION_FAILURE`. Its
  ignored `.dotnet/provisioning-i6/` residue contains only the two
  hash-verified official archives (`2` files, `334721515` bytes); I7 did not
  authorise its deletion.
- Remote CI, product/provider runtime, ordinary browser use, deployment,
  publication, Human Gate, activation and lifecycle transition remain outside
  authority.

## Outcome and next action

`GOV-2026-R1` is `COMPLETE` as a project-wide governance increment. Every
owner-facing payload explicitly presented for copying now uses its own fenced
Markdown block labelled `text`; labels, reasons and instructions remain outside
the box. The rule covers the exact next message, a suggested title when the
owner is told to copy it, parallel-lane messages and worker-return messages.
Corpus `6.7.1`, coordination revision `1.4.1`, the template, Quality Gate,
current state and policy verifier agree while preserving the existing 14 fields
and closed enums. Focused policy, documentation, link and secret checks passed,
and two final independent reviews closed with zero findings at every severity;
no product, interface, provider, runtime, Human Gate, activation or lifecycle
state changed. The next action is to use the new box format in every subsequent
governed handoff; no separate project execution is required for adoption.

`AUD-2026-R1-R5-R2` is `BLOCKED` by `ISOLATION_FAILURE`. It began on clean
`main@dcd3d24064e0709981e8ba0cff223de6a2c35565`; its single initial shutdown
preflight and exact baseline freeze passed, and the committed runner and focal
test remained byte-identical. The local execution boundary then rejected the
durable-capture wrapper before `CreateProcess`. No PowerShell or test process
started, no owned evidence directory was created and the focal test is
`NOT_RUN`. No retry or in-line correction followed; `Doctor`, `Quick` and the
conditional online `Full` are also `NOT_RUN`. `STATE-06` and every protected
boundary remain unchanged.

`AUD-2026-R1-R5-R1` is `BLOCKED` by `ISOLATION_FAILURE`. It began on clean
`main@34e5f3358491a1eb52b508c0170d6a9ac3169bc4`; its single initial shutdown
preflight, baseline freeze, minimum implementation and static candidate review
passed. The sole focal command built the candidate and reported one matching
test file, but its final verdict and exit code were not retained. Read-only recovery
found neither a matching process nor a durable result artefact. No retry or
in-line correction followed, and `Doctor`, `Quick` and the conditional online
`Full` are `NOT_RUN`. The implementation therefore remains a statically reviewed
but executably unverified candidate; `STATE-06` and every protected boundary are
unchanged.

`ARCH-2026-R1` is `COMPLETE` as a documentary increment. ADR-0009 records the
revised Web, API, data and Linux delivery topology at status `proposed`; the
architecture index exposes that status without granting implementation, and
this plan preserves the exact baseline, negative scope, review and validation
evidence. The final independent review closed at `P0=0`, `P1=0`, `P2=0`,
`P3=0`, and all four deterministic documentary verifiers passed on their first
execution. No source, configuration, dependency, schema, runtime, external
resource, factual state, Human Gate or lifecycle position changed. The next
action is the owner's separate review of the exact proposed ADR revision; no
decision is inferred by this completed preparation.

`AUD-2026-R1-R4` is `BLOCKED` by `GATE_FAILURE`. npm generated only the
compatible `nanoid 3.3.18` lock identity, while the Dashboard manifest,
dependency edge and developer-toolchain ranges remained unchanged. The
focused audit, `Doctor` and `Quick` passed. The sole online `Full` confirmed
zero npm vulnerabilities and passed all reported stages through the isolated
Dashboard browser audit, then failed in the later STATE-06 consolidated E2E
runner on a missing `marker` property. The aggregate result is `FAIL`; no
retry, executable diagnosis or in-line correction followed, and the closing
shutdown found zero matching processes and zero owned listeners.

`AUD-2026-R1-R3-R1` is `BLOCKED` by `GATE_FAILURE`. It recovered the transient
shutdown residue without terminating a process, proved the exact authorised
baseline, propagated the canonical dotnet host through the legacy boundary and
passed its focused policy check, corrected legacy runner, `Doctor` and `Quick`.
The sole online `Full` also proved the correction in its canonical position,
then failed the later Dashboard dependency audit on one high-severity
`nanoid <3.3.18` advisory. The aggregate result is `FAIL`; no retry, dependency
change or in-line correction followed, and the closing shutdown found zero
matching processes and zero owned listeners.

`AUD-2026-R1-R1` is `BLOCKED` by `GATE_FAILURE`. Its exact one-line control
correction passed the focused policy verifier with `105` assertions, `Doctor`
passed and the first `Quick` passed as `NON_GATE`. The sole online `Full`
passed secret scanning, policy, locked restore/build, .NET tests, coverage,
NuGet vulnerability and fail-closed runtime-audit evidence before legacy
compatibility failed to resolve .NET SDK `10.0.302` for MSBuild inventory. The
canonical aggregate result is `FAIL`, not partial success. No retry, executable
diagnosis or correction followed, and the closing shutdown reported zero
matching processes and zero owned listeners. `STATE-06 INTEGRATION`, Human
Gates, activation and external authority remain unchanged.

The original `AUD-2026-R1` first-`Quick` failure remains historical and is not
rewritten. `AUD-2026-R1-R1` proves the plan-control correction and reaches the
canonical gate, but does not convert either historical failure or its own
`Full FAIL` into a passing disposition.

Continuation `I7` remains factually `BLOCKED`; its first `Doctor` failure is
preserved. I7-R1 resolved only the authorised empty-key parser defect: the
protected hashes, `94` focused assertions and the new `Doctor` passed. I7-R1
is nevertheless `BLOCKED`, not `PASS`, because its first `Quick` failed one
architecture assertion after the Release build, `528/528` unit tests and
`99/100` architecture tests succeeded. The stop rule preserved that result,
left the online `Full` `NOT_RUN` and prohibited diagnosis or correction. No
product, provider, browser, external action, Human Gate, activation or
lifecycle authority was inferred.

## Change log

- `2026-08-28`: `GOV-2026-R1` adopted one fenced Markdown `text` block per
  owner-facing copy payload across the root instruction, coordination authority,
  template, Quality Gate, factual records and policy verifier. Corpus `6.7.1`
  and coordination revision `1.4.1` passed the focused policy, documentation,
  link and secret checks; two final independent reviews closed at zero findings
  without product or lifecycle change.
- `2026-08-28`: the owner separately authorised validation recovery
  `AUD-2026-R1-R5-R2` from clean `main@dcd3d24`. The initial preflight and
  baseline check passed, and implementation/tests remained frozen. The local
  execution boundary rejected the durable-capture wrapper before process
  creation; the lot stopped as `BLOCKED`/`ISOLATION_FAILURE` without retry.
  Focal test, `Doctor`, `Quick` and `Full` are `NOT_RUN`, with `STATE-06`
  unchanged.
- `2026-08-28`: the owner separately authorised `AUD-2026-R1-R5-R1` from clean
  `main@34e5f3`. The initial preflight and baseline check passed, and the minimum
  guarded parser plus focal source-contract regression passed static review. The
  sole focal command did not retain its final verdict or exit code; read-only
  recovery found no process or durable result artefact. The lot stopped as
  `BLOCKED`/`ISOLATION_FAILURE` without retry; `Doctor`, `Quick` and `Full` are
  `NOT_RUN`, with `STATE-06` unchanged.
- `2026-08-28`: the owner authorised documentary increment `ARCH-2026-R1` on
  clean `main@0b09bd6`. The plan, proposed ADR-0009 and architecture index were
  updated without implementation or lifecycle progression. Three initial `P2`
  review findings were corrected; final review closed at zero findings, and
  the development-flow, code-documentation, Markdown-link and secret checks
  each passed once. The protected adjacent lot remained outside this envelope.
- `2026-08-27`: the owner requested continuation of the adopted development
  method from clean `main@0f59408`. `AUD-2026-R1-R4` changed only the
  transitive `nanoid` lock identity to patched `3.3.18`; the focused audit,
  `Doctor` and `Quick` passed. The sole online `Full` confirmed zero npm
  vulnerabilities and passed the isolated browser audit, then failed the
  STATE-06 consolidated E2E runner on a missing `marker` property. The stop
  rule prohibited retry or in-line correction and left `STATE-06` unchanged.
- `2026-08-27`: the owner authorised `AUD-2026-R1-R3-R1` to recover the first
  shutdown blocker and resume only the diagnosed legacy SDK identity fix from
  clean `main@6ecc72f`. PID `8952` was already absent; the recovery preflight,
  implementation, focused checks, `Doctor` and `Quick` passed. The sole online
  `Full` proved legacy compatibility, then failed the Dashboard dependency
  audit on one high-severity `nanoid` advisory. Stop-on-failure prohibited
  retry or dependency correction and left `STATE-06` unchanged.
- `2026-08-27`: the owner separately authorised `AUD-2026-R1-R1` to rename
  only the plan's initial-baseline control and resume one sequential gate
  attempt from clean `main@b60ef4d`. The control, focused verifier, `Doctor`
  and `Quick` passed. The sole online `Full` failed legacy compatibility after
  its preceding reported stages passed; stop-on-failure prohibited retry,
  diagnosis execution or correction and left lifecycle unchanged.
- `2026-08-27`: the owner authorised `AUD-2026-R1` to correct only the Git
  ignore collision, clean-room Markdown inventory and stale consolidated-CI
  architecture regression. The mandatory preflight passed with zero matching
  process or listener, and the exact authorised baseline was clean before this
  live plan was updated. No implementation or lifecycle authority was inferred.
- `2026-08-27`: the frozen candidate closed independent static review with
  zero findings, all three focused checks and `Doctor` passed, then the first
  `Quick` failed the required `- Initial baseline:` plan-control invariant.
  The stop rule left online `Full` unexecuted and prohibited correction or
  retry; factual records only were reconciled.
- `2026-08-27`: created from the authorised baseline after read-only DB-Notifier
  and RAG-Challenge audits; no lifecycle or product authority was inferred.
- `2026-08-27`: implementation and policy evidence recorded; the envelope was
  completed without converting the exact-toolchain blocker into a pass.
- `2026-08-27`: independent rechecks closed with zero unresolved `P0`, `P1` or
  `P2`; two final `P3` wording/help clarifications were corrected before the
  focused commit.
- `2026-08-27`: the owner authorised isolated exact-toolchain provisioning and
  one sequential online canonical-gate attempt under continuation `I6`; no
  product, external runtime, Human Gate, activation or lifecycle authority was
  inferred.
- `2026-08-27`: exact toolchains and archive hashes passed, but the execution
  boundary rejected cleanup of the verified ignored staging directory before
  process creation. `I6` stopped as `BLOCKED`/`ISOLATION_FAILURE`; `Doctor`,
  `Quick` and `Full` remained `NOT_RUN`, and no retry or alternate deletion
  mechanism was attempted.
- `2026-08-27`: the owner replaced exact toolchain pins with bounded compatible
  ranges so host updates within .NET 10.0, Node 24 and npm 11 remain usable.
  Continuation `I7` reopened implementation and the preserved canonical
  sequence without granting database/provider, browser, deploy, push, Human
  Gate, activation or lifecycle authority.
- `2026-08-27`: static I7 review closed at `P0=0`, `P1=0`, `P2=0`, `P3=0` and
  shutdown preflight passed with zero matching process or owned listener. The
  first `Doctor` then failed with exit code `1` while the policy helper parsed
  the empty-name root package key in `package-lock.json`. The stop rule left
  `Quick` and the sole online `Full` `NOT_RUN`; a closing preflight again
  observed zero matching process or owned listener. I7 closed as
  `BLOCKED`/`GATE_FAILURE` without correction, retry or lifecycle change.
- `2026-08-27`: the owner separately authorised I7-R1 to correct only the
  empty-name root-lockfile parser and restart the preserved sequence. Read-only
  discovery identified `scripts/assert-dbnotifier-shutdown.ps1`; its single
  preflight passed with zero matching process and owned listener. Baseline
  `main@fc70012` was clean and the three protected manifest hashes were frozen
  before implementation.
- `2026-08-27`: I7-R1 changed only empty-name dictionary handling in the
  policy helper and its regression. All protected hashes remained identical;
  the focused suite passed `94` assertions and `Doctor` passed. The first
  `Quick` then failed one architecture assertion after a clean Release build,
  `528/528` unit tests and `99/100` architecture tests passed. The stop rule
  left online `Full` `NOT_RUN`; I7-R1 closed as `BLOCKED`/`GATE_FAILURE`
  without diagnosis, retry, prohibited runtime or lifecycle change.
