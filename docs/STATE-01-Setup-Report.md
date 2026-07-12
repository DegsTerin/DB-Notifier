# STATE-01 Project Setup Report

## Execution

- State/fase: `STATE-01 PROJECT_SETUP`
- Corpus version: `3.1.4`
- Branch: `main`, no initial commit
- Environment: WSL2 orchestration, Windows PowerShell 5.1, Pester 3.4.0, Node.js 24.18.0
- .NET environment: workspace-local SDK `8.0.422`, runtime `8.0.28`
- Date and executor: 2026-07-11, Codex
- Scope: Git initialization, modular solution scaffold, conventions, tests, Dashboard scaffold, safe configuration, and CI definition

## Delivered setup

- `DBNotifier.sln` with ten .NET projects for Domain, Application, provider abstractions, PostgreSQL provider boundary, Infrastructure, Agent, API, WPF Desktop, unit tests, and architecture tests.
- Dependency direction encoded through project references; Domain has no outer-layer reference.
- Infrastructure-only Agent, liveness API, and WPF startup scaffolds without monitoring or product domain rules.
- Central target/configuration in `global.json`, `Directory.Build.props`, `Directory.Packages.props`, and `.editorconfig`.
- Nullable analysis, .NET analyzers, warnings-as-errors, deterministic builds, central exact direct-package versions, and NuGet lockfiles configured.
- React/TypeScript Dashboard build scaffold with an npm lockfile.
- GitHub Actions jobs for .NET, legacy Pester, and Dashboard.
- Development onboarding and secret-handling instructions.
- Legacy behavior preserved through canonical DB-Notifier paths plus explicit deprecated shims.

## Naming migration follow-up

After the validated baseline was committed as `ad8baf6`, legacy product artifacts were migrated to canonical DB-Notifier names without a blind global replacement:

- Canonical PowerShell app/module, test suite, WPF prototype, installer definition, and output names now use `DBNotifier`/`DB-Notifier`.
- Deprecated PgNotifier entry points remain as thin warning/delegation shims only.
- Existing explicitly supplied PgNotifier display names, log paths, and configuration files remain accepted and are never overwritten by a shim.
- The new installer uses a distinct application ID and configuration root for side-by-side rollback.
- `build/build.ps1 -ValidateOnly` proves that the canonical app and module form a syntactically valid bundle without requiring packaging tools.
- Actual EXE/installer packaging remains not executed because `ps2exe` and Inno Setup are not installed.

Follow-up evidence: 10/10 Pester compatibility tests passed; deprecated module export resolved; PowerShell bundle validation passed; .NET build/test remained green; Dashboard check/build/audit remained green.

## Checks and evidence

| Gate | Check | Result | Evidence summary |
|---|---|---|---|
| Git setup | Initialize repository and rename branch | APROVADO | Git worktree exists on `main`, without an initial commit |
| Solution structure | Parse solution/project paths | APROVADO | 10 projects declared; no missing project path |
| Project metadata | Parse JSON, MSBuild XML, and XAML | APROVADO | All checked files parsed successfully |
| .NET locked restore | `dotnet restore DBNotifier.sln --locked-mode` | APROVADO | All 10 projects restored against committed lockfiles |
| .NET Release build | `dotnet build ... --no-restore` | APROVADO | 10 projects; 0 warnings; 0 errors |
| .NET tests | `dotnet test ... --no-build` | APROVADO | 2 passed; 0 failed; 0 skipped |
| .NET format/static analysis | `dotnet format ... --verify-no-changes` | APROVADO | Exit code 0 with no required change |
| NuGet dependency audit | `dotnet list ... package --vulnerable --include-transitive` | APROVADO | No vulnerable package reported in any project |
| API liveness sample | Start Release API and request `/health/live` | APROVADO | Windows request returned `{"status":"Alive"}`; process then stopped |
| Dashboard clean restore | `npm ci --ignore-scripts` | APROVADO | 26 packages installed from lockfile |
| Dashboard static check | `npm run check` | APROVADO | TypeScript completed with exit code 0 |
| Dashboard production build | `npm run build` | APROVADO | Vite 8.1.4 built 13 modules successfully |
| Dashboard dependency audit | `npm audit --audit-level=high` | APROVADO | 0 vulnerabilities after upgrading Vite/plugin |
| Legacy compatibility | Parser plus Pester | APROVADO | Syntax valid; 10/10 tests passed, including old configuration preservation |
| Compatibility bundle | `build/build.ps1 -ValidateOnly` | APROVADO | Canonical app/module bundle parsed successfully |
| Markdown/corpus | Internal links, count, whitespace | APROVADO | 23 Markdown files, 0 broken links, 13 prompt files, 0 trailing whitespace |
| Secret pattern review | Scoped assigned-secret scan | APROVADO COM RESSALVAS | 0 matching assignments; repository history does not yet exist |
| CI execution | Remote workflow | NÃO APLICÁVEL | Workflow defined locally; no remote repository/run authorized |

## Dependency remediation

The first Dashboard audit found two esbuild/Vite advisories, including one high-severity result. Vite `5.4.x` and `@vitejs/plugin-react` `4.x` were replaced with exact versions `8.1.4` and `6.0.3`. A clean reinstall, audit, type check, and build then passed with zero reported vulnerabilities.

## Boundary review

- No provider contract or PostgreSQL probe was implemented; the provider project is an explicitly non-functional boundary marker.
- No Domain entity, use case, database schema, persistence, authentication, RBAC, or product UI was introduced.
- `/health/live` reports only process liveness and does not claim Agent, provider, database, or dependency health.
- No secret, real database action, service control, installer, deploy, or publication was performed.
- The SDK was installed only in the Git-ignored workspace `.dotnet/` directory under explicit authorization.
- PgNotifier names that remain executable are documented deprecated shims; historical documents retain the old name as evidence.

## Automatic audit

- Bootstrap structure: APROVADO.
- Configuration and secret baseline: APROVADO COM RESSALVAS pending history-aware scanning after the initial commit.
- .NET locked restore, build, tests, format, and package audit: APROVADO.
- Dashboard restore/build/check/audit: APROVADO.
- Legacy characterization: APROVADO.
- Remote CI: NÃO APLICÁVEL until publication is separately authorized.
- Overall result: `APROVADO` for automatic `STATE-01` checks, with the remote-CI limitation explicitly retained.

## Human Gate

- Phase: `STATE-01 PROJECT_SETUP`
- Validator and date: PENDENTE
- Clean onboarding sample: PENDENTE
- .NET build/static analysis/tests: automatic evidence approved
- Dashboard and legacy checks: automatic evidence approved
- Decision: `PENDENTE`
- Required decision: approve, approve with reservations, or reject the setup handoff before `STATE-02`

The project remains in `STATE-01`; this report does not authorize `STATE-02 ARCHITECTURE`.
