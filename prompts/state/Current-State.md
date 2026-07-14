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
- Human Gate de `STATE-05`: `PENDENTE` e ainda dependente das amostras humanas visuais, de teclado, leitor de tela, zoom, scaling, High Contrast, TV e Tray.
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

- Cinco incrementos implementam inventário/status, histórico/alertas, configuração/capabilities, Tray seguro e flyout operacional em React e WPF .NET 10.
- Todas as superfícies usam adapters determinísticos locais e identificam dados de demonstração; nenhuma chama API, Agent, database, IdP, vault, notification channel ou executor.
- Design System `2.1.2` fornece tokens canônicos, geração CSS/XAML determinística, marca provider-neutral e shell empresarial compartilhado.
- Idiomas atuais: `pt-BR` padrão e `en-GB`, com catálogos XML canônicos e adapters TypeScript/XAML gerados.
- Temas selecionáveis atuais: Light e Dark. Valor antigo ou inválido System migra para Light; Windows High Contrast permanece override independente.
- Dashboard Web é responsivo de 320 CSS px a ultrawide e atende desktop, tablet e mobile Web; não existe aplicativo móvel nativo.
- Modo TV é session-only no Dashboard Web, com Fullscreen opcional, saída persistente, relógio/freshness e verdade de demonstração.
- O Dashboard preserva instantes ISO/UTC e cálculos de freshness, mas apresenta data/hora no fuso do sistema do navegador com rótulo explícito; idioma e tema permanecem disponíveis no modo TV.
- Em mobile Web de `320` a `620` CSS px, marca abreviada e controles de idioma/tema/TV permanecem em uma única linha contida, com alvos de `44×44` CSS px.
- Em larguras Web compactas abaixo de `768` CSS px, coleções de Alertas e capabilities administrativas usam uma única coluna; timestamps, providers e reason codes quebram dentro do próprio cartão.
- O refresh atual recalcula apenas relógio e freshness sobre o snapshot local. Leitura API imediata e reconciliação não sobreposta a cada 30 segundos pertencem a `STATE-06`.
- WPF é uma aplicação Windows desktop com mínimo de `820×620` DIP; não possui modo TV.
- Flyout do Tray apresenta quatro instâncias demonstrativas, estados textuais/semânticos, snapshot local, atalhos seguros e Restart não interativo.

## Evidência automática atual

- Restore locked, build Release, 128 testes unit/model/provider/presentation e 5 testes de arquitetura aprovados; 0 avisos e 0 erros.
- `dotnet format`, auditoria NuGet, 28 testes Dashboard, typecheck, build Vite, auditoria npm, geração/drift de marca/tokens/localização e gate documental aprovados.
- 10 testes Pester e validação do bundle legado aprovados.
- CI reproduz format, auditorias NuGet/npm, bundle, links Markdown, smoke fail-closed, integridade Git e matriz Dashboard headless, além dos builds/testes já existentes.
- Matriz Dashboard atual cobre 52 amostras (`2 locales × 2 temas × 13 viewports/rotas`), sem overflow global ou controles interativos sem nome; modal e TV passaram nos quatro pares locale/tema. Mobile `320×568`/`390×844` manteve o TopBar contido em uma linha, e Alertas/Configuração usaram uma coluna sem overflow interno; TV preservou idioma/tema e evidenciou o fuso do sistema.
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
- A reauditoria automática final do Design System `2.1.0` e do flyout do Tray permanece aprovada em seu escopo histórico; os incrementos `2.1.1` de hora local/TV/TopBar e `2.1.2` de cartões compactos também passaram nos gates Dashboard aplicáveis, sem substituir o Human Gate.
- Human Gate `STATE-05` ainda precisa confirmar visual do ícone/layout/nome/preferências/TV/Tray e executar Narrator, teclado, zoom, scaling e High Contrast em ambos os idiomas/temas aplicáveis.
- `STATE-06`: integração API/Agent/UI, heartbeat/enrollment/revocation, reconciliação, SignalR hints, refresh TV real, notificações e E2E sandbox.
- `STATE-07`: PostgreSQL real/homologação, control adapters/post-probe e providers adicionais independentes.
- `STATE-08`: MSI/WiX ou equivalente, Authenticode, SBOM, update channel, backup/restore, rollout e rollback.

## Próximo gate

Executar as amostras humanas pendentes do `STATE-05` conforme `docs/STATE-05-Human-Gate-Validation.md` e solicitar uma decisão inequívoca exclusiva para `STATE-05`.

Este documento contém somente a verdade presente. Evolução e decisões históricas permanecem em `State-Transition-Log.md` e nos relatórios originais.
