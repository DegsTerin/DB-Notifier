# STATE-06 — R6-WEB1 Automatic Remediation Report

## Decision summary

- Authorised baseline: `1a192a82b3879975e5ecd51a1ebfd156c9db52da`
- Scope: Web containment of the disabled status and responsive separation between status, latency and sparkline evidence
- Design System contract: `3.2.0`, unchanged
- Implementation state: `COMPLETE`
- Automatic result: `APPROVED` within the restricted local R6-WEB1 scope
- Focused commit: this report is committed with the completed increment; the identifier is recorded in the final hand-off
- Human sample `R6-HV-W01`: `REJECTED`; this automatic result does not replace or infer a human decision
- `R6-HV-W02`: `BLOCKED`
- `R6-HV-P01`: `NOT TESTED`

R6-WEB1 corrects only the Web defect recorded after R6-UI1. It does not approve W01, accept R6, resume W02/P01 or change lifecycle state. Repeating W01 visibly requires a separate proposal and explicit authorisation.

## Authority and preserved boundaries

The increment changes the shared Web status markup, the Overview row layout, existing Dashboard browser auditors, one directly related presentation regression test and factual documentation. WPF, Tray/flyout, route identifiers, contracts, fixtures, persisted state, generated localisation, generated tokens, provider assets, dependencies, `package.json` and lockfiles remain unchanged.

No operational source, provider, database, credential, secret, notification, command, administrative execution, schema, migration, restore, installation, download, external access, remote CI, push or deployment was used. Normal delivery, Agent Fleet, SignalR outside its existing test-only sandbox, provider loading, packaging and command execution remain disabled or unavailable as previously recorded.

## Factual cause

The rejected human sample exposed a container-width defect that the prior gate did not measure:

- the Overview remained in its two-column desktop composition slightly above the global `1100px` breakpoint, leaving the fleet panel narrower than the viewport implied;
- each instance row still allocated five tracks with a minimum-width status track, latency and sparkline;
- the global status pill deliberately used `white-space: nowrap`, but the long disabled label could be wider than the grid track assigned to its border;
- the text therefore escaped the visible pill and entered the sparkline region while the owning panel's `overflow: hidden` masked the descendant overflow from the existing page-level gate;
- the previous auditor measured the Overview container and document width, but not the text fragments inside the pill or intersections among status, latency and sparkline rectangles.

This was a Web Presentation defect. It did not require a data, fixture, provider, persistence or operational-authority change.

## Implemented correction

- The localised status label now has a dedicated inline element inside the unchanged semantic badge, allowing its visible fragments to be measured and contained without changing the accessible name.
- Overview rows now place the status badge inside an explicit bounded status region. The global no-wrap behaviour remains unchanged for Inventory and other status consumers.
- The fleet panel owns an inline-size container. Its rows use named grid areas and reflow from the panel's actual width rather than waiting for a viewport breakpoint.
- At constrained panel widths, status remains after the instance identity and latency/sparkline move into a following row. At the compact threshold, identity, status, latency and sparkline form one ordered vertical flow.
- The full pt-BR and en-GB disabled labels may wrap only when necessary. Font size, label content and sparkline evidence are not reduced, truncated, hidden or removed.
- The authoritative `no-sparkline` branch uses the same ordered status containment without reserving a false demonstration trend region.

Healthy, Warning/Degraded, Critical, Unknown, Stale and Disabled remain text-first and retain their icon and border. Enabled=false remains visible and excluded from current health. Demonstration/source truth, accepted evidence time, explicit time zone, provider fallback, forced-colour behaviour and keyboard/accessibility contracts remain unchanged.

## Strengthened automatic evidence

No new harness was introduced. The existing Dashboard auditor now measures every Overview row using real browser geometry:

- `Range.getClientRects()` proves every visible text fragment lies inside the pill;
- pill bounds lie inside the dedicated status region, which lies inside the row;
- latency and each present sparkline lie inside the same row;
- status/latency, status/sparkline and latency/sparkline intersection areas are zero;
- pill `scrollWidth`/`scrollHeight` and row `scrollWidth` remain bounded, while the measured identity, status region, pill, label fragments, latency and each present sparkline remain vertically contained by row geometry;
- latency and every expected demonstration sparkline have positive geometry and visible computed styles; demonstration rows require four sparklines, while the authoritative branch requires zero;
- all four demonstration rows and exactly one disabled row remain present with the complete locale-specific disabled label;
- focal forced-colour samples prove the media query is active, all four pill borders remain visible and every pill resolves to system text and canvas colours;
- the same checks run in standard Dashboard, TV demonstration mode, forced colours and the existing authoritative HTTPS sandbox;
- the authoritative TV evidence retains zero demonstration sparklines and a visible source-truth explanation.

The ordinary matrix gained the human-review dimensions `1180×760` and `820×620`. Across pt-BR/en-GB and Light/Dark, it executed 128 viewport samples, 96 pre-existing forced-colour route/page-scale samples and 24 additional focal forced-colour zoom/reflow status samples. The focal `100%` and `200%` checks cover physical `1180×760` and `820×620` viewports; the `400%` checks use `1280×900` and `1440×1000`, yielding the supported `320px` and `360px` CSS reflow widths. Each focal sample combines the corresponding CSS layout width and device scale, asserts the resulting `innerWidth` and therefore exercises real reflow rather than counting duplicate pinch-scale geometry. The existing compact, minimum, desktop, ultrawide and TV widths also remained covered.

## Automatic evidence register

| Gate | Result |
|---|---|
| Baseline and shutdown preflight | `PASS` — exact baseline, clean worktree, no owned runtime or listener before work |
| Node/npm toolchain | `PASS` — installed Node.js `24.18.0` and npm `11.16.0`; no installation or global switch |
| Dashboard type-check and build | `PASS` — offline type-check and production build using existing dependencies |
| Dashboard tests | `PASS` — 67/67, including the new containment/reflow contract |
| Focused coverage | `PASS` — 25/25 presentation tests; 98.09% lines, 92.00% branches and 85.71% functions across the exercised modules |
| Browser geometry matrix | `PASS` — 128 viewports, 96 forced-colour route/page-scale samples and 24 focal zoom/reflow status samples in pt-BR/en-GB and Light/Dark |
| Authoritative TV browser sandbox | `PASS` — 12 bounded scenarios, zero external HTTP origin, no operational data, status contained and zero demonstration sparkline in authoritative mode |
| Architecture, excluding the preserved global gate | `PASS` — 5/5 Dashboard TV SignalR isolation and HTTP response-boundary tests |
| Global architecture gate | `13/14` in the proportional run — only the unchanged R0 assertion failed as recorded below |
| Generated brand, providers, tokens and localisation | `PASS` — 11 provider identities and 22 theme variants retained; generated outputs unchanged |
| Code documentation and Markdown links | `PASS` — 326 comment-capable files and 619 local links |
| Secret scan | `PASS` — current non-ignored worktree and available Git history |
| Protected diff | `PASS` — WPF/Tray, manifests, dependencies, lockfiles, generated sources and operational contracts have zero diff |
| Final cleanup | `PASS` — zero owned process, listener or temporary root after exact post-run audit |

The first toolchain attempt resolved the host `PATH` to Node.js `22.23.1`/npm `10.9.8` and failed closed. During final aggregation, invoking npm 11 by absolute path without also constraining its child-process `PATH` likewise resolved the child `node` command to 22.23.1 and failed closed. The already installed, project-required Node.js `24.18.0`/npm `11.16.0` directory was then selected only for the validation-process environment and restored afterwards; no installation, download, NVM activation or persistent PATH change occurred.

The first browser runner returned success, but an independent post-run audit found its exact GUID-owned Chrome profile directory still present with no referencing process or listener. After verifying its absolute path, runner prefix and zero process reference, the profile was removed under the authorised cleanup boundary. The existing runner was then hardened to require three consecutive bounded absence checks after deleting its exact GUID root; a directory that persists or reappears now fails closed. The final repeated audit proved `0` owned processes, `0` owned listeners and `0` matching temporary roots. No unrelated Chrome process, profile or file was touched.

One successful matrix run was followed by an incorrect failure in its outer ad-hoc PowerShell wrapper because that wrapper inspected a residual native `$LASTEXITCODE` after the audited script had already completed successfully. This was not a Dashboard or runner failure. The wrapper check was removed, the canonical runner invocation was repeated unchanged, and it completed with process exit code `0` and the same `128`/`96`/`24` passing totals.

The known global R0 failure remains exactly `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`: the assertion expects the pre-existing literal `state05-dashboard-failure.json`. No bypass, exclusion or correction was applied. The R5 NuGet metadata incident also remains recorded; this increment performed no restore, download or metadata access.

## Completion disposition

The authorised defect is corrected and automatically evidenced: the complete disabled label remains inside its status pill in pt-BR and en-GB, the pill remains inside its status region, latency and sparkline remain separate and contained, compact layouts reflow in factual order, no page-level horizontal overflow is introduced and authoritative mode continues to omit demonstration trends.

R6-WEB1 is therefore automatically complete within its restricted scope. `R6-HV-W01` remains rejected until a separately authorised visible repetition produces a new explicit human decision. W02 remains blocked, every P01 physical condition remains not tested, the R0 and R5 records remain intact, and no R6 acceptance, R7/R8 work, O1/AIOps implementation or lifecycle transition is authorised.
