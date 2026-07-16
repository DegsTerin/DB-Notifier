# ADR-0005 — Packaging, Signing, and Updates

- Status: accepted
- Date: 2026-07-11
- Owners: release and Windows platform architecture

## Context

Agents and Desktop clients run with different privileges and must be updated without breaking protocol compatibility, configuration, identity, or rollback. The Inno/ps2exe package is a legacy compatibility artifact and does not meet the final signing/update model.

## Decision

- Package the production Windows Agent as a per-machine Windows service installer and Desktop as an explicitly selected companion feature/artifact.
- Use a reproducible MSI toolchain suitable for CI (proposed WiX Toolset) and Authenticode-sign executable/MSI artifacts with an organization-controlled code-signing key or approved signing service.
- Keep legacy Inno/ps2exe packaging only for the compatibility monitor; it cannot be labeled the production Agent release.
- Publish a signed update manifest containing artifact hash, version, channel, minimum/maximum protocol, rollout constraints, and rollback target.
- Separate `dev`, `preview`, and `stable` channels. Agents update in staged rings with pause/kill controls and health observation.
- Download and verify updates without executing them. Installation requires policy, maintenance window where applicable, signature/hash verification, and preserved prior version/config.
- Never bundle secrets or private signing material. CI receives short-lived signing authorization only in an approved release environment.

## STATE-08 Windows App Runtime obligations

The `STATE-05` Desktop notification increment introduces a deployment dependency on Microsoft Windows App SDK/Windows App Runtime 2.2 without converting the WPF client into a packaged application. This does not change the accepted packaging direction or constitute release evidence. The production installer in `STATE-08` must:

- detect the target architecture and a compatible Windows App Runtime 2.2 before enabling the modern notification path, and install the approved redistributable only with explicit package ownership and signature/hash verification;
- keep the signed Desktop executable and notification identity asset at a stable installer-owned path, because unpackaged Windows notification registration is path-sensitive;
- preserve a functional legacy notification-area fallback when the runtime is absent, damaged, unsupported or cannot be initialised;
- include the runtime/application compatibility pair in clean-install, upgrade, repair and rollback tests, without removing a shared runtime still required by another application;
- restore the prior signed executable, compatible runtime expectation and owned notification registration during rollback; and
- remove DB Notifier-owned startup and notification registration during uninstall while preserving user data, audit evidence, unrelated registrations and shared runtime installations.

An accepted modern-notification API call, an elevated process or a visible Windows card is not proof of production support, provider homologation or successful external notification delivery.

## Alternatives

- Silent self-update from an unsigned URL: rejected for supply-chain and rollback risk.
- In-place replacement without side-by-side cache: rejected because recovery from failed startup would be fragile.
- One artifact with Agent service and Dashboard/server: rejected because platforms, privilege, and rollout cadence differ.

## Consequences

- The reproducible signed MSI direction with WiX or an equivalent toolchain was retrospectively ratified with reservations in `STATE-02`. The exact installer version, signing service and operational integration remain unimplemented decisions for `STATE-08`; architecture ratification is not release evidence.
- Update compatibility must be exercised across Agent/API version pairs.
- Rollback preserves identity, configuration, SQLite backup, logs, and audit while reverting binaries only unless a separate schema procedure is approved.
- Downgrade is blocked when local schema/protocol is incompatible with the target binary.
- Unpackaged notification identity requires a stable installed path; ad hoc build/output paths are development evidence only and must not become release registration targets.

## Acceptance checks

- Threat model covers compromised feed, stolen signing authorization, downgrade, partial install, reboot, and revoked certificate.
- Release gate proves signature, hash, clean install, upgrade, interrupted upgrade, rollback, and uninstallation without deleting user data by default.
- Release gate proves Windows App Runtime detection/installation or safe fallback, stable notification identity across upgrade/rollback, and cleanup of only DB Notifier-owned registration during uninstall.
- Legacy and production package names/product IDs cannot overwrite each other accidentally.
