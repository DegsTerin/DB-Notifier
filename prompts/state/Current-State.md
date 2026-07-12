# Estado Atual do Projeto

## Estado

`STATE-04 BACKEND_IMPLEMENTATION`

## Situação factual

O primeiro incremento de `STATE-04 BACKEND_IMPLEMENTATION` implementa contratos neutros de Domain/Application, registro aberto de providers e o adapter PostgreSQL de readiness. Não há scheduler operacional, banco real testado ou provider homologado; o monitor PowerShell de compatibilidade continua sendo o único runtime funcional para usuário final.

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
- Controles administrativos e health autenticado PostgreSQL declarados explicitamente `Unsupported` neste incremento.
- Agent registra o catálogo/provider por DI, sem iniciar scheduler, conexão real ou UI.
- 30 testes .NET aprovados (26 unit/model/provider + 4 arquitetura); build Release e format com 0 avisos/erros.

## Pendente

- Expandir Domain/Application além do probe inicial: scheduler, políticas, eventos, alertas, autorização e transações.
- Completar o vertical slice PostgreSQL com discovery, health autenticado, credencial via vault, retries e integração de persistência/outbox.
- Implementar descoberta/carregamento seguro de pacotes de provider além do registro DI em processo.
- Integrar persistência operacional do Agent/API, eventos, alertas, RBAC e comandos administrativos tipados conforme autorização.
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

Planejar e executar o primeiro incremento de `STATE-04 BACKEND_IMPLEMENTATION`: núcleo neutro e vertical slice PostgreSQL em .NET 10, sem antecipar UI funcional de `STATE-05`.

Este documento descreve somente o presente. Histórico pertence a `State-Transition-Log.md`.
