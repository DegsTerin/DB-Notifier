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

## Alternatives

- Silent self-update from an unsigned URL: rejected for supply-chain and rollback risk.
- In-place replacement without side-by-side cache: rejected because recovery from failed startup would be fragile.
- One artifact with Agent service and Dashboard/server: rejected because platforms, privilege, and rollout cadence differ.

## Consequences

- Final installer technology remains proposed until a `STATE-02` Human Gate accepts WiX or an equivalent reproducible signed toolchain.
- Update compatibility must be exercised across Agent/API version pairs.
- Rollback preserves identity, configuration, SQLite backup, logs, and audit while reverting binaries only unless a separate schema procedure is approved.
- Downgrade is blocked when local schema/protocol is incompatible with the target binary.

## Acceptance checks

- Threat model covers compromised feed, stolen signing authorization, downgrade, partial install, reboot, and revoked certificate.
- Release gate proves signature, hash, clean install, upgrade, interrupted upgrade, rollback, and uninstallation without deleting user data by default.
- Legacy and production package names/product IDs cannot overwrite each other accidentally.
