# MOD-12 Observer Foundation Report

## Status and authority

- Date: 2026-07-16
- Workspace lifecycle position: `STATE-05 FRONTEND_IMPLEMENTATION`, progression on hold and Human Gate pending
- MOD-12 mode status: no mode promoted; `none → OBSERVER` remains pending
- Authority: Bruno requested execution of the reconciled `Prompt-IA.md` and designated AIOps/AI as DB-Notifier's principal strategic differentiator
- Delivered scope: local, deterministic, in-memory, non-mutating foundation only

This report is factual evidence for one increment. It does not change the lifecycle state, activate MOD-12, approve a Human Gate, declare provider support, authorise data collection or permit external inference or administrative execution.

## Outcome

The loose intake file was reconciled with its existing canonical owner, [`../prompts/foundation/AIOps-And-AI-Module.md`](../prompts/foundation/AIOps-And-AI-Module.md), and removed from the repository root. The canonical specification retains its stronger security and governance controls while adding the product-owner priority, estimated plan duration and an explicit prohibition on autonomous data deletion.

The first implementation increment lives in `DBNotifier.Application.AIOps`. It deliberately uses the existing provider-neutral Application boundary rather than creating a runtime assembly or UI preview before an owning integration exists.

Implemented:

- A bounded numeric evidence contract with immutable evidence ID, instance and authorisation scope, observation/receipt time, unit, quality, missingness, single-series cardinality, source/schema/transformation provenance, classification, redaction result, ephemeral retention and permitted purpose.
- Contract-shape validation for finite values and bounded stable identifiers. Handling and authorisation fields remain caller assertions; a future trusted adapter must derive and authenticate them from canonical authorised telemetry.
- No-lookahead evidence selection that fails closed for missing, stale, future, timestamp-conflicting, unknown-quality, duplicate-time or mixed in-window source contracts.
- Versioned deterministic threshold rules with a separate evaluation window and freshness limit, typed comparisons, consecutive-sample debounce, maximum gap, evidence links, validity and stable limitation codes.
- An explainable ordinary-least-squares capacity analyser with model/policy versions, evidence window, freshness, sample count, historical RMSE, scale-independent fit quality, a two-sided 95% slope interval and a point threshold estimate from the complete fitted line.
- Conditional time-sensitivity bounds derived by holding the fitted intercept fixed at the slope interval bounds. They are explicitly labelled as sensitivity estimates, not a 95% confidence interval for exhaustion time.
- Conservative horizon classification: detected only when the full sensitivity range is within the horizon, not detected only when the full range is outside, and `InsufficientEvidence` when it straddles the horizon.
- Bounded request/policy materialisation, deterministic ordering, cancellation between analysers, numeric overflow refusal and safe temporal saturation.
- A report capability declaration that is always `OBSERVER` and false for collection, persistence, LLM, recommendation, planning and execution.
- An architecture allowlist for the exact public AIOps contract and its absence of outer DB-Notifier types.

No input contains free-form query, log, document, secret, credential, connection string, SQL, shell or executable payload fields.

## Prompt coverage after this increment

| Layer | Current disposition | Evidence and remaining boundary |
|---|---|---|
| 1. Collection and normalisation | `PARTIAL` | Consumer-side sanitised numeric contract only; no collector, adapter, runtime binding or automatic dataset |
| 2. Deterministic rules | `PARTIAL` | Typed numeric thresholds, debounce, freshness/window and evidence; no persisted configuration, stateful cooldown or alert integration |
| 3. Statistics and prediction | `PARTIAL` | One explainable OLS capacity model; no seasonality, anomaly catalogue, backtesting service or production calibration |
| 4. Intelligent correlation | `PLANNED` | No causal or correlation engine |
| 5. Knowledge base | `PLANNED` | No ingestion, retrieval, embedding or external content |
| 6. LLM | `PLANNED / DISABLED` | No model dependency, prompt, inference or network path |
| 7. Planning | `SPECIFIED ONLY` | No plan type or plan creation in the public foundation |
| 8. Risk, policy and approval | `SPECIFIED ONLY` | Deterministic future contract; no approval workflow or UI |
| 9. Executor | `PROHIBITED IN THIS SCOPE` | No command, provider, credential, vault, SQL, shell or executor surface |
| 10. Feedback and learning | `PLANNED` | No feedback collection, dataset or model/policy mutation |

The code therefore proves a foundation, not completion of the ten-layer module.

## Security and architecture boundaries

- The foundation consumes caller-supplied in-memory evidence and performs no I/O.
- It has no reference to a concrete provider, persistence implementation, Agent runtime, API runtime, UI, network client, secret store, LLM or executor.
- Every highlighted result retains `AnalysisId`, `InstanceId`, `AuthorisationScopeId` and immutable evidence links.
- Mixed source/schema/transformation contracts inside the analysed window become `InsufficientEvidence`; an expired version outside the window does not poison a homogeneous current window.
- A trusted future integration must map canonical contracts to these types after server-side authorisation, classification and redaction. Direct deserialisation of untrusted caller assertions is not authorised.
- The synchronous work is bounded to 10,000 samples and 100 policies per analyser type, and cancellation is checked between policies. A remote pipeline still requires window caching, total work budgeting, backpressure and its own load evidence before integration.
- No dataset is collected or persisted by default.
- No finding is a recommendation, permission, plan, command or proof of causality.

## Verification

Environment: Windows, repository-local .NET SDK `10.0.301`, Release configuration, 2026-07-16.

| Check | Result |
|---|---|
| Focused MOD-12 regressions | `34/34` passed |
| Full solution tests | `219/219` unit and `14/14` architecture passed |
| Coverage gate | passed: `79.62%` lines, `58.4%` branches; minimums `70%`/`45%` |
| Full solution build | passed with zero warnings/errors using isolated `C:\tmp\DBNotifier-AIOps-Solution-Validation` output |
| Normal output build | blocked only for WPF copy by pre-existing visible `DB Notifier` PID 6976; that process was not started, stopped or altered by this increment |
| Locked restore | passed for all 13 .NET projects; no dependency was added or changed |
| .NET format | passed with no changes required |
| Code-documentation gate | passed for 203 comment-capable source files; public API/en-GB review also completed manually |
| Markdown-link gate | passed for 189 local links in 63 files |
| Secret scan and diff checks | current non-ignored worktree secret scan and `git diff --check` passed |
| Fail-closed runtime smoke | passed: liveness `200`, protected HTTP endpoints `426`, Agent workers disabled and local persistence not initialised |

The isolated output changes only the validation destination; it compiles the complete solution, including WPF, against the same source and locked dependencies. The pre-existing visible DB Notifier process remains under user control.

## Gate classification

- Local implementation Quality Gate: `APPROVED` in the inactive foundation scope recorded here. This does not promote a MOD-12 mode or alter a lifecycle/Human Gate.
- `none → OBSERVER` promotion: `NOT REQUESTED / PENDING`. Minimum missing evidence includes a trusted canonical telemetry adapter, approved data policy, offline evaluation set and explicit Human Gate.
- `OBSERVER → ADVISOR` and later promotions: `NOT AUTHORISED`.
- Current `STATE-05` Human Gate: unchanged and still `PENDENTE`.

## Next MOD-12 increments, in order

1. Define the trusted canonical telemetry adapter and purpose-specific data policy; keep collection opt-in and persistence disabled by default.
2. Build an offline evaluation corpus with provenance, authority, classification, expiry, provider/version/platform segmentation and adversarial fixtures.
3. Add robust baseline/anomaly and correlation components with reproducible evidence, calibration and deterministic fallback.
4. Re-run security, load, false-positive/negative and data-poisoning evaluations, then request the independent `none → OBSERVER` Quality/Human Gate.
5. Only after that promotion, integrate a read-only pipeline and factual UI. LLM-backed `ADVISOR`, typed planning and any execution remain separate future gates.

The immediate repository gate remains the pending `STATE-05` human validation documented in [`STATE-05-Human-Gate-Validation.md`](STATE-05-Human-Gate-Validation.md); this increment does not replace or expand that review.
