# STATE-06 — R6-UI1 Automatic Remediation Report

## Decision summary

- Authorised baseline: `321e04d83cdaeab6ad3a4bdedd929c60083788fc`
- Scope: WPF finish, visual uniformity and responsive reflow against the existing Web Dashboard reference
- Design System contract: `3.2.0`
- Implementation state: `COMPLETE`
- Automatic result: `APPROVED` within the restricted local R6-UI1 scope
- Focused commit: this report is committed with the completed increment; the identifier is recorded in the final hand-off
- Human sample `R6-HV-W01`: `REJECTED`; this automatic result does not replace or infer a human decision
- `R6-HV-W02`: `BLOCKED`
- `R6-HV-P01`: `NOT TESTED`

R6-UI1 corrects the presentation defects recorded after R6-WPF2. It does not approve W01, accept R6, resume W02/P01 or change lifecycle state. Repeating W01 visibly requires a separate proposal and explicit authorisation.

## Authority and preserved boundaries

The increment changes only the secondary WPF Presentation surface, reusable WPF components introduced by R6-WPF2, the existing WPF auditor, directly related architecture tests and factual documentation. The Web Dashboard remains the normative visual reference and has no tracked source change. Route identifiers, data contracts, deterministic fixture facts, persisted product state, dependencies and lockfiles remain unchanged.

No operational source, provider integration, notification, command, administrative execution, database, secret, migration, restore, installation, download, external access, remote CI, push or deployment was used. Normal delivery, Agent Fleet, SignalR, provider loading, packaging and command execution remain disabled or unavailable as previously recorded.

## Factual causes

Inspection of the rejected post-R6-WPF2 sample and the original implementation established these presentation causes:

- the status pill used an effectively infinite corner radius and an unbounded horizontal measurement path, producing elliptical shapes and allowing long factual text to escape its intended field;
- the provider ring combined fixed minimum widths with insufficient responsive insets, allowing the plot to meet or cross its card boundary at constrained widths;
- the eight WPF routes retained desktop-only grids and tables at `820×620`, compressing fields instead of changing to a single readable flow;
- the native `DataGridCell` template did not propagate its generic `Padding` setter into every generated content presenter, so adjacent values could visually concatenate despite a declared padding value;
- provider identifiers used character ellipsis in constrained cells, which hid part of an authoritative identifier;
- the previous auditor proved presence and approximate containment but did not measure the visible text inside each pill, real cell gutters, the actual ring/card rectangles or the visible portion of a focused scrollable table.

These were Presentation defects only. They did not require a contract, fixture, provider, persistence or operational-authority change.

## Implemented correction

### Status and provider components

- Status pills now use the canonical six-DIP control radius, bounded grid measurement, consistent horizontal/vertical padding and wrapping only when the available field genuinely requires it.
- The complete localised state remains exposed once to assistive technology. Raw-only diagnostic peers expose label and pill rectangles without duplicating readable content.
- Unsupported semantic icon values fail closed to `Unknown`.
- The provider distribution component reflows its ring and legend, preserves the complete circle and uses positive inset on all four card edges.
- Raw-only card and ring peers expose exact geometry to the auditor while the visible provider legend remains the authoritative accessible content.
- Provider identifiers wrap without character ellipsis; registered PostgreSQL, MySQL and MongoDB assets and the neutral SQL Server fallback remain unchanged.

### Responsive route composition

- At `1180×760`, the eight routes retain a recognisable desktop organisation with proportional columns, complete text, real gutters and aligned headers/content.
- At `820×620`, the shell uses one vertical page flow. Inventory, Alerts, History, non-secret configuration and capability evidence switch from rigid columns to complete stacked records.
- The compact layout has no page-level horizontal scroll. It retains provider, support, environment, state, exact time, latency, summary and reason fields in visual and accessibility reading order.
- Overview rows, KPI groups, paired panels, provider catalogue cards, Preferences panels and configuration controls reflow without hiding content.
- The WPF native caption, work-area placement, DPI behaviour, application scrollbars and one-task-stop read-only DataGrid semantics remain native adaptations rather than copied browser behaviour.

The twelve-DIP desktop cell gutter is the WPF `Space3` value. It is applied to generated cell content because the native DataGrid template does not reliably template-bind a `Thickness` from the cell's `Padding`; this is the documented narrow platform exception and not a parallel token set.

## Strengthened automatic evidence

The existing `audit-state05-wpf.ps1` runner was extended; no new harness was introduced. Its R6-UI1 matrix now proves:

- complete pill text lies inside the corresponding non-elliptical pill;
- first and last desktop cell gutters are positive and adjacent fields have a measured gap;
- compact tables expose one stacked field set, no visible headers and no horizontal scrolling;
- the provider ring lies inside the actual card rectangle with positive margins on all four sides;
- focus remains owned, not off-screen and fully contained for bounded controls;
- a focused scrollable DataGrid has a meaningful visible intersection of at least one row while retaining bounded vertical scrolling;
- required fields, route order, localisation, source truth, disabled-health separation, unknown/stale fail-closed semantics and exact time-zone labels remain present;
- every process and temporary state root is cleaned and the original preference bytes are restored in `finally`.

At `1180×760`, measured first/last gutters are 12/12 pixels for Inventory, Alerts and History and 12/13 pixels for Configuration/Capability; minimum adjacent-content gaps are 24 and 25 pixels respectively. Ring-to-card margins are positive in every direction: Overview `L19/T97/R164/B49`, Providers `L23/T87/R705/B42`. Compact focused DataGrids expose a 475-pixel visible intersection against required row minima of 46–52 pixels.

## Automatic evidence register

| Gate | Result |
|---|---|
| Baseline and scoped diff | `PASS` — exact baseline confirmed; Web, project manifests, dependencies and lockfiles have zero diff |
| Release solution build | `PASS` — offline `--no-restore`, 18 projects, 0 warnings and 0 errors |
| WPF tests | `PASS` — 3/3 |
| Focused Presentation tests | `PASS` — 66/66 |
| WPF architecture contracts | `PASS` — 15/15 |
| Full architecture suite | `47/48` — only the unchanged global R0 assertion fails as recorded below |
| WPF visual/reflow matrix | `PASS` — 64/64 executable route samples; 32 physical `1920×1080` samples remain `NOT TESTED` |
| Dashboard regression | `PASS` — exact installed Node.js 24.18.0/npm 11.16.0, type-check, 66/66 tests and production build |
| Coverage | `PASS` — 393/393 tests, 81.92% lines, 53.56% branches and 10 required components |
| Generated tokens, brand, providers and localisation | `PASS` — 11 provider identities and 22 theme variants retained |
| PowerShell compatibility | `PASS` — auditor parses under PowerShell 7 and Windows PowerShell 5.1 |
| Formatting, comments, links and secret scan | `PASS` — repository gates completed without a secret finding or formatting change |
| Preference restoration | `PASS` — before/after SHA-256 exactly `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F` |
| Cleanup | `PASS` — zero owned process, listener, state root or coverage residue |
| Independent read-only review | `PASS` — no material finding after the final matrix and capture inspection |

The final matrix covers eight routes, pt-BR/en-GB, Light/Dark and `820×620`/`1180×760`: 64 executed route samples passed and none failed. The 32 `1920×1080` combinations remain `NOT TESTED` because the active Windows work area is `1920×1032`; no Windows setting was changed.

The known global R0 failure remains exactly `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`: the assertion expects the pre-existing literal `state05-dashboard-failure.json`. No bypass, exclusion or correction was applied. The R5 NuGet metadata incident also remains recorded; this increment performed no restore, download or metadata access.

## Completion disposition

The authorised defects are corrected and automatically evidenced: pills are bounded and rectangular, disabled text remains contained, the provider ring is complete, desktop tables have real gutters, all eight routes reflow at both supported dimensions, compact records retain every factual field, page-level horizontal scrolling is absent at the minimum size and focus remains visible. Web source, dependencies and lockfiles remain unchanged.

R6-UI1 is therefore automatically complete within its restricted scope. `R6-HV-W01` remains rejected until a separately authorised visible repetition produces a new explicit human decision. W02 remains blocked, every P01 physical condition remains not tested, the R0 and R5 records remain intact, and no R6 acceptance, R7/R8 work, O1/AIOps implementation or lifecycle transition is authorised.
