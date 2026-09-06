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

## Remaining validation and boundaries

Online locked Setup, Quick (NON_GATE), canonical Full and feature-branch
publication remain pending. This narrow repair does not complete MN007,
provider support, production release or a lifecycle transition. Exact-prefix
recognition deliberately fails closed when an unknown Codex bootstrap is
introduced and requires a separately reviewed identity update.

The static Render Dashboard is unchanged and has no applicable deployment for
this tooling repair. MAIN's protected work and the separate MN007 candidate
remain untouched.
