# DB-Notifier

DB-Notifier is the successor to PgNotifier, which was conceptually inspired by MySQL Notifier. DB-Notifier evolves that lineage through an independent clean-room implementation: public behavioural documentation may inform requirements, but Oracle/MySQL code, assets and identity are not reused. The result remains an MIT-licensed, open multi-provider platform designed to accept any database engine through versioned providers/plugins, built around an Agent, Desktop/Tray client, central API, and Web Dashboard.

Agents are designed to monitor local, remote, datacenter, hybrid, and cloud databases from Windows, Linux, containers, or cloud workloads. Monitoring credentials, database administration credentials, OS service identities, and cloud control-plane identities remain separate and vault-backed.

The workspace is technically positioned in `STATE-06 INTEGRATION`. Its normal composition remains fail-closed: monitoring and synchronisation are disabled by default, ordinary user interfaces show identified local demonstration data, and authorised integration compositions remain opt-in and test-only. `ActivationState=None`; no provider is publicly supported or operationally homologated, and no administrative execution, external runtime, deployment or lifecycle transition is enabled. The authoritative present-tense snapshot is [`prompts/state/Current-State.md`](prompts/state/Current-State.md); historical implementation and gate evidence remains in the owning reports.

Server-side evidence is recorded in the [`STATE-06 Agent Identity and Fleet Integration Report`](docs/STATE-06-Agent-Identity-And-Fleet-Integration-Report.md). Agent-side and resilience evidence remain in their separate reports. Dashboard TV evidence is split between the [`authoritative snapshot and periodic reconciliation report`](docs/STATE-06-Dashboard-TV-Authoritative-Reconciliation-Report.md), the later [`browser composition and recovery report`](docs/STATE-06-Dashboard-TV-Browser-Composition-And-Recovery-Evidence-Report.md), and the integrated [`authoritative observation pipeline E2E report`](docs/STATE-06-Authoritative-Observation-Pipeline-E2E-Sandbox-Report.md).

## Start here

Repository agents start with [`AGENTS.md`](AGENTS.md), the primary source for permanent and reusable working instructions. All governed work then follows [`prompts/Start-Here.md`](prompts/Start-Here.md), which defines detailed routing, current state, authority, lifecycle, quality gates, and safety limits.

Code and configuration documentation follows [`docs/Code-Documentation-Standards.md`](docs/Code-Documentation-Standards.md): comments use British English, document intent and remain synchronised with implementation. Run `npm run comments:verify` from `src/DBNotifier.Dashboard.Web` to check the project-wide module inventory.

All frontend work follows the official [`DB-Notifier Design System`](docs/design/DB-Notifier-Design-System.md): a restrained modern enterprise identity, shared React/WPF semantic tokens, WCAG 2.2 AA and explicit Light/Dark theme contracts. Retired System values migrate safely to Light, while High Contrast remains an independent accessibility override. The current Design System is `3.2.1`; the independent product-mark asset revision remains `2.6.13`. The Windows client is notification-area-first and uses its full WPF shell only as a secondary drill-down, while the Web Dashboard remains the complete responsive surface. The completed `STATE-05` implementation and its Human Gate remain bounded by their recorded demonstration and environmental limitations. The complete request-to-evidence cross-reference is maintained in the [`STATE-05 request traceability audit`](docs/STATE-05-Request-Traceability-Audit.md).

The discovery outputs for the transformation are:

- [`docs/Legacy-Inventory.md`](docs/Legacy-Inventory.md): verified behavior, assets, limitations, and risks in PgNotifier.
- [`docs/Legacy-Migration-Plan.md`](docs/Legacy-Migration-Plan.md): incremental migration, the audited clean-room MySQL Notifier functional-coverage matrix, compatibility contract, milestones, rollback, and gate criteria.
- [`docs/Legacy-Compatibility.md`](docs/Legacy-Compatibility.md): canonical names and explicit deprecated shims.
- [`docs/STATE-00-Discovery-Report.md`](docs/STATE-00-Discovery-Report.md): historical discovery evidence and the original Human Gate record, now retrospectively ratified with explicit scope limits.
- [`docs/Human-Gate-Retrospective-Ratification.md`](docs/Human-Gate-Retrospective-Ratification.md): completed independent retrospective ratification records for `STATE-00` through `STATE-04`.

The project has technically completed discovery, setup, architecture, database modelling, backend implementation and the governed frontend phase. It is now executing only explicitly authorised, restricted `STATE-06` integration increments; completing one increment does not promote the lifecycle or activate a planned capability automatically. See [`prompts/state/Current-State.md`](prompts/state/Current-State.md) for current truth and [`docs/Development.md`](docs/Development.md) for onboarding commands.

## Current legacy application

The functional implementation is a Windows tray monitor written in PowerShell 5.1:

```text
src/app/DBNotifier.ps1
src/modules/DBNotifier/
config/appsettings.json
tests/DBNotifier.Legacy.Tests.ps1
packaging/inno/DBNotifier.iss
```

It monitors local or remote PostgreSQL endpoints with `pg_isready.exe` and falls back to a TCP reachability check that is presented only as unproved transport evidence. The legacy client contains no Start, Stop, or Restart executor; these administrative capabilities remain `Unsupported` until their governed contract and homologation are complete.

Deprecated `PgNotifier` entry points remain only as compatibility shims. They forward to the DB-Notifier implementation, emit a deprecation warning, and preserve explicit legacy configuration paths without overwriting them.

The folders `desktop-wpf/`, `pixel-ui/`, and `tray-app/` are visual prototypes backed by mock data. They are not integrated DB-Notifier clients.

## Safe legacy checks

On Windows PowerShell, use the repository runner. It limits the execution-policy bypass to the child process, requires Pester `3.4.0`, rejects unexpected skips and enforces the legacy coverage floor:

```powershell
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\scripts\run-legacy-tests.ps1
```

Compatibility executable/installer creation remains unavailable in R5. The manifest contract now requires the complete executable and transitive dependency closure, with a version, SHA-256 digest and official provenance for every file; satisfying that contract still requires separate implementation authority before generation can be enabled. Build scripts never install or download tools.

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
