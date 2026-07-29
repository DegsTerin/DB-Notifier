# Estado Atual

Este documento é o snapshot factual vigente do workspace em 2026-07-29. Ele
não concede autoridade de execução. A evolução, os resultados substituídos e
as decisões tomadas no seu contexto original permanecem no
[`State-Transition-Log.md`](State-Transition-Log.md) e nos relatórios
proprietários.

## Lifecycle, gates e ativação

- Posição do workspace: `STATE-06 INTEGRATION`.
- Nenhuma transição para `STATE-07 TESTING_HOMOLOGATION` foi executada ou
  autorizada.
- A cadeia anterior permanece válida: `STATE-00` e `STATE-04` foram
  retrospectivamente `APROVADOS`; `STATE-01` a `STATE-03`, `APROVADOS COM
  RESSALVAS`; e o Human Gate de `STATE-05`, `APROVADO` por decisão
  substitutiva. As fontes proprietárias são a
  [ratificação retrospectiva](../../docs/Human-Gate-Retrospective-Ratification.md)
  e o
  [Human Gate de `STATE-05`](../../docs/STATE-05-Human-Gate-Validation.md).
- O resultado registrado do Human Gate final de `STATE-06` permanece
  `APROVADO COM RESSALVAS`, limitado à baseline revista em 2026-07-20. Os
  incrementos posteriores possuem gates próprios, não alteram o lifecycle por
  si mesmos e não autorizam inferir prontidão para transição. A evidência
  proprietária é o
  [relatório do Human Gate](../../docs/STATE-06-Final-Human-Gate-Report.md).
- O
  [ADR-0007](../../docs/architecture/ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md)
  está `accepted` somente como decisão arquitetural. A
  [proposta histórica `STATE-06 → STATE-07`](../../docs/STATE-06-To-STATE-07-Transition-Proposal.md)
  permanece `INVALIDADA PARA EXECUÇÃO`. A
  [nova proposta reconciliada](../../docs/STATE-06-To-STATE-07-Transition-Revalidation-Proposal.md)
  foi revisada para incorporar a diretriz estratégica de concluir os escopos
  de integração `STATE-06` de MOD-12 e JOSE antes da revalidação consolidada
  de saída. A delimitação detalhada `PC-M12`/`PC-JOSE` foi aceita em
  2026-07-29 exclusivamente como diretriz documental de fechamento da
  integração `STATE-06`. A proposta continua não autorizante, com todos os
  itens técnicos pendentes de autoridades próprias e elegibilidade `NÃO
  REAVALIADA`.
- O estado de ativação de MOD-12 é `ActivationState=None`. `OBSERVER`,
  `ADVISOR`, `ASSISTANT` e `CONTROLLED_AUTOMATION` permanecem inativos.
- O gate de ativação `None → Observer` e uma transição de lifecycle são
  decisões independentes. Nenhuma delas pode ser inferida da outra.
- A pré-condição estratégica não exige AIOps ou JOSE operacionalmente
  completos antes de `STATE-07`: exige somente os escopos integrados,
  bounded, verificáveis e fail-closed pertencentes ao `STATE-06`. MOD-12 O5,
  homologação JOSE, ativação, suporte público e release conservam fases e
  decisões próprias.

## Baseline técnica

- A última árvore executável inventariada é
  `9512dc1de15619eadd9d2e8e6b5476bb77a13abd`, de 2026-07-28. Ela estava na
  branch `main`, com worktree limpa, e contém como ancestrais `84217c6`,
  `2c1e05f`, `1a27dca`, `96cf248`, `ff0adc7` e `3c13d57`. A baseline
  administrativa da primeira reconciliação é
  `04db6594e192dec822fbd326c792eec4f3a37714`, descendente direto que alterou
  somente os quatro documentos da reconciliação. A revisão estratégica
  aceita somente como diretriz documental está em
  `db62377e5d762e7ee334e7bf0e7309a0ab5c4b9b`, descendente documental direto
  de `04db659`. Nenhuma dessas referências constitui a futura baseline técnica
  depois dos fechamentos MOD-12/JOSE.
- Entre a baseline examinada pelo Human Gate final `1a27dca` e `9512dc1`
  existem `143` commits, `440` caminhos alterados, `90.922` inserções e
  `2.993` remoções. O intervalo inclui `126` caminhos em `src/`, `127` em
  `tests/`, `25` em `scripts/`, três mudanças de schema Server, lotes R0–R8,
  R-SEQ/R-EGRESS/R-FENCE/R-NET, MOD-12 O1–D8, tooling, SDK, dependências e
  governança. Os gates próprios desses lotes não se agregam automaticamente
  como um novo gate de lifecycle.
- Entre a reconciliação anterior `ff0adc7` e `9512dc1` existem `24` commits e
  `41` caminhos, sem mudança em `src/`, migration ou package/lockfile. O
  intervalo contém, porém, `11` caminhos C# test-only, atualização de
  `global.json` para .NET SDK `10.0.302`, evolução dos harnesses D6–D8,
  JOSE-0 e mudanças de governança. Essa ausência focal de source não restaura
  a elegibilidade invalidada.
- A solução contém 19 projetos .NET 10, com targets `net10.0` ou
  `net10.0-windows10.0.22621.0`; os hosts de sandbox em `tests/` não pertencem
  à composição normal. O Dashboard usa React e TypeScript; o cliente Windows
  usa WPF.
- Domain e Application permanecem provider-neutral. Provider SDK,
  infraestrutura, persistência, Agent, API, Desktop, Dashboard e testes
  conservam fronteiras próprias e dependências voltadas para dentro.
- Agent usa SQLite somente para estado local autorizado; Server usa PostgreSQL
  somente para persistência central. As 15 migrations atuais são seis do Agent
  SQLite e nove do Server PostgreSQL. Nenhuma migration está aplicada a
  PostgreSQL existente ou operacional.
- O provider PostgreSQL implementa endpoint tipado, discovery de
  `pg_isready`, readiness, fallback TCP degradado, probe Npgsql autenticado e
  normalização canônica. PostgreSQL permanece `Homologation=None` e suporte
  público `No`, conforme a
  [matriz de capabilities](../../docs/architecture/Provider-Capability-Matrix.md).
- O ConfigMigrator permanece em .NET 10, com dry-run, rejeição de secrets e
  campos desconhecidos, backup, journal durável, recuperação determinística,
  rollback e revalidação.
- Os lotes R0, R0-F1, R1, R2-A, R3, R4-A, R4-B, R5, R6, R7-A0 e
  R8 permanecem encerrados somente nos respetivos escopos locais. R5 está
  tecnicamente aprovado, mas sua conformidade com a autoridade original
  permanece `REPROVADA` pelo incidente NuGet não reclassificado. Os Human
  Gates R6 e R8 permanecem `APROVADOS COM RESSALVAS`: as cinco condições
  físicas R6, locked restore, freshness online de advisories, CI remota e a
  repetição PostgreSQL própria de R8 continuam não testados naquele escopo;
  quatro achados R8 permanecem classificados como `CONTIDO`. Laboratórios
  focais posteriores não substituem essas evidências, e os resultados não
  constituem homologação, runtime operacional ou autorização externa. As
  fontes são os relatórios
  [R5](../../docs/STATE-06-Audit-Remediation-R5-Report.md),
  [Human Gate R6](../../docs/STATE-06-Audit-Remediation-R6-Human-Gate-Report.md),
  [R8](../../docs/STATE-06-Audit-Remediation-R8-Report.md) e
  [Human Gate R8](../../docs/STATE-06-Audit-Remediation-R8-Human-Gate-Report.md).
- R-SEQ está automaticamente `APROVADO` somente no escopo local validado. A
  nona migration Server estabelece um corte inclusivo por Agent e o ledger
  `rejected_observation_sequences`, sem criar amostra ou efeito de saúde para
  a rejeição consumida. Posições históricas não são reconstruídas; evidência
  pós-corte ausente ou contraditória falha fechada como retryable, e o Agent
  somente reconhece a rejeição quando o high-water autoritativo cobre sua
  sequência. O laboratório PostgreSQL descartável passou, mas nenhuma
  migration foi aplicada a PostgreSQL existente ou operacional. As fontes são
  o
  [relatório R-SEQ original](../../docs/STATE-06-R-SEQ-Rejected-Observation-Sequence-Remediation-Report.md)
  e o
  [relatório do ledger durável](../../docs/STATE-06-R-SEQ-Durable-Rejection-Ledger-Report.md).
- R-EGRESS foi aceito somente para a corrida entre leitura `Active`, revogação
  principal e commit da ingestão; R-FENCE está automaticamente `APROVADO`
  somente como regressão local do mesmo gate por Agent. A observação que perde
  essa ordem não cria efeito próprio, embora R-SEQ ainda possa projetar uma
  sucessora aceita antes da revogação e retida atrás do gap recusado. A
  campanha PostgreSQL descartável multiprocesso passou, mas não concede
  fairness, prioridade, starvation freedom, timeout próprio, SLO, homologação
  ou suporte operacional. R-EGRESS não designa política de network egress;
  essa fronteira pertence a R-NET. As fontes são os relatórios
  [R-EGRESS](../../docs/STATE-06-R-EGRESS-Observation-Ingestion-Revocation-Linearisation-Report.md),
  [R-FENCE](../../docs/STATE-06-R-FENCE-Agent-Identity-Fence-Lifecycle-Regression-Report.md)
  e da
  [campanha física](../../docs/STATE-06-R-EGRESS-R-FENCE-PostgreSql-Multiprocess-Load-Report.md).
- R-NET está automaticamente `APROVADO` somente no escopo local validado.
  Quatro políticas positivas por consumidor exigem CIDR e porta exatos,
  recusam atomicamente respostas DNS proibidas, conectam somente a IP aprovado
  preservando o hostname TLS e mantêm o legado PowerShell estritamente
  loopback. A campanha local controlada de DNS, PKI, IdP e PostgreSQL TLS
  passou; os construtores produtivos permanecem em trust `System` e o trust
  sintético continua estritamente test-only. DNS/PKI/IdP/PostgreSQL
  operacionais, DNSSEC, trust provisionado, proxy, failover, HA,
  cross-platform, desempenho, provider homologado e suporte público continuam
  não homologados. As fontes são o
  [relatório determinístico R-NET](../../docs/STATE-06-R-NET-Network-Egress-Remediation-Report.md)
  e o
  [relatório da campanha física](../../docs/STATE-06-R-NET-Local-DNS-PKI-IdP-PostgreSql-TLS-Homologation-Report.md).

## Composição normal e limites operacionais

- Monitoring e sincronização continuam desabilitados por padrão. Agent Fleet,
  command polling e notification delivery normais permanecem indisponíveis ou
  recusam startup quando uma configuração incompleta tenta habilitá-los.
- As rotas normais de comando são tombstones fail-closed. Não existe
  `CommandAttempt`, executor administrativo, post-probe ou fallback de
  execução. Start, Stop e Restart permanecem `Unsupported`.
- A API conserva contratos locais de ingestão, identidade, RBAC, auditoria,
  catálogo e sandboxes explicitamente guardados. Não há IdP, PKI, certificado,
  chave, vault, canal de notificação, provider registry operacional, database
  target ou infraestrutura real configurada.
- A
  [proposta de capacidade JOSE completa](../../docs/STATE-06-JOSE-Complete-Capability-Proposal.md)
  e o
  [ADR-0008](../../docs/architecture/ADR-0008-JOSE-Cryptographic-Profiles-And-Key-Lifecycle.md)
  estão revistos como proposta `1.2.0`/ADR revision `1.2`. O ADR permanece
  `proposed`. O lote exclusivamente documental `JOSE-0` foi autorizado e
  produziu o
  [pacote de Architecture, Security and Coverage Design](../../docs/STATE-06-JOSE-0-Architecture-Security-And-Coverage-Design-Report.md):
  cinco profiles e safety caps provisórios, mapas de
  ownership/trust/data/egress, `JOSE-T01`–`JOSE-T17`,
  `JOSE-REQ-001`–`JOSE-REQ-012`, planos de teste/migração/rollback e
  classificação `318/318` das entradas IANA JOSE/JWT Claims, com zero
  `Unreviewed`. Os gates documentais automáticos e os rechecks independentes
  passaram, com zero P0/P1 residual. O proprietário revisou e aceitou o pacote
  exclusivamente como preparação documental. Profiles, caps e decisões
  permanecem provisórios, não normativos, `NotImplemented`, `NotHomologated`,
  `RuntimeDisabled` e `NotAdvertised`; a decisão não aceita a proposta ou
  ADR-0008. `JOSE-1`, `JOSE-D1`, todos os lotes de implementação e todos os
  decision packets permanecem não autorizados ou fora do escopo. Não há JWE,
  emissão JWS própria, ciclo de chaves operacional, IdP, vault/KMS/HSM ou
  capacidade JOSE no runtime além do relying-party JWT/JWKS já descrito.
- A diretriz estratégica de 2026-07-29 tornou o futuro fechamento
  `JOSE STATE-06 INTEGRATION SCOPE COMPLETE` pré-condição da revalidação de
  saída. O escopo proposto percorre `JOSE-1`, `JOSE-D1` e os lotes
  `JOSE-2/3/4/5` aplicáveis, todos sob decisões separadas; não antecipa
  `JOSE-6`/homologação de `STATE-07`, `JOSE-7`/release de `STATE-08` ou
  qualquer autoridade executiva. A delimitação `PC-JOSE-01`–`05` foi aceita
  somente como direção documental; `JOSE-1`, `JOSE-D1` e os lotes posteriores
  continuam não autorizados.
- Não há loader dinâmico nem carregamento ou distribuição operacional de
  provider packages, instalador, assinatura, update channel ou entrega
  operacional. `REQ-050` permanece `PARCIAL`: a matriz clean-room existe, mas
  provider MySQL, integração, comandos, startup, update e packaging
  correspondentes não estão implementados nem homologados.

## Interfaces atuais

- O contrato normativo vigente é o
  [Design System `3.2.1`](../../docs/design/DB-Notifier-Design-System.md), com
  `pt-BR` e `en-GB`, temas Light e Dark e Windows High Contrast como override
  independente.
- Dashboard Web e WPF expõem oito destinos comuns. O Web é responsivo a partir
  de 320 CSS px e não existe cliente móvel nativo. O WPF é
  notification-area-first: flyout como superfície primária e shell completo
  como destino secundário; ele não possui modo TV.
- As superfícies normais apresentam dados locais determinísticos e
  identificados como demonstração. As composições autoritativas existentes são
  test-only, opt-in e desabilitadas por padrão; não usam provider, banco,
  identidade ou canal externo real.
- No sandbox TV autorizado, a entrada lê a API imediatamente e a reconciliação
  seguinte ocorre de forma serial 30 segundos após a conclusão da leitura
  anterior. SignalR é apenas um hint autenticado para antecipar uma nova
  leitura; não substitui polling nem prova tempo real. O modo é session-only,
  Fullscreen é opcional e a saída, a freshness e a verdade da fonte permanecem
  visíveis.
- O registro visual atual contém onze identidades e 22 variantes, com fallback
  neutro universal. Identidade visual não implica implementação, homologação
  ou suporte. Web, WPF, Tray, taskbar, favicon e notificações derivam da mesma
  geometria canônica da marca.
- Delivery autoritativo de notificações, ações administrativas operacionais e
  dados de provider real continuam ausentes. Scaling Windows físico a 200%,
  mixed-DPI entre monitores e as demais condições de `R6-HV-P01` permanecem
  sem evidência física.
- A inicialização automática do WPF no logon ainda não existe; a inicialização
  notification-area-first aplica-se somente quando o processo é iniciado.

## MOD-12 e gate `None → Observer`

- A fundação MOD-12 permanece local, in-memory, provider-neutral e inativa. A
  composição normal contém somente o control plane Observer dormente e uma
  autoridade de ativação indisponível; não contém pipeline operacional,
  hosted service, publisher, corpus operacional, LLM, recomendação, plano,
  executor ou automação.
- O1, O2-A, O2-B, O3-A e O3-B estão automática e humanamente aprovados somente
  nos seus sandboxes locais, sintéticos e test-only. O4 está automaticamente
  `APROVADO`, com Human Gate `APROVADO COM RESSALVAS`, somente como
  projeção/API/UI factual, read-only e sintética; forecast operacional,
  representatividade de produção e acessibilidade/DPI físicos não foram
  comprovados.
- O Quality Gate O5 permanece `BLOQUEADO` e nunca foi convertido em Human Gate
  O5. O5-R1 foi aprovado com ressalvas somente como definição documental de
  uma célula candidata; O5-R2 e O5-R3 foram aprovados somente nos seus escopos
  inativo e sintético. O5-R4 foi aprovado sem achados críticos ou altos, mas
  preserva três lacunas médias: owner operacional nominal, autoridade/custódia
  de ativação e continuidade/reconciliação independente.
- O5-R5-A está automática e humanamente `APROVADO` somente como prontidão e
  runner test-only. O5-R5-B está apenas automaticamente `APROVADO` como driver
  test-only; não possui Human Gate próprio. Nenhum deles aprova a campanha. A
  campanha física pós-D3 mais recente foi `REPROVADA` em
  `Cancellation/Cold`, repetição 13, com working set de `2.916.352 bytes`
  contra o limite inclusivo de `786.432 bytes`. Ela parou com `158/560`
  amostras e `4/16` resumos: `HM-01 BLOQUEADO`, `HM-02 REPROVADO` e
  `HM-03 NÃO TESTADO`.
- A metodologia vigente é V3. A correção bounded da alocação gerenciada de
  `Cancellation/Cold` permanece aceita no escopo test-only, mas a campanha
  física posterior continuou reprovada e não comprovou a causa do working set
  residual.
- D4 permanece tecnicamente `BLOQUEADO` e humanamente `ACEITO COMO BLOQUEADO`.
  Seu gate de lockfiles executou internamente um restore bloqueado fora da
  autoridade; dependências e lockfiles não mudaram, mas uma consulta de
  metadados não pode ser descartada. O incidente não foi reclassificado como
  autorizado.
- O resultado temporal mais recente é
  [PF-OBS-1-D5 `BLOQUEADO`](../../docs/STATE-06-MOD-12-PF-OBS-1-D5-Process-History-Diagnostic-Report.md)
  e
  [humanamente `ACEITO COMO BLOQUEADO`](../../docs/STATE-06-MOD-12-PF-OBS-1-D5-Human-Gate-Report.md).
  Duas repetições do prefixo V3 exato e duas de cada controle preservaram
  `704/704` amostras, mas não reproduziram o excesso histórico; nenhuma causa
  foi atribuída e nenhuma correção especulativa foi aplicada.
- O rebaseline pós-formatação
  [PF-OBS-1-D6 está `BLOQUEADO`](../../docs/STATE-06-MOD-12-PF-OBS-1-D6-Post-Format-Non-Intrusive-Rebaseline-Report.md).
  O defeito anterior do writer foi corrigido e coberto por regressão. Na
  retomada explicitamente autorizada, o primeiro processo parou em `35/158`
  amostras e `1/4` resumos: `FirstByte/Cold` excedeu o coeficiente de
  repetibilidade V3 (`0,22923232701994542` contra `0,2`). `Cancellation/Cold`
  não foi alcançado, a segunda execução não começou e nenhum braço externo ou
  controle D5 foi executado. O resultado não reproduz nem refuta o excesso de
  working set.
- O diagnóstico
  [PF-OBS-1-D7 está `BLOQUEADO`](../../docs/STATE-06-MOD-12-PF-OBS-1-D7-FirstByte-Cold-Repeatability-Diagnostic-Report.md).
  Três processos novos preservaram `35/35` amostras e o objeto de resumo V3;
  os coeficientes `0,024460739425192193`, `0,010868664937096893` e
  `0,011026690622714398` ficaram abaixo do limite inalterado de `0,2`.
  Porém, os JSONs omitiram os campos explícitos de contagem de resumo exigidos
  pelo contrato. A implementação e o gate estático foram corrigidos sem
  alterar ou substituir evidência, mas as três execuções são
  contractualmente incompletas: a classificação D7-U não foi admitida e o
  braço externo permaneceu proibido. Nenhuma causa foi atribuída.
- O lote corretivo
  [PF-OBS-1-D7-R1 foi concluído como `D7-R1.NOT_REPRODUCED`](../../docs/STATE-06-MOD-12-PF-OBS-1-D7-R1-Summary-Count-Contract-Rerun-Report.md)
  e o seu
  [Human Gate foi `APROVADO COM RESSALVAS`](../../docs/STATE-06-MOD-12-PF-OBS-1-D7-R1-Human-Gate-Report.md).
  Três envelopes R1-U admissíveis preservaram `35/35` amostras e `1/1`
  resumo; os coeficientes `0,02148452314834982`, `0,0096634992433091` e
  `0,012714703648847817` ficaram abaixo do limite V3 inalterado de `0,2`.
  Com `0/3` falhas, R1-E permaneceu proibido. Os três relatórios D7 históricos
  continuam byte a byte, bloqueados e excluídos da classificação R1. O
  resultado e a aceitação humana comprovam não recorrência somente no lote
  R1; nenhuma amostra humana adicional estava prevista, nenhuma causa foi
  atribuída e nenhum gate adjacente foi aprovado.
- O diagnóstico
  [PF-OBS-1-D8 foi concluído como `D8.EARLY_GATE_INTERMITTENT`](../../docs/STATE-06-MOD-12-PF-OBS-1-D8-Exact-Prefix-Completion-Reconciliation-Report.md).
  Seu
  [Human Gate foi `APROVADO COM RESSALVAS`](../../docs/STATE-06-MOD-12-PF-OBS-1-D8-Human-Gate-Report.md),
  aceitando somente essa classificação automática.
  As duas tentativas novas foram admissíveis e não observadas. A primeira
  reteve um target stop exato em `Cancellation/Cold`, measured repetition 5,
  com `150/158` amostras, `4/4` resumos e working set de `2.797.568 bytes`
  contra o limite de `786.432 bytes`. A segunda parou no resumo
  `FirstByte/Cold`, com `35/158` amostras, `1/4` resumos e coeficiente
  `0,20075118542700426` contra `0,2`. A árvore D8 não entrou numa
  classificação de recorrência do working set porque somente `1/2`
  tentativas foi target-eligible. Nenhuma causa foi atribuída; observação
  externa, controles D5 e HM-01–HM-03 permaneceram proibidos. A aceitação
  humana não aprovou PF-OBS-1, O5, Observer, `ActivationState` ou lifecycle.
- PF-OBS-1 e O5 permanecem sem aprovação. A causa do pico físico residual,
  corpus e ambiente piloto representativos, calibração operacional,
  homologação exata da célula e owners materiais continuam pendentes.
- A diretriz estratégica de 2026-07-29 tornou o futuro fechamento
  `MOD-12 STATE-06 INTEGRATION SCOPE COMPLETE` pré-condição da revalidação de
  saída. A delimitação `PC-M12-01`–`04`, aceita somente como direção
  documental, exige no futuro revalidar O1–O4 num único boundary
  product-owned, com adapters/activation guard reais exercidos por harness
  sintético, sem implementação test-only paralela e com zero worker/I/O em
  `ActivationState=None`. Nenhum lote foi autorizado. PF-OBS, O5 e D9 não são
  reclassificados nem executados: permanecem no handoff de
  homologação/ativação de `STATE-07`, com D9 opcional e dependente de
  autorização própria.

## Decisões que exigem nova autoridade

- Qualquer novo diagnóstico, mudança metodológica ou campanha física requer
  autorização explícita e separada. `D9` não foi autorizado nem transformado
  em pré-condição automática.
- `JOSE-1`, `JOSE-D1`, aceitação do ADR-0008 e qualquer código, dependência,
  migration, login/IdP, chave, custodiante, egress candidate, infraestrutura,
  homologação ou profile JOSE operacional exigem decisões explícitas e
  separadas. A direção documental aceita torna o fechamento integrado
  `JOSE-1`–`JOSE-5` aplicável uma pré-condição futura, mas nem o aceite da
  delimitação nem o aceite humano limitado de `JOSE-0` concedem qualquer
  dessas autoridades.
- Qualquer ativação `None → Observer` exige os gates próprios e uma decisão
  explícita. O fechamento sintético MOD-12 no `STATE-06` não aprova O5, e o
  bloqueio de ativação não pode ser contornado por mudança de lifecycle.
- Qualquer transição para `STATE-07` exige decisão de lifecycle própria. A
  estratégia vigente exige antes dela os fechamentos de integração STATE-06
  de MOD-12 e JOSE, a revalidação consolidada da baseline resultante, novas
  amostras humanas e um Human Gate de revalidação. Cada item exige autoridade
  própria. O Human Gate de 2026-07-20 e os gates posteriores permanecem
  históricos nos seus escopos; a proposta antiga está invalidada, e a nova
  não concede autoridade executiva.
- Produção, PostgreSQL operacional, provider homologado, runtime externo,
  publicação, deploy e ação administrativa real continuam não autorizados.
