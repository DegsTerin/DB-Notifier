# DB-Notifier

DB-Notifier is the independent successor to PgNotifier. Under the owner's corrected intent in `GOV-MN-RESTORE-01`, MySQL Notifier 1.1.8 may inform sanitised, observable and non-expressive functional outcomes for improving DB-Notifier. It is not an implementation base or clone mandate. Source-exposed analysis is separated from implementation and test authorship; resulting code, tests and assets are independently authored and provider-neutral. Oracle/MySQL source expression, binaries, branding, product copy and vendor-specific internal architecture are not imported into the MIT project. DB-Notifier remains an open multi-provider platform designed to accept database engines through independently governed versioned providers/plugins, built around an Agent, Desktop/Tray client, central API, and Web Dashboard.

Agents are designed to monitor local, remote, datacenter, hybrid, and cloud databases from Windows, Linux, containers, or cloud workloads. Monitoring credentials, database administration credentials, OS service identities, and cloud control-plane identities remain separate and vault-backed.

The workspace is technically positioned in `STATE-06 INTEGRATION`. Its normal composition remains fail-closed: monitoring and synchronisation are disabled by default, ordinary user interfaces show identified local demonstration data, and authorised integration compositions remain opt-in and test-only. `ActivationState=None`; no provider is publicly supported or operationally homologated, and no administrative execution, external runtime, deployment or lifecycle transition is enabled. The authoritative present-tense snapshot is [`prompts/state/Current-State.md`](prompts/state/Current-State.md); historical implementation and gate evidence remains in the owning reports.

Server-side evidence is recorded in the [`STATE-06 Agent Identity and Fleet Integration Report`](docs/STATE-06-Agent-Identity-And-Fleet-Integration-Report.md). Agent-side and resilience evidence remain in their separate reports. Dashboard TV evidence is split between the [`authoritative snapshot and periodic reconciliation report`](docs/STATE-06-Dashboard-TV-Authoritative-Reconciliation-Report.md), the later [`browser composition and recovery report`](docs/STATE-06-Dashboard-TV-Browser-Composition-And-Recovery-Evidence-Report.md), and the integrated [`authoritative observation pipeline E2E report`](docs/STATE-06-Authoritative-Observation-Pipeline-E2E-Sandbox-Report.md).

## Start here

Repository agents start with [`AGENTS.md`](AGENTS.md), the primary source for permanent and reusable working instructions. All governed work then follows [`prompts/Start-Here.md`](prompts/Start-Here.md), which defines detailed routing, current state, authority, lifecycle, quality gates, and safety limits.

Code and configuration documentation follows [`docs/Code-Documentation-Standards.md`](docs/Code-Documentation-Standards.md): comments use British English, document intent and remain synchronised with implementation. Run `npm run comments:verify` from `src/DBNotifier.Dashboard.Web` to check the project-wide module inventory.

All frontend work follows the official [`DB-Notifier Design System`](docs/design/DB-Notifier-Design-System.md): a restrained modern enterprise identity, shared React/WPF semantic tokens, WCAG 2.2 AA and explicit Light/Dark theme contracts. Retired System values migrate safely to Light, while High Contrast remains an independent accessibility override. The current Design System is `3.4.2`; the independent product-mark asset revision remains `2.6.13`. The Windows client is notification-area-first and uses its full WPF shell only as a secondary drill-down, while the Web Dashboard remains the complete responsive surface. MySQL Notifier may inform functional outcomes, but every retained behaviour remains a DB-Notifier-owned decision implemented through its provider-neutral, accessible and least-privilege architecture. The completed `STATE-05` implementation and its Human Gate remain bounded by their recorded demonstration and environmental limitations. The [`STATE-05 request traceability audit`](docs/STATE-05-Request-Traceability-Audit.md) is the historical cross-reference for requests consolidated through its 2026-07-17 cutoff; later work is recorded in the current state, transition log and owning reports.

The discovery outputs for the transformation are:

- [`docs/Legacy-Inventory.md`](docs/Legacy-Inventory.md): verified behavior, assets, limitations, and risks in PgNotifier.
- [`docs/Legacy-Migration-Plan.md`](docs/Legacy-Migration-Plan.md): incremental PgNotifier migration, compatibility contract, milestones, rollback, gate criteria and the active MySQL Notifier functional-coverage matrix.
- [`docs/STATE-06-MySQL-Notifier-Functional-Reference-Restoration-Report.md`](docs/STATE-06-MySQL-Notifier-Functional-Reference-Restoration-Report.md): corrected authority, safe functional-reference boundary and routing to incremental implementation.
- [`docs/STATE-06-MySQL-Notifier-Authority-Revocation-Report.md`](docs/STATE-06-MySQL-Notifier-Authority-Revocation-Report.md): immutable historical evidence of the earlier literal revocation instruction that the corrected authority prospectively supersedes.
- [`docs/Legacy-Compatibility.md`](docs/Legacy-Compatibility.md): canonical names and explicit deprecated shims.
- [`docs/STATE-00-Discovery-Report.md`](docs/STATE-00-Discovery-Report.md): historical discovery evidence and the original Human Gate record, now retrospectively ratified with explicit scope limits.
- [`docs/Human-Gate-Retrospective-Ratification.md`](docs/Human-Gate-Retrospective-Ratification.md): completed independent retrospective ratification records for `STATE-00` through `STATE-04`.

The project has technically completed discovery, setup, architecture, database modelling, backend implementation and the governed frontend phase. It is now executing only explicitly authorised, restricted `STATE-06` integration increments; completing one increment does not promote the lifecycle or activate a planned capability automatically. See [`prompts/state/Current-State.md`](prompts/state/Current-State.md) for current truth and [`docs/Development.md`](docs/Development.md) for onboarding commands.

## Canonical development flow

[`PLANS.md`](PLANS.md) is the non-authorising live execution ledger for broad
or cross-cutting work. The checked-in development entry point exposes the same
ordered workflow locally and in CI:

```powershell
./scripts/development.ps1 Doctor
./scripts/development.ps1 Setup
./scripts/development.ps1 Quick
./scripts/development.ps1 Full
```

The workflow accepts stable compatible developer toolchains within .NET SDK
`>=10.0.302 <10.1.0`, Node.js `>=24.18.0 <25.0.0` and npm
`>=11.16.0 <12.0.0`. It rejects prereleases and crossings into an unvalidated
release line while allowing ordinary servicing updates inside those bounds.

Use `-PlanOnly` to inspect the exact deterministic sequence without running a
preflight, restore, build, test, runtime or network action. `Quick` is always
`NON_GATE`; only `Full` delegates to the canonical aggregate Quality Gate.
`Setup -Offline` and `Full -Offline` use local caches and report online
advisory freshness checks as `NOT_RUN`, so an offline result is never
equivalent to the complete online gate. Exact prerequisites and operational
limits are documented in [`docs/Development.md`](docs/Development.md).

## Current legacy application

The functional implementation is a Windows tray monitor written in PowerShell 5.1:

```text
src/app/DBNotifier.ps1
src/modules/DBNotifier/
config/appsettings.json
tests/DBNotifier.Legacy.Tests.ps1
packaging/inno/DBNotifier.iss
```

It monitors only loopback PostgreSQL endpoints with `pg_isready.exe` and falls back to a loopback TCP reachability check that is presented only as unproved transport evidence. Remote legacy entries remain parseable for compatibility and migration, but the compatibility runtime refuses them before DNS, process or socket execution; governed remote monitoring belongs to the Agent/provider path. The legacy client contains no Start, Stop, or Restart executor; these administrative capabilities remain `Unsupported` until their governed contract and homologation are complete.

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
