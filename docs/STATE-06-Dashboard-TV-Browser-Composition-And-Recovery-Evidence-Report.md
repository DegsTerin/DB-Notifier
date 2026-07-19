# Relatório STATE-06 — Composição do Dashboard TV no Navegador e Evidência de Recuperação

## Resultado em linguagem simples

O incremento foi concluído dentro do laboratório local autorizado. Um Chrome separado, sem reutilizar o perfil comum do utilizador, abriu o Dashboard e a API fictícia sob a mesma origem HTTPS no próprio computador. A leitura imediata, a espera real de 30 segundos, o cache `ETag`/`304`, as falhas, as recuperações, o cancelamento e a rejeição de respostas atrasadas foram observados no navegador.

Isso não liga o DB-Notifier a banco de dados, Agent, provider, internet ou infraestrutura operacional. O resultado prova somente a composição local com dados determinísticos de teste. Não promove o produto para `OBSERVER`, não altera `STATE-06` e não autoriza um próximo incremento.

## Autoridade e escopo

- Data local: 2026-07-18.
- Estado mantido: `STATE-06 INTEGRATION`.
- Autoridade: autorização explícita de Bruno para `Dashboard TV Browser Composition and Recovery Evidence Sandbox`.
- Permitido: host HTTPS loopback temporário, identidade exclusivamente de teste, Dashboard e API sandbox no mesmo navegador dedicado, E2E, falhas determinísticas, evidência sanitizada e limpeza.
- Proibido e não executado: acesso externo, download ou nova dependência, SignalR, notificações, Agent/provider operacional, monitoramento, persistência externa, IdP/PKI/vault reais, comandos, LLM, executor, deploy, promoção e transição de estado.

## Implementação concluída

### Host temporário

O projeto de teste `DBNotifier.DashboardTv.BrowserSandboxHost`:

- exige os argumentos exatos `--activation local-test --dashboard-root <dist>` e recusa opções adicionais;
- valida o diretório Vite construído antes de abrir listener;
- publica somente em uma porta dinâmica de `127.0.0.1` com HTTPS;
- gera para cada execução um certificado P-256 curto e exclusivamente de teste;
- compõe os ficheiros estáticos do Dashboard com o endpoint sandbox read-only já existente;
- mantém em memória apenas contagens, ordem, horários, status, presença de condição ETag, cancelamento e concorrência dos cenários fixos;
- não regista provider, Agent, monitoramento, banco, persistência, comando ou serviço operacional.

### Navegador e automação

O runner PowerShell:

- usa somente Chrome ou Edge já instalado e nunca baixa um navegador;
- constrói o Dashboard com a flag exata `local-test` e o host com `--no-restore`;
- inicia um processo headless dedicado com perfil GUID dentro da pasta temporária;
- bloqueia resolução de hosts não-loopback e desabilita funções Chromium conhecidas por produzir tráfego de fundo;
- confia somente no SPKI do certificado gerado para aquela execução;
- preserva logs apenas durante a execução, falha imediatamente se host ou browser terminar antes da prontidão e encerra a árvore de processos no `finally`;
- verifica que nenhum processo pertencente ao host/perfil permaneceu antes de remover a pasta temporária.

O audit Node usa apenas APIs nativas e Chrome DevTools Protocol. Não foi acrescentado pacote npm, NuGet ou ferramenta externa. Ele aguarda uma navegação identificada antes de interagir com a nova página, observa as requisições HTTP/HTTPS do browser, usa temporizadores acelerados somente depois de uma prova real da cadência e emite apenas o resumo sanitizado.

### Correções diretas encontradas durante a revisão

1. **Alta — Fetch nativo perdia o receptor global.** O adapter capturava `window.fetch`, mas o invocava como membro da classe. No navegador real isso falhava antes de criar tráfego, embora os mocks unitários passassem. A chamada agora preserva explicitamente `globalThis`, e um teste de regressão confirma o receptor.
2. **Alta — uma resposta indefinidamente suspensa bloquearia toda reconciliação futura.** O reader ganhou deadline explícito de 10 segundos, combinando cancelamento da sessão com cancelamento do pedido e classificando timeout como `offline`. O valor é validado na construção e coberto por teste.
3. **Média — a automação podia interagir com a página anterior durante navegação.** Cada cenário agora usa um marcador único de navegação e somente clica após comprovar que o novo documento terminou de carregar.
4. **Média — a primeira escala acelerada tornava o cenário lento impossível.** O deadline artificial de 300 ms era menor que a resposta intencional de 350 ms. A escala do harness passou a 700 ms; os valores de produto continuam 10 segundos e 30 segundos.
5. **Média — o primeiro arranque do Chrome não falhava rapidamente.** O argumento de bloqueio de resolução foi preservado como uma opção única, stdout/stderr passaram a logs temporários e o runner agora detecta encerramento antecipado do processo.
6. **Baixa — a inclusão inicial na solução trouxe ruído x86/x64 e pastas de solução.** Esse ruído foi removido antes do registro; o diff final contém somente o projeto novo e quatro configurações `Any CPU`.

Todos esses achados foram corrigidos e repetidos. Não restou achado alto ou médio conhecido no diff final.

## Evidência E2E observada

A execução final usou `Chrome/150.0.7871.125` em HTTPS loopback e terminou com exit code `0`:

| Cenário | Requisições | Concorrência máxima | Resultado observado |
|---|---:|---:|---|
| Cadência autoritativa | 2 | 1 | leitura imediata `200`; segunda leitura condicional `304`; intervalo mínimo `30.020 ms` |
| Denied preservado | 2 | 1 | `403` factual sem descartar o snapshot válido |
| Contrato incompatível preservado | 2 | 1 | `426` factual sem substituir evidência |
| Corpo malformado preservado | 2 | 1 | `200` rejeitado pela validação fechada |
| Corpo acima do limite preservado | 2 | 1 | `200` rejeitado antes de aceitação |
| Erro temporário preservado | 2 | 1 | `503` factual com último snapshot mantido |
| Erro e recuperação | 3 | 1 | nova leitura autoritativa restaurou `ready` |
| Timeout e recuperação | 3 | 1 | pedido suspenso cancelado; recuperação serial posterior |
| Offline e recuperação | 2 | 1 | interrupção loopback preservou o snapshot e releitura recuperou |
| Cancelamento e fencing | 1 | 1 | resposta tardia cancelada não sobrescreveu a sessão substituta |
| Pausa e retomada | 2 | 1 | retomada permaneceu serial |

Durante a janela auditada, o CDP observou `677` requisições HTTP/HTTPS do navegador e `0` requisições para origem não-loopback. O número inclui navegações, assets, snapshots e polling do endpoint de evidência local; não é uma métrica de carga do produto.

Antes e depois da matriz, o modo normal exibiu quatro itens demonstrativos e não consultou a API sandbox. A matriz exibiu somente os dois itens fictícios `Finance sandbox` e `Orders sandbox` quando havia snapshot aceito.

## Verificações automáticas

- solução .NET 10: `16` projetos, build Release, `0` erros e `0` avisos;
- testes .NET: `304/304` unitários, `17/17` de arquitetura e `8/8` de integração;
- cobertura .NET: `78,37%` linhas e `52,34%` branches, acima dos pisos `70%`/`45%`;
- Dashboard: toolchain exata, assets de marca/tokens/localização/provider icons, TypeScript, `57/57` testes e builds normal/sandbox aprovados;
- legado Pester: `23` testes, um skip condicional previsto e `32,08%` de cobertura de comandos;
- fixture NuGet offline: inventário completo dos `16` projetos aprovado;
- formatação .NET e `git diff --check`: aprovados nesta revisão;
- documentação: `246` fontes comment-capable; `347` links Markdown locais em `84` arquivos; secret scan do worktree não ignorado e histórico disponível aprovados;
- E2E browser final: aprovado e com cleanup de processo/perfil verificado.

O gate NuGet usou o relatório sintético positivo versionado para validar completude e comportamento fail-closed do verificador. Por proibição de acesso externo, ele não é uma consulta atual aos advisories do registry. O mesmo limite vale para npm: nenhum `npm audit` remoto foi executado.

## Observado, inferido e não testado

### Observado

- estado do DOM apresentado e comportamento HTTP da matriz no Chrome dedicado;
- cadência real de aproximadamente 30 segundos e concorrência máxima igual a um;
- zero origem HTTP externa durante a janela observada;
- cancelamento recebido pelo host nos cenários próprios;
- zero processo e zero pasta efêmera pertencente ao harness após a execução final;
- gates e contagens descritos acima.

### Inferido por inspeção direta

- ausência de integração operacional, porque o host usa a fixture imutável existente e não regista os componentes de provider, Agent, persistência ou comando;
- material criptográfico exclusivamente de teste, porque é criado em processo para uma execução curta e descartado com o host;
- nenhuma nova dependência, pela comparação dos projetos/manifests/lockfiles e execução sem restore de rede.

### Não testado

- Edge, Firefox, Safari, outros sistemas operativos ou versões de browser;
- navegador headed, Fullscreen humano, acessibilidade assistiva ou qualidade visual deste incremento;
- proxy corporativo, CORS de deploy, TLS/PKI/IdP reais, rotação/revogação de identidade operacional;
- suspensão prolongada, burn-in, múltiplos clientes, carga, fairness ou backpressure de frota;
- SignalR, notificação, fonte viva, Agent/provider, monitoramento ou banco;
- consulta atual de advisories NuGet/npm, CI remota, deploy, produção ou recuperação operacional.

## Limitações e condições residuais

- A execução única em Chrome não constitui homologação de browser.
- Observar zero origem externa prova somente a janela e o processo auditados; não é uma garantia de isolamento de rede para software futuro.
- A identidade e o certificado sintéticos não modelam PKI, provisionamento, rotação ou revogação reais.
- O perfil temporário e a fixture em memória não provam persistência, reinício ou continuidade operacional.
- A cadência permanece polling. Sem SignalR, uma alteração só seria percebida na próxima releitura; SignalR continua fora do escopo e exigiria incremento próprio.
- A fonte é fictícia. Nenhum resultado prova saúde, autenticação ou suporte de database/provider.

## Classificação dos gates

- Quality Gate automático deste incremento restrito: **APROVADO**.
- Human Gate deste incremento: **PENDENTE**.
- Saída de `STATE-06`, promoção `none → OBSERVER`, produção e release: **NÃO AVALIADOS E NÃO AUTORIZADOS**.

Uma eventual aceitação humana deste relatório encerra somente este incremento e suas limitações. Ela não concede autoridade para SignalR, runtime operacional, integração externa, novo desenvolvimento, promoção ou transição de estado.
