# STATE-06 MOD-12 Trusted Telemetry and Offline Evaluation Report

## Status and authority

- Date: 2026-07-17
- Workspace lifecycle position: `STATE-06 INTEGRATION`
- MOD-12 mode status: no mode promoted; `none → OBSERVER` remains pending
- Authority: Bruno explicitly authorised a local and isolated trusted canonical telemetry adapter, opt-in data policy and offline evaluations after the formal `STATE-05 → STATE-06` transition was recorded
- Explicit exclusions: LLM, executor, external database, external action and automatic promotion to `OBSERVER`

This report records one local implementation increment. It does not authorise collection, persistence, runtime binding, provider support, external telemetry, recommendation, planning, execution or lifecycle progression.

## Outcome

The increment adds a deliberately narrow integration boundary within `DBNotifier.Application.AIOps`:

- `CanonicalObserverTelemetryAdapter` accepts only the existing provider-neutral `HealthObservation` Domain contract plus an authoritative receipt instant.
- The first transformation emits only `database.probe.duration` in canonical milliseconds. It never copies provider type/version, method, limitations, normalised error text, native diagnostics or arbitrary labels into MOD-12 evidence.
- Evidence identity, instance and observation time derive from the canonical observation. Authorisation scope, classification, sanitisation result, retention and permitted purpose derive from the explicit data policy rather than telemetry assertions.
- Exact instance-and-Agent allowlisting, purpose matching, policy-period containment, UTC/no-lookahead checks, canonical bounds, status/evidence consistency, evidence strength and synthetic-evidence restrictions fail closed with stable sanitised codes.
- The public AIOps architecture allowlist permits exactly `HealthObservation` as the sole outer DB-Notifier signature type. No provider, persistence, Agent, API, UI or infrastructure type crosses the boundary.

The adapter is not registered with dependency injection, a worker, API endpoint, Agent loop or user interface. Calling it remains an explicit in-memory application action and does not activate `OBSERVER`.

## Purpose-specific opt-in data policy

`ObserverDataPolicy` provides a bounded policy with:

- zero-value `Disabled` opt-in and an explicit `Enabled` state;
- a single declared use: local runtime analysis or isolated offline evaluation;
- a non-empty authorisation scope, inclusive effective instant and exclusive expiry;
- up to 1,000 exact instance-and-Agent source pairs;
- synthetic evidence allowed only by an offline-evaluation policy;
- fixed operational-telemetry classification, sanitised status, ephemeral analysis-only retention and non-mutating purpose;
- immutable `false` declarations for collection, persistence and Observer-mode activation.

The policy is not an authentication credential or signed authorisation snapshot. The owning server-side application boundary must construct it only after authenticating the caller and resolving current scope. This increment proves deterministic enforcement of a supplied policy, not the provenance of that policy.

## Governed offline evaluation

The in-memory offline framework requires dataset ID/version, non-secret provenance and authority references, operational classification, UTC creation/expiry, deterministic case IDs and explicit provider/version/platform segmentation. It caps a dataset at 1,000 cases and each case at the existing 10,000-sample analysis bound, checks cancellation between cases and samples, and performs no file, database or network I/O.

The versioned test corpus `observer.reference-corpus` contains five synthetic cases in the `synthetic-db` / `1.0.0` / `offline-windows` segment:

| Case class | Expected outcome | Observed outcome |
|---|---|---|
| Sustained duration threshold | Detected | Exact pass |
| Below-threshold duration | Not detected | Exact pass |
| Future telemetry | Adapter rejection | Exact pass |
| Disabled opt-in | Adapter rejection | Exact pass |
| Cross-instance evidence | Evidence-boundary rejection | Exact pass |

Observed corpus metrics were five exact passes, one true positive, one true negative, zero false positives, zero false negatives, precision `1.0`, recall `1.0`, three rejected adversarial cases and zero accepted adversarial cases. These values characterise only this small deterministic synthetic corpus; they are not provider calibration, production accuracy or general poisoning resistance.

## Security and architecture boundaries

- No new dependency or project reference was added.
- No secret, query text, connection string, executable content, free-form provider payload or native diagnostic enters the adapted evidence contract.
- The adapter rejects disabled, wrong-purpose, premature, expired, pre-policy, wrong-source, future, reversed-time, non-UTC, unknown-evidence, unauthorised synthetic, healthy-transport-only and excessive-duration inputs.
- Offline evaluation uses the same trusted adapter and deterministic threshold analyser; it contains no LLM, model download, knowledge retrieval, recommendation, plan or executor surface.
- No data is collected or persisted by MOD-12. The existing fail-closed Agent remained disabled and did not initialise SQLite during the local runtime smoke.
- No external database, provider runtime, credential, IdP, vault, channel, service control, infrastructure mutation or external action was used.

## Verification

Environment: Windows, repository SDK .NET 10, Release configuration, 2026-07-17.

| Check | Observed result |
|---|---|
| Focused MOD-12 tests | `54/54` passed |
| Full unit/model/provider/presentation suite | `250/250` passed |
| Architecture suite | `15/15` passed |
| Full solution build | passed with zero warnings and zero errors |
| Coverage gate | passed: `80.02%` lines and `60.30%` branches; floors `70%`/`45%` |
| .NET format/analyser gate | passed with no required changes |
| Code-documentation gate | passed; new public APIs received XML documentation and en-GB review |
| Secret scan | passed for the current non-ignored worktree and available Git history |
| Fail-closed runtime smoke | passed: liveness `200`, protected HTTP endpoints `426`, Agent workers disabled and local persistence absent |
| Diff and Markdown links | passed after factual documentation synchronisation |

Online NuGet/npm vulnerability audits were not repeated because this authority excludes external actions and the increment adds or changes no dependency. Dashboard, browser, WPF visual, legacy PowerShell and provider-runtime suites were not repeated because no owning surface changed.

## Gate classification

- Local implementation Quality Gate: `APPROVED` for the isolated adapter, policy and offline-evaluation scope recorded here.
- `none → OBSERVER`: `PENDING`; this increment neither requests nor grants promotion.
- `OBSERVER → ADVISOR` and later modes: `NOT AUTHORISED`.
- Lifecycle position: remains `STATE-06 INTEGRATION`; no transition is implied.

## Remaining conditions before any Observer promotion

- Bind policy creation to authenticated, current server-side authorisation and revocation evidence.
- Expand governed datasets across authorised provider/version/platform segments and representative operational distributions.
- Add independent poisoning, tampering, replay, load, cancellation and total-work-budget evidence plus runtime backpressure design.
- Calibrate false-positive/negative and freshness behaviour beyond the five-case synthetic corpus.
- Complete a dedicated security review, automatic Quality Gate and explicit Human Gate for `none → OBSERVER`.

LLM-backed recommendation, planning and any typed executor remain separate future increments and gates after `OBSERVER`; they are not part of this delivery.
