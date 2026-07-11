# Relatório de Migração dos Prompts

## Resumo

- Data: 2026-07-11
- Prompts de controle lidos: 2 (`Prompt-New-Project.md` e `Prompt-Adapt-Doc.md`)
- Prompts herdados analisados: 75
- Prompts adaptados e mantidos ativos: 75
- Prompts arquivados: 0
- Prompts renomeados: 3

## Alterações relevantes

- Remoção do domínio ShiftFlow, seu histórico preenchido e suas alegações de execução.
- Substituição de Node.js/Next.js/Prisma e paths herdados por uma baseline tecnológica proposta para DB-Notifier, ainda sujeita a ADR.
- Inclusão de `STATE-00 DISCOVERY_MIGRATION` para representar honestamente o PgNotifier legado.
- Preservação do fluxo controlado STATE-01 a STATE-08, agora orientado ao DB-Notifier.
- Conversão dos módulos de equipes, turnos e Kanban em Agent Fleet, Service Control e Monitoring Operations.
- Adaptação de segurança para credenciais de monitoramento/administração, identidade de Agent, RBAC por instância e comandos auditáveis.
- Relatórios e validações herdados foram transformados em templates; aprovações e evidências do outro projeto foram removidas.

## Renomeações

- `Team-Management-Module.md` → `Agent-Fleet-Module.md`
- `Shift-Management-Module.md` → `Service-Control-Module.md`
- `Operational-Kanban-Module.md` → `Monitoring-Operations-Module.md`

## Arquivamento

Nenhum prompt foi arquivado. A estrutura de governança, fases, auditorias e gates continua útil após a reescrita conceitual. Por isso, `prompts/_archive/obsolete-prompts.md` não foi criado.

## Lacunas identificadas

- ADR definitivo de stack e estratégia de migração PowerShell → .NET.
- Matriz de capacidades por engine para probe, métricas e Start/Stop/Restart.
- Protocolo versionado Agent/API, provisionamento de identidade e rotação.
- Escolha de cofre de credenciais por Windows, servidor e ambiente híbrido.
- Política de retenção/agregação de métricas e eventos.
- Estratégia de atualização e assinatura de Agent/Desktop.
- Ambientes e licenças para homologar Oracle e SQL Server; semântica própria do MongoDB.

## Novos prompts sugeridos

- `Provider-SDK-Module.md`
- `Instance-Catalog-Module.md`
- `Alerting-Notification-Module.md`
- `Agent-Api-Protocol.md`
- `Credential-Vault-Policy.md`
- `Provider-Capability-Matrix.md`
- `Observability-Retention-Policy.md`
- `Legacy-Migration-Plan.md`

Esses documentos devem ser criados em uma versão MINOR futura, depois de validar a baseline arquitetural em STATE-02; não foram adicionados agora para preservar a contagem e o escopo dos 75 arquivos herdados.
