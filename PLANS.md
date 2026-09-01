# DB-Notifier Live Execution Plan

<!-- Purpose: Maintains the non-authorising execution ledger for the current broad engineering increment. -->

This file is the canonical live plan for the current broad or cross-cutting
increment. It records execution intent and evidence; it does not replace
`AGENTS.md`, the instruction corpus, an accepted ADR, `Current-State.md`, the
append-only history, a Quality Gate, an Agent Gate or a preserved historical
Human Gate.

## Portfolio preview publication record

- Plan ID: `PORTFOLIO-PREVIEW-01`
- Status: `AUTOMATED_GATE_FAIL`; the exact local portfolio candidate and each
  corrective successor passed their local gates and were published to GitHub,
  but the latest remote GitHub Actions execution exposed one line-ending-
  dependent localisation provenance hash on Linux and one bounded synthetic-
  process startup timeout on Windows; Render publication remains stopped
- Created: `2026-09-01`
- Baseline: `codex/portfolio-preview@492c1b3b7ee8d0374ddd16dd3d3e9af213e7bbe9`
- Published candidate: `main@8c7d5d36ad2bcdad124c74a426840c0c4f439027`
- First corrective successor: `main@6956f88d13ffef3d2e5236dd0ba5b25fa41ffd45`
- Exact root-cause successor: `main@e17293c1b9830836f99d9a036441a0a155d294c0`
- Cross-platform harness successor: `main@c14c95db8e5dcf23896a52023c664df04eba13bf`
- Authority: the owner's current request to publish DB-Notifier as an honest
  work-in-progress full-stack portfolio, with a professional README, a real
  demonstration GIF and a Render-hosted static preview
- Lifecycle state: `STATE-06 INTEGRATION`; unchanged
- Execution mode: `SEQUENTIAL_ONLY` / `SINGLE_OWNER`
- Writer: the coordinating task owns the isolated portfolio worktree and every
  path in the candidate write set; all reviews remain read-only
- Rollback: remove only the isolated `codex/portfolio-preview` worktree and
  branch before publication, or revert a later focused portfolio commit; never
  reset, clean or overwrite the protected principal worktree

### Objective and public truth boundary

Publish a polished source portfolio and static Web Dashboard preview that
demonstrate full-stack engineering without claiming production readiness,
live database monitoring, provider homologation, administrative execution or
external data. The hosted Dashboard and recorded media use deterministic local
demonstration data only. API, Agent and WPF deployment remain outside Render.

### Positive scope and candidate write set

1. `PLANS.md`
2. `README.md`
3. `SECURITY.md`
4. `render.yaml`
5. `docs/assets/db-notifier-demo.gif`
6. `docs/assets/db-notifier-demo.png`
7. `src/DBNotifier.Dashboard.Web/index.html`
8. `src/DBNotifier.Dashboard.Web/public/robots.txt`
9. `src/DBNotifier.Dashboard.Web/src/generated/localisation.ts`
10. `src/DBNotifier.Desktop.Wpf/Generated/Localisation.pt-BR.xaml`
11. `src/DBNotifier.Desktop.Wpf/Generated/Localisation.en-GB.xaml`
12. `src/DBNotifier.Dashboard.Web/src/generated/design-tokens.css`
13. `src/DBNotifier.Desktop.Wpf/Generated/DesignTokens.Core.xaml`
14. `src/DBNotifier.Desktop.Wpf/Generated/DesignTokens.Light.xaml`
15. `src/DBNotifier.Desktop.Wpf/Generated/DesignTokens.Dark.xaml`
16. `scripts/verify-script-syntax.ps1`
17. `tests/DBNotifier.ScriptSyntax.Tests.ps1`
18. `tests/DBNotifier.ContinuousImprovement.Tests.ps1`
19. `.gitattributes`
20. `scripts/generate-localisation.mjs`
21. `tests/DBNotifier.UnitTests/PostgreSqlProviderTests.cs`

The dependency manifests and lockfiles may enter the write set only if a
current security check proves a directly relevant, minimally correctable
public-preview blocker. Every additional path requires this record to be
updated before mutation.

### Negative scope and protected work

- The dirty principal worktree, its uncommitted `S06-DFR-03-AUTH-01 v1.1`
  candidate, all untracked files and every pre-existing user change are
  protected and remain untouched.
- The prohibited `mysql-notifier-1.1.8-src` tree is not enumerated, searched,
  read, copied or exposed as metadata.
- No API, Agent, WPF, provider, database, migration, authentication, secret,
  administrative action, production topology or lifecycle state is activated.
- No generated source, immutable report or historical evidence is rewritten.
- The remote correction does not weaken, skip or pin around either safety gate;
  it is limited to portable fixtures, fail-closed child-process validation and
  sanitised failure classification.
- GitHub and Render publication occur only after local validation, public
  history review, exact authenticated account discovery and a zero-cost target
  are proved. No paid resource, custom domain or production database is in
  scope.

### Definition of Ready

- Shutdown preflight passes with zero matching process and owned listener.
- The isolated branch and worktree start clean at the frozen baseline.
- The principal worktree and prohibited source boundary remain untouched.
- The README, media and hosted preview are explicitly labelled as synthetic,
  work-in-progress portfolio evidence.
- One writer owns every mutable path and local runtime used for capture.

### Definition of Done

- The first README screen communicates value, stack, work-in-progress status
  and the deterministic-data boundary to recruiters and developers.
- The README contains a real, sanitised GIF plus a static fallback image,
  architecture, demonstrated capabilities, local preview instructions, status,
  roadmap, security and licence links.
- `render.yaml` defines only a static Dashboard service with safe response
  headers and no API, database, secret or operational integration.
- The Dashboard build, focused tests, link/media checks, secret checks and a
  dedicated-browser visual review pass on the exact candidate.
- The canonical repository gate is run once when its declared prerequisites
  are ready; any failure remains factual and prevents a readiness claim.
- The focused candidate is independently reviewed at `P0=0` and `P1=0` before
  a local commit or external publication.
- Any GitHub or Render publication records the exact account, repository,
  branch, public URL and observed remote result without implying production.

### Stop codes

- `BASELINE_DRIFT`: the isolated baseline, branch or candidate paths diverge.
- `PROTECTED_WORK_OVERLAP`: the principal worktree or another owner's path
  would be changed.
- `ISOLATION_FAILURE`: shutdown, browser, filesystem or prohibited-tree
  isolation cannot be proved.
- `PUBLIC_TRUTH_FAILURE`: media or prose implies live or production behaviour.
- `SECRET_OR_PROVENANCE_FAILURE`: public history, media, notices or licences
  cannot be shown safe.
- `AUTOMATED_GATE_FAIL`: an applicable local or remote check fails.
- `EXTERNAL_PREREQUISITE`: an exact account, valid credential, free resource or
  safe publication mechanism is unavailable.

### Increment plan

1. `COMPLETE` — freeze authority, baseline, protected work and public truth.
2. `COMPLETE` — implement the portfolio README, security policy and Render
   Static Site definition; reconcile the deterministic generated adapters
   exposed by the first focused test run.
3. `COMPLETE` — build the Dashboard and capture sanitised real demonstration
   media in a dedicated browser. The eight-frame GIF and static fallback are
   entirely Dark theme, use `en-GB`, contain deterministic synthetic data only
   and expose no console warning, console error or external connection.
4. `COMPLETE` — execute focused checks, canonical validation and independent
   review on the frozen candidate. Focused checks, `Doctor`, `Quick` and the
   online canonical `Full` gate passed. The first independent review found one
   `P1` incorrect media-format defect and one `P2` reporting-route gap; both
   were corrected, and focused re-review passed at `P0=0`, `P1=0`, `P2=0`,
   `P3=0`.
5. `AUTOMATED_GATE_FAIL` — commit and publish the focused branch to the public
   GitHub repository. The first exact-commit workflow failed in the Windows
   script-syntax fixture and Linux reparse fixture; no Render resource was
   created after that failure.
6. `AUTOMATED_GATE_FAIL` — publish the first corrective successor after focused
   tests, `Quick`, the canonical `Full` gate and independent review passed. The
   second remote run proved the Linux reparse correction and exposed one exact
   shared script-syntax fixture defect before the Node child was invoked.
7. `LOCAL_COMPLETE` — remove the disproved speculative Node invocation change,
   select exactly one discovered Node application in the disposable fixture and
   repeat the applicable local gates and independent review. Focused tests,
   `Quick`, the canonical `Full` gate and review at `P0=0`, `P1=0`, `P2=0`,
   `P3=0` passed.
8. `AUTOMATED_GATE_FAIL` — publish the exact root-cause successor. The third
   remote run proved the scalar Node resolution on Windows and exposed two
   later cross-platform harness/checkout defects.
9. `LOCAL_COMPLETE` — preserve pre-throw sanitised test evidence incrementally,
   enforce deterministic LF checkouts for CSS/XAML and repeat the applicable
   local gates and independent review. Focused tests, `Quick`, the canonical
   `Full` gate and review at `P0=0`, `P1=0`, `P2=0`, `P3=0` passed.
10. `AUTOMATED_GATE_FAIL` — publish the cross-platform harness successor. The
    fourth remote run proved every earlier fixture correction, then exposed one
    raw-source line-ending dependency in localisation provenance on Linux and a
    five-second synthetic process startup deadline under Windows runner load.
11. `LOCAL_COMPLETE` — make localisation generation independent of checkout
    line endings and retain the real Windows process-tree regression with a
    bounded fixture-only startup allowance. The focused checks, `Quick` and the
    canonical `Full` gate passed, and independent review closed at `P0=0`,
    `P1=0`, `P2=0`, `P3=0`; a new exact-commit GitHub Actions result remains
    required before the zero-cost Render Static Site can be created.

### Validation evidence

- Dashboard toolchain verification, type checking, build, `74/74` Web tests,
  Markdown links, code-documentation policy, brand verification and npm audit
  passed; the npm audit reported zero vulnerabilities.
- The initial generated-adapter test exposed canonical localisation and token
  drift, which was reconciled from the owning sources. The first canonical
  `Doctor` then reported the expected missing restored assets in the clean
  worktree; `Setup` completed locked restores for `19` projects and the next
  `Doctor` passed.
- The first `Quick` preserved a development-flow policy failure caused by an
  omitted exact governance literal in the new README. After the README restored
  that literal, the focused policy check passed `160` assertions and the next
  `Quick` passed.
- The canonical online `Full` gate passed through secret scanning, policy and
  syntax checks, locked restore and build with zero warnings or errors, `825`
  .NET tests, `74` Web tests, line coverage `83.39%`, branch coverage `56.46%`,
  NuGet and npm vulnerability checks, legacy compatibility, browser
  accessibility samples and the consolidated `STATE-06` harness.
- Dedicated-browser media review confirmed `en-GB`, Dark theme and a
  `1280x720` rendering viewport. The final GIF contains eight normalised
  `1120x630` frames, is approximately `1.3 MB`, loops without external data and
  has a matching genuine `1120x630` PNG fallback selected for reduced-motion
  readers.
- The first independent review closed at `P0=0`, `P1=1`, `P2=1`, `P3=0`.
  The `P1` identified a JPEG payload incorrectly named `.png`; the fallback was
  re-exported with the PNG signature `89504E470D0A1A0A` and referenced through
  a reduced-motion `<picture>` source. The `P2` identified a conditional private
  vulnerability-reporting dead end; `SECURITY.md` now supplies a
  non-disclosing public-contact fallback. Independent focused re-review passed
  at `P0=0`, `P1=0`, `P2=0`, `P3=0`; it also confirmed the PNG is
  pixel-identical to the GIF's first frame and contains no embedded metadata or
  detected local-path marker.
- Local YAML parser validation is `NOT_RUN` because neither `ConvertFrom-Yaml`
  nor PyYAML is installed. `render.yaml` was checked against the current Render
  Static Site schema and remains subject to Render's remote Blueprint
  validation before publication.
- The final online canonical `Full` execution on the completed local candidate
  returned `DISPOSITION|PASS|stage=All`. It reconfirmed the secret scan, the
  `825` .NET and `74` Web tests, line coverage `83.39%`, branch coverage
  `56.46%`, zero NuGet/npm vulnerability findings, `128` viewport samples,
  `96` forced-colour route/page-scale samples, `24` focal zoom/reflow samples
  and the consolidated `STATE-06` harness. Closing shutdown reported zero
  matching process and zero owned listener.
- GitHub publication created the public repository
  `https://github.com/DegsTerin/DB-Notifier` and pushed the exact candidate as
  `main@8c7d5d36ad2bcdad124c74a426840c0c4f439027`. Private vulnerability reporting
  is enabled; the default branch, description and portfolio topics were
  verified through the authenticated GitHub CLI account `DegsTerin`.
- GitHub Actions run `33562428994` preserved two remote failures. The Windows
  canonical job passed the repository gate until
  `DBNotifier.ScriptSyntax.Tests.ps1`, where its valid path-with-spaces fixture
  failed under Node `24.20.0`. The Linux Dashboard job reached
  `DBNotifier.ContinuousImprovement.Tests.ps1`, where PowerShell `7.6.5` did not
  create the requested `Junction` fixture on Unix and the test subsequently
  exercised a missing path instead of a real reparse-backed path.
- Read-only diagnosis classified the first Linux result as a fixture false
  positive, not evidence that the controller read or hashed the external
  receipt. Its correction uses a Windows junction or Unix symbolic link and
  proves the resulting path has `ReparsePoint` before exercising the unchanged
  fail-closed controller.
- GitHub Actions run `33565126622` passed the continuous-improvement regression
  with `141` assertions on both runners, confirming that correction remotely.
  Both jobs then failed in the script-syntax fixture because `Get-Command node`
  returned multiple applications and the fixture supplied their complete
  `.Source` collection to the scalar `NodePath` parameter. The sanitised failure
  proved the Node child was never invoked, disproving the earlier spaced-path
  inference; the original gate invocation must therefore be restored.
- Focused validation passed the script-syntax regression with `13` assertions
  and zero disposable residue, the continuous-improvement regression with
  `141` assertions, the `50` PowerShell / `14` Node repository syntax inventory,
  development-flow policy with `160` assertions, development-flow tests with
  `112` assertions and the documentation gate over `450` source files.
- `Quick` passed as non-gate feedback. The canonical online `Full` gate then
  returned `DISPOSITION|PASS|stage=All`, reconfirming zero secret and dependency
  findings, `825` .NET tests, `74` Web tests, line coverage `83.39%`, branch
  coverage `56.46%`, the accessibility sample matrix and the consolidated
  `STATE-06` harness. Independent static review passed at `P0=0`, `P1=0`,
  `P2=0`, `P3=0`.
- The exact root-cause correction restored `verify-script-syntax.ps1` byte-for-
  byte to its pre-correction state and changed only the disposable test to
  select the first discovered Node `ApplicationInfo`, validate its non-empty
  `Source` and reuse that scalar path. The focused fixture passed `14`
  assertions with zero residue; `Quick` passed as non-gate feedback.
- The subsequent canonical online `Full` gate returned
  `DISPOSITION|PASS|stage=All` and repeated the complete secret, policy, build,
  `825` .NET test, `74` Web test, coverage, dependency, browser-accessibility and
  consolidated `STATE-06` evidence. Independent static review passed at
  `P0=0`, `P1=0`, `P2=0`, `P3=0` and confirmed the production gate is identical
  to `main@8c7d5d36ad2bcdad124c74a426840c0c4f439027`.
- GitHub Actions run `33566535592` passed the corrected script-syntax fixture
  with `14` assertions on Windows and advanced the canonical job through the
  complete .NET build/test/coverage, dependency, runtime and legacy gates. The
  Linux job also passed scalar Node binding, then exposed that the test helper's
  atomic assignment discarded a sanitised `ErrorRecord` emitted before the
  expected terminating exception.
- The same run reached `tokens:verify` on Windows and found the generated CSS
  stale. Read-only byte comparison proved that `core.autocrlf=true` added only
  carriage returns because `.gitattributes` did not declare CSS or XAML EOLs;
  the canonical checksum and LF blobs remained correct. The next correction
  preserves incremental test output and aligns CSS/XAML checkout bytes with the
  repository-wide LF rule without normalising or weakening generated-content
  verification.
- The incremental-capture regression passed locally with `15` assertions and
  zero disposable residue. Git attributes resolved all six generated CSS/XAML
  artefacts to `text eol=lf`, while both token and localisation verification
  passed without changing either generator.
- `Quick` passed as non-gate feedback. The subsequent canonical online `Full`
  gate returned `DISPOSITION|PASS|stage=All` and repeated the complete secret,
  policy, build, `825` .NET test, `74` Web test, coverage, dependency,
  browser-accessibility and consolidated `STATE-06` evidence. Independent
  static review passed at `P0=0`, `P1=0`, `P2=0`, `P3=0`.
- GitHub Actions run `33568867344` proved the preceding script, reparse,
  incremental-capture and generated-token corrections on both runners. The
  Linux job passed all policy and fixture checks, then `localisation:verify`
  found that the generated provenance checksum had been calculated from CRLF
  catalogue bytes while the Linux checkout supplied canonical LF bytes. The
  Windows job independently passed the same corrected fixtures, restored and
  built all `19` projects with zero warnings or errors, passed architecture,
  WPF and integration tests, then one process-tree theory case exhausted its
  five-second synthetic PowerShell startup deadline before emitting the child
  PID. Both job failures remain the exact first remote results; neither job was
  rerun.
- The fourth-run remediation normalises catalogue line endings before both
  validation and provenance hashing, declares the canonical XML inputs as LF
  and regenerates all three adapters with source checksum
  `b7aeb39f3508fdf033e99d630bb0fd1f28f0a2d8c21826cd4a0152337e3ed5d1`.
  It changes no localisation text. The Windows fixture now allows `15` seconds
  only for its synthetic PowerShell process to publish a child PID; the real
  `50` millisecond timeout, cancellation, process-tree termination and cleanup
  assertions remain unchanged.
- Focused localisation verification and both real process-tree theory cases
  passed, followed by a closing shutdown with zero matching processes and zero
  owned listeners. `Quick` passed as non-gate feedback. The sole canonical
  `Full` execution returned `DISPOSITION|PASS|stage=All`: secret and dependency
  scans passed, all `825` .NET and `74` Web tests passed, coverage remained
  `83.39%` lines and `56.46%` branches, the `128` viewport, `96` forced-colour
  and `24` zoom/reflow samples passed, and the consolidated `STATE-06` harness
  passed with local test data only. Closing shutdown again found zero matching
  processes and zero owned listeners.
- Independent read-only review of the seven-path candidate passed at `P0=0`,
  `P1=0`, `P2=0`, `P3=0`. It confirmed cross-platform hash determinism, exact
  provenance-only generated changes, unchanged product timeout and termination
  semantics, en-GB documentation and absence of secrets or protected material.

## Active continuous-improvement control record

- Plan ID: `GOV-CONTINUOUS-IMPROVEMENT-01`
- Status: `AUTOMATED_GATE_FAIL`; the prior final review at `P0=0`, `P1=5`,
  `P2=2`, `P3=0` and the latest re-review at `P0=0`, `P1=3`, `P2=0`, `P3=0`
  remain preserved. The next independent review at `P0=0`, `P1=2`, `P2=0`,
  `P3=0` is also preserved. This same isolated 17-path successor is correcting
  only canonical execution-key replay and the complete governed-path matrix; no
  reviewer `FAIL` is converted by the writer's local passes
- Created: `2026-08-30`
- Initial baseline: detached
  `d75e98112334cf1a74fcfb0614f8b308a9ebbdb9`
- Authority: the current owner-authorised audit-and-remediation request for a
  continuous-improvement process and independent improvement agents, narrowed
  by the coordinating task to the DB-Notifier `9.1.0` candidate in this exact
  isolated worktree
- Lifecycle state: `STATE-06 INTEGRATION`; unchanged
- Execution mode and topology: `SEQUENTIAL_ONLY` / `SINGLE_OWNER`; this task
  is the only writer in the isolated worktree and every reviewer remains
  read-only
- Writer: `/root/audit_rag_continuous` owns only the exact candidate paths
  below; the coordinating task retains integration, promotion and Agent Gate
  custody
- Independent reviewers: the completed final review is preserved as a failing
  gate; a new read-only reviewer must inspect the corrected frozen candidate
- Rollback strategy: discard or reverse only the isolated candidate paths
  before integration; after a future promotion, use the recorded last-known-
  good revision and a separately gated compensating commit, never destructive
  reset or loss of protected work
- Protected work and negative scope: the principal worktree, its predecessor
  product WIP, the preserved 29-row verifier failure, the first failed v9
  audit, product source, dependencies, `Language-Policy.md`, lifecycle,
  providers, databases, runtime, remote, baseline branch, push, publication,
  deployment, secrets and every prohibited source/reference tree

### Objective and acceptance

Materialise one event-driven and bounded continuous-improvement control plane
that derives work from immutable events, uses stable fingerprints and keys,
prevents every same-candidate retry, requires causal delta before a successor
attempt, quarantines invalid progress, derives candidate classification from
governed paths, records each reviewer and receipt factually, and keeps
implementation, verification, review, integration and observation identities
disjoint. A recoverable pending journal protects every append. Governance
changes pass old and new gates; promotion and rollback resolve real Git commit,
  tree and preimage facts for the exact isolated candidate and prior LKG.

Every event carrying an execution identity must persist its declared execution
scope and acceptance digests and replay the canonical execution key from the
improvement fingerprint, baseline and those inputs. The controller-owned
governance matrix must classify all 28 official authority, state, planning,
architecture, standards, playbook, template, prompt-system and development-
control paths as high-risk governance without making ordinary product paths
high-risk by default.

Metrics must be derived from the validated append-only ledger, retain explicit
numerators and denominators, exclude unobserved promotions from escape-rate
denominators and never allow a mutable dashboard or caller-supplied aggregate
to become evidence.

### Exact candidate write set

1. `AGENTS.md`
2. `PLANS.md`
3. `prompts/Start-Here.md`
4. `prompts/governance/Continuous-Improvement.md`
5. `prompts/governance/Conversation-Coordination-Prompt.md`
6. `prompts/governance/Governance.md`
7. `prompts/governance/Quality-Gates.md`
8. `prompts/state/Continuous-Improvement-Backlog.md`
9. `prompts/state/Current-State.md`
10. `prompts/state/State-Transition-Log.md`
11. `prompts/system/AI-Software-Engineering-Master-Prompt.md`
12. `prompts/system/Prompt-System-Change-Log.md`
13. `scripts/ci.ps1`
14. `scripts/continuous-improvement.ps1`
15. `scripts/verify-development-flow.ps1`
16. `tests/DBNotifier.ContinuousImprovement.Tests.ps1`
17. `tests/DBNotifier.DevelopmentFlow.Tests.ps1`

Every path not listed above is immutable for this candidate.

### Definition of Ready

- Shutdown preflight reports zero matching process and zero owned listener
  before every applicable technical action.
- The exact worktree is detached at the initial baseline with a clean index and
  no pre-existing worktree diff.
- The prohibited reference boundary is neither accessed nor enumerated.
- Authority, lifecycle, clean-room, product, external and destructive limits
  remain unchanged.
- One writer owns the full candidate; reviewers cannot write or promote it.

### Definition of Done

- Corpus `9.1.0`, coordination `2.1.0`, the new governance authority and the
  factual backlog agree on one process and one set of stable literals.
- The controller proves stable fingerprints, execution/dispatch keys, locked
  hash-chained pending/append recovery, strict UTF-8 without BOM, bounded
  event-driven decisions, same-candidate rejection, derived causal receipts and
  risk, factual review receipts, disjoint roles, old/new meta-gates, exact
  candidate isolation, Git-proven LKG observation/rollback and anti-gaming
  metrics.
- Behavioural regressions, development-flow regressions, PowerShell AST,
  explicit Markdown links, exact-path diff hygiene and the complete policy
  verifier pass in this isolated worktree.
- The first audit `FAIL` and principal-worktree verifier `FAIL` remain visible
  and are not counted as successful candidate evidence.
- Independent review and promotion remain with the coordinating task; no
  commit, branch update, remote action or product/lifecycle transition occurs
  in this writer task.

### Increment plan

1. `COMPLETE` — reconcile authority, baseline, protected boundaries and the
   inherited failure evidence.
2. `COMPLETE` — correct strict recursive JSON admission, full path-derived
   risk, ledger-bound causal receipts, operational-path confinement, structured
   review findings, predecessor-exact attempt admission and metric provenance.
3. `COMPLETE` — run the focused behavioural and development-flow policy tests,
   PowerShell AST and explicit-link validation on the complete correction.
4. `COMPLETE` — run the complete development-flow verifier once after the full
   delta and preserve every earlier result and reviewer finding.
5. `COMPLETE` — freeze the exact baseline, empty index and 17-path diff for a
   new independent P0-P3 review without commit or promotion.
6. `COMPLETE` — preserve the subsequent `P0=0`, `P1=3`, `P2=0`, `P3=0`
   review and correct failed-gate role separation, pending-recovery request
   identity and factual failed old/new check results in the same 17 paths.
7. `COMPLETE` — preserve the next independent review at `P0=0`, `P1=2`,
   `P2=0`, `P3=0` for frozen digest
   `808ef853c1a6786920254d83d1301e5611d50d27ca9bb1dcdbb4ec36f94c5823`.
8. `COMPLETE` — reject non-canonical execution keys during append,
   idempotent recovery, admission and full replay; replace partial governed-path
   matching with the exact 28-path matrix and table-driven regressions.
9. `COMPLETE` — rerun behavioural, development-flow, complete verifier, AST and
   explicit-link gates after the full correction, then reconcile state and the
   exact 17-path candidate before freeze.
10. `PENDING` — dispatch the frozen successor for a new independent read-only
    P0-P3 review; promotion remains outside this writer.

### Preserved evidence and stop conditions

- `PASS`: entry shutdown preflight reported zero matching process and zero
  owned listener.
- `FAIL` (preserved): the entry `Doctor` found dependencies unrestored in this
  new isolated worktree. This does not replace the focused governance checks
  and is not relabelled as a product or canonical gate pass.
- `FAIL` (preserved predecessor): the principal-worktree
  `scripts/verify-development-flow.ps1` stopped on pre-existing drift in the
  protected 29-row MySQL Notifier functional matrix. This candidate does not
  access or alter that product WIP.
- `FAIL` (preserved audit): the initial v9 final review returned P0 zero, P1
  three, P2 one and P3 zero before its successor review passed. The original
  finding set remains an immutable input to the new backlog.
- `FAIL` (preserved successor review): one preliminary P1 plus five additional
  P1 findings identified direct append recovery, caller-owned causal and risk
  facts, phantom reviews, integrator/observer overlap and opaque Git identities;
  one P2 identified permissive BOM handling. The implementation now contains
  reproducing counterexamples and corrections, but independent successor review
  remains pending.
- `FAIL` (preserved focal): the first post-schema behavioural run exposed an
  empty review projection under StrictMode after the opaque revision probe. The
  defect and leaked Git stderr were corrected without rewriting the result.
- `FAIL` (preserved validator): the first strengthened development-flow run
  found a historical phrase split across two lines; the prose was reflowed
  without semantic change and the result remains factual.
- `FAIL` (preserved final successor review): the final independent review
  returned `P0=0`, `P1=5`, `P2=2`, `P3=0`. It found duplicate-property JSON
  ambiguity, incomplete path-derived risk, non-factual causal hashes,
  unconstrained ledger/candidate paths, incomplete P0-P3 gate evidence,
  attempt admission after a non-failed predecessor and unbound metric
   provenance. No prior PASS or FAIL is rewritten by this corrective successor.
- `FAIL` (preserved corrective focal): the first behavioural run after adding
  structured review evidence exposed a P2 disposition copied onto
  `DUPLICATE_SUPPRESSED` without an execution key. Propagation was restricted to
  candidate-lifecycle events; the successor run passed 88 assertions.
- `FAIL` (preserved corrective full gate): the first full verifier stopped on a
  required `zero denominator` literal split by prose wrapping. The phrase was
  reflowed without changing its contract; the successor full gate passed.
- `FAIL` (preserved latest re-review): the next independent review returned
  `P0=0`, `P1=3`, `P2=0`, `P3=0`. It found that `GATE_FAILED` did not apply the
  verifier/implementer separation used by `GATE_PASSED`, recovery could return
  pending event A as success for requested event B, and a failed governance
  gate could preserve `PASS`/`PASS` old/new results.
- `FAIL` (preserved latest focal): the first behavioural run of this successor
  exposed an empty-array collapse while replaying an exact recovered request as
  `Ledger event 2 reuses an event ID.` The focused correction retained an
  explicit empty prior-event array; later runs also exposed and corrected empty
  reviewer binding and assertion wording without weakening rejection.
- `FAIL` (preserved execution/risk re-review): the independent review of frozen
  candidate
  `808ef853c1a6786920254d83d1301e5611d50d27ca9bb1dcdbb4ec36f94c5823`
  returned `P0=0`, `P1=2`, `P2=0`, `P3=0`. It found that arbitrary well-formed
  execution-key digests were not recomputed from declared inputs and that 11
  official authority/state/development-control paths could remain `STANDARD`.
- `FAIL` (preserved execution-identity focal): the first behavioural run after
  the new replay fields stopped at controller line 1025 with
  `EXECUTION_KEY_IDENTITY_INVALID: execution inputs cannot exist without an
  execution key.` The post-execution duplicate fixture lacked the now-required
  declared identity; the fixture was corrected without weakening admission.
- `PASS` (current successor): the behavioural suite passed 140 assertions, the
  development-flow suite passed 112 assertions, the complete verifier passed
  160 assertions, AST passed for all five PowerShell policy files, and the
  repository link gate resolved 1007 local links in 233 files.
- `PASS` (mechanical): the continuous-improvement behavioural suite passed 98
  assertions, the development-flow policy suite passed 111 assertions, the
  complete verifier passed 159 assertions, AST passed for all five changed
  PowerShell policy files and the repository link gate resolved 1007 local
  links in 233 files.
- Stop on `AUTHORITY_MISMATCH`, `BASELINE_DRIFT`, `SCOPE_OVERLAP`,
  `ISOLATION_FAILURE`, `MUTABLE_RESOURCE_COLLISION`, `GATE_FAILURE`, an exact
  candidate path mismatch, an invalid/pending ledger chain, caller-supplied risk
  or causal identity, a same-candidate attempt, absent causal receipt, phantom
  reviewer, overlapping role identities, opaque Git receipt, BOM, failed
  old/new meta-gate, or any prohibited-boundary access.

## Active autonomous-delivery control record

- Plan ID: `GOV-AUTONOMOUS-DELIVERY-01`
- Status: `LOCAL_COMPLETE`; the focused governance candidate passed in an
  isolated exact-baseline worktree and is closed by the narrow local commit;
  the principal worktree's protected predecessor WIP and its factual verifier
  failure remain outside this increment
- Created: `2026-08-30`
- Initial baseline: `main@971adf2d0432ee4c61112535e69b938eb7b561b4`
- Authority: the owner's current request to execute
  `C:\Projects\Autonomous-Project-Delivery.md` across DB-Notifier,
  Shift-Flow and RAG-Challenge; the source contained 428 lines, 14,038 bytes
  and SHA-256
  `7591ADBC0D8DBACCCEC73FF698A4046B14B587939F1418A55E4E2AD845E16FE1`
  when this plan was opened
- Lifecycle state: `STATE-06 INTEGRATION`; unchanged at plan entry
- Execution mode: `SEQUENTIAL_ONLY` for every DB-Notifier write and mutable
  validation; parallel lanes are read-only
- Writer: coordinating task for integration; the delegated governance writer
  owned only the 13 clean instruction and policy-validator paths listed below
- Independent reviewers: one semantic authority reviewer and one
  coordination/safety reviewer over the integrated exact-path candidate
- Protected work: the entire pre-existing `S06-DFR-03-AUTH-01 v1.1` tracked
  diff, its `ISOLATION_FAILURE`, its open P1 findings and every unrelated
  product, dependency, generated, localisation, state and history change
- External and higher-authority boundary: protected-reference access,
  licensing/legal decisions, secrets, destructive operations without an
  Automated Safety Gate, real providers/databases, production, deploy,
  publication, push and external infrastructure

### Objective and scope

Replace prospective development Human Gates and owner-mediated copy/paste
handoffs with objective Agent Gates and factual automatic dispatch, without
rewriting historical decisions or weakening product-user authentication,
RBAC, administrative confirmation, clean-room, licensing, data, secret,
destructive, external or production controls.

Positive scope is limited to the active instruction corpus, its two explicit
policy validators, factual reconciliation in this plan/current state/history,
focused validation, independent review and a narrow local governance commit.
Product implementation and the protected predecessor candidate are negative
scope.

### Exact candidate write set

The delegated governance writer owned exactly these 13 paths:

1. `AGENTS.md`
2. `prompts/Start-Here.md`
3. `prompts/governance/Governance.md`
4. `prompts/governance/Lifecycle.md`
5. `prompts/governance/Quality-Gates.md`
6. `prompts/governance/Conversation-Coordination-Prompt.md`
7. `prompts/governance/Language-Policy.md`
8. `prompts/templates/Templates.md`
9. `prompts/system/AI-Software-Engineering-Master-Prompt.md`
10. `prompts/system/Prompt-System-Change-Log.md`
11. `prompts/operations/Operational-Playbooks.md`
12. `scripts/verify-development-flow.ps1`
13. `tests/DBNotifier.DevelopmentFlow.Tests.ps1`

Coordinator-owned integration is limited to localised hunks in:

1. `PLANS.md`
2. `prompts/state/Current-State.md`
3. `prompts/state/State-Transition-Log.md`

Every other path, including all predecessor product WIP, is outside the
candidate and must remain unstaged.

### Definition of Ready

- Shutdown preflight is `PASS` with zero matching process and zero owned
  listener.
- Repository identity, branch, HEAD, index, tracked WIP and worktrees are
  recorded without enumerating an excluded reference boundary.
- Canonical owners, active Human Gate/copy contracts and validator consumers
  are mapped.
- One writer owns DB-Notifier mutation at a time; reviewers remain read-only.
- The predecessor candidate and unrelated WIP are protected from staging and
  commit.

### Definition of Done

- Corpus `9.0.0` and coordination `2.0.0` consistently own prospective
  autonomous decisions, gates and dispatch.
- Agent Gates use objective `PASS`/`FAIL`, independent P0-P3 review and the six
  canonical operational states.
- Dispatch uses the four canonical routes, factual receipts, deduplication,
  reconciliation before retry and deterministic fallback, with no owner
  copy/paste dependency.
- Current facts and append-only history distinguish prospective supersession
  from preserved Human Gate evidence.
- Explicit allowlisted policy validation, AST parsing, diff hygiene and two
  independent semantic reviews pass with zero open P0/P1.
- Only the clean governance paths and any safely staged plan/state hunks enter
  a focused local commit; predecessor WIP remains unstaged.

### Increment plan

1. `COMPLETE` — resolve authority, baseline, WIP and clean-room boundary.
2. `COMPLETE` — map canonical gate, lifecycle, coordination, template and
   validator consumers through three independent read-only repository audits.
3. `COMPLETE` — integrate corpus `9.0.0`, coordination `2.0.0`, current
   factual state and this live plan.
4. `COMPLETE` — preserve the principal-worktree verifier failure, correct all
   independent findings, then validate the exact 16-path candidate in an
   isolated worktree where the verifier passed 151 assertions.
5. `COMPLETE` — stage only owned governance hunks, commit locally and retain the
   automatic continuation record; predecessor WIP remains unstaged.

### Evidence and blockers

- `PASS`: shutdown preflight at entry and again before coordinator integration.
- `PASS`: Git baseline and tracked-only WIP inventory without untracked root
  enumeration.
- `PASS`: three delegated audits mapped the three repositories without writes.
- `FAIL` (preserved): early focal policy attempts found missing literals and
  checker assumptions; successor evidence remains required.
- `PASS`: focused policy test completed with 108 assertions; PowerShell AST
  parsed both validator files; 13 explicit Markdown files resolved 85 relative
  targets; exact-path diff hygiene passed.
- `FAIL` (preserved): `scripts/verify-development-flow.ps1` stopped on
  pre-existing drift in the protected 29-row MySQL Notifier functional matrix.
  The product WIP was not changed and this mandatory result is not relabelled.
- `AUTOMATED_GATE_FAIL`: initial final DB v9 review returned P0 zero, P1 three,
  P2 one and P3 zero. The exact write set, factual dispatch receipts and
  cumulative destructive/external gate regression coverage are now reconciled.
  The principal-worktree full-verifier failure remains preserved.
- `PASS`: isolated successor worktree at exact HEAD
  `971adf2d0432ee4c61112535e69b938eb7b561b4`, overlaid with only the enumerated
  16 candidate paths, passed shutdown preflight and the complete
  `scripts/verify-development-flow.ps1` policy verifier with 151 assertions.
- `PASS`: independent successor review returned P0 zero, P1 zero, P2 zero and
  P3 zero after write-set, receipt, coverage and evidence reconciliation.
- `NOT_RUN`: `Quick`, `Full`, runtime, provider, database, browser, deploy,
  publication and push; they are outside this governance-only increment.
- Current blocker: none for this governance increment. The protected predecessor
  candidate remains separately `BLOCKED / ISOLATION_FAILURE`; its product WIP
  and principal-worktree verifier failure were neither changed nor promoted.

### Dispatch receipts

#### Governance audit and implementation candidate

- Ledger dispatch ID: `NÃO APLICÁVEL`; the tool returned no separate dispatch
  identifier.
- Route/source/destination: `DELEGATE_SUBAGENT` from `/root` to the confirmed
  canonical task `/root/db_governance_audit`.
- Tool and timestamp: `collaboration.spawn_agent` on `2026-08-30`; the tool
  result exposed no wall-clock timestamp.
- Repository/baseline/corpus: `C:\Projects\DB-Notifier`,
  `main@971adf2d0432ee4c61112535e69b938eb7b561b4`, target corpus `9.0.0`
  and coordination `2.0.0`.
- Objective/scope/protection: audit the prospective autonomy contract and
  produce only the 13-path candidate enumerated above; preserve the predecessor
  candidate, product WIP and clean-room boundary.
- Receipt/cursor: confirmed destination `/root/db_governance_audit`; no
  separate receipt ID or cursor was exposed.
- Result/status/retry: audit returned and the 13-path candidate was integrated
  serially; `AGENT_DECIDED`; no retry.
- Deduplication key/fallback: repository + exact baseline + corpus version +
  autonomous-governance objective; reconcile in `CONTINUE_CURRENT` if the
  destination becomes unavailable.

#### Independent final review

- Ledger dispatch ID: `NÃO APLICÁVEL`; the tool returned no separate dispatch
  identifier.
- Route/source/destination: `DELEGATE_SUBAGENT` from `/root` to confirmed
  canonical task `/root/db_v9_final_review`.
- Tool and timestamp: `collaboration.spawn_agent` on `2026-08-30`; no
  wall-clock timestamp or cursor was exposed.
- Repository/baseline/corpus: the same exact repository and baseline above,
  corpus `9.0.0`, coordination `2.0.0`.
- Objective/scope/protection: read-only P0-P3 review of the exact governance
  candidate; no protected-reference enumeration and no mutation.
- Receipt/result/status/retry: confirmed destination and initial final result
  `FAIL` with P0 zero, P1 three, P2 one and P3 zero. After candidate changes,
  the same confirmed reviewer returned `PASS` with P0-P3 zero;
  `AUTOMATED_GATE_PASS`; no duplicate dispatch or unreconciled retry.
- Deduplication key/fallback: repository + baseline + corpus + final-review
  objective; correction occurred safely in `CONTINUE_CURRENT`, followed by a
  return to the same confirmed reviewer only after the candidate changed.

The `S06-DFR-03-AUTH-01` record below remains the preserved predecessor. Its
blocked disposition and WIP are not superseded, staged or corrected by this
governance plan.

## Control record

- Plan ID: `GOV-MN-RESTORE-01`
- Status: `COMPLETE`
- Created: `2026-08-28`
- Initial baseline: `main@830b20c423b604cc72ec2e1b53423ac53066e067`
- Preserved predecessors: all first factual dispositions remain immutable;
  `GOV-MN-REV-01` remains `COMPLETE` as the factual result of the owner's
  earlier literal wording but is prospectively superseded by the corrected
  intent recorded in this plan,
  `S06-DFR-02` and `S06-DFR-01` remain `COMPLETE`,
  `GOV-2026-R1` remains `COMPLETE`,
  `AUD-2026-R1-R5-R2` remains `BLOCKED` by its rejected durable-capture
  wrapper before process creation,
  `AUD-2026-R1-R5-R1` remains `BLOCKED` by its unprovable sole focal result,
  while `AUD-2026-R1-R4` remains `BLOCKED` by its sole online `Full`
- Lifecycle state: `STATE-06 INTEGRATION`; unchanged by this plan
- Authority: Bruno's explicit clarification that the intended direction is to
  recreate MySQL Notifier functionality in DB-Notifier and use MySQL Notifier
  as a functional basis for improving DB-Notifier
- Execution mode: `SEQUENTIAL_ONLY`; execution topology `SINGLE_OWNER`.
  Independent lanes are read-only, and every write, integration step,
  validation and Git operation is sequential
- Writer: coordinating conversation only
- Independent reviewers: one read-only authority-restoration reviewer, one
  functional-matrix reviewer and one licensing-boundary reviewer produce
  independent candidates; a final reviewer examines the integrated frozen diff
- Factual-state owner: `prompts/state/Current-State.md`
- Historical owner: `prompts/state/State-Transition-Log.md`

## Task envelopes

### MySQL Notifier functional-reference restoration `GOV-MN-RESTORE-01` — complete

- Envelope status: `COMPLETE`.
- Corrected human authority: restore MySQL Notifier 1.1.8 as a functional and
  behavioural reference for improving DB-Notifier and recreate its useful
  capabilities as DB-Notifier-owned functionality.
- Workspace and frozen baseline: `C:\Projects\DB-Notifier`, branch `main`,
  commit `830b20c423b604cc72ec2e1b53423ac53066e067`; index and tracked worktree
  were clean before this corrective plan update.
- Initial shutdown evidence: the canonical preflight returned `PASS` with zero
  matching processes and zero owned listeners. No process was stopped.
- Verifiable objective: supersede the prospective prohibition created by
  `GOV-MN-REV-01`; restore the MySQL Notifier functional-inspiration clause of
  `REQ-047`; reactivate `REQ-048`, `REQ-050` and the 29-record `MN-*`/`MN-Q*`
  functional-coverage matrix; permit sanitised behavioural
  inspection and independently implemented provider-neutral equivalents;
  preserve all historical records and existing DB-Notifier evidence; and name
  the smallest separately authorised implementation lot that should follow.
- Safe interpretation of “copy functionality”: cover sanitised, observable and
  non-expressive user outcomes and improve them through DB-Notifier-owned
  requirements, architecture, code, tests and assets. The reference may inform
  capability discovery and behavioural acceptance criteria, but it is not an
  implementation base, source-compatibility target or mandate to clone a
  distinctive selection, arrangement, interface or interaction expression.
- Licensing boundary: this envelope does not authorise copying, translating,
  adapting, linking, redistributing or importing Oracle/MySQL source code,
  compiled libraries, artwork, logos, trade dress, product copy or other
  protected expression into the MIT project. Literal reuse would require exact
  component/rightsholder provenance, documented applicable rights or
  permissions, a compatible distribution model, specialist legal review and a
  separate owner decision; neither the owner decision nor a local additional
  linking permission can relicense Oracle/MySQL or third-party material from
  the reference tree or grant trademark rights. No such authority or evidence
  is inferred here.
- Positive scope: this live plan; permanent root-agent instructions; README;
  project vision; migration-plan authority and its preserved 29-row matrix;
  Design System authority; current factual state; one corrective documentary
  report; instruction-system changelog; append-only transition history; the
  development-flow policy regression; focused policy, documentation, link and
  secret checks; independent read-only review; and one focused local commit.
- Source-reference scope: this corrective lot relies on the sanitised inventory
  already frozen by `GOV-MN-REV-01` and one separated read-only provenance lane.
  That lane sampled licence/notice material plus representative source, project
  and resource-manifest metadata to classify ownership and asset risk. It
  executed and modified nothing, copied no target file into the workspace and
  delivered no source extract, signature, identifier, constant or internal
  design to the coordinating or implementation lanes. The ignored
  `mysql-notifier-1.1.8-src/` tree remains quarantined and untracked. Any future
  source-code inspection requires separate explicit authority and a
  source-exposed analyst who may return only an approved sanitised provenance
  record and behavioural specification; corresponding implementation and test
  authors must remain unexposed to source, binaries, assets, decompiled material
  and raw or unsanitised source-derived notes and may receive only that approved
  sanitised handoff. Independent provenance/similarity review is required
  before integration.
- Historical scope preserved read-only: `GOV-MN-REV-01`, prior Human Gates,
  ADRs, reports, request traceability, plan envelopes, changelog entries,
  transition-log entries and commits retain their original wording and factual
  dispositions. The current clarification supersedes only their prospective
  authority where they conflict.
- Earlier `unread`/no-access statements in predecessor envelopes remain true
  for those executions and are not rewritten. This corrected-authority lot
  separately authorised the bounded provenance sampling recorded above. Any
  future source-code inspection requires its own explicit authority and
  separated-role envelope.
- Protected work and negative scope: no product source, test behaviour,
  provider, dependency, lockfile, schema, generated artefact or runtime
  composition changes in this corrective governance lot. Do not access a real
  database/provider/network service, start DB-Notifier, deploy, publish, push,
  decide a Human Gate, activate a capability or transition lifecycle.
- Provider distinction: MySQL Notifier is the functional reference product;
  MySQL remains a separate database-engine provider candidate. Reactivating
  functional coverage does not implement or homologate that provider.
- Artefact classification and ownership: instructions, migration plan and
  Design System are `AUTHORITY`; current state is `CURRENT_FACT`; transition
  log is append-only `HISTORY`; this file is `PLAN`; the corrective report is
  `EVIDENCE`; README and changelog are governed documentation. The coordinating
  conversation is sole writer; all reviewers remain read-only.
- Definition of Ready: shutdown and exact-baseline checks pass; current and
  historical authorities are classified; the 29 matrix records remain
  byte-preserved from their pre-revocation form; licensing and security limits
  are explicit; ownership, checks, reviewers and stop codes are frozen.
- Definition of Done: all active authorities agree that MySQL Notifier may
  inform observable non-expressive functional requirements and independently
  authored provider-neutral behaviour through the separated-role boundary;
  the functional-inspiration clause of `REQ-047`, `REQ-048`, `REQ-050` and all
  29 matrix records have active dispositions and remaining exits;
  code/assets/vendor expression remain prohibited without a
  separate licensing decision; existing product evidence and rejected unsafe
  mechanisms remain truthful; corpus, Design System, state, changelog and
  append-only history agree; policy regression and independent review pass;
  one focused local commit exists; lifecycle remains unchanged.
- Stop rule: baseline drift, tracked owner work, copying protected expression,
  weakening a security rejection, changing product implementation, conflicting
  authority, historical-evidence rewrite, provider/product conflation, failed
  mandatory check, secret/host disclosure or need for legal, Human Gate,
  external or lifecycle authority stops the affected work without widening.
- Objective stop codes: `AUTHORITY_MISMATCH`, `BASELINE_DRIFT`,
  `SCOPE_OVERLAP`, `DEPENDENCY_UNREADY`, `ISOLATION_FAILURE`,
  `MUTABLE_RESOURCE_COLLISION`, `GATE_FAILURE`, `EXTERNAL_AUTHORITY_REQUIRED`
  and `HUMAN_DECISION_REQUIRED` retain their governed meanings.
- Rollback strategy: before commit, reverse only this envelope's owned
  documentary candidate under separately authorised recovery; after commit,
  preserve history and use a separately authorised focused revert. No product,
  external tree, provider, runtime or external system is mutated by this lot.
- Focused validation evidence: the first development-flow policy execution
  returned `FAIL`/exit `1` because the required provider-boundary literal was
  split by a Markdown line break in the new report. The report representation
  was corrected without changing policy meaning; the successor execution
  passed `125` assertions. After the verifier was strengthened for `REQ-047`,
  exact matrix identity, separately authorised future inspection and the
  non-authorisation of `S06-DFR-03`, its first execution returned `FAIL`/exit
  `1` because one new count assertion crossed a Markdown line break. The
  assertion was made line-break-safe without weakening the contract; its
  successor passed `127` assertions. The standalone regression then passed
  `98` assertions. The documentation gate passed `447` comment-capable files,
  the Markdown gate passed `995` local links in `231` files, the
  available-history secret scan passed and `git diff --check` passed.
- Structural preservation evidence: the historical revocation report has no
  diff, and the first 771,775 bytes of the append-only transition log retain
  SHA-256
  `6ebb28113f5e36c9118939e27455f67db096f80974fbf7a3bab83fc8c4598762`.
  The 29 matrix rows retain normalised SHA-256
  `8961a3af4b02de68a2b16b83159c48779e5d26cffaabbb6de387e4a6b61d1449`.
  One draft evidence patch initially matched a repeated historical context and
  temporarily changed the prefix; the immediate hash check caught it, the
  uncommitted block was removed and the frozen prefix was restored before any
  integration.
- Gate applicability: `Quick` and `Full` are `NOT_RUN` because this corrective
  policy/documentation lot changes no executable product behaviour,
  dependency, generated artefact or runtime composition. They are not used as
  evidence for this disposition.
- Independent review evidence: the first frozen-diff authority review returned
  `FAIL` with `P0=0`, `P1=3`, `P2=3`, `P3=0`; the first licensing review
  returned `FAIL` with `P0=0`, `P1=1`, `P2=2`, `P3=1`. Findings covered factual
  source exposure, `REQ-047`, matrix non-authority, future inspection,
  category counts, policy assertions, official-source verification, sanitised
  handoff wording, third-party scope and referential naming. The candidate now
  addresses every finding. The official 17 USC §102(b), 15 USC §1125 and Oracle
  trademark pages were refreshed on 2026-08-28 as contextual engineering
  sources, not specialist legal review. Both final frozen-diff re-reviews
  returned `PASS` with `P0=0`, `P1=0`, `P2=0`, `P3=0`; they confirmed the
  corrected authority, source-exposure record, 29-row identity, append-only
  prefix, non-authorising routing, restricted third-party scope, independent
  MySQL provider boundary and zero product/lifecycle change.
- Outcome: `GOV-MN-RESTORE-01 COMPLETE`. The functional-inspiration clause of
  `REQ-047`, `REQ-048`, `REQ-050` and all 29 matrix records are active with
  their bounded dispositions and exits. Zero mandatory item remains in this
  corrective governance target. `S06-DFR-03 Desktop Fleet Authenticated Runtime
  Binding` is the prioritised next technical lot, remains separately authorised
  and requires an owner decision packet before implementation authority.

### MySQL Notifier authority revocation `GOV-MN-REV-01` — complete

- Envelope status: `COMPLETE`.
- Exact human authority: inspect the local `mysql-notifier-1.1.8-src` tree and
  revoke every existing decision or permission to incorporate, recreate or use
  the complete MySQL Notifier 1.1.8 feature set in DB-Notifier.
- Workspace and frozen baseline: `C:\Projects\DB-Notifier`, branch `main`,
  commit `0c1da9d9cc11fa0a0678ce1435660e1a833364e1`; index and tracked worktree
  were clean before the audit and plan update.
- Initial shutdown evidence: the canonical preflight returned `PASS` with zero
  matching processes and zero owned listeners. No process was stopped.
- Verifiable objective: establish a sanitised local inventory of the reference
  tree; identify its provenance, licensing and functional families without
  executing or copying it; supersede every current DB-Notifier authority that
  treats MySQL Notifier behaviour or comprehensive parity as a requirements
  source; mark `REQ-050` and the `MN-*`/`MN-Q*` catalogue as revoked and
  historical; preserve existing DB-Notifier-owned behaviour and historical
  evidence without deriving further work from the reference.
- Positive scope: this live plan; read-only inspection of the explicitly named
  external tree; permanent root-agent instructions; the current README,
  migration-plan authority, Design System authority and factual state; one
  sanitised documentary audit report; the instruction-system changelog and
  append-only transition history; the existing development-flow policy
  verifier and its revocation regression; focused documentation, link, policy
  and secret checks; independent read-only review; and one focused local
  commit.
- Historical scope preserved read-only: prior Human Gates, ADRs, completed
  implementation reports, request-traceability evidence, earlier plan
  envelopes, prior changelog entries and prior transition-log entries retain
  their original facts and wording. They no longer grant prospective authority
  where the current revocation supersedes them.
- Protected work and negative scope: do not execute, build, import, translate,
  adapt, copy, track, move, delete or modify the external tree or any of its
  contents. Do not remove existing DB-Notifier-owned product behaviour or
  tests, alter the independent future MySQL database-provider roadmap, change a
  dependency or lockfile, access a database/provider/network service, start a
  product runtime, deploy, publish, push, decide a Human Gate, activate a
  capability or transition lifecycle.
- Authority effect: MySQL Notifier and its source tree, binaries, assets,
  product text, public behavioural documentation, internal architecture and
  historical feature catalogue become provenance/history only. They are not a
  current or future source of requirements, parity targets, design decisions,
  code, tests, assets, roadmap items, acceptance criteria or implementation
  authority. The prohibition applies to each individual feature and to every
  combination or purported complete set. Similar generic outcomes may continue
  only when independently owned and justified by DB-Notifier requirements.
- Provider distinction: this revocation concerns the MySQL Notifier reference
  product and comprehensive-feature mandate. It does not remove MySQL as a
  database-engine candidate from the open provider catalogue; any future MySQL
  provider still requires its own provider-neutral authority, implementation,
  licensing review and homologation.
- Artefact classification and ownership: `AGENTS.md`, the migration plan and
  Design System are `AUTHORITY`; current state is `CURRENT_FACT`; the transition
  log is append-only `HISTORY`; this file is `PLAN`; the new audit report is
  `EVIDENCE`; the README and changelog are governed documentation. The
  coordinating conversation is the sole writer for every path, the Git index
  and the focused commit. Parallel reviewers remain read-only.
- Definition of Ready: shutdown and exact-baseline checks pass; the target tree
  exists and is ignored by the existing protected-tree rule; applicable
  instructions and current state are read; current prospective authorities and
  historical records are classified; no tracked owner work is present; scope,
  ownership, reviewers, checks and stop codes are frozen.
- Definition of Done: the local tree is verified by counts and deterministic
  SHA-256 inventory; the report records provenance, licence families,
  functional families and non-execution limits without reproducing source;
  every current authority permits only historical/provenance mention and
  independently justified DB-Notifier requirements; the MySQL Notifier
  inspiration clause in `REQ-047`, all of `REQ-048` and `REQ-050`, and every
  `MN-*`/`MN-Q*` future exit are revoked; existing product-owned behaviour and
  the independent MySQL provider boundary are explicitly preserved; corpus
  version, current state, changelog and append-only history agree; applicable
  checks and independent review are factual; one focused local commit exists;
  lifecycle remains unchanged.
- Stop rule: any baseline drift, tracked user work, attempt to copy or execute
  external material, unresolved current authority, historical-evidence rewrite,
  provider/product conflation, failed mandatory check, secret/host disclosure
  or need for product-removal, external, Human Gate or lifecycle authority stops
  the affected work without implicit widening.
- Objective stop codes: `AUTHORITY_MISMATCH`, `BASELINE_DRIFT`,
  `SCOPE_OVERLAP`, `DEPENDENCY_UNREADY`, `ISOLATION_FAILURE`,
  `MUTABLE_RESOURCE_COLLISION`, `GATE_FAILURE`, `EXTERNAL_AUTHORITY_REQUIRED`
  and `HUMAN_DECISION_REQUIRED` retain their governed meanings.
- Rollback strategy: before commit, reverse only this envelope's owned
  documentary candidate under separately authorised recovery; after commit,
  preserve history and use a separately authorised focused revert. The
  external tree, product runtime, providers, schemas and external systems have
  no rollback action because this envelope does not mutate them.
- Prospective supersession of predecessor envelopes: every earlier phrase in
  this ledger that authorised clean-room inspiration, public-reference use,
  parity or a MySQL Notifier-derived exit remains an immutable record of its
  former envelope only. It is not executable authority after `GOV-MN-REV-01`.
- Observed audit evidence: the ignored external tree retained 110 files, ten
  directories and 6,554,849 bytes with deterministic SHA-256
  `6f7c58b1c36c91dd2c3d7406aba31eb3d8213e6e737dce23be6fc0eaa1b4c676`.
  It remains unmodified, ignored and untracked, and no target content was
  executed, compiled, decompiled, copied or consulted by network.
- Observed local provenance and risk: local metadata identifies Oracle/MySQL
  Notifier 1.1.8, GPLv2 with an additional linking permission, third-party
  notices, branded assets, privileged operations and a Windows/MySQL-specific
  .NET Framework composition. Authenticity, chain of custody, reproducibility,
  binary provenance and legal compatibility remain unproved; the tree is
  quarantined historical evidence, not an implementation blueprint.
- Verification evidence: development-flow policy `123` assertions; standalone
  regression `98`; documentation `447` comment-capable files; Markdown `993`
  local links in `230` files; secret scan and `git diff --check` both passed.
  The post-change target identity matched, and the 766,612-byte append-only
  history prefix retained SHA-256
  `8aa11f2ceebf3a2ab3094476ec77247d2066b668e14a2ef04dd44c9317167bfc`.
- Review evidence: initial independent frozen-diff review reported `P0=0`,
  `P1=0`, `P2=1`, `P3=0`. The sole `P2` found missing policy assertions for
  non-effects, README, vision, the bounded `7.0.0` entry and transition log;
  all named boundaries are now asserted and the focused verifier passes.
- Final independent review: `P0=0`, `P1=0`, `P2=0`, `P3=0`; the reviewer
  independently confirmed the corrected assertions, unchanged historical
  matrix, append-only prefix, target-tree identity and zero tracked target
  files. No actionable finding remains.
- Post-review control correction: one policy execution failed because the
  administrative reconciliation had renamed the mandatory `- Execution mode:`
  plan label. The label was restored with canonical `SEQUENTIAL_ONLY` while
  retaining `SINGLE_OWNER` as the separate execution topology; the next policy
  execution passed 123 assertions. The failed result is not relabelled.
- Gate applicability: `Quick` and canonical `Full` are not applicable to this
  policy/documentation-only revocation because product behaviour, dependencies,
  generated artefacts and runtime composition are unchanged. They are not
  treated as `NOT_RUN` stages or substituted by the focused checks.
- Disposition: `GOV-MN-REV-01 COMPLETE`; the prospective MySQL Notifier
  authority is revoked for individual features, combinations and the complete
  set while historical truth, existing DB-Notifier-owned behaviour and the
  independent future MySQL provider boundary remain intact.
- Delivery: this completed record is included in the single focused local
  commit required by repository policy. Its object ID is reported in the final
  hand-off rather than embedded here.
- Remaining mandatory items for `GOV-MN-REV-01`: `0`. Deleting or relocating
  the external tree, removing existing product behaviour, implementing a MySQL
  provider or entering a later lifecycle stage are separate actions and not
  remainder of this target.

### Desktop Fleet Agent/API read-only integration `S06-DFR-02` — complete

- Envelope status: `COMPLETE`.
- Exact human authority: integrate the DB Notifier Tray with a provider-neutral
  Agent/API read-only source as the smallest coherent successor to
  `S06-DFR-01`; preserve the clean-room boundary, independent product identity,
  current baseline, gates and `STATE-06 INTEGRATION`; do not access or use the
  protected external source tree.
- Workspace and frozen baseline: `C:\Projects\DB-Notifier`, branch `main`,
  commit `d805e86b313d084d1d4f44c4b7559e699e4c6eda`; index and tracked worktree
  were clean without enumerating untracked material.
- Initial shutdown evidence: the mandatory preflight returned `PASS`; no
  process was stopped, matching processes were `0` and owned listeners were
  `0`.
- Public behavioural basis: the clean-room matrix and public Oracle/MySQL
  references already frozen in
  `docs/STATE-06-Desktop-Fleet-Read-Only-Reconciliation-Report.md`. No
  proprietary source, binary, artwork, wording, trade dress or vendor
  architecture is an input.
- Verifiable objective: expose one bounded human-authorised API projection of
  the latest provider-neutral Agent observations; consume that versioned
  projection through an HTTPS `IDesktopFleetSnapshotSource`; map denied,
  offline, incompatible and failed transport outcomes to stable non-secret
  reconciliation results; and prove that the existing Tray coordinator can use
  the source without enabling provider or administrative behaviour.
- Positive scope: this plan; the minimum Application wire/read contracts;
  authorisation-scoped PostgreSQL read projection; one human-policy Server GET
  endpoint; one bounded Infrastructure HTTP adapter; the minimum WPF
  composition seam needed to admit an injected authenticated source while
  preserving the current demonstration default; focused unit, integration and
  architecture regressions; the owning Design System clarification; factual
  report, current state and append-only history; applicable local validation;
  and one focused local commit.
- Frozen read-only scope: provider implementations and registry activation;
  Agent assignment, monitoring and ingestion behaviour; schemas and migrations;
  Dashboard runtime; package and lockfile identities; installers, update
  channels and authoritative notification delivery. Existing demonstration and
  review modes retain their authority and factual labels.
- Protected work and negative scope: no MySQL or other provider/driver,
  discovery, configuration mutation, Start/Stop/Restart, database or OS
  administration, authoritative notification, firewall, tunnel, infrastructure
  mutation, real OIDC/identity-provider flow, credential provisioning, deploy,
  publication, push, Human Gate, activation, homologation or lifecycle
  transition. Agent mTLS identity MUST NOT be reused as Desktop human identity.
  The protected external source tree and every item beneath it remain strictly
  inaccessible.
- Identity and activation boundary: the Server read remains protected by the
  existing human JWT policy and `instances.read` scope. The HTTP adapter accepts
  only a caller-owned authenticated client and never acquires, persists, logs
  or accepts a token through arguments, files or environment variables. The
  ordinary executable retains the labelled local demonstration until a
  separately authorised Desktop human-identity composition exists; tests may
  inject an isolated authenticated client. This is an activation dependency,
  not authority to weaken the API or reuse Agent identity.
- Artefact classification and ownership: governing prompts and Design System
  are `AUTHORITY`; current state is `CURRENT_FACT`; the state log is append-only
  `HISTORY`; this file is `PLAN`; source and tests are `IMPLEMENTATION`; the
  report is `EVIDENCE`. This coordinating conversation is the sole writer for
  every path, Git index and focused commit.
- Execution topology and mutable resources: `SINGLE_OWNER` and
  `SEQUENTIAL_ONLY`. HTTP reads are bounded and cancellation-aware; the existing
  coordinator retains the single-flight boundary; every validation process is
  shut down before the next technical stage.
- Definition of Ready: shutdown and exact baseline checks passed; tracked state
  is clean; the current Agent observation state, human authorisation store,
  Server endpoint policy, bounded HTTP reader, Tray reconciliation contract,
  WPF composition, test owners and negative scope are identified. The missing
  Desktop human identity is classified as a separately authorised activation
  dependency rather than silently implemented.
- Definition of Done: an authenticated and scope-filtered API request returns
  only coherent provider-neutral latest-observation fields; invalid or
  inconsistent stored evidence fails closed; the client enforces HTTPS, exact
  protocol/schema, response bounds and non-secret typed failures; the existing
  Tray reconciliation accepts the source and retains prior evidence after read
  failure; ordinary WPF startup remains demonstration-only; focused and
  canonical checks are recorded factually; one focused local commit exists;
  lifecycle remains unchanged.
- Stop rule: any protected-path exposure, baseline drift, scope overlap,
  licensing conflict, unready implementation dependency, insecure identity
  shortcut, failed required gate or need for provider, administration,
  notification, external-infrastructure or lifecycle authority stops the
  affected work without implicit widening or evidence replacement.
- Objective stop codes: `AUTHORITY_MISMATCH`, `BASELINE_DRIFT`,
  `SCOPE_OVERLAP`, `DEPENDENCY_UNREADY`, `ISOLATION_FAILURE`,
  `MUTABLE_RESOURCE_COLLISION`, `GATE_FAILURE`, `EXTERNAL_AUTHORITY_REQUIRED`
  and `HUMAN_DECISION_REQUIRED` retain their governed meanings.
- Rollback strategy: before commit, reverse only this envelope's owned
  candidate under separately authorised recovery; after commit, preserve
  history and use a separately authorised focused revert. No provider, schema,
  external service or lifecycle rollback applies.
- Implemented outcome: `desktop-fleet.v1` carries only bounded latest-observation
  fields from an `instances.read`-scoped human Server endpoint to an HTTPS
  `IDesktopFleetSnapshotSource`. Persistence rejects incoherent Agent/state/
  sample joins, the client maps typed non-secret failures and the existing
  coordinator preserves single-flight, freshness and last-accepted semantics.
- Identity disposition: WPF admits an already authorised injected source, while
  the ordinary executable remains the labelled local demonstration. No token
  argument, token file, environment token, Agent-certificate reuse, OIDC flow,
  egress activation or external service was introduced.
- Focal evidence: Application/store and HTTP-source regressions `10/10`;
  isolated authenticated HTTPS endpoint `3/3`; WPF/source architecture `2/2`;
  WPF build with zero warnings and zero errors. Four earlier focal invocations
  exposed and corrected three syntax/analyser issues plus one over-broad test
  assertion; those failures remain factual.
- Shutdown evidence: every executable stage was preceded by the canonical
  preflight. One pre-`Doctor` invocation found a project-owned PowerShell reader
  left by this task; exact process metadata proved ownership, only that PID was
  terminated, and the next preflight passed with zero processes/listeners.
- Development evidence: `Doctor` passed. Final `Quick` passed as `NON_GATE`
  with Release build, unit `543/543`, architecture `101/101`, Dashboard `74/74`
  and the included asset, localisation, documentation, Markdown and script
  checks.
- Canonical evidence: exactly one `Full` completed
  `DISPOSITION|PASS|stage=All`; unit `543/543`, architecture `101/101`,
  integration `171/171`, WPF `10/10`, line coverage `83.39%`, branch coverage
  `56.46%`, vulnerability/runtime/legacy/bundle/Web/browser/consolidated gates
  passed. Browser and consolidated harnesses used isolated local processes and
  local test data only.
- Final review disposition: coordinating frozen-scope, clean-room, licensing,
  architecture, identity, security and factual-state review closed with
  `P0=0`, `P1=0`, `P2=0`, `P3=0`. No independent reviewer was assigned under
  the current single-conversation authority; runtime activation remains absent.
- Delivery: this completed record is included in the single focused local
  commit required by repository policy. Its object ID is reported in the final
  hand-off rather than embedded here.
- Remaining mandatory items for `S06-DFR-02`: `0`. Desktop human identity and
  egress activation, provider integrations, discovery, authoritative
  notifications and administration are separately authorised future work, not
  remainder of this target.
- Resulting state: `STATE-06 INTEGRATION`, eligibility and
  `MOD-12 ActivationState=None` remain unchanged; no Human Gate, provider
  support, runtime activation or external authority is inferred.

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

`GOV-MN-RESTORE-01` is `COMPLETE`. The corrected owner intent restores the
MySQL Notifier functional-inspiration clause of `REQ-047`, `REQ-048`, `REQ-050`
and the 29-row functional matrix as non-authorising routing for independently
implemented DB-Notifier outcomes. The reference is not an implementation base,
clone mandate or compatibility target; source-exposed provenance analysis is
separated from implementation/test authorship, and literal reference-tree
material remains outside the MIT deliverable without the separately evidenced
rights and distribution decision. Focused policy `127`, standalone regression
`98`, documentation `447`, Markdown `995`, secret and diff checks passed; both
final independent re-reviews closed at zero findings. `STATE-06 INTEGRATION`
remains unchanged. The next action is an owner decision on whether to request a
non-implementing authority packet for `S06-DFR-03 Desktop Fleet Authenticated
Runtime Binding`.

As the preserved historical predecessor result, `GOV-MN-REV-01` remains
`COMPLETE` for the owner's earlier literal wording. Its report, commit, failed
and passing checks, reviews and append-only entry remain unchanged facts; its
prospective prohibition is superseded by `GOV-MN-RESTORE-01`.

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

- `2026-08-28`: `GOV-MN-REV-01` statically audited the ignored MySQL Notifier
  1.1.8 tree and revoked every prospective functional-use authority while
  preserving history, existing DB-Notifier-owned behaviour and the independent
  future MySQL provider boundary. Corpus `7.0.0`, Design System `3.4.1`, focused
  checks and append-only integrity passed; an initial `P2` regression gap was
  corrected and final independent review closed with zero findings.
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
