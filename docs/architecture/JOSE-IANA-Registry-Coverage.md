# JOSE and JWT IANA Registry Coverage

## Status and authority

- Status: `JOSE-0 PROVISIONAL CLASSIFICATION — NOT RUNTIME POLICY`
- Version: `jose-iana-coverage-0.1.0`
- Capture date in project timezone: 2026-07-28
- Acquisition date in UTC: 2026-07-29
- Lifecycle: `STATE-06 INTEGRATION`
- ADR: [ADR-0008](ADR-0008-JOSE-Cryptographic-Profiles-And-Key-Lifecycle.md)
  revision `1.2`, status `proposed`

This inventory classifies every row in the pinned IANA JOSE registries and
the IANA JWT Claims registry. It is a documentary standards baseline, not an
algorithm allowlist, library capability claim, implementation result,
homologation result, runtime switch, authorisation or public support claim.

The exact acquired CSV bodies are preserved, without private or secret
material, in the reversible Base64-encoded ZIP evidence
[`iana-jose-jwt-snapshots-2026-07-28.zip.b64`](evidence/jose-0/iana-jose-jwt-snapshots-2026-07-28.zip.b64).
Its [manifest](evidence/jose-0/README.md) records reconstruction and
verification. Runtime never reads this evidence or the live IANA registries
to choose policy.

## Snapshot manifest

### JOSE registry

- Registry page: <https://www.iana.org/assignments/jose/jose.xhtml>
- Canonical XML checked for registry metadata:
  <https://www.iana.org/assignments/jose/jose.xml>
- `Created`: 2015-01-23
- `Last Updated`: 2026-05-22
- XML acquisition: `2026-07-29T00:03:23.6996969Z`
- XML bytes: `58,073`
- XML SHA-256:
  `989bf4152905fd28051886f36a3b4a54c99bba6f6a97e2ae13213e817488b859`
- XML `Last-Modified`: `2026-05-22T17:01:21Z`
- CSV acquisition window:
  `2026-07-29T00:08:29.4868404Z`–`2026-07-29T00:08:34.3478968Z`

| Preserved CSV | Rows | Bytes | SHA-256 |
|---|---:|---:|---|
| `web-signature-encryption-header-parameters.csv` | 44 | 3,394 | `e66121e940ab248a4a60c58534c15f7d92f86e4974b5812af4928f053d99e617` |
| `web-signature-encryption-algorithms.csv` | 53 | 6,027 | `5a09ebf769cc2b3ccbec86a6d5a6c2be441c371cc0c136626463727884c0b7fc` |
| `web-encryption-compression-algorithms.csv` | 1 | 121 | `2cdae9003156eedf57c47aa3bc73b0ad94fd0a441185e2f8136abd9f01f7bd90` |
| `web-key-types.csv` | 5 | 401 | `6511cf891ad75c1845a26c495ca9beee0869033490c1b3f72e96127a45e12814` |
| `web-key-elliptic-curve.csv` | 8 | 678 | `8505343ad98e91e449ea3d7ec4728fbb9c3e28a8d433f05f72c189e2659f7251` |
| `web-key-parameters.csv` | 33 | 2,589 | `2ce0708246fe33ebc5515faa3bf7017f97c52c6f82232769a3738b83e685a73a` |
| `web-key-use.csv` | 2 | 172 | `5be9a2ab909a39a13b3672af90d9031ce949885aaec5a965ef9198158b44522d` |
| `web-key-operations.csv` | 8 | 656 | `95dd71016fbb4669758940b194f691bf4cbcf52c25a2c474a4961377a0a0e80a` |
| `web-key-set-parameters.csv` | 1 | 124 | `5b6ca5109e444f380d47cb2bc28f67bc0c6100049e116d05580dc256d029d764` |
| **JOSE total** | **155** | **14,162** | component hashes above |

The XHTML representation was also acquired at
`2026-07-29T00:03:24.0202257Z`: `69,567` bytes, SHA-256
`c3e5f09d71d1bcfe1466008aa58c39e2c79e484891ce159792354a4301dbd09f`.
It is auxiliary presentation evidence and is not retained because the exact
CSV components preserve every classified row.

### JWT Claims registry

- Registry page: <https://www.iana.org/assignments/jwt/jwt.xhtml>
- Claims CSV:
  <https://www.iana.org/assignments/jwt/claims.csv>
- Metadata XML:
  <https://www.iana.org/assignments/jwt/jwt.xml>
- `Created`: 2015-01-23
- `Last Updated`: 2026-07-20
- Claims CSV acquisition: `2026-07-29T00:08:34.9649652Z`
- Claims CSV `Last-Modified`: `2026-07-20T20:16:22Z`
- Claims CSV bytes: `18,690`
- Claims CSV SHA-256:
  `a87f3c08c02b671823e937c7880d29c0cb8c7a5fd518063ab1dec594660f799b`
- Claims rows: `163`
- Metadata XML acquisition: `2026-07-29T00:03:44.313Z`
- Metadata XML bytes: `60,389`
- Metadata XML SHA-256:
  `194f272197c18b92281e9bb09903dcb7346e14b99ed100352530858ce42ee0dc`

The JWT XML contains three registries. Only the exact `claims.csv` body is
retained in the evidence archive and only JSON Web Token Claims enter this
document's denominator. Confirmation Methods and Status Mechanisms are
outside the authorised snapshot target.

### Capture method and reproducibility

Each official HTTPS response was read entirely into memory. SHA-256 was
calculated over the exact response body bytes before any decoding or
line-ending conversion. The same bodies were fetched a second time, their
hashes matched, and they were placed unchanged into one deterministic ZIP.
No live registry response, executable policy, dependency or runtime cache was
created.

The ZIP/DOS entry calendar dates equal each registry's `Last Updated` date
and their wall-clock fields are `00:00:00`. ZIP/DOS stores no UTC offset, so
API projections may display a local offset; the fields are not suffixed with
`Z` and are not asserted HTTP timestamps. Reproducibility is established by
the exact archive and entry hashes, not by a timezone projection.

## Classification model

### Independent axes

Every row below has its own IANA fact, DB-Notifier decision, roadmap and
handling. Independently of those values, all 318 rows currently have:

| Axis | Value for every row in this `JOSE-0` snapshot |
|---|---|
| Implementation | `NotImplemented` as a governed JOSE profile |
| Verification | `NotTested` per registry row |
| Homologation | `NotHomologated` |
| Runtime | `RuntimeDisabled` |
| Authorisation | `JOSE-0-documentary-only` |
| Public support | `NotAdvertised` |

The current aggregate JWT relying-party path does not change a row to
`Implemented`: it lacks the proposed exact algorithm/type profile and does
not prove every claim or registry feature individually.

### Decision, roadmap and handling

- `Adopted`: candidate for an exact provisional profile.
- `SafelyAdapted`: recognised only in a named constrained inbound, test-only
  or purpose-specific form; it requires an explicit roadmap, owner and sunset
  or profile decision and never pairs with `NotPlanned`.
- `Rejected`: no semantics or authority in the baseline. It remains
  recognised for bounded refusal/drop coverage.
- `Unreviewed`: fail-closed state for any future or changed entry. There are
  zero in this snapshot.
- `NotPlanned`: no implementation lot.
- `Scheduled(JOSE-x)`: roadmap only; the cited lot remains separately
  authorised and may reject the item.
- `ValidateAndMap`: validate under the exact profile and expose only a
  canonical bounded value.
- `ProtocolOnly`: process only inside the named protocol and do not expose it
  as generic identity/authorisation data.
- `IgnoreAndDropAtBoundary`: grant no meaning and do not propagate, persist,
  log or audit the value. The exact token profile separately decides whether
  its presence makes the whole token invalid.
- `RejectWholeObject`: the baseline always rejects an object selecting the
  item.

`NS` below means the IANA registry has no implementation-requirement field
for that row. `Public`/`Private` in the JWK table is the IANA information
class, not a DB-Notifier data-classification decision. Each row inherits its
exact description, change controller and reference from the preserved CSV;
the composite matrix key prevents same-name rows from collapsing.

## JOSE registry classification

### JSON Web Signature and Encryption Header Parameters — 44/44

| Composite row key | IANA | Decision | Roadmap | Handling/reason |
|---|---|---|---|---|
| `alg@JWS` | NS | Adopted | `Scheduled(JOSE-1/2)` | Validate exact profile allowlist |
| `jku@JWS` | NS | Rejected | NotPlanned | Ignore/drop; never create egress |
| `jwk@JWS` | NS | Rejected | NotPlanned | Ignore/drop; embedded key is not trust |
| `kid@JWS` | NS | Adopted | `Scheduled(JOSE-1/2/3)` | Bounded opaque hint; one eligible key |
| `x5u@JWS` | NS | Rejected | NotPlanned | Ignore/drop; never create egress |
| `x5c@JWS` | NS | SafelyAdapted | `Scheduled(JOSE-3)` | Exact preconfigured trust profile only |
| `x5t@JWS` | NS | Rejected | NotPlanned | SHA-1 thumbprint is not admitted trust |
| `x5t#S256@JWS` | NS | SafelyAdapted | `Scheduled(JOSE-3)` | Additional binding only |
| `typ@JWS` | NS | Adopted | `Scheduled(JOSE-1/2/5)` | Exact mutually exclusive token type |
| `cty@JWS` | NS | SafelyAdapted | `Scheduled(JOSE-1/2)` | Exact allowlist for nested/typed content |
| `crit@JWS` | NS | Adopted | `Scheduled(JOSE-1/2)` | All names recognised and processed |
| `alg@JWE` | NS | Adopted | `Scheduled(JOSE-1/4)` | Validate exact profile allowlist |
| `enc@JWE` | NS | Adopted | `Scheduled(JOSE-1/4)` | Validate protected exact allowlist |
| `zip@JWE` | NS | Rejected | NotPlanned | RejectWholeObject; compression denied |
| `jku@JWE` | NS | Rejected | NotPlanned | Ignore/drop; never create egress |
| `jwk@JWE` | NS | Rejected | NotPlanned | Ignore/drop; embedded key is not trust |
| `kid@JWE` | NS | Adopted | `Scheduled(JOSE-1/3/4)` | Bounded opaque hint; one eligible key |
| `x5u@JWE` | NS | Rejected | NotPlanned | Ignore/drop; never create egress |
| `x5c@JWE` | NS | SafelyAdapted | `Scheduled(JOSE-3/4)` | Exact preconfigured trust profile only |
| `x5t@JWE` | NS | Rejected | NotPlanned | SHA-1 thumbprint is not admitted trust |
| `x5t#S256@JWE` | NS | SafelyAdapted | `Scheduled(JOSE-3/4)` | Additional binding only |
| `typ@JWE` | NS | Adopted | `Scheduled(JOSE-1/4/5)` | Exact outer object type |
| `cty@JWE` | NS | SafelyAdapted | `Scheduled(JOSE-1/4)` | Exact nested content allowlist |
| `crit@JWE` | NS | Adopted | `Scheduled(JOSE-1/4)` | All names recognised and processed |
| `epk@JWE` | NS | SafelyAdapted | `Scheduled(JOSE-1/4)` | Exact future ECDH profile only |
| `apu@JWE` | NS | SafelyAdapted | `Scheduled(JOSE-1/4)` | Exact future ECDH profile only |
| `apv@JWE` | NS | SafelyAdapted | `Scheduled(JOSE-1/4)` | Exact future ECDH profile only |
| `iv@JWE` | NS | SafelyAdapted | `Scheduled(JOSE-1/4)` | AES-GCM key-wrap profile only |
| `tag@JWE` | NS | SafelyAdapted | `Scheduled(JOSE-1/4)` | AES-GCM key-wrap profile only |
| `p2s@JWE` | NS | Rejected | NotPlanned | Ignore/drop; no PBES2 purpose is admitted |
| `p2c@JWE` | NS | Rejected | NotPlanned | Ignore/drop; no PBES2 purpose or work factor is admitted |
| `iss@JWE` | NS | Rejected | NotPlanned | Replicated claim denied by common profile |
| `sub@JWE` | NS | Rejected | NotPlanned | Replicated claim denied by common profile |
| `aud@JWE` | NS | Rejected | NotPlanned | Replicated claim denied by common profile |
| `b64@JWS` | NS | SafelyAdapted | `Scheduled(JOSE-1/2)` | Test-only detached non-JWT; forbidden for JWT |
| `ppt@JWS` | NS | Rejected | NotPlanned | No PASSporT profile |
| `url@JWE,JWS` | NS | Rejected | NotPlanned | No ACME profile; never egress authority |
| `nonce@JWE,JWS` | NS | Rejected | NotPlanned | No ACME JOSE profile |
| `svt@JWS` | NS | Rejected | NotPlanned | No signature-validation-token profile |
| `iheSSId@JWS` | NS | Rejected | NotPlanned | No IHE submission-set profile |
| `jwt@JWS` | NS | Rejected | NotPlanned | No OpenID VP header profile |
| `client_id@JWS` | NS | Rejected | NotPlanned | No header-based OAuth client identity |
| `trust_chain@JWS` | NS | Rejected | NotPlanned | Signed federation chain is not local trust |
| `peer_trust_chain@JWS` | NS | Rejected | NotPlanned | Signed federation chain is not local trust |

The `iv` and `tag` rows above are AES-GCM key-wrap header parameters, not the
ordinary JWE serialisation members with the same spelling.

### JSON Web Signature and Encryption Algorithms — 53/53

| Algorithm | IANA requirement | Decision | Roadmap | Handling/reason |
|---|---|---|---|---|
| `HS256` | Required | Rejected | NotPlanned | No same-owner MAC profile; denied across independent trust |
| `HS384` | Optional | Rejected | NotPlanned | No same-owner MAC profile |
| `HS512` | Optional | Rejected | NotPlanned | No same-owner MAC profile |
| `RS256` | Recommended | SafelyAdapted | `Scheduled(JOSE-2/5A)` | Exact issuer-bound inbound compatibility and sunset |
| `RS384` | Optional | Rejected | NotPlanned | RejectWholeObject |
| `RS512` | Optional | Rejected | NotPlanned | RejectWholeObject |
| `ES256` | Recommended+ | Adopted | `Scheduled(JOSE-1/2)` | Exact P-256 candidate |
| `ES384` | Optional | Rejected | NotPlanned | RejectWholeObject |
| `ES512` | Optional | Rejected | NotPlanned | RejectWholeObject |
| `PS256` | Optional | Adopted | `Scheduled(JOSE-1/2)` | Exact PSS candidate |
| `PS384` | Optional | Rejected | NotPlanned | RejectWholeObject |
| `PS512` | Optional | Rejected | NotPlanned | RejectWholeObject |
| `none` | Optional | Rejected | NotPlanned | Always RejectWholeObject |
| `RSA1_5` | Recommended- | Rejected | NotPlanned | Always RejectWholeObject |
| `RSA-OAEP` | Recommended+ | Rejected | NotPlanned | SHA-1 OAEP profile denied |
| `RSA-OAEP-256` | Optional | Adopted | `Scheduled(JOSE-1/4)` | Exact test-only candidate |
| `A128KW` | Recommended | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Requires distinct purpose/custody decision |
| `A192KW` | Optional | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Requires distinct purpose/custody decision |
| `A256KW` | Recommended | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Requires distinct purpose/custody decision |
| `dir` | Recommended | Rejected | NotPlanned | No same-owner direct-encryption custody profile |
| `ECDH-ES` | Recommended+ | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Exact curve/KDF/key-validation profile required |
| `ECDH-ES+A128KW` | Recommended | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Exact curve/KDF/key-validation profile required |
| `ECDH-ES+A192KW` | Optional | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Exact curve/KDF/key-validation profile required |
| `ECDH-ES+A256KW` | Recommended | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Exact curve/KDF/key-validation profile required |
| `A128GCMKW` | Optional | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Distinct key-wrap purpose required |
| `A192GCMKW` | Optional | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Distinct key-wrap purpose required |
| `A256GCMKW` | Optional | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Distinct key-wrap purpose required |
| `PBES2-HS256+A128KW` | Optional | Rejected | NotPlanned | Password-derived-key purpose absent |
| `PBES2-HS384+A192KW` | Optional | Rejected | NotPlanned | Password-derived-key purpose absent |
| `PBES2-HS512+A256KW` | Optional | Rejected | NotPlanned | Password-derived-key purpose absent |
| `A128CBC-HS256` | Required | Rejected | NotPlanned | Baseline rejects CBC-HMAC |
| `A192CBC-HS384` | Optional | Rejected | NotPlanned | Baseline rejects CBC-HMAC |
| `A256CBC-HS512` | Required | Rejected | NotPlanned | Baseline rejects CBC-HMAC |
| `A128GCM` | Recommended | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Separate reduced-key-size decision |
| `A192GCM` | Optional | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Separate reduced-key-size decision |
| `A256GCM` | Recommended | Adopted | `Scheduled(JOSE-1/4)` | Exact content-encryption candidate |
| `EdDSA` | Deprecated | Rejected | NotPlanned | Polymorphic deprecated identifier denied |
| `RS1` | Prohibited | Rejected | NotPlanned | Reject prohibited WebCrypto/JWK entry |
| `RSA-OAEP-384` | Optional | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Separate algorithm decision |
| `RSA-OAEP-512` | Optional | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Separate algorithm decision |
| `A128CBC` | Prohibited | Rejected | NotPlanned | Reject prohibited WebCrypto/JWK entry |
| `A192CBC` | Prohibited | Rejected | NotPlanned | Reject prohibited WebCrypto/JWK entry |
| `A256CBC` | Prohibited | Rejected | NotPlanned | Reject prohibited WebCrypto/JWK entry |
| `A128CTR` | Prohibited | Rejected | NotPlanned | Reject prohibited WebCrypto/JWK entry |
| `A192CTR` | Prohibited | Rejected | NotPlanned | Reject prohibited WebCrypto/JWK entry |
| `A256CTR` | Prohibited | Rejected | NotPlanned | Reject prohibited WebCrypto/JWK entry |
| `HS1` | Prohibited | Rejected | NotPlanned | Reject prohibited WebCrypto/JWK entry |
| `ES256K` | Optional | Rejected | NotPlanned | `secp256k1` excluded |
| `ML-DSA-44` | Optional | SafelyAdapted | `Scheduled(JOSE-1 feasibility)` | RFC 9964 size/cost/interoperability measurement only |
| `ML-DSA-65` | Optional | SafelyAdapted | `Scheduled(JOSE-1 feasibility)` | RFC 9964 size/cost/interoperability measurement only |
| `ML-DSA-87` | Optional | SafelyAdapted | `Scheduled(JOSE-1 feasibility)` | RFC 9964 size/cost/interoperability measurement only |
| `Ed25519` | Optional | SafelyAdapted | `Scheduled(JOSE-1 feasibility)` | Fully specified identifier; disabled |
| `Ed448` | Optional | Rejected | NotPlanned | No use case or profile |

IANA `Required` is an interoperability registry fact, not a security command.
It does not enable `HS256` or CBC-HMAC.

### JSON Web Encryption Compression Algorithms — 1/1

| Value | IANA | Decision | Roadmap | Handling/reason |
|---|---|---|---|---|
| `DEF` | NS | Rejected | NotPlanned | RejectWholeObject; no decompression |

### JSON Web Key Types — 5/5

| `kty` | IANA requirement | Decision | Roadmap | Handling/reason |
|---|---|---|---|---|
| `EC` | Recommended+ | Adopted | `Scheduled(JOSE-1/3)` | Exact curve/profile required |
| `RSA` | Required | Adopted | `Scheduled(JOSE-1/3)` | Exact size/exponent/profile required |
| `oct` | Required | SafelyAdapted | `Scheduled(JOSE-1 feasibility)` | No external-trust profile; private material prohibited in public set |
| `OKP` | Optional | SafelyAdapted | `Scheduled(JOSE-1 feasibility)` | Exact fully specified algorithm/curve only |
| `AKP` | Optional | SafelyAdapted | `Scheduled(JOSE-1 feasibility)` | Exact RFC 9964 parameters; public only |

### JSON Web Key Elliptic Curve — 8/8

| Curve | IANA requirement | Decision | Roadmap | Handling/reason |
|---|---|---|---|---|
| `P-256` | Recommended+ | Adopted | `Scheduled(JOSE-1/3)` | Exact ES256 candidate |
| `P-384` | Optional | Rejected | NotPlanned | No use case or profile; bounded refusal coverage only |
| `P-521` | Optional | Rejected | NotPlanned | No use case or profile; bounded refusal coverage only |
| `Ed25519` | Optional | SafelyAdapted | `Scheduled(JOSE-1 feasibility)` | Fully specified identifier only |
| `Ed448` | Optional | Rejected | NotPlanned | No use case or profile |
| `X25519` | Optional | SafelyAdapted | `Scheduled(JOSE-1/4 conditional)` | Exact ECDH profile required |
| `X448` | Optional | Rejected | NotPlanned | No use case or profile |
| `secp256k1` | Optional | Rejected | NotPlanned | RejectWholeObject |

### JSON Web Key Parameters — 33/33

| Composite row key | IANA class | Decision | Roadmap | Handling/reason |
|---|---|---|---|---|
| `kty@*` | Public | Adopted | `Scheduled(JOSE-1/3)` | Exact profile/key-type match |
| `use@*` | Public | Adopted | `Scheduled(JOSE-1/3)` | Exact purpose match |
| `key_ops@*` | Public | Adopted | `Scheduled(JOSE-1/3)` | Exact operation subset |
| `alg@*` | Public | Adopted | `Scheduled(JOSE-1/3)` | Exact algorithm binding |
| `kid@*` | Public | Adopted | `Scheduled(JOSE-1/3)` | Bounded opaque hint |
| `x5u@*` | Public | Rejected | NotPlanned | Never egress authority |
| `x5c@*` | Public | SafelyAdapted | `Scheduled(JOSE-3)` | Exact preconfigured trust only |
| `x5t@*` | Public | Rejected | NotPlanned | SHA-1 thumbprint denied |
| `x5t#S256@*` | Public | SafelyAdapted | `Scheduled(JOSE-3)` | Additional binding only |
| `crv@EC` | Public | Adopted | `Scheduled(JOSE-1/3)` | Exact profile curve |
| `x@EC` | Public | Adopted | `Scheduled(JOSE-1/3)` | Exact coordinate length/range |
| `y@EC` | Public | Adopted | `Scheduled(JOSE-1/3)` | Exact coordinate length/range |
| `d@EC` | Private | Rejected | NotPlanned | Private member forbidden at public boundary |
| `n@RSA` | Public | Adopted | `Scheduled(JOSE-1/3)` | Exact modulus size |
| `e@RSA` | Public | Adopted | `Scheduled(JOSE-1/3)` | Exact exponent |
| `d@RSA` | Private | Rejected | NotPlanned | Private member forbidden |
| `p@RSA` | Private | Rejected | NotPlanned | Private member forbidden |
| `q@RSA` | Private | Rejected | NotPlanned | Private member forbidden |
| `dp@RSA` | Private | Rejected | NotPlanned | Private member forbidden |
| `dq@RSA` | Private | Rejected | NotPlanned | Private member forbidden |
| `qi@RSA` | Private | Rejected | NotPlanned | Private member forbidden |
| `oth@RSA` | Private | Rejected | NotPlanned | Private member forbidden |
| `k@oct` | Private | Rejected | NotPlanned | Symmetric key forbidden at public boundary |
| `crv@OKP` | Public | SafelyAdapted | `Scheduled(JOSE-1/3 feasibility)` | Exact algorithm/curve pair |
| `d@OKP` | Private | Rejected | NotPlanned | Private member forbidden |
| `x@OKP` | Public | SafelyAdapted | `Scheduled(JOSE-1/3 feasibility)` | Exact public length |
| `ext@*` | Public | Rejected | NotPlanned | WebCrypto extractability is not local policy |
| `iat@*` | Public | Rejected | NotPlanned | Ignore/drop; no adopted JWK lifecycle protocol or local authority |
| `nbf@*` | Public | Rejected | NotPlanned | Ignore/drop; no adopted JWK lifecycle protocol or local authority |
| `exp@*` | Public | Rejected | NotPlanned | Ignore/drop; no adopted JWK lifecycle protocol or local authority |
| `revoked@*` | Public | Rejected | NotPlanned | Ignore/drop; never local lifecycle authority |
| `pub@AKP` | Public | SafelyAdapted | `Scheduled(JOSE-1/3 feasibility)` | Exact RFC 9964 public length |
| `priv@AKP` | Private | Rejected | NotPlanned | Private seed forbidden at public boundary |

### JSON Web Key Use — 2/2

| Use | IANA | Decision | Roadmap | Handling |
|---|---|---|---|---|
| `sig` | NS | Adopted | `Scheduled(JOSE-1/3)` | Exact purpose/operation match |
| `enc` | NS | Adopted | `Scheduled(JOSE-1/3)` | Exact purpose/operation match |

### JSON Web Key Operations — 8/8

| Operation | IANA | Decision | Roadmap | Handling/reason |
|---|---|---|---|---|
| `sign` | NS | Adopted | `Scheduled(JOSE-1/3)` | Separate semantic signing port |
| `verify` | NS | Adopted | `Scheduled(JOSE-1/3)` | Separate semantic verification port |
| `encrypt` | NS | Adopted | `Scheduled(JOSE-1/3)` | Separate semantic encryption port |
| `decrypt` | NS | Adopted | `Scheduled(JOSE-1/3)` | Separate semantic decryption port |
| `wrapKey` | NS | SafelyAdapted | `Scheduled(JOSE-1/3/4 conditional)` | Exact custodian/profile only |
| `unwrapKey` | NS | SafelyAdapted | `Scheduled(JOSE-1/3/4 conditional)` | Exact custodian/profile only |
| `deriveKey` | NS | SafelyAdapted | `Scheduled(JOSE-1/3/4 conditional)` | Exact ECDH/KDF profile only |
| `deriveBits` | NS | Rejected | NotPlanned | Generic raw-bit derivation not exposed |

### JSON Web Key Set Parameters — 1/1

| Parameter | IANA | Decision | Roadmap | Handling |
|---|---|---|---|---|
| `keys` | NS | Adopted | `Scheduled(JOSE-1/3)` | Complete bounded array; no truncation |

### JOSE disposition totals

| Decision | Count |
|---|---:|
| Adopted | 33 |
| SafelyAdapted | 44 |
| Rejected | 78 |
| Unreviewed | 0 |
| **Total** | **155** |

## JWT Claims registry classification

Claim decisions are keyed by `(claim, token type, purpose, profile)`.
Grouping below is only a compact presentation: every one of the 163 exact
claim names is listed once, and every member inherits the group's decision,
roadmap, handling and rationale. A later profile may make a more restrictive
decision but cannot silently broaden one.

| Group | Count | Decision | Handling | Roadmap | Exact claims | Purpose/source and boundary |
|---|---:|---|---|---|---|---|
| `C01` | 4 | Adopted | ValidateAndMap | `Scheduled(JOSE-2/5A)` | `iss`, `aud`, `exp`, `nbf` | Exact human access-token issuer/audience/time; RFC 7519/9068 |
| `C02` | 1 | Adopted | ValidateAndMap | `Scheduled(JOSE-5A)` | `sub` | Canonical identity only with validated `iss`; never email/name |
| `C03` | 1 | Adopted | ValidateAndMap | `Scheduled(JOSE-2/5A)` | `iat` | Bounded issuance/freshness; not revocation |
| `C04` | 1 | SafelyAdapted | ProtocolOnly | per future artefact | `jti` | Purpose-bound replay decision; digest/TTL only, never raw token/`jti` store |
| `C05` | 2 | Adopted | ValidateAndMap | `Scheduled(JOSE-2/5A)` | `scope`, `client_id` | RFC 9068 context; exact server-side mapping, never direct role |
| `C06` | 3 | SafelyAdapted | ValidateAndMap | `Scheduled(JOSE-5A)` | `roles`, `groups`, `entitlements` | Allowlisted issuer mapping input; never direct RBAC |
| `C07` | 1 | SafelyAdapted | ValidateAndMap | `Scheduled(JOSE-5A)` | `name` | Optional display label only; PII minimisation |
| `C08` | 18 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `given_name`, `family_name`, `middle_name`, `nickname`, `preferred_username`, `profile`, `picture`, `website`, `email`, `email_verified`, `gender`, `birthdate`, `zoneinfo`, `locale`, `phone_number`, `phone_number_verified`, `address`, `updated_at` | OIDC PII/contact/demographic/URLs; unnecessary for DB-Notifier baseline |
| `C09` | 8 | SafelyAdapted | ProtocolOnly | `Scheduled(JOSE-5A)` | `azp`, `nonce`, `auth_time`, `at_hash`, `c_hash`, `acr`, `amr`, `sid` | OIDC ID-token/session only; mutually exclusive from API access token |
| `C10` | 1 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `sub_jwk` | Embedded subject key does not establish trust |
| `C11` | 1 | SafelyAdapted | ProtocolOnly | separate decision | `cnf` | Conditional sender constraint only after mTLS/DPoP profile |
| `C12` | 3 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `events`, `toe`, `txn` | No Security Event Token delivery/replay profile |
| `C13` | 2 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `act`, `may_act` | No token exchange/delegation profile |
| `C14` | 2 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `_claim_names`, `_claim_sources` | No aggregated/distributed claims or claim-source egress |
| `C15` | 1 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `authorization_details` | No Rich Authorisation Requests; cannot bypass canonical RBAC |
| `C16` | 1 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `token_introspection` | No JWT introspection-response profile |
| `C17` | 3 | Rejected | IgnoreAndDropAtBoundary | separate decision | `htm`, `htu`, `ath` | No DPoP proof JWT profile |
| `C18` | 3 | Rejected | IgnoreAndDropAtBoundary | Watchlist | `status`, `status_list`, `ttl` | No Token Status List profile; snapshot reference remains an Internet-Draft |
| `C19` | 5 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `sip_from_tag`, `sip_date`, `sip_callid`, `sip_cseq_num`, `sip_via_branch` | SIP identity/call metadata |
| `C20` | 3 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `orig`, `dest`, `mky` | PASSporT/STIR telephony identity/media |
| `C21` | 1 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `rph` | SIP resource priority |
| `C22` | 2 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `vot`, `vtm` | Vector of Trust/trustmark URL is not DB-Notifier trust |
| `C23` | 2 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `attest`, `origid` | SHAKEN telephony attestation |
| `C24` | 1 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `jcard` | Structured vCard PII/nesting |
| `C25` | 1 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `at_use_nbr` | ETSI counter cannot replace atomic server state |
| `C26` | 2 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `div`, `opt` | Diverted-call PASSporT/nesting |
| `C27` | 2 | Rejected | IgnoreAndDropAtBoundary | separate protocol decision | `vc`, `vp` | W3C VC/VP is a separate credential/trust protocol |
| `C28` | 1 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `sph` | SIP priority |
| `C29` | 3 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `ace_profile`, `cnonce`, `exi` | ACE OAuth constrained-device semantics |
| `C30` | 21 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `eat_nonce`, `ueid`, `sueids`, `oemid`, `hwmodel`, `hwversion`, `oemboot`, `dbgstat`, `location`, `eat_profile`, `submods`, `uptime`, `bootcount`, `bootseed`, `dloas`, `swname`, `swversion`, `manifests`, `measurements`, `measres`, `intuse` | EAT identity/location/software/measurement data; no attestation trust model |
| `C31` | 7 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `cdniv`, `cdnicrit`, `cdniip`, `cdniuc`, `cdniets`, `cdnistt`, `cdnistd` | CDNI URI/IP/renewal semantics |
| `C32` | 1 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `sig_val_claims` | Nested signature-validation assertion is not local verification |
| `C33` | 10 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `verified_claims`, `place_of_birth`, `nationalities`, `birth_family_name`, `birth_given_name`, `birth_middle_name`, `salutation`, `title`, `msisdn`, `also_known_as` | High-risk identity-assurance/legal PII not required |
| `C34` | 1 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `atc` | Authority Token Challenge is telephony-specific |
| `C35` | 1 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `sub_id` | SET subject identifier without SET profile |
| `C36` | 3 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `rcd`, `rcdi`, `crn` | Rich Call Data PII/remote resources |
| `C37` | 1 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `msgi` | SIP message-integrity profile absent |
| `C38` | 2 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `rdap_allowed_purposes`, `rdap_dnt_allowed` | RDAP-specific purposes/tracking semantics |
| `C39` | 1 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `geohash` | Geolocation/high privacy |
| `C40` | 4 | Rejected | IgnoreAndDropAtBoundary | separate protocol decision | `_sd`, `...`, `_sd_alg`, `sd_hash` | RFC 9901 SD-JWT/KB-JWT disclosure and key binding are separate |
| `C41` | 11 | Rejected | IgnoreAndDropAtBoundary | NotPlanned | `consumerPlmnId`, `consumerSnpnId`, `producerPlmnId`, `producerSnpnId`, `producerSnssaiList`, `producerNsiList`, `producerNfSetId`, `producerNfServiceSetId`, `sourceNfInstanceId`, `analyticsIdList`, `resOwnerId` | 3GPP topology/slice/owner metadata |
| `C42` | 1 | Rejected | IgnoreAndDropAtBoundary | separate protocol decision | `cmw` | RFC 9999 RATS wrapper requires a separate attestation trust model |
| `C43` | 19 | Rejected | IgnoreAndDropAtBoundary | separate federation decision | `jwks`, `metadata`, `constraints`, `crit`, `ref`, `delegation`, `logo_uri`, `authority_hints`, `trust_anchor_hints`, `trust_marks`, `trust_mark_issuers`, `trust_mark_owners`, `metadata_policy`, `metadata_policy_crit`, `source_endpoint`, `keys`, `trust_mark_type`, `trust_chain`, `trust_anchor` | OpenID Federation objects/URLs/policies cannot choose local trust or egress |
| `C44` | 1 | Rejected | IgnoreAndDropAtBoundary | Watchlist | `stpl` | STIR OCSP staple; snapshot reference remains an Internet-Draft |

### JWT Claims disposition totals

| Decision | Count |
|---|---:|
| Adopted | 8 |
| SafelyAdapted | 14 |
| Rejected | 141 |
| Unreviewed | 0 |
| **Total** | **163** |

## Coverage result and delta rule

| Registry scope | Classified | Denominator | Coverage | Unreviewed | Duplicate matrix keys | Missing/extra |
|---|---:|---:|---:|---:|---:|---:|
| IANA JOSE | 155 | 155 | 100% | 0 | 0 | 0/0 |
| IANA JWT Claims | 163 | 163 | 100% | 0 | 0 | 0/0 |
| **Combined** | **318** | **318** | **100%** | **0** | **0** | **0/0** |

The JOSE Contact Information table is administrative metadata, not one of
the nine registries, and is excluded from the denominator. JWT Confirmation
Methods and JWT Status Mechanisms are separate registries and are also
excluded.

Any entry added, removed or changed after these hashes becomes
`Unreviewed + RuntimeDisabled + NotAdvertised` until a new authorised capture,
diff, security classification and evidence update completes. A changed live
registry never alters a deployed profile automatically.

## Difficult entries and explicit safeguards

- `alg`, `d`, `crv`, `x`, `iat`, `nbf`, `exp`, `crit`, `keys` and similar
  names exist in different registries or contexts. Composite keys and
  purpose-specific profiles prevent semantic collapse.
- `EdDSA` is deprecated, while `Ed25519` and `Ed448` are the fully specified
  RFC 9864 identifiers.
- `AKP` is a key type, not a curve. RFC 9964 requires `alg` and `pub`;
  `priv` is a 32-byte private seed and is prohibited from public trust,
  fixtures and retained evidence.
- ML-DSA public keys and signatures are materially larger than traditional
  values. Exact size caps and feasibility measurement are mandatory before
  any later decision.
- JWK `iat`, `nbf`, `exp` and `revoked` are untrusted OpenID Federation
  metadata and never control DB-Notifier key lifecycle.
- WebCrypto rows with usage location `JWK` and IANA status `Prohibited` are
  not ordinary JWS/JWE operational algorithms and remain rejected.
- A registered claim name does not make a value true, necessary, safe to
  retain or authorised for RBAC.
