# STATE-06 — R-EGRESS Observation Ingestion/Revocation Linearisation Report

## Decisão

O lote focal `R-EGRESS` está automaticamente `APROVADO` somente no escopo
local validado.

Ingestão autoritativa de observações e revogação principal do Agent agora
possuem uma única ordem por Agent. Se a ingestão obtém essa ordem primeiro,
ela deve commitar ou fazer rollback antes de a revogação principal poder
commitar. Se a revogação vence, nenhuma amostra, projeção de estado, evento
canônico, mensagem de outbox do Server ou delivery de notificação da
observação recusada pode commitar depois.

Esta decisão não altera `STATE-06 INTEGRATION`, não promove Human Gate ou
lifecycle e não modifica `ActivationState=None`.

## Autoridade, baseline e desambiguação

- Autoridade: execução integral, em uma única entrega, somente da corrida
  entre verificação de Agent ativo, revogação e commit da ingestão
  autoritativa de observações.
- Baseline inicial limpa: commit `c8d322e`.
- Data local: 2026-07-25.
- Permitido: shutdown preflight, diagnóstico, componentes diretamente
  afetados, regressões, checks completos aplicáveis, cobertura, documentação
  técnica proprietária, estado factual, log append-only, shutdown final e um
  commit local focal.
- Proibido e não executado: `PM-3`, dependência, migration, runtime externo,
  provider ou PostgreSQL operacional, lifecycle, `ActivationState`, push,
  pull request e deploy.

Neste lote, por instrução explícita vigente, `R-EGRESS` designa exclusivamente
a serialização `Active`/revogação/commit da ingestão. SSRF e política de
network egress não foram auditados, corrigidos nem encerrados.

## Preflight obrigatório

Antes da primeira ação técnica:

- processos DB-Notifier pertencentes ao projeto: `0`;
- listeners pertencentes ao projeto: `0`;
- janelas de runtime DB-Notifier: `0`;
- janela comum do VS Code preservada: `1`;
- worktree: limpa em `c8d322e`.

Nenhum navegador comum, IDE, database engine, serviço monitorado ou processo
alheio foi encerrado.

## Diagnóstico final

A autenticação mTLS e a ingestão consultavam a identidade em momentos
separados. Dentro da ingestão, a transação serializável lia `agents.state`,
mas não bloqueava a linha da identidade. O único row lock PostgreSQL
pertencia ao cursor de observações, enquanto a revogação atualizava outra
linha, `agents`.

Assim, esta interleaving era possível:

1. ingestão lia `State = Active`;
2. revogação atualizava `State`, `RevokedAt` e `ConcurrencyToken`, e commitava;
3. ingestão criava amostra e efeitos derivados, e commitava depois.

`Serializable` não criava por si só um conflito obrigatório entre essas duas
transações, porque a ingestão não bloqueava nem atualizava a identidade. O
gate estático anterior também protegia somente writers do próprio
`ServerObservationIngestionStore`; a revogação não participava dele.

A limitação já estava corretamente preservada no
[relatório E2E autoritativo de 2026-07-19](STATE-06-Authoritative-Observation-Pipeline-E2E-Sandbox-Report.md):
a barreira histórica pausava antes da entrada no store e não disputava a
janela pós-leitura `Active`/pré-commit. Esse relatório e sua decisão humana não
foram reescritos.

## Correção implementada

### Ordem comum por Agent

`AgentIdentityTransactionFence` tornou o antigo gate de ingestão uma
capacidade compartilhada por:

- ingestão validada;
- consumo central de sequência rejeitada;
- commit da revogação principal.

Cada lease é cancelável, liberado em todo caminho de sucesso ou falha e
remove a entrada ociosa do dicionário sem permitir que dois gates para o
mesmo Agent coexistam.

### Âncora durável no PostgreSQL

Dentro da transação serializável de ingestão, a identidade exata é lida com
`FOR NO KEY UPDATE`. Esse lock:

- permanece até commit ou rollback da ingestão;
- conflita com a atualização não-chave da revogação principal;
- permite que a própria transação preserve relações por chave;
- evita usar o cursor de observações como substituto indevido da autoridade
  da identidade.

Apenas `Npgsql.EntityFrameworkCore.PostgreSQL` usa esse caminho. O fallback
local admite exatamente `Microsoft.EntityFrameworkCore.Sqlite`, protegido
pelo gate compartilhado e pela transação serializável. O caminho de ingestão
aceita falha fechado para qualquer terceiro provider de persistência central.

### Decisão de autoridade e falha de serialização

A ingestão exige simultaneamente:

- `State == "Active"`;
- `RevokedAt == null`.

Um rótulo `Active` com timestamp de revogação não é aceito. Se uma falha de
persistência ou serialização ocorrer durante a disputa, o store:

1. abre uma transação serializável nova e bloqueia a identidade;
2. depois do lock, procura aceitação, duplicidade ou conflito já commitado;
3. se a revogação estiver comprovada e não houver aceitação concorrente,
   consome a sequência recusada na mesma transação pelas regras de R-SEQ;
4. se a persistência continuar incerta, retorna somente `Retryable`.

Autenticação concluída antes dessa transação continua sendo apenas um
prefiltro, não autoridade para commitar depois.

Essa ordem também fecha uma janela de classificação em três participantes:
uma aceitação que venceu o row lock e commitou antes da revogação continua
sendo reconhecida como `Duplicate` ou conflito factual; ela não pode ser
reclassificada como `Rejected/agent.not_active` por duas leituras
descoordenadas.

### Escopo por mensagem e preservação de R-SEQ

A garantia é atribuída à observação que disputa a revogação. Se a revogação
vence, essa mensagem recusada não cria amostra ou efeito próprio. O consumo do
seu slot pode, contudo, fechar um gap e tornar contígua uma amostra sucessora
que havia sido aceita e persistida antes da revogação. Nesse caso, estado,
evento e outbox derivados depois da revogação mantêm a proveniência exclusiva
da sucessora previamente aceita; não provêm da observação recusada e não
estendem a autoridade dela.

Proibir também essa reconciliação histórica quebraria o contrato vigente do
R-SEQ. A regressão focal distingue explicitamente as duas proveniências.

## Cenários provados localmente

As regressões concorrentes usam SQLite nomeado em memória, um keeper e
contextos/conexões independentes. Um interceptor pausa a transação dona depois
das leituras e antes da primeira escrita. O número de contextos criados prova
que o concorrente parou no gate antes de entrar na persistência, sem depender
de sleep. Regressões adicionais separam a proveniência R-SEQ e a classificação
de replay; o guard de fonte fixa a ordem transacional que não pode ser
exercida como contenção PostgreSQL neste lote.

| Ordem | Resultado observado |
|---|---|
| Ingestão primeiro | a revogação não criou contexto enquanto a ingestão estava pausada; depois da liberação, a observação commitou como `Accepted` e somente então a identidade terminou `Revoked` |
| Revogação primeiro | a ingestão não criou contexto enquanto a revogação estava pausada; depois do commit da revogação, a observação retornou `Rejected/agent.not_active` |
| Drift `Active + RevokedAt` | a ingestão recusou a identidade, consumiu somente a sequência e não criou efeito de saúde |
| Sucessora aceita atrás de gap | a sequência `2` foi aceita antes da revogação sem projeção; consumir a sequência `1` recusada depois liberou somente os efeitos com proveniência da sequência `2` |
| Replay aceito e revogação já duráveis após falha | uma sequência `2` aceita atrás de gap continuou `Duplicate`, não `Rejected`, quando a classificação nova encontrou a identidade revogada |

### Matriz de efeitos duráveis

| Efeito | Ingestão primeiro | Revogação primeiro |
|---|---:|---:|
| `agents.state` final | `Revoked` | `Revoked` |
| cursor de observação | `1` | `1`, somente consumo R-SEQ |
| `health_samples` | `1` | `0` |
| `instance_observation_states` | `1` | `0` |
| `events` | `1` | `0` |
| Server `outbox_messages` | `1` | `0` |
| `notification_deliveries` | `0` | `0` |
| audit de `agent.revoke` | `1` | `1` |

O cursor `1` no caminho revogação-primeiro não é evidência de saúde. Ele
prova somente que o slot rejeitado foi resolvido de forma contígua, conforme
R-SEQ.

## Regressões e validação observada

Ambiente: Windows, SDK local .NET `10.0.301`, configuração `Release`, sem
restore e sem acesso operacional externo.

| Verificação | Resultado |
|---|---|
| Duas ordens concorrentes e drift de revogação | `3/3` aprovadas |
| Proveniência da sucessora e classificação de replay | `2/2` aprovadas |
| Guard arquitetural de lock, fence e ordem de classificação | `1/1` aprovado |
| Sincronização, R-SEQ e revogação diretamente afetadas | `63/63` aprovadas |
| E2E autoritativo existente de replay/reorder/staleness/revogação | `1/1` aprovado |
| Build Release da solução | aprovado, `0` warnings e `0` erros |
| Suíte completa .NET | `413/413` unitários, `124/124` integrações, `93/93` arquitetura e `10/10` WPF |
| Cobertura .NET | `82,11%` linhas, `54,30%` branches e `10/10` componentes obrigatórios |
| `dotnet format --verify-no-changes --no-restore` | aprovado |
| Documentação de código | aprovada para `385` arquivos comment-capable |
| Links Markdown | `752` links locais em `198` arquivos aprovados |
| Secret scan | worktree não ignorado e histórico Git disponível aprovados |

Uma primeira tentativa das quatro suítes em paralelo produziu `123/124`
integrações: o diagnóstico não relacionado
`WaitHandleCandidateIsTimelyAndCancellationAware` excedeu sua asserção de
prazo sob contenção do host. A suíte de integração completa foi repetida
isoladamente e passou `124/124`; as outras três suítes também foram executadas
isoladamente para que os totais acima não dependessem da tentativa paralela.

Os pisos obrigatórios de `70%` de linhas e `45%` de branches foram
preservados. A meta orientativa baseada em risco de `80%` de linhas também foi
atingida, sem reduzir piso por componente.

## Observado, inferido e não testado

### Observado

- os dois interleavings locais descritos acima, com contextos independentes;
- nenhuma entrada do concorrente na persistência antes da liberação do gate;
- negação do drift `Active + RevokedAt`;
- proveniência exclusiva da sucessora aceita antes da revogação;
- classificação estática de replay aceito e identidade já revogada após uma
  falha sintética de persistência;
- efeitos duráveis exatos da matriz;
- regressão completa e cobertura quantificadas.

### Inferido por implementação e inspeção

- o `FOR NO KEY UPDATE` conflita com a atualização da mesma linha pela
  revogação principal no PostgreSQL;
- múltiplos processos Server dependem do row lock do PostgreSQL, não do gate
  local;
- a ordem `identity lock → replay classification → rejection consumption`
  elimina a falsa rejeição do interleaving dinâmico de três participantes;
- nenhum schema, pacote, contrato público ou migration mudou.

### Não testado

- PostgreSQL real, SQLSTATE `40001`, contenção multiprocesso de Server e
  latência do lock sob carga;
- o interleaving dinâmico exato `falha → aceitação concorrente → revogação`
  entre statements; o teste de replay parte dos dois fatos já duráveis, e o
  guard arquitetural protege a ordem que fecha essa janela;
- provider, database, certificado, identidade, rede ou runtime operacional;
- heartbeat, assignment, command e certificate-batch races, que possuem
  contratos e lotes próprios;
- SSRF, DNS rebinding e política de network egress;
- produção, deploy e advisories online.

O guard arquitetural prova a presença e a ordem lexical das operações no
fonte; não substitui uma execução PostgreSQL. Um laboratório PostgreSQL
descartável exigiria autorização separada porque runtime externo foi proibido
neste lote.

## Riscos, rollback e limites

- O lock da identidade permanece durante a reconciliação bounded da ingestão,
  limitada a dez lotes de mil amostras. Isso pode atrasar uma revogação
  concorrente; latência e carga PostgreSQL não foram medidas.
- O gate local não promete prioridade, fairness ou limite próprio de espera.
  A revogação o adquire antes da decisão RBAC definitiva, inclusive para um
  resultado `Denied`, e writers de ingestão já enfileirados podem antecedê-la.
  Disponibilidade e latência desse comportamento não foram ensaiadas sob
  carga.
- Uma falha de persistência cuja causa não possa ser reclassificada permanece
  `Retryable`; nunca é convertida em aceitação.
- O consumo R-SEQ não passa a guardar `MessageId` ou motivo central sem a
  migration que continua fora do escopo.
- Reverter a fence reabriria a corrida. O rollback recomendado é forward-fix;
  não existe schema ou dado novo a desfazer.
- Sincronização normal permanece desabilitada por padrão.

## Shutdown e limpeza pós-validação

Auditorias independentes durante os gates localizaram nove helpers de build
órfãos pertencentes ao workspace: sete nós MSBuild com `nodeReuse` e dois
`VBCSCompiler`, todos com caminho e parentage verificados e com o processo pai
já ausente. Somente esses nove helpers foram encerrados.

A auditoria pós-validação repetida observou:

- processos DB-Notifier ou helpers pertencentes ao projeto: `0`;
- listeners pertencentes ao projeto: `0`;
- janelas de produto DB-Notifier: `0`;
- uma janela comum do VS Code com o nome do repositório no título, preservada
  deliberadamente como IDE do usuário.

Nenhum navegador comum, database engine, serviço monitorado ou processo
alheio foi encerrado.

## Resultado factual

R-EGRESS encerra, no escopo local validado, a possibilidade conhecida de uma
ingestão começar sua decisão como `Active`, deixar a revogação principal
vencer a ordem e ainda commitar amostra ou efeitos atribuíveis à mesma
observação. Isso não bloqueia a projeção posterior de uma sucessora já aceita
antes da revogação e retida por gap. O fechamento não prova PostgreSQL
operacional, não amplia o significado para SSRF/network egress e não autoriza
lifecycle, ativação, runtime ou ação externa.
