# STATE-06 Desktop Fleet Agent/API Read-Only Integration Report

## Purpose and status

This report records the bounded clean-room `S06-DFR-02` implementation lot. It
connects the Application-owned Tray reconciliation contract to a versioned,
provider-neutral Agent-observation read model exposed through the human Server
API. The lot remains inside `STATE-06 INTEGRATION`; it does not perform or
authorise a lifecycle transition, provider activation or external deployment.

The public behavioural requirements remain those frozen by `S06-DFR-01` from
Oracle's archived *MySQL Notifier Reference Manual*, the official 1.1.8 release
announcement and the archived documentation index. No Oracle/MySQL source,
binary, artwork, logo, trade dress, proprietary wording or internal architecture
was consulted or used. The protected local reference-source directory remains
outside the authorised boundary.

## Functional-coverage matrix

`Adopted` means the provider-neutral behaviour is present. `Safely adapted`
means DB Notifier meets the user goal through its own architecture and identity.
`Rejected` means the reference behaviour conflicts with an explicit DB-Notifier
boundary. `Scheduled` means a separate authority or lifecycle phase remains
necessary.

| Publicly documented reference capability | Classification after `S06-DFR-02` | Current DB Notifier coverage | Boundary or later entry condition |
|---|---|---|---|
| Persistent Windows notification-area presence | Adopted | Existing notification-area-first startup and independent database-and-bell identity remain unchanged. | No vendor artwork, wording or trade dress. |
| One-click display of monitored item status | Safely adapted | The compact flyout consumes the existing reconciled provider-neutral snapshot contract; an authenticated API adapter can now supply it. | Ordinary startup remains the labelled demonstration until Desktop human identity is separately authorised. |
| Aggregate visual status in the taskbar icon | Safely adapted | One accepted or retained frame drives the canonical semantic mark and textual aggregate. | Stale, future or invalid evidence fails closed to Unknown. |
| Manual **Refresh Status** action | Adopted | The flyout action requests the same serial read-only source used by the timer. | It cannot discover, configure or control a service. |
| Configurable monitoring or ping interval | Safely adapted | The ordinary Desktop cadence remains the fixed bounded 30-second reconciliation interval. | Persisted interval preferences require separate authority. |
| Display and management of monitored services or instances | Scheduled | Read-only latest-observation presentation is integrated; configuration mutation is absent. | Requires an authorised inventory/configuration owner, audit and mutation tests. |
| Local and remote instance monitoring | Safely adapted at the read boundary; runtime activation scheduled | The Server projects the latest coherent observations received from assigned Agents, filtered by the human actor's `instances.read` scope; the HTTPS source maps them into the Tray contract. | Real provider monitoring, Desktop token acquisition, egress admission and topology homologation remain separately gated. |
| Start, stop and restart commands | Rejected for this lot | No command route or method is present in the source or endpoint. | Future execution requires exact capability, privilege, confirmation, idempotency, audit and post-action probe. |
| Automatic discovery and name-filter auto-add | Rejected | No service enumeration, name filtering or automatic mutation exists. | Conflicts with provider neutrality, least privilege and explicit inventory ownership. |
| Shortcuts to related management tools | Safely adapted | Existing shortcuts open only DB Notifier destinations. | No vendor tool or executable is launched. |
| Status-change notifications | Scheduled; existing local exception preserved | This lot adds no delivery path and does not widen the bounded local demonstration exception. | Operational delivery needs preference, persistence, deduplication, quiet policy, acknowledgement and audit authority. |
| Automatic launch at Windows sign-in | Scheduled | Not implemented by this lot. | Owned by signed packaging and reversible registration in `STATE-08`. |
| Connection options such as SSL or SSH | Scheduled | No provider connection or tunnel is implemented. | Requires provider SDK, opaque credential references, transport policy and independent homologation. |
| WMI/DCOM remote-service administration and firewall changes | Rejected | The Agent/API read model performs no inbound Agent call, service control, firewall change or tunnel creation. | Conflicts with the outbound Agent boundary and infrastructure-mutation prohibition. |
| Product update, installer and version checks | Scheduled | Not implemented by this lot. | Requires release, packaging, network and update-trust authority. |

## Implemented increment

The Application layer owns the exact `desktop-fleet.v1` wire contract and maps
only bounded, unique, UTC, provider-neutral fields into `inventory.v1`. The
human API endpoint requires the existing `HumanApi` policy, the exact protocol
and message-schema headers, and the existing human rate limit. Its PostgreSQL
store projection:

- resolves the active human subject and non-expired `instances.read` scopes;
- selects only non-archived instances whose current observation belongs to the
  exact assigned Agent;
- joins the latest state to its exact health sample and rejects disagreement in
  instance, Agent, sequence, provider, status or timestamps;
- returns at most 256 deterministically ordered observations; and
- omits endpoints, tags, credential references, error details, commands and
  provider-native diagnostics.

The Infrastructure adapter performs one GET through a caller-owned authenticated
`HttpClient`, requires HTTPS, exact response protocol/schema headers, an exact
JSON media type and a 512 KiB streamed ceiling, and rejects redirects observed
at the response boundary. It maps authentication failures to `Denied`, transient
transport outcomes to `Offline`, protocol/contract refusals to `Incompatible`
and all other HTTP failures to a stable non-secret `Failed` result. The existing
single-flight coordinator remains the owner of validation, last-accepted
retention, freshness ageing and coherent Tray presentation.

WPF now has a narrow constructor seam for an already authorised source. The
ordinary executable does not create such a source and continues to compose the
explicit local demonstration. This preserves factual startup while avoiding an
unsafe token argument, token file, environment token, Agent-certificate reuse or
unapproved external identity flow.

## Identity and activation boundary

The Server endpoint remains a human API. Agent mTLS identity is not accepted as
Desktop identity. The adapter does not acquire, persist, log or provision a
bearer token and does not own OIDC, redirects, proxy policy or egress admission.
An operational Desktop composition therefore still requires separate authority
for a short-lived human identity flow, exact Server origin/network policy and
topology homologation. Until then, API integration is implemented and tested as
an injectable boundary but is not runtime-available in ordinary WPF startup.

## Explicit negative scope

This lot adds no MySQL or other provider, provider discovery, database access,
Agent inbound listener, assignment change, Start/Stop/Restart, notification
delivery, credential provisioning, firewall or tunnel change, external
infrastructure, deployment, publication, Human Gate, activation or lifecycle
transition. It changes no schema, migration, package, lockfile or dependency.

## Validation evidence

The mandatory shutdown preflight passed before implementation and each
executable validation stage with zero matching DB-Notifier processes and zero
owned listeners. The implementation baseline remained
`main@d805e86b313d084d1d4f44c4b7559e699e4c6eda`.

Observed focal evidence before the canonical workflow:

- Desktop Fleet application/store and HTTP-source regressions passed `10/10`;
- the isolated human-authenticated HTTPS endpoint matrix passed `3/3`;
- the WPF/source architecture regressions passed `2/2`; and
- the WPF project built with zero warnings and zero errors.

Four earlier focal compilation or expectation runs failed while correcting
three C# syntax/analyser issues and one over-broad architecture substring
assertion. Those results remain factual and are not relabelled as passes.

The first preflight requested before `Doctor` found one project-owned PowerShell
reader left by an earlier inefficient documentation command. Its exact PID,
executable and command line proved ownership; only that process was terminated,
and the repeated shutdown preflight passed with zero residue before validation
continued. `Doctor` then passed repository-root, toolchain, lockfile and restored
dependency checks.

The final `Quick` development-feedback run passed with a clean Release build,
`543` unit tests, `101` architecture tests and `74` Dashboard tests, plus brand,
provider-asset, token, localisation, TypeScript, documentation, Markdown and
script-policy verification. `Quick` remains `NON_GATE`.

Exactly one canonical `Full` run completed with
`DISPOSITION|PASS|stage=All`. It included locked restore and a zero-warning
Release build; `543` unit, `101` architecture, `171` integration and `10` WPF
tests; `83.39%` line and `56.46%` branch coverage; NuGet and npm vulnerability
checks; fail-closed runtime, legacy PowerShell, bundle and script gates; `74`
Dashboard tests and the production Web build; the `STATE-05` browser audit; and
the `STATE-06` consolidated harness with local test data only. This evidence is
limited to the bounded lot and cannot approve a Human Gate, runtime identity,
provider homologation or lifecycle transition.
