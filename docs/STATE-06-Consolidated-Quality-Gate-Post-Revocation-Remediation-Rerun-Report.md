# Relatório STATE-06 — Nova repetição da Campanha Consolidada pós-remediação de revogação

## Status e autoridade

- Data: 2026-07-20.
- Baseline congelada: commit `84217c64312a024ec4f286adfe4872184a21849c` na branch `main`.
- Baseline técnica incorporada: remediação `f9bb567`.
- Baseline factual incorporada: aceitação `67e0187`.
- Estado mantido: `STATE-06 INTEGRATION`.
- Classificação automática desta campanha: `APROVADO` com as limitações registradas.
- Repetição anterior no commit `66d0a9f`: permanece historicamente `REPROVADA`.
- Primeira campanha: permanece historicamente `BLOQUEADA`.
- Amostra humana, Human Gate final, runtime operacional, promoção e transição: não autorizados e não executados.

Bruno autorizou exclusivamente esta nova repetição automática, com verificações offline, artefactos existentes, runtimes locais temporários, Chrome dedicado, cleanup e relatório factual. A campanha não recebeu autoridade para corrigir qualquer achado.

## Resultado em linguagem simples

O laboratório completo passou desta vez. O mesmo Agent fictício atravessou enrollment, heartbeat, assignments, observações, API, Dashboard TV, SignalR, notificação em memória, transporte de comando não executável e revogação. A sequência terminou provando separadamente que o Server gravou a revogação, recusou novas operações e fez o Agent entrar em quarentena local sem apagar seu último assignment válido.

Os testes gerais, cobertura, persistência, navegador, limites, cancelamento, fencing, PowerShell e cleanup também passaram. Nenhum comando administrativo foi executado: foram criados dois registros sintéticos de journal e zero `CommandAttempt`.

Esta aprovação vale somente para o Quality Gate automático desta baseline e deste sandbox. Ela não prova PostgreSQL real, provider operacional, notificação visível no Windows, execução administrativa, produção ou escala. Também não abre nem aprova o Human Gate final.

## Baseline factual congelada

| Item | Evidência observada |
|---|---|
| commit | `84217c64312a024ec4f286adfe4872184a21849c` |
| branch | `main` |
| worktree inicial | limpa |
| ancestralidade | `f9bb567` e `67e0187` confirmados |
| mudança técnica depois de `f9bb567` | nenhuma; somente documentação em `docs/` e `prompts/` |
| shutdown preflight | zero processo, listener, navegador dedicado ou root consolidado próprio |
| espaço livre temporário | `318,59 GiB` |
| .NET SDK | `10.0.301` |
| EF CLI | `10.0.9` |
| Node/npm | `24.18.0` / `11.16.0` |
| PowerShell moderno | `7.6.3` |
| Windows PowerShell | `5.1.26100.8875` |
| Chrome dedicado | `150.0.7871.125` |

O primeiro matcher do preflight incluiu o próprio processo `pwsh` auditor porque sua linha de comando continha o caminho do repositório. A repetição excluiu explicitamente o PID do auditor e comprovou zero runtime do produto. Nenhum processo foi encerrado por esse falso positivo.

## Sequência executada

1. Shutdown preflight, worktree limpa, ancestralidade, diff técnico, ferramentas, espaço e temporários foram verificados.
2. Integridade Git, documentação, links, segredos, NuGet sintético, assets e auditorias offline foram executados.
3. A solução foi compilada em Release sem restore; suítes completas, cobertura, formatação, Dashboard, Pester e bundle foram executados.
4. Persistências foram compiladas em Debug sem restore; ambos os contextos EF foram verificados contra model drift.
5. O smoke fail-closed iniciou apenas API e Agent locais temporários e comprovou defaults desabilitados.
6. O harness correlacionado foi executado uma única vez em Chrome dedicado e passou até a revogação terminal.
7. As regressões Agent Fleet/R1–R7, Dashboard/SignalR e notificação foram repetidas serialmente.
8. O runner Dashboard TV foi executado uma única vez em novo Chrome dedicado e passou sua matriz.
9. Os dois runners modernos foram recusados pelo Windows PowerShell 5.1 antes de criar recursos.
10. Processos, listeners, profiles, stores, temporários e integridade Git foram auditados novamente.
11. Somente depois do cleanup começou a criação deste relatório factual.

Nenhuma falha foi repetida seletivamente e nenhuma correção foi aplicada durante a campanha.

## Verificações automáticas

### Repositório e supply chain offline

| Verificação | Resultado |
|---|---|
| `git fsck --full` | código `0`; somente objetos dangling históricos, sem corrupção |
| documentação de código | `280` fontes comment-capable aprovadas |
| links Markdown | `450` links locais em `102` arquivos aprovados |
| secret scan | aprovado no worktree não ignorado e histórico disponível |
| fixture NuGet sintética | `17` projetos reconhecidos |
| `npm audit --offline --audit-level=high` | zero vulnerabilidade no cache disponível |
| toolchain | Node `24.18.0` e npm `11.16.0` aprovados |
| marca/tokens/localização | aprovados |
| ícones de provider | `11` identidades e `22` variantes aprovadas |
| acesso externo/download | nenhum |

A auditoria npm offline não comprova advisories publicados depois do conteúdo disponível no cache local. A fixture NuGet comprova integridade estrutural do verificador, não uma consulta atual ao registry.

### Build, testes e cobertura

| Verificação | Resultado |
|---|---|
| build Release da solução com `--no-restore` | `17` projetos; `0` erro; `0` warning; `44,44 s` |
| testes unitários | `332/332` |
| testes de arquitetura | `30/30` |
| testes de integração completos | `16/16` |
| cobertura .NET | linhas `78,9%`; branches `49,51%` |
| formatação somente-verificação | aprovada |
| Dashboard typecheck | aprovado |
| Dashboard testes | `60/60` |
| Dashboard build | aprovado |
| Pester legado | `23` aprovados; `1` skip condicional esperado; `32,08%` (`290/904`) |
| bundle `ValidateOnly` | aprovado |

Os pisos de `70%` de linhas, `45%` de branches e `25%` de cobertura Pester foram superados.

### Persistência, EF e composição normal

- os projetos Agent SQLite e Server PostgreSQL compilaram em Debug sem restore, erro ou warning;
- `dotnet ef migrations has-pending-model-changes --no-build` retornou “No changes have been made” para ambos os contextos;
- os logs detalhados das regressões não contiveram `Microsoft.EntityFrameworkCore.Query[10102]`;
- o smoke retornou liveness `200`, recusou todos os endpoints protegidos com HTTP `426`, registrou os quatro workers Agent desabilitados e não inicializou persistência Agent;
- a busca sob `src/` encontrou zero referência ao host consolidado ou à sua ativação.

## Sequência E2E correlacionada observada

1. O runner construiu somente os artefactos locais já restaurados e iniciou host HTTPS loopback e Chrome dedicado.
2. Um Agent e uma identidade humana read-only, ambos sintéticos, foram separados.
3. Enrollment, assignment read-only, heartbeat e Agent SQLite foram exercitados.
4. Duas observações atravessaram offline, replay e ingestão idempotente até a projeção da API.
5. O Dashboard TV realizou leitura imediata; SignalR permaneceu hint e a reconciliação de 30 segundos permaneceu autoritativa.
6. Uma transição posterior à baseline produziu exatamente uma entrega no sink em memória.
7. O transporte não executável criou dois journals e zero `CommandAttempt`.
8. O último subprocesso de comando saiu antes da revogação.
9. O Server commitou o Agent e `1/1` certificado como revogados.
10. Heartbeat e assignments foram recusados diretamente pelo Server.
11. O primeiro heartbeat Agent-side alcançou o transporte, recebeu negação e persistiu quarentena sob fence `3`.
12. Assignments posteriores pararam localmente, preservaram o last-known-good e avançaram para fence `4`.
13. Observação e command poll permaneceram recusados.
14. O resultado terminal não reteve diagnóstico de falha e confirmou cleanup.

Resumo sanitizado observado:

```json
{"result":"passed","browser":"Chrome/150.0.7871.125","correlatedRun":true,"observationSamples":2,"notificationDeliveries":1,"commandJournalEntries":2,"commandAttempts":0,"revokedCertificateCount":1,"heartbeatFenceAfterRevocation":3,"assignmentsFenceAfterRevocation":4,"maximumSnapshotConcurrency":1,"observedHttpRequests":80,"observedWebSockets":22,"observedExternalHttpRequests":0,"operationalData":false}
```

## Regressões proprietárias observadas

| Grupo | Resultado |
|---|---|
| Agent Fleet, identidade, pipeline, comando, revogação, R1–R7 e sanitização | `10/10` |
| Dashboard snapshot e SignalR | `4/4` |
| notificação reconciliada | `2/2` |

O runner Dashboard TV separado passou em Chrome dedicado com:

- hint autenticado, releitura autoritativa e publicação única;
- reconciliação independente em `30.011 ms` e cadência mínima de `30.022 ms`;
- ETag/`304`, denied, incompatible, malformed, oversized, error/recovery, timeout/recovery, offline/recovery, reconnect, late fencing e pause/resume;
- concorrência máxima `1` em todos os cenários;
- `1.248` requests HTTP locais, `27` WebSockets, zero origem HTTP externa e `operationalData=false`.

## Contrato PowerShell

Os runners consolidado e Dashboard TV foram invocados separadamente pelo Windows PowerShell `5.1.26100.8875`. Ambos retornaram exit code `1` com `ScriptRequiresUnmatchedPSVersion=true` antes de criar root, processo, listener ou profile. Os runners modernos foram executados somente pelo PowerShell `7.6.3`; o Pester legado permaneceu no Windows PowerShell 5.1.

## Achados classificados

### Nenhum achado crítico, alto ou médio aberto

Todos os critérios obrigatórios desta baseline passaram. A falha histórica continua preservada em seu relatório e não foi apagada; a nova campanha prova somente o comportamento atual remediado.

### Ferramenta — matcher inicial incluiu o auditor

O matcher inicial do preflight identificou o próprio `pwsh` da auditoria. A causa era a presença do caminho do repositório na linha de comando do auditor. A checagem foi repetida excluindo somente seu PID e comprovou zero runtime do produto. Nenhum processo foi encerrado e nenhum critério foi relaxado.

### Ambiente — root de cobertura anterior preservado

O cleanup final encontrou um root `DBNotifier-DotNet-Coverage-*` com última alteração em 2026-07-19, anterior à campanha. Ele não correspondia ao root criado e removido pela cobertura atual. Por falta de ownership desta campanha, foi preservado e classificado como resíduo preexistente, não como falha de cleanup atual.

## Observado, inferido e não testado

### Observado

- todos os gates e contagens deste relatório;
- cadeia correlacionada completa e revogação granular;
- R1–R7, fencing, budgets, cancelamento e sanitização;
- zero `CommandAttempt`, zero origem HTTP externa observada pelos browsers e cleanup dos recursos atuais;
- worktree limpa antes da documentação deste relatório.

### Inferido por inspeção direta

- ausência de composição normal do host consolidado;
- ausência de mudança técnica depois de `f9bb567`;
- root de cobertura datado de 2026-07-19 como preexistente à campanha de 2026-07-20.

### Não testado

- PostgreSQL real, provider ou banco monitorado;
- PKI, IdP, vault, credencial ou Agent operacional;
- apresentação visível de notificação Windows;
- comando administrativo, executor, post-probe ou efeito em serviço/infraestrutura;
- rede externa, deploy, produção, escala, endurance, HA ou disaster recovery;
- amostra humana, Human Gate final, promoção ou transição.

## Limitações e condições residuais

- A exceção original de `finalising-revocation` continua irrecuperável; a campanha atual não reconstrói o passado.
- Server SQLite em memória não prova PostgreSQL ou concorrência distribuída.
- R3 prova sobreposição HTTP, não locking equivalente a PostgreSQL.
- Agent SQLite, identidades, certificados, assignments, observações e comandos são sintéticos e efêmeros.
- Um host e um Chrome não constituem homologação ampla, sizing ou fairness de frota.
- SignalR permanece best-effort; API e polling permanecem autoritativos.
- O sink em memória não prova apresentação pelo Windows.
- O transporte não executável não prova executor ou controle administrativo.
- Auditorias offline não comprovam advisories externos atuais.
- Aprovação automática não substitui amostras humanas nem o Human Gate final.

## Cleanup e isolamento

- zero processo ou listener pertencente à campanha permaneceu;
- os dois Chromes dedicados e seus profiles efêmeros foram encerrados/removidos;
- hosts, Agent SQLite, stores e roots temporários desta campanha foram removidos;
- o root de cobertura atual foi removido pelo próprio verificador;
- um root de cobertura preexistente de 2026-07-19 foi preservado, sem ser atribuído a esta campanha;
- nenhum processo, browser, arquivo ou recurso alheio foi encerrado ou removido;
- a worktree permaneceu limpa e a baseline continuou em `84217c6` antes da criação deste relatório.

## Classificação dos gates

| Gate | Classificação |
|---|---|
| baseline, shutdown e integridade | `APROVADO` |
| supply chain e verificações offline | `APROVADO` com limitações de atualidade |
| build, suítes e cobertura | `APROVADO` |
| documentação, formatação e assets | `APROVADO` |
| persistência, migrations e EF | `APROVADO` |
| smoke fail-closed e composição normal | `APROVADO` |
| R1–R7, sanitização e budgets | `APROVADO` |
| harness correlacionado e revogação terminal | `APROVADO` |
| Dashboard TV/SignalR/browser | `APROVADO` |
| notificação em sink de teste | `APROVADO` |
| comando deliberadamente não executável | `APROVADO` |
| PowerShell 7/5.1 | `APROVADO` |
| cleanup e ausência de acesso externo | `APROVADO` |
| **Quality Gate consolidado desta baseline** | **`APROVADO`** |
| repetição anterior no commit `66d0a9f` | `REPROVADA` historicamente; não reescrita |
| primeira campanha | `BLOQUEADA` historicamente; não reescrita |
| amostra humana e Human Gate final | `NÃO AUTORIZADOS` / `NÃO EXECUTADOS` |
| promoção e transição | `NÃO AUTORIZADAS` |

Depois da inclusão deste relatório e dos registros factuais, o gate documental passou para `280` fontes comment-capable e `451` links locais em `103` arquivos; secret scan, escopo exclusivo de cinco documentos e `git diff --check` também passaram. Build e testes de produto não foram repetidos depois da documentação porque a baseline técnica permaneceu inalterada.

## Próxima atividade

Bruno deve revisar este relatório, principalmente `Resultado em linguagem simples`, `Sequência E2E correlacionada observada`, `Achados classificados`, `Limitações e condições residuais` e `Classificação dos gates`. Se aceitar a classificação automática e desejar continuar, a próxima autoridade segura é exclusivamente uma proposta documental para as amostras humanas finais. Nenhuma amostra, Human Gate, promoção ou transição deve ser autorizada junto com a aceitação deste relatório.
