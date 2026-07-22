# STATE-06 — R6-WPF1 Automatic Remediation Report

## Decision summary

- Baseline: `2cafd5d0df874833a2041a44ffd66e110163c8c6`
- Scope: navigation clarity and semantic visual consistency between WPF and Web
- Automatic result: `APPROVED — HUMAN W01 REPETITION PENDING SEPARATE AUTHORITY`
- Global gate: `NON-GREEN — PRE-EXISTING R0 ASSERTION ONLY`
- External access, restore, installation, download, notification and operational source: `NOT USED`

This increment remediates only the two defects recorded by `R6-HV-W01`. It does not approve that human sample, accept R6, resume W02 or P01, or change lifecycle state.

## Root causes

The shared localisation catalogue used `Configuração`/`Configuration` and `Configurações`/`Settings` for adjacent destinations. In pt-BR, the distinction depended almost entirely on singular versus plural, while neither locale stated that the first destination owns operational parameters and the second owns local interface preferences.

The WPF navigation rail also reused the dark shell-chrome background and muted chrome foreground. The Web Dashboard reserves that chrome for product identity and presents navigation on a neutral surface. The WPF choice therefore changed hierarchy and density rather than making a justified native adaptation, and its 190-DIP rail left little room for an unambiguous operational label.

## Implemented remediation

- The canonical visible labels are now `Configuração operacional` / `Operational configuration` and `Preferências` / `Preferences`.
- The same names flow from the canonical XML catalogues into generated React and WPF adapters, navigation labels, page titles, TopBar accessible text and the Tray operational-configuration shortcut. Stable route IDs, URLs, `DesktopView` values and contracts are unchanged.
- The WPF rail now uses neutral semantic surface, text and border resources. Current-route selection retains a distinct semantic background, foreground, accessible current-item status and a three-DIP action-colour rail. Keyboard focus remains a separate focus-ring state.
- The WPF navigation width is 230 DIP so the longer canonical labels remain complete without changing the 820×620 minimum window contract. The state card, summary-card radius and rail hierarchy now use the existing card and surface semantics rather than introducing new tokens.
- Design System `3.1.3` records an explicit eight-destination Web/WPF consistency matrix. It classifies mandatory equivalence, justified native adaptation and platform-exclusive presentation for every shared route.

## Automatic evidence

| Gate | Result |
|---|---|
| Localisation generation and drift verification | Passed; React and WPF adapters regenerated from `dbnotifier.localisation.v1` |
| Dashboard type-check and tests | Passed; 66/66 |
| Dashboard production build | Passed offline with installed dependencies |
| Headless Dashboard audit | Passed; 120 viewport samples and 96 forced-colours route/zoom samples across pt-BR/en-GB and Light/Dark on isolated Chrome 150.0.7871.129 |
| WPF Release build | Passed with zero warnings and zero errors using `--no-restore` |
| Focused WPF presentation architecture contracts | Passed; 12/12 |
| WPF notification-boundary tests | Passed; 3/3 |
| Shared unit tests | Passed; 393/393 |
| Proportional .NET coverage | Passed; 81.92% lines, 53.56% branches and all 10 required components present |
| Design tokens, brand and provider registries | Passed deterministic drift verification |
| Code documentation | Passed for 316 comment-capable files |
| Markdown links | Passed for 599 local links in 131 files |
| Secret scan and diff check | Passed |

The headless Dashboard matrix revalidated the R6-G1 chart containment and R6-FC1 forced-colours behaviour. WPF minimum/default dimensions, compact reflow below 1000 DIP, bounded scrolling, localisation resources, accessible names, current-item semantics, High Contrast resources, reduced-motion adapter and native DPI contracts are covered by compiled markup and non-visual automated contracts. The existing WPF audit runner necessarily exposes the secondary shell and changes its dimensions through Windows UI Automation; it was not run because this increment expressly forbids a visible human sample. Consequently, no WPF screenshot or human visual approval is inferred, and a visible W01 repetition remains mandatory.

## Preserved boundaries

The application preference file remained byte-for-byte unchanged. Its SHA-256 before and after the increment is:

`ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`

No route, URL, data contract, persisted product state, operational configuration, notification lifecycle, provider capability or administrative authority changed. Delivery, Agent Fleet, normal SignalR, dynamic provider loading, packaging and command execution remain unavailable as previously recorded.

The complete architecture suite remains non-green with 44 passing tests and only `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources` failing because it still expects `state05-dashboard-failure.json`. This is the exact pre-existing R0 contradiction and was not changed, excluded from the global result or bypassed. The R5 NuGet metadata incident also remains recorded and uncorrected; no restore or NuGet access occurred.

## Cleanup and disposition

The final audit found zero DB-Notifier-owned process, zero owned listener and zero `DBNotifier-Dashboard-Runner-*` or `DBNotifier-R6-WPF1-*` temporary root. No user browser, IDE, service, database or unrelated process was touched.

R6-WPF1 is automatically complete only as the authorised local remediation. The next permissible step is a separate proposal and explicit authorisation to repeat visibly only `R6-HV-W01` against the R6-WPF1 commit. W02 and P01 remain blocked and not tested respectively, and R6 remains unaccepted.
