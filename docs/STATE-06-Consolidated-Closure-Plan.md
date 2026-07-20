# Plano consolidado de fechamento do STATE-06

## Status e autoridade

- Data: 2026-07-18
- Estado mantido: `STATE-06 INTEGRATION`
- Tipo: plano exclusivamente documental
- Implementação, alteração de código/configuração executável, build, teste de produto e runtime: `NÃO AUTORIZADOS`
- Acesso externo, recurso operacional, promoção e transição de estado: `NÃO AUTORIZADOS`
- Próxima execução técnica: `PENDENTE DE AUTORIZAÇÃO SEPARADA`

Este documento responde à autorização explícita de Bruno para elaborar um plano único de fechamento do `STATE-06`. Ele consolida as lacunas, a ordem recomendada, os limites e os gates futuros. Não implementa nenhuma das capacidades descritas, não declara o estado concluído e não substitui as autorizações específicas que serão necessárias antes de cada ação técnica.

## Atualização factual posterior — Incremento 1

Depois da publicação deste plano, Bruno emitiu uma autorização separada e explícita para executar somente o `Incremento 1 — Authoritative Observation Pipeline E2E Sandbox`, preservando todas as proibições transcritas abaixo. Essa autorização posterior não nasceu deste documento e não liberou os Incrementos 2–4.

O Incremento 1 foi concluído tecnicamente no sandbox local em 2026-07-19. A cadeia testada usa fonte provider-neutral sintética, processos Agent efêmeros, SQLite/outbox local, HTTPS/mTLS com material somente de teste, ingestão central efêmera e projeção read-only para o contrato TV existente. O relatório proprietário é o [Relatório STATE-06 — Authoritative Observation Pipeline E2E Sandbox](STATE-06-Authoritative-Observation-Pipeline-E2E-Sandbox-Report.md). O Quality Gate automático restrito está aprovado, e Bruno aceitou o Human Gate próprio do incremento com as limitações registradas. `STATE-06 INTEGRATION` não foi encerrado nem promovido.

A aceitação autorizou exclusivamente seu registro factual. Posteriormente, Bruno autorizou separadamente o Incremento 2 e a aquisição controlada do cliente oficial; a implementação local foi concluída no commit `c945c1b`, e Bruno aceitou seu Human Gate próprio com as limitações registradas. Bruno autorizou depois separadamente o [Incremento 3](STATE-06-Reconciled-Local-Notification-Delivery-Sandbox-Report.md), concluído no commit `d43e49a` com Quality Gate automático restrito aprovado e Human Gate próprio aceito com as limitações registradas. O [Incremento 4](STATE-06-Command-Transport-Safety-E2E-Sandbox-Report.md) foi autorizado e concluído localmente no commit final `54a65f5`; seu Quality Gate automático restrito foi aprovado e Bruno aceitou seu Human Gate próprio com as limitações registradas. Qualquer campanha consolidada ou nova atividade técnica depende de decisão posterior, separada e explícita de Bruno.

## Resumo para não especialistas

As principais peças locais já existem: identidade de Agent para teste, heartbeat, assignments read-only, armazenamento SQLite do Agent, recuperação entre processos e um Dashboard TV que consulta uma API sandbox a cada 30 segundos sem sobrepor pedidos. Essas peças foram aceitas nos seus incrementos restritos.

Ainda falta ligá-las numa cadeia completa e segura. A cadeia pretendida começa numa observação fictícia e determinística, passa pelo Agent e pela API, chega ao Dashboard e pode produzir uma notificação local. Um aviso SignalR deverá apenas antecipar uma nova consulta à API; ele nunca será tratado como a verdade. Também falta demonstrar o transporte de comandos sem executar comando algum e, por fim, auditar o `STATE-06` como um todo.

O fechamento recomendado possui quatro incrementos técnicos futuros e um gate final. Este plano evita novas propostas documentais fragmentadas: cada incremento poderá ser autorizado diretamente contra a seção correspondente deste documento. Aceitar um incremento não autoriza automaticamente o seguinte.

## Resultado da análise factual

O `STATE-06` permanece incompleto. A classificação abaixo compara o [Lifecycle](../prompts/governance/Lifecycle.md) com o [estado corrente](../prompts/state/Current-State.md) e com os relatórios já aceitos.

| Requisito de saída | Evidência atual | Classificação | Lacuna para fechamento |
|---|---|---|---|
| Contratos versionados | Contratos Agent Fleet, heartbeat, assignments e snapshot TV possuem versões e validação fail-closed. | Substancialmente atendido | Repetir compatibilidade e divergência de versão na composição E2E final. |
| Autenticação de Agent | Enrollment, certificado P-256 de teste, mTLS e revogação foram exercitados em sandbox. | Atendido no escopo de integração local | Reutilizar a identidade de teste na cadeia integrada; PKI operacional não é requisito deste estado. |
| Sincronização offline | Heartbeat e assignments possuem replay/LKG, retry/backoff, fencing e reinício entre processos no sandbox. | Parcial | Provar observação canônica, outbox e recuperação Agent → API na cadeia completa. |
| E2E em sandbox | Agent Fleet e Dashboard TV foram compostos em sandboxes separados. | Parcial | Provar uma única cadeia fonte sintética → Agent → API → Dashboard/Tray/notificação. |
| Entrega de notificações | Existem demonstrações locais e plumbing de publicação Windows, mas não estão ligados a estado reconciliado da API. | Não atendido para integração autoritativa | Implementar opt-in, baseline silenciosa, deduplicação, entrega local e falhas a partir de transições reconciliadas. |
| TV ligado à API a cada 30 segundos | Leitura imediata, cadência serial, ETag/304, cancelamento, fencing e recuperação foram exercitados em Chrome/HTTPS loopback. | Atendido no sandbox aceito | Preservar esse comportamento na cadeia final e acrescentar somente o hint SignalR. |
| Antecipação segura por SignalR | Não existe cliente SignalR no Dashboard e nenhum hint foi implementado. | Não atendido | Implementar autenticação, coalescência, reconexão e nova leitura autoritativa sem substituir o polling. |
| Reconexão, duplicidade, reorder e versão | Foram exercitados em partes do Agent Fleet e do Dashboard. | Parcial | Repetir os casos nas fronteiras da cadeia completa e do hint SignalR. |
| Expiração de comando | O servidor expira comandos e a inbox do Agent trata replay em testes locais; o polling durável normal continua recusando startup. | Parcial | Executar E2E de transporte, persistência e acknowledgement sem executor ou efeito administrativo. |
| Providers e canais integrados | Há abstrações/provider PostgreSQL não homologado e fixtures locais, mas nenhuma fonte sandbox percorre a cadeia completa; notificações autoritativas estão ausentes. | Parcial | Usar um adapter provider-neutral exclusivamente de teste e o canal local de notificação. Providers reais ficam para `STATE-07`. |
| Quality/Human Gate do estado | Somente gates próprios dos incrementos foram aceitos. | Não atendido | Produzir relatório automático consolidado e depois solicitar um Human Gate exclusivo de saída do `STATE-06`. |

## Definição de fechamento

O `STATE-06` somente estará tecnicamente pronto para decisão humana quando todas as condições abaixo forem verdadeiras:

1. os quatro incrementos futuros deste plano tiverem sido implementados, revisados e aceitos nos seus escopos;
2. uma composição E2E única tiver provado o caminho Agent → API → interfaces/canal com fonte exclusivamente sintética e determinística;
3. os critérios de reconexão, duplicidade, reorder, versão e expiração tiverem evidência controlada;
4. o Dashboard preservar leitura inicial, reconciliação serial de 30 segundos e último snapshot factual, usando SignalR apenas como hint;
5. a notificação local nascer somente de uma transição reconciliada, com opt-in, baseline silenciosa e deduplicação;
6. o transporte de comando terminar em recebimento seguro, `Expired`, `Rejected` ou `Unsupported`, sem executor, attempt operacional, provider ou efeito externo;
7. todos os runtimes temporários, listeners e perfis de navegador tiverem sido encerrados;
8. o Quality Gate consolidado estiver aprovado, sem achado crítico/alto conhecido aberto;
9. o relatório final distinguir claramente o que foi observado, inferido, não testado e reservado para estados posteriores;
10. Bruno tiver revisado as amostras humanas exigidas e emitido uma decisão explícita sobre o Human Gate do `STATE-06`.

Mesmo depois dessas condições, o estado não muda automaticamente. Uma autorização separada será necessária para a transição formal `STATE-06 → STATE-07`.

## Decisões de arquitetura que orientam o plano

### D-01 — A consulta à API continua autoritativa

SignalR transportará somente um hint autenticado e limitado, por exemplo uma revisão opaca que indique que pode haver mudança. O cliente sempre fará nova leitura read-only da API. Perder, repetir ou reordenar hints não poderá alterar a verdade apresentada.

### D-02 — O polling de 30 segundos permanece obrigatório

A reconciliação periódica continuará ativa enquanto o modo TV estiver ativo. Um hint poderá antecipar uma leitura, mas não reiniciar pedidos concorrentes, criar catch-up ilimitado ou desabilitar a leitura periódica.

### D-03 — A fonte integrada será sintética e provider-neutral

O E2E usará um adapter determinístico pertencente somente ao harness de teste para produzir observações canônicas. Ele não será registrado no Worker normal e não provará suporte a engine, driver, banco, credencial ou provider real. Homologação por engine/plataforma pertence ao `STATE-07`.

### D-04 — O transporte de comandos não é um executor

O fechamento do critério de expiração será feito apenas com poll, validação, persistência e acknowledgement autenticados. Nenhum `Start`, `Stop`, `Restart`, shell, driver, serviço, banco ou provider será chamado. Acknowledgement significará somente que o envelope foi recebido e persistido, nunca que a ação foi executada.

### D-05 — Tudo continua desabilitado por padrão

Cada composição futura deverá exigir flags exatas de sandbox, material de teste e loopback. Worker, Dashboard, WPF e API normais continuarão fail-closed fora dessa composição. Nenhum serviço deverá permanecer em background depois dos testes.

### D-06 — Dependências externas não são presumidas

O cliente SignalR não aparece hoje no lockfile do Dashboard. Este plano não seleciona, baixa nem autoriza pacote. Antes do incremento correspondente, deverá ser verificado se uma implementação oficial compatível está disponível localmente. Qualquer download, novo pacote ou acesso externo exigirá autorização explícita e revisão de supply chain.

## Sequência de execução futura

### Incremento 1 — Authoritative Observation Pipeline E2E Sandbox

**Status factual posterior:** implementação local concluída no commit `3449918` em 2026-07-19; Quality Gate automático restrito aprovado; Human Gate próprio aceito por Bruno com as limitações registradas. Consulte o [relatório do incremento](STATE-06-Authoritative-Observation-Pipeline-E2E-Sandbox-Report.md). Esta atualização não altera o texto original de escopo nem autoriza o incremento seguinte.

#### Objetivo

Compor a fonte sintética, o Agent, sua persistência local, a ingestão da API e o read model consumido pelo Dashboard, sem SignalR ou notificação nesta etapa.

#### Escopo futuro permitido por este plano

- adapter provider-neutral determinístico somente no projeto/harness de teste;
- observações canônicas versionadas e limitadas;
- persistência Agent SQLite, outbox e replay idempotente;
- transporte Agent → API sob HTTPS/mTLS exclusivamente de teste;
- persistência central apenas efêmera/local do sandbox;
- projeção read-only que alimente o snapshot TV existente;
- reinício real entre processos, perda de resposta, reconexão, duplicidade, reorder, staleness e incompatibilidade de versão;
- revogação da identidade durante sincronização e preservação do último estado válido;
- cleanup comprovado de processos, listeners, bancos efêmeros e material de teste.

#### Critérios de aceite

1. nenhuma fonte sintética é registrada na composição normal;
2. uma observação aceita aparece no snapshot por meio da API, nunca por ligação direta entre Dashboard e Agent;
3. a desconexão preserva a outbox e a reconexão envia exatamente os itens pendentes;
4. replay exato não duplica efeito e replay conflitante falha fechado;
5. mensagens fora de ordem e versões incompatíveis não substituem evidência válida;
6. revogação impede novas publicações e não transforma dado antigo em fresco;
7. nenhum comando, notificação, SignalR, provider real ou acesso externo ocorre;
8. o runtime normal continua desabilitado e todo runtime de teste é encerrado.

#### Evidência mínima

- testes unitários de contrato e persistência;
- E2E entre processos com relógio controlado;
- contagens de outbox, ingestão e projeção antes/depois de falhas;
- prova sanitizada de zero segredo e zero recurso externo;
- relatório próprio e revisão direta do diff.

### Incremento 2 — Authenticated SignalR Change Hint Sandbox

**Status factual posterior:** depois da proposta documental, Bruno autorizou separadamente a implementação local e o acesso temporário exclusivo ao registry npm oficial. O [relatório do Incremento 2](STATE-06-Authenticated-SignalR-Change-Hint-Sandbox-Report.md) registra a implementação do commit `c945c1b`, o cliente oficial `@microsoft/signalr@10.0.0` fixado, o Quality Gate automático restrito aprovado e o Human Gate próprio aceito com as limitações registradas. A API e o polling continuam autoritativos; a composição normal permanece sem hub/publisher ativo. Incremento 3, runtime operacional, promoção e transição não foram autorizados.

#### Objetivo

Antecipar a reconciliação do Dashboard TV quando a projeção autoritativa mudar, mantendo a API e o polling de 30 segundos como fontes obrigatórias.

#### Escopo futuro permitido por este plano

- hub/endpoint SignalR autenticado exclusivamente no sandbox;
- autorização read-only equivalente à leitura do snapshot;
- hint mínimo, versionado, limitado e sem observação, segredo ou payload de provider;
- cliente Dashboard ativado somente na composição sandbox exata;
- coalescência de hints repetidos enquanto uma leitura estiver em curso;
- fencing por sessão, cancelamento, reconnect/backoff limitado e fallback para polling;
- autenticação negada, desconexão, perda, duplicidade, reorder e hint incompatível;
- inventário de dependência e lockfile antes de qualquer alteração.

#### Critérios de aceite

1. um hint nunca atualiza a tela diretamente;
2. uma mudança válida antecipa exatamente uma nova leitura quando não houver outra em curso;
3. hints repetidos/reordenados não causam regressão nem tempestade de pedidos;
4. a concorrência máxima de leitura permanece `1`;
5. a cadência periódica continua funcionando sem SignalR e depois de desconexão;
6. autenticação ausente/inválida é recusada sem revelar estado;
7. saída do modo TV cancela hub, timers e pedidos da sessão antiga;
8. nenhum acesso externo ou pacote é adquirido sem autorização específica.

#### Evidência mínima

- relógio controlado e testes de estado do cliente;
- E2E em navegador dedicado sob HTTPS loopback;
- linha do tempo de leitura inicial, hint, leitura antecipada e próxima reconciliação periódica;
- concorrência máxima, reconnects limitados e zero origem externa observada;
- relatório próprio e revisão direta do diff.

### Incremento 3 — Reconciled Local Notification Delivery Sandbox

**Status factual atual:** Bruno autorizou separadamente a [proposta detalhada](STATE-06-Reconciled-Local-Notification-Delivery-Sandbox-Proposal.md). A implementação local foi concluída no commit `d43e49a`; o [relatório do Incremento 3](STATE-06-Reconciled-Local-Notification-Delivery-Sandbox-Report.md) registra contrato/projeção read-only, baseline silenciosa, ledger/cursor/deduplicação/fencing, consumidor WPF opt-in, testes locais e Quality Gate automático restrito aprovado. Bruno aceitou o Human Gate próprio com as limitações registradas. O Incremento 4 também foi implementado e aceito com as limitações registradas; runtime operacional, campanha consolidada, promoção e transição permanecem não autorizados.

#### Objetivo

Produzir uma notificação Windows local somente quando uma transição factual reconciliada pela API ocorrer, preservando fonte, freshness e opt-in.

#### Escopo futuro permitido por este plano

- consumidor read-only de transições autoritativas dentro da composição local de teste;
- preferência opt-in explícita, desabilitada por padrão;
- baseline inicial silenciosa;
- identidade estável de evento/transição e deduplicação durável no sandbox;
- fila serial limitada, cancelamento, política de silêncio e tratamento de falha de publicação;
- conteúdo localizado que identifique origem de teste, estado anterior/novo e horário factual;
- publicação Windows local pelo caminho já existente, com fallback local seguro;
- testes automatizados e, somente sob autorização posterior específica, uma amostra humana visível.

#### Critérios de aceite

1. a inicialização não produz notificação;
2. somente uma mudança reconciliada produz uma tentativa de entrega;
3. replay, reconnect, restart e hints duplicados não repetem a mesma transição;
4. dado stale/unknown não é apresentado como saudável ou atual;
5. opt-out, silêncio, cancelamento e falha de publicação preservam estado e auditoria sanitizada;
6. a notificação não executa ação e seu clique pode no máximo abrir a interface local segura;
7. não há e-mail, SMS, webhook, SignalR externo ou outro canal operacional;
8. aceitação da API Windows não é descrita como prova de que o Shell mostrou a notificação.

#### Evidência mínima

- matriz determinística de transições, baseline, duplicidade, restart e falhas;
- inspeção da persistência local sanitizada;
- testes WPF/Tray, arquitetura, localização e acessibilidade aplicáveis;
- amostra visível separada, se autorizada, com processo encerrado antes do registro;
- relatório próprio e revisão direta do diff.

### Incremento 4 — Command Transport Safety E2E Sandbox

**Status factual atual:** os Incrementos 1–3 foram concluídos e aceitos com as limitações registradas. Bruno autorizou separadamente a [proposta detalhada do Incremento 4](STATE-06-Command-Transport-Safety-E2E-Sandbox-Proposal.md). A implementação local foi concluída no commit final `54a65f5`, e o [relatório próprio](STATE-06-Command-Transport-Safety-E2E-Sandbox-Report.md) classifica o Quality Gate automático restrito como aprovado. Bruno aceitou o Human Gate próprio do Incremento 4 com as limitações registradas; campanha consolidada, runtime operacional, comando, executor, promoção e transição continuam não autorizados.

#### Objetivo

Completar o protocolo durável de polling/acknowledgement do Agent e provar expiração, replay e incompatibilidade sem executar qualquer comando administrativo.

#### Baseline que será preservada

- o servidor já seleciona somente comandos compatíveis e não expirados em testes locais;
- comandos expirados são marcados `Expired`;
- acknowledgement repetido é idempotente;
- a inbox do Agent aceita replay exato e rejeita replay conflitante;
- o Worker normal ainda recusa command polling porque faltam request IDs, sequências e replay duráveis ponta a ponta;
- `Start`, `Stop` e `Restart` permanecem `Unsupported` e não existe executor.

#### Escopo futuro permitido por este plano

- contratos versionados de poll e acknowledgement com request/message IDs e sequência monotónica;
- transporte HTTPS/mTLS exclusivamente de teste;
- inbox e acknowledgement duráveis no SQLite Agent sandbox;
- resposta terminal segura `Expired`, `Rejected` ou `Unsupported` sem attempt de execução;
- timeout, cancelamento, retry/backoff limitado, perda de resposta e replay exato;
- agente errado/revogado, versão incompatível, lote acima do limite, duplicidade, gap e reorder;
- activation guard que mantenha o Worker normal recusando ativação fora do sandbox.

#### Critérios de aceite

1. comando expirado nunca alcança execução e termina factualmente como `Expired`;
2. comando recebido pode ser persistido/acknowledged, mas nunca muda para `Running` ou `Succeeded`;
3. nenhum `CommandAttempt`, resultado de provider ou post-probe é criado;
4. replay exato não duplica efeito e conflito falha fechado;
5. mensagens fora de ordem, gaps, Agent revogado/errado e versões incompatíveis são recusados;
6. cancelamento e perda de resposta preservam o estado necessário para replay;
7. não há processo, shell, serviço, banco monitorado ou infraestrutura afetada;
8. o command polling normal continua desabilitado até homologação e autorização posteriores.

#### Evidência mínima

- testes unitários de máquina de estados, sequência e replay;
- E2E entre API e Agent sandbox com relógio controlado;
- inspeção de que nenhuma attempt/execução/ação foi criada;
- smoke fail-closed da composição normal;
- relatório próprio e revisão direta do diff.

## Campanha final — Quality Gate consolidado do STATE-06

**Status factual atual:** Bruno autorizou separadamente a [campanha consolidada](STATE-06-Consolidated-Quality-Gate-Campaign-Proposal.md) sobre o commit `5a47aae`. Os checks e harnesses existentes passaram, mas o [relatório factual](STATE-06-Consolidated-Quality-Gate-Campaign-Report.md) classificou o Quality Gate consolidado como `BLOQUEADO`: pipeline, browser/SignalR, notificação e comando usavam sandboxes independentes. A [remediação autorizada](STATE-06-Consolidated-Quality-Gate-Remediation-Proposal.md) implementou um harness único correlacionado e corrigiu os dois achados menores no commit `ac12791`; Bruno aceitou seu [relatório factual](STATE-06-Consolidated-E2E-Evidence-Harness-And-Deterministic-Gate-Remediation-Report.md) com as limitações registradas. A [proposta documental da repetição](STATE-06-Consolidated-Quality-Gate-Rerun-Proposal.md) está pronta para revisão; nova campanha, amostra humana, Human Gate final, promoção e transição não estão autorizadas.

O Human Gate próprio do Incremento 4 e o Human Gate próprio da remediação foram aceitos. A primeira campanha permanece bloqueada; uma nova execução proporcional exige autorização separada e não poderá corrigir achados silenciosamente.

### E2E consolidado obrigatório

O cenário principal deverá demonstrar, em processos locais separados:

1. criação de identidade exclusivamente de teste e enrollment controlado;
2. emissão de uma observação canônica pela fonte sintética;
3. persistência/outbox no Agent;
4. interrupção controlada da API e preservação offline;
5. reconexão e ingestão idempotente;
6. projeção read-only e snapshot consumido pelo Dashboard TV;
7. hint SignalR que antecipa uma releitura sem sobreposição;
8. reconciliação periódica posterior a 30 segundos;
9. uma transição posterior que produz uma única notificação local opt-in;
10. replay/duplicidade/reorder que não produz nova notificação nem regressão;
11. comando de teste expirado ou incompatível recusado sem execução;
12. revogação da identidade, falha fechada e preservação factual do último snapshot como stale;
13. encerramento integral de Agent, API, Dashboard, WPF, navegador dedicado, listeners e perfis temporários.

### Verificações automáticas mínimas

- `git diff --check` e revisão integral do diff;
- formatação, build Release e testes de toda a solução;
- testes unitários, arquitetura, integração, Dashboard, WPF/Tray e Pester aplicáveis;
- compatibilidade de contratos e migrations/model drift locais;
- cobertura comparada aos pisos vigentes, sem esconder regressão por exclusão indevida;
- documentação/comments em inglês britânico onde aplicável, links Markdown e headings;
- secret scan do worktree e histórico disponível;
- dependency/lockfile audit offline e registro explícito do que não pôde ser atualizado sem rede;
- smoke fail-closed da composição normal;
- processo/listener/profile cleanup final;
- relatório consolidado com achados por gravidade e matriz Lifecycle → evidência.

### Classificação do gate automático

O gate somente poderá ser `APROVADO` se não houver achado crítico/alto conhecido, todos os critérios de saída tiverem evidência e nenhuma proibição tiver sido violada. Limitação ambiental ou de homologação posterior poderá permanecer registrada sem ser apresentada como implementação concluída.

Resultados possíveis:

- `APROVADO`: pronto para Human Gate do `STATE-06`;
- `REPROVADO`: existe falha comprovada que precisa de remediação;
- `BLOQUEADO`: evidência obrigatória não pôde ser produzida com segurança/autorização;
- `NÃO APLICÁVEL`: somente para verificação que não pertença ao `STATE-06`, com justificativa explícita.

## Human Gate final do STATE-06

O Human Gate não será inferido a partir das aceitações dos incrementos. Bruno deverá receber um único resumo que identifique o relatório automático consolidado, as amostras repetidas, as limitações e a decisão exata solicitada.

### Amostras humanas propostas

1. observar o Agent perder e recuperar a conexão local sem perder ou duplicar a observação;
2. observar o Dashboard TV fazer leitura imediata, uma releitura antecipada após hint e a reconciliação posterior sem pedidos concorrentes;
3. observar uma única notificação local para uma transição posterior à baseline, com origem de sandbox claramente indicada;
4. verificar que uma repetição da mesma transição não gera nova notificação;
5. verificar no relatório que o comando expirado/incompatível foi recusado e que nenhuma ação foi executada;
6. confirmar visualmente a distinção entre fonte sintética, dado stale/unknown e qualquer suporte apenas planejado.

Cada amostra visível exigirá autorização específica para abrir runtimes locais e navegador dedicado. Todos deverão ser encerrados antes do registro documental da decisão.

### Decisão humana futura

Depois da revisão, Bruno poderá decidir `APROVADO`, `APROVADO COM RESSALVAS` ou `REPROVADO` exclusivamente para o Human Gate do `STATE-06`. Uma resposta curta só terá valor de gate quando responder diretamente ao resumo completo de um único estado.

A aprovação do Human Gate não autoriza por si só `STATE-07`. A transição formal continuará exigindo uma instrução posterior, inequívoca e separada.

## Fora de escopo do fechamento do STATE-06

Permanecem fora deste plano e não são requisitos para encerrar o estado:

- banco monitorado real, credencial real, provider operacional ou homologação por engine/plataforma;
- PostgreSQL central real, IdP, PKI, vault, token service ou infraestrutura corporativa;
- `Start`, `Stop`, `Restart`, executor, attempt operacional, post-probe ou ação externa;
- e-mail, SMS, webhook, Teams, Slack ou canal externo de notificação;
- serviço/worker operacional permanente, deploy, publicação, instalação ou acesso remoto;
- carga representativa, endurance, disaster recovery e homologação multi-browser/multi-plataforma;
- LLM, recomendação, planejamento, automação ou promoção `none → OBSERVER` do MOD-12;
- `STATE-07`, `STATE-08` ou qualquer transição automática.

Esses itens pertencem a decisões independentes, principalmente homologação em `STATE-07`, release em `STATE-08` ou gates próprios do MOD-12.

## Riscos e condições de parada

| Risco | Tratamento obrigatório |
|---|---|
| SignalR ser tratado como fonte de verdade | Hint mínimo; toda atualização depende de nova leitura da API. |
| Dependência SignalR indisponível localmente | Parar antes de alterar lockfile; solicitar autorização específica para aquisição externa. |
| Fixture ser confundida com provider suportado | Identificação explícita de sandbox e ausência de registro na composição normal. |
| Tempestade de hints ou notificações | Coalescência, fila serial limitada, deduplicação durável e backpressure. |
| Acknowledgement ser confundido com execução | Estados e texto deixam claro `received/persisted`, sem `Running`/`Succeeded`. |
| Um harness esconder falha entre componentes | Processos, stores e transportes separados; evidência por fronteira. |
| Runtime de teste permanecer aberto | Cleanup em `finally` e verificação externa de zero processo/listener/profile. |
| Gate de incremento ser confundido com gate do estado | Relatórios e registros mantêm classificações separadas. |

Qualquer necessidade de acesso externo, dependência não disponível, credencial real, recurso operacional, ação administrativa ou ampliação material deverá interromper o incremento e voltar para autorização humana.

## Autorizações futuras e ordem para Bruno

Nenhuma execução é autorizada pelo plano em si. Autorizações separadas concluíram tecnicamente os quatro incrementos, a primeira campanha bloqueada, a remediação aceita e a repetição da campanha. A repetição no commit `66d0a9f` ficou `REPROVADA` por falha do harness correlacionado em `finalising-revocation`. A ordem restante é:

1. Incrementos 1–4 concluídos e aceitos em seus escopos restritos;
2. campanha consolidada executada e `BLOQUEADA` pela ausência de composição única;
3. proposta documental, implementação restrita e aceitação humana própria da remediação concluídas;
4. proposta documental da repetição, autorização e execução concluídas; Quality Gate repetido `REPROVADO`;
5. revisar o [relatório da repetição](STATE-06-Consolidated-Quality-Gate-Rerun-Report.md) sem inferir aprovação;
6. propor e autorizar separadamente a remediação do achado `finalising-revocation`, sem iniciar amostra humana;
7. depois da remediação aceita, autorizar e repetir novamente a campanha automática;
8. somente com Quality Gate aprovado, autorizar separadamente as amostras humanas;
9. decidir o Human Gate do `STATE-06`;
10. somente depois, decidir separadamente se autoriza a transição para `STATE-07`.

O texto abaixo é preservado como o texto histórico que autorizou o Incremento 1; ele já foi usado e não deve ser repetido como nova autorização:

> AUTORIZO o Incremento 1 do Plano Consolidado de Fechamento do STATE-06 — Authoritative Observation Pipeline E2E Sandbox, limitado à fonte provider-neutral exclusivamente sintética, observações canônicas versionadas, persistência/outbox Agent SQLite, transporte HTTPS/mTLS de teste, ingestão e projeção read-only da API, snapshot Dashboard TV e testes locais de reinício, offline/reconexão, replay, duplicidade, reorder, staleness, revogação e incompatibilidade. Autorizo runtimes temporários exclusivamente locais, que deverão ser encerrados ao final. Permanecem proibidos acesso externo, dependências/downloads, provider ou banco operacional, SignalR, notificações, comandos, executor, serviços permanentes, deploy, LLM, promoção e transição de estado.

Essa autorização histórica liberou somente o primeiro incremento. O presente documento, por si só, continua sem liberar implementação; o segundo incremento dependeu de outra autorização explícita já registrada.

## Entregáveis deste incremento documental

Este pedido produz somente:

- este plano consolidado;
- atualização factual do estado corrente;
- registro append-only da autorização e do plano;
- atualização do changelog do corpus;
- validações documentais locais e commit focado.

Nenhum código de produto/teste, solução, projeto, pacote, lockfile, configuração executável, migration, runtime ou saída gerada é criado ou alterado.
