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

## 2026-07-13 — Símbolo de idioma e tema Light/Dark S05-HG-006

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: substituir o globo ambíguo por símbolo code-native de tradução/idiomas e remover a preferência System, mantendo somente Light/Dark no Dashboard e WPF.
- Escopo: ciclos `pt-BR` ↔ `en-GB` e Light ↔ Dark, Sol/Lua, migração segura de System/valor inválido para Light, persistência existente e High Contrast independente.
- Gates: 22 testes Dashboard, 133 testes .NET e builds Web/.NET aprovados; 44 amostras browser padrão e quatro TV cobriram as quatro combinações correntes sem overflow/controle sem nome; quatro amostras WPF restauraram os ciclos com 34 controles focalizáveis e nenhum sem nome; Chrome focado sem erros de console.
- Evidências: Design System `2.0.0`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md` e artefactos temporários sanitizados fora do Git.
- Riscos/ressalvas: preferências System antigas passam a Light; High Contrast não foi removido; a reauditoria automática não constitui aprovação visual humana.
- Aprovador: PENDENTE para clareza visual em `S05-HG-006`; Human Gate completo permanece pendente.

## 2026-07-13 — Flyout operacional do Tray inspirado no PgNotifier

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`
- Estado resultante: sem transição
- Decisão: adotar a leitura rápida e os atalhos do panfleto legado como referência de experiência, preservando identidade DB Notifier, neutralidade de provider e verdade de capability.
- Escopo: Design System `2.1.0`, flyout WPF bilíngue/temático, quatro instâncias demonstrativas, estados não dependentes apenas de cor, atalhos locais para Inventário/Histórico/Configuração e Restart explicitamente não interativo.
- Gates: build Release .NET 10 sem avisos/erros, 133 testes .NET, 23 testes Dashboard, geração/drift bilíngue e gate documental de 164 fontes aprovados; quatro amostras WPF principais mantiveram 34 controles focalizáveis e nenhum sem nome. A ativação visual do flyout não foi inferida do host automático e permanece no Human Gate.
- Evidências: `docs/STATE-05-Frontend-Implementation-Report.md`, Design System e artefactos temporários sanitizados quando produzidos.
- Riscos/ressalvas: nenhuma arte PostgreSQL, consulta externa, notificação por mudança ou operação de serviço foi adicionada; integração factual pertence ao `STATE-06` e controlo depende de implementação e homologação exata no `STATE-07`.
- Aprovador: PENDENTE para a revisão humana do novo flyout; a solicitação de implementação não aprova o Human Gate completo.

## 2026-07-13 — Contestação e início da ratificação retrospectiva dos Human Gates

- Estado anterior: posição técnica `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate de `STATE-05` pendente.
- Estado resultante: sem transição; progressão de lifecycle `EM ESPERA`.
- Decisão: preservar os relatórios e transições históricas, mas retirar sua autoridade corrente até ratificação independente de `STATE-00` a `STATE-04`.
- Motivo: o validador declarou não ter certeza de que os Human Gates anteriores foram aprovados corretamente e informou que havia apenas respondido “Aprovado”; em seguida autorizou a correção das divergências e a regularização completa.
- Escopo: regra contra aprovação curta/ambígua, template de ratificação, pacote retrospectivo por estado, sincronização de estado/README/playbook/Human Gate e reforço de CI.
- Integridade Git: referência interna Codex com caminho excessivamente longo foi copiada e validada em `%TEMP%`, removida pontualmente e `git show-ref`/`git fsck --full` retornaram código 0; nenhum commit, branch ou tag de produto foi removido.
- Gates humanos: `STATE-00`, `STATE-01`, `STATE-02`, `STATE-03`, `STATE-04` e `STATE-05` permanecem `PENDENTE` até decisões inequívocas separadas.
- Evidências: `docs/Human-Gate-Retrospective-Ratification.md`, `prompts/governance/Quality-Gates.md`, `prompts/state/Current-State.md` e relatórios históricos originais.
- Riscos/ressalvas: evidência técnica continua válida em seu escopo, mas não substitui amostras humanas; `STATE-06`, laboratório, homologação e release permanecem bloqueados.
- Aprovador: Bruno autorizou o processo de regularização, não a aprovação dos gates individuais.

## 2026-07-13 — Regularização automática e reauditoria final de STATE-05

- Estado anterior: posição técnica `STATE-05 FRONTEND_IMPLEMENTATION`, progressão `EM ESPERA`.
- Estado resultante: sem transição; progressão permanece `EM ESPERA`.
- Decisão: fechar as divergências reproduzíveis de CI e emitir a reauditoria automática corrente do Design System `2.1.0`/Tray sem inferir decisões humanas.
- Escopo: gates CI de format, vulnerabilidades NuGet/npm, bundle legado, links Markdown, smoke fail-closed, integridade Git e matriz Dashboard headless bilíngue Light/Dark.
- Gates: 133 testes .NET, 23 Dashboard, 10 Pester, builds/formato/drift/bundle aprovados; 93 links em 58 Markdown; 44 amostras Web; API `200/401/403`; Agent desabilitado por padrão; auditorias de dependência sem vulnerabilidades conhecidas.
- Evidências: `docs/STATE-05-Frontend-Implementation-Final-Reaudit.md`, scripts de auditoria em `scripts/` e `.github/workflows/ci.yml`.
- Riscos/ressalvas: XML completo de APIs públicas preexistentes continua incremental; WPF visível, Narrator, zoom/scaling, High Contrast, TV e Tray permanecem amostras humanas; sistemas externos não foram contactados.
- Aprovador: resultado automático; Human Gates `STATE-00` a `STATE-05` permanecem `PENDENTE`.

## 2026-07-13 — Ratificação retrospectiva de STATE-00 DISCOVERY_MIGRATION

- Estado anterior: posição técnica `STATE-05 FRONTEND_IMPLEMENTATION`, progressão `EM ESPERA`; `STATE-00` a `STATE-04` aguardavam ratificação.
- Estado resultante: sem transição; `STATE-00` ratificado como `APROVADO`, `STATE-01` a `STATE-05` permanecem pendentes e a progressão continua `EM ESPERA`.
- Decisão: o validador Bruno aprovou retrospectivamente somente `STATE-00 DISCOVERY_MIGRATION` após revisar inventário legado/protótipos, semântica degradada de TCP, ausência de controle real e estratégia incremental com rollback.
- Ressalvas: a decisão vale apenas para discovery e planejamento; não valida ambiente real nem substitui integração, infraestrutura, homologação de provider ou testes operacionais futuros.
- Evidências: confirmação inequívoca recebida no Human Gate e registrada em `docs/Human-Gate-Retrospective-Ratification.md`; baseline automático corrente permanece aprovado em seu escopo.
- Riscos/limites: nenhuma ação externa, banco, serviço, deploy ou infraestrutura foi executada por esta ratificação.
- Aprovador: Bruno — `Ratifico a decisão acima exclusivamente para STATE-00 DISCOVERY_MIGRATION.`

## 2026-07-13 — Ratificação retrospectiva de STATE-01 PROJECT_SETUP

- Estado anterior: posição técnica `STATE-05 FRONTEND_IMPLEMENTATION`, progressão `EM ESPERA`; `STATE-00` ratificado e `STATE-01` a `STATE-04` pendentes.
- Estado resultante: sem transição; `STATE-01` ratificado como `APROVADO COM RESSALVAS`, `STATE-02` a `STATE-05` permanecem pendentes e a progressão continua `EM ESPERA`.
- Decisão: o validador Bruno aprovou retrospectivamente somente `STATE-01 PROJECT_SETUP` após revisar estrutura/fronteiras, baseline .NET 10, compatibilidade legada, ausência de comportamento provider prematuro/deploy e falta de prova remota da CI.
- Amostra humana: onboarding `NÃO REPETIDO`; o validador não executou restore, build, testes ou validação e não confirmou independentemente os resultados automáticos registrados.
- Ressalvas: decisão baseada exclusivamente na documentação/evidência apresentada; onboarding não repetido; CI remota sem comprovação independente.
- Evidências: confirmação inequívoca recebida e registrada em `docs/Human-Gate-Retrospective-Ratification.md`; baseline automático corrente permanece válido apenas em seu escopo local.
- Riscos/limites: nenhuma ação externa, banco, serviço, deploy ou infraestrutura foi executada por esta ratificação.
- Aprovador: Bruno — `Ratifico a decisão acima exclusivamente para STATE-01 PROJECT_SETUP.`

## 2026-07-13 — Ratificação retrospectiva de STATE-02 ARCHITECTURE

- Estado anterior: posição técnica `STATE-05 FRONTEND_IMPLEMENTATION`, progressão `EM ESPERA`; `STATE-00`/`STATE-01` ratificados e `STATE-02` a `STATE-04` pendentes.
- Estado resultante: sem transição; `STATE-02` ratificado como `APROVADO COM RESSALVAS`, `STATE-03` a `STATE-05` permanecem pendentes e a progressão continua `EM ESPERA`.
- Decisão: Bruno aceitou individualmente ADR-0001/2/3/4/6 e aceitou ADR-0005 com ressalvas; revisou standalone/offline, híbrido outbound-only, separação de credenciais, replay/timeout, update/rollback e AIOps `OBSERVER`.
- Amostras não repetidas: nenhuma execução operacional, penetration test, homologação de provider, update real ou validação de infraestrutura.
- Ressalvas: sem homologação operacional; pipeline MSI/assinatura/update futuro; provisionamento/rotação/mTLS ainda exigem validação; nenhuma capability/provider foi autorizada para controle administrativo real.
- Evidências: confirmação inequívoca e decisões individuais registradas em `docs/Human-Gate-Retrospective-Ratification.md`; pacote ativo sincronizado em `docs/architecture/README.md`.
- Riscos/limites: arquitetura aceita não constitui implementação, release, segurança ofensiva, homologação ou autorização externa.
- Aprovador: Bruno — `Ratifico a decisão acima exclusivamente para STATE-02 ARCHITECTURE.`

## 2026-07-13 — Ratificação retrospectiva de STATE-03 DATABASE_MODELING

- Estado anterior: posição técnica `STATE-05 FRONTEND_IMPLEMENTATION`, progressão `EM ESPERA`; `STATE-00` a `STATE-02` ratificados e `STATE-03`/`STATE-04` pendentes.
- Estado resultante: sem transição; `STATE-03` ratificado como `APROVADO COM RESSALVAS`, `STATE-04`/`STATE-05` permanecem pendentes e a progressão continua `EM ESPERA`.
- Decisão: Bruno aprovou propriedade de stores, modelo/invariantes e trigger; aprovou migrations Up/Down e retenção/rollback com ressalvas.
- Amostras não repetidas: PostgreSQL real, migration produtiva, backup/PITR, restore operacional completo, legal hold, exclusão produtiva e rollback real.
- Ressalvas: aprovação limitada ao modelo, migrations revisadas e testes não produtivos; produção depende de futuros gates de release, backup, autorização e recuperação; PostgreSQL permaneceu offline/não homologado; retenção não autoriza exclusão.
- Evidências: confirmação inequívoca e revisão detalhada registradas em `docs/Human-Gate-Retrospective-Ratification.md`; autoridade ativa sincronizada em `docs/data/README.md`.
- Riscos/limites: nenhuma migration real, exclusão, backup, restore ou mutação externa foi executada por esta ratificação.
- Aprovador: Bruno — `Ratifico a decisão acima exclusivamente para STATE-03 DATABASE_MODELING.`

## 2026-07-13 — Ratificação retrospectiva de STATE-04 BACKEND_IMPLEMENTATION

- Estado anterior: posição técnica `STATE-05 FRONTEND_IMPLEMENTATION`, progressão `EM ESPERA`; `STATE-00` a `STATE-03` ratificados e `STATE-04` pendente.
- Estado resultante: sem transição; `STATE-04` ratificado como `APROVADO`, retrospectiva `STATE-00` a `STATE-04` concluída, `STATE-05` Human Gate pendente e progressão `EM ESPERA`.
- Decisão: Bruno aprovou auditoria/correções, provider/estados, autorização humana/Agent, idempotência/entrega, ConfigMigrator e verdade de capabilities/suporte no escopo determinístico apresentado.
- Amostras não repetidas: PostgreSQL/certificados/IdP/vault/notificações reais, execução administrativa, integrações externas e homologação/infraestrutura reais.
- Ressalvas: PostgreSQL homologation `None`/public support `No`; capabilities administrativas `Unsupported`; integração, identidades externas, vault, notificações e execução permanecem para fases futuras autorizadas; decisão limitada ao `STATE-04` evidenciado.
- Evidências: confirmação inequívoca e revisão detalhada registradas em `docs/Human-Gate-Retrospective-Ratification.md`; estado e addendum de `STATE-05` sincronizados.
- Riscos/limites: nenhuma conexão real, credencial, certificado, serviço, comando administrativo, deploy ou mutação externa foi executada por esta ratificação.
- Aprovador: Bruno — `Ratifico a decisão acima exclusivamente para STATE-04 BACKEND_IMPLEMENTATION.`

## 2026-07-13 — Correção de hora local, controles TV e TopBar mobile

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente.
- Estado resultante: sem transição; `STATE-05` e seu Human Gate permanecem pendentes.
- Decisão: corrigir três divergências observadas na revisão humana do Dashboard e tornar nova guia dedicada o padrão de futuras amostras Chrome autorizadas.
- Escopo: Design System `2.1.1`, formatter system-local com zona explícita, preservação de ISO/UTC e freshness, idioma/tema visíveis no modo TV, TopBar mobile em uma linha com alvos `44×44`, regressões e protocolo de revisão em nova guia.
- Gates: 27 testes Dashboard, typecheck e build Vite aprovados; matriz headless de 44 viewports aprovada em `pt-BR`/`en-GB` e Light/Dark. Amostras `320×568`/`390×844` sem overflow e com TopBar contido; quatro amostras TV com idioma/tema visíveis, Fullscreen/restauração e fuso do sistema evidenciado.
- Evidências: código React/CSS e teste de hora determinístico; `docs/design/DB-Notifier-Design-System.md`; finding `S05-HG-007` em `docs/STATE-05-Human-Gate-Validation.md`; relatórios sanitizados temporários em `%TEMP%`.
- Riscos/ressalvas: resultado automático não constitui aprovação visual; nenhuma interface visível, WPF, Narrator, High Contrast ou scaling foi aberta/alterada durante a correção. Snapshot continua demonstrativo e integração real permanece em `STATE-06`.
- Aprovador: PENDENTE para a revalidação visual dos três pontos; nenhuma decisão de Human Gate foi inferida.

## 2026-07-13 — Esclarecimento de janela independente para revisão Chrome

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente.
- Estado resultante: sem transição; `STATE-05` e seu Human Gate permanecem pendentes.
- Decisão: corrigir a interpretação anterior de “nova guia dedicada”; amostras visíveis autorizadas do Dashboard devem abrir em uma nova janela independente do Chrome.
- Escopo: regra permanente em `AGENTS.md`, protocolo do Human Gate e changelog do corpus `3.32.3`; nenhuma mudança funcional no Dashboard.
- Evidência: esclarecimento explícito do validador acompanhado por capturas que diferenciam uma guia na janela existente de uma janela Chrome separada.
- Riscos/ressalvas: o controle do navegador não deve navegar, reorganizar ou fechar janelas/guias preexistentes; a janela de revisão continua sujeita a autorização interativa específica.
- Aprovador: Bruno esclareceu o comportamento esperado; isso não constitui decisão do Human Gate.

## 2026-07-13 — Correção de cartões operacionais no mobile

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente.
- Estado resultante: sem transição; `STATE-05` e seu Human Gate permanecem pendentes.
- Decisão: registrar a captura humana como finding `S05-HG-008` e corrigir a compressão em duas colunas das telas Alertas e Configuração.
- Escopo: Design System `2.1.2`, uma coluna abaixo de `768` CSS px, quebra segura de timestamps/providers/reason codes, regressão de fonte e amostras adicionais a `320×568`.
- Gates: 28 testes Dashboard, typecheck e build Vite aprovados; matriz headless ampliada para 52 amostras em `pt-BR`/`en-GB` e Light/Dark. Alertas e Configuração passaram a `390×844` e `320×568` com uma coluna, sem overflow de cartão ou documento.
- Evidências: CSS e teste de apresentação; capturas/relatórios sanitizados em `%TEMP%`; Design System; finding `S05-HG-008`.
- Riscos/ressalvas: nenhuma interface visível foi aberta e a correção automática não constitui aprovação visual. WPF, Narrator, High Contrast, scaling, integração externa e execução administrativa não foram exercitados.
- Aprovador: PENDENTE para revalidação visual mobile; Human Gate completo permanece pendente.

## 2026-07-14 — Aprovação de 390 px e correção do resumo de Alertas

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente.
- Estado resultante: sem transição; `STATE-05` e seu Human Gate permanecem pendentes.
- Decisão: registrar a aprovação explícita apenas da amostra `390 px` de `S05-HG-008` e tratar separadamente o espaço vazio descoberto na faixa narrow-desktop como `S05-HG-009`.
- Escopo: Design System `2.1.3`, largura integral do resumo de Alertas, três tracks para três métricas até `1100` CSS px, regressão de fonte e nova amostra `960×1040`.
- Gates: 29 testes Dashboard, typecheck e build Vite aprovados; matriz headless ampliada para 56 amostras em `pt-BR`/`en-GB` e Light/Dark. As quatro novas amostras de Alertas mediram três métricas/tracks, zero pixel de lacuna direita e nenhum overflow global.
- Evidências: captura humana não versionada; CSS/teste de apresentação; relatórios/capturas sanitizados em `%TEMP%`; findings `S05-HG-008` e `S05-HG-009`.
- Riscos/ressalvas: `320 px`, 200% zoom e confirmação visual do resumo corrigido permanecem pendentes. Nenhum WPF, Narrator, High Contrast, scaling ou integração externa foi exercitado.
- Aprovador: Bruno aprovou exclusivamente a amostra `390 px`; nenhuma aprovação de `S05-HG-009` ou do Human Gate completo foi inferida.

## 2026-07-14 — Auditoria Lighthouse reproduzível e correção de contraste

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente.
- Estado resultante: sem transição; `STATE-05` e seu Human Gate permanecem pendentes.
- Decisão: executar diretamente a auditoria Lighthouse recomendada, tornar a coleta reproduzível e corrigir os findings automáticos confirmados sem inferir aprovação humana.
- Escopo: quatro rotas Dashboard, mobile `390×844`, desktop `1440×1000`, três repetições, Chrome headless isolado sem extensões, Lighthouse `13.4.0`, política `robots.txt` para console interno e contraste do rótulo lateral Light.
- Gates: 31 testes Dashboard, typecheck e build Vite aprovados; matriz final de 24 relatórios com medianas Performance/Acessibilidade/Boas Práticas `100`, CLS zero, crawler policy válida em `24/24` e zero requisições de extensão. SEO `66` decorre exclusivamente do bloqueio intencional `Disallow: /`.
- Evidências: `docs/STATE-05-Lighthouse-Audit.md`, `scripts/run-state05-lighthouse-audit.ps1`, relatórios sanitizados consolidados e payloads brutos temporários em `%TEMP%`.
- Riscos/ressalvas: Lighthouse não substitui revisão humana; não houve browser visível, WPF, Narrator, zoom nativo, High Contrast, scaling, integração externa ou execução administrativa. `robots.txt` é orientação de crawler, não controle de acesso.
- Aprovador: resultado automático autorizado por Bruno; Human Gate `STATE-05` permanece `PENDENTE`.

## 2026-07-14 — WPF, High Contrast, scaling e limite da amostra Narrator

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente.
- Estado resultante: sem transição; evidência técnica WPF ampliada e Narrator permanece não testado.
- Decisão: repetir a matriz WPF em tamanho padrão/mínimo, testar High Contrast e scaling reais com restauração, e não inferir resultado auditivo quando o validador informou não conseguir testar Narrator.
- Escopo: oito amostras WPF bilíngues Light/Dark, High Contrast nativo, 125%/120 DPI e 150%/144 DPI, opções reais de scaling, foco/UI Automation e correção DPI-aware do capturador.
- Gates: build WPF sem avisos/erros; nenhuma amostra com controle focalizável sem nome; foco Tab contido; High Contrast restaurado para Off; scaling restaurado para 100%; 200% indisponível no monitor `1920×1080`.
- Evidências: `docs/STATE-05-WPF-Accessibility-Audit.md`, `scripts/audit-state05-wpf.ps1` e relatórios/capturas temporários em `%TEMP%\DBNotifier-State05-Audit`.
- Riscos/ressalvas: WPF reportou consciência de DPI do sistema, sem prova mixed-DPI/per-monitor; High Contrast e scaling ainda exigem aceitação humana; fala/ordem/usabilidade do Narrator não foram testadas.
- Aprovador: evidência automática autorizada por Bruno; o validador não conseguiu executar Narrator e nenhuma decisão do Human Gate foi inferida.

## 2026-07-14 — Overview operacional e flyout do Tray em duas colunas

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente.
- Estado resultante: sem transição; novo incremento frontend verificado automaticamente e Human Gate ainda pendente.
- Decisão: incorporar o padrão visual/funcional fornecido pelo usuário às capacidades já existentes, preservando a verdade de dados demonstrativos, suporte e autorização.
- Escopo: Design System `2.2.0`; Overview-first em React/WPF; métricas, frota, alertas, tendência demonstrativa e distribuição do fixture; TV sobre a mesma Overview; navegação de cinco itens; flyout WPF em duas colunas com ações seguras e Restart indisponível.
- Gates: build Release .NET 10 com 0 avisos/erros; 128 testes unitários e 5 de arquitetura; 32 testes Dashboard, typecheck e build Vite; matriz responsiva de 72 amostras; Lighthouse final de 30 relatórios com todas as medianas de Performance/Acessibilidade/Boas Práticas em `100`, Acessibilidade/Boas Práticas `100` em `30/30`, Performance `100` em `29/30`, CLS zero e crawler policy válida.
- Evidências: código React/CSS/WPF/localização, `docs/design/DB-Notifier-Design-System.md`, relatórios `STATE-05`, runners Dashboard/Lighthouse e payloads temporários sanitizados em `%TEMP%`.
- Riscos/ressalvas: Overview, gráficos, providers e alertas continuam demonstrativos; nenhuma integração, notificação externa ou ação administrativa foi ativada. A primeira matriz Lighthouse detectou contraste `4.03:1` nos eixos; o token foi corrigido e a matriz completa repetida. Visual do Dashboard/WPF/Tray ainda requer confirmação humana.
- Aprovador: implementação solicitada por Bruno; `S05-HG-010` e o Human Gate completo permanecem `PENDENTES`.

## 2026-07-14 — Auditoria completa de rastreabilidade das solicitações

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente.
- Estado resultante: sem transição; `STATE-05` e seu Human Gate permanecem pendentes.
- Decisão: consolidar toda a sequência de solicitações visuais, funcionais, de acessibilidade e governança em uma matriz requisito → documentação → implementação → teste/evidência → pendência.
- Escopo: 37 unidades de requisito, 60 arquivos Markdown preexistentes, 287 entradas rastreadas pelo Git, histórico até `9069942`, componentes React/WPF, testes e runners de auditoria.
- Resultado: toda solicitação consolidada possui documento proprietário ou evidência histórica identificada; requisitos substituídos, limitações de plataforma, amostras humanas e trabalho de `STATE-06/07` permanecem diferenciados. O `README.md` corrente foi sincronizado de cinco para seis incrementos e de Design System `2.1.0` para `2.2.0`; o índice documental passou a listar as evidências atuais.
- Gates: 173 links Markdown em 61 arquivos; 171 arquivos de código no gate documental; marca/tokens/localização sem drift; 32 testes Dashboard, typecheck e build Vite; 128 testes .NET unit/model/provider/presentation e 5 de arquitetura; format .NET limpo; 10 testes Pester.
- Evidências: `docs/STATE-05-Request-Traceability-Audit.md`, `docs/README.md`, `README.md` e `prompts/state/Current-State.md`.
- Riscos/ressalvas: a auditoria documental não repete todos os testes citados, não aprova visual/acessibilidade humana, não implementa integração externa e não autoriza transição, provider, notificação real ou controle administrativo.
- Aprovador: não aplicável a uma decisão de Human Gate; auditoria solicitada por Bruno.

## 2026-07-14 — Reconhecimento humano da matriz de rastreabilidade

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente.
- Estado resultante: sem transição; `STATE-05` e seu Human Gate permanecem pendentes.
- Decisão: reconhecer que a matriz consolidada representa corretamente as solicitações do usuário e aceitá-la somente como inventário documental.
- Evidência: confirmação explícita de Bruno — `A lista representa corretamente minhas solicitações. Reconheço a matriz como inventário documental, sem aprovar ainda o Human Gate de STATE-05.`
- Limite: o reconhecimento não aprova implementação, evidência técnica, experiência visual/acessível, `S05-HG-010`, Human Gate ou transição para `STATE-06`.
- Aprovador: Bruno, exclusivamente quanto à representação documental da lista.

## 2026-07-14 — Refinamento Design System 2.3.0 solicitado após revisão visual

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente e `S05-HG-010` não aprovado na primeira revisão visível.
- Estado resultante: sem transição; o incremento automático foi corrigido/verificado e `S05-HG-010` requer nova revisão humana.
- Decisão: implementar os findings explícitos da primeira revisão sem copiar artwork proprietário, habilitar integração externa ou apresentar capacidades futuras como disponíveis.
- Escopo: navegação Dashboard na ordem Overview, Instances, Alerts, Performance, History, Configuration, Providers e Settings; sino/engrenagem no TopBar; wordmark visual verde/branco; marca provider-neutral de banco com sino; KPIs separados; glyphs de provider e status textual; ícones de alertas; Performance/Providers; ativação direta e conteúdo seguro do flyout WPF.
- Gates automáticos: 32 testes Dashboard, typecheck/build, 96 amostras responsivas bilíngues Light/Dark e 16 relatórios Lighthouse atuais. O primeiro passe Lighthouse móvel detectou semântica ARIA inválida na marca; após correção, Accessibility/Best Practices atingiram `100` em `16/16` e Performance ficou em `99`–`100`. Build/testes .NET e gates completos são registrados no hand-off do commit.
- Evidências: Design System `2.3.0`, relatórios `STATE-05`, matriz de rastreabilidade ampliada para 45 requisitos, código React/WPF e runners reproduzíveis. Payloads Lighthouse permanecem temporários e sanitizados fora do Git.
- Riscos/ressalvas: Performance/Providers/KPIs/alertas continuam fixtures; TopBar não entrega notificações externas; WPF Settings usa a configuração segura existente; Restart/Silent Mode não executam ações. A assinatura foi endurecida de `MouseUp` para o evento padrão `MouseClick`; UI Automation/ponteiro injetado não provaram o callback do shell, portanto um clique humano ainda é necessário antes de qualquer aceite do Tray.
- Aprovador: implementação solicitada por Bruno; Human Gate `STATE-05` permanece `PENDENTE`.

## 2026-07-14 — Correção do flyout e paridade estrutural WPF/Web

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente e `S05-HG-010` reprovado na amostra WPF/Tray.
- Estado resultante: sem transição; falha técnica corrigida e revisão visual humana ainda pendente.
- Decisão: tratar a exceção informada pelo validador e a diferença WPF/Web como findings bloqueantes, sem inferir aprovação a partir do clique que encontrou o defeito.
- Escopo: valores WPF tipados para `Padding`/`CornerRadius`, regressão arquitetural para templates diferidos, rail Desktop com Overview, Instances, Alerts, Performance, History, Configuration, Providers e Settings, títulos localizados, destinos somente leitura separados e reflow compacto.
- Gates: build WPF Release com 0 avisos/erros; 7 testes de arquitetura; matriz UI Automation de oito combinações `pt-BR/en-GB × Light/Dark × 1180×760/820×620`, todas com 16 controles focalizáveis visíveis, nenhum sem nome e foco contido; ativação dirigida do `NotifyItemIcon` abriu o flyout `560×473` com processo responsivo e sem diálogo `.NET`.
- Evidências: `S05-HG-010`, Design System `2.3.1`, matriz de rastreabilidade `REQ-045/046`, teste `WpfPresentationContractTests` e capturas sanitizadas temporárias sob `%TEMP%\DBNotifier-State05-Audit`.
- Riscos/ressalvas: paridade é semântica e estrutural, não pixel-a-pixel; dados permanecem demonstrativos; notificações por mudança são `STATE-06`; ações administrativas reais continuam indisponíveis; o validador ainda precisa aceitar ou rejeitar visualmente o WPF/flyout corrigido.
- Aprovador: correção solicitada por Bruno após encontrar a falha; nenhuma decisão do Human Gate foi inferida.

## 2026-07-14 — Cliente Windows notification-area-first inspirado no MySQL Notifier

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente e shell WPF ainda exibido na inicialização normal.
- Estado resultante: sem transição; hierarquia Windows corrigida e `S05-HG-011` permanece pendente de revisão humana.
- Decisão: tratar o WPF principalmente como notificador da área de notificação, usando o Oracle MySQL Notifier como referência conceitual de interação e mantendo o Dashboard Web como superfície completa.
- Escopo: Design System `2.4.0`; pesquisa nos manuais oficiais arquivados; inicialização normal somente no Tray; flyout como superfície primária; shell WPF como drill-down; argumento `--show-desktop` para auditoria; instrução permanente e matriz `REQ-047`.
- Gates: build completo com zero avisos/erros; 132 testes unit/model/provider/presentation, sete de arquitetura, 32 Dashboard e dez Pester; format, marca/tokens/localização, 175 links Markdown, 173 arquivos no gate documental, auditorias npm/NuGet e runtime fail-closed aprovados. O smoke dirigido confirmou startup sem main-window handle, NotifyIcon/flyout e shell secundário, processo responsivo e ausência de diálogo `.NET`; a auditoria `pt-BR`/Dark `820×620` encontrou 16 controles focalizáveis, nenhum sem nome e 12 passos de Tab contidos.
- Evidências: `docs/design/DB-Notifier-Design-System.md`, `docs/Legacy-Migration-Plan.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`, política `TrayStartupPolicy` e runner WPF.
- Riscos/ressalvas: MySQL Notifier está em Lifetime Sustaining Support e não é dependência; código, arte, identidade MySQL, WMI/DCOM, firewall e controle direto não foram reutilizados. Notificações reais/ícone agregado são `STATE-06`; Start/Stop/Restart dependem de `STATE-07`.
- Aprovador: implementação solicitada por Bruno; nenhuma aprovação de `S05-HG-011` ou do Human Gate foi inferida.

## 2026-07-14 — Decisão clean-room e política agregada do Tray

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate pendente e hierarquia notification-area-first implementada.
- Estado resultante: sem transição; Design System `2.4.1` e `S05-HG-011` continuam pendentes de revisão humana.
- Decisão: o product owner escolheu a implementação clean-room funcionalmente inspirada no comportamento público do MySQL Notifier, preservando licença MIT e identidade independente do DB Notifier.
- Escopo: regra permanente de proveniência; matriz `REQ-048`; resumo provider-neutral Healthy/Warning/Critical/Unknown; stale tratado como unknown; precedência de severidade; política futura opt-in/change-only; texto agregado demonstrativo no Tray.
- Gates: geração de localização aprovada; 135 testes unit/model/provider/presentation aprovados; build WPF Release aprovado com zero avisos/erros; validação completa registrada no relatório de implementação.
- Evidências: `AGENTS.md`, Design System `2.4.1`, `docs/Legacy-Migration-Plan.md`, `docs/STATE-05-Request-Traceability-Audit.md`, `TrayFleetPresentationPolicy` e testes de Tray.
- Riscos/ressalvas: nenhum código, binário, ativo, logótipo, trade dress, texto ou arquitetura Oracle/MySQL foi incorporado. O ícone continua estático e a entrega Windows não foi implementada; ambos exigem estado autorizado e integração de `STATE-06`.
- Aprovador: direção clean-room escolhida por Bruno; nenhuma aprovação de `S05-HG-011`, do Human Gate ou de transição foi inferida.

## 2026-07-14 — Ícone transparente semântico e cobertura funcional completa da referência

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e revisão `S05-HG-011` pendentes.
- Estado resultante: sem transição; Design System `2.5.0` implementado em seu escopo demonstrativo e capacidades futuras explicitamente faseadas.
- Decisão: remover o fundo azul, ampliar o banco, usar sino verde/amarelo/vermelho/cinza conforme o agregado provider-neutral e inventariar todas as capacidades públicas documentadas do MySQL Notifier sem copiar código, arte, identidade ou mecanismos inseguros.
- Escopo: gerador clean-room canônico; SVG transparente; ICO padrão e variantes Healthy/Warning/Critical/Unknown em nove resoluções; seleção do ícone pela fixture agregada; `REQ-049/050`; matriz adotar/adaptar/rejeitar-ou-substituir com destino `STATE-05/06/07/08`.
- Gates: validação completa registrada no relatório de implementação após geração determinística, builds, testes, documentação, dependências e checks de segurança aplicáveis.
- Evidências: Design System `2.5.0`, `docs/Legacy-Migration-Plan.md`, `docs/STATE-05-Request-Traceability-Audit.md`, assets gerados, `TrayApplicationController` e testes de apresentação.
- Riscos/ressalvas: a fixture atual demonstra Critical/vermelho, mas não existe atualização por Agent/API ou entrega Windows real em `STATE-05`; Start/Stop/Restart, auto-start/update e integrações permanecem nos estados posteriores indicados. Cobertura de requisito não significa implementação, homologação ou suporte.
- Aprovador: implementação solicitada por Bruno; nenhuma aprovação visual de `S05-HG-011`, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Aprovação humana limitada de S05-HG-011

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-011` pendentes.
- Estado resultante: sem transição; `S05-HG-011` aprovado e Human Gate geral de `STATE-05` ainda pendente.
- Decisão: Bruno respondeu explicitamente `S05-HG-011 APROVADO` após a amostra autorizada do WPF/Tray.
- Escopo: inicialização normal somente na área de notificação, ausência de shell principal automático, ícone transparente com sino semântico, flyout compacto e shell WPF como destino secundário.
- Gates: processo `aa47af2` iniciou responsivo com `MainWindowHandle = 0`; nenhum Narrator, High Contrast ou scaling foi acionado; o processo WPF foi encerrado após a decisão.
- Evidências: resposta humana inequívoca de 2026-07-15, `docs/STATE-05-Human-Gate-Validation.md`, Design System `2.5.0` e matriz `REQ-047/048/049`.
- Riscos/ressalvas: a aprovação não prova estado Agent/API real, entrega Windows, provider homologado, ação administrativa, acessibilidade restante ou produção. `S05-HG-010` e o Human Gate geral continuam pendentes.
- Aprovador: Bruno, exclusivamente para `S05-HG-011`.

## 2026-07-15 — Cor semântica única da marca entre Web e WPF

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado.
- Estado resultante: sem transição; Design System `2.6.0` implementado automaticamente e `S05-HG-010` ainda requer revisão humana.
- Decisão: todas as ocorrências operacionais da marca banco+sino devem consumir o mesmo agregado provider-neutral, com Healthy verde, Warning amarelo, Critical vermelho e Unknown cinzento; ícones de navegação permanecem funcionais e não representam saúde.
- Escopo: gerador de cinco SVGs Web e cinco ICOs Windows; favicon/header Dashboard freshness-aware; header, janela/taskbar, flyout e NotifyIcon WPF alimentados pela mesma `TrayFleetSummary.State`; defaults estáticos Unknown; matriz `REQ-051`.
- Gates: geração e drift da marca, typecheck, 33 testes Dashboard, build Web, build WPF Release, suíte .NET, formatação, documentação, dependências e smoke tray-first conforme evidência do incremento.
- Evidências: Design System `2.6.0`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`, assets gerados e políticas Web/WPF.
- Riscos/ressalvas: a fixture local atual seleciona Critical/vermelho; estado Agent/API real permanece `STATE-06`. A alteração não reabre a hierarquia notification-area-first já aprovada em `S05-HG-011`, mas a coerência visual precisa ser repetida em `S05-HG-010`.
- Aprovador: correção solicitada por Bruno; nenhuma aprovação de `S05-HG-010`, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Refinamento Light/Dark do ícone semântico

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado.
- Estado resultante: sem transição; Design System `2.6.1` implementado automaticamente e a decisão visual de `S05-HG-010` permanece pendente.
- Decisão: substituir o Critical alaranjado por vermelho profundo `#C62828` e tornar o interior do cilindro do banco totalmente transparente, preservando contorno azul, sino semântico e a mesma política agregada em todas as superfícies.
- Escopo: gerador SVG/ICO canônico, cinco SVGs Web, cinco ICOs Windows, cache-busting Web, regressão estrutural, Design System, Human Gate e matriz `REQ-052`; nenhum fluxo Tray, estado, provider ou capacidade operacional foi alterado.
- Gates: geração/drift da marca, tokens/localização, typecheck, 33 testes Dashboard, build Web, npm sem vulnerabilidades, build .NET Release com zero avisos/erros, 135 testes unitários, sete de arquitetura, format, dez Pester, bundle, NuGet, documentação/links, runtime fail-closed, 96 amostras Dashboard e startup WPF oculto sem janela principal aprovados.
- Evidências: Design System `2.6.1`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`, ativos gerados e regressão de transparência superior a 70% nos ICOs de 256 px.
- Riscos/ressalvas: Light/Dark ainda precisam de comparação humana visível dentro de `S05-HG-010`; a nova tonalidade não prova acessibilidade subjetiva nem estado Agent/API real. A aprovação de `S05-HG-011` permanece limitada ao fluxo notification-area-first e ao mapeamento semântico já aceitos.
- Aprovador: refinamento solicitado por Bruno; nenhuma aprovação de `S05-HG-010`, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Keyline adaptativa e legibilidade do ícone pequeno

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado.
- Estado resultante: sem transição; Design System `2.6.2` implementado automaticamente e a decisão visual de `S05-HG-010` permanece pendente.
- Decisão: preservar transparência e Critical `#C62828`, acrescentar keyline profunda sob os traços claros e adaptar peso/detalhe raster para superfícies Windows de 16/20 px.
- Escopo: gerador SVG/ICO canônico, cinco SVGs Web, cinco ICOs Windows, cache-busting Web, simplificação sem costura intermediária em 16/20 px, regressão de alfa/peso, Design System, Human Gate e matriz `REQ-053`; nenhuma política de estado, provider ou capacidade operacional mudou.
- Gates: geração/drift da marca, tokens/localização, typecheck, 33 testes Dashboard, build Web, npm sem vulnerabilidades, build .NET Release com zero avisos/erros, 135 testes unitários, sete de arquitetura, format, dez Pester, bundle, NuGet, documentação/links, runtime fail-closed, 96 amostras Dashboard e startup WPF oculto sem janela principal aprovados.
- Evidências: Design System `2.6.2`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`; regressão exige alfa zero no interior, mais de 50% do canvas 256 px transparente e pelo menos 150 pixels substancialmente opacos em 16 px.
- Riscos/ressalvas: keyline e simplificação ainda requerem inspeção humana real em Light/Dark, título, taskbar e área de notificação. Estado Agent/API real continua `STATE-06`; `S05-HG-011` não foi reaberto porque fluxo e mapeamento semântico permaneceram intactos.
- Aprovador: Bruno concordou explicitamente com o refinamento; nenhuma aprovação de `S05-HG-010`, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Raster pixel-alinhado e favicon pequeno dedicado

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado.
- Estado resultante: sem transição; Design System `2.6.3` implementado automaticamente e a decisão visual de `S05-HG-010` permanece pendente.
- Decisão: tratar como falha válida a menor nitidez de `2.6.2` diante dos ícones vizinhos e preservar transparência/Critical `#C62828` enquanto se reduz a mistura de bordas em superfícies pequenas.
- Escopo: raster 16/20 px pixel-alinhado, cobertura 24 px reduzida, paleta compacta até 32 px, costuras do banco restauradas, sino sólido, cinco favicons ICO de quatro resoluções, cache-busting Web, regressões e matriz `REQ-054`; SVG grande, agregação e capacidades operacionais não mudaram.
- Gates: geração/drift da marca, tokens/localização, typecheck, 33 testes Dashboard, build Web, npm sem vulnerabilidades, build .NET Release com zero avisos/erros, 135 testes unitários, sete de arquitetura, format, dez Pester, bundle, NuGet, documentação/links, 96 amostras Dashboard e startup WPF oculto sem janela principal aprovados.
- Evidências: Design System `2.6.3`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`; regressão exige zero pixels com alfa parcial em 16/20 px, interior transparente e divergência entre as quatro cores semânticas.
- Riscos/ressalvas: a nitidez comparativa ainda exige nova inspeção humana em favicon, título, taskbar, overflow de notificações e flyout. Estado Agent/API real continua `STATE-06`; `S05-HG-011` não foi reaberto porque fluxo e mapeamento semântico permaneceram intactos.
- Aprovador: finding relatado por Bruno; nenhuma aprovação de `S05-HG-010`, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Marca compacta e nome canônico nas notificações Windows

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado.
- Estado resultante: sem transição; Design System `2.6.4` implementado automaticamente e a decisão visual de `S05-HG-010` permanece pendente.
- Decisão: aceitar que `2.6.3` melhorou materialmente apenas a notificação Windows, tratar a menor nitidez das demais superfícies como falha válida e impedir que o shell exponha `DBNotifier.Desktop.Wpf` como nome visual.
- Escopo: SVG/ICO canônico de camada única, alfa binário em 16/20/24/32 px, contorno `#0078D4`, sino/clapper semântico sólido, Critical `#C62828`, favicons e ICOs regenerados, cache-busting Web, metadados WPF File Description/Product Name `DB Notifier`, regressões e matriz `REQ-055/056`.
- Gates: restore locked, build Release sem avisos/erros, 135 testes unitários, oito de arquitetura, format, 33 testes Dashboard, typecheck/build, geração/drift, documentação/links, dependências, dez Pester, bundle, runtime fail-closed, integridade Git e 96 amostras Dashboard aprovados.
- Evidências: Design System `2.6.4`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`; executável Release expõe File Description e Product Name `DB Notifier`; regressão exige zero alfa parcial nas quatro menores entradas ICO.
- Riscos/ressalvas: nitidez comparativa, cache/atribuição efetiva do shell e aparência nas superfícies reais ainda exigem repetição humana de `S05-HG-010`. Estado Agent/API real continua `STATE-06`; `S05-HG-011` não foi reaberto porque fluxo notification-area-first e política semântica não mudaram.
- Aprovador: correções solicitadas por Bruno; nenhuma aprovação de `S05-HG-010`, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Seleção nativa de quadros ICO no WPF

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado.
- Estado resultante: sem transição; Design System `2.6.5` implementado automaticamente e a decisão visual de `S05-HG-010` permanece pendente.
- Decisão: tratar a melhora parcial de `2.6.4` como insuficiente e corrigir o downsampling adicional comprovado no caminho de renderização WPF, preservando a geometria, transparência e Critical `#C62828` já definidos.
- Escopo: decoder WPF por quadro nativo, header de 40 px, flyout de 32 px, métrica pequena do Windows para NotifyIcon, papéis nativos separados para título/taskbar, hosts sem stretch e pixel-snapped, cache-busting Web, regressões e matriz `REQ-057`; nenhuma política de estado, provider ou capacidade operacional mudou.
- Gates: build WPF Release sem avisos/erros, nove testes de arquitetura e 33 testes Dashboard aprovados no passe focado; a revalidação completa permanece registrada no relatório de implementação.
- Evidências: Design System `2.6.5`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`; regressões rejeitam o antigo `BitmapImage` genérico e exigem tamanho por superfície, métrica do shell e `WM_SETICON` independente.
- Riscos/ressalvas: a correção remove uma causa técnica objetiva de suavização, mas comparação perceptual com ícones vizinhos ainda exige repetição humana. Estado Agent/API real continua `STATE-06`; `S05-HG-011` não foi reaberto porque fluxo notification-area-first e política semântica não mudaram.
- Aprovador: refinamento solicitado por Bruno; nenhuma aprovação de `S05-HG-010`, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Unificação do modelo canônico da marca

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado.
- Estado resultante: sem transição; Design System `2.6.6` implementado no escopo de ativos e a decisão visual de `S05-HG-010` permanece pendente.
- Decisão: tratar como finding válido as diferenças de desenho ainda visíveis após `2.6.5` e exigir um único modelo bonito, nítido e de boa qualidade em todas as superfícies, com a mesma geometria, proporções e transparência; somente a cor semântica do sino e a densidade de amostragem da resolução podem variar.
- Escopo: um contrato geométrico para SVG/ICO, cilindro/costuras/sino compartilhados, canvas e interior transparentes, supersampling 2×/4×, BGRA com alfa direto, payload favicon/Windows equivalente em 16/20/24/32 px, substituição do logo GDI do cliente PowerShell por carregamento/empacotamento da família canônica, retirada do glyph Info não relacionado do balloon WPF, seleção WPF DPI-aware com quadro exato/imediatamente maior, ativos semânticos regenerados, cache-busting Web, regressões e matriz `REQ-058`; política agregada e fluxo notification-area-first foram preservados.
- Gates: `brand:verify`, build Release com zero avisos/erros, 135/135 testes unitários, 9/9 de arquitetura, 33/33 Dashboard, 11/11 Pester, validação do bundle, 174 fontes no gate documental, 179 links em 61 Markdown, matriz Web de 96 amostras, auditorias NuGet/npm e runtime fail-closed foram aprovados. As regressões comparam a fonte geométrica, máscaras alfa entre estados, canais de cor nas bordas parcialmente transparentes e igualdade byte-a-byte dos quadros favicon/Windows de mesmo tamanho; a aceitação perceptual continua humana.
- Evidências: Design System `2.6.6`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`, gerador canônico e famílias SVG/ICO produzidas.
- Riscos/ressalvas: equivalência estrutural não prova beleza ou nitidez percebida nas superfícies reais; browser tab/header, WPF title/header/flyout, taskbar, overflow e notificação Windows ainda exigem repetição humana de `S05-HG-010`. Estado Agent/API real continua `STATE-06`; `S05-HG-011` permanece aprovado porque seu fluxo e mapeamento semântico não mudaram.
- Aprovador: padronização solicitada por Bruno; nenhuma aprovação de `S05-HG-010`, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Associação semântica de favicon e fonte da notificação

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado.
- Estado resultante: sem transição; Design System `2.6.7` implementado no escopo controlável de favicon e origem do quadro de notificação, com decisão visual de `S05-HG-010` ainda pendente.
- Decisão: tratar como finding válido as duas últimas divergências relatadas após `2.6.6`, sem confundir cache do navegador ou renderização do Windows Shell com estado Healthy comprovado e sem prometer controle dinâmico sobre atribuição pertencente ao sistema operacional.
- Escopo: helper tipado único para caminhos de header/favicon por estado e revisão, substituição atômica de candidatos identificados/legados em `useLayoutEffect`, remoção de duplicatas, estado explícito no nó, quadro WPF maior da mesma variante oferecido durante `ShowBalloonTip`, restauração imediata do quadro pequeno do NotifyIcon, criação exception-safe dos recursos nativos, regressões e matriz `REQ-059`; geometria, política agregada e fluxo notification-area-first permanecem inalterados.
- Gates: restore locked, formato .NET, build Release com zero avisos/erros, 135/135 testes unitários, 9/9 de arquitetura, 34/34 Dashboard, typecheck/build Vite, 11/11 Pester, validação do bundle, geração/drift, documentação/links e auditorias NuGet/npm foram aprovados. Chrome headless isolado encontrou exatamente um favicon ativo com estado/path `critical` e revisão `2.6.7-critical`.
- Evidências: Design System `2.6.7`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`, helper `semanticBrand.ts` e regressões de substituição/restauração.
- Riscos/ressalvas: Chromium e Windows podem preservar associações/entradas históricas; a repetição deve usar uma nova janela/guia e uma notificação recém-entregue. O Windows Shell controla a apresentação final da atribuição, e atualização por Agent/API real e notificações por mudança pertencem a `STATE-06`. `S05-HG-011` permanece aprovado porque seu fluxo e política de estado não mudaram.
- Aprovador: correção solicitada por Bruno; nenhuma aprovação de `S05-HG-010`, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Ícone verde fixo da confirmação de disponibilidade

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado.
- Estado resultante: sem transição; Design System `2.6.8` implementado no escopo controlável da confirmação inicial, com decisão visual de `S05-HG-010` ainda pendente.
- Decisão: tratar a primeira notificação de recolhimento como confirmação não-state-bearing de disponibilidade do aplicativo e oferecer ao Windows a marca canônica transparente com sino verde fixo, sem representar esse verde como saúde da frota.
- Escopo: `availabilityNotificationIcon` nativo separado, variante canônica verde de 32 px somente durante `ShowBalloonTip`, restauração do `applicationIcon` agregado em `finally`, revisão determinística `2.6.8`, regressões e matriz `REQ-060`; demais superfícies state-bearing, agregação, geometria e fluxo notification-area-first permanecem inalterados.
- Gates: formato .NET, build Release com zero avisos/erros, 135/135 testes unitários, 9/9 de arquitetura, 34/34 Dashboard, typecheck/build Vite, 11/11 Pester, bundle legado, marca/tokens/localização, 175 fontes no gate documental, 179 links em 61 Markdown, 96 amostras headless, auditorias NuGet/npm e runtime fail-closed aprovados. Chrome headless isolado encontrou exatamente um favicon com estado/path `critical` e revisão `2.6.8-critical`; o quadro Healthy de 32 px foi verificado com transparência e verde canônico `#48C75F`.
- Evidências: Design System `2.6.8`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`, `TrayApplicationController.cs` e regressões de seleção/restauração.
- Riscos/ressalvas: o Windows Shell continua controlando a atribuição, escala, cache e snapshots históricos; uma nova notificação precisa ser revisada humanamente. Futuras notificações de estado permanecem em `STATE-06` e devem usar estado factual mais texto explícito. `S05-HG-011` permanece aprovado porque ativação, flyout e Tray semântico não mudaram.
- Aprovador: refinamento solicitado por Bruno; nenhuma aprovação de `S05-HG-010`, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Seleção semântica por significado da notificação

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado.
- Estado resultante: sem transição; Design System `2.6.9` implementado no escopo de seleção visual tipada, sem ativar entrega por mudança.
- Decisão: cada notificação resolve o sino pelo significado factual da própria mensagem, independentemente do agregado mantido no Tray: disponibilidade/recuperação verde, aviso amarelo, crítico vermelho profundo e informação/desconhecido cinza; valor inválido falha para Unknown.
- Escopo: `TrayNotificationMeaning`, `TrayNotificationPresentationPolicy`, confirmação first-hide ligada ao significado AvailabilityOrRecovery, restauração do ícone agregado em `finally`, apenas um `ShowBalloonTip` em `STATE-05`, revisão determinística Web `2.6.9`, cinco casos unitários e matriz `REQ-061`. Nenhum publisher Agent/API, evento real ou nova entrega foi introduzido.
- Gates: restore locked, formato .NET, build Release com zero avisos/erros, 140/140 testes unitários, 9/9 de arquitetura, 34/34 Dashboard, typecheck/build Vite, 11/11 Pester, bundle legado, marca/tokens/localização, 175 fontes no gate documental, 179 links em 61 Markdown, 96 amostras headless, auditorias NuGet/npm e runtime fail-closed aprovados. Chrome headless isolado encontrou exatamente um favicon com estado/path `critical` e revisão `2.6.9-critical`.
- Evidências: Design System `2.6.9`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`, `docs/Legacy-Migration-Plan.md`, políticas Application/WPF e regressões de significado/escopo/restauração.
- Riscos/ressalvas: integração `STATE-06` ainda deve classificar transições tipadas, serializar entregas para impedir cruzamento de ícones, aplicar opt-in/deduplicação/quiet policy e nomear instância/estado no texto. O Windows Shell mantém autoridade sobre atribuição, escala, cache e snapshots históricos. `S05-HG-010` continua pendente e `S05-HG-011` não foi reaberto.
- Aprovador: esclarecimento funcional solicitado por Bruno; nenhuma aprovação visual, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Compatibilidade da transparência ICO no Windows

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado.
- Estado resultante: sem transição; Design System `2.6.10` implementado no ativo canônico, com nova revisão humana ainda necessária.
- Decisão: manter o canvas do ícone transparente tanto no canal alfa BGRA quanto na máscara AND legada do ICO, sem afirmar que o cartão de notificação controlado pelo Windows seja transparente.
- Escopo: gerador canônico de ativos, cinco favicons ICO, cinco famílias ICO Windows, regressão pixel a pixel da máscara legada, revisão Web determinística `2.6.10`, Design System, Human Gate e matriz `REQ-062`. Geometria, cores semânticas, seleção por significado, fluxo notification-area-first e capacidades operacionais permanecem inalterados.
- Gates: restore locked, formato .NET, build Release com zero avisos/erros, 140/140 testes unitários, 9/9 de arquitetura, 34/34 Dashboard, typecheck/build Vite, 11/11 Pester, bundle legado, marca/tokens/localização, 175 fontes no gate documental, 179 links em 61 Markdown, 96 amostras headless, auditorias NuGet/npm e runtime fail-closed aprovados. A auditoria dos nove quadros Windows encontrou zero divergências entre pixels totalmente transparentes e bits AND, e Chrome headless isolado encontrou exatamente um favicon `critical` na revisão `2.6.10-critical`.
- Evidências: Design System `2.6.10`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`, `docs/Legacy-Migration-Plan.md`, gerador, ativos regenerados e regressão de transparência.
- Riscos/ressalvas: notificações antigas são snapshots e não mudam retroativamente; o Windows Shell continua responsável pelo fundo do cartão, atribuição, escala e cache. Uma notificação recém-entregue ainda precisa de inspeção humana em `S05-HG-010`; `S05-HG-011` não foi reaberto.
- Aprovador: correção solicitada por Bruno; nenhuma aprovação visual, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Silhueta aberta e fonte nativa da notificação

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado.
- Estado resultante: sem transição; Design System `2.6.11` implementado no ativo canônico, com nova notificação humana ainda necessária.
- Decisão: tratar como finding válido a aparência de bloco azul na amostra `2.6.10`, distinguindo o canvas tecnicamente transparente de uma silhueta azul excessivamente densa no tamanho de 16 px e sem afirmar controle do cartão ou glyph final pertencentes ao Windows Shell.
- Escopo: contorno canônico reduzido de `4.5` para `3.5` unidades, quadro de 16 px com `155/256` pixels totalmente transparentes, `90` pixels com alfa `>=128` e cobertura azul ponderada de `42.84` pixels equivalentes; fonte WPF na métrica pequena nativa; política de lease begin/shown/fallback/failure/dispose; callback `BalloonTipShown` como ponto de restauração best-effort; fallback de dois segundos; revisão Web `2.6.11`; auditoria headless do favicon e matriz `REQ-063`. Resumo, flyout, estado persistido, capacidades e integração externa permanecem inalterados.
- Gates: restore locked, formato .NET, build Release com zero avisos/erros, 141/141 testes unitários, 9/9 de arquitetura, 34/34 Dashboard, typecheck/build Vite, 11/11 Pester, bundle legado, marca/tokens/localização, 175 fontes no gate documental, links Markdown, 96 amostras headless, auditorias NuGet/npm e runtime fail-closed aprovados. As quatro execuções headless encontraram exatamente um favicon `critical` na revisão `2.6.11-critical`.
- Evidências: Design System `2.6.11`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md`, `docs/Legacy-Migration-Plan.md`, gerador, políticas Application/WPF, ativos regenerados e regressões de densidade/transparência/lease/favicon.
- Riscos/ressalvas: WinForms oferece um único slot, portanto a variante verde ocupa temporariamente o NotifyIcon até o callback ou fallback, sem alterar o agregado lógico. `BalloonTipShown` prova somente que o balloon foi exibido; não garante a captura ou apresentação do glyph em toda versão do Windows. O cartão, atribuição, escala, cache e snapshots continuam sob autoridade do Shell. Uma notificação recém-entregue ainda precisa de inspeção humana em `S05-HG-010`; `S05-HG-011` não foi reaberto.
- Aprovador: correção solicitada por Bruno; nenhuma aprovação visual, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Microglifo transparente e confirmação em todo fechamento explícito

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado; Design System `2.6.11` implementado.
- Estado resultante: sem transição; Design System `2.6.12` implementado e verificado automaticamente no incremento de apresentação local, ainda dependente de confirmação humana específica.
- Decisão: tratar como findings válidos a persistência perceptual do bloco azul na atribuição da notificação e a ausência de nova confirmação após fechamentos subsequentes, sem prometer que o aplicativo controle o fundo do cartão ou que o Windows sempre apresente uma solicitação aceita.
- Escopo: microglifo óptico pixel-aligned nos quadros pequenos, PNG transparente dedicado de disponibilidade, Windows App SDK `2.2.0`, bootstrap fail-safe do Windows App Runtime, `AppNotificationManager` registrado como `DB Notifier`, ativação limitada a revelar o shell local, fallback legado único e política que solicita nova confirmação em cada `CloseRequest`, mas não em Minimize, startup oculto, Show ou Exit. Agregado, flyout, estado persistido e capacidades externas permanecem inalterados.
- Gates: `143/143` testes unit/model/provider/presentation, `9/9` testes de arquitetura, `34/34` testes Dashboard, typecheck/build Vite, build Release .NET 10 sem avisos/erros, format, 11/11 Pester, bundle legado, marca/tokens/localização, auditorias NuGet/npm, runtime fail-closed e matriz headless de 96 amostras aprovados. A porta `4173`, já ocupada, não foi perturbada; a matriz usou portas alternativas isoladas.
- Evidências: Design System `2.6.12`, gerador/ativos de marca, `WindowsAppNotificationPublisher`, política Application de `CloseRequest`, controller WPF, lockfile do Desktop e regressões .NET; os relatórios proprietários e a matriz de rastreabilidade permanecem responsáveis pelo fechamento documental completo.
- Riscos/ressalvas: Windows/Focus Assist pode suprimir a apresentação visível mesmo após aceitar a publicação. Instalação do Windows App Runtime, identidade/caminho estáveis, packaging, rollback e limpeza de desinstalação pertencem a `STATE-08`; notificações por mudança factual Agent/API continuam em `STATE-06`. `S05-HG-010` permanece pendente e a aprovação limitada de `S05-HG-011` não foi reaberta.
- Aprovador: correções solicitadas por Bruno; nenhuma aprovação visual, do Human Gate ou de transição foi inferida.

## 2026-07-15 — Restauração de uma única geometria da marca em todos os tamanhos

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado; Design System `2.6.12` implementado.
- Estado resultante: sem transição; Design System `2.6.13` implementado e verificado automaticamente, com confirmação humana limitada dos detalhes anteriores de transparência/repetição e nova comparação perceptual ainda necessária.
- Decisão: registrar que Bruno confirmou os dois ciclos válidos de fechamento e declarou a transparência/repetição como desejadas, sem extrapolar essa confirmação para o ícone completo, `S05-HG-010` ou o Human Gate. Tratar como finding válido a diferença visual entre o microglifo pequeno e o banco canônico maior, restaurando uma única geometria em todas as superfícies.
- Escopo: remoção de `microGlyph16` e `renderMicroBitmap`; geração de todos os quadros 16/20/24/32/40/48/64/128/256 px a partir de `markGeometry`, cobertura 4× e alfa direto; revisão/cache-busting Web `2.6.13`; regra permanente de modelo único e `REQ-065`. Publisher Windows, PNG de disponibilidade, fallback, agregado, flyout, persistência e política de `CloseRequest` permaneceram inalterados.
- Gates: restore locked, build Release .NET 10 com zero avisos/erros, formato, `143/143` testes unit/model/provider/presentation, `9/9` arquitetura, `34/34` Dashboard, typecheck/build Vite, `11/11` Pester, bundle legado, marca/tokens/localização, documentação/links, auditorias NuGet/npm, runtime fail-closed e 96 amostras Dashboard headless aprovados. Uma primeira tentativa headless terminou com código 1 somente ao encontrar um arquivo de cache ainda aberto durante a limpeza final; seu resultado foi descartado. Somente o processo/pasta temporários isolados foram encerrados, e a repetição completa em portas alternativas aprovou as 96 amostras com código 0.
- Evidências: Design System `2.6.13`, gerador e ativos regenerados, regressão de geometria única, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Request-Traceability-Audit.md` e capturas fornecidas pelo validador sem incorporação de dados externos.
- Riscos/ressalvas: o quadro de 16 px é inevitavelmente mais denso e antialiased, embora derive do mesmo modelo; beleza/nitidez em browser, título, Tray, taskbar, flyout e overflow dependem de nova inspeção humana autorizada. `S05-HG-010`, o Human Gate e `STATE-06` permanecem pendentes; `S05-HG-011` não foi reaberto.
- Aprovador: transparência/repetição confirmadas por Bruno e unificação geométrica solicitada por ele; nenhuma aprovação do ícone corrigido, de `S05-HG-010`, do Human Gate ou de transição foi inferida.

## 2026-07-16 — Aprovação humana limitada da geometria única da marca

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `S05-HG-011` aprovado; `REQ-065` implementado e automaticamente verificado no Design System `2.6.13`.
- Estado resultante: sem transição; a amostra perceptual de `REQ-065` está aprovada, enquanto `S05-HG-010`, o Human Gate de `STATE-05` e `STATE-06` permanecem pendentes.
- Decisão: aceitar somente a geometria única do banco com sino após comparação humana nas superfícies reais, sem extrapolar a resposta para os demais detalhes visuais ou de acessibilidade.
- Escopo: Dashboard em nova janela independente do Chrome e WPF com `--show-desktop`; comparação de browser tab/header, WPF title/header/flyout, taskbar e notification overflow. Narrator, High Contrast e scaling não foram usados. Publisher, política de fechamento, estado, integração e código não mudaram.
- Gates: os gates automáticos de `2.6.13` já registrados permaneceram válidos; o Dashboard local preexistente respondeu em `4173`, o WPF abriu responsivo com o título `DB Notifier — Visão geral` e a árvore Git estava limpa. A janela dedicada e o processo WPF foram encerrados após a decisão, preservando o listener local preexistente.
- Evidências: resposta exata `Ícone único APROVADO`, de Bruno, em 2026-07-16; `REQ-065`; Design System `2.6.13`; protocolo `docs/STATE-05-Human-Gate-Validation.md` e relatório de implementação.
- Riscos/ressalvas: a aprovação não prova estado Agent/API real, notificações factuais, provider homologado, acessibilidade restante, todos os detalhes de `S05-HG-010`, o Human Gate completo ou produção. A amostra precisa ser repetida somente se o contrato/gerador da marca mudar materialmente.
- Aprovador: Bruno, exclusivamente para a geometria única de `REQ-065`; nenhuma aprovação de `S05-HG-010`, do Human Gate ou de transição foi inferida.

## 2026-07-16 — Sincronização temática da caption e da rolagem WPF

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e os detalhes restantes de `S05-HG-010` pendentes; `REQ-064/065` e `S05-HG-011` encerrados somente em seus escopos limitados.
- Estado resultante: sem transição; Design System `2.6.14` implementado e verificado automaticamente para `REQ-066`, com comparação visual Light/Dark ainda pendente.
- Decisão: tratar como finding válido a caption branca e o scrollbar claro observados pelo validador no WPF Dark, sem substituir a barra de título padrão do Windows nem forçar cores de produto durante High Contrast.
- Escopo: adaptador isolado para atributos DWM de modo escuro/caption/texto em Windows 11 suportado, com busca nativa limitada ao `System32`, borda ativa/inativa mantida pelo Windows e fallback de caption nativa no Windows 10; aplicação após `SourceInitialized` e em mudanças de tema; reset de cores para o padrão do Windows em High Contrast; dicionário global de estilos de scrollbar com recursos semânticos, templates vertical/horizontal, `PART_Track` e comandos de linha/página; regra permanente, Design System, Human Gate, relatório e matriz `REQ-066`. Nenhum estado de provider, integração, notificação, Tray, ativo de marca ou capacidade administrativa mudou.
- Gates: build WPF Release com zero avisos/erros e `10/10` testes de arquitetura aprovados no passe focado. A validação completa da solução e dos gates documentais é registrada no relatório proprietário deste incremento; nenhuma execução visível adicional ou configuração de acessibilidade do Windows foi usada.
- Evidências: Design System `2.6.14`, `NativeWindowThemePolicy`, `Resources/ControlStyles.xaml`, regressão WPF em `WpfPresentationContractTests`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md` e `docs/STATE-05-Request-Traceability-Audit.md`.
- Riscos/ressalvas: atributos explícitos de cor da caption exigem suporte da versão do Windows e falham com segurança para a caption nativa quando indisponíveis. Build/teste estrutural não comprovam a aparência real, todos os hosts de scrollbar ou High Contrast; Light → Dark → Light em tamanho padrão/mínimo e High Contrast sob autorização separada continuam pendentes. `S05-HG-010`, o Human Gate e `STATE-06` não foram aprovados.
- Aprovador: correções solicitadas por Bruno; nenhuma aprovação visual, do Human Gate ou de transição foi inferida.

## 2026-07-16 — Execução visível limitada de caption e rolagem WPF

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate e `S05-HG-010` pendentes; `REQ-066` implementado e estruturalmente verificado no Design System `2.6.14`.
- Estado resultante: sem transição; a correção foi observada em execução real, com decisão humana limitada ainda pendente.
- Escopo: somente WPF com `--show-desktop`, Light → Dark → Light em `1180×760` e `820×620`, inspeção da caption e scrollbar principal e exercício de rolagem por UI Automation. Chrome, Narrator, High Contrast e scaling não foram usados.
- Evidências: seis capturas temporárias mostraram caption e scrollbar principal coerentes com Light/Dark; o padrão `Scroll` moveu a posição vertical de `0.00%` para `9.38%` e a restaurou em ambos os temas; os seis ciclos terminaram sem exceção. DataGrid e ComboBox popup não tiveram scrollbars individualmente forçados visíveis nesta amostra.
- Encerramento: somente o processo iniciado para a amostra foi encerrado; nenhum processo WPF permaneceu, e a preferência original `pt-BR`/Dark foi restaurada.
- Riscos/ressalvas: a observação não substitui decisão humana, não comprova High Contrast e não aprova `S05-HG-010`, o Human Gate ou `STATE-06`.
- Aprovador: execução autorizada por Bruno; nenhuma aprovação foi inferida.

## 2026-07-16 — Reauditoria da cobertura funcional pública do MySQL Notifier

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente.
- Estado resultante: sem transição; `REQ-050` permanece `PARCIAL` e o Human Gate continua pendente.
- Decisão: decompor a cobertura clean-room anteriormente agregada em 25 resultados funcionais `MN-*` e quatro invariantes de qualidade `MN-Q*`, preservando separadamente disposição, maturidade, fase proprietária e condição de saída.
- Escopo: comparação comportamental do manual Oracle MySQL Notifier 1.1 e das release notes oficiais até 1.1.8 com a arquitetura, documentação, implementação e evidência atuais do DB-Notifier; atualização documental de matriz, rastreabilidade, estado e índice. Nenhum código, provider, integração, comando, infraestrutura ou artefacto Oracle foi introduzido.
- Gates: 29 IDs únicos confirmados (`25 + 4`); `npm run markdown:verify` aprovou 182 links locais em 61 arquivos; `npm run comments:verify` aprovou 178 arquivos comment-capable; `git diff --check` saiu sem achados no passe final.
- Evidências: `docs/Legacy-Migration-Plan.md`, `docs/STATE-05-Request-Traceability-Audit.md`, `docs/STATE-05-Human-Gate-Validation.md`, `prompts/state/Current-State.md` e as duas publicações oficiais ligadas na matriz.
- Riscos/ressalvas: a auditoria deriva somente resultados públicos e não é inspeção de código, binário, arte ou trade dress Oracle. Itens futuros não constituem implementação, suporte ou homologação; WMI/DCOM, firewall automático, segredo em configuração e acoplamento direto ao Workbench continuam rejeitados ou substituídos. `STATE-06`, `STATE-07` e `STATE-08` permanecem sujeitos aos próprios gates.
- Aprovador: solicitado por Bruno; nenhuma aprovação de amostra, Human Gate ou progressão foi inferida.

## 2026-07-16 — Remediação dos lotes 1–5 da auditoria completa

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente.
- Estado resultante: sem transição; `STATE-05 FRONTEND_IMPLEMENTATION` permanece ativo, com o Human Gate pendente e `STATE-06` não autorizado.
- Decisão: Bruno declarou exatamente `APROVO a remediação dos lotes 1–5, sem avançar o STATE-05 e sem executar ações externas.` A decisão autoriza somente a remediação local dos achados previamente apresentados; não é aprovação de Human Gate, homologação, release ou runtime externo.
- Escopo: lote 1, legado/configuração/transport-only; lote 2, provider/utility/transporte/autenticação/payload; lote 3, reconciliação/migration/concorrência/durabilidade fail-closed; lote 4, freshness factual, Dashboard/WPF/Tray e Design System `3.0.0`, preservando a marca `2.6.13`; lote 5, toolchain/CI/gates/documentação/packaging fail-closed. A decisão histórica `S05-HG-011` não é reutilizada porque freshness, conteúdo e ativação de Tray/flyout mudaram.
- Gates: build .NET Release sem avisos/erros; `179/179` unitários e `13/13` arquitetura; cobertura `78,89%` linhas/`56,69%` branches; `42/42` Dashboard, typecheck e Vite; `23` Pester aprovados, um skip condicional, cobertura `32,08%`; 96 amostras Edge headless; runtime fail-closed e gates de assets, documentação, dependências offline, segredos e Git aprovados. Evidência automática não substitui a repetição humana das superfícies alteradas.
- Evidências: `docs/STATE-05-Complete-Audit-Remediation-Report.md`, implementação e regressões locais dos lotes, Design System `3.0.0`, `docs/STATE-05-Design-System-Implementation-Report.md`, `docs/STATE-05-Human-Gate-Validation.md` e estado corrente.
- Riscos/ressalvas: `M-02`, `M-03` e `M-05` são contenções, não funcionalidades — delivery e command polling recusam startup e raw observations são preservadas. Nenhuma migration foi aplicada a PostgreSQL; somente stores SQLite efêmeros de teste foram usados. Nenhum banco externo/monitorado, serviço real, credential, certificado, IdP, vault, canal, CI remota, deploy, publicação, instalação externa ou infraestrutura real foi exercido.
- Aprovador: Bruno, exclusivamente para a remediação local dos lotes 1–5 sob os limites citados; nenhuma decisão de Human Gate ou progressão foi inferida.

## 2026-07-16 — Finding humano de ausência de notificação por mudança de status

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera, Human Gate pendente e decisão histórica `S05-HG-011` não reutilizável após as mudanças do Design System `3.0.0`.
- Estado resultante: sem transição; a nova amostra de freshness/Tray permanece pendente por finding funcional bloqueante, e `STATE-06` continua não autorizado.
- Decisão: registrar a observação exata de Bruno, `Deveria está notificando a cada mudança de status`, sem convertê-la em aprovação ou reprovação do Human Gate completo e sem inferir autorização para antecipar notification delivery.
- Escopo: Dashboard local e WPF notification-area-first com fixture demonstrativa compartilhada; startup oculto, uma ativação primária do ícone, flyout, atualização de freshness em cadência de 30 segundos e inspeção do caminho de publicação Windows. Narrator, High Contrast, scaling, banco/serviço real, Agent/API, canal externo e transição permaneceram fora da amostra.
- Evidências: Dashboard e WPF iniciaram com uma instância Healthy, uma Degraded, uma Timeout e uma Stale; as superfícies e o ícone envelheceram sem substituir o snapshot. A inspeção comprovou que `RefreshFleetPresentation` não solicita publicação e que o publicador atual trata somente a confirmação local de `CloseRequest`; portanto a ausência observada não foi mera supressão do Windows ou Focus Assist. A janela dedicada, o WPF e o listener local iniciados para a amostra foram encerrados depois do finding.
- Riscos/ressalvas: `MN-004` já exige notificações opt-in, supressão do snapshot inicial, transições factuais por instância, deduplicação, quiet policy e integração autorizada em `STATE-06`. Uma exceção de demonstração que publique banners durante `STATE-05` exige decisão e especificação explícitas; o comentário atual não as substitui.
- Aprovador: Bruno atuou como validador e identificou o finding; nenhuma decisão formal sobre a amostra completa, o Human Gate ou a progressão foi inferida.

## 2026-07-16 — Remediação demonstrativa local das notificações por mudança

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera, Human Gate pendente e finding bloqueante na nova amostra de freshness/Tray.
- Estado resultante: sem transição; Design System `3.0.1` implementado e verificado automaticamente, com repetição humana focada ainda pendente. `STATE-06` continua não autorizado.
- Decisão: Bruno declarou exatamente `AUTORIZO a remediação demonstrativa local no STATE-05, notificando cada mudança individual, sem notificação inicial, sem Agent/API/banco externo e sem transição.` A autorização constitui opt-in somente para a fixture determinística e não amplia delivery operacional.
- Escopo: política Application de captura/diff por `InstanceId`, `HealthStatus` e freshness; baseline silencioso; três transições esperadas da fixture; mensagens pt-BR/en-GB com instância/anterior/atual e verdade sem dados externos; quatro PNGs semânticos derivados da geometria canônica; publisher moderno com logo por evento, tag única, grupo separado, identificadores limitados a 16 caracteres, mute, expiração e ativação show-only; fila fallback limitada/serializada com um único `ShowBalloonTip` e timer monotónico como única fonte de avanço; atualização da baseline antes da entrega; documentação, estado e rastreabilidade. Nenhuma chamada Agent/API/provider/database, ação administrativa ou canal externo foi introduzida.
- Gates: build .NET 10 Release com zero avisos/erros; `185/185` testes unitários, `13/13` arquitetura e `42/42` Dashboard; cobertura `78,95%` linhas/`57,01%` branches; typecheck/build Vite, .NET format, geração/drift de brand/tokens/localização, 23 testes legados/um skip, bundle, runtime fail-closed, secret scan, documentação, Git e 96 amostras Chrome headless aprovados. Auditorias online não foram repetidas. Evidência de apresentação Windows visível permanece exclusivamente humana.
- Evidências: Design System `3.0.1`, `TrayInstanceStateChangePolicy`, `TrayApplicationController`, `WindowsAppNotificationPublisher`, catálogos/adapters gerados, cinco PNGs de notificação e regressões .NET/TypeScript; relatórios `STATE-05` atualizados sem reescrever o finding anterior.
- Riscos/ressalvas: Windows/Focus Assist pode suprimir banners mesmo após aceitar a publicação; o fallback é best-effort e limitado. Persistência de preferência/deduplicação, acknowledgement, quiet policy operacional, auditoria, estado reconciliado e canais reais permanecem `STATE-06`. A nova amostra deve confirmar ausência de banner inicial e observar Analytics, Orders e Finance separadamente antes de qualquer decisão humana.
- Aprovador: implementação autorizada por Bruno dentro do limite demonstrativo citado; nenhuma aprovação de amostra, Human Gate ou progressão foi inferida.

## 2026-07-16 — Execução focada das notificações demonstrativas locais 3.0.1

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera, Human Gate pendente e repetição humana da correção `3.0.1` ainda não executada.
- Estado resultante: sem transição; o baseline de notificações de mudança permaneceu silencioso e três entradas individuais possuem evidência runtime local, mas a classificação visual direta do validador continua pendente. `STATE-06` permanece não autorizado.
- Decisão: Bruno declarou exatamente `AUTORIZO nova amostra humana do WPF 3.0.1 para confirmar ausência de notificação inicial e as três mudanças demonstrativas, sem Narrator, High Contrast, scaling, ações externas ou transição.` A autorização não incluiu shell, mudança de preferência, Agent, API, banco ou canal externo.
- Escopo: um único processo WPF Release normal em `en-GB`, sem argumentos, executado por 330 segundos; observação auxiliar em memória filtrada apenas pelo título e pelas três mensagens esperadas; leitura compartilhada restrita à presença desses textos no store/WAL local do Windows; nenhuma ativação ou CloseRequest do Tray/shell.
- Gates: aos 15,5 segundos o processo estava responsivo e sem janela principal; nenhuma das três mensagens apareceu no store local nos primeiros 120 segundos, e a UI Automation não expôs título ou mensagem alvo. Analytics materializou aos 127,7 segundos, Regional orders aos 187,7 segundos e Primary finance aos 276,6 segundos. O processo estava responsivo novamente ao concluir a observação e foi encerrado pelo PID próprio, sem processo residual.
- Evidências: os três textos `en-GB` exatos apareceram separadamente no store/WAL local. O listener UI Automation e duas inspeções filtradas da Central de Notificações não expuseram banner correspondente. Classificação técnica corrente: solicitada/materializada localmente, visibilidade não observada; supressão do Shell e limite de automação permanecem indistinguíveis sem o relato direto de Bruno.
- Riscos/ressalvas: presença de texto no store privado do Windows é evidência auxiliar de aceitação/materialização, não prova de apresentação visível. Nenhum screenshot amplo ou conteúdo de outra notificação foi coletado. Narrator, High Contrast, scaling, acessibilidade restante, integração, provider real, ação externa e transição permaneceram fora do escopo.
- Aprovador: Bruno autorizou a execução; nenhuma decisão de aprovação/reprovação da amostra, do Human Gate ou da progressão foi inferida. A classificação visual de cada uma das três entradas ainda deve ser fornecida por Bruno.

## 2026-07-16 — Elevação estratégica e consolidação canónica de MOD-12

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente; `MOD-12 AIOPS_AI` documentado como roadmap `OBSERVER`-first.
- Estado resultante: sem transição; `MOD-12` passa a ser o principal diferencial estratégico do produto sem autorizar integração, coleta automática de dataset, LLM externo, executor, homologação ou promoção de modo.
- Decisão: Bruno solicitou `Leia e execute: Prompt-IA.md, mas antes coloque este arquivo no lugar certo e com o nome correto` e declarou `Quero que este seja o grande diferencial do projeto`. A instrução foi consolidada no proprietário canónico `foundation/AIOps-And-AI-Module.md` em vez de criar autoridade duplicada.
- Escopo: prioridade estratégica permanente, duração estimada em planos, proibição explícita de exclusão autónoma de dados, preservação de logs sanitizados e remoção do rascunho avulso da raiz. A execução funcional fica limitada ao primeiro incremento local, determinístico, não mutável e sem integração do modo `OBSERVER`, sujeito a evidência própria.
- Gates: decisão e consolidação documental não aprovam implementação, Human Gate ou progressão; qualquer resultado técnico do incremento `OBSERVER` deve ser registrado separadamente depois dos checks reais.
- Evidências: `AGENTS.md`, visão do produto, especificação e guardrails AIOps/IA, changelog do corpus e ausência do arquivo avulso `Prompt-IA.md`.
- Riscos/ressalvas: prioridade de produto não reduz segurança, governança de dados, separação de responsabilidades, provider neutrality, gates independentes por modo ou autoridade humana sobre ações destrutivas.
- Aprovador: Bruno definiu a prioridade e solicitou a execução; nenhuma promoção de estado ou modo foi inferida.

## 2026-07-16 — Fundação local inativa MOD-12 Observer

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente; MOD-12 priorizado estrategicamente, sem modo operacional ativo.
- Estado resultante: sem transição e sem promoção `none → OBSERVER`; fundação local/in-memory não mutável implementada e Quality Gate automático aprovado somente nesse escopo.
- Decisão: executar o menor incremento técnico real compatível com os guardrails: contrato de evidência sanitizada/scope-bound, thresholds determinísticos e previsão explicável de capacidade, sem UI demonstrativa, dependência nova, coleta, dataset, integração, persistência, rede, LLM, recomendação, plano ou executor.
- Escopo: `DBNotifier.Application.AIOps`; seleção fail-closed de evidência, proveniência/versionamento/classificação/sanitização/retenção/finalidade, freshness e validade, debounce, OLS com RMSE/R²/IC95 da taxa, sensibilidade temporal explicitamente não apresentada como IC de exaustão, horizon uncertainty, bounds/cancelamento; 34 regressões focadas, allowlist arquitetural, relatório e estado factual.
- Gates: restore locked aprovado; build Release da solução completa em output isolado com zero avisos/erros; `219/219` unitários e `14/14` arquitetura; cobertura `79,62%` linhas/`58,4%` branches; format, documentação, links Markdown, secret scan, diff e runtime fail-closed aprovados. Auditorias online NuGet/npm não foram repetidas porque nenhuma dependência mudou.
- Evidências: `docs/MOD-12-Observer-Foundation-Report.md`, namespace Application AIOps, `AIOpsObserverTests` e allowlist `AIOpsObserverPublicSurfaceExposesOnlyApprovedContracts`.
- Riscos/ressalvas: flags de segurança/autoridade são assertions do caller até existir adapter confiável; não há caching de janelas, orçamento/backpressure de pipeline, eval dataset, poison/red-team fixture, integração ou homologação. O processo visível preexistente `DB Notifier` PID 6976 bloqueou apenas a cópia no output WPF normal, permaneceu intocado e o build isolado do mesmo projeto passou.
- Aprovador: incremento solicitado por Bruno; Quality Gate automático não constitui Human Gate, ativação de modo ou progressão de lifecycle.

## 2026-07-16 — Classificação humana da notificação Analytics

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente; as três transições demonstrativas `3.0.1` estavam materializadas localmente, sem classificação visual direta.
- Estado resultante: sem transição; Analytics passa a ter classificação humana `VISÍVEL`, enquanto Regional orders e Primary finance permanecem pendentes.
- Decisão: Bruno respondeu exatamente `1. VISÍVEL`, interpretado conforme a lista ordenada apresentada no hand-off anterior, em que o item 1 corresponde a Analytics.
- Escopo: registro da observação humana da primeira das três notificações já executadas; nenhuma nova execução, mudança de preferência, ação externa, integração, aprovação de amostra completa ou decisão de Human Gate.
- Gates: não aplicável a build/testes, pois não houve mudança de produto; a evidência humana não sobrescreve a ausência observada por UI Automation nem a evidência técnica de materialização local.
- Evidências: resposta direta de Bruno, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Design-System-Implementation-Report.md` e `prompts/state/Current-State.md`.
- Riscos/ressalvas: a classificação limitada de Analytics não restaura automaticamente `S05-HG-011`, não aprova `STATE-05` e não autoriza `STATE-06`; as outras duas classificações e demais amostras continuam necessárias.
- Aprovador: Bruno, somente como validador visual da notificação Analytics.

## 2026-07-16 — Classificação humana da notificação Regional orders

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente; Analytics estava classificada como `VISÍVEL`, enquanto Regional orders e Primary finance permaneciam sem classificação visual direta.
- Estado resultante: sem transição; Regional orders passa a ter classificação humana `VISÍVEL`, enquanto Primary finance permanece pendente.
- Decisão: Bruno respondeu `1. VISÍVEL` dentro da lista explicitamente ordenada `Próximos passos, em ordem`, interpretado conforme o próximo item factual do hand-off anterior, em que o item 1 correspondia a Regional orders. Os pedidos subsequentes para seguir não foram tratados como decisão do Human Gate nem como autorização específica para abrir uma nova aplicação visível ou alterar preferências do sistema.
- Escopo: registro da observação humana da segunda das três notificações já executadas; nenhuma nova execução, mudança de preferência, ação externa, integração, aprovação de amostra completa ou decisão de Human Gate.
- Gates: não aplicável a build/testes, pois não houve mudança de produto; a evidência humana não sobrescreve a ausência observada por UI Automation nem a evidência técnica de materialização local.
- Evidências: resposta direta de Bruno, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Design-System-Implementation-Report.md` e `prompts/state/Current-State.md`.
- Riscos/ressalvas: a classificação limitada de Regional orders não restaura automaticamente `S05-HG-011`, não aprova `STATE-05` e não autoriza `STATE-06`; Primary finance e as demais amostras continuam necessárias.
- Aprovador: Bruno, somente como validador visual da notificação Regional orders.

## 2026-07-16 — Classificação humana de Primary finance e finding de cobertura de transições

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente; Analytics e Regional orders estavam classificadas como `VISÍVEL`, e Primary finance aguardava observação direta.
- Estado resultante: sem transição; Primary finance passa a ter classificação humana `VISÍVEL`, completando as três apresentações da fixture Current-to-Stale. Um novo finding mantém a amostra pendente: não há validação visível de `Stale → Healthy` nem de `Healthy →` todos os demais estados.
- Decisão: Bruno autorizou uma nova execução WPF normal e visível limitada a Primary finance, autorizou separadamente o encerramento do processo preexistente PID `100`, respondeu exatamente `Primary finance: VISÍVEL` e acrescentou `Só não vi teste de Stale para Healthy ou Healthy para todos os outros status`.
- Escopo: o PID preexistente `100` foi encerrado somente após autorização. O build WPF Release sem restore passou com zero avisos/erros. Um único processo novo, PID `31348`, foi executado de 19:59:41 a 20:05:13, permaneceu responsivo e sem janela principal, e foi encerrado após a janela de observação. Nenhum store, banner alheio, Narrator, High Contrast, scaling, Agent, API, banco ou ação externa foi inspecionado ou alterado.
- Gates: a worktree rastreada permaneceu limpa e nenhum processo DB Notifier ficou residual. A inspeção de código confirmou detecção genérica por diferença de status/freshness, mapeamento automático de todos os significados de destino, um caso `Healthy → Maintenance` e eventos canônicos para `Timeout → Healthy` e várias saídas de Healthy; não existe matriz completa correspondente ao finding nem cenário Windows equivalente.
- Evidências: observação direta de Bruno, `TrayInstanceStateChangePolicy`, `DesktopDemonstrationEvidence`, `TrayApplicationController`, `TrayPresentationTests`, `SynchronizationTests`, `docs/STATE-05-Human-Gate-Validation.md`, `docs/STATE-05-Design-System-Implementation-Report.md` e `prompts/state/Current-State.md`.
- Riscos/ressalvas: a fixture operacional é imutável e só envelhece de Current para Stale; ela não pode provar recuperação ou mudanças de health. O finding é de cobertura, não uma falha observada da detecção ou entrega. Uma matriz local adicional e sua publicação Windows exigem autorização própria; nenhuma aprovação do Human Gate, restauração automática de `S05-HG-011` ou progressão foi inferida.
- Aprovador: Bruno, como validador visual de Primary finance e autor do finding de cobertura; nenhuma remediação foi ainda autorizada.

## 2026-07-16 — Matriz isolada de validação de transições 3.0.2

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera, Human Gate pendente e finding de cobertura para `Stale → Healthy` e saídas de `Healthy` ainda aberto.
- Estado resultante: sem transição; Design System `3.0.2` implementado e verificado automaticamente. A amostra Windows visível permanece não executada e `STATE-06` continua não autorizado.
- Decisão: Bruno declarou exatamente `AUTORIZO a remediação local no STATE-05 para criar uma matriz determinística e isolada de validação das notificações Stale → Healthy e Healthy → Degraded/Unavailable/AuthFailed/Timeout/Maintenance/Unknown/Stale, com testes automáticos completos, sem alterar a fixture operacional normal, sem Agent/API/banco externo, ações externas ou transição.` A autorização cobre código/testes locais, não iniciar WPF nem emitir banners.
- Escopo: argumento exato e disabled-by-default `--review-notification-transitions`; oito casos Application imutáveis, ordenados e individualmente identificados; startup notification-area-first silencioso; um caso por tick de 30 segundos; idioma/Tray sem avanço; supressão das transições da fixture normal durante a validação; avanço antes da entrega; texto `caso/total` localizado e verdade local/sem dados externos. A fixture normal, o publisher moderno/fallback, os ativos de marca e as fronteiras de Agent/API/provider/database permaneceram inalterados.
- Gates: build Release da solução completa com zero avisos/erros; `225/225` testes unitários, `14/14` arquitetura e `42/42` Dashboard; cobertura `79,66%` linhas/`58,44%` branches; typecheck/build Vite, .NET format, drift de marca/tokens/localização e runtime fail-closed aprovados. A suíte legada em Windows PowerShell passou 23 testes com um skip condicional e `32,08%` de cobertura; bundle, documentação de 203 arquivos, 191 links Markdown e secret scan passaram. Os gates Git são concluídos com o commit.
- Evidências: `TrayNotificationValidationPolicy`, `TrayNotificationTransitionValidationMatrix`, `TrayApplicationController`, catálogos/adapters gerados, `TrayPresentationTests`, `WpfPresentationContractTests`, Design System `3.0.2`, `REQ-067`, relatório e protocolo de Human Gate atualizados.
- Riscos/ressalvas: nenhum processo WPF foi iniciado e nenhuma das oito notificações foi solicitada nesta remediação; código/testes não provam entrega visível. Windows Shell/Focus Assist continuam soberanos. Preferência persistida, deduplicação durável, acknowledgement, quiet policy, auditoria e estado reconciliado permanecem `STATE-06`.
- Aprovador: implementação automática autorizada por Bruno no limite citado; nenhuma aprovação da amostra, do Human Gate ou da progressão foi inferida.

## 2026-07-16 — Validação humana visível da matriz de transições 3.0.2

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera, Human Gate pendente e `REQ-067` automaticamente completo, com amostra Windows ainda não executada.
- Estado resultante: sem transição; os oito casos locais de `REQ-067` possuem classificação humana `VISÍVEL`. O Human Gate e `STATE-06` permanecem não autorizados.
- Decisão: Bruno autorizou exatamente `AUTORIZO nova amostra humana visível do WPF 3.0.2 com --review-notification-transitions, em execução normal e sem abrir o shell, para observar as oito transições, sem Narrator, High Contrast, scaling, Agent/API/banco externo, ações externas ou transição.` Após a execução, classificou primeiro `Stale → Healthy` como `VISÍVEL` e confirmou `Sim todos visivel` para os sete casos restantes explicitamente enumerados.
- Escopo: build WPF Release sem restore; um único processo com apenas `--review-notification-transitions`; baseline silencioso; oito intervalos de 30 segundos; observação humana das notificações; verificação técnica limitada a PID, responsividade e ausência de janela principal. Nenhum store de notificação, banner alheio, shell, Narrator, High Contrast, scaling, Agent, API, banco ou ação externa foi inspecionado ou alterado.
- Gates: build com zero avisos/erros; PID `30876` iniciado às 21:13:48, responsivo e com `MainWindowHandle = 0` durante toda a janela; encerrado após o oitavo intervalo; nenhum processo DB Notifier residual.
- Evidências: classificação direta de Bruno para `Stale → Healthy` e confirmação conjunta inequívoca para `Healthy → Degraded/Unavailable/AuthFailed/Timeout/Maintenance/Unknown/Stale`; relatório, Human Gate, rastreabilidade e estado atual sincronizados.
- Riscos/ressalvas: a classificação prova apresentação visível apenas para a matriz local determinística nessa máquina/sessão. Não prova estado reconciliado, delivery operacional, preferência durável, outra política Windows, acessibilidade, produção ou integração. A decisão histórica `S05-HG-011` e o Human Gate completo não são restaurados por inferência.
- Aprovador: Bruno, exclusivamente para a visibilidade dos oito casos de `REQ-067`; nenhuma aprovação adicional ou progressão foi inferida.

## 2026-07-16 — Aprovação humana da amostra visual WPF 3.0.2

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente; a amostra visual WPF atual ainda precisava de decisão humana.
- Estado resultante: sem transição; os aspectos visuais observados do WPF `3.0.2` estão aprovados em escopo limitado. Teclado, Narrator, High Contrast, scaling e o Human Gate completo permanecem pendentes.
- Decisão: Bruno autorizou WPF `--show-desktop` para revisar Overview, TopBar, oito destinos, wordmark, caption e scrollbars em `pt-BR`/Light e `en-GB`/Dark a `1180×760` e `820×620`, com automação local limitada e restauração. Depois respondeu exatamente `Amostra visual WPF 3.0.2: APROVADA,` e observou diferenças visuais entre navegador e WPF que podem ser melhoradas posteriormente.
- Escopo: build Release sem restore; um processo WPF PID `24000`; quatro combinações locale/tema/tamanho; navegação por oito destinos através dos AutomationIds do próprio aplicativo; nenhuma inspeção de outra janela. A preferência original `pt-BR`/Light foi restaurada antes do encerramento.
- Gates: build com zero avisos/erros; processo responsivo em todos os ciclos; preferências persistidas restauradas; nenhum processo DB Notifier residual; worktree rastreada limpa.
- Evidências: aprovação direta de Bruno; `HG05-04/05` visual aprovado; Overview, TopBar, wordmark, oito destinos, caption e scrollbar principal observados. As diferenças Web/WPF ficam como observação não bloqueante porque nenhum defeito ou critério específico foi nomeado.
- Riscos/ressalvas: a aprovação não cobre teclado, screen reader, High Contrast, scaling, zoom, Dashboard nem scrollbars de DataGrid/ComboBox popup que não foram forçados visíveis. Não autoriza `STATE-06` nem restaura automaticamente a decisão histórica `S05-HG-011`.
- Aprovador: Bruno, somente para a amostra visual WPF `3.0.2`; nenhuma aprovação adicional ou progressão foi inferida.

## 2026-07-16 — Aprovação humana da amostra visual Dashboard 3.0.2

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente; a amostra visual WPF e as onze notificações locais estavam concluídas, enquanto a amostra visual Dashboard corrente aguardava execução e decisão humana.
- Estado resultante: sem transição; os aspectos visuais observados do Dashboard `3.0.2`, incluindo o modo TV, estão aprovados em escopo limitado. Teclado, Narrator, zoom nativo, estabilidade perante o modo de aplicação do Windows, High Contrast, scaling e as demais amostras nomeadas continuam pendentes.
- Decisão: Bruno autorizou exatamente `AUTORIZO a próxima amostra humana visível do Dashboard 3.0.2 em Chrome dedicado com perfil temporário isolado e preview local, para revisar pt-BR/Light e en-GB/Dark em 1440, 1920, 390 e 320 CSS px, os oito destinos, Overview/KPIs/status/alertas/gráficos/providers, TopBar/wordmark e modo TV com Fullscreen/saída, com automação local limitada à janela dedicada e fechamento ao final; sem tocar meu Chrome existente, sem Narrator, zoom nativo, High Contrast, scaling, Agent/API/banco externo, ações externas ou transição.` Depois respondeu exatamente `Amostra visual Dashboard 3.0.2: APROVADA`.
- Escopo: build Vite de produção; preview local isolado em `127.0.0.1:4187`, PID `22288`; Chrome `150.0.7871.115` dedicado com perfil temporário e CDP somente em `127.0.0.1:9229`; `pt-BR`/Light e `en-GB`/Dark a `1440×900`, `1920×1080`, `390×844` e `320×568`; oito destinos por combinação; dois passes TV/Fullscreen. A automação alterou somente rota, locale, tema e viewport da janela dedicada e restaurou o perfil descartável para `pt-BR`/Light/Overview antes do encerramento.
- Gates: `npm run build` aprovado; 64 observações rota/largura sem overflow horizontal global; Fullscreen nativo verdadeiro, saída visível e ausência de overflow em ambos os passes TV. Uma heurística simples contou 16 ocorrências de elementos sem texto/`aria-label`/`title`, mas a verificação isolada as mapeou exatamente aos campos `search` de Inventory e History nas quatro larguras e dois pares; o código os mantém dentro de `label` com texto localizado, portanto não havia controle sem nome acessível. Chrome/perfil, preview `4187`, listener CDP `9229` e scripts temporários foram encerrados/removidos. O preview preexistente em `4173`, PID `29292`, permaneceu intocado; a worktree rastreada permaneceu limpa.
- Evidências: aprovação direta de Bruno; `HG05-01/02` visual aprovado; `HG05-03` reflow 390/320 aprovado com zoom 200% ainda pendente; `HG05-09` visual Dashboard/WPF aprovado nos elementos observados; `HG05-10` aprovado; relatório do Design System, rastreabilidade e estado atual sincronizados.
- Riscos/ressalvas: a aprovação não cobre operação por teclado, screen reader, zoom nativo, mudança do modo de aplicação do Windows, High Contrast, scaling, diálogo/foco, hora local, amostra narrow-desktop, a repetição atualizada do Tray/flyout, Agent/API/database, dados externos, ação administrativa ou produção. Emulação de CSS px não prova zoom nativo; o fixture continua demonstrativo e o refresh API TV continua em `STATE-06`.
- Aprovador: Bruno, somente para a amostra visual Dashboard `3.0.2`; nenhuma aprovação do Human Gate completo, `S05-HG-011` atualizado ou progressão foi inferida.

## 2026-07-16 — Remediação da ordem de Tab nas grades WPF 3.0.3

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente; a parte Dashboard da amostra somente por teclado passou, enquanto o WPF não concluiu diálogo/en-GB porque células virtualizadas da grade de Configuração tornaram a ordem de Tab dependente do tempo de realização.
- Estado resultante: sem transição; Design System `3.0.3` implementado e verificado automaticamente. Cada DataGrid estritamente somente leitura é um único ponto de Tab, as células não entram individualmente na ordem de tarefas e a repetição humana WPF permanece pendente.
- Decisão: Bruno declarou exatamente `AUTORIZO a remediação local no STATE-05 da navegação por Tab nas grades somente leitura do WPF 3.0.2, com testes automáticos, sem alterar dados operacionais, Agent/API/banco externo, ações externas ou transição.` A autorização cobre somente código, testes e validação local dessa interação.
- Escopo: estilo compartilhado DataGrid/DataGridCell em `MainWindow.xaml`, regressão estrutural em `WpfPresentationContractTests`, contrato normativo e evidência corrente. Nenhuma fixture, dado operacional, idioma, tema, provider, Agent, API, banco, publisher, notificação ou ação administrativa foi alterado.
- Gates: solução Release completa com zero avisos/erros; `225/225` testes unitários, `15/15` arquitetura e `42/42` Dashboard; .NET format, Node/npm toolchain, typecheck/build Vite, marca, tokens, localização, documentação de 203 arquivos, 192 links Markdown e secret scan aprovados. Uma regressão visível automatizada com pausa de `1,1 s` por tecla passou em `pt-BR`/Light e `en-GB`/Dark: oito pontos até confirmação, exatamente duas grades, nenhuma célula virtualizada, foco modal contido, Escape/restauração e ordem reversa.
- Evidências: `MainWindow.xaml`, `WpfPresentationContractTests`, Design System `3.0.3`, `REQ-068`, relatório do Design System, auditoria WPF, protocolo de Human Gate e estado corrente. Preferências WPF foram restauradas byte a byte; processos e scripts dedicados foram removidos.
- Riscos/ressalvas: a regressão técnica não substitui a observação nem a classificação humana. A amostra visual `3.0.2` e a parte Dashboard do teclado permanecem válidas; somente o WPF `3.0.3` de teclado/diálogo deve ser repetido. Narrator, High Contrast, scaling, integração e `STATE-06` não foram autorizados nem inferidos.
- Aprovador: implementação automática autorizada por Bruno no limite citado; nenhuma aprovação humana da repetição, do Human Gate ou da progressão foi inferida.

## 2026-07-17 — Aprovação humana da amostra de teclado WPF 3.0.3

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente; a remediação de `REQ-068` estava automaticamente verificada, mas a repetição humana WPF de teclado/diálogo ainda aguardava decisão.
- Estado resultante: sem transição; a amostra WPF `3.0.3` de teclado/diálogo está aprovada em seu escopo limitado. O Human Gate completo e `STATE-06` permanecem não autorizados.
- Decisão: Bruno autorizou exatamente uma amostra WPF `--show-desktop`, somente por teclado, em `pt-BR`/Light e `en-GB`/Dark para observar Tab/Shift+Tab, as duas grades como pontos únicos, foco visível, diálogo, Escape e restauração do foco, com fechamento/restauração e sem Narrator, High Contrast, scaling, Agent/API/banco externo, ações externas ou transição. Depois respondeu exatamente `Amostra humana de teclado WPF 3.0.3: APROVADA`.
- Escopo: a primeira tentativa foi abortada antes de qualquer tecla porque o Windows recusou a atribuição de foreground. Uma segunda execução dedicada, PID `30228`, percorreu os oito destinos em cada combinação, a sequência de Configuração com exatamente oito pontos incluindo as duas grades e nenhuma célula, contenção modal, Escape com restauração do trigger e ordem reversa.
- Gates: build Release com zero avisos/erros e a baseline já aprovada de `225/225` testes unitários, `15/15` de arquitetura e `42/42` Dashboard; 80 observações de foco visíveis, limitadas e sem bounds ausentes; preferências restauradas byte a byte com hash `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`; processo e script dedicados removidos; nenhum WPF residual. O preview preexistente em `4173`, PID `29292`, permaneceu intocado. A sincronização documental passou o gate de 195 links Markdown locais e `git diff --check`.
- Evidências: decisão direta de Bruno; `REQ-068`; [auditoria WPF](../../docs/STATE-05-WPF-Accessibility-Audit.md); [protocolo de Human Gate](../../docs/STATE-05-Human-Gate-Validation.md); [relatório do Design System](../../docs/STATE-05-Design-System-Implementation-Report.md); estado factual sincronizado.
- Riscos/ressalvas: a aprovação não cobre Narrator, zoom nativo, estabilidade perante o modo de aplicação do Windows, High Contrast, scaling, Tray/flyout, hora local, Alertas em narrow desktop, Agent/API/banco, ações externas ou produção. Não aprova o Human Gate completo, não restaura automaticamente `S05-HG-011` e não autoriza `STATE-06`.
- Aprovador: Bruno, somente para a amostra humana WPF `3.0.3` de teclado/diálogo; nenhuma aprovação adicional ou progressão foi inferida.

## 2026-07-17 — Aprovação humana da campanha combinada visível STATE-05

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente; zoom nativo, modo de aplicações do Windows, High Contrast, scaling, hora local, Alertas narrow-desktop, Tray/flyout corrente e leitor de tela ainda constavam como amostras abertas.
- Estado resultante: sem transição; a campanha combinada está aprovada em seu escopo. Dashboard High Contrast e a cobertura ampla de leitor de tela permanecem sem execução, e a decisão formal exclusiva do Human Gate continua pendente.
- Decisão: depois da execução combinada, Bruno respondeu exatamente `Amostra combinada visível STATE-05: APROVADA. Narrator: AUDÍVEL E COMPREENSÍVEL.` A decisão aprova os elementos realmente observados e ouvidos; não preenche automaticamente a seção de decisão formal do Human Gate.
- Escopo Dashboard: Chrome `150.0.7871.115` dedicado, perfil temporário isolado, preview local `4187` e CDP local `9229`; zoom nativo 200% com `devicePixelRatio 2`; oito destinos em `pt-BR`/Light e `en-GB`/Dark; Alertas em `960` CSS px; hora local; tema explícito Dark durante `AppsUseLightTheme 0 → 1 → 0`. O Dashboard não foi foreground-reviewed em High Contrast e não foi percorrido com Narrator.
- Escopo WPF: executável Release corrente `net10.0-windows10.0.22621.0`, SHA-256 `6F817330E2743FA2A7FA2E29D98206703B691BECE984C4026B02FD1598D921AB`; preferência explícita Light durante o ciclo de modo de aplicações; High Contrast real com `Preto em Alto Contraste`; Inventory DataGrid no mínimo; ComboBox de Configuração expandido; scaling real 125%/120 DPI e 150%/144 DPI; startup normal oculto, ícone real, flyout corrente e Narrator no caminho `pt-BR` das quatro ações.
- Gates: dezesseis observações Dashboard sob zoom 200% sem overflow global; Alertas com três colunas, lacuna direita zero e nenhum overflow; hora visível `BRT`/`GMT-3` para `America/Sao_Paulo`; temas explícitos estáveis; flags High Contrast `126 → 127 → 126`; DataGrid horizontal com aproximadamente `50,19%` de view size; scaling 125% com bounds `1025×775`/view size `69,66%` e 150% com bounds `1230×737`/view size `88,99%`; dez observações de foco do flyout com Narrator. O monitor não ofereceu scaling 200% e os sete itens do ComboBox não geraram scrollbar própria.
- Integridade da evidência: uma execução inicial no output genérico desatualizado `net10.0-windows`, uma tentativa de scaling apenas por registro que permaneceu em 96 DPI e uma primeira tentativa Narrator que perdeu o flyout foram descartadas. Todas as observações WPF contadas foram repetidas no executável corrente. Preferência WPF original, `AppsUseLightTheme`, High Contrast, scaling 100%/96 DPI e Narrator desligado foram restaurados; Chrome/perfil/preview dedicados, WPF e Settings foram encerrados; worktree permaneceu limpa. O preview preexistente `4173`, PID `29292`, ficou intocado.
- Evidências: [protocolo de Human Gate](../../docs/STATE-05-Human-Gate-Validation.md), [auditoria WPF](../../docs/STATE-05-WPF-Accessibility-Audit.md), [relatório do Design System](../../docs/STATE-05-Design-System-Implementation-Report.md), [rastreabilidade](../../docs/STATE-05-Request-Traceability-Audit.md) e estado factual sincronizados.
- Riscos/ressalvas: o Narrator foi aprovado somente como audível e compreensível no flyout WPF `pt-BR`; Dashboard, shell WPF completo, `en-GB`, toda pronúncia e todos os anúncios de estado não foram exercitados. Dashboard High Contrast não foi revisto em foreground. A scrollbar do popup ComboBox não foi gerada, scaling Windows 200% estava indisponível e mixed-DPI/per-monitor não foi provado. Nenhum Agent, API, banco externo, ação externa, execução administrativa ou integração foi exercitado; `STATE-06` permanece não autorizado.
- Aprovador: Bruno, exclusivamente para a campanha combinada visível e o caminho Narrator ouvido; nenhuma decisão formal do Human Gate ou progressão foi inferida.

## 2026-07-17 — Aprovação formal do Human Gate de STATE-05 com ressalvas

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate pendente após conclusão do relatório automático e das campanhas humanas visual, teclado/diálogo, notificações e combinada.
- Estado resultante: `STATE-05 FRONTEND_IMPLEMENTATION` mantido, progressão em espera e Human Gate `APROVADO COM RESSALVAS`. Nenhuma transição ou workflow de transição foi iniciado.
- Decisão: Bruno declarou exatamente `Human Gate STATE-05: APROVADO COM RESSALVAS. Revisei o relatório automático e as amostras humanas repetidas. Aceito explicitamente as ressalvas registradas. Não autorizo transição automática para STATE-06.` A formulação responde ao resumo exclusivo do gate, nomeia o estado, confirma revisão da evidência e aceita as ressalvas sem ambiguidade.
- Relatório e amostras: baseline final revisada de build Release sem avisos/erros, `225/225` testes unit/model/provider/presentation, `15/15` arquitetura e `42/42` Dashboard, além dos gates documentais; campanhas humanas visuais Dashboard/WPF, teclado/diálogo, onze notificações locais, zoom nativo, modo de aplicações, WPF High Contrast/scaling disponível, hora local, Alertas narrow-desktop, Tray/flyout e Narrator limitado ao flyout WPF `pt-BR`.
- Ressalvas aceitas: Dashboard High Contrast não exercitado; Narrator não exercitado no Dashboard, shell WPF completo ou `en-GB`; popup ComboBox sem scrollbar própria gerada; scaling Windows 200% indisponível; mixed-DPI/per-monitor não provado. Esses itens permanecem fatos ausentes, não passes inferidos.
- Segurança e fase: nenhum Agent, API, banco externo, ação externa, execução administrativa, infraestrutura, homologação ou produção foi exercitado. Integração e delivery operacional continuam pertencentes a `STATE-06`; homologação e controle permanecem nas fases proprietárias.
- Shutdown preflight: a nova mensagem começou com inventário e verificação obrigatórios; nenhum processo DB Notifier estava ativo e nenhum PID precisou ser encerrado.
- Evidências: [Human Gate](../../docs/STATE-05-Human-Gate-Validation.md), [estado atual](Current-State.md), [relatório do Design System](../../docs/STATE-05-Design-System-Implementation-Report.md), [auditoria WPF](../../docs/STATE-05-WPF-Accessibility-Audit.md) e [rastreabilidade](../../docs/STATE-05-Request-Traceability-Audit.md) sincronizados.
- Riscos/ressalvas: a aprovação encerra somente o Human Gate de `STATE-05`; não elimina dívida, não implementa escopos futuros e não autoriza progressão. Uma transição exige autorização posterior, separada e explícita de Bruno.
- Aprovador: Bruno, para o Human Gate de `STATE-05` exclusivamente, com ressalvas e sem autorização de transição.

## 2026-07-17 — Remediação pós-gate das ressalvas correntes de STATE-05

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate `APROVADO COM RESSALVAS`; Dashboard High Contrast/Narrator amplo, popup ComboBox, scaling 200% e mixed-DPI constavam como evidência ausente aceita.
- Estado resultante: sem transição; Design System `3.0.4` implementado no escopo local. A lacuna automática de forced colours e a dívida de configuração system-DPI estão corrigidas; o popup ComboBox possui fixture isolada pronta. As amostras humanas/ambientais restantes não foram inferidas.
- Decisão: Bruno solicitou `Corrigir todas as ressalvas, pendencias`. Pela posição explícita anterior, a execução foi limitada às ressalvas correntes de `STATE-05`; nenhum trabalho proprietário de `STATE-06`, `STATE-07` ou `STATE-08` foi antecipado e nenhuma progressão foi iniciada.
- Escopo: CSS e auditoria forced-colour do Dashboard; entry point WPF com Per-Monitor V2 antes de WPF/WinForms; manifesto `asInvoker`; política exata `--review-combobox-overflow`, template WPF e runner UI Automation; testes e documentação. A fixture normal de sete itens, Agent, API, banco, provider, notificação operacional, ação administrativa, infraestrutura e preferência do usuário não foram alterados.
- Gates: build .NET Release completo com zero avisos/erros; build Vite; `230/230` testes unit/model/provider/presentation, `15/15` arquitetura, cobertura `79,66%` linhas/`58,48%` branches e `42/42` Dashboard; Chrome `150.0.7871.115` aprovou 96 amostras responsivas e 32 rotas forced-colour em `pt-BR`/`en-GB` e Light/Dark. Um processo WPF normal ficou oculto/responsivo, retornou classificação per-monitor aware `2` e foi encerrado sem residual. Toolchain, marca, tokens, localização, .NET format, documentação de 204 arquivos, 210 links, 23 testes Pester com um skip condicional, bundle, secret scan, `git diff --check`, `git fsck --full` e smoke fail-closed passaram. Auditorias online de NuGet/npm não foram repetidas nem inferidas.
- Evidências: Design System `3.0.4`, [relatório de implementação](../../docs/STATE-05-Design-System-Implementation-Report.md), [Human Gate](../../docs/STATE-05-Human-Gate-Validation.md), [auditoria WPF](../../docs/STATE-05-WPF-Accessibility-Audit.md), `REQ-070` e estado factual sincronizados.
- Riscos/ressalvas: a execução automática não substitui Dashboard foreground High Contrast nem fala humana em Dashboard/shell WPF completo/`en-GB`; o popup visível aguarda autorização exata. O monitor continua sem scaling 200% e o host de um monitor não permite prova física mixed-DPI. Esses fatos não são falhas automaticamente corrigíveis nem passes inferidos.
- Aprovador: implementação local autorizada por Bruno no pedido citado; nenhuma nova decisão de Human Gate e nenhuma transição foram inferidas.

## 2026-07-17 — Aprovação humana da amostra final de remediação STATE-05 3.0.4

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate formal `APROVADO COM RESSALVAS`; a remediação automática `3.0.4` estava aprovada, mas Dashboard foreground High Contrast/Narrator amplo e o popup ComboBox isolado aguardavam amostra humana.
- Estado resultante: sem transição; a amostra final `3.0.4` está aprovada e todas as pendências localmente acionáveis de `REQ-070` possuem evidência automática e humana. Scaling físico Windows 200% e movimento mixed-DPI continuam indisponíveis no hardware. O Human Gate formal permanece `APROVADO COM RESSALVAS` porque a aprovação da amostra não foi uma decisão substitutiva do gate.
- Decisão: Bruno autorizou exatamente Dashboard `3.0.4` em Chrome dedicado/perfil temporário e WPF `3.0.4` com `--show-desktop --review-combobox-overflow`, em `pt-BR`/Light e `en-GB`/Dark, usando High Contrast e Narrator, sem tocar seu Chrome, alterar scaling, usar Agent/API/banco, executar ação externa ou transição. Depois respondeu exatamente `Amostra final visível STATE-05 3.0.4: APROVADA. High Contrast: VISÍVEL. Narrator: AUDÍVEL E COMPREENSÍVEL. Foco final: RESTAURADO.`
- Escopo Dashboard: preview local `127.0.0.1:4194`; Chrome dedicado com perfil temporário isolado e CDP local `127.0.0.1:9235`; oito destinos em cada combinação; foco alternado entre navegação corrente e conteúdo principal; High Contrast real e Narrator ativos. As dezesseis observações registraram forced colours ativo, rota/título/navegação coerentes, foco principal e zero overflow horizontal global.
- Escopo WPF: executável Release `3.0.4` com os dois argumentos exatos; oito destinos em cada combinação; popup isolado em ambos os idiomas. A árvore UI Automation proprietária do `ScenarioSelector` expôs sete `ListItem` descendentes e uma scrollbar range-bearing utilizável; a barra percorreu máximo e mínimo sem mudar a fixture normal.
- Integridade da evidência: uma falha inicial de marshaling nativo ocorreu depois de ativar High Contrast, antes de abrir aplicação ou Narrator; flags `126` foram restauradas imediatamente. Consultas UIA iniciais duplicaram peers do popup a partir da raiz ou filtraram itens recortados como offscreen; uma inspeção limitada ao processo provou os sete descendentes proprietários e o runner final passou com essa fronteira. Esses descartes são falhas do harness, não passes nem defeitos do produto. O helper final recebeu retorno negativo ao solicitar foreground, mas Bruno confirmou diretamente `Foco final: RESTAURADO`.
- Restauração: Chrome/perfil/preview, WPF, Narrator, portas e helpers temporários foram encerrados/removidos; High Contrast voltou a flags `126` e esquema `Preto em Alto Contraste`; DPI permaneceu 96; preferências WPF foram restauradas; nenhum processo DB Notifier permaneceu e a worktree voltou limpa. O registro desta aprovação começou depois de novo shutdown preflight com zero processo e zero listener DB Notifier.
- Gates documentais: verificação de 214 links Markdown locais em 63 arquivos, documentação de 204 arquivos de fonte comment-capable e `git diff --check` aprovados; nenhuma suíte de produto foi repetida porque o incremento contém somente sincronização factual da decisão humana sobre a baseline `3.0.4` já validada.
- Evidências: [Human Gate](../../docs/STATE-05-Human-Gate-Validation.md), [auditoria WPF](../../docs/STATE-05-WPF-Accessibility-Audit.md), [relatório do Design System](../../docs/STATE-05-Design-System-Implementation-Report.md), [rastreabilidade](../../docs/STATE-05-Request-Traceability-Audit.md) e estado factual sincronizados.
- Riscos/ressalvas: não houve scaling Windows, Agent, API, banco externo, ação administrativa, integração, homologação ou produção. Scaling 200% e mixed-DPI só podem ser amostrados quando hardware compatível existir; não são inferidos. Nenhum workflow de `STATE-06` foi iniciado.
- Aprovador: Bruno, exclusivamente para a amostra final visível `3.0.4`; nenhuma nova decisão formal do Human Gate ou autorização de transição foi inferida.

## 2026-07-17 — Decisão formal substitutiva do Human Gate de STATE-05

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, progressão em espera e Human Gate formal `APROVADO COM RESSALVAS`; a remediação automática e a amostra final `3.0.4` estavam aprovadas, e scaling físico 200%/mixed-DPI permaneciam limitações ambientais indisponíveis.
- Estado resultante: `STATE-05 FRONTEND_IMPLEMENTATION` mantido, progressão em espera e Human Gate `APROVADO`. A decisão substitui o resultado corrente anterior sem apagar seu registro. Nenhuma transição ou workflow de `STATE-06` foi iniciado.
- Decisão: Bruno declarou exatamente `Human Gate STATE-05: APROVADO. Revisei o relatório automático, as amostras humanas repetidas e a remediação 3.0.4. Aceito scaling físico 200% e mixed-DPI como limitações ambientais indisponíveis neste hardware. Não autorizo transição para STATE-06.` A formulação nomeia um único estado, confirma a evidência revista, aceita os dois limites ambientais como não bloqueantes e separa explicitamente gate de transição.
- Relatório e amostras: baseline Design System `3.0.4` com build Release sem avisos/erros, `230/230` testes unit/model/provider/presentation, `15/15` arquitetura e `42/42` Dashboard, além das campanhas humanas visual, teclado/diálogo, notificações, combinada e final de High Contrast/Narrator/ComboBox.
- Limites aceitos: scaling físico Windows 200% e movimento mixed-DPI entre monitores continuam sem observação porque o hardware disponível não os oferece. A aprovação não os converte em passes, não amplia suporte e não substitui futura evidência física se o contrato mudar ou hardware adequado ficar disponível.
- Segurança e fase: nenhum Agent, API, banco externo, ação externa, execução administrativa, integração, infraestrutura, homologação, produção ou release foi exercitado. `STATE-06` permanece sem autorização.
- Shutdown preflight: esta nova ação documental começou com inventário e verificação obrigatórios; zero processo e zero listener pertencentes ao DB Notifier foram encontrados, e nenhum PID precisou ser encerrado.
- Gates: verificação de 219 links Markdown locais em 63 arquivos, documentação de 204 arquivos de fonte comment-capable, `git diff --check` e `git diff --cached --check` aprovados. Nenhuma suíte de produto foi repetida porque esta ação somente registra a decisão humana sobre a baseline `3.0.4` já validada.
- Evidências: [Human Gate](../../docs/STATE-05-Human-Gate-Validation.md), [estado atual](Current-State.md), [relatório do Design System](../../docs/STATE-05-Design-System-Implementation-Report.md), [auditoria WPF](../../docs/STATE-05-WPF-Accessibility-Audit.md) e [rastreabilidade](../../docs/STATE-05-Request-Traceability-Audit.md) sincronizados.
- Riscos/ressalvas: o gate aprovado encerra a validação humana de `STATE-05` no escopo registrado, mas não implementa trabalho de integração, homologação ou release e não concede autoridade de progressão. Uma transição exige nova autorização explícita de Bruno.
- Aprovador: Bruno, exclusivamente para o Human Gate de `STATE-05`, sem autorização de transição.

## 2026-07-17 — Transição formal para Integration

- Estado anterior: `STATE-05 FRONTEND_IMPLEMENTATION`, Human Gate formal `APROVADO` e progressão em espera por ausência de autoridade de transição.
- Estado solicitado: `STATE-06 INTEGRATION`.
- Decisão: Bruno declarou exatamente `AUTORIZO a transição formal de STATE-05 para STATE-06 INTEGRATION e, após o registro da transição, autorizo o incremento local e isolado do MOD-12 para adapter confiável de telemetria canônica, política opt-in de dados e avaliações offline, sem LLM, executor, banco externo, ações externas ou promoção automática para OBSERVER.` A autorização é posterior, separada e explícita; ela promove o estado sem alterar a decisão histórica do Human Gate.
- Escopo: registrar a transição e liberar como primeiro incremento somente o trabalho local e isolado de `MOD-12 AIOPS_AI` descrito na decisão. Os demais entregáveis de integração permanecem sujeitos a autorização própria.
- Gates: auditoria automática de `STATE-05` aprovada sobre a baseline Design System `3.0.4`; Human Gate de `STATE-05` formalmente `APROVADO` por decisão substitutiva; autorização explícita de progressão recebida nesta sessão.
- Evidências: [estado atual](Current-State.md), [Human Gate](../../docs/STATE-05-Human-Gate-Validation.md), [relatório do Design System](../../docs/STATE-05-Design-System-Implementation-Report.md), [fundação MOD-12](../../docs/MOD-12-Observer-Foundation-Report.md) e resposta de Bruno nesta sessão.
- Shutdown preflight: inventário por PID, caminho, linha de comando, parentage, porta e janela encontrou zero runtime, navegador dedicado ou listener pertencente ao DB-Notifier; as portas de validação Dashboard `4173`/`4187` estavam livres. A única janela contendo o nome do repositório era o Visual Studio Code do usuário, PID `5056`, preservado por não ser runtime do produto.
- Riscos/ressalvas: esta transição não prova integração, provider, dataset, telemetria externa, homologação ou produção. LLM, recomendação, planejamento, executor, banco externo, coleta/ação externa e promoção automática para `OBSERVER` permanecem proibidos. `none → OBSERVER` continua dependente de Quality Gate e Human Gate próprios.
- Aprovador: Bruno, 2026-07-17.
- Estado resultante: `STATE-06 INTEGRATION`.

## 2026-07-17 — Primeiro incremento MOD-12 de Integration

- Estado anterior: `STATE-06 INTEGRATION`, primeiro incremento explicitamente autorizado depois do registro formal da transição; nenhum modo MOD-12 ativo.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e `none → OBSERVER` continua pendente.
- Decisão: executar exatamente o incremento local e isolado autorizado por Bruno para adapter confiável de telemetria canônica, política opt-in de dados e avaliações offline, sem LLM, executor, banco externo, ações externas ou promoção automática.
- Escopo: adapter de `HealthObservation` para a série provider-neutral de duração; política fail-closed por finalidade, vigência, escopo e par instância-Agent; dataset/evaluation runner in-memory com proveniência, autoridade, classificação, expiração, segmentação e casos adversariais; testes, arquitetura e documentação factual.
- Implementação: handling fields derivam da política, não da telemetria; conteúdo provider-native/erro não é copiado; source/time/evidence/canonical bounds são recusados por códigos estáveis. A política nunca autoriza coleta, persistência ou ativação. O corpus sintético de cinco casos cobre detecção, não detecção, futuro, opt-in desabilitado e cruzamento de instância.
- Gates: `54/54` testes MOD-12, `250/250` unit/model/provider/presentation, `15/15` arquitetura, build Release com zero avisos/erros e cobertura `80,02%` linhas/`60,30%` branches aprovados. .NET format/analyzers, documentação en-GB/XML, secret scan, diff, links Markdown e smoke fail-closed também aprovados.
- Evidências: [relatório do incremento](../../docs/STATE-06-MOD-12-Trusted-Telemetry-Report.md), [estado atual](Current-State.md), [arquitetura](../foundation/Solution-Architecture-Document.md), testes `AIOpsIntegrationTests` e resposta de Bruno nesta sessão.
- Segurança/limites: somente `HealthObservation` do Domain cruza a superfície pública; não há DI/runtime, I/O, dependência nova, provider concreto, banco, credential, rede MOD-12, UI, LLM, recomendação, plano ou executor. O smoke usou apenas loopback e encerrou os processos próprios. Auditorias online não foram repetidas.
- Riscos/ressalvas: a policy provenance ainda depende de futura boundary autenticada; o corpus pequeno e sintético não prova calibração, provider real ou resistência geral a poisoning. Datasets multi-segmento, budget/backpressure, carga, tampering/replay e gates independentes permanecem obrigatórios antes de `OBSERVER`.
- Shutdown preflight: um segundo inventário antes do incremento confirmou zero runtime, navegador dedicado, janela de produto ou listener DB-Notifier. Os processos locais de build/teste/smoke foram encerrados normalmente e a verificação final não encontrou resíduo proprietário.
- Aprovador: Bruno, exclusivamente para este incremento local e isolado; nenhuma promoção de modo ou nova transição foi inferida.

## 2026-07-17 — Segundo incremento restrito MOD-12 de Integration

- Estado anterior: `STATE-06 INTEGRATION`, primeiro incremento MOD-12 concluído e nenhum modo ativo.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e `none → OBSERVER` continua pendente.
- Revisão e decisão: Bruno declarou não ter identificado inconsistência arquitetural evidente no código, estado, log e relatório do primeiro incremento. Com ressalvas evolutivas sobre proveniência, versionamento, corpus, backpressure e documentação, autorizou um novo incremento restrito exclusivamente para proveniência autenticada/revogação da `ObserverDataPolicy`, budget/limites/backpressure e ampliação determinística multi-segmento. A autorização excluiu expressamente promoção, LLM, recomendação, planejamento, execução, coleta/persistência operacional, telemetria/provider runtime, banco externo e novos serviços/workers/APIs de Observer.
- Escopo implementado: grants de política e snapshots de revogação canônicos assinados por ECDSA P-256/SHA-256 sob âncoras públicas distintas; verificação fail-closed de identidade, integridade, vigência e revogação de grant/chave; schema/version independentes para policy e dataset; admissão obrigatória por casos/amostras/unidades de trabalho e limite temporal; métricas offline por três segmentos sintéticos.
- Corpus: nove casos exatos em `fixture-relational/1.0.0/offline-windows`, `fixture-document/2.0.0/offline-linux` e `fixture-keyvalue/3.0.0/offline-container`; três verdadeiros positivos, três verdadeiros negativos, três recusas adversariais, zero falso positivo/negativo e zero adversarial aceito. Os nomes são fixtures e não afirmam suporte/homologação.
- Gates: `65/65` testes AIOps, `261/261` unit/model/provider/presentation, `15/15` arquitetura, build Release com zero avisos/erros e cobertura `80,39%` linhas/`60,80%` branches aprovados. .NET format/analyzers, documentação en-GB/XML, links Markdown, secret scan, diff e smoke fail-closed também aprovados.
- Evidências: [relatório do incremento](../../docs/STATE-06-MOD-12-Authenticated-Provenance-And-Budget-Report.md), [estado atual](Current-State.md), [guardrails AIOps](../../docs/architecture/AIOps-Architecture-Guardrails.md), testes `AIOpsIntegrationTests` e decisão de Bruno nesta sessão.
- Segurança/limites: somente chaves públicas e assinaturas copiadas entram no contexto; chaves privadas de teste são efêmeras e descartadas. Não há issuer/distribuidor runtime, DI, I/O MOD-12, persistência, provider concreto, rede externa, UI, LLM, recomendação, plano ou executor. O smoke usou loopback, manteve workers desabilitados e não inicializou persistência local.
- Riscos/ressalvas: futura emissão server-side, distribuição protegida e rotação de trust anchors/revogação continuam não implementadas. O corpus ainda é sintético e pequeno; poisoning/replay/carga/calibração e limites de memória/concorrência runtime exigem evidência própria antes de qualquer proposta de promoção.
- Shutdown preflight: o inventário inicial encontrou zero runtime, navegador dedicado, janela de produto ou listener DB-Notifier. Durante a validação, helpers de compilação exclusivamente sob `.dotnet` do workspace deixados por timeout foram identificados pelo caminho e encerrados; o IDE e seu build host foram preservados. O build limpo posterior passou.
- Aprovador: Bruno, exclusivamente para este incremento restrito; nenhuma promoção de modo, transição ou autoridade adicional foi inferida.

## 2026-07-17 — Aceitação do segundo incremento restrito MOD-12

- Estado anterior: `STATE-06 INTEGRATION`, segundo incremento restrito MOD-12 concluído e automaticamente validado; nenhum modo MOD-12 ativo.
- Estado resultante: `STATE-06 INTEGRATION` mantido; incremento aceito em revisão documental e `none → OBSERVER` permanece pendente.
- Avaliação: Bruno considerou o resumo do commit `50c6897` consistente com o relatório detalhado, reconheceu a preservação dos limites arquiteturais e julgou internamente coerentes os resultados de build, testes, cobertura e gates apresentados.
- Ressalva da revisão: a avaliação foi baseada na documentação apresentada, sem inspeção direta do diff do commit ou código-fonte, e não substitui code review quando necessário.
- Decisão: Bruno registrou exatamente `Incremento restrito MOD-12 aceito, sem promoção para OBSERVER.`
- Escopo da aceitação: somente o incremento de proveniência autenticada/revogação, separação de confiança, schema/version, budget/backpressure e corpus offline multi-segmento documentado no commit `50c6897`.
- Limites: nenhuma transição, ativação operacional, integração runtime ou autoridade de implementação adicional foi concedida. Novo incremento, runtime ou proposta `none → OBSERVER` exige autorização separada, revisão arquitetural e de segurança, Quality Gate específico e Human Gate dedicado.
- Gates: documentação de `208` arquivos comment-capable, `231` links Markdown locais e `git diff --check` aprovados. Builds, testes e smoke de produto não foram repetidos porque esta ação apenas registra a decisão humana sobre o commit `50c6897` já validado e não altera código ou runtime.
- Evidências: [relatório do incremento](../../docs/STATE-06-MOD-12-Authenticated-Provenance-And-Budget-Report.md), [estado atual](Current-State.md), commit local `50c6897` e decisão de Bruno nesta sessão.
- Shutdown preflight: zero helper, processo, janela de produto ou listener conhecido pertencente ao DB-Notifier; nenhuma interrupção foi necessária.
- Aprovador: Bruno, exclusivamente para a aceitação documental do incremento restrito.

## 2026-07-17 — Remediação pós-code-review do segundo incremento MOD-12

- Estado anterior: `STATE-06 INTEGRATION`, segundo incremento restrito aceito documentalmente e nenhum modo MOD-12 ativo.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, remediação automaticamente validada e `none → OBSERVER` pendente.
- Revisão e decisão: a inspeção direta somente leitura do commit `50c6897` confirmou o escopo e isolamento, mas classificou como Médias a ausência de anti-rollback, a materialização anterior ao budget e a inferência de P-256 apenas por tamanho, e como Baixa a possibilidade de reutilizar âncoras. Bruno autorizou exatamente a remediação local desses pontos, testes e documentação, proibindo runtime, providers operacionais, persistência externa, LLM, executor, serviços, workers, APIs, ações externas e promoção.
- Implementação: a revogação possui sequência positiva assinada e deve coincidir com série/revisão exatas do checkpoint confiável; snapshot anterior é recusado como rollback. DER/OID, ponto EC, importação e curva exportada devem comprovar NIST P-256. Grant e revogação não podem partilhar identificador nem material público. O runner calcula admissão por contagens sem enumerar telemetria, recusa antes de cópia e materializa somente o caso corrente depois da admissão.
- Gates: `71/71` testes AIOps, `267/267` unit/model/provider/presentation, `15/15` arquitetura, build Release com zero avisos/erros, cobertura `80,41%` linhas/`61,17%` branches, .NET format, documentação de `208` fontes, `236` links Markdown em `66` arquivos, secret scan corrente/histórico, diff e staged diff aprovados.
- Evidências: [relatório da remediação](../../docs/STATE-06-MOD-12-Provenance-And-Budget-Remediation-Report.md), [relatório histórico revisado](../../docs/STATE-06-MOD-12-Authenticated-Provenance-And-Budget-Report.md), [estado atual](Current-State.md), testes `AIOpsIntegrationTests` e autorização de Bruno nesta sessão.
- Segurança/limites: nenhuma dependência, DI, rede, arquivo, banco, persistência, provider, UI, coleta, LLM, recomendação, plano, comando ou executor foi acrescentado. O checkpoint é configuração local confiável; emissão, distribuição, avanço atómico e persistência durável continuam futuros. A fonte de telemetria permanece memória do chamador; não há alegação de streaming ou homologação runtime.
- Shutdown preflight: zero processo ou listener proprietário foi encontrado. A única janela com o nome do projeto era o Visual Studio Code do usuário, preservado por ser IDE e não runtime do produto.
- Gate humano: a autorização permitiu implementar a remediação, mas não pré-aprova sua aceitação. A revisão humana deste incremento corretivo permanece separada e não promove modo ou ciclo de vida.
- Aprovador: Bruno, exclusivamente para a execução desta remediação restrita.

## 2026-07-17 — Aceitação humana da remediação pós-code-review MOD-12

- Estado anterior: `STATE-06 INTEGRATION`, remediação do commit `6a5f00f` automaticamente validada, aceitação humana corretiva pendente e nenhum modo MOD-12 ativo.
- Estado resultante: `STATE-06 INTEGRATION` mantido; remediação aceita em seu escopo corretivo e `none → OBSERVER` continua pendente.
- Decisão: Bruno declarou exatamente `Revisão humana da remediação MOD-12 em STATE-06, commit 6a5f00f: ACEITA. Revisei o relatório e os resultados automáticos. Aceito as limitações registradas. Não autorizo promoção para OBSERVER nem novo incremento.`
- Escopo da aceitação: somente anti-rollback por checkpoint exato, validação NIST P-256, âncoras materialmente distintas, admissão anterior à materialização, testes e documentação registrados no commit `6a5f00f`.
- Limites: a decisão não ativa runtime, não promove modo ou ciclo de vida, não autoriza novo incremento e não altera as condições futuras de emissão/distribuição, checkpoint durável, rotação, memória/concorrência runtime, arquitetura, segurança, Quality Gate ou Human Gate.
- Gates: decisão registrada sobre a baseline automática de `71/71` testes AIOps, `267/267` unit/model/provider/presentation, `15/15` arquitetura, build Release sem avisos/erros, cobertura `80,41%`/`61,17%` e demais gates documentados. Nesta ação exclusivamente documental, o gate de `208` fontes comment-capable e `238` links Markdown locais em `66` arquivos passou; nenhum build, teste ou runtime foi repetido.
- Evidências: [relatório da remediação](../../docs/STATE-06-MOD-12-Provenance-And-Budget-Remediation-Report.md), [estado atual](Current-State.md), commit `6a5f00f` e decisão de Bruno nesta sessão.
- Shutdown preflight: zero processo, helper ou listener proprietário foi encontrado; a única janela com o nome do projeto era o Visual Studio Code do usuário e foi preservada.
- Aprovador: Bruno, exclusivamente para a aceitação humana do incremento corretivo, sem autoridade adicional.

## 2026-07-17 — Proposta documental do próximo incremento restrito MOD-12

- Estado anterior: `STATE-06 INTEGRATION`, segundo incremento MOD-12 e remediação aceitos, nenhum modo ativo e nenhuma autoridade de implementação remanescente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, com proposta não autorizante pendente de decisão.
- Solicitação: Bruno declarou exatamente `Solicito uma proposta exclusivamente documental para o próximo incremento restrito de STATE-06, sem implementação, runtime, ações externas ou promoção para OBSERVER.`
- Proposta: desenhar futuramente, ainda sem código, a governança de autoridade/âncoras/revogação/checkpoint/rotação e o envelope de contagem/bytes/memória/tempo/cancelamento/concorrência, acompanhados por threat model, rastreabilidade e vetores de teste especificados.
- Limites: a proposta não autoriza o incremento que descreve; não altera arquitetura normativa, código, runtime, segredo, persistência, Agent/API/UI/provider, corpus operacional, LLM, executor, modo ou ciclo de vida.
- Evidências: [proposta documental](../../docs/STATE-06-MOD-12-Trust-Governance-And-Resource-Envelope-Proposal.md), [estado atual](Current-State.md), [guardrails AIOps](../../docs/architecture/AIOps-Architecture-Guardrails.md) e solicitação de Bruno nesta sessão.
- Shutdown preflight: zero processo, helper ou listener proprietário foi encontrado; a única janela com o nome do projeto era o Visual Studio Code do usuário e foi preservada.
- Gates: links Markdown aprovados (`242` links locais em `67` arquivos), documentação aprovada (`208` fontes comment-capable), secret scan do worktree aprovado e `git diff --check` aprovado. Build, testes e runtime de produto não foram executados porque não são aplicáveis a esta proposta exclusivamente documental.
- Próxima decisão: Bruno poderá ajustar, adiar, rejeitar ou autorizar separadamente apenas o incremento documental proposto. Nenhuma resposta é interpretada como promoção para `OBSERVER`.
- Aprovador: não aplicável; esta entrada registra solicitação e proposta, não aprovação do próximo incremento.

## 2026-07-17 — Incremento documental MOD-12 de governança de confiança e envelope de recursos

- Estado anterior: `STATE-06 INTEGRATION`, proposta documental registrada, nenhum modo MOD-12 ativo e nenhuma implementação adicional autorizada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, pacote documental concluído como candidato de revisão, `ADR-0007` em status `proposed` e `none → OBSERVER` pendente.
- Decisão: depois de concluir que a proposta era coerente, exclusivamente documental e não continha autorização implícita de desenvolvimento, Bruno autorizou exatamente o incremento `MOD-12 — Trust Governance and Resource Envelope Design`, limitado ao mapa de responsabilidades, ADR, contrato conceitual, checkpoint durável, envelope de recursos, threat model, rastreabilidade e plano de testes futuros. A autorização proibiu implementação, código, migrations, runtime, chaves reais, persistência, serviços, ações externas e promoção para `OBSERVER`.
- Entrega: `ADR-0007` compara quatro alternativas e propõe bundle confiável independente do transporte, assertion/delegações autenticadas por funções separadas, papéis/chaves segregados, high-water de época/checkpoint host-owned e envelope explícito. O contrato conceitual define bundle, máquina de estados/quarentena, rotação/recuperação, limites por mínimo confiável, corpus governado, `22` ameaças com owner/controle/vetor, `53` vetores contratuais futuros e três campanhas empíricas posteriores, todos não executados.
- Revisão arquitetural: preserva o verificador MOD-12 puro, aceita somente sucessor direto, diferencia atomicidade local de convergência global e mantém transporte conectado/offline fora da raiz de confiança.
- Revisão de segurança: assinatura não substitui autorização/scope ceiling; restore integral e split view permanecem riscos residuais sem witness/reconciliação independente; root/signers/publicador/checkpoint/auditor possuem responsabilidades incompatíveis.
- Revisão de dados/recursos: o budget atual continua provando apenas casos/amostras/trabalho/tempo locais. Bytes, memória, concorrência, fairness, fila e latência de cancelamento são contratos futuros sem números ou evidência runtime; resultado incompleto é sempre não autorizante.
- Limites: nenhum código, configuração, teste executável, migration, segredo, chave, store, API, Agent, provider, serviço, worker, fila, rede, coleta, persistência, LLM, executor ou runtime foi criado ou executado. O pacote não aceita antecipadamente o ADR e não promove modo ou ciclo de vida.
- Evidências: [relatório do incremento](../../docs/STATE-06-MOD-12-Trust-Governance-And-Resource-Envelope-Report.md), [ADR-0007](../../docs/architecture/ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md), [contrato conceitual](../../docs/architecture/AIOps-Trust-Governance-And-Resource-Envelope.md), [threat model](../../docs/architecture/Threat-Model.md), [guardrails AIOps](../../docs/architecture/AIOps-Architecture-Guardrails.md), [proposta/autorização](../../docs/STATE-06-MOD-12-Trust-Governance-And-Resource-Envelope-Proposal.md) e decisão de Bruno nesta sessão.
- Shutdown preflight: zero processo DB-Notifier encerrado ou remanescente, zero janela bloqueante e zero listener proprietário; a janela do Visual Studio Code do usuário foi identificada e preservada.
- Gates: 266 links Markdown locais em 70 arquivos aprovados, documentação de 208 fontes comment-capable aprovada, secret scan do worktree não ignorado e do histórico Git disponível aprovado, séries completas de 22 ameaças, 53 vetores determinísticos e três campanhas empíricas futuras conferidas, inspeção de escopo aprovada, `git diff --check` e `git diff --cached --check` aprovados; build, testes e runtime de produto são `NÃO APLICÁVEIS` e não foram executados neste escopo documental.
- Human Gate: pendente para o pacote documental; a autorização de elaboração não é aceitação do `ADR-0007` nem do incremento concluído.
- Próxima decisão: Bruno poderá aceitar, aceitar com ressalvas, pedir ajustes ou rejeitar apenas este pacote documental. Qualquer implementação ou proposta `none → OBSERVER` exige nova autoridade e gates próprios.
- Aprovador: Bruno, exclusivamente para executar o incremento documental; nenhuma aceitação humana do resultado foi inferida.

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
