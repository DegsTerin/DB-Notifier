# STATE-06 — Audit Remediation R2-A Report

## Disposition

- Increment: `R2-A — Contenção da superfície de comandos`.
- Findings: `AUD-H03`, `AUD-H04` and `AUD-M10` only.
- Authorised baseline: `40daad7a27a60da2e2cbb99c60aaed417a9591a2`.
- Execution authority: Bruno's explicit local-only authorisation issued on 2026-07-20.
- Local implementation and R2-A-specific verification: completed.
- Automatic result in the authorised R2-A scope: `APROVADO`.
- Independent human decision: `ACEITO COM RESSALVA`, recorded from Bruno's exact decision `ACEITO O R2-A COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.` on 2026-07-20.
- Repository-wide qualification: the unfiltered .NET solution test command remains `BLOQUEADO` only by the exact pre-existing R0 architecture assertion already reserved after R1; it was not changed under this authority.
- Lifecycle: remains `STATE-06 INTEGRATION`; no transition was requested or performed.
- MOD-12: unchanged and inactive; no `OBSERVER`, O1, LLM, recommendation, command or automation was enabled.

This report proves local containment. It does not implement an administrative protocol, command executor, provider control, PostgreSQL operation or production support.

## Authorised boundary

The increment permitted only local source, tests, documentation, builds and temporary local runtimes needed to contain command creation, polling and acknowledgement, remove normal Agent delivery composition and isolate the execution-ineligible sandbox receipt store. It prohibited R2-B, `CommandAttempt`, executor, SQL, shell, utilities, Start/Stop/Restart, administrative credentials, operational schema migration, mutation or deletion of persisted commands, external access, downloads, remote CI, push, deploy, real databases or services, R3–R8, R7-A0/O1, LLM, recommendations, automation, mode promotion and lifecycle transition.

The mandatory shutdown preflight started from the exact clean baseline and proved zero project-owned processes, listeners and visible windows. No pre-existing operational command store was connected and no operational row was read, changed or deleted.

## Implemented containment

### Normal Server API

- The v1 creation, poll and acknowledgement handlers no longer bind administrative payloads or resolve command persistence and delivery services.
- Authenticated legacy routes are stable tombstones returning HTTP `503`, `application/problem+json`, `Cache-Control: no-store`, code `command.surface_unavailable` and `retryable=false`.
- `IServerCommandDeliveryStore` and `ServerCommandDeliveryStore` are absent from normal Server dependency injection, so persisted commands cannot be offered by normal polling.
- Catalog and audit retain their existing shared authorised-operations service; the command creation method has no mapped normal route.

### Normal Agent Worker

- `IAgentCommandInboxStore`, `ICommandDeliveryTransport`, `HttpCommandDeliveryTransport` and `AgentCommandPollingWorker` are absent from normal composition.
- Startup validates `CommandPollingEnabled` immediately and rejects it with `command.polling_durable_protocol_unavailable` regardless of the observation-synchronisation flag.
- Existing v1 types remain uncomposed compatibility code; they were not reactivated or extended.

### Exact sandbox and durable `Never`

- Entry still requires the exact `--sandbox-command-transport` marker and its strict bounded argument set.
- The child host creates a dedicated fixture-owned `command-transport-receipts.sqlite` file. It does not put transport state or receipts into the normal Agent database.
- The isolated schema contains only fencing/pending-message state and receipt-only rows. Receipt states are prefixed `Receipt*` and contain no running, succeeded, failed or result fields.
- Every receipt persists `ExecutionPolicy` as `Never`; SQLite enforces `execution_policy = 'Never'`, duplicate matching revalidates it and acknowledgement rehydration rejects any invalid policy.
- The normal Agent command inbox and its legacy sandbox transport table remain empty during the multiprocess E2E.
- No operational Agent or Server migration was created or changed.

### Provider and execution boundary

- The PostgreSQL provider remains unchanged: `Start`, `Stop` and `Restart` are still `Unsupported`.
- No executor, `CommandAttempt`, administrative identity, post-probe, SQL, shell or provider-control path was added.
- R2-B remains a separate future programme and is not implemented or authorised.

## Completion evidence

| R2-A criterion | Observed evidence |
|---|---|
| Normal composition cannot create, offer, receive or execute commands | Three persistence-free tombstones; Server delivery store and Agent inbox/transport/worker absent from normal DI; architecture inventory passed |
| Stable non-sensitive HTTP and startup outcomes | Three direct endpoint tests proved identical 503 problems even with malformed JSON; two flag combinations proved the stable startup code |
| Configuration cannot activate polling | Startup validation is unconditional on `CommandPollingEnabled`; normal composition contains no polling worker or delivery transport |
| Existing command rows are not offered or changed | Normal delivery store is unregistered; the increment contains no data access, migration, deletion or rewrite procedure for existing rows |
| Sandbox rows cannot resemble executable work | Dedicated receipt database, `Receipt*` states, no result fields, durable `Never` constraint and normal inbox-empty E2E proof |
| PostgreSQL administrative capabilities remain unavailable | Provider source and tests remain unchanged; no control capability was added |

## Automatic evidence

Environment observed locally on 2026-07-20: Windows, .NET SDK `10.0.301`, Windows PowerShell `5.1 Desktop` with pinned Pester `3.4.0`, and already installed local dependencies only.

| Gate | Observed result |
|---|---|
| Shutdown preflight | Passed: zero project-owned process, listener or visible window before work |
| Release build | Passed for 18 solution projects, 0 warnings and 0 errors |
| Focused R2-A unit tests | Passed, 53/53 |
| Focused architecture tests | Passed, 3/3 |
| Multiprocess command sandbox E2E | Passed, 1/1, including isolated receipt persistence and empty normal inbox |
| Full .NET solution tests | 403 passed; 1 exact pre-existing R0 architecture assertion failed, so the aggregate command remains non-green |
| Architecture suite excluding the exact baseline contradiction | Passed, 34/34 |
| Coverage | Passed: 79.39% lines, 50.72% branches and 10/10 required components present |
| .NET format | Passed with no changes required |
| Lockfiles | Passed: 18/18 tracked; offline locked restore left repository state unchanged |
| Legacy/Pester | Passed: 29 tests, 1 allow-listed conditional skip, 32.08% command coverage (290/904) |
| Fail-closed runtime audit | Passed: liveness 200, protected HTTP 426, composed workers disabled, command polling absent and persistence not initialised |
| Bundle validation | Passed |
| Code documentation | Passed for 293 comment-capable source files; new and modified comments reviewed as British English |
| Markdown links | Passed for 534 local links in 118 Markdown files |
| Secrets | Passed for the current non-ignored worktree and available Git history without printing matched values |
| Final shutdown and cleanup | Passed: zero project-owned process, listener, visible window or R2 temporary root |

The global failure remains `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`: it expects fixed artifact filenames while the accepted R0 workflow uses bounded diagnostic directories and run-attempt-qualified names. That contradiction predates R2-A, is unchanged in this diff and is explicitly outside the authority.

## Explicit limitations

- Direct tombstone tests execute the route delegates without an identity provider; the normal runtime still applies its existing protected-transport and authentication middleware before an authorised caller can receive the typed 503 response.
- The legacy v1 command contracts, stores and runner types still exist as uncomposed code. R2-A proves normal route and dependency isolation; it does not delete compatibility code or historical tables.
- The sandbox Server response journal already persists the exact envelope containing `Never`; the new dedicated Agent receipt database closes the separate loss when receiving and rehydrating a fixture.
- No real PostgreSQL, monitored provider, credential, command, notification, browser, remote CI or external service was exercised.
- Online NuGet advisory refresh and `npm audit` were not run because external access and downloads were prohibited.

## Rollback and stop condition

The safe rollback is to keep all three normal routes unavailable and leave command delivery uncomposed. Never restore the v1 operational handlers or normal Agent polling registrations. Preserve the isolated receipt database only for bounded fixture diagnosis, then remove it through the owning sandbox cleanup. If compatibility requires a different HTTP status or a new contract, or if implementation requires an operational migration or data mutation, stop and request separate authority.

## Human decision and next boundary

Bruno accepted R2-A on 2026-07-20 with the exact reservation `ACEITO O R2-A COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.` R2-A and `AUD-H03`, `AUD-H04` and `AUD-M10` are therefore accepted and closed only by containment in this bounded local scope. The repository-wide R0 assertion remains an explicit pre-existing blocker and may not be corrected under this decision. The acceptance does not authorise R2-B, R3, commands, execution, AIOps activation, lifecycle promotion or transition.
