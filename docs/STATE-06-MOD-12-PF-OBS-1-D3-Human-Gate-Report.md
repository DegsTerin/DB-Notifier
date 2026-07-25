# PF-OBS-1-D3 — Human Gate Report

## Decision

- Decision date: 2026-07-25.
- Automatic report:
  [PF-OBS-1-D3 Cancellation/Cold Working-Set Diagnosis and Remediation](STATE-06-MOD-12-PF-OBS-1-D3-Cancellation-Working-Set-Report.md).
- Reviewed implementation commit: `5fcc0da9b5b5cb9a0fcdea4fdc971fd3736bc75f`.
- Automatic result: `APPROVED`.
- Human Gate result: `APPROVED`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.

Bruno made the exact decision:

> HUMAN GATE DO PF-OBS-1-D3: APROVADO

## Accepted scope

The decision accepts only the D3 diagnosis and test-only remediation:

- the retained V3 evidence proved repeated allocation of the complete
  `65,536`-byte Cancellation/Cold input;
- one exact bounded buffer now receives a fresh complete copy for every
  serial Cold invocation;
- Warm continues to use the immutable deterministic source;
- cancellation, checkpoints, workload, sequence and fail-closed admission
  remain unchanged;
- protocol `pfobs1-physical-measurement-3.0.0`, SHA-256
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`
  and the inclusive `786,432`-byte working-set limit remain unchanged;
- the implementation remains isolated from normal composition.

## Preserved limitations

- The failed PF-OBS-1-V3 campaign remains historical evidence and is not
  reclassified.
- The retained counters do not identify ownership of the exact committed
  pages among GC, runtime, thread stack or another Windows process region.
- Synthetic tests prove bounded storage reuse and the unchanged gate; they
  do not replace a future physical campaign.
- PostgreSQL, the pilot pipeline, corpus admission and production behaviour
  remain untested by D3.
- PF-OBS-1 and O5 remain unapproved.

## Authority boundary

This Human Gate:

- closes only PF-OBS-1-D3;
- does not execute or authorise another physical campaign;
- does not authorise PostgreSQL, provider, corpus or data access;
- does not activate Observer or change `ActivationState=None`;
- does not authorise LLMs, recommendations, commands or automation;
- does not authorise push, deploy or lifecycle transition.

## Next decision

A future physical campaign under the exact frozen V3 protocol requires a
separate explicit execution authorisation. Its automatic result and Human
Gate remain independent of this approval.
