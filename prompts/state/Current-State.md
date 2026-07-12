# Estado Atual do Projeto

## Estado

`STATE-04 BACKEND_IMPLEMENTATION`

## Situação factual

Cinco incrementos de `STATE-04 BACKEND_IMPLEMENTATION` implementam contratos neutros, registro aberto, readiness/health autenticado PostgreSQL, scheduler, SQLite local, readers de vault, sincronização idempotente, eventos/alertas, API autorizada para Agents e API humana OIDC/JWT com RBAC/auditoria e comandos apenas `Pending`. Monitoring e synchronization permanecem desabilitados por default; nenhum banco, IdP, certificado ou credencial real foi testado e nenhum provider está homologado.

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
- Quatro migrations provider-specific não produtivas, com constraints, índices, concorrência, idempotência e referências opacas de credencial.
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
- Comandos permanecem `Pending`, sem outbox/attempt/Agent/executor ou ação administrativa real.
- Controles administrativos permanecem explicitamente `Unsupported`.
- Agent registra catálogo/provider, persistência, vault e scheduler por DI; monitoring permanece desabilitado por default, sem conexão real ou UI.
- 75 testes .NET aprovados (71 unit/model/provider + 4 arquitetura); build Release e format aprovados em .NET 10 com 0 avisos/erros.

## Pendente

- Expandir Domain/Application com retenção, delivery de notificações, consulta de auditoria e protocolo de entrega de comandos sem executor administrativo.
- Completar o vertical slice PostgreSQL com discovery e execução sandbox habilitada usando credencial descartável autorizada.
- Implementar retenção local/central e delivery real de notificações com backpressure/deduplicação.
- Implementar descoberta/carregamento seguro de pacotes de provider além do registro DI em processo.
- Integrar IdP/MFA real, provisionamento de usuários/papéis e mutations de catálogo em ambiente autorizado.
- Cobrir falhas isoladas, autorização negativa, idempotência, retries, timeout, `UnknownOutcome` e testes do provider.
- Auditar `STATE-04` e submeter seus entregáveis ao Human Gate antes de qualquer transição para UI.
- Implementação e homologação dos demais providers por ondas independentes.

## Riscos

- Armazenamento e rotação de credenciais.
- Semânticas diferentes de controle administrativo.
- Conectividade remota e privilégios.
- Compatibilidade Agent/API e operação offline.
- Licenciamento e ambientes de Oracle/SQL Server.
- Qualidade, privacidade, prompt injection e risco operacional da futura automação por IA.

## Próximo gate

Planejar o sexto incremento de `STATE-04 BACKEND_IMPLEMENTATION`: retenção local/central, server-outbox e delivery de notificações com backpressure/deduplicação, além de consulta autorizada de auditoria, sem antecipar UI funcional de `STATE-05` ou execução administrativa real.

Este documento descreve somente o presente. Histórico pertence a `State-Transition-Log.md`.
