# PF-OBS-1-D3 — Cancellation/Cold Working-Set Diagnosis and Remediation

## Decision

PF-OBS-1-D3 is automatically `APPROVED` within its local, synthetic,
test-only scope.

The dominant avoidable allocation pressure in `Cancellation/Cold` was
identified and removed without changing the frozen physical protocol, any
limit, workload size, cancellation semantics or normal composition.

No physical campaign, PostgreSQL runtime or Observer runtime was executed.
`ActivationState=None` remains unchanged.

## Retained evidence diagnosis

The complete failed PF-OBS-1-V3 report retained at:

`artifacts/pf-obs-1/e4b39e453cf3486c93c6ed8682260c5e/hm-01-03-run-1.json`

has SHA-256:

`B12B8C096DE79F619B59A8F21EA3191A12FEEAE6439F729265B5CEA1AF80C1DE`.

The eighteen retained `Cancellation/Cold` samples show:

| Evidence | Value |
|---|---:|
| Cumulative managed allocation | `1,244,744 bytes` |
| Per-sample minimum | `66,360 bytes` |
| Per-sample maximum | `76,776 bytes` |
| Per-sample mean | `69,152.44 bytes` |
| Failed-sample working-set delta | `2,998,272 bytes` |
| Failed-sample managed-heap delta | `0 bytes` |
| Failed-sample cumulative allocation | `76,776 bytes` |
| Frozen working-set limit | `786,432 bytes` |

The former implementation cloned the complete `65,536`-byte input inside
every `Cancellation/Cold` sample. The array storage alone accounted for at
least `85.39%` and up to `98.79%` of each retained sample's managed
allocation. The failure occurred on the eighteenth retained invocation of
that path, after `1,244,744` cumulative bytes had been allocated by the
phase.

The retained allow-listed counters prove the repeated full-buffer allocation
and the process-wide working-set cliff. They do not identify whether the
exact `2,998,272` committed pages belonged to a GC segment, runtime
bookkeeping, a thread stack or another Windows process region. D3 does not
invent that unavailable sub-attribution.

## Focal remediation

`O5R5CancellationInputBuffer` now:

- allocates one exact `65,536`-byte cold buffer when the serial test-only
  workload catalogue is created;
- freshly copies all deterministic source bytes into that buffer for every
  Cold invocation;
- returns immutable warm bytes for Warm invocations;
- performs zero managed allocation across the frozen sequence of five
  warm-ups and thirty measured materialisations;
- remains valid only for the exact O1 maximum input length.

The cancellation operation still:

- captures its bounded resource checkpoint;
- creates the linked inner cancellation token;
- requests cancellation after the unchanged ten-millisecond interval;
- observes that cancellation without cancelling the owning runner;
- honours outer cancellation;
- runs under the existing single-sample, no-queue protocol.

No array pool, new dependency, new garbage collection, result selection,
source of authority or normal-composition reference was introduced.

## Preserved protocol

| Invariant | Preserved value |
|---|---|
| Protocol | `pfobs1-physical-measurement-3.0.0` |
| Protocol SHA-256 | `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A` |
| Input | `65,536 bytes` |
| Accounted memory | `524,288 bytes` |
| Empirical working-set limit | `786,432 bytes` inclusive |
| Cancellation elapsed limit | `3,000 ms` inclusive |
| Warm-ups and measured samples | `5 + 30` |
| Parallel samples | `1` |
| Queue | disabled |
| Required physical campaigns | `2` consecutive campaigns |

The logical input, copy work, inner cancellation and complete evidence shape
are unchanged. Reusing storage removes an implementation allocation defect;
it does not redefine the workload or methodology, so no protocol version or
digest change was made.

## Automatic evidence

| Check | Result |
|---|---|
| Focused integration tests | `18/18` passed |
| Focused architecture tests | `11/11` passed |
| Complete integration suite | `114/114` passed |
| Complete architecture suite | `90/90` passed |
| Release solution build | passed with zero warnings and errors |
| Cancellation/Cold materialisations | `35/35`, exact input, zero per-sample storage allocation |
| Cancellation checkpoints | `35/35`, exactly one per sample |
| Working-set boundary | `786,432` accepted; `786,433` rejected |
| D3 class coverage | `84.61%` lines; `66.66%` branches |
| Changed-file .NET format | passed |
| Physical marker | absent |
| Physical campaign | not executed |
| PostgreSQL or Observer runtime | not executed |
| Normal-composition reference | zero |
| Activation state | `None` |

The synthetic boundary tests remain non-authorising. They prove the unchanged
fail-closed gate and the absence of repeated input allocation; they do not
replace future physical working-set evidence.

## Cleanup and authority boundary

The final audit found no DB-Notifier-owned process, listener, physical
temporary root, coverage directory or new retained campaign artefact. Build
and test outputs are ordinary ignored artefacts.

D3 does not reclassify the failed PF-OBS-1-V3 campaign. Bruno subsequently
decided exactly `HUMAN GATE DO PF-OBS-1-D3: APROVADO`. The separate
[D3 Human Gate report](STATE-06-MOD-12-PF-OBS-1-D3-Human-Gate-Report.md)
records that acceptance and its authority boundary.

The decision closes only D3. A future physical campaign still requires a
separate explicit execution authorisation. PostgreSQL, the pilot pipeline,
corpus admission, Observer activation and lifecycle transition remain
prohibited.
