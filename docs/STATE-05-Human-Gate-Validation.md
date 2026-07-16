# STATE-05 Human Gate Validation

## Gate status

`PENDENTE`

This document prepares the required human validation for `STATE-05 FRONTEND_IMPLEMENTATION`. It is neither an approval nor evidence that an unperformed sample passed. The validator must record their own name, date, observations and decision after operating the interfaces.

No transition to `STATE-06` is authorised by this document.

## Scope

The human sample covers:

- the React Dashboard in Brazilian Portuguese and British English;
- the WPF Desktop/Tray shell in Brazilian Portuguese and British English;
- explicit Light and Dark preferences plus independent Windows High Contrast behaviour;
- keyboard-only operation and visible focus;
- Narrator reading order, names, states and announcements;
- native browser zoom, Windows scaling and High Contrast where the environment permits;
- operational truth for stale data, planned providers and unavailable administrative execution.

Mobile and tablet samples apply to the responsive Dashboard Web. There is no native mobile application in this phase. Dashboard TV presentation is session-only and distinct from standard desktop/ultrawide responsiveness; it does not prove external real-time ingestion, unattended kiosk provisioning or WPF TV support. WPF remains a Windows desktop interface with a supported minimum window size of `820×620` DIP.

## Automatic evidence to review first

| Evidence | Current result |
|---|---|
| Design System implementation report | Design System `2.6.13` translation-icon, Light/Dark-only, one canonical database-and-bell geometry at every generated size, transparent availability PNG registered as `DB Notifier` through a fail-safe Windows app-notification publisher, confirmation requested on every explicit `CloseRequest`, state-specific favicon replacement, typed per-notification green/yellow/red/grey selection, shared eight-item navigation, operational Overview, TopBar actions, ultrawide, TV-presentation, system-local time, responsive contracts, typed WPF markup and two-column operational-Tray implementation; complete automatic revalidation belongs to the implementation report, while the commit identifier belongs to the implementation hand-off |
| Dashboard matrix | 96 Design System `2.6.13` pt-BR/en-GB × Light/Dark × viewport/route samples across 24 viewports plus four recorded TV interactions; no global overflow or unnamed interactive control; all eight routes passed their applicable sampled sizes, and every current headless sample selected the fixture's Critical favicon |
| Lighthouse matrix | Thirty-report three-run baseline plus 16 current Design System `2.3.0` reports covering all eight routes in mobile/desktop; current Accessibility/Best Practices `100` in `16/16`, Performance `99`–`100`; valid internal-console crawler policy; SEO `66` by intentional non-crawlability |
| WPF matrix | Eight current Design System `2.3.1` locale/theme/size samples: four at `1180×760` and four at `820×620`, each with 16 visible focusable controls, none unnamed and 12 sampled Tab steps contained; earlier scaling/High Contrast samples remain separately limited |
| WPF High Contrast/scaling | High Contrast responded to the real Windows flag in two technical samples; 125%/120 DPI and 150%/144 DPI minimum-window samples passed automatic checks; 200% was not offered by the active `1920×1080` display; human confirmation remains pending |
| Automated tests | 34 Dashboard, 143 .NET unit/model/provider/presentation and 9 architecture tests approved after `2.6.13`; 11 legacy compatibility tests remain the last recorded complete compatibility result. Solution Release, Dashboard typecheck and Vite production build also passed. Automatic evidence remains separate from human acceptance |
| Security/dependencies | npm and NuGet reported no known vulnerabilities in the recorded audit |

Primary automatic evidence:

- [`STATE-05-Design-System-Implementation-Report.md`](STATE-05-Design-System-Implementation-Report.md)
- [`STATE-05-Localisation-Implementation-Report.md`](STATE-05-Localisation-Implementation-Report.md)
- [`STATE-05-Frontend-Implementation-Reaudit.md`](STATE-05-Frontend-Implementation-Reaudit.md)
- [`STATE-05-Lighthouse-Audit.md`](STATE-05-Lighthouse-Audit.md)
- [`STATE-05-WPF-Accessibility-Audit.md`](STATE-05-WPF-Accessibility-Audit.md)

## Validation environment preflight

Observed on 2026-07-13 before the human sample:

| Item | Observed value |
|---|---|
| Operating system | Microsoft Windows 11 Enterprise `10.0.26200` (`AMD64`) |
| .NET SDK | `10.0.301` |
| Node.js | `v24.18.0` |
| Chrome | `150.0.7871.115` |
| Windows Narrator | Available |
| NVDA | Not installed/discoverable; Narrator is the available reader |
| Current Windows scale | 100% (`96` DPI) |
| Current High Contrast state | Off |

The preflight proves tool availability only. It does not prove screen-reader usability, scaling behaviour or visual acceptance.

## Safety and evidence rules

- An agent or automation MUST NOT start Narrator/NVDA, open a visible validation browser/application, or change zoom, scaling, theme or High Contrast unless the validator has separately confirmed that exact interactive action.
- General continuation words such as “continue/seguir” or “approved/aprovado” do not by themselves authorise an accessibility tool or operating-system preference change.
- Use only the local deterministic demonstration data. Do not connect to an Agent, API, database, identity provider, credential store or administrative executor.
- Do not enter credentials, connection strings, host names or production identifiers.
- Do not capture unrelated windows, notifications, Narrator history or personal desktop content.
- Stop the local Dashboard preview, Chrome validation profile and WPF process after sampling.
- Restore browser zoom, Windows scaling, theme and High Contrast to their original values after each sample.
- Record a failure exactly as observed; do not approve around an accessibility defect.

## Dashboard human protocol

### Start the local sample

From `src/DBNotifier.Dashboard.Web`:

```powershell
npm run build
node.exe node_modules/vite/bin/vite.js preview --host 127.0.0.1 --port 4173
```

Open `http://127.0.0.1:4173/` in a new independent Chrome window, not as another tab in an existing window. Do not navigate, reuse or rearrange existing user windows or tabs. Use the visible TopBar buttons to select the required locale and theme. Start or stop Narrator with `Windows+Ctrl+Enter` only when ready to listen to the sample.

### Keyboard and Narrator tasks

Repeat the critical path once in `pt-BR`/Light and once in `en-GB`/Dark:

1. Start before the page content and press `Tab` through the skip link, language buttons, theme buttons, navigation and scenario control.
2. Confirm that focus is always visible and follows the visual order without becoming trapped.
3. Confirm that Narrator announces each language/theme control, its group and selected/pressed state.
4. Visit Inventory, History, Alerts and Configuration; confirm one main heading per view and meaningful navigation-current feedback.
5. In Inventory, exercise loading, empty, offline, error, denied and maintenance scenarios. Confirm distinct titles/messages and no colour-only distinction.
6. Confirm that stale status includes visible text/timestamp and is not announced as current healthy data.
7. Open the confirmation example. Confirm initial focus enters the dialogue, forward/reverse Tab remains inside, Escape closes it and focus returns to its trigger.
8. Confirm that planned/unhomologated providers remain explicitly labelled and that no administrative execution appears available.

### Native zoom and compact layout

1. Set Chrome page zoom to 200% through the browser menu.
2. Repeat Inventory navigation and the confirmation dialogue at approximately 1280×800 physical window size.
3. Resize to compact widths represented by 390 and 320 CSS pixels where practical.
4. Confirm that content reflows without document-level two-dimensional scrolling, overlap, clipped preference buttons or hidden focus.
5. Restore Chrome zoom to 100%.

## WPF human protocol

### Start the local sample

From the repository root:

```powershell
dotnet build .\src\DBNotifier.Desktop.Wpf\DBNotifier.Desktop.Wpf.csproj -c Release --no-restore
Start-Process .\src\DBNotifier.Desktop.Wpf\bin\Release\net10.0-windows10.0.22621.0\DBNotifier.Desktop.Wpf.exe
```

Use the visible TopBar buttons to select the locale and theme. No database or service process is controlled by this shell.

### Keyboard and Narrator tasks

Repeat the critical path once in `pt-BR`/Light and once in `en-GB`/Dark, at the default size and `820×620` DIP minimum:

1. Press `Tab` and arrow keys through language/theme groups, view/scenario selectors, grids and visible actions.
2. Confirm visible focus and that Narrator announces the selected locale/theme, selector names, grid names, column headers and meaningful cell values.
3. Confirm Inventory summary counts and stale data are understandable without colour.
4. Visit History/Alerts and Configuration; confirm read-only/unsupported/denied wording remains explicit.
5. Open each safe preview dialogue and confirm title, message, button order and focus return.
6. Minimise/close to Tray, open the notification-area fleet flyout and confirm the current locale/theme, four demonstration instances, textual status, source truth and local snapshot label remain understandable.
7. Use each safe flyout shortcut, dismiss with Escape and focus loss, and confirm Restart is explanatory/non-interactive rather than executable.
8. Exit through the flyout and confirm no DB-Notifier process remains.

### Two-cycle close-to-Tray notification protocol

This focused protocol belongs to pending `S05-HG-010`; it does not repeat or reopen the approved notification-area-first hierarchy of `S05-HG-011`.

1. Obtain explicit consent to open WPF, then start one fresh Review process. Clear or distinguish older DB Notifier Notification Centre snapshots before judging the new icon; do not delete unrelated notifications.
2. Open the secondary WPF shell through the safe local flyout shortcut or the authorised `--show-desktop` audit argument. Record which route was used.
3. Explicitly close the shell with its Close button. Do not minimise it. Confirm that Windows presents, or record that it suppressed, the first fresh availability confirmation attributed to `DB Notifier`.
4. Inspect only the newly delivered entry. Confirm that its small application mark reads as an open database on a transparent icon canvas with a green bell, while recognising that the notification card background, attribution scale and cache belong to Windows.
5. Reveal the same local secondary shell by activating the notification's `action=show` path when Windows exposes it. If the banner was suppressed, use the safe Tray/flyout shortcut instead and record that substitution. No database, provider, service or external action may occur.
6. Explicitly close the shell a second time. Confirm that a second fresh publication is requested and, when Windows displays it, inspect the new banner again. The stable tag/group is expected to replace the earlier current availability entry rather than accumulate two stale cards in Notification Centre.
7. As negative controls, verify that hidden startup, Show and minimise do not request this confirmation. Exit only after the visual sample and confirm that Exit itself does not request another notification.
8. Record each cycle separately as `VISÍVEL`, `SUPRIMIDO/BLOQUEADO` or `FALHOU`, including whether the displayed name/icon matched. A successful publication call is not proof of a visible banner: Windows policy and Focus Assist may suppress it. Do not change Focus Assist or notification settings without separate consent.

## Theme stability, High Contrast and scaling protocol

These samples change user display preferences and must be performed interactively by the validator. Do not automate them silently.

1. Select Light and then Dark in each interface. Change Windows application mode Light → Dark → Light and confirm the explicit DB Notifier selection remains stable without a restart; there is no selectable System theme.
2. Enable Windows High Contrast/Contrast Themes. Confirm WPF and Dashboard retain readable text, borders, selected state and focus; verify status meaning remains textual.
3. Disable High Contrast and confirm the selected semantic preference returns.
4. Sample Windows scaling at 125%, 150% and 200% where the display environment permits. At each scale, inspect WPF default and minimum sizes for clipped labels, overlapping controls and inaccessible scroll regions.
5. Restore the original 100% scaling, Light/Dark system setting and High Contrast Off state.

## Visual and operational-truth review

The validator must confirm that:

- the identity remains modern, calm, professional and restrained in both themes;
- there is no neon, glassmorphism, excessive glow or decorative motion;
- the TopBar language/theme controls are discreet, legible and correctly positioned;
- information hierarchy, density and spacing remain usable at desktop, tablet and compact Web sizes;
- planned, implemented, homologated, unsupported, unavailable, denied and stale remain distinct facts;
- administrative controls do not imply executable capability.

## Sample record

Do not mark a row until the named human validator has performed it.

| ID | Human sample | Validator result | Sanitised evidence/notes |
|---|---|---|---|
| `HG05-01` | Dashboard `pt-BR` Light, keyboard and Narrator | SHELL VISUAL APROVADO — ÍCONE/TECLADO/NARRATOR PENDENTES | The human reviewer accepted the second shell refinement, rejected the first filled database mark and has not yet reviewed its outlined replacement. Keyboard and Narrator were not performed. |
| `HG05-02` | Dashboard `en-GB` Dark, keyboard and Narrator | PENDENTE | |
| `HG05-03` | Dashboard native 200% zoom and compact reflow | 390 PX APROVADO — 320 PX/200% PENDENTES | Bruno approved the visible `390×844` Alert/Configuration sample on 2026-07-14; minimum width and native 200% zoom remain unreviewed. |
| `HG05-04` | WPF `pt-BR` Light, default/minimum, keyboard and Narrator | PENDENTE | Default/minimum UI Automation passed; the validator could not perform Narrator. |
| `HG05-05` | WPF `en-GB` Dark, default/minimum, keyboard and Narrator | PENDENTE | Default/minimum UI Automation passed; Narrator was not repeated in this combination. |
| `HG05-06` | Explicit Light/Dark stability across Windows application-mode changes | PENDENTE | |
| `HG05-07` | Windows High Contrast on both interfaces | PENDENTE | Real Windows flag and WPF override were observed and restored automatically; Dashboard and human confirmation remain pending. |
| `HG05-08` | WPF Windows scaling at 125%, 150% and 200% where permitted | PENDENTE | Effective 120/144 DPI proved automatically; 200% was unavailable on the active display; human confirmation remains pending. |
| `HG05-09` | Visual hierarchy and operational-truth review | PENDENTE | |
| `HG05-10` | Dashboard TV mode at `1920×1080`, Fullscreen entry/exit and viewing distance | PENDENTE | |

## Recorded human finding

### `S05-HG-001` — Dashboard visual identity does not meet the enterprise Design System

- Date observed: 2026-07-13.
- Source: explicit user review of the local `pt-BR` Dashboard in the active Human Gate session, supported by a sanitised screenshot supplied in the conversation and not committed to the repository.
- Classification: blocking Human Gate visual finding.
- Human feedback: the interface was judged visually unattractive and not modern or enterprise-grade.
- Corroborating review: the sampled screen has an abrupt Light-header/Dark-content split, heavy outlined preference controls, compressed side navigation, excessive borders, weak density/hierarchy balance and a crowded data region at the observed viewport.
- Scope not tested: Narrator announcements, complete keyboard path, dialogue focus and remaining Human Gate samples were not performed after the visual rejection.
- Required remediation: revise the shared shell, typography, spacing, navigation, preference-control treatment, surface hierarchy and responsive data presentation in the canonical token/component sources; then repeat automatic contrast/responsive/accessibility gates and restart `HG05-01`.
- Remediation status: IMPLEMENTED AND AUTOMATICALLY RE-AUDITED on 2026-07-13; human revalidation remains pending.
- Remediation evidence: Design System `1.3.0` adds cohesive shell chrome; the Dashboard changes to horizontal labelled navigation and complete cards before content compression; WPF uses the same chrome and restrained selected states. The affected automatic matrix passed in 60 browser samples and seven WPF samples with no global overflow or unnamed focusable/interactable controls.
- First remediation feedback: the human reviewer explicitly judged the result better, but requested further refinement inspired by international market software; this was not recorded as visual acceptance.
- Second remediation evidence: Design System `1.3.1` applies a reviewed Carbon/Grafana/Fluent pattern synthesis through a consolidated metric band, coherent SVG iconography, quieter selected navigation/preferences and reduced competing card chrome. The repeated 60-sample browser matrix passed; representative WPF Light/Dark samples retained zero unnamed focusable controls.
- Human closure: the reviewer answered “Sim!” to the explicit visual-approval question on 2026-07-13. This closes the visual identity finding only; it does not approve keyboard, Narrator or the overall Human Gate.
- Follow-up identity request: use a simple database image as the icon throughout the active system. Design System `1.3.2` implemented the first generated provider-neutral cylinder across Web, WPF, executable, Tray and installer surfaces, but the reviewer explicitly rejected that filled mark as unattractive.
- Lifecycle impact: `STATE-05` remains active; the Human Gate cannot be approved until the pending accessibility and remaining samples are completed.

### `S05-HG-002` — First canonical database mark is visually unattractive

- Date observed: 2026-07-13.
- Source: explicit user review of the generated filled-cylinder database mark in the active Human Gate session.
- Classification: blocking brand checkpoint; it does not reopen the accepted shell hierarchy.
- Human feedback: the database icon was judged unattractive.
- Remediation: Design System `1.3.3` replaces the heavy filled cylinder with a balanced white outline, rounded three-pixel strokes and three clearly separated database levels on the same canonical brand background.
- Automatic evidence: SVG/ICO drift verification, Dashboard tests/build and WPF build/runtime sampling apply to the same generated asset family.
- Human closure: PENDENTE; only review of the outlined replacement may close this finding.
- Lifecycle impact: Narrator remains off and the Human Gate remains pending.

### `S05-HG-003` — Fullscreen Dashboard leaves an inactive strip and uses the wrong visual product spelling

- Date observed: 2026-07-13.
- Source: explicit user review of the fullscreen `pt-BR` Dashboard, supported by a sanitised screenshot supplied in the conversation and not committed to the repository.
- Classification: blocking visual layout and identity checkpoint; it does not reopen the previously accepted shell hierarchy.
- Human feedback: the main content left excessive empty space on the right in fullscreen, and visible product text must use `DB Notifier`.
- Remediation: Design System `1.3.4` removes the fixed `1640` CSS px main-region limit, stretches operational content across the available shell width and defines `DB Notifier` as the visual display name while preserving technical paths and identifiers.
- Automatic evidence: 66 browser samples passed. All six locale/theme combinations at `1920×1080` measured a zero-pixel main-region right gap, a 48-pixel content inset, no global overflow and no unnamed interactive control. WPF `pt-BR` Dark exposed the window name `DB Notifier — Inventário`, 37 focusable controls and none without a name.
- TV scope at remediation time: no dedicated mode existed in Design System `1.3.4`; this historical limitation is superseded by the separately recorded `S05-HG-005` implementation and does not turn the ultrawide correction itself into TV support.
- Human closure: PENDENTE; the visible fullscreen correction and display name still require reviewer confirmation.
- Lifecycle impact: Narrator remains off and the Human Gate remains pending.

### `S05-HG-004` — Expanded language and theme groups should become compact icon controls

- Date observed: 2026-07-13.
- Source: explicit user review of Dashboard and WPF TopBar screenshots in the active Human Gate session.
- Classification: visual density and interaction refinement; it does not approve the existing pending brand, layout or accessibility checkpoints.
- Human feedback: replace the expanded `pt-BR`/`en-GB` and System/Light/Dark controls with one language-image button and one theme-image button using System, sun and moon states.
- Remediation: Design System `1.3.5` uses one generic language icon button cycling `pt-BR` ↔ `en-GB` and one monitor/sun/moon button cycling System → Light → Dark in React and WPF. Localised accessible names and tooltips expose the current and next states.
- Automatic evidence: 66 browser samples passed with no global overflow or unnamed interactive control. All six locale/theme combinations completed and restored both preference cycles in Web and WPF; every WPF sample exposed 34 focusable controls and found none without a name.
- Human closure: PENDENTE; the compact controls require visual confirmation in the visible Dashboard preview.
- Lifecycle impact: Narrator remains off and the Human Gate remains pending.

### `S05-HG-005` — Add a dedicated Dashboard TV mode for continuous monitoring

- Date observed: 2026-07-13.
- Source: explicit user request in the active Human Gate session.
- Classification: new Dashboard presentation capability; it does not authorise integration or imply a live external source.
- Human request: include TV mode for real-time Dashboard monitoring with one expand/collapse icon button.
- Implementation: Design System `1.4.0` adds a session-only Dashboard Web TV mode. The button enters the ready, unfiltered inventory, requests browser Fullscreen, hides navigation/scenario/filters, enlarges metrics/table and remains visible as the exit control. Escape from established Fullscreen also restores the standard shell.
- Operational truth at the `1.4.0` implementation checkpoint: the UTC clock and freshness evaluation updated continuously, while the snapshot remained demonstration-only. `S05-HG-007` supersedes only the visible clock presentation with system-local time and an explicit zone label. Real API/SignalR ingestion and live-source health belong to `STATE-06` and are not claimed here.
- Automatic evidence: 66 standard browser samples plus six `1920×1080` TV samples passed. Every locale/theme combination entered native Fullscreen, retained the demonstration badge and footer truth, exposed the collapse control, hid navigation/filters, displayed the table, exited and restored the standard shell with no global overflow or unnamed interactive control. A separate unavailable-Fullscreen sample kept TV active, retained the exit control and announced the limitation.
- Human closure: PENDENTE; viewing distance, visual density and enter/exit behaviour require confirmation in `HG05-10`.
- Lifecycle impact: Narrator remains off and the Human Gate remains pending.

### `S05-HG-006` — Clarify the language icon and remove the System theme option

- Date observed: 2026-07-13.
- Source: explicit user review during the active Human Gate session.
- Classification: global preference-contract and visual-clarity change; it supersedes the selectable theme/icon portion of `S05-HG-004` without rewriting its historical evidence.
- Human feedback: the globe-style language icon is confusing; retain only Dark and Light and remove the System theme option.
- Remediation: Design System `2.0.0` replaces the globe with a multi-script translation/languages symbol in React and WPF. Theme preference now cycles only Light ↔ Dark with sun/moon icons; invalid or retired System persistence migrates safely to Light. Windows High Contrast remains an independent accessibility override.
- Automatic evidence: 22 Dashboard tests, 133 .NET tests and both builds passed. Forty-four standard browser samples plus four TV samples covered the four current locale/theme combinations with no global overflow or unnamed interactive control. Four WPF combinations completed and restored both two-state cycles, each exposing 34 focusable controls and none unnamed. A focused Chrome visual sample confirmed the new icon, localised names and console without errors.
- Human closure: PENDENTE; the reviewer must confirm that the replacement icon is visually clear and that the two-state theme control is accepted.
- Lifecycle impact: Narrator remains off and the Human Gate remains pending.

### `S05-HG-007` — Use system-local time and retain global controls across TV and mobile layouts

- Date observed: 2026-07-13.
- Source: explicit user review of the visible local Dashboard in Light mode, supported by sanitised screenshots supplied in the conversation and not committed to the repository.
- Classification: blocking responsive and presentation-consistency finding.
- Human feedback: the displayed date/time appeared incorrect because the UI forced UTC rather than the system-local time; TV mode must retain language and theme controls; compact mobile TopBar icons require alignment refinement; future Chrome reviews must open in a new independent Chrome window rather than another tab in an existing window.
- Root cause: Dashboard instants originated from the system clock but `App.tsx` forced every visible formatter to UTC; TV CSS explicitly hid the language/theme selectors; the `620` CSS px breakpoint forced all TopBar controls into a separate full-width row.
- Remediation: Design System `2.1.1` renders exact instants in the browser system time zone with an explicit short zone label while retaining ISO/UTC machine values and freshness calculations. TV mode keeps language, theme and expand/collapse controls visible. The `320`–`620` CSS px TopBar keeps the shortened brand and three `44×44` controls in one contained row.
- Automatic evidence: 27 Dashboard tests, typecheck and production build passed. The repeated 44-sample browser matrix passed across `pt-BR`/`en-GB`, Light/Dark and 11 viewports. The `320×568` and `390×844` samples kept the TopBar in one contained row without horizontal overflow. All four TV samples retained language/theme controls, entered/exited Fullscreen and reported their browser system time zone; the local `pt-BR`/Light evidence resolved `America/Sao_Paulo` and displayed `13/07/2026, 22:54:43 BRT` for the corresponding UTC instant.
- Human closure: PENDENTE; system-local time, TV preferences and the compact TopBar require visible reviewer confirmation.
- Lifecycle impact: the Human Gate remains pending; this remediation does not authorise Narrator, WPF, High Contrast, scaling or `STATE-06` integration.

### `S05-HG-008` — Alert and administrative-capability cards are compressed on mobile

- Date observed: 2026-07-13.
- Source: explicit user review of compact Dashboard Alert and Configuration views, supported by sanitised screenshots supplied in the conversation and not committed to the repository.
- Classification: blocking compact-reflow and readability finding.
- Human feedback: the mobile presentation requires correction; alert cards and administrative controls appeared in narrow parallel columns, producing excessive wrapping, clipped technical reason codes and poor use of the available width.
- Root cause: the tablet rule changed both collections from three columns to two at `1100` CSS px, but the compact `760` CSS px breakpoint did not reset them to one column. Long timestamps and capability reason codes also lacked local wrapping safeguards.
- Remediation: Design System `2.1.2` requires one full-width column for these operational collections below `768` CSS px. Dashboard CSS now resets both grids at the compact breakpoint, removes fixed alert-summary height pressure and safely wraps timestamps, provider identifiers and capability reason codes without changing their text.
- Automatic evidence: 28 Dashboard tests, typecheck and production build passed. The browser matrix expanded to 52 samples across `pt-BR`/`en-GB`, Light/Dark and 13 routes/viewports. Alert and Configuration samples at `390×844` and `320×568` each measured one card column with no card-content or document-level horizontal overflow.
- Human closure: PARCIALMENTE APROVADO; Bruno explicitly approved the visible `390 px` sample on 2026-07-14. The `320 px` minimum-width sample remains pending, so this finding is not closed.
- Lifecycle impact: `HG05-03` and the overall Human Gate remain pending; automatic reflow evidence is not a human visual approval.

### `S05-HG-009` — Alert summary leaves unused columns at narrow-desktop width

- Date observed: 2026-07-14.
- Source: incidental human review of the dedicated local Dashboard window after the `390 px` approval, supported by a sanitised screenshot supplied in the conversation and not committed to the repository.
- Classification: responsive visual-density finding separate from the approved `390 px` card flow.
- Human feedback: the Alert summary displayed a conspicuous unused region to the right of its three visible metrics.
- Root cause: at `1100` CSS px or below, the general Inventory summary rule replaced the Alert-specific three-column grid with five columns. The Alert view supplied only three metrics, leaving two empty grid tracks; its historical `760 px` maximum also prevented the band from using the full operational width.
- Remediation: Design System `2.1.3` requires feature summary bands to use their owning width and match their grid tracks to visible metrics. The Alert band now has no fixed maximum and explicitly retains three columns through the narrow-desktop breakpoint before reflowing to one column below `768` CSS px.
- Automatic evidence: 29 Dashboard tests, typecheck and production build passed. The matrix expanded to 56 standard samples, including `alerts-narrow-desktop-960x1040` in every locale/theme pair; each sample measured three cards, three grid columns, a zero-pixel parent right gap and no document overflow.
- Human closure: PENDENTE; the corrected narrow-desktop Alert summary requires visible reviewer confirmation.
- Lifecycle impact: the overall Human Gate remains pending; the incidental discovery does not invalidate the approved `390 px` sample.

### `S05-HG-010` — Operational Overview and two-column notification-area flyout

- Date requested: 2026-07-14.
- Source: explicit user request to make the product work and look like the supplied DB Notifier reference while incorporating the functionality already present.
- Classification: blocking visual/product-pattern confirmation; not a request to fabricate external monitoring or administrative support.
- Interpreted target: dark navy enterprise shell, Overview-first navigation, compact metric band, fleet status with small trends, recent alerts, performance trend, provider distribution and a compact two-column Tray flyout. Vendor logos, claimed online providers, external notifications and enabled service control are excluded until their owning phases prove them.
- Implementation: React and WPF now open on the operational Overview, built from the existing deterministic inventory/timeline fixture. Dashboard TV uses the same Overview. Dashboard navigation follows the requested eight-item order and adds internal TopBar alerts/Settings actions. Tray status rows occupy the left column and safe Dashboard/Configuration/log actions occupy the right; Restart and Silent Mode remain unavailable and non-interactive.
- Automatic evidence: 32 Dashboard tests, production Web/WPF builds, 72 responsive samples and 30 clean Lighthouse reports passed. The first Lighthouse run exposed `4.03:1` contrast in the trend-axis labels; the semantic token was corrected and the complete 30-report matrix was repeated with Accessibility `100` in `30/30` and all Performance medians at `100`.
- Human closure: PENDENTE; the Overview and Tray must be reviewed visibly in the requested locale/theme combinations. Automatic evidence does not establish visual equivalence or usability of Windows notification-area activation.
- Lifecycle impact: `STATE-05` and its Human Gate remain pending. Real API/Agent refresh and change notifications remain `STATE-06`; service Start/Stop/Restart remains dependent on exact implementation and homologation in `STATE-07`.
- First visible review result: NOT APPROVED. The reviewer reported missing notification/settings TopBar actions, an incorrect wordmark, incomplete navigation order, missing provider marks/status emphasis, incorrect Overview KPI names/card treatment, insufficient Recent Alerts icons, generic chart titles/appearance, an unsatisfactory system mark and unreliable notification-area flyout activation.
- Remediation increment: Design System `2.3.0` defines the eight-item navigation order, internal TopBar notifications/settings navigation, separate Total Instances/Healthy/Warning/Critical cards, recognisable provider glyphs beside visible text, semantic alert icons including Restarted, Performance/Providers panels, the green-accent `DBNotifier` visual wordmark with accessible name `DB Notifier`, and a generated database-and-bell product mark. WPF TopBar parity and direct left/right notification-icon flyout activation were added; Restart and Silent Mode remain visibly unavailable.
- Remediation evidence: 32 Dashboard tests, typecheck/build, .NET build/tests, token/localisation/brand generation checks, 96 responsive samples and 16 current mobile/desktop Lighthouse reports passed. An initial current mobile Lighthouse run exposed an invalid ARIA attribute on the brand container; the semantic image role was added and the full sixteen-report regression then achieved Accessibility/Best Practices `100` throughout. Direct Windows-shell activation and visual equivalence remain human samples.
- Second visible review, 2026-07-14: the independent Chrome window showed the remediated Overview, eight-item navigation, four KPI cards, provider/status rows, semantic Recent Alerts and Performance/Providers panels. TopBar notification navigation opened Alerts and the gear opened Settings. Windows UI Automation initially targeted the identically named tooltip instead of the notification button; the corrected button target and an injected pointer still did not prove a shell callback, which is an automation limitation rather than evidence of product failure.
- Third visible review, 2026-07-14: the validator clicked the visible notification-area icon and proved the shell callback, but the flyout failed during deferred template materialisation with `XamlParseException`: numeric token `12` was not a valid `Padding` value. The validator also rejected the structural difference between the four-destination WPF selector and the eight-destination Web navigation. This was a valid `S05-HG-010` failure, not a Human Gate approval.
- Root cause and remediation: generated spacing/radius primitives are `Double` resources. Assigning them through `DynamicResource` to WPF `Thickness` or `CornerRadius` properties compiled successfully but failed only when the flyout template was first opened. The flyout now uses parser-created typed literals for those properties, and an architecture regression rejects numeric spacing/radius resources on typed WPF properties. The WPF shell now exposes the same eight primary destinations and order as Web through a native left navigation rail, with separate Alerts, Performance, History, Configuration, Providers and Settings outcomes.
- Revalidation evidence: the WPF Release build and seven architecture tests passed. The existing UI Automation audit rendered the new shell at `1180×760` and `820×620`, exercised both languages/themes and found sixteen visible focusable controls with no unnamed control. The compact pass reflowed KPIs to two columns, stacked paired panels and kept all eight destinations available. A targeted invocation of the exact Windows `NotifyItemIcon` named `DB Notifier · demonstração local` opened the `Visão rápida da frota` flyout at `560×473`; the process remained responsive and no `.NET` exception dialog was present. Screenshots remain sanitised and temporary under `%TEMP%\DBNotifier-State05-Audit`.
- Fourth visible review, 2026-07-15: an authorised independent Chrome window and WPF `--show-desktop` sample opened successfully without Narrator, High Contrast or scaling. The validator observed that the Web header and WPF taskbar/header retained a green bell while the deterministic aggregate was Critical/red, and requested that every product-mark occurrence follow the bell's semantic colour. This is a valid consistency finding; it is not an approval or rejection decision for `S05-HG-010`.
- Fourth remediation: Design System `2.6.0` makes Dashboard header/favicon, WPF header/window/taskbar, flyout and NotifyIcon consume the same provider-neutral aggregate. Generated Web SVG and Windows ICO families cover Healthy/Warning/Critical/Unknown; static files without runtime evidence fail safely to Unknown/grey. The already approved notification-area-first hierarchy in `S05-HG-011` is unchanged.
- Fifth visible review, 2026-07-15: after the cross-surface synchronisation, the validator reported that the Critical bell appeared orange and that the dark database fill looked strange in Light mode. The requested correction was a darker red and a database cylinder with no interior fill. This is a visual finding for `S05-HG-010`, not an approval or rejection of that sample.
- Fifth remediation: Design System `2.6.1` changes the Critical bell from `#EB4C4C` to the deeper red `#C62828` and removes the database-body fill from both generated SVG and raster ICO layers. The blue outline, white bell outline, semantic state mapping and cross-surface aggregate policy are preserved. Because the notification-area-first workflow and state mapping did not change, the prior `S05-HG-011` approval remains valid; Light/Dark appearance still requires the pending `S05-HG-010` visual review.
- Sixth visible review, 2026-07-15: the authorised independent Chrome/WPF sample showed the corrected deep-red bell and transparent cylinder. The header-scale mark was acceptable, but screenshots exposed a pale blue silhouette on Light Windows surfaces and a document-like appearance in the 16 px title/notification-area rendering. The validator agreed with a further legibility refinement while explicitly retaining transparency and the current Critical red. This is not an approval of `S05-HG-010`.
- Sixth remediation: Design System `2.6.2` adds a deep-navy keyline behind the blue database and white bell strokes, enforces stronger minimum raster strokes, and omits the middle seam at 16/20 px. The complete geometry remains from 24 px, the Critical accent remains `#C62828`, and the cylinder interior remains transparent. This presentation-only refinement does not alter the approved notification-area-first workflow or semantic aggregate mapping of `S05-HG-011`.
- Seventh visible review, 2026-07-15: the authorised independent Chrome/WPF sample displayed `2.6.2` without runtime errors, but the validator found the DB Notifier mark visibly less sharp than neighbouring Windows and browser icons in the taskbar, notification overflow, title bar and browser tab. The screenshots are treated as a valid `S05-HG-010` legibility finding, not as approval or rejection of the already approved notification-area-first workflow.
- Seventh remediation: Design System `2.6.3` replaces multilayer small-raster treatment with pixel-aligned 16/20 px entries, bounded 24 px antialiasing, a compact palette through 32 px and dedicated four-resolution browser favicons. The small database uses Windows blue `#0078D4`, retains its seams and places a solid semantic bell over the deep-navy keyline. Transparency and Critical `#C62828` remain unchanged; detailed SVG and larger ICO geometry remain intact.
- Eighth visible review, 2026-07-15: screenshots from the authorised independent Chrome/WPF sample showed that only the Windows notification rendering had materially improved. The browser favicon, WPF title, taskbar, notification overflow and header marks still appeared softer than neighbouring icons. The Windows notification also attributed the application to the internal identifier `DBNotifier.Desktop.Wpf`. The validator explicitly requested a canonical `DB Notifier` attribution and another `S05-HG-010` repetition; this is a valid failure of the previous visual remediation, not an approval.
- Eighth remediation: Design System `2.6.4` removes the stacked deep-navy/white outlines from the complete active SVG/ICO family, uses one `#0078D4` database outline and one solid semantic bell/clapper, and applies binary pixel alignment to 16/20/24/32 px entries. WPF now declares `DB Notifier` as File Description and Product Name while retaining its technical assembly filename. Automated evidence can prove generated geometry, alpha and binary metadata; comparative sharpness and actual Windows attribution remain part of the next human repetition.
- Ninth visible review, 2026-07-15: the authorised `2.6.4` sample showed a visible improvement, but the validator still found the product mark less sharp than neighbouring Windows icons. This feedback keeps `S05-HG-010` pending; it does not reopen the previously approved notification-area-first behaviour of `S05-HG-011`.
- Ninth remediation: inspection of the active WPF decoder proved that it selected the 128 px ICO frame by default and then reduced it for the 30–40 px WPF surfaces. Design System `2.6.5` explicitly selects the nearest native frame, uses 40 px without stretching in the shell header, 32 px without stretching in the flyout, requests the Windows small-icon metric for `NotifyIcon`, and assigns separate native title/taskbar icons to the WPF window. Architecture and Dashboard structural regressions prevent the default large-frame decoder from returning. Human comparison remains required because automatic evidence cannot decide perceptual parity with unrelated Windows icons.
- Tenth visible review, 2026-07-15: screenshots from the authorised `2.6.5` repetition showed visibly different mark models across the browser tab/header, WPF header/flyout, title, taskbar, notification overflow and Windows notification surfaces. The validator requested one attractive, sharp, high-quality model everywhere, with the same geometry, proportions and transparency. This is a valid `S05-HG-010` consistency finding, not an approval or a reopening of `S05-HG-011`.
- Tenth remediation: Design System `2.6.6` makes one geometry contract the source of both SVG and ICO output. Every active resolution retains the same database-and-bell silhouette, proportions, transparent canvas/interior and database seams; semantic variants change only the bell accent, while resolution changes only the sampling density. Small rasters use bounded supersampling and straight-alpha BGRA so partially transparent edge pixels retain their canonical blue/red colour instead of producing a dark or grey fringe. Browser favicon frames reuse the same raster payload as the corresponding Windows frames, the retained PowerShell client loads the same family instead of drawing a second model and the first-hide Windows balloon no longer requests an unrelated native information glyph. WPF converts its 32/40 DIP hosts to physical pixels through the current `DpiScale` and selects an exact or next-larger frame, preventing the fixed-frame upscale identified above 100%. Structural regressions compare geometry, alpha masks, same-size frame payloads and the DPI-aware selection path; perceptual quality still requires the next human repetition.
- Eleventh visible review, 2026-07-15: the authorised `2.6.6` repetition showed the unified model on the main Dashboard/WPF/Tray surfaces, but the validator identified two remaining exceptions: the Chrome tab displayed an apparently Healthy/green favicon while the fixture aggregate was Critical, and the first-hide Windows notification displayed a tiny attribution icon that did not visibly match the current semantic bell. The validator reiterated that every state-bearing product mark must follow the corresponding aggregate colour. This is a valid `S05-HG-010` finding, not approval or rejection of the sample and not a reopening of `S05-HG-011`.
- Eleventh remediation: the active fixture, generated payloads and served files were audited before code changes. The fixture starts Critical, its Critical favicon contains the canonical `#C62828` bell and the public/build outputs were identical. These facts rule out a Healthy current payload and make a stale Chromium page-to-favicon association the supported inference, while the browser's internal cache decision remains unobserved. Design System `2.6.7` now derives the header and favicon paths from one typed aggregate-state helper and replaces identified or legacy favicon candidates atomically in `useLayoutEffect` with one state-and-revision-specific node. During first-hide notification delivery, WPF supplies a larger native frame from that same aggregate state and restores the small notification-area frame immediately afterwards. Windows Shell still owns notification attribution, scaling and cache, so this is a best-effort semantic source rather than a guarantee that the Shell's tiny attribution glyph changes dynamically; old Notification Centre entries remain historical snapshots. Reconciled Agent/API state and change-only notifications remain assigned to `STATE-06`.
- Twelfth visible review, 2026-07-15: the validator acknowledged the Windows attribution limitation and narrowed the current first-hide confirmation to a transparent mark with a fixed green bell. In this one non-state-bearing message, green means that DB Notifier remains available after hiding; it does not indicate fleet or database health. This is a new `S05-HG-010` finding, not an approval, rejection or reopening of `S05-HG-011`.
- Twelfth remediation: Design System `2.6.8` loads a dedicated canonical transparent green availability frame only during the synchronous first-hide `ShowBalloonTip` call and restores the state-bearing Tray icon in `finally`. All other runtime marks and every future state-bearing notification remain bound to factual aggregate state. Windows still owns final attribution rendering, scaling, caching and immutable historical entries.
- Thirteenth clarification, 2026-07-15: the product owner confirmed that every new notification must use the canonical icon whose bell corresponds to that notification's own meaning. This clarification is not a new visible sample, approval, rejection or permission to start Chrome/WPF, and it does not reopen `S05-HG-011`.
- Thirteenth remediation: Design System `2.6.9` introduces a provider-neutral typed selector. Availability or recovery maps to green, warning to yellow, critical to deep red, and informational, unknown or invalid meaning fails safely to grey. The current first-hide confirmation uses the availability mapping; the Tray retains its independent aggregate state. Five new unit cases and structural regressions cover the mapping, fail-safe fallback, single STATE-05 delivery call and restoration. Real event classification and delivery remain `STATE-06` work.
- Fourteenth visible review, 2026-07-15: the newly delivered first-hide notification still appeared to place the tiny attribution icon on a rectangular backing field. The screenshot is accepted as an `S05-HG-010` transparency finding; it is not an approval, rejection or permission to open another visible process. The light notification card itself remains a Windows-owned surface and is not part of the icon canvas.
- Fourteenth remediation: frame-by-frame inspection proved that the BGRA alpha channel and corners were transparent, but every generated ICO carried an all-zero legacy AND mask. Design System `2.6.10` now derives that mask from the actual alpha channel so every fully transparent pixel is masked and visible or partially covered pixels remain governed by straight alpha. The focused regression checks each pixel in all nine Windows frame sizes and reports zero mask/alpha disagreements. A new notification is required because Windows does not rewrite older Notification Centre snapshots.
- Fifteenth visible review, 2026-07-15: the fresh `2.6.10` notification no longer supplied evidence of an opaque legacy mask, but the validator still perceived the tiny blue database as a filled backing block and explicitly rejected that appearance. This is a new `S05-HG-010` legibility/transparency finding, not an approval and not permission to reopen WPF.
- Fifteenth remediation: pixel inspection found a genuinely transparent 16 px canvas but excessive blue-outline density: the former `4.5`-unit stroke left `137/256` pixels fully transparent and occupied `63.09` alpha-weighted blue pixel equivalents. Design System `2.6.11` uses one `3.5`-unit outline, leaving `155/256` pixels fully transparent while retaining `90` pixels at alpha `>=128`. WPF now supplies that native Windows small frame directly and holds the green availability icon until `BalloonTipShown` reports that Windows displayed the balloon, with a bounded two-second restoration fallback, instead of offering a larger source and restoring it immediately. This is a best-effort Shell boundary, not proof of attribution-glyph capture. A newly delivered notification remains necessary for human confirmation.
- Sixteenth visible review, 2026-07-15: the new notification was still perceived as having a blue-backed database glyph, and the reviewer found that closing, reopening and closing the WPF shell again did not produce another visible confirmation. These are valid `S05-HG-010` transparency/legibility and repeatability findings. They do not prove that the `2.6.11` ICO canvas was opaque, do not constitute visual approval and do not reopen the notification-area-first hierarchy already approved in `S05-HG-011`.
- Sixteenth remediation: Design System `2.6.12` separates native small rendering from canonical artwork. The 16/20/24 px ICO frames use a binary, pixel-aligned optical micro-glyph; 32 px and above retain the canonical antialiased geometry. A generated transparent 64 px PNG with a green availability bell is registered under display name `DB Notifier` through `AppNotificationManager`. Windows App SDK auto-bootstrap is disabled; explicit fail-safe runtime initialisation ensures unavailable runtime/platform/asset/registration/publication falls back to the single retained `ShowBalloonTip` path without preventing Tray startup. Every explicit `CloseRequest` now requests a confirmation, while minimise, hidden startup, Show and Exit do not. The modern entry uses stable tag/group identity, five-minute expiration and reboot expiration; activation accepts only `action=show` and may reveal only the local secondary shell. Solution Release completed with zero warnings/errors; `143/143` unit/model/provider/presentation, `9/9` architecture and `34/34` Dashboard tests, Dashboard typecheck and Vite production build passed. These automatic results do not establish visible Shell delivery or visual acceptance.
- Seventeenth visible review, 2026-07-15: one fresh WPF process completed two valid explicit CloseRequest cycles through the real notification-area icon and flyout path. The validator then stated that the previously requested transparency and repeated-notification behaviour were now as intended. This is limited human closure of those two `REQ-064` details only. One intermediate native-window re-show attempt did not exercise the WPF state transition and was discarded rather than counted as evidence. In the same review, the validator identified that the tiny title/Tray glyph used a visibly different database shape from the larger WPF header mark and required one identical icon model everywhere. That consistency finding keeps `S05-HG-010` pending and does not approve the complete `STATE-05` Human Gate.
- Seventeenth remediation: Design System `2.6.13` removes the separate 16 px grid and its 20/24 px expansions. Every ICO and favicon frame from 16 through 256 px now derives from the same canonical curved `markGeometry`, with the same full cylinder ellipse, both seams, bell, clapper, proportions and transparent silhouette. Four-by-four supersampling and straight alpha apply at every size; only target resolution and semantic bell colour may vary. The Windows app-notification publisher, transparent availability PNG and CloseRequest policy were not changed. The repeated gates passed with `34/34` Dashboard tests, `143/143` unit/model/provider/presentation tests, `9/9` architecture tests, zero-warning/error Release build and 96 headless locale/theme/viewport samples. These results establish structural uniformity, not perceptual acceptance on real shell surfaces.
- Human closure: `PENDENTE`. The runtime failure and objective rendering divergences are remediated automatically, but the validator still needs to decide whether the unified mark, revised WPF shell and flyout are visually acceptable. No approval is inferred from the bug report or from automated activation.

### `S05-HG-011` — Make the Windows client notification-area-first

- Date requested: 2026-07-14.
- Source: explicit product-owner clarification that WPF primarily exists as the DB Notifier presence in the Windows notification area and should take conceptual inspiration from Oracle MySQL Notifier.
- Classification: product-role correction and blocking Tray workflow sample; it supersedes only the assumption that the full WPF shell should appear automatically or compete with the Web Dashboard.
- Reference analysis: Oracle's archived MySQL Notifier manual describes a taskbar-resident notifier whose icon opens the primary status menu, lists monitored servers separately, reflects aggregate state, can notify on status changes and delegates richer management to secondary applications. Its final documented series is under Lifetime Sustaining Support. The reference is used for public behavioural requirements only; no Oracle code, artwork or vendor-specific implementation is reused.
- Product-owner decision: on 2026-07-14, Bruno explicitly selected option 1, requiring a clean-room implementation that preserves the DB Notifier MIT licence and independent identity. This records informed product direction but does not approve this human sample or the `STATE-05` Human Gate.
- Adopted contract: ordinary DB Notifier startup creates the Windows notification icon and leaves the full WPF shell hidden. One icon activation opens the compact fleet flyout. The full WPF shell remains a secondary drill-down destination. The explicit `--show-desktop` argument exists only for development and bounded accessibility/visual audits.
- Rejected legacy behaviours: no service auto-add by name, WMI/DCOM reverse callback, firewall mutation, provider connection-file coupling or unconditional Start/Stop/Restart. Authorised Agent/API state and change-only Windows notifications remain `STATE-06`; administrative actions remain gated by exact capability and `STATE-07` homologation.
- Automatic evidence: the focused WPF Release build completed with zero warnings/errors and all twelve Tray presentation cases passed. The clean-room policy aggregates provider-neutral state with `Critical > Warning > Unknown > Healthy` precedence, prevents stale evidence from counting as healthy and defines future notifications as opt-in, non-initial and change-only without delivering them. Normal startup produced no main-window handle; the Windows notification icon was found, its activation opened the localised fleet flyout, the safe Dashboard action opened the secondary WPF shell, the process remained responsive and no `.NET` dialogue appeared. The `--show-desktop` audit override rendered `pt-BR`/Dark at `820×620` with sixteen focusable controls, none unnamed and twelve contained Tab steps. The full revalidation passed 135 unit/presentation tests, seven architecture tests, 32 Dashboard tests, ten Pester tests, build/format, generated-asset drift, Markdown/documentation, dependency and fail-closed runtime gates.
- Follow-up visual requirement, 2026-07-14: the product owner rejected the blue icon tile and requested a larger database with a bell whose green/yellow/red state follows the fleet aggregate, functionally inspired by the public MySQL Notifier behaviour. Design System `2.5.0` removes the tile, adds transparent Healthy/Warning/Critical/Unknown variants and selects the red-bell variant for the current labelled Critical fixture. This is automatic presentation evidence only; live reconciled alert changes and Windows delivery remain `STATE-06`.
- Functional-coverage requirement, 2026-07-14: the request for all public MySQL Notifier functionality is recorded through a complete adopt/adapt/reject-or-replace matrix in `Legacy-Migration-Plan.md`. Rows assigned to `STATE-06`, `STATE-07` and `STATE-08` are planned requirements, not implementation or support claims, and unsafe vendor-specific network/service mechanisms are replaced by DB Notifier security contracts.
- Human closure: `APROVADO` by Bruno on 2026-07-15 through the explicit response `S05-HG-011 APROVADO`, after the authorised normal-startup review. This approves the notification-area-first hierarchy, transparent semantic icon, compact flyout and secondary-shell workflow represented by this sample only.
- Lifecycle impact: no transition. `STATE-05` and its overall Human Gate remain pending.

## Human Gate decision

- Phase: `STATE-05 FRONTEND_IMPLEMENTATION`
- Validator: PENDENTE
- Validation date: PENDENTE
- Automatic report reviewed: PENDENTE
- Critical samples repeated: PENDENTE
- Experience and error messages: PENDENTE
- Security/authorisation truth: PENDENTE
- Remaining coverage: remediated Overview and the still-independent visual details of `S05-HG-010`, beginning with perceptual confirmation that the `2.6.13` mark has one recognisably identical database-and-bell geometry in the browser tab/header, WPF title/header/flyout, taskbar and notification overflow. The transparent availability asset and repeated explicit-close behaviour received limited human confirmation in the seventeenth review and do not need another functional two-cycle sample unless their implementation changes; that confirmation does not approve the rest of the mark or gate. Fullscreen/TV layout and controls, system-local time, compact mobile TopBar, `320 px` Alert/Configuration cards, corrected narrow-desktop Alert summary, wordmark, translation icon and Light/Dark-only control confirmation plus all keyboard, screen-reader, zoom, scaling, High Contrast and remaining samples also remain pending. The notification-area-first workflow and Tray aggregate mapping are closed by `S05-HG-011`; real state-change notification delivery remains assigned to `STATE-06`.
- Decision: `PENDENTE`
- Justification/evidence: PENDENTE

Permitted decisions after the sample are `APROVADO`, `APROVADO COM RESSALVAS` or `REPROVADO`. Only an explicit human decision may change this section or authorise the lifecycle-transition workflow.

## Recommended next step

Continue with the still-independent `S05-HG-010` visual details by comparing the fresh `2.6.13` mark in the browser tab/header, WPF title/header/flyout, taskbar and notification overflow. Confirm that every occurrence reads as the same open database with both seams and the same bell proportions; size and factual bell colour may differ, but the silhouette must not. The previously accepted transparency and repeated CloseRequest behaviour do not need to be repeated unless the publisher or close policy changes. Then complete the remaining accessibility samples. Obtain fresh, sample-specific consent before opening Chrome, WPF or any Windows accessibility setting. Do not repeat `S05-HG-011` unless a later implementation materially changes the notification-area-first workflow or Tray aggregate mapping.
