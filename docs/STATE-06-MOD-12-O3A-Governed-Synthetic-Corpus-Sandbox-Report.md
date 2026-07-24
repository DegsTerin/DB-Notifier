# MOD-12 O3-A — Governed Synthetic Corpus Sandbox Report

## Decision summary

- Authorised baseline: `4ca1d42ee03a1c36d8e2a85ef68062902794fcb9`
- Automatic result: `APPROVED`
- Human acceptance: `APPROVED` in the separate [O3-A Human Gate](STATE-06-MOD-12-O3A-Human-Gate-Report.md)
- Runtime boundary: exact test-only opt-in sandbox
- Corpus: deterministic, local, synthetic, non-personal, non-secret and in-memory
- Production representativeness: `FALSE`
- MOD-12 activation: `ActivationState=None`
- Normal composition references: `0`
- Operational telemetry, corpus, provider, database or credential: `0`
- Training, UI, LLM, recommendation, command or automation: `0`
- Lifecycle transition: `NOT AUTHORISED`

O3-A implements the governance and admission contract for a synthetic corpus. It proves that authority, provenance,
purpose, retention, withdrawal, exact membership, immutable partitions and predeclared quantitative criteria can be
checked fail closed. It does not prove that the fixture represents production, authorise a real corpus, train a model,
activate `OBSERVER` or make any operational support claim.

## Exact sandbox boundary

The exact process marker is `o3a-governed-corpus-sandbox`. It is recognised only by the existing consolidated test
host and accepts only a version-four synthetic run identifier. Product source and normal composition contain no O3-A
marker, verifier, authority or process bridge.

The fixture exists only in the integration-test assembly. Its three P-256 private keys are generated independently
for the data-owner, data-governance and corpus-attestation roles, remain in memory and are disposed after each run.
The process output contains only a stable result code, a digest of the run identifier, aggregate case and partition
counts, `productionRepresentative=false` and `activationState=None`.

## Governed manifest and content addressing

The canonical signed manifest binds:

- dataset identity, positive monotonic revision and predecessor digest;
- synthetic tenant, `test-only` environment and `observer-evaluation-only` purpose;
- validity window and irreversible withdrawal state;
- exact sorted membership digest;
- each case identifier, partition, segment and SHA-256 content digest;
- three predeclared quantitative segment criteria;
- authority, project-owned synthetic licence, classification, origin, retention and withdrawal policy;
- consent and redaction dispositions appropriate to synthetic data;
- explicit known biases and known gaps;
- the immutable declaration `ProductionRepresentative=false`.

Two exact approvals are required from distinct data-owner and data-governance keys. A third materially distinct corpus
attestation key authenticates the same canonical manifest. A signature proves authority and integrity only; it does
not prove label correctness, production prevalence or operational fitness.

## Partitions, segments and quantitative criteria

The fixture contains nine exact members:

- three immutable and disjoint partitions: `Development`, `Calibration` and `Holdout`;
- three provider-neutral segments: `capacity`, `availability` and `latency`;
- exactly one member per segment in each partition;
- predeclared outcomes `healthy`, `warning` and `critical`;
- predeclared load classes `low`, `nominal` and `high`;
- predeclared quality classes `complete`, `noisy` and `adversarial`;
- zero permitted missing members under the current synthetic criterion.

Case identifiers, content digests, evidence fingerprints and source-group identifiers must all be unique. This blocks
direct duplicates and cross-partition leakage. The verifier also rejects conflicting outcomes for one scenario,
unapproved transformations, non-finite or out-of-range values, absent segments, count drift and incomplete manifests.

These balanced synthetic counts are test construction, not an estimate of production distribution. The manifest
states that synthetic distributions do not model production prevalence and that no production topology, provider or
workload evidence is present.

## Continuity and withdrawal

Revision one is accepted only without a predecessor. Later revisions must be direct successors and name the exact
accepted manifest digest. Lower revisions, gaps, divergent predecessors and dataset substitution enter quarantine.
A head also freezes the exact membership and quantitative-criteria digests, so even a newly signed successor cannot
move cases between partitions or redefine its acceptance criteria.
A withdrawn head cannot return to an active state within the dataset series. No test can silently replace, omit or add
membership because both the aggregate membership digest and each content digest are verified.

## Fail-closed evidence

| Scenario | Observed result |
|---|---|
| manifest/content mismatch | quarantined as invalid membership |
| self-consistent content change without matching manifest authority | quarantined as unproved authority |
| duplicate case identifier | quarantined as invalid membership |
| duplicate evidence fingerprint or source group | rejected as partition leakage |
| missing value above the declared limit | rejected as unproved quality |
| conflicting scenario outcomes | rejected as unproved quality |
| out-of-range value or unapproved transformation | rejected as poisoning/quality failure |
| missing segment criterion | rejected as invalid manifest |
| crossed role or invalid signature | quarantined as unproved authority |
| expired scope | rejected |
| production-representativeness claim | rejected as invalid manifest |
| rollback, gap, divergent predecessor or partition drift | quarantined |
| reactivation after withdrawal | quarantined as irreversible withdrawal |
| 96 deterministic property-style mutations | all refused; no exception and no corpus content in diagnostics |

## Automated evidence

All checks ran locally and offline, without restore, download, dependency changes or external access.

| Check | Result |
|---|---|
| focused O3-A integration tests | `9/9 PASSED` |
| focused O3-A architecture tests | `4/4 PASSED` |
| complete integration suite | `62/62 PASSED` |
| complete architecture suite | `67/67 PASSED` |
| proportional unit/coverage suite | `399/399 PASSED` |
| coverage gate | lines `81.98%`, branches `53.85%`, `10/10` required components present |
| Release solution build | `PASSED`, zero warnings and zero errors |
| exact separate-process proof | `9` cases, `3` partitions, `productionRepresentative=false`, `ActivationState=None` |

The final format, documentation, Markdown-link, secret, diff and cleanup gates are recorded in the focused commit
handoff after this report is validated.

## Cleanup and preserved boundaries

- Corpus content and all private key material remain in memory and are disposed at the end of each test or process.
- No corpus file, store, cache, provider artefact, credential, personal data or operational telemetry is created.
- No product source, normal composition, package declaration, lockfile, configuration, schema, migration or UI changed.
- `ObserverActivationState` still declares only `None`.
- No provider, database, browser, network endpoint, CI, push or deploy was used.
- The final process, listener, window and project-owned temporary-root checks remain part of the final evidence pass.

## Limitations and next gate

O3-A proves only a governance mechanism against deterministic synthetic fixtures. It cannot establish production
representativeness, label validity under real workloads, provider/version/topology coverage, real bias prevalence or
an empirical operational resource envelope. No real corpus is authorised.

The automatic O3-A result is `APPROVED`. Bruno subsequently decided exactly
`HUMAN GATE DO O3-A: APROVADO`. The separate
[O3-A Human Gate report](STATE-06-MOD-12-O3A-Human-Gate-Report.md) records that acceptance and its authority boundary.
The decision accepted only this synthetic sandbox and its stated limitations. O3-B, any real corpus, normal
composition, model training, LLM, recommendation, command, automation, `OBSERVER` activation and lifecycle transition
remain unauthorised.
