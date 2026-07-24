# MOD-12 O3-B — Governed Offline Calibration and Holdout Evaluation Sandbox Report

## Decision summary

- Authorised baseline: `89f290cc950ed0385ee004c57ff9d0a3c07a8f85`
- Automatic result: `APPROVED`
- Human acceptance: `APPROVED` in the separate [O3-B Human Gate](STATE-06-MOD-12-O3B-Human-Gate-Report.md)
- Runtime boundary: exact test-only opt-in sandbox
- Input corpus: the nine approved synthetic O3-A members only
- Production representativeness: `FALSE`
- Result authority: `FALSE`
- MOD-12 activation: `ActivationState=None`
- Normal composition references: `0`
- Operational telemetry, corpus, provider, database or credential: `0`
- Training, UI, LLM, recommendation, command or automation: `0`
- Lifecycle transition: `NOT AUTHORISED`

O3-B proves a governed offline calibration and holdout protocol for the existing deterministic MOD-12 threshold
analyser. It does not train a model. Development and calibration members select only a bounded threshold direction
and midpoint for each provider-neutral segment. The resulting policy is content-addressed, dual-approved, attested
and frozen before the partition gate permits the first holdout read.

The automatic approval means that this synthetic protocol completed under its predeclared safety and measurement
criteria. It is not an assertion of production quality, provider support, operational prediction accuracy or fitness
to activate `OBSERVER`.

## Exact sandbox boundary

The exact marker is `o3b-governed-offline-evaluation-sandbox`. Only the existing consolidated test host recognises
it, and it requires a version-four local run identifier. O3-B types, keys, corpus access, policy freeze, evaluator and
process bridge exist only in test projects. Architecture tests prove that product source and normal composition name
none of them.

The separate-process proof emits only:

- a bounded result code;
- a digest of the synthetic run identifier;
- the aggregate segment count;
- `syntheticApproved=true`;
- `productionRepresentative=false`;
- `authorising=false`;
- `activationState=None`.

No case identifier, scenario, source-group value, corpus body, private key or provider-native value is emitted.

## Policy freeze and holdout separation

The partition gate exposes only `Development` and `Calibration` until an exact policy is frozen. Policy construction
cannot enumerate `Holdout`. The frozen policy binds:

- O3-A dataset identity and monotonic revision;
- exact corpus manifest, membership and criteria digests;
- independent development and calibration partition digests;
- one deterministic threshold rule for each of `availability`, `capacity` and `latency`;
- complete quantitative criteria for every required metric;
- freeze instant, `ProductionRepresentative=false` and `IsAuthorising=false`.

Two distinct synthetic policy-approver keys sign the canonical payload, and a third policy-attestation key signs the
same payload. The SHA-256 policy digest addresses that exact payload. The holdout may open only at a later instant,
only once and only for the exact corpus/policy pair. Reuse and rollback are quarantined before case evaluation.

The current synthetic fixture contains only one holdout member per segment. The thresholds and acceptance limits are
therefore test-protocol bounds, not calibrated production tolerances.

## Measurement contract and observed synthetic result

Each holdout member is mapped to one sanitised provider-neutral metric and evaluated twice through the existing
`DeterministicThresholdAnalyser`. The expected positive class is the O3-A synthetic `critical` label. The reported
prediction error is the mean absolute binary critical-signal error; it is not a time-to-failure forecast error.

| Segment | Coverage | Unknown/abstention | False positive | False negative | Prediction error | Stability | Explainability |
|---|---:|---:|---:|---:|---:|---:|---:|
| availability | 1.00 | 0.00 | 1 | 0 | 1.00 | 1.00 | 1.00 |
| capacity | 1.00 | 0.00 | 0 | 0 | 0.00 | 1.00 | 1.00 |
| latency | 1.00 | 0.00 | 0 | 0 | 0.00 | 1.00 | 1.00 |

Every segment also records one accounted duration unit, `4,096` accounted memory bytes and four deterministic work
units. These reproducible accounting values enforce the sandbox envelope; they are not host performance benchmarks.

The availability false positive is retained rather than hidden. Its predeclared synthetic criterion permits at most
one false positive and a maximum binary prediction error of `1.00` for the single-member segment. Consequently,
`SyntheticApproved=true` proves protocol completeness and bounded behaviour, not useful real-world precision. A real
quality gate would require a separately authorised representative corpus and materially stronger segment criteria.

## Fail-closed evidence

| Scenario | Observed result |
|---|---|
| policy altered after signature | rejected as invalid |
| wrong approval role or invalid authority | quarantined |
| corpus revision or membership binding divergence | rejected |
| holdout opened before freeze | refused |
| second holdout read or exact pair reuse | refused or quarantined |
| corpus rollback | quarantined |
| corpus withdrawal or expiry | rejected before holdout |
| cross-partition leakage or incomplete corpus | rejected by O3-A admission |
| missing result segment | rejected |
| altered or non-finite metric | rejected without publication |
| result attestation mismatch | quarantined |
| deadline already reached | rejected |
| cancellation | rejected without partial report |
| case, work or memory saturation | rejected without partial report |
| 96 deterministic policy mutations | all refused |

Result publication is all-or-nothing. An incomplete run has no publishable result package. Complete metrics are
content-addressed and signed with the exact policy-attestation key; a result cannot change its measurements,
synthetic approval or non-authorising declarations without invalidating its digest or attestation.

## Automated evidence

All recorded checks ran locally and offline without restore, download, dependency changes or external access.

| Check | Result |
|---|---|
| focused O3-B integration tests | `10/10 PASSED` |
| focused O3-B architecture tests | `4/4 PASSED` |
| integration-test Release build | `PASSED`, zero warnings and zero errors |
| architecture-test Release build | `PASSED`, zero warnings and zero errors |
| consolidated sandbox-host Release build | `PASSED`, zero warnings and zero errors |
| complete Release solution build | `PASSED`, zero warnings and zero errors |
| O3-B test-assembly source coverage | lines `93.91%` (`540/575`) |
| complete integration suite | `72/72 PASSED` |
| complete architecture suite | `71/71 PASSED` |
| proportional MOD-12 unit suite | `77/77 PASSED` |
| .NET format verification | `PASSED` |
| code-documentation gate | `350` files, `PASSED` |
| Markdown-link gate | `696` links in `155` files, `PASSED` |
| current worktree and available-history secret scan | `PASSED` |
| Node.js/npm baseline | Node.js `24.18.0`, npm `11.16.0`, `PASSED` |
| exact separate-process proof | `3` segments, synthetic approved, non-representative, non-authorising, `ActivationState=None` |

The final diff check passed. Cleanup found zero DB-Notifier process, listener or O3-B temporary root before staging.

## Preserved boundaries and cleanup

- O3-A content and all O3-B signing material remain in memory and are disposed after each test or process.
- No corpus, policy, result, cache, store or private key is persisted by the sandbox.
- Product source, normal composition, package declarations, dependencies and lockfiles are unchanged.
- `ObserverActivationState` still contains only `None`.
- No provider, database, browser, network endpoint, CI, push or deploy was used.
- The temporary coverage directory was removed after the aggregate was read.
- The final process, listener and temporary-root audit found zero residue.

## Limitations and next gate

The synthetic O3-A corpus is deliberately small and balanced. It does not model production prevalence, real
providers, versions, topology, workload drift, incident rarity or label uncertainty. Its single holdout member per
segment cannot establish operational false-positive or false-negative rates. The binary error measured here also
does not validate the separate OLS capacity-forecast path against future production outcomes.

The automatic O3-B result is `APPROVED` only for the authorised sandbox. Bruno subsequently decided exactly
`HUMAN GATE DO O3-B: APROVADO`. The separate
[O3-B Human Gate report](STATE-06-MOD-12-O3B-Human-Gate-Report.md) records that acceptance and its authority boundary.
The decision accepts this governed synthetic evaluation while retaining its false-positive and representativeness
limitations. Any representative or real corpus, normal composition, `OBSERVER`, UI, LLM, recommendation, command,
automation, push, deploy or lifecycle transition requires later and separate authority.
