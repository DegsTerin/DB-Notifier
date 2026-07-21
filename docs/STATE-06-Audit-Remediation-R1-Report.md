# STATE-06 — Audit Remediation R1 Report

## Disposition

- Increment: `R1 — Verdade e durabilidade da notificação local`.
- Finding: `AUD-H02` only.
- Authorised baseline: `3ee505ec55cab84f1af3043491355fb9f00b5f43`.
- Execution authority: Bruno's explicit local-only authorisation issued on 2026-07-20.
- Local implementation and R1-specific verification: completed.
- Automatic result in the authorised R1 scope: `APROVADO`.
- Independent human decision: `ACEITO COM RESSALVA`, recorded from Bruno's exact decision `ACEITO O R1 COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.` on 2026-07-20.
- Repository-wide qualification: the unfiltered .NET solution test command remains `BLOQUEADO` by one R0 architecture assertion that already contradicted the workflow in the authorised baseline; it is not an R1 regression and was not changed under this authority.
- Lifecycle: remains `STATE-06 INTEGRATION`; no transition was requested or performed.
- MOD-12: unchanged and inactive; no `OBSERVER`, O1, LLM, recommendation, command or automation was enabled.

This report proves a bounded local remediation. It is not proof that the Windows Shell displayed a notification, and it is not production, provider, PostgreSQL, remote-CI or external-service homologation.

## Authorised boundary

The increment was limited to the local reconciled-notification truth and durability defect described by `AUD-H02`. It permitted local source, tests, documentation, builds and temporary test-only runtimes needed to prove that `Accepted` follows the actual Windows boundary rather than volatile queue admission. Visible Windows notification delivery, external access, downloads, remote CI, push, deploy, real databases or services, ordinary operational activation, R2–R8, R7-A0, O1, LLM, recommendations, commands, automation, mode promotion and lifecycle transition remained outside the boundary and were not performed.

The mandatory shutdown preflight verified the exact authorised baseline, a clean tracked worktree and no matching DB-Notifier process or listener before implementation began.

## Implemented remediation

### Durable ledger and factual outcomes

- Ledger schema v2 separates `Queued`, `Attempting`, `Accepted`, `Retryable`, `Rejected` and `Suppressed` with stable numeric values for conservative v1 reading.
- A complete validated page is atomically staged as durable `Queued` or `Suppressed` work before the first platform call. Each new item receives a positive monotonic `QueueSequence` that survives server reorder and coordinator restart.
- Pending entries are never discarded by pruning. Capacity beyond the 256-entry bound fails explicitly instead of silently losing an item.
- The cursor advances only after every item in the page has a durable terminal decision. A pending ledger item missing from the replayed server page fails closed as a conflict.
- `Attempting` is committed before the non-transactional Windows call. An interrupted attempt becomes `Rejected` with `delivery.uncertain_after_restart` and is not replayed, preventing an unprovable duplicate.
- A retryable result observes a five-second backoff and a two-attempt budget. Delivery itself has a five-second deadline; bounded timeout or boundary failure becomes terminal factual uncertainty.

### Windows boundary

- The reconciled WPF path no longer treats admission to the ordinary in-memory fallback queue as acceptance.
- The tested modern-first boundary returns `Accepted` only after the modern publisher returns success or the direct legacy `NotifyIcon.ShowBalloonTip` call returns normally.
- A busy legacy boundary returns `Retryable`, leaving the durable ledger as the owner of pending work. The existing volatile queue remains unchanged for the separate demonstration and close-to-Tray behaviours.
- Platform API acceptance still does not prove that Focus Assist, user policy or the Windows Shell displayed the notification.

### Compatibility and isolation

- Every v2 owner also acquires the legacy v1 ownership fence, preventing a pre-R1 process from creating a concurrent split ledger. Earlier `Accepted` and `Attempting` entries are conservatively migrated to terminal `Rejected` uncertainty because pre-R1 acceptance cannot be proved after the fact.
- Normal composition remains disabled by default. The exact existing sandbox marker, explicit notification opt-in, loopback HTTPS and dedicated temporary ledger constraints remain required.
- A dedicated `net10.0-windows10.0.22621.0` test project exercises the exact WPF boundary policy without invoking a real notification or requiring generated visual assets.
- NuGet gate fixtures now enumerate the eighteenth solution project; no dependency or package version changed.

## Criterion evidence

| R1 completion criterion | Observed evidence |
|---|---|
| Burst items `2..N` are durable before acceptance | Unit test stages all three items before the first boundary delegate runs and confirms delivery in queue order |
| Crash boundaries produce recovery or factual terminality without duplicate replay | Tests cover durable `Queued` restart, interrupted `Attempting` rejection, accepted restart deduplication and conservative v1 migration |
| Restart, replay, reorder, busy fallback and unavailable modern publisher | Unit, integration and WPF boundary suites cover each condition; durable sequence wins over page reorder |
| Normal composition remains disabled | Existing exact sandbox guards are preserved; architecture isolation test confirms no volatile-queue acceptance path |
| Multiprocess ledger/cursor and WPF sink proof | A child `dotnet test` process holds the real file fence while the parent proves exclusion; three WPF policy tests execute without visible UI |
| Final cleanup | The bounded child process, HTTPS test host, SQLite fixtures and temporary ledger roots were stopped or removed by their owning tests; final shutdown proof is recorded below |

## Automatic evidence

Environment observed locally on 2026-07-20: Windows, .NET SDK `10.0.301`, Windows PowerShell `5.1 Desktop` with pinned Pester `3.4.0`, and already installed local dependencies only.

| Gate | Observed result |
|---|---|
| Release build | Passed for 18 solution projects, 0 warnings and 0 errors |
| R1 unit tests | Passed, 14/14, including controlled-time deadline and retained legacy-fence proofs |
| R1 integration tests | Passed, 4/4, including the operating-system multiprocess ledger fence |
| WPF boundary tests | Passed, 3/3 without emitting a visible notification |
| R1 architecture evidence | Passed, including direct tested boundary use and exclusion of volatile queue acceptance |
| Full .NET solution tests | 398 passed; 1 pre-existing R0 architecture assertion failed, so this aggregate command is not green |
| Architecture suite excluding the exact baseline contradiction | Passed, 34/34 |
| Coverage | Passed: 79.01% lines, 50.37% branches and 10/10 required components present |
| .NET format | Passed with no changes required |
| Lockfiles | Passed: 18/18 tracked; offline locked restore left the tracked baseline unchanged apart from the authorised worktree |
| Legacy/Pester | Passed: 29 tests, 1 allow-listed conditional skip, 32.08% command coverage (290/904) |
| Code documentation | Passed for 289 comment-capable source files |
| Markdown links | Passed for 528 local links in 117 Markdown files |
| Fail-closed runtime audit | Passed: liveness `200`, protected HTTP endpoints `426`, workers disabled and persistence not initialised |
| Consolidated local sandbox | Passed with an isolated HTTPS loopback host and dedicated browser profile; owned resources were cleaned |
| Secrets | Passed without printing matched values |
| Final shutdown proof | Passed: 0 matching DB-Notifier processes, 0 owned listeners, 0 visible project windows and 0 R1 temporary roots |

The single full-suite failure is `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`. It expects the literal filenames `state05-dashboard-failure.json` and `state06-consolidated-failure.json`, while the baseline workflow already uses bounded diagnostic directories, `*.json` paths and run-attempt-qualified artifact names. Both sides of that contradiction are present in commit `3ee505ec55cab84f1af3043491355fb9f00b5f43`. Altering the R0 workflow or its assertion was not authorised by R1, so the failure remains visible and outside the claimed R1 pass.

## Explicit limitations

- No visible Windows notification, Notification Centre entry, Focus Assist behaviour, click path or human accessibility sample was executed. Such a sample requires separate human authorisation.
- The Windows API and ledger do not share a transaction. R1 deliberately prefers terminal uncertainty after an interrupted `Attempting` call instead of risking a duplicate; it does not claim exactly-once visible delivery.
- The v1 migration cannot retroactively prove whether an earlier volatile fallback was displayed. It therefore rejects ambiguous earlier acceptance without replay.
- The multiprocess test proves cooperative operating-system file fencing, not defence against a malicious local administrator, offline file replacement, disk failure or full-storage rollback.
- The 16-item page, 256-entry ledger, two-attempt budget and five-second backoff/deadline are sandbox controls, not operational fleet sizing.
- Remote CI, online NuGet advisory refresh, `npm audit`, downloads, PostgreSQL/provider runtime, external services and production environments were not exercised.

## Rollback

Disable the exact reconciled sandbox consumer and preserve the v2 ledger for diagnosis. A code revert must not downgrade or reinterpret v2 pending entries as delivered, and must not restore acceptance by volatile queue admission. If a rollback is required, the affected sandbox remains fail-closed until the ledger is inspected under a separately authorised procedure.

## Human decision and next boundary

Bruno accepted R1 on 2026-07-20 with the exact reservation `ACEITO O R1 COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.` R1 and `AUD-H02` are therefore accepted and closed only in this bounded local sandbox scope. The repository-wide R0 assertion remains an explicit pre-existing blocker and may not be corrected under this decision. The acceptance does not authorise a visible sample, R2 or later lots, R7-A0/O1, AIOps activation or lifecycle transition.
