# Relatório STATE-06 — Repetição da Campanha Consolidada de Quality Gate

## Status e autoridade

- Data: 2026-07-19.
- Baseline executada: commit `66d0a9fa661f9ec05c365aececa7e10cf6903c8b`, que contém a remediação aceita `ac12791`.
- Estado mantido: `STATE-06 INTEGRATION`.
- Classificação automática final: `REPROVADO`.
- Alterações técnicas durante a campanha: nenhuma.
- Acesso externo, recurso operacional, comando administrativo, `CommandAttempt`, executor, notificação Windows visível, deploy e amostra humana: não usados.
- Human Gate final, promoção e transição: não abertos, não autorizados e não inferidos.

Este relatório registra somente a repetição automática autorizada. Ele não reescreve o [relatório histórico bloqueado](STATE-06-Consolidated-Quality-Gate-Campaign-Report.md), não corrige o achado encontrado e não concede autoridade para remediação ou nova execução.

## Resultado em linguagem simples

Quase toda a campanha passou: o projeto compilou, todos os testes gerais e isolados passaram, a cobertura ficou acima dos pisos, o Dashboard funcionou no Chrome local, os warnings EF antigos não reapareceram e os runners recusaram corretamente o PowerShell antigo.

Porém, o teste mais importante desta repetição — o laboratório que liga todos os quatro incrementos na mesma execução — falhou no fechamento. Depois de testar observações, Dashboard, SignalR, notificação e transporte não executável de comando, ele tentou revogar o mesmo Agent. O host local respondeu `503` na etapa `finalising-revocation` e o runner terminou com código `1`.

Esse resultado não pode ser arredondado para aprovação. Os testes isolados de revogação passaram, mas eles não substituem a cadeia correlacionada obrigatória. Como a campanha estava proibida de corrigir achados, nenhuma alteração foi feita e nenhum retry foi usado para ocultar a falha. O Quality Gate repetido fica, portanto, `REPROVADO`.

Em termos simples: as peças funcionaram separadamente, mas a prova única de que todas continuam funcionando juntas falhou exatamente ao verificar a revogação. O `STATE-06` continua aberto e o Human Gate final não pode começar enquanto esse achado permanecer.

## Baseline factual congelada

| Item | Evidência observada |
|---|---|
| Commit técnico | `66d0a9fa661f9ec05c365aececa7e10cf6903c8b` |
| Remediação incorporada | `ac12791` |
| Branch do repositório principal | `main` |
| Cópia executada | worktree detached, limpa e materializada com LF canônico |
| Diferença técnica `ac12791..66d0a9f` | nenhuma |
| .NET SDK | `10.0.301` |
| EF CLI | `10.0.9` |
| Node/npm | `24.18.0` / `11.16.0` |
| PowerShell moderno | `7.6.3` |
| Windows PowerShell | `5.1.26100.8875` |
| Browser dedicado | Chrome `150.0.7871.125` |
| `package-lock.json` SHA-256 | `65CA0E44F1E95E1981C3046181C2A471EAEFCB4EECF673E1BB410F844DC9FEA4` |
| `global.json` SHA-256 | `A0FC2EFCD4CC3A24444007B6E49F5B3C754E9D9E1869052C6AB7BDCBA7CB7960` |

O shutdown preflight encontrou zero processo, listener ou navegador dedicado pertencente ao DB-Notifier. A solução continha `17` projetos. O host consolidado, sob `tests/`, tinha zero `PackageReference` próprio e uma única `ProjectReference`; nenhuma composição normal em `src/DBNotifier.Server.Api` ou `src/DBNotifier.Agent.Worker` o ativava.

## Sequência executada

1. shutdown preflight, inspeção de ownership e congelamento do commit;
2. inspeção read-only do diff, solução, projeto consolidado, fixture NuGet, queries EF e contrato PowerShell;
3. gates offline de documentação, links, secrets, dependências disponíveis, assets, tokens, localização e ícones;
4. build Release, suítes .NET, Dashboard, cobertura, format, Pester, bundle, migrations e smoke fail-closed;
5. uma execução do harness correlacionado obrigatório em Chrome dedicado;
6. execução serial do browser harness aceito e dos testes proprietários isolados;
7. captura dos logs EF e teste negativo dos runners sob Windows PowerShell 5.1;
8. auditoria de integridade e cleanup de processos, perfis, stores, diretórios e worktrees temporárias;
9. criação deste relatório factual, sem correção técnica.

## Verificações automáticas

### Repositório e supply chain offline

- `git fsck --full`: código `0`; objetos dangling históricos foram apenas informativos, sem corrupção.
- `git diff --check` e inspeções de escopo: aprovados.
- documentação de código: `279` fontes comment-capable aprovadas na baseline.
- links Markdown: `415` links locais em `97` arquivos aprovados na baseline.
- secret scan do worktree não ignorado e do histórico disponível: aprovado.
- fixture NuGet sintética: `17` projetos reconhecidos.
- `npm audit --offline --audit-level=high`: zero vulnerabilidade no cache local disponível; não comprova advisories atuais do registry.
- toolchain, marca, tokens, localização e ícones de provider: aprovados; `11` identidades e `22` variantes.
- nenhuma restauração externa, aquisição, download ou alteração de package/lockfile ocorreu.

### Build, testes e cobertura

| Verificação | Resultado |
|---|---|
| `dotnet build DBNotifier.sln -c Release --no-restore` | `17` projetos, `0` erro, `0` warning |
| testes de arquitetura | `29/29` aprovados |
| testes unitários | `332/332` aprovados |
| testes de integração completos | `14/14` aprovados |
| cobertura .NET | linhas `78,9%`; branches `49,51%` |
| `dotnet format --verify-no-changes --no-restore` | aprovado |
| Dashboard typecheck | aprovado |
| Dashboard testes | `60/60` aprovados |
| Dashboard build | aprovado |
| Pester legado | `23` aprovados; `1` skip condicional esperado; `32,08%` (`290/904`) |
| bundle `ValidateOnly` | aprovado |
| smoke fail-closed | aprovado |

Os pisos vigentes de cobertura, `70%` para linhas e `45%` para branches, foram superados.

### Persistência, EF e composição normal

- build Debug sem restore das persistências Agent SQLite e Server PostgreSQL: zero erro e zero warning;
- `dotnet ef migrations has-pending-model-changes --no-build`: nenhum model drift nos dois contextos;
- repetição detalhada dos oito E2E `AgentFleetApiEndToEndTests`: `8/8` aprovados e zero ocorrência de `Microsoft.EntityFrameworkCore.Query[10102]`;
- smoke normal: liveness `200`, endpoints protegidos HTTP `426`, quatro workers Agent desabilitados e nenhuma base Agent inicializada;
- zero referência de ativação normal do sandbox consolidado em Server ou Worker.

## Sequência E2E observada

### Harness correlacionado obrigatório

O runner `run-state06-consolidated-e2e.ps1`, invocado por PowerShell 7 com Chrome dedicado e perfil efêmero, observou com sucesso as fases iniciais:

- isolamento da composição normal;
- leitura imediata, hint SignalR, notificação em sink de teste e cadência independente;
- preservação offline, recuperação e fencing;
- transporte de comando deliberadamente não executável até o início do fechamento.

Na chamada final, o endpoint local `finalise` respondeu HTTP `503` com a evidência sanitizada:

- `code`: `state06.consolidated_operation_failed`;
- `stage`: `finalising-revocation`;
- runner: exit code `1` após aproximadamente `94,9` segundos.

A inspeção read-only do código delimitou essa etapa a:

1. revogar o mesmo Agent da cadeia correlacionada;
2. executar uma tentativa única de heartbeat;
3. executar uma tentativa única de leitura de assignments;
4. exigir que ambas sejam negadas antes de testar observação e comando revogados.

A resposta deliberadamente sanitizada não revelou se a exceção veio da revogação, do heartbeat, dos assignments ou de uma das duas asserções fail-closed. Portanto, a causa precisa não foi inventada. Como a execução não alcançou o snapshot final, ela não produziu evidência válida das contagens terminais esperadas de observações, entrega, journal e zero attempt para esta campanha.

### Browser Dashboard TV e SignalR

O runner proprietário separado passou em Chrome dedicado sobre HTTPS loopback:

- hint SignalR autenticado seguido de releitura autoritativa;
- uma publicação, `3` requests no cenário e concorrência máxima `1`;
- reconciliação independente depois de `30.012 ms`;
- cadência autoritativa com intervalo mínimo de `30.007 ms`;
- ETag/`304`, denied, incompatible, malformed, oversized, error, timeout, offline/recovery, reconnect, cancelamento, late fencing e pause/resume serial;
- `1.230` requests HTTP locais e `27` WebSockets;
- zero request HTTP externo e `operationalData=false`.

### Regressões proprietárias

- Agent Fleet, identidade, revogação, pipeline autoritativo e transporte não executável: `8/8` aprovados;
- Dashboard snapshot e SignalR: `4/4` aprovados;
- notificação reconciliada: `2/2` aprovados;
- `SyntheticObservationPipelineSurvivesOfflineReplayReorderStalenessAndRevocation`: aprovado;
- `SilentBaselineThenCommittedTransitionIsDeliveredOnceAcrossConsumerRestart`: aprovado;
- `CommandTransportReplaysAcrossProcessesAndCannotCreateAnExecutionAttempt`: aprovado.

Esses resultados provam as fronteiras isoladas, mas não anulam a falha da cadeia única obrigatória.

## Contrato PowerShell

Os runners consolidado e Dashboard TV declaram PowerShell 7. Sob Windows PowerShell 5.1, ambos recusaram a execução antes de criar recursos, com exit code `1`, `ScriptRequiresUnmatchedPSVersion=true` e zero novo root temporário. O runner Pester legado permaneceu compatível com Windows PowerShell 5.1.

## Achados classificados

### Alta — fechamento correlacionado de revogação falhou

- Evidência: HTTP `503`, `state06.consolidated_operation_failed`, estágio `finalising-revocation`, exit code `1`.
- Impacto: o critério obrigatório de revogar o mesmo Agent e provar recusa de heartbeat, assignments, observação e comando na mesma execução não foi satisfeito.
- Delimitação: os testes isolados de revogação passaram; não há evidência de efeito operacional, bypass em produção ou `CommandAttempt`.
- Classificação: achado `ALTO` e Quality Gate `REPROVADO`.
- Remediação futura sugerida: proposta separada para tornar a causa observável de forma sanitizada, reproduzir deterministicamente a corrida e corrigir somente o harness ou comportamento comprovadamente proprietário do sandbox. Nenhuma dessas ações está autorizada por este relatório.

### Ferramenta resolvida — materialização inicial converteu LF para CRLF

A primeira worktree temporária, criada sob o `core.autocrlf=true` global, materializou CSS gerado com CRLF e fez o verificador de tokens acusar conteúdo stale. O blob do commit e a worktree principal continham LF canônico e hash diferente somente pela conversão de checkout. Essa cópia foi descartada antes da campanha válida; uma nova worktree detached foi criada com `core.autocrlf=false`, cujo hash coincidiu com o blob e cujo gate de tokens passou. Não houve alteração rastreada nem defeito do commit.

### Ferramenta resolvida — remoção dos metadados das worktrees

O Git removeu as duas pastas temporárias e desregistrou as worktrees, mas o OneDrive/ACL local reteve dois diretórios internos `.git/worktrees` vazios ou quase vazios como reparse points read-only. Depois de validar os caminhos exatos e confirmar que não eram a worktree principal, apenas esses metadados não registrados tiveram ACL/atributos locais normalizados e foram removidos. O `node_modules` original foi preservado. O resultado final foi zero resíduo da campanha.

## Matriz resumida Lifecycle → evidência

| Critério STATE-06 | Evidência desta repetição | Classificação |
|---|---|---|
| baseline, integridade e escopo | commit exato, worktree limpa e hashes preservados | `APROVADO` |
| contratos/versionamento/incompatibilidade | suítes .NET, Dashboard e E2E proprietários | `APROVADO` |
| autenticação/revogação isoladas | Agent Fleet e SignalR E2E | `APROVADO` no sandbox isolado |
| pipeline offline/replay/reorder | pipeline autoritativo E2E | `APROVADO` isoladamente |
| TV: leitura inicial, 30 s, ETag e concorrência 1 | Chrome dedicado | `APROVADO` |
| SignalR somente hint | Chrome dedicado e arquitetura | `APROVADO` |
| notificação reconciliada/deduplicada | E2E com sink de teste | `APROVADO` isoladamente |
| transporte sem execução | command E2E; zero attempt no teste proprietário | `APROVADO` isoladamente |
| warnings EF `10102` | captura detalhada: zero ocorrência | `APROVADO` |
| contrato PowerShell | pwsh 7 executa; 5.1 recusa antes de recursos | `APROVADO` |
| cadeia única até revogação terminal | falha `503` em `finalising-revocation` | `REPROVADO` |
| cleanup | zero processo, perfil, root ou metadado temporário | `APROVADO` |

## Observado, inferido e não testado

### Observado

- todos os resultados numéricos e exit codes registrados acima;
- uma falha do harness correlacionado na etapa de revogação;
- aprovação dos E2E proprietários quando executados separadamente;
- zero warning EF `10102` na captura aplicável;
- Chrome real local, HTTPS loopback, cadência de 30 segundos e zero HTTP externo observado pelos harnesses;
- recusa pré-recurso sob Windows PowerShell 5.1;
- limpeza completa dos recursos pertencentes à campanha.

### Inferido por inspeção direta

- a falha ocorreu dentro da sequência delimitada entre `RevokeAgentAsync` e as asserções de negação de heartbeat/assignments;
- o host consolidado permanece restrito a `tests/` e não é ativado pela composição normal;
- nenhum package, lockfile, migration ou fonte técnica mudou entre `ac12791` e `66d0a9f`.

### Não testado

- causa exata da exceção interna sanitizada;
- PostgreSQL real, provider, banco monitorado, IdP, PKI, vault ou credencial operacional;
- notificação Windows visível ou canal externo;
- execução de comando, `CommandAttempt`, executor, shell, serviço ou infraestrutura;
- carga representativa, endurance, HA, disaster recovery, escala de frota ou múltiplos browsers/plataformas;
- experiência humana, acessibilidade humana e Human Gate final;
- runtime operacional, deploy, promoção ou transição.

## Limitações e condições residuais

- A falha pode envolver uma corrida ou divergência de estado no sandbox correlacionado, mas isso é hipótese, não conclusão.
- A aprovação dos testes isolados não prova composição única sob a mesma revogação.
- SQLite efêmero, certificados e identidades de teste não provam PostgreSQL, PKI, key store ou recuperação operacional.
- SignalR continua best-effort; API e polling periódico permanecem autoritativos.
- O sink prova pedido local de entrega, não apresentação visível pelo Windows.
- O transporte prova persistência/acknowledgement e recusa, não execução.
- A auditoria npm offline não comprova advisories publicados depois do cache local.
- Nenhum resultado deste relatório constitui evidência de produção ou autorização de ativação.

## Cleanup e isolamento

- runners e hosts temporários encerrados;
- Chrome dedicado e perfil efêmero removidos;
- zero processo ou listener pertencente aos harnesses;
- zero root temporário de runtime pertencente à campanha;
- duas worktrees temporárias removidas e zero registro/metadado residual;
- junctions temporários removidos sem atravessar ou apagar o `node_modules` original;
- worktree técnica avaliada limpa e hashes rastreados preservados;
- repositório principal e alterações documentais concorrentes preservados.

## Classificação dos gates

| Gate | Resultado |
|---|---|
| baseline e shutdown | `APROVADO` |
| inspeção read-only e escopo | `APROVADO` |
| supply chain e gates offline | `APROVADO` com limitação de cache offline |
| build, testes e cobertura | `APROVADO` |
| EF/model drift/warnings | `APROVADO` |
| smoke fail-closed | `APROVADO` |
| browser e E2E proprietários | `APROVADO` isoladamente |
| E2E correlacionado obrigatório | `REPROVADO` |
| PowerShell | `APROVADO` |
| cleanup | `APROVADO` |
| **Quality Gate consolidado repetido** | **`REPROVADO`** |
| Human Gate final | `NÃO ABERTO` |
| promoção/transição | `NÃO AUTORIZADA` |

Não foi encontrado achado crítico, ação externa ou efeito operacional. O achado alto aberto impede aprovação automática e impede iniciar amostras humanas como se o Quality Gate estivesse concluído.

## Próxima atividade

Nenhuma remediação está autorizada por este relatório. O próximo passo seguro é Bruno revisar este resultado e, se desejar continuar, solicitar uma proposta exclusivamente documental de remediação do achado `finalising-revocation`. Essa proposta deverá delimitar diagnóstico reproduzível e evidência sanitizada sem corrigir código ainda. Somente uma autorização posterior e separada poderá liberar a implementação da remediação; depois dela, outra autorização deverá repetir a campanha. Amostra humana, Human Gate final, promoção e transição continuam proibidos enquanto o Quality Gate permanecer `REPROVADO`.
