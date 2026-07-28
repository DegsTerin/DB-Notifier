# PF-OBS-1-D7-R1 — Summary-Count Contract Rerun Proposal

## Status and authority

Status:
`PROPOSED — DOCUMENTATION COMPLETE — NOT AUTHORISED FOR IMPLEMENTATION OR EXECUTION`.

The current authority permits only this documentary proposal. It does not
authorise source or configuration changes, builds, tests, physical processes,
external observation, dependency activity, downloads, PostgreSQL, operational
data or providers, Observer, `ActivationState`, lifecycle progression,
deployment, push or pull request.

D7-R1 is a new corrective evidence lot. It does not reopen, repair, rewrite,
replace or reclassify the three blocked D7 reports.

## Corrective question

D7 executed three complete V3 `FirstByte/Cold` groups, but each published JSON
omitted the explicit `expectedSummaryCount` and `completedSummaryCount` fields
required by its frozen evidence contract. The original summary objects and
all `35/35` samples remain retained, but the reports are contract-incomplete
and D7 stopped before classification.

D7-R1 asks only:

1. can a new independently identified lot prove the corrected summary-count
   schema before physical execution;
2. can exactly three fresh unobserved processes publish complete R1 envelopes
   containing the unchanged D7/V3 measurements and both explicit counts; and
3. if at least two valid R1 unobserved summaries reproduce the exact failure,
   does the already bounded external observer change the recurrence pattern?

D7-R1 does not investigate or attribute the cause of D6. It does not resume
D6, execute D5 controls or execute the HM-01–HM-03 campaign.

## Frozen identity and dependencies

- Proposal version:
  `pfobs1-d7-r1-summary-count-contract-rerun-1.0.0`.
- Proposal SHA-256:
  `4FD5E92E4674BF93DF102DCFC564614E7D417C400292324A76E45011AE39F5C5`.
- Predecessor D7 SHA-256:
  `7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF`.
- Unchanged V3 SHA-256:
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
- Referenced D6 SHA-256:
  `5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845`.
- Corrected implementation baseline:
  `558658160282f35c60f7e4e8fa09c672a2115e6b`.
- Lifecycle: `STATE-06 INTEGRATION`.
- MOD-12 activation: `ActivationState=None`.

Canonical statement:

```text
pfobs1-d7-r1-summary-count-contract-rerun|1.0.0|d7=7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF|v3=60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A|d6=5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845|blocked-execution=0939b6560d2c4556ad781be9d9e3b376|blocked-report-sha256=AB48D99C1688EB3FC163DBAD59D4E393739B04BF1C82AB59C9239A5B180E2C85,0087DFAA4E7E014A7C037E1887C879A4A9C5501F1228280962854ED62224066F,7E167AFEBBC01F8DF4551A12CFD6036F2CD2156A0D92EF73E3DD80C9212D00C2|blocked-evidence=preserve-byte-for-byte-exclude-from-classification|scope=first-byte-cold-5-30-summary|summary-count-schema=expected-1-completed-1-iff-summary-retained|schema-gate=typed-roundtrip-exact-json-fields-values-validator|unobserved-runs=3|external-runs=2|external-gate=valid-r1-unobserved-failures>=2|historical-runs=replaced-0|intraprocess-extra-captures=0|external=supervisor-before-after-os-counters-only|v3-threshold=0.20|forced-gc=v3-two-existing-only|activation=none
```

Any change to this statement requires a new version and digest before
implementation or execution.

## Immutable blocked evidence

The three D7 reports remain historical blocked evidence. D7-R1 must verify
their byte counts and SHA-256 values before and after any future activity:

| Historical report | Bytes | SHA-256 |
|---|---:|---|
| D7 unobserved run 1 | `31,467` | `AB48D99C1688EB3FC163DBAD59D4E393739B04BF1C82AB59C9239A5B180E2C85` |
| D7 unobserved run 2 | `31,430` | `0087DFAA4E7E014A7C037E1887C879A4A9C5501F1228280962854ED62224066F` |
| D7 unobserved run 3 | `31,473` | `7E167AFEBBC01F8DF4551A12CFD6036F2CD2156A0D92EF73E3DD80C9212D00C2` |

They must not be edited, renamed, regenerated, copied into an R1 result,
counted towards R1 classification or described as replacement runs. If any
file is absent or any byte count or hash differs, D7-R1 stops as
`D7-R1.BLOCKED_PREDECESSOR_EVIDENCE`.

## Preserved measurement contract

D7-R1 inherits the unchanged D7/V3 measurement semantics:

- the original V3 precondition and its two existing forced collections;
- exactly five warm-ups followed by thirty measured
  `FirstByte/Cold` samples;
- the original V3 runner, samples, summary, threshold and failure codes;
- five fixed sequential groups of six measured samples;
- the original coefficient over the five group medians;
- `FirstByteFixedWindowElapsed` as the repeatability basis;
- the unchanged inclusive coefficient limit `0.20`; and
- exact binary agreement with the independent integrity recomputation.

D7-R1 must not change, copy, reimplement or condition V3, D6 or the original
D7 measured algorithm. It adds only a distinct corrective identity and
evidence envelope around the reused test-only measurement result.

## Proposed future implementation boundary

A separately authorised implementation may add only:

- a typed `D7-R1` protocol identity and non-authorising evidence envelope;
- exact marker-gated R1 measured and supervisor entrypoints in the existing
  test-only sandbox boundary;
- a strict R1 temporary root and atomic writer;
- a synthetic typed round-trip regression for the complete and incomplete
  summary-count shapes;
- focal tests for predecessor preservation, R1 identity, count values,
  serialised field names, deserialisation, validation, run bounds and cleanup;
  and
- an architecture regression proving that V3, D6, the original D7 measured
  algorithm and normal `src/` composition remain unchanged.

The R1 envelope must contain:

- R1 version and digest;
- predecessor D7, V3 and D6 digests;
- `ActivationState=None`;
- arm and one-based R1 run;
- a nested original D7 measured result;
- `expectedSampleCount=35`;
- the exact completed sample count;
- `expectedSummaryCount=1`;
- `completedSummaryCount=1` only when the original summary is retained,
  otherwise `0`;
- the original summary and bounded failures;
- the independent five-median integrity result;
- external evidence only for the conditional R1-E arm; and
- a permanently false authorising flag.

The implementation must not mutate the predecessor D7 reports or their
retention directory. No normal product project may reference an R1 type,
marker or composition path.

## Frozen corrective sequence

### R1-0 — Predecessor and source gate

Before implementation:

1. prove the proposal, D7, V3 and D6 digests;
2. prove all three blocked D7 byte counts and hashes;
3. prove a clean or safely isolatable worktree;
4. prove zero DB-Notifier runtime or listener; and
5. identify the exact allowed test-only files before editing.

Any mismatch blocks the increment without source modification.

### R1-1 — Static schema gate

Before any physical process, the future implementation must pass all of the
following without restore or download:

1. compile the isolated test-only implementation;
2. serialise a complete typed R1 envelope and prove the exact camel-case JSON
   properties `expectedSummaryCount` and `completedSummaryCount`;
3. prove both values are numeric `1` for a complete retained summary;
4. serialise an incomplete synthetic shape and prove
   `expectedSummaryCount=1`, `completedSummaryCount=0` and no false complete
   disposition;
5. deserialise both shapes through the production R1 evidence reader;
6. reject missing, duplicate, string-valued, negative, excessive or
   inconsistent count fields;
7. prove the supervisor validator independently enforces the nested summary
   presence and exact count values;
8. prove exact `5 + 30` V3 membership, one original summary and binary
   integrity agreement;
9. prove zero R1 reference in normal `src/` composition and zero additional
   in-process capture or forced collection;
10. prove strict output paths, atomic publication and fail-closed cleanup; and
11. reverify the predecessor D7 byte counts and hashes.

A reflection-only property check is insufficient. The gate must inspect the
actual serialised UTF-8 JSON and its typed round trip. Any failure blocks R1
before physical execution; it does not authorise a repair in the same
execution increment unless that repair was explicitly included in the
authority.

### R1-U — New unobserved lot

Only after R1-1 passes, execute exactly three consecutive fresh unobserved
processes. Each process performs:

1. the unchanged V3 precondition;
2. exactly `35` `FirstByte/Cold` scenarios in original order;
3. the original V3 summary exactly once;
4. independent external integrity recomputation after publication; and
5. atomic publication of one complete R1 envelope.

No R1 metric or process-state capture may occur inside the measured process.
The three historical D7 processes are not counted. No failed, incomplete or
interrupted R1 process is replaced, and no later R1-U process begins after an
invalid report.

Before launching the first R1-U process, record a sanitised environment
declaration and the exact sandbox-host binary SHA-256. All three processes
must use that same binary.

### R1-U admissibility

An R1-U report is admissible only when:

- every frozen identity and digest is exact;
- `expectedSampleCount=35` and `completedSampleCount=35`;
- `expectedSummaryCount=1` and `completedSummaryCount=1`;
- the original summary is present exactly once;
- all samples and the summary pass the original structural validation;
- the independent coefficient matches the original summary exactly;
- no external observation is present; and
- publication, sanitisation and cleanup are complete.

Any non-admissible report blocks R1 without classification.

### R1-U classification

Count only the three new admissible R1-U reports:

| Failed original summaries | Classification | Next action |
|---:|---|---|
| `0/3` | `D7-R1.NOT_REPRODUCED` | Stop; external arm prohibited |
| `1/3` | `D7-R1.INTERMITTENT` | Stop; external arm prohibited |
| `2/3` | `D7-R1.REPEATED` | External arm eligible |
| `3/3` | `D7-R1.REPEATED_CONSECUTIVELY` | External arm eligible |

A failed summary retains the exact D7 definition:
`o5r5d1.threshold.repeatability-coefficient`, `FirstByte/Cold`,
`RepeatabilityCoefficient`, `Ratio`, observed value greater than `0.20`, null
warm-up class and null repetition.

The R1 result does not retroactively assign a classification to D7.

### R1-E — Conditional external lot

Only `D7-R1.REPEATED` or `D7-R1.REPEATED_CONSECUTIVELY` may release exactly
two additional fresh processes. They use the same unchanged `35` scenarios,
summary, R1 envelope and count validation.

The external observer is unchanged from D7 and remains supervisor-only:

- one system snapshot immediately before launch;
- one system snapshot immediately after exit;
- final measured-process counters from the retained process handle;
- only system idle/kernel/user times, physical-memory load and availability,
  process user/kernel CPU, cycle count, peak working set, page faults, handle
  count and thread count; and
- explicit typed unavailable values without inference.

Polling, tracing, ETW/EventPipe, debugger, profiler, priority, affinity,
power-plan changes, `EmptyWorkingSet` and forced collection remain prohibited.

| Failed original summaries | Classification |
|---:|---|
| `0/2` | `D7-R1.EXTERNAL_NOT_REPRODUCED` |
| `1/2` | `D7-R1.EXTERNAL_MIXED` |
| `2/2` | `D7-R1.EXTERNAL_REPRODUCED` |

External observations remain descriptive. They may support an association or
observation-sensitivity hypothesis, never causal attribution.

## Evidence and retention contract

Each R1 report must:

- use a new execution identifier and the strict R1 temporary root;
- contain no machine, host, workstation, device or user name, process
  identifier, local path, command line, payload, credential, endpoint or
  provider topology;
- retain all original samples, summary, failures, counts and integrity values;
- publish once through a write-through temporary sibling and atomic move;
- receive a byte count and SHA-256 before retention;
- be retained separately from the blocked D7 evidence; and
- remain non-authorising.

The factual R1 report must list every attempted process. An incomplete or
failed process is evidence and must not disappear from the sequence.

Cleanup must remove only the exact validated R1 temporary root after retained
hash equivalence is proved. It must leave the three historical D7 reports
unchanged and finish with zero project-owned process, listener, temporary
sibling or R1 temporary root.

## Stop rules

D7-R1 stops as blocked when:

- any frozen proposal or dependency digest differs;
- any predecessor D7 report is absent or its bytes or hash differ;
- the serialised schema gate is incomplete or fails;
- a sample, summary, count, failure, integrity value or run identity differs;
- a process or report is incomplete;
- an unauthorised in-process capture or external counter appears;
- a historical or failed process would need to be replaced;
- restore, download, new dependency, privilege elevation or an unavailable
  mandatory capability would be required;
- sanitisation, atomic publication, retention or cleanup fails; or
- execution would require changing V3, D6, D7 history, thresholds, host
  settings, product composition or an authorised boundary.

## Required future validation and deliverables

A future authorised execution must deliver:

1. the isolated R1 identity, envelope, writer, supervisor and regressions;
2. the complete R1-0 and R1-1 static evidence;
3. exactly three attempted R1-U reports and, only conditionally, two R1-E
   reports;
4. before/after predecessor D7 byte and hash verification;
5. applicable Release build and focal/full regression suites without restore;
6. a factual R1 report with every attempted process, classification or
   blocking cause, hashes, limitations and causal non-claim;
7. current-state and append-only history updates;
8. formatting, documentation, Markdown, secret and diff gates;
9. verified process, listener and temporary-root cleanup; and
10. one focused local commit, including a blocked or failed outcome.

The automatic R1 result is not a Human Gate. It cannot approve PF-OBS-1, O5,
Observer activation or lifecycle progression.

## Explicitly excluded

- repair, rewrite, renaming, deletion or replacement of the three D7 reports;
- modification of V3, D6, the original D7 measured algorithm or thresholds;
- D6, D5 controls or the HM-01–HM-03 campaign;
- intraprocess instrumentation, polling, tracing, ETW/EventPipe,
  debugger/profiler, priority, affinity or host-setting changes;
- dependency, restore, download or installation;
- PostgreSQL, provider, database, operational data or credential;
- normal runtime composition, Observer, recommendation or automation;
- `ActivationState` or lifecycle change;
- deployment, publication, push or pull request; and
- causal remediation or speculative correction.

## Future authority required

No implementation, build, test or execution may begin from this proposal
alone. A future authority, if desired, must reproduce the version and digest
exactly and may use:

```text
AUTORIZO exclusivamente a implementação e execução do PF-OBS-1-D7-R1 conforme a proposta versionada pfobs1-d7-r1-summary-count-contract-rerun-1.0.0 e SHA-256 4FD5E92E4674BF93DF102DCFC564614E7D417C400292324A76E45011AE39F5C5. Autorizo a implementação test-only isolada da identidade, envelope, writer, supervisor e regressões R1; os gates R1-0 e R1-1; e, somente após ambos passarem, exatamente três processos novos R1-U com o grupo FirstByte/Cold V3 exato. Somente se os três relatórios forem admissíveis e pelo menos dois repetirem a falha exata, autorizo exatamente dois processos adicionais R1-E com os snapshots externos allow-listed. Autorizo evidência sanitizada, retenção separada, verificação before/after dos três hashes D7, cleanup, documentação e commit local. Permanecem proibidos reparar, reescrever, renomear, excluir ou substituir evidência D7; alterar V3, D6, o algoritmo medido D7 ou thresholds; instrumentação intraprocesso; polling, tracing, ETW/EventPipe, debugger/profiler; dependência, restore ou download; PostgreSQL, provider/dado operacional; D6, controles D5 ou campanha HM-01–HM-03; Observer, ActivationState, lifecycle, deploy, push e PR.
```
