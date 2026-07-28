# PF-OBS-1-D6 — Post-Format Non-Intrusive Rebaseline Report

## Decision

PF-OBS-1-D6 is `BLOCKED`.

The explicitly authorised resumption also stopped fail-closed. Its first fresh
process completed the `35` `FirstByte/Cold` samples and produced the first
original V3 summary, but that summary exceeded the unchanged repeatability
coefficient limit: `0.22923232701994542` observed against the inclusive `0.2`
limit. The prefix therefore stopped at `35/158` samples and `1/4` summaries
before reaching `Cancellation/Cold`.

The second unobserved process was not started. The failure neither reproduces
nor disproves the historical working-set excess because the relevant phase was
not reached. The conditional external-observation arm and historical D5
comparison remain unexecuted.

The paragraphs below preserve the earlier evidence-publication incident that
made this fresh authority necessary.

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

## Authorised resumption

Bruno explicitly authorised two new complete D6 executions after the evidence
writer correction, retaining the frozen D6 and V3 protocols and every original
limit. The resumption baseline was branch `main`, commit
`8383f78699a1857856eaae785277ee39b70438c4`, with a clean worktree and no
DB-Notifier process, window or owned listener.

The Release sandbox host was rebuilt with SDK `10.0.302`, `--no-restore`,
disabled build servers and shared compilation disabled. Its SHA-256 before the
physical attempt was
`09379824363CD6FCEC1053FAB20200AA19D6F54CD3D4DA50D92B485B2EA34A63`.

Unobserved run 1 started at `2026-07-28T12:27:52.8369709Z` in a fresh process
and stopped `43.588 ms` later after the first original summary failed:

| Fact | Retained result |
|---|---:|
| Complete samples | `35/158` |
| Original summaries | `1/4` |
| Phase / temperature | `FirstByte/Cold` |
| Warm-up / measured samples | `5/30` |
| Repeatability basis | `FirstByteFixedWindowElapsed` |
| Observed coefficient | `0.22923232701994542` |
| Inclusive coefficient limit | `0.2` |
| Stable failure code | `o5r5d1.threshold.repeatability-coefficient` |
| `Cancellation/Cold` reached | no |
| Working-set excess classified | no |

The fail-closed report declares the unchanged D6 and V3 digests,
`ActivationState=None`, `Complete=false` and
`WorkingSetExcessReproduced=false`. The last value means only that no retained
failure met the working-set reproduction predicate before the stop; it is not
evidence that `Cancellation/Cold` would have passed.

The sanitised evidence was retained as
`artifacts/pf-obs-1-d6/67b02229cf5246fb93d5230d29c494ea/unobserved-run-1.json`:

- size: `29,778 bytes`;
- SHA-256:
  `9E6578AE516524DC12E73B9F848303C1DDD3AF9614A53A6E2CBB95D2B3F36952`;
- prohibited host, user and repository-path values: absent;
- temporary source root: removed after retention.

The immutable summary failure is a D6 stop condition. Run 2 was not started,
no replacement was attempted and the conditional arms were not authorised by
the reproduction gate.

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
| Resumption Release host build with `--no-restore` | passed, zero warnings/errors |
| Resumption D6 focal integration tests | passed, `4/4` |
| Resumption D6 architecture isolation | passed, `1/1` |
| Resumption unobserved run 1 | fail-closed, `35/158` samples and `1/4` summaries |
| Resumption unobserved run 2 | not executed |
| Resumption code-documentation gate | passed, `422` comment-capable sources |
| Resumption Markdown-link gate | passed, `812` local links in `207` files |
| Resumption secret scan and diff check | passed |

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

D6 remains incomplete and technically `BLOCKED`. The corrected evidence writer
is proved, but the resumed prefix could not pass the first original V3 summary,
so no valid post-format working-set rebaseline exists.

Any investigation of the newly observed `FirstByte/Cold` repeatability failure,
another D6 attempt or a methodological change requires separate explicit
authority. The conditional external-observation arm and historical D5
comparison remain unavailable unless two complete unobserved runs first
reproduce the unchanged working-set excess. No result approves PF-OBS-1, O5,
Observer activation or a lifecycle transition.
