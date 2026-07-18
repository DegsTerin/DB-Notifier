# AIOps Trust Governance and Resource Envelope

## Status and authority

- Status: reviewed documentary contract supporting proposed `ADR-0007`
- Date: 2026-07-17
- Lifecycle position: `STATE-06 INTEGRATION`
- Authority: Bruno authorised a documentation-only MOD-12 increment covering responsibilities, an ADR, conceptual trust/checkpoint/resource contracts, threat modelling, traceability and future test planning
- Review record: the documentary increment was humanly approved from its report; `ADR-0007` remains `proposed` because the decision explicitly accepted it as a proposal
- Explicit exclusions: implementation, source code, migrations, runtime, real keys, persistence, services, external actions and promotion to `OBSERVER`

This document specifies a future-facing architecture contract. It does not assert that a trust distributor, durable checkpoint, resource coordinator, queue, witness or operational corpus exists. It gives no component permission to collect telemetry or activate MOD-12.

## Relationship to the current baseline

| Concern | Current observed baseline | This documentary contract | Still requires later authority and evidence |
|---|---|---|---|
| Policy provenance | Local verification of signed grants against application-supplied P-256 public anchors | Separation of policy authorisation, signature roles and distribution | Issuer, custody, provisioning and operational distribution |
| Revocation | Signed `observer-policy-revocation.v2` snapshot checked against an exact application-supplied sequence | Signed bundle, contiguous generations, durable checkpoint, quarantine and reconciliation | Store, transaction, restore integration, witness and runbook |
| Resource budget | Synchronous limits for cases, declared samples, deterministic work units and elapsed time | Encoded/expanded bytes, parser, accounted memory, work, deadline, cancellation, quotas, explicit fairness boundary and output limits | Implementation, measured values, load and memory homologation |
| Materialisation | Aggregate telemetry admission precedes enumeration; one case is copied and sorted at a time | Bounded streaming/framing and reservation before every material allocation | Parser/transport design and tests |
| Concurrency | A single call is synchronous; no shared coordinator exists | One fenced host/process coordinator plus authenticated hierarchical reservation; serial first proof | Coordinator/fencing, contention and any separately authorised fairness evidence |
| Corpus | Nine deterministic synthetic cases in three synthetic segments | Governed manifest, digest, scope, expiry and adversarial plan | Real authorised data, broader distributions, calibration and red team |
| Runtime | No MOD-12 registration, I/O, collection or persistence | Runtime remains explicitly absent | Separate proposal, authorisation, Quality Gate and Human Gate |

The current runner may inspect a bounded case collection and later copy several bounded per-case structures. That is valid evidence for its local fixture contract, not proof of streaming, peak-memory control, cancellation latency or shared backpressure.

## Scope and non-goals

This contract defines:

- trust domains, roles and incompatible responsibilities;
- a canonical signed trust-bundle model;
- durable checkpoint invariants and state transitions;
- normal rotation and exceptional compromise recovery;
- resource ceilings, reservation, charging, backpressure and fairness;
- corpus manifest governance;
- threat-to-control-to-test traceability; and
- deterministic future test vectors.

It deliberately does not select a database, table, migration, API route, message bus, vault, certificate format, signing service, witness technology, queue, worker, provider or production limit.

## Trust domains and logical scope

Continuity uses a stable domain head and a subordinate series checkpoint so an old recovery epoch cannot select an old checkpoint merely by changing the lookup key.

The stable trust-domain head key excludes series and recovery epoch:

```text
trustDomainId
  + authorityId
  + tenant/environment
  + purpose
  + policyScopeId
```

Its constant-size monotonic value stores the current positive `rootSetVersion`/digest, positive `recoveryEpochHighWater`, current `seriesId`, current domain-head digest and current subordinate-head digests. The one permitted first bootstrap initialises a positive `recoveryEpochHighWater` and initial `seriesId`; ordinary rebootstrap is forbidden. Every compromise recovery checked-increments the epoch, may select one new series and keeps that series immutable inside the epoch. Planned root rotation leaves epoch and series unchanged. A lower epoch or another series in the current epoch is rejected without consulting a separate old checkpoint. No unbounded set of superseded epochs/series is retained, no counter wraps, and exhaustion requires quarantine plus a separately authorised new trust-domain ceremony.

Every subordinate artefact binds a non-circular `epochContextId`, not the final domain-head digest:

```text
epochContextId = H(canonical stable trust-domain key
                   + recoveryEpoch + seriesId
                   + rootSetVersion + rootSetDigest
                   + bootstrap/recovery/root-transition ceremony ID and nonce)
```

The inputs are independently selected/provisioned and exclude bundle, revocation, corpus, approval-signature and final-head digests. The final domain-head digest is computed afterwards over `epochContextId`, the accepted bundle/revocation/corpus payload digests, profile and consumed ceremony ID/nonce; it excludes the ceremony approval's signature/digest. Bootstrap, recovery and root-transition approvals may therefore precommit to that exact proposed final head without a cryptographic fixed point. Any candidate component refers to the ceremony only by its preassigned ID/nonce, never by an approval signature/digest that would create a cycle.

A subordinate series checkpoint is keyed by the stable domain head plus the selected epoch and series. Historical heads belong to a bounded audit/accumulator retention policy and never participate in trust selection. Recovery atomically advances the constant-size domain high-water and the new subordinate head; an old epoch can never be treated as an uninitialised independent domain.

`policyScopeId` identifies a stable authorisation domain, not a request-created digest and not a resource-quota bucket. Every element is canonical and signed where it crosses a trust boundary. Provider names, UI labels, display names, mutable tags and caller-selected aliases cannot define or widen trust or quota keys. A package valid for one logical domain is invalid in every other domain.

### Canonical scope predicate

The first design represents a scope ceiling as a bounded, sorted set of exact canonical atoms. Each atom contains stable tenant, environment, instance, Agent and evidence-kind identifiers under a versioned schema; no alias, wildcard or inherited UI hierarchy is accepted.

For authorised ceiling `C` and requested scope `S`, acceptance requires `S` to be non-empty and every complete atom in `S` to be exactly present in `C`. Equality of a digest alone is not a subset proof: the host validates the canonical sets and predicate first, then checks their signed digests. A superset, partial overlap, parent/child alias or unknown atom fails closed. A future hierarchical scope model requires a new schema and architecture decision.

## Responsibility and identity map

| Role | Owns | May do | Must never do | Required evidence |
|---|---|---|---|---|
| Root governance authority | Trust-domain policy, root-set lifecycle, algorithm profile and exceptional bootstrap/recovery approval | Approve root-set transitions, root delegations and recovery under dual control | Issue routine grants, publish bundles or reset checkpoints alone | Threshold/dual-control approval signature over the exact transition |
| Bootstrap governance approval quorum | Independently provisioned first-install authority | Sign one exact `TrustBootstrapApproval` for a proved pristine domain | Trust a candidate-supplied key, rebootstrap an initialised domain or perform routine rotation/recovery | Independent provisioning record, threshold signature and one-use nonce |
| Root-transition approval quorum | Planned direct-successor root-set authority under the current accepted root | Sign one exact `RootSetTransitionApproval` after independent provisioning of the proposed root set | Bootstrap, recover compromise, introduce a root through a bundle or skip a root predecessor | Current-root delegation, independent provisioning record, threshold signature and one-use nonce |
| Recovery governance approval quorum | Independently provisioned out-of-band compromise authority | Sign one exact `TrustRecoveryApproval` for quarantined continuity | Rely only on the compromised/current/candidate root, issue routine updates or select an unproved old head | Out-of-band provisioning, incident evidence, threshold signature and one-use nonce |
| Root key custodian | Protected use of root signing capability | Produce a signature for a separately approved root operation | Decide policy, publish artefacts or expose private material | Custody audit and signature bound to the approval |
| Policy authority | Legitimate purpose, tenant/environment, scope ceiling and validity | Co-sign an immutable policy decision under dual control | Hold the grant-signing role, publish or advance checkpoint state | Authenticated policy-decision approval and exact ceiling |
| Policy-authorisation attestation signer | Canonical materialisation of an independently authenticated policy decision | Sign only an assertion embedding the exact policy-decision approval | Decide policy, sign grants/bundles/revocation or invent an approval/scope | Role-specific signature, complete approval artefact and delegation |
| Trust-bundle signer | Bundle generation and delegated role-key statements | Sign one canonical bundle within root delegation | Sign grants/revocation or broaden the policy authority's ceiling | Role-specific key ID and signed bundle digest |
| Grant signer | Approved policy grants | Sign only grants linked to a valid `PolicyAuthorisationAssertion` | Authorise its own scope, revoke, publish or change trust roots | Grant signature and linked assertion digest |
| Revocation authority | Irreversible revocation decisions and incident scope | Co-sign exact tombstone additions and emergency freeze under dual control | Issue grants, silently remove tombstones or replace root governance | Authenticated revocation-decision approval and incident reference |
| Revocation-decision attestation signer | Canonical proof of an approved revocation decision | Sign only the exact decision approved by the revocation authority | Create a decision, sign the resulting snapshot or remove a tombstone | Distinct role signature, approval artefact and delegation |
| Revocation snapshot signer | Canonical cumulative revocation snapshots | Sign the exact direct successor linked to approved decisions | Create decisions, omit tombstones, issue grants or reset continuity | Role-specific signature, previous-head digest and decision assertions |
| Key custodian | Operational private-key protection | Use only the role and operation approved for its key | Decide policy, publish or reuse material across roles | Custody/audit evidence and non-exportability where available |
| Publisher/assembler | Byte-for-byte delivery of public artefacts | Carry a complete signed package over any later authorised transport | Authorise, sign, select a root, reorder history or claim acceptance | Content digest and delivery metadata only |
| Host trust coordinator | Candidate validation, continuity, local commit and quarantine | Compare, reconcile, reserve and expose an immutable accepted context | Mint policy, reduce a checkpoint, trust transport or call an executor | Validation decision, CAS/transaction result and sanitised audit intent |
| Durable checkpoint owner | Protected local monotonic state | Persist and compare the exact logical checkpoint | Select policy or silently restore/decrease state | Checkpoint revision, digest, continuity/recovery evidence |
| MOD-12 verifier/evaluator | Pure deterministic verification and offline analysis | Consume an immutable verified context and bounded input | Network, file/database I/O, key custody, trust recovery or runtime activation | Deterministic outcome and sanitised refusal code |
| Independent auditor/reconciler | Cross-head comparison and protected audit review | Detect gaps, forks, restore and policy/signature mismatch | Issue policy, sign operational artefacts or choose a branch automatically | Independent head/chain evidence and review decision |
| Data-governance co-approver | Purpose, classification, retention, partition and distribution governance for corpora | Co-sign one exact `CorpusRevisionApproval` with the dataset owner | Produce corpus content, sign its manifest or activate a revision | Independent decision identity, exact approval and one-use nonce |
| Dataset owner/steward | Corpus authority, classification, expected segments and expiry | Approve an immutable corpus revision under dual control | Collect by default, sign its own manifest, broaden purpose or promote a model automatically | Authenticated corpus approval, distribution criteria and expiry |
| Corpus-manifest attestation signer | Canonical proof of one approved corpus revision | Sign only the approved manifest under a distinct root-delegated role | Produce data, approve labels, reuse policy/revocation keys or select the active revision | Role signature, approval artefact, revision chain and delegation |

### Mandatory separation

- The policy authority is distinct from grant signer, key custodian and publisher.
- Bootstrap-governance, root-transition-governance, out-of-band recovery-governance, root-signing, policy-decision, policy-attestation, trust-bundle, grant, revocation-decision, revocation-snapshot, corpus-decision and corpus-manifest signatures use distinct key identities and distinct public-key material.
- The root custodian is distinct from the publisher and routine operational signers.
- Bootstrap and recovery approval trust is independently provisioned and never selected by a candidate bundle/root. Planned root-transition approval is verified under the current accepted root-delegated governance role plus an independent provisioning record for the successor.
- The checkpoint owner cannot issue policy or reduce the checkpoint.
- The auditor/reconciler cannot issue policy, sign operational evidence or resolve a fork without an authorised recovery decision.
- Organisational deployments may assign people to roles only if the resulting identities, permissions, approvals and audit paths preserve these incompatibilities. Convenience is not an exception.

A signature demonstrates origin and integrity under a key. Legitimate authorisation additionally requires an independently authenticated governance approval, a materialised assertion, a valid root-signed role delegation and an enforced canonical scope predicate. A compromised approval quorum remains a catastrophic residual risk; an attestation signer alone is never treated as the policy or revocation decision-maker.

### Authenticated policy authorisation and role delegation

`PolicyDecisionApproval` is a canonical, threshold/dual-control signed public security artefact containing its schema/ID, exact stable trust domain, current recovery epoch, immutable series, `epochContextId`, tenant/environment, purpose, `policyScopeId`, bounded canonical scope ceiling, maximum resource ceilings, validity, decision nonce and approver quorum. `PolicyAuthorisationAssertion` embeds the complete approval or its cryptographically bound bytes/digest and adds the same epoch/series/context binding under a distinct attestation signature. The host verifies both independent signatures and exact field equality. A bare approval ID/digest is never authorisation.

`RevocationDecisionApproval` follows the same separation and exact recovery-epoch/series/`epochContextId` binding for irreversible tombstone additions, emergency freeze or signer retirement. A distinct attestation role materialises the decision, and a different snapshot signer produces the cumulative direct-successor snapshot. No valid snapshot signer may invent a decision or remove a tombstone.

`CorpusRevisionApproval` is a canonical dual-control artefact from the dataset owner and data-governance authority. It binds the stable trust domain, exact recovery epoch/series/`epochContextId`, dataset and canonical scope, exact previous revision/digest (or an independently proved first revision), canonical manifest-payload digest, exact case/partition membership digests, quantitative segment criteria, transformations, validity, withdrawal status and one-use decision nonce. The manifest-payload digest is computed over the canonical manifest payload only, excluding its digest/signature attestation envelope. The separate corpus-manifest attestation signer may materialise only those exact approved fields and cannot choose the active revision.

`TrustBootstrapApproval` is verified only under independently provisioned bootstrap-governance quorum keys. It binds the stable domain key, an independently provisioned registry entry/marker currently in the `pristine` state, independently provisioned initial root-set version/digest, initial positive recovery epoch and series, and one exact initial domain-head digest covering the initial bundle generation/digest, revocation head/sequence, authorised corpus-head set and algorithm/canonicalisation profile. It also binds purpose, validity, decision nonce and quorum. Its atomic consumption changes that existing marker to `initialised/consumed` with that exact first domain head; bootstrap can never create its own evidence of first installation and an initialised domain can never use bootstrap again.

`RootSetTransitionApproval` is verified under the current accepted root-delegated root-transition-governance role and an independent provisioning record for the proposed root set. It binds the exact current/direct-successor root versions/digests, stable domain, unchanged recovery epoch/series, resulting non-circular `epochContextId`, exact proposed domain-head digest, bounded overlap, reason, validity, decision nonce and quorum. Candidate components refer only to its preassigned ID/nonce. Before commit, every candidate approval, delegation, signer, bundle, revocation and corpus head must also validate under the proposed remaining root set. A root cannot be removed while any active candidate artefact depends exclusively on it. Atomic consumption advances the planned root and exact proposed self-verifiable subordinate head together; it never resets epoch/revocation continuity or silently retains removed-root trust.

`TrustRecoveryApproval` is verified under an independently provisioned out-of-band recovery quorum that does not rely only on the current, compromised or candidate root. It binds the stable domain, exact prior domain/revocation/corpus heads or explicit independently evidenced continuity loss, current/proposed root references, checked `oldEpoch + 1`, chosen series immutable in the new epoch, and one exact proposed domain-head digest covering the initial new-epoch bundle generation/digest, revocation head/sequence, authorised corpus-head set and algorithm/canonicalisation profile. It also binds reason, incident reference, validity, decision nonce and quorum. Its atomic consumption advances the epoch and installs only that exact proposed subordinate state.

Deletion of all local state is not proof of a first bootstrap; without an independent installation/domain registry or monotonic marker, the scope remains quarantined. Every approval signer/key set above is distinct from root signing, policy/revocation/corpus decisions and operational roles. Candidate content can never select the verification key for its own approval.

`RoleKeyDelegation` is a root-signed canonical statement binding one public-key identity/material digest to one exact role, stable trust domain, recovery epoch, immutable series, `epochContextId`, authority, tenant/environment, purpose, validity, algorithm profile and optional generation range. Root material can never be reused by any approval or operational role. An operational signer cannot delegate itself or its successor. Rotation of the trust-bundle signer or another operational key requires a root-approved delegation already valid for the transition.

Every subordinate security artefact — policy/revocation/corpus decision, assertion, delegation, grant, revocation snapshot, corpus manifest/head and trust bundle — cryptographically binds the exact recovery epoch, immutable series and non-circular `epochContextId` selected independently by the host. After recovery, every old-epoch subordinate artefact is ineligible even if its signature and ordinary validity remain sound. Intentional carry-forward requires fresh approval/reissuance under the new epoch with new identifiers and, where applicable, new key material; a publisher cannot repackage an old revoked artefact into the new namespace.

The untrusted publisher may carry assertions/delegations in the same bounded transport envelope as a bundle, but the host authenticates their distinct signatures and revocation/validity independently. A bundle reference or audit digest alone is never sufficient. A missing, fabricated, altered, expired, revoked or out-of-scope assertion/delegation rejects the candidate without advancing trust state.

## Conceptual trust-bundle contract

The future bundle is an immutable canonical byte sequence. Field names below are conceptual, not a C# or wire schema.

| Field/group | Meaning | Required invariant |
|---|---|---|
| `bundleSchemaVersion` | Shape and canonicalisation version | Exact supported major; no implicit downgrade |
| `bundleId` | Globally stable content identity | Unique and bound into the signature |
| `trustDomainId` | Root trust domain | Exact match to independently provisioned configuration |
| `rootSetVersion`, `rootSetDigest` | Independently provisioned root-set continuity | Exact current durable head, or the exact approved direct successor being atomically installed through independent provisioning and `RootSetTransitionApproval`; never selected by bundle content alone |
| `authorityId` / `seriesId` | Issuing authority and continuity series | Authority matches the stable domain head; series matches its selected subordinate checkpoint |
| `recoveryEpoch` | Explicit exceptional recovery generation | Exact current epoch or checked direct successor under a consumed recovery approval; no same-epoch series change or wrap |
| `epochContextId` | Non-circular binding to domain, epoch/series, root set and ceremony ID/nonce | Host recomputes it from independently selected inputs; it excludes candidate/final-head/approval-signature digests and cannot be supplied as authority by the bundle |
| `tenantId` / `environmentId` | Isolation boundary | Exact, signed and never wildcarded implicitly |
| `purpose`, `policyScopeId`, requested scope/digest | Permitted use, stable authorisation domain and exact requested atoms | Canonical requested set must be a non-empty subset of the independently authenticated ceiling |
| `policyDecisionApproval`, `policyAuthorisationAssertion` | Dual-control policy decision plus its separately signed canonical materialisation | Both independently authenticated, valid, unrevoked and byte-for-byte consistent for domain/purpose/ceiling |
| `roleKeyDelegations[]` | Root-signed statements for every operational signing key used | Exact role/domain/scope/validity; operational signer cannot self-delegate |
| `generation` | Positive monotonic bundle generation | Normal update is the direct successor only |
| `issuedAt`, `notBefore`, `expiresAt` | UTC validity | Ordered, bounded and checked against trusted clock policy |
| `previousBundleDigest` | Digest of the immediately accepted predecessor | Required after bootstrap; exact chain continuity |
| `operationalKeys[]` | Public keys by exact role, scope, algorithm and validity | Distinct identity/material per role; bounded cardinality |
| `retiredKeys[]` | Explicitly retired role keys | Never reactivated automatically |
| `requiredRevocation` | Snapshot schema, recovery epoch, series, sequence, previous/current digest, cumulative tombstone accumulator/set, freshness and decision assertions | Ordinary update exactly matches current epoch/series and uses a checked direct successor/complete chain; first bootstrap or approved recovery uses the approval-bound initial epoch/series/head and exact initial sequence `1`; recovery binds the old revocation head while starting a new namespace in which old-epoch artefacts are ineligible; candidate content cannot select/reset either path |
| `authorisedCorpusHeads[]` | Exact signed epoch/series/`epochContextId`/authority/dataset/scope/revision/digest selections | Bounded cardinality; current monotonic heads only; publisher cannot choose, carry forward or roll them back |
| `algorithmProfile` | Allowed signature, digest, curve and canonicalisation rules | Host allowlist may be stricter; automatic downgrade forbidden |
| `resourceCeilings` | Complete finite bundle maxima for every mandatory numeric admission dimension | Required; each bundle value must be no greater than its authenticated policy-assertion ceiling; a host-local ceiling may independently be stricter |
| Exact/maximum input declarations | Required encoded length/exact counts and maximum expanded/structure/cardinality values, classified by schema | Exact fields equal the final observation; maximum fields may be lower but never exceeded; omitted applicable fields fail closed |
| `auditReferences[]` | Stable evidence references | Bootstrap/recovery/root-transition ceremony approvals appear only by preassigned ID/nonce to avoid a digest cycle; other non-circular approvals may use IDs/digests; references never authorise by themselves |
| `bundleSignature` | Role-specific signature over canonical content | Valid only under independently rooted trust-bundle role |

The root public key is not carried as a trust-establishing field. A root identifier may be present for exact selection, but key material and trust status come from the independent provisioning boundary.

### Trust control-plane update order

Trust/revocation/corpus-head updates use dedicated host-reserved bounded admission capacity and a deterministic priority scheduling lane that evaluation work cannot consume. Data-plane capacity saturation cannot refuse a revocation or narrowing update for lack of control capacity. Host CPU/thread scheduling, GC and failure can still delay or prevent completion; the future design must enforce a control-phase deadline, while physical latency remains empirical evidence in `HM-01`.

1. Record a control-plane absolute monotonic deadline before reservation and honour pre-start cancellation.
2. Acquire only a bounded dedicated control-plane ingress lease under trusted local limits; an unauthenticated scope or bundle ceiling never selects this capacity.
3. Require exact framing where the schema declares it, first-byte/inter-read liveness, and independent encoded/expanded/ratio/structure/cardinality limits before allocation.
4. Parse only the bounded canonical structure and reject omitted required declarations, unknown critical fields or unsupported schema/profile.
5. Select exactly one validation branch from independently provisioned host state, never from candidate content:
   - **ordinary update/planned root rotation:** use the fresh current durable domain/root/revocation heads; planned rotation additionally requires the current-root-delegated `RootSetTransitionApproval` and independent provisioning record for the direct-successor root;
   - **first bootstrap:** require `Uninitialised`, an independently provisioned `pristine` marker, bootstrap-governance quorum and initial root set; or
   - **exceptional recovery:** require existing quarantine/recovery state, the independently provisioned out-of-band recovery quorum, exact old-head evidence/continuity-loss record and independently provisioned proposed root.
6. Validate the branch anchor. For ordinary processing, current root configuration older than the durable head, the same version with another digest, or root material reused by another role quarantines. For bootstrap/recovery, candidate content cannot choose the approval quorum, marker, root or expected prior state.
7. Authenticate approvals and delegations under the branch authority: current accepted root/revocation for ordinary updates; bootstrap quorum plus approved initial root for first installation; or out-of-band recovery quorum plus approved proposed root for recovery. For planned root rotation, authenticate the transition under the current root and independently revalidate every candidate subordinate artefact under the proposed remaining root set; refuse removal while any artefact depends exclusively on the removed root. The old recovery head is bound evidence, not authority for selecting a new root.
8. Authenticate revocation continuity. Ordinary updates require the current epoch/series and a direct successor or bounded complete chain. Bootstrap requires the exact approved initial epoch/series/head at sequence `1`. Recovery requires exact checked `oldEpoch + 1`, the approval-bound new series/head at sequence `1`, a new identifier namespace and freshly issued subordinate artefacts; the old revocation head remains bound recovery evidence but no old-epoch artefact remains eligible.
9. Apply the candidate revocation state to every candidate approval, assertion, delegation, signer, grant and corpus head. A signer valid for authorising its own retirement cannot be used after the transition; no candidate may exempt itself.
10. Verify the policy decision approval and its distinct `PolicyAuthorisationAssertion`, exact epoch/series/`epochContextId`, purpose/scope predicate and resource ceiling relationship.
11. Reproduce canonical bundle bytes/digest and verify the bundle signature under the exact branch-approved, delegated and unrevoked role.
12. Compare root-set, recovery-epoch, series, bundle-generation, revocation and corpus-head continuity with checked successor arithmetic. Validate every bounded catch-up hop; ordinary processing accepts no gap, wrap or branch choice.
13. Validate current/candidate freshness and trusted-clock policy without extending validity. An inactive candidate is rejected while a fresh intact active context remains; expiry/unprovable freshness of the active context quarantines it.
14. Revalidate immutable digests and atomically commit the selected root-set reference, consumed one-use approval (when applicable), bundle, revocation head, corpus heads, constant-size domain head, subordinate checkpoint, marker transition and audit intent.
15. Publish a new immutable `contextRevision` and signal cancellation of every evaluation pinned to the superseded revision. The control-plane lease is reclaimable only after its owned work is quiescent or an approved isolation fence has terminated it.

Authentication cannot make the bytes needed to authenticate free. The dedicated bounded reader limits pre-authentication cost; the design must never claim that all cost occurs only after authentication.

A candidate transport envelope may contain one bundle or a trusted-locally-bounded contiguous chain from the active head to a fresh final head. Connected pull and controlled offline import use the same canonical chain; notification hints never carry authority. Every intermediate signature, delegation, authorisation, predecessor and revocation transition is validated in order, while only the final complete tuple becomes visible through the local transaction. A numerically later bundle without the complete bounded chain is a gap, not catch-up.

Malformed, unsupported, unauthenticated, expired or not-yet-valid candidate bytes are rejected and audited without deactivating a current tuple that remains fresh and whose continuity is intact. Authenticated equivocation, an authenticated gap showing missing continuity, expiry/unprovable freshness of the current tuple, compromise evidence or damaged/restored durable state enters quarantine. Authority unavailability permits the last accepted tuple only until its existing validity/freshness expires; it never creates a local extension.

## Durable checkpoint model

### Logical value

| Value | Purpose |
|---|---|
| Stable trust-domain head key | Prevents cross-scope reuse without letting epoch/series select an older record |
| Current `rootSetVersion`/digest | Detects independent-root rollback, divergence and material reuse across roles |
| Positive `recoveryEpochHighWater` and current immutable `seriesId` | Rejects every lower epoch and same-epoch alternate series with constant-size state |
| Subordinate series checkpoint key and `generation` | Enforces monotonic continuity within the selected recovery epoch/series |
| Current bundle digest | Detects same-generation equivocation |
| Previous bundle digest | Preserves direct chain relation |
| Revocation recovery epoch/series, sequence, previous/current digest and cumulative tombstone accumulator | Ordinary state exactly matches the stable domain and advances irreversibly; approved bootstrap/recovery installs the exact approval-bound sequence-`1` head in its new namespace while binding the prior head as recovery evidence |
| Current authorised corpus head(s) | Binds exact authority/dataset/scope/revision/manifest digests and withdrawal state |
| Algorithm/canonicalisation profile | Prevents silent downgrade |
| Local concurrency revision | Supports future compare-and-swap/transaction conflict detection |
| Consumed bootstrap/recovery/root-transition decision nonce and accepted-at instant | Prevents ordinary replay of one-use governance approvals |
| Bounded audit-accumulator/intent digest and retention revision | Supports sanitised continuity evidence without unbounded live metadata |
| Continuity/reconciliation status | Distinguishes active, quarantined and recovered state |

### States

- `Uninitialised`: no trusted continuity exists; normal evidence consumption is forbidden.
- `Active`: one complete locally committed bundle/revocation/checkpoint tuple is current and fresh.
- `Quarantined`: continuity, integrity, freshness or scope cannot be proved; MOD-12 consumption is forbidden for that scope while provider-neutral deterministic monitoring remains independent.
- `Recovering`: an independently authorised reconciliation/recovery procedure is evaluating evidence; normal updates remain forbidden.
- `Retired`: the trust domain or scope is intentionally closed and cannot reactivate automatically.

### Transition rules

| Current/input | Required result | State mutation |
|---|---|---|
| `Uninitialised` + ordinary bundle | `trust.bootstrap_required` | None; remain `Uninitialised` |
| `Uninitialised` + valid unused `TrustBootstrapApproval` plus independently provisioned first-install/domain marker in `pristine` state | Validate complete root/authorisation/bundle/revocation/corpus evidence | Consume approval, change marker to `initialised/consumed` and commit one `Active` head atomically |
| `Uninitialised` + missing/expired/wrong-domain/replayed/mismatched `TrustBootstrapApproval` while independent marker is genuinely `pristine` | `trust.bootstrap_invalid` | Reject/audit; remain `Uninitialised`, marker unchanged |
| Missing local domain record without independent first-install/domain marker | `trust.continuity_unproved` | Quarantine; deletion is not treated as a new bootstrap |
| Marker is `initialised/consumed` but domain head is missing/corrupt | `trust.continuity_unproved` | Enter `Quarantined`; bootstrap replay is forbidden |
| Authenticated root-transition candidate lower than the fresh intact current root head | `trust.root_rollback` | Reject/audit candidate; preserve current `Active` tuple |
| Independently provisioned current root configuration lower than its durable head, or same root-set version with another digest/material reuse | `trust.root_rollback` / `trust.split_view` | Enter `Quarantined`; bundle cannot repair root continuity |
| Authenticated root-transition candidate at the same version with another digest, or with a missing predecessor | `trust.split_view` / `trust.generation_gap` | Enter `Quarantined`; no branch auto-wins |
| Planned root transition leaves a candidate artefact dependent only on a removed root | `trust.authorisation_unproved` | Reject candidate; preserve fresh current `Active` tuple and bounded overlap |
| Existing domain head + bundle from superseded epoch/series | `trust.epoch_superseded` | Reject; never bootstrap an old epoch as a new domain |
| Same bundle generation + same digest | Idempotent success | No logical advancement |
| Same bundle generation + different digest | `trust.split_view` | Enter `Quarantined` |
| Lower bundle generation | `trust.rollback` | Reject/audit candidate; preserve fresh current `Active` tuple |
| Direct successor + exact predecessor + valid revocation | Candidate accepted | One local commit to new `Active` head |
| Bounded complete contiguous catch-up chain | Validate every direct transition; accept fresh final head | One local commit to final `Active` head |
| Authenticated future generation with an incomplete gap | `trust.generation_gap` | Enter `Quarantined`; full chain or extraordinary reconciliation required |
| Wrong trust domain, tenant/environment, purpose or canonical scope | `trust.scope_mismatch` | Reject without changing another scope |
| Lower recovery epoch or alternate series in the current epoch | `trust.epoch_superseded` | Reject/audit candidate; preserve fresh current `Active` tuple |
| Proposed future epoch/series without the exact unused `TrustRecoveryApproval` | `trust.recovery_invalid` | Reject candidate; preserve fresh current tuple unless current continuity is independently compromised |
| Malformed/unauthenticated candidate while current tuple is fresh/intact | Reject candidate and audit | Preserve current `Active` tuple |
| Expired/not-yet-valid candidate while current tuple is fresh/intact | `trust.candidate_inactive` | Reject candidate; preserve current `Active` tuple |
| Current tuple expired or trusted clock/freshness unproved | `trust.freshness_unproved` | Enter or remain `Quarantined` |
| Revocation sequence/digest exactly equals the current head | Idempotent success | No logical advancement |
| Revocation sequence lower than the current head | `trust.revocation_rollback` | Reject/audit candidate; preserve fresh current `Active` tuple |
| Authenticated revocation at the same sequence with another digest | `trust.revocation_split_view` | Enter `Quarantined`; keep stored head unchanged and choose no branch |
| Authenticated future revocation with a missing predecessor | `trust.revocation_gap` | Enter `Quarantined`; keep stored head unchanged pending complete chain/recovery |
| Unauthenticated or non-authorising candidate removes a tombstone/reuses an identifier | `trust.authorisation_unproved` | Reject candidate; preserve fresh current `Active` tuple |
| Currently authorised snapshot signer authenticates a tombstone removal, identifier reuse or decision omission | `trust.compromise_recovery_required` | Enter `Quarantined`/`Recovering`; keep stored head unchanged |
| Revocation tombstone accumulator reaches its authenticated metadata cap | `trust.revocation_capacity` | Enter `Quarantined`/`Recovering`; no pruning, ordinary advance or evaluation |
| Checked successor of bundle generation, root-set version, revocation sequence or recovery epoch would overflow | `trust.counter_exhausted` | Enter `Quarantined`/`Recovering`; no wrap or ordinary advance |
| Missing, corrupt or restored checkpoint | `trust.continuity_unproved` | Enter `Quarantined` |
| Crash before local commit | Old complete tuple remains visible | No partial advancement |
| Crash after local commit | New complete tuple is visible idempotently | No mixed tuple |
| Root/signer compromise | `trust.compromise_recovery_required` | Enter `Recovering`/`Quarantined`; only a valid unused `TrustRecoveryApproval` may checked-advance the epoch |
| Explicit retirement | Validate authorised retirement | Commit `Retired`; no automatic reactivation |

Normal advancement accepts a direct successor or a bounded complete chain of direct successors. A gap or fast-forward is not resolved by choosing the numerically highest signed generation.

### Restore, clone and split-view limitation

A local checkpoint and local audit restored together can look internally consistent while both are old. Digest chaining detects a fork only when independent views are compared. Therefore:

- every known restore, clone, continuity loss or divergence quarantines the scope;
- resumption requires an independently authenticated current head and a contiguous chain or separately authorised recovery artefact;
- total absence is distinguishable from first installation only through an independent domain/install registry or monotonic marker; otherwise bootstrap is refused;
- no branch wins automatically by timestamp, generation or signer;
- an independent witness, monotonic platform anchor or protected external audit head is required before claiming detection of a silent full-store rollback; and
- until such a mechanism is implemented and tested, silent restore and isolated split view remain explicit residual risks.

This contract does not choose or create that independent mechanism.

## Rotation and recovery

Normal operational-key rotation preserves the root set, trust domain, recovery epoch, immutable series, authorised scope and contiguous generation. Root governance first approves and root custody signs the replacement `RoleKeyDelegation`; policy authority separately approves any policy impact. A later bundle uses a bounded overlap in which old and replacement role keys are both explicitly delegated and valid, then retires the old key in a direct successor backed by an independently authenticated root/revocation decision. The current trust-bundle signer cannot create its own successor delegation or unilaterally change another role's status.

Planned root-set rotation is a separate independent-provisioning ceremony. A valid `RootSetTransitionApproval` binds the direct previous/new root-set versions/digests, bounded overlap, exact proposed self-verifiable head, domain, validity and one-use nonce; the durable domain head advances atomically with the independently provisioned direct successor. Before commit, every candidate subordinate artefact validates under the proposed remaining root set; removal waits until no candidate artefact depends exclusively on the retiring root. A bundle cannot introduce or silently retain a root. Replay of an authenticated lower root candidate is rejected/audited while a fresh intact current root remains active. An independently provisioned current root below its durable head, an authenticated same-version divergent candidate, missing continuity or reuse of root material in an operational role quarantines the domain. Compromise of the current root requires an out-of-band quorum that does not rely only on the compromised root.

Compromise recovery always checked-increments `recoveryEpochHighWater` and may select a new series; a series never changes inside one epoch. It requires root governance, an unused `TrustRecoveryApproval`, incident-response evidence, quarantine and independent reconciliation. The approval binds the expected old head (or explicit continuity-loss evidence), new root-set reference, exact next epoch/series, reason and validity. Its atomic consumption permanently supersedes every lower epoch before the new subordinate checkpoint becomes active. Counter exhaustion or wrap remains quarantined and requires a separately authorised new trust-domain ceremony. Compromise recovery cannot be disguised as routine rotation.

Every ordinary revocation head exactly matches the stable domain's current recovery epoch and immutable series; its positive sequence advances with checked arithmetic. Revocation tombstones are cumulative and irreversible within that epoch for grants, policy approvals/assertions, delegations, operational keys, bundles and corpus approvals/heads. Identifiers are never reused across epochs. Snapshot cardinality is finite; reaching the cap requires a controlled recovery under `TrustRecoveryApproval` into a checked higher epoch, sequence-`1` approval-bound head and new identifier namespace, never an ordinary reset. The prior revocation head is bound into the recovery approval while every old-epoch subordinate artefact becomes ineligible. Pruning live tombstones is forbidden. Older history may move only to a bounded authenticated audit accumulator after the epoch high-water makes it ineligible for trust selection.

## Resource envelope

### Sources and composition of limits

Limits do not all compose by taking a numerical minimum. The future coordinator must first validate the complete, version-compatible declarations and then use the rule for each field class:

| Field class | Composition rule | Invalid input |
|---|---|---|
| Numeric ceilings | `min(localTrustedCeiling, policyAssertionCeiling, bundleCeiling, requestLimit)` | Reject if the policy assertion is unauthorised, the bundle value exceeds its policy-assertion ceiling, or the request exceeds the already trusted minimum of local, policy and bundle values |
| Allowlists and permitted scope atoms | Canonical set intersection | Reject an empty intersection or any item outside the authenticated policy assertion |
| Capabilities and safety switches | Logical AND; any deny remains deny | Reject an attempted enablement not present in every authoritative source |
| Schema, algorithm and accounting profiles | Exact match or an explicitly approved compatibility-matrix entry | Reject an implicit conversion, unknown profile or downgrade |
| Queue state | Explicit `disabled` sentinel for the first proof | Reject a positive queue depth; queueing requires separate authority |

The sources have different authority:

- `localTrustedCeiling` is selected by the future host through trusted configuration and may be stricter than any portable policy or bundle.
- `policyAssertionCeiling` is a complete finite group approved through `PolicyDecisionApproval` and bound by `PolicyAuthorisationAssertion`.
- `bundleCeiling` is bound by the bundle signature and must be less than or equal to its policy-assertion value. It need not be less than a different host's local ceiling; the local minimum still controls that host.
- `requestLimit` is complete for every applicable dimension. A value above the trusted local/policy/bundle result is refused instead of silently clamped, so the caller cannot mistake a different execution envelope for the requested one.
- A producer declaration is evidence to check, not a source of authority and never raises a limit.

Every applicable numeric field must be present, positive, finite, checked for overflow and compatible with the selected profile. Missing, zero, negative, unbounded or incompatible values fail closed; absence is never represented as infinity. The disabled-queue sentinel is the narrow non-positive exception and is not one of the mandatory numeric groups.

Producer declarations state either an **exact** value or a **maximum** value. Exact values must equal the observed final counter; maximum values permit a smaller observation but never a larger one. Omitting an applicable declaration fails closed. The manifest schema identifies which form applies, so a producer cannot change it per item.

Accounting separates five kinds of state:

- **Reusable capacity leases:** active slots, accounted-memory reservations and bounded buffers/output capacity. Lease expiry requests cancellation but does not make capacity reusable. Release occurs exactly once only after the owned work has proved quiescent or a separately authorised isolation fence has terminated it.
- **Cumulative execution consumption:** observed bytes, cardinality, deterministic work and elapsed time. It only increases and is never returned during the execution.
- **Window quota tokens:** authenticated hierarchical bytes/work/admission allowance. Unused reservation may be returned; consumed allowance replenishes only by its trusted time-window policy.
- **Control-plane metadata:** constant-size active trust/corpus heads plus bounded authenticated accumulators and retention records. Crash-safe compaction must not remove a tombstone or resurrect retired authority.
- **Audit counters:** monotonic sanitised reserved/consumed/rejected/released values; they are evidence, not capacity.

Observed heap or working-set memory is empirical calibration evidence only. It is not a deterministic admission dimension and cannot substitute for accounted-memory reservation.

### Dimensions and accounting

| Dimension | Unit and scope | Reservation/charging rule | Future evidence required |
|---|---|---|---|
| Encoded input | Bytes per item and execution | Count while reading before buffer growth | Boundary tests and bounded-reader proof |
| Expanded input | Bytes per item and execution | Independent cap during decompression/decoding | Expansion-bomb tests |
| Expansion ratio | Expanded/encoded ratio | Stop before either ratio or byte cap is crossed | Compressed adversarial fixtures |
| Structure | Depth, tokens, fields, strings, arrays and key entries | Bound before constructing nested objects | Parser/cardinality tests |
| Cases/samples/segments | Counts per item, execution and scope | Compare declared and observed monotonic counters | Mutable/lying/infinite-source tests |
| Deterministic work | Versioned units per framing, byte, parse, signature, lookup, sort, copy, analysis and result | Reserve before each operation; non-linear work has a safe upper bound | Model review and boundary vectors |
| Accounted memory | Versioned conservative `accounted bytes` for input, parser, objects, sort/index, current case and result | Formula/profile is fixed for runtime/architecture; reserve simultaneous peak before allocation | Formula tests plus allocation instrumentation |
| Observed memory | Heap/working-set peak | Measurement only for `HM-02`; never a direct admission boundary or substitute for deterministic reservation | Reproducible load environment |
| Total deadline | Absolute monotonic time from admission request before any reservation through finalisation | Expired when `now >= deadline`; never renewed by progress or retry | Virtual-clock decision tests |
| Ingress lease/read liveness | Reservation lease, time to first byte and maximum idle interval between bounded reads | Deadline requests cancellation; reservation is held until quiescence is proved or an authorised fence terminates the work | Silent/non-cooperative-source and fencing tests plus later latency measurement |
| Phase deadline | Bounded read/parse/verify/sort/analyse/finalise windows | Must fit inside total deadline | Phase-stall vectors |
| Cancellation latency | Monotonic time/chunks after cancellation is observable | Check before phases and between bounded chunks; start no new work | Injected cancellation at every phase |
| Concurrency | Active slots and reusable capacity within one fenced coordinator on one host/process, then by authenticated hierarchy | Atomic acquisition before work; a second coordinator owner is rejected; reusable leases release only after quiescence/fencing | Contention, coordinator-fencing and restart tests |
| Fairness/window quotas | Admission/bytes/work tokens at coordinator-global → tenant/environment → authority/principal → purpose → exact-scope buckets | Acquire all atomically; parent buckets prevent fragmentation; the first serial proof claims containment only, not fairness | Virtual window/metadata tests |
| Future queue | Depth, bytes and maximum age | Disabled by default; separately authorised, bounded and observable | Queue-specific gate if ever proposed |
| Control-plane metadata | Active trust/corpus heads, irreversible revocation accumulator, one-use approval nonces and bounded audit/retention state | Constant-size active heads plus authenticated bounded retention; compaction is atomic and cannot resurrect retired state | Capacity, crash and compaction vectors |
| Output | Result count and encoded/accounted-memory size | Reserve count, bytes and accounted memory before building the report; diagnostics may be bounded, but factual truth is never truncated into a passing result | Oversized-result vectors |

All additions and multiplications use checked arithmetic. Overflow is a refusal, not silent saturation. The work model must account for cryptographic verification and sorting; elapsed time is not a substitute for deterministic work.

### Admission and backpressure sequence

Trust, revocation, root and corpus-head updates use bounded host-reserved control admission capacity and a deterministic priority lane that evaluation work cannot consume. A valid control update commits independently of data-plane admission. Saturated evaluation capacity therefore cannot consume or cause refusal of the control reservation; physical scheduling/liveness still requires a bounded phase deadline and later `HM-01` evidence. A new evaluation cannot start while its required control context is pending or quarantined.

For each evaluation:

1. Pin one immutable active `contextRevision` containing the trust checkpoint, root/revocation heads, policy assertion and authorised corpus head. Record an absolute monotonic deadline before any reservation. It is no later than the request deadline or the earliest applicable policy, bundle, revocation, corpus, delegation or freshness deadline.
2. If cancellation is already requested, the context is not active or the deadline has expired, refuse without evaluation acquisition.
3. Validate the local bootstrap envelope, field-class composition and exact/maximum declarations. A bundle above its policy assertion or a request above the trusted effective envelope is invalid rather than silently clamped.
4. Acquire a short coordinator-scoped ingress lease plus only an independently authenticated ingress-principal bucket when one exists.
5. Require a cancellation/deadline-aware bounded source. Apply first-byte and inter-read idle deadlines, encoded/expanded byte limits and parser-structure limits before materialising telemetry or corpus cases.
6. Authenticate the immutable data manifest under the pinned context and derive its canonical resource-quota hierarchy; an untrusted claimed scope never chooses pre-authentication quota.
7. Atomically acquire every authenticated parent/child quota and reusable-capacity lease. If any acquisition fails, roll back all acquisitions and retain ingress capacity until owned work is quiescent.
8. Reconcile exact declarations for equality at end-of-input and maximum declarations as monotonic upper bounds. Reserve accounted memory, result count/bytes and cumulative work/byte allowance before each relevant operation.
9. Process only bounded cooperative chunks/current cases, checking absolute, phase and idle deadlines, cancellation and context supersession between them. A non-pre-emptible primitive requires an independently authorised terminable isolation boundary.
10. Charge cumulative consumption monotonically. Cumulative consumption never decreases, and consumed window tokens replenish only by policy.
11. On refusal, exception, timeout, cancellation or context supersession, request cancellation. On every outcome, prove owned work quiescent or terminate the authorised isolation fence. Only then release reusable leases and unused window reservation exactly once. Never return consumed execution/window allowance as unused.
12. Immediately before publication, revalidate that the exact pinned `contextRevision` is still active, fresh and unrevoked. A changed head makes the result `IncompleteNonAuthorising` with `trust.context_superseded`; no stale evaluation can authorise a gate or update.
13. Publish a passing result only after the whole authorised execution completes and its truthful output fits the admitted count, byte and accounted-memory envelope.

No queue, retry, continuation, partial acceptance or budget increase is implicit. Capacity unavailable at admission produces deterministic backpressure. Lease expiry is a cancellation signal, not proof that capacity is free. A cooperative component that fails to become quiescent keeps its reservation and is quarantined; reusable capacity may be recovered only through a proved isolation fence. A byte/count cap alone is not latency evidence.

### Initial concurrency and fairness policy

- The first separately authorised local proof uses `maximum parallelism = 1` inside one fenced coordinator on one host/process. The word `global` means that coordinator's accounting domain; it makes no fleet-wide or cross-host claim.
- Coordinator ownership has a unique fenced identity. A second live or restored owner is rejected rather than creating another apparent global slot.
- No backlog or contender registry is retained. A second admission is rejected while the slot is occupied, so the first proof demonstrates containment only and makes no deterministic rotation or starvation-freedom claim.
- Authorisation and quota keys are different: the exact policy scope proves permission, while hierarchical parent buckets aggregate all child scopes for capacity.
- All coordinator-global, tenant/environment, authority/principal, purpose and exact-scope buckets are acquired atomically. A child scope cannot evade a consumed parent ceiling by changing a digest, alias, provider label or request partition.
- Creating a quota-ledger entry consumes bounded parent/coordinator metadata capacity. Entry cardinality and lifetime are finite; an expired entry is removed only after all work and leases are quiescent, and crash-safe compaction cannot resurrect consumed quota or retired authority.
- A future fairness claim requires a separately authorised bounded contender registry or queue, deterministic scheduling rules and virtual-time evidence. Cross-restart window-quota or fairness claims additionally require replay-resistant durable quota state; without it, the claim is limited to one fenced process lifetime and startup fails closed where continuity is required.

### Partial and interrupted results

Only a complete run may have a passing disposition. An interrupted run may report fully completed case identifiers and sanitised consumption counters for diagnosis, but:

- its disposition is `IncompleteNonAuthorising`;
- `Passed`, corpus acceptance, calibration and every promotion signal remain false;
- precision, recall, calibration and every aggregate derived from the completed subset are neither calculated nor serialised; the fields are absent/null, not merely excluded from gate evidence;
- incomplete output cannot be resumed or merged automatically; and
- any manual retry is a new execution with a new admission decision.

An accepted trust, revocation, root or corpus-head update cancels evaluations pinned to the superseded context. Even if their computation finishes, their results remain non-authorising and are not published as current evidence.

Cancellation and deadline are distinct. Before acquisition, cancellation is checked first, then `now >= deadline`, then capacity. At each later serialised checkpoint, an already detected trust/integrity/source violation wins, followed by an already detected arithmetic/resource-limit breach, cancellation and then deadline; no extra work is performed merely to search for a higher-priority cause. Time strictly before the deadline may proceed, while equality or later is expired. Thus simultaneous cancellation and deadline resolves as cancellation unless a higher-priority violation was already detected. The current local runner's different behaviour remains evidence only for its existing contract.

### Conceptual refusal codes

These names are design vocabulary, not implemented API values:

| Category | Codes |
|---|---|
| Trust | `trust.bootstrap_required`, `trust.bootstrap_invalid`, `trust.authorisation_unproved`, `trust.schema_unsupported`, `trust.algorithm_unsupported`, `trust.algorithm_downgrade`, `trust.rollback`, `trust.epoch_superseded`, `trust.split_view`, `trust.generation_gap`, `trust.scope_mismatch`, `trust.candidate_inactive`, `trust.freshness_unproved`, `trust.continuity_unproved`, `trust.root_rollback`, `trust.revocation_rollback`, `trust.revocation_split_view`, `trust.revocation_gap`, `trust.revocation_capacity`, `trust.counter_exhausted`, `trust.recovery_invalid`, `trust.context_superseded`, `trust.compromise_recovery_required` |
| Admission | `resource.envelope_invalid`, `resource.schema_unsupported`, `resource.capacity_global`, `resource.capacity_scope`, `resource.coordinator_conflict`, `resource.control_metadata`, `resource.quota_window`, `resource.encoded_bytes`, `resource.expanded_bytes`, `resource.expansion_ratio`, `resource.structure`, `resource.cardinality`, `resource.memory`, `resource.work`, `resource.output` |
| Execution | `resource.first_byte_timeout`, `resource.idle_timeout`, `resource.phase_timeout`, `resource.source_not_cancellable`, `resource.quiescence_unproved`, `resource.deadline`, `resource.cancelled`, `resource.source_mismatch`, `resource.arithmetic_overflow` |
| Corpus | `corpus.authorisation_unproved`, `corpus.rollback`, `corpus.split_view`, `corpus.generation_gap`, `corpus.revision_exhausted`, `corpus.scope_mismatch`, `corpus.expired`, `corpus.withdrawn`, `corpus.manifest_mismatch`, `corpus.partition_invalid`, `corpus.integrity_unproved` |

Diagnostics identify the failed dimension and policy version without including telemetry content, private material or sensitive topology.

## Governed offline corpus manifest

Digest checks alone do not authenticate a corpus because an attacker could replace the manifest, content and both digests together. Every future corpus revision therefore requires an independently approved canonical manifest payload plus a signed attestation envelope under a root-delegated `corpus-manifest-attestation` role. The envelope carries the manifest-payload digest computed over canonical payload bytes that exclude the envelope itself, preventing self-reference. That role is distinct from the corpus producer, label reviewer, policy approval, revocation and operational bundle-signing roles.

The durable checkpoint holds a bounded authenticated registry with one constant-size authorised corpus head per admitted dataset/scope: `datasetId`, canonical scope, positive `corpusRevision`, `manifestDigest`, `previousManifestDigest`, status and applicable root/role-delegation version. Registry cardinality and bytes consume the control-metadata ceiling. A first revision requires a valid one-use `CorpusRevisionApproval` bound to an independently proved first-revision marker; each normal update is the direct successor. A lower revision is rollback, the same revision with another digest is split view, and a skipped predecessor requires a bounded complete chain or extraordinary recovery. Withdrawal is irreversible within an epoch, dataset identifiers are not reused, and a withdrawn revision cannot be replayed as active.

Every immutable manifest contains:

- stable trust domain, recovery epoch, immutable series/`epochContextId`, dataset identifier, positive corpus revision, previous-manifest digest, dataset schema version and independent content version;
- owner/steward, independently authenticated approving authority, attestation role/delegation and validity interval;
- source category, consent/authority, permitted purpose and tenant/environment scope;
- classification, redaction decision, retention class and expiry;
- canonical content digest; the separately signed attestation/head carries the non-self-referential manifest-payload digest;
- exact case identifiers and content digests, exact partition membership and prohibited cross-partition duplicates;
- provider/version/platform/topology segments, explicitly described as fixtures or observed data, with quantitative expected counts/distribution criteria;
- time range, inclusion/exclusion criteria and transformations;
- label origin, reviewer and known uncertainty;
- known bias, missing segments, distribution and duplication/leakage checks;
- adversarial cases, holdout rules and prohibited reuse; and
- supersession/withdrawal status and irreversible withdrawal tombstones.

The current accepted root/role and corpus head authenticate a candidate transition; the candidate cannot legitimise its own signer or erase its predecessor. Evaluation reads only the exact manifest/content pair named by the active `contextRevision`. A valid signature or digest does not make labels correct. Expired, cross-scope, duplicated, tampered, withdrawn, unreviewed, distribution-incompatible or purpose-incompatible material is quarantined. Production feedback never updates the corpus, policy or model automatically. This document neither creates nor enlarges a corpus.

## Threat, control and future-test traceability

| ID | Threat | Required control | Future vector(s) | Accountable owner | Enforcer/reviewer |
|---|---|---|---|---|---|
| `M12-T01` | Unauthorised issuance under a valid signer | Independently authenticated dual-control policy decision, separate assertion attestation, canonical subset predicate and delegated signer role | `TR-10`, `TR-16` | Policy authority | Host trust coordinator / independent auditor |
| `M12-T02` | Rollback | Stable epoch high-water, direct predecessors and monotonic trust, root, revocation and corpus heads | `TR-01`, `TR-02`, `TR-17`, `TR-20`, `TR-23`, `CO-07` | Host trust coordinator | Checkpoint owner / independent auditor |
| `M12-T03` | Freeze | Bounded validity/freshness and no implicit extension | `TR-07`, `CO-03` | Policy authority | Host trust coordinator |
| `M12-T04` | Fast-forward/generation gap | Direct successor or bounded complete chain; extraordinary one-use recovery for unresolved gaps | `TR-05`, `TR-18`, `TR-20`, `TR-23`, `CO-07` | Host trust coordinator | Root governance authority |
| `M12-T05` | Split view/equivocation | Same-generation digest check, independent reconciliation and no automatic branch choice | `TR-03`, `TR-14`, `TR-20`, `TR-23`, `CO-07` | Independent auditor | Host trust coordinator |
| `M12-T06` | Cross-scope replay | Signed exact tenant/environment/purpose, canonical subset predicate and stable domain key | `TR-08`, `TR-16`, `CO-03` | Policy authority | Host trust coordinator |
| `M12-T07` | Operational signer compromise | Distinct keys/roles, root delegation, independently approved ceilings, cumulative revocation and quarantine | `TR-10`, `TR-12`, `TR-13`, `TR-20` | Security architecture | Host trust coordinator / incident response |
| `M12-T08` | Root/revocation compromise | One-use out-of-band recovery approval, mandatory epoch increment, root-set head, dual control and full quarantine | `TR-13`, `TR-20`, `TR-22`, `TR-23` | Root governance authority | Incident response / independent auditor |
| `M12-T09` | Key/algorithm confusion or downgrade | Role/material separation, root delegation, allowlist and explicit profile migration | `TR-09`, `TR-11`, `TR-12`, `TR-23`, `CO-06` | Security architecture | Host trust coordinator |
| `M12-T10` | Clock manipulation | Trusted monotonic/UTC policy, bounded skew and fail-closed freshness | `TR-07` | Host platform owner | Host trust coordinator |
| `M12-T11` | Partial update, crash or TOCTOU | Immutable digests, revalidation and one local CAS/transaction per complete head transition | `TR-06`, `TR-20`, `TR-23`, `CO-07` | Host trust coordinator | Checkpoint owner |
| `M12-T12` | Restore, clone, absence or corruption | Independent first-install marker, quarantine, one-use recovery approval and monotonic epoch high-water | `TR-04`, `TR-14`, `TR-17`, `TR-21`, `TR-22` | Durable checkpoint owner | Independent auditor |
| `M12-T13` | Audit or retention tampering | Protected authenticated accumulator/head, bounded retention and independent comparison | `TR-15`, `RE-33` | Independent auditor | Host trust coordinator |
| `M12-T14` | Lying, mutable, silent, infinite or non-quiescent source | Cancellation-aware bounded reader, liveness deadlines, observed counters and termination fencing | `RE-03`, `RE-04`, `RE-15`, `RE-28`, `RE-30` | Host resource coordinator | MOD-12 owner |
| `M12-T15` | Bundle/parser/decompression exhaustion | Independent byte/ratio/structure caps and exact/maximum declarations before allocation | `RE-05`–`RE-08`, `RE-26`, `RE-35` | Host resource coordinator | Parser/security reviewer |
| `M12-T16` | Memory exhaustion or unsafe lease reuse | Versioned accounted-memory reservation, proved quiescence and separately measured empirical peak | `RE-09`, `RE-10`, `RE-30`, `RE-36`, `HM-02` | Host resource coordinator | Performance/homologation owner |
| `M12-T17` | Work undercharge, invalid composition or overflow | Versioned work model, field-class composition and checked arithmetic | `RE-01`, `RE-02`, `RE-11`, `RE-12`, `RE-23`, `RE-31` | MOD-12 owner | Security reviewer |
| `M12-T18` | Cancellation/deadline ignored | Bounded cooperative chunks, phase/total deadlines, liveness lease, fencing and deterministic precedence | `RE-13`–`RE-15`, `RE-27`, `RE-28`, `RE-30`, `HM-01` | MOD-12 owner | Host resource coordinator |
| `M12-T19` | Concurrent oversubscription or false fairness claim | One fenced coordinator domain, atomic hierarchical reservations, parent window quotas and explicit no-fairness boundary | `RE-16`–`RE-19`, `RE-24`, `RE-29`, `RE-34` | Host resource coordinator | Independent fairness reviewer |
| `M12-T20` | Authorising partial, stale or oversized result | Complete-only pass, exact active context revalidation, absent subset aggregates and admitted truthful output | `RE-20`, `RE-21`, `RE-25`, `RE-32`, `TR-24` | Evaluation owner | Quality reviewer |
| `M12-T21` | Corpus replacement, poisoning, rollback or bias | Independently approved signed manifest, monotonic corpus head, exact membership, withdrawal and segment evidence | `CO-01`–`CO-08` | Dataset owner/steward | Data governance reviewer |
| `M12-T22` | Sensitive diagnostic leakage | Stable sanitised codes and no content/private material | `RE-22` | Security and privacy owner | Evaluation owner |
| `M12-T23` | Trust update starved by evaluation or stale-context publication | Dedicated control admission/scheduling lane, independent commit, context cancellation and publication-time revalidation | `TR-24`, `RE-32`, `HM-01` | Host trust coordinator | Security reviewer / evaluation owner |
| `M12-T24` | Control metadata exhaustion, unsafe compaction or duplicate coordinator | Bounded constant-size heads, authenticated retention/compaction and unique fenced ownership | `RE-18`, `RE-33`, `RE-34` | Host resource coordinator | Checkpoint owner / independent auditor |

## Deterministic future test plan

No vector in this section has been implemented or executed. Contract vectors use fixed clocks/schedulers, bounded cooperative fixtures and sanitised evidence. Real latency and memory claims are separated into later empirical homologation campaigns.

### Trust and checkpoint vectors

| ID | Precondition/input | Expected decision/code | Work/memory constraint | Required evidence |
|---|---|---|---|---|
| `TR-01` | Active generation `n`; valid signed `n-1` presented | Reject `trust.rollback`; remain at `n` | No semantic evaluation or checkpoint write | Before/after head and refusal audit |
| `TR-02` | Active `n`; exact `n` and digest replayed | Idempotent success | No new allocation beyond bounded verification; no advance | Unchanged checkpoint revision/head |
| `TR-03` | Active `n`; same generation with another valid digest | Quarantine `trust.split_view` | Stop before MOD-12 consumption | Both digests and quarantine transition |
| `TR-04` | Previously initialised store is missing, corrupt or known restored while the independent installation marker remains | Quarantine `trust.continuity_unproved` | No last-known-valid consumption | Install marker, restore/corruption fixture and state |
| `TR-05` | Active `n`; valid signed `n+2` without `n+1` chain | Quarantine `trust.generation_gap` | No blind fast-forward | Gap evidence and unchanged head |
| `TR-06` | Crash/fault injected before, during and after local commit | Only old-complete or new-complete tuple visible | Reservation released exactly once | Transaction/CAS trace and restart state |
| `TR-07` | Expired/not-yet-valid candidate while current tuple is fresh; separately, active tuple expires or the trusted clock regresses | Reject/audit the stale candidate and preserve `Active`; quarantine the stale active tuple with `trust.freshness_unproved` | Bounded verification only | Virtual-clock trace, candidate result and unchanged/quarantined head as applicable |
| `TR-08` | Valid bundle replayed across tenant/environment/purpose/scope | Reject `trust.scope_mismatch` | No telemetry read | Signed scope versus requested scope |
| `TR-09` | Root/policy-decision/policy-attestation/revocation/corpus/bundle material reused, wrong-role signature, or required `RoleKeyDelegation` missing/forged/expired/revoked | Reject `trust.authorisation_unproved` before state change | Bounded cryptographic work | Root/delegation/role/key identifiers and sanitised code |
| `TR-10` | Policy assertion lacks its independently authenticated dual-control `PolicyDecisionApproval`, or a valid operational signer exceeds the approved ceiling | Reject `trust.authorisation_unproved` | No checkpoint advance | Decision approval, assertion, canonical sets and subset result |
| `TR-11` | Unsupported schema, unsupported algorithm or attempted profile downgrade | Reject `trust.schema_unsupported`, `trust.algorithm_unsupported` or `trust.algorithm_downgrade`, respectively | Bound parsing and signature attempts | Selected/current/candidate profile and exact refusal code |
| `TR-12` | Normal key overlap then retirement | Accept contiguous generations only | Old key unusable after retirement | Chain, validity windows and final key set |
| `TR-13` | Compromised operational signer, approval quorum or root/revocation authority | Ordinary chain cannot self-recover; affected domain quarantines | No normal consumption during recovery | Incident classification and separate recovery/root-set approval evidence |
| `TR-14` | Two consumers receive different signed branches | Detect on authenticated reconciliation; neither branch auto-wins | No cross-branch consumption after detection | Independent heads and reconciliation result |
| `TR-15` | Audit entries deleted, changed, reordered or restored | Current continuity-head/intent mismatch quarantines; unrelated diagnostic-history tamper opens an incident while an independently intact current head remains active | No trust state repair from audit alone | Classified audit-chain/head comparison |
| `TR-16` | Missing/fabricated/altered/expired policy assertion; valid scope alias, wildcard, parent/child, overlap or superset attempt | Reject `trust.authorisation_unproved`/`scope_mismatch`; only canonical non-empty subset passes | No checkpoint advance or telemetry read | Independent assertion signature/delegation and atom-by-atom predicate |
| `TR-17` | Recovery advances `recoveryEpochHighWater`, then a valid old epoch/alternate series or old approval/assertion/delegation/grant/revocation/corpus artefact is replayed/repackaged | Reject `trust.epoch_superseded`/`trust.authorisation_unproved`; constant-size epoch high-water never decreases, series is immutable and no old subordinate artefact is eligible | No old checkpoint lookup or old artefact reuse as bootstrap | Domain head before/after, artefact epoch/series/`epochContextId` bindings and comparison |
| `TR-18` | Offline/connected consumer at `n` receives bounded complete chain `n+1..n+k`; variants omit an authenticated generation, tamper an intermediate signature, or request a successor/wrap from maximum bundle generation | Complete chain advances atomically; authenticated omission returns `trust.generation_gap` and quarantines; unauthenticated/tampered candidate rejects and preserves a fresh intact current tuple; checked overflow returns `trust.counter_exhausted` and quarantines without commit | Bound chain bytes/generations; no intermediate exposure or wrap | Per-hop validation, checked generation arithmetic, resulting state and one final commit only on success |
| `TR-19` | Malformed, unsupported or bad-signature candidate arrives while current tuple is fresh/intact | Reject/audit candidate; current tuple remains `Active` | Bounded ingress only | Current head/freshness unchanged and refusal evidence |
| `TR-20` | Revocation candidate has wrong domain epoch/series, lower sequence, same sequence/different digest, a gap, removed tombstone, reused identifier, revoked decision/delegation, full accumulator or checked sequence overflow | Wrong epoch/series rejects by `trust.epoch_superseded`/`trust.recovery_invalid`; lower rejects `trust.revocation_rollback` preserving fresh current; same-sequence divergence/gap quarantines by `trust.revocation_split_view`/`trust.revocation_gap`; unauthorised removal rejects `trust.authorisation_unproved`, while removal authenticated by the current role quarantines by `trust.compromise_recovery_required`; cap/overflow quarantines by `trust.revocation_capacity`/`trust.counter_exhausted`; a valid direct successor adds tombstones atomically | Bounded chain and accumulator; no candidate self-authorisation or wrap | Domain epoch/series, previous/current digests, decision approval, sequence arithmetic, tombstone accumulator and resulting head |
| `TR-21` | Genuinely pristine marker receives missing/expired/wrong-domain/replayed/mismatched `TrustBootstrapApproval`; separately, a valid one-use exact-head approval is present; separately, an `initialised/consumed` marker has a missing head | Invalid approval rejects `trust.bootstrap_invalid` and remains `Uninitialised`; only the valid one-use tuple changes marker and head atomically; initialised marker plus missing head quarantines `trust.continuity_unproved` | One bounded bootstrap transaction; no self-created marker | Approval nonce, marker before/after, exact domain/root/head binding and replay result |
| `TR-22` | Recovery approval is missing, replayed, does not bind old/new heads, fails to increment the epoch, changes series within an epoch, carries an old-epoch subordinate artefact forward, or would overflow the epoch | Invalid approval/carry-forward quarantines `trust.recovery_invalid`/`trust.authorisation_unproved`; checked overflow quarantines `trust.counter_exhausted`; only a valid one-use approval atomically installs the exact new root/head at `oldEpoch + 1` with freshly issued subordinate artefacts | Constant-size head; no ordinary evaluation, old-namespace eligibility or wrap | Approval fields/nonce, checked arithmetic, reissued artefact bindings and before/after domain head |
| `TR-23` | Root-set candidate rolls back, presents same version/different digest, skips a predecessor, reuses subordinate key material, lacks approval, prematurely removes a root still exclusively required by an active candidate artefact, or would overflow; independently provisioned current root is below its durable head; separately, a valid bounded-overlap rotation is presented | Lower replay rejects `trust.root_rollback` preserving fresh current; divergence/gap quarantines `trust.split_view`/`trust.generation_gap`; invalid approval/material/orphaned delegation rejects `trust.authorisation_unproved` preserving current; provisioned-current rollback quarantines `trust.root_rollback`; overflow quarantines `trust.counter_exhausted`; valid candidate revalidates fully under proposed roots and advances atomically | Bounded root set/profile; no orphaned trust or wrap | Old/new root arithmetic, approval, provisioning state, proposed-root validation of every candidate artefact and final head |
| `TR-24` | A valid root/revocation/policy/corpus update arrives while an evaluation is admitted and data-plane capacity is saturated | Reserved control admission and deterministic virtual priority schedule process it within the configured control-phase bound, cancel the old context and refuse stale publication with `trust.context_superseded`; new work pins only the new revision | Control capacity remains bounded; physical latency is not inferred | Virtual scheduling, commit/cancellation/publication ordering and exact context revisions |

### Resource and backpressure vectors

| ID | Precondition/input | Expected decision/code | Work/memory constraint | Required evidence |
|---|---|---|---|---|
| `RE-01` | Each numeric limit at `max-1`, `max`, `max+1` | First two follow policy; last rejects the corresponding stable `resource.*` dimension code | Never consume above effective limit | Reserved/consumed counters and exact code |
| `RE-02` | Valid numeric values combine with a stricter local host; separately, bundle exceeds policy assertion or request exceeds trusted local/policy/bundle result | Valid values use the numeric minimum; invalid bundle/request rejects `resource.envelope_invalid` instead of clamping | No extra reservation | Raw four-source values, validation result and effective limit |
| `RE-03` | Declared count smaller/larger than observed | Reject `resource.source_mismatch` on mismatch | Stop at bounded next item | Declared/observed counts and read count |
| `RE-04` | Mutable or infinite cooperative source | Reject deterministically | Bounded reads and virtual cancellation checkpoints | Reader trace |
| `RE-05` | Encoded bytes at limit and one byte over | Over-limit input rejects `resource.encoded_bytes` | No buffer beyond reserved cap | Bounded-reader byte count |
| `RE-06` | Small encoded input expands above byte cap or ratio | Reject `resource.expanded_bytes` or `resource.expansion_ratio` for the first crossed boundary | Stop expansion before exceeding reservation | Encoded/expanded counters and selected boundary |
| `RE-07` | Depth/string/collection/cardinality at and above cap | Above-cap input rejects `resource.structure`/`resource.cardinality` | No oversized object graph | Parser counters and allocation trace |
| `RE-08` | One valid item would make aggregate exceed limit | Reject before allocating/processing that item | Previous complete work only | Per-item reservation decision |
| `RE-09` | Versioned accounted-memory formula exactly available then short by one accounted byte | Exact admission succeeds; short rejects `resource.memory` | No relevant allocation before rejection | Profile/formula and reusable-capacity ledger |
| `RE-10` | Success, refusal, exception, deadline and cancellation with owned work proved quiescent | Reusable leases/unused reservation release exactly once after quiescence; consumed cumulative/window allowance does not roll back | Reusable totals return to baseline only after termination evidence | Separate capacity/consumption/window ledgers and quiescence record |
| `RE-11` | Maximum bounded crypto, parse, sort and analysis inputs; separately, the next operation would exceed reserved work by one unit | Work model charges each operation, including non-linear cost; over-budget operation rejects `resource.work` before it starts | `consumed <= reserved` | Versioned work breakdown, reservation decision and exact code |
| `RE-12` | Sums/products near integer maximum | Reject `resource.arithmetic_overflow` | No wrapped allocation/work value | Checked-arithmetic result |
| `RE-13` | Virtual time strictly before, exactly at and after total/phase deadline; separately, request deadline exceeds the earliest policy/bundle/revocation/corpus/delegation freshness bound | Before may proceed; equality/after returns `resource.deadline`/`resource.phase_timeout`; effective total deadline equals the earliest context bound and no result outlives it, with active-context expiry resolved by the declared higher-priority `trust.freshness_unproved` rule | No new work begins at/after applicable expiry | Requested/effective deadlines, all component bounds and virtual monotonic/phase trace |
| `RE-14` | Cancellation during cooperative read, expand, parse, verify, sort and analyse chunks | `resource.cancelled`; no new work after observation | Bounded virtual chunks | Injection point and observation order |
| `RE-15` | Source primitive lacks cancellation/deadline contract | Reject `resource.source_not_cancellable` before it can hold capacity | No source call or long-lived lease | Source-capability decision |
| `RE-16` | Two admissions exceed one global slot/reservation | One wins atomically; other gets `resource.capacity_global` | No oversubscription | Deterministic barrier and ledger |
| `RE-17` | Simultaneously eligible scopes contend repeatedly while queue/contender registry is disabled | One serial slot remains contained; losing admission returns `resource.capacity_global`, rejected attempts are not remembered and evidence explicitly makes no rotation/starvation-freedom claim | All hierarchical caps preserved; zero retained contenders | Admission/code trace and `fairnessClaim=false` evidence |
| `RE-18` | Scope hopping or ledger growth via child digests, many valid child scopes, provider labels, aliases or fragmented requests | Parent buckets aggregate all children and metadata cardinality/lifetime; no extra quota/unbounded entry growth | Same parent window and metadata ceiling | Authorisation versus quota keys and bucket ledger |
| `RE-19` | A slot is occupied and a caller attempts to create backlog while queue is disabled | Immediate `resource.capacity_global`; no persisted/retried work or queue object | Zero queued items | Admission code and storage inspection |
| `RE-20` | Deadline after one case and during the next | Causal code is `resource.deadline` and disposition is `IncompleteNonAuthorising`; only the completed case identifier may be diagnostic and the partial case is discarded | No subset aggregate or resumed work | Output disposition, causal code and counters |
| `RE-21` | Incomplete subset has apparently perfect metrics | Precision/recall/calibration/aggregates are absent/null and no gate is emitted | No implicit resume/merge | Serialised fields and gate result |
| `RE-22` | Refusal fixture contains sensitive topology/content | Stable sanitised code only | Diagnostic output size bounded | Secret/content canary scan |
| `RE-23` | Mandatory applicable dimension missing, zero, negative, unbounded or unsupported; disabled-queue sentinel malformed; checked sum/product overflows | Reject `resource.envelope_invalid`/`resource.arithmetic_overflow` | No allocation or acquisition from invalid value | Schema/value matrix, sentinel case and checked result |
| `RE-24` | Bootstrap succeeds but coordinator-global capacity, one authenticated scope bucket or one window quota acquisition fails | Roll back all partial acquisitions and reject `resource.capacity_global`, `resource.capacity_scope` or `resource.quota_window`, respectively | No work or leaked lease | Atomic acquisition ledger and exact failed layer/code |
| `RE-25` | Truthful output at result-count, encoded-byte or accounted-memory limit and one unit over | At limit completes; over-limit returns `resource.output` and remains non-passing without truncated truth | Output never exceeds any reservation | Output fields, code, disposition and count/size ledger |
| `RE-26` | Expansion-ratio input has zero encoded length, boundary ratio or overflowing multiplication | Empty valid form follows schema; undefined/overflow ratio rejects safely | No division-by-zero/wrapped cap | Encoded/expanded arithmetic trace |
| `RE-27` | Trust/source violation, resource breach, cancellation and deadline become observable at the same checkpoint | Declared precedence yields one stable code without extra discovery work | No later work | Virtual condition matrix and terminal code |
| `RE-28` | Ingress lease acquired but first byte never arrives, or a cooperative source stalls between reads | No first byte returns `resource.first_byte_timeout`; inter-read stall returns `resource.idle_timeout`; cancellation is requested and capacity releases only after quiescence/fence | Deadline does not create a second lease | Virtual liveness clock, termination evidence and ledger |
| `RE-29` | A scope consumes a parent/window quota and the coordinator restarts | No cross-restart fairness/quota claim without replay-resistant durable state; where continuity is required, missing state fails closed | No quota reset presented as replenishment | Restart fixture, persisted head/token state or explicit process-lifetime claim |
| `RE-30` | Cancellation/deadline reaches a misbehaving cooperative component; variants use a non-pre-emptible primitive with and without an authorised termination fence | Non-quiescent work returns `resource.quiescence_unproved`, keeps reservation and quarantines; proved fence termination permits one release; unfenced primitive returns `resource.source_not_cancellable` before work | No capacity reuse while old work can execute | Exact code, worker/fence identity, termination proof and lease ledger |
| `RE-31` | Numeric, allowlist, capability, schema/profile and queue fields are varied across local, policy, bundle and request sources | Valid numeric minimum applies only after validation; sets intersect and deny dominates; unsupported schema/profile returns `resource.schema_unsupported`; empty/elevating/mismatched composition or positive queue returns `resource.envelope_invalid` | No acquisition from invalid composition | Per-field raw sources, rule and exact refusal code |
| `RE-32` | Evaluation capacity is saturated while a valid revocation/context update arrives; an old evaluation later completes | Evaluation cannot consume/refuse reserved control admission; deterministic virtual priority scheduling commits within its phase bound, cancels old work and refuses stale publication with `trust.context_superseded`; subsequent work pins the new context | Control reservation remains bounded; no physical-latency claim | Separate ledgers and ordered scheduling/context/commit/publication trace |
| `RE-33` | Trust/corpus heads, nonces, quota entries or audit retention reach ordinary metadata cap; separately, irreversible revocation reaches its cap; crash occurs before/during/after compaction | Ordinary metadata cap refuses new admission with `resource.control_metadata`; revocation cap enters recovery with `trust.revocation_capacity`; crash exposing neither complete old nor complete new accumulator quarantines `trust.continuity_unproved`; no retired item resurrects | Declared control-metadata ceiling never exceeded | Exact code/state, cardinality/bytes, accumulator roots, crash points and restart state |
| `RE-34` | Second coordinator, cloned owner or stale process claims the same accounting domain; separately, the fenced owner restarts | Conflicting/stale owner rejects `resource.coordinator_conflict`; only the current fence may admit or release; no fleet-wide claim is inferred | One effective coordinator slot | Owner/fence tokens, process/domain identity and admission ledger |
| `RE-35` | Exact declaration is smaller/larger than final observation; maximum declaration observes below/at/above; applicable field is omitted | Exact mismatch, maximum overflow or omission rejects `resource.source_mismatch`/`resource.envelope_invalid`; only valid forms complete | Incremental counters remain bounded | Manifest form, final counters, EOF check and refusal |
| `RE-36` | Accounted-memory reservation is sufficient while empirical heap varies; separately, accounted reservation is one byte short | Empirical variation is recorded for `HM-02` but does not directly decide admission; insufficient accounted memory rejects `resource.memory` | Allocation follows accounted profile only | Reservation ledger and separate profiler observation |

### Later empirical homologation campaigns

These campaigns are not deterministic contract vectors and cannot be satisfied by a virtual clock:

| ID | Measurement | Required environment/evidence |
|---|---|---|
| `HM-01` | Real cancellation, first-byte, idle and control-update scheduling/commit latency under maximum admitted evaluation load | Declared hardware/runtime/OS, repeated distribution, worst observed latency and failure threshold |
| `HM-02` | Heap, working-set and allocation peak versus the versioned accounted-memory formula | Declared runtime/architecture, profiler/counters, warm/cold repetitions and conservative headroom decision |
| `HM-03` | CPU/elapsed cost versus deterministic work model under crypto, parse, sort and analysis boundaries | Declared environment, repeatable fixtures, variance and calibration decision without replacing work limits with time |

### Corpus-governance vectors

| ID | Precondition/input | Expected decision | Work/memory constraint | Required evidence |
|---|---|---|---|---|
| `CO-01` | Manifest or content differs from the exact pair named by the authenticated active corpus head | Quarantine `corpus.manifest_mismatch` | No evaluation | Head, manifest/content expected and observed digests |
| `CO-02` | Candidate contains duplicate/leaked cases across governed partitions | Reject `corpus.partition_invalid`, preserve the fresh active corpus head and emit only bounded non-passing diagnostics | Bounded duplicate index | Duplicate identifiers/digests and unchanged active head |
| `CO-03` | Corpus replayed cross-scope or after expiry | Reject `corpus.scope_mismatch` or `corpus.expired` | No evaluation | Scope/clock comparison |
| `CO-04` | Candidate omits a required segment or violates its quantitative distribution criteria | Reject `corpus.partition_invalid`, preserve the fresh active corpus head and emit a non-passing coverage report with no general claim | Output bounded by manifest segments | Expected/observed distribution and unchanged active head |
| `CO-05` | Label flip, conflicting evidence or poisoned transformation | Quarantine affected revision with `corpus.integrity_unproved`; no automatic correction, relabelling or learning | No evaluation or automatic correction | Source/label/reviewer lineage and quarantine state |
| `CO-06` | Attacker replaces manifest, content and self-consistent digests; variants use forged/wrong-role approval or corpus key reused from another role | Reject `corpus.authorisation_unproved`; only the root-delegated corpus role plus independent approval can advance the head | Bounded signature/profile work; no case read | Approval, role delegation, key identity and current/candidate heads |
| `CO-07` | Candidate corpus revision is lower, same revision/different digest, skips predecessor, reuses dataset identifier after withdrawal, replays withdrawn content or requests a successor/wrap at maximum revision | Lower/withdrawn replay rejects `corpus.rollback`/`corpus.withdrawn` and preserves fresh active; same-revision divergence/gap quarantines by `corpus.split_view`/`corpus.generation_gap` with stored head unchanged; identifier reuse rejects `corpus.withdrawn`; checked overflow quarantines `corpus.revision_exhausted`; valid direct successor commits atomically | Bounded chain and constant-size active head; no wrap | Checked revision arithmetic, digests, withdrawal tombstone and before/after corpus head/state |
| `CO-08` | Candidate content omits/adds a case, changes partition membership, duplicates across partitions or violates declared segment counts/distribution | Reject `corpus.partition_invalid`, preserve the fresh active corpus head and emit only bounded non-passing coverage evidence; never silently reweight | Bounded case/duplicate index and output | Exact case/partition identifiers, expected/observed segment distribution and unchanged active head |

## Documentary acceptance checklist

| Requirement from the authorised proposal | Documentary result |
|---|---|
| Responsibilities and trust boundaries are explicit | Responsibility matrix and mandatory separation above |
| ADR compares at least three alternatives | `ADR-0007` compares four alternatives |
| Rollback, freeze, split view, fast-forward, compromise and restore have controls | Checkpoint state machine and traceability `M12-T02`–`M12-T13` |
| Missing/restored checkpoint quarantines until authenticated reconciliation | `Quarantined`/`Recovering` transitions and restore limitation |
| Normal rotation differs from compromise recovery | Separate rotation and recovery sections |
| Checkpoint never decreases normally | Direct-successor and rollback rules |
| Envelope covers counts, bytes, accounted memory, work, time, cancellation, quota, explicit no-fairness boundary, partial results and fenced concurrency | Resource envelope and contract vectors `RE-01`–`RE-36`; physical evidence remains `HM-01`–`HM-03` |
| Every threat has control, future vector and owner | Traceability table `M12-T01`–`M12-T24` |
| Corpus has independently authenticated governance, monotonic head, exact membership and adversarial plan | Manifest plus `CO-01`–`CO-08` |
| No provider-specific or secret dependency | Explicit non-goals and provider-neutral keys/scopes |
| No hidden critical unknown presented as resolved | Residual restore/split-view and unmeasured numerical limits are explicit |
| No runtime or mode promotion | Status, non-goals and implementation gate below |

## Implementation and promotion gate

The documentary increment received human acceptance as recorded in the append-only state log. That acceptance was not a lifecycle Human Gate, did not accept `ADR-0007` as an implemented decision and authorised no code. This direct automatic re-audit corrects the package factually but does not convert the ADR from `proposed` to `accepted`.

A future implementation proposal must separately name components, owners, store, transaction/restore model, provisioning/reconciliation mechanism, measured limits, test environment and rollback. It must obtain explicit authority before changing an implementation source file or creating a runtime. `none → OBSERVER` remains a later independent decision requiring implementation evidence, security review, offline evaluation evidence, Quality Gate and dedicated Human Gate. No further review of this unchanged documentary package is a prerequisite; a new review is triggered only by a materially new proposal or lifecycle decision.
