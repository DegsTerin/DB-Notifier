# PF-OBS-1-D8 — Exact-Prefix Completion Reconciliation Proposal

## Status and authority

Status:
`PROPOSED — DOCUMENTATION COMPLETE — NOT AUTHORISED FOR IMPLEMENTATION OR EXECUTION`.

The current authority permits only this documentary reconciliation and
proposal. It does not authorise source or configuration changes, builds,
tests, physical processes, external observation, dependency activity,
downloads, PostgreSQL, providers or operational data, Observer,
`ActivationState`, lifecycle progression, deployment, push or pull request.

D8 does not reopen, repair, replace or reclassify D5, D6, D7 or D7-R1. It
defines one bounded future question after reconciling their different methods
and dispositions.

## Reconciled evidence

| Lot | Admissible result | What it proves | What it does not prove |
|---|---|---|---|
| D5 | `BLOCKED`; Human Gate `ACCEPTED AS BLOCKED` | Eight fresh processes retained `704/704` samples; neither exact-prefix run reproduced the historical `Cancellation/Cold` working-set excess and no control exceeded `176,128 bytes` | The additional in-process process-history captures prevent D5 from serving as a non-intrusive post-format rebaseline; their perturbation was not itself proved, and no cause was attributed |
| D6 | `BLOCKED` | The corrected writer published one valid fresh-process report; `FirstByte/Cold` stopped at `35/158` samples and `1/4` summaries with coefficient `0.22923232701994542` against `0.20` | `Cancellation/Cold` was not reached, so the historical working-set excess was neither reproduced nor refuted |
| D7 | `BLOCKED` | Three fresh processes physically retained the exact `FirstByte/Cold` group and observed coefficients below `0.025` | The reports omitted two mandatory summary-count fields, so `0/3` could not enter the classification table and cannot reclassify D6 |
| D7-R1 | `D7-R1.NOT_REPRODUCED`; Human Gate `APPROVED WITH RESERVATIONS` | Three new admissible processes retained `35/35` samples and `1/1` summary with `0/3` exact failures and coefficients below `0.022` | The lot does not identify the cause of D6, repair D7 or reach `Cancellation/Cold` |

Across D7 and D7-R1, six later fresh processes physically retained the
complete `FirstByte/Cold` group without exceeding `0.20`; only the three R1
reports are contractually admissible for classification. This makes the D6
summary failure non-recurrent in the valid R1 lot, not disproved, repaired or
causally explained.

The unresolved question is narrower than it was before D7: no uninstrumented
post-format process has yet completed the unchanged V3 prefix through
`Cancellation/Cold` measured repetition 13. D5 reached that position with
additional process-history captures; D6 removed those captures but stopped at
the first summary.

## Diagnostic question

D8 asks only:

> Can exactly two fresh, unobserved processes pass the four unchanged V3
> summary groups after the valid D7-R1 non-recurrence result, enter
> `Cancellation/Cold` and produce an admissible outcome through measured
> repetition 13; and, for those two target-eligible outcomes, does the
> unchanged working-set excess recur?

D8 does not investigate the cause of an early V3 failure or a working-set
excess. It does not execute an external observer, repeat the D5 controls,
resume the complete HM-01–HM-03 campaign or select favourable runs.

## Frozen identity and dependencies

- Proposal version:
  `pfobs1-d8-exact-prefix-completion-reconciliation-1.0.0`.
- Proposal SHA-256:
  `208DA70A8D638E50E2951DECDA83414B9E1F7CF4AFF957D09C1EAA2A8B1B8814`.
- Unchanged V3 SHA-256:
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
- D5 protocol SHA-256:
  `82606BF31085214607C8CBE401C0F4050D9465523B9B01F89FFE45731012FFDA`.
- D6 protocol SHA-256:
  `5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845`.
- D7 proposal SHA-256:
  `7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF`.
- D7-R1 proposal SHA-256:
  `4FD5E92E4674BF93DF102DCFC564614E7D417C400292324A76E45011AE39F5C5`.
- Documentary baseline: branch `main`, commit
  `1b6066893296e5e2bd4e37b34bfba4472eed6ede`.
- Lifecycle: `STATE-06 INTEGRATION`.
- MOD-12 activation: `ActivationState=None`.

Canonical statement:

```text
pfobs1-d8-exact-prefix-completion-reconciliation|1.0.0|v3=60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A|d5=82606BF31085214607C8CBE401C0F4050D9465523B9B01F89FFE45731012FFDA|d5-report=2CC5AD651A40AAC82CC6EFBB02456F9F1CEC99C4E4153E861E14447EBA2A5418|d5-human=334C91A9E2D733478039AFCCCC263823AEAF9507174C7C44C112F94C2B7D4A34|d6=5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845|d6-report=9FD674AA32F47FB5BCBC37CC90616CAE066BE3299C5278DDDF6D3A50465AAEDD|d7=7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF|d7-report=B916B42409210E269241CE6187053E4411DE57502A7DD06B934455F46F428D2A|d7-r1=4FD5E92E4674BF93DF102DCFC564614E7D417C400292324A76E45011AE39F5C5|d7-r1-report=4686BCA6C94BE60708BE439F697060EED56523AED81CED26C1B8C84705A0F820|d7-r1-human=D0FE791D85136CFE44776465DEC093122C909F5C7E07A957A23F5E1504A5C1D9|runs=2|arm=unobserved|precondition=v3-exact|prefix=first-byte-cold-5-30-summary,first-byte-warm-5-30-summary,idle-cold-5-30-summary,idle-warm-5-30-summary,cancellation-cold-5-13|expected-samples=158|expected-summaries=4|target-stop=cancellation-cold-working-set-excess-through-measured-13|target-eligible=complete-prefix-or-exact-target-stop|valid-early-stop-run2=continue|invalid-report-run2=prohibited|working-set-limit=786432|repeatability-limit=0.20|intraprocess-extra-captures=0|external=0|d5-controls=0|historical-runs-replaced=0|forced-gc=v3-two-existing-only|activation=none
```

Any change to this statement requires a new version and digest before
implementation or execution.

## Frozen documentary evidence

The reconciliation depends on these owner documents exactly as inspected
before this proposal:

| Evidence | Bytes | SHA-256 |
|---|---:|---|
| [D5 report](STATE-06-MOD-12-PF-OBS-1-D5-Process-History-Diagnostic-Report.md) | `7,381` | `2CC5AD651A40AAC82CC6EFBB02456F9F1CEC99C4E4153E861E14447EBA2A5418` |
| [D5 Human Gate](STATE-06-MOD-12-PF-OBS-1-D5-Human-Gate-Report.md) | `2,455` | `334C91A9E2D733478039AFCCCC263823AEAF9507174C7C44C112F94C2B7D4A34` |
| [D6 report](STATE-06-MOD-12-PF-OBS-1-D6-Post-Format-Non-Intrusive-Rebaseline-Report.md) | `10,067` | `9FD674AA32F47FB5BCBC37CC90616CAE066BE3299C5278DDDF6D3A50465AAEDD` |
| [D7 report](STATE-06-MOD-12-PF-OBS-1-D7-FirstByte-Cold-Repeatability-Diagnostic-Report.md) | `9,636` | `B916B42409210E269241CE6187053E4411DE57502A7DD06B934455F46F428D2A` |
| [D7-R1 report](STATE-06-MOD-12-PF-OBS-1-D7-R1-Summary-Count-Contract-Rerun-Report.md) | `9,815` | `4686BCA6C94BE60708BE439F697060EED56523AED81CED26C1B8C84705A0F820` |
| [D7-R1 Human Gate](STATE-06-MOD-12-PF-OBS-1-D7-R1-Human-Gate-Report.md) | `3,321` | `D0FE791D85136CFE44776465DEC093122C909F5C7E07A957A23F5E1504A5C1D9` |

The retained local evidence was also found intact against its owning reports:

| Lot | Retained reports | Total bytes | Disposition |
|---|---:|---:|---|
| D5 | `8` | `1,249,536` | Historical blocked evidence; never counted as D8 |
| D6 | `1` | `29,778` | Historical blocked evidence; never counted as D8 |
| D7 | `3` | `94,370` | Historical contract-incomplete evidence; never counted as D8 |
| D7-R1 | `3` | `95,170` | Historical admissible evidence; never counted as D8 |

A future D8 gate must verify every predecessor byte count and SHA-256 against
the owning report before and after activity. Any absence or mismatch blocks
D8 without altering, renaming, regenerating or replacing predecessor
evidence.

## Preserved V3 contract

D8 preserves:

- the V3 precondition and its two existing forced collections;
- the original scenario order and original sample records;
- five warm-ups and thirty measured samples for each of
  `FirstByte/Cold`, `FirstByte/Warm`, `Idle/Cold` and `Idle/Warm`;
- one original V3 summary immediately after each complete group;
- five `Cancellation/Cold` warm-ups followed by measured repetitions 1–13;
- `158` expected samples and four expected summaries for a complete prefix;
- the unchanged inclusive repeatability coefficient limit `0.20`;
- the unchanged inclusive empirical working-set limit `786,432 bytes`; and
- all other V3 absolute, resource, work, cancellation and evidence limits.

D8 adds no metric or measurement inside the child process. The existing V3
before, checkpoint and after observations remain the sole sample-level
resource evidence.

## Proposed future implementation boundary

A separately authorised implementation may add only:

- a distinct immutable D8 identity and non-authorising evidence envelope;
- exact marker-gated D8 measured and supervisor entrypoints in the existing
  test-only sandbox boundary;
- a strict D8 temporary root and atomic writer;
- typed complete-prefix and valid-early-stop report shapes;
- raw UTF-8 schema validation and typed round-trip regressions for all
  expected/completed sample and summary counts;
- an independent supervisor validator for V3 order, failures, counts and
  working-set classification; and
- architecture tests proving zero D8 reference in normal `src/` composition
  and zero new capture, collection or operating-system observer in the
  measured process.

The future measured child must reuse the existing V3 scenario construction,
precondition, sample runner and summary. It must not change V3, D5, D6, D7,
D7-R1 or their retained evidence.

## Frozen future sequence

### D8-0 — Predecessor and source gate

Before implementation:

1. prove the D8 canonical statement and digest;
2. prove the V3, D5, D6, D7 and D7-R1 frozen identities;
3. prove every retained predecessor artefact against its owning byte count
   and hash;
4. prove a clean or safely isolatable worktree;
5. prove zero DB-Notifier runtime, listener or D8 temporary root; and
6. identify the exact test-only files permitted to change.

Any mismatch blocks D8 before source modification.

### D8-1 — Static schema and isolation gate

Before a physical process, a future implementation must:

1. build the isolated test-only boundary without restore or download;
2. prove exact `158`-sample and four-summary membership and order;
3. serialise and round-trip a complete report with numeric
   `expectedSampleCount=158`, `completedSampleCount=158`,
   `expectedSummaryCount=4` and `completedSummaryCount=4`;
4. serialise and round-trip every permitted valid-early-stop boundary with
   internally consistent numeric counts, the exact retained V3 failure and
   `Complete=false`;
5. serialise and round-trip an exact `Cancellation/Cold` working-set target
   stop with `4/4` summaries, the completed sample that crossed the limit and
   no later sample;
6. reject missing, duplicate, string-valued, negative, excessive or
   inconsistent count fields;
7. prove one original summary after each completed full group and no summary
   for an incomplete group;
8. prove the unchanged V3 failure codes, repeatability and working-set
   limits;
9. prove no additional in-process capture, forced collection, external
   observer or D5 control;
10. prove strict output paths, atomic publication, bounded arguments,
   sanitisation and fail-closed cleanup; and
11. reverify all predecessor evidence.

Any failure blocks D8 before physical execution. A reflection-only schema
check is insufficient.

### D8-U — Two fixed unobserved attempts

Only after D8-0 and D8-1 pass, execute exactly two consecutive fresh
processes using the same frozen Release sandbox-host binary.

Each process:

1. executes the unchanged V3 precondition;
2. executes each of the four complete V3 groups and its original summary in
   order;
3. stops at the exact unchanged V3 failure boundary if a sample or summary
   fails;
4. otherwise enters `Cancellation/Cold` and executes until an unchanged V3
   failure or measured repetition 13;
5. atomically publishes every completed sample, completed summary, failure
   and sanitised environment fact; and
6. remains non-authorising with `ActivationState=None`.

If run 1 publishes an admissible report, including a valid early stop or exact
target stop, run 2 still executes. This is part of the frozen two-attempt
design and tests recurrence; it is not a replacement. If run 1 is
contract-invalid, publication fails or cleanup fails, run 2 is prohibited. No
attempted run is replaced.

## Admissibility criteria

A D8 report is admissible only when:

- every frozen identity and digest is exact;
- the one-based run and frozen host binary are exact;
- all completed scenarios are the unchanged contiguous V3 prefix;
- expected counts are numeric `158` samples and four summaries;
- completed counts match the retained arrays and failure boundary exactly;
- every completed full group has exactly one original V3 summary;
- any early stop retains the exact original V3 failure and no later sample;
- an exact target stop retains `4/4` summaries, between `141` and `158`
  completed samples, the completed `Cancellation/Cold` sample that crossed
  the working-set limit and no later sample;
- a complete prefix retains `158/158` samples and `4/4` summaries;
- the supervisor independently validates order, counts, failures and
  classifications;
- no external observation or D5 process-history capture is present;
- evidence is sanitised, atomically retained and non-authorising; and
- cleanup proves zero project process, listener, temporary sibling and D8
  temporary root.

An invalid or incomplete report blocks D8 and prohibits any replacement.

## Decision tree

Apply this tree only to the two fixed admissible attempts:

1. If either report is invalid, classify
   `D8.BLOCKED_EVIDENCE`; do not interpret V3 or working-set results.
2. If either process fails before becoming target-eligible:
   - any failure other than the exact original repeatability-coefficient
     failure on one of the four pre-target summaries:
     `D8.PREFIX_BLOCKED_OTHER_V3`;
   - one exact pre-target summary failure and one target-eligible outcome:
     `D8.EARLY_GATE_INTERMITTENT`;
   - two exact pre-target summary failures at the same phase:
     `D8.EARLY_GATE_REPEATED`;
   - two exact pre-target summary failures at different phases:
     `D8.EARLY_GATE_DIVERGENT`.
   No working-set recurrence classification is entered.
3. A process becomes target-eligible when it either completes the exact
   `158`-sample prefix without an excess or stops after retaining an exact
   `Cancellation/Cold` working-set failure through measured repetition 13.
   If both processes are target-eligible, count each process once when it
   retains that exact target failure:

| Target-eligible processes with an excess | Classification | Consequence |
|---:|---|---|
| `0/2` | `D8.WORKING_SET_NOT_REPRODUCED` | Stop; no external or D5-control arm |
| `1/2` | `D8.WORKING_SET_INTERMITTENT` | Stop; any causal or external design requires a new proposal |
| `2/2` | `D8.WORKING_SET_REPEATED` | Stop; a separately authorised external/control proposal may be considered |

Every classification remains diagnostic and non-authorising. None approves
PF-OBS-1, HM-01–HM-03, O5, Observer or lifecycle progression.

## Stop rules

D8 stops fail-closed when:

- any proposal, predecessor, source or retained-evidence identity differs;
- D8-0 or D8-1 is incomplete or fails;
- a report is absent, malformed, inconsistent or cannot be validated
  independently;
- a process starts with a different binary or outside the strict marker;
- a historical or attempted process would need to be replaced;
- any sample, summary, threshold, workload, precondition or V3 failure
  semantics change;
- an additional in-process capture, forced collection, external observation
  or D5 control appears;
- restore, download, dependency, privilege elevation or host mutation would
  be required;
- sanitisation, atomic retention or exact cleanup fails; or
- execution would require PostgreSQL, a provider, operational data, normal
  product composition, Observer, `ActivationState` or lifecycle change.

## Required future validation and deliverables

A future authorised increment must deliver:

1. the isolated D8 identity, envelope, writer, supervisor and regressions;
2. complete D8-0 and D8-1 evidence;
3. exactly two attempted D8-U reports unless the first is contract-invalid;
4. before/after predecessor-evidence verification;
5. applicable Release build and focal/full regression suites without restore;
6. a factual D8 report with every attempt and the exact decision-tree result;
7. sanitised retained hashes and cleanup evidence;
8. current-state and append-only history updates;
9. formatting, documentation, Markdown, secret and diff gates; and
10. one focused local commit, including a blocked or failed outcome.

The automatic D8 result is not a Human Gate and cannot approve PF-OBS-1, O5,
Observer activation or lifecycle progression.

## Documentary validation

- The canonical statement reproduced SHA-256
  `208DA70A8D638E50E2951DECDA83414B9E1F7CF4AFF957D09C1EAA2A8B1B8814`.
- All `15` retained predecessor reports matched the byte counts and SHA-256
  values in their owning reports.
- All six frozen owner documents matched the byte counts and SHA-256 values
  recorded in this proposal.
- The source-documentation gate passed for `427` comment-capable files.
- The Markdown link gate passed for `832` local links in `213` files.
- The current non-ignored worktree and available Git history passed the
  secret scan.
- `git diff --check` passed.
- Product build, tests, physical processes and runtime were not executed
  because the current authority is exclusively documentary.

## Explicitly excluded

- implementation or execution under the current documentary authority;
- modification or reclassification of V3, D5, D6, D7 or D7-R1;
- change to a workload, sample, summary, threshold or failure code;
- replacement or favourable selection of historical or D8 attempts;
- in-process instrumentation, polling, tracing, ETW/EventPipe,
  debugger/profiler, priority, affinity or host-setting changes;
- external snapshots, D5 controls or the complete HM-01–HM-03 campaign;
- dependency, restore, download or installation;
- PostgreSQL, provider, database, operational data or credential;
- normal runtime composition, Observer, recommendation or automation;
- `ActivationState` or lifecycle change;
- deployment, publication, push or pull request; and
- causal attribution or speculative correction.

## Future authority required

No implementation, build, test or physical process may begin from this
proposal alone. A future authority, if desired, must cite the exact version
and digest and may use:

```text
AUTORIZO exclusivamente a implementação e execução do PF-OBS-1-D8 conforme a proposta versionada pfobs1-d8-exact-prefix-completion-reconciliation-1.0.0 e SHA-256 208DA70A8D638E50E2951DECDA83414B9E1F7CF4AFF957D09C1EAA2A8B1B8814. Autorizo a implementação test-only isolada da identidade, envelope, writer, supervisor e regressões D8; os gates D8-0 e D8-1; e, somente após ambos passarem, exatamente dois processos novos D8-U com o prefixo V3 exato de até 158 amostras, quatro resumos antes de Cancellation/Cold e target stop exato se o working-set exceder o limite até measured repetition 13. Se o primeiro relatório for admissível, inclusive como valid-early-stop ou target-stop, autorizo o segundo processo como a segunda tentativa pré-registada; se for contractualmente inválido, o segundo permanece proibido. Autorizo evidência sanitizada, retenção separada, verificação before/after das evidências predecessoras, cleanup, documentação e commit local. Permanecem proibidos substituir tentativas; alterar V3, D5, D6, D7, D7-R1, workloads, failures ou thresholds; instrumentação intraprocesso; observação externa, controles D5 ou campanha HM-01–HM-03; polling, tracing, ETW/EventPipe, debugger/profiler; dependência, restore ou download; PostgreSQL, provider ou dado operacional; Observer, ActivationState, lifecycle, deploy, push e PR.
```
