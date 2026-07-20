# Relatório STATE-06 — Consolidated Revocation Finalisation Diagnostic and Harness Remediation

## Status e autoridade

- Data: 2026-07-20.
- Baseline anterior à implementação: `c03cba1`.
- Estado mantido: `STATE-06 INTEGRATION`.
- Autoridade: incremento restrito autorizado explicitamente por Bruno.
- Alterações técnicas: somente `tests/` e `scripts/`.
- Alterações sob `src/`, solução, projetos, packages, lockfiles e migrations: nenhuma.
- Classificação automática deste incremento: `APROVADO` com as limitações abaixo.
- Repetição da Campanha Consolidada no commit `66d0a9f`: continua historicamente `REPROVADA`.
- Human Gate próprio desta remediação: `ACEITO COM AS LIMITAÇÕES REGISTRADAS` por Bruno em 2026-07-20.
- Nova campanha, amostra humana, Human Gate final, promoção e transição: não autorizados e não executados.

## Resultado em linguagem simples

O laboratório agora informa exatamente em qual fronteira uma falha ocorreu e mostra apenas fatos permitidos, como status HTTP, estado, código, tentativa e número monotónico do fence. Ele nunca devolve texto de exceção, stack trace, caminho, token ou material de certificado.

A revogação deixou de ser uma única etapa ambígua. O harness comprova separadamente que:

1. o Server gravou o Agent e todos os seus certificados como revogados;
2. heartbeat e assignments foram negados diretamente pelo Server;
3. o Agent registrou a quarentena local depois da negação do heartbeat;
4. a tentativa posterior de assignments parou localmente e preservou o último snapshot válido;
5. observação e comando também continuaram negados;
6. nenhum `CommandAttempt` ou efeito operacional foi criado.

Os cenários `R1`–`R7` foram executados com ordem controlada, cancellation e budgets. Todos passaram. O harness completo também passou uma vez em Chrome dedicado, com o Dashboard TV ativo, polling autoritativo, SignalR apenas como hint e transporte de comando deliberadamente não executável.

A resposta histórica `503` da campanha anterior não continha detalhe suficiente para reconstruir retrospectivamente a exceção original. Por isso, este relatório não inventa essa causa. A matriz excluiu como defeitos atuais as ordens de revogação, leitura, saída de processo, relógio e lease previstas na proposta. Durante a remediação, o diagnóstico encontrou e reproduziu outro defeito exclusivamente de teste: pedidos sintéticos de comando usavam o relógio real enquanto o Server usava o relógio controlado. A correção passou a fornecer o mesmo instante controlado a ambos, sem alterar código de produto.

## Diagnóstico e correções diretas

### Granularidade e envelope sanitizado

O estágio agregado `finalising-revocation` foi substituído por fronteiras estáveis para request e commit de revogação, negações diretas de heartbeat/assignments, quarentena local, observação, comando e contagens duráveis. O último estágio fica congelado quando ocorre uma falha.

O endpoint test-only devolve somente uma categoria fechada e os campos permitidos. O auditor de browser aplica uma segunda allow-list antes de imprimir qualquer diagnóstico. Um teste negativo forneceu texto com aparência de token, caminho de PKCS#12 e thumbprint; nenhum desses valores apareceu na serialização.

### Relógio controlado

O coordinator do harness agora usa o mesmo `TimeProvider` ajustável do Server. A finalização não consulta mais `DateTimeOffset.UtcNow` para avançar o relógio. Os pedidos negativos de command poll passaram a receber explicitamente `sandbox.Now`.

Essa última correção foi determinada por evidência reproduzível: depois de remover o catch-up de parede, o estágio `finalising-command-gap-negative` retornou HTTP `400` em vez do `409` esperado porque o request criado com hora real estava no futuro em relação ao Server controlado. Após alinhar as fontes de tempo, a mesma fronteira e o harness completo passaram.

### Budget de prontidão

Uma execução não modificada anterior à remediação não publicou readiness dentro dos `30` segundos do runner, embora o processo ainda estivesse vivo e o cleanup tenha sido completo. Outra execução aquecida passou. O runner agora usa um budget explícito de `90` segundos medido por `Stopwatch`; não repete o host, não tenta até passar e continua subordinado ao budget de `15` minutos do processo.

## Matriz R1–R7 observada

| Cenário | Ordem comprovada | Resultado |
|---|---|---|
| `R1` | revogação sem snapshot concorrente | commit central, negação direta e quarentena local aprovados |
| `R2` | snapshot autoritativo concluído antes da revogação | snapshot factual e revogação posterior aprovados |
| `R3` | resposta de snapshot mantida em barrier enquanto a revogação commita | ambas concluíram sem deadlock ou perda de estado |
| `R4` | revogação antes da próxima leitura e publicação do hint | API continuou autoritativa; hint permaneceu best-effort |
| `R5` | revogação depois da saída confirmada do subprocesso de comando | processo encerrou; comando permaneceu não executável |
| `R6` | avanço explícito do relógio controlado antes da revogação | nenhuma consulta ao relógio real foi usada como barrier |
| `R7` | lease antigo ocupado, expirado e substituído por fence maior | contender foi recusado; owner antigo não removeu o fence novo |

Cada cenário teve budget de `30` segundos; o conjunto teve budget de `4` minutos. Os sete terminaram em aproximadamente `18` segundos na execução focada e em aproximadamente `12` segundos dentro da suíte final. Não houve retry genérico, `Sleep` como barrier, relaxamento de asserção ou aumento de timeout para esconder falha terminal.

## Sequência E2E observada

1. O runner comprovou ausência de resíduo e iniciou apenas o host HTTPS loopback e Chrome dedicado.
2. O mesmo Agent sintético foi inscrito, recebeu assignment read-only, enviou heartbeat e persistiu replay no Agent SQLite.
3. O Dashboard TV fez leitura imediata; SignalR funcionou somente como hint e a reconciliação autoritativa independente ocorreu após `30` segundos com concorrência máxima `1`.
4. Uma transição reconciliada produziu uma entrega no sink de teste e não foi duplicada após reinício.
5. O transporte sintético de comando produziu `2` entradas de journal e `0` `CommandAttempt`.
6. O último subprocesso de comando saiu antes da revogação.
7. A revogação retornou `Revoked`; o Server mostrou o Agent e `1/1` certificado em estado `Revoked`.
8. Heartbeat e assignments diretos foram recusados pelo Server.
9. O heartbeat Agent-side alcançou o transporte, recebeu negação e persistiu `RevokedOrDenied` sob fence `3`.
10. Assignments Agent-side parou localmente, sem novo transporte, preservou o last-known-good e liberou o fence `4`.
11. Observação e command poll diretos continuaram negados; o child de comando também falhou fechado.
12. O resultado terminal manteve `2` observações, `1` entrega, `2` journals, `0` attempt, `0` origem HTTP externa e `operationalData=false`.
13. Host, Chrome, perfil, listener, Agent SQLite e diretórios temporários próprios foram encerrados ou removidos.

Resumo sanitizado emitido:

```json
{"result":"passed","correlatedRun":true,"observationSamples":2,"notificationDeliveries":1,"commandJournalEntries":2,"commandAttempts":0,"revokedCertificateCount":1,"heartbeatFenceAfterRevocation":3,"assignmentsFenceAfterRevocation":4,"maximumSnapshotConcurrency":1,"observedExternalHttpRequests":0,"operationalData":false}
```

## Verificação

| Verificação | Resultado observado |
|---|---|
| shutdown/cleanup antes e depois dos runs | zero processo, listener, browser dedicado ou root temporário pertencente ao projeto |
| build Release da solução | `17` projetos; `0` erro; `0` warning |
| testes unitários | `332/332` |
| testes de arquitetura | `30/30` |
| testes de integração | `16/16` |
| matriz R1–R7 e sanitização focadas | `2/2` métodos; sete cenários aprovados |
| cobertura .NET | linhas `78,9%`; branches `49,51%` |
| formatação | aprovada após correção mecânica de indentação no teste novo |
| Dashboard | toolchain aprovado; TypeScript aprovado; `60/60` testes; build aprovado |
| harness correlacionado remediado | aprovado em Chrome `150.0.7871.125` |
| origem HTTP externa no harness | `0` |
| command safety | `2` journals; `0` `CommandAttempt` |

As verificações documentais, links, secrets e diff final são registradas no commit desta entrega depois da inclusão deste relatório. Nenhum restore, download ou acesso externo foi executado.

## Achados classificados

### Corrigido — alta precisão diagnóstica insuficiente

O estágio único impossibilitava localizar a falha. A granularidade, o contexto tipado, a allow-list dupla e o teste negativo corrigem o problema sem expor dados sensíveis.

### Corrigido — relógios test-only divergentes

O uso combinado de relógio real e controlado foi reproduzido com HTTP `400` numa fronteira que deveria testar gap e retornar `409`. Todos os pedidos sintéticos envolvidos agora recebem explicitamente o relógio do sandbox.

### Corrigido — readiness frio menor que o trabalho observado

O limite de `30` segundos falhou uma vez antes do readiness. Ele foi substituído por um budget explícito de `90` segundos, sem retry de processo e com cleanup obrigatório.

### Sem achado alto ou crítico aberto no escopo da remediação

R1–R7, harness correlacionado, isolamento e gates aplicáveis passaram. Isso não apaga nem reclassifica a falha histórica da campanha.

## Limitações e condições residuais

- A exceção interna histórica de `finalising-revocation` não pode ser recuperada do payload sanitizado antigo; nenhuma causa retrospectiva foi inventada.
- A matriz exclui os interleavings especificados como defeitos atuais do harness remediado, mas não prova concorrência operacional futura.
- Server SQLite em memória possui comportamento diferente de PostgreSQL.
- Agent SQLite, identidades, certificados, assignments, observações e comandos são exclusivamente sintéticos e efêmeros.
- O barrier de `R3` mantém a resposta HTTP depois da leitura autoritativa; ele prova sobreposição de requests, não um lock de leitura equivalente a PostgreSQL.
- SignalR continua hint descartável; API e polling permanecem fontes autoritativas.
- O sink não prova apresentação visível de notificação Windows.
- O transporte não executa comandos e não prova executor, post-probe ou controle administrativo.
- Um único Chrome local não constitui homologação de browser, sistema operacional ou hardware.
- Esta execução é Quality Gate próprio da remediação, não repetição da Campanha Consolidada.
- A campanha do commit `66d0a9f` permanece `REPROVADA` até nova campanha completa, separadamente autorizada.
- Human Gate final, amostra humana, runtime operacional, promoção e transição continuam não autorizados.

## Classificação dos gates

| Gate | Resultado |
|---|---|
| escopo exclusivo `tests/`/`scripts/` e documentação factual | `APROVADO` |
| alteração sob `src/`, solução, projetos, packages, lockfiles ou migrations | nenhuma |
| diagnóstico granular e sanitizado | `APROVADO` |
| revogação central separada de negação e quarentena | `APROVADO` |
| matriz determinística `R1`–`R7` | `APROVADO` |
| fencing, relógio, budgets e cancelamento | `APROVADO` |
| comando deliberadamente não executável | `APROVADO` |
| harness correlacionado remediado | `APROVADO` |
| cleanup e ausência de acesso externo | `APROVADO` |
| **Quality Gate próprio da remediação** | **`APROVADO`** |
| Campanha Consolidada repetida no commit `66d0a9f` | `REPROVADA` historicamente; não reclassificada |
| Human Gate próprio da remediação | `ACEITO COM AS LIMITAÇÕES REGISTRADAS` por Bruno em 2026-07-20 |
| nova campanha, amostra humana e Human Gate final | `NÃO AUTORIZADOS` |
| promoção/transição | `NÃO AUTORIZADAS` |

## Decisão humana registrada

Depois de solicitar e receber a leitura direta deste relatório, principalmente de `Resultado em linguagem simples`, `Sequência E2E observada`, `Limitações e condições residuais` e `Classificação dos gates`, Bruno declarou em 2026-07-20:

> Remediação STATE-06 Consolidated Revocation Finalisation Diagnostic and Harness, commit f9bb567: ACEITA COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo nova Campanha Consolidada, amostra humana, Human Gate final, runtime operacional, promoção nem transição de estado.

Essa decisão encerra somente o Human Gate próprio da remediação e aceita suas limitações documentadas. O `STATE-06 INTEGRATION` permanece inalterado; a repetição da Campanha Consolidada continua `REPROVADA`, e nova campanha, amostra humana, Human Gate final, runtime operacional, promoção e transição continuam não autorizados.
