# STATE-06 — Audit Remediation R6-G1 Performance Chart Report

- Date: 2026-07-21
- Baseline: `c3c8ed10493084fd4155a439f5f9cd044b8db23e`
- Lifecycle: `STATE-06 INTEGRATION`, unchanged
- Scope: clipped deterministic Performance chart reported by `R6-HV-D01`
- Automatic result: `APPROVED`
- Global architecture gate: `NON-GREEN — PRE-EXISTING R0 ASSERTION ONLY`
- Visible human repetition: `PENDING — NOT AUTHORISED BY R6-G1`

## Authority and boundaries

R6-G1 used only the separately authorised local remediation phase. The mandatory preflight observed the exact baseline, a clean worktree and zero DB-Notifier-owned runtime, listener or synthetic `ping.exe` process. It also found one stale, process-free Lighthouse profile created on 2026-07-14 under the exact project-owned temporary prefix; that exact 21,116-byte residue was removed before the R6-G1 runtime began.

The work used existing installed dependencies, headless Chrome with isolated temporary profiles, deterministic fixtures and loopback-only preview/sandbox hosts. No restore, installation, download, external request, ordinary browser profile, visible human sample, Windows configuration, notification, operational source, provider, database, credential, secret, migration, schema or operational data was used. WPF and Tray were not changed or opened.

## Root cause and negative proof

The Overview chart reserves a fixed total height that includes its padding. Its nested grid nevertheless retained the intrinsic minimum height of the `640×150` SVG plus the time-label row. The child content therefore exceeded the available content box and the owning card's deliberate `overflow: hidden` clipped the lower chart area.

The existing browser harness previously measured horizontal overflow but not vertical containment between the card, chart, plot, SVG and time labels. R6-G1 extended that same harness with bounded geometry and scroll measurements; no new harness was introduced.

Before changing the CSS, the new gate was run against the uncorrected Dashboard. It failed in `pt-BR`/Light with 30 viewports and reported all three expected defects:

- clipping in the 100%/200%/400% reflow matrix;
- clipping under modelled forced colours;
- clipping in the required TV layout widths.

The runner then proved complete cleanup: no audit process, listener on ports `43620`/`43621` or `DBNotifier-Dashboard-Runner-*` directory remained. The small sanitised failure summary was retained only until this factual result was recorded and was not committed as runtime output.

## Focused correction

The production correction adds `min-height: 0` to the existing `.trend-plot` grid item and its nested SVG. This lets both items surrender intrinsic SVG height to the existing `minmax(0, 1fr)` track while preserving the card size, full axes, both series, time labels and visible demonstration/source truth. No series, point, axis, label or content was hidden, removed or shortened.

The existing Dashboard browser audit now:

- measures vertical containment and scroll overflow for the card, chart, axis, plot, SVG and time labels;
- exercises Overview and Performance at 100%, 200% and 400% browser scale, plus 640 px and 320 px reflow equivalents;
- applies the same chart containment gate to modelled forced colours and TV layouts;
- fails if any chart layer escapes or is clipped by its owning card.

The existing authoritative TV browser sandbox now also requires zero `.trend-chart` elements and a non-empty source-truth explanation after every accepted authoritative snapshot. It separately requires the labelled demonstration chart to return in the standard local-fixture view.

## Automatic validation

- Dashboard TypeScript check: passed.
- Dashboard tests: 65/65 passed, including the focused intrinsic-height regression contract.
- Normal production Dashboard build: passed with 60 transformed modules.
- Chrome `150.0.7871.129` responsive audit: 120/120 viewport samples passed across `pt-BR`/`en-GB` and Light/Dark.
- The responsive audit covered 48 focused Overview/Performance chart samples; 16 TV chart layouts and eight forced-colour chart routes additionally passed vertical containment, for 72 chart-containment samples in total.
- Forced colours: 32/32 route samples passed; accessibility trees exposed 325–326 nodes with no unnamed interactive control gate failure.
- Authoritative TV browser sandbox: 12/12 scenarios passed over HTTPS loopback, with serial reads, bounded recovery, zero external HTTP request, no operational data, zero demonstration chart after authoritative acceptance and visible source truth.
- Node.js `24.18.0` and npm `11.16.0`: exact toolchain passed.
- Design tokens, localisation, brand assets and provider icon registries: deterministic verification passed.
- Architecture tests: 43/44 in the aggregate; the only failure is the unchanged R0 assertion `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`, still expecting `state05-dashboard-failure.json`. The explicit selection excluding only that known assertion passed 43/43.
- Code documentation, Markdown links, secret scan and staged-diff gates are recorded after the final documentation update.

## Final cleanup

The sandbox-specific Dashboard build was replaced by a normal production build after the authoritative test. Every owned Chrome, Node, .NET host and preview process ended within its runner deadline. The final audit found zero DB-Notifier process, synthetic `ping.exe`, owned listener, `DBNotifier-Dashboard-Runner-*`, `DBNotifier-DashboardTv-BrowserE2E-*`, `DBNotifier-R6-G1-*` or Lighthouse temporary root. Automated screenshots and browser profiles existed only inside the deleted runner roots. The ordinary browser, IDE and unrelated processes remained untouched.

## Preserved contracts

- Demonstration data remains visibly labelled in Overview, Performance and TV demonstration mode.
- Authoritative TV snapshots suppress all demonstration charts and retain explicit source truth.
- `Enabled=false`, Unknown/Stale, hostile-text validation, localised route titles, complete identifiers, explicit time zones, keyboard focus and Design System meanings are unchanged.
- R2-A, R3, R4-A, R4-B and R5 containments remain intact.
- The R5 NuGet metadata incident and the global R0 assertion remain explicit and uncorrected.
- Delivery, Agent Fleet, normal SignalR, provider loading, packaging, commands, R2-B, R7–R8, R7-A0, O1, AIOps modes, LLMs, recommendations, automation and lifecycle transition remain unavailable or unauthorised.

## Human evidence boundary and next decision

R6-G1 does not change Bruno's factual decision `AMOSTRA R6-HV-D01 REPROVADA: gráfico de desempenho cortado durante o reflow`. Automatic remediation evidence cannot replace that decision.

The next permissible step is a separate proposal and explicit authorisation to repeat only `R6-HV-D01` visibly on the remediated commit. The remaining forced-colours, authoritative TV and WPF/Tray visible samples may resume only after that focused human repetition is approved and under separately explicit authority. R6 remains unaccepted and no lifecycle transition is authorised.
