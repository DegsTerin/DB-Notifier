# STATE-05 WPF Accessibility Audit

## Result

`AUTOMATIC EVIDENCE APPROVED WITH LIMITATIONS — HUMAN NARRATOR SAMPLE NOT COMPLETED`

The WPF build, UI Automation matrix, Windows High Contrast response and available Windows scaling samples passed in the scope described below. The human validator reported that they could not perform the Narrator sample. Narrator therefore remains `PENDENTE`; this is neither a product failure nor an approval.

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

## Windows High Contrast

High Contrast was enabled through the documented Windows `SPI_SETHIGHCONTRAST` mechanism and confirmed simultaneously by the native flag and WPF `SystemParameters.HighContrast`.

| Sample | Result |
|---|---|
| `pt-BR` / Light / `1180×760` | 34 focusable, 0 unnamed, 12 contained Tab steps; target-window capture legible |
| `en-GB` / Dark / `1180×760` | 34 focusable, 0 unnamed, 12 contained Tab steps; repeated target-window capture legible |

The effective Windows colours exposed dark window surfaces, white window text, cyan selection and a visible focus colour. Text, boundaries, selection and non-colour status symbols remained visible in the repeated target-only captures. One initial `en-GB` capture was incomplete immediately after a preference cycle; an isolated repetition rendered fully and classified the first image as a transient `PrintWindow` timing artefact rather than persistent product behaviour.

High Contrast was restored to Off after the samples. This automatic/technical visual evidence does not replace the named human review required by `HG05-07`.

## Windows scaling

The audit changed the Windows Display Settings selection, launched a fresh WPF process at each available required scale and verified the effective window DPI with `GetDpiForWindow`.

| Scale | Effective DPI | Sample | Window bounds in physical pixels | Accessibility result |
|---:|---:|---|---:|---|
| 125% | 120 | `pt-BR` / Light / minimum | `1025×775` | 23 focusable, 0 unnamed, focus contained |
| 150% | 144 | `en-GB` / Dark / minimum | `1230×930` | 21 focusable, 0 unnamed, focus contained |
| 200% | Not available | Not run | Not applicable | Environment does not offer this value |

At 125% and 150%, the TopBar, five metrics, footer and primary content remained legible. The minimum-width Inventory table correctly exposed an accessible horizontal `ScrollPattern`; approximately 68% of its columns were visible at once and the remaining columns stayed reachable through the DataGrid scroll region. The outer content exposed vertical scrolling where required.

The WPF window reported DPI-awareness classification `1` (`PROCESS_SYSTEM_DPI_AWARE`). Fresh processes launched at 125% and 150% received the correct system DPI, as proved above. Moving a running window between monitors with different DPI values and live rescaling of an already-running process were not tested. Microsoft recommends additional per-monitor DPI handling for those scenarios, so mixed-DPI/multi-monitor behaviour remains technical debt rather than an inferred pass.

The audit runner now places its own UI Automation/capture thread in Per-Monitor V2 context. This prevents virtualised bounds from truncating `PrintWindow` evidence at non-100% scaling and records effective DPI, awareness classification and DataGrid scrolling in each report.

## Narrator outcome

Narrator was started only after explicit authorisation, and the visible `pt-BR`/Light WPF window was focused. The human validator then reported: `Não consigo testar a função narrador` and requested Narrator to be stopped.

Observed automatic facts are limited to:

- Narrator is installed and its process started;
- the WPF automation tree exposed named controls and a contained focus order;
- Narrator and WPF were stopped after the human report.

No claim is made about spoken order, pronunciation, verbosity, state announcements or usability. `HG05-01`, `HG05-02`, `HG05-04` and `HG05-05` remain pending wherever they require a human screen-reader sample.

## Evidence and limitations

- Raw JSON reports and target-window PNGs are stored under `%TEMP%\DBNotifier-State05-Audit` and are not committed because they are environment-specific.
- [`scripts/audit-state05-wpf.ps1`](../scripts/audit-state05-wpf.ps1) is the reproducible, target-process-only runner.
- [`STATE-05-Human-Gate-Validation.md`](STATE-05-Human-Gate-Validation.md) remains the authority for human samples and the final gate decision.
- Microsoft documents WPF DPI behaviour and the need for explicit per-monitor handling in [Developing a Per-Monitor DPI-Aware WPF Application](https://learn.microsoft.com/en-us/windows/win32/hidpi/declaring-managed-apps-dpi-aware).
- Microsoft documents the High Contrast parameter in [SystemParametersInfoW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow).

## Lifecycle impact

The automatic WPF/High Contrast/available-scaling evidence is approved with the limitations above. The `STATE-05` Human Gate remains `PENDENTE`; no transition to `STATE-06` is authorised.

## Recommended next step

Do not retry Narrator until the human validator has a workable listening method or another named validator is available. Continue with non-screen-reader human samples independently, then record Narrator as `NÃO EXECUTADO — VALIDADOR SEM CONDIÇÃO DE TESTE` in any interim gate decision. A final `STATE-05` approval must either include a completed screen-reader sample or carry an explicit, accepted reservation that accurately describes the missing evidence.
