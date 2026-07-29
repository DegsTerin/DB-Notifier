# Proposta reconciliada de revalidação — STATE-06 para STATE-07

> **PROPOSTA PREPARATÓRIA. ELEGIBILIDADE NÃO REAVALIADA. REVALIDAÇÃO
> TÉCNICA, HUMAN GATE E TRANSIÇÃO NÃO EXECUTADOS NEM AUTORIZADOS.**

## Status e autoridade

- Data: 2026-07-29.
- Estado mantido: `STATE-06 INTEGRATION`.
- Estado futuro considerado: `STATE-07 TESTING_HOMOLOGATION`.
- Árvore executável examinada: commit
  `9512dc1de15619eadd9d2e8e6b5476bb77a13abd`, branch `main`, worktree limpa
  no início deste lote.
- Baseline do Human Gate final histórico: commit
  `1a27dca393f00bc683235d7f8898dcc86f5841e0`.
- Registro factual daquele Human Gate: commit
  `96cf2488679c2b8b2abcccf8d6473d07c8c8d823`.
- Human Gate histórico: `APROVADO COM RESSALVAS` somente na baseline então
  revista.
- Situação corrente de elegibilidade: `NÃO REAVALIADA`.
- Transição `STATE-06 → STATE-07`: `NÃO AUTORIZADA` e `NÃO EXECUTADA`.
- MOD-12: `ActivationState=None`; nenhum modo foi ativado.
- JOSE-0: aceito somente como preparação documental; ADR-0008 permanece
  `proposed`.

O proprietário autorizou exclusivamente a reconciliação documental do estado
factual com a árvore `9512dc1`, o inventário das mudanças posteriores aos
gates e a elaboração desta proposta com uma matriz proporcional. Não autorizou
build, testes, runtime, downloads, acesso externo, mudança de código ou
dependências, `JOSE-1`, `D9`, ativação do MOD-12 ou transição de lifecycle.

O commit que entregar este lote documental será descendente de `9512dc1`.
Ele somente poderá ser tratado como uma futura baseline administrativa se o
seu delta contra `9512dc1` estiver restrito a estes quatro caminhos:

1. `docs/STATE-06-To-STATE-07-Transition-Revalidation-Proposal.md`;
2. `docs/README.md`;
3. `prompts/state/Current-State.md`;
4. `prompts/state/State-Transition-Log.md`.

Qualquer outro caminho, mudança concorrente ou delta técnico exige novo
inventário antes de uma futura revalidação.

## Relação com a proposta histórica

A
[proposta de transição de 2026-07-20](STATE-06-To-STATE-07-Transition-Proposal.md)
continua `INVALIDADA PARA EXECUÇÃO`. Ela não foi modificada, reabilitada ou
substituída como autoridade.

Este documento não revive, incorpora por referência, prolonga nem substitui
como autoridade a proposta histórica invalidada. As evidências históricas
permanecem válidas somente nas respetivas baselines e não recebem classificação
corrente antes da revalidação separadamente autorizada. Copiar, parafrasear ou
responder ao antigo texto revogado não autoriza transição.

Da mesma forma:

- o
  [Human Gate final de 2026-07-20](STATE-06-Final-Human-Gate-Report.md)
  permanece um facto histórico válido e imutável na baseline `1a27dca`;
- os gates posteriores continuam válidos somente nos seus lotes e escopos
  proprietários;
- a soma de gates locais não constitui automaticamente um Quality Gate de
  lifecycle na baseline corrente;
- um futuro adendo de revalidação não reabre, apaga ou substitui a decisão
  histórica.

## Baselines e proveniência

| Âncora | Papel histórico | Relação observada com `9512dc1` |
|---|---|---|
| `84217c64312a024ec4f286adfe4872184a21849c` | baseline da campanha automática consolidada do STATE-06 | ancestral |
| `2c1e05fd8ad4dec2174682fee66aafbd92efc6ee` | registro e aceite daquela campanha automática | ancestral |
| `1a27dca393f00bc683235d7f8898dcc86f5841e0` | baseline examinada pelo Human Gate final | ancestral |
| `96cf2488679c2b8b2abcccf8d6473d07c8c8d823` | registro factual do Human Gate final | ancestral |
| `ff0adc76166d82d01542aa091e15dd39e7ff3fa1` | baseline técnica da reconciliação anterior | ancestral |
| `3c13d57fd71e8c51469cf29bf70b3ac2d4de6002` | invalidação executiva da proposta histórica na baseline `ff0adc7` | ancestral |
| `9512dc1de15619eadd9d2e8e6b5476bb77a13abd` | baseline técnica pré-lote aqui examinada | alvo do inventário |

A ancestralidade preserva proveniência; ela não prova que os resultados
antigos continuam válidos depois das mudanças.

## Método e limites do inventário

O inventário usou somente leitura local de Git e texto:

- `git status --short --branch`;
- `git rev-parse` e `git show`;
- `git merge-base --is-ancestor`;
- `git rev-list --count` e contagem de merges;
- `git log`, `git diff --name-only`, `--name-status`, `--numstat` e
  `--shortstat`;
- classificação de caminhos e leitura dos relatórios proprietários.

Os eixos funcionais abaixo podem sobrepor-se: um mesmo commit pode alterar
produto, testes e documentação. As coortes `85` commits somente documentais e
`58` commits com algum caminho técnico ou executável são exclusivas entre si e
somam os `143` commits posteriores à baseline examinada pelo Human Gate.

Nenhum resultado deste inventário é um resultado de build, teste, runtime,
homologação ou revalidação técnica.

## Inventário quantitativo posterior aos gates

| Intervalo até `9512dc1` | Commits | Caminhos únicos | Inserções | Remoções |
|---|---:|---:|---:|---:|
| baseline automática `84217c6..9512dc1` | 155 | 451 | 95.398 | 2.958 |
| aceite automático `2c1e05f..9512dc1` | 154 | 450 | 95.137 | 2.957 |
| baseline do Human Gate `1a27dca..9512dc1` | 143 | 440 | 90.922 | 2.993 |
| registro do Human Gate `96cf248..9512dc1` | 142 | 439 | 90.785 | 2.994 |
| reconciliação anterior `ff0adc7..9512dc1` | 24 | 41 | 11.507 | 50 |

No intervalo principal `1a27dca..9512dc1`, os `440` caminhos incluem:

- `126` em `src/`;
- `127` em `tests/`;
- `25` em `scripts/`;
- `132` em `docs/`;
- `12` em `prompts/`;
- `18` distribuídos entre raiz, CI, build, configuração, assets,
  localização e aplicações legadas.

A classificação funcional sobreposta encontrou:

| Eixo | Commits relacionados | Caminhos relacionados | Churn |
|---|---:|---:|---:|
| produto/runtime | 29 | 133 | +18.876 / -1.880 |
| testes, harnesses e tooling | 56 | 152 | +48.600 / -717 |
| SDK, dependências e build | 9 | 13 | +802 / -140 |
| documentação e governança | 138 | 147 | +23.306 / -272 |
| MOD-12/AIOps | 68 | 83 | +12.797 / -143 |

Três mudanças de schema Server foram acrescentadas depois da baseline do
Human Gate:

1. `AddExplicitAlertRouting`;
2. `AddDurableDeliveryOwnership`;
3. `AddRejectedObservationSequenceLedger`.

Elas acrescentaram seis arquivos de migration e alteraram o model snapshot. A
árvore `9512dc1` contém `15` migrations: seis Agent/SQLite e nove
Server/PostgreSQL. Nenhuma delas foi aplicada por este lote ou está apresentada
como aplicada a PostgreSQL operacional.

O intervalo `ff0adc7..9512dc1` merece distinção própria: não contém mudança em
`src/`, migration ou package/lockfile. Contudo, contém `11` caminhos C#
test-only, mudança de `global.json` para o SDK .NET `10.0.302`, evolução dos
harnesses D6–D8, JOSE-0 e mudanças documentais/de governança. Portanto, a
ausência de código-fonte de produto nesse intervalo não restaura a
elegibilidade invalidada.

## Inventário qualitativo das mudanças

| Família | Mudança observada depois do Human Gate | Verdade factual preservada |
|---|---|---|
| R0–R8 | hardening de auditoria, notificação durável, comando fail-closed, Agent Fleet, routing, delivery ownership, migrador/supply chain, Web/WPF/acessibilidade, fundação inativa do MOD-12 e reauditoria consolidada | gates valem somente nos respetivos lotes; ressalvas e quatro achados R8 `CONTIDO` permanecem |
| Persistência e ingestão | três migrations Server, routing explícito, ownership durável, ledger de sequências recusadas e linearização com revogação | nenhum PostgreSQL operacional foi migrado; rollback, corte histórico e concorrência mantêm limites próprios |
| R-SEQ | resolução fail-closed de gaps recusados e ledger durável por Agent | aprovado apenas no escopo local; não constitui suporte operacional |
| R-EGRESS/R-FENCE | ordenação entre revogação e commit, fence por identidade e campanha PostgreSQL multiprocesso | não prova fairness, prioridade, starvation freedom, SLO ou operação |
| R-NET | policies de egress por consumidor, DNS revalidation, IP pinning, TLS hostname e laboratório local DNS/PKI/IdP/PostgreSQL | trust produtivo continua `System`; infraestrutura e trust operacionais não foram homologados |
| Agent, tooling e supply chain | validação de opções antes do startup, runner local endurecido, fixtures de auditoria, remediação do lockfile e atualização do SDK | locked restore, freshness online de advisories e CI remota continuam sem nova prova consolidada |
| Web/WPF e identidade visual | responsividade, navegação, High Contrast, amostras visuais, registry de providers e nome visual | condições físicas R6 ainda limitadas; aparência não implica capability ou suporte |
| MOD-12 O1–O4 | sandboxes locais de trust, observações, pipeline, holdout, projeção/API/UI e control plane dormente | `ActivationState=None`; composição normal não possui pipeline operacional, LLM, recomendação ou automação |
| MOD-12 O5/PF-OBS-1 | campanhas, metodologia e diagnósticos D1–D8, incluindo D8 `EARLY_GATE_INTERMITTENT` | O5 e PF-OBS-1 continuam sem aprovação; D8 não autoriza D9, O5 ou ativação |
| JOSE-0 | proposta, ADR-0008, profiles, threat/requirement maps e cobertura IANA `318/318` exclusivamente documentais | ADR/profile/caps continuam provisórios, `NotImplemented`, `RuntimeDisabled` e `NotAdvertised`; `JOSE-1` não autorizado |
| Governança e estado | consolidação do corpus, novas fronteiras de autoridade, registro de gates e aceite JOSE-0 | nenhuma mudança documental concede transição, runtime ou suporte |

Os commits técnicos de referência incluem, sem pretender substituir os
relatórios proprietários:

- `878103d`, `f188145`, `4752868`, `a054ef0`, `0ff89c0` e `004f9e5` para
  R1–R5;
- `878a7ea` a `eb14985` e `40bf9f5` para R6 Web/WPF;
- `cf8ee87` para a fundação inativa R7;
- `ab60f43` até `413f6ec` para a progressão local O1–D5;
- `c8d322e`, `3ba282f`, `980e890`, `3988d3a`, `e23bb51`, `3ed15fc` e
  `bb5a63a` para R-SEQ, R-EGRESS, R-FENCE e R-NET;
- `ff0adc7` e `7793f1e` para lockfile e SDK;
- `fd320bf` até `46746c7` para D6–D8;
- `ef6b28a`, `6d0bbdc`, `7cbcd0c` e `9512dc1` para JOSE.

## Ressalvas que uma futura decisão deve transportar

Uma futura revalidação ou decisão não poderá omitir:

1. a evidência de integração histórica era local, sintética e de sandbox;
2. nenhum provider, engine, versão, plataforma ou topologia possui
   homologação operacional; PostgreSQL permanece `Homologation=None` e
   suporte público `No`;
3. nenhuma migration foi aplicada a PostgreSQL existente ou operacional;
4. PKI, IdP, vault, rotação, identidade, credenciais e trust operacionais não
   foram exercitados;
5. monitorização, sincronização, delivery e sandboxes continuam desabilitados
   ou fail-closed na composição normal;
6. Start, Stop e Restart continuam `Unsupported`; não há executor,
   `CommandAttempt` ou post-probe operacional;
7. SignalR permanece apenas hint; API e reconciliação periódica são
   autoritativas;
8. scaling físico Windows a 200%, mixed-DPI, leitor de ecrã e as demais
   condições físicas R6 mantêm as limitações registradas;
9. locked restore, freshness online de advisories e CI remota não receberam
   nova prova consolidada;
10. o incidente de autoridade R5 e o restore bloqueado fora da autoridade em
    D4 permanecem históricos e não reclassificados;
11. R-SEQ não reconstrói posições históricas; R-EGRESS/R-FENCE não provam
    fairness/SLO; R-NET não homologa infraestrutura operacional;
12. quatro achados R8 permanecem `CONTIDO`, não apagados;
13. MOD-12 permanece inativo, O5/PF-OBS-1 permanecem sem aprovação e não há
    AIOps/IA operacional completa;
14. JOSE-0 permanece desenho preparatório, sem `JOSE-1`, JWE, emissão JWS,
    key lifecycle ou custódia operacional;
15. carga representativa, endurance, HA, disaster recovery, produção, deploy,
    publicação e release continuam sem autorização ou prova.

## Regra de reutilização proporcional

Evidência histórica poderá servir como proveniência e como referência para
selecionar casos. Ela somente poderá ser carregada para um relatório corrente
quando, cumulativamente:

1. o código, contrato, configuração, SDK e dependências que governam o
   comportamento não tiverem mudança material desde a evidência;
2. a cadeia Git e a integridade do artefacto estiverem confirmadas;
3. a suíte de regressão corrente que cobre o comportamento passar;
4. nenhuma mudança adjacente alterar semântica, segurança, timing, persistência
   ou ambiente;
5. a limitação original continuar explícita;
6. o relatório corrente justificar a reutilização item a item.

Se qualquer condição falhar ou permanecer indeterminada, a evidência deve ser
repetida sob autoridade própria ou classificada `BLOQUEADA`; nunca
`APROVADA` por inferência.

## Matriz proposta de revalidação proporcional

Todos os itens estão `PENDENTES DE AUTORIZAÇÃO`. A coluna “tratamento” define
o que deverá constar numa futura autoridade e não registra execução neste
lote.

| ID | Escopo afetado | Evidência futura mínima | Tratamento |
|---|---|---|---|
| `RV-01` | baseline, shutdown, worktree e cadeia Git | preflight completo; commit exato; worktree limpa; ancestralidade; inventário de todo delta contra `9512dc1`; whitelist do lote documental | **obrigatório**; divergência ou resíduo bloqueia |
| `RV-02` | .NET `10.0.302`, CI, locks, dependências e supply chain | SDK real; restore locked autorizado; auditoria de dependências; build Release; format/analyzers; freshness online e CI classificadas separadamente | **obrigatório**; download, rede ou CI exigem autoridade externa adicional |
| `RV-03` | 19 projetos .NET, produto e boundaries | suítes unitárias, arquitetura, integração, WPF/legado e regressão completas; cobertura mínima `70%` linhas e `45%` branches, sem redução de floor local | **obrigatório**; falha ou cobertura inferior bloqueia |
| `RV-04` | Dashboard, WPF, Tray, Design System e registry visual | typecheck, testes e build Web; tokens/branding/registry; TV 30 s sem overlap; SignalR hint; WPF/Tray e acessibilidade automatizada | **obrigatório**; condições físicas indisponíveis tornam-se ressalvas explícitas, não aprovação |
| `RV-05` | integração STATE-06 transversal | E2E correlacionado: reconnect, replay, dedup, reorder, revogação, fencing, stale/unknown, API autoritativa, notificação test-only, comando não executável e cleanup | **obrigatório**; nenhum provider ou serviço operacional |
| `RV-06` | R0–R8 e quatro containments R8 | reauditoria finding-by-finding; confirmar que `H03`, `H04`, `H13` e `M23` continuam fail-closed/contidos; preservar incidentes e ressalvas | **obrigatório** |
| `RV-07` | 15 migrations, R4 e R-SEQ | cadeia/model drift, up/down/rollback, routing, ownership e ledger; laboratório PostgreSQL descartável somente se autorizado | **obrigatório**; PostgreSQL físico é **condicional** e nunca operacional |
| `RV-08` | R-EGRESS/R-FENCE | regressão de ordenação e isolamento; campanha multiprocesso bounded somente se autorizada; cleanup | regressão **obrigatória**; laboratório físico **condicional**; fairness/SLO continuam ressalva |
| `RV-09` | R-NET e trust boundaries | policies, DNS revalidation, IP pinning, TLS hostname e testes negativos; fixture local DNS/PKI/IdP/PostgreSQL somente se autorizada | regressão **obrigatória**; infraestrutura real e trust operacional excluídos |
| `RV-10` | MOD-12 | provar composição normal dormente, isolamento test-only, fail-closed e `ActivationState=None`; repetir apenas regressões determinísticas pertinentes | **obrigatório e limitado**; D6–D8 físicos, `D9`, O5 e ativação ficam excluídos |
| `RV-11` | JOSE-0 e ADR-0008 | integridade documental, cobertura `318/318`, links, requisitos/threats, status `proposed` e ausência de runtime | **obrigatório documental**; `JOSE-1`, `JOSE-D1`, chaves e runtime excluídos |
| `RV-12` | documentação, segurança e legado | links; code-doc en-GB; secret e real-host scan; clean-room/licença; estado; diff e staged diff | **obrigatório** |
| `RV-13` | encerramento e resíduos | zero processo, listener, browser dedicado, root temporário, container ou artefacto não autorizado; relatório consolidado sanitizado | **obrigatório** |
| `RV-H01` | decisão humana da baseline corrente | relatório automático corrente; repetição dos seis cenários humanos equivalentes com IDs novos; ressalvas e cobertura pendente apresentadas num único resumo | **obrigatório depois de `RV-01`–`RV-13`**; decisão humana separada |

### Novos IDs das amostras humanas

Os IDs históricos `S06-HG-001` a `S06-HG-006` permanecem imutáveis. Uma
campanha futura deve usar:

| Novo ID | Cenário equivalente a repetir na baseline corrente |
|---|---|
| `S06-RV-HG-001` | Agent disponível, perda, pendência, recuperação e replay único |
| `S06-RV-HG-002` | leitura TV inicial, hint, reconciliação de 30 s, falha e recuperação |
| `S06-RV-HG-003` | uma notificação Windows sintética, factual e autorizada |
| `S06-RV-HG-004` | supressão de duplicata sem segunda publicação |
| `S06-RV-HG-005` | comando sintético recusado, journals e zero execução |
| `S06-RV-HG-006` | `unknown` corrente distinto de `stale`, com origem e suporte factuais |

Essa campanha somente poderá começar depois de um relatório automático
consolidado corrente e de autoridade específica para as amostras. O resultado
deverá integrar um **Human Gate de revalidação da baseline corrente do
STATE-06**, como adendo independente. Ele não reabre nem substitui o Human
Gate final de 2026-07-20.

## Itens independentes da transição

Os itens abaixo não são pré-requisitos ocultos para mudar de fase. Continuam
com gates e autoridades próprios:

- nova campanha física PF-OBS-1;
- D6, D7, D7-R1 ou D8 físicos;
- `D9`;
- Quality/Human Gate O5;
- ativação `None → Observer` ou de qualquer modo MOD-12;
- `JOSE-1`, `JOSE-D1` ou implementação JOSE;
- provider operacional, IdP, PKI, vault ou credencial real;
- comando administrativo, deploy, publicação, produção ou release.

A futura revalidação deve confirmar que esses itens continuam inativos,
isolados ou não implementados; não deve executá-los para criar elegibilidade
artificial.

## Condições de parada

Uma futura atividade deve parar sem promover estado quando ocorrer qualquer
uma destas condições:

1. baseline divergente, worktree suja não explicada ou mudança técnica
   concorrente;
2. shutdown ou cleanup não comprovado;
3. delta não classificado ou caminho fora da whitelist;
4. necessidade de download, rede, database, provider, credencial ou serviço
   fora da autoridade;
5. secret, material sensível ou identificador real de host;
6. P0/P1 aberto, advisory conhecido aplicável ou gate obrigatório falho;
7. cobertura abaixo dos pisos ou floor de componente reduzido;
8. runtime normal, comando administrativo, Observer ou JOSE ativado;
9. aplicação de migration a banco existente ou operacional;
10. amostra humana obrigatória ausente, incompleta ou contraditória;
11. decisão humana ambígua ou sem resumo informado de um único estado;
12. resíduo de processo, listener, browser, container ou artefacto.

P2 residual somente poderá prosseguir se for remediado ou formalmente aceito
com owner, impacto, prazo/condição e ressalva explícita no novo Human Gate.

## Critérios futuros de elegibilidade

A baseline corrente somente poderá ser apresentada para uma decisão de
transição quando:

1. `RV-01` a `RV-13` tiverem classificação factual num relatório automático
   consolidado;
2. itens obrigatórios estiverem aprovados e itens condicionais estiverem
   aprovados, formalmente bloqueados ou excluídos com justificação;
3. `S06-RV-HG-001` a `S06-RV-HG-006` tiverem decisões individuais;
4. o Human Gate de revalidação tiver decisão informada e registrada;
5. todas as ressalvas herdadas e novas estiverem no handoff;
6. não houver transição implícita, ativação MOD-12 ou claim de homologação;
7. uma decisão posterior e inequívoca autorizar apenas a transição documental
   `STATE-06 → STATE-07`.

Mesmo nesse cenário, a transição não homologa provider, engine, plataforma,
topologia ou operação.

## Sequência de autoridades futuras

Cada passo para ao concluir e não concede o seguinte:

1. **Aceitar ou devolver esta proposta documental.**
2. **Autorizar a revalidação técnica automática**, com baseline, comandos,
   runtime local, downloads/rede e laboratórios delimitados.
3. **Autorizar as seis amostras humanas novas**, somente se o relatório
   automático permitir.
4. **Decidir o Human Gate de revalidação do STATE-06**, depois do resumo
   informado.
5. **Autorizar a transição documental `STATE-06 → STATE-07`**, em decisão
   separada.
6. **Já em STATE-07, autorizar uma proposta de plano mestre de homologação.**

Identificar o passo seguinte não o autoriza.

## Primeira atividade futura do STATE-07

Se a transição vier a ser autorizada e registrada, a primeira atividade segura
deve ser outra proposta documental: um plano mestre de homologação
provider-neutral, com matriz incremental por engine, versão, plataforma,
topologia, papel, transporte e capability.

Esse plano deve selecionar alvos exatos, recursos, credenciais de teste,
limites de máquina, riscos, custos, segurança, carga, falha, recuperação,
acessibilidade, cleanup, rollback, Quality Gate e Human Gate. Elaborar ou
executar esse plano requer autoridade própria.

## Efeito e rollback deste lote

Este lote altera somente documentação. Não existe binário, dependência,
configuração executável, schema ou runtime a restaurar.

Se uma correção for necessária, ela deve ser feita por novo commit documental
focado e por entrada append-only no histórico quando houver novo facto.
Relatórios e decisões históricas não devem ser reescritos. Uma eventual
transição futura também exigirá decisão formal própria; não pode ser revertida
ou inferida por simples edição oportunista.

## Fora de escopo

Este lote não autoriza nem executa:

- build, testes, restore, downloads ou mudança de dependência;
- Agent, API, Dashboard, WPF, Tray, browser, provider, database ou container;
- acesso externo, rede remota, cloud, CI remota ou registry;
- source, configuração executável, migration ou schema;
- JOSE-1, JOSE-D1, JWS/JWE novo, key lifecycle, IdP, PKI, vault/KMS/HSM;
- D9, nova campanha PF-OBS-1, O5 ou ativação MOD-12;
- comando administrativo, `CommandAttempt`, executor ou post-probe;
- deploy, publicação, instalação, produção, release ou suporte público;
- transição de lifecycle.

## Resultado documental

Esta proposta:

- reconcilia a verdade factual com a árvore executável `9512dc1`;
- inventaria as mudanças posteriores aos gates sem declará-las revalidadas;
- define uma matriz proporcional e condições de parada;
- preserva a proposta antiga como evidência inválida para execução;
- mantém `STATE-06 INTEGRATION`, `ActivationState=None` e ADR-0008
  `proposed`;
- exige decisões separadas para revalidação técnica, amostras humanas, Human
  Gate, transição e trabalho futuro de STATE-07.

Os validators de documentação, secret scan, build, testes e runtime não foram
executados porque não integraram a autoridade deste lote. Foram autorizadas
somente inspeções Git/textuais e higiene do diff documental.
