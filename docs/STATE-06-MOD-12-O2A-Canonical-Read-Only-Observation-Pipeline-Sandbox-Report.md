# MOD-12 O2-A — Canonical Read-Only Observation Pipeline Sandbox Report

## Decision summary

- Authorised baseline: `6cfe42facb83819447c216757cdf1d73bfc180de`
- Automatic result: `APPROVED`
- Human acceptance: `PENDING`
- Runtime boundary: exact test-only opt-in sandbox
- Synthetic durable state: caller-owned temporary O1 trust root only
- Other O2-A state: bounded in-memory fixtures
- MOD-12 activation: `ActivationState=None`
- Normal composition references: `0`
- Operational telemetry, corpus, provider, database or credential: `0`
- UI, LLM, recommendation, command or automation: `0`
- Lifecycle transition: `NOT AUTHORISED`

O2-A composes the existing production Agent dispatch and Server validation boundaries with the accepted synthetic O1
trust/resource sandbox and the pure MOD-12 analysis service. It does not activate `OBSERVER`, register a normal runtime
or claim that any provider, corpus or operational source is supported.

## Exact sandbox composition

The executable chain is:

```text
synthetic HealthObservation fixture
  -> O2AAgentOutboxStore
  -> AgentOutboxDispatchRunner
  -> O2ALoopbackObservationTransport
  -> ObservationBatchIngestor
  -> O2ACanonicalObservationPipeline
  -> O1 trust checkpoint and resource coordinator
  -> CanonicalObserverTelemetryAdapter
  -> ObserverAnalysisService
  -> complete non-authorising report only
```

The chain is reachable as a separate process only through
`o2a-canonical-observation-pipeline-sandbox` in the existing consolidated test host. The process accepts one exact
`reference` operation and an O2-A-named root below the operating-system temporary directory. Product source and normal
project composition contain no marker, pipeline, loopback transport or process-bridge reference.

## Canonical envelope

`O2ACanonicalObservationEnvelope` is immutable, versioned as `o2a.canonical-observation.v1` and provider-neutral.

| Contract group | Executable fields and rule |
|---|---|
| Identity | message, sequence, observation, instance and Agent identifiers |
| Scope | exact authorisation scope, synthetic environment and non-mutating permitted purpose |
| Version | contract, provider declaration, transformation and O1 accounting profiles |
| Time | observed and Server-received UTC instants, sequence and context revision |
| Result | canonical status, method, evidence level, duration metric family and milliseconds |
| Provenance | digest of the validated source plus digest and context retained by the O2-A outcome |
| Classification | operational-telemetry class, sanitised status and ephemeral-analysis-only retention |
| Completeness | exact expected/actual item counts, explicit missing-field collection and bounded limitations |

The envelope has no secret, credential, connection string, query, SQL, shell, command or provider-native payload field.
The initial contract deliberately supports only the narrow duration-by-outcome transformation. A canonical observation
carrying the existing diagnostic-error subcontract is rejected as
`o2a.canonical.error_contract_unsupported` rather than silently losing error meaning.

## Fail-closed evidence

| Scenario | Result |
|---|---|
| schema or envelope mismatch | rejected by the production Server validator |
| future skew above five minutes | rejected as `observation.observed_at_future` |
| stale evidence | accepted as factual backlog but analysed as `InsufficientEvidence`, never healthy by default |
| exact duplicate while pending | classified duplicate without another analysis |
| exact replay after completion | classified duplicate without another publication |
| message or observation identity conflict | rejected with stable O2-A conflict code |
| reorder with a lower gap | retained without analysis until the missing contiguous sequence arrives |
| signed grant revocation | adaptation refused as `aiops.observer.adapter.provenance_revoked` |
| in-flight O1 context supersession | complete publication refused as `aiops.observer.analysis.context_stale` |
| absolute deadline at admission | refused as `resource.deadline` |
| over-limit input declaration | refused as `resource.input_bytes` before a lease |
| cancellation before publication | no outcome or report is published; the same source can be retried safely |
| resource release and fencing | quiescent release proved in O2-A; inherited monotonic-fence and reserved-control tests passed |
| complete normal sample | exactly one complete report, `IsAuthorising=false`, capability `observer-analysis`, state `None` |

No incomplete, cancelled, expired, stale-context, revoked or resource-refused attempt enters `PublishedReports`.

## Automated evidence

All checks ran locally and offline without restore, download or dependency changes.

| Check | Result |
|---|---|
| focused O2-A integration tests | `6/6 PASSED` |
| focused O2-A architecture tests | `4/4 PASSED` |
| O1 fencing/control regressions | `2/2 PASSED` |
| complete integration suite | `38/38 PASSED` |
| complete architecture suite | `59/59 PASSED` |
| MOD-12 unit tests | `77/77 PASSED` |
| complete unit suite, excluding the known visible-terminal readiness case | `397/397 PASSED` |
| WPF regression suite observed in the proportional run | `10/10 PASSED` |
| proportional suite total | `504/504 PASSED` across the individually completed suites |
| Release solution build | `PASSED`, zero warnings and zero errors |
| exact separate-process marker run | `accepted=1`, `published=1`, `activationState=None`, `authorising=false` |
| `dotnet format --verify-no-changes --no-restore` | `PASSED` |
| code-documentation gate | `PASSED` for `340` comment-capable source files |
| Markdown links | `PASSED` for `672` local links in `149` files |
| secret scan | `PASSED` for the non-ignored worktree and available Git history |

One parallel solution orchestration initially reported `UnauthorizedAccessException` in the unrelated legacy
configuration-migration fault-injection test while moving its temporary file. The exact four-case test was repeated
immediately and passed `4/4`; the complete isolated unit suite then passed `397/397`. No code outside O2-A was changed.

The pre-existing
`TimeoutAndCancellationTerminateSyntheticReadinessProcessTree` test remained excluded from the proportional unit run.
Its R8 evidence records that it can leave a visible generic Windows Terminal tab after the synthetic `ping.exe` child
has ended. It is outside O2-A, and no O2-A conclusion depends on it.

## Cleanup and preserved boundaries

- The separate-process O2-A root was resolved below the operating-system temporary directory, inspected and removed.
- Test helpers validate the exact temporary-root prefix before recursive removal and assert absence afterwards.
- Final process, listener, visible-window and temporary-root checks found no O2-A or DB-Notifier-owned residue.
- No product `src/` file changed or references O2-A.
- `ObserverActivationState` still declares only `None`.
- No package, lockfile, project reference, schema, migration or product configuration changed.
- No browser, UI, provider, database, telemetry source, corpus, credential or external endpoint was used.
- No CI, push, deploy, lifecycle transition or mode activation occurred.

## Limitations and next gate

O2-A is a synthetic integration proof, not an operational pipeline. Its Agent outbox and Server-side O2-A state are
in-memory; only the already accepted O1 trust checkpoint uses a temporary durable filesystem. Crash/restart continuity,
bounded retention, operational observability and backpressure hardening belong to a separately authorised O2-B.
Representative corpus, calibration, empirical host limits, normal composition, kill switch, rollback, API/UI projection
and the transition to `OBSERVER` remain blocked.

The authorised automatic O2-A increment is `APPROVED`. A separate Human Gate may now accept, accept with reservations
or reject only this synthetic sandbox and its stated limitations. That decision will not activate `OBSERVER` or
authorise O2-B.
