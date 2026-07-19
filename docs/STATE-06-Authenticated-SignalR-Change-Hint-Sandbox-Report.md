# Relatório STATE-06 — Authenticated SignalR Change Hint Sandbox

## Resultado em linguagem simples

O Incremento 2 foi concluído dentro do laboratório local autorizado. Quando uma projeção fictícia muda, o servidor toca uma “campainha” SignalR autenticada. Essa campainha não transporta inventário, status, timestamps ou decisão visual: ela leva somente a versão do contrato e uma revisão SHA-256 opaca. O Dashboard então consulta novamente a API HTTPS, que continua sendo a única fonte do snapshot apresentado.

No Chrome dedicado, a mudança sintética produziu uma leitura API antecipada `200`, condicional e serial. A próxima leitura periódica ocorreu `30.016 ms` depois da conclusão da leitura inicial, retornou `304` e não foi adiada pelo hint. A concorrência máxima de snapshot e de conexão autenticada permaneceu `1`. Uma interrupção loopback preservou o último snapshot, o polling recuperou a leitura e uma reconexão SignalR foi observada. Todos os processos e o perfil temporário foram encerrados ao final.

Isso não ativa monitoramento real nem transforma SignalR em fonte de verdade. A composição normal da API não registra o hub ou publisher, e o cliente é carregado dinamicamente somente depois dos guardas exatos do Dashboard TV sandbox. Não houve notificação, Agent/provider operacional, banco externo, IdP/PKI/vault real, comando, LLM, executor, deploy, promoção ou transição de estado.

## Autoridade e escopo

- Data local de conclusão: 2026-07-19.
- Estado mantido: `STATE-06 INTEGRATION`.
- Commit local da implementação: `c945c1b`.
- Autoridade: autorização explícita de Bruno para `Authenticated SignalR Change Hint Sandbox`, inclusive acesso temporário exclusivamente ao registry npm oficial para o cliente oficial e suas dependências transitivas.
- Permitido e executado: contrato mínimo versionado; hub read-only somente no sandbox HTTPS loopback; autenticação exclusivamente de teste e mesma origem; cliente oficial somente na composição Dashboard sandbox; API/polling autoritativos; concorrência `1`; coalescência, budget, cancelamento, fencing e reconnect limitados; testes determinísticos e Chrome E2E com perfil efêmero; aquisição e auditoria npm delimitadas.
- Proibido e não executado: runtime operacional, origem externa durante execução, CDN, cliente artesanal, SignalR ativo na composição normal, notificações, Agent/provider operacional, monitoramento real, banco externo, identidade operacional, comandos, LLM, executor, deploy, promoção e transição.

## Implementação concluída

### Contrato e servidor sandbox

- `dashboard-tv-change-hint.v1` contém somente `schemaVersion` e `projectionRevision` no formato exato `sha256-` mais 64 dígitos hexadecimais minúsculos.
- O registro exige simultaneamente ambiente `DashboardTvSandbox`, snapshot sandbox habilitado e guarda independente `DashboardTvSignalRSandbox:Enabled=true`; caso contrário, não registra SignalR, autenticação, sessão ou publisher.
- A composição normal `DBNotifier.Server.Api/Program.cs` não chama o registro nem mapeia o hub. Dois testes de arquitetura protegem essa separação e o carregamento dinâmico do cliente.
- O hub não declara método invocável pelo cliente. `MaximumParallelInvocationsPerClient=1`, mensagem recebida máxima de `1.024` bytes, handshake de `5` segundos, timeout do cliente de `30` segundos e keepalive de `10` segundos são fixos.
- A sessão troca a identidade read-only de teste já autenticada por cookie `__Host-` `Secure`, `HttpOnly`, `SameSite=Strict`, com vida de cinco minutos. O servidor aceita somente HTTPS, IP loopback literal, origem exata e cookie único limitado.
- Cada token usa 32 bytes de `RandomNumberGenerator`; somente o digest SHA-256, o sujeito limitado e a expiração ficam em memória. Nova emissão substitui a anterior do mesmo sujeito, a store admite no máximo quatro sessões, usa comparação em tempo constante e zera buffers ao remover ou revogar.
- A autenticação e a evidência retêm somente contagens agregadas. Não há token, cookie, sujeito, connection ID ou payload nos relatórios do harness.

### Dashboard e reconciliação autoritativa

- O cliente oficial é importado dinamicamente somente depois de o modo TV estar ativo e de o navegador passar a guarda exata `local-test` + HTTPS loopback.
- O bootstrap usa a mesma origem e a identidade de teste existente; o hub usa apenas o cookie não legível por JavaScript. Nenhum token é colocado em URL, storage ou log.
- Hints incompatíveis fecham somente o canal opcional. Falha de conexão ou reconexão não altera a tela: o polling HTTPS continua.
- Reconnect possui no máximo quatro tentativas, atrasos `0/1.000/2.000/5.000 ms` e limite total de `15.000 ms`.
- O coordenador continua dono de todas as leituras. Ele admite no máximo dois reads derivados de hint por janela periódica, coalesce hints durante trabalho ativo, prioriza o deadline periódico pendente e mantém uma única requisição em voo.
- A leitura inicial cria o deadline independente de 30 segundos. Reads de hint e retry manual não cancelam nem reprogramam esse deadline. Saída do modo TV cancela snapshot, conexão, reconnect e callbacks da geração anterior.

### Harness e fonte sintética

O host temporário existente recebeu somente a composição SignalR sandbox, uma fonte provider-neutral em memória e rotas fixas de controle/evidência E2E. O controle primeiro atualiza o snapshot sintético, calcula a mesma revisão canônica do corpo e somente depois publica o hint. Ele não aceita provider, endpoint, payload ou estado arbitrário. O runner continua usando certificado P-256 curto, Chrome instalado, HTTPS `127.0.0.1`, perfil GUID e limpeza no bloco `finally`.

## Sequência E2E observada

| Etapa | Evidência observada |
|---|---|
| Dashboard normal | quatro itens demonstrativos; zero consulta ao snapshot antes de entrar em TV |
| Entrada no TV sandbox | leitura imediata `200`; dois itens fictícios; conexão SignalR autenticada ativa |
| Commit sintético e hint | revisão opaca válida publicada depois da mudança da fonte |
| Read antecipado | segunda consulta HTTPS com `If-None-Match`, resposta `200` e novo nome sintético apresentado somente depois da API |
| Deadline periódico | terceira consulta condicional `304`, iniciada `30.016 ms` após a primeira conclusão; hint não deslocou o deadline |
| Serialização | concorrência máxima de snapshot `1`; conexão autenticada máxima `1` |
| Offline/reconexão | último snapshot preservado, polling recuperado e exatamente uma nova conexão SignalR observada |
| Fencing/cancelamento | resposta tardia da sessão anterior não substituiu a sessão nova; saída do TV reduziu conexões ativas a zero |
| Cleanup | host, Chrome dedicado e perfil temporário encerrados; zero processo pertencente ao harness permaneceu |

A matriz completa anterior de `ETag/304`, denied, incompatível, corpo malformado/acima do limite, `503`, timeout, offline, fencing e pausa/retomada também passou com concorrência `1`. O CDP observou `1.226` requisições HTTP/HTTPS locais, `26` WebSockets locais ao longo das várias sessões TV, e `0` origem externa. Essas contagens incluem navegação, assets, polling de evidência e sessões repetidas do teste; não são métricas de carga do produto.

## Dependência, licença, integridade e advisories

- Cliente selecionado: `@microsoft/signalr@10.0.0`, versão exata no manifesto e lockfile.
- Aquisição: somente `https://registry.npmjs.org/`, com scripts de instalação desabilitados.
- Lockfile: 18 entradas novas, todas com versão, URL do registry oficial e integridade criptográfica.
- Licenças observadas nos manifests instalados: MIT para 15 pacotes, BSD-2-Clause para `webidl-conversions`, BSD-3-Clause para `tough-cookie` e Unlicense para `fetch-cookie`; nenhuma licença incompatível foi identificada nesta revisão local.
- Integridade publicada do cliente: SHA-512 registrada no lockfile e metadata do registry.
- `npm audit --json` no registry oficial: `0` vulnerabilidades conhecidas em todas as severidades no momento da consulta.
- Nenhum outro pacote, fonte, CDN ou download foi usado. A consulta npm autorizada é evidência pontual, não garantia futura de supply chain.

## Achados da revisão direta e correções

1. **Média — cancelamento poderia deixar o listener de abort removido após conexão bem-sucedida.** O listener agora permanece vinculado à vida da sessão TV, e geração mais cancelamento rejeitam callbacks tardios.
2. **Média — cleanup concorrente poderia tentar revogar a mesma sessão duas vezes.** A propriedade local de sessão passou a ser consumida uma única vez; revogação de servidor continua idempotente e sem revelar existência.
3. **Média — o primeiro E2E provava hint e polling, mas não exigia reconexão SignalR após offline.** A matriz final passou a comparar contagens agregadas antes/depois da interrupção e observou uma reconexão com máximo ativo `1`.
4. **Baixa — importações do host ficaram fora da ordenação canônica.** A ordem foi corrigida e `dotnet format --verify-no-changes` passou.
5. **Ferramenta legada — Pester 3.4 sob PowerShell 7.6 interpretou incorretamente três asserções `Should Throw`.** Os mesmos caminhos falharam fechado em sondas diretas. O gate canônico foi repetido no Windows PowerShell compatível com Pester 3.4 e registrou `23` testes aprovados, com um skip condicional previsto. Nenhuma fixture ou verificador legado foi alterado.

Não restou achado crítico, alto ou médio conhecido no diff final.

## Verificações automáticas

- shutdown preflight: zero componente e listener DB-Notifier antes do trabalho;
- solução .NET 10: 16 projetos, build Release `--no-restore`, zero erro e zero aviso;
- testes .NET: `321/321` unitários, `19/19` de arquitetura e `11/11` de integração;
- cobertura .NET: `77,97%` linhas e `51,62%` branches, acima dos pisos `70%`/`45%`;
- Dashboard: Node `24.18.0`, npm `11.16.0`, TypeScript, `60/60` testes, assets/tokens/localisation/provider icons e Vite aprovados;
- browser E2E final: Chrome `150.0.7871.125`, todos os cenários aprovados e cleanup verificado;
- Pester compatível: `23` testes aprovados, um skip condicional previsto, cobertura `32,08%` (`290/904` comandos);
- `dotnet format --verify-no-changes`, documentação de código, fixture NuGet offline para 16 projetos, secret scan, smoke fail-closed e `git diff --check`: aprovados;
- npm oficial: licença/integridade revisadas e audit atual sem advisory conhecido;
- cleanup final: zero processo e zero perfil/pasta temporária do browser E2E.

## Observado, inferido e não testado

### Observado

- sessão SignalR autenticada, publicação de hint, read HTTPS antecipado e atualização somente pela resposta API;
- deadline real de 30 segundos independente do hint, `ETag/304` e concorrência máxima `1`;
- interrupção, recuperação do polling e uma reconexão SignalR;
- recusa pós-revogação da sessão no E2E HTTPS de integração;
- contrato, budget, coalescência, expiry e reconnect limits nos testes determinísticos;
- origem exclusivamente loopback durante o browser E2E e cleanup integral.

### Inferido por inspeção direta

- isolamento da composição normal, protegido também por testes de arquitetura que leem os pontos de composição;
- ausência de conteúdo operacional no hint, pela forma exata do contrato e validação em ambos os lados;
- ausência de segredo persistido, pela store somente de digest e inexistência de alteração em persistência/configuração.

### Não testado

- identidade, cookie, PKI, IdP, rotação, revogação ou distribuição operacionais;
- proxy, balanceador, sticky sessions, backplane, múltiplas instâncias ou múltiplos browsers concorrentes;
- fallback Long Polling forçado; o E2E final observou WebSocket;
- carga, endurance, DDoS, fairness ou backpressure de frota;
- Edge, Firefox, Safari, browser headed ou amostra humana visual/acessível;
- Agent/provider/banco/monitoramento real, notificações, comandos, LLM, executor, deploy ou produção;
- advisories NuGet atuais; o gate NuGet permaneceu na fixture offline existente.

## Limitações e condições residuais

- SignalR é best-effort: não garante entrega, ordem, exactly-once ou tempo real. Mudanças perdidas continuam dependentes do polling.
- O cliente oficial existe como chunk lazy no artefacto Web, mas não é solicitado nem ativado pela composição normal; isso foi inspecionado e protegido por teste de arquitetura.
- A revisão opaca não atualiza nem ordena estado no cliente. Duplicidade e reorder podem causar releitura adicional dentro do budget, mas nunca aplicar payload ou regredir o snapshot diretamente.
- Dois reads por janela protegem o browser local contra uma sequência curta de hints; não constituem proteção operacional contra abuso distribuído.
- Cookie e sessão em memória modelam somente autenticação local de teste. Cinco minutos de expiração foram provados por teste determinístico da store, não por espera real no browser.
- O encerramento ativo e a revogação foram exercitados, mas uma navegação abrupta pode impedir o `DELETE` best-effort; o token então expira no processo local em até cinco minutos e desaparece com o host.
- Um Chrome e um processo Kestrel loopback não comprovam compatibilidade de deploy ou escala.
- A auditoria npm é pontual e não elimina risco futuro de pacote comprometido ou advisory novo.

## Classificação dos gates

- Quality Gate automático deste incremento restrito: **APROVADO** no escopo local documentado.
- Human Gate próprio do incremento: **ACEITO COM AS LIMITAÇÕES REGISTRADAS** por Bruno em 2026-07-19.
- Quality/Human Gate final do `STATE-06`, Incremento 3, runtime operacional, promoção `none → OBSERVER`, `STATE-07`, produção e release: **NÃO AVALIADOS E NÃO AUTORIZADOS**.

## Decisão humana registrada

Depois de solicitar a leitura direta do relatório, especialmente de `Resultado em linguagem simples`, `Sequência E2E observada`, `Limitações e condições residuais` e `Classificação dos gates`, Bruno declarou em 2026-07-19:

> Incremento STATE-06 Authenticated SignalR Change Hint Sandbox, commit c945c1b: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo novo incremento, runtime operacional, promoção nem transição de estado.

Essa decisão encerra somente o Human Gate próprio do Incremento 2 e aceita as limitações documentadas. O `STATE-06 INTEGRATION` permanece inalterado; Incremento 3, runtime operacional, promoção e transição continuam sem autorização.
