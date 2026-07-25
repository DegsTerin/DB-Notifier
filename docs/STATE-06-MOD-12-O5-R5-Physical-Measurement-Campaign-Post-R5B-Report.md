# O5-R5 — Physical HM-01–HM-03 Campaign after O5-R5-B

## Disposition

- Date: 2026-07-25.
- Driver baseline: `737dab6ae1667c41f41d09bc6954c827b0e07f57`.
- Frozen protocol version: `o5r5a-physical-measurement-1.0.0`.
- Frozen protocol SHA-256:
  `266B7A952DF1A46BEE4577894D0A9206D92917AC661E0E17F9052DE1EB415DD7`.
- Automatic campaign result: `FAILED`.
- `HM-01`: `BLOCKED` before a complete liveness matrix.
- `HM-02`: `FAILED`.
- `HM-03`: `NOT TESTED`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.

The physical campaign stopped at the first preregistered threshold failure, as required. No
threshold, workload, runner or protocol value was changed after observing the result.

## Environment

| Dimension | Sanitised observed value |
|---|---|
| Operating system | Microsoft Windows `10.0.26200`, x64 |
| Measurement process | x64 |
| .NET SDK | `10.0.301` |
| .NET runtime | `10.0.10` |
| Logical processors | `8` |
| Installed physical memory | `16,963,534,848 bytes` |
| Monotonic clock frequency | `10,000,000 ticks/second` |
| Initial process/listener state | zero project-owned process and listener |

The baseline was clean and exact before execution. The campaign used one isolated
`dotnet test` process, no restore and no external access.

## Stop condition

The driver completed `19` of the expected `560` samples:

- five `FirstByte/Cold` warm-ups;
- thirteen accepted `FirstByte/Cold` measured samples;
- one failed `FirstByte/Cold` measured sample at repetition `14`;
- zero complete phase/temperature summaries.

The failed sample recorded:

| Metric | Observed | Inclusive limit | Result |
|---|---:|---:|---|
| Elapsed time | `14.0726 ms` | `2,250 ms` | within limit |
| CPU time | `0 ms` | not a work-rate phase | factual only |
| Managed-heap peak delta | `0 bytes` | `786,432 bytes` | within limit |
| Working-set peak delta | `864,256 bytes` | `786,432 bytes` | **exceeded by 77,824 bytes** |
| Allocation peak | `70,592 bytes` | `786,432 bytes` | within limit |
| Cumulative allocation | `70,592 bytes` | factual only | factual only |

The stable failure code was `o5r5a.measurement.threshold_exceeded`. The working-set delta exceeded
the frozen empirical memory ceiling by approximately `9.9%`. This is a failed physical headroom
measurement, not a protocol or test infrastructure error.

Across the `19` retained partial samples, the observed ranges were:

| Metric | Minimum | Mean | Maximum |
|---|---:|---:|---:|
| Elapsed time | `6.3475 ms` | `8.2795 ms` | `15.3732 ms` |
| CPU time | `0 ms` | `6.5789 ms` | `15.625 ms` |
| Managed-heap peak delta | `0 bytes` | `0 bytes` | `0 bytes` |
| Working-set peak delta | `0 bytes` | `45,487.16 bytes` | `864,256 bytes` |
| Allocation peak | `70,296 bytes` | `70,594.95 bytes` | `70,944 bytes` |
| Cumulative allocation | `70,296 bytes` | `70,594.95 bytes` | `70,944 bytes` |

These partial ranges are diagnostic evidence only. They do not replace the exact 5/30 summaries
and cannot approve HM-01 or any unexecuted phase.

## HM classification

### HM-01 — liveness

`BLOCKED`. First-byte elapsed time remained inside its threshold for the retained samples, but the
required complete first-byte, idle, cancellation and control-update matrix was not executed after
the mandatory stop. No liveness approval or percentile summary is claimed.

### HM-02 — memory and allocation

`FAILED`. One measured first-byte/cold sample exceeded the frozen working-set peak-delta ceiling.
The fail-closed driver stopped immediately.

### HM-03 — deterministic compute cost

`NOT TESTED`. Parse, cryptography, sort and analysis were not reached. No CPU-per-work-unit or
elapsed-per-work-unit result is claimed.

## Evidence integrity and cleanup

- Temporary JSON evidence size: `12,628 bytes`.
- Temporary JSON evidence SHA-256:
  `90F5A06CD58655D537CD53B2D14F03F3D174FD8306305F0246DD3C0BC3956AF7`.
- The opt-in, output, SDK and installed-memory environment variables were absent after execution.
- The exact temporary evidence file and project-owned root were removed after documentary
  extraction.
- No physical result was retained as product data or published to Observer.
- No provider, database, corpus, credential, network, LLM, recommendation, command or automation
  was used.

## Preserved boundaries

- O5-R5-B remains automatically approved as an isolated driver; this campaign result does not
  rewrite its implementation evidence.
- `ActivationState=None` remains unchanged.
- Product source, normal composition, dependencies and lockfiles remain unchanged.
- No correction was attempted after the physical limit failed.
- O5-R6 remains outside the authorised scope and is not eligible while O5-R5 is failed.

## Next decision

O5-R5 is physically `FAILED`. The next eligible step is a separate proposal for analysing and
remediating the empirical working-set headroom failure. Repeating the campaign, changing the
threshold, changing the workload or proceeding to O5-R6 all require separate authorisation.
