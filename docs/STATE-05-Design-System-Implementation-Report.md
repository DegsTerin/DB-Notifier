# STATE-05 Design System Implementation Report

## Outcome

The first three DB-Notifier Design System increments, two `S05-HG-001` visual remediation iterations, the canonical product-mark refinements, the ultrawide shell correction, compact preference controls, Dashboard TV presentation, operational Overview, reference-aligned Dashboard/Tray refinement, WPF parity/runtime hardening, notification-area-first increment, clean-room aggregate-policy increment, transparent semantic-icon increment, cross-surface aggregate-colour increment, Light/Dark mark refinement and two small-surface legibility refinements are complete. They establish the canonical token/schema sources, deterministic React/WPF asset generation, explicit Light/Dark preference contracts and the distinct Web/Windows product roles required by Design System `2.6.3`.

The Dashboard and WPF Desktop now apply generated semantic tokens, expose one discreet translation/languages icon button and one cycling Light/Dark icon button in the upper-right TopBar and preserve the validated preferences. Localised accessible names and tooltips identify the current and next states without flags or permanently expanded groups. The translation symbol replaces the ambiguous globe, while sun/moon states replace the retired System/monitor option. WPF still gives Windows High Contrast precedence over the generated palette.

User-facing Web, WPF, Tray and installer surfaces use the display name `DB Notifier`; technical identifiers and compatibility paths retain `DBNotifier` or `DB-Notifier` as appropriate. The Dashboard main region stretches across the available desktop and ultrawide shell width. Its dedicated TV presentation requests native Fullscreen, removes navigation/filter density, enlarges the operational Overview and retains a visible collapse control, system-local clock and demonstration truth. It does not claim the external real-time integration reserved for `STATE-06`.

## International product-pattern review

The second visual iteration reviewed current official guidance rather than copying a product skin:

- [IBM Carbon's UI Shell](https://carbondesignsystem.com/components/UI-shell-header/usage/) treats the header as persistent orientation, keeps product identity to the left and global utilities to the right.
- [Grafana Saga's object-list guidance](https://grafana.com/developers/saga/templates/lists-of-objects/) distinguishes tables for open-ended exploration from lists for named objects with predictable structure, and its [table template](https://grafana.com/developers/saga/templates/table/) keeps filters adjacent to data.
- [Microsoft Fluent navigation](https://fluent2.microsoft.design/components/web/react/core/nav/usage) stays brief and scannable, while [Fluent cards](https://fluent2.microsoft.design/components/web/react/core/card/usage) organise related information through a predictable hierarchy.

DB-Notifier translates those principles into its own provider-neutral identity: code-native outlined icons replace text glyphs, selected navigation uses a restrained surface plus a narrow accent, related fleet metrics share divided bands, and language/theme controls remain visible without dominating the shell.

The human reviewer explicitly accepted the second visual refinement on 2026-07-13 and requested one follow-up identity rule: the entire active product must use a simple database image. The first filled-cylinder mark was then explicitly rejected as unattractive. Its replacement uses a lighter outlined cylinder and appears in the Dashboard header/favicon, WPF header/window, executable/shortcuts, Tray and installer without adopting any database vendor identity.

## Canonical token source

- Contract: `dbnotifier.design-tokens.v1`.
- JSON Schema restricts envelope, token-set names, key format, token types and values.
- `core.tokens.json` defines brand/neutral/status palettes, spacing, radius, control heights, typography, motion, easing and elevation primitives.
- `semantic.light.tokens.json` and `semantic.dark.tokens.json` expose identical semantic names/types for surfaces, cohesive chrome, text, borders, actions, focus, selection, status, overlay and elevation.
- `components.tokens.json` maps App Shell Chrome, App, Card, Button, Input, Focus and Status component decisions to semantic aliases.
- Strict JSON remains comment-free; semantics are owned by the official Design System specification.

## Deterministic platform generation

`scripts/generate-design-tokens.mjs` validates every source set, schema reference, alias, type, circular reference and Light/Dark parity before producing:

```text
src/DBNotifier.Dashboard.Web/src/generated/design-tokens.css
src/DBNotifier.Desktop.Wpf/Generated/DesignTokens.Core.xaml
src/DBNotifier.Desktop.Wpf/Generated/DesignTokens.Light.xaml
src/DBNotifier.Desktop.Wpf/Generated/DesignTokens.Dark.xaml
```

`tokens:generate` writes adapters; `tokens:verify` compares canonical generated content byte-for-byte and fails on drift. Every generated header carries the SHA-256 checksum of the schema and four canonical sources. CI runs verification before Dashboard typecheck/tests/build.

Generated CSS includes core custom properties plus resolved `data-theme="light"` and `data-theme="dark"` blocks. WPF output includes core primitives and resolved Light/Dark `SolidColorBrush` resources with stable keys.

`scripts/generate-brand-assets.mjs` similarly produces byte-stable SVG and nine-resolution ICO assets from one provider-neutral drawing algorithm. The canonical transparent mark and the Healthy, Warning, Critical and Unknown Windows variants share the same database silhouette and change only the bell accent. `brand:verify` compares every generated asset byte-for-byte and runs in Dashboard CI before the existing token/localisation gates.

## Preference contracts

React and .NET 10 share these exact concepts:

- Preference: `light | dark`.
- Effective theme: `light | dark`.
- Web key: `dbnotifier.theme.preference.v1`.
- UI preference schema: `dbnotifier.ui-preferences.v1`.
- WPF file name: `ui-preferences.v1.json`.
- Invalid, missing or retired `system` data fails safely to Light.
- Explicit Light/Dark remains stable across operating-system colour-mode changes.

The React adapter accesses only versioned local UI preference storage and the document root. WPF stores the validated language and selected theme together in the versioned current-user file; locale-only, invalid and retired System documents migrate safely to Light without losing the language.

## React theme runtime

- A blocking local bootstrap runs in the document head before application styles can paint, validates the persisted preference and applies `data-theme` plus `data-theme-preference` without a wrong-theme flash.
- Storage access, invalid values and retired System values fail safely to Light without interrupting rendering.
- `ThemeSelector` uses one code-native sun/moon icon button, cycles Light → Dark, applies immediately and exposes the current/next preference through its localised accessible name, tooltip and polite announcement.
- Operating-system colour-mode changes do not alter the explicit preference.
- Cross-tab storage changes accept only validated preference values.
- Route, filters, modal state and other React state remain owned by their existing components and are not reset by a theme change.
- Generated CSS loads before feature CSS. Hand-written feature styles contain no raw colour values or shadows and use canonical spacing/radius tokens for their corresponding declarations.
- At `1100` CSS px and below, the shell switches from the persistent side navigation to labelled horizontal navigation and replaces the seven-column inventory table with complete operational cards. The previous compressed `82` px rail and clipped `980` px table no longer exist.
- Summary cards use restrained surface elevation and compact status markers rather than heavy status-coloured top borders; preference groups use quiet chrome surfaces rather than outlined containers.
- The second refinement consolidates fleet summaries into one responsive metric band, replaces navigation/summary text glyphs with a coherent SVG icon set and reduces selected-preference intensity while retaining the tested contrast pairs.

## WPF theme runtime

- The application loads core and Light generated dictionaries before constructing the window, then atomically replaces only the semantic theme dictionary.
- Only explicit Light and Dark are selectable; old System values migrate to Light without a Windows colour-mode observer.
- Windows High Contrast takes precedence and maps semantic resources to live system brushes without persisting an effective theme.
- One atomic, bounded `%LocalAppData%\DB-Notifier\ui-preferences.v1.json` document stores only schema, locale and selected theme; failures remain session-local.
- Main window chrome, surfaces, cards, inputs, buttons, tables, statuses and footer consume generated `DynamicResource` keys. Custom Button/ComboBox/DataGrid selection templates retain legibility in Dark instead of inheriting incompatible native Light colours.
- WPF uses the same translation/languages icon button and one current-theme icon button at the upper-right of the TopBar; UI Automation names expose current/next states, and complete two-state cycles restore the starting preferences.

## Operational Overview

- Web and WPF now open on a provider-neutral operational Overview before the detailed Inventory, History/Alerts and Configuration views.
- The Overview composes the existing deterministic fixture into a metric band, fleet-status list, recent-alert list, accessible demonstration trend and provider-distribution fixture; it does not introduce a second data source.
- Status remains textual and symbolic. Sparklines and the trend are explicitly decorative/demonstrative and never presented as external telemetry.
- Provider distribution describes fixture composition only and never implies implementation, homologation or public support.
- Dashboard TV presentation uses the same complete Overview rather than a separate, potentially divergent screen.

## Operational Tray flyout

- The minimal native Tray menu is replaced by a compact token-driven WPF flyout using the canonical database mark, Light/Dark resources and generated `pt-BR`/`en-GB` copy.
- A two-column scan places four provider-neutral demonstration rows and their healthy, degraded, timeout and stale states on the left, with safe shortcuts to Overview, Inventory, History/Alerts and non-secret Configuration on the right.
- The header and exact timestamp identify demonstration data and a local snapshot rather than implying an external observation.
- Restart is deliberately rendered as a non-interactive unavailable explanation. It does not dispatch a command and states the future capability, authorisation, confirmation and audit prerequisites.
- Left-click activation and the Windows secondary-click menu path both route to the same flyout; Escape, focus loss and repeated activation dismiss it. The first close-to-Tray notification remains native to Windows.

## Notification-area-first Windows role

- Normal WPF startup creates the Windows notification icon without showing the full desktop shell or an ordinary taskbar window.
- Activating the icon opens the compact fleet flyout as the primary Windows interaction. Its safe Dashboard action opens the existing full WPF shell as a secondary drill-down.
- `--show-desktop` is a bounded development/accessibility-audit override. The reproducible WPF audit uses it explicitly; unknown arguments retain the notification-area default.
- The hierarchy is conceptually informed by Oracle's archived MySQL Notifier taskbar workflow. DB Notifier does not reuse Oracle code/artwork and rejects automatic name-filter discovery, WMI/DCOM/firewall mutation and unconditional service control.
- Status-change notifications, authorised Agent/API state and aggregate icon updates remain integration work in `STATE-06`. Start/Stop/Restart remain unavailable until their exact capability and authorisation path is implemented and homologated in `STATE-07`.
- Runtime smoke evidence observed a zero normal-startup main-window handle, found and activated the real DB Notifier `NotifyItemIcon`, opened the localised flyout and then the secondary shell, with a responsive process and no `.NET` dialogue.

## Clean-room aggregate policy

- The product owner selected a clean-room implementation based on public behaviour, preserving the MIT licence and DB Notifier identity. No Oracle/MySQL source, binary, artwork, logo, trade dress, product copy or vendor-specific architecture is incorporated.
- Application-owned policy maps canonical health and freshness to `Healthy`, `Warning`, `Critical` and `Unknown` without provider-name branches. Stale evidence is always unknown; aggregate precedence is `Critical > Warning > Unknown > Healthy`, and an empty fleet is unknown.
- The STATE-05 deterministic flyout and tooltip expose the aggregate text while retaining demonstration/source/freshness truth. The notification-area icon selects a transparent Healthy/Warning/Critical/Unknown asset from that deterministic aggregate; the current fixture is Critical and therefore selects the red-bell variant.
- A pure future-delivery policy suppresses the initial snapshot and requires explicit opt-in plus a materially changed reconciled summary. It does not deliver notifications; authorised Application/Agent integration and Windows delivery remain `STATE-06`.

## Transparent semantic notification icon

- The former blue rounded tile and the later dark cylinder-body fill were removed. The larger outlined database and lower-right bell now render directly on a fully transparent canvas and interior so the active Light, Dark or Windows notification-area surface supplies the surrounding colour.
- Bell accents use the canonical non-colour-redundant aggregate states: green for Healthy, yellow for Warning, deep red `#C62828` for Critical and grey for Unknown. The tooltip and flyout continue to expose the state in text, so colour is not the only evidence.
- The Dashboard, design-system source, executable/application icon and four semantic Tray variants are generated by the same clean-room algorithm. No Oracle/MySQL artwork, logo or binary is incorporated.
- In `STATE-05`, selection is proved only against the labelled deterministic fixture. Updating the icon from reconciled Agent/API observations and delivering Windows status-change notifications remain `STATE-06` work.

## Cross-surface aggregate colour

- Dashboard header and favicon now derive their SVG from the same freshness-aware `Critical > Warning > Unknown > Healthy` aggregate used by the Windows policy. The current deterministic fixture selects Critical/red and may move to Warning or Unknown only as its evidence becomes stale.
- Main WPF header, window/title/taskbar, flyout header and NotifyIcon receive the same `TrayFleetSummary.State` instance. No WPF surface retains the old green default when the shared fixture is Critical.
- Static executable, installer, shortcut and Design System files cannot observe runtime evidence and therefore use the neutral Unknown/grey asset. They never imply Healthy merely because the application is installed.
- Language, theme and navigation do not affect bell colour. Real Agent/API state replacement remains an authorised `STATE-06` integration and is not claimed by this presentation increment.

## Light/Dark mark refinement

- The first synchronised Critical sample exposed two human visual findings: `#EB4C4C` appeared orange and the opaque dark cylinder body conflicted with Light mode.
- Design System `2.6.1` deepens Critical to `#C62828` and eliminates the body-fill layer from both vector and multi-resolution Windows raster generation. The blue database outline and white bell outline remain unchanged for recognition at small sizes.
- The `2.6.1` regression rejected the old fill and colour and initially used a broad transparency percentage as supporting evidence. The current regression uses the stronger invariant that a representative cylinder-interior pixel has alpha zero; keyline coverage is assessed separately and intentionally increases visible edge pixels. Visual acceptance in both themes remains a human decision in `S05-HG-010`.

## Small-surface legibility refinement

- The Light Windows sample showed that transparent assets were technically correct but the bright-blue outline alone became pale over white selection surfaces, while sub-pixel strokes made the 16 px title/notification-area asset resemble a document.
- Design System `2.6.2` adds a deep-navy `#0F2940` keyline behind the blue database and white bell strokes. The keyline is visible against Light surfaces and visually recedes behind the brighter foreground strokes on Dark surfaces; it does not fill the cylinder or add an enclosing tile.
- Raster generation enforces resolution-aware minimum stroke radii. The 16/20 px variants omit the middle seam, while 24 px and larger variants retain the full geometry. Regression evidence requires at least 150 substantially opaque pixels in each 16 px ICO, alpha zero at a representative database-interior point and more than half of the 256 px canvas fully transparent.
- The Critical bell remains `#C62828`; provider-neutral aggregation, user-facing state text, WPF/Web bindings and `S05-HG-011` workflow are unchanged.

## Pixel-hinted small-icon refinement

- The next authorised sample showed that the `2.6.2` mark remained less sharp than neighbouring Windows icons in the taskbar, notification overflow, title bar and browser tab. The keyline improved contrast, but multi-layer antialiasing still produced pale edge pixels and the simplified cylinder lost a recognisable database seam.
- Design System `2.6.3` keeps the transparent canvas, deep-navy keyline and Critical `#C62828`, but gives the 16–32 px raster family a dedicated compact treatment. Database strokes use Windows blue `#0078D4`, the seams remain visible and the bell becomes a solid semantic silhouette rather than a white-outline stack.
- The 16/20 px entries use pixel-aligned coverage with no partially transparent edge pixels; 24 px uses bounded two-by-two coverage and 32 px keeps smooth coverage with the compact palette. Larger Windows and Web header marks retain the detailed vector treatment.
- Four-resolution semantic ICOs now drive the browser favicon independently from the full SVG header mark. The favicon and Windows families are still generated from the same geometry, aggregate state and canonical generator.

## Verification

| Gate | Result |
|---|---|
| Token generation | Approved; four adapters generated |
| Deterministic drift verification | Approved; neutral defaults plus four semantic SVG, four-resolution favicon ICO and nine-resolution Windows ICO variants |
| Schema/reference/type/theme parity | Approved |
| Light/Dark canonical contrast pairs | Approved; all tested pairs `>= 4.5:1` |
| Dashboard typecheck | Approved |
| Dashboard tests | Approved; 33/33, including cross-surface aggregate precedence, Overview, Tray structure/safety and system-time regression guards |
| Dashboard production build | Approved |
| .NET 10 Release build | Approved; 0 warnings/errors |
| .NET tests | Approved; 135 unit/model/provider/presentation + 7 architecture = 142/142 |
| .NET format verification | Approved |
| Legacy compatibility | Approved; 10/10 Pester and bundle validation |
| Dependency audit | Approved; no npm or NuGet vulnerabilities reported |
| Documentation gate | Approved; 174 comment-capable source files and 179 local Markdown links in 61 files |
| React visual/responsive matrix | Approved; 96 locale/theme/viewport-route samples across the four current locale/theme combinations and 320–1920 CSS px |
| React overflow/accessibility | Approved; 0 global overflow and 0 unnamed interactive controls in all four locale/theme combinations |
| Preference cycles | Approved; Web and WPF restored both locales and both themes after two activations in all four combinations, with 0 unnamed interactive/focusable controls |
| Dashboard TV mode | Approved; four `1920×1080` locale/theme samples entered native Fullscreen, retained demonstration truth and the exit control, hid navigation/filters, displayed the complete Overview and restored the standard shell; the unavailable-Fullscreen fallback remains covered |
| WPF UI Automation/visual matrix | Approved; eight locale/theme/size combinations at `1180×760` and `820×620`, each with 16 visible focusable controls, none unnamed and 12 contained Tab steps; earlier High Contrast evidence remains separate |
| Tray flyout runtime | Approved automatically in its local fixture scope; normal startup exposed no main window, the real notification icon opened the localised flyout, its safe action opened the secondary shell, the process remained responsive and no `.NET` dialogue appeared. Bruno approved the bounded `S05-HG-011` human sample on 2026-07-15; the overall Human Gate remains pending |
| Lighthouse regression | Approved; 16 current eight-route mobile/desktop reports, Accessibility and Best Practices `100` in `16/16`, Performance `99`–`100`; SEO `66` remains the intentional internal-console crawler policy |

## Security and phase boundaries

- Theme data is presentation-only and contains no user, provider, endpoint, credential or infrastructure data.
- Browser persistence contains only the validated Light or Dark preference under `dbnotifier.theme.preference.v1`.
- Storage failures and retired System values are contained locally through the Light fallback and do not relax any product authorisation boundary.
- Generated adapters do not connect to a database, Agent, API, IdP, vault or administrative executor.
- No provider support or homologation state changed.
- `STATE-05` remains active; Human Gate, `STATE-06` and the multi-database laboratory remain blocked.

## Remaining increments

1. Confirm the remediated operational Overview and two-column Tray flyout together with the database-and-bell mark, green/white `DBNotifier` visual wordmark, eight-item navigation, TopBar alert/Settings actions, KPI cards, provider/status rows, charts and safe unavailable actions in visible sessions.
2. Obtain explicit consent, then complete the keyboard/Narrator portion of `HG05-01`, native browser zoom, Windows scaling, High Contrast and remaining visual samples.
3. Present the explicit Human Gate decision; no lifecycle transition occurs automatically.

## Recommendation

Confirm the remediated operational Overview and two-column Tray flyout together with the database-and-bell mark, eight-item navigation, TopBar actions, KPI/status/chart treatment, ultrawide/TV layouts, accessible `DB Notifier` name, visual `DBNotifier` wordmark, translation icon and Light/Dark-only control in visible sessions. Then obtain explicit consent before starting Narrator and continuing the remaining samples in [`STATE-05-Human-Gate-Validation.md`](STATE-05-Human-Gate-Validation.md).
