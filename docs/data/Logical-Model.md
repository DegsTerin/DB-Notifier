# Logical Data Model and Invariants

## Agent SQLite model

```text
agent_registration
agent_fleet_state
instance_assignments
health_observations
outbox_messages
inbox_commands
checkpoints
```

### Agent registration

Stores locally recognised Agent identity metadata by `agent_id`, with unique installation ID, environment, opaque certificate/key-store reference, public thumbprint, certificate expiry, fail-closed identity state, active configuration version, timestamps, and concurrency token. The ordinary store contains no private key, certificate body or enrollment token. The current Agent-side coordinator refuses a second local registration.

### Agent Fleet state

Stores the next heartbeat sequence, exact pending heartbeat envelope, last durable receipt, assignment ETag/generation/freshness evidence, latest sanitised failure, next monotonic operation fence, current lease owner/kind/fence/expiry and concurrency token. The lease tuple is either completely absent or completely populated, and every heartbeat/assignment mutation validates the exact unexpired fence inside its transaction. The pending envelope is retained across a lost response and cleared only after a compatible receipt. It contains process/connectivity evidence, not instance-health evidence.

### Instance assignments

Stores the last-known-valid authorised assignment for each instance: provider, non-secret endpoint JSON, opaque monitoring credential reference, tags, interval, timeout, retries, policy/version, enabled state, and concurrency token. The broader historical row still has a separate administrative reference for pre-existing command contracts, but Agent Fleet reconciliation always writes that field as null and the transmitted contract cannot carry it.

Constraints enforce positive intervals/timeouts and non-negative retries. Assignment deletion does not cascade into observation history.

### Health observations

Bounded local cache keyed by global observation ID. It preserves provider/method/evidence, canonical status, source time, duration, attempt count, safe error code/message, and redacted detail JSON.

Indexes support instance/time retrieval and retention by creation time. Status constraints reject values outside the canonical health contract. TCP-only evidence is represented by `Degraded` at the contract/application layer.

### Agent outbox

Durable at-least-once queue keyed by global message ID with unique monotonic sequence, schema version, payload, occurrence/availability time, attempt count, and acknowledgement time.

The sequence/index prevents duplicate local ordering slots; `message_id` survives retries. Acknowledged rows may later be purged by bounded retention after a safe tombstone interval.

### Agent inbox commands

Durable command receipt keyed by server command ID with unique idempotency key, target/capability, typed parameters, lifecycle state, expiry, result, and concurrency token. It contains no free-form SQL/shell contract and no resolved credential.

### Checkpoints

One row per stream/cursor with a non-negative sequence. Checkpoints allow safe restart/reconciliation without interpreting last-known health as current health.

## Server PostgreSQL model

```text
agent_enrollment_tokens >── agents ──< agent_certificates
                              ├─< agent_capabilities
                              ├─< agent_heartbeats
                              ├── agent_heartbeat_cursors
                              └─< database_instances ──< health_samples ──< events
                                          │                   │                 │
                                          ├─< incidents       └─< command_attempts
                                          ├─< maintenance_windows
                                          └─< administrative_commands ──< command_attempts

alert_rules
notification_channels ──< notification_deliveries >── events

users ──< role_assignments >── roles ──< role_permissions >── permissions
audit_entries
outbox_messages
```

### Inventory and Agent fleet

- `agent_enrollment_tokens`: public token ID, exact scope, random salt and SHA-256 proof, explicit issue/expiry instants and one-time consumption/revocation state; the original token is never stored. The schema requires expiry after issuance but does not currently impose a maximum lifetime.
- `agents`: unique installation identity, environment/platform/version, enrollment/revocation/last-seen state and concurrency token. The legacy certificate-thumbprint pointer remains only as a compatibility bridge.
- `agent_certificates`: authoritative normalised thumbprint, public-key/CSR digests, validity, lifecycle, monotonic revocation and concurrency metadata; it stores neither certificate bodies nor private keys.
- `agent_capabilities`: versioned provider/platform claims, unique per Agent/capability combination.
- `agent_heartbeats`: immutable process/connectivity evidence with unique message ID and per-Agent sequence, canonical payload digest, protocol range, queue state, Agent/server times and explicit gap evidence.
- `agent_heartbeat_cursors`: highest accepted per-Agent heartbeat sequence and last message ID, retained independently of heartbeat-detail deletion for fail-closed replay protection.
- `database_instances`: provider-neutral inventory with `jsonb` endpoint/tags, separate credential references, assigned Agent, policy timings, archive time, concurrency token.

Deleting an Agent or instance is restricted while durable evidence references it. Instance archival is explicit; it is not a history cascade.

### Monitoring history and operations

- `health_samples`: immutable observation evidence, unique message ID, indexed by instance/time and status/time.
- `events`: immutable canonical events linked optionally to instance, Agent, and source observation; indexed by type/time and correlation.
- `incidents`: mutable operational aggregate with status/severity/open/ack/close times and concurrency token.
- `maintenance_windows`: bounded start/end range with reason and creator.

Agent process health and instance health are separate rows/semantics.

### Alerting and notifications

- `alert_rules`: versioned mutable definition using non-secret `jsonb` scope/config and archival time.
- `notification_channels`: non-secret config JSON plus optional opaque credential reference.
- `notification_deliveries`: unique channel/event delivery, attempt count, terminal state, and safe error code.

### Administrative commands

- `administrative_commands`: unique idempotency key, target/Agent/capability, typed `jsonb` parameters, requester, reason, expiry, authorization snapshot reference, expected versions, lifecycle state, concurrency token.
- `command_attempts`: unique command/attempt number, timing/state, optional post-probe observation, safe error/diagnostic references.

Database success is not inferred solely from adapter completion; the contract requires a post-probe before `Succeeded`.

### Identity, RBAC, and audit

- `users`: unique external subject ID; no password/hash baseline is stored because authentication provider selection belongs to implementation/security integration.
- `roles`, `permissions`, `role_permissions`: normalized permission catalog.
- `role_assignments`: unique user/role/scope tuple with grant actor/time and optional expiry.
- `audit_entries`: immutable actor/action/target/outcome/correlation/detail records. Initial PostgreSQL migration installs a trigger that rejects update/delete.

### Server outbox

Stores durable integration events inside the same transaction as state changes. Unique message ID, schema version, payload, occurrence/availability/attempt/published times support idempotent delivery and recovery.

## Cross-cutting invariants

- No property/column named password, secret value, connection string, or private key exists.
- Credential references are separated by monitoring/administration purpose where applicable.
- JSON is not a substitute for secrets, IDs, indexed time/status, constraints, or core relationships.
- Status/state values use database check constraints matching accepted canonical contracts.
- All many-to-one relationships that preserve evidence use `Restrict`; only relationship tables/capability registrations use deliberate cascade.
- Optimistic concurrency is explicit on mutable configuration/aggregate rows.
- Append-only tables do not expose soft delete.
- Migrations are provider-specific and never run against a monitored database.
- Agent Fleet assignment persistence is configuration evidence only; it does not activate a provider, probe, scheduler or command path.
- Agent Fleet leases coordinate only temporary local sandbox processes. They are not a distributed lock, service lease or authority to enable the ordinary Worker.
- A corrupt, future or incomplete Agent SQLite schema is refused by the sandbox guard without repair or silent recreation.
