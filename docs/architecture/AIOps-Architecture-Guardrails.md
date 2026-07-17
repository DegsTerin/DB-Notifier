# AIOps/AI Architecture Guardrails

## Status

MOD-12 remains inactive and outside the runtime baseline. A local, in-memory and non-mutating Observer foundation implements bounded evidence, threshold and capacity-analysis contracts inside Application. Restricted `STATE-06` increments add a trusted canonical-health adapter, authenticated purpose-specific policy grants with independently signed revocation snapshots, explicit offline processing budgets and a deterministic segmented evaluation runner. A later documentation-only increment defines a review candidate for trust governance, durable checkpoint semantics and a fuller resource envelope in proposed [ADR-0007](ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md) and its [conceptual contract](AIOps-Trust-Governance-And-Resource-Envelope.md); none of those future boundaries is implemented. Nothing is registered with a runtime pipeline, provider, persistence, UI, LLM or executor, and nothing satisfies the `none → OBSERVER` promotion gate. This document defines the data, risk, contract and evaluation boundaries so later work cannot mistake local integration evidence or documentary design for an activated mode or bypass deterministic controls.

## Allowed initial mode

The only initial mode is `OBSERVER`: consume sanitized, authorized telemetry and produce non-mutating signals/evaluation artifacts. `ADVISOR`, `ASSISTANT`, and `CONTROLLED_AUTOMATION` each require separate opt-in policy, technical evidence, Quality Gate, and Human Gate.

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
- policy authorisation is materialised as a separately signed assertion under a distinct root-delegated role; a bundle-carried ID/digest or operational signature alone is insufficient;
- the host evaluates a bounded canonical-set subset predicate before trusting the scope digest; aliases, wildcards, partial overlap and supersets fail closed;
- root custody, policy authority, role-specific signers, publisher, host trust coordinator, checkpoint owner, verifier and independent auditor have distinct identities and incompatible permissions;
- policy-attestation, trust-bundle, grant and revocation signing use distinct root-delegated key identities and distinct public-key material; an operational signer cannot delegate its successor;
- the publisher/transport is untrusted for policy, ordering and freshness even if its transport is authenticated;
- a normal update is a direct monotonic successor or trusted-locally-bounded complete chain of direct successors, each bound to its exact predecessor and revocation head;
- lower generations are rejected, equal generations with another digest quarantine the scope, and gaps require separately authorised reconciliation rather than a highest-generation-wins rule;
- a stable trust-domain head keeps the recovery epoch/series high-water outside the epoch-specific checkpoint key, so superseded epochs cannot bootstrap as new domains;
- bundle, revocation state, stable domain head, subordinate checkpoint and audit intent become visible through one future local transaction only; this never implies fleet-wide simultaneous atomicity;
- missing, corrupt, cloned, restored or divergent continuity quarantines MOD-12 until authenticated independent reconciliation;
- malformed or unauthenticated candidate bytes are rejected without deactivating a fresh intact current tuple; authenticated equivocation/gaps, current expiry or continuity damage quarantine; and
- a local checkpoint restored with its local audit cannot prove silent anti-rollback or absence of split view without an independent witness, monotonic anchor or reconciliation source.

The pure MOD-12 verifier owns no network, private key, persistence, recovery or publication responsibility. Proposed `ADR-0007` remains subject to architecture, security, data and Human Gate review, and its acceptance would not authorise implementation.

## Resource admission and backpressure

The current offline runner proves only local case/sample/work/time admission. A future resource boundary must additionally:

- start an absolute monotonic deadline before any reservation, use a short global pre-authentication ingress lease, and require first-byte/inter-read liveness plus a cooperative cancellation/deadline-aware source;
- apply a trusted bounded reader before authentication and charge encoded bytes, expanded bytes, expansion ratio, parser structure and cardinality before relevant allocation;
- calculate every effective limit as the minimum of complete finite trusted-host, separately signed policy-assertion, bundle and per-request ceilings; each later layer can only reduce earlier trusted ceilings, while producer declarations may never raise them;
- after authentication, acquire global and hierarchical tenant/environment/authority/purpose/exact-scope buckets atomically, keeping authorisation keys separate from quota keys;
- distinguish reusable leases (slots/accounted memory/output), monotonic per-execution consumption (bytes/work/time), window quota tokens and audit counters; only reusable/unused capacity is released;
- use checked arithmetic and a versioned work model that includes parsing, cryptography, sorting, copying, analysis and result construction;
- use a versioned conservative accounted-memory formula, while real heap/working-set and cancellation latency remain later empirical evidence;
- use bounded phase work and the declared deterministic terminal precedence; equality with an absolute deadline is expired;
- keep a future queue disabled until separately authorised with depth, byte, age and discard limits;
- use parent window buckets and deterministic rotation among simultaneously eligible canonical scopes; the first serial/no-queue proof establishes containment, not general starvation freedom; and
- mark every timeout, cancellation or partial execution as incomplete and non-authorising; subset precision, recall, calibration and aggregates are absent/null rather than serialised.

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

Datasets record owner, source, consent/authority, classification, time range, exclusions, transformations, known bias, and expiry. Production feedback never changes a model/policy automatically.

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
