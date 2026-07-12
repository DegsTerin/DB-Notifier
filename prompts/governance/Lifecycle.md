# Ciclo de Desenvolvimento

## Regra geral

Cada fase exige entradas, entregáveis, auditoria automática, decisão humana e handoff. Correções pertencem à fase dona do defeito. Os templates ficam em `../templates/Templates.md`.

## STATE-00 DISCOVERY_MIGRATION

Objetivo: compreender o PgNotifier e propor migração incremental.

Entregáveis: inventário, visão, baseline arquitetural, riscos, plano incremental e corpus de instruções. Não autoriza código da arquitetura-alvo.

Aceite: fatos do legado são verificáveis; proposta não é apresentada como implementação; lacunas e decisões pendentes estão explícitas.

## STATE-01 PROJECT_SETUP

Objetivo: preparar solução, convenções, builds, testes, configuração segura e estrutura modular.

Entregáveis: solution/projects, build reproduzível, testes mínimos, lint/análise estática, configuração por ambiente e CI inicial. Sem regras funcionais de domínio.

Aceite: bootstrap limpo, checks aprovados, nenhum secret e nenhuma implementação prematura.

## STATE-02 ARCHITECTURE

Objetivo: validar limites Agent/API/UI/provider, dados, segurança, implantação e migração.

Entregáveis: arquitetura, ADRs, contratos, threat model, protocolo conceitual, matriz de capacidades e plano de evolução.

Aceite: dependências apontam para o núcleo; falhas parciais, offline, atualização, rollback e compatibilidade foram tratados.

## STATE-03 DATABASE_MODELING

Objetivo: modelar catálogo, eventos, incidentes, alertas, auditoria, outbox e configuração interna.

Entregáveis: modelo, constraints, índices, retenção e migrations não produtivas.

Aceite: segredo não integra o modelo comum; banco interno não é confundido com bancos monitorados; migrations e rollback são verificáveis.

## STATE-04 BACKEND_IMPLEMENTATION

Objetivo: implementar Domain, Application, Provider SDK, providers autorizados, persistência e API.

Entregáveis: casos de uso, probes, normalização de erros, eventos, alertas, RBAC, comandos administrativos e testes.

Aceite: núcleo independente de engine, falhas isoladas, autorização server-side e operações idempotentes.

## STATE-05 FRONTEND_IMPLEMENTATION

Objetivo: implementar Tray/Desktop e Dashboard.

Entregáveis: inventário, status, histórico, alertas, configuração, ações autorizadas e Design System oficial com paridade React/WPF; estados vazio/loading/offline/error/stale/denied.

Aceite: acessibilidade WCAG AA, responsividade, teclado, contraste, temas Light/Dark/System, persistência, paridade React/WPF, timestamp de dados e ausência de dependência exclusiva de cor.

## STATE-06 INTEGRATION

Objetivo: integrar Agent, API, interfaces, providers e canais.

Entregáveis: contratos versionados, autenticação de Agent, sincronização offline, E2E em sandbox e entrega de notificações.

Aceite: reconexão, duplicidade, reorder, expiração de comando e incompatibilidade de versão foram testados.

## STATE-07 TESTING_HOMOLOGATION

Objetivo: validar funcionalidade, segurança, carga e operação representativa.

Entregáveis: matriz por engine/plataforma/papel, testes negativos, carga, recuperação, acessibilidade e relatório de homologação.

Aceite: cobertura e limitações explícitas; providers não homologados não são anunciados; achados críticos resolvidos ou formalmente bloqueados.

## STATE-08 PRODUCTION_RELEASE

Objetivo: liberar artefatos assinados com observabilidade, migração e rollback.

Entregáveis: release candidate, SBOM quando aplicável, assinatura, release notes, runbook, backup/restore, rollout e relatório.

Aceite: alvo e autorização explícitos; secrets externos; health checks reais; rollback ensaiado; sem feature oculta no fechamento.

## Matriz módulo × fase

| Módulo | S02 Arquitetura | S03 Dados | S04 Backend | S05 UI | S06 Integração | S07 Homologação | S08 Release |
|---|---|---|---|---|---|---|---|
| Identity/Access | Contratos | Persistência | Auth/RBAC | Fluxos | Agent/API | Segurança | Operação |
| Instance Catalog | Modelo | Schema | Casos de uso | Gestão | Sync | Escala | Migração |
| Agent Fleet | Protocolo | Heartbeat | API | Visão | Offline | Plataformas | Atualização |
| Service Control | Capabilities | Auditoria | Comandos | Confirmação | Execução | Privilégios | Runbook |
| Provider SDK | Contratos | N/A | Providers | Capabilities | Drivers | Engines | Compatibilidade |
| Monitoring | Semântica | Samples | Scheduler | Status | Real time | Carga/falhas | Métricas |
| Events/Alerts | Fluxos | Retenção | Regras/outbox | Timeline | Canais | Storm/dedup | Observabilidade |
| Dashboard | UX | Consultas | API | Interface | SignalR | A11y/carga | Publicação |
| AIOps/IA | Dados, risco e contratos | Features/feedback | Regras e serviços | Explicações/aprovação | Pipelines | Evals/red team | Rollout por modo |

Um módulo pode ser desenhado antes de implementado, mas desenho não autoriza código prematuro.

MOD-12 começa obrigatoriamente em `OBSERVER`. `ADVISOR`, `ASSISTANT` e `CONTROLLED_AUTOMATION` exigem gates independentes, política opt-in e homologação por classe de ação.
