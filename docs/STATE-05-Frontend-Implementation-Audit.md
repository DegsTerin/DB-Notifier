# STATE-05 Frontend Implementation Closure Audit

## Decision

**REJECTED** on 2026-07-12. `STATE-05 FRONTEND_IMPLEMENTATION` remains active and its Human Gate must not be requested yet.

The functional, security and build baselines pass, but the browser audit found two high-severity accessibility/responsiveness defects: global horizontal overflow at supported mobile widths and an `aria-modal` confirmation dialogue that neither receives nor contains keyboard focus and does not close with Escape. Both defects are reproducible in the built production Dashboard and block closure.

No transition to `STATE-06 INTEGRATION`, provider laboratory, real connection, administrative execution or provider homologation occurred.

The subsequent remediation and approved automatic re-audit are preserved in `STATE-05-Frontend-Implementation-Reaudit.md`. This rejected report remains the immutable record of the original findings.

## Scope and environment

- Workspace: DB-Notifier `main`, Windows/WSL, 2026-07-12.
- Runtime baseline: .NET SDK `10.0.301`; all active targets remain `net10.0` or `net10.0-windows`.
- Browser sample: Google Chrome `150.0.7871.115` in headless mode through Chrome DevTools Protocol; Node.js `24.18.0` drove the protocol client.
- Dashboard sample: production Vite build served locally on loopback only.
- WPF sample: Release `DBNotifier.Desktop.Wpf.exe`, default `1180×760` window, Windows UI Automation.
- Evidence output: sanitised JSON and PNG files under `%TEMP%\DBNotifier-State05-Audit`; temporary evidence is not committed.

## Automatic gate results

| Gate | Result | Evidence |
|---|---|---|
| .NET 10 Release build | APPROVED | 13 projects, 0 warnings, 0 errors |
| .NET tests | APPROVED | 120 unit/model/provider/presentation + 5 architecture = 125/125 |
| .NET format | APPROVED | `dotnet format --verify-no-changes` exited 0 |
| Dashboard documentation gate | APPROVED | 138 comment-capable source files after the audit tooling update |
| Dashboard typecheck/tests/build | APPROVED | TypeScript clean, 7/7 tests, Vite production build |
| Dependency audit | APPROVED | `npm audit --audit-level=high`: 0 vulnerabilities |
| Legacy compatibility | APPROVED | Pester 10/10 |
| Bundle composition | APPROVED | `build.ps1 -ValidateOnly` |
| Operational states | APPROVED | loading/empty/offline/error/denied/maintenance roles, busy state and retry visibility matched their contracts |
| Reduced motion | APPROVED | loading pseudo-element computed `animation-name: none` under reduced-motion emulation |
| Accessibility tree | APPROVED WITH RESERVATION | 285 exposed nodes, expected landmarks/roles, no unnamed link/button/combobox/textbox; this is not a screen-reader session |
| Dashboard keyboard sequence | APPROVED WITH RESERVATION | skip link first, then navigation, scenario and filters; modal sequence fails separately below |
| Desktop/laptop/tablet layout | APPROVED | 1440×1000, 1024×768 and 768×1024 without global horizontal overflow |
| Reflow-equivalent sample | APPROVED WITH RESERVATION | 640 CSS px sample, equivalent to a 1280 px viewport at 200% reflow, had no global overflow; native user zoom remains a Human Gate sample |
| Mobile layout | REJECTED | global overflow and clipping at 390×844 and 320×568, including Inventory, Alerts and Configuration |
| Confirmation dialogue keyboard access | REJECTED | initial focus outside, Tab escapes immediately, Escape does not close |
| WPF runtime/UI Automation | APPROVED WITH RESERVATION | visible 1180×760 window, 32 exposed focusable elements, deterministic Tab traversal and clean process shutdown; one unnamed framework Pane is non-actionable |

## Findings

### S05-AUD-001 — High — Mobile viewport expands beyond the visible page

At a requested width of 390 px, the document client width was 375 px and its scroll width was 457 px. At 320 px, the client width was 305 px and the scroll width remained 457 px. Inventory, Alerts and Configuration all reproduced the same global overflow. Screenshots show headings, descriptive text, the demonstration badge, navigation and cards clipped behind a horizontal scrollbar.

The grid/flex intrinsic width propagates from the horizontal mobile navigation through `.app-shell`, `.page-layout`, `.sidebar` and `main`. A controlled remediation must constrain grid/flex children with appropriate `min-width: 0`, keep horizontal navigation scrolling inside its own container, and prove that content — rather than merely the viewport — no longer clips.

Reproduction:

```powershell
# Terminal 1, from the Dashboard directory
node.exe node_modules/vite/bin/vite.js preview --host 127.0.0.1 --port 4173

# Terminal 2, using the installed Chrome path
chrome.exe --headless=new --remote-debugging-port=9224 --user-data-dir="$env:TEMP\DBNotifier-State05-Chrome" http://127.0.0.1:4173/

# Terminal 3, from the repository root
node.exe .\scripts\audit-state05-dashboard.mjs
```

Acceptance requires `documentScrollWidth <= documentClientWidth + 1` at 320, 390, 768, 1024 and 1440 px for every implemented view, plus visual confirmation that labels and controls remain readable.

### S05-AUD-002 — High — Modal semantics do not match keyboard behaviour

The confirmation surface declares `role="dialog"` and `aria-modal="true"`, but opening it leaves focus outside the dialogue. Four subsequent Tab presses reached the skip link and main navigation rather than remaining in the dialogue. Escape did not close it.

The remediation must move focus to a meaningful control or heading on open, contain Tab/Shift+Tab while open, close on Escape, restore focus to the invoking control, and prevent background interaction. Tests must cover both the `confirmationRequired` example and capability decision previews.

Reproduction:

```powershell
# Use the three-terminal preview/Chrome setup documented in S05-AUD-001, then run:
node.exe .\scripts\audit-state05-dashboard.mjs
```

Acceptance requires initial focus inside the dialogue, all forward/reverse Tab samples inside it, Escape closure and focus restoration to the opener.

## Human accessibility evidence inventory

The following evidence is explicitly **PENDING** and cannot be inferred from the accessibility tree, screenshots or automated contrast checks:

1. Dashboard with NVDA or Narrator plus Chrome/Edge: announce page title, landmarks, current navigation item, headings, scenario selector, inventory table/cards, timestamps, support truth and every operational live-state transition.
2. Dashboard dialogue after remediation: announce dialogue name/description, initial focus, disabled execution control, contained Tab/Shift+Tab, Escape closure and restored focus.
3. Dashboard native browser zoom: sample 200% at 1280×720 and verify reflow, visible focus, text readability and absence of two-dimensional page scrolling outside legitimate data regions.
4. WPF with Narrator or NVDA: announce window title, View and Scenario selectors, summary values, DataGrid headers/cells, non-colour status text, History/Alerts and Configuration views.
5. WPF Tray: announce Open/status/Exit items, verify window hide/restore and ensure no database/service action is implied or invoked.

Human evidence must record assistive technology/browser versions, Windows scaling, tested path, observed announcements, defects, operator, date and sanitised evidence. It remains pending until the automatic re-audit is approved.

## Security and phase-boundary review

- Dashboard and WPF continued to use deterministic demonstration adapters.
- No API, Agent, database, IdP, vault, certificate, notification channel or administrative executor was contacted.
- PostgreSQL remains implemented but not homologated; MySQL, MariaDB, MongoDB and SQL Server are not activated by this audit.
- Start/Stop/Restart remain unavailable and no command was created or dispatched.
- Temporary browser and desktop processes were terminated after evidence collection.

## Required remediation and next gate

1. Correct `S05-AUD-001` and add regression coverage for all implemented routes at 320/390/768/1024/1440 CSS px.
2. Correct `S05-AUD-002` with focus entry, containment, Escape, restoration and automated keyboard tests.
3. Re-run both audit scripts and the complete build/test/security stack.
4. Issue a `STATE-05` re-audit report. Only an approved re-audit may be presented for the Human Gate.
5. Keep `STATE-06` and the multi-database Docker laboratory blocked until the Human Gate is explicitly approved.
