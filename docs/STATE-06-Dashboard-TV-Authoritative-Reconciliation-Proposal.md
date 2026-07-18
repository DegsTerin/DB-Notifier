# Proposta STATE-06 — Snapshot Autoritativo e Reconciliação Periódica do Modo TV em Sandbox

## Status e autoridade

- Data: 2026-07-18
- Posição do ciclo de vida: `STATE-06 INTEGRATION`, sem alteração
- Natureza: proposta exclusivamente documental e não executiva
- Origem: solicitação de Bruno por uma proposta para o próximo incremento restrito de `STATE-06`, sem implementação, promoção ou transição de estado
- Baseline aceita: incremento `Agent Fleet Sandbox Resilience and Protocol Compatibility`, commit `cee9cdf`, aceito com as limitações registradas
- Estado operacional: Dashboard, API, Agent Fleet e Worker não foram iniciados por este pedido

Este documento não autoriza código, configuração, contrato executável, dependência, build, teste, runtime, integração ou ação externa. Ele não aprova o Quality/Human Gate de saída de `STATE-06`, não promove `OBSERVER` e não autoriza `STATE-07`.

## Resumo para não especialistas

Hoje o modo TV do Dashboard mostra uma demonstração montada dentro do próprio navegador. A tela atualiza o relógio e calcula quando os dados ficam antigos, mas ainda não busca um retrato autorizado na API do DB-Notifier.

O próximo incremento recomendado criaria, somente num laboratório local e descartável, um caminho de leitura entre o modo TV e uma API de teste. Ao entrar no modo TV, o Dashboard faria uma leitura imediata. Enquanto o modo permanecesse ativo, faria nova leitura a cada 30 segundos, sempre esperando a leitura anterior terminar. Se a API falhasse, a tela manteria o último retrato válido com seus horários originais e mostraria claramente que ele está desatualizado, offline, negado ou com erro.

Em linguagem simples, a tela deixaria de consultar uma fotografia criada dentro dela e passaria a consultar uma fotografia controlada pelo servidor de testes. Isso ainda não seria monitoramento real: não haveria banco monitorado, provider, Agent operacional ou dado externo.

## Nome recomendado do próximo incremento

`STATE-06 — Dashboard TV Authoritative Snapshot and Periodic Reconciliation Sandbox`

Em português simples: provar localmente que o modo TV lê dados autorizados da API, repete a leitura sem sobreposição e nunca transforma falha ou dado antigo em estado saudável.

## Por que este é o próximo passo

O [ciclo de desenvolvimento](../prompts/governance/Lifecycle.md) exige em `STATE-06` que o modo TV:

1. leia a API autorizada imediatamente ao entrar;
2. faça nova leitura a cada 30 segundos enquanto estiver ativo;
3. nunca mantenha duas leituras concorrentes;
4. preserve o último snapshot e seus timestamps quando houver falha;
5. trate stale, offline, denied, erro e incompatibilidade como fatos distintos.

A inspeção factual encontrou que o Dashboard ainda usa `buildDemonstrationSnapshot` como fonte. O intervalo de 30 segundos existente recalcula somente freshness sobre essa fixture; ele não consulta a API. Não existe cliente SignalR no Dashboard e não existe um read model de TV exposto pela API.

Os incrementos Agent Fleet anteriores já possuem decisões próprias e não devem ser reabertos ou ampliados implicitamente. Integrar primeiro uma leitura periódica, local e somente de apresentação fecha uma lacuna coesa de `STATE-06` sem ativar Agent, provider, monitoramento, comando ou canal de notificação.

## Baseline factual que não deve ser reinterpretada

- O Dashboard atual é uma superfície demonstrativa e provider-neutral.
- `InventorySnapshot.CurrentSchemaVersion` já identifica o contrato de apresentação `inventory.v1`, mas isso não prova um endpoint HTTP de TV.
- O endpoint humano de catálogo existente não equivale ao snapshot completo de apresentação e não deve ser reutilizado por inferência.
- A API não consulta bancos monitorados diretamente.
- O modo TV é session-only e a opção de Fullscreen é apenas apresentação do navegador.
- O relógio visual de um segundo não é cadência de dados.
- O intervalo atual de 30 segundos envelhece a evidência local; ele não comprova reconciliação com a API.
- SignalR, entrega de notificações e dados operacionais ainda não estão integrados.
- A aceitação do commit `cee9cdf` não autorizou este incremento.

## Objetivo do incremento futuro

Demonstrar em sandbox local que um modo TV read-only pode consumir um snapshot provider-neutral, versionado e autorizado da API com estas propriedades:

- leitura imediata ao entrar no modo TV;
- apenas uma requisição em andamento;
- nova tentativa periódica a cada 30 segundos sob relógio controlável;
- cancelamento e descarte seguro de resposta tardia ao sair ou reentrar;
- validação fail-closed de versão, tamanho, identidade do snapshot, timestamps, ETag e conteúdo;
- suporte a `304 Not Modified` sem alterar artificialmente os horários da evidência;
- preservação do último snapshot válido em falha transitória;
- distinção explícita entre loading, empty, offline, error, denied, incompatible, stale e ready;
- ausência de fallback silencioso para a fixture demonstrativa enquanto o modo TV estiver configurado para a API sandbox;
- nenhuma escrita, comando, provider, monitoramento ou efeito externo.

## Escopo proposto para uma autorização futura

### 1. Read model versionado

- Definir um contrato read-only próprio do snapshot do modo TV, reutilizando tipos canônicos de apresentação apenas quando os limites arquiteturais permitirem.
- Incluir versão de schema, identificador/digest do snapshot, geração server-side e itens com `observedAt`/`receivedAt` preservados.
- Definir limites explícitos de quantidade e bytes antes da materialização integral da resposta.
- Recusar versão ausente, futura ou divergente, campos desconhecidos, duplicidades, timestamps inválidos e conteúdo acima dos limites.
- Não incluir secrets, referências administrativas, connection strings, instalação de Agent, certificado ou dados provider-native desnecessários.

### 2. Endpoint API exclusivamente read-only

- Expor uma rota versionada para leitura do snapshot de TV somente na composição local autorizada.
- Exigir autenticação humana de teste e permissão read-only server-side; a UI nunca será a barreira de autorização.
- Produzir ETag forte e admitir `If-None-Match`/`304` sem criar dado novo.
- Usar uma fonte determinística e local de sandbox; não acessar PostgreSQL real, Agent, provider ou banco monitorado.
- Garantir por teste que a rota não cria comando, evento, alerta, outbox, amostra, audit de ação administrativa ou qualquer mutação operacional.

### 3. Adapter de leitura do Dashboard

- Criar uma fronteira tipada entre transporte HTTP e estado de apresentação.
- Aceitar somente HTTPS loopback na composição E2E sandbox.
- Aplicar limites ao corpo antes da desserialização irrestrita.
- Sanitizar erros e nunca expor token, header de autenticação ou conteúdo bruto inesperado.
- Manter a fonte demonstrativa como caminho padrão fora da composição sandbox; não criar configuração operacional habilitada por padrão.

### 4. Coordenador periódico do modo TV

- Iniciar uma leitura imediatamente na transição inativa → ativa.
- Usar relógio e scheduler injetáveis para provar intervalos de 30 segundos sem esperar tempo real nos testes unitários.
- Manter no máximo uma leitura em voo; ticks durante uma chamada longa não abrem requisições paralelas.
- Coalescer no máximo uma solicitação pendente e respeitar cancelamento, deadline e geração da sessão.
- Ignorar resposta de uma sessão anterior, mesmo quando ela termina depois da nova sessão.
- Encerrar timer, requisição e subscriptions locais ao sair do modo TV ou desmontar o componente.
- Não alterar `observedAt` nem `receivedAt` apenas porque o relógio avançou ou a API confirmou `304`.

### 5. Estados factuais e acessibilidade

- Mostrar claramente a origem `API sandbox local` sem sugerir produção ou tempo real.
- Preservar o último snapshot válido durante falha transitória e recalcular freshness com o relógio corrente.
- Tratar `401/403` como denied, `426` como incompatible e timeout/rede indisponível como offline ou error conforme a evidência exata.
- Nunca apresentar o último estado saudável como atual quando seus timestamps estiverem stale ou inválidos.
- Preservar teclado, leitor de tela, foco, Light/Dark, High Contrast, Fullscreen opcional e saída visível do modo TV.
- Não redesenhar Dashboard, navegação, marca ou Design System além do mínimo necessário para comunicar fonte e estado.

### 6. Testes locais proporcionais

- Testes unitários do contrato, validação, scheduler, cancelamento, coalescência e máquina de estados.
- Testes Dashboard para entrada/saída, foco, acessibilidade, fonte factual e preservação do snapshot.
- E2E local com API HTTPS, autenticação exclusivamente de teste e browser dedicado com perfil temporário isolado quando houver amostra humana autorizada.
- Cenários `200`, `304`, empty, corpo no limite e acima do limite, resposta lenta, timeout, cancelamento, resposta fora de ordem, `401`, `403`, `426`, `429`, `503`, JSON incompleto e timestamps inválidos.
- Prova determinística da leitura imediata, ticks de 30 segundos, zero sobreposição e descarte de resposta tardia.
- Prova de que nenhuma rota de escrita, Agent, provider, comando, notificação ou recurso externo foi acionado.
- Encerramento de API, Dashboard, browser e listener temporários ao final.

## Fora de escopo absoluto

Mesmo que a implementação futura desta proposta seja autorizada, permanecem fora do escopo:

- SignalR, WebSocket, server push ou qualquer hint antecipado;
- entrega de notificações, e-mail, webhook, chat ou canal externo;
- Agent operacional, scheduler Agent Fleet, enrollment, rotação de certificado ou push de revogação;
- PostgreSQL real, IdP real, PKI, vault, proxy, cloud ou rede corporativa;
- provider, driver, conexão a banco monitorado, probe ou coleta de telemetria;
- assignments operacionais, acknowledgement, comando, Start/Stop/Restart ou executor;
- persistência nova, migration ou escrita no banco central;
- LLM, recomendação, planejamento, MOD-12 runtime ou promoção para `OBSERVER`;
- deploy, publicação, pacote, serviço, worker ou configuração habilitada por padrão;
- mudança de Design System, redesign ou nova funcionalidade fora do modo TV;
- declaração de produção, homologação, `STATE-07`, release ou transição de estado.

SignalR permanece um incremento posterior independente. Excluí-lo aqui evita misturar a fonte autoritativa periódica — que é obrigatória — com uma optimização de antecipação que não pode substituir a leitura da API.

## Critérios de aceite propostos para a implementação futura

Uma implementação futura só poderá ser considerada concluída se:

1. o diff permanecer dentro do read model, endpoint read-only, adapter/coordenador TV, testes e documentação proprietária;
2. o modo normal continuar demonstrativo e qualquer composição sandbox exigir ativação local exata e explícita;
3. a entrada no modo TV produzir exatamente uma leitura imediata;
4. o relógio controlado comprovar reconciliação periódica de 30 segundos;
5. nenhuma combinação de tick, lentidão, reentrada ou resposta tardia produzir duas requisições simultâneas ou sobrescrever snapshot mais novo;
6. `304` preservar os timestamps originais da evidência;
7. falhas preservarem o último snapshot válido apenas como evidência histórica/freshness, nunca como saúde atual inventada;
8. respostas negadas, incompatíveis, inválidas e acima do limite falharem fechadas;
9. a API comprovar autorização read-only e ausência de mutações;
10. runtimes e perfis temporários serem encerrados e removidos;
11. build, testes, cobertura, arquitetura, acessibilidade, documentação, links, secrets, format e smoke fail-closed aplicáveis passarem;
12. revisão direta do diff classificar achados por gravidade e corrigir os autorizados antes do relatório;
13. relatório automático distinguir observado, inferido, não testado e proibido;
14. Bruno emitir uma decisão humana separada somente depois de revisar o relatório.

## Riscos e limitações que permanecerão

- Uma API e autenticação locais de teste não provam IdP, autorização, CORS, proxy, TLS corporativo ou produção.
- Uma fixture determinística não prova dados reais, provider, Agent ou persistência PostgreSQL.
- Relógio controlado e browser local provam a lógica de cadência, não disponibilidade contínua, suspensão de aba, economia de energia ou políticas de dispositivos reais.
- ETag e validação de schema não tornam o conteúdo verdadeiro; apenas protegem consistência e compatibilidade do contrato testado.
- Sem SignalR, mudanças podem aparecer somente na próxima leitura periódica.
- Sem uma fonte operacional, a legenda deve continuar identificando o sandbox e não pode alegar tempo real.
- Este incremento isolado não satisfaz todos os critérios de saída de `STATE-06`; notificações, SignalR e demais integrações continuam independentes.

## Entregáveis documentais deste pedido

Este pedido produz somente:

- esta proposta delimitada;
- correção factual no estado corrente da aceitação já registrada de `cee9cdf`;
- atualização do estado corrente para indicar que existe uma proposta pendente de decisão;
- entrada factual append-only no histórico.

Não há relatório de implementação, alteração de código, contrato executável, dependência, build, teste de produto, runtime ou Human Gate neste pedido.

## Decisão solicitada a Bruno

Bruno pode ajustar, adiar, rejeitar ou autorizar separadamente somente o incremento proposto. Aceitar esta proposta não implementa nada.

Uma autorização futura inequívoca poderá usar a seguinte redação:

> AUTORIZO o incremento restrito de STATE-06 — Dashboard TV Authoritative Snapshot and Periodic Reconciliation Sandbox, limitado a contrato versionado e endpoint read-only de snapshot, autenticação exclusivamente de teste, adapter Dashboard ativado somente em sandbox local, leitura imediata ao entrar no modo TV, reconciliação serializada a cada 30 segundos, ETag/304, cancelamento, preservação factual do último snapshot e testes locais determinísticos/E2E, com runtimes temporários encerrados ao final. Permanecem proibidos SignalR, notificações, Agent ou provider operacional, monitoramento, comandos, persistência ou banco externo, IdP/PKI/vault reais, LLM, executor, deploy, promoção e transição de estado.

Essa eventual autorização permitiria somente a implementação local descrita. Qualquer SignalR, canal, fonte operacional, novo incremento, promoção ou transição continuaria exigindo decisão separada.

## Adendo factual — autorização consumida e implementação local

Em 2026-07-18, Bruno emitiu separadamente a autorização exata proposta acima. A autoridade foi consumida somente pelo contrato `dashboard-tv.v1`, endpoint read-only sob dupla ativação sandbox, autenticação humana em processo de teste, adapter HTTPS loopback, leitura imediata, reconciliação serial após 30 segundos, ETag/`304`, cancelamento, preservação do último snapshot e testes locais.

O modo normal continua demonstrativo. A fonte sandbox é uma fixture imutável em memória e nenhum SignalR, notificação, Agent/provider operacional, monitoramento, comando, persistência externa, IdP/PKI/vault real, LLM, executor, deploy, promoção ou transição foi implementado. O resultado e suas limitações pertencem ao [relatório do incremento](STATE-06-Dashboard-TV-Authoritative-Reconciliation-Report.md); este adendo não transforma a proposta histórica em autoridade permanente.

## Adendo factual — decisão humana do incremento

Depois de ler o relatório e confirmar que compreendeu o caráter exclusivamente local, determinístico, fictício e não operacional do sandbox, Bruno decidiu em 2026-07-18: `Aceitar com as limitações e a ressalva o incremento correspondente ao commit 70b3960.` Ele confirmou que a ressalva do teste legado não decorre das alterações deste incremento.

A decisão aplica-se exclusivamente ao resultado implementado e não autoriza novo desenvolvimento, promoção para `OBSERVER`, ativação operacional, deploy ou transição de estado. Este registro não reabre a autoridade consumida nem cria um próximo incremento.
