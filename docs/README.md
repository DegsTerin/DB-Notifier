# DB-Notifier Documentation

Current discovery and migration artifacts:

- [`Legacy-Inventory.md`](Legacy-Inventory.md): verified PgNotifier behavior, limitations, and capability truth.
- [`Legacy-Migration-Plan.md`](Legacy-Migration-Plan.md): incremental PgNotifier → DB-Notifier milestones, compatibility, verification, and rollback.
- [`Legacy-Compatibility.md`](Legacy-Compatibility.md): canonical names, deprecated shims, configuration preservation, and removal gate.
- [`STATE-00-Discovery-Report.md`](STATE-00-Discovery-Report.md): checks, findings, limitations, and pending Human Gate.
- [`Development.md`](Development.md): `STATE-01` scaffold, tool baseline, checks, and onboarding.
- [`STATE-01-Setup-Report.md`](STATE-01-Setup-Report.md): approved setup evidence, dependency remediation, compatibility migration, and Human Gate.
- [`architecture/README.md`](architecture/README.md): accepted `STATE-02` architecture pack and decision index.
- [`STATE-02-Architecture-Report.md`](STATE-02-Architecture-Report.md): architecture audit, material decisions, walkthrough, and accepted Human Gate.
- [`data/README.md`](data/README.md): `STATE-03` logical model, retention, migrations, and recovery guidance.
- [`STATE-03-Database-Modeling-Report.md`](STATE-03-Database-Modeling-Report.md): approved automatic/Human Gate evidence, migration verification, and accepted limits.

The governing instruction corpus starts at [`../prompts/Start-Here.md`](../prompts/Start-Here.md).

Avoid committing generated installers, executables, logs, secrets, or temporary build output.
