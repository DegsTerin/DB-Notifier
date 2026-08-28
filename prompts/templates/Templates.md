# Templates do DB-Notifier

Templates não representam execução nem aprovação até serem preenchidos com evidências reais.

## Contrato mínimo de handoff governado

Aplicar a todo handoff conforme a autoridade
[`Conversation-Coordination-Prompt.md`](../governance/Conversation-Coordination-Prompt.md)
e a
[`Política de Idioma`](../governance/Language-Policy.md).
Os 14 campos, suas chaves canônicas e sua ordem são normativos. Os rótulos,
valores, razões e mensagens apresentados ao proprietário permanecem em
`pt-BR`:

```text
Situação (`Status`):
Concluído (`Completed`):
Restante para este objetivo (`Remaining for this target`):
Próximo passo (`Next step`):
Próxima etapa (`Next stage`):
Sua ação agora (`Your action now`): Raciocínio recomendado do Codex: <nível> (`<identificador técnico>`). Motivo: <razão específica>. <orientação para copiar e enviar a mensagem pronta, resultado esperado e limite aplicável>
Ação da conversa (`Conversation action`):
Destino da conversa (`Conversation target`):
Título sugerido (`Suggested title`):
Motivo da conversa (`Conversation reason`):
Próxima mensagem exata (`Exact next message`):
Trabalho paralelo (`Parallel work`):
Plano paralelo (`Parallel plan`):
Mensagens paralelas exatas (`Exact parallel messages`):
```

Em uma instância real, o rótulo de cada campo permanece fora da caixa. Todo
payload que o proprietário seja orientado a copiar ocupa sozinho um bloco de
código Markdown cercado e rotulado `text`. O preenchimento de
`Próxima mensagem exata` segue este formato:

Próxima mensagem exata (`Exact next message`):

```text
<mensagem completa, específica e pronta para copiar>
```

Quando `START_NEW` exigir que o proprietário copie um título, esse título usa
outro bloco `text`. Mensagens de lanes paralelas e mensagens de retorno também
usam um bloco separado por payload, com a identificação correspondente fora da
caixa. Rótulos, razões, instruções e alternativas nunca entram no conteúdo
copiável.

`Ação da conversa` (`Conversation action`) aceita somente
`CONTINUE_CURRENT`, `START_NEW` ou `RETURN_TO_EXISTING`.
`Trabalho paralelo` (`Parallel work`) aceita somente `SEQUENTIAL_ONLY`,
`PARALLEL_OPTIONAL` ou `PARALLEL_RECOMMENDED`.

`Sua ação agora` começa com exatamente um nível do catálogo `Leve` (`low`),
`Médio` (`medium`), `Alto` (`high`), `Extra alto` (`xhigh`), `Máximo` (`max`)
ou `Ultra` (`ultra`), seguido de uma razão específica para a próxima
interação. Usar o menor esforço suficiente, reavaliar a recomendação a cada
handoff e não declarar seleção, aplicação ou disponibilidade sem evidência da
superfície, do modelo e da conta vigentes. A recomendação é consultiva e não
altera roteamento, paralelismo, autoridade, ownership, gates ou lifecycle.

Templates podem conter placeholders; uma instância real deve substituí-los por
conteúdo completo. Todo handoff real, inclusive quando concluído, parcial ou
bloqueado, deve preencher `Próxima mensagem exata` (`Exact next message`) com
uma única mensagem completa, específica, em `pt-BR` e pronta para copiar e
enviar literalmente na conversa indicada, dentro de exatamente um bloco
`text`.

`Próxima mensagem exata` nunca aceita valor vazio, placeholder, alternativas ou
``Não se aplica (`None`) — nenhuma mensagem é necessária``. Quando nenhuma
ação adicional de projeto for conhecida, fornecer uma mensagem segura de
confirmação ou encerramento que declare não autorizar nova ação. A mensagem
deve corresponder a `Próximo passo`, `Sua ação agora`, `Ação da conversa` e
`Destino da conversa`.

A mensagem pronta não representa decisão antes de ser enviada. Ela não pode
presumir ou fabricar aprovação, Human Gate, ADR, `ActivationState`, lifecycle,
operação Git ou ação externa. Uma decisão formal pendente recebe mensagem para
apresentação ou revisão do pacote decisório, salvo quando o proprietário já
tiver escolhido inequivocamente seu resultado no contexto vigente.

Para trabalho sequencial,
`Plano paralelo` (`Parallel plan`) começa por
``Não se aplica (`None`) —`` com a razão concreta e
`Mensagens paralelas exatas` (`Exact parallel messages`) usa
``Não se aplica (`None`) — o trabalho paralelo não é recomendado``.

## Complemento de encerramento de fase

- Estado encerrado:
- Estado recomendado:
- Objetivo e escopo entregues:
- Arquivos/artefatos alterados:
- ADRs e decisões:
- Verificações e resultados:
- Desligamento preventivo (`shutdown preflight`): componentes/PIDs encerrados, portas em escuta verificadas e resíduos:
- Interfaces/esquemas/protocolos:
- Riscos e dívida:
- Reversão (`rollback`):
- Pré-condições da próxima fase:
- Auditoria automática:
- Validação humana (`Human Gate`):
- Passos ordenados e local de execução:
- Resultado esperado:
- Restrições ou cuidados:
- Evidência/resposta que o usuário deve retornar:
- Comunicação com o proprietário em `pt-BR`:
- Idioma dos artefatos novos ou preservado nos arquivos existentes:
- Convenções externas mantidas e exceções linguísticas justificadas:
- Impacto na localização da interface: nenhum ou autoridade específica:
- Resultado do gate de idioma:

## Envelope fechado da tarefa

- ID do envelope e versão:
- Status: `DRAFT`/`READY`/`IN_PROGRESS`/`BLOCKED`/`COMPLETE`
- Autoridade humana exata e limites não renunciáveis:
- `STATE`, módulo, lote ou gate proprietário:
- Workspace, branch/worktree e baseline congelada:
- Estado inicial do index, worktree, untracked e artefatos protegidos:
- Objetivo e resultado verificável:
- Escopo positivo:
- Escopo negativo:
- Contratos e dependências congelados:
- Classe de cada artefato: `AUTHORITY`/`CURRENT_FACT`/`HISTORY`/`EVIDENCE`/`PLAN`/`IMPLEMENTATION`/`GENERATED`
- Modo de execução: `SAFE_PARALLEL`/`CONTRACT_FROZEN_PARALLEL`/`SINGLE_OWNER`/`SEQUENTIAL_ONLY`
- Writer exclusivo por path e artefato lógico:
- Recursos mutáveis e owner exclusivo:
- Entradas compartilhadas somente leitura:
- Revisor independente e critérios de independência:
- Definition of Ready:
- Definition of Done:
- Regressões e checks proporcionais:
- Evidências esperadas e sanitização:
- Estratégia de rollback:
- Stop codes aplicáveis e condição objetiva:
- Disposição final: `PASS`/`FAIL`/`BLOCKED`/`PARTIAL`/`NOT_RUN`

O envelope fica `READY` somente quando baseline, autoridade, escopos,
dependências, ownership, recursos, checks, revisores e stop codes forem
coerentes. Ele delimita a execução, mas não cria autoridade, aprovação ou
transição.

## Plano vivo executável

Usar no `PLANS.md` raiz para trabalho amplo, transversal, multi-incremento ou
auditoria com remediação já autorizada:

- Control record: ID, status, data, baseline, autoridade, estado, writer e
  reviewers.
- Source provenance and adaptation boundary.
- Capability disposition: adopted/adapted/already governed/rejected/deferred.
- Positive scope.
- Negative scope and protected work.
- Definition of Ready.
- Definition of Done.
- Findings: ID, severidade `P0`–`P3`, evidência e disposição.
- Increment plan: ordem, owner, status e critério de saída.
- Evidence log: data, comando/revisão, resultado e limitação.
- Blockers and limitations, com stop code quando aplicável.
- Outcome and next action.
- Append-only plan change log.

Atualizar o plano durante a execução e reconciliá-lo antes do handoff. Um
resultado ou decisão factual pertence também à sua fonte proprietária; o plano
sozinho nunca a substitui.

## Plano de trabalho paralelo

- Conversa coordenadora e rótulo confirmado pelo proprietário ou identificador canônico fornecido pela plataforma:
- Versão do corpus:
- Referência da base (`commit`/hash):
- Estado/validação/lote:
- Autoridade e escopo negativo:
- Raciocínio recomendado para a coordenadora e razão:
- Frentes numeradas (`lanes`), objetivo e resultado esperado:
- Raciocínio recomendado e razão por frente:
- Dependências congeladas e acíclicas:
- Propriedade exclusiva de caminhos:
- Propriedade exclusiva de artefatos lógicos:
- Propriedade exclusiva de recursos mutáveis:
- Entradas compartilhadas somente leitura:
- Arquivos, recursos e ações proibidos por frente:
- Ramificação/árvore de trabalho (`branch`/`worktree`) por responsável de escrita e autoridade correspondente:
- Isolamento de portas, processos, bancos, índices, temporários, caches e saídas:
- Verificações e evidências por frente:
- Condições de parada:
- Mensagem completa de início por frente:
- Formato de retorno:
- Ordem determinística de integração:
- Verificações locais por integração:
- Verificações transversais:
- Alternativa sequencial (`fallback`):

## Mensagem de início de uma conversa auxiliar

Antes dos 19 campos existentes, inserir a seguinte frase de preâmbulo não
canônica, que não cria campo adicional:

```text
Para esta lane, recomenda-se usar o raciocínio <nível> (`<identificador técnico>`) do Codex porque <razão específica>.
```

```text
Projeto:
Espaço de trabalho (`Workspace`):
Frente de trabalho (`Lane`):
Rótulo da conversa:
Conversa coordenadora confirmada:
Base (`Baseline`):
Estado/validação/lote:
Autoridade vigente:
Objetivo exclusivo:
Pré-condições:
Dependências congeladas:
Escrita exclusiva permitida ou somente leitura:
Entradas somente leitura:
Arquivos e ações proibidos:
Verificações:
Resultado esperado:
Condições de parada:
Ordem de integração:
Formato da mensagem de retorno:
```

## Retorno de uma conversa auxiliar

- Frente e rótulo da conversa:
- Base efetivamente usada:
- Situação da frente:
- Candidato produzido:
- Arquivos e caminhos tocados:
- Artefatos lógicos e recursos mutáveis tocados:
- Verificações, resultados e evidências:
- Limitações e riscos:
- Condições de parada acionadas:
- Dependências ou autoridade ainda pendentes:
- Mensagem exata de retorno à coordenadora:

A mensagem exata de retorno é apresentada logo após o último rótulo, sozinha em
um bloco `text`.

Uma conversa auxiliar entrega somente um candidato. Ela não declara conclusão
do lote, estado, validação ou projeto e não atualiza memória ou decisões
permanentes.

## Relatório de execução

- Estado/fase:
- Versão e referência do commit:
- Ambiente, data e executor:
- Escopo e provedores (`providers`) cobertos:
- Pré-condições e configuração sanitizada:
- Comandos/testes e resultados:
- Desligamento preventivo (`shutdown preflight`): identificação, encerramento, verificação e limitações:
- Falhas e correções:
- Itens não testados:
- Riscos residuais:
- Decisão da validação:
- Idioma da comunicação, dos artefatos e das exceções:
- Confirmação de que a localização da interface não foi inferida:
- Evidências:

Usar para integração, homologação ou publicação (`release`). A fase determina
as verificações adicionais em `../governance/Quality-Gates.md`.

## Auditoria automática

- Estado e escopo:
- Entregáveis esperados:
- Verificações executadas:
- Resultado por validação: APROVADO/REPROVADO/BLOQUEADO/NÃO APLICÁVEL
- Achados por severidade:
- Evidências:
- Limitações do ambiente:
- Comunicação com o proprietário em `pt-BR`:
- Novos artefatos em `en-GB` ou idioma existente preservado:
- Nomes externos e identificadores preservados:
- Resultado do gate de idioma:
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
