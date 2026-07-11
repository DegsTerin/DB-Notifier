# Development Bootstrap

## Supported setup baseline

- Git with the default branch `main`.
- .NET SDK `8.0.422` selected through `global.json` and installed locally in the ignored `.dotnet/` directory for this workspace.
- Windows is required to run the WPF Desktop and the PgNotifier legacy application.
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
  DBNotifier.Agent.Worker/
  DBNotifier.Server.Api/
  DBNotifier.Desktop.Wpf/
  DBNotifier.Dashboard.Web/
tests/
  DBNotifier.UnitTests/
  DBNotifier.Architecture.Tests/
  PgNotifier.Tests.ps1
```

The new projects contain infrastructure-only bootstrap code and assembly markers. Provider contracts, domain rules, persistence, monitoring, and product UI do not belong to `STATE-01`.

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

Warnings are treated as errors, nullable analysis and .NET analyzers are enabled, and the Domain dependency direction has a baseline architecture test.

## Legacy checks

```powershell
Invoke-Pester .\tests\PgNotifier.Tests.ps1
```

These tests characterize the only functional legacy provider behavior. They must stay separate from claims about DB-Notifier provider support.

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

`.github/workflows/ci.yml` defines initial jobs for the .NET solution, PgNotifier Pester suite, and Dashboard. It is not evidence of a passing remote run until the repository is published and the workflow actually executes.
