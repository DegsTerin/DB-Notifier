# Proposta STATE-06 — Repetição da Campanha Consolidada de Quality Gate

## Status e autoridade

- Data: 2026-07-19.
- Estado mantido: `STATE-06 INTEGRATION`.
- Tipo: proposta exclusivamente documental para uma futura repetição da campanha automática.
- Baseline técnica incorporada: remediação do commit `ac12791`.
- Baseline documental aceita: registro da decisão humana no commit `dd58b26`.
- Campanha histórica: `BLOQUEADA`, preservada no [relatório original](STATE-06-Consolidated-Quality-Gate-Campaign-Report.md).
- Execução, build, testes de produto, runtime, navegador e geração de nova evidência: `NÃO AUTORIZADOS` por esta proposta.
- Amostra humana, Human Gate final, promoção e transição: `NÃO AUTORIZADOS` e não inferidos.

Este documento descreve como uma nova campanha poderá reavaliar o Quality Gate consolidado depois da remediação aceita. Ele não executa a campanha, não altera a classificação histórica, não declara o `STATE-06` concluído e não concede autoridade para qualquer ação descrita abaixo.

## Resultado em linguagem simples

A primeira campanha verificou os quatro incrementos separadamente, mas não conseguiu provar que todos usavam a mesma identidade, os mesmos dados e a mesma execução. Por isso, terminou bloqueada. A remediação do commit `ac12791` acrescentou um laboratório único, somente de teste, que ligou Agent, API, Dashboard, SignalR, notificação e transporte não executável de comando. Essa remediação passou nos seus próprios testes e foi aceita com limitações.

O próximo passo técnico possível é repetir a campanha completa. A diferença principal será que a nova campanha deverá executar o laboratório correlacionado como evidência central, além de repetir os gates gerais e os testes proprietários existentes. Ela deverá confirmar que o bloqueio original realmente foi removido no commit avaliado, que os dois warnings EF não reapareceram e que os runners modernos recusam PowerShell 5.1 antes de criar recursos.

A campanha continuará sendo somente uma auditoria automática local. Ela não poderá corrigir arquivos, instalar dependências, acessar a internet, mostrar notificação Windows, executar comando administrativo ou abrir o Human Gate final. Se algo falhar, o resultado será registrado como `REPROVADO` ou `BLOQUEADO`; não será consertado silenciosamente.

## Baseline factual

| Marco | Evidência factual | Efeito sobre a nova campanha |
|---|---|---|
| Primeira campanha | commit `5a47aae`; Quality Gate consolidado `BLOQUEADO` | relatório permanece histórico e não será reescrito |
| Relatório do bloqueio | ausência de composição única; dois warnings EF; contrato PowerShell implícito | três achados devem ser reavaliados diretamente |
| Remediação | commit `ac12791`; Quality Gate restrito `APROVADO` | fornece o harness único e as correções determinísticas |
| Aceitação da remediação | commit `dd58b26`; aceita com limitações | permite propor a repetição, mas não a executa |
| Estado corrente | `STATE-06 INTEGRATION` | nenhuma promoção, transição ou ativação operacional |

A remediação registrou como evidência histórica, que deverá ser medida novamente:

- solução .NET 10 com `17` projetos e build Release sem erro ou warning;
- `332/332` testes unitários, `29/29` de arquitetura, `14/14` de integração e `60/60` do Dashboard;
- execução correlacionada com `2` observações, `1` entrega ao sink de teste, `2` registros no journal e `0` `CommandAttempt`;
- concorrência máxima de snapshot `1` e zero origem HTTP externa observada;
- recusa do runner moderno em Windows PowerShell 5.1 antes da criação de recursos;
- cleanup final com zero processo, perfil ou root temporário pertencente ao harness.

Esses números não são resultado da campanha futura. A repetição deverá produzi-los novamente ou registrar a divergência.

## Baseline que deverá ser congelada na execução futura

No início da eventual campanha, o executor deverá registrar o commit exato, a branch e a worktree. A baseline técnica deverá conter `ac12791` e o registro humano `dd58b26`. O commit documental que vier a conter esta proposta poderá ficar acima deles, desde que a inspeção prove que nenhuma fonte, configuração executável, solução, projeto, package, lockfile ou migration mudou depois de `ac12791`.

Qualquer mudança técnica posterior deverá interromper a campanha antes do build e voltar para revisão de escopo. O executor não poderá escolher silenciosamente outro commit nem misturar alterações não relacionadas com a evidência.

## Objetivo

Produzir uma nova classificação automática, sanitizada e reproduzível do Quality Gate consolidado do `STATE-06`, reavaliando todos os critérios aplicáveis no commit congelado e comprovando que a cadeia única remediada funciona dentro do sandbox local, sem ativar composição normal, recurso operacional ou ação administrativa.

## Diferenças obrigatórias em relação à primeira campanha

1. O `DBNotifier.State06.ConsolidatedSandboxHost` será a evidência principal da cadeia completa.
2. O mesmo `runId`, Agent, Agent SQLite, Server SQLite e projeção deverão atravessar as fronteiras correlacionáveis.
3. Dashboard, SignalR e notificação deverão consumir a projeção criada pela observação do mesmo Agent.
4. O transporte de comando deverá usar a mesma identidade Agent e terminar com zero `CommandAttempt`.
5. A revogação deverá negar heartbeat, assignments, observação e comando na mesma execução.
6. As consultas EF deverão terminar sem `Microsoft.EntityFrameworkCore.Query[10102]`.
7. Os runners modernos deverão ser chamados por `pwsh` 7 ou superior, com uma verificação negativa pré-recurso em Windows PowerShell 5.1.
8. O relatório novo terá nome próprio e não substituirá o relatório bloqueado da primeira campanha.

## Princípios obrigatórios

- somente artefatos já existentes no commit congelado podem ser executados;
- nenhum comando de verificação poderá formatar, regenerar, restaurar externamente ou corrigir arquivos;
- toda conexão de runtime deverá permanecer em HTTPS loopback ou no IPC local já pertencente aos harnesses;
- apenas dados, identidades, certificados, bancos e sinks sintéticos serão permitidos;
- API e polling de 30 segundos continuarão autoritativos; SignalR continuará apenas um hint;
- concorrência máxima de leitura continuará `1`, com budgets, cancelamento e fencing existentes;
- acknowledgement continuará significando recepção/persistência, nunca execução;
- o Human Gate final e a transição permanecerão fora da classificação automática;
- qualquer resíduo, acesso externo ou ampliação necessária encerrará a execução com resultado não aprovado.

## Sequência futura proposta

### Fase 0 — Shutdown preflight e congelamento

- identificar e encerrar somente componentes e runtimes comprovadamente pertencentes ao DB-Notifier;
- provar zero processo, listener, janela, ícone de notificação e navegador dedicado remanescente;
- registrar commit, branch, status Git, ferramentas, espaço temporário e hashes dos manifests/lockfiles;
- exigir worktree limpa e dependências já disponíveis localmente;
- confirmar PowerShell 7, Windows PowerShell 5.1 para o teste negativo e Chrome ou Edge já instalado.

Falha de ownership, worktree suja ou necessidade de download bloqueará a campanha antes do primeiro build.

### Fase 1 — Inspeção read-only da remediação

- comparar a baseline anterior à remediação com o commit congelado;
- confirmar que o projeto consolidado existe somente sob `tests/`;
- confirmar ausência de referência do host em projetos `src/` e nas composições normais;
- confirmar inclusão mínima `Any CPU` na solução e presença exata na fixture NuGet positiva;
- confirmar ausência de package, lockfile, migration ou dependência acrescentada pela remediação;
- revisar diretamente as duas ordenações EF e os testes de isolamento/PowerShell;
- mapear cada achado histórico para uma evidência executável atual.

### Fase 2 — Gates gerais offline

- integridade Git e `git diff --check`;
- documentação de código em inglês britânico, links Markdown e headings;
- secret scan do worktree e histórico disponível, sem expor valores;
- verificação NuGet pela fixture offline e npm audit somente com cache local disponível;
- toolchain, assets de marca, tokens, localização e ícones de provider;
- confirmação de que manifests, packages, lockfiles, migrations e fontes de dependência não mudaram durante a campanha.

Ausência de cache será registrada como limitação ou bloqueio. Não autorizará rede.

### Fase 3 — Build, suítes e cobertura

- build Release completo da solução .NET 10, sempre sem restore externo;
- testes unitários, arquitetura e integração de toda a solução;
- cobertura .NET comparada aos pisos vigentes de `70%` de linhas e `45%` de branches;
- `dotnet format --verify-no-changes`, sem aplicar alterações;
- Dashboard: toolchain, typecheck, `60` testes ou contagem corrente explicada, build e verificadores de assets;
- Pester legado pelo runner compatível e bundle `ValidateOnly`;
- migrations/model drift apenas local e offline;
- smoke fail-closed das composições normais;
- captura dos logs EF aplicáveis para comprovar ausência do warning `10102`.

Mudança de contagem não será falha por si só, mas deverá ser explicada pelo diff. Falha, warning novo ou regressão de cobertura não poderá ser ocultado por exclusão.

### Fase 4 — E2E correlacionado obrigatório

Executar serialmente o runner consolidado existente, com um navegador dedicado e perfil efêmero. A evidência deverá confirmar, no mesmo run:

1. criação de material P-256, Agent e identidade humana read-only separados e exclusivamente de teste;
2. enrollment, assignment read-only e heartbeat;
3. observação `Degraded` preservada offline e ingerida uma única vez após replay entre processos;
4. Dashboard normal ainda demonstrativo antes do modo TV;
5. leitura imediata do snapshot autoritativo ao entrar no modo TV;
6. SignalR autenticado apenas como hint de releitura HTTPS;
7. segunda observação `Unavailable` com perda de resposta e replay idempotente;
8. uma única entrega da transição posterior à baseline ao sink em memória;
9. reinício do consumidor sem entrega duplicada;
10. reconciliação independente depois de 30 segundos, ETag/`304` e concorrência máxima `1`;
11. preservação factual do último snapshot durante offline, recovery e fencing de sessão antiga;
12. incompatibilidade, gap, perda de resposta e replay do transporte não executável de comando;
13. revogação seguida de recusa de heartbeat, assignments, observação e comando;
14. `2` observações, `1` entrega, `2` registros de journal e `0` `CommandAttempt` no fechamento;
15. zero origem HTTP externa e cleanup integral.

Se a implementação corrente emitir contagem diferente, a campanha deverá investigar somente por inspeção e classificar o fato; não poderá editar o harness.

### Fase 5 — Regressões proprietárias e matriz de falhas

Depois do E2E correlacionado, repetir serialmente os harnesses proprietários aceitos para garantir que a composição nova não mascarou regressão:

- Agent Fleet e identidade/revogação;
- pipeline autoritativo Agent → API;
- Dashboard TV/SignalR/browser;
- notificação reconciliada com baseline silenciosa;
- transporte não executável de comando.

A matriz deverá cobrir offline/reconexão, resposta perdida, replay, duplicidade, reorder, incompatibilidade, expiração, stale/unknown, cancelamento, timeout, fencing, tempestade/coalescência e falha de cleanup.

### Fase 6 — Contrato PowerShell

- executar os runners modernos somente com `pwsh` 7 ou superior;
- invocar o runner consolidado pelo Windows PowerShell 5.1 com política local controlada apenas para alcançar `#Requires`;
- exigir `ScriptRequiresUnmatchedPSVersion`, exit code não zero e nenhuma criação de diretório, processo, listener ou perfil;
- manter o Pester legado sob seu runner Windows PowerShell 5.1;
- não adaptar scripts durante a campanha.

### Fase 7 — Cleanup e auditoria final

- encerrar host, processos filhos e navegador dedicado;
- remover somente perfis, stores e diretórios temporários comprovadamente pertencentes à campanha;
- provar zero processo/listener/profile/root novo e preservar recursos anteriores ou alheios;
- confirmar worktree limpa e hashes rastreados inalterados;
- separar resultados observados, inferidos, não testados e bloqueados.

### Fase 8 — Relatório factual novo

Criar `STATE-06-Consolidated-Quality-Gate-Rerun-Report.md`, sem alterar o relatório histórico bloqueado. O relatório deverá conter:

- baseline, ambiente, ferramentas, comandos, durações e exit codes;
- sequência correlacionada sanitizada e contagens observadas;
- resultados dos gates gerais, cobertura, EF e PowerShell;
- matriz `Lifecycle → requisito → evidência → classificação`;
- achados por gravidade, limitações e condições residuais;
- cleanup e confirmação de ausência de acesso externo;
- classificação automática final `APROVADO`, `REPROVADO` ou `BLOQUEADO`;
- indicação explícita de que Human Gate, amostra humana, promoção e transição permanecem separados.

## Envelope de recursos

Os limites propostos são de segurança do laboratório, não sizing operacional:

- duração total máxima: `120 minutos`;
- timeout máximo por comando: `30 minutos`, salvo limite menor do próprio teste;
- no máximo um harness E2E e um navegador dedicado ativos por vez;
- no máximo `2 GiB` de temporários pertencentes à campanha;
- exigir pelo menos `10 GiB` livres no volume temporário antes do início;
- nenhuma paralelização de harnesses que compartilhem store, porta, identidade ou perfil;
- cancelamento no primeiro acesso externo, processo não atribuível ou cleanup inseguro.

O executor não poderá ampliar limites durante a campanha sem nova autorização.

## Critérios de aceite do Quality Gate repetido

O resultado global somente poderá ser `APROVADO` se:

1. o commit e a worktree forem congelados e permanecerem íntegros;
2. todos os gates gerais aplicáveis passarem sem correção durante a campanha;
3. a cadeia correlacionada completa for observada no harness único;
4. snapshot, SignalR e notificação derivarem da mesma projeção alimentada pelo Agent;
5. API/polling permanecerem autoritativos, com cadência real de 30 segundos e concorrência `1`;
6. replay, duplicidade, reorder, incompatibilidade, expiração, cancelamento e fencing preservarem estado factual;
7. a notificação ocorrer uma vez depois da baseline e não se repetir;
8. o transporte de comando terminar com zero attempt, executor ou efeito;
9. a revogação negar todas as operações Agent exigidas;
10. os warnings EF históricos não reaparecerem;
11. o contrato PowerShell e a recusa pré-recurso em 5.1 forem comprovados;
12. não houver achado crítico ou alto aberto;
13. não houver acesso externo, dependência nova, artefato rastreado alterado ou violação de autoridade;
14. cleanup e worktree limpa forem comprovados;
15. o relatório não transformar evidência de sandbox em alegação operacional.

Um resultado parcial não será arredondado para aprovação.

## Classificação do gate

- `APROVADO`: todos os critérios obrigatórios possuem evidência e não há achado crítico/alto aberto;
- `REPROVADO`: uma falha técnica reproduzida viola requisito, segurança ou integridade da evidência;
- `BLOQUEADO`: a evidência obrigatória exige mudança, acesso, dependência, ambiente ou autoridade indisponível;
- `NÃO APLICÁVEL`: permitido somente para uma verificação realmente externa ao `STATE-06`, com justificativa.

A nova classificação será corrente somente para a baseline executada. O relatório histórico da primeira campanha continuará registrando corretamente o bloqueio observado no commit `5a47aae`.

## Gravidade dos achados

- `CRÍTICA`: ação real, segredo exposto, bypass de identidade/autorização ou recurso externo afetado;
- `ALTA`: perda/duplicidade autoritativa, execução implícita, regressão fail-closed ou ausência da cadeia obrigatória;
- `MÉDIA`: falha de budget, cancelamento, fencing, compatibilidade, cleanup recuperável ou cobertura relevante;
- `BAIXA`: precisão documental, diagnóstico ou manutenção sem impacto no critério obrigatório;
- `FERRAMENTA/AMBIENTE`: limitação reproduzível do host, sem ser convertida em passe.

A campanha apenas registra achados e recomenda remediação. Nenhuma gravidade concede autoridade para corrigir.

## Fora de escopo absoluto

- implementação, remediação, refactor ou criação de novo harness;
- alteração de código, configuração executável, solução, projeto, package, lockfile ou migration;
- restore externo, download, registry, CDN ou qualquer acesso à internet;
- recurso, credencial, certificado, Agent, provider, banco, IdP, PKI, vault ou canal operacional;
- PostgreSQL real, banco monitorado, serviço permanente ou infraestrutura externa;
- comando administrativo, `CommandAttempt`, executor, shell, post-probe, `Start`, `Stop` ou `Restart`;
- notificação Windows visível, e-mail, SMS, webhook, Teams, Slack ou canal externo;
- UI administrativa, monitoramento real, carga representativa, endurance, HA ou disaster recovery;
- LLM, recomendação, planejamento, automação ou promoção `none → OBSERVER`;
- deploy, publicação, instalação, amostra humana, Human Gate final, `STATE-07` ou transição.

## Riscos e limitações residuais

- O Server SQLite em memória não prova PostgreSQL, restart distribuído ou power-loss operacional.
- Agent SQLite efêmero e certificados de teste não provam key store, PKI, rotação ou recuperação reais.
- Um único host e uma única máquina não provam isolamento de rede, escala ou fairness de frota.
- Chrome ou Edge local não constituem homologação ampla de navegador ou plataforma.
- SignalR permanece best-effort e nunca substitui a reconciliação periódica.
- O sink em memória prova solicitação de entrega, não apresentação de notificação pelo Windows.
- O transporte prova persistência/acknowledgement e recusa segura, não execução de comando.
- Auditoria offline não comprova advisories atuais de registries externos.
- A cadência real de 30 segundos pode sofrer ruído do host; tolerância não poderá reduzir a regra nem permitir sobreposição.
- A aprovação automática, se obtida, ainda não inclui experiência humana nem autoriza `STATE-07`.

## Condições de parada

A futura campanha deverá parar antes de prosseguir se:

- shutdown, ownership ou worktree limpa não puderem ser provados;
- o commit executado contiver mudança técnica posterior à baseline sem revisão;
- surgir necessidade de editar, regenerar, restaurar, instalar ou baixar algo;
- faltar cache/dependência e a continuação exigir rede;
- aparecer dado, identidade, certificado ou recurso não exclusivamente de teste;
- qualquer fluxo alcançar command attempt, executor, shell, serviço, banco ou infraestrutura;
- um processo, listener, perfil ou store não puder ser atribuído e removido com segurança;
- um budget for excedido;
- a evidência obrigatória não puder ser sanitizada ou correlacionada;
- houver alteração rastreada não documental produzida durante a campanha.

O resultado deverá ser `REPROVADO` para violação técnica comprovada ou `BLOQUEADO` quando faltar autoridade/evidência. A campanha não poderá continuar por conveniência.

## Entregáveis futuros

- relatório novo e separado da repetição;
- matriz de rastreabilidade dos critérios de saída do `STATE-06`;
- resumo sanitizado da cadeia correlacionada e da matriz de falhas;
- inventário de comandos, versões, resultados e cleanup;
- classificação automática consolidada;
- atualização factual do estado, plano e histórico;
- commit local exclusivamente documental do relatório.

Nenhum log bruto, banco, certificado, chave, token, perfil, binário ou temporário integrará o commit.

## Decisão futura de Bruno

Se Bruno concordar e desejar autorizar somente a repetição automática, o texto sugerido é:

> AUTORIZO exclusivamente a repetição da Campanha Consolidada de Quality Gate do STATE-06 no commit corrente informado no hand-off, que deverá conter a remediação aceita `ac12791`, limitada a shutdown preflight, congelamento e inspeção read-only da baseline, verificações integralmente offline, build/testes/cobertura dos artefatos existentes, execução serial dos harnesses sandbox existentes e do harness correlacionado Agent → API → Dashboard/SignalR → notificação em sink de teste → transporte de comando deliberadamente não executável, revogação, replay, falhas, verificação dos warnings EF, contrato PowerShell, cleanup e relatório factual novo. Autorizo runtimes temporários exclusivamente locais e um navegador dedicado com perfil efêmero, que deverão ser encerrados e removidos ao final. A campanha não poderá alterar ou corrigir código, configuração executável, solução, projetos, packages, lockfiles ou migrations; qualquer falha ou lacuna deverá ser registrada como REPROVADA ou BLOQUEADA e voltar para autorização separada. Permanecem proibidos acesso externo, downloads, recursos ou credenciais operacionais, provider/banco real, comando administrativo, CommandAttempt, executor, shell, serviço/infraestrutura afetados, canais externos, notificação Windows visível, LLM, deploy, amostra humana, Human Gate final, promoção e transição de estado.

Esse texto ainda não foi emitido como autorização. Revisar, aceitar ou citar esta proposta não inicia a campanha.

## Próximo passo para Bruno

1. Leia principalmente `Resultado em linguagem simples`, `Baseline factual`, `Sequência futura proposta`, `Critérios de aceite`, `Fora de escopo absoluto` e `Riscos e limitações residuais`.
2. Se algum limite estiver incorreto, responda somente com os pontos que deseja alterar.
3. Se concordar e quiser executar a repetição automática, envie exatamente o texto da seção `Decisão futura de Bruno`.
4. Depois da eventual execução, revise o relatório novo antes de autorizar qualquer amostra humana.
5. Não autorize junto a essa decisão o Human Gate final, promoção ou transição.

Nenhuma ação técnica é necessária enquanto esta proposta estiver apenas em revisão.
