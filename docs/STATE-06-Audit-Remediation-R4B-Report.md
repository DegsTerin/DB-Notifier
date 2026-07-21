# STATE-06 — Audit Remediation R4-B Report

## Disposition

- Increment: `R4-B — Ownership distribuído e durabilidade do delivery`.
- Finding: `AUD-H07` only.
- Authorised baseline: `20323defa34fdb7b0b475400b6bb474217042baf`.
- Execution authority: Bruno's explicit local-only authorisation issued on 2026-07-21.
- Local implementation and R4-B-specific verification: completed.
- Automatic result in the authorised R4-B scope: `APROVADO`.
- Independent human decision: `PENDENTE`.
- Lifecycle: remains `STATE-06 INTEGRATION`; no transition was requested or performed.
- Normal delivery: disabled and startup-refused; no external publisher or channel adapter is registered.
- MOD-12: unchanged and inactive; no `OBSERVER`, O1, LLM, recommendation, command or automation was enabled.

This report proves the central PostgreSQL ownership protocol in one disposable local laboratory. It does not activate
delivery, homologate PostgreSQL generally, prove an external channel, apply a migration operationally or close the
independent Human Gate for R4-B.

## Authorised boundary

The increment permitted local source, tests, documentation, synthetic publisher/receiver behaviour, one minimal
Server EF migration, generated PostgreSQL SQL and a disposable PostgreSQL laboratory. The laboratory was required to
use an image already present locally, `--pull never`, a project-owned network/volume/container, loopback-only host
binding, synthetic unrecorded credentials, bounded resources and proved cleanup.

The work did not use an external channel, real credential, existing PostgreSQL service, monitored database,
operational migration, remote CI, push, deploy, new dependency or download. R2-A, R3, R4-A, disabled normal Agent
Fleet, `UnavailableAgentCertificateIssuer`, the unavailable normal Server publisher and the pre-existing R0 gate
were preserved. R2-B, R5–R8, R7-A0/O1, command execution, administrative SQL/shell, LLM, recommendation, automation,
mode promotion and lifecycle transition remained outside authority.

The mandatory shutdown preflight found no process, listener or visible window attributable to DB-Notifier before
the work. No monitored database, service, ordinary browser or IDE process was stopped.

## Implemented remediation

### Atomic ownership and database-clock leases

- Outbox and notification stores now accept a bounded claim request with a non-empty worker identity, a batch of at
  most 100 and a lease between one second and five minutes.
- PostgreSQL selects eligible rows with `FOR UPDATE SKIP LOCKED` and claims them in the same transaction through an
  update-returning CTE. A row lock therefore does not block unrelated due work at the head of the queue.
- Lease creation, expiry comparison, attempt time, retry availability and terminal times use PostgreSQL
  `clock_timestamp()`. Worker clocks do not decide ownership.
- Every successful claim increments a durable 64-bit fence. Hand-off and completion require the exact row, owner,
  fence and unexpired lease; a previous owner cannot complete after reclaim.
- Expired pre-hand-off leases remain reclaimable while the attempt budget remains. Exhausted work is retained as
  dead-letter evidence rather than deleted.

### Eligibility and idempotency

- Outbox claim requires unpublished, non-terminal, due work below the attempt limit.
- Notification claim additionally requires the exact R4-A binding/channel pair, enabled binding, enabled canonical
  rule, enabled channel, matching environment and instance, matching event type and exact optional instance scope.
  Ineligible or quarantined rows are not claimed.
- The outbox envelope derives one stable key from its durable message ID. Notification envelopes carry the persisted
  R4-A idempotency key. Both keys are mandatory members of the publisher/adapter contracts and reach the synthetic
  side-effect boundary unchanged.
- R4-B does not claim exactly-once external delivery. Idempotency allows a capable receiver to recognise replay;
  uncertain side effects are still retained as ambiguous and never replayed automatically.

### Fail-closed hand-off, retry and terminal evidence

- Immediately before a potentially side-effecting call, the store durably records `handoff_started_at` under the
  exact live fence.
- A typed retry result schedules exponential retry from the database clock; the fifth durable attempt moves the item
  to `DeadLettered` with a stable non-secret code.
- A result-ID mismatch or exception after the durable hand-off marker becomes `Ambiguous`. Cancellation after that
  boundary leaves the marker and lease intact; expiry reconciliation later makes the item `Ambiguous`.
- An expired hand-off marker can never return to `Pending`. The retained row records the ambiguous time and is not
  selected again.
- Error evidence is a bounded stable code. Publisher exception messages, endpoint details and credentials are not
  persisted or logged by this protocol.

### Normal-composition containment

- `ServerOutboxEnabled=true` or `NotificationDeliveryEnabled=true` is still refused before Kestrel binds.
- Normal composition still registers `UnavailableServerMessagePublisher` and no `INotificationChannelAdapter`.
- The PostgreSQL ownership store is composed only behind those disabled workers; its presence is not activation and
  it cannot produce an external side effect by itself.
- The exact laboratory marker exists only in the integration test and runner. Production source does not read it.

## Schema and migration evidence

One Server migration adds only the ownership, fence, availability, hand-off, terminal-time and safe-error fields
required by R4-B. It adds eligibility indexes and constraints for non-negative fences, complete lease tuples,
mutually exclusive outbox terminal evidence, notification terminal evidence and the new `DeadLettered`/`Ambiguous`
states. Existing rows receive only the non-destructive fence default; no operational data update or backfill exists.

`dotnet ef migrations has-pending-model-changes` reports no drift. Forward PostgreSQL SQL was generated locally and
inspected for every new field and constraint and for the absence of updates to existing delivery rows. Reverse SQL
contains only `delivery.durable_ownership_downgrade_blocked`; it contains no delivery update or column drop. The
migration was applied only to the authorised disposable laboratory database and never to an existing or monitored
database.

## Disposable PostgreSQL laboratory

- Docker Desktop supplied the already-local `postgres:16-alpine` image whose image ID and repository digest both
  equal `sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb`.
- The runner validates that exact identity and invokes `docker run --pull never`; it never invokes pull, build or an
  external package operation.
- Each run creates random project-owned names and the exact label `com.db-notifier.r4b=true`, one dedicated network,
  one volume and one container. PostgreSQL is published only on an ephemeral `127.0.0.1` port. Docker Desktop does
  not publish host ports from an `--internal` network, so the final network is a dedicated bridge; no external client
  or endpoint is contacted by the test.
- The container is limited to one CPU, 512 MiB memory, 256 processes and a bounded health wait. The password is
  random, exists only in a temporary file and process-scoped connection value, is never printed or committed, and is
  cleared before cleanup.
- The runner removes only exact-labelled resources it owns and verifies zero remaining labelled containers,
  networks or volumes. An early pre-test runner iteration marked container ownership only after parsing native
  output; its failed cleanup was detected, the exact-labelled resources were manually verified and removed, and the
  runner was corrected to establish ownership through Docker inspection before validation. The final cleanup audit
  also found that attempt's exact random-prefixed temporary directory, removed its sole password file and then the
  empty directory after path validation. Subsequent failure and the final passing run both completed with zero
  residue, and the closing audit found no process, Docker resource or R4-B temporary directory.

The laboratory passed one consolidated PostgreSQL integration scenario for both queues. It proves concurrent
non-overlapping workers, locked-head bypass, reclaim before hand-off, fence increment, stale-owner rejection,
post-hand-off ambiguity, unchanged idempotency keys, receiver deduplication, bounded attempts, dead-letter and
cancellation-safe retention. This is evidence only for the pinned local engine/topology and ownership capability;
it is not general PostgreSQL support or delivery homologation.

## Automatic evidence

Environment observed locally on 2026-07-21: Windows, .NET SDK `10.0.301`, PowerShell 7 and Docker Desktop engine
`29.6.1`. Only installed or cache-resident dependencies were used.

| Gate | Observed result |
|---|---|
| Shutdown preflight | Passed: zero DB-Notifier-owned process, listener or visible product window before work |
| Release build | Passed for 18 solution projects, 0 warnings and 0 errors |
| Full unit suite | Passed, 371/371 |
| WPF suite | Passed, 3/3 |
| Normal integration suite | Passed, 22/22; the R4-B case is inert without its exact local-test marker |
| PostgreSQL ownership laboratory | Passed, 1/1, for both outbox and notifications using the pinned local image |
| Focused R4-B architecture tests | Passed, 3/3, for normal containment, concurrency primitives and runner isolation |
| Architecture suite excluding the exact R0 contradiction | Passed, 42/42; global suite is 42/43 and non-green only for R0 |
| Coverage | Passed: 81.71% lines, 52.22% branches and 10/10 required components present |
| EF model drift | Passed: no model changes after the new migration |
| PostgreSQL SQL generation/inspection | Passed for non-mutating forward shape and fail-closed reverse SQL |
| .NET format | Passed with no changes required after formatting the generated migration |
| Code documentation | Passed for 311 comment-capable source files |
| Markdown links | Passed for 556 local links in 121 Markdown files |
| Secrets | Passed for the current non-ignored worktree and available Git history without printing matched values |
| Docker cleanup | Passed: zero exact-labelled container, network or volume after the final run |

The known global failure remains
`State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`: it expects fixed
artifact filenames while the accepted R0 workflow uses bounded diagnostic directories and run-attempt-qualified
names. It predates R4-B, is unchanged and remains unauthorised for correction.

## Completion assessment

| R4-B criterion | Observed evidence |
|---|---|
| Atomic claim and no duplicate concurrent ownership | Two workers claim disjoint sets for each queue under real PostgreSQL |
| Database-clock lease, reclaim and monotonic fence | SQL uses `clock_timestamp()`; expired pre-hand-off work is reclaimed with a greater fence |
| No head-of-line blocking | A separately locked oldest due row does not prevent the next eligible row from being claimed |
| Mandatory idempotency propagation | Synthetic publisher/adapter receives the expected stable key on every hand-off |
| Old fence cannot complete | Completion under the previous owner/fence is rejected after reclaim |
| Retry is bounded and durable | Attempt five is retained as `DeadLettered`; no row is deleted |
| Ambiguous side effect is fail-closed | Expired post-hand-off lease becomes terminal `Ambiguous` and is never reclaimed |
| Crash/replay remains receiver-safe | Synthetic receiver records one side effect per idempotency key despite replay simulation |
| Normal external delivery remains unavailable | Startup refusal, unavailable publisher and zero normal adapters remain under architecture tests |
| Laboratory is disposable and local | Pinned local image, `--pull never`, loopback bind, bounded resources and zero labelled residue |

The automatic R4-B scope is complete. Independent human acceptance is still required before `AUD-H07` may be
recorded as accepted/closed in the remediation programme.

## Preserved limitations and next boundary

- No real publisher, channel adapter, credential or endpoint exists; delivery remains disabled and unavailable.
- No operational migration, existing PostgreSQL database, monitored database or production data was touched.
- The laboratory proves only the pinned PostgreSQL 16 Alpine container, local Docker Desktop topology and the tested
  ownership matrix. It does not homologate PostgreSQL generally or authorise deployment.
- The protocol deliberately chooses manual reconciliation over automatic replay after uncertainty. An operational
  administrative protocol for inspecting or resolving ambiguous/dead-letter rows is not part of R4-B.
- R0 remains visibly non-green and unchanged. R2-A/R3/R4-A containment remains intact.
- No lifecycle state or AIOps mode was promoted.

The next permitted action is independent review of this report, the focused commit and the recorded gates. The exact
human decision requested is either `ACEITO O R4-B COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR
CORREÇÃO FORA DO ESCOPO.` or a precise request for adjustments. Acceptance would close only the bounded R4-B review;
it would not activate delivery, apply a migration, authorise a channel, correct R0, release R5, enable AIOps or
transition the lifecycle.
