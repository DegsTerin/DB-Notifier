# Arquitetura da Solução DB-Notifier

## Status

Baseline aceita no Human Gate de `STATE-02 ARCHITECTURE`. ADR-0001 a ADR-0006 e os contratos governam as fases seguintes até serem substituídos formalmente. Aceitação arquitetural não representa implementação ou homologação.

## Princípios

- Núcleo independente de engine, UI, transporte e persistência.
- Providers traduzem detalhes nativos para contratos canônicos sem ocultar diagnóstico útil.
- Observação e administração são capacidades distintas.
- Operação local continua durante indisponibilidade da API.
- Protocolos, eventos e comandos são versionados e idempotentes.
- Dados desconhecidos ou vencidos nunca aparecem como saudáveis.
- O catálogo de providers é aberto: nenhum enum fechado ou lista compilada no núcleo limita os motores integráveis.

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
  DBNotifier.Providers.SapHana/
  DBNotifier.Providers.Sqlite/
  DBNotifier.Providers.<Engine>/
  DBNotifier.Infrastructure/
  DBNotifier.Persistence.Agent.Sqlite/
  DBNotifier.Persistence.Server.PostgreSql/
  DBNotifier.Agent.Worker/
  DBNotifier.Desktop.Wpf/
  DBNotifier.Server.Api/
  DBNotifier.ConfigMigrator/
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

Novos providers são descobertos por registro/plugin versionado e possuem identificador estável, configuração não secreta tipada, referências de credencial, probes, capabilities, normalização de erro e fixtures próprios. Domain e Application não recebem condicionais por nome de engine. O objetivo de cobertura é universal, com entrega incremental e homologação independente por engine/versão/plataforma.

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

O modelo entregue em `STATE-03` separa os providers por assembly: o Agent referencia apenas SQLite, o Server apenas PostgreSQL, e Infrastructure mantém somente convenções relacionais compartilhadas. Modelo, retenção e recuperação estão em `../../docs/data/README.md`; entrega não equivale a aplicação produtiva de migration.

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
- Ao entrar no modo TV, o Dashboard faz uma leitura autorizada imediata da API e, enquanto o modo permanecer ativo, inicia uma nova leitura autoritativa a cada 30 segundos somente depois de a anterior terminar. Um hint autenticado do SignalR pode antecipar a leitura, mas não substitui essa reconciliação periódica.
- Falha, timeout ou desconexão durante a atualização mantém o último snapshot conhecido, preserva seus timestamps e apresenta erro, offline ou stale conforme os fatos; o cliente nunca avança artificialmente `observedAt` ou `receivedAt`.
- Dashboard exibe `observedAt`, `receivedAt` e condição stale.

## Implantação

- Desktop/Agent assinados e atualizáveis de modo controlado.
- API e Dashboard podem ser on-premises ou cloud.
- Agents podem operar em Windows, Linux, containers ou hosts/workloads cloud, próximos ao banco local ou remoto.
- O Agent, nunca o Dashboard ou a API central, estabelece a conexão de monitoramento com a instância usando o adaptador provider homologado.
- Rede corporativa, VPN, private endpoint, proxy/túnel aprovado ou TLS público são topologias suportáveis por configuração e política; DB-Notifier não abre firewall nem publica banco automaticamente.
- Monitoramento, administração do banco, controle de serviço do SO e API cloud usam identidades/referências separadas conforme a capability.
- Configurações por ambiente, sem secrets no pacote.
- Observabilidade inclui logs estruturados, métricas, traces e health checks reais.
- Rollback separa binário, configuração, schema interno e protocolo.

## Pacote arquitetural proposto

- ADR-0001 (`accepted`): .NET 10 LTS obrigatório em todos os projetos ativos, builds, testes, CI e implementação futura.
- ADR-0002: cofre, referências opacas, identidade mTLS e provisionamento de Agent.
- ADR-0003: protocolo HTTP durável, SignalR não autoritativo e compatibilidade.
- ADR-0004: EF Core, SQLite local, PostgreSQL central, migrations separadas e retenção.
- ADR-0005: packaging, assinatura, atualização em anéis e rollback.
- ADR-0006: capabilities de provider e controle administrativo tipado.
- Contratos canônicos, protocolo conceitual, threat model, matriz PostgreSQL e guardrails AIOps/IA.

Índice: `../../docs/architecture/README.md`. Status: ADR-0001 a ADR-0006 `accepted`.
