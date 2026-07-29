# ADR-0008 — JOSE Cryptographic Profiles and Key Lifecycle

- Status: proposed
- Date: 2026-07-28
- Revision: 1.2
- Owners: security, identity, platform, data and affected module architecture
- Decision authority: explicit architecture and security decision `JOSE-D1`
- Implementation status: not authorised
- JOSE-0 status: accepted only as documentary preparation; ADR not accepted

## Context

The Server API already validates externally issued signed JWT access tokens
through bounded OIDC discovery and JWKS retrieval. That relying-party path
proves one JWS/JWK use, but it is not a complete JOSE capability: DB-Notifier
does not process every core serialisation, provide JWE, issue product-owned
JWS artefacts, operate a key lifecycle, host an IdP, or use a homologated
vault, KMS or HSM.

The observed baseline also does not configure explicit accepted JWT
algorithms or token types at its validation boundary, and the canonical human
principal/persistence model identifies a person by `subject_id` rather than
the collision-resistant `(issuer, subject)` pair proposed here. These are
recorded gaps, not evidence of exploitation or authority to change the
runtime.

JOSE spans processing formats, algorithms, key representation and application
profiles. A library feature, IANA registration, roadmap item, test harness,
homologation result, runtime switch, authorisation and public support claim are
different facts. Treating them as one status would overstate current support.

The authorised `JOSE-0` documentary package is prepared in:

- [JOSE Security Profile and Key Lifecycle](JOSE-Security-Profile-And-Key-Lifecycle.md),
  which defines provisional profiles, targets, absolute safety caps and
  ownership/trust/data/egress maps;
- [JOSE IANA Registry Coverage](JOSE-IANA-Registry-Coverage.md), which
  classifies the pinned JOSE and JWT Claims snapshots; and
- the
  [JOSE-0 design report](../STATE-06-JOSE-0-Architecture-Security-And-Coverage-Design-Report.md),
  which owns requirement traceability and the future test, migration and
  rollback plans.

Those documents prepare this ADR without accepting it. They introduce no
dependency, migration, runtime, external service, key or lifecycle change.
The owner subsequently accepted the package exclusively as documentary
preparation. That limited review does not approve a profile or numeric value,
meet `JOSE-D1`, accept this ADR or authorise `JOSE-1` or any implementation.

Human identity, Agent enrolment, durable commands, package updates,
confidential envelopes and MOD-12 also have different owners and accepted
ADRs. A generic JOSE layer must not replace mTLS, server-side RBAC, durable
protocol semantics, platform package signing, data classification or Human
Gates.

## Scope and existing authority

This ADR proposes a cross-cutting security boundary. It does not supersede:

- ADR-0002 for Agent identity, enrolment and secret custody;
- ADR-0003 and `Agent-API-Protocol.md` for durable Agent messages, offline
  behaviour, sequencing and idempotency;
- ADR-0004 for persistence and retention;
- ADR-0005 for package, platform signing and updates;
- ADR-0007 for MOD-12 trust distribution, checkpoints, quarantine and
  activation; or
- `Security-And-Access.md`, `Network-Egress-Policy.md`, lifecycle and Quality
  Gates as their thematic authorities.

Any JOSE use that changes one of those contracts requires the owning ADR or
document to be revised first. Multi-signature can prove origin and integrity
under a cryptographic policy; it cannot create RBAC, dual control, business
approval or a lifecycle Human Gate.

## Proposed decision

If accepted after the required feasibility evidence, DB-Notifier will
introduce a versioned JOSE security capability governed by immutable,
purpose-specific profiles. The capability will have three separate completion
claims:

1. normative coverage: every line in the pinned registries and every adopted
   RFC feature is classified;
2. profile implementation: every adopted or safely adapted feature in that
   profile is implemented, bounded and tested; and
3. operational support: only an exact authorised and homologated combination
   of purpose, platform, topology, key custodian and algorithm can be enabled.

The six core JWS/JWE Compact, Flattened JSON and General JSON serialisations
must be demonstrated in a test-only conformance harness. Runtime profiles may
remain narrower. No product JWE profile is required without a justified use
case, and no public JOSE claim is permitted before an authorised `STATE-08`
release gate.

Each internal JOSE profile will bind:

- a trusted purpose and opaque policy revision;
- semantic operation and typed artefact schema;
- explicit `typ`/`cty` rules;
- issuer, subject and exact audience rules where claims apply;
- serialisation, nesting, signature and recipient policy;
- exact algorithm, key type, curve, size and operation allowlists;
- trusted public-key source and opaque private-key operation binding;
- encoded/decoded size, depth, cardinality, candidate-key, time, memory and
  work budgets;
- replay, freshness, rotation, compromise and revocation rules;
- stable sanitised outcomes and canonical audit intent; and
- owner, evidence, sunset and activation authority.

The receiving component selects a trusted policy before parsing the object.
Untrusted `alg`, `enc`, `typ`, `cty`, `kid`, `jku`, `jwk`, `x5u` or `x5c`
never selects policy, trust or network authority.

## Standards and registry boundary

The normative core covers RFCs 7515, 7516, 7517, 7518 and 7519. The extension
inventory covers RFCs 7638, 7797, 8037, 8812, 8725, 9864 and 9964. Only
public/non-secret inputs and sanitised expected outputs from RFC 7520, RFC
8037 and RFC 9964 may be retained; unsafe or legacy examples prove parsing or
refusal, not runtime enablement.

The IANA JOSE registries and IANA JWT Claims registry are captured as
reproducible snapshots with URL, official `Last Updated` value, acquisition
time, bytes and SHA-256. The snapshots prepared for this revision report
`Last Updated: 2026-05-22` for JOSE and `2026-07-20` for JWT Claims. The JOSE
snapshot includes the RFC 9964 `AKP` key type and `ML-DSA-44`, `ML-DSA-65`
and `ML-DSA-87` algorithms. New or changed entries default to
`Unreviewed + RuntimeDisabled`; runtime never queries IANA to make policy.

RFC 9700 and RFC 9068 become normative only for an adopted OAuth/JWT access
token use. OAuth browser guidance that has not completed RFC publication,
including the RFC-to-be 10017 at the time of this revision, remains an
informative watch item. COSE, SD-JWT, DPoP, JSON Proof and other application
protocols require separate use cases and decisions.

The coverage matrix keeps independent fields for registry status,
DB-Notifier decision, roadmap, implementation, verification, homologation,
runtime availability, authorisation and public support. Registry presence or
library capability never grants support.

## Architecture placement

- Domain has no JOSE, token, algorithm, key, IdP or KMS dependency.
- Application owns business purpose, authorisation, replay/idempotency,
  canonical outcomes and an opaque protection-policy reference. It does not
  own `Jose*` profiles, algorithms, serialisations, headers, key descriptors
  or IdentityModel types.
- A Security and Identity Infrastructure boundary owns JOSE profiles,
  bounded parsing/serialisation, public trust resolution, cryptographic
  library adapters and key-operation adapters.
- Server API/BFF composition maps a route and authenticated business action to
  a trusted semantic security operation. No public generic sign, encrypt or
  decrypt endpoint is permitted.
- MOD-11 owns the canonical `AuditEntry` and RBAC. JOSE creates a sanitised
  audit intent, not a parallel audit store.
- Dashboard never stores tokens in Web Storage, inspects an access token to
  make authorisation decisions or receives a signing/decryption key.
- Agent remains authenticated by mTLS and continues to use ADR-0003 durable
  contracts unless their owner explicitly adopts a versioned change.
- Providers and monitored databases remain outside the JOSE boundary.

Architecture tests must prove that Domain and Application do not reference
JOSE/IdentityModel or concrete cryptographic types. An assembly
`DBNotifier.Infrastructure.Security.Jose` is created only if dependency and
ownership analysis proves a distinct boundary; organisation alone is not
sufficient.

Ports exposed beyond the security boundary are semantic and artefact-specific.
The server, not the caller, selects the schema, payload representation,
headers, signers, recipients, profile and key binding. Authorisation, rate
limits and quotas precede cryptographic work. Detailed verification/decryption
errors remain internal; callers receive a stable uniform refusal.

No hand-written AES, RSA, ECDSA, EdDSA, ML-DSA, KDF, MAC or constant-time
primitive will be introduced. An approved library supplies primitives;
DB-Notifier owns policy enforcement, strict bounds, trust selection, auditing
and integration.

## Initial algorithm posture

The final allowlists remain subject to feasibility and homologation.

| Class | Proposed posture |
|---|---|
| JWS production candidate | `PS256` with RSA at least 2048 bits or `ES256` with P-256 and exact JOSE `R || S` encoding, per purpose and custodian |
| JWS inbound compatibility | `RS256` only for an exact external issuer/profile with a documented sunset |
| OKP | `Ed25519` remains scheduled until the complete .NET, IdP/custodian and cross-platform path is proved |
| AKP/post-quantum | `AKP` and `ML-DSA-44/65/87` are `Scheduled + RuntimeDisabled` pending use case, interoperability, parameter validation and size/cost budgets |
| Symmetric JWS | denied across independent trust boundaries; a same-owner MAC use requires a separate profile and key |
| Unsecured JWS | always deny `alg=none` |
| JWE content candidate | `A256GCM`, 96-bit IV and 128-bit tag |
| JWE key-management candidate | `RSA-OAEP-256` with RSA at least 2048 bits for test/homologation; no operational use is implied |
| ECDH-ES | scheduled until curve, point validation, ephemeral-key, KDF and recipient policies are exact |
| Other symmetric key management | `dir`, AES Key Wrap and PBES2 remain runtime-disabled pending a distinct use and custody decision |
| Legacy JWE | deny `RSA1_5` and CBC-HMAC content encryption in the baseline |
| Compression | deny `zip` in the baseline |

Each key version is bound to one algorithm, purpose, tenant/environment and
direction. A migration such as `RS256` to `PS256` does not silently reuse the
same RSA pair. Exact RSA modulus/exponent, PSS parameters, curves, ECDSA
encoding and invalid-key-test candidates are proposed in `JOSE-0`.
`JOSE-1`, if separately authorised, measures the candidates; only
`JOSE-D1` may freeze accepted values.

RFC 7638 thumbprints identify public key material; they do not prove
provenance, tenant, purpose or lifecycle version.

## Processing and header rules

- Reject duplicate JSON member names, malformed or padded base64url, excessive
  depth/cardinality and unknown or unprocessed critical headers before
  expensive work.
- Header names must be disjoint across protected, shared unprotected and
  per-signature/per-recipient buckets.
- JWS requires protected `alg`; every security-relevant shared field is
  protected. `b64=false` is always denied for JWT and remains test-only for a
  separate detached non-JWT profile.
- JWE requires `enc` in the protected header. When present, `zip`, `typ`,
  `cty` and other shared security properties are also protected. Compact and
  single-recipient profiles require protected `alg`.
- General JWE multi-recipient remains test-only in the baseline and uses one
  shared protected `alg`. A future heterogeneous profile must explicitly
  process per-recipient `alg`, `kid`, `epk`, `apu` and `apv` without allowing
  them to select trust.
- When recipient identity, cardinality or order has policy meaning, bind the
  roster through protected data, AAD or an inner signed envelope. One valid
  unwrap/decryption/authentication path does not prove that the original
  roster was preserved.
- A missing `kid` is accepted only when issuer, profile, algorithm, key type,
  use, operations and status leave exactly one eligible key. Ambiguity fails
  closed without trying an unbounded key set.
- Token-provided `jku`/`x5u` never triggers network access; embedded
  `jwk`/`x5c` never becomes a trust anchor.
- Detached content is verified over exact protocol bytes, never a parsed and
  reserialised equivalent.
- JWE releases plaintext only after successful authentication and exposes one
  indistinguishable external decryption failure.
- A wrapped-key profile creates a fresh CEK per JWE. The profile sets a
  per-key invocation/byte budget and a multi-instance IV strategy. Exhaustion
  fails closed. Counters require atomic reservation, fencing, restart and
  rollback evidence.
- Sign-then-encrypt uses separate inner and outer profiles.
- Replicated `iss`, `sub` or `aud` JWE headers are denied by default. An
  exception never drives pre-decryption routing/authorisation and must equal
  the validated inner claim.
- JWE does not conceal ciphertext length, protected headers or recipient
  metadata. Padding exists only under an approved bounded protocol.

## Human identity boundary

JOSE does not choose the Dashboard browser architecture or remove the current
password rules by inference. Before browser integration, MOD-01/MOD-09/MOD-11
must decide:

- canonical identity `(issuer, subject)`, issuer/tenant allowlists,
  provisioning/JIT, offboarding and collision handling;
- server-side mapping from claims to `User`, `RoleAssignment` and scope;
- strict access-token versus ID-token profiles; the API denies ID tokens;
- RFC 9068 `typ=at+jwt` when the chosen IdP supports that profile;
- BFF, token-mediating backend or memory-only SPA, with BFF evaluated first;
- Authorization Code + PKCE `S256`, exact redirects, `state`, `nonce`,
  mix-up, CORS, CSRF, cookie, refresh and logout rules;
- MFA and `acr`/`amr` requirements where risk requires them; and
- the factual limit of self-contained JWT revocation, with TTL,
  introspection, denylist, continuous access evaluation or reauthentication
  selected per profile.

The Dashboard never stores a token in `localStorage`, `sessionStorage`, URL,
log or telemetry and never derives RBAC from client-side token inspection.

## Key material and lifecycle

Signing, verification, encryption and decryption identities are separate by
environment, tenant where applicable, purpose and direction. Durable private
keys, KEKs, seeds and durable symmetric secrets:

- reside in an approved OS/corporate vault, KMS or HSM;
- are addressed by an opaque versioned reference;
- are non-exportable where supported;
- never appear in application configuration, ordinary persistence, logs,
  exceptions, telemetry, screenshots, reports, tests or chat output; and
- are not considered backed up by a DB-Notifier metadata backup.

Ephemeral CEKs, KDF outputs, plaintext and intermediate buffers may exist
briefly in authorised process memory because JWE requires them. Their
lifetime, size and ownership are bounded; they are never intentionally dumped,
logged, cached or persisted and are cleared/disposed on a best-effort basis
without claiming perfect zeroisation in managed memory. Publication in an RFC
does not make a private/symmetric component permissible project material.
Known private keys, seeds, KEKs, CEKs and symmetric secrets are not copied
into tests, the repository or evidence. Secret operations use unique per-run
keys and differential/interoperability checks and do not claim reproduction
of an exact private vector. A candidate that requires a durable known-secret
fixture remains blocked; this ADR creates no policy exception.

Permissions are distinct for public read/verify, sign, encrypt/wrap,
decrypt/unwrap, profile administration, rotate, disable, recover and audit
read. Ordinary runtime identities cannot administer lifecycle. Compromise and
destruction require dual control and durable canonical audit.

Signing rotation publishes/adopts a new verification key before the signer
switches, then retains the previous verify key only for a bounded overlap.
Encryption rotation installs the new private decryption key before producers
receive the public encryption key, then retains the previous decrypt key for
all authorised ciphertext, queues and backups. These are separate ceremonies.
Compromise recovery uses emergency denial, cache invalidation, quarantine,
reissuance and independent authority rather than ordinary rotation.

## JWKS, egress and infrastructure

Public JWKS processing has bounded bytes, cardinality, candidates, HTTP
headers and freshness. Private members are rejected. Key selection enforces
issuer/profile/algorithm/key type/use/operations/status and uniqueness.
Unknown-key refresh is single-flight, rate-limited and negatively cached; a
validated response replaces the last-known-good set atomically. LKG has an
explicit maximum age and never overrides an emergency denylist.

OIDC discovery does not grant arbitrary JWKS-origin authority. Any different
origin must be administratively preconfigured. Token-provided URLs and caller
input never create egress.

The current R-NET policy has no KMS/HSM/vault consumer. `JOSE-0` proposes the
non-registered candidates `human-identity-backchannel`,
`server-key-custody` and, only if required,
`server-workload-identity`; none changes the four current policy identifiers
or may silently reuse `human-identity`. Endpoint, CIDR, port, DNS, TLS
hostname, redirects, proxy, failover/private endpoint, credential bootstrap,
scopes, payloads, timeout, retries and telemetry must be fixed per topology
before any candidate can be promoted.

Cloud SDK transport must be injected through the admitted R-NET connector.
Ambient endpoint, proxy, credential-chain and telemetry fallback is denied.
The current metadata/link-local hard denial remains. Any IMDS requirement
needs a separate R-NET ADR/gate and cannot weaken the general SSRF boundary.

Infrastructure as code may create identities, policies and references, but it
must not export private material into state, plan, output, pipeline variables
or artefacts.

## Data and audit consequences

The authoritative profile registry is an immutable, reviewed, versioned
deployment snapshot with digest and monotonic revision. PostgreSQL cannot
create algorithms, trust sources or key locators. If persistence selects an
already admitted revision, activation requires server-side RBAC, optimistic
concurrency, atomic publication, monotonic fencing and append-only audit.
Unknown, divergent or lower revisions fail closed.

The effective state of a key version is the denial-dominant intersection of
its previously admitted profile binding, monotonic lifecycle
checkpoint/generation, emergency denylist and factual custodian state; the
most restrictive state wins. PostgreSQL cannot retarget a key reference,
reactivate a disabled/revoked version or reduce generation. Restore, missing
state, divergence or rollback must reconcile with the trusted snapshot and
custodian or fail closed before verify, sign, encrypt or decrypt.

Ordinary persistence may store approved non-secret metadata: purpose, profile
revision, sanitised opaque key ID, public thumbprint, lifecycle state and
activation/retirement times. Replay storage, when required, stores only a
purpose-bound digest and minimal issuer/audience/expiry/outcome with bounded
cardinality and retention.

Bearer tokens are never written to ordinary product persistence, logs or
audit. A server-side BFF token/session store may retain them only under the
separate browser-identity decision, with encryption, least privilege, TTL,
retention and no-backup rules. Plaintext, CEKs and unrestricted claims are
never persisted. JWS/JWE/ciphertext may be durable only when the owning use
case explicitly requires it and defines classification, store, TTL,
retention, backup, decryption-key retention, AI exclusion and
migration/rollback. Ciphertext remains sensitive and is never an audit/log
payload.

JOSE security events extend MOD-11 `AuditEntry`; they do not create a parallel
store. Audit and metrics never contain tokens, plaintext, ciphertext, tags,
private/symmetric keys, unrestricted claims, vault locator/ARN or raw
custodian responses. High-volume pre-Application refusals use bounded
aggregation/sampling to prevent audit flooding.

## Delivery and decision sequence

`JOSE-0` is documentary. Its review package now prepares provisional
profiles, pinned registry snapshots, conservative target limits and absolute
safety caps for a possible spike, ownership/trust/data/egress maps, traceable
requirements and threats while this ADR remains `proposed`. Preparing or
reviewing that package is not acceptance.

`JOSE-1` is a separately authorised test-only .NET 10 feasibility spike. It
must measure all six core serialisations, library behaviour and `N-1/N/N+1`
bounds,
interoperability, cross-platform behaviour, supply-chain evidence and SBOM
without custom cryptography.

Only after those outputs may `JOSE-D1`, an explicit architecture/security
decision checkpoint, change this ADR to `accepted`. `JOSE-D1` is not a
lifecycle Human Gate and does not authorise product code, external services,
keys, migrations, deployment or lifecycle progression. Each implementation,
use case, infrastructure integration, homologation and release remains
separately authorised.

## Alternatives

### Continue with JWT bearer validation only

This remains the safe runtime baseline. It does not meet the requested
conformance harness, JWE, JSON serialisation or product-owned key-lifecycle
objective.

### Enable every algorithm supported by a library

Rejected. Library capability changes with versions and is not a security,
authorisation or support policy.

### Build a custom JOSE implementation

Rejected. Custom primitives and ad hoc parsers create unacceptable assurance
and maintenance risk.

### Put JOSE contracts in Application

Rejected. It couples business use cases to transport/cryptographic formats and
conflicts with inward dependency rules.

### Use JOSE for every internal message

Rejected. mTLS, durable protocol semantics, package signatures, idempotency,
database controls and MOD-12 trust have distinct owners and guarantees.

## Acceptance conditions

This ADR may move from `proposed` to `accepted` only at `JOSE-D1`, when:

- the `JOSE-0` review confirms the prepared owner/use-case, standards,
  registry, trust, data, egress, requirement and threat maps without a
  critical unknown;
- the IANA JOSE and JWT Claims snapshots have no `Unreviewed` or otherwise
  unclassified entry;
- `JOSE-1` proves a safe .NET 10 path for all six core serialisations without
  custom cryptographic primitives;
- `JOSE-D1` reviews the measured evidence and freezes the accepted numeric
  limits; no product parser predates that decision;
- Domain/Application isolation and semantic, non-oracle ports are proved;
- algorithm/key, General JWE, CEK/IV, JWKS, browser identity, R-NET, profile
  authority, audit and lifecycle rules have security-owner approval;
- ADRs 0002/0003/0004/0005/0007 remain intact or have an owner-approved change
  plan for an explicitly selected use case;
- dependency licence, provenance, advisories and SBOM are reviewed;
- no Critical/High threat remains unresolved in the proposed boundary; and
- security and architecture owners explicitly record the decision,
  reservations and evidence.

Acceptance establishes architectural constraints only. It does not select or
homologate a real IdP/custodian, activate a runtime profile, authorise
implementation or permit a public support claim.
