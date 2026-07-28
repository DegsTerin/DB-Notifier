# PF-OBS-1-D8 — Human Gate Report

## Decision

- Decision date: 2026-07-28.
- Automatic report:
  [PF-OBS-1-D8 Exact-Prefix Completion Reconciliation](STATE-06-MOD-12-PF-OBS-1-D8-Exact-Prefix-Completion-Reconciliation-Report.md).
- Reviewed final automatic-report commit:
  `f7fe56196b97dda6b378a1cf56babf7168b480c2`.
- Reviewed automatic-report bytes: `10,792`.
- Reviewed automatic-report SHA-256:
  `64A21CF0CA314C3228632C1AE075C13B1E900C094D537355B1E025A77E024046`.
- Automatic result: `D8.EARLY_GATE_INTERMITTENT`.
- Human Gate result: `APPROVED WITH RESERVATIONS`.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.

Bruno made the exact decision:

> HUMAN GATE DO PF-OBS-1-D8: APROVADO COM RESSALVAS — revisei o relatório
> automático PF-OBS-1-D8 e aceito a classificação
> D8.EARLY_GATE_INTERMITTENT. Reconheço que a recorrência do working set não
> foi classificada, nenhuma causa foi atribuída e PF-OBS-1, O5, Observer,
> ActivationState e lifecycle não foram aprovados.

## Accepted scope and reservations

The decision accepts the automatic `D8.EARLY_GATE_INTERMITTENT`
classification for the two fixed D8-U attempts:

- run 1 was admissible and target-eligible, retaining an exact
  `Cancellation/Cold` working-set stop at `150/158` samples and `4/4`
  summaries;
- run 2 was admissible but stopped at the exact `FirstByte/Cold`
  repeatability summary with `35/158` samples and `1/4` summary;
- the frozen tree therefore classified the pre-target gate as intermittent;
  and
- the working-set recurrence branch was not entered because only one of the
  two attempts was target-eligible.

The reservations preserve every causal and lifecycle limit. The result does
not prove whether the working-set excess is repeated, intermittent or absent.
It does not identify a scheduler, runtime, garbage collector, hardware,
workload or host-load cause for either observed failure.

## Authority boundary

This Human Gate:

- closes only the human review of PF-OBS-1-D8 as
  `APPROVED WITH RESERVATIONS`;
- accepts only the automatic `D8.EARLY_GATE_INTERMITTENT` classification;
- does not reclassify D5, D6, D7 or D7-R1;
- does not classify working-set recurrence;
- does not approve PF-OBS-1 or the O5 Quality Gate;
- does not authorise another diagnostic, external observation, D5 control or
  the HM-01–HM-03 campaign;
- does not activate Observer or change `ActivationState=None`;
- does not authorise a lifecycle transition, PostgreSQL, provider or
  operational data; and
- does not authorise runtime execution, deployment, publication, push or pull
  request.

The registration of this decision changes documentation only. It does not
change source, tests, runtime, executable configuration, dependencies or
retained predecessor/D8 evidence.

## Registration validation

- Mandatory shutdown preflight found zero verified DB-Notifier process and
  zero owned listener.
- The reviewed automatic report matched its recorded byte count and SHA-256.
- The source-documentation gate passed for 429 comment-capable files.
- The Markdown-link gate passed for 843 local links in 217 files.
- The current non-ignored worktree and available Git history passed the
  secret scan.
- `git diff --check` passed.
- Product build, tests and runtime are not repeated because this registration
  changes documentation only.

## Resulting state

PF-OBS-1-D8 is technically complete as `D8.EARLY_GATE_INTERMITTENT` and its
Human Gate is closed as `APPROVED WITH RESERVATIONS`.

Working-set recurrence remains unclassified and no cause is attributed.
PF-OBS-1 and O5 remain blocked. `STATE-06 INTEGRATION` and
`ActivationState=None` remain unchanged.

The next step, if further investigation is desired, is a separately
authorised documentary D9 proposal limited to the intermittent
`FirstByte/Cold` gate observed in D8. It must preserve V3, all historical
evidence and thresholds, and must define its diagnostic question,
admissibility, stop rules and decision tree before any implementation or
physical execution.
