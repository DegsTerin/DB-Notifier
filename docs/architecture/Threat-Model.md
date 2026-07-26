# DB-Notifier Threat Model

## Scope and assets

This model covers Dashboard, API, Agent, providers, monitored endpoints, OS/service adapters, local/central storage, vaults, update channel, and future AIOps inputs. It is an architecture artifact, not a penetration-test result.

Critical assets:

- Monitoring and administrative credentials.
- Agent private keys and enrollment material.
- Inventory, topology, health history, audit, and user identity.
- Administrative command authorization and results.
- Package signing identity and update manifests.
- Windows app-notification identity, activation registration and stable installed path.
- AIOps knowledge sources, datasets, prompts, evaluations, and approvals.
- Future MOD-12 root anchors, signed policy-authorisation assertions, root-signed role delegations, role-specific public keys, signed trust bundles, trust-domain epoch heads, revocation heads, durable checkpoints and independent audit heads.
- Future MOD-12 resource ceilings/reservations and governed corpus manifests; producer declarations are never trusted ceilings.

## Trust boundaries

1. Browser/Desktop user ↔ API.
2. API ↔ Agent over outbound HTTPS/mTLS.
3. Agent ↔ local vault.
4. Agent ↔ provider driver/native utility.
5. Provider ↔ monitored database.
6. Agent ↔ operating-system/service manager.
7. API ↔ PostgreSQL/notification/secret manager.
8. Release pipeline ↔ signing service/update channel.
9. Sanitized telemetry/knowledge ↔ future AIOps/LLM boundary.
10. Windows Shell app-notification activation ↔ local WPF Desktop dispatcher.
11. Root/policy governance ↔ future role-specific signing and key-custody boundaries.
12. Future signers ↔ untrusted trust-bundle publisher/transport ↔ host trust coordinator.
13. Future host trust coordinator ↔ durable checkpoint owner ↔ independent auditor/reconciler.
14. Bounded telemetry/corpus source ↔ future resource admission coordinator ↔ pure MOD-12 verifier/evaluator.

## Threats and required controls

| Boundary/threat | Example | Required controls | Verification |
|---|---|---|---|
| Endpoint SSRF | User config points Agent/server to metadata/internal service | provider endpoint schema, immutable positive CIDR/port policy, deny precedence, atomic DNS-answer validation, IP-pinned connection and no API-side probe to monitored databases | deterministic CIDR, mixed-answer, metadata and DNS-rebinding regressions; see [Network egress policy](Network-Egress-Policy.md) |
| HTTP redirect or OIDC-origin escape | A configured endpoint or discovery document redirects identity/payload traffic to another origin | no automatic redirect/proxy/ambient credentials, exact OIDC origin, bounded JSON response and fresh policy admission for every physical socket | redirect, cross-origin, response-size/media-type and pinned-connector regressions |
| Certificate-validation egress | TLS chain construction retrieves attacker-selected AIA, CRL or OCSP locations outside policy | offline cached revocation, certificate downloads disabled, exact server/client authentication EKU and no `NoCheck` fallback | TLS-option and certificate-profile regressions; operational trust/revocation availability remains unproved |
| Native command injection | Host/service/path/extra args become shell text | typed process arguments, no shell, allowlisted executable discovery, provider-owned validation | metacharacter/path tests, static review |
| SQL injection/unsafe probe | Free-form SQL enters provider | fixed/typed provider operations, parameters, no LLM/user SQL executor | provider contract tests |
| Agent impersonation | Stolen token/certificate publishes false health | one-time enrollment, mTLS, non-exportable key, revocation, scope binding, anomaly/audit | cloned Agent/revocation tests |
| Agent Fleet duplicate local owner | Two Agent processes advance one heartbeat or LKG concurrently | fixture-local expiring lease, monotonic fencing token, exact fence validation on every commit, old-owner release refusal | competing-owner, expired-owner and real multiprocess restart tests |
| Agent Fleet retry amplification | Transient or malformed response causes an unbounded loop | typed retry authority, bounded attempts/elapsed time/delay/jitter, cancellation, `Retry-After` ceiling and zero retry for terminal classifications | deterministic retry/budget/cancellation and Problem Details tests |
| Agent SQLite corruption or partial schema | Harness repairs/recreates evidence and resumes optimistically | read/write-existing mode, bounded integrity check, exact migration history and fail-closed preservation | corrupt, future, incomplete, migration rollback and lock tests |
| Assignment/revocation race | A stale response is presented as current authority after revocation | serialisable snapshot, Agent concurrency-token recheck, retryable consistency refusal, denial on the next post-revocation authorisation and LKG-as-history semantics | coordinated post-commit delivery/revocation E2E and quarantine test |
| Observation ingestion/revocation race | An Agent is read as active, principal revocation commits, and health effects attributable to that same observation commit afterwards | one per-Agent process gate, exact principal-row `FOR NO KEY UPDATE` during PostgreSQL ingestion, `State` plus `RevokedAt` validation, fenced serialisation-failure reclassification and R-SEQ rejection consumption | deterministic local tests plus the passed [physical PostgreSQL campaign](../STATE-06-R-EGRESS-R-FENCE-PostgreSql-Multiprocess-Load-Report.md): eight independent-process scenarios, seven exact `pg_blocking_pids()` observations, `86` per-item SQLSTATE `40001` occurrences, `68/68` bounded retryable convergences, crash recovery, cancellation, Agent isolation, zero deadlock, zero unclassified failure and zero owned residue |
| Agent identity-fence starvation or contention | Queued ingestion delays principal revocation, or a revocation later denied by RBAC blocks ingestion while deciding and auditing | safety ordering and cancellable waits are retained, but the current gate has no priority, fairness or independent wait bound and is acquired before final RBAC | the passed [physical PostgreSQL campaign](../STATE-06-R-EGRESS-R-FENCE-PostgreSql-Multiprocess-Load-Report.md) characterises fixed `4 × 25` same-Agent and distinct-Agent loads only; its observed latencies, throughput and revocation position establish no SLO, fairness or priority, and pre-RBAC redesign evidence remains required before claiming bounded revocation latency |
| Dashboard TV stale or untrusted snapshot | A late, malformed, unauthorised or unchanged response is presented as fresh operational truth | exact sandbox activation, server-side human policy, bounded versioned schema, strong ETag, serial reconciliation, session fencing, cancellation and LKG-with-original-timestamps semantics | deterministic coordinator/validation tests and temporary HTTPS loopback `200`/`304`/`401` E2E |
| Replay/duplicate command | Captured command causes repeated restart | durable command ID/idempotency, expiry, Agent binding, terminal result reuse | duplicate/reorder E2E |
| Privilege escalation | Monitoring credential executes admin action | separate references/sessions, server RBAC, provider capability and local policy | negative credential/RBAC tests |
| Secret exfiltration | Exception/log/API returns connection string | opaque references, centralized redaction, structured logs, bounded native details | secret canary/log scan |
| UI authorization bypass | Hidden button called directly | server-side policy and target scope on every mutation | direct API negative tests |
| Windows notification activation abuse | Forged, stale or unexpected Shell activation arguments attempt to invoke a provider or administrative action | allow only the exact local `action=show` intent; ignore every unknown argument; marshal to the WPF dispatcher; reveal only the local secondary shell; never translate activation data into a command, provider target or permission | unknown/missing argument tests, dispatcher-shutdown race test, direct review that activation reaches no command/application executor |
| Stale/false healthy | Offline Agent's last sample remains green | observed/received/stale semantics, AgentOffline independent state | clock/offline tests |
| Queue exhaustion | API outage fills disk | bounded encrypted/local queue, backpressure, overflow event, reserved disk floor | outage/load tests |
| DoS/cardinality | Agent sends huge batches/tags | auth before cost, body/batch/tag limits, rate limit, quotas | fuzz/load tests |
| Cross-tenant access | ID enumeration leaks another scope | scoped queries, non-sequential public IDs, authorization filters, audit | IDOR tests |
| Audit tampering | Operator deletes evidence | append-only permissions, integrity/retention controls, separation of duty | privileged negative tests |
| Supply-chain compromise | Malicious package/update | lockfiles, dependency audit, SBOM, signed artifacts/manifests, hash verification, staged rollout | signature/tamper tests |
| Downgrade | Attacker installs vulnerable Agent | minimum version policy, signed manifest, schema/protocol compatibility | downgrade test |
| Prompt injection | Retrieved document asks LLM to execute/exfiltrate | treat content as data, sanitization, provenance, structured output, no direct executor | adversarial eval/red team |
| Data poisoning | Fake history biases recommendations | source identity/quality, immutable provenance, approval, offline governed training | poisoned-fixture eval |

## MOD-12 pre-runtime trust and resource threats

The threats below apply to any future implementation of trust distribution or bounded ingestion. The current MOD-12 verifier has no runtime, issuer, publisher, durable checkpoint or resource coordinator, so this section is architectural risk analysis rather than an operational-test result. The complete field/state contract and one-to-one threat → control → owner → vector mapping are in [AIOps Trust Governance and Resource Envelope](AIOps-Trust-Governance-And-Resource-Envelope.md#threat-control-and-future-test-traceability).

| IDs | Threat group | Required control | Future verification | Accountable/enforcement roles |
|---|---|---|---|---|
| `M12-T01`, `M12-T06` | Valid signer issues unauthorised scope, or a valid package is replayed across tenant/environment/purpose | independently authenticated dual-control policy decision, separate assertion attestation, root-delegated role, canonical-set subset predicate and stable cross-scope domain key | fabricated/expired decision/assertion, alias/wildcard/superset and cross-scope replay vectors | policy authority / host trust coordinator |
| `M12-T02`–`M12-T05` | Trust, root, revocation or corpus rollback, freeze, fast-forward or split view | bounded freshness, direct/complete contiguous chains, constant-size recovery-epoch/root/revocation/corpus heads, digest comparison, independent reconciliation and no automatic branch choice | older/equal-divergent/gapped/catch-up generations for every head and two-consumer fork | host trust coordinator / independent auditor |
| `M12-T07`–`M12-T09` | Operational signer, approval quorum, revocation/root compromise, old-epoch artefact reuse, key-role confusion or downgrade | materially distinct role keys, independent dual-control decisions, non-circular epoch-context binding, old-epoch ineligibility/fresh issuance, algorithm allowlist, cumulative revocation, quarantine and one-use recovery/root approvals | wrong-role/material/delegation/algorithm/context, tombstone resurrection/repackaging, root rotation and compromise recovery | security architecture / root governance |
| `M12-T10` | Clock manipulation hides freeze or causes unsafe validity | trusted clock policy, bounded skew and fail-closed freshness | virtual future/past/regressed clock | host platform owner |
| `M12-T11` | TOCTOU, crash or partial local update exposes mixed trust state | immutable digests, revalidation and one local compare-and-swap/transaction over every complete active head | mutation and crash before/during/after trust, revocation, root and corpus commits | host trust coordinator |
| `M12-T12` | Missing, corrupt, cloned, silently restored or superseded checkpoint loses monotonicity or impersonates first install | independent first-install marker, constant-size epoch high-water, quarantine and one-use authenticated bootstrap/recovery | full-store restore/clone, missing marker, replayed approval, old epoch and overflow | checkpoint owner / independent auditor |
| `M12-T13` | Audit/retention state is deleted, reordered, restored or compacted unsafely | protected authenticated accumulator/head, bounded retention and comparison outside the audited operator | audit tamper/restore/cap/compaction vectors | independent auditor |
| `M12-T14`, `M12-T15` | Lying/silent/infinite/non-quiescent source or bundle/parser/decompression bomb | pre-reservation deadline, cooperative bounded reader, liveness/phase deadlines, exact/maximum declarations, independent byte/ratio/structure caps and termination fencing | false/omitted counts, no first byte, infinite/non-cooperative source, expansion and nesting limits | host resource coordinator |
| `M12-T16`, `M12-T17` | Accounted-memory exhaustion, unsafe lease reuse, invalid field composition, work undercharge or arithmetic overflow | deterministic accounted-memory reservation, quiescence before reuse, class-specific composition, versioned work model and checked arithmetic; observed memory remains empirical | accounted-memory boundary, observed-memory campaign, composition matrix, maximum sorting/crypto and overflow | host resource coordinator / MOD-12 owner |
| `M12-T18`, `M12-T19` | Cancellation/deadline ignored, duplicate coordinator, concurrent fragmentation or false fairness claim | cooperative chunks, total/phase/liveness deadlines, termination fence, one uniquely fenced coordinator, atomic hierarchical buckets and explicit no-fairness boundary | cancellation/quiescence, duplicate ownership, restart, scope fragmentation, serial contention and window tests | MOD-12 owner / host resource coordinator |
| `M12-T20` | A favourable partial, stale-context or truncated result is mistaken for a passing evaluation | complete-only pass, exact context revalidation, subset aggregates absent and truthful output admitted as a whole | timeout/cancel/context-update after favourable subset and oversized output | evaluation owner |
| `M12-T21` | Whole-corpus replacement, poisoning, rollback, withdrawal replay, duplication, leakage or segment bias | independently approved role-signed manifest, monotonic corpus head, exact membership, irreversible withdrawal, expiry and quantitative distribution checks | wrong signer, full replacement, rollback/split/gap, label flip, duplicate, cross-scope/expiry and omitted segment | dataset owner/steward |
| `M12-T22` | Refusal/audit diagnostics disclose sensitive telemetry or topology | stable sanitised codes, bounded diagnostics and canary scan | sensitive-content refusal fixture | security and privacy owner |
| `M12-T23` | Evaluation saturation delays revocation or an old-context result publishes after a narrowing update | bounded host-reserved control-plane capacity, independent commit, context cancellation and publication-time exact-revision revalidation | saturated data plane plus revocation/context update and late completion | host trust coordinator / evaluation owner |
| `M12-T24` | Trust/control metadata grows without bound, compaction resurrects authority or two coordinators claim one accounting domain | constant-size active heads, bounded authenticated accumulators/retention, crash-safe compaction and unique fenced ownership | metadata cap/compaction crash, cloned/stale coordinator and restart continuity | host resource coordinator / checkpoint owner |

Digest chaining detects divergence only when views are compared; it does not prevent split view. Likewise, a local checkpoint and local audit restored together cannot prove a silent rollback, and total absence cannot be distinguished from first installation without an independent marker. Any future claim stronger than local continuity requires an independent witness, monotonic platform anchor or authenticated reconciliation mechanism that has itself been implemented and tested.

## Administrative command abuse cases

- A Viewer crafts a restart request: server returns denied and creates a security audit entry without a command.
- An authorized Operator targets an out-of-scope instance: denied server-side.
- A valid command reaches a revoked Agent: API refuses delivery and Agent certificate fails authentication.
- A command expires after acknowledgement: Agent reports expired without invoking adapter.
- Adapter times out after possible side effect: result is `UnknownOutcome`; post-probe and operator review determine recovery.
- LLM recommends restart: recommendation has no command authority; deterministic risk/RBAC/approval workflow is still required.
- A user activates a Windows notification: the only permitted outcome is showing the local secondary WPF shell. Activation is navigation, not authentication, authorization, provider evidence or an administrative command, and it cannot start, stop or restart a database or service.

## Privacy and data classification

| Class | Examples | Rule |
|---|---|---|
| Secret | passwords, tokens, private keys, full connection strings | never ordinary storage/log/protocol/AI |
| Restricted | host topology, usernames, audit actors, query samples | encrypt, scope, minimize, retain by policy |
| Internal | health samples, versions, tags, incidents | authorized access and retention |
| Public | published docs/release metadata | integrity/provenance still required |

Free-form query text and database data are excluded from the baseline. Any future collection requires a new data-classification decision and redaction/evaluation evidence.

## Residual risks requiring later evidence

- Choice and enterprise integration of vault/signing services.
- Real driver/native utility behavior on supported engine/platform versions.
- Certificate lifecycle under offline/expired conditions.
- WPF/Desktop local privilege boundary and service IPC design.
- Windows App Runtime availability, stable unpackaged registration and Shell policy across install, repair, rollback and uninstall; local API acceptance does not prove visible notification delivery or production support.
- Central multi-tenancy model if the product becomes shared SaaS.
- Package/update rollback across schema changes.
- Root provisioning, key custody, policy-authorisation workflow, trust-bundle publication and compromise-recovery ceremony for MOD-12.
- The passed physical R-EGRESS/R-FENCE campaign is evidence only for its pinned disposable PostgreSQL image, local
  machine, bounded fixture and one execution. Long-duration contention, starvation freedom, priority, a product wait
  bound, operational PostgreSQL support and any SLO remain unproved.
- Silent full-store restore and fleet-wide split-view detection until an independent continuity/witness mechanism is selected, implemented and tested.
- Safe numerical ceilings for MOD-12 encoded/expanded bytes, deterministic accounted memory, work, deadlines, cancellation latency, control metadata and coordinator concurrency until reproducible measurement exists; observed memory is calibration evidence only, and the serial/no-queue proof makes no fairness claim.
- A concrete terminable isolation mechanism for non-pre-emptible work and its failure/recovery procedure.
- Authenticated corpus approval/root delegation and durable corpus-head technology; signatures cannot prove label correctness or eliminate bias.

## Security acceptance scenarios

- Walk through standalone, on-premises, and hybrid topology.
- Exercise stolen enrollment token, cloned Agent, revoked/expired certificate, and vault outage.
- Test SSRF/DNS rebinding and process-argument metacharacters.
- Prove monitoring credentials and Viewer role cannot issue administrative operations.
- Replay/duplicate/expire/cancel a command.
- Scan logs/events/audit for secret canaries.
- Tamper with update artifact/manifest and verify rejection.
- Run AIOps prompt-injection/data-poisoning fixtures before any mode beyond `OBSERVER`.
- Walk through dual-control policy/revocation/corpus decisions, distinct attestation/root delegations, canonical scope subset, role-specific material, root/operational rotation, one-use bootstrap/recovery and an untrusted publisher without treating transport authentication as authority.
- Exercise older, equal-divergent, direct-successor, complete catch-up, gapped and superseded trust, root, revocation and corpus heads plus stale candidates, missing first-install marker, restored checkpoint and two-consumer split view; no unsafe branch may become active.
- Saturate evaluation capacity while committing a valid narrowing update, then prove that old-context work is cancelled and cannot publish or authorise.
- Exercise encoded/expanded/parser/accounted-memory/work/declaration/composition/total-and-phase-deadline/liveness/cancellation/quiescence/fencing/control-metadata/coordinator/output boundaries; measure observed memory separately and prove subset aggregates are absent from every incomplete result before any MOD-12 runtime proposal.
