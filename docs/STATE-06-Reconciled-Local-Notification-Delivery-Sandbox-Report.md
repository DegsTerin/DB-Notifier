# Relatório STATE-06 — Reconciled Local Notification Delivery Sandbox

## Resultado em linguagem simples

O Incremento 3 foi implementado no commit `d43e49a` dentro do laboratório local autorizado. O fluxo novo não observa banco real e não recebe notificações pelo SignalR. Ele consulta, a cada 30 segundos, uma lista read-only de mudanças de estado que já foram reconciliadas e gravadas pelo servidor. A primeira consulta cria somente um ponto de partida silencioso; mudanças anteriores a esse ponto não geram notificação atrasada.

Quando aparece uma mudança sintética nova e atual, o consumidor grava primeiro a intenção num ledger local isolado, solicita uma notificação pelo caminho Windows já existente e registra o resultado. O identificador estável do evento, o hash completo do conteúdo, a Tag determinística e o cursor monotónico impedem repetição normal depois de reinício. A fila de fallback continua serial e limitada a `16`. O aceite pela API do Windows ou pelo fallback significa apenas que o pedido local foi aceito; não prova que o Windows mostrou a mensagem ao utilizador.

O E2E iniciou uma API HTTPS real em `127.0.0.1`, com certificado e identidade exclusivamente de teste, e um Server SQLite em memória. Ele estabeleceu a baseline sem entrega, commitou uma transição sintética `Healthy → Unavailable`, leu essa transição pelo endpoint autenticado, entregou-a uma vez a um sink de teste, reabriu o mesmo ledger e confirmou que não houve repetição. Uma requisição sem identidade recebeu `401`. O E2E não iniciou o WPF nem apresentou uma notificação visível; integração visual e comportamento do Windows foram verificados por compilação, testes de arquitetura e inspeção direta do adapter.

Nenhum runtime operacional foi ativado. O `Program.cs` normal da API não registra nem mapeia o endpoint novo, e o WPF só cria o consumidor quando recebe simultaneamente a flag exata do sandbox, opt-in explícito, origem HTTPS por IP loopback, diretório dedicado sob `%TEMP%`, sujeito de teste limitado e impressão SHA-256 do certificado efêmero. SignalR, canais externos, provider/monitoramento real, banco externo, comandos, LLM, executor, deploy, promoção e transição de estado permaneceram fora do escopo.

## Autoridade e escopo

- Data local de conclusão: 2026-07-19.
- Estado mantido: `STATE-06 INTEGRATION`.
- Commit local da implementação: `d43e49a`.
- Autoridade: autorização explícita de Bruno para `Reconciled Local Notification Delivery Sandbox`.
- Permitido e executado: contrato/projeção read-only versionados; endpoint HTTPS loopback com autenticação de teste; consumidor WPF somente sob composição sandbox e opt-in; baseline silenciosa; ledger local isolado; cursor, deduplicação e fencing; fila/budget/cancelamento/silêncio; localização `pt-BR`/`en-GB`; caminho Windows existente e fallback; testes determinísticos e E2E locais; runtimes temporários encerrados.
- Proibido e não executado: acesso externo, pacote/download novo, runtime operacional, canal externo, SignalR como fonte de notificação, Agent/provider/monitoramento ou banco operacional, persistência externa, identidade real, comando, UI administrativa, LLM, executor, deploy, promoção e transição.

## Implementação concluída

### Contrato, evento e projeção read-only

- O wire contract é `reconciled-local-notification-transition.v1`; o ledger é `reconciled-local-notification-ledger.v1`.
- Cada transição contém `EventId`, instância/label sintético, evento/severidade, par anterior/atual, tempos UTC, freshness e marcador exato `synthetic-sandbox`.
- A derivação canónica agora conserva o par anterior/atual e o grava em `canonical-event-details.v1`. A mensagem outbox histórica `canonical.event.v1` não foi alterada.
- A projeção une evento, observação e instância commitados. Ela exige Agent/instância coincidentes, evidência `Synthetic`, status e timestamps exatamente iguais aos da observação de origem e detalhes versionados compatíveis. Divergência sintética falha fechado.
- Eventos iniciais `Connected` sem estado anterior avançam o cursor, mas permanecem silenciosos. Eventos não sintéticos não são expostos por esse sandbox.
- O cursor opaco contém no máximo `32` heads Agent/sequence, em representação binária canónica base64url. Um cursor seguinte não pode remover Agent nem reduzir sequência.

### Endpoint e transporte local

- Rota fixa: `/api/v1/dashboard/reconciled-notification-transitions`.
- O registro exige ambiente `DashboardTvSandbox`, snapshot sandbox habilitado e guarda independente `ReconciledLocalNotificationSandbox:Enabled=true`.
- O endpoint reutiliza a identidade/policy humana read-only exclusivamente de teste e o rate limiter local. A API normal não o registra ou mapeia.
- Baseline aceita somente `?baseline=true`; continuação aceita somente um cursor canónico. Query, projeção ou contrato incompatível recebem erro sanitizado e não liberam dados parciais.
- O cliente exige HTTPS por IP loopback, identidade de teste, schema header exato, `application/json; charset=utf-8`, corpo máximo de `65.536` bytes, JSON sem campos desconhecidos e cancelamento propagado.

### Ledger, budget e recuperação

- O ledger fica em diretório sandbox dedicado sob `%TEMP%`; nenhuma configuração ou credencial operacional é persistida.
- Um lock aberto com `FileShare.None` impede dois consumidores simultâneos do mesmo ledger. Revisão otimista e substituição por arquivo temporário protegem cada atualização local completa.
- O ledger conserva cursor, revisão e até `256` decisões recentes. Cada página e fila têm no máximo `16` transições; o transporte, texto, número de Agents, materialização, tentativas e tempos possuem limites explícitos.
- Antes do hand-off não transacional ao Windows, o estado `Attempting` é persistido. Se houver encerramento nessa janela, a retomada classifica o resultado como terminal/incerto e não repete automaticamente.
- Entrega retryable admite no máximo duas tentativas. Silêncio ou freshness diferente de `current` produz supressão durável, sem replay posterior.
- O primeiro ciclo sem cursor grava somente a baseline. Ciclos concorrentes são recusados e o cursor avança apenas depois de todos os itens da página receberem decisão durável.

### WPF e publicação Windows

- A composição normal continua inerte. A ativação exige `--reconciled-notification-sandbox`, `--notifications-opt-in` e todos os parâmetros locais válidos; qualquer ausência ou formato inválido mantém o consumidor desligado.
- O certificado curto é fixado pela impressão SHA-256 informada pelo harness; nenhuma validação permissiva de TLS foi introduzida.
- O ciclo é imediato na entrada e depois periódico a cada 30 segundos, sem sobreposição. Dispose cancela o trabalho, libera ledger, cliente e timers.
- Enquanto esse sandbox está ativo, notificações da fixture demonstrativa normal são suprimidas para não misturar origens.
- Título, estados, horário e freshness são localizados em `pt-BR` e `en-GB`, sempre com texto explícito de sandbox sintético e ausência de dados externos.
- O publisher moderno usa group `local-reconcile`, Tag hexadecimal determinística de 16 caracteres, mark semântico existente, áudio silenciado e expiração de dez minutos. Falha usa a fila local Tray existente, serial e limitada.

## Sequência E2E observada

| Etapa | Evidência observada |
|---|---|
| Host local | Kestrel HTTPS em IP loopback, certificado P-256 curto, identidade exclusivamente de teste e Server SQLite em memória |
| Estado inicial | Agent e instância sintéticos commitados; cursor reconciliado em sequência `1`; nenhum evento histórico entregue |
| Baseline | resposta autenticada válida; ledger gravou o head `1`; sink permaneceu com zero pedido |
| Commit sintético | observação sequência `2`, evento `Disconnected/Error`, detalhes `Healthy → Unavailable` e cursor `2` commitados |
| Leitura | endpoint retornou uma transição sintética atual, íntegra e versionada |
| Entrega | ledger gravou intent e resultado; sink de teste aceitou exatamente um pedido |
| Reinício | primeiro ledger foi fechado, o mesmo diretório foi reaberto e o ciclo seguinte retornou `NoChanges`; total continuou em uma entrega |
| Identidade ausente | baseline sem header de sujeito recebeu HTTP `401` |
| Cleanup | hosts, conexões, locks e diretórios temporários pertencentes aos testes foram encerrados/removidos; zero processo e zero listener DB-Notifier permaneceram |

O sink E2E é deliberadamente local e observável; ele não chama a API Windows. Assim, o teste comprova Agent-head/Server API/transport/coordinator/ledger/restart até a fronteira de publicação, mas não prova apresentação visual, Focus Assist ou Notification Centre.

## Achados da revisão direta e correções

1. **Alta — projeção poderia aceitar detalhes de evento incompatíveis com a observação de origem.** Status e timestamps agora precisam coincidir exatamente; divergência sintética retorna indisponibilidade fail-closed.
2. **Média — evento sintético malformado poderia ser confundido com evento inicial silencioso.** Somente `Connected` com anterior nulo é silencioso; qualquer outro par ausente/incompatível falha fechado.
3. **Média — transporte validava schema e tamanho, mas não o media type.** O cliente agora exige `application/json; charset=utf-8` antes de desserializar.
4. **Média — substituição do ledger poderia prosseguir se o cancelamento chegasse depois do flush temporário.** O token é verificado novamente imediatamente antes do replace.
5. **Baixa — labels limitados ainda aceitavam caracteres de controlo invisíveis.** A validação rejeita todos os caracteres de controlo antes de apresentação ou hashing.
6. **Baixa — o primeiro certificado E2E com key storage efêmero produziu EOF no Schannel deste host.** O harness foi alinhado ao padrão local existente `UserKeySet | Exportable`; buffers PKCS#12 continuam zerados e o possível contêiner temporário é registrado como limitação.
7. **Ferramenta/teste — a primeira falha deixou um lock vazio sob `%TEMP%` porque o cleanup executou antes do dispose de uma declaração `await using`.** A ordem foi corrigida, o caminho exato foi inspecionado e removido, e a repetição final terminou sem resíduo.

Não restou achado crítico, alto ou médio conhecido no diff final.

## Verificações automáticas

- shutdown preflight inicial: zero processo e zero listener DB-Notifier;
- solução .NET 10: `16` projetos, build Release `--no-restore`, zero erro e zero aviso;
- testes .NET: `327/327` unitários, `22/22` de arquitetura e `13/13` de integração;
- testes próprios novos: `6` unitários, `3` de arquitetura e `2` E2E HTTPS/SQLite;
- cobertura .NET: `77,14%` linhas e `49,71%` branches, acima dos pisos `70%`/`45%`;
- Dashboard: TypeScript, Vite, assets/tokens/localisation/provider icons e `60/60` testes aprovados;
- Pester legado compatível: `23` aprovados, um skip condicional previsto e cobertura `32,08%` (`290/904` comandos);
- `dotnet format --verify-no-changes`, documentação de código para `263` fontes, `373` links locais em `89` arquivos, fixture NuGet offline para `16` projetos, secret scan e smoke fail-closed: aprovados;
- cleanup final: zero processo, zero listener e zero diretório temporário pertencente a este incremento.

## Observado, inferido e não testado

### Observado

- baseline silenciosa, uma transição commitada, entrega única, reabertura do ledger e ausência de repetição;
- `401` sem a identidade de teste;
- cursor canónico/dominância, opt-in inerte, silêncio, stale, deduplicação e fencing de processo nos testes determinísticos;
- build WPF e presença da composição/publisher/fallback protegida pelos testes de arquitetura;
- bounds de página, ledger, resposta, tentativas, Agents e intervalos no código e testes;
- todos os runtimes e resíduos temporários pertencentes ao trabalho encerrados ao final.

### Inferido por inspeção direta

- ausência do endpoint na composição normal da API;
- ausência de SignalR, canais externos, comandos ou executor no pipeline novo;
- publicação Windows restrita ao publisher existente e fallback local;
- nenhum segredo gravado no ledger, payload, relatório ou evidência.

### Não testado

- apresentação visível pelo Windows, Focus Assist, Notification Centre, clique humano, acessibilidade visual ou balloon real;
- processo WPF real consumindo a API real; o E2E encerra na implementação testável de `IReconciledNotificationSink`;
- crash real em cada instrução entre intent, Windows e resultado; os estados e políticas foram testados deterministicamente, não por power-loss;
- múltiplos processos hostis, restauração de storage, corrupção física, quota/disco cheio ou permissões NTFS adversariais;
- múltiplos Agents no E2E, página acima de `16`, ledger acima de `256`, endurance ou carga;
- provider, Agent, monitoramento, banco, PKI/IdP/vault, canal ou runtime operacional;
- deploy, instalação, produção, `OBSERVER`, estado posterior ou gate final do `STATE-06`.

## Limitações e condições residuais

- O ledger e a API Windows não compartilham transação. `Attempting` mais Tag estável preferem não repetir uma entrega incerta; por isso uma falha nessa janela pode perder uma notificação. O incremento não promete exactly-once.
- Aceitação pelo publisher ou pela fila fallback não prova que o utilizador viu uma mensagem.
- Um arquivo local não detecta sozinho a restauração silenciosa de uma cópia antiga válida do diretório inteiro. O cursor impede rollback durante a vida normal do ledger, mas uma âncora independente seria necessária para detectar full-storage rollback.
- `FileShare.None` impede proprietários cooperativos simultâneos; não é defesa contra administrador local malicioso ou manipulação offline.
- O E2E usa SQLite em memória como substituto do Server PostgreSQL e dados exclusivamente sintéticos. Não comprova o provider de persistência operacional.
- O certificado P-256 curto usa `UserKeySet | Exportable` por compatibilidade Schannel. Nenhum PKCS#12 é gravado pelo teste e o buffer exportado é zerado, mas não houve inspeção independente de possível contêiner temporário do perfil.
- Os limites `16` itens, `32` Agents, `256` decisões, duas tentativas, dois minutos de freshness, cinco segundos de HTTP e 30 segundos de reconciliação são limites deste sandbox, não dimensionamento operacional ou backpressure de frota.
- Silêncio é uma decisão booleana da composição de teste; não existe calendário, política corporativa ou UI administrativa.
- O publisher Windows foi exercitado por compilação/inspeção, não por amostra humana. Nenhuma alegação de compatibilidade geral Windows decorre deste incremento.

## Classificação dos gates

- Quality Gate automático deste incremento restrito: **APROVADO** no escopo local documentado.
- Human Gate próprio do Incremento 3: **PENDENTE**; nenhuma aceitação foi inferida.
- Quality/Human Gate final do `STATE-06`, Incremento 4, runtime operacional, promoção `none → OBSERVER`, `STATE-07`, produção e release: **NÃO AVALIADOS E NÃO AUTORIZADOS**.

## Próxima decisão humana

Bruno deve ler principalmente `Resultado em linguagem simples`, `Sequência E2E observada`, `Limitações e condições residuais` e `Classificação dos gates`. Depois deve escolher somente uma opção:

1. aceitar este Incremento 3 com as limitações registradas;
2. solicitar uma remediação local claramente delimitada;
3. rejeitar o incremento.

Se concordar, o texto factual sugerido é:

> Incremento STATE-06 Reconciled Local Notification Delivery Sandbox, commit d43e49a: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo novo incremento, runtime operacional, promoção nem transição de estado.

Essa decisão autorizaria somente o registro da aceitação. O Incremento 4, runtime operacional, promoção e transição continuariam separados e proibidos.
