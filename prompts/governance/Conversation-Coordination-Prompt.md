# DB-Notifier — Coordenação de Conversas e Trabalho Paralelo Seguro

- Status: autoridade temática normativa
- Revisão: `1.4.0`
- Versão de introdução no corpus: `6.2.0`
- Versão desta revisão no corpus: `6.6.0`
- Projeto: `DB-Notifier`
- Workspace: raiz confirmada do repositório ou do worktree atribuído à conversa

Este documento foi adaptado no próprio local a partir da fonte fornecida pelo
proprietário, cujo SHA-256 anterior à incorporação era
`0019950242314908762CAD3E2AEA01C122023E3867885289E04FB3A70CA912D4`.

## Finalidade, autoridade e limites

Este documento é a autoridade temática única para:

1. decidir quando continuar na conversa atual;
2. decidir quando iniciar uma nova conversa;
3. decidir quando retornar a uma conversa anterior confirmada;
4. fornecer sempre a mensagem exata que o proprietário deverá enviar;
5. recomendar o nível de raciocínio do Codex adequado a cada conversa;
6. classificar se várias conversas podem trabalhar simultaneamente;
7. impedir que o paralelismo autorizado produza writers sobrepostos,
   sobrescrita, integração ambígua ou mudança fora de autoridade;
8. fechar o envelope operacional de cada tarefa com baseline, escopo,
   ownership, recursos, checks, revisores e stop codes;
9. classificar topologia de execução e artefatos sem alterar os enums do
   handoff governado.

[`Governance.md`](Governance.md)
permanece proprietário da autoridade, execução controlada, lifecycle, bloqueio
e memória. Este documento especializa somente a coordenação de conversas e o
trabalho paralelo. Segurança, Human Gates, ADRs, estado factual e autorizações
externas conservam seus proprietários existentes.

A [`Política de Idioma`](Language-Policy.md) governa toda comunicação com o
proprietário. Rótulos, valores, razões, orientações e mensagens prontas para
copiar são apresentados em `pt-BR`. Chaves canônicas, comandos, paths e enums
podem permanecer em inglês entre crases ou parênteses somente quando
tecnicamente necessário.

Esta política não autoriza, por si só, alteração de arquivo, inicialização ou
operação Git, branch, worktree, commit, merge, rebase, push, código, build,
teste, runtime, ação externa, decisão arquitetural, Human Gate, ativação ou
transição de lifecycle. Cada ação continua dependente da autoridade já exigida
pelo projeto.

Segurança e consistência têm prioridade sobre velocidade. Os controles são
fail-safe: previnem, detectam e interrompem trabalho inseguro, mas não prometem
ausência absoluta de erros. Na dúvida, classificar como `SEQUENTIAL_ONLY`.

Neste documento, `conversa worker` e `agente de engenharia` designam uma frente
de trabalho temporária. O termo `Agent` permanece reservado ao componente de
produto DB-Notifier, e MOD-12 continua regido pela sua autoridade própria.

As regras de navegação manual aplicam-se a conversas visíveis ao proprietário.
Spawn e coordenação de subagentes internos permitidos pela plataforma são
orquestração técnica, não alegação de que o agente abriu, encontrou, renomeou
ou mudou uma conversa do proprietário. Um identificador canônico fornecido
pela plataforma pode identificar a coordenadora interna, mas não satisfaz
`RETURN_TO_EXISTING`.

## 1. Roteamento entre conversas

Todo handoff governado deverá classificar a próxima interação usando
exatamente um destes valores:

- `CONTINUE_CURRENT`
- `START_NEW`
- `RETURN_TO_EXISTING`

### `CONTINUE_CURRENT`

Usar quando o mesmo objetivo, estado ou lote continua ativo e o contexto atual
permanece confiável.

### `START_NEW`

Usar quando começar outro estado, gate, lote ou assunto independente; quando o
contexto atual já não for confiável; ou quando não existir referência
confirmada para uma conversa anterior.

### `RETURN_TO_EXISTING`

Usar somente quando o título ou label da conversa anterior tiver sido fornecido
ou confirmado pelo proprietário. Sem essa confirmação, usar `START_NEW`.

### Regras obrigatórias de navegação

- O agente recomenda a conversa; o proprietário realiza a navegação manual.
- O agente nunca afirma que abriu, encontrou, renomeou ou mudou de conversa.
- Nunca inventar título, label, link ou identificador de conversa existente.
- Um título sugerido para conversa nova é somente uma proposta não canônica.
- Ao retornar a uma conversa antiga, reconciliar integralmente o contexto com
  a versão do corpus, baseline, estado factual e autoridade vigentes.
- Toda conversa nova ou retomada deve reler as instruções aplicáveis e
  confirmar estado, autoridade, escopo positivo e escopo negativo antes de
  agir.
- Se o contexto de uma decisão formal se perder, usar `START_NEW`, reconstruir
  o resumo vigente a partir da memória oficial e solicitar nova decisão
  inequívoca; nunca inferir ou transportar aprovação.

Conversas são contexto temporário. A memória oficial é tipada:

- governança e instruções: autoridade vigente;
- `Current-State.md`: presente factual;
- `State-Transition-Log.md`: histórico factual append-only;
- ADRs: decisões arquiteturais;
- relatórios: evidência de execuções específicas;
- commits: evidência versionada das mudanças, sem autoridade implícita.

O pedido e a resposta de um Human Gate permanecem numa única conversa
coordenadora que contenha o resumo integral e vigente. A decisão continua
humana e deve ser registrada na memória oficial; a conversa não a substitui.

## 2. Avaliação de paralelismo

A avaliação de paralelismo é independente do roteamento da conversa principal.
Todo handoff governado deverá usar exatamente um destes valores:

- `SEQUENTIAL_ONLY`
- `PARALLEL_OPTIONAL`
- `PARALLEL_RECOMMENDED`

### `SEQUENTIAL_ONLY`

Usar quando houver dependência entre tarefas, arquivos ou recursos
compartilhados, contrato instável, decisão humana pendente, isolamento
insuficiente, runtime comum ou risco de conflito.

Trabalho com runtime ou processos DB-Notifier é `SEQUENTIAL_ONLY` por padrão
devido ao shutdown preflight. Uma exceção exige plano único, isolamento
comprovado e autoridade explícita que sejam compatíveis com o preflight.

### `PARALLEL_OPTIONAL`

Usar quando as tarefas forem independentes, mas o ganho esperado for pequeno
ou o custo de coordenação puder superar o benefício.

### `PARALLEL_RECOMMENDED`

Usar somente quando existirem duas ou mais tarefas realmente independentes,
limitadas, verificáveis e com ganho material de tempo ou especialização.

Usar o menor número útil de conversas. Na dúvida, usar `SEQUENTIAL_ONLY`.

### Envelope, topologia e recursos da execução

Antes de qualquer implementação, a coordenadora fecha um envelope versionado
com autoridade, baseline, estado, objetivo, escopos positivo e negativo,
trabalho protegido, contratos e dependências, ownership, recursos mutáveis,
checks, revisores, evidência, rollback e condições de parada. Trabalho amplo
mantém também o `../../PLANS.md` vivo. Nenhum dos dois concede autoridade.

O envelope usa exatamente uma topologia operacional:

- `SAFE_PARALLEL`: lanes independentes e somente leitura ou totalmente
  isoladas, sem contrato ou recurso mutável compartilhado;
- `CONTRACT_FROZEN_PARALLEL`: contratos e dependências foram congelados antes
  de lanes isoladas com write sets disjuntos;
- `SINGLE_OWNER`: uma única conversa escreve o conjunto coerente enquanto
  lanes auxiliares permanecem read-only;
- `SEQUENTIAL_ONLY`: dependência, runtime, gate, decisão, isolamento ou risco
  exige ordem estrita.

A topologia não substitui `Parallel work`. O handoff continua aceitando apenas
`SEQUENTIAL_ONLY`, `PARALLEL_OPTIONAL` ou `PARALLEL_RECOMMENDED`; as duas
classificações devem ser coerentes. Sem autorização específica de branches e
worktrees, qualquer topologia com análise paralela conserva `SINGLE_OWNER` ou
writers sequenciais na coordenadora.

Cada artefato é classificado como `AUTHORITY`, `CURRENT_FACT`, `HISTORY`,
`EVIDENCE`, `PLAN`, `IMPLEMENTATION` ou `GENERATED`. Cada recurso mutável —
arquivo, contrato, schema, migration, lockfile, branch, worktree, banco,
índice, porta, processo, runtime, temporário, cache, output ou recurso externo
— possui um único owner. A classificação não reduz a proteção da fonte
proprietária nem permite edição manual de artefato gerado ou histórico.

Usar stop codes canônicos nos bloqueios operacionais:

- `AUTHORITY_MISMATCH`: autoridade ausente, conflitante ou insuficiente;
- `BASELINE_DRIFT`: branch, commit, worktree ou contrato divergiu da baseline;
- `SCOPE_OVERLAP`: path, artefato, requisito ou writer sobrepõe outro escopo;
- `DEPENDENCY_UNREADY`: ferramenta, contrato, pacote ou entrada obrigatória
  não está pronta;
- `ISOLATION_FAILURE`: worktree, processo, porta, banco, cache ou output não
  pode ser isolado;
- `MUTABLE_RESOURCE_COLLISION`: duas atividades atingem o mesmo recurso;
- `GATE_FAILURE`: check obrigatório produziu falha factual;
- `EXTERNAL_AUTHORITY_REQUIRED`: ação externa exige autorização própria;
- `HUMAN_DECISION_REQUIRED`: ADR, Human Gate ou outra decisão humana permanece
  pendente.

Stop code preserva a causa original, impede retry corretivo silencioso e não
autoriza descarte, ampliação de escopo ou substituição de evidência.

## 3. Contrato do handoff governado

Todo handoff governado deverá apresentar exatamente estes 14 campos, nesta
ordem. O rótulo visível fica em `pt-BR`; a chave canônica inglesa permanece
entre parênteses para validação técnica:

```text
Situação (`Status`):
Concluído (`Completed`):
Restante para este objetivo (`Remaining for this target`):
Próximo passo (`Next step`):
Próxima etapa (`Next stage`):
Sua ação agora (`Your action now`):
Ação da conversa (`Conversation action`):
Destino da conversa (`Conversation target`):
Título sugerido (`Suggested title`):
Motivo da conversa (`Conversation reason`):
Próxima mensagem exata (`Exact next message`):
Trabalho paralelo (`Parallel work`):
Plano paralelo (`Parallel plan`):
Mensagens paralelas exatas (`Exact parallel messages`):
```

### Recomendação obrigatória de raciocínio do Codex

Em todo handoff real, `Sua ação agora` (`Your action now`) deverá começar com
exatamente uma recomendação para a próxima interação, no seguinte formato:

```text
Raciocínio recomendado do Codex: <nível> (`<identificador técnico>`). Motivo: <razão específica>.
```

Usar somente o catálogo abaixo:

| Nível apresentado ao proprietário | Nome em superfícies gráficas do Codex | `model_reasoning_effort` | Uso orientativo |
|---|---|---|---|
| `Leve` | `Light` | `low` | Tarefa rápida, determinística e bem delimitada, como consulta factual estável, status, encerramento seguro ou inspeção focal de baixo risco. |
| `Médio` | `Medium` | `medium` | Opção equilibrada para a maioria das tarefas focais, com requisitos claros, algum planejamento e validação moderada. |
| `Alto` | `High` | `high` | Implementação, diagnóstico ou revisão não trivial, com lógica complexa, múltiplos componentes, premissas ou edge cases relevantes. |
| `Extra alto` | `Extra High` | `xhigh` | Trabalho difícil, longo ou multietapas, como integração, segurança, arquitetura ou auditoria que exija várias fontes, trade-offs e rechecagem. |
| `Máximo` | `Max` | `max` | Problema excepcionalmente difícil tratado como uma única tarefa, quando profundidade importa mais que tempo ou tokens e não há decomposição independente útil. |
| `Ultra` | `Ultra` | `ultra` | Tarefa grande e complexa divisível em frentes significativas e independentes, quando subagentes trazem ganho material e a superfície, o modelo e a conta são elegíveis. |

Selecionar o menor esforço suficiente para o objetivo seguinte. Maior esforço
pode aumentar tempo e consumo de tokens e não garante, por si só, melhor
resultado. Reavaliar o nível a cada handoff; não transportar automaticamente a
recomendação da conversa atual para outra conversa ou lane.

A disponibilidade dos níveis depende da superfície, do modelo e da conta. Não
afirmar que um nível está disponível, selecionado ou aplicado sem evidência da
superfície vigente. Quando a opção ideal puder estar indisponível, declarar na
mesma orientação um fallback proporcional, sem transformar uma faixa de níveis
em recomendação principal.

A recomendação é consultiva e independente de `Conversation action`, `Parallel
work`, escopo, autoridade, ownership, preflight, Quality Gates, revisão humana,
ADR, Human Gate, `ActivationState` e lifecycle. `Ultra` não cria trabalho
paralelo autorizado, e `PARALLEL_RECOMMENDED` não exige `Ultra`. Sob
`SEQUENTIAL_ONLY`, nenhuma escolha permite writers concorrentes; eventuais
subagentes permanecem somente leitura e a integração continua central quando
essas atividades forem permitidas.

Em plano paralelo, indicar separadamente o nível da coordenadora e de cada
lane. Cada mensagem governada de conversa auxiliar deverá repetir, antes dos 19
campos existentes, a recomendação e a razão específicas daquela lane em uma
frase de preâmbulo não canônica, que não cria campo adicional.

Esta matriz aplica a orientação oficial de usar o menor esforço suficiente e
preserva a distinção entre profundidade individual e decomposição com
subagentes. Referências oficiais vigentes na adoção:
[configuração de modelos do Codex](https://learn.chatgpt.com/docs/models) e
[subagentes do Codex](https://learn.chatgpt.com/docs/agent-configuration/subagents).

`Próxima mensagem exata` (`Exact next message`) deve conter sempre uma única
mensagem completa, específica, preenchida, em `pt-BR` e pronta para o
proprietário copiar e enviar literalmente na conversa indicada. Essa obrigação
permanece quando o objetivo estiver concluído, parcial ou bloqueado e quando
nenhuma ação adicional de projeto for conhecida.

Esse campo nunca aceita valor vazio, placeholder, lista de alternativas,
sugestão abstrata ou
``Não se aplica (`None`) — nenhuma mensagem é necessária``. Quando não houver
ação adicional de projeto, fornecer uma mensagem segura de confirmação ou
encerramento que declare expressamente não autorizar nova ação.

A mensagem deverá ser coerente com `Próximo passo` (`Next step`),
`Sua ação agora` (`Your action now`), `Ação da conversa`
(`Conversation action`) e `Destino da conversa` (`Conversation target`).
`Sua ação agora` deverá orientar o proprietário a copiar e enviar essa
mensagem e explicar o resultado esperado, depois da recomendação de raciocínio
obrigatória.

Uma mensagem pronta para copiar não constitui decisão do proprietário antes de
ser efetivamente enviada e nunca presume, fabrica ou amplia aprovação,
autoridade, Human Gate, ADR, `ActivationState`, lifecycle, operação Git ou ação
externa. Quando uma decisão formal ainda estiver pendente, a mensagem solicita
a apresentação ou revisão do pacote decisório; ela somente expressa um
resultado de decisão quando o proprietário já o tiver escolhido
inequivocamente no contexto vigente.

Quando `START_NEW` não for escolhido, `Título sugerido` (`Suggested title`)
deve começar por ``Não se aplica (`None`) —`` e declarar concretamente por que
não há título novo. Um título sugerido nunca é apresentado como título
existente.

Quando o trabalho for sequencial:

- `Plano paralelo` (`Parallel plan`) deve começar por
  ``Não se aplica (`None`) —`` e conter a razão concreta;
- `Mensagens paralelas exatas` (`Exact parallel messages`) deve ser
  ``Não se aplica (`None`) — o trabalho paralelo não é recomendado``.

Placeholders podem existir somente nos templates normativos. Nenhum placeholder
pode permanecer num handoff, plano ou mensagem real.

## 4. Plano paralelo obrigatório

Um plano paralelo deverá possuir:

1. uma única conversa coordenadora identificada por título ou label confirmado
   pelo proprietário ou, na orquestração interna, por identificador canônico
   fornecido pela plataforma;
2. baseline comum identificada pela versão do corpus e, quando existir, commit
   ou hash;
3. lanes ou workstreams numerados;
4. objetivo e resultado esperado de cada lane;
5. nível de raciocínio recomendado para a coordenadora e para cada lane, com
   razão específica;
6. dependências congeladas, acíclicas e explicitadas;
7. ownership exclusivo de paths, artefatos lógicos e recursos mutáveis;
8. inputs compartilhados somente leitura;
9. arquivos, recursos e ações proibidos para cada lane;
10. checks e evidências esperadas;
11. condições objetivas de parada;
12. mensagem completa para iniciar cada conversa worker;
13. mensagem de retorno pronta para copiar à coordenadora;
14. ordem determinística de integração;
15. checks transversais após a integração;
16. fallback explícito para execução sequencial.

Sem identificação confiável da coordenadora, baseline estável, ownership
exclusivo ou fallback sequencial, o trabalho é `SEQUENTIAL_ONLY`. Somente um
título ou label confirmado pelo proprietário permite `RETURN_TO_EXISTING`.

## 5. Ownership exclusivo e isolamento

Deve existir somente um writer para cada:

- arquivo ou diretório;
- contrato ou schema;
- migration;
- lockfile;
- manifesto;
- solution ou project file;
- configuração ou pipeline;
- banco, índice ou corpus mutável;
- porta, processo ou runtime;
- temporário, cache ou output de build;
- recurso externo;
- artefato lógico compartilhado.

O ownership usa a menor granularidade segura. Se uma lane possuir um diretório,
nenhuma outra lane poderá escrever num descendente. Artefatos logicamente
acoplados continuam indivisíveis mesmo quando atravessam vários paths.

Estado, histórico, changelog do corpus, ADRs, relatórios e decisões de gate,
decisões permanentes e integração pertencem exclusivamente à conversa
coordenadora. Inputs compartilhados por workers permanecem somente leitura.

Uma worker concluída entrega apenas um candidato para integração. Ela não
declara o lote, estado, gate ou projeto como concluído.

## 6. Git, branches, worktrees e recursos mutáveis

Trabalho paralelo de escrita somente é elegível quando todas estas condições
forem satisfeitas:

1. o repositório estiver rastreado por Git;
2. o proprietário tiver autorizado especificamente o workflow paralelo e as
   operações Git necessárias ao lote;
3. cada writer usar branch própria e worktree isolado próprio;
4. os write sets permanecerem sem sobreposição;
5. portas, processos, bancos, índices, temporários, caches e outputs estiverem
   isolados quando aplicável.

Git existente e a autorização permanente de commit local final não autorizam
branch, worktree, merge ou rebase. Branches diferentes no mesmo worktree não
constituem isolamento.

Sem workflow paralelo de escrita especificamente autorizado, conversas
simultâneas podem executar somente análise, pesquisa, revisão e auditoria
read-only. Toda alteração de arquivo ocorre sequencialmente na conversa
coordenadora.

Merge, rebase e qualquer outra operação de integração Git somente podem ser
executados pela coordenadora quando tiverem autoridade explícita própria.
Nenhuma autorização de workflow implica push, publicação, deploy, consumo pago
ou alteração externa.

## 7. Responsabilidades das workers

Cada conversa worker deverá:

- reler as instruções aplicáveis;
- confirmar baseline, autoridade e ownership antes de agir;
- trabalhar somente dentro da sua lane;
- aplicar least privilege;
- não integrar outras lanes;
- não atualizar estado, histórico ou changelog;
- não aceitar nem alterar ADR;
- não promover lifecycle ou ativação;
- não solicitar, confirmar ou registrar Human Gate;
- não executar ação externa não autorizada;
- informar arquivos, artefatos, recursos, checks, limitações e riscos;
- produzir uma mensagem exata de retorno para a coordenadora.

## 8. Condições obrigatórias de parada

A worker deverá parar antes de continuar quando detectar:

- arquivo, artefato ou recurso com ownership sobreposto;
- baseline alterada ou desatualizada;
- mudança concorrente inesperada;
- dependência ainda não integrada;
- contrato ou schema instável;
- colisão de porta, processo, banco, índice, cache, output ou runtime;
- decisão humana pendente;
- necessidade de ampliar escopo ou autoridade;
- falha de isolamento;
- conflito de integração;
- ação potencialmente irreversível.

Não utilizar last-write-wins, sobrescrita automática ou reversão de trabalho
alheio. A frente afetada deve ser preservada e retomada sequencialmente a partir
da última baseline validada. Essa retomada não autoriza reset, checkout
destrutivo, revert ou descarte de trabalho.

## 9. Responsabilidades da conversa coordenadora

A coordenadora deverá:

- manter escopo, autoridade e baseline;
- validar o plano antes de abrir workers;
- congelar dependências e ownership;
- integrar uma entrega por vez, na ordem definida;
- inspecionar cada resultado e resolver conflitos centralmente;
- executar checks locais após cada integração;
- executar todos os checks transversais sobre o resultado combinado;
- atualizar estado, histórico e relatórios uma única vez;
- apresentar Human Gate somente após integração e auditoria consolidadas.

Custódia e integração não concedem autoridade decisória. A coordenadora não
aceita ADR, não decide Human Gate e não promove lifecycle ou ativação em nome do
proprietário.

## 10. Mensagens governadas de conversa auxiliar

Cada mensagem paralela exata (`Exact parallel message`) real deverá começar
pela seguinte frase de preâmbulo não canônica, preenchida sem placeholders:

```text
Para esta lane, recomenda-se usar o raciocínio <nível> (`<identificador técnico>`) do Codex porque <razão específica>.
```

Essa frase não é um campo do contrato. Em seguida, a mensagem deverá conter os
19 campos existentes:

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

A mensagem de retorno deverá identificar a frente, a base efetivamente usada,
a situação, o candidato produzido, os arquivos, artefatos e recursos tocados,
as verificações e evidências, as limitações, os riscos, as condições de parada
acionadas e a mensagem exata que deve ser enviada à coordenadora.

## 11. Documentação, validação e gates

[`Templates.md`](../templates/Templates.md) é
proprietário dos formatos reutilizáveis.
[`Quality-Gates.md`](Quality-Gates.md) é
proprietário dos critérios de verificação. Esses documentos especializam esta
autoridade sem criar uma segunda política.

Após mudanças de coordenação, executar auditoria proporcional que cubra:

- inventário de arquivos;
- links locais;
- UTF-8, LF, newline final e trailing whitespace;
- consistência dos enums e campos;
- ausência de placeholders em handoffs e mensagens reais;
- segurança, ausência de secrets e sanitização;
- autoridade, escopo positivo e escopo negativo;
- baseline, ownership e isolamento;
- coerência entre versão, estado, histórico e relatório;
- revisão semântica independente sobre baseline congelada;
- confirmação de que paralelismo não altera lifecycle, ADRs ou Human Gates.

Não declarar aprovação até os checks aplicáveis passarem. Uma falha de gate
interrompe a integração e é relatada factualmente; não autoriza correção fora do
escopo.

## 12. Controle de mudanças desta política

Antes de modificar esta autoridade ou a sua integração no corpus:

1. ler as instruções aplicáveis, entrada, governança, estado, histórico,
   templates, Quality Gates, ADRs e documentação relacionada;
2. identificar conflitos, duplicações e documentos proprietários;
3. apresentar arquivos, finalidade, autoridade temática, ordem, versão e
   riscos;
4. obter a autoridade exigida antes da escrita;
5. atualizar changelog, estado e histórico somente quando a mudança ocorrer;
6. preservar segurança, Human Gates, ADRs e limites de autoridade.

O handoff da mudança deverá apresentar arquivos alterados, política adotada,
situações paralelas e sequenciais, limitações, checks, riscos residuais,
recomendação de conversa, recomendação de paralelismo e mensagens exatas.
