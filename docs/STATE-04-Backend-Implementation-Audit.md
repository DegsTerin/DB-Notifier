# STATE-04 Backend Implementation Automatic Audit

## Decision

**REPROVADO** on 2026-07-12. The implemented backend increments are buildable, tested, provider-neutral and fail closed, but the accepted `STATE-04`/M4 deliverable set is not complete. This result does not transition the project and does not invalidate the increments already delivered.

Audited baseline: commit `ed20903` on `main`, workspace-local .NET SDK `10.0.301`, Windows-hosted .NET/PowerShell processes and local Node.js tooling. No monitored database, real credential, certificate, IdP, external channel, remote migration, deployment or administrative action was used.

## Expected deliverables

The audit compared the workspace with:

- `prompts/governance/Lifecycle.md` — provider-neutral Domain/Application, authorized providers, persistence, API, probes, normalized errors, events, alerts, RBAC, commands and tests.
- `docs/Legacy-Migration-Plan.md` M4 — PostgreSQL behavior compatible with the characterized legacy slice, configuration migration with dry-run/backup/validation/idempotency, bounded isolated monitoring, security-negative coverage and no unsupported engine claim.
- Accepted ADR-0001 through ADR-0006, canonical contracts, Agent/API protocol, threat model and provider capability matrix.

## Gate results

| Gate | Result | Evidence |
|---|---|---|
| State and authorized scope | APROVADO | Workspace remains in `STATE-04`; no UI, deployment, real service control or phase transition was introduced. |
| Mandatory .NET 10 baseline | APROVADO | SDK `10.0.301`; all active .NET projects target `net10.0` or `net10.0-windows`. |
| Dependency architecture | APROVADO | 4/4 architecture tests; Domain has no outer DB-Notifier references, Application has no concrete provider, and provider abstractions remain inward-facing. |
| Provider-neutral core | APROVADO | Open `ProviderType`/registry, no engine-name conditionals in Domain/Application, failure isolation and unknown-not-healthy tests. |
| PostgreSQL monitoring slice | APROVADO | Typed endpoint, shell-free readiness/TCP fallback, authenticated Npgsql probe, normalized outcomes and unsupported admin capabilities are implemented and unit-tested; no live PostgreSQL evidence exists. |
| Legacy configuration migration | REPROVADO | No CLI/service implements dry-run, source preservation, timestamped backup, validation report, secret rejection and idempotent rerun required by M4 and `Legacy-Migration-Plan.md`. |
| Characterized PostgreSQL discovery compatibility | REPROVADO | The target provider accepts an explicit path or `pg_isready` on `PATH`, but does not implement/test the characterized Windows installation discovery behavior required by M4. |
| Required provider/security fixtures | REPROVADO | Canonical mappings and authorization negatives exist, but the required fixture matrix is incomplete for missing utility, DNS/refused-port execution, expired credential, local-service states and post-command `UnknownOutcome`/post-probe scenarios. Administrative execution remains intentionally absent and safe. |
| Server-side authorization | APROVADO | Human routes require JWT plus server-side RBAC; Agent routes require enrolled-certificate identity bound to the route. Runtime denial: catalog/audit `401`, command poll `403` without certificate. |
| Idempotency and offline durability | APROVADO | Observation outbox/ingestion, command creation/poll/ack, conflict replay, expiry, bounded retry and durable checkpoints are covered. Full reconnection/reorder E2E remains correctly assigned to `STATE-06`. |
| Secrets and command safety | APROVADO | Safe defaults contain no credentials; monitoring/admin purposes are separated; no shell command template or permissive TLS override was found. One explicit `not-a-real-secret` negative fixture was inspected and is not credential material. |
| Persistence and rollback | APROVADO | Six non-production migrations, SQLite forward/rollback tests, PostgreSQL script assertions, retention preservation and append-only audit controls. |
| Dependency and supply-chain checks | APROVADO | NuGet and npm audits reported zero known vulnerabilities; provider package signature/hash tamper tests pass; packages are not auto-loaded. |
| Documentation consistency | REPROVADO | `Provider-Capability-Matrix.md` still labels authenticated health, canonical event persistence and outbox dispatch as partial/planned despite implemented evidence. |
| UI boundary | APROVADO | Dashboard remains a build-only scaffold and WPF contains no functional `STATE-05` product flow. |

## Commands and observed results

Executed from the repository root unless noted:

| Check | Result |
|---|---|
| `dotnet restore DBNotifier.sln --locked-mode` | APROVADO; 12 projects |
| Release build with warnings as errors | APROVADO; 0 warnings, 0 errors |
| `dotnet test` | APROVADO; 82 unit/model/provider + 4 architecture = 86/86 |
| `dotnet format --verify-no-changes --no-restore` | APROVADO |
| NuGet direct/transitive vulnerability audit | APROVADO; no vulnerable packages reported |
| Dashboard `npm ci`, typecheck and Vite build | APROVADO |
| Dashboard `npm audit --audit-level=high` | APROVADO; 0 vulnerabilities |
| Legacy Pester compatibility suite | APROVADO; 10/10 |
| `build/build.ps1 -ValidateOnly` | APROVADO; bundle validation passed |
| Static engine-name scan in Domain/Application | APROVADO; no concrete engine conditional found |
| Static private-key/credential-pattern review | APROVADO with inspected negative fixture; no private key or committed runtime secret found |
| API fail-closed smoke | APROVADO; liveness `200`, human routes `401`, Agent command poll `403` without certificate |
| Disabled Agent smoke | APROVADO; all workers logged disabled and no SQLite file was created |

## Findings

### HIGH — Missing configuration migration deliverable

- Impact: a PgNotifier user cannot perform the approved side-by-side migration safely; M4 exit criteria and rollback evidence are incomplete.
- Reproduction: search of `src/` and `tests/` finds no configuration migration application service, CLI or test suite; only the requirements remain in migration/compatibility documents.
- Required correction: implement a provider-neutral migration service and safe CLI mode that reads without modifying the legacy source, rejects secret material, supports dry-run, writes a sanitized report, creates a timestamped backup before target replacement, writes atomically, and treats an exact rerun idempotently.

### MEDIUM — PostgreSQL discovery and fixture gaps

- Impact: the new provider does not yet match the M4 characterized discovery promise, and several required failure modes are inferred from fakes rather than exercised through adapter fixtures.
- Required correction: add a typed Windows `pg_isready` discovery adapter without shell use; add negative fixtures for missing utility, DNS/refused port, credential invalid/expired semantics and incompatible capability/version. Post-command and local-service fixtures may prove explicit `Unsupported` until an executor is authorized, but the matrix must test that behavior.

### LOW — Capability matrix drift

- Impact: readers may understate implemented authenticated monitoring, canonical events and durable outbox behavior, while the progress report states them correctly.
- Required correction: synchronize implementation wording without changing the `Homologation: None` and `Public support claim: No` columns.

## Non-blocking deferred scope

- Real PostgreSQL/credential failure, enrolled mTLS, IdP/MFA, external notification, legal hold/backup integration, package activation, command execution/post-probe, heartbeat/reconnection E2E and provider homologation remain untested.
- These items belong to later integration/homologation unless explicitly required above by the accepted M4 compatibility exit.
- No provider, including PostgreSQL, may be announced as homologated or publicly supported from this audit.

## Recommendation

Do not request the `STATE-04` Human Gate yet. Execute a remediation increment for the configuration migrator, characterized PostgreSQL discovery/negative fixtures and capability-matrix correction; then rerun this automatic audit. A successful rerun may recommend the Human Gate, which must include real provider-failure and authorization-negative samples before any transition to `STATE-05`.
