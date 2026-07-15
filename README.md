# DB-Notifier

DB-Notifier is the successor to PgNotifier, which was conceptually inspired by MySQL Notifier. DB-Notifier evolves that lineage through an independent clean-room implementation: public behavioural documentation may inform requirements, but Oracle/MySQL code, assets and identity are not reused. The result remains an MIT-licensed, open multi-provider platform designed to accept any database engine through versioned providers/plugins, built around an Agent, Desktop/Tray client, central API, and Web Dashboard.

Agents are designed to monitor local, remote, datacenter, hybrid, and cloud databases from Windows, Linux, containers, or cloud workloads. Monitoring credentials, database administration credentials, OS service identities, and cloud control-plane identities remain separate and vault-backed.

The workspace is technically positioned in `STATE-05 FRONTEND_IMPLEMENTATION`, with lifecycle progression on hold. Ten frontend increments provide a provider-neutral operational Overview, eight-destination Dashboard navigation, inventory, history/alerts, performance/provider demonstrations, configuration/capabilities, a safe notification-area-first Windows Tray, an operational demonstration flyout and a clean-room aggregate-state policy in React/.NET 10 WPF. Retrospective ratification of `STATE-00` through `STATE-04` is complete; the current `STATE-05` Human Gate remains pending. All views are demonstration-only, with no external integration, mutation, database/service control or provider homologation.

## Start here

Repository agents start with [`AGENTS.md`](AGENTS.md), the primary source for permanent and reusable working instructions. All governed work then follows [`prompts/Start-Here.md`](prompts/Start-Here.md), which defines detailed routing, current state, authority, lifecycle, quality gates, and safety limits.

Code and configuration documentation follows [`docs/Code-Documentation-Standards.md`](docs/Code-Documentation-Standards.md): comments use British English, document intent and remain synchronised with implementation. Run `npm run comments:verify` from `src/DBNotifier.Dashboard.Web` to check the project-wide module inventory.

All frontend work follows the official [`DB-Notifier Design System`](docs/design/DB-Notifier-Design-System.md): a restrained modern enterprise identity, shared React/WPF semantic tokens, WCAG 2.2 AA and explicit Light/Dark theme contracts. Retired System values migrate safely to Light, while High Contrast remains an independent accessibility override. The current Design System is `2.4.1`; the Windows client is notification-area-first and uses its full WPF shell only as a secondary drill-down, while the Web Dashboard remains the complete responsive surface. Implementation increments and automatic audits are approved only in their tested scope, while the Human Gate remains pending. The complete request-to-evidence cross-reference is maintained in the [`STATE-05 request traceability audit`](docs/STATE-05-Request-Traceability-Audit.md).

The discovery outputs for the transformation are:

- [`docs/Legacy-Inventory.md`](docs/Legacy-Inventory.md): verified behavior, assets, limitations, and risks in PgNotifier.
- [`docs/Legacy-Migration-Plan.md`](docs/Legacy-Migration-Plan.md): incremental migration, compatibility contract, milestones, rollback, and gate criteria.
- [`docs/Legacy-Compatibility.md`](docs/Legacy-Compatibility.md): canonical names and explicit deprecated shims.
- [`docs/STATE-00-Discovery-Report.md`](docs/STATE-00-Discovery-Report.md): historical discovery evidence and the original Human Gate record, now retrospectively ratified with explicit scope limits.
- [`docs/Human-Gate-Retrospective-Ratification.md`](docs/Human-Gate-Retrospective-Ratification.md): completed independent retrospective ratification records for `STATE-00` through `STATE-04`.

The project has technically completed discovery, setup, architecture, database modelling and backend implementation, and their retrospective Human Gate authority is now recorded independently. The project remains in `STATE-05` until its separate human visual/accessibility gate is completed. See [`prompts/state/Current-State.md`](prompts/state/Current-State.md) for the factual hold and [`docs/Development.md`](docs/Development.md) for onboarding commands.

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
