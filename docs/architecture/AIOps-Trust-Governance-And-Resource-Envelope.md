# AIOps Trust Governance and Resource Envelope

## Status and authority

- Status: review candidate supporting proposed `ADR-0007`
- Date: 2026-07-17
- Lifecycle position: `STATE-06 INTEGRATION`
- Authority: Bruno authorised a documentation-only MOD-12 increment covering responsibilities, an ADR, conceptual trust/checkpoint/resource contracts, threat modelling, traceability and future test planning
- Explicit exclusions: implementation, source code, migrations, runtime, real keys, persistence, services, external actions and promotion to `OBSERVER`

This document specifies a future-facing architecture contract. It does not assert that a trust distributor, durable checkpoint, resource coordinator, queue, witness or operational corpus exists. It gives no component permission to collect telemetry or activate MOD-12.

## Relationship to the current baseline

| Concern | Current observed baseline | This documentary contract | Still requires later authority and evidence |
|---|---|---|---|
| Policy provenance | Local verification of signed grants against application-supplied P-256 public anchors | Separation of policy authorisation, signature roles and distribution | Issuer, custody, provisioning and operational distribution |
| Revocation | Signed `observer-policy-revocation.v2` snapshot checked against an exact application-supplied sequence | Signed bundle, contiguous generations, durable checkpoint, quarantine and reconciliation | Store, transaction, restore integration, witness and runbook |
| Resource budget | Synchronous limits for cases, declared samples, deterministic work units and elapsed time | Encoded/expanded bytes, parser, memory, work, deadline, cancellation, quotas, fairness and output limits | Implementation, measured values, load and memory homologation |
| Materialisation | Aggregate telemetry admission precedes enumeration; one case is copied and sorted at a time | Bounded streaming/framing and reservation before every material allocation | Parser/transport design and tests |
| Concurrency | A single call is synchronous; no shared coordinator exists | Global ingress lease plus authenticated hierarchical reservation; serial first proof | Scheduler/coordinator, contention and fairness evidence |
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

Continuity uses two levels so an old recovery epoch cannot select an old checkpoint merely by changing the lookup key.

The stable trust-domain head key excludes series and recovery epoch:

```text
trustDomainId
  + authorityId
  + tenant/environment
  + purpose
  + policyScopeId
```

Its monotonic value selects the current `recoveryEpoch`, current `seriesId`, current domain-head digest and permanently superseded epochs/series. A subordinate series checkpoint is then keyed by that stable domain head plus the selected epoch and series. Recovery atomically advances the domain-level high-water mark and retires the previous epoch; an old epoch can never be treated as an uninitialised independent domain.

`policyScopeId` identifies a stable authorisation domain, not a request-created digest and not a resource-quota bucket. Every element is canonical and signed where it crosses a trust boundary. Provider names, UI labels, display names, mutable tags and caller-selected aliases cannot define or widen trust or quota keys. A package valid for one logical domain is invalid in every other domain.

### Canonical scope predicate

The first design represents a scope ceiling as a bounded, sorted set of exact canonical atoms. Each atom contains stable tenant, environment, instance, Agent and evidence-kind identifiers under a versioned schema; no alias, wildcard or inherited UI hierarchy is accepted.

For authorised ceiling `C` and requested scope `S`, acceptance requires `S` to be non-empty and every complete atom in `S` to be exactly present in `C`. Equality of a digest alone is not a subset proof: the host validates the canonical sets and predicate first, then checks their signed digests. A superset, partial overlap, parent/child alias or unknown atom fails closed. A future hierarchical scope model requires a new schema and architecture decision.

## Responsibility and identity map

| Role | Owns | May do | Must never do | Required evidence |
|---|---|---|---|---|
| Root governance authority | Trust-domain policy, algorithm profile and exceptional recovery approval | Approve root delegation and recovery under dual control | Issue routine grants, publish bundles or reset checkpoints alone | Immutable approval ID/digest, approvers, scope and validity |
| Root key custodian | Protected use of root signing capability | Produce a signature for a separately approved root operation | Decide policy, publish artefacts or expose private material | Custody audit and signature bound to the approval |
| Policy authority | Legitimate purpose, tenant/environment, scope ceiling and validity | Approve an immutable policy-authorisation record | Hold the grant-signing role, publish or advance checkpoint state | Authorisation ID/digest and exact ceiling |
| Policy-authorisation attestation signer | Canonical proof of an approved policy decision | Sign only the exact record approved by the policy authority under a root-delegated role | Decide policy, sign grants/bundles/revocation or invent a scope | Role-specific signature, approval digest and delegation |
| Trust-bundle signer | Bundle generation and delegated role-key statements | Sign one canonical bundle within root delegation | Sign grants/revocation or broaden the policy authority's ceiling | Role-specific key ID and signed bundle digest |
| Grant signer | Approved policy grants | Sign only grants linked to a valid `PolicyAuthorisationAssertion` | Authorise its own scope, revoke, publish or change trust roots | Grant signature and linked assertion digest |
| Revocation authority | Revocation decisions and incident scope | Approve grant/key withdrawal and emergency freeze | Issue grants or replace root governance | Revocation decision ID/digest and incident/audit reference |
| Revocation signer | Canonical revocation snapshots | Sign the exact approved snapshot | Create revocation decisions, grants or checkpoint updates | Role-specific signature and approval reference |
| Key custodian | Operational private-key protection | Use only the role and operation approved for its key | Decide policy, publish or reuse material across roles | Custody/audit evidence and non-exportability where available |
| Publisher/assembler | Byte-for-byte delivery of public artefacts | Carry a complete signed package over any later authorised transport | Authorise, sign, select a root, reorder history or claim acceptance | Content digest and delivery metadata only |
| Host trust coordinator | Candidate validation, continuity, local commit and quarantine | Compare, reconcile, reserve and expose an immutable accepted context | Mint policy, reduce a checkpoint, trust transport or call an executor | Validation decision, CAS/transaction result and sanitised audit intent |
| Durable checkpoint owner | Protected local monotonic state | Persist and compare the exact logical checkpoint | Select policy or silently restore/decrease state | Checkpoint revision, digest, continuity/recovery evidence |
| MOD-12 verifier/evaluator | Pure deterministic verification and offline analysis | Consume an immutable verified context and bounded input | Network, file/database I/O, key custody, trust recovery or runtime activation | Deterministic outcome and sanitised refusal code |
| Independent auditor/reconciler | Cross-head comparison and protected audit review | Detect gaps, forks, restore and policy/signature mismatch | Issue policy, sign operational artefacts or choose a branch automatically | Independent head/chain evidence and review decision |
| Dataset owner/steward | Corpus authority, classification, manifest and expiry | Approve governed offline use and report limitations | Collect by default, broaden purpose or promote a model automatically | Manifest, review, digest, expiry and segment statement |

### Mandatory separation

- The policy authority is distinct from grant signer, key custodian and publisher.
- Policy-authorisation, trust-bundle, grant and revocation signatures use distinct key identities and distinct public-key material.
- The root custodian is distinct from the publisher and routine operational signers.
- The checkpoint owner cannot issue policy or reduce the checkpoint.
- The auditor/reconciler cannot issue policy, sign operational evidence or resolve a fork without an authorised recovery decision.
- Organisational deployments may assign people to roles only if the resulting identities, permissions, approvals and audit paths preserve these incompatibilities. Convenience is not an exception.

A signature demonstrates origin and integrity under a key. Legitimate authorisation requires a separately signed `PolicyAuthorisationAssertion`, a valid root-signed role delegation and an enforced canonical scope predicate.

### Authenticated policy authorisation and role delegation

`PolicyAuthorisationAssertion` is a canonical public security artefact containing its schema/ID, authority and trust domain, tenant/environment, purpose, `policyScopeId`, bounded canonical scope ceiling, validity, maximum resource ceilings, approval ID/digest and policy-authorisation signature. The policy authority makes the decision; the separate attestation signer materialises only that approved decision.

`RoleKeyDelegation` is a root-signed canonical statement binding one public-key identity/material digest to one exact role, trust domain, authority, tenant/environment, purpose, validity, algorithm profile and optional generation range. An operational signer cannot delegate itself or its successor. Rotation of the trust-bundle signer or another operational key requires a root-approved delegation already valid for the transition.

The untrusted publisher may carry assertions/delegations in the same bounded transport envelope as a bundle, but the host authenticates their distinct signatures and revocation/validity independently. A bundle reference or audit digest alone is never sufficient. A missing, fabricated, altered, expired, revoked or out-of-scope assertion/delegation rejects the candidate without advancing trust state.

## Conceptual trust-bundle contract

The future bundle is an immutable canonical byte sequence. Field names below are conceptual, not a C# or wire schema.

| Field/group | Meaning | Required invariant |
|---|---|---|
| `bundleSchemaVersion` | Shape and canonicalisation version | Exact supported major; no implicit downgrade |
| `bundleId` | Globally stable content identity | Unique and bound into the signature |
| `trustDomainId` | Root trust domain | Exact match to independently provisioned configuration |
| `authorityId` / `seriesId` | Issuing authority and continuity series | Authority matches the stable domain head; series matches its selected subordinate checkpoint |
| `recoveryEpoch` | Explicit exceptional recovery generation | Changes only through authorised recovery, never normal rotation |
| `tenantId` / `environmentId` | Isolation boundary | Exact, signed and never wildcarded implicitly |
| `purpose`, `policyScopeId`, requested scope/digest | Permitted use, stable authorisation domain and exact requested atoms | Canonical requested set must be a non-empty subset of the independently authenticated ceiling |
| `policyAuthorisationAssertion` | Canonical assertion, ID/digest and distinct signature of the legitimate approval | Independently authenticated, valid, unrevoked and exact for domain/purpose/ceiling |
| `roleKeyDelegations[]` | Root-signed statements for every operational signing key used | Exact role/domain/scope/validity; operational signer cannot self-delegate |
| `generation` | Positive monotonic bundle generation | Normal update is the direct successor only |
| `issuedAt`, `notBefore`, `expiresAt` | UTC validity | Ordered, bounded and checked against trusted clock policy |
| `previousBundleDigest` | Digest of the immediately accepted predecessor | Required after bootstrap; exact chain continuity |
| `operationalKeys[]` | Public keys by exact role, scope, algorithm and validity | Distinct identity/material per role; bounded cardinality |
| `retiredKeys[]` | Explicitly retired role keys | Never reactivated automatically |
| `requiredRevocation` | Snapshot schema, series, sequence, digest and freshness | Exact authenticated snapshot required for the bundle |
| `algorithmProfile` | Allowed signature, digest, curve and canonicalisation rules | Host allowlist may be stricter; automatic downgrade forbidden |
| `resourceCeilings` | Complete finite bundle maxima for every mandatory dimension | Required; each value must be no greater than the authenticated policy-assertion and local ceilings |
| `encodedLength`, `expandedLength`, cardinalities | Producer declarations for bounded admission | Observed values remain authoritative and cannot exceed declarations/limits |
| `auditReferences[]` | Stable IDs/digests of approvals and rotation decisions | References are evidence links, not authorisation by themselves |
| `bundleSignature` | Role-specific signature over canonical content | Valid only under independently rooted trust-bundle role |

The root public key is not carried as a trust-establishing field. A root identifier may be present for exact selection, but key material and trust status come from the independent provisioning boundary.

### Canonical validation and admission order

1. Record the absolute monotonic deadline before any reservation; honour pre-start cancellation.
2. Acquire only a bounded global ingress bootstrap lease and, where independently available, an ingress-principal quota. An unauthenticated claimed policy scope never selects a quota.
3. Apply trusted local framing, first-byte and inter-read idle limits; a missing/false length or silent source never creates an unbounded read or lease.
4. Enforce independent encoded-byte, expanded-byte, compression-ratio, nesting, token, string and collection limits.
5. Parse only the bounded canonical structure and reject unknown critical fields or unsupported schema.
6. Resolve the independently provisioned root and local algorithm allowlist; verify every required `RoleKeyDelegation`.
7. Reproduce canonical bytes/digest and verify the trust-bundle signature under its exact delegated role.
8. Independently verify the `PolicyAuthorisationAssertion` signature, validity/revocation, exact domain/purpose and canonical requested-scope subset predicate.
9. Verify validity and trusted-clock policy without granting a local extension.
10. Compare the stable domain-head recovery high-water mark, selected series, generation and predecessor digest to durable state; validate every item of a bounded contiguous catch-up chain in order.
11. Verify each generation's exact required revocation snapshot, role-specific key and freshness.
12. Calculate every effective resource ceiling as the minimum of complete finite trusted-local, policy-assertion, bundle and caller-requested limits.
13. Atomically acquire authenticated hierarchical quota/capacity reservations; if this second acquisition fails, release the bootstrap lease and reject.
14. Reserve remaining reusable memory/output capacity and cumulative work/byte budget before the relevant materialisation or high-cost semantic work.
15. Revalidate immutable digests and commit the final selected bundle, revocation state, domain head, series checkpoint and audit intent in one local transaction.
16. Publish only the resulting immutable verified context to the pure MOD-12 verifier.

Authentication cannot make the bytes needed to authenticate free. The bounded bootstrap reader limits pre-authentication cost; the design must never claim that all cost occurs only after authentication.

A candidate transport envelope may contain one bundle or a trusted-locally-bounded contiguous chain from the active head to a fresh final head. Connected pull and controlled offline import use the same canonical chain; notification hints never carry authority. Every intermediate signature, delegation, authorisation, predecessor and revocation transition is validated in order, while only the final complete tuple becomes visible through the local transaction. A numerically later bundle without the complete bounded chain is a gap, not catch-up.

Malformed, unsupported or unauthenticated candidate bytes are rejected and audited without deactivating a current tuple that remains fresh and whose continuity is intact. Authenticated equivocation, an authenticated gap showing missing continuity, expiry of the current tuple, compromise evidence or damaged/restored durable state enters quarantine. Authority unavailability permits the last accepted tuple only until its existing validity/freshness expires; it never creates a local extension.

## Durable checkpoint model

### Logical value

| Value | Purpose |
|---|---|
| Stable trust-domain head key | Prevents cross-scope reuse without letting epoch/series select an older record |
| Current `recoveryEpoch`, `seriesId` and superseded epoch/series set | Enforces a domain-level recovery high-water mark |
| Subordinate series checkpoint key and `generation` | Enforces monotonic continuity within the selected recovery epoch/series |
| Current bundle digest | Detects same-generation equivocation |
| Previous bundle digest | Preserves direct chain relation |
| Revocation series, sequence and digest | Binds the exact revocation head |
| Algorithm/canonicalisation profile | Prevents silent downgrade |
| Local concurrency revision | Supports future compare-and-swap/transaction conflict detection |
| Accepted-at instant and audit-intent digest | Supports sanitised continuity evidence |
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
| `Uninitialised` + independently authorised bootstrap | Validate complete root/authorisation/bundle/revocation evidence | One local commit to `Active` |
| Existing domain head + bundle from superseded epoch/series | `trust.epoch_superseded` | Reject; never bootstrap an old epoch as a new domain |
| Same generation + same digest | Idempotent success | No logical advancement |
| Same generation + different digest | `trust.split_view` | Enter `Quarantined` |
| Lower generation/sequence | `trust.rollback` | Reject; checkpoint never decreases |
| Direct successor + exact predecessor + valid revocation | Candidate accepted | One local commit to new `Active` head |
| Bounded complete contiguous catch-up chain | Validate every direct transition; accept fresh final head | One local commit to final `Active` head |
| Authenticated future generation with an incomplete gap | `trust.generation_gap` | Enter `Quarantined`; full chain or extraordinary reconciliation required |
| Wrong series/epoch/scope | `trust.scope_mismatch` | Reject without changing another scope |
| Malformed/unauthenticated candidate while current tuple is fresh/intact | Reject candidate and audit | Preserve current `Active` tuple |
| Expired/not-yet-valid evidence or untrusted clock | `trust.freshness_unproved` | Enter or remain `Quarantined` |
| Missing, corrupt or restored checkpoint | `trust.continuity_unproved` | Enter `Quarantined` |
| Crash before local commit | Old complete tuple remains visible | No partial advancement |
| Crash after local commit | New complete tuple is visible idempotently | No mixed tuple |
| Root/signer compromise | `trust.compromise_recovery_required` | Enter `Recovering`/`Quarantined` |
| Explicit retirement | Validate authorised retirement | Commit `Retired`; no automatic reactivation |

Normal advancement accepts a direct successor or a bounded complete chain of direct successors. A gap or fast-forward is not resolved by choosing the numerically highest signed generation.

### Restore, clone and split-view limitation

A local checkpoint and local audit restored together can look internally consistent while both are old. Digest chaining detects a fork only when independent views are compared. Therefore:

- every known restore, clone, continuity loss or divergence quarantines the scope;
- resumption requires an independently authenticated current head and a contiguous chain or separately authorised recovery artefact;
- no branch wins automatically by timestamp, generation or signer;
- an independent witness, monotonic platform anchor or protected external audit head is required before claiming detection of a silent full-store rollback; and
- until such a mechanism is implemented and tested, silent restore and isolated split view remain explicit residual risks.

This contract does not choose or create that independent mechanism.

## Rotation and recovery

Normal rotation preserves the trust domain, recovery epoch, authorised scope and contiguous generation. Root governance first approves and root custody signs the replacement `RoleKeyDelegation`; policy authority separately approves any policy impact. A later bundle uses a bounded overlap in which old and replacement role keys are both explicitly delegated and valid, then retires the old key in a direct successor backed by an independently authenticated root/revocation decision. The current trust-bundle signer cannot create its own successor delegation or unilaterally change another role's status.

Compromise recovery changes the recovery epoch or trust series and requires root governance, incident-response evidence, quarantine, independent reconciliation and a new bootstrap decision. The recovery transaction advances the stable domain-head epoch high-water mark and permanently supersedes the old epoch/series before the new subordinate checkpoint becomes active. A suspected root cannot authorise its own replacement through the compromised chain. Compromise recovery cannot be disguised as routine rotation.

## Resource envelope

### Sources and precedence of limits

For every dimension `d`:

```text
effectiveLimit[d] = min(localTrustedCeiling[d], policyAssertionCeiling[d], bundleCeiling[d], requestLimit[d])
```

- `localTrustedCeiling` is selected by the future host through trusted configuration.
- `policyAssertionCeiling` is a complete finite group approved by the policy authority and authenticated under the separate attestation role.
- `bundleCeiling` is a complete finite group bound by the bundle signature and can only reduce both the local and policy-assertion values.
- `requestLimit` is a complete finite group supplied for one execution and can only reduce all trusted values.
- A producer declaration is evidence to check, not a source of authority and never raises a limit.
- Every mandatory dimension must be present, positive, finite and version-compatible in all four groups. Missing, zero, negative, unbounded or overflowed values fail closed; absence is never represented as infinity.

Accounting separates four kinds of state:

- **Reusable capacity leases:** active slots, accounted-memory reservations and bounded buffers/output capacity. They expire under a monotonic lease and are released exactly once.
- **Cumulative execution consumption:** observed bytes, cardinality, deterministic work and elapsed time. It only increases and is never returned during the execution.
- **Window quota tokens:** authenticated hierarchical bytes/work/admission allowance. Unused reservation may be returned; consumed allowance replenishes only by its trusted time-window policy.
- **Audit counters:** monotonic sanitised reserved/consumed/rejected/released values; they are evidence, not capacity.

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
| Observed memory | Heap/working-set peak | Measurement only; never confused with deterministic reservation | Reproducible load environment |
| Total deadline | Absolute monotonic time from admission request before any reservation through finalisation | Expired when `now >= deadline`; never renewed by progress or retry | Virtual-clock decision tests |
| Ingress lease/read liveness | Reservation lease, time to first byte and maximum idle interval between bounded reads | Lease cannot outlive total deadline; silent/non-cooperative source is cancelled/refused | Silent-source contract tests and later latency measurement |
| Phase deadline | Bounded read/parse/verify/sort/analyse/finalise windows | Must fit inside total deadline | Phase-stall vectors |
| Cancellation latency | Monotonic time/chunks after cancellation is observable | Check before phases and between bounded chunks; start no new work | Injected cancellation at every phase |
| Concurrency | Active slots and reusable capacity globally and by authenticated hierarchy | Atomic acquisition before work; reusable leases release exactly once | Contention tests |
| Fairness/window quotas | Admission/bytes/work tokens at global → tenant/environment → authority/principal → purpose → exact-scope buckets | Acquire all atomically; parent buckets prevent fragmentation; ledger entry cardinality/lifetime is itself globally and parent-bounded; deterministic eligible-scope rotation limits repeated dominance | Virtual scheduler/window/metadata tests |
| Future queue | Depth, bytes and maximum age | Disabled by default; separately authorised, bounded and observable | Queue-specific gate if ever proposed |
| Output | Result count and encoded/accounted-memory size | Reserve before building report; diagnostics may be bounded, but factual truth is never truncated into a passing result | Oversized-result vectors |

All additions and multiplications use checked arithmetic. Overflow is a refusal, not silent saturation. The work model must account for cryptographic verification and sorting; elapsed time is not a substitute for deterministic work.

### Admission and backpressure sequence

1. Record the absolute monotonic deadline before any reservation; if cancellation is already requested, return cancelled without acquisition.
2. Validate the local bootstrap envelope and acquire a short global ingress lease, plus only an independently authenticated ingress-principal bucket when one exists.
3. Require a cancellation/deadline-aware bounded source. Apply first-byte and inter-read idle deadlines, encoded/expanded byte limits and parser-structure limits.
4. Authenticate and authorise the immutable trust/data manifest and derive its canonical resource-quota hierarchy; an untrusted claimed scope never chooses pre-authentication quota.
5. Calculate complete effective limits and atomically acquire every authenticated parent/child quota and reusable-capacity lease. If any acquisition fails, roll back all acquisitions and release ingress capacity.
6. Reconcile declarations with observed cumulative counters incrementally.
7. Reserve accounted memory/output capacity and cumulative work/byte allowance before each relevant operation.
8. Process only bounded cooperative chunks/current cases, checking absolute/phase/idle deadlines and cancellation between them.
9. Charge cumulative consumption monotonically. Cumulative consumption never decreases; held reusable capacity never exceeds its lease; consumed window tokens replenish only by policy.
10. Release reusable leases and unused window reservation exactly once on success, refusal, exception, timeout or cancellation. Never return consumed execution/window allowance as though it were unused.
11. Publish a passing result only after the whole authorised execution completes and its truthful output fits the admitted envelope.

No queue, retry, continuation, partial acceptance or budget increase is implicit. Capacity unavailable at admission produces deterministic backpressure. A future source primitive must accept cancellation and deadline cooperatively; a non-cooperative source is refused unless a separately authorised isolation boundary proves termination. A byte/count cap alone is not latency evidence.

### Initial concurrency and fairness policy

- The first separately authorised local proof uses `maximum parallelism = 1` globally.
- No backlog is retained; a second admission is rejected while the slot is occupied.
- Authorisation and quota keys are different: the exact policy scope proves permission, while hierarchical parent buckets aggregate all child scopes for capacity.
- All global, tenant/environment, authority/principal, purpose and exact-scope buckets are acquired atomically. A child scope cannot evade a consumed parent ceiling by changing a digest, alias, provider label or request partition.
- Creating a quota-ledger entry consumes bounded parent/global metadata capacity. Entry cardinality and lifetime are finite; an expired entry is removed only after all leases close, and repeated creation of valid child scopes cannot grow metadata without bound.
- A future fairness-capable coordinator uses a versioned deterministic rotation among eligible canonical scope keys under each parent: an eligible key cannot win twice while another eligible key in that scheduling round has not received an opportunity. Window tokens additionally bound sequential reacquisition.
- With queueing disabled, rejected attempts are not retained; fairness applies only to the bounded set of simultaneously eligible attempts and trusted window quotas. The first serial proof demonstrates containment, not general starvation freedom. A broader fairness claim requires a separately authorised bounded coordinator and virtual-time evidence.

### Partial and interrupted results

Only a complete run may have a passing disposition. An interrupted run may report fully completed case identifiers and sanitised consumption counters for diagnosis, but:

- its disposition is `IncompleteNonAuthorising`;
- `Passed`, corpus acceptance, calibration and every promotion signal remain false;
- precision, recall, calibration and every aggregate derived from the completed subset are neither calculated nor serialised; the fields are absent/null, not merely excluded from gate evidence;
- incomplete output cannot be resumed or merged automatically; and
- any manual retry is a new execution with a new admission decision.

Cancellation and deadline are distinct. Before acquisition, cancellation is checked first, then `now >= deadline`, then capacity. At each later serialised checkpoint, an already detected trust/integrity/source violation wins, followed by an already detected arithmetic/resource-limit breach, cancellation and then deadline; no extra work is performed merely to search for a higher-priority cause. Time strictly before the deadline may proceed, while equality or later is expired. Thus simultaneous cancellation and deadline resolves as cancellation unless a higher-priority violation was already detected. The current local runner's different behaviour remains evidence only for its existing contract.

### Conceptual refusal codes

These names are design vocabulary, not implemented API values:

| Category | Codes |
|---|---|
| Trust | `trust.bootstrap_required`, `trust.authorisation_unproved`, `trust.rollback`, `trust.epoch_superseded`, `trust.split_view`, `trust.generation_gap`, `trust.scope_mismatch`, `trust.freshness_unproved`, `trust.continuity_unproved`, `trust.compromise_recovery_required` |
| Admission | `resource.envelope_invalid`, `resource.schema_unsupported`, `resource.capacity_global`, `resource.capacity_scope`, `resource.quota_window`, `resource.encoded_bytes`, `resource.expanded_bytes`, `resource.structure`, `resource.cardinality`, `resource.memory`, `resource.work`, `resource.output` |
| Execution | `resource.first_byte_timeout`, `resource.idle_timeout`, `resource.source_not_cancellable`, `resource.deadline`, `resource.cancelled`, `resource.source_mismatch`, `resource.arithmetic_overflow`, `resource.incomplete` |

Diagnostics identify the failed dimension and policy version without including telemetry content, private material or sensitive topology.

## Governed offline corpus manifest

Every future corpus revision requires an immutable manifest containing:

- dataset identifier, dataset schema version and independent content version;
- owner/steward and approving authority;
- source category, consent/authority, permitted purpose and tenant/environment scope;
- classification, redaction decision, retention class and expiry;
- canonical content digest and manifest digest;
- provider/version/platform/topology segments, explicitly described as fixtures or observed data;
- time range, inclusion/exclusion criteria and transformations;
- label origin, reviewer and known uncertainty;
- known bias, missing segments, distribution and duplication/leakage checks;
- adversarial cases, holdout rules and prohibited reuse; and
- supersession/withdrawal status.

A valid signature or digest does not make labels correct. Expired, cross-scope, duplicated, tampered, unreviewed or purpose-incompatible material is quarantined. Production feedback never updates the corpus, policy or model automatically. This document neither creates nor enlarges a corpus.

## Threat, control and future-test traceability

| ID | Threat | Required control | Future vector(s) | Accountable owner | Enforcer/reviewer |
|---|---|---|---|---|---|
| `M12-T01` | Unauthorised issuance under a valid signer | Independently authenticated policy assertion, canonical subset predicate and delegated signer role | `TR-10`, `TR-16` | Policy authority | Host trust coordinator / independent auditor |
| `M12-T02` | Rollback | Stable domain epoch high-water, series checkpoint, direct predecessor and no decrement | `TR-01`, `TR-02`, `TR-17` | Host trust coordinator | Checkpoint owner / independent auditor |
| `M12-T03` | Freeze | Bounded validity/freshness and no implicit extension | `TR-07` | Policy authority | Host trust coordinator |
| `M12-T04` | Fast-forward/generation gap | Direct successor or bounded complete chain; extraordinary recovery for unresolved gaps | `TR-05`, `TR-18` | Host trust coordinator | Root governance authority |
| `M12-T05` | Split view/equivocation | Same-generation digest check, independent reconciliation and no automatic branch choice | `TR-03`, `TR-14` | Independent auditor | Host trust coordinator |
| `M12-T06` | Cross-scope replay | Signed exact tenant/environment/purpose, canonical subset predicate and stable domain key | `TR-08`, `TR-16` | Policy authority | Host trust coordinator |
| `M12-T07` | Operational signer compromise | Distinct keys/roles, root delegation, scope ceiling, revocation and quarantine | `TR-10`, `TR-12`, `TR-13` | Security architecture | Host trust coordinator / incident response |
| `M12-T08` | Root/revocation compromise | Out-of-band recovery epoch, domain high-water, dual control and full quarantine | `TR-13`, `TR-17` | Root governance authority | Incident response / independent auditor |
| `M12-T09` | Key/algorithm confusion or downgrade | Role delegation, material separation, allowlist and explicit profile migration | `TR-09`, `TR-11`, `TR-12` | Security architecture | Host trust coordinator |
| `M12-T10` | Clock manipulation | Trusted monotonic/UTC policy, bounded skew and fail-closed freshness | `TR-07` | Host platform owner | Host trust coordinator |
| `M12-T11` | Partial update, crash or TOCTOU | Immutable digest, revalidation and one local CAS/transaction | `TR-06` | Host trust coordinator | Checkpoint owner |
| `M12-T12` | Restore, clone, absence or corruption | Quarantine plus independent continuity reconciliation and epoch high-water | `TR-04`, `TR-14`, `TR-17` | Durable checkpoint owner | Independent auditor |
| `M12-T13` | Audit tampering | Protected append-only audit intent/head and independent comparison | `TR-15` | Independent auditor | Host trust coordinator |
| `M12-T14` | Lying, mutable, silent or infinite source | Cancellation-aware bounded reader, liveness deadlines and observed counters | `RE-03`, `RE-04`, `RE-28` | Host resource coordinator | MOD-12 owner |
| `M12-T15` | Bundle/parser/decompression exhaustion | Independent byte/ratio/structure caps before allocation | `RE-05`–`RE-08`, `RE-26` | Host resource coordinator | Parser/security reviewer |
| `M12-T16` | Memory exhaustion | Versioned accounted-memory reservation and separately measured peak | `RE-09`, `RE-10`, `HM-02` | Host resource coordinator | Performance/homologation owner |
| `M12-T17` | Work undercharge or overflow | Versioned work model, accounting classes and checked arithmetic | `RE-11`, `RE-12`, `RE-23` | MOD-12 owner | Security reviewer |
| `M12-T18` | Cancellation/deadline ignored | Bounded cooperative chunks, pre-reservation deadline, liveness lease and deterministic precedence | `RE-13`–`RE-15`, `RE-27`, `RE-28`, `HM-01` | MOD-12 owner | Host resource coordinator |
| `M12-T19` | Concurrent oversubscription/starvation | Atomic hierarchical reservations, parent window quotas and deterministic eligible-scope rotation | `RE-16`–`RE-19`, `RE-24`, `RE-29` | Host resource coordinator | Independent fairness reviewer |
| `M12-T20` | Authorising partial/oversized result | Complete-only pass, absent subset aggregates and admitted truthful output | `RE-20`, `RE-21`, `RE-25` | Evaluation owner | Quality reviewer |
| `M12-T21` | Corpus poisoning, replay or bias | Governed immutable manifest, review, expiry and segment evidence | `CO-01`–`CO-05` | Dataset owner/steward | Data governance reviewer |
| `M12-T22` | Sensitive diagnostic leakage | Stable sanitised codes and no content/private material | `RE-22` | Security and privacy owner | Evaluation owner |

## Deterministic future test plan

No vector in this section has been implemented or executed. Contract vectors use fixed clocks/schedulers, bounded cooperative fixtures and sanitised evidence. Real latency and memory claims are separated into later empirical homologation campaigns.

### Trust and checkpoint vectors

| ID | Precondition/input | Expected decision/code | Work/memory constraint | Required evidence |
|---|---|---|---|---|
| `TR-01` | Active generation `n`; valid signed `n-1` presented | Reject `trust.rollback`; remain at `n` | No semantic evaluation or checkpoint write | Before/after head and refusal audit |
| `TR-02` | Active `n`; exact `n` and digest replayed | Idempotent success | No new allocation beyond bounded verification; no advance | Unchanged checkpoint revision/head |
| `TR-03` | Active `n`; same generation with another valid digest | Quarantine `trust.split_view` | Stop before MOD-12 consumption | Both digests and quarantine transition |
| `TR-04` | Previously active store missing, corrupt or known restored | Quarantine `trust.continuity_unproved` | No last-known-valid consumption | Restore marker/corruption fixture and state |
| `TR-05` | Active `n`; valid signed `n+2` without `n+1` chain | Quarantine `trust.generation_gap` | No blind fast-forward | Gap evidence and unchanged head |
| `TR-06` | Crash/fault injected before, during and after local commit | Only old-complete or new-complete tuple visible | Reservation released exactly once | Transaction/CAS trace and restart state |
| `TR-07` | Frozen, expired, not-yet-valid and regressed-clock inputs | Quarantine `trust.freshness_unproved` | Bounded verification only | Virtual-clock trace and deterministic status |
| `TR-08` | Valid bundle replayed across tenant/environment/purpose/scope | Reject `trust.scope_mismatch` | No telemetry read | Signed scope versus requested scope |
| `TR-09` | Grant/revocation/bundle key reused, wrong-role signature, or required `RoleKeyDelegation` missing/forged/expired/revoked | Reject `trust.authorisation_unproved` before state change | Bounded cryptographic work | Root/delegation/role/key identifiers and sanitised code |
| `TR-10` | Valid operational signer exceeds a valid policy-authorisation ceiling | Reject `trust.authorisation_unproved` | No checkpoint advance | Authenticated assertion, canonical sets and subset result |
| `TR-11` | Unsupported schema/algorithm or downgrade candidate | Reject closed | Bound parsing and signature attempts | Selected profile and refusal code |
| `TR-12` | Normal key overlap then retirement | Accept contiguous generations only | Old key unusable after retirement | Chain, validity windows and final key set |
| `TR-13` | Compromised operational signer/root recovery attempt | Ordinary chain cannot self-recover; affected domain quarantines | No normal consumption during recovery | Separate recovery approval/epoch evidence |
| `TR-14` | Two consumers receive different signed branches | Detect on authenticated reconciliation; neither branch auto-wins | No cross-branch consumption after detection | Independent heads and reconciliation result |
| `TR-15` | Audit entries deleted, changed, reordered or restored | Current continuity-head/intent mismatch quarantines; unrelated diagnostic-history tamper opens an incident while an independently intact current head remains active | No trust state repair from audit alone | Classified audit-chain/head comparison |
| `TR-16` | Missing/fabricated/altered/expired policy assertion; valid scope alias, wildcard, parent/child, overlap or superset attempt | Reject `trust.authorisation_unproved`/`scope_mismatch`; only canonical non-empty subset passes | No checkpoint advance or telemetry read | Independent assertion signature/delegation and atom-by-atom predicate |
| `TR-17` | Recovery advances epoch/series, then a valid old epoch is replayed | Reject `trust.epoch_superseded`; domain high-water never decreases | No old subordinate checkpoint lookup as bootstrap | Domain head before/after and superseded set |
| `TR-18` | Offline/connected consumer at `n` receives bounded complete chain `n+1..n+k`; variants omit an authenticated generation or tamper an intermediate signature | Complete chain advances atomically; authenticated omission returns `trust.generation_gap` and quarantines; unauthenticated/tampered candidate rejects and preserves a fresh intact current tuple | Bound chain bytes/generations; no intermediate exposure | Per-hop validation, resulting state and one final commit only on success |
| `TR-19` | Malformed, unsupported or bad-signature candidate arrives while current tuple is fresh/intact | Reject/audit candidate; current tuple remains `Active` | Bounded ingress only | Current head/freshness unchanged and refusal evidence |

### Resource and backpressure vectors

| ID | Precondition/input | Expected decision/code | Work/memory constraint | Required evidence |
|---|---|---|---|---|
| `RE-01` | Each numeric limit at `max-1`, `max`, `max+1` | First two follow policy; last rejects exact dimension | Never consume above effective limit | Reserved/consumed counters |
| `RE-02` | Policy-assertion, bundle or request ceiling attempts to exceed an earlier trusted ceiling | Dimension-wise minimum across all four groups wins | No extra reservation | Four source limits and effective result |
| `RE-03` | Declared count smaller/larger than observed | Reject `resource.source_mismatch` on mismatch | Stop at bounded next item | Declared/observed counts and read count |
| `RE-04` | Mutable or infinite cooperative source | Reject deterministically | Bounded reads and virtual cancellation checkpoints | Reader trace |
| `RE-05` | Encoded bytes at limit and one byte over | Over-limit input rejects `resource.encoded_bytes` | No buffer beyond reserved cap | Bounded-reader byte count |
| `RE-06` | Small encoded input expands above cap/ratio | Reject `resource.expanded_bytes` | Stop expansion before exceeding reservation | Encoded/expanded counters |
| `RE-07` | Depth/string/collection/cardinality at and above cap | Above-cap input rejects `resource.structure`/`resource.cardinality` | No oversized object graph | Parser counters and allocation trace |
| `RE-08` | One valid item would make aggregate exceed limit | Reject before allocating/processing that item | Previous complete work only | Per-item reservation decision |
| `RE-09` | Versioned accounted-memory formula exactly available then short by one accounted byte | Exact admission succeeds; short rejects `resource.memory` | No relevant allocation before rejection | Profile/formula and reusable-capacity ledger |
| `RE-10` | Success, refusal, exception, deadline and cancellation | Reusable leases/unused reservation release exactly once; consumed cumulative/window allowance does not roll back | Reusable totals return to baseline only | Separate capacity/consumption/window ledgers |
| `RE-11` | Maximum bounded crypto, parse, sort and analysis inputs | Work model charges each operation, including non-linear cost | `consumed <= reserved` | Versioned work breakdown |
| `RE-12` | Sums/products near integer maximum | Reject `resource.arithmetic_overflow` | No wrapped allocation/work value | Checked-arithmetic result |
| `RE-13` | Virtual time strictly before, exactly at and after deadline | Before may proceed; equality/after returns `resource.deadline` | No work/acquisition begins at or after expiry | Virtual monotonic-clock trace |
| `RE-14` | Cancellation during cooperative read, expand, parse, verify, sort and analyse chunks | `resource.cancelled`; no new work after observation | Bounded virtual chunks | Injection point and observation order |
| `RE-15` | Source primitive lacks cancellation/deadline contract | Reject `resource.source_not_cancellable` before it can hold capacity | No source call or long-lived lease | Source-capability decision |
| `RE-16` | Two admissions exceed one global slot/reservation | One wins atomically; other gets `resource.capacity_global` | No oversubscription | Deterministic barrier and ledger |
| `RE-17` | Simultaneously eligible scopes contend repeatedly | Canonical rotation prevents a second win before another eligible peer receives an opportunity | All hierarchical caps preserved | Virtual eligible-set/scheduler order |
| `RE-18` | Scope hopping or ledger growth via child digests, many valid child scopes, provider labels, aliases or fragmented requests | Parent buckets aggregate all children and metadata cardinality/lifetime; no extra quota/unbounded entry growth | Same parent window and metadata ceiling | Authorisation versus quota keys and bucket ledger |
| `RE-19` | Backlog while queue is disabled | Immediate backpressure; no persisted/retried work | Zero queued items | Admission and storage inspection |
| `RE-20` | Deadline after one case and during the next | Only completed case may be diagnostic; `IncompleteNonAuthorising` | Partial case discarded | Output disposition and counters |
| `RE-21` | Incomplete subset has apparently perfect metrics | Precision/recall/calibration/aggregates are absent/null and no gate is emitted | No implicit resume/merge | Serialised fields and gate result |
| `RE-22` | Refusal fixture contains sensitive topology/content | Stable sanitised code only | Diagnostic output size bounded | Secret/content canary scan |
| `RE-23` | Mandatory dimension missing, zero, negative, unbounded or unsupported; checked sum/product overflows | Reject `resource.envelope_invalid`/`resource.arithmetic_overflow` | No allocation or acquisition from invalid value | Schema/value matrix and checked result |
| `RE-24` | Bootstrap succeeds but one authenticated hierarchical bucket/capacity acquisition fails | Roll back all partial acquisitions and reject exact capacity/quota code | No work or leaked lease | Atomic acquisition ledger |
| `RE-25` | Truthful output at encoded/accounted-memory limit and one unit over | At limit completes; over-limit run is non-passing without truncated truth | Output never exceeds reservation | Output fields, disposition and size ledger |
| `RE-26` | Expansion-ratio input has zero encoded length, boundary ratio or overflowing multiplication | Empty valid form follows schema; undefined/overflow ratio rejects safely | No division-by-zero/wrapped cap | Encoded/expanded arithmetic trace |
| `RE-27` | Trust/source violation, resource breach, cancellation and deadline become observable at the same checkpoint | Declared precedence yields one stable code without extra discovery work | No later work | Virtual condition matrix and terminal code |
| `RE-28` | Ingress lease acquired but first byte never arrives, or a cooperative source stalls between reads | No first byte returns `resource.first_byte_timeout`; inter-read stall returns `resource.idle_timeout`; release lease exactly once | Lease never outlives total deadline | Virtual liveness clock and ledger |
| `RE-29` | One canonical scope repeatedly reacquires while a peer becomes eligible in the same parent/window | Parent window tokens and rotation bound dominance; serial no-queue profile makes no broader fairness claim | No backlog/retry | Virtual window/rotation evidence |

### Later empirical homologation campaigns

These campaigns are not deterministic contract vectors and cannot be satisfied by a virtual clock:

| ID | Measurement | Required environment/evidence |
|---|---|---|
| `HM-01` | Real cancellation, first-byte and idle latency for each approved source/parser primitive at its maximum admitted size | Declared hardware/runtime/OS, repeated distribution, worst observed latency and failure threshold |
| `HM-02` | Heap, working-set and allocation peak versus the versioned accounted-memory formula | Declared runtime/architecture, profiler/counters, warm/cold repetitions and conservative headroom decision |
| `HM-03` | CPU/elapsed cost versus deterministic work model under crypto, parse, sort and analysis boundaries | Declared environment, repeatable fixtures, variance and calibration decision without replacing work limits with time |

### Corpus-governance vectors

| ID | Precondition/input | Expected decision | Work/memory constraint | Required evidence |
|---|---|---|---|---|
| `CO-01` | Manifest/content digest tampered | Quarantine corpus | No evaluation | Expected/observed digests |
| `CO-02` | Duplicate/leaked cases across governed partitions | Reject or report factual invalid partition | Bounded duplicate index | Duplicate identifiers/digests |
| `CO-03` | Corpus replayed cross-scope or after expiry | Reject | No evaluation | Scope/clock comparison |
| `CO-04` | Segment omitted or materially imbalanced | Non-passing coverage report, no general claim | Output bounded by manifest segments | Expected/observed distribution |
| `CO-05` | Label flip, conflicting evidence or poisoned transformation | Quarantine affected revision | No automatic correction or learning | Source/label/reviewer lineage |

## Documentary acceptance checklist

| Requirement from the authorised proposal | Documentary result |
|---|---|
| Responsibilities and trust boundaries are explicit | Responsibility matrix and mandatory separation above |
| ADR compares at least three alternatives | `ADR-0007` compares four alternatives |
| Rollback, freeze, split view, fast-forward, compromise and restore have controls | Checkpoint state machine and traceability `M12-T02`–`M12-T13` |
| Missing/restored checkpoint quarantines until authenticated reconciliation | `Quarantined`/`Recovering` transitions and restore limitation |
| Normal rotation differs from compromise recovery | Separate rotation and recovery sections |
| Checkpoint never decreases normally | Direct-successor and rollback rules |
| Envelope covers counts, bytes, memory, work, time, cancellation, quota, fairness, partial results and concurrency | Resource envelope and contract vectors `RE-01`–`RE-29`; physical evidence remains `HM-01`–`HM-03` |
| Every threat has control, future vector and owner | Traceability table `M12-T01`–`M12-T22` |
| Corpus has governed manifest and adversarial plan | Manifest plus `CO-01`–`CO-05` |
| No provider-specific or secret dependency | Explicit non-goals and provider-neutral keys/scopes |
| No hidden critical unknown presented as resolved | Residual restore/split-view and unmeasured numerical limits are explicit |
| No runtime or mode promotion | Status, non-goals and implementation gate below |

## Implementation and promotion gate

Architecture, security and data reviewers must review this package and Bruno must make a separate Human Gate decision. Even acceptance authorises no code.

A future implementation proposal must separately name components, owners, store, transaction/restore model, provisioning/reconciliation mechanism, measured limits, test environment and rollback. It must obtain explicit authority before changing a source file or creating a runtime. `none → OBSERVER` remains a later independent decision requiring implementation evidence, security review, offline evaluation evidence, Quality Gate and dedicated Human Gate.
