# STATE-05 Human Gate Validation

## Gate status

`PENDENTE`

This document prepares the required human validation for `STATE-05 FRONTEND_IMPLEMENTATION`. It is neither an approval nor evidence that an unperformed sample passed. The validator must record their own name, date, observations and decision after operating the interfaces.

No transition to `STATE-06` is authorised by this document.

## Scope

The human sample covers:

- the React Dashboard in Brazilian Portuguese and British English;
- the WPF Desktop/Tray shell in Brazilian Portuguese and British English;
- Light, Dark and System preferences;
- keyboard-only operation and visible focus;
- Narrator reading order, names, states and announcements;
- native browser zoom, Windows scaling and High Contrast where the environment permits;
- operational truth for stale data, planned providers and unavailable administrative execution.

Mobile and tablet samples apply to the responsive Dashboard Web. There is no native mobile application in this phase. There is also no dedicated TV, wallboard or kiosk mode; standard desktop/ultrawide responsiveness is not evidence of TV-mode support. WPF remains a Windows desktop interface with a supported minimum window size of `820×620` DIP.

## Automatic evidence to review first

| Evidence | Current result |
|---|---|
| Design System implementation report | Design System `1.3.4` visual remediation, outlined database-mark and ultrawide gates approved; commit identifier is reported in the implementation hand-off |
| Dashboard matrix | 66 locale/theme/viewport-route samples, including `960×1040` and `1920×1080`; no global overflow or unnamed interactive control |
| WPF matrix | Six locale/theme combinations and minimum-window sample recorded after visual remediation |
| Automated tests | 21 Dashboard, 131 .NET unit/model/provider/presentation, 5 architecture and 10 legacy compatibility tests approved |
| Security/dependencies | npm and NuGet reported no known vulnerabilities in the recorded audit |

Primary automatic evidence:

- [`STATE-05-Design-System-Implementation-Report.md`](STATE-05-Design-System-Implementation-Report.md)
- [`STATE-05-Localisation-Implementation-Report.md`](STATE-05-Localisation-Implementation-Report.md)
- [`STATE-05-Frontend-Implementation-Reaudit.md`](STATE-05-Frontend-Implementation-Reaudit.md)

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

Open `http://127.0.0.1:4173/` in a dedicated Chrome window. Use the visible TopBar buttons to select the required locale and theme. Start or stop Narrator with `Windows+Ctrl+Enter` only when ready to listen to the sample.

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
Start-Process .\src\DBNotifier.Desktop.Wpf\bin\Release\net10.0-windows\DBNotifier.Desktop.Wpf.exe
```

Use the visible TopBar buttons to select the locale and theme. No database or service process is controlled by this shell.

### Keyboard and Narrator tasks

Repeat the critical path once in `pt-BR`/Light and once in `en-GB`/Dark, at the default size and `820×620` DIP minimum:

1. Press `Tab` and arrow keys through language/theme groups, view/scenario selectors, grids and visible actions.
2. Confirm visible focus and that Narrator announces the selected locale/theme, selector names, grid names, column headers and meaningful cell values.
3. Confirm Inventory summary counts and stale data are understandable without colour.
4. Visit History/Alerts and Configuration; confirm read-only/unsupported/denied wording remains explicit.
5. Open each safe preview dialogue and confirm title, message, button order and focus return.
6. Minimise/close to Tray, reopen the window from the Tray menu and confirm the current locale remains reflected in the native menu.
7. Exit through the Tray menu and confirm no DB-Notifier process remains.

## System, High Contrast and scaling protocol

These samples change user display preferences and must be performed interactively by the validator. Do not automate them silently.

1. With System selected, switch Windows application mode Light → Dark → Light. Confirm both interfaces follow without restart and explicit Light/Dark remain stable.
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
| `HG05-03` | Dashboard native 200% zoom and compact reflow | PENDENTE | |
| `HG05-04` | WPF `pt-BR` Light, default/minimum, keyboard and Narrator | PENDENTE | |
| `HG05-05` | WPF `en-GB` Dark, default/minimum, keyboard and Narrator | PENDENTE | |
| `HG05-06` | System live Light/Dark observation on both interfaces | PENDENTE | |
| `HG05-07` | Windows High Contrast on both interfaces | PENDENTE | |
| `HG05-08` | WPF Windows scaling at 125%, 150% and 200% where permitted | PENDENTE | |
| `HG05-09` | Visual hierarchy and operational-truth review | PENDENTE | |

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
- TV scope: repository and Design System review confirm that no dedicated TV/wallboard/kiosk mode is implemented; the ultrawide correction must not be presented as such.
- Human closure: PENDENTE; the visible fullscreen correction and display name still require reviewer confirmation.
- Lifecycle impact: Narrator remains off and the Human Gate remains pending.

## Human Gate decision

- Phase: `STATE-05 FRONTEND_IMPLEMENTATION`
- Validator: PENDENTE
- Validation date: PENDENTE
- Automatic report reviewed: PENDENTE
- Critical samples repeated: PENDENTE
- Experience and error messages: PENDENTE
- Security/authorisation truth: PENDENTE
- Remaining coverage: outlined database-mark, fullscreen layout and display-name confirmation plus all keyboard, screen-reader, zoom, scaling, High Contrast and remaining samples above
- Decision: `PENDENTE`
- Justification/evidence: PENDENTE

Permitted decisions after the sample are `APROVADO`, `APROVADO COM RESSALVAS` or `REPROVADO`. Only an explicit human decision may change this section or authorise the lifecycle-transition workflow.

## Recommended next step

Confirm the outlined canonical database mark, fullscreen layout and `DB Notifier` display name in the visible `pt-BR` Dashboard, then obtain explicit consent before starting Narrator and continuing the keyboard/accessibility protocol; record every result without inferring the final Human Gate decision.
