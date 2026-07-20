# Proposta STATE-06 — Amostras humanas finais

## Status e autoridade

- Data: 2026-07-20.
- Estado mantido: `STATE-06 INTEGRATION`.
- Tipo: proposta exclusivamente documental para uma futura sessão de amostras humanas.
- Baseline automática aceita: commit `84217c64312a024ec4f286adfe4872184a21849c`.
- Relatório factual aceito: commit `2c1e05fd8ad4dec2174682fee66aafbd92efc6ee`.
- Quality Gate consolidado atual: `APROVADO` com as limitações registradas.
- Execução, build, testes de produto, runtime, browser e apresentação Windows: `NÃO AUTORIZADOS` por esta proposta.
- Human Gate final, promoção e transição: `NÃO AUTORIZADOS` e não inferidos.

Bruno aceitou o resultado da nova repetição automática e autorizou exclusivamente a elaboração desta proposta. Nenhuma amostra foi aberta, repetida ou aprovada nesta atividade. Este documento não decide o Human Gate do `STATE-06` e não autoriza sua própria execução.

## Resultado em linguagem simples

Os testes automáticos passaram. Falta agora uma sessão curta em que Bruno veja e confirme os comportamentos mais importantes com os próprios olhos: perda e recuperação da conexão local, atualização do Dashboard TV, uma notificação local sem repetição e a indicação clara de que os dados são fictícios. Para o transporte de comando, Bruno verá apenas evidência sanitizada de que o item foi recusado e de que nenhuma ação foi executada.

Essa sessão não usará banco, provider, Agent, credencial ou infraestrutura reais. Tudo continuará no laboratório local, com identidades e dados de teste. O Chrome deverá ser um processo dedicado com perfil temporário, e qualquer WPF usado para a notificação deverá ser iniciado somente na composição sandbox existente e com opt-in explícito.

Existe uma condição importante: o harness automático atual usa Chrome headless e sink de notificação em memória. Portanto, antes de mostrar qualquer amostra, o executor deverá confirmar sem alterar arquivos que os componentes já aceitos conseguem oferecer uma apresentação humana estável. Se isso não for possível, a amostra afetada será `BLOQUEADA`; não será permitido criar ou corrigir código durante a sessão.

## Baseline factual

| Fato | Evidência atual | Consequência para a futura sessão |
|---|---|---|
| Quality Gate consolidado | `APROVADO` na baseline `84217c6` | permite planejar as amostras; não as executa |
| relatório da campanha | commit `2c1e05f`, aceito com limitações | será a referência automática do futuro Human Gate |
| cadeia correlacionada | Agent → API → Dashboard/SignalR → sink → comando não executável | deverá ser preservada sem alegação operacional |
| Dashboard TV | leitura imediata, hint e reconciliação de 30 segundos provados automaticamente | deverá ser apresentado em Chrome dedicado e visível |
| notificação | exatamente uma entrega no sink em memória | não prova apresentação visível pelo Windows |
| publicação WPF | adapter sandbox opt-in e caminho Windows existente | elegibilidade visual deverá ser comprovada antes da amostra |
| comando | dois journals sintéticos e zero `CommandAttempt` | somente evidência sanitizada será mostrada; nenhum comando será executado |
| dados e identidades | sintéticos e efêmeros | rótulos visíveis não poderão sugerir operação real |
| estado do ciclo de vida | `STATE-06 INTEGRATION` | Human Gate final e transição continuam separados |

A campanha histórica bloqueada e a repetição histórica reprovada não serão apagadas. A futura sessão avaliará somente a baseline corrente elegível e suas limitações conhecidas.

## Objetivo

Permitir que Bruno repita e classifique as amostras humanas mínimas exigidas para o `STATE-06`, com evidência sanitizada, limites claros e cleanup integral, sem transformar observação humana em autorização operacional ou transição automática.

## Arquitetura proposta para a sessão

### 1. Preflight e congelamento

Antes de iniciar qualquer componente, o executor deverá:

1. encerrar e comprovar a ausência de runtimes pertencentes ao DB-Notifier;
2. registrar branch, commit, worktree e ancestralidade de `84217c6` e `2c1e05f`;
3. confirmar que qualquer diferença posterior é exclusivamente documental;
4. confirmar ausência de acesso externo e disponibilidade offline dos artefatos existentes;
5. identificar os processos, listeners, pastas temporárias e perfis que pertencerão à sessão;
6. fazer uma inspeção read-only de elegibilidade de cada amostra.

Worktree suja, mudança técnica posterior à baseline, dependência ausente ou apresentação que exija edição impedem a abertura da sessão. Nenhum trabalho do utilizador poderá ser descartado para fabricar uma baseline elegível.

### 2. Superfícies permitidas

- um host sandbox HTTPS em loopback, temporário e já existente;
- processos Agent/API exclusivamente sintéticos e já implementados sob `tests/`;
- um Chrome visível, dedicado e com perfil efêmero, sem reutilizar o navegador normal de Bruno;
- o Dashboard TV sandbox existente, com API/polling autoritativos e SignalR somente como hint;
- um processo WPF sandbox existente, somente se a inspeção confirmar todos os guards locais e o caminho de encerramento;
- uma única notificação Windows local e sintética, seguida de uma repetição deliberadamente suprimida;
- um resumo de evidência sanitizada para os fatos que não são visuais.

Os componentes deverão ser iniciados serialmente, com ownership por PID/caminho/linha de comando. A sessão não poderá anexar automação ao navegador comum, encerrar processos alheios ou deixar runtime aberto após a coleta da decisão de cada amostra.

### 3. Separação de papéis

- O executor prepara o laboratório, conduz os passos e registra somente fatos observáveis.
- Bruno observa a superfície, pede repetição quando necessário e classifica cada amostra.
- Os verificadores automáticos fornecem contagens e tempos; não respondem por Bruno.
- O relatório posterior preserva observação automática, observação humana, inferência e item não testado em campos distintos.

O executor não poderá sugerir a classificação humana nem preencher antecipadamente uma decisão.

## Amostras propostas

### `S06-HG-001` — perda e recuperação da conexão local do Agent

**Preparação:** iniciar somente a cadeia sintética elegível, confirmar uma observação aceita e exibir no Dashboard a origem local de sandbox e o horário/freshness do último snapshot.

**Ação conduzida:** interromper de forma controlada apenas o transporte local pertencente à amostra, preservar a observação pendente e restabelecer a conexão. Não encerrar ou controlar banco, serviço ou processo alheio.

**O que Bruno deverá observar:**

1. estado local indisponível/stale sem apagar o último fato válido;
2. recuperação posterior para o snapshot autoritativo;
3. ausência de duas entradas visuais para a mesma observação.

**Evidência complementar:** contadores sanitizados deverão mostrar a mesma observação preservada offline, ingerida uma vez e sem efeito duplicado.

**Aceite:** Bruno confirma recuperação compreensível e ausência aparente de perda/duplicidade; os contadores automáticos correspondentes permanecem válidos. Se a conexão do Agent não puder ser apresentada de forma distinguível do offline do navegador, a amostra será `BLOQUEADA`, não aprovada por aproximação.

### `S06-HG-002` — leitura imediata, hint e reconciliação do Dashboard TV

**Preparação:** abrir o Dashboard no Chrome dedicado e entrar no modo TV com a fonte sintética explicitamente visível.

**Ação conduzida:** observar a leitura inicial, avançar uma única transição sintética para produzir um hint e aguardar a reconciliação periódica independente de 30 segundos.

**O que Bruno deverá observar:**

1. snapshot disponível logo após entrar no modo TV;
2. atualização antecipada depois do hint;
3. permanência da atualização durante a releitura periódica;
4. interface responsiva e indicação factual da fonte.

**Evidência complementar:** horários das três leituras, `ETag`/`304`, hint autenticado e concorrência máxima `1`, sem expor token ou certificado.

**Aceite:** os três momentos são distinguíveis e coerentes; a evidência comprova que o hint não substituiu o polling e que não houve pedidos sobrepostos.

### `S06-HG-003` — uma notificação local posterior à baseline

**Preparação:** iniciar o WPF somente com os guards sandbox existentes, opt-in explícito, endpoint HTTPS loopback, identidade de teste e ledger sob pasta temporária própria. A leitura inicial deverá permanecer silenciosa.

**Ação conduzida:** commitar uma única transição sintética posterior à baseline e aguardar o ciclo local limitado.

**O que Bruno deverá observar:** exatamente uma notificação local cujo texto identifique `DB Notifier`, origem sintética/local, mudança de estado, horário/freshness e ausência de dados externos.

**Evidência complementar:** cursor, event ID e resultado de publicação deverão ser representados apenas por valores agregados ou hashes abreviados sanitizados.

**Aceite:** Bruno classifica a mensagem como visível e factualmente clara. O aceite da API Windows ou do fallback não será tratado como visibilidade; se Bruno não a enxergar, a amostra será `REPROVADA` ou `BLOQUEADA` conforme a causa observada.

### `S06-HG-004` — repetição suprimida

**Preparação:** manter o mesmo ledger isolado e a mesma sessão de `S06-HG-003`.

**Ação conduzida:** repetir exatamente a mesma transição e aguardar uma janela completa e limitada de reconciliação.

**O que Bruno deverá observar:** nenhuma segunda notificação para o mesmo evento.

**Evidência complementar:** uma entrega total, cursor monotónico, deduplicação verdadeira e zero segundo pedido ao publisher.

**Aceite:** não aparece segunda notificação durante a janela declarada e os contadores confirmam a supressão. Ausência visual isolada, sem contador do publisher, não é prova suficiente.

### `S06-HG-005` — comando recusado sem execução

**Preparação:** não abrir shell, serviço, banco, processo alvo ou infraestrutura. O transporte continuará deliberadamente não executável.

**Ação conduzida:** apresentar a Bruno o resumo sanitizado da fixture expirada ou incompatível já exercitada na cadeia correlacionada.

**O que Bruno deverá verificar:** disposição terminal segura, dois journals sintéticos, zero `CommandAttempt`, zero executor, zero post-probe e nenhuma ação afetada.

**Aceite:** Bruno confirma que o texto não confunde recebimento/persistência com execução. Nenhum botão ou comando administrativo poderá ser apresentado como disponível.

### `S06-HG-006` — verdade visual da origem, freshness e suporte

**Preparação:** manter o Dashboard dedicado na mesma fonte de sandbox e selecionar exemplos já existentes que representem dado corrente, stale/unknown e suporte somente planejado.

**Ação conduzida:** percorrer os rótulos e estados sem trocar para provider ou fonte real.

**O que Bruno deverá observar:**

1. fonte sintética/local claramente indicada;
2. stale e unknown distintos de saudável;
3. suporte planejado distinto de suporte comprovado;
4. significado preservado sem depender apenas de cor.

**Aceite:** Bruno consegue explicar essas três diferenças em linguagem simples a partir da tela. Ausência de um dos exemplos torna somente esta amostra `BLOQUEADA`; não autoriza criar nova fixture durante a sessão.

## Ordem e orçamento da futura sessão

As amostras deverão ocorrer na ordem `S06-HG-001` a `S06-HG-006`, reutilizando somente recursos efêmeros cuja ownership permaneça comprovada. O envelope proposto é:

- no máximo uma sessão de até 60 minutos;
- um host correlacionado, um Chrome dedicado e, quando elegível, um WPF sandbox por vez;
- uma execução inicial e no máximo uma repetição integral de cada amostra a pedido de Bruno;
- nenhuma repetição seletiva destinada apenas a transformar falha em passe;
- timeout explícito por etapa e cancelamento controlado;
- encerramento imediato se surgir origem externa, recurso operacional ou efeito administrativo.

Se o tempo terminar, as amostras restantes ficam `PENDENTES`; não são inferidas a partir do relatório automático.

## Registro humano proposto

Para cada amostra, o relatório futuro deverá deixar um bloco sem preenchimento antecipado:

- ID e título;
- commit e horário local/UTC;
- superfície observada;
- passos efetivamente repetidos;
- resultado automático complementar;
- observação de Bruno, em palavras próprias;
- classificação: `APROVADA`, `APROVADA COM RESSALVA`, `REPROVADA`, `BLOQUEADA` ou `PENDENTE`;
- ressalva e evidência sanitizada;
- confirmação inequívoca de que a classificação vale somente para aquela amostra.

O executor poderá registrar as palavras de Bruno, mas não completará por conta própria a observação ou a classificação.

## Critérios de aceite da campanha de amostras

A futura campanha de amostras somente ficará pronta para apresentação ao Human Gate final quando:

1. as seis amostras forem efetivamente repetidas por Bruno na baseline congelada;
2. nenhuma amostra obrigatória permanecer `BLOQUEADA`, `PENDENTE` ou `REPROVADA`;
3. toda ressalva estiver associada à amostra e ao seu impacto;
4. origem sintética, estado stale/unknown e suporte planejado estiverem claros;
5. notificação visível e supressão da duplicata forem avaliadas separadamente;
6. o transporte de comando continuar com zero execução ou efeito;
7. todos os runtimes, listeners, stores e perfis próprios forem encerrados/removidos;
8. um relatório factual novo distinguir a evidência humana da automática;
9. nenhuma mudança técnica tiver ocorrido durante a sessão;
10. o Human Gate final continuar pendente para decisão posterior e separada.

Uma ressalva humana não poderá anular achado crítico/alto, violação de escopo, efeito operacional ou falha de cleanup.

## Classificação e condições de parada

- `APROVADA`: Bruno repetiu a amostra e confirmou o resultado esperado.
- `APROVADA COM RESSALVA`: o comportamento central foi confirmado e a limitação está identificada sem contradizer segurança ou verdade factual.
- `REPROVADA`: o comportamento observado contradiz o critério.
- `BLOQUEADA`: a amostra não pode ser produzida com os artefatos, ambiente ou autoridade disponíveis.
- `PENDENTE`: a amostra ainda não foi repetida ou decidida por Bruno.

A sessão deverá parar quando houver necessidade de editar arquivos, baixar componente, acessar origem externa, usar credencial/recurso real, iniciar comando/executor, controlar serviço, reutilizar navegador comum, ultrapassar budget ou quando o cleanup seguro não puder ser garantido.

## Cleanup obrigatório

Antes de registrar o resultado documental, o executor deverá provar:

- zero processo, listener ou janela pertencente à sessão;
- Chrome dedicado encerrado e perfil efêmero removido;
- WPF sandbox e ícone de notificação encerrados;
- Agent/API/hosts de teste encerrados;
- ledgers, SQLite, certificados e raízes temporárias próprios removidos;
- rede do navegador restaurada após qualquer simulação offline;
- zero alteração técnica rastreada e preservação de todo recurso preexistente.

Se a notificação continuar visível na Central de Notificações, Bruno deverá ser informado e a remoção será limitada à identidade/tag própria desta amostra, somente se tecnicamente comprovada. Nenhuma notificação alheia poderá ser lida ou removida.

## Fora de escopo absoluto

- implementação, correção, refactor ou alteração de código/configuração executável;
- solução, projetos, packages, lockfiles, migrations, restore, download ou acesso externo;
- provider, banco, Agent, credencial, certificado, PKI, IdP, vault ou recurso operacional;
- monitoramento real, persistência externa, canal externo ou serviço permanente;
- comando administrativo, `CommandAttempt`, executor, shell, post-probe, `Start`, `Stop` ou `Restart`;
- API/UI administrativa, LLM, recomendação, automação ou promoção `none → OBSERVER`;
- carga, endurance, HA, disaster recovery, deploy ou publicação;
- repetição do Quality Gate automático como substituto da observação humana;
- decisão do Human Gate final, promoção para `STATE-07` ou qualquer transição de estado.

## Riscos e limitações residuais

- A inspeção confirmou que o runner consolidado existente é headless; uma apresentação humana exigirá orquestração temporária dos componentes existentes, não simples repetição literal do runner.
- O sink em memória da campanha automática não prova apresentação Windows; `S06-HG-003` existe precisamente para avaliar essa fronteira.
- O Windows pode aceitar uma publicação sem mostrar a mensagem por Focus Assist, política ou estado da Central de Notificações. O relatório deverá distinguir aceitação da API, fallback e visibilidade humana.
- A não repetição visual precisa ser combinada com contador sanitizado; silêncio sozinho é ambíguo.
- O Dashboard mostra consequência e freshness, mas pode não expor diretamente toda a transição interna de conectividade do Agent. Se a distinção exigida por `S06-HG-001` não existir nos artefatos atuais, a amostra ficará bloqueada.
- Um único computador, Windows e Chrome não constituem homologação ampla, escala ou evidência operacional.
- Identidades, certificados, SQLite, observações, assignments e comandos continuam sintéticos e efêmeros.
- SignalR continua best-effort; API e reconciliação periódica continuam autoritativas.
- O transporte não executável não prova controle administrativo.
- A conclusão das amostras ainda não decide o Human Gate final e não autoriza transição.

## Entregáveis futuros

Se a execução for autorizada separadamente, ela deverá produzir somente:

- checklist de elegibilidade e ownership;
- seis registros de amostra sem decisão predefinida;
- evidência sanitizada de tempos, contagens, deduplicação e zero execução;
- inventário de cleanup;
- relatório factual exclusivamente documental;
- atualização factual de estado/histórico e commit local do relatório.

Screenshots com dados sensíveis, logs brutos, bancos, certificados, tokens, perfis, binários e temporários não poderão entrar no commit.

## Decisão futura de Bruno

Se Bruno concordar e desejar abrir somente as amostras humanas, o texto sugerido é:

> AUTORIZO exclusivamente a execução das amostras humanas finais `S06-HG-001` a `S06-HG-006` do STATE-06 no commit corrente informado no hand-off, cuja ancestralidade deverá conter a baseline automática `84217c6` e o relatório aceito `2c1e05f`, limitada a shutdown preflight, inspeção read-only de elegibilidade, cadeia sintética local já implementada, perda/recuperação controlada do transporte loopback, Dashboard TV visível, leitura imediata, hint SignalR, reconciliação de 30 segundos, uma notificação Windows local sintética e sua duplicata deliberadamente suprimida, revisão sanitizada de comando não executável, verdade visual de origem/freshness/suporte e registro factual das decisões de cada amostra. Autorizo runtimes temporários exclusivamente locais, um Chrome dedicado visível com perfil efêmero e o WPF somente na composição sandbox existente com opt-in explícito; todos deverão ser encerrados e removidos ao final. A sessão não poderá alterar ou corrigir código, configuração executável, solução, projetos, packages, lockfiles ou migrations; qualquer amostra que exija mudança deverá ser registrada como BLOQUEADA e voltar para autorização separada. Permanecem proibidos acesso externo, downloads, recursos ou credenciais operacionais, provider/banco real, navegador comum do utilizador, comando administrativo, CommandAttempt, executor, shell, serviço/infraestrutura afetados, canal externo, LLM, deploy, Human Gate final, promoção e transição de estado.

Esse texto ainda não foi emitido como autorização. Ler, aceitar ou citar esta proposta não inicia runtime, browser, WPF ou amostra humana.

## Próximo passo para Bruno

1. Abra este documento e leia principalmente `Resultado em linguagem simples`, `Amostras propostas`, `Critérios de aceite da campanha de amostras`, `Fora de escopo absoluto` e `Riscos e limitações residuais`.
2. Verifique se aceita que uma amostra incapaz de ser apresentada pelos artefatos atuais fique `BLOQUEADA`, sem correção durante a sessão.
3. Se discordar, responda somente com os pontos que deseja alterar.
4. Se concordar e desejar executar as seis amostras, envie exatamente o texto da seção `Decisão futura de Bruno`.
5. Não autorize junto dessa decisão o Human Gate final, promoção ou transição. Essas decisões somente poderão ser apresentadas depois das seis amostras e do cleanup.

Nenhuma ação técnica é necessária ou autorizada enquanto esta proposta estiver somente em revisão.
