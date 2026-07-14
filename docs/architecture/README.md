# DB-Notifier Architecture Pack

## Status and authority

This pack contains the `STATE-02 ARCHITECTURE` decisions and contracts used by the technical implementation. It defines boundaries and constraints but is not evidence of implementation. ADR-0001 through ADR-0006 were historically recorded as accepted; the validator later contested whether that bundled Human Gate was informed, so its current lifecycle authority remains pending independent retrospective ratification. Existing implementation continues to follow the decisions until ratification rejects one or a superseding ADR is accepted.

## System context

```text
Operators / Administrators
           |
     Web Dashboard -------------------+
           | HTTPS                    |
           v                          |
    DB-Notifier Server/API <---- notification adapters
           ^
           | outbound HTTPS + mTLS
           |
    DB-Notifier Agent
       |           |
  local outbox   Desktop/Tray
       |
 provider adapters
       |
 monitored database instances and approved OS/service adapters
```

The API is the source of authorization, catalog, policy, command state, and central audit. Agents perform probes close to instances, continue local observation while offline, and synchronize by an idempotent outbox. SignalR may reduce latency but never replaces durable API state. Desktop/Tray can show local state but cannot grant authorization.

## Dependency boundaries

```text
DBNotifier.Domain
       ^
       |
DBNotifier.Provider.Abstractions
       ^
       |
DBNotifier.Application
       ^
       |
Infrastructure / Providers / Agent / API / Desktop
```

- Domain owns canonical value semantics, not drivers, storage, transport, UI, or engine rules.
- Provider abstractions own typed engine capabilities and normalized results.
- Application owns use cases, ports, authorization requirements, and transaction boundaries.
- Infrastructure and interfaces implement adapters.
- Provider-specific persistence is split into Agent/SQLite and Server/PostgreSQL assemblies; neither runtime carries the other runtime's database provider.
- Dashboard consumes versioned API contracts and never accesses central storage directly.
- A provider may depend on abstractions and Application integration points; Domain never depends on a provider.

## Runtime responsibilities

| Component | Owns | Must not own |
|---|---|---|
| Agent | scheduling, provider execution, local cache/outbox, heartbeat, command adapter invocation | human RBAC policy, central truth, plaintext secret persistence |
| Provider | configuration validation, probes, native diagnostics, declared capabilities, typed administrative adapter | cross-instance orchestration, UI, global authorization |
| API | identity, RBAC, inventory, policies, durable events, command lifecycle, central audit | direct access to monitored databases |
| Dashboard | presentation, filtering, accessible interaction, confirmation UX | authorization decisions, secret disclosure |
| Desktop/Tray | local Agent visibility, offline/stale indication, approved local interaction | bypass of API policy for centrally governed commands |

## Failure and consistency model

- Per-instance probe failures are isolated; one provider failure does not stop the scheduler.
- Agent/API communication is at-least-once with idempotent ingestion and monotonic per-Agent sequence numbers.
- Offline observations remain locally queued within retention/size bounds; overflow is explicit and audited.
- Commands have idempotency keys, expiry, target version/capability requirements, and durable terminal states.
- Unknown, stale, incompatible, or unverifiable state never maps to healthy/success.
- SignalR events are hints; consumers re-read authoritative state after reconnect.
- Clock skew is measured. `observedAt`, `receivedAt`, and server time remain distinct.

## Deployment shapes

- Standalone: Agent + Desktop on Windows, local SQLite, no central command path.
- Distributed Agent: headless Agent on Windows, Linux, container, or cloud workload, using authorized local/remote provider connectivity; WPF Desktop remains Windows-specific.
- On-premises: Agents connect outbound to an API/Dashboard deployment inside the organization.
- Cloud/hybrid: Agents connect outbound through approved proxies/firewalls; monitored databases do not need inbound access from the cloud service.
- Central API and Dashboard scale independently. PostgreSQL central storage, queue/backpressure mechanisms, and notification adapters remain server-side.

## Architecture deliverables

- [ADR-0001 — Runtime stack and incremental migration](ADR-0001-Runtime-Stack-And-Migration.md)
- [ADR-0002 — Secrets and Agent identity](ADR-0002-Secrets-And-Agent-Identity.md)
- [ADR-0003 — Agent/API protocol and compatibility](ADR-0003-Agent-API-Protocol.md)
- [ADR-0004 — Persistence, migrations, and retention](ADR-0004-Persistence-And-Retention.md)
- [ADR-0005 — Packaging, signing, and updates](ADR-0005-Packaging-Signing-And-Updates.md)
- [ADR-0006 — Provider capability and administrative control](ADR-0006-Provider-Capabilities-And-Control.md)
- [Canonical contracts](Canonical-Contracts.md)
- [Agent/API protocol](Agent-API-Protocol.md)
- [Threat model](Threat-Model.md)
- [Provider capability matrix](Provider-Capability-Matrix.md)
- [AIOps/AI guardrails](AIOps-Architecture-Guardrails.md)

The incremental delivery and rollback sequence remains in [`../Legacy-Migration-Plan.md`](../Legacy-Migration-Plan.md). Legacy naming compatibility remains in [`../Legacy-Compatibility.md`](../Legacy-Compatibility.md).
