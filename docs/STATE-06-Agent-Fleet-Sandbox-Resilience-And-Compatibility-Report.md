# STATE-06 Agent Fleet Sandbox Resilience and Protocol Compatibility Report

## Authority and status

- Date: 2026-07-18
- Lifecycle position: `STATE-06 INTEGRATION`, unchanged
- Increment: `Agent Fleet Sandbox Resilience and Protocol Compatibility`
- Authority: Bruno authorised only temporary local harness runtimes, real process restart, deterministic retry/backoff/cancellation, protocol failures, local concurrency/fencing, SQLite fault injection and the assignment/revocation race
- Automatic restricted-increment Quality Gate: `APPROVED`
- Human review of this increment: `PENDING`
- `STATE-06` exit Quality/Human Gate: `NOT EVALUATED`
- Operational runtime, external resources, promotion and lifecycle transition: `NOT AUTHORISED`

This report records one local implementation and its automatic evidence. It does not accept the result on Bruno's behalf, enable the ordinary Worker, approve `OBSERVER` or authorise another increment.

## Plain-language outcome

The test-only Agent Fleet model was subjected to controlled failures: one process was killed after the Server accepted a heartbeat, two local owners competed, retries were cancelled or exhausted, protocol responses were missing or contradictory, SQLite was locked or failed around transactions, and revocation was coordinated with an assignment response.

The observed result is fail-closed. A newer process can recover an abandoned heartbeat under a higher fencing token without creating a second Server effect. Old owners cannot confirm work. Terminal protocol and identity states do not retry. Pre-commit storage failure rolls back, while post-commit failure leaves explicit durable evidence that can be reopened. This remains a local test model; it is not an operational Agent service.

## Implemented boundaries

### Temporary multiprocess harness

- `DBNotifier.AgentFleet.SandboxHost` starts only with the exact `--sandbox-agent-fleet` marker.
- It accepts only an existing SQLite file and optional marker under a fixture-owned temporary directory whose name has the sandbox prefix.
- It accepts only an HTTPS loopback endpoint and bounded opaque references.
- Private P-256 test identity material and the pinned Server thumbprint cross a current-user-only named pipe in bounded frames. They do not enter process arguments, environment additions, ordinary files, SQLite or logs.
- The harness validates SQLite integrity and the exact known migration history before use. It never creates, migrates or repairs the supplied file.
- The integration fixture owns exact child PIDs, terminates the first child deliberately, waits for the second, clears SQLite pools and removes the temporary directory.

The harness has no reference from or to the ordinary Agent Worker composition. It is not a service, daemon, deployable runtime, provider or production identity adapter.

### Deterministic retry and cancellation

- A sandbox-only resilience coordinator obtains one expiring local lease for heartbeat or assignment reconciliation.
- Retry is restricted to typed transient results and bounded by attempts, cumulative scheduled delay, exponential delay ceiling, symmetric injected jitter and lease expiry.
- Only a bounded delta `Retry-After` no greater than five minutes can reduce the scheduled delay. The HTTP Problem Details `retryable=false` classification terminates a nominally transient status.
- Cancellation interrupts backoff and releases the exact fence. Exhausting attempts or elapsed budget returns the terminal code `agent_fleet.retry_budget_exhausted`.
- Revoked, denied, expired, incompatible, conflicting, malformed and non-retryable responses make no second transport attempt.

No scheduler or continuous loop was registered. The ordinary Worker still throws `agent_fleet.sandbox_only` if the disabled client flag is set to true.

### SQLite lease, fencing and fault behaviour

- `agent_fleet_state` now carries `next_operation_fence` plus an all-null or all-populated owner/kind/fence/expiry tuple.
- Acquisition is one parameterised atomic SQLite update. `BUSY`/`LOCKED`, an unexpired owner or fence exhaustion refuses acquisition.
- Every heartbeat and assignment mutation checks Agent, owner, kind, fence, exact expiry and controlled UTC time inside its serialisable transaction.
- Release clears only the exact current owner/fence. A previous or expired owner cannot clear a newer lease or confirm a heartbeat.
- Deterministic fault points cover lease acquisition, heartbeat pending/acknowledgement and complete assignment replacement before and after commit. Tests distinguish rollback from a durable commit whose caller did not receive confirmation.
- The migration refuses `Down` while a lease tuple remains. After exact release it migrates to the preceding schema and forward again.
- Corrupt, future and incomplete stores are refused and preserved. Simulated disk-full uses an injected exception and never consumes host disk space.

### Protocol compatibility and revocation consistency

- Agent Fleet protocol major `1` with major `1` is the only declared compatibility path. Protocol `0`, future `2`, missing schema and body/header schema divergence return `426`; no `N-1` support is inferred.
- Duplicate, gapped, stale/reordered and conflicting heartbeat identifiers/sequences have separate outcomes. Exact duplicate replay creates no second Server heartbeat.
- Missing/duplicated/future response headers, partial JSON, unknown body fields and maximum-plus-one response bodies are invalid.
- Weak ETags, query/header divergence, digest/body divergence and `304` with a different local version do not replace the LKG.
- Expiry under controlled time persists `Expired` before transport and remains fail-closed after coordinator reconstruction.
- Server assignment reads use a serialisable transaction and recheck the Agent concurrency token immediately before commit. A commit consistency failure is the only assignment-unavailable case marked retryable.
- The coordinated race proves that a snapshot authorised and committed before revocation can finish delivery afterwards only as historical configuration evidence. The next authorisation after durable revocation is denied. Existing Agent-side E2E evidence proves that denial quarantines the local identity while retaining LKG only as history.

## E2E evidence

The real multiprocess scenario uses a file-backed Agent SQLite database, named in-memory Server SQLite, loopback HTTPS/mTLS and an ephemeral P-256 CA/Agent identity:

1. the parent E2E fixture performs test enrollment and persists only allowed metadata;
2. process A acquires fence `1`, persists heartbeat sequence `1` and sends it to the real Server;
3. after Server acceptance, a non-secret marker pauses process A before local acknowledgement;
4. the parent kills that exact child process and verifies the pending envelope remains;
5. process B starts after the controlled lease expiry, acquires fence `2` and replays the exact message/sequence;
6. the Server returns its duplicate receipt, Agent SQLite advances once to sequence `2`, and the next fence becomes `3`;
7. the Server contains one heartbeat row, and no command, attempt, health sample, event, outbox item or notification is created;
8. both child processes, Kestrel listener, certificate material, database and fixture directory are disposed.

Separate local E2E tests prove protocol-negative inputs, controlled certificate expiry and the coordinated post-commit assignment/revocation timing described above.

## Direct review findings and corrections

No unresolved Critical or High defect was found in the authorised scope. The following issues were found and corrected before delivery:

| Severity | Finding | Correction and evidence |
|---|---|---|
| Medium | The first child launch used the copy of the harness assembly in the integration-test output, where standalone runtime dependencies were incomplete. | The integration project retains only a build-order reference and resolves the harness from its owning output. The real multiprocess test then passed. |
| Medium | Maximum-attempt exhaustion initially returned the last transient result with `Retryable=true`, allowing an outer caller to restart the loop. | Exhaustion now returns `agent_fleet.retry_budget_exhausted` with `Retryable=false`; a focused test proves it. |
| Medium | A generic assignment `503` was initially mapped as transient even when the Server Problem Details classified invalid stored configuration or limits as non-retryable. | HTTP transport now honours explicit `retryable=false`; only the consistency-conflict path emits `retryable=true`. |
| Medium | Existing migration-count tests still assumed four Agent migrations. | Expectations and rollback semantics were updated for the fifth migration, including the safe intermediate rollback to the preceding Agent Fleet schema. |
| Low | A failed file-backed E2E cleanup could retain a pooled SQLite handle. | The exact fixture clears SQLite pools before deleting its owned directory; no residue remained after rerun. |

## Verification

| Gate | Observed result |
|---|---|
| Shutdown preflight | Passed before implementation and later technical actions; no DB-Notifier process/listener was present |
| Release build, complete solution | Passed, 15 projects, zero warnings and zero errors |
| Unit/model/provider/presentation tests | `300/300` passed |
| Architecture tests | `16/16` passed |
| HTTPS/mTLS/SQLite integration tests | `5/5` passed |
| Focused sandbox resilience tests | `14/14` passed |
| .NET coverage | `78.54%` lines and `52.73%` branches; floors are `70%` and `45%` |
| Agent migration model drift | No pending Entity Framework model changes |
| .NET format/analyzers | Passed after the formatter normalised the generated migration and import order |
| Code documentation | Passed for `229` comment-capable source files |
| Markdown links | Passed for `312` local links in `75` files |
| Secret scan | Passed for the non-ignored worktree and available Git history |
| Diff and Git integrity | `git diff --check` and `git fsck --full` passed; only pre-existing unreachable objects may be reported by Git |
| Fail-closed runtime smoke | Passed: liveness `200`, all protected routes `426`, all Agent workers disabled and no local persistence initialised |
| Runtime cleanup | Passed: zero DB-Notifier process, zero owned listener and zero Agent Fleet sandbox temporary directory remained |

Online NuGet/npm vulnerability audits remain `NOT AUTHORISED` because this increment prohibits external resources. No earlier online result is inferred.

## Limitations and residual conditions

- The private identity package is test-only. Its encrypted bytes are zeroed, but the temporary managed password string cannot be deterministically zeroed before garbage collection. It is never persisted or logged.
- Named-pipe current-user isolation, Windows certificate handling and loopback mTLS do not prove an operational key store, PKI, token service, trust distribution, rotation, renewal or recovery design.
- The harness executes one bounded heartbeat per child. It does not prove a long-running scheduler, service lifecycle, fleet load, fairness, sustained contention or multi-machine coordination.
- SQLite fault injection proves named transaction boundaries, lock refusal, migration and corruption behaviour; it is not filesystem, power-loss, storage-controller or operating-system crash certification.
- `PRAGMA quick_check(1)` and exact migration history are local integrity/admission checks, not cryptographic rollback detection or backup authenticity.
- The assignment/revocation race documents the unavoidable historical-response window after a pre-revocation authorisation commit. No push revocation channel exists; the Agent learns revocation on its next authenticated call.
- Only Agent Fleet protocol major `1` is implemented. `N-1` is deliberately unsupported rather than tested as compatible.
- No PostgreSQL, IdP, vault, proxy, corporate network, external provider, monitoring, command, UI, notification, deploy, publication, LLM or executor participated.

These limitations prevent any claim of operational readiness, `OBSERVER`, `STATE-07`, production or release.

## Gate classification and decision requested

The automatic Quality Gate is `APPROVED` only for this restricted local increment and the observed evidence above. The Human Gate for this increment is `PENDING`. The exit gate for `STATE-06`, promotion to `OBSERVER`, operational activation and lifecycle transition are `NOT EVALUATED` and remain separately prohibited.

Bruno should review this report, especially the E2E sequence, direct-review findings and residual limitations. He may accept this restricted increment, accept it with the listed limitations, request a scoped remediation or reject it. Any decision applies only to this increment and cannot authorise a new increment, promotion or state transition implicitly.
