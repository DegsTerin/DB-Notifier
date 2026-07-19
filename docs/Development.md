# Development Bootstrap

## Supported setup baseline

- Git with the default branch `main`.
- .NET 10 LTS SDK `10.0.301` selected without roll-forward through `global.json` and installed locally in the ignored `.dotnet/` directory for this workspace.
- Windows is required to run the WPF Desktop and the renamed DB-Notifier compatibility application. The Desktop uses the versioned `net10.0-windows10.0.22621.0` TFM while retaining Windows 10 version 1809 (`10.0.17763.0`) as its declared minimum platform.
- Node.js `24.18.0` with npm `11.16.0` is used for the Dashboard scaffold; both are declared in `package.json`, and dependencies are locked in `package-lock.json`.
- Windows PowerShell 5.1 with Pester `3.4.0` is used for legacy characterisation.

Installing an SDK or dependency changes the development environment and is not performed automatically by this repository.

## Repository layout

```text
src/
  DBNotifier.Domain/
  DBNotifier.Application/
  DBNotifier.Provider.Abstractions/
  DBNotifier.Providers.PostgreSql/
  DBNotifier.Infrastructure/
  DBNotifier.Persistence.Agent.Sqlite/
  DBNotifier.Persistence.Server.PostgreSql/
  DBNotifier.Agent.Worker/
  DBNotifier.Server.Api/
  DBNotifier.ConfigMigrator/
  DBNotifier.Desktop.Wpf/
  DBNotifier.Dashboard.Web/
tests/
  DBNotifier.UnitTests/
  DBNotifier.Architecture.Tests/
  DBNotifier.Legacy.Tests.ps1
```

The projects created in `STATE-01` began as infrastructure-only bootstrap code and assembly markers. `STATE-03` added isolated persistence assemblies so the Agent carries SQLite without PostgreSQL and the Server carries PostgreSQL without SQLite. Eight `STATE-04` increments add provider-neutral health contracts, an open provider registry, bounded probes, a hosted scheduler, PostgreSQL discovery/readiness/authenticated adapters, controlled SQLite assignments/outbox, read-only platform vault adapters, idempotent synchronization, canonical events/alert deliveries, certificate-authorized Agent ingestion, OIDC/JWT human RBAC/audit, command delivery/ack without execution, bounded retention, durable delivery runners, authorized audit queries, signed/hash-verified provider-package discovery without loading code, and an isolated legacy configuration migrator without adding UI.

Agent monitoring remains `false` in `src/DBNotifier.Agent.Worker/appsettings.json`. Enabling it requires an explicit non-empty Agent ID and may initialize/migrate the configured absolute SQLite path. Credential references use `windows-credential-manager` or `linux-secret-service`; no plaintext file/environment vault fallback exists.

Agent synchronization remains disabled by default. Observation synchronization may be configured only with the same Agent ID, an absolute HTTPS server address, bounded cadence/batch size, an exact Agent/provider-version inventory, and a valid client certificate with private key in the current identity's personal certificate store. Command polling has an independent `CommandPollingEnabled` switch but currently rejects startup with `command.polling_durable_protocol_unavailable`; durable request identifiers, sequences and acknowledgement replay must be implemented before this path becomes enableable. The Server still protects the receipt-only command endpoints through certificate enrolment and exact Agent route authorisation, but no Start/Stop/Restart executor or command attempt exists.

Human API endpoints use the separate `HumanBearer` JWT scheme. Configure an absolute HTTPS OIDC authority and audience through `HumanAuthentication`; absent or invalid configuration fails closed. The JWT `sub` must map to an active platform user with a non-expired role assignment and the exact permission/scope. No password, signing key, token, or bootstrap administrator is stored in repository configuration.

After endpoint routing, the API selects certificate authentication only for Agent policies and `HumanBearer` only for human policies before applying identity-aware quotas. Public or unrecognised endpoints use a no-op authentication scheme, so an invalid bearer header on `/health/live` cannot trigger token validation or authentication-audit persistence.

Agent retention and Server retention are separately opt-in, with dry-run defaults. Server outbox and notification delivery remain disabled and now reject startup with `server.delivery_durable_lease_unavailable`; no external publisher, channel adapter or unsafe multi-worker delivery path is enableable until transactional claim/lease and idempotency are implemented. Raw server observations are preserved until an aggregate-before-delete contract exists.

## Windows Desktop runtime and local notifications

`DBNotifier.Desktop.Wpf` references the centrally pinned `Microsoft.WindowsAppSDK` package at version `2.2.0`. Its modern local app-notification path therefore requires a compatible Windows App Runtime 2.2 installation for the current process architecture. The repository does not install that machine dependency during restore, build or ordinary startup; installation and target-machine detection belong to the signed `STATE-08` package.

The Desktop sets `WindowsAppSdkBootstrapInitialize` to `false` and owns dynamic-dependency initialisation explicitly. Its notification publisher calls the Windows App SDK bootstrap API before registering with `AppNotificationManager`, and it releases that bootstrap lease during disposal. A missing or damaged runtime, unsupported platform, missing identity asset, or registration/publication failure is handled fail-safe: normal notification-area startup remains available and an explicit Close request can use the bounded legacy `NotifyIcon` balloon fallback. Diagnostics record only a stable stage and exception type, never notification content, local paths or external state.

During `STATE-05`, `AppNotificationManager` is used only for the local confirmation requested each time the user explicitly closes the secondary WPF shell to the notification area. Platform acceptance of that request is not proof that Windows displayed it, because Focus Assist, user policy and the Windows Shell remain authoritative. App-notification delivery from an elevated process is not treated as supported; the Desktop runs in the ordinary user session. Any observed presentation is also not evidence of provider integration, external delivery, administrative capability, homologation, public support or production readiness.

## Legacy configuration migration

The .NET 10 configuration migrator is dry-run by default and requires absolute source/target paths:

```powershell
& $dotnet run --project .\src\DBNotifier.ConfigMigrator -c Release -- `
  migrate --source "C:\path\to\PgNotifier\appsettings.json" `
  --target "C:\path\to\DB-Notifier\appsettings.json"
```

Add `--apply` only after reviewing the sanitized report. Apply preserves the source, creates timestamped source/previous-target backups, writes target/report/manifest atomically, and exact reruns return `AlreadyCurrent`. Rollback refuses a target whose hash changed:

```powershell
& $dotnet run --project .\src\DBNotifier.ConfigMigrator -c Release -- `
  rollback "C:\path\to\DB-Notifier\appsettings.json.migration-manifest.json"
```

Secret-shaped or unsupported fields reject the entire migration and produce no target or backup.

## .NET checks

From Windows PowerShell, use the workspace-local SDK:

```powershell
$dotnet = ".\.dotnet\dotnet.exe"
& $dotnet --info
& $dotnet restore .\DBNotifier.sln --locked-mode
& $dotnet build .\DBNotifier.sln --configuration Release --no-restore
& $dotnet test .\DBNotifier.sln --configuration Release --no-build
& $dotnet format .\DBNotifier.sln --verify-no-changes --no-restore
.\scripts\verify-dotnet-coverage.ps1 -DotNetPath $dotnet
```

All active projects target `net10.0` or an appropriate versioned .NET 10 Windows TFM; the WPF Desktop currently targets `net10.0-windows10.0.22621.0`. Warnings are treated as errors, nullable analysis and .NET analyzers are enabled, and the Domain dependency direction has a baseline architecture test.

The unit-test coverage gate requires at least 70% line coverage and 45% branch coverage. The separate legacy Pester gate requires at least 25% command coverage; these are regression floors for the current bounded suites, not claims of complete behavioural coverage.

## Legacy checks

```powershell
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\scripts\run-legacy-tests.ps1
```

These tests characterize the only functional legacy provider behavior under its new canonical DB-Notifier paths. Deprecated PgNotifier shims are covered separately and must not be confused with new provider support.

## Dashboard bootstrap

`src/DBNotifier.Dashboard.Web` contains the deterministic React demonstration views and Design System theme runtime. The normal build has no Agent, API, database, IdP or administrative integration. One restricted `STATE-06` adapter exists only for the exact `local-test` flag on an HTTPS loopback origin and reads the test-authenticated Dashboard TV sandbox endpoint; it does not admit an external endpoint or operational identity. The ordinary sandbox registration still supplies the fixed in-memory fixture. Only the integration-test host replaces that source with a read-only projection of synthetic observations received through Agent SQLite/outbox and HTTPS/mTLS; neither this projection nor its synthetic provider is registered by the normal API or Agent Worker. The Dashboard CI job uses the committed lockfile and runs:

```powershell
npm run toolchain:verify
npm ci
npm run brand:verify
npm run tokens:verify
npm run localisation:verify
npm run check
npm run comments:verify
npm run markdown:verify
npm test
npm run build
npm audit --audit-level=high
```

The separately authorised Dashboard TV browser composition gate uses only an already installed Chrome or Edge and never downloads a browser or package:

```powershell
.\scripts\run-state06-dashboard-tv-browser-e2e.ps1 -BrowserProduct Chrome
```

It builds the exact local-test Dashboard, starts one temporary HTTPS loopback host, creates an isolated browser profile, observes the 30-second cadence plus the deterministic recovery matrix, rejects external HTTP origins and removes every owned process/profile at the end. This is local sandbox evidence, not browser homologation or an operational runtime command.

The separately authorised observation-pipeline E2E reuses the existing Agent Fleet process harness. It starts only temporary .NET child processes and an HTTPS loopback test host, transfers P-256 test identity material through a current-user-only named pipe, reopens one fixture-owned Agent SQLite file across restarts and removes every owned process/database at the end. Run the focused scenario after a Release build with:

```powershell
dotnet test .\tests\DBNotifier.IntegrationTests\DBNotifier.IntegrationTests.csproj `
  --configuration Release --no-build `
  --filter "FullyQualifiedName~SyntheticObservationPipelineSurvivesOfflineReplayReorderStalenessAndRevocation"
```

This command is test evidence only. It does not activate monitoring, an operational provider, the normal Worker, SignalR, notifications or commands.

Keep presentation adapters deterministic and disabled by default until the owning integration state explicitly authorises another source or dependency.

## Configuration and secrets

- Commit only safe defaults and samples.
- Store local secret material only in an approved operating-system or corporate secret store and pass opaque references through configuration. Environment variables may carry non-secret switches and references, not secret values.
- Never put passwords, tokens, connection strings, or encryption keys in `appsettings.json`.
- Monitoring and administrative identities remain separate.

Run `.\scripts\verify-secrets.ps1` to scan non-ignored worktree files and available Git history using high-confidence signatures. The scanner prints only signature names and paths, never matched values. It supplements rather than replaces provider-native secret-store review.

## CI

`.github/workflows/ci.yml` defines bounded jobs for the .NET solution and coverage floor, legacy compatibility and coverage floor, Dashboard static/build/dependency gates, the isolated browser matrix, and history-aware secret scanning. Checkouts do not persist credentials, Node uses the exact supported patch, and concurrent runs for the same ref cancel older work.

GitHub Action tag-to-commit mappings could not be independently verified without network access during the current maintenance increment. The remaining `actions/*@v4` tags are therefore an explicit supply-chain residual: they must be replaced by reviewed full commit SHAs before a remote CI run is treated as supply-chain attestation. This limitation does not justify inventing a SHA or disabling otherwise useful local gates.

## Audit and packaging tool boundaries

The Dashboard CDP audit requires an explicit browser product and records its real CDP product/version. It does not silently label Edge as Chrome. The Lighthouse runner requires an already provisioned `lighthouse.cmd` at exactly `13.4.0`; it never calls `npx` to download code and enforces per-run performance, accessibility and best-practices thresholds.

The WPF automation runner now fails on incorrect window bounds, unnamed or insufficient focus targets, off-screen Tab traversal, broken preference cycles, unsupported DPI awareness and missing screenshot evidence. It remains a bounded shell sample: eight-destination content correctness, live-region speech, Narrator usability, modal/flyout experience, real High Contrast and perceptual quality remain explicit Human Gate samples rather than claims inferred from UI Automation.

`build/build.ps1 -ValidateOnly` remains dependency-free. Executable/installer packaging is blocked while `build/compatibility-toolchain.json` has status `blocked`; approval requires exact ps2exe and Inno Setup versions plus locally verified hashes. PowerShell/WPF, Pixel UI and Python Tray prototype executable builders are non-distributable and fail closed.

## Dependency maintenance status

The 2026-07-16 offline maintenance check found no advisory in the locally available NuGet/npm metadata; this is not fresh registry attestation. Compatible patch candidates existed for the Microsoft 10.0 servicing line, Microsoft.NET.Test.Sdk and Vite, but their packages were not present in the local caches. Manifests and generated lockfiles were therefore left unchanged rather than hand-edited or fetched without authority.

xUnit v3 migration is deferred to a separately verified dependency increment. Its exit conditions are: packages available from the constrained NuGet source, official migration compatibility reviewed, all test projects restored with regenerated lockfiles, analyser/source changes complete, the same or stronger test and coverage results observed, and rollback to the current xUnit 2.9.3 baseline documented. Availability of a newer major version alone is not authority to migrate.
