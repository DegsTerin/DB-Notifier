# STATE-06 — Audit Remediation R6-FC1 Forced-Colours Report

- Date: 2026-07-21
- Baseline: `639b67251771fce708f99092bc5baf5b45a9f53a`
- Lifecycle: `STATE-06 INTEGRATION`, unchanged
- Authorised defects: selected-route text invisible; Critical KPI right-hand boundary absent
- Automatic result: `APPROVED`
- Global gate: `NON-GREEN — PRE-EXISTING R0 ASSERTION ONLY`
- `R6-HV-D02` human decision: `REJECTED — VISIBLE REPETITION PENDING SEPARATE AUTHORITY`

## Authority and boundaries

R6-FC1 used only the separately authorised focal local remediation. It permitted minimal Dashboard CSS, directly related tests and assertions, the existing headless browser runner and harness, offline checks, factual documentation and one focused commit. It did not authorise a visible human sample, WPF/Tray, naming changes, a new harness or dependency, restore, download, external data, Windows configuration, operational sources, R7/R8, O1, AIOps activation or lifecycle progression.

The mandatory preflight observed the exact baseline, a clean worktree and zero DB-Notifier process, listener or temporary root. No React component, TypeScript product contract, data, localisation, generated token, provider asset, WPF/Tray file or normal runtime composition changed.

## Reproduction and root causes

The new assertions were applied before the CSS correction. The focused source test then produced the expected `64/65` result because the selected navigation item did not yet opt out of the browser's second forced-colour remapping and the independent Overview KPI edge rule was absent.

The existing browser audit was then run against the unchanged normal build. It stopped on the first pt-BR/Light combination with both expected failures:

- selected-route text, system colour or focus evidence was incomplete;
- one or more Overview KPI boundaries were absent in the 100%/200%/400% forced-colour matrix.

The regression runner released ports `7396`/`7397`, removed its profile and diagnostic root and left zero matching process, listener or runner directory.

The two causes were:

1. `.nav-item.active` selected the correct system `Highlight` and `HighlightText` values but retained `forced-color-adjust: auto`. Chrome could therefore remap the custom selected button again, producing the human-observed blank route-label rectangle. The selected item now uses `forced-color-adjust: none` only after all of its colours are explicitly mapped to system keywords. Its focus outline uses `CanvasText`, and the Alerts count uses `Highlight`/`HighlightText` with a visible system-colour boundary.
2. `.overview-kpis .summary-card` created four independent fully bordered cards, but the later generic `.summary-card:last-child` segmented-summary rule removed the inline-end boundary from the fourth card. Normal elevation could visually mask that missing edge; forced colours deliberately removes box shadows and exposed it. A more specific Overview-only rule now restores the existing card-border token on the last independent KPI without changing segmented summary cards.

No content, metric, state, icon, label, series or boundary was hidden to obtain the result.

## Regression coverage

The existing Dashboard CDP audit now records `24` forced-colour samples per locale/theme combination:

- eight canonical routes: Overview, Inventory, Alerts, Performance, History, Configuration, Providers and Settings;
- zoom at 100%, 200% and 400%;
- active forced colours and system Canvas/CanvasText/Highlight/HighlightText;
- non-remapped selected navigation, visible non-empty label, selected-route icon and system-colour Alerts count;
- distinct focus and exactly one `aria-current="page"`;
- all four boundaries of each of the four Overview KPIs;
- status boundaries, accessibility-tree names, chart containment and absence of horizontal overflow.

The runner uses the same existing disposable profile and bounded cleanup. Its final browser invocation also suppresses background networking and sends every non-loopback destination to the closed local proxy `127.0.0.1:9`, with an explicit bypass only for `127.0.0.1`. The pre-fix pass targeted only the loopback Dashboard and requested no external URL; explicit closed-proxy enforcement was added before the accepted final evidence.

## Automatic validation record

- Dashboard type-check: passed.
- Dashboard tests: `65/65` passed.
- Normal Dashboard build from installed dependencies: passed; `60` modules transformed.
- Chrome `150.0.7871.129` isolated matrix: `120` ordinary viewport samples and `96` forced-colour route/zoom samples passed across pt-BR/en-GB and Light/Dark.
- Final browser ports `23907`/`23908`: released; exact profile, evidence and diagnostic roots removed.
- Node.js `24.18.0` and npm `11.16.0`: verified.
- Design tokens, localisation, `11` provider identities/`22` variants and canonical brand assets: verified unchanged.
- Code-documentation gate: `316` comment-capable source files passed.
- Markdown-link gate: `586` local links in `128` files passed.
- Secret scan: current non-ignored worktree and available Git history passed.
- Node and PowerShell syntax, exact changed-file scope and `git diff --check`: passed.
- `dotnet format DBNotifier.sln --verify-no-changes --no-restore`: passed.
- Architecture tests excluding only the known R0 assertion: `43/43` passed.
- The owning architecture test reached the new closed-proxy assertions and then failed only because the workflow does not contain the fixed name `state05-dashboard-failure.json`. This is the exact pre-existing R0 contradiction; it was not changed or bypassed.

No restore, package installation, download, visible browser, common browser profile, external data source, Windows setting, notification, database, provider, credential, migration, CI, push or deployment was used.

## Preserved product truth

- `Enabled=false`, Unknown/Stale, route titles, complete IDs, explicit time zones and hostile-text validation are unchanged.
- Demonstration labelling and authoritative-snapshot separation are unchanged.
- Provider fallback, performance charts and the R6-G1 reflow correction remain intact.
- The visual `DBNotifier` naming observation was outside the authorised D02 decision and was not changed.
- R2-A, R3, R4-A, R4-B and R5 containments remain intact.
- The R5 NuGet metadata incident and the global R0 failure remain recorded and uncorrected.
- D03, W01 and W02 remain `BLOCKED`; P01 and each physical condition remain `NOT TESTED`.

## Decision boundary and next step

R6-FC1 is automatically approved only as a local remediation. It does not replace Bruno's factual decision that `R6-HV-D02` is rejected, does not accept R6 and does not resume another sample.

The next permissible step is a separate proposal and explicit authority to repeat visibly only `R6-HV-D02` against the R6-FC1 commit. Any successful automatic result remains insufficient to approve the human sample without that repetition and a new explicit human decision.
