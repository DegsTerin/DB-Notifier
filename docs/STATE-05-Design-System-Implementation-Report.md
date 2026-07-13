# STATE-05 Design System Implementation Report

## Outcome

The first two DB-Notifier Design System increments are complete. They establish the canonical token schema/source, deterministic React/WPF generation, platform-neutral Light/Dark/System preference contracts and the complete React runtime required by Design System `1.0.0`.

The Dashboard now applies generated tokens throughout its hand-written feature styles and exposes the persisted System/Light/Dark preference. WPF still consumes its previous raw resources and remains the next separate increment.

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

The React adapter now accesses only versioned local UI preference storage, the document root and the system colour-scheme media query. WPF does not yet access Windows theme settings or a preference file; that platform adapter belongs to the next increment.

## React theme runtime

- A blocking local bootstrap runs in the document head before application styles can paint, validates the persisted preference and applies `data-theme` plus `data-theme-preference` without a wrong-theme flash.
- Storage access, invalid values and unavailable media-query APIs fail safely to System without interrupting rendering.
- `ThemeSelector` uses a native radio group with visible System, Light and Dark labels, immediate application, keyboard operation and a polite current-preference announcement.
- `matchMedia` changes update the effective theme live only through System resolution; explicit Light and Dark remain stable.
- Cross-tab storage changes accept only validated preference values.
- Route, filters, modal state and other React state remain owned by their existing components and are not reset by a theme change.
- Generated CSS loads before feature CSS. Hand-written feature styles contain no raw colour values or shadows and use canonical spacing/radius tokens for their corresponding declarations.

## Verification

| Gate | Result |
|---|---|
| Token generation | Approved; four adapters generated |
| Deterministic drift verification | Approved |
| Schema/reference/type/theme parity | Approved |
| Light/Dark canonical contrast pairs | Approved; all tested pairs `>= 4.5:1` |
| Dashboard typecheck | Approved |
| Dashboard tests | Approved; 14/14 |
| Dashboard production build | Approved |
| .NET 10 Release build | Approved; 0 warnings/errors |
| .NET tests | Approved; 128 unit/model/provider/presentation + 5 architecture = 133/133 |
| .NET format verification | Approved |
| Legacy compatibility | Approved; 10/10 Pester and bundle validation |
| Dependency audit | Approved; no npm or NuGet vulnerabilities reported |
| Documentation gate | Approved; 148 comment-capable source files |
| React visual/responsive sample | Approved; explicit Light at 1440 px and explicit Dark at exact 390 CSS px |
| Compact document overflow | Approved; `clientWidth = scrollWidth = 390` |

## Security and phase boundaries

- Theme data is presentation-only and contains no user, provider, endpoint, credential or infrastructure data.
- Browser persistence contains only the validated semantic preference under `dbnotifier.theme.preference.v1`; the effective system-derived theme is not persisted.
- Storage and system-theme failures are contained locally and do not relax any product authorisation boundary.
- Generated adapters do not connect to a database, Agent, API, IdP, vault or administrative executor.
- No provider support or homologation state changed.
- `STATE-05` remains active; Human Gate, `STATE-06` and the multi-database laboratory remain blocked.

## Remaining increments

1. WPF: theme service, atomic `ResourceDictionary` switching, System/High Contrast observer, safe local preference file, ThemeSelector and migration from raw resources.
2. Shared React/WPF component and operational-state parity, including removal of remaining unauthorised WPF raw visual values.
3. Full Light/Dark/System visual, contrast, persistence, accessibility and responsive re-audit.
4. Human screen-reader/native-zoom/theme samples and Human Gate.

## Recommendation

Execute the third Design System increment: integrate generated resources and the complete Light/Dark/System preference lifecycle into WPF, including High Contrast observation, without changing the provider-neutral demonstration boundary.
