# Estado Atual do Projeto

## Estado

`STATE-01 PROJECT_SETUP`

## Situação factual

O Human Gate de `STATE-00` aprovou a baseline, a migração incremental PostgreSQL-first e a inicialização de Git. O scaffold modular, o Dashboard mínimo, as convenções, os testes iniciais e a CI foram preparados e validados em `STATE-01`; a aplicação PgNotifier continua sendo o único monitor funcional. A auditoria automática está aprovada e o Human Gate de saída permanece pendente.

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
- Caracterização não mutável do legado: 8 testes Pester aprovados em Windows PowerShell 5.1.
- Sistema de instruções adaptado e consolidado.
- Human Gate de `STATE-00` aprovado em 2026-07-11.
- Repositório Git inicializado com branch `main`; ainda sem commit inicial.
- Solução `DBNotifier.sln` com limites modulares e projetos de teste sem regras funcionais prematuras.
- Dashboard React/TypeScript com lockfile, check, build e auditoria de dependências aprovados.
- CI inicial para .NET, Dashboard e caracterização PgNotifier.
- SDK .NET `8.0.422` instalado localmente em `.dotnet/` e ignorado pelo Git.
- Restore bloqueado por lockfile, build Release (0 avisos/erros), 2 testes, format e auditoria NuGet aprovados.
- Liveness da API comprovada localmente e processo encerrado após o teste.

## Pendente

- ADR definitivo de stack.
- Matriz de capacidades por engine.
- Arquitetura de dados, avaliações e políticas para MOD-12 AIOPS_AI.
- Human Gate de `STATE-01` e primeiro commit do repositório.
- Implementação e homologação de qualquer provider além do comportamento PostgreSQL legado.
- Build reproduzível do legado ou scaffold, pois `build/build.ps1` citado pela documentação antiga não existe.

## Riscos

- Armazenamento e rotação de credenciais.
- Semânticas diferentes de controle administrativo.
- Conectividade remota e privilégios.
- Compatibilidade Agent/API e operação offline.
- Licenciamento e ambientes de Oracle/SQL Server.
- Qualidade, privacidade, prompt injection e risco operacional da futura automação por IA.

## Próximo gate

Revisar `docs/STATE-01-Setup-Report.md` e registrar o Human Gate antes de `STATE-02 ARCHITECTURE`.

Este documento descreve somente o presente. Histórico pertence a `State-Transition-Log.md`.
