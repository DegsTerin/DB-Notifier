# DB-Notifier Design System

## Document control

| Field | Value |
|---|---|
| Status | Official frontend specification |
| Design System version | `1.3.4` |
| Product phase | `STATE-05 FRONTEND_IMPLEMENTATION` |
| Platforms | React Web Dashboard and .NET 10 WPF Desktop/Tray |
| Themes | Light, Dark and System |
| Accessibility target | WCAG 2.2 AA |
| Interface languages | Brazilian Portuguese (`pt-BR`, default) and British English (`en-GB`) |

This document is normative for all new or modified DB-Notifier frontend work. “MUST”, “MUST NOT”, “SHOULD” and “MAY” express requirement strength. Product truth, security and accessibility requirements take precedence over visual preference.

## 1. Product character

DB-Notifier is a professional database operations product. Its visual identity MUST feel modern, clean, calm, precise and trustworthy, suitable for long-running enterprise use.

The intended character is:

- enterprise-grade rather than consumer-oriented;
- contemporary rather than futuristic;
- information-dense without appearing crowded;
- restrained, with clear hierarchy and purposeful colour;
- consistent across React and WPF while respecting native platform behaviour;
- provider-neutral: no database vendor owns the core visual identity.

The following are explicitly outside the visual direction:

- neon palettes, glow effects or cyberpunk styling;
- glassmorphism as a primary surface treatment;
- decorative gradients, excessive transparency or animated backgrounds;
- exaggerated shadows, oversized rounding or novelty controls;
- status communication based only on colour;
- provider logos used as substitutes for health or support state;
- motion without operational or orientation value.

## 2. Design principles

### 2.1 Truth before decoration

The interface MUST distinguish observed, stale, inferred, unavailable, unsupported, planned and unhomologated states. Visual polish MUST NOT imply connectivity, capability or support that has not been proved.

### 2.2 Safe operations are visibly different

Read-only navigation, configuration changes and administrative actions MUST have distinct visual treatment. Destructive or privileged actions require capability, permission, availability, confirmation and audit context before becoming enabled.

### 2.3 One semantic language across platforms

React and WPF MUST consume the same semantic token names and component state model. Platform adapters MAY render native details differently, but meaning, hierarchy, contrast and interaction outcomes MUST remain equivalent.

### 2.4 Accessibility is a component contract

Keyboard behaviour, focus, accessible name, state announcement, contrast, reduced motion and reflow are part of each component definition, not a later audit activity.

### 2.5 Density with progressive disclosure

Primary views show fleet state and urgent exceptions first. Technical evidence, timestamps and capability details remain available without overwhelming the initial scan. Compact presentation MUST NOT remove critical context.

## 3. Design System architecture

### 3.1 Canonical token source

Implementation MUST introduce the following platform-neutral source structure:

```text
design-system/
  tokens/
    core.tokens.json
    semantic.light.tokens.json
    semantic.dark.tokens.json
    components.tokens.json
  schema/
    design-tokens.schema.json
```

The token contract identifier is `dbnotifier.design-tokens.v1`. Strict JSON token files do not receive inline comments; this specification supplies their semantics.

Generated platform adapters MUST be produced from the canonical files:

```text
src/DBNotifier.Dashboard.Web/src/generated/design-tokens.css
src/DBNotifier.Desktop.Wpf/Generated/DesignTokens.Light.xaml
src/DBNotifier.Desktop.Wpf/Generated/DesignTokens.Dark.xaml
src/DBNotifier.Desktop.Wpf/Generated/DesignTokens.Core.xaml
```

Generated files MUST contain a source checksum and MUST NOT be edited manually. CI MUST regenerate them and fail on drift.

### 3.2 Token layers

1. **Core tokens** contain platform-independent primitives such as palette, size and duration.
2. **Semantic tokens** express purpose, such as `colour.text.primary` or `colour.status.critical.foreground`.
3. **Component tokens** express controlled component decisions, such as `button.primary.background.default`.
4. **Platform adapters** map semantic/component tokens to CSS custom properties or WPF `DynamicResource` keys.

Product components MUST consume semantic or component tokens. Direct consumption of palette primitives is limited to token generation, approved illustrations and test fixtures.

### 3.3 Versioning

- MAJOR: removed/renamed token or incompatible semantic change.
- MINOR: additive token, component or theme capability.
- PATCH: value correction that preserves semantic purpose.

Every token change MUST update the Design System version, generated artefacts, contrast evidence and affected visual baselines.

## 4. Theme architecture

### 4.1 Preference model

The public preference is exactly one of:

```text
system | light | dark
```

`system` is the default for a new user. Effective theme resolution is:

```text
light preference -> Light
dark preference  -> Dark
system preference + operating system dark -> Dark
system preference + operating system light/unknown -> Light
```

Changing the operating-system theme MUST update an active `system` preference without restarting or recreating the application. An explicit Light or Dark preference MUST ignore later system changes.

### 4.2 React implementation contract

- Apply `data-theme="light|dark"` to `<html>`.
- Persist the preference, not the resolved theme, under `dbnotifier.theme.preference.v1` in `localStorage`.
- Validate stored values; invalid or inaccessible storage falls back to `system` without failing page startup.
- Resolve System using `matchMedia("(prefers-color-scheme: dark)")` and subscribe only while preference is `system`.
- Run a minimal bootstrap before the application stylesheet/React render to prevent a flash of the wrong theme.
- Set `color-scheme: light dark` consistently with the resolved theme so native controls and scrollbars match.
- Theme changes MUST preserve route, filters, modal state where safe, keyboard focus and unsaved non-secret input.

Theme preference is presentation-only data. It MUST NOT contain identity, provider, credential or infrastructure information.

### 4.3 WPF implementation contract

- Expose `ThemePreference.System`, `ThemePreference.Light` and `ThemePreference.Dark` through an application service independent of views.
- Load theme-specific `ResourceDictionary` instances atomically and reference theme resources using `DynamicResource`.
- Observe Windows colour-mode changes while the preference is System.
- Persist only versioned UI preferences in `%LocalAppData%\DB-Notifier\ui-preferences.v1.json` using atomic replacement and current-user access.
- Invalid/unreadable preference data MUST fail safely to System; it MUST NOT prevent application startup.
- Theme switching MUST preserve window, selected view, scenario, focus and Tray ownership.
- Windows High Contrast MUST take precedence over DB-Notifier theme colours and preserve native system resources where required.

Future authenticated preference synchronisation MAY be added in `STATE-06` or later, but local selection remains the offline fallback and never carries secrets.

### 4.4 Interface-language contract

The supported interface locales are exactly `pt-BR` and `en-GB`; unsupported or unreadable preferences fail safely to `pt-BR`. React and WPF MUST consume adapters generated from `localisation/messages.pt-BR.xml` and `localisation/messages.en-GB.xml`. Generated TypeScript/XAML resources MUST have identical keys and positional placeholders, and CI MUST reject drift.

React persists only the validated locale under `dbnotifier.language.preference.v1`, applies `lang` before application render and synchronises changes between tabs. WPF persists only the validated locale in the versioned local UI-preference file, replaces its generated `ResourceDictionary` without recreating the window and keeps Tray text aligned. Provider identifiers, contract names and reason codes remain stable machine values rather than translated data.

Language switching MUST preserve route/view, scenario, filters, focus where safe and all factual capability distinctions. Every responsive and accessibility sample MUST exercise both locales because British English copy may expand differently from Brazilian Portuguese.

## 5. Core tokens

### 5.1 Brand palette

| Token | Value | Intended use |
|---|---:|---|
| `palette.brand.50` | `#EFF7FF` | Subtle selected surface |
| `palette.brand.100` | `#DCEEFF` | Light information surface |
| `palette.brand.200` | `#B9DDFF` | Decorative border |
| `palette.brand.300` | `#86C4F4` | Dark-theme secondary accent |
| `palette.brand.400` | `#63B3ED` | Dark-theme action/accent |
| `palette.brand.500` | `#2D8BD1` | Interactive accent |
| `palette.brand.600` | `#0B5CAD` | Canonical DB-Notifier brand |
| `palette.brand.700` | `#084B8A` | Hover |
| `palette.brand.800` | `#063A6C` | Active |
| `palette.brand.900` | `#052C50` | Dark structural surface |
| `palette.brand.950` | `#031B31` | Deep structural surface |

Blue is the brand colour, not a health colour. Provider-specific colours MUST NOT replace it in the application shell.

### 5.2 Neutral palette

| Token | Value |
|---|---:|
| `palette.neutral.0` | `#FFFFFF` |
| `palette.neutral.25` | `#F8FAFC` |
| `palette.neutral.50` | `#F4F7FA` |
| `palette.neutral.100` | `#E8EDF2` |
| `palette.neutral.200` | `#D9E0E7` |
| `palette.neutral.300` | `#B8C5CF` |
| `palette.neutral.400` | `#94A6B2` |
| `palette.neutral.500` | `#6F818E` |
| `palette.neutral.600` | `#526273` |
| `palette.neutral.700` | `#334650` |
| `palette.neutral.800` | `#1C2B36` |
| `palette.neutral.850` | `#17242E` |
| `palette.neutral.900` | `#111C24` |
| `palette.neutral.950` | `#0B1218` |
| `palette.neutral.1000` | `#05090D` |

Dark surfaces use layered dark blue-neutrals rather than pure black. Light surfaces use restrained cool neutrals rather than pure grey.

### 5.3 Sizing and spacing

The base unit is 4 px. Layout MUST use tokens rather than arbitrary values.

| Token | Value |
|---|---:|
| `space.0` | `0` |
| `space.1` | `4px` |
| `space.2` | `8px` |
| `space.3` | `12px` |
| `space.4` | `16px` |
| `space.5` | `20px` |
| `space.6` | `24px` |
| `space.8` | `32px` |
| `space.10` | `40px` |
| `space.12` | `48px` |
| `space.16` | `64px` |

Standard control heights are `40px` compact and `44px` comfortable. Icon-only interactive targets MUST be at least `44×44px` on Web and `40×40` device-independent pixels on WPF, with additional spacing where needed to meet WCAG target requirements.

### 5.4 Radius

| Token | Value | Use |
|---|---:|---|
| `radius.none` | `0` | Tables and joined edges |
| `radius.small` | `4px` | Code/status details |
| `radius.control` | `6px` | Inputs and buttons |
| `radius.medium` | `8px` | Popovers and compact cards |
| `radius.large` | `10px` | Main cards and dialogues |
| `radius.xlarge` | `12px` | Reserved feature surfaces |
| `radius.pill` | `999px` | Badges only |

Rounded containers MUST remain restrained. Large pill-shaped controls are not the default enterprise control shape.

### 5.5 Elevation

| Token | Light | Dark |
|---|---|---|
| `elevation.0` | none | none |
| `elevation.1` | `0 2px 8px rgba(22,37,50,.06)` | `0 2px 8px rgba(0,0,0,.24)` |
| `elevation.2` | `0 8px 24px rgba(22,37,50,.12)` | `0 8px 24px rgba(0,0,0,.36)` |
| `elevation.3` | `0 20px 60px rgba(8,25,38,.24)` | `0 20px 60px rgba(0,0,0,.52)` |

Borders establish most hierarchy. Shadows are reserved for raised menus, popovers, dialogues and drag/overlay states.

### 5.6 Motion

| Token | Value | Use |
|---|---:|---|
| `motion.instant` | `0ms` | Reduced motion and immediate state |
| `motion.fast` | `100ms` | Hover/press feedback |
| `motion.standard` | `160ms` | Focus, selection and small reveal |
| `motion.emphasised` | `240ms` | Dialogue/popover entry |
| `motion.slow` | `320ms` | Rare layout transition |
| `easing.standard` | `cubic-bezier(.2,0,0,1)` | General transition |
| `easing.exit` | `cubic-bezier(.4,0,1,1)` | Exit transition |

Animations MUST NOT delay an operation, loop decoratively or imply live activity without live data. Under reduced motion, translation, scaling and indefinite animation MUST be removed; essential progress uses a static indicator and text.

## 6. Semantic colour tokens

### 6.1 Theme surfaces and text

| Semantic token | Light | Dark |
|---|---:|---:|
| `colour.canvas` | `#F4F7FA` | `#0B1218` |
| `colour.surface.default` | `#FFFFFF` | `#111C24` |
| `colour.surface.subtle` | `#F8FAFC` | `#17242E` |
| `colour.surface.raised` | `#FFFFFF` | `#1C2B36` |
| `colour.surface.sunken` | `#E8EDF2` | `#05090D` |
| `colour.surface.inverse` | `#0D2538` | `#F4F7FA` |
| `colour.text.primary` | `#17202A` | `#F2F6F8` |
| `colour.text.secondary` | `#526273` | `#B8C5CF` |
| `colour.text.muted` | `#6F818E` | `#94A6B2` |
| `colour.text.inverse` | `#FFFFFF` | `#17202A` |
| `colour.border.default` | `#D9E0E7` | `#334650` |
| `colour.border.strong` | `#9AACBC` | `#58707D` |
| `colour.action.primary.background` | `#0B5CAD` | `#63B3ED` |
| `colour.action.primary.foreground` | `#FFFFFF` | `#07131C` |
| `colour.action.primary.hover` | `#084B8A` | `#86C4F4` |
| `colour.action.primary.active` | `#063A6C` | `#2D8BD1` |
| `colour.focus.ring` | `#9A4A00` | `#FFC857` |
| `colour.selection.background` | `#DCEEFF` | `#063A6C` |
| `colour.selection.foreground` | `#052C50` | `#F2F6F8` |
| `colour.overlay.scrim` | `rgba(8,25,38,.68)` | `rgba(0,0,0,.74)` |

### 6.2 Cohesive application chrome

The TopBar and its preference region use dedicated chrome semantics instead of `surface.inverse`. Chrome remains a restrained deep navy in Light and Dark so switching themes never creates a visually inverted header/content split. Theme differences remain visible in the canvas, surfaces, controls and data regions.

| Semantic token | Light | Dark |
|---|---:|---:|
| `colour.chrome.background` | `#10283B` | `#0E1B25` |
| `colour.chrome.surface` | `#18384F` | `#172C3A` |
| `colour.chrome.foreground` | `#F4F7FA` | `#F4F7FA` |
| `colour.chrome.muted` | `#C7D3DC` | `#C7D3DC` |
| `colour.chrome.border` | `#3B586C` | `#3B586C` |
| `colour.chrome.selected.background` | `#DCEEFF` | `#24465D` |
| `colour.chrome.selected.foreground` | `#052C50` | `#F4F7FA` |

Chrome text/background ratios range from `9.92:1` to `16.25:1`; selected preference pairs are `11.95:1` in Light and `9.26:1` in Dark. Components consume the corresponding `component.shell.chrome.*` aliases. Chrome is reserved for shared application identity and preferences; feature panels MUST NOT recreate it as a decorative dark band.

### 6.3 Operational status

Every status component MUST combine icon, text and colour. These colours represent operational meaning and MUST NOT be assigned to provider identity.

| Status | Light foreground/background | Dark foreground/background | Canonical symbol |
|---|---|---|---|
| Healthy/success | `#17633A` / `#EDF8F1` | `#75D69C` / `#0F2A1E` | `●` or approved check-circle icon |
| Degraded/warning | `#805300` / `#FFF7E4` | `#F3C66D` / `#2B2412` | `▲` or warning-triangle icon |
| Critical/error | `#982B2B` / `#FFF0F0` | `#FF9C9C` / `#32191B` | `■` or error-circle icon |
| Information | `#155B78` / `#EDF8FC` | `#8BD3F7` / `#102632` | `●` or information icon |
| Maintenance | `#5A4192` / `#F4F0FF` | `#C9B3FF` / `#271E3A` | `◆` or tools icon |
| Stale/unknown/neutral | `#4E5C69` / `#F0F3F5` | `#C2CDD4` / `#202B32` | `◷` or clock/help icon |

Provider support labels use neutral/information semantics:

- `Implemented · not homologated`: information.
- `Planned · not implemented`: neutral.
- `Unsupported`: neutral with explicit text.
- `Unavailable`: warning only when a normally supported capability is temporarily unavailable.
- `Denied`: critical access state without implying system failure.

### 6.4 Required contrast evidence

The initial palette has the following calculated text contrast ratios:

| Pair | Ratio |
|---|---:|
| Light primary text / surface | `16.45:1` |
| Light secondary text / surface | `6.26:1` |
| Light primary action foreground/background | `6.67:1` |
| Light focus ring / surface | `6.26:1` |
| Light status pairs | `6.24:1` to `6.99:1` |
| Dark primary text / surface | `15.89:1` |
| Dark secondary text / surface | `9.81:1` |
| Dark primary action foreground/background | `8.22:1` |
| Dark focus ring / surface | `11.23:1` |
| Dark status pairs | `8.13:1` to `9.61:1` |

Generated theme tests MUST recompute ratios. The table is evidence for the specification, not a substitute for automated validation.

## 7. Typography

The shared font stack is:

```text
"Segoe UI Variable", "Segoe UI", system-ui, -apple-system, sans-serif
```

No remote font request is permitted for core UI. WPF uses Segoe UI/Segoe UI Variable available on Windows; Web falls back to the platform system stack. Monospace content uses `"Cascadia Mono", "Cascadia Code", Consolas, monospace`.

| Token | Size / line height | Weight | Use |
|---|---|---:|---|
| `type.caption` | `12/16` | 400/600 | Metadata, timestamps |
| `type.body.small` | `13/18` | 400 | Dense table/support text |
| `type.body` | `14/20` | 400 | Default application text |
| `type.body.strong` | `14/20` | 600 | Emphasis and controls |
| `type.title.small` | `16/22` | 600 | Card/section title |
| `type.title.medium` | `20/28` | 600 | View subsection |
| `type.title.large` | `28/36` | 650 | Page title |
| `type.display` | `32/40` | 650 | Reserved overview display |
| `type.metric` | `28/34` | 650 | KPI value |

Body copy MUST NOT be smaller than 14 px by default. Uppercase is limited to short labels with tracking; paragraphs and actions use sentence case. Numeric tables SHOULD use tabular figures when available.

## 8. Iconography and brand assets

- Use a single Fluent-compatible outlined icon family at 16, 20 and 24 px/DIP.
- Core navigation uses 20 px icons; primary commands use 16 or 20 px; empty states MAY use 32 px.
- Icons MUST have consistent stroke weight and optical alignment.
- Emoji and text glyphs are not production icons.
- Shell and summary icons SHOULD use the shared code-native outlined SVG set when a licensed external family is unnecessary; stroke, view box and optical size remain consistent across the set.
- Icon-only buttons require an accessible name and tooltip.
- Decorative icons are hidden from assistive technology.
- Status icons always have adjacent visible text.
- Provider logos MAY appear in provider detail/catalogue contexts, never as the only provider name or status indicator.
- Third-party icons MUST be vendored with recorded licence/provenance; runtime downloads are prohibited.
- The DB-Notifier product mark is a simple outlined white database cylinder on a `palette.brand.600` rounded square. Rounded three-pixel strokes preserve a light enterprise appearance and legibility at 16 px. It is provider-neutral and MUST NOT be replaced by a PostgreSQL, MySQL or other vendor logo.
- `scripts/generate-brand-assets.mjs` is the canonical cross-platform generator. It emits the Design System/Web SVG and a multi-resolution Windows ICO at 16/20/24/32/40/48/64/128/256 px; CI MUST fail on generated drift.
- Dashboard favicon/header, WPF header/window, Windows executable/shortcuts, Tray and installer MUST consume this same mark. Release signing remains a `STATE-08` concern and MUST NOT be claimed during frontend implementation.
- When adjacent text already exposes the product name, Web treats the mark as decorative. Native surfaces provide the stable accessible name `DB Notifier` where the platform exposes the image independently.

The user-facing display name and wordmark are written as `DB Notifier`. Architecture, governance and technical prose retain the canonical name `DB-Notifier`; identifiers use `DBNotifier` only where spaces or punctuation are not valid. The mark MUST retain clear space equal to at least half its icon height.

## 9. Layout and responsiveness

### 9.1 Breakpoint roles

| Range | Role | Expected behaviour |
|---|---|---|
| `320–479` CSS px | Compact | Single-column cards; internal horizontal navigation; no page overflow |
| `480–767` | Wide compact | Single-column content with more generous metadata layout |
| `768–1100` | Tablet/narrow desktop | Horizontal navigation, compact data cards and 3–5 column summaries according to content |
| `1101–1439` | Standard desktop | Persistent navigation and data tables |
| `1440+` | Wide desktop | Shell and operational regions use the available width; prose retains a controlled reading measure |

Breakpoints respond to content, not device names. Every component MUST tolerate text expansion of at least 30%. Global horizontal scrolling is forbidden at 320 CSS px. Deliberate local scrolling is permitted for navigation and data regions when labelled and keyboard-operable.

### 9.2 Shell

- Header: product identity, environment/demonstration context and user/theme controls.
- Navigation: stable view order, clear selected state and accessible current item.
- Main: one page title, optional description/actions, then operational content.
- Footer/status area: connectivity/data-source truth without competing with primary tasks.
- Maximum readable prose line length: approximately 75 characters.
- The main application region MUST stretch across the available desktop and ultrawide shell width. A fixed page-level maximum width MUST NOT create an inactive strip at the right edge; content-specific reading measures remain permitted.
- Dense tables may use available width; forms and dialogues use controlled widths.
- A navigation rail MUST NOT retain visible labels once its width forces wrapping or compression; switch to a labelled horizontal navigation region first.
- Seven-column operational tables switch to the complete compact-card alternative at `1100` CSS px or below. The alternative preserves all fields and does not rely on horizontal page scrolling.

### 9.3 WPF adaptation

The WPF minimum window remains usable at `820×620` DIP. Below the comfortable table width, content SHOULD use controlled scrolling or a compact item template; columns MUST NOT silently truncate critical support/status text. Windows scaling at 100%, 125%, 150% and 200% MUST be sampled before release.

### 9.4 TV and wallboard scope

Design System `1.3.4` does not define a dedicated TV, wallboard or kiosk mode. Responsive Web behaviour at desktop or ultrawide dimensions MUST NOT be presented as TV-mode support. Any future TV mode requires a separately approved interaction, density, viewing-distance, focus/navigation, refresh and long-running-display contract.

## 10. Reusable component catalogue

All components define default, hover, pressed, focus-visible, selected, disabled, busy, invalid and read-only states where applicable.

### 10.1 Application shell

`AppShell`, `TopBar`, `SideNavigation`, `PageHeader`, `ContentRegion` and `FooterStatus` establish the common hierarchy. The shell owns theme application and responsive navigation; feature views MUST NOT recreate it. TopBar chrome remains cohesive across Light and Dark, while surface hierarchy and content colours communicate the effective theme.

### 10.2 Theme selector

`ThemeSelector` exposes System, Light and Dark as discreet pressed-state buttons with visible localised text. It MUST announce the current preference, apply immediately, persist safely and remain keyboard-operable. An icon alone is insufficient.

On desktop and tablet-width shells, the selector belongs in the upper-right TopBar preference region. At compact mobile widths it MAY wrap beneath the brand, but MUST remain right-aligned, fully visible and ahead of feature content.

### 10.2.1 Language selector

`LanguageSelector` exposes `pt-BR` and `en-GB` as discreet pressed-state buttons in the same upper-right TopBar preference region. Each compact code MUST expose the full native language name to assistive technology and as supplementary pointer text. It applies immediately, persists only the validated BCP 47 value, updates document/window and assistive-technology labels, and remains usable at compact widths. Flags MUST NOT replace the locale controls.

Language and theme groups MUST retain separate accessible names, a programmatically determinable selected state and an obvious focus indicator. Their compact presentation MUST NOT obscure the product brand or cause document-level horizontal overflow.

### 10.3 Buttons

Variants:

- Primary: one dominant safe action per region.
- Secondary: standard action.
- Quiet: low-emphasis/navigation action.
- Destructive: dangerous confirmed action only.
- Icon: compact action with accessible name.

Disabled controls MUST remain legible and MUST provide adjacent explanation when their unavailability is operationally important. Loading buttons retain width, expose busy state and prevent duplicate activation.

### 10.4 Inputs and selection

`TextField`, `SearchField`, `Select`, `Checkbox`, `RadioGroup`, `Switch` and future secret-reference picker require persistent labels, optional help, error association and keyboard access. Placeholder text is not a label. Secret values are never displayed; protected references use safe labels.

### 10.5 Cards and metrics

`SurfaceCard`, `SummaryCard`, `MetricCard` and `AlertCard` use consistent padding, border, radius and heading order. A metric includes label, value, freshness/context and optional trend; it MUST NOT use colour alone.

Related fleet metrics SHOULD form one bordered metric band with internal dividers at comfortable widths. This reduces competing card chrome and preserves scan order. The band reflows to two columns and then one column without removing labels, icons or values.

### 10.6 Status and support

`StatusBadge`, `SeverityBadge`, `SupportBadge`, `FreshnessIndicator` and `CapabilityState` map only canonical domain/presentation states. Product code MUST NOT assemble arbitrary status colours.

### 10.7 Data presentation

`DataTable`/WPF `DataGrid`, `DefinitionList`, `Timeline`, `KeyValueList` and responsive item cards share field labels and ordering. Tables require caption/accessible name, column headers, keyboard navigation, loading/empty/error treatment and a compact alternative where global reflow would otherwise fail.

### 10.8 Feedback and overlays

`InlineMessage`, `Banner`, `Toast`, `Tooltip`, `Popover` and `ModalDialog` have distinct roles:

- Inline message: field/section context.
- Banner: page-level persistent condition.
- Toast: brief confirmation, never the sole error record.
- Tooltip: supplementary explanation, available by keyboard and pointer.
- Popover: non-modal contextual controls.
- Modal dialogue: decision requiring contained focus and background inertness.

Modal dialogue requirements include initial focus, forward/reverse containment, Escape unless unsafe, focus restoration, accessible name/description, scroll containment and a visible primary/secondary action order. Destructive confirmation names the target, impact and audit reason.

### 10.9 Operational states

`LoadingState`, `EmptyState`, `OfflineState`, `ErrorState`, `DeniedState`, `MaintenanceState`, `StaleState` and `FilteredEmptyState` require:

- unique title and safe explanatory text;
- correct live-region/alert semantics;
- truthful retry or recovery action;
- no fabricated current health;
- stable layout where practical;
- theme-independent icon/text treatment.

### 10.10 Tray

The Tray surface is an extension of the Desktop client, not a separate design language. It uses the signed brand icon and native menu conventions. Open, factual status and Exit remain clearly separated. Database/service actions are absent unless capability, privilege, confirmation and audit contracts are later approved.

## 11. Interaction states

| State | Requirement |
|---|---|
| Default | Clear affordance and readable label |
| Hover | Subtle background/border change; never required to discover function |
| Pressed | Immediate visual feedback without displacement |
| Focus-visible | 2–3 px high-contrast ring with offset; never removed |
| Selected/current | Persistent shape/text/icon treatment plus accessible state |
| Disabled | Reduced emphasis but readable; cannot receive action; reason available when material |
| Busy | `aria-busy`/automation equivalent, stable label/size, duplicate action blocked |
| Invalid | Error text linked to control; icon/text, not red alone |
| Read-only | Visually distinct from editable and still selectable/copyable where safe |
| Stale | Timestamp and stale label remain visible; stale health is not current health |

Pointer, keyboard and assistive-technology activation MUST produce equivalent outcomes. Hover-only content and drag-only operations are prohibited.

## 12. Accessibility requirements

### 12.1 WCAG 2.2 AA baseline

- Normal text contrast: at least `4.5:1`.
- Large text contrast: at least `3:1`.
- UI components, meaningful graphics and focus indicators: at least `3:1` against adjacent colours.
- Text and essential controls reflow at 320 CSS px and 200% zoom without unintended two-dimensional page scrolling.
- Text remains usable at 200% browser zoom and OS scaling.
- Target size meets WCAG 2.2 AA minimum, with project targets of 44 px Web and 40 DIP WPF where practical.
- Colour is never the only means of communicating state.
- Focus order follows visual/task order and remains visible.
- Focus is never trapped except within an active modal, where Escape/restoration rules apply.
- Status changes are announced without stealing focus.
- Reduced-motion and Windows High Contrast preferences are respected.

### 12.2 Web semantics

Use native HTML before ARIA. Each view has one `main` and one `h1`; navigation exposes current state; tables use captions/headers; forms use labels; dialogues use native `<dialog>` where supported by the project baseline. Custom controls require documented keyboard patterns and automated accessibility-tree coverage.

### 12.3 WPF automation

Interactive and meaningful data elements require appropriate `AutomationProperties.Name`, help text, control type and live setting. Resource swaps MUST NOT destroy UI Automation identity. DataGrid headers/cells, selectors, dialogues and Tray menus require Narrator/NVDA sampling.

## 13. Content and data presentation

- Interface copy uses concise `pt-BR` or `en-GB` in sentence case according to the active supported preference.
- Code comments remain British English under the project documentation standard.
- Provider identifiers remain stable machine values; visible provider names may be localised separately.
- UTC timestamps include `UTC`; local time requires an explicit timezone label.
- Relative time supplements, never replaces, an exact timestamp for operational evidence.
- Error messages state what failed, safe impact and recovery without exposing secrets or internals.
- Buttons use explicit verbs. Avoid generic “OK” where a meaningful action exists.
- Terms `implemented`, `homologated`, `supported`, `available` and `authorised` MUST retain their distinct product meanings.
- Empty states explain whether data is absent, filtered, inaccessible or unavailable.

## 14. React and WPF parity

| Contract | React | WPF |
|---|---|---|
| Semantic colour | CSS custom property | `DynamicResource` brush |
| Typography | CSS type token/class | Dynamic style resource |
| Spacing/radius | CSS token | Thickness/CornerRadius resource |
| Theme selection | `data-theme` + service/hook | Theme service + ResourceDictionary |
| Language selection | `lang` + generated catalogue/context | Generated `ResourceDictionary` + localisation service |
| System observation | `matchMedia` | Windows theme observer |
| Persistence | versioned `localStorage` value | versioned local UI preferences file |
| Focus | native DOM/focus-visible | native WPF focus adorner |
| Modal | native `<dialog>` contract | owned WPF Window/dialog service |
| Status | shared presentation state mapping | shared presentation state mapping |
| Reduced motion | media query | system animation preference/adaptor |
| High contrast | forced-colours/system colours | Windows High Contrast resources |

Parity means semantic equivalence, not identical pixels. Native platform behaviour wins when it improves accessibility or expected interaction without changing meaning.

## 15. Validation and re-audit criteria

### 15.1 Token and architecture gates

- Token JSON validates against the schema.
- Generated CSS/XAML is deterministic and has no drift.
- Hand-written feature styles contain no raw colour values, shadows, radii or spacing outside approved exceptions.
- Every consumed semantic token exists in Light and Dark.
- No component imports provider-specific presentation into the core design layer.

### 15.2 Theme behaviour gates

- Light, Dark and System resolve correctly on Web and WPF.
- Preference survives application restart/reload.
- Invalid/unavailable persistence fails to System without crash.
- System changes update only System preference.
- No wrong-theme flash is visible on Dashboard startup.
- Switching theme preserves route, view, filters, focus and safe unsaved state.
- Native controls, scrollbars, overlays and Tray-related windows match the effective theme where platform-supported.

### 15.3 Contrast and accessibility automation

- Compute every declared foreground/background pair in both themes.
- Fail below WCAG thresholds; no manual exception for normal product text.
- Exercise keyboard order, focus visibility, dialogue entry/containment/Escape/restoration and live states.
- Verify accessibility trees contain expected landmarks/roles/names and no unnamed interactive controls.
- Verify reduced motion and forced-colours/high-contrast behaviour.

### 15.4 Visual and responsive matrix

Dashboard visual regression samples MUST cover:

```text
Themes: Light, Dark, System resolving to each
Locales: pt-BR and en-GB
Viewports: 320×568, 390×844, 768×1024, 1024×768, 1440×1000, 1920×1080
Views: Inventory, History, Alerts, Configuration
States: ready, loading, empty, offline, error, denied, maintenance, stale, filtered-empty
Overlays: confirmation-required, denied, unsupported, unavailable, unknown
Preferences: reduced motion and 200% native browser zoom
```

WPF samples MUST cover `pt-BR` and `en-GB` with Light, Dark, System Light, System Dark and Windows High Contrast at the default and minimum window sizes, plus 100%, 125%, 150% and 200% Windows scaling where the environment permits.

Acceptance requires no unintended page overflow, clipping, illegible truncation, overlapping focus ring, theme mismatch or state communicated only by colour.

### 15.5 Human Gate samples

- Chrome/Edge with NVDA or Narrator in Light and Dark.
- Dashboard theme selector, main navigation, operational states, tables/cards and modal dialogue.
- Native 200% zoom and keyboard-only task completion.
- WPF with Narrator/NVDA across selectors, summaries, grids, dialogue and Tray.
- Visual review for hierarchy, density, consistency and absence of neon/glow/exaggerated effects.
- Explicit review that planned/unhomologated providers and unsupported actions remain truthful in both themes.

## 16. Implementation sequence within STATE-05

1. Add token schema/source, generator and deterministic drift test.
2. Implement theme preference resolver and persistence contracts with unit tests.
3. Refactor React raw values into generated semantic tokens and reusable components.
4. Refactor WPF raw values into generated `DynamicResource` dictionaries and shared styles.
5. Implement ThemeSelector on both platforms and System observation.
6. Add Light/Dark/System contrast, behaviour, visual and persistence tests.
7. Repeat the full `STATE-05` automatic re-audit.
8. Perform the human screen-reader/zoom/theme samples.
9. Present the Human Gate; no transition occurs automatically.

The implementation MUST remain deterministic and disconnected from real databases, Agents, credentials, IdP and administrative execution during `STATE-05`.

## 17. Governance and exceptions

- The Design System is owned by the frontend architecture boundary and reviewed with accessibility and security concerns.
- New frontend components MUST use the catalogue or propose an additive component contract here.
- Raw visual values require a documented, narrow exception and a follow-up token decision.
- Feature teams MUST NOT fork token names or create provider-specific themes.
- Screenshots are evidence, not token sources.
- Legacy prototypes under `desktop-wpf/`, `pixel-ui/` and `tray-app/` are references only and do not override this specification.
- A change that reduces contrast, hides support truth, bypasses focus behaviour or introduces an unsafe action is rejected regardless of visual appeal.

## 18. Definition of Done for Design System implementation

- Canonical token source and generated React/WPF adapters exist.
- Light, Dark and System work and persist on both platforms.
- System theme changes are observed without restart.
- Core reusable components consume semantic tokens.
- Existing STATE-05 views contain no unauthorised raw visual values.
- Both themes pass automated contrast and accessibility gates.
- Responsive/visual matrices pass without unintended overflow or clipping.
- Human screen-reader, zoom, scaling and theme samples are recorded.
- Documentation, token version and evidence are synchronised.
- Automatic re-audit is approved.
- Human Gate is explicitly approved before `STATE-06`.
