# STATE-06 Desktop Fleet Read-Only Reconciliation Report

## Purpose and status

This report records the clean-room functional-coverage decision and the bounded
`S06-DFR-01` implementation lot for the Windows notification-area client. The
lot remains inside `STATE-06 INTEGRATION`; it does not perform or authorise a
lifecycle transition.

The comparison uses only public Oracle/MySQL documentation retrieved on
2026-08-28. No Oracle/MySQL source, binary, artwork, logo, trade dress, product
copy or internal architecture was used. The local reference-source directory is
outside the authorised boundary and is not an input to this report or the
implementation.

## Public behavioural sources

- [MySQL Notifier 1.1 manual](https://downloads.mysql.com/docs/mysql-notifier-en.pdf),
  particularly the documented taskbar interaction, status display, menu,
  monitoring, interval and remote-service behaviours.
- [MySQL Notifier 1.1.8 release announcement](https://dev.mysql.com/blog-archive/mysql-notifier-1-1-8/),
  used only for public release-level descriptions of monitoring, Workbench and
  connection-option behaviour.
- [MySQL archived documentation index](https://dev.mysql.com/doc/index-archive.html),
  used to establish the provenance of the archived manual.

The manual's legal notice identifies the referenced software and documentation
as proprietary material. That reinforces the requirements-only clean-room
boundary; it does not grant reuse rights.

## Functional-coverage matrix

`Adopted` means the provider-neutral behaviour is present. `Safely adapted`
means DB Notifier preserves the user goal through its own architecture and
identity. `Rejected` means the reference behaviour conflicts with an explicit
DB-Notifier boundary. `Scheduled` means the behaviour needs its own later
authority and is not claimed by this lot.

| Publicly documented reference capability | Classification | DB Notifier coverage after `S06-DFR-01` | Boundary or later entry condition |
|---|---|---|---|
| Persistent Windows notification-area presence | Adopted | Existing notification-area-first startup and independent database-and-bell identity remain unchanged. | No vendor artwork, wording or trade dress. |
| One-click display of monitored item status | Safely adapted | The compact flyout presents up to four provider-neutral instances and the secondary inventory retains the complete accepted snapshot. | Current source remains an explicit local demonstration adapter. |
| Aggregate visual status in the taskbar icon | Safely adapted | One reconciled frame drives the canonical DB Notifier semantic mark and textual aggregate. | Stale or invalid evidence fails closed to Unknown. |
| Manual **Refresh Status** action | Adopted | A flyout action requests the same serial read-only reconciliation used by the timer. | It cannot discover, configure or control a service. |
| Configurable monitoring or ping interval | Safely adapted | The ordinary desktop interval remains a fixed bounded 30 seconds and restarts only after completion. | Persisted interval preferences require separate authority. |
| Display and management of monitored services or instances | Scheduled | Read-only presentation is covered; add, delete and configuration mutation are absent. | Requires an authorised inventory/configuration owner, persistence contract, audit and tests. |
| Local and remote instance monitoring | Scheduled | The Application contract can accept provider-neutral snapshots; no Agent, API or remote source is connected. | Requires separately authorised transport, identity, credential reference and topology homologation. |
| Start, stop and restart commands | Rejected for this lot | Existing unavailable explanations remain; no command is enabled. | A future capability requires exact provider/topology support, privilege, confirmation, idempotency, audit and post-action probe. |
| Automatic discovery and name-filter auto-add | Rejected | No service enumeration, name filtering or automatic mutation exists. | Conflicts with provider neutrality, least privilege and explicit inventory ownership. |
| Shortcuts to related management tools | Safely adapted | Shortcuts open only DB Notifier Dashboard, operational configuration and local logs. | No vendor tool or executable is launched. |
| Status-change notifications | Scheduled; existing local exception preserved | This lot adds no delivery path and does not widen the bounded local demonstration exception. | Factual integrated delivery needs preference, persistence, deduplication, quiet policy, acknowledgement and audit authority. |
| Automatic launch at Windows sign-in | Scheduled | Not implemented by this lot. | Owned by signed packaging and reversible registration in `STATE-08`. |
| Connection options such as SSL or SSH | Scheduled | No connection transport or provider integration is implemented. | Requires provider SDK, secret-store references, transport policy and independent homologation. |
| WMI/DCOM remote-service administration and firewall changes | Rejected | No WMI, DCOM, firewall, tunnel or infrastructure mutation is introduced. | Conflicts with the DB-Notifier Agent boundary and prohibition on automatic infrastructure mutation. |
| Product update, installer and version checks | Scheduled | Not implemented by this lot. | Requires release, packaging, network and update-trust authority. |

## Implemented increment

The Application layer now owns a versioned `IDesktopFleetSnapshotSource`
contract and a `DesktopFleetReconciliationCoordinator`. The coordinator:

- enforces a non-blocking single-flight acquisition boundary;
- validates the current schema, bounded collection size, unique non-empty
  identifiers, required bounded labels and latency limits;
- copies an accepted item collection before retaining it;
- derives detailed freshness and the aggregate from one clock instant;
- replaces source exceptions with stable non-secret reason codes; and
- retains the last accepted snapshot after offline, denied, incompatible or
  failed reads while allowing its freshness to age.

The WPF composition uses an isolated local demonstration adapter. Initial,
periodic, manual and localisation refreshes enter the same coordinator. The
timer is stopped before acquisition and restarted after completion, while the
coordinator independently rejects overlap. One Dispatcher continuation applies
the accepted or retained frame to the notification icon, tooltip, flyout and
secondary inventory surface. The flyout reports source acceptance or retained
evidence and exposes a read-only refresh action.

## Explicit negative scope

This lot does not add a MySQL provider, provider-specific branching, Agent/API
connectivity, service discovery, service control, database administration,
credentials, persistence, infrastructure mutation, external notifications,
installer behaviour or lifecycle progression. Existing local demonstration and
review modes retain their previous authority boundaries.

## Validation evidence

The mandatory shutdown preflight passed before executable work and each
validation stage, with zero matching DB-Notifier processes and zero owned
listeners. The exact implementation baseline remained
`main@8e1fb1c4cee61b2bb2d1a67012d2a78db072ef47` until the required focal delivery
commit.

Observed focal evidence:

- the reconciliation regression filter passed `5/5` tests;
- the WPF architecture regression passed `1/1`;
- the directly affected legacy architecture regression passed `1/1`;
- the Dashboard presentation regression passed `25/25`; and
- the WPF project built with zero warnings and zero errors.

The final `Quick` development-feedback run passed with a Release build, `533`
unit tests, `100` architecture tests and `74` Dashboard tests, plus localisation,
code-documentation, Markdown-link, TypeScript and script-policy verification.
`Quick` remains `NON_GATE`.

One canonical `Full` run completed with `DISPOSITION|PASS|stage=All`. Its
evidence included the locked restore and clean Release build; `533` unit,
`100` architecture, `168` integration and `10` WPF tests; `83.44%` line and
`56.81%` branch coverage; NuGet and npm vulnerability checks; fail-closed
runtime, legacy PowerShell, bundle and script gates; `74` Dashboard tests and
the production Web build; the `STATE-05` Dashboard browser audit; and the
`STATE-06` consolidated harness with local test data only.

Earlier development-feedback runs found and corrected three stale test
expectations plus one rejected-candidate reason-code mismatch. Those failed
results remain part of the factual work history; they are not relabelled as
passes. The final canonical result is evidence only for this bounded lot and
cannot approve a Human Gate or lifecycle transition.
