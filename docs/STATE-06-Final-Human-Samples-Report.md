# Relatório STATE-06 — Amostras humanas finais

## Status e autoridade

- Data: 2026-07-20.
- Baseline executada: commit `279bc7007b33aa3ce555d406b3a1b4840f8832ff` na branch `main`.
- Baseline automática incorporada: commit `84217c64312a024ec4f286adfe4872184a21849c`.
- Relatório automático aceito incorporado: commit `2c1e05fd8ad4dec2174682fee66aafbd92efc6ee`.
- Estado mantido: `STATE-06 INTEGRATION`.
- Classificação da campanha de amostras: `BLOQUEADA`.
- Quality Gate consolidado automático: permanece `APROVADO` com as limitações registradas.
- Human Gate final: `PENDENTE` e não aberto.
- Runtime operacional, promoção e transição: não autorizados e não executados.

Bruno autorizou exclusivamente as amostras `S06-HG-001` a `S06-HG-006` com artefatos já implementados, runtimes temporários locais, Chrome dedicado e WPF sandbox opt-in. A autorização proibiu correção, alteração técnica, acesso externo, recurso operacional, comando administrativo, executor, Human Gate final, promoção e transição.

## Resultado em linguagem simples

Quatro das seis amostras foram aprovadas por Bruno. Ele viu o Dashboard TV atualizar depois de um hint e reconciliar novamente, viu uma notificação Windows claramente identificada como sandbox sintético, confirmou que ela não apareceu uma segunda vez e aceitou a evidência de que o transporte de comando gravou somente registros sintéticos sem executar ação alguma.

Duas amostras não puderam ser apresentadas de maneira fiel com os artefatos atuais. O replay offline do Agent termina antes de a interface humana ser aberta, e o Dashboard não mostra essa conexão interna separadamente do offline do navegador. Na última amostra, a origem sandbox, o estado stale e o suporte planejado são visíveis, mas o único item com status interno `unknown` já está stale e, corretamente, aparece como `Desatualizado`.

Esses dois bloqueios são lacunas de apresentação da evidência humana, não falhas novas no Quality Gate automático e não prova de defeito operacional. Contudo, as regras exigem que todas as amostras obrigatórias sejam repetidas; por isso o Human Gate final não pode ser aberto ainda.

## Baseline e preflight

Antes de cada nova sessão técnica foi comprovado:

- branch `main` e commit `279bc70`;
- worktree limpa;
- `84217c6` e `2c1e05f` na ancestralidade;
- zero mudança técnica depois de `84217c6`;
- zero processo, listener, Chrome dedicado, WPF ou root temporário pertencente ao DB-Notifier;
- uso somente dos binários e do Dashboard já construídos localmente;
- nenhum build, restore, download ou acesso externo autorizado.

A primeira versão do filtro de processos produziu exceções internas ao encontrar processos sem linha de comando. Suas contagens foram descartadas, o filtro foi corrigido e o preflight válido retornou zero processo e zero listener antes de qualquer runtime.

## Resultado por amostra

| Amostra | Resultado | Decisor/evidência principal |
|---|---|---|
| `S06-HG-001` — perda/recuperação do Agent | `BLOQUEADA` | inspeção direta: o replay offline ocorre durante a inicialização anterior à apresentação; a UI não distingue conexão do Agent de offline do browser |
| `S06-HG-002` — leitura/hint/30 segundos | `APROVADA` | sequência visível e decisão explícita de Bruno `S06-HG-002: APROVADA` |
| `S06-HG-003` — uma notificação local | `APROVADA` | notificação Windows observada e decisão explícita de Bruno |
| `S06-HG-004` — duplicata suprimida | `APROVADA` | janela adicional sem segunda notificação, ledger com uma tentativa e decisão explícita de Bruno |
| `S06-HG-005` — comando sem execução | `APROVADA` | dois journals, zero `CommandAttempt`, recusa terminal e decisão explícita de Bruno |
| `S06-HG-006` — origem/freshness/suporte | `BLOQUEADA` | origem sandbox, stale e suporte planejado visíveis; exemplo visual `unknown` ausente nos artefatos atuais |

`BLOQUEADA` não significa `APROVADA COM RESSALVA`. Nenhum resultado automático ou inferência substituiu a observação humana ausente.

## Sequência humana e técnica observada

### `S06-HG-001` — bloqueio de elegibilidade

A inicialização do harness executa enrollment, heartbeat, assignment e replay offline antes de publicar o marcador de prontidão que permite abrir o navegador. A evidência sanitizada confirma o replay, mas a interface disponível mostra somente o snapshot resultante. Simular offline do Chrome prova a conexão do navegador, não a conexão do Agent.

Como a proposta proibia aprovação por aproximação e qualquer mudança técnica, a amostra foi encerrada como `BLOQUEADA` sem abrir runtime específico para ela.

### `S06-HG-002` — Dashboard TV

Uma primeira apresentação manual confirmou o modo TV, `Sandbox Assignment`, a origem local e o estado stale. Ela não manteve a conexão SignalR porque o browser manual não possuía o header adicional usado pelo auditor dedicado; a evidência mostrou polling válido, cinco snapshots e zero conexão SignalR ativa. A tentativa foi declarada insuficiente e totalmente limpa.

A repetição válida reutilizou o auditor aceito em um Chrome dedicado visível. Bruno pôde observar a sequência enquanto o auditor:

1. entrou no modo TV e fez a leitura imediata;
2. commitou uma segunda observação;
3. recebeu o hint SignalR e fez uma releitura autoritativa;
4. aguardou a reconciliação independente de 30 segundos;
5. simulou offline/recovery apenas no browser;
6. finalizou o transporte não executável e a revogação;
7. encerrou host, Chrome e perfil.

Resumo sanitizado:

```json
{"result":"passed","browser":"Chrome/150.0.7871.125","observationSamples":2,"notificationDeliveries":1,"commandJournalEntries":2,"commandAttempts":0,"maximumSnapshotConcurrency":1,"observedHttpRequests":72,"observedWebSockets":12,"observedExternalHttpRequests":0,"operationalData":false}
```

Bruno decidiu exatamente `S06-HG-002: APROVADA`.

### `S06-HG-003` e `S06-HG-004` — notificação e deduplicação

O WPF foi iniciado somente com os guards `--reconciled-notification-sandbox` e `--notifications-opt-in`, endpoint HTTPS loopback, identidade de teste, pin efêmero e ledger sob a pasta temporária da sessão. A leitura inicial ficou silenciosa.

Depois da transição sintética `Degradado → Indisponível`, o ledger registrou uma entrada, uma tentativa e `delivery.local_platform_accepted`. Um segundo ciclo avançou a revisão do ledger sem criar nova entrada ou tentativa. A plataforma aceitar uma publicação não foi usada como prova visual: Bruno apresentou a notificação que viu, com `DB Notifier`, `sandbox sintético`, transição, horário/freshness e aviso de ausência de dados externos.

A pedido de Bruno, toda a amostra foi repetida uma vez. A repetição produziu novamente uma entrada e uma tentativa, seguida de um ciclo completo sem nova publicação. Bruno decidiu exatamente:

```text
S06-HG-003: APROVADA
S06-HG-004: APROVADA
```

A imagem apresentada por Bruno permaneceu somente na conversa; não foi copiada para o repositório.

### `S06-HG-005` — transporte deliberadamente não executável

Nas duas sessões de notificação, a finalização correlacionada produziu o mesmo resumo:

- `finalised=true`;
- dois journals sintéticos;
- zero `CommandAttempt`;
- comando, observação, heartbeat e assignments negados depois da revogação;
- revogação central commitada;
- nenhum diagnóstico terminal de falha.

Nenhum shell, processo alvo, serviço, banco, executor ou post-probe foi iniciado. Bruno decidiu exatamente `S06-HG-005: APROVADA`.

### `S06-HG-006` — bloqueio de verdade visual completa

A primeira imagem do Dashboard confirmou:

- `Modo TV · sandbox autoritativo local`;
- `Snapshot do sandbox local`;
- `Sandbox Assignment`;
- estado `Desatualizado`;
- texto de que nenhum dado externo é usado;
- texto de que providers planejados não representam suporte público ou homologação.

Uma sessão final abriu a composição demonstrativa existente para procurar o exemplo `unknown`. A inspeção direta confirmou que `Catálogo` possui status interno `unknown`, suporte `Planejado · não implementado` e idade de `540.000 ms`. Como o limiar de freshness é cinco minutos, a UI dá precedência correta a stale e apresenta `Desatualizado`, não `Desconhecido`.

Não existe outro exemplo elegível que apresente simultaneamente `unknown` sem alterar uma fixture. A sessão foi encerrada como `BLOQUEADA`, conforme o critério aprovado, e nenhuma fixture foi modificada.

## Incidentes de ferramenta e tratamento

- O primeiro filtro de preflight continha uma variável de pipeline incorreta. O resultado foi descartado e a repetição fail-closed passou.
- Uma primeira inicialização de host não concluiu o hand-off ao navegador. O host e os dois logs temporários foram encerrados/removidos antes de nova tentativa.
- Uma rotina de cleanup identificou o próprio PowerShell porque sua linha de comando continha o profile temporário. Ela terminou a si própria; a inspeção seguinte localizou somente o host próprio, que foi encerrado, e comprovou o cleanup. Nenhum processo alheio foi afetado.
- O primeiro Chrome manual não manteve SignalR autenticado. Seu resultado foi rejeitado para `S06-HG-002`; a repetição com o auditor existente passou.
- O Chrome dedicado emitiu mensagens internas sobre um endpoint GCM descontinuado. O auditor observou zero origem HTTP externa, e as regras de resolução bloquearam destinos não-loopback; a mensagem não foi tratada como prova de acesso externo.

Esses incidentes não foram ocultados, não causaram mudança técnica e não foram convertidos em aprovação humana.

## Cleanup e isolamento

Depois de cada tentativa ou amostra:

- Chrome dedicado e todos os processos com o profile próprio foram encerrados;
- WPF sandbox e ícone da área de notificações foram encerrados;
- host consolidado, Agent/API temporários e subprocessos próprios foram encerrados;
- perfis, ledgers, SQLite, certificados, logs e roots temporários próprios foram removidos;
- zero listener pertencente à sessão permaneceu;
- nenhum navegador normal, IDE, banco, serviço ou processo alheio foi encerrado;
- worktree permaneceu limpa e o commit técnico não mudou.

O Windows pode manter a notificação já apresentada na Central de Notificações. Nenhuma notificação alheia foi lida ou removida, e nenhuma alegação de limpeza global da Central é feita.

## Verificação do registro documental

Depois do encerramento integral dos runtimes, o registro documental passou nas verificações aplicáveis:

- gate de documentação: `280` arquivos de fonte passíveis de comentários;
- gate de links Markdown: `462` links locais em `105` arquivos;
- secret scan: worktree não ignorada e histórico Git disponível sem achados.

Build, testes de produto e harnesses não foram repetidos durante o registro: a autoridade permitia somente documentar as decisões, e a evidência técnica desta campanha já havia sido produzida nas sessões descritas acima.

## Observado, inferido e não testado

### Observado

- decisões explícitas de Bruno para `S06-HG-002` a `S06-HG-005`;
- imagem do Dashboard e imagem da notificação apresentadas por Bruno;
- sequência visível do auditor, contagens sanitizadas e cleanup;
- ledger com uma entrada/uma tentativa e segundo ciclo sem nova tentativa, repetido duas vezes;
- dois journals e zero `CommandAttempt`, repetidos duas vezes;
- ausência de apresentação elegível para `S06-HG-001` e `S06-HG-006`.

### Inferido por inspeção direta

- causa test-only dos dois bloqueios: ordem de inicialização do harness para `S06-HG-001` e precedência stale sobre o status interno `unknown` para `S06-HG-006`;
- ausência de mudança técnica durante toda a campanha.

### Não testado

- Agent, provider, banco, identidade, PKI, IdP, credencial ou infraestrutura operacional;
- PostgreSQL real, rede externa, deploy, produção, escala ou outros sistemas operativos;
- comando administrativo, executor, post-probe ou efeito em serviço;
- comportamento geral de Focus Assist/Notification Centre além da mensagem vista por Bruno;
- Human Gate final, promoção ou transição.

## Limitações e condições residuais

- A prova automática de replay offline continua válida, mas não substitui a experiência humana ausente de `S06-HG-001`.
- Offline do navegador não deve ser apresentado como offline do Agent.
- O estado interno `unknown` da fixture não é visualmente exposto enquanto a mesma evidência estiver stale.
- Uma remediação futura deverá decidir se a apresentação pertence somente a `tests/`/`scripts/` ou se exige alteração factual da fixture em `src/`; esta campanha não concede nenhuma dessas autoridades.
- A apresentação Windows foi observada nesta máquina e sessão; não constitui homologação geral.
- SignalR permanece hint; API e polling permanecem autoritativos.
- Transporte não executável não prova controle administrativo.
- Quality Gate automático aprovado não elimina a obrigação das duas amostras bloqueadas.

## Classificação dos gates

| Gate | Classificação |
|---|---|
| Quality Gate consolidado automático | `APROVADO` com limitações; inalterado |
| `S06-HG-001` | `BLOQUEADA` |
| `S06-HG-002` | `APROVADA` por Bruno |
| `S06-HG-003` | `APROVADA` por Bruno |
| `S06-HG-004` | `APROVADA` por Bruno |
| `S06-HG-005` | `APROVADA` por Bruno |
| `S06-HG-006` | `BLOQUEADA` |
| campanha das amostras humanas | `BLOQUEADA` |
| Human Gate final do `STATE-06` | `PENDENTE` / não aberto |
| runtime operacional, promoção e transição | `NÃO AUTORIZADOS` |

## Próxima atividade

Nenhuma implementação está autorizada. Para continuar, Bruno deverá autorizar separadamente somente uma proposta documental de remediação de `S06-HG-001` e `S06-HG-006`. Essa proposta deverá comparar uma apresentação test-only com qualquer mudança que alcance `src/`, preservar a verdade de Agent/offline/freshness e manter o Human Gate final fechado até a repetição das duas amostras.
