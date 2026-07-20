# Proposta STATE-06 — Diagnóstico e remediação determinística de `finalising-revocation`

> Execução posterior: Bruno autorizou separadamente o incremento restrito em 2026-07-20. A implementação e o Quality Gate próprios estão registrados no [relatório factual da remediação](STATE-06-Consolidated-Revocation-Finalisation-Diagnostic-And-Harness-Remediation-Report.md). O texto abaixo permanece como delimitação histórica da proposta e não autoriza nova campanha, amostra humana, promoção ou transição.

## Status e autoridade

- Data: 2026-07-20.
- Estado mantido: `STATE-06 INTEGRATION`.
- Baseline técnica examinada: commit `66d0a9fa661f9ec05c365aececa7e10cf6903c8b`.
- Baseline documental corrente: commit `c50eef5c2410ec3a0ffef0ff441c5c2db70c3cda`.
- Quality Gate consolidado repetido: `REPROVADO`.
- Achado proprietário: falha `ALTA` no estágio agregado `finalising-revocation`.
- Autoridade desta atividade: exclusivamente elaborar esta proposta documental.
- Implementação, build, testes, runtime, browser, acesso externo, nova campanha, amostra humana, promoção e transição: não autorizados e não executados.

Esta proposta não corrige a falha e não altera sua classificação. Ela delimita um futuro incremento de diagnóstico e remediação somente do harness. Qualquer execução dependerá de autorização posterior, separada e explícita.

## Resultado em linguagem simples

O teste completo falhou quando chegou à revogação do Agent, mas o nome atual da etapa é amplo demais. Dentro de `finalising-revocation`, o programa faz várias coisas: grava a revogação, testa heartbeat, testa assignments e verifica os resultados. Como todas compartilham o mesmo rótulo, o relatório sabe onde a sequência estava, mas não sabe qual operação falhou.

Corrigir diretamente agora seria arriscado: poderíamos adicionar uma espera ou repetição que apenas escondesse uma corrida. A proposta adota uma ordem mais segura:

1. separar cada fronteira da etapa final;
2. registrar somente códigos e estados sanitizados, nunca exceções, caminhos ou material de identidade;
3. reproduzir interleavings controlados entre revogação, leitura do Dashboard e stores SQLite;
4. corrigir apenas se a causa estiver no harness de teste;
5. parar e pedir nova autorização se a evidência apontar defeito em código de produto sob `src/`.

Para uma pessoa não técnica: primeiro o laboratório ganhará “marcadores de etapa” mais precisos. Depois ele provocará, de forma controlada, as ordens possíveis dos eventos. Só então será permitido ajustar o próprio laboratório. Não haverá tentativa genérica automática para transformar uma falha intermitente em passe.

## Baseline factual da inspeção direta

Os quatro artefatos técnicos diretamente envolvidos possuem o mesmo blob no commit `66d0a9f` e no `HEAD` documental `c50eef5`:

- `tests/DBNotifier.IntegrationTests/AgentFleetApiEndToEndTests.ConsolidatedHarness.cs`;
- `tests/DBNotifier.IntegrationTests/AgentFleetApiEndToEndTests.cs`;
- `scripts/run-state06-consolidated-e2e.ps1`;
- `scripts/audit-state06-consolidated-e2e.mjs`.

Não existe mudança técnica posterior à baseline que explique a divergência entre o passe da remediação e a falha da campanha repetida.

A inspeção do código mostrou:

1. `FinaliseAsync` define `finalising-revocation` antes de chamar a revogação e conserva esse rótulo durante revogação, heartbeat, assignments e duas asserções;
2. a revogação humana usa endpoint real do sandbox, transação serializável e atualiza atomicamente o Agent e seus certificados;
3. a autenticação do certificado consulta novamente Agent e certificado ativos no Server store a cada request autenticado;
4. um heartbeat negado grava `RevokedOrDenied` no Agent SQLite;
5. a tentativa de assignments executada depois disso pode falhar localmente, antes do transporte, porque a identidade já está indisponível;
6. cada operação Agent-side adquire e libera um lease local com fence, e os helpers exigem uma tentativa e um fence;
7. os processos filhos do transporte de comando são aguardados até a saída antes da revogação;
8. o browser ainda está em modo TV e pode realizar leituras autoritativas periódicas enquanto a finalização escreve no Server SQLite compartilhado;
9. a resposta `503` expõe somente o código geral e o estágio, por desenho sanitizado;
10. os testes isolados de revogação passaram, mas não exercem necessariamente a mesma concorrência e o mesmo histórico da composição completa.

Esses fatos delimitam o problema, mas não provam a causa.

## Problema a resolver

O futuro incremento deverá responder, com evidência reproduzível e sem dados sensíveis:

- a revogação foi negada, entrou em conflito, não persistiu ou persistiu corretamente?
- Agent e certificados ficaram `Revoked` no mesmo commit?
- o heartbeat chegou ao Server e foi negado, ou falhou antes do transporte?
- assignments foram negados pelo Server ou recusados localmente após o heartbeat?
- algum lease/fence Agent SQLite permaneceu ocupado depois dos processos de comando?
- uma leitura concorrente do Dashboard/notification projection interferiu na escrita SQLite?
- o avanço do relógio controlado alterou validade, autorização ou lease?
- alguma asserção do harness descreve de forma imprecisa um comportamento que é fail-closed, mas local?

O incremento não poderá declarar a causa sem responder essas perguntas por estados tipados, barriers determinísticos ou inspeção durável.

## Hipóteses documentais

| ID | Hipótese | Evidência atual | Como distinguir futuramente |
|---|---|---|---|
| `H1` | contenção entre a escrita de revogação e leitura concorrente no Server SQLite | browser permanece ativo; falha foi intermitente entre duas execuções históricas | barrier test-only antes/durante/depois do commit e outcome tipado da revogação |
| `H2` | endpoint retornou disposition diferente de `Revoked` | helper exige exatamente `Revoked`; estágio não separa HTTP, JSON e asserção | registrar status, disposition e error code sanitizados antes da asserção |
| `H3` | lease/fence Agent SQLite não estava disponível após subprocessos | heartbeat/assignments exigem lease; composição completa usa o mesmo Agent SQLite | registrar aquisição, fence e liberação por operação; provocar ownership controlado |
| `H4` | relógio do Server, coordinator ou lease divergiu após o catch-up de parede | Server usa relógio ajustável; coordinator conserva relógio fixo inicial | registrar offsets limitados e testar instantes controlados sem usar horário real como sincronização |
| `H5` | heartbeat negado corretamente e assignments recusados localmente, mas a evidência confunde as duas fronteiras | `RevokedOrDenied` impede transporte posterior de assignments | provar separadamente negação direta do endpoint e quarentena local/LKG preservado |
| `H6` | falha ambiental não determinística sem defeito lógico | apenas uma falha e um passe históricos | não aceitar a hipótese por descarte; exigir matriz determinística e repetição limitada sem retry mascarador |

As hipóteses não são conclusões nem autorização para escolher uma correção.

## Arquitetura proposta

### 1. Estágios granulares e imutáveis

Substituir o único rótulo agregado por fronteiras test-only exatas, por exemplo:

- `finalising-revocation-request`;
- `finalising-revocation-commit-verification`;
- `finalising-heartbeat-server-denial`;
- `finalising-heartbeat-local-quarantine`;
- `finalising-assignments-server-denial`;
- `finalising-assignments-local-quarantine`;
- `finalising-observation-denial`;
- `finalising-command-denial`;
- `finalising-durable-counts`.

O estágio avançará apenas depois de a fronteira anterior produzir evidência válida. Em falha, o último estágio ficará congelado.

### 2. Envelope sanitizado de falha

Estender somente o contrato interno do harness para expor:

- estágio e boundary estáveis;
- status HTTP quando existir;
- disposition e error code tipados já sanitizados;
- número de tentativas;
- presença e valor monotónico não secreto do fence;
- estados canônicos de Agent, certificados e identidade local;
- indicação booleana de transporte tentado;
- contagens limitadas de linhas/evidências esperadas;
- categoria fechada como `persistence-conflict`, `transport-denied`, `local-quarantine`, `lease-busy`, `assertion-mismatch` ou `unexpected`.

Permanecem proibidos exception message/stack trace no payload, caminhos locais, porta completa, headers, token, senha, certificado, chave, PKCS#12, subject, thumbprint ou payload operacional. Logs detalhados, se necessários, ficarão somente em memória do processo e serão reduzidos à categoria antes de sair do boundary.

### 3. Prova separada de revogação server-side e fail-closed local

Depois do commit de revogação, o harness deverá comprovar separadamente:

1. Agent central em `Revoked` e `RevokedAt` preenchido;
2. todos os certificados limitados do Agent em `Revoked` com instante monotónico;
3. request direto de heartbeat negado pelo Server;
4. request direto de assignments negado pelo Server;
5. coordinator Agent persiste ou conserva `RevokedOrDenied`;
6. tentativa Agent-side de assignments recusa localmente sem substituir o last-known-good;
7. observação e comando continuam negados;
8. zero `CommandAttempt` e zero efeito operacional.

Isso evita usar uma quarentena local correta como se fosse prova de que o endpoint de assignments foi realmente consultado.

### 4. Scheduler determinístico de interleavings

Usar barriers/gates exclusivamente de teste, sem `Sleep` como sincronização, para executar ao menos estas ordens:

| Cenário | Ordem controlada |
|---|---|
| `R1` | revogação sem leitura concorrente |
| `R2` | snapshot inicia e conclui antes do commit de revogação |
| `R3` | snapshot é mantido no barrier enquanto a revogação tenta commitar |
| `R4` | revogação commita antes da próxima leitura e do hint |
| `R5` | revogação imediatamente após saída confirmada do último subprocesso de comando |
| `R6` | revogação após avanço controlado do relógio, sem consultar `UtcNow` como barrier |
| `R7` | lease Agent intencionalmente ocupado e depois liberado sob fence mais novo |

Cada cenário terá deadline, cancelamento e cleanup próprios. Uma falha esperada por contenção deverá ter outcome explícito; deadlock, timeout silencioso ou retry ilimitado reprovarão o incremento.

### 5. Árvore de decisão da correção

O futuro incremento poderá corrigir, na mesma autorização, apenas causas comprovadas dentro de `tests/` ou `scripts/`:

- granularidade/ordem incorreta da evidência;
- barrier ou fencing do harness;
- liberação/espera de processo pertencente ao harness;
- uso inconsistente dos relógios controlados do harness;
- distinção entre negação server-side e quarentena local;
- serialização estritamente test-only necessária para o SQLite sintético, desde que a concorrência relevante continue testada em cenário próprio.

Não será permitido adicionar retry genérico ao runner, repetir até passar, aumentar timeout sem causa, sair do modo TV para evitar a concorrência, remover uma asserção de segurança ou trocar falha por sucesso.

Se a causa exigir alteração em `src/`, persistência produtiva, autenticação normal, contrato público, migration, package, solução ou projeto, o incremento deverá parar como `BLOQUEADO`, registrar a evidência e solicitar uma remediação separada.

## Escopo futuro proposto

Uma eventual implementação ficará limitada a:

- `tests/DBNotifier.IntegrationTests/AgentFleetApiEndToEndTests.ConsolidatedHarness.cs`;
- testes proprietários existentes em `tests/DBNotifier.IntegrationTests/`;
- `scripts/audit-state06-consolidated-e2e.mjs`;
- `scripts/run-state06-consolidated-e2e.ps1` somente se necessário para transportar evidência sanitizada e cleanup;
- testes de arquitetura/segurança que provem isolamento e ausência de segredo;
- documentação e registros factuais correspondentes.

Nenhum novo projeto, package, dependency, lockfile, migration, fonte externa ou mudança de solução é necessário ou permitido.

## Testes futuros obrigatórios

| Área | Evidência mínima |
|---|---|
| diagnóstico | cada boundary produz estágio/categoria tipada e sanitizada |
| segredo | testes negativos rejeitam ou suprimem exception, path, header, token e material de certificado |
| revogação central | Agent e todos os certificados commitados como revogados antes dos probes |
| negação direta | heartbeat, assignments, observação e comando recusados pelo Server |
| quarentena local | estado `RevokedOrDenied`, LKG preservado e nenhuma nova mutação autoritativa |
| concorrência | `R1`–`R7` sob barriers determinísticos, deadline e cancelamento |
| fencing | owner antigo não grava; owner corrente libera lease; nenhum lease residual |
| relógio | Server, Agent e lease usam instantes explicitamente controlados |
| command safety | journal esperado, zero `CommandAttempt`, zero executor/efeito |
| composição | browser/TV, API/polling autoritativos e SignalR apenas hint permanecem ativos |
| regressão | harness correlacionado completo e E2E proprietários passam sem retry até sucesso |
| isolamento | nenhuma referência de `src/` ao harness e composição normal inalterada |
| cleanup | zero processo, listener, profile, pipe, store ou root pertencente ao run |

A validação do incremento poderá executar o harness remediado e os testes aplicáveis, mas essa execução será evidência da remediação, não uma nova Campanha Consolidada.

## Critérios de aceite da remediação futura

O futuro incremento somente poderá ser `APROVADO` se:

1. a causa for reproduzida ou excluída por matriz determinística, sem atribuição especulativa;
2. toda falha terminal indicar boundary e categoria sanitizados;
3. revogação central, negação direta e quarentena local forem evidências distintas;
4. o mesmo Agent, stores e run continuarem correlacionados;
5. nenhum retry genérico, relaxamento de asserção ou timeout ampliado mascarar a falha;
6. os cenários `R1`–`R7` terminarem dentro dos budgets e sem deadlock;
7. o harness completo passar em execução limpa e única depois da correção;
8. browser TV continuar ativo, API/polling autoritativos, SignalR apenas hint e concorrência de snapshot máxima `1`;
9. transporte de comando continuar deliberadamente não executável, com zero `CommandAttempt`;
10. nenhuma mudança ocorrer sob `src/`, solução, projetos, packages, lockfiles ou migrations;
11. composição normal continuar desabilitada e sem referência ao harness;
12. zero acesso externo, recurso operacional, notificação visível ou dado real for usado;
13. todos os processos e temporários próprios forem encerrados e removidos;
14. relatório factual distinguir observado, inferido, não testado e bloqueado;
15. não houver achado crítico ou alto aberto dentro do escopo da remediação.

Mesmo com aprovação, o Quality Gate consolidado repetido continuará historicamente `REPROVADO`. Reclassificá-lo exigirá outra campanha completa e separadamente autorizada.

## Classificação futura

- `APROVADO`: causa delimitada, correção do harness comprovada e todos os critérios atendidos;
- `REPROVADO`: comportamento test-only ainda viola revogação, isolamento, determinismo ou segurança;
- `BLOQUEADO`: a causa exige mudança em `src/`, nova dependência, acesso, autoridade ou ambiente fora do escopo;
- `NÃO APLICÁVEL`: apenas para verificação realmente externa, com justificativa.

## Fora de escopo absoluto

- alteração em qualquer arquivo sob `src/`;
- alteração de solução, projeto, package, lockfile, migration ou schema;
- acesso externo, download, registry, CDN, restore externo ou nova dependência;
- runtime operacional, composição normal, serviço permanente ou deploy;
- recurso, identidade, certificado, credencial, provider, banco, IdP, PKI, vault ou canal operacional;
- PostgreSQL real ou database monitorado;
- comando administrativo, `CommandAttempt`, executor, shell, post-probe, `Start`, `Stop` ou `Restart`;
- notificação Windows visível, e-mail, SMS, webhook, Teams, Slack ou canal externo;
- nova UI ou API administrativa;
- LLM, recomendação, planejamento, automação ou promoção `none → OBSERVER`;
- nova Campanha Consolidada, amostra humana, Human Gate final, `STATE-07`, promoção ou transição.

## Riscos e limitações residuais

- A causa permanece desconhecida até uma execução futura instrumentada; esta proposta não a resolve por inferência.
- Instrumentação excessiva pode alterar timing. Por isso, os markers devem ser in-memory, bounded e testados com barriers explícitos.
- Server SQLite em memória possui locking diferente de PostgreSQL. Uma correção test-only não prova concorrência operacional futura.
- Separar request direto e quarentena local aumenta a precisão, mas não transforma o sandbox em Agent operacional.
- Um passe depois da correção não apaga a falha histórica; a campanha deverá ser repetida integralmente.
- O browser local único não constitui homologação ampla.
- O sink de teste não prova apresentação de notificação pelo Windows.
- A auditoria offline não comprova advisories atuais.
- Se o problema estiver em autenticação, persistência ou contrato de produto, esta remediação deverá parar em vez de ampliar silenciosamente o escopo.

## Condições de parada

O futuro incremento deverá parar se:

- a causa exigir qualquer alteração sob `src/`;
- a reprodução exigir rede, download, package, migration ou recurso não sintético;
- a evidência sanitizada não puder distinguir as fronteiras sem expor segredo;
- surgir `CommandAttempt`, executor, processo/serviço afetado ou canal externo;
- uma correção depender de retry até passar, sleep não determinístico ou remoção de asserção;
- um processo, listener, profile, pipe, store ou diretório não puder ser atribuído e removido;
- a worktree contiver mudança preexistente conflitante;
- o budget for excedido ou cleanup ficar incompleto.

## Entregáveis futuros

- granularidade de estágio e envelope sanitizado do harness;
- matriz determinística `R1`–`R7` e testes negativos;
- correção mínima somente em `tests/`/`scripts/`, se a causa pertencer a essas fronteiras;
- relatório factual próprio com causa observada, diff e limitações;
- atualização de estado/histórico e commit local focado;
- nenhum código de produto, dependência, migration, runtime operacional ou transição.

## Decisão futura de Bruno

Se Bruno concordar com esta proposta e desejar autorizar somente o incremento restrito, o texto sugerido é:

> AUTORIZO o incremento restrito de remediação do STATE-06 — Consolidated Revocation Finalisation Diagnostic and Harness Remediation, limitado à granularidade de estágios e evidência sanitizada no harness correlacionado, prova separada da revogação central, negação server-side e quarentena Agent local, barriers determinísticos para os cenários R1–R7, fencing, relógio controlado, budgets, cancelamento, testes e correção mínima exclusivamente sob `tests/` e `scripts/` quando a causa for comprovadamente test-only. Autorizo runtimes temporários exclusivamente locais e Chrome dedicado com perfil efêmero, que deverão ser encerrados e removidos ao final. Se a causa exigir alteração sob `src/`, solução, projetos, packages, lockfiles ou migrations, o incremento deverá parar como BLOQUEADO e solicitar nova autorização. Permanecem proibidos acesso externo, downloads, recursos ou credenciais operacionais, provider/banco real, composição normal, comandos administrativos, CommandAttempt, executor, shell, canal externo, notificação Windows visível, LLM, deploy, nova Campanha Consolidada, amostra humana, Human Gate final, promoção e transição de estado.

Essa autorização futura liberaria somente diagnóstico, correção test-only comprovada e relatório da remediação. A nova Campanha Consolidada continuaria exigindo autorização posterior, separada e explícita.

## Próximo passo para Bruno

1. Abra esta proposta e leia `Resultado em linguagem simples`, `Baseline factual da inspeção direta`, `Arquitetura proposta`, `Critérios de aceite da remediação futura`, `Fora de escopo absoluto` e `Riscos e limitações residuais`.
2. Se discordar, informe somente os limites ou critérios que deseja alterar.
3. Se concordar e quiser iniciar o incremento, copie exatamente o texto da seção `Decisão futura de Bruno`.
4. Não autorize junto a nova Campanha Consolidada, amostra humana, Human Gate, promoção ou transição.

Enquanto a proposta estiver em revisão, nenhuma ação técnica adicional é necessária.
