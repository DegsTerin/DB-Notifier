# DB-Notifier — Coordenação de Conversas e Trabalho Paralelo Seguro

- Status: autoridade temática normativa
- Revisão: `1.0.0`
- Versão de introdução no corpus: `6.2.0`
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
4. fornecer a mensagem exata que o proprietário deverá enviar;
5. classificar se várias conversas podem trabalhar simultaneamente;
6. impedir que o paralelismo autorizado produza writers sobrepostos,
   sobrescrita, integração ambígua ou mudança fora de autoridade.

[`prompts/governance/Governance.md`](prompts/governance/Governance.md)
permanece proprietário da autoridade, execução controlada, lifecycle, bloqueio
e memória. Este documento especializa somente a coordenação de conversas e o
trabalho paralelo. Segurança, Human Gates, ADRs, estado factual e autorizações
externas conservam seus proprietários existentes.

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

## 3. Contrato do handoff governado

Todo handoff governado deverá apresentar estes campos, exatamente nesta ordem:

```text
Status:
Completed:
Remaining for this target:
Next step:
Next stage:
Your action now:
Conversation action:
Conversation target:
Suggested title:
Conversation reason:
Exact next message:
Parallel work:
Parallel plan:
Exact parallel messages:
```

`Exact next message` deve conter uma mensagem completa, preenchida e pronta
para copiar. Quando nenhuma mensagem for necessária, usar exatamente
`None — no message is required`.

Quando `START_NEW` não for escolhido, `Suggested title` deve começar por
`None —` e declarar concretamente por que não há título novo. Um título
sugerido nunca é apresentado como título existente.

Quando o trabalho for sequencial:

- `Parallel plan` deve começar por `None —` e conter a razão concreta;
- `Exact parallel messages` deve ser
  `None — parallel work is not recommended`.

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
5. dependências congeladas, acíclicas e explicitadas;
6. ownership exclusivo de paths, artefatos lógicos e recursos mutáveis;
7. inputs compartilhados somente leitura;
8. arquivos, recursos e ações proibidos para cada lane;
9. checks e evidências esperadas;
10. condições objetivas de parada;
11. mensagem completa para iniciar cada conversa worker;
12. mensagem de retorno pronta para copiar à coordenadora;
13. ordem determinística de integração;
14. checks transversais após a integração;
15. fallback explícito para execução sequencial.

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

## 10. Mensagens governadas de worker

Cada `Exact parallel message` real deverá conter:

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

A mensagem de retorno deverá identificar lane, baseline efetivamente usada,
status, candidato produzido, arquivos, artefatos e recursos tocados, checks e
evidências, limitações, riscos, condições de parada acionadas e a mensagem
exata que deve ser enviada à coordenadora.

## 11. Documentação, validação e gates

[`prompts/templates/Templates.md`](prompts/templates/Templates.md) é
proprietário dos formatos reutilizáveis.
[`prompts/governance/Quality-Gates.md`](prompts/governance/Quality-Gates.md) é
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
