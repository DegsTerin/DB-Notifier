# Agent/API Protocol v1 — Conceptual Specification

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

Observation ingestion supports current major `N` and previous major `N-1` during declared rolling-upgrade windows. Commands are assigned only when Agent, provider, and command schema are explicitly compatible.

## Enrollment

```text
POST /api/v1/agents/enroll
```

Input: one-time token, Agent-generated public key/CSR, installation ID, platform, and requested scope. Output: Agent ID, signed client certificate or approved enrollment result, API endpoints, server trust material reference, and compatibility policy.

Rules:

- Token is single-use, short-lived, hashed at rest, scope-bound, and audited.
- Reusing a token or installation identity is rejected without revealing another Agent.
- Private key never leaves the Agent key store.
- Enrollment does not grant provider credentials or administrative permission.

## Heartbeat

```text
POST /api/v1/agents/{agentId}/heartbeats
```

Heartbeat uses `AgentHeartbeat` and is idempotent by `messageId`. Server response provides accepted time, clock-skew estimate, compatibility state, policy/config version hints, and command cursor hint. Heartbeat health does not imply instance health.

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

## Configuration reconciliation

```text
GET /api/v1/agents/{agentId}/configuration?afterVersion=<token>
POST /api/v1/agents/{agentId}/configuration/{version}:ack
```

Configuration contains only authorized non-secret values and credential references. It is signed/hashed at the application layer, versioned, validated before activation, and applied atomically. Agent keeps last-known-valid configuration and reports rejection without discarding it.

## Command retrieval and lifecycle

```text
GET  /api/v1/agents/{agentId}/commands?after=<cursor>&limit=<bounded>
POST /api/v1/agents/{agentId}/commands/{commandId}:ack
POST /api/v1/agents/{agentId}/commands/{commandId}:result
```

1. Server authorizes and persists a command before it becomes available.
2. Agent retrieves by durable cursor, verifies target, expiry, cancellation, versions, capability, and local policy.
3. Agent acknowledges receipt idempotently.
4. Agent executes one typed attempt under timeout/cancellation rules.
5. Agent performs the required independent post-probe.
6. Agent submits a terminal or `UnknownOutcome` result idempotently.

SignalR may notify `commandsAvailable(agentId, cursor)` only. It never carries executable parameters and never changes command state.

## Cancellation and expiry

- Pending/acknowledged commands may be cancelled before execution according to policy.
- Agent checks expiry immediately before side effects.
- A running operation that cannot safely cancel completes/returns its real state; the API never labels it cancelled solely because a request arrived.
- Retry never creates a second effect when a terminal result or idempotency record exists.

## Retry policy

- Retry only failures classified retryable.
- Use exponential backoff with jitter and a server-provided `Retry-After` ceiling.
- Authentication, authorization, revoked identity, incompatible version, invalid schema, and unsupported capability do not retry indefinitely.
- Backpressure reduces batch size/frequency and exposes queue depth/age.

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
