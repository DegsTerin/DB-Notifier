# O5-R5-A — Physical Measurement Readiness and Test-only Runner Report

## Disposition

- Date: 2026-07-25.
- Authorised baseline: `160cfdcdf63a425b1b16033fd557b90727b3de88`.
- Automatic result: `APPROVED`.
- Physical O5-R5 campaign: not executed.
- `HM-01`: `NOT TESTED`.
- `HM-02`: `NOT TESTED`.
- `HM-03`: `NOT TESTED`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.
- Normal-composition evaluation and publication: zero.
- Provider, database, corpus, credential, external data and operational runtime: not used.

O5-R5-A removes the readiness blockers recorded by the earlier blocked O5-R5 attempt. It freezes
the protocol before results and supplies one marker-gated runner whose automatic validation uses
only synthetic counters. This approval proves measurement readiness; it does not provide physical
performance evidence, approve headroom or resume O5-R5.

## Frozen protocol

The normative protocol is
[O5-R5-A — Frozen Physical Measurement Protocol](STATE-06-MOD-12-O5-R5A-Physical-Measurement-Protocol.md).

| Property | Frozen value |
|---|---:|
| Version | `o5r5a-physical-measurement-1.0.0` |
| SHA-256 | `266B7A952DF1A46BEE4577894D0A9206D92917AC661E0E17F9052DE1EB415DD7` |
| Warm-up samples per phase/temperature | `5` |
| Measured samples per phase/temperature | `30` |
| Percentiles | P50, P95, P99 and maximum |
| Maximum coefficient of variation | `0.20` |
| Maximum checkpoints | `64` |
| Empirical memory headroom | `50%`; `786,432 bytes` at the maximum reservation |
| First-byte/idle observation threshold | `2,250 ms` |
| Cancellation/control-update threshold | `3,000 ms` |
| Compute absolute deadline | `10,000 ms` |
| Elapsed and CPU work cost | `100 microseconds per deterministic work unit` |

The canonical representation is invariant-culture, line-oriented UTF-8. The digest was calculated
and recorded before any synthetic runner test was executed. Warm-ups remain in evidence but never
enter distributions; measured outliers cannot be removed.

## Runner boundary

The single runner is compiled only in `DBNotifier.IntegrationTests` and requires the exact marker
`DBNOTIFIER_O5_R5_A_TEST_ONLY`. It:

- admits one sample at a time and has no queue;
- labels first-byte, idle, cancellation, control-update, parse, cryptography, sort and analysis;
- accepts predeclared deterministic work and accounted-memory reservations only inside the O1
  envelope;
- captures monotonic elapsed time, managed heap, cumulative allocations, peak checkpoint
  allocation, working set and process CPU time;
- returns complete, sanitised, in-memory samples only after every counter boundary is valid;
- refuses marker/protocol mismatch, saturation, cancellation, checkpoint exhaustion, resource
  excess, counter rollback, missing counters and malformed sample batches;
- exposes immutable non-authorising outcomes and has no evaluation or publication capability.

The future physical source is present but was not instantiated. Its implementation uses only
`Stopwatch`, `GC.GetGCMemoryInfo`, `GC.GetTotalAllocatedBytes` and the current process
`WorkingSet64`/`TotalProcessorTime`. No profiler package, process launch, file sink, network client
or dependency was added.

## Synthetic evidence

| Check | Result |
|---|---|
| Exact protocol, envelope, thresholds and digest | `APPROVED` |
| All eight phase labels | `APPROVED` |
| Elapsed, memory and work limits at N / N+1 | `APPROVED` |
| Deterministic work admission at maximum−1 / maximum / maximum+1 | `APPROVED` |
| Cancellation before complete sample | `APPROVED` fail-closed |
| Concurrent admission while occupied | `APPROVED` fail-closed |
| Checkpoint 64 / 65 boundary | `APPROVED` fail-closed |
| Counter rollback and source failure | `APPROVED` fail-closed |
| Marker and protocol mismatch | `APPROVED` fail-closed |
| Exact 5/30 partition membership and no outlier removal | `APPROVED` |
| Variation above `0.20` | `APPROVED` fail-closed |
| Normal source and composition isolation | `APPROVED` |
| Physical source absent from automatic tests | `APPROVED` |

No automatic test creates `O5R5DotNetMeasurementSource`; all counter values are deterministic
fixtures. Therefore the numbers above are protocol-boundary evidence, not host measurements.

## Validation

| Validation | Observed result |
|---|---|
| Focused O5-R5-A integration tests | `7/7` passed |
| Focused O5-R5-A architecture tests | `6/6` passed |
| Complete integration suite | `98/98` passed |
| Complete architecture suite | `85/85` passed |
| Complete unit suite | `401/401` passed |
| .NET 10 Release solution build | passed; zero warnings and zero errors |
| .NET format/analyser verification | passed |
| Lockfile gate | passed for `18` projects; dependency files and worktree state unchanged |
| Code-documentation gate | passed |
| Markdown-link gate | passed |
| Secret scan | passed |
| Git staged-diff check | passed |

The lockfile gate invokes `dotnet restore --locked-mode` internally. It reported no package
download and left all dependency files and repository state unchanged. This invocation did not
change the runner, protocol or dependency graph.

## Isolation and cleanup

- Normal source contains zero O5-R5-A marker, runner, source or diagnostic reference.
- `ObserverActivationState` still contains only `None`.
- Normal Server composition remains dormant with an unavailable activation authority.
- No package, lockfile, product source, configuration, UI or operational contract changed.
- No DB Notifier runtime, listener, physical workload, profiler, trace or campaign artefact was
  left running or retained.
- Final cleanup found seven orphaned MSBuild worker nodes created by the locked restore. Their
  executable and command line were verified against the workspace-local .NET 10 SDK before only
  those exact PIDs were stopped; the repeated audit then reported zero owned process and listener.

## Limits and next decision

O5-R5-A is automatically complete, but its Human Gate is pending. `HM-01`, `HM-02` and `HM-03`
remain `NOT TESTED`; no physical headroom, host reproducibility or production suitability has been
proved.

The next eligible decision is a separate Human Gate O5-R5-A. If accepted, a new authorisation may
repeat the physical O5-R5 campaign against this exact version and digest. O5-R6, data/provider/
database access, Observer activation, deployment and lifecycle transition remain unauthorised.
