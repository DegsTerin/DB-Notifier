# MOD-12 O2-B — Durable Pipeline Continuity, Backpressure and Observability Sandbox Report

## Decision summary

- Authorised baseline: `2408093ff2a66d05e4f00f523cf4b3b35be71ac4`
- Automatic result: `APPROVED`
- Human acceptance: `PENDING`
- Runtime boundary: exact test-only opt-in sandbox
- Durable state: synthetic caller-owned operating-system temporary root only
- MOD-12 activation: `ActivationState=None`
- Normal composition references: `0`
- Operational telemetry, corpus, provider, database or credential: `0`
- UI, LLM, recommendation, command or automation: `0`
- Lifecycle transition: `NOT AUTHORISED`

O2-B adds authenticated crash continuity, bounded no-queue admission and sanitised counters around the accepted O2-A
pipeline. It does not register a normal runtime, activate `OBSERVER`, make an operational support claim or connect to
any source outside deterministic synthetic fixtures.

## Exact sandbox boundary

The exact process marker is `o2b-durable-pipeline-sandbox`. It is recognised only by the existing consolidated
test host and accepts only the `reference` operation plus a direct operating-system temporary child whose leaf starts
with `DBNotifier-O2B-`.

Product source and normal composition contain no O2-B marker, store, pipeline or process bridge. The only O2-A change
is an internal test method that restores a previously authenticated high-water/idempotency baseline and aligns a fresh
synthetic O1 session to the retained monotonic context revision.

## Durable transaction

One complete payload owns:

- stream, instance and scope identity;
- writer fence and highest contiguous sequence;
- monotonic O1 context revision;
- bounded message and observation digests;
- bounded pending gap records;
- bounded processing outcomes and non-authorising publication records;
- exact total publication count;
- sanitised code counters.

The store serialises the complete payload, authenticates it with SHA-256, embeds its predecessor digest and commits it
through a same-directory write-through temporary file followed by atomic replacement. A separate witness records the
last committed generation and state digest. Recovery accepts only:

1. the exact state named by the witness; or
2. one direct successor whose predecessor is the witness, which proves a crash after state replacement and before
   witness replacement.

Missing, corrupt, rolled-back or divergent continuity creates a durable quarantine code. Normal processing cannot
repair or bypass quarantine. Every session durably increments the writer fence; a previous session cannot commit after
a newer session opens.

The durable publication ledger is the O2-B publication boundary. O2-A's transient in-memory report is never exposed as
an O2-B publication before the complete outcome and publication record commit atomically. A crash before that commit
leaves durable pending work for replay; a crash after state replacement exposes the already committed publication
exactly once after witness repair.

## Recovery and fail-closed evidence

| Scenario | Observed result |
|---|---|
| restart and exact replay | high-water and publication survive; replay is duplicate; publication total stays one |
| durable gap and reorder | later sequence survives restart and publishes only after the missing lower sequence |
| crash before admission replacement | old complete state, with no phantom pending record |
| interruption after durable admission | pending work resumes once after restart |
| crash before completion replacement | old state with pending work; restart completes once |
| crash after completion replacement, before witness | new complete state accepted as the direct successor; no re-publication |
| missing witness/state tuple | durable quarantine and ingress refusal |
| corrupt authenticated state | durable quarantine and ingress refusal |
| restored old state against a newer witness | rollback quarantine |
| stale writer fence | old session refused as `o2b.store.stale_fence`; current session remains writable |
| context supersession | stale-context result is not published; context revision two survives restart and remains monotonic |
| cancellation after durable admission | pending work remains and resumes once on restart |
| absolute deadline | sequence completes with a refusal outcome and no partial publication |
| resource release | O1 lease reports `resource.released` after quiescence |

The independently accepted O1 sandbox remains the authority for signed trust-bundle durability and resource fencing.
O2-B creates fresh ephemeral synthetic O1 key material for each logical test session, aligns that session to the
authenticated O2-B context revision and never persists private keys. This report therefore proves pipeline continuity,
not operational trust distribution or protected key storage.

## Backpressure, retention and observability

- Concurrency remains serial and queue-free. A simultaneous caller receives `o2b.backpressure.busy`.
- At most eight non-contiguous pending observations are retained. The next admission receives
  `o2b.backpressure.saturated`.
- Message and observation digests retain at most 64 records each while preserving every pending identity.
- Detailed outcomes and publications retain the newest 32 records each.
- Aggregate publication truth remains exact independently of detail compaction.
- State documents are capped at 1 MiB.
- The store lock has a two-second bounded acquisition budget.
- Deadlines and cancellation are propagated through O2-A/O1; leases must become quiescent before reuse.
- The observability view contains only stable codes, counts, bounded cardinalities, high-water, publication total and
  quarantine state. Tests prove it contains no provider, message, instance or payload field/value.

These values are sandbox safety limits only. They are not operational ceilings, SLOs, fleet-wide fairness guarantees
or capacity recommendations.

## Automated evidence

All checks ran locally and offline, without restore, download, dependency changes or external access.

| Check | Result |
|---|---|
| focused O2-B integration tests | `15/15 PASSED` |
| focused O2-B architecture tests | `4/4 PASSED` |
| focused O1/O2-A regression tests | `16/16 PASSED` |
| complete integration suite | `53/53 PASSED` |
| complete architecture suite | `63/63 PASSED` |
| proportional unit/coverage suite | `399/399 PASSED` |
| coverage gate | lines `81.98%`, branches `53.85%`, `10/10` required components present |
| Release solution build | `PASSED`, zero warnings and zero errors |
| exact process restart/replay | two separate launches over one ledger; both reported `highest=1`, `totalPublications=1`, `activationState=None`, `authorising=false` |
| `dotnet format --verify-no-changes --no-restore` | `PASSED` |
| code-documentation gate | `PASSED` for `344` comment-capable source files |
| Markdown links | `PASSED` for `680` local links in `151` files |
| secret scan | `PASSED` for the non-ignored worktree and available Git history |

## Cleanup and preserved boundaries

- Every in-process fixture removed its exact `DBNotifier-O2B-*` root in `finally`.
- The separate-process proof used one literal validated temporary root, which was removed after both launches.
- Each O2-A session directory was deleted only after its in-memory keys and synchronisation primitives were disposed.
- No product source, package declaration, lockfile, migration, schema, configuration, UI or normal runtime changed.
- `ObserverActivationState` still declares only `None`.
- No browser, provider, database, telemetry source, corpus, credential, network endpoint, CI, push or deploy was used.
- Final process, listener, window and temporary-root checks remain part of the final evidence pass.

## Limitations and next gate

O2-B is still a synthetic sandbox. Its local hash witness detects the authorised missing/corrupt/state-rollback cases,
but a hostile full restore of every local state and witness artefact requires the independent host/backup witness
described by ADR-0007; no operational backup integration is claimed. Resource values have not been empirically
calibrated on a representative corpus. There is no normal composition, operational observability backend, kill switch,
UI, provider support, LLM, recommendation, command, automation or `OBSERVER` activation.

The automatic O2-B result is `APPROVED`. Human acceptance remains a separate pending decision. Acceptance of this
report would accept only the synthetic durable pipeline and its explicit limits; it would not authorise O3, a corpus,
normal composition, `OBSERVER` or lifecycle transition.
