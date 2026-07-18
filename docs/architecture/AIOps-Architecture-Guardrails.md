# AIOps/AI Architecture Guardrails

## Status

MOD-12 remains inactive and outside the runtime baseline. A local, in-memory and non-mutating Observer foundation implements bounded evidence, threshold and capacity-analysis contracts inside Application. Restricted `STATE-06` increments add a trusted canonical-health adapter, authenticated purpose-specific policy grants with independently signed revocation snapshots, explicit offline processing budgets and a deterministic segmented evaluation runner. A later documentation-only increment defines a reviewed conceptual contract for trust governance, durable checkpoint semantics and a fuller resource envelope supporting proposed [ADR-0007](ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md); none of those future boundaries is implemented. Nothing is registered with a runtime pipeline, provider, persistence, UI, LLM or executor, and nothing satisfies the `none → OBSERVER` promotion gate. This document defines the data, risk, contract and evaluation boundaries so later work cannot mistake local integration evidence or documentary design for an activated mode or bypass deterministic controls.

## Allowed initial mode

No mode is active. The first mode eligible for a later independent promotion is `OBSERVER`: consume sanitised, authorised telemetry and produce non-mutating signals/evaluation artefacts. `OBSERVER`, `ADVISOR`, `ASSISTANT`, and `CONTROLLED_AUTOMATION` each require their applicable opt-in policy, technical evidence, Quality Gate and dedicated Human Gate.

## Data contract

Every AI-consumable item derives from canonical contracts and adds:

- source/provenance and authorization scope;
- schema and transformation version;
- event/observation time and freshness;
- unit, quality, missingness, and cardinality;
- classification/redaction result;
- retention class and permitted purposes;
- immutable link to the underlying evidence.

Secrets, full connection strings, private keys, enrollment material, unapproved query text, and raw database content are forbidden. AIOps storage is separate from operational command state.

## Pre-runtime trust governance

Any future policy/trust distribution design must preserve these invariants:

- the root trust anchor is provisioned independently and is never accepted from the bundle or evidence it validates;
- policy and revocation decisions require independently authenticated dual control and are materialised by separate attestation roles; an attestation signer, bundle-carried ID/digest or operational signature alone is insufficient;
- the host evaluates a bounded canonical-set subset predicate before trusting the scope digest; aliases, wildcards, partial overlap and supersets fail closed;
- root custody/governance, policy/revocation decisions, policy/revocation/corpus attestation, role-specific signers, publisher, host trust coordinator, checkpoint owner, verifier and independent auditor have distinct identities and incompatible permissions;
- root, decision, policy-attestation, trust-bundle, grant, revocation-decision/snapshot and corpus-manifest roles use distinct root-delegated key identities and distinct public-key material; root material is never reused operationally and an operational signer cannot delegate its successor;
- every subordinate approval/assertion/delegation/grant/revocation/corpus/bundle artefact binds a host-derived non-circular `epochContextId`; recovery makes all old-epoch artefacts ineligible and requires fresh identifiers/issuance;
- the publisher/transport is untrusted for policy, ordering and freshness even if its transport is authenticated;
- trust, root, cumulative revocation and corpus-head updates accept only a direct monotonic successor or trusted-locally-bounded complete chain, each bound to its exact predecessor; revocation tombstones are irreversible within an epoch and identifiers are never reused;
- lower generations are rejected, equal generations with another digest quarantine the scope, and gaps require separately authorised reconciliation rather than a highest-generation-wins rule;
- a stable constant-size trust-domain head stores root-set version/digest and recovery-epoch high-water outside the subordinate checkpoint key; series is immutable within an epoch, counters never wrap and superseded epochs cannot bootstrap as new domains;
- bootstrap, root transition and compromise recovery require distinct replay-resistant one-use approvals under independently selected bootstrap, current-root-transition or out-of-band recovery trust; each precommits to the exact proposed head, and deletion is not first installation without an independent pristine marker;
- planned root rotation revalidates every candidate artefact under the proposed remaining root set and cannot remove a root while any active candidate artefact depends exclusively on it;
- root reference, consumed approval, bundle, revocation state, corpus heads, stable domain head, subordinate checkpoint and audit intent become visible through one future local transaction only; this never implies fleet-wide simultaneous atomicity;
- missing, corrupt, cloned, restored, stale or divergent current continuity quarantines MOD-12 until authenticated independent reconciliation/recovery;
- malformed, unauthenticated, expired or not-yet-valid candidate bytes are rejected without deactivating a fresh intact current tuple; authenticated equivocation/gaps, current expiry or continuity damage quarantine;
- bounded host-reserved control-plane capacity prevents evaluation saturation from blocking a root, revocation, policy or corpus-head update; a committed update cancels old-context evaluations and publication revalidates the exact active revision; and
- a local checkpoint restored with its local audit cannot prove silent anti-rollback or absence of split view without an independent witness, monotonic anchor or reconciliation source.

The pure MOD-12 verifier owns no network, private key, persistence, recovery or publication responsibility. The documentary increment has been reviewed and accepted as documentation; `ADR-0007` remains explicitly `proposed`. No review of the unchanged package is pending, and any later ADR adoption or implementation requires separate authority without activating MOD-12 automatically.

## Resource admission and backpressure

The current offline runner proves only local case/sample/work/time admission. A future resource boundary must additionally:

- start an absolute monotonic deadline before any reservation, use a short ingress lease inside one uniquely fenced host/process coordinator, and require first-byte/inter-read liveness plus a cooperative cancellation/deadline-aware source;
- apply a trusted bounded reader before authentication and charge encoded bytes, expanded bytes, expansion ratio, parser structure and cardinality before relevant allocation;
- compose fields by class: validated numeric ceilings use the minimum, allowlists intersect, deny dominates capabilities, profiles require exact/approved compatibility and queue state remains disabled; a bundle above policy or request above the trusted result rejects instead of silently clamping;
- classify every producer declaration as exact or maximum, check exact equality at end-of-input, refuse an exceeded maximum and fail closed when an applicable declaration is omitted;
- after authentication, acquire coordinator-global and hierarchical tenant/environment/authority/purpose/exact-scope buckets atomically, keeping authorisation keys separate from quota keys;
- distinguish reusable leases (slots/accounted memory/output), monotonic per-execution consumption (bytes/work/time), window quota tokens, bounded control metadata and audit counters; lease expiry requests cancellation but reusable capacity releases only after proved quiescence or authorised termination fencing;
- use checked arithmetic and a versioned work model that includes parsing, cryptography, sorting, copying, analysis and result construction;
- use a versioned conservative accounted-memory formula for admission, while real heap/working-set memory and cancellation latency remain later empirical evidence and never direct admission controls;
- use bounded phase work and the declared deterministic terminal precedence; equality with an absolute deadline is expired;
- keep a future queue disabled until separately authorised with depth, byte, age and discard limits;
- make no deterministic fairness or starvation-freedom claim for the first serial/no-queue proof; a future bounded contender registry/queue and cross-restart quota state require separate authority/evidence;
- pin each evaluation to an active immutable trust/corpus context whose deadline cannot outlive its components, cancel superseded work and refuse stale publication; and
- mark every timeout, cancellation, supersession or partial execution as incomplete and non-authorising; subset precision, recall, calibration and aggregates are absent/null rather than serialised.

The first future local proof remains serial (`maximum parallelism = 1`). Safe operational numbers require reproducible measurement; no number is established by this documentary contract.

## Processing boundaries

```text
Canonical sanitized telemetry
  -> deterministic rules
  -> statistical/anomaly signals
  -> correlation hypotheses
  -> approved knowledge retrieval
  -> optional LLM synthesis
  -> structured recommendation only
  -> deterministic risk/RBAC/approval system
  -> typed provider command path (future gated mode)
```

The LLM has no database driver, vault, Agent command, shell, SQL, or provider-executor tool. Retrieved content is untrusted data and cannot change system instructions or authorization.

## Evaluation contract

Before promotion, maintain a versioned offline evaluation set covering:

- groundedness/evidence citation;
- precision/recall and false-positive/negative rates;
- confidence calibration and `INSUFFICIENT_EVIDENCE` behavior;
- stale, missing, conflicting, poisoned, and adversarial inputs;
- prompt injection and exfiltration attempts;
- provider/version/platform segmentation;
- cost, latency, availability, and deterministic fallback;
- recommendation acceptance/rejection and harmful-action rate.

Datasets use independently approved role-signed manifests and monotonic corpus heads recording exact case/partition membership, owner, source, consent/authority, classification, time range, exclusions, transformations, quantitative segment criteria, known bias, expiry and irreversible withdrawal. Production feedback never changes a corpus, model or policy automatically.

## Risk and action boundary

- Risk class is determined by deterministic policy, never by model output.
- Recommendation, plan, approval, command creation, execution, and verification are distinct audited records.
- Free-form model text cannot become SQL/shell/native arguments.
- AI-derived output or policy can never delete data autonomously; destructive operations retain an authorised human owner, double confirmation, a rehearsed runbook, verified recovery and the most restrictive applicable policy.
- Kill switch disables AI-derived recommendations/automation without disabling deterministic monitoring.
- Low confidence, missing evidence, policy conflict, or model unavailability falls back to deterministic behavior.

## Retention and privacy

- No AIOps dataset is collected by default.
- Feature/embedding/knowledge retention requires purpose-specific policy and deletion behavior.
- Internal incidents used for retrieval preserve access scope and provenance.
- Model/provider data-processing terms and geographic constraints require legal/security approval before external inference.

## Promotion gates

| Promotion | Minimum evidence |
|---|---|
| none → OBSERVER | canonical telemetry, classification, offline evals, no executor access |
| OBSERVER → ADVISOR | grounded recommendations, calibrated confidence, citations, red team |
| ADVISOR → ASSISTANT | typed plans, deterministic risk, approval UX, audit, no implicit execution |
| ASSISTANT → CONTROLLED_AUTOMATION | action-class homologation, opt-in scope, rollback, kill switch, live monitoring and incident response |

No mode promotion is implied by the general DB-Notifier lifecycle state.
