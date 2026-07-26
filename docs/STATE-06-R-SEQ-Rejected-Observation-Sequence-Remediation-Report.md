# STATE-06 — R-SEQ Rejected Observation Sequence Remediation Report

## Decisão

O lote focal `R-SEQ` está automaticamente `APROVADO` no escopo local
validado.

A progressão sequencial central agora distingue a classificação terminal do
payload da prova durável de resolução do seu slot. Uma observação rejeitada
somente libera o tombstone local depois que o cursor do Server cobre a sua
sequência. Consumir esse slot não cria amostra de saúde, estado de instância,
evento canônico, mensagem de outbox do Server nem delivery de notificação.

Esta decisão não altera `STATE-06 INTEGRATION`, não promove qualquer gate de
lifecycle e não modifica `ActivationState=None`.

## Autoridade e limites

O proprietário autorizou somente diagnóstico final e correção da progressão
sequencial após observações rejeitadas, componentes diretamente afetados,
testes de regressão e documentação técnica proprietária.

Permaneceram fora do lote:

- `PM-3`, `R-EGRESS` e qualquer lote posterior;
- dependências, migrations e aplicação de schema;
- runtime externo, provider ou PostgreSQL operacional;
- lifecycle, Human Gate de estado e `ActivationState`;
- push, pull request e deploy.

## Diagnóstico final

A causa foi comprovada pela combinação de três comportamentos:

1. `ObservationBatchIngestor` classificava falhas de validação como
   `Rejected` sem passar pelo armazenamento central;
2. `AgentOutboxDispatchRunner` e `AgentOutboxStore` tratavam todo `Rejected`
   como reconhecimento terminal local;
3. `ServerObservationIngestionStore` avançava o cursor somente por
   `health_samples` exatamente contíguas.

Assim, uma sequência `N` rejeitada podia desaparecer do outbox local sem
deixar prova central, enquanto uma amostra válida `N+1` já aceita permanecia
armazenada mas nunca projetada.

A limitação não era nova: o
[relatório E2E autoritativo de 2026-07-19](STATE-06-Authoritative-Observation-Pipeline-E2E-Sandbox-Report.md)
já registrava que uma recusa terminal criava um gap e que não havia protocolo
de skip/tombstone naquele incremento. Esse relatório histórico e sua
aceitação não foram reescritos.

## Correção implementada

### Resolução central sem migration

`IRejectedObservationSequenceStore` separa a capacidade de resolver uma
sequência rejeitada do contrato comum de ingestão. A composição normal do
Server implementa a capacidade no mesmo `AgentGate`, transação serializável e
cursor usados pela ingestão aceita.

Dentro da transação:

| Condição | Resultado | Mutação |
|---|---|---|
| prefixo aceito já armazenado | reconcilia primeiro | projeta somente as amostras válidas |
| sequência rejeitada `<= cursor` | `Rejected` idempotente | nenhuma nova evidência |
| sequência rejeitada `= cursor + 1` | consome o slot e reconcilia sucessores | avança o cursor sem `health_sample` da rejeição |
| sequência rejeitada `> cursor + 1` | `Retryable` | preserva o gap |
| Agent inexistente ou persistência não comprovada | `Retryable` | não cria cursor nem reconhecimento |

Agents conhecidos mas já inativos podem ter o slot da recusa consumido. Isso
registra somente a resolução terminal da stream e não reativa a identidade,
não aceita a observação e não produz efeito de saúde.

As recusas de instância não atribuída, provider incompatível e conflito
seguem a mesma barreira. Uma nova amostra válida em sequência já coberta pelo
cursor é rejeitada como conflito, impedindo evidência retroativa não
projetável.

### Reconhecimento local condicionado

O dispatcher compara cada envelope rejeitado com
`HighestContiguousSequence`. `Rejected` permanece terminal apenas quando o
high-water do Server cobre a sequência correspondente. Caso contrário, o
resultado aplicado ao outbox é `Retryable` com
`sync.rejection_unconfirmed`.

Essa barreira inclui:

- payload não suportado rejeitado localmente antes de qualquer requisição;
- HTTP não transitório que rejeita o request inteiro sem resposta de
  ingestão;
- rejeição acima de um gap ainda aberto.

Esses caminhos preservam o item em backoff. Eles não podem avançar o cursor
automaticamente porque o Server não recebeu prova suficiente do slot.

## Regressões adicionadas

As regressões focais provam:

- rejeição sem high-water permanece localmente retryable;
- rejeição coberta pelo high-water torna-se terminal;
- payload recusado localmente e HTTP `4xx` não são reconhecidos;
- sequência rejeitada libera uma amostra válida sucessora já armazenada;
- replay da rejeição é idempotente;
- tentativa válida retroativa no slot rejeitado é recusada;
- rejeição acima de gap fica retryable até o prefixo chegar;
- provider mismatch consome somente o slot e libera o sucessor;
- revogação em voo avança o cursor de `4` para `5`, mantendo exatamente quatro
  samples, quatro eventos e estado projetado na sequência `4`.

## Validação observada

Ambiente: Windows, SDK local .NET `10.0.301`, configuração `Release`, sem
restore e sem acesso operacional externo.

| Verificação | Resultado |
|---|---|
| Shutdown preflight inicial | zero processo, listener, janela ou runtime DB-Notifier identificado |
| Shutdown final | zero processo, listener ou janela de runtime; a janela comum do VS Code foi preservada |
| Build focal de Unit Tests | aprovado, zero avisos e zero erros |
| `SynchronizationTests` | `52/52` aprovados |
| E2E focal de replay/reorder/staleness/revogação | `1/1` aprovado |
| Build Release da solução | aprovado, zero avisos e zero erros |
| Suíte completa | `408/408` unitários, `124/124` integrações, `92/92` arquitetura e `10/10` WPF |
| Cobertura .NET | `82,09%` linhas, `54,26%` branches, `10/10` componentes obrigatórios presentes |
| `dotnet format --verify-no-changes --no-restore` | aprovado |
| Gate de documentação de código | aprovado para `382` arquivos comment-capable |
| Gate de links Markdown | aprovado após o registro documental final |
| Secret scan | aprovado no worktree não ignorado e histórico Git disponível |

O piso obrigatório de `70%` de linhas e `45%` de branches foi preservado. A
meta orientativa baseada em risco de `80%` de linhas também foi atingida.

## Riscos e limitações residuais

- Sem migration, o cursor prova que o slot foi resolvido, mas não persiste
  centralmente o `MessageId` nem o motivo original da rejeição.
- Gaps históricos que já foram reconhecidos antes de R-SEQ não podem ser
  reconstruídos automaticamente a partir do cursor atual.
- Um payload corrompido somente no Agent ou uma recusa de request sem resposta
  autoritativa permanece pendente e requer correção de configuração, identidade
  ou dado local; R-SEQ não introduz protocolo remoto de skip.
- A janela concorrente interna entre leitura de Agent `Active` e commit não
  pertence a R-SEQ; qualquer fencing adicional permanece no lote separado
  `R-EGRESS`.
- A prova de persistência central usou SQLite em memória nos testes e o E2E
  sandbox local. PostgreSQL serializável real, multiprocesso de Server e
  aplicação de migration não foram executados nem autorizados.
- A sincronização normal permanece desabilitada por padrão.

## Resultado factual

R-SEQ encerra, no escopo local validado, o bloqueio permanente causado por
uma sequência terminalmente rejeitada no pipeline central. O fechamento não
converte a rejeição em evidência de saúde e não autoriza runtime, provider,
produção, lifecycle ou ativação.
