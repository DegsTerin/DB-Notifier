# MOD-12 — Pacote de prontidão para a transição `NONE → OBSERVER`

## Decisão executiva

- Data: 2026-07-23.
- Baseline documental analisada: `d4e970c0d0323407541b37bc18c6c2f02c85114f`.
- Ciclo de vida: `STATE-06 INTEGRATION`.
- Estado de ativação MOD-12: `ActivationState=None`.
- Resultado do pacote: **NÃO PRONTO PARA ATIVAÇÃO**.
- Próximo incremento recomendado: **O2-A — Canonical Read-Only Observation Pipeline Sandbox**.

O MOD-12 possui uma fundação determinística inativa e um O1 sintético, test-only, automática e humanamente aprovado. Isso é suficiente para preparar o próximo sandbox de integração, mas não é evidência para ativar `OBSERVER`.

A transição permanece bloqueada porque ainda não existem, em conjunto:

- composição normal deny-by-default para o modo `OBSERVER`;
- ingresso canônico Agent → Server → MOD-12 sob o mesmo contexto imutável de confiança e recursos;
- corpus representativo governado e calibração por segmentos;
- limites empíricos homologados;
- projeção somente leitura e observabilidade operacional do modo;
- Quality Gate e Human Gate específicos da ativação.

Este pacote é exclusivamente documental. Não implementa, executa ou ativa componente algum e não altera o ciclo de vida.

## Autoridade e limites

Bruno autorizou exclusivamente:

- analisar `ADR-0007`, O1 e a fundação MOD-12;
- identificar lacunas;
- definir composição futura, entradas canônicas read-only, corpus, recursos, segurança, rollback, observabilidade e matrizes de testes/Human Gate;
- classificar requisitos;
- propor plano incremental, critérios de ativação e próximo lote.

Permanecem proibidos neste pacote: código, configuração, build, teste, serviço, telemetria real, provider ou banco real, corpus real, LLM, recomendação, comando, automação, acesso externo, push, deploy, ativação e transição de ciclo de vida.

## Significado das classificações

| Classificação | Significado neste pacote |
|---|---|
| `PRONTO` | Existe decisão aceita ou evidência histórica aceita suficiente para servir de base ao próximo incremento restrito. Não significa produção ou ativação. |
| `LACUNA` | O requisito é necessário, mas a implementação, decisão ou evidência ainda não existe de forma suficiente. |
| `BLOQUEADO` | O requisito não pode avançar sob a autoridade atual ou depende de lacunas anteriores e de autorização separada. |
| `NÃO TESTADO` | Existe desenho ou implementação parcial, mas falta prova no ambiente e no escopo exigidos. |

As evidências históricas não foram reexecutadas, porque esta autorização proibiu testes e runtimes.

## Fontes normativas e factuais

- [ADR-0007 — AIOps Trust Distribution and Resource Admission](architecture/ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md).
- [Contrato de governança de confiança e recursos](architecture/AIOps-Trust-Governance-And-Resource-Envelope.md).
- [Guardrails de arquitetura AIOps](architecture/AIOps-Architecture-Guardrails.md).
- [Programa AIOps e sequência O1–O5](STATE-06-MOD-12-Operational-AIOps-Programme-And-Restricted-Observer-Proposal.md).
- [Relatório da fundação OBSERVER](MOD-12-Observer-Foundation-Report.md).
- [Relatório R7-A0](STATE-06-Audit-Remediation-R7-A0-Report.md).
- [Relatório automático O1](STATE-06-MOD-12-O1-Durable-Trust-And-Resource-Admission-Report.md).
- [Human Gate O1](STATE-06-MOD-12-O1-Human-Gate-Report.md).
- [Estado atual](../prompts/state/Current-State.md).

## Matriz consolidada de prontidão

Cada requisito aparece uma única vez com uma classificação atual.

| ID | Requisito | Classificação | Evidência ou lacuna objetiva |
|---|---|---|---|
| `OR-01` | Sequência independente `NONE → OBSERVER → ADVISOR → ASSISTANT → CONTROLLED_AUTOMATION` | `PRONTO` | Sequência aceita como direção; nenhum modo posterior é condição implícita do `OBSERVER`. |
| `OR-02` | Separação entre capability implementada e modo ativo | `PRONTO` | R7-A0 declara `Capability=observer-analysis` e `ActivationState=None`. |
| `OR-03` | Serviço puro, determinístico, provider-neutral e não autorizador | `PRONTO` | Fundação Application produz análise reproduzível sem I/O, segredo, provider, executor ou mutação. |
| `OR-04` | Publicação somente de execução completa | `PRONTO` | R7-A0 recusa parcial, deadline, cancelamento e contexto obsoleto sem expor métricas de subconjunto. |
| `OR-05` | Continuidade, dual control, quarantine, recovery e admissão finita | `PRONTO` | O1 aprovado no sandbox sintético test-only; não constitui operação normal. |
| `OR-06` | Ausência de integração normal e de autoridade operacional | `PRONTO` | Evidência O1 aceita registra zero referência na composição normal e preserva `ActivationState=None`. |
| `OR-07` | Contrato canônico de observação read-only completo | `LACUNA` | Existe `HealthObservation` canônica e adapter limitado a duração de probe, mas não o envelope completo proposto neste pacote. |
| `OR-08` | Pipeline Agent → Server → MOD-12 sob um único contexto imutável | `LACUNA` | Há sandboxes separados e componentes preexistentes, mas não uma composição O2 conjunta e rastreável. |
| `OR-09` | Idempotência, sequência, replay, gap, freshness e revogação na entrada AIOps | `LACUNA` | Os controles existem em fronteiras anteriores ou no O1 sintético, mas não estão provados na cadeia AIOps integrada. |
| `OR-10` | Coordinator e store com ownership de produto claramente definido | `LACUNA` | O1 está no assembly de integração e em filesystem temporário; o host/store futuros ainda não foram selecionados. |
| `OR-11` | Opt-in normal por tenant/environment/purpose/scope | `LACUNA` | A política conceitual existe, mas não há configuração normal implementada para `OBSERVER`. |
| `OR-12` | Kill switch e rollback operacional do modo | `LACUNA` | O estado `None` é seguro hoje, mas não existe ainda mecanismo normal de ativação/desativação a ensaiar. |
| `OR-13` | Projeção/API/UI read-only do `OBSERVER` | `LACUNA` | Não existe superfície `OBSERVER`; apresentação pertence ao O4. |
| `OR-14` | Observabilidade sanitizada de admission, trust, corpus e análise | `LACUNA` | Há vocabulário e audit intents sintéticos; não há métricas/logs/traces operacionais do pipeline futuro. |
| `OR-15` | Corpus sintético exato, assinado e bounded para integração | `PRONTO` | O1 prova manifest/head/membership sintéticos e independência de papéis para o próximo sandbox. |
| `OR-16` | Corpus representativo governado por provider/version/platform/topology | `LACUNA` | As fixtures atuais são explicitamente sintéticas e não representam operação. |
| `OR-17` | Calibração de thresholds, baseline, previsão e insuficiência de evidência | `LACUNA` | Regras e OLS existem, mas não foram calibradas em corpus representativo segmentado. |
| `OR-18` | Robustez contra poisoning, conflito, missingness e distribuição incompatível | `LACUNA` | O1 cobre integridade estrutural sintética; validade semântica e viés continuam fora de prova. |
| `OR-19` | Limites determinísticos de bytes, estrutura, memória contabilizada, trabalho, output, deadline, cancelamento e fencing | `PRONTO` | O1 prova o contrato sintético serial, queue-disabled, em um host/processo. |
| `OR-20` | Valores de limites seguros para runtime e carga representativa | `NÃO TESTADO` | O ADR não escolhe números; O1 não homologa valores operacionais. |
| `OR-21` | Latência física de cancelamento, first-byte, idle e control update (`HM-01`) | `NÃO TESTADO` | Exige ambiente de medição declarado e campanha empírica. |
| `OR-22` | Memória observada versus fórmula contabilizada (`HM-02`) | `NÃO TESTADO` | Exige profiler, repetições e headroom aprovado. |
| `OR-23` | CPU/elapsed versus modelo de trabalho (`HM-03`) | `NÃO TESTADO` | Exige campanha empírica reproduzível. |
| `OR-24` | Fairness, quotas fleet-wide e continuidade cross-host/restart | `NÃO TESTADO` | O1 declara apenas contenção serial local, sem fila ou claim de fairness. |
| `OR-25` | Provisionamento, rotação, revogação e recovery de chaves operacionais | `LACUNA` | O1 usa chaves efêmeras sintéticas; tecnologia e owner operacionais não foram escolhidos. |
| `OR-26` | Witness independente contra rollback completo/split view isolado | `LACUNA` | Checkpoint local não detecta administrador que restaure ou reescreva todo o estado. |
| `OR-27` | Segurança revisada sem achado crítico/alto aberto para o escopo de ativação | `BLOQUEADO` | Depende da implementação O2–O4 e de revisão posterior. |
| `OR-28` | Provider e telemetria reais homologados para o primeiro escopo | `BLOQUEADO` | A autoridade atual proíbe fonte real; PostgreSQL é apenas candidato futuro não vinculante. |
| `OR-29` | Dados/corpus reais com base legal, classificação, retenção e aprovação | `BLOQUEADO` | Requer decisão de data governance e autorização externa separada. |
| `OR-30` | Reexecução corrente dos gates existentes | `NÃO TESTADO` | Este pacote não executou testes, builds ou serviços por proibição expressa. |
| `OR-31` | Quality Gate completo da ativação `NONE → OBSERVER` | `BLOQUEADO` | Somente pode ocorrer depois de O2, O3 e O4 concluídos. |
| `OR-32` | Human Gate informado e específico da ativação | `BLOQUEADO` | Não pode ser antecipado por este pacote nem pelo Human Gate O1. |
| `OR-33` | LLM, recomendação, plano, comando ou automação no `OBSERVER` | `BLOQUEADO` | Deliberadamente fora do modo e proibido; não é lacuna para ativar `OBSERVER`. |
| `OR-34` | Coerência documental pós-O1 | `LACUNA` | Alguns textos conceituais ainda dizem que os vetores O1 não foram implementados; devem ser atualizados factualmente sem reescrever o histórico. |

## Composição futura

### Cadeia-alvo do `OBSERVER`

```text
Provider homologado no Agent                           [futuro; fora do O2-A]
  -> HealthObservation canônica + qualidade/freshness
  -> outbox autenticado e idempotente do Agent
  -> ingestão Server: identidade, sequência, replay e escopo
  -> projeção canônica read-only
  -> ObserverIngressAdapter
  -> HostTrustCoordinator: contextRevision imutável
  -> HostResourceCoordinator: admissão/fencing/quiescência
  -> ObserverAnalysisService puro
  -> ObserverSignalResult completo e não autorizador
  -> projeção/API/UI OBSERVER somente leitura            [O4]
```

### Limites de ownership

| Componente futuro | Responsabilidade | Proibição |
|---|---|---|
| Agent/provider | Produzir observação normalizada no escopo homologado | Não decide sinal AIOps, confiança ou autorização |
| Ingresso Server | Autenticar origem, validar sequência, contrato, escopo e freshness | Não acessa diretamente o banco monitorado |
| `ObserverIngressAdapter` | Converter somente campos canônicos aprovados em evidência numérica | Não lê segredo, query ou payload provider-native |
| `HostTrustCoordinator` | Selecionar e avançar contexto completo de confiança/corpus | Não emite suas próprias credenciais ou approvals |
| `HostResourceCoordinator` | Admitir recursos e proteger control plane | Não promete fairness ou capacidade fleet-wide sem prova |
| `ObserverAnalysisService` | Analisar contexto imutável e produzir resultado não autorizador | Sem rede, store, provider, UI ou executor |
| Projeção `OBSERVER` | Expor sinais, evidência, incerteza e freshness read-only | Não recomenda nem cria comando |

O O2-A deverá compor essa cadeia apenas com fixtures sintéticas e marker test-only exato. A composição normal continuará sem referência e com `ActivationState=None`.

## Entrada canônica read-only proposta

O envelope futuro deverá ser versionado, imutável e provider-neutral. O O2-A deverá definir nomes e tipos executáveis; este pacote define somente os grupos obrigatórios.

| Grupo | Conteúdo mínimo | Regra |
|---|---|---|
| Identidade | evento, origem, Agent, instância e idempotency key estáveis | Sem alias não autenticado ou identidade derivada de conteúdo livre |
| Escopo | tenant, environment, purpose e scope canônico | Deve ser subconjunto exato do contexto autorizado |
| Versão | schema, transformação, unidade e perfil de accounting | Perfil desconhecido ou downgrade falha fechado |
| Tempo | observado, aceito, sequência, freshness e future-skew | UTC; stale/future/gap permanece explícito |
| Resultado | outcome canônico, métrica, valor, unidade e qualidade | Somente valores finitos e dentro dos limites |
| Proveniência | revisão de contexto, origem factual e digest do envelope | Deve sobreviver até o resultado |
| Classificação | classe de dados, redaction, retenção e finalidade permitida | Finalidade incompatível é recusada |
| Completude | missingness, cardinalidade e declaração exact/maximum | Ausência aplicável falha fechado |

Nunca pertencem ao envelope: senha, token, connection string, chave privada, certificado completo, query livre, SQL, shell, conteúdo bruto do banco, dados pessoais não aprovados ou detalhe provider-native não normalizado.

O conjunto inicial do O2-A deve continuar estreito: observações sintéticas equivalentes a `HealthObservation`, incluindo duration segmentada por outcome, freshness, sequência e qualidade. Novas famílias de sinais exigirão autorização posterior.

## Corpus representativo e governança

### Corpus de integração O2

| Requisito | Estado |
|---|---|
| Manifest/head assinados, papéis distintos, dual control, membership exata e predecessor | `PRONTO` no O1 sintético |
| Casos de replay, gap, stale, future, duplicate, revocation e context supersession | `LACUNA` na cadeia Agent → Server → MOD-12 |
| Conteúdo real ou representativo | Fora do O2-A |

### Corpus de avaliação O3

Antes de qualquer gate de ativação, um corpus governado deverá declarar e comprovar:

- dataset/revision/previous digest e membership exata;
- partitions independentes de desenvolvimento, calibração e holdout;
- provider, versão, plataforma, topologia, outcome, carga e qualidade de cada segmento;
- origem fixture versus observada, classificação, redaction, consentimento/autoridade, retenção e expiry;
- expected counts e critérios quantitativos aprovados para cada segmento;
- missingness, duplicação/leakage, bias conhecido e lacunas;
- casos stale, absent, conflitantes, ruidosos, adversariais e poisoned;
- origem e incerteza de labels, revisores independentes e withdrawal irreversível.

Um manifest assinado comprova autoridade e integridade, não comprova que labels sejam corretos ou que a distribuição seja representativa.

PostgreSQL poderá ser proposto futuramente como primeiro segmento real de homologação, mas não está escolhido por este pacote e não implicará suporte geral.

## Recursos e backpressure

| Dimensão | Base disponível | Exigência antes da ativação |
|---|---|---|
| Bytes encoded/expanded e expansion ratio | Contrato O1 sintético | Medir no pipeline real homologado e aprovar ceilings |
| Estrutura e cardinalidade | Limites O1 | Provar reader/parser antes de materialização |
| Memória contabilizada | Fórmula O1 | Executar `HM-02` e aprovar headroom por runtime/arquitetura |
| Trabalho determinístico | Unidades O1 | Executar `HM-03` e revisar custos não lineares |
| Deadline, phase, first-byte e idle | Contrato O1 | Executar `HM-01` sob carga máxima admitida |
| Cancelamento/quiescência/fence | Prova local O1 | Provar no ownership e isolamento selecionados |
| Concorrência | Serial `1`, queue disabled | Manter assim no primeiro Observer ou autorizar política posterior |
| Quotas/fairness | Sem claim | Não anunciar fairness; qualquer fila requer lote próprio |
| Output/metadata | Bounded no O1 | Definir retenção, cardinalidade e compaction do runtime |
| Control lane | Separado no O1 | Provar que updates não são bloqueados por avaliação saturada |

Nenhum valor numérico operacional será inferido do sandbox. A ausência de calibração bloqueia a ativação.

## Segurança, rollback e observabilidade

### Segurança obrigatória

- deny by default e opt-in exato por mode/tenant/environment/purpose/scope;
- separação material de root, policy, bundle, revocation, corpus, bootstrap e recovery;
- dual control e nonce one-use;
- checkpoint, corpus head, approvals consumidos e audit intent em commit atômico;
- quarantine para rollback, gap, split view, corrupção, restore e freshness não comprovada;
- revalidação do mesmo `contextRevision` imediatamente antes da publicação;
- diagnóstico sanitizado por código estável;
- zero segredo, SQL, shell, provider API, LLM ou executor no MOD-12.

### Rollback da futura ativação

A ativação somente poderá ser considerada reversível se:

1. o estado inicial permanecer `None`;
2. um opt-in exato habilitar somente o escopo aprovado;
3. o kill switch impedir novas admissões e cancelar trabalho corrente;
4. resultados incompletos ou do contexto antigo não forem publicados;
5. o monitoramento determinístico normal continuar independente;
6. desativar `OBSERVER` não alterar, excluir ou reinterpretar observações canônicas;
7. trust/corpus comprometido entrar em quarantine, sem fallback para evidência não autenticada;
8. rollback de software/configuração possuir procedimento e ensaio próprios;
9. reativação exigir contexto fresco e gate explícito, nunca ocorrer automaticamente.

Hoje apenas os itens 1, 5 e a semântica fail-closed parcial têm base suficiente. O mecanismo operacional completo é uma lacuna.

### Observabilidade mínima futura

| Área | Evidência permitida | Conteúdo proibido |
|---|---|---|
| Ingresso | accepted/refused, schema, source class, freshness e contadores bounded | Payload, segredo ou topologia sensível |
| Trust/corpus | context revision, head digest abreviado, state e refusal code | Chave privada, assinatura completa ou conteúdo |
| Recursos | reserved/consumed/rejected/released, deadline e fence | Dados da observação |
| Análise | versão, feature IDs, completeness, disposition e duração | Agregado parcial apresentado como resultado |
| Operação | saturation, quarantine, kill-switch state e rollback result | Alegação de saúde/provider não comprovada |

SLOs, alertas, retenção e ownership desses sinais ainda são lacunas.

## Plano incremental

| Incremento | Objetivo | Saída obrigatória | Mode após conclusão |
|---|---|---|---|
| `O2-A` | Compor ingresso canônico sintético Agent → Server → O1 → análise | Envelope versionado, pipeline test-only, replay/freshness/revocation/context tests, relatório e gates | `None` |
| `O2-B` | Endurecer persistência sintética, restart, backpressure e observabilidade do pipeline | Crash/restart, audit/refusal codes, bounded retention e cleanup | `None` |
| `O3-A` | Definir e aprovar corpus representativo governado | Manifest, segmentos, holdout, data-governance decision e critérios quantitativos | `None` |
| `O3-B` | Calibrar sinais e limites; executar segurança/carga/HM-01–03 | Precision/recall/FP/FN/calibration, insufficient-evidence e limites medidos | `None` |
| `O4` | Expor projeção/API/UI read-only factual | Hipóteses, evidências, incerteza, freshness, acessibilidade e amostras humanas | `None` |
| `O5` | Quality Gate e Human Gate de ativação | Relatório consolidado, rollback ensaiado e decisão humana exata | `None` até a decisão |
| Ativação | Aplicar opt-in no escopo aprovado | `ActivationState=Observer` somente após autorização própria | `Observer` |

Cada incremento exige autorização, relatório automático e aceitação separados. Aprovar O2, O3 ou O4 não ativa o modo.

## Matriz mínima de testes futuros

Nenhum item desta seção foi executado por este pacote.

| Campanha | Conteúdo | Gate |
|---|---|---|
| Contrato O2 | schema/version/unit, hostiles, bounds, exact/maximum, sanitização | Todos passam; unknown/downgrade recusados |
| Pipeline O2 | duplicate, reorder, replay, gap, stale, future, revocation e in-flight supersession | Zero dupla análise; publicação somente completa e atual |
| Continuidade O2-B | crash antes/durante/depois, missing/corrupt/restore, fence antigo e restart | Somente estado antigo ou novo completo; dano entra em quarantine |
| Recursos O2-B | limites ±1, saturação, cancelamento, timeout, non-quiescence e control update | Nenhum oversubscription ou stale publication |
| Corpus O3 | membership, partitions, leakage, bias, poisoning, withdrawal e segment coverage | Corpus incompatível nunca gera passe |
| Avaliação O3 | precision, recall, FP/FN, calibration, insufficient evidence por segmento | Thresholds previamente aprovados e atendidos em holdout |
| Empírico O3 | `HM-01`, `HM-02`, `HM-03`, load/endurance e restart | Limites e SLOs medidos no ambiente declarado |
| Segurança | threat model, roles, replay, scope, restore, split view, secret canaries | Zero crítico/alto aberto no escopo |
| Composição | normal disabled, opt-in exato, kill switch e rollback | Zero referência/atividade quando `None`; reversão comprovada |
| UI O4 | truth, freshness, unknown/stale, no recommendation, WCAG e reflow | Automático e amostras humanas aprovados |

As condições físicas de acessibilidade/DPI não testadas no R6 não podem ser transferidas como aprovação para uma futura UI `OBSERVER`. O O4 deverá testá-las ou obter ressalva humana específica.

## Human Gates futuros

| Gate | Decisão necessária | Não autoriza |
|---|---|---|
| O2-A/O2-B | Aceitar a cadeia sintética e seus limites | Corpus real, UI ou ativação |
| O3-A | Aceitar autoridade, representatividade e uso do corpus | Ativação ou coleta adicional |
| O3-B | Aceitar calibração, segurança, recursos e riscos residuais | UI ou ativação |
| O4 | Aceitar verdade visual, explicabilidade e acessibilidade | Ativação |
| O5 Quality Gate | Confirmar que todos os critérios objetivos passaram | Não substitui decisão humana |
| O5 Human Gate | `APROVADO`, `APROVADO COM RESSALVAS` ou `REPROVADO` para o escopo exato | Não executa a transição |
| Transição | Autorizar separadamente a mudança `None → Observer` | ADVISOR, LLM, comandos ou automação |

## Critérios objetivos para permitir a proposta de ativação

Uma proposta executiva de ativação somente poderá ser apresentada quando todos os critérios abaixo estiverem satisfeitos:

1. O2-A e O2-B automática e humanamente aprovados.
2. O3-A possuir decisão formal de data governance e corpus ativo, fresco, segmentado e não retirado.
3. O3-B atender thresholds quantitativos predefinidos no holdout, incluindo `INSUFFICIENT_EVIDENCE`.
4. `HM-01`, `HM-02` e `HM-03` concluídos no ambiente declarado, com limites e headroom aprovados.
5. Threat model e revisão de segurança sem achado crítico ou alto aberto no escopo.
6. Opt-in exato, kill switch, quarantine, recovery e rollback implementados e ensaiados.
7. Normal composition permanecer inativa quando `ActivationState=None`.
8. O4 automática e humanamente aprovado, sem recomendação, comando ou autoridade insinuada.
9. Observabilidade, SLOs, retenção, owners e resposta a incidentes definidos e testados.
10. Matriz de homologação nomear exatamente provider/version/platform/topology/signal; entradas ausentes permanecerem não suportadas.
11. Quality Gate O5 consolidado aprovado com evidência sanitizada e cleanup.
12. Human Gate O5 informado e específico, seguido de autorização separada da transição.

Qualquer item ausente mantém `ActivationState=None`.

## Proposta concisa do próximo lote

### O2-A — Canonical Read-Only Observation Pipeline Sandbox

Proposta de autorização futura, ainda **não concedida**:

> AUTORIZO EXCLUSIVAMENTE A IMPLEMENTAÇÃO LOCAL DO O2-A — CANONICAL READ-ONLY OBSERVATION PIPELINE SANDBOX, sobre a baseline a confirmar, somente sob marker test-only opt-in e mantendo `ActivationState=None`. Autorizo definir o envelope canônico provider-neutral, compor fixtures sintéticas Agent → Server → O1 → MOD-12, provar idempotência, replay, gap, freshness, revogação, context supersession, deadlines, cancellation e publicação completa não autorizadora, com store temporária sintética, testes offline, documentação e commit local focado. Proíbo composição normal, telemetria/corpus/provider/banco/credencial real, nova dependência, acesso externo, UI, LLM, recomendação, comando, automação, push, deploy, ativação e transição. A conclusão exigirá zero referência na composição normal, cleanup, Quality Gate e Human Gate separados.

Antes de qualquer execução, essa proposta deverá ser revisada contra a baseline então corrente e autorizada explicitamente por Bruno.

## Disposição final

- Pacote documental: `CONCLUÍDO`.
- Prontidão para implementar O2-A: `PRONTO PARA PROPOSTA`, não para execução.
- Prontidão para `NONE → OBSERVER`: `BLOQUEADO`.
- `ActivationState`: `None`.
- Implementação, Quality Gate, Human Gate, ativação e lifecycle: não autorizados por este pacote.
