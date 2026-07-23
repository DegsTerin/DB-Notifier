# STATE-06 Audit Remediation — R7-A0 Inactive MOD-12 Foundation Report

## Decision summary

- Authorised baseline: `ee16f302e703ccad7574c77cb374cd0f44905508`
- Findings: `AUD-H13` and `AUD-M11`
- Automatic result: `APPROVED`
- Human acceptance: `PENDING`
- MOD-12 capability: `observer-analysis`
- MOD-12 activation state: `None`
- O1, runtime and lifecycle transition: `NOT AUTHORISED`

R7-A0 corrects only the already inactive MOD-12 Application foundation. It creates no trust host, persistence, corpus, telemetry source, provider, database, API, UI, worker or operational composition.

## Finding disposition

| Finding | Correction | Current disposition |
|---|---|---|
| `AUD-H13` | Absolute deadlines are revalidated after every bounded phase and immediately before publication. Cancellation, expiry and stale context revisions return typed, empty, non-authorising reports. | `ADDRESSED` in the inactive foundation |
| `AUD-M11` | Capability and activation are separate; probe duration series retain their outcome; incomplete offline runs publish no subset results or metrics; telemetry sources are bounded, cancellation-aware and materialised before analysis; source exceptions are sanitised; corpus provenance is explicitly declarative only. | `ADDRESSED` in the inactive foundation |

This disposition does not close O1 requirements. Durable trust continuity, signed corpus heads, checkpointing, host coordination, persistence and operational admission remain future and unauthorised.

## Focal corrections

### Capability without mode activation

`ObserverCapabilityProfile` no longer declares `Mode=OBSERVER`. It now exposes:

- `Capability=observer-analysis`;
- `ActivationState=None`;
- immutable false declarations for collection, persistence, LLM, recommendation, planning and execution.

The analysis report schema is versioned as `aiops.observer.analysis.v2` and always reports `IsAuthorising=false`.

### Complete-only publication

One explicit execution context binds:

- an exclusive absolute UTC deadline;
- the revision admitted with the evidence;
- the revision current at publication;
- the clock used only for deterministic deadline checks.

The service checks cancellation and deadline before work, after every analyser and immediately before publication. It checks the exact context revision at the publication boundary. Any cancellation, expiry or stale revision returns an empty typed report; completed subset findings are discarded.

The offline evaluator applies the same complete-only rule. Cancelled or expired processing publishes:

- no case results;
- no segment aggregates;
- no precision or recall;
- no favourable partial result;
- `IsAuthorising=false`.

### Outcome-preserving probe duration

The canonical adapter no longer mixes durations from unlike health outcomes in one series. It derives bounded provider-neutral keys such as:

- `database.probe.duration.healthy`;
- `database.probe.duration.degraded`;
- `database.probe.duration.timeout`;
- corresponding keys for the remaining canonical outcomes.

The transformation version is now `observer-duration-by-outcome.v2`. Provider-native details, errors and arbitrary labels still do not cross the adapter boundary.

### Offline admission and provenance truth

After aggregate budget admission and before any case analysis, every telemetry source is copied into an immutable bounded list. Enumeration:

- checks cancellation before and during work;
- stops at the existing hard sample bound;
- detects count or content drift;
- converts untrusted source exceptions into `aiops.observer.offline.telemetry_source_unavailable`;
- never reflects exception text or source content.

Corpus provenance and authority references remain useful declarations, but the dataset and report now classify their authority as `DeclaredOnly`. No signed manifest, authenticated corpus head or trust claim is implied.

## Synthetic evidence

The focal tests independently cover:

- capability `observer-analysis` with activation `None`;
- cancellation before analysis;
- absolute deadline expiry after a completed analyser phase;
- stale publication revision;
- suppression of threshold, forecast, case, segment, precision and recall subsets;
- distinct healthy, degraded and timeout duration series;
- source exception sanitisation;
- cancellation during source enumeration;
- enumeration beyond the declared/hard bound;
- existing independent ECDSA P-256 grant/revocation signatures, crossed identity/material refusal, tampering, revocation, rollback and checkpoint vectors;
- architecture allowlisting of the exact public MOD-12 surface.

All evidence is synthetic, local and in-memory.

## Verification

All commands ran locally, offline and without restore or dependency resolution.

| Check | Result |
|---|---|
| focused MOD-12 tests | `77/77 PASSED` |
| complete unit suite | `399/399 PASSED` |
| complete architecture suite | `52/52 PASSED` |
| WPF tests | `10/10 PASSED` |
| integration tests | `22/22 PASSED` |
| complete solution tests | `483/483 PASSED` |
| Release solution build | `PASSED`, zero warnings and zero errors |
| coverage gate | `PASSED`, lines `81.98%`, branches `53.85%`, ten required components present |
| `dotnet format --verify-no-changes` | `PASSED` |
| code-documentation gate | `PASSED`, 329 comment-capable files |
| Markdown-link gate | `PASSED`, 637 local links in 143 files |
| secret scan | `PASSED`, non-ignored worktree and available Git history |
| dependency/lockfile diff | `PASSED`, zero changed dependency or lock files |
| UI diff | `PASSED`, zero Dashboard, WPF, Tray or Presentation files changed |
| normal-composition reference gate | `PASSED`, zero references outside the owning AIOps Application namespace |
| final cleanup | `PASSED`, zero project-owned process, listener or coverage temporary root |

An initial attempt to run two focused builds concurrently caused one compiler process to lose the shared Application intermediate output file lock. Both gates were immediately repeated sequentially and passed. This was local build contention, not a product, test or runtime failure.

## Preserved boundaries

- No dependency, package, lockfile, restore, installation, download or external access was used.
- No runtime, trust host, API, worker, listener, persistence, corpus file, telemetry source, provider or database was created.
- No `OBSERVER` activation, O1, LLM, recommendation, command, automation or lifecycle transition occurred.
- No UI, Dashboard, WPF or Tray product surface changed.
- R0-F1 remains accepted and the local global .NET gate remains green.
- The R5 NuGet metadata incident remains recorded and unchanged.

## Next decision

The next permissible step is a separate human decision to accept or reject R7-A0 in this bounded inactive-foundation scope. Acceptance would not authorise O1, `none → OBSERVER`, R8, operational AIOps or lifecycle transition.
