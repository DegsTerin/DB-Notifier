# Relatório STATE-06 — Command Transport Safety E2E Sandbox

## Status e autoridade

- Data: 2026-07-19.
- Estado mantido: `STATE-06 INTEGRATION`.
- Incremento técnico: implementação base no commit `3f23d3e` e correção final de escopo das fixtures no commit `54a65f5`.
- Autoridade: autorização explícita de Bruno limitada ao sandbox local descrito na [proposta](STATE-06-Command-Transport-Safety-E2E-Sandbox-Proposal.md).
- Quality Gate automático deste incremento: `APROVADO` no escopo local documentado.
- Human Gate próprio do Incremento 4: `PENDENTE`.
- Campanha consolidada, runtime operacional, promoção e transição: `NÃO AUTORIZADOS` e não executados.

## Resultado em linguagem simples

O incremento construiu um laboratório local para provar a entrega segura de mensagens deliberadamente não executáveis. O Agent grava cada pedido antes de usar a rede. O servidor grava a identidade, a sequência, o hash e a resposta antes de devolvê-la. Se a resposta se perde, um processo novo repete exatamente a mesma mensagem e recebe a resposta já registrada, sem repetir a mudança.

O laboratório transportou duas fixtures sintéticas: uma foi registrada como `Acknowledged`, que significa somente “recebida e persistida”, e a outra terminou como `Unsupported`. Uma fixture vencida terminou como `Expired`; uma versão incompatível e um controle neutro fora do namespace permitido nunca foram entregues. Nenhum item alcançou `Running`, `Succeeded`, `Failed` ou `UnknownOutcome`, e nenhum `CommandAttempt`, resultado de provider ou post-probe foi criado.

Isso não habilita comandos no DB-Notifier normal. A API normal não registra os endpoints v2 do sandbox. O Worker normal continua com command polling desabilitado por padrão e recusa sua ativação com `command.polling_durable_protocol_unavailable`. Não existe executor, chamada de shell, controle de processo, serviço, banco ou infraestrutura neste incremento.

## Implementação factual

### Contratos e limites

- contratos v2 distintos para poll, acknowledgement, respostas e erro tipado, com campos desconhecidos recusados;
- `messageId`, resposta própria, `inReplyToMessageId`, sequência monotónica por Agent e SHA-256 do JSON exato persistido;
- lotes de até `16`, corpo HTTP de até `64 KiB`, parâmetros JSON de até `4 KiB` e até `32` versões de provider;
- timeout de `5 s` por tentativa, no máximo duas repetições, backoff de `100 ms` e `250 ms` e lease local de `20 s`;
- gate sem espera com concorrência máxima observável igual a `1`, cancelamento propagado e resposta `429` tipada quando ocupado;
- somente capabilities com prefixo `sandbox.command.` e `CommandExecutionPolicy.Never` passam a fronteira Agent.

### Persistência e migrations

- Agent SQLite: tabela `command_transport_state` com próxima sequência, mensagem pendente, tipo, JSON, hash, tentativas, lease e fencing; inbox e preparação do acknowledgement são atómicas;
- Server: cursor por Agent e journal de request/response, com unicidade de `messageId`, `agentId + sequence` e `responseMessageId`;
- migrations `AddCommandTransportSafetySandbox` geradas para Agent SQLite e Server PostgreSQL, com rollback e atualização dos snapshots;
- nenhuma migration foi aplicada a PostgreSQL ou banco externo; somente bancos efêmeros de teste, modelos locais e scripts offline foram exercitados.

### Transporte e isolamento

- endpoints v2 existem apenas como extensão chamada pelo host de integração, sob HTTPS loopback, mTLS e identidade P-256 exclusivamente de teste;
- o cliente limita a resposta antes de materializá-la, exige versão/media type e converte recusas de identidade em erro sanitizado;
- o executável temporário exige o switch exato `--sandbox-command-transport`, diretório temporário próprio, SQLite local existente, HTTPS loopback e identidade recebida por pipe privado;
- a composição normal da API e do Agent Worker não referencia nem ativa o coordinator v2.

## Sequência E2E observada

1. O teste criou API HTTPS em `127.0.0.1`, CA/certificados P-256 e enrollment exclusivamente de teste, além de Agent SQLite em diretório temporário próprio.
2. Antes do fluxo válido, schema incompatível retornou `426`, gap de sequência retornou `409` com próxima sequência `1`, corpo acima de `64 KiB` retornou `413` e Agent divergente da rota retornou `403`; nenhuma dessas recusas avançou o journal.
3. O primeiro processo Agent enviou o poll de sequência `1`. O servidor o commitou uma vez, mas o harness descartou as três respostas permitidas. O processo terminou com falha controlada; o Agent preservou a mesma mensagem e hash, três tentativas e inbox vazia. O servidor manteve um único registro.
4. Um segundo processo reabriu o mesmo SQLite, repetiu o poll exato, gravou duas fixtures na inbox e preparou atomicamente o acknowledgement de sequência `2`. O servidor o commitou uma vez, mas as três respostas foram novamente descartadas. O Agent preservou o acknowledgement exato e o servidor manteve somente dois registros no journal.
5. Um terceiro processo repetiu o acknowledgement exato, recebeu a resposta persistida e limpou a pendência. A inbox terminou com uma fixture `Acknowledged` e outra `Unsupported`; `CompletedAt` e `ResultJson` permaneceram nulos.
6. Um poll vazio de sequência `3` foi pausado depois do commit. A revogação foi então commitada e, ao liberar a barreira, somente a resposta histórica já decidida retornou. Uma nova tentativa depois da revogação foi recusada antes do store e não avançou o cursor `3`.
7. As inspeções finais confirmaram a fixture vencida como `Expired`, a incompatível e o controle neutro fora do namespace como `Pending` e não entregues, zero attempts/observações/eventos/outbox/notificações e zero estado de execução.
8. Todos os filhos, API, listeners, pipes, conexões e diretórios temporários pertencentes ao teste foram encerrados ou removidos.

## Falhas, retry, cancelamento e fencing

- perda de resposta depois do commit de poll e acknowledgement foi exercitada em processos reais separados;
- falha SQLite antes do commit conjunto de inbox/acknowledgement reverteu tudo e preservou o poll pendente;
- cancelamento durante o envio preservou identidade, sequência, hash e tentativa para replay posterior;
- duas perdas seguidas usaram a mesma identidade e os backoffs exatos; a terceira tentativa concluiu sem criar mensagem nova;
- lease expirado entregou fence maior ao novo proprietário e o fence antigo foi recusado;
- replay exato devolveu a resposta persistida; os testes confirmaram conflito e gap sem avanço, enquanto a mesma regra de store recusa reorder desconhecido;
- IDs ou idempotency keys duplicados na mesma resposta são recusados antes da mutação local.

## Achados da revisão direta e correções

1. **Alta — limite HTTP inicialmente usava uma feature como metadata.** Foi substituído por `IRequestSizeLimitMetadata`; o E2E confirmou `413` antes da desserialização integral.
2. **Alta — resultado de acknowledgement poderia divergir da disposição enviada.** O Agent agora aceita somente transições compatíveis e recusa conflito antes de concluir a inbox.
3. **Média — recusa de autenticação sem corpo tipado produzia classificação genérica.** `401/403` agora viram `agent.identity_inactive` sanitizado e não retryable.
4. **Média — lease de `10 s` não cobria três timeouts máximos.** O limite passou a `20 s`, suficiente para `3 × 5 s` mais os dois backoffs do sandbox.
5. **Média — resposta poderia depender do erro de unicidade para detectar duplicidade interna.** IDs e idempotency keys duplicados agora falham antes de consultar ou gravar a inbox.
6. **Média — parâmetros limitados por tamanho ainda podiam não ser JSON.** Server e Agent agora exigem um objeto JSON válido antes de transportar ou persistir.
7. **Baixa — seleção limitada do Server não tinha ordem SQL explícita.** Uma ordem determinística precede o limite e remove o aviso/indeterminação do provider de teste.
8. **Ferramenta — EF gerou primeiro migrations vazias a partir de binários Debug antigos.** Elas foram removidas antes do registro, os projetos foram recompilados e as migrations finais foram regeneradas e inspecionadas. Nenhum banco externo foi contactado ou alterado.

Não restou achado crítico, alto ou médio conhecido no diff final.

## Verificações automáticas

- shutdown preflight inicial: zero processo e zero listener DB-Notifier;
- solução .NET 10: `16` projetos, build Release `--no-restore`, zero erro e zero aviso;
- testes .NET: `332/332` unitários, `25/25` de arquitetura e `14/14` de integração;
- testes próprios: `5` unitários, `3` de arquitetura e `1` E2E entre processos;
- cobertura .NET: `78,91%` linhas e `49,51%` branches, acima dos pisos `70%`/`45%`;
- Dashboard: toolchain, TypeScript, Vite, tokens, marca, localização, provider icons e `60/60` testes aprovados;
- Pester legado: `23` aprovados, um skip condicional previsto e cobertura `32,08%` (`290/904` comandos), executado em Windows PowerShell 5.1 porque o matcher `Should Throw` do Pester 3.4 não é compatível com PowerShell Core 7.6 neste host;
- `dotnet format --verify-no-changes`, documentação de código para `273` fontes, `390` links Markdown locais em `92` arquivos, fixture NuGet offline para `16` projetos, secret scan e smoke fail-closed: aprovados;
- cleanup final: zero processo, zero listener e zero diretório `dbnotifier-command-transport-sandbox-*` remanescente.

## Observado, inferido e não testado

### Observado

- request persistido antes da rede, replay do mesmo ID/hash após perda de resposta e restart real do processo Agent;
- journal com uma mutação por sequência e resposta estável para replay;
- versão, gap, tamanho, identidade errada e revogação recusados sem avanço indevido;
- expiração, `Unsupported`, cancelamento, retry/backoff, fault injection, fencing e ausência total de attempts/execução;
- composição normal fail-closed e cleanup integral do runtime temporário.

### Inferido por inspeção direta e protegido por arquitetura

- endpoints v2 não são registrados pelo `Program` normal;
- o Worker normal não compõe o client/coordinator v2 e continua recusando polling;
- os módulos novos não contêm executor, shell, processo, provider ou post-probe;
- nenhuma nova dependência, package, lockfile, fonte NuGet ou acesso externo foi introduzido.

### Não testado

- PostgreSQL real, migration externa, restart do processo Server com journal em disco ou recuperação após power-loss físico;
- PKI, IdP, vault, token service, rotação, revogação distribuída ou credencial operacional;
- múltiplos Agents, carga, endurance, fairness, sizing, disco cheio, corrupção, restauração integral do storage ou host local malicioso;
- comando operacional, autorização administrativa, `CommandAttempt`, executor, provider, post-probe, efeito em processo/serviço/banco/infraestrutura;
- deploy, produção, observabilidade operacional, campanha consolidada, `OBSERVER`, `STATE-07` ou transição.

## Limitações e condições residuais

- O E2E reinicia processos Agent contra um Server sandbox mantido no mesmo processo. O journal Server é provado por replay, transação, modelo e testes, mas não por restart de PostgreSQL ou de um processo Server com arquivo em disco.
- O Server E2E usa SQLite em memória como substituto local. A migration PostgreSQL foi gerada e seu script/rollback foram inspecionados offline; não houve aplicação em PostgreSQL.
- A mensagem pendente preserva identidade, sequência, hash e tentativas, mas não possui deadline absoluto persistido entre reinícios. O sandbox aplica timeout por tentativa, retry limitado e lease; uma política operacional de expiração da intenção exigiria decisão futura.
- O gate de backpressure Server é global ao processo sandbox, portanto é mais restritivo que “um por Agent” e não constitui desenho de fairness ou capacidade de frota.
- A tentativa recusada depois da revogação permanece pendente no SQLite Agent para evidência e replay fail-closed. Não existe dead-letter, resolução administrativa ou limpeza automática autorizada.
- O teste de corrida prova os dois lados do commit de revogação dentro do host local; não prova PKI distribuída, atraso de propagação ou restauração de storage.
- Os certificados e a CA são efêmeros e exclusivamente de teste. O PKCS#12 passa apenas por pipe privado, seu buffer é zerado e nenhuma inspeção independente de contêiner temporário do perfil Windows foi realizada.
- Os limites numéricos são envelope de segurança do laboratório, não sizing operacional.

## Classificação dos gates

- Quality Gate automático deste Incremento 4 restrito: **APROVADO** no escopo local documentado.
- Human Gate próprio do Incremento 4: **PENDENTE**; não foi inferido da autorização de implementação nem dos testes.
- Incrementos 1–3: permanecem **ACEITOS COM AS LIMITAÇÕES REGISTRADAS** nas decisões anteriores.
- Campanha Quality Gate consolidada e Human Gate final do `STATE-06`: **NÃO EXECUTADOS E NÃO AUTORIZADOS**.
- Runtime operacional, comandos reais, promoção `none → OBSERVER`, `STATE-07`, deploy, produção e transição: **NÃO AVALIADOS E NÃO AUTORIZADOS**.

## Próxima decisão

Bruno deve revisar principalmente `Resultado em linguagem simples`, `Sequência E2E observada`, `Limitações e condições residuais` e `Classificação dos gates`. A decisão deve aceitar com as limitações, solicitar uma remediação delimitada ou rejeitar somente este incremento.

Uma eventual aceitação autorizará exclusivamente o registro factual dessa decisão. Ela não autorizará a campanha consolidada, runtime operacional, comando, promoção ou transição de estado.

Se concordar, a decisão exata sugerida é:

> Incremento STATE-06 Command Transport Safety E2E Sandbox, commit 54a65f5: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo campanha consolidada, novo incremento, runtime operacional, promoção nem transição de estado.
