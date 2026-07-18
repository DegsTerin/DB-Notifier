# STATE-06 Agent Identity and Fleet Integration Report

## Status and authority

- Date: 2026-07-17
- Workspace lifecycle position: `STATE-06 INTEGRATION`
- Authority: Bruno authorised one restricted local increment for test-only enrollment, Agent revocation, heartbeat, a human Agent Fleet catalogue, read-only assignments, versioned contracts, persistence, local sandbox E2E tests and factual README correction
- Temporary runtime authority: local test runtimes only, with mandatory shutdown after validation
- Explicit exclusions: operational databases or credentials, external providers or channels, administrative commands or Start/Stop/Restart, deployment, publication, LLM, executor, promotion to `OBSERVER` and automatic lifecycle transition

This report records the implementation and its evidence. It does not activate an Agent Fleet in production, authorise a new increment or change the lifecycle state.

## Summary for non-specialists

The Server now has a locally testable identity and read-only fleet boundary. In plain language, a test Agent can use a one-time code to request a public client certificate, report that it is alive, read only the monitoring assignments belonging to it and later be revoked by an authorised person. A revoked certificate is refused on the next authenticated request.

The implementation is deliberately unable to issue production certificates. The successful end-to-end scenario creates its own temporary certificate authority, HTTPS server and database entirely inside the local test sandbox. It does not contact a real database provider, use a real identity service, execute a command or enable monitoring.

## Implemented boundary

| Capability | Implemented behaviour | Activation boundary |
|---|---|---|
| Enrollment | `POST /api/v1/agents/enroll`; HTTPS-only, dedicated rate limit, one-time scope-bound token in the authorisation header, P-256 CSR proof of possession and generic refusal responses | Production certificate issuer is deliberately unavailable; only the E2E fixture can issue a certificate |
| Heartbeat | `POST /api/v1/agents/{agentId}/heartbeats`; versioned, mTLS route-bound, canonical payload digest, durable sequence cursor, exact duplicate/conflict/gap outcomes and server-owned `LastSeenAt` | No monitored-instance health, provider call or operational telemetry is inferred |
| Assignments | `GET /api/v1/agents/{agentId}/assignments`; complete deterministic read-only snapshot, ETag/`304`, exact Agent/environment/version binding | No Agent worker consumes or applies the snapshot |
| Human catalogue | `GET /api/v1/agents`; active human identity plus server-side `agents.read` RBAC and safe projected fields only | No UI or external IdP flow was added |
| Revocation | `POST /api/v1/agents/{agentId}:revoke`; active human identity plus `agents.revoke`, monotonic Agent/certificate transition and append-only audit in one serialisable store transaction | An already authenticated in-flight request may linearise before revocation; every later certificate validation observes the revoked state |

All new HTTP contracts use protocol/schema version one, bounded input validation, correlation metadata and typed outcomes. Enrollment is the only anonymous route, and anonymous means only that no prior Agent identity exists: the route still requires the protected-transport marker and production rate policy.

## Identity, provenance and secret handling

- The enrollment token has a public identifier and a 32-byte random secret. Persistence stores only its 16-byte salt and domain-separated SHA-256 proof, never the raw token.
- The token is bound to installation, environment, platform and scope, has explicit issue/expiry instants and is consumed in the same transaction that creates the Agent and certificate metadata.
- The schema requires expiry after issuance but does not impose a maximum token lifetime. A future token-provisioning authority must supply that operational policy.
- The Agent owns the private key. The request carries only a public PKCS#10 CSR; production persistence stores neither private key, certificate body nor raw token.
- Issued certificate evidence is accepted only for ECDSA on the explicit NIST P-256 OIDs, client-authentication use, non-CA basic constraints, digital signature, the exact CSR digest and a current validity window.
- Authentication rechecks canonical thumbprint, active certificate state, active Agent state, validity and, for non-legacy rows, the SHA-256 digest of the presented subject public key.
- Enrollment denials, including unavailable issuance, are audited with sanitised codes that contain no presented token or certificate material.

## Persistence, bounds and backpressure

The central model adds three normalised stores:

- `agent_enrollment_tokens` for salted one-time token proofs and lifecycle state;
- `agent_certificates` for public certificate identity, validity and revocation metadata; and
- `agent_heartbeat_cursors` for replay protection independent of heartbeat-detail retention.

Heartbeat rows add canonical payload digest, previous accepted sequence and explicit gap evidence. A unique `(AgentId, Sequence)` index prevents duplicate per-Agent sequence slots.

Read-only responses are bounded and fail without partial output:

- at most 5,000 assignments and 4 MiB of aggregate UTF-8 endpoint/tag JSON per snapshot;
- at most 1,024 RBAC scope rows admitted per authorisation decision;
- at most 5,000 complete safe catalogue rows; and
- at most 128 certificate rows admitted by one revocation transaction.

The assignment query incrementally accounts JSON bytes before retaining the complete snapshot. It never selects `AdministrativeCredentialReference`. Endpoint and tag JSON are validated as JSON with the expected root kinds, but the current provider-neutral contract still relies on upstream governance to keep `EndpointJson` non-secret; no provider-specific field schema or sensitive-name deny-list was introduced.

## Migration and downgrade protection

The eighth repository migration, and fifth Server PostgreSQL migration, creates the normalised tables, constraints and indexes. Its SQL also:

- stops fail-closed if legacy heartbeat sequences or canonical certificate thumbprints collide;
- backfills existing Agent certificate pointers without converting inconsistent legacy Agents into active certificates;
- backfills previous heartbeat sequence/gap evidence and the highest durable heartbeat cursor; and
- blocks `Down` while an active Agent enrolled by this feature would remain trusted by the legacy certificate pointer after the normalised evidence was removed.

The migration was generated and its PostgreSQL SQL was inspected by model tests, but it was not applied to a PostgreSQL process. The `Up`, failure guards, backfill, serialisable concurrency and `Down` guard therefore remain unexecuted PostgreSQL evidence. The local E2E uses SQLite `EnsureCreated`, not EF migration execution. No report statement should reinterpret generated SQL as an exercised database migration.

## Local end-to-end evidence

One E2E scenario starts a real Kestrel HTTPS listener on an ephemeral loopback port and a named in-memory SQLite database. It generates ephemeral P-256 root, server, CSR and Agent keys, validates the signed CSR, and performs the following flow:

1. seed only the salted/hash form of a test enrollment token;
2. enroll once and reject sequential replay without token or Agent disclosure;
3. prove that the issued public certificate contains the CSR public key;
4. send heartbeat sequence `1`, its exact duplicate, a conflicting replay and a sequence `3` gap;
5. retrieve one safe assignment, verify omission of the administrative credential reference and exercise ETag/`304`;
6. deny catalogue and revocation to an authenticated human without RBAC;
7. return the safe catalogue to the authorised test human;
8. revoke the Agent, repeat revocation idempotently and preserve the first `RevokedAt`;
9. deny the next mTLS heartbeat and assignment request; and
10. inspect durable rows and prove that no command, attempt, health sample, event, outbox message or notification delivery was created.

Windows Schannel requires transport private keys to be available through a key container. The fixture therefore rehydrates encrypted PKCS#12 bytes into temporary, non-persistent user key containers, clears those bytes and disposes the certificates and containers at teardown. No key file or fixed key material is committed.

This is not an E2E test of production composition. Human authentication uses a bounded test-only header, certificate revocation checking is `NoCheck` against the fixture's custom root, the fixture limiter is relaxed, no Agent worker participates and no PostgreSQL migration runs.

## Direct review and remediation before commit

An independent read-only review identified one high rollback risk, the absence of public-key digest enforcement, missing negative RBAC evidence, unbounded response dimensions and two over-broad protocol statements. Before delivery:

- downgrade now refuses to discard normalised identity evidence while a newly enrolled Agent remains active;
- non-legacy client authentication now binds the presented subject public key to its stored SHA-256 evidence;
- the E2E now proves `agents.read` and `agents.revoke` denial for an unprivileged human;
- assignments, catalogue, RBAC scopes and revocation certificate sets have explicit ceilings; and
- documentation now distinguishes explicit token expiry from a maximum lifetime and exact heartbeat replay within detail retention from fail-closed cursor rejection after retention.

The review also noted a general concurrency boundary: a request already authenticated before a concurrent revocation may complete as an operation ordered before that revocation. The next request is denied. Existing administrative command poll/ack code was not expanded or enabled under this increment, and command execution remains prohibited and disabled.

## Verification

Environment: Windows, .NET SDK `10.0.301`, Release configuration, 2026-07-17.

| Check | Observed result |
|---|---|
| Mandatory shutdown preflight | passed: no DB-Notifier runtime or owned listener was present; the user's Visual Studio Code process was preserved |
| Full solution build | passed for 14 .NET 10 projects with zero warnings and zero errors |
| Unit/model/provider/presentation tests | `279/279` passed |
| Architecture tests | `15/15` passed |
| Local Agent Fleet E2E | `1/1` passed over loopback HTTPS/mTLS and in-memory SQLite |
| Coverage gate | passed: `77.36%` lines and `53.78%` branches; floors `70%`/`45%` |
| .NET format/analyser gate | passed with no required changes |
| Code-documentation and Markdown-link gates | passed after factual synchronisation |
| Secret scan | passed for the non-ignored worktree and available Git history |
| Fail-closed runtime smoke | passed: liveness `200`; catalogue, audit, enrollment and command poll protected HTTP routes returned `426`; all Agent workers remained disabled and local Agent persistence was not initialised |
| Git diff checks | passed before commit |

Online NuGet/npm vulnerability audits were not repeated because the authorised increment prohibits external resources and no result was inferred from an earlier audit. No operational PostgreSQL, database provider, OIDC service, PKI, vault, external channel, deployment or publication was contacted.

## Limitations and residual conditions

- Production enrollment always returns issuer unavailable until a separately authorised PKI/provisioning design is implemented.
- There is no token-provisioning API, certificate rotation/overlap, CRL/OCSP integration, external IdP exercise or operational key store.
- PostgreSQL migration/backfill/rollback and serialisable concurrency races were not executed; only SQL generation/inspection and SQLite behaviour were tested.
- Enrollment replay was exercised sequentially, not as two concurrent PostgreSQL transactions.
- Exact heartbeat receipt replay requires the immutable detail row. After detail retention, the cursor still rejects stale sequences but cannot reconstruct the earlier receipt.
- The Agent worker does not enroll, store a client identity, send heartbeat, reconcile/apply assignments or acknowledge configuration.
- The E2E revokes one certificate; the bounded multi-certificate loop is implemented but not exercised with 128 rows or a concurrent rotation.
- Catalogue and assignment ceilings are implemented but their maximum-plus-one refusal cases are not exercised by the current E2E.
- Endpoint/tag JSON has type/aggregate bounds but no provider-specific schema capable of proving that every upstream field is non-secret.
- No external runtime, provider, operational telemetry, command execution, LLM, recommendation, executor or `OBSERVER` mode was introduced.

## Gate classification

- Local automatic Quality Gate: `APPROVED` for this restricted implementation and its recorded evidence.
- Human review of this increment: `PENDING`.
- Lifecycle position: remains `STATE-06 INTEGRATION`.
- `none → OBSERVER`: `PENDING` and explicitly outside this authority.
- New implementation increment, external integration or `STATE-07`: `NOT AUTHORISED`.

## Next activity for a non-specialist

The next activity is one human review of this report, not another implementation and not a lifecycle promotion. Open this file and read the Summary, Local end-to-end evidence, Limitations and Gate classification sections. Check that the description matches what you intended to authorise and that the limitations are acceptable.

Then send one of these clear decisions:

- `Incremento STATE-06 Agent Identity and Fleet: ACEITO com as limitações registradas. Não autorizo novo incremento nem promoção.`
- `Incremento STATE-06 Agent Identity and Fleet: AJUSTES NECESSÁRIOS`, followed by the exact concern.

Acceptance closes only this increment. It does not enable production enrollment, run a provider, authorise a command, promote MOD-12 or move the project to another state.
