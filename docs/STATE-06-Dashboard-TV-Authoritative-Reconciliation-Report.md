# STATE-06 Dashboard TV Authoritative Snapshot and Periodic Reconciliation Sandbox Report

## Authority and status

- Date: 2026-07-18
- Lifecycle position: `STATE-06 INTEGRATION`, unchanged
- Increment: `Dashboard TV Authoritative Snapshot and Periodic Reconciliation Sandbox`
- Authority: Bruno authorised only a versioned read-only snapshot, test-only authentication, exact local sandbox activation, immediate TV entry read, serial 30-second reconciliation, strong ETag/`304`, cancellation, last-valid-snapshot preservation and deterministic/local E2E tests
- Automatic restricted-increment Quality Gate: `APPROVED WITH RESERVATION`
- Human review of this increment: `ACCEPTED WITH RECORDED LIMITATIONS AND RESERVATION` on 2026-07-18 for commit `70b3960`
- `STATE-06` exit Quality/Human Gate: `NOT EVALUATED`
- SignalR, notifications, operational Agent/provider/monitoring, commands, external persistence/identity, LLM, executor, deploy, promotion and lifecycle transition: `NOT AUTHORISED`

This report records one local sandbox implementation. Bruno subsequently accepted commit `70b3960` with the limitations and unrelated legacy-test reservation recorded here. That decision does not enable an operational runtime, approve `OBSERVER`, authorise another increment or transition the lifecycle.

## Plain-language outcome

The normal Dashboard still uses its clearly labelled demonstration. Only an exact build flag on an HTTPS loopback origin can compose the new adapter. In that temporary local sandbox, entering TV mode starts one read immediately. A new read is scheduled 30 seconds after the previous read finishes, so requests cannot overlap.

The API returns an immutable test snapshot protected by human test authentication and a strong content ETag. A matching conditional request returns `304 Not Modified`; the Dashboard then keeps the original snapshot and evidence times. Network, authorisation, compatibility or validation failures do not replace a previously accepted snapshot. The interface retains that evidence, continues to age it factually and labels the current sandbox failure.

This is not monitoring. The snapshot is a deterministic in-memory fixture and no Agent, provider, monitored database, PostgreSQL, external identity or notification channel participates.

## Implemented boundaries

### Versioned contract and source

- `dashboard-tv.v1` is a separate read-only transport contract that converts into the existing `inventory.v1` presentation model only after validation.
- Server and Dashboard boundaries enforce `500` items, `200` characters per textual field and latency from `0` to `3,600,000` milliseconds.
- Schema, exact field set, item identity uniqueness, canonical health states, UTC format, chronological ordering and future timestamps fail closed.
- The server fixture is created once from `TimeProvider`; its two provider-neutral items remain immutable for the process lifetime.
- The contract carries no credential, connection string, Agent identity, provider-native payload, command or administrative reference.

### API sandbox activation and protocol

- The route is fixed at `GET /api/v1/dashboard/tv-snapshot`.
- Registration requires both host environment `DashboardTvSandbox` and `DashboardTvSandbox:Enabled=true`; either missing guard leaves the service and route absent.
- The endpoint uses the existing human read policy and human rate-limit policy. E2E authentication is an in-process test scheme carrying only a bounded test subject.
- Responses include schema identity, `Cache-Control: private, no-cache` and a strong SHA-256 ETag calculated from the exact JSON bytes.
- A matching strong `If-None-Match` returns `304` with no body. Missing, weak or malformed conditional tags cannot be treated as a match.
- The endpoint performs no write, persistence, Agent/provider access, command, event, outbox or notification work.

### Dashboard adapter and coordinator

- The adapter exists only when `VITE_DB_NOTIFIER_TV_SANDBOX=local-test` and the browser origin is HTTPS loopback (`localhost`, `127.0.0.1` or `[::1]`).
- It reads only the fixed same-origin route, uses same-origin browser authentication and accepts no configurable endpoint or runtime credential.
- The response body is limited to `512 KiB` before JSON is accepted; schema headers, strong ETag, exact keys, bounds and timestamps are validated.
- TV entry triggers the first read without waiting for a timer. The next timer is created only after the active read settles, proving maximum concurrency of one.
- Leaving or re-entering TV aborts the request, cancels the timer and increments a session generation so late responses cannot publish.
- `304` preserves the accepted snapshot and its acceptance/evidence times. Failures preserve the last valid snapshot and publish `offline`, `error`, `denied` or `incompatible` source truth.
- Outside the exact sandbox composition, the existing demonstration remains the only source and no API call occurs.

## Deterministic and E2E evidence

- TypeScript tests prove exact activation guards, bounded parsing, test-subject isolation, strong ETag requirements, immediate read, no overlap, 30-second scheduling, `304` preservation, offline LKG preservation, abort and late-completion rejection.
- .NET unit tests prove the deterministic fixture, contract bounds, timestamp/identity failure and dual activation guards.
- .NET E2E tests start temporary Kestrel on HTTPS loopback with a P-256 certificate and in-process human test identity. They prove authenticated `200`, strong ETag, schema header, conditional `304`, empty `304` body and unauthenticated `401`.
- The E2E host, listener, certificate and test identity are disposed by the fixture. No browser sample was requested or opened.

## Direct review findings and corrections

No unresolved Critical or High issue has been identified in the authorised scope. The direct review found and corrected these implementation-stage issues:

| Severity | Finding | Correction and evidence |
|---|---|---|
| High | The first endpoint composition reused the ordinary human JWT policy. It failed closed without an IdP, but it was not an exclusively test-authenticated boundary and could have mixed identities if an IdP were configured. | The endpoint now has a dedicated scheme and policy registered only under both sandbox guards. The handler admits only one bounded test subject on HTTPS loopback and cannot authorise ordinary human routes. |
| Medium | The first TypeScript test timestamp could be later than the executing host clock, making the strict future-evidence validator reject the nominal valid case. | The fixture now uses an unambiguously historical UTC instant; the validator was not weakened. |
| Medium | A queued rejected promise was created before the coordinator consumed it, producing an unhandled-rejection race in the test runner. | The test now rejects a controlled deferred operation only after the scheduled reconciliation begins. |
| Medium | A self-signed E2E certificate initially remained in an in-memory key representation that Windows Schannel could not serve reliably. | The certificate is rehydrated into a non-persistent current-user test key container, its PKCS#12 bytes are zeroed and HTTPS E2E passes. |
| Medium | Initial failure labels claimed that a last snapshot was preserved even when the first read had never accepted one. | Failure labels now state only the observed sandbox condition; preserved evidence is visible only when it actually exists. |
| Low | An older presentation regression test matched the previous parameterless TV status component. | The test now asserts the factual sandbox-source property while preserving Fullscreen, clock and freshness checks. |

## Verification

| Gate | Observed result |
|---|---|
| Shutdown preflight | Passed; zero DB-Notifier process or owned listener was present |
| Complete Release build | Passed for `15` projects with zero warnings and zero errors |
| .NET unit/model/provider/presentation tests | `304/304` passed, including `4` focused contract tests |
| Architecture tests | `16/16` passed |
| Local integration tests | `8/8` passed, including `3` Dashboard TV HTTPS tests |
| Dashboard TypeScript check and production build | Passed |
| Dashboard tests | `49/49` passed, including `7` reconciliation tests |
| .NET coverage | `78.37%` lines and `52.34%` branches; floors are `70%` and `45%` |
| .NET format/analyzers | Passed |
| Generated brand, tokens and localisation | Passed and current |
| Code documentation | Passed for `236` comment-capable source files |
| Markdown links | Passed for `322` local links in `77` Markdown files |
| Secret scan | Passed for the non-ignored worktree and available Git history |
| Fail-closed runtime smoke | Passed: liveness `200`, protected endpoints `426`, Agent workers disabled and no local persistence initialised |
| Legacy compatibility suite | One pre-existing fixture-maintenance test failed: its complete empty NuGet report lists `13` projects while baseline commit `f58dd05` and the current solution both contain `15` |
| Runtime cleanup | Passed after all local tests: no DB-Notifier process or owned listener remained |

Online dependency audits are not authorised because this increment prohibits external resources. No prior online result will be inferred.

## Limitations and residual conditions

- The deterministic fixture is server-authoritative only inside this sandbox; it is not operational truth and proves no monitored database state.
- Test authentication and a loopback certificate do not prove an IdP, proxy, corporate TLS, CORS deployment, PKI, vault or production authorisation model.
- The browser guard is a composition safeguard, not an authorisation boundary; the API policy remains the actual boundary.
- A `512 KiB` browser body ceiling is checked from `Content-Length` when present and again after Fetch has produced text. The browser Fetch API does not expose a portable streaming abort proof for this small sandbox response.
- The serial coordinator schedules 30 seconds after the preceding request settles. This deliberately prevents overlap but does not attempt fixed-wall-clock catch-up after a slow request.
- A `304` proves only that the sandbox resource representation is unchanged. It does not make old evidence fresh.
- No browser UI E2E or human visual sample was executed; deterministic source/presentation tests and real API HTTPS E2E are separate layers.
- No SignalR hint exists, so a fixture change could be observed only at the next periodic read.
- No operational source, persistence, browser suspension endurance, multi-client load, provider, Agent, notification, command, deploy or production path was tested.

These limitations prevent any claim of complete `STATE-06`, operational readiness, `OBSERVER`, `STATE-07`, production or release.

## Gate classification and human decision

The automatic Quality Gate is `APPROVED WITH RESERVATION` only for this restricted increment. Every changed-product, architecture, integration, frontend, coverage, format, documentation, link, secret and fail-closed runtime gate passed. The reservation is the unrelated legacy characterisation fixture described above; it already listed `13` projects while baseline commit `f58dd05` contained `15`, and repairing it was not silently added to this Dashboard-only authority.

Bruno accepted commit `70b3960` with the limitations and reservation after confirming that he reviewed the outcome, corrections, verification and residual conditions and understood that the increment is a deterministic local sandbox rather than operational monitoring. His decision applies exclusively to this increment and explicitly does not authorise new development, `OBSERVER`, operational activation, deploy or any lifecycle transition.

Bruno's decision was: `Aceitar com as limitações e a ressalva o incremento correspondente ao commit 70b3960.` No further action is authorised by that acceptance.

## Post-acceptance status of the legacy-fixture reservation

A later, separately authorised remediation restored the positive synthetic NuGet report from `13` to exact parity with the solution's `15` projects. The verifier remained fail-closed, both negative fixtures remained rejected and the complete legacy gate passed. This later evidence resolves the known fixture-maintenance condition prospectively; it does not rewrite this report's original automatic gate or Bruno's acceptance of commit `70b3960` with the reservation then present. The owning evidence is the [NuGet fixture parity remediation report](STATE-06-NuGet-Legacy-Fixture-Parity-Remediation-Report.md).
