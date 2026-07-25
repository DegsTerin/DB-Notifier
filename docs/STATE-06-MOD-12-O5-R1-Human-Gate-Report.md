# MOD-12 O5-R1 — Human Gate Report

## Decision

- Decision date: 2026-07-24
- Reviewed report:
  [Pilot Scope and Observer Data Governance Report](STATE-06-MOD-12-O5-R1-Pilot-Scope-And-Data-Governance-Report.md)
- Reviewed documentation commit: `649df1691158e36e721e9cacd447acd7414735f2`
- Documentary result: `COMPLETED WITH PENDING DECISIONS`
- Human Gate result: `APPROVED WITH RESERVATIONS`
- Lifecycle: `STATE-06 INTEGRATION` unchanged
- MOD-12 activation: `ActivationState=None`

Bruno made the exact decision:

> HUMAN GATE O5-R1: APROVADO COM RESSALVAS — aceito a célula OBS-PILOT-PG16-LOCAL-001 como escopo
> candidato exclusivo do futuro laboratório Observer, limitada ao artefacto PostgreSQL documentado,
> single-primary local, loopback-only, Agent Windows e sinais read-only definidos no relatório.
>
> Aceito a governança, retenção, partições, thresholds e critérios de parada propostos. Reconheço que isso não
> constitui homologação, suporte público, representatividade de produção ou autorização para laboratório, corpus ou
> runtime.
>
> Os responsáveis por dados, governança, segurança, incidentes e homologação permanecem pendentes de nomeação e
> bloqueiam O5-R6 e O5-R8. A versão semântica, TLS e credencial sintética deverão ser comprovados somente em lotes
> posteriormente autorizados.
>
> Esta decisão encerra apenas o O5-R1 e permite somente apresentar a proposta do O5-R2. Não autoriza implementação,
> dados/provider/banco real, OBSERVER, push, deploy ou transição. ActivationState=None permanece inalterado.

## Accepted scope

The decision accepts only the documentary pilot direction:

- candidate cell `OBS-PILOT-PG16-LOCAL-001`;
- the previously documented immutable PostgreSQL 16 Alpine artefact;
- a future disposable single-primary local laboratory;
- loopback-only reachability from a Windows Agent;
- the exact read-only signal set defined by O5-R1;
- the proposed data minimisation, retention, withdrawal and partition rules;
- the proposed controlled-matrix representativeness boundary;
- the proposed label, integrity and holdout thresholds;
- the stop criteria and additional-authority matrix.

The candidate cell is now the exclusive scope that a future O5-R2 proposal may reference. This acceptance is not
provider selection for public support and is not homologation.

## Reservations and open decisions

The following reservations remain blocking:

- the corpus is not production-representative and no corpus exists;
- no provider or database laboratory was started;
- the exact semantic PostgreSQL server version has not been observed and bound;
- synthetic TLS and monitoring credential arrangements have not been proved;
- data owner, data-governance approver and corpus-attestation owner remain unnamed;
- security, incident and provider-homologation owners remain unnamed;
- physical `HM-01`–`HM-03` limits remain unmeasured;
- forecasts, capacity prediction and production prevalence remain outside the candidate cell;
- PostgreSQL homologation remains `None` and public support remains `No`.

The missing owners continue to block O5-R6 and O5-R8. Approval of O5-R1 does not silently assign any person to a
role or waive separation of duties.

## Authority boundary

This Human Gate:

- closes only the documentary O5-R1 decision;
- allows only a later proposal for O5-R2;
- does not authorise O5-R2 implementation;
- does not authorise code, configuration, tests or product runtime;
- does not authorise a laboratory, corpus, provider, database, credential or certificate;
- does not authorise external access, push or deploy;
- does not activate `OBSERVER`;
- preserves `ActivationState=None`;
- does not change lifecycle state;
- does not authorise LLMs, recommendations, commands, automation or execution.

The separately authorised registration changed documentation only. No source, executable configuration, dependency
or runtime was changed.

## Next decision

The next eligible action is a concise O5-R2 authorisation proposal. O5-R2 must remain provider-neutral and inactive,
must preserve `ActivationState=None`, and requires separate authority before any implementation. O5-R6 and O5-R8
remain blocked until the material owners are named and their prerequisites are separately accepted.
