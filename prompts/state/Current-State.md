# Estado Atual do Projeto

## Estado

`STATE-02 ARCHITECTURE`

## Situação factual

O Human Gate de `STATE-01` aprovou o scaffold, os checks e a migração canônica de nomes. Em `STATE-02 ARCHITECTURE`, o pacote de ADRs, contratos, threat model, protocolo, matriz PostgreSQL e guardrails AIOps foi proposto e aprovado com ressalvas na auditoria automática; o Human Gate permanece pendente. O monitor PowerShell de compatibilidade continua sendo o único monitor funcional.

## Produto atual

- Monitor PostgreSQL local/remoto para Windows.
- PowerShell, Windows Forms/WPF, scripts Python experimentais e Inno Setup.
- Probes por `pg_isready` com fallback TCP.
- Controle de serviços Windows locais quando permitido.

## Produto-alvo

- Plataforma multi-provider.
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
- SDK .NET `8.0.422` instalado localmente em `.dotnet/` e ignorado pelo Git.
- Restore bloqueado por lockfile, build Release (0 avisos/erros), 2 testes, format e auditoria NuGet aprovados.
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

## Pendente

- Human Gate de `STATE-02` para aceitar/rejeitar os ADRs propostos.
- Walkthrough humano de ameaças e cenários standalone/on-premises/híbridos.
- Implementação e homologação de qualquer provider além do comportamento PostgreSQL legado.

## Riscos

- Armazenamento e rotação de credenciais.
- Semânticas diferentes de controle administrativo.
- Conectividade remota e privilégios.
- Compatibilidade Agent/API e operação offline.
- Licenciamento e ambientes de Oracle/SQL Server.
- Qualidade, privacidade, prompt injection e risco operacional da futura automação por IA.

## Próximo gate

Validar o pacote arquitetural, revisar as decisões materiais de `docs/STATE-02-Architecture-Report.md` e registrar o Human Gate antes de `STATE-03 DATABASE_MODELING`.

Este documento descreve somente o presente. Histórico pertence a `State-Transition-Log.md`.
