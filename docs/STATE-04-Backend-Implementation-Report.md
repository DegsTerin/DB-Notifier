# STATE-04 Backend Implementation Progress Report

## Outcome

The first seven `STATE-04 BACKEND_IMPLEMENTATION` increments are implemented in .NET 10. They establish a provider-neutral Domain/Application slice, an open provider registry/SDK, PostgreSQL readiness/authenticated probes, hosted monitoring/outbox/maintenance workers, transactional local persistence, platform vault readers, idempotent central ingestion, canonical event/alert derivation, Agent/human authorization, command delivery/ack without execution, bounded retention, durable delivery state machines, authorized audit reads, and fail-closed provider-package verification.

This is incremental evidence, not closure of `STATE-04`, provider homologation, or public PostgreSQL support.

The automatic closure audit is currently **REPROVADO** because the accepted M4 configuration migrator, characterized PostgreSQL discovery behavior and required negative fixture matrix are incomplete. See [`STATE-04-Backend-Implementation-Audit.md`](STATE-04-Backend-Implementation-Audit.md). The seven increments below remain valid implementation evidence.

## Delivered scope

- Open `ProviderType` value object with canonical stable identifiers and no closed engine enum.
- Canonical health statuses, evidence levels, normalized error categories, retryability, observation quality, and validated health observations.
- Opaque credential references with separate monitoring, administration, OS-control, and cloud-control purposes.
- Provider-owned endpoint properties with explicit rejection of secret-shaped fields.
- `IDatabaseProvider`, capabilities, endpoint validation, probe result, and open `ProviderRegistry` contracts.
- Provider-neutral `ProbeInstanceHandler` that resolves plugins, rejects invalid configuration, isolates provider failures, and never maps unknown evidence to healthy.
- PostgreSQL typed endpoint validation and readiness mapping for `pg_isready` results.
- Safe native process invocation through `ProcessStartInfo.ArgumentList` with no shell concatenation.
- Bounded timeout and TCP fallback; transport-only success maps to `Degraded`, never `Healthy`.
- Agent dependency-injection registration for providers, persistence, scheduler, and vault adapters; monitoring remains disabled by default.

## Dependency direction

```text
Domain
  ^
Provider.Abstractions
  ^                 ^
Application         PostgreSql provider
  ^                 ^
  +------ Agent ----+
```

- Domain references no DB-Notifier outer assembly.
- Provider abstractions reference Domain only.
- Application references Domain and provider abstractions, never a concrete provider.
- PostgreSQL references provider abstractions, never Application.

## Increment 2

- `ProbePolicy` bounds per-attempt timeout/backoff to five minutes, attempts to 10, and exponential backoff to the configured maximum.
- One vault-resolved monitoring credential lease is reused across retries and zeroed/disposed immediately after the probe operation.
- Monitoring rejects administration/OS/cloud credential purposes and normalizes vault failure without calling the provider.
- `MonitoringCycleRunner` obtains due assignments through a port and isolates probe/persistence failure per instance.
- PostgreSQL authenticated health uses an Npgsql connection, TLS mode `require` or `verify-full`, a fixed `SELECT 1`, disabled pooling, and bounded connection/command timeout.
- PostgreSQL authentication, network, timeout, unknown, and healthy outcomes map to canonical statuses without native exception text.
- `AgentObservationOutboxSink` writes the sanitized observation, monotonically increasing checkpoint, and outbox envelope in one SQLite transaction.
- Duplicate-observation failure rolls back the outbox and sequence; payload tests confirm that credential/password fields are absent.
- The Agent resolves credentials only through the vault port; platform readers are added in Increment 3.

## Increment 3

- `AgentStoreInitializer` creates the configured directory and applies local SQLite migrations only when explicitly invoked by enabled monitoring.
- `AgentMonitoringAssignmentSource` reads enabled/due assignments, converts primitive provider-owned endpoint JSON, parses opaque credential-reference JSON, and skips invalid entries with sanitized structured event codes.
- `AgentMonitoringWorker` runs recurring cycles with bounded 1–300 second cadence and structured due/persisted/failure/duration telemetry.
- Monitoring is disabled by default. Disabled startup does not initialize/migrate the store or connect to any database.
- `CompositeCredentialVault` selects an exact stable vault provider and rejects unknown/duplicate adapters without fallback.
- `windows-credential-manager` reads a Generic Credential target through `CredReadW`; its blob convention is UTF-8 secret plus the Credential Manager username.
- `linux-secret-service` invokes `secret-tool` without a shell using attributes `application=db-notifier` and `id=<locator>`; its secret value convention is username, newline, then secret.
- Both platform readers copy into the disposable lease and zero temporary mutable buffers where the runtime permits. Neither adapter writes/provisions credentials.

## Increment 4

- Provider-neutral synchronization contracts model bounded batches, per-item accepted/duplicate/rejected/retryable outcomes, monotonic sequence acknowledgement, and a highest contiguous server cursor.
- `AgentOutboxStore` dispatches only ordered due items, records terminal acknowledgements, and applies capped exponential retry without deleting the tombstone.
- `AgentOutboxDispatchWorker` is disabled by default, shares race-safe SQLite initialization, emits structured counts, and isolates transient dispatch failure.
- `HttpObservationBatchTransport` sends versioned JSON only to an absolute HTTPS base address; enabled runtime requires a valid private-key client certificate and keeps normal server certificate validation/revocation enabled.
- The HTTPS transport rejects user-info/query/fragment base addresses even when instantiated outside the Agent composition root; Kestrel requests optional client certificates and the ingestion policy still denies requests without a validated Agent identity.
- HTTP timeout, unavailable transport, missing item result, and malformed successful response remain retryable; an invalid `2xx` contract never terminally acknowledges local data.
- `ServerObservationIngestionStore` rejects inactive Agents and unassigned instances, deduplicates message/observation IDs, rejects conflicting Agent sequences terminally, and persists the sample, event, server outbox and matching alert deliveries atomically.
- Status transitions derive bounded canonical `Connected`, `Recovered`, `Disconnected`, `Timeout`, `AuthenticationFailed`, or `Degraded` events; unchanged status emits no duplicate event.
- PostgreSQL migration `EnforceAgentObservationSequence` adds unique `(agent_id, sequence)` enforcement with a reversible down path.
- `POST /api/v1/agents/{agentId}/observations:batch` requires a valid enrolled client certificate and an identity claim exactly matching the route; protocol v1 and 1 MiB request limits fail closed.

## Increment 5

- Human API authentication uses a distinct `HumanBearer` JWT scheme backed by an external absolute-HTTPS OIDC authority/audience; repository defaults contain no issuer, signing key, token, password, or bootstrap administrator and therefore fail closed. Human endpoints apply a fixed per-client-IP rate limit with no queue.
- The authenticated JWT `sub` resolves only to an active platform user. RBAC is evaluated server-side through role assignments and exact permission codes with non-expired `Global`, `Environment`, or `Instance` scope.
- `GET /api/v1/catalog/instances` returns only minimal non-secret instance projections authorized by `instances.read`; endpoint JSON and credential references are not exposed.
- `POST /api/v1/instances/{instanceId}/commands` requires `commands.create`, an active assigned Agent, an exact `Supported` capability/version claim, bounded expiry, stable identifiers, reason, and object-shaped parameters without secret/free-form SQL/shell fields.
- Command creation is idempotent: an exact retry returns the existing command, while reuse of the key with different intent returns conflict.
- Created commands remain `Pending`; no server outbox, Agent inbox, command attempt, Start/Stop/Restart adapter, or administrative execution is invoked by this increment.
- Created, duplicate, denied, unavailable-capability, conflicting, and semantically invalid requests emit sanitized append-only audit entries without copying reason, parameters, credentials, tokens, or the idempotency key.

## Increment 6

- Agent retention selects bounded seven-day observations, 24-hour acknowledged-outbox tombstones and 30-day terminal inbox commands; dry-run and apply modes use parameterized SQLite time queries.
- Central retention selects bounded unreferenced 30-day observations, 90-day heartbeats, 12-month delivered notifications and seven-day published server-outbox tombstones. Referenced observations, unpublished outbox and all audit entries are preserved.
- Retention workers are opt-in; `ApplyChanges` remains false by default and no production deletion, legal-hold override or external database is authorized.
- `ServerOutboxDeliveryRunner` and `NotificationDeliveryRunner` isolate per-item failure, enforce batches up to 100, preserve stable IDs for consumer deduplication, and persist capped exponential retry/delivery state.
- Missing publisher/channel adapters remain retryable. The repository registers no external e-mail, webhook, queue or other delivery adapter, so no external contact occurs automatically.
- `GET /api/v1/audit` requires active-human `audit.read` with non-expired Global scope, bounded snapshot-stable pagination/filters, rate limiting and a sanitized audit entry for successful or denied reads.

## Increment 7

- Versioned command polling/ack contracts carry message identity, schema, Agent, monotonic durable sequence, timestamps, exact Agent/provider versions and bounded batches.
- The mTLS Agent API exposes route-bound polling and ack endpoints. The Server expires stale commands, delivers only exact-compatible assignments, and accepts ack idempotently only from the assigned Agent.
- The Agent inbox rejects conflicting command/idempotency replays, persists exact retries once, and advances only from `Available` to `Acknowledged`. No `CommandAttempt`, terminal result, provider call, or Start/Stop/Restart execution exists in this path.
- The reversible Agent migration `AddCommandCompatibilityEnvelope` persists provider and expected Agent/provider versions; pre-existing rows are backfilled as `unknown` and therefore cannot silently satisfy exact compatibility.
- Command polling is independently disabled by default; enabled operation still requires the existing HTTPS/private-key certificate configuration.
- Provider package discovery scans only bounded top-level directories and returns verified descriptors plus sanitized diagnostics. It never uses reflection, `AssemblyLoadContext`, entry-point activation, or automatic registration.
- Verification requires schema v1, `net10.0`, a trusted RSA public-key ID, RSA-PSS/SHA-256 signature over the exact manifest bytes, SHA-256 for every declared file, entry-assembly hash coverage, bounded files/counts/sizes, and rejects traversal, links and reparse points.
- No trusted key or package root is configured by default, and no private signing material is stored in the repository.

## Capability truth

| Capability | Implementation | Homologation/public support |
|---|---|---|
| Open provider registration | Implemented in-process registry plus signed package discovery/verification | Verified packages are not loaded or homologated |
| PostgreSQL endpoint validation | Implemented and unit-tested | Not homologated |
| PostgreSQL `pg_isready` readiness | Implemented | No real PostgreSQL execution in these increments |
| TCP fallback | Implemented; transport-only canonical mapping tested | No live network integration evidence |
| Authenticated PostgreSQL health | Implemented with Npgsql/TLS/fixed query; outcome mappings unit-tested | No real credential/database execution or homologation |
| Retry/backoff and monitoring cycle | Implemented through provider-neutral Application ports | Hosted scheduler exists; no live enabled run |
| Agent SQLite observation/outbox | Implemented and tested transactionally in memory | Initialization implemented; no production store/retention worker |
| Hosted Agent scheduler | Implemented, structured logs, disabled by default | No enabled live-target execution |
| SQLite store initialization/assignments | Implemented; migration and due/invalid filtering tested | No production store/retention exercise |
| Windows/Linux vault readers | Implemented read-only with exact provider selection | No real credential lookup performed |
| Agent outbox dispatch/ack | Implemented with ordered batching, terminal ack and capped retry | No live enrolled Agent/API exchange |
| Central observation ingestion | Implemented idempotently with Agent/instance scope validation | Logic tested on ephemeral SQLite; no PostgreSQL instance contacted |
| Canonical events/alerts | Implemented for health transitions and matching alert delivery preparation | No external notification channel delivered |
| Agent ingestion API | Implemented with certificate authentication and route-bound authorization | Negative live smoke only; no real certificate enrollment/mTLS E2E |
| Human authentication | External OIDC/JWT bearer scheme, fail-closed defaults | No real identity provider/token/MFA flow exercised |
| Scoped RBAC/catalog | Active-user, permission, expiry and Global/Environment/Instance scope enforced | Read-only catalog slice; no role/catalog administration API |
| Administrative command creation/delivery | Idempotent, audited creation plus exact-version mTLS poll/ack and durable Agent inbox | No attempt, adapter execution, post-probe, or homologation |
| Agent/central retention | Bounded dry-run/apply stores and opt-in workers | Apply disabled by default; no production/legal-hold exercise |
| Server outbox/notification delivery | Durable batch/retry/dedup state machines | No external publisher/channel adapter registered or homologated |
| Audit query API | Global `audit.read`, bounded filters/pagination and read auditing | No export pipeline or real IdP/PostgreSQL exercise |
| Start/Stop/Restart | Explicitly `Unsupported` | Not implemented or authorized |
| Other database providers | Registry accepts arbitrary identifiers | Adapters not implemented |

## Verification evidence

Executed from the repository root with workspace-local .NET SDK `10.0.301`:

| Check | Result |
|---|---|
| Forced restore and lockfile refresh | Approved for all 12 projects |
| Release build with warnings as errors | Approved; 0 warnings, 0 errors |
| Unit/model/provider tests | Approved; 82/82 |
| Architecture tests | Approved; 4/4 |
| Total .NET tests | Approved; 86/86 |
| Synchronization certification subset | Approved; 21/21 |
| Human access/RBAC subset | Approved; 8/8 |
| Maintenance/delivery/audit subset | Approved; 13/13 |
| Command/package verification subset | Approved; 6/6 |
| Additional negative tests | Approved for monotonic head blocking, rejected tombstone, missing/invalid response, timeout, HTTPS enforcement, sequence conflict, Agent/instance scope and route authorization |
| `dotnet format --verify-no-changes` | Approved |
| Locked restore | Approved for all 12 projects |
| NuGet direct/transitive vulnerability audit | Approved; no vulnerable packages reported |
| Legacy compatibility Pester suite | Approved; 10/10 |
| Disabled Agent startup smoke | Process remained alive after DI/host build, logged command polling/monitoring/sync/retention disabled, was terminated, and created no SQLite file or probe |
| Local API authorization smoke | Liveness returned 200; command polling without a client certificate returned 403 (observation ingestion denial remains previously evidenced) |
| Human API fail-closed smoke | Catalog and audit query without a bearer token returned 401 |

Test samples additionally cover ordered dispatch/ack/retry, blocked head-of-line ordering, terminal tombstones, malformed responses, timeout, Agent/instance scope, sequence conflict, canonical transitions, alert deliveries, scoped catalog filtering, expired/missing permissions, capability denial, exact idempotency, sanitized audit, dry-run/apply retention, referenced/unpublished preservation, server-outbox retry, missing notification adapter/backoff, successful delivery and paginated audit reads.

## Limits and next increment

- No non-ephemeral database, external service, remote network endpoint, vault, production migration, notification channel, or administrative action was contacted or changed. Only the local API liveness/denial smoke used loopback HTTP.
- Provider activation, real OIDC/MFA/token integration, role/user/catalog mutations, audit export, command execution/post-probe, real notification adapters, legal-hold/backup integration, metrics/traces, enrollment workflow, and a real mTLS Agent/API exchange remain pending.
- Platform vault readers have no live-secret evidence. Windows expects a Generic Credential UTF-8 blob; Linux requires `secret-tool`/Secret Service and the documented two-line secret convention.
- Npgsql necessarily consumes a managed password string from its password-provider callback for the connection lifetime; the password is excluded from the connection string, pooling is disabled, and the disposable source character buffer is zeroed, but runtime secret review remains required.
- Runtime package discovery/signature/hash verification is implemented without loading; sandboxed activation and lifecycle isolation remain pending.
- UI remains reserved for `STATE-05`.
- `STATE-04` Human Gate remains pending until the phase deliverables and negative provider/authorization evidence are complete.
