# JOSE Security Profile and Key Lifecycle

## Status, authority and boundary

- Status: `JOSE-0 REVIEW CANDIDATE — NOT ACCEPTED`
- Document version: `jose-security-profile-0.1.0`
- Date: 2026-07-28
- Lifecycle position: `STATE-06 INTEGRATION`
- Decision record:
  [ADR-0008](ADR-0008-JOSE-Cryptographic-Profiles-And-Key-Lifecycle.md),
  revision `1.2`, status `proposed`
- Owners: security, identity, platform, data and affected-module architecture
- Runtime effect: none

This document is the technical design output of the authorised documentary
lot `JOSE-0`. It prepares, but does not accept, ADR-0008. Every profile,
algorithm posture, target and safety cap below is provisional input to a
separately authorised `JOSE-1` feasibility spike. Only the later `JOSE-D1`
architecture and security decision may accept the ADR and freeze measured
limits.

This document does not authorise source code, executable configuration,
dependencies, restore, migrations, a runtime, a real IdP or login, keys,
certificates, a vault, KMS, HSM, database or external-service integration,
deployment, publication, lifecycle progression, `JOSE-1`, a MOD-12 change or
MOD-12 activation. Accepted thematic authorities continue to prevail. In
particular, ADRs 0002, 0003, 0004, 0005 and 0007 are not changed by this
candidate.

The companion
[IANA registry coverage](JOSE-IANA-Registry-Coverage.md) is the standards
inventory. The
[JOSE-0 report](../STATE-06-JOSE-0-Architecture-Security-And-Coverage-Design-Report.md)
owns execution evidence, requirements traceability and the test, migration
and rollback plans.

## Observed baseline and exact gaps

The current Server API is an externally issued JWT relying party, not a
general JOSE capability.

| Observed fact | Evidence and consequence |
|---|---|
| Human authentication uses ASP.NET Core `JwtBearer` with exact configured issuer and audience, lifetime and signing-key validation, HTTPS metadata, a one-minute clock skew and no saved token | [`ServerNetworkSecurity.cs`](../../src/DBNotifier.Server.Api/Security/ServerNetworkSecurity.cs); this is one inbound Compact JWS/JWK path only |
| The current options do not declare `ValidAlgorithms`/`AlgorithmValidator` or `ValidTypes`/`TypeValidator` | No individual algorithm or `typ` is classified as implemented, interoperable or homologated by `JOSE-0`; exact allowlists are a future implementation target |
| The current actor resolver reads only `sub` and Application represents `HumanActor(string SubjectId)` | [`HumanIdentitySecurity.cs`](../../src/DBNotifier.Server.Api/Security/HumanIdentitySecurity.cs) and [`AuthorizedOperations.cs`](../../src/DBNotifier.Application/Access/AuthorizedOperations.cs); this is not the target identity tuple `(issuer, subject)` |
| Central persistence imposes global uniqueness on `users.subject_id` and has no issuer component | [`ServerDbContext.cs`](../../src/DBNotifier.Persistence.Server.PostgreSql/ServerDbContext.cs); a future change requires an explicit, fail-closed identity migration with no inferred issuer |
| R-NET has exactly four compiled consumer IDs; `human-identity` owns current OIDC discovery and JWKS retrieval | [Network egress policy](Network-Egress-Policy.md); no KMS, HSM, vault, STS, token, introspection or revocation endpoint is admitted |
| No product-owned JWS producer, JWE processor, General/Flattened JSON processor, detached-payload processor or operational key lifecycle exists | A dependency's incidental capability is not implementation or support |

The current subject-only identity remains safe only inside its current
single, exact configured issuer boundary. It must not be presented as a
multi-issuer identity model or used to infer a future issuer during
migration.

## Design invariants

1. Trusted composition selects a purpose and immutable profile revision
   before parsing an untrusted JOSE object.
2. Domain and Application do not own JOSE formats, algorithms, key
   descriptors, IdP, KMS or concrete cryptographic types.
3. Ports crossing the security boundary are semantic and artefact-specific.
   There is no public generic sign, encrypt, decrypt or verify oracle.
4. `alg`, `enc`, `typ`, `cty`, `kid`, `jku`, `jwk`, `x5u`, `x5c`, claims or
   payload content cannot select policy, trust, a key custodian or network
   egress.
5. Authentication and cryptographic integrity do not create business
   authorisation, RBAC, dual control, idempotency, audit approval or a Human
   Gate.
6. Durable or retained private keys, seeds, KEKs and symmetric secrets remain
   outside source, configuration, ordinary persistence, test fixtures,
   evidence, logs and chat. A separately authorised future test may generate
   unique ephemeral material in process memory for one run and must destroy it
   without serialising or retaining it. Public standards evidence does not
   waive that rule.
7. A profile revision is monotonic, immutable after admission and denial
   dominant. Unknown, divergent, stale or lower state fails closed.
8. Registry presence, roadmap, implementation, verification, homologation,
   runtime enablement, authority and public support remain independent facts.
9. All candidate limits use checked aggregate arithmetic and fail before
   expensive cryptographic work wherever framing permits.
10. A failed or incomplete operation releases no plaintext, favourable
    partial result, new trust state or authorising outcome.

## Provisional profile catalogue

Every profile below is `NotImplemented`, `NotTested`,
`NotHomologated`, `RuntimeDisabled` and `NotAdvertised`. Its only present
authority is the documentary `JOSE-0` authorisation.

| Profile ID | Purpose and direction | Candidate format | Candidate cryptographic posture | Trust and claims | Future owner/gate |
|---|---|---|---|---|---|
| `human-access-token-inbound.v0` | Validate one externally issued human API access token; inbound only | Compact JWS JWT only; no JWE or nesting | Exact issuer-bound `RS256` compatibility with sunset; `PS256` or `ES256` target only after IdP evidence | Exact issuer/audience; canonical `(issuer, subject)`; RFC 9068 mode requires `typ=at+jwt` or `application/at+jwt`, `iss`, `sub`, `aud`, `exp`, `iat`, `jti` and `client_id`; optional claims follow the coverage matrix and server-side mapping | MOD-01/MOD-11 and Server/API; `JOSE-2` plus `JOSE-5A`; ADR still requires `JOSE-D1` |
| `jws-core-conformance.v0` | Prove core signing serialisations without a product producer | Compact, Flattened JSON and General JSON; General covers multiple signatures | `PS256` and `ES256` candidates; `RS256` compatibility/negative coverage; ephemeral keys only | No network, identity, operational payload or durable key; exact protected-header policy | Security and QA; test-only `JOSE-1` |
| `jws-detached-conformance.v0` | Prove exact-byte detached JWS separately from JWT | Compact, Flattened and General detached payload; encoded payload and `b64=false` variants | Same candidate algorithms as core JWS | Non-JWT only; `b64=false` is forbidden for JWT; caller supplies exact protocol bytes and content type | Security and QA; test-only `JOSE-1` |
| `jwe-core-conformance.v0` | Prove core encryption serialisations, AAD and homogeneous multi-recipient processing | Compact, Flattened JSON and General JSON; one shared protected `alg`; external AAD where supported | `RSA-OAEP-256` plus `A256GCM`; fresh CEK and IV; ephemeral keys only; `zip` denied | Recipient roster is policy-bound; no operational or sensitive payload, persistence, network or custodian | Security and QA; test-only `JOSE-1` |
| `nested-jwt-conformance.v0` | Prove bounded sign-then-encrypt composition | Exactly one inner JWS JWT and one outer JWE layer | Separate inner JWS and outer JWE profile instances; no key reuse across purpose/direction | Explicit inner `typ`, outer `cty=JWT`, exact issuer/audience and no pre-decryption claim authority | Security and QA; test-only `JOSE-1` |

There is deliberately no generic product signing profile, operational
confidential-envelope profile, Agent profile, update profile or MOD-12
profile. Each would require a named use case, its owning ADR or module
authority and a separately authorised lot.

### Human access-token claim handling

The provisional human profile distinguishes claim acceptance from claim
authority:

- `iss`, `sub`, `aud`, `exp`, `iat` and, in RFC 9068 mode, `client_id` are
  validated against exact profile rules;
- `nbf`, when present, is validated with the same bounded clock policy;
- RFC 9068 `jti` is validated and bounded, but no global replay store is
  inferred; any replay digest is purpose-bound and separately designed;
- `scope`, `roles`, `groups` and `entitlements` are untrusted mapping inputs,
  never direct permissions;
- `name` is an optional display value, never an identity or audit key;
- ID-token/session claims are rejected by this access-token profile and
  belong only to a future mutually exclusive browser/session profile; and
- additional registered claims are dropped at the security/Application
  boundary unless a profile explicitly assigns semantics. `Rejected` in the
  global inventory does not automatically mean that an otherwise valid
  external token is rejected merely for carrying an unknown private claim;
  the exact token profile decides `RejectWholeObject` versus
  `IgnoreAndDropAtBoundary`.

## Provisional target limits and absolute safety caps

### Interpretation

The target is the expected representative value for `JOSE-1`; the hard cap
is the absolute admission ceiling for that test-only spike. `N-1`, `N` and
`N+1` tests use the hard cap as `N`. These values are engineering hypotheses,
not accepted runtime limits or SLOs. `JOSE-1` must measure them on .NET 10
Windows and Linux, and `JOSE-D1` may reduce, replace or reject them.

All byte counts are actual UTF-8 or decoded binary bytes, never
`Content-Length` alone. Aggregate counters use checked arithmetic. Exceeding
one dimension refuses the whole object with a stable, uniform outcome.

| Dimension | Target | Provisional hard cap | Scope and refusal |
|---|---:|---:|---|
| Encoded human access token | 8 KiB | 16 KiB | complete Compact JWS before parsing |
| Human decoded protected header | 512 B | 2 KiB | exactly one protected header |
| Human decoded claims | 4 KiB | 8 KiB | exactly one claims object |
| Encoded conformance object | 64 KiB | 128 KiB | complete JWS/JWE JSON or Compact object |
| One conformance header bucket | 2 KiB | 4 KiB | per protected or unprotected bucket |
| All conformance header buckets | 8 KiB | 16 KiB | all signatures/recipients combined |
| Conformance payload/plaintext/ciphertext | 16 KiB | 64 KiB | no partial payload release |
| External AAD | 4 KiB | 8 KiB | exact bytes, independently counted |
| JSON depth | 4 | 8 | complete object graph |
| Top-level JWT claims | 32 | 64 | nested structures remain inside depth/node/byte limits |
| JSON nodes in a JWT | 128 | 256 | properties and array elements combined |
| JSON nodes in one JWS/JWE | 128 | 512 | properties and array elements combined |
| Array items in one claim | 32 | 64 | no truncation |
| Members in one header bucket | 8 | 16 | duplicates are refused before counting as valid |
| JOSE nesting | 1 layer | 2 layers | only explicit sign-then-encrypt may use two |
| Signatures or recipients | 2 | 4 | General serialisation only; no favourable partial acceptance |
| `crit` names | 1 | 4 | distinct, recognised, profile-allowed and fully processed |
| One public JWK | 4 KiB/16 members | 8 KiB/32 members | private/symmetric members are refused in public trust |
| JWKS document | 64 KiB/16 keys | 256 KiB/64 keys | actual streamed bytes; at most 4,096 JSON nodes |
| Eligible key candidates | 1 | 1 | ambiguity fails before cryptography; there is no "try all" fallback |
| JWS cryptographic operations | 2 | 4 | one per required General signature |
| JWE cryptographic operations | one AEAD plus two wraps, or one unwrap plus one AEAD | one AEAD plus four wraps, or one unwrap plus one AEAD | consumption selects one intended recipient by trusted context |
| Human local processing | p95 at most 10 ms | 100 ms cooperative deadline | bytes/cardinality/operations remain the enforceable caps |
| JWS/JWE local processing | p95 at most 100 ms | 1 second cooperative deadline | ML-DSA is measured separately with p95 target 250 ms/signature and 2-second object deadline |
| JWKS parse/selection | p95 at most 25 ms | 250 ms cooperative deadline | excludes network retrieval |
| Human accounted transient memory | 128 KiB | 512 KiB | input, decode, JSON and crypto-adapter buffers |
| JWS/JWE accounted transient memory | 2 MiB | 8 MiB | per complete admitted JOSE object, including all signatures, recipients and nested layers; reserve before processing and retain until quiescence |
| JWKS accounted transient memory | 2 MiB | 8 MiB | per issuer/profile parse |
| Concurrency and queue in first proof | 1 active | 1 active, zero queued | no fairness, throughput or fleet-wide capacity claim |

Field-specific caps override the broader table:

| Field | Provisional rule |
|---|---|
| Header/claim name, `alg`, `enc`, `typ`, `cty` | 64 ASCII bytes; registered spelling and case rules remain exact |
| `kid` | 128 UTF-8 bytes; opaque equality only, never a path, URI, query or log label |
| `iss` | one exact configured HTTPS issuer, at most 2,048 UTF-8 bytes and still inside the aggregate claims cap, with no token-driven egress |
| `sub` | non-empty, at most 300 UTF-8 bytes and still inside the aggregate claims cap, interpreted only with the validated issuer |
| `aud` | the exact configured audience for the human profile, at most 300 UTF-8 bytes and still inside the aggregate claims cap; future multi-audience profiles need a separate cap and rule |
| Arbitrary string admitted by a profile | 2 KiB UTF-8, still inside the aggregate object cap |
| Base64url segment | unpadded canonical form only; invalid alphabet, padding, non-zero trailing bits or decoded overflow is refused |
| PBES2 `p2c` | parsed only for bounded recognition/negative coverage, maximum `10,000`; PBES2 execution remains `NotPlanned + RuntimeDisabled` |
| HTTP response headers for a future JWKS fetch | 32 KiB aggregate; redirects, ambient proxy, cookies, ambient credentials and decompression remain disabled |

The human profile also carries provisional temporal hypotheses: a 15-minute
target and 60-minute maximum token lifetime, with a 60-second clock-skew
target and provisional hard cap matching the current one-minute baseline.
RFC 9068 requires only a small allowance for clock skew and does not define
these numbers. The exact IdP, session, offboarding and revocation design may
require stricter values. Any increase beyond the current skew requires a
separate identity/security decision and measured evidence; it is not part of
an equivalent migration. A token beyond the eventual profile lifetime is
refused even when its signature is valid.

A deadline is not a cancellation guarantee. The directly enforceable
boundaries are bytes, nodes, candidates, operations and reserved accounted
memory. Work that has crossed into a
non-cancellable primitive retains its reservation until quiescence or an
independently authorised termination fence. The first spike is serial and
has no queue so it cannot claim fairness, starvation freedom or reusable
capacity after an arbitrary timeout.

### Candidate cryptographic parameters

| Candidate | Provisional exact parameters | Status |
|---|---|---|
| `RS256` | RSA modulus exactly 2,048, 3,072 or 4,096 bits; exponent `65537`; PKCS#1 v1.5 verification only; exact issuer-bound inbound compatibility and sunset | `SafelyAdapted`, future only |
| `PS256` | RSA modulus exactly 2,048, 3,072 or 4,096 bits, with 3,072 as measurement target; exponent `65537`; SHA-256; MGF1 SHA-256; salt 32 bytes; standard trailer | `Adopted` candidate |
| `ES256` | NIST P-256; SHA-256; validated public point; fixed JOSE `R || S` signature of 64 bytes; reject DER input and out-of-range values | `Adopted` candidate |
| `RSA-OAEP-256` | RSA modulus exactly 2,048, 3,072 or 4,096 bits; exponent `65537`; SHA-256; MGF1 SHA-256; empty label | test-only `Adopted` candidate |
| `A256GCM` | 32-byte CEK; 12-byte IV; 16-byte authentication tag; one content-encryption operation and at most the 64 KiB profile plaintext hard cap per fresh CEK | test-only `Adopted` candidate |
| `Ed25519` | fully specified RFC 9864 identifier; exact 32-byte public key and 64-byte signature | feasibility only; `SafelyAdapted + RuntimeDisabled` |
| `ML-DSA-44` | exact 1,312-byte public key and 2,420-byte signature | feasibility only; `SafelyAdapted + RuntimeDisabled` |
| `ML-DSA-65` | exact 1,952-byte public key and 3,309-byte signature | feasibility only; `SafelyAdapted + RuntimeDisabled` |
| `ML-DSA-87` | exact 2,592-byte public key and 4,627-byte signature | feasibility only; `SafelyAdapted + RuntimeDisabled` |

For RFC 9964 `AKP`, `alg` and `pub` are required, parameters must match and a
public JWK containing `priv` is refused. The 32-byte ML-DSA seed is private
material and is never retained in project fixtures or evidence. Feasibility
uses a unique ephemeral key per run without claiming reproduction of a
published private vector.

`none`, `RSA1_5`, SHA-1 signatures/thumbprints as trust, CBC-HMAC content
encryption, bare CBC/CTR WebCrypto entries, `secp256k1` and `zip` are denied
by the baseline. Symmetric MAC, direct encryption, AES Key Wrap, AES-GCM Key
Wrap, PBES2 and ECDH-ES variants are not globally unsafe by registry
presence, but remain disabled unless a distinct purpose, custody model and
measured profile is later approved.

## Processing and selection contract

### Admission order

1. Trusted composition supplies the exact purpose and profile revision.
2. A bounded reader counts actual bytes and validates the expected
   serialisation shape without choosing policy from content.
3. Strict UTF-8/JSON and base64url processing rejects duplicates, malformed
   values, excess depth/cardinality and unknown critical extensions.
4. Protected and unprotected header buckets are proved disjoint. Every
   policy-relevant field is protected.
5. The profile validates format, `typ`/`cty`, algorithm, key type, curve,
   size, operations, claims and nesting before key attempts.
6. Trust resolution operates only inside the preconfigured issuer/profile
   trust source. `kid` is a bounded hint and ambiguity fails closed.
7. Cryptographic verification or authenticated decryption occurs within the
   admitted work reservation.
8. Claims or plaintext are released only after complete cryptographic and
   profile validation.
9. Application performs business authorisation, RBAC, replay/idempotency and
   the canonical state change.
10. A sanitised security outcome and canonical MOD-11 audit intent are
    emitted without the protected artefact or unrestricted claims.

Flattened and General JSON processing rejects a name repeated across
protected, shared-unprotected and per-signature/per-recipient headers.
General JWS requires the configured signer set or quorum, not "any one valid
signature". General JWE requires the configured recipient roster. A valid
unwrap path does not prove that an attacker preserved roster cardinality or
identity.

Detached JWS verifies the exact externally supplied bytes. JSON parse and
re-serialisation never creates an equivalent payload. Nested JWT is
sign-then-encrypt, uses separate keys and profiles and validates the inner
token only after outer authentication.

### Public-key admission and JWKS

- JWK/JWKS is public security metadata but remains untrusted input.
- A public set containing `d`, `p`, `q`, `dp`, `dq`, `qi`, `oth`, `k`,
  `priv` or another profile-defined private member is refused as a whole.
- `use`, `key_ops`, `alg`, `kty`, curve/size, issuer/profile binding,
  lifecycle status and emergency denial must be mutually consistent.
- RFC 7638 thumbprint identifies public key material only. It does not prove
  provenance, issuer, tenant, purpose or key lifecycle version.
- `kid` must be unique inside the eligible active trust set. Absence is
  accepted only when all other trusted selectors leave exactly one key.
- Token-provided `jku`/`x5u` does not initiate a fetch. Embedded `jwk`,
  `x5c`, `trust_chain` or `peer_trust_chain` does not establish trust.
- A future unknown-`kid` refresh is at most one coalesced, rate-limited
  attempt per issuer/profile generation. A fully validated candidate
  atomically replaces the last-known-good set; a failure preserves it only
  within an accepted freshness window and never overrides emergency denial.

The following cache/refresh values are future fixture targets only. They do
not change the current `human-identity` implementation:

| Dimension | Target | Provisional hard cap |
|---|---:|---:|
| Normal refresh/fresh age | refresh every 15 minutes | fresh age at most 60 minutes; origin may shorten, never lengthen |
| Total fetch deadline | 5 seconds | 10 seconds including the bounded body; no redirect or immediate retry |
| Response headers | 16 KiB | 32 KiB |
| Unknown-`kid` refresh | 5-minute cooldown | at most one per request and one per issuer/profile per 60 seconds, single-flight |
| Negative cache | 5-minute TTL and 128 digests | 15-minute TTL and 512 digests; never retain raw attacker `kid` |
| Failure backoff | 1 minute | exponential up to 15 minutes; one attempt per cycle |
| Sets retained | current only | current plus one LKG |
| Stale LKG | disabled | only an explicitly approved profile: target 15 minutes, maximum 60 minutes; emergency denial always wins |
| Preconfigured issuer/profile caches | 4 | 16; never attacker-keyed |
| Globally accounted cache memory | 16 MiB | 64 MiB |

No JWKS refresh, cache, publisher or key store is added by `JOSE-0`.

## Ownership map

| Concern | Accountable owner | Enforcement owner | Required evidence/reviewer |
|---|---|---|---|
| Standards snapshot and per-entry classification | Security Architecture | JOSE profile registry owner | Security standards review and coverage check |
| Purpose, profile and algorithm policy | Security + affected-module Architecture | trusted Server/API composition and security boundary | `JOSE-D1`; no module owner is bypassed |
| Human identity, provisioning, offboarding, session and RBAC | MOD-01/MOD-11 | Server/BFF and central authorisation stores | Identity, privacy and security review |
| Browser flow and accessible session UX | MOD-09 with MOD-01/MOD-11 | BFF/Dashboard boundaries | browser security and accessibility evidence |
| Current IdP discovery/JWKS trust | Security + Network Security | existing `human-identity` R-NET consumer | exact issuer/topology evidence |
| Future public trust/key operations and custody | Platform/SRE + Security | separately approved trust resolver and key-operation adapter | custodian/topology homologation; not part of `JOSE-0` |
| Network destination admission | Network Security/R-NET owner | pinned egress connector | DNS/TLS/redirect/proxy/credential negative evidence |
| Data classification, retention, restore and migration | Data Architecture + data owner | owning store and migration boundary | schema/retention/backup/rollback review |
| Canonical audit and separation of duty | MOD-11 | server-side RBAC and append-only audit | security/audit review and canary scan |
| Conformance, negative, fuzz and interoperability evidence | QA/SDET + Security | isolated test harness | independent security review |
| Agent, durable protocol, update or MOD-12 use | respective ADR/module owner | respective boundary | separate ADR/change plan and authority |
| ADR acceptance and numeric freeze | Bruno with architecture and security owners | `JOSE-D1` decision record | `JOSE-0` + separately authorised `JOSE-1` evidence |

An accountable owner cannot delegate a security decision to an IANA row, a
library, a token, PostgreSQL, a cloud SDK or a key custodian.

## Trust map

```text
Official RFC/IANA sources
        |
        | public standards input; no runtime trust
        v
Versioned JOSE-0 snapshot ----> security standards review
                                      |
                                      | proposed policy only
                                      v
Trusted route/use case ----> immutable profile revision
                                      |
Untrusted JOSE object --------------> bounded JOSE boundary
                                      |          |
                                      |          +--> preconfigured public trust
                                      |               (exact issuer/profile only)
                                      |
                                      +--> opaque key-operation reference
                                           (future approved custodian only)
                                      |
                                      v
Sanitised outcome ----> Application authorisation/RBAC/idempotency
                                      |
                                      v
Canonical state and MOD-11 audit intent
```

Trust is not circular:

- the object cannot supply the profile or root that validates it;
- IANA defines names, not trusted keys or runtime policy;
- an external IdP can authenticate its exact token profile but cannot grant a
  DB-Notifier permission without server-side mapping;
- PostgreSQL may later select only a previously admitted profile revision; it
  cannot create or weaken one;
- a custodian may report key state or perform an allowed operation, but it
  cannot change DB-Notifier purpose, tenant or business authority; and
- MOD-12 trust roots, checkpoints, policy approvals and activation remain
  governed solely by ADR-0007 and their own future authority.

## Data map

No schema, migration, row, cache or store is created by this design.

| Artefact | Classification | Candidate handling | Prohibited handling | Owner and lifecycle |
|---|---|---|---|---|
| Access, ID or refresh token | secret bearer/replayable | memory only, or a separately approved encrypted BFF session/token store for the minimum TTL | ordinary PostgreSQL/SQLite, backup, log, audit payload, URL, browser Web Storage or AI | Identity/session owner; separate browser decision |
| STS or workload-identity credential/token | secret bearer/delegated identity | bounded process memory for the minimum TTL, exact audience/scope and one separately admitted custodian flow | source/configuration/state/output, ordinary persistence, backup, log, audit payload, browser, AI or reuse as human/Agent identity | Platform identity owner; separate identity/R-NET decision |
| Public JWK/JWKS | public but integrity-critical | bounded cache keyed by issuer/profile/revision and freshness | private members, trust inferred from object, unrestricted LKG or AI | Security/trust owner; rebuild/reconcile |
| Immutable profile snapshot | restricted security metadata | signed/reviewed deployment artefact with digest and monotonic revision after future acceptance | caller-created profile, algorithm from database, downgrade or silent mutation | Security + platform; restore must reconcile |
| JWS or signed artefact | inherits payload classification | only the exact use-case store and retention if that case requires durability | generic archive, unrestricted audit/log or cross-purpose replay | owning module/data owner |
| JWE ciphertext | sensitive; leaks size/header/recipient metadata | only an approved use-case store with TTL and retained decrypt-key dependency | log, audit payload, generic cache or AI | owning data owner; backup and key retirement coupled |
| Plaintext, CEK or KDF output | transient secret | bounded authorised process memory for the minimum operation lifetime | persistence, cache, intentional dump, backup, log, audit or AI | security boundary |
| Opaque key/profile reference and public thumbprint | restricted security metadata | future approved store with RBAC, generation and reconciliation | private material, mutable retargeting or locator/ARN in audit | Platform/data/security |
| Replay evidence | restricted security metadata | purpose-bound digest plus minimum issuer/audience/expiry/outcome and bounded TTL/cardinality | raw token, raw `jti`, payload or cross-profile reuse | use-case/Application and data owner |
| Security event | internal/restricted | canonical MOD-11 `AuditEntry` with stable code and sanitised IDs | token, claims, plaintext, ciphertext, tag, key or raw custodian response | MOD-11 |
| IANA snapshot | public standards evidence | immutable repository evidence plus hashes and acquisition metadata | runtime policy lookup or automatic enablement | Security standards owner |

### Current identity data incompatibility

The proposed canonical external identity is `(issuer, subject)`, while the
current Application and central store use only `subject`. A future migration
must preserve stable `user_id` references for users, roles and other existing
relational owners, introduce an explicit issuer dimension and unique
composite identity, and require an administratively reviewed mapping for
every existing user. It must not concatenate values into an ambiguous string
or backfill issuer from the currently configured authority by inference.

Current audit entries use a textual subject-derived `ActorId` without a user
foreign key, as shown by
[`AuthorizedOperationsStore.cs`](../../src/DBNotifier.Persistence.Server.PostgreSql/AuthorizedOperationsStore.cs)
and
[`ServerDbContext.cs`](../../src/DBNotifier.Persistence.Server.PostgreSql/ServerDbContext.cs).
They remain append-only historical evidence and cannot be rewritten or
assigned an inferred issuer. A future audit identity format needs explicit
versioning and must preserve the distinction between legacy subject-only
evidence and new issuer-bound events.

An active legacy user without a proved issuer is refused fail-closed during
cutover. Introducing a distinct quarantine state would require its own data
decision and migration. Schema rollback is blocked once two issuers can carry
the same subject or new rows depend on the composite form. The detailed
future plan is in the JOSE-0 report; no migration is authorised here.

## Egress and infrastructure map

### Current and candidate consumers

| Consumer ID | Purpose | Status in `JOSE-0` | Mandatory boundary before any future registration |
|---|---|---|---|
| `human-identity` | Existing OIDC discovery and JWKS retrieval for the exact configured authority | existing R-NET consumer; unchanged | remains discovery/JWKS only; no token, introspection, revocation, KMS or STS traffic |
| `human-identity-backchannel` | Future BFF token exchange, introspection or revocation if selected | candidate only; not compiled, configured or authorised | exact origins/routes, secret-bearing request controls, credential scope, no redirects and a separate outage/retry policy |
| `server-key-custody` | Future vault/KMS/HSM metadata and data-plane key operations | candidate only; not compiled, configured or authorised | exact vendor/topology/endpoints, private link/failover, payload, quotas, TLS, injected transport and opaque key reference |
| `server-workload-identity` | Future STS/workload-identity bootstrap only when indispensable | candidate only; not compiled, configured or authorised | exact trust exchange, subject/audience/scopes, endpoint and expiry; no ambient credential chain or implicit metadata access |

For every future topology, Network Security must freeze the exact consumer,
origin/hostname, CIDRs, ports, DNS behaviour, TLS identity, private
endpoint/failover, payload maximum, timeout, retries, rate/quota, credential
bootstrap, scopes, telemetry, owner and outage mode. A cloud SDK is admissible
only if its HTTP transport is injected through the R-NET pinned connector and
ambient endpoint, redirect, proxy, credential-chain and telemetry fallbacks
are disabled or proved absent.

Link-local and metadata addresses remain hard-denied. An IMDS need would
require its own R-NET ADR and gate and cannot weaken the general SSRF
boundary. A token, header, claim, key metadata, tenant, region, caller or
database row never creates a destination.

No infrastructure-as-code, identity, policy, endpoint, vault, KMS, HSM, STS
or service is selected or created by `JOSE-0`.

## Key ownership and lifecycle model

The following identities remain materially distinct:

- external IdP signing key;
- future DB-Notifier artefact-signing key;
- future DB-Notifier decryption key;
- platform package/update signing key under ADR-0005;
- Agent mTLS identity under ADRs 0002/0003;
- MOD-12 role keys and roots under ADR-0007; and
- human, workload, profile administrator, rotation, recovery and audit
  principals.

The minimum binding is:

```text
key-version
  -> exact algorithm and parameters
  -> one purpose
  -> tenant/environment scope
  -> direction and allowed operations
  -> custodian and opaque reference
  -> monotonic lifecycle generation
  -> active/verify-only/decrypt-only/disabled/revoked/destroyed state
```

The effective state is the denial-dominant intersection of the admitted
profile, monotonic checkpoint, emergency denylist and factual custodian
state. A database cannot retarget a reference, reduce a generation or
reactivate disabled/revoked material.

### Signing rotation

1. Create a new version for the same exact purpose under separate authority.
2. Admit and distribute its public verification material.
3. Prove all intended verifiers recognise the new version.
4. Switch the signer to the new version.
5. Retain the previous version as verify-only for the bounded maximum
   artefact/queue/cache lifetime.
6. Retire it, prove refusal outside the overlap and close canonical audit.

### Encryption rotation

1. Admit the new private decryption version at the exact recipient.
2. Publish/admit its public encryption or key-management material.
3. Switch new envelopes to the new version.
4. Retain the previous version for decrypt-only while any authorised
   ciphertext, queue or backup depends on it.
5. Re-encrypt, expire or retain data under the owning data policy.
6. Retire/destroy only after dependency and recovery evidence is complete.

Compromise is not normal rotation. It invokes emergency denial, cache
invalidation, affected-scope quarantine, reissuance, independent approval and
durable audit. A compromised signer or ordinary runtime identity cannot
authorise its own recovery.

No key, custodian, lifecycle store or ceremony is instantiated by this
document.

## Stable outcomes and observability boundary

Future external failures are deliberately coarse, for example:

- `jose.object_invalid`;
- `jose.profile_unavailable`;
- `jose.algorithm_denied`;
- `jose.key_unavailable`;
- `jose.trust_unavailable`;
- `jose.authentication_failed`;
- `jose.resource_limit`;
- `jose.replay_denied`; and
- `jose.operation_unavailable`.

External responses do not distinguish padding, tag, unwrap, candidate key,
claim or signature detail in a way that creates an oracle. Internal bounded
metrics may include purpose/profile revision, permitted algorithm,
serialisation, stable outcome, duration, cache age and correlation ID.
Attacker-controlled `kid`, claims, issuer text, key locator/ARN, token,
payload, plaintext, ciphertext, tag, private/symmetric material and raw
custodian responses are never metric labels, logs or audit payload.

## Compatibility boundary

- Human JWT relying-party behaviour is characterised before any future
  profile switch.
- The proposed profile is introduced disabled and compared against the
  current exact issuer/audience path before it can replace it.
- A profile major version, token type, schema, algorithm and key lifecycle
  generation change independently and never downgrade automatically.
- Agent/API durable messages, package/update signatures and MOD-12 artefacts
  remain byte-for-byte and semantically unchanged unless their own owner
  approves a versioned change.
- A feature flag or disabled profile is not rollback authority for a schema,
  protocol or key lifecycle. Each layer has its own compatibility proof.

## Conditions before ADR acceptance

This candidate is not an ADR acceptance. Before `JOSE-D1`, all of the
following remain mandatory:

1. `JOSE-0` documentary checks and independent review complete with zero
   unclassified registry entries and no critical design unknown.
2. A separately authorised `JOSE-1` measures all provisional caps at
   `N-1/N/N+1`, all five profiles and all six core JWS/JWE serialisations.
3. Library capability, licence, provenance, advisories, .NET 10
   cross-platform behaviour, SBOM and lack of custom primitives are proved.
4. Interoperability, strict parsing, negative/tamper, resource and
   non-oracle behaviour pass.
5. Architecture and security owners review the exact measured profile,
   identity migration, trust, data, egress and lifecycle consequences.
6. `JOSE-D1` explicitly records accepted/rejected values and any
   reservations. It does not grant product implementation, infrastructure,
   lifecycle progression or public support.
