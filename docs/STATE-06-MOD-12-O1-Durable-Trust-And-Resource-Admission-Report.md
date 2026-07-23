# MOD-12 O1 — Durable Trust Continuity and Resource Admission Sandbox Report

## Decision summary

- Authorised baseline: `c38c494413d16ce1090d31f30d7b2efa87c589dd`
- Automatic result: `APPROVED`
- Human acceptance: `APPROVED` in the separate [O1 Human Gate](STATE-06-MOD-12-O1-Human-Gate-Report.md)
- Runtime boundary: exact test-only opt-in sandbox
- Durable store: synthetic temporary filesystem only
- MOD-12 activation: `ActivationState=None`
- Normal composition references: `0`
- Operational data, provider, database, credential, LLM, recommendation, command or automation: `0`
- Lifecycle transition: `NOT AUTHORISED`

O1 implements the first local proof described by ADR-0007. It does not activate `OBSERVER` and does not add a product runtime. The implementation lives in the existing integration-test assembly and is reachable as a separate process only through the exact marker `o1-durable-trust-resource-sandbox` in the existing consolidated sandbox host.

## Implemented boundary

### Host trust coordinator

The sandbox validates a complete candidate before one atomic publication:

- independently provisioned public roots;
- P-256 keys with materially distinct key material for root, bundle, policy, corpus, bootstrap and recovery roles;
- root-signed, epoch- and series-bound role delegations;
- exact tenant, environment, purpose and scope;
- bundle signature and bounded validity;
- two distinct signatures for policy, corpus, bootstrap and recovery decisions;
- one-use decision nonces committed with the checkpoint;
- a signed, independently approved corpus manifest;
- finite resources, serial parallelism `1` and a disabled queue.

Exact replay of the already accepted bundle is idempotent and does not advance the checkpoint. Idempotency cannot bypass quarantine.

### Durable continuity

One digest-protected state envelope contains the trust checkpoint, corpus heads, consumed approval nonces and sanitised audit intents. The store writes a complete same-directory temporary file, flushes it, atomically replaces the state and then advances an independent continuity witness and installation marker. Corrupt or missing continuity metadata after initialisation fails closed. This local integrity mechanism is not presented as protection from an administrator who can rewrite every file in the synthetic root.

The recovery matrix proves:

- crash before replacement exposes the complete old state;
- crash after replacement and before witness exposes the complete new state and repairs the witness on restart;
- a restored lower state is quarantined as `trust.rollback`;
- validly signed rollback, generation gap and split view enter durable quarantine;
- ordinary and idempotent candidates cannot cross quarantine;
- recovery requires a new epoch, new series, distinct recovery root hierarchy, exact predecessor and dual one-use recovery approval;
- recovery clears quarantine only through another complete atomic commit.

### Resource admission and fencing

The local coordinator validates all declared dimensions before issuing a lease:

- input bytes;
- structure depth and item cardinality;
- deterministic accounted memory;
- deterministic work units;
- result count and output bytes;
- control metadata;
- absolute deadline and cancellation;
- maximum parallelism `1`;
- queue disabled.

Arithmetic is checked. A second data-plane admission is refused immediately without retaining a contender. A separately bounded control lease remains available while data capacity is occupied. Data leases carry monotonic fences and release reusable capacity exactly once. A release without quiescence is recorded as forced-fence release; an old fence cannot release newer capacity.

The existing test-only process host also owns an exclusive file fence. A concurrent process using the same synthetic accounting root receives `o1_sandbox.failed:fence_unavailable`.

This is a containment proof for one host/process. It makes no claim of fleet-wide accounting, cross-restart quotas, fairness, starvation freedom or physical latency.

### Synthetic corpus

The immutable test manifest binds:

- exact dataset, tenant, environment, purpose, epoch and series;
- monotonic revision and predecessor digest;
- exact case identifiers, partitions, segments and content digests;
- declared segment counts;
- independent data-owner and data-governance approval;
- independent corpus-attestation signature.

Changed content, attestation, scope, membership, revision or segment counts fail closed. The fixture is synthetic and bounded; it is not representative operational evidence, training data or backtesting authority.

## Vector traceability

The executable catalogue contains exactly `68` unique entries and maps every contractual identifier to an enforcing component, test group and accountable owner.

| Vector set | Count | Primary component | Executable evidence | Boundary |
|---|---:|---|---|---|
| `TR-01`–`TR-08` | 8 | `O1TrustCoordinator` | continuity, replay, rollback, gap and divergence | local synthetic trust |
| `TR-09`–`TR-16` | 8 | `O1TrustCoordinator` | independent roles, scope, dual control and cryptographic refusal | ephemeral P-256 fixtures |
| `TR-17`–`TR-23` | 7 | `O1SandboxStore` | crash/restart, witness, quarantine and recovery | temporary local store |
| `TR-24` | 1 | `O1ResourceCoordinator` | reserved control capacity and stale fencing | one local coordinator |
| `RE-01`–`RE-12` | 12 | `O1ResourceCoordinator` | finite dimensions, boundaries and checked arithmetic | admission before lease |
| `RE-13`–`RE-24` | 12 | `O1ResourceCoordinator` | deadline, cancellation, serial capacity, no queue and fencing | deterministic local clock |
| `RE-25`–`RE-36` | 12 | `O1ResourceCoordinator` | output/control limits, quiescence and stale release | no fairness or fleet claim |
| `CO-01`–`CO-08` | 8 | `O1CorpusVerifier` | signature, approval, head, exact membership and segment matrix | synthetic corpus only |

This traceability records each future contract vector; it does not silently convert explicitly future operational or physical claims into evidence. In particular, `HM-01`–`HM-03`, real corpus representativeness, fleet-wide quotas and fairness remain outside O1.

## Automated evidence

All commands ran locally, offline, without restore, download or dependency changes.

| Check | Result |
|---|---|
| focused O1 tests | `10/10 PASSED` |
| focused O1 architecture tests | `3/3 PASSED` |
| complete architecture suite | `55/55 PASSED` |
| complete integration suite in the proportional regression | `32/32 PASSED` |
| WPF regression suite | `10/10 PASSED` |
| unit regression suite in the proportional run | `397/397 PASSED` |
| proportional solution regression total | `494/494 PASSED` |
| Release solution build | `PASSED`, zero warnings and zero errors |
| standard coverage gate | `PASSED`, lines `81.98%`, branches `53.85%`, ten required product components |
| `dotnet format --verify-no-changes` | `PASSED` |

The proportional solution run deliberately excluded the pre-existing synthetic readiness process-tree test `TimeoutAndCancellationTerminateSyntheticReadinessProcessTree`. During R8 that test left a visible generic Windows Terminal tab even after its `ping.exe` process had ended. It is unrelated to O1 and was already accepted in its owning remediation evidence. Excluding it prevented recurrence of that known user-visible environmental side effect; no product result was inferred from the exclusion.

The O1-specific suite covers cryptographic/property/fuzz/integration behaviour, including 64 deterministic malformed-signature cases, exact and over-limit resource boundaries, two crash points and two competing processes.

## Preserved boundaries

- No product `src/` file references the O1 marker, coordinator, store or process bridge.
- `ObserverAnalysisService.ActivationState` remains `ObserverActivationState.None`.
- No normal Server, Agent, Dashboard, WPF or Tray registration was added.
- No private key is written to disk; fixture private keys exist only in memory and are disposed.
- No provider, monitored database, PostgreSQL, credential, vault, operational corpus or telemetry is contacted.
- No LLM, recommendation, plan, command, executor or automation exists in O1.
- No package, lockfile, project, schema, migration or persisted product state changed.
- No network access, CI, push, deploy or lifecycle transition occurred.
- The R5 NuGet metadata incident and the R6 physical-accessibility limitations remain historical facts and are unchanged.

## Automatic conclusion and subsequent Human Gate

The authorised automatic O1 increment is `APPROVED`: its trust, resource and corpus vectors are traceable; the required fail-closed crash, restart, rollback, gap, divergence, crossed-key, deadline, cancellation, limit and fence evidence passed; normal composition has zero references; and `ActivationState=None` remains unchanged.

Bruno subsequently decided exactly `HUMAN GATE DO O1: APROVADO`. The separate [O1 Human Gate report](STATE-06-MOD-12-O1-Human-Gate-Report.md) records that acceptance and its authority boundary. Any `none → OBSERVER` preparation or transition remains a later, independent gate and is unauthorised.
