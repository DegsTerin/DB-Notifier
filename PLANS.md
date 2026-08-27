# DB-Notifier Live Execution Plan

<!-- Purpose: Maintains the non-authorising execution ledger for the current broad engineering increment. -->

This file is the canonical live plan for the current broad or cross-cutting
increment. It records execution intent and evidence; it does not replace
`AGENTS.md`, the instruction corpus, an accepted ADR, `Current-State.md`, the
append-only history, a Quality Gate or a Human Gate.

## Control record

- Plan ID: `AUD-2026-R1-R1`
- Status: `BLOCKED`
- Created: `2026-08-27`
- Initial baseline: `main@3762f71c116af206b911a086b836cef11cd1894d`
- Continuation baseline: `main@b60ef4d302e4c4dc3f0e474be27eaa4b8c6beb13`
- Preserved predecessor: `AUD-2026-R1` remains `BLOCKED` by its first `Quick`;
  this continuation does not rewrite that disposition
- Lifecycle state: `STATE-06 INTEGRATION`; unchanged by this plan
- Authority: Bruno's explicit authorisation for the bounded
  `AUD-2026-R1-R1 Plan Control Integrity` corrective continuation
- Execution mode: `SEQUENTIAL_ONLY`
- Writer: coordinating conversation only
- Independent reviewer: one read-only factual-diff review after the stopped
  sequence and before the focused local commit
- Factual-state owner: `prompts/state/Current-State.md`
- Historical owner: `prompts/state/State-Transition-Log.md`

## Task envelopes

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
| `AUD-R1-R1-002` | `P1` | The sole online `Full` legacy-compatibility stage could not resolve .NET SDK `10.0.302` while inventorying target frameworks for `DBNotifier.Domain.csproj`. | `OPEN`; the legacy suite reported `Failed=1`, `Pending=0`, forcing `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`. Stop-on-failure prohibits diagnosis execution, correction or retry in this lot. |

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

## Blockers and limitations

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
