# Proposta STATE-06 — Remediação test-only das amostras humanas bloqueadas

> **Proposta histórica executada e superada.** A implementação está no
> [relatório de remediação](STATE-06-Final-Human-Samples-Test-Only-Evidence-Remediation-Report.md);
> a sequência posterior está nos relatórios da
> [segunda remediação](STATE-06-Final-Human-Samples-Second-Test-Only-Remediation-Report.md),
> da
> [repetição final](STATE-06-Final-Human-Samples-Second-Post-Remediation-Repetition-Report.md)
> e do
> [Human Gate final](STATE-06-Final-Human-Gate-Report.md). O texto abaixo
> preserva a autoridade e o diagnóstico existentes quando a proposta foi
> escrita.

## Status e autoridade

- Data: 2026-07-20.
- Estado mantido: `STATE-06 INTEGRATION`.
- Baseline documental examinada: commit `b946c1f5d094d26292b3d70b364f2a6c1eb900a4`.
- Baseline automática incorporada: commit `84217c64312a024ec4f286adfe4872184a21849c`.
- Relatório automático aceito incorporado: commit `2c1e05fd8ad4dec2174682fee66aafbd92efc6ee`.
- Campanha humana corrente: `BLOQUEADA`, com `S06-HG-002` a `S06-HG-005` aprovadas e `S06-HG-001`/`S06-HG-006` bloqueadas.
- Autoridade desta atividade: exclusivamente elaborar esta proposta documental.
- Implementação, build, testes de produto, runtime, browser, acesso externo, repetição humana, Human Gate final, promoção e transição: não autorizados e não executados.

Esta proposta não corrige as duas lacunas e não reclassifica nenhuma amostra. Ela delimita um possível incremento futuro exclusivamente test-only. Qualquer implementação, validação automática e repetição humana dependerá de autorizações posteriores, separadas e explícitas.

## Resultado em linguagem simples

Duas demonstrações finais não puderam ser concluídas:

1. o laboratório já prova automaticamente que o Agent perde a conexão, guarda uma observação e a envia uma única vez depois da recuperação, mas tudo isso termina antes de a tela ser aberta;
2. o Dashboard contém um item `unknown`, porém ele já tem mais de cinco minutos e a tela corretamente o apresenta como `stale` (`Desatualizado`). Assim, Bruno não consegue ver `unknown` e `stale` como conceitos diferentes.

A remediação proposta não altera o DB-Notifier normal. Ela acrescentaria somente ao laboratório de testes:

- uma superfície auxiliar, claramente marcada como evidência test-only, que mostre o transporte do Agent em etapas reais e não confunda essa perda com o navegador offline;
- uma fonte de snapshot test-only contendo um exemplo `unknown` corrente e outro exemplo `stale`, ambos sintéticos, com suporte apenas planejado e sem aparência de dados operacionais.

Para uma pessoa não técnica: será como adicionar duas placas de identificação ao laboratório. Uma dirá exatamente quando o Agent perdeu e recuperou sua ligação local. A outra permitirá comparar, na mesma tela, “não sabemos o estado” com “o dado ficou antigo”. Essas placas não existirão no produto normal.

## Baseline factual da inspeção direta

### `S06-HG-001`

A inspeção de `tests/DBNotifier.IntegrationTests/AgentFleetApiEndToEndTests.ConsolidatedHarness.cs` confirmou:

1. `InitialiseAsync` executa enrollment, assignments e heartbeat;
2. em seguida, executa o child host contra um endpoint HTTPS loopback indisponível e comprova uma observação pendente;
3. depois executa o mesmo child host contra a API sandbox disponível e comprova replay único;
4. somente depois define `initialReplayObserved=true`, estabelece o snapshot/notificação inicial, muda o estágio para `ready` e permite que o runner abra o navegador;
5. o endpoint de evidência atual expõe o resultado agregado `InitialReplayObserved`, mas não mantém barriers humanos entre perda, pendência e recuperação;
6. o auditor atual usa `Network.emulateNetworkConditions` para testar offline do browser. Isso prova a ligação do navegador e não pode substituir a ligação interna do Agent.

Logo, o comportamento automático existe, mas a sequência humana não é elegível na ordem atual.

### `S06-HG-006`

A inspeção confirmou:

1. `src/DBNotifier.Dashboard.Web/src/presentation.ts` cria o item demonstrativo `demo-004` com status `unknown` e idade de `540.000 ms`;
2. o limiar canônico de stale na mesma apresentação é de cinco minutos;
3. `StatusBadge` em `src/DBNotifier.Dashboard.Web/src/App.tsx` dá precedência visual correta a `stale` antes do status de saúde;
4. por isso, alterar a precedência da UI seria factualmente errado e está fora da remediação;
5. o contrato Dashboard TV já aceita o status canônico `unknown`;
6. o child host sintético sob `tests/DBNotifier.AgentFleet.SandboxHost` já aceita `HealthStatus.Unknown`;
7. a projeção sandbox existente mapeia `HealthStatus.Unknown` para `unknown`, mas aplica um único rótulo de suporte sintético;
8. a composição de integração sob `tests/` já pode substituir `IDashboardTvSnapshotSource` somente no host E2E, sem registrar essa fonte na composição normal.

Esses fatos indicam que uma comparação visual factual pode ser montada em `tests/` sem modificar a fixture normal ou a regra correta de stale. Essa conclusão é de desenho; só uma implementação futura poderá confirmá-la.

## Problemas a resolver

O incremento futuro deverá resolver somente estas duas lacunas de apresentação:

| ID | Lacuna | Resultado mínimo futuro |
|---|---|---|
| `HGR-001` | perda/replay termina antes da apresentação | Bruno vê etapas separadas de transporte Agent disponível, indisponível com pendência preservada e recuperado com replay único |
| `HGR-006` | `unknown` da fixture normal já aparece como stale | Bruno vê, na mesma composição sandbox, `unknown` corrente e `stale` como estados textuais distintos, além de suporte planejado e origem sintética |

Não fazem parte do problema: SignalR, notificação, comando não executável, Quality Gate automático já aprovado, UI normal, provider, monitoramento real ou comportamento operacional.

## Decisão da fronteira `tests/scripts` versus `src`

A fronteira inicial recomendada é exclusivamente `tests/` e `scripts/`:

- o comportamento de perda/replay já existe no harness; falta somente dividi-lo por barriers e evidência humana sanitizada;
- o Dashboard e seu contrato já sabem apresentar `unknown` e `stale` corretamente;
- uma fonte test-only pode fornecer exemplos temporais e rótulos sintéticos exatos pela API sandbox existente;
- nenhuma regra de produto precisa ser alterada para provar as duas diferenças.

Arquivos sob `src/` permanecem somente como referências read-only da semântica existente. Se a implementação demonstrar que um critério não pode ser atendido sem alterar `src/`, a remediação deverá parar como `BLOQUEADA`; não poderá converter automaticamente esta proposta em autoridade para mudar produto ou fixture normal.

## Arquitetura proposta

### 1. Estado test-only correlacionado e sanitizado

O harness correlacionado deverá manter um estado imutável por etapa, limitado a:

- `agent-transport-ready`;
- `agent-transport-unavailable`;
- `agent-observation-pending`;
- `agent-transport-recovered`;
- `agent-replay-accepted-once`;
- `visual-truth-ready`;
- `completed` ou uma falha terminal tipada.

Cada avanço exigirá a evidência durável correspondente antes de publicar a etapa. A superfície humana receberá somente estados, contagens, horários UTC e identificadores de correlação abreviados. Token, chave, certificado, PKCS#12, senha, header, caminho local, payload e texto de exceção permanecerão proibidos.

### 2. Remediação proposta para `S06-HG-001`

O futuro harness deverá separar a inicialização em barriers determinísticos:

1. preparar identidade, assignment, heartbeat e primeiro snapshot sintéticos;
2. publicar readiness para a superfície auxiliar test-only enquanto o browser continua online;
3. executar uma observação contra endpoint loopback deliberadamente indisponível;
4. comprovar que exatamente uma observação ficou pendente localmente e que nenhuma nova amostra chegou ao Server;
5. manter essa etapa congelada até a coleta humana ou avanço explícito do runner;
6. restabelecer somente o transporte Agent → API sandbox;
7. reproduzir a mesma observação e comprovar outbox vazia, uma ingestão server-side e nenhum efeito duplicado;
8. atualizar o Dashboard pela API autoritativa e concluir a etapa de recuperação.

A apresentação deverá combinar:

- Dashboard normal do sandbox mostrando o último snapshot válido, horário e freshness;
- uma faixa ou página auxiliar test-only, visualmente separada e rotulada `Evidência do harness — não é estado operacional`, mostrando exclusivamente a ligação do Agent e as contagens sanitizadas.

O browser não será colocado offline nessa amostra. A evidência deverá provar que requests browser → API continuam disponíveis enquanto Agent → API está indisponível. Qualquer teste de offline do browser continuará pertencendo ao E2E automático existente e não será apresentado como perda do Agent.

### 3. Remediação proposta para `S06-HG-006`

Uma fonte `IDashboardTvSnapshotSource` implementada dentro do projeto de testes existente deverá fornecer, somente sob o marker exato do harness:

- um item sintético `unknown` com evidência corrente;
- um item sintético não saudável com evidência anterior ao limiar de cinco minutos, portanto apresentado como `stale`;
- rótulo textual de suporte `Planejado — não implementado` ou equivalente localizado já existente;
- origem `sandbox local sintético`, timestamps e indicação de ausência de dados externos;
- provider IDs de fixture, sem sugerir homologação de engine real.

Os itens deverão atravessar a mesma API versionada e o mesmo adapter Dashboard TV usados pelo sandbox. A UI normal continuará aplicando sua regra vigente: freshness stale tem precedência sobre o status interno. A remediação não poderá alterar `StatusBadge`, o limiar de cinco minutos ou a fixture demonstrativa `demo-004`.

A amostra humana deverá mostrar `unknown` e `stale` simultaneamente ou em duas etapas congeladas e inequívocas da mesma fonte sandbox. Se o item `unknown` envelhecer durante a sessão e cruzar o limiar, a amostra ficará inválida e será reiniciada integralmente dentro do budget, não reinterpretada como passe.

### 4. Isolamento da composição normal

Os novos endpoints, fonte e superfície auxiliar deverão existir somente quando todos os guardas forem satisfeitos:

- executável do host de teste existente;
- marker exato de ativação;
- HTTPS loopback;
- identidade exclusivamente de teste;
- run ID imprevisível e header de correlação exato;
- diretórios/stores efêmeros pertencentes à sessão;
- budget e cancelamento ativos.

Testes de arquitetura deverão provar que o `Program` normal, WPF normal e Dashboard normal não registram, referenciam ou descobrem esses controles. A superfície auxiliar não poderá conter botão administrativo, endpoint genérico, origem externa ou caminho de composição operacional.

### 5. Fencing, budget e cleanup

- somente uma transição do harness poderá executar por vez;
- cada barrier terá deadline e cancelamento;
- repetição de control request será idempotente ou recusada com disposition tipada;
- uma etapa anterior não poderá sobrescrever etapa mais nova;
- o runner terá budget total limitado e no máximo uma repetição integral por amostra;
- host, Chrome dedicado, perfil, stores, certificados, pipes, logs e roots temporários deverão ser encerrados/removidos no `finally`;
- falha de cleanup reprovará o incremento e impedirá a repetição humana.

## Escopo futuro proposto

Uma eventual implementação ficará limitada a:

- `tests/DBNotifier.IntegrationTests/AgentFleetApiEndToEndTests.ConsolidatedHarness.cs` e arquivos parciais test-only do mesmo projeto;
- `tests/DBNotifier.State06.ConsolidatedSandboxHost/` sem novo projeto;
- testes de integração/arquitetura existentes sob `tests/`;
- `scripts/run-state06-consolidated-e2e.ps1` e um runner humano local específico, se necessário;
- auditoria browser local sob `scripts/` apenas para comprovar a superfície e a semântica;
- relatório factual e registros documentais correspondentes.

Não são necessários nem permitidos: mudança na solução, novo projeto, package, lockfile, migration, restore, download ou arquivo sob `src/`.

## Testes futuros obrigatórios

| Área | Evidência mínima |
|---|---|
| ordem Agent | readiness humano ocorre antes da perda/replay, com barriers e sem `Sleep` como sincronização |
| distinção de transporte | browser → API permanece disponível enquanto Agent → API está indisponível |
| preservação | uma observação pendente local, zero ingestão nova durante a perda e último snapshot válido preservado |
| replay | a mesma observação é ingerida uma vez, outbox termina vazia e não há duplicidade visual ou durável |
| snapshot visual | um item `unknown` corrente e um item `stale` são aceitos pelo contrato e apresentados com texto distinto |
| suporte/origem | suporte planejado, sandbox local e ausência de dados externos permanecem explícitos |
| temporalidade | timestamps válidos; item corrente não cruza silenciosamente o limiar durante a amostra |
| acessibilidade | estados não dependem apenas de cor, possuem nome acessível e ordem de foco previsível |
| segurança | endpoints recusam origem não-loopback, identidade/run ID ausentes e repetição fora de etapa |
| sanitização | nenhum secret, certificado, path, header, payload ou exception text entra na evidência |
| isolamento | zero referência/registro na composição normal e nenhuma alteração sob `src/` |
| regressão | harness correlacionado existente, SignalR hint, reconciliação e comando não executável continuam passando |
| cleanup | zero processo, listener, browser/profile, store, pipe, certificado, log ou root residual |

A validação futura será o Quality Gate próprio da remediação. Ela não repetirá nem reclassificará as amostras humanas e não abrirá o Human Gate final.

## Critérios de aceite da remediação futura

O incremento futuro somente poderá ser aceito se:

1. `S06-HG-001` possuir três momentos humanos inequívocos: disponível, indisponível com pendência preservada e recuperado com replay único;
2. offline do browser não for usado nem rotulado como offline do Agent;
3. a superfície auxiliar indicar claramente que é evidência test-only, não UI operacional;
4. o último snapshot válido permanecer visível durante a perda;
5. contagens duráveis comprovarem uma pendência, uma ingestão e zero duplicidade;
6. `S06-HG-006` apresentar `unknown` corrente e `stale` como textos/semânticas diferentes;
7. suporte planejado e origem sintética/local forem inequívocos e não dependerem de cor;
8. a API e o adapter Dashboard TV existentes continuarem sendo a fronteira da apresentação;
9. nenhuma alteração ocorrer sob `src/`, solução, projetos, packages, lockfiles ou migrations;
10. composição normal, WPF normal e Dashboard normal permanecerem inalterados e desabilitados para esses controles;
11. testes de segurança, arquitetura, integração, browser e regressão aplicáveis passarem offline;
12. todos os runtimes e temporários próprios forem encerrados/removidos;
13. o relatório distinguir observado, inferido, não testado e bloqueado;
14. nenhuma amostra humana, Human Gate, promoção ou transição for inferida a partir dos testes.

Mesmo aprovado, esse incremento apenas tornará as duas amostras elegíveis. Bruno ainda precisará autorizar e repetir `S06-HG-001` e `S06-HG-006` numa sessão humana posterior.

## Ordem das atividades futuras

1. autorização separada do incremento test-only;
2. preflight e inspeção da baseline;
3. implementação restrita e testes automáticos;
4. relatório factual e aceitação humana própria da remediação;
5. autorização separada para repetir somente `S06-HG-001` e `S06-HG-006`;
6. execução visível e decisão de Bruno para cada amostra;
7. relatório atualizado da campanha humana;
8. somente se as seis amostras estiverem concluídas, proposta separada do Human Gate final.

Nenhuma etapa concede automaticamente a seguinte.

## Classificação futura

- `APROVADO`: os critérios test-only foram atendidos e as duas amostras tornaram-se elegíveis;
- `REPROVADO`: a remediação viola verdade factual, determinismo, isolamento, segurança ou cleanup;
- `BLOQUEADO`: atender ao critério exige `src/`, nova dependência, acesso ou autoridade fora do escopo;
- `NÃO APLICÁVEL`: somente para verificação realmente externa, com justificativa explícita.

## Fora de escopo absoluto

- alteração em qualquer arquivo sob `src/`;
- mudança da fixture normal `demo-004`, do limiar de stale ou da precedência `stale > status`;
- solução, projeto, package, lockfile, migration, schema, restore, download ou dependência;
- acesso externo, CDN, registry, recurso ou credencial operacional;
- provider, banco, monitoramento, Agent, PKI, IdP, vault ou serviço operacional;
- composição normal, runtime permanente, API/UI administrativa ou deploy;
- SignalR novo, notificação Windows, canal externo ou repetição de `S06-HG-002` a `S06-HG-005`;
- comando administrativo, `CommandAttempt`, executor, shell de ação, post-probe ou efeito em serviço/infraestrutura;
- LLM, recomendação, planejamento automatizado ou promoção `none → OBSERVER`;
- nova Campanha Consolidada completa, repetição humana, Human Gate final, promoção para `STATE-07` ou transição.

## Riscos e limitações residuais

- Uma superfície test-only prova a sequência do laboratório, não que o produto operacional possua uma tela de conectividade do Agent.
- Uma fonte test-only prova semântica visual do Dashboard, não suporte de provider ou monitoramento real.
- A separação de `unknown` e `stale` depende de timestamps; atraso humano pode invalidar a amostra corrente e exigir repetição integral.
- Instrumentação pode alterar timing. Por isso, barriers e estado durável deverão governar a sequência, não sleeps ou animações.
- Server/Agent SQLite efêmeros não representam PostgreSQL, escala ou concorrência operacional.
- Um Chrome e um Windows não constituem homologação ampla.
- Alterar `src/` poderia simplificar a fixture, mas expandiria desnecessariamente o impacto e exigiria autorização própria.
- A conclusão técnica da remediação não reabre automaticamente a campanha humana nem decide o Human Gate.

## Condições de parada

O incremento futuro deverá parar como `BLOQUEADO` se:

- qualquer critério exigir mudança sob `src/`, solução, projeto, package, lockfile ou migration;
- a perda do Agent não puder ser distinguida sem simular offline do navegador;
- o snapshot test-only precisar falsificar origem, suporte, status ou timestamp;
- uma evidência puder expor secret, certificado, identidade privada, path ou payload;
- surgir acesso externo, recurso operacional, comando, executor, canal ou efeito real;
- a sequência depender de retry até passar, sleep não determinístico ou relaxamento de asserção;
- o budget expirar ou qualquer processo/temporário não puder ser atribuído e removido;
- houver mudança preexistente conflitante no workspace.

## Entregáveis futuros

- state machine e barriers exclusivamente test-only para perda/recovery do Agent;
- superfície auxiliar sanitizada e claramente não operacional;
- fonte test-only de verdade visual `unknown`/`stale`/suporte planejado;
- testes de segurança, arquitetura, integração, browser, acessibilidade e cleanup;
- relatório factual próprio e atualização de estado/histórico;
- commit local focado, sem código de produto ou transição.

## Verificação desta proposta

O incremento exclusivamente documental passou:

- gate de documentação para `280` arquivos de fonte passíveis de comentários;
- gate de links Markdown para `467` links locais em `106` arquivos;
- secret scan da worktree não ignorada e do histórico Git disponível.

Build, testes de produto, harness, runtime e browser não foram executados porque não pertencem à autoridade documental desta atividade.

## Decisão futura de Bruno

Se Bruno concordar com esta proposta e desejar autorizar somente o incremento test-only, o texto sugerido é:

> AUTORIZO o incremento restrito de remediação do STATE-06 — Final Human Samples Test-only Evidence Remediation, limitado ao harness correlacionado e runners sob `tests/` e `scripts/`, com barriers determinísticos para apresentar separadamente transporte Agent disponível, indisponível com uma observação preservada e recuperado com replay único, superfície auxiliar sanitizada e inequivocamente test-only, fonte de snapshot exclusivamente de teste com `unknown` corrente, `stale`, suporte planejado e origem sintética pela API/Dashboard TV sandbox existentes, fencing, budgets, cancelamento, testes automáticos e documentação factual. Autorizo runtimes temporários exclusivamente locais e Chrome dedicado com perfil efêmero para o Quality Gate próprio da remediação, que deverão ser encerrados e removidos ao final. Se qualquer critério exigir alteração sob `src/`, solução, projetos, packages, lockfiles ou migrations, o incremento deverá parar como BLOQUEADO e solicitar nova autorização. Permanecem proibidos acesso externo, downloads, recursos ou credenciais operacionais, provider/banco/Agent operacional, composição normal, notificação Windows, comandos administrativos, CommandAttempt, executor, shell de ação, serviço ou infraestrutura afetados, canal externo, LLM, deploy, repetição das amostras humanas, Human Gate final, promoção e transição de estado.

Essa autorização futura liberaria somente implementação test-only, validação automática e relatório próprio. A repetição humana de `S06-HG-001` e `S06-HG-006` continuaria exigindo outra autorização.

## Próximo passo para Bruno

1. Abra esta proposta e leia principalmente `Resultado em linguagem simples`, `Baseline factual da inspeção direta`, `Decisão da fronteira tests/scripts versus src`, `Arquitetura proposta`, `Critérios de aceite da remediação futura`, `Fora de escopo absoluto` e `Riscos e limitações residuais`.
2. Se discordar, informe somente os pontos que deseja alterar.
3. Se concordar e quiser iniciar a implementação test-only, copie exatamente o texto da seção `Decisão futura de Bruno`.
4. Não autorize junto a repetição das amostras, o Human Gate final, promoção ou transição.

Enquanto esta proposta estiver em revisão, nenhuma ação técnica adicional é necessária ou autorizada.
