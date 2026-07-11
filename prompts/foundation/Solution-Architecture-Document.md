# Arquitetura da Solução DB-Notifier

## Status

Baseline proposta. Deve ser confirmada em `STATE-02 ARCHITECTURE` por ADRs antes de ser tratada como arquitetura implementada.

## Princípios

- Núcleo independente de engine, UI, transporte e persistência.
- Providers traduzem detalhes nativos para contratos canônicos sem ocultar diagnóstico útil.
- Observação e administração são capacidades distintas.
- Operação local continua durante indisponibilidade da API.
- Protocolos, eventos e comandos são versionados e idempotentes.
- Dados desconhecidos ou vencidos nunca aparecem como saudáveis.

## Componentes

```text
src/
  DBNotifier.Domain/
  DBNotifier.Application/
  DBNotifier.Provider.Abstractions/
  DBNotifier.Providers.PostgreSql/
  DBNotifier.Providers.MySql/
  DBNotifier.Providers.SqlServer/
  DBNotifier.Providers.Oracle/
  DBNotifier.Providers.MongoDb/
  DBNotifier.Infrastructure/
  DBNotifier.Agent.Worker/
  DBNotifier.Desktop.Wpf/
  DBNotifier.Server.Api/
  DBNotifier.Dashboard.Web/
tests/
```

Dependências apontam para dentro: Domain não conhece provider, driver, UI, API ou persistência. Application define casos de uso e portas. Infrastructure e interfaces implementam adaptadores.

## Provider SDK

Contrato conceitual mínimo:

```text
IDatabaseProvider
  ProviderType
  ValidateConfiguration
  ProbeHealth
  GetCapabilities
  ExecuteAdministrativeCommand
  NormalizeError
```

Cada provider declara capacidades, limitações, requisitos de privilégio e plataformas homologadas. Ausência de capacidade retorna `Unsupported`, nunca uma tentativa improvisada.

Start, Stop e Restart podem usar SQL administrativo, utilitário nativo, API do fornecedor ou serviço do sistema operacional apenas por adaptador e política explícitos.

## Modelo interno de dados

O armazenamento do DB-Notifier não deve ser confundido com as bases monitoradas.

Entidades mínimas:

- `Instance`, `Endpoint`, `ProviderType` e `CredentialReference`
- `Agent`, `AgentCapability` e `Heartbeat`
- `HealthSample`, `Event`, `Incident` e `MaintenanceWindow`
- `AlertRule`, `NotificationChannel` e `NotificationDelivery`
- `AdministrativeCommand` e `CommandAttempt`
- `User`, `Role`, `Permission` e `RoleAssignment`
- `AuditEntry` e `OutboxMessage`

Requisitos de modelagem:

- IDs globais e timestamps UTC.
- Índices por instância, tempo, status e correlação.
- Retenção e agregação para amostras de alta frequência.
- Concorrência otimista e idempotência.
- Soft delete somente onde o histórico permitir.
- Segredos representados por referências opacas.
- Migrations compatíveis com SQLite local e PostgreSQL central, quando aplicável.

## Módulos

### MOD-03 AGENT_FLEET

Identidade, versão, plataforma, capacidades, heartbeat, revogação, fila local, reconciliação e atualização controlada dos Agents.

### MOD-04 SERVICE_CONTROL

Comandos assíncronos de Start, Stop e Restart com capability, RBAC, confirmação, motivo, timeout, idempotência, resultado verificável e auditoria.

### MOD-09 DASHBOARD

Visão global e drill-down por servidor, ambiente, provider, tag e status; disponibilidade, latência, incidentes, alertas, Agents desconectados e dados obsoletos.

### MOD-10 MONITORING_OPERATIONS

Estados `Healthy`, `Degraded`, `Unavailable`, `AuthFailed`, `Timeout`, `Unknown` e `Maintenance`; correlação, reconhecimento, silenciamento temporário, atribuição e encerramento de incidentes sem apagar histórico.

### MOD-11 RBAC_AUDIT

Papéis, permissões, escopos e trilha imutável. Os detalhes normativos estão em `../governance/Security-And-Access.md`.

### Módulos transversais

- MOD-01 IDENTITY_ACCESS
- MOD-02 INSTANCE_CATALOG
- MOD-05 PROVIDER_SDK
- MOD-06 HEALTH_MONITORING
- MOD-07 EVENT_HISTORY
- MOD-08 ALERTING

### MOD-12 AIOPS_AI

Regras determinísticas, análise estatística, correlação, base de conhecimento, LLM, planejamento e automação controlada. A especificação completa está em `AIOps-And-AI-Module.md`. O módulo é roadmap e não integra a baseline implementada.

## Comunicação Agent/API

- HTTPS obrigatório; autenticação forte do Agent.
- Contrato versionado com compatibilidade declarada.
- Idempotency key para comandos e lotes de eventos.
- Outbox no Agent e no servidor.
- Sequência/cursor para reconciliar eventos offline.
- Heartbeat separado do estado das instâncias.
- Comando expira e pode ser cancelado; reexecução não deve duplicar efeito.

## Persistência e tempo real

- SQLite local guarda configuração autorizada, cache e outbox.
- PostgreSQL central guarda catálogo, políticas, eventos, auditoria e usuários.
- SignalR publica mudanças; a API continua sendo fonte de autorização e dados.
- Dashboard exibe `observedAt`, `receivedAt` e condição stale.

## Implantação

- Desktop/Agent assinados e atualizáveis de modo controlado.
- API e Dashboard podem ser on-premises ou cloud.
- Configurações por ambiente, sem secrets no pacote.
- Observabilidade inclui logs estruturados, métricas, traces e health checks reais.
- Rollback separa binário, configuração, schema interno e protocolo.

## ADRs pendentes

- Stack definitiva e estratégia PowerShell → .NET.
- Cofre por plataforma e modelo de provisionamento do Agent.
- Formato e compatibilidade do protocolo Agent/API.
- ORM/migrations e retenção de telemetria.
- Atualização e assinatura de Agent/Desktop.
- Matriz de capacidades/licenças por engine.
