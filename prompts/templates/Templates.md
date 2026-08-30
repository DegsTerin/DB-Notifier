# Templates do DB-Notifier

Templates não representam execução, receipt, evidência ou decisão até serem
preenchidos com fatos verificáveis.

## Registro de despacho autônomo

- ID do despacho:
- Chave de deduplicação:
- Origem:
- Destino factual:
- Rota: `CONTINUE_CURRENT`/`DELEGATE_SUBAGENT`/`RETURN_TO_EXISTING`/`START_NEW_AUTO_DISPATCH`
- Projeto e workspace:
- Versão do corpus e baseline:
- Estado/lote/gate:
- Objetivo exclusivo:
- Escopo positivo:
- Escopo negativo e trabalho protegido:
- Ownership de paths, artefatos e recursos:
- Dependências congeladas:
- Verificações e evidências:
- Condições de parada:
- Resultado esperado:
- Ferramenta e horário:
- Resultado factual da ferramenta:
- Receipt/cursor:
- Estado atual:

O payload é enviado diretamente pela ferramenta. Não criar texto, título,
mensagem exata ou bloco para o proprietário copiar, colar ou encaminhar.

## Envelope fechado da tarefa

- ID do envelope e versão:
- Status: `DRAFT`/`READY`/`IN_PROGRESS`/`BLOCKED`/`COMPLETE`
- Objetivos e requisitos canônicos aplicáveis:
- Autoridade e limites não renunciáveis:
- `STATE`, módulo, lote ou gate proprietário:
- Workspace, branch/worktree e baseline congelada:
- Estado inicial do index, worktree, untracked e artefatos protegidos:
- Objetivo e resultado verificável:
- Escopo positivo:
- Escopo negativo:
- Contratos e dependências congelados:
- Classe de cada artefato: `AUTHORITY`/`CURRENT_FACT`/`HISTORY`/`EVIDENCE`/`PLAN`/`IMPLEMENTATION`/`GENERATED`
- Modo: `SAFE_PARALLEL`/`CONTRACT_FROZEN_PARALLEL`/`SINGLE_OWNER`/`SEQUENTIAL_ONLY`
- Writer exclusivo por path e artefato lógico:
- Recursos mutáveis e owner exclusivo:
- Entradas compartilhadas somente leitura:
- Revisores independentes:
- Definition of Ready:
- Definition of Done:
- Regressões e checks:
- Evidências e sanitização:
- Rollback:
- Automated Safety Gate destrutivo: `NÃO APLICÁVEL` ou evidência completa:
- Pré-requisitos externos: `NÃO APLICÁVEL` ou itens presentes/ausentes:
- Stop codes:
- Disposição mecânica: `PASS`/`FAIL`/`BLOCKED`/`PARTIAL`/`NOT_RUN`
- Decisão: `AGENT_DECIDED`/`AUTOMATED_GATE_PASS`/`AUTOMATED_GATE_FAIL`/`LOCAL_COMPLETE`/`EXTERNAL_PREREQUISITE`/`BLOCKED_BY_HIGHER_AUTHORITY`

O envelope fica `READY` somente quando baseline, escopos, dependências,
ownership, recursos, checks, revisores e stop codes forem coerentes. Ele
delimita a execução e não altera autoridade superior ou pré-requisito externo.

## Plano vivo executável

Usar no `PLANS.md` para trabalho amplo, transversal, multi-incremento ou
auditoria com remediação:

- Control record: ID, status, data, baseline, autoridade, estado, writer e reviewers.
- Source provenance and adaptation boundary.
- Capability disposition.
- Positive scope.
- Negative scope and protected work.
- Definition of Ready.
- Definition of Done.
- Findings: ID, severidade `P0`–`P3`, evidência e disposição.
- Increment plan: ordem, owner, status e critério de saída.
- Evidence log: data, comando/revisão, resultado e limitação.
- Blockers and limitations, com stop code.
- Outcome and automatic next action.
- Dispatch receipt.
- Append-only plan change log.

Atualizar durante a execução e reconciliar antes do próximo despacho. Resultado
factual pertence também à fonte proprietária; o plano sozinho não o substitui.

## Plano de trabalho paralelo

- Coordenadora e identificador canônico da plataforma:
- Versão do corpus e baseline:
- Estado/lote:
- Objetivo global:
- Frentes numeradas e resultado por frente:
- Dependências congeladas e acíclicas:
- Ownership exclusivo de paths:
- Ownership exclusivo de artefatos:
- Ownership exclusivo de recursos:
- Entradas compartilhadas somente leitura:
- Arquivos, recursos e ações proibidos:
- Branch/worktree por writer e autoridade:
- Isolamento de portas, processos, bancos, caches e outputs:
- Verificações e evidências por frente:
- Condições de parada:
- Payload interno por frente:
- Ordem determinística de integração:
- Verificações locais e transversais:
- Fallback sequencial:

## Mensagem interna de início de worker

- Projeto:
- Workspace:
- Lane:
- Coordenadora:
- Baseline:
- Estado/lote:
- Autoridade:
- Objetivo exclusivo:
- Pré-condições:
- Dependências congeladas:
- Escrita exclusiva permitida ou somente leitura:
- Entradas somente leitura:
- Escopo negativo e trabalho protegido:
- Verificações:
- Resultado esperado:
- Condições de parada:
- Ordem de integração:
- Formato de retorno:

Enviar diretamente pelo mecanismo de subagentes. Não exibir como payload para o
proprietário encaminhar.

## Retorno estruturado de worker

- Lane e identificador:
- Baseline efetivamente usada:
- Situação:
- Candidato produzido:
- Arquivos e paths tocados:
- Artefatos e recursos tocados:
- Verificações, resultados e evidências:
- Achados `P0`–`P3`:
- Limitações e riscos:
- Stop conditions acionadas:
- Dependências pendentes:
- Recomendação de integração:

Uma worker entrega somente candidato. Ela não declara conclusão do lote,
lifecycle, Agent Gate ou projeto e não atualiza memória permanente.

## Receipt de despacho

- ID:
- Deduplication key:
- Origem:
- Destino:
- Rota:
- Ferramenta: obrigatória nas três rotas baseadas em ferramenta; para
  `CONTINUE_CURRENT`, `NÃO APLICÁVEL — registro local no plano`
- Horário:
- Resultado confirmado:
- Cursor/revisão:
- Retry executado: sim/não; justificativa:
- Estado reconciliado:

Resultado incerto é reconciliado antes de qualquer retry.

O registro de `CONTINUE_CURRENT` comprova a continuação local e a atualização
do plano; não fabrica receipt de ferramenta. As demais três rotas exigem
resultado confirmado do mecanismo usado.

## Relatório de execução

- Estado/fase:
- Baseline e commit:
- Ambiente, data e executor:
- Escopo e providers cobertos:
- Pré-condições e configuração sanitizada:
- Comandos/testes e resultados:
- Shutdown preflight:
- Falhas e correções:
- Itens não testados:
- Riscos residuais:
- Disposição mecânica:
- Decisão agente:
- Idioma e exceções:
- Evidências:

## Auditoria automática

- Estado e escopo:
- Entregáveis esperados:
- Verificações executadas:
- Resultado por validação: `PASS`/`FAIL`/`BLOCKED`/`PARTIAL`/`NOT_RUN`
- Achados `P0`–`P3`:
- Evidências:
- Limitações:
- Resultado do gate de idioma:
- Recomendação:

## Agent Gate

- ID e baseline:
- Estado/lote:
- Relatório automático:
- Checks obrigatórios e disposições:
- Amostras agentes executadas:
- Amostras não executadas e motivo:
- Revisores independentes:
- Achados: `P0`/`P1`/`P2`/`P3`
- Segurança e autorização do produto:
- Cobertura e limitações:
- Rollback:
- Decisão: `AUTOMATED_GATE_PASS`/`AUTOMATED_GATE_FAIL`
- Decisão da coordenadora: `AGENT_DECIDED` ou estado bloqueado aplicável
- Data e evidências:

`AUTOMATED_GATE_PASS` exige todos os checks obrigatórios em `PASS` e zero
`P0`/`P1`. Nenhum julgamento converte `FAIL`, `BLOCKED`, `PARTIAL` ou `NOT_RUN`
obrigatório em sucesso.

## Adendo a Human Gate histórico

- Fase original:
- Registro histórico preservado:
- Motivo da revalidação atual:
- Evidência histórica:
- Evidência atual reproduzida:
- Amostras agentes atuais:
- Limitações:
- Agent Gate atual:
- Data e responsáveis agentes:

Este adendo não reescreve, ratifica em nome humano ou altera a decisão
histórica. Ele registra somente a avaliação atual e prospectiva.

## ADR

- ID e título:
- Status: proposta/aceita/substituída
- Contexto:
- Decisão agente:
- Alternativas:
- Consequências:
- Segurança e operação:
- Compatibilidade/migração:
- Revisores independentes:
- Agent Gate:
- Data e responsáveis:
