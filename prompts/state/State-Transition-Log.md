# Log de Transições de Estado

## Regras

- Append-only: correções são novas entradas.
- Usar datas ISO 8601.
- Registrar apenas transições ou decisões reais.
- Evidências devem existir e estar sanitizadas.
- Relatório, auditoria ou recomendação não alteram estado sozinhos.

## 2026-07-11 — Entrada inicial

- Estado anterior: não declarado
- Estado resultante: `STATE-00 DISCOVERY_MIGRATION`
- Decisão: registrar o PgNotifier como legado e preparar a transformação DB-Notifier.
- Evidências: README do PgNotifier, inventário do workspace e baseline documental.
- Resultado: documentação preparada; arquitetura multi-provider não declarada como implementada.

## 2026-07-11 — Migração do corpus

- Estado anterior: `STATE-00 DISCOVERY_MIGRATION`
- Estado resultante: sem transição
- Decisão: adaptar 75 prompts herdados para o domínio DB-Notifier.
- Resultado: 77 arquivos de controle/instrução coerentes, sem implementação de produto.

## 2026-07-11 — Consolidação do corpus

- Estado anterior: `STATE-00 DISCOVERY_MIGRATION`
- Estado resultante: sem transição
- Decisão: reorganizar os 77 arquivos em 12 documentos temáticos.
- Motivo: reduzir boilerplate, wrappers por fase e custo de manutenção, preservando estado e histórico separados.
- Resultado: hierarquia consolidada e referências atualizadas.

## 2026-07-11 — Inclusão do módulo AIOps/IA

- Estado anterior: `STATE-00 DISCOVERY_MIGRATION`
- Estado resultante: sem transição
- Decisão: incluir `MOD-12 AIOPS_AI` como especificação de longo prazo.
- Escopo: arquitetura em camadas, modos operacionais, risco, aprovação, execução tipada, governança de modelos e integração documental.
- Resultado: módulo documentado como roadmap; nenhuma IA ou automação declarada como implementada.

## 2026-07-11 — Fechamento técnico da descoberta

- Estado anterior: `STATE-00 DISCOVERY_MIGRATION`
- Estado resultante: sem transição
- Decisão: concluir o inventário verificável e submeter o plano incremental PgNotifier → DB-Notifier ao Human Gate.
- Escopo: comportamento legado, compatibilidade de configuração, marcos M0-M8, riscos, rollback e proposta de baseline tecnológica.
- Gates: auditoria automática recomendou `STATE-01 PROJECT_SETUP`; Human Gate permanece `PENDENTE`.
- Evidências: `docs/Legacy-Inventory.md`, `docs/Legacy-Migration-Plan.md`, `docs/STATE-00-Discovery-Report.md` e 8 testes Pester aprovados.
- Riscos/ressalvas: build legado não reproduzível por ausência de `build/build.ps1`; workspace sem Git; runtime real e controles de serviço não testados.

## 2026-07-11 — Transição para Project Setup

- Estado anterior: `STATE-00 DISCOVERY_MIGRATION`
- Estado solicitado: `STATE-01 PROJECT_SETUP`
- Decisão: `APROVADO` pelo usuário após revisão do handoff apresentado.
- Escopo: baseline .NET 8/WPF/ASP.NET Core + React, PostgreSQL como primeiro provider, migração side-by-side e inicialização de Git.
- Gates: auditoria automática de `STATE-00` concluída; Human Gate aprovado explicitamente.
- Evidências: `docs/STATE-00-Discovery-Report.md`, `docs/Legacy-Inventory.md`, `docs/Legacy-Migration-Plan.md` e resposta do usuário na sessão.
- Riscos/ressalvas: SDK .NET 8 ainda ausente; runtime real, packaging e controles administrativos permanecem não autorizados/não testados.
- Aprovador: Bruno, 2026-07-11.
- Estado resultante: `STATE-01 PROJECT_SETUP`.

## 2026-07-11 — Scaffold inicial de Project Setup

- Estado anterior: `STATE-01 PROJECT_SETUP`
- Estado resultante: sem transição
- Decisão: materializar o scaffold aprovado sem introduzir regras funcionais de fases futuras.
- Escopo: Git/main, solução modular, convenções, Agent/API/WPF bootstrap, Dashboard, testes e CI.
- Gates: Dashboard, legado e checks estruturais aprovados; gate .NET bloqueado porque há runtimes, mas nenhum SDK instalado.
- Evidências: `docs/STATE-01-Setup-Report.md`, lockfile npm, 8 testes Pester aprovados e auditoria npm com 0 vulnerabilidades.
- Riscos/ressalvas: código C# ainda não compilado; CI remota não executada; Human Gate de `STATE-01` pendente.

## 2026-07-11 — Validação local de Project Setup

- Estado anterior: `STATE-01 PROJECT_SETUP`
- Estado resultante: sem transição
- Decisão: instalar o SDK .NET 8 isolado no workspace após autorização explícita e concluir a auditoria automática.
- Escopo: restore por lockfile, build Release, testes, format, auditoria NuGet e amostra de liveness da API.
- Gates: auditoria automática aprovada; Human Gate permanece `PENDENTE`.
- Evidências: SDK `8.0.422`, 10 projetos compilados com 0 avisos/erros, 2 testes aprovados, format limpo, nenhum pacote NuGet vulnerável e `/health/live` respondendo `Alive`.
- Riscos/ressalvas: CI remota não executada; nenhum provider funcional, banco real ou comando administrativo testado.

## 2026-07-11 — Migração canônica de nomes legados

- Estado anterior: `STATE-01 PROJECT_SETUP`
- Estado resultante: sem transição
- Decisão: tornar DB-Notifier o nome canônico dos artefatos executáveis, preservando compatibilidade por shims PgNotifier finos e documentados.
- Escopo: app/módulo PowerShell, configuração default, testes, protótipos, packaging, build e documentação.
- Gates: 10 testes Pester aprovados, shim exportado, configuração antiga preservada, bundle válido, .NET e Dashboard sem regressão.
- Evidências: `docs/Legacy-Compatibility.md`, `docs/STATE-01-Setup-Report.md` e commit inicial `ad8baf6`.
- Riscos/ressalvas: EXE/installer não gerados porque `ps2exe` e Inno Setup não estão instalados; remoção dos shims depende do gate documentado.

## 2026-07-11 — Transição para Architecture

- Estado anterior: `STATE-01 PROJECT_SETUP`
- Estado solicitado: `STATE-02 ARCHITECTURE`
- Decisão: `APROVADO` pelo usuário após revisão do handoff apresentado.
- Escopo: encerrar scaffold, checks, CI inicial e migração canônica de nomes; autorizar ADRs, contratos, threat model, protocolo conceitual e matriz de capacidades.
- Gates: auditoria automática de `STATE-01` aprovada; Human Gate aprovado explicitamente.
- Evidências: `docs/STATE-01-Setup-Report.md`, commits `ad8baf6` e `98891fd`, 10 testes Pester, build .NET sem avisos/erros, 2 testes .NET, format e auditorias de dependência aprovados.
- Riscos/ressalvas: CI remota e packaging real não executados; nenhum provider DB-Notifier, banco real ou comando administrativo homologado.
- Aprovador: Bruno, 2026-07-11.
- Estado resultante: `STATE-02 ARCHITECTURE`.

## 2026-07-11 — Pacote arquitetural proposto

- Estado anterior: `STATE-02 ARCHITECTURE`
- Estado resultante: sem transição
- Decisão: propor seis ADRs e contratos transversais para revisão/auditoria antes do Human Gate.
- Escopo: limites, stack/migração, secrets/Agent identity, protocolo, persistência/retenção, packaging/update, provider capabilities, threat model, matriz PostgreSQL e AIOps.
- Gates: decisão humana ainda `PENDENTE`; ADRs permanecem `proposed`.
- Evidências: `docs/architecture/` e `docs/STATE-02-Architecture-Report.md`.
- Riscos/ressalvas: proposta retargeta produto futuro para .NET 10; WiX, mTLS/vault, EF/migrations e retenção ainda não foram implementados/homologados.

## 2026-07-11 — Baseline única .NET 10 LTS

- Estado anterior: `STATE-02 ARCHITECTURE`
- Estado resultante: sem transição
- Decisão: aceitar ADR-0001 com .NET 10 LTS obrigatório desde a baseline ativa até o fim do projeto.
- Escopo: SDK, target frameworks, C#, dependências Microsoft, lockfiles, build, testes, CI e documentação.
- Autoridade: instrução explícita do product owner; versões anteriores permanecem apenas no histórico append-only.
- Gates: ADR-0001 `accepted`; ADR-0002 a ADR-0006 e Human Gate final de `STATE-02` permanecem pendentes.
- Evidências: SDK `10.0.301`, targets `net10.0`/`net10.0-windows`, 10 projetos compilados com 0 avisos/erros, 2 testes aprovados, format limpo, auditoria NuGet sem vulnerabilidades, API `Alive`, lockfiles e ADR-0001.

## 2026-07-11 — Transição para Database Modeling

- Estado anterior: `STATE-02 ARCHITECTURE`
- Estado solicitado: `STATE-03 DATABASE_MODELING`
- Decisão: `APROVADO` pelo usuário para ADR-0001 a ADR-0006 e encerramento da arquitetura.
- Escopo: aceitar limites, .NET 10, secrets/Agent identity, protocolo, persistência/retenção, packaging/update, provider capabilities, contracts, threat model e AIOps guardrails.
- Gates: auditoria automática `APROVADO COM RESSALVAS`; Human Gate aprovado explicitamente.
- Evidências: `docs/STATE-02-Architecture-Report.md`, `docs/architecture/`, commit `4e6dbcc` e retarget .NET 10 `dd5f558`.
- Riscos/ressalvas: implementação, migrations, WiX/signing, vault/mTLS real, providers e pentest permanecem não executados; retenção não autoriza deleção produtiva.
- Aprovador: Bruno, 2026-07-11.
- Estado resultante: `STATE-03 DATABASE_MODELING`.

## 2026-07-11 — Modelagem interna concluída

- Estado anterior: `STATE-03 DATABASE_MODELING`
- Estado resultante: sem transição
- Decisão: concluir a implementação e auditoria automática dos entregáveis de modelagem, mantendo o Human Gate pendente.
- Escopo: modelos SQLite/PostgreSQL, constraints, índices, concorrência, idempotência, referências opacas de credencial, retenção, quatro migrations e recuperação.
- Gates: auditoria automática `APROVADO`; Human Gate `PENDENTE`.
- Evidências: `docs/STATE-03-Database-Modeling-Report.md`, `docs/data/`, build .NET 10 de 12 projetos sem avisos/erros, 5 testes, scripts PostgreSQL offline, format e auditoria NuGet sem vulnerabilidades.
- Riscos/ressalvas: nenhuma migration aplicada a PostgreSQL, produção ou banco monitorado; retenção, vault/mTLS, autenticação, RBAC e backend continuam não implementados.

## 2026-07-11 — Objetivo universal de providers

- Estado anterior: `STATE-03 DATABASE_MODELING`
- Estado resultante: sem transição
- Decisão: aceitar como objetivo do produto todos os motores de banco de dados, priorizando os mais utilizados e conhecidos mundialmente.
- Escopo: catálogo aberto por provider/plugin, ondas de prioridade, inclusão explícita de SAP HANA e SQLite monitorável, além de engines relacionais, NoSQL, distribuídas, especializadas e cloud-managed.
- Gates: cada engine/versão/plataforma/capability continua exigindo implementação, segurança, fixtures e homologação próprias antes de suporte público.
- Evidências: visão do projeto, arquitetura da solução, matriz de providers e plano incremental atualizados.
- Riscos/ressalvas: objetivo universal não implica entrega simultânea, suporte imediato ou controle administrativo disponível para toda engine.
- Aprovador: Bruno, 2026-07-11.

## 2026-07-11 — Cobertura de conexão e credenciais

- Estado anterior: `STATE-03 DATABASE_MODELING`
- Estado resultante: sem transição
- Decisão: aceitar monitoramento e controle provider-specific para bancos locais, remotos e cloud a partir de Agents Windows, Linux, containers ou workloads cloud.
- Escopo: drivers/protocolos/sockets/utilitários/APIs homologados, rede autorizada e referências distintas para monitoramento, administração, serviço do SO e plano de controle cloud.
- Gates: conectividade, identidade e cada capability exigem implementação, testes de segurança e homologação por provider/plataforma.
- Evidências: visão, arquitetura, ADR-0002, ADR-0006, matriz de conectividade e política de segurança atualizadas.
- Riscos/ressalvas: DB-Notifier não abre firewall, não cria endpoint público e não presume que credencial do banco controle Windows, Linux, container ou cloud.
- Aprovador: Bruno, 2026-07-11.

## 2026-07-11 — Linhagem conceitual do produto

- Estado anterior: `STATE-03 DATABASE_MODELING`
- Estado resultante: sem transição
- Decisão: registrar que o PgNotifier foi inspirado conceitualmente no MySQL Notifier e que o DB-Notifier é o sucessor independente do PgNotifier.
- Escopo: contexto histórico e inspiração da experiência de monitor/Tray.
- Gates: não aplicável; nenhuma implementação, transição ou compatibilidade técnica aprovada por este registro.
- Evidências: declaração explícita do product owner e documentação de visão/inventário atualizada.
- Riscos/ressalvas: não declarar reutilização de código, dependência, afiliação ou compatibilidade com o MySQL Notifier sem evidência própria.
- Aprovador: Bruno, 2026-07-11.

## 2026-07-11 — Transição para Backend Implementation

- Estado anterior: `STATE-03 DATABASE_MODELING`
- Estado solicitado: `STATE-04 BACKEND_IMPLEMENTATION`
- Decisão: `APROVADO` pelo usuário para encerrar a modelagem e avançar após consolidar, na ordem, roadmap universal, SAP HANA/SQLite, conectividade/credenciais e linhagem do produto.
- Escopo: aceitar modelo lógico, separação SQLite/PostgreSQL, constraints, índices, retenção, quatro migrations, rollback e recuperação como baseline do backend.
- Gates: auditoria automática de `STATE-03` `APROVADO`; Human Gate aprovado explicitamente.
- Evidências: `docs/STATE-03-Database-Modeling-Report.md`, `docs/data/`, commits `c0beac1`, `52bd9c8`, `1e76f08` e `7c4626a`.
- Riscos/ressalvas: nenhuma migration aplicada a PostgreSQL/produção; Domain/Application, Provider SDK, vault/mTLS, RBAC, providers, comandos e integração ainda não implementados.
- Aprovador: Bruno, 2026-07-11.
- Estado resultante: `STATE-04 BACKEND_IMPLEMENTATION`.

## 2026-07-11 — Primeiro incremento de Backend Implementation

- Estado anterior: `STATE-04 BACKEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar núcleo provider-neutral, registro aberto e adapter PostgreSQL de readiness como primeira validação concreta.
- Escopo: tipos canônicos de health/erro/credencial, endpoint sem secrets, contracts/registry, probe Application, `pg_isready`, timeout, fallback TCP e integração DI do Agent.
- Gates: incremento automático aprovado por build/format e 30 testes; Human Gate de encerramento de `STATE-04` permanece pendente.
- Evidências: `docs/STATE-04-Backend-Implementation-Report.md`, 26 testes unit/model/provider, 4 testes de arquitetura e build .NET 10 sem avisos/erros.
- Riscos/ressalvas: sem banco real, scheduler, vault, health autenticado, persistência operacional, API, RBAC, comando administrativo, homologação ou UI.

## 2026-07-11 — Segundo incremento de Backend Implementation

- Estado anterior: `STATE-04 BACKEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: adicionar retry/scheduler neutros, resolução temporária de credencial, health PostgreSQL autenticado e persistência SQLite/outbox transacional.
- Escopo: policy limitada, backoff, ciclo isolado, vault port/credential lease, Npgsql TLS + `SELECT 1`, observation/checkpoint/outbox atômicos.
- Gates: incremento automático aprovado por build/format e 40 testes; Human Gate de encerramento de `STATE-04` permanece pendente.
- Evidências: `docs/STATE-04-Backend-Implementation-Report.md`, 36 testes unit/model/provider, 4 testes de arquitetura e rollback SQLite efêmero.
- Riscos/ressalvas: sem scheduler recorrente, vault real, banco/rede real, store initialization, API/RBAC/comandos, homologação ou UI; managed password string do driver exige revisão operacional posterior.

## 2026-07-11 — Terceiro incremento de Backend Implementation

- Estado anterior: `STATE-04 BACKEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar store initialization/assignments, scheduler recorrente, telemetria estruturada e readers reais de vault Windows/Linux.
- Escopo: SQLite migration opt-in, filtering de assignments, BackgroundService, logs estruturados, Credential Manager e Secret Service read-only.
- Gates: incremento automático aprovado por build/format e 46 testes; Human Gate de encerramento de `STATE-04` permanece pendente.
- Evidências: `docs/STATE-04-Backend-Implementation-Report.md`, 42 testes unit/model/provider, 4 testes de arquitetura, initializer SQLite efêmero e composite vault.
- Riscos/ressalvas: monitoring default off; sem banco/rede/credential real, cloud identity, mTLS, dispatch/ingestion, RBAC/comandos, homologação ou UI.

## Template de nova entrada

- Data:
- Estado anterior:
- Estado solicitado:
- Decisão:
- Escopo:
- Gates:
- Evidências:
- Riscos/ressalvas:
- Aprovador:
