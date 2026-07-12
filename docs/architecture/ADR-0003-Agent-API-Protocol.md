# ADR-0003 — Agent/API Protocol and Compatibility

- Status: accepted
- Date: 2026-07-11
- Owners: Agent and API architecture

## Context

Agents may be offline, duplicated, reordered, upgraded gradually, or connected through restrictive outbound-only networks. Health observations and commands need different reliability and authorization semantics.

## Decision

- Use outbound HTTPS with mTLS and JSON contracts under `/api/v1` for the first protocol generation.
- Use durable HTTP endpoints for enrollment, heartbeat, observation/event batches, command polling, acknowledgement, and result submission.
- Use SignalR only as an authenticated hint that new authoritative state or commands exist; loss of a SignalR message cannot lose work.
- Every message carries `messageId`, `schemaVersion`, `agentId`, `sequence`, `occurredAt`, and `sentAt`. Batch ingestion is at-least-once; the server deduplicates by Agent/message or observation ID.
- Maintain a monotonic sequence per Agent outbox. Gaps are visible and request reconciliation; sequence does not replace globally unique IDs.
- Commands are server-created durable resources with `commandId`, idempotency key, target, capability, reason, requested/expiry times, authorization snapshot, and expected Agent/provider versions.
- Agents poll by cursor and acknowledge receipt before execution. Terminal results are idempotent. Expired, cancelled, unsupported, revoked, or version-incompatible commands are never executed.
- Support the current major protocol and one previous major for observation ingestion during rolling upgrades. Administrative commands require an explicitly compatible command contract and fail closed.

## Alternatives

- WebSocket-only protocol: rejected because reconnect/loss semantics would duplicate durable queue behavior.
- Server-initiated inbound Agent connections: rejected for hybrid/firewall compatibility and blast-radius reasons.
- Exactly-once delivery claim: rejected; at-least-once plus idempotency is testable and honest.
- Binary protocol initially: deferred until measured scale justifies additional compatibility cost.

## Consequences

- Server ingestion and command handlers require durable idempotency records.
- Offline queues need size/age bounds and explicit overflow events.
- Version negotiation and compatibility fixtures are release requirements.
- Payload compression may be negotiated later but cannot alter canonical hashes/IDs.

## Security and privacy

- Authenticate Agent before parsing high-cost payloads; apply body/batch/cardinality limits.
- Authorize Agent scope server-side and reject target IDs outside enrollment scope.
- Bind command acknowledgement/result to the intended Agent and command attempt.
- Do not transport secrets in protocol payloads. Provider config contains references and non-secret fields only.

## Acceptance checks

- Sequence diagrams cover enrollment, normal sync, offline replay, duplicate batch, command expiry, cancellation, and incompatible versions.
- Contract tests prove idempotent retries and no administrative execution from a SignalR payload.
- Protocol details in `Agent-API-Protocol.md` remain consistent with this ADR.
