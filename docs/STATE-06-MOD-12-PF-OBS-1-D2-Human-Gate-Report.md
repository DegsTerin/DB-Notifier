# PF-OBS-1-D2 — Human Gate Report

## Decision

- Decision date: 2026-07-25.
- Automatic report:
  [PF-OBS-1-D2 FirstByte Repeatability Diagnosis and Remediation](STATE-06-MOD-12-PF-OBS-1-D2-FirstByte-Repeatability-Report.md).
- Reviewed implementation commit: `28adf3a0f96debeb138c6b46868975c806a35b0e`.
- Automatic result: `APPROVED`.
- Human Gate result: `APPROVED`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.

Bruno made the exact decision:

> HUMAN GATE DO PF-OBS-1-D2: APROVADO

## Accepted scope

The decision accepts only the D2 diagnosis and frozen FirstByte v3 methodology:

- protocol version `pfobs1-physical-measurement-3.0.0`;
- canonical SHA-256
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`;
- absolute FirstByte latency retained separately from fixed-window repeatability;
- exactly `100,000` post-observation work units declared before results;
- every raw sample preserved without selection, removal, replacement or concealment;
- unchanged or stricter absolute, memory, CPU, work, security and cancellation limits;
- marker-gated, test-only implementation isolated from normal composition.

## Preserved limitations

- No physical v3 campaign has been executed.
- The retained v2 failure remains historical evidence and is not reclassified.
- PostgreSQL, pilot runtime, representative corpus and production behaviour remain untested by D2.
- PF-OBS-1 and O5 remain unapproved.
- No more specific host cause than the proven methodological instability was inferred.

## Authority boundary

This Human Gate:

- closes only PF-OBS-1-D2;
- does not execute or authorise a physical campaign;
- does not authorise PostgreSQL, provider, corpus or data access;
- does not activate Observer or change `ActivationState=None`;
- does not authorise LLMs, recommendations, commands or automation;
- does not authorise push, deploy or lifecycle transition.

## Next decision

A future physical campaign under the exact frozen v3 protocol requires a separate explicit
authorisation. Its automatic result and Human Gate remain independent of this approval.
