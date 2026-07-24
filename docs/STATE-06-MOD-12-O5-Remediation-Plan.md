# Plano de remediação das lacunas do O5

## Status e autoridade

- Data: 2026-07-24.
- Baseline de elaboração: `42541abd4546a01f83d21fe2382cd5a14cb7c94e`.
- Origem: [O5 — Quality Gate de ativação `None → Observer`](STATE-06-MOD-12-O5-None-To-Observer-Quality-Gate-Report.md).
- Natureza: plano documental; nenhum lote está autorizado.
- Estado do MOD-12: `ActivationState=None`.
- Resultado O5 corrente: `BLOQUEADO`.
- Implementação, runtime, dado real, ativação e transição: não executados.

Este plano transforma as lacunas do O5 em lotes independentes e ordenados pelo menor risco. Cada
lote exige autorização, relatório automático e decisão humana próprios. A aprovação de um lote não
autoriza o seguinte e nunca altera automaticamente o estado de ativação.

## Princípios de execução futura

1. Definir primeiro o escopo exato; somente depois admitir ambiente, provider ou dados.
2. Manter `ActivationState=None` durante todos os lotes de remediação.
3. Separar prova sintética, prova representativa de laboratório e prova operacional.
4. Limitar a primeira entrada a um único provider/version/platform/topology/signal set.
5. Preferir abstention/`Unknown` quando evidência, contexto ou qualidade forem insuficientes.
6. Não usar corpus para calibração antes de autoridade, classificação, retenção e partições estarem
   aprovadas.
7. Congelar política e thresholds antes do primeiro acesso ao holdout.
8. Fazer segurança, rollback e resposta a incidentes falharem fechados antes de qualquer ensaio de
   ativação.
9. Não inferir suporte geral a partir de uma única entrada homologada.
10. Repetir o O5 somente depois de todos os lotes obrigatórios terem decisões explícitas.

## Matriz de lacunas e remediação

| Lacuna O5 | Estado atual | Remediação planeada | Evidência mínima | Autoridade adicional |
|---|---|---|---|---|
| Escopo exato de ativação | `BLOQUEADO` | `O5-R1` define uma única célula piloto | provider, versão, plataforma, topologia e sinais nomeados; exclusões explícitas | elaboração e decisão documental |
| Corpus representativo | `BLOQUEADO` | `O5-R6` admite corpus governado para a célula escolhida | autoridade, proveniência, representatividade, labels, retenção, withdrawal e partições | acesso a fonte representativa; possível dado real exige autorização específica |
| Calibração e holdout | `BLOQUEADO` | `O5-R7` congela política e executa holdout one-use | thresholds prévios, métricas por segmento e `INSUFFICIENT_EVIDENCE` | execução de avaliação sobre o corpus aceito |
| Limites físicos `HM-01`–`HM-03` | `NÃO TESTADO` | `O5-R5` mede latência, memória e trabalho | ambiente declarado, repetições, distribuição, pior caso e headroom aprovado | runtime local controlado e profiler/counters homologados |
| Segurança do escopo ativável | `BLOQUEADO` | `O5-R4` executa threat model e revisão independente | zero crítico/alto aberto e disposição dos restantes | implementação prévia do control plane; revisão de segurança autorizada |
| Opt-in, kill switch e rollback | `BLOQUEADO` | `O5-R2` cria control plane inativo e ensaia reversão | default `None`, opt-in exato, kill switch bounded, quarantine, recovery e rollback | código/configuração local futura, sem ativação |
| Observabilidade e SLOs | `BLOQUEADO` | `O5-R3` define e ensaia operação | SLIs/SLOs, retenção, owners, alertas e runbooks exercitados | código/configuração e simulações locais futuras |
| Homologação exata | `BLOQUEADO` | `O5-R8` homologa somente a célula piloto | matriz capability/outcome, segurança, carga, falha, recovery e offline | provider/banco/laboratório real e credenciais sintéticas autorizados |
| Rehearsal integrado | `BLOQUEADO` | `O5-R9` reúne os resultados aceitos sem transição | opt-in/kill switch/rollback, soak, restart, offline e incident drill | runtime integrado local separado e autorização própria |
| Repetição do O5 | `BLOQUEADO` | `O5-R10` reaudita toda a cadeia | todos os critérios O5 classificados e cleanup | autorização separada, depois Human Gate próprio |

## Escopo piloto recomendado

O `O5-R1` deverá avaliar PostgreSQL como primeira opção porque é o único provider com backend slice
já implementado. Isso reduz a distância de engenharia, mas não constitui escolha, homologação ou
suporte. A decisão deverá restringir-se a uma célula, por exemplo:

`PostgreSQL / versão exata / sistema operativo exato / Agent local ou remoto exato / health e
latency read-only`.

Versão, plataforma, topologia e sinais não serão inventados neste plano. Eles dependerão do
ambiente que o utilizador puder disponibilizar e aceitar no Human Gate do `O5-R1`. Todas as demais
combinações permanecerão `Not evaluated` ou `Unsupported`, conforme a matriz aplicável.

## Sequência incremental

### O5-R1 — Escopo piloto e decisão de governança de dados

Objetivo: escolher documentalmente uma única célula de ativação e definir antecipadamente as
condições que tornarão o corpus e a avaliação aceitáveis.

Saídas obrigatórias:

- provider/version/platform/topology/signal set exatos;
- finalidade, owner, data owner, security owner e incident owner;
- fontes permitidas e proibidas;
- classificação, minimização, redaction, retenção, expiry e withdrawal;
- definição mensurável de representatividade para cada segmento;
- método de label, revisão independente e tratamento de conflito;
- partições development/calibration/holdout e política contra leakage;
- métricas e processo para aprovar thresholds antes do holdout;
- ambiente candidato para `HM-01`–`HM-03`;
- lista explícita do que continuará não suportado.

Critério de conclusão: todas as decisões acima classificadas como `APROVADA`, `BLOQUEADA` ou
`NÃO DEFINIDA`, sem campo implícito. Qualquer ausência mantém os lotes com dados bloqueados.

### O5-R2 — Control plane inativo de ativação e rollback

Dependência: O5-R1 aceito.

Objetivo: implementar futuramente, ainda em `None`, o controle provider-neutral necessário para
opt-in exato, kill switch, quarantine, recovery e rollback.

Critérios mensuráveis:

- default e configuração ausente resultam sempre em `None`;
- opt-in é limitado à célula, ambiente e finalidade aprovados;
- aprovação é autenticada, one-use, expira e não pode ampliar escopo;
- kill switch recusa novas admissões e cancela trabalho corrente dentro do deadline declarado;
- contexto antigo nunca publica após kill switch, revogação ou rollback;
- monitoramento determinístico permanece independente;
- restart, corrupção, restore antigo, fence obsoleto e falha parcial entram em quarantine;
- rollback restaura somente estado antigo ou novo completo;
- reativação nunca ocorre automaticamente.

Parada imediata: necessidade de comando, executor, migração operacional, dado real ou ativação do
Observer.

### O5-R3 — Observabilidade, SLOs e resposta a incidentes

Dependência: contratos de estado e diagnósticos do O5-R2.

Objetivo: definir e provar a operação observável antes de qualquer integração real.

Critérios mensuráveis:

- catálogo fechado de SLIs e códigos sanitizados;
- SLOs numéricos aprovados antes da campanha física;
- cardinalidade e retenção bounded;
- owner e escalonamento para cada alerta;
- runbooks de saturação, corrupção, stale, split view, kill switch e rollback;
- exercícios sintéticos com tempo de detecção, contenção, recuperação e encerramento registados;
- zero payload, segredo, SQL, identificador sensível ou topologia não aprovada nos diagnósticos.

Parada imediata: SLO não mensurável, alerta sem owner ou runbook que exija autoridade não concedida.

### O5-R4 — Threat model e revisão de segurança do escopo ativável

Dependências: O5-R1, O5-R2 e O5-R3 aceitos.

Objetivo: revisar a composição futura realista, não apenas os sandboxes isolados.

Critérios mensuráveis:

- assets, trust boundaries, identities e data flows completos;
- abuso de opt-in, replay, downgrade, rollback, split view, poisoning, leakage e exfiltration;
- red team de controle, corpus, API/UI e observabilidade;
- zero achado crítico ou alto aberto;
- achados médios/baixos com owner, prazo e decisão de risco;
- secret canaries e sanitização aprovados;
- evidência de que o Observer continua incapaz de recomendar, comandar ou executar.

Parada imediata: qualquer crítico/alto ou boundary desconhecida.

### O5-R5 — Campanha física `HM-01`, `HM-02` e `HM-03`

Dependências: escopo, SLOs e segurança aceitos.

Objetivo: medir o envelope no ambiente declarado sem substituir limites determinísticos por tempo.

Critérios mensuráveis:

- `HM-01`: distribuição, percentis e pior caso de first-byte, idle, cancellation e control update
  sob carga máxima admitida;
- `HM-02`: heap, working set e allocation peak em warm/cold runs, comparados à fórmula e ao
  headroom predefinido;
- `HM-03`: CPU/elapsed versus work units para parse, crypto, sort e análise, incluindo limites ±1;
- repetições e variância suficientes conforme protocolo pré-registado;
- zero oversubscription, publicação stale ou starvation do control lane;
- limites finais versionados e aprovados para o ambiente exato.

Parada imediata: profiler indisponível, ambiente não reproduzível, SLO falhado ou headroom
insuficiente.

### O5-R6 — Admissão de corpus governado e representativo

Dependências: O5-R1 e O5-R4; fonte e autoridade específicas previamente autorizadas.

Objetivo: construir o primeiro corpus representativo somente para a célula aprovada.

Critérios mensuráveis:

- manifest autenticado e content-addressed;
- origem, autoridade, licença/finalidade, classificação e retenção verificáveis;
- membership exata e partições imutáveis/disjuntas;
- cobertura por provider/version/platform/topology/outcome/carga/qualidade;
- representatividade comparada à população declarada, sem alegação além dela;
- missingness, duplicidade, leakage, bias, label uncertainty e conflitos quantificados;
- poisoning, corrupção, substituição, rollback, expiry e withdrawal falham fechados;
- nenhuma senha, token, connection string, SQL, dado pessoal não aprovado ou conteúdo bruto.

Parada imediata: autoridade incerta, dados pessoais não aprovados, segredo, licença incompatível,
segmento insuficiente ou impossibilidade de demonstrar representatividade.

### O5-R7 — Calibração e holdout representativos

Dependência: O5-R6 humanamente aceito.

Objetivo: avaliar o MOD-12 determinístico no corpus representativo sem retroalimentar o holdout.

Critérios mensuráveis:

- política autenticada e congelada antes do primeiro acesso ao holdout;
- thresholds quantitativos aprovados por segmento antes da execução;
- cobertura, abstention/Unknown, FP, FN, erro, estabilidade e explicabilidade completos;
- `INSUFFICIENT_EVIDENCE` exercitado;
- resultado reproduzível para o par exato corpus/política;
- nenhum ajuste após holdout; mudança exige nova revisão e novo holdout;
- falha fechada para métrica incompleta, segmento ausente, resultado não finito ou revisão
  divergente.

Parada imediata: threshold perdido, leakage, holdout reutilizado, segmento reprovado ou corpus
retirado.

### O5-R8 — Homologação da célula piloto

Dependências: O5-R4, O5-R5 e O5-R7 aceitos; laboratório real especificamente autorizado.

Objetivo: homologar somente a combinação exata escolhida, sem ampliar suporte.

Critérios mensuráveis:

- matriz provider/version/platform/topology/signal completa;
- autenticação read-only, TLS, outcomes, freshness e erros canônicos comprovados;
- carga, endurance, restart, falha, recovery e operação offline aprovados;
- credenciais sintéticas ou de laboratório mantidas por referência;
- zero comando administrativo, SQL livre, alteração de banco ou mutação de infraestrutura;
- todas as combinações não testadas permanecem sem suporte.

Parada imediata: download não autorizado, credencial real não aprovada, alteração do banco,
ambiente não isolado ou resultado fora da célula.

### O5-R9 — Rehearsal integrado de ativação e incidente

Dependências: O5-R2–R8 aceitos.

Objetivo: ensaiar a composição completa em ambiente local isolado sem alterar o estado canônico do
produto ou o lifecycle.

Critérios mensuráveis:

- entrada e saída bounded do modo simulado;
- kill switch, rollback, restart, offline, saturation e incident drill aprovados;
- zero recomendação, comando, automação ou efeito externo;
- UI preserva fonte, freshness, incerteza e limites;
- desativação não altera observações canônicas;
- cleanup integral e evidência sanitizada.

Parada imediata: qualquer efeito externo, estado residual, publicação de contexto antigo ou
necessidade de transição real.

### O5-R10 — Repetição do Quality Gate

Dependência: todos os lotes anteriores com decisões próprias.

Objetivo: repetir o O5 contra as evidências finais, sem corrigir durante a auditoria.

Critério de conclusão: todos os doze requisitos objetivos do O5 classificados. Qualquer
`BLOQUEADO`, `REPROVADO` ou requisito obrigatório `NÃO TESTADO` mantém
`ActivationState=None`.

Somente um O5 automático `APROVADO` permitirá apresentar um Human Gate O5. Mesmo um Human Gate
aprovado não executará a transição; `None → Observer` exigirá autorização final separada.

## Dependências resumidas

```text
O5-R1
  ├─> O5-R2 ─> O5-R3 ─> O5-R4 ─> O5-R5
  └─────────────────────> O5-R6 ─> O5-R7
                 O5-R4/R5/R7 ─────> O5-R8
                 O5-R2...R8 ──────> O5-R9 ─> O5-R10
```

O5-R6 também depende de autorização específica da fonte representativa. O5-R8 depende de
autorização específica para laboratório/provider/banco real. Nenhuma dessas autoridades é
concedida por este plano.

## Riscos principais

| Risco | Controle planeado |
|---|---|
| Chamar corpus de laboratório de “produção” | declarar população e escopo; `ProductionRepresentative` somente quando comprovado para essa população |
| Overfitting ou leakage | partições imutáveis, freeze pré-holdout e holdout one-use |
| Ativação acidental | default `None`, opt-in exato, zero auto-promotion e gates de composição |
| Kill switch tardio ou incompleto | deadline, cancellation, fencing e prova sob carga máxima |
| SLO inventado a partir do sandbox | números aprovados antes das medições e vinculados ao ambiente |
| Suporte geral inferido de PostgreSQL | homologação por célula; demais linhas continuam `None`/`No` |
| Diagnóstico expor dado sensível | códigos bounded, cardinalidade e secret canaries |
| Correção durante Quality Gate | auditoria somente leitura; falha gera lote separado |

## Proposta concisa do primeiro lote

### O5-R1 — Escopo piloto e decisão de governança de dados

Proposta futura, ainda **não autorizada**:

> AUTORIZO EXCLUSIVAMENTE A ELABORAÇÃO DOCUMENTAL LOCAL DO O5-R1 — ESCOPO PILOTO E DECISÃO DE GOVERNANÇA DE DADOS PARA O OBSERVER, sobre a baseline então atual, sem implementação, runtime, dado real ou transição. Autorizo definir uma única célula provider/version/platform/topology/signal, avaliar PostgreSQL como candidato sem declará-lo escolhido ou homologado, nomear owners, fontes permitidas, autoridade, classificação, minimização, retenção, representatividade, labels, partições, métricas, processo de thresholds, ambiente HM-01–03 e exclusões. Proíbo acesso a corpus/provider/banco real, código, configuração, LLM, recomendação, comando, automação, push, deploy e ativação. A conclusão exigirá matriz integral, critérios mensuráveis, riscos, decisões pendentes e proposta do O5-R2; `ActivationState=None` permanecerá inalterado.

## Disposição

- Plano: `CONCLUÍDO` documentalmente.
- Lotes O5-R1–O5-R10: não autorizados.
- Dados, provider e banco reais: não acessados.
- Código e configuração: inalterados.
- `ActivationState`: `None`.
- Human Gate O5: bloqueado.
- Transição `None → Observer`: bloqueada.
