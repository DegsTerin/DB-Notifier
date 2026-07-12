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
| Runtime and migration decision | `ADR-0001` | Proposed; Human Gate choice required |
| Secrets and Agent identity | `ADR-0002` | Proposed; Human Gate choice required |
| Agent/API compatibility | `ADR-0003` | Proposed; Human Gate choice required |
| Persistence and retention | `ADR-0004` | Proposed; retention values are design defaults only |
| Packaging/signing/update | `ADR-0005` | Proposed; toolchain not installed/validated |
| Provider/admin capability policy | `ADR-0006` | Proposed |
| Canonical health/event/error/command contracts | `Canonical-Contracts.md` | Proposed |
| Durable protocol and offline semantics | `Agent-API-Protocol.md` | Proposed |
| Trust boundaries/threats/controls | `Threat-Model.md` | Proposed; no penetration-test claim |
| PostgreSQL capability truth | `Provider-Capability-Matrix.md` | Proposed; zero DB-Notifier provider homologation |
| MOD-12 data/risk/evaluation constraints | `AIOps-Architecture-Guardrails.md` | Proposed; roadmap only |

## Material decisions for Human Gate

1. Retain the validated .NET 8 scaffold as history/evidence but target .NET 10 LTS before backend implementation because .NET 8 support ends on 2026-11-10.
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

Automatic audit result: `APROVADO COM RESSALVAS`. The package covers the required architecture concerns; acceptance of proposed runtime, identity, persistence/retention, and packaging decisions remains a Human Gate responsibility.

## Human Gate

- Phase: `STATE-02 ARCHITECTURE`
- Validator and date: PENDENTE
- ADRs accepted/rejected: PENDENTE
- Threat/hybrid walkthrough: PENDENTE
- Retention defaults reviewed: PENDENTE
- Packaging/signing choice reviewed: PENDENTE
- AIOps boundaries reviewed: PENDENTE
- Decision: `PENDENTE`
- Required outcome: accept the ADR set (optionally with explicit reservations) before retargeting the scaffold or entering `STATE-03 DATABASE_MODELING`

The project remains in `STATE-02`; this report does not pre-approve schema implementation or product code.
