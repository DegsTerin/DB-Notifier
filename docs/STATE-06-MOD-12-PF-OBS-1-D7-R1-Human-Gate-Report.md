# PF-OBS-1-D7-R1 — Human Gate Report

## Decision

- Decision date: 2026-07-28.
- Automatic report:
  [PF-OBS-1-D7-R1 Summary-Count Contract Rerun](STATE-06-MOD-12-PF-OBS-1-D7-R1-Summary-Count-Contract-Rerun-Report.md).
- Reviewed implementation commit:
  `deef15122cd42989f87debbfdb2a8789bb056279`.
- Automatic result: `D7-R1.NOT_REPRODUCED`.
- Human Gate result: `APPROVED WITH RESERVATIONS`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.

Bruno made the exact decision:

> HUMAN GATE DO PF-OBS-1-D7-R1: APROVADO COM RESSALVAS — revisei o relatório
> automático PF-OBS-1-D7-R1 e aceito a classificação
> D7-R1.NOT_REPRODUCED. Nenhuma amostra humana adicional foi prevista pelo
> protocolo. Reconheço que D7 e D6 permanecem historicamente bloqueados, que
> nenhuma causa foi atribuída e que PF-OBS-1, O5, Observer, ActivationState e
> lifecycle não foram aprovados.

## Accepted scope and reservations

The decision accepts the automatic `D7-R1.NOT_REPRODUCED` classification for
the corrective R1 lot:

- three new admissible R1-U reports each retained `35/35` samples and `1/1`
  summary;
- all three V3 `FirstByte/Cold` repeatability coefficients remained below
  the unchanged inclusive `0.20` limit;
- the exact failure count was `0/3`;
- the conditional external R1-E arm therefore remained prohibited; and
- no additional human sample was specified by the D7-R1 protocol.

The reservations preserve the historical and causal limits of that result.
D7 and D6 remain blocked, and the valid R1 lot neither repairs those records
nor attributes a cause to the earlier D6 failure.

## Authority boundary

This Human Gate:

- closes only the human review of PF-OBS-1-D7-R1 as
  `APPROVED WITH RESERVATIONS`;
- accepts non-recurrence only for the three admissible R1-U processes;
- does not reclassify, replace or unblock D7 or D6;
- does not approve PF-OBS-1 or the O5 Quality Gate;
- does not authorise another diagnostic, D6 continuation, D5 control or the
  HM-01–HM-03 campaign;
- does not activate Observer or change `ActivationState=None`;
- does not authorise a lifecycle transition, PostgreSQL, provider or
  operational data; and
- does not authorise runtime execution, deployment, publication, push or pull
  request.

The registration of this decision changes documentation only. It does not
change source, tests, runtime, executable configuration, dependencies or
retained D7/D7-R1 evidence.

## Registration validation

- The source-documentation gate passed for `427` comment-capable files.
- The Markdown link gate passed for `824` local links in `212` files.
- The current non-ignored worktree and available Git history passed the
  secret scan.
- `git diff --check` passed.
- Product build, tests and runtime were not repeated because this
  registration changes documentation only.

## Resulting state

PF-OBS-1-D7-R1 is technically complete as `D7-R1.NOT_REPRODUCED` and its
Human Gate is closed as `APPROVED WITH RESERVATIONS`.

D7 and D6 remain historically blocked. PF-OBS-1 and O5 remain blocked.
`STATE-06 INTEGRATION` and `ActivationState=None` remain unchanged.

Any further diagnosis, physical campaign, PF-OBS-1 continuation, Observer
activation or lifecycle transition requires separate explicit authority and
its applicable gates.
