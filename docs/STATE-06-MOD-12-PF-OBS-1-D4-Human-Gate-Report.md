# PF-OBS-1-D4 — Human Gate Report

## Decision

- Decision date: 2026-07-25.
- Automatic report:
  [PF-OBS-1-D4 Residual Cancellation/Cold Working-Set Diagnosis](STATE-06-MOD-12-PF-OBS-1-D4-Residual-Working-Set-Report.md).
- Reviewed implementation commit: `1b4662e26313b62c47eb1035d4fbc8889c231a41`.
- Automatic result: `BLOCKED`.
- Human Gate result: `ACCEPTED AS BLOCKED`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.

Bruno made the exact decision:

> HUMAN GATE DO PF-OBS-1-D4: ACEITO COMO BLOQUEADO — reconheço que a causa
> do working set residual não foi comprovada, que nenhuma correção foi
> aplicada ao workload V3 e que nenhuma campanha física foi executada.
> Aceito o incidente de restore bloqueado registrado, sem reclassificá-lo
> como autorizado. Esta decisão encerra somente o D4 e não autoriza D5, nova
> campanha física, PostgreSQL, OBSERVER ou transição.

## Accepted disposition

The decision accepts the fail-closed D4 disposition rather than treating the
diagnostic as a technical approval:

- the historical `2,916,352`-byte process-wide growth was not reproduced;
- neither the existing `Task.Delay` path nor the wait-handle candidate proved
  ownership of the historical residual working set;
- no mechanism replacement or speculative workload remediation was applied;
- protocol `pfobs1-physical-measurement-3.0.0`, its authenticated digest,
  workloads, SLOs and the inclusive `786,432`-byte limit remain unchanged;
- no physical campaign was executed.

## Preserved incident and limitations

- The blocked restore performed internally by
  `verify-dotnet-lockfiles.ps1` remains outside the D4 authority.
- The repository diff proved zero dependency or lockfile change and the
  command reported no download, but possible NuGet metadata access cannot be
  excluded.
- The incident is not reclassified as authorised by this Human Gate.
- The failed post-D3 physical campaign remains historical evidence.
- PF-OBS-1 and O5 remain unapproved.

## Authority boundary

This Human Gate:

- closes only the human review of PF-OBS-1-D4 as `ACCEPTED AS BLOCKED`;
- does not approve the D4 technical completion criteria;
- does not authorise D5 or another diagnostic;
- does not authorise a physical campaign, PostgreSQL, provider, corpus or
  pilot runtime;
- does not activate Observer or change `ActivationState=None`;
- does not authorise push, deploy or lifecycle transition.

## Next decision

Any diagnostic that reproduces the exact process history preceding
`Cancellation/Cold` requires a separate explicit authorisation. Any future
physical campaign remains independently gated.
