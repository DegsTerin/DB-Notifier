# Relatório STATE-06 — Remediação test-only das amostras humanas finais

## Status e autoridade

- Data: 2026-07-20.
- Estado mantido: `STATE-06 INTEGRATION`.
- Baseline técnica inicial: commit `e6b604bed7ba1e532d99b82f1cf944dff6ee8ed3`.
- Baseline automática incorporada: commit `84217c64312a024ec4f286adfe4872184a21849c`.
- Relatório automático aceito incorporado: commit `2c1e05fd8ad4dec2174682fee66aafbd92efc6ee`.
- Classificação automática desta remediação: `APROVADO`, com as limitações registradas.
- Estado das amostras: `S06-HG-001` e `S06-HG-006` continuam `BLOQUEADAS` até repetição humana separadamente autorizada; `S06-HG-002` a `S06-HG-005` permanecem aprovadas.
- Human Gate final, runtime operacional, promoção e transição: não autorizados e não executados.

Bruno autorizou somente alterações test-only sob `tests/` e `scripts/`, testes automáticos, runtimes locais temporários e documentação factual. A autorização exigia parada `BLOQUEADA` se qualquer critério dependesse de `src/`, solução, projetos, packages, lockfiles ou migrations. Nenhuma dessas fronteiras proibidas precisou ser alterada.

## Resultado em linguagem simples

O laboratório agora consegue mostrar, de maneira distinta e factual, as duas situações que antes não eram apresentáveis:

1. a ligação interna do Agent começa disponível, fica indisponível enquanto exatamente uma observação permanece guardada e depois recupera, enviando essa observação uma única vez;
2. o Dashboard TV recebe pela API sandbox dois exemplos simultâneos: um estado `Desconhecido` ainda atual e outro estado `Desatualizado`, com origem sintética local e suporte apenas planejado.

Uma página auxiliar identifica expressamente que é evidência do harness e não estado operacional. Durante a perda do Agent, o navegador continua ligado à API e o último snapshot válido permanece visível. Assim, offline do navegador não é usado como substituto de offline do Agent.

O Quality Gate automático percorreu essa sequência num Chrome dedicado com perfil efêmero, sem abrir uma amostra para decisão humana. A regressão do harness consolidado anterior também passou. Nenhum código de produto foi alterado, nenhum dado externo foi usado e nenhum comando foi executado.

## Escopo implementado

### Host e ativação test-only

O host consolidado sob `tests/` recebeu o marker exato `state06-final-human-samples-remediation`. Esse modo:

- permanece no executável de testes já existente;
- exige HTTPS loopback, identidade humana exclusivamente de teste e run ID exato;
- expõe somente uma página de evidência, quatro transições test-only e o endpoint sanitizado já delimitado;
- mantém o modo consolidado anterior inalterado como modo padrão do runner;
- não é referenciado nem registado pelos `Program` normais, WPF normal ou Dashboard normal.

### Sequência do Agent

O estado correlacionado foi dividido em barriers determinísticos:

1. `agent-transport-ready`: enrollment, assignment, heartbeat e uma observação sintética inicial já aceites;
2. `agent-observation-pending`: transporte Agent → API deliberadamente indisponível, uma segunda observação na outbox Agent, uma observação ainda no Server e browser → API disponível;
3. `agent-replay-accepted-once`: transporte recuperado, outbox vazia, duas observações no Server e snapshot autoritativo `Indisponível`;
4. `visual-truth-ready`: fonte test-only de verdade visual ativada;
5. `completed`: evidência terminal, cleanup autorizado e zero `CommandAttempt`.

As transições usam a serialização, o relógio controlado, os budgets, o cancelamento e o fencing do harness. A evidência publicada contém somente estados e contagens; não contém token, chave, certificado, header, path, payload, corpo de observação ou texto de exceção.

### Verdade visual

A composição de integração substitui `IDashboardTvSnapshotSource` somente no novo modo test-only. A fonte produz dois itens UUID v4 sintéticos e versionados:

- `Unknown atual - fixture sintética`, com status `unknown` e timestamps correntes;
- `Evidência desatualizada - fixture sintética`, com status interno `degraded` e timestamps seis minutos anteriores ao relógio controlado.

Os dois itens atravessam o endpoint API e o adapter Dashboard TV existentes. A regra de produto `stale > status`, o limiar de cinco minutos e a fixture normal `demo-004` não foram alterados. A tela apresenta texto, não apenas cor, e mantém `Planejado — não implementado` e `Sandbox local sintético` na superfície auxiliar.

### Runner e auditor

O runner PowerShell existente recebeu `-EvidenceMode`, cujo valor padrão continua `Consolidated`. O valor exato `FinalHumanSamplesRemediation` seleciona o novo marker e um auditor browser próprio. O auditor:

- aceita somente endpoints loopback e um run ID UUID v4;
- não usa `Network.emulateNetworkConditions`;
- avança a perda, recuperação e verdade visual pela mesma origem autenticada de teste;
- comprova contagens duráveis, preservação, replay único, textos visíveis, concorrência máxima `1`, zero `CommandAttempt` e zero origem HTTP externa;
- encerra o host e fecha a ligação CDP no final.

O runner preserva o requisito PowerShell 7, budget total, browser dedicado, perfil efêmero e cleanup em `finally`. Windows PowerShell 5.1 continua a recusá-lo antes da criação de recursos.

## Sequência E2E observada

O Quality Gate próprio passou no Chrome `150.0.7871.125`:

1. abriu a superfície auxiliar test-only e o Dashboard same-origin;
2. leu imediatamente o snapshot inicial `Degradado`;
3. tornou somente Agent → API indisponível;
4. comprovou browser → API ainda disponível, uma observação pendente, uma amostra no Server e último snapshot preservado;
5. recuperou Agent → API e comprovou outbox vazia, duas amostras no Server e replay aceite uma única vez;
6. releu o Dashboard e observou o snapshot autoritativo `Indisponível`;
7. ativou a fonte visual e observou exatamente dois itens, com `Desconhecido` e `Desatualizado` distintos;
8. confirmou suporte planejado, origem sintética, zero tentativa de comando e concorrência serial;
9. encerrou host, Chrome, perfil e roots temporários.

Resumo sanitizado observado:

```json
{"result":"passed","browser":"Chrome/150.0.7871.125","humanRemediationMode":true,"agentLossObserved":true,"agentReplayAcceptedOnce":true,"pendingObservations":0,"serverObservationSamples":2,"unknownAndStaleVisible":true,"commandAttempts":0,"maximumSnapshotConcurrency":1,"observedHttpRequests":72,"observedWebSockets":3,"observedExternalHttpRequests":0,"operationalData":false}
```

A regressão separada do modo consolidado anterior também passou com duas observações, uma entrega no sink de teste, dois journals, zero `CommandAttempt`, revogação, fences `3` e `4`, concorrência de snapshot `1`, zero origem HTTP externa e `operationalData=false`. Ela não repetiu nenhuma amostra humana nem publicou notificação Windows.

## Achados durante a implementação

Todos os achados ocorreram exclusivamente nos artefactos test-only e foram corrigidos dentro da autoridade:

- analisadores inicialmente exigiram cultura explícita para um parse de teste e um tipo concreto para a fonte fallback;
- a primeira asserção de dispatch interpretou incorretamente uma observação aceite como não reconhecida; a expectativa foi alinhada à semântica real observada;
- o primeiro snapshot visual usou GUIDs parseáveis pelo .NET, mas sem bits de versão/variante UUID aceites pelo validador browser; os dois identificadores sintéticos foram corrigidos para UUID v4;
- depois de cada falha foram comprovados zero processo e zero listener pertencentes à sessão antes da ação seguinte; três roots temporários órfãos de tentativas anteriores foram atribuídos ao harness e removidos de `%TEMP%` antes da repetição.

Nenhum achado exigiu relaxar uma asserção, usar retry até passar, alterar `src/` ou expandir a autorização.

## Verificação

| Gate | Resultado observado |
|---|---|
| shutdown/cleanup | zero processo, listener ou root temporário do harness depois de cada execução final |
| build Release da solução, `--no-restore` | `17` projetos; `0` erro; `0` warning |
| testes unitários | `332/332` |
| testes de arquitetura | `31/31` |
| testes de integração | `18/18` |
| cobertura .NET | linhas `78,9%`; branches `49,51%` |
| Dashboard | `60/60`, typecheck e build Vite aprovados |
| E2E da remediação | aprovado; sequência Agent e verdade visual completas |
| regressão E2E consolidada | aprovada; cadeia anterior preservada |
| formatação .NET | aprovada, somente verificação |
| documentação de código | `282` arquivos comment-capable aprovados |
| links Markdown | `472` links locais em `107` arquivos aprovados |
| Pester legado | `23` testes aprovados, `1` skip condicional esperado, cobertura `32,08%` |
| PowerShell 5.1 | recusa antecipada aprovada, exit `1`, zero root criado |
| Node/npm | Node `24.18.0`, npm `11.16.0`; versões exatas aprovadas |
| supply chain offline | npm audit no cache: zero vulnerabilidade; fixture NuGet exercitada pela suíte legada |
| assets gerados | marca, tokens, localização e 11 identidades/22 variantes de provider aprovados |
| segredos | worktree não ignorada e histórico Git disponível sem achados |
| integridade Git | `git fsck --full` exit `0`; somente objetos dangling históricos |

A auditoria npm offline não prova advisories publicados depois do conteúdo existente no cache. A fixture NuGet prova estrutura sintética do gate, não consulta atual a registry.

## Escopo e isolamento confirmados

- Alterações executáveis: somente `tests/` e `scripts/`.
- Documentação factual: este relatório e os registros proprietários.
- Inalterados: `src/`, `DBNotifier.sln`, projetos, packages, lockfiles e migrations.
- Acesso externo/download: nenhum.
- Recursos operacionais: nenhum.
- Provider, banco, Agent, identidade, PKI, IdP, vault ou credencial operacional: nenhum.
- Notificação Windows: não executada.
- Comandos administrativos, `CommandAttempt`, executor, shell de ação e infraestrutura afetada: nenhum.
- Composição normal, deploy, LLM, promoção e transição: inalterados e não autorizados.

## Observado, inferido e não testado

### Observado

- sequência Agent disponível → indisponível com uma pendência → recuperado com replay único;
- browser → API disponível durante a perda Agent → API;
- último snapshot válido preservado e snapshot recuperado apresentado;
- `Desconhecido` corrente e `Desatualizado` apresentados como textos distintos;
- suporte planejado e origem sintética presentes;
- todos os gates e contagens da tabela de verificação;
- cleanup final sem processo, listener ou root da sessão.

### Inferido por inspeção direta e testes de arquitetura

- o marker e os controles novos não são descobertos pela composição normal;
- a página auxiliar não oferece comando administrativo e publica somente campos sanitizados;
- os guards de identidade, run ID, loopback, rate limit e serialização são herdados do host de integração já testado.

### Não testado

- qualquer Agent, provider, banco, identidade, credencial, PKI, IdP, vault, rede ou infraestrutura operacional;
- PostgreSQL real, escala, concorrência distribuída ou outro browser/sistema operativo;
- notificação Windows visível, canal externo ou comando administrativo;
- repetição humana de `S06-HG-001` ou `S06-HG-006`;
- Human Gate final, runtime operacional, promoção ou transição.

## Limitações e condições residuais

- A superfície auxiliar prova a sequência do laboratório; ela não implementa uma tela operacional de conectividade do Agent.
- A fonte visual prova semântica da API/Dashboard TV sandbox; ela não prova monitoramento, suporte ou homologação de provider.
- Agent e Server usam SQLite efêmero e identidades sintéticas; isso não representa PostgreSQL, PKI ou escala operacional.
- Um único Chrome e uma máquina Windows não constituem homologação ampla.
- A amostra humana futura deve usar os barriers e decidir cada momento; o passe automático não substitui essa observação.
- O item `unknown` corrente pode envelhecer; uma sessão humana que ultrapasse o limite deve ser reiniciada, não reinterpretada.
- A regressão consolidada usa sink de notificação e transporte deliberadamente não executável; ela não publica notificação Windows nem prova comandos reais.
- `S06-HG-001` e `S06-HG-006` continuam historicamente `BLOQUEADAS` até uma repetição humana autorizada e aprovada.

## Classificação dos gates

| Gate | Classificação |
|---|---|
| Quality Gate próprio da remediação | `APROVADO` com as limitações registradas |
| isolamento `tests/scripts` | `APROVADO` |
| segurança, sanitização, fencing, budget e cancelamento | `APROVADO` no sandbox local autorizado |
| regressão consolidada | `APROVADO` |
| cleanup | `APROVADO` |
| `S06-HG-001` e `S06-HG-006` | continuam `BLOQUEADAS`; não repetidas |
| campanha humana completa | continua `BLOQUEADA` |
| Human Gate final | `PENDENTE` e não aberto |
| runtime operacional, promoção e transição | `NÃO AUTORIZADOS` |

## Decisão humana da remediação

Depois de solicitar e receber a leitura direta deste relatório, principalmente de `Resultado em linguagem simples`, `Sequência E2E observada`, `Limitações e condições residuais` e `Classificação dos gates`, Bruno declarou em 2026-07-20:

> Remediação STATE-06 Final Human Samples Test-only Evidence, commit dd420d1: ACEITA COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo repetição das amostras humanas, Human Gate final, runtime operacional, promoção nem transição de estado.

Essa decisão aceita somente o resultado e as limitações desta remediação. Ela não altera retroativamente a campanha humana, não aprova `S06-HG-001` ou `S06-HG-006` e não concede autoridade para abrir sua repetição.

## Próxima atividade

A remediação já foi aceita e nenhuma ação técnica está autorizada por essa aceitação. Se Bruno desejar continuar, a próxima atividade será solicitar ou conceder separadamente autorização para repetir exclusivamente `S06-HG-001` e `S06-HG-006`. As quatro amostras já aprovadas não devem ser repetidas. Mesmo que as duas repetições sejam aprovadas, o Human Gate final continuará sendo uma decisão posterior e independente.
