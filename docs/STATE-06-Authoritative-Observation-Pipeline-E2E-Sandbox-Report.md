# Relatório STATE-06 — Authoritative Observation Pipeline E2E Sandbox

## Resultado em linguagem simples

O Incremento 1 do plano consolidado foi concluído dentro do laboratório local autorizado. Uma fonte inteiramente fictícia produziu estados canônicos não saudáveis sem consultar banco monitorado, provider operacional ou internet. Seis processos Agent temporários e separados reabriram o mesmo SQLite de teste, preservaram e reenviaram a outbox, autenticaram-se numa API HTTPS loopback com certificado efêmero e alimentaram uma projeção read-only consumida pelo contrato existente do Dashboard TV.

O cenário comprovou indisponibilidade local e reconexão, perda determinística de uma resposta já aceita, replay exato sem efeito duplicado, lote entregue fora de ordem, timestamps envelhecidos com `ETag` estável, rejeição de protocolo/schema incompatível e headers inválidos. Uma sexta publicação permaneceu em voo depois de autenticação e validação; a revogação foi confirmada antes da entrada na persistência, e a publicação foi então recusada sem alterar o último snapshot. Ao final, nenhum processo ou listener do sandbox permaneceu ativo.

Isso não ativa monitoramento real. O provider sintético existe somente no harness de teste, o Worker normal continua desabilitado, a composição normal da API não registra a projeção dinâmica e o Dashboard normal continua demonstrativo. Não houve SignalR, notificação, comando, executor, provider/banco operacional, acesso externo, deploy, LLM, promoção ou transição de estado.

## Autoridade e escopo

- Data local de conclusão: 2026-07-19; autorização recebida em 2026-07-18.
- Estado mantido: `STATE-06 INTEGRATION`.
- Autoridade: autorização explícita de Bruno para o `Incremento 1 — Authoritative Observation Pipeline E2E Sandbox` do [plano consolidado](STATE-06-Consolidated-Closure-Plan.md).
- Permitido: fonte provider-neutral exclusivamente sintética; observações canônicas versionadas; Agent SQLite/outbox; HTTPS/mTLS de teste; ingestão e projeção read-only; snapshot TV; processos e runtimes temporários exclusivamente locais; reinício, offline/reconexão, replay, duplicidade, reorder, staleness, revogação e incompatibilidade.
- Proibido e não executado: acesso externo, dependência/download, provider ou banco operacional, SignalR, notificações, comandos, executor, serviços permanentes, deploy, LLM, promoção e transição de estado.
- Commit local da implementação e deste relatório: `3449918`.

## Implementação concluída

### Contrato e rota de ingestão

- A porta assíncrona `IDashboardTvSnapshotSource` separa o endpoint read-only da origem concreta do snapshot.
- A fixture imutável anterior continua sendo a implementação padrão do sandbox Dashboard TV.
- A rota real `/api/v1/agents/{agentId}/observations:batch` foi extraída da entrada do Server para uma extensão reutilizável, sem criar rota paralela.
- O endpoint exige exatamente um valor para protocolo, schema e Agent version. Protocolo/schema devem corresponder à versão corrente; Agent version é somente um identificador obrigatório, limitado e com alfabeto estável. Valor ausente, duplicado, acima do limite ou sintaticamente inválido falha com `426` antes da ingestão. Não foi implementada negociação de compatibilidade por versão do Agent.
- O transporte e o dispatcher agora recusam respostas ambíguas, desconhecidas ou duplicadas como `sync.response_invalid`; nenhum resultado contraditório pode reconhecer terminalmente a outbox.

### Projeção read-only do Dashboard TV

A projeção sintética:

- é registrada somente pelo host E2E opcional e nunca pelo `Program` normal;
- lê instância, estado reconciliado e amostra de origem numa transação serializável;
- consulta no máximo `501` estados como sentinela e falha fechado acima do limite público de `500` antes de buscar as amostras;
- exige coincidência exata de Agent, instância, provider, observação, sequência, status e timestamps;
- aceita somente `EvidenceLevel.Synthetic` e recusa `Healthy`, preservando a regra canônica que proíbe saúde sintética;
- nunca projeta endpoint, referência de credencial, tags, Agent ID, erro ou detalhe provider-native;
- deriva `GeneratedAt` da maior mudança reconciliada, mantendo corpo e `ETag` estáveis quando nada factual muda.

### Harness Agent isolado

O projeto temporário existente `DBNotifier.AgentFleet.SandboxHost` ganhou um modo exato `--sandbox-observation-pipeline`. Esse modo:

- valida argumentos não secretos, raiz/pasta pertencente à fixture, HTTPS sobre IP loopback literal, Agent, assignment e provider sintético exatos;
- recebe o PKCS#12 de teste e a pinagem do Server por named pipe limitado ao utilizador corrente, nunca por argumentos, ambiente ou SQLite;
- usa a cadeia real `MonitoringCycleRunner` → provider-neutral adapter → `AgentObservationOutboxSink` → `AgentOutboxStore` → `AgentOutboxDispatchRunner` → `HttpObservationBatchTransport`;
- reabre o SQLite Agent migrado em cada processo e não referencia Worker, Server persistence, Server API ou provider operacional;
- não resolve credential, não abre conexão com database monitorado/operacional e não cria comando ou serviço permanente; somente o SQLite Agent pertencente à fixture é aberto;
- emite somente quatro contagens agregadas e sanitizadas no stdout;
- zera o buffer PKCS#12, encerra o processo e permite a remoção segura do SQLite/pasta temporária pelo teste pai.

## Sequência E2E observada

| Etapa | Processo/ação local | Evidência observada |
|---|---|---|
| Enrollment e assignment | processo de teste pai | Agent de teste inscrito; assignment read-only sem credencial de monitoramento reconciliado para o SQLite Agent |
| Offline | filho 1, listener loopback deliberadamente ausente | observação `Degraded` e sequência `1` persistidas; outbox manteve `1` item retryable |
| Reconexão | filho 2, API HTTPS/mTLS ativa | sequência `1` aceita; snapshot mostrou `degraded` com origem sintética |
| Perda de resposta | filho 3 | Server aceitou sequência `2`/`Unavailable`; adapter descartou deterministicamente a resposta aceita; outbox permaneceu retryable |
| Replay exato | filho 4 | outbox recebeu reconhecimento terminal e as contagens de amostra/estado/evento permaneceram unitárias; a disposition interna `Duplicate` é sustentada pelo contrato/store e regressões unitárias, não exposta pelo stdout agregado do filho |
| Reorder | filho 5 | sequências `4` e `3` foram enviadas nessa ordem; o cursor só avançou contiguamente e terminou em `4`/`Unavailable` |
| Compatibilidade e headers | chamadas mTLS diretas | schema incompatível, Agent version sintaticamente inválida, protocolo ausente e protocolo duplicado retornaram `426`; estado e `ETag` não mudaram |
| Staleness | relógio avançado seis minutos | timestamps anteriores foram preservados; leitura condicional retornou `304` com o mesmo `ETag` |
| Revogação em sincronização | filho 6 e endpoint humano de teste | a requisição mTLS da sequência `5` foi pausada pelo fixture depois de autenticação/validação e antes do store; a revogação commitou primeiro, o store retornou `Rejected/agent.not_active`, e o último snapshot permaneceu inalterado |
| Cleanup | descarte dos fixtures | seis filhos aguardados/encerrados, Kestrel parado, pools SQLite limpos, pasta/banco efêmeros removidos e zero processo DB-Notifier residual |

As contagens finais foram:

- Agent SQLite: `5` observações sintéticas, sequências `1–5`, e `5` envelopes terminalmente reconhecidos; tentativas por sequência `2, 2, 1, 1, 1`;
- Server SQLite efêmero: `4` amostras aceitas, cursor contíguo `4`, estado final `Unavailable`, `4` eventos canônicos e `4` mensagens server-outbox não publicadas;
- efeitos proibidos: `0` notification deliveries, `0` administrative commands e `0` command attempts.

## Achados da revisão direta e correções

1. **Alta — proveniência de sequência incompleta.** A primeira projeção comparava observação, Agent, instância, provider, status e tempo, mas não comparava `HealthSample.Sequence` com `InstanceObservationState.LastProcessedSequence`. A igualdade agora é obrigatória e possui regressão dedicada.
2. **Alta — resposta HTTP ambígua poderia reconhecer o primeiro resultado.** O dispatcher agrupava resultados repetidos e aceitava o primeiro. Transporte e runner agora recusam lote com ID repetido, desconhecido ou disposition inválida como resposta integralmente retryable; dois testes protegem o comportamento.
3. **Alta — estado `Healthy` sintético poderia atravessar uma linha legada/corrompida.** A ingestão já proibia essa combinação, mas a projeção aceitava o enum isoladamente. O adapter agora recusa explicitamente `Healthy + Synthetic`, com teste de regressão.
4. **Média — validação parcial dos headers.** A rota anterior verificava somente o protocolo. A rota extraída agora exige um protocolo, um message schema e um identificador Agent version limitado/estável. O E2E cobre schema incompatível, Agent version sintaticamente inválida e protocolo ausente/duplicado. Não existe range ou negociação N-1/N+1 de Agent version, e o relatório não os apresenta como implementados.
5. **Média — alteração compartilhada de fixture regrediu um cenário anterior.** A primeira composição removeu a referência sintética de monitoramento da fixture usada pelos testes legados. A opção credential-free foi isolada somente no novo cenário; a suíte completa confirmou a baseline anterior intacta.
6. **Média — fonte dinâmica scoped tornava o snapshot vazio instável.** Uma nova instância podia ancorar `GeneratedAt` novamente e mudar o `ETag` sem mudança factual. O host E2E agora usa uma única fonte singleton, há regressão de snapshot vazio estável e a composição sandbox normal possui teste explícito que exige a fixture imutável.
7. **Média — falha parcial no IPC não limpava todos os buffers recebidos.** Se um frame posterior falhasse, material anterior ou payload incompleto podia sobreviver até a coleta. O leitor agora tem ownership explícito e zera buffers em todos os caminhos de falha; um E2E multiprocesso fecha o pipe no meio do PKCS#12 e confirma saída fail-closed sanitizada.
8. **Média — revogação inicialmente ocorria antes de iniciar a sexta requisição.** O E2E agora pausa a requisição já autenticada e validada antes da entrada no store, confirma a revogação, libera a requisição e exige `Rejected/agent.not_active`, cursor `4` e snapshot inalterado. A barreira pertence somente ao host E2E e não altera produto.
9. **Baixa — asserção SQLite ordenava `DateTimeOffset` no provider.** O primeiro E2E completou o fluxo, mas falhou na verificação final porque SQLite não traduz essa ordenação. A ordenação desnecessária foi removida e o cenário completo passou.
10. **Baixa — referência de projeto/lockfile gerada sem necessidade.** Uma referência direta redundante ao SDK de providers alterou transitoriamente um lockfile. A referência foi removida; o harness compila pela dependência transitiva existente e nenhum projeto, pacote ou lockfile permanece alterado.
11. **Baixa — `Uri.IsLoopback` admitia hostname.** O novo modo de observação agora exige HTTPS sobre endereço IP loopback literal, evitando depender de resolução de `localhost`; o E2E continua usando `127.0.0.1`.
12. **Baixa — summaries legados descreviam somente heartbeat.** A documentação do harness e das APIs modificadas foi alinhada aos dois modos temporários e às regras de resposta não ambígua, sem mudar comportamento operacional.

Não restou achado crítico ou alto conhecido no diff final. A corrida interna em que uma revogação ocorre depois da consulta `Active`, mas antes do commit da mesma transação de ingestão, não foi exercitada; a evidência prova requisição em voo com revogação commitada antes da entrada no store, não linearização em qualquer ponto arbitrário da transação.

## Verificações automáticas

- shutdown preflight: `0` processo DB-Notifier pertencente ao projeto antes da ação;
- solução .NET 10: `16` projetos, build Release com `--no-restore`, `0` erros e `0` avisos;
- testes .NET: `316/316` unitários, `17/17` de arquitetura e `10/10` de integração;
- E2E focado: `2/2` — pipeline integrado e falha IPC — aprovados após processos-filhos temporários e cleanup;
- cobertura .NET: `78,48%` linhas e `52,24%` branches, acima dos pisos `70%`/`45%`;
- `dotnet format --verify-no-changes --no-restore`: aprovado;
- legado/Pester: gate aprovado com `23` testes identificados, `1` skip condicional previsto e cobertura `32,08%` (`290/904` comandos);
- Dashboard: Node `24.18.0`/npm `11.16.0`, assets/tokens/localisation/provider-icons, TypeScript, `57/57` testes e build Vite aprovados sem instalação;
- documentação: `250` fontes comment-capable e `356` links Markdown locais em `86` arquivos aprovados;
- fixture NuGet offline completa para `16` projetos, secret scan, smoke fail-closed e validação do bundle de compatibilidade aprovados;
- cleanup final: `0` processo, `0` listener e `0` pasta temporária DB-Notifier residual;
- nenhuma dependência, projeto, solution entry, package, lockfile ou migration foi acrescentada.

Auditorias NuGet/npm online permanecem deliberadamente não executadas porque acesso externo e downloads foram proibidos. A fixture NuGet verifica completude/fail-closed do relatório sintético e não é atestação atual de advisories de registry.

## Observado, inferido e não testado

### Observado

- persistência/outbox Agent SQLite atravessando seis processos-filhos reais;
- indisponibilidade de listener loopback, retry/backoff, reconexão e acknowledgement;
- ingestão HTTPS/mTLS e recusa `agent.not_active` de uma requisição em voo depois de a revogação commitada, com material exclusivamente de teste;
- replay exato sem efeito duplicado observado, sequência invertida e cursor contíguo `4`;
- snapshot read-only, `ETag` forte, `304`, timestamps antigos e último estado preservado;
- contagens locais e centrais descritas acima;
- todos os gates automáticos explicitamente quantificados neste relatório.

### Inferido por inspeção direta

- isolamento da composição normal, porque o provider sintético existe somente no executável de teste e o adapter de projeção não é registrado pelo Server normal;
- ausência de vazamento de endpoint/credencial/erro, pela seleção explícita de campos e pelos canários unitários serializados;
- ausência de provider/serviço operacional, pelas referências de assembly protegidas pelo teste de arquitetura;
- zero dependência nova, pela revisão de projetos, manifests e lockfiles.
- disposition interna `Duplicate` do replay exato, pelo contrato/store inspecionado e regressões unitárias; o resultado agregado do filho não expõe essa enumeração.

### Não testado

- provider, driver, database, credencial, PostgreSQL central ou SQLite monitorado real;
- PKI, key store, token service, rotação, distribuição ou revogação operacional;
- disputa dentro da transação entre a consulta de Agent ativo e o commit; a prova em voo pausa antes da entrada no store;
- perda física de energia, corrupção de disco, carga, endurance, fleet-wide fairness ou disaster recovery;
- PostgreSQL serializable real, múltiplas instâncias do Server ou concorrência multiprocesso de dispatch;
- compatibilidade ou negociação N-1/N+1 de Agent version; o teste prova protocolo/schema corrente e validação sintática do identificador Agent version;
- renderização React/browser do estado integrado, freshness visual, acessibilidade ou amostra humana;
- SignalR, notificação, comando, executor, ação externa, deploy, produção, LLM ou `OBSERVER`;
- advisories atuais de NuGet/npm ou CI remota.

## Limitações e condições residuais

- Evidência sintética não prova saúde, autenticação de provider, suporte ou homologação de provider/database.
- Idempotência de efeito não significa transporte exactly-once; a entrega permanece at-least-once com replay seguro no caso exercitado.
- O replay/reorder cobre um lote curto e uma única stream Agent; não prova reordenação ilimitada ou concorrência de frota.
- A recusa foi observada para uma requisição autenticada em voo cuja persistência começou somente depois do commit da revogação. A janela interna posterior à consulta `Active` e anterior ao commit do store não foi disputada e exigiria um contrato futuro de fencing/row locking para alegar linearização mais forte.
- O outbox local marca uma recusa terminal, mas ainda não persiste seu motivo; uma recusa cria um gap se a mesma stream fosse reativada posteriormente. Nenhuma reativação ocorreu, e não existe protocolo de skip/tombstone autorizado neste incremento.
- A projeção depende da amostra raw para provar evidência e latência. Como retenção raw está desabilitada, isso é seguro agora; uma política futura de poda exigirá contrato próprio para não produzir `503`.
- A ingestão admite pequeno skew futuro, enquanto o snapshot TV recusa `ObservedAt > ReceivedAt`; esse caso falha fechado e não foi normalizado nem apresentado como dado válido.
- O relógio controlado e o `304` provam preservação factual, não a apresentação visual de um rótulo stale no browser.
- A CA, certificados, PKCS#12 e named pipes são infraestrutura exclusivamente de teste, não arquitetura operacional de identidade. O PKCS#12 não foi gravado em arquivo do projeto, argumentos, ambiente ou SQLite e seus buffers foram zerados; o child carrega a chave com `UserKeySet | Exportable` para Schannel, portanto um possível contêiner temporário do utilizador e sua ausência pós-dispose não foram inspecionados, e não se alega chave estritamente memory-only.
- O Server central usado pelo E2E é SQLite em memória. Isso não prova PostgreSQL real, migrations produtivas ou comportamento distribuído.

## Classificação dos gates

- Quality Gate automático deste incremento restrito: **APROVADO** no escopo local e offline documentado acima.
- Human Gate deste incremento: **ACEITO COM AS LIMITAÇÕES REGISTRADAS** por Bruno em 2026-07-19.
- Incremento 2/SignalR, notificações, comandos, saída de `STATE-06`, promoção `none → OBSERVER`, produção e release: **NÃO AVALIADOS E NÃO AUTORIZADOS**.

## Decisão humana registrada

Depois de receber orientação explícita para revisar principalmente o resultado em linguagem simples, a sequência E2E observada, as limitações e condições residuais e a classificação dos gates, Bruno declarou em 2026-07-19:

> Incremento STATE-06 Authoritative Observation Pipeline E2E Sandbox, commit 3449918: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo novo incremento, runtime operacional, promoção nem transição de estado.

Essa decisão encerra somente o Human Gate próprio deste incremento. O `STATE-06 INTEGRATION` permanece inalterado; Incremento 2, runtime operacional, promoção e transição continuam não autorizados.
