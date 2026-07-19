# Proposta STATE-06 — Command Transport Safety E2E Sandbox

> **Status factual posterior (2026-07-19):** Bruno autorizou separadamente a implementação restrita. O incremento foi concluído no commit final `54a65f5`; o [relatório factual](STATE-06-Command-Transport-Safety-E2E-Sandbox-Report.md) registra o Quality Gate automático restrito aprovado e o Human Gate próprio aceito com as limitações registradas. O `STATE-06` permanece inalterado, sem campanha consolidada, runtime operacional, comando, promoção ou transição.

## Status e autoridade

- Data: 2026-07-19.
- Estado mantido: `STATE-06 INTEGRATION`.
- Tipo: proposta exclusivamente documental para o Incremento 4 do [Plano consolidado de fechamento do STATE-06](STATE-06-Consolidated-Closure-Plan.md).
- Implementação, alteração de código/configuração executável, migration, pacote, lockfile, build, teste de produto e runtime: `NÃO AUTORIZADOS` e não executados.
- Acesso externo, comando administrativo, ação externa, promoção e transição de estado: `NÃO AUTORIZADOS` e não executados.
- Decisão de execução futura: `PENDENTE DE AUTORIZAÇÃO SEPARADA`.

Este documento descreve uma opção técnica para revisão posterior. Ele não ativa polling de comandos, não cria capacidade de execução, não inicia API/Agent e não constitui evidência de funcionamento.

## Resultado em linguagem simples

O DB-Notifier já possui partes de um correio local de comandos: a API consegue selecionar registros compatíveis e o Agent consegue guardar mensagens recebidas e reconhecer uma repetição idêntica. Entretanto, o correio ainda não possui um recibo durável completo de ponta a ponta.

Hoje, se uma resposta se perder depois de o servidor alterar seu estado, não existe evidência durável suficiente para garantir que uma repetição é exatamente a mesma mensagem. Também não há uma regra persistida no servidor para distinguir, de forma inequívoca, uma repetição legítima, uma mensagem atrasada, um salto de sequência e uma tentativa de reutilizar a mesma identidade com conteúdo diferente. Por esse motivo, o Worker normal recusa ativar o polling.

O Incremento 4 propõe construir futuramente um laboratório local que simule esse correio com identificadores, sequências, hashes e respostas persistidos. O laboratório deverá desligar a ligação durante pontos controlados, reiniciar processos e demonstrar que a mesma mensagem pode ser retomada sem duplicação ou avanço indevido.

Nenhum comando será executado. As fixtures usarão somente capacidades sintéticas deliberadamente não executáveis e resultados terminais seguros, como `Expired`, `Rejected` e `Unsupported`. “Acknowledged” significará apenas “recebido e registrado”, nunca “executado com sucesso”.

## Nome recomendado do incremento futuro

`STATE-06 — Command Transport Safety E2E Sandbox`

Em português: **Sandbox E2E de segurança do transporte de comandos**.

## Por que este é o próximo passo

Os Incrementos 1, 2 e 3 do plano consolidado foram concluídos e aceitos com limitações. Eles provaram, em sandbox, observação autoritativa, atualização antecipada do Dashboard por hint autenticado e notificação local baseada em transição reconciliada.

Resta resolver o critério de transporte de comandos exigido para o fechamento do `STATE-06`: provar expiração, persistência, acknowledgement, replay e incompatibilidade sem criar executor e sem produzir efeito administrativo. A campanha final consolidada somente poderá ser proposta para execução depois que este incremento também tiver relatório próprio e Human Gate aceito.

## Baseline factual que deve ser preservada

- Existem contratos v1 de poll, envelope, acknowledgement e respectivas respostas.
- A API possui endpoints autenticados de Agent para poll e acknowledgement e exige cabeçalhos de versão.
- O servidor seleciona registros `Pending`/`Available` do Agent correto, compatíveis com a versão esperada e ainda não expirados.
- O servidor marca como `Expired` os registros vencidos encontrados durante o poll ou acknowledgement.
- Um acknowledgement repetido de um registro já `Acknowledged` retorna resultado `Duplicate`.
- A inbox SQLite do Agent persiste um envelope novo, aceita replay exatamente igual e recusa colisão por `CommandId` ou `IdempotencyKey` com conteúdo diferente.
- A inbox não executa o conteúdo recebido. Os testes correntes preservam `CompletedAt` e `ResultJson` nulos e não criam `CommandAttempt`.
- O protocolo atual aloca uma sequência local antes do pedido, mas não possui outbox durável do pedido nem journal durável da resposta. Uma resposta perdida pode, portanto, deixar uma lacuna sem replay comprovável.
- O servidor atual não persiste a identidade, sequência, hash e resposta de cada poll/ack para diferenciar replay exato, conflito, gap e reorder entre reinícios.
- A resposta de poll cria uma identidade nova durante cada chamada; a resposta de acknowledgement não possui envelope correlacionável equivalente.
- Datas, tamanho HTTP, campos desconhecidos e alguns limites de contrato ainda não são verificados uniformemente em toda a fronteira.
- O `AgentCommandPollingWorker` normal permanece fail-closed: mesmo com a opção de polling habilitada, a validação de startup recusa a ativação porque o protocolo durável ainda não está disponível.
- `Start`, `Stop` e `Restart` continuam `Unsupported`; não existe executor, resultado de provider ou post-probe autorizado.

Essa baseline impede afirmar que idempotência local isolada já equivale a protocolo durável ponta a ponta.

## Objetivo do incremento futuro

Provar exclusivamente em sandbox local que API e Agent conseguem:

1. persistir a intenção de poll ou acknowledgement antes do envio;
2. correlacionar request, response, Agent, sequência e conteúdo por identidades estáveis;
3. repetir exatamente uma mensagem depois de timeout, cancelamento, perda de resposta ou reinício;
4. aceitar o replay exato sem repetir mutação;
5. recusar conflito, gap, reorder, identidade errada, revogação e versão incompatível;
6. encerrar itens sintéticos como `Expired`, `Rejected` ou `Unsupported` sem execução;
7. aplicar limites de tamanho, lote, tempo, concorrência e retry antes de trabalho não limitado;
8. manter a composição normal inativa e fail-closed.

O resultado provará somente o protocolo exercitado no laboratório local. Não provará transporte operacional, autorização administrativa, execução, homologação de provider, disponibilidade de produção ou segurança de uma PKI real.

## Arquitetura proposta

### 1. Separação obrigatória entre entrega e execução

O incremento deverá terminar na inbox do Agent e no acknowledgement devolvido à API. Não poderá existir referência de runtime a executor, provider, shell, processo, serviço do sistema, banco monitorado ou control plane.

As fixtures deverão usar identificadores reservados ao sandbox, por exemplo `sandbox.command.expired.v1` e `sandbox.command.unsupported.v1`. Elas serão explicitamente marcadas como não executáveis e não poderão usar `control.start.v1`, `Start`, `Stop` ou `Restart`.

Os únicos estados centrais alcançáveis pelo cenário serão:

| Situação | Estado terminal permitido | Significado factual |
|---|---|---|
| item válido recebido e registrado | `Acknowledged` | transporte confirmado; nenhuma execução ocorreu |
| validade encerrada | `Expired` | item recusado por expiração |
| identidade, sequência ou conteúdo inválido | `Rejected` | protocolo recusado de forma fechada |
| capacidade sintética não suportada | `Unsupported` conceitual ou resultado equivalente versionado | ausência deliberada de capacidade executável |

`Running`, `Succeeded`, `Failed`, `UnknownOutcome` e qualquer `CommandAttempt` deverão permanecer com contagem zero no E2E deste incremento. Se o modelo persistido não permitir `Unsupported` como estado do comando, o contrato deverá representá-lo como disposition terminal sem reclassificá-lo falsamente como sucesso.

### 2. Contratos versionados e correlacionáveis

Propõem-se contratos futuros distintos para poll e acknowledgement, com evolução incompatível explícita em relação à versão atual. Os nomes finais poderão seguir a convenção gerada do repositório, mas deverão representar conceitualmente:

- `command-poll-request.v2` e `command-poll-response.v2`;
- `command-acknowledgement-request.v2` e `command-acknowledgement-response.v2`;
- `command-protocol-problem.v1` para erros tipados e sanitizados.

Cada pedido deverá conter no mínimo:

- versão exata do protocolo e do schema;
- `messageId` não vazio e imutável;
- `agentId` igual à identidade autenticada e à rota;
- `sequence` estritamente monotónica no stream do Agent;
- `sentAt` UTC, usado como evidência e não como autoridade exclusiva de expiração;
- tipo exato da mensagem;
- conteúdo limitado;
- hash canônico do conteúdo persistido com o pedido.

Cada resposta deverá conter no mínimo:

- identidade própria estável;
- `inReplyToMessageId` e a sequência recebida;
- `agentId`, versões e horários UTC;
- resultado ou lote limitado;
- identidade/fingerprint estáveis para replay;
- erro tipado sem stack trace, segredo ou payload arbitrário.

Campos desconhecidos, versão incompatível, identificador vazio, horário inválido, enum desconhecido, conteúdo acima do limite e divergência entre rota, certificado e payload deverão falhar fechados antes de mutar cursor ou comandos.

### 3. Um stream monotónico por Agent

Poll e acknowledgement compartilharão um stream lógico ordenado por Agent. A regra proposta para o próximo pedido é `lastAcceptedSequence + 1`.

O servidor deverá classificar cada entrada assim:

| Entrada | Resultado proposto |
|---|---|
| próxima sequência e identidade nova | processar uma vez e persistir resultado |
| mesma sequência, mesma identidade e mesmo hash | devolver a resposta persistida, sem nova mutação |
| mesma sequência com identidade ou hash diferente | rejeitar como conflito |
| sequência menor que a última, sem replay conhecido | rejeitar como reorder/stale |
| sequência maior que a próxima esperada | rejeitar como gap |

Uma mensagem recusada por conflito, gap ou reorder não avançará o cursor. A resposta de erro deverá informar somente o código seguro e a sequência esperada, sem vazar conteúdo histórico.

### 4. Outbox durável do Agent

Antes de iniciar HTTPS, o Agent deverá gravar atomicamente no SQLite sandbox:

- identidade e sequência reservadas;
- tipo e payload canônico;
- hash do payload;
- estado `PendingSend`;
- deadline e contador limitado de tentativas;
- metadados mínimos de correlação.

Timeout, cancelamento, desconexão ou encerramento do processo não criarão uma nova sequência. Depois do reinício, o Agent repetirá o mesmo pedido, com a mesma identidade, sequência e conteúdo. Somente uma resposta integralmente validada poderá concluir a entrada e permitir que a sequência seguinte seja reservada.

A gravação da inbox de comandos e a preparação do acknowledgement deverão ser atómicas no store local. A confirmação local como `Acknowledged` ocorrerá somente depois de a resposta correspondente do servidor ser validada.

### 5. Journal durável do servidor

O servidor sandbox deverá persistir, na mesma unidade transacional da mutação autorizada:

- Agent, sequência, `messageId`, tipo e hash recebidos;
- classificação do pedido;
- identidade e conteúdo canônico da resposta;
- cursor anterior e cursor resultante;
- instante UTC recebido pelo servidor.

Assim, se a conexão cair depois do commit e antes de o Agent receber a resposta, o replay exato devolverá o resultado já persistido. A implementação não poderá reconstruir uma nova resposta com outro ID nem repetir a mutação.

O desenho físico poderá usar uma tabela de journal e um cursor por Agent, mas a migration e o modelo deverão ser validados somente em bancos efêmeros locais. Nenhuma migration poderá ser aplicada a PostgreSQL ou banco externo.

### 6. Expiração e autoridade temporal

O relógio controlado do servidor será a autoridade para decidir expiração central. `SentAt` e horários do Agent serão validados como evidência limitada, mas não poderão prolongar a validade do comando.

Os cenários deverão cobrir:

- item já expirado antes do poll;
- item que expira depois do poll e antes do acknowledgement;
- replay de resposta depois da expiração;
- horário do Agent adiantado, atrasado ou fora da tolerância documental.

Em todos eles, a expiração será monotónica: um item `Expired` não poderá voltar a `Pending`, `Available` ou `Acknowledged`. O Agent deverá registrar a disposição terminal recebida sem inventar execução ou sucesso.

### 7. Identidade, revogação e fronteira de autorização

O sandbox deverá reutilizar somente material P-256 efêmero de teste e autenticação mTLS loopback já delimitada pela integração Agent Fleet. A rota, o certificado autenticado e o `agentId` do corpo deverão identificar o mesmo Agent.

A revogação deverá possuir um ponto de linearização verificável:

- pedido aceito antes do commit da revogação poderá concluir segundo a decisão já persistida;
- pedido iniciado ou repetido depois do commit da revogação deverá ser recusado antes de ler ou mutar o stream de comandos;
- uma recusa por revogação não avançará cursor nem produzirá resposta de conteúdo anteriormente não entregue.

O teste deverá registrar essa ordem com barreiras determinísticas. Ele não deverá alegar proteção operacional, PKI real ou resistência a restauração integral do storage.

### 8. Budget, backpressure e cancelamento

Os valores abaixo são limites propostos somente para o sandbox, não dimensionamento operacional:

- no máximo `16` comandos ou acknowledgements por lote;
- no máximo `64 KiB` para o corpo HTTP completo;
- no máximo `4 KiB` de parâmetros JSON por fixture;
- no máximo `32` versões de provider declaradas;
- uma única operação de transporte em curso por Agent;
- timeout de `5 s` por tentativa;
- no máximo duas repetições depois da tentativa inicial;
- backoff determinístico de `100 ms` e `250 ms` no harness.

Os limites de corpo, coleção e string deverão ser aplicados antes da materialização integral ou do trabalho proporcional ao conteúdo. Cancelamento deverá interromper espera e I/O, mas não apagar a intenção durável necessária ao replay. Uma fila cheia deverá recusar trabalho novo de forma explícita; nunca crescer sem limite nem executar em paralelo.

### 9. HTTPS/mTLS e composição somente sandbox

O E2E futuro usará processos temporários separados, HTTPS loopback e certificados/identidades exclusivamente de teste. Hostname, porta, diretórios e bancos serão efêmeros e pertencentes ao harness.

O código compartilhado poderá receber os contratos e stores necessários, mas nenhum registro novo deverá ativar polling na composição normal. O smoke normal deverá continuar falhando com o reason code já previsto enquanto uma homologação operacional futura não existir.

Nenhum token, chave privada, certificado completo, payload de parâmetros ou caminho sensível será gravado no relatório. Material temporário será removido depois de encerrar os processos proprietários.

### 10. Falhas e reinícios E2E

O harness deverá permitir pontos de falha determinísticos, no mínimo:

1. antes de persistir a intenção no Agent;
2. depois de persistir e antes do envio;
3. depois de o servidor persistir o resultado e antes da resposta chegar;
4. depois de a inbox local persistir e antes do acknowledgement;
5. depois de o servidor aceitar o acknowledgement e antes da resposta chegar;
6. durante cancelamento e encerramento de processo;
7. durante uma corrida controlada com revogação.

Cada reinício deverá abrir novamente stores em disco separados, retomar a mesma identidade de mensagem pendente e preservar o fencing. Banco em memória não será evidência suficiente para os cenários de restart.

## Threat model delimitado

| Ameaça | Tratamento proposto | Limite que permanece |
|---|---|---|
| resposta perdida causa nova mensagem | outbox Agent e journal Server com replay da mesma identidade | não prova rede distribuída operacional |
| mesma sequência com conteúdo diferente | hash canônico e rejeição por conflito | hash não substitui autenticação |
| gap ou reorder avança estado | cursor monotónico e recusa sem mutação | um único stream por Agent no sandbox |
| Agent errado lê ou confirma item | vínculo certificado/rota/payload e assignment | identidade exclusivamente de teste |
| Agent revogado continua consultando | barreira de revogação antes do store | não é PKI operacional |
| lote ou corpo esgota recurso | limites antes da materialização, fila e concorrência `1` | números não são sizing de produção |
| clock do Agent prolonga validade | relógio Server autoritativo e expiração monotónica | relógio local controlado no E2E |
| acknowledgement é confundido com execução | estados, contratos, relatório e contagem zero de attempts | execução futura exigirá outro estado/gate |
| fixture aciona capacidade real | IDs sintéticos allow-listed e ausência de executor | não homologa comando/provider |
| log/evidência vaza conteúdo | campos allow-listed, hashes e sanitização | revisão humana ainda necessária |

## Matriz de testes futuros

### Contratos e validação

- round-trip canônico e versão exata para poll, acknowledgement, responses e problems;
- versão anterior, futura e desconhecida recusadas sem mutação;
- campos desconhecidos, IDs vazios, enums inválidos, datas não UTC e ordem temporal inválida;
- corpo, strings, parâmetros, versões e lote nos limites, um acima e muito acima;
- rota, certificado, header e payload divergentes;
- sanitização de erros e evidência.

### Sequência, journal e replay

- primeira sequência aceita e próxima sequência esperada;
- replay exato antes e depois de restart devolvendo a mesma resposta persistida;
- mesma sequência com `messageId` diferente;
- mesmo `messageId` com hash diferente;
- gap, reorder e replay desconhecido;
- perda de response de poll e de acknowledgement;
- falha transacional sem cursor parcialmente avançado;
- concorrência de duas mensagens para a mesma próxima sequência.

### Inbox, expiração e estados

- envelope novo persistido uma vez;
- replay exato local sem duplicação;
- colisão por `CommandId` ou idempotency key recusada;
- expiração antes do poll e entre poll/ack;
- `Unsupported` e `Rejected` terminais sem tentativa;
- acknowledgement repetido idempotente;
- nenhum item alcança `Running`, `Succeeded`, `Failed` ou `UnknownOutcome`;
- contagem zero de `CommandAttempt`, resultado de provider e post-probe.

### Resiliência, budget e segurança

- timeout, cancelamento e dois retries com backoff controlado;
- fila cheia e concorrência máxima observada igual a `1`;
- crash/restart em cada ponto de falha planejado;
- Agent errado, certificado ausente/inválido e Agent revogado;
- corrida revogação/request nos dois lados da barreira de commit;
- apenas loopback, zero origem externa e zero segredo na evidência;
- remoção dos bancos, certificados, listeners e processos temporários.

### Composição normal

- `CommandPollingEnabled` continua `false` por padrão;
- tentativa de habilitação normal continua recusada pelo guard de startup;
- nenhum Worker novo, endpoint sandbox, executor ou provider é ativado normalmente;
- nenhuma alteração de processo, serviço, banco monitorado ou infraestrutura ocorre.

## Sequência E2E futura proposta

1. confirmar shutdown completo do DB-Notifier;
2. criar diretório isolado, relógio controlado e material P-256 exclusivamente de teste;
3. iniciar API sandbox HTTPS loopback com store efêmero em disco;
4. iniciar Agent sandbox com SQLite separado e polling permitido somente pelo harness;
5. semear diretamente uma fixture sintética não executável, sem usar API administrativa/UI;
6. persistir e enviar poll com identidade e sequência estáveis;
7. interromper a resposta depois do commit Server;
8. reiniciar o Agent e observar replay da mesma mensagem e mesma resposta;
9. persistir o envelope e preparar acknowledgement sem qualquer execução;
10. perder a resposta do acknowledgement, reiniciar e confirmar replay idempotente;
11. repetir com expiração, incompatibilidade, conflito, gap, reorder e revogação;
12. inspecionar bancos e evidência para confirmar os estados terminais e zero attempts;
13. executar smoke da composição normal e confirmar recusa de ativação;
14. encerrar processos/listeners, remover material temporário e verificar cleanup.

A fixture será inserida apenas pelo controlador de teste no banco efêmero. O incremento não criará UI, comando humano, endpoint de criação ou capacidade administrativa.

## Escopo proposto para uma autorização futura

Uma autorização futura de implementação poderá abranger somente:

- contratos versionados de poll, acknowledgement, responses e erros tipados;
- outbox/journal/cursor duráveis para identidade, hash, sequência e replay;
- evolução restrita de stores e migrations Agent/Server necessárias ao sandbox;
- aplicação e teste de migrations apenas em SQLite/armazenamento efêmero local;
- HTTPS loopback, mTLS e identidade/revogação exclusivamente de teste;
- fixtures sintéticas allow-listed e deliberadamente não executáveis;
- estados terminais seguros sem `CommandAttempt` ou executor;
- timeout, cancelamento, retry/backoff, fila, budget, backpressure e fencing limitados;
- fault injection, crash/restart e testes unitários, arquitetura, integração e E2E locais;
- smoke fail-closed da composição normal, documentação factual e cleanup integral.

Nenhuma dependência nova ou acesso externo está incluído. Se a implementação descobrir que ambos são necessários, deverá parar antes da aquisição e solicitar autorização separada.

## Fora de escopo absoluto

Permanecerão proibidos:

- `Start`, `Stop`, `Restart` ou qualquer comando administrativo real;
- executor, `CommandAttempt`, post-probe, provider call, shell ou process spawning;
- alteração de serviço, processo alheio, banco monitorado, firewall, tunnel, cloud ou infraestrutura;
- API/UI de criação ou gestão de comandos e qualquer controle administrativo;
- polling de comandos na composição normal ou runtime operacional;
- Agent/provider/banco/credencial operacional e PostgreSQL externo;
- migration aplicada fora dos bancos efêmeros do sandbox;
- IdP, PKI, vault, token service ou certificado real;
- monitoramento real, canal externo, SignalR/notificação como transporte de comando;
- dependência, pacote, download, registry, CDN ou acesso externo;
- serviço/worker permanente, deploy, publicação ou instalação;
- LLM, recomendação, planejamento, automação ou MOD-12 `none → OBSERVER`;
- promoção, `STATE-07`, `STATE-08` ou qualquer transição automática.

## Critérios de aceite propostos

O incremento futuro somente poderá ser classificado como concluído se houver evidência de que:

1. poll e acknowledgement possuem versões, identidades, hashes e correlação explícitos;
2. a intenção Agent é persistida antes do envio e sobrevive a restart;
3. o servidor persiste cursor e resposta junto da mutação correspondente;
4. resposta perdida é recuperada pelo replay da mesma identidade, sequência e conteúdo;
5. replay exato não duplica inbox, estado ou acknowledgement;
6. conflito, gap, reorder e replay desconhecido falham fechados sem avançar cursor;
7. Agent autenticado, rota e payload sempre coincidem;
8. pedido posterior ao commit de revogação é recusado antes de ler/mutar o stream;
9. expiração é monotónica e usa o relógio Server controlado como autoridade;
10. lote, corpo e coleções acima do limite são recusados antes de trabalho não limitado;
11. concorrência máxima por Agent é `1` e retries/deadlines permanecem limitados;
12. cancelamento preserva a intenção necessária ao replay;
13. fixtures usam somente IDs sintéticos não executáveis;
14. nenhum item alcança `Running`, `Succeeded`, `Failed` ou `UnknownOutcome`;
15. existem zero `CommandAttempt`, zero resultado de provider e zero post-probe;
16. nenhum processo, shell, serviço, banco monitorado ou infraestrutura é afetado;
17. a composição normal continua com polling desabilitado e startup fail-closed;
18. testes locais passam e todos os processos/listeners/stores/certificados temporários são encerrados/removidos;
19. revisão direta do diff não encontra achado crítico/alto aberto;
20. relatório distingue claramente transporte confirmado de execução inexistente;
21. Quality Gate automático e Human Gate próprio permanecem separados;
22. nenhuma promoção ou transição de estado ocorre automaticamente.

## Entregáveis futuros

- contratos e reason codes versionados;
- outbox/cursor Agent e journal/cursor Server duráveis;
- migrations e stores restritos ao desenho autorizado;
- composição/harness E2E HTTPS/mTLS exclusivamente local;
- fixture sintética não executável e fault injection determinístico;
- matriz de testes, evidência sanitizada e inspeção de estados/contagens;
- smoke fail-closed normal e cleanup verificável;
- relatório factual com achados por gravidade e gates próprios.

## Classificação futura dos gates

O Quality Gate automático do incremento deverá ser classificado somente depois da implementação e da execução integral das verificações autorizadas:

- `APROVADO`: todos os critérios de aceite possuem evidência, não existe achado crítico/alto aberto e nenhuma proibição foi violada;
- `REPROVADO`: uma falha comprovada exige remediação específica e nova autorização quando aplicável;
- `BLOQUEADO`: uma evidência obrigatória não pôde ser produzida com segurança ou dentro da autoridade concedida;
- `NÃO APLICÁVEL`: somente para uma verificação que realmente não pertença ao escopo, acompanhada de justificativa factual.

O Human Gate do Incremento 4 será uma decisão posterior de Bruno baseada no relatório e nas limitações. Ele não poderá ser inferido do Quality Gate, desta proposta ou de uma autorização de implementação. Mesmo uma aceitação futura do Incremento 4 não autorizará a campanha consolidada, runtime operacional, promoção ou transição de estado.

## Riscos e limitações residuais

- O E2E usará uma única máquina e stores locais; não provará partição de rede, HA, escala ou comportamento distribuído operacional.
- Certificados, revogação e identidade serão exclusivamente de teste; não provarão PKI, rotação, HSM/vault ou identidade corporativa.
- Os limites numéricos protegerão apenas o sandbox exercitado e não serão sizing de frota ou SLA.
- Uma transação não abrange simultaneamente SQLite Agent e store Server; o protocolo reduz ambiguidades por replay, mas não promete exactly-once distribuído absoluto.
- O ponto de linearização da revogação resolverá a corrida testada, mas não provará reconciliação independente ou resistência a rollback integral do storage.
- O hash canônico detecta conflito no journal, mas não substitui autenticação, autorização ou integridade do transporte.
- Migrations validadas em SQLite efêmero não provam PostgreSQL operacional, backup, restore ou rollback de produção.
- O uso de fixtures não executáveis não homologa nenhum comando, provider ou topologia.
- `Acknowledged` continuará significando transporte/persistência, não autorização, início, conclusão ou sucesso administrativo.
- A campanha consolidada, o Human Gate final e qualquer transição continuarão pendentes depois deste incremento.

## Condições de parada futuras

A implementação deverá parar e voltar a Bruno se exigir:

- dependência/download/acesso externo não autorizado;
- credencial, certificado, banco, provider ou infraestrutura operacional;
- execução, attempt, post-probe ou capacidade administrativa;
- ativação do Worker normal;
- migration fora do sandbox local;
- alteração material dos limites ou do modelo de ameaça;
- relaxamento de autenticação, revogação, sequência, budget ou fail-closed;
- promoção, transição de estado ou campanha final automática.

## Rollback futuro

Se uma implementação futura falhar em qualquer gate, o rollback será remover somente os contratos v2, journal/outbox/cursors, migrations e composição E2E introduzidos pelo incremento, restaurando a baseline aceita dos Incrementos 1–3 e mantendo o polling normal recusado. Nenhum banco externo ou recurso operacional deverá existir para ser revertido.

## Entregáveis documentais deste pedido

Este pedido produz somente:

- esta proposta e sua delimitação de autoridade;
- referência factual no plano consolidado;
- atualização do estado corrente e registro append-only;
- validações documentais locais e commit focado.

Nenhum código, projeto, configuração executável, migration, pacote, lockfile, teste, API, Agent, comando ou runtime foi criado, alterado ou executado.

## Decisão futura de Bruno

Bruno poderá ajustar, adiar ou rejeitar esta proposta. Se desejar liberar a implementação local exatamente nos limites acima, uma autorização futura possível é:

> AUTORIZO o incremento restrito de STATE-06 — Command Transport Safety E2E Sandbox, limitado a contratos versionados de poll/acknowledgement e erros tipados, outbox e journal duráveis com message ID, hash e sequência monotónica, stores/migrations aplicados somente em sandbox local efêmero, transporte HTTPS/mTLS e identidade/revogação exclusivamente de teste, fixtures sintéticas deliberadamente não executáveis, disposições terminais seguras, budget, backpressure, cancelamento, retry/replay, fencing, fault injection, reinício entre processos e testes determinísticos/E2E exclusivamente locais, mantendo o command polling normal desabilitado e encerrando todos os runtimes ao final. Permanecem proibidos acesso externo, novas dependências/downloads, runtime operacional, API/UI administrativa, Start/Stop/Restart, CommandAttempt, executor, post-probe, shell, processos/serviços/bancos/infraestrutura afetados, Agent/provider/banco/credencial operacional, migration externa, IdP/PKI/vault reais, canais externos, LLM, deploy, promoção e transição de estado.

Esse texto é somente uma sugestão documental. Ele não vale como autorização enquanto Bruno não o emitir posteriormente como uma decisão nova, explícita e separada.
