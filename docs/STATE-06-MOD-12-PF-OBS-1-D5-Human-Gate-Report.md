# PF-OBS-1-D5 — Human Gate Report

## Decision

- Decision date: 2026-07-25.
- Automatic report:
  [PF-OBS-1-D5 Process-History Diagnostic](STATE-06-MOD-12-PF-OBS-1-D5-Process-History-Diagnostic-Report.md).
- Reviewed implementation commit:
  `413f6ec684a0f7156173bc8f9d070310b0c0324c`.
- Automatic result: `BLOCKED`.
- Human Gate result: `ACCEPTED AS BLOCKED`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.

Bruno made the exact decision:

> HUMAN GATE DO PF-OBS-1-D5: ACEITO COMO BLOQUEADO — reconheço que o excesso
> histórico de working set não foi reproduzido nas duas execuções do prefixo
> V3 exato, que nenhuma causa foi atribuída e que nenhuma correção ou
> campanha física foi executada. Esta decisão encerra somente o D5 e não
> autoriza novo diagnóstico, campanha física, PostgreSQL, OBSERVER ou
> transição.

## Accepted disposition

The decision accepts the fail-closed D5 disposition without converting it
into technical approval:

- all eight fresh-process comparisons completed and retained `704/704`
  samples;
- the two exact-prefix runs reached only `12,288 bytes` of per-sample
  Cancellation/Cold working-set delta;
- no control exceeded `176,128 bytes`;
- the historical `2,916,352`-byte excess was not reproduced;
- no phase, resource or memory-region category received causal attribution;
- no speculative correction or physical campaign was executed.

## Preserved boundaries

- The two existing V3 preconditioning `GC.Collect` calls remain unchanged.
- Protocol `pfobs1-physical-measurement-3.0.0`, its digest, workloads, SLOs
  and the inclusive `786,432`-byte limit remain unchanged.
- PF-OBS-1 and O5 remain unapproved.
- No new diagnostic, physical campaign, PostgreSQL laboratory, provider,
  corpus or Observer runtime is authorised.
- `ActivationState=None` remains immutable.

## Authority boundary

This Human Gate:

- closes only the human review of PF-OBS-1-D5 as `ACCEPTED AS BLOCKED`;
- does not approve the D5 technical completion criteria;
- does not authorise implementation, correction or another diagnostic;
- does not authorise HM-01–HM-03, PostgreSQL or PF-OBS-1 continuation;
- does not authorise push, deploy, Observer activation or lifecycle
  transition.

## Next decision

Any further diagnosis, methodology change or physical campaign requires a
new explicit authorisation. This Human Gate does not select the next
technical approach.
