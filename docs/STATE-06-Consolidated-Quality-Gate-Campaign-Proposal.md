# Proposta STATE-06 — Campanha Consolidada de Quality Gate

> **Status factual posterior (2026-07-19):** Bruno autorizou separadamente a campanha automática sobre o commit `5a47aae`. Os checks e harnesses existentes passaram, mas o [relatório consolidado](STATE-06-Consolidated-Quality-Gate-Campaign-Report.md) classificou o Quality Gate como `BLOQUEADO` porque não existe uma composição única correlacionando os quatro incrementos sem criar novo harness. Nenhuma remediação, Human Gate final, promoção ou transição foi autorizada.

## Status e autoridade

- Data: 2026-07-19.
- Estado mantido: `STATE-06 INTEGRATION`.
- Tipo: apresentação exclusivamente documental da campanha consolidada prevista no [plano de fechamento](STATE-06-Consolidated-Closure-Plan.md).
- Execução da campanha, build, testes de produto, runtimes, navegador, WPF e geração de evidência operacional: `NÃO AUTORIZADOS` nesta etapa.
- Implementação, remediação, alteração de código/configuração executável, projeto, pacote, lockfile ou migration: `NÃO AUTORIZADOS`.
- Acesso externo, download, provider/banco/credencial operacional, comando, deploy, promoção e transição: `NÃO AUTORIZADOS`.
- Quality Gate consolidado e Human Gate final do `STATE-06`: `PENDENTES` e não inferidos.

Este documento apresenta como a campanha poderá ser executada futuramente. Ele não executa a campanha, não declara o `STATE-06` concluído e não autoriza nenhuma das atividades descritas abaixo. O texto de autorização ao final é apenas uma sugestão para decisão posterior de Bruno.

## Resultado em linguagem simples

Os quatro incrementos técnicos do plano de fechamento já foram concluídos e aceitos com limitações. Cada um provou uma parte do percurso:

1. uma fonte fictícia enviou observações pelo Agent até a API e o Dashboard;
2. SignalR avisou o Dashboard de que poderia haver mudança, sem substituir a consulta à API;
3. uma mudança reconciliada produziu uma única solicitação local de notificação;
4. mensagens de comando deliberadamente não executáveis foram transportadas, repetidas e recusadas com segurança.

A campanha consolidada será a auditoria que confere essas quatro peças em conjunto. Ela deverá repetir os testes gerais do repositório, tentar percorrer uma única sequência local controlada, provocar falhas conhecidas e produzir um relatório que diga exatamente o que funcionou, o que foi apenas inferido, o que não foi testado e o que ficou bloqueado.

Essa campanha não é um novo incremento. Ela não pode criar a peça que estiver faltando nem corrigir silenciosamente um defeito. Se a composição única exigir código novo, mudança de configuração executável, pacote, download ou ampliação de autoridade, a execução deverá parar e classificar a evidência correspondente como `BLOQUEADA` ou `REPROVADA`. Uma remediação futura exigirá autorização própria.

## Baseline factual

| Parte | Evidência aceita | Limite preservado |
|---|---|---|
| Incremento 1 — pipeline autoritativo | commit `3449918`; Quality Gate restrito aprovado e Human Gate próprio aceito com limitações | fonte, identidade, persistência e transporte exclusivamente sintéticos/locais |
| Incremento 2 — hint SignalR | commit `c945c1b`; Quality Gate restrito aprovado e Human Gate próprio aceito com limitações | SignalR é somente hint; API e polling continuam autoritativos |
| Incremento 3 — notificação reconciliada | commit `d43e49a`; Quality Gate restrito aprovado e Human Gate próprio aceito com limitações | entrega automática usa sink de teste; apresentação Windows humana não foi provada nesse incremento |
| Incremento 4 — transporte seguro de comando | commit final `54a65f5`; Quality Gate restrito aprovado e Human Gate próprio aceito com limitações | fixtures deliberadamente não executáveis; nenhum `CommandAttempt`, executor ou efeito administrativo |

A baseline automática mais recente registrou solução .NET 10 com `16` projetos, `332/332` testes unitários, `25/25` de arquitetura, `14/14` de integração, `60/60` do Dashboard, cobertura .NET de `78,91%` de linhas e `49,51%` de branches, Pester compatível com `23` testes aprovados e um skip previsto, além dos gates de formato, documentação, links, NuGet offline, secrets, assets/localização e smoke fail-closed.

Esses números são evidência histórica dos incrementos, não resultado da campanha futura. A campanha deverá medi-los novamente sem presumir que continuam válidos.

## Lacuna que a campanha deve resolver

Os relatórios atuais comprovam as partes em harnesses próprios. Ainda não existe um relatório consolidado que correlacione, numa mesma execução governada:

- identidade de teste e revogação;
- observação canônica, outbox e reconexão;
- ingestão, projeção e snapshot autoritativo;
- leitura inicial, hint SignalR e reconciliação serial de 30 segundos;
- transição reconciliada e deduplicação da notificação local;
- transporte não executável de comando;
- falha fechada, preservação de stale e cleanup integral.

A primeira atividade técnica futura deverá verificar se os harnesses existentes podem ser compostos sem alteração. Ausência dessa capacidade não autoriza a criação de um novo harness durante a campanha.

## Nome recomendado

`STATE-06 — Consolidated Integration Quality Gate Campaign`

Em português: `Campanha Consolidada de Quality Gate da Integração STATE-06`.

## Objetivo

Produzir evidência automática, sanitizada e reproduzível de que os critérios de saída do `STATE-06` estão atendidos no sandbox local autorizado, mantendo as composições normais desabilitadas, sem recurso operacional, sem ação administrativa e sem transformar limitações de laboratório em alegação de produção.

## O que a campanha é — e o que não é

| Item | Significado |
|---|---|
| Campanha automática | inspeção, builds, testes e harnesses locais existentes, seguidos de relatório factual |
| Quality Gate consolidado | classificação técnica `APROVADO`, `REPROVADO`, `BLOQUEADO` ou `NÃO APLICÁVEL` |
| Human Gate final | decisão posterior de Bruno, baseada no relatório e em amostras humanas separadamente autorizadas |
| Transição para `STATE-07` | decisão ainda posterior e independente, mesmo se os dois gates forem aprovados |
| Remediação | mudança de produto ou teste que não pertence à campanha e exige nova autorização |

## Princípios obrigatórios

1. executar somente sobre o commit e a worktree identificados no início;
2. usar exclusivamente dados, identidades, certificados, bancos e endpoints de teste locais;
3. preservar API e polling como fontes autoritativas;
4. manter concorrência de leitura máxima igual a `1` e respeitar budgets existentes;
5. nunca ativar a composição normal do Agent, API, Dashboard, WPF ou command polling;
6. nunca converter acknowledgement em prova de execução;
7. não corrigir, regenerar ou atualizar silenciosamente qualquer artefato;
8. encerrar todos os processos, listeners, perfis e stores temporários antes do relatório;
9. classificar cada alegação como observada, inferida, não testada ou bloqueada;
10. manter o Human Gate e a transição fora da classificação automática.

## Sequência proposta da campanha

### Fase 0 — Preflight e congelamento da baseline

- executar o shutdown preflight e exigir zero processo/listener DB-Notifier;
- confirmar branch, commit, status Git e ausência de mudança preexistente não relacionada;
- inventariar SDK .NET, Node/npm, PowerShell/Pester, Chrome disponível e dependências locais;
- confirmar espaço temporário e que nenhum comando precisará de rede ou download;
- registrar hashes dos manifestos/lockfiles relevantes sem alterá-los.

Se o repositório não estiver limpo ou o shutdown não puder ser provado, a campanha deverá parar antes do primeiro build.

### Fase 1 — Auditoria estática e de supply chain offline

- revisão direta do diff entre a baseline aceita e o commit corrente, sem reescrever histórico;
- `git diff --check`, integridade Git e inspeção de arquivos inesperados;
- documentação/comments, links Markdown, assets, tokens e localização;
- secret scan do worktree e histórico disponível, sem imprimir valores encontrados;
- verificação NuGet/npm somente com metadados e caches já disponíveis;
- confirmação de versões fixadas e ausência de alteração em package/lockfile.

Ausência de cache necessário deverá ser registrada como limitação ou bloqueio. Não autoriza acesso ao registry.

### Fase 2 — Build e suítes completas

- build Release da solução .NET 10 sem restore externo;
- testes unitários, arquitetura e integração de toda a solução;
- cobertura .NET comparada aos pisos vigentes de `70%` de linhas e `45%` de branches;
- formatação/analyzers sem alteração automática;
- toolchain, typecheck, testes e build do Dashboard com dependências já presentes;
- Pester legado pelo runner compatível e bundle `ValidateOnly`;
- verificação de migrations/model drift somente local e offline;
- smoke fail-closed da composição normal.

Qualquer comando que proponha alterar fonte, formato, lockfile, migration ou saída rastreada deverá ser interrompido. A campanha apenas verifica.

### Fase 3 — Composição E2E autoritativa

Somente se os harnesses atuais permitirem a composição sem mudança, executar em processos locais separados:

1. criar identidade e certificados efêmeros exclusivamente de teste;
2. fazer enrollment controlado e estabelecer heartbeat/assignment read-only necessários;
3. emitir uma observação canônica sintética;
4. persistir observação e outbox no SQLite Agent;
5. interromper a API em ponto controlado e confirmar preservação offline;
6. reiniciar o processo Agent e reconciliar a mesma mensagem sem duplicidade;
7. ingerir e projetar a observação na API local;
8. ler o snapshot Dashboard TV imediatamente ao entrar no modo sandbox;
9. confirmar que um hint SignalR provoca apenas uma releitura autoritativa coalescida;
10. confirmar uma reconciliação periódica posterior, iniciada 30 segundos depois da conclusão da leitura anterior;
11. preservar o último snapshot factual como stale durante falha controlada.

O Dashboard nunca poderá receber a observação diretamente do Agent ou do SignalR.

### Fase 4 — Transição reconciliada e notificação automática

- estabelecer baseline inicial silenciosa;
- commitar uma transição sintética posterior à baseline;
- ler a transição pela projeção read-only;
- registrar intent no ledger e entregar uma vez ao sink automático de teste;
- repetir a evidência por replay, reconnect e restart sem nova entrega;
- verificar opt-out, silêncio, cancelamento, stale/unknown e fencing;
- não abrir amostra visual nem afirmar que o Windows apresentou uma notificação.

A publicação Windows visível pertence às amostras humanas futuras e exige autorização específica.

### Fase 5 — Transporte seguro de comando

- transportar apenas fixtures `sandbox.command.*` com `CommandExecutionPolicy.Never`;
- comprovar expiração, incompatibilidade, gap, reorder, duplicidade, perda de resposta, retry e replay;
- comprovar resposta estável depois do commit e recusa de nova mensagem após revogação;
- exigir zero `CommandAttempt`, `Running`, `Succeeded`, provider result ou post-probe;
- confirmar que o Worker normal continua recusando command polling.

Nenhum `Start`, `Stop`, `Restart`, shell, processo, serviço, banco ou infraestrutura poderá ser alcançado.

### Fase 6 — Matriz de falhas e recuperação

| Falha controlada | Resultado obrigatório |
|---|---|
| API indisponível antes do envio | outbox preservada; nenhuma perda ou falsa confirmação |
| resposta perdida depois do commit | replay exato; uma única mutação autoritativa |
| mensagem duplicada ou reordenada | nenhuma regressão, duplicidade ou notificação nova |
| contrato incompatível | recusa tipada e preservação do último estado válido |
| identidade revogada | novas operações recusadas antes do store; snapshot anterior torna-se stale |
| SignalR ausente/desconectado | polling periódico continua autoritativo |
| tempestade de hints | coalescência e concorrência HTTP máxima `1` |
| cancelamento ou timeout | recursos liberados; estado reiniciável e factual |
| falha de ledger/notificação | estado sanitizado, sem repetição indevida e sem alegação exactly-once |
| fixture de comando inválida | recusa sem attempt, executor ou efeito |

### Fase 7 — Cleanup e relatório

- encerrar Agent, API, Dashboard, WPF de teste, Chrome dedicado e processos filhos;
- remover somente perfis, certificados, pipes, stores e diretórios temporários criados pela campanha;
- provar zero processo/listener/profile/store temporário pertencente à campanha;
- confirmar worktree sem artefato inesperado;
- produzir relatório consolidado e matriz `Lifecycle → requisito → evidência → classificação`;
- classificar achados por gravidade e separar correção recomendada de autoridade para corrigir.

## Envelope de recursos proposto

Os valores abaixo são limites de segurança da campanha local, não sizing operacional:

- prazo total máximo da campanha automática: `120 minutos`;
- prazo máximo por comando individual: `30 minutos`, salvo timeout menor já imposto pelo próprio teste;
- no máximo um harness E2E com runtimes ativos por vez;
- no máximo um Chrome dedicado, sempre com perfil efêmero isolado;
- nenhuma paralelização manual entre suítes que disputem os mesmos stores, portas ou perfis;
- até `2 GiB` de artefatos temporários próprios da campanha;
- não iniciar se houver menos de `10 GiB` livres no volume temporário;
- cancelar no primeiro resíduo de runtime não atribuível com segurança ou na primeira necessidade de acesso externo.

Exceder um limite não autoriza aumentá-lo durante a execução. O fato deve ser registrado e levado a Bruno.

## Evidência obrigatória

- commit, branch, worktree, ambiente e versões de ferramentas;
- comandos executados, diretórios, duração e exit codes;
- contagens de testes e cobertura sem exclusão nova;
- linha do tempo UTC correlacionada por IDs sanitizados da sequência E2E;
- contagens antes/depois de outbox, ingestão, projeção, leituras, hints, transições, ledger e journal;
- concorrência máxima observada, retries, backoffs, cancelamentos e fences;
- hashes/IDs de teste sanitizados, nunca chaves, tokens ou certificados completos;
- lista de processos/listeners/perfis/stores criada e respectivo cleanup;
- achados por gravidade, limitações e itens não testados;
- relatório final com classificação do Quality Gate consolidado.

Screenshots só serão usados em amostra humana posterior. Evidência automática deve preferir logs estruturados e sanitizados.

## Critérios de aceite do Quality Gate consolidado

O resultado só poderá ser `APROVADO` se:

1. os gates gerais aplicáveis passarem sem alteração corretiva durante a campanha;
2. a sequência E2E autoritativa for observada sem ligação direta indevida entre componentes;
3. reconexão, duplicidade, reorder, versão e expiração tiverem evidência controlada;
4. leitura inicial, hint e reconciliação de 30 segundos preservarem concorrência máxima `1`;
5. falha preservar o último snapshot como stale/unknown, nunca saudável por inferência;
6. a notificação automática partir somente de transição reconciliada posterior à baseline e não se repetir;
7. o transporte de comando terminar sem attempt ou execução;
8. não houver achado crítico ou alto conhecido aberto;
9. nenhuma proibição ou limite de autoridade tiver sido violado;
10. o cleanup final e a worktree limpa forem comprovados;
11. o relatório distinguir observado, inferido, não testado e bloqueado;
12. nenhuma limitação de sandbox for apresentada como prova operacional.

## Classificação do gate

- `APROVADO`: todos os critérios obrigatórios possuem evidência e não há achado crítico/alto aberto;
- `REPROVADO`: uma falha técnica comprovada viola requisito ou segurança;
- `BLOQUEADO`: uma evidência obrigatória não pode ser produzida sem código, acesso ou autoridade adicionais;
- `NÃO APLICÁVEL`: permitido somente para verificação realmente fora de `STATE-06`, com justificativa factual.

Um resultado misto não será arredondado para aprovação. Cada requisito terá sua própria classificação, e o resultado global seguirá a condição obrigatória mais restritiva.

## Gravidade dos achados

- `CRÍTICA`: possibilidade de ação real, exposição de segredo, bypass de identidade/autorização ou resíduo externo;
- `ALTA`: perda/duplicidade autoritativa, execução implícita, regressão de fail-closed ou falsa alegação de saúde;
- `MÉDIA`: budget, cancelamento, fencing, cleanup, compatibilidade ou evidência incompleta sem efeito crítico demonstrado;
- `BAIXA`: precisão documental, diagnóstico ou manutenção sem impacto no gate obrigatório;
- `FERRAMENTA/AMBIENTE`: limitação reproduzível do host, registrada sem ser convertida em passe.

A campanha não corrige achados. Ela relata impacto, reprodução e remediação recomendada.

## Entregáveis futuros da campanha

- relatório `STATE-06-Consolidated-Quality-Gate-Campaign-Report.md`;
- matriz de rastreabilidade dos critérios de saída do `STATE-06`;
- resumo sanitizado da sequência E2E e da matriz de falhas;
- inventário de comandos, versões, resultados e cleanup;
- classificação técnica consolidada;
- atualização factual de `Current-State.md`, plano e histórico;
- commit documental focado do relatório.

Nenhum artefato binário, log bruto, certificado, chave, token, banco, perfil de navegador ou diretório temporário integrará o commit.

## Human Gate posterior

Mesmo com Quality Gate automático aprovado, o Human Gate final continuará pendente. Bruno deverá receber o relatório consolidado e, sob nova autorização específica, repetir as amostras críticas previstas no plano:

- desconexão/reconexão do Agent;
- leitura inicial, hint e reconciliação posterior do Dashboard TV;
- uma notificação local posterior à baseline e ausência de repetição;
- confirmação de que comando expirado/incompatível não foi executado;
- distinção visível entre sandbox, stale/unknown e suporte apenas planejado.

A campanha automática não abre nem pré-aprova essas amostras. A aprovação humana também não promove o projeto automaticamente.

## Fora de escopo absoluto

- qualquer implementação, correção ou novo harness;
- alteração de código, configuração executável, solução, projeto, pacote, lockfile ou migration;
- restore/download, registry externo, CDN ou acesso à internet;
- banco, provider, Agent, credencial, certificado, IdP, PKI, vault ou canal operacional;
- PostgreSQL real, database monitorado, serviço permanente ou infraestrutura externa;
- comando administrativo, `CommandAttempt`, executor, shell, post-probe, `Start`, `Stop` ou `Restart`;
- e-mail, SMS, webhook, Teams, Slack ou outra notificação externa;
- carga representativa, endurance, HA, disaster recovery ou homologação;
- LLM, recomendação, planejamento, automação ou promoção `none → OBSERVER`;
- deploy, publicação, instalação, `STATE-07`, `STATE-08` ou transição de estado.

## Riscos e limitações residuais

- Os harnesses atuais foram construídos por incremento; a campanha pode descobrir que não existe composição única executável sem código novo.
- O Server de partes do E2E usa SQLite em memória e não prova PostgreSQL operacional, restart durável do Server ou power-loss físico.
- Certificados, enrollment e revogação continuam exclusivamente de teste e não provam PKI distribuída.
- SignalR continua best-effort; o polling é obrigatório para autoridade e recuperação.
- O ledger de notificação e a API Windows não oferecem exactly-once, e aceite local não prova apresentação visível.
- Backpressure e budgets são envelopes locais, não fairness, escala ou sizing de frota.
- O transporte de comandos prova somente persistência e recusa segura; não homologa nenhuma ação.
- Auditorias de dependência offline não substituem advisories atualizados do registry.
- Uma única máquina, um Chrome e Windows local não provam compatibilidade ampla.
- As aceitações dos quatro incrementos não substituem o Quality Gate consolidado nem o Human Gate final.

## Condições de parada

A campanha futura deverá parar imediatamente se:

- o shutdown preflight ou a atribuição de processo/listener não puder ser provado;
- houver mudança preexistente que possa ser confundida com evidência da campanha;
- for necessário alterar ou criar código, teste, harness, configuração executável ou migration;
- faltar dependência/cache e a continuação exigir rede ou download;
- aparecer segredo, credencial, certificado ou recurso não exclusivamente de teste;
- um fluxo alcançar command attempt, executor, shell, serviço, banco ou infraestrutura;
- cleanup não conseguir remover apenas os recursos comprovadamente pertencentes à campanha;
- um limite de tempo, disco, concorrência ou escopo for excedido;
- a evidência obrigatória não puder ser sanitizada sem perder sua verificabilidade.

## Decisão futura de Bruno

Se Bruno concordar com esta proposta e desejar autorizar somente a campanha automática, o texto sugerido é:

> AUTORIZO exclusivamente a Campanha Consolidada de Quality Gate do STATE-06, limitada à inspeção read-only do commit corrente, verificações offline, build/testes dos artefatos existentes e execução dos harnesses sandbox já implementados para identidade de teste, pipeline autoritativo Agent → API → interfaces, hint SignalR, reconciliação de 30 segundos, notificação automática em sink de teste, transporte deliberadamente não executável de comandos, falhas, revogação, replay, cleanup e relatório factual consolidado. Autorizo runtimes temporários exclusivamente locais e um navegador dedicado com perfil efêmero, que deverão ser encerrados e removidos ao final. A campanha não poderá alterar código, configuração executável, solução, projetos, packages, lockfiles ou migrations, nem corrigir achados; qualquer lacuna que exija mudança deverá ser registrada como REPROVADA ou BLOQUEADA e voltar para autorização separada. Permanecem proibidos acesso externo, downloads, recursos ou credenciais operacionais, provider/banco real, comando administrativo, CommandAttempt, executor, shell, serviço/infraestrutura afetados, canais externos, LLM, deploy, amostra humana, promoção e transição de estado.

Esse texto ainda não foi emitido como autorização. Revisar ou citar esta proposta não inicia a campanha.

## Próximo passo para Bruno

1. Leia `Resultado em linguagem simples`, `Lacuna que a campanha deve resolver`, `Sequência proposta da campanha`, `Critérios de aceite`, `Fora de escopo absoluto` e `Riscos e limitações residuais`.
2. Se algum limite estiver incorreto, responda somente com os pontos que deseja alterar.
3. Se concordar e quiser executar a campanha automática, envie exatamente o texto da seção `Decisão futura de Bruno`.
4. Depois da eventual execução, leia o relatório consolidado antes de autorizar qualquer amostra humana.

Nenhuma ação técnica é necessária enquanto Bruno estiver apenas revisando este documento.
