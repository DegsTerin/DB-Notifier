# STATE-05 Complete-Audit Remediation Report

## Authority and lifecycle result

Bruno gave the following explicit authority on 2026-07-16:

> “APROVO a remediação dos lotes 1–5, sem avançar o STATE-05 e sem executar ações externas.”

This authority permits the local remediation of the five agreed lots. It does not approve the `STATE-05` Human Gate, does not authorise a transition to `STATE-06`, and does not authorise deployment, publication, remote migration, external installation, real database or service control, credential use, identity-provider access, network mutation or any other external action.

The workspace remains in `STATE-05 FRONTEND_IMPLEMENTATION`, with lifecycle progression `EM ESPERA`. The complete automatic validation result for the authorised local remediation scope is **APPROVED WITH THE RECORDED LIMITATIONS**. Automatic evidence remains separate from human acceptance.

## Scope by remediation lot

| Lot | Local remediation completed in scope | Boundary retained |
|---|---|---|
| 1 — legacy safety and compatibility | Disabled permissive discovery, removed service-control exposure, normalised fail-closed configuration, rejected malformed configuration, restricted the retained PostgreSQL readiness utility to approved installation roots and prevented TCP-only evidence from becoming database health | No PostgreSQL service, database, utility from an untrusted path or external endpoint was controlled |
| 2 — provider, transport, authentication and payload boundaries | Restricted utility discovery and arguments, enforced strict TLS validation, introduced protected-transport and endpoint-aware authentication boundaries, and strengthened enum, evidence, payload and timestamp validation | No certificate, IdP, credential, remote API or monitored database was used |
| 3 — synchronisation, data, concurrency and durability | Added bounded/fair monitoring concurrency, durable ingestion cursor/state reconciliation, gap/ownership/replay checks, a guarded migration and serial SQLite outbox sequencing; unsafe delivery, command polling and raw-deletion paths now fail closed | The migration was created but not applied to PostgreSQL; durable delivery and command protocols remain future work |
| 4 — factual frontend and Design System | Made freshness authoritative for status/KPIs/filters, mapped stale or invalid evidence to Unknown, shared one immutable WPF demonstration snapshot, hardened routing/timestamps/compact layouts/accessibility and moved the current token contract to Design System `3.0.0` | All UI data remains clearly labelled local demonstration data; brand geometry remains revision `2.6.13`; no API/Agent binding or administrative execution was enabled |
| 5 — toolchain, CI, documentation and packaging | Tightened SDK/Node/OS/tool provenance, timeouts and concurrency; strengthened coverage, documentation, dependency, secret, legacy and browser gates; made unsupported packaging/toolchain paths fail closed; recorded the lifecycle-safe close-out | No dependency or tool was downloaded, no installer was built or published, and no CI, Git host or other external system was changed |

## High findings

| ID | Diagnostic finding | Remediation outcome |
|---|---|---|
| `H-01` | The retained legacy client exposed auto-discovery/service-control behaviour and accepted permissive configuration. | Auto-discovery defaults and service-control entry points were removed or disabled, configuration is normalised fail closed, and malformed JSON is rejected rather than silently accepted. |
| `H-02` | TCP-only reachability, including a changed PID observation, could be promoted to healthy database state. | TCP fallback is presented as transport-only evidence, uses a non-healthy warning treatment and contributes `Unknown` rather than authenticated health. |
| `H-03` | `pg_isready` execution/discovery could accept an untrusted executable path. | Both the retained PowerShell compatibility client and the .NET provider accept only the exact utility name from operating-system-known PostgreSQL installation roots, reject reparse/non-file candidates and ignore mutable Program Files variables plus untrusted configuration/PATH locations as trust roots. |
| `H-04` | Utility arguments or database-name input could alter `pg_isready`/conninfo semantics. | Untrusted extra arguments are removed; database names reject empty, control-character, conninfo and URI forms; provider invocation no longer injects `-d` into the readiness utility. |
| `H-05` | Protected endpoints lacked a complete HTTPS boundary and correct authentication routing. | Protected HTTP requests fail closed. Endpoint metadata selects `HumanBearer` only for the Human API policy and certificate authentication only for Agent policies; public endpoints and unknown metadata use the explicit no-op selector instead of inheriting a human or Agent identity. Explicit schemes remain explicit. |
| `H-06` | Enums, evidence, payloads and timestamps were not fully validated, including future skew. | Trust-boundary validation now rejects undefined enums, invalid evidence combinations, terminal-state violations, malformed payloads and invalid/future/reversed timestamps instead of converting them into factual health. |
| `H-07` | Ingestion, cursors, replay, state and events did not durably and atomically reconcile gaps and Agent ownership. | Per-Agent serialisation and transaction isolation protect reconciliation, and payload hashes make replay explicit. A new observation from a non-assigned Agent is rejected. An accepted sequence above a gap is stored while the cursor remains at the missing sequence; after the gap is filled, a previously accepted sample whose Agent no longer owns the instance advances reconciliation without changing state or creating an event/outbox record. Current state follows only the assigned Agent, and a guarded seventh migration backfills cursor/state from valid contiguous ownership history. |
| `H-08` | The monitoring cycle lacked bounded concurrency, deadline, fairness and assignment limits; concurrent SQLite persistence could race the sequence checkpoint. | Probe work is bounded by concurrency/deadline/fair scheduling and assignment caps. The singleton SQLite sink serialises its sequence transaction, preserving unique monotonic outbox ordering under concurrent probes. |

## Medium findings

| ID | Diagnostic finding | Remediation outcome |
|---|---|---|
| `M-01` | PostgreSQL TLS validation was not restricted to full hostname/chain verification with revocation checks. | The authenticated provider accepts only verify-full semantics and enables certificate revocation checking. |
| `M-02` | Notification delivery/outbox processing had no durable lease protocol. | **Contained, not implemented:** enabling the incomplete delivery worker now causes a stable startup refusal. Durable leasing, retry/reconciliation and external delivery remain future authorised work. |
| `M-03` | Retention could delete raw observations before an independent aggregate/cursor made deletion safe. | **Contained, not implemented:** raw server observations are preserved and unsafe deletion is disabled. Aggregate-aware retention remains future authorised work. |
| `M-04` | Batch, quota and assignment bounds were insufficient. | Agent batches and assignments are capped, assignment sequence retrieval is bounded, monitoring concurrency is bounded, and authentication-audit admission uses one global quota. Server ingestion still processes bounded batch items individually; this is a known performance residual, not evidence of unbounded acceptance. |
| `M-05` | Command polling lacked a durable protocol. | **Contained, not implemented:** enabling command polling now causes a stable startup refusal. Durable polling, acknowledgement, expiry, replay resistance and post-action semantics remain future authorised work. |
| `M-06` | Authentication audit data was not fully sanitised, globally limited and correctly routed for public endpoints. | Audit records are sanitised and globally quota-controlled, repeated saturation warnings are aggregated, and public endpoints use a no-op audit path rather than manufacturing authentication events. |

The containments for `M-02`, `M-03` and `M-05` deliberately remove unsafe runtime claims. They do not implement notification delivery, aggregate-aware retention or durable command polling, and they must not be described as support for those future capabilities.

## Frontend and Design System remediation

- Design System `3.0.0` is the current token/implementation contract. It removes provider-named visual tokens and uses neutral categorical data colours that do not imply provider identity, support, health or homologation.
- The canonical database-and-bell asset remains brand revision `2.6.13`. The WPF native-caption and scrollbar correction remains historical implementation evidence from `2.6.14`; neither number is replaced or rewritten as brand history.
- Dashboard KPIs, filtering and aggregates now derive from effective freshness. Stale, non-finite, future-skewed or reversed evidence fails safely to `Unknown`; Maintenance remains a warning state.
- WPF MainWindow, Tray and flyout consume one immutable local demonstration snapshot. Refresh ages freshness without changing the snapshot generation time, and double-click no longer competes with the single-activation flyout workflow.
- Hash routing is allow-listed and preserves browser back, forward, reload and skip-link behaviour. Compact Dashboard/TV layouts, timestamp ordering, WPF labels, live regions, scroll ownership and the non-executable capability dialogue were hardened without enabling an external action.
- The 2026-07-15 decision `S05-HG-011 APROVADO` remains historical evidence for the exact sample then reviewed. Because this remediation changed shared freshness, Tray/flyout content and activation behaviour, those changed surfaces require a new human sample; the earlier decision is not reused by inference.

## Tooling, CI and packaging remediation

- The repository pins the .NET SDK selection and Node/npm contract, constrains CI operating systems, timeouts and concurrency, and disables checkout credential persistence.
- Documentation, .NET coverage, NuGet-report schema, secret scanning, Pester compatibility coverage, generated assets, Dashboard/WPF audits and fail-closed runtime checks have explicit repository gates.
- Prototype packaging and automatic vendor-icon download paths fail closed. The compatibility-toolchain manifest requires exact version/hash evidence before future use.
- Mutable third-party CI action tags remain a documented supply-chain residual; no unverifiable offline commit SHA was invented. Lighthouse `13.4.0` must already be provisioned for its gate, and the gate does not download it implicitly.
- Dependencies not already available locally were not changed merely to manufacture an audit result; the planned xUnit v3 migration remains deferred.

## Validation and evidence status

| Evidence class | Result |
|---|---|
| Focused regression checks used while implementing lots 1–5 | Observed locally by the owning remediation passes; their integrated confirmation is governed by the row below |
| Integrated format, build, automated tests and coverage | Approved: .NET Release build with zero warnings/errors; `179/179` unit and `13/13` architecture tests; line coverage `78.89%` and branch coverage `56.69%`; `42/42` Dashboard tests, typecheck and Vite production build |
| Legacy compatibility | Approved: `23` Pester tests passed, one explicit conditional PostgreSQL 18 fixture was skipped, zero failed; command coverage `32.08%` (`290/904`); bundle validation passed |
| Generated assets and browser matrix | Approved: brand, token and localisation drift checks passed; Edge `150.0.4078.65` completed `96` locale/theme/route/viewport samples without global overflow or unnamed interactive controls |
| Tooling, dependency, documentation and security gates | Approved within offline evidence: exact Node `24.18.0`/npm `11.16.0`, npm audit cache reported zero advisories, NuGet report/schema and all `13` projects passed against local metadata, `197` comment-capable sources and `184` local Markdown links in `62` files passed, and worktree/history secret scans reported no high-confidence path-level findings while suppressing any matched value |
| Fail-closed runtime and repository integrity | Approved: local liveness returned `200`, protected plaintext requests returned `426`, Agent workers remained disabled without a database, validation processes were stopped, and Git diff/integrity checks passed |
| External database/service/identity/infrastructure validation | Not executed and not authorised |
| Human validation of changed Dashboard/WPF/Tray surfaces | Pending; automatic evidence cannot approve it |
| Lifecycle transition | None; `STATE-05` remains active and progression remains on hold |

## Residual boundaries

- No provider is homologated and no public-support claim changes.
- No real PostgreSQL migration, database connection, certificate, credential, IdP, vault, notification channel, command executor or administrative post-probe was exercised.
- Delivery, command polling and aggregate-aware raw retention remain deliberately unavailable through fail-closed containment.
- Server batch ingestion retains an item-by-item persistence cost inside its bound; authentication audit saturation intentionally suppresses excess records after the global quota while emitting only a bounded aggregate warning.
- Utility provenance now constrains name, location and filesystem shape, but executable signing/hash policy and installation ACL attestation remain future hardening.
- The NuGet gate proved report structure, complete project coverage and locally available metadata; without an external advisory refresh, it is not a fresh registry attestation. The secret scanner is a supplemental high-confidence pattern gate, not proof of absolute secret absence.
- Lighthouse remains conditional on the exact locally provisioned version, and mutable external action tags remain a documented CI supply-chain reservation.

## Close-out decision

This report records completion of the authorised local remediation scope with the observed integrated evidence above. It must not be used as Human Gate approval, external-runtime evidence, provider homologation, release authority or permission to move beyond `STATE-05`.
