# Proposta STATE-06 — Nova repetição da Campanha Consolidada após remediação de revogação

> Execução posterior: Bruno autorizou separadamente a campanha no commit `84217c6`. A execução terminou `APROVADA` em 2026-07-20 e está preservada no [relatório factual pós-remediação](STATE-06-Consolidated-Quality-Gate-Post-Revocation-Remediation-Rerun-Report.md). O texto abaixo permanece como delimitação histórica da proposta e não autoriza nova execução, amostra humana, Human Gate, promoção ou transição.

## Status e autoridade

- Data: 2026-07-20.
- Estado mantido: `STATE-06 INTEGRATION`.
- Tipo: proposta exclusivamente documental para uma futura repetição automática do Quality Gate consolidado.
- Baseline solicitada na abertura desta proposta: commit `67e0187`.
- Remediação técnica incorporada: commit `f9bb567`.
- Quality Gate próprio da remediação: `APROVADO`.
- Human Gate próprio da remediação: `ACEITO COM AS LIMITAÇÕES REGISTRADAS`.
- Repetição anterior no commit `66d0a9f`: `REPROVADA`, preservada no [relatório histórico](STATE-06-Consolidated-Quality-Gate-Rerun-Report.md).
- Execução, build, testes de produto, runtime, browser e geração de evidência operacional: `NÃO AUTORIZADOS` por esta proposta.
- Amostra humana, Human Gate final, promoção e transição: `NÃO AUTORIZADOS` e não inferidos.

Esta proposta descreve uma campanha futura. Ela não executa nenhum verificador, não reclassifica a campanha anterior, não aprova o Quality Gate consolidado e não declara o `STATE-06` concluído.

## Resultado em linguagem simples

A repetição anterior passou nos testes gerais, mas falhou quando o laboratório único tentou finalizar a revogação do Agent. A remediação aceita tornou essa parte observável e separou seis fatos que antes apareciam sob um único nome: pedido de revogação, gravação central, negação direta pelo Server, quarentena local, preservação do último assignment válido e recusa dos demais fluxos.

O próximo passo técnico possível é executar novamente toda a campanha, do início ao fim, usando os artefatos já existentes. Essa nova execução não poderá aproveitar apenas o resultado isolado da remediação. Ela deverá reconstruir a cadeia completa — Agent, API, Dashboard, SignalR, notificação de teste, transporte não executável de comando e revogação — e terminar com todos os gates obrigatórios aprovados na mesma baseline congelada.

Se qualquer etapa falhar, a campanha deverá registrar `REPROVADO` ou `BLOQUEADO`. O executor não poderá editar arquivos, aumentar limites durante a execução, instalar algo, acessar a internet ou repetir seletivamente uma falha até obter um passe.

## Baseline factual

| Marco | Evidência | Situação para a campanha futura |
|---|---|---|
| primeira campanha | commit `5a47aae`; composição única ausente | `BLOQUEADA` historicamente |
| primeira remediação | commit `ac12791`; harness correlacionado criado | aceita com limitações |
| repetição anterior | baseline `66d0a9f`; relatório `c50eef5` | `REPROVADA` em `finalising-revocation` |
| remediação de revogação | commit `f9bb567` | Quality Gate próprio aprovado e Human Gate próprio aceito |
| registro da aceitação | commit `67e0187` | somente documentação factual acima da remediação técnica |
| estado corrente | `STATE-06 INTEGRATION` | sem runtime operacional, promoção ou transição |

A inspeção documental realizada para esta proposta confirmou que o intervalo `f9bb567..67e0187` contém somente cinco documentos Markdown. Nenhum arquivo técnico foi alterado nesse intervalo. Essa constatação deverá ser repetida e registrada na futura campanha; ela não é evidência antecipada de execução.

Os resultados do gate próprio da remediação — `332/332` unitários, `30/30` de arquitetura, `16/16` de integração, `60/60` do Dashboard, cobertura de `78,9%/49,51%` e passe do harness Chrome — são baseline histórica. A campanha futura deverá medir tudo novamente e explicar qualquer contagem diferente pelo diff congelado.

## Baseline que deverá ser congelada

A futura autorização deverá nomear o commit exato apresentado no hand-off desta proposta. Esse commit poderá estar acima de `67e0187` apenas por esta proposta e seus registros factuais. Antes do primeiro build, a campanha deverá provar cumulativamente que:

1. `f9bb567` e `67e0187` pertencem à ancestralidade do commit executado;
2. não existe mudança em código, configuração executável, solução, projeto, package, lockfile ou migration depois de `f9bb567`;
3. a worktree está limpa;
4. nenhum submódulo, artefacto gerado ou dependência local divergiu silenciosamente;
5. as ferramentas e caches necessários já estão disponíveis sem rede.

Se qualquer uma dessas condições falhar, a campanha deverá parar antes do build. A proposta não autoriza checkout destrutivo, descarte de trabalho ou alteração para fabricar uma baseline elegível.

## Objetivo

Produzir uma nova classificação automática e reproduzível do Quality Gate consolidado do `STATE-06`, provando que a cadeia correlacionada completa, inclusive sua finalização de revogação remediada, funciona no sandbox local sob budgets, cancelamento, fencing, isolamento e cleanup, sem transformar evidência sintética em alegação operacional.

## Arquitetura da campanha proposta

### Fonte de verdade

- O harness correlacionado será a evidência principal da cadeia integrada.
- API e polling HTTPS de 30 segundos continuarão autoritativos para o Dashboard TV.
- SignalR continuará somente um hint autenticado de mudança.
- A notificação será observada somente no sink de teste.
- O transporte de comando continuará deliberadamente não executável e deverá terminar com zero `CommandAttempt`.
- Os harnesses proprietários serão regressões complementares, não substitutos da cadeia única.

### Fronteiras obrigatórias da revogação

A execução correlacionada somente poderá ser considerada completa se comprovar separadamente:

1. resposta tipada do pedido de revogação;
2. Agent central e todos os certificados de teste commitados como revogados;
3. heartbeat e assignments recusados diretamente pelo Server;
4. primeiro heartbeat Agent-side alcançando o transporte, recebendo a negação e persistindo `RevokedOrDenied`;
5. tentativa posterior de assignments parada localmente, sem novo transporte;
6. preservação do last-known-good de assignments;
7. avanço monotónico dos fences e liberação segura das leases;
8. observação e command poll recusados após a revogação;
9. saída dos subprocessos de comando antes da revogação;
10. zero `CommandAttempt`, executor ou efeito operacional.

Falha deverá produzir somente diagnóstico tipado e allow-listed. Texto de exceção, stack trace, caminhos, tokens, chaves, certificados ou valores com aparência de segredo não poderão entrar no relatório.

### Interleavings R1–R7

A matriz já implementada deverá ser executada como parte da suíte de integração e permanecer determinística:

- `R1`: revogação sem snapshot concorrente;
- `R2`: snapshot concluído antes da revogação;
- `R3`: resposta de snapshot mantida em barrier durante o commit de revogação;
- `R4`: revogação antes da leitura seguinte e publicação do hint;
- `R5`: revogação depois da saída do subprocesso de comando;
- `R6`: avanço do relógio controlado antes da revogação;
- `R7`: lease antigo expirado e substituído por fence superior.

Nenhum `Sleep` poderá substituir os barriers existentes. O relógio real não poderá ser usado para alinhar artificialmente requests ao relógio controlado.

## Sequência futura proposta

### Fase 0 — Shutdown e congelamento

- encerrar somente processos, listeners, janelas e navegadores comprovadamente pertencentes ao DB-Notifier;
- provar que nenhum runtime próprio permanece;
- registrar commit, branch, status Git, versões das ferramentas, espaço livre e diretório temporário;
- verificar a ancestralidade e o diff técnico desde `f9bb567`;
- exigir worktree limpa e dependências já disponíveis localmente.

Resíduo não atribuível, worktree suja, baseline divergente ou necessidade de rede deverá bloquear a campanha antes da execução.

### Fase 1 — Inspeção read-only

- mapear cada achado da repetição anterior para a correção e a evidência atuais;
- confirmar que o diagnóstico granular, a dupla allow-list e a matriz R1–R7 permanecem somente em `tests/`/`scripts/`;
- confirmar ausência de composição do harness no runtime normal;
- confirmar relógio controlado compartilhado, readiness de `90` segundos e budgets já declarados;
- confirmar que a remediação não introduziu package, lockfile, migration ou projeto;
- revisar diretamente os testes de isolamento e sanitização.

### Fase 2 — Gates gerais offline

- integridade Git, `git diff --check` e ausência de alteração produzida pela campanha;
- documentação de código, inglês britânico, links Markdown e headings;
- secret scan sanitizado do worktree e histórico disponível;
- verificação NuGet por fixture offline;
- toolchain, marca, tokens, localização e ícones;
- auditoria npm somente em modo offline e somente se o cache local já permitir;
- confirmação de manifests, packages, lockfiles, migrations e fontes de dependência inalterados.

Cache ausente não autoriza acesso externo. O resultado correspondente deverá ser `BLOQUEADO` ou limitação explícita conforme o requisito afetado.

### Fase 3 — Build, testes e cobertura

- build Release completo da solução .NET 10 com `--no-restore`;
- suítes unitária, arquitetura e integração completas;
- cobertura comparada aos pisos de `70%` de linhas e `45%` de branches;
- `dotnet format --verify-no-changes --no-restore`;
- Dashboard: toolchain, typecheck, testes, build e gates de assets;
- Pester legado no runner compatível e bundle `ValidateOnly`;
- migrations/model drift apenas local e offline;
- smokes fail-closed das composições normais;
- captura dos logs EF para comprovar ausência do warning `Microsoft.EntityFrameworkCore.Query[10102]`.

Mudança de contagem deverá ser explicada pelo diff, nunca ocultada por filtro ou exclusão.

### Fase 4 — Harness correlacionado obrigatório

Executar uma vez, serialmente, o runner consolidado existente por PowerShell 7, com navegador dedicado e perfil efêmero. A execução deverá provar no mesmo run:

1. Agent e identidade humana read-only separados e exclusivamente de teste;
2. enrollment, assignment read-only, heartbeat e Agent SQLite;
3. observação sintética offline, replay e ingestão idempotente;
4. leitura imediata do Dashboard TV e reconciliação independente após 30 segundos;
5. SignalR apenas como hint e concorrência máxima de snapshot `1`;
6. uma entrega da transição posterior à baseline no sink de teste, sem duplicação após reinício;
7. duas entradas de journal e zero `CommandAttempt` no transporte não executável;
8. saída do último subprocesso antes da revogação;
9. todas as fronteiras de revogação descritas nesta proposta;
10. resumo terminal sem falha residual, zero origem HTTP externa e `operationalData=false`.

Uma falha terminal não poderá ser repetida seletivamente. A campanha deverá preservar seu estágio sanitizado e prosseguir somente para cleanup e relatório.

### Fase 5 — Regressões sandbox proprietárias

Somente se a cadeia correlacionada passar, executar serialmente os harnesses já existentes para:

- identidade, enrollment, heartbeat, assignments e revogação;
- resiliência e compatibilidade do Agent Fleet;
- pipeline autoritativo Agent → API;
- Dashboard TV, polling, ETag/`304`, SignalR e recovery no browser;
- notificação reconciliada com baseline silenciosa;
- transporte seguro e deliberadamente não executável de comando;
- matriz R1–R7 e teste negativo de sanitização.

Os harnesses não poderão rodar em paralelo quando compartilharem porta, store, identidade, perfil ou orçamento do host.

### Fase 6 — Contrato PowerShell e cleanup

- usar `pwsh` 7 ou superior nos runners modernos;
- comprovar que Windows PowerShell 5.1 recusa esses runners em `#Requires` antes de criar qualquer recurso;
- preservar o Pester legado no runner compatível;
- encerrar host, filhos e navegador dedicado;
- remover somente profiles, stores e diretórios temporários pertencentes à campanha;
- provar zero processo, listener, profile ou root novo e worktree ainda limpa.

### Fase 7 — Relatório factual novo

Criar um relatório com nome próprio, sem substituir relatórios anteriores. Ele deverá conter:

- baseline congelada, ferramentas, comandos, durações e exit codes;
- sequência correlacionada e resumo sanitizado;
- resultados dos gates gerais, cobertura, EF, PowerShell e R1–R7;
- achados por gravidade;
- separação entre observado, inferido, não testado e bloqueado;
- limitações, cleanup e confirmação de ausência de acesso externo;
- classificação final `APROVADO`, `REPROVADO` ou `BLOQUEADO`;
- indicação de que amostra humana, Human Gate final, promoção e transição continuam separados.

Nome recomendado: `STATE-06-Consolidated-Quality-Gate-Post-Revocation-Remediation-Rerun-Report.md`.

## Envelope de recursos

Os limites são do laboratório, não sizing operacional:

- duração total máxima de `120` minutos;
- timeout máximo por comando de `30` minutos, respeitando limites menores já implementados;
- budget existente de `15` minutos para o runner consolidado;
- readiness único de `90` segundos, sem reiniciar o host até passar;
- `30` segundos por cenário R1–R7 e `4` minutos para a matriz;
- um harness E2E e um navegador dedicado por vez;
- no máximo `2 GiB` de temporários pertencentes à campanha;
- pelo menos `10 GiB` livres no volume temporário antes do início;
- cancelamento no primeiro acesso externo, processo não atribuível ou cleanup inseguro.

Nenhum limite poderá ser ampliado durante a campanha sem nova autorização.

## Critérios de aceite

O Quality Gate consolidado somente poderá ser `APROVADO` se:

1. a baseline exata e a worktree limpa forem provadas e permanecerem íntegros;
2. não houver mudança técnica posterior a `f9bb567` sem revisão e autoridade próprias;
3. todos os gates gerais aplicáveis passarem sem correção durante a campanha;
4. build e suítes completas passarem sem erro ou warning;
5. cobertura permanecer acima dos pisos estabelecidos;
6. R1–R7 e sanitização passarem dentro dos budgets;
7. o harness correlacionado terminar toda a cadeia, inclusive a revogação granular;
8. commit central, negação direta, quarentena local, LKG e fences forem comprovados separadamente;
9. API/polling permanecerem autoritativos e SignalR apenas um hint;
10. notificação ocorrer uma vez depois da baseline e não se repetir;
11. o transporte terminar com zero `CommandAttempt`, executor ou efeito;
12. o warning EF `10102` não reaparecer;
13. PowerShell 5.1 recusar os runners modernos antes de criar recursos;
14. não existir achado crítico ou alto aberto;
15. não ocorrer acesso externo, download, alteração rastreada ou violação de autoridade;
16. cleanup completo e worktree limpa forem provados;
17. o relatório preservar todas as limitações do sandbox.

Resultado parcial não poderá ser convertido em aprovação.

## Classificação do gate

- `APROVADO`: todos os critérios obrigatórios têm evidência e não há achado crítico ou alto aberto;
- `REPROVADO`: uma falha técnica reproduzida viola requisito, segurança ou integridade da evidência;
- `BLOQUEADO`: falta autoridade, ambiente, cache, dependência ou evidência indispensável;
- `NÃO APLICÁVEL`: permitido somente para verificação realmente externa ao escopo, com justificativa.

A nova classificação valerá somente para a baseline executada. Os relatórios históricos continuarão registrando corretamente seus resultados anteriores.

## Gravidade dos achados

- `CRÍTICA`: ação real, segredo exposto, bypass de identidade/autorização ou recurso externo afetado;
- `ALTA`: perda ou duplicidade autoritativa, execução implícita, regressão fail-closed ou cadeia obrigatória incompleta;
- `MÉDIA`: falha de budget, cancelamento, fencing, compatibilidade, diagnóstico obrigatório ou cleanup recuperável;
- `BAIXA`: precisão documental ou manutenção sem impacto no critério obrigatório;
- `FERRAMENTA/AMBIENTE`: limitação reproduzível do host, sem ser convertida em passe.

A campanha somente registra achados. Ela não autoriza remediação.

## Fora de escopo absoluto

- implementação, correção, refactor, formatação aplicável ou criação de harness;
- alteração de código, configuração executável, solução, projeto, package, lockfile ou migration;
- restore externo, download, registry, CDN ou acesso à internet;
- recurso, credencial, certificado, Agent, provider, banco, IdP, PKI, vault ou canal operacional;
- PostgreSQL real, banco monitorado, serviço permanente ou infraestrutura externa;
- comando administrativo, `CommandAttempt`, executor, shell, post-probe, `Start`, `Stop` ou `Restart`;
- notificação Windows visível ou canal externo;
- UI administrativa, monitoramento real, carga representativa, endurance, HA ou disaster recovery;
- LLM, recomendação, planejamento, automação ou promoção `none → OBSERVER`;
- deploy, publicação, instalação, amostra humana, Human Gate final, `STATE-07` ou transição.

## Riscos e limitações residuais

- A exceção original da falha histórica continua irrecuperável; a nova campanha prova o comportamento atual, não reconstrói o passado.
- Server SQLite em memória não prova PostgreSQL, restart distribuído ou power-loss operacional.
- R3 prova sobreposição na fronteira HTTP, não locking equivalente a PostgreSQL.
- Agent SQLite, identidades, certificados, assignments, observações e comandos continuam sintéticos e efêmeros.
- Um único host e navegador não provam escala, fairness, homologação ampla ou isolamento de rede operacional.
- SignalR continua best-effort e não substitui a reconciliação periódica.
- O sink prova entrega interna, não apresentação de notificação Windows.
- O transporte prova persistência, acknowledgement e recusa segura, não execução administrativa.
- A auditoria npm offline não comprova advisories posteriores ao cache local.
- Um resultado automático aprovado ainda não inclui experiência humana nem autoriza transição.

## Condições de parada

A futura campanha deverá parar e classificar o resultado quando:

- shutdown, ownership, baseline ou worktree limpa não puderem ser provados;
- aparecer mudança técnica posterior a `f9bb567` sem revisão própria;
- surgir necessidade de editar, restaurar, instalar, baixar ou acessar rede;
- qualquer fluxo alcançar recurso operacional, comando, attempt, executor ou canal externo;
- um budget for excedido;
- a cadeia correlacionada ou uma fronteira de revogação obrigatória falhar;
- o diagnóstico necessário não puder ser sanitizado;
- processo, listener, profile ou store próprio não puder ser encerrado/removido com segurança;
- a campanha produzir alteração rastreada não documental.

Falha técnica comprovada será `REPROVADO`; falta de autoridade ou evidência indispensável será `BLOQUEADO`.

## Entregáveis futuros

- relatório factual novo e separado;
- matriz de rastreabilidade do fechamento do `STATE-06`;
- resumo sanitizado da cadeia e da revogação terminal;
- inventário de comandos, versões, resultados e cleanup;
- classificação automática consolidada;
- atualização factual do estado, plano e histórico;
- commit local exclusivamente documental do relatório.

Logs brutos, bancos, certificados, chaves, tokens, profiles, binários e temporários não integrarão o commit.

## Decisão futura de Bruno

Se Bruno concordar e desejar autorizar somente esta nova repetição automática, o texto sugerido é:

> AUTORIZO exclusivamente a nova repetição da Campanha Consolidada de Quality Gate do STATE-06 no commit corrente informado no hand-off, cuja baseline técnica deverá conter `f9bb567` e cuja baseline factual deverá conter `67e0187`, limitada a shutdown preflight, congelamento e inspeção read-only, verificações integralmente offline, build, testes, cobertura, R1–R7, execução serial dos harnesses sandbox existentes e do harness correlacionado Agent → API → Dashboard/SignalR → notificação em sink de teste → transporte de comando deliberadamente não executável, revogação granular, replay, falhas, warnings EF, contrato PowerShell, cleanup e relatório factual novo. Autorizo runtimes temporários exclusivamente locais e Chrome dedicado com perfil efêmero, que deverão ser encerrados e removidos ao final. A campanha não poderá alterar nem corrigir código, configuração executável, solução, projetos, packages, lockfiles ou migrations; falha ou lacuna deverá ser registrada como REPROVADA ou BLOQUEADA e voltar para autorização separada. Permanecem proibidos acesso externo, downloads, recursos ou credenciais operacionais, provider/banco real, comando administrativo, CommandAttempt, executor, shell, serviço/infraestrutura afetados, canais externos, notificação Windows visível, LLM, deploy, amostra humana, Human Gate final, promoção e transição de estado.

Esse texto ainda não foi emitido como autorização. Ler, aceitar ou citar esta proposta não inicia a campanha.

## Próximo passo para Bruno

1. Leia principalmente `Resultado em linguagem simples`, `Baseline factual`, `Arquitetura da campanha proposta`, `Critérios de aceite`, `Fora de escopo absoluto` e `Riscos e limitações residuais`.
2. Se algum limite estiver incorreto, responda somente com os pontos que deseja alterar.
3. Se concordar e quiser executar a campanha automática, envie exatamente o texto da seção `Decisão futura de Bruno`.
4. Não autorize junto dessa decisão amostra humana, Human Gate final, promoção ou transição.

Nenhuma ação técnica é necessária ou autorizada enquanto esta proposta estiver somente em revisão.
