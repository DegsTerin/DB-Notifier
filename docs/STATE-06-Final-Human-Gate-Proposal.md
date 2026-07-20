# Proposta STATE-06 — Human Gate final

> Resultado posterior: depois de autorizar separadamente a abertura documental, Bruno recebeu o resumo único e decidiu `APROVADO COM RESSALVAS` exclusivamente para o Human Gate final do `STATE-06`. A decisão e suas limitações estão no [relatório factual](STATE-06-Final-Human-Gate-Report.md). Esta proposta permanece como delimitação histórica e não autoriza transição para `STATE-07`.

## Status e autoridade

- Data: 2026-07-20.
- Estado mantido: `STATE-06 INTEGRATION`.
- Baseline automática aprovada: commit `84217c64312a024ec4f286adfe4872184a21849c`.
- Relatório automático aceito: commit `2c1e05fd8ad4dec2174682fee66aafbd92efc6ee`.
- Remediação test-only mais recente aceita: commit `9d654262d7a451d82f049350c99b903f5da1d226`.
- Registro factual dessa aceitação: commit `129b9fd4f7fc774b8ac9616115524660d569ffc9`.
- Repetição humana final e relatório aceito: commit `10a82498d9c04a6eb7a61c08d20af4775bcd9f92`.
- Human Gate final: `PENDENTE` e não aberto.
- Runtime, browser, promoção e transição: não autorizados e não executados nesta atividade.

Bruno aceitou a repetição final de `S06-HG-001` e `S06-HG-006` com as limitações registradas e autorizou exclusivamente esta proposta documental. O documento organiza o resumo, os critérios de elegibilidade, as ressalvas e o formato de decisão futura. Ele não abre, preenche ou decide o Human Gate e não autoriza `STATE-07`.

## Resultado em linguagem simples

O trabalho técnico e as verificações humanas previstos para o fechamento do `STATE-06` foram concluídos dentro do sandbox local autorizado. O Quality Gate automático atual está aprovado, e Bruno aprovou individualmente as seis amostras humanas.

Isso permite preparar a decisão final do estado, mas não permite assumir a resposta. O Human Gate final é o momento em que Bruno receberá um único resumo de tudo o que passou, do que ficou limitado e do que ainda pertence ao `STATE-07` ou a etapas futuras. Ele deverá escolher conscientemente entre:

- `APROVADO`;
- `APROVADO COM RESSALVAS`;
- `REPROVADO`.

Mesmo uma aprovação não muda o estado automaticamente. A eventual transição `STATE-06 → STATE-07` continuará exigindo uma autorização posterior, clara e separada.

## Baseline factual consolidada

| Elemento | Estado factual |
|---|---|
| ciclo de vida | `STATE-06 INTEGRATION` |
| Quality Gate consolidado | `APROVADO` na baseline `84217c6` e aceito com limitações no relatório `2c1e05f` |
| achados críticos, altos ou médios dessa campanha | nenhum aberto |
| regressão histórica no commit `66d0a9f` | permanece `REPROVADA`; não reescrita |
| primeira campanha consolidada | permanece historicamente `BLOQUEADA`; não reescrita |
| remediações posteriores | concluídas, verificadas e aceitas em escopos test-only |
| mudanças de produto depois da baseline automática | nenhuma; `9d65426` alterou somente `tests/`, `scripts/` e documentação |
| amostras humanas `S06-HG-001` a `S06-HG-006` | seis decisões individuais `APROVADA` |
| campanha humana | `CONCLUÍDA COM AS LIMITAÇÕES REGISTRADAS` |
| Human Gate final | `PENDENTE` e não aberto |
| runtime operacional, promoção e transição | não autorizados |

As fontes proprietárias deste resumo são:

- [relatório da nova repetição do Quality Gate consolidado](STATE-06-Consolidated-Quality-Gate-Post-Revocation-Remediation-Rerun-Report.md);
- [relatório original das amostras humanas](STATE-06-Final-Human-Samples-Report.md);
- [relatório da repetição humana final](STATE-06-Final-Human-Samples-Second-Post-Remediation-Repetition-Report.md);
- [relatório da segunda remediação test-only](STATE-06-Final-Human-Samples-Second-Test-Only-Remediation-Report.md);
- [estado factual corrente](../prompts/state/Current-State.md);
- [Quality Gates](../prompts/governance/Quality-Gates.md) e [Lifecycle](../prompts/governance/Lifecycle.md).

## Evidência automática que deverá aparecer no resumo do gate

O futuro resumo único deverá identificar explicitamente:

1. baseline automática `84217c6` e relatório aceito `2c1e05f`;
2. build Release de 17 projetos com zero erro e warning;
3. `332/332` testes unitários, `30/30` de arquitetura e `16/16` de integração da campanha consolidada;
4. Dashboard `60/60`, typecheck e build aprovados;
5. cobertura de `78,9%` de linhas e `49,51%` de branches;
6. cadeia correlacionada Agent → API → Dashboard/SignalR → notificação em sink de teste → transporte deliberadamente não executável;
7. reconexão, replay, duplicidade, reorder, revogação, fencing, budgets, cancelamento e cleanup;
8. duas observações, uma entrega em sink, dois journals e zero `CommandAttempt`;
9. atualização TV imediata, hint seguro, reconciliação independente de 30 segundos e concorrência máxima 1;
10. ausência de achado crítico, alto ou médio aberto na campanha atual;
11. limitações de supply chain offline e de ambiente;
12. histórico bloqueado/reprovado preservado sem reclassificação retroativa.

A segunda remediação `9d65426` não mudou produto. Seu Quality Gate próprio acrescentou `31/31` testes de arquitetura, `19/19` de integração e um gate contínuo de 180 segundos para o runner humano, além da regressão consolidada. Esses números complementam, mas não reescrevem, a campanha automática proprietária da baseline `84217c6`.

## Amostras humanas que deverão aparecer no resumo do gate

| ID | Evidência humana e decisão |
|---|---|
| `S06-HG-001` | Agent disponível → indisponível com uma pendência preservada → recuperado com replay único; Browser → API permaneceu disponível; `APROVADA` |
| `S06-HG-002` | leitura imediata, hint SignalR, releitura autoritativa, reconciliação de 30 segundos, offline/recovery e concorrência 1; `APROVADA` |
| `S06-HG-003` | uma notificação Windows local sintética posterior à baseline; `APROVADA` |
| `S06-HG-004` | duplicata deliberada suprimida sem segunda publicação; `APROVADA` |
| `S06-HG-005` | comando sintético não executável recusado, journals presentes e zero `CommandAttempt`; `APROVADA` |
| `S06-HG-006` | `Desconhecido` corrente separado de `Desatualizado`, origem sintética e suporte planejado visíveis; `APROVADA` |

As imagens apresentadas por Bruno permaneceram na conversa e não foram copiadas ao repositório. As decisões exatas e os valores visíveis estão preservados nos relatórios factuais.

## Limitações e ressalvas obrigatórias

O futuro resumo não poderá solicitar decisão sem apresentar, em linguagem simples, pelo menos estas condições:

1. toda a integração exercitada é local, sintética, determinística e restrita a sandbox;
2. Server SQLite em memória não prova PostgreSQL real, concorrência distribuída ou operação em escala;
3. Agent SQLite, identidades, certificados, assignments, observações e comandos usados nos gates são efêmeros e de teste;
4. nenhum provider, banco monitorado, credencial, PKI, IdP, vault ou Agent operacional foi homologado;
5. SignalR é apenas hint; API e polling de 30 segundos permanecem autoritativos;
6. o sink em memória automático não prova apresentação Windows geral, embora uma notificação sintética visível tenha sido aprovada na amostra humana;
7. o transporte de comando é deliberadamente não executável e não prova comando administrativo, executor, post-probe ou efeito em infraestrutura;
8. uma máquina Windows e uma versão local do Chrome não constituem homologação multi-plataforma, escala ou endurance;
9. auditorias offline não garantem advisories externos publicados depois do cache local disponível;
10. os presenters finais de `S06-HG-001` e `S06-HG-006` terminaram por timeout/expiração depois das decisões humanas, porque o controle final não foi acionado; o cleanup passou, mas o resumo terminal positivo e sua contagem final de origens não foram produzidos;
11. o Human Gate não homologa providers nem autoriza produção, deploy, operação externa, comando real ou MOD-12 `none → OBSERVER`;
12. `ADR-0007` permanece `proposed` e não é implicitamente aceito por este gate;
13. carga representativa, segurança operacional, matriz real por engine/plataforma, HA e disaster recovery pertencem ao `STATE-07` ou a gates posteriores;
14. aprovação do Human Gate não autoriza nem executa a transição para `STATE-07`.

## Arquitetura da futura sessão de Human Gate

A futura sessão será exclusivamente documental e conversacional:

1. executar shutdown preflight e provar worktree limpa;
2. confirmar que o commit informado no hand-off contém `84217c6`, `2c1e05f`, `9d65426`, `129b9fd` e `10a8249`;
3. verificar que não existe mudança técnica posterior sem Quality Gate proporcional;
4. abrir somente os relatórios e regras já existentes;
5. apresentar um único resumo para `STATE-06`, com evidência automática, seis amostras, cobertura pendente e todas as ressalvas obrigatórias;
6. perguntar se Bruno deseja `APROVADO`, `APROVADO COM RESSALVAS` ou `REPROVADO`;
7. exigir uma confirmação inequívoca que nomeie exclusivamente `STATE-06`;
8. registrar factualmente a decisão, sem modificar código ou executar runtime;
9. manter transição, `STATE-07`, promoção, deploy e recursos operacionais fechados.

Nenhum browser, WPF, host, harness ou amostra deverá ser repetido para abrir o gate. Se Bruno pedir nova evidência, a sessão deverá parar e solicitar autorização específica para essa execução.

## Critérios de elegibilidade para abrir o gate

O Human Gate final somente poderá ser apresentado quando:

1. a baseline corrente estiver limpa e contiver todos os commits exigidos;
2. nenhuma alteração técnica posterior tiver invalidado o Quality Gate consolidado ou as amostras;
3. o relatório automático `2c1e05f` continuar classificado `APROVADO` com limitações;
4. não houver achado crítico, alto ou médio atual conhecido e aberto no escopo do `STATE-06`;
5. as seis amostras mantiverem decisões humanas individuais explícitas;
6. as limitações automáticas e humanas estiverem apresentadas sem omissão;
7. uma única decisão for solicitada exclusivamente para `STATE-06`;
8. o texto deixar claro que a decisão não promove estado nem autoriza operação.

Se qualquer item falhar, o gate deverá permanecer `PENDENTE` ou ser apresentado como `BLOQUEADO`, conforme a causa. Nenhuma evidência poderá ser corrigida ou reinterpretada silenciosamente durante a sessão.

## Formato da decisão futura

Depois de receber o resumo completo, Bruno deverá escolher uma das três classificações:

### `APROVADO`

Significa que Bruno considera atendidos os critérios de integração do `STATE-06` e aceita as limitações como fronteiras de fases posteriores, sem tratá-las como suporte operacional já comprovado.

### `APROVADO COM RESSALVAS`

Significa que Bruno considera o estado encerrável, mas deseja registrar nominalmente limitações ou condições que deverão permanecer visíveis no hand-off e no `STATE-07`. As ressalvas deverão ser escritas na própria decisão.

### `REPROVADO`

Significa que Bruno identifica uma lacuna que impede o fechamento do `STATE-06`. A decisão deverá nomear o ponto e não autorizar correção automática.

Uma resposta curta ou uma simples autorização para continuar não decidirá o gate. A confirmação deverá seguir o contrato:

> Confirmo a decisão acima exclusivamente para STATE-06.

## Fora de escopo absoluto

Esta proposta e a futura abertura documental não autorizam:

- execução ou repetição de build, testes, harness, browser, WPF, Agent ou API;
- alteração de código, configuração executável, solução, projetos, packages, lockfiles ou migrations;
- acesso externo, downloads, provider ou banco real;
- credencial, IdP, PKI, vault ou token service operacional;
- notificação, canal externo ou comando administrativo;
- `CommandAttempt`, executor, shell de ação, post-probe ou efeito em serviço/infraestrutura;
- deploy, publicação, instalação, produção ou serviço permanente;
- LLM, recomendação, planejamento, automação ou MOD-12 `none → OBSERVER`;
- aprovação pré-preenchida do Human Gate;
- promoção, `STATE-07` ou qualquer transição de estado.

## Riscos e condições de parada

| Risco | Tratamento obrigatório |
|---|---|
| aceitação de incremento ser confundida com Human Gate do estado | solicitar uma decisão nova, única e nominal para `STATE-06` |
| seis amostras aprovadas serem tratadas como aprovação automática | manter Human Gate `PENDENTE` até a confirmação final explícita |
| limitação de sandbox ser omitida | apresentar integralmente a seção de ressalvas antes da decisão |
| histórico reprovado ser apagado | preservar os relatórios históricos e explicar que a evidência atual é prospectiva |
| terminal não zero dos presenters ser ocultado | registrar timeout/expiração posteriores às decisões e a ausência do resumo final de rede |
| aprovação ser interpretada como suporte operacional | separar integração em sandbox de homologação, produção e provider real |
| gate ser confundido com transição | exigir autorização posterior e separada para `STATE-06 → STATE-07` |
| baseline técnica divergir | parar o gate e solicitar Quality Gate ou revisão proporcional |

## Critérios de aceite desta proposta documental

Esta proposta estará completa quando:

1. identificar a baseline automática, o relatório automático, a remediação e a repetição humana aceitos;
2. resumir a evidência automática sem reexecutá-la;
3. relacionar as seis decisões humanas individuais;
4. distinguir fatos observados, limitações e itens de fases posteriores;
5. definir elegibilidade, formato de decisão e condições de parada;
6. proibir runtime, mudança técnica, promoção e transição;
7. fornecer somente uma autorização futura para abrir o gate, sem preaprovar sua decisão.

## Verificação documental desta proposta

- documentação de código: aprovada para `284` arquivos comment-capable;
- links Markdown: `500` links locais em `112` arquivos aprovados;
- secret scan do worktree não ignorado: aprovado;
- escopo exclusivo de seis documentos e `git diff --check`: aprovados;
- build, testes, runtime e browser: `NÃO APLICÁVEIS` e não executados nesta atividade documental.

## Decisão futura de Bruno

Se Bruno concordar com esta proposta e desejar abrir o Human Gate final em uma atividade posterior, poderá enviar exatamente:

> AUTORIZO exclusivamente a apresentação e abertura documental do Human Gate final do STATE-06 no commit corrente informado no hand-off, cuja ancestralidade deverá conter a baseline automática `84217c6`, o relatório automático aceito `2c1e05f`, a remediação aceita `9d65426`, seu registro factual `129b9fd` e o relatório humano aceito `10a8249`, limitada a shutdown preflight, inspeção read-only de elegibilidade, resumo único da evidência automática, das seis amostras humanas, da cobertura pendente e das limitações registradas, seguido de uma solicitação inequívoca de decisão exclusivamente para o STATE-06. Não autorizo build, testes, runtime, browser, WPF, alteração de arquivos executáveis, acesso externo, correção, promoção ou transição de estado. Se a baseline divergir ou a evidência estiver incompleta, o gate deverá permanecer PENDENTE ou BLOQUEADO e voltar para autorização separada.

Essa autorização futura permitirá somente apresentar o resumo e solicitar a decisão. Ela não escolherá `APROVADO`, `APROVADO COM RESSALVAS` ou `REPROVADO`, não registrará resposta por inferência e não autorizará `STATE-07`.

## Próxima atividade

Nenhuma ação adicional está autorizada por esta proposta. Bruno deverá revisá-la, especialmente `Baseline factual consolidada`, `Limitações e ressalvas obrigatórias`, `Arquitetura da futura sessão de Human Gate`, `Critérios de elegibilidade`, `Fora de escopo absoluto` e `Riscos e condições de parada`.

Se concordar, poderá copiar exatamente a autorização da seção `Decisão futura de Bruno`. Se desejar alterações, deverá indicar somente os pontos documentais a corrigir.
