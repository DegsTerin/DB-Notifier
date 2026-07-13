# STATE-05 Design System Implementation Report

## Outcome

The first three DB-Notifier Design System increments are complete. They establish the canonical token schema/source, deterministic React/WPF generation, platform-neutral Light/Dark/System preference contracts and complete theme runtimes on both interfaces required by Design System `1.2.0`.

The Dashboard and WPF Desktop now apply generated semantic tokens, expose discreet language and theme buttons in the upper-right TopBar and preserve the validated preferences. System follows live platform colour preference; explicit Light and Dark remain stable. WPF gives Windows High Contrast precedence over the generated palette.

## Canonical token source

- Contract: `dbnotifier.design-tokens.v1`.
- JSON Schema restricts envelope, token-set names, key format, token types and values.
- `core.tokens.json` defines brand/neutral/status palettes, spacing, radius, control heights, typography, motion, easing and elevation primitives.
- `semantic.light.tokens.json` and `semantic.dark.tokens.json` expose identical semantic names/types for surfaces, text, borders, actions, focus, selection, status, overlay and elevation.
- `components.tokens.json` maps initial App, Card, Button, Input, Focus and Status component decisions to semantic aliases.
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

## WPF theme runtime

- The application loads core and Light generated dictionaries before constructing the window, then atomically replaces only the semantic theme dictionary.
- System resolves the current-user Windows application colour preference and observes it at a bounded interval; explicit Light and Dark ignore that derived value.
- Windows High Contrast takes precedence and maps semantic resources to live system brushes without persisting an effective theme.
- One atomic, bounded `%LocalAppData%\DB-Notifier\ui-preferences.v1.json` document stores only schema, locale and selected theme; failures remain session-local.
- Main window surfaces, cards, inputs, buttons, tables, statuses and footer consume generated `DynamicResource` keys. Custom Button/ComboBox templates retain legibility in Dark instead of inheriting incompatible native Light colours.
- `pt-BR`, `en-GB`, System, Light and Dark are distinct accessible radio-button groups at the upper-right of the WPF TopBar.

## Verification

| Gate | Result |
|---|---|
| Token generation | Approved; four adapters generated |
| Deterministic drift verification | Approved |
| Schema/reference/type/theme parity | Approved |
| Light/Dark canonical contrast pairs | Approved; all tested pairs `>= 4.5:1` |
| Dashboard typecheck | Approved |
| Dashboard tests | Approved; 19/19 |
| Dashboard production build | Approved |
| .NET 10 Release build | Approved; 0 warnings/errors |
| .NET tests | Approved; 131 unit/model/provider/presentation + 5 architecture = 136/136 |
| .NET format verification | Approved |
| Legacy compatibility | Approved; 10/10 Pester and bundle validation |
| Dependency audit | Approved; no npm or NuGet vulnerabilities reported |
| Documentation gate | Approved; 160 comment-capable source files |
| React visual/responsive matrix | Approved; 54 locale/theme/viewport-route samples across 320, 390, 640, 768, 1024 and 1440 CSS px |
| React overflow/accessibility | Approved; 0 global overflow and 0 unnamed interactive controls in all six locale/theme combinations |
| WPF UI Automation/visual matrix | Approved; six locale/theme combinations plus `820×620` minimum-window sample |

## Security and phase boundaries

- Theme data is presentation-only and contains no user, provider, endpoint, credential or infrastructure data.
- Browser persistence contains only the validated semantic preference under `dbnotifier.theme.preference.v1`; the effective system-derived theme is not persisted.
- Storage and system-theme failures are contained locally and do not relax any product authorisation boundary.
- Generated adapters do not connect to a database, Agent, API, IdP, vault or administrative executor.
- No provider support or homologation state changed.
- `STATE-05` remains active; Human Gate, `STATE-06` and the multi-database laboratory remain blocked.

## Remaining increments

1. Human screen-reader, native browser zoom, Windows scaling and High Contrast samples required by the Design System matrix.
2. Human visual review of hierarchy, focus, truth labels and selector behaviour in both languages and themes.
3. Explicit Human Gate presentation; no lifecycle transition occurs automatically.

## Recommendation

Execute the remaining human accessibility, native zoom/scaling, High Contrast and visual samples, then present the `STATE-05` Human Gate without changing the provider-neutral demonstration boundary.
