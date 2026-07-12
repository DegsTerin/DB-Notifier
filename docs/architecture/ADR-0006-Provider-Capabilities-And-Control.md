# ADR-0006 — Provider Capabilities and Administrative Control

- Status: accepted
- Date: 2026-07-11
- Owners: provider and security architecture

## Context

Database engines and platforms expose different readiness, metric, discovery, and administrative mechanisms. Treating Start/Stop/Restart as universal would create unsafe guesses. Observation and administration also require different credentials and authorization.

## Decision

- Providers implement typed contracts for configuration validation, health probing, error normalization, capability discovery, and approved administrative adapters.
- Capability IDs are versioned data, not UI conditionals. Initial examples include `health.readiness`, `health.authenticated`, `discovery.windows-service`, `control.start`, `control.stop`, and `control.restart`.
- Capability state is `Supported`, `Unsupported`, `Unavailable`, or `Unknown`, with reason, prerequisites, platform scope, provider version, and observation time.
- Absence or uncertainty returns `Unsupported`/`Unknown`; the core never improvises an engine or OS command.
- Separate `IMonitoringCredential` and `IAdministrativeCredential` references and provider sessions.
- Administrative execution requires a typed command adapter chosen by provider/platform capability, server-side RBAC, target scope, reason, confirmation, idempotency, expiry, maintenance policy, and immutable audit.
- A command is successful only after the adapter result and an independent post-command probe satisfy the command's verification policy. Timeout or ambiguous effect returns `UnknownOutcome`, not success.
- The legacy PowerShell Windows service path is characterization evidence only. The target PostgreSQL provider will use a dedicated Windows-service adapter for local services; remote service control is unsupported until a separately homologated mechanism exists.

## Alternatives

- Generic shell command templates: rejected because injection, quoting, privilege, and platform semantics are unsafe.
- SQL-based control for all engines: rejected because database availability and vendor semantics differ.
- UI-only action hiding: rejected because authorization must be server-side.

## Consequences

- Provider packages include capability fixtures and a homologation matrix.
- UI renders unavailable/unsupported/denied distinctly.
- New engines can ship observation-only before administrative control.
- Provider native diagnostics can accompany canonical errors only after redaction and size limits.

## Acceptance checks

- Capability matrix states implementation and homologation separately.
- Negative tests cover missing admin credential, denied RBAC, expired command, incompatible Agent, unsupported platform, duplicate request, and ambiguous post-probe.
- No provider support claim is derived only from an enum or roadmap entry.
