# DB-Notifier Documentation

Current discovery and migration artifacts:

- [`Legacy-Inventory.md`](Legacy-Inventory.md): verified PgNotifier behavior, limitations, and capability truth.
- [`Legacy-Migration-Plan.md`](Legacy-Migration-Plan.md): incremental PgNotifier → DB-Notifier milestones, compatibility, verification, and rollback.
- [`Legacy-Compatibility.md`](Legacy-Compatibility.md): canonical names, deprecated shims, configuration preservation, and removal gate.
- [`STATE-00-Discovery-Report.md`](STATE-00-Discovery-Report.md): checks, findings, limitations, and accepted Human Gate.
- [`Development.md`](Development.md): `STATE-01` scaffold, tool baseline, checks, and onboarding.
- [`Code-Documentation-Standards.md`](Code-Documentation-Standards.md): project-wide British English documentation policy, review requirements, automated gate, and narrow format exceptions.
- [`STATE-01-Setup-Report.md`](STATE-01-Setup-Report.md): approved setup evidence, dependency remediation, compatibility migration, and Human Gate.
- [`architecture/README.md`](architecture/README.md): accepted `STATE-02` architecture pack and decision index.
- [`STATE-02-Architecture-Report.md`](STATE-02-Architecture-Report.md): architecture audit, material decisions, walkthrough, and accepted Human Gate.
- [`data/README.md`](data/README.md): `STATE-03` logical model, retention, migrations, and recovery guidance.
- [`STATE-03-Database-Modeling-Report.md`](STATE-03-Database-Modeling-Report.md): approved automatic/Human Gate evidence, migration verification, and accepted limits.
- [`STATE-04-Backend-Implementation-Report.md`](STATE-04-Backend-Implementation-Report.md): completed Domain/Application, open Provider SDK, PostgreSQL backend-slice evidence, and accepted limitations.
- [`STATE-04-Backend-Implementation-Audit.md`](STATE-04-Backend-Implementation-Audit.md): automatic closure-gate evidence, findings, limitations, and remediation required before the Human Gate.
- [`STATE-04-Backend-Implementation-Reaudit.md`](STATE-04-Backend-Implementation-Reaudit.md): approved remediation re-audit, accepted Human Gate, reservations, and transition evidence.
- [`STATE-05-Frontend-Implementation-Report.md`](STATE-05-Frontend-Implementation-Report.md): Dashboard/WPF inventory-status increment, presentation semantics, accessibility evidence, checks, and remaining UI scope.

The governing instruction corpus starts at [`../prompts/Start-Here.md`](../prompts/Start-Here.md).

Avoid committing generated installers, executables, logs, secrets, or temporary build output.
