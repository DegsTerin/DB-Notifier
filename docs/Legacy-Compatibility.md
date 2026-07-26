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

- The canonical visual default uses display name `DB Notifier`; the technical
  storage path remains `%LocalAppData%\DB-Notifier\logs\dbnotifier.log`.
- Existing PgNotifier JSON is still accepted when its path is passed explicitly through either entry point.
- Legacy field names such as `pgIsReady` remain validated compatibility input for the versioned migrator and existing explicitly supplied legacy files; canonical generated and sample DB-Notifier configuration omits them.
- Remote legacy entries remain parseable and migratable, but the compatibility runtime admits only the exact `localhost` alias or a loopback IP literal. A remote name or address is refused before DNS, `pg_isready` or TCP execution; remote monitoring belongs to the policy-bound Agent/provider architecture.
- No shim moves, edits, or deletes a legacy configuration file.
- `DBNotifier.ConfigMigrator` dry-runs by default, creates a durable authenticated recovery journal before replacement, rejects path/link/concurrency ambiguity, blocks secrets and writes only a new DB-Notifier configuration.

## Packaging compatibility

- Compatibility artifacts are `DBNotifier.exe` and `DBNotifier-Setup.exe`; neither is a production Agent/Desktop release.
- Historical `DBNotifierDesktop.exe`, `DBNotifierTray.exe`, and `DBNotifierPixelUI.exe` prototype packaging is blocked and non-distributable. Those names do not identify supported product artifacts.
- The DB-Notifier installer has a distinct application ID and `%ProgramData%\DB-Notifier` configuration root, allowing side-by-side rollback.
- The compatibility `.iss` path does not recreate PgNotifier output names; it only keeps automation from failing immediately while producing the canonical package.
- The functional PowerShell compatibility client consumes the generated `DBNotifier.{Healthy,Warning,Critical,Unknown}.ico` family for its notification-area and popup marks. Packaging copies those assets into `Assets`; the compatibility path must fail safely to Unknown and must not draw or ship a separate legacy logo.
- Compatibility packaging remains unavailable in R5. Its manifest contract requires every executable and transitive dependency to carry an exact version, SHA-256 hash, role and official provenance; even a complete future manifest cannot enable generation without separate authority. Prototype packaging fails closed. No build script installs or downloads tooling.

## Removal gate

Deprecated names may be removed only after all of the following are evidenced:

1. The versioned configuration migrator is released and its rollback is tested.
2. A supported DB-Notifier release can import representative PgNotifier configurations without data loss.
3. Upgrade and side-by-side installer paths are homologated.
4. Usage telemetry or an approved operational inventory shows no required legacy entry point.
5. Release notes announce removal at least one supported release in advance.
6. The removal is approved through the owning lifecycle gate and recorded in the transition log.

There is no calendar-based removal date while these conditions remain unmet.
