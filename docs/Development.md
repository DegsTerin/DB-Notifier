# Development Bootstrap

## Supported setup baseline

- Git with the default branch `main`.
- .NET 10 LTS SDK `10.0.302` selected without roll-forward through `global.json` and installed locally in the ignored `.dotnet/` directory for this workspace.
- Windows is required to run the WPF Desktop and the renamed DB-Notifier compatibility application. The Desktop uses the versioned `net10.0-windows10.0.22621.0` TFM while retaining Windows 10 version 1809 (`10.0.17763.0`) as its declared minimum platform.
- Node.js `24.18.0` with npm `11.16.0` is used for the Dashboard scaffold; both are declared in `package.json`, and dependencies are locked in `package-lock.json`.
- PowerShell `7.0` or later (`pwsh`) is required by modern browser and consolidated STATE-06 sandbox runners.
- Windows PowerShell 5.1 with Pester `3.4.0` is used for legacy characterisation.

Installing an SDK or dependency changes the development environment and is not performed automatically by this repository.

## Canonical development workflow

All ordinary preparation and validation begins at the checked-in entry point:

```powershell
./scripts/development.ps1 Doctor
./scripts/development.ps1 Setup
./scripts/development.ps1 Quick
./scripts/development.ps1 Full
```

The entry point runs its executable task in an isolated PowerShell child,
removes AI provider credentials from that child without reading them, and
applies the mandatory DB-Notifier shutdown assertion before technical work.
It does not stop an ambiguous process, install a toolchain, control a database
or broaden the current lifecycle authority.

The tasks have distinct contracts:

- `Doctor` is read-only. It checks the exact worktree, pinned PowerShell, Git,
  .NET, Node.js and npm versions, tracked lockfiles and restored dependency
  readiness, and returns actionable failures.
- `Setup` performs only locked .NET and npm restores. It does not install an
  SDK or mutate a lockfile.
- `Quick` provides bounded local feedback for formatting, compilation, unit
  and architecture tests, Dashboard static/tests and repository policy. Its
  first disposition is `NON_GATE`; it deliberately excludes integration,
  coverage, runtime matrices, online advisory audits and Human Gates.
- `Full` delegates exactly once to `scripts/ci.ps1`, the canonical aggregate
  repository gate used by CI. It preserves the existing .NET/WPF, legacy,
  Dashboard, browser, STATE-06 sandbox, coverage, dependency, documentation,
  secret and Git-integrity responsibilities.

Every executable task crosses a shell-free child-process boundary. Before the
child starts, the runner removes inherited DB-Notifier activation markers,
connection/configuration overrides, sandbox build flags, ASP.NET hosting
overrides and AI-provider credentials from the child's private environment
without reading their values. Direct `scripts/ci.ps1` invocation applies the
same boundary. The calling shell remains unchanged, and a physical campaign
must still use its separately authorised owning runner.

Append `-PlanOnly` to any task to print its versioned, ordered plan, or run the
same command twice to compare it in automation, without process inventory, child creation,
restore, build, test, runtime or network access:

```powershell
./scripts/development.ps1 Quick -PlanOnly
./scripts/development.ps1 Full -Offline -PlanOnly
```

Only `Setup` and `Full` accept `-Offline`. Offline locked restore uses
`scripts/NuGet.Offline.config`, which clears package sources, and npm uses its
local cache. Both isolated entry points suppress .NET workload update
notifications in this mode. The full runner emits `NOT_RUN` for online NuGet
and npm advisory freshness and finishes with a `PARTIAL` disposition even when
every runnable check passes.

After the mandatory shutdown assertion, the canonical gate scans the current
non-ignored worktree and available Git history for high-confidence secret
signatures before it executes repository policy or product checks. Secret
values are never printed. Git diff and object-integrity checks still close the
ordered gate after executable validation.

Broad and cross-cutting increments maintain [`../PLANS.md`](../PLANS.md) as a
live execution ledger containing the frozen baseline, authority, positive and
negative scope, protected work, ownership, findings, incremental progress,
evidence, blockers and outcome. The plan never grants authority and does not
replace current factual state, append-only history, an ADR, a Quality Gate or
a Human Gate.

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
  DBNotifier.Desktop.Wpf.Tests/
  DBNotifier.Architecture.Tests/
  DBNotifier.IntegrationTests/
  DBNotifier.ServerConcurrency.SandboxHost/
  DBNotifier.AgentFleet.SandboxHost/
  DBNotifier.DashboardTv.BrowserSandboxHost/
  DBNotifier.State06.ConsolidatedSandboxHost/
  DBNotifier.Legacy.Tests.ps1
```

The projects created in `STATE-01` began as infrastructure-only bootstrap code and assembly markers. `STATE-03` added isolated persistence assemblies so the Agent carries SQLite without PostgreSQL and the Server carries PostgreSQL without SQLite. Eight `STATE-04` increments add provider-neutral health contracts, an open provider registry, bounded probes, a hosted scheduler, PostgreSQL discovery/readiness/authenticated adapters, controlled SQLite assignments/outbox, read-only platform vault adapters, idempotent synchronization, canonical events/alert deliveries, certificate-authorized Agent ingestion, OIDC/JWT human RBAC/audit, command delivery/ack without execution, bounded retention, durable delivery runners, authorized audit queries, signed/hash-verified provider-package discovery without loading code, and an isolated legacy configuration migrator without adding UI.

Agent monitoring remains `false` in `src/DBNotifier.Agent.Worker/appsettings.json`. Enabling it requires an explicit non-empty Agent ID and may initialize/migrate the configured absolute SQLite path. Credential references use `windows-credential-manager` or `linux-secret-service`; no plaintext file/environment vault fallback exists.

Local SQLite runtime files belong outside the worktree. Exact `.db`, `.sqlite`
and `.sqlite3` names and their journal/WAL/SHM sidecars are ignored as a
last-resort hygiene boundary; a deliberate versioned fixture requires an
explicit narrow exception.

Agent synchronization remains disabled by default. Observation synchronization may be configured only with the same Agent ID, an absolute HTTPS server address, bounded cadence/batch size, an exact Agent/provider-version inventory, and a valid client certificate with private key in the current identity's personal certificate store. Command polling has an independent `CommandPollingEnabled` switch but currently rejects startup with `command.polling_durable_protocol_unavailable`; durable request identifiers, sequences and acknowledgement replay must be implemented before this path becomes enableable. The Server still protects the receipt-only command endpoints through certificate enrolment and exact Agent route authorisation, but no Start/Stop/Restart executor or command attempt exists.

Human API endpoints use the separate `HumanBearer` JWT scheme. Configure an absolute HTTPS OIDC authority and audience through `HumanAuthentication`; absent or invalid configuration fails closed. The JWT `sub` must map to an active platform user with a non-expired role assignment and the exact permission/scope. No password, signing key, token, or bootstrap administrator is stored in repository configuration.

Every outbound connection in the normal Agent, provider and Server composition
is also subject to the
[network egress policy](architecture/Network-Egress-Policy.md). Configuration
is compiled once from `DBNotifier:NetworkEgress:Policies`; each enabled
consumer requires its own positive CIDR and exact-port policy. Monitoring and
synchronisation remain disabled with an empty policy set. Enabling them
requires `provider-monitoring` and `agent-synchronization`, respectively.
Configuring OIDC requires `human-identity`, while configured central
PostgreSQL requires `server-database`. Isolated loopback sandboxes and dormant
transports absent from normal composition retain their existing activation
barriers; any future normal composition must adopt this policy before it can
receive runtime authority.

The shared HTTP boundary disables redirects, proxies, cookies, ambient
credentials and automatic decompression. Each new physical socket resolves
again, validates every DNS answer and connects to an approved IP while
retaining the original hostname for TLS identity. The Server accepts only one
central PostgreSQL TCP host with `SSL Mode=VerifyFull`,
`Check Certificate Revocation=true` and unsafe diagnostics/trust bypasses
disabled. The connection is pinned when its process-local data source is first
created; DNS rotation requires a controlled restart.

TLS chain construction uses offline cached revocation evidence with
certificate downloads disabled. A missing local trust or revocation proof
therefore fails closed; the repository does not fetch or provision that
material. This local behaviour is security containment, not evidence that an
IdP, PKI or PostgreSQL deployment is operational.

After endpoint routing, the API selects certificate authentication only for Agent policies and `HumanBearer` only for human policies before applying identity-aware quotas. Public or unrecognised endpoints use a no-op authentication scheme, so an invalid bearer header on `/health/live` cannot trigger token validation or authentication-audit persistence. `/health/live` reports process liveness only. `/health/ready` applies a five-second bound and returns only sanitised status/reason fields after checking the non-secret shape of central configuration, central PostgreSQL connectivity and exact compiled/applied migration compatibility; it never reports a connection string or provider exception.

Agent retention and Server retention are separately opt-in, with dry-run defaults. Server outbox and notification delivery remain disabled and now reject startup with `server.delivery_durable_lease_unavailable`; no external publisher, channel adapter or unsafe multi-worker delivery path is enableable until transactional claim/lease and idempotency are implemented. Raw server observations are preserved until an aggregate-before-delete contract exists.

## Windows Desktop runtime and local notifications

`DBNotifier.Desktop.Wpf` references the centrally pinned `Microsoft.WindowsAppSDK` package at version `2.2.0`. Its modern local app-notification path therefore requires a compatible Windows App Runtime 2.2 installation for the current process architecture. The repository does not install that machine dependency during restore, build or ordinary startup; installation and target-machine detection belong to the signed `STATE-08` package.

The Desktop sets `WindowsAppSdkBootstrapInitialize` to `false` and owns dynamic-dependency initialisation explicitly. Its notification publisher calls the Windows App SDK bootstrap API before registering with `AppNotificationManager`, and it releases that bootstrap lease during disposal. A missing or damaged runtime, unsupported platform, missing identity asset, or registration/publication failure is handled fail-safe: normal notification-area startup remains available and an explicit Close request can use the bounded legacy `NotifyIcon` balloon fallback. Diagnostics record only a stable stage and exception type, never notification content, local paths or external state.

During `STATE-05`, `AppNotificationManager` is used only for the local confirmation requested each time the user explicitly closes the secondary WPF shell to the notification area. Platform acceptance of that request is not proof that Windows displayed it, because Focus Assist, user policy and the Windows Shell remain authoritative. App-notification delivery from an elevated process is not treated as supported; the Desktop runs in the ordinary user session. Any observed presentation is also not evidence of provider integration, external delivery, administrative capability, homologation, public support or production readiness.

The separately gated reconciled-notification sandbox uses ledger schema `reconciled-local-notification-ledger.v2`. It persists every validated page item as `Queued` or `Suppressed` before the first Windows hand-off, assigns a monotonic local queue sequence, records `Attempting` before the non-transactional call, and records `Accepted` only after the modern publisher or direct `NotifyIcon.ShowBalloonTip` boundary returns successfully. A busy legacy boundary remains `Retryable`; it is never accepted merely because work entered the ordinary volatile Tray queue. Restart resumes `Queued` work in ledger order, while an interrupted `Attempting` hand-off becomes terminal `Rejected` uncertainty to avoid an unprovable duplicate. The bounded two-attempt policy, five-second backoff/deadline, 256-entry ledger, exact sandbox opt-in and silent baseline remain test-only controls, not operational sizing or proof that the Windows Shell displayed a notification.

## Legacy configuration migration

The .NET 10 configuration migrator is dry-run by default and requires absolute source/target paths:

```powershell
& $dotnet run --project .\src\DBNotifier.ConfigMigrator -c Release -- `
  migrate --source "C:\path\to\PgNotifier\appsettings.json" `
  --target "C:\path\to\DB-Notifier\appsettings.json"
```

Add `--apply` only after reviewing the sanitised report. Apply captures the source and previous target, writes authenticated backups and pending artefacts, then durably creates a recovery journal before replacing the target. A later start deterministically restores the authenticated old state or completes the authenticated new state after interruption. Exact reruns return `AlreadyCurrent`; rollback revalidates the target, backups, manifest, report and journal immediately before mutation and refuses any changed identity or hash:

```powershell
& $dotnet run --project .\src\DBNotifier.ConfigMigrator -c Release -- `
  rollback "C:\path\to\DB-Notifier\appsettings.json.migration-manifest.json"
```

Secret-shaped or unsupported fields reject the entire migration and produce no target or backup.

## .NET checks

These direct commands are component diagnostics for focused investigation;
they do not replace `./scripts/development.ps1 Full`. From Windows PowerShell,
use the workspace-local SDK:

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

The unit-test coverage gate requires at least 70% line coverage and 45% branch coverage. An 80% line-coverage level is a risk-based directional target, not a replacement automatic gate; existing component floors must not be reduced without explicit authority, evidence and a governance record. The separate legacy Pester gate requires at least 25% command coverage. These are regression floors for the current bounded suites, not claims of complete behavioural coverage.

## Script syntax checks

The repository gate discovers tracked and unignored PowerShell and Node scripts
dynamically. It parses PowerShell without executing it and uses the pinned Node
runtime only for JavaScript syntax:

```powershell
pwsh -NoProfile -NonInteractive -File .\scripts\verify-script-syntax.ps1
pwsh -NoProfile -NonInteractive -File .\tests\DBNotifier.ScriptSyntax.Tests.ps1
pwsh -NoProfile -NonInteractive -File .\tests\DBNotifier.RunnerProcess.Tests.ps1
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass `
  -File .\scripts\verify-script-syntax.ps1 -PowerShellOnly -LegacyCompatibleOnly
```

The Windows PowerShell command covers only scripts compatible with version
5.1; scripts declaring PowerShell 7 remain in the primary gate. Syntax
validation does not replace tests, type checking, linting or runtime
validation.

## R4-B disposable PostgreSQL ownership laboratory

The R4-B concurrency proof requires PowerShell 7, Docker Desktop and the exact PostgreSQL image already present
locally. The runner validates image ID/digest, uses `--pull never`, publishes PostgreSQL only on an ephemeral
`127.0.0.1` port, generates a temporary synthetic password and removes only its exact-labelled container, network,
volume and temporary directory:

```powershell
pwsh -NoProfile -NonInteractive -File .\scripts\run-r4b-postgresql-delivery-lab.ps1
```

Do not pull the image to satisfy this gate. If the pinned image is absent, Docker is unavailable or cleanup cannot
be proved, stop and report the boundary. A passing run proves only claim/lease/fence/reclaim, idempotency,
dead-letter and ambiguous-side-effect behaviour for that disposable PostgreSQL 16 Alpine topology. It does not
activate normal delivery, apply an operational migration or homologate PostgreSQL generally. See the
[R4-B evidence report](STATE-06-Audit-Remediation-R4B-Report.md).

## R-EGRESS/R-FENCE disposable PostgreSQL load campaign

The multiprocess load runner requires PowerShell 7, the workspace-local .NET
SDK, Docker Desktop and the exact pinned PostgreSQL image already present. It
uses only GUID-labelled temporary resources and never pulls an image:

```powershell
pwsh -NoProfile -NonInteractive -File .\scripts\run-r-egress-postgresql-fence-lab.ps1
```

A passing run proves only the scoped Active/revocation/commit ordering and
identity-fence regression in the controlled local topology. It does not apply
an operational migration or grant fairness, SLO, homologation or provider
support. See the
[campaign report](STATE-06-R-EGRESS-R-FENCE-PostgreSql-Multiprocess-Load-Report.md).

## R-NET local DNS, PKI, IdP and PostgreSQL TLS campaign

The conditional R-NET campaign requires PowerShell 7, the workspace-local
.NET SDK, Docker Desktop and the exact pinned PostgreSQL image already
available locally. It creates synthetic certificates and credentials under a
restricted ACL, uses a process-local custom trust seam, registers only the
exact temporary CRL in the current user's CA cache, runs PostgreSQL cells
sequentially and removes every owned resource:

```powershell
pwsh -NoProfile -NonInteractive -File .\scripts\run-r-net-local-homologation.ps1
```

Do not pull an image, install a root certificate or accept a protected Windows
trust prompt to satisfy this gate. The runner takes an exclusive lock, removes
default routes from every cell, proves a loopback-only effective listener and
recovers only exact signed CRL artefacts and labelled Docker residue left by a
previous interrupted run. The effective resolver file must contain only the
runner's loopback destination, external routes must be absent and PostgreSQL
must retain zero effective/permitted/inheritable/ambient capability with
`NoNewPrivs=1`. If the image, Docker, route/DNS/listener/capability boundary,
exact CRL registration/recovery or cleanup cannot be proved, stop and report
the boundary. A passing run proves only the controlled local matrix in the
[R-NET homologation report](STATE-06-R-NET-Local-DNS-PKI-IdP-PostgreSql-TLS-Homologation-Report.md);
it does not change production system trust, configure an operational IdP,
apply a migration or homologate PostgreSQL/provider support generally.

## Legacy checks

```powershell
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File .\scripts\run-legacy-tests.ps1
```

These tests characterize the only functional legacy provider behavior under its new canonical DB-Notifier paths. Deprecated PgNotifier shims are covered separately and must not be confused with new provider support.

## Dashboard bootstrap

`src/DBNotifier.Dashboard.Web` contains the deterministic React demonstration views and Design System theme runtime. The normal build has no Agent, API, database, IdP or administrative integration. One restricted `STATE-06` adapter exists only for the exact `local-test` flag on an HTTPS loopback origin and reads the test-authenticated Dashboard TV sandbox endpoint; it does not admit an external endpoint or operational identity. The ordinary sandbox registration still supplies the fixed in-memory fixture. Only the integration-test host replaces that source with a read-only projection of synthetic observations received through Agent SQLite/outbox and HTTPS/mTLS; neither this projection nor its synthetic provider is registered by the normal API or Agent Worker. The canonical gate and supplemental Linux Dashboard stage use the committed lockfile and run these responsibilities through `scripts/ci.ps1`:

```powershell
npm run toolchain:verify
npm ci --ignore-scripts --no-audit --no-fund
npm run brand:verify
npm run provider-icons:verify
npm run tokens:verify
npm run localisation:verify
npm run check
npm run comments:verify
npm run markdown:verify
npm test
npm run build
npm audit --audit-level=high
```

The O4 Observer browser audit uses the same owned-process boundary, an exact
HTTPS loopback host and a dedicated Chrome profile:

```powershell
pwsh -NoProfile -NonInteractive -File .\scripts\run-o4-observer-browser-audit.ps1 `
  -DotNetPath .\.dotnet\dotnet.exe
```

The separately authorised Dashboard TV browser composition gate uses only an already installed Chrome or Edge and never downloads a browser or package:

```powershell
pwsh -NoProfile -NonInteractive -File .\scripts\run-state06-dashboard-tv-browser-e2e.ps1 `
  -BrowserProduct Chrome -DotNetPath .\.dotnet\dotnet.exe
```

It builds the exact local-test Dashboard, starts one temporary HTTPS loopback host, creates an isolated browser profile, observes the 30-second cadence plus the deterministic recovery matrix, rejects external HTTP origins and removes every owned process/profile at the end. This is local sandbox evidence, not browser homologation or an operational runtime command.

All four browser runners declare `#Requires -Version 7.0`, resolve an explicit
executable path, preserve every child-process argument boundary and capture
stdout/stderr incrementally. Their `-DotNetPath` default is
`.dotnet\dotnet.exe`; CI passes `dotnet` explicitly after `setup-dotnet`.
Automated Node/CDP audits have a 600-second default global deadline; the
visible presenter uses 900 seconds so its internal 12-minute review budget can
finish before bounded cleanup.
Windows PowerShell 5.1 rejects these runners before their parameter or
process-creation body runs; it remains supported only for the legacy
compatibility and syntax gates. The visible final-human-review runner remains
reserved for an explicitly authorised human sample and is never invoked by
the automatic syntax or process-helper tests.

The separately authorised consolidated remediation harness correlates the already accepted STATE-06 sandboxes in one test-only run:

```powershell
pwsh -NoProfile -NonInteractive -File .\scripts\run-state06-consolidated-e2e.ps1 `
  -BrowserProduct Chrome -DotNetPath .\.dotnet\dotnet.exe
```

The runner builds only from already restored dependencies, creates one file-backed Agent SQLite root, one in-memory Server SQLite database, separate ephemeral Agent and human test identities, and a dedicated browser profile. It proves Agent replay, authoritative Dashboard reads, authenticated SignalR hints, one in-memory notification delivery, non-executable command journalling, granular central revocation, direct Server denial, local quarantine and cleanup. Its failure response and browser auditor expose only an allow-listed typed diagnostic. The companion integration matrix executes `R1`–`R7` with explicit barriers, controlled time, fencing and cancellation. The normal Agent Worker and Server API do not compose this host. The command path cannot create `CommandAttempt`, and this runner is remediation evidence rather than a rerun or approval of the Consolidated Quality Gate campaign.

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

`.github/workflows/ci.yml` delegates the canonical Windows evidence sequence
exactly once to `scripts/ci.ps1 -Stage All`. A supplemental Linux job calls the
same entry point with `-Stage Dashboard` to retain cross-platform Dashboard
compatibility evidence without defining a second aggregate gate. The canonical
sequence covers the .NET solution and coverage floor, legacy compatibility and
coverage floor, Dashboard static/build/dependency gates, the isolated browser
matrix, the bounded STATE-06 sandbox and history-aware secret scanning.
Checkouts do not persist credentials, Node uses `.nvmrc`, the full checkout is
available to the history scan, and concurrent runs for the same ref cancel
older work.

The R5 provenance check queried only `refs/tags/v4` from the official `github.com/actions/*` repositories with `git ls-remote --refs`, without credentials, clone or artefact download. The workflow pins the observed full commits for checkout, setup-dotnet, setup-node and upload-artifact. This local provenance check does not claim that remote CI ran or attest future upstream movement.

## Audit and packaging tool boundaries

The Dashboard CDP audit requires an explicit browser product and records its real CDP product/version. It does not silently label Edge as Chrome. The Lighthouse runner requires an already provisioned `lighthouse.cmd` at exactly `13.4.0`; it never calls `npx` to download code and enforces per-run performance, accessibility and best-practices thresholds.

The WPF automation runner now fails on incorrect window bounds, unnamed or insufficient focus targets, off-screen Tab traversal, broken preference cycles, unsupported DPI awareness and missing screenshot evidence. It remains a bounded shell sample: eight-destination content correctness, live-region speech, Narrator usability, modal/flyout experience, real High Contrast and perceptual quality remain explicit Human Gate samples rather than claims inferred from UI Automation.

`build/build.ps1 -ValidateOnly` remains dependency-free. Executable/installer packaging is unconditionally unavailable in R5; the versioned toolchain contract requires complete executable and transitive dependency closure, exact versions, SHA-256 hashes, roles and official repository/commit provenance before any later generation authority can even be considered. PowerShell/WPF, Pixel UI and Python Tray prototype executable builders remain non-distributable and fail closed.

## Dependency maintenance status

The 2026-07-16 offline maintenance check found no advisory in the locally available NuGet/npm metadata; this is not fresh registry attestation. Compatible patch candidates existed for the Microsoft 10.0 servicing line, Microsoft.NET.Test.Sdk and Vite, but their packages were not present in the local caches. Manifests and generated lockfiles were therefore left unchanged rather than hand-edited or fetched without authority.

xUnit v3 migration is deferred to a separately verified dependency increment. Its exit conditions are: packages available from the constrained NuGet source, official migration compatibility reviewed, all test projects restored with regenerated lockfiles, analyser/source changes complete, the same or stronger test and coverage results observed, and rollback to the current xUnit 2.9.3 baseline documented. Availability of a newer major version alone is not authority to migrate.
