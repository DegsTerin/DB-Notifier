# Development Bootstrap

## Supported setup baseline

- Git with the default branch `main`.
- .NET 10 LTS SDK `10.0.301` selected through `global.json` and installed locally in the ignored `.dotnet/` directory for this workspace.
- Windows is required to run the WPF Desktop and the renamed DB-Notifier compatibility application.
- Node.js 24 is used for the Dashboard scaffold; dependencies are locked in `package-lock.json`.
- Windows PowerShell 5.1 with Pester is used for legacy characterization.

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

Agent synchronization also remains disabled by default, and command polling has an additional independent `CommandPollingEnabled` switch. Enabling it requires the same Agent ID, an absolute HTTPS server address, bounded cadence/batch size, an exact Agent/provider-version inventory, and a valid client certificate with private key in the current identity's personal certificate store. The Server accepts ingestion, command polling and command ack only after certificate enrollment lookup and exact Agent route authorization. Polling stores commands as `Acknowledged`; it does not execute Start/Stop/Restart or create a command attempt.

Human API endpoints use the separate `HumanBearer` JWT scheme. Configure an absolute HTTPS OIDC authority and audience through `HumanAuthentication`; absent or invalid configuration fails closed. The JWT `sub` must map to an active platform user with a non-expired role assignment and the exact permission/scope. No password, signing key, token, or bootstrap administrator is stored in repository configuration.

Agent retention and Server retention/outbox/notification workers are separately opt-in. Defaults are disabled, and retention defaults to dry-run even when enabled. No external publisher or notification adapter is configured by the repository; enabling delivery without one records bounded retry state rather than contacting a channel.

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
```

All active projects target `net10.0` or `net10.0-windows`. Warnings are treated as errors, nullable analysis and .NET analyzers are enabled, and the Domain dependency direction has a baseline architecture test.

## Legacy checks

```powershell
Invoke-Pester .\tests\DBNotifier.Legacy.Tests.ps1
```

These tests characterize the only functional legacy provider behavior under its new canonical DB-Notifier paths. Deprecated PgNotifier shims are covered separately and must not be confused with new provider support.

## Dashboard bootstrap

`src/DBNotifier.Dashboard.Web` contains the deterministic STATE-05 React demonstration views and Design System theme runtime. It has no external Agent, API, database, IdP or administrative integration. Its CI job uses the committed lockfile and runs:

```powershell
npm ci
npm run tokens:verify
npm run check
npm run comments:verify
npm test
npm run build
```

Keep presentation adapters deterministic until the owning integration state explicitly authorises external dependencies.

## Configuration and secrets

- Commit only safe defaults and samples.
- Use environment variables or an approved secret store for local secret material.
- Never put passwords, tokens, connection strings, or encryption keys in `appsettings.json`.
- Monitoring and administrative identities remain separate.

## CI

`.github/workflows/ci.yml` defines initial jobs for the .NET solution, legacy compatibility suite, and Dashboard. It is not evidence of a passing remote run until the repository is published and the workflow actually executes.
