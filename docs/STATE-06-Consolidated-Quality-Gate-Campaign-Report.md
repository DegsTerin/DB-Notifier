# Relatório STATE-06 — Campanha Consolidada de Quality Gate

## Status e autoridade

- Data: 2026-07-19.
- Baseline executada: branch `main`, commit `5a47aae7030a406ffbe8b3960599fc68b9825d1b`.
- Estado mantido: `STATE-06 INTEGRATION`.
- Autoridade: autorização explícita de Bruno para a campanha descrita na [proposta documental](STATE-06-Consolidated-Quality-Gate-Campaign-Proposal.md), sem alteração ou remediação de código.
- Quality Gate consolidado do `STATE-06`: **BLOQUEADO**.
- Human Gate final do `STATE-06`: **NÃO ABERTO**.
- Promoção, `STATE-07`, runtime operacional e transição: **NÃO AUTORIZADOS** e não executados.

## Resultado em linguagem simples

As quatro partes construídas anteriormente continuam funcionando nos seus próprios laboratórios. O build passou sem erros ou avisos, todos os testes passaram, o Dashboard funcionou num Chrome exclusivo, a cadência real de 30 segundos foi observada, a notificação automática chegou uma vez ao sink de teste e o transporte de mensagens de comando permaneceu incapaz de executar qualquer ação.

Entretanto, esses laboratórios não estão ligados numa única execução. O teste do pipeline cria sua própria API, identidade e armazenamento. O teste do navegador usa outra fonte de snapshot em memória. O teste de notificação cria outro servidor e outro banco em memória. O teste de comando cria ainda outro sandbox. Executá-los em sequência prova que cada peça funciona separadamente, mas não prova que a mesma observação percorreu Agent → API → Dashboard/SignalR → notificação enquanto a mesma identidade também permaneceu sujeita à revogação e ao transporte não executável.

A campanha estava proibida de criar um novo harness ou alterar código. Por isso, ela parou nessa fronteira e classificou o gate como `BLOQUEADO`. Isso não significa que uma falha funcional tenha sido encontrada; significa que a evidência obrigatória de integração completa ainda não pode ser produzida com os artefatos existentes.

## Resumo do resultado

| Área | Resultado |
|---|---|
| Shutdown preflight e baseline | `APROVADO` |
| Auditorias estáticas/offline | `APROVADO` |
| Build e suítes completas | `APROVADO` |
| Cobertura e formatação | `APROVADO` |
| Smoke fail-closed | `APROVADO` |
| Pipeline autoritativo isolado | `APROVADO` |
| SignalR/TV/30 segundos no Chrome isolado | `APROVADO` |
| Notificação reconciliada em sink de teste | `APROVADO` |
| Transporte de comando não executável | `APROVADO` |
| Composição única correlacionada dos quatro incrementos | `BLOQUEADO` |
| Quality Gate consolidado | **`BLOQUEADO`** |

## Baseline factual congelada

- UTC inicial: `2026-07-19T21:44:07.5560630Z`.
- Worktree inicial: limpa.
- Processos/listeners DB-Notifier iniciais: `0/0`.
- .NET SDK: `10.0.301`.
- Entity Framework CLI e packages: `10.0.9`.
- Node.js/npm: `24.18.0` / `11.16.0`.
- PowerShell principal: `7.6.3`.
- Chrome dedicado: `150.0.7871.125`.
- Espaço livre no volume temporário: `322,12 GiB`.
- Projetos da solução: `16`.
- `node_modules` e assets NuGet existentes: disponíveis localmente; nenhum restore/download foi executado.
- SHA-256 inicial/final do `package-lock.json`: `65CA0E44F1E95E1981C3046181C2A471EAEFCB4EECF673E1BB410F844DC9FEA4`.
- SHA-256 inicial/final do `global.json`: `A0FC2EFCD4CC3A24444007B6E49F5B3C754E9D9E1869052C6AB7BDCBA7CB7960`.

O `git fsck --full` saiu com código `0` e listou somente blobs/trees antigos não alcançáveis já presentes no repositório; nenhuma corrupção foi indicada.

## Sequência executada

1. O shutdown preflight confirmou zero processo e zero listener pertencente ao DB-Notifier.
2. Branch, commit, worktree, versões, espaço temporário, caches e hashes foram congelados.
3. A busca read-only encontrou harnesses próprios de Agent/pipeline/comando, Dashboard/SignalR/TV e notificação, mas nenhuma referência executável a campanha consolidada.
4. Gates de integridade, documentação, links, secrets, NuGet offline, npm offline, assets, tokens, localização e TypeScript foram executados.
5. A solução foi compilada em Release sem restore.
6. As suítes .NET completas foram executadas; depois os quatro E2E proprietários foram repetidos individualmente.
7. Dashboard tests/build, Pester, bundle, cobertura, formatação e smoke fail-closed foram executados.
8. O browser E2E foi iniciado primeiro pelo Windows PowerShell 5.1 e encontrou incompatibilidade de ferramenta no cleanup. Nenhuma amostra foi aceita dessa tentativa; o diretório temporário exclusivo foi verificado e removido.
9. O mesmo runner foi repetido no PowerShell 7 e passou no Chrome dedicado com perfil efêmero.
10. Uma primeira verificação EF com `--no-build` leu assemblies Debug antigos e acusou model drift. Os dois projetos de persistência foram recompilados em Debug, sem restore, e Agent/Server então confirmaram ausência de mudanças pendentes.
11. A inspeção correlacionou as fronteiras dos harnesses e confirmou que eles não compartilham uma única identidade, store, projeção ou execução.
12. O cleanup confirmou zero processo/listener e zero diretório temporário criado pela campanha.

## Verificações automáticas

### Repositório e supply chain offline

- `git diff --check`: aprovado.
- `git fsck --full`: aprovado, com objetos antigos não alcançáveis informativos.
- documentação: `273` fontes comment-capable aprovadas.
- links Markdown: `397` links locais em `93` arquivos aprovados antes do relatório.
- pacote documental final: documentation gate novamente aprovado para `273` fontes comment-capable, links novamente aprovados para `397` referências locais em `94` arquivos e secret scan novamente aprovado.
- secret scan: worktree não ignorado e histórico Git disponível aprovados.
- NuGet: fixture sintética complete-empty aprovada para `16` projetos; não é advisory atualizado de registry.
- npm: `npm audit --offline --audit-level=high` retornou zero vulnerabilidade no cache disponível; não é consulta atual ao registry.
- Node/npm, marca, `11` identidades/`22` variantes de provider icon, tokens e localização: aprovados.
- TypeScript `--noEmit`: aprovado.
- package/lockfile/global.json: sem alteração.

### Build, testes e cobertura

- build Release da solução: `16` projetos, zero erro e zero aviso.
- testes unitários: `332/332`.
- testes de arquitetura: `25/25`.
- testes de integração: `14/14`.
- testes Dashboard: `60/60`.
- build Vite: aprovado.
- cobertura .NET: `78,91%` de linhas e `49,51%` de branches, acima dos pisos `70%/45%`.
- Pester legado: `23` aprovados, um skip condicional previsto e cobertura `32,08%` (`290/904`).
- `dotnet format --verify-no-changes`: aprovado.
- bundle `ValidateOnly`: aprovado.
- Agent/Server EF model drift: não detectado depois de recompilar os assemblies Debug realmente consumidos pelo EF CLI.

### Smoke fail-closed

- liveness local: `200`.
- endpoints HTTP protegidos: `426`.
- workers Agent: todos registraram estado desabilitado.
- persistência Agent normal: não inicializada.
- API/Worker normais: nenhum sandbox v2, SignalR, notificação ou command polling ativado.

## Evidência E2E observada por componente

### Pipeline autoritativo

O E2E `SyntheticObservationPipelineSurvivesOfflineReplayReorderStalenessAndRevocation` passou novamente em processo isolado. Ele observou enrollment de teste, assignment read-only, Agent SQLite/outbox, API indisponível, reconexão, perda de resposta, replay, reorder, incompatibilidade, snapshot autoritativo, staleness e revogação.

### SignalR, Dashboard TV e navegador

O Chrome dedicado em HTTPS loopback observou:

- hint SignalR autenticado seguido por releitura da API;
- `3` requests no cenário de hint, uma publicação e concorrência máxima `1`;
- próxima reconciliação independente após `30.018 ms`;
- cenário autoritativo periódico com intervalo mínimo de `30.011 ms`;
- ETag/`304`, denied, incompatible, malformed, oversized, error, timeout e offline/recovery;
- reconnect SignalR, cancelamento, late fencing e pause/resume serial;
- `1.228` requests HTTP locais e `26` WebSockets observados pelo harness;
- zero request HTTP externo e `operationalData=false`.

### Notificação reconciliada

O E2E `SilentBaselineThenCommittedTransitionIsDeliveredOnceAcrossConsumerRestart` passou novamente. A baseline permaneceu silenciosa, a transição sintética `Healthy → Unavailable` foi entregue uma vez ao `RecordingSink`, e a reabertura do ledger não repetiu o pedido.

Isso não prova apresentação visível pelo Windows; a campanha não autorizou amostra humana.

### Transporte não executável de comando

O E2E `CommandTransportReplaysAcrossProcessesAndCannotCreateAnExecutionAttempt` passou novamente. Ele cobriu versão incompatível, gap, tamanho, identidade errada, perda de resposta, replay, acknowledgement, revogação e isolamento final, com zero `CommandAttempt` ou efeito operacional.

## Bloqueio da composição consolidada

A inspeção direta encontrou quatro fronteiras independentes:

1. o pipeline autoritativo cria `AgentFleetSandbox`, `AgentFileSandbox`, identidade, API e projeção próprios dentro do teste de integração;
2. o browser host substitui `IDashboardTvSnapshotSource` por `BrowserSignalRSnapshotSource`, derivado da fixture `DashboardTvSandboxSnapshotSource`, e não pela projeção do pipeline Agent;
3. a notificação cria `NotificationSandbox`, certificado e Server SQLite em memória próprios e commita diretamente sua transição sintética;
4. o comando cria outro `AgentFleetSandbox`, outro `AgentFileSandbox` e outra identidade.

Os testes não expõem uma sessão compartilhável que permita ao Chrome e ao consumidor de notificação usar a mesma projeção produzida pelo Agent, nem permitem que revogação e comando sejam correlacionados à mesma identidade dessa cadeia. A busca em `src/`, `tests/` e `scripts/` também não encontrou um orquestrador consolidado existente.

Criar esse orquestrador exigiria código/teste novo. Como isso estava expressamente proibido, o critério obrigatório de composição única ficou `BLOQUEADO`.

## Achados classificados

### Alta — evidência E2E consolidada indisponível

- Impacto: impede comprovar o principal critério de fechamento que liga Agent → API → interfaces/canal numa mesma execução correlacionada.
- Evidência: sandboxes, identidades, stores e fontes separados, conforme inspeção e E2E proprietários.
- Classificação: `BLOQUEADO`, porque a evidência exige novo harness e não porque uma falha funcional tenha sido reproduzida.
- Remediação recomendada: propor e autorizar separadamente um harness consolidado somente de teste, sem ativar composição normal.

### Baixa — warnings EF de consulta limitada sem ordenação

- Evidência: a classe `AgentFleetApiEndToEndTests` emitiu dois warnings `Microsoft.EntityFrameworkCore.Query[10102]` nos cenários que exercitam Agent Fleet.
- Inspeção: existem `Take` limitados sem `OrderBy` ao carregar certificados para revogação e scopes de autorização. Nos caminhos atuais, exceder o limite falha fechado e a ordem não altera a decisão; impacto material não foi demonstrado.
- Impacto: ruído de diagnóstico e risco de esconder regressão futura, sem falha observada nesta campanha.
- Remediação recomendada: adicionar ordenação estável em incremento autorizado e repetir os testes.

### Ferramenta — runner de browser depende implicitamente de PowerShell moderno

- Evidência: sob Windows PowerShell 5.1, `String.Contains(value, StringComparison)` falhou durante cleanup; sob PowerShell 7.6.3, o runner passou integralmente.
- Impacto: primeira invocação não concluiu e deixou somente o diretório temporário próprio, depois removido com verificação de ownership.
- Remediação recomendada: declarar a versão mínima ou usar uma comparação compatível, sob autorização separada.

### Ferramenta resolvida — EF CLI leu assemblies Debug antigos

- Evidência: `has-pending-model-changes --no-build` usou `bin/Debug` antigo e acusou drift; depois de build Debug sem restore, ambos os contextos retornaram “No changes have been made”.
- Impacto final: nenhum model drift observado; não permanece como achado técnico.

Não foi encontrado achado crítico. O achado alto é uma lacuna obrigatória de evidência, não uma vulnerabilidade ou efeito operacional.

## Matriz resumida Lifecycle → evidência

| Critério STATE-06 | Evidência desta campanha | Classificação |
|---|---|---|
| contratos/versionamento/incompatibilidade | suítes .NET, Dashboard e casos E2E | `APROVADO` |
| autenticação/revogação Agent | Agent Fleet E2E e SignalR same-origin | `APROVADO` no sandbox |
| sincronização offline/replay/reorder | pipeline autoritativo E2E | `APROVADO` isoladamente |
| TV: leitura inicial, 30 s, ETag, concorrência 1 | Chrome dedicado | `APROVADO` |
| SignalR somente hint | Chrome dedicado e arquitetura | `APROVADO` |
| notificação reconciliada/deduplicada | E2E com sink de teste | `APROVADO` isoladamente |
| expiração/replay de comando sem execução | command transport E2E | `APROVADO` isoladamente |
| cadeia única Agent → API → interfaces/canal | nenhum harness compartilhado | `BLOQUEADO` |
| cleanup | zero resíduo pertencente à campanha | `APROVADO` |

## Observado, inferido e não testado

### Observado

- todos os números de build/teste/cobertura descritos neste relatório;
- os quatro E2E proprietários passando separadamente;
- Chrome real, HTTPS loopback, SignalR, cadência de 30 segundos e zero request externo observado pelo harness;
- smoke normal fail-closed;
- ausência de model drift após build correto dos assemblies consumidos;
- zero processo/listener e zero temporário criado pela campanha no cleanup.

### Inferido por inspeção direta

- as composições normais permanecem sem ativação dos sandboxes;
- a ausência de uma sessão/orquestrador compartilhável impede correlação ponta a ponta sem código novo;
- os warnings EF atuais não mudam a decisão porque os dois caminhos falham fechado acima do limite e não dependem da ordem abaixo dele.

### Não testado

- composição única dos quatro incrementos;
- apresentação visual da notificação pelo Windows;
- PostgreSQL real, PKI/IdP/vault operacionais, provider/banco monitorado ou credencial real;
- carga, endurance, HA, power-loss, disaster recovery ou compatibilidade ampla de browsers/plataformas;
- qualquer comando administrativo, executor, provider result ou post-probe;
- advisories atuais de registries externos;
- Human Gate final, `STATE-07`, produção ou promoção MOD-12.

## Cleanup e isolamento

- processos/listeners DB-Notifier finais pertencentes à campanha: `0/0`;
- perfis/diretórios `DBNotifier-DashboardTv-BrowserE2E-*`: `0`;
- diretórios `DBNotifier-Runtime-Audit-*`: `0`;
- diretórios de notificação, Agent Fleet e command transport criados pela campanha: `0`;
- worktree antes da documentação factual: limpa;
- navegador comum, IDE, serviços, bancos e processos alheios: não tocados.

Um diretório de cobertura `DBNotifier-DotNet-Coverage-4b618...` criado às `20:49Z`, antes do início desta campanha às `21:44Z`, foi identificado e preservado por não pertencer à execução atual. O diretório de cobertura criado pela campanha foi removido pelo runner.

O diretório temporário da primeira tentativa de browser foi removido de forma não recuperável depois de confirmar caminho, prefixo e ausência de processo proprietário; continha somente artefatos efêmeros da tentativa falha.

## Classificação dos gates

- Checks comuns e Quality Gates dos componentes existentes: **APROVADOS**.
- Critério obrigatório de composição E2E única: **BLOQUEADO**.
- Quality Gate consolidado do `STATE-06`: **BLOQUEADO**.
- Human Gate final do `STATE-06`: **NÃO ABERTO**; um gate técnico bloqueado não será enviado como aprovação humana.
- Runtime operacional, promoção `none → OBSERVER`, `STATE-07`, deploy e transição: **NÃO AVALIADOS E NÃO AUTORIZADOS**.

## Próxima atividade

Nenhuma implementação está autorizada. O próximo passo seguro é uma proposta exclusivamente documental de remediação do bloqueio, limitada ao desenho de um harness consolidado de teste e ao tratamento separado dos dois achados não bloqueantes.

Se Bruno desejar continuar, o pedido sugerido é:

> Apresente uma proposta exclusivamente documental para remediar o bloqueio da Campanha Consolidada de Quality Gate do STATE-06, cobrindo um harness único que correlacione os quatro incrementos já aceitos, os warnings EF de ordenação e a compatibilidade do runner de browser com a versão declarada de PowerShell, sem implementação, runtime, acesso externo, promoção ou transição de estado.

Esse pedido produzirá somente uma proposta. Qualquer implementação posterior continuará exigindo autorização separada.
