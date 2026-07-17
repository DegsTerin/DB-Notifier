# STATE-06 MOD-12 Trust Governance and Resource Envelope Report

## Status and authority

- Date: 2026-07-17
- Workspace lifecycle position: `STATE-06 INTEGRATION`
- MOD-12 mode status: no active mode; `none → OBSERVER` remains pending
- Authority: after reviewing the [documentation-only proposal](STATE-06-MOD-12-Trust-Governance-And-Resource-Envelope-Proposal.md), Bruno authorised only the responsibility map, ADR, conceptual trust/checkpoint/resource contracts, threat model, traceability and future-test plan
- Explicit exclusions: implementation, code, migrations, runtime, real keys, persistence, services, external actions and promotion to `OBSERVER`

This report records a documentary architecture increment. It is not implementation evidence and grants no authority beyond review of the resulting documents.

## Outcome

The authorised package is complete as a review candidate:

- proposed [ADR-0007](architecture/ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md) compares four alternatives and recommends a channel-independent signed trust bundle, a host-owned durable checkpoint and explicit resource reservation;
- the [conceptual contract](architecture/AIOps-Trust-Governance-And-Resource-Envelope.md) defines roles, incompatible permissions, independently authenticated policy assertions/root delegations, bundle fields, two-level checkpoint state, rotation/recovery, resource dimensions, corpus governance, traceability and future vectors;
- the [threat model](architecture/Threat-Model.md) now registers future MOD-12 trust/checkpoint/resource assets, four additional boundaries, threat groups, owners, controls, residual risks and future acceptance scenarios;
- the [AIOps guardrails](architecture/AIOps-Architecture-Guardrails.md) distinguish current local evidence from the unimplemented trust and resource design; and
- the [architecture index](architecture/README.md) exposes the ADR as `proposed` and the contract as a review candidate.

`ADR-0007` is intentionally not marked accepted. Authorisation to write a decision candidate is not Human Gate acceptance of that decision.

## Architecture decisions proposed for review

### Trust and authorisation

- The root is provisioned independently and never trusted from the bundle it validates.
- Policy authorisation, root/operational key custody, bundle/grant/revocation signing, publication, host coordination, checkpoint ownership, verification and audit are separate responsibilities.
- A valid operational signature proves origin and integrity, not legitimate scope. A separately signed `PolicyAuthorisationAssertion`, root-signed `RoleKeyDelegation` and canonical atom-by-atom subset predicate are mandatory; a digest alone cannot prove subset.
- Policy-attestation, trust-bundle, grant and revocation roles use distinct key identities and distinct public-key material.
- The publisher is an untrusted carrier even if a future transport is authenticated.

### Continuity and revocation

- A normal trust update is a direct successor or a bounded complete chain of direct successors, each linked to its exact predecessor digest, authorisation/delegation and revocation head.
- A stable trust-domain head stores the recovery epoch/series high-water outside the subordinate series checkpoint, so an old epoch cannot bootstrap itself by selecting another key.
- Lower generations are rollback; the same generation with another digest is split view; a gap is extraordinary reconciliation, not a highest-generation-wins update.
- Bundle, revocation head, checkpoint and audit intent would become visible in one future local transaction. No fleet-wide simultaneous atomicity is claimed.
- Missing, corrupt, cloned, restored or divergent continuity quarantines only the affected MOD-12 scope; deterministic monitoring remains independent.
- A checkpoint and local audit restored together cannot prove silent anti-rollback. Digest chaining also does not prevent split view until independent views are compared. Those limitations remain explicit.

### Resources and corpus

- Effective limits are the minimum of complete finite trusted-host, separately signed policy-assertion, bundle and per-request ceilings. Each later layer can only reduce earlier trusted ceilings; producer declarations can never raise them.
- The absolute deadline starts before any lease. A short global ingress lease and first-byte/inter-read liveness protect pre-authentication work; authenticated hierarchical buckets are acquired only after the canonical scope is proved.
- The envelope covers encoded/expanded bytes, expansion ratio, parser structure, cardinality, versioned accounted/observed memory, work, deadline, cancellation latency, concurrency, hierarchical quota/fairness, future queue and output size.
- Reusable leases, cumulative execution consumption, window quota tokens and audit counters have separate rules; only reusable/unused capacity is released.
- A future source must be cooperatively cancellation/deadline-aware. Size/count limits alone are not latency evidence.
- The first future local proof remains serial and retains no queue. No operational numerical value was invented.
- An incomplete result is diagnostic and non-authorising; subset precision, recall, calibration and aggregates are absent/null rather than merely barred from a gate.
- A future corpus requires an immutable manifest with authority, scope, classification, provenance, digest, versions, transformations, segments, bias, expiry and adversarial governance. No corpus was collected or enlarged.

## Reviews performed

### Architecture review

The package was compared with the accepted architecture pack, Agent/API protocol direction, persistence/retention ADRs and the current inactive MOD-12 boundary. It preserves dependency direction and makes a future trust bundle independent of transport, allowing later connected or controlled-offline delivery without treating either channel as a trust anchor.

Four alternatives were considered: static application configuration, signed bundle with durable host checkpoint, transparency/multi-witness state and self-contained evidence carrying its own anchors. The signed-bundle option is proposed because it preserves verifier isolation and offline-capable validation while keeping witness infrastructure as a compatible future strengthening. Static configuration remains only the current test baseline; circular self-trust is rejected.

### Security review

The threat traceability registers `M12-T01` through `M12-T22`. It covers unauthorised issuance, rollback, freeze, fast-forward, split view, cross-scope replay, signer/root compromise, downgrade/confusion, clock manipulation, crash/TOCTOU, restore/clone, audit tampering, source/bundle exhaustion, memory/work/overflow, cancellation, concurrency/fairness, partial-result misuse, corpus poisoning and diagnostic leakage.

Each threat has a required control, accountable owner, enforcement/review role and future vector. Silent whole-store restore and isolated split view remain residual until an independent witness, monotonic platform anchor or authenticated reconciliation mechanism is selected, implemented and tested.

### Data and resource review

The review distinguishes the observed local budget — cases, declared samples, deterministic work and elapsed time — from future requirements that have no implementation evidence. It explicitly records that the current synchronous runner does not prove bounded encoded/expanded bytes, peak memory, shared concurrency, fairness, a queue or cancellation latency.

The corrected plan contains 53 deterministic future contract vectors: 19 for trust/checkpoint, 29 for resource/backpressure and five for corpus governance. Three later empirical campaigns separately cover real cancellation/liveness latency, observed memory and work-to-runtime calibration. None was implemented or executed in this increment.

## Scope confirmation

- Only Markdown architecture, governance and factual evidence files are changed.
- No C#, TypeScript, configuration, migration, project, package, dependency or executable test file is changed.
- No DI registration, API route, Agent path, provider, database, store, queue, service, worker, key, certificate, network client, LLM, recommendation, plan, command or executor is created.
- No runtime, build, product test, provider integration, data collection, persistence or external action is performed.
- The lifecycle remains `STATE-06 INTEGRATION`; no MOD-12 mode is active.

## Verification

Environment: Windows, documentation-only workspace, 2026-07-17.

| Check | Observed result |
|---|---|
| Mandatory shutdown preflight | passed: zero DB-Notifier processes stopped or remaining, zero blocking project windows and zero owned listeners; the user's Visual Studio Code window was preserved |
| Markdown-link gate | passed for 266 local links in 70 Markdown files |
| Code-documentation gate | passed for 208 comment-capable source files; no source file changed |
| Secret scan | passed for the current non-ignored worktree and available Git history |
| Git diff checks | `git diff --check` and `git diff --cached --check` passed |
| Changed-file scope inspection | passed: nine Markdown files only (three added and six modified); no product source, configuration, executable test or runtime file changed |
| Product build/tests/runtime | `NOT APPLICABLE`; prohibited by the documentary scope and not executed |

## Gate classification

- Documentary Quality Gate: `APPROVED` for the authorised documentation-only scope.
- Architecture/security/data review candidate: complete automatically, pending Bruno's human decision.
- `ADR-0007`: `proposed`, not accepted.
- Implementation authority: absent.
- `none → OBSERVER`: `PENDING` and outside this increment.
- Later MOD-12 modes: `NOT AUTHORISED`.

## Limitations and residual conditions

- No root, policy assertion, role delegation, key, signer, publisher, trust-domain head, checkpoint store, transaction, witness, audit sink, resource coordinator or queue exists.
- No production ceiling for bytes, memory, work, time, cancellation or concurrency is selected.
- No restore, split-view, rotation, compromise, load, memory, fairness, poisoning or red-team vector was executed.
- Current policy/revocation cryptography and budget tests remain the accepted local baseline; this document does not upgrade their evidence.
- The design does not choose a storage, protocol, certificate, signing or witness technology.
- Human acceptance of this package would still not authorise implementation or mode promotion.

## Next decision

Bruno should review this report, proposed `ADR-0007`, the conceptual contract and the updated threat model. The valid decisions are `ACCEPTED`, `ACCEPTED WITH RESERVATIONS`, `CHANGES REQUESTED` or `REJECTED` for this documentation-only increment.

If accepted, the next activity is not runtime. It is a separately proposed and separately authorised local contract/test increment, still without operational integration. No implementation may begin from this report alone.

## Human review addendum — 2026-07-17

After commit `137c889`, Bruno reviewed this report and accepted the documentation-only increment, including every limitation and residual condition recorded above. He expressly clarified that he did not have direct access to the local `ADR-0007`, conceptual contract or threat-model files; his decision therefore relies on this report's summary and references rather than an independent inspection of those documents. That precision limits the evidence reviewed but does not alter his acceptance of the documentary increment.

The resulting classification is:

- documentary increment: `ACCEPTED`;
- human-review basis: this report, without independent human inspection of the linked local documents;
- `ADR-0007`: remains `proposed`; it was accepted as a proposed documentary architecture decision, not promoted to `accepted` status;
- implementation and runtime authority: absent;
- lifecycle and MOD-12 mode: unchanged; `STATE-06 INTEGRATION` remains current and `none → OBSERVER` remains pending.

The earlier pending classification and next-decision text remain the pre-decision automatic-report snapshot. This addendum is the later factual Human Gate outcome and grants no implementation authority.
