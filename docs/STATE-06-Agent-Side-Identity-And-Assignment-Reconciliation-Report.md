# STATE-06 Agent-side Identity and Assignment Reconciliation Report

## Authority and status

- Date: 2026-07-18
- Lifecycle position: `STATE-06 INTEGRATION`, unchanged
- Increment: `Agent-side Test Identity and Read-only Assignment Reconciliation`
- Authority: Bruno authorised test-only enrollment, an E2E-only ephemeral private identity, durable heartbeat, read-only assignment reconciliation, Agent SQLite persistence/migration and local sandbox tests
- Automatic restricted-increment Quality Gate: `APPROVED`
- Human review of this increment: `ACCEPTED WITH RECORDED LIMITATIONS` on 2026-07-18 for commit `beb936b`
- `STATE-06` exit Quality/Human Gate: `NOT EVALUATED`
- Runtime activation, `OBSERVER`, promotion and lifecycle transition: `NOT AUTHORISED`

This report records one local implementation increment. It does not make the Agent Fleet operational, accept the result on Bruno's behalf or authorise another increment.

## Outcome

The Agent-side half of enrollment, heartbeat and assignment reconciliation is implemented as explicit Application ports/coordinators, a bounded HTTPS transport and an Agent SQLite store. The only private-key implementation exists inside the E2E test fixture. The ordinary `DBNotifier.Agent.Worker` has no identity adapter, scheduler or transport registration for this flow; its new flag defaults to `false` and deliberately throws `agent_fleet.sandbox_only` if configuration attempts to enable it.

The implementation performs no provider call, database monitoring, observation collection, command polling/execution, UI update, notification, LLM operation or external action.

## Implemented boundaries

### Test enrollment and identity

- The coordinator accepts a one-time token only as an in-memory method input and sends it only through the enrollment authorisation header.
- The E2E identity adapter creates an ECDSA NIST P-256 key and CSR in the test process, validates the issued public certificate against that key and disposes all private material with the fixture.
- The client independently validates certificate size, UTC expiry, CSR/certificate public-key equality, P-256 OID, digital-signature use, client-auth extended use and non-CA basic constraints.
- Agent SQLite persists only Agent/installation/environment identifiers, an opaque E2E identity reference, public thumbprint, public expiry and fail-closed state. It has no token, private-key or certificate-body column.
- A second or conflicting local registration is refused; there is no automatic replacement, reenrollment or recovery after revocation.

### Durable heartbeat

- SQLite stores the exact pending heartbeat JSON, `messageId`, sequence and next sequence before transport.
- A simulated response loss retains the same envelope. A newly constructed coordinator reloads and replays it; the Server returns its durable duplicate receipt and only then does SQLite advance.
- Queue evidence is derived from bounded local outbox facts and does not infer database-instance health.
- Denial or revocation quarantines the local identity. Transient loss remains retryable only through a later explicit one-shot call; no continuous retry scheduler, backoff loop or worker was added.

### Read-only assignment reconciliation

- The Server and Agent share a SHA-256 version over only transmitted fields. Endpoint and tag JSON are canonicalised so harmless wire whitespace cannot change client verification.
- The Agent validates schema, exact Agent/environment binding, strong ETag, digest, unique instance IDs, cardinality, aggregate JSON bytes, fields and timing limits before persistence.
- A valid complete snapshot atomically replaces local rows and advances the active configuration version in the same SQLite transaction. `304 Not Modified` preserves rows and refreshes success evidence.
- Invalid or altered evidence records a sanitised error and preserves the last-known-valid rows. If that snapshot remains within freshness policy, the identity stays `Active`; it becomes `Stale` only when freshness is actually exceeded.
- Reconciled rows contain no administrative credential reference and are not passed to monitoring, provider or command code.

### Agent SQLite migration

`IntegrateAgentFleetClientState` is the fourth Agent migration. It adds certificate metadata and identity state, assignment tags and one `agent_fleet_state` row per registration. Existing registrations become `Conflict` rather than being silently trusted. `Down` refuses while any registration exists because an older schema would lose the state needed to distinguish active, expired, revoked and conflicting identities.

The test migrated a clean ephemeral database, proved unsafe rollback refusal, explicitly removed the test identity, migrated to the preceding version and migrated forward again. Entity Framework reported no pending model changes.

## E2E evidence

The second integration test uses only loopback HTTPS, named in-memory Server and Agent SQLite stores, a fixed clock, an ephemeral test CA and ephemeral P-256 Agent material. It proves this sequence:

1. seed a one-time test token and enroll through the real API route;
2. confirm no raw token is persisted and no ordinary persistence property can hold private key, token or certificate DER;
3. accept heartbeat sequence one on the Server while simulating loss of its response;
4. reconstruct the coordinator, replay the same durable heartbeat and receive the duplicate result;
5. send the next heartbeat and advance monotonically;
6. apply assignment snapshot v1, then exercise `304` without row replacement;
7. replace the complete Server snapshot, alter one authenticated response body after transport and reject its unchanged digest while preserving v1;
8. reconcile the genuine v2 atomically with two rows;
9. revoke through the local human-authorised sandbox route, classify the next authentication denial as revoked/denied and quarantine further assignment reads;
10. confirm zero administrative command, command attempt, health sample, health observation, event, outbox or notification delivery.

The fixtures disposed both stores, all HTTP clients/certificates/keys and Kestrel. The final independent check observed zero DB-Notifier process and zero project-owned listener.

## Direct review findings and corrections

No unresolved Critical or High defect was identified inside the authorised scope. The following issues were found and corrected before delivery:

| Severity | Finding | Correction and evidence |
|---|---|---|
| Medium | The earlier Server assignment digest included an internal concurrency token that was not transmitted, so an Agent could not independently recompute it. | Replaced it with one shared digest over transmitted fields only; body/ETag alteration is rejected in unit and E2E tests. |
| Medium | Raw JSON whitespace could make the Server and deserialised Agent calculate different digests for equivalent wire content. | Canonicalised endpoint/tag JSON before hashing and added a whitespace/serialisation round-trip test. |
| Medium | Authentication middleware can return `401/403` before endpoint version headers; the first transport ordering classified that revocation response as an invalid protocol response. | Classify non-success HTTP status before requiring success headers; revoked E2E identity now reaches `RevokedOrDenied`. |
| Medium | The first local acknowledgement check matched only heartbeat `messageId` and sequence, not every field of the pending envelope. | Require canonical equality of the complete persisted request and an exact returned sequence before clearing it; the negative test proves an altered envelope remains pending. |
| Medium | The first `Down` guard allowed rollback after changing an identity to `Conflict`, which could erase revocation/conflict semantics while leaving a legacy registration. | `Down` now refuses whenever any registration exists; the migration test proves refusal and safe empty rollback. |
| Medium | The initial generated migration defaults would not safely classify pre-existing registrations. | Existing rows now receive `Conflict`, tags receive valid empty-array JSON, and the complete migration path is executed in SQLite tests. |
| Low | The first assignment HTTP response ceiling could reject a contract-valid maximum projection because it allowed little overhead beyond endpoint/tag bytes. | Kept streaming admission and raised the total bounded wire ceiling to 16 MiB. |
| Low | An invalid response immediately labelled a still-fresh LKG as stale. | Fresh LKG remains active with a recorded failed attempt; `Stale` is now reserved for exceeded freshness. |

An early parallel diagnostic build also encountered a shared output-file lock; the same builds passed sequentially. This was an environmental collision, not a product defect.

## Verification

| Gate | Observed result |
|---|---|
| Release build, complete solution | Passed, 14 projects, zero warnings and zero errors |
| Unit/model/provider/presentation tests | `283/283` passed |
| Architecture tests | `15/15` passed |
| HTTPS/mTLS/SQLite integration tests | `2/2` passed |
| Focused new safety/migration tests | `7/7` passed |
| .NET coverage | `76.25%` lines and `48.50%` branches; floors are `70%` and `45%` |
| Agent migration model drift | No pending Entity Framework model changes |
| .NET format/analyzers | Passed |
| Code documentation | Passed for `223` comment-capable source files |
| Markdown links | Passed for `293` local links in `73` files |
| Secret scan | Passed for the non-ignored worktree and available Git history |
| Diff and Git integrity | `git diff --check` passed; `git fsck --full` exited successfully with only pre-existing unreachable objects listed |
| Fail-closed runtime smoke | Passed: liveness `200`, protected HTTP routes `426`, all Agent workers disabled, no Agent database initialised |
| Runtime cleanup | Passed: zero DB-Notifier process and zero owned listener |

Online NuGet/npm vulnerability audits were not run because this increment expressly prohibits external resources; no earlier online result is inferred.

## Limitations and residual conditions

- The E2E identity reference and private key are process-local test artefacts. There is no operational key store, issuer, token provisioner, trust distribution, certificate rotation or recovery workflow.
- The ordinary Agent Worker cannot run this client. There is no periodic heartbeat/reconciliation scheduler, concurrency lock across processes, retry/backoff/jitter implementation or service lifecycle.
- Restart evidence is a new coordinator over the same live SQLite sandbox, not a separate operating-system process restart.
- Expiry and protocol incompatibility are fail-closed code paths but were not separately driven end to end in this increment.
- SQLite transaction/rollback and one lost-response replay were exercised; concurrent Agent writers, corrupted files, disk-full behaviour and crash-at-instruction boundaries were not.
- PostgreSQL, external identity, corporate TLS, proxy behaviour and real network failures were not exercised.
- The assignment schema treats endpoint/tags as non-secret by contract; it does not prove every future provider schema is correctly classified.
- A last-known-valid snapshot is local configuration evidence only. There is no operational assignment acknowledgement, signed application-layer snapshot, provider activation or monitoring result.
- No real credentials, databases, providers, UI, notification channel, deployment or external system participated.

These are accepted boundaries of the restricted sandbox increment, not evidence of operational readiness.

## Gate classification and human decision

The automatic Quality Gate is `APPROVED` only for this restricted implementation and its recorded local evidence. Bruno accepted commit `beb936b` with the limitations recorded in this report. The Quality/Human Gate for leaving `STATE-06`, promotion to `OBSERVER`, `STATE-07`, release and every operational integration are `NOT EVALUATED` and remain independently gated.

Bruno's exact decision was: `Incremento STATE-06 Agent-side Identity and Assignment Reconciliation, commit beb936b: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo novo incremento, promoção nem transição de estado.`

No further action is pending for this increment. A future proposal, remediation, implementation, promotion or lifecycle decision requires new and explicit authority.
