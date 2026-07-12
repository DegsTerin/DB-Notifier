# DB-Notifier Documentation

Current discovery and migration artifacts:

- [`Legacy-Inventory.md`](Legacy-Inventory.md): verified PgNotifier behavior, limitations, and capability truth.
- [`Legacy-Migration-Plan.md`](Legacy-Migration-Plan.md): incremental PgNotifier → DB-Notifier milestones, compatibility, verification, and rollback.
- [`Legacy-Compatibility.md`](Legacy-Compatibility.md): canonical names, deprecated shims, configuration preservation, and removal gate.
- [`STATE-00-Discovery-Report.md`](STATE-00-Discovery-Report.md): checks, findings, limitations, and pending Human Gate.
- [`Development.md`](Development.md): `STATE-01` scaffold, tool baseline, checks, and onboarding.
- [`STATE-01-Setup-Report.md`](STATE-01-Setup-Report.md): setup evidence, dependency remediation, and the blocked .NET gate.

The governing instruction corpus starts at [`../prompts/Start-Here.md`](../prompts/Start-Here.md).

Avoid committing generated installers, executables, logs, secrets, or temporary build output.
