# ADR-0001 — Runtime Stack and Incremental Migration

- Status: accepted
- Date: 2026-07-11
- Owners: DB-Notifier architecture / product owner

## Context

The first setup evidence temporarily targeted .NET 8. The product owner subsequently required .NET 10 LTS from the active project baseline through the end of the project. According to the official [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), .NET 8 support ends on 2026-11-10, while .NET 10 LTS support extends to 2028-11-14. The historical commit is preserved for audit, but it is not an active supported baseline.

The PowerShell compatibility monitor must remain runnable while boundaries and replacement slices are introduced. A big-bang rewrite would remove the only executable behavior before equivalent evidence exists.

## Decision

- Use .NET 10 LTS for every active .NET project, test project, build, CI job, package baseline, and future project added to this repository.
- Pin the workspace SDK in `global.json`; no active project may target .NET 8/9 or multi-target an earlier runtime without a new explicit ADR and product-owner approval.
- Retarget the existing scaffold immediately, regenerate lockfiles, and repeat restore/build/test/format/dependency evidence before further architecture work is closed.
- Use C#/.NET for Domain, Application, provider abstractions, providers, infrastructure, Agent, API, and WPF Desktop.
- Use ASP.NET Core for the HTTP API and SignalR only for real-time hints.
- Use React + TypeScript for the Web Dashboard; exact library versions remain lockfile-managed and replaceable behind API contracts.
- Keep PowerShell/Python UI artifacts as compatibility/reference assets only. No new product behavior is added to them.
- Migrate by vertical slices: characterize legacy behavior, define contracts, implement PostgreSQL provider/Agent slice, transition Desktop, integrate API/Dashboard, then homologate providers individually.

## STATE-05 Desktop runtime refinement

The accepted .NET 10 decision is retained. The WPF Desktop now uses the versioned `net10.0-windows10.0.22621.0` TFM so its Windows-specific notification contracts are compiled against an explicit SDK surface, while `SupportedOSPlatformVersion` remains `10.0.17763.0`. Other active projects remain on their appropriate .NET 10 TFM.

Only the Desktop presentation boundary references `Microsoft.WindowsAppSDK`/Windows App Runtime 2.2. Build-generated Windows App SDK bootstrap initialisation is disabled; the Desktop explicitly attempts dynamic-dependency initialisation and fails safely to the existing local `NotifyIcon` path if the runtime, platform or notification registration is unavailable. This refinement adds no provider integration, external delivery, administrative authority, homologation or public-support claim. Runtime installation and release-machine detection remain deferred to `STATE-08` under ADR-0005.

## Alternatives

- Remain on .NET 8: rejected for product implementation because the remaining support window is too short.
- Rewrite everything before shipping a runnable milestone: rejected because rollback and behavioral comparison would be lost.
- Electron or Python for the production Desktop: rejected for the baseline because WPF integrates with the approved Windows Agent/runtime and avoids a second production runtime.
- Engine-specific services: rejected because they fragment canonical semantics and operations.

## Consequences

- Git history retains the earlier setup evidence, but the working baseline and all future implementation use only .NET 10 LTS.
- Runtime upgrades after .NET 10 require an explicit ADR, compatibility plan, and full validation; silent framework drift is prohibited.
- Windows remains required for WPF and Windows service adapters; Domain/Application/provider tests should remain cross-platform where possible.
- Windows App Runtime 2.2 is a Desktop deployment dependency rather than a Domain, Application, Agent, API or provider dependency. Development and release evidence must distinguish successful bootstrap/API acceptance from visible Windows delivery and supported production operation.
- The PostgreSQL compatibility app stays available until replacement criteria and rollback tests pass.

## Security and operations

- Runtime versions are pinned, dependency-scanned, and upgraded within vendor support.
- CI must test Windows for WPF and PowerShell plus a non-Windows job for cross-platform assemblies when introduced.
- No runtime upgrade may silently change config, protocol, secret storage, or service identity.

## Compliance checks

- `global.json` selects a .NET 10 LTS SDK and every active target framework is `net10.0` or its appropriate versioned Windows equivalent; the WPF Desktop is specifically `net10.0-windows10.0.22621.0`.
- Lockfiles, analyzer/build/test/format evidence, and dependency audits are regenerated under .NET 10.
- The Desktop lockfile pins Microsoft Windows App SDK 2.2, automatic bootstrap initialisation remains disabled, and architecture/source guards require an explicit fail-safe initialisation path that preserves notification-area startup.
- No legacy entry point is removed as a side effect.
