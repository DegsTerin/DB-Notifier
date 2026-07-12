# Estado Atual do Projeto

## Estado

`STATE-05 FRONTEND_IMPLEMENTATION`

## Situação factual

O projeto está em `STATE-05 FRONTEND_IMPLEMENTATION`. Dois incrementos implementam contratos `inventory.v1`/`history-alerts.v1` e visões somente leitura de inventário, status, histórico e alertas no Dashboard React e Desktop WPF .NET 10, com adapters determinísticos e estados operacionais incluindo manutenção. Nenhuma integração, mutation, entrega externa ou ação administrativa foi ativada; nenhum provider está homologado.

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

## Pendente

- Implementar configuração e apresentação capability-aware de confirmação/denied/unsupported sem antecipar execução administrativa.
- Completar validação de acessibilidade com leitor de tela, contraste automatizado, zoom, teclado e viewports representativos.
- Definir comportamento de Tray/notification area preservando offline, stale e suporte factual.
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

Executar o terceiro incremento de `STATE-05`: configuração provider-neutral e apresentação capability-aware de confirmação/denied/unsupported no Dashboard e WPF, sem persistir mutations, despachar comandos ou habilitar Start/Stop/Restart.

Este documento descreve somente o presente. Histórico pertence a `State-Transition-Log.md`.
