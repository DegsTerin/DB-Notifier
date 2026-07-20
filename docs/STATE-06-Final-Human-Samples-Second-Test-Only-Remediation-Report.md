# Relatório STATE-06 — Segunda remediação test-only das amostras humanas finais

## Status e autoridade

- Data: 2026-07-20.
- Baseline inicial: commit `d3a61a9613192ad880d42d336313bd2baeb422f9`.
- Estado mantido: `STATE-06 INTEGRATION`.
- Classificação automática desta remediação: `APROVADO`, com as limitações registradas.
- `S06-HG-001` e `S06-HG-006`: continuam `BLOQUEADAS`; não foram repetidas humanamente.
- `S06-HG-002` a `S06-HG-005`: não foram repetidas e preservam as decisões anteriores.
- Human Gate final, runtime operacional, promoção e transição: não autorizados e não executados.

Bruno autorizou exclusivamente uma segunda remediação sob `tests/` e `scripts/`, com polling serial, orçamento de no máximo 30 leituras de evidência por minuto, estados factuais, recuperação positiva, gate de 180 segundos e runner Chrome visível versionado. A autorização permitiu somente Quality Gate automático local; não permitiu abrir amostra humana.

## Resultado em linguagem simples

O problema de excesso de consultas foi corrigido no laboratório test-only. A página não consulta mais quatro vezes por segundo. Agora espera a leitura anterior terminar e agenda a próxima depois de 2,1 segundos. No teste contínuo de três minutos, ela fez no máximo 29 leituras por minuto, nunca sobrepôs duas leituras e não recebeu nenhum `429` real.

A página também deixou de tratar todos os erros como “indisponível”. Ela diferencia:

- funcionamento normal;
- limitação temporária pelo rate limit;
- acesso de teste negado;
- indisponibilidade de transporte ou servidor.

Depois de respostas controladas `429`, `403` e `503`, a próxima leitura válida restaurou explicitamente `Browser → API disponível`. A última evidência válida permaneceu preservada durante as falhas.

Também foi criado um runner visível e versionado. Ele abre Chrome dedicado com perfil temporário, seleciona exatamente uma das duas amostras e mantém cada etapa parada até avanço explícito. O Quality Gate percorreu automaticamente os dois modos apenas para testar o runner e registrou `humanDecisionRecorded: false`. Portanto, a correção técnica passou, mas Bruno ainda não repetiu nem aprovou as duas amostras.

## Escopo implementado

### Polling e estados factuais

A página test-only agora:

- usa `setTimeout` serial de 2.100 ms em vez de `setInterval` de 250 ms;
- limita concorrência a uma leitura;
- aplica deadline, cancelamento e fence contra resposta tardia;
- respeita `Retry-After` quando presente e usa backoff bounded caso contrário;
- restaura o estado positivo após resposta `200` válida;
- mantém mensagens diferentes para `429`, `401/403` e falha de rede/`5xx`;
- preserva horário e conteúdo da última leitura válida;
- expõe somente contadores e estados sanitizados no DOM test-only.

O auditor automático passou a observar o DOM já atualizado pela página. Ele não mantém um segundo polling HTTP concorrente.

### Instrumentação e orçamento

O host test-only registra, de forma bounded e sem headers ou payloads:

- quantidade de leituras de evidência;
- respostas `429` do limiter real;
- concorrência ativa e máxima;
- maior quantidade de leituras em uma janela móvel de 60 segundos;
- contadores preexistentes de snapshots, replay e comandos.

O limitador real permaneceu em 100 permits por minuto. Ele não foi aumentado para mascarar o consumo anterior.

### Seleção de uma amostra por sessão

O host aceita três seletores fechados:

- `quality-gate`, usado apenas pelo auditor automático completo;
- `S06-HG-001`, que permite somente perda, recovery e encerramento dessa amostra;
- `S06-HG-006`, que permite somente verdade visual e encerramento dessa amostra.

Qualquer outro identificador é recusado antes de criar o laboratório. `S06-HG-006` não precisa mais executar perda/replay do Agent, e `S06-HG-001` não ativa a fixture `unknown`/`stale`.

### Runner visível versionado

Foram acrescentados:

- `scripts/run-state06-final-human-review.ps1`, runner PowerShell 7 que possui host, Chrome, perfil, temporários e cleanup;
- `scripts/present-state06-final-human-review.mjs`, presenter CDP que injeta somente headers de teste, observa origem de rede e nunca registra decisão humana.

O runner:

- exige `S06-HG-001` ou `S06-HG-006`;
- usa HTTPS loopback, run ID UUID v4 e identidade exclusivamente de teste;
- abre Chrome visível com perfil efêmero, sem reutilizar o navegador normal;
- mantém barriers no processo até controle explícito, timeout ou cancelamento;
- mostra controles claramente rotulados como laboratório test-only;
- encerra host, presenter, Chrome e temporários em `finally`;
- recusa Windows PowerShell 5.1 antes de criar recursos.

Os controles apenas avançam fixtures sintéticas existentes. Eles não são comandos administrativos, não criam `CommandAttempt`, não executam shell de ação e não afetam banco, serviço ou infraestrutura.

## Sequência E2E observada

### Gate de estabilidade e recuperação

No Chrome `150.0.7871.125`, o auditor automático:

1. abriu a página e o Dashboard TV no estágio `agent-transport-ready`;
2. introduziu exatamente uma resposta controlada `429`, observou o estado `limited` e depois `available`;
3. introduziu uma resposta controlada `403`, observou `denied` e depois `available`;
4. introduziu uma resposta controlada `503`, observou `unavailable` e depois `available`;
5. manteve o barrier inicial por 180 segundos sem autoavanço;
6. observou 96 leituras reais, zero `429` real, máximo 29 leituras/minuto e concorrência máxima 1;
7. percorreu perda do Agent com uma pendência, recovery com replay único e verdade visual `unknown`/`stale`;
8. comprovou zero `CommandAttempt`, concorrência de snapshot 1 e zero origem HTTP externa;
9. encerrou o host e removeu Chrome, perfil e roots temporários.

Resumo sanitizado final:

```json
{"result":"passed","browser":"Chrome/150.0.7871.125","controlled429Recovered":true,"controlled403Recovered":true,"controlled503Recovered":true,"durationGateSeconds":180,"evidenceRequests":96,"evidenceRateLimitedResponses":0,"maximumEvidenceConcurrency":1,"maximumEvidenceRequestsPerMinute":29,"agentLossObserved":true,"agentReplayAcceptedOnce":true,"unknownAndStaleVisible":true,"commandAttempts":0,"maximumSnapshotConcurrency":1,"observedExternalHttpRequests":0,"operationalData":false}
```

### Smoke automático do runner visível

O runner visível foi executado em duas sessões separadas e completamente limpas:

- `S06-HG-001`: readiness, perda, pendência, recovery, replay e encerramento concluídos;
- `S06-HG-006`: readiness, `unknown` corrente, `stale`, encerramento e não expiração concluídos.

Ambas registraram:

```json
{"automatedQualityGate":true,"humanDecisionRecorded":false,"observedExternalHttpRequests":0,"operationalData":false}
```

Essas execuções provam o runner e seus barriers. Não são amostras humanas e não reclassificam `S06-HG-001` ou `S06-HG-006`.

### Regressão consolidada

O modo consolidado anterior passou depois da mudança com:

- duas observações;
- uma entrega no sink de teste;
- dois journals de comando deliberadamente não executável;
- zero `CommandAttempt`;
- um certificado revogado;
- fences `3` e `4`;
- concorrência de snapshot 1;
- 72 requests HTTP, 18 WebSockets e zero HTTP externo;
- `operationalData=false`.

## Verificação

| Gate | Resultado observado |
|---|---|
| shutdown preflight e cleanup | zero processo, listener ou root STATE-06 pertencente às execuções |
| build Release da solução, `--no-restore` | 17 projetos; 0 erro; 0 warning |
| testes unitários | `332/332` |
| testes de arquitetura | `31/31` |
| testes de integração | `19/19` |
| cobertura .NET | linhas `78,9%`; branches `49,51%` |
| Dashboard | `60/60`, typecheck e build Vite aprovados |
| gate de 180 segundos | aprovado; 96 leituras, máximo 29/min, concorrência 1 e zero `429` real |
| recovery tipado | `429`, `403` e `503` controlados, cada um seguido por `available` |
| runner visível `S06-HG-001` | smoke automático aprovado; nenhuma decisão humana |
| runner visível `S06-HG-006` | smoke automático aprovado; nenhuma decisão humana |
| regressão E2E consolidada | aprovada; cadeia anterior preservada |
| formatação .NET | aprovada, somente verificação |
| documentação de código | `284` arquivos comment-capable aprovados |
| links Markdown | aprovado para 484 links locais em 110 arquivos |
| Pester legado no Windows PowerShell 5.1 | 23 aprovados, 1 skip condicional esperado, cobertura `32,08%` |
| PowerShell 7 para runners modernos | declarado e análise sintática aprovada |
| recusa Windows PowerShell 5.1 | exit `1` antes de criar root |
| seletor inválido `S06-HG-002` | exit `2` antes de criar runtime ou root |
| Node/npm | Node `24.18.0`, npm `11.16.0` aprovados |
| supply chain npm offline | zero vulnerabilidade no cache disponível |
| assets | marca, localização, tokens e 11 identidades/22 variantes aprovados |
| segredos | worktree não ignorada e histórico Git disponível sem achados |

### Limitação observada do Pester 3.4 no PowerShell 7

O runner legado foi tentado primeiro no PowerShell 7 e três testes `Should Throw` falharam dizendo que não houve exceção. A execução direta das duas fixtures NuGet confirmou exit `1` e exceções nos pontos esperados. A mesma suíte, executada no Windows PowerShell 5.1 compatível com o Pester 3.4 fixado, passou `23` testes com um skip esperado.

Nenhum arquivo legado ou verificador foi alterado. O passe reportado pertence ao host legado compatível; a incompatibilidade do matcher antigo com PowerShell 7 permanece registrada, sem ser apresentada como correção deste incremento.

## Escopo e isolamento confirmados

- Mudanças executáveis: somente `tests/` e `scripts/`.
- Inalterados: `src/`, `DBNotifier.sln`, projetos, packages, lockfiles e migrations.
- Novas dependências/downloads: nenhum.
- Acesso externo observado: nenhum.
- Recursos, dados, credenciais, providers, bancos ou Agents operacionais: nenhum.
- Composição normal e runtime operacional: inalterados.
- Notificação Windows, comando administrativo, executor e efeito em infraestrutura: nenhum.
- Repetição humana e Human Gate final: não executados.
- Promoção e transição: não executadas.

## Observado, inferido e não testado

### Observado

- polling serial a 2,1 segundos, máximo 29 leituras/minuto e concorrência 1;
- zero resposta `429` do limiter real durante o gate saudável;
- estados tipados e restauração positiva depois de `429`, `403` e `503` controlados;
- barrier inicial estável por 180 segundos sem autoavanço;
- execução automática separada dos dois modos do runner visível;
- perda/recovery, replay único e `unknown`/`stale` preservados;
- zero origem HTTP externa e zero `CommandAttempt`;
- todos os resultados da tabela de verificação;
- cleanup final sem processo ou root STATE-06 da tarefa.

### Inferido por inspeção direta e testes de arquitetura

- composição normal não descobre marker, host, presenter ou controles test-only;
- resultados tardios não podem ultrapassar o fence da página;
- selectors e transições fora de ordem falham fechados;
- o presenter não contém caminho para registrar uma decisão humana.

### Não testado

- decisão humana de `S06-HG-001` ou `S06-HG-006`;
- uso prolongado além de três minutos ou múltiplos browsers simultâneos;
- Agent, provider, banco, credencial, PKI, IdP ou infraestrutura operacional;
- PostgreSQL real, escala ou concorrência distribuída;
- notificação Windows, canal externo ou comando administrativo;
- Human Gate final, runtime operacional, promoção ou transição.

## Limitações e condições residuais

- Três minutos atravessam várias janelas do limiter e superam a falha original de cerca de 25 segundos; não provam longa duração operacional.
- `429`, `403` e `503` controlados provam a lógica visual. O fluxo saudável separado é que prova compatibilidade com o limiter real.
- O runner visível foi exercitado automaticamente para seu Quality Gate; Bruno ainda precisa observar e decidir as duas amostras.
- Barriers persistem somente durante a sessão bounded. Reinício invalida o run ID e exige nova sessão.
- `unknown` continua temporal. Se expirar durante observação humana, a tela o marca `EXPIRADA` e a amostra deve ser reiniciada.
- Chrome e Windows desta máquina não constituem homologação ampla.
- SQLite e identidades efêmeros não representam PostgreSQL, PKI ou escala operacional.
- A limitação Pester 3.4/PowerShell 7 é preexistente e não pertence à remediação test-only.
- A aprovação automática não substitui Human Gate e não abre nova repetição por conta própria.

## Classificação dos gates

| Gate | Classificação |
|---|---|
| Quality Gate próprio da segunda remediação | `APROVADO` com as limitações registradas |
| orçamento, serialização e recovery positivo | `APROVADO` no sandbox local autorizado |
| runner visível versionado e seleção por amostra | `APROVADO` automaticamente, sem decisão humana |
| isolamento `tests/scripts` | `APROVADO` |
| segurança, sanitização, fencing, cancelamento e cleanup | `APROVADO` no escopo test-only |
| regressão consolidada | `APROVADO` |
| `S06-HG-001` e `S06-HG-006` | continuam `BLOQUEADAS`; não repetidas humanamente |
| campanha humana | continua `BLOQUEADA` |
| Human Gate final | `PENDENTE` e não aberto |
| runtime operacional, promoção e transição | `NÃO AUTORIZADOS` |

## Próxima atividade

Nenhuma nova execução está autorizada por este relatório. Bruno deverá primeiro revisar e aceitar ou rejeitar esta segunda remediação. Uma eventual aceitação permitirá somente registrar a decisão.

Depois dessa aceitação, a repetição humana exclusiva de `S06-HG-001` e `S06-HG-006` ainda exigirá autorização separada. `S06-HG-002` a `S06-HG-005` não deverão ser repetidas, e o Human Gate final continuará posterior.
