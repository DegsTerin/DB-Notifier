# STATE-06 — Audit Remediation R4-A Report

## Disposition

- Increment: `R4-A — Roteamento, readiness e deadlines locais`.
- Findings: `AUD-H06`, `AUD-M06` and `AUD-M08`; containment only for `AUD-H07`.
- Authorised baseline: `5bb43974572290734e66e787f152b903d9a1e1cb`.
- Execution authority: Bruno's explicit local-only authorisation issued on 2026-07-21.
- Local implementation and R4-A-specific verification: completed.
- Focused local commit: `a054ef00fa01751693cf28bd8e1ce68eeb9f288b`.
- Automatic result in the authorised R4-A scope: `APROVADO`.
- Independent human decision: `ACEITO O R4-A COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.`
- Lifecycle: remains `STATE-06 INTEGRATION`; no transition was requested or performed.
- MOD-12: unchanged and inactive; no `OBSERVER`, O1, LLM, recommendation, command or automation was enabled.

This report proves bounded local routing integrity, Server readiness and monitoring deadline remediation. It does not
activate external delivery, implement a publisher, establish distributed delivery ownership or close `AUD-H07`.

## Authorised boundary

The increment permitted only local source, tests, documentation, synthetic processes, ephemeral SQLite, loopback
runtime evidence, one minimal Server EF migration, local PostgreSQL SQL generation and one focused commit. It
prohibited real PostgreSQL or services, Docker, operational migration, external channels, publisher activation,
claim/lease/fence/reclaim, new dependencies, external access, downloads, remote CI, push, deploy, R4-B, R5–R8,
R7-A0/O1, commands, administrative SQL or shell, Start/Stop/Restart, LLM, recommendations, automation, mode
promotion and lifecycle transition.

The mandatory shutdown preflight found no process, listener or visible window attributable to DB-Notifier before
the work. No monitored database, service or ordinary user application was stopped.

## Implemented remediation

### Explicit provider-neutral routing (`AUD-H06`)

- `alert_rule_channel_bindings` records the exact rule, channel and environment. The referenced rule owns the
  explicit instance scope and canonical event-type configuration; malformed scope/configuration fails closed.
- Event reconciliation creates pending work only for enabled bindings whose rule, channel, environment and exact
  instance scope all match. Enabled but unbound channels, cross-environment bindings and other-instance rules do not
  receive work.
- Every new delivery stores its event-and-binding provenance and a deterministic
  `notification:{eventId}:{bindingId}` idempotency key. Unique constraints protect both identities.
- A composite foreign key proves that the channel copied onto a delivery is the channel owned by the referenced
  binding; a mismatched binding/channel pending row is rejected by the ephemeral SQLite model.
- The migration retains every historical `Pending` row whose binding is unprovable as `Quarantined`, with the
  stable reason `notification.binding_unproven`. It does not infer a binding, send or delete a row.
- Downgrade is deliberately fail-closed: the generated PostgreSQL rollback script raises
  `notification.explicit_binding_downgrade_blocked` and contains no delivery update or binding-table drop.

### External-delivery containment (`AUD-H07` remains open)

- Normal composition still registers only `UnavailableServerMessagePublisher` and no notification channel adapter.
- Any request to enable Server outbox or notification delivery is rejected synchronously before Kestrel is
  configured or bound.
- Pending selection defensively requires the exact active binding, enabled channel/rule/instance, environment and
  instance scope, but it does not claim distributed ownership.
- Claim, lease, fence, reclaim, concurrent PostgreSQL proof, ambiguous-side-effect recovery and adapter idempotency
  propagation remain exclusively R4-B. R4-A does not declare `AUD-H07` closed.

### Separate bounded Server readiness (`AUD-M06`)

- `/health/live` reports only process liveness and does not consult configuration or persistence.
- `/health/ready` validates a bounded PostgreSQL connection-string shape with non-empty host/database, requires the
  Npgsql provider, checks central connectivity and requires the applied migration sequence to equal the compiled
  sequence exactly.
- One five-second deadline bounds even a non-cooperative readiness boundary through `WaitAsync`; request
  cancellation remains distinct.
- Responses contain only `Alive`, `Ready`, `NotReady` and stable reason codes. Connection values, provider exception
  text and secrets are never returned; readiness failures use `503` and `Cache-Control: no-store`.

### Monitoring deadline, future skew and process cleanup (`AUD-M08`)

- The global monitoring deadline starts before assignment retrieval and bounds a source that ignores cancellation.
  Source timeout becomes the stable cycle outcome `monitoring.assignment_source_deadline_exceeded`.
- Assignment scheduling uses the single canonical `AgentFleetProtocol.MaximumFutureClockSkew`. A timestamp exactly
  at the limit is clamped to the current instant; one tick beyond the limit is ignored and cannot defer probes.
- The same canonical skew limit now governs observation ingress, removing the previous duplicate constant.
- `pg_isready` timeout and caller cancellation share one production wait/cleanup path. It kills the complete owned
  tree, waits under an independent five-second cleanup bound, refuses unconfirmed termination, then returns timeout
  or propagates caller cancellation.
- Two Windows-only synthetic cases start a PowerShell parent and long-running `ping` child and prove both PIDs exit
  through the timeout and cancellation branches. No real PostgreSQL utility or service is involved.

## Schema and SQL evidence

- One Server migration was scaffolded with the repository-pinned `dotnet-ef 10.0.9` from an existing cache-only
  source. No new dependency or package version was introduced and no external source was contacted.
- The resulting schema, check constraints and composite binding/channel integrity were exercised with ephemeral
  SQLite. The PostgreSQL-specific migration itself was not applied to SQLite or any real database.
- `dotnet ef migrations has-pending-model-changes` reported no drift.
- Forward PostgreSQL SQL was generated locally and inspected for quarantine, the alternate key, the composite
  foreign key, provenance checks and idempotency uniqueness.
- Reverse PostgreSQL SQL was generated locally and inspected: it only raises the fail-closed downgrade code and
  contains neither `UPDATE notification_deliveries` nor `DROP TABLE alert_rule_channel_bindings`.
- No migration was applied, no operational row was read or mutated, and no PostgreSQL connection was attempted.

## Completion evidence

| R4-A criterion | Observed evidence |
|---|---|
| A rule without an exact binding creates no sendable delivery | Ingestion matrix admits only the bound channel and excludes enabled unbound, cross-environment and out-of-instance routes |
| Event/binding provenance and idempotency are durable | Composite FK, event/binding uniqueness, deterministic idempotency uniqueness and mismatch rejection in ephemeral SQLite |
| Historical unproven pending work remains retained and unsendable | Generated forward SQL sets `Quarantined` plus stable reason before installing the pending-provenance constraint |
| Delivery stays disabled and `AUD-H07` stays open | Startup validation precedes Kestrel configuration; unavailable publisher and zero normal adapters remain; no claim/lease/fence exists |
| Liveness and readiness are factually separate | Endpoint test proves liveness does not consult persistence and readiness returns bounded sanitised `503` for an unavailable dependency |
| Assignment retrieval is inside the global deadline | A deliberately non-cooperative source returns the stable deadline outcome within the configured cycle bound |
| Future timestamps cannot suspend probes indefinitely | Exact-limit and limit-plus-one-tick SQLite scheduling case; one canonical skew constant across Agent and ingress |
| Timeout and cancellation leave no synthetic `pg_isready` tree | Production wait/cleanup path terminates and observes both parent and child PIDs in both branches |

## Automatic evidence

Environment observed locally on 2026-07-21: Windows and .NET SDK `10.0.301`; only installed or cache-resident
dependencies were used.

| Gate | Observed result |
|---|---|
| Shutdown preflight | Passed: zero DB-Notifier-owned process, listener or visible product window before work |
| Release build | Passed for 18 solution projects, 0 warnings and 0 errors |
| Focused R4-A unit tests | Passed, 121/121 across routing, persistence, readiness, deadlines, future skew and process cleanup |
| Full unit suite | Passed, 370/370 |
| R4-A architecture tests | Passed, 2/2 |
| Architecture suite excluding the exact R0 contradiction | Passed, 39/39 |
| Coverage | Passed: 81.46% lines, 52.76% branches and 10/10 required components present |
| EF model drift | Passed: no model changes after the new migration |
| PostgreSQL SQL generation/inspection | Passed locally for forward quarantine/integrity and fail-closed reverse SQL; no database connection/application |
| .NET format | Passed with no changes required |
| Code documentation | Passed for 306 comment-capable source files |
| Markdown links | Passed for 548 local links in 120 Markdown files |
| Secrets | Passed for the current non-ignored worktree and available Git history without printing matched values |
| Full .NET aggregate command | Non-green: the exact pre-existing R0 architecture assertion failed; 15 unrelated HTTPS sandbox tests were additionally blocked by this managed runner's unavailable Windows Schannel credentials |
| Final shutdown and cleanup | Passed: no DB-Notifier/testhost/ping process or project-owned listener remained; the sole visible `dotnet` process was verified by its limited-access command line as the user's VS Code C# Dev Kit build host and was left untouched |

The aggregate command recorded 370/370 unit tests and 3/3 WPF tests passed, 39/40 architecture tests passed with
only the reserved R0 contradiction, and 6/21 integration tests passed. The 15 integration failures occur before
their HTTP assertions because the managed execution identity cannot acquire Windows TLS credentials. Repeating with
Windows Event Log output disabled exposed the same `Credenciais não disponíveis no pacote de segurança` Schannel
boundary. R4-A changes no certificate, TLS or integration-test composition, so this is reported as an environmental
limitation rather than corrected outside scope.

The R0 failure remains
`State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`: it expects fixed
artifact filenames while the accepted R0 workflow uses bounded diagnostic directories and run-attempt-qualified
names. It predates R4-A, is unchanged and remains unauthorised for correction.

An independent post-implementation revalidation immediately before the human decision reproduced the Release
build with zero warnings or errors, 370/370 unit tests, 3/3 WPF tests, 21/21 integration tests, 2/2 focused R4-A
architecture tests and the same single pre-existing R0 failure in the global architecture suite. The later 21/21
integration result shows that the earlier Schannel limitation did not recur under the revalidation identity; it
does not rewrite the factual evidence from the original managed execution.

## Preserved containment and limitations

- R2-A command tombstones, absent normal command transports/workers/stores and sandbox-only durable
  `ExecutionPolicy.Never` remain unchanged.
- R3 validation, disabled normal Fleet, empty normal provider registry and `UnavailableAgentCertificateIssuer`
  remain unchanged.
- External notification delivery, Server outbox publication and notification adapters remain unavailable.
- No claim, lease, fence, reclaim, dead-letter protocol or PostgreSQL concurrency campaign was implemented or
  inferred. `AUD-H07` remains open and belongs to R4-B.
- The idempotency key is persisted but intentionally does not flow to an adapter while adapters and delivery are
  unavailable; that future hand-off belongs to R4-B.
- No PostgreSQL/provider/service/Docker runtime, operational migration, external endpoint, remote CI, online
  advisory feed, push or deploy was used.
- No lifecycle state or AIOps mode was promoted.

## Rollback and stop condition

Safe rollback is containment: keep external delivery disabled, retain bindings, provenance and quarantined work,
and do not downgrade the schema. The migration refuses destructive downgrade because the preceding schema cannot
represent this evidence safely. If future work requires real PostgreSQL, a tenant model, channel activation,
operational data mutation, claim/lease/fence, a new dependency, R0 correction or changes to R2-A/R3 containment,
stop and request separate authority.

## Human decision and next boundary

On 2026-07-21, Bruno issued the exact decision `ACEITO O R4-A COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM
AUTORIZAR CORREÇÃO FORA DO ESCOPO.` This closes the independent review of R4-A and accepts `AUD-H06`, `AUD-M06` and
`AUD-M08` only within the bounded local scope evidenced by this report. `AUD-H07` remains contained and open.

The decision does not authorise correction of R0, R4-B, delivery activation, real PostgreSQL or external services,
R5–R8, R7-A0/O1, AIOps promotion, commands, automation or a lifecycle transition. A proposal for any later lot must
be requested and reviewed separately before implementation authority can exist.
