# STATE-05 WPF Accessibility Audit

## Result

`AUTOMATIC AND BOUNDED HUMAN EVIDENCE APPROVED WITH LIMITATIONS`

The WPF build, UI Automation matrix, current Windows High Contrast response, available Windows scaling samples, keyboard/dialogue paths, notification-area flyout and bilingual full-shell Narrator paths passed in the scopes described below. The final Design System `3.0.4` sample also exposed the isolated seven-item ComboBox popup and its usable scrollbar in both locales. Physical Windows 200% scaling and mixed-DPI movement remain unavailable environmental evidence rather than failed product observations.

No database, Agent, API, service controller, identity provider, vault, notification channel or administrative executor was contacted.

## Authority and environment

Bruno explicitly requested validation of WPF, Narrator, High Contrast and Windows scaling on 2026-07-14. The initial and restored environment was:

| Item | Value |
|---|---|
| Operating system | Windows 11 Enterprise `10.0.26200` |
| Display | one `1920×1080` 60 Hz monitor |
| Initial/final scaling | `100%`, 96 DPI |
| Initial/final High Contrast | Off |
| Initial/final Narrator | Not running |
| Initial/final DB Notifier WPF processes | None |

Windows Display Settings offered `100%`, `125%`, `150%` and `175%`. It did not offer `200%` on the active display. The gate requires 200% only where the environment permits it, so 200% is recorded as unavailable rather than passed or failed.

The 2026-07-17 combined human campaign repeated the applicable display and accessibility observations against the current `net10.0-windows10.0.22621.0` executable with SHA-256 `6F817330E2743FA2A7FA2E29D98206703B691BECE984C4026B02FD1598D921AB`. The original WPF preference bytes, Windows application mode, High Contrast flag, scaling and Narrator state were restored at the end.

## WPF matrix

The Release build completed with zero warnings and zero errors. Eight current UI Automation samples covered:

- `pt-BR` and `en-GB`;
- Light and Dark;
- default `1180×760` DIP and minimum `820×620` DIP windows.

| Window | Samples | Focusable controls | Unnamed focusable controls | Tab sample |
|---|---:|---:|---:|---|
| `1180×760` | 4 | 34 each | 0 | 12 steps; focus remained in DB Notifier |
| `820×620` | 4 | 21 each | 0 | 12 steps; focus remained in DB Notifier |

Every combination completed and restored the `pt-BR` ↔ `en-GB` and Light ↔ Dark preference cycles. The sample is deterministic and read-only.

### Design System 2.3.1 shell revalidation

The complete eight-sample locale/theme/size matrix was repeated after the four-destination selector was replaced by the shared eight-destination navigation rail and the compact Overview reflow was introduced. All eight reports passed with 16 visible focusable controls, zero unnamed focusable controls and 12 contained Tab steps. The `820×620` samples used two KPI columns, stacked paired panels and suppressed the redundant state card so the complete navigation remained available. This revalidation did not repeat Windows scaling, High Contrast or Narrator; their evidence and limitations below remain separate.

## Windows High Contrast

High Contrast was enabled through the documented Windows `SPI_SETHIGHCONTRAST` mechanism and confirmed simultaneously by the native flag and WPF `SystemParameters.HighContrast`.

| Sample | Result |
|---|---|
| `pt-BR` / Light / `1180×760` | 34 focusable, 0 unnamed, 12 contained Tab steps; target-window capture legible |
| `en-GB` / Dark / `1180×760` | 34 focusable, 0 unnamed, 12 contained Tab steps; repeated target-window capture legible |

The effective Windows colours exposed dark window surfaces, white window text, cyan selection and a visible focus colour. Text, boundaries, selection and non-colour status symbols remained visible in the repeated target-only captures. One initial `en-GB` capture was incomplete immediately after a preference cycle; an isolated repetition rendered fully and classified the first image as a transient `PrintWindow` timing artefact rather than persistent product behaviour.

High Contrast was restored to Off after the samples. This historical automatic/technical evidence was later supplemented by the named human review below.

In the authorised 2026-07-17 combined campaign, the current WPF executable was observed with the real High Contrast flags changing `126 → 127 → 126` and the `Preto em Alto Contraste` scheme. At `820×620`, the Inventory DataGrid exposed a real horizontal scrollbar with approximately `50.19%` horizontal view size and Configuration allowed its ordinary ComboBox to expand. The final `3.0.4` review then used the isolated overflow argument under the same real High Contrast override: both `pt-BR` and `en-GB` popups exposed exactly seven owner-scoped options and one usable vertical scrollbar. Bruno classified High Contrast as visible, and the original flags/scheme were restored.

## Windows scaling

The audit changed the Windows Display Settings selection, launched a fresh WPF process at each available required scale and verified the effective window DPI with `GetDpiForWindow`.

| Scale | Effective DPI | Sample | Window bounds in physical pixels | Accessibility result |
|---:|---:|---|---:|---|
| 125% | 120 | `pt-BR` / Light / minimum | `1025×775` | 23 focusable, 0 unnamed, focus contained |
| 150% | 144 | `en-GB` / Dark / minimum | `1230×930` | 21 focusable, 0 unnamed, focus contained |
| 200% | Not available | Not run | Not applicable | Environment does not offer this value |

At 125% and 150%, the TopBar, five metrics, footer and primary content remained legible. The minimum-width Inventory table correctly exposed an accessible horizontal `ScrollPattern`; approximately 68% of its columns were visible at once and the remaining columns stayed reachable through the DataGrid scroll region. The outer content exposed vertical scrolling where required.

The authorised 2026-07-17 human repetition used the real Windows Display Settings UI. At 125%, `GetDpiForWindow` reported 120 DPI, the physical bounds were `1025×775`, horizontal scrolling was exposed and the view size was approximately `69.66%`. At 150%, it reported 144 DPI, the fitted physical bounds were `1230×737`, horizontal scrolling was exposed and the view size was approximately `88.99%`. The difference from the earlier `1230×930` automatic capture reflects the current screen-fit position and does not alter the reported DPI. An initial registry-only attempt was discarded because the running WPF process remained at 96 DPI. The real display selection and system DPI were restored to 100%/96 DPI. Bruno approved the combined visible campaign; 200% remained unavailable on the active monitor.

The WPF window reported DPI-awareness classification `1` (`PROCESS_SYSTEM_DPI_AWARE`). Fresh processes launched at 125% and 150% received the correct system DPI, as proved above. Moving a running window between monitors with different DPI values and live rescaling of an already-running process were not tested. Microsoft recommends additional per-monitor DPI handling for those scenarios, so mixed-DPI/multi-monitor behaviour remains technical debt rather than an inferred pass.

The audit runner now places its own UI Automation/capture thread in Per-Monitor V2 context. This prevents virtualised bounds from truncating `PrintWindow` evidence at non-100% scaling and records effective DPI, awareness classification and DataGrid scrolling in each report.

## Narrator outcome

Narrator was started only after explicit authorisation, and the visible `pt-BR`/Light WPF window was focused. The human validator then reported: `Não consigo testar a função narrador` and requested Narrator to be stopped.

Observed automatic facts are limited to:

- Narrator is installed and its process started;
- the WPF automation tree exposed named controls and a contained focus order;
- Narrator and WPF were stopped after the human report.

That historical attempt makes no claim about spoken order, pronunciation, verbosity, state announcements or usability.

The authorised 2026-07-17 combined campaign later started Narrator and used the real normal-startup WPF notification-area path. An initial focus-loss attempt closed the flyout and was discarded. After Narrator was already active, the real `DB Notifier` icon was invoked again; the current `pt-BR` fleet flyout remained visible while ten focus observations traversed Open Dashboard, Open Configuration, Open logs and Exit. Bruno then stated exactly `Amostra combinada visível STATE-05: APROVADA. Narrator: AUDÍVEL E COMPREENSÍVEL.` Narrator and WPF were stopped afterwards.

That first decision approved audibility and comprehensibility for the bounded `pt-BR` flyout action path. The final Design System `3.0.4` sample subsequently traversed the complete WPF shell in `pt-BR`/Light and `en-GB`/Dark with Narrator active, including all eight destinations and both isolated ComboBox popup reviews. Bruno then classified Narrator as `AUDÍVEL E COMPREENSÍVEL`. This closes the named full-shell and `en-GB` Human Gate samples without claiming every future integrated-state announcement or pronunciation.

## Design System 2.6.14 native-chrome addendum

The later `REQ-066` review found that the Windows-managed caption and default WPF scrollbar did not follow the selected Dark theme. Design System `2.6.14` adds focused structural remediation without changing the earlier scaling, automation or Narrator samples:

- `NativeWindowThemePolicy` synchronises documented Windows 11 DWM immersive-dark, caption and text attributes after the native window handle exists and after every explicit Light/Dark change; Windows continues to own the active/inactive border, and Windows 10 retains its native caption;
- entering Windows High Contrast resets caption and text to the DWM default instead of forcing the product palette;
- one implicit application `ScrollBar` style uses the existing semantic resources for track, thumb, border and interaction states while retaining vertical/horizontal `PART_Track` templates and line/page commands;
- the WPF Release build completed with zero warnings/errors, and the tenth architecture test verifies the platform adapter, High Contrast reset, semantic-resource use and scrolling contract.

The original High Contrast and scaling observations above predated this native-chrome implementation. An authorised 2026-07-16 run then exercised Light → Dark → Light at `1180×760` and `820×620`: the caption and visible main scrollbar followed both themes, and UI Automation moved the scrollbar from `0.00%` to `9.38%` and restored it in Light and Dark. The 2026-07-17 combined campaign subsequently exercised the current implementation in real High Contrast and real 125%/150% scaling as recorded above. The DataGrid scrollbar was generated and observed; the seven-item ComboBox popup did not require an independent scrollbar.

## Design System 3.0.3 read-only-grid keyboard addendum

The authorised 2026-07-16 keyboard-only sample exposed a timing-sensitive focus-order defect in the Configuration capability `DataGrid`. A deliberately paced Tab sequence entered virtualised cells, then returned to the Window/TopBar after `service.stop` before reaching the two following buttons. A faster sequence could materialise the remaining rows and reach those actions, which proved that task order incorrectly depended on row realisation timing.

Design System `3.0.3` keeps every strictly read-only shell `DataGrid` as one explicit table-level Tab stop, applies `KeyboardNavigation.TabNavigation=None`, and removes each virtualised cell from the Tab sequence. Headers and cells remain represented by the native WPF grid automation contract. A new architecture regression confirms that all five shell grids remain read-only and that the shared DataGrid/DataGridCell styles retain this contract.

The focused Release build completed with zero warnings/errors. A dedicated visible-process regression paused `1.1 s` after every key in both `pt-BR`/Light and `en-GB`/Dark. Each Configuration path reached the confirmation action after eight stops, exposed exactly the two DataGrid controls once each, exposed no cell stop, contained forward/reverse dialogue focus, closed with Escape, restored the trigger and preserved reverse action order. A separate UI Automation read confirmed that both grids remained keyboard-focusable and retained their cell/data elements after the Tab change. Preferences were restored byte for byte and no dedicated WPF process or temporary script remained.

In the authorised human repetition on 2026-07-17, an initial foreground refusal aborted before any key was sent. The next dedicated `--show-desktop` process, PID `30228`, completed the same bounded path in `pt-BR`/Light and `en-GB`/Dark with 80 visible bounded focus observations, all eight destinations, exactly two DataGrid stops, zero cell stops, modal containment, Escape restoration and reverse order. Preferences were restored to the original bytes, the process/script were removed and Bruno responded exactly `Amostra humana de teclado WPF 3.0.3: APROVADA`.

## Design System 3.0.4 accepted-reservation remediation

The former system-DPI implementation debt is corrected structurally. A dedicated `Program` entry point calls `SetHighDpiMode(PerMonitorV2)` before constructing the WPF application or any WinForms notification-area resource; the .NET project declares the same `ApplicationHighDpiMode`, selects that startup object and retains an `asInvoker` manifest. The Release build completed with zero warnings/errors. A normal notification-area-first process then remained responsive with no main window and returned `PROCESS_PER_MONITOR_DPI_AWARE` classification `2` through `GetProcessDpiAwareness`; it was stopped with zero residual process. This proves the current process is per-monitor aware, while physical movement between different-DPI monitors and live `WM_DPICHANGED` layout remain unobserved on the single-monitor host.

The shared ComboBox template now binds its internal popup `ScrollViewer` to the control's `MaxDropDownHeight` and explicitly preserves automatic vertical scrolling. Exact argument `--review-combobox-overflow` changes only that maximum from the normal `320` DIP to `128` DIP for the unchanged seven-item scenario fixture; it neither changes items nor reveals the secondary shell without independent `--show-desktop`. Unit and architecture tests cover the isolated opt-in and template wiring. In the separately authorised final visible sample, the owner-scoped UI Automation tree exposed exactly seven `ListItem` descendants and one usable range-bearing scrollbar in both `pt-BR` and `en-GB`; the scrollbar moved to its maximum and returned to its minimum without changing the selected fixture item.

## Evidence and limitations

- Raw JSON reports and target-window PNGs are stored under `%TEMP%\DBNotifier-State05-Audit` and are not committed because they are environment-specific.
- [`scripts/audit-state05-wpf.ps1`](../scripts/audit-state05-wpf.ps1) is the reproducible, target-process-only runner.
- [`STATE-05-Human-Gate-Validation.md`](STATE-05-Human-Gate-Validation.md) remains the authority for human samples and the final gate decision.
- Microsoft documents WPF DPI behaviour and the need for explicit per-monitor handling in [Developing a Per-Monitor DPI-Aware WPF Application](https://learn.microsoft.com/en-us/windows/win32/hidpi/declaring-managed-apps-dpi-aware).
- Microsoft documents the High Contrast parameter in [SystemParametersInfoW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow).

## Lifecycle impact

The automatic and bounded human WPF/High Contrast/available-scaling evidence is approved with the limitations above. The Design System `2.6.14` native-chrome correction is structurally and visibly approved in its WPF scope. The Design System `3.0.3` read-only-grid Tab contract is structurally, technically and humanly approved in its keyboard/dialogue scope. Design System `3.0.4` corrects the process DPI policy and now has direct human/runtime evidence for its isolated ComboBox overflow case and bilingual full-shell Narrator path. Physical mixed-DPI movement and Windows scaling 200% remain unobserved because the available hardware did not expose them. The final sample decision does not replace the earlier formal Human Gate decision or authorise `STATE-06`.

## Recommended next step

Do not repeat the approved WPF keyboard/dialogue, High Contrast, available-scaling, bilingual Narrator or ComboBox-overflow paths unless their owning contracts change. Sample 200% Windows scaling only on hardware that offers it and mixed-DPI movement only with two safely available displays; do not simulate either as a human pass. Keep the lifecycle in `STATE-05` until a separate transition authorisation is received.
