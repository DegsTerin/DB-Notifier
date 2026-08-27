# DB-Notifier Live Execution Plan

<!-- Purpose: Maintains the non-authorising execution ledger for the current broad engineering increment. -->

This file is the canonical live plan for the current broad or cross-cutting
increment. It records execution intent and evidence; it does not replace
`AGENTS.md`, the instruction corpus, an accepted ADR, `Current-State.md`, the
append-only history, a Quality Gate or a Human Gate.

## Control record

- Plan ID: `DEV-FLOW-01`
- Status: `BLOCKED`
- Created: `2026-08-27`
- Initial baseline: `main@f0f220c539fde685e2c500b4944ebca168aaec7d`
- Continuation baseline: `main@e6416f6dac3f65d672f0247da88850881bae9120`
- Lifecycle state: `STATE-06 INTEGRATION`; unchanged by this plan
- Authority: Bruno's request to implement in DB-Notifier the development
  method and flow used by RAG-Challenge
- Execution mode: `SEQUENTIAL_ONLY`
- Writer: coordinating conversation only
- Independent reviewers: three read-only discovery lanes before implementation
  and three named read-only recheck lanes after implementation
- Factual-state owner: `prompts/state/Current-State.md`
- Historical owner: `prompts/state/State-Transition-Log.md`

## Task envelopes

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

### Authorised continuation `I7` — current

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

### Authorised corrective continuation `I7-R1` — current

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

## Blockers and limitations

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
