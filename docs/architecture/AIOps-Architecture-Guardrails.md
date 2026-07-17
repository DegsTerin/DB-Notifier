# AIOps/AI Architecture Guardrails

## Status

MOD-12 remains inactive and outside the runtime baseline. A local, in-memory and non-mutating Observer foundation implements bounded evidence, threshold and capacity-analysis contracts inside Application. Restricted `STATE-06` increments add a trusted canonical-health adapter, authenticated purpose-specific policy grants with independently signed revocation snapshots, explicit offline processing budgets and a deterministic segmented evaluation runner. None is registered with a runtime pipeline, provider, persistence, UI, LLM or executor, and none satisfies the `none → OBSERVER` promotion gate. This document defines the data, risk, contract and evaluation boundaries so later work cannot mistake local integration evidence for an activated mode or bypass deterministic controls.

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
