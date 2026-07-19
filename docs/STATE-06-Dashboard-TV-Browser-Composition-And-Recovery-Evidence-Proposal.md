# Proposta STATE-06 — Composição do Dashboard TV no Navegador e Evidência de Recuperação em Sandbox

## Status e autoridade

- Data: 2026-07-18
- Estado mantido: `STATE-06 INTEGRATION`
- Tipo: proposta exclusivamente documental
- Implementação, configuração executável, dependência, build, teste de produto e runtime: `NÃO AUTORIZADOS`
- Acesso externo, deploy, promoção e transição de estado: `NÃO AUTORIZADOS`
- Decisão de execução futura: `PENDENTE DE AUTORIZAÇÃO SEPARADA`

Este documento não inicia o incremento que descreve. Ele organiza uma opção pequena para decisão posterior e não constitui evidência de que os cenários propostos já foram executados.

## Resumo para não especialistas

O Dashboard TV já possui peças locais que sabem consultar uma API fictícia e segura para testes. Os testes existentes comprovaram separadamente a API e a lógica da tela, mas ainda não abriram a aplicação completa em um navegador para observar todas essas peças trabalhando juntas.

O próximo passo recomendado é criar futuramente um laboratório local e temporário que abra o Dashboard em um navegador isolado, consulte somente a API sandbox no próprio computador e confirme o comportamento diante de sucesso, ausência de mudança e falhas. Isso não ligará o DB-Notifier a banco, Agent, provider ou internet e não transformará a demonstração em monitoramento real.

## Nome recomendado do próximo incremento

`STATE-06 — Dashboard TV Browser Composition and Recovery Evidence Sandbox`

Em português: **Composição do Dashboard TV no Navegador e Evidência de Recuperação em Sandbox**.

## Por que este é o próximo passo

O incremento aceito no commit `70b3960` deixou duas camadas validadas:

1. a API HTTPS sandbox e seu snapshot autoritativo fictício foram exercitados por E2E local;
2. o adapter e o coordenador do Dashboard foram exercitados por testes TypeScript determinísticos.

O próprio [relatório do incremento](STATE-06-Dashboard-TV-Authoritative-Reconciliation-Report.md) registra que nenhum E2E da interface dentro de um navegador foi executado. Portanto, ainda falta comprovar a composição entre servidor, origem HTTPS, autenticação de teste, Fetch do navegador, entrada no modo TV, temporizador e apresentação factual de falhas.

Essa lacuna deve ser fechada antes de acrescentar SignalR. A reconciliação periódica é a fonte durável obrigatória; o SignalR seria apenas uma otimização posterior. Além disso, o cliente SignalR não pertence hoje às dependências bloqueadas do Dashboard, e esta proposta não autoriza download ou nova dependência.

## Baseline factual preservada

- O modo normal do Dashboard continua usando dados de demonstração claramente identificados.
- Somente a composição sandbox exata pode consultar o endpoint read-only `dashboard-tv.v1`.
- A entrada no modo TV solicita uma leitura imediata.
- A leitura seguinte é agendada 30 segundos depois de a anterior terminar, sem sobreposição.
- `ETag`/`304`, cancelamento, fencing da sessão e preservação do último snapshot válido já existem no escopo local aceito.
- A fonte do sandbox é uma fixture imutável em memória; ela não representa banco de dados ou ambiente operacional.
- Não existe SignalR no Dashboard, e a ausência dele não impede a reconciliação periódica.
- A remediação NuGet do commit `4732ed7` foi aceita e não é parte deste próximo incremento.

## Objetivo do incremento futuro

Produzir evidência repetível de que o Dashboard TV, executado em um navegador real e isolado, compõe corretamente a API sandbox existente e preserva a verdade factual sob sucesso, cache, falha, cancelamento e retomada, sem alterar a fonte de dados, ativar runtime normal ou acessar qualquer recurso externo.

O resultado deverá provar apenas o caminho local testado. Não deverá ser descrito como monitoramento, tempo real, produção, homologação de browser, saúde de provider ou prontidão operacional.

## Escopo proposto para uma autorização futura

### 1. Composição HTTPS exclusivamente local

- Criar um harness temporário que apresente Dashboard e API sob a composição HTTPS loopback esperada pelo adapter existente.
- Reutilizar somente certificados e identidade sintética de teste gerados para a execução local.
- Recusar host, origem, certificado, subject ou flag que não correspondam exatamente ao sandbox.
- Manter o modo normal e os comandos normais de execução sem acesso à fonte sandbox.
- Encerrar servidor, navegador dedicado, listeners e artefactos temporários ao final.

O harness local será infraestrutura de teste, não servidor de produção, proxy recomendado ou modelo de deploy.

### 2. Navegador dedicado e isolado

- Executar os cenários em um processo de navegador dedicado, com perfil temporário próprio.
- Não reutilizar janela, perfil, cookies, extensões, sessão autenticada ou histórico do navegador comum do utilizador.
- Limitar toda navegação à origem HTTPS loopback autorizada.
- Remover o perfil temporário somente depois que o processo dedicado terminar.

### 3. Cenário E2E positivo

- Confirmar que entrar no modo TV inicia exatamente uma leitura imediata.
- Confirmar que o snapshot fictício recebido da API aparece com origem, horário e freshness factuais.
- Confirmar que a próxima leitura começa somente após 30 segundos e depois da conclusão da anterior.
- Confirmar que sair e entrar novamente cancela a sessão anterior e não aceita conclusão tardia.
- Confirmar que o modo normal continua demonstrativo antes e depois do teste.

### 4. Cache e ausência de mudança

- Observar o envio do strong ETag recebido anteriormente.
- Confirmar que `304 Not Modified` não substitui o snapshot nem altera artificialmente seus horários de observação.
- Confirmar que `304` não transforma evidência antiga em evidência fresca.

### 5. Matriz de falhas e recuperação

Exercitar, de forma determinística e sem dados externos:

- acesso negado ou identidade de teste inválida;
- versão de contrato incompatível;
- resposta malformada ou maior que o limite;
- timeout, conexão local interrompida e resposta tardia após cancelamento;
- erro temporário seguido de recuperação na leitura posterior;
- saída do modo TV durante uma leitura;
- retomada após uma pausa controlada da página, sem leituras concorrentes.

Em todos os casos, a tela deverá preservar o último snapshot válido somente como evidência histórica e mostrar separadamente `denied`, `incompatible`, `error`, `offline` ou `stale`, conforme o caso. Uma falha jamais poderá ser apresentada como estado saudável ou atualizado.

### 6. Evidência, revisão e limpeza

- Registrar comandos, versões, portas loopback, duração, resultados e exit codes de forma sanitizada.
- Registrar contagens de requisições e concorrência máxima observada, sem capturar tokens, chaves ou material privado.
- Executar os gates de arquitetura, integração, frontend, documentação, links, secrets e escopo aplicáveis às alterações futuras.
- Fazer revisão direta do diff e classificar achados por gravidade.
- Demonstrar ao final zero processo e zero listener pertencente ao sandbox.

## Fora de escopo absoluto

Este incremento futuro não deverá incluir:

- SignalR, WebSocket, server push, notificações ou qualquer canal externo;
- nova dependência, download, restore por rede, CDN ou acesso à internet;
- Agent ou provider operacional, probes, monitoramento, observações reais ou credenciais reais;
- PostgreSQL ou outro banco, persistência externa ou estado operacional durável;
- IdP, PKI, vault, token service ou certificado operacional;
- comandos administrativos, `Start`, `Stop`, `Restart`, planejamento ou execução;
- LLM, recomendação, executor ou promoção `none → OBSERVER`;
- serviço permanente, worker normal, API pública, instalação, deploy, publicação ou telemetria;
- alteração da cadência autoritativa de 30 segundos ou promessa de tempo real;
- reutilização ou controlo do navegador comum do utilizador;
- promoção para `STATE-07`, produção, release ou transição de estado.

## Critérios de aceite propostos para a implementação futura

O incremento somente poderá ser classificado como concluído se houver evidência de que:

1. a ativação exige simultaneamente a composição sandbox exata, HTTPS loopback e identidade de teste válida;
2. o modo normal permanece demonstrativo e desabilitado para a API sandbox;
3. a leitura imediata acontece uma vez ao entrar no modo TV;
4. a cadência posterior é de 30 segundos após a conclusão anterior e a concorrência máxima observada é `1`;
5. `ETag`/`304` preserva conteúdo e timestamps sem alegar freshness nova;
6. cancelamento e fencing impedem atualização por resposta tardia;
7. todos os casos negativos preservam a última evidência válida com estado factual de falha ou staleness;
8. a recuperação posterior ocorre por nova leitura autoritativa, nunca por inferência do cliente;
9. o navegador usa perfil temporário isolado e somente loopback;
10. nenhum segredo, dado real, pacote novo ou acesso externo é necessário;
11. todos os runtimes temporários e listeners são encerrados ao final;
12. o relatório distingue claramente observado, inferido, não testado e fora de escopo;
13. os gates automáticos aplicáveis passam e a decisão humana do incremento permanece separada;
14. nenhum texto ou configuração sugere `OBSERVER`, produção, homologação ou transição.

## Riscos e limitações que permanecerão

- Um navegador local e uma API fictícia não comprovam proxy, CORS, TLS corporativo, IdP ou infraestrutura de produção.
- A identidade sintética não comprova provisionamento, rotação ou revogação operacional de credenciais.
- Uma execução em um navegador não homologa outras versões, sistemas operativos, dispositivos ou condições prolongadas de suspensão.
- A matriz local não prova carga multi-cliente nem backpressure de frota.
- Sem SignalR, uma mudança continua visível apenas na próxima leitura periódica; isso é uma latência conhecida, não perda de verdade autoritativa.
- A fonte continua fictícia e não prova conexão, autenticação ou saúde de banco de dados.
- Este incremento não completa `STATE-06` e não atende por si só ao Quality/Human Gate de saída do estado.

## Entregáveis documentais deste pedido

Este pedido produz somente:

- esta proposta e sua delimitação de autoridade;
- atualização factual do estado atual;
- registro append-only da solicitação e da proposta;
- validação documental local e commit focado.

Nenhum harness, teste, contrato, configuração, fonte ou runtime foi criado ou alterado.

## Decisão solicitada a Bruno

Bruno pode ajustar, adiar, rejeitar ou autorizar separadamente a implementação futura. Se concordar exatamente com o escopo, poderá responder:

> AUTORIZO o incremento restrito de STATE-06 — Dashboard TV Browser Composition and Recovery Evidence Sandbox, limitado a harness HTTPS loopback temporário, autenticação exclusivamente de teste, composição do Dashboard TV e API sandbox em navegador dedicado com perfil efêmero, E2E da leitura imediata e cadência serial de 30 segundos, ETag/304, cancelamento, fencing, matriz determinística de falhas/recuperação, evidência sanitizada e encerramento integral dos runtimes ao final. Permanecem proibidos acesso externo, novas dependências ou downloads, SignalR, notificações, Agent/provider operacional, monitoramento, persistência ou banco externo, IdP/PKI/vault reais, comandos, LLM, executor, deploy, promoção e transição de estado.

Essa eventual resposta autorizaria somente a implementação local descrita. Qualquer ampliação continuaria exigindo uma nova decisão explícita.

## Resultado posterior da proposta

Bruno forneceu depois exatamente a autorização delimitada acima. A implementação local foi concluída sob essa autoridade e está documentada no [relatório de composição e recuperação no navegador](STATE-06-Dashboard-TV-Browser-Composition-And-Recovery-Evidence-Report.md). O Quality Gate automático restrito foi aprovado e Bruno aceitou depois o Human Gate próprio com todas as limitações registradas. Essa decisão autorizou exclusivamente seu registro factual e proibiu novo incremento, runtime operacional, promoção e transição. Esta atualização não altera a natureza originalmente documental desta proposta, não promove o produto e não concede autoridade adicional.
