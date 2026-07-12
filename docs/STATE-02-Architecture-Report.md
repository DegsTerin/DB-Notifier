# STATE-02 Architecture Report

## Execution

- State/fase: `STATE-02 ARCHITECTURE`
- Corpus version: `3.2.0`
- Branch baseline: `main` at `b1dd151`
- Date and executor: 2026-07-11, Codex
- Scope: system boundaries, six ADRs, canonical contracts, Agent/API protocol, threat model, provider matrix, AIOps guardrails, compatibility, rollback, and evolution constraints
- Implementation scope: none; no product rule, provider, schema, migration, secret, deployment, or real administrative action was added

## Deliverables

| Deliverable | Artifact | Status |
|---|---|---|
| Architecture/context/boundaries | `architecture/README.md` | Proposed and internally reviewed |
| Runtime and migration decision | `ADR-0001` | Accepted |
| Secrets and Agent identity | `ADR-0002` | Accepted |
| Agent/API compatibility | `ADR-0003` | Accepted |
| Persistence and retention | `ADR-0004` | Accepted; retention values remain design defaults, not production deletion authority |
| Packaging/signing/update | `ADR-0005` | Accepted; toolchain still requires later implementation/validation |
| Provider/admin capability policy | `ADR-0006` | Accepted |
| Canonical health/event/error/command contracts | `Canonical-Contracts.md` | Accepted architecture contract |
| Durable protocol and offline semantics | `Agent-API-Protocol.md` | Accepted conceptual protocol |
| Trust boundaries/threats/controls | `Threat-Model.md` | Accepted architecture model; no penetration-test claim |
| PostgreSQL capability truth | `Provider-Capability-Matrix.md` | Accepted capability baseline; zero DB-Notifier provider homologation |
| MOD-12 data/risk/evaluation constraints | `AIOps-Architecture-Guardrails.md` | Accepted guardrails; roadmap only |

## Material decisions for Human Gate

1. `ACCEPTED`: use .NET 10 LTS exclusively across all active projects, builds, tests, CI, and future implementation; the earlier .NET 8 commit is historical evidence only.
2. Use outbound HTTPS/mTLS Agent identity after one-time enrollment; secret material remains in platform/external vault adapters behind opaque references.
3. Use durable JSON/HTTP `/api/v1` with at-least-once idempotent batches and command polling; SignalR is a non-authoritative hint only.
4. Use separate EF Core SQLite-Agent and PostgreSQL-server models/migration sets; initial retention values guide design but do not authorize production deletion.
5. Propose reproducible signed MSI/WiX production packaging; retain Inno/ps2exe only for legacy compatibility.
6. Require versioned provider capabilities, separate monitoring/admin identities, server RBAC, typed commands, expiry/idempotency, and independent post-probe.

## Architecture audit

| Gate | Result | Evidence |
|---|---|---|
| Dependencies point inward | APROVADO | Domain/Application/provider/infrastructure responsibilities and prohibited dependencies are explicit |
| Partial failure and offline | APROVADO | per-instance isolation, bounded Agent outbox, at-least-once ingestion, gaps/backpressure, stale semantics |
| Agent/API compatibility | APROVADO | version headers, `N/N-1` observation window, fail-closed command compatibility, rolling-upgrade scenarios |
| Security/threat model | APROVADO COM RESSALVAS | controls and negative scenarios defined; real vault, certificate, driver, IPC, and pentest evidence belong to later phases |
| Administrative safety | APROVADO | typed provider adapters, capability, RBAC, separate credential, expiry, audit, post-probe, `UnknownOutcome` |
| Provider truth | APROVADO | PostgreSQL legacy/planned/homologated states separated; all other engines explicitly unsupported/unimplemented |
| Persistence/retention | APROVADO COM RESSALVAS | storage ownership and migration strategy defined; schema/migration evidence belongs to `STATE-03` |
| Update and rollback | APROVADO COM RESSALVAS | signing/rings/rollback defined; WiX/signing service choice and execution remain unvalidated |
| AIOps/AI controls | APROVADO | `OBSERVER` only initially; no secret/executor access; deterministic policy and independent promotion gates |
| Legacy compatibility | APROVADO | canonical/shim/removal policy and side-by-side rollback preserved |

## Required walkthrough scenarios

- Standalone Windows Agent/Desktop during API outage.
- Hybrid Agent behind outbound-only proxy/firewall.
- Duplicate/reordered observation batches and an outbox gap/overflow.
- Agent/API versions `N` and `N-1`; incompatible administrative command.
- Stolen enrollment token, cloned/revoked Agent, expired certificate, and vault outage.
- SSRF/DNS rebinding endpoint and native process-argument metacharacters.
- Viewer/out-of-scope operator attempting Start/Stop/Restart.
- Command duplicate, expiry, cancellation, timeout after possible side effect, and failed post-probe.
- Tampered/downgraded update and rollback across incompatible local schema.
- Prompt-injected/poisoned AIOps content with deterministic fallback and kill switch.

## Evidence checks

- Architecture/corpus links: 38 Markdown files checked, 0 broken links.
- Architecture pack structure: 12 documents, 6 ADRs, all required ADR sections/status present.
- Prompt corpus count: 13, unchanged by architecture docs.
- Whitespace/diff hygiene: approved.
- Product build/tests: not required by architecture-only changes; baseline evidence remains in `STATE-01-Setup-Report.md`.
- External factual check: official .NET lifecycle page reviewed for .NET 8/.NET 10 support dates.

### Accepted ADR-0001 implementation evidence

- Workspace SDK: .NET 10 LTS `10.0.301` selected by `global.json`.
- Active targets: `net10.0` and `net10.0-windows` only; no active .NET 8/9 target.
- Microsoft hosting package: `10.0.9`; test/coverage toolchain lockfiles regenerated from current stable packages.
- Locked restore: all 10 projects approved.
- Release build: all 10 projects, 0 warnings, 0 errors.
- Tests: 2 passed, 0 failed, 0 skipped under .NET 10.
- `dotnet format --verify-no-changes`: approved.
- NuGet transitive vulnerability audit: no vulnerable package reported.
- API runtime sample: `/health/live` returned `{"status":"Alive"}` under .NET 10 and the process was stopped cleanly.

Automatic audit result: `APROVADO COM RESSALVAS`. The Human Gate subsequently accepted all six ADRs. Remaining reservations concern later implementation/homologation evidence, not the architecture decision set.

## Human Gate

- Phase: `STATE-02 ARCHITECTURE`
- Validator and date: Bruno, 2026-07-11
- Automatic report reviewed: accepted through explicit approval in the project session
- ADRs accepted/rejected: ADR-0001 through ADR-0006 `accepted`
- Threat/hybrid walkthrough: architecture scenarios accepted; independent operational/security execution remains required in later phases
- Retention defaults reviewed: accepted as modeling defaults, not production deletion authority
- Packaging/signing choice reviewed: accepted architecturally; WiX/signing implementation remains unvalidated
- AIOps boundaries reviewed: accepted with MOD-12 remaining roadmap/`OBSERVER`-first
- Decision: `APROVADO`
- Evidence: explicit user response “Sim” to acceptance of all six ADRs, closure of `STATE-02`, and transition to `STATE-03`

This gate authorizes `STATE-03 DATABASE_MODELING`. It does not authorize production migrations, provider/backend implementation, deployment, or later-phase gates.
