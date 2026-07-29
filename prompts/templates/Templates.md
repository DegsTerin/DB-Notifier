# Templates do DB-Notifier

Templates não representam execução nem aprovação até serem preenchidos com evidências reais.

## Contrato mínimo de handoff governado

Aplicar a todo handoff conforme a autoridade
[`Conversation-Coordination-Prompt.md`](../../Conversation-Coordination-Prompt.md).
Os nomes e a ordem dos campos são normativos:

```text
Status:
Completed:
Remaining for this target:
Next step:
Next stage:
Your action now:
Conversation action: CONTINUE_CURRENT/START_NEW/RETURN_TO_EXISTING
Conversation target:
Suggested title:
Conversation reason:
Exact next message:
Parallel work: SEQUENTIAL_ONLY/PARALLEL_OPTIONAL/PARALLEL_RECOMMENDED
Parallel plan:
Exact parallel messages:
```

Templates podem conter placeholders; uma instância real deve substituí-los por
conteúdo completo. Quando nenhuma mensagem for exigida, usar
`Exact next message: None — no message is required`. Para trabalho sequencial,
`Parallel plan` começa por `None —` com a razão concreta e
`Exact parallel messages` usa
`None — parallel work is not recommended`.

## Complemento de handoff de fase

- Estado encerrado:
- Estado recomendado:
- Objetivo e escopo entregues:
- Arquivos/artefatos alterados:
- ADRs e decisões:
- Checks e resultados:
- Shutdown preflight: componentes/PIDs encerrados, listeners verificados e resíduos:
- Interfaces/schemas/protocolos:
- Riscos e dívida:
- Rollback:
- Pré-condições da próxima fase:
- Auditoria automática:
- Human Gate:
- Passos ordenados e local de execução:
- Resultado esperado:
- Restrições ou cuidados:
- Evidência/resposta que o usuário deve retornar:

## Plano de trabalho paralelo

- Conversa coordenadora e label confirmado pelo proprietário ou identificador canônico fornecido pela plataforma:
- Versão do corpus:
- Commit/hash da baseline:
- Estado/gate/lote:
- Autoridade e escopo negativo:
- Lanes numeradas, objetivo e resultado esperado:
- Dependências congeladas e acíclicas:
- Ownership exclusivo de paths:
- Ownership exclusivo de artefatos lógicos:
- Ownership exclusivo de recursos mutáveis:
- Inputs compartilhados somente leitura:
- Arquivos, recursos e ações proibidos por lane:
- Branch/worktree por writer e autoridade correspondente:
- Isolamento de portas, processos, bancos, índices, temporários, caches e outputs:
- Checks e evidências por lane:
- Condições de parada:
- Mensagem completa de início por lane:
- Formato de retorno:
- Ordem determinística de integração:
- Checks locais por integração:
- Checks transversais:
- Fallback sequencial:

## Mensagem de início de uma worker

```text
Projeto:
Workspace:
Lane:
Conversation label:
Conversa coordenadora confirmada:
Baseline:
Estado/gate/lote:
Autoridade existente:
Objetivo exclusivo:
Pré-condições:
Dependências congeladas:
Escrita exclusiva permitida ou read-only:
Inputs somente leitura:
Arquivos e ações proibidos:
Checks:
Output esperado:
Condições de parada:
Ordem de integração:
Formato da mensagem de retorno:
```

## Retorno de uma worker

- Lane e conversation label:
- Baseline efetivamente usada:
- Status da lane:
- Candidato produzido:
- Arquivos e paths tocados:
- Artefatos lógicos e recursos mutáveis tocados:
- Checks, resultados e evidências:
- Limitações e riscos:
- Condições de parada acionadas:
- Dependências ou autoridade ainda pendentes:
- Mensagem exata de retorno à coordenadora:

Uma worker entrega somente um candidato. Ela não declara conclusão do lote,
estado, gate ou projeto e não atualiza memória ou decisões permanentes.

## Relatório de execução

- Estado/fase:
- Versão e commit:
- Ambiente, data e executor:
- Escopo e providers cobertos:
- Pré-condições e configuração sanitizada:
- Comandos/testes e resultados:
- Shutdown preflight: identificação, encerramento, verificação e limitações:
- Falhas e correções:
- Itens não testados:
- Riscos residuais:
- Decisão do gate:
- Evidências:

Usar para integração, homologação ou release. A fase determina as verificações adicionais em `../governance/Quality-Gates.md`.

## Auditoria automática

- Estado e escopo:
- Entregáveis esperados:
- Checks executados:
- Resultado por gate: APROVADO/REPROVADO/BLOQUEADO/NÃO APLICÁVEL
- Achados por severidade:
- Evidências:
- Limitações do ambiente:
- Recomendação:

## Human Gate

- Fase:
- Validador e data:
- Relatório automático revisado (identificador/commit):
- Amostras críticas repetidas pelo validador:
- Amostras não repetidas e motivo:
- Experiência operacional:
- Segurança/autorização:
- Cobertura pendente:
- Ressalvas aceitas:
- Decisão: PENDENTE/APROVADO/APROVADO COM RESSALVAS/REPROVADO
- Justificativa e evidências:
- Confirmação inequívoca do validador: `Confirmo a decisão acima exclusivamente para <STATE-ID>`

Uma palavra isolada ou autorização para continuar não preenche este template. Cada fase exige decisão separada; gates agrupados permanecem pendentes.

## Ratificação retrospectiva de Human Gate

- Fase original:
- Registro histórico contestado:
- Motivo da contestação:
- Evidência automática histórica:
- Evidência automática repetida agora:
- Amostras humanas repetidas agora:
- Limitações ainda não exercitadas:
- Ressalvas e dívida aceitas:
- Decisão de ratificação: PENDENTE/APROVADO/APROVADO COM RESSALVAS/REPROVADO
- Validador e data:
- Confirmação inequívoca: `Ratifico a decisão acima exclusivamente para <STATE-ID>`

A ratificação é um adendo e não reescreve o relatório ou a decisão histórica contestada.

## ADR

- ID e título:
- Status: proposta/aceita/substituída
- Contexto:
- Decisão:
- Alternativas:
- Consequências:
- Segurança e operação:
- Compatibilidade/migração:
- Data e responsáveis:
