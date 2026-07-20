# STATE-06 — Audit Remediation R0 Report

## Disposition

- Increment: `R0 — Integridade dos gates, dependências e runners`.
- Execution authority: the explicit local-only authorisation issued by Bruno on 2026-07-20.
- Local implementation: completed.
- Automatic result in the authorised local scope: `APROVADO`.
- Human decision on the increment: `PENDENTE`.
- Lifecycle: remains `STATE-06 INTEGRATION`; no transition was requested or performed.
- MOD-12: unchanged and inactive; no `OBSERVER`, O1, LLM, recommendation, command or automation was enabled.

This report is evidence of one local remediation increment. It is not a production, provider, PostgreSQL, remote-CI or external-service homologation.

## Authorised boundary

The increment was limited to the NuGet gate, solution lockfiles, STATE-05/06 runner isolation and cleanup, harness-only rate limiting, proportional CI integration, coverage, architecture evidence and PowerShell/Pester compatibility. Network access, downloads, remote CI execution, push, deploy, real databases or services, product operational changes, R1–R8, R7-A0, O1 and lifecycle/mode progression remained prohibited.

The mandatory preflight started from clean commit `b39e13938801459cc3742a64ed35dec076d77c49`, with no verified DB-Notifier process or listener. Temporary runtimes used by this increment were local and were stopped before hand-off.

## Implemented remediation

### NuGet and lockfiles

- The NuGet vulnerability gate now obtains the complete project set from the solution and evaluates every project's target frameworks independently through MSBuild.
- A legitimate NuGet version-one no-finding report may omit `frameworks`; optional framework detail, when present, must match the independent MSBuild inventory exactly.
- The Pester suite covers malformed, incomplete, future-schema, divergent-source, omitted-project, direct-vulnerability, transitive-vulnerability and both valid empty-report shapes.
- The consolidated host now has a generated and tracked `packages.lock.json`.
- A dedicated offline fixture clears package sources for local locked-restore proof without changing the canonical `NuGet.config`.
- `verify-dotnet-lockfiles.ps1` proves that all 17 solution projects have tracked lockfiles and that locked restore leaves the pre-existing repository status unchanged.

### Runners and diagnostics

- STATE-05 now requires PowerShell 7, owns a GUID-named profile/evidence root, captures the actual Vite listener PID, bounds every Node process and CDP command, waits for stable route/layout state and performs bounded process/profile/root cleanup retries.
- STATE-06 now creates the version-four `runId` before host startup and carries it into the host, Agent store prefix, browser audit and cleanup ownership checks.
- STATE-06 cleanup targets only the exact browser profile, host tree, Node process and Agent helpers/root correlated to that run. It no longer identifies another concurrent run by comparing a shared before/after root set.
- The consolidated build disables persistent build-server reuse.
- Both CI browser jobs can retain only bounded, redacted JSON failure summaries. Screenshots, profiles, stores, raw response bodies and broad logs are not uploaded.
- Node/CDP commands have per-command deadlines, and the owning PowerShell processes have global Node deadlines. Cleanup remains in `finally`.

### Harness rate limiting

- Test-only consolidated control endpoints use `ConsolidatedHarnessRateLimitPolicy` instead of sharing the ordinary Dashboard/human partition.
- The test policy has a fixed 300-request/minute, zero-queue budget keyed by the immutable owning `runId`; request headers cannot create extra partitions.
- Normal Server production limits remain unchanged at 60 human, 120 Agent and 5 enrollment requests per minute. The test-only policy is absent from normal Server composition.

### Coverage, architecture, PowerShell and CI

- The coverage gate preserves the 70% line and 45% branch aggregate floors and now also fails if any of the ten expected unit-tested product components is absent or has no measured line coverage.
- Architecture tests prove lockfile presence, test-only/normal rate-limit separation, exact runner ownership, bounded CDP work and proportional CI wiring.
- The legacy gate fails early outside Windows PowerShell 5.1 Desktop and continues to require the pinned Pester 3.4.0 module without installing it.
- CI keeps STATE-05 on `pwsh`, verifies tracked lockfiles, and adds a bounded Windows STATE-06 consolidated job after the .NET and Dashboard jobs.

## Execution observations

Two runner defects were exposed while validating R0 and were corrected before the final successful series:

1. One STATE-05 report observed an unstable compact route/layout sample. The auditor now waits for the exact hash, completed document, requested viewport and two animation frames before measuring.
2. One later STATE-05 Node execution failed without retaining a diagnostic. The runner now writes a bounded JSON category and redacted Node error tail only when a diagnostic directory is explicitly supplied.

These failed attempts are not counted as the final three-run completion evidence.

The remediation also removed two verified old temporary artefacts that predated the final series: one orphaned dedicated Chrome profile root and one old `DBNotifier-State05-Audit` evidence root. Neither had an associated live process. They were temporary files and are not recoverable from the filesystem cleanup; no user browser/profile or unrelated process was touched.

## Automatic evidence

Environment observed on 2026-07-20:

- Windows host; local .NET SDK `10.0.301`.
- Node.js `24.18.0`; npm `11.16.0`.
- Chrome `150.0.7871.125`, always launched with a dedicated temporary profile.
- Windows PowerShell `5.1 Desktop` with Pester `3.4.0` for the legacy gate; PowerShell 7 for modern runners.

| Gate | Observed result |
|---|---|
| Release build | Passed, 17 projects, 0 warnings, 0 errors |
| .NET tests | Passed, 385/385: 332 unit, 34 architecture, 19 integration |
| .NET format | Passed with no changes required |
| Coverage | Passed: 78.9% lines, 49.51% branches, 10/10 required components present |
| Legacy/Pester | Passed: 29 passed, 1 allow-listed conditional skip, 32.08% command coverage (290/904) |
| Incompatible PowerShell host | `pwsh` rejected early as intended for the pinned legacy gate |
| Lockfiles | Passed: 17/17 tracked; offline locked restore left repository status unchanged |
| NuGet adversarial fixtures | Passed under Pester, including valid SDK-empty shape and direct/transitive rejection |
| Dashboard static/test gate | Passed: toolchain, brand, provider icons, tokens, localisation, typecheck, comments, 524 Markdown links, 60/60 tests and production build |
| Code documentation | Passed for 286 comment-capable source files |
| Secrets | Current worktree and available history passed without printing values |
| Bundle validation | Passed |
| Git integrity | `git fsck --full` exited 0; existing dangling local objects were reported but are not repository corruption |

Final consecutive runner evidence:

- STATE-06 consolidated final series: 3/3 passed in 84.0 s, 70.1 s and 72.1 s.
- STATE-05 final stabilised series: 3/3 passed in 83.2 s, 82.5 s and 83.7 s.
- Each STATE-05 run covered 96 viewport samples and 32 forced-colour route samples across `pt-BR`/`en-GB` and Light/Dark.
- After the series, the shutdown proof found zero matching DB-Notifier runtime process, listener, dedicated browser/profile, runner root, Agent store/root or coverage root.

## Explicit limitations

- Remote GitHub Actions was not executed because the authority explicitly prohibited remote CI. The workflow change is locally inspected and covered by architecture tests, not remotely observed.
- No package/advisory source was contacted. The real `dotnet list package --vulnerable` command was not repeated in this turn; the corrected gate was exercised against the captured legitimate empty shape and adversarial local fixtures.
- `npm audit` and an online NuGet advisory refresh were not run because they require external access.
- Action tags remain mutable. No SHA was invented or changed without online provenance verification, as required by the accepted plan.
- No PostgreSQL instance, provider runtime, production persistence, PKI/IdP, deployment or external service was used.

## Rollback

The increment can be reverted as one focused Git commit while retaining this report as historical evidence. Reverting would restore the known incompatible NuGet gate and weaker runner cleanup, so such a rollback must leave those gates explicitly blocked rather than treating them as trustworthy approval evidence.

## Next gate

The only next decision for this increment is a separate human review of this report and focused diff. Accepting R0 would close only R0; it would not authorise R1, any later remediation lot, R7-A0/O1, AIOps mode promotion or lifecycle transition.
