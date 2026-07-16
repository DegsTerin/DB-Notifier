# STATE-05 Design System Implementation Report

## Outcome

The first three DB-Notifier Design System increments, two `S05-HG-001` visual remediation iterations, the canonical product-mark refinements, the ultrawide shell correction, compact preference controls, Dashboard TV presentation, operational Overview, reference-aligned Dashboard/Tray refinement, WPF parity/runtime hardening, notification-area-first increment, clean-room aggregate-policy increment, transparent semantic-icon increment, cross-surface aggregate-colour increment, Light/Dark mark refinement, twelve small-surface legibility/notification refinements and the native WPF chrome correction are complete. They establish the canonical token/schema sources, deterministic React/WPF asset generation, explicit Light/Dark preference contracts and the distinct Web/Windows product roles required by Design System `2.6.14`.

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

`scripts/generate-brand-assets.mjs` similarly produces byte-stable SVG and nine-resolution ICO assets from one provider-neutral geometry contract. SVG, favicon and Windows output share the same database-and-bell silhouette, proportions, seams and transparent interior; semantic variants change only the bell accent, while raster resolution changes only sampling density. `brand:verify` compares every generated asset byte-for-byte and runs in Dashboard CI before the existing token/localisation gates.

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
- The standard Windows caption remains native and, on supported Windows 11 builds, synchronises its DWM-managed background and text from the current semantic resources after handle creation and on every theme change. Windows retains the active/inactive border, High Contrast returns caption colours to Windows defaults, and Windows 10 retains its native caption.
- One implicit token-driven `ScrollBar` style covers application `ScrollViewer`, `DataGrid`, `ComboBox` and popup scrollbars. Its vertical and horizontal templates retain `PART_Track`, line/page commands and orientation while replacing the fixed light Aero presentation.
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
- Left-click activation and the Windows secondary-click menu path both route to the same flyout; Escape, focus loss and repeated activation dismiss it. Every explicit close of the secondary shell requests a native Windows availability confirmation; minimise, hidden startup, Show and Exit do not.

## Notification-area-first Windows role

- Normal WPF startup creates the Windows notification icon without showing the full desktop shell or an ordinary taskbar window.
- Activating the icon opens the compact fleet flyout as the primary Windows interaction. Its safe Dashboard action opens the existing full WPF shell as a secondary drill-down.
- `--show-desktop` is a bounded development/accessibility-audit override. The reproducible WPF audit uses it explicitly; unknown arguments retain the notification-area default.
- The hierarchy is conceptually informed by Oracle's archived MySQL Notifier taskbar workflow. DB Notifier does not reuse Oracle code/artwork and rejects automatic name-filter discovery, WMI/DCOM/firewall mutation and unconditional service control.
- Status-change notifications, authorised Agent/API state and aggregate icon updates remain integration work in `STATE-06`. Start/Stop/Restart remain unavailable until their exact capability and authorisation path is implemented and homologated in `STATE-07`.
- Runtime smoke evidence observed a zero normal-startup main-window handle, found and activated the real DB Notifier `NotifyItemIcon`, opened the localised flyout and then the secondary shell, with a responsive process and no `.NET` dialogue.
- Design System `2.6.13` preserves the app-notification publisher introduced in `2.6.12`: Windows App SDK auto-bootstrap is disabled and explicit fail-safe Windows App Runtime initialisation is attempted. When runtime/platform/asset/registration/publication is available, `AppNotificationManager` registers the transparent availability PNG under display name `DB Notifier`; otherwise the single legacy `ShowBalloonTip` path remains available without blocking notification-area-first startup. The modern notification uses a stable tag/group, five-minute expiration and reboot expiration, and activation accepts only the local `action=show` route to reveal the secondary shell. Windows and Focus Assist may still suppress visible presentation.

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

## Compact single-layer shell-mark refinement

- The visible `2.6.3` sample proved that the Windows notification itself was clearer, but the browser tab, title bar, taskbar, notification overflow and product headers still looked softer than adjacent platform icons. The same sample also exposed the technical attribution `DBNotifier.Desktop.Wpf` in the Windows notification.
- Design System `2.6.4` removes the remaining deep-navy/white outline stack from the active vector and raster families. One Windows-blue `#0078D4` cylinder outline and one solid semantic bell/clapper now form the complete transparent mark; Critical remains `#C62828`.
- All 16/20/24/32 px entries now use binary, pixel-aligned alpha. The 16/20 px entries omit the middle seam, 24 px and larger restore it, and only the 40 px and larger entries retain bounded antialiasing. The canonical SVG uses the same simplified geometry so Dashboard and WPF headers no longer retain the detailed multilayer form.
- WPF assembly metadata now exposes `DB Notifier` as both File Description and Product Name while retaining the technical assembly filename. This provides the Windows shell with the canonical visual attribution without renaming namespaces or compatibility identifiers.

## Native-frame WPF shell refinement

- The next visible sample confirmed that `2.6.4` improved the mark but did not yet match neighbouring Windows icons. Inspection proved that WPF's default multi-resolution ICO path selected the 128 px frame and downsampled it into the 30–40 px WPF surfaces. This discarded the dedicated binary-alpha small frames even though the canonical ICO already contained them.
- Design System `2.6.5` decodes the nearest ICO frame explicitly. The full-shell header uses 40 px, the compact flyout uses 32 px, and both image hosts use `Stretch=None` plus device-pixel snapping. The notification-area icon requests the platform small-icon metric rather than accepting the decoder default.
- The WPF window now assigns independent native small/title and large/taskbar handles with `WM_SETICON`. The handles are retained for the window lifetime and released on close, preventing Windows from deriving both roles from one WPF bitmap.
- The SVG/ICO geometry, transparent canvas, provider-neutral aggregate mapping and Critical `#C62828` remain unchanged. This is a rendering-path correction, so `S05-HG-011` remains closed while comparative sharpness remains a human decision within `S05-HG-010`.

## Unified canonical-mark refinement

- The authorised `2.6.5` repetition proved that native frame selection alone did not make every occurrence visually identical. Screenshots still exposed different cylinder/bell models across the browser tab/header, WPF title/header/flyout, taskbar, notification overflow and Windows notification surfaces.
- Inspection found that vector and raster output used separate geometry paths, small raster branches could omit a database seam, and coverage-weighted edge channels were stored in a straight-alpha DIB. Those differences could change the silhouette or produce a dark/grey fringe when Windows composited the icon.
- Design System `2.6.6` makes one immutable geometry definition drive both SVG paths and raster coverage. Every active resolution retains the same cylinder, both seams, bell/clapper, proportions, transparent canvas/interior and semantic alpha mask. Healthy, Warning, Critical and Unknown differ only through the bell colour; resolution changes only the sampling density.
- The 16/20/24 px raster entries use bounded 2× supersampling and 32 px or larger entries use 4× supersampling. Straight-alpha BGRA stores full canonical colour in partially covered pixels and expresses coverage only through alpha, preventing premultiplied-looking dark edges. The 16/20/24/32 px browser favicon payloads are byte-identical to their corresponding Windows ICO frames.
- The retained functional PowerShell compatibility client no longer draws its former GDI logo. It loads the same generated semantic ICO family, fails safely to Unknown when state is unsupported and packages those assets beside the compatibility executable; the installer consumes that package instead of introducing another mark model. The WPF first-hide balloon also stops requesting the unrelated native information glyph, leaving Windows attribution on the canonical application mark.
- Native WPF frame selection now converts the 32/40 DIP hosts through the owning visual's effective DPI. It chooses an exact frame or the smallest next-larger frame, rather than upscaling a fixed 32/40 px source above 100%; Windows notification/title/taskbar roles retain their native metric handling. This is a rendering correction, not a change to notification-area-first behaviour or provider-neutral state mapping; `S05-HG-011` remains approved. The later `REQ-065` review accepted the resulting cross-surface mark geometry on 2026-07-16, while the other independent `S05-HG-010` details remain pending.

## Semantic favicon and Windows-notification source refinement

- The authorised `2.6.6` repetition showed that the browser tab and Windows first-hide notification remained visually different while header, WPF, taskbar and notification-area occurrences were consistent. The Dashboard fixture and generated assets were audited before changing code: its initial aggregate is Critical, the Critical favicon contains the canonical `#C62828` bell and the production/public assets are byte-identical. Those facts rule out a Healthy current payload and are consistent with a stale Chromium page-to-favicon association; the browser's internal cache decision was not independently observable.
- Design System `2.6.7` moves state/revision path construction into one typed helper used by both header and favicon. The initial HTML remains Unknown, but React uses `useLayoutEffect` to atomically replace identified or legacy favicon candidates with a state-and-revision-specific node instead of mutating an old link's `href` or leaving competing nodes. The replacement records the aggregate explicitly and exposes the four available ICO sizes, forcing a new deterministic candidate evaluation without a temporal cache buster and reducing the risk of historical association reuse.
- Under Design System `2.6.7`, the WPF first-hide notification retained the approved WinForms `NotifyIcon.MouseClick` lifecycle and `ToolTipIcon.None`; no stock information/error glyph or second mark was introduced. Immediately around the synchronous Shell notification call, the controller supplied the 32 px native frame for the same aggregate and restored the Windows small-icon frame in `finally`. That increment offered the Shell a sharper state-correct source without changing Tray activation, provider policy or notification delivery semantics.
- Windows still owns the top attribution glyph, its scale, notification snapshots and identity cache. WinForms `ShowBalloonTip` does not expose the Win32 `hBalloonIcon`/`NIIF_USER` custom-icon path, and modern Windows attribution cannot be truthfully promised as dynamically replaceable. Design System `2.6.7` therefore improved the then-controllable source while preserving this platform limit; the subsequent `2.6.8` exception is documented below, and change-only application notifications plus state text/content remain `STATE-06` work.

## Fixed green availability-notification refinement

- The authorised `2.6.7` repetition confirmed the Windows platform limit and narrowed the first-hide requirement: this specific message should show the transparent product mark with a green bell wherever the Shell accepts the supplied source. The message confirms that DB Notifier remains available after its window is hidden; it does not report database or fleet health.
- Design System `2.6.8` gives the controller a separate `availabilityNotificationIcon`, loaded from the canonical transparent Healthy-colour frame only for the synchronous `ShowBalloonTip` call. The factual aggregate `applicationIcon` remains the notification-area state and is restored in `finally`, including when notification delivery raises an exception.
- No geometry, generated asset family, Tray activation, aggregate precedence or provider capability changes. Future state-bearing notifications remain assigned to `STATE-06` and must use factual state plus explicit text. Windows still decides the final attribution glyph, scale, cache and historical notification snapshot.

## Per-notification semantic selection

- The product owner clarified that every new notification must use the canonical mark whose bell corresponds to that notification's own meaning. Design System `2.6.9` adds the provider-neutral `TrayNotificationMeaning` and `TrayNotificationPresentationPolicy` contracts: availability or recovery resolves to green, warning to yellow, critical to deep red, and informational, unknown or invalid input to neutral grey.
- The existing first-hide confirmation now obtains its green frame through this typed policy rather than a direct Healthy-state constant. Its meaning remains application availability, not fleet health. The state-bearing Tray icon is still selected independently from the aggregate summary and restored in `finally`.
- `STATE-05` implements and tests presentation selection only and retains exactly one local `ShowBalloonTip` call. Agent/API integration, event-transition classification, serialised Windows delivery, opt-in preferences, deduplication, quiet policy and end-to-end notification evidence remain assigned to `STATE-06`. Future notification title/body content must identify the affected instance and state because colour alone is insufficient and Windows may retain its own attribution/cache.

## Legacy Windows transparency-mask refinement

- The authorised `2.6.9` screenshot showed an apparent backing square around the tiny Windows notification attribution mark. Inspection of every ICO frame proved that its BGRA corners and canvas were already fully transparent, but the canonical generator wrote an all-zero legacy AND mask. A Windows consumer that falls back to this mask can therefore treat the complete icon rectangle as opaque even when the 32-bit alpha channel is correct.
- Design System `2.6.10` derives each ICO AND-mask bit from the corresponding bottom-up BGRA pixel. Every fully transparent pixel is masked out; visible and partially covered pixels remain unmasked and continue to use straight alpha. The five browser-favicon ICOs and five Windows ICOs were regenerated from the same canonical geometry without changing silhouette, state colours or notification-meaning selection.
- A focused parser verified all nine Windows frame sizes: transparent-pixel and masked-pixel counts match exactly, with zero disagreements in 16, 20, 24, 32, 40, 48, 64, 128 and 256 px. This removes the application-owned opaque canvas fallback. Windows still owns the notification card surface, attribution scale/cache and historical snapshots; the card itself is not and cannot truthfully be described as transparent by this WPF path.

## Native small-frame silhouette and notification-capture refinement

- The newly delivered `2.6.10` notification still looked like a blue backing block to the reviewer. Pixel inspection distinguished perception from file structure: the 16 px frame already had a transparent canvas and a correct legacy mask, but its `4.5`-unit database stroke occupied `63.09` alpha-weighted blue pixel equivalents and left only `137/256` pixels fully transparent. The tiny outline was therefore visually too dense even though no blue background rectangle existed in the asset.
- Design System `2.6.11` narrows the canonical database stroke to `3.5` units while retaining the same open cylinder, both seams, bell geometry, state colours and transparent interior. Its 16 px frame leaves `155/256` pixels fully transparent, contains `90` pixels at alpha `>=128` and reduces alpha-weighted blue coverage to `42.84` pixel equivalents. The result remains strong enough to identify at native size while exposing materially more open canvas.
- The WPF first-hide notification now loads the availability source at `SystemInformation.SmallIconSize` instead of loading a 32 px source for Shell downscaling. Because WinForms exposes one icon slot, it keeps that green availability frame assigned only until `BalloonTipShown` reports that Windows displayed the balloon, then restores the factual aggregate Tray icon. A provider-neutral lease policy makes begin, shown, fallback, failure and disposal paths explicit; a bounded two-second dispatcher fallback handles a suppressed callback, while a synchronous exception attempts restoration immediately. This best-effort restoration point prevents the former immediate race without claiming that every Windows build captured the attribution glyph or changing the fleet summary, flyout text or persisted state.

## Optical micro-glyph and repeatable Windows app-notification refinement

- The fresh `2.6.11` sample remained perceptually unacceptable to the reviewer: the tiny attribution glyph still read as a blue block, and only the first close of the secondary WPF shell produced a visible confirmation. This is the sixteenth `S05-HG-010` finding; it does not invalidate the transparent alpha/mask facts already established for `2.6.11`, approve the replacement or reopen `S05-HG-011`.
- Design System `2.6.12` gives 16/20/24 px frames a deterministic optical micro-glyph with binary alpha and pixel-aligned database/bell cells. Frames at 32 px and above retain the canonical curved geometry with four-by-four antialiasing and straight alpha. The notification identity is a generated transparent 64 px PNG with the green availability bell, registered explicitly as `DB Notifier` through `AppNotificationManager` rather than relying on the legacy balloon's attribution capture.
- The WPF project disables Windows App SDK auto-bootstrap. `WindowsAppNotificationPublisher` attempts explicit runtime initialisation, registers only when the runtime, platform and identity asset are available, records sanitised local fallback stages, and shuts the runtime down safely. Registration or publication failure returns to the only retained `ShowBalloonTip` call; that fallback still uses the bounded semantic-icon lease documented in `2.6.11`.
- `TrayPresentationPolicy` now requests a new availability confirmation for every explicit `CloseRequest`. Minimise, hidden startup, Show and Exit remain negative paths. The modern entry uses stable `window-hidden`/`local-availability` tag/group identity, expires after five minutes and on reboot, so a second close replaces the prior availability entry rather than accumulating stale cards. Activation validates `action=show` and reveals only the local secondary shell; no database, provider, service or infrastructure action is dispatched.
- Windows Shell, notification policy and Focus Assist remain authoritative. A successful `Show` request is not evidence that a banner was displayed, the PNG transparency does not make the Windows-owned card transparent, and human acceptance requires two fresh consecutive Show → CloseRequest cycles in a newly started review process.

## Single-geometry restoration across every icon size

- The authorised two-cycle `2.6.12` review confirmed, in the validator's words, that the previously requested transparency and repeatability were now as intended. That confirmation closes only those two local `REQ-064` details; it does not approve the complete `S05-HG-010` sample or Human Gate. An intermediate native-window re-show attempt was discarded because it did not exercise the WPF state transition.
- The same screenshots exposed a new objective inconsistency: the binary 16/20/24 px optical grid read as a different database model from the canonical WPF header mark. Design System `2.6.13` therefore removes the second grid, its dedicated renderer and nearest-grid expansion. Every 16–256 px frame now derives from the same `markGeometry`, including the complete ellipse, both seams, bell, clapper, proportions and transparent silhouette.
- All raster sizes use the existing four-by-four coverage model and straight-alpha colour. Semantic variants retain byte-identical alpha masks at the same size and differ only in bell colour. The notification PNG, Windows app-notification publisher, fallback and explicit CloseRequest policy are unchanged, isolating the visual correction from the behaviour already confirmed by the validator.
- At 16 px, the canonical frame contains `134/256` fully transparent pixels, `112` partially covered pixels and both blue/semantic layers. Small output is necessarily denser and more antialiased than the retired grid. In the authorised independent-Chrome/WPF review on 2026-07-16, Bruno compared the real browser, WPF and Windows shell roles and responded exactly `Ícone único APROVADO`; this closes the perceptual requirement of `REQ-065` without approving the remaining `S05-HG-010` details or the `STATE-05` Human Gate. Any future adjustment must refine the shared geometry or stroke contract rather than introduce another size-specific mark.

## Native caption and scrolling theme correction

- The nineteenth visible `S05-HG-010` review exposed two theme leaks in WPF: the Windows-managed caption remained white in Dark and the default WPF scrollbar remained light. The screenshots are treated as a valid `REQ-066` finding rather than as approval of the surrounding Overview.
- Design System `2.6.14` introduces an isolated native-window theme policy. On Windows 11 build 22000 or later it applies documented immersive-dark, caption and text DWM attributes only after `SourceInitialized`, reapplies them after every explicit theme change and ignores unsupported attributes without replacing the standard Windows caption. Native imports are restricted to `System32`, translucent resources fall back to DWM defaults, the active/inactive border remains Windows-owned and entering High Contrast restores `DWMWA_COLOR_DEFAULT` so Windows retains accessibility ownership. Windows 10 intentionally keeps its native caption.
- `Resources/ControlStyles.xaml` supplies one implicit Light/Dark/High-Contrast-aware scrollbar style. Track, thumb, border and interaction states consume existing semantic `DynamicResource` brushes; vertical and horizontal templates preserve `PART_Track` plus every line and page command. Parser-owned grid lengths use typed literals, avoiding the deferred numeric-resource failure previously observed in the Tray flyout.
- Focused automatic evidence comprises a zero-warning/error WPF Release build and `10/10` architecture tests. Visible Light → Dark → Light comparison at default/minimum size, including scrollbars in the main `ScrollViewer`, a `DataGrid` and `ComboBox`, remains a required human sample. High Contrast was not reopened because that Windows setting requires separate consent.

## Verification

| Gate | Result |
|---|---|
| Token generation | Approved; four adapters generated |
| Deterministic drift verification | Approved; neutral defaults plus four semantic SVG, four-resolution favicon ICO, nine-resolution Windows ICO variants and the transparent 64 px Windows availability PNG |
| Unified mark geometry | Approved in the focused regression; SVG and every 16–256 px ICO frame consume the same geometry contract, retain the complete cylinder ellipse and both seams, and use identical alpha masks across the four semantic variants |
| Cross-size proportional audit | Approved independently for 27 size-by-layer combinations against the 256 px reference; the maximum normalised bounds deviation was `0.050781` at 16 px and the maximum centroid deviation was `0.003089`, both within raster tolerance. All corners remain transparent |
| Straight-alpha raster composition | Approved in the focused regression; partial-alpha edges retain canonical colour channels, and 16/20/24/32 px favicon payloads match the corresponding Windows frames byte-for-byte |
| Legacy ICO transparency mask | Approved in the focused regression; every fully transparent BGRA pixel is set in the legacy AND mask, every visible/partially covered pixel remains unset and all nine Windows frame sizes report zero disagreements |
| Single raster model at every size | Approved in the focused regression; 16/20/24 px no longer have a literal grid or second renderer, every size uses canonical four-by-four coverage and straight alpha, and same-size semantic variants preserve identical alpha masks |
| Native WPF frame selection | Approved structurally; DIP-to-physical DPI conversion, exact/next-larger decoding, largest-only terminal fallback, native Windows small-icon metric for Tray and the legacy notification fallback, separate title/taskbar roles and no-stretch pixel-snapped hosts are covered by regression tests |
| Windows app-notification publisher | Approved structurally; auto-bootstrap is disabled, explicit fail-safe bootstrap/register/show/unregister/shutdown paths exist, attribution uses display name `DB Notifier` plus the transparent PNG, tag/group/five-minute/reboot expiry are bounded, activation accepts only `action=show`, and `ShowBalloonTip` remains the single fallback. Visible Shell delivery is not inferred |
| Windows display metadata | Approved; Release executable File Description and Product Name are both `DB Notifier`, while the technical filename remains unchanged |
| Schema/reference/type/theme parity | Approved |
| Light/Dark canonical contrast pairs | Approved; all tested pairs `>= 4.5:1` |
| Dashboard typecheck | Approved |
| Dashboard tests | Approved; 34/34 after `2.6.13`, including the single-geometry source guard across all raster sizes, transparent notification PNG generation, atomic semantic-favicon replacement, cross-surface aggregate precedence, Overview, Tray structure/safety and system-time regression guards |
| Headless semantic favicon | Approved after `2.6.13` in 96 samples on isolated alternative ports: pt-BR/en-GB × Light/Dark × 24 viewports; every active DOM used the Critical favicon expected from the fixture. The already occupied port `4173` was not disturbed |
| Dashboard production build | Approved |
| .NET 10 Release build | Approved; 0 warnings/errors |
| .NET tests | Approved; 143 unit/model/provider/presentation + 10 architecture = 153/153, including native-caption/scrollbar theme structure, two consecutive Show → CloseRequest confirmation decisions and the Windows publisher/bootstrap contract |
| .NET format verification | Approved |
| Legacy compatibility | Approved; 11/11 Pester plus bundle validation, including canonical semantic ICO loading/packaging instead of GDI drawing |
| Dependency audit | Approved; the NuGet recheck after the Windows App SDK dependency reported no known vulnerabilities, and the last recorded npm audit also reported none. The runtime/deployment prerequisite remains bounded to fail-safe local fallback now and installer ownership in `STATE-08` |
| Documentation gate | Approved; 178 comment-capable source files and 182 local Markdown links in 61 files |
| Fail-closed runtime | Approved; the existing API/Agent security smoke retained its expected closed defaults and responses |
| React visual/responsive matrix | Approved after `2.6.13`; 96 pt-BR/en-GB × Light/Dark × viewport/route samples across 24 viewports, including 320–1920 CSS px, with the expected Critical favicon in every sample |
| React overflow/accessibility | Approved; 0 global overflow and 0 unnamed interactive controls in all four locale/theme combinations |
| Preference cycles | Approved; Web and WPF restored both locales and both themes after two activations in all four combinations, with 0 unnamed interactive/focusable controls |
| Dashboard TV mode | Approved; four `1920×1080` locale/theme samples entered native Fullscreen, retained demonstration truth and the exit control, hid navigation/filters, displayed the complete Overview and restored the standard shell; the unavailable-Fullscreen fallback remains covered |
| WPF UI Automation/visual matrix | Historical matrix approved for eight locale/theme/size combinations at `1180×760` and `820×620`, each with 16 visible focusable controls, none unnamed and 12 contained Tab steps. It predates `2.6.14`; visible native-caption/scrollbar confirmation and the earlier separate High Contrast boundary remain pending |
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

1. Obtain explicit consent and repeat the focused `REQ-066` WPF Light → Dark → Light caption/scrollbar sample at default and minimum size. Then complete the remaining operational Overview/TopBar/wordmark visual details and the keyboard/Narrator portion of `HG05-01`, native browser zoom, Windows scaling and High Contrast samples. Do not repeat the accepted `REQ-064/065` icon samples unless their owning implementation changes.
2. Present the explicit Human Gate decision; no lifecycle transition occurs automatically.

## Recommendation

First repeat the Design System `2.6.14` WPF native-caption/scrollbar correction under fresh consent, without opening Chrome or changing Windows accessibility settings. Then continue with the remediated operational Overview and two-column Tray flyout, eight-item navigation, TopBar actions, KPI/status/chart treatment, ultrawide/TV layouts, accessible `DB Notifier` name, visual `DBNotifier` wordmark, translation icon and Light/Dark-only control. The `2.6.13` database-and-bell geometry is humanly accepted in the bounded `REQ-065` scope and need not be repeated unless its contract changes. Obtain separate explicit consent before starting Narrator, High Contrast, scaling or the other remaining samples in [`STATE-05-Human-Gate-Validation.md`](STATE-05-Human-Gate-Validation.md).
