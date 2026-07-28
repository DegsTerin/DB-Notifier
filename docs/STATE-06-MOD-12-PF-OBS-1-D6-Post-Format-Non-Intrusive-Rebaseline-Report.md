# PF-OBS-1-D6 — Post-Format Non-Intrusive Rebaseline Report

## Decision

PF-OBS-1-D6 is `BLOCKED`.

The first authorised unobserved process reached evidence publication, but the
atomic writer raised `IOException` because it attempted to move its temporary
file before closing the write-through stream. The writer then removed the
temporary sibling as designed, leaving no durable report from which sample
completeness or working-set reproduction could be classified.

The preregistered stop rule was applied. The first attempt was not silently
replaced, the second unobserved run was not started, and neither external
observation nor the historical D5 comparison was executed. The writer defect
has been corrected and covered by a regression test, but the two-run
rebaseline requires fresh explicit authority.

## Authority and baseline

Bruno authorised a local, synthetic and test-only post-format rebaseline with:

- a sanitised environment declaration;
- a preregistered protocol;
- isolated implementation and tests;
- exactly two executions of the unchanged V3 prefix without additional
  in-process instrumentation;
- two externally observed executions and one D5 comparison only if both
  unobserved executions reproduced the excess.

PostgreSQL, operational data and providers, dependencies, downloads, V3 limit
changes, the complete HM-01–HM-03 campaign, Observer, `ActivationState`,
lifecycle, deployment, push and pull request remained prohibited.

The initial repository baseline was branch `main`, commit
`e464fa1dc54c1beac2f34c8efc103d0a2201908a`, with no DB-Notifier runtime or
owned listener active.

## Frozen method

The
[D6 protocol](STATE-06-MOD-12-PF-OBS-1-D6-Post-Format-Non-Intrusive-Rebaseline-Protocol.md)
was frozen before physical execution as
`pfobs1-d6-post-format-non-intrusive-rebaseline-1.0.0`, SHA-256
`5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845`.
It depends on the unchanged V3 digest
`60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.

Each unobserved fresh process is restricted to the original first `158`
scenarios:

- complete `FirstByte/Cold`, `FirstByte/Warm`, `Idle/Cold` and `Idle/Warm`
  groups, including their four original summaries;
- `Cancellation/Cold` through measured repetition 13;
- the two existing V3 forced collections only;
- no additional in-process memory, thread, handle or process-state capture.

This corrects the methodological ambiguity in D5: D6 restores the four
original summaries at their original temporal positions and removes D5's
additional process-history captures from the measured process.

## Sanitised environment

| Fact | Observed value |
|---|---|
| Operating system | `Microsoft Windows 10.0.26200`, `X64` |
| Process architecture | `X64` |
| Repository-selected SDK | `.NET SDK 10.0.302` |
| Runtime used by the host | `Microsoft.NETCore.App 10.0.10` |
| Logical processors visible to the process | `8` |
| Installed physical memory | `16,963,534,848 bytes` |
| Monotonic-clock frequency | `10,000,000 ticks/second` |

No machine, host, user or device name, process identifier, credential,
payload or provider topology is retained.

## Attempt and incident

Unobserved run 1 was started in one fresh marker-gated process. The process
returned `o5r5d6.failed:IOException` during evidence publication and exited
non-zero. No final JSON or temporary sibling remained.

Code inspection located the defect in the atomic writer: the file stream used
an `await using` declaration whose scope extended beyond `File.Move`.
Consequently, Windows still held the temporary file open when publication was
attempted. The correction gives the stream an explicit nested disposal scope
and moves the file only after that scope has ended.

Because no durable report exists, D6 does not claim:

- the number or order of completed samples;
- completion of all four summaries;
- reproduction or non-reproduction of the working-set excess; or
- any causal interpretation.

The failed attempt consumed the first execution allowed by the exact
authorisation. Starting a replacement or run 2 would have silently changed
the authorised two-execution procedure, so execution stopped.

## Validation

| Check | Result |
|---|---|
| D6 focal integration tests after the correction | passed, `4/4` |
| Complete integration suite | passed, `141/141` |
| D6 focal architecture isolation | passed, `1/1` |
| Complete architecture suite | passed, `97/97` |
| Release solution build with `--no-restore` | passed, zero warnings/errors |
| Evidence-writer close-before-move regression | passed |
| .NET formatting and analyser gate | passed at warning severity |
| Code-documentation gate | passed, `422` comment-capable sources |
| Markdown-link gate | passed, `811` local links in `207` files |
| Current-worktree and available-history secret scan | passed |
| Dependencies, restore and download | unchanged / not executed |
| PostgreSQL, provider or operational data | not used |
| External observation and D5 comparison | not executed |
| HM-01–HM-03 campaign | not executed |

An initial combined formatting and documentation command exceeded its
60-second command window before producing a result. A process inventory found
no owned residue. Each gate was then executed separately and passed; the
timeout was not classified as either a pass or a product failure.

The implementation remains confined to the integration-test assembly and the
existing consolidated sandbox host behind the exact marker
`pf-obs-1-d6-post-format-non-intrusive-test-only`. Normal `src/` composition
contains no D6 reference, and every report remains non-authorising with
`ActivationState=None`.

## Cleanup

- the exact temporary root from the failed attempt was empty and removed;
- the empty retained-evidence root was removed;
- no final JSON or `.tmp` sibling was retained;
- no D6 process, DB-Notifier product process or owned listener remained;
- no browser, PostgreSQL, Docker, provider, database or external runtime was
  started.

## Consequence

D6 remains incomplete and technically `BLOCKED`. The corrected implementation
is ready, but no valid physical rebaseline result exists.

A fresh explicit authority is required for two complete unobserved D6
executions. Only if both reproduce the unchanged excess may the already
defined external-observation arm and historical D5 comparison proceed. Such
authority would not approve PF-OBS-1, O5, Observer activation or a lifecycle
transition.
