# Estado Atual

## Posição do ciclo de vida

- Posição técnica do workspace: `STATE-05 FRONTEND_IMPLEMENTATION`.
- Progressão de ciclo de vida: `EM ESPERA`.
- Motivo: o validador informou em 2026-07-13 que não tem certeza de ter aprovado conscientemente os Human Gates anteriores e que respostas curtas como “Aprovado” foram interpretadas como decisões completas.
- Human Gates de `STATE-00` a `STATE-04`: registros históricos preservados, mas ratificação retrospectiva `PENDENTE` para cada estado.
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
- Design System `2.1.0` fornece tokens canônicos, geração CSS/XAML determinística, marca provider-neutral e shell empresarial compartilhado.
- Idiomas atuais: `pt-BR` padrão e `en-GB`, com catálogos XML canônicos e adapters TypeScript/XAML gerados.
- Temas selecionáveis atuais: Light e Dark. Valor antigo ou inválido System migra para Light; Windows High Contrast permanece override independente.
- Dashboard Web é responsivo de 320 CSS px a ultrawide e atende desktop, tablet e mobile Web; não existe aplicativo móvel nativo.
- Modo TV é session-only no Dashboard Web, com Fullscreen opcional, saída persistente, relógio/freshness e verdade de demonstração.
- O refresh atual recalcula apenas relógio e freshness sobre o snapshot local. Leitura API imediata e reconciliação não sobreposta a cada 30 segundos pertencem a `STATE-06`.
- WPF é uma aplicação Windows desktop com mínimo de `820×620` DIP; não possui modo TV.
- Flyout do Tray apresenta quatro instâncias demonstrativas, estados textuais/semânticos, snapshot local, atalhos seguros e Restart não interativo.

## Evidência automática atual

- Restore locked, build Release, 128 testes unit/model/provider/presentation e 5 testes de arquitetura aprovados; 0 avisos e 0 erros.
- `dotnet format`, auditoria NuGet, 23 testes Dashboard, typecheck, build Vite, auditoria npm, geração/drift de marca/tokens/localização e gate documental aprovados.
- 10 testes Pester e validação do bundle legado aprovados.
- CI reproduz format, auditorias NuGet/npm, bundle, links Markdown, smoke fail-closed, integridade Git e matriz Dashboard headless, além dos builds/testes já existentes.
- Matriz Dashboard atual cobre 44 amostras (`2 locales × 2 temas × 11 viewports/rotas`), sem overflow global ou controles interativos sem nome; modal e TV passaram nos quatro pares locale/tema.
- API local respondeu liveness `200`, catálogo humano sem token `401` e poll de Agent sem certificado `403`; Agent permaneceu ativo com defaults desabilitados e foi encerrado após a amostra.
- Git worktree rastreado permanece limpo antes deste incremento; a referência interna longa e inválida encontrada na auditoria foi copiada para `%TEMP%`, removida pontualmente e `git show-ref`/`git fsck --full` voltaram a sair com código 0.
- Evidência automática não substitui Human Gate nem prova runtime externo, provider real, acessibilidade humana ou produção.

## Ratificação retrospectiva pendente

- Pacote proprietário: `docs/Human-Gate-Retrospective-Ratification.md`.
- `STATE-00`: revisar discovery, inventário legado, migração incremental, build/Git originalmente bloqueados e limites de runtime.
- `STATE-01`: revisar bootstrap, compatibilidade, build/test e ausência de repetição humana do onboarding limpo.
- `STATE-02`: decidir separadamente sobre ADR-0001 a ADR-0006, threat model, cenários híbridos, retenção, packaging e AIOps.
- `STATE-03`: revisar modelos SQLite/PostgreSQL, migrations, rollback, retenção e ausência de execução PostgreSQL real.
- `STATE-04`: revisar provider PostgreSQL, autorização negativa, migrador, ausência de homologação e ausência de execução administrativa.
- Cada decisão permanece `PENDENTE` até confirmação inequívoca do validador para um único estado.

## Dívida e limitações atuais

- A documentação automática garante cabeçalhos de módulos e inventário; cobertura XML completa das APIs públicas preexistentes permanece incremental e exige revisão humana quando o arquivo proprietário for alterado.
- A reauditoria automática final do Design System `2.1.0` e do flyout do Tray está aprovada em `docs/STATE-05-Frontend-Implementation-Final-Reaudit.md`; isso não substitui o Human Gate.
- Human Gate `STATE-05` ainda precisa confirmar visual do ícone/layout/nome/preferências/TV/Tray e executar Narrator, teclado, zoom, scaling e High Contrast em ambos os idiomas/temas aplicáveis.
- `STATE-06`: integração API/Agent/UI, heartbeat/enrollment/revocation, reconciliação, SignalR hints, refresh TV real, notificações e E2E sandbox.
- `STATE-07`: PostgreSQL real/homologação, control adapters/post-probe e providers adicionais independentes.
- `STATE-08`: MSI/WiX ou equivalente, Authenticode, SBOM, update channel, backup/restore, rollout e rollback.

## Próximo gate

Concluir a ratificação retrospectiva de `STATE-00` a `STATE-04`, um estado por vez. Depois executar as amostras humanas pendentes do `STATE-05` e solicitar uma decisão inequívoca exclusiva para `STATE-05`.

Este documento contém somente a verdade presente. Evolução e decisões históricas permanecem em `State-Transition-Log.md` e nos relatórios originais.
