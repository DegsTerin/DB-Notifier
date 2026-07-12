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
  DBNotifier.Desktop.Wpf/
  DBNotifier.Dashboard.Web/
tests/
  DBNotifier.UnitTests/
  DBNotifier.Architecture.Tests/
  DBNotifier.Legacy.Tests.ps1
```

The projects created in `STATE-01` began as infrastructure-only bootstrap code and assembly markers. `STATE-03` added isolated persistence assemblies so the Agent carries SQLite without PostgreSQL and the Server carries PostgreSQL without SQLite. The first two `STATE-04` increments add provider-neutral health contracts, an open provider registry, bounded probe/scheduling policies, PostgreSQL readiness/authenticated adapters, and transactional local observation/outbox persistence without adding UI.

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

`src/DBNotifier.Dashboard.Web` contains only a TypeScript/React build scaffold. Its CI job uses the committed lockfile and runs:

```powershell
npm ci
npm run check
npm run build
```

Do not add product screens or monitoring behavior until their owning lifecycle state.

## Configuration and secrets

- Commit only safe defaults and samples.
- Use environment variables or an approved secret store for local secret material.
- Never put passwords, tokens, connection strings, or encryption keys in `appsettings.json`.
- Monitoring and administrative identities remain separate.

## CI

`.github/workflows/ci.yml` defines initial jobs for the .NET solution, legacy compatibility suite, and Dashboard. It is not evidence of a passing remote run until the repository is published and the workflow actually executes.
