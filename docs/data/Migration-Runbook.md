# Migration and Recovery Runbook

## Scope

These commands generate/validate non-production migrations only. Applying them to production or a monitored database is prohibited without a later authorized release/migration procedure.

## Tooling

```powershell
$dotnet = ".\.dotnet\dotnet.exe"
& $dotnet tool restore
& $dotnet restore .\DBNotifier.sln --locked-mode
```

The repository pins .NET 10 and `dotnet-ef` in `global.json` and `.config/dotnet-tools.json`.

## Add an Agent SQLite migration

```powershell
& $dotnet tool run dotnet-ef migrations add <MigrationName> `
  --context DBNotifier.Persistence.Agent.Sqlite.AgentDbContext `
  --project .\src\DBNotifier.Persistence.Agent.Sqlite\DBNotifier.Persistence.Agent.Sqlite.csproj `
  --startup-project .\src\DBNotifier.Persistence.Agent.Sqlite\DBNotifier.Persistence.Agent.Sqlite.csproj `
  --output-dir Migrations
```

## Add a Server PostgreSQL migration

```powershell
& $dotnet tool run dotnet-ef migrations add <MigrationName> `
  --context DBNotifier.Persistence.Server.PostgreSql.ServerDbContext `
  --project .\src\DBNotifier.Persistence.Server.PostgreSql\DBNotifier.Persistence.Server.PostgreSql.csproj `
  --startup-project .\src\DBNotifier.Persistence.Server.PostgreSql\DBNotifier.Persistence.Server.PostgreSql.csproj `
  --output-dir Migrations
```

Design-time factories select providers without storing connection strings. Migration generation does not contact a database.

## Review checklist

1. Build with warnings-as-errors and inspect generated Up/Down operations.
2. Confirm correct context/provider and no cross-store table.
3. Confirm IDs, UTC times, required fields, max lengths, FKs/delete behavior, unique/idempotency constraints, status checks, and indexes.
4. Search for password, secret value, connection string, private key, token value, or provider-native sensitive content.
5. Review destructive operations explicitly; a generated drop/rename is never accepted blindly.
6. For custom SQL, verify provider syntax, transactional behavior, idempotent cleanup in Down, and least privilege.
7. Generate scripts for initial→latest, previous→latest, latest→previous, and latest→zero where supported.
8. Run SQLite migration tests against an ephemeral database and PostgreSQL script/model tests without a real target.
9. Run locked restore, build, tests, format, and dependency vulnerability audit.

## Current migration chain

- Agent SQLite: `InitialAgentSchema` → `AddAgentStateConstraints`.
- Server PostgreSQL: `InitialServerSchema` → `AddServerStateConstraints`.

The server initial migration adds an append-only trigger for `audit_entries`; Down removes the trigger/function before dropping tables.

## Rollback rules

- Rollback target must be compatible with application/protocol and data written since upgrade.
- Back up the internal store and migration history before any non-ephemeral rollback.
- Never use rollback to alter a monitored database.
- Prefer forward-fix when Down would lose accepted data; record the decision and new migration.
- SQLite rollback tests use an in-memory ephemeral database and validate latest→previous→zero.
- PostgreSQL Down scripts are reviewed/generated offline in `STATE-03`; execution requires a later disposable PostgreSQL sandbox authorization.
