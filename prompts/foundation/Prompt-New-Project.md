# Visão do Projeto DB-Notifier

## Contexto

O workspace contém o PgNotifier, um monitor PostgreSQL para Windows implementado principalmente em PowerShell. Ele é o legado executável e a base de aprendizado, não a arquitetura final.

O DB-Notifier será uma plataforma profissional para monitorar e administrar múltiplas instâncias de bancos de dados locais, remotas, corporativas, híbridas ou em cloud.

## Objetivos

- Monitorar simultaneamente diferentes instâncias e motores.
- Detectar disponibilidade, indisponibilidade, timeout, falha de autenticação, reconexão e resposta lenta.
- Oferecer histórico de eventos, alertas e notificações.
- Permitir ações administrativas controladas de Start, Stop e Restart quando a engine e o ambiente oferecerem uma capacidade segura.
- Manter aplicação Tray/Desktop simples e preparar Dashboard Web centralizado.
- Evoluir incrementalmente o PgNotifier sem uma reescrita big bang.

## Motores previstos

- PostgreSQL
- MySQL e MariaDB
- SQL Server
- Oracle
- MongoDB
- Novos motores por providers/plugins

Presença nesta lista representa objetivo arquitetural. Suporte só pode ser anunciado depois de implementação e homologação próprias.

## Informações de uma instância

- ID e nome amigável
- Tipo e versão do banco
- Ambiente, tags e grupo
- Host, porta e database/service name quando aplicável
- Referência segura à credencial de monitoramento
- Referência opcional à credencial administrativa
- Intervalo, timeout, retries e política de monitoramento
- Agent responsável
- Status, latência, última amostra e horário de atualização
- Capacidades administrativas declaradas pelo provider

## Eventos canônicos

- `Connected`
- `Disconnected`
- `Timeout`
- `AuthenticationFailed`
- `SlowResponse`
- `Degraded`
- `Recovered`
- `AgentOffline`
- `AdministrativeCommandCompleted`
- `AdministrativeCommandFailed`

## Arquitetura esperada

```text
Database instances
        |
DB-Notifier Agent + Tray/Desktop
        |
HTTPS / protocolo versionado
        |
DB-Notifier Server/API
        |
Dashboard Web + canais de notificação
```

O Agent executa em background, realiza probes próximos das instâncias, mantém operação local quando desconectado e sincroniza com a API. A API centraliza inventário, políticas, eventos e acesso. O Dashboard oferece visão, filtros, histórico, alertas e ações autorizadas.

## Segurança obrigatória

- Menor privilégio e separação entre monitoramento e administração.
- Segredos armazenados em cofre do sistema operacional ou secret manager.
- Criptografia em trânsito e proteção adequada em repouso.
- Autenticação do Dashboard e identidade revogável dos Agents.
- RBAC aplicado server-side.
- Auditoria de login, segredo, configuração e comando administrativo.
- Nenhuma connection string completa em log, UI, commit ou evidência.

## Stack de referência

A baseline preferencial, sujeita a ADR, é:

- .NET 10 LTS/C# para Core, Agent, serviços e Desktop WPF durante todo o projeto.
- ASP.NET Core e SignalR para API e atualizações em tempo real.
- React/TypeScript para Dashboard.
- SQLite para estado local do Agent.
- PostgreSQL para persistência central.

## Migração incremental

1. Inventariar e testar o comportamento PgNotifier existente.
2. Isolar PostgreSQL atrás de abstração de provider.
3. Separar Domain, Application e Infrastructure.
4. Introduzir Agent e nova aplicação Desktop preservando migração de configuração.
5. Criar API e protocolo versionado.
6. Criar Dashboard.
7. Adicionar providers um por vez, com matriz de capacidades e homologação.

## Critérios de sucesso

- Arquitetura extensível sem condicionais de engine no núcleo.
- Falhas parciais não tornam o sistema inteiro indisponível.
- Status inclui horário e indicador de dado obsoleto.
- Controle administrativo é explícito, seguro, auditável e opcional.
- Logs, testes, documentação, atualização e rollback fazem parte da entrega.
