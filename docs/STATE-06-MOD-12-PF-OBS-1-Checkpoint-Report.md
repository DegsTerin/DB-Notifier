# PF-OBS-1 — Functional PostgreSQL and Observer Pilot Checkpoint

## Disposition

- Date: 2026-07-25.
- Authorised baseline: `5f425af4d89fd6112ffb3a620ce35e2e12f9851c`.
- Campaign result: `BLOCKED` at the physical repeatability gate.
- PostgreSQL laboratory: implementation prepared; full live campaign not reached.
- Human sample: not eligible.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.

This checkpoint records implemented and automatically verified campaign infrastructure without
claiming that PF-OBS-1, HM-01–HM-03, the PostgreSQL pilot or the Observer activation gate passed.

## Implemented scope

- A marker-gated PostgreSQL 16 collector accepts only an ephemeral `localhost` connection with
  hostname-verified TLS, pooling disabled and bounded command deadlines.
- The laboratory corpus contract defines exactly 36 measurements split into immutable,
  disjoint development, calibration and holdout partitions.
- The policy is content-addressed and frozen before holdout evaluation.
- A complete accepted observation can traverse the synthetic Agent → Server → O1 → MOD-12
  path and produce a non-authorising O4 projection.
- The O4 sandbox can render that projection while the normal composition retains zero
  activation authority.
- A project-owned PowerShell runner defines exact `Campaign`, `Start` and `Stop` actions,
  pinned PostgreSQL image identity, loopback-only networking, runtime-generated synthetic
  credentials, TLS material and exact cleanup boundaries.
- The physical driver can run in the existing dedicated sandbox host rather than inside the
  xUnit process.

The PostgreSQL project reference and resulting lockfile entries reuse the repository's existing
`Npgsql 10.0.3` resolution. No package version or external dependency was added.

## Automatic verification

| Gate | Observed result |
|---|---|
| Dedicated sandbox host Release build | passed, zero warnings and zero errors |
| Focused integration tests | `17/17` passed |
| Focused architecture tests | `4/4` passed |
| PowerShell syntax | passed |
| Code-documentation gate | passed for `378` comment-capable files |
| Worktree and history secret scan | passed |
| External download | none; the pinned PostgreSQL 16 image was already local |
| Activation | remained `None` |

## Physical stop condition

The physical protocol remains version `o5r5a-physical-measurement-1.0.0`, digest
`266B7A952DF1A46BEE4577894D0A9206D92917AC661E0E17F9052DE1EB415DD7`, with a maximum
elapsed-time coefficient of variation of `0.20`.

Dedicated executions passed the absolute per-sample limits but did not produce a reproducible
complete matrix. Successive runs stopped at different summary batches, including:

- `ControlUpdate/Cold`, coefficient of variation approximately `0.27`;
- `Idle/Warm`, coefficient of variation approximately `0.25`;
- `Sort/Cold`, coefficient of variation approximately `0.204`;
- `Cryptography/Cold`, coefficient of variation approximately `0.201`;
- a diagnostic high-priority process still failed `Sort/Warm`.

The failures therefore cannot be treated as a single xUnit-host artefact or selected away. The
campaign stopped before the live PostgreSQL pipeline, calibration, holdout, resilience campaign
and human sample. No result was promoted to evidence of production representativeness.

## Cleanup and restrictions

- Zero PF-OBS-1 container, network or volume remained.
- Zero project-owned listener or runtime remained.
- Zero PF-OBS-1 or O5-R5 physical temporary directory remained.
- No browser or human sample was opened.
- No push, deploy, Observer activation or lifecycle transition occurred.

The current source changes are a resumable local checkpoint, not an accepted Quality Gate.
Changing the frozen physical methodology requires an explicit decision that preserves absolute
SLOs and prevents favourable-run selection before any further physical campaign is executed.
