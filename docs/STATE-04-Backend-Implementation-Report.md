# STATE-04 Backend Implementation Progress Report

## Outcome

The first two `STATE-04 BACKEND_IMPLEMENTATION` increments are implemented in .NET 10. They establish a provider-neutral Domain/Application slice, an open provider registry/SDK, PostgreSQL readiness/authenticated probes, bounded retry policy, an isolated monitoring-cycle runner, and transactional Agent SQLite observation/outbox persistence.

This is incremental evidence, not closure of `STATE-04`, provider homologation, or public PostgreSQL support.

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
- Agent dependency-injection registration for the open registry and PostgreSQL adapter; no scheduler or real target is started automatically.

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
- The Agent registers a safe unavailable-vault default; a real OS/cloud vault adapter remains required before authenticated runtime configuration.

## Capability truth

| Capability | Implementation | Homologation/public support |
|---|---|---|
| Open provider registration | Implemented in-process constructor/DI registry | Not a third-party packaging/loading system yet |
| PostgreSQL endpoint validation | Implemented and unit-tested | Not homologated |
| PostgreSQL `pg_isready` readiness | Implemented | No real PostgreSQL execution in these increments |
| TCP fallback | Implemented; transport-only canonical mapping tested | No live network integration evidence |
| Authenticated PostgreSQL health | Implemented with Npgsql/TLS/fixed query; outcome mappings unit-tested | No real credential/database execution or homologation |
| Retry/backoff and monitoring cycle | Implemented through provider-neutral Application ports | No recurring hosted scheduler/assignment source yet |
| Agent SQLite observation/outbox | Implemented and tested transactionally in memory | No production store initialization/retention worker |
| Start/Stop/Restart | Explicitly `Unsupported` | Not implemented or authorized |
| Other database providers | Registry accepts arbitrary identifiers | Adapters not implemented |

## Verification evidence

Executed from the repository root with workspace-local .NET SDK `10.0.301`:

| Check | Result |
|---|---|
| Forced restore and lockfile refresh | Approved for all 12 projects |
| Release build with warnings as errors | Approved; 0 warnings, 0 errors |
| Unit/model/provider tests | Approved; 36/36 |
| Architecture tests | Approved; 4/4 |
| Total .NET tests | Approved; 40/40 |
| `dotnet format --verify-no-changes` | Approved |
| Locked restore | Approved for all 12 projects |
| NuGet direct/transitive vulnerability audit | Approved; no vulnerable packages reported |
| Legacy compatibility Pester suite | Approved; 10/10 |
| Agent startup smoke | Process remained alive after DI/host build and was then terminated; no probe configured or executed |

Test samples include arbitrary future provider identifiers, duplicate registration, secret-shaped endpoint rejection, unknown provider behavior, all readiness/authenticated mappings, TCP-only degradation, typed argument separation, unsupported administrative capabilities, retry attempts, credential purpose/disposal, vault failure, per-instance isolation, transactional SQLite outbox rollback/sequence, and dependency direction.

## Limits and next increment

- No real database, service, network endpoint, vault, API, migration, or administrative action was contacted or changed.
- PostgreSQL discovery, a real vault adapter, recurring scheduler/assignment source, store initialization, central ingestion, event derivation, RBAC, commands, API endpoints, and operational telemetry remain pending.
- Npgsql necessarily creates managed connection-string/password strings internally for the connection lifetime; pooling is disabled and the disposable source character buffer is zeroed, but stronger platform/vault integration and runtime secret review remain required.
- Runtime plugin package discovery/signature/loading remains pending; this increment provides the open in-process contract and registry.
- UI remains reserved for `STATE-05`.
- `STATE-04` Human Gate remains pending until the phase deliverables and negative provider/authorization evidence are complete.
