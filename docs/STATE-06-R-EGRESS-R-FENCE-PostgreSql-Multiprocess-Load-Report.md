# STATE-06 — R-EGRESS/R-FENCE PostgreSQL Multiprocess and Load Report

## Status

`PASS` automático e local para a campanha física test-only R-EGRESS/R-FENCE. Os oito cenários, os dois perfis
bounded `4 × 25`, a validação sanitizada do resumo `schemaVersion=2` e a limpeza final terminaram sem falha não
classificada, deadlock ou resíduo próprio.

Este resultado não é Human Gate, não muda lifecycle nem `ActivationState` e não autoriza PostgreSQL operacional,
homologação, SLO, fairness, prioridade, runtime externo ou ação de produto.

## Relação com a evidência histórica

Este relatório complementa, sem reescrever:

- o
  [relatório histórico R-EGRESS](STATE-06-R-EGRESS-Observation-Ingestion-Revocation-Linearisation-Report.md),
  que provou deterministicamente as duas ordens no processo e protegeu por inspeção a ordem do row lock PostgreSQL;
  e
- o
  [relatório histórico R-FENCE](STATE-06-R-FENCE-Agent-Identity-Fence-Lifecycle-Regression-Report.md),
  que provou cancelamento, liberação excepcional, descarte idempotente e isolamento do gate process-local.

Esses relatórios mantêm os seus resultados e limites originais. A campanha deste relatório acrescenta a evidência
física que eles deixavam pendente: processos independentes disputando a mesma identidade e um PostgreSQL real
descartável, sem transformar esse laboratório em suporte operacional.

## Autoridade e limites

Incluído:

- host, testes e runner estritamente test-only para R-EGRESS/R-FENCE;
- PostgreSQL descartável, limitado a loopback e identificado como propriedade exclusiva da campanha;
- processos independentes de ingestão e revogação;
- prova de blocking PostgreSQL, ordens de commit, crash, cancelamento, isolamento entre Agents e carga bounded;
- métricas descritivas e evidência sanitizada;
- documentação técnica proprietária e gates locais aplicáveis.

Excluído:

- qualquer PostgreSQL existente, monitorado, partilhado ou operacional;
- migration operacional, produção, deploy, publicação, push ou pull request;
- provider/database target real, credencial real, IdP, PKI, canal externo ou infraestrutura remota;
- lifecycle, Human Gate ou mudança de `ActivationState`;
- promessa ou definição de SLO, fairness, prioridade ou latência máxima do produto.

Migrations podem existir somente dentro do banco descartável criado para a fixture. Elas não constituem autorização
nem evidência de migration operacional.

## Objetivo de segurança

A campanha provou fisicamente que a fronteira durável mantém a ordem por Agent quando o gate process-local não é
compartilhado:

1. uma ingestão que possui o lock PostgreSQL termina antes da revogação concorrente;
2. uma revogação que vence a ordem impede efeitos atribuíveis à observação recusada;
3. um conflito serializável é reclassificado pelo caminho produtivo fail-closed;
4. crash ou cancelamento não deixam lock, sessão, efeito parcial ou bloqueio permanente;
5. contenção de um Agent não bloqueia outro Agent; e
6. o perfil bounded termina sem deadlock, efeito contraditório ou resíduo próprio.

## Topologia test-only executada

- PostgreSQL descartável `postgres:16-alpine`, já disponível localmente e fixado pelo digest
  `sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb`, sem pull.
- Uma única porta dinâmica publicada em IPv4 loopback.
- Container, diretório temporário e quaisquer recursos auxiliares com `runId` aleatório e ownership labels exatas.
- Credencial sintética aleatória, sem reutilização e sem valor em argumentos, logs, relatório ou Git.
- IPC pai/filho por named pipes com `PipeOptions.CurrentUserOnly` para o segredo sintético, barreiras e comandos.
  Fechar o pipe no meio de um frame deve falhar fechado. Cada filho escreve exatamente um resultado sanitizado em
  `stdout`; o pai redireciona e valida integralmente esse fluxo antes de o expor.
- Cada operação de carga atribui SQLSTATE `40001` somente pelo delta observado durante aquele item; uma ocorrência
  anterior no mesmo processo não contamina os itens seguintes.
- `Application Name` único por participante, `Pooling=false` e timeouts limitados apenas às sessões da campanha.
- Nenhum processo filho pode herdar autoridade além do cenário e do Agent exatos que lhe foram atribuídos.

O runner comprovou a identidade de cada recurso antes de encerrá-lo ou removê-lo e preservou Docker Desktop, IDE,
browser, bancos e processos alheios.

## Prova física de blocking

Antes de liberar cada holder, o coordenador deve consultar `pg_blocking_pids()` a partir de uma sessão de observação
separada e provar:

- PID bloqueador e PID bloqueado pertencem aos `Application Name` test-only esperados;
- a relação é causada pela identidade do Agent e pela ordem transacional do cenário;
- nenhum participante alheio aparece na relação; e
- o waiter ainda não criou efeitos duráveis.

Foram observadas sete relações blocker/waiter válidas. Ausência da relação quando exigida ou timeout continuaram
classificados como reprovação, nunca como prova de blocking.

## Matriz de cenários

| ID | Cenário | Prova obrigatória | Resultado |
|---|---|---|---|
| `MP-01` | ingestão adquire `FOR NO KEY UPDATE` antes da revogação | `pg_blocking_pids()` mostra revogação bloqueada; ingestão termina `Accepted`; revogação termina depois; efeitos pertencem somente à observação aceita | `PASS` |
| `MP-02` | revogação principal vence antes da ingestão | ingestão não cria sample/state/event/outbox próprio e termina na classificação fail-closed aplicável | `PASS` |
| `MP-03` | conflito serializável | PostgreSQL produz SQLSTATE `40001`; o wrapper de execução preserva a causa de persistência e a reclassificação produtiva converge sem ampliar captura a `InvalidOperationException` arbitrária | `PASS` |
| `MP-04` | crash do holder | encerra somente o PID filho identificado; PostgreSQL libera sessão/lock; um sucessor conclui sem reparo manual ou efeito parcial | `PASS` |
| `MP-05` | cancelamento do waiter | cancelamento limitado interrompe a espera, cria zero efeito e não impede progresso posterior | `PASS` |
| `MP-06` | isolamento entre Agents | Agent B progride enquanto Agent A permanece bloqueado; nenhuma identidade, cursor ou efeito cruza Agents | `PASS` |
| `MP-07` | carga bounded no mesmo Agent | quatro processos executam 25 operações cada; exatamente 100 operações concluem sem deadlock ou efeito contraditório | `PASS` |
| `MP-08` | carga bounded distribuída | quatro processos executam 25 operações em Agents distintos e a corrida de revogação converge sob contenção | `PASS` |

## Perfil de carga

O perfil fixo é `4 × 25`: quatro processos e 25 operações por processo, totalizando 100 operações por perfil. Ele é
uma carga de caracterização test-only, não sizing, benchmark de produto ou SLO.

Resultados separados para mesmo Agent e Agents distintos:

| Métrica | Mesmo Agent | Agents distintos |
|---|---:|---:|
| operações concluídas | `100` | `100` |
| p50 | `105,4391 ms` | `127,4807 ms` |
| p95 | `334,8821 ms` | `275,6282 ms` |
| p99 | `4.195,9385 ms` | `3.474,3736 ms` |
| máximo | `4.239,3057 ms` | `5.364,1133 ms` |
| throughput observado | `11,3550 ops/s` | `11,6109 ops/s` |
| máximo de sessões bloqueadas | `3` | `1` |
| SQLSTATE `40001` | `38` | `45` |
| disposições `Retryable` | `0` | `67` |
| deadlocks | `0` | `0` |

O resumo fechado `schemaVersion=2` reteve os seguintes contadores globais:

| Campo | Valor |
|---|---:|
| cenários | `8` |
| observações de blocking | `7` |
| SQLSTATE `40001` | `86` |
| `Accepted` | `106` |
| `Rejected` | `27` |
| `Retryable` | `68` |
| convergências bounded após `Retryable` | `68` |
| deadlocks | `0` |
| falhas não classificadas | `0` |
| posição registrada da revogação | `0` |
| recursos residuais | `0` |

O valor zero da posição de revogação é o resultado factual desta execução, não promessa de prioridade. Contagens de
commits e rollbacks, duração específica de blocking, CPU e working set permaneceram fora do resumo fechado. Toda
classificação `Retryable` admitida pela matriz exigiu convergência bounded; `68/68` convergiram e nenhuma foi tratada
como sucesso terminal por si só.

As métricas só caracterizam esta máquina, imagem, digest, fixture e execução. Não permitem declarar fairness,
prioridade, starvation freedom ou qualquer SLO. Timeouts do harness são limites de segurança da campanha, não
contratos do produto.

## Invariantes de efeitos

Cada cenário deve reconciliar `MessageId`, `ObservationId`, Agent e sequência contra:

- `agents`;
- `agent_observation_cursors`;
- `rejected_observation_sequences`;
- `health_samples`;
- `instance_observation_states`;
- `events`;
- `outbox_messages`;
- `notification_deliveries`; e
- `audit_entries`.

A observação que perde para a revogação deve produzir zero sample/state/event/outbox próprio. Um sucessor
anteriormente aceito pode ser reconciliado pelas regras R-SEQ, mas a sua proveniência deve permanecer separada da
mensagem recusada.

## Gates e resultados

| Gate | Resultado |
|---|---|
| shutdown preflight da campanha | `PASS` — nenhum processo, listener ou janela DB-Notifier ativo |
| imagem/digest e engine locais comprovados sem pull | `PASS` — imagem/digest exatos já locais |
| build Release focal | `PASS` — zero warning e zero erro nos projetos diretamente afetados |
| cenários físicos `MP-01` a `MP-08` | `PASS` — `8/8` |
| perfil mesmo Agent `4 × 25` | `PASS` — `100/100`, zero deadlock |
| perfil Agents distintos `4 × 25` | `PASS` — `100/100`, zero deadlock e convergência bounded |
| regressões focais R-EGRESS/R-FENCE/R-SEQ | `PASS` — inclusive wrapper de persistência e durabilidade do ledger |
| suítes completas aplicáveis | `PASS` — `508/508` unitários, `96/96` arquitetura, `126/126` integração e `10/10` WPF |
| cobertura e pisos por componente | `PASS` — `83,35%` linhas, `56,27%` branches e `10/10` componentes obrigatórios acima dos seus pisos |
| documentação de código e links Markdown | `PASS` — `411` fontes comment-capable e `781` links locais em `203` arquivos |
| format e PowerShell estático | `PASS` — `dotnet format`, parser PowerShell e PSScriptAnalyzer |
| secret scan | `PASS` — worktree não ignorado e histórico Git disponível |
| validação de segredo do runner/resumo | `PASS` — saída redirecionada e resumo fechado recusam material de conexão |
| zero processo, listener, lock, sessão, container e diretório próprio residual | `PASS` — todos os contadores finais iguais a zero |

O piso obrigatório permanece 70% de linhas e 45% de branches, sem redução por componente. A meta de 80% de linhas
permanece orientativa e baseada em risco.

## Evidência sanitizada retida

Este relatório reteve somente:

- versões e digest públicos;
- IDs sintéticos e `Application Name` test-only;
- ordens, códigos, contagens e métricas;
- SQLSTATE, duração e relação blocker/waiter;
- hashes de artefactos não secretos;
- comandos sem conexão, credencial ou caminho sensível; e
- inventário final de resíduos igual a zero.

Connection strings, passwords, tokens, private keys, certificados privados, valores IPC e dumps de banco/processo
não podem aparecer em logs, relatório, screenshot, commit ou chat.

## Cleanup e falha

Todo caminho, incluindo cancelamento, crash ou falha de assert:

1. liberar as barreiras de teste ainda controláveis;
2. tentar encerrar todos os processos filhos comprovadamente pertencentes ao `runId`, acumulando falhas para nova
   tentativa em vez de abandonar os filhos restantes;
3. verificar ownership antes de remover recursos descartáveis;
4. remover credencial e diretório temporários somente após o fim dos owners;
5. confirmar ausência de locks/sessões `Application Name` da campanha; e
6. varrer host, marcador e token exclusivos do runner antes de comprovar zero processo, listener, container e recurso
   próprio residual.

A limpeza final comprovou `containers=0`, `networks=0`, `volumes=0`, diretórios temporários próprios `=0`, processos
DB-Notifier `=0` e listeners DB-Notifier `=0`. Uma limpeza incompleta teria reprovado a campanha mesmo com asserts
funcionais anteriores aprovados.

## Limites preservados

- Não existe SLO aprovado para latência, throughput, blocking ou revogação.
- O gate process-local continua sem promessa de fairness, prioridade ou timeout próprio.
- O perfil `4 × 25` não prova capacidade operacional, starvation freedom ou comportamento de longa duração.
- PostgreSQL loopback descartável não é PostgreSQL operacional, homologação, HA, failover, TLS/PKI ou produção.
- IPC `CurrentUserOnly` pertence somente ao harness; não define IPC de produto.
- Nenhuma migration operacional, transição de lifecycle, Human Gate ou mudança de `ActivationState` é autorizada.

## Resultado factual

O resultado automático da campanha é `PASS`: oito cenários, dois perfis bounded de 100 operações, `86` ocorrências
sanitizadas de SQLSTATE `40001`, `68/68` convergências após `Retryable`, zero deadlock, zero falha não classificada e
zero resíduo próprio. A campanha fecha a lacuna de evidência física multiprocesso R-EGRESS/R-FENCE somente para esta
imagem, máquina, fixture e execução test-only.

`STATE-06 INTEGRATION` e `ActivationState=None` permanecem inalterados. Esta campanha não é Human Gate nem transição
de lifecycle e não prova PostgreSQL operacional, homologação, SLO, fairness, prioridade ou starvation freedom.
