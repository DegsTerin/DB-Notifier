# O5-R2 — Inactive Observer Control Plane Report

## Disposition

- Date: 2026-07-24.
- Authorised baseline: `601bf2d1b4352f1df83d959f653b11357ac9018b`.
- Automatic result: `APPROVED`.
- Human Gate O5-R2: `PENDING`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.
- Provider, database, corpus, credential and telemetry: not used.
- External access, restore, download, push and deploy: not performed.

O5-R2 adds the dormant provider-neutral control boundary needed for later safety work without
adding an activation path. Normal Server composition now resolves only
`DormantObserverControlPlane` and `UnavailableObserverActivationAuthority`. The coordinator owns no
store, evaluator, pipeline, corpus, publisher, hosted service or background task. Every normal
admission returns `None`, `MayEvaluate=false`, `MayPublish=false`, fence `0`, with zero evaluation
starts and zero publications.

The durable implementation used to exercise opt-in, kill switch, recovery and rollback exists only
inside the exact `DBNOTIFIER_O5_R2_TEST_ONLY` integration sandbox. Its store and keys are synthetic,
temporary and absent from normal composition.

## Implemented boundaries

### Normal composition

- `ObserverActivationState` still has only `None`.
- `Program.cs` registers the dormant coordinator and unavailable authority.
- Missing configuration needs no fallback: the normal coordinator starts safe and remains `None`.
- The dormant coordinator refuses before approval authentication and exposes no evaluator or
  publisher dependency.
- The activation authority contains no key, store or opt-in capability and rejects every approval.
- No API, UI, hosted worker, configuration switch or state transition was added.

### Approval contract

- Approvals are bound to one approval nonce, cell, environment, purpose, immutable revision,
  inclusive start and exclusive expiry.
- Lifetime is limited to 15 minutes.
- Exactly two distinct roles are required.
- Signature bytes and identifiers are bounded at construction.
- The sandbox uses distinct synthetic HMAC keys for roles A and B and a third distinct key for
  checkpoint authentication.
- Approval nonces are consumed atomically and at most once; replay fails closed.
- The exact accepted candidate scope is
  `OBS-PILOT-PG16-LOCAL-001 / local-laboratory / read-only-observation`.
- A successful sandbox admission remains a control simulation only: it never grants evaluation or
  publication authority and never changes activation.

### Kill switch, fencing and rollback

- The priority kill switch commits a complete inactive checkpoint before cancelling every active
  simulated admission.
- The kill switch advances the monotonic fence, removes the active context and rejects all later
  admissions.
- The authorised proof attempted 100 post-kill admissions; all 100 were refused.
- Old, current and stale fences cannot publish because publication remains impossible while
  activation is `None`.
- Rollback has only one outcome: a complete, fenced, kill-switched `None` checkpoint.
- Restart invalidates an interrupted active simulation before returning a safe checkpoint.

### Durable synthetic continuity

The temporary store uses authenticated envelopes, durable temporary writes, atomic replacement and
a separate monotonic authenticated witness. It accepts only:

1. the previous complete checkpoint when a crash occurs before replacement; or
2. the next complete checkpoint when a crash occurs after replacement but before witness advance.

Corrupt envelopes, missing continuity, lower generations, generation gaps, equal-generation
divergence and invalid activation state enter authenticated quarantine. Temporary files are
restricted to one exact GUID-owned sandbox root and removed after every test.

## Adversarial matrix

| Scenario | Result |
|---|---|
| exact marker missing or wrong | refused before sandbox creation |
| normal authority unavailable | refused; state `None` |
| wrong cell/environment/purpose | refused |
| approval expired or deadline elapsed | refused |
| crossed role key or altered authentication | refused |
| approval replay | refused |
| cancellation before commit | cancelled without activation |
| crash before replace | old complete state recovered |
| crash after replace/before witness | new complete state recovered |
| restart with interrupted context | context fenced and cleared; state `None` |
| state corruption | quarantined |
| rollback | quarantined |
| generation gap | quarantined |
| split view | quarantined |
| obsolete/current context publication | both refused |
| kill switch with current context | context cancelled and fenced |
| 100 admissions after kill switch | `100/100` refused |
| rollback request | complete killed `None` checkpoint |
| missing normal configuration | dormant normal composition remains `None` |
| cleanup | exact temporary roots removed |

## Automated evidence

All checks ran locally and offline without restore, download or dependency changes.

| Check | Result |
|---|---|
| shutdown preflight | baseline exact, worktree clean, zero DB-Notifier runtime/root |
| focused normal-composition tests | `2/2 PASSED` |
| focused O5-R2 integration tests | `8/8 PASSED` |
| complete unit suite | `401/401 PASSED` |
| complete integration suite | `83/83 PASSED` |
| complete architecture suite | `75/75 PASSED` |
| complete WPF suite | `10/10 PASSED` |
| complete Release solution build | passed, zero warnings and zero errors |
| proportional .NET coverage | lines `82.01%`, branches `53.92%`, ten required components present |
| fail-closed runtime audit | passed; live `200`, protected endpoints `426`, Agent workers disabled, no command polling or local persistence |

The first solution build was intentionally given a one-second executor deadline and left orphaned
MSBuild nodes after the caller timed out. The nodes were identified by exact executable path,
command line and parentage, then removed with `dotnet build-server shutdown`. The subsequent
complete build passed. This was a build-tool cleanup event, not a product runtime or code defect.

## Preserved limitations

- O5-R2 does not activate `OBSERVER` and cannot be used to activate it.
- The test-only simulated admission does not consume O2/O3, start analysis or publish a result.
- No operational store, key, approval, corpus, provider, database, credential or telemetry exists.
- No recommendation, LLM, command, automation or execution surface was added.
- The O5 automatic gate remains blocked by the remaining O5-R3–O5-R10 prerequisites.
- O5-R6 and O5-R8 remain additionally blocked by the unnamed owners and laboratory/data
  prerequisites recorded by O5-R1.
- PostgreSQL remains a candidate cell only, with `Homologation=None` and public support `No`.

## Cleanup

The synthetic stores removed every GUID-owned temporary root in `finally`. The fail-closed audit
closed its local Server and Agent processes. Final cleanup confirmed zero DB-Notifier process,
listener, coverage root and `dbnotifier-o5r2-*` temporary root before the focused commit.

## Next decision

The only eligible next step is the Human Gate O5-R2. Approval would close this increment only and
would permit proposing O5-R3 separately. It would not authorise O5-R3 implementation, provider or
database runtime, corpus, `OBSERVER`, push, deploy or lifecycle transition.
