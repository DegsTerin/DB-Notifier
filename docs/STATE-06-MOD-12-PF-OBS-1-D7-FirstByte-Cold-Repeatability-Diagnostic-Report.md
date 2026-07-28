# PF-OBS-1-D7 — FirstByte/Cold Repeatability Diagnostic Report

## Result

PF-OBS-1-D7 is `BLOCKED`.

All three authorised unobserved processes completed the exact V3
`FirstByte/Cold` group. None exceeded the unchanged inclusive repeatability
coefficient limit of `0.20`. However, their JSON schemas omitted the explicit
`expectedSummaryCount` and `completedSummaryCount` fields required by the
frozen evidence contract. The summaries themselves are retained, but the
omission makes every physical report contract-incomplete. Under the D7 stop
rules, the observed `0/3` cannot enter the D7-U classification table.

The schema and static regression were corrected after discovery, without
altering the retained evidence. No run was replaced and no additional process
was started. This blocked result does not invalidate the retained D6 failure,
establish its cause, approve PF-OBS-1 or O5, or authorise any activation or
lifecycle progression.

## Authority and baseline

- Authority: Bruno's explicit authorisation of the implementation, static
  gate, three unobserved processes and only conditionally two external
  processes.
- Frozen proposal:
  `pfobs1-d7-first-byte-cold-repeatability-diagnostic-1.0.0`.
- Frozen proposal SHA-256:
  `7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF`.
- Unchanged V3 SHA-256:
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
- Referenced D6 SHA-256:
  `5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845`.
- Git baseline: branch `main`, commit
  `a29e2d99b9d19f9cbc0a8e7b9901dd532d09803f`, clean worktree.
- Physical host binary SHA-256:
  `45B96DDD9ECB07CFC188700E8D8D0F0114F44F80B2417459172331D6D7E994B8`.
- Environment: .NET SDK `10.0.302`, installed physical memory
  `16,963,534,848` bytes, eight logical processors and
  `10,000,000 Hz` stopwatch frequency.
- Shutdown preflight: zero verified DB-Notifier process, window, notification
  area instance or owned listener before implementation and physical
  execution.

The environment declaration and retained reports contain no host name, user
name, repository path, process identifier, command line or local path.

## Isolated implementation

The implementation is contained in the integration-test assembly and the
existing consolidated sandbox host:

- one exact measured entrypoint with no D7 process counters;
- one supervisor entrypoint as the sole fresh-child launch boundary;
- original V3 scenario construction, precondition, sample runner and summary;
- independent integrity recomputation over the five fixed sequential groups;
- strict temporary destinations, bounded typed schemas and atomic evidence
  publication;
- one before/after operating-system counter allow-list implemented only for
  the conditional external arm; and
- architecture tests proving no D7 composition in `src/` and rejecting
  polling, tracing, priority, affinity, power and additional garbage
  collection controls.

The external capture implementation received structural and unit coverage but
was not physically exercised because the protocol gate remained closed.

## Static gate and escaped defect

The initial static gate passed before physical execution for the following
properties:

- the D7, V3 and D6 digests matched their immutable declarations;
- the selected group was exactly the first `35` V3 scenarios: five warm-ups
  followed by thirty measured `FirstByte/Cold` scenarios;
- the original summary was invoked exactly once after all samples;
- D7 markers were absent from product composition;
- external APIs appeared only in the supervisor;
- no D7 polling, tracing, ETW/EventPipe, debugger, profiler, priority,
  affinity, power-plan or extra garbage collection mechanism was present;
- the focal D7 integration tests passed `7/7`;
- the focal architecture test passed `1/1`; and
- the build completed with zero warnings and zero errors, without restore or
  download.

That initial gate did not assert the two explicit summary-count properties and
therefore allowed an incomplete evidence schema to reach physical execution.
This is a D7 gate escape. After discovery:

- `expectedSummaryCount` and `completedSummaryCount` were added to the typed
  measured report;
- supervisor identity validation now enforces both values;
- the protocol identity regression now enforces the schema properties; and
- the corrected implementation passed the applicable build, focal and
  architecture gates.

These corrections do not repair, rewrite or supersede the three already
published physical reports.

## D7-U physical evidence

Execution identifier:
`0939b6560d2c4556ad781be9d9e3b376`.

Each row represents a new unobserved process. All rows retained `35/35`
samples, the original summary object, exact binary agreement with the
independent integrity computation, `ActivationState=None`, no external
observation and the code `o5r5d7.report.complete`. Despite that code, every row
is contract-incomplete because both required explicit summary-count fields are
absent.

| Run | V3 coefficient | Five group medians in milliseconds | SHA-256 | Bytes |
|---:|---:|---|---|---:|
| 1 | `0.024460739425192193` | `0.0769`, `0.08175`, `0.08215`, `0.08095`, `0.08205` | `AB48D99C1688EB3FC163DBAD59D4E393739B04BF1C82AB59C9239A5B180E2C85` | `31,467` |
| 2 | `0.010868664937096893` | `0.0706`, `0.0705`, `0.0706`, `0.0689`, `0.06915` | `0087DFAA4E7E014A7C037E1887C879A4A9C5501F1228280962854ED62224066F` | `31,430` |
| 3 | `0.011026690622714398` | `0.0706`, `0.07045`, `0.0704`, `0.06905`, `0.0688` | `7E167AFEBBC01F8DF4551A12CFD6036F2CD2156A0D92EF73E3DD80C9212D00C2` | `31,473` |

Exact V3 repeatability failures: `0/3`.

Automatic D7-U classification: not entered; `D7 BLOCKED`.

## Conditional D7-E decision

The external arm requires a valid completed D7-U classification with at least
`2/3` exact failures. D7-U is blocked by incomplete reports, so D7-E is
prohibited. The observed summaries would also have produced only `0/3`
failures, but that non-admissible observation is not used as the release gate.

Consequently:

- external processes executed: `0/2`;
- external before/after snapshots captured: `0`;
- D7-E classification: not applicable; and
- no historical D5 control was authorised by or executed under D7.

## Relationship to D6

The retained D6 process reported `0.22923232701994542` from group medians
`0.08985`, `0.1424`, `0.14285`, `0.16075` and `0.093` milliseconds. D7
reported three complete coefficients below `0.025`.

This comparison shows only that the D6 excess was not observed in the three
fresh but contract-incomplete D7-U reports. It is not an admissible D7
classification and does not prove a scheduler, runtime, garbage collector,
hardware or host-load explanation. No external association can be assessed
because the external arm remained prohibited.

## Validation and incidents

- Full solution build: passed with zero warnings and zero errors.
- Full integration suite: the initial parallel run passed `146/148`. The two
  failures were pre-existing O3A/O3B process-bridge tests that concurrently
  replace global `Console.Out`, producing exchanged or empty capture. Each
  failed test passed in its own fresh isolated test process (`1/1` and `1/1`).
  The final full run passed `148/148`, confirming transient legacy test-suite
  concurrency interference rather than a D7 behavioural regression.
- Full architecture suite: passed `98/98`.
- Formatting, source-documentation, Markdown-link, secret and diff gates:
  passed at final hand-off.

The first compilation attempt exposed two nullable-flow diagnostics in the new
argument parser. No runner was entered. The annotations were corrected and
all subsequent builds passed.

The post-execution evidence-contract review then found the absent explicit
summary-count fields. The tracked implementation and static regression were
corrected, but the three retained reports were deliberately left byte-for-byte
unchanged. The defect therefore blocks D7 rather than causing a silent
evidence rewrite or replacement run.

One pre-execution hash audit used the reserved PowerShell variable `$Host` and
therefore produced no hash. It did not create an evidence root or start a
measurement. The audit was repeated with a task-specific variable and passed;
no physical run was consumed or replaced.

## Evidence retention and cleanup

Three sanitised final reports are retained locally below:

`artifacts/pf-obs-1-d7/0939b6560d2c4556ad781be9d9e3b376/`

The ignored evidence files are not part of the source commit. Their hashes and
byte counts are fixed in this report. Temp-to-retained hashes matched, and
explicit scans found none of the prohibited identity or path fields.

The exact temporary D7 root was removed after retention. No `.tmp` or
`.measured.json` sibling remained. The final shutdown check found zero
project-owned process, listener or D7 temporary root.

## Boundaries and resulting state

No V3 or D6 code, threshold or historical evidence was altered. No
instrumentation was added inside a measured process. No dependency, restore,
download, PostgreSQL, provider, operational datum, HM-01–HM-03 campaign,
Observer, `ActivationState`, lifecycle transition, deployment, push or pull
request occurred.

PF-OBS-1-D7 is `BLOCKED` by three contract-incomplete reports. The corrected
implementation has passed the applicable non-physical validation, but no
physical rerun is authorised.
PF-OBS-1-D6, PF-OBS-1 and O5 remain blocked; `STATE-06 INTEGRATION` and
`ActivationState=None` remain unchanged. Any D7 rerun, causal investigation,
new physical diagnostic or campaign requires separate explicit authority.
