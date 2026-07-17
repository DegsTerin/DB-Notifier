# STATE-06 MOD-12 Authenticated Provenance, Budget and Segmented Evaluation Report

## Status and authority

- Date: 2026-07-17
- Workspace lifecycle position: `STATE-06 INTEGRATION`
- MOD-12 mode status: no mode promoted; `none → OBSERVER` remains pending
- Authority: after reviewing the previous increment, Bruno authorised one further restricted `STATE-06` increment for authenticated `ObserverDataPolicy` provenance and revocation, processing budgets/backpressure and deterministic multi-segment offline evaluation
- Explicit exclusions: runtime provider integration, operational collection or persistence, external telemetry, external database, Observer services/workers/APIs, LLM, recommendation, planning, execution and lifecycle or mode promotion

This report records one local implementation increment. It does not authorise an operational Observer or change the lifecycle state.

## Outcome

The increment closes the three authorised local gaps while preserving the inactive, in-memory MOD-12 boundary:

- A signed `ObserverDataPolicyGrant` binds the complete immutable policy to a grant ID, issuer identity, grant-signing key and validity period.
- A separately signed `ObserverPolicyRevocationSnapshot` carries bounded revoked grant and grant-signing-key identifiers with an independent version and freshness period.
- Purpose-specific `ObserverPolicyTrustAnchor` instances keep grant and revocation trust separate. They contain only copied ECDSA P-256 public-key material; MOD-12 never receives or stores a private key. `ObserverPolicyTrustConfiguration` is supplied separately by the authorised application boundary, so untrusted signed evidence cannot bring its own trust anchor.
- The adapter accepts caller-supplied `ObserverDataPolicyVerificationContext` evidence and a separate application-configured trust configuration, verifies exact issuer/key identity, both ECDSA P-256/SHA-256 signatures, grant validity, current revocation evidence, grant/key revocation and exact data purpose, and then applies the existing policy and telemetry checks.
- Canonical length-prefixed signing payloads bind policy identity, independent schema/version, purpose, authorisation scope, period, synthetic-evidence flag and deterministically ordered exact sources. Revocation payloads bind their full identity, freshness and ordered revocation sets.
- Raw `ObserverDataPolicy` can no longer cross either adapter entry point without authenticated provenance and a current authenticated revocation view.

Cryptographic verification is local and deterministic for a supplied context. This increment deliberately does not create an issuer, key-management service, revocation distributor, trust-store persistence or runtime registration.

## Version separation

- `ObserverDataPolicy.PolicySchemaVersion` is `observer-data-policy.v1`, independent of `PolicyVersion`.
- `ObserverOfflineEvaluationDataset.DatasetSchemaVersion` is `observer-offline-dataset.v1`, independent of `DatasetVersion`.
- Revocation snapshots carry their own `SnapshotVersion`, independently from grant and policy versions.

These distinctions allow contract shape, governed content and revocation state to evolve without conflating their identities.

## Processing budget and backpressure

Every offline run now requires an explicit `ObserverProcessingBudget` with four independent ceilings:

- admitted case count;
- aggregate telemetry-envelope count;
- deterministic work units;
- elapsed local processing time, capped by the contract at ten minutes.

The deterministic reservation model is one case-admission unit, one adaptation unit per telemetry envelope and one analysis unit per case. Case, sample and worst-case work totals are calculated before processing. A workload that exceeds any admission ceiling returns an empty, non-passing backpressure report without reading case telemetry. Elapsed-time exhaustion is checked before each atomic unit and returns only fully completed cases, exact consumed work and a non-passing incomplete report. Cancellation remains independent and throws before further work.

This is local admission control, not a queue or asynchronous runtime. It introduces no retry, worker, API, persistence, memory buffer beyond existing bounded collections or implicit partial acceptance.

## Governed multi-segment corpus

The deterministic synthetic reference corpus now contains nine exact cases across three deliberately non-homologating fixture segments:

| Fixture segment | Platform | Reference cases | Adversarial case |
|---|---|---:|---|
| `fixture-relational` / `1.0.0` | `offline-windows` | detected; not detected | future telemetry |
| `fixture-document` / `2.0.0` | `offline-linux` | detected; not detected | disabled opt-in |
| `fixture-keyvalue` / `3.0.0` | `offline-container` | detected; not detected | cross-instance evidence |

Observed corpus metrics were nine exact passes, three true positives, three true negatives, zero false positives, zero false negatives, precision `1.0`, recall `1.0`, three rejected adversarial cases and zero accepted adversarial cases. All three segment summaries passed exactly. These labels and results are offline fixtures only; they do not claim runtime provider integration, calibration, homologation or public support.

Additional focused cases verify untrusted issuer identity, tampered grant signature, tampered revocation signature, revoked grant, revoked grant-signing key, stale revocation evidence and case/sample/work/time backpressure.

## Security and architecture boundaries

- Identity, authorisation assertion, cryptographic integrity and revocation are represented and evaluated separately.
- Trust is exact and fail-closed: no unknown issuer, alternate key, stale list, invalid signature or missing current revocation view has a fallback path.
- Signatures and public keys are defensively copied. Test private keys are generated ephemerally in memory and disposed; none is committed, logged, persisted or placed in evidence.
- Public policy and dataset schema versions are distinct from content versions.
- No new dependency, project reference, DI registration, provider reference, runtime service, network call, file operation or persistence path was added.
- Only the existing provider-neutral `HealthObservation` remains an outer DB-Notifier type on the public AIOps surface.
- Collection, persistence and Observer activation remain immutable `false` policy properties.
- No LLM, recommendation, plan, command or executor surface exists in this increment.

## Verification

Environment: Windows, repository SDK .NET `10.0.301`, Release configuration, 2026-07-17.

| Check | Observed result |
|---|---|
| Focused AIOps tests | `65/65` passed, including `31` adapter/policy/offline integration cases |
| Full unit/model/provider/presentation suite | `261/261` passed |
| Architecture suite | `15/15` passed |
| Full solution build | passed with zero warnings and zero errors |
| Coverage gate | passed: `80.39%` lines and `60.80%` branches; floors `70%`/`45%` |
| .NET format/analyser gate | passed with no required changes |
| Code-documentation gate | passed for `208` comment-capable source files, followed by human en-GB/API review |
| Secret scan | passed for the current non-ignored worktree and available Git history |
| Fail-closed runtime smoke | passed: liveness `200`, all protected HTTP endpoints `426`, Agent workers disabled and local persistence absent |
| Diff and Markdown links | passed after final factual synchronisation |

One initial Release build attempt encountered a WPF intermediate-output file lock held by project-local compiler helpers left by a timed-out validation command. Fifteen helpers whose executable paths were under the repository `.dotnet` directory were identified and stopped; Visual Studio Code and its build host were preserved. The clean non-incremental Release rebuild then passed with zero warnings and zero errors.

Online NuGet/npm vulnerability audits were not repeated because this authority excludes external actions and no dependency changed. Dashboard, WPF visual, provider-runtime and external integration suites are not evidence for this isolated Application increment.

## Gate classification

- Local implementation Quality Gate: `APPROVED` for this isolated provenance, revocation, budget and offline-corpus scope.
- `none → OBSERVER`: `PENDING`; it was expressly excluded and receives no implicit authority.
- Later MOD-12 modes: `NOT AUTHORISED`.
- Lifecycle position: remains `STATE-06 INTEGRATION`.

## Human review of the increment

On 2026-07-17, Bruno accepted this restricted MOD-12 increment after reviewing the presented commit summary and detailed report. The review found the report technically consistent, the declared scope preserved and no indication of lifecycle progression or operational activation in the material presented.

The reviewer explicitly reserved that this assessment was based on the presented documentation rather than direct inspection of the commit diff or source code, and therefore does not replace a code review where one is required.

The recorded decision was: `Incremento restrito MOD-12 aceito, sem promoção para OBSERVER.` The acceptance grants no further implementation authority. Any new increment, runtime integration or `none → OBSERVER` proposal continues to require separate authorisation, architecture and security review, a specific Quality Gate and a dedicated Human Gate.

## Remaining conditions

- Define and review the future server-side issuance, protected trust-anchor configuration, key rotation and authenticated revocation distribution boundary before any runtime use.
- Expand the corpus beyond nine synthetic cases with governed distributions, stale/missing/conflicting evidence, broader poisoning/replay scenarios, load envelopes and calibrated false-positive/negative behaviour.
- Establish memory and concurrency evidence for any proposed runtime topology; this increment proves only bounded synchronous offline admission and cancellation.
- Complete a dedicated security review, automatic Quality Gate and explicit Human Gate before any `none → OBSERVER` proposal.

The completion of this increment is not a request for promotion and cannot be used as automatic evidence of an operational Observer.
