# Relatório STATE-06 — Consolidated E2E Evidence Harness and Deterministic Gate Remediation

## Status e autoridade

- Data: 2026-07-19.
- Estado mantido: `STATE-06 INTEGRATION`.
- Baseline de implementação: commit `131704d14a3d9f95a6f420fa13a990939946ad90`.
- Entrega: commit local que contém este relatório.
- Autoridade: remediação local e isolada autorizada textualmente por Bruno.
- Classificação automática deste incremento: `APROVADO`.
- Human Gate próprio desta remediação: `ACEITO COM AS LIMITAÇÕES REGISTRADAS` por Bruno em 2026-07-19.
- Campanha Consolidada histórica: continua `BLOQUEADA`; não foi repetida.
- Human Gate final, promoção e transição: não abertos, não autorizados e não inferidos.

## Resultado em linguagem simples

O bloqueio técnico que impedia uma prova única foi remediado. Antes, quatro laboratórios separados provavam partes diferentes. Agora existe um quinto programa somente de teste que liga as partes já aceitas em uma única execução local: o mesmo Agent sintético produz a observação; a mesma API a grava; o Dashboard a consulta; o SignalR apenas avisa que há mudança; a notificação lê essa mesma transição; e o transporte de comando usa a mesma identidade do Agent sem executar comando algum.

Esse programa não faz parte do DB-Notifier normal. Ele exige uma frase de ativação exata, fica dentro de `tests/`, usa apenas SQLite temporário e identidades fictícias, abre somente HTTPS loopback e encerra todos os processos ao final. O Server API e o Agent Worker normais continuam sem registrar o harness.

Os dois avisos do Entity Framework também foram eliminados com ordenação por chaves estáveis antes dos limites. Os runners modernos agora exigem PowerShell 7 de forma explícita; Windows PowerShell 5.1 os recusa antes de criar diretório, processo, listener ou perfil.

## Implementação factual

- novo `DBNotifier.State06.ConsolidatedSandboxHost` sob `tests/`, com referência apenas ao harness de integração existente e sem `PackageReference`;
- uma única execução correlacionada por GUID público de teste;
- um Agent SQLite em arquivo temporário e um Server SQLite compartilhado em memória;
- certificado P-256/mTLS exclusivamente para o Agent e identidade humana read-only sintética em listener HTTPS loopback separado, dentro da mesma aplicação e do mesmo Server SQLite;
- pipeline de observação com offline, resposta perdida, replay idempotente e projeção autoritativa;
- Dashboard TV em navegador dedicado, leitura imediata, hint SignalR autenticado, ETag/304, cadência real de 30 segundos, offline/recovery, fencing e concorrência máxima `1`;
- consumidor de notificação com baseline silenciosa, cursor/ledger local, uma entrega no sink em memória e deduplicação após reinício;
- transporte de comando v2 com incompatibilidade, gap, perda de resposta de poll/ack, replay, journal durável e política `Never`;
- revogação do mesmo Agent seguida de recusa de heartbeat, assignments, observação e comando;
- runner/auditor local sem pacote novo, download ou origem externa;
- inclusão mínima do 17º projeto na solução e na fixture NuGet sintética positiva;
- ordenação EF de certificados por `AgentCertificateId` e de scopes por `RoleAssignmentId`, `RoleId` e `PermissionId` antes de `Take`;
- `#Requires -Version 7.0` nos dois runners modernos de browser.

## Sequência E2E observada

1. O host criou material P-256 de teste, Agent/Server SQLite efêmeros e identidades Agent/humana separadas.
2. O Agent foi inscrito, recebeu assignment read-only e enviou heartbeat.
3. Uma observação `Degraded` ficou pendente offline e foi repetida por outro processo; o Server a aceitou uma vez.
4. O Dashboard normal permaneceu na demonstração e não leu o snapshot integrado antes da entrada no modo TV.
5. Ao entrar no modo TV, o navegador leu imediatamente a mesma projeção e exibiu `Degraded`; uma conexão SignalR autenticada foi observada.
6. Uma observação `Unavailable` sofreu perda da resposta aceita, foi repetida e permaneceu uma única segunda amostra no Server.
7. O consumidor local entregou exatamente uma transição ao sink em memória; o reinício com o mesmo ledger não a duplicou.
8. O hint SignalR disparou releitura HTTPS condicional; a API, não o hint, continuou autoritativa.
9. A reconciliação independente ocorreu após o prazo real de 30 segundos, retornou `304`, manteve ETag e não sobrepôs requests.
10. O navegador preservou o último snapshot durante offline local, recuperou por API e descartou trabalho de sessão encerrada.
11. O transporte de comando recusou versão e gap inválidos, sobreviveu à perda de respostas de poll/ack e concluiu replay entre processos.
12. Após revogação, heartbeat, assignments, observação e comando foram negados; a projeção manteve `2` amostras.
13. A inspeção final encontrou `2` registros de journal, `0` `CommandAttempt` e uma única entrega no sink.
14. O auditor registrou `0` origem HTTP externa; o runner encerrou host/browser e removeu perfil e roots temporários novos.

Resumo sanitizado emitido pelo auditor:

```json
{"result":"passed","correlatedRun":true,"observationSamples":2,"notificationDeliveries":1,"commandJournalEntries":2,"commandAttempts":0,"maximumSnapshotConcurrency":1,"observedExternalHttpRequests":0,"operationalData":false}
```

## Correções determinísticas menores

As duas consultas EF apontadas pela campanha agora aplicam ordenação total e estável antes do limite. O build e as suítes que exercitam enrollment, RBAC, revogação e catálogo terminaram sem warning, inclusive sem o `10102` anteriormente observado. Não houve mudança de schema, migration, autorização ou conteúdo retornado.

Windows PowerShell `5.1.26100.8875` recusou o runner consolidado com `ScriptRequiresUnmatchedPSVersion`, exit code `1`, sem alterar o número de roots temporários e com zero processo pertencente ao harness. PowerShell `7.6.3` executou o E2E e o cleanup completos. O runner legado não foi alterado.

## Verificação

- restore do novo host com fontes NuGet explicitamente vazias: aprovado, sem download;
- build Release da solução com `17` projetos: `0` erro e `0` warning;
- testes .NET: `332/332` unitários, `29/29` arquitetura e `14/14` integração;
- Dashboard: `60/60` testes, TypeScript, toolchain, brand, tokens, localização e ícones de provider aprovados;
- E2E correlacionado em Chrome `150.0.7871.125`: aprovado;
- ativação ausente e inválida do host: exit code `2`;
- PowerShell 5.1: recusa pré-recurso aprovada;
- PowerShell 7.6.3: execução e cleanup aprovados;
- formatação: aprovada sem mudança;
- documentação: `279` fontes comment-capable aprovadas;
- links Markdown: `408` links locais em `96` arquivos aprovados;
- fixture NuGet offline: `17` projetos completos e sem vulnerabilidade sintética;
- secret scan do worktree não ignorado e histórico disponível: aprovado;
- `git diff --check`: aprovado.

## Limitações e condições residuais

- A prova usa somente dados sintéticos, Server SQLite em memória, Agent SQLite efêmero e certificados/identidades de teste.
- O listener humano e o listener mTLS são portas loopback distintas da mesma aplicação e compartilham o mesmo Server SQLite; isso não escolhe topologia operacional futura.
- Apenas o Chrome instalado foi usado nesta execução; isso não homologa navegador, sistema operacional ou hardware.
- O sink de notificação é memória de teste. Nenhuma notificação Windows visível ou canal externo foi acionado.
- SignalR continua um hint descartável; snapshot HTTPS e polling de 30 segundos continuam autoritativos.
- O transporte de comando prova somente persistência e acknowledgement. Não existe `CommandAttempt`, executor, shell, Start/Stop/Restart ou efeito sobre serviço, banco ou infraestrutura.
- Os limites são os já definidos pelos sandboxes; esta remediação não produz sizing operacional, fairness de frota ou capacidade de produção.
- A execução não usa PostgreSQL real, IdP/PKI/vault real, provider, credencial operacional ou acesso externo.
- Aprovar esta remediação não muda a classificação da campanha histórica. Uma nova Campanha Consolidada exige autorização separada e deverá reavaliar o gate no commit então corrente.
- Human Gate final, amostra humana, promoção e transição continuam proibidos neste incremento.

## Classificação dos gates

- Escopo da remediação: `APROVADO`.
- Isolamento do runtime normal: `APROVADO`.
- Cadeia correlacionada do harness: `APROVADO`.
- Zero execução administrativa: `APROVADO`.
- EF determinístico: `APROVADO`.
- Contrato PowerShell e cleanup: `APROVADO`.
- Quality Gate da remediação: `APROVADO`.
- Campanha Consolidada histórica: `BLOQUEADO`, sem reclassificação automática.
- Human Gate próprio desta remediação: `ACEITO COM AS LIMITAÇÕES REGISTRADAS` por Bruno em 2026-07-19.
- Human Gate final do `STATE-06`: `NÃO ABERTO`.
- Promoção/transição: `NÃO AUTORIZADAS`.

## Decisão humana registrada

Depois de solicitar e receber a leitura direta deste relatório, principalmente de `Resultado em linguagem simples`, `Sequência E2E observada`, `Limitações e condições residuais` e `Classificação dos gates`, Bruno declarou em 2026-07-19:

> Remediação STATE-06 Consolidated E2E Evidence Harness and Deterministic Gate, commit ac12791: ACEITA COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo nova Campanha Consolidada, amostra humana, Human Gate final, runtime operacional, promoção nem transição de estado.

Essa decisão encerra somente o Human Gate próprio da remediação e aceita suas limitações documentadas. O `STATE-06 INTEGRATION` permanece inalterado; a Campanha Consolidada histórica continua `BLOQUEADA`, e nova campanha, amostra humana, Human Gate final, runtime operacional, promoção e transição continuam não autorizados.
