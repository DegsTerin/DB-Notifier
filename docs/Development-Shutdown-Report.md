# Development shutdown ownership repair

## Scope and authority

This isolated repair corrects a Windows Codex CUA ownership false positive in
the canonical shutdown preflight. The owner explicitly permitted only this
repair after independent manual process ownership verification. Existing
BLOCKED results remain valid historical evidence; no process is terminated
and no general-purpose exclusion interface is introduced.

The exact scope and baseline are recorded in [PLANS.md](../PLANS.md).

## Verification contract

Strong DB-Notifier executable and repository-path ownership take precedence.
A shared CUA host requires coherent executable, parent and command-role
evidence and verified absence of a window or TCP listener. Missing metadata,
ordinary Node workloads and conflicting identity remain fail-closed.

The existing live owned-helper regression remains unchanged. New synthetic
tests cover recognised CUA roles and negative identity, activity and metadata
cases. Focused tests, the actual corrected preflight, Quick and canonical Full
are separate evidence. No prior failure is relabelled or retried.

## Observed verification

The candidate is independently authored against public parent
`1a07fcd24d58941b06954c8415fa4862e902d077`. The production recogniser hashes
the complete raw launcher prefix, retaining internal bytes and trimming only
outer whitespace. Its two literal SHA-256 identities were independently
derived from the previously observed command records. Unknown bootstraps,
different parents, redirected paths, windows, listeners or missing live
identity evidence do not receive the exclusion. Vendor command text is not
stored in this repository or executed by the tests.

- AST: passed for both changed PowerShell files.
- Independent lifecycle and security reviews: no unresolved P0–P3 findings.
- First focused synthetic regression: exit 0, 46 assertions.
- Historical in-memory replay: exit 0, 48 assertions, including rejection of
  slash-altered prefixes through the real candidate digest implementation.
- Actual corrected shutdown preflight: exit 0, zero matching processes and
  zero owned listeners.
- Existing development-flow suite: exit 0, 158 assertions, including the
  unchanged real owned-helper regression; policy verifier: 160 assertions.
  Its preflight and final shutdown both passed.

Three external replay-preparation helpers failed before invoking the
classifier: incompatible transcript file sharing, a wrong transcript content
type and strict-mode property enumeration on an empty JSON object. Their
first receipts remain unchanged. Each separately reviewed preparation
successor addressed its specific cause; no failed product candidate was
retried, relabelled or counted as a passing test.

Sanitised first-result receipts are retained outside the deliverable under
`C:/Projects/Temp/DB-Notifier-Shutdown-Evidence-20260905`. The original CUA
workers had exited naturally before live validation. Therefore the positive
incident case is demonstrated by historical replay with synthetic OS
metadata, not by claiming a live reproduction of those exited processes.

## Canonical validation closeout

Online locked Setup passed for 19 solution projects and 44 npm packages
without changing tracked files. Quick returned exit 0 (NON_GATE), but its
separate first closure returned BLOCKED for a transient PowerShell process.
A concurrent authentication diagnostic overlapped that observation; its
identity was not proved to be the matched PID. The PID subsequently
disappeared without termination. That first BLOCKED remains preserved. An
independently admitted, strictly serial preflight then passed; Quick was not
rerun and the recogniser was not broadened.

The first canonical Full passed on Windows from 00:58:26 to 01:08:37 UTC on
6 September 2026, with exit 0 and `DISPOSITION|PASS|stage=All`. It validates
commit `e24329559eabeea6003309722ab9c9525cdc43ab`, tree
`00f136bef59de31b2034c958e8b2ac12259f702f`, with 853 tracked inputs held
unchanged throughout the run. Final shutdown passed with zero matching
processes/listeners and the worktree remained clean.

- Tests: 543 unit, 101 architecture, 171 integration, 10 WPF and 79 Web.
  The additional unit-test coverage pass is not a second set of unique tests.
- Coverage: 83.39% lines and 56.46% branches, above the 70%/45% floors.
- Build: zero warnings and errors. Secret scanning, NuGet vulnerability
  checks, npm audit, bundle validation and fail-closed runtime checks passed.
- Dashboard audit: 128 viewport samples, 96 forced-colour samples and
  24 focal zoom/reflow samples across both supported locales and themes.
  The consolidated STATE-06 harness passed with local test data only.

The immutable `full-first.json` receipt has SHA-256
`3be36401710ec4882c33837619a35bbfc15689fbcadfe8c0a14bb87823621f68`.
This report and PLANS.md form a later documentation-only closeout; their own
documentation/link/diff checks do not transfer Full to that later tree. The
other 851 tracked inputs, including the production classifier and its tests,
remain byte-identical to the validated commit.

## Publication and remaining boundaries

The authorised publication target is the non-forced feature branch
`codex/shutdown-cua-20260905` in `DegsTerin/DB-Notifier`. Publication is accepted
only after the first push and exact remote-reference verification have their
own receipts, `publication-first.json` and `verified-ref-first.json`, under
the evidence root. Local Full is not evidence of remote CI, a merge or
promotion to main.

This narrow repair does not complete MN007, provider support, production
release or a lifecycle transition. Exact-prefix recognition deliberately
fails closed when an unknown Codex bootstrap is introduced and requires a
separately reviewed identity update.

The static Render Dashboard is unchanged and has no applicable deployment for
this tooling repair. MAIN's protected work and the separate MN007 candidate
remain untouched.
