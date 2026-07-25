# PF-OBS-1 v2 — Resumption Report

## Decision

- Date: 25 July 2026.
- Authorised baseline: `b151f9fff273f1005f22955ca405c3b98ed51cea`.
- Frozen-methodology commit executed: `8adf34416a1a7bbcaf316b753c907c6aba749cd6`.
- Lifecycle: `STATE-06 INTEGRATION`.
- Activation state: `None`.
- Automatic result: `FAILED`.
- Human sample: not eligible and not opened.

The scheduler-robust protocol was implemented, validated and committed before any resumed physical
measurement. The first physical campaign then failed an unchanged absolute threshold and the
macrocampaign stopped immediately. No replacement run, PostgreSQL pipeline exercise, calibration,
holdout, resilience campaign or visible Observer sample followed.

## Frozen methodology

Protocol `pfobs1-physical-measurement-2.0.0`, SHA-256
`53F40F7DC72548EB488FFF729823BF0DCFD64EDD085314022C8E746CC45B5D71`, retains all thirty measured
samples, every raw distribution and every absolute gate. Relative repeatability uses five fixed
sequential groups of six and the median of each group. Two complete consecutive passing campaigns
were required and a failed run could not be replaced.

Before physical execution:

- `10/10` focused integration tests passed;
- `8/8` focused architecture tests passed;
- the PowerShell campaign parsed successfully;
- code-documentation, Markdown-link and secret gates passed;
- the worktree was clean and no owned runtime or Docker resource existed.

## Physical stop

The dedicated process built successfully with zero warnings and zero errors. Physical campaign
one then returned the stable diagnostic:

```text
o5r5a.measurement.threshold_exceeded
```

This code proves that one complete physical sample exceeded at least one unchanged absolute gate.
It does not identify the phase or metric. The temporary detailed report was removed by the
existing fail-safe cleanup before it was copied to the pilot evidence root, so the exact phase,
sample and dimension are `NOT IDENTIFIED`. They must not be inferred from an earlier campaign.

Because this was an absolute-threshold failure:

- campaign one is `FAILED`;
- campaign two is `NOT RUN`;
- reproducibility is `NOT PROVED`;
- HM-01, HM-02 and HM-03 are `INCOMPLETE`;
- the PostgreSQL laboratory validation, live read-only pipeline, lab corpus, calibration, holdout,
  load, security, restart, offline, kill-switch and rollback sequence are `NOT RUN`;
- the human sample and final PF-OBS-1 Human Gate are `NOT ELIGIBLE`.

## Cleanup and preserved boundaries

Post-stop inspection found:

- zero PF-OBS-1 state file;
- zero owned process;
- zero owned container, network or volume;
- zero PF-OBS-1 or O5-R5 physical temporary root;
- a clean worktree before this factual report.

No image was downloaded. No real data, production credential, external service, LLM,
recommendation, command, administrative automation, push, deploy, Observer activation or
lifecycle transition occurred. `ActivationState=None` remains unchanged.

## Required next decision

No further physical execution is authorised by this result. A future proposal may address only
fail-closed retention of the detailed failed report and phase-specific sanitised diagnostics so a
new, separately authorised physical campaign can identify the failing absolute dimension. It must
not weaken the frozen SLO or select a favourable rerun.
