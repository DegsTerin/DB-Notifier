# ADR-0007 — AIOps Trust Distribution and Resource Admission

- Status: accepted
- Date: 2026-07-17
- Adoption date: 2026-07-20
- Owners: security, data and platform architecture
- Lifecycle position: `STATE-06 INTEGRATION`
- Implementation status: not authorised; no runtime or MOD-12 mode is active

## Context

The inactive MOD-12 foundation can verify an authenticated policy grant and an independently signed revocation snapshot against application-supplied public trust anchors. It also requires an exact revocation sequence selected by trusted application configuration and applies a bounded, synchronous budget to deterministic offline evaluations.

That local implementation deliberately does not issue or distribute policy, rotate keys, persist or advance a checkpoint, reconcile restored state, reserve process-wide resources or bind MOD-12 to a runtime. Treating the existing verifier as any of those facilities would create false anti-rollback and resource-safety claims.

Before a later implementation can be proposed, the architecture needs to separate legitimate policy authorisation from signature production, define how a future host could obtain and commit trust state without letting evidence choose its own root, and specify admission dimensions beyond declared item counts. The design must also remain provider-neutral, fail closed and preserve the independent `none → OBSERVER` gate.

## Decision

Adopt a signed, scope-bound trust bundle coordinated by a future host-owned trust boundary, with a durable monotonic checkpoint and an explicit resource reservation envelope. The detailed conceptual contracts are defined in [AIOps Trust Governance and Resource Envelope](AIOps-Trust-Governance-And-Resource-Envelope.md).

The accepted architectural decision has these invariants:

1. The MOD-12 verifier remains a pure consumer of an immutable, already selected verification context. It owns no private key, network client, trust store, checkpoint store, queue or runtime lifecycle.
2. A root trust anchor is provisioned through a boundary independent from the bundle and evidence it validates. A bundle may identify the expected root but cannot introduce or replace it.
3. Root governance, policy and revocation decisions, policy/revocation/corpus attestation, grant/revocation/trust-bundle signing, key custody, publication, verification, checkpoint ownership and audit review are distinct responsibilities. Policy and revocation decisions require independently authenticated dual control; an attestation signer cannot invent an approval.
4. A versioned canonical trust bundle binds the root-set head, authority, immutable series, positive recovery epoch, non-circular `epochContextId`, tenant/environment, purpose, canonical requested scope, policy decision/assertion, monotonic generation, validity, role-key delegations, cumulative revocation state, authorised corpus heads, predecessor digest, algorithm profile, complete finite resource ceilings, exact/maximum declarations and audit references. Transport remains outside the trust root.
5. Root, approval, policy-attestation, corpus-attestation, grant, revocation-decision, revocation-snapshot and trust-bundle roles use distinct key identities and distinct public-key material. Every subordinate key has an exact root-signed `RoleKeyDelegation`; root material is never reused operationally and no operational signer delegates itself or its successor.
6. The currently accepted root and revocation heads authenticate an ordinary candidate before candidate state applies. Every subordinate security artefact binds a host-derived non-circular `epochContextId`; recovery makes all old-epoch artefacts ineligible and requires fresh identifiers/issuance. Ordinary revocation uses direct-successor or bounded-complete-chain continuity, independently approved irreversible cumulative tombstones, no identifier reuse and a finite accumulator; a candidate cannot legitimise itself or omit an earlier tombstone.
7. Bootstrap, root-set transition and compromise recovery use distinct authenticated one-use approvals under independently selected trust: bootstrap quorum plus pristine marker, current-root-delegated transition quorum plus independent successor provisioning, or out-of-band recovery quorum. Each approval precommits to the exact non-circular proposed head. Recovery checked-increments a constant-size epoch high-water, installs a sequence-`1` revocation head/new namespace and may choose one series immutable inside that epoch. Absence is not first installation without an independent marker, and no counter wraps.
8. A future host processes root, revocation, policy and corpus-head updates through bounded host-reserved control-plane capacity that evaluation work cannot consume. It starts deadlines before reservation, bounds pre-authentication work and commits a complete candidate before publishing a new immutable `contextRevision`.
9. The host commits the selected root reference, consumed one-use approval where applicable, bundle, cumulative revocation state, corpus heads, stable domain head, subordinate checkpoint and durable audit intent as one local transaction. This is local atomicity only; it does not claim simultaneous fleet-wide change.
10. Missing, corrupt, restored, divergent, stale or unverifiable **current** continuity places the affected MOD-12 scope in quarantine. A malformed, unauthenticated, expired, not-yet-valid or lower-generation candidate alone does not deactivate a fresh intact current tuple. An authenticated same-generation divergent candidate or gap is continuity evidence and quarantines. Consumption after continuity loss resumes only through authenticated reconciliation or extraordinary recovery; normal processing never decreases a head. Quarantine does not disable provider-neutral deterministic monitoring.
11. Resource fields compose by class: validated numeric ceilings use the minimum; allowlists intersect; denial dominates capabilities; profiles require exact or explicitly approved compatibility; and queue state remains disabled. A bundle above its policy assertion or a request above the trusted host/policy/bundle result is rejected rather than silently clamped. Producer exact declarations must equal final observation, maxima may only reduce, and omitted applicable fields fail closed.
12. Evaluation admission uses one uniquely fenced coordinator on one host/process, then atomic authenticated hierarchical quotas/capacity. Limits cover encoded/expanded bytes and ratio, parser structure/cardinality, deterministic accounted memory, work, total/phase/liveness time, cancellation, output count/bytes and concurrency. Observed heap/working-set memory is empirical calibration only.
13. Lease expiry requests cancellation but never proves capacity reusable. Capacity is released exactly once only after owned work is quiescent or an independently authorised termination fence ends it; an unfenced non-pre-emptible primitive is refused.
14. Every evaluation pins one active immutable `contextRevision`, and its deadline cannot outlive relevant trust/corpus validity. An accepted control update cancels old-context work; publication revalidates the exact revision and stale results are non-authorising.
15. A future corpus uses an independently approved, role-signed manifest and monotonic durable corpus head with exact case/partition membership, quantitative segment criteria and irreversible withdrawal. Replacing content, manifest and self-consistent digests together cannot establish authority.
16. An interrupted, cancelled, superseded or over-budget evaluation publishes no passing or authorising partial result. Subset precision, recall, calibration and aggregates are absent rather than merely excluded from a gate; only bounded sanitised diagnostics may remain.
17. The first future proof remains serial (`maximum parallelism = 1`) within one fenced coordinator and proves containment, not fleet-wide accounting, deterministic fairness or starvation freedom. Any contender registry/queue, cross-restart quota claim and operational numerical limit require separate authority and evidence; this ADR invents none.
18. Adoption of this ADR does not activate `OBSERVER`, authorise telemetry collection, create persistence or permit a runtime integration.

## Responsibility boundary

```text
Policy authority ---- authorises purpose, canonical scope ceiling and validity
        |
        v
Independent approval/attestation boundaries ---- prove policy, revocation and corpus decisions
        |
        v
Root governance and delegations ---- bind root-set transitions and exact subordinate roles
        |
        v
Role-specific signing boundaries ---- produce grant/revocation/corpus/bundle signatures
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
- Decision: accepted architectural baseline, with quarantine and authenticated independent reconciliation required for restore, divergence or continuity loss.

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

Root-set rotation is a separate ceremony. It requires an independently provisioned direct-successor root set and one-use `RootSetTransitionApproval` bound to the exact old/new versions, digests, bounded overlap and proposed head. Before commit, every candidate subordinate artefact must validate under the proposed remaining root set; removal is refused while any artefact depends exclusively on the retiring root. A bundle cannot introduce or silently retain a root, and root material cannot be reused by a subordinate role. Replay of a lower root candidate rejects without deactivating a fresh intact current root; rollback of the independently provisioned current root or an authenticated same-version-divergent/gapped transition quarantines the domain.

## Compromise recovery

Compromise recovery is not normal rotation. It requires a separately authorised security procedure, explicit quarantine, independent reconciliation evidence and one unused `TrustRecoveryApproval` that binds the exact old/new heads, root-set references, reason, validity, quorum and nonce. One local recovery transaction checked-increments the constant-size `recoveryEpochHighWater`, selects a series immutable inside that new epoch and permanently supersedes every lower epoch. A compromised operational signer cannot authorise its own recovery. Root compromise requires replacement through an out-of-band quorum that does not rely only on the compromised root.

No automatic fallback, generation decrement, same-epoch series change, identifier/key resurrection, schema downgrade, algorithm downgrade or counter wrap is permitted during recovery. Exhaustion remains quarantined until a separately authorised new trust-domain ceremony.

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
- Per-scope and coordinator-global reservations require capacity measurement before safe numerical defaults can be approved.
- Offline availability is intentionally lost when freshness, continuity or reconciliation cannot be proved.

### Residual risks

- Local state plus digest chaining does not prove absence of fleet-wide split view. Independent reconciliation or witnesses are needed for that assurance.
- An undetected full restore of both checkpoint and local audit can defeat purely local monotonicity. Backup/restore integration or an independent witness must expose continuity loss; until then, restore must quarantine by procedure.
- A legitimately authorised but compromised root remains a catastrophic trust event requiring out-of-band recovery.
- A compromised policy/revocation approval quorum remains catastrophic even though an attestation signer alone cannot invent a decision.
- A cancelled component that cannot prove quiescence retains its reservation unless an authorised termination fence ends it; safe isolation technology remains undecided.
- A signed corpus proves authority and integrity, not correctness of labels or freedom from bias.
- Resource dimensions do not establish safe values. Load and memory evidence remains future work.

## Security and operational constraints

- Private keys, certificates, credentials, vault integration and signing services remain outside this documentary increment.
- The trust bundle is public security metadata but remains untrusted input until fully validated.
- Authorisation is deny-by-default and bounded by tenant/environment, purpose and scope ceiling.
- The host authenticates before high-cost semantic processing wherever framing permits, while a bounded reader limits work required to reach signature verification.
- Trust/revocation/root/corpus admission capacity and deterministic priority scheduling are separate from evaluation capacity, so evaluation cannot consume or cause capacity refusal of a narrowing update. Scheduler/CPU/GC latency remains bounded by a future control-phase deadline and requires empirical evidence; all control metadata remains explicitly bounded.
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

Bruno adopted this ADR as an architectural decision on 2026-07-20. Adoption establishes the architectural constraints only: it does not authorise implementation, source or configuration changes, runtime, persistence, services, telemetry collection, a provider or database connection, O1, or promotion to `OBSERVER`.

Any implementation must still be proposed and authorised as a new explicitly bounded increment with named components, stores, resource limits, rollback, automatic Quality Gate and the applicable human authority. Architecture adoption does not advance the lifecycle or any MOD-12 mode.

## Documentary review record

On 2026-07-17, Bruno accepted the commit `137c889` documentation increment and this ADR as a proposed documentary architecture decision. He explicitly stated that his review was based on the increment report and did not include direct independent access to this ADR, the conceptual contract or the threat model. This was human acceptance of a documentary increment, not a canonical lifecycle Human Gate. At that time, the ADR status remained `proposed` because that was the explicit decision, not because direct file inspection is a mandatory gate.

A later direct automatic re-audit of the linked local documents strengthened bootstrap/recovery/root continuity, cumulative revocation, control/data-plane separation, quiescence before lease reuse, resource-field composition, coordinator fencing and authenticated corpus heads. Those factual corrections did not expand authority or change the lifecycle.

On 2026-07-20, after accepting the operational AIOps programme as direction, Bruno declared exactly `ADR-0007: ADOTADO COMO DECISÃO ARQUITETURAL, SEM AUTORIZAÇÃO DE IMPLEMENTAÇÃO.` This later decision changes the ADR status from `proposed` to `accepted` and nothing else. It is not a lifecycle Human Gate and does not authorise implementation, runtime, persistence, services, external action, O1 or promotion to `OBSERVER`.
