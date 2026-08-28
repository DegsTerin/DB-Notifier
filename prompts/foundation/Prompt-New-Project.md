# Visão do Projeto DB-Notifier

## Contexto

O workspace contém o PgNotifier, um monitor PostgreSQL para Windows implementado principalmente em PowerShell. Conforme registro do product owner, o PgNotifier foi inspirado conceitualmente no MySQL Notifier, e o DB-Notifier é seu sucessor. `GOV-MN-RESTORE-01` registra a intenção corrigida do proprietário, restaura a cláusula de inspiração funcional de `REQ-047` e permite que MySQL Notifier 1.1.8 informe resultados funcionais observáveis, sanitizados e não expressivos para aperfeiçoar o DB-Notifier; ele não é base de implementação nem mandato de clone. Os resultados úteis devem ser recriados por requisitos, código, testes e ativos próprios, provider-neutral e seguros. Analistas expostos à fonte entregam somente registro de proveniência e especificação comportamental sanitizados e aprovados; autores correspondentes de implementação/testes não recebem source, binários, ativos, material decompilado nem notas brutas ou não sanitizadas derivadas da fonte. Código-fonte, binários, identidade, texto de produto, trade dress, arquitetura interna e material terceiro originário, incluído ou derivado da árvore de referência Oracle/MySQL não entram no projeto MIT sem proveniência/direitos documentados, modelo de distribuição compatível, revisão jurídica especializada e decisão separada do proprietário. Essa linhagem não declara compatibilidade técnica, afiliação ou dependência.

O PgNotifier é o legado executável e a base de aprendizado, não a arquitetura final. O DB-Notifier transforma a ideia original em uma plataforma independente, segura e multi-provider.

O DB-Notifier será uma plataforma profissional para monitorar e administrar múltiplas instâncias de bancos de dados locais, remotas, corporativas, híbridas ou em cloud. O produto deve aceitar qualquer motor de banco de dados por meio de providers/plugins, sem uma lista fechada ou condicionais de engine no núcleo.

## Objetivos

- Monitorar simultaneamente diferentes instâncias e motores.
- Detectar disponibilidade, indisponibilidade, timeout, falha de autenticação, reconexão e resposta lenta.
- Oferecer histórico de eventos, alertas e notificações.
- Permitir ações administrativas controladas de Start, Stop e Restart quando a engine e o ambiente oferecerem uma capacidade segura.
- Manter aplicação Tray/Desktop simples e preparar Dashboard Web centralizado.
- Evoluir incrementalmente o PgNotifier sem uma reescrita big bang.
- Priorizar os motores mais utilizados e conhecidos mundialmente, preservando uma extensão documentada para motores novos, especializados e proprietários.
- Fazer do `MOD-12 AIOPS_AI` o principal diferencial estratégico do produto: transformar telemetria factual, provider-neutral e sanitizada em sinais, previsões e recomendações explicáveis, mantendo cada promoção e qualquer automação sob os gates próprios definidos em [`AIOps-And-AI-Module.md`](AIOps-And-AI-Module.md).

## Cobertura universal por providers

O catálogo de destino é aberto. As ondas iniciais priorizam:

- Relacionais/SQL: PostgreSQL, MySQL, MariaDB, Microsoft SQL Server/Azure SQL, Oracle, SAP HANA, SQLite, IBM Db2, Firebird e CockroachDB.
- Documento/NoSQL: MongoDB, Couchbase e CouchDB.
- Distribuídos/wide-column: Apache Cassandra e ScyllaDB.
- Key-value/data platforms: Redis e Valkey.
- Busca, séries temporais e grafos: Elasticsearch, OpenSearch, InfluxDB e Neo4j.
- Serviços gerenciados/cloud: variantes compatíveis e APIs próprias de AWS, Azure, Google Cloud, Oracle Cloud e outros fornecedores.
- Novos motores, forks e produtos proprietários por providers/plugins futuros.

Essa relação é uma priorização, não um limite. Um provider deve poder registrar um identificador estável, schema de endpoint, capabilities e normalização próprios sem alterar o núcleo. Presença na visão representa objetivo arquitetural; suporte público só pode ser anunciado depois de implementação e homologação específicas do conjunto engine, versão, plataforma e operação.

SQLite possui dois papéis independentes: armazenamento interno local do Agent e possível alvo monitorado por um provider SQLite. Um papel não comprova nem substitui o outro.

## Ambientes e formas de conexão

- Bancos locais ou remotos, em Windows, Linux, containers, datacenter, ambientes híbridos e cloud devem ser representáveis no catálogo.
- O Agent conecta ao banco pelo driver, protocolo, socket, utilitário nativo ou API homologada do provider; a API central não acessa diretamente o banco monitorado.
- Monitoramento autenticado usa credencial de menor privilégio própria para leitura de health/metrics.
- Controle administrativo usa outra identidade e somente quando a capability for segura: credencial administrativa do banco, identidade do serviço Windows/Linux, identidade de workload ou API do fornecedor/cloud.
- Conectividade pode usar rede local, VPN, private endpoint, proxy/túnel aprovado ou endpoint público protegido por TLS e política. O produto não cria exposição de rede automaticamente.
- Cofres e identidade devem funcionar em Windows, Linux e serviços cloud/corporativos sem persistir segredo em configuração, banco interno, log ou UI.

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

## Stack obrigatória

A baseline aceita nos ADRs e obrigatória para os projetos ativos é:

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
- Novo provider integrável por contrato/plugin sem alterar Domain ou contratos canônicos existentes.
- Falhas parciais não tornam o sistema inteiro indisponível.
- Status inclui horário e indicador de dado obsoleto.
- Controle administrativo é explícito, seguro, auditável e opcional.
- Logs, testes, documentação, atualização e rollback fazem parte da entrega.
