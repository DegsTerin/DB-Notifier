# Agent/API Protocol v1

Observation batching and the receipt-only command polling/ack subset were implemented in `STATE-04`. The restricted `STATE-06` Agent Fleet increments implement HTTPS enrollment with an unavailable-by-default issuer, normalised certificate revocation, heartbeat and complete read-only assignments. A sandbox-only Agent-side coordinator consumes enrollment, heartbeat and assignment routes through bounded HTTPS transport, but the ordinary Worker cannot activate that client. Event batching, operational assignment activation/acknowledgement, certificate rotation, command execution/result and SignalR remain conceptual targets.

## Transport and trust

- HTTPS/TLS is mandatory; production certificate validation cannot be disabled.
- Enrollment is the only endpoint allowed with a one-time enrollment token. All operational endpoints require the enrolled Agent's mTLS identity.
- Agents initiate outbound connections. The API never requires inbound access to an Agent.
- Payloads are JSON UTF-8 with `Content-Type: application/json`; maximum body, batch, field, and cardinality limits are server policy.
- Compression is optional and negotiated; decompressed-size limits apply before parsing.

## Version negotiation

Operational requests send:

```text
DBN-Protocol-Version: 1
DBN-Agent-Version: <semver>
DBN-Message-Schema: <positive integer>
```

Responses include the selected protocol, supported range, server version, and a correlation ID. Unsupported versions return a typed `426 ProtocolUpgradeRequired` response without accepting state.

Observation ingestion conceptually supports current major `N` and previous major `N-1` during declared rolling-upgrade windows. The implemented Agent Fleet enrollment/heartbeat/assignment slice declares only protocol major `1`: local tests prove `1/1`, while `0` and future `2` fail closed with `426`. No Agent Fleet `N-1` compatibility is claimed until a later contract declares and tests it. Commands are assigned only when Agent, provider, and command schema are explicitly compatible.

## Enrollment

```text
POST /api/v1/agents/enroll
```

Input: one-time token, Agent-generated public key/CSR, installation ID, platform, Agent version and requested scope. Output: Agent ID, signed public client certificate and expiry, or a typed refusal.

The implemented request carries the one-time token only as `Authorization: DBN-Enrollment <token>`; the JSON body contains public CSR and non-secret metadata. Before committing enrollment, the Server parses the admitted CSR again and compares its SubjectPublicKeyInfo to the issued certificate with a constant-time byte comparison, independently of issuer-reported digests. A successful response contains the Agent ID, public client-certificate DER and expiry. API endpoint discovery, trust-material distribution and certificate rotation are not implemented.

Rules:

- Token is single-use, has an explicit expiry, is hashed at rest, scope-bound, and audited. The current schema requires expiry after issuance but does not enforce a maximum lifetime; future provisioning must apply the operational lifetime policy.
- Reusing a token or installation identity is rejected without revealing another Agent.
- Private key never leaves the Agent key store.
- Enrollment does not grant provider credentials or administrative permission.
- The production composition uses `UnavailableAgentCertificateIssuer` and therefore fails closed with `503`; only the local E2E sandbox substitutes a P-256 CA and material generated at test runtime.
- No token-provisioning API exists. The E2E test seeds a salt and SHA-256 proof directly into an ephemeral SQLite database; no raw token, certificate body or private key is stored by ordinary persistence.
- Enrollment is limited to five attempts per loopback/client address per minute in the production API composition, with no queue.

## Heartbeat

```text
POST /api/v1/agents/{agentId}/heartbeats
```

Heartbeat uses a versioned `AgentHeartbeatRequest`, is bound to the mTLS Agent route and is idempotent by exact `messageId`, sequence and canonical payload digest. A durable per-Agent cursor survives heartbeat-detail retention. Exact replay returns the original receipt only while its immutable detail row remains retained; after detail retention, the cursor still rejects a stale sequence with `409` but cannot reconstruct the earlier receipt. Conflicting identifiers also return `409`; a higher sequence with a gap is accepted and marked explicitly. Server receipt time owns `LastSeenAt`, and the response provides accepted time, highest accepted sequence and clock-skew estimate. Heartbeat health does not imply instance health.

The sandbox Agent-side store persists the complete pending heartbeat envelope before transport. A lost response leaves that exact envelope pending, including its `messageId` and sequence. A real second operating-system process reopens the same fixture-owned SQLite file, obtains a newer fencing token after the abandoned lease expires, replays the exact envelope and advances only after the Server returns a compatible duplicate receipt. The two processes create one Server heartbeat row. This is explicit sandbox harness behaviour, not a continuously scheduled Worker or operational heartbeat service.

Every sandbox heartbeat mutation requires an exact unexpired local lease containing Agent, owner, operation kind and monotonic fence. A previous process cannot clear a pending envelope or release a newer owner's lease. SQLite `BUSY`/`LOCKED` is a refusal, never permission to proceed without a fence.

## Observation and event synchronization

```text
POST /api/v1/agents/{agentId}/observations:batch
POST /api/v1/agents/{agentId}/events:batch
```

Input includes an envelope batch, first/last sequence, and local queue checkpoint. Response reports each accepted, duplicate, rejected, or retryable item plus highest contiguous sequence.

Rules:

- Duplicate IDs return the original acceptance identity.
- A partial batch does not force retry of accepted items.
- Sequence gaps are recorded and returned; later items may be accepted if policy permits, but the gap remains visible.
- Validation failures are terminal for that item; transient server failures are retryable with bounded exponential backoff and jitter.
- Agent retains an acknowledged tombstone/checkpoint long enough to survive response loss.

## Read-only assignment reconciliation

```text
GET /api/v1/agents/{agentId}/assignments?afterVersion=<sha256>
```

The implemented endpoint returns one complete, deterministically ordered snapshot for the exact active Agent and its environment. It includes endpoint JSON classified by contract as non-secret, tags and an opaque monitoring-only credential reference. `AdministrativeCredentialReference`, commands and provider execution are excluded. The snapshot is admitted incrementally under ceilings of 5,000 records and 4 MiB of aggregate UTF-8 endpoint/tag JSON, and fails closed rather than truncating. Its strong SHA-256 ETag supports `If-None-Match`, `afterVersion` and `304 Not Modified`. The digest now covers only transmitted fields and canonicalised endpoint/tag JSON, so the Agent can independently recompute it after HTTP serialisation.

Before a snapshot can be serialised, the Server resolves the exact open Provider SDK identifier, applies the provider-owned endpoint validator, rejects unknown endpoint fields, scans endpoint and nested tag keys for secret-like names, and accepts only the exact structured opaque credential-reference schema with `Monitoring` purpose. The normal Server registry is deliberately empty, so ordinary composition cannot distribute a provider assignment. The sandbox Agent-side coordinator repeats the same validation independently before any SQLite delete or insert, in addition to schema, Agent/environment binding, strong ETag, digest, uniqueness, field and aggregate JSON-byte checks. `304` refreshes reconciliation evidence only when its strong ETag exactly matches the local version; weak, divergent, reordered or unsafe evidence is refused. Every refusal preserves the last-known-valid set and records only a sanitised code. Local rows never receive the server-side administrative credential reference. This persistence is not operational activation: no assignment is delivered to a provider or scheduler, no acknowledgement protocol was added, and the ordinary Worker rejects Agent Fleet client activation.

The Server reads assignments inside a serialisable transaction, captures the Agent concurrency token, rechecks active/unrevoked state immediately before commit and returns a retryable consistency refusal if the commit cannot prove a coherent view. A snapshot whose authorisation transaction committed before revocation can finish HTTP delivery afterwards; it is historical configuration evidence, not proof of current authority. A request whose authorisation occurs after the durable revocation is refused, and the Agent quarantines its identity on the proved denial while retaining any LKG only as historical evidence.

## Human Agent Fleet catalogue and revocation

```text
GET  /api/v1/agents
POST /api/v1/agents/{agentId}:revoke
```

The catalogue requires the server-side `agents.read` permission under global or environment scope, admits at most 1,024 authorisation scopes and 5,000 complete safe rows, and omits installation IDs, token proofs, certificate metadata and private material. Revocation requires `agents.revoke`; it first commits the principal Agent identity and append-only audit evidence in a serialisable transaction, making authentication fail closed independently of certificate cardinality. Outstanding certificate metadata is then revoked in independently committed batches of at most 128. The certificate-row lifecycle is the durable resumable cursor, so no schema change is required; interruption returns a typed accepted-but-reconciling outcome and repetition resumes without changing the original `RevokedAt` or reactivating the Agent. The certificate validator reads the normalised certificate table and, for non-legacy rows, rechecks the presented public-key digest on every authenticated request.

There is no external identity provider, operational certificate authority, certificate-rotation endpoint or token-management service in this increment.

## Command retrieval and lifecycle

```text
POST /api/v1/agents/{agentId}/commands:poll
POST /api/v1/agents/{agentId}/commands:ack
POST /api/v1/agents/{agentId}/commands/{commandId}:result
```

1. Server authorizes and persists a command before it becomes available.
2. Agent polls with its durable command-protocol sequence and exact Agent/provider version inventory; the Server filters target, expiry and compatibility before returning a bounded batch.
3. Agent persists an exact-idempotent inbox entry and acknowledges receipt as a bounded batch. Conflicting command/idempotency replay is rejected.
4. A future, separately gated executor may execute one typed attempt under timeout/cancellation rules.
5. Agent performs the required independent post-probe.
6. Agent submits a terminal or `UnknownOutcome` result idempotently.

SignalR may notify `commandsAvailable(agentId, cursor)` only. It never carries executable parameters and never changes command state.

The implemented receipt-only path stops at `Acknowledged`: it creates no `CommandAttempt`, never calls a provider, and never submits a result. Command polling is disabled by default independently of observation synchronization.

## Cancellation and expiry

- Pending/acknowledged commands may be cancelled before execution according to policy.
- Agent checks expiry immediately before side effects.
- A running operation that cannot safely cancel completes/returns its real state; the API never labels it cancelled solely because a request arrived.
- Retry never creates a second effect when a terminal result or idempotency record exists.

## Retry policy

- Retry only failures classified retryable. A transient HTTP status carrying `retryable=false` is terminal for the unchanged request.
- The sandbox coordinator uses injected time, delay and jitter, bounded exponential backoff, a bounded delta `Retry-After` ceiling, maximum attempts, maximum cumulative delay and an enclosing lease expiry.
- Cancellation interrupts transport or backoff and the exact lease is released; an abandoned process lease is recoverable only after expiry under a higher fence.
- Authentication, authorisation, revoked/expired identity, incompatible version, invalid schema, conflict, invalid stored configuration and unsupported capability do not retry.
- Backpressure reduces batch size/frequency and exposes queue depth/age.

These retry and fencing rules are composed only by the temporary test harness. The ordinary Worker has no Agent Fleet scheduler, identity adapter or transport registration and rejects attempted activation with `agent_fleet.sandbox_only`.

## Error response

```text
ProblemDetails
  type: stable documentation URI/code
  title: safe summary
  status: HTTP status
  code: canonical machine code
  correlationId
  retryable
  retryAfter optional
  errors: bounded field errors optional
```

Responses never include secrets, stack traces, native command lines, or another tenant's identifiers.

For the implemented Agent Fleet slice, the `retryable` field is authoritative when present. `Retry-After` is read only as a non-negative delta no greater than five minutes and only for a response that remains classified transient. Every inventoried C# JSON response consumer uses the shared streaming boundary with an exact actual-byte ceiling, admitted JSON media type/UTF-8 character set, contract-specific maximum depth and unknown-member refusal; the Dashboard TV reader applies the equivalent browser streaming boundary with fatal UTF-8 decoding and exact schema validation. `Content-Length` is only an early refusal and never replaces the actual streamed count. Missing/duplicated/future negotiation headers, partial JSON, unknown body fields and over-limit response bodies are invalid responses rather than compatibility or revocation evidence.

## Required compatibility scenarios

- Agent offline across multiple policy/config versions.
- Duplicate batch after response loss.
- Out-of-order/gapped sequence.
- Revocation while Agent is connected.
- API on `N`, Agent on `N-1`, and command requiring `N`.
- Command expires before retrieval, after acknowledgement, and during a non-cancellable adapter operation.
- SignalR loss/reconnect with command polling recovery.
- Local outbox reaches age/size limit.
- Clock skew exceeds policy.
