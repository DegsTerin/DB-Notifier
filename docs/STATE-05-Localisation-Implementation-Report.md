# STATE-05 Localisation Implementation Report

## Outcome

DB-Notifier now provides Brazilian Portuguese (`pt-BR`) and British English (`en-GB`) interface choices in the React Dashboard and .NET 10 WPF Desktop/Tray shell. Brazilian Portuguese remains the safe default. This increment changes presentation only: it does not integrate an API, Agent, database, identity provider, credential store or administrative executor.

## Shared contract

- `localisation/messages.pt-BR.xml` and `localisation/messages.en-GB.xml` are the canonical catalogues under schema `dbnotifier.localisation.v1`.
- `scripts/generate-localisation.mjs` validates exact key parity, duplicate keys and positional placeholders before deterministically generating the typed React catalogue and WPF dictionaries.
- CI runs `localisation:verify` and fails when a generated adapter drifts from the canonical source.
- Provider identifiers, capability identifiers, reason codes and versioned contract names are deliberately not translated.
- The Application layer accepts only `pt-BR` and `en-GB`; invalid values fail safely to `pt-BR`.

## Platform behaviour

### React Dashboard

- A native language selector applies copy immediately, updates the document `lang`, title and description, and announces the selected language.
- The bootstrap reads `dbnotifier.language.preference.v1` before React render, avoiding an initial language mismatch where valid storage is available.
- Storage denial or malformed values do not block rendering; cross-tab changes accept only supported locales.
- Demonstration fixtures, navigation, states, filters, tables/cards and confirmation copy use typed catalogue keys.

### WPF Desktop and Tray

- The language selector replaces a generated localisation `ResourceDictionary` while preserving the window, selected view and safe demonstration state.
- Only schema and locale are stored in `%LocalAppData%\DB-Notifier\ui-preferences.v1.json`; writes use temporary-file replacement and failure remains session-local.
- Dynamic XAML resources update structural copy, accessible names and headers; code-generated fixture/status/dialog values are rebuilt through the same catalogue.
- Tray menu, tooltip and first-hide notification update with the active locale.

## Responsive scope

The React Dashboard is the adaptive interface for compact mobile, tablet, laptop and desktop viewports. Its existing content-driven breakpoints cover 320, 390, 768, 1024 and 1440 CSS-pixel samples, with wrapped/hideable selector labels and no intentional global horizontal scrolling. The native WPF application is a Windows desktop surface with a supported minimum of `820×620` DIP; it is not presented as an Android/iOS tablet or mobile application.

## Verification

| Gate | Observed result |
|---|---|
| Canonical catalogue | Approved; 196 keys per locale with exact key/placeholder parity |
| Generated adapter drift | Approved for TypeScript and both WPF dictionaries |
| Dashboard | Approved; typecheck, 18/18 tests and Vite production build |
| .NET 10 solution | Approved; Release build with 0 warnings/errors and 136/136 tests |
| Source documentation/format | Approved; 158 comment-capable files and `dotnet format` clean |
| Dependencies | Approved; npm reported 0 vulnerabilities; NuGet remained covered by the solution gate |
| Browser responsive/accessibility | Approved; 18 locale/viewport-route samples, 0 global overflow, 0 unnamed interactive controls and modal entry/Escape/restoration passing |
| WPF UI Automation | Approved with existing framework reservation; both locales at 1180×760 and 820×620, deterministic visible Tab sample and one unnamed framework Pane |
| Legacy compatibility | Approved; canonical and deprecated entry-point samples passed |

Sanitised runtime screenshots and JSON remain in `%TEMP%\DBNotifier-State05-Audit\pt-BR` and `%TEMP%\DBNotifier-State05-Audit\en-GB`; they are deliberately not committed.

## Boundaries and remaining evidence

- This increment proves implemented interface localisation, not provider homologation or production readiness.
- Human screen-reader review and native 200% zoom/scaling samples remain required for the `STATE-05` Human Gate in both languages.
- Theme completion on WPF remains a separate Design System increment; localisation resources do not imply that Light/Dark/System parity is complete there.

## Recommendation

Complete the WPF Light/Dark/System and High Contrast increment, then repeat the full bilingual automatic re-audit before requesting the remaining human accessibility samples.
