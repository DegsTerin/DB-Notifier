# Proposta STATE-06 — Authenticated SignalR Change Hint Sandbox

## Status e autoridade

- Data: 2026-07-19.
- Estado mantido: `STATE-06 INTEGRATION`.
- Tipo: proposta exclusivamente documental para o Incremento 2 do [Plano consolidado de fechamento do STATE-06](STATE-06-Consolidated-Closure-Plan.md).
- Implementação, alteração de código/configuração executável, dependência, lockfile, build, teste de produto e runtime: `NÃO AUTORIZADOS` e não executados.
- Acesso externo, download, deploy, promoção e transição de estado: `NÃO AUTORIZADOS` e não executados.
- Decisão de execução futura: `PENDENTE DE AUTORIZAÇÃO SEPARADA`.

Este documento organiza uma opção técnica para decisão posterior. Ele não inicia SignalR, não acrescenta pacote, não habilita servidor ou cliente e não constitui evidência de funcionamento.

## Resultado em linguagem simples

O Dashboard TV já consulta a API local ao entrar no modo TV e volta a consultá-la periodicamente. O Incremento 2 propõe acrescentar futuramente uma **campainha local**: quando o servidor perceber que o snapshot pode ter mudado, SignalR avisará o navegador para consultar a API um pouco antes.

A campainha não levará dados de monitoramento e não poderá alterar a tela. Ela somente pedirá uma nova leitura. A API continuará sendo a única fonte do snapshot, e a consulta periódica de 30 segundos continuará funcionando mesmo se SignalR desconectar, perder avisos ou falhar completamente.

Isso continuará sendo um laboratório local com identidade e dados de teste. Não será monitoramento real, serviço permanente, acesso externo, notificação ao utilizador, comando administrativo ou promoção do produto.

## Nome recomendado do incremento futuro

`STATE-06 — Authenticated SignalR Change Hint Sandbox`

Em português: **Sandbox de aviso autenticado de mudança por SignalR**.

## Por que este é o próximo passo

O Incremento 1, commit `3449918`, comprovou a cadeia sintética fonte → Agent → SQLite/outbox → HTTPS/mTLS → API → projeção read-only do Dashboard TV. Bruno aceitou seu Human Gate próprio com as limitações registradas. O Dashboard já preserva leitura imediata, strong `ETag`, `304`, cancelamento, fencing de sessão, último snapshot factual e reconciliação serial de 30 segundos.

O critério ainda aberto no `STATE-06` é antecipar uma leitura com segurança quando a projeção muda. O [Lifecycle](../prompts/governance/Lifecycle.md) exige que essa antecipação não produza leituras concorrentes nem substitua a reconciliação periódica. O plano consolidado reserva essa responsabilidade ao Incremento 2 antes de notificações locais e transporte de comandos.

## Baseline factual preservada

- O modo normal do Dashboard permanece demonstrativo.
- Somente a composição sandbox exata pode ler o snapshot autoritativo local.
- Entrar no modo TV inicia uma leitura imediata.
- O coordenador atual aceita no máximo uma leitura em voo, agenda outra 30 segundos depois da conclusão anterior e rejeita conclusões de sessões antigas.
- `ETag`/`304` não substitui timestamps nem transforma evidência antiga em fresca.
- Falhas preservam o último snapshot apenas como evidência histórica.
- A composição normal da API não registra a projeção sintética do Incremento 1.
- Não existe `AddSignalR`, `MapHub`, `HubConnection` ou referência SignalR no código atual.
- O manifesto e o lockfile do Dashboard não contêm `@microsoft/signalr`; a consulta direcionada executada sobre o cache npm local não retornou entrada correspondente.
- O Server usa `Microsoft.NET.Sdk.Web`, mas esta proposta não presume que toda a composição necessária esteja validada nem autoriza nova referência NuGet.

## Objetivo do incremento futuro

Provar em sandbox HTTPS loopback que um aviso SignalR autenticado, mínimo e não autoritativo pode antecipar uma consulta ao snapshot sem alterar diretamente a interface, criar sobreposição, adiar indefinidamente o polling, expor dados ou sobreviver à sessão TV que o criou.

O resultado provará somente o comportamento local exercitado. Não provará entrega em tempo real, infraestrutura distribuída, proxy corporativo, IdP operacional, escala multi-cliente ou produção.

## Arquitetura proposta

### 1. A API permanece autoritativa

O fluxo proposto será:

1. uma mudança sintética é commitada na projeção local autorizada;
2. o sandbox publica um hint mínimo em memória;
3. o navegador recebe o hint autenticado;
4. o coordenador solicita novamente `/api/v1/dashboard/tv-snapshot` com o último strong `ETag`;
5. somente o corpo validado da API ou um `304` altera o estado interno da reconciliação;
6. SignalR nunca fornece inventário, status, freshness, timestamp de observação ou decisão visual.

Um hint perdido é aceitável porque a consulta periódica continua obrigatória. Um hint repetido ou fora de ordem também é aceitável porque ele não representa a verdade do snapshot.

### 2. Hub somente no sandbox

- O hub será registrado apenas quando ambiente, configuração e host HTTPS loopback corresponderem exatamente à composição de teste.
- A composição normal da API não chamará `AddSignalR`, não mapeará o hub e não publicará hints.
- O hub será read-only do ponto de vista do navegador: nenhum método de mutação, inscrição arbitrária, escolha de grupo ou payload enviado pelo cliente será exposto.
- O escopo permitido será o mesmo da leitura do snapshot TV; uma identidade sem essa autorização será recusada antes de receber qualquer hint.
- Origem, esquema HTTPS e endereço loopback serão validados de forma fail-closed; CORS aberto e origem externa serão proibidos.
- A publicação ocorrerá somente depois do commit factual exercitado pelo sandbox. Uma falha de publicação não reverterá o commit nem transformará o hint em canal durável.

### 3. Autenticação exclusivamente de teste

Navegadores não podem depender de um header arbitrário durante o upgrade WebSocket. Para evitar token em URL, argumentos, logs ou storage JavaScript, a proposta recomenda uma credencial efêmera de teste entregue como cookie `HttpOnly`, `Secure` e `SameSite=Strict` pela mesma origem HTTPS loopback do harness.

- O cookie existirá somente no perfil efêmero do navegador dedicado.
- A identidade será limitada ao subject read-only do Dashboard TV sandbox e terá validade curta, limitada à fixture.
- O hub recusará cookie ausente, expirado, duplicado, malformado ou incompatível.
- O servidor validará também a origem exata; cookie isolado não será suficiente para aceitar uma origem diferente.
- O valor não será impresso, persistido no projeto, enviado em query string nem reutilizado fora do teste.
- O encerramento do navegador e a remoção do perfil efêmero eliminarão o material de teste; isso não será apresentado como arquitetura de IdP ou sessão operacional.

Esta opção é uma decisão proposta, não uma implementação já selecionada. Qualquer alternativa futura deverá oferecer proteção equivalente e passar por revisão de segurança antes do código.

### 4. Contrato mínimo e versionado

O hint terá um contrato próprio e exato, conceitualmente `dashboard-tv-change-hint.v1`, contendo somente:

- `schemaVersion`: identificador exato do contrato;
- `projectionRevision`: token opaco, limitado e não secreto para correlação e testes.

Campos adicionais, observações, itens de inventário, Agent ID, provider payload, endpoint, credencial, erro interno, timestamp de saúde ou conteúdo de banco serão proibidos. O cliente validará chaves exatas, tipo, tamanho e versão antes de aceitar o hint como simples gatilho.

`projectionRevision` não ordenará o snapshot, não atualizará freshness, não substituirá o `ETag` e não autorizará ignorar uma leitura. Versão desconhecida encerrará somente o canal de hint de forma fail-closed; o polling autoritativo continuará.

### 5. Coordenador único de reconciliação

O coordenador do Dashboard TV continuará dono de todas as leituras. A integração SignalR não chamará `fetch` diretamente.

- Concorrência máxima de snapshot: `1`.
- Estado pendente por sessão: no máximo um bit de hint e um bit de deadline periódico.
- Hints recebidos antes do início de uma leitura serão coalescidos em uma única solicitação.
- Hints recebidos durante uma leitura poderão produzir no máximo um follow-up, sujeito ao orçamento da sessão.
- O orçamento proposto permite no máximo duas leituras antecipadas por hints entre duas reconciliações periódicas; hints adicionais apenas marcam a sessão como possivelmente suja até o próximo polling.
- O deadline periódico não será cancelado ou adiado por uma leitura antecipada. Se vencer durante outra leitura, uma única reconciliação periódica ficará pendente e terá prioridade no follow-up.
- Uma leitura antecipada bem-sucedida poderá retornar `304`; isso não mudará timestamps factuais.
- O cliente não exibirá “tempo real” nem inferirá saúde a partir da presença do canal.

Esses limites são próprios do sandbox proposto. Eles não são capacidade, SLA ou dimensionamento operacional.

### 6. Sessão, cancelamento e reconnect

- Entrar no modo TV cria uma nova geração de sessão, inicia a leitura imediata e, somente depois da ativação exata, inicia o canal de hints.
- Sair do modo TV incrementa o fence, cancela hub, reconnect, timers e pedidos da sessão antiga e descarta callbacks tardios.
- Desconexão do hub não altera o snapshot nem o estado factual proveniente da API.
- Reconnect usará relógio injetável, backoff determinístico, limite de tentativas e cancelamento; ao esgotar o orçamento, o canal permanecerá desligado e o polling continuará.
- Uma reconexão não produzirá leitura por si só até que o handshake autenticado termine e um hint válido seja recebido; o polling cobre mudanças perdidas.
- Hints repetidos, perdidos ou reordenados não reiniciarão indefinidamente o backoff nem criarão catch-up ilimitado.

Os valores exatos do backoff, deadline, payload e validade da identidade deverão ser constantes nomeadas, justificadas e protegidas por testes no incremento futuro. Nenhum valor operacional é definido por esta proposta.

## Dependência e supply chain

O cliente oficial `@microsoft/signalr` não está declarado no manifesto ou lockfile, e a consulta direcionada ao cache npm local não retornou entrada correspondente. Portanto:

1. uma futura autorização que continue proibindo pacote novo e download não será suficiente para implementar este incremento;
2. o executor deverá parar antes de editar código se não houver autorização explícita para a dependência;
3. não será permitido implementar manualmente o protocolo SignalR, substituir por WebSocket/SSE próprio ou usar CDN;
4. uma autorização futura poderá permitir exclusivamente a seleção de uma versão exata e compatível do cliente oficial Microsoft, atualização determinística do manifesto/lockfile e acesso temporário ao registry npm oficial necessário para essa aquisição;
5. dependências transitivas, licença, integridade, advisories, tamanho e diff do lockfile deverão ser revisados antes de aceitar a alteração;
6. qualquer necessidade de outro pacote, registry, NuGet ou fonte externa interromperá o incremento e exigirá nova autorização.

Se Bruno preferir manter acesso externo e pacote novo proibidos, a decisão correta será adiar o Incremento 2, preservando o polling atual de 30 segundos.

## Threat model delimitado

| Ameaça | Contenção proposta | Evidência futura |
|---|---|---|
| Conexão não autenticada | cookie efêmero, mesma origem, política read-only e recusa fail-closed | handshake ausente/inválido/expirado recusado sem payload |
| Origem externa ou cross-site | HTTPS sobre IP loopback literal, `Origin` exata e sem CORS aberto | matriz de origem, host e esquema negativos |
| Hint tratado como verdade | coordenador exige nova leitura API e ignora conteúdo para apresentação | teste prova que UI não muda antes da resposta HTTP válida |
| Vazamento de observação/segredo | contrato de duas chaves, tamanho limitado e canários proibidos | serialização exata e secret/content scan |
| Flood de hints | uma leitura em voo, bits pendentes e orçamento entre pollings | contagem e concorrência máxima sob rajada |
| Replay ou reorder | hint não ordena snapshot; `ETag` e corpo API continuam autoritativos | duplicidade/reorder sem regressão ou tempestade |
| Callback de sessão antiga | generation fence e cancelamento integral | saída/reentrada com callback tardio descartado |
| Reconnect infinito | backoff, tentativas e tempo limitados; polling independente | relógio controlado e encerramento após budget |
| Downgrade/incompatibilidade | schema exato; canal fecha e polling continua | contrato N/N+1/N-1 e campos desconhecidos |
| Token em URL/log/storage | cookie `HttpOnly` de teste e sanitização | canários em URL, console, evidência e ficheiros |
| Ativação normal acidental | registro condicionado ao sandbox exato e testes de arquitetura | composição normal sem serviço, rota ou cliente |
| Falha após commit | hint best-effort; polling recupera a verdade | publicação perdida seguida de reconciliação periódica |

## Matriz de testes futuros

### Contrato e servidor

- serialização exata de `dashboard-tv-change-hint.v1` e recusa de campo desconhecido, tamanho excessivo ou versão incompatível;
- autenticação/autorização positiva somente para a identidade read-only de teste;
- cookie ausente, expirado, duplicado e malformado;
- origem, host, esquema e rota incompatíveis;
- ausência de método mutável chamável pelo cliente;
- emissão somente depois de mudança sintética commitada;
- perda ou falha de publicação sem alteração do estado autoritativo.

### Coordenador TypeScript

- entrada TV: uma leitura imediata e uma conexão de hint por sessão;
- hint idle: exatamente uma leitura antecipada;
- rajada durante leitura: concorrência `1` e no máximo um follow-up permitido;
- orçamento esgotado: polling continua e não há loop de leituras;
- deadline periódico durante leitura antecipada: um follow-up periódico prioritário;
- hint repetido/reordenado: nenhuma regressão de snapshot ou freshness;
- hint incompatível: canal encerrado, polling preservado;
- `304`: corpo e tempos factuais preservados;
- saída/reentrada: conexão, timers, pedidos e callbacks antigos cancelados/fenced;
- reconnect/backoff: sequência determinística, limite observado e cancelamento.

### E2E em navegador dedicado

Uma execução futura, somente se autorizada, usará o harness HTTPS loopback e um navegador dedicado com perfil efêmero. A linha do tempo mínima será:

1. entrar no modo TV e observar a leitura inicial;
2. confirmar a conexão SignalR autenticada;
3. commitar uma mudança sintética e observar um hint mínimo;
4. observar exatamente uma leitura antecipada da API;
5. confirmar que a tela só muda depois do snapshot HTTP válido;
6. avançar o relógio até a próxima reconciliação periódica sem que o hint tenha adiado o deadline;
7. repetir com `304`, duplicidade, reorder, desconexão, reconnect, auth negada e versão incompatível;
8. sair do modo TV durante trabalho em voo e confirmar fencing;
9. encerrar browser, API, hub e helpers e remover o perfil/pasta efêmeros;
10. comprovar zero processo, listener e artefacto temporário residual.

O navegador comum, suas sessões, cookies, extensões, abas e histórico permanecerão intocados.

## Escopo proposto para uma autorização futura

Uma autorização de implementação poderá abranger somente:

- contrato mínimo `dashboard-tv-change-hint.v1`;
- hub SignalR read-only registrado apenas no sandbox exato;
- autenticação efêmera de teste sob HTTPS loopback e mesma origem;
- adapter Dashboard sandbox que entrega hints ao coordenador existente;
- separação rigorosa entre hint e leitura autoritativa;
- polling serial de 30 segundos independente do canal;
- coalescência, budget, cancelamento, session fencing e reconnect/backoff limitados;
- testes unitários, integração e E2E Chrome dedicado com relógio controlado;
- atualização factual da documentação e cleanup integral;
- se explicitamente autorizado no mesmo ato, aquisição controlada apenas do cliente oficial `@microsoft/signalr` e respectivas alterações de manifesto/lockfile.

## Fora de escopo absoluto

Permanecerão proibidos:

- uso de hint como snapshot, estado, ordem, freshness ou prova de saúde;
- alteração ou remoção do polling autoritativo de 30 segundos;
- SignalR na composição normal, serviço permanente, backplane ou escala distribuída;
- hub público, origem externa, CORS aberto, CDN ou transporte artesanal;
- payload de observação, Agent, provider, endpoint, segredo ou conteúdo de banco;
- notificação Windows, e-mail, SMS, webhook ou qualquer canal externo;
- Agent/provider operacional, monitoramento real, PostgreSQL ou banco externo;
- IdP, PKI, vault, token service ou certificado operacional;
- comandos, `Start`, `Stop`, `Restart`, planejamento, executor ou ação externa;
- LLM, recomendação ou promoção `none → OBSERVER`;
- deploy, publicação, instalação, produção, `STATE-07` ou transição de estado;
- qualquer dependência, download ou registry além do cliente oficial explicitamente autorizado.

## Critérios de aceite propostos

O incremento futuro somente poderá ser classificado como concluído se houver evidência de que:

1. a composição normal não contém hub, cliente ou publisher SignalR ativo;
2. o sandbox exige HTTPS loopback, origem exata e identidade read-only de teste;
3. o hint possui contrato mínimo, exato, versionado e sem conteúdo operacional;
4. o hint nunca altera a tela nem freshness diretamente;
5. uma mudança commitada antecipa uma leitura API quando o orçamento permite;
6. a concorrência máxima de snapshot observada permanece `1`;
7. duplicidade, reorder e flood são coalescidos sem tempestade ou regressão;
8. o polling de 30 segundos continua independente, inclusive sem SignalR e após desconexão;
9. autenticação ausente/inválida e contrato incompatível fecham somente o canal de hint sem revelar estado;
10. saída do modo TV cancela conexão, reconnect, timers e pedidos e rejeita callbacks tardios;
11. a dependência oficial, se autorizada, fica fixada no lockfile e passa revisão de licença, integridade, advisories e diff;
12. nenhum cliente SignalR artesanal, CDN ou origem externa é introduzido;
13. o E2E usa somente navegador dedicado, perfil efêmero, dados sintéticos e loopback;
14. todos os runtimes e artefactos temporários são encerrados/removidos;
15. relatório, revisão direta do diff e gates automáticos aplicáveis distinguem observado, inferido e não testado;
16. o Human Gate próprio permanece separado, e nenhum estado ou modo é promovido.

## Entregáveis futuros

- contrato e threat-model atualizados;
- hub/publisher sandbox e autenticação exclusivamente de teste;
- adapter SignalR do Dashboard e evolução limitada do coordenador;
- testes determinísticos e E2E em navegador dedicado;
- inventário e evidência da dependência autorizada;
- relatório de implementação, achados classificados e cleanup;
- decisão automática e Human Gate próprios do incremento.

## Riscos e limitações residuais

- SignalR reduz latência percebida, mas não garante tempo real, entrega, ordem ou exactly-once.
- Um cookie local de teste não comprova IdP, SSO, expiração, revogação ou sessão operacional.
- Um único navegador e servidor loopback não comprovam proxy, balanceador, sticky session, backplane ou escala multi-cliente.
- O canal é best-effort; mudanças durante desconexão serão recuperadas somente pelo polling.
- O budget proposto protege o cliente local, mas não prova backpressure de frota ou defesa operacional contra DDoS.
- A versão do pacote oficial ainda não foi selecionada nem auditada porque acesso externo foi proibido neste pedido.
- Esta proposta não implementa notificações, comandos, providers, produção nem o Quality/Human Gate final do `STATE-06`.

## Rollback futuro

Se a implementação futura falhar em qualquer gate, o rollback será remover somente o hub/publisher sandbox, o adapter SignalR e a dependência explicitamente acrescentada, restaurando o coordenador de polling aceito. A rota snapshot, a cadência de 30 segundos, o Incremento 1 e a composição normal não serão reescritos. Nenhum rollback operacional ou externo será necessário porque o incremento permanecerá local.

## Entregáveis documentais deste pedido

Este pedido produz somente:

- esta proposta e sua delimitação de autoridade;
- referência factual no plano consolidado;
- atualização do estado corrente e registro append-only;
- validação documental local e commit focado.

Nenhum código, configuração executável, pacote, lockfile, teste, certificado, cookie, hub, cliente, browser ou runtime foi criado ou alterado.

## Decisão futura de Bruno

Bruno poderá ajustar, adiar ou rejeitar esta proposta. Como o cliente oficial não está declarado no projeto e a consulta direcionada ao cache local não retornou entrada correspondente, uma autorização futura de implementação deverá também decidir explicitamente sobre a dependência:

- **Autorizar aquisição controlada:** permitir somente `@microsoft/signalr`, versão exata escolhida e auditada, manifesto/lockfile e acesso temporário ao registry npm oficial; ou
- **Manter acesso externo proibido:** adiar o Incremento 2 e conservar exclusivamente o polling atual.

Nenhuma dessas opções é inferida por este documento. A implementação, o acesso externo, o pacote e o runtime continuam proibidos até uma decisão posterior explícita.

## Adendo factual posterior

Em 2026-07-19, Bruno autorizou separadamente a implementação local descrita nesta proposta e o acesso temporário exclusivo ao registry npm oficial para o cliente oficial e suas dependências transitivas. O incremento foi implementado no commit `c945c1b`; `@microsoft/signalr@10.0.0` ficou fixado no manifesto/lockfile, e o [relatório de implementação](STATE-06-Authenticated-SignalR-Change-Hint-Sandbox-Report.md) registra evidências, supply chain, limitações e Quality Gate automático restrito aprovado.

Este adendo não reescreve o escopo documental original nem constitui aceitação humana. O Human Gate próprio permanece pendente. Incremento 3, runtime operacional, notificações, promoção e transição continuam sem autorização.
