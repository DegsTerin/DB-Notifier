# PF-OBS-1-D7 — FirstByte/Cold Repeatability Diagnostic Proposal

## Status and authority

Status: `PROPOSED — NOT AUTHORISED FOR IMPLEMENTATION OR EXECUTION`.

Bruno authorised only this documentary proposal after D6 stopped at the first
original V3 summary. This document does not authorise source or configuration
changes, builds, tests, physical runs, external observation, dependencies,
downloads, PostgreSQL, operational data or providers, Observer,
`ActivationState`, lifecycle progression, deployment, push or pull request.

D7 is a focused diagnostic proposal. It does not restart D6, execute the
HM-01–HM-03 campaign or change the accepted meaning of any historical result.

## Diagnostic question

D6 retained all `35` `FirstByte/Cold` samples but its original V3 summary
reported a repeatability coefficient of `0.22923232701994542` against the
unchanged inclusive limit of `0.20`. The run stopped before
`Cancellation/Cold`.

D7 asks only:

1. does the same summary failure recur across fresh, unobserved processes;
2. which of the five fixed sequential group medians accounts for each
   retained coefficient;
3. does a bounded external before/after observer materially change the
   recurrence pattern; and
4. are any coarse host-load observations consistently associated with the
   failed summaries?

D7 must distinguish recurrence, intermittence, observation sensitivity and
unresolved correlation. It must not claim scheduler, runtime, garbage
collector, hardware or host-load causality without a separately designed
causal experiment.

## Frozen references

- Proposal version:
  `pfobs1-d7-first-byte-cold-repeatability-diagnostic-1.0.0`.
- Proposal SHA-256:
  `7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF`.
- V3 dependency:
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
- D6 dependency:
  `5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845`.
- Lifecycle: `STATE-06 INTEGRATION`.
- MOD-12 activation: `ActivationState=None`.

Canonical statement:

```text
pfobs1-d7-first-byte-cold-repeatability-diagnostic|1.0.0|v3=60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A|d6=5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845|unobserved-runs=3|external-runs=2|external-gate=unobserved-failures>=2|precondition=v3-exact|scope=first-byte-cold-5-30-summary|repeatability=five-fixed-sequential-groups-six-samples-median-cv|coefficient-limit=0.20|samples=all|intraprocess-extra-captures=0|external=supervisor-before-after-os-counters-only|forced-gc=v3-two-existing-only|activation=none
```

Any implementation-authorisation request must reproduce this statement and
digest exactly. A change requires a new documentary proposal, version and
digest before execution.

## Preserved V3 contract

D7 must use the original V3 objects and algorithms without copying or
reimplementing their behaviour:

- unchanged V3 precondition and its two existing forced collections;
- exactly five warm-ups followed by thirty measured
  `FirstByte/Cold` samples;
- every sample retained in original order, with no selection or replacement;
- the original V3 `Summarise` operation;
- five fixed sequential groups of six measured samples;
- the median of each group calculated by the existing V3 implementation;
- coefficient of variation over those five medians;
- `FirstByteFixedWindowElapsed` as the repeatability basis; and
- unchanged inclusive maximum coefficient `0.20`.

D7 must not add, remove, move, repeat or condition a V3 collection, sample,
work unit, checkpoint, limit, summary rule or failure code.

## Proposed future implementation boundary

If separately authorised, implementation may add only:

- one marker-gated D7 runner inside the integration-test assembly;
- one exact D7 entrypoint in the existing consolidated sandbox host;
- typed, sanitised D7 evidence records and an atomic writer;
- focal tests for identity, exact scenario order, evidence completeness,
  stop rules and cleanup; and
- an architecture test proving zero D7 reference in normal `src/`
  composition.

The runner must select the first `35` unchanged V3 scenarios and call the
original summary once. It must not introduce instrumentation, callbacks or
branches inside the measured process or the V3 measurement boundary.

## Frozen diagnostic sequence

### D7-0 — Static contract gate

Before any physical run, a future authorised implementation must prove:

- the proposal, V3 and D6 digests;
- exact `5 + 30` scenario order and membership;
- original summary ownership and repeatability basis;
- exactly five fixed groups of six measured samples;
- inclusive coefficient limit `0.20`;
- zero D7 reference in product composition;
- zero additional forced collection; and
- atomic fail-closed evidence publication.

Any mismatch blocks D7 before physical execution.

### D7-U — Unobserved arm

Execute exactly three consecutive fresh processes. Each process performs:

1. the unchanged V3 precondition;
2. the exact `35` `FirstByte/Cold` samples;
3. the original V3 summary; and
4. atomic publication of all samples, the summary, failures and sanitised
   environment facts.

No D7 metric or process-state capture may occur inside these processes.
There are no replacement runs.

After publication, the supervising process may recompute the five group
medians and their coefficient from all retained
`RepeatabilityWindowMilliseconds` values. This is an integrity check only:
the recomputed result must equal the original V3 summary within exact
serialised double representation. Any disagreement is
`D7.CONTRACT_MISMATCH` and blocks interpretation.

### D7-U classification

Count only complete fresh-process reports:

| Failed original summaries | Classification | Next action |
|---:|---|---|
| `0/3` | `D7.NOT_REPRODUCED` | Stop; external arm prohibited |
| `1/3` | `D7.INTERMITTENT` | Stop; external arm prohibited |
| `2/3` | `D7.REPEATED` | External arm eligible |
| `3/3` | `D7.REPEATED_CONSECUTIVELY` | External arm eligible |

A failed summary means only the exact stable failure code
`o5r5d1.threshold.repeatability-coefficient` with
`FirstByte/Cold`, `RepeatabilityCoefficient`, `Ratio`, observed value greater
than `0.20`, null warm-up class and null repetition.

Any sample-level V3 failure, missing sample, missing summary, different
failure code or incomplete report blocks D7 without entering this table.

### D7-E — Conditional external arm

Only `D7.REPEATED` or `D7.REPEATED_CONSECUTIVELY` may release exactly two
additional fresh processes. They repeat the same unchanged `35` samples and
summary.

A separate supervisor may take one snapshot immediately before process launch
and one immediately after process exit. The allow-list is:

- system idle, kernel and user processor times;
- physical-memory load and available physical memory;
- measured-process user and kernel CPU time;
- measured-process cycle count;
- measured-process peak working set and page-fault count; and
- final process handle and thread counts when the operating system exposes
  them without polling.

Snapshots must use operating-system APIs already available on the host, with
no download or dependency. Unavailable counters are explicit `Unavailable`;
they are never replaced or inferred. Polling, tracing, ETW/EventPipe,
debuggers, profilers, priority or affinity changes, power-plan changes,
`EmptyWorkingSet` and forced collection are prohibited.

The external arm is classified descriptively:

| Failed original summaries | Classification |
|---:|---|
| `0/2` | `D7.EXTERNAL_NOT_REPRODUCED` |
| `1/2` | `D7.EXTERNAL_MIXED` |
| `2/2` | `D7.EXTERNAL_REPRODUCED` |

Differences between unobserved and external arms may establish observation
sensitivity as a hypothesis. Coarse before/after counters may establish
association only. Neither result attributes cause.

## Evidence contract

Each future report must retain:

- proposal, V3 and D6 versions and digests;
- `ActivationState=None`;
- arm and one-based run;
- start/completion UTC timestamps;
- expected and completed sample/summary counts;
- all unchanged V3 samples in original order;
- the original V3 summary and bounded failures;
- the five externally recomputed group medians and coefficient;
- sanitised operating-system, architecture, runtime, SDK, logical-processor,
  installed-memory and monotonic-clock facts; and
- external snapshots only for D7-E.

Evidence must not contain a machine, host, workstation, device or user name,
process identifier, local path, command line, payload, credential, endpoint
or provider topology. Every report is non-authorising.

## Stop rules

D7 stops and is `BLOCKED` when:

- a frozen digest, V3 scenario, sample, summary, limit or calculation differs;
- any process or report is incomplete;
- atomic publication or sanitisation fails;
- the integrity recomputation differs from the V3 summary;
- an unapproved in-process capture or external counter appears;
- a dependency, restore, download, privilege elevation or unavailable
  mandatory capability would be required;
- a DB-Notifier process, listener or temporary root remains after cleanup; or
- execution would require changing the host, priority, affinity, power plan,
  V3, product composition or an authorised boundary.

No failed or blocked run is silently replaced.

## Required validation and deliverables

A future execution increment must deliver:

1. isolated D7 implementation and architecture evidence;
2. applicable Release build and focal/full regression suites without restore;
3. at most three D7-U and, conditionally, two D7-E reports;
4. a factual report with classifications, retained hashes and limitations;
5. current-state and append-only history updates;
6. documentation, Markdown, secret, formatting and diff gates;
7. verified cleanup; and
8. one focused local commit, including blocked or failed factual outcomes.

The automatic D7 result is not a Human Gate and cannot approve PF-OBS-1, O5,
Observer activation or lifecycle progression.

## Explicitly excluded

- changes to V3, D6, workloads, sample membership, groups or limits;
- full D6, D5 or HM-01–HM-03 execution;
- in-process diagnostics, profiler, debugger, ETW or EventPipe;
- new dependency, restore, download or installation;
- PostgreSQL, provider, database, operational data or credential;
- normal runtime composition, Observer, recommendation or automation;
- `ActivationState` or lifecycle change;
- deployment, publication, push or pull request; and
- causal remediation or speculative correction.

## Future authority required

No implementation or execution may begin from this proposal alone. A future
authority, if desired, must be explicit and may use:

```text
AUTORIZO exclusivamente a implementação e execução do PF-OBS-1-D7 conforme a proposta versionada pfobs1-d7-first-byte-cold-repeatability-diagnostic-1.0.0 e SHA-256 7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF. Autorizo o gate estático, três processos novos não observados com o grupo FirstByte/Cold V3 exato e, somente se pelo menos dois repetirem a falha, dois processos novos adicionais com os snapshots externos before/after allow-listed. Autorizo implementação test-only isolada, testes, evidência sanitizada, cleanup, documentação e commit local. Permanecem proibidos alteração de V3/D6/thresholds, substituição de runs, instrumentação intraprocesso, polling, tracing, ETW/EventPipe, debugger/profiler, dependência/download, PostgreSQL, provider/dado operacional, campanha HM-01–HM-03, Observer, ActivationState, lifecycle, deploy, push e PR.
```
