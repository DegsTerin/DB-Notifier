# Estado Atual do Projeto

## Estado

`STATE-05 FRONTEND_IMPLEMENTATION`

## Situação factual

O projeto está em `STATE-05 FRONTEND_IMPLEMENTATION`. Quatro incrementos implementam inventário/status, histórico/alertas, configuração/capabilities e Tray seguro no Dashboard React/Desktop WPF .NET 10. O Tray controla somente a janela DB-Notifier; adapters continuam determinísticos, sem integração, secret, mutation, controle de banco/serviço ou provider homologado.

## Produto atual

- Linhagem registrada: MySQL Notifier inspirou conceitualmente o PgNotifier; DB-Notifier é o sucessor independente do PgNotifier.
- Monitor PostgreSQL local/remoto para Windows.
- PowerShell, Windows Forms/WPF, scripts Python experimentais e Inno Setup.
- Probes por `pg_isready` com fallback TCP.
- Controle de serviços Windows locais quando permitido.

## Produto-alvo

- Plataforma multi-provider.
- Catálogo aberto para qualquer banco por provider/plugin, priorizando os motores mais usados e conhecidos mundialmente.
- Monitoramento-alvo local/remoto/cloud por Agents Windows, Linux, containers ou workloads cloud, com conectividade e credentials provider-specific.
- Identidades separadas para monitoramento, administração do banco, controle do serviço do SO e APIs cloud.
- Agent, Tray/Desktop, API e Dashboard.
- Provider SDK, eventos, alertas, RBAC e auditoria.
- SQLite local e persistência central, conforme ADR.

## Concluído

- Inventário verificável do legado, incluindo comportamento, protótipos, limitações e riscos.
- Visão e baseline arquitetural propostas.
- Plano incremental de migração, compatibilidade de configuração, marcos e rollback.
- Caracterização e compatibilidade do legado: 10 testes Pester aprovados em Windows PowerShell 5.1.
- Sistema de instruções adaptado e consolidado.
- `AGENTS.md` raiz consolidado como fonte operacional das instruções permanentes, com roteamento para o corpus temático e sem substituir ADRs, gates ou evidências.
- Human Gate de `STATE-00` aprovado em 2026-07-11.
- Repositório Git inicializado com branch `main` e commit inicial criado.
- Solução `DBNotifier.sln` com limites modulares e projetos de teste sem regras funcionais prematuras.
- Dashboard React/TypeScript com lockfile, check, build e auditoria de dependências aprovados.
- CI inicial para .NET, Dashboard e compatibilidade legada.
- SDK .NET 10 LTS `10.0.301` instalado localmente em `.dotnet/` e ignorado pelo Git.
- Todos os 10 projetos ativos retargeteados para `net10.0`/`net10.0-windows`: restore, build Release (0 avisos/erros), 2 testes, format, auditoria NuGet e liveness aprovados.
- Liveness da API comprovada localmente e processo encerrado após o teste.
- Commit inicial `ad8baf6` criado na branch `main`.
- App, módulo, testes, protótipos e packaging legados renomeados canonicamente para DB-Notifier.
- Entradas PgNotifier antigas preservadas como shims documentados e cobertas por testes de compatibilidade.
- `build/build.ps1` criado com validação de bundle e sem instalação automática de dependências.
- Human Gate de `STATE-01` aprovado em 2026-07-11.
- Pacote arquitetural `docs/architecture/` com seis ADRs propostos e limites de componentes/falhas.
- Contratos canônicos de health, eventos, erros, capabilities, comandos, heartbeat e credenciais.
- Protocolo Agent/API v1 conceitual com operação offline, idempotência, reconciliação e compatibilidade.
- Threat model, matriz de capacidades PostgreSQL e guardrails de dados/risco/evals para MOD-12.
- ADR-0001 aceito: baseline única .NET 10 LTS durante todo o projeto.
- ADR-0002 a ADR-0006 e pacote arquitetural completo aceitos no Human Gate de `STATE-02`.
- Modelo lógico documentado para catálogo, Agents, health, eventos, incidentes, alertas, comandos, RBAC, auditoria, outbox e configuração local.
- Persistência isolada em `DBNotifier.Persistence.Agent.Sqlite` e `DBNotifier.Persistence.Server.PostgreSql`; cada runtime carrega somente seu provider.
- Seis migrations provider-specific não produtivas, com constraints, índices, concorrência, idempotência, envelope de compatibilidade e referências opacas de credencial.
- SQLite validado em memória do initial ao latest e rollback latest → previous → zero; PostgreSQL validado por scripts forward/rollback offline.
- Retenção e deleção segura documentadas; audit PostgreSQL protegido contra update/delete por trigger.
- Solução .NET 10 com 12 projetos: restore locked, build Release (0 avisos/erros), 5 testes, format e auditoria NuGet aprovados.
- Dependência nativa SQLite vulnerável inicialmente resolvida por pin central seguro, sem supressão; auditoria final sem vulnerabilidades.
- Auditoria automática de `STATE-03` aprovada e relatório de evidências emitido.
- Objetivo universal de providers aceito: PostgreSQL primeiro, seguido por ondas priorizadas e extensão aberta sem condicionais de engine no núcleo.
- Topologias local, remota, Windows, Linux, híbrida e cloud aceitas como objetivo, sem autorizar abertura automática de rede ou reutilização insegura de credenciais.
- Human Gate de `STATE-03` aprovado em 2026-07-11 após modelagem, migrations, rollback, retenção e recuperação revisados.
- `ProviderType` aberto e canônico, sem enum fechado de engines; registro aceita providers futuros sem alteração do núcleo.
- Contratos de health/evidência/erro/credencial/capability/endpoint e caso de uso de probe implementados de forma provider-neutral.
- Endpoint PostgreSQL tipado, `pg_isready` sem shell, timeout e fallback TCP implementados; TCP-only é sempre `Degraded`.
- Health PostgreSQL autenticado implementado com Npgsql, TLS configurável seguro, query fixa e mapeamento canônico; ainda sem evidência contra banco real.
- Retry/backoff limitado, credential lease descartável, purpose enforcement e isolamento de falha por instância implementados.
- Sink SQLite grava observation, checkpoint monotônico e outbox atomicamente; rollback e ausência de secret no payload testados.
- Initializer/assignment source e worker recorrente implementados com intervalo limitado, filtering seguro e telemetria estruturada.
- Readers `windows-credential-manager` e `linux-secret-service` implementados sem shell/fallback plaintext e selecionados por provider exato.
- Outbox dispatch/ack implementado com ordem monotônica, outcomes por item, tombstone e retry exponencial limitado.
- Ingestão central valida Agent/instância, deduplica IDs, rejeita conflito de sequência e persiste sample/event/server-outbox/alert deliveries atomicamente.
- API de observation batch exige certificado de Agent ativo e autorização exata do `agentId` na rota; chamada local sem certificado negada com 403.
- Terceira migration PostgreSQL adiciona unicidade `(agent_id, sequence)` com rollback offline verificável.
- Autenticação humana separada usa OIDC/JWT externo, exige `sub`, aplica rate limit sem fila e falha fechada sem Authority/Audience seguros.
- RBAC server-side filtra catálogo por `instances.read` e escopos Global/Environment/Instance não expirados.
- Criação de comando exige `commands.create`, Agent/capability/version exatos, validação segura, idempotência e auditoria sanitizada.
- Poll/ack de comandos exige mTLS, Agent da rota, versão exata e sequência durável; o inbox aceita replay idêntico, rejeita conflito e não cria attempt/resultado/executor.
- Discovery de pacotes valida `net10.0`, chave pública confiável, assinatura RSA-PSS/SHA-256, hashes, limites e paths sem carregar assembly ou registrar provider automaticamente.
- Retenção Agent/central implementada em lotes, com dry-run default, preservação de audit/referências e workers opt-in.
- Server-outbox e notification delivery possuem runners duráveis, backoff limitado e IDs estáveis; adapters externos não são registrados.
- Consulta de auditoria exige `audit.read` Global, paginação estável por snapshot/filtros limitados e audita acesso permitido/negado.
- Controles administrativos permanecem explicitamente `Unsupported`.
- Agent registra catálogo/provider, persistência, vault e scheduler por DI; monitoring permanece desabilitado por default, sem conexão real ou UI.
- Migrador isolado .NET 10 executa dry-run default, bloqueia secrets/campos desconhecidos, preserva origem, cria backups, escreve atomicamente, produz relatório/manifesto, rerun idempotente e rollback protegido por hash.
- Discovery PostgreSQL tipado cobre path, sibling de `postgres.exe`, `PATH` e instalações Program Files sem shell/reparse; credencial expirada falha antes de chamar provider.
- 104 testes .NET aprovados (99 unit/model/provider + 5 arquitetura); build Release e format aprovados em .NET 10 com 0 avisos/erros.
- Auditoria inicial reprovada preservada em `docs/STATE-04-Backend-Implementation-Audit.md`; remediação reauditada como `APROVADO` em `docs/STATE-04-Backend-Implementation-Reaudit.md`.
- Human Gate de `STATE-04` aprovado em 2026-07-12 após revisão das evidências de falha de provider, autorização negativa e sanitização do migrador.
- Transição factual para `STATE-05 FRONTEND_IMPLEMENTATION`, sem autorizar integração externa, execução administrativa ou homologação de provider.
- Contrato provider-neutral de apresentação `inventory.v1` com status canônico, timestamps, stale após cinco minutos e resumo que nunca conta dado vencido como saudável atual.
- Dashboard responsivo com inventário/status, busca/filtro, tabela/cards, labels de suporte e estados ready/loading/empty/offline/error/stale/denied/filtered-empty.
- Shell WPF .NET 10 com o mesmo inventário/status e cenários operacionais, `DataGrid` read-only, AutomationProperties e navegação por teclado.
- 107 testes .NET e 3 testes de apresentação Dashboard aprovados; builds Release/Vite, typecheck, format, npm audit, smoke da janela WPF e amostras visuais desktop/compacta aprovados.
- Contrato `history-alerts.v1`, timeline pesquisável/filtrável e alertas com severidade/estado/timestamps implementados sem condicionais de engine.
- Dashboard e WPF apresentam histórico/alertas somente leitura; acknowledge/silence permanecem desabilitados e manutenção é um estado explícito.
- 109 testes .NET e 4 testes Dashboard aprovados no segundo incremento; builds e amostras visuais desktop/compacta aprovados.
- Contrato `configuration-capabilities.v1` e visões Dashboard/WPF implementam configuração não secreta e decisões confirmation/denied/unsupported/unavailable/unknown.
- Start/Stop/Restart PostgreSQL permanecem `Unsupported`; confirmação é apenas exemplo rotulado e o controle final de execução permanece desabilitado.
- 114 testes .NET e 5 testes Dashboard aprovados no terceiro incremento; builds e amostras visuais desktop/compacta aprovados.
- Tray Windows real implementado com abrir, status factual e sair; minimizar/fechar recolhe a janela sem tocar banco ou serviço, com descarte explícito do ícone/menu.
- Guards automatizados validam contraste WCAG AA, semântica, foco e reduced-motion no Dashboard, além de contraste textual WPF e política provider-neutral do Tray.
- 125 testes .NET e 7 testes Dashboard aprovados no quarto incremento; smoke close-to-Tray comprovou processo vivo sem janela visível e cleanup posterior.
- Padrão global de documentação de código em inglês britânico formalizado, com cabeçalhos de módulo nos fontes manuais, exceções estreitas para formatos estritos/gerados/imutáveis e gate `comments:verify` integrado ao CI.
- Auditoria automática de encerramento de `STATE-05` executada e `REPROVADA`: overflow horizontal global em 390/320 px e diálogo modal sem entrada/contenção/restauração de foco ou fechamento por Escape bloqueiam o Human Gate.
- Bloqueadores `S05-AUD-001` e `S05-AUD-002` remediados: viewports/rotas sem overflow global e diálogo nativo com foco inicial, Tab/Shift+Tab contidos, Escape e restauração ao acionador.
- Reauditoria automática de `STATE-05` `APROVADA` com 125 testes .NET, 8 testes Dashboard, 10 Pester, builds/format/bundle/dependências e amostras Chrome/WPF aprovadas; Human Gate permanece pendente.
- Design System `1.0.0` formalizado como especificação oficial: identidade empresarial moderna/contida, tokens canônicos, componentes, WCAG 2.2 AA, temas Light/Dark/System, persistência e paridade React/WPF.
- Primeiro incremento do Design System concluído: schema/tokens canônicos, geração CSS/XAML determinística, contratos System/Light/Dark em TypeScript/.NET 10 e gates de drift/contraste.
- 133 testes .NET e 12 testes Dashboard aprovados no primeiro incremento, que estabeleceu a base antes da integração runtime.
- Segundo incremento do Design System concluído no React: bootstrap pré-render sem flash de tema incorreto, preferência System/Light/Dark persistida de modo resiliente, observação live do sistema, sincronização entre abas e `ThemeSelector` acessível.
- Dashboard migrado para CSS gerado e tokens semânticos/componentes canônicos, sem cores ou shadows crus nos estilos manuais e sem alterar rotas, filtros, estado seguro ou fronteiras provider-neutral.
- 14 testes Dashboard, build/typecheck, drift de tokens, gate documental de 148 fontes, npm/NuGet audit, 133 testes .NET, 10 Pester, bundle e amostras Light 1440/Dark 390 aprovados; viewport de 390 CSS px comprovado sem overflow global.
- Localização de interface `pt-BR` (padrão) e `en-GB` implementada no Dashboard React e no Desktop/Tray WPF a partir de catálogos XML canônicos com geração TypeScript/XAML determinística e gate de drift no CI.
- Seletores de idioma persistem somente o locale validado, falham com segurança para `pt-BR` e preservam identificadores técnicos; o Dashboard permanece responsivo para amostras mobile/tablet/desktop e o WPF mantém mínimo desktop de `820×620` DIP.
- Incremento bilíngue verificado com 18 testes Dashboard, 136 testes .NET, build Release sem avisos/erros, 18 amostras browser sem overflow global e WPF/UI Automation nos dois locales em `1180×760` e `820×620`.
- Terceiro incremento do Design System concluído: WPF aplica tokens gerados, troca Light/Dark atomicamente, segue o tema Windows em System, dá precedência a High Contrast e persiste tema/locale juntos sem material secreto.
- Dashboard e WPF expõem botões discretos `pt-BR`, `en-GB`, System, Light e Dark na região superior direita do TopBar, com estado selecionado acessível, teclado e adaptação compacta.
- Reauditoria automática combinada aprovada em 54 amostras browser (`2 locales × 3 temas × 9 viewport/rota`), sem overflow global ou controlo interativo sem nome, além das seis combinações WPF e amostra mínima `820×620`.
- Protocolo do Human Gate de `STATE-05` preparado em `docs/STATE-05-Human-Gate-Validation.md`; preflight confirmou Narrator disponível, escala atual 100% e High Contrast desligado.
- Primeira amostra humana interrompida com `S05-HG-001`: o Dashboard `pt-BR` foi explicitamente reprovado na revisão visual por não parecer moderno nem empresarial; teclado/Narrator e demais amostras não foram executados.
- Primeira remediação visual de `S05-HG-001` implementada no Design System `1.3.0`: chrome coeso Light/Dark, seletores discretos, hierarquia de superfícies refinada, navegação horizontal antes da compressão e cartões operacionais completos em larguras estreitas, com paridade WPF.
- Reauditoria automática afetada aprovada em 60 amostras browser (`2 locales × 3 preferências × 10 viewport/rota`, incluindo `960×1040`) e sete amostras WPF, sem overflow global nem controles interativos/focalizáveis sem nome; o resultado humano inicial não foi sobrescrito.
- Primeira remediação julgada explicitamente melhor pelo usuário, porém ainda insuficiente para aceitação visual; foi solicitada nova inspiração em softwares empresariais internacionais.
- Segunda remediação Design System `1.3.1` consolidou os KPIs em uma faixa operacional, substituiu glifos por ícones SVG coerentes, conteve a seleção de navegação/preferências e reduziu `card soup`, após revisão de padrões oficiais Carbon, Grafana Saga e Fluent.
- Segunda matriz afetada aprovada novamente em 60 amostras browser; WPF Light/Dark representativos mantiveram 0 controles focalizáveis sem nome.
- Segunda remediação visual explicitamente aprovada pelo usuário em 2026-07-13; a aprovação fecha `S05-HG-001` somente na sua porção visual e não abrange teclado, Narrator ou o Human Gate completo.
- Design System `1.3.2` implementa o pedido subsequente de um ícone simples de banco de dados em todo o produto ativo: SVG/ICO determinísticos e provider-neutral no Dashboard, WPF, executável/atalhos, Tray e instalador. A confirmação visual do novo ícone permanece pendente.

## Pendente

- Completar amostras humanas bilíngues com leitor de tela, zoom nativo, Windows scaling/High Contrast e revisão visual conforme inventário do Design System.
- Confirmar visualmente o ícone canônico de banco de dados; somente após novo consentimento explícito iniciar Narrator e as demais porções pendentes de `HG05-01`.
- Manter adapters de apresentação determinísticos até `STATE-06`; não integrar silenciosamente banco, IdP, certificado, canal ou provider real durante a fase de UI.
- Representar capabilities e suporte de modo factual: ação ausente/negada/unsupported não pode aparecer como executável ou homologada.
- Preservar as pendências posteriores de ativação sandbox de pacotes, integração real, execução/post-probe de comandos, legal hold/backup e adapters externos.
- Implementação e homologação dos demais providers por ondas independentes.

## Riscos

- Armazenamento e rotação de credenciais.
- Semânticas diferentes de controle administrativo.
- Conectividade remota e privilégios.
- Compatibilidade Agent/API e operação offline.
- Licenciamento e ambientes de Oracle/SQL Server.
- Qualidade, privacidade, prompt injection e risco operacional da futura automação por IA.

## Próximo gate

Confirmar o ícone canônico na janela visível e obter consentimento explícito antes de iniciar Narrator/teclado em `HG05-01`. O Human Gate, `STATE-06` e o laboratório continuam bloqueados.

Este documento descreve somente o presente. Histórico pertence a `State-Transition-Log.md`.
