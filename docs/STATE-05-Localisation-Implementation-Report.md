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

- Discreet `pt-BR` and `en-GB` pressed-state buttons in the upper-right TopBar apply copy immediately, update the document `lang`, title and description, and announce the selected language.
- The bootstrap reads `dbnotifier.language.preference.v1` before React render, avoiding an initial language mismatch where valid storage is available.
- Storage denial or malformed values do not block rendering; cross-tab changes accept only supported locales.
- Demonstration fixtures, navigation, states, filters, tables/cards and confirmation copy use typed catalogue keys.

### WPF Desktop and Tray

- Discreet `pt-BR` and `en-GB` radio buttons in the upper-right TopBar replace a generated localisation `ResourceDictionary` while preserving the window, selected view, selected theme and safe demonstration state.
- Only schema, validated locale and selected semantic theme are stored together in `%LocalAppData%\DB-Notifier\ui-preferences.v1.json`; writes use temporary-file replacement and failure remains session-local.
- Dynamic XAML resources update structural copy, accessible names and headers; code-generated fixture/status/dialog values are rebuilt through the same catalogue.
- Tray menu, tooltip and first-hide notification update with the active locale.

## Responsive scope

The React Dashboard is the adaptive interface for compact mobile, tablet, laptop and desktop viewports. Its existing content-driven breakpoints cover 320, 390, 768, 1024 and 1440 CSS-pixel samples, with wrapped/hideable selector labels and no intentional global horizontal scrolling. The native WPF application is a Windows desktop surface with a supported minimum of `820×620` DIP; it is not presented as an Android/iOS tablet or mobile application.

## Verification

| Gate | Observed result |
|---|---|
| Canonical catalogue | Approved; 196 keys per locale with exact key/placeholder parity |
| Generated adapter drift | Approved for TypeScript and both WPF dictionaries |
| Dashboard | Approved; typecheck, 19/19 tests and Vite production build |
| .NET 10 solution | Approved; Release build with 0 warnings/errors and 136/136 tests |
| Source documentation/format | Approved; 160 comment-capable files and `dotnet format` clean |
| Dependencies | Approved; npm reported 0 vulnerabilities; NuGet remained covered by the solution gate |
| Browser responsive/accessibility | Approved; 54 locale/theme/viewport-route samples, 0 global overflow, 0 unnamed interactive controls and modal entry/Escape/restoration passing |
| WPF UI Automation | Approved across all six locale/theme combinations and the 820×620 minimum; deterministic visible Tab samples recorded |
| Legacy compatibility | Approved; canonical and deprecated entry-point samples passed |

Sanitised runtime screenshots and JSON remain under `%TEMP%\DBNotifier-State05-Audit\<locale>\<theme>`; they are deliberately not committed.

## Boundaries and remaining evidence

- This increment proves implemented interface localisation, not provider homologation or production readiness.
- Human screen-reader review and native 200% zoom/scaling samples remain required for the `STATE-05` Human Gate in both languages.
- Windows High Contrast and native 125%, 150% and 200% scaling remain human/native-environment samples even though the WPF runtime support is implemented.

## Recommendation

Complete the remaining human bilingual screen-reader, native zoom/scaling, High Contrast and visual samples before presenting the `STATE-05` Human Gate.
