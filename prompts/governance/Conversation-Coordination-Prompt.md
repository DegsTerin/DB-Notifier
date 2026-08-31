# DB-Notifier — Coordenação Autônoma e Trabalho Paralelo Seguro

- Status: autoridade temática normativa
- Revisão: `2.1.0`
- Versão de introdução no corpus: `6.2.0`
- Versão desta revisão no corpus: `9.1.0`
- Projeto: `DB-Notifier`
- Workspace: raiz confirmada do repositório ou do worktree atribuído à tarefa

Este documento foi adaptado no próprio local a partir da fonte fornecida pelo
proprietário, cujo SHA-256 anterior à incorporação era
`0019950242314908762CAD3E2AEA01C122023E3867885289E04FB3A70CA912D4`.

## Finalidade, autoridade e limites

Este documento é a autoridade temática única para:

1. continuar automaticamente na tarefa atual;
2. delegar uma frente a subagente;
3. retornar a uma tarefa existente confirmada pela plataforma;
4. criar e despachar uma nova tarefa quando necessário;
5. registrar receipt, deduplicação e resultado de cada despacho;
6. classificar paralelismo, ownership e isolamento;
7. impedir writers sobrepostos, sobrescrita e integração ambígua;
8. fechar o envelope operacional com baseline, escopo, recursos, checks,
   revisores e stop codes.

[`Governance.md`](Governance.md) permanece proprietário da autoridade,
execução controlada, lifecycle, bloqueio e memória. Segurança, identidade de
usuário, RBAC, confirmação administrativa do produto, limites jurídicos,
clean-room, destrutivos e pré-requisitos externos conservam seus proprietários.

Human Gates de desenvolvimento e navegação manual são prospectivamente
substituídos por Agent Gates objetivos e despacho direto. Decisões e relatórios
históricos permanecem inalterados.

Esta política não autoriza por si só ação externa, exposição de secret,
operação destrutiva insegura, publicação, deploy, push ou acesso fora do escopo.
Na dúvida sobre isolamento, usar `SEQUENTIAL_ONLY`.

Neste documento, `worker` ou `subagente` designa uma frente de engenharia. O
termo `Agent` do produto DB-Notifier e MOD-12 permanecem regidos por suas
autoridades próprias.

## 1. Rotas autônomas

Todo próximo trabalho usa exatamente uma rota:

- `CONTINUE_CURRENT`
- `DELEGATE_SUBAGENT`
- `RETURN_TO_EXISTING`
- `START_NEW_AUTO_DISPATCH`

### `CONTINUE_CURRENT`

Usar quando o objetivo ou lote continua ativo e o contexto permanece
confiável. Atualizar o plano e executar sem encerrar apenas para produzir um
handoff.

### `DELEGATE_SUBAGENT`

Usar para frente concreta, independente e verificável. Enviar diretamente o
payload completo, acompanhar, revisar e integrar o candidato.

### `RETURN_TO_EXISTING`

Usar somente com identificador factual fornecido pela plataforma. Reconciliar
corpus, baseline, estado e autoridade antes do despacho.

### `START_NEW_AUTO_DISPATCH`

Usar quando uma nova tarefa durável for necessária. Identificar projeto e
ambiente corretos, criar, enviar o payload, acompanhar e integrar.

O proprietário não navega, cria tarefa, escolhe destino nem encaminha mensagem.
Não produzir título sugerido, `Exact next message`, copy box ou instrução para
copiar e colar.

## 2. Receipt, deduplicação e veracidade

Cada rota registra internamente:

- ID e chave de deduplicação;
- origem e destino;
- rota;
- projeto, versão do corpus e baseline;
- objetivo e escopo;
- horário e ferramenta;
- resultado factual da ferramenta;
- estado atual e cursor quando aplicável.

Em melhoria contínua, a chave de deduplicação do despacho é o dispatch key
estável definido em [`Continuous-Improvement.md`](Continuous-Improvement.md),
derivado do execution key, rota, origem e destino exatos. Trocar rótulo,
timestamp ou conversa não cria novo intento nem autoriza retry do candidato.

`CONTINUE_CURRENT` usa um registro local factual no plano e não inventa
ferramenta ou receipt. `DELEGATE_SUBAGENT`, `RETURN_TO_EXISTING` e
`START_NEW_AUTO_DISPATCH` exigem confirmação factual da ferramenta. Nunca
declarar criação, envio, retomada ou conclusão sem a evidência correspondente.
Resultado incerto de ferramenta exige reconciliação antes de retry. A mesma
chave de deduplicação identifica o mesmo intento lógico e impede envio
duplicado.

## 3. Payload interno obrigatório

Todo despacho contém:

1. projeto e workspace autorizado;
2. baseline e versão do corpus;
3. estado, lote ou validação proprietária;
4. objetivo exclusivo e resultado esperado;
5. autoridade vigente;
6. escopo positivo;
7. escopo negativo e trabalho protegido;
8. dependências congeladas;
9. paths, artefatos e recursos com ownership exclusivo;
10. entradas somente leitura;
11. ações proibidas;
12. checks, evidência e critérios de aceite;
13. condições de parada e rollback;
14. formato estruturado de retorno.

O payload é enviado diretamente. Não é exibido como texto para o proprietário
copiar ou encaminhar.

## 4. Avaliação de paralelismo

Usar exatamente um valor:

- `SEQUENTIAL_ONLY`
- `PARALLEL_OPTIONAL`
- `PARALLEL_RECOMMENDED`

`SEQUENTIAL_ONLY` aplica-se quando houver dependência, contrato instável,
writer ou recurso compartilhado, runtime comum, gate, isolamento insuficiente
ou risco de conflito. Trabalho com runtime DB-Notifier é sequencial por padrão.

`PARALLEL_OPTIONAL` aplica-se a frentes independentes cujo ganho esperado seja
pequeno. `PARALLEL_RECOMMENDED` exige duas ou mais frentes independentes,
limitadas, verificáveis e com ganho material.

Usar o menor número útil de agentes. Maior quantidade não constitui progresso
nem consenso.

## 5. Envelope, topologia e recursos

Antes de implementação, a coordenadora fecha envelope versionado com
autoridade, baseline, objetivo, escopos, trabalho protegido, contratos,
ownership, recursos, checks, revisores, evidência, rollback e stop conditions.
Trabalho amplo mantém `PLANS.md` vivo. Nenhum deles substitui autoridade.

Usar uma topologia:

- `SAFE_PARALLEL`: lanes read-only ou totalmente isoladas;
- `CONTRACT_FROZEN_PARALLEL`: contratos congelados e write sets disjuntos;
- `SINGLE_OWNER`: um writer e lanes auxiliares read-only;
- `SEQUENTIAL_ONLY`: ordem estrita.

Cada artefato é `AUTHORITY`, `CURRENT_FACT`, `HISTORY`, `EVIDENCE`, `PLAN`,
`IMPLEMENTATION` ou `GENERATED`. Cada arquivo, contrato, schema, migration,
lockfile, branch, worktree, banco, índice, porta, processo, runtime, temporário,
cache, output ou recurso externo possui um único owner.

## 6. Stop codes e disposições

Usar stop codes factuais:

- `AUTHORITY_MISMATCH`
- `BASELINE_DRIFT`
- `SCOPE_OVERLAP`
- `DEPENDENCY_UNREADY`
- `ISOLATION_FAILURE`
- `MUTABLE_RESOURCE_COLLISION`
- `GATE_FAILURE`
- `EXTERNAL_AUTHORITY_REQUIRED`
- `EXTERNAL_PREREQUISITE`
- `BLOCKED_BY_HIGHER_AUTHORITY`

Estados de decisão e conclusão:

- `AGENT_DECIDED`
- `AUTOMATED_GATE_PASS`
- `AUTOMATED_GATE_FAIL`
- `LOCAL_COMPLETE`
- `EXTERNAL_PREREQUISITE`
- `BLOCKED_BY_HIGHER_AUTHORITY`

Stop code preserva a primeira causa factual, não autoriza descarte ou ampliação
de escopo e não converte `FAIL`, `BLOCKED`, `PARTIAL` ou `NOT_RUN` em `PASS`.

## 7. Ownership e Git

Somente um writer pode possuir cada boundary. Ownership de diretório exclui
outro writer em qualquer descendente. Artefatos logicamente acoplados continuam
indivisíveis mesmo quando atravessam vários paths.

Estado, histórico, changelog, ADRs, relatórios, Agent Gates e integração
pertencem à coordenadora. Inputs compartilhados por workers são read-only.

Escrita paralela exige Git rastreado, workflow autorizado, branch e worktree
isolados por writer, write sets disjuntos e isolamento de recursos. Sem essas
condições, agentes paralelos são read-only e a coordenadora escreve
sequencialmente. Nenhuma autorização local implica push, publicação ou deploy.

## 8. Responsabilidades dos workers

Cada worker:

- relê as instruções aplicáveis;
- confirma baseline, autoridade, ownership e negative scope;
- trabalha somente na lane;
- aplica least privilege;
- não integra outras lanes;
- não atualiza estado, histórico ou changelog;
- não altera decisão permanente;
- não executa ação externa não autorizada;
- entrega candidato, arquivos, recursos, checks, evidência, limitações e riscos.

Uma worker não declara lote, gate, estado ou projeto como concluído.

## 9. Condições de parada

A frente afetada para diante de ownership sobreposto, baseline drift, mudança
concorrente, dependência não integrada, contrato instável, colisão de recurso,
falha de isolamento, conflito de integração, risco destrutivo ou autoridade
superior. Preservar o estado e continuar qualquer trabalho independente seguro.

Não usar last-write-wins, reset, checkout destrutivo, sobrescrita automática ou
reversão de trabalho alheio.

## 10. Responsabilidades da coordenadora

A coordenadora:

- mantém objetivo, escopo, autoridade e baseline;
- congela dependências, ownership e recursos;
- despacha e acompanha workers;
- integra uma entrega por vez;
- executa checks locais e transversais;
- decide ADRs locais e lifecycle após `AUTOMATED_GATE_PASS`;
- registra fatos uma única vez;
- continua automaticamente para o próximo lote seguro.

## 11. Fallback de despacho

Se uma rota ou ferramenta estiver indisponível:

1. continuar na tarefa atual;
2. delegar subagente interno;
3. retornar a tarefa existente confirmada;
4. manter o trabalho na fila coordenada;
5. registrar `EXTERNAL_PREREQUISITE` somente quando nenhuma continuação segura
   for tecnicamente possível.

A indisponibilidade nunca produz texto para o proprietário copiar.

## 12. Agent Gate e revisão independente

Cada candidato recebe revisão independente proporcional ao risco, com achados
`P0`–`P3`. `PASS` exige zero `P0` e zero `P1`, todos os checks obrigatórios em
`PASS`, baseline exata, evidência sanitizada e rollback adequado. Risco elevado
de arquitetura, segurança, persistência ou compatibilidade recebe segunda
revisão independente.

Human Gates anteriores permanecem história. O Agent Gate atual não os apaga,
ratifica ou reinterpreta.

## 13. Limites destrutivos e externos

Despacho não cria autoridade. Uma mutação local destrutiva exige Automated
Safety Gate com alvo exato e resolvido, rejeição de raiz/home/caminho amplo,
variável, glob ou identificador não resolvido, WIP preservado, checkpoint,
rollback, ausência de alternativa mais segura, necessidade objetiva e revisão
independente.

Ação externa exige cumulativamente autoridade aplicável, credencial existente
e atualmente válida por mecanismo seguro, conta e ambiente exatos, ferramenta
disponível e apta, limite de custo aplicável, critério de sucesso e verificação
segura ou reversão. A falta de qualquer item aciona fallback local e, somente
quando nada material restar, `EXTERNAL_PREREQUISITE`.

## 14. Comunicação com o proprietário

Comunicar somente o resultado consolidado ou uma dependência externa inevitável
quando não houver progresso material possível. A comunicação usa `pt-BR` e
informa fatos, validações, limitações e riscos; não pede preferência técnica,
aprovação rotineira, navegação, criação de tarefa ou encaminhamento de payload.

## 15. Integração com melhoria contínua

Um evento de finding não cria automaticamente uma tarefa ou writer. A
coordenadora deduplica o fingerprint no backlog factual, escolhe uma única rota
e despacha no máximo uma ação bounded. `ATTEMPT_STARTED` exige ownership e
candidate digest exatos; um `GATE_FAILED` retorna primeiro à análise causal, não
à repetição do mesmo payload.

Implementador, revisores, integrador e observador seguem a separação de roles
da autoridade de melhoria contínua. Receipt de despacho, evento de ledger e
resultado de gate são evidências distintas e não se substituem.

## 16. Validação e mudança desta política

Após alteração, validar:

- versão, links e fontes proprietárias;
- ausência de requisitos de copy-and-paste;
- rotas, receipts, deduplicação e fallback;
- ownership, isolamento e stop codes;
- estados de Agent Gate;
- preservação de autenticação/RBAC do produto, segurança, clean-room, histórico
  e limites externos;
- revisão semântica independente.

Uma falha interrompe a integração e permanece factual. Mudanças desta política
atualizam o changelog e o estado factual apenas depois do fato, sem reescrever
evidência anterior.
