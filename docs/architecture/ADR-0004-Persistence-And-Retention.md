# ADR-0004 — Persistence, Migrations, and Retention

- Status: accepted
- Date: 2026-07-11
- Owners: data and platform architecture

## Context

The Agent needs bounded offline state; the server needs durable catalog, observations, events, commands, RBAC, audit, and outbox data. SQLite and PostgreSQL have different concurrency and operational characteristics. Monitored databases must never be used as DB-Notifier storage.

## Decision

- Use SQLite for Agent-authorized configuration metadata, last known state, scheduler checkpoints, inbox/outbox, and bounded observation cache.
- Use PostgreSQL for central catalog, Agent registry, policies, observations, events/incidents, alerting, commands, identities/RBAC, audit, and server outbox.
- Use EF Core behind Infrastructure repositories/units of work, with separate Agent and Server DbContexts and provider-specific migration sets. Domain/Application do not expose EF types.
- Generate migrations from an explicit model snapshot, review SQL, and test forward/rollback in disposable environments. Production application is separately authorized in later phases.
- Use UTC instants, globally unique IDs, optimistic concurrency tokens, foreign keys, check constraints, and unique idempotency keys.
- Partition/aggregate high-frequency observations by time and scope when measured volume requires it. Events, incidents, commands, and audit remain separate from raw samples.

## Initial retention classes

| Class | Initial policy for design/testing | Notes |
|---|---|---|
| Raw health observations | 30 days central; bounded 7 days/size locally | configurable by environment; aggregate before deletion |
| Hourly aggregates | 13 months | supports seasonality/capacity review |
| Events/incidents | 24 months | longer policy may be required by organization |
| Command attempts and audit | 24 months minimum design target | append-only/protected; legal policy may override |
| Agent outbox | until acknowledged, then short tombstone retention | hard size/age limit with explicit overflow |
| AIOps datasets/features | no default collection | separate opt-in policy and classification required |

These are architectural defaults for capacity and schema design, not a production data-retention authorization.

## Alternatives

- One shared schema/provider for SQLite and PostgreSQL: rejected because provider differences would hide unsafe migrations.
- Store all raw telemetry forever: rejected for cost, privacy, and query-performance reasons.
- Eventual persistence without an outbox: rejected because partial failures would lose or duplicate state changes.

## Consequences

- Retention workers need idempotent, observable batches and legal-hold exclusions.
- Local database corruption/recovery and central backup/restore require distinct runbooks.
- Schema compatibility is part of Agent/API release negotiation.
- Soft delete is not universal; audit/event history uses explicit lifecycle fields.

## Acceptance checks

- `STATE-03` model distinguishes secrets by opaque reference and monitored databases from internal storage.
- Migrations are tested for clean create, upgrade, failed upgrade, restore, and rollback where supported.
- Retention tests protect audit/command evidence and expose deletion/aggregation counts.
