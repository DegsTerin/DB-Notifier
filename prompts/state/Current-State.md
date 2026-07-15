# Estado Atual

## Posição do ciclo de vida

- Posição técnica do workspace: `STATE-05 FRONTEND_IMPLEMENTATION`.
- Progressão de ciclo de vida: `EM ESPERA`.
- Motivo: a ratificação retrospectiva de `STATE-00` a `STATE-04` foi concluída; o Human Gate atual de `STATE-05` ainda impede progressão.
- Human Gate de `STATE-00`: ratificação retrospectiva `APROVADA` por Bruno em 2026-07-13, limitada a discovery/planejamento e sem validar runtime real, integração, infraestrutura, providers ou testes operacionais futuros.
- Human Gate de `STATE-01`: ratificação retrospectiva `APROVADA COM RESSALVAS` por Bruno em 2026-07-13, baseada em revisão documental; onboarding não repetido e execução remota da CI sem comprovação independente.
- Human Gate de `STATE-02`: ratificação retrospectiva `APROVADA COM RESSALVAS` por Bruno em 2026-07-13; ADR-0001/2/3/4/6 aceitos e ADR-0005 aceito com ressalvas, sem prova operacional, homologação, penetration test, update real ou autorização administrativa.
- Human Gate de `STATE-03`: ratificação retrospectiva `APROVADA COM RESSALVAS` por Bruno em 2026-07-13, limitada ao modelo, migrations revisadas e testes não produtivos; PostgreSQL real, produção, backup/PITR, restore, legal hold, exclusão e rollback operacional não foram executados.
- Human Gate de `STATE-04`: ratificação retrospectiva `APROVADA` por Bruno em 2026-07-13, limitada às evidências determinísticas disponíveis; homologação PostgreSQL permanece `None`, suporte público `No`, capabilities administrativas `Unsupported` e integrações/identidades/execução reais permanecem futuras.
- Human Gate de `STATE-05`: `PENDENTE`. A amostra limitada `S05-HG-011` de Tray foi aprovada por Bruno em 2026-07-15; as demais amostras visuais, de teclado, leitor de tela, zoom, scaling, High Contrast e TV continuam pendentes.
- Nenhuma transição para `STATE-06`, homologação, laboratório multi-provider, integração externa, execução administrativa ou release está autorizada.

## Baseline técnica observada

- Branch `main`; solução com 13 projetos .NET 10, todos em `net10.0` ou `net10.0-windows`.
- Domain e Application permanecem provider-neutral; providers, infraestrutura, persistência, Agent, API, Desktop e Dashboard respeitam as fronteiras aceitas.
- Agent usa SQLite apenas para estado local autorizado; Server usa PostgreSQL apenas para persistência central. Monitored databases nunca são persistence targets do DB-Notifier.
- Seis migrations provider-specific modelam constraints, índices, idempotência, concorrência, outbox, RBAC, audit append-only e rollback não produtivo.
- Provider PostgreSQL implementa endpoint tipado, discovery de `pg_isready`, readiness, fallback TCP degradado, probe Npgsql autenticado e normalização canônica; não existe homologação ou suporte público.
- ConfigMigrator .NET 10 oferece dry-run, bloqueio de secrets/campos desconhecidos, backups, escrita atômica, relatório, idempotência e rollback protegido por hash.
- API implementa ingestão e poll/ack de Agent protegidos por certificado/rota, autenticação humana OIDC/JWT fail-closed, RBAC, catálogo, criação auditada de comando e consulta de auditoria.
- Start/Stop/Restart permanecem `Unsupported`; não existe executor, attempt, post-probe ou fallback administrativo.
- Monitoring, sincronização, polling, retenção aplicada, outbox externo e notification delivery permanecem desabilitados por padrão.
- Não há IdP, certificado, vault, canal de notificação, database target ou infraestrutura real configurada no repositório.

## Frontend implementado em STATE-05

- Onze incrementos implementam Overview operacional, navegação compartilhada de oito destinos, inventário/status, histórico/alertas, demonstrações de desempenho/providers, configuração/capabilities, Tray seguro, flyout operacional, inicialização Windows notification-area-first, política agregada clean-room e ícones semânticos transparentes em React e WPF .NET 10.
- Todas as superfícies usam adapters determinísticos locais e identificam dados de demonstração; nenhuma chama API, Agent, database, IdP, vault, notification channel ou executor.
- Design System `2.5.0` fornece tokens canônicos, geração CSS/XAML determinística, marca provider-neutral transparente de banco ampliado com sino semântico, wordmark visual `DBNotifier` verde/branco, shell empresarial compartilhado, contrato WPF para propriedades de valor composto, hierarquia Windows notification-area-first e política agregada provider-neutral.
- Idiomas atuais: `pt-BR` padrão e `en-GB`, com catálogos XML canônicos e adapters TypeScript/XAML gerados.
- Temas selecionáveis atuais: Light e Dark. Valor antigo ou inválido System migra para Light; Windows High Contrast permanece override independente.
- Dashboard Web é responsivo de 320 CSS px a ultrawide e atende desktop, tablet e mobile Web; não existe aplicativo móvel nativo.
- Modo TV é session-only no Dashboard Web, abre a Overview operacional, mantém Fullscreen opcional, saída persistente, relógio/freshness e verdade de demonstração.
- O Dashboard preserva instantes ISO/UTC e cálculos de freshness, mas apresenta data/hora no fuso do sistema do navegador com rótulo explícito; idioma e tema permanecem disponíveis no modo TV.
- Em mobile Web de `320` a `620` CSS px, marca abreviada e controles de idioma/tema/TV/alertas/Settings permanecem em uma única linha contida, com alvos de `44×44` CSS px.
- Em larguras Web compactas abaixo de `768` CSS px, coleções de Alertas e capabilities administrativas usam uma única coluna; timestamps, providers e reason codes quebram dentro do próprio cartão.
- O resumo de Alertas usa toda a largura operacional e três tracks para suas três métricas até o breakpoint compacto, sem herdar colunas vazias do Inventário.
- O refresh atual recalcula apenas relógio e freshness sobre o snapshot local. Leitura API imediata e reconciliação não sobreposta a cada 30 segundos pertencem a `STATE-06`.
- WPF é o cliente Windows notification-area-first e não possui modo TV. A inicialização normal mantém somente o NotifyIcon; um clique abre o flyout primário e o shell WPF completo é um drill-down secundário. O argumento explícito `--show-desktop` existe para desenvolvimento/auditoria. O shell possui mínimo de `820×620` DIP, preserva os mesmos oito destinos e ordem do Dashboard e reflui KPIs/painéis abaixo de `1000` DIP.
- Flyout do Tray usa leitura compacta em duas colunas: quatro instâncias demonstrativas e seus estados à esquerda; atalhos seguros para Dashboard, Configuração e logs à direita; Restart e Silent Mode permanecem explicativos e não interativos. A política clean-room resume estados como Healthy/Warning/Critical/Unknown, trata stale como unknown e usa precedência `Critical > Warning > Unknown > Healthy`; texto e tooltip continuam claramente demonstrativos. O ícone não possui fundo azul, usa um banco ampliado em canvas transparente e seleciona sino verde/amarelo/vermelho/cinza conforme o agregado determinístico; a fixture atual é Critical e seleciona vermelho. Depois da correção do XAML diferido, Bruno aprovou explicitamente em `S05-HG-011` a hierarquia notification-area-first, o ícone semântico, o flyout e o shell secundário em 2026-07-15. Mudanças derivadas de estado Agent/API real pertencem a `STATE-06`.

## Evidência automática atual

- Restore locked, build Release, 135 testes unit/model/provider/presentation e 7 testes de arquitetura aprovados; 0 avisos e 0 erros na revalidação deste incremento.
- `dotnet format`, auditoria NuGet, 32 testes Dashboard, typecheck, build Vite, auditoria npm, geração/drift de marca/tokens/localização e gate documental aprovados.
- 10 testes Pester e validação do bundle legado aprovados.
- CI reproduz format, auditorias NuGet/npm, bundle, links Markdown, smoke fail-closed, integridade Git e matriz Dashboard headless, além dos builds/testes já existentes.
- A auditoria de rastreabilidade `docs/STATE-05-Request-Traceability-Audit.md` consolida 50 unidades de requisito desta sequência de trabalho e liga cada uma a documento, implementação, teste/evidência e pendência; ela não altera o Human Gate.
- Bruno reconheceu em 2026-07-14 que a matriz representa corretamente suas solicitações e a aceitou somente como inventário documental, declarando explicitamente que isso não aprova o Human Gate de `STATE-05`.
- Bruno aprovou exclusivamente `S05-HG-011` em 2026-07-15 após a revisão autorizada do WPF/Tray; o processo usado na amostra foi encerrado e nenhuma decisão sobre o Human Gate completo foi inferida.
- Matriz Dashboard atual cobre 96 amostras (`2 locales × 2 temas × 24 viewports/rotas`), sem overflow global ou controles interativos sem nome; modal e TV passaram nos quatro pares locale/tema. As oito rotas atuais incluem Overview, Instances, Alerts, Performance, History, Configuration, Providers e Settings. A Overview preserva quatro instâncias e três alertas de demonstração; mobile mantém os cinco controles globais contidos, Alertas/Configuração usam uma coluna e o resumo de Alertas a `960×1040` ocupa a linha completa.
- A baseline Lighthouse `13.4.0` preserva 30 relatórios limpos de três repetições sobre as cinco rotas anteriores. A regressão Design System `2.3.0` acrescenta 16 relatórios atuais (`8` rotas × mobile/desktop × `1` execução): Acessibilidade/Boas Práticas `100` em `16/16`, Performance `99`–`100` e crawler policy válida. O primeiro passe móvel detectou `aria-label` proibido no contêiner de marca; após correção do papel semântico, as 16 amostras foram repetidas. SEO `66` é consequência intencional de `Disallow: /` no console interno, não falha de sintaxe; isso não substitui o Human Gate.
- Matriz WPF Design System `2.3.1` cobre oito combinações de locale/tema/tamanho padrão e mínimo, cada uma com 16 controles focalizáveis visíveis, nenhum sem nome e 12 passos de Tab contidos. High Contrast real foi reconhecido/restaurado e duas amostras técnicas anteriores permaneceram legíveis. Scaling real anterior a 125%/120 DPI e 150%/144 DPI passou no mínimo com rolagem acessível; 200% não foi oferecido pelo monitor `1920×1080`. A janela é `PROCESS_SYSTEM_DPI_AWARE`; cenário mixed-DPI/per-monitor continua não provado.
- O incremento `2.4.0` repetiu startup normal sem janela principal, ativação do NotifyIcon/flyout, abertura segura do shell secundário e ausência de diálogo `.NET`; o processo permaneceu responsivo. O runner WPF passou a usar `--show-desktop` e repetiu `pt-BR`/Dark a `820×620` com 16 controles focalizáveis, nenhum sem nome e 12 passos de Tab contidos. O incremento `2.4.1` acrescentou três testes de agregação/freshness/notificação, totalizando 135 testes unitários, e preservou build WPF sem avisos/erros. O incremento `2.5.0` gerou e verificou o SVG transparente, o ICO padrão e quatro variantes semânticas de nove resoluções; seleção por fixture está implementada, enquanto atualização por estado real e entrega continuam fora de `STATE-05`.
- API local respondeu liveness `200`, catálogo humano sem token `401` e poll de Agent sem certificado `403`; Agent permaneceu ativo com defaults desabilitados e foi encerrado após a amostra.
- Git worktree rastreado permanece limpo antes deste incremento; a referência interna longa e inválida encontrada na auditoria foi copiada para `%TEMP%`, removida pontualmente e `git show-ref`/`git fsck --full` voltaram a sair com código 0.
- Evidência automática não substitui Human Gate nem prova runtime externo, provider real, acessibilidade humana ou produção.

## Ratificação retrospectiva concluída

- Pacote proprietário: `docs/Human-Gate-Retrospective-Ratification.md`.
- `STATE-00`: `APROVADO` retrospectivamente em 2026-07-13, com limites explícitos de discovery/planejamento.
- `STATE-01`: `APROVADO COM RESSALVAS` retrospectivamente em 2026-07-13; onboarding não repetido e CI remota não comprovada independentemente.
- `STATE-02`: `APROVADO COM RESSALVAS` retrospectivamente em 2026-07-13; todos os ADRs aceitos, com ADR-0005 e limites operacionais registrados.
- `STATE-03`: `APROVADO COM RESSALVAS` retrospectivamente em 2026-07-13; modelo/migrations não produtivos aceitos e limites de produção/recuperação preservados.
- `STATE-04`: `APROVADO` retrospectivamente em 2026-07-13; provider/autorizações/migrador aceitos no escopo determinístico e limites de homologação/suporte/execução preservados.
- Resultado: `STATE-00` a `STATE-04` possuem decisões retrospectivas inequívocas e independentes. Isso não aprova `STATE-05` nem autoriza `STATE-06`.

## Dívida e limitações atuais

- A documentação automática garante cabeçalhos de módulos e inventário; cobertura XML completa das APIs públicas preexistentes permanece incremental e exige revisão humana quando o arquivo proprietário for alterado.
- A reauditoria automática final do Design System `2.1.0` permanece aprovada em seu escopo histórico; os incrementos `2.1.1` de hora local/TV/TopBar, `2.1.2` de cartões compactos, `2.1.3` do resumo responsivo, `2.2.0` da Overview/flyout em duas colunas, `2.3.0` do refinamento solicitado, `2.3.1` da paridade/robustez WPF, `2.4.0` da hierarquia tray-first, `2.4.1` da política clean-room e `2.5.0` do ícone semântico transparente também passaram nos gates automáticos aplicáveis, sem substituir o Human Gate.
- Relatórios iniciais de localização/Design System preservam controles e contagens da época como evidência histórica; o contrato atual é Design System `2.5.0`, ícone único de tradução, Light/Dark e onze incrementos frontend.
- Lighthouse é evidência laboratorial local; não prova experiência humana, produção ou telemetria real. O console bloqueia deliberadamente o rastreamento, portanto a nota SEO agregada não é um gate aplicável enquanto `robots.txt` bloquear crawlers.
- Human Gate `STATE-05` ainda precisa confirmar visual da Overview remediada e dos detalhes independentes de `S05-HG-010`, wordmark, oito destinos, TopBar, KPIs, providers/status, alertas/gráficos, layout/preferências/TV e executar Narrator, teclado, zoom, scaling e High Contrast em ambos os idiomas/temas aplicáveis. O fluxo Tray/ícone de `S05-HG-011` está fechado e não precisa ser repetido sem mudança material.
- O Narrator foi iniciado somente após autorização explícita e interrompido a pedido do validador, que informou não conseguir realizar a amostra. Fala, ordem auditiva e usabilidade permanecem `NÃO TESTADAS`, sem inferência de aprovação ou reprovação do produto.
- `STATE-06`: integração API/Agent/UI, heartbeat/enrollment/revocation, reconciliação, SignalR hints, refresh TV real, notificações e E2E sandbox.
- `STATE-07`: PostgreSQL real/homologação, control adapters/post-probe e providers adicionais independentes.
- `STATE-08`: MSI/WiX ou equivalente, Authenticode, SBOM, update channel, backup/restore, rollout e rollback.
- A inicialização automática do WPF no logon do Windows ainda não existe; ela depende de preferência explícita, registro reversível e ownership do instalador assinado em `STATE-08`. A inicialização tray-first atual aplica-se somente quando o processo é executado.

## Próximo gate

Executar as amostras humanas pendentes do `STATE-05` conforme `docs/STATE-05-Human-Gate-Validation.md` e solicitar uma decisão inequívoca exclusiva para `STATE-05`.

Este documento contém somente a verdade presente. Evolução e decisões históricas permanecem em `State-Transition-Log.md` e nos relatórios originais.
