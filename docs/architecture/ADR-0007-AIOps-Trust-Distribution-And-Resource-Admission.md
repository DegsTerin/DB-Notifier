# ADR-0007 — AIOps Trust Distribution and Resource Admission

- Status: proposed
- Date: 2026-07-17
- Owners: security, data and platform architecture
- Lifecycle position: `STATE-06 INTEGRATION`
- Implementation status: documentary design only; no runtime or MOD-12 mode is active

## Context

The inactive MOD-12 foundation can verify an authenticated policy grant and an independently signed revocation snapshot against application-supplied public trust anchors. It also requires an exact revocation sequence selected by trusted application configuration and applies a bounded, synchronous budget to deterministic offline evaluations.

That local implementation deliberately does not issue or distribute policy, rotate keys, persist or advance a checkpoint, reconcile restored state, reserve process-wide resources or bind MOD-12 to a runtime. Treating the existing verifier as any of those facilities would create false anti-rollback and resource-safety claims.

Before a later implementation can be proposed, the architecture needs to separate legitimate policy authorisation from signature production, define how a future host could obtain and commit trust state without letting evidence choose its own root, and specify admission dimensions beyond declared item counts. The design must also remain provider-neutral, fail closed and preserve the independent `none → OBSERVER` gate.

## Proposed decision

Adopt a signed, scope-bound trust bundle coordinated by a future host-owned trust boundary, with a durable monotonic checkpoint and an explicit resource reservation envelope. The detailed conceptual contracts are defined in [AIOps Trust Governance and Resource Envelope](AIOps-Trust-Governance-And-Resource-Envelope.md).

The proposed decision has these invariants:

1. The MOD-12 verifier remains a pure consumer of an immutable, already selected verification context. It owns no private key, network client, trust store, checkpoint store, queue or runtime lifecycle.
2. A root trust anchor is provisioned through a boundary independent from the bundle and evidence it validates. A bundle may identify the expected root but cannot introduce or replace it.
3. Policy decision, policy-authorisation attestation, grant/revocation/trust-bundle signing, key custody, publication, verification, checkpoint ownership and audit review are distinct responsibilities. A valid operational signature proves origin and integrity; legitimate scope additionally requires a separately signed, independently verified `PolicyAuthorisationAssertion`.
4. A versioned canonical trust bundle binds authority, series, recovery epoch, tenant/environment, purpose, canonical requested scope, policy assertion, monotonic generation, validity, role-key delegations, required revocation state, predecessor digest, algorithm profile, complete finite resource ceilings, size/cardinality declarations and audit references. Its trust semantics are independent of whether a later separately authorised deployment carries one bounded contiguous chain over outbound Agent/API configuration or controlled offline import.
5. Policy-attestation, grant, revocation and trust-bundle signing roles use distinct key identities and distinct public-key material. Every operational key has an exact root-signed `RoleKeyDelegation`; an operational signer cannot delegate itself or its successor.
6. The host authenticates the assertion/delegations and evaluates a bounded canonical-set subset predicate; a digest alone cannot prove that requested scope is within the approved ceiling.
7. A future host starts an absolute monotonic deadline before acquiring any lease, bounds silent/encoded/expanded input and validates canonical form, roles, scope, time, contiguous generation/recovery continuity and revocation before making a candidate visible to MOD-12.
8. The host commits the selected bundle, revocation state, stable domain epoch/series high-water, subordinate checkpoint and durable audit intent as one local transaction. This is local atomicity only; it does not claim simultaneous global change across consumers.
9. Missing, corrupt, restored, divergent or unverifiable current continuity places the affected MOD-12 scope in quarantine. A malformed/unauthenticated candidate alone does not deactivate a fresh intact current tuple. Consumption after continuity loss resumes only through authenticated reconciliation; normal processing never decreases an epoch or checkpoint. Quarantine does not disable provider-neutral deterministic monitoring.
10. Resource admission uses a pre-authentication global ingress lease followed by atomic authenticated hierarchical quotas/capacity. Complete finite limits cover encoded/expanded bytes, liveness, cardinality, versioned accounted memory, deterministic work, deadline, cancellation, output and concurrency; the effective value is the minimum of host, policy assertion, bundle and request, while producer declarations only reduce or are checked against it. Any future queue is separately authorised and bounded.
11. An interrupted, cancelled or over-budget evaluation publishes no passing or authorising partial result. Subset precision, recall, calibration and aggregates are absent rather than merely excluded from a gate; only bounded sanitised diagnostics may remain.
12. Initial future proof remains serial (`maximum parallelism = 1`) and proves containment, not general fairness. Operational numerical limits, real cancellation latency and observed memory require reproducible measurement and a separately authorised implementation/load increment; this ADR invents none.
13. Completion or later acceptance of this ADR does not activate `OBSERVER`, authorise telemetry collection, create persistence or permit a runtime integration.

## Responsibility boundary

```text
Policy authority ---- authorises purpose, canonical scope ceiling and validity
        |
        v
Independent attestation/root delegations ---- prove the approved decision and role keys
        |
        v
Role-specific signing boundaries ---- produce grant/revocation/bundle signatures
        |
        v
Untrusted publisher/transport ---- carries bounded public artefacts only
        |
        v
Future host trust coordinator ---- validates, reconciles and commits local state
        |
        v
Pure MOD-12 verifier ---- consumes an immutable verified context
```

The publisher is not trusted to authorise, sign, select a root or advance a checkpoint. The host is not allowed to mint policy. The verifier is not allowed to recover, persist or fetch trust state.

## Alternatives considered

### A. Static application configuration only

Keep public anchors and an exact revocation sequence in manually managed configuration, matching the present local proof.

- Advantages: smallest conceptual surface; no distribution protocol; straightforward local verification.
- Disadvantages: no defined rotation, authenticated publication, durable advancement, restore reconciliation or fleet consistency; manual drift creates freeze and split-view risk.
- Decision: retain only as the current inactive test baseline, not as a future operational design.

### B. Signed bundle with host-owned durable checkpoint

Distribute a canonical signed bundle through an untrusted transport, validate it against an independently provisioned root, and commit it with the revocation checkpoint in a host-owned local transaction.

- Advantages: separates evidence from trust configuration, permits bounded offline verification, supports deterministic rotation and confines persistence outside MOD-12.
- Disadvantages: a local store cannot alone detect a complete malicious restore or prove that another consumer saw the same generation; recovery and reconciliation remain operational obligations.
- Decision: proposed baseline, with quarantine and authenticated independent reconciliation required for restore, divergence or continuity loss.

### C. Transparency log or multi-witness trust state

Require an append-only transparency service, quorum or multiple independent witnesses for every generation.

- Advantages: strongest detection of equivocation, rollback and split view across consumers.
- Disadvantages: creates external services, availability dependencies and operational complexity that are not authorised or justified by current evidence; offline behaviour is harder.
- Decision: not selected for the first design. Preserve compatible digests and audit references so a later threat/risk decision can add witnesses without changing MOD-12 evidence semantics.

### D. Self-contained evidence that supplies its own anchors

Let each grant or revocation artefact carry the root or trust configuration used to validate it.

- Advantages: superficially simple transport and bootstrap.
- Disadvantages: circular trust; an attacker can replace evidence and its asserted root together. It also collapses authorisation, integrity and selection into one untrusted object.
- Decision: rejected.

## Normal rotation

Normal rotation is a planned change within the existing authority and scope ceiling:

1. Root governance approves and root custody signs the replacement role-key delegation; policy authority separately approves any policy impact and overlap window.
2. The next bundle generation includes the existing and replacement root-delegated public keys for their exact roles, validity periods and scopes.
3. The candidate links to the accepted predecessor digest and required revocation state.
4. The host validates and locally commits the generation before grants signed only by the new key are eligible.
5. A later generation retires the old key after the bounded overlap. Retirement never reactivates an earlier key automatically.

Rotation cannot change tenant/environment, purpose or scope ceiling implicitly.

## Compromise recovery

Compromise recovery is not normal rotation. It requires a separately authorised security procedure, a new recovery epoch or series, explicit quarantine and independent reconciliation evidence. One local recovery transaction advances an epoch/series high-water mark keyed without the epoch and permanently supersedes the old continuity branch. A compromised operational signer cannot authorise its own recovery. Root compromise requires replacement through the out-of-band provisioning boundary and cannot be repaired by a bundle signed under the compromised root.

No automatic fallback, generation decrement, key resurrection, schema downgrade or algorithm downgrade is permitted during recovery.

## Consequences

### Positive

- The pure verifier remains isolated from I/O, secrets and operational authority.
- Identity, authorisation, integrity, revocation, durable state and publication have separate owners and evidence.
- Local checkpoint advancement can be deterministic, idempotent and crash-safe without claiming impossible global atomicity.
- Resource admission becomes measurable across memory, bytes, work, time and concurrency rather than relying on producer-declared counts.
- The bundle can be transported by different future deployment shapes without making transport a trust anchor.

### Cost and complexity

- A future implementation needs a host-owned coordinator, durable state, protected audit and a recovery procedure, each under new authority.
- Role and key separation increases operational ceremony and demands rehearsed rotation/compromise runbooks.
- Per-scope and global reservations require capacity measurement before safe numerical defaults can be approved.
- Offline availability is intentionally lost when freshness, continuity or reconciliation cannot be proved.

### Residual risks

- Local state plus digest chaining does not prove absence of fleet-wide split view. Independent reconciliation or witnesses are needed for that assurance.
- An undetected full restore of both checkpoint and local audit can defeat purely local monotonicity. Backup/restore integration or an independent witness must expose continuity loss; until then, restore must quarantine by procedure.
- A legitimately authorised but compromised root remains a catastrophic trust event requiring out-of-band recovery.
- Resource dimensions do not establish safe values. Load and memory evidence remains future work.

## Security and operational constraints

- Private keys, certificates, credentials, vault integration and signing services remain outside this documentary increment.
- The trust bundle is public security metadata but remains untrusted input until fully validated.
- Authorisation is deny-by-default and bounded by tenant/environment, purpose and scope ceiling.
- The host authenticates before high-cost semantic processing wherever framing permits, while a bounded reader limits work required to reach signature verification.
- Audit records contain identifiers, generations, digests, decisions and sanitised refusal codes, never private material or sensitive telemetry.
- Retry is never implicit. A rejected candidate requires a new explicit admission after the reported condition changes.

## Compatibility and migration

- Bundle schema version, policy schema version, policy content version, revocation schema/version, dataset schema version and dataset content version remain independent.
- A new major schema or cryptographic algorithm profile requires explicit compatibility policy and no automatic downgrade.
- A connected or offline consumer may catch up only through a trusted-locally-bounded complete contiguous chain whose every transition is validated. A later generation without its intermediate chain remains a gap.
- The current `observer-policy-revocation.v2` sequence check remains evidence for the local verifier only; it is not proof of distribution, persistence or coordinated advancement.
- Bootstrap from no checkpoint and recovery from lost continuity are security ceremonies, not ordinary migration paths.
- No table, database, protocol route, certificate format, signing product or queue technology is selected by this ADR.

## Acceptance and implementation gate

This ADR is a proposed decision produced under a documentation-only `STATE-06` authority. Architecture, security and data review plus Bruno's separate Human Gate are required before it can become accepted. Acceptance would still not authorise code or runtime; any implementation would need a new, explicitly bounded increment and its own checks.
