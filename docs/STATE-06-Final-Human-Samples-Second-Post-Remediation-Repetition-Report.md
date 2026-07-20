# Relatório STATE-06 — Repetição final pós-segunda-remediação das amostras humanas

## Status e autoridade

- Data: 2026-07-20.
- Baseline executada: commit `129b9fd4f7fc774b8ac9616115524660d569ffc9`.
- Ancestralidade exigida: remediação `9d65426` e registro factual `129b9fd`, confirmados antes da execução.
- Estado mantido: `STATE-06 INTEGRATION`.
- `S06-HG-001`: `APROVADA` explicitamente por Bruno.
- `S06-HG-006`: `APROVADA` explicitamente por Bruno.
- `S06-HG-002` a `S06-HG-005`: não foram repetidas e preservam suas aprovações anteriores.
- Campanha das seis amostras humanas: `CONCLUÍDA COM AS LIMITAÇÕES REGISTRADAS`; todas possuem decisão individual `APROVADA`.
- Human Gate final, runtime operacional, promoção e transição: não autorizados e não executados.

Bruno autorizou exclusivamente a repetição das duas amostras antes bloqueadas, uma por sessão, com o runner visível versionado, runtimes locais temporários e Chrome dedicado com perfil efêmero. A autorização proibiu alterações técnicas, acesso externo, recursos operacionais, repetição das quatro amostras já aprovadas, notificação Windows, comandos, executor, Human Gate final, promoção e transição.

## Resultado em linguagem simples

As duas verificações que faltavam foram apresentadas com sucesso e aprovadas por Bruno.

Na primeira, Bruno viu o Agent começar disponível, ficar indisponível enquanto uma observação permanecia guardada e depois recuperar. O Browser continuou ligado à API durante toda a sequência. Depois da recuperação, a pendência voltou a zero, o Server passou de uma para duas amostras e a tela confirmou que o replay foi aceito exatamente uma vez.

Na segunda, Bruno viu simultaneamente duas instâncias no Dashboard TV sandbox: uma `Desconhecido` ainda corrente e outra `Desatualizado`. A página também mostrou que a origem era sintética e local, que não havia dados externos e que o suporte era apenas planejado, não implementado nem homologado.

As decisões foram dadas na conversa enquanto as evidências válidas estavam visíveis. Os runners não registraram essas decisões automaticamente, conforme o desenho de separação humana. As janelas fecharam depois pelos limites bounded das sessões porque o controle final de encerramento não foi acionado a tempo; o cleanup removeu integralmente os recursos pertencentes às duas sessões.

## Sequência humana observada

### Elegibilidade e isolamento

Antes da primeira sessão foram confirmados:

- branch `main` no commit `129b9fd4f7fc774b8ac9616115524660d569ffc9`;
- ancestralidade de `9d65426` e `129b9fd`;
- worktree limpa;
- zero processo, listener ou root temporário STATE-06 pertencente ao DB-Notifier;
- host e Dashboard já compilados localmente, sem build, restore ou download nesta atividade.

As duas janelas exibiram endereço HTTPS loopback `127.0.0.1`, aviso explícito de harness test-only, origem sintética local e ausência de estado operacional.

### `S06-HG-001` — perda e recuperação do Agent

Uma primeira apresentação foi encerrada sem decisão humana e sem ser convertida em passe. Depois do cleanup integral, uma nova sessão separada apresentou:

1. **Disponível:** Browser → API disponível, Agent → API `available`, zero pendência e uma amostra aceita pelo Server.
2. **Perda:** Browser → API permaneceu disponível, Agent → API mudou para `unavailable`, exatamente uma observação ficou pendente e o Server permaneceu com uma amostra.
3. **Recuperação:** Browser → API permaneceu disponível, Agent → API mudou para `recovered`, pendências voltaram a zero, o Server passou para duas amostras e `Replay único` mostrou `Aceite exatamente uma vez`.

O Dashboard preservou o snapshot durante a perda. Depois, apresentou o conteúdo da observação recuperada e, com o passar do tempo, classificou corretamente o snapshot como `Desatualizado`; essa freshness visual não foi confundida com o transporte do Agent.

Depois de perguntar quais eram as três etapas e receber a explicação baseada nas imagens apresentadas, Bruno declarou exatamente:

> S06-HG-001: APROVADA

A decisão ocorreu antes do timeout da sessão. O controle `Encerrar amostra e limpar laboratório` não foi acionado; o presenter atingiu posteriormente seu limite de espera por conclusão explícita, saiu com erro tipado e o `finally` do runner executou o cleanup integral.

### `S06-HG-006` — verdade visual de origem, freshness e suporte

Uma nova sessão, selecionada exclusivamente para `S06-HG-006`, apresentou:

1. estado inicial com Browser → API disponível, origem sintética local, ausência de dados externos e suporte planejado sem homologação;
2. ativação test-only da verdade visual, inicialmente antes da reconciliação do iframe;
3. reconciliação periódica posterior, com total de duas instâncias;
4. `Unknown atual - fixture sintética` classificado como `Desconhecido`;
5. `Evidência desatualizada - fixture sintética` classificada como `Desatualizado`;
6. prazo visível de `36 segundos restantes` para a expiração do item `unknown`, sem renovação silenciosa do timestamp.

Com a tela ainda válida na imagem apresentada, Bruno declarou exatamente:

> S06-HG-006: APROVADA

Depois da decisão, o prazo terminou antes de o controle final ser acionado. O presenter recusou considerar a amostra automaticamente concluída depois da expiração, saiu com erro tipado e o runner executou o cleanup integral. A expiração posterior não reescreve a observação nem a decisão que ocorreram enquanto a tela mostrava `36 segundos restantes`.

## Evidência e limites de interpretação

### Observado

- as três etapas distintas de `S06-HG-001` nas imagens apresentadas por Bruno;
- Browser → API disponível durante disponibilidade, perda e recuperação do Agent;
- uma pendência preservada, duas amostras finais e replay aceito exatamente uma vez;
- duas instâncias simultâneas, uma `Desconhecido` corrente e outra `Desatualizado`, em `S06-HG-006`;
- origem sintética local, suporte planejado e ausência declarada de dados externos;
- decisões humanas exatas `S06-HG-001: APROVADA` e `S06-HG-006: APROVADA`;
- zero processo, listener, perfil ou root temporário pertencente às sessões depois de cada cleanup;
- worktree permaneceu limpa durante toda a repetição.

### Inferido por inspeção do runner aceito

- o Chrome usado era dedicado e possuía perfil efêmero próprio;
- resolução externa estava bloqueada pelos argumentos versionados do runner;
- a composição normal não expõe a página ou os controles test-only;
- as decisões humanas permaneceram fora do presenter, que reportou `humanDecisionRecorded=false`.

### Não observado ou não testado

- resumo terminal positivo dos presenters: ambos terminaram por condição bounded depois das decisões humanas;
- contagem terminal de origens externas dessas duas sessões específicas, porque o assert final não foi alcançado; nenhuma origem externa apareceu nas evidências ou erros, e o runner permaneceu configurado para bloqueá-la;
- Agent, provider, banco, credencial, PKI, IdP ou infraestrutura operacional;
- notificação Windows, comando administrativo, `CommandAttempt`, executor ou efeito externo;
- Human Gate final, runtime operacional, promoção ou transição.

As imagens foram apresentadas na conversa e não foram copiadas para o repositório. Este relatório registra somente os valores visíveis e as decisões explícitas, sem inventar artefatos adicionais.

## Limitações e condições residuais

- As duas amostras usam dados determinísticos e sintéticos; não comprovam operação real.
- A prova pertence a uma máquina Windows e uma versão local do Chrome; não constitui homologação ampla.
- O estado `unknown` é temporal. A decisão de `S06-HG-006` foi tomada enquanto ele ainda estava corrente; a expiração posterior encerrou corretamente o runner como falha, em vez de renovar a evidência.
- Os controles finais não foram acionados antes dos limites das sessões. Isso produziu saídas terminais não zero depois das decisões, embora o cleanup integral tenha passado.
- A ausência do resumo terminal positivo limita a prova de rede dessas sessões específicas. O isolamento continua sustentado pelo HTTPS loopback visível, pela configuração fail-closed do runner e pelo Quality Gate automático aceito da remediação.
- Aprovar as seis amostras humanas não abre nem decide automaticamente o Human Gate final do `STATE-06`.

## Cleanup

Depois de cada sessão foram confirmados:

- zero processo do host, presenter ou Chrome dedicado pertencente à execução;
- zero listener pertencente à execução;
- zero root `DBNotifier-State06-HumanReview-*`;
- worktree limpa no commit `129b9fd`;
- nenhum navegador comum, IDE, banco, serviço ou processo alheio encerrado.

## Classificação dos gates

| Gate | Classificação |
|---|---|
| baseline, ancestralidade e shutdown preflight | `APROVADO` |
| `S06-HG-001` | `APROVADA` explicitamente por Bruno |
| `S06-HG-002` | permanece `APROVADA` |
| `S06-HG-003` | permanece `APROVADA` |
| `S06-HG-004` | permanece `APROVADA` |
| `S06-HG-005` | permanece `APROVADA` |
| `S06-HG-006` | `APROVADA` explicitamente por Bruno |
| campanha das seis amostras humanas | `CONCLUÍDA COM AS LIMITAÇÕES REGISTRADAS`; seis decisões individuais `APROVADA` |
| isolamento e cleanup | `APROVADO` |
| conclusão terminal dos presenters | `NÃO APROVADA`; timeout/expiração posteriores às decisões, com cleanup aprovado |
| Human Gate final do `STATE-06` | `PENDENTE` e não aberto |
| runtime operacional, promoção e transição | `NÃO AUTORIZADOS` |

## Verificação documental

- documentação de código: aprovada para `284` arquivos comment-capable;
- links Markdown: `487` links locais em `111` arquivos aprovados;
- secret scan do worktree não ignorado: aprovado;
- escopo documental e `git diff --check`: aprovados;
- build, testes de produto e novos harnesses: não repetidos durante o registro, porque nenhuma mudança executável foi realizada depois das sessões humanas.

## Próxima atividade

Bruno revisou este relatório e declarou em 2026-07-20:

> Repetição final das amostras humanas S06-HG-001 e S06-HG-006 do STATE-06 na baseline 129b9fd, relatório commit 10a8249: ACEITA COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente a elaboração de uma proposta documental para o Human Gate final do STATE-06, sem execução, runtime, browser, promoção ou transição de estado.

A aceitação confirma somente o resultado e as limitações desta repetição. A autoridade adicional foi consumida exclusivamente pela [proposta documental do Human Gate final](STATE-06-Final-Human-Gate-Proposal.md). O próprio gate permanece `PENDENTE`; promoção e transição continuam decisões posteriores e separadas.
