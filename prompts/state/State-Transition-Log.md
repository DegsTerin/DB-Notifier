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

## 2026-07-12 — Quarto incremento de Backend Implementation

- Estado anterior: `STATE-04 BACKEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar dispatch/ack do Agent, ingestão central idempotente, eventos/alertas e primeira superfície API autorizada.
- Escopo: batch protocol v1, retry/ack SQLite, HTTPS com certificado cliente, scope Agent/instância, event derivation, alert deliveries e endpoint de observations.
- Gates: incremento automático aprovado por build/format e 56 testes; Human Gate de encerramento de `STATE-04` permanece pendente.
- Evidências: `docs/STATE-04-Backend-Implementation-Report.md`, 52 testes unit/model/provider, 4 testes de arquitetura e smoke local 200/403.
- Riscos/ressalvas: synchronization default off; sem PostgreSQL, certificado/mTLS E2E, vault, notification delivery, RBAC humano, comando real, homologação ou UI.

## 2026-07-12 — Auditoria de certificação do quarto incremento

- Estado anterior: `STATE-04 BACKEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: revisar integralmente dispatch/ack, ingestão idempotente, eventos/alertas e autorização da primeira API antes de aceitar o incremento como tecnicamente fechado.
- Escopo: ordem monotônica, tombstone, retry/backoff, resposta parcial/inválida, timeout, Agent/instance scope, sequência, transação, transições canônicas e autorização positiva/negativa.
- Gates: build/format aprovados em .NET 10 com 0 avisos/erros, 67 testes aprovados, restore locked, auditoria NuGet, Pester 10/10 e smoke local 200/403.
- Evidências: `docs/STATE-04-Backend-Implementation-Report.md`, subset de sincronização 21/21 e correção que impede `2xx` malformado de reconhecer dados terminalmente.
- Riscos/ressalvas: sem PostgreSQL real, certificado/mTLS E2E ou entrega externa de notificação; esses itens continuam explicitamente fora da certificação deste incremento.

## 2026-07-12 — Quinto incremento de Backend Implementation

- Estado anterior: `STATE-04 BACKEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar autenticação humana externa, RBAC server-side, catálogo autorizado, auditoria e criação idempotente de comandos sem executor administrativo.
- Escopo: JWT/OIDC fail-closed, subject ativo, permissions/scopes não expirados, catálogo mínimo, capability/version gate, comando `Pending`, conflito idempotente e auditoria sanitizada.
- Gates: build/format aprovados em .NET 10 com 0 avisos/erros e 75 testes; Human Gate de encerramento de `STATE-04` permanece pendente.
- Evidências: `docs/STATE-04-Backend-Implementation-Report.md`, subset humano/RBAC 8/8, nenhum command attempt/outbox e negação de permissão/capability/expiração testada.
- Riscos/ressalvas: sem IdP/token/MFA real, PostgreSQL, mutations de usuário/papel/catálogo, command delivery/execution/post-probe, homologação ou UI.

## 2026-07-12 — Sexto incremento de Backend Implementation

- Estado anterior: `STATE-04 BACKEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar retenção local/central, server-outbox, delivery durável de notificações e consulta autorizada de auditoria sem canal externo.
- Escopo: batches limitados, dry-run/apply, tombstones, preservação de referências/audit, retry/backoff, IDs deduplicáveis, adapters abertos e `audit.read` Global paginado.
- Gates: build/format aprovados em .NET 10 com 0 avisos/erros e 80 testes; Human Gate de encerramento de `STATE-04` permanece pendente.
- Evidências: `docs/STATE-04-Backend-Implementation-Report.md`, subset maintenance/delivery/audit 13/13, dry-run/aplicação efêmera e workers desabilitados por default.
- Riscos/ressalvas: nenhuma deleção produtiva, legal hold/backup real, publisher/canal externo, PostgreSQL, IdP, command delivery/execution, homologação ou UI.

## 2026-07-12 — Sétimo incremento de Backend Implementation

- Estado anterior: `STATE-04 BACKEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar protocolo mTLS de entrega/ack sem executor e discovery/verificação de pacotes sem carregamento automático.
- Escopo: mensagens v1, sequência durável, compatibilidade exata, expiração, idempotência/replay, inbox local, assinatura RSA-PSS/SHA-256, hashes, limites e paths seguros.
- Gates: build/format aprovados em .NET 10 com 0 avisos/erros e 86 testes; Human Gate de encerramento de `STATE-04` permanece pendente.
- Evidências: `docs/STATE-04-Backend-Implementation-Report.md`, subset command/package 6/6, nenhum command attempt/resultado e nenhum assembly carregado.
- Riscos/ressalvas: sem mTLS E2E real, ativação sandbox de package, execução/post-probe, PostgreSQL, certificado/chave de produção, homologação ou UI.

## 2026-07-12 — Auditoria automática de encerramento de Backend Implementation

- Estado anterior: `STATE-04 BACKEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: `REPROVADO`; os sete incrementos permanecem evidência válida, mas o conjunto M4 aceito está incompleto.
- Escopo: entregáveis/lifecycle, arquitetura, provider PostgreSQL, migração legada, autorização, idempotência, persistência, secrets, dependências, runtime seguro e documentação.
- Gates: .NET 10 build/format e 86 testes aprovados; Dashboard/Pester/bundle/dependency scans aprovados; gate de completude reprovado.
- Evidências: `docs/STATE-04-Backend-Implementation-Audit.md`; ausência verificável do migrador de configuração, discovery caracterizado parcial e fixtures negativas incompletas.
- Riscos/ressalvas: Human Gate não solicitado; nenhum banco, credential, certificado, IdP, canal, executor administrativo, deploy ou migration remota foi exercitado.

## 2026-07-12 — Remediação e reauditoria de Backend Implementation

- Estado anterior: `STATE-04 BACKEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar bloqueadores M4 e reexecutar auditoria automática; resultado `APROVADO`, Human Gate ainda `PENDENTE`.
- Escopo: migrador PgNotifier → DB-Notifier isolado, dry-run/backup/atomicidade/idempotência/rollback, discovery PostgreSQL tipado, fallback/credencial expirada, fixtures e matriz factual.
- Gates: .NET 10 build/format aprovados com 0 avisos/erros, 104 testes, Dashboard/Pester/bundle/dependency scans e CLI smoke aprovados.
- Evidências: `docs/STATE-04-Backend-Implementation-Reaudit.md`; `Applied` → `AlreadyCurrent` → `RolledBack`, núcleo sem nome de engine e PostgreSQL ainda não homologado.
- Riscos/ressalvas: sem banco/credential/certificado/IdP/canal real, command executor/post-probe, provider loading, deploy, migration remota, homologação ou UI funcional.

## 2026-07-12 — Transição para Frontend Implementation

- Estado anterior: `STATE-04 BACKEND_IMPLEMENTATION`
- Estado solicitado: `STATE-05 FRONTEND_IMPLEMENTATION`
- Decisão: `APROVADO` pelo usuário para encerrar o backend após revisão explícita da reauditoria e de suas limitações.
- Escopo: aceitar o núcleo provider-neutral, Provider SDK aberto, persistência, APIs autorizadas, RBAC/auditoria, entrega sem execução, retenção/delivery, verificação de pacotes e migrador seguro como baseline para Tray/Desktop e Dashboard.
- Gates: reauditoria automática de `STATE-04` `APROVADO`; Human Gate aprovado explicitamente.
- Evidências: `docs/STATE-04-Backend-Implementation-Reaudit.md`, 104 testes .NET, fixtures representativas de falha de provider e autorização negativa, smoke sanitizado do migrador e resposta do usuário na sessão.
- Riscos/ressalvas: as amostras aceitas são determinísticas e não substituem banco/credential/certificado/IdP/canal real; integração externa, ativação de provider, execução/post-probe de comandos e homologação permanecem não autorizadas nesta transição.
- Aprovador: Bruno, 2026-07-12.
- Estado resultante: `STATE-05 FRONTEND_IMPLEMENTATION`.

## 2026-07-12 — Primeiro incremento de Frontend Implementation

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar fundação visual e vertical slice provider-neutral de inventário/status somente leitura no Dashboard e WPF.
- Escopo: contrato `inventory.v1`, adapters determinísticos, resumo/status/stale, busca/filtro, timestamps, suporte factual e estados ready/loading/empty/offline/error/denied.
- Gates: builds .NET 10/Vite, typecheck, format, 107 testes .NET, 3 testes Dashboard, npm audit, smoke da janela WPF e amostras visuais desktop/compacta aprovados; Human Gate de encerramento de `STATE-05` permanece pendente.
- Evidências: `docs/STATE-05-Frontend-Implementation-Report.md`; nenhuma conexão externa, credential, mutation, canal, comando administrativo ou provider homologado.
- Riscos/ressalvas: leitor de tela, contraste automatizado, zoom, WPF visual e matriz completa de viewports permanecem para os próximos incrementos/gate da fase.

## 2026-07-12 — Segundo incremento de Frontend Implementation

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar histórico/timeline e alertas provider-neutral somente leitura no Dashboard e WPF.
- Escopo: contrato `history-alerts.v1`, busca/filtro, severidade acessível, estados de alerta, timestamps, manutenção e adapters determinísticos.
- Gates: builds .NET 10/Vite, 109 testes .NET, 4 testes Dashboard, npm audit, smoke WPF e amostras visuais desktop/compacta aprovados; Human Gate de `STATE-05` permanece pendente.
- Evidências: `docs/STATE-05-Frontend-Implementation-Report.md`; nenhuma mutation, entrega externa, comando administrativo, conexão real ou homologação.
- Riscos/ressalvas: acknowledge/silence são somente estados demonstrativos; leitor de tela, contraste automatizado, zoom e WPF visual permanecem pendentes.

## 2026-07-12 — Terceiro incremento de Frontend Implementation

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar configuração provider-neutral e preview capability-aware sem persistência ou execução.
- Escopo: contrato `configuration-capabilities.v1`, campos não secretos, estados confirmation/denied/unsupported/unavailable/unknown e confirmação demonstrativa desabilitada.
- Gates: builds .NET 10/Vite, 114 testes .NET, 5 testes Dashboard, smoke WPF e amostras visuais desktop/compacta aprovados; Human Gate de `STATE-05` permanece pendente.
- Evidências: `docs/STATE-05-Frontend-Implementation-Report.md`; PostgreSQL Start/Stop/Restart permanecem `Unsupported` e nenhum comando foi criado/despachado.
- Riscos/ressalvas: confirmação é exemplo de UX, não capability suportada; acessibilidade completa, Tray e integração permanecem pendentes.

## 2026-07-12 — Quarto incremento de Frontend Implementation

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar Tray/notification area seguro e reforçar evidências automatizadas de acessibilidade.
- Escopo: abrir/ocultar/sair do WPF, status local factual, descarte de recursos, contraste WCAG AA, semântica, foco e reduced-motion.
- Gates: 125 testes .NET, 7 testes Dashboard, builds .NET 10/Vite e smoke close-to-Tray aprovados; Human Gate de `STATE-05` permanece pendente.
- Evidências: `docs/STATE-05-Frontend-Implementation-Report.md`; fechar ocultou a janela e manteve apenas o processo DB-Notifier vivo até o cleanup de validação.
- Riscos/ressalvas: ícone Tray ainda usa asset genérico do sistema; leitor de tela, zoom e amostras humanas completas permanecem para auditoria/Human Gate.

## 2026-07-12 — Auditoria automática de encerramento de Frontend Implementation

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: `REPROVADO`; Human Gate não aberto.
- Escopo: Chrome/CDP em nove amostras de viewport/rota, teclado, árvore acessível, estados/reduced-motion, diálogo, WPF/UI Automation, builds, testes, dependências e limites de segurança.
- Gates: 125 testes .NET, 7 testes Dashboard e 10 Pester aprovados; build/format/bundle/npm audit aprovados; layout mobile e teclado do diálogo reprovados.
- Evidências: `docs/STATE-05-Frontend-Implementation-Audit.md`, `scripts/audit-state05-dashboard.mjs`, `scripts/audit-state05-wpf.ps1`; JSON/PNG sanitizados temporários fora do Git.
- Riscos/ressalvas: overflow global em 390/320 px e `aria-modal` sem entrada/contenção/restauração de foco ou Escape; amostra humana de leitor de tela pendente.

## 2026-07-12 — Remediação e reauditoria de Frontend Implementation

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: reauditoria automática `APROVADA`; Human Gate permanece pendente.
- Escopo: contenção intrínseca mobile, diálogo nativo, foco inicial, Tab/Shift+Tab, Escape, restauração, regressões e repetição completa dos gates.
- Gates: 125 testes .NET, 8 testes Dashboard, 10 Pester, build/format/bundle/npm audit, nove amostras Chrome e WPF/UI Automation aprovados.
- Evidências: `docs/STATE-05-Frontend-Implementation-Reaudit.md`; 390 px `375/375`, 320 px `305/305`, modal contido e foco restaurado.
- Riscos/ressalvas: leitor de tela e zoom nativo permanecem amostras humanas; nenhuma integração, execução administrativa ou homologação foi autorizada.

## 2026-07-12 — Formalização do Design System

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: formalizar antes do Human Gate a identidade moderna empresarial e os temas Light/Dark/System com paridade React/WPF.
- Escopo: arquitetura de tokens, paletas semânticas, tipografia, espaçamento, elevação, motion, componentes, ícones, responsividade, WCAG 2.2 AA, persistência, validação e governança.
- Gates: especificação oficial `1.0.0` com valores contrastados e critérios objetivos de implementação/reauditoria; nenhuma alteração visual funcional neste incremento documental.
- Evidências: `docs/design/DB-Notifier-Design-System.md`.
- Riscos/ressalvas: implementação, testes de tema e nova reauditoria ainda pendentes; Human Gate, `STATE-06` e laboratório multi-banco permanecem bloqueados.

## 2026-07-12 — Primeiro incremento do Design System

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar fonte canônica e contratos antes de migrar visualmente React/WPF.
- Escopo: schema `dbnotifier.design-tokens.v1`, core/semantic/component tokens, gerador/verificador CSS/XAML e contratos System/Light/Dark TypeScript/.NET 10.
- Gates: geração/drift, paridade Light/Dark, contraste canônico, build .NET 10, 133 testes .NET, 12 Dashboard e documentação aprovados.
- Evidências: `docs/STATE-05-Design-System-Implementation-Report.md`; saídas CSS/XAML geradas deterministicamente.
- Riscos/ressalvas: telas ainda usam estilos atuais; ThemeSelector, bootstrap, observadores e persistência runtime permanecem pendentes.

## 2026-07-13 — Segundo incremento do Design System

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: integrar o ciclo completo System/Light/Dark e tokens canônicos no Dashboard React, preservando os adapters determinísticos.
- Escopo: bootstrap pré-render, persistência resiliente versionada, observação `matchMedia`, sincronização entre abas, ThemeSelector acessível e migração dos estilos manuais para tokens gerados.
- Gates: drift de tokens, typecheck, 14 testes Dashboard, build Vite, documentação de 148 fontes, npm/NuGet sem vulnerabilidades, build .NET 10 sem warnings/errors, 133 testes .NET, format, 10 Pester e bundle aprovados.
- Evidências: `docs/STATE-05-Design-System-Implementation-Report.md`; amostras Chrome Light 1440 e Dark em viewport exato de 390 CSS px aprovadas, com `clientWidth = scrollWidth = 390`.
- Riscos/ressalvas: WPF ainda não consome o ciclo de tema runtime nem migrou recursos manuais; reauditoria automática completa e Human Gate permanecem pendentes.

## 2026-07-13 — Incremento bilíngue de localização da interface

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar `pt-BR` e `en-GB` no Dashboard React e Desktop/Tray WPF com `pt-BR` como fallback seguro.
- Escopo: catálogos XML canônicos, geração TypeScript/XAML, seletores persistentes, recursos dinâmicos, fixtures localizadas e matriz responsiva bilíngue.
- Gates: geração/drift, testes/builds, documentação, dependências e auditorias runtime bilíngues aplicáveis; Human Gate permanece pendente.
- Evidências: `docs/STATE-05-Localisation-Implementation-Report.md`; nenhuma integração externa ou capability administrativa foi habilitada.
- Riscos/ressalvas: amostras humanas bilíngues de leitor de tela e zoom/scaling permanecem pendentes; tema WPF completo continua em incremento separado.

## 2026-07-13 — Terceiro incremento do Design System e TopBar de preferências

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: concluir o ciclo Light/Dark/System no WPF e padronizar idioma/tema como botões discretos no canto superior direito do TopBar das duas interfaces.
- Escopo: preferência WPF unificada e atômica, troca de dicionário semântico, observação do tema Windows, precedência de High Contrast, migração dos recursos visuais WPF e grupos acessíveis `pt-BR`/`en-GB`/System/Light/Dark em React e WPF.
- Gates: 19 testes Dashboard, 136 testes .NET, typecheck/builds e matriz automática de 54 amostras browser aprovada sem overflow global ou controlo interativo sem nome; seis combinações WPF e mínimo `820×620` amostrados.
- Evidências: `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Localisation-Implementation-Report.md` e artefactos temporários sanitizados sob `%TEMP%\DBNotifier-State05-Audit\<locale>\<theme>`.
- Riscos/ressalvas: leitor de tela, zoom/scaling nativo, High Contrast visual e revisão humana continuam pendentes; Human Gate, `STATE-06`, integração externa e laboratório permanecem bloqueados.

## 2026-07-13 — Preparação do Human Gate de STATE-05

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: manter o Human Gate `PENDENTE`; nenhuma amostra ou aprovação humana foi inferida.
- Escopo: protocolo bilíngue para teclado, Narrator, zoom nativo, Windows scaling, System/High Contrast, Tray, revisão visual e verdade operacional em Dashboard/WPF.
- Gates: preflight observou Windows 11, Chrome 150, .NET 10.0.301, Narrator disponível, escala atual 100% e High Contrast desligado; resultados humanos continuam não executados.
- Evidências: `docs/STATE-05-Human-Gate-Validation.md` com nove amostras `PENDENTE` e campos de validador/decisão não preenchidos.
- Riscos/ressalvas: NVDA não está disponível neste ambiente; mudanças de escala/High Contrast exigem operação interativa e restauração pelo validador; `STATE-06` permanece bloqueado.

## 2026-07-13 — Achado humano visual S05-HG-001

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: reprovar a porção visual de `HG05-01` e interromper as demais amostras até remediação.
- Escopo: revisão humana do Dashboard local `pt-BR` no início do Human Gate; teclado, Narrator, zoom, High Contrast e WPF não foram testados nesta execução.
- Gates: `S05-HG-001` classificado como bloqueador porque a interface foi explicitamente julgada não moderna e não empresarial, em conflito com o Design System oficial.
- Evidências: feedback explícito e screenshot sanitizada fornecida pelo utilizador na sessão; registro factual em `docs/STATE-05-Human-Gate-Validation.md` sem incorporar a imagem ao Git.
- Riscos/ressalvas: Human Gate continua `PENDENTE` e bloqueado; remediação visual e repetição dos gates automáticos afetados são obrigatórias antes de reiniciar `HG05-01`.

## 2026-07-13 — Remediação visual e reauditoria automática de S05-HG-001

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: considerar a implementação e a reauditoria automática da remediação concluídas, mantendo o achado aberto até nova validação humana.
- Escopo: Design System `1.3.0`, tokens de chrome, shell React/WPF, seletores discretos, hierarquia de superfícies, navegação responsiva, cartões de dados compactos e seleção do DataGrid.
- Gates: 60 amostras browser aprovadas em dois locales, três preferências e dez viewport/rotas; sete amostras WPF aprovadas; 19 testes Dashboard, 136 .NET, 10 Pester, builds, format, drift, documentação, bundle e auditorias de dependência aprovados.
- Evidências: `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md` e JSON/PNG sanitizados temporários fora do Git, incluindo a largura `960×1040` que reproduzia o problema.
- Riscos/ressalvas: a reauditoria automática não substitui a reprovação humana original; `HG05-01`, Narrator, zoom, scaling, High Contrast e o Human Gate completo permanecem pendentes.

## 2026-07-13 — Segunda remediação visual internacional de S05-HG-001

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: preservar a primeira remediação como melhoria parcial e executar novo refinamento antes de repetir a decisão visual humana.
- Escopo: revisão de padrões oficiais Carbon/Grafana/Fluent, ícones SVG próprios, faixa consolidada de KPIs, navegação selecionada contida, preferências de menor ênfase e ajuste cromático equivalente no WPF.
- Gates: 60 amostras browser repetidas sem overflow global, controle interativo sem nome ou regressão modal; WPF `pt-BR` Light/Dark representativo com 0 controles focalizáveis sem nome; 20 testes Dashboard e builds aplicáveis aprovados.
- Evidências: `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md` e JSON/PNG sanitizados temporários fora do Git.
- Riscos/ressalvas: a avaliação “ficou melhor” não foi interpretada como aprovação; a segunda revisão visual, Narrator e o Human Gate completo permanecem pendentes.

## 2026-07-13 — Aprovação visual e ícone canônico de banco de dados

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: registrar a aprovação humana explícita da segunda remediação somente na porção visual e implementar o pedido subsequente de um ícone simples de banco de dados em todo o produto ativo.
- Escopo: Design System `1.3.2`, gerador SVG/ICO provider-neutral, favicon/cabeçalho Web, cabeçalho/janela/executável/Tray WPF, atalhos e instalador; protótipos PostgreSQL legados não foram alterados.
- Gates: geração e drift do ativo aprovados; 21 testes Dashboard, typecheck/build Web e build WPF Release com 0 avisos/erros aprovados; amostra WPF `pt-BR` Light iniciou com o novo ícone, 37 controles focalizáveis e 0 sem nome.
- Evidências: `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md` e PNG/JSON WPF sanitizados temporários fora do Git.
- Riscos/ressalvas: o Human Gate permanece pendente; teclado, Narrator, zoom, scaling, High Contrast, demais amostras e confirmação visual do ícone não foram inferidos.
- Aprovador: usuário, por resposta explícita “Sim!” à pergunta de aprovação visual em 2026-07-13.

## 2026-07-13 — Remediação do ícone canônico S05-HG-002

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: preservar a aprovação humana da hierarquia do shell, registrar a reprovação específica do primeiro ícone e substituí-lo antes de continuar o Human Gate.
- Escopo: Design System `1.3.3`, gerador SVG/ICO e cache-busting Web; o cilindro branco preenchido foi substituído por contorno branco arredondado em todas as superfícies já integradas.
- Gates: drift SVG/ICO, 21 testes Dashboard, typecheck/build Web, build/testes .NET e amostra visual proporcional aplicáveis à mesma família de ativos.
- Evidências: `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md` e PNG sanitizado temporário fora do Git.
- Riscos/ressalvas: `S05-HG-002`, confirmação humana do ícone, Narrator e o Human Gate completo permanecem pendentes.
- Aprovador: PENDENTE para o ícone substituto.

## 2026-07-13 — Remediação fullscreen e nome visual S05-HG-003

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: corrigir a faixa inativa observada à direita em tela cheia, padronizar o nome visual solicitado e registrar separadamente a inexistência de modo TV dedicado.
- Escopo: Design System `1.3.4`, shell Web sem limite fixo de largura, display name `DB Notifier` em Web/WPF/Tray/instalador, preservação de caminhos técnicos e nova amostra `1920×1080`.
- Gates: 66 amostras browser aprovadas em dois locales, três preferências e onze viewport/rotas, com gap principal zero e inset de conteúdo de 48 px em ultrawide; 21 testes Dashboard, typecheck/build Web, build .NET 10 sem avisos/erros, 136 testes .NET e amostra WPF com 37 controles focalizáveis e 0 sem nome aprovados.
- Evidências: `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md` e JSON/PNG sanitizados temporários fora do Git.
- Riscos/ressalvas: não há modo TV/wallboard/kiosk implementado; responsividade ultrawide não equivale a esse suporte. `S05-HG-002`, `S05-HG-003`, Narrator e o Human Gate completo permanecem pendentes de validação humana.
- Aprovador: PENDENTE para a correção visível e o nome visual.

## 2026-07-13 — Controles compactos de preferência S05-HG-004

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: substituir os grupos textuais expandidos por um botão de idioma e um botão de tema com iconografia code-native equivalente no React e WPF.
- Escopo: Design System `1.3.5`, ícone genérico de idioma, ícones Sistema/Sol/Lua, ciclos limitados `pt-BR` ↔ `en-GB` e System → Light → Dark, nomes acessíveis/tooltips localizados e persistência existente.
- Gates: 66 amostras browser e seis combinações WPF aprovadas, com os dois ciclos completos/restaurados em cada combinação de locale/tema, sem overflow global nem controles sem nome; cada WPF expôs 34 controles focalizáveis e 0 sem nome; 21 testes Dashboard e builds Web/.NET 10 aplicáveis aprovados.
- Evidências: `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md` e JSON/PNG sanitizados temporários fora do Git.
- Riscos/ressalvas: a reauditoria automática não constitui aprovação visual; `S05-HG-002`, `S05-HG-003`, `S05-HG-004`, Narrator e o Human Gate completo permanecem pendentes.
- Aprovador: PENDENTE para os controles compactos.

## 2026-07-13 — Modo TV do Dashboard S05-HG-005

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: implementar modo TV session-only no Dashboard Web com um controle ampliar/desampliar e integração opcional ao Fullscreen nativo.
- Escopo: Design System `1.4.0`, inventário ready sem filtros, shell sem navegação/cenário, densidade para distância, relógio UTC/freshness contínuos, saída persistente e verdade de demonstração preservada.
- Gates: 22 testes Dashboard, typecheck/build Web e 72 amostras browser aprovados; as seis amostras TV `1920×1080` entraram em Fullscreen, mantiveram badge/disclaimer e saída, exibiram a tabela, restauraram o shell e apresentaram 0 overflow/controles sem nome. Fallback sem Fullscreen também aprovado com anúncio e saída preservados.
- Evidências: `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md` e JSON/PNG sanitizados temporários fora do Git.
- Riscos/ressalvas: atualização visual/freshness sobre snapshot de demonstração não comprova feed externo em tempo real; API/SignalR, reconexão e saúde da fonte pertencem a `STATE-06`. WPF TV, app móvel nativo, kiosk unattended e burn-in não foram implementados.
- Aprovador: PENDENTE para `HG05-10`; Human Gate completo permanece pendente.

## 2026-07-13 — Requisito de atualização real do modo TV atribuído ao STATE-06

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: documentar para `STATE-06 INTEGRATION` a leitura autorizada imediata da API ao entrar no modo TV e uma nova leitura autoritativa a cada 30 segundos enquanto o modo estiver ativo.
- Escopo: requisições sem sobreposição; hints autenticados do SignalR podem antecipar uma leitura, mas não substituir a reconciliação periódica; falhas preservam o último snapshot e timestamps e apresentam erro/offline/stale factual.
- Gates futuros: relógio controlado e E2E em sandbox devem provar a cadência, ausência de concorrência, hint/reconciliação, desconexão e recuperação antes do aceite de `STATE-06`.
- Evidências: `prompts/foundation/Solution-Architecture-Document.md`, `prompts/governance/Lifecycle.md`, `prompts/governance/Quality-Gates.md`, `docs/Legacy-Migration-Plan.md` e Design System `1.4.1`.
- Riscos/ressalvas: nenhuma integração ou atualização real foi implementada em `STATE-05`; o modo TV continua usando snapshot de demonstração até a fase autorizada.
- Origem: solicitação explícita do product owner; não constitui aprovação do Human Gate de `STATE-05`.

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
