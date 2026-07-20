# Relatório STATE-06 — Repetição pós-remediação das amostras humanas finais

## Status e autoridade

- Data: 2026-07-20.
- Baseline: commit `cfd620f9a4cdd6d5d6613d8d1498b82be36616a9`, contendo a remediação aceita `dd420d1` e seu registro factual.
- Estado mantido: `STATE-06 INTEGRATION`.
- `S06-HG-001`: `BLOQUEADA`.
- `S06-HG-006`: `BLOQUEADA` / não apresentada validamente.
- Campanha humana: continua `BLOQUEADA`.
- Human Gate final, runtime operacional, promoção e transição: não autorizados e não executados.

Bruno autorizou somente a repetição humana de `S06-HG-001` e `S06-HG-006` com a cadeia sintética local existente, Chrome dedicado visível, perfil efêmero e cleanup integral. Mudanças de código, configuração executável, solução, projetos, packages, lockfiles e migrations permaneceram proibidas.

## Resultado em linguagem simples

A repetição não pôde ser aprovada.

Na primeira amostra, o laboratório abriu o Agent como `available`, sem pendências e com uma amostra no Server. O Dashboard TV mostrou `Sandbox Assignment` como `Degradado`. Contudo, a mesma página dizia `Browser → API indisponível`. A causa foi comprovada: a página consulta a evidência quatro vezes por segundo, atinge o limitador local com respostas `429` e troca o rótulo para indisponível sem restaurá-lo depois de uma leitura bem-sucedida.

Essa contradição invalida exatamente a prova exigida por `S06-HG-001`: não é possível pedir que Bruno confirme que Browser → API permanece disponível enquanto Agent → API fica indisponível se a superfície já declara o browser indisponível antes da perda do Agent. A sequência não foi avançada e nenhuma aprovação foi solicitada.

Para `S06-HG-006`, o host conseguiu preparar `unknown` corrente e `stale`, mas o helper transitório que abriria a apresentação visível encerrou antes do hand-off. Uma tentativa final foi recusada pelo parser antes de iniciar qualquer runtime. Como Bruno não recebeu uma apresentação válida, a amostra também permanece bloqueada; a prova automática anterior não foi usada como substituto da observação humana.

## Baseline e preflight

Antes dos runtimes foi confirmado:

- branch `main` e commit `cfd620f`;
- worktree limpa;
- `dd420d1` e `cfd620f` na ancestralidade;
- host e Dashboard já construídos localmente;
- zero processo, listener ou root temporário pertencente ao DB-Notifier;
- nenhum build, restore, download ou acesso externo necessário.

O primeiro comando de inspeção usou por engano o nome reservado `$Host`; seu resultado parcial foi descartado e o preflight válido foi repetido antes do runtime.

## Sequência observada de `S06-HG-001`

O Chrome dedicado abriu a página `DB Notifier — evidência test-only do Agent` com perfil efêmero e a mesma origem HTTPS loopback do Dashboard. A leitura direta do DOM renderizado confirmou:

```json
{"stage":"agent-transport-ready","agentTransport":"available","pending":"0","serverSamples":"1","replay":"Ainda não concluído","visualTruth":"Ainda não ativada","browserApi":"Browser → API indisponível","tvActive":true,"snapshotBadge":"Snapshot do sandbox local","names":["Sandbox Assignment"],"statuses":["Degradado"]}
```

O log sanitizado do host continha `233` respostas `429` do endpoint de evidência no momento da inspeção. O helper CDP registrou zero origem HTTP externa. A janela permaneceu responsiva e o Dashboard continuou mostrando seu snapshot, mas o rótulo factual da ligação do browser estava incorreto.

Por isso:

- o endpoint de perda do Agent não foi invocado nessa apresentação;
- nenhuma observação humana de pendência/recovery foi solicitada;
- `S06-HG-001` permaneceu `BLOQUEADA` sem aprovação inferida.

## Causa factual do bloqueio

A inspeção direta do artefacto aceito confirmou a combinação:

1. `BuildHumanEvidencePage` agenda `refresh` a cada `250 ms`;
2. o endpoint de evidência usa `HumanApiRateLimit`;
3. qualquer resposta não aprovada entra no `catch` e fixa `Browser → API indisponível`;
4. o caminho de sucesso atualiza os demais campos, mas não restaura `Browser → API disponível`.

O Quality Gate automático anterior concluiu rapidamente a sequência e, portanto, não detectou a degradação temporal da apresentação humana. Isso não invalida as provas automáticas de outbox/replay ou `unknown`/`stale`; invalida a estabilidade da superfície para `S06-HG-001`.

## Tentativa de `S06-HG-006`

Depois do cleanup integral da primeira apresentação, uma sessão separada avançou no host local as transições sintéticas de perda, recovery e `visual-truth-ready` antes de abrir o browser. O helper de apresentação encerrou antes da janela ficar elegível e a tentativa foi completamente limpa. Uma última invocação continha um erro de sintaxe PowerShell e foi recusada pelo parser antes de criar processo ou root.

Nenhuma tela `unknown`/`stale` foi entregue a Bruno nesta repetição. Portanto:

- o resultado automático `unknownAndStaleVisible=true` continua apenas como evidência automática histórica;
- `S06-HG-006` não recebeu decisão humana nova;
- a amostra permanece `BLOQUEADA`.

## Cleanup e isolamento

Depois de cada tentativa:

- helper Node, host e árvore do Chrome dedicado foram encerrados;
- perfil efêmero, SQLite Agent/Server, certificados, logs e roots da sessão foram removidos;
- zero processo, listener ou root pertencente à sessão permaneceu;
- worktree permaneceu limpa;
- nenhum navegador comum, IDE, banco, serviço ou processo alheio foi encerrado;
- nenhum arquivo de código ou configuração executável foi alterado.

## Observado, inferido e não testado

### Observado

- apresentação inicial `agent-transport-ready` e Dashboard TV `Degradado`;
- contradição textual `Agent available` versus `Browser → API indisponível`;
- `233` respostas `429` no endpoint de evidência;
- zero origem HTTP externa observada pelo helper;
- cleanup final completo;
- ausência de hand-off humano válido para ambas as decisões.

### Inferido por inspeção direta

- a cadência de `250 ms`, o rate limiter e a ausência de restauração positiva explicam a contradição;
- um runner visível versionado e testado reduziria a dependência de orquestração transitória durante a próxima repetição.

### Não testado

- perda/recovery do Agent em apresentação humana nesta sessão;
- decisão humana sobre `unknown` versus `stale`;
- qualquer recurso operacional, provider, banco real, comando ou notificação Windows;
- Human Gate final, promoção ou transição.

## Limitações e condições residuais

- A prova automática do commit `dd420d1` permanece aceita, mas não substitui as duas amostras humanas.
- A correção necessária parece restrita à superfície/runners test-only; isso é uma conclusão de inspeção, não autorização de mudança.
- Reduzir a frequência de polling sem testar rate limit, recovery positivo e duração humana poderia apenas deslocar o problema.
- Manipular o DOM ou o timer via CDP para esconder o rótulo foi deliberadamente rejeitado porque falsificaria a evidência aceita.
- O helper transitório não deve ser tratado como artefacto reprodutível; uma futura remediação deve fornecer um runner visível versionado, limitado e testado.

## Verificação documental

Depois do cleanup integral, somente o relatório e os registros factuais Markdown foram alterados. As verificações locais concluíram:

- documentação de código: `APROVADO`, 282 arquivos comment-capable;
- links Markdown: `APROVADO`, 476 links locais em 108 arquivos;
- secret scan do worktree não ignorado, sem histórico: `APROVADO`;
- `git diff --check`: `APROVADO`;
- escopo do diff: somente os cinco arquivos Markdown de relatório, índice e estado.

## Classificação dos gates

| Gate | Classificação |
|---|---|
| baseline, ancestralidade e preflight | `APROVADO` |
| isolamento e ausência de acesso externo | `APROVADO` |
| cleanup | `APROVADO` |
| `S06-HG-001` | `BLOQUEADA` por contradição factual causada por rate limit |
| `S06-HG-006` | `BLOQUEADA`; apresentação humana não concluída |
| campanha humana | `BLOQUEADA` |
| Human Gate final | `PENDENTE` e não aberto |
| runtime operacional, promoção e transição | `NÃO AUTORIZADOS` |

## Próxima atividade

Nenhuma correção está autorizada. O próximo passo deve ser uma proposta documental separada para uma remediação test-only mínima que:

1. alinhe a cadência da página ao rate limit e restaure explicitamente `Browser → API disponível` depois de uma leitura válida;
2. teste a página por uma duração representativa da observação humana;
3. forneça um runner visível versionado, com barriers persistentes para `S06-HG-001` e preparação determinística de `S06-HG-006`;
4. preserve `src/`, composição normal, dependências e todos os limites de ciclo de vida.

Somente depois de essa remediação ser implementada, testada e aceita poderá haver nova autorização para repetir as duas amostras.
