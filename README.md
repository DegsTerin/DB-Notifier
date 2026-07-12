# DB-Notifier

DB-Notifier is the successor to PgNotifier, which was conceptually inspired by MySQL Notifier. DB-Notifier evolves that lineage into an independent, open multi-provider platform designed to accept any database engine through versioned providers/plugins, built around an Agent, Desktop/Tray client, central API, and Web Dashboard.

Agents are designed to monitor local, remote, datacenter, hybrid, and cloud databases from Windows, Linux, containers, or cloud workloads. Monitoring credentials, database administration credentials, OS service identities, and cloud control-plane identities remain separate and vault-backed.

The workspace is currently in `STATE-04 BACKEND_IMPLEMENTATION` after approval of the database-modeling Human Gate. Eight neutral backend increments now provide the open Provider SDK, PostgreSQL discovery/readiness/authenticated adapters, hosted scheduling, controlled SQLite initialization/assignments/outbox, read-only Windows/Linux vault adapters, idempotent synchronization, canonical events/alerts, certificate-authorized Agent ingestion, OIDC/JWT human RBAC/audit, command delivery/ack without execution, bounded retention/delivery, signed provider-package verification without loading, and a safe PgNotifier configuration migrator. The remediation re-audit is `APROVADO`; Human Gate and transition remain pending. No real database/vault/channel was exercised and no provider is homologated.

## Start here

All work governed by this repository starts at [`prompts/Start-Here.md`](prompts/Start-Here.md). It defines the required reading order, current state, authority, lifecycle, quality gates, and safety limits.

The discovery outputs for the transformation are:

- [`docs/Legacy-Inventory.md`](docs/Legacy-Inventory.md): verified behavior, assets, limitations, and risks in PgNotifier.
- [`docs/Legacy-Migration-Plan.md`](docs/Legacy-Migration-Plan.md): incremental migration, compatibility contract, milestones, rollback, and gate criteria.
- [`docs/Legacy-Compatibility.md`](docs/Legacy-Compatibility.md): canonical names and explicit deprecated shims.
- [`docs/STATE-00-Discovery-Report.md`](docs/STATE-00-Discovery-Report.md): discovery evidence and the approved Human Gate.

The project has completed the approved discovery, setup, and architecture gates. See [`prompts/state/Current-State.md`](prompts/state/Current-State.md) for the factual state and [`docs/Development.md`](docs/Development.md) for onboarding commands.

## Current legacy application

The functional implementation is a Windows tray monitor written in PowerShell 5.1:

```text
src/app/DBNotifier.ps1
src/modules/DBNotifier/
config/appsettings.json
tests/DBNotifier.Legacy.Tests.ps1
packaging/inno/DBNotifier.iss
```

It monitors local or remote PostgreSQL endpoints with `pg_isready.exe` and falls back to a TCP reachability check. Local Windows services can optionally expose Start, Stop, and Restart actions. These administrative actions must not be exercised on a real service without explicit authorization.

Deprecated `PgNotifier` entry points remain only as compatibility shims. They forward to the DB-Notifier implementation, emit a deprecation warning, and preserve explicit legacy configuration paths without overwriting them.

The folders `desktop-wpf/`, `pixel-ui/`, and `tray-app/` are visual prototypes backed by mock data. They are not integrated DB-Notifier clients.

## Safe legacy checks

On Windows PowerShell with Pester installed:

```powershell
Invoke-Pester .\tests\DBNotifier.Legacy.Tests.ps1
```

To create the PowerShell compatibility executable after explicitly installing `ps2exe`:

```powershell
.\build\build.ps1 -SkipInstaller
```

Without `-SkipInstaller`, Inno Setup 6 must also be available. Build scripts never install those tools automatically.

The bundle composition can be checked without packaging dependencies:

```powershell
.\build\build.ps1 -ValidateOnly
```

## Target direction

The mandatory project baseline uses .NET 10 LTS/C# for Domain, Application, providers, Agent, API, and WPF Desktop, plus React/TypeScript for the Dashboard. No active project targets an earlier .NET version. Accepted ADRs define SQLite for authorized local Agent state, PostgreSQL for central persistence, versioned provider contracts, and an open provider catalog.

The migration is deliberately incremental. The legacy application stays runnable while contracts and scaffolding are introduced, PostgreSQL behavior is placed behind a provider boundary, configuration is migrated with backup and validation, and each milestone has an explicit rollback path.

## Safety

- Do not commit connection strings, passwords, tokens, or full secret material.
- Keep monitoring and administrative credentials separate.
- Do not run service controls, remote migrations, installs, or deployments without explicit authorization.
- Treat TCP reachability as degraded transport evidence, never as proof that a database is healthy.
- Do not announce a provider as supported before implementation and homologation.

## License

See [`LICENSE`](LICENSE).
