# Agent/API Protocol v1

Observation batching and the receipt-only command polling/ack subset are implemented in `STATE-04`. Enrollment, heartbeat, configuration, event batching, command execution/result and SignalR remain conceptual targets.

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
