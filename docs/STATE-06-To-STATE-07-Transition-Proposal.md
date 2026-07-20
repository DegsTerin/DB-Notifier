# Proposta de transição formal — STATE-06 para STATE-07

## Status e autoridade

- Data: 2026-07-20.
- Estado atual mantido: `STATE-06 INTEGRATION`.
- Estado proposto: `STATE-07 TESTING_HOMOLOGATION`.
- Quality Gate consolidado do `STATE-06`: `APROVADO` com limitações.
- Human Gate final do `STATE-06`: `APROVADO COM RESSALVAS`.
- Baseline do Human Gate: commit `1a27dca393f00bc683235d7f8898dcc86f5841e0`.
- Registro factual do Human Gate: commit `96cf2488679c2b8b2abcccf8d6473d07c8c8d823`.
- Transição: `NÃO EXECUTADA` e não autorizada por este documento.
- Build, testes, runtime, browser, acesso externo, implementação, promoção e ação operacional: não autorizados e não executados.

Bruno autorizou exclusivamente esta proposta documental. Ela descreve a mudança formal de fase, o handoff e as fronteiras da próxima etapa. Não altera `Current-State.md` para `STATE-07`, não cria autorização operacional e não concede acesso a provider, banco, credencial, rede ou infraestrutura.

## Resultado em linguagem simples

O `STATE-06` concluiu seus gates: a integração em sandbox foi aprovada automaticamente, as seis amostras humanas foram aprovadas e o Human Gate final foi aprovado com ressalvas. Isso torna o projeto elegível para entrar na fase seguinte.

A transição para `STATE-07` mudará somente a fase oficial de trabalho. Ela não transformará os dados sintéticos em prova de produção, não homologará PostgreSQL ou qualquer outro provider e não permitirá iniciar testes reais sem nova autorização.

No `STATE-07`, o objetivo será provar, de forma incremental e controlada, quais combinações de engine, versão, plataforma, topologia, papel e capability realmente funcionam. Cada campanha deverá declarar exatamente seus alvos, recursos, credenciais de teste, riscos, cleanup e gates antes de executar qualquer coisa.

## Baseline factual de saída do STATE-06

| Elemento | Resultado de saída |
|---|---|
| posição formal | `STATE-06 INTEGRATION` |
| Quality Gate consolidado | `APROVADO` na baseline `84217c6`, relatório aceito `2c1e05f` |
| Human Gate final | `APROVADO COM RESSALVAS`, relatório `96cf248` |
| amostras humanas | `S06-HG-001` a `S06-HG-006` com decisões individuais `APROVADA` |
| achados críticos, altos ou médios atuais no gate consolidado | nenhum aberto |
| histórico | primeira campanha `BLOQUEADA` e repetição `REPROVADA` preservadas |
| composição normal | runtimes de sandbox desabilitados e isolados |
| comando administrativo | não executável; zero `CommandAttempt` nas evidências |
| provider operacional homologado | nenhum |
| MOD-12 | sem promoção `none → OBSERVER` |
| `ADR-0007` | `proposed` |
| worktree no início desta proposta | limpa no commit `96cf248` |
| runtime próprio no preflight | zero processo, listener ou root STATE-06 |

As evidências proprietárias são:

- [relatório da campanha automática consolidada](STATE-06-Consolidated-Quality-Gate-Post-Revocation-Remediation-Rerun-Report.md);
- [relatório da repetição humana final](STATE-06-Final-Human-Samples-Second-Post-Remediation-Repetition-Report.md);
- [relatório do Human Gate final](STATE-06-Final-Human-Gate-Report.md);
- [plano consolidado de fechamento](STATE-06-Consolidated-Closure-Plan.md);
- [estado factual corrente](../prompts/state/Current-State.md);
- [Lifecycle](../prompts/governance/Lifecycle.md), [Quality Gates](../prompts/governance/Quality-Gates.md) e [Segurança e acesso](../prompts/governance/Security-And-Access.md).

## Entregáveis transferidos para STATE-07

O handoff leva como baseline de laboratório, sem transformar em homologação:

1. contratos versionados de identidade, Agent Fleet, observações, snapshot, SignalR hint, notificações e transporte não executável;
2. enrollment e identidades exclusivamente de teste, heartbeat, revogação, catálogo e assignments read-only;
3. persistência SQLite local do Agent, outbox, replay, last-known-good, fencing e quarantine test-only;
4. pipeline sintético Agent → API → Dashboard TV com leitura imediata e reconciliação autoritativa de 30 segundos;
5. SignalR autenticado somente como hint e sem substituir a API;
6. notificação local reconciliada, baseline silenciosa, deduplicação e sink automático de teste;
7. comando deliberadamente não executável com poll, acknowledgement, journal, replay, expiry e incompatibilidade;
8. harness correlacionado, R1–R7, revogação granular, budgets, cancelamento, fault injection e cleanup;
9. relatórios automáticos, amostras humanas, limitações e histórico de remediações;
10. fronteiras fail-closed que mantêm a composição normal desabilitada para os sandboxes.

Esses entregáveis são evidência de integração local. Nenhum deles deve ser anunciado como suporte público de provider, plataforma ou operação.

## Ressalvas herdadas obrigatórias

O `STATE-07` deverá preservar, até evidência própria em contrário:

1. toda a evidência de saída é local, sintética e de sandbox;
2. nenhum provider, engine, versão, plataforma ou topologia está homologado operacionalmente;
3. PostgreSQL real, concorrência distribuída e persistência central operacional não foram comprovados;
4. identidades, certificados, assignments, observações e comandos usados nos gates eram efêmeros e de teste;
5. PKI, IdP, vault, token service, rotação e recuperação operacionais permanecem não exercitados;
6. SignalR continua best-effort e não autoritativo;
7. o transporte de comando permanece não executável e não prova `Start`, `Stop`, `Restart`, executor ou post-probe;
8. uma máquina Windows e uma versão de Chrome não constituem matriz representativa;
9. carga, endurance, HA, disaster recovery e homologação multi-plataforma permanecem pendentes;
10. auditorias offline não garantem advisories externos posteriores ao cache disponível;
11. os presenters humanos finais terminaram por condições bounded depois das decisões, embora o cleanup tenha passado;
12. `ADR-0007` continua `proposed`;
13. MOD-12 permanece em `none`, sem autorização de `OBSERVER`;
14. produção, deploy, publicação e release pertencem a gates posteriores.

As ressalvas não são falhas ocultas. Elas definem precisamente o trabalho de homologação ainda necessário e impedem alegações prematuras de suporte.

## Objetivo e entregáveis de STATE-07

Conforme o Lifecycle, `STATE-07 TESTING_HOMOLOGATION` tem como objetivo validar funcionalidade, segurança, carga e operação representativa. Seus entregáveis futuros deverão incluir:

- matriz explícita por provider/engine, versão, plataforma, topologia, papel, transporte e capability;
- testes positivos e negativos de autenticação, autorização, revogação, rotação e menor privilégio;
- testes de SSRF, replay, impersonation, command injection, sanitização e supply chain aplicáveis;
- carga, backpressure, fairness, memória, cancelamento, timeout e recuperação;
- falhas parciais, reconnect, restart, reorder, duplicidade, stale/unknown, HA e recovery proporcionais ao alvo;
- acessibilidade e comportamento representativo das interfaces nos ambientes selecionados;
- documentação de configuração, credenciais por referência, setup, cleanup, rollback e evidência sanitizada;
- relatório de homologação que marque cada célula como homologada, limitada, reprovada, bloqueada ou não executada;
- Quality Gate automático e Human Gate próprios do `STATE-07`.

A presença de um engine no roadmap não o coloca automaticamente na matriz executada. Uma combinação somente poderá ser chamada de homologada depois de passar seus critérios específicos.

## Critérios de entrada para a transição formal

A transição somente poderá ser registrada quando:

1. o commit corrente estiver limpo e contiver `84217c6`, `2c1e05f`, `10a8249`, `1a27dca` e `96cf248`;
2. nenhuma mudança técnica posterior tiver invalidado os gates;
3. o Quality Gate do `STATE-06` continuar `APROVADO`;
4. o Human Gate final continuar `APROVADO COM RESSALVAS`;
5. as ressalvas herdadas estiverem incluídas no handoff;
6. a decisão futura nomear explicitamente `STATE-06 → STATE-07`;
7. a autoridade futura limitar a transição a registros documentais de ciclo de vida;
8. ficar explícito que nenhuma campanha, runtime ou acesso de `STATE-07` nasce dessa transição.

Se houver divergência, mudança técnica não auditada, worktree suja ou autoridade ambígua, a transição deverá permanecer `PENDENTE` ou `BLOQUEADA` e retornar para decisão separada.

## Efeito documental da futura transição

Se autorizada, a atividade de transição poderá somente:

1. executar shutdown preflight e inspeção read-only de elegibilidade;
2. alterar `prompts/state/Current-State.md` para `STATE-07 TESTING_HOMOLOGATION`;
3. acrescentar uma entrada append-only em `prompts/state/State-Transition-Log.md`;
4. atualizar o status factual deste handoff e dos índices estritamente necessários;
5. executar gates documentais, secret scan e `git diff --check`;
6. criar um commit local focado da transição.

Ela não poderá alterar código, solução, projetos, configuração executável, packages, lockfiles ou migrations.

## Primeira atividade depois da transição

A primeira atividade segura do `STATE-07` deverá ser outra proposta documental: um plano mestre de homologação que priorize matrizes incrementais, ambientes descartáveis e critérios de parada.

Esse plano deverá propor, sem executar:

- qual provider/engine será o primeiro candidato;
- versões e plataformas exatas;
- topologias e capabilities a testar;
- recursos locais ou externos necessários;
- credenciais exclusivamente de teste e cofre aprovado;
- riscos, custos, limites de máquina e cleanup;
- testes de segurança, carga, falha, recuperação e acessibilidade;
- Quality Gate e Human Gate da campanha.

Transição de estado não é autorização para elaborar ou executar esse plano além do registro mínimo de handoff, salvo se Bruno conceder nova autoridade.

## Fora de escopo absoluto

Esta proposta e a futura transição formal não autorizam:

- build, testes, restore, downloads ou novas dependências;
- execução de Agent, API, Dashboard, WPF, browser, provider ou banco;
- Docker, container, VM, serviço, worker ou listener;
- acesso externo, registry, cloud, rede remota ou infraestrutura corporativa;
- credencial, certificado, PKI, IdP, vault ou secret operacional;
- provider real, banco monitorado, PostgreSQL central ou migration externa;
- `Start`, `Stop`, `Restart`, `CommandAttempt`, executor, shell de ação ou post-probe;
- notificação ou canal externo;
- carga, stress, endurance, HA, DR ou teste destrutivo;
- deploy, publicação, instalação, produção ou release;
- LLM, recomendação, automação ou MOD-12 `none → OBSERVER`;
- aceitação de `ADR-0007`;
- `STATE-08` ou qualquer promoção automática.

Cada item futuro exigirá autoridade específica e proporcional.

## Riscos e condições de parada

| Risco | Tratamento obrigatório |
|---|---|
| transição ser confundida com homologação | registrar suporte atual como `No`/`None` até evidência por célula |
| fase permitir acesso externo implicitamente | exigir autorização específica por ambiente, recurso e alvo |
| provider do roadmap ser anunciado como suportado | manter matriz factual e linguagem de suporte planejado |
| credencial de monitoramento ser reutilizada para administração | separar identidades, referências e permissões |
| SQLite interno ser confundido com provider SQLite | manter papéis e evidências independentes |
| sandbox ser tratado como prova de escala | criar campanhas de carga próprias e bounded |
| comando não executável ser promovido a executor | exigir capability, RBAC, confirmação, auditoria e post-probe homologados |
| ressalvas do Human Gate desaparecerem | transportar todas para o plano e relatórios de `STATE-07` |
| mudança técnica aparecer antes da transição | parar e solicitar Quality Gate proporcional |
| estado avançar sem autorização inequívoca | manter `Current-State.md` em `STATE-06` até decisão exata |

## Critérios de aceite desta proposta

Esta proposta estará completa quando:

1. demonstrar que Quality Gate e Human Gate do `STATE-06` estão encerrados;
2. preservar todas as ressalvas aceitas;
3. definir claramente o efeito limitado da transição;
4. separar transição de qualquer trabalho executável do `STATE-07`;
5. definir critérios de entrada, parada e rollback documental;
6. fornecer um handoff verificável e provider-neutral;
7. apresentar uma única autorização futura sem executar a transição.

## Verificação documental desta proposta

- documentação de código: aprovada para `284` arquivos comment-capable;
- links Markdown: `514` links locais em `114` arquivos aprovados;
- secret scan do worktree não ignorado: aprovado;
- escopo exclusivo de seis documentos e `git diff --check`: aprovados;
- build, testes, runtime, browser e acesso externo: `NÃO APLICÁVEIS` e não executados nesta atividade documental.

## Decisão futura de Bruno

Se Bruno concordar com esta proposta e desejar efetuar a transição formal em atividade posterior, poderá enviar exatamente:

> AUTORIZO exclusivamente a transição formal do DB-Notifier de STATE-06 INTEGRATION para STATE-07 TESTING_HOMOLOGATION no commit corrente informado no hand-off, cuja ancestralidade deverá conter a baseline automática `84217c6`, o relatório automático aceito `2c1e05f`, o relatório humano aceito `10a8249`, a proposta do Human Gate `1a27dca` e o registro do Human Gate aprovado com ressalvas `96cf248`. A atividade fica limitada a shutdown preflight, inspeção read-only de elegibilidade, atualização factual de Current-State, State-Transition-Log, handoff e índices estritamente necessários, gates documentais e commit local. Esta transição não autoriza build, testes, runtime, browser, WPF, Docker, acesso externo, provider ou banco real, credenciais, implementação, correção, homologação, deploy, MOD-12, promoção adicional ou ação operacional. Qualquer campanha ou plano executável de STATE-07 exigirá autorização posterior e separada. Se a baseline divergir, a transição deverá permanecer BLOQUEADA.

Essa decisão futura autorizará somente a mudança formal de fase. Ela não homologará qualquer provider e não iniciará atividades técnicas do `STATE-07`.

## Próxima atividade

Nenhuma transição está autorizada por este documento. Bruno deverá revisar principalmente `Baseline factual de saída`, `Ressalvas herdadas obrigatórias`, `Critérios de entrada`, `Efeito documental`, `Fora de escopo absoluto` e `Riscos e condições de parada`.

Se concordar, poderá copiar exatamente o texto da seção `Decisão futura de Bruno`. Se desejar alterações, deverá indicar somente os pontos documentais a corrigir.
