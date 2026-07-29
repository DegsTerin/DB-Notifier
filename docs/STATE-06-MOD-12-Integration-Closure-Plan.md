# Plano documental de fechamento da integração MOD-12 no STATE-06

> **PLANO DOCUMENTAL PREPARADO E PENDENTE DE REVISÃO. NÃO AUTORIZA CÓDIGO,
> BUILD, TESTES, RUNTIME, DEPENDÊNCIAS, D9, O5, PF-OBS, ATIVAÇÃO MOD-12,
> JOSE OU TRANSIÇÃO DE LIFECYCLE.**

## Status e autoridade

- Data: 2026-07-29.
- Baseline administrativa de elaboração:
  `2a59548788bb6cbe6f88bbf531bd953b718165d8`, branch `main`, worktree limpa
  no preflight.
- Última árvore executável inventariada:
  `9512dc1de15619eadd9d2e8e6b5476bb77a13abd`.
- Posição do workspace: `STATE-06 INTEGRATION`.
- Estado do MOD-12: `ActivationState=None`; nenhum modo está ativo.
- Origem:
  [`PC-M12-01`–`04`](STATE-06-To-STATE-07-Transition-Revalidation-Proposal.md#fechamento-de-integração-do-mod-12).
- Natureza: plano documental não autorizante.
- Disposição humana deste plano: `PENDENTE DE REVISÃO`.
- Execução técnica, Quality Gate corrente, amostras humanas e classificação de
  fechamento: `NÃO AUTORIZADOS` e `NÃO EXECUTADOS`.

Bruno autorizou exatamente:

> AUTORIZO exclusivamente um lote documental para elaborar o plano de
> fechamento MOD-12 STATE-06 conforme PC-M12-01–04. Não autorizo código,
> build, testes, runtime, downloads, acesso externo, dependências, D9, O5,
> PF-OBS, ativação MOD-12, JOSE-1 nem transição de lifecycle.

Esta autoridade permite somente elaborar este plano e reconciliar a memória
documental correspondente. Aceitar futuramente o plano significará aceitar sua
decomposição e ordem como direção documental; não autorizará qualquer lote
técnico, comando, gate, amostra humana, ativação ou transição.

## Objetivo e definição limitada de fechamento

O objetivo é transformar `PC-M12-01`–`04` numa sequência verificável de lotes
futuros que:

1. estabeleça um único caminho canônico product-owned
   Agent → Server → MOD-12 → projeção/API/UI;
2. exercite esse caminho exclusivamente por harness sintético autorizado;
3. preserve a composição normal dormente e fail-closed, com zero trabalho ou
   I/O pertencente ao MOD-12 em `ActivationState=None`;
4. revalide, na baseline técnica final, os requisitos históricos de O1–O4 e
   somente as parcelas inativas/sintéticas já existentes de O5-R2/O5-R3;
5. produza Quality Gate corrente, novas amostras humanas aplicáveis e uma
   decisão específica de fechamento.

O fechamento máximo permitido será **`MOD-12 STATE-06 INTEGRATION SCOPE
COMPLETE`**. Ele significará somente:

> Candidato Observer integrado e revalidado em sandbox sintético; composição
> normal mantida dormente e fail-closed, com `ActivationState=None`; pronto
> para homologação.

Esse resultado não significará “MOD-12 finalizado”, “AIOps operacional ou
completa”, “Observer ativo”, “pronto para produção”, provider homologado,
precisão operacional, O5 aprovado ou lifecycle promovido.

## Baseline factual e proveniência histórica

Nenhum gate foi reexecutado neste lote. Os resultados abaixo permanecem
factos históricos e oráculos de regressão, não classificações correntes de
`PC-M12`.

| Escopo | Resultado histórico | Commit de implementação revisto | Limite preservado |
|---|---|---|---|
| O1 | automático `APPROVED`; Human Gate `APPROVED` | `ab60f4375af8e466a72077ecfa771ce5a7d07477` | 68 vetores locais de trust/resource/corpus; implementação, store e witness locais sintéticos test-only; sem witness externo independente ou capacidade operacional |
| O2-A | automático `APPROVED`; Human Gate `APPROVED` | `eec5511b15208e7aadb569b9c8a82db0200fdc74` | cadeia sintética com boundaries product-owned existentes de dispatch/ingestão, mas pipeline e transport de integração test-owned; estado maioritariamente in-memory |
| O2-B | automático `APPROVED`; Human Gate `APPROVED` | `9b25d2dbf5bff6d9ef2f6a964806bd8f82a5e98a` | continuidade, replay, backpressure e retention em raiz temporária; limites são fixtures, não SLOs ou ceilings operacionais |
| O3-A | automático `APPROVED`; Human Gate `APPROVED` | `2832153565a3a8a1f5bb1748dfb9cda11cd1745c` | nove membros, três partições e três segmentos sintéticos; `ProductionRepresentative=false`; implementação especializada test-owned |
| O3-B | automático `APPROVED`; Human Gate `APPROVED` | `1736571fe56c5723e0514b8e86450c89b4f53ca4` | protocolo sintético de freeze/holdout; disponibilidade reteve um falso positivo e erro binário `1.00`; não prova precisão ou forecast operacional |
| O4/O4-UI1 | automático `APPROVED`; Human Gate `APPROVED WITH RESERVATIONS` | `77211bc29af36de9976a8a57c7c3faba99bc59c4` e `0e740671c2b92220ccd6b9de8899ff99dcc953a9` | projeção/API C# test-owned e Dashboard em composição especial; amostra mostrou `Stale` e forecast `Unknown`; acessibilidade/DPI físicos não comprovados |
| O5-R2 | automático `APPROVED`; Human Gate `APPROVED` somente no escopo inativo | `3fe55ed359916df8853dcec6d829a632a9b6f4dc` | composição normal registra somente control plane dormente e authority indisponível; sandbox não concede avaliação/publicação |
| O5-R3 | automático `APPROVED`; Human Gate `APPROVED` somente no escopo sintético | `a1e7ead768cd2f2c02e924cd615a7253bb1ec32c` | observabilidade e incidentes fechados em fixture; sem sink, alerta, owner nominal ou SLO operacional |

Os nove commits revistos acima são ancestrais da baseline de elaboração. Isso
preserva proveniência, mas não carry-forward automático. Em especial:

- O2-A recebeu evolução posterior para continuidade O2-B;
- o host consolidado e a composição adjacente mudaram depois dos gates;
- O4 recebeu mudança posterior em `b151f9f` para uma ramificação PF-OBS-1;
- a futura composição product-owned será, por definição, uma mudança material.

Consequentemente, fixtures, vetores e checklists podem ser reutilizados como
oráculos; as classificações históricas não podem ser reutilizadas como
resultado corrente.

## Lacuna atual que o plano deve fechar

O estado observado na baseline de elaboração é:

- `DBNotifier.Application.AIOps` possui contratos, adapters, analisadores e
  control plane provider-neutral product-owned;
- a composição normal do Server registra somente
  `UnavailableObserverActivationAuthority` e `DormantObserverControlPlane`;
- `ObserverActivationState` contém somente `None`;
- a orquestração especializada de O1, O2-A, O2-B, O3-A, O3-B e a
  projeção/API O4 permanece predominantemente em `tests/`;
- a UI O4 pertence a `src/`, mas somente entra numa composição de sandbox
  explicitamente guardada;
- não existe ainda um único caminho product-owned
  Agent → Server → MOD-12 → projeção/API/UI exercitado ponta a ponta.

Portanto, gates históricos de sandboxes isolados não satisfazem
`PC-M12-02`. O fechamento exige ownership canônico no produto sem ligar a
composição normal.

## Arquitetura-alvo do fechamento

```text
Fixture sintética e fault injectors                         [test-only]
        |
Agent outbox/dispatch product-owned
        |
Server ingestão, identidade, sequência e revogação product-owned
        |
Boundary + adapters + activation guard MOD-12 product-owned
        |
Trust/continuity/resource ports product-owned
        |
Análise, policy, corpus e avaliação provider-neutral product-owned
        |
Projeção e verificação factual product-owned
        |
API read-only + UI factual product-owned
        |
Harness sintético explicitamente autorizado                [test-only]
```

### Regra de ownership

O código test-only poderá possuir somente:

- fixtures e corpus sintéticos;
- chaves efémeras, clocks e fault injectors;
- adapters de filesystem, transport, store ou identidade estritamente
  sintéticos;
- launcher, process marker, auditor e cleanup do harness;
- expectativas e vetores de teste.

O código test-only não poderá possuir uma segunda implementação da lógica de:

- envelope e validação canônicos;
- sequência, replay, gap, idempotência e publicação;
- continuidade, quarantine, fencing, backpressure ou retention;
- policy freeze, holdout e decisão de publicação;
- projeção, trace, freshness ou estados factuais;
- activation guard e autorização de avaliação/publicação.

Quando um port não possuir implementação operacional, o harness poderá fornecer
um adapter sintético, mas deverá exercer o mesmo contrato e a mesma
orquestração product-owned. Dependência `src → tests`, cópia de lógica ou
desvio do caminho canônico bloqueia o lote.

### Regra da composição normal

Em `ActivationState=None`, a composição normal deverá:

- registrar somente a boundary dormente e a authority indisponível;
- iniciar zero hosted worker ou background task pertencente ao MOD-12;
- criar zero schedule, timer, fila ou polling MOD-12;
- abrir zero listener, store, ficheiro, socket, provider ou database pelo
  MOD-12;
- ler zero corpus ou observação para análise MOD-12;
- publicar zero sinal, projeção, recomendação, plano ou comando;
- reportar `MayEvaluate=false`, `MayPublish=false` e fence `0`;
- falhar fechada diante de configuração ausente, aproximada ou não autorizada.

O Server e o Agent podem continuar as suas responsabilidades normais; “zero
I/O” refere-se ao trabalho próprio do MOD-12, não ao encerramento do produto.

## Ordem técnica e dependências

Os IDs `PC-M12` são requisitos, não uma ordem segura de implementação. A ordem
futura deverá ser:

```text
PC-M12-02  caminho product-owned
     ↓
PC-M12-03  trust, recursos, isolamento e estados factuais
     ↓
PC-M12-01  revalidação na baseline técnica resultante
     ↓
PC-M12-04  Quality Gate, amostras humanas e decisão de fechamento
```

Revalidar O1–O4 antes das mudanças de integração produziria evidência obsoleta.
Cada lote abaixo para ao concluir e exige autoridade própria.

### Gate proprietário de cada lote

M12-IC1, M12-IC2, M12-IC3 e M12-IC4 exigirão, dentro da autoridade exata do
respetivo lote:

- preflight, baseline, whitelist e checks aplicáveis explicitamente
  autorizados;
- relatório factual separado;
- Quality Gate automático próprio com classificação
  `APROVADO`, `REPROVADO`, `BLOQUEADO` ou `N/A` justificado;
- cleanup e rollback verificados;
- decisão de parar antes do lote seguinte.

O próximo lote somente se torna elegível quando o gate proprietário anterior
estiver `APROVADO`. Uma amostra humana descoberta antes de M12-IC6 exigirá
autoridade própria e não será inferida da autoridade técnica. M12-IC5 é uma
auditoria consolidada adicional: não substitui nem agrega retroativamente os
gates proprietários de M12-IC1–IC4. Cada autoridade de lote poderá abranger
sua implementação e validação própria explicitamente delimitadas; não
abrangerá o lote seguinte.

## Lotes futuros propostos

### M12-IC1 — Boundary product-owned e composição dormente

**PC principal:** `PC-M12-02`.

**Objetivo:** congelar e implementar os contratos de integração, ports,
adapters e activation guard product-owned sem criar caminho ativo.

**Saídas mínimas futuras:**

- inventário exato da baseline, delta e ownership;
- contrato versionado do contexto de integração e da publicação;
- ports para trust, continuity, resource admission, corpus, policy,
  projection e clock/cancellation;
- composição normal exclusivamente dormente;
- composição sintética explícita que injeta fixtures através dos ports;
- testes arquiteturais que proíbam `src → tests`, implementação paralela,
  hosted worker e I/O em `None`;
- relatório factual e rollback do lote.

**Parada:** dependência, migration, configuração operacional, novo estado de
ativação, worker, I/O normal, provider/corpus real ou necessidade de O5.

### M12-IC2 — Pipeline canônico Agent → Server → MOD-12

**Dependência:** M12-IC1 com relatório e Quality Gate próprio `APROVADO`.

**PC principais:** `PC-M12-02` e `PC-M12-03`.

**Objetivo:** exercer no harness sintético o dispatch e a ingestão reais até a
boundary MOD-12 product-owned, sem rota alternativa.

**Evidência mínima futura:**

- contratos `N-1/N/N+1` e recusa de versão incompatível;
- identidade, provenance, classification, scope e freshness preservados;
- replay e duplicate sem segunda análise/publicação;
- reorder/gap sem análise antes da sequência contígua;
- revogação, supersession e contexto stale sem publicação;
- restart e crash old-or-new, idempotency ledger e writer fence;
- backpressure, retention e compaction bounded;
- deadline, cancellation, quiescência e cleanup;
- diagnostic-error recusado ou preservado explicitamente, nunca descartado;
- zero secret ou payload provider-native em envelope/diagnóstico;
- relatório completo, corrente e `IsAuthorising=false`.

**Parada:** publicação parcial, replay duplicado, gap processado, contexto
revogado/stale publicado, limite unbounded ou I/O normal em `None`.

### M12-IC3 — Sinais, projeção, API e UI factuais

**Dependência:** M12-IC2 com relatório e Quality Gate próprio `APROVADO`.

**PC principais:** `PC-M12-02` e `PC-M12-03`.

**Objetivo:** ligar corpus/policy/análise, projeção, API e UI product-owned ao
mesmo trace sintético, mantendo a rota ausente da composição normal.

**Evidência mínima futura:**

- manifesto, membership, partições e critérios O3-A ligados ao contexto
  corrente;
- policy autenticada e congelada antes do primeiro holdout;
- holdout one-use e all-or-nothing;
- falso positivo histórico e erro `1.00` preservados ou drift investigado e
  bloqueado, nunca ocultado;
- trace Agent/O2 → resultado O3 → policy → corpus → projection, com digests e
  context revision;
- projeção recusada quando incompleta, alterada, expirada, revogada,
  superseded, future-skewed ou divergente;
- API autenticada/autorizada, bounded, read-only e sem endpoint de escrita;
- UI exercitando separadamente `Current`, `Stale`, `Unknown`,
  `Detected`, `NotDetected` e `InsufficientEvidence`;
- forecast ausente exibido como `Unknown`;
- falha de transport/integrity diferenciada de `Unknown`;
- fonte, freshness, incerteza, limitações e `ActivationState=None` visíveis;
- zero recomendação, plano, aprovação, comando ou ação.

**Parada:** corpus alegado representativo, holdout aberto antes do freeze,
critério relaxado após resultado, trace divergente, estado factual apresentado
como sucesso ou rota ativa na composição normal.

### M12-IC4 — Revalidação integrada na baseline técnica final

**Dependência:** M12-IC1–IC3 com Quality Gates próprios `APROVADOS` e
baseline técnica congelada.

**PC principais:** `PC-M12-01` e `PC-M12-03`.

**Objetivo:** revalidar contra o único caminho final todos os requisitos
aplicáveis de O1, O2-A, O2-B, O3-A, O3-B e O4, além de somente estas parcelas:

- O5-R2: composição dormente, authority indisponível, kill/refusal, fencing e
  zero avaliação/publicação;
- O5-R3: códigos sanitizados, bounds, runbooks não executáveis e exercícios
  sintéticos, sem sink ou claim de SLO operacional.

Esse escopo não reabre O5, não executa O5-R1/R4–R10, não reclassifica o gate
O5 e não executa PF-OBS, HM ou D9.

**Matrizes mínimas futuras:**

- 68 vetores O1 e respetivos casos adversariais;
- matriz completa O2 de versão, identity, sequence, replay, gap, freshness,
  revocation, supersession, restart, crash e publication;
- corrupção, rollback, gap, divergence, quarantine, recovery e stale fence;
- limites exatos e `±1`, checked arithmetic, budgets, deadline, cancellation,
  quiescência, saturation, retention e compaction;
- O3 authority, poisoning, missingness, leakage, withdrawal, policy freeze,
  holdout, métricas e non-finite/missing results;
- O4 trace, schema, auth, métodos negativos, freshness/disposition,
  localisation, temas, reflow, teclado, forced colours e acessibilidade
  automatizada;
- composição normal dormente e harness isolado;
- cleanup de processo, listener, browser, store e raiz temporária.

Qualquer vetor não reproduzível deve ser classificado `BLOQUEADO`; não pode ser
aprovado por ancestralidade ou por Human Gate histórico.

### M12-IC5 — Quality Gate consolidado PC-M12

**Dependência:** M12-IC4 com Quality Gate próprio `APROVADO` e sem mudança
posterior na baseline.

**PC principal:** parcela automática de `PC-M12-04`.

**Objetivo:** auditar sem corrigir a baseline candidata e emitir um único
relatório automático.

**Critérios mínimos futuros:**

- preflight, worktree, commit, ancestralidade e delta exatos;
- todos os requisitos de `PC-M12-01`–`03` com classificação corrente;
- caminho product-owned único e ausência de lógica paralela test-only;
- zero MOD-12 worker, scheduling ou I/O na composição normal em `None`;
- build, formato/analyzers, unit, arquitetura, integração, Web, WPF,
  segurança, acessibilidade e regressões aplicáveis aprovados;
- cobertura mínima de `70%` linhas e `45%` branches, sem reduzir piso local;
- zero P0/P1 aberto;
- P2 somente com owner, impacto, condição/prazo e handoff explícitos, sem
  dispensar boundary, guard, fail-closed ou segurança;
- documentação en-GB de código, links, secrets/real-host e diff aprovados;
- zero resíduo e relatório sanitizado;
- O5 `BLOQUEADO`, PF-OBS/HM sem aprovação e `ActivationState=None`
  preservados.

Falha, bloqueio, drift ou resíduo impedem a amostra humana. Correção exige
novo lote de remediação `M12-ICR-*` e repetição proporcional; o auditor não
corrige durante o gate.

### M12-IC6 — Amostras humanas e decisão de fechamento

**Dependência:** M12-IC5 automaticamente aprovado.

**PC principal:** parcela humana de `PC-M12-04`.

As amostras exigirão autoridade de runtime/browser separada e usarão IDs
novos:

| ID | Amostra mínima futura |
|---|---|
| `S06-M12-IC-H01` | pt-BR/en-GB, Light/Dark, desktop/compacto, teclado/foco, verdade sintética/read-only e `ActivationState=None` |
| `S06-M12-IC-H02` | matriz visível `Current`/`Stale`/`Unknown` e `Detected`/`NotDetected`/`InsufficientEvidence`, forecast `Unknown`, evidence trace e limitações |
| `S06-M12-IC-H03` | payload expired/revoked/superseded/tampered/incomplete produz indisponibilidade sanitizada e visualmente distinta de `Unknown`, sem revelar detalhe de transport/integrity, resultado parcial favorável ou ação |

Leitor de tela físico, mixed-DPI e scaling Windows somente poderão ser
classificados aprovados se forem realmente autorizados e amostrados. Caso
contrário, permanecerão não testados e como ressalva explícita.

Depois das amostras, Bruno receberá um resumo de um único escopo contendo:

- commit e relatório automático revistos;
- decisão individual das três amostras;
- cobertura, limitações e ressalvas;
- handoff O5/PF-OBS/STATE-07;
- decisão solicitada:
  `PENDENTE`, `APROVADO`, `APROVADO COM RESSALVAS` ou `REPROVADO`.

Esse Human Gate fecha somente o escopo de integração MOD-12 no `STATE-06`.
Não é Human Gate O5, gate `None → Observer`, Human Gate de lifecycle ou
autorização de transição.

Ausência de resposta, resposta ambígua ou decisão que não esteja ligada ao
relatório e às três amostras mantém o gate `PENDENTE` e não produz a
classificação `MOD-12 STATE-06 INTEGRATION SCOPE COMPLETE`.

## Matriz de rastreabilidade PC-M12

| Requisito | Lotes proprietários | Proveniência histórica | Resultado corrente obrigatório |
|---|---|---|---|
| `PC-M12-01` | M12-IC4 | relatórios/Human Gates O1–O4 e parcelas O5-R2/R3 | revalidação item a item na baseline final, sem carry-forward implícito |
| `PC-M12-02` | M12-IC1–IC3 | sandboxes O2/O3/O4 e control plane dormente | um caminho product-owned ponta a ponta, harness sem lógica paralela e publicação somente completa/corrente |
| `PC-M12-03` | M12-IC2–IC4 | vetores O1/O2/O3/O4 | trust, quarantine, fencing, budgets, cancellation, isolamento, cleanup e estados factuais aprovados |
| `PC-M12-04` | M12-IC5–IC6 | Quality/Human Gates históricos somente como formato e limitações | Quality Gate corrente, três amostras novas e decisão específica de fechamento |

Cada relatório futuro deverá manter um ledger com:

- `PC-ID`, requisito e critério mensurável;
- evidência histórica usada somente como proveniência;
- gap corrente;
- owner e lote;
- autoridade exata e baseline;
- paths/contratos afetados;
- checks, thresholds, resultado e artefacto;
- amostra humana ou `N/A` justificado;
- classificação, limitação e handoff;
- trigger e alvo de rollback;
- digest/identificador sanitizado da evidência.

Os eixos `planeado`, `autorizado`, `implementado`, `verificado`,
`homologado`, `ativado` e `lifecycle` permanecem independentes.

## Regra de reutilização proporcional

Uma evidência histórica somente poderá reduzir repetição quando todos os
itens forem provados:

1. requisito e comportamento continuam idênticos;
2. cadeia Git e integridade do artefacto estão confirmadas;
3. componente, contrato, configuração, SDK e dependências não sofreram mudança
   material;
4. regressão corrente que cobre o comportamento passou;
5. semântica, segurança, timing, persistência e ambiente não sofreram drift;
6. limitação histórica permanece explícita;
7. relatório corrente justifica o carry-forward item a item.

Mudança product-owned, alteração posterior de O4 ou condição indeterminada
obriga repetição ou classificação `BLOQUEADA`.

## Condições globais de parada

Qualquer lote futuro para imediatamente quando ocorrer:

1. baseline, delta, worktree ou resíduo não isolado;
2. implementação alternativa test-only ou dependência `src → tests`;
3. `ActivationState` diferente de `None`;
4. worker, scheduler, store, listener, provider read ou publicação MOD-12 na
   composição normal;
5. dado, corpus, provider, database, credencial, chave operacional, rede ou
   serviço externo fora da autoridade;
6. package, lockfile, dependency, migration ou schema fora da whitelist;
7. root trazida pelo bundle, dual control quebrado ou quarantine contornável;
8. resultado parcial/cancelled/expired/revoked/stale publicado;
9. replay duplicado, gap processado ou fence obsoleto aceito;
10. holdout pré-freeze/reutilizado, leakage, withdrawal revertido, falso
    positivo ocultado ou critério relaxado;
11. trace divergente, métrica ausente/não finita ou drift determinístico;
12. acesso API indevido, método de escrita ou estado factual apresentado como
    sucesso;
13. gate, cobertura ou amostra humana falhar ou bloquear;
14. necessidade de D9, O5, PF-OBS, JOSE, ativação ou lifecycle.

Parada não autoriza remediação. O responsável registra causa, impacto,
evidência, trabalho seguro ainda possível e condição objetiva de desbloqueio.

## Handoff obrigatório para STATE-07

Mesmo com o fechamento aprovado, o handoff deve preservar:

- O5 `BLOQUEADO`, sem reclassificação;
- O5-R4 com owner operacional nominal, autoridade/custódia de ativação e
  continuidade/reconciliação independente ainda pendentes;
- PF-OBS/HM sem aprovação e D6–D8 como história imutável;
- D9 opcional e não autorizado;
- corpus representativo, calibração operacional, segurança/red team, carga,
  recovery, célula exata e O5-R6–R10 como homologação posterior;
- PostgreSQL `Homologation=None` e suporte público `No`;
- `ActivationState=None`;
- zero LLM, recomendação, plano, comando ou automação.

Esse handoff não entra em `STATE-07`. JOSE ainda deverá completar seu próprio
escopo `STATE-06`, seguido pela revalidação consolidada, novas amostras
humanas, Human Gate de revalidação e uma decisão de lifecycle separada.

## Riscos e contenções

| Risco | Impacto | Contenção obrigatória |
|---|---|---|
| migrar lógica test-owned e alterar semântica | gates históricos deixam de representar o comportamento | oráculos históricos, diff de comportamento e revalidação completa na baseline final |
| criar pipeline paralelo para facilitar o harness | `PC-M12-02` permanece insatisfeito | ownership product-owned e testes arquiteturais contra duplicação |
| ligar acidentalmente a composição normal | trabalho AIOps não autorizado | guard `None`, authority indisponível e provas de zero worker/I/O |
| transformar fixture em claim operacional | suporte ou segurança falsos | labels sintéticos, limites preservados e claims fechados |
| confundir parcelas O5-R2/R3 com aprovação O5 | ativação prematura | revalidar somente regressões inativas/sintéticas e preservar O5 `BLOQUEADO` |
| mudança posterior invalidar revalidação | evidência obsoleta | congelar baseline antes de M12-IC4 e repetir após qualquer mudança material |
| Human Gate dispensar falha técnica | fechamento não sustentado | amostras somente após Quality Gate aprovado; P0/P1 e boundaries não são dispensáveis |

## Rollback e cleanup futuros

Este lote não possui rollback técnico porque não altera produto. Correção do
plano será feita por novo commit documental focado; uma substituição futura
deverá ser registrada num novo documento, no `Current-State` e no histórico
append-only como supersessão. Este ficheiro, seu commit, relatórios e Human
Gates históricos não serão reescritos.

Cada lote técnico futuro deverá definir antes da execução:

- trigger, owner e baseline de rollback;
- separação entre source, configuração, schema e runtime;
- remoção segura de composição/registro do lote;
- preservação de auditoria e evidência;
- cleanup de processo, listener, browser, store, chave efémera e raiz
  temporária;
- revalidação do estado dormente após rollback.

Migration, estado operacional ou dado real não são pressupostos deste plano.
Se surgirem como necessidade, o lote para e volta para nova decisão.

## Autoridades futuras mínimas

Este plano prevê **mínimo 8 decisões futuras**, sem agrupamento entre lotes,
gates ou decisões:

1. revisar e aceitar, aceitar com ressalvas, devolver ou rejeitar este plano
   apenas como direção documental;
2. autorizar M12-IC1;
3. autorizar M12-IC2;
4. autorizar M12-IC3;
5. autorizar M12-IC4;
6. autorizar M12-IC5;
7. autorizar as três amostras humanas M12-IC6;
8. decidir o Human Gate e a classificação final do fechamento MOD-12.

M12-IC6 é uma única campanha humana bounded, autorizada como um escopo que
contém três amostras individualmente registradas e decididas. Se a autoridade
futura exigir decisões ou ambientes separados, a contagem mínima aumenta.

Cada `M12-ICR-*` de remediação acrescentará pelo menos uma autoridade e poderá
exigir repetição de gate. Aceitar uma decisão não concede a seguinte.

## Fora de escopo deste lote documental

Não foram autorizados ou executados:

- alteração de source, testes, configuração executável, migration ou schema;
- build, test, format, coverage, validator ou runtime;
- restore, download, package, lockfile ou dependência;
- Agent, Server, Dashboard, WPF, Tray, browser, provider, database ou
  container;
- acesso externo, rede, CI, registry, cloud ou infraestrutura;
- corpus, telemetria, credencial, certificado ou chave operacional;
- D9, O5, PF-OBS, HM ou qualquer diagnóstico/campanha;
- LLM, recomendação, plano operacional, comando ou automação;
- JOSE-1 ou qualquer lote JOSE;
- ativação MOD-12, `None → Observer`, homologação ou lifecycle.

## Resultado documental e próxima decisão

Este documento:

- decompõe `PC-M12-01`–`04` em seis lotes futuros;
- fixa a ordem técnica `02 → 03 → 01 → 04`;
- separa ownership product-owned de fixtures do harness;
- preserva todos os bloqueios e claims máximos;
- define evidência, stop conditions, rollback e autoridades;
- não classifica qualquer `PC-M12` como satisfeito.

A próxima decisão permitida é somente revisar este plano. Opções:

- `ACEITO EXCLUSIVAMENTE COMO DIREÇÃO DOCUMENTAL`;
- `ACEITO COM RESSALVAS DOCUMENTAIS`;
- `AJUSTES SOLICITADOS`;
- `REJEITADO`.

Mesmo um aceite sem ressalvas não autorizará M12-IC1.

Os validators de documentação, secret/host scan, build, testes e runtime não
integram a autoridade deste lote e não devem ser apresentados como executados.
São permitidas somente inspeções locais Git/textuais, revisão independente e
higiene do diff documental.

## Referências

- [Proposta reconciliada de revalidação STATE-06 → STATE-07](STATE-06-To-STATE-07-Transition-Revalidation-Proposal.md)
- [Programa operacional MOD-12](STATE-06-MOD-12-Operational-AIOps-Programme-And-Restricted-Observer-Proposal.md)
- [O1 — Trust e resource admission](STATE-06-MOD-12-O1-Durable-Trust-And-Resource-Admission-Report.md)
- [O2-A — Pipeline canônico](STATE-06-MOD-12-O2A-Canonical-Read-Only-Observation-Pipeline-Sandbox-Report.md)
- [O2-B — Continuidade durável](STATE-06-MOD-12-O2B-Durable-Pipeline-Continuity-Report.md)
- [O3-A — Corpus governado sintético](STATE-06-MOD-12-O3A-Governed-Synthetic-Corpus-Sandbox-Report.md)
- [O3-B — Calibração e holdout sintéticos](STATE-06-MOD-12-O3B-Governed-Offline-Calibration-And-Holdout-Report.md)
- [O4 — Projeção/API/UI factual](STATE-06-MOD-12-O4-Factual-Observer-Projection-API-UI-Sandbox-Report.md)
- [Human Gate O4](STATE-06-MOD-12-O4-Human-Gate-Report.md)
- [O5-R2 — Control plane inativo](STATE-06-MOD-12-O5-R2-Inactive-Control-Plane-Report.md)
- [O5-R3 — Observabilidade sintética](STATE-06-MOD-12-O5-R3-Observability-SLO-Incident-Response-Report.md)
- [AIOps e IA](../prompts/foundation/AIOps-And-AI-Module.md)
- [Quality Gates](../prompts/governance/Quality-Gates.md)
- [Estado factual](../prompts/state/Current-State.md)
