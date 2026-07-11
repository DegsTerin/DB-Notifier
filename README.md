# DB-Notifier

DB-Notifier is the planned successor to PgNotifier: a multi-provider platform for monitoring database instances through an Agent, Desktop/Tray client, central API, and Web Dashboard.

The workspace is currently in `STATE-00 DISCOVERY_MIGRATION`. The PowerShell PgNotifier application remains the only functional monitoring implementation and must be treated as legacy code, not as the final architecture. PostgreSQL is the only engine with observed legacy behavior; other engines are roadmap items until their providers are implemented and homologated.

## Start here

All work governed by this repository starts at [`prompts/Start-Here.md`](prompts/Start-Here.md). It defines the required reading order, current state, authority, lifecycle, quality gates, and safety limits.

The discovery outputs for the transformation are:

- [`docs/Legacy-Inventory.md`](docs/Legacy-Inventory.md): verified behavior, assets, limitations, and risks in PgNotifier.
- [`docs/Legacy-Migration-Plan.md`](docs/Legacy-Migration-Plan.md): incremental migration, compatibility contract, milestones, rollback, and gate criteria.
- [`docs/STATE-00-Discovery-Report.md`](docs/STATE-00-Discovery-Report.md): discovery evidence and the approved Human Gate.

The discovery gate has since been approved and the repository is now in `STATE-01 PROJECT_SETUP`. See [`docs/Development.md`](docs/Development.md) for the scaffold and onboarding commands.

## Current legacy application

The functional implementation is a Windows tray monitor written in PowerShell 5.1:

```text
src/app/PgNotifier.ps1
src/modules/PgNotifier/
config/appsettings.json
tests/PgNotifier.Tests.ps1
packaging/inno/PgNotifier.iss
```

It monitors local or remote PostgreSQL endpoints with `pg_isready.exe` and falls back to a TCP reachability check. Local Windows services can optionally expose Start, Stop, and Restart actions. These administrative actions must not be exercised on a real service without explicit authorization.

The folders `desktop-wpf/`, `pixel-ui/`, and `tray-app/` are visual prototypes backed by mock data. They are not integrated DB-Notifier clients.

## Safe legacy checks

On Windows PowerShell with Pester installed:

```powershell
Invoke-Pester .\tests\PgNotifier.Tests.ps1
```

The documented legacy command `build\build.ps1` is not available in this workspace. Packaging is therefore not currently reproducible from the root documentation.

## Target direction

The approved setup baseline uses .NET 8/C# for Domain, Application, providers, Agent, API, and WPF Desktop, plus React/TypeScript for the Dashboard. SQLite for authorized local Agent state, PostgreSQL for central persistence, protocol details, and other architectural commitments remain subject to ADRs in `STATE-02`.

The migration is deliberately incremental. The legacy application stays runnable while contracts and scaffolding are introduced, PostgreSQL behavior is placed behind a provider boundary, configuration is migrated with backup and validation, and each milestone has an explicit rollback path.

## Safety

- Do not commit connection strings, passwords, tokens, or full secret material.
- Keep monitoring and administrative credentials separate.
- Do not run service controls, remote migrations, installs, or deployments without explicit authorization.
- Treat TCP reachability as degraded transport evidence, never as proof that a database is healthy.
- Do not announce a provider as supported before implementation and homologation.

## License

See [`LICENSE`](LICENSE).
