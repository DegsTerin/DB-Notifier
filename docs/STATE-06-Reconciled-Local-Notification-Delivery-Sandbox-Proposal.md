# Proposta STATE-06 — Reconciled Local Notification Delivery Sandbox

## Status e autoridade

- Data: 2026-07-19.
- Estado mantido: `STATE-06 INTEGRATION`.
- Tipo: proposta exclusivamente documental para o Incremento 3 do [Plano consolidado de fechamento do STATE-06](STATE-06-Consolidated-Closure-Plan.md).
- Implementação, alteração de código/configuração executável, migration, pacote, lockfile, build, teste de produto e runtime: `NÃO AUTORIZADOS` e não executados.
- Acesso externo, canal externo, deploy, promoção e transição de estado: `NÃO AUTORIZADOS` e não executados.
- Decisão de execução futura: `PENDENTE DE AUTORIZAÇÃO SEPARADA`.

Este documento descreve uma opção técnica para revisão posterior. Ele não habilita notificações reconciliadas, não inicia WPF/API/Agent, não envia aviso ao Windows e não constitui evidência de funcionamento.

## Resultado em linguagem simples

Hoje o DB-Notifier possui duas peças separadas:

1. o sandbox integrado consegue levar dados fictícios e determinísticos do Agent até uma projeção read-only da API;
2. o aplicativo WPF já sabe pedir ao Windows uma notificação local de demonstração, com um balão local como alternativa segura.

Essas peças ainda **não estão ligadas**. A notificação atual nasce de uma fixture local de demonstração, não de uma mudança confirmada pela API. O Incremento 3 propõe construir futuramente essa ligação somente num laboratório local.

Em termos simples, o aplicativo faria uma primeira leitura silenciosa para saber o estado inicial. Depois, consultaria uma lista read-only e limitada de transições já reconciliadas pela API. Somente uma transição nova, válida e posterior à primeira leitura poderia produzir uma tentativa de notificação. Repetições, reinícios e reconexões não deveriam repetir o mesmo aviso.

O utilizador teria de habilitar essa função explicitamente; ela continuaria desligada por padrão. O aviso identificaria claramente que a origem é um sandbox com dados sintéticos. Clicar nele poderia, no máximo, abrir a interface local segura. Nenhuma ação administrativa, banco real, provider real, canal externo ou monitoramento operacional faria parte do incremento.

## Nome recomendado do incremento futuro

`STATE-06 — Reconciled Local Notification Delivery Sandbox`

Em português: **Sandbox de entrega de notificação local baseada em transição reconciliada**.

## Por que este é o próximo passo

O Incremento 1, commit `3449918`, comprovou em sandbox a cadeia fonte sintética → Agent → SQLite/outbox → HTTPS/mTLS de teste → API → projeção read-only. O Incremento 2, commit `c945c1b`, comprovou que um hint SignalR autenticado pode antecipar uma releitura do Dashboard sem substituir a API nem o polling periódico. Bruno aceitou os Human Gates próprios de ambos com as limitações registradas.

O requisito seguinte do plano consolidado é provar que uma mudança reconciliada pode chegar ao canal Windows local sem ser confundida com uma fixture, sem duplicação descontrolada e sem ativar o runtime normal. O Incremento 3 não transforma SignalR em canal de notificação: o cliente WPF deverá obter a transição pela API read-only. SignalR continuará restrito ao Dashboard TV e permanecerá apenas um hint não autoritativo.

## Baseline factual que deve ser preservada

- O publicador WPF existente registra o nome `DB Notifier`, usa a API de notificações do Windows quando disponível e mantém um balão local como fallback.
- A API do Windows retorna somente que aceitou o pedido de publicação; isso não prova que o Windows Shell mostrou o cartão ao utilizador.
- A notificação demonstrativa existente usa grupo próprio, identificador limitado, conteúdo local, sem áudio, ativo semântico e ativação que apenas revela o shell WPF local.
- O fallback existente usa fila serial limitada a `16`; quando cheia, descarta de forma segura em vez de crescer sem limite.
- A fixture WPF atual calcula mudanças depois de uma baseline silenciosa, mas sua deduplicação é de sessão e não é durável entre reinícios.
- A origem atual continua sendo uma demonstração local; ela não consome a projeção reconciliada da API.
- A persistência central já cria `EventId` estável para eventos canônicos derivados dentro da reconciliação e registra `canonical.event.v1` na outbox. Não existe, porém, endpoint read-only de transições destinado ao WPF.
- O evento persistido atual identifica tipo, severidade, instância, Agent, observação e horários, mas não expõe num contrato read-only próprio o par anterior/atual necessário à mensagem local.
- O contrato de snapshot TV não possui cursor de transições. Comparar somente snapshots permitiria perceber diferenças, mas não provaria uma identidade estável de evento nem evitaria todas as repetições após reinício.
- A composição normal mantém notification delivery incompleto em fail-closed; nenhum Worker de entrega reconciliada está ativo.
- Não existe canal externo, identidade operacional, provider homologado, banco externo, executor ou ação administrativa autorizada.

Essas limitações impedem a proposta de simplesmente reutilizar a fixture ou inventar que o snapshot atual já é uma fila de eventos.

## Objetivo do incremento futuro

Provar localmente, com dados exclusivamente sintéticos e opt-in explícito, que o WPF consegue:

1. estabelecer uma baseline silenciosa;
2. ler pela API somente transições canônicas reconciliadas posteriores a um cursor;
3. validar versão, identidade, ordem, origem, estado e freshness;
4. persistir localmente o cursor e a decisão de deduplicação;
5. solicitar no máximo uma entrega por transição elegível;
6. preservar cancelamento, limites, falhas e evidência sanitizada;
7. permanecer totalmente inativo na composição normal.

O resultado provará apenas o comportamento exercitado no sandbox local. Não provará entrega operacional, apresentação garantida pelo Windows, exactly-once distribuído, disponibilidade de produção, provider homologado ou canal externo.

## Arquitetura proposta

### 1. A transição reconciliada será a única entrada válida

O fluxo futuro deverá ser:

1. uma observação sintética é recebida e reconciliada pela cadeia autorizada do Incremento 1;
2. a mesma transação central cria o evento canônico com identidade estável;
3. uma projeção read-only do sandbox expõe somente eventos canônicos já commitados;
4. o cliente WPF sandbox lê um lote limitado depois do seu cursor;
5. o cliente valida integralmente o contrato antes de alterar baseline, cursor ou deduplicação;
6. uma política local decide se a transição é elegível;
7. a decisão durável é registrada antes da tentativa de publicação;
8. o adaptador Windows recebe apenas conteúdo localizado e sanitizado;
9. o resultado factual da tentativa é registrado sem alegar visibilidade.

Uma fixture WPF, um hint SignalR, um callback de interface, um timer ou a simples diferença entre dois cartões nunca serão autoridade suficiente para produzir a notificação reconciliada.

### 2. Contrato read-only mínimo e versionado

Propõe-se um contrato conceitual `reconciled-local-notification-transition.v1`, com resposta paginada/cursada e limites exatos. Cada item deverá conter somente:

- `schemaVersion`: versão exata do contrato;
- `cursor`: posição opaca, monotónica dentro da fixture e limitada;
- `eventId`: identidade estável do evento canônico;
- `instanceId`: identidade provider-neutral não secreta;
- `displayName`: nome limitado e sanitizado da instância de teste;
- `eventType` e `severity`: classificação canônica permitida;
- `previousStatus` e `currentStatus`: par exato que produziu o evento;
- `observedAt` e `receivedAt`: horários UTC factuais;
- `freshness`: `current`, `stale` ou `unknown`, calculado segundo política explícita;
- `sourceKind`: valor exato que identifique a origem sintética do sandbox.

O contrato não deverá conter credencial, connection string, endpoint de banco, payload provider-native, mensagem interna de exceção, detalhes de consulta, hostname operacional, segredo ou texto arbitrário não limitado.

Campos desconhecidos, versão incompatível, cursor regressivo, IDs vazios/duplicados, lote fora do limite, enum desconhecido, horário futuro ou ordem temporal inválida deverão falhar de forma fechada. Um lote inválido não avançará cursor, baseline ou ledger local.

### 3. Origem do par anterior/atual

O par `previousStatus`/`currentStatus` deverá ser capturado na fronteira que já conhece ambos os valores durante a reconciliação. Não deverá ser reconstruído no WPF a partir de texto, severidade ou `eventType`.

A evolução futura poderá acrescentar esse par ao payload versionado do evento/projeção sem reescrever eventos históricos. Eventos antigos que não possuam a forma exata serão inelegíveis e não serão convertidos por suposição. Qualquer mudança persistente necessária deverá ficar restrita à persistência de sandbox autorizada e receber revisão própria de migration/model drift.

### 4. Endpoint e autenticação somente no sandbox

- A rota será `GET`, read-only, limitada por lote e cursor e mapeada somente quando ambiente, configuração e host local coincidirem exatamente com a composição de teste.
- A composição normal não registrará a projeção, rota, consumidor ou loop de reconciliação de notificações.
- O transporte usará HTTPS loopback e identidade humana exclusivamente de teste, com autorização dedicada de leitura.
- A implementação poderá reutilizar a fronteira de autenticação de teste já aceita, mas não poderá alargar essa identidade para rotas humanas normais, Agent ou comandos.
- Requisição sem identidade, fora de loopback, em HTTP, com cursor inválido ou sem permissão será recusada antes de revelar qualquer transição.
- Respostas usarão `Cache-Control: private, no-cache`; a paginação/cursor, e não SignalR, será responsável pela continuidade.

Esta proposta não escolhe IdP, PKI, vault ou sessão operacional e não afirma que a autenticação de teste seja adequada para produção.

### 5. Baseline silenciosa e cursor

Na primeira ativação válida:

- o consumidor lerá a posição corrente e a registrará como baseline;
- nenhum evento existente antes dessa baseline será publicado;
- a baseline só será aceita depois de validar integralmente a resposta;
- falha, cancelamento ou resposta incompatível manterão o recurso inativo e sem notificação;
- reativar depois de opt-out criará uma nova baseline silenciosa, salvo se uma futura política diferente for explicitamente autorizada.

Nas leituras seguintes, somente eventos estritamente posteriores ao cursor confirmado poderão entrar na fila. O cursor será avançado de forma durável apenas junto com uma decisão explícita para cada item do lote: entregue à API Windows, suprimido por política, inelegível ou falho com retry permitido.

### 6. Opt-in e isolamento da composição normal

Três guardas independentes serão obrigatórios:

1. host/ambiente exato de sandbox;
2. configuração técnica local do harness;
3. preferência explícita do utilizador, desabilitada por padrão.

Se qualquer guarda faltar, o consumidor não inicia, não abre timer, não lê transições e não cria ledger. O opt-out cancela leituras e entregas pendentes e impede novos pedidos. Ele não será interpretado como autorização para apagar evidência histórica.

O incremento poderá acrescentar apenas o controle local mínimo necessário para informar e alterar essa preferência, sem redesenhar Settings ou criar configuração operacional. O texto deverá explicar que são notificações de dados sintéticos do sandbox.

### 7. Ledger durável e deduplicação

O cliente WPF sandbox deverá possuir um ledger local isolado, versionado e sem segredos, pertencente ao adaptador de teste — não ao SQLite do Agent e não ao banco central operacional. A tecnologia concreta deverá ser escolhida somente durante uma implementação autorizada após inventário das dependências já disponíveis; pacote novo ou download exigirá autorização separada.

O ledger deverá manter, no mínimo:

- versão do schema local;
- cursor confirmado;
- `eventId` canônico;
- hash canônico do conteúdo relevante;
- disposição `baseline`, `suppressed`, `attempting`, `accepted`, `retryable` ou `failed-terminal`;
- número limitado de tentativas;
- horários UTC da primeira leitura e da última decisão;
- código sanitizado de resultado.

Repetir o mesmo `eventId` com o mesmo hash será idempotente. Repetir o ID com conteúdo diferente indicará conflito e falhará fechado. Um cursor menor que o confirmado será tratado como rollback/replay e não poderá apagar o checkpoint.

### 8. Janela de falha entre ledger e Windows

A API de notificações do Windows não participa da transação do ledger. Portanto, não é possível prometer entrega distribuída exactly-once: uma falha do processo pode ocorrer entre o registro local e a chamada ao Windows, ou entre a chamada e o registro do resultado.

A mitigação proposta combina:

- `eventId` estável;
- estado durável `attempting` antes da chamada externa local;
- `Tag` Windows determinística, limitada a `16` caracteres, derivada do evento;
- grupo exclusivo do sandbox reconciliado;
- detecção local de colisão antes de publicar;
- retomada fail-closed depois de crash;
- número e prazo de retries limitados.

Se dois eventos produzirem o mesmo identificador Windows truncado, o consumidor não publicará o segundo por aproximação: registrará conflito sanitizado e exigirá remediação do esquema de identificação. A documentação deverá falar em **uma tentativa idempotente e substituível no Windows**, não em garantia absoluta de exactly-once ou de apresentação visível.

### 9. Fila, budget e backpressure

- Haverá um único consumidor e no máximo uma publicação em curso.
- A fila de entrega será limitada a `16`, preservando o limite factual do fallback existente.
- Cada leitura terá tamanho máximo, deadline e cancelamento.
- Cada ciclo terá limite de eventos examinados, materializados, persistidos e publicados.
- Overflow não descartará silenciosamente o cursor: o item ficará pendente ou será registrado como suprimido segundo regra determinística, sem avançar sobre trabalho não classificado.
- Rajadas serão processadas em ordem estável de cursor; não haverá tarefas ou timers ilimitados.
- Cancelamento do aplicativo ou opt-out interromperá trabalho novo e encerrará o item em voo em estado retomável.
- Exceções esperadas serão reduzidas a códigos sanitizados; `OutOfMemoryException` e falhas de integridade não serão absorvidas como retry comum.

Os números operacionais além dos limites já existentes serão calibrados somente no sandbox futuro e não serão apresentados como capacidade de frota ou produção.

### 10. Política de silêncio e freshness

Esta proposta separa duas ideias:

- **sem áudio:** todas as notificações deste sandbox permanecerão silenciosas, coerentes com o caminho demonstrativo atual;
- **período de silêncio:** uma janela local opcional poderá suprimir eventos sem produzir uma rajada atrasada quando terminar.

Uma transição suprimida por período de silêncio continuará deduplicada e auditada como `suppressed`; não será republicada depois de perder atualidade. `stale` e `unknown` nunca serão convertidos em `healthy` nem receberão linguagem de recuperação. Antes da publicação, o consumidor deverá revalidar a freshness contra o relógio injetável e suprimir qualquer texto que tenha se tornado materialmente enganoso.

### 11. Conteúdo e ativação seguros

A mensagem deverá existir em `pt-BR` e `en-GB` nos catálogos canônicos e conter apenas:

- identificação explícita de `Sandbox local — dados sintéticos`;
- nome limitado da instância fictícia;
- estado anterior e novo em linguagem factual;
- horário factual da observação e indicação de freshness;
- marca/ativo semântico já existente.

O texto não dirá “tempo real”, “produção”, “provider suportado”, “entrega garantida” ou “banco saudável” quando a evidência não sustentar isso. Dados stale/unknown manterão rótulo explícito e não dependerão somente de cor.

O clique poderá somente revelar a interface local segura já existente. Não abrirá URL externa, não executará comando, não reconhecerá alerta, não mudará estado, não acionará provider e não chamará `Start`, `Stop` ou `Restart`.

### 12. Publicação primária e fallback

- O caminho primário continuará sendo o `AppNotificationManager` já existente, com o display name e ativos canônicos do DB Notifier.
- O fallback continuará sendo exclusivamente o balão local serializado do Tray.
- O fallback só será solicitado quando o publicador primário declarar indisponibilidade/falha; aceitação primária não produzirá também um balão.
- Falha de um item não bloqueará indefinidamente os itens posteriores, mas retries e avanço de cursor deverão respeitar a ordem e o ledger.
- O resultado `accepted` significará apenas que a API Windows aceitou o pedido.
- Uma amostra humana visível, se autorizada depois, será evidência separada de que o cartão apareceu naquele computador e naquela execução.

## Threat model delimitado

| Ameaça | Contenção proposta | Evidência futura |
|---|---|---|
| Fixture ou hint tratado como verdade | aceitar somente contrato read-only produzido depois do commit canônico | teste negativo com fixture/hint sem evento |
| Acesso fora do sandbox | ambiente/configuração/HTTPS/loopback/identidade/opt-in independentes | composição normal sem rota, consumidor ou timer |
| Replay ou rollback de cursor | checkpoint monotónico e conflito fail-closed | replay, cursor regressivo e restart |
| Mesmo ID com conteúdo diferente | hash canônico e recusa do conflito | matriz de colisão/conteúdo divergente |
| Duplicação após crash | ledger `attempting`, Tag determinística e retomada limitada | crash antes/depois da chamada Windows |
| Tempestade de eventos | lote, fila `16`, uma publicação em voo, deadline e budget | rajada acima do limite sem crescimento ilimitado |
| Estado antigo apresentado como atual | revalidação de freshness antes da publicação | relógio avançado entre leitura e entrega |
| Baseline produzir avisos históricos | checkpoint inicial silencioso | store pré-carregado e zero publicação no startup |
| Opt-out incompleto | cancelamento, fence e ausência de timer/leitura posterior | opt-out durante fetch/fila/publicação |
| Vazamento de segredo/provider | allowlist de campos, limites e sanitização | canários proibidos em resposta, logs e ledger |
| Clique executar ação | argumento de ativação limitado ao shell local | argumentos ausentes/desconhecidos e nenhuma mutação |
| API Windows confundida com entrega | resultado nomeado como aceitação de pedido | relatório e testes sem alegação de visibilidade |
| Fallback concorrente | fila serial existente e apenas um item em voo | falha primária com sequência controlada |
| Contrato incompatível | versão/chaves/enums/tamanhos exatos | N, N-1, N+1 e campos desconhecidos |

## Matriz de testes futuros

### Contrato e API

- contrato exato, serialização determinística e limites de lote/cursor/texto;
- autenticação/autorização positiva somente no sandbox de teste;
- HTTP, host/origem remota, identidade ausente e permissão inválida recusados;
- evento somente depois do commit da reconciliação;
- `previousStatus` e `currentStatus` provenientes da fronteira canônica, não inferidos no cliente;
- paginação estável, replay exato, cursor regressivo, gap, reorder e versão incompatível;
- nenhum evento histórico incompleto convertido por suposição;
- composição normal sem rota/projeção de notificação.

### Política, ledger e fila

- opt-in desligado por padrão e três guardas independentes;
- primeira ativação com store pré-carregado produz baseline e zero avisos;
- nova transição produz uma única decisão de publicação;
- restart, reconnect e repetição do mesmo evento não duplicam;
- ID igual/conteúdo diferente e colisão de Tag falham fechado;
- crash antes e depois da chamada Windows preserva retomada determinística;
- fila de `16`, overflow, deadline, cancelamento e opt-out em cada fronteira;
- período de silêncio registra supressão sem rajada posterior;
- stale/unknown e avanço de relógio não geram texto de saúde atual;
- ledger não contém secrets, payload provider-native ou mensagens arbitrárias.

### WPF, Tray e acessibilidade

- conteúdo localizado em `pt-BR` e `en-GB`, sem depender somente de cor;
- ativo semântico e identidade visual canônica;
- `Tag`/grupo dentro dos limites Windows e estáveis por evento;
- aceitação primária não aciona fallback simultâneo;
- falha primária aciona no máximo um fallback local serial;
- clique abre somente o shell local seguro;
- startup normal continua oculto no Tray e sem notificação reconciliada;
- High Contrast, foco e nomes acessíveis não sofrem regressão.

### E2E local

Uma execução futura, somente se autorizada, deverá usar processos locais separados, armazenamento efêmero e material exclusivamente de teste:

1. iniciar Agent/API/WPF sandbox com opt-in desligado e observar zero notificação;
2. habilitar explicitamente o opt-in e registrar baseline silenciosa;
3. enviar uma observação sintética posterior que produza evento canônico;
4. observar a API expor a transição commitada e o WPF aceitá-la uma vez;
5. repetir leitura, evento, reconnect e restart e observar zero nova tentativa;
6. exercitar stale/unknown, versão incompatível, cursor regressivo, fila cheia, período de silêncio, cancelamento e falha do publicador;
7. confirmar que Dashboard/SignalR continuam independentes e que nenhum hint fornece conteúdo à notificação;
8. inspecionar ledger e evidência sanitizados;
9. confirmar zero comando, attempt, provider, banco externo, canal externo ou efeito administrativo;
10. encerrar todos os processos/listeners e remover stores/perfis temporários.

A apresentação visual do cartão pelo Windows não deverá ser inferida da automação. Uma amostra visível exigirá autorização humana posterior e específica, será executada no WPF sandbox e terá cleanup próprio.

## Escopo proposto para uma autorização futura

Uma autorização futura de implementação poderá abranger somente:

- contrato versionado e projeção read-only de transições canônicas commitadas;
- captura explícita do par anterior/atual na reconciliação do sandbox;
- endpoint HTTPS loopback, autenticação/autorização exclusivamente de teste e paginação por cursor;
- consumidor WPF ativado apenas sob composição sandbox exata e opt-in desligado por padrão;
- baseline inicial silenciosa;
- ledger local isolado, cursor monotónico, hash, deduplicação e fencing entre reinícios;
- fila serial limitada a `16`, budget, deadlines, cancelamento, período de silêncio e falhas;
- evolução restrita do publicador Windows existente para identidade determinística do evento;
- fallback Tray local seguro, conteúdo `pt-BR`/`en-GB` e ativação somente para a interface local;
- testes unitários, arquitetura, integração e E2E exclusivamente locais;
- atualização factual da documentação e encerramento integral dos runtimes temporários.

Nenhuma dependência nova ou acesso externo está incluído nessa proposta. Se a implementação descobrir que ambos são necessários, deverá parar antes da aquisição e solicitar autorização separada.

## Fora de escopo absoluto

Permanecerão proibidos:

- notificação reconciliada na composição normal ou runtime operacional;
- e-mail, SMS, webhook, Teams, Slack, push móvel ou qualquer canal externo;
- SignalR como fonte da notificação ou alteração do seu papel de hint do Dashboard;
- provider, Agent, banco monitorado, PostgreSQL central ou credencial operacional;
- IdP, PKI, vault, token service ou certificado real;
- persistência externa, serviço/worker permanente ou infraestrutura distribuída;
- acknowledgement de alerta, mutação de estado ou UI administrativa;
- comandos, `Start`, `Stop`, `Restart`, shell, executor, attempt ou ação externa;
- monitoramento real, homologação de provider ou afirmação de suporte público;
- LLM, recomendação, planejamento, automação ou promoção `none → OBSERVER`;
- pacote, download, registry, CDN ou acesso externo sem autorização própria;
- deploy, publicação, instalação, produção, `STATE-07` ou transição de estado;
- promessa de exactly-once ou de apresentação garantida pelo Windows.

## Critérios de aceite propostos

O incremento futuro somente poderá ser classificado como concluído se houver evidência de que:

1. a composição normal não registra endpoint, consumidor, ledger, timer ou entrega reconciliada;
2. o sandbox exige ambiente/configuração exatos, HTTPS loopback, identidade de teste e opt-in explícito;
3. a primeira leitura é silenciosa mesmo com eventos históricos presentes;
4. somente evento canônico commitado e posterior ao cursor pode produzir tentativa;
5. o contrato possui versão, campos e limites exatos e inclui anterior/atual sem inferência do cliente;
6. replay, reorder, reconnect e restart não repetem a mesma transição;
7. rollback de cursor, conflito de conteúdo e colisão de identificador falham fechado;
8. ledger, fila e cursor sobrevivem ao restart sem crescer sem limite;
9. há no máximo uma publicação em curso e a fila total é limitada a `16`;
10. opt-out, cancelamento, silêncio, overflow e falha preservam decisão e evidência sanitizada;
11. stale/unknown nunca são apresentados como healthy/current;
12. conteúdo identifica sandbox sintético, estado anterior/novo, horário e freshness em ambos os idiomas;
13. clique abre no máximo a interface local segura e não produz mutação;
14. aceitação primária não duplica fallback e falha primária usa no máximo um fallback serial;
15. relatório distingue pedido aceito pela API Windows de apresentação visual observada;
16. nenhum canal/recurso externo, comando, executor, provider operacional ou banco externo é tocado;
17. testes e E2E locais passam e todos os runtimes/listeners/stores temporários são encerrados/removidos;
18. revisão direta do diff não encontra achado crítico/alto aberto;
19. Quality Gate automático e Human Gate próprio permanecem separados;
20. nenhuma promoção ou transição de estado ocorre automaticamente.

## Entregáveis futuros

- contrato/projeção read-only e threat model atualizados;
- endpoint sandbox e autenticação exclusivamente de teste;
- política opt-in, baseline silenciosa e consumidor WPF sandbox;
- ledger local durável, deduplicação, cursor, fencing e fila limitada;
- adaptação mínima do publicador/fallback local existente;
- catálogos localizados e testes de acessibilidade aplicáveis;
- matriz determinística e E2E local com cleanup;
- relatório de implementação, achados classificados e gates próprios.

## Riscos e limitações residuais

- A API Windows não oferece transação conjunta com o ledger; a mitigação reduz duplicação, mas não prova exactly-once absoluto.
- Aceitação pelo `AppNotificationManager` não prova que Focus Assist, política do sistema ou o Windows Shell exibiram o cartão.
- Um Windows local e um único WPF/API não provam outras versões, perfis, políticas corporativas ou escala.
- A autenticação e os dados continuarão sendo exclusivamente de teste.
- O ledger local não é desenho de persistência operacional nem substitui retenção/governança futura.
- O limite `16` protege o processo local exercitado, mas não prova backpressure de frota.
- Eventos canônicos históricos atuais não expõem todo o par anterior/atual no contrato necessário; a implementação futura deverá evoluir essa fronteira sem inferência retroativa.
- A projeção sintética atual recusa saúde `Healthy` sem evidência suficiente; o teste não poderá fabricar recuperação saudável para contornar essa política.
- A função continuará desabilitada por padrão e não será evidência de notification delivery operacional.
- Incremento 4, campanha final, Human Gate do estado e transição para `STATE-07` permanecerão separados.

## Rollback futuro

Se uma implementação futura falhar em qualquer gate, o rollback será remover somente a projeção/rota de transições do sandbox, o consumidor/ledger local de teste e a adaptação específica do publicador, restaurando a baseline aceita dos Incrementos 1 e 2 e o caminho demonstrativo WPF existente. Nenhum evento histórico será reescrito e nenhum rollback externo será necessário.

## Entregáveis documentais deste pedido

Este pedido produz somente:

- esta proposta e sua delimitação de autoridade;
- referência factual no plano consolidado;
- atualização do estado corrente e registro append-only;
- validações documentais locais e commit focado.

Nenhum código, projeto, configuração executável, migration, pacote, lockfile, teste, ledger, endpoint, notificação, WPF, API, Agent ou runtime foi criado, alterado ou executado.

## Decisão futura de Bruno

Bruno poderá ajustar, adiar ou rejeitar esta proposta. Se desejar liberar a implementação local exatamente nos limites acima, uma autorização futura possível é:

> AUTORIZO o incremento restrito de STATE-06 — Reconciled Local Notification Delivery Sandbox, limitado a contrato e projeção read-only versionados de transições canônicas commitadas, endpoint HTTPS loopback com autenticação exclusivamente de teste, consumidor WPF somente na composição sandbox e opt-in desabilitado por padrão, baseline inicial silenciosa, ledger local isolado com cursor monotónico, deduplicação e fencing entre reinícios, fila serial limitada, budget, cancelamento, política de silêncio, conteúdo localizado, publicação Windows pelo caminho existente com fallback local seguro e testes determinísticos/E2E exclusivamente locais. Autorizo runtimes temporários exclusivamente locais, que deverão ser encerrados ao final. Permanecem proibidos acesso externo, novas dependências/downloads, runtime operacional, canais externos, SignalR como fonte de notificação, Agent/provider/monitoramento ou banco operacional, persistência externa, IdP/PKI/vault reais, comandos, UI administrativa, LLM, executor, deploy, promoção e transição de estado.

Esse texto é somente uma sugestão documental. Ele não vale como autorização enquanto Bruno não o emitir posteriormente como uma decisão nova, explícita e separada.
