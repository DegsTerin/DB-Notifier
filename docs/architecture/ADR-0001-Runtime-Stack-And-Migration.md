# ADR-0001 — Runtime Stack and Incremental Migration

- Status: proposed
- Date: 2026-07-11
- Owners: DB-Notifier architecture / product owner

## Context

The approved project scaffold targets .NET 8, WPF, ASP.NET Core, and React/TypeScript. The product must support a long implementation and homologation runway. According to the official [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), .NET 8 support ends on 2026-11-10, while .NET 10 LTS support extends to 2028-11-14. Starting production implementation on a runtime near end of support would create immediate upgrade debt.

The PowerShell compatibility monitor must remain runnable while boundaries and replacement slices are introduced. A big-bang rewrite would remove the only executable behavior before equivalent evidence exists.

## Decision

- Keep the current .NET 8 scaffold reproducible as `STATE-01` evidence.
- Before `STATE-04 BACKEND_IMPLEMENTATION`, retarget new DB-Notifier product projects to .NET 10 LTS, regenerate lockfiles, and repeat build/test/analyzer evidence. This retarget is conditional on Human Gate acceptance of this ADR.
- Use C#/.NET for Domain, Application, provider abstractions, providers, infrastructure, Agent, API, and WPF Desktop.
- Use ASP.NET Core for the HTTP API and SignalR only for real-time hints.
- Use React + TypeScript for the Web Dashboard; exact library versions remain lockfile-managed and replaceable behind API contracts.
- Keep PowerShell/Python UI artifacts as compatibility/reference assets only. No new product behavior is added to them.
- Migrate by vertical slices: characterize legacy behavior, define contracts, implement PostgreSQL provider/Agent slice, transition Desktop, integrate API/Dashboard, then homologate providers individually.

## Alternatives

- Remain on .NET 8: rejected for product implementation because the remaining support window is too short.
- Rewrite everything before shipping a runnable milestone: rejected because rollback and behavioral comparison would be lost.
- Electron or Python for the production Desktop: rejected for the baseline because WPF integrates with the approved Windows Agent/runtime and avoids a second production runtime.
- Engine-specific services: rejected because they fragment canonical semantics and operations.

## Consequences

- The repository will temporarily contain a validated .NET 8 scaffold and a proposed .NET 10 product target. Documentation must state that distinction.
- Retargeting is a controlled setup/architecture action, not a feature implementation.
- Windows remains required for WPF and Windows service adapters; Domain/Application/provider tests should remain cross-platform where possible.
- The PostgreSQL compatibility app stays available until replacement criteria and rollback tests pass.

## Security and operations

- Runtime versions are pinned, dependency-scanned, and upgraded within vendor support.
- CI must test Windows for WPF and PowerShell plus a non-Windows job for cross-platform assemblies when introduced.
- No runtime upgrade may silently change config, protocol, secret storage, or service identity.

## Acceptance checks

- Human Gate explicitly chooses the .NET 10 product target or records a supported alternative.
- Retarget plan includes lockfile regeneration, analyzer/build/test evidence, and rollback to the last scaffold commit.
- No legacy entry point is removed as a side effect.
