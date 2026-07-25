# PF-OBS-1 — Post-D3 Physical Campaign Report

## Decision

The post-D3 PF-OBS-1 V3 physical campaign is `FAILED`.

Execution stopped during the first of the two authorised consecutive
campaigns. The second campaign was not started, no replacement campaign was
run and no third campaign was run.

`ActivationState=None` remains unchanged.

## Authorised baseline and protocol

| Field | Value |
|---|---|
| Baseline | `ec2013a379963544e0f33b6497dc23ba503e5fd8` |
| Protocol | `pfobs1-physical-measurement-3.0.0` |
| Protocol SHA-256 | `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A` |
| Runner | Existing marker-gated test-only host containing D3 |
| Campaigns authorised | Exactly two consecutive campaigns |
| Campaigns started | One |
| Campaigns completed | Zero |
| Replacement or third campaign | None |

The existing Release host was used without build, restore, download,
implementation or configuration change. Reflection inspection before
execution confirmed that its integration assembly contained the internal
`O5R5CancellationInputBuffer` D3 component and its `Materialise` method.

## Exact gate failure

| Field | Retained value |
|---|---|
| Diagnostic | `o5r5d1.threshold.working-set-peak` |
| Phase | `Cancellation` |
| Temperature | `Cold` |
| Sample | measured repetition `13` |
| Metric | `WorkingSetPeak` |
| Observed | `2,916,352 bytes` |
| Inclusive limit | `786,432 bytes` |
| Excess | `2,129,920 bytes` |
| Completed samples | `158/560` |
| Completed summaries | `4/16` |

The failed sample retained:

- elapsed time `16.0434 ms`;
- CPU time `15.625 ms`;
- heap peak delta `0 bytes`;
- allocation peak `0 bytes`;
- cumulative managed allocation `0 bytes`;
- declared work `100,000` units;
- accounted memory `524,288 bytes`.

This evidence proves that D3 removed the former per-sample managed input
allocation at the failure point. It also proves that removing that allocation
was insufficient to keep the process working-set delta within the unchanged
absolute limit. The allow-listed counters do not identify ownership of the
remaining committed pages, so this campaign does not infer a GC, runtime,
thread-stack, scheduler or operating-system cause.

## Results reached before the stop

| Phase | Temperature | Result | Repeatability coefficient | Working-set maximum |
|---|---|---|---:|---:|
| FirstByte | Cold | Passed | `0.03282218637396583` | `69,632 bytes` |
| FirstByte | Warm | Passed | `0.06657535708486224` | `0 bytes` |
| Idle | Cold | Passed | `0.011478682961185926` | `442,368 bytes` |
| Idle | Warm | Passed | `0.008208101631600236` | `520,192 bytes` |

FirstByte uses the frozen fixed-window repeatability basis. These four
isolated summaries do not prove a complete campaign or two-campaign
reproducibility.

## HM-01–HM-03 classification

| Measurement | Classification | Basis |
|---|---|---|
| HM-01 latency, cancellation and control update | `BLOCKED` | The Cancellation matrix stopped before HM-01 completed. |
| HM-02 heap, working set and allocation | `FAILED` | Cancellation/Cold exceeded the absolute working-set limit by `2,129,920 bytes`. |
| HM-03 CPU and time per work unit | `NOT TESTED` | Parse, cryptography, ordering and analysis were not reached. |
| Two-campaign reproducibility | `FAILED` | The first campaign failed, so the mandatory stop rule prohibited the second. |

## Retained evidence

- Run identifier:
  `20e251594f0747dfb6f2bd5fd8046020`.
- Local path:
  `artifacts/pf-obs-1/20e251594f0747dfb6f2bd5fd8046020/hm-01-03-run-1.json`.
- Size: `123,348 bytes`.
- SHA-256:
  `4699130A77448E14E99C38BB279F5DD9A29E67D654CEA8E22FF58800943E2C84`.
- Structural state: complete failed report, `158` raw samples, four
  summaries, one allow-listed diagnostic and no temporary sibling.
- Environment: Microsoft Windows `10.0.26200`, .NET runtime `10.0.9`,
  SDK `10.0.301`, x64 process, eight logical processors and
  `16,963,534,848` installed-memory bytes.

The report was written by the bounded host, copied through a write-through
stream, flushed to disk, decoded, protocol-validated, hash-verified and
atomically committed to the project-owned retained archive before cleanup.

## Execution integrity and cleanup

Two orchestration attempts were rejected before PowerShell or the physical
host started: one JavaScript syntax rejection and one local execution-policy
rejection. They created no process, temporary root or evidence and are not
physical campaigns.

The one direct host execution is the sole physical campaign in this
authorisation. After its failed evidence was retained:

- the second campaign was not started;
- no replacement or third campaign was executed;
- the exact physical temporary root was removed;
- zero DB-Notifier product process or listener remained;
- the archive contained exactly the committed run-one report;
- no temporary evidence sibling remained;
- the source worktree remained clean before this factual documentation.

No PostgreSQL, provider, database, corpus, credential, network access,
Observer runtime, push, deploy or lifecycle transition was used.

## Consequence

PF-OBS-1 remains stopped at the unchanged absolute physical working-set gate.
D3 remains valid as a bounded allocation remediation, but the physical
campaign proves that it did not close the working-set failure.

Any further diagnosis or remediation of the residual Cancellation/Cold
working-set delta requires separate explicit authorisation. No further
physical execution is authorised by this report.
