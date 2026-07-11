# PgNotifier Legacy Inventory

## Purpose and scope

This inventory records the behavior observed in the workspace on 2026-07-11. It is a `STATE-00 DISCOVERY_MIGRATION` artifact and does not claim that the DB-Notifier target architecture exists.

No real database, Windows service action, installer, deployment, or remote environment was exercised during discovery.

## Executable assets

| Area | Asset | Observed role | Classification |
|---|---|---|---|
| Entry point | `src/app/PgNotifier.ps1` | Resolves configuration, enforces STA, imports the module, and starts the tray application | Functional legacy |
| Core and UI | `src/modules/PgNotifier/PgNotifier.psm1` | Configuration, discovery, probes, state calculation, WinForms tray/popup, logging, and service actions in one 1,356-line module | Functional legacy, highly coupled |
| Module metadata | `src/modules/PgNotifier/PgNotifier.psd1` | PowerShell 5.1 manifest exporting `Start-PgNotifierApplication` | Functional legacy |
| Configuration | `config/appsettings.json` | Safe default with automatic discovery and no fixed instances | Functional legacy |
| Example | `examples/appsettings.sample.json` | Local, alternate-port, and remote PostgreSQL examples | Documentation/sample |
| Tests | `tests/PgNotifier.Tests.ps1` | Eight Pester checks for manifest, JSON, normalization, discovery assumptions, paths, and remote configuration | Functional legacy tests |
| Installer | `packaging/inno/PgNotifier.iss` | Inno Setup definition for the old executable and configuration path | Legacy packaging definition |
| WPF prototype | `desktop-wpf/` | PowerShell/XAML UI driven by mock JSON | Prototype only |
| Pixel prototype | `pixel-ui/` | Static Tkinter visual recreation | Prototype only |
| Tray prototype | `tray-app/` | Tkinter/pystray popup with fixed instances; only Exit is functional | Prototype only |

The root README previously referenced `build/build.ps1`, but that file and directory are absent. A clean legacy package cannot be reproduced from the checked-in workspace as it stands.

## Observed behavior

### Configuration

- Loads JSON and recursively merges it over defaults.
- Expands environment variables and resolves relative paths from the configuration directory.
- Falls back to `%LocalAppData%\PgNotifier\logs\pgnotifier.log` when the configured log is not writable.
- Supports an empty `instances` array and optional automatic discovery.
- Deduplicates instances by Windows service name or `host:port`.
- Silently replaces missing, empty, or invalid JSON with default configuration.

### PostgreSQL discovery and monitoring

- Discovers Windows services whose name starts with `postgresql` or whose display name contains `PostgreSQL`.
- Attempts to infer the port from PostgreSQL registry keys and otherwise uses `5432`.
- Resolves `pg_isready.exe` from configuration, the configuration folder, `PATH`, an instance's PostgreSQL `bin` folder, or standard installation folders.
- Executes `pg_isready` with host, port, timeout, retries, delay, and configured extra arguments.
- Uses a raw TCP connection when `pg_isready` is unavailable.
- Distinguishes legacy states `UP`, `UP_TCP_ONLY`, `RESTARTED`, `STOPPED`, `RUNNING_NO_CONN`, `MISSING`, and `UNKNOWN`.
- Detects a restart by a changed Windows service PID and displays a temporary restart badge.

### User interface and operations

- Hosts a WinForms notification icon and popup.
- Displays up to the current resolved instance collection with a selected instance.
- Opens the log and configuration, reloads configuration, and exits.
- Enables Start, Stop, or Restart only for a configured/discovered local Windows service when `restartAllowed` is true.
- Executes service control synchronously through PowerShell service cmdlets and logs success or failure.

## Compatibility baseline to preserve

The first DB-Notifier PostgreSQL milestone must preserve, or explicitly deprecate with a documented replacement, the following:

1. Installation-free source execution on a supported Windows development machine during the transition.
2. Empty configuration startup without requiring PostgreSQL.
3. Discovery of local PostgreSQL Windows services.
4. Explicit local and remote `host:port` entries.
5. `pg_isready` resolution and bounded execution.
6. A clearly degraded TCP-only result when no PostgreSQL-aware probe is available.
7. Visible stopped, unavailable, timeout, restarted, and unknown conditions.
8. Local service control remaining disabled by default for remote endpoints.
9. User-writable logs and configuration reload.
10. Migration from existing PgNotifier JSON without deleting or overwriting the source file.

## Gaps and risks

| Severity | Finding | Migration requirement |
|---|---|---|
| High | Monitoring, service control, configuration, logging, and UI share one PowerShell module | Introduce Domain/Application/provider boundaries before expanding engines |
| High | A successful TCP handshake can be displayed as a healthy-running state even though PostgreSQL readiness was not proven | Map TCP-only to `Degraded` with method and timestamp |
| High | Administrative actions have no RBAC, reason, idempotency key, durable audit entry, expiry, or post-command verification | Do not carry the legacy execution path into distributed control; implement the governed command contract first |
| High | Invalid configuration silently falls back to defaults | Fail safely with a visible validation error and preserve the last known valid configuration |
| High | The documented root build script is missing | Establish a reproducible build in `STATE-01`; do not claim packaging readiness |
| Medium | `NotificationsEnabled`, the global notification flag, log level, and start-minimized setting are parsed but not consistently enforced | Define configuration semantics and cover them with tests |
| Medium | `extraArguments` are converted to a process argument string by custom quoting | Replace with a typed argument API and validate provider-owned options |
| Medium | State lacks canonical `observedAt`, `receivedAt`, stale status, and event history | Add canonical health results and persistence contracts |
| Medium | The tests do not exercise real probes, timeout behavior, invalid JSON reporting, service authorization, UI, or packaging | Add characterization and negative tests before replacing the legacy path |
| Medium | The WPF and Python clients duplicate a fixed three-row mock design | Choose one production Desktop path and archive prototypes only after useful visual behavior is captured |
| Low | Legacy naming appears throughout paths, package identifiers, log locations, and installer identity | Use a compatibility window and side-by-side-safe upgrade rules rather than a blind global rename |

## Capability truth at discovery

| Capability | PostgreSQL legacy | Other providers |
|---|---|---|
| Local Windows service discovery | Observed in code; not exercised against a real service in this run | Not implemented |
| PostgreSQL-aware readiness | Implemented with `pg_isready`; unit path checks pass | Not implemented |
| TCP reachability fallback | Implemented; degraded semantics need correction | Generic transport primitive only, not provider support |
| Remote endpoint configuration | Covered by a passing normalization test | Not implemented as provider support |
| Start/Stop/Restart | Implemented for local Windows services; not exercised in discovery | Not implemented |
| History, alerts, RBAC, audit, Agent, API, Dashboard | Not implemented | Not implemented |

## Discovery evidence

- Environment: WSL2 orchestration with Windows PowerShell `5.1.26100.8737`.
- Pester: `3.4.0`.
- Test result: 8 passed, 0 failed, 0 skipped, 0 pending.
- Repository status: the workspace was not a Git worktree at discovery time.
- Build status: blocked because `build/build.ps1` is absent.
- Runtime status: not tested against a real PostgreSQL instance or service.
