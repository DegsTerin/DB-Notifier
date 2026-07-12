# ADR-0002 — Secrets and Agent Identity

- Status: accepted
- Date: 2026-07-11
- Owners: security and platform architecture

## Context

Agents need provider credentials without exposing secret material to the API, Dashboard, logs, configuration, or ordinary persistence. Monitoring and administration require different privileges. Agents also require revocable identities for outbound API communication.

## Decision

- Represent every provider credential as an opaque `CredentialReference`; ordinary config and databases never contain a plaintext password or complete connection string.
- On Windows standalone/Agent deployments, use a vault adapter backed by DPAPI-protected machine/user scope or Windows Credential Manager according to the service identity selected during installation.
- On servers, use an external secret-manager adapter. The central database stores only provider, locator/reference, metadata, and rotation state.
- Separate monitoring and administrative references. Absence of an administrative reference means administrative capability is unavailable.
- Provision each Agent with a unique identity. Enrollment uses a one-time, short-lived, scope-bound token delivered out of band; the token cannot publish observations or receive commands.
- After enrollment, use a client certificate for mTLS. Certificates are short-lived relative to installation lifetime, rotated before expiry, and revocable by Agent ID/certificate thumbprint.
- Bind Agent identity to tenant/environment scope and declared platform version. Capabilities sent by an Agent are claims that the server validates against policy and registered provider metadata.
- Secret retrieval occurs inside the Agent immediately before provider use. Secret values are not returned through Application contracts and are cleared/disposed where the runtime allows.

## Alternatives

- API-managed encrypted passwords: rejected because the encryption key and bulk credential store increase central blast radius.
- Shared Agent API key: rejected because revocation, attribution, and compromise isolation are inadequate.
- Enrollment token as permanent identity: rejected because bootstrap material has different risk and lifecycle.

## Consequences

- Vault and certificate adapters are infrastructure concerns behind ports.
- Backup/restore must preserve references and identity metadata without pretending external secrets were backed up.
- Rotation can temporarily produce `CredentialUnavailable`; it must not appear as database failure or healthy state.
- Offline Agents can use locally authorized monitoring references but cannot bypass expiry/revocation checks for centrally issued commands.

## Threat controls

- Never log secret values, connection strings, enrollment tokens, certificate private keys, or provider command text containing secrets.
- Protect enrollment against replay and bind it to expected Agent metadata.
- Store private keys non-exportably where platform support permits.
- Rate-limit and audit enrollment, rotation, revocation, secret lookup failures, and administrative-reference changes.
- Redact provider exceptions before persistence or transport.

## Acceptance checks

- Threat-model walkthrough covers stolen token, cloned Agent, revoked certificate, vault unavailable, and rotation failure.
- Negative contracts prove monitoring credentials cannot execute an administrative command.
- Recovery procedure distinguishes restoring DB-Notifier metadata from restoring external secrets.
