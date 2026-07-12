# Legacy Naming Compatibility

## Policy

DB-Notifier is the canonical product name. `DBNotifier` is used where identifiers cannot contain a hyphen. PgNotifier names remain only where required to preserve an existing invocation or to document migration history.

Compatibility code must forward to the canonical implementation, emit a deprecation warning where practical, and never fork product behavior.

## Canonical and deprecated mappings

| Canonical DB-Notifier artifact | Deprecated compatibility artifact | Behavior |
|---|---|---|
| `src/app/DBNotifier.ps1` | `src/app/PgNotifier.ps1` | Wrapper forwards `-ConfigPath` unchanged |
| `src/modules/DBNotifier/DBNotifier.psd1` | `src/modules/PgNotifier/PgNotifier.psd1` | Old module exports only the deprecated start command |
| `Start-DBNotifierApplication` | `Start-PgNotifierApplication` | Deprecated function warns and delegates |
| `tests/DBNotifier.Legacy.Tests.ps1` | `tests/PgNotifier.Tests.ps1` | Old test path warns and loads the canonical suite |
| `desktop-wpf/DBNotifier.Desktop.ps1` | `desktop-wpf/PgNotifier.Desktop.ps1` | Old prototype entry point warns and delegates |
| `packaging/inno/DBNotifier.iss` | `packaging/inno/PgNotifier.iss` | Old include path compiles the canonical installer definition |

## Configuration compatibility

- The canonical default uses display name `DB-Notifier` and `%LocalAppData%\DB-Notifier\logs\dbnotifier.log`.
- Existing PgNotifier JSON is still accepted when its path is passed explicitly through either entry point.
- Legacy field names such as `pgIsReady` are PostgreSQL provider settings, not product branding, and remain unchanged until a versioned configuration migrator exists.
- No shim moves, edits, or deletes a legacy configuration file.
- The future migration tool must dry-run, back up, validate, report, and write only a new DB-Notifier configuration.

## Packaging compatibility

- New artifacts are `DBNotifier.exe`, `DBNotifier-Setup.exe`, `DBNotifierDesktop.exe`, `DBNotifierTray.exe`, and `DBNotifierPixelUI.exe`.
- The DB-Notifier installer has a distinct application ID and `%ProgramData%\DB-Notifier` configuration root, allowing side-by-side rollback.
- The compatibility `.iss` path does not recreate PgNotifier output names; it only keeps automation from failing immediately while producing the canonical package.
- Build scripts report missing prerequisites and never install `ps2exe`, PyInstaller, or Inno Setup automatically.

## Removal gate

Deprecated names may be removed only after all of the following are evidenced:

1. The versioned configuration migrator is released and its rollback is tested.
2. A supported DB-Notifier release can import representative PgNotifier configurations without data loss.
3. Upgrade and side-by-side installer paths are homologated.
4. Usage telemetry or an approved operational inventory shows no required legacy entry point.
5. Release notes announce removal at least one supported release in advance.
6. The removal is approved through the owning lifecycle gate and recorded in the transition log.

There is no calendar-based removal date while these conditions remain unmet.
