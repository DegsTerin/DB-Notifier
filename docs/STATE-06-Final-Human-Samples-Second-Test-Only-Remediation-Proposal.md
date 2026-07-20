# Proposta STATE-06 — Segunda remediação test-only das amostras humanas finais

> Execução posterior: Bruno autorizou separadamente a implementação test-only proposta. O Quality Gate próprio foi concluído como `APROVADO` com limitações, sem repetir amostras humanas; o resultado está no [relatório factual](STATE-06-Final-Human-Samples-Second-Test-Only-Remediation-Report.md). O texto abaixo permanece como delimitação histórica e não autoriza nova execução, repetição humana, Human Gate, promoção ou transição.

## Status e autoridade

- Data: 2026-07-20.
- Estado mantido: `STATE-06 INTEGRATION`.
- Baseline documental: commit `9b86923d3c80cfbdade14464910e1197b57483b7`.
- Remediação automática anterior aceita: commit `dd420d1`.
- Registro factual da aceitação anterior: commit `cfd620f`.
- Repetição humana posterior: `S06-HG-001` e `S06-HG-006` continuam `BLOQUEADAS`.
- Tipo: proposta exclusivamente documental de uma segunda remediação test-only.
- Implementação, build, testes de produto, runtime, browser e acesso externo: `NÃO AUTORIZADOS` por esta proposta.
- Repetição humana, Human Gate final, promoção e transição: `NÃO AUTORIZADOS` e não inferidos.

Bruno autorizou exclusivamente a elaboração desta proposta, cobrindo alinhamento da cadência da página ao rate limit, restauração positiva do estado Browser → API, teste de duração humana e runner visível versionado com barriers persistentes. Este documento não implementa a correção e não autoriza sua própria execução.

## Resultado em linguagem simples

A primeira remediação provou automaticamente os comportamentos corretos, mas sua página de apresentação consulta a API rápido demais. Em cerca de 25 segundos, ela pode consumir as 100 leituras permitidas por minuto. Quando a API começa a responder `429` — que significa “muitas solicitações” — a página escreve `Browser → API indisponível` e não volta a escrever “disponível” depois que uma leitura funciona novamente.

Isso criou uma contradição na amostra humana: o Agent ainda estava disponível e o Dashboard continuava acessível, mas a faixa auxiliar dizia que o browser estava indisponível. A amostra foi corretamente interrompida, sem pedir uma aprovação baseada em uma tela falsa.

A segunda lacuna é de condução da sessão. O runner existente é automático e abre Chrome oculto. Para apresentar as telas a Bruno foi necessário usar um helper transitório; ele não permaneceu ativo até o hand-off de `S06-HG-006`. A solução proposta é um runner visível, versionado no repositório, que mantenha cada etapa parada até avanço explícito e faça cleanup ao concluir ou expirar.

A futura correção continuará somente em `tests/` e `scripts/`. Ela não mudará a API, o Dashboard ou qualquer runtime normal do produto.

## Baseline factual da inspeção direta

### Página de evidência e rate limit

A inspeção read-only de `AgentFleetApiEndToEndTests.ConsolidatedHarness.cs` confirmou:

1. `BuildHumanEvidencePage` executa uma leitura imediata e depois usa `setInterval(refresh, 250)`;
2. essa cadência tenta até quatro leituras por segundo, ou 240 por minuto;
3. o endpoint usa a política compartilhada `HumanApiRateLimit`;
4. a fixture define essa política como janela fixa de um minuto, `PermitLimit = 100` e fila zero;
5. qualquer resposta não bem-sucedida entra no mesmo `catch` e escreve `Browser → API indisponível`;
6. o caminho de sucesso atualiza estágio, transporte e contagens, mas não restaura `Browser → API disponível`;
7. a repetição humana observou `233` respostas `429`, confirmando que a divergência não é apenas teórica.

O limite não deve ser aumentado para esconder o problema. A página e o auditor devem respeitar um orçamento explícito, sem sobreposição e com folga para Dashboard, controles e reconciliação.

### Runner e apresentação visível

A inspeção read-only de `run-state06-consolidated-e2e.ps1` e do auditor correspondente confirmou:

1. o runner aceita somente `Consolidated` ou `FinalHumanSamplesRemediation`;
2. ambos são fluxos automáticos e iniciam Chromium com `--headless=new`;
3. o auditor avança perda, recovery e verdade visual sem aguardar decisão humana;
4. o auditor consulta diretamente o endpoint de evidência enquanto a própria página também o consulta;
5. não existe no repositório um runner visível proprietário das etapas humanas;
6. o helper improvisado durante a repetição não é artefato versionado nem prova reprodutível;
7. a cadeia `unknown` corrente versus `stale` já passou automaticamente, mas não chegou a um hand-off humano válido.

### Fronteira arquitetural

Os fatos observados continuam indicando uma correção exclusivamente test-only:

- página, endpoint, estado correlacionado e host pertencem a `tests/`;
- runner e auditor pertencem a `scripts/`;
- a regra normal do Dashboard para `unknown`, `stale`, polling e SignalR não precisa mudar;
- a composição normal não referencia o marker test-only;
- não há justificativa para alterar `src/`, solução, projetos, packages, lockfiles ou migrations.

Se uma implementação futura contradisser essa fronteira, ela deverá parar como `BLOQUEADA` e solicitar nova autorização.

## Problemas que a futura remediação deverá resolver

| ID | Problema factual | Resultado futuro mínimo |
|---|---|---|
| `HGR2-001` | polling de 250 ms excede o rate limit | página usa cadência serial de no máximo 30 leituras por minuto e mantém margem mensurável |
| `HGR2-002` | sucesso posterior não restaura o rótulo | uma leitura `200` válida sempre restaura explicitamente `Browser → API disponível` |
| `HGR2-003` | `429`, negação e falha de transporte têm o mesmo texto | estados tipados e factuais distinguem limitação, acesso negado e indisponibilidade |
| `HGR2-004` | gate automático termina antes de revelar degradação temporal | ensaio contínuo de no mínimo 180 segundos atravessa janelas reais do limitador |
| `HGR2-005` | runner atual é headless e autoavança | runner visível versionado mantém barriers até avanço explícito ou timeout |
| `HGR2-006` | `S06-HG-006` dependeu de helper transitório | apresentação `unknown`/`stale` nasce e termina no runner versionado |

## Decisão da fronteira `tests/scripts` versus `src`

A implementação futura deverá permanecer limitada a:

- página, estado e endpoints test-only no harness correlacionado existente;
- testes de integração e arquitetura existentes;
- runner e presenter/auditor locais sob `scripts/`;
- documentação factual e registros correspondentes.

Arquivos sob `src/` poderão ser lidos apenas para confirmar que a semântica normal continua inalterada. Nenhuma correção visual, alteração de política de produto ou novo mecanismo de runtime normal faz parte deste incremento.

## Arquitetura proposta

### 1. Cadência serial e orçamento de requisições

A página de evidência deverá substituir `setInterval` por um agendamento serial que somente agenda a próxima leitura depois de a anterior terminar.

Contrato proposto:

- intervalo normal mínimo de dois segundos;
- no máximo 30 leituras do endpoint de evidência por minuto;
- concorrência máxima de leitura igual a `1`;
- deadline e cancelamento por leitura;
- nenhum polling direto paralelo pelo presenter humano;
- controles humanos limitados às transições previstas da amostra;
- contadores sanitizados no host para total por rota, `429` e concorrência máxima;
- margem suficiente sob os 100 permits/minuto compartilhados para a leitura inicial, Dashboard TV, controles e reconciliação.

O runner automático poderá observar o DOM já atualizado pela página. Ele não deverá duplicar o polling de evidência a cada 250 ms. Esperas locais em CDP que apenas leem o DOM não consomem o rate limit da API.

### 2. Estado factual Browser → API

A página deverá representar pelo menos quatro resultados distintos:

| Resultado HTTP/transporte | Texto factual proposto | Efeito sobre a última evidência válida |
|---|---|---|
| `200` com contrato válido | `Browser → API disponível` | atualiza dados e restaura explicitamente o estado positivo |
| `429` | `Browser → API temporariamente limitada pelo sandbox` | preserva a última evidência, aplica backoff e não a chama de indisponível |
| `401`/`403` | `Browser → API: acesso de teste negado` | preserva dados anteriores como não atuais e falha fechada |
| timeout, erro de rede ou `5xx` | `Browser → API indisponível` | preserva a última evidência com freshness explícita |

Uma resposta válida posterior deverá remover o motivo transitório e restaurar o texto positivo. Essa recuperação não poderá alterar nem reinterpretar o estado separado de Agent → API.

Os valores apresentados antes da recuperação deverão conservar o horário da última leitura válida. Falha de transporte não poderá fabricar freshness, apagar pendências ou avançar barriers.

### 3. Backoff, fencing e cancelamento

- `429` deverá respeitar `Retry-After` quando o host o fornecer; sem esse header, usará backoff test-only limitado e nunca menor que a cadência normal;
- somente a leitura mais nova poderá atualizar o DOM;
- resultado tardio de uma requisição cancelada não poderá sobrescrever um estágio posterior;
- mudança de estágio poderá solicitar leitura imediata somente depois de concluir a leitura corrente;
- fechamento da página, expiração da sessão ou cleanup cancelará fetch, timers e presenter;
- uma transição repetida será idempotente ou recusada com disposition tipada.

### 4. Gate de duração humana

O Quality Gate futuro deverá manter a página e o Dashboard TV ativos por pelo menos 180 segundos, atravessando mais de uma janela fixa real do limitador.

Durante esse ensaio deverá comprovar:

- zero `429` em operação saudável;
- Browser → API continuamente disponível;
- no máximo 30 leituras de evidência por minuto;
- concorrência de leitura máxima `1`;
- Dashboard TV e reconciliação continuam funcionais;
- nenhuma origem HTTP externa;
- nenhum avanço automático de barrier humano;
- cleanup completo ao final.

Um teste negativo separado e inequivocamente automático deverá introduzir exatamente uma resposta `429` controlada, confirmar o texto de limitação e depois confirmar que a próxima leitura válida restaura `Browser → API disponível`. Essa falha injetada não poderá ser usada como evidência humana nem permanecer habilitada no modo de apresentação.

### 5. Runner visível versionado

Um runner proprietário sob `scripts/` deverá substituir comandos transitórios. Ele poderá ser um modo explicitamente humano do runner existente ou um script dedicado, desde que tenha uma única fonte de lifecycle e cleanup.

Contrato obrigatório:

- seleção exata de uma amostra: `S06-HG-001` ou `S06-HG-006`;
- Chrome dedicado visível, perfil efêmero e origem HTTPS loopback;
- presenter Node versionado que injeta apenas os headers de teste necessários, observa rede e permanece ativo até conclusão, cancelamento ou timeout;
- uma amostra por sessão, sem repetir `S06-HG-002` a `S06-HG-005`;
- nenhuma transição automática depois do estágio inicial;
- barriers mantidos no processo até ação explícita e recusados fora de ordem;
- indicação visível de estágio, última leitura válida, tempo restante e caráter `test-only`;
- deadline total limitado, saída sanitizada e cleanup em `finally`;
- zero reutilização do browser/perfil comum do utilizador.

“Persistente” significa persistente durante a sessão bounded do host: o estágio não avança sozinho enquanto Bruno observa. Não significa persistência operacional nem sobrevivência a reinício; qualquer reinício invalida o run ID e exige uma nova sessão integral.

### 6. Controles de apresentação exclusivamente test-only

A própria página auxiliar poderá oferecer controles de avanço estritamente stage-gated, com nomes como:

- `Apresentar perda sintética do Agent`;
- `Apresentar recuperação sintética do Agent`;
- `Apresentar unknown e stale`;
- `Encerrar amostra e limpar laboratório`.

Esses controles não são comandos administrativos do DB-Notifier. Eles somente acionam as transições sintéticas já existentes no harness, não criam `CommandAttempt`, não executam shell e não afetam serviço, banco ou infraestrutura. Devem ser visualmente rotulados como controles do laboratório, possuir nomes acessíveis e ficar desabilitados durante uma transição.

O presenter deverá manter a autenticação de teste na mesma origem e comprovar que uma página sem identidade, run ID ou ordem correta recebe negação.

### 7. Fluxo futuro de `S06-HG-001`

1. runner inicia host e Chrome visível na etapa `agent-transport-ready`;
2. página permanece estável até Bruno observar Agent disponível, Browser → API disponível, zero pendência e uma amostra Server;
3. avanço explícito cria exatamente uma observação pendente com Agent → API indisponível;
4. barrier preserva o último snapshot válido e Browser → API disponível;
5. avanço explícito recupera o transporte e comprova replay único, outbox vazia e duas amostras Server;
6. runner mantém a etapa recuperada até a decisão humana ou timeout;
7. encerramento limpa todos os recursos e registra somente evidência sanitizada.

Nenhum passo colocará o browser offline. Um `429` durante a apresentação saudável bloqueia a amostra e não poderá ser ignorado ou reinterpretado.

### 8. Fluxo futuro de `S06-HG-006`

1. nova sessão seleciona exclusivamente `S06-HG-006`;
2. runner ativa a fonte visual test-only pela transição stage-gated existente;
3. Dashboard TV apresenta simultaneamente `Desconhecido` corrente e `Desatualizado`;
4. página auxiliar mantém visíveis suporte planejado, origem sintética e ausência de dados externos;
5. um prazo visual informa quando o item `unknown` deixará de ser corrente;
6. se o prazo expirar, a amostra muda para `EXPIRADA`, não renova silenciosamente timestamps e exige nova sessão;
7. encerramento remove host, presenter, Chrome, perfil e stores temporários.

Esse fluxo não altera `StatusBadge`, o limiar normal de cinco minutos nem a fixture normal do Dashboard.

### 9. Isolamento e cleanup

- marker exato, run ID UUID v4, identidade exclusivamente de teste e HTTPS loopback continuam obrigatórios;
- composição normal, WPF normal e Dashboard normal não podem descobrir o runner ou os controles;
- nenhum log poderá conter token, header, certificado, chave, payload, path local completo ou texto de exceção não sanitizado;
- PIDs, listeners, perfil, stores, certificados e roots serão atribuídos antes da abertura;
- o runner deverá encerrar filhos antes do host e remover somente paths comprovadamente próprios;
- falha de cleanup reprovará a remediação e impedirá nova repetição humana.

## Escopo futuro proposto

Uma implementação posterior poderá alterar exclusivamente:

- `tests/DBNotifier.IntegrationTests/AgentFleetApiEndToEndTests.ConsolidatedHarness.cs` e parciais test-only proprietários;
- testes existentes sob `tests/DBNotifier.IntegrationTests/` e `tests/DBNotifier.Architecture.Tests/`;
- `scripts/run-state06-consolidated-e2e.ps1` apenas se o modo automático existente for preservado;
- um runner/presenter humano versionado e o auditor automático correspondente sob `scripts/`;
- relatórios e registros factuais Markdown.

Não deverá haver novo projeto, package, dependência, lockfile ou migration. Reutilização de Node, PowerShell 7, CDP e Chrome já disponíveis deverá permanecer integralmente offline.

## Testes futuros obrigatórios

| Área | Evidência mínima |
|---|---|
| orçamento | página consome no máximo 30 leituras/minuto sob política real de 100/minuto |
| serialização | concorrência máxima `1`; nenhuma resposta tardia sobrescreve estado novo |
| duração | 180 segundos contínuos, mais de uma janela do limitador e zero `429` saudável |
| recovery positivo | `429` controlado → texto limitado → próxima resposta válida → texto disponível |
| falhas tipadas | `401/403`, `429`, timeout/rede e `5xx` não usam a mesma mensagem |
| preservação | última evidência e seu horário permanecem factuais durante falha |
| runner | Chrome visível dedicado, presenter persistente, uma amostra e nenhum autoavanço |
| barriers | etapas fora de ordem e repetidas são negadas ou idempotentes de forma tipada |
| `S06-HG-001` | disponível → uma pendência preservada → replay único, sempre com browser online |
| `S06-HG-006` | `unknown` corrente e `stale` separados; expiração invalida em vez de renovar |
| acessibilidade | controles e estados têm texto, foco, nome acessível e não dependem de cor |
| segurança | loopback, identidade, run ID, origem e sanitização recusam entradas inválidas |
| isolamento | zero referência na composição normal e zero alteração sob `src/` |
| regressão | modo automático anterior, R1–R7 e cadeia consolidada continuam passando |
| cleanup | zero processo, listener, browser/perfil, store, certificado, log ou root residual |

O Quality Gate próprio poderá usar falhas sintéticas e browser dedicado automático. Ele não repetirá as decisões humanas e não poderá preencher `S06-HG-001` ou `S06-HG-006` como aprovadas.

## Critérios de aceite da futura remediação

A segunda remediação somente poderá ser aceita se:

1. a causa do `429` saudável for eliminada por consumo abaixo do orçamento, sem aumentar silenciosamente o limitador;
2. polling for serial, cancelável e limitado a 30 leituras/minuto;
3. resposta `200` válida restaurar explicitamente Browser → API disponível;
4. `429`, negação e indisponibilidade receberem estados factuais distintos;
5. o ensaio de 180 segundos passar sem `429` em fluxo saudável;
6. a recuperação depois de um `429` controlado for observada automaticamente;
7. o runner visível e seu presenter forem arquivos versionados, não comandos improvisados;
8. cada barrier permanecer estável até avanço explícito, timeout ou cancelamento;
9. uma sessão puder apresentar somente uma das duas amostras;
10. `S06-HG-001` preservar último snapshot e browser online durante perda do Agent;
11. `S06-HG-006` apresentar `unknown` corrente e `stale` sem renovar timestamp silenciosamente;
12. nenhuma amostra já aprovada for repetida;
13. nenhuma mudança ocorrer sob `src/`, solução, projetos, packages, lockfiles ou migrations;
14. testes aplicáveis de integração, arquitetura, browser, segurança e regressão passarem offline;
15. cleanup terminar sem resíduo atribuído ao DB-Notifier;
16. relatório distinguir observado, inferido, não testado e bloqueado;
17. nenhuma decisão humana, Human Gate, promoção ou transição for inferida.

Mesmo aprovada, a remediação apenas tornará as duas amostras novamente elegíveis. A repetição humana exigirá autorização separada.

## Fora de escopo absoluto

- qualquer alteração sob `src/`;
- mudança da API, Dashboard, WPF, Agent Worker ou composição normal;
- aumento de rate limit para mascarar consumo excessivo;
- mudança da regra normal `stale > status`, do limiar de cinco minutos ou da fixture normal;
- solução, projeto, package, lockfile, migration, schema, nova dependência ou download;
- acesso externo, CDN, registry, recurso ou credencial operacional;
- provider, banco, Agent, PKI, IdP, vault, monitoramento ou serviço operacional;
- notificação Windows, canal externo, SignalR novo ou repetição de `S06-HG-002` a `S06-HG-005`;
- comando administrativo, `CommandAttempt`, executor, shell de ação, post-probe ou efeito real;
- LLM, recomendação, automação ou promoção `none → OBSERVER`;
- repetição humana, Human Gate final, promoção para `STATE-07` ou transição.

## Riscos e limitações residuais

- Três minutos provam estabilidade além da falha observada e através de janelas do limiter; não representam uso de longa duração ou carga operacional.
- O orçamento compartilha uma política por IP loopback. Novas rotas test-only futuras exigiriam recalcular a margem, não copiar o valor cegamente.
- Um `429` injetado prova a lógica visual de recuperação; o ensaio saudável separado é que prova compatibilidade com o limiter real.
- O runner visível prova reprodutibilidade do laboratório, não uma funcionalidade do produto.
- Barriers persistem somente dentro da sessão limitada; reinício é uma nova execução, não recuperação durável.
- `unknown` continua sendo temporal. Uma observação humana demorada pode expirar corretamente e exigir nova sessão.
- Um Chrome e uma máquina Windows não constituem homologação ampla.
- SQLite e identidades efêmeros não representam PostgreSQL, PKI, concorrência ou escala operacional.
- A conclusão automática da remediação não aprova as duas amostras nem abre o Human Gate final.

## Condições de parada

O incremento futuro deverá parar como `BLOQUEADO` se:

- exigir mudança em `src/`, solução, projeto, package, lockfile ou migration;
- precisar aumentar o rate limit em vez de reduzir e serializar o consumo;
- o runner depender novamente de helper não versionado ou comando manual improvisado;
- a página não conseguir distinguir limitação, negação e indisponibilidade;
- qualquer estágio avançar sem ação explícita ou puder ser sobrescrito por resposta tardia;
- o teste de duração produzir `429`, sobreposição ou perda de freshness;
- a amostra `unknown` renovar silenciosamente seu timestamp;
- surgir acesso externo, recurso operacional, comando, notificação ou efeito real;
- secret, certificado, header, payload ou path puder entrar na evidência;
- timeout, cancelamento ou cleanup deixar resíduo;
- houver mudança preexistente conflitante no worktree.

## Entregáveis futuros

- página test-only com polling serial, estados factuais e recuperação positiva;
- instrumentação sanitizada de budget, `429` e concorrência;
- teste automático contínuo de 180 segundos e teste negativo de recovery;
- runner e presenter visíveis versionados, limitados a uma amostra por sessão;
- barriers stage-gated para `S06-HG-001` e `S06-HG-006`;
- testes de segurança, arquitetura, integração, browser, acessibilidade, regressão e cleanup;
- relatório factual próprio e registros documentais;
- commit local focado, sem código de produto ou transição.

## Ordem das atividades futuras

1. Bruno revisa esta proposta.
2. Se concordar, autoriza separadamente somente a implementação test-only e seu Quality Gate.
3. O executor faz novo preflight, implementa exclusivamente `tests/`/`scripts/`, testa e limpa tudo.
4. Bruno revisa e aceita ou rejeita o relatório técnico da remediação.
5. Somente depois poderá autorizar nova repetição humana de `S06-HG-001` e `S06-HG-006`.
6. As duas amostras são apresentadas e decididas separadamente, sem repetir as quatro já aprovadas.
7. Se todas as seis estiverem concluídas, uma proposta separada apresenta o Human Gate final.

Nenhuma etapa concede automaticamente a seguinte.

## Classificação futura

- `APROVADO`: todos os critérios test-only passam e as duas amostras ficam tecnicamente elegíveis;
- `REPROVADO`: verdade factual, budget, determinismo, isolamento, segurança ou cleanup falham;
- `BLOQUEADO`: o resultado exige autoridade ou fronteira fora do escopo;
- `NÃO APLICÁVEL`: somente para verificação realmente externa, com justificativa explícita.

## Verificação desta proposta

As verificações locais desta atividade exclusivamente documental concluíram:

- documentação de código: `APROVADO`, 282 arquivos comment-capable;
- links Markdown: `APROVADO`, 480 links locais em 109 arquivos;
- secret scan do worktree não ignorado, sem histórico: `APROVADO`;
- `git diff --check`: `APROVADO`;
- escopo: somente quatro arquivos Markdown de proposta, índice, estado e histórico.

Build, testes de produto, harness, runtime e browser foram deliberadamente `NÃO EXECUTADOS` porque não pertencem à autoridade documental desta atividade.

## Decisão futura de Bruno

Se Bruno concordar e desejar autorizar somente a segunda remediação test-only, o texto sugerido é:

> AUTORIZO o incremento restrito de segunda remediação do STATE-06 — Final Human Samples Test-only Presentation Stability and Persistent Review Runner, limitado ao harness correlacionado e runners/auditores sob `tests/` e `scripts/`, com polling serial de no máximo 30 leituras de evidência por minuto, concorrência máxima 1, estados factuais distintos para sucesso, rate limit, negação e indisponibilidade, restauração explícita de Browser → API disponível após leitura válida, backoff, fencing, cancelamento, teste contínuo mínimo de 180 segundos, falha `429` controlada e runner Chrome visível versionado com uma amostra por sessão e barriers persistentes até avanço explícito. Autorizo somente testes automáticos offline, runtimes temporários locais e Chrome dedicado com perfil efêmero para o Quality Gate próprio, todos encerrados e removidos ao final. Se qualquer critério exigir alteração sob `src/`, solução, projetos, packages, lockfiles ou migrations, o incremento deverá parar como BLOQUEADO e solicitar nova autorização. Permanecem proibidos acesso externo, downloads, recursos ou credenciais operacionais, provider/banco/Agent operacional, composição normal, notificação Windows, repetição de `S06-HG-002` a `S06-HG-005`, comandos administrativos, CommandAttempt, executor, shell de ação, serviço ou infraestrutura afetados, canal externo, LLM, deploy, repetição humana, Human Gate final, promoção e transição de estado.

Essa autorização futura liberaria somente implementação test-only, Quality Gate automático e relatório factual. Não abriria as amostras humanas.

## Próximo passo para Bruno

1. Abra esta proposta e leia principalmente `Resultado em linguagem simples`, `Baseline factual da inspeção direta`, `Arquitetura proposta`, `Critérios de aceite da futura remediação`, `Fora de escopo absoluto` e `Riscos e limitações residuais`.
2. Se desejar alterações, informe somente os pontos que devem mudar.
3. Se concordar e quiser implementar a remediação, copie exatamente o texto da seção `Decisão futura de Bruno`.
4. Não autorize junto a repetição humana, o Human Gate final, promoção ou transição.

Enquanto a proposta estiver em revisão, nenhuma ação técnica adicional é necessária ou autorizada.
