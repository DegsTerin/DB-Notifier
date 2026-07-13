# STATE-05 Design System Implementation Report

## Outcome

The first three DB-Notifier Design System increments, two `S05-HG-001` visual remediation iterations, the canonical product-mark refinement and the ultrawide shell correction are complete. They establish the canonical token/schema sources, deterministic React/WPF asset generation, platform-neutral Light/Dark/System preference contracts and a cohesive enterprise shell on both interfaces required by Design System `1.3.4`.

The Dashboard and WPF Desktop now apply generated semantic tokens, expose discreet language and theme buttons in the upper-right TopBar and preserve the validated preferences. Dedicated chrome semantics keep product identity cohesive across themes without the rejected Light-header/Dark-content inversion. System follows live platform colour preference; explicit Light and Dark remain stable. WPF gives Windows High Contrast precedence over the generated palette.

User-facing Web, WPF, Tray and installer surfaces use the display name `DB Notifier`; technical identifiers and compatibility paths retain `DBNotifier` or `DB-Notifier` as appropriate. The Dashboard main region now stretches across the available desktop and ultrawide shell width instead of stopping at a fixed `1640` CSS px maximum. Responsive desktop behaviour remains distinct from a dedicated TV/wallboard/kiosk mode, which is not implemented in this phase.

## International product-pattern review

The second visual iteration reviewed current official guidance rather than copying a product skin:

- [IBM Carbon's UI Shell](https://carbondesignsystem.com/components/UI-shell-header/usage/) treats the header as persistent orientation, keeps product identity to the left and global utilities to the right.
- [Grafana Saga's object-list guidance](https://grafana.com/developers/saga/templates/lists-of-objects/) distinguishes tables for open-ended exploration from lists for named objects with predictable structure, and its [table template](https://grafana.com/developers/saga/templates/table/) keeps filters adjacent to data.
- [Microsoft Fluent navigation](https://fluent2.microsoft.design/components/web/react/core/nav/usage) stays brief and scannable, while [Fluent cards](https://fluent2.microsoft.design/components/web/react/core/card/usage) organise related information through a predictable hierarchy.

DB-Notifier translates those principles into its own provider-neutral identity: code-native outlined icons replace text glyphs, selected navigation uses a restrained surface plus a narrow accent, five related fleet metrics share one divided band, and language/theme controls remain visible without dominating the shell.

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

`scripts/generate-brand-assets.mjs` similarly produces byte-stable SVG and nine-resolution ICO assets from one provider-neutral drawing algorithm. `brand:verify` compares every generated asset byte-for-byte and runs in Dashboard CI before the existing token/localisation gates.

## Preference contracts

React and .NET 10 share these exact concepts:

- Preference: `system | light | dark`.
- Effective theme: `light | dark`.
- Web key: `dbnotifier.theme.preference.v1`.
- UI preference schema: `dbnotifier.ui-preferences.v1`.
- WPF file name: `ui-preferences.v1.json`.
- Invalid data fails safely to System.
- Explicit Light/Dark overrides system state; System follows the current platform request.

The React adapter accesses only versioned local UI preference storage, the document root and the system colour-scheme media query. WPF stores the validated language and selected theme together in the versioned current-user file; locale-only documents from the preceding increment migrate safely to System without losing the language.

## React theme runtime

- A blocking local bootstrap runs in the document head before application styles can paint, validates the persisted preference and applies `data-theme` plus `data-theme-preference` without a wrong-theme flash.
- Storage access, invalid values and unavailable media-query APIs fail safely to System without interrupting rendering.
- `ThemeSelector` uses discreet pressed-state buttons with visible System, Light and Dark labels, immediate application, keyboard operation and a polite current-preference announcement.
- `matchMedia` changes update the effective theme live only through System resolution; explicit Light and Dark remain stable.
- Cross-tab storage changes accept only validated preference values.
- Route, filters, modal state and other React state remain owned by their existing components and are not reset by a theme change.
- Generated CSS loads before feature CSS. Hand-written feature styles contain no raw colour values or shadows and use canonical spacing/radius tokens for their corresponding declarations.
- At `1100` CSS px and below, the shell switches from the persistent side navigation to labelled horizontal navigation and replaces the seven-column inventory table with complete operational cards. The previous compressed `82` px rail and clipped `980` px table no longer exist.
- Summary cards use restrained surface elevation and compact status markers rather than heavy status-coloured top borders; preference groups use quiet chrome surfaces rather than outlined containers.
- The second refinement consolidates fleet summaries into one responsive metric band, replaces navigation/summary text glyphs with a coherent SVG icon set and reduces selected-preference intensity while retaining the tested contrast pairs.

## WPF theme runtime

- The application loads core and Light generated dictionaries before constructing the window, then atomically replaces only the semantic theme dictionary.
- System resolves the current-user Windows application colour preference and observes it at a bounded interval; explicit Light and Dark ignore that derived value.
- Windows High Contrast takes precedence and maps semantic resources to live system brushes without persisting an effective theme.
- One atomic, bounded `%LocalAppData%\DB-Notifier\ui-preferences.v1.json` document stores only schema, locale and selected theme; failures remain session-local.
- Main window chrome, surfaces, cards, inputs, buttons, tables, statuses and footer consume generated `DynamicResource` keys. Custom Button/ComboBox/DataGrid selection templates retain legibility in Dark instead of inheriting incompatible native Light colours.
- `pt-BR`, `en-GB`, System, Light and Dark are distinct accessible radio-button groups at the upper-right of the WPF TopBar.

## Verification

| Gate | Result |
|---|---|
| Token generation | Approved; four adapters generated |
| Deterministic drift verification | Approved |
| Schema/reference/type/theme parity | Approved |
| Light/Dark canonical contrast pairs | Approved; all tested pairs `>= 4.5:1` |
| Dashboard typecheck | Approved |
| Dashboard tests | Approved; 21/21 |
| Dashboard production build | Approved |
| .NET 10 Release build | Approved; 0 warnings/errors |
| .NET tests | Approved; 131 unit/model/provider/presentation + 5 architecture = 136/136 |
| .NET format verification | Approved |
| Legacy compatibility | Approved; 10/10 Pester and bundle validation |
| Dependency audit | Approved; no npm or NuGet vulnerabilities reported |
| Documentation gate | Approved; 161 comment-capable source files |
| React visual/responsive matrix | Approved; 66 locale/theme/viewport-route samples across 320, 390, 640, 768, 960, 1024, 1440 and 1920 CSS px |
| React overflow/accessibility | Approved; 0 global overflow and 0 unnamed interactive controls in all six locale/theme combinations |
| WPF UI Automation/visual matrix | Approved; six locale/theme combinations plus `820×620` minimum-window sample, with representative Light/Dark repetition after the second refinement |

## Security and phase boundaries

- Theme data is presentation-only and contains no user, provider, endpoint, credential or infrastructure data.
- Browser persistence contains only the validated semantic preference under `dbnotifier.theme.preference.v1`; the effective system-derived theme is not persisted.
- Storage and system-theme failures are contained locally and do not relax any product authorisation boundary.
- Generated adapters do not connect to a database, Agent, API, IdP, vault or administrative executor.
- No provider support or homologation state changed.
- `STATE-05` remains active; Human Gate, `STATE-06` and the multi-database laboratory remain blocked.

## Remaining increments

1. Confirm the outlined database mark, ultrawide layout and `DB Notifier` display name in the visible Dashboard preview.
2. Obtain explicit consent, then complete the keyboard/Narrator portion of `HG05-01`, native browser zoom, Windows scaling, High Contrast and remaining visual samples.
3. Present the explicit Human Gate decision; no lifecycle transition occurs automatically.

## Recommendation

Confirm the outlined canonical database mark, ultrawide layout and `DB Notifier` display name in the visible Dashboard preview, then obtain explicit consent before starting Narrator and continuing the remaining samples in [`STATE-05-Human-Gate-Validation.md`](STATE-05-Human-Gate-Validation.md).
