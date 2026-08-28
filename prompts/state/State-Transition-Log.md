# Log de Transições de Estado

## Regras

- Append-only: correções são novas entradas.
- Usar datas ISO 8601.
- Registrar apenas transições ou decisões reais.
- Evidências devem existir e estar sanitizadas.
- Relatório, auditoria ou recomendação não alteram estado sozinhos.
- Toda “Próxima decisão” ou “Próxima ação” pertence ao contexto histórico da
  entrada em que foi registrada; somente `Current-State.md` informa a situação
  e os limites vigentes.

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

## 2026-07-17 — Aceitação humana do incremento documental MOD-12 de governança de confiança

- Estado anterior: `STATE-06 INTEGRATION`, pacote documental do commit `137c889` concluído, Quality Gate documental aprovado, Human Gate do incremento pendente, `ADR-0007` em status `proposed` e nenhum modo MOD-12 ativo.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, incremento documental `ACEITO`, `ADR-0007` mantido como `proposed` e `none → OBSERVER` pendente.
- Decisão: Bruno declarou exatamente `Incremento documental MOD-12 — Trust Governance and Resource Envelope Design, commit 137c889: ACEITO. Revisei o relatório apresentado e aceito as limitações e condições residuais nele registradas. Aceito o ADR-0007 como uma decisão arquitetural documental proposta. Esta decisão não autoriza implementação, runtime, persistência, serviços, ações externas nem promoção para OBSERVER.`
- Alcance da revisão: Bruno registrou que não teve acesso direto aos arquivos locais do `ADR-0007`, contrato conceitual e threat model. A aceitação se baseia no relatório que os resume e referencia; essa delimitação não altera a decisão sobre o incremento, mas não constitui inspeção humana independente desses documentos nem promove o ADR de `proposed` para `accepted`.
- Limitações aceitas: checkpoint local isolado não prova restauração integral silenciosa; split view ainda exige witness ou reconciliação independente; não existem limites operacionais numéricos; nenhum vetor futuro foi executado; nenhuma tecnologia específica de armazenamento, assinatura ou distribuição foi escolhida.
- Evidências: commit `137c889`, [relatório com adendo humano](../../docs/STATE-06-MOD-12-Trust-Governance-And-Resource-Envelope-Report.md), [ADR-0007](../../docs/architecture/ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md), [contrato conceitual](../../docs/architecture/AIOps-Trust-Governance-And-Resource-Envelope.md), [threat model](../../docs/architecture/Threat-Model.md) e decisão de Bruno nesta sessão.
- Shutdown preflight desta ação de registro: zero processo DB-Notifier encerrado ou remanescente, zero janela bloqueante e zero listener proprietário; a janela do Visual Studio Code do usuário, PID `6020`, foi identificada e preservada.
- Gates deste registro: 270 links Markdown locais em 70 arquivos aprovados, documentação de 208 fontes comment-capable aprovada, secret scan do worktree não ignorado e histórico Git disponível aprovado, escopo confirmado em quatro arquivos Markdown, `git diff --check` e `git diff --cached --check` aprovados. Build, testes e runtime de produto não foram repetidos porque esta ação apenas registra a decisão humana e não altera produto ou execução.
- Human Gate: `ACEITO` exclusivamente para o incremento documental. Não é Human Gate de ciclo de vida, ativação de modo, implementação ou operação.
- Limites de autoridade: nenhuma implementação, runtime, persistência, serviço, ação externa, integração ou promoção para `OBSERVER` foi autorizada. Nenhum novo incremento foi autorizado.
- Próxima atividade: nenhuma ação técnica está autorizada. Uma futura mudança de `ADR-0007` para `accepted` exige revisão/decisão separada e inequívoca; qualquer incremento posterior exige nova autoridade e gates próprios.
- Aprovador: Bruno, com revisão limitada ao relatório apresentado e às condições nele resumidas.

## 2026-07-17 — Reauditoria automática direta e correção documental do pacote MOD-12

- Estado anterior: `STATE-06 INTEGRATION`, incremento documental do commit `137c889` aceito com base no relatório, `ADR-0007` mantido como `proposed`, nenhum modo MOD-12 ativo e nenhuma autoridade de implementação remanescente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, pacote documental diretamente inspecionado/corrigido, `ADR-0007` ainda `proposed`, nenhum modo MOD-12 ativo e nenhuma nova atividade autorizada.
- Autoridade: depois de perguntar por que recebia pedidos repetidos de revisão documental, Bruno autorizou o Codex a revisar o pacote do zero e corrigir os erros, declarando exatamente `Você poderia fazer tudo isso, revisar tudo do zero e corrigir, pois eu posso cometer erros, ja você pode fazer isso tudo de forma rapida e melhor que eu.` A ação foi interpretada e mantida como reauditoria/correção do pacote documental MOD-12 já aceito, sem código, runtime ou adoção implícita do ADR.
- Escopo executado: inspeção direta do ADR, contrato conceitual, threat model, guardrails, proposta, relatório, índice de arquitetura, estado atual e histórico; comparação somente leitura com a fronteira inativa existente do MOD-12; correções exclusivamente em Markdown. Nenhum arquivo de implementação, teste executável, configuração, migration ou dependência foi alterado.
- Achados: seis categorias altas e onze médias de desenho futuro foram corrigidas. Elas cobriam denial of service por candidato stale, revogação/recuperação/root/corpus anti-rollback, aprovação/first-install, artefatos cross-epoch e referências criptográficas circulares, quiescência/fencing, separação control/data plane, autenticidade do corpus, composição do envelope, metadados/coordenador/fairness, declarações exatas/máximas e códigos/vetores determinísticos. Não eram vulnerabilidades runtime atuais porque distribuidor, checkpoint, coordenador e autoridade de corpus não existem.
- Correções centrais: quorums e materiais distintos para bootstrap/root-transition/recovery; marker `pristine` independente; `epochContextId` não circular; head exato precommitted; reemissão obrigatória após recovery; revogação cumulativa e limitada; root removal somente após revalidação sob as raízes remanescentes; control plane reservado com limite de scheduler factual; publicação vinculada ao `contextRevision`; release somente após quiescência/fence; composição por classe; memória observada somente empírica; coordenador fenced host/process sem claim fleet-wide/fairness; corpus aprovado/assinado com head monotónico e digest não autorreferente.
- Rastreabilidade corrente: `24` ameaças (`M12-T01`–`M12-T24`) e `68` vetores determinísticos futuros não executados — `24` de confiança/checkpoint, `36` de recursos/backpressure e `8` de corpus — além de três campanhas empíricas. Isso substitui factualmente o snapshot histórico de `22` ameaças/`53` vetores do commit `137c889`, sem reescrever as entradas anteriores.
- Precisão de governança: as expressões históricas `Human Gate do incremento` e `Human Gate: ACEITO` nas entradas anteriores significavam aceitação humana do incremento documental, não um Human Gate canônico do ciclo de vida. Nenhum novo Human Gate ocorreu; `ADR-0007` permanece `proposed` porque a decisão o aceitou explicitamente como proposta documental, não por faltar repetição da revisão.
- Shutdown preflight: zero processo DB-Notifier encerrado ou remanescente, zero janela bloqueante e zero listener proprietário; a janela do Visual Studio Code do usuário, PID `6020`, foi identificada e preservada.
- Gates: links Markdown, documentação, secret scan, `git diff --check`, continuidade/definição dos IDs e catálogo de refusal codes aprovados localmente; os comandos finais e resultados estão no adendo da reauditoria do relatório. Build, testes e runtime de produto são `NÃO APLICÁVEIS` porque nenhum produto/executável foi alterado ou executado.
- Amostra humana operacional: `NÃO APLICÁVEL`; nenhum comportamento executável, interface, serviço ou runtime foi produzido. A reauditoria automática não substitui um futuro Human Gate de ciclo de vida.
- Evidências: [relatório com adendo da reauditoria](../../docs/STATE-06-MOD-12-Trust-Governance-And-Resource-Envelope-Report.md), [ADR-0007 proposto](../../docs/architecture/ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md), [contrato conceitual](../../docs/architecture/AIOps-Trust-Governance-And-Resource-Envelope.md), [threat model](../../docs/architecture/Threat-Model.md), [guardrails AIOps](../../docs/architecture/AIOps-Architecture-Guardrails.md) e [estado atual](Current-State.md).
- Limites de autoridade: nenhuma implementação, persistência, serviço, worker, API, provider, chave real, corpus real, ação externa, runtime, adoção formal do ADR ou promoção para `OBSERVER` foi autorizada.
- Próxima atividade: nenhuma ação nem nova revisão do mesmo pacote está pendente para Bruno. Um novo fluxo começa somente se ele solicitar separadamente adoção do ADR, outro incremento, implementação, integração ou promoção; cada caso exigirá sua própria autoridade e gates.
- Aprovador: Bruno, exclusivamente para a reauditoria e correção documental; nenhuma aprovação arquitetural executiva, de modo ou de ciclo de vida foi inferida.

## 2026-07-17 — Implementação restrita de identidade e Agent Fleet em STATE-06

- Estado anterior: `STATE-06 INTEGRATION`, sem Agent Fleet operacional, issuer de certificado produtivo, enrollment, revogação normalizada, heartbeat/catálogo/assignments correntes ou autorização de promoção.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, fronteira server-side local de identidade e Agent Fleet implementada e Quality Gate automático aprovado, com revisão humana do incremento pendente.
- Autoridade: Bruno declarou `AUTORIZO o próximo incremento restrito de STATE-06 para implementar localmente a integração read-only de identidade e Agent Fleet: enrollment controlado com material exclusivamente de teste, revogação, heartbeat, catálogo e assignments read-only, contratos versionados, persistência e testes E2E somente em sandbox local, além da correção factual do README. Autorizo runtimes temporários exclusivamente locais necessários aos testes, que deverão ser encerrados ao final.` A repetição resumida posterior preservou o mesmo escopo.
- Exclusões preservadas: nenhum banco/credencial operacional, provider ou canal externo, comando administrativo, Start/Stop/Restart, deploy, publicação, LLM, executor, promoção para `OBSERVER` ou transição automática. O Agent worker, monitoring, sincronização e command polling permaneceram desabilitados.
- Shutdown preflight: antes da ação técnica, zero processo/runtime/listener DB-Notifier foi encontrado; a janela do Visual Studio Code do usuário, PID `6020`, foi identificada e preservada.
- Implementação: contratos v1; token one-time com salt/hash e escopo exato; enrollment HTTPS-only com issuer produtivo indisponível; validação CSR/P-256; certificado normalizado e public-key digest; heartbeat com cursor/digest/replay/conflito/gap; assignments completos read-only com ETag; catálogo humano sob `agents.read`; revogação monotónica sob `agents.revoke`; limites de assignments/bytes/scopes/catálogo/certificados; audit append-only; nenhuma referência administrativa carregada no snapshot.
- Persistência/migration: oitava migration total e quinta Server migration adicionada com recusa de duplicidades, backfill conservador, cursor de heartbeat e guard de downgrade que impede apagar evidência normalizada enquanto um Agent novo permanecer ativo. O SQL foi gerado e inspecionado, mas não aplicado a PostgreSQL; `Up`, backfill, concorrência serializável e `Down` permanecem não executados.
- E2E sandbox: Kestrel HTTPS em porta loopback efémera, SQLite nomeado em memória, CA/servidor/CSR/certificado P-256 efémeros e autenticação humana de teste. O fluxo cobriu enrollment/replay sequencial, binding da chave CSR, heartbeat aceito/duplicado/conflitante/com gap, assignment seguro/ETag, RBAC positivo e negativo, revogação idempotente e recusa mTLS posterior. Nenhum command, attempt, health sample, evento, outbox ou notification delivery foi criado.
- Revisão automática independente: encontrou rollback fail-open, ausência de binding do public-key digest, negação RBAC não exercitada, dimensões sem limite e afirmações imprecisas sobre lifetime/replay. Esses pontos foram corrigidos antes do commit. Corridas PostgreSQL reais, retenção sem detalhe, limites máximo+um, múltiplos certificados e composição produtiva permanecem limitações declaradas, não resultados inferidos.
- Gates: build Release dos `14` projetos aprovado com zero avisos/erros; `279/279` testes unit/model/provider/presentation, `15/15` arquitetura e `1/1` E2E aprovados; cobertura `77,36%` linhas/`53,78%` branches acima dos pisos; .NET format, documentação, links Markdown, secret scan, diff e smoke fail-closed aprovados. O smoke confirmou liveness `200`, HTTP `426` inclusive no enrollment, workers Agent desabilitados e ausência de persistência local. Auditorias online NuGet/npm não foram repetidas por dependerem de recurso externo proibido.
- Encerramento de runtime: o smoke encerrou os processos que criou; a verificação final observou `0` processo DB-Notifier e `0` listener proprietário.
- Evidências: [relatório do incremento](../../docs/STATE-06-Agent-Identity-And-Fleet-Integration-Report.md), [protocolo Agent/API](../../docs/architecture/Agent-API-Protocol.md), [modelo lógico](../../docs/data/Logical-Model.md), testes unitários/E2E e decisão de Bruno nesta sessão.
- Human Gate do incremento: `PENDENTE`. A autorização de implementação não é aceitação do resultado e não concede nova autoridade.
- Próxima atividade: Bruno deve revisar uma vez o relatório corrente e responder com aceitação, ressalvas, remediação delimitada ou rejeição. Nenhuma resposta promove automaticamente `OBSERVER`, `STATE-07` ou qualquer integração operacional.
- Aprovador: Bruno, exclusivamente para executar o incremento restrito; nenhuma aceitação humana do resultado foi inferida.

## 2026-07-18 — Remediação factual documental do incremento Agent Identity and Fleet

- Estado anterior: `STATE-06 INTEGRATION`, commit `cc2d828` com Quality Gate automático aprovado somente para o incremento restrito, revisão humana pendente e nenhuma autoridade de implementação remanescente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, relatório/runbook/registros factuais corrigidos, revisão humana do incremento ainda pendente e Quality/Human Gate de saída de `STATE-06` não avaliado.
- Autoridade: Bruno declarou exatamente `AUTORIZO exclusivamente uma remediação documental local do incremento cc2d828, sem alteração de código ou runtime, para corrigir factualmente o relatório, o Migration Runbook e os registros correspondentes quanto ao versionamento dos contratos humanos, alcance da auditoria de recusas de enrollment, concorrência entre assignments e revogação, origem da entropia do token e classificação do gate. Não autorizo novo incremento, ação externa, promoção ou transição de estado.`
- Escopo executado: alterações somente em `docs/STATE-06-Agent-Identity-And-Fleet-Integration-Report.md`, `docs/data/Migration-Runbook.md`, `prompts/state/Current-State.md` e nesta entrada append-only. Nenhum código, configuração, projeto, migration, teste executável, dependência ou artefacto gerado foi alterado.
- Correções factuais: contratos Agent-facing de enrollment/heartbeat/assignments negociam protocolo/schema v1; catálogo/revogação humanos são tipados sob `/api/v1`, mas não negociam headers Agent nem carregam `SchemaVersion`. Somente recusas que alcançam Application/store geram `audit_entries`; HTTPS/protocolo/binding/rate limit recusam antes desse audit. Assignment retrieval lê Agent e projeção em consultas separadas sem snapshot comum à revogação; nenhuma violação foi observada, mas a corrida não prova cancelamento imediato. A fixture gera segredo de teste com 32 bytes criptograficamente aleatórios; não existe fonte/provisionador operacional cuja entropia esteja provada.
- Migration Runbook: a cadeia Server agora inclui a quinta migration `IntegrateAgentFleetIdentity`, seus stores/backfills/guard de `Down` e o limite factual de que `Up`, backfill, corridas serializáveis e `Down` não foram executados em PostgreSQL; SQLite `EnsureCreated` não é apresentado como evidência de migration.
- Classificação de gate: o `APPROVED` original permanece somente para o incremento restrito e sua evidência registrada. O Quality Gate da remediação documental está `APPROVED` pelos checks locais abaixo; a revisão humana do incremento continua `PENDING`; o Quality/Human Gate de saída de `STATE-06` está `NOT EVALUATED`; nenhum novo incremento, `STATE-07`, `OBSERVER` ou ação externa foi autorizado.
- Shutdown preflight: zero processo DB-Notifier foi encontrado ou encerrado, zero processo correspondente permaneceu e zero listener proprietário permaneceu; nenhum navegador, IDE, banco ou processo alheio foi alterado.
- Gates documentais: documentação aprovada para `215` fontes comment-capable; `280` links Markdown locais em `71` arquivos aprovados; secret scan do worktree não ignorado e histórico Git disponível aprovado; `git diff --check` aprovado. A revisão humana confirmou vocabulário en-GB nos trechos alterados, precisão técnica e escopo exato de quatro arquivos Markdown.
- Build, testes e runtime de produto: `NÃO APLICÁVEIS` e não executados, porque a autoridade proíbe runtime e a mudança afeta somente Markdown factual; os resultados do commit `cc2d828` permanecem evidência histórica, não foram promovidos como nova observação.
- Human Gate: continua `PENDENTE` exclusivamente para o incremento Agent Identity and Fleet. Esta remediação não registra aceitação humana nem decisão de ciclo de vida.
- Próxima atividade: Bruno deve revisar a síntese factual corrigida e decidir uma vez entre aceitar o incremento com as limitações registradas ou solicitar novo ajuste específico. Nenhuma decisão promove estado ou concede autoridade adicional automaticamente.
- Aprovador: Bruno, exclusivamente para a remediação documental delimitada; nenhuma implementação, ação externa, promoção ou transição foi inferida.

## 2026-07-18 — Aceitação humana do incremento Agent Identity and Fleet

- Estado anterior: `STATE-06 INTEGRATION`, implementação do commit `cc2d828` e remediação factual documental do commit `c5e3cbd` concluídas, Quality Gates restritos aprovados, revisão humana do incremento pendente e nenhuma autoridade técnica remanescente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, incremento Agent Identity and Fleet `ACEITO COM AS LIMITAÇÕES REGISTRADAS`, Quality/Human Gate de saída de `STATE-06` não avaliado e nenhum modo MOD-12 ativo.
- Decisão: Bruno declarou exatamente `Incremento STATE-06 Agent Identity and Fleet, commits cc2d828 e c5e3cbd: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo novo incremento, promoção nem transição de estado.`
- Alcance: registro factual somente no relatório do incremento, estado atual e histórico append-only. Nenhum código, configuração, migration, teste executável, dependência, runtime ou artefacto gerado foi alterado.
- Limitações aceitas: issuer/provisionador e fonte de entropia operacionais inexistentes; contratos humanos sem negociação de schema Agent; recusas anteriores à Application sem `audit_entries` duráveis; assignment/revogação sem snapshot comum; migration, backfill e corridas serializáveis PostgreSQL não executados; enrollment replay concorrente não exercitado; retenção de detalhe de heartbeat limitada; Agent worker sem enrollment, key store, heartbeat ou aplicação/ack de assignments; limites máximo-mais-um e múltiplos certificados não exercitados; ausência de prova de conteúdo não secreto por schema de provider; nenhuma integração ou ativação operacional.
- Evidências aceitas: commits `cc2d828` e `c5e3cbd`, [relatório do incremento](../../docs/STATE-06-Agent-Identity-And-Fleet-Integration-Report.md), [Migration Runbook](../../docs/data/Migration-Runbook.md), resultados automáticos históricos e decisão de Bruno nesta sessão.
- Shutdown preflight desta ação de registro: zero processo DB-Notifier encontrado ou encerrado, zero processo correspondente remanescente e zero listener proprietário; nenhum navegador, IDE, banco ou processo alheio foi alterado.
- Gates deste registro: documentação aprovada para `215` fontes comment-capable; `282` links Markdown locais em `71` arquivos aprovados; secret scan do worktree não ignorado e histórico Git disponível aprovado; inspeção de escopo e `git diff --check` aprovadas. Build, testes e runtime de produto são `NÃO APLICÁVEIS` e não foram repetidos porque esta autoridade permite somente registrar a decisão humana.
- Decisão humana: `ACEITO COM AS LIMITAÇÕES REGISTRADAS` exclusivamente para o incremento dos commits `cc2d828` e `c5e3cbd`. Isso não é o Human Gate canónico de saída de `STATE-06`, promoção de MOD-12, aprovação operacional nem autorização executiva adicional.
- Limites de autoridade: nenhum novo incremento, código, runtime, ação externa, promoção ou transição foi autorizado. `OBSERVER`, `STATE-07` e toda integração operacional permanecem fora do escopo.
- Próxima atividade: nenhuma ação adicional é exigida de Bruno para este incremento e nenhuma ação técnica está autorizada. Se ele decidir continuar futuramente, o primeiro passo será pedir uma proposta exclusivamente documental para um único incremento restrito de `STATE-06`; qualquer implementação ainda exigirá autorização separada.
- Aprovador: Bruno, com aceitação expressamente limitada às evidências e limitações registradas.

## 2026-07-18 — Proposta documental do próximo incremento Agent-side em STATE-06

- Estado anterior: `STATE-06 INTEGRATION`, incremento server-side Agent Identity and Fleet dos commits `cc2d828` e `c5e3cbd` aceito com limitações, nenhum novo incremento autorizado e nenhum runtime operacional ativo.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, com proposta não autorizante pendente de decisão e nenhuma alteração de produto.
- Solicitação: Bruno declarou exatamente `Apresente uma proposta exclusivamente documental para o próximo incremento restrito de STATE-06, sem implementação.`
- Proposta: um futuro incremento `Agent-side Test Identity and Read-only Assignment Reconciliation`, limitado a enrollment exclusivamente de teste, heartbeat com replay durável, aplicação atômica/LKG de assignments no SQLite Agent, comportamento offline/revogado/incompatível e E2E sandbox sem provider ou monitoramento.
- Limites: a proposta não autoriza implementação, código, configuração, migration, runtime, segredo, key store/issuer/token service operacional, provider, monitoramento, comando, UI, canal, ação externa, MOD-12, promoção ou transição.
- Evidências: [proposta documental](../../docs/STATE-06-Agent-Side-Identity-And-Assignment-Reconciliation-Proposal.md), [estado atual](Current-State.md), [protocolo Agent/API](../../docs/architecture/Agent-API-Protocol.md), ADR-0002/ADR-0003 aceitos, baseline dos commits `cc2d828`, `c5e3cbd` e `27673d8`, e solicitação de Bruno nesta sessão.
- Shutdown preflight: zero processo DB-Notifier encontrado ou encerrado, zero processo correspondente remanescente e zero listener proprietário; nenhum navegador, IDE, banco ou processo alheio foi alterado.
- Gates desta proposta: documentação aprovada para `215` fontes comment-capable; `286` links Markdown locais em `72` arquivos aprovados; secret scan do worktree não ignorado e histórico Git disponível aprovado; escopo exato de três arquivos Markdown e `git diff --check` aprovados. Build, testes e runtime de produto são `NÃO APLICÁVEIS` e não foram executados neste escopo exclusivamente documental.
- Próxima decisão: Bruno poderá ajustar, adiar, rejeitar ou autorizar separadamente somente o incremento proposto. Nenhuma decisão promoverá `OBSERVER`, encerrará `STATE-06` ou autorizará `STATE-07` automaticamente.
- Aprovador: não aplicável; esta entrada registra solicitação e proposta, não aprovação do incremento descrito.

## 2026-07-18 — Implementação Agent-side sandbox em STATE-06

- Estado anterior: `STATE-06 INTEGRATION`, fronteira server-side Agent Identity and Fleet aceita, proposta Agent-side não executiva registrada, nenhum runtime Agent Fleet operacional e nenhum novo modo/estado autorizado.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, incremento Agent-side sandbox tecnicamente concluído, Quality Gate automático restrito aprovado e decisão humana do incremento pendente.
- Autoridade: Bruno declarou exatamente `AUTORIZO o incremento restrito de STATE-06 — Agent-side Test Identity and Read-only Assignment Reconciliation, limitado a enrollment exclusivamente de teste, identidade privada somente em adapter E2E efêmero, heartbeat durável, reconciliação read-only de assignments, persistência/migration Agent SQLite e testes locais sandbox, mantendo todos os runtimes desabilitados por padrão e encerrados após os testes. Permanecem proibidos recursos operacionais ou externos, providers, monitoramento, comandos, UI, notificações, LLM, executor, promoção e transição de estado.`
- Shutdown preflight: antes da ação técnica, zero processo DB-Notifier e zero listener proprietário foram observados; nenhum processo, banco, browser, IDE ou recurso alheio foi alterado.
- Implementação: portas e coordinator Agent-side one-shot; enrollment exclusivamente de teste; validação CSR/certificado P-256; transporte HTTPS v1 limitado a enrollment/heartbeat/assignments; referência opaca e metadados públicos em SQLite; envelope heartbeat pendente/replay exato; digest de assignments verificável pelo cliente; ETag, limites, aplicação transacional completa e LKG/freshness; estados fail-closed de offline, stale, incompatível, expirado, revogado/negado e conflito.
- Isolamento: a única chave privada existe no adapter E2E e é descartada com a fixture. O Worker normal não registra identity adapter, transport ou scheduler; `AgentFleetClient.Enabled` permanece `false` e qualquer tentativa de ativá-lo encerra startup com `agent_fleet.sandbox_only`. Nenhum provider, monitoramento, comando, UI, notificação, LLM ou sistema externo foi chamado.
- Persistência/migration: quarta migration Agent `IntegrateAgentFleetClientState`; registros legados recebem `Conflict`; heartbeat e reconciliação usam `agent_fleet_state`; `Down` recusa enquanto qualquer registration existir. Clean `Up`, recusa, remoção explícita da identidade de teste, `Down` para a migration anterior e novo `Up` passaram em SQLite efêmero; Entity Framework confirmou ausência de model drift.
- E2E: o segundo cenário usa Kestrel HTTPS/mTLS loopback, SQLite Server/Agent separados, CA/token/chave/certificado somente de teste e relógio fixo. Foram provados enrollment, ausência de token no store, resposta perdida, replay após reconstrução lógica do coordinator, avanço monotónico, snapshot/`304`, rejeição de corpo alterado com LKG preservado, substituição completa, revogação e quarentena, com zero efeito operacional.
- Revisão direta: não encontrou defeito Crítico/Alto pendente. Corrigiu achados Médios no digest antes ligado a campo interno, canonicalização JSON, confirmação do envelope heartbeat completo, classificação de `401/403` anterior aos headers de versão, defaults/guard de rollback e achados Baixos no teto total de resposta e semântica de freshness. Uma colisão inicial de output em build paralelo desapareceu na repetição sequencial e foi classificada como ambiental.
- Gates executáveis: build Release completo aprovado com zero avisos/erros; `283/283` unit/model/provider/presentation, `15/15` arquitetura, `2/2` E2E e `7/7` testes focados aprovados; cobertura `76,25%` linhas/`48,50%` branches acima de `70%`/`45%`; .NET format/analyzers, migration/model drift e smoke fail-closed aprovados.
- Gates de repositório: documentação aprovada para `223` fontes comment-capable; `293` links Markdown locais em `73` arquivos; secret scan do worktree não ignorado e histórico disponível aprovado; `git diff --check` aprovado; `git fsck --full` saiu com sucesso e listou apenas objetos antigos não alcançáveis.
- Cleanup: o smoke encerrou API/Agent temporários e a verificação final observou zero processo DB-Notifier e zero listener proprietário.
- Evidências: [relatório Agent-side](../../docs/STATE-06-Agent-Side-Identity-And-Assignment-Reconciliation-Report.md), [proposta com adendo factual](../../docs/STATE-06-Agent-Side-Identity-And-Assignment-Reconciliation-Proposal.md), [protocolo](../../docs/architecture/Agent-API-Protocol.md), [modelo lógico](../../docs/data/Logical-Model.md), [Migration Runbook](../../docs/data/Migration-Runbook.md), código e testes locais.
- Limitações: sem key store/issuer/token provisioner/PKI operacional; sem scheduler/backoff contínuo; restart apenas lógico; expiração/incompatibilidade não dirigidas separadamente no novo E2E; sem concorrência/crash/disk-full SQLite; sem PostgreSQL, provider, rede/identidade externa, acknowledgement operacional ou activation de assignments. Auditorias online não foram executadas e nenhum resultado antigo foi inferido.
- Classificação: Quality Gate automático `APROVADO` somente para este incremento restrito; Human Gate do incremento `PENDENTE`; Quality/Human Gate de saída de `STATE-06` `NÃO AVALIADO`; promoção, `OBSERVER`, `STATE-07` e release não autorizados.
- Próxima atividade: Bruno deve revisar uma única vez o relatório/commit e responder com aceitação das limitações, remediação especificamente delimitada ou rejeição. Nenhuma dessas opções autoriza automaticamente outro incremento ou transição.
- Aprovador: Bruno autorizou a execução delimitada; a aceitação humana do resultado não foi inferida.

## 2026-07-18 — Aceitação humana do incremento Agent-side sandbox

- Estado anterior: `STATE-06 INTEGRATION`, commit `beb936b` tecnicamente concluído, Quality Gate automático restrito aprovado, decisão humana do incremento pendente e nenhuma promoção/transição autorizada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, incremento Agent-side Identity and Assignment Reconciliation `ACEITO COM AS LIMITAÇÕES REGISTRADAS`, sem runtime operacional ou nova autoridade.
- Decisão: Bruno declarou exatamente `Incremento STATE-06 Agent-side Identity and Assignment Reconciliation, commit beb936b: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo novo incremento, promoção nem transição de estado.`
- Alcance: registro factual somente no relatório, adendo factual da proposta, estado atual e histórico append-only. Nenhum código, configuração, migration, dependência, teste executável, runtime ou artefacto gerado foi alterado.
- Limitações aceitas: identity adapter/key P-256 somente no E2E; ausência de key store, issuer, token provisioner, trust distribution, rotação e recovery operacionais; Worker sem scheduler/transporte ativável; restart apenas lógico; expiração/incompatibilidade sem cenário Agent-side E2E separado; concorrência/crash/disk-full SQLite não exercitados; PostgreSQL/rede/identidade externas ausentes; classificação não secreta de futuros provider schemas não provada; nenhum acknowledgement operacional, provider, monitoring, UI, notification, LLM ou ação externa.
- Evidências aceitas: commit `beb936b`, [relatório Agent-side](../../docs/STATE-06-Agent-Side-Identity-And-Assignment-Reconciliation-Report.md), resultados automáticos registrados, revisão direta e decisão de Bruno nesta sessão.
- Shutdown preflight: workspace limpo no commit `beb936b`, zero processo DB-Notifier e zero listener proprietário; nenhum processo, banco, browser, IDE ou recurso alheio foi alterado.
- Build, testes e runtime: `NÃO APLICÁVEIS` e não repetidos, porque esta autoridade permite somente registro Markdown factual. Os resultados do commit `beb936b` permanecem evidência histórica e não são promovidos como nova observação.
- Gates documentais: documentação aprovada para `223` fontes comment-capable; `294` links Markdown locais em `73` arquivos; secret scan do worktree não ignorado e histórico disponível aprovado; inspeção de escopo e `git diff --check` aprovadas.
- Classificação: decisão humana `ACEITA COM AS LIMITAÇÕES REGISTRADAS` somente para o incremento; Quality/Human Gate de saída de `STATE-06` continua `NÃO AVALIADO`; `OBSERVER`, `STATE-07`, release, integração externa e novo incremento não autorizados.
- Próxima atividade: nenhuma ação adicional é exigida de Bruno e nenhuma ação técnica está autorizada. Um próximo fluxo só começa se ele solicitar explicitamente uma proposta, remediação ou novo incremento, preservando gates e autoridade separados.
- Aprovador: Bruno, exclusivamente para aceitar o incremento e registrar essa decisão; nenhuma promoção, transição ou autoridade executiva adicional foi inferida.

## 2026-07-18 — Proposta documental de resiliência e compatibilidade Agent Fleet em sandbox

- Estado anterior: `STATE-06 INTEGRATION`, incrementos server-side e Agent-side de identidade/Agent Fleet aceitos com limitações, nenhum runtime operacional ativo e nenhuma nova implementação autorizada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, com uma proposta não executiva pendente de decisão e nenhuma alteração de produto.
- Solicitação: Bruno declarou exatamente `Apresente uma proposta exclusivamente documental para o próximo incremento restrito de STATE-06, sem implementação, promoção ou transição de estado.`
- Proposta: um futuro incremento `Agent Fleet Sandbox Resilience and Protocol Compatibility`, limitado a harness multiprocesso local, restart real entre processos de teste, retry/backoff/cancelamento determinísticos, compatibilidade/falhas de protocolo, concorrência/fencing local, fault injection SQLite e prova coordenada da corrida assignment/revogação.
- Limites: esta proposta não autoriza código, configuração, migration, build, teste executável, runtime, segredo, recurso externo, identidade operacional, provider, monitoramento, comando, UI, canal, MOD-12, promoção ou transição.
- Evidências: [proposta documental](../../docs/STATE-06-Agent-Fleet-Sandbox-Resilience-And-Compatibility-Proposal.md), [estado atual](Current-State.md), [relatório Agent-side aceito](../../docs/STATE-06-Agent-Side-Identity-And-Assignment-Reconciliation-Report.md), [protocolo Agent/API](../../docs/architecture/Agent-API-Protocol.md) e solicitação de Bruno nesta sessão.
- Shutdown preflight: zero processo DB-Notifier encontrado ou encerrado, zero processo correspondente remanescente e zero listener proprietário; nenhum navegador, IDE, banco ou processo alheio foi alterado.
- Gates desta proposta: documentação aprovada para `223` fontes comment-capable; `303` links Markdown locais em `74` arquivos aprovados; secret scan do worktree não ignorado e histórico Git disponível aprovado; inspeção de escopo e `git diff --cached --check` aprovadas. Build, testes e runtime de produto são `NÃO APLICÁVEIS` e não foram executados neste escopo exclusivamente documental.
- Próxima decisão: Bruno poderá ajustar, adiar, rejeitar ou autorizar separadamente somente o incremento proposto. Nenhuma decisão promoverá `OBSERVER`, encerrará `STATE-06` ou autorizará `STATE-07` automaticamente.
- Aprovador: não aplicável; esta entrada registra solicitação e proposta, não aprovação do incremento descrito.

## 2026-07-18 — Implementação restrita de resiliência e compatibilidade Agent Fleet em sandbox

- Estado anterior: `STATE-06 INTEGRATION`, proposta documental registrada, incrementos server-side/Agent-side anteriores aceitos, Worker Agent Fleet desabilitado e nenhum runtime operacional autorizado.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, incremento tecnicamente concluído com Quality Gate automático restrito aprovado e Human Gate próprio pendente.
- Autorização: Bruno declarou exatamente `AUTORIZO o incremento restrito de STATE-06 — Agent Fleet Sandbox Resilience and Protocol Compatibility, limitado a harness e runtimes temporários exclusivamente locais, reinício E2E entre processos, retry/backoff/cancelamento determinísticos, compatibilidade e falhas de protocolo, concorrência/fencing local, fault injection SQLite e corrida assignment/revogação, mantendo o Worker normal desabilitado e encerrando todos os processos ao final. Permanecem proibidos recursos operacionais ou externos, PKI/key store/token service operacionais, providers, monitoramento, comandos, UI, notificações, LLM, executor, deploy, promoção e transição de estado.`
- Escopo executado: orquestrador sandbox de retry/cancelamento; lease/fencing monotónico no SQLite Agent; guard de integridade/schema; migration `HardenAgentFleetSandboxResilience`; harness filho loopback com IPC current-user-only; E2E multiprocesso; protocolo negativo/expiração; fault injection/lock/corrupção; consistência assignment/revogação; isolamento arquitetural do Worker e documentação factual.
- Evidência automática: build Release dos 15 projetos sem avisos/erros; `300/300` unit/model/provider/presentation; `16/16` arquitetura; `5/5` integração HTTPS/mTLS/SQLite; cobertura `78,54%` linhas/`52,73%` branches; sem model drift; format aprovado; documentação `229` fontes; `312` links locais em `75` arquivos; secret scan, diff e Git aprovados; smoke fail-closed aprovado.
- E2E principal: processo A persistiu/enviou heartbeat sequência um, foi encerrado pelo PID exato depois da aceitação Server e antes do ack local; processo B reabriu o mesmo store após expiração, obteve fence superior, repetiu o envelope exato e avançou uma única vez; o Server manteve um único efeito.
- Corrida assignment/revogação: snapshot autorizado/committed antes da revogação pode terminar entrega depois apenas como evidência histórica; a próxima autorização após revogação é recusada. O LKG não equivale a autorização atual.
- Revisão direta: nenhum Critical/High não resolvido. Foram corrigidos resolução incompleta do artefacto filho, resultado ainda retryable após budget, `503` não retryable, expectativas da quinta migration e pool SQLite no cleanup.
- Shutdown/cleanup: preflights sem componente DB-Notifier ativo; runtimes temporários locais iniciados somente pelos testes; ao fim, zero processo DB-Notifier, zero listener proprietário e zero diretório `dbnotifier-agent-fleet-sandbox-*` remanescente.
- Limites: identidade/CA/IPC somente de teste; sem key store/PKI/token service/rotação/recovery operacionais; sem scheduler contínuo, multi-host/fleet load, PostgreSQL real, IdP/vault/proxy/provider/monitoring/comando/UI/notificação/LLM/executor/deploy; protocol Agent Fleet somente major `1`; corrupção/fault injection não é certificação de power-loss; revogação não possui push channel.
- Gates: Quality Gate automático `APROVADO` somente para este incremento; Human Gate do incremento `PENDENTE`; saída de `STATE-06`, `OBSERVER`, `STATE-07`, produção e release `NÃO AVALIADOS` e não autorizados.
- Evidências: [relatório](../../docs/STATE-06-Agent-Fleet-Sandbox-Resilience-And-Compatibility-Report.md), [proposta/adendo](../../docs/STATE-06-Agent-Fleet-Sandbox-Resilience-And-Compatibility-Proposal.md), [protocolo](../../docs/architecture/Agent-API-Protocol.md), [modelo lógico](../../docs/data/Logical-Model.md), [Migration Runbook](../../docs/data/Migration-Runbook.md), [threat model](../../docs/architecture/Threat-Model.md), código e testes locais.
- Próxima decisão: Bruno deve revisar o relatório e responder se aceita, aceita com limitações, solicita remediação específica ou rejeita somente este incremento. Nenhuma resposta autoriza novo incremento, promoção ou transição implicitamente.
- Aprovador: pendente; esta entrada não preenche o Human Gate por Bruno.

## 2026-07-18 — Aceitação humana do incremento de resiliência e compatibilidade Agent Fleet em sandbox

- Estado anterior: `STATE-06 INTEGRATION`, commit `cee9cdf` tecnicamente concluído, Quality Gate automático restrito aprovado, decisão humana do incremento pendente e nenhuma promoção/transição autorizada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, incremento `Agent Fleet Sandbox Resilience and Protocol Compatibility` `ACEITO COM AS LIMITAÇÕES REGISTRADAS`, sem runtime operacional ou nova autoridade.
- Decisão: Bruno declarou exatamente `Incremento STATE-06 Agent Fleet Sandbox Resilience and Protocol Compatibility, commit cee9cdf: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo novo incremento, promoção nem transição de estado.`
- Alcance: registro factual somente no relatório, adendo factual da proposta, estado atual e histórico append-only. Nenhum código, configuração, migration, dependência, teste executável, runtime ou artefacto gerado foi alterado.
- Limitações aceitas: identidade/CA/IPC somente de teste; ausência de key store, PKI, token service, trust distribution, rotação e recovery operacionais; ausência de scheduler contínuo, multi-host e fleet load; SQLite local sem certificação de power-loss; revogação sem push channel; somente protocolo Agent Fleet major `1`; sem PostgreSQL real, IdP, vault, proxy, provider, monitoring, comando, UI, notificação, LLM, executor, deploy, produção ou release.
- Evidências aceitas: commit `cee9cdf`, [relatório de resiliência e compatibilidade](../../docs/STATE-06-Agent-Fleet-Sandbox-Resilience-And-Compatibility-Report.md), resultados automáticos registrados, revisão direta e decisão de Bruno nesta sessão.
- Shutdown preflight: workspace limpo no commit `cee9cdf`, zero processo DB-Notifier e nenhum processo, banco, browser, IDE ou recurso alheio alterado.
- Build, testes e runtime: `NÃO APLICÁVEIS` e não repetidos, porque esta autoridade permite somente registro Markdown factual. Os resultados do commit `cee9cdf` permanecem evidência histórica e não são promovidos como nova observação.
- Gates documentais: documentação aprovada para `229` fontes comment-capable; `313` links Markdown locais em `75` arquivos; secret scan do worktree não ignorado e histórico disponível aprovado; inspeção de escopo e `git diff --check` aprovadas.
- Classificação: decisão humana `ACEITA COM AS LIMITAÇÕES REGISTRADAS` somente para o incremento; Quality/Human Gate de saída de `STATE-06` continua `NÃO AVALIADO`; `OBSERVER`, `STATE-07`, release, integração externa e novo incremento não autorizados.
- Próxima atividade: nenhuma ação adicional é exigida de Bruno e nenhuma ação técnica está autorizada. Um próximo fluxo só começa se ele solicitar explicitamente uma proposta, remediação ou novo incremento, preservando gates e autoridade separados.
- Aprovador: Bruno, exclusivamente para aceitar o incremento e registrar essa decisão; nenhuma promoção, transição ou autoridade executiva adicional foi inferida.

## 2026-07-18 — Proposta documental de snapshot autoritativo e reconciliação periódica do modo TV

- Estado anterior: `STATE-06 INTEGRATION`, incrementos Agent Fleet aceitos com limitações, Dashboard ainda demonstrativo, nenhum runtime operacional ativo e nenhum novo incremento autorizado.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, com uma proposta não executiva pendente de decisão e nenhuma alteração de produto.
- Solicitação: Bruno declarou exatamente `Apresente uma proposta exclusivamente documental para o próximo incremento restrito de STATE-06, sem implementação, promoção ou transição de estado.`
- Proposta: um futuro incremento `Dashboard TV Authoritative Snapshot and Periodic Reconciliation Sandbox`, limitado a read model e endpoint versionados/read-only, autenticação exclusivamente de teste, adapter Dashboard sob composição sandbox exata, leitura imediata na entrada, reconciliação serializada de 30 segundos, ETag/`304`, cancelamento, estados fail-closed, preservação factual do último snapshot e testes locais.
- Justificativa factual: o Dashboard ainda usa `buildDemonstrationSnapshot`; o intervalo corrente de 30 segundos apenas recalcula freshness e não lê a API. A leitura imediata e periódica sem sobreposição é critério explícito de `STATE-06`.
- Limites: esta proposta não autoriza código, configuração, contrato executável, dependência, build, teste de produto, runtime, SignalR, notificação, Agent/provider operacional, monitoramento, comando, persistência, banco ou identidade externa, LLM, executor, deploy, promoção ou transição.
- Evidências: [proposta documental](../../docs/STATE-06-Dashboard-TV-Authoritative-Reconciliation-Proposal.md), [estado atual](Current-State.md), [Lifecycle](../governance/Lifecycle.md), código Dashboard/API inspecionado em somente leitura e decisão de Bruno nesta sessão.
- Shutdown preflight: workspace limpo no commit `25b10dc`, zero processo DB-Notifier e nenhum processo, banco, browser, IDE ou recurso alheio alterado.
- Build, testes e runtime: `NÃO APLICÁVEIS` e não executados neste escopo exclusivamente documental.
- Gates documentais: documentação aprovada para `229` fontes comment-capable; `318` links Markdown locais em `76` arquivos; secret scan do worktree não ignorado e histórico disponível aprovado; inspeção factual de código, escopo e `git diff --check` aprovadas.
- Classificação: proposta documental produzida; Quality/Human Gate de implementação e saída de `STATE-06` `NÃO AVALIADOS`; `OBSERVER`, `STATE-07`, release e novo incremento não autorizados.
- Próxima decisão: Bruno poderá ajustar, adiar, rejeitar ou autorizar separadamente somente o incremento proposto. Nenhuma decisão curta será interpretada como implementação, promoção ou transição.
- Aprovador: não aplicável; esta entrada registra solicitação e proposta, não aprovação da implementação descrita.

## 2026-07-18 — Implementação restrita do snapshot autoritativo e reconciliação periódica do modo TV

- Estado anterior: `STATE-06 INTEGRATION`, proposta documental registrada, Dashboard normal demonstrativo, nenhum runtime operacional ativo e nenhuma implementação nova autorizada além deste pedido.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, incremento tecnicamente concluído com Quality Gate automático restrito `APROVADO COM RESSALVA` e decisão humana própria pendente.
- Autorização: Bruno declarou exatamente `AUTORIZO o incremento restrito de STATE-06 — Dashboard TV Authoritative Snapshot and Periodic Reconciliation Sandbox, limitado a contrato versionado e endpoint read-only de snapshot, autenticação exclusivamente de teste, adapter Dashboard ativado somente em sandbox local, leitura imediata ao entrar no modo TV, reconciliação serializada a cada 30 segundos, ETag/304, cancelamento, preservação factual do último snapshot e testes locais determinísticos/E2E, com runtimes temporários encerrados ao final. Permanecem proibidos SignalR, notificações, Agent ou provider operacional, monitoramento, comandos, persistência ou banco externo, IdP/PKI/vault reais, LLM, executor, deploy, promoção e transição de estado.`
- Escopo executado: contrato `dashboard-tv.v1` e validação bounded; fixture imutável em memória; endpoint read-only sob dupla ativação; política/test subject exclusivos de HTTPS loopback; strong ETag/`304`; adapter same-origin sob flag exata; leitura imediata; reconciliação serial após 30 segundos; cancelamento/fencing de sessão; LKG factual; rótulos acessíveis; testes e documentação.
- Isolamento: modo normal continua demonstrativo; a política sandbox não autoriza rotas humanas comuns; nenhum SignalR, notificação, Agent/provider, monitoramento, comando, persistência, banco externo, IdP/PKI/vault real, LLM, executor, deploy, promoção ou transição foi acrescentado.
- Evidência automática: build Release dos `15` projetos sem avisos/erros; `304/304` unitários, `16/16` arquitetura, `8/8` integração e `49/49` Dashboard aprovados; cobertura `78,37%` linhas/`52,34%` branches; format/analyzers, geração, documentação `236` fontes, links, secrets e smoke fail-closed aprovados.
- Revisão direta: corrigiu um achado Alto de política JWT comum inicialmente reutilizada pelo endpoint, três achados Médios de relógio/rejeição/certificado nos testes e um achado Baixo de regressão textual. Nenhum Critical/High permanece aberto no escopo.
- Ressalva automática: o conjunto legado tem um teste preexistente de fixture NuGet que ainda lista `13` projetos embora o baseline `f58dd05` e a solução corrente tenham `15`. A correção desse teste não foi inferida dentro da autoridade restrita do Dashboard; os testes legados restantes não indicaram regressão do produto alterado.
- Shutdown/cleanup: preflight inicial sem componente DB-Notifier; runtimes Kestrel/certificados iniciados apenas por E2E e dispostos; smoke local encerrado; verificação final sem processo ou listener DB-Notifier.
- Evidências: [relatório](../../docs/STATE-06-Dashboard-TV-Authoritative-Reconciliation-Report.md), [proposta/adendo](../../docs/STATE-06-Dashboard-TV-Authoritative-Reconciliation-Proposal.md), [Design System](../../docs/design/DB-Notifier-Design-System.md), [threat model](../../docs/architecture/Threat-Model.md), código e testes locais.
- Gates: Quality Gate automático `APROVADO COM RESSALVA` somente para este incremento; Human Gate do incremento `PENDENTE`; saída de `STATE-06`, `OBSERVER`, `STATE-07`, produção e release `NÃO AVALIADOS` e não autorizados.
- Próxima decisão: Bruno deve revisar o relatório e o commit e responder se aceita com as limitações/ressalva, solicita remediação especificamente delimitada ou rejeita somente este incremento. Nenhuma resposta autoriza novo incremento, promoção ou transição implicitamente.
- Aprovador: pendente; esta entrada não preenche o Human Gate por Bruno.

## 2026-07-18 — Aceitação humana do incremento Dashboard TV em sandbox

- Estado anterior: `STATE-06 INTEGRATION`, commit `70b3960` tecnicamente concluído, Quality Gate automático restrito `APROVADO COM RESSALVA`, decisão humana do incremento pendente e nenhuma promoção/transição autorizada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, incremento `Dashboard TV Authoritative Snapshot and Periodic Reconciliation Sandbox` `ACEITO COM AS LIMITAÇÕES E A RESSALVA REGISTRADAS`, sem runtime operacional ou nova autoridade.
- Decisão: depois de confirmar a leitura das seções de resultado, correções, verificação e limitações, Bruno declarou `Decisão: Aceitar com as limitações e a ressalva o incremento correspondente ao commit 70b3960.`
- Compreensão registrada: Bruno confirmou que o incremento usa somente sandbox local e dados determinísticos/fictícios, sem monitoramento real, bancos, Agents, providers, SignalR, notificações, persistência operacional ou infraestrutura de produção, e que os resultados não comprovam funcionamento operacional.
- Alcance: registro factual somente no relatório, adendo da proposta, estado atual e histórico append-only. Nenhum código, configuração, dependência, teste executável, runtime ou artefacto gerado foi alterado.
- Limitações e ressalva aceitas: todas as condições residuais do relatório; Quality Gate automático restrito com a ressalva preexistente da fixture legada NuGet de `13` projetos contra os `15` do baseline/solução, não relacionada ao diff do incremento.
- Evidências aceitas: commit `70b3960`, [relatório Dashboard TV](../../docs/STATE-06-Dashboard-TV-Authoritative-Reconciliation-Report.md), resultados automáticos e decisão informada de Bruno nesta sessão.
- Shutdown preflight: workspace limpo no commit `70b3960`, zero processo DB-Notifier e zero listener proprietário; nenhum processo, banco, browser, IDE ou recurso alheio foi alterado.
- Build, testes e runtime: `NÃO APLICÁVEIS` e não repetidos, porque esta ação registra somente a decisão em Markdown. Os resultados do commit `70b3960` permanecem evidência histórica e não são apresentados como nova execução.
- Gates documentais: documentação aprovada para `236` fontes comment-capable; `323` links Markdown locais em `77` arquivos; secret scan do worktree não ignorado e histórico disponível aprovado; inspeção de escopo e `git diff --check` aprovadas.
- Classificação: decisão humana `ACEITA COM AS LIMITAÇÕES E A RESSALVA REGISTRADAS` somente para o incremento; Quality/Human Gate de saída de `STATE-06` continua `NÃO AVALIADO`; `OBSERVER`, ativação operacional, deploy, `STATE-07`, release, novo desenvolvimento e transição não autorizados.
- Próxima atividade: nenhuma ação adicional é exigida de Bruno e nenhum novo trabalho técnico está autorizado. Um próximo fluxo só começa com solicitação e autoridade explícitas e separadas.
- Aprovador: Bruno, exclusivamente para aceitar este incremento; nenhuma promoção, transição ou autoridade executiva adicional foi inferida.

## 2026-07-18 — Proposta documental de remediação da fixture legada NuGet

- Estado anterior: `STATE-06 INTEGRATION`, incremento Dashboard TV aceito no commit `70b3960` com uma ressalva automática preexistente da fixture NuGet e nenhuma nova implementação autorizada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, com uma proposta não executiva pendente de decisão e nenhuma alteração de produto ou teste.
- Solicitação: Bruno declarou exatamente `Apresente uma proposta exclusivamente documental para remediar a fixture legada do relatório NuGet de 13 para 15 projetos, sem implementação, promoção ou transição de estado.`
- Diagnóstico observado: `DBNotifier.sln` contém `15` projetos; `nuget-vulnerability-report.complete-empty.json` contém `13`; faltam exclusivamente `DBNotifier.IntegrationTests` e `DBNotifier.AgentFleet.SandboxHost`, ambos `net10.0`. O verificador recusa corretamente a cobertura incompleta.
- Proposta: acrescentar futuramente somente essas duas entradas à fixture positiva, preservar o script fail-closed e as fixtures negativas, executar o verificador offline e o conjunto Pester/legado, e atualizar a documentação factual sem reescrever a evidência histórica.
- Limites: nenhum JSON, PowerShell, teste, solução, projeto, package, lockfile, fonte NuGet ou código foi alterado; nenhum restore, build, teste executável, acesso externo, runtime, promoção ou transição foi autorizado ou executado.
- Evidências: [proposta documental](../../docs/STATE-06-NuGet-Legacy-Fixture-Parity-Remediation-Proposal.md), solução, fixture positiva, verificador NuGet, teste Pester e decisão de Bruno nesta sessão.
- Shutdown preflight: workspace limpo no commit `68b9f4e`, zero processo DB-Notifier e zero listener proprietário; nenhum processo, banco, browser, IDE ou recurso alheio foi alterado.
- Build, testes e runtime: `NÃO APLICÁVEIS` e não executados neste escopo exclusivamente documental.
- Gates documentais: documentação aprovada para `236` fontes comment-capable; `324` links Markdown locais em `78` arquivos; secret scan do worktree não ignorado e histórico disponível aprovado; inspeção de escopo e `git diff --check` aprovadas.
- Classificação: proposta documental produzida; Quality/Human Gate de remediação e saída de `STATE-06` `NÃO AVALIADOS`; `OBSERVER`, `STATE-07`, release e implementação não autorizados.
- Próxima decisão: Bruno pode ajustar, adiar, rejeitar ou autorizar separadamente somente a remediação proposta. A proposta não é autorização implícita.
- Aprovador: não aplicável; esta entrada registra solicitação, diagnóstico e proposta, não aprovação da execução.

## 2026-07-18 — Remediação da paridade da fixture legada NuGet

- Estado anterior: `STATE-06 INTEGRATION`, proposta documental `fd052f2`, fixture positiva com `13` dos `15` projetos e nenhuma execução autorizada até a decisão de Bruno.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, remediação tecnicamente concluída com Quality Gate automático restrito `APROVADO` e decisão humana própria pendente.
- Autorização: Bruno declarou exatamente `AUTORIZO exclusivamente a remediação local da fixture positiva NuGet em STATE-06, limitada a acrescentar os projetos DBNotifier.IntegrationTests e DBNotifier.AgentFleet.SandboxHost com target net10.0 ao relatório sintético complete-empty, executar os testes Pester/offline correspondentes e atualizar a documentação factual. Não autorizo alteração do verificador, solução, projetos, packages, lockfiles, fontes NuGet, código de produto, acesso externo, runtime, promoção ou transição de estado.`
- Escopo executado: duas entradas `net10.0` acrescentadas somente a `nuget-vulnerability-report.complete-empty.json`; relatório, proposta/adendo, ressalva histórica e estado factual atualizados.
- Isolamento: verificador, fixtures negativas, solução, projetos, packages, lockfiles, fontes NuGet e código permaneceram inalterados; nenhum restore, build, acesso externo ou runtime ocorreu.
- Evidência automática: relatório positivo aceito para `15` projetos; fixture inválida e fixture sem frameworks recusadas; gate legado `23` testes com `1` skip condicional aceito; cobertura Pester `32,08%` (`290/904`), acima do piso `25%`.
- Correção da execução de evidência: o primeiro comando composto terminou depois do caso positivo porque `$LASTEXITCODE` não foi definido pelo script PowerShell bem-sucedido; nenhuma ausência de saída foi inferida. Cenários negativos e gate completo foram repetidos explicitamente e aprovados.
- Ressalva histórica: a condição técnica da fixture foi resolvida prospectivamente, sem reclassificar retroativamente o Quality Gate ou a aceitação humana do commit Dashboard TV `70b3960`.
- Shutdown/cleanup: preflight no commit limpo `fd052f2`, zero processo DB-Notifier e zero listener proprietário; nenhum runtime foi iniciado.
- Gates documentais: documentação aprovada para `236` fontes comment-capable; `328` links Markdown locais em `79` arquivos; JSON/paridade, secret scan do worktree não ignorado e histórico disponível, escopo e `git diff --check` aprovados.
- Evidências: [relatório da remediação](../../docs/STATE-06-NuGet-Legacy-Fixture-Parity-Remediation-Report.md), [proposta/adendo](../../docs/STATE-06-NuGet-Legacy-Fixture-Parity-Remediation-Proposal.md), fixture positiva, verificador e testes Pester locais.
- Gates: Quality Gate automático `APROVADO` somente para esta remediação; Human Gate da remediação `PENDENTE`; saída de `STATE-06`, `OBSERVER`, `STATE-07`, produção e release `NÃO AVALIADOS` e não autorizados.
- Próxima decisão: Bruno deve revisar o relatório e responder se aceita, solicita remediação documental/test-data especificamente delimitada ou rejeita somente este resultado. Nenhuma resposta autoriza novo incremento, acesso externo, promoção ou transição implicitamente.
- Aprovador: pendente; esta entrada não preenche a decisão humana por Bruno.

## 2026-07-18 — Aceitação humana da remediação da fixture legada NuGet

- Estado anterior: `STATE-06 INTEGRATION`, commit `4732ed7` tecnicamente concluído, Quality Gate automático restrito aprovado, decisão humana da remediação pendente e nenhuma promoção/transição autorizada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, remediação `NuGet Legacy Fixture Parity` `ACEITA COM AS LIMITAÇÕES REGISTRADAS`, sem runtime operacional ou nova autoridade.
- Decisão: Bruno declarou exatamente `Remediação da fixture legada NuGet, commit 4732ed7: ACEITA COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo novo incremento, acesso externo, promoção nem transição de estado.`
- Alcance: registro factual somente no relatório, adendo da proposta, estado atual e histórico append-only. Nenhuma fixture, script, solução, projeto, package, lockfile, fonte NuGet, código, teste executável ou runtime foi alterado.
- Limitações aceitas: relatório positivo estritamente sintético/offline; ausência de auditoria atual de feeds/advisories; manutenção futura necessária se projetos/TFMs mudarem; skip condicional `pg_isready` não relacionado; ausência de efeito sobre saída de `STATE-06` ou release.
- Evidências aceitas: commit `4732ed7`, [relatório da remediação](../../docs/STATE-06-NuGet-Legacy-Fixture-Parity-Remediation-Report.md), resultados automáticos registrados e decisão de Bruno nesta sessão.
- Shutdown preflight: workspace limpo no commit `4732ed7`, zero processo DB-Notifier e zero listener proprietário; nenhum processo, banco, browser, IDE ou recurso alheio foi alterado.
- Build, testes e runtime: `NÃO APLICÁVEIS` e não repetidos, porque esta autoridade permite somente registro Markdown factual. Os resultados do commit `4732ed7` permanecem evidência histórica e não são promovidos como nova execução.
- Gates documentais: documentação aprovada para `236` fontes comment-capable; `329` links Markdown locais em `79` arquivos; secret scan do worktree não ignorado e histórico disponível, escopo e `git diff --check` aprovados.
- Classificação: decisão humana `ACEITA COM AS LIMITAÇÕES REGISTRADAS` somente para a remediação; Quality/Human Gate de saída de `STATE-06` continua `NÃO AVALIADO`; acesso externo, `OBSERVER`, `STATE-07`, release, novo incremento e transição não autorizados.
- Próxima atividade: nenhuma ação adicional é exigida de Bruno e nenhum novo trabalho está autorizado. Um próximo fluxo só começa mediante solicitação e autoridade explícitas e separadas.
- Aprovador: Bruno, exclusivamente para aceitar esta remediação e registrar a decisão; nenhuma promoção, transição ou autoridade executiva adicional foi inferida.

## 2026-07-18 — Proposta documental de composição do Dashboard TV no navegador

- Estado anterior: `STATE-06 INTEGRATION`, remediação NuGet aceita no commit `4732ed7`, sandbox Dashboard TV aceito no commit `70b3960` e nenhum novo incremento executável autorizado.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, com uma proposta documental não executiva pendente de decisão.
- Solicitação: Bruno declarou exatamente `Apresente uma proposta exclusivamente documental para o próximo incremento restrito de STATE-06, sem implementação, acesso externo, promoção ou transição de estado.`
- Lacuna selecionada: a API HTTPS sandbox e a lógica TypeScript do Dashboard foram testadas em camadas separadas, mas o relatório aceito registra que nenhum E2E da interface dentro de navegador foi executado.
- Proposta: compor futuramente Dashboard TV e API existentes em HTTPS loopback, navegador dedicado e perfil efêmero; provar leitura imediata, cadência serial de 30 segundos, `ETag`/`304`, cancelamento, fencing e matriz determinística de falhas/recuperação; preservar modo normal demonstrativo e limpar integralmente processos/listeners.
- Sequenciamento: SignalR permanece posterior e separado porque é apenas otimização da releitura autoritativa, não existe no Dashboard atual e seu cliente não pertence às dependências bloqueadas atuais.
- Limites: nenhum código, configuração executável, harness, contrato, dependência, build, teste de produto ou runtime foi criado ou alterado; nenhum acesso externo, SignalR, notificação, Agent/provider operacional, monitoramento, persistência, banco, identidade real, comando, LLM, executor, deploy, promoção ou transição foi autorizado ou executado.
- Evidências: [proposta documental](../../docs/STATE-06-Dashboard-TV-Browser-Composition-And-Recovery-Evidence-Proposal.md), [relatório Dashboard TV](../../docs/STATE-06-Dashboard-TV-Authoritative-Reconciliation-Report.md), lifecycle, Design System, dependências locais bloqueadas e decisão de Bruno nesta sessão.
- Shutdown preflight: workspace limpo no commit `eeb5b62`, zero processo DB-Notifier e zero listener proprietário; nenhum processo, banco, browser, IDE ou recurso alheio foi alterado.
- Build, testes de produto e runtime: `NÃO APLICÁVEIS` e não executados neste escopo exclusivamente documental.
- Gates documentais: documentação aprovada para `236` fontes comment-capable; `332` links Markdown locais em `80` arquivos; secret scan do worktree não ignorado e histórico disponível aprovado; inspeção de escopo e `git diff --check` aprovadas. Quality/Human Gate do incremento futuro e saída de `STATE-06` permanecem `NÃO AVALIADOS`.
- Próxima decisão: Bruno pode ajustar, adiar, rejeitar ou autorizar separadamente somente a implementação local proposta. A proposta não concede autoridade implícita.
- Aprovador: não aplicável; esta entrada registra solicitação e proposta, não aprovação de execução.

## 2026-07-18 — Incremento local de identidade visual de providers

- Estado anterior: `STATE-06 INTEGRATION`, incrementos anteriores aceitos em seus escopos, proposta Dashboard TV Browser Composition separada e nenhum runtime operacional ativo.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, incremento `Provider Identity Icons` tecnicamente concluído, Quality Gate automático restrito aprovado e Human Gate próprio pendente.
- Autorização: Bruno solicitou exatamente `Quero que use os icones correspondentes ao banco, icone do PostgreSQL para o banco do PostgreSQL, icone do mongo para identificar os bancos do mongo, você pode encontrar e baixar os icone originais neste site: https://skillicons.dev/, Lembrando que o objetivo da plataforma é aceita todos os bancos de dados, fasça isso para todos os bancos de dados existentes.`
- Interpretação segura: a plataforma preserva qualquer `providerType` válido e usa fallback genérico universal; somente uma identidade exata, local e licenciada pode substituir esse fallback. A presença de um ícone não implementa, homologa nem publica suporte ao provider.
- Escopo executado: Design System `3.1.0`; manifest/proveniência/licença Skill Icons; gerador offline; onze registros e 22 variantes Web/WPF; componente React e controle WPF; Overview, distribuição, inventário, histórico, alertas, configuração, Providers e Tray; fallback de falha/unknown/forced colours/High Contrast; testes e documentação factual.
- Cobertura exata: Cassandra, DynamoDB, Elasticsearch, Firebase, MongoDB, MySQL, PlanetScale, PostgreSQL, Redis, SQLite e Supabase. SQL Server/Azure SQL, MariaDB, Oracle, SAP HANA, Db2, Valkey e qualquer ID não mapeado mantêm o glifo neutro e o texto integral; nenhum logo semelhante foi substituído.
- Proveniência: Skill Icons fixado no commit `7f7e691e71aec64e8354bf697835e009d1ad80f8`; licença MIT integral, blob SHA upstream e SHA-256 local registrados; nenhum download em build ou runtime. Os PNGs WPF foram gerados em perfil Chrome Headless temporário isolado e inspecionados antes do consumo.
- Evidência automática: geração/verificação de `11` identidades e `22` variantes; build Release dos `15` projetos .NET 10 sem avisos/erros; `304/304` unitários; `17/17` arquitetura; `55/55` Dashboard; TypeScript, Vite, .NET format/analyzers, documentação para `242` fontes, links Markdown, secret scan, licença e hashes aprovados.
- Revisão direta: a primeira rasterização WPF recortava o canto do SVG e foi rejeitada visualmente; o gerador passou a escalar a arte completa em documento isolado de 64×64 px. Uma propriedade XAML de outra família foi recusada pelo build e substituída por `Image` decorativa WPF sem Automation peer. A revisão independente final também identificou e remediou a ordenação das colunas WPF, a contenção de identificadores longos, referências SVG/CSS/SMIL e namespaces permissivos, validação PNG insuficiente, derivados órfãos não detetados e LF finais que impediam correspondência byte-exata com onze blobs upstream. Os blobs Git passaram a ser recalculados, o sanitizador ganhou autotestes negativos e as repetições finais passaram.
- Shutdown/cleanup: o preflight inicial observou zero processo DB-Notifier. Geração e testes usaram somente helpers locais limitados; a verificação final encontrou zero runtime, navegador dedicado, helper ou listener correspondente. Nenhum banco, navegador comum, IDE ou processo alheio foi alterado.
- Evidências: [relatório do incremento](../../docs/STATE-06-Provider-Identity-Icons-Report.md), [Design System](../../docs/design/DB-Notifier-Design-System.md), [inventário de assets](../../design-system/provider-icons/README.md), manifest, código e testes locais.
- Limitações: Skill Icons é coleção de terceiros e não prova origem oficial de cada vendor nem concede direitos de marca; a cobertura de assets não é universo completo nem catálogo de suporte; PostgreSQL continua não homologado; demais providers da fixture continuam não implementados; comparação humana cross-platform deste incremento permanece pendente; renderer deve permanecer fixado para regeneração determinística.
- Gates: Quality Gate automático `APROVADO` somente para este incremento local; Human Gate do incremento `PENDENTE`; saída de `STATE-06`, `OBSERVER`, homologação, `STATE-07`, produção e release `NÃO AVALIADOS` e não autorizados.
- Próxima decisão: Bruno deve autorizar e revisar uma amostra visual local dedicada, depois aceitar com as limitações registradas, solicitar remediação específica ou rejeitar somente este incremento. Nenhuma resposta autoriza novo provider, acesso externo, promoção ou transição implicitamente.
- Aprovador: Bruno autorizou a execução delimitada; a aceitação humana do resultado não foi inferida.

## 2026-07-18 — Pesquisa de fontes alternativas para identidades visuais de providers

- Estado anterior: `STATE-06 INTEGRATION`, baseline `Provider Identity Icons` com onze identidades/22 variantes automaticamente aprovada, amostra visual local anterior encerrada e Human Gate próprio ainda pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, pesquisa alternativa concluída sem aceitar novo ativo e baseline visual inalterada.
- Autorização: Bruno declarou exatamente `Os icones que você não conseguiu encontrar busque em outros sites na internet` depois de solicitar ícones correspondentes aos bancos. A autoridade cobriu pesquisa de fontes públicas alternativas dentro do incremento visual; não dispensou revisão de direitos e não autorizou provider, database externo, monitoramento, comando, runtime operacional, deploy, promoção ou transição.
- Critério conservador: identidade exata, direito de cópia/redistribuição do arquivo e permissão de marca aplicável ao uso/suporte factual corrente devem ser evidenciados separada e simultaneamente. Licença de coleção, origem oficial, atribuição ou objetivo futuro não substituem os demais requisitos.
- Pesquisa concluída: o arquivo Firebird do Devicon possui licença MIT, mas a autorização oficial examinada refere-se ao uso junto de backend suportado. Os ativos OpenSearch são oficiais, mas sua política só dispensa permissão para os contextos enumerados, incluindo indicar software ou serviço que usa OpenSearch; outros usos do logo exigem autorização prévia. Como DB-Notifier não implementa, integra, homologa nem suporta publicamente esses providers no estado corrente, nenhum dos dois ativos foi incorporado.
- Fallback preservado: Firebird, OpenSearch, Microsoft SQL Server, Azure SQL, Oracle Database, SAP HANA, IBM Db2, MariaDB, Valkey, CockroachDB, Couchbase, Apache CouchDB, ScyllaDB, InfluxDB, Neo4j e todo ID não mapeado continuam com glifo neutro e nome integral. Nenhuma marca relacionada, corporativa ou de plataforma substitui um engine ausente.
- Isolamento: nenhum código, manifest, gerador, asset, registro React/WPF, teste, projeto, dependência ou saída gerada integra o resultado final. `THIRD-PARTY-NOTICES.md` continua distribuindo somente a atribuição/licença Skill Icons já aceita.
- Evidência de produto: uma invocação preliminar de `node scripts/generate-provider-icon-assets.mjs --generate` foi executada antes da conclusão de direitos e falhou de modo fechado, com exit code `1` e antes de escrever saídas, porque os bytes provisórios Firebird não correspondiam ao blob Git upstream declarado. O source provisório e todas as alterações técnicas relacionadas foram removidos. Depois da rejeição dos candidatos, geração bem-sucedida, rasterização, build, testes Dashboard/.NET, runtime e nova amostra visual ficaram `NÃO APLICÁVEIS` e não foram executados como nova evidência. A evidência automática `11`/`22` anterior permanece histórica e não foi promovida por inferência.
- Evidências documentais: [relatório atualizado](../../docs/STATE-06-Provider-Identity-Icons-Report.md), [Design System `3.1.1`](../../docs/design/DB-Notifier-Design-System.md), [inventário de assets](../../design-system/provider-icons/README.md) e aviso Skill Icons existente.
- Shutdown preflight: nenhum componente DB-Notifier ou listener proprietário permaneceu antes da ação técnica; o único match por command line foi o próprio shell da auditoria.
- Cleanup da pesquisa: os quatro diretórios temporários verificados sob `C:\tmp` (checkouts/downloads Simple Icons, Devicon, Valkey e OpenSearch) foram removidos; eram reproduzíveis a partir das fontes públicas e as quatro verificações finais retornaram ausência.
- Gates documentais: `APROVADOS`; documentação para `242` fontes comment-capable, `342` links Markdown locais em `83` arquivos, secret scan do worktree não ignorado e histórico disponível, escopo documental e `git diff --check` passaram com exit code `0`. Código, manifest, inventário, gerador, assets, registries, testes e notices distribuídos permaneceram sem diff.
- Gates: Quality Gate automático histórico da baseline permanece `APROVADO`; Human Gate do incremento permanece `PENDENTE`; saída de `STATE-06`, `OBSERVER`, homologação, `STATE-07`, produção e release `NÃO AVALIADOS` e não autorizados.
- Próxima decisão: concluir o gate documental deste registro. Como nenhum ativo visual mudou, a pesquisa não exige nova amostra; a decisão humana pendente continua referindo-se ao incremento visual já apresentado anteriormente.
- Aprovador: Bruno autorizou exclusivamente a pesquisa alternativa; a aceitação humana do incremento não foi inferida.

## 2026-07-18 — Aceitação humana de Provider Identity Icons

- Estado anterior: `STATE-06 INTEGRATION`, baseline de onze identidades/22 variantes com Quality Gate automático aprovado, pesquisa alternativa documental aprovada, amostra Dashboard/WPF/Tray encerrada e Human Gate próprio pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e Human Gate de `Provider Identity Icons` aceito com as limitações registradas.
- Decisão: Bruno declarou exatamente `ACEITO o incremento Provider Identity Icons no STATE-06, com as limitações registradas; não autorizo provider, banco externo nem transição de estado.`
- Base informada: a solicitação de decisão identificou o relatório/evidência automática aprovados, a amostra local anterior de Dashboard e WPF/Tray em Light, Dark e High Contrast, a baseline inalterada de onze identidades/22 variantes e o fallback conservador resultante da pesquisa em fontes alternativas. A resposta exata ao pedido condicional confirma a revisão humana sem inferir autoridade adicional.
- Limitações aceitas: cobertura de asset não é implementação, homologação ou suporte; PostgreSQL continua não homologado e suporte público `No`; providers não mapeados preservam glifo neutro e nome integral; Firebird/OpenSearch e os demais candidatos rejeitados só podem ser reconsiderados com direitos e verdade factual próprios.
- Escopo desta ação: registro Markdown factual somente; nenhum código, manifest, inventário, gerador, asset, registry, teste, dependência, runtime ou saída gerada foi alterado.
- Evidências: [relatório Provider Identity Icons](../../docs/STATE-06-Provider-Identity-Icons-Report.md), commits `f93804c`, `cc767f4` e `1bccf47`, decisão explícita de Bruno nesta sessão e preflight sem componente/listener DB-Notifier ativo.
- Gates documentais: `APROVADOS`; documentação para `242` fontes comment-capable, `343` links Markdown locais em `83` arquivos, secret scan do worktree não ignorado e histórico disponível, escopo restrito a quatro documentos e `git diff --check` passaram com exit code `0`. Código, assets, geradores e testes permaneceram sem diff.
- Classificação: Human Gate do incremento `ACEITO COM AS LIMITAÇÕES REGISTRADAS`; Quality/Human Gate de saída de `STATE-06`, homologação, `OBSERVER`, `STATE-07`, produção e release `NÃO AVALIADOS` e não autorizados.
- Próxima atividade: nenhuma ação adicional é exigida de Bruno e nenhum novo trabalho é autorizado. Uma futura identidade/provider exige solicitação e autoridade explícitas separadas.
- Aprovador: Bruno, exclusivamente para o incremento `Provider Identity Icons`.

## 2026-07-18 — Dashboard TV Browser Composition and Recovery Evidence Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, snapshot/reconciliação Dashboard TV do commit `70b3960` aceito, proposta documental de composição no navegador pendente e nenhum runtime operacional ativo.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, incremento local tecnicamente concluído, Quality Gate automático restrito aprovado e Human Gate próprio pendente.
- Autorização: Bruno autorizou explicitamente host HTTPS loopback temporário, identidade exclusivamente de teste, Dashboard TV/API sandbox num navegador dedicado com perfil efêmero, leitura imediata/cadência serial 30 segundos, ETag/304, cancelamento, fencing, matriz de falhas/recuperação, evidência sanitizada e cleanup. Ele proibiu acesso externo, novas dependências/downloads, SignalR, notificações, Agent/provider operacional, monitoramento, persistência/banco externo, IdP/PKI/vault reais, comandos, LLM, executor, deploy, promoção e transição.
- Escopo executado: novo projeto de teste `DBNotifier.DashboardTv.BrowserSandboxHost`; certificado P-256 curto e exclusivamente de teste; composição estática/API na mesma origem; runner PowerShell de Chrome/Edge instalado; audit CDP nativo; cenários determinísticos; deadline de pedido de 10 segundos; regressões do receptor Fetch, timeout e navegação; fixture NuGet ampliada para 16 projetos; Design System factual `3.1.2` e documentação correspondente.
- Revisão direta: foram encontrados e corrigidos dois achados altos (receptor incorreto do Fetch nativo e ausência de deadline por pedido), três médios (corrida de navegação, escala impossível do cenário lento e arranque do browser sem fail-fast) e um baixo (ruído inicial da solução). Nenhum achado alto/médio conhecido permaneceu no diff final.
- Evidência browser: `Chrome/150.0.7871.125`; ativação ausente/inválida recusada com exit code `2`; leitura imediata `200`; releitura condicional `304`; intervalo real mínimo `30.020 ms`; concorrência máxima `1` em todos os cenários; preservação/recuperação para denied, incompatível, malformado, oversized, erro, timeout e offline; cancelamento/fencing e pausa/retomada aprovados; `677` requisições HTTP/HTTPS observadas pelo CDP e `0` origem externa.
- Gates: build Release dos 16 projetos com zero aviso/erro; `304/304` unitários, `17/17` arquitetura, `8/8` integração, `57/57` Dashboard e `23` Pester; cobertura .NET `78,37%` linhas/`52,34%` branches e legado `32,08%`; toolchain/assets/TypeScript/build normal e sandbox, format e fixture NuGet offline aprovados; documentação para `246` fontes comment-capable, `347` links Markdown locais em `84` arquivos, secret scan do worktree não ignorado e histórico disponível, e `git diff --check` aprovados.
- Dependências/advisories: nenhum pacote/manifests de dependência foi acrescentado e nenhum restore/download externo foi executado. A fixture NuGet comprova completude/fail-closed do verificador para 16 projetos, não advisories atuais; npm/NuGet registry audit atual não foi executado pela proibição de acesso externo.
- Shutdown e cleanup: preflight inicial no commit `5c17952` encontrou zero componente/listener DB-Notifier. O runner final encerrou host, Chrome e perfil GUID; verificação posterior encontrou zero processo e zero diretório temporário correspondente. Nenhum navegador comum, IDE, banco ou processo alheio foi alterado.
- Evidências: [relatório do incremento](../../docs/STATE-06-Dashboard-TV-Browser-Composition-And-Recovery-Evidence-Report.md), [proposta autorizada](../../docs/STATE-06-Dashboard-TV-Browser-Composition-And-Recovery-Evidence-Proposal.md), código, scripts e testes locais.
- Limitações: Chrome único não é homologação; a fixture não prova fonte viva; identity/TLS são exclusivamente de teste; Edge/outros browsers, headed/Fullscreen humano, proxy/PKI/IdP, endurance, multi-cliente, carga, SignalR, provider/Agent/monitoramento e produção não foram testados.
- Próxima decisão: Bruno deve ler o relatório e aceitar com as limitações, solicitar remediação específica ou rejeitar somente este incremento. Nenhuma dessas opções autoriza automaticamente novo incremento, runtime operacional, promoção ou transição.
- Aprovador: Bruno autorizou a execução delimitada; a aceitação humana do resultado não foi inferida.

## 2026-07-18 — Aceitação humana de Dashboard TV Browser Composition and Recovery Evidence Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, incremento do commit `588ffec` tecnicamente concluído, Quality Gate automático restrito aprovado e Human Gate próprio pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e Human Gate do incremento aceito com as limitações registradas.
- Decisão: depois de declarar concordância com o conteúdo apresentado nas seções indicadas, Bruno declarou exatamente `Incremento STATE-06 Dashboard TV Browser Composition and Recovery Evidence Sandbox, commit 588ffec: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo novo incremento, runtime operacional, promoção nem transição de estado.`
- Base informada: resultado em linguagem simples, achados diretos corrigidos, evidência E2E Chrome/HTTPS loopback, matriz de falhas/recuperação, limitações residuais e classificação dos gates registradas no relatório do commit `588ffec`.
- Limitações aceitas: Chrome único não é homologação; fixture e identidade/TLS de teste não provam fonte viva ou PKI operacional; Edge/outros browsers, headed/Fullscreen humano, proxy/IdP, endurance, múltiplos clientes, carga, SignalR, provider/Agent/monitoramento, banco e produção permanecem não testados ou fora de escopo.
- Escopo desta ação: registro Markdown factual somente; nenhum código, projeto, configuração, dependência, teste, runtime ou saída gerada foi alterado.
- Evidências: [relatório do incremento](../../docs/STATE-06-Dashboard-TV-Browser-Composition-And-Recovery-Evidence-Report.md), commit `588ffec`, decisão explícita de Bruno nesta sessão e preflight sem componente/listener DB-Notifier ativo.
- Gates documentais: `APROVADOS`; documentação para `246` fontes comment-capable, `348` links Markdown locais em `84` arquivos, secret scan do worktree não ignorado e histórico disponível, escopo restrito a cinco documentos e `git diff --check` passaram com exit code `0`. Código, projetos, dependências, testes e runtimes permaneceram sem diff.
- Classificação: Human Gate do incremento `ACEITO COM AS LIMITAÇÕES REGISTRADAS`; saída de `STATE-06`, `OBSERVER`, SignalR, runtime operacional, integração externa, novo incremento, produção e release `NÃO AVALIADOS` e não autorizados.
- Próxima atividade: nenhuma ação adicional é exigida de Bruno e nenhum novo trabalho está autorizado. Uma próxima atividade só poderá começar mediante solicitação e autorização explícitas separadas.
- Aprovador: Bruno, exclusivamente para aceitar este incremento e registrar factualmente a decisão.

## 2026-07-18 — Plano documental consolidado de fechamento do STATE-06

- Estado anterior: `STATE-06 INTEGRATION`, incrementos restritos anteriores aceitos, mas Quality/Human Gate de saída do estado ainda não avaliados.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e nenhuma execução técnica liberada.
- Autorização: Bruno autorizou exclusivamente a elaboração de um plano documental consolidado cobrindo SignalR autenticado, notificações locais baseadas em estado reconciliado, E2E Agent → API → interfaces, resolução do critério de transporte de comandos e Quality/Human Gate final. Implementação, runtime operacional, acesso externo, promoção e transição permaneceram proibidos.
- Análise factual: contratos, identidade Agent de teste, heartbeat/assignments, resiliência offline e Dashboard TV periódico possuem evidências restritas aceitas. A cadeia E2E permanece separada, SignalR inexiste, notificações não consomem estado reconciliado e o command polling normal continua fail-closed. Expiração/idempotência de comando já possuem testes locais parciais, mas não transporte durável Agent/API ponta a ponta.
- Plano: quatro incrementos futuros e independentes — cadeia autoritativa de observações, hint SignalR, notificação Windows local reconciliada e transporte seguro de comandos sem executor — seguidos por campanha automática consolidada e Human Gate único do estado.
- Limites: a fonte futura é sintética/provider-neutral; SignalR é apenas hint; acknowledgement não significa execução; providers/bancos/identidades operacionais, canais externos, ações administrativas, LLM, deploy e estados posteriores ficam fora do fechamento.
- Escopo desta ação: criação do [plano consolidado](../../docs/STATE-06-Consolidated-Closure-Plan.md) e atualização factual de estado, histórico e changelog. Nenhum código, projeto, pacote, lockfile, configuração executável, migration, build, teste de produto ou runtime foi alterado/executado.
- Gates documentais: `APROVADOS`; documentação para `246` fontes comment-capable, `353` links Markdown locais em `85` arquivos, secret scan do worktree não ignorado e histórico disponível, `git diff --check` e escopo restrito a quatro arquivos Markdown passaram com exit code `0`. Isso não constitui Quality Gate de saída do `STATE-06`.
- Próxima decisão: Bruno pode revisar o plano e, se concordar, autorizar separadamente somente o Incremento 1 com o texto delimitado no documento. Nenhum passo técnico é automático.
- Aprovador: Bruno, exclusivamente para autorizar a elaboração documental; conteúdo do plano e qualquer execução futura permanecem sujeitos a decisão separada.

## 2026-07-19 — Incremento 1 Authoritative Observation Pipeline E2E Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, plano consolidado documental concluído e Incremento 1 ainda não executado.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, implementação local concluída, Quality Gate automático restrito aprovado e Human Gate próprio pendente.
- Autorização: Bruno autorizou separadamente somente a fonte provider-neutral sintética, observações canônicas versionadas, Agent SQLite/outbox, HTTPS/mTLS de teste, ingestão/projeção read-only, snapshot TV e testes locais de reinício, offline/reconexão, replay, duplicidade, reorder, staleness, revogação e incompatibilidade. Runtimes temporários exclusivamente locais deveriam ser encerrados. Acesso externo/download, provider/banco operacional, SignalR, notificações, comandos, executor, serviços permanentes, deploy, LLM, promoção e transição permaneceram proibidos.
- Implementação: o harness multiprocesso existente ganhou um modo sintético exato; o Agent usa a cadeia real de monitoring/outbox/dispatch somente com adapter privado de teste; a API reutiliza a rota canônica extraída; e somente o host E2E troca a fixture TV imutável por uma projeção singleton de evidência sintética reconciliada. Nenhum caminho operacional novo foi ativado: Worker normal e projeção dinâmica permanecem desabilitados, enquanto a rota normal existente ganhou somente validação versionada fail-closed.
- Evidência E2E: seis filhos reabriram um único Agent SQLite e provaram offline/reconexão, perda determinística de resposta aceita, replay sem efeito duplicado, lote `4,3`, staleness/`304`, headers incompatíveis e uma sequência `5` em voo recusada como `agent.not_active` depois de a revogação commitada e antes da entrada no store. O Server efêmero terminou com quatro amostras, quatro eventos, quatro mensagens server-outbox não publicadas, cursor `4`, zero notification delivery, zero comando e zero attempt.
- Revisão direta: três achados altos e cinco médios encontrados durante a revisão foram remediados, incluindo proveniência de sequência, resposta ambígua, `Healthy + Synthetic`, headers, estabilidade do snapshot vazio, limpeza de buffers IPC e revogação coordenada. Não restou achado crítico/alto conhecido. A janela interna pós-consulta `Active`/pré-commit permanece não testada e não é apresentada como linearização transacional.
- Gates: build Release dos `16` projetos com `0` erro/aviso; `316/316` testes unitários, `17/17` de arquitetura, `10/10` de integração e `57/57` Dashboard; cobertura .NET `78,48%`/`52,24%`; Pester `23` com skip condicional previsto e cobertura `32,08%`; format, documentação `250`, links `356/86`, fixture NuGet offline, secrets, smoke fail-closed, assets/tokens/localisation/provider-icons, TypeScript, Vite e bundle validate aprovados. Auditorias online não foram executadas por proibição de acesso externo.
- Cleanup: preflight e verificação final provaram `0` processo, `0` listener e `0` pasta temporária DB-Notifier residual. Uma pasta Cobertura temporária de execução interrompida foi validada sob `%TEMP%` e removida pontualmente; nenhum arquivo do workspace ou processo alheio foi alterado.
- Limitações: fonte/provider/identidade são somente de teste; o Server E2E usa SQLite em memória; possível contêiner temporário `UserKeySet` não foi inspecionado; não há negociação N-1/N+1 de Agent version; replay/reorder são curtos; e SignalR, notificação autoritativa, comandos, browser integrado, produção, `OBSERVER` e estados posteriores não foram avaliados.
- Próxima decisão: Bruno deve revisar o relatório e aceitar o Incremento 1 com as limitações, solicitar remediação delimitada ou rejeitá-lo. Nenhuma opção autoriza automaticamente o Incremento 2, runtime operacional, promoção ou transição.
- Aprovador: Bruno autorizou a execução delimitada; a decisão do Human Gate do resultado não foi inferida.

## 2026-07-19 — Aceitação do Incremento 1 Authoritative Observation Pipeline E2E Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, incremento do commit `3449918` tecnicamente concluído, Quality Gate automático restrito aprovado e Human Gate próprio pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, Human Gate próprio do incremento aceito com as limitações registradas.
- Decisão: Bruno declarou exatamente `Incremento STATE-06 Authoritative Observation Pipeline E2E Sandbox, commit 3449918: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo novo incremento, runtime operacional, promoção nem transição de estado.`
- Evidências revisadas: [relatório do incremento](../../docs/STATE-06-Authoritative-Observation-Pipeline-E2E-Sandbox-Report.md), incluindo resultado em linguagem simples, sequência E2E observada, limitações e condições residuais e classificação dos gates; commit local `3449918`; decisão de Bruno nesta sessão.
- Limitações aceitas: fonte/provider/identidade somente de teste; Server central E2E em SQLite em memória; possível contêiner temporário `UserKeySet` não inspecionado; ausência de negociação N-1/N+1 de Agent version; janela interna pós-consulta `Active`/pré-commit não disputada; replay/reorder curtos; SignalR, notificações, comandos, browser integrado, produção, `OBSERVER` e estados posteriores não avaliados.
- Autoridade: exclusivamente o registro factual desta decisão. Incrementos 2–4, runtime operacional, promoção e transição permanecem não autorizados.
- Shutdown preflight: `0` processo e `0` listener pertencente ao DB-Notifier antes da alteração documental; nenhum processo ou recurso alheio foi encerrado.
- Gates documentais deste registro: documentação aprovada para `250` fontes comment-capable; `356` links Markdown locais em `86` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; nenhum build, teste ou runtime de produto foi repetido porque esta ação altera somente o registro factual da decisão humana.
- Gates: Quality Gate automático restrito `APROVADO`; Human Gate próprio do Incremento 1 `ACEITO COM AS LIMITAÇÕES REGISTRADAS`; Quality/Human Gate final do `STATE-06` não inferido e ainda não executado.
- Próxima atividade: nenhuma atividade técnica está autorizada. Qualquer proposta ou execução do Incremento 2 exige decisão posterior, separada e explícita de Bruno.
- Aprovador: Bruno, 2026-07-19.

## 2026-07-19 — Proposta documental do Incremento 2 Authenticated SignalR Change Hint Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, Incremento 1 aceito com as limitações registradas e Incrementos 2–4 sem autorização de execução.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, com proposta documental do Incremento 2 pronta para revisão e nenhuma implementação autorizada.
- Solicitação: Bruno declarou exatamente `Apresente uma proposta exclusivamente documental para o Incremento 2 do Plano Consolidado de Fechamento do STATE-06, sem implementação, runtime operacional, acesso externo, promoção ou transição de estado.`
- Baseline observada: API/polling TV continuam autoritativos; leitura imediata, serialização, strong `ETag`, `304`, cancelamento e fencing já existem. Não há referência SignalR no código nem `@microsoft/signalr` no manifesto/lockfile; a consulta direcionada ao cache npm local não retornou entrada correspondente.
- Proposta: hub read-only somente no sandbox, autenticação efêmera de teste sob mesma origem HTTPS loopback, contrato mínimo versionado, API obrigatória após cada hint, polling de 30 segundos independente, concorrência `1`, coalescência/budget, cancelamento, fencing e reconnect limitados.
- Dependência: cliente artesanal, CDN e substituição por protocolo próprio são rejeitados. Implementação futura exige autorização explícita para aquisição controlada do cliente oficial `@microsoft/signalr` ou adiamento do incremento.
- Autoridade: somente documentação. Código, configuração executável, pacote, lockfile, build, teste de produto, browser, runtime, acesso externo, notificação, comando, promoção e transição não foram autorizados nem executados.
- Shutdown preflight: `0` processo e `0` listener pertencente ao DB-Notifier antes da alteração documental; nenhum processo ou recurso alheio foi encerrado.
- Gates documentais: documentação aprovada para `250` fontes comment-capable; `363` links Markdown locais em `87` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; escopo e `git diff --check` aprovados.
- Build, testes e runtime de produto: `NÃO APLICÁVEIS` e não executados neste escopo exclusivamente documental.
- Gates futuros: Quality/Human Gate do Incremento 2 e gate final do `STATE-06` não avaliados; esta proposta não os antecipa.
- Evidência: [proposta documental](../../docs/STATE-06-Authenticated-SignalR-Change-Hint-Sandbox-Proposal.md), [plano consolidado](../../docs/STATE-06-Consolidated-Closure-Plan.md), manifesto/lockfile Dashboard, consulta local do cache npm e decisão de Bruno nesta sessão.
- Próxima decisão: Bruno pode ajustar, adiar ou rejeitar a proposta. Para implementação, deverá autorizar separadamente o incremento e decidir explicitamente entre aquisição controlada do cliente oficial e manutenção da proibição de acesso externo/pacote novo.
- Aprovador: não aplicável; esta entrada registra solicitação e proposta, não aprovação de execução.

## 2026-07-19 — Incremento 2 Authenticated SignalR Change Hint Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, Incremento 1 aceito e proposta documental do Incremento 2 pronta, sem implementação autorizada naquele momento.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, implementação local do commit `c945c1b` concluída, Quality Gate automático restrito aprovado e Human Gate próprio pendente.
- Autorização: Bruno autorizou separadamente contrato mínimo versionado, hub read-only somente no sandbox HTTPS loopback, autenticação exclusivamente de teste e mesma origem, cliente Dashboard somente na composição sandbox, API/polling autoritativos, concorrência `1`, coalescência, budget, cancelamento, fencing, reconnect limitado, testes determinísticos e browser E2E. Também autorizou temporariamente somente o registry npm oficial para `@microsoft/signalr` e suas dependências transitivas. Todo outro pacote/fonte e todo recurso operacional permaneceram proibidos.
- Implementação: contrato `dashboard-tv-change-hint.v1`; cookie host-only efêmero com token aleatório/digest em memória; hub sem método cliente; publisher de revisão opaca; coordinator com dois hints por janela e deadline periódico independente; cliente oficial lazy somente após guardas do TV sandbox; fonte/control/evidência provider-neutral apenas no host E2E.
- Isolamento: o `Program` normal não registra nem mapeia o hub/publisher; a composição normal não carrega o chunk SignalR. Não houve notificação, Agent/provider/monitoramento real, banco externo, identidade operacional, comando, LLM, executor, deploy, promoção ou transição.
- Evidência browser: Chrome `150.0.7871.125`; hint produziu read HTTPS condicional `200`; polling seguinte retornou `304` após `30.016 ms` sem ser adiado; concorrência máxima de snapshot e conexão autenticada `1`; offline preservou estado e uma reconexão SignalR foi observada; `1.226` requisições HTTP/HTTPS e `26` WebSockets locais, `0` origem externa; cleanup integral aprovado.
- Supply chain: `@microsoft/signalr@10.0.0` e 17 transitivas exatas, todas do registry oficial e com integridade; 15 MIT, uma BSD-2-Clause, uma BSD-3-Clause e uma Unlicense; `npm audit` com zero vulnerabilidade conhecida no momento da consulta. Nenhum outro pacote/fonte foi utilizado.
- Gates: build Release dos 16 projetos com zero erro/aviso; `321/321` unitários, `19/19` arquitetura, `11/11` integração e `60/60` Dashboard; cobertura .NET `77,97%` linhas/`51,62%` branches; Pester compatível `23` com um skip previsto e cobertura `32,08%`; format, documentação, assets, TypeScript/Vite, fixture NuGet offline, secrets, smoke fail-closed e E2E aprovados.
- Revisão direta: dois achados médios de lifecycle/cleanup foram corrigidos, a prova de reconexão foi elevada de inferência para observação e a ordenação de imports foi corrigida. A execução Pester 3.4 sob PowerShell 7.6 apresentou incompatibilidade de `Should Throw`; o host Windows PowerShell compatível passou e sondas diretas confirmaram fail-closed. Nenhum código legado foi alterado.
- Limitações: SignalR continua best-effort; cookie/PKI são somente de teste; um Chrome/um Kestrel não provam browser/deploy/escala; Long Polling forçado, múltiplos clientes, carga/endurance/DDoS e identidade operacional não foram testados; budget local não é backpressure de frota; audit npm é pontual.
- Evidências: [relatório do incremento](../../docs/STATE-06-Authenticated-SignalR-Change-Hint-Sandbox-Report.md), [proposta autorizada](../../docs/STATE-06-Authenticated-SignalR-Change-Hint-Sandbox-Proposal.md), commit `c945c1b`, código, lockfile, testes e resumo sanitizado do Chrome E2E.
- Próxima decisão: Bruno deve revisar o relatório e aceitar com as limitações, solicitar remediação específica ou rejeitar somente este incremento. Nenhuma decisão libera automaticamente o Incremento 3, runtime operacional, promoção ou transição.
- Aprovador: Bruno autorizou a execução delimitada; a decisão do Human Gate do resultado não foi inferida.

## 2026-07-19 — Aceitação do Incremento 2 Authenticated SignalR Change Hint Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, incremento do commit `c945c1b` tecnicamente concluído, Quality Gate automático restrito aprovado e Human Gate próprio pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e Human Gate próprio do Incremento 2 aceito com as limitações registradas.
- Decisão: Bruno declarou exatamente `Incremento STATE-06 Authenticated SignalR Change Hint Sandbox, commit c945c1b: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo novo incremento, runtime operacional, promoção nem transição de estado.`
- Evidências revisadas: [relatório do incremento](../../docs/STATE-06-Authenticated-SignalR-Change-Hint-Sandbox-Report.md), especialmente resultado em linguagem simples, sequência E2E observada, limitações e condições residuais e classificação dos gates; commit `c945c1b`; decisão explícita de Bruno nesta sessão.
- Limitações aceitas: SignalR best-effort; cookie/PKI exclusivamente de teste; Chrome/Kestrel únicos sem prova de deploy ou escala; Long Polling forçado, múltiplos clientes, carga/endurance/DDoS e identidade operacional não testados; budget local sem alegação de backpressure de frota; audit npm pontual.
- Autoridade: exclusivamente o registro factual da decisão. Incremento 3, runtime operacional, promoção e transição permanecem não autorizados.
- Shutdown preflight: zero processo sandbox pertencente ao projeto, zero processo DB-Notifier e zero listener DB-Notifier antes desta alteração documental; nenhum processo alheio foi encerrado.
- Escopo desta ação: somente cinco documentos Markdown factuais; nenhum código, projeto, pacote, lockfile, configuração executável, build, teste de produto ou runtime foi alterado ou executado.
- Gates: Quality Gate automático restrito `APROVADO`; Human Gate próprio do Incremento 2 `ACEITO COM AS LIMITAÇÕES REGISTRADAS`; Quality/Human Gate final do `STATE-06` não inferido e ainda não executado.
- Próxima atividade: nenhuma atividade técnica está autorizada ou é exigida. Qualquer proposta ou execução do Incremento 3 exige solicitação e autorização posteriores, separadas e explícitas de Bruno.
- Aprovador: Bruno, 2026-07-19.

## 2026-07-19 — Proposta documental do Incremento 3 Reconciled Local Notification Delivery Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, Incrementos 1 e 2 concluídos e aceitos com as limitações registradas; Incrementos 3 e 4 sem autorização de execução.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, com proposta documental do Incremento 3 pronta para revisão e nenhuma implementação autorizada.
- Solicitação: Bruno declarou `proposta exclusivamente documental para o Incremento 3`.
- Baseline observada: o WPF possui publicação Windows local e fallback Tray limitado para demonstrações, mas não consome transição reconciliada. Eventos canônicos centrais possuem `EventId` estável, porém não há endpoint read-only/cursor para o WPF nem contrato consumível com o par anterior/atual; a deduplicação demonstrativa não é durável entre reinícios.
- Proposta: projeção/API read-only versionada somente em HTTPS loopback sandbox; autenticação exclusivamente de teste; consumidor WPF sob três guardas e opt-in desligado por padrão; baseline silenciosa; ledger local isolado com cursor monotónico, hash, deduplicação e fencing; fila serial limitada a `16`; budget, cancelamento, silêncio, freshness, localização e publicação pelo caminho Windows existente com fallback seguro.
- Limitação central: ledger e API Windows não compartilham transação. Identidade estável, estado `attempting` e Tag determinística mitigam repetição, mas a proposta não promete exactly-once nem trata aceitação da API Windows como prova de apresentação visível.
- Autoridade: somente documentação. Código, configuração executável, migration, pacote, lockfile, build, teste de produto, notificação, WPF/API/Agent runtime, acesso externo, promoção e transição não foram autorizados nem executados.
- Shutdown preflight: `0` processo e `0` listener pertencente ao DB-Notifier antes da alteração documental; nenhum processo ou recurso alheio foi encerrado.
- Gates documentais: documentação aprovada para `256` fontes comment-capable; `373` links Markdown locais em `89` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; `git diff --check` aprovado. Build, testes e runtime de produto foram `NÃO APLICÁVEIS` e não executados.
- Gates futuros: Quality/Human Gate do Incremento 3 e gate final do `STATE-06` não avaliados; esta proposta não os antecipa.
- Evidência: [proposta documental](../../docs/STATE-06-Reconciled-Local-Notification-Delivery-Sandbox-Proposal.md), [plano consolidado](../../docs/STATE-06-Consolidated-Closure-Plan.md), código do publicador Windows/Tray, contrato de snapshot e persistência de eventos canônicos inspecionados diretamente.
- Próxima decisão: Bruno pode revisar, ajustar, adiar ou rejeitar a proposta. Qualquer implementação exige autorização posterior, separada e explícita; o texto sugerido no documento não é autorização por si só.
- Aprovador: não aplicável; esta entrada registra solicitação e proposta, não aprovação de execução.

## 2026-07-19 — Incremento 3 Reconciled Local Notification Delivery Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, Incrementos 1 e 2 aceitos com limitações e proposta documental do Incremento 3 pronta; execução ainda não autorizada naquele momento.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, implementação local do commit `d43e49a` concluída, Quality Gate automático restrito aprovado e Human Gate próprio pendente.
- Autorização: Bruno autorizou separadamente contrato/projeção read-only de transições canónicas commitadas, endpoint HTTPS loopback autenticado somente para teste, consumidor WPF sandbox opt-in, baseline silenciosa, ledger local isolado, cursor/deduplicação/fencing, fila/budget/cancelamento/silêncio, localização, caminho Windows/fallback existentes e testes determinísticos/E2E locais. Runtimes temporários deveriam ser encerrados; todo recurso operacional ou externo permaneceu proibido.
- Implementação: contratos `reconciled-local-notification-transition.v1`, `reconciled-local-notification-ledger.v1` e detalhes `canonical-event-details.v1`; cursor vectorial monotónico; projeção que confirma evento/observação sintéticos; endpoint fora do `Program` normal; reader HTTPS limitado; ledger atómico com ownership lock; coordinator com intent antes do Windows, baseline/supressão/retry/deduplicação; WPF ativado apenas por argumentos locais exatos e opt-in; publisher/fallback existentes com conteúdo localizado.
- Evidência E2E: Kestrel HTTPS em `127.0.0.1`, certificado P-256 e identidade de teste, Server SQLite em memória, baseline sem entrega, commit `Healthy → Unavailable`, uma leitura/entrega, reabertura do ledger sem repetição e `401` sem identidade. O sink E2E termina antes da API Windows e não prova apresentação visível.
- Gates: build Release dos `16` projetos com zero erro/aviso; `327/327` unitários, `22/22` arquitetura, `13/13` integração e `60/60` Dashboard; cobertura `77,14%`/`49,71%`; Pester `23` com um skip previsto e cobertura `32,08%`; format, documentação `263`, links `373/89`, NuGet offline `16`, secrets, localisation/assets/tokens/provider icons e smoke fail-closed aprovados.
- Revisão direta: incompatibilidade evento/observação, evento sintético malformado, media type, cancelamento pré-replace e caracteres de controlo foram endurecidos. A primeira falha Schannel e seu lock temporário residual foram corrigidos e limpos pontualmente.
- Limitações: não há exactly-once entre ledger e Windows; aceite local não prova apresentação; full-storage rollback não é detectável pelo arquivo sozinho; WPF/Windows visual real não foi executado; Server E2E usa SQLite em memória; certificado de teste usa `UserKeySet | Exportable`; limites são somente do sandbox, sem alegação operacional ou fleet-wide.
- Evidências: [relatório do incremento](../../docs/STATE-06-Reconciled-Local-Notification-Delivery-Sandbox-Report.md), [proposta autorizada](../../docs/STATE-06-Reconciled-Local-Notification-Delivery-Sandbox-Proposal.md), commit `d43e49a`, testes e inspeção direta do diff.
- Cleanup: zero processo, zero listener e zero diretório temporário pertencente ao incremento depois da validação final; nenhum processo ou arquivo alheio foi alterado.
- Próxima decisão: Bruno deve aceitar com as limitações, solicitar remediação delimitada ou rejeitar somente este Incremento 3. Nenhuma decisão libera automaticamente o Incremento 4, runtime operacional, promoção ou transição.
- Aprovador: Bruno autorizou a execução delimitada; a decisão do Human Gate próprio do resultado não foi inferida.

## 2026-07-19 — Aceitação do Incremento 3 Reconciled Local Notification Delivery Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, incremento do commit `d43e49a` tecnicamente concluído, Quality Gate automático restrito aprovado e Human Gate próprio pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e Human Gate próprio do Incremento 3 aceito com as limitações registradas.
- Decisão: Bruno declarou exatamente `Incremento STATE-06 Reconciled Local Notification Delivery Sandbox, commit d43e49a: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo novo incremento, runtime operacional, promoção nem transição de estado.`
- Evidências revisadas: [relatório do incremento](../../docs/STATE-06-Reconciled-Local-Notification-Delivery-Sandbox-Report.md), especialmente resultado em linguagem simples, sequência E2E observada, limitações e condições residuais e classificação dos gates; commit `d43e49a`; decisão explícita de Bruno nesta sessão.
- Limitações aceitas: ausência de exactly-once entre ledger/Windows; aceite local sem prova de apresentação; full-storage rollback não detectável pelo arquivo sozinho; `FileShare.None` sem defesa contra administrador/manipulação offline; SQLite em memória no Server E2E; possível contêiner `UserKeySet`; limites exclusivamente sandbox; silêncio booleano; publisher Windows sem amostra humana neste incremento.
- Autoridade: exclusivamente o registro factual da decisão. Incremento 4, runtime operacional, promoção e transição permanecem não autorizados.
- Shutdown preflight: zero processo e zero listener pertencente ao DB-Notifier antes desta alteração documental; nenhum processo alheio foi encerrado.
- Escopo desta ação: somente cinco documentos Markdown factuais; nenhum código, projeto, pacote, lockfile, configuração executável, build, teste de produto ou runtime foi alterado ou executado.
- Gates: Quality Gate automático restrito `APROVADO`; Human Gate próprio do Incremento 3 `ACEITO COM AS LIMITAÇÕES REGISTRADAS`; Quality/Human Gate final do `STATE-06` não inferido e ainda não executado.
- Próxima atividade: nenhuma atividade técnica está autorizada ou é exigida. Qualquer proposta ou execução do Incremento 4 exige solicitação e autorização posteriores, separadas e explícitas de Bruno.
- Aprovador: Bruno, 2026-07-19.

## 2026-07-19 — Proposta documental do Incremento 4 Command Transport Safety E2E Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, Incrementos 1–3 concluídos e aceitos com as limitações registradas; Incremento 4 sem autorização de execução.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, com proposta documental do Incremento 4 pronta para revisão e nenhuma implementação autorizada.
- Solicitação: Bruno declarou `proposta exclusivamente documental do Incremento 4`.
- Baseline observada: contratos v1 e stores locais já cobrem seleção compatível, expiração, acknowledgement repetido e replay exato da inbox, mas não há outbox Agent nem journal Server duráveis que vinculem message ID, sequência, hash e resposta entre reinícios. O Worker normal permanece fail-closed e não existe executor autorizado.
- Proposta: contratos correlacionáveis; um stream monotónico por Agent; outbox/cursor no Agent; journal/cursor e resposta estável no Server; HTTPS/mTLS e revogação somente de teste; fixtures sintéticas não executáveis; expiração terminal; limites antes da materialização; concorrência `1`; retry/cancelamento/fencing; fault injection e restart E2E local.
- Separação de segurança: `Acknowledged` significa somente transporte/persistência. `Start`, `Stop`, `Restart`, `CommandAttempt`, `Running`, `Succeeded`, executor, provider, shell, processo/serviço/banco/infraestrutura afetados e runtime normal permanecem proibidos.
- Autoridade: somente documentação. Código, configuração executável, migration, pacote, lockfile, build, teste de produto, API/Agent runtime, acesso externo, comando, promoção e transição não foram autorizados nem executados.
- Shutdown preflight: `0` processo e `0` listener pertencente ao DB-Notifier antes da alteração documental; nenhum processo ou recurso alheio foi encerrado.
- Gates documentais: documentação aprovada para `263` fontes comment-capable; `384` links Markdown locais em `91` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; `git diff --check` aprovado. Build, testes e runtime de produto foram `NÃO APLICÁVEIS` e não executados.
- Gates futuros: Quality/Human Gate do Incremento 4, campanha consolidada e gate final do `STATE-06` não avaliados; esta proposta não os antecipa.
- Evidência: [proposta documental](../../docs/STATE-06-Command-Transport-Safety-E2E-Sandbox-Proposal.md), [plano consolidado](../../docs/STATE-06-Consolidated-Closure-Plan.md), contratos/stores/Worker atuais inspecionados diretamente e decisão de Bruno nesta sessão.
- Próxima decisão: Bruno pode revisar, ajustar, adiar ou rejeitar a proposta. Qualquer implementação exige autorização posterior, separada e explícita; o texto sugerido no documento não é autorização por si só.
- Aprovador: não aplicável; esta entrada registra solicitação e proposta, não aprovação de execução.

## 2026-07-19 — Incremento 4 Command Transport Safety E2E Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, Incrementos 1–3 aceitos com limitações e proposta documental do Incremento 4 pronta; implementação ainda não autorizada naquele ponto histórico.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, implementação local concluída no commit final `54a65f5`, Quality Gate automático restrito aprovado e Human Gate próprio pendente.
- Autorização: Bruno autorizou contratos v2 de poll/acknowledgement e erros tipados, outbox/journal/cursor duráveis, migrations somente sandbox, HTTPS/mTLS e revogação de teste, fixtures não executáveis, budget/backpressure/cancelamento/retry/replay/fencing/fault injection/restart locais. Runtime operacional, ação, executor, `CommandAttempt`, acesso externo, deploy, promoção e transição permaneceram proibidos.
- Implementação: stream monotónico compartilhado; request/response IDs e SHA-256 exatos; resposta estável; Agent SQLite com pendência/lease/fence; Server cursor/journal; limites de lote/corpo/parâmetros/provider versions, timeout e retries; endpoints/client/host somente sandbox; `CommandExecutionPolicy.Never` e prefixo sintético obrigatório.
- Evidência E2E: schema, gap, tamanho e identidade divergente recusados; perda das três respostas de poll e ack preservou e repetiu a mesma mensagem entre processos; inbox terminou `Acknowledged`/`Unsupported`; expirada ficou `Expired`; versão incompatível e controle neutro fora do namespace não foram entregues; request commitado antes da revogação retornou resposta histórica e request posterior foi recusado sem avanço.
- Isolamento: zero `CommandAttempt`, `Running`, `Succeeded`, resultado de provider, post-probe, shell, processo/serviço/banco/infraestrutura afetados; API e Worker normais não compõem o v2 e o guard de polling continua fail-closed.
- Gates: build Release dos `16` projetos com zero erro/aviso; `332/332` unitários, `25/25` arquitetura, `14/14` integração e `60/60` Dashboard; cobertura `78,91%`/`49,51%`; Pester `23` com um skip previsto e cobertura `32,08%`; format, documentação `273`, links Markdown `390/92`, NuGet offline `16`, secrets, assets/localização e smoke fail-closed aprovados.
- Limitações: Server E2E em SQLite memória e sem restart Server em disco; migration PostgreSQL somente gerada/inspecionada offline; nenhuma deadline absoluta da intenção persistida; backpressure global e estrito; pendência pós-revogação preservada sem dead-letter; identidade/PKI exclusivamente de teste; nenhum sizing ou runtime operacional comprovado.
- Cleanup: zero processo, zero listener e zero diretório `dbnotifier-command-transport-sandbox-*` pertencente ao incremento após a validação.
- Evidências: [relatório do incremento](../../docs/STATE-06-Command-Transport-Safety-E2E-Sandbox-Report.md), [proposta autorizada](../../docs/STATE-06-Command-Transport-Safety-E2E-Sandbox-Proposal.md), commits `3f23d3e` e `54a65f5`, testes e inspeção direta do diff.
- Próxima decisão: Bruno deve aceitar com as limitações, solicitar remediação delimitada ou rejeitar somente este Incremento 4. Mesmo uma aceitação autorizará apenas seu registro; campanha consolidada, runtime, promoção e transição permanecerão separados.
- Aprovador: Bruno autorizou a execução delimitada; a decisão do Human Gate próprio do resultado não foi inferida.

## 2026-07-19 — Aceitação do Incremento 4 Command Transport Safety E2E Sandbox

- Estado anterior: `STATE-06 INTEGRATION`, incremento do commit final `54a65f5` tecnicamente concluído, Quality Gate automático restrito aprovado e Human Gate próprio pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e Human Gate próprio do Incremento 4 aceito com as limitações registradas.
- Decisão: Bruno declarou exatamente `Incremento STATE-06 Command Transport Safety E2E Sandbox, commit 54a65f5: ACEITO COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo campanha consolidada, novo incremento, runtime operacional, promoção nem transição de estado.`
- Evidências revisadas: [relatório do incremento](../../docs/STATE-06-Command-Transport-Safety-E2E-Sandbox-Report.md), especialmente resultado em linguagem simples, sequência E2E observada, limitações e condições residuais e classificação dos gates; commit `54a65f5`; decisão explícita de Bruno nesta sessão.
- Limitações aceitas: Server E2E em SQLite memória e sem restart Server em disco; migration PostgreSQL somente gerada e inspecionada offline; ausência de deadline absoluta persistida para a intenção; backpressure global e estrito sem fairness de frota; pendência pós-revogação preservada sem dead-letter; identidade/PKI exclusivamente de teste; limites sem sizing ou prova operacional.
- Autoridade: exclusivamente o registro factual da decisão. Campanha consolidada, novo incremento, runtime operacional, promoção e transição permanecem não autorizados.
- Shutdown preflight: zero processo e zero listener pertencente ao DB-Notifier antes desta alteração documental; nenhum processo alheio foi encerrado.
- Escopo desta ação: somente cinco documentos Markdown factuais; nenhum código, projeto, pacote, lockfile, configuração executável, build, teste de produto ou runtime foi alterado ou executado.
- Gates documentais deste registro: documentação aprovada para `273` fontes comment-capable; `390` links Markdown locais em `92` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; inspeção de escopo e `git diff --check` aprovadas. Build, testes e runtime de produto são `NÃO APLICÁVEIS` e não foram executados.
- Gates: Quality Gate automático restrito `APROVADO`; Human Gate próprio do Incremento 4 `ACEITO COM AS LIMITAÇÕES REGISTRADAS`; campanha Quality Gate consolidada e Human Gate final do `STATE-06` não inferidos, não executados e não autorizados.
- Próxima atividade: nenhuma atividade técnica está autorizada ou é exigida. Qualquer proposta ou execução da campanha consolidada exige solicitação e autorização posteriores, separadas e explícitas de Bruno.
- Aprovador: Bruno, 2026-07-19.

## 2026-07-19 — Proposta documental da Campanha Consolidada de Quality Gate do STATE-06

- Estado anterior: `STATE-06 INTEGRATION`, quatro incrementos do plano de fechamento concluídos, com Quality Gates restritos aprovados e Human Gates próprios aceitos com as limitações registradas; campanha consolidada não autorizada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, com proposta documental da campanha pronta para revisão e nenhuma execução autorizada.
- Solicitação: Bruno declarou `desejo continuar, peço primeiro uma apresentação documental da campanha consolidada.`
- Baseline observada: os commits `3449918`, `c945c1b`, `d43e49a` e `54a65f5` possuem relatórios e decisões próprias; a evidência está distribuída entre harnesses de pipeline, Dashboard/SignalR, notificação e transporte não executável de comando, sem relatório consolidado corrente.
- Proposta: campanha de verificação, não novo incremento; preflight, auditoria offline, suítes completas, tentativa de composição E2E apenas com harnesses existentes, matriz de falhas, cleanup, relatório factual e classificação técnica sem correção silenciosa.
- Regra de bloqueio: se a cadeia única exigir código, teste, harness, configuração executável, migration, pacote, download ou ampliação de autoridade, a campanha futura deverá parar e classificar a evidência como `BLOQUEADA` ou `REPROVADA`; remediação exigirá autorização separada.
- Autoridade: somente documentação. Campanha, build, testes de produto, runtime, navegador, WPF, amostra humana, acesso externo, implementação, remediação, promoção e transição não foram autorizados nem executados.
- Shutdown preflight: `0` processo e `0` listener pertencente ao DB-Notifier antes da alteração documental; nenhum processo ou recurso alheio foi encerrado.
- Gates documentais: documentação aprovada para `273` fontes comment-capable; `397` links Markdown locais em `93` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; inspeção de escopo e `git diff --check` aprovadas. Build, testes e runtime de produto foram `NÃO APLICÁVEIS` e não executados.
- Gates futuros: Quality Gate consolidado, Human Gate final do `STATE-06` e transição permanecem separados, pendentes e não inferidos.
- Evidência: [proposta documental](../../docs/STATE-06-Consolidated-Quality-Gate-Campaign-Proposal.md), [plano consolidado](../../docs/STATE-06-Consolidated-Closure-Plan.md), [estado corrente](Current-State.md), quatro relatórios aceitos e inspeção read-only dos harnesses/testes existentes.
- Próxima decisão: Bruno pode revisar, solicitar alterações, adiar ou emitir separadamente o texto sugerido na proposta. O texto documental não é autorização por si só.
- Aprovador: não aplicável; esta entrada registra solicitação e proposta, não aprovação da campanha.

## 2026-07-19 — Campanha Consolidada de Quality Gate do STATE-06

- Estado anterior: `STATE-06 INTEGRATION`, quatro incrementos aceitos com limitações e proposta da campanha pronta; Quality/Human Gate final ainda não executado.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, Quality Gate consolidado `BLOQUEADO` e Human Gate final não aberto.
- Autorização: Bruno autorizou inspeção read-only, verificações offline, build/testes dos artefatos existentes, harnesses locais, Chrome dedicado/perfil efêmero, cleanup e relatório, proibindo código, remediação, download, recurso operacional, amostra humana, promoção e transição.
- Baseline: branch `main`, commit `5a47aae7030a406ffbe8b3960599fc68b9825d1b`, worktree limpa, zero processo/listener, .NET `10.0.301`, EF `10.0.9`, Node/npm `24.18.0`/`11.16.0`, PowerShell `7.6.3` e Chrome `150.0.7871.125`.
- Gates aprovados: build Release dos `16` projetos com zero erro/aviso; `332/332` unitários, `25/25` arquitetura, `14/14` integração, `60/60` Dashboard; cobertura `78,91%/49,51%`; Pester `23`/um skip previsto/`32,08%`; format, documentação `273`, links `397/93` antes do relatório, secrets, NuGet fixture offline `16`, npm audit offline, assets/tokens/localização, bundle, model drift e smoke fail-closed aprovados.
- Browser: primeira invocação sob Windows PowerShell 5.1 falhou no cleanup pela sobrecarga `Contains`; seu diretório temporário foi verificado/removido. A repetição sob PowerShell 7 passou em Chrome dedicado: hint autenticado, releitura, ETag/304, cadências `30.018/30.011 ms`, concorrência máxima `1`, doze cenários, zero request externo e cleanup integral.
- E2E proprietários: pipeline, SignalR/revogação, notificação com baseline silenciosa/deduplicação e comando não executável/replay/zero attempt passaram novamente, mas em sandboxes separados.
- Bloqueio: browser usa `BrowserSignalRSnapshotSource` derivado de fixture própria; notificação cria `NotificationSandbox`/Server SQLite próprios; pipeline e comando criam `AgentFleetSandbox`/stores/identidades independentes. Não existe orquestrador compartilhado em `src/`, `tests/` ou `scripts/`; criá-lo exigiria alteração proibida.
- Achados: `ALTA` lacuna de evidência da composição única; `BAIXA` dois warnings EF `10102` em queries limitadas de certificados/scopes sem impacto material demonstrado; `FERRAMENTA` dependência implícita do runner de browser em PowerShell moderno. O falso drift EF por assemblies Debug antigos foi resolvido por build Debug sem restore e não permaneceu como achado.
- Cleanup: zero processo/listener e zero temporário pertencente à campanha; hashes de package-lock/global.json preservados; worktree limpa antes do relatório. Um diretório de cobertura criado antes da campanha foi preservado por não pertencer a ela.
- Autoridade remanescente: exclusivamente registrar o relatório factual. Remediação, nova campanha, amostra humana, Human Gate final, runtime operacional, promoção e transição não estão autorizados.
- Evidência: [relatório consolidado](../../docs/STATE-06-Consolidated-Quality-Gate-Campaign-Report.md), [proposta autorizada](../../docs/STATE-06-Consolidated-Quality-Gate-Campaign-Proposal.md), comandos/resultados desta sessão e inspeção read-only dos harnesses.
- Próxima atividade: nenhuma atividade técnica está autorizada. Bruno pode solicitar uma proposta exclusivamente documental de remediação do harness consolidado e dos achados não bloqueantes.
- Aprovador: Bruno autorizou a campanha; a classificação automática é `BLOQUEADO` e nenhuma decisão de Human Gate foi inferida.

## 2026-07-19 — Proposta documental de remediação do Quality Gate consolidado do STATE-06

- Estado anterior: `STATE-06 INTEGRATION`, Quality Gate consolidado `BLOQUEADO` pela ausência de uma composição única correlacionada; Human Gate final não aberto.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e bloqueio mantidos, com proposta documental de remediação pronta para revisão.
- Solicitação: Bruno pediu exclusivamente uma proposta para um harness único que correlacione os quatro incrementos, os warnings EF de ordenação e a compatibilidade do runner com a versão declarada de PowerShell, proibindo implementação, runtime, acesso externo, promoção e transição.
- Proposta: host/orquestrador exclusivamente de teste com um run correlacionado, um Agent sintético, stores Agent/Server SQLite efêmeros, identidades Agent/humana separadas, projeção Server compartilhada por Dashboard/SignalR/notificação, comando `Never`, revogação, replay, evidence ledger sanitizado e cleanup.
- Achados menores: ordenação futura por chaves estáveis antes dos dois `Take` EF, sem migration ou mudança RBAC; PowerShell 7 mínimo declarado para runners modernos, mantendo o legado em Windows PowerShell 5.1.
- Escopo futuro sugerido: novo projeto sob `tests/`, inclusão mínima `Any CPU` na solução e fixture NuGet positiva, runner/auditor sem dependência nova, testes e documentação. Packages, lockfiles, migrations, acesso externo e composição normal permaneceriam proibidos.
- Autoridade corrente: somente documentação. Nenhum código, configuração executável, solução, projeto, fixture, script, build, teste de produto, runtime ou browser foi criado, alterado ou executado.
- Shutdown preflight: zero processo e zero listener pertencente ao DB-Notifier antes da alteração documental; nenhum processo alheio foi encerrado.
- Gates documentais: documentação aprovada para `273` fontes comment-capable; `403` links Markdown locais em `95` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; inspeção de escopo e `git diff --check` aprovadas. Build, testes e runtime de produto são `NÃO APLICÁVEIS` e não foram executados.
- Gates: Quality Gate consolidado continua `BLOQUEADO`; Human Gate final não aberto; implementação, nova campanha, amostra humana, promoção e transição não inferidas.
- Evidência: [proposta de remediação](../../docs/STATE-06-Consolidated-Quality-Gate-Remediation-Proposal.md), [relatório bloqueado](../../docs/STATE-06-Consolidated-Quality-Gate-Campaign-Report.md), [plano consolidado](../../docs/STATE-06-Consolidated-Closure-Plan.md) e inspeção read-only das fronteiras existentes.
- Próxima decisão: Bruno pode revisar, pedir alterações, adiar ou emitir separadamente o texto da seção `Decisão futura de Bruno`. O documento não autoriza sua própria implementação.
- Aprovador: não aplicável; registro de solicitação e proposta, não aprovação da remediação.

## 2026-07-19 — Remediação Consolidated E2E Evidence Harness and Deterministic Gate

- Estado anterior: `STATE-06 INTEGRATION`, Quality Gate consolidado histórico `BLOQUEADO`, remediação documental autorizada e Human Gate final não aberto.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, remediação técnica concluída e Quality Gate automático restrito da remediação `APROVADO`. A campanha histórica continua `BLOQUEADA` até eventual repetição separadamente autorizada.
- Autorização: Bruno autorizou um host/orquestrador exclusivamente de teste, execução local correlacionada, identidades Agent/humana de teste separadas, SQLite efêmero, pipeline até Dashboard/SignalR/notificação em sink de teste, comando não executável, revogação, replay, falhas, fencing, budgets, cleanup, ordenação EF, PowerShell 7, testes e documentação. Acesso externo, recurso operacional, comando/`CommandAttempt`/executor, canal externo, notificação Windows visível, nova campanha, amostra humana, promoção e transição permaneceram proibidos.
- Implementação: novo `DBNotifier.State06.ConsolidatedSandboxHost` sob `tests/`, referência somente ao harness de integração existente, sem package novo; runner/auditor offline; inclusão mínima como 17º projeto da solução e da fixture NuGet positiva; duas consultas EF ordenadas por chaves estáveis antes de `Take`; runners modernos com `#Requires -Version 7.0`.
- Evidência correlacionada: o mesmo Agent sintético e o mesmo Server SQLite produziram `2` observações, Dashboard imediato e reconciliado, hint SignalR, `1` entrega deduplicada no sink, `2` registros de journal e `0` `CommandAttempt`; heartbeat, assignments, observação e comando foram recusados após revogação. A execução Chrome observou concorrência máxima de snapshot `1`, zero request HTTP externo e nenhum dado operacional.
- Isolamento e cleanup: host exigiu ativação exata; composição normal permaneceu sem o harness; processos locais foram encerrados, perfil efêmero removido e a contagem de roots Agent temporários permaneceu `0 → 0` após a execução final.
- Gates: build Release da solução com `17` projetos e zero erro/aviso; `332/332` unitários, `29/29` arquitetura e `14/14` integração; E2E correlacionado em Chrome aprovado; recusa pré-recurso no Windows PowerShell 5.1 aprovada; demais verificações offline registradas no relatório factual.
- Limitações: somente dados, certificados, identidades, SQLite e sink sintéticos; listeners loopback; nenhum PostgreSQL, provider, IdP/PKI/vault, canal, comando ou runtime operacional. A prova corrige a lacuna do harness, mas não reclassifica a campanha anterior nem abre o Human Gate final.
- Evidências: [relatório da remediação](../../docs/STATE-06-Consolidated-E2E-Evidence-Harness-And-Deterministic-Gate-Remediation-Report.md), [proposta autorizada](../../docs/STATE-06-Consolidated-Quality-Gate-Remediation-Proposal.md), execução E2E e inspeção direta do diff.
- Próxima decisão: Bruno deve revisar e aceitar com limitações, solicitar remediação delimitada ou rejeitar somente esta remediação. Uma nova Campanha Consolidada continuará exigindo autorização posterior, separada e explícita.
- Aprovador: Bruno autorizou a execução; a revisão humana própria do resultado não foi inferida.

## 2026-07-19 — Aceitação da remediação Consolidated E2E Evidence Harness and Deterministic Gate

- Estado anterior: `STATE-06 INTEGRATION`, remediação do commit `ac12791` tecnicamente concluída, Quality Gate automático restrito aprovado, Human Gate próprio pendente e Campanha Consolidada histórica bloqueada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e Human Gate próprio da remediação aceito com as limitações registradas. A campanha histórica continua `BLOQUEADA`.
- Decisão: Bruno declarou exatamente `Remediação STATE-06 Consolidated E2E Evidence Harness and Deterministic Gate, commit ac12791: ACEITA COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo nova Campanha Consolidada, amostra humana, Human Gate final, runtime operacional, promoção nem transição de estado.`
- Evidências revisadas: [relatório da remediação](../../docs/STATE-06-Consolidated-E2E-Evidence-Harness-And-Deterministic-Gate-Remediation-Report.md), especialmente resultado em linguagem simples, sequência E2E observada, limitações e condições residuais e classificação dos gates; commit `ac12791`; decisão explícita de Bruno nesta sessão.
- Limitações aceitas: somente dados, certificados, identidades, SQLite e sink sintéticos; listeners HTTPS loopback; Chrome único; SignalR apenas como hint; transporte restrito a persistência/acknowledgement com zero `CommandAttempt`; ausência de sizing, fairness de frota, PostgreSQL/provider/IdP/PKI/vault e runtime operacionais; a remediação não reclassifica a campanha histórica.
- Autoridade: exclusivamente o registro factual desta decisão. Nova Campanha Consolidada, amostra humana, Human Gate final, runtime operacional, promoção e transição permanecem não autorizados.
- Shutdown preflight: zero processo e zero listener pertencente ao DB-Notifier antes desta alteração documental; nenhum processo ou recurso alheio foi encerrado.
- Escopo desta ação: somente cinco documentos Markdown factuais; nenhum código, projeto, configuração executável, package, lockfile, migration, build, teste de produto, browser ou runtime foi alterado ou executado.
- Gates documentais deste registro: documentação aprovada para `279` fontes comment-capable; `408` links Markdown locais em `96` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; inspeção de escopo, `git diff --check` e `git diff --cached --check` aprovados. Build, testes e runtime de produto são `NÃO APLICÁVEIS` e não foram executados.
- Gates: Quality Gate automático restrito da remediação `APROVADO`; Human Gate próprio `ACEITO COM AS LIMITAÇÕES REGISTRADAS`; Campanha Consolidada histórica `BLOQUEADA`; Human Gate final do `STATE-06` não aberto; promoção e transição não autorizadas.
- Próxima atividade: nenhuma atividade técnica está autorizada ou é exigida. Se Bruno desejar continuar, deverá solicitar ou autorizar separadamente uma nova Campanha Consolidada no commit corrente; o registro desta aceitação não concede essa autoridade.
- Aprovador: Bruno, 2026-07-19.

## 2026-07-19 — Proposta documental de repetição da Campanha Consolidada de Quality Gate

- Estado anterior: `STATE-06 INTEGRATION`, remediação `ac12791` aceita com limitações, Campanha Consolidada histórica `BLOQUEADA`, Human Gate final não aberto e nenhuma repetição autorizada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e bloqueio histórico mantidos, com proposta documental separada da repetição pronta para revisão.
- Solicitação: Bruno pediu exclusivamente uma proposta para repetir a Campanha Consolidada no commit corrente, incorporando a remediação aceita, sem implementação, runtime, browser, amostra humana, promoção ou transição.
- Baseline documental: implementação técnica `ac12791`, registro humano `dd58b26` e worktree limpa antes desta alteração. A futura campanha deverá congelar o commit exato e impedir mudança técnica posterior não revisada.
- Proposta: inspeção read-only, gates offline, build/testes/cobertura existentes, harness correlacionado como evidência principal, regressões proprietárias, ausência dos warnings EF, contrato PowerShell, cleanup e relatório novo que preserve o relatório histórico.
- Critério central: a cadeia deverá usar o mesmo run, Agent, Agent SQLite, Server SQLite e projeção para Agent → API → Dashboard/SignalR → notificação e transporte não executável, terminando com zero `CommandAttempt` e revogação fail-closed.
- Autoridade: exclusivamente documentação. Nenhum código, configuração executável, solução, projeto, package, lockfile, migration, build, teste de produto, runtime, browser ou acesso externo foi autorizado ou executado.
- Shutdown preflight: zero processo e zero listener pertencente ao DB-Notifier antes da alteração documental; nenhum processo ou recurso alheio foi encerrado.
- Gates documentais: documentação aprovada para `279` fontes comment-capable; `415` links Markdown locais em `97` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; inspeção de escopo, `git diff --check` e `git diff --cached --check` aprovados. Build, testes e runtime de produto são `NÃO APLICÁVEIS` e não foram executados.
- Gates futuros: repetição da Campanha Consolidada, amostra humana, Human Gate final, promoção e transição continuam não autorizados e não inferidos.
- Evidência: [proposta da repetição](../../docs/STATE-06-Consolidated-Quality-Gate-Rerun-Proposal.md), [relatório histórico bloqueado](../../docs/STATE-06-Consolidated-Quality-Gate-Campaign-Report.md), [relatório da remediação aceita](../../docs/STATE-06-Consolidated-E2E-Evidence-Harness-And-Deterministic-Gate-Remediation-Report.md), plano consolidado e estado corrente.
- Próxima decisão: Bruno pode revisar, pedir alterações, adiar ou emitir separadamente o texto da seção `Decisão futura de Bruno`. A proposta não inicia sua própria campanha.
- Aprovador: não aplicável; registro de solicitação e proposta, não autorização de execução.

## 2026-07-19 — Proposta executiva e técnica do programa MOD-12 AIOps operacional

- Estado anterior: `STATE-06 INTEGRATION`, nenhum modo MOD-12 ativo, `none → OBSERVER` pendente, `ADR-0007` `proposed` e nenhum novo incremento MOD-12 autorizado.
- Estado resultante: sem transição; `STATE-06 INTEGRATION`, modos e `ADR-0007` inalterados, com proposta do programa AIOps e do primeiro incremento restrito O1 pronta para revisão.
- Autorização: Bruno declarou exatamente `Autorizo a elaboração da proposta executiva e técnica para tornar o MOD-12 uma AIOps operacional completa, começando pelo incremento restrito do OBSERVER, sem ativação automática, LLM, recomendações ou execução nesta primeira autorização.`
- Proposta: definição verificável de AIOps completa, arquitetura-alvo, separação dos modos, programa `O1`–`O5`, trilhas futuras `ADVISOR`/`ASSISTANT`/`CONTROLLED_AUTOMATION`, estratégia provider-neutral, recomendação não vinculante de PostgreSQL como primeira homologação futura, segurança, métricas, riscos, decisões abertas e gates independentes.
- Primeiro incremento proposto: `MOD-12 O1 — Durable Trust Continuity and Resource Admission Sandbox`, limitado a trust/checkpoint/resource coordinator host-side em sandbox local, sem telemetria, provider, runtime normal, LLM, recomendação, plano, comando, executor ou promoção.
- Autoridade preservada: exclusivamente documentação. A proposta não adota `ADR-0007`, não autoriza O1, código/configuração executável, migration, package, build/teste de produto, runtime, browser, acesso externo, banco/provider real, UI, ativação, Human Gate, promoção ou transição.
- Shutdown preflight: `Stopped=0`, `RemainingProjectOwned=0`, `OwnedListeners=0` e `DedicatedReviewBrowsers=0`; a janela do VS Code do usuário, PID `12932`, foi identificada e preservada.
- Gates documentais: documentação aprovada para `279` fontes comment-capable; `430` links Markdown locais em `98` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; `git diff --check` aprovado. Build, testes, cobertura e runtime de produto são `NÃO APLICÁVEIS` e não foram executados.
- Evidência: [proposta do programa MOD-12](../../docs/STATE-06-MOD-12-Operational-AIOps-Programme-And-Restricted-Observer-Proposal.md), [estado corrente](Current-State.md), [guardrails](../../docs/architecture/AIOps-Architecture-Guardrails.md), [ADR-0007 proposto](../../docs/architecture/ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md) e [contrato documental de confiança/recursos](../../docs/architecture/AIOps-Trust-Governance-And-Resource-Envelope.md).
- Próxima decisão: Bruno pode responder `ACEITA COMO DIREÇÃO`, `ACEITA COM RESSALVAS`, `AJUSTES SOLICITADOS` ou `REJEITADA`. Aceitação da proposta não autoriza implementação; adoção/ajuste do `ADR-0007` e eventual O1 exigem decisões posteriores, separadas e explícitas.
- Aprovador: não aplicável; esta entrada registra autorização e produção da proposta, não decisão sobre seu conteúdo nem Human Gate.

## 2026-07-19 — Repetição da Campanha Consolidada de Quality Gate do STATE-06

- Estado anterior: `STATE-06 INTEGRATION`, primeira campanha consolidada historicamente `BLOQUEADA`, remediação `ac12791` aceita, repetição proposta e Human Gate final não aberto.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e Quality Gate consolidado repetido `REPROVADO`.
- Autorização: Bruno autorizou exclusivamente a repetição no commit `66d0a9f`, com shutdown, inspeção read-only, gates offline, build/testes/cobertura, harnesses locais seriais, Chrome dedicado efêmero, EF, PowerShell, cleanup e relatório. Correção, acesso externo, recurso operacional, comando/`CommandAttempt`/executor, notificação Windows visível, amostra humana, promoção e transição permaneceram proibidos.
- Baseline: commit exato `66d0a9fa661f9ec05c365aececa7e10cf6903c8b`, contendo `ac12791`, sem mudança técnica posterior à remediação; .NET `10.0.301`, EF `10.0.9`, Node/npm `24.18.0`/`11.16.0`, PowerShell `7.6.3`, Windows PowerShell `5.1.26100.8875` e Chrome `150.0.7871.125`.
- Gates gerais: build Release de `17` projetos com zero erro/warning; `332/332` unitários, `29/29` arquitetura, `14/14` integração, `60/60` Dashboard; cobertura `78,9%/49,51%`; Pester `23`/um skip esperado/`32,08%`; format, documentação, links, secrets, NuGet fixture `17`, npm audit offline, assets, tokens, localização, bundle, model drift e smoke fail-closed aprovados.
- Evidência isolada: Agent Fleet/pipeline/comando `8/8`, Dashboard/SignalR `4/4`, notificação `2/2`; zero warning EF `10102`. O browser proprietário passou com cadências de `30.012/30.007 ms`, concorrência `1`, `1.230` requests HTTP locais, `27` WebSockets e zero request HTTP externo.
- Falha: o harness correlacionado obrigatório respondeu HTTP `503`, código `state06.consolidated_operation_failed`, na etapa `finalising-revocation`, e terminou com exit code `1`. A resposta sanitizada não permitiu distinguir entre falha de revogação, heartbeat, assignments ou asserção fail-closed; nenhuma causa foi inventada e nenhum retry/correção foi executado.
- Achado: `ALTA` — ausência de prova terminal de que a mesma revogação nega heartbeat, assignments, observação e comando na cadeia única. A aprovação dos testes isolados não substitui esse critério obrigatório.
- PowerShell: os dois runners modernos recusaram Windows PowerShell 5.1 antes de criar recursos; exit code `1`, erro de versão esperado e zero novo root.
- Cleanup: zero processo, listener, perfil, store, root ou metadado de worktree pertencente à campanha; worktree técnica limpa, hashes preservados e `node_modules` principal preservado. Conversão CRLF de uma primeira cópia inválida e retenção local de metadados reparse/ACL foram eventos de ferramenta resolvidos e documentados, sem alteração do commit.
- Autoridade remanescente: exclusivamente registrar este resultado factual. Remediação, nova campanha, amostra humana, Human Gate final, runtime operacional, promoção e transição não estão autorizados.
- Evidência: [relatório da repetição](../../docs/STATE-06-Consolidated-Quality-Gate-Rerun-Report.md), [proposta autorizada](../../docs/STATE-06-Consolidated-Quality-Gate-Rerun-Proposal.md), [relatório histórico bloqueado](../../docs/STATE-06-Consolidated-Quality-Gate-Campaign-Report.md) e comandos/resultados desta sessão.
- Próxima atividade: Bruno deve revisar o relatório; se desejar continuar, poderá solicitar uma proposta exclusivamente documental de remediação do achado `finalising-revocation`. Human Gate final e amostras humanas não podem começar enquanto o Quality Gate estiver `REPROVADO`.
- Aprovador: Bruno autorizou a campanha; a classificação `REPROVADO` é automática e nenhuma decisão humana sobre o resultado foi inferida.

## 2026-07-20 — Proposta documental de remediação de `finalising-revocation`

- Estado anterior: `STATE-06 INTEGRATION`, Quality Gate consolidado repetido `REPROVADO`, achado alto em `finalising-revocation` e Human Gate final não aberto.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e classificação `REPROVADO` mantidos, com proposta documental pronta para revisão.
- Autorização: Bruno declarou ciência do relatório `c50eef5` e autorizou exclusivamente elaborar a proposta de remediação, proibindo implementação, runtime, acesso externo, nova campanha, amostra humana, promoção e transição.
- Shutdown preflight: branch `main`, commit `c50eef5`, worktree limpa, zero processo, listener ou navegador dedicado pertencente ao DB-Notifier; nenhum processo alheio foi encerrado.
- Inspeção read-only: os quatro artefatos técnicos relevantes possuem blobs idênticos em `66d0a9f` e no `HEAD`; o estágio agregado contém revogação, heartbeat, assignments e asserções, impedindo atribuir a causa a uma fronteira exata. A revogação central é transacional; autenticação consulta estado ativo; heartbeat negado persiste `RevokedOrDenied`; assignments posterior pode falhar localmente antes do transporte.
- Proposta: granularidade de estágios, envelope de falha sanitizado, prova separada de commit central/negação server-side/quarentena local, matriz de barriers determinísticos `R1`–`R7`, fencing, relógio controlado e árvore de decisão que permite correção apenas em `tests/`/`scripts/`.
- Condição de parada: qualquer causa que exija `src/`, solução, projeto, package, lockfile, migration, acesso externo ou recurso operacional deverá bloquear o incremento futuro e voltar para autorização separada.
- Autoridade corrente: somente documentação. Nenhum código, configuração executável, build, teste, runtime, browser ou acesso externo foi executado ou alterado.
- Gates: Quality Gate consolidado permanece `REPROVADO`; Human Gate final não aberto; implementação, nova campanha, amostra humana, promoção e transição não inferidas.
- Evidência: [proposta de remediação](../../docs/STATE-06-Consolidated-Revocation-Finalisation-Remediation-Proposal.md), [relatório reprovado](../../docs/STATE-06-Consolidated-Quality-Gate-Rerun-Report.md), inspeção read-only do commit `66d0a9f` e autorização explícita de Bruno.
- Próxima decisão: Bruno pode pedir ajustes, adiar ou enviar exatamente o texto da seção `Decisão futura de Bruno`. A proposta não autoriza sua própria implementação.
- Aprovador: não aplicável; registro de solicitação e proposta, não Human Gate nem autorização de implementação.

## 2026-07-20 — Remediação Consolidated Revocation Finalisation Diagnostic and Harness

- Estado anterior: `STATE-06 INTEGRATION`, Quality Gate consolidado repetido `REPROVADO`, proposta de remediação concluída e implementação ainda não iniciada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, Quality Gate próprio da remediação `APROVADO`, campanha anterior ainda `REPROVADA` e Human Gate próprio da remediação pendente.
- Autorização: Bruno limitou a ação a granularidade test-only, evidência sanitizada, prova separada de revogação central/negação/quarentena, R1–R7, fencing, relógio, budgets, cancellation, testes e correção exclusivamente sob `tests/`/`scripts/`; qualquer necessidade de `src/`, solução, projeto, package, lockfile ou migration exigiria bloqueio.
- Shutdown preflight: branch `main`, baseline `c03cba1`, worktree limpa, zero processo, listener ou navegador dedicado pertencente ao DB-Notifier; nenhum processo alheio foi encerrado.
- Diagnóstico: duas reproduções sem alteração produziram uma expiração de readiness e um passe completo, sem reproduzir a falha histórica. A instrumentação granular posteriormente reproduziu HTTP `400` em `finalising-command-gap-negative` e provou divergência test-only entre `DateTimeOffset.UtcNow` no request e o relógio controlado do Server. A exceção histórica exata de `finalising-revocation` permanece irrecuperável do payload antigo e não foi inventada.
- Implementação: envelope allow-listed de falha; estágios granulares; commit central, negação direta e quarentena local separados; transporte contado; LKG preservado; fences expostos; coordinator e command polls no mesmo relógio controlado; readiness com budget único de `90` segundos; matriz R1–R7 com barrier, clock e leases reais de SQLite sandbox.
- Evidência: R1–R7 e teste negativo aprovados; harness completo Chrome aprovado com `2` observações, `1` entrega, `2` journals, `0` `CommandAttempt`, `1/1` certificado revogado, fences `3 → 4`, concorrência de snapshot `1` e zero origem HTTP externa.
- Gates: build Release `17` projetos sem erro/warning; `332/332` unitários, `30/30` arquitetura, `16/16` integração, cobertura `78,9%/49,51%`, Dashboard `60/60`, TypeScript/build e formatação aprovados. Gates documentais, secrets, links, diff e cleanup final constam do relatório/commit da entrega.
- Escopo: nenhum arquivo sob `src/`, solução, projeto, package, lockfile ou migration foi alterado; nenhum acesso externo, recurso operacional, comando real, executor, notificação visível, deploy, promoção ou transição ocorreu.
- Limitações: SQLite e identidades exclusivamente sintéticos; R3 prova sobreposição no boundary HTTP, não locking de PostgreSQL; Chrome único; nenhum resultado operacional. O passe da remediação não reclassifica a campanha anterior.
- Evidência documental: [relatório da remediação](../../docs/STATE-06-Consolidated-Revocation-Finalisation-Diagnostic-And-Harness-Remediation-Report.md), [proposta autorizada](../../docs/STATE-06-Consolidated-Revocation-Finalisation-Remediation-Proposal.md) e [relatório histórico reprovado](../../docs/STATE-06-Consolidated-Quality-Gate-Rerun-Report.md).
- Próxima decisão: Bruno deve revisar e decidir somente o Human Gate próprio desta remediação. Nova Campanha Consolidada, amostra humana, Human Gate final, promoção e transição exigem decisões posteriores e separadas.
- Aprovador: Bruno autorizou a implementação; a aceitação humana do resultado não foi inferida.

## 2026-07-20 — Aceitação da remediação Consolidated Revocation Finalisation Diagnostic and Harness

- Estado anterior: `STATE-06 INTEGRATION`, remediação do commit `f9bb567` tecnicamente concluída, Quality Gate próprio `APROVADO`, Human Gate próprio pendente e repetição da Campanha Consolidada `REPROVADA`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e Human Gate próprio da remediação aceito com as limitações registradas. A campanha repetida continua `REPROVADA`.
- Decisão: Bruno declarou exatamente `Remediação STATE-06 Consolidated Revocation Finalisation Diagnostic and Harness, commit f9bb567: ACEITA COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo nova Campanha Consolidada, amostra humana, Human Gate final, runtime operacional, promoção nem transição de estado.`
- Evidências revisadas: [relatório da remediação](../../docs/STATE-06-Consolidated-Revocation-Finalisation-Diagnostic-And-Harness-Remediation-Report.md), principalmente resultado em linguagem simples, sequência E2E observada, limitações e condições residuais e classificação dos gates; commit `f9bb567`; decisão explícita de Bruno nesta sessão.
- Limitações aceitas: exceção histórica irrecuperável; SQLite e identidades exclusivamente sintéticos; R3 prova sobreposição HTTP, não locking de PostgreSQL; Chrome único; sink sem apresentação Windows; SignalR apenas como hint; comando deliberadamente não executável; nenhuma evidência operacional.
- Autoridade: exclusivamente o registro factual desta decisão. Nova Campanha Consolidada, amostra humana, Human Gate final, runtime operacional, promoção e transição permanecem não autorizados.
- Shutdown preflight: zero processo, listener ou navegador dedicado pertencente ao DB-Notifier antes desta alteração documental; nenhum processo ou recurso alheio foi encerrado.
- Escopo desta ação: somente cinco documentos Markdown factuais; nenhum código, configuração executável, solução, projeto, package, lockfile, migration, build, teste de produto, browser ou runtime foi alterado ou executado.
- Gates documentais deste registro: documentação aprovada para `280` fontes comment-capable; `444` links Markdown locais em `101` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; inspeção de escopo e `git diff --check` aprovadas. Build, testes e runtime de produto são `NÃO APLICÁVEIS` e não foram executados.
- Gates: Quality Gate próprio da remediação `APROVADO`; Human Gate próprio `ACEITO COM AS LIMITAÇÕES REGISTRADAS`; repetição da Campanha Consolidada `REPROVADA`; Human Gate final do `STATE-06` não aberto; promoção e transição não autorizadas.
- Próxima atividade: nenhuma atividade técnica está autorizada ou é exigida. Se Bruno desejar continuar, deverá solicitar ou autorizar separadamente uma nova Campanha Consolidada no commit corrente; este registro não concede essa autoridade.
- Aprovador: Bruno, 2026-07-20.

## 2026-07-20 — Proposta documental de nova repetição da Campanha Consolidada pós-remediação

- Estado anterior: `STATE-06 INTEGRATION`, repetição anterior da Campanha Consolidada `REPROVADA`, remediação `f9bb567` com Quality Gate próprio aprovado e Human Gate próprio aceito, registro factual `67e0187` e nenhuma nova campanha autorizada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e proposta documental da nova repetição pronta para revisão, sem execução ou reclassificação de gate.
- Solicitação: Bruno pediu exclusivamente a proposta documental para repetir a campanha sobre o commit `67e0187`, que contém a remediação aceita `f9bb567`, sem execução, runtime, browser, amostra humana, promoção ou transição.
- Baseline proposta: o futuro commit executado deverá conter `f9bb567` e `67e0187` na ancestralidade e nenhuma mudança técnica posterior a `f9bb567`; qualquer commit documental acima deles deverá ser auditado antes do build.
- Proposta: shutdown, congelamento, inspeção read-only, gates offline, build/testes/cobertura existentes, R1–R7, harness correlacionado completo com revogação granular, regressões proprietárias, warnings EF, contrato PowerShell, cleanup e relatório factual novo.
- Critério central: a cadeia única deverá provar separadamente commit central da revogação, negação server-side, quarentena Agent local, preservação do LKG, fences monotónicos, observação/comando negados e zero `CommandAttempt`.
- Autoridade: exclusivamente documentação. Nenhum build, teste de produto, runtime, browser, acesso externo, correção, nova campanha, amostra humana, Human Gate final, promoção ou transição foi autorizado ou executado.
- Shutdown preflight: zero processo, listener ou navegador dedicado pertencente ao DB-Notifier antes da alteração documental; nenhum processo ou recurso alheio foi encerrado.
- Escopo desta ação: somente proposta e registros Markdown; código, configuração executável, solução, projetos, packages, lockfiles e migrations permanecem inalterados.
- Gates documentais: documentação aprovada para `280` fontes comment-capable; `450` links Markdown locais em `102` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; escopo de quatro documentos e `git diff --check` aprovados. Build, testes e runtime de produto são `NÃO APLICÁVEIS` e não foram executados.
- Evidências: [proposta da nova repetição](../../docs/STATE-06-Consolidated-Quality-Gate-Post-Revocation-Remediation-Rerun-Proposal.md), [relatório da remediação aceita](../../docs/STATE-06-Consolidated-Revocation-Finalisation-Diagnostic-And-Harness-Remediation-Report.md), [relatório histórico reprovado](../../docs/STATE-06-Consolidated-Quality-Gate-Rerun-Report.md) e plano consolidado.
- Gates futuros: a campanha, amostra humana, Human Gate final, promoção e transição continuam não autorizados e não inferidos.
- Próxima decisão: Bruno pode solicitar ajustes, adiar ou enviar exatamente o texto da seção `Decisão futura de Bruno`. A proposta não inicia sua própria campanha.
- Aprovador: não aplicável; registro de solicitação e proposta, não autorização de execução.

## 2026-07-20 — Nova repetição da Campanha Consolidada pós-remediação de revogação

- Estado anterior: `STATE-06 INTEGRATION`, repetição anterior `REPROVADA`, remediação `f9bb567` aceita, proposta `84217c6` pronta e campanha ainda não executada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido e Quality Gate consolidado da baseline `84217c6` classificado `APROVADO`. Amostra humana e Human Gate final continuam não autorizados.
- Autorização: Bruno autorizou somente shutdown, baseline read-only, gates offline, build/testes/cobertura, R1–R7, harnesses sandbox seriais, Chrome dedicado, revogação granular, EF, PowerShell, cleanup e relatório factual, sem correção ou acesso externo.
- Baseline: branch `main`, commit `84217c64312a024ec4f286adfe4872184a21849c`, worktree limpa, `f9bb567`/`67e0187` ancestrais e zero mudança técnica posterior a `f9bb567`.
- Gates gerais: build Release de `17` projetos com zero erro/warning; `332/332` unitários, `30/30` arquitetura, `16/16` integração; cobertura `78,9%/49,51%`; Dashboard `60/60`; Pester `23` aprovados, `1` skip esperado e `32,08%`; formatação, bundle, NuGet sintético, assets, links, secrets e auditorias offline aprovados.
- Persistência/runtime normal: ambos os contextos sem model drift; zero warning EF `10102`; smoke com live `200`, protegidos `426`, quatro workers desabilitados e nenhuma persistência Agent inicializada; zero referência do host consolidado sob `src/`.
- Evidência correlacionada: `2` observações, `1` entrega, `2` journals, `0` `CommandAttempt`, `1/1` certificado revogado, fences `3 → 4`, snapshot concurrency `1`, zero HTTP externo e `operationalData=false`.
- Regressões: Agent Fleet/pipeline/comando/R1–R7 `10/10`, Dashboard/SignalR `4/4`, notificação `2/2`; runner browser passou todos os cenários com `1.248` requests locais, `27` WebSockets e zero origem externa.
- PowerShell: os dois runners modernos recusaram Windows PowerShell 5.1 com exit `1` e `ScriptRequiresUnmatchedPSVersion=true` antes de criar recursos; execução autorizada ocorreu em PowerShell 7.
- Cleanup: zero processo, listener, profile, store ou root pertencente à campanha permaneceu; um root de cobertura datado de 2026-07-19 foi preservado como preexistente; worktree limpa antes do relatório.
- Achados: nenhum crítico, alto ou médio aberto. O matcher inicial incluiu o próprio auditor e foi corrigido apenas na consulta; a auditoria offline e o root preexistente permanecem limitações factuais.
- Escopo: nenhum código, configuração executável, solução, projeto, package, lockfile ou migration foi alterado; nenhum recurso operacional, comando, notificação Windows, acesso externo, deploy, amostra humana, promoção ou transição ocorreu.
- Gates documentais da entrega: documentação aprovada para `280` fontes comment-capable; `451` links Markdown locais em `103` arquivos aprovados; secret scan, escopo exclusivo de cinco documentos e `git diff --check` aprovados.
- Evidência: [relatório da nova repetição](../../docs/STATE-06-Consolidated-Quality-Gate-Post-Revocation-Remediation-Rerun-Report.md), [proposta autorizada](../../docs/STATE-06-Consolidated-Quality-Gate-Post-Revocation-Remediation-Rerun-Proposal.md), relatório anterior `REPROVADO` e outputs sanitizados da campanha.
- Gates: Quality Gate consolidado atual `APROVADO`; repetição anterior `REPROVADA` historicamente; primeira campanha `BLOQUEADA` historicamente; amostra humana, Human Gate final, promoção e transição não autorizados.
- Próxima decisão: Bruno deve revisar o relatório. Se aceitar a classificação e desejar continuar, poderá autorizar separadamente somente uma proposta documental de amostras humanas finais; este registro não abre essas amostras.
- Aprovador: classificação automática; decisão humana sobre este relatório ainda não inferida.

## 2026-07-20 — Aceitação da nova repetição e proposta das amostras humanas finais

- Estado anterior: `STATE-06 INTEGRATION`, Quality Gate consolidado da baseline `84217c6` classificado `APROVADO`, relatório commit `2c1e05f` aguardando revisão humana e amostras finais não abertas.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, campanha automática aceita com as limitações registradas e proposta documental das seis amostras humanas pronta para revisão. Amostras e Human Gate final permanecem pendentes.
- Decisão: Bruno declarou exatamente `Nova repetição da Campanha Consolidada de Quality Gate do STATE-06 na baseline 84217c6, relatório commit 2c1e05f: ACEITA COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente a elaboração de uma proposta documental para as amostras humanas finais do STATE-06, sem execução, runtime, browser, Human Gate final, promoção ou transição de estado.`
- Efeito da aceitação: a classificação automática atual e suas limitações foram aceitas; a primeira campanha continua historicamente `BLOQUEADA` e a repetição no commit `66d0a9f` continua `REPROVADA`. A decisão não é o Human Gate final.
- Proposta: seis amostras identificadas como `S06-HG-001` a `S06-HG-006`, cobrindo perda/recuperação local, leitura imediata/hint/reconciliação TV, notificação Windows sintética, supressão de duplicata, evidência de comando não executável e verdade visual de origem/freshness/suporte.
- Elegibilidade: o runner consolidado corrente é headless e a campanha automática usa sink em memória. A futura sessão deverá compor somente artefatos já existentes; amostra que exija mudança técnica ficará `BLOQUEADA` e voltará para autorização separada.
- Limites futuros propostos: cadeia exclusivamente sintética, HTTPS loopback, Chrome dedicado visível com perfil efêmero, WPF apenas no sandbox opt-in existente, budgets, ownership, decisão humana individual e cleanup integral. Recurso operacional, origem externa, comando, executor e navegador comum permanecem proibidos.
- Autoridade desta atividade: somente documentação. Nenhum código, configuração executável, solução, projeto, package, lockfile, migration, build, teste de produto, runtime, browser, WPF, notificação ou acesso externo foi autorizado ou executado.
- Shutdown preflight: branch `main`, commit `2c1e05f`, worktree limpa e zero processo, listener ou navegador dedicado pertencente ao DB-Notifier antes da alteração documental; nenhum processo ou recurso alheio foi encerrado.
- Gates documentais: documentação aprovada para `280` fontes comment-capable; `459` links Markdown locais em `104` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; `git diff --check` aprovado. Build, testes de produto, runtime e browser são `NÃO APLICÁVEIS` e não foram executados.
- Evidências: [proposta das amostras](../../docs/STATE-06-Final-Human-Samples-Proposal.md), [relatório aceito da campanha](../../docs/STATE-06-Consolidated-Quality-Gate-Post-Revocation-Remediation-Rerun-Report.md), [plano consolidado](../../docs/STATE-06-Consolidated-Closure-Plan.md), governança de Quality/Human Gates e inspeção read-only dos adapters/runners existentes.
- Gates: Quality Gate consolidado atual `APROVADO` e aceito com limitações; amostras humanas `NÃO EXECUTADAS`; Human Gate final `PENDENTE` e não aberto; promoção e transição não autorizadas.
- Próxima decisão: Bruno pode pedir ajustes, adiar ou enviar exatamente o texto da seção `Decisão futura de Bruno` da proposta. Esse texto abrirá somente as seis amostras e não decidirá o Human Gate final.
- Aprovador: Bruno, 2026-07-20, exclusivamente para aceitação da campanha automática e elaboração desta proposta.

## 2026-07-20 — Campanha das amostras humanas finais do STATE-06

- Estado anterior: `STATE-06 INTEGRATION`, Quality Gate consolidado `APROVADO`, campanha automática aceita, proposta das seis amostras concluída e Human Gate final pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido, `S06-HG-002` a `S06-HG-005` aprovadas por Bruno, `S06-HG-001` e `S06-HG-006` bloqueadas e campanha humana classificada `BLOQUEADA`. Human Gate final não aberto.
- Autorização: Bruno autorizou somente as amostras `S06-HG-001` a `S06-HG-006` na baseline corrente contendo `84217c6` e `2c1e05f`, com artefatos existentes, HTTPS loopback, Chrome dedicado visível, WPF sandbox opt-in, cleanup e registro factual. Alteração técnica, acesso externo, recurso operacional, comando, executor, Human Gate, promoção e transição permaneceram proibidos.
- Baseline: branch `main`, commit `279bc7007b33aa3ce555d406b3a1b4840f8832ff`, worktree limpa, ancestralidade requerida confirmada e zero mudança técnica depois de `84217c6`.
- `S06-HG-001`: `BLOQUEADA`; o replay offline do Agent ocorre antes da apresentação e a UI disponível não distingue conexão do Agent de offline do navegador. A evidência automática não substituiu a amostra humana.
- `S06-HG-002`: auditor existente executado em Chrome dedicado visível; leitura imediata, hint, releitura, reconciliação de 30 segundos, offline/recovery e concorrência máxima `1` passaram. Bruno decidiu `S06-HG-002: APROVADA`.
- `S06-HG-003/004`: WPF sandbox produziu uma notificação sintética visível; ledger com uma entrada/uma tentativa e segundo ciclo sem nova publicação. A pedido de Bruno, a sessão foi repetida integralmente com o mesmo resultado. Bruno aprovou ambas explicitamente.
- `S06-HG-005`: duas sessões produziram dois journals, zero `CommandAttempt`, recusa pós-revogação e zero efeito administrativo. Bruno aprovou explicitamente.
- `S06-HG-006`: `BLOQUEADA`; origem sandbox, stale e suporte planejado ficaram visíveis, mas o único item interno `unknown` possui idade de `540.000 ms` e é corretamente apresentado como `Desatualizado`. Nenhuma fixture foi alterada.
- Evidência visível: Bruno apresentou imagem do Dashboard e imagem da notificação na conversa. Elas não foram copiadas para o repositório. Decisões exatas: `S06-HG-002: APROVADA`, `S06-HG-003: APROVADA`, `S06-HG-004: APROVADA` e `S06-HG-005: APROVADA`.
- Evidência sanitizada: visible audit com Chrome `150.0.7871.125`, duas observações, uma entrega, dois journals, zero attempt, concorrência `1`, `72` requests HTTP, `12` WebSockets, zero HTTP externo e `operationalData=false`; notificação/command foram repetidos uma vez.
- Incidentes de ferramenta: filtro de preflight inválido descartado e repetido; hand-off inicial sem browser limpo; primeira apresentação manual sem SignalR rejeitada; self-match do PowerShell no cleanup corrigido. Nenhum incidente foi convertido em passe ou causou mudança técnica.
- Cleanup: zero host, Chrome, WPF, profile, listener, ledger, SQLite, certificado, log ou root temporário pertencente às sessões; worktree final limpa. Nenhum navegador normal, IDE, banco, serviço ou processo alheio foi encerrado.
- Verificação do registro: gate de documentação aprovado para `280` arquivos de fonte passíveis de comentários, gate Markdown aprovado para `462` links locais em `105` arquivos e secret scan aprovado. Build, testes de produto e harnesses não foram repetidos durante o registro documental.
- Gates: Quality Gate automático permanece `APROVADO`; campanha humana `BLOQUEADA`; Human Gate final `PENDENTE` e não aberto; runtime operacional, promoção e transição não autorizados.
- Evidências: [relatório das amostras](../../docs/STATE-06-Final-Human-Samples-Report.md), [proposta consumida](../../docs/STATE-06-Final-Human-Samples-Proposal.md), relatório automático aceito e decisões explícitas de Bruno nesta sessão.
- Próxima decisão: Bruno poderá autorizar separadamente somente uma proposta documental de remediação de `S06-HG-001` e `S06-HG-006`. Implementação, repetição, Human Gate final, promoção e transição continuam fechados.
- Aprovador: Bruno, exclusivamente para `S06-HG-002` a `S06-HG-005`; bloqueios `S06-HG-001`/`006` são classificações factuais de elegibilidade, não decisões humanas inferidas.

## 2026-07-20 — Proposta documental de remediação das amostras humanas bloqueadas do STATE-06

- Estado anterior: `STATE-06 INTEGRATION`, Quality Gate consolidado `APROVADO`, campanha humana `BLOQUEADA`, `S06-HG-002` a `S06-HG-005` aprovadas, `S06-HG-001`/`S06-HG-006` bloqueadas e Human Gate final pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e todas as classificações anteriores mantidos. A proposta test-only está pronta para revisão e não autoriza sua própria implementação.
- Autorização: Bruno autorizou exclusivamente uma proposta documental cobrindo apresentação distinguível da perda/recovery do Agent, exemplo visual factual de `unknown` separado de `stale`, inspeção da fronteira `tests/scripts` versus `src` e critérios de repetição humana. Implementação, runtime, browser, acesso externo, Human Gate, promoção e transição permaneceram proibidos.
- Baseline: branch `main`, commit `b946c1f5d094d26292b3d70b364f2a6c1eb900a4`, worktree inicialmente limpa, ancestralidade de `84217c6` e `2c1e05f` confirmada e shutdown preflight com zero processo/listener pertencente ao DB-Notifier.
- Inspeção `S06-HG-001`: o harness executa perda e replay antes de publicar readiness; a evidência agregada comprova o replay, mas não oferece barriers humanos. Offline do browser permanece uma fronteira diferente e não será reutilizado como prova do Agent.
- Inspeção `S06-HG-006`: a fixture normal cria `demo-004` como `unknown` com idade de nove minutos, acima do limiar stale de cinco minutos; a UI corretamente prioriza `stale`. Contrato, child host e composição DI test-only já possuem as fronteiras necessárias para desenhar exemplos separados sem mudar essa regra.
- Fronteira proposta: implementação futura inicialmente limitada a `tests/`/`scripts/`, com superfície auxiliar sanitizada do harness e fonte de snapshot test-only pela API/Dashboard TV sandbox. Qualquer necessidade de `src/`, solução, projeto, package, lockfile ou migration obriga parada `BLOQUEADA` e nova autorização.
- Gates: documentação aprovada para `280` arquivos de fonte passíveis de comentários, links Markdown aprovados para `467` links locais em `106` arquivos e secret scan aprovado. Build, testes de produto, harness, runtime e browser não foram executados por estarem fora da autoridade documental.
- Evidência: [proposta de remediação](../../docs/STATE-06-Final-Human-Samples-Blocked-Remediation-Proposal.md), [relatório das amostras](../../docs/STATE-06-Final-Human-Samples-Report.md) e inspeção read-only dos artefatos citados.
- Próxima decisão: Bruno pode pedir alterações ou copiar exatamente a decisão futura da proposta para autorizar somente a implementação test-only e seu Quality Gate próprio. Repetição humana, Human Gate final, promoção e transição continuam separados.
- Aprovador: Bruno, exclusivamente para elaborar esta proposta documental.

## 2026-07-20 — Remediação test-only das amostras humanas finais bloqueadas

- Estado anterior: `STATE-06 INTEGRATION`, Quality Gate consolidado `APROVADO`, campanha humana `BLOQUEADA`, `S06-HG-002` a `S06-HG-005` aprovadas, `S06-HG-001`/`006` bloqueadas e proposta test-only concluída.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. A remediação test-only passou seu Quality Gate próprio, mas aguarda aceitação humana e não reclassifica nenhuma amostra.
- Autorização: Bruno autorizou somente harness/runners sob `tests/` e `scripts/`, barriers Agent, superfície auxiliar sanitizada, fonte test-only `unknown`/`stale`, fencing, budgets, cancelamento, testes locais, Chrome dedicado efêmero e documentação. Alteração sob `src/`, solução, projetos, packages, lockfiles ou migrations exigiria parada `BLOQUEADA`.
- Baseline: branch `main`, commit inicial `e6b604bed7ba1e532d99b82f1cf944dff6ee8ed3`, com `84217c6` e `2c1e05f` na ancestralidade e shutdown preflight sem runtime pertencente ao DB-Notifier.
- Implementação: marker exato `state06-final-human-samples-remediation`; barriers `ready → pending → replay accepted once → visual truth → completed`; página auxiliar inequivocamente test-only; fonte de snapshot de integração com um item `unknown` corrente e outro stale; auditor browser próprio; seletor de modo no runner consolidado com default anterior preservado.
- Fronteira: mudanças executáveis somente em `tests/`/`scripts/`; nenhum arquivo sob `src/`, solução, projeto, package, lockfile ou migration foi alterado. Composição normal e runtime operacional permanecem inalterados.
- E2E próprio: Agent loss observado com uma pendência e uma amostra Server preservada; browser/API disponível; recovery com pendência zero, duas amostras Server e replay único; `unknown`/`stale` visíveis; zero `CommandAttempt`; concorrência de snapshot `1`; `72` requests HTTP, `3` WebSockets, zero HTTP externo e `operationalData=false`.
- Regressão consolidada: aprovada com duas observações, uma entrega em sink de teste, dois journals, zero `CommandAttempt`, revogação, fences `3/4`, concorrência `1`, zero HTTP externo e `operationalData=false`.
- Gates gerais: build Release de `17` projetos com zero erro/warning; `332/332` unitários, `31/31` arquitetura, `18/18` integração; cobertura `78,9%/49,51%`; Dashboard `60/60`; Pester `23` aprovados, um skip condicional esperado e `32,08%`; formatação, typecheck/build, assets, documentação, links, segredos, npm offline, PowerShell 5.1 e integridade Git aprovados.
- Achados test-only: cultura/tipo exigidos por analisadores, expectativa inicial de dispatch e UUID sem bits versionados foram corrigidos exclusivamente no escopo autorizado. Nenhum achado exigiu mudança de produto ou relaxamento de evidência.
- Cleanup: zero processo, listener ou root temporário próprio depois dos E2E finais; roots órfãos atribuídos a tentativas anteriores do mesmo harness foram removidos antes da repetição.
- Gates: Quality Gate próprio `APROVADO` com limitações; campanha humana ainda `BLOQUEADA`; `S06-HG-001`/`006` não repetidas; Human Gate final não aberto; runtime operacional, promoção e transição não autorizados.
- Evidência: [relatório da remediação](../../docs/STATE-06-Final-Human-Samples-Test-Only-Evidence-Remediation-Report.md), [proposta autorizada](../../docs/STATE-06-Final-Human-Samples-Blocked-Remediation-Proposal.md), [relatório da campanha humana](../../docs/STATE-06-Final-Human-Samples-Report.md) e resultados sanitizados desta execução.
- Próxima decisão: Bruno deve revisar e decidir somente a aceitação desta remediação. Repetição humana de `S06-HG-001` e `S06-HG-006` exigirá autorização posterior e separada; as quatro amostras aprovadas não devem ser repetidas.
- Aprovador: Bruno autorizou a implementação; a aceitação humana do resultado não foi inferida.

## 2026-07-20 — Aceitação da remediação test-only das amostras humanas finais

- Estado anterior: `STATE-06 INTEGRATION`, remediação do commit `dd420d1` tecnicamente concluída, Quality Gate próprio `APROVADO`, aceitação humana pendente e campanha humana `BLOQUEADA`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. A remediação foi aceita com as limitações registradas; `S06-HG-001` e `S06-HG-006` continuam bloqueadas e não foram repetidas.
- Decisão: Bruno declarou exatamente `Remediação STATE-06 Final Human Samples Test-only Evidence, commit dd420d1: ACEITA COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo repetição das amostras humanas, Human Gate final, runtime operacional, promoção nem transição de estado.`
- Evidências revisadas: [relatório da remediação](../../docs/STATE-06-Final-Human-Samples-Test-Only-Evidence-Remediation-Report.md), principalmente resultado em linguagem simples, sequência E2E observada, limitações e condições residuais e classificação dos gates; commit `dd420d1`; decisão explícita de Bruno nesta sessão.
- Efeito: Quality Gate próprio e limitações aceitos; nenhuma reclassificação retroativa da campanha humana; `S06-HG-002` a `S06-HG-005` permanecem aprovadas e `S06-HG-001`/`006` permanecem bloqueadas.
- Autoridade: exclusivamente o registro factual desta decisão. Repetição humana, Human Gate final, runtime operacional, promoção e transição permanecem não autorizados.
- Shutdown preflight: branch `main`, commit `dd420d1`, worktree limpa e zero processo, listener ou root temporário pertencente ao DB-Notifier antes desta alteração documental; nenhum processo ou recurso alheio foi encerrado.
- Escopo desta ação: somente quatro documentos Markdown factuais; nenhum código, configuração executável, solução, projeto, package, lockfile, migration, build, teste de produto, browser ou runtime foi alterado ou executado.
- Gates documentais: documentação aprovada para `282` arquivos comment-capable; `473` links Markdown locais em `107` arquivos aprovados; secret scan do worktree não ignorado aprovado; escopo documental e `git diff --check` aprovados. Build, testes de produto, browser e runtime são `NÃO APLICÁVEIS` e não foram executados.
- Gates: Quality Gate próprio da remediação `APROVADO` e aceito com limitações; campanha humana `BLOQUEADA`; Human Gate final não aberto; promoção e transição não autorizadas.
- Próxima atividade: nenhuma ação técnica está autorizada por este registro. Se Bruno desejar continuar, deverá autorizar separadamente a repetição exclusiva de `S06-HG-001` e `S06-HG-006`; as quatro amostras já aprovadas não devem ser repetidas.
- Aprovador: Bruno, 2026-07-20, exclusivamente para aceitar a remediação e registrar essa decisão.

## 2026-07-20 — Repetição pós-remediação das amostras humanas `S06-HG-001` e `S06-HG-006`

- Estado anterior: `STATE-06 INTEGRATION`, remediação `dd420d1` aceita, `S06-HG-002` a `S06-HG-005` aprovadas, `S06-HG-001`/`006` bloqueadas e repetição ainda não aberta.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. `S06-HG-001` e `S06-HG-006` continuam `BLOQUEADAS`; campanha humana continua `BLOQUEADA` e Human Gate final não foi aberto.
- Autorização: Bruno autorizou somente a repetição das duas amostras sobre baseline contendo `dd420d1` e `cfd620f`, com cadeia sintética existente, Chrome dedicado visível, perfil efêmero e cleanup, sem mudança técnica, acesso externo, demais amostras, notificação, comando, executor, Human Gate, promoção ou transição.
- Baseline: branch `main`, commit `cfd620f9a4cdd6d5d6613d8d1498b82be36616a9`, worktree limpa, ancestralidade exigida e artefactos locais existentes confirmados.
- `S06-HG-001`: a página abriu em `agent-transport-ready`, Agent `available`, pendências `0`, uma amostra Server e Dashboard `Degradado`, mas mostrou `Browser → API indisponível`. O host registou `233` respostas `429`; a perda do Agent não foi avançada nem submetida a decisão humana.
- Causa: polling test-only a cada `250 ms`, endpoint sob `HumanApiRateLimit`, catch que fixa indisponibilidade e ausência de restauração positiva no caminho de sucesso.
- `S06-HG-006`: o host preparou `visual-truth-ready` numa tentativa separada, mas o helper encerrou antes do hand-off; uma invocação final foi recusada pelo parser antes de iniciar runtime. Nenhuma decisão humana foi solicitada ou inferida.
- Acesso externo: zero origem HTTP externa observada pelo helper CDP na apresentação válida inicial.
- Cleanup: zero host, helper, Chrome/profile, listener, SQLite, certificado, log ou root temporário pertencente às tentativas; worktree limpa e nenhum processo alheio encerrado.
- Escopo: nenhum código, configuração executável, solução, projeto, package, lockfile ou migration alterado; nenhum build, restore, download, recurso operacional, comando ou notificação executado.
- Gates: preflight/ancestralidade `APROVADO`; isolamento/cleanup `APROVADO`; `S06-HG-001` e `S06-HG-006` `BLOQUEADAS`; campanha humana `BLOQUEADA`; Human Gate final pendente; promoção e transição não autorizadas.
- Evidência: [relatório da repetição](../../docs/STATE-06-Final-Human-Samples-Post-Remediation-Repetition-Report.md), [relatório da remediação aceita](../../docs/STATE-06-Final-Human-Samples-Test-Only-Evidence-Remediation-Report.md), DOM sanitizado, contagem de `429` e decisões de parada desta sessão.
- Próxima atividade: nenhuma correção está autorizada. Para continuar, Bruno deverá autorizar separadamente somente uma proposta documental de remediação test-only da cadência/recovery da página e de um runner humano visível versionado. Nova repetição e Human Gate final permanecem separados.
- Aprovador: Bruno autorizou a repetição; nenhum passe humano foi inferido.

## 2026-07-20 — Proposta documental da segunda remediação test-only das amostras finais

- Estado anterior: `STATE-06 INTEGRATION`, campanha humana `BLOQUEADA`, `S06-HG-002` a `S06-HG-005` aprovadas e repetição pós-remediação de `S06-HG-001`/`006` bloqueada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. A nova proposta documental está pronta e não autoriza implementação, runtime ou amostra humana.
- Autorização: Bruno autorizou exclusivamente elaborar uma proposta para alinhamento da cadência ao rate limit, recuperação positiva de Browser → API, teste de duração humana e runner visível versionado com barriers persistentes.
- Baseline: branch `main`, commit `9b86923d3c80cfbdade14464910e1197b57483b7`, worktree limpa e shutdown preflight com zero processo, listener ou root temporário pertencente ao DB-Notifier.
- Inspeção factual: a página test-only tenta 240 leituras/minuto por `setInterval` de 250 ms contra uma política compartilhada de 100 permits/minuto; o catch trata toda falha como indisponibilidade e o sucesso não restaura o texto positivo. O runner versionado existente é headless e autoavança; não existe runner visível persistente no repositório.
- Proposta: polling serial de no máximo 30 leituras/minuto, concorrência `1`, estados distintos para `200`, `429`, `401/403` e falha de transporte/`5xx`, recovery positivo, ensaio de 180 segundos, falha `429` controlada e uma sessão visível versionada por amostra com barriers stage-gated.
- Fronteira: implementação futura exclusivamente sob `tests/`/`scripts/` e documentação. Necessidade de `src/`, solução, projetos, packages, lockfiles ou migrations exige parada `BLOQUEADA` e nova autorização.
- Gates documentais: documentação aprovada para `282` arquivos comment-capable; links Markdown aprovados para `480` links locais em `109` arquivos; secret scan do worktree não ignorado, escopo documental e `git diff --check` aprovados.
- Não executado: build, testes de produto, harness, runtime, browser, acesso externo, repetição humana, Human Gate final, promoção ou transição.
- Evidência: [proposta da segunda remediação](../../docs/STATE-06-Final-Human-Samples-Second-Test-Only-Remediation-Proposal.md), [relatório da repetição bloqueada](../../docs/STATE-06-Final-Human-Samples-Post-Remediation-Repetition-Report.md) e inspeção read-only dos artefatos test-only.
- Próxima decisão: Bruno pode solicitar alterações ou copiar a decisão futura da proposta para autorizar somente implementação test-only, Quality Gate automático e relatório. Repetição humana e Human Gate final permanecem decisões posteriores.
- Aprovador: Bruno, exclusivamente para elaborar esta proposta documental.

## 2026-07-20 — Segunda remediação test-only de estabilidade e runner persistente

- Estado anterior: `STATE-06 INTEGRATION`, segunda proposta documental concluída, campanha humana `BLOQUEADA` e `S06-HG-001`/`006` ainda bloqueadas.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. A remediação passou seu Quality Gate próprio e aguarda aceitação humana do resultado; nenhuma amostra foi repetida.
- Autorização: Bruno autorizou somente harness/runners/auditores sob `tests/` e `scripts/`, polling serial máximo 30/minuto, estados tipados, recovery positivo, gate mínimo de 180 segundos, falha `429` controlada, Chrome dedicado e documentação, com cleanup integral.
- Baseline: branch `main`, commit `d3a61a9613192ad880d42d336313bd2baeb422f9`, worktree limpa e shutdown preflight com zero processo, listener ou root STATE-06 pertencente ao DB-Notifier.
- Implementação: polling serial de 2,1 segundos; estados `available`/`limited`/`denied`/`unavailable`; restauração positiva; deadline, backoff e fencing; contadores bounded; selectors fechados `quality-gate`, `S06-HG-001` e `S06-HG-006`; runner/presenter visíveis versionados com uma amostra por sessão.
- Gate de duração: Chrome `150.0.7871.125`, respostas controladas `429`/`403`/`503` recuperadas, 180 segundos, 96 leituras, zero `429` real, máximo 29/minuto, concorrência 1, zero HTTP externo e zero `CommandAttempt`.
- Runner visível: smokes automáticos separados de `S06-HG-001` e `S06-HG-006` passaram com `humanDecisionRecorded=false`; nenhuma observação ou decisão de Bruno foi inferida.
- Regressão consolidada: duas observações, uma entrega em sink, dois journals, zero `CommandAttempt`, revogação, fences `3/4`, concorrência 1 e zero HTTP externo.
- Gates gerais: build Release de 17 projetos sem erro/warning; `332/332` unitários, `31/31` arquitetura, `19/19` integração, Dashboard `60/60`, cobertura `78,9%/49,51%`, formatação, documentação, links, segredos, assets, toolchain e npm offline aprovados.
- Pester: `23` testes aprovados, um skip esperado e cobertura `32,08%` no Windows PowerShell 5.1 compatível. No PowerShell 7, três matchers legados `Should Throw` falharam apesar de a execução direta comprovar as exceções; nenhum artefato legado foi alterado.
- Fronteira: alterações executáveis somente em `tests/`/`scripts/`; `src/`, solução, projetos, packages, lockfiles e migrations inalterados; nenhum acesso externo, recurso operacional, notificação, comando, executor, deploy, promoção ou transição.
- Cleanup: zero host, presenter, Chrome/perfil, listener, store, certificado, log ou root STATE-06 da tarefa ao final; uma pasta de cobertura preexistente de 2026-07-19 foi preservada e não atribuída a esta execução.
- Evidência: [relatório da segunda remediação](../../docs/STATE-06-Final-Human-Samples-Second-Test-Only-Remediation-Report.md), [proposta autorizada](../../docs/STATE-06-Final-Human-Samples-Second-Test-Only-Remediation-Proposal.md) e saídas sanitizadas dos gates.
- Próxima decisão: Bruno deverá revisar e aceitar ou rejeitar somente esta remediação. Nova repetição de `S06-HG-001`/`006`, Human Gate final, promoção e transição exigem autorizações posteriores separadas.
- Aprovador: Bruno autorizou a implementação; a aceitação humana do resultado não foi inferida.

## 2026-07-20 — Aceitação da segunda remediação test-only de estabilidade e runner persistente

- Estado anterior: `STATE-06 INTEGRATION`, segunda remediação do commit `9d65426` tecnicamente concluída, Quality Gate próprio `APROVADO`, aceitação humana pendente e campanha humana `BLOQUEADA`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. A remediação foi aceita com as limitações registradas; `S06-HG-001` e `S06-HG-006` continuam bloqueadas e não foram repetidas.
- Decisão: Bruno declarou exatamente `Remediação STATE-06 Final Human Samples Test-only Presentation Stability and Persistent Review Runner, commit 9d65426: ACEITA COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente o registro factual desta decisão. Não autorizo repetição das amostras humanas, Human Gate final, runtime operacional, promoção nem transição de estado.`
- Evidências revisadas: [relatório da segunda remediação](../../docs/STATE-06-Final-Human-Samples-Second-Test-Only-Remediation-Report.md), principalmente resultado em linguagem simples, sequência E2E observada, limitações e condições residuais e classificação dos gates; commit `9d65426`; decisão explícita de Bruno nesta sessão.
- Efeito: Quality Gate próprio e limitações aceitos; nenhuma reclassificação retroativa da campanha humana; `S06-HG-002` a `S06-HG-005` permanecem aprovadas e `S06-HG-001`/`006` permanecem bloqueadas.
- Autoridade: exclusivamente o registro factual desta decisão. Repetição humana, Human Gate final, runtime operacional, promoção e transição permanecem não autorizados.
- Shutdown preflight: branch `main`, commit `9d654262d7a451d82f049350c99b903f5da1d226`, worktree limpa e zero processo, listener ou root temporário pertencente ao DB-Notifier antes desta alteração documental; nenhum processo ou recurso alheio foi encerrado.
- Escopo desta ação: somente quatro documentos Markdown factuais; nenhum código, configuração executável, solução, projeto, package, lockfile, migration, build, teste de produto, browser ou runtime foi alterado ou executado.
- Gates documentais: documentação aprovada para `284` arquivos comment-capable; `485` links Markdown locais em `110` arquivos aprovados; secret scan do worktree não ignorado aprovado; escopo documental e `git diff --check` aprovados. Build, testes de produto, browser e runtime são `NÃO APLICÁVEIS` e não foram executados.
- Gates: Quality Gate próprio da remediação `APROVADO` e aceito com limitações; campanha humana `BLOQUEADA`; Human Gate final não aberto; promoção e transição não autorizadas.
- Próxima atividade: nenhuma ação técnica está autorizada por este registro. Se Bruno desejar continuar, deverá autorizar separadamente a repetição exclusiva de `S06-HG-001` e `S06-HG-006`; as quatro amostras já aprovadas não devem ser repetidas.
- Aprovador: Bruno, 2026-07-20, exclusivamente para aceitar a remediação e registrar essa decisão.

## 2026-07-20 — Repetição final pós-segunda-remediação de `S06-HG-001` e `S06-HG-006`

- Estado anterior: `STATE-06 INTEGRATION`, segunda remediação `9d65426` aceita e registrada em `129b9fd`, `S06-HG-002` a `S06-HG-005` aprovadas, `S06-HG-001`/`006` bloqueadas e Human Gate final não aberto.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. `S06-HG-001` e `S06-HG-006` foram aprovadas explicitamente por Bruno; a campanha das seis amostras humanas fica `CONCLUÍDA COM AS LIMITAÇÕES REGISTRADAS`, com seis decisões individuais `APROVADA`. Human Gate final permanece pendente e não aberto.
- Autorização: Bruno autorizou somente a repetição das duas amostras na baseline contendo `9d65426` e `129b9fd`, com runner visível versionado, uma amostra por sessão, barriers persistentes, runtimes locais temporários, Chrome dedicado efêmero e cleanup. Mudança técnica, acesso externo, recurso operacional, demais amostras, notificação, comando, executor, Human Gate, promoção e transição permaneceram proibidos.
- Baseline: branch `main`, commit `129b9fd4f7fc774b8ac9616115524660d569ffc9`, worktree limpa, ancestralidade exigida e shutdown preflight com zero processo, listener ou root temporário próprio.
- `S06-HG-001`: Bruno observou Agent disponível com zero pendência/uma amostra Server, Agent indisponível com uma pendência preservada/uma amostra Server e Agent recuperado com zero pendência/duas amostras/replay aceito exatamente uma vez, sempre com Browser → API disponível. Decisão exata: `S06-HG-001: APROVADA`.
- `S06-HG-006`: Bruno observou duas instâncias depois da reconciliação, uma `Desconhecido` corrente e outra `Desatualizado`, prazo visível sem renovação silenciosa, suporte planejado e origem sintética local. Decisão exata: `S06-HG-006: APROVADA`.
- Separação humana: os presenters registraram `humanDecisionRecorded=false`; as decisões vieram exclusivamente de Bruno na conversa e não foram inferidas das execuções automáticas.
- Condição terminal: os controles finais não foram acionados antes dos limites. `S06-HG-001` terminou por timeout de conclusão explícita e `S06-HG-006` por expiração do `unknown`, ambos depois das decisões; os presenters saíram com erro tipado e o cleanup integral passou. O resumo terminal positivo e sua contagem final de origens não foram produzidos.
- Evidência visível: quatro imagens da segunda sessão de `S06-HG-001` e duas imagens de `S06-HG-006` foram apresentadas na conversa e não copiadas ao repositório; os valores visíveis e as decisões estão no [relatório factual](../../docs/STATE-06-Final-Human-Samples-Second-Post-Remediation-Repetition-Report.md).
- Cleanup: depois de cada sessão, zero host, presenter, Chrome/profile, listener ou root `DBNotifier-State06-HumanReview-*`; worktree permaneceu limpa. Nenhum navegador comum, IDE, banco, serviço ou processo alheio foi encerrado.
- Escopo: nenhum código, configuração executável, solução, projeto, package, lockfile ou migration alterado; nenhum build, restore, download, notificação, comando ou recurso operacional executado.
- Gates documentais: documentação aprovada para `284` arquivos comment-capable; `487` links Markdown locais em `111` arquivos aprovados; secret scan do worktree não ignorado, escopo documental e `git diff --check` aprovados.
- Gates: Quality Gate consolidado automático permanece `APROVADO`; seis amostras humanas `APROVADAS`; campanha humana `CONCLUÍDA COM AS LIMITAÇÕES REGISTRADAS`; Human Gate final `PENDENTE` e não aberto; runtime operacional, promoção e transição não autorizados.
- Próxima decisão: Bruno deverá revisar o relatório factual. Uma eventual aceitação poderá autorizar separadamente somente uma proposta documental para o Human Gate final; não abrirá o gate, promoção ou transição.
- Aprovador: Bruno, exclusivamente para `S06-HG-001` e `S06-HG-006`; nenhuma decisão de Human Gate final foi inferida.

## 2026-07-20 — Aceitação da repetição final e proposta documental do Human Gate final

- Estado anterior: `STATE-06 INTEGRATION`, Quality Gate consolidado aprovado e aceito, seis amostras com decisões individuais `APROVADA`, relatório da repetição final `10a8249` aguardando aceitação e Human Gate final pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. O relatório humano foi aceito com as limitações registradas e a proposta documental do Human Gate final foi concluída; o gate continua `PENDENTE` e não aberto.
- Decisão: Bruno declarou exatamente `Repetição final das amostras humanas S06-HG-001 e S06-HG-006 do STATE-06 na baseline 129b9fd, relatório commit 10a8249: ACEITA COM AS LIMITAÇÕES REGISTRADAS. AUTORIZO exclusivamente a elaboração de uma proposta documental para o Human Gate final do STATE-06, sem execução, runtime, browser, promoção ou transição de estado.`
- Baseline: branch `main`, commit `10a82498d9c04a6eb7a61c08d20af4775bcd9f92`, worktree limpa e shutdown preflight com zero processo, listener ou root STATE-06 pertencente ao DB-Notifier.
- Inspeção de governança: `Quality-Gates.md`, `Lifecycle.md`, `Governance.md`, template de Human Gate e plano consolidado confirmam que a decisão deve nomear um único estado, incluir relatório automático, amostras, cobertura pendente e ressalvas e permanecer separada da transição.
- Proposta: [Human Gate final do STATE-06](../../docs/STATE-06-Final-Human-Gate-Proposal.md), com baseline, evidência automática, seis amostras, limitações obrigatórias, elegibilidade, formato das três decisões possíveis, condições de parada e autorização futura separada.
- Autoridade: somente documentação. Nenhum build, teste, runtime, browser, WPF, acesso externo, correção, promoção ou transição foi autorizado ou executado.
- Gates documentais: documentação aprovada para `284` arquivos comment-capable; `500` links Markdown locais em `112` arquivos aprovados; secret scan do worktree não ignorado, escopo exclusivo de seis documentos e `git diff --check` aprovados.
- Gates: Human Gate final `PENDENTE` e não aberto; runtime operacional, MOD-12 `none → OBSERVER`, `STATE-07`, promoção e transição não autorizados.
- Próxima decisão: Bruno deverá revisar a proposta. Se concordar, poderá copiar somente o texto de `Decisão futura de Bruno` para autorizar a abertura documental e apresentação do resumo; essa autorização ainda não decidirá o gate.
- Aprovador: Bruno, exclusivamente para aceitar a repetição humana e elaborar a proposta documental.

## 2026-07-20 — Human Gate final do `STATE-06`

- Estado anterior: `STATE-06 INTEGRATION`, Quality Gate consolidado `APROVADO` e aceito com limitações, seis amostras humanas com decisões individuais `APROVADA`, proposta final concluída e Human Gate `PENDENTE`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. Human Gate final encerrado como `APROVADO COM RESSALVAS`; transição para `STATE-07` continua pendente e não autorizada.
- Autorização: Bruno autorizou somente shutdown preflight, inspeção read-only de elegibilidade, apresentação de um resumo único e solicitação inequívoca de decisão exclusivamente para `STATE-06`. Build, testes, runtime, browser, WPF, mudança executável, acesso externo, correção, promoção e transição permaneceram proibidos.
- Baseline: branch `main`, commit `1a27dca393f00bc683235d7f8898dcc86f5841e0`, worktree limpa, ancestralidade de `84217c6`, `2c1e05f`, `9d65426`, `129b9fd`, `10a8249` e `1a27dca`, zero mudança técnica posterior a `9d65426` e zero runtime próprio.
- Resumo apresentado: relatório automático `2c1e05f`, build/testes/cobertura, cadeia correlacionada, seis amostras humanas, cobertura pendente, limitações de sandbox/operação e separação entre gate e transição.
- Decisão: Bruno declarou exatamente `Human Gate final do STATE-06: APROVADO COM RESSALVAS. Aceito expressamente que a evidência está limitada a sandbox local sintético; providers, PostgreSQL, PKI, escala, endurance e operação real não foram homologados; o transporte de comandos permaneceu não executável; e os presenters finais encerraram por condições bounded posteriores às decisões, embora o cleanup tenha passado. Confirmo a decisão acima exclusivamente para STATE-06.`
- Ressalvas: sandbox sintético; nenhuma homologação de provider/PostgreSQL/PKI/escala/endurance/operação; comando não executável; presenters bounded depois das decisões; cleanup aprovado; limites complementares do relatório preservados.
- Evidência: [relatório do Human Gate final](../../docs/STATE-06-Final-Human-Gate-Report.md), [proposta consumida](../../docs/STATE-06-Final-Human-Gate-Proposal.md), [campanha automática](../../docs/STATE-06-Consolidated-Quality-Gate-Post-Revocation-Remediation-Rerun-Report.md), [repetição humana](../../docs/STATE-06-Final-Human-Samples-Second-Post-Remediation-Repetition-Report.md) e decisão explícita de Bruno nesta sessão.
- Escopo desta ação: somente documentação factual; nenhum build, teste, runtime, browser, WPF, acesso externo, correção, promoção ou transição executado.
- Gates documentais: documentação aprovada para `284` arquivos comment-capable; `502` links Markdown locais em `113` arquivos aprovados; secret scan do worktree não ignorado, escopo exclusivo de seis documentos e `git diff --check` aprovados.
- Gates: Quality Gate consolidado `APROVADO`; Human Gate final `APROVADO COM RESSALVAS`; `STATE-07`, runtime operacional, promoção e transição `NÃO AUTORIZADOS`.
- Próxima decisão: Bruno poderá autorizar separadamente somente uma proposta documental de transição `STATE-06 → STATE-07`. A proposta não deverá executar nem inferir a transição.
- Aprovador: Bruno, 2026-07-20, exclusivamente para o Human Gate final do `STATE-06`.

## 2026-07-20 — Proposta documental da transição `STATE-06 → STATE-07`

- Estado anterior: `STATE-06 INTEGRATION`, Quality Gate consolidado `APROVADO`, Human Gate final `APROVADO COM RESSALVAS` e transição ainda não autorizada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. A proposta de handoff para `STATE-07 TESTING_HOMOLOGATION` foi concluída e não autoriza sua própria execução.
- Autorização: Bruno autorizou exclusivamente uma proposta documental incorporando gates encerrados, ressalvas herdadas, critérios de entrada, handoff e proibições, sem transição, build, testes, runtime, browser, acesso externo, implementação, promoção ou ação operacional.
- Baseline: branch `main`, commit `96cf2488679c2b8b2abcccf8d6473d07c8c8d823`, worktree limpa, ancestralidade de toda a cadeia `84217c6` → `96cf248` e shutdown preflight com zero processo, listener ou root STATE-06 próprio.
- Handoff: contratos e E2E sandbox, Agent/identidade de teste, pipeline, TV/SignalR, notificação reconciliada, comando não executável, revogação, R1–R7, gates e histórico são transferidos como evidência de laboratório, não como homologação.
- Ressalvas: nenhum provider/engine/plataforma/topologia operacional homologado; PostgreSQL/PKI/IdP/vault/escala/endurance/HA/DR pendentes; comando não executável; SignalR não autoritativo; presenters bounded; `ADR-0007` proposed; MOD-12 em `none`.
- Proposta: [transição formal `STATE-06 → STATE-07`](../../docs/STATE-06-To-STATE-07-Transition-Proposal.md), com efeito documental limitado, critérios de entrada e parada, escopo futuro de homologação e autorização separada.
- Autoridade: somente seis documentos; nenhum código, configuração executável, solução, projeto, package, lockfile, migration, build, teste, runtime, acesso externo ou recurso operacional alterado/executado.
- Gates documentais: documentação aprovada para `284` arquivos comment-capable; `514` links Markdown locais em `114` arquivos aprovados; secret scan do worktree não ignorado, escopo exclusivo de seis documentos e `git diff --check` aprovados.
- Gates: Quality Gate `STATE-06` `APROVADO`; Human Gate `STATE-06` `APROVADO COM RESSALVAS`; transição `PENDENTE`; `STATE-07` ainda não ativo.
- Próxima decisão: Bruno deverá revisar a proposta e, se concordar, enviar exatamente a autorização futura nela contida. Qualquer campanha de `STATE-07` continuará dependendo de autoridade posterior própria.
- Aprovador: Bruno, exclusivamente para elaborar esta proposta documental.

## 2026-07-20 — Aceitação como direção da proposta MOD-12 AIOps operacional

- Estado anterior: `STATE-06 INTEGRATION`, proposta executiva/técnica do programa MOD-12 pronta para revisão, nenhum modo AIOps ativo, `ADR-0007` `proposed` e O1 não autorizado.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. A proposta MOD-12 foi aceita como direção estratégica e técnica, sem efeito executivo.
- Revisão: Bruno solicitou revisão da definição de conclusão, sequência dos modos, escopo do O1 e recomendação sobre PostgreSQL; em seguida respondeu exatamente `ACEITA COMO DIREÇÃO`.
- Decisão aceita: definição verificável de AIOps completa; sequência `OBSERVER → ADVISOR → ASSISTANT → CONTROLLED_AUTOMATION`; programa `O1`–`O5`; arquitetura provider-neutral; e PostgreSQL apenas como candidato recomendado à primeira homologação futura.
- Limites: a decisão não adota `ADR-0007`, não autoriza O1, código/configuração executável, migration, package, build/teste de produto, runtime, browser, acesso externo, telemetria/provider/banco real, LLM, recomendação, plano, executor, `none → OBSERVER`, promoção ou transição. A proposta de `STATE-06 → STATE-07` permanece separada e pendente.
- Shutdown preflight: branch `main`, commit `45fb3d3`, worktree limpa, `Stopped=0`, `RemainingProjectOwned=0`, `OwnedListeners=0`, `BlockingProjectWindows=0` e `DedicatedReviewBrowsers=0`; nenhum processo ou recurso alheio foi encerrado.
- Escopo desta ação: somente três documentos Markdown factuais; nenhum artefato executável ou runtime foi alterado ou executado.
- Gates documentais: documentação aprovada para `284` arquivos comment-capable; `517` links Markdown locais em `114` arquivos aprovados; secret scan do worktree não ignorado e histórico disponível aprovado; escopo documental e `git diff --check` aprovados. Build, testes, cobertura e runtime de produto são `NÃO APLICÁVEIS` e não foram executados.
- Evidência: [proposta MOD-12 aceita como direção](../../docs/STATE-06-MOD-12-Operational-AIOps-Programme-And-Restricted-Observer-Proposal.md), [estado corrente](Current-State.md) e [ADR-0007 ainda proposto](../../docs/architecture/ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md).
- Próxima decisão MOD-12: adoção, ajuste ou rejeição formal do `ADR-0007`, separada de qualquer autorização de implementação. A próxima decisão de ciclo de vida continua sendo a eventual transição formal `STATE-06 → STATE-07` por sua proposta própria.
- Aprovador: Bruno, 2026-07-20, exclusivamente para aceitar a proposta como direção.

## 2026-07-20 — Adoção arquitetural do `ADR-0007`

- Estado anterior: `STATE-06 INTEGRATION`, proposta MOD-12 aceita como direção, nenhum modo AIOps ativo, `ADR-0007` `proposed` e O1 não autorizado.
- Estado resultante: sem transição e sem promoção; `STATE-06 INTEGRATION` e MOD-12 em `none` mantidos. `ADR-0007` passa a `accepted` exclusivamente como decisão arquitetural.
- Decisão: Bruno declarou exatamente `ADR-0007: ADOTADO COMO DECISÃO ARQUITETURAL, SEM AUTORIZAÇÃO DE IMPLEMENTAÇÃO.`
- Efeito: as fronteiras de confiança distribuída, checkpoint durável, admissão de recursos, quarentena, verificador puro e primeira prova serial passam a ser arquitetura obrigatória para uma eventual implementação.
- Limites: a decisão não autoriza O1, código, configuração executável, migration, package, build/teste de produto, runtime, persistência, serviço, acesso externo, telemetria/provider/banco real, PostgreSQL, chave/credencial, LLM, recomendação, plano, executor, `none → OBSERVER`, promoção ou transição.
- Shutdown preflight: branch `main`, commit `b99d033`, worktree limpa, `Stopped=0`, `RemainingProjectOwned=0`, `OwnedListeners=0`, `BlockingProjectWindows=0` e `DedicatedReviewBrowsers=0`; nenhum processo ou recurso alheio foi encerrado.
- Escopo desta ação: somente sincronização factual do ADR, contratos e índices documentais correntes; relatórios e decisões anteriores que registravam o ADR como proposto foram preservados como evidência histórica.
- Gates documentais: documentação aprovada para `284` arquivos comment-capable; `521` links Markdown locais em `114` arquivos aprovados; secret scan do worktree não ignorado e histórico Git disponível aprovado; escopo documental e `git diff --check` aprovados. Build, testes, cobertura e runtime de produto são `NÃO APLICÁVEIS` e não foram executados.
- Gates: Quality Gate e Human Gate do `STATE-06` permanecem nos resultados já registrados. Implementação do ADR/O1 e promoção MOD-12 continuam `NÃO AUTORIZADAS`.
- Evidência: [ADR-0007 adotado](../../docs/architecture/ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md), [contrato de confiança e recursos](../../docs/architecture/AIOps-Trust-Governance-And-Resource-Envelope.md), [proposta MOD-12 aceita como direção](../../docs/STATE-06-MOD-12-Operational-AIOps-Programme-And-Restricted-Observer-Proposal.md) e [estado corrente](Current-State.md).
- Próxima decisão MOD-12: Bruno poderá autorizar ou não, de forma separada e explícita, o O1 com limites exatos; até essa decisão, nenhuma implementação ou ativação de `OBSERVER` é permitida. A decisão de ciclo de vida `STATE-06 → STATE-07` continua independente e pendente.
- Aprovador: Bruno, 2026-07-20, exclusivamente para adoção arquitetural do `ADR-0007`.

## 2026-07-20 — Aceitação do lote de remediação `R1`

- Estado anterior: `STATE-06 INTEGRATION`, R1 implementado no commit `878103d`, escopo automático R1 `APROVADO`, decisão humana pendente e gate .NET global bloqueado por uma inconsistência R0 preexistente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R1 e `AUD-H02` aceitos e fechados somente no escopo local bounded autorizado.
- Decisão: Bruno declarou exatamente `ACEITO O R1 COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.`
- Ressalva: a asserção `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources` já contradizia o workflow na baseline `3ee505ec55cab84f1af3043491355fb9f00b5f43`; permanece visível, bloqueante para o comando global e sem autorização de correção.
- Evidência: [relatório R1](../../docs/STATE-06-Audit-Remediation-R1-Report.md), [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md) e commit local `878103d30bdb17c2d0bc36cc038e0f57b376c70f`.
- Limites: a decisão não autoriza correção R0, notificação Windows visível, R2–R8, R7-A0/O1, LLM, recomendação, comando, automação, promoção de modo ou transição de lifecycle.
- Escopo desta ação: somente registro documental factual; nenhum código executável, build, teste, runtime, browser, acesso externo, banco/serviço real, push ou deploy.
- Aprovador: Bruno, 2026-07-20, exclusivamente para o R1 com a ressalva registrada.

## 2026-07-20 — Implementação local do lote de remediação `R2-A`

- Estado anterior: `STATE-06 INTEGRATION`, R0 aceito, R1 aceito com a ressalva global preexistente, R2-A autorizado exclusivamente sobre a baseline `40daad7a27a60da2e2cbb99c60aaed417a9591a2` e decisão humana do novo lote ainda inexistente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R2-A está implementado e `APROVADO` automaticamente no escopo local autorizado; aceitação humana permanece `PENDENTE`.
- Autorização: Bruno autorizou somente contenção local dos achados `AUD-H03`, `AUD-H04` e `AUD-M10`, alterações/builds/testes/runtimes locais/documentação/commit focado. R2-B, execução, comando administrativo, schema operacional, dados existentes, acesso externo e todos os lotes/modos/transições posteriores permaneceram proibidos.
- Baseline e preflight: branch `main`, commit `40daad7a27a60da2e2cbb99c60aaed417a9591a2`, worktree limpa e zero processo, listener ou janela pertencente ao DB-Notifier antes da implementação.
- Contenção normal: criação/poll/ack v1 retornam tombstone 503 tipado sem binding ou persistência; Server delivery store e Agent inbox/transport/worker não são registrados; `CommandPollingEnabled=true` falha em qualquer combinação.
- Sandbox: marker exato preservado; fencing, pending replay e recibos ficam em `command-transport-receipts.sqlite`, separado do banco normal do Agent; states são exclusivamente `Receipt*`; `ExecutionPolicy.Never` é persistida, revalidada e protegida por constraint SQLite.
- Dados e providers: nenhuma store operacional preexistente foi conectada e nenhuma row operacional foi consultada, alterada ou excluída; nenhuma migration operacional foi criada; PostgreSQL `Start`/`Stop`/`Restart` permanecem `Unsupported`; nenhum `CommandAttempt`, executor, post-probe ou credencial administrativa foi criado.
- Gates: build Release 18 projetos sem aviso/erro; 53 testes focados, 3 provas focadas de arquitetura e 1 E2E multiprocess aprovados; suíte ampla com 403 passes e a única falha R0 preexistente; arquitetura válida 34/34; cobertura 79,39%/50,72% com 10/10 componentes; format, 18 lockfiles offline, Pester/PowerShell 5.1, runtime fail-closed, bundle e documentação aprovados; shutdown final sem processo, listener, janela ou root temporário próprio.
- Ressalva: o gate global continua bloqueado exatamente pela asserção R0 que espera nomes fixos de artifacts, sem regressão nova e sem correção autorizada.
- Evidência: [relatório factual R2-A](../../docs/STATE-06-Audit-Remediation-R2A-Report.md) e [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md).
- Próxima decisão: Bruno deverá revisar o relatório e o handoff e decidir separadamente se aceita R2-A. Aceitação não autorizará R2-B, R3, comando, execução, AIOps ou transição.
- Aprovador: decisão humana ainda pendente; nenhum aceite foi inferido da autorização de implementação.

## 2026-07-20 — Aceitação do lote de remediação `R2-A`

- Estado anterior: `STATE-06 INTEGRATION`, R2-A implementado no commit `f1881451fea83a69ffd9a8db50e5b681264f8519`, escopo automático R2-A `APROVADO`, decisão humana pendente e gate .NET global bloqueado pela inconsistência R0 preexistente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R2-A e `AUD-H03`, `AUD-H04` e `AUD-M10` aceitos e fechados somente por contenção no escopo local bounded autorizado.
- Decisão: Bruno declarou exatamente `ACEITO O R2-A COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.`
- Ressalva: a asserção `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources` continua esperando nomes fixos de artifacts que contradizem o workflow R0 já aceito; permanece visível, bloqueante para o comando global e sem autorização de correção.
- Evidência: [relatório R2-A](../../docs/STATE-06-Audit-Remediation-R2A-Report.md), [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md) e commit local `f1881451fea83a69ffd9a8db50e5b681264f8519`.
- Limites: a decisão não autoriza R2-B, R3–R8, correção R0, comando, executor, provider control, banco/serviço real, R7-A0/O1, LLM, recomendação, automação, promoção de modo ou transição de lifecycle.
- Escopo desta ação: somente registro documental factual; nenhum código executável, build, teste, runtime, browser, acesso externo, banco/serviço real, push ou deploy.
- Próxima decisão: Bruno poderá solicitar separadamente a proposta de autorização do R3; isso não autoriza sua implementação.
- Aprovador: Bruno, 2026-07-20, exclusivamente para o R2-A com a ressalva registrada.

## 2026-07-20 — Implementação local do lote de remediação `R3`

- Estado anterior: `STATE-06 INTEGRATION`, R0 aceito, R1 e R2-A aceitos com a ressalva global preexistente, R3 autorizado exclusivamente sobre a baseline `ede62bb3617e106803b4109daa5f9df73de2484e` e decisão humana do novo lote ainda inexistente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R3 está implementado e `APROVADO` automaticamente no escopo local autorizado; aceitação humana permanece `PENDENTE`.
- Autorização: Bruno autorizou somente os achados `AUD-H05`, `AUD-H08`, `AUD-H11` e `AUD-M07`, alterações/builds/testes/runtimes locais, fixtures sintéticas, SQLite efêmero, loopback, documentação/evidência sanitizada e commit focado. Integrações reais, ativação normal, nova dependência, schema amplo, correção R0, R2-B, R4–R8, R7-A0/O1, comandos/modos/transições posteriores permaneceram proibidos.
- Baseline e preflight: branch `main`, commit `ede62bb3617e106803b4109daa5f9df73de2484e`, worktree limpa e zero processo, listener ou janela de produto pertencente ao DB-Notifier antes da implementação; a IDE do utilizador permaneceu intocada.
- Assignment boundary: Server valida provider/endpoint/referência/tags antes de construir resposta; Agent repete a validação antes de substituir SQLite e preserva LKG após recusa; a composição normal não registra provider distribuível.
- Identidade: principal é revogada e auditada antes de lotes retomáveis de até 128 certificados; CSR e certificado exigem SPKI idêntico antes do commit; issuer normal permanece indisponível.
- HTTP: leitores C# inventariados compartilham limite real por stream, content type, depth e schema; Dashboard TV aplica limite equivalente com UTF-8 fatal; arquitetura impede APIs de resposta não limitadas em `src/`.
- Schema: nenhuma migration foi necessária ou criada; nenhum SQL PostgreSQL foi gerado ou aplicado e nenhum banco real foi tocado.
- Gates: build Release sem aviso/erro; suítes R3, Agent Fleet, Dashboard, cobertura, arquitetura sem a contradição exata, format, Pester/PowerShell 5.1, runtime fail-closed, bundle, documentação, assets e secrets aprovados. O comando .NET global permanece não verde somente pela asserção R0 preexistente, inalterada e sem autorização de correção.
- Evidência: [relatório factual R3](../../docs/STATE-06-Audit-Remediation-R3-Report.md), [protocolo Agent/API](../../docs/architecture/Agent-API-Protocol.md) e [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md).
- Próxima decisão: Bruno deverá revisar o relatório e decidir separadamente se aceita R3. Aceitação não autorizará R2-B, R4, normal Agent Fleet, integração real, AIOps ou transição.
- Aprovador: decisão humana ainda pendente; nenhum aceite foi inferido da autorização de implementação.

## 2026-07-20 — Aceitação do lote de remediação `R3`

- Estado anterior: `STATE-06 INTEGRATION`, R3 implementado no commit `4752868988e1f4189d3ee61d77a4edc890b2c15f`, escopo automático R3 `APROVADO`, decisão humana pendente e gate .NET global bloqueado pela inconsistência R0 preexistente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R3 e `AUD-H05`, `AUD-H08`, `AUD-H11` e `AUD-M07` aceitos e fechados somente no escopo local bounded autorizado.
- Decisão: Bruno declarou exatamente `ACEITO O R3 COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.`
- Ressalva: a asserção `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources` continua esperando nomes fixos de artifacts que contradizem o workflow R0 aceito; permanece visível, bloqueante para o comando global e sem autorização de correção.
- Evidência: [relatório R3](../../docs/STATE-06-Audit-Remediation-R3-Report.md), [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md) e commit local `4752868988e1f4189d3ee61d77a4edc890b2c15f`.
- Limites: a decisão não autoriza correção R0, R2-B, R4–R8, Agent Fleet normal, issuer/provider/PKI/vault/credenciais reais, banco/serviço real, R7-A0/O1, LLM, recomendação, comando, automação, promoção de modo ou transição de lifecycle.
- Escopo desta ação: somente registro documental factual; nenhum código executável, build, teste, runtime, browser, acesso externo, banco/serviço real, push ou deploy.
- Próxima decisão: Bruno poderá solicitar separadamente a proposta de autorização do R4; isso não autoriza sua implementação.
- Aprovador: Bruno, 2026-07-20, exclusivamente para o R3 com a ressalva registrada.

## 2026-07-21 — Implementação local do lote de remediação `R4-A`

- Estado anterior: `STATE-06 INTEGRATION`, R0 aceito, R1/R2-A/R3 aceitos com a ressalva global preexistente, R4-A autorizado exclusivamente sobre a baseline `5bb43974572290734e66e787f152b903d9a1e1cb` e decisão humana do novo lote ainda inexistente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R4-A está implementado e `APROVADO` automaticamente no escopo local autorizado; aceitação humana permanece `PENDENTE`. `AUD-H07` continua contido e aberto.
- Autorização: Bruno autorizou somente `AUD-H06`, `AUD-M06`, `AUD-M08` e preservação de `AUD-H07`, com source/tests/docs, fixtures/processos sintéticos, SQLite efêmero, SQL PostgreSQL apenas gerado/inspecionado e commit focado. PostgreSQL/serviço/canal real, migration operacional, delivery, claim/lease/fence, nova dependência, acesso externo, R4-B, R5–R8, R7-A0/O1, comandos/modos/transições permaneceram proibidos.
- Baseline e preflight: branch `main`, commit `5bb43974572290734e66e787f152b903d9a1e1cb`; as alterações R4-A preexistentes no worktree foram preservadas, auditadas e corrigidas. Zero processo, listener ou janela atribuível ao DB-Notifier foi encontrado antes da ação.
- Roteamento: binding explícito regra/canal/ambiente com escopo de instância fail-closed; proveniência/idempotência evento-binding; FK composta impede canal divergente; pendência histórica sem prova é `Quarantined` sem inferência/envio/exclusão; downgrade recusa perda de evidência.
- Readiness: `/health/live` permanece process-only; `/health/ready` valida shape PostgreSQL não secreto, Npgsql, conectividade e migrations exatas sob cinco segundos, inclusive fronteira não cooperativa, e expõe somente status/código sanitizado.
- Deadline/processo: deadline começa antes de assignments e limita fonte não cooperativa; future skew usa uma constante canónica e não suspende probe; timeout/cancelamento encerram e observam a árvore sintética pai/filho antes de retornar.
- Delivery: configuração é validada antes de Kestrel; publisher normal continua indisponível e nenhum adapter normal existe. Claim/lease/fence/reclaim/idempotência no adapter/concorrência PostgreSQL não foram implementados; `AUD-H07` e R4-B permanecem abertos.
- Schema: uma migration Server mínima foi gerada com `dotnet-ef 10.0.9` cache-only; SQLite efêmero validou o modelo final; SQL PostgreSQL forward/reverse foi gerado e inspecionado sem conexão ou aplicação; zero model drift.
- Gates: build Release 18 projetos sem aviso/erro; 121 testes focados, 370/370 unitários, 2/2 arquitetura R4-A e 39/39 arquitetura exceto a contradição R0 passaram; cobertura 81,46%/52,76% com 10/10 componentes; EF, format, documentação, links e secrets passaram. O agregado manteve a falha R0 preexistente e 15 testes HTTPS sandbox não relacionados ficaram ambientalmente bloqueados pela identidade gerenciada sem credenciais Schannel; nenhuma correção fora do escopo foi feita. O shutdown final deixou zero runtime/listener do projeto; o único `dotnet` remanescente foi comprovado como build host da extensão C# da IDE e preservado.
- Evidência: [relatório factual R4-A](../../docs/STATE-06-Audit-Remediation-R4A-Report.md), [modelo lógico](../../docs/data/Logical-Model.md) e [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md).
- Próxima decisão: Bruno deverá revisar o relatório e o commit focado e decidir separadamente se aceita R4-A. Aceitação não autorizará R4-B, delivery, PostgreSQL real, R5, AIOps ou transição.
- Aprovador: decisão humana ainda pendente; nenhum aceite foi inferido da autorização de implementação.

## 2026-07-21 — Aceitação do lote de remediação `R4-A`

- Estado anterior: `STATE-06 INTEGRATION`, R4-A implementado no commit `a054ef00fa01751693cf28bd8e1ce68eeb9f288b`, escopo automático R4-A `APROVADO`, decisão humana pendente e gate .NET global não verde pela inconsistência R0 preexistente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R4-A e `AUD-H06`, `AUD-M06` e `AUD-M08` aceitos e fechados somente no escopo local bounded autorizado. `AUD-H07` continua contido e aberto.
- Decisão: Bruno declarou exatamente `ACEITO O R4-A COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.`
- Revisão independente anterior à decisão: build Release de 18 projetos sem aviso/erro; 370/370 unitários, 3/3 WPF, 21/21 integrações e 2/2 testes de arquitetura R4-A aprovados; cobertura de 81,46%/52,76%; EF sem drift; SQL forward/reverse inspecionado sem conexão/aplicação; format, documentação, links e secrets aprovados. O agregado permaneceu não verde somente pela asserção R0 preexistente, com 39/40 testes de arquitetura.
- Evidência histórica preservada: a limitação Schannel de 15 integrações na execução original permanece registrada; ela não se repetiu na revisão posterior e não foi reclassificada como defeito do R4-A.
- Limites: a decisão não autoriza correção R0, R4-B, delivery, PostgreSQL ou serviço real, migration operacional, R5–R8, R7-A0/O1, comando, LLM, recomendação, automação, promoção de modo ou transição de lifecycle.
- Escopo desta ação: somente registro documental factual; nenhum código executável, build, teste, runtime, browser, acesso externo, banco/serviço real, push ou deploy.
- Próxima decisão: Bruno poderá solicitar separadamente uma proposta de autorização para o lote seguinte; isso não autoriza implementação.
- Aprovador: Bruno, 2026-07-21, exclusivamente para o R4-A com a ressalva registrada.

## 2026-07-21 — Implementação local do lote de remediação `R4-B`

- Estado anterior: `STATE-06 INTEGRATION`, R0/R1/R2-A/R3/R4-A aceitos nos seus limites, `AUD-H07` contido e aberto, e R4-B autorizado exclusivamente sobre a baseline `20323defa34fdb7b0b475400b6bb474217042baf`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R4-B está implementado e `APROVADO` automaticamente no escopo local autorizado; aceitação humana permanece `PENDENTE`, portanto `AUD-H07` ainda não está registrado como aceito/fechado.
- Autorização: Bruno autorizou somente `AUD-H07`, ownership/fencing/retry/ambiguidade/dead-letter para as duas filas Server, uma migration mínima, laboratório PostgreSQL local descartável com imagem pinned já local, fixtures sintéticas, documentação, evidência sanitizada e commit focado. Download/pull, dependência nova, integração/canal/credencial real, PostgreSQL existente, migration operacional, push/deploy, R5–R8, R7-A0/O1, comandos/modos/transições permaneceram proibidos.
- Baseline e preflight: branch `main`, commit `20323defa34fdb7b0b475400b6bb474217042baf`, worktree limpa e zero processo, listener ou janela atribuível ao DB-Notifier antes da implementação; IDE e serviços do utilizador permaneceram intocados.
- Ownership: `FOR UPDATE SKIP LOCKED`, lease e retry pelo `clock_timestamp()` PostgreSQL, fence monotónico e owner/fence/expiry exatos protegem claim, hand-off, completion e reclaim; row lock no item mais antigo não bloqueia trabalho elegível posterior.
- Outcome: marker durável antecede o side effect; exceção, mismatch, cancelamento/expiração depois do marker tornam a row `Ambiguous` sem replay; retry explícito é bounded e o quinto attempt fica `DeadLettered`; nenhum outcome é apagado.
- Idempotência: outbox deriva chave estável do message ID e notification preserva a chave R4-A; publisher/adapter recebem a chave obrigatoriamente. O receptor sintético comprovou deduplicação, sem alegação de exactly-once externo.
- Contenção: delivery normal continua desabilitado e recusado antes do bind; `UnavailableServerMessagePublisher`, zero adapter, R2-A/R3/R4-A, Fleet normal desabilitado e issuer indisponível permanecem intactos.
- Schema: a oitava migration Server acrescenta somente ownership/fence/attempt availability/ambiguous/dead-letter e constraints/índices proprietários. EF não encontrou drift; SQL forward não faz data update e `Down` recusa downgrade destrutivo. Aplicação ocorreu somente no laboratório descartável, nunca em PostgreSQL existente ou operacional.
- Laboratório: Docker Desktop usou a imagem local `postgres:16-alpine`/`sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb`, `--pull never`, bind `127.0.0.1`, credencial efémera não impressa, recursos bounded e container/rede/volume próprios. A matriz PostgreSQL passou 1/1 para outbox e notifications; a auditoria de encerramento confirmou zero processo, recurso Docker exact-labelled ou diretório temporário R4-B residual.
- Gates: build Release 18 projetos sem aviso/erro; 371/371 unitários, 3/3 WPF, 22/22 integrações normais, laboratório 1/1 e arquitetura R4-B passaram; arquitetura sem a contradição R0 passou; cobertura 81,71%/52,22% com 10/10 componentes; EF, SQL, format, documentação, links e secrets passaram. O gate global permanece não verde exclusivamente pela asserção R0 preexistente, sem correção.
- Evidência: [relatório factual R4-B](../../docs/STATE-06-Audit-Remediation-R4B-Report.md), [modelo lógico](../../docs/data/Logical-Model.md), [desenvolvimento](../../docs/Development.md) e [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md).
- Próxima decisão: Bruno deverá revisar o relatório e o commit focado e decidir separadamente se aceita R4-B. Aceitação não autorizará delivery operacional, migration, canal real, R5, correção R0, AIOps ou transição.
- Aprovador: decisão humana ainda pendente; nenhum aceite foi inferido da autorização de implementação.

## 2026-07-21 — Aceitação do lote de remediação `R4-B`

- Estado anterior: `STATE-06 INTEGRATION`, R4-B implementado no commit `0ff89c0bb511810c5d1fee42a5a7106cc1f70e6d`, escopo automático `APROVADO`, decisão humana pendente e gate global não verde pela inconsistência R0 preexistente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R4-B e `AUD-H07` aceitos e fechados somente no escopo local bounded autorizado.
- Decisão: Bruno declarou exatamente `ACEITO O R4-B COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.`
- Ressalva: a asserção `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources` permanece visível e não verde, esperando nomes fixos de artifacts que contradizem o workflow R0 aceito; nenhuma correção foi autorizada ou executada.
- Evidência: [relatório R4-B](../../docs/STATE-06-Audit-Remediation-R4B-Report.md), [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md) e commit local `0ff89c0bb511810c5d1fee42a5a7106cc1f70e6d`.
- Limites: a decisão não autoriza delivery normal, migration operacional, publisher/adapter/canal/credencial real, PostgreSQL existente, correção R0, R2-B, R5–R8, R7-A0/O1, comando, LLM, recomendação, automação, promoção de modo ou transição de lifecycle.
- Escopo desta ação: somente registro documental factual; nenhum código executável, build, teste, runtime, acesso externo, banco/serviço real, push ou deploy.
- Próxima decisão: Bruno poderá solicitar separadamente a proposta de autorização do lote seguinte; isso não autoriza implementação.
- Aprovador: Bruno, 2026-07-21, exclusivamente para o R4-B com a ressalva registrada.

## 2026-07-21 — Implementação local do lote de remediação `R5`

- Estado anterior: `STATE-06 INTEGRATION`, R0/R1/R2-A/R3/R4-A/R4-B aceitos nos seus limites, R5 autorizado exclusivamente sobre a baseline `21dd72dbf5858001a2301f03adf4aa7a82d75798`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R5 está tecnicamente implementado e `APROVADO` automaticamente sob a conclusão pós-incidente; conformidade com a autoridade original permanece `REPROVADA` pelo incidente preservado e aceitação humana continua pendente.
- Autorização: somente `AUD-H09`, `AUD-H10`, `AUD-M09`, `AUD-M22`, `AUD-M23` e `AUD-M24`, com filesystem/processos sintéticos, fault injection, builds/testes offline, consulta read-only de proveniência das Actions oficiais e um commit local. Downloads, dependência nova, segredo/vault/provider/loader/packaging/serviço/banco real, artefacto distribuível, CI remota, push/deploy, R6–R8, R7-A0/O1, comandos, AIOps e transição permaneceram proibidos.
- Implementação: journal autenticado anterior ao replace e recuperação determinística do migrador; Linux Secret Service indisponível sem PATH; packages de árvore exata convertidos em snapshot imutável; toolchain v2 de closure completa com geração ainda recusada; Actions fixadas nos SHAs observados oficialmente; configuração canónica sem `pgIsReady`, notificação global respeitada e `RESTARTED` bounded.
- Proveniência: `git ls-remote --refs` contra `github.com/actions/{checkout,setup-dotnet,setup-node,upload-artifact}` resolveu `refs/tags/v4` para `11d5960a326750d5838078e36cf38b85af677262`, `67a3573c9a986a3f9c594539f4ab511d57bb3ce9`, `49933ea5288caeca8642d1e84afbd3f7d6820020` e `ea165f8d65b6e75b540449e92b4886f43607fa02`; nenhum clone, download, mirror ou credencial foi usado.
- Gates: build 18 projetos sem aviso/erro; 380 unitários, 22 integrações, 3 WPF e arquitetura 42/42 sem a única asserção R0 passaram; cobertura 81,88%/52,98%; Pester 32 aprovados/um skip previsto/32,38%; format, runtime fail-closed, documentação, links e secrets passaram. O agregado ficou 42/43 apenas pela falha R0 preexistente.
- Incidente de fronteira: o verificador de lockfiles foi chamado indevidamente e executou internamente `dotnet restore --locked-mode`, proibido neste lote. Não houve alteração de package cache, lockfile ou árvore do repositório, mas o cache HTTP NuGet recebeu quatro atualizações de service index/vulnerability metadata de `api.nuget.org`. O evento foi preservado, o gate não foi repetido e o R5 não foi declarado concluído sob a autoridade original.
- Continuação autorizada: depois de receber o relatório do incidente, Bruno declarou exatamente `AUTORIZO EXCLUSIVAMENTE A CONCLUSÃO LOCAL DO R5 APÓS O INCIDENTE DE METADADOS NUGET REGISTRADO, SEM NOVO RESTORE, DOWNLOAD OU ACESSO EXTERNO, LIMITADA À REVALIDAÇÃO OFFLINE, DOCUMENTAÇÃO FINAL E UM COMMIT FOCADO.` A decisão permite fechar tecnicamente e commitar, mas não torna a execução original retroativamente conforme nem aceita o R5 como Human Gate.
- Revalidação pós-incidente: preflight zero, build 18 projetos `--no-restore`, R5 unitário 29/29, fault injection apply/rollback e cobertura focada, arquitetura 42/42 sem apenas R0, bundle nos dois hosts PowerShell, Pester 32/32 com um skip previsto/32,38%, format `--no-restore`, documentação, 559 links, secrets, diff e Git integrity passaram sem novo restore, rede, download ou pacote. A suíte integral não foi repetida para não reabrir as duas tabs visíveis do teste sintético preexistente de `ping.exe`; zero processo permaneceu.
- Ressalvas: a asserção arquitetural global R0 preexistente permanece intocada e deve continuar explícita; loader, packaging e vault Linux normais permanecem indisponíveis.
- Próxima decisão: Bruno deverá revisar o relatório e o commit focado e decidir separadamente se aceita R5 com o incidente e a ressalva R0 registrados. Isso não autorizará R6, packaging, loader, vault real, AIOps ou transição.
- Aprovador: decisão humana pendente; nenhuma aprovação foi inferida da autorização de implementação.

## 2026-07-21 — Aceitação do lote de remediação `R5`

- Estado anterior: `STATE-06 INTEGRATION`, R5 implementado no commit `004f9e51f02ad4a366eb3a019cc4f9633aefaa67`, resultado técnico automático `APROVADO`, conformidade com a autoridade original `REPROVADA` pelo incidente NuGet preservado, decisão humana pendente e gate global não verde pela inconsistência R0 preexistente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R5 e `AUD-H09`, `AUD-H10`, `AUD-M09`, `AUD-M22`, `AUD-M23` e `AUD-M24` aceitos e fechados somente no escopo local bounded autorizado.
- Decisão: Bruno declarou exatamente `ACEITO O R5 COM AS RESSALVAS DO INCIDENTE DE METADADOS NUGET REGISTRADO E DO GATE GLOBAL R0 PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.`
- Ressalvas: a aceitação não reclassifica a execução original como conforme, não apaga o acesso de metadados NuGet registrado e mantém intocada e não verde a asserção global R0 preexistente; nenhuma correção fora do R5 foi autorizada ou executada.
- Evidência: [relatório R5](../../docs/STATE-06-Audit-Remediation-R5-Report.md), [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md) e commit local `004f9e51f02ad4a366eb3a019cc4f9633aefaa67`.
- Limites: a decisão não autoriza R6–R8, packaging, loader, vault/package/provider/canal/banco/serviço real, R7-A0/O1, AIOps, comando, LLM, recomendação, automação, promoção de modo ou transição de lifecycle.
- Escopo desta ação: somente registro documental factual; nenhum código executável, build, teste, restore, runtime, acesso externo, banco/serviço real, push ou deploy.
- Próxima decisão: Bruno poderá solicitar separadamente a proposta de autorização do R6, sem implementação; isso não concede autoridade de execução.
- Aprovador: Bruno, 2026-07-21, exclusivamente para o R5 com as duas ressalvas registradas.

## 2026-07-21 — Implementação local da fase automática do lote de remediação `R6`

- Estado anterior: `STATE-06 INTEGRATION`, R0/R1/R2-A/R3/R4-A/R4-B/R5 aceitos nos seus limites, incidente NuGet R5 e gate global R0 preservados, e R6 automático autorizado exclusivamente sobre a baseline `a8d67e35af0be55c9d01a6141677b52fb0f23ed4`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. A fase automática R6 está implementada e `APROVADA` no escopo local autorizado; aceitação humana R6 permanece `PENDENTE`.
- Autorização: somente `AUD-H12`, `AUD-M12`–`AUD-M21` e `AUD-L01`, camadas de apresentação/frontend, Design System, localização, geração determinística, processos sintéticos, browser headless dedicado, testes offline e commit focado. Restore, download, acesso externo, browser/perfil comum, amostra visível, Windows notification/configuration, fonte/provider/banco/credencial real, migration, CI/push/deploy, R2-B, R7–R8, R7-A0/O1, AIOps, comandos e transição permaneceram proibidos.
- Implementação: instância desabilitada visível mas fora da saúde corrente; snapshot e instante de aceitação atómicos; validação hostil equivalente; SignalR apenas hint autenticado para nova leitura HTTP; marca do flyout sincronizada; reduced motion somente leitura; UTC/zona local explícitos; gráficos demonstrativos separados de fonte autoritativa; enums desconhecidos como `Unknown`; paths WPF; work area/DPI/reflow/scroll; título por rota, IDs completos e registries gerados de manifest único.
- Gates: build Release de 18 projetos sem aviso/erro; 393/393 unitários, 63/63 focados, 3/3 WPF, 4/4 integrações TV e 11/11 contratos WPF; Dashboard type-check, 64/64 testes e build; coverage 81,92%/53,56%; matriz headless Chrome com 96 viewports e 32 rotas forced-colour em pt-BR/en-GB e Light/Dark; geração, documentação, links e secrets aprovados. O format encontrou somente indentação no switch alterado, corrigida antes da revalidação final.
- Ressalvas: o comando global de arquitetura permanece 43/44 exclusivamente pela asserção R0 preexistente que espera `state05-dashboard-failure.json`; nenhuma correção foi feita. O incidente NuGet R5 permanece registrado e nenhum restore ou acesso externo ocorreu. Windows 200%, mixed-DPI físico, Narrator, High Contrast físico, reduced motion físico e flyout visível permanecem `NÃO TESTADOS`.
- Evidência: [relatório factual R6](../../docs/STATE-06-Audit-Remediation-R6-Report.md) e [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md).
- Próxima decisão: Bruno poderá autorizar separadamente as amostras humanas visíveis bounded do R6. Isso não autoriza implementação adicional, R7/R8, R7-A0/O1, AIOps ou transição.
- Aprovador: decisão humana R6 pendente; nenhuma aceitação foi inferida da autorização da fase automática.

## 2026-07-21 — Execução interrompida das amostras humanas visíveis R6

- Estado anterior: `STATE-06 INTEGRATION`, fase automática R6 aprovada no commit `878a7ea285b332aa9eedc59f4f4a69d5ba001c27`, amostras humanas visíveis autorizadas sem implementação ou correção.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. Aceitação humana R6 continua `PENDENTE`.
- Execução: Dashboard aberto somente em Chrome dedicado, perfil temporário isolado, preview loopback e fixtures demonstrativas. Nenhum browser/perfil comum, acesso externo, notificação, configuração Windows, fonte operacional ou código foi usado ou alterado.
- Decisão da amostra: Bruno declarou exatamente `AMOSTRA R6-HV-D01 REPROVADA: gráfico de desempenho cortado durante o reflow`.
- Stop condition: corrigir o corte exige alteração de código e validação, ambas fora da autoridade. A execução parou imediatamente; forced colours visível, TV autoritativa e WPF/Tray ficaram `BLOQUEADAS`. High Contrast, reduced motion, Narrator, escala Windows 200% e mixed-DPI físicos permaneceram `NÃO TESTADOS`.
- Cleanup: árvore Chrome e preview exatos encerrados; listeners loopback `43450`/`43451` ausentes; diretório temporário exato removido; zero processo, listener ou perfil da revisão permaneceu. Browser comum, IDE e processos não relacionados ficaram intocados.
- Evidência: [relatório das amostras humanas R6](../../docs/STATE-06-Audit-Remediation-R6-Human-Samples-Report.md) e [relatório automático R6](../../docs/STATE-06-Audit-Remediation-R6-Report.md).
- Limites: nenhuma correção, diagnóstico mutante, novo harness, restore, download, acesso externo, R2-B, R7–R8, R7-A0/O1, AIOps, comando, automação, promoção ou transição foi executada.
- Próxima decisão: requer proposta e autorização separadas para diagnóstico/remediação focal do gráfico e repetição da amostra; isso não autoriza aceitar R6 nem retomar automaticamente as demais amostras.
- Aprovador: decisão humana limitada a `R6-HV-D01`; nenhuma aceitação R6 foi inferida.

## 2026-07-21 — Remediação automática focal `R6-G1` do gráfico de desempenho

- Estado anterior: `STATE-06 INTEGRATION`, R6 automático aprovado, `R6-HV-D01` reprovada por corte do gráfico no reflow, demais amostras visíveis interrompidas e R6-G1 autorizado exclusivamente sobre `c3c8ed10493084fd4155a439f5f9cd044b8db23e`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R6-G1 está automaticamente `APROVADO`; a decisão humana de `R6-HV-D01` não mudou e a aceitação R6 continua `PENDENTE`.
- Causa: o grid interno preservava a altura mínima intrínseca do SVG e da linha de horários dentro de uma altura total que já incluía padding; o `overflow: hidden` deliberado do card cortava o excedente vertical.
- Correção: `min-height: 0` somente no plot e SVG existentes; nenhuma série, eixo, ponto, rótulo ou source truth foi ocultado. WPF/Tray e contratos operacionais não mudaram.
- Gate regressivo: o harness existente passou a medir card/chart/axis/plot/SVG/times, zoom 100%/200%/400%, equivalentes de reflow, forced colours e TV. Antes da correção, o gate novo falhou nas três superfícies esperadas; depois, 72 amostras de contenção passaram.
- Autoritativo: o sandbox TV existente passou a exigir zero `.trend-chart` e source truth não vazio no snapshot autoritativo, além do retorno do gráfico demonstrativo rotulado fora desse modo; 12/12 cenários HTTPS loopback passaram, com zero request externo e zero dado operacional.
- Gates: TypeScript, 65/65 testes Dashboard, build normal, 120 viewports bilingues Light/Dark, 32 rotas forced colours, toolchain, tokens, localização, marca e provider registries passaram. Arquitetura permaneceu 43/44 somente pela asserção R0 preexistente; a seleção sem ela passou 43/43.
- Limites: nenhum restore, download, acesso externo, amostra visível, browser comum, Windows setting/notification, WPF/Tray, provider/banco/credencial real, R2-B, R7–R8, R7-A0/O1, AIOps, comando, automação, promoção ou transição foi executado.
- Evidência: [relatório R6-G1](../../docs/STATE-06-Audit-Remediation-R6-G1-Report.md), [relatório da execução humana interrompida](../../docs/STATE-06-Audit-Remediation-R6-Human-Samples-Report.md) e [relatório automático R6](../../docs/STATE-06-Audit-Remediation-R6-Report.md).
- Próxima decisão: requer proposta e autorização separadas para repetir visivelmente somente `R6-HV-D01`; R6 não está aceito e as demais amostras não são retomadas automaticamente.
- Aprovador: decisão humana R6 pendente; nenhuma aprovação da amostra ou do R6 foi inferida.

## 2026-07-21 — Repetição visível aprovada da amostra `R6-HV-D01`

- Estado anterior: `STATE-06 INTEGRATION`, R6 e R6-G1 aprovados automaticamente, `R6-HV-D01` inicialmente reprovada, repetição focal autorizada exclusivamente sobre `13418ee3be1929655737fe7f086337605358e7ba` e demais amostras ainda bloqueadas.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. A repetição remediada de `R6-HV-D01` está `APROVADA`; a aceitação humana R6 continua `PENDENTE`.
- Preparação: baseline e worktree limpas; build normal offline aprovado. Um primeiro Chrome preparatório criou abas extras por divisão do argumento DNS, foi descartado antes do hand-off e teve oito processos e perfil exatos removidos. A segunda janela usou perfil vazio, uma página DB Notifier visível, preview loopback e proxy fechado para destinos não loopback.
- Inspeção: checklist pt-BR/en-GB, Light/Dark, Overview, Inventory, Performance, TV demonstrativa, Enabled=false, source truth, IDs, fuso, teclado/foco, scroll/reflow e gráfico em 100%/200%/400%. Bruno respondeu exatamente `INSPEÇÃO R6-HV-D01 CONCLUÍDA: Sem observações`.
- Decisão: depois do cleanup e do resumo factual, Bruno declarou exatamente `AMOSTRA R6-HV-D01 REPETIDA — APROVADA`.
- Cleanup: browser dedicado ausente, árvore preview exata encerrada, listeners `2970`/`2971` ausentes, root temporário e ambos os perfis removidos, zero processo/ping/resíduo; browser comum, IDE e processos não relacionados intocados.
- Evidência: [relatório da repetição R6-HV-D01](../../docs/STATE-06-Audit-Remediation-R6-HV-D01-Repetition-Report.md), [relatório R6-G1](../../docs/STATE-06-Audit-Remediation-R6-G1-Report.md) e [relatório da reprovação inicial](../../docs/STATE-06-Audit-Remediation-R6-Human-Samples-Report.md).
- Limites: a decisão não aprova R6 completo, não retoma `R6-HV-D02`, `R6-HV-D03`, `R6-HV-W01`, `R6-HV-W02` ou `R6-HV-P01` e não autoriza correção R0, R2-B, R7–R8, R7-A0/O1, AIOps, comando, automação, promoção ou transição.
- Próxima decisão: Bruno poderá solicitar uma proposta separada para retomar as amostras visíveis R6 restantes, sem execução automática.
- Aprovador: Bruno, 2026-07-21, exclusivamente para a repetição remediada `R6-HV-D01`.

## 2026-07-21 — Retomada interrompida das amostras visíveis R6 restantes

- Estado anterior: `STATE-06 INTEGRATION`, fase automática R6 e R6-G1 aprovadas, repetição remediada de `R6-HV-D01` aprovada, amostras `R6-HV-D02`, `R6-HV-D03`, `R6-HV-W01`, `R6-HV-W02` e `R6-HV-P01` autorizadas para retomada sequencial visível sobre `2e3b93b4333d22947d21a0baa46b1b05adcaea9d`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. `R6-HV-D02` está `REPROVADA`; `R6-HV-D03`, `R6-HV-W01` e `R6-HV-W02` estão `BLOQUEADAS`; `R6-HV-P01` e suas cinco condições físicas estão `NÃO TESTADAS`. A aceitação humana R6 continua `PENDENTE`.
- Preparação D02: preflight com baseline exata, worktree limpa e zero runtime; build normal já disponível, sem nova geração; Chrome dedicado com perfil temporário, uma página DB Notifier, preview e debugging somente em loopback, destinos não loopback dirigidos a proxy local fechado e forced colours limitado ao navegador.
- Inspeção parcial: o pt-BR Overview foi suficiente para observar que o texto da rota selecionada ficava invisível e que a borda lateral do cartão Crítico desaparecia. A campanha não alega cobertura das demais rotas, idiomas, temas ou interações.
- Resposta preliminar: Bruno declarou exatamente `INSPEÇÃO R6-HV-D02 CONCLUÍDA: REPROVADA — o texto da rota selecionada fica invisível; a borda lateral do cartão Crítico desaparece em forced colours;`.
- Decisão: depois do cleanup e do resumo factual, Bruno declarou exatamente `AMOSTRA R6-HV-D02 REPROVADA: o texto da rota selecionada fica invisível e a borda lateral do cartão Crítico desaparece em forced colours`.
- Stop condition: os achados exigem diagnóstico, código/CSS e validação fora da autoridade. D03, W01 e W02 não foram abertos; P01 não foi observada; nenhuma correção, harness, fixture, configuração do Windows ou notificação foi tentada.
- Cleanup: árvore Chrome e preview identificadas por perfil, parentage e listener foram encerradas; listeners `53483`/`53484` ausentes; root temporário D02 removido; auditoria independente confirmou zero processo, listener ou diretório de amostra e baseline limpa antes da documentação.
- Evidência: [relatório das amostras restantes R6](../../docs/STATE-06-Audit-Remediation-R6-Remaining-Human-Samples-Report.md), [repetição aprovada de R6-HV-D01](../../docs/STATE-06-Audit-Remediation-R6-HV-D01-Repetition-Report.md), [R6-G1](../../docs/STATE-06-Audit-Remediation-R6-G1-Report.md) e [relatório automático R6](../../docs/STATE-06-Audit-Remediation-R6-Report.md).
- Limites: a decisão não aceita R6, não autoriza remediação ou repetição, não retoma as amostras posteriores e não autoriza correção R0, alteração do incidente NuGet R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, LLM, recomendação, automação, promoção ou transição.
- Próxima decisão: requer proposta e autorização separadas para diagnóstico e remediação focal dos dois defeitos forced-colours D02. Qualquer repetição ou retomada posterior também exigirá autoridade separada.
- Aprovador: Bruno, 2026-07-21, exclusivamente para a decisão de `R6-HV-D02`.

## 2026-07-21 — Remediação automática focal `R6-FC1` de forced colours

- Estado anterior: `STATE-06 INTEGRATION`, R6/R6-G1 aprovados automaticamente, repetição remediada D01 aprovada e `R6-HV-D02` reprovada por texto da rota invisível e borda lateral Crítico ausente; R6-FC1 autorizado exclusivamente sobre `639b67251771fce708f99092bc5baf5b45a9f53a`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R6-FC1 está automaticamente `APROVADO`; a decisão humana D02 permanece `REPROVADA` e a aceitação humana R6 continua `PENDENTE`.
- Causas: o item selecionado escolhia `Highlight`/`HighlightText`, mas permitia novo remapeamento automático do navegador; a regra genérica do último cartão removia a borda inline-end do quarto KPI independente, lacuna exposta quando forced colours removia a elevação.
- Correção: `forced-color-adjust: none` somente depois do mapeamento explícito a cores do sistema, foco `CanvasText`, contador Alerts também system-colour e override Overview-only que restaura a borda existente do último KPI. Nenhum conteúdo ou significado foi ocultado.
- Regressão pré-correção: teste focado `64/65`; browser pt-BR/Light falhou exatamente em selected-route/system/focus e em borda KPI; portas `7396`/`7397`, perfil, diagnóstico e runner foram removidos sem resíduo.
- Harness existente: forced colours ampliado de oito para 24 amostras por locale/theme — oito rotas × zoom 100%/200%/400% — medindo label, foco, `aria-current`, contador, quatro bordas de quatro KPIs, árvore acessível, overflow e gráfico. O Chrome dedicado agora bloqueia destinos não loopback por proxy local fechado e suprime rede em background.
- Gates finais: type-check, `65/65` Dashboard, build de 60 módulos, 120 viewports, 96 combinações forced-colours, toolchain, tokens, localização, 11 identidades/22 variantes, marca e 316 arquivos de documentação passaram. Arquitetura sem somente a asserção R0 passou `43/43`; o teste proprietário alcançou as novas asserções e falhou depois apenas no nome preexistente `state05-dashboard-failure.json`.
- Cleanup: portas finais `23907`/`23908` ausentes; zero processo, listener, perfil, evidência, diagnóstico ou root temporário do runner. Browser comum, WPF/Tray, Windows, serviços e dados não relacionados permaneceram intocados.
- Evidência: [relatório R6-FC1](../../docs/STATE-06-Audit-Remediation-R6-FC1-Report.md), [decisão humana D02](../../docs/STATE-06-Audit-Remediation-R6-Remaining-Human-Samples-Report.md), [R6-G1](../../docs/STATE-06-Audit-Remediation-R6-G1-Report.md) e [relatório automático R6](../../docs/STATE-06-Audit-Remediation-R6-Report.md).
- Limites: nenhuma amostra humana, WPF/Tray, marca/nomenclatura, restore, download, acesso externo, provider/banco/credencial real, correção R0, alteração do incidente NuGet R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, LLM, recomendação, automação, promoção ou transição foi executada.
- Próxima decisão: requer proposta e autorização separadas para repetir visivelmente somente `R6-HV-D02` sobre o commit focal; D03/W01/W02/P01 não são retomadas automaticamente.
- Aprovador: decisão humana D02 permanece a reprovação anterior; nenhuma aprovação de D02 ou R6 foi inferida do resultado automático.

## 2026-07-22 — Repetição visível aprovada da amostra `R6-HV-D02`

- Estado anterior: `STATE-06 INTEGRATION`, R6/R6-G1/R6-FC1 aprovados automaticamente, repetição remediada D01 aprovada, `R6-HV-D02` inicialmente reprovada e sua repetição focal autorizada exclusivamente sobre `da58418eaf6668e07c73299617d6f7d3a731ca66`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. A repetição remediada de `R6-HV-D02` está `APROVADA`; a reprovação original permanece preservada e a aceitação humana R6 continua `PENDENTE`.
- Preparação: baseline e worktree limpas; build normal já existente, portanto nenhuma geração foi executada. A janela dedicada usou perfil descartável, uma página DB Notifier visível, preview/debugging somente em loopback, proxy local fechado para destinos não loopback e forced colours limitado ao Chrome.
- Inspeção: checklist pt-BR/en-GB, Light/Dark, oito rotas, zoom 100%/200%/400%, rota selecionada, foco, contador Alerts, quatro limites dos KPIs, cores do sistema, gráficos, teclado, acessibilidade, provider fallback e estados textuais. Bruno respondeu exatamente `INSPEÇÃO R6-HV-D02 REPETIDA CONCLUÍDA: Sem observações`.
- Evidência visual: a captura sanitizada fornecida na conversa corroborou o Overview pt-BR com rota selecionada legível, quatro cartões KPI integralmente delimitados — inclusive a borda direita de Crítico —, estados textuais e gráfico contido. Nenhum binário de screenshot foi copiado ao repositório e uma única captura não foi apresentada como prova independente de toda a matriz.
- Decisão: depois do cleanup e do resumo factual, Bruno declarou exatamente `AMOSTRA R6-HV-D02 REPETIDA — APROVADA`.
- Cleanup: árvores dedicadas Chrome/preview encerradas por identidade; listeners `3978`/`3979` ausentes; root temporário exato removido; duas verificações confirmaram zero runtime, listener ou diretório da repetição. Browser comum, IDE, serviços, bancos e processos não relacionados permaneceram intocados.
- Gates documentais: `590` links Markdown locais em `129` arquivos, secret scan do worktree não ignorado e histórico disponível, classificação factual e `git diff --check` aprovados. Build, teste e runtime de produto foram `NÃO APLICÁVEIS` depois da amostra porque somente evidência Markdown foi alterada.
- Evidência: [relatório da repetição R6-HV-D02](../../docs/STATE-06-Audit-Remediation-R6-HV-D02-Repetition-Report.md), [relatório R6-FC1](../../docs/STATE-06-Audit-Remediation-R6-FC1-Report.md) e [relatório da reprovação inicial D02](../../docs/STATE-06-Audit-Remediation-R6-Remaining-Human-Samples-Report.md).
- Limites: a decisão não aprova R6 completo, não retoma `R6-HV-D03`, `R6-HV-W01`, `R6-HV-W02` ou `R6-HV-P01` e não autoriza correção R0, alteração do incidente NuGet R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, LLM, recomendação, automação, promoção ou transição.
- Próxima decisão: Bruno poderá solicitar uma proposta separada para retomar as amostras visíveis R6 restantes, sem execução automática.
- Aprovador: Bruno, 2026-07-22, exclusivamente para a repetição remediada `R6-HV-D02`.

## 2026-07-22 — Retomada interrompida após aprovação D03 e reprovação W01

- Estado anterior: `STATE-06 INTEGRATION`, R6/R6-G1/R6-FC1 aprovados automaticamente, repetições remediadas D01/D02 aprovadas e D03/W01/W02/P01 autorizadas sequencialmente sobre `4d79c7fd42d00703793259f33cc991e3af1dcb7f`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. `R6-HV-D03` está `APROVADA`, `R6-HV-W01` está `REPROVADA`, `R6-HV-W02` permanece `BLOQUEADA`, `R6-HV-P01` e suas cinco condições permanecem `NÃO TESTADAS`, e a aceitação humana R6 continua `PENDENTE`.
- D03: build local-test offline, host HTTPS e Chrome dedicado somente em loopback; fonte sintética autoritativa identificada, zero gráfico/sparkline demonstrativo, explicação de tendência indisponível, 56 snapshots/54 condicionais, concorrência máxima um, uma conexão SignalR sandbox e zero hint publicado. Bruno respondeu `INSPEÇÃO R6-HV-D03 CONCLUÍDA: Sem observações` e decidiu exatamente `AMOSTRA R6-HV-D03 APROVADA`.
- W01: build WPF Release `--no-restore` passou sem aviso/erro; processo dedicado em composição test-only quiet, sem listener e sem notificação Windows reportada. As imagens e inspeção parcial expuseram a ambiguidade de `Configuração`/`Configurações` e diferença visual perante o Web Dashboard prejudicial à consistência e compreensão inicial.
- Decisão W01: Bruno declarou exatamente `AMOSTRA R6-HV-W01 REPROVADA: as opções Configuração e Configurações são ambíguas e a diferença visual em relação ao Dashboard Web prejudica a consistência e a compreensão inicial`.
- Stop condition: W02 não foi aberta nem inferida; P01 não foi executada e cada condição física permaneceu não testada. Nenhuma correção de código, CSS, harness, Design System, nomenclatura ou Windows foi tentada.
- Preferência: o cleanup detectou mudança temporária de `pt-BR/light` para `pt-BR/dark`. Depois de parar o PID exato e remover runtime/listener/root, Bruno autorizou separadamente somente restaurar `dark → light`; o hash final coincidiu byte a byte com o valor pré-amostra.
- Evidência: [relatório desta retomada](../../docs/STATE-06-Audit-Remediation-R6-Remaining-Human-Samples-Resumption-Report.md), [repetição D02](../../docs/STATE-06-Audit-Remediation-R6-HV-D02-Repetition-Report.md), [R6-FC1](../../docs/STATE-06-Audit-Remediation-R6-FC1-Report.md) e [relatório automático R6](../../docs/STATE-06-Audit-Remediation-R6-Report.md).
- Limites: a decisão não aceita R6, não autoriza remediação ou repetição W01, não retoma W02/P01 e não autoriza correção R0, alteração do incidente NuGet R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, LLM, recomendação, automação, promoção ou transição.
- Próxima decisão: requer proposta e autorização separadas para diagnóstico/remediação focal dos defeitos W01; repetição e retomada posterior também exigem autoridade separada.
- Aprovador: Bruno, 2026-07-22, exclusivamente para as decisões individuais D03/W01 e para a restauração focal da preferência.

## 2026-07-22 — Remediação automática focal `R6-WPF1`

- Estado anterior: `STATE-06 INTEGRATION`, D01/D02 repetidas e D03 aprovadas, `R6-HV-W01` reprovada, W02 bloqueada e P01 não testada; R6-WPF1 autorizado exclusivamente sobre `2cafd5d0df874833a2041a44ffd66e110163c8c6`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R6-WPF1 está automaticamente `APROVADO`; a decisão humana W01 permanece `REPROVADA` até repetição visível separadamente autorizada, e a aceitação humana R6 continua `PENDENTE`.
- Causas: o catálogo partilhado distinguia destinos adjacentes por `Configuração`/`Configurações` em pt-BR e por `Configuration`/`Settings` em en-GB; a rail WPF reutilizava chrome escuro/muted reservado à identidade, divergindo da hierarquia neutra do Web e comprimindo rótulos em 190 DIP.
- Correção: rótulos canónicos `Configuração operacional`/`Operational configuration` e `Preferências`/`Preferences` propagados deterministicamente a menu, títulos, TopBar, acessibilidade e atalho Tray, sem alterar IDs/URLs/contratos. A rail WPF passou a superfícies/textos/bordas semânticos neutros, 230 DIP, seleção por texto/fundo/trilho e foco independente; Design System `3.1.3` recebeu matriz explícita para oito destinos, adaptações nativas e exclusividades.
- Gates: localização verificada; Dashboard 66/66, type-check e build; auditor headless 120 viewports e 96 forced-colours pt-BR/en-GB Light/Dark; WPF Release zero avisos/erros, arquitetura focal 12/12, WPF 3/3 e unitários 393/393; coverage proporcional 81,92% linhas/53,56% branches com dez componentes; tokens, marca, providers, documentação, 599 links, secrets e diff passaram.
- Gate global: 44/45 arquitetura; a única falha continuou sendo a asserção R0 preexistente que espera `state05-dashboard-failure.json`. Nenhuma correção, exclusão do resultado global ou bypass foi feito. O incidente NuGet R5 permaneceu registrado e nenhum restore/acesso NuGet ocorreu.
- Preferência e cleanup: SHA-256 local anterior/final `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`; zero processo, listener ou root temporário do lote. O auditor WPF que expõe janela não foi executado pela proibição de amostra visível; nenhum resultado humano foi inferido.
- Evidência: [relatório R6-WPF1](../../docs/STATE-06-Audit-Remediation-R6-WPF1-Report.md), [reprovação W01](../../docs/STATE-06-Audit-Remediation-R6-Remaining-Human-Samples-Resumption-Report.md) e [Design System 3.1.3](../../docs/design/DB-Notifier-Design-System.md).
- Limites: nenhuma amostra humana, W02/P01, notificação, configuração do Windows, rota/contrato/dado persistido, fonte/provider/banco real, restore/download/acesso externo, CI remota, push/deploy, correção R0, alteração do incidente R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, LLM, recomendação, automação, promoção ou transição foi executada.
- Próxima decisão: requer proposta e autorização separadas para repetir visivelmente somente `R6-HV-W01` sobre o commit focal. W02/P01 não retomam automaticamente.
- Aprovador: autorização de implementação R6-WPF1 por Bruno em 2026-07-22; nenhuma aprovação humana de W01 ou R6 foi inferida.

## 2026-07-22 — Repetição visível `R6-HV-W01` reprovada após R6-WPF1

- Estado anterior: `STATE-06 INTEGRATION`, R6-WPF1 automaticamente aprovado no commit `288500de9e14212eca29b9b91b3de930fcb0ecb6`, W01 pendente de repetição, W02 bloqueada e P01 não testada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. `R6-HV-W01` permanece `REPROVADA`, agora por paridade visual incompleta após a correção da nomenclatura. A aceitação humana R6 continua `PENDENTE`.
- Execução: build Release existente reutilizado; processo dedicado com `--notifications-quiet`, endpoint HTTPS loopback sem listener, ledger temporário e sujeito sintético. Nenhuma fonte operacional, conexão externa, notificação ou alteração de código foi executada.
- Observação: `Configuração operacional` e `Preferências` ficaram inequívocas, mas KPIs e providers não apresentaram os ícones equivalentes, regiões de instâncias/alertas/providers mantiveram organização diferente e o gráfico WPF mostrou essencialmente linhas sem eixos percentuais, horários, badge e estrutura informativa equivalentes ao Web.
- Decisão: Bruno declarou exatamente `AMOSTRA R6-HV-W01 REPETIDA — REPROVADA: o WPF ainda não reproduz a organização visual do Web; faltam ícones e componentes equivalentes e o gráfico apresenta apenas linhas sem eixos, rótulos e estrutura informativa`.
- Cleanup: PID dedicado `48132` encerrado por identidade sem Close; porta sem listener, root temporário removido, zero processo/listener/resíduo e preferência restaurada byte a byte ao SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`.
- Evidência: [relatório da repetição W01](../../docs/STATE-06-Audit-Remediation-R6-HV-W01-Repetition-Report.md) e [relatório automático R6-WPF1](../../docs/STATE-06-Audit-Remediation-R6-WPF1-Report.md).
- Limites: W02 permanece bloqueada; P01 e as cinco condições físicas permanecem não testadas. Nenhuma correção, R6 acceptance, R0, incidente R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, LLM, automação, promoção ou transição foi autorizada ou inferida.
- Próxima decisão: requer proposta e autorização separadas para inventariar e implementar paridade visual WPF/Web de ícones, componentes, organização e gráfico, seguida por nova repetição humana também separada.
- Aprovador: Bruno, 2026-07-22, exclusivamente para a decisão da repetição `R6-HV-W01`.

## 2026-07-22 — Remediação automática focal `R6-WPF2`

- Estado anterior: `STATE-06 INTEGRATION`, R6-WPF1 aprovado automaticamente no seu escopo de nomenclatura, `R6-HV-W01` reprovada por paridade visual incompleta, W02 bloqueada e P01 não testada; R6-WPF2 autorizado exclusivamente sobre `8e595de369a77aa0f534356ab618d0222e71d583`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R6-WPF2 está automaticamente `APROVADO` no seu escopo estrutural e informacional; a decisão humana W01 permanece `REPROVADA`, e a aceitação humana R6 continua `PENDENTE`.
- Correção: WPF passou a usar componentes code-native reutilizáveis para KPIs, ícones semânticos, pills de estado, distribuição de providers e desempenho; Overview, Instâncias, Alertas, Desempenho, Histórico, Configuração operacional, Providers e Preferências preservam o conjunto informacional partilhado do Web. O gráfico contém grade, eixo `100%`/`50%`/`0%`, horários `09:50`–`10:15`, duas séries, badge e nome acessível; provider assets gerados são consumidos com fallback neutro.
- Matriz automática: 64/64 amostras executáveis passaram nas oito rotas, pt-BR/en-GB, Light/Dark e `820×620`/`1180×760`. As 32 combinações `1920×1080` estão `NÃO TESTADAS` porque a área útil ativa mede `1920×1032` e nenhuma configuração do Windows foi alterada.
- Gates: solução Release offline com 0 avisos/erros; WPF 3/3, Presentation focal 66/66, arquitetura WPF 14/14, Dashboard 66/66, type-check e build; coverage 393/393 com 81,92% linhas/53,56% branches e dez componentes; toolchain Node.js 24.18.0/npm 11.16.0, tokens, marca, 11 identidades/22 variantes de providers, localização, 326 arquivos documentáveis, 607 links, secrets, formatação e diffs passaram.
- Gate global: 46/47 arquitetura; a única falha continua sendo a asserção R0 preexistente `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`, que espera o literal `state05-dashboard-failure.json`. Nenhuma correção, exclusão ou bypass foi feito. O incidente NuGet R5 permaneceu registrado e nenhum restore, download ou acesso a metadados ocorreu.
- Imutabilidade: Dashboard Web, `.csproj`, `Directory.Packages.props`, lockfiles, `package.json` e `package-lock.json` têm zero diff contra a baseline. Nenhuma nova dependência, harness, rota, contrato, dado ou capacidade operacional foi introduzida.
- Preferência e cleanup: SHA-256 anterior/final exatamente `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`; auditor registrou zero processo e estado residual, e os oito roots temporários exatos da campanha foram removidos.
- Evidência: [relatório R6-WPF2](../../docs/STATE-06-Audit-Remediation-R6-WPF2-Report.md) e [Design System 3.2.0](../../docs/design/DB-Notifier-Design-System.md). O identificador do commit focal está no hand-off final do incremento.
- Limites: nenhuma amostra humana, W02/P01, notificação, configuração do Windows, fonte/provider/banco real, restore/download/acesso externo, CI remota, push/deploy, correção R0, alteração do incidente R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, LLM, recomendação, automação, promoção ou transição foi executada.
- Próxima decisão: requer proposta e autorização separadas para repetir visivelmente somente `R6-HV-W01` após R6-WPF2. W02/P01 não retomam automaticamente.
- Aprovador: autorização de implementação R6-WPF2 por Bruno em 2026-07-22; nenhuma aprovação humana de W01 ou R6 foi inferida.

## 2026-07-22 — Repetição visível `R6-HV-W01` reprovada após R6-WPF2

- Estado anterior: `STATE-06 INTEGRATION`, R6-WPF2 automaticamente aprovado no commit `cf605f7fc775e22d7537ad39e27488e193feac8a`, W01 pendente de repetição, W02 bloqueada e P01 não testada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. `R6-HV-W01` permanece `REPROVADA`; a aceitação humana R6 continua `PENDENTE`.
- Execução: outputs Release existentes reutilizados; Web de referência em preview loopback e Chrome com perfil temporário/host resolution bloqueada; WPF com `--notifications-quiet`, endpoint HTTPS loopback sem listener, estado temporário e sujeito sintético. O audit de conexões encontrou zero tráfego não loopback. Nenhum build, restore, download, fonte operacional, notificação, código ou configuração permanente foi executado.
- Observações: status pills WPF com geometria divergente; texto de instância desabilitada fora do campo; gráfico circular de Providers cortado; reflow compacto inadequado; primeira coluna e densidade da tabela de instâncias insuficientes; truncamento e scroll horizontal em Alertas/Histórico; Configuração operacional e outras rotas ainda sem acabamento e uniformidade visual equivalentes ao Web.
- Decisão: Bruno declarou exatamente `AMOSTRA R6-HV-W01 REPETIDA — REPROVADA: persistem defeitos de acabamento e uniformidade entre WPF e Web, incluindo status pills divergentes, texto fora do campo, gráfico de Providers cortado, reflow inadequado e tabelas com espaçamento, colunas e conteúdo mal ajustados.`
- Limitação física: `1920×1080` permaneceu `NÃO TESTADA` porque a área útil ativa mede `1920×1032`; nenhuma configuração do Windows foi alterada. Nenhum resultado positivo foi inferido para variantes não mencionadas ou para o Tray/flyout.
- Cleanup: a primeira comparação de identidade recusou fail-closed um timestamp deserializado em formato dependente de cultura antes de qualquer mutação; a comparação UTC normalizada comprovou o mesmo PID/caminho/instante. WPF, Chrome e preview foram então encerrados por identidade exata, com zero processo/listener/janela/root residual e preferência restaurada byte a byte ao SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`.
- Evidência: [relatório da repetição W01 pós-R6-WPF2](../../docs/STATE-06-Audit-Remediation-R6-HV-W01-Post-WPF2-Repetition-Report.md) e capturas sanitizadas fornecidas por Bruno na inspeção humana, sem binários adicionados ao repositório.
- Limites: W02 permanece bloqueada; P01 e suas cinco condições permanecem não testadas. Nenhuma correção, R6 acceptance, R0, incidente R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, LLM, automação, promoção ou transição foi autorizada ou inferida.
- Próxima decisão: requer proposta e autorização separadas para remediar acabamento/uniformidade, pills e contenção de texto, gráfico de Providers, reflow e tabelas, seguida por nova repetição humana também separadamente autorizada.
- Aprovador: Bruno, 2026-07-22, exclusivamente para a decisão da repetição `R6-HV-W01`.

## 2026-07-22 — Remediação automática focal `R6-UI1`

- Estado anterior: `STATE-06 INTEGRATION`, R6-WPF2 automaticamente aprovado, `R6-HV-W01` reprovada por pills/texto, gráfico de Providers, reflow, espaçamento e tabelas, W02 bloqueada e P01 não testada; R6-UI1 autorizado exclusivamente sobre `321e04d83cdaeab6ad3a4bdedd929c60083788fc`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R6-UI1 está automaticamente `APROVADO` no seu escopo; a decisão humana W01 permanece `REPROVADA` até repetição visível separadamente autorizada, e a aceitação humana R6 continua `PENDENTE`.
- Causas: raio virtualmente infinito e medição horizontal não bounded deformavam pills; larguras fixas comprimiam ring/legenda; rotas mantinham tabelas desktop no tamanho mínimo; o template nativo do `DataGridCell` não aplicava o padding declarado ao conteúdo; providers aceitavam ellipsis; o auditor anterior não media texto/pill, gutters reais, card/ring ou foco visível de listas roláveis.
- Correção: pills com raio/padding bounded e texto contido; gráfico circular e legenda responsivos; providers completos sem ellipsis; composição desktop proporcional em `1180×760`; registros empilhados completos e zero scroll horizontal de página em `820×620`; gutters `Space3` no conteúdo real; peers raw-only permitem geometria sem duplicar leitura acessível.
- Matriz automática: 64/64 rotas executáveis passaram nas oito rotas, pt-BR/en-GB, Light/Dark e `820×620`/`1180×760`; 32 combinações `1920×1080` permaneceram `NÃO TESTADAS` porque a área útil mede `1920×1032`.
- Medidas: gutters de 12–13 px, gaps adjacentes mínimos de 24–25 px, margens card/ring positivas nos quatro lados e interseção visível de DataGrid compacto superior ao mínimo de uma linha.
- Gates: build Release offline 0 avisos/erros; WPF 3/3, Presentation 66/66, arquitetura focal 15/15, Dashboard 66/66; coverage 393/393, 81,92% linhas/53,56% branches e dez componentes; toolchain Node.js 24.18.0/npm 11.16.0, tokens, marca, 11 identidades/22 variantes de providers, localização, PowerShell 7/5.1, documentação, links, secrets, formatação e diff passaram.
- Gate global: 47/48 arquitetura; a única falha continuou sendo a asserção R0 preexistente `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`, que espera `state05-dashboard-failure.json`. Nenhuma correção, exclusão ou bypass foi aplicado; o incidente NuGet R5 permaneceu registrado e nenhum restore, download ou acesso a metadados ocorreu.
- Imutabilidade e cleanup: Dashboard Web, dependências e lockfiles têm zero diff; preferência anterior/final exatamente `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`; zero processo, listener, state root ou resíduo de coverage pertencente ao lote.
- Evidência: [relatório R6-UI1](../../docs/STATE-06-Audit-Remediation-R6-UI1-Report.md), [reprovação W01 pós-R6-WPF2](../../docs/STATE-06-Audit-Remediation-R6-HV-W01-Post-WPF2-Repetition-Report.md) e [Design System 3.2.0](../../docs/design/DB-Notifier-Design-System.md). O identificador do commit focal está no hand-off final.
- Limites: nenhuma amostra humana, W02/P01, notificação, configuração do Windows, fonte/provider/banco real, restore/download/acesso externo, CI remota, push/deploy, correção R0, alteração do incidente R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, LLM, recomendação, automação, promoção ou transição foi executada.
- Próxima decisão: requer proposta e autorização separadas para repetir visivelmente somente `R6-HV-W01` após R6-UI1. W02/P01 não retomam automaticamente.
- Aprovador: autorização de implementação R6-UI1 por Bruno em 2026-07-22; nenhuma aprovação humana de W01 ou R6 foi inferida.

## 2026-07-22 — Repetição visível `R6-HV-W01` reprovada após R6-UI1

- Estado anterior: `STATE-06 INTEGRATION`, R6-UI1 automaticamente aprovado no commit `c7479d853f20ceb45aab70173431e46480b458e9`, W01 pendente de repetição, W02 bloqueada e P01 não testada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. `R6-HV-W01` permanece `REPROVADA`, agora pela contenção Web incorreta observada após R6-UI1; a aceitação humana R6 continua `PENDENTE`.
- Execução: outputs Release existentes reutilizados; Web de referência em preview loopback e Chrome dedicado com perfil temporário, proxy fechado e resolução externa negada; WPF com ativação test-only completa e `--notifications-quiet`, endpoint HTTPS loopback sem listener, estado temporário e sujeito sintético. A auditoria encontrou zero conexão não loopback. Nenhum build, restore, download, fonte operacional, notificação, código ou configuração permanente foi executado.
- Observação: no Web compacto da rota Visão geral, o texto `Desabilitada · excluída da saúde atual` ultrapassou o status pill e invadiu a região reservada ao sparkline durante o reflow. A campanha parou nesse defeito material, sem inferir resultado positivo para as demais rotas, combinações de idioma/tema, variantes compactas ou Tray/flyout.
- Decisão: Bruno declarou exatamente `AMOSTRA R6-HV-W01 REPETIDA — REPROVADA: na versão Web, o texto “Desabilitada · excluída da saúde atual” ultrapassa o status pill e invade a área do sparkline durante o reflow`.
- Limitação física: `1920×1080` permaneceu `NÃO TESTADA` porque a área útil ativa mede `1920×1032`; nenhuma configuração do Windows foi alterada.
- Cleanup: duas comparações recusaram fail-closed a interpretação cultural do timestamp antes de qualquer encerramento. A leitura ISO preservada comprovou PID, caminho, comando quiet e instante exatos; WPF, Chrome e preview foram então encerrados por identidade, com zero processo, listener, janela, elemento Tray ou root residual e preferência restaurada byte a byte ao SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`.
- Evidência: [relatório da repetição W01 pós-R6-UI1](../../docs/STATE-06-Audit-Remediation-R6-HV-W01-Post-UI1-Repetition-Report.md) e captura sanitizada fornecida por Bruno na conversa, sem binário adicionado ao repositório.
- Limites: o resultado automático R6-UI1 permanece factual somente no seu escopo. W02 permanece bloqueada; P01 e suas cinco condições permanecem não testadas. Nenhuma correção, aceitação R6, correção R0, alteração do incidente R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, LLM, automação, promoção ou transição foi autorizada ou inferida.
- Próxima decisão: requer proposta e autorização separadas para remediação focal da contenção Web do estado desabilitado e da alocação responsiva entre status e sparkline; qualquer repetição humana posterior também exigirá autoridade separada.
- Aprovador: Bruno, 2026-07-22, exclusivamente para a decisão da repetição `R6-HV-W01`.

## 2026-07-22 — Remediação automática focal `R6-WEB1`

- Estado anterior: `STATE-06 INTEGRATION`, R6-UI1 automaticamente aprovado, `R6-HV-W01` reprovada pela contenção Web incorreta do status desabilitado, W02 bloqueada e P01 não testada; R6-WEB1 autorizado exclusivamente sobre `1a192a82b3879975e5ecd51a1ebfd156c9db52da`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R6-WEB1 está automaticamente `APROVADO` no seu escopo focal; a decisão humana W01 permanece `REPROVADA`, e a aceitação humana R6 continua `PENDENTE`.
- Causa: a grade de cinco colunas dependia do breakpoint global da viewport enquanto o painel esquerdo já estava estreito; o pill global mantinha no-wrap e podia receber uma track menor que o texto. O `overflow: hidden` do painel mascarava o descendente para o gate de overflow global, que não media os fragmentos do texto nem as interseções status/latência/sparkline.
- Correção: label mensurável dentro do badge; região bounded de status; áreas de grid nomeadas; container query pela largura real do painel; reflow ordenado em largura restrita; ramo autoritativo sem sparkline preservado. Nenhum texto, fonte, estado ou sparkline foi truncado, reduzido, ocultado ou removido para obter o resultado.
- Matriz Dashboard: 67/67 testes e 25/25 testes focais de apresentação; type-check/build offline; 128 viewports, 96 forced-colour rota/page-scale e 24 medições focais de zoom/reflow passaram em pt-BR/en-GB e Light/Dark. A submatriz focal usa `1180×760`/`820×620` em 100% e 200%, e `1280×900`/`1440×1000` em 400%, com larguras CSS efetivas de `320px` e `360px` no último caso.
- Sandbox TV autoritativo: 12 cenários bounded passaram em HTTPS loopback; zero origem HTTP externa, zero dado operacional, zero sparkline demonstrativa no snapshot autoritativo e containment geométrico aprovado.
- Gates complementares: toolchain instalada Node.js 24.18.0/npm 11.16.0; coverage focal 98,09% linhas/92,00% branches/85,71% funções; arquitetura Web/TV focal 5/5; marca, 11 identidades/22 variantes de providers, tokens, localização, 326 arquivos documentáveis, 619 links e secrets passaram.
- Gate global: 13/14 no filtro proporcional; a única falha continuou sendo a asserção R0 preexistente `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`, que espera `state05-dashboard-failure.json`. Nenhuma correção, exclusão ou bypass foi aplicado; o incidente NuGet R5 permaneceu registrado e nenhum restore, download ou acesso a metadados ocorreu.
- Imutabilidade: WPF/Tray, `package.json`, dependências, lockfiles, fixtures, contratos, fontes geradas e Design System 3.2.0 têm zero diff. Nenhuma nova dependência, harness, rota, dado ou capacidade operacional foi introduzida.
- Cleanup: a reaudição independente encontrou um perfil Chrome GUID do runner sem processo/listener associado; após validação exata de propriedade, o root foi removido. A prova final registrou zero processo, zero listener e zero root temporário correspondente.
- Evidência: [relatório R6-WEB1](../../docs/STATE-06-Audit-Remediation-R6-WEB1-Report.md), [reprovação W01 pós-R6-UI1](../../docs/STATE-06-Audit-Remediation-R6-HV-W01-Post-UI1-Repetition-Report.md) e [Design System 3.2.0](../../docs/design/DB-Notifier-Design-System.md). O identificador do commit focal está no hand-off final.
- Limites: nenhuma amostra humana, alteração WPF/Tray, W02/P01, notificação, configuração do Windows, fonte/provider/banco real, restore/download/acesso externo, CI remota, push/deploy, correção R0, alteração do incidente R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, LLM, recomendação, automação, promoção ou transição foi executada.
- Próxima decisão: requer proposta e autorização separadas para repetir visivelmente somente `R6-HV-W01` após R6-WEB1. W02/P01 não retomam automaticamente.
- Aprovador: autorização de implementação R6-WEB1 por Bruno em 2026-07-22; nenhuma aprovação humana de W01 ou R6 foi inferida.

## 2026-07-23 — Repetição visível `R6-HV-W01` aprovada após R6-WEB1

- Estado anterior: `STATE-06 INTEGRATION`, R6-WEB1 automaticamente aprovado no commit `eb14985615dd160500e40e56bd8e910789d98c7d`, W01 reprovada pela decisão humana pós-R6-UI1, W02 bloqueada e P01 não testada.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. `R6-HV-W01` está `APROVADA` na repetição posterior ao R6-WEB1; a aceitação humana R6 continua `PENDENTE`.
- Execução: outputs existentes reutilizados sem build; Web em preview loopback e Chrome com perfil temporário, proxy fechado e resolução externa negada; WPF com ativação test-only completa e `--notifications-quiet`. Nenhum restore, download, acesso externo, fonte operacional, notificação, código ou configuração permanente foi executado.
- Observação: Bruno concluiu a inspeção com a resposta exata `INSPEÇÃO R6-HV-W01 APÓS R6-WEB1 CONCLUÍDA: Sem observações`.
- Decisão: Bruno declarou exatamente `AMOSTRA R6-HV-W01 REPETIDA — APROVADA`.
- Cleanup: WPF, Chrome dedicado e preview foram encerrados por identidade exata sem usar o Close normal; preferência restaurada byte a byte ao SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`; zero processo, janela, elemento Tray, listener ou root temporário após três verificações de ausência.
- Evidência: [relatório da repetição W01 pós-R6-WEB1](../../docs/STATE-06-Audit-Remediation-R6-HV-W01-Post-WEB1-Repetition-Report.md). Nenhuma captura binária foi adicionada ao repositório.
- Histórico: os resultados W01 bloqueado/reprovados anteriores permanecem preservados e não foram reescritos.
- Limites: W02 permanece bloqueada; P01 e suas cinco condições permanecem não testadas. A decisão não aceita R6 completo nem autoriza correção R0, alteração do incidente R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, automação, promoção ou transição.
- Próxima decisão: qualquer aceitação R6, retomada W02/P01 ou progressão exige proposta e autoridade separadas.
- Aprovador: Bruno, 2026-07-23, exclusivamente para a repetição `R6-HV-W01` após R6-WEB1.

## 2026-07-23 — Mecanismo test-only W02 implementado

- Estado anterior: `STATE-06 INTEGRATION`, W01 aprovada após R6-WEB1, W02 bloqueada por ausência de mecanismo seguro, P01 não testada e aceitação humana R6 pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. O mecanismo automático W02 está `APROVADO` no escopo local autorizado; a amostra humana `R6-HV-W02` não foi executada e continua `BLOQUEADA` até autorização visível separada.
- Implementação: marker exato/único `--review-flyout-live-update`; três frames in-memory bounded que exigem exatamente as quatro identidades sintéticas; agregado e contagens derivados das mesmas quatro linhas; atualização no mesmo Dispatcher turn de mark/tooltip do Tray e mark/texto/contagem/linhas do flyout; semantic icons acompanham cada estado; hide para o timer e restaura somente a fixture normal de Tray/flyout.
- Isolamento: W02 não cria publisher, não inicia refresh normal, não ativa sandbox reconciliado e recusa confirmação de Close, fila legacy e fallback. Os três atalhos do shell secundário ficam desabilitados, uma guarda adicional recusa a navegação e a restauração não atualiza o shell oculto. A sequência não contém arquivo, HTTP, rede, persistência, comando ou integração operacional.
- Gates observados: build WPF e solução Release sem restore, ambos com 0 avisos/erros; WPF 10/10; unitários focados de apresentação 66/66 e completos 393/393; arquitetura focal 23/23; documentação, links, tokens, localização, secrets e format check aprovados. A arquitetura global ficou 51/52 exclusivamente pela falha R0 preexistente que procura `state05-dashboard-failure.json`. O primeiro build expôs somente CA1859 e passou depois da correção de tipo.
- Imutabilidade: nenhuma dependência, package, lockfile, schema, migration, fixture operacional ou contrato externo foi alterado; preferência local permaneceu no SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`.
- Evidência: [relatório R6-W02](../../docs/STATE-06-Audit-Remediation-R6-W02-Test-Mechanism-Report.md) e [Design System 3.2.1](../../docs/design/DB-Notifier-Design-System.md). O commit focal será registrado no hand-off.
- Limites: nenhuma amostra visível, notificação Windows, persistência, fonte real, acesso externo, push/deploy, correção R0, alteração do incidente R5, R7–R8, R7-A0/O1, AIOps, comando, automação, promoção ou transição foi executada.
- Próxima decisão: requer autorização separada e explícita para executar visivelmente apenas `R6-HV-W02`, seguida de cleanup e decisão humana própria. P01 e aceitação R6 não retomam automaticamente.
- Aprovador: autorização de implementação automática W02 por Bruno em 2026-07-23; nenhuma aprovação humana da amostra ou do R6 foi inferida.

## 2026-07-23 — Amostra visível `R6-HV-W02` aprovada

- Estado anterior: `STATE-06 INTEGRATION`, mecanismo automático W02 aprovado no commit `40bf9f57d65479b8d5f8f55ff5e5b1a36791c84b`, W02 bloqueada até autorização visível, P01 não testada e aceitação humana R6 pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. `R6-HV-W02` está `APROVADA`; a aceitação humana R6 continua `PENDENTE`.
- Execução: somente o WPF existente com marker exato `--review-flyout-live-update`, PID `28584` e início `2026-07-23T05:47:18.1360319Z`; flyout `Visão rápida da frota` aberto por UI Automation no ícone exato do DB Notifier. Nenhum build, restore, download, acesso externo, persistência, integração operacional, comando ou notificação Windows foi executado.
- Observação: Bruno concluiu a inspeção com a resposta exata `INSPEÇÃO R6-HV-W02 CONCLUÍDA: Sem observações`.
- Decisão: Bruno declarou exatamente `AMOSTRA R6-HV-W02 APROVADA`.
- Cleanup: o PID dedicado foi encerrado por identidade exata sem usar o Close normal; zero processo, listener, janela, elemento Tray ou root temporário; preferência preservada byte a byte no SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`.
- Evidência: [relatório da amostra W02](../../docs/STATE-06-Audit-Remediation-R6-HV-W02-Report.md). A captura sanitizada fornecida por Bruno permaneceu na conversa; nenhum binário foi adicionado ao repositório.
- Histórico: o bloqueio W02 anterior permanece preservado e não foi reescrito.
- Limites: P01 e suas cinco condições permanecem não testadas. A decisão não aceita R6 completo nem autoriza correção R0, alteração do incidente R5, R2-B, R7–R8, R7-A0/O1, AIOps, comando, automação, promoção ou transição.
- Próxima decisão: qualquer aceitação R6, execução P01 ou progressão exige proposta e autoridade separadas.
- Aprovador: Bruno, 2026-07-23, exclusivamente para a amostra `R6-HV-W02`.

## 2026-07-23 — Human Gate do lote R6 aprovado com ressalvas

- Estado anterior: `STATE-06 INTEGRATION`, fase automática R6 e remediações focais aprovadas nos seus escopos, D01/D02/D03/W01/W02 aprovadas, P01 não testada e aceitação humana do lote R6 pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. O Human Gate específico do lote R6 está `APROVADO COM RESSALVAS`; o lote está humanamente encerrado somente no seu escopo.
- Decisão: Bruno declarou exatamente `HUMAN GATE DO R6: APROVADO COM RESSALVAS — aceito que R6-HV-P01 e suas cinco condições físicas permanecem NÃO TESTADAS; reconheço que essa ausência limita a evidência de acessibilidade e DPI físico, sem invalidar as amostras automáticas e humanas aprovadas. Esta decisão encerra somente o lote R6 e não autoriza correção do R0, R7-A0, R8, O1, AIOps, execução operacional ou transição de lifecycle.`
- Ressalvas: High Contrast físico, reduced motion físico, Narrator, escala física Windows 200% e mixed-DPI entre monitores permanecem individualmente `NÃO TESTADOS`; nenhuma condição foi inferida como aprovada.
- Evidência: [relatório do Human Gate R6](../../docs/STATE-06-Audit-Remediation-R6-Human-Gate-Report.md), relatórios automáticos e decisões individuais D01/D02/D03/W01/W02 já registrados.
- Atividade documental: baseline `c781101a8607b0be1cf7fca593f6c7d1b1a92fc5`, worktree limpa, zero processo/listener e preferência preservada no SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`; nenhum código, runtime, push, deploy ou transição.
- Limites: a decisão não corrige ou dispensa R0, não altera o incidente R5 e não autoriza R7-A0, R8, O1, AIOps, execução operacional, promoção ou lifecycle.
- Próxima decisão: correção R0, R7-A0, R8, eventual P01 ou progressão exigem propostas e autorizações separadas.
- Aprovador: Bruno, 2026-07-23, exclusivamente para o Human Gate do lote R6.

## 2026-07-23 — R0-F1 corrige a asserção global preexistente

- Estado anterior: `STATE-06 INTEGRATION`, lote R6 humanamente aprovado com ressalvas, gate global local não verde somente pela asserção R0 que procurava nomes de diagnósticos no workflow em vez dos runners.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R0-F1 está automaticamente `APROVADO`, a asserção bloqueante está resolvida e o gate .NET local corrente está verde.
- Causa: os runners escrevem `state05-dashboard-failure.json` e `state06-consolidated-failure.json`, enquanto o workflow publica corretamente os diretórios sanitizados por `*.json`; o teste atribuía os nomes exatos ao arquivo de responsabilidade errado.
- Correção: nomes exatos verificados nos runners; paths sanitizados, `if-no-files-found: error`, retenção de sete dias, job e timeout verificados no workflow. Nenhuma verificação foi removida ou enfraquecida.
- Gates: primeira compilação focal bloqueada por CA1875 e corrigida para `Regex.Count`; repetição focal `1/1`, arquitetura `52/52`, WPF `10/10`, unitários `393/393`, integração `22/22`, solução `477/477`, build Release com zero avisos/erros e format aprovados, sempre offline e sem restore.
- Imutabilidade: workflow e runners STATE-05/06 permaneceram nos SHA-256 `E777E3EC83A6E9DC5EF7A8257045A84CA81C471EB0B1EA6BBB33243CB7C16DD5`, `CB628EF5FC2CB76374130245DD468425834A55856C4F6A58C9B612D5D4FA92FA` e `96EA2DC780038ACB3D1FB6DC739AD006BBBFAE69B573BF5E730F2F4CB08A6A33`; nenhuma dependência ou lockfile mudou.
- Evidência: [relatório R0-F1](../../docs/STATE-06-Audit-Remediation-R0-F1-Report.md). Relatórios históricos que registraram a falha permanecem factuais nas suas baselines e não foram reescritos.
- Limites: incidente NuGet R5 preservado; nenhuma CI remota, runtime de produto, acesso externo, push/deploy, R7-A0, R8, O1, AIOps, promoção ou transição.
- Próxima decisão: R7-A0 continua exigindo proposta e autorização separadas; R8 permanece posterior e igualmente não autorizado.
- Aprovador: autorização técnica R0-F1 por Bruno em 2026-07-23; nenhuma aceitação humana adicional ou lifecycle foi inferido.

## 2026-07-23 — R0-F1 aceito

- Estado anterior: `STATE-06 INTEGRATION`, R0-F1 automaticamente aprovado no commit `68e8aa7dee0bf1e59c7ac70b4398fa9e4a6becb9`, gate local corrente verde e aceitação humana pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R0-F1 está humanamente aceito no seu escopo focal.
- Decisão: Bruno declarou exatamente `ACEITO O R0-F1, SEM AUTORIZAR R7-A0, R8, O1, AIOps OU TRANSIÇÃO.`
- Evidência: [relatório R0-F1](../../docs/STATE-06-Audit-Remediation-R0-F1-Report.md), incluindo causa, diff focal, `52/52` arquitetura, `477/477` solução, build, format e hashes imutáveis dos runners/workflow.
- Atividade documental: baseline `68e8aa7dee0bf1e59c7ac70b4398fa9e4a6becb9`, worktree limpa, zero processo/listener e preferência preservada no SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`; nenhum código, runtime, push, deploy ou transição.
- Limites: a aceitação não autoriza R7-A0, R8, O1, AIOps, execução operacional, promoção ou lifecycle e não altera o incidente NuGet R5.
- Próxima decisão: qualquer atividade posterior exige proposta e autorização separadas.
- Aprovador: Bruno, 2026-07-23, exclusivamente para R0-F1.

## 2026-07-23 — R7-A0 corrige a fundação MOD-12 inativa

- Estado anterior: `STATE-06 INTEGRATION`, R0-F1 aceito, gate .NET local verde, R6 encerrado com ressalvas e R7-A0 autorizado exclusivamente sobre `ee16f302e703ccad7574c77cb374cd0f44905508`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R7-A0 está automaticamente `APROVADO` no escopo de `AUD-H13` e `AUD-M11`; aceitação humana permanece `PENDENTE`.
- Implementação: capability `observer-analysis` separada de `ActivationState=None`; duração de probe separada por outcome; relatório v2 completo-only com deadline absoluto, cancelamento e context revision; execução offline sem agregados parciais, com materialização imutável/bounded/cancellation-aware, exceções sanitizadas e proveniência `DeclaredOnly`.
- Vetores: cancelamento antes e durante trabalho, expiry entre fases, publicação stale, fonte hostil, overflow de enumeração, outcome separado e regressões criptográficas independentes de grant/revocation/tampering/rollback/checkpoint.
- Gates: MOD-12 `77/77`; unitários `399/399`; arquitetura `52/52`; WPF `10/10`; integração `22/22`; solução `483/483`; build Release com zero avisos/erros; coverage `81,98%` linhas/`53,85%` branches e dez componentes; format aprovado.
- Observação ambiental: a tentativa inicial de executar dois builds focais em paralelo causou somente disputa do arquivo intermediário Application; ambos foram repetidos sequencialmente e aprovados.
- Evidência: [relatório R7-A0](../../docs/STATE-06-Audit-Remediation-R7-A0-Report.md) e [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md).
- Limites: nenhum restore, download, acesso externo, runtime operacional, trust host, persistência, corpus/telemetria real, UI, provider, banco, O1, ativação `OBSERVER`, LLM, recomendação, comando, automação, R8, push, deploy ou transição.
- Próxima decisão: Bruno poderá aceitar ou rejeitar separadamente somente o R7-A0; qualquer O1, R8, AIOps operacional, promoção ou lifecycle exige autoridade posterior explícita.
- Aprovador: resultado automático local; decisão humana R7-A0 pendente.

## 2026-07-23 — R7-A0 aceito

- Estado anterior: `STATE-06 INTEGRATION`, R7-A0 automaticamente aprovado no commit `cf8ee87f50027fad25f7b5c0051940f03a9bef92`, com aceitação humana pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R7-A0, `AUD-H13` e `AUD-M11` estão humanamente aceitos e fechados somente no escopo bounded da fundação MOD-12 inativa.
- Decisão: Bruno declarou exatamente `ACEITO O R7-A0, SEM AUTORIZAR O1, OBSERVER, R8, AIOPS OPERACIONAL OU TRANSIÇÃO.`
- Evidência: [relatório R7-A0](../../docs/STATE-06-Audit-Remediation-R7-A0-Report.md), incluindo capability separada da ativação, complete-only publication, materialização offline bounded e os gates automáticos aprovados.
- Atividade documental: baseline `cf8ee87f50027fad25f7b5c0051940f03a9bef92`, worktree limpa e zero processo/listener no preflight; nenhum código, runtime, push, deploy ou transição.
- Limites: a aceitação não autoriza trust host, persistência, corpus/telemetria real, O1, `OBSERVER`, R8, AIOps operacional, execução, promoção ou lifecycle.
- Próxima decisão: qualquer R8, O1, ativação `OBSERVER`, AIOps operacional ou progressão exige proposta e autorização separadas.
- Aprovador: Bruno, 2026-07-23, exclusivamente para R7-A0.

## 2026-07-23 — R8 reauditoria consolidada concluída automaticamente

- Estado anterior: `STATE-06 INTEGRATION`, R0-F1 e R7-A0 aceitos, R6 encerrado com ressalvas, R8 autorizado exclusivamente sobre `6e529810aee82446c9f2863363201ac2ef25d151`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. R8 está automaticamente `APROVADO`; aceitação humana permanece `PENDENTE`.
- Matriz: 39 achados originais classificados exatamente uma vez — 35 `ENCERRADO`, quatro `CONTIDO`, zero `ABERTO`, zero `BLOQUEADO` e zero achado `NÃO TESTADO`.
- Gates: build Release sem avisos/erros; solução `483/483`; coverage `81,98%` linhas/`53,85%` branches; Dashboard `67/67`; E2E e browser audit aprovados; matriz WPF `64/64` executável; Pester, segurança, supply chain, normal composition fail-closed, packaging bloqueado e cleanup aprovados.
- Limitações: incidente NuGet R5 preservado; locked restore, advisory freshness online, CI remota, repetição atual do laboratório PostgreSQL e cinco condições físicas R6 permanecem `NÃO TESTADOS`.
- Evidência: [relatório R8](../../docs/STATE-06-Audit-Remediation-R8-Report.md) e [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md).
- Limites: nenhum código/configuração/dependência foi corrigido; nenhum runtime operacional, provider/banco real, R2-B, O1, `OBSERVER`, LLM, recomendação, comando, automação, push, deploy ou lifecycle foi executado.
- Próxima decisão: Human Gate separado poderá aceitar ou rejeitar somente o R8 com suas limitações; nenhuma decisão está pré-preenchida.
- Aprovador: resultado automático local; decisão humana R8 pendente.

## 2026-07-23 — Evidência de cleanup do R8 corrigida e revalidada

- Estado anterior: `STATE-06 INTEGRATION`, R8 automaticamente aprovado no commit `d1fed5a0e927c97f8dfa9f2edd024ad9f1a85aad`, com Human Gate pendente.
- Incidente: Bruno reportou uma aba visível genérica do Windows Terminal intitulada `C:\WINDOWS\system32\ping.exe`, aberta durante o teste sintético preexistente de encerramento da árvore `ping.exe 127.0.0.1 -t`. Não havia processo `ping.exe`, mas a aba residual não foi detectada pela primeira auditoria baseada em caminho/comando de processo.
- Correção factual: a declaração inicial de cleanup integral foi reconhecida como prematura. Bruno fechou somente a aba e autorizou exclusivamente a correção documental e a revalidação, sem código.
- Revalidação: zero `ping.exe`, zero janela correspondente, zero processo/listener DB-Notifier e zero temporário correspondente; preferência preservada no SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`.
- Estado resultante: sem transição; R8 permanece automaticamente `APROVADO` somente depois desta prova corrigida. Human Gate R8 continua `PENDENTE`.
- Limites: nenhum código, teste, configuração, runtime operacional, acesso externo, push, deploy, O1, `OBSERVER`, AIOps operacional ou lifecycle foi alterado ou autorizado.
- Próxima decisão: Human Gate separado do R8, baseado no relatório corrigido e nas limitações já registradas.
- Aprovador: revalidação automática local; decisão humana R8 pendente.

## 2026-07-23 — Human Gate R8 aprovado com ressalvas

- Estado anterior: `STATE-06 INTEGRATION`, R8 automaticamente aprovado depois da correção documental e revalidação de cleanup do commit `cd5534188897f18504e9411349793e96636b1a61`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` mantido. A remediação da auditoria está humanamente encerrada com ressalvas.
- Decisão: Bruno declarou exatamente `HUMAN GATE DO R8: APROVADO COM RESSALVAS — aceito as limitações e contenções registradas no relatório R8. Esta decisão encerra somente a remediação e não autoriza O1, OBSERVER, AIOps operacional ou transição de lifecycle.`
- Escopo aceito: 35 achados `ENCERRADO`, quatro `CONTIDO`, nenhum `ABERTO` ou `BLOQUEADO`, com a prova corrigida de cleanup.
- Ressalvas preservadas: incidente NuGet R5; locked restore, advisory online, CI remota e laboratório PostgreSQL atual não repetidos; cinco condições físicas R6 `NÃO TESTADAS`; contenções futuras obrigatórias.
- Evidência: [relatório automático R8](../../docs/STATE-06-Audit-Remediation-R8-Report.md) e [Human Gate R8](../../docs/STATE-06-Audit-Remediation-R8-Human-Gate-Report.md).
- Limites: nenhum código, runtime, push, deploy, R2-B, O1, `OBSERVER`, AIOps operacional ou lifecycle foi autorizado.
- Próxima decisão: qualquer O1, ativação `OBSERVER`, AIOps incremental ou transição exige proposta e autorização separadas.
- Aprovador: Bruno, 2026-07-23, exclusivamente para o Human Gate R8.

## 2026-07-23 — O1 sandbox concluído automaticamente

- Estado anterior: `STATE-06 INTEGRATION`, R8 humanamente encerrado com ressalvas, R7-A0 aceito e O1 autorizado exclusivamente sobre `c38c494413d16ce1090d31f30d7b2efa87c589dd`.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O1 está automaticamente `APROVADO`; Human Gate O1 permanece `PENDENTE`.
- Implementação: marker test-only exato no host sandbox existente; coordinator de confiança; bundle, delegações, manifest e approvals assinados por papéis/chaves sintéticos distintos; nonce one-use; store atómica de checkpoint/heads/audit intent; crash/restart; quarentena de rollback/gap/divergência; recuperação separadamente rooted; recursos finitos; serialização, control lane, quiescência e fencing multiprocess.
- Rastreabilidade: catálogo executável com `24` vetores `TR`, `36` `RE` e oito `CO`, cada um ligado a componente, grupo de teste e owner, sem converter claims físicos/fleet/fairness futuros em aprovação.
- Gates: O1 `10/10`; arquitetura O1 `3/3`; arquitetura completa `55/55`; regressão proporcional `494/494`; build Release com zero avisos/erros; coverage `81,98%` linhas/`53,85%` branches; format aprovado.
- Limite de regressão: o teste preexistente de árvore sintética de readiness foi excluído desta execução para não repetir a aba visível do Windows Terminal registrada no R8; ele não pertence ao O1 e nenhuma conclusão O1 depende dele.
- Evidência: [relatório O1](../../docs/STATE-06-MOD-12-O1-Durable-Trust-And-Resource-Admission-Report.md) e [plano de remediação](../../docs/STATE-06-Complete-Project-Audit-Remediation-Plan.md).
- Limites: zero referência na composição normal; nenhum restore/download/dependência, acesso externo, dado/corpus/provider/banco/credencial real, LLM, recomendação, comando, automação, push, deploy, `OBSERVER` ou lifecycle.
- Próxima decisão: Human Gate O1 separado poderá aceitar ou rejeitar somente este sandbox. Qualquer `none → OBSERVER` continua sendo decisão posterior e independente.
- Aprovador: resultado automático local; decisão humana O1 pendente.

## 2026-07-23 — Human Gate O1 aprovado

- Estado anterior: `STATE-06 INTEGRATION`, O1 automaticamente aprovado no commit `ab60f4375af8e466a72077ecfa771ce5a7d07477`, com Human Gate próprio pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O1 está humanamente aceito somente como sandbox test-only opt-in.
- Decisão: Bruno declarou exatamente `HUMAN GATE DO O1: APROVADO`.
- Escopo aceito: continuidade de confiança durável, dual control one-use, recuperação/quarentena fail-closed, admissão bounded de recursos, fencing e rastreabilidade sintética conforme o relatório automático O1.
- Evidência: [relatório automático O1](../../docs/STATE-06-MOD-12-O1-Durable-Trust-And-Resource-Admission-Report.md) e [Human Gate O1](../../docs/STATE-06-MOD-12-O1-Human-Gate-Report.md).
- Atividade documental: baseline `ab60f4375af8e466a72077ecfa771ce5a7d07477`, worktree limpa e shutdown preflight com zero processo ou janela DB-Notifier; nenhum código, teste, runtime, push, deploy ou transição.
- Limites: a aceitação não autoriza `none → OBSERVER`, composição normal, dado/corpus/provider/banco/credencial real, LLM, recomendação, comando, automação, execução operacional, promoção ou lifecycle.
- Próxima decisão: qualquer preparação, verificação ou execução de `none → OBSERVER` exige proposta, autorização, Quality Gate e Human Gate posteriores e separados.
- Aprovador: Bruno, 2026-07-23, exclusivamente para o Human Gate O1.

## 2026-07-23 — Pacote de prontidão `NONE → OBSERVER` elaborado

- Estado anterior: `STATE-06 INTEGRATION`, O1 automática e humanamente aprovado somente como sandbox test-only, `ActivationState=None`.
- Autoridade: elaboração documental local, sem código, configuração, runtime, teste, acesso externo, ativação ou transição.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos.
- Resultado: a fundação determinística e O1 estão prontos para sustentar a proposta do próximo sandbox, mas a ativação `NONE → OBSERVER` permanece `BLOQUEADA`.
- Lacunas principais: pipeline canônico Agent → Server → MOD-12 sob um único contexto, ownership/composição normal, opt-in/kill switch/rollback, corpus representativo, calibração, limites empíricos `HM-01`–`HM-03`, observabilidade e projeção read-only.
- Não testado: nenhuma evidência foi reexecutada; os resultados citados são evidências históricas aceitas.
- Evidência: [pacote de prontidão](../../docs/STATE-06-MOD-12-None-To-Observer-Readiness-Package.md).
- Próximo lote proposto: `O2-A — Canonical Read-Only Observation Pipeline Sandbox`, ainda sem autorização de implementação.
- Limites: nenhum dado/provider/banco/corpus real, LLM, recomendação, comando, automação, push, deploy, Quality Gate, Human Gate, `OBSERVER` ou lifecycle foi executado ou autorizado.
- Aprovador: resultado documental local; decisão sobre O2-A pendente.

## 2026-07-23 — Pacote de prontidão `NONE → OBSERVER` aceito como direção

- Estado anterior: `STATE-06 INTEGRATION`, pacote documental concluído, `ActivationState=None` e O2-A não autorizado.
- Decisão: Bruno declarou exatamente `ACEITO O PACOTE DE PRONTIDÃO COMO DIREÇÃO, SEM AUTORIZAR O2-A, OBSERVER OU TRANSIÇÃO.`
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos.
- Escopo aceito: matriz de prontidão, lacunas, composição futura, critérios objetivos de ativação, plano O2–O5 e O2-A como próximo lote proposto.
- Limites: a aceitação não é Quality Gate, Human Gate de ativação ou autorização de código, runtime, O2-A, `OBSERVER`, push, deploy ou lifecycle.
- Evidência: [pacote de prontidão](../../docs/STATE-06-MOD-12-None-To-Observer-Readiness-Package.md).
- Próxima decisão: qualquer implementação do O2-A exige proposta e autorização posteriores e separadas.
- Aprovador: Bruno, 2026-07-23, exclusivamente como direção documental.

## 2026-07-23 — O2-A canonical read-only observation pipeline sandbox concluído automaticamente

- Estado anterior: `STATE-06 INTEGRATION`, O1 automática e humanamente aprovado somente como sandbox test-only, pacote `NONE → OBSERVER` aceito como direção e `ActivationState=None`.
- Autoridade: implementação local exclusiva do O2-A sobre `6cfe42facb83819447c216757cdf1d73bfc180de`, somente sob marker test-only e sem composição normal, dado real, acesso externo, UI, ativação ou transição.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O2-A está automaticamente `APROVADO`; Human Gate O2-A permanece `PENDENTE`.
- Implementação: envelope canônico provider-neutral versionado; Agent outbox sintético; fronteiras de produção `AgentOutboxDispatchRunner` e `ObservationBatchIngestor`; O1 trust/resource; adapter canônico; análise MOD-12 pura; publicação somente completa e não autorizadora.
- Matriz adversarial: contrato, idempotência, duplicidade, replay, reorder, gap, freshness, future skew, erro não normalizado, revogação assinada, context supersession, deadline, cancellation, limites, quiescência e fencing falharam fechados.
- Gates: O2-A `6/6`; arquitetura O2-A `4/4`; integração `38/38`; arquitetura `59/59`; MOD-12 `77/77`; unitários proporcionais `397/397`; WPF `10/10`; build Release sem avisos/erros; format, documentação, links, secrets, diff e processo exato aprovados.
- Nota de regressão: uma orquestração paralela encontrou `UnauthorizedAccessException` transitório no teste legado de migração; o grupo exato passou `4/4` e a suite unitária isolada passou `397/397`. Nenhum código fora do O2-A foi alterado.
- Evidência: [relatório automático O2-A](../../docs/STATE-06-MOD-12-O2A-Canonical-Read-Only-Observation-Pipeline-Sandbox-Report.md).
- Limites: store O2-A/outbox in-memory, O1 durável somente em raiz temporária; sem crash/restart O2-B, corpus representativo, calibração, observabilidade operacional, composição normal, kill switch, rollback, UI, LLM, recomendação, comando, automação, push, deploy, `OBSERVER` ou lifecycle.
- Próxima decisão: Human Gate O2-A separado poderá aceitar, aceitar com ressalvas ou rejeitar somente este sandbox. O2-B e qualquer ativação continuam sem autorização.
- Aprovador: resultado automático local; decisão humana O2-A pendente.

## 2026-07-23 — Human Gate O2-A aprovado

- Estado anterior: `STATE-06 INTEGRATION`, O2-A automaticamente aprovado no commit `eec5511b15208e7aadb569b9c8a82db0200fdc74`, com Human Gate próprio pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O2-A está humanamente aceito somente como sandbox test-only opt-in.
- Decisão: Bruno declarou exatamente `HUMAN GATE DO O2-A: APROVADO`.
- Escopo aceito: envelope canônico provider-neutral, cadeia sintética Agent → Server → O1 → MOD-12, controles fail-closed de contrato/identidade/sequência/freshness/revogação/contexto/recursos e publicação somente completa e não autorizadora, conforme o relatório automático O2-A.
- Evidência: [relatório automático O2-A](../../docs/STATE-06-MOD-12-O2A-Canonical-Read-Only-Observation-Pipeline-Sandbox-Report.md) e [Human Gate O2-A](../../docs/STATE-06-MOD-12-O2A-Human-Gate-Report.md).
- Atividade documental: baseline `eec5511b15208e7aadb569b9c8a82db0200fdc74`, worktree limpa e shutdown preflight com zero processo, listener ou janela DB-Notifier; nenhum código, teste, runtime de produto, push, deploy ou transição.
- Limites: a aceitação não autoriza O2-B, composição normal, dado/corpus/provider/banco/credencial real, UI, LLM, recomendação, comando, automação, `OBSERVER`, execução operacional, promoção ou lifecycle.
- Próxima decisão: qualquer proposta ou implementação O2-B e qualquer ativação `None → Observer` exigem autorizações e gates posteriores e separados.
- Aprovador: Bruno, 2026-07-23, exclusivamente para o Human Gate O2-A.

## 2026-07-23 — O2-B durable pipeline continuity, backpressure and observability sandbox concluído automaticamente

- Estado anterior: `STATE-06 INTEGRATION`, O2-A automática e humanamente aprovado somente como sandbox test-only e `ActivationState=None`.
- Autoridade: implementação local exclusiva do O2-B sobre `2408093ff2a66d05e4f00f523cf4b3b35be71ac4`, somente sob marker test-only e sem composição normal, dado real, acesso externo, UI, ativação ou transição.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O2-B está automaticamente `APROVADO`; Human Gate O2-B permanece `PENDENTE`.
- Implementação: ledger temporário autenticado com commit old-or-new, witness monotônico e quarantine; sequência/idempotência/gap/contexto/outcome/publicação duráveis; retomada pós-crash; fence por sessão; backpressure serial sem fila; retenção bounded e observabilidade sanitizada por códigos e contadores.
- Matriz adversarial: crash antes/depois do replace, interrupção após admissão, replay pós-restart, reorder/gap, missing/corrupt/rollback, fence antigo, supersession de contexto, saturação, concurrency busy, deadline, cancellation, quiescência e compaction falharam fechados sem duplicar publicação.
- Gates: O2-B `15/15`; arquitetura O2-B `4/4`; regressão O1/O2-A `16/16`; integração `53/53`; arquitetura `63/63`; unitários/coverage `399/399`; cobertura `81,98%/53,85%`; build Release sem avisos/erros; format, documentação, links e secrets aprovados; processo separado executado duas vezes sobre o mesmo ledger com `totalPublications=1` e `ActivationState=None`.
- Evidência: [relatório automático O2-B](../../docs/STATE-06-MOD-12-O2B-Durable-Pipeline-Continuity-Report.md).
- Limites: sandbox sintético e store temporária; witness apenas local; sem corpus representativo, calibração, valores operacionais, fairness fleet-wide, composição normal, kill switch, UI, LLM, recomendação, comando, automação, push, deploy, `OBSERVER` ou lifecycle.
- Próxima decisão: Human Gate O2-B separado poderá aceitar, aceitar com ressalvas ou rejeitar somente este sandbox. O3 e qualquer ativação continuam sem autorização.
- Aprovador: resultado automático local; decisão humana O2-B pendente.

## 2026-07-23 — Human Gate O2-B aprovado

- Estado anterior: `STATE-06 INTEGRATION`, O2-B automaticamente aprovado no commit `9b25d2dbf5bff6d9ef2f6a964806bd8f82a5e98a`, com Human Gate próprio pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O2-B está humanamente aceito somente como sandbox test-only opt-in.
- Decisão: Bruno declarou exatamente `HUMAN GATE DO O2-B: APROVADO`.
- Escopo aceito: continuidade autenticada e atômica, restart/replay sem duplicar publicação, quarantine de continuidade, fencing de sessão, backpressure e retenção bounded, quiescência e observabilidade sanitizada, conforme o relatório automático O2-B.
- Evidência: [relatório automático O2-B](../../docs/STATE-06-MOD-12-O2B-Durable-Pipeline-Continuity-Report.md) e [Human Gate O2-B](../../docs/STATE-06-MOD-12-O2B-Human-Gate-Report.md).
- Atividade documental: baseline `9b25d2dbf5bff6d9ef2f6a964806bd8f82a5e98a`, worktree limpa e shutdown preflight com zero processo, listener ou janela DB-Notifier; nenhum código, teste, runtime de produto, push, deploy ou transição.
- Limites: a aceitação não autoriza O3, composição normal, dado/corpus/provider/banco/credencial real, UI, LLM, recomendação, comando, automação, `OBSERVER`, execução operacional, promoção ou lifecycle.
- Próxima decisão: qualquer proposta ou implementação O3-A e qualquer ativação `None → Observer` exigem autorizações e gates posteriores e separados.
- Aprovador: Bruno, 2026-07-23, exclusivamente para o Human Gate O2-B.

## 2026-07-23 — O3-A governed synthetic corpus sandbox concluído automaticamente

- Estado anterior: `STATE-06 INTEGRATION`, O2-B automática e humanamente aprovado somente como sandbox test-only e `ActivationState=None`.
- Autoridade: implementação local exclusiva do O3-A sobre `4ca1d42ee03a1c36d8e2a85ef68062902794fcb9`, somente sob marker test-only, com corpus exclusivamente sintético e sem composição normal, dado real, treino, UI, ativação ou transição.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O3-A está automaticamente `APROVADO`; Human Gate O3-A permanece `PENDENTE`.
- Implementação: contrato provider-neutral, manifest autenticado e content-addressed, três papéis/chaves sintéticas distintos, dupla aprovação, proveniência/classificação/finalidade/retenção/retirada, membership exata, partições imutáveis de desenvolvimento/calibração/holdout e critérios quantitativos prévios para três segmentos.
- Matriz adversarial: corrupção, substituição, duplicidade, leakage entre partições, missingness, conflito, poisoning, critérios incompletos, papel/assinatura inválidos, expiração, alegação de representatividade de produção, rollback, gap, divergência, reativação após retirada e 96 mutações determinísticas falharam fechados.
- Gates: O3-A `9/9`; arquitetura O3-A `4/4`; integração `62/62`; arquitetura `67/67`; unitários/coverage `399/399`; cobertura `81,98%/53,85%`; build Release sem avisos/erros; processo separado reportou nove casos, três partições, `productionRepresentative=false` e `ActivationState=None`.
- Evidência: [relatório automático O3-A](../../docs/STATE-06-MOD-12-O3A-Governed-Synthetic-Corpus-Sandbox-Report.md).
- Limites: as nove fixtures equilibradas não são representativas de produção; sem corpus real, dados pessoais, provider/version/topology real, calibração operacional, treinamento, composição normal, UI, LLM, recomendação, comando, automação, push, deploy, `OBSERVER` ou lifecycle.
- Próxima decisão: Human Gate O3-A separado poderá aceitar, aceitar com ressalvas ou rejeitar somente este sandbox sintético. O3-B, qualquer corpus real e qualquer ativação continuam sem autorização.
- Aprovador: resultado automático local; decisão humana O3-A pendente.

## 2026-07-24 — Human Gate O3-A aprovado

- Estado anterior: `STATE-06 INTEGRATION`, O3-A automaticamente aprovado no commit `2832153565a3a8a1f5bb1748dfb9cda11cd1745c`, com Human Gate próprio pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O3-A está humanamente aceito somente como sandbox test-only opt-in e corpus sintético.
- Decisão: Bruno declarou exatamente `HUMAN GATE DO O3-A: APROVADO`.
- Escopo aceito: autoridade/proveniência/finalidade/retenção/retirada, manifest autenticado e content-addressed, membership exata, partições imutáveis, critérios quantitativos prévios e recusas fail-closed do corpus sintético, conforme o relatório automático O3-A.
- Evidência: [relatório automático O3-A](../../docs/STATE-06-MOD-12-O3A-Governed-Synthetic-Corpus-Sandbox-Report.md) e [Human Gate O3-A](../../docs/STATE-06-MOD-12-O3A-Human-Gate-Report.md).
- Atividade documental: baseline `2832153565a3a8a1f5bb1748dfb9cda11cd1745c`, worktree limpa e shutdown preflight com zero processo ou listener DB-Notifier; nenhum código, teste, runtime de produto, push, deploy ou transição.
- Limites: a aceitação não autoriza O3-B, corpus/telemetria/provider/banco/credencial real, dados pessoais, treinamento, UI, LLM, recomendação, comando, automação, `OBSERVER`, execução operacional, promoção ou lifecycle; `ProductionRepresentative=false` permanece obrigatório.
- Próxima decisão: qualquer proposta ou implementação O3-B e qualquer ativação `None → Observer` exigem autorizações e gates posteriores e separados.
- Aprovador: Bruno, 2026-07-24, exclusivamente para o Human Gate O3-A.

## 2026-07-24 — O3-B governed offline calibration and holdout sandbox concluído automaticamente

- Estado anterior: `STATE-06 INTEGRATION`, O3-A automática e humanamente aprovado somente como corpus sandbox sintético e `ActivationState=None`.
- Autoridade: implementação local exclusiva do O3-B sobre `89f290cc950ed0385ee004c57ff9d0a3c07a8f85`, somente sob marker test-only, usando apenas o corpus sintético O3-A e sem composição normal, dado real, treino, UI, ativação ou transição.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O3-B está automaticamente `APROVADO`; Human Gate O3-B permanece `PENDENTE`.
- Implementação: partition gate que exclui holdout da calibração; política bounded por segmento, dual-approved, attested, content-addressed e congelada; holdout one-use; binding exato corpus/política; métricas completas e autenticadas; continuidade contra reuse e rollback; publicação all-or-nothing e não autorizadora.
- Medição sintética: três segmentos com cobertura `1,00`, abstention `0,00`, estabilidade `1,00` e explicabilidade `1,00`; capacidade e latência sem falso positivo/negativo; disponibilidade com um falso positivo e erro binário `1,00`, preservado como limitação factual. Essa aprovação prova o protocolo e não qualidade de produção.
- Matriz adversarial: leakage, retirada/expiração, revisão/membership divergente, política alterada, papel incorreto, holdout pré-freeze/repetido, rollback, métrica incompleta/adulterada/não finita, segmento ausente, deadline, cancellation, caso/trabalho/memória saturados e 96 mutações determinísticas falharam fechados.
- Gates: O3-B `10/10`; integração `72/72`; arquitetura O3-B `4/4`; arquitetura completa `71/71`; MOD-12 proporcional `77/77`; solução Release sem avisos/erros; cobertura focal do source test-only `93,91%` (`540/575`); format, documentação, links, secrets, diff e cleanup aprovados; processo exato reportou três segmentos, aprovação sintética, não representatividade, não autoridade e `ActivationState=None`.
- Evidência: [relatório automático O3-B](../../docs/STATE-06-MOD-12-O3B-Governed-Offline-Calibration-And-Holdout-Report.md).
- Limites: corpus de nove fixtures e um holdout por segmento; sem prova de prevalência, provider/version/topology, previsão OLS futura ou qualidade operacional; sem corpus/telemetria real, dados pessoais, treino, composição normal, UI, LLM, recomendação, comando, automação, push, deploy, `OBSERVER` ou lifecycle.
- Próxima decisão: Human Gate O3-B separado poderá aceitar, aceitar com ressalvas ou rejeitar somente este sandbox. Qualquer corpus real e qualquer passo posterior continuam sem autorização.
- Aprovador: resultado automático local; decisão humana O3-B pendente.

## 2026-07-24 — Human Gate O3-B aprovado

- Estado anterior: `STATE-06 INTEGRATION`, O3-B automaticamente aprovado no commit `1736571fe56c5723e0514b8e86450c89b4f53ca4`, com Human Gate próprio pendente.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O3-B está humanamente aceito somente como sandbox test-only opt-in e avaliação de corpus sintético.
- Decisão: Bruno declarou exatamente `HUMAN GATE DO O3-B: APROVADO`.
- Escopo aceito: separação desenvolvimento/calibração/holdout, política determinística autenticada e congelada, holdout one-use, métricas segmentadas completas, resultado sintético não autorizador e recusas fail-closed, conforme o relatório automático O3-B.
- Limitação preservada: o segmento sintético disponibilidade reteve um falso positivo e erro binário `1,00`; a aceitação não converte isso em precisão, forecast ou qualidade comprovada de produção.
- Evidência: [relatório automático O3-B](../../docs/STATE-06-MOD-12-O3B-Governed-Offline-Calibration-And-Holdout-Report.md) e [Human Gate O3-B](../../docs/STATE-06-MOD-12-O3B-Human-Gate-Report.md).
- Atividade documental: baseline `1736571fe56c5723e0514b8e86450c89b4f53ca4`, worktree limpa e shutdown preflight com zero processo ou listener DB-Notifier; nenhum código, teste, runtime de produto, push, deploy ou transição.
- Limites: a aceitação não autoriza O4, corpus real ou representativo, composição normal, dado/telemetria/provider/banco/credencial real, UI, LLM, recomendação, comando, automação, `OBSERVER`, execução operacional, promoção ou lifecycle.
- Próxima decisão: qualquer O4, trabalho com corpus representativo, preparação de ativação ou transição exige proposta e autorização posteriores e separadas.
- Aprovador: Bruno, 2026-07-24, exclusivamente para o Human Gate O3-B.

## 2026-07-24 — O4 factual read-only Observer projection, API and UI sandbox concluído automaticamente

- Estado anterior: `STATE-06 INTEGRATION`, O3-B automática e humanamente aprovado somente como sandbox sintético e `ActivationState=None`.
- Autoridade: implementação local exclusiva do O4 sobre `a7f70fe86e21050eba8b570b5ef0eb1184c91ff1`, somente sob marker test-only, usando resultados sintéticos O2/O3 e sem composição normal, dado real, WPF/Tray, recomendação, ação, ativação ou transição.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O4 está automaticamente `APROVADO`; amostra visível e Human Gate O4 permanecem `PENDENTES`.
- Implementação: projeção in-memory content-addressed com rastreabilidade O2→resultado O3→política→corpus; API GET autenticada em HTTPS loopback; Dashboard dedicado pt-BR/en-GB e Light/Dark; sinais, freshness, Unknown, evidências, incerteza e limitações factuais.
- Verdade preservada: o sinal sintético completo está `Stale`; nenhuma previsão completa existe no resultado O2 e a UI mostra `Unknown` em vez de inferir forecast da aprovação sintética O3.
- Matriz adversarial: incompletude, alteração, expiração, revogação, supersession, future skew, divergência de política/corpus, schema incompatível, texto hostil, número não finito, falta de autenticação, write e origem/marker impróprios falharam fechados.
- Gates: O4 integração `3/3`; arquitetura `3/3`; integração completa `75/75`; arquitetura completa `74/74`; unitários `399/399` após um timeout sintético preexistente não reproduzido; WPF `10/10`; Dashboard `72/72`; coverage `81,98%/53,85%`; build Release sem avisos/erros; browser `16/16`, acessibilidade, forced colours modelado, zero origem externa, normal build com zero referência O4 e cleanup aprovados.
- Evidência: [relatório automático O4](../../docs/STATE-06-MOD-12-O4-Factual-Observer-Projection-API-UI-Sandbox-Report.md).
- Limites: corpus/resultado somente sintéticos e não representativos; autenticação fixa somente de sandbox; acessibilidade física e amostra humana O4 não testadas; sem composição normal, telemetria/provider/banco/credencial real, WPF/Tray, LLM, recomendação, comando, automação, push, deploy, `OBSERVER` ou lifecycle.
- Próxima decisão: proposta e autorização separadas para amostra humana visível O4; somente depois, Human Gate O4 informado. O5 e qualquer transição continuam sem autorização.
- Aprovador: resultado automático local; decisão humana O4 pendente.

## 2026-07-24 — Correção da aba residual do Windows Terminal no teste sintético PostgreSQL

- Estado anterior: o teste unitário de encerramento da árvore sintética executava `ping.exe 127.0.0.1 -t` por `Start-Process`; em execuções anteriores e novamente após O4, o terminal padrão do Windows reteve uma aba genérica com erro de pipe `0x800700e8`.
- Autoridade: pedido explícito de Bruno para corrigir exclusivamente a ocorrência recorrente.
- Correção: a fixture passou a criar o processo filho diretamente com `UseShellExecute=false`, `CreateNoWindow=true`, `WindowStyle=Hidden` e streams redirecionados; pai e filho devem expor handle de janela igual a zero.
- Gates: timeout e cancellation `2/2`; suíte unitária `399/399`; zero novo processo Windows Terminal; zero `ping.exe` residual; format, documentação, links, secrets, diff e cleanup aprovados.
- Limites: nenhuma alteração em provider, runtime normal, composição, dependência, O4, `ActivationState=None`, `OBSERVER` ou lifecycle.

## 2026-07-24 — O4-UI1 remediação visual e organizacional concluída automaticamente

- Estado anterior: O4 automaticamente aprovado somente como sandbox sintético, primeiras amostras humanas visíveis `REPROVADAS` por hierarquia, espaçamento, alinhamento e distribuição insuficientes; `ActivationState=None`.
- Autoridade: implementação local exclusiva do O4-UI1 sobre `67e83bff61921387dbbe606499a8569bb4d11dd6`, limitada à UI Web Observer test-only, sem API, contrato factual, dependência, dado real, WPF/Tray, ativação ou transição.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O4-UI1 está automaticamente `APROVADO`; repetição humana visível e Human Gate O4 permanecem `PENDENTES`.
- Implementação: resumo factual em três cartões, composição desktop equilibrada, empilhamento antecipado, cabeçalhos e ícones semânticos code-native, sinal com rótulo legível e chave técnica preservada, forecast Unknown explícito e rastreabilidade ordenada com hashes compactos e valores completos acessíveis.
- Verdade preservada: sinal `Stale`, forecast `Unknown`, conteúdo sintético/não operacional/não autorizador, API estritamente read-only, ausência de recomendação, comando, automação ou execução e zero referência no build normal.
- Gates: Dashboard `72/72`; O4 arquitetura `4/4`; arquitetura completa `75/75`; integração O4 `3/3`; solução Release sem avisos/erros; coverage `81,98%/53,85%`; browser `28/28`, forced colours modelado, acessibilidade, zero origem externa, zero referência O4 no build normal e cleanup aprovados.
- Evidência: [relatório automático O4-UI1](../../docs/STATE-06-MOD-12-O4-UI1-Visual-Organisation-Remediation-Report.md).
- Limites: a reprovação humana anterior permanece histórica; nenhuma nova amostra humana foi autorizada ou executada. Sem dado/corpus/provider/banco/credencial real, composição normal, WPF/Tray, LLM, recomendação, comando, automação, push, deploy, `OBSERVER`, O5 ou lifecycle.
- Próxima decisão: proposta e autorização separadas para repetir as amostras humanas visíveis do O4 após O4-UI1; somente depois poderá existir Human Gate O4 informado.
- Aprovador: resultado automático local; decisão humana O4 pendente.

## 2026-07-24 — Amostras humanas O4 após O4-UI1 aprovadas

- Estado anterior: O4-UI1 automaticamente aprovado, primeiras amostras O4 historicamente reprovadas e repetição remediada pendente; `ActivationState=None`.
- Autoridade: repetição local e visível exclusiva sobre `0e740671c2b92220ccd6b9de8899ff99dcc953a9`, sem implementação, correção, dado real, O5, ativação ou transição.
- Inspeção: Bruno respondeu exatamente `INSPEÇÃO O4 APÓS O4-UI1 CONCLUÍDA: Sem observações` contra o checklist pt-BR/en-GB, Light/Dark, desktop/compacto, teclado, foco, rastreabilidade e verdades factuais.
- Decisão: Bruno declarou exatamente `AMOSTRAS HUMANAS O4 APÓS O4-UI1 — APROVADAS`.
- Evidência: [relatório da repetição humana O4-UI1](../../docs/STATE-06-MOD-12-O4-UI1-Human-Samples-Repetition-Report.md), com screenshot conversacional corroborante não copiado para o repositório.
- Cleanup: zero processo O4/Chrome dedicado, listener e root temporário; build normal restaurado com zero referência O4; baseline e worktree limpas antes do registro.
- Estado resultante: sem transição; as amostras remediadas O4 estão aprovadas, a reprovação original permanece histórica e o Human Gate O4 continua `PENDENTE`.
- Limites: sem composição normal, dado/corpus/provider/banco/credencial real, WPF/Tray, LLM, sugestão, recomendação, comando, automação, O5, push, deploy, `OBSERVER` ou lifecycle.
- Próxima decisão: proposta concisa e decisão separada do Human Gate O4; eventual aceitação de O4 não ativará `OBSERVER` nem autorizará O5.
- Aprovador: Bruno, 2026-07-24, somente para as amostras humanas O4 após O4-UI1.

## 2026-07-24 — Human Gate O4 aprovado com ressalvas

- Estado anterior: `STATE-06 INTEGRATION`, O4 e O4-UI1 automaticamente aprovados, repetição humana visível aprovada e Human Gate O4 pendente; `ActivationState=None`.
- Decisão: Bruno declarou exatamente `HUMAN GATE DO O4: APROVADO COM RESSALVAS — aceito o O4, o O4-UI1 e as amostras humanas aprovadas como evidência suficiente do sandbox Observer factual, sintético, read-only e não autorizador. Reconheço que o corpus não representa produção, a previsão permanece Unknown e acessibilidade/DPI físicos não foram comprovados. Esta decisão encerra somente o O4 e não autoriza O5, dados reais, ativação do OBSERVER, recomendações, comandos, automação, deploy ou transição de lifecycle.`
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O4 está humanamente encerrado somente no seu sandbox factual, sintético, read-only e não autorizador.
- Escopo aceito: projeção provider-neutral in-memory, rastreabilidade content-addressed, API HTTPS loopback autenticada e read-only, UI dedicada, sinal `Stale`, forecast `Unknown`, incerteza e limitações explícitas, O4-UI1 e amostras visíveis repetidas.
- Ressalvas: o corpus não representa produção; `Unknown` não comprova capacidade preditiva; acessibilidade e DPI físicos não foram demonstrados; suporte operacional, qualidade de produção e prontidão de ativação não são inferidos.
- Evidência: [relatório automático O4](../../docs/STATE-06-MOD-12-O4-Factual-Observer-Projection-API-UI-Sandbox-Report.md), [relatório O4-UI1](../../docs/STATE-06-MOD-12-O4-UI1-Visual-Organisation-Remediation-Report.md), [amostras humanas repetidas](../../docs/STATE-06-MOD-12-O4-UI1-Human-Samples-Repetition-Report.md) e [Human Gate O4](../../docs/STATE-06-MOD-12-O4-Human-Gate-Report.md).
- Atividade documental: baseline `ea849ad5589bfe7936410d0aa85ba4bccee16a68`; shutdown preflight removeu um perfil temporário dedicado residual sem processo associado e depois comprovou zero processo e zero root temporário DB-Notifier. Nenhum código, teste, runtime de produto, push, deploy ou transição.
- Limites: a aceitação não autoriza O5, dado/telemetria/corpus/provider/banco/credencial real, composição normal, LLM, sugestão, recomendação, comando, automação, `OBSERVER`, execução operacional, push, deploy, promoção ou lifecycle.
- Próxima decisão: qualquer O5, evidência representativa, Quality Gate de ativação, `None → Observer` ou transição exige proposta, autorização e gates posteriores e separados.
- Aprovador: Bruno, 2026-07-24, exclusivamente para o Human Gate O4.

## 2026-07-24 — O5 Quality Gate de ativação bloqueado

- Estado anterior: `STATE-06 INTEGRATION`, O1–O4 aceitos somente nos seus sandboxes e `ActivationState=None`.
- Autoridade: execução local exclusiva do O5 sobre `970a2785abd11e3c493d26d2b00507f0f3a29462`, sem implementação, correção, dado/provider real, acesso externo, ativação ou transição.
- Estado resultante: sem transição; `STATE-06 INTEGRATION` e `ActivationState=None` mantidos. O resultado automático O5 é `BLOQUEADO`.
- Evidência verde preservada: build Release sem avisos/erros; integração `75/75`; arquitetura `75/75`; unitários MOD-12 `36/36`; Dashboard `72/72`; auditor O4 `28/28`; isolamento do build normal e cleanup aprovados.
- Bloqueios: corpus apenas sintético e `ProductionRepresentative=false`; calibração sem validade operacional; `HM-01`–`HM-03` não testados; ausência de revisão de segurança da composição ativável; opt-in/kill switch/rollback operacionais não implementados ou ensaiados; SLOs, retenção, owners e resposta a incidentes não testados; nenhuma entrada de provider homologada.
- Escopo de provider: PostgreSQL permanece `Homologation=None` e suporte público `No`; nenhum provider, banco, credencial, telemetria ou corpus real foi utilizado.
- Evidência: [relatório automático O5](../../docs/STATE-06-MOD-12-O5-None-To-Observer-Quality-Gate-Report.md).
- Limites: nenhuma correção foi tentada; código e configuração permaneceram inalterados; sem LLM, recomendação, comando, automação, push, deploy, `OBSERVER` ou lifecycle.
- Próxima decisão: somente uma autorização separada para elaborar o plano de remediação das lacunas O5 é elegível. Human Gate O5 e transição `None → Observer` permanecem bloqueados.
- Aprovador: resultado automático local; Human Gate O5 não solicitado.

## 2026-07-24 — Plano de remediação das lacunas O5 elaborado

- Estado anterior: O5 automático `BLOQUEADO`, `STATE-06 INTEGRATION` e `ActivationState=None`.
- Autoridade: elaboração documental local exclusiva sobre `42541abd4546a01f83d21fe2382cd5a14cb7c94e`, sem implementação, teste, runtime, dado/provider real, acesso externo, ativação ou transição.
- Plano: dez lotes separados cobrem escopo/governança, control plane inativo, observabilidade/SLO/incidente, segurança, `HM-01`–`HM-03`, corpus representativo, calibração/holdout, homologação exata, rehearsal integrado e repetição do O5.
- Ordem de risco: primeiro definir uma única célula provider/version/platform/topology/signal; somente depois autorizar controles, medições, dados representativos e laboratório.
- PostgreSQL: recomendado apenas como candidato para avaliação por possuir o único backend slice implementado; permanece não escolhido, `Homologation=None` e suporte público `No`.
- Evidência: [plano de remediação O5](../../docs/STATE-06-MOD-12-O5-Remediation-Plan.md).
- Estado resultante: sem transição; nenhum lote O5-R1–O5-R10 autorizado, `ActivationState=None`, Human Gate O5 e `None → Observer` bloqueados.
- Próxima decisão: Bruno poderá aceitar, aceitar com ressalvas, solicitar ajustes ou rejeitar o plano. Somente depois uma autorização separada poderá liberar o O5-R1 documental.
- Aprovador: plano documental elaborado; decisão humana pendente.

## 2026-07-24 — Plano de remediação O5 aceito como direção

- Estado anterior: plano O5 concluído documentalmente, decisão humana pendente, O5 automático `BLOQUEADO` e `ActivationState=None`.
- Decisão: Bruno declarou exatamente `ACEITO O PLANO COMO DIREÇÃO, SEM IMPLEMENTAÇÃO`.
- Escopo aceito: ordem O5-R1–O5-R10, dependências, critérios mensuráveis, critérios de parada, riscos e proposta documental do primeiro lote.
- Estado resultante: sem transição; o plano está aceito somente como direção. Nenhum lote, código, configuração, runtime, dado/provider/banco real ou ativação foi autorizado.
- Evidência: [plano de remediação O5](../../docs/STATE-06-MOD-12-O5-Remediation-Plan.md).
- Limites: PostgreSQL permanece apenas candidato, não escolhido e não homologado; `ActivationState=None`, Human Gate O5 e `None → Observer` permanecem bloqueados.
- Próxima decisão: proposta concisa e autorização separada do O5-R1 documental.
- Aprovador: Bruno, 2026-07-24, exclusivamente para a direção documental.

## 2026-07-24 — O5-R1 escopo piloto e governança elaborados

- Estado anterior: plano O5 aceito como direção, O5 automático `BLOQUEADO`, O5-R1 ainda não executado e `ActivationState=None`.
- Autoridade: elaboração documental local exclusiva sobre `52697b8b3c48fc906e88dd8acfb0f6735ae4c37b`, sem código, configuração, teste, runtime, dado/provider/banco real, acesso externo, ativação ou transição.
- Célula candidata: `OBS-PILOT-PG16-LOCAL-001`, com artefacto PostgreSQL 16 Alpine documentado por digest, single-primary descartável loopback-only, Agent no host Windows 11 x64 e sinais read-only estreitos.
- Governança: fontes e campos permitidos/proibidos, retenção proposta, papéis separados, partições disjuntas, representatividade limitada à matriz controlada, labels e thresholds determinísticos definidos.
- Thresholds: mínimo `30/30/60` por célula Development/Calibration/Holdout; 100% de integridade/cobertura/labels; zero leakage, segredo, resultado parcial, FP ou FN no holdout; forecast permanece fora do escopo e `Unknown`.
- Estado resultante: O5-R1 `CONCLUÍDO COM DECISÕES PENDENTES`; célula não escolhida, owners materiais não nomeados, corpus/laboratório não autorizados, O5-R2 não autorizado e `ActivationState=None`.
- Evidência: [relatório O5-R1](../../docs/STATE-06-MOD-12-O5-R1-Pilot-Scope-And-Data-Governance-Report.md).
- Limites: PostgreSQL permanece `Homologation=None` e suporte público `No`; nenhum suporte, dado real, previsão, recomendação, comando, automação, `OBSERVER` ou lifecycle foi ativado.
- Próxima decisão: Human Gate O5-R1 deverá aceitar, aceitar com ressalvas, solicitar ajustes ou rejeitar a candidata e os thresholds, além de dispor sobre os owners pendentes.
- Aprovador: resultado documental local; Human Gate O5-R1 pendente.

## 2026-07-24 — Human Gate O5-R1 aprovado com ressalvas

- Estado anterior: O5-R1 concluído documentalmente com célula candidata, thresholds e owners pendentes; `ActivationState=None`.
- Decisão: Bruno declarou exatamente `HUMAN GATE O5-R1: APROVADO COM RESSALVAS` e aceitou `OBS-PILOT-PG16-LOCAL-001` somente como escopo candidato exclusivo do futuro laboratório.
- Escopo aceito: artefacto PostgreSQL documentado, single-primary local, loopback-only, Agent Windows, sinais read-only, governança, retenção, partições, thresholds e critérios de parada do O5-R1.
- Ressalvas: nenhuma homologação, suporte público ou representatividade de produção; owners materiais pendentes; versão semântica, TLS e credencial sintética ainda não comprovados; laboratório, corpus e runtime não autorizados.
- Estado resultante: O5-R1 fechado com ressalvas, sem transição; O5-R2 pode somente ser proposto. O5-R6 e O5-R8 continuam bloqueados e `ActivationState=None` permanece.
- Evidência: [relatório O5-R1](../../docs/STATE-06-MOD-12-O5-R1-Pilot-Scope-And-Data-Governance-Report.md) e [Human Gate O5-R1](../../docs/STATE-06-MOD-12-O5-R1-Human-Gate-Report.md).
- Limites: nenhum código, configuração, teste, runtime, corpus, provider/banco real, push, deploy, `OBSERVER` ou lifecycle foi autorizado ou executado.
- Próxima decisão: proposta concisa de autorização do O5-R2, ainda sem implementação.
- Aprovador: Bruno, 2026-07-24, exclusivamente para o Human Gate O5-R1.

## 2026-07-24 — O5-R2 control plane inativo implementado

- Estado anterior: O5-R1 humanamente aprovado com ressalvas, O5 automático `BLOQUEADO`, O5-R2 não implementado e `ActivationState=None`.
- Autoridade: implementação local exclusiva do O5-R2 sobre `601bf2d1b4352f1df83d959f653b11357ac9018b`, sem restore, download, dado/provider/banco/corpus real, acesso externo, UI, recomendação, comando, automação, ativação ou transição.
- Composição normal: somente `DormantObserverControlPlane` e `UnavailableObserverActivationAuthority`; zero store, key, evaluator, pipeline, corpus, publisher, hosted service, trabalho ou caminho `None → Observer`.
- Sandbox: marker exato `DBNOTIFIER_O5_R2_TEST_ONLY`, approval dual autenticado, one-use, bounded e scope-bound; checkpoint/witness autenticados, crash-consistent, com kill switch, cancellation, fencing, quarantine, recovery e rollback para `None`.
- Evidência: [relatório O5-R2](../../docs/STATE-06-MOD-12-O5-R2-Inactive-Control-Plane-Report.md); `2/2` testes de composição, `8/8` O5-R2, `401/401` unitários, `83/83` integrações, `75/75` arquitetura, build Release sem aviso/erro, cobertura `82,01%` linhas/`53,92%` branches e auditoria fail-closed aprovados.
- Critérios: `100/100` admissões simuladas pós-kill recusadas; contextos atuais/obsoletos nunca publicaram; crash aceitou somente estado antigo ou novo completo; corrupção, rollback, gap e split view entraram em quarantine; cleanup temporário aprovado.
- Estado resultante: O5-R2 automático `APROVADO`, Human Gate O5-R2 `PENDENTE`, O5 geral ainda `BLOQUEADO`, `STATE-06 INTEGRATION` e `ActivationState=None` inalterados.
- Limites: PostgreSQL continua candidato não homologado; sem laboratório, Docker, provider/banco real, corpus, credencial, telemetria, LLM, recomendação, comando, automação, push, deploy, `OBSERVER` ou lifecycle.
- Próxima decisão: Human Gate O5-R2 separado; eventual aprovação permitirá somente propor O5-R3.
- Aprovador: resultado automático local; decisão humana O5-R2 pendente.

## 2026-07-24 — Human Gate O5-R2 aprovado

- Estado anterior: O5-R2 automático `APROVADO`, Human Gate O5-R2 `PENDENTE`, O5 geral
  `BLOQUEADO`, `STATE-06 INTEGRATION` e `ActivationState=None`.
- Decisão humana exata: `HUMAN GATE DO O5-R2: APROVADO`.
- Evidência revista: [relatório automático O5-R2](../../docs/STATE-06-MOD-12-O5-R2-Inactive-Control-Plane-Report.md)
  e implementação focal no commit `3fe55ed359916df8853dcec6d829a632a9b6f4dc`.
- Registro: [relatório do Human Gate O5-R2](../../docs/STATE-06-MOD-12-O5-R2-Human-Gate-Report.md).
- Escopo aceito: control plane provider-neutral dormente; autoridade normal indisponível; approvals
  sintéticos duais, autenticados, one-use e bounded; kill switch prioritário; cancellation,
  fencing, quarantine, recovery e rollback crash-consistent, todos limitados ao sandbox autorizado.
- Estado resultante: O5-R2 automática e humanamente `APROVADO`; O5 geral permanece `BLOQUEADO`;
  `STATE-06 INTEGRATION` e `ActivationState=None` permanecem inalterados.
- Limites: sem O5-R3, laboratório, corpus/provider/banco/credencial real, UI, LLM, recomendação,
  comando, automação, acesso externo, push, deploy, `OBSERVER` ou transição.
- Próxima decisão: somente uma proposta concisa de autorização do O5-R3, sem implementação.
- Aprovador: Bruno.

## 2026-07-24 — O5-R3 observabilidade, SLOs e resposta a incidentes concluído automaticamente

- Estado anterior: O5-R2 automática e humanamente `APROVADO`, O5 geral `BLOQUEADO`,
  O5-R3 não implementado e `ActivationState=None`.
- Autoridade: implementação local exclusiva do O5-R3 sobre
  `cdfdce9d3961b2ca64f4f009c2b822a13ced7f47`, sem restore, download, integração
  externa, alerta real, dado/provider/banco/corpus real, UI, recomendação, comando,
  automação, ativação ou transição.
- Sandbox: marker exato `DBNOTIFIER_O5_R3_TEST_ONLY`, catálogo
  `o5r3-observability-1.0.0`, quatro SLIs/SLOs numéricos, seis diagnósticos O5-R2
  allow-listed, seis alertas, ownership funcional, escalonamento e seis runbooks
  não executáveis.
- Exercícios: saturação, corrupção, Stale, split view, kill switch e rollback
  cumpriram detecção `250/1000 ms`, contenção `1000/3000 ms`, recuperação
  `3000/8000 ms` e encerramento `6000/15000 ms`.
- Contenções: payload canary, incidente/código desconhecido, SLO inválido ou
  excedido, owner/runbook ausente, timeline inválida, expiração, cancellation e
  capacidade falharam fechados; retenção e cardinalidade permaneceram bounded.
- Evidência: [relatório automático O5-R3](../../docs/STATE-06-MOD-12-O5-R3-Observability-SLO-Incident-Response-Report.md);
  O5-R3 `8/8`, arquitetura O5-R3 `4/4`, unitários `401/401`, integração `91/91`,
  arquitetura `79/79`, WPF `10/10`, build Release sem aviso/erro e cobertura
  `82,01%` linhas/`53,92%` branches.
- Estado resultante: O5-R3 automático `APROVADO`, Human Gate O5-R3 `PENDENTE`,
  O5 geral ainda `BLOQUEADO`, `STATE-06 INTEGRATION` e `ActivationState=None`
  inalterados.
- Limites: owners são papéis funcionais, não pessoas nomeadas; sem sink, paging,
  alerta operacional, persistência, PostgreSQL homologado, LLM, recomendação,
  comando, automação, push, deploy, `OBSERVER` ou lifecycle.
- Próxima decisão: Human Gate O5-R3 separado; eventual aprovação permitirá somente
  propor O5-R4.
- Aprovador: resultado automático local; decisão humana O5-R3 pendente.

## 2026-07-24 — Human Gate O5-R3 aprovado

- Estado anterior: O5-R3 automático `APROVADO`, Human Gate O5-R3 `PENDENTE`, O5
  geral `BLOQUEADO`, `STATE-06 INTEGRATION` e `ActivationState=None`.
- Decisão humana exata: `HUMAN GATE DO O5-R3: APROVADO`.
- Evidência revista: [relatório automático O5-R3](../../docs/STATE-06-MOD-12-O5-R3-Observability-SLO-Incident-Response-Report.md)
  e implementação focal no commit `a1e7ead768cd2f2c02e924cd615a7253bb1ec32c`.
- Registro: [relatório do Human Gate O5-R3](../../docs/STATE-06-MOD-12-O5-R3-Human-Gate-Report.md).
- Escopo aceito: catálogo sintético fechado e versionado, quatro SLIs/SLOs
  numéricos, seis diagnósticos/alertas, ownership funcional, escalonamento,
  runbooks não executáveis, exercícios adversariais e sanitização/cardinalidade
  bounded.
- Estado resultante: O5-R3 automática e humanamente `APROVADO`; O5 geral
  permanece `BLOQUEADO`; `STATE-06 INTEGRATION` e `ActivationState=None`
  permanecem inalterados.
- Limites: owners materiais continuam sem nomeação; sem sink, paging, alerta
  operacional, O5-R4, dado/provider/banco/corpus real, LLM, recomendação,
  comando, automação, acesso externo, push, deploy, `OBSERVER` ou transição.
- Próxima decisão: somente uma proposta concisa de autorização do O5-R4, sem
  implementação.
- Aprovador: Bruno.

## 2026-07-24 — O5-R4 threat model e revisão de segurança concluídos automaticamente

- Estado anterior: O5-R3 automática e humanamente `APROVADO`, O5 geral
  `BLOQUEADO`, O5-R4 não executado e `ActivationState=None`.
- Autoridade: execução local exclusiva do O5-R4 sobre
  `1f9e90874ae580e571a9d657dc8099f217ded7e0`, sem implementação, correção,
  dado/provider/banco/corpus/credencial real, acesso externo, recomendação,
  comando, automação, ativação ou transição.
- Modelo: oito assets, oito identidades/responsabilidades, dez trust boundaries
  e treze grupos adversariais cobrindo controle, telemetria, corpus, holdout,
  API/UI, observabilidade e a separação de recomendação/comando/execução.
- Achados: zero crítico, zero alto, zero boundary desconhecida e três médios de
  prontidão, todos com owner funcional, deadline e decisão fail-closed:
  accountability material, autoridade/custódia operacional e continuidade/
  reconciliação independente.
- Evidência: [relatório automático O5-R4](../../docs/STATE-06-MOD-12-O5-R4-Threat-Model-And-Security-Review-Report.md);
  unitários focados `38/38`, integrações O1–O5-R3 `69/69`, arquitetura
  `27/27`, browser/API O4 `28/28`, read/write indevidos recusados, zero origem
  externa e secret scan aprovado.
- Incidente de ferramenta: três builds paralelos disputaram arquivos de
  compilação antes de executar testes; build servers foram encerrados, um build
  único passou sem aviso/erro e as suítes foram repetidas sem build, todas
  verdes. Não foi achado de produto ou segurança.
- Estado resultante: O5-R4 automático `APROVADO`, Human Gate O5-R4 `PENDENTE`,
  O5 geral ainda `BLOQUEADO`, `STATE-06 INTEGRATION` e `ActivationState=None`
  inalterados.
- Limites: sem código/configuração/dependência alterado, penetration test
  operacional, identidade/chave real, sink/on-call real, HM-01–HM-03,
  PostgreSQL homologado, O5-R5, `OBSERVER`, push, deploy ou lifecycle.
- Próxima decisão: Human Gate O5-R4 separado; eventual aprovação permitirá
  somente propor O5-R5.
- Aprovador: resultado automático local; decisão humana O5-R4 pendente.

## 2026-07-24 — Human Gate O5-R4 aprovado

- Estado anterior: O5-R4 automático `APROVADO`, Human Gate O5-R4 `PENDENTE`, O5
  geral `BLOQUEADO`, `STATE-06 INTEGRATION` e `ActivationState=None`.
- Decisão humana exata: `HUMAN GATE DO O5-R4: APROVADO`.
- Evidência revista: [relatório automático O5-R4](../../docs/STATE-06-MOD-12-O5-R4-Threat-Model-And-Security-Review-Report.md)
  e revisão focal no commit `76d724371a14b44f1d038f7dba62d99bc505e672`.
- Registro: [relatório do Human Gate O5-R4](../../docs/STATE-06-MOD-12-O5-R4-Human-Gate-Report.md).
- Escopo aceito: threat model local sintético, boundaries conhecidos, matriz
  adversarial, zero crítico/alto e três lacunas médias com owner, prazo e decisão
  fail-closed.
- Estado resultante: O5-R4 automática e humanamente `APROVADO`; O5 geral
  permanece `BLOQUEADO`; `STATE-06 INTEGRATION` e `ActivationState=None`
  permanecem inalterados.
- Limites: as três lacunas médias permanecem abertas; sem O5-R5, identidade/chave
  real, penetration test operacional, dado/provider/banco/corpus real, LLM,
  recomendação, comando, automação, acesso externo, push, deploy, `OBSERVER` ou
  transição.
- Próxima decisão: somente uma proposta concisa de autorização do O5-R5, sem
  implementação ou execução.
- Aprovador: Bruno.

## 2026-07-24 — O5-R5 bloqueado antes da campanha física

- Estado anterior: O5-R4 automática e humanamente `APROVADO`, O5 geral
  `BLOQUEADO`, O5-R5 não executado e `ActivationState=None`.
- Autoridade: execução local exclusiva de `HM-01`, `HM-02` e `HM-03` sobre
  `505f098e710aceadf78548d20a0a139a2d211df4`, com carga sintética e ferramentas
  existentes, sem implementação, configuração, restore, download, dependência,
  dado/provider/banco real, acesso externo, ativação ou transição.
- Preflight: baseline exata, worktree limpo, zero processo e zero listener
  pertencente ao projeto.
- Parada: não existe runner O5-R5/HM sob `src/`, `tests/` ou `scripts/`; os SLOs
  O5-R3 não definem thresholds físicos nem headroom HM; e a cadeia local não
  possui profiler e analisador capazes de atribuir heap, allocation peak, CPU e
  elapsed às fases exigidas sem novo código ou instalação.
- Ferramentas: `dotnet-counters`, `dotnet-trace`, `dotnet-gcdump`, PerfView e
  Windows Performance Analyzer ausentes; WPR disponível, porém insuficiente sem
  analisador suportado e runner phase-labelled.
- Estado resultante: O5-R5 automático `BLOQUEADO`; `HM-01`, `HM-02` e `HM-03`
  permanecem `NÃO TESTADOS`; O5 geral continua `BLOQUEADO`; `STATE-06
  INTEGRATION` e `ActivationState=None` permanecem inalterados.
- Evidência: [relatório O5-R5](../../docs/STATE-06-MOD-12-O5-R5-Physical-Measurement-Campaign-Report.md).
- Limites: nenhuma carga, profiler, build ou teste foi iniciado depois da
  condição de parada; nenhum código, configuração, dependência ou lockfile
  mudou; nenhum número físico foi inferido.
- Próxima decisão: somente uma proposta concisa de O5-R5-A para pré-registrar
  thresholds/headroom e autorizar um runner e instrumentação test-only bounded.
  O5-R6, dados reais, `OBSERVER` e transição permanecem não autorizados.
- Aprovador: resultado automático local bloqueado; decisão humana O5-R5 não
  elegível antes da remediação.

## 2026-07-25 — O5-R5-A prontidão de medição concluída automaticamente

- Estado anterior: O5-R5 automático `BLOQUEADO`, `HM-01`–`HM-03` `NÃO
  TESTADOS`, O5 geral `BLOQUEADO` e `ActivationState=None`.
- Autoridade: implementação local exclusiva do O5-R5-A sobre
  `160cfdcdf63a425b1b16033fd557b90727b3de88`, sem campanha física, nova
  dependência, instalação, download, acesso externo, dado/provider/banco real,
  publicação, ativação ou transição.
- Protocolo: versão `o5r5a-physical-measurement-1.0.0`, SHA-256
  `266B7A952DF1A46BEE4577894D0A9206D92917AC661E0E17F9052DE1EB415DD7`,
  congelada antes dos testes com thresholds numéricos, headroom `3/2`, cinco
  warm-ups, trinta medições, P50/P95/P99/máximo, CV `0.20` e critérios de
  parada.
- Implementação: um runner serial no assembly de integração, protegido pelo
  marker exato `DBNOTIFIER_O5_R5_A_TEST_ONLY`, rotula first-byte, idle,
  cancellation, control-update, parse, cryptography, sort e analysis e retorna
  somente evidência completa, sanitizada, in-memory e não autorizadora.
- Evidência: `7/7` integrações focais, `6/6` arquitetura focal, `98/98`
  integrações completas, `85/85` arquitetura completa e `401/401` unitários
  passaram; build Release teve zero aviso/erro. N/N+1, maximum−1/maximum/
  maximum+1, cancellation, saturação, checkpoint 64/65, rollback, fonte
  indisponível, protocolo divergente e variância falharam fechados.
- Ferramenta: o gate de lockfiles invocou `dotnet restore --locked-mode`;
  nenhum download foi reportado, e dependências, lockfiles e estado do
  repositório permaneceram inalterados. Sete workers MSBuild órfãos desse gate
  foram identificados por caminho/linha de comando, encerrados por PID exato e
  a reaudição confirmou zero processo ou listener pertencente ao workspace.
- Estado resultante: O5-R5-A automático `APROVADO`; Human Gate O5-R5-A
  `PENDENTE`; O5-R5 original continua `BLOQUEADO`; `HM-01`–`HM-03` continuam
  `NÃO TESTADOS`; O5 geral, `STATE-06 INTEGRATION` e `ActivationState=None`
  permanecem inalterados.
- Limites: a fonte física foi compilada, mas não instanciada; nenhuma carga
  física, profiler, medição do host, publicação Observer, provider/banco/corpus
  real, O5-R6, push, deploy ou transição foi executada.
- Evidência: [relatório automático O5-R5-A](../../docs/STATE-06-MOD-12-O5-R5A-Measurement-Readiness-Report.md)
  e [protocolo congelado](../../docs/STATE-06-MOD-12-O5-R5A-Physical-Measurement-Protocol.md).
- Próxima decisão: Human Gate O5-R5-A separado; eventual aprovação permitirá
  somente propor uma autorização para repetir fisicamente o O5-R5.
- Aprovador: resultado automático local; decisão humana O5-R5-A pendente.

## 2026-07-25 — Human Gate O5-R5-A aprovado

- Estado anterior: O5-R5-A automático `APROVADO`, Human Gate `PENDENTE`,
  O5-R5 `BLOQUEADO`, `HM-01`–`HM-03` `NÃO TESTADOS` e
  `ActivationState=None`.
- Decisão humana exata: `HUMAN GATE DO O5-R5-A: APROVADO`.
- Evidência revista: [relatório automático O5-R5-A](../../docs/STATE-06-MOD-12-O5-R5A-Measurement-Readiness-Report.md),
  [protocolo congelado](../../docs/STATE-06-MOD-12-O5-R5A-Physical-Measurement-Protocol.md)
  e implementação no commit `45ade378ef8e5468a45e9507a77f25229327b29a`.
- Registro: [relatório Human Gate O5-R5-A](../../docs/STATE-06-MOD-12-O5-R5A-Human-Gate-Report.md).
- Estado resultante: O5-R5-A automática e humanamente `APROVADO`;
  `STATE-06 INTEGRATION` e `ActivationState=None` inalterados.
- Limites: aprovação de prontidão apenas; nenhuma medição física, headroom,
  provider, banco, corpus, publicação Observer, O5-R6 ou transição foi
  autorizada por esta decisão.
- Próxima ação: a repetição física O5-R5 foi autorizada separadamente sobre a
  mesma baseline, usando somente o protocolo e runner aprovados.
- Aprovador: Bruno.

## 2026-07-25 — Repetição física O5-R5 bloqueada antes da medição

- Estado anterior: O5-R5-A automática e humanamente `APROVADO`, repetição
  física O5-R5 autorizada, `HM-01`–`HM-03` `NÃO TESTADOS` e
  `ActivationState=None`.
- Autoridade: campanha física local usando somente protocolo e runner
  existentes, sem alteração de código/configuração, restore, download, acesso
  externo, provider/banco/corpus real, ativação ou transição.
- Preflight: source baseline autorizada
  `45ade378ef8e5468a45e9507a77f25229327b29a`; execução após o commit
  documental `5a63245`; worktree limpo, zero processo e zero listener do
  workspace.
- Condição de parada: existe uma definição de
  `O5R5DotNetMeasurementSource`, mas zero construção/instanciação, zero
  entrypoint físico e zero workload físico para as oito fases. Os sete testes
  descobertos são exclusivamente sintéticos.
- Estado resultante: repetição O5-R5 `BLOQUEADA`; `HM-01`, `HM-02` e `HM-03`
  permanecem `NÃO TESTADOS`; O5 geral, `STATE-06 INTEGRATION` e
  `ActivationState=None` inalterados.
- Limites: nenhuma fonte física, workload, contador, profiler, trace ou teste
  foi executado; nenhum código/configuração/dependência mudou.
- Evidência: [relatório da repetição O5-R5](../../docs/STATE-06-MOD-12-O5-R5-Physical-Measurement-Campaign-Repetition-Report.md).
- Próxima decisão: somente proposta concisa O5-R5-B para um driver físico
  marker-gated usando o runner existente; O5-R6 continua não autorizado.
- Aprovador: resultado automático local bloqueado; Human Gate O5-R5 não
  elegível.

## 2026-07-25 — O5-R5-B driver físico concluído automaticamente

- Estado anterior: O5-R5-A automática e humanamente `APROVADO`, repetição
  física O5-R5 `BLOQUEADA`, `HM-01`–`HM-03` `NÃO TESTADOS` e
  `ActivationState=None`.
- Autoridade: implementação local exclusiva do O5-R5-B sobre
  `92044fcbac86ccc67bc6495eeb63a27b7ff7154d`, seguida da campanha física
  somente após validação automática, sem restore, download, dependência,
  acesso externo, dado/provider/banco real, ativação ou transição.
- Implementação: entrypoint inerte no assembly de integração exige o digest
  exato, valida o destino temporário e somente depois constrói a única fonte
  física. O driver materializa oito fases, duas temperaturas, cinco warm-ups e
  trinta medições, totalizando `560` cenários seriais bounded.
- Gates: `11/11` integrações focais, `7/7` arquitetura focal, `102/102`
  integrações completas, `86/86` arquitetura completa e `401/401` unitários
  passaram. Build Release teve zero aviso/erro; format, documentação, links,
  tokens, localização, provider assets, secret scan e isolamento passaram.
- Estado resultante: O5-R5-B automático `APROVADO`; `HM-01`–`HM-03`
  permanecem `NÃO TESTADOS` até a campanha física autorizada nesta mesma
  atividade; O5 geral, `STATE-06 INTEGRATION` e `ActivationState=None`
  permanecem inalterados.
- Limites: produto, composição normal, dependências e lockfiles não mudaram;
  nenhuma medição física, publicação, provider, banco, corpus, recomendação,
  comando ou automação ocorreu neste gate.
- Evidência: [relatório automático O5-R5-B](../../docs/STATE-06-MOD-12-O5-R5B-Physical-Campaign-Driver-Report.md).
- Próxima ação: execução física O5-R5 já autorizada, com parada imediata e sem
  correção se qualquer limite ou requisito de reprodutibilidade falhar.
- Aprovador: resultado automático local; Human Gate O5-R5 ainda não elegível
  antes da campanha.

## 2026-07-25 — Campanha física O5-R5 pós-O5-R5-B reprovada

- Estado anterior: O5-R5-B automático `APROVADO`, `HM-01`–`HM-03` ainda
  `NÃO TESTADOS`, O5 geral `BLOQUEADO` e `ActivationState=None`.
- Autoridade: execução física local HM-01–HM-03 depois da validação automática,
  usando somente protocolo congelado, driver test-only e APIs .NET/Windows
  existentes, sem correção, restore, download, acesso externo ou dado real.
- Preflight: driver baseline
  `737dab6ae1667c41f41d09bc6954c827b0e07f57`; worktree limpo; zero processo
  e listener do projeto; .NET SDK `10.0.301`, runtime `10.0.10`, Windows
  `10.0.26200`, oito processadores lógicos e `16.963.534.848` bytes de memória.
- Parada: no sample medido `FirstByte/Cold` repetição 14, o pico adicional de
  working set foi `864.256 bytes`, acima do teto imutável de `786.432 bytes`
  por `77.824 bytes`; código `o5r5a.measurement.threshold_exceeded`.
- Estado resultante: campanha O5-R5 `REPROVADA`; HM-02 `REPROVADO`, HM-01
  `BLOQUEADO` sem matriz completa e HM-03 `NÃO TESTADO`; O5 geral,
  `STATE-06 INTEGRATION` e `ActivationState=None` inalterados.
- Integridade: 19/560 samples completos e 0/16 summaries; evidência temporária
  de 12.628 bytes com SHA-256
  `90F5A06CD58655D537CD53B2D14F03F3D174FD8306305F0246DD3C0BC3956AF7`;
  ambiente opt-in removido e root temporário eliminado após extração.
- Limites: nenhum threshold, protocolo, workload ou código foi alterado depois
  do resultado; nenhuma publicação, dado/provider/banco/corpus real,
  recomendação, comando, automação, O5-R6 ou transição ocorreu.
- Evidência: [relatório físico pós-O5-R5-B](../../docs/STATE-06-MOD-12-O5-R5-Physical-Measurement-Campaign-Post-R5B-Report.md).
- Próxima decisão: somente proposta separada para analisar/remediar o headroom
  físico; repetição, alteração do limite e O5-R6 permanecem não autorizados.
- Aprovador: resultado automático local reprovado; Human Gate O5-R5 não
  elegível antes de remediação.

## 2026-07-25 — PF-OBS-1 interrompida no gate físico de reprodutibilidade

- Estado anterior: O5-R5 físico `REPROVADO`, O5 geral `BLOQUEADO`,
  `STATE-06 INTEGRATION` e `ActivationState=None`.
- Autoridade: macrocampanha local PF-OBS-1 consolidando driver físico,
  laboratório PostgreSQL 16, pipeline Observer, corpus, resiliência e amostra
  humana, sem produção, ativação, push, deploy ou transição.
- Implementação preparada: collector PostgreSQL TLS/loopback, corpus de 36
  medições com partições disjuntas, política congelada antes do holdout,
  projeção O4 não autorizadora, processo físico dedicado e runner
  `Campaign`/`Start`/`Stop`.
- Gates aprovados: build Release sem avisos/erros, `17/17` integrações focais,
  `4/4` arquitetura focal, sintaxe PowerShell, documentação de 378 fontes e
  secret scan.
- Condição de parada: execuções físicas dedicadas passaram os limites
  absolutos por sample, mas lotes diferentes excederam o coeficiente máximo
  congelado de variação `0.20`; uma execução diagnóstica em prioridade alta
  também falhou.
- Estado resultante: PF-OBS-1 `BLOQUEADA` antes do laboratório vivo,
  calibração/holdout físicos, campanha de resiliência e amostra humana;
  `ActivationState=None` e `STATE-06 INTEGRATION` inalterados.
- Cleanup: zero container, rede, volume, processo, listener ou diretório
  temporário PF-OBS-1/O5-R5 residual.
- Evidência:
  [checkpoint PF-OBS-1](../../docs/STATE-06-MOD-12-PF-OBS-1-Checkpoint-Report.md).
- Próxima decisão: corrigir e congelar separadamente a metodologia física sem
  relaxar SLO absoluto nem selecionar apenas execuções favoráveis.
- Aprovador: resultado automático local bloqueado; Human Gate PF-OBS-1 não
  elegível.

## 2026-07-25 — metodologia física PF-OBS-1 v2 congelada antes da retomada

- Estado anterior: PF-OBS-1 `BLOQUEADA` pelo coeficiente de variação bruto;
  `STATE-06 INTEGRATION` e `ActivationState=None`.
- Autoridade: corrigir localmente a metodologia física sem relaxar SLO,
  limite de recurso ou segurança e retomar depois a macrocampanha.
- Decisão técnica congelada: manter todos os 30 resultados e todos os gates
  absolutos, mas calcular repetibilidade por cinco grupos sequenciais fixos de
  seis amostras e mediana de grupo, ainda com coeficiente máximo `0.20`.
- Reprodutibilidade: duas campanhas completas consecutivas são obrigatórias;
  falha ou incompletude para a execução, sem terceira tentativa substituta.
- Protocolo: `pfobs1-physical-measurement-2.0.0`, SHA-256
  `53F40F7DC72548EB488FFF729823BF0DCFD64EDD085314022C8E746CC45B5D71`.
- Gates prévios à medição: `10/10` integrações focais, `8/8` provas
  arquiteturais, sintaxe PowerShell, documentação, links e secret scan
  aprovados.
- Estado resultante: protocolo v2 congelado; nenhuma medição física v2
  executada neste checkpoint; `ActivationState=None` inalterado.
- Evidência:
  [protocolo físico PF-OBS-1 v2](../../docs/STATE-06-MOD-12-PF-OBS-1-Physical-Measurement-Protocol-v2.md).

## 2026-07-25 — retomada PF-OBS-1 v2 parada em gate absoluto

- Estado anterior: protocolo v2 congelado, PF-OBS-1 autorizada localmente,
  `STATE-06 INTEGRATION` e `ActivationState=None`.
- Execução: a primeira das duas campanhas físicas consecutivas iniciou após
  preflight limpo e build Release sem aviso ou erro.
- Condição de parada: `o5r5a.measurement.threshold_exceeded`.
- Integridade: nenhuma execução substituta ocorreu; a segunda campanha e
  todos os estágios posteriores não foram executados.
- Limitação de evidência: o cleanup removeu o relatório físico temporário
  antes da cópia, então fase e métrica exatas permanecem não identificadas.
- Estado resultante: PF-OBS-1 v2 `REPROVADA`; HM-01–HM-03 incompletos;
  laboratório, pipeline, corpus, holdout, resiliência e amostra humana não
  elegíveis.
- Cleanup: zero state file, processo, container, rede, volume ou root
  temporário project-owned.
- Evidência:
  [relatório de retomada PF-OBS-1 v2](../../docs/STATE-06-MOD-12-PF-OBS-1-v2-Resumption-Report.md).
- Aprovador: resultado automático local reprovado; Human Gate PF-OBS-1 não
  elegível.

## 2026-07-25 — PF-OBS-1-D1 retenção diagnóstica concluída

- Estado anterior: PF-OBS-1 v2 `REPROVADA`, relatório temporário removido
  antes da cópia, fase e métrica exatas indisponíveis e
  `ActivationState=None`.
- Autoridade: implementar retenção local fail-closed da evidência física
  aprovada ou reprovada, diagnosticar por campos allow-listed, testar
  corrupção/incompletude/escrita/cleanup e criar commit focado.
- Implementação: diagnósticos bounded de fase, amostra, métrica, valor,
  limite e unidade; validação estrutural; escrita write-through seguida de
  move atómico; arquivo project-owned retido antes da interpretação do exit
  code e do cleanup temporário.
- Gates: `13/13` integrações focais, `9/9` provas arquiteturais focais,
  `109/109` integrações completas e `88/88` provas arquiteturais completas
  passaram; build Release do host consolidado teve zero aviso/erro; sintaxe
  PowerShell, format, documentação, links, secret scan, digest v2, corrupção,
  incompletude, falha de escrita, round trip e isolamento passaram.
- Estado resultante: PF-OBS-1-D1 automático `APROVADO`; protocolo
  `pfobs1-physical-measurement-2.0.0` e limites inalterados;
  `ActivationState=None`.
- Evidência:
  [relatório PF-OBS-1-D1](../../docs/STATE-06-MOD-12-PF-OBS-1-D1-Evidence-Retention-Report.md).
- Próxima ação: uma única retomada integral já autorizada, sem execução
  substituta e com parada imediata diante de falha.
- Aprovador: resultado automático local; nenhum Human Gate ou transição
  inferido.

## 2026-07-25 — única retomada PF-OBS-1 pós-D1 reprovada

- Estado anterior: PF-OBS-1-D1 automático `APROVADO`, protocolo v2
  congelado, exatamente uma retomada autorizada e `ActivationState=None`.
- Preflight: commit `6964d12053676fa9f2d505492777ba9b339f1c25`,
  worktree limpo, zero processo/state/root/recurso Docker próprio, Docker
  `29.6.2` disponível e imagem PostgreSQL 16 pinned já presente localmente.
- Execução: a primeira campanha reteve cinco warm-ups e trinta medições de
  `FirstByte/Cold`, totalizando `35/560` samples e `1/16` summaries.
- Condição de parada: coeficiente de repetibilidade
  `0,2943936135230859`, acima do limite inclusivo imutável `0,20`; código
  `o5r5d1.threshold.repeatability-coefficient`.
- Integridade: relatório de `25.208 bytes`, SHA-256
  `4BC4DF3F436B137B909F3A034190D5FF6446A46EAD2FBBCAFEF0B67509B0153C`,
  retido atomicamente antes do cleanup. Nenhuma amostra foi removida,
  reordenada, substituída ou ocultada.
- Estado resultante: retomada PF-OBS-1 `REPROVADA`; segunda campanha,
  laboratório vivo, pipeline, corpus, calibração, holdout, resiliência e
  amostra humana não executados; `ActivationState=None`.
- Cleanup: zero processo, state file, root temporário, container, rede,
  volume ou arquivo temporário próprio. Um JSON local ignorado pelo Git
  permanece intencionalmente como evidência.
- Evidência:
  [relatório pós-D1](../../docs/STATE-06-MOD-12-PF-OBS-1-Post-D1-Resumption-Report.md).
- Próxima decisão: nova tentativa ou remediação exige autorização separada;
  nenhum Observer ou lifecycle gate está elegível.
- Aprovador: resultado automático local reprovado; nenhuma decisão humana
  inferida.

## 2026-07-25 — PF-OBS-1-D2 metodologia FirstByte v3 congelada

- Estado anterior: retomada PF-OBS-1 pós-D1 `REPROVADA` em
  `FirstByte/Cold`, evidência v2 retida e `ActivationState=None`.
- Autoridade: diagnosticar somente a evidência retida, separar latência
  absoluta e repetibilidade, implementar runner/driver test-only, testar
  offline e não executar campanha física.
- Evidência causal: os 30 valores v2 ficaram entre `0,0313` e `0,0960 ms`;
  os cinco grupos tiveram medianas `0,06310`, `0,03405`, `0,03415`,
  `0,03335` e `0,03395 ms`, com coeficiente
  `0,2943936135230859`. A primeira mediana foi mais de `1,8` vez a maior
  das restantes; o valor relativo usava a operação inteira
  submilissegundo. Scheduler, JIT e page-in não puderam ser distinguidos.
- Decisão técnica: preservar latência absoluta até o primeiro byte e usar
  para repetibilidade somente uma janela posterior predeclarada de
  `100.000` observações; exatamente um checkpoint separa os campos e todos
  os resultados brutos permanecem retidos.
- Protocolo: `pfobs1-physical-measurement-3.0.0`, SHA-256
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
  SLOs absolutos, memória, CPU, trabalho, segurança, cancelamento, `30`
  amostras, cinco grupos de seis, limite `0,20` e duas campanhas
  consecutivas não foram relaxados; FirstByte recebeu gates adicionais de
  work rate.
- Gates: `16/16` integrações focais, `10/10` provas arquiteturais focais,
  `111/111` integrações completas, `89/89` provas arquiteturais completas e
  build Release com zero aviso/erro; format, documentação, links, secrets e
  parser PowerShell passaram.
- Execução física: marcador ausente; nenhuma nova medição, campanha,
  PostgreSQL ou runtime piloto foi executado.
- Estado resultante: PF-OBS-1-D2 automático `APROVADO` no escopo test-only;
  PF-OBS-1 e O5 continuam sem aprovação; `ActivationState=None`.
- Evidência:
  [relatório D2](../../docs/STATE-06-MOD-12-PF-OBS-1-D2-FirstByte-Repeatability-Report.md)
  e
  [protocolo v3](../../docs/STATE-06-MOD-12-PF-OBS-1-Physical-Measurement-Protocol-v3.md).
- Próxima decisão: Human Gate D2 separado; qualquer campanha física futura
  exige autorização posterior e independente.
- Aprovador: resultado automático local; nenhuma decisão humana ou
  transição inferida.

## 2026-07-25 — Human Gate PF-OBS-1-D2 aprovado

- Estado anterior: PF-OBS-1-D2 automático `APROVADO`,
  `STATE-06 INTEGRATION` e `ActivationState=None`.
- Evidência revista: commit
  `28adf3a0f96debeb138c6b46868975c806a35b0e`, relatório D2 e protocolo
  `pfobs1-physical-measurement-3.0.0` com SHA-256
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
- Decisão exata de Bruno:
  `HUMAN GATE DO PF-OBS-1-D2: APROVADO`.
- Escopo aceito: diagnóstico metodológico, separação da latência absoluta
  e repetibilidade FirstByte, janela fixa de `100.000` observações,
  preservação integral das amostras e limites iguais ou mais restritivos.
- Limitações: nenhuma campanha física v3, PostgreSQL, runtime piloto,
  corpus representativo, Observer ou transição foi executado ou autorizado;
  a reprovação física v2 permanece preservada.
- Estado resultante: PF-OBS-1-D2 encerrado e humanamente `APROVADO`;
  PF-OBS-1 e O5 continuam sem aprovação; `ActivationState=None`.
- Evidência:
  [Human Gate D2](../../docs/STATE-06-MOD-12-PF-OBS-1-D2-Human-Gate-Report.md).
- Próxima decisão: somente uma autorização separada poderá liberar uma
  futura campanha física sob o protocolo v3.
- Aprovador: Bruno; nenhum lifecycle gate ou ativação inferido.

## 2026-07-25 — campanha física PF-OBS-1-V3 reprovada

- Estado anterior: PF-OBS-1-D2 automática e humanamente `APROVADO`,
  protocolo v3 congelado, PF-OBS-1 e O5 sem aprovação,
  `ActivationState=None`.
- Autoridade: executar exatamente duas campanhas físicas consecutivas de
  HM-01–HM-03, interromper na primeira falha e preservar atomicamente toda
  evidência antes do cleanup.
- Baseline e protocolo: commit
  `2f5c128e226a18cfef4f93323a66f6ebfb46e2a2`,
  `pfobs1-physical-measurement-3.0.0`, SHA-256
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
- Execução: a primeira campanha completou `158/560` amostras e `4/16`
  resumos antes de parar em `Cancellation/Cold`, repetição medida `13`.
- Condição de parada: `WorkingSetPeak` observado de `2.998.272 bytes`
  excedeu o limite inclusivo de `786.432 bytes` por `2.211.840 bytes`;
  código `o5r5d1.threshold.working-set-peak`.
- Classificação: `HM-01 BLOQUEADO`, `HM-02 REPROVADO`,
  `HM-03 NÃO TESTADO` e reprodutibilidade de duas campanhas `REPROVADA`.
- Integridade: a segunda campanha não foi iniciada; não houve campanha
  substituta nem terceira execução. FirstByte/Cold e FirstByte/Warm
  passaram o gate v3 isolado de repetibilidade, sem provar
  reprodutibilidade da campanha incompleta.
- Evidência: relatório de `123.245 bytes`, SHA-256
  `B12B8C096DE79F619B59A8F21EA3191A12FEEAE6439F729265B5CEA1AF80C1DE`,
  retido atomicamente em
  `artifacts/pf-obs-1/e4b39e453cf3486c93c6ed8682260c5e/hm-01-03-run-1.json`.
- Cleanup: zero processo, listener, root temporário ou arquivo parcial
  próprio. PostgreSQL, laboratório, pipeline, corpus e Observer não foram
  executados.
- Estado resultante: PF-OBS-1-V3 `REPROVADA`; PF-OBS-1 e O5 continuam sem
  aprovação; `ActivationState=None`.
- Evidência:
  [relatório da campanha física PF-OBS-1-V3](../../docs/STATE-06-MOD-12-PF-OBS-1-v3-Physical-Campaign-Report.md).
- Próxima decisão: diagnóstico e remediação do pico de working set exigem
  autorização separada; nenhuma nova campanha ou progressão é inferida.
- Aprovador: resultado automático local reprovado; nenhuma decisão humana
  inferida.

## 2026-07-25 — PF-OBS-1-D3 remediação Cancellation/Cold concluída

- Estado anterior: PF-OBS-1-V3 `REPROVADA` em `Cancellation/Cold`,
  PF-OBS-1 e O5 sem aprovação, `ActivationState=None`.
- Autoridade: diagnosticar a evidência retida, corrigir somente runner e
  driver test-only, validar sinteticamente sem campanha física e preservar
  limites, workload e protocolo.
- Diagnóstico: as dezoito amostras retidas alocaram `1.244.744 bytes`; a
  clonagem de `65.536` bytes por invocação representou `85,39%`–`98,79%`
  da alocação gerenciada de cada amostra e precedeu o salto process-wide de
  `2.998.272 bytes`.
- Limitação causal: a evidência comprova a pressão de alocação dominante,
  mas não distingue a propriedade das páginas exatas entre GC, runtime,
  stack ou outra região Windows.
- Remediação: um buffer frio de tamanho exato, criado uma vez antes da
  medição, recebe cópia integral nova por invocação serial; Warm continua
  usando a fonte imutável. Inner cancellation, conteúdo, trabalho e
  sequência `5 + 30` permanecem inalterados.
- Protocolo preservado: `pfobs1-physical-measurement-3.0.0`, SHA-256
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`;
  working set inclusivo `786.432 bytes`, sem nova coleta de lixo,
  dependência ou composição normal.
- Gates: `18/18` integrações focais, `11/11` provas arquiteturais focais,
  `114/114` integrações completas, `90/90` provas arquiteturais completas
  e build Release com zero aviso/erro. As `35` materializações alocaram zero
  armazenamento gerenciado por amostra; `786.432` foi aceito e `786.433`
  recusado. Cobertura focal do novo componente: `84,61%` linhas e `66,66%`
  branches.
- Execução física: marcador ausente; nenhuma campanha, PostgreSQL,
  laboratório ou Observer foi executado.
- Estado resultante: PF-OBS-1-D3 automático `APROVADO` no escopo test-only;
  a reprovação V3 permanece histórica, PF-OBS-1 e O5 continuam sem
  aprovação e `ActivationState=None`.
- Evidência:
  [relatório PF-OBS-1-D3](../../docs/STATE-06-MOD-12-PF-OBS-1-D3-Cancellation-Working-Set-Report.md).
- Próxima decisão: Human Gate D3 separado; qualquer nova campanha física
  exige autorização posterior independente.
- Aprovador: resultado automático local; nenhuma decisão humana ou
  transição inferida.

## 2026-07-25 — Human Gate PF-OBS-1-D3 aprovado

- Estado anterior: PF-OBS-1-D3 automático `APROVADO`,
  PF-OBS-1-V3 historicamente `REPROVADA`, `STATE-06 INTEGRATION` e
  `ActivationState=None`.
- Evidência revista: commit
  `5fcc0da9b5b5cb9a0fcdea4fdc971fd3736bc75f`, relatório D3 e protocolo
  `pfobs1-physical-measurement-3.0.0` com SHA-256
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
- Decisão exata de Bruno:
  `HUMAN GATE DO PF-OBS-1-D3: APROVADO`.
- Escopo aceito: diagnóstico da alocação dominante, buffer bounded único,
  cópia completa por invocação serial, cancelamento preservado e gate
  inclusivo de working set inalterado em `786.432 bytes`.
- Limitações: a propriedade das páginas exatas não foi inferida; a
  reprovação V3 permanece histórica e nenhuma nova campanha física,
  PostgreSQL, pipeline piloto, corpus ou Observer foi executado ou
  autorizado.
- Estado resultante: PF-OBS-1-D3 encerrado e humanamente `APROVADO`;
  PF-OBS-1 e O5 continuam sem aprovação; `ActivationState=None`.
- Evidência:
  [Human Gate D3](../../docs/STATE-06-MOD-12-PF-OBS-1-D3-Human-Gate-Report.md).
- Próxima decisão: somente uma autorização separada poderá liberar uma
  futura campanha física sob o protocolo V3.
- Aprovador: Bruno; nenhum lifecycle gate ou ativação inferido.

## 2026-07-25 — campanha física PF-OBS-1 pós-D3 reprovada

- Estado anterior: PF-OBS-1-D3 automática e humanamente `APROVADO`,
  protocolo V3 congelado, PF-OBS-1 e O5 sem aprovação e
  `ActivationState=None`.
- Autoridade: executar exatamente duas campanhas físicas consecutivas,
  interromper na primeira falha e reter toda evidência antes do cleanup.
- Baseline e protocolo: commit
  `ec2013a379963544e0f33b6497dc23ba503e5fd8`,
  `pfobs1-physical-measurement-3.0.0`, SHA-256
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
- Execução: a primeira campanha completou `158/560` amostras e `4/16`
  resumos antes de parar em `Cancellation/Cold`, repetição medida `13`.
- Condição de parada: `WorkingSetPeak` observado de `2.916.352 bytes`
  excedeu o limite inclusivo de `786.432 bytes` por `2.129.920 bytes`;
  código `o5r5d1.threshold.working-set-peak`.
- Efeito D3 observado: a amostra reprovada registrou allocation peak,
  cumulative managed allocation e heap delta iguais a `0 bytes`. A
  alocação removida não explica nem encerra o working-set residual, cuja
  propriedade exata permanece desconhecida.
- Classificação: `HM-01 BLOQUEADO`, `HM-02 REPROVADO`,
  `HM-03 NÃO TESTADO` e reprodutibilidade de duas campanhas `REPROVADA`.
- Integridade: a segunda campanha não foi iniciada; não houve campanha
  substituta nem terceira execução. Duas rejeições anteriores ao processo
  físico não criaram runtime/evidência e não foram contabilizadas.
- Evidência: relatório de `123.348 bytes`, SHA-256
  `4699130A77448E14E99C38BB279F5DD9A29E67D654CEA8E22FF58800943E2C84`,
  retido atomicamente em
  `artifacts/pf-obs-1/20e251594f0747dfb6f2bd5fd8046020/hm-01-03-run-1.json`.
- Cleanup: zero processo, listener, root temporário ou arquivo parcial
  próprio. PostgreSQL, pipeline, corpus e Observer não foram executados.
- Estado resultante: campanha pós-D3 `REPROVADA`; D3 continua aceito no
  seu escopo test-only, PF-OBS-1 e O5 continuam sem aprovação e
  `ActivationState=None`.
- Evidência:
  [relatório da campanha pós-D3](../../docs/STATE-06-MOD-12-PF-OBS-1-Post-D3-Physical-Campaign-Report.md).
- Próxima decisão: diagnóstico ou remediação do working-set residual exige
  autorização separada; nenhuma nova campanha ou progressão é inferida.
- Aprovador: resultado automático local reprovado; nenhuma decisão humana
  inferida.

## 2026-07-25 — PF-OBS-1-D4 bloqueado por causa residual não comprovada

- Estado anterior: campanha física pós-D3 `REPROVADA` em
  `Cancellation/Cold`, com working-set residual de `2.916.352 bytes`,
  alocação gerenciada e heap delta iguais a zero e causa sem atribuição.
- Estado solicitado: diagnóstico e remediação local, test-only, do
  working-set residual sem nova campanha física.
- Decisão: `BLOQUEADO`; o diagnóstico isolado não reproduziu o salto
  histórico e não comprovou causa suficiente para alterar o workload V3.
- Protocolo diagnóstico:
  `pfobs1-d4-cancellation-diagnostic-1.0.0`, SHA-256
  `FBA236A73DCBA87BA7247394082080AF9EB72A7D8AFE8CD3667C44A24EA5EDB7`,
  uma preparação e 35 amostras integrais por estratégia.
- Evidência instrumental: duas comparações completas foram rejeitadas porque
  `Process.Threads` e depois `Toolhelp` perturbavam a própria medição; as 140
  amostras rejeitadas foram preservadas e nenhuma foi selecionada ou ocultada.
- Comparação final: caminho `Task.Delay` `35/35`, máximo
  `516.096 bytes`; candidato wait-handle `35/35`, máximo
  `385.024 bytes`; zero resíduo de recurso próprio e cancellation abaixo de
  `100 ms` em todas as amostras.
- Disposição de código: instrumentação e writer D4 permanecem marker-gated e
  test-only; runner, workload, protocolo V3, digest, SLOs e composição normal
  permanecem inalterados.
- Gates focais: `22/22` integrações e `12/12` arquitetura passaram; build
  Release do host dedicado passou sem avisos ou erros. As suítes completas
  passaram `118/118` integrações e `91/91` arquitetura.
- Incidente de validação: `verify-dotnet-lockfiles.ps1`, invocado como gate
  de lockfile, executou restore bloqueado dos 18 projetos fora da autorização.
  O comando e o diff confirmaram zero alteração em dependências/lockfiles e
  nenhum download foi reportado, mas consulta de metadados NuGet não pode ser
  descartada nem reclassificada como autorizada.
- Escopo negativo: nenhuma campanha física, PostgreSQL, provider, banco,
  corpus, acesso externo, Observer, push, deploy ou transição.
- Estado resultante: PF-OBS-1-D4 `BLOQUEADO`, PF-OBS-1 e O5 continuam sem
  aprovação e `ActivationState=None`.
- Evidência:
  [relatório PF-OBS-1-D4](../../docs/STATE-06-MOD-12-PF-OBS-1-D4-Residual-Working-Set-Report.md).
- Próxima decisão: qualquer diagnóstico da história processual anterior a
  `Cancellation/Cold` e qualquer nova campanha física exigem autorizações
  posteriores e separadas.
- Aprovador: resultado automático local bloqueado; nenhuma decisão humana
  inferida.

## 2026-07-25 — Human Gate PF-OBS-1-D4 aceito como bloqueado

- Estado anterior: PF-OBS-1-D4 automaticamente `BLOQUEADO`, causa residual
  não comprovada, workload V3 inalterado e nenhuma campanha física executada.
- Decisão humana exata: `HUMAN GATE DO PF-OBS-1-D4: ACEITO COMO BLOQUEADO —
  reconheço que a causa do working set residual não foi comprovada, que
  nenhuma correção foi aplicada ao workload V3 e que nenhuma campanha física
  foi executada. Aceito o incidente de restore bloqueado registrado, sem
  reclassificá-lo como autorizado. Esta decisão encerra somente o D4 e não
  autoriza D5, nova campanha física, PostgreSQL, OBSERVER ou transição.`
- Disposição: a revisão humana do D4 está encerrada como
  `ACEITO COMO BLOQUEADO`; o resultado técnico não é convertido em aprovação.
- Incidente preservado: o restore bloqueado interno ao gate de lockfile
  continua fora da autoridade D4 e não é reclassificado como autorizado.
- Escopo negativo: zero código, runtime, D5, campanha física, PostgreSQL,
  provider, corpus, Observer, push, deploy ou transição.
- Estado resultante: PF-OBS-1 e O5 continuam sem aprovação,
  `STATE-06 INTEGRATION` permanece vigente e `ActivationState=None` permanece
  imutável.
- Evidência:
  [Human Gate D4](../../docs/STATE-06-MOD-12-PF-OBS-1-D4-Human-Gate-Report.md).
- Próxima decisão: somente autorização separada poderá liberar qualquer D5,
  diagnóstico adicional ou futura campanha física.
- Aprovador: Bruno; nenhum lifecycle gate ou ativação inferido.

## 2026-07-25 — PF-OBS-1-D5 bloqueado sem reprodução do excesso histórico

- Estado anterior: PF-OBS-1-D4 humanamente aceito como `BLOQUEADO`,
  workload V3 inalterado, PF-OBS-1 e O5 sem aprovação e
  `ActivationState=None`.
- Autoridade: reproduzir controladamente o histórico processual anterior a
  `Cancellation/Cold`, atribuir o working set residual ou terminar
  `BLOQUEADO`, sem campanha física.
- Esclarecimento de autoridade: somente as duas chamadas `GC.Collect`
  preexistentes no precondicionamento V3 podiam ser preservadas e executadas;
  nenhuma chamada foi adicionada, removida, movida, repetida ou condicionada.
- Protocolo:
  `pfobs1-d5-process-history-diagnostic-1.0.0`, SHA-256
  `82606BF31085214607C8CBE401C0F4050D9465523B9B01F89FFE45731012FFDA`,
  quatro variantes e duas execuções em processos novos por variante.
- Execução: prefixo exato `158/158` duas vezes, sem FirstByte `88/88` duas
  vezes, sem Idle `88/88` duas vezes e Cancellation isolada `18/18` duas
  vezes; total `704/704` amostras integralmente retidas.
- Resultado: o prefixo exato atingiu no máximo `12.288 bytes` de working-set
  delta nos dois processos; o maior controle atingiu `176.128 bytes`. Todos
  permaneceram abaixo do limite imutável de `786.432 bytes` e nenhum
  reproduziu o salto histórico de `2.916.352 bytes`.
- Decisão automática: `BLOQUEADO`; sem reprodução dupla, nenhuma fase ou
  recurso recebeu atribuição causal e nenhuma correção foi aplicada.
- Gates: `6/6` testes focais, `124/124` integrações, `1/1` arquitetura focal,
  `92/92` arquitetura completa e build Release da solução sem
  avisos/erros passaram offline e sem restore.
- Escopo negativo: zero campanha HM-01–HM-03, PostgreSQL, laboratório,
  provider, banco, corpus real, acesso externo, Observer, push, deploy ou
  transição.
- Estado resultante: PF-OBS-1-D5 `BLOQUEADO`, PF-OBS-1 e O5 continuam sem
  aprovação, `STATE-06 INTEGRATION` permanece vigente e
  `ActivationState=None` permanece imutável.
- Evidência:
  [relatório PF-OBS-1-D5](../../docs/STATE-06-MOD-12-PF-OBS-1-D5-Process-History-Diagnostic-Report.md).
- Próxima decisão: Human Gate separado pode aceitar a disposição bloqueada;
  qualquer diagnóstico ou campanha posterior exige nova autorização.
- Aprovador: resultado automático local bloqueado; nenhuma decisão humana
  inferida.

## 2026-07-25 — Human Gate PF-OBS-1-D5 aceito como bloqueado

- Estado anterior: PF-OBS-1-D5 automaticamente `BLOQUEADO`, excesso
  histórico não reproduzido, nenhuma causa atribuída e nenhuma correção ou
  campanha física executada.
- Decisão humana exata: `HUMAN GATE DO PF-OBS-1-D5: ACEITO COMO BLOQUEADO —
  reconheço que o excesso histórico de working set não foi reproduzido nas
  duas execuções do prefixo V3 exato, que nenhuma causa foi atribuída e que
  nenhuma correção ou campanha física foi executada. Esta decisão encerra
  somente o D5 e não autoriza novo diagnóstico, campanha física, PostgreSQL,
  OBSERVER ou transição.`
- Disposição: a revisão humana do D5 está encerrada como
  `ACEITO COMO BLOQUEADO`; o resultado técnico não é convertido em aprovação.
- Escopo negativo: zero código, runtime, novo diagnóstico, campanha física,
  PostgreSQL, provider, corpus, Observer, push, deploy ou transição.
- Estado resultante: PF-OBS-1 e O5 continuam sem aprovação,
  `STATE-06 INTEGRATION` permanece vigente e `ActivationState=None` permanece
  imutável.
- Evidência:
  [Human Gate D5](../../docs/STATE-06-MOD-12-PF-OBS-1-D5-Human-Gate-Report.md).
- Próxima decisão: somente autorização separada pode definir e executar
  qualquer diagnóstico, mudança metodológica ou futura campanha física.
- Aprovador: Bruno; nenhum lifecycle gate ou ativação inferido.

## 2026-07-25 — R-SEQ corrige progressão após rejeição terminal

- Estado anterior: `STATE-06 INTEGRATION`, `ActivationState=None`; uma
  rejeição terminal podia ser reconhecida localmente sem resolver o seu slot
  no cursor central e bloquear a projeção das sequências posteriores.
- Autoridade: diagnóstico final e correção focal de R-SEQ, componentes
  diretamente afetados, regressões e documentação técnica proprietária, sem
  migration, runtime externo, lifecycle ou ativação.
- Diagnóstico: validação sem persistência, reconhecimento local incondicional
  de `Rejected` e reconciliação exclusiva por `health_samples` formavam o gap
  permanente. A limitação histórica de 2026-07-19 foi preservada sem
  reescrita.
- Implementação: o cursor existente passou a representar slots contíguos
  resolvidos por amostra aceita ou rejeição consumida. O consumo ocorre sob o
  mesmo gate e transação serializável, não cria evidência de saúde e
  reconcilia sucessores válidos já armazenados. Rejeição acima de gap
  permanece retryable, e sequência já resolvida não aceita inserção
  retroativa.
- Barreira local: o dispatcher somente reconhece `Rejected` quando
  `HighestContiguousSequence` cobre o envelope. Payload local ou HTTP `4xx`
  sem prova autoritativa permanece pendente em backoff.
- Gates: build Release da solução com zero avisos/erros; `52/52` regressões de
  sincronização; `1/1` E2E focal; suítes completas `408/408` unitários,
  `124/124` integrações, `92/92` arquitetura e `10/10` WPF; cobertura
  `82,09%` linhas e `54,26%` branches com `10/10` componentes obrigatórios;
  format, documentação de código, links Markdown e secret scan aprovados.
- Ressalvas: sem migration, o cursor não retém centralmente o `MessageId` nem
  o motivo da rejeição; gaps históricos não são reconstruídos; recusas
  exclusivamente locais/request-level exigem correção ou intervenção; a
  janela concorrente de `R-EGRESS` e PostgreSQL serializável real não foram
  avaliados.
- Escopo negativo: zero dependência, migration, PostgreSQL operacional,
  runtime externo, PM-3, R-EGRESS, lifecycle, ActivationState, push, PR ou
  deploy.
- Estado resultante: R-SEQ automaticamente `APROVADO` somente no escopo
  local, `STATE-06 INTEGRATION` e `ActivationState=None` inalterados.
- Evidência:
  [relatório R-SEQ](../../docs/STATE-06-R-SEQ-Rejected-Observation-Sequence-Remediation-Report.md).
- Próxima decisão: qualquer correção R-EGRESS, migration, runtime,
  lifecycle, ativação ou lote posterior exige autorização separada.
- Aprovador: autoridade de execução concedida por Bruno; resultado automático
  local, sem Human Gate ou transição inferidos.

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

## 2026-07-25 — R-EGRESS lineariza ingestão de observações e revogação principal

- Estado anterior: `STATE-06 INTEGRATION`, `ActivationState=None`; R-SEQ
  encerrado localmente, mas a ingestão ainda podia ler `Active`, perder a
  ordem para uma revogação principal e commitar efeitos de saúde depois.
- Autoridade: execução integral e focal de R-EGRESS somente para a corrida
  entre verificação de Agent ativo, revogação e commit da ingestão
  autoritativa, sem aprovação intermediária.
- Desambiguação: neste lote `R-EGRESS` designa exclusivamente essa corrida;
  SSRF e política de network egress não foram avaliados nem encerrados.
- Diagnóstico: transações serializáveis independentes protegiam o cursor e a
  identidade em linhas diferentes; o gate anterior pertencia apenas à
  ingestão e a consulta `Active` não bloqueava `agents`.
- Implementação: um gate por Agent passou a ordenar ingestão, consumo R-SEQ e
  revogação principal no processo. A ingestão PostgreSQL bloqueia a identidade
  com `FOR NO KEY UPDATE` até commit/rollback, exige `State=Active` e
  `RevokedAt=null` e reclassifica falha concorrente numa transação nova com a
  ordem `identity lock → replay → rejeição`. SQLite permanece somente fallback
  local exato; o caminho de ingestão aceita falha fechado para terceiro
  provider.
- Limite por mensagem: a observação que perde para a revogação não cria
  amostra ou efeito próprio. O consumo R-SEQ do gap pode projetar uma sucessora
  aceita antes da revogação, sempre com a proveniência dessa sucessora.
- Regressões: as duas ordens concorrentes e o drift `Active + RevokedAt`
  passaram `3/3`; proveniência/classificação passaram `2/2`; o guard
  arquitetural passou `1/1`; sincronização/R-SEQ/revogação passaram `63/63`;
  o E2E autoritativo existente passou `1/1`.
- Gates: build Release com zero warnings/erros; suítes completas `413/413`
  unitários, `124/124` integrações, `93/93` arquitetura e `10/10` WPF;
  cobertura `82,11%` linhas e `54,30%` branches com `10/10` componentes;
  format, documentação de código, links Markdown e secret scan aprovados.
- Nota de execução: uma tentativa paralela teve `123/124` integrações por
  prazo do diagnóstico não relacionado `WaitHandleCandidateIsTimelyAndCancellationAware`;
  a suíte isolada passou `124/124`, e somente as execuções isoladas compõem os
  totais finais.
- Shutdown: nove helpers de build órfãos pertencentes ao workspace foram
  encerrados após verificação de caminho e parentage; a auditoria
  pós-validação terminou com zero processo, listener ou janela de produto
  DB-Notifier, preservando a IDE do usuário.
- Escopo negativo: zero dependência, migration, PostgreSQL operacional,
  runtime externo, PM-3, SSRF/network egress, lifecycle, `ActivationState`,
  push, PR ou deploy.
- Ressalvas: o row lock PostgreSQL foi implementado e protegido por teste de
  fonte, mas PostgreSQL real, SQLSTATE `40001`, multiprocesso e latência sob
  carga não foram executados. O cursor de uma rejeição continua sem ledger de
  motivo/`MessageId` por ausência de migration. O gate local não garante
  prioridade, fairness ou limite próprio de espera e a revogação o adquire
  antes do RBAC definitivo; contenção e latência desse risco não foram
  testadas.
- Estado resultante: R-EGRESS automaticamente `APROVADO` somente no escopo
  local validado; `STATE-06 INTEGRATION` e `ActivationState=None` inalterados.
- Evidência:
  [relatório R-EGRESS](../../docs/STATE-06-R-EGRESS-Observation-Ingestion-Revocation-Linearisation-Report.md).
- Próxima decisão: a aceitação focal do proprietário pode encerrar somente
  este lote; qualquer PM-3, SSRF/network egress, PostgreSQL real, lifecycle,
  ativação ou lote posterior exige autorização separada.
- Aprovador: autoridade de execução concedida por Bruno; resultado automático
  local, sem Human Gate ou transição inferidos.

## 2026-07-26 — Aceitação focal de R-EGRESS e consolidação pública PM-3

- Estado anterior: `STATE-06 INTEGRATION`, `ActivationState=None`; R-EGRESS
  automaticamente aprovado no escopo local, mas a aceitação posterior do
  proprietário ainda não estava registrada no corpus.
- Decisão: Bruno declarou exatamente `ACEITO O R-EGRESS exclusivamente no
  escopo da corrida Active/revogação/commit da ingestão de observações, sem
  autorizar SSRF ou política de network egress, PM-3, dependências, migrations,
  runtime externo, lifecycle, ActivationState, push, PR, deploy ou qualquer
  lote posterior.` Depois revogou somente a restrição contra lotes posteriores
  e autorizou a execução sequencial dos lotes técnicos locais obrigatórios,
  preservando as demais fronteiras declaradas.
- Escopo PM-3: corrigir fatos públicos obsoletos sobre Design System e Human
  Gates, encaminhar verdade presente ao estado corrente e rotular o relatório
  de migração de prompts como evidência histórica, sem reescrever seus fatos.
- Gates: baseline limpa; auditoria pública sem alegação indevida adicional de
  suporte/homologação; `757` links locais em `198` arquivos Markdown aprovados;
  caminhos e comandos públicos citados existentes.
- Estado resultante: R-EGRESS aceito somente no escopo focal já validado; PM-3
  concluído documentalmente; `STATE-06 INTEGRATION` e
  `ActivationState=None` inalterados.
- Riscos/ressalvas: links externos não foram consultados; políticas públicas
  sem proprietário ou contacto factual não foram inventadas. SSRF/network
  egress continua um lote técnico separado neste ponto histórico.
- Aprovador: Bruno, exclusivamente nas fronteiras acima; nenhuma decisão de
  lifecycle, Human Gate ou ativação foi inferida.

## 2026-07-26 — R-FENCE fecha regressões do lifecycle do gate por Agent

- Estado anterior: `STATE-06 INTEGRATION`, `ActivationState=None`; R-EGRESS
  aceito no seu escopo focal, com o lifecycle do gate interno coberto apenas
  indiretamente pelas ordens funcionais de ingestão e revogação.
- Autoridade: execução sequencial dos lotes técnicos locais obrigatórios, sem
  pausas intermediárias e sem ampliar as fronteiras proibidas.
- Diagnóstico: cancelamento de waiter, liberação excepcional, descarte
  repetido e independência entre Agents eram propriedades implementadas, mas
  ainda não possuíam regressões determinísticas diretas.
- Implementação: acesso interno concedido exclusivamente ao assembly de
  testes; quatro regressões diretas adicionadas; comentário XML inexato sobre
  um owner “bounded” corrigido. Nenhum algoritmo ou contrato produtivo mudou.
- Gates: regressões focais `4/4`; suíte unitária completa `417/417`; build
  Release da solução com zero warnings/erros; cobertura `82,12%` de linhas e
  `54,31%` de branches, com `10/10` componentes obrigatórios.
- Escopo negativo: zero dependência, migration, runtime externo, PostgreSQL
  operacional, lifecycle, Human Gate, `ActivationState`, push, PR ou deploy.
- Estado resultante: R-FENCE automaticamente `APROVADO` somente no escopo
  local de regressão; `STATE-06 INTEGRATION` e `ActivationState=None`
  inalterados.
- Riscos/ressalvas: PostgreSQL real, multiprocesso, fairness, prioridade,
  timeout próprio e latência sob carga permanecem não testados.
- Evidência:
  [relatório R-FENCE](../../docs/STATE-06-R-FENCE-Agent-Identity-Fence-Lifecycle-Regression-Report.md).
- Aprovador: autoridade de execução concedida por Bruno; resultado automático
  local, sem Human Gate, lifecycle ou ativação inferidos.

## 2026-07-26 — R-NET fecha a fronteira local de network egress

- Estado anterior: `STATE-06 INTEGRATION`, `ActivationState=None`; PM-3 e
  R-FENCE encerrados, enquanto os caminhos explícitos de saída ainda não
  possuíam uma autoridade comum de destino, DNS, pinning e PKI offline.
- Autoridade: Bruno revogou a restrição anterior contra lotes posteriores e
  autorizou a identificação e execução sequencial de todos os lotes técnicos
  locais obrigatórios, sem pausas intermediárias, preservando a proibição de
  novas dependências, migrations operacionais, runtime ou ação externa,
  lifecycle, Human Gates, `ActivationState`, push, PR e deploy.
- Diagnóstico: sincronização HTTP podia herdar redirect, proxy e resolução do
  handler; `pg_isready`, TCP e Npgsql recebiam hostname; OIDC e PostgreSQL
  central não compartilhavam uma política positiva; a validação de
  certificados permitia tentativa online; e o shim PowerShell admitia host
  remoto. A ameaça estava documentada, mas os consumidores não possuíam uma
  autoridade local única que entregasse somente IP aprovado.
- Implementação: quatro políticas imutáveis por consumidor compilam
  allowlists CIDR, denylists prioritárias, portas e limites de DNS. Respostas
  mistas, metadata, link-local, multicast, unspecified, broadcast, escopo IPv6
  e endereços fora da lista são recusados atomicamente. HTTP reautoriza cada
  socket físico, não usa redirect, proxy, cookie ou credencial ambiental e
  conecta ao IP aprovado. Provider, Agent, OIDC e PostgreSQL central retêm o
  hostname original apenas para SNI e identidade TLS. Cadeias usam revogação
  offline e downloads desabilitados. O legado ficou limitado a loopback sem
  DNS remoto.
- Gates: `78/78` regressões focais; build Release com zero warnings/erros;
  `501/501` unitários, `124/124` integrações, `96/96` arquitetura e `10/10`
  WPF; cobertura `82,41%` de linhas e `55,89%` de branches com `10/10`
  componentes; Pester `34` aprovados, um skip condicional previsto e
  `35,17%` de cobertura de comandos; format e documentação de código
  aprovados; `768` links locais em `201` arquivos e secret scan do worktree
  mais histórico Git aprovados; JSON alterado válido; zero manifest de
  dependência ou migration alterado.
- Nota de execução: a primeira suíte de integração produziu `123/124` porque
  o diagnóstico temporal O5 preexistente
  `WaitHandleCandidateIsTimelyAndCancellationAware` falhou sob carga local.
  O caso passou isoladamente e a repetição completa passou `124/124`; nenhuma
  mudança R-NET toca esse diagnóstico. Um guard arquitetural intermediário
  detectou leitura HTTP direta no OIDC, que foi substituída pelo leitor JSON
  compartilhado e limitado antes do resultado final `96/96`, que inclui três
  guards adicionais da composição normal.
- Escopo negativo: zero dependência, migration, runtime de produto, DNS/IdP,
  PKI/PostgreSQL operacional, acesso de rede, lifecycle, Human Gate,
  `ActivationState`, push, PR ou deploy.
- Shutdown final: sete nós MSBuild e um `VBCSCompiler` órfãos, todos
  comprovadamente executados pelo SDK local do workspace, foram encerrados. A
  verificação terminou com zero processo, listener ou janela de produto; a
  janela da IDE do utilizador foi preservada.
- Ressalvas: a revogação offline depende de material local atual; o trust
  continua pertencendo ao sistema operacional; a conexão PostgreSQL central
  fixa o primeiro IP aprovado até restart; cada conexão HTTP física reavalia
  DNS, mas um socket pooled válido pode ser reutilizado; falha de certificado
  no handshake precede auditoria HTTP; proxy, failover e comportamento real
  sob carga não foram homologados.
- Estado resultante: R-NET automaticamente `APROVADO` somente no escopo local
  validado; `STATE-06 INTEGRATION` e `ActivationState=None` inalterados.
- Evidência:
  [relatório R-NET](../../docs/STATE-06-R-NET-Network-Egress-Remediation-Report.md).
- Aprovador: autoridade de execução concedida por Bruno; resultado automático
  local, sem Human Gate, lifecycle ou ativação inferidos.

## 2026-07-26 — R-SEQ passa a preservar rejeições consumidas em ledger durável

- Estado anterior: `STATE-06 INTEGRATION`, `ActivationState=None`; a correção
  cursor-only de R-SEQ encerrava o gap sequencial, mas não retinha
  centralmente `MessageId` ou motivo da rejeição consumida.
- Autoridade: complemento técnico focal de R-SEQ limitado aos componentes
  diretamente afetados, migration de código, regressões e documentação
  proprietária, sem migration operacional, lifecycle, Human Gate, ativação ou
  ação externa.
- Diagnóstico: um ledger sem marco de corte confundiria posições históricas
  não reconstruíveis com evidência pós-migration ausente. A retenção futura de
  amostras aceitas também precisa preservar uma prova durável do slot antes de
  qualquer purga.
- Implementação: `agent_observation_cursors` ganhou um corte inclusivo por
  Agent e `rejected_observation_sequences` passou a guardar Agent, sequência,
  `MessageId`, motivo sanitizado e instante autoritativo de consumo. Rejeição
  e cursor commitam na mesma transação serializável; replay do mesmo
  slot/`MessageId` conserva o motivo original; evidência aceita/rejeitada
  coexistente ou ausente após o corte falha fechada como retryable. Nenhuma
  rejeição cria amostra, estado, evento, outbox ou delivery próprio.
- Migration: `AddRejectedObservationSequenceLedger` tornou-se a nona migration
  Server. Cursores existentes adotam `highest_contiguous_sequence + 1`, novos
  cursores começam no slot um e nenhum fato histórico é sintetizado. O `Up`
  recusa overflow do corte; o `Down` recusa apagar qualquer ledger não vazio.
- Evidência PostgreSQL: laboratório descartável, limitado a loopback e usando
  `postgres:16-alpine` fixado por
  `sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb`
  passou `1/1` para upgrade/backfill, consumo/replay, rollback protegido,
  rollback vazio, overflow guard e reaplicação. O recurso próprio foi removido
  sem resíduo; nenhum PostgreSQL existente, monitorado ou operacional foi
  migrado.
- Gates: build Release com zero warnings/erros; `507/507` unitários, `96/96`
  arquitetura, `125/125` integrações na repetição completa, `10/10` WPF, E2E
  focal `1/1` e laboratório de migration `1/1`; cobertura `83,33%` de linhas
  e `56,18%` de branches com `10/10` componentes obrigatórios. A primeira
  execução de integração teve um flake temporal O5/R5 não relacionado, que
  passou isoladamente antes da repetição completa. Formatação, documentação de
  406 arquivos de código, 772 links locais em 202 arquivos e secret scan do
  worktree não ignorado mais histórico Git passaram.
- Retenção: o ledger fica fora da retenção temporal ordinária. Purga futura de
  amostra aceita após o corte exige tombstone durável ou ledger de resolução
  equivalente, em lote separado.
- Escopo negativo: zero migration operacional, runtime normal, lifecycle,
  Human Gate, `ActivationState`, push, PR, deploy ou ação em banco monitorado.
- Shutdown final: oito helpers MSBuild/Roslyn comprovadamente pertencentes ao
  SDK local do workspace foram encerrados; restaram zero processo ou listener
  DB-Notifier e zero contêiner R-SEQ, R-EGRESS ou R-NET.
- Estado resultante: `STATE-06 INTEGRATION` e `ActivationState=None`
  inalterados; nenhuma transição ou ativação foi inferida.
- Evidência:
  [relatório do ledger R-SEQ](../../docs/STATE-06-R-SEQ-Durable-Rejection-Ledger-Report.md).
- Aprovador: autoridade de execução concedida por Bruno; a evidência focal não
  constitui Human Gate, lifecycle ou ativação.

## 2026-07-26 — R-EGRESS/R-FENCE passa campanha física PostgreSQL multiprocesso endurecida

- Estado anterior: `STATE-06 INTEGRATION`, `ActivationState=None`; R-EGRESS e
  R-FENCE possuíam evidência determinística local, mas PostgreSQL real
  descartável, processos independentes, SQLSTATE `40001` e carga concorrente
  ainda não tinham evidência física proprietária.
- Autoridade: Bruno autorizou a execução sequencial de todos os lotes técnicos
  locais ainda obrigatórios, incluindo shutdown, implementação, testes,
  cobertura e documentação, sem novas dependências, migration operacional,
  runtime ou ação externa, lifecycle, Human Gate, ativação, push, PR ou deploy.
- Diagnóstico e correção produtiva: a primeira campanha expôs um SQLSTATE
  `40001` encapsulado por `InvalidOperationException`. O store passou a
  reconhecer somente cadeias que contêm `DbUpdateException` ou `DbException`,
  sem capturar `InvalidOperationException` arbitrária nem cancelamento. A
  regressão focal preserva essa fronteira.
- Endurecimento test-only: cada item de carga passou a atribuir `40001` pelo
  seu próprio delta, impedindo contaminação por ocorrência anterior do mesmo
  processo. O cleanup passou a tentar todos os filhos, acumular falhas para
  nova tentativa e varrer host, marcador e token exclusivos do runner antes
  de concluir ausência de resíduo.
- Campanha física: o resumo fechado `schemaVersion=2` passou `8/8` cenários
  contra `postgres:16-alpine` fixado por
  `sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb`.
  Foram observadas sete relações blocker/waiter exatas, `86` ocorrências de
  SQLSTATE `40001`, `106` disposições `Accepted`, `27` `Rejected`, `68`
  `Retryable` e `68/68` convergências bounded, com zero deadlock ou falha não
  classificada.
- Carga mesmo Agent: `100/100` operações, `38` ocorrências de `40001`, zero
  `Retryable`, máximo de três sessões bloqueadas, p50 `105,4391 ms`, p95
  `334,8821 ms`, p99 `4.195,9385 ms`, máximo `4.239,3057 ms` e throughput
  observado `11,3550 ops/s`.
- Carga Agents distintos: `100/100` operações, `45` ocorrências de `40001`,
  `67` `Retryable`, máximo de uma sessão bloqueada, p50 `127,4807 ms`, p95
  `275,6282 ms`, p99 `3.474,3736 ms`, máximo `5.364,1133 ms` e throughput
  observado `11,6109 ops/s`.
- Gates: build Release focal com zero warnings/erros; `508/508` unitários,
  `96/96` arquitetura, `126/126` integração e `10/10` WPF; cobertura `83,35%`
  de linhas e `56,27%` de branches com `10/10` componentes obrigatórios;
  `dotnet format`, parser PowerShell, PSScriptAnalyzer, documentação de `411`
  fontes comment-capable, `781` links locais em `203` arquivos Markdown e
  secret scan do worktree não ignorado mais histórico Git aprovados.
- Cleanup: `containers=0`, `networks=0`, `volumes=0`, diretórios temporários
  próprios `=0`, processos DB-Notifier `=0` e listeners DB-Notifier `=0`.
- Shutdown documental final: sete nós MSBuild reutilizáveis, todos comprovados
  pelo PID, caminho do SDK local e modo de nó, foram encerrados. A repetição
  terminou com zero processo ou listener do workspace.
- Limites: a posição registrada da revogação foi zero, mas isso não estabelece
  prioridade. A execução não prova SLO, fairness, starvation freedom, limite
  próprio de espera, PostgreSQL operacional, homologação ou suporte público.
  Nenhuma migration foi aplicada a banco existente, monitorado ou operacional.
- Estado resultante: `STATE-06 INTEGRATION` e `ActivationState=None`
  inalterados; o resultado automático não constitui Human Gate, transição de
  lifecycle ou ativação.
- Evidência:
  [relatório físico R-EGRESS/R-FENCE](../../docs/STATE-06-R-EGRESS-R-FENCE-PostgreSql-Multiprocess-Load-Report.md).
- Aprovador: autoridade de execução concedida por Bruno; resultado automático
  local, sem Human Gate, lifecycle ou ativação inferidos.

## 2026-07-26 — R-NET passa homologação física local de DNS, PKI, IdP e PostgreSQL TLS

- Estado anterior: `STATE-06 INTEGRATION`, `ActivationState=None`; R-NET
  possuía política produtiva e evidência determinística, mas o resolvedor do
  sistema, o mecanismo de cadeia/revogação, o backchannel IdP e PostgreSQL TLS
  ainda não tinham uma campanha física local conjunta.
- Autoridade: lote condicional local R-NET em fixtures controladas de DNS,
  PKI, IdP e PostgreSQL TLS, com shutdown, implementação, testes, cobertura,
  documentação e commit focal; sem download, dependência, dado/credencial
  real, infraestrutura remota, migration operacional, lifecycle, Human Gate,
  ativação, push, PR ou deploy.
- Implementação: os três consumidores TLS diretamente afetados ganharam uma
  seam `internal` de fábrica de política de cadeia para integração. Todos os
  construtores públicos e a composição produtiva continuam em trust `System`,
  revogação `Offline`, `EntireChain`, `NoFlag`, ServerAuth, downloads
  desabilitados e `CustomTrustStore` vazio. Nenhum callback permissivo ou
  configuração produtiva de trust foi acrescentado.
- PKI: a CA sintética permaneceu ausente de `CurrentUser\Root`. Somente sua
  CRL pública exata foi registrada temporariamente em `CurrentUser\CA` e
  removida ao final. Tentativas preliminares que acionaram aviso protegido do
  Windows foram recusadas, sem aceitar ou automatizar o diálogo; `certutil`
  silencioso também falhou fechado. A repetição aprovada evitou integralmente
  o Root store.
- DNS/IdP: o resolvedor do sistema admitiu somente respostas loopback para
  `localhost`; DNS controlado provou recusa atômica de metadata, nova admissão
  e IP pinning. Discovery/JWKS/JWT HTTPS local retornou `200` no caso válido e
  `401` para redirect, origem cruzada, issuer, audience e chave incorretos,
  com zero hit nos destinos recusados.
- PostgreSQL: quatro células 16 Alpine descartáveis foram executadas
  sequencialmente na imagem local fixada por
  `sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb`,
  sem pull, volume ou migration. A bridge própria desabilitou masquerade e
  comunicação entre contêineres; cada célula removeu rotas default, substituiu
  o resolver efetivo por loopback, recusou resolução `.invalid`, executou
  PostgreSQL como não-root com capacidades ativas zero/`NoNewPrivs=1` e expôs
  um listener Windows somente em `127.0.0.1`. Foram provados o SHA-256 exato do
  certificado servido, PostgreSQL 16, `VerifyFull`, `pg_stat_ssl`, provider
  `Healthy`, senha incorreta distinta, hostname/raiz/revogação/CRL recusados
  nos caminhos central e provider e negativa de política antes do transporte.
- Endurecimento de cleanup: um lock exclusivo impede campanhas concorrentes.
  O preflight recupera somente Docker com labels/nome/run ID exatos e CRL cujo
  par público preservado possui subject esperado e assinatura válida; falha
  controlada de remoção preserva esse artefacto. Crash não cooperativo não foi
  injetado na campanha final.
- Campanha final: `11/11` casos em cinco invocações bounded; build Release de
  `19` projetos com zero warnings/erros; `508/508` unitários, `96/96`
  arquitetura, `137/137` integração comum e `10/10` WPF; cobertura `83,37%`
  de linhas e `56,26%` de branches com `10/10` componentes presentes.
  Formatação, parser, PSScriptAnalyzer, documentação de `415` fontes,
  inventário estático de `19/19` lockfiles, `792` links Markdown locais em
  `204` arquivos e secret scan do worktree não ignorado mais histórico Git
  passaram. Locked restore e freshness online de advisories não foram
  executados porque download e infraestrutura externa estavam proibidos.
- Cleanup da campanha e auditoria externa: processos/listeners próprios `=0`,
  contêineres/redes/volumes próprios `=0`, diretórios/logs temporários `=0`,
  certificados sintéticos em `CurrentUser\Root`, `CA` e `My` `=0`, CRL
  própria `=0`.
- Shutdown final: zero processo ou listener DB-Notifier, zero helper do SDK
  local do workspace e zero recurso R-NET. O único processo `dotnet`
  remanescente foi comprovado por PID, executável e linha de comando como o
  build host da extensão C# do VS Code em `C:\Program Files\dotnet` e foi
  preservado por não pertencer ao runtime do projeto.
- Limites: a evidência não prova DNSSEC, DNS/PKI/IdP/PostgreSQL operacional,
  Root/enterprise trust provisioning, CRL/OCSP operacional, provider
  homologado, proxy, failover, HA, cross-platform, performance, disponibilidade
  ou suporte público. `CustomRootTrust` permanece estritamente test-only.
- Estado resultante: `STATE-06 INTEGRATION` e `ActivationState=None`
  inalterados; o resultado automático não constitui Human Gate, lifecycle ou
  ativação.
- Evidência:
  [relatório físico R-NET](../../docs/STATE-06-R-NET-Local-DNS-PKI-IdP-PostgreSql-TLS-Homologation-Report.md).
- Aprovador: autoridade de execução concedida por Bruno; resultado automático
  local, sem Human Gate, lifecycle ou ativação inferidos.

## 2026-07-26 — Estado corrente recomposto como snapshot factual compacto

- Estado anterior: `STATE-06 INTEGRATION`, `ActivationState=None`; após o PM-2,
  o estado corrente voltou a acumular métricas, hashes, topologias e narrativa
  de execução dos lotes R-SEQ, R-EGRESS, R-FENCE, R-NET e PM-3, embora esses
  fatos já estivessem preservados neste log append-only e nos relatórios
  proprietários.
- Autoridade: execução dos trabalhos locais restantes solicitada por Bruno,
  limitada neste incremento à manutenção documental do corpus e sem ampliar
  autoridade técnica, operacional ou externa.
- Diagnóstico: `Current-State.md` havia crescido de 10.877 bytes e 180 linhas
  após o PM-2 para 17.420 bytes e 269 linhas. O crescimento reintroduziu
  evidência histórica detalhada num documento cuja responsabilidade é
  apresentar somente o presente factual.
- Alteração: o bloco R-* foi reduzido a quatro resumos correntes: disposição e
  ressalvas do programa R0–R8; invariantes e limites R-SEQ; garantia focal e
  limites R-EGRESS/R-FENCE; e política e limites R-NET. A narrativa PM-3 foi
  removida do snapshot porque permanece preservada na entrada histórica
  própria.
- Preservação: comandos, contagens, cobertura, hashes, topologia, cleanup,
  decisões, incidentes e limites retirados continuam íntegros nas entradas
  históricas e nos relatórios proprietários. A conclusão transversal de que
  laboratórios focais posteriores não substituem evidências não executadas no
  escopo R8 permaneceu explícita no resumo corrente. Nenhum relatório
  histórico foi reescrito.
- Gates documentais: links locais, headings, referências do corpus, whitespace
  e diff foram revalidados no estado resultante sem alvo quebrado.
- Escopo negativo: zero alteração de código executável, configuração,
  dependência, migration, runtime, lifecycle, Human Gate, `ActivationState`,
  provider, suporte público ou ação externa.
- Estado resultante: `STATE-06 INTEGRATION` e `ActivationState=None`
  inalterados; `Current-State.md` voltou a distinguir presente factual de
  evidência histórica detalhada.
- Evidência:
  [`Current-State.md`](Current-State.md) e
  [changelog `4.1.1`](../system/Prompt-System-Change-Log.md).
- Aprovador: autoridade de manutenção local concedida por Bruno; nenhuma
  aprovação de lifecycle, Human Gate ou ativação foi inferida.

## 2026-07-26 — R-D4-TIMING isola diagnóstico temporal de carga alheia

- Estado anterior: `STATE-06 INTEGRATION`, `ActivationState=None`; a flutuação
  preexistente de `WaitHandleCandidateIsTimelyAndCancellationAware` já havia
  sido registrada em R-EGRESS e R-NET.
- Autoridade: execução sequencial de todos os trabalhos técnicos locais ainda
  obrigatórios, sem dependência, runtime externo, lifecycle, Human Gate,
  ativação ou ação remota.
- Diagnóstico: duas execuções da solução e uma execução da assembly
  reproduziram `136/137` integrações porque o timer de `CancelAfter(10 ms)` não
  sinalizou dentro do deadline congelado de `100 ms` sob concorrência das
  demais coleções; o caso filtrado passou `1/1` em `28 ms`.
- Alteração: uma coleção xUnit própria, marcada com
  `DisableParallelization = true`, passou a isolar somente a classe de
  diagnóstico O5/R5. Deadline, timer, `WaitHandle`, `Stopwatch`, cancelamento
  externo e asserções permaneceram inalterados.
- Resultado: build focal com zero warnings/erros, assembly de integração
  `137/137`, solução completa `771/771`, formatação e documentação de `420`
  fontes aprovadas; cobertura `83,41%` de linhas e `56,62%` de branches com
  `10/10` componentes obrigatórios.
- Limite: o isolamento elimina concorrência entre coleções da assembly, não
  concede imunidade a saturação arbitrária do host, benchmark ou SLO. Eventual
  interferência interprocesso exigirá processo/projeto dedicado, nunca
  flexibilização silenciosa do deadline.
- Escopo negativo: zero mudança de produto, dependência, configuração,
  migration, runtime, R5 histórico, lifecycle, Human Gate, `ActivationState`,
  push, pull request ou deploy.
- Estado resultante: `STATE-06 INTEGRATION` e `ActivationState=None`
  inalterados.
- Evidência:
  [relatório R-D4-TIMING](../../docs/STATE-06-R-D4-Timing-Diagnostic-Isolation-Report.md).
- Aprovador: resultado automático local sob autoridade de manutenção de Bruno;
  nenhuma aprovação de lifecycle, Human Gate ou ativação foi inferida.

## 2026-07-27 — Baseline ff0adc7 reconciliada e proposta STATE-06 → STATE-07 invalidada

- Estado anterior: `STATE-06 INTEGRATION`, `ActivationState=None`; a proposta
  de transição criada no commit `45fb3d3` permanecia documental, não
  autorizante e ainda continha um texto histórico de autorização futura
  baseado na elegibilidade avaliada em 2026-07-20.
- Autoridade: Bruno autorizou exclusivamente um incremento documental para
  reconciliar a baseline `ff0adc7`, atualizar a verdade factual e os registros
  aplicáveis, invalidar a proposta antiga para execução e executar os gates
  documentais pertinentes, sem código-fonte, lifecycle, `ActivationState`,
  infraestrutura externa, runtime operacional ou commit.
- Baseline observada: `HEAD` em
  `ff0adc76166d82d01542aa091e15dd39e7ff3fa1`; os commits `84217c6`,
  `2c1e05f`, `10a8249`, `1a27dca`, `96cf248` e `45fb3d3` permanecem ancestrais.
  Entre `96cf248` e `ff0adc7` existem `118` commits e `412` caminhos alterados,
  incluindo `254` caminhos de fonte ou configuração executável, oito
  manifests/lockfiles e sete arquivos sob `Migrations/` do Server PostgreSQL.
  A ancestralidade histórica não substitui a revalidação dessa mudança
  material.
- Último commit: `ff0adc7` altera somente
  `src/DBNotifier.Dashboard.Web/package-lock.json`, atualizando as resoluções
  de desenvolvimento de `postcss` `8.5.17 → 8.5.23` e `nanoid`
  `3.3.15 → 3.3.16`. Nenhum código-fonte, lifecycle ou `ActivationState` foi
  alterado por esse commit.
- Decisão documental: a proposta antiga foi marcada
  `INVALIDADA PARA EXECUÇÃO`; seu texto copiável de autorização foi revogado e
  retirado da versão corrente. O critério que exigia ausência de mudança
  técnica posterior não está satisfeito em `ff0adc7`. Os gates históricos
  permanecem preservados nas respectivas baselines, mas não são
  reclassificados nem tratados como elegibilidade corrente.
- Worktree preexistente: `Project-Recovery-Instructions.md` já estava
  não rastreado no início e foi preservado sem leitura ou alteração.
- Gates documentais: documentação de código aprovada para `420` fontes
  comment-capable; `808` links Markdown locais em `206` arquivos aprovados;
  secret scan do worktree não ignorado e do histórico Git disponível
  aprovado; escopo limitado a cinco documentos rastreados e
  `git diff --check` aprovados. Build, testes e runtime de produto são
  `NÃO APLICÁVEIS` e não foram executados.
- Escopo negativo: zero alteração de código-fonte, lifecycle,
  `ActivationState`, dependência, lockfile, migration, runtime operacional,
  infraestrutura externa, provider, banco, credencial, deploy, push, PR ou
  commit.
- Estado resultante: `STATE-06 INTEGRATION` e `ActivationState=None`
  inalterados; não existe proposta executável corrente para a transição
  `STATE-06 → STATE-07`.
- Próxima condição: qualquer reconsideração futura exige autorização separada
  para uma nova proposta exclusivamente documental, reconciliada com a
  baseline então vigente e com revalidação proporcional das mudanças
  posteriores. Essa proposta futura também não executará a transição.

## 2026-07-28 — PF-OBS-1-D6 interrompido sem evidência física durável

- Estado anterior: `STATE-06 INTEGRATION`, `ActivationState=None`; D5
  humanamente aceito como `BLOQUEADO`, PF-OBS-1 e O5 sem aprovação e nenhuma
  rebaseline válida no ambiente pós-formatação.
- Autoridade: registrar o ambiente sanitizado, pré-registrar e implementar um
  D6 local, sintético e test-only e executar exatamente duas repetições
  não observadas do prefixo V3. Observação externa e repetição comparativa de
  D5 ficavam condicionadas à reprodução do excesso em ambas.
- Protocolo:
  `pfobs1-d6-post-format-non-intrusive-rebaseline-1.0.0`, SHA-256
  `5B1ED90AC9238B57F94AF923434589A0F0E22925EE98DFB753DCF392A209A845`,
  dependente do V3 imutável
  `60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A`.
  O prefixo contém `158` cenários, os quatro resumos V3 originais em suas
  posições temporais e zero captura intraprocesso adicional.
- Ambiente sanitizado: Windows `10.0.26200` X64, processo X64, SDK
  `10.0.302`, runtime `Microsoft.NETCore.App 10.0.10`, oito processadores
  lógicos, `16.963.534.848 bytes` de memória física e frequência monotônica
  de `10.000.000 ticks/s`; nenhuma identidade de máquina foi retida.
- Incidente: a primeira execução não observada chegou à publicação, mas saiu
  com `o5r5d6.failed:IOException`. O writer tentou mover o arquivo temporário
  enquanto o stream write-through ainda estava aberto; o cleanup removeu o
  temporário e nenhum JSON durável permaneceu.
- Disposição fail-closed: sem relatório durável, contagem, completude,
  reprodução e causa não foram inferidas. A tentativa não foi substituída, a
  segunda execução não começou e observação externa e controle D5 não foram
  executados.
- Correção: o stream passou a ser fechado em escopo explícito antes do move
  atômico e uma regressão reproduz a publicação e a ausência de `.tmp`. O
  protocolo, o V3, seus limites, workloads e duas coletas preexistentes
  permaneceram inalterados.
- Gates: `4/4` testes focais D6, `141/141` integrações, `1/1` arquitetura
  focal, `97/97` arquitetura completa e build Release da solução com zero
  avisos/erros passaram com `--no-restore`; format/analyzers, documentação de
  `422` fontes, `811` links locais em `207` arquivos e secret scan também
  passaram.
- Incidente de validação: um comando combinado ultrapassou sua janela de
  `60 s` sem conclusão e não foi classificado como passe ou falha. O inventário
  encontrou zero helper proprietário residual; os gates foram repetidos
  separadamente e concluíram com sucesso.
- Escopo negativo: zero dependência, download, PostgreSQL, provider, dado
  operacional, campanha HM-01–HM-03, Observer, `ActivationState`, lifecycle,
  deploy, push ou pull request.
- Cleanup: os roots temporário e de retenção vazios foram removidos; zero
  processo, listener ou artefacto D6 permaneceu.
- Estado resultante: PF-OBS-1-D6 `BLOQUEADO`, PF-OBS-1 e O5 continuam sem
  aprovação, `STATE-06 INTEGRATION` permanece vigente e
  `ActivationState=None` permanece imutável.
- Evidência:
  [relatório PF-OBS-1-D6](../../docs/STATE-06-MOD-12-PF-OBS-1-D6-Post-Format-Non-Intrusive-Rebaseline-Report.md).
- Próxima condição: nova autoridade explícita para duas execuções D6
  não observadas completas; somente reprodução em ambas poderá liberar os
  braços condicionais já definidos.
- Aprovador: Bruno autorizou a tentativa física limitada; nenhum resultado,
  Human Gate, lifecycle ou ativação foi inferido.

## 2026-07-28 — Retomada PF-OBS-1-D6 bloqueada no primeiro resumo V3

- Estado anterior: PF-OBS-1-D6 `BLOQUEADO` pela falha de publicação da
  evidência, writer corrigido e testado, PF-OBS-1 e O5 sem aprovação,
  `STATE-06 INTEGRATION` e `ActivationState=None`.
- Autoridade: duas novas execuções completas do prefixo V3 exato em processos
  novos e sem instrumentação intraprocesso adicional; somente reprodução do
  excesso em ambas liberaria duas observações externas e D5 histórico como
  controle.
- Baseline: branch `main`, commit
  `8383f78699a1857856eaae785277ee39b70438c4`, worktree limpa, SDK
  `10.0.302`, zero processo, janela ou listener DB-Notifier. O host Release
  SHA-256
  `09379824363CD6FCEC1053FAB20200AA19D6F54CD3D4DA50D92B485B2EA34A63`
  foi compilado com zero avisos/erros, `--no-restore`, build servers
  desabilitados e compilação compartilhada desabilitada.
- Execução: o primeiro processo novo reteve `35/158` amostras e o primeiro dos
  quatro resumos originais. O resumo `FirstByte/Cold`, com cinco warm-ups e
  trinta medições, excedeu o coeficiente de repetibilidade inclusivo:
  `0,22923232701994542` observado contra limite `0,2`, código
  `o5r5d1.threshold.repeatability-coefficient`.
- Disposição fail-closed: o prefixo parou antes de `Cancellation/Cold`; o
  resultado não reproduz nem refuta o excesso histórico de working set. A
  segunda execução não começou, nenhuma substituição ocorreu e os braços de
  observação externa e controle D5 não foram executados.
- Evidência: relatório sanitizado de `29.778 bytes`, SHA-256
  `9E6578AE516524DC12E73B9F848303C1DDD3AF9614A53A6E2CBB95D2B3F36952`,
  retido sob
  `artifacts/pf-obs-1-d6/67b02229cf5246fb93d5230d29c494ea/`;
  identidade de host, usuário e caminho do repositório ausentes.
- Gates: build focal Release aprovado com zero avisos/erros; testes focais D6
  `4/4` e arquitetura de isolamento `1/1` aprovados. Um comando combinado
  excedeu `60 s` depois desses dois primeiros resultados e antes da
  arquitetura; inventário confirmou zero helper residual e o teste de
  arquitetura passou separadamente. Documentação de `422` fontes, `812` links
  locais em `207` arquivos, secret scan e diff também passaram.
- Cleanup: root temporário da execução removido; zero processo ou listener
  DB-Notifier permaneceu. O artefacto sanitizado retido é evidência, não
  runtime, provider, suporte ou ativação.
- Escopo negativo: zero PostgreSQL, dado/provider operacional, dependência,
  download, alteração V3, campanha HM-01–HM-03 completa, Observer,
  `ActivationState`, lifecycle, deploy, push ou pull request.
- Estado resultante: PF-OBS-1-D6, PF-OBS-1 e O5 permanecem `BLOQUEADOS`;
  `STATE-06 INTEGRATION` e `ActivationState=None` permanecem inalterados.
- Evidência proprietária:
  [relatório PF-OBS-1-D6](../../docs/STATE-06-MOD-12-PF-OBS-1-D6-Post-Format-Non-Intrusive-Rebaseline-Report.md).
- Próxima condição: investigação de repetibilidade de `FirstByte/Cold`, nova
  tentativa D6 ou mudança metodológica exige autoridade explícita separada.
- Aprovador: execução limitada autorizada por Bruno; nenhum Human Gate,
  lifecycle ou ativação inferido.

## 2026-07-28 — Proposta documental PF-OBS-1-D7 definida sem execução

- Estado anterior: PF-OBS-1-D6 `BLOQUEADO` no primeiro resumo V3,
  `FirstByte/Cold` com coeficiente `0,22923232701994542` contra `0,2`,
  `Cancellation/Cold` não alcançado, PF-OBS-1 e O5 sem aprovação,
  `STATE-06 INTEGRATION` e `ActivationState=None`.
- Autoridade: exclusivamente elaborar uma proposta documental D7, preservando
  integralmente V3 e seus limites; implementação, código, thresholds,
  dependências, PostgreSQL, providers, execução física, Observer,
  `ActivationState`, lifecycle, deploy, push e pull request proibidos.
- Baseline: branch `main`, commit
  `f556969564028862fec2001f8fb3e0076ae92f7c`, worktree limpa e shutdown
  preflight com zero processo, janela ou listener DB-Notifier.
- Proposta:
  `pfobs1-d7-first-byte-cold-repeatability-diagnostic-1.0.0`, SHA-256
  `7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF`,
  dependente dos digests V3 e D6 imutáveis.
- Desenho futuro: gate estático; três processos novos não observados com
  precondição V3, `5 + 30` amostras `FirstByte/Cold` e resumo original;
  recomputação externa apenas para integridade; duas observações adicionais
  before/after permitidas somente se pelo menos dois dos três resumos
  repetirem a falha.
- Limites metodológicos: zero captura intraprocesso, seleção, substituição,
  polling, tracing, ETW/EventPipe, debugger/profiler, prioridade, afinidade,
  power-plan ou coleta adicional. Associação externa permanece descritiva e
  não autoriza atribuição causal.
- Classificações propostas: `D7.NOT_REPRODUCED` para `0/3`,
  `D7.INTERMITTENT` para `1/3`, `D7.REPEATED` para `2/3` e
  `D7.REPEATED_CONSECUTIVELY` para `3/3`; braço externo separado em
  `0/2`, `1/2` ou `2/2`.
- Execução observada: nenhuma. Zero arquivo de código/configuração,
  dependência, build, teste, runner, processo físico, PostgreSQL, provider,
  Observer, `ActivationState`, lifecycle ou ação remota foi alterado ou
  executado.
- Gates documentais: documentação de `422` fontes comment-capable e `814`
  links locais em `208` arquivos aprovados; secret scan do worktree não
  ignorado e histórico disponível e `git diff --check` aprovados.
- Estado resultante: proposta PF-OBS-1-D7 disponível, mas não autorizada para
  implementação ou execução; PF-OBS-1-D6, PF-OBS-1 e O5 permanecem
  `BLOQUEADOS`, `STATE-06 INTEGRATION` e `ActivationState=None` inalterados.
- Evidência:
  [proposta PF-OBS-1-D7](../../docs/STATE-06-MOD-12-PF-OBS-1-D7-FirstByte-Cold-Repeatability-Diagnostic-Proposal.md).
- Próxima condição: decisão explícita separada deve citar a versão e o digest
  D7 antes de qualquer implementação ou execução.
- Aprovador: elaboração documental autorizada por Bruno; nenhuma aprovação
  técnica, Human Gate, lifecycle ou ativação inferida.

## 2026-07-28 — PF-OBS-1-D7 bloqueado por schema físico incompleto

- Estado anterior: proposta D7 disponível, PF-OBS-1-D6 `BLOQUEADO` no
  primeiro resumo V3, PF-OBS-1 e O5 sem aprovação,
  `STATE-06 INTEGRATION` e `ActivationState=None`.
- Autoridade: implementação test-only isolada, gate estático, exatamente três
  processos D7-U novos e, somente perante pelo menos duas falhas, exatamente
  dois processos D7-E com snapshots externos before/after allow-listed;
  testes, evidência sanitizada, cleanup, documentação e commit local.
- Baseline: branch `main`, commit
  `a29e2d99b9d19f9cbc0a8e7b9901dd532d09803f`, worktree limpa e shutdown
  preflight sem processo, janela, notification-area instance ou listener
  DB-Notifier.
- Contrato executado:
  `pfobs1-d7-first-byte-cold-repeatability-diagnostic-1.0.0`, SHA-256
  `7FE2D4FD524713ACE02E152210BBA411B97277012DBCF1982D6EBD07D28F60DF`,
  V3 e D6 preservados pelos seus digests imutáveis.
- Implementação: measured child sem contador D7 intraprocesso, supervisor
  como único launcher, seleção original dos primeiros `35` cenários V3,
  resumo original único, recomputação de integridade independente, writer
  atómico restrito e braço externo condicional isolado do produto.
- Gate estático inicial: digests, grupo `FirstByte/Cold` `5 + 30`, ordem,
  resumo, isolamento de `src/`, captura externa apenas no supervisor e
  ausências metodológicas passaram, mas o gate não verificava os campos
  explícitos `expectedSummaryCount` e `completedSummaryCount`.
- D7-U observado: três processos com `35/35` amostras, objeto de resumo
  aprovado, integridade binária exata e coeficientes
  `0,024460739425192193`, `0,010868664937096893` e
  `0,011026690622714398`; os três JSONs omitiram as duas contagens de resumo
  obrigatórias e são contractualmente incompletos.
- Correção posterior: os dois campos, sua validação no supervisor e sua
  regressão estrutural foram acrescentados sem alterar a evidência retida,
  substituir run ou iniciar novo processo físico.
- Classificação automática: não admitida; D7 `BLOQUEADO` pelo stop rule de
  relatório incompleto. O braço externo permaneceu proibido. Zero processo
  D7-E, snapshot externo ou controle D5 foi executado.
- Comparação: o excesso D6 `0,22923232701994542` não se repetiu nesta
  amostra. Nenhuma causa de scheduler, runtime, garbage collector, hardware
  ou carga do host foi atribuída.
- Validação: build da solução sem warning ou erro; testes focais D7 `7/7`,
  arquitetura focal `1/1`, integração completa final `148/148` e arquitetura
  completa `98/98` aprovados. A primeira suíte de integração paralela aprovou
  `146/148`; os dois testes legados O3A/O3B que competem pelo `Console.Out`
  global passaram isoladamente `1/1` e `1/1`, e a repetição integral final
  passou, classificando o incidente como interferência de concorrência
  preexistente, não regressão D7. Gates finais de formatação, documentação,
  links, secrets e diff aprovados.
- Incidentes: a primeira compilação encontrou dois diagnósticos nullable no
  parser novo e não iniciou runner; a correção passou. Um hash audit anterior
  à medição tentou usar a variável PowerShell reservada `$Host`, não produziu
  hash, root ou processo e foi repetido corretamente sem consumir ou
  substituir run.
- Evidência: execução
  `0939b6560d2c4556ad781be9d9e3b376`, três JSON finais sanitizados retidos
  localmente sob `artifacts/pf-obs-1-d7/`; hashes e tamanhos fixados no
  relatório proprietário.
- Cleanup: root temporário exato removido, zero sibling `.tmp` ou
  `.measured.json`, processo, listener ou root temporário D7 residual.
- Escopo negativo: zero alteração V3/D6/threshold, substituição de run,
  instrumentação intraprocesso, polling, tracing, ETW/EventPipe,
  debugger/profiler, dependência/download, PostgreSQL, provider/dado
  operacional, campanha HM-01–HM-03, Observer, `ActivationState`, lifecycle,
  deploy, push ou pull request.
- Estado resultante: D7, PF-OBS-1-D6, PF-OBS-1 e O5 permanecem
  `BLOQUEADOS`;
  `STATE-06 INTEGRATION` e `ActivationState=None` permanecem inalterados.
- Evidência proprietária:
  [relatório PF-OBS-1-D7](../../docs/STATE-06-MOD-12-PF-OBS-1-D7-FirstByte-Cold-Repeatability-Diagnostic-Report.md).
- Próxima condição: qualquer repetição física D7, investigação causal, novo
  diagnóstico físico ou campanha requer autoridade explícita separada.
- Aprovador: execução limitada autorizada explicitamente pelo usuário; nenhum
  Human Gate, lifecycle ou ativação inferido.

## 2026-07-28 — Proposta documental PF-OBS-1-D7-R1 congelada sem execução

- Estado anterior: PF-OBS-1-D7 `BLOQUEADO` por três relatórios físicos sem os
  campos explícitos `expectedSummaryCount` e `completedSummaryCount`;
  implementação corrigida e validada sem nova execução; PF-OBS-1-D6,
  PF-OBS-1 e O5 `BLOQUEADOS`, `STATE-06 INTEGRATION` e
  `ActivationState=None`.
- Autoridade: exclusivamente elaborar a proposta documental D7-R1 para uma
  repetição corretiva; implementação adicional, execução física, alteração de
  thresholds, dependência/download, PostgreSQL, provider/dado operacional,
  Observer, `ActivationState`, lifecycle, deploy, push e pull request
  proibidos.
- Baseline: branch `main`, commit
  `558658160282f35c60f7e4e8fa09c672a2115e6b`, worktree limpa e shutdown
  preflight com zero processo, listener ou root temporário D7.
- Proposta:
  `pfobs1-d7-r1-summary-count-contract-rerun-1.0.0`, SHA-256
  `4FD5E92E4674BF93DF102DCFC564614E7D417C400292324A76E45011AE39F5C5`,
  dependente dos digests D7, V3 e D6 inalterados.
- Preservação: os três relatórios D7 permanecem byte a byte, separados e
  excluídos de qualquer classificação R1; tamanhos `31.467`, `31.430` e
  `31.473` bytes e respectivos hashes
  `AB48D99C1688EB3FC163DBAD59D4E393739B04BF1C82AB59C9239A5B180E2C85`,
  `0087DFAA4E7E014A7C037E1887C879A4A9C5501F1228280962854ED62224066F`
  e
  `7E167AFEBBC01F8DF4551A12CFD6036F2CD2156A0D92EF73E3DD80C9212D00C2`
  reconfirmados.
- Gate futuro: um envelope e identidade próprios R1 devem provar por
  serialização UTF-8 real, round trip tipado e validação independente os
  campos camel-case e valores de contagem completos/incompletos; reflexão
  isolada é insuficiente.
- Sequência futura: após gates R1-0 e R1-1, exatamente três processos R1-U
  novos e sem substituição; somente três relatórios admissíveis entram na
  classificação. Exatamente dois R1-E ficam condicionados a pelo menos duas
  falhas R1-U admissíveis.
- Limites: V3, D6, algoritmo medido D7, threshold `0,20`, grupos, amostras,
  resumos e dois `GC.Collect` preexistentes permanecem congelados; zero
  instrumentação intraprocesso, polling, tracing, ETW/EventPipe,
  debugger/profiler, prioridade, afinidade, mudança do host ou atribuição
  causal.
- Execução observada nesta atividade: nenhuma implementação, build, teste de
  produto, processo físico, observação externa, restore, download,
  PostgreSQL, provider, runtime, Observer, `ActivationState`, lifecycle ou
  ação remota.
- Gates documentais: digest canônico recalculado e aprovado; três hashes e
  tamanhos predecessores aprovados; documentação de `425` fontes
  comment-capable, `818` links Markdown locais em `210` arquivos, secret scan
  do worktree não ignorado e histórico disponível e `git diff --check`
  aprovados.
- Estado resultante: proposta D7-R1 disponível, mas não autorizada para
  implementação ou execução; D7, D6, PF-OBS-1 e O5 permanecem `BLOQUEADOS`;
  `STATE-06 INTEGRATION` e `ActivationState=None` inalterados.
- Evidência proprietária:
  [proposta PF-OBS-1-D7-R1](../../docs/STATE-06-MOD-12-PF-OBS-1-D7-R1-Summary-Count-Contract-Rerun-Proposal.md).
- Próxima condição: uma autoridade explícita separada deve citar exatamente a
  versão e o digest D7-R1 antes de qualquer implementação, build, teste ou
  execução.
- Aprovador: elaboração documental autorizada explicitamente pelo usuário;
  nenhuma aprovação técnica, Human Gate, lifecycle ou ativação inferida.

## 2026-07-28 — Proposta documental de capacidade JOSE completa

- Estado anterior: a Server API validava JWT/JWS humano externo por OIDC e
  JWKS sob egress limitado; não havia JWE, emissão JOSE própria, ciclo de
  chaves operacional, IdP ou KMS/HSM/vault configurado.
- Autoridade: elaborar uma proposta para incluir JOSE completo na
  documentação, desenvolvimento, arquitetura e infraestrutura do projeto.
  A solicitação não autorizou implementação, dependência, migration, runtime,
  infraestrutura externa, chave, deployment ou transição.
- Definição proposta: cobertura integral e testável de JWS, JWE, JWK/JWKS,
  JWA, JWT, serializações Compact/Flattened/General, nested/detached,
  algoritmo, header, key type e lifecycle, com cada entrada aplicável
  classificada como `Adopted`, `Safely adapted`, `Rejected` ou `Scheduled`.
  Completude não significa habilitar algoritmos inseguros.
- Arquitetura: Domain e providers permanecem independentes; Application
  possuiria finalidade/perfis/ports; Infrastructure possuiria adapters de
  biblioteca e custodiantes; Server API faria composição explícita; Agent
  continuaria em mTLS; Dashboard dependeria de fluxo OIDC separado.
- Segurança: perfis mutuamente exclusivos por finalidade, allowlists,
  recusa de `none`, `RSA1_5`, URLs de chave recebidas e compression baseline;
  separação sign/verify/encrypt/decrypt; referências opacas; rotação,
  revogação, compromisso e auditoria sem token, plaintext ou chave.
- Sequência futura: oito lotes `JOSE-0` a `JOSE-7`, desde decisão documental
  e spike de biblioteca até JWS, JWK/JWKS, JWE, integração, infraestrutura,
  homologação e release. Cada lote exige autoridade e gates próprios.
- ADR: criado
  [ADR-0008](../../docs/architecture/ADR-0008-JOSE-Cryptographic-Profiles-And-Key-Lifecycle.md)
  com status `proposed`; nenhuma decisão arquitetural foi aceita por
  inferência.
- Evidência proprietária:
  [proposta STATE-06 JOSE](../../docs/STATE-06-JOSE-Complete-Capability-Proposal.md).
- Execução observada: somente documentação; nenhum source, teste,
  configuração executável, dependency, restore/download, migration, IdP,
  chave, vault/KMS/HSM, banco, serviço, runtime, deploy, push ou pull request.
- Estado resultante: `STATE-06 INTEGRATION` permanece; ADR-0008 e todos os
  lotes JOSE permanecem não autorizados para implementação; nenhuma
  homologação ou capacidade operacional foi inferida.
- Próxima condição: revisão humana desta proposta e, se aceita, autorização
  separada e limitada de `JOSE-0`.
- Aprovador: elaboração da proposta autorizada explicitamente pelo usuário;
  nenhuma aprovação técnica, Human Gate ou lifecycle inferida.

## 2026-07-28 — PF-OBS-1-D7-R1 concluído sem reprodução da falha D6

- Estado anterior: proposta D7-R1 congelada, D7 e D6 `BLOQUEADOS`,
  implementação D7 corrigida sem nova execução, PF-OBS-1 e O5
  `BLOQUEADOS`, `STATE-06 INTEGRATION` e `ActivationState=None`.
- Autoridade: implementar identidade, envelope, writer, supervisor e
  regressões R1 test-only; executar R1-0/R1-1 e, após aprovação, exatamente
  três R1-U novos; dois R1-E somente perante três relatórios admissíveis e
  pelo menos duas falhas; preservar D7, sanitizar, reter, limpar, documentar e
  criar commit local.
- Baseline: branch `main`, commit
  `d981dad9400e3932ad0a365c828eea3e7e4da562`, worktree limpa, SDK
  `10.0.302` e shutdown preflight com zero processo, listener ou root
  temporário R1.
- Contrato:
  `pfobs1-d7-r1-summary-count-contract-rerun-1.0.0`, SHA-256
  `4FD5E92E4674BF93DF102DCFC564614E7D417C400292324A76E45011AE39F5C5`;
  digests D7, V3 e D6 inalterados.
- R1-0: aprovado; proposta, baseline, zero code drift desde a correção e os
  três tamanhos/hashes D7 predecessores foram comprovados antes de editar.
- Implementação: identidade/envelope R1 próprios, raw UTF-8 count validation,
  round trip tipado, validator D7 reutilizado, writer atómico, markers medido
  e supervisor, classificadores e regressões isolados em testes; zero
  composição normal.
- R1-1: aprovado; shapes completo/incompleto, nomes e valores JSON reais,
  missing/duplicate/string/negative/excessive/inconsistent, paths, publicação,
  argumentos, blocked external, isolamento e hashes passaram. Algoritmo D7 e
  driver V3 mantiveram SHA-256
  `A9DA5EEB6768E28189FCF4E0B7A3897FFA46F295C579B870E261681A588F3CEC`
  e
  `BC2134B417AD57286AF7A2E2D000779C77508BBAC2F204F227B16A2FED26EAC2`.
- Execução R1-U:
  `164c0af6ff7a4e0caac7fd9200979e1c`, host SHA-256
  `B8CC33E3AAD30EA9E2914DF33C2823A7BBE4EFC52F4281A9678C6D3E9D1FED64`;
  três processos novos com envelopes admissíveis, `35/35` amostras, `1/1`
  resumo e coeficientes `0,02148452314834982`, `0,0096634992433091` e
  `0,012714703648847817`.
- Classificação automática: `0/3` falhas,
  `D7-R1.NOT_REPRODUCED`; R1-E permaneceu proibido. Zero processo externo,
  snapshot, D6, controle D5 ou HM-01–HM-03 foi executado.
- Evidência R1: três JSONs sanitizados com `31.902`, `31.659` e `31.609`
  bytes e SHA-256
  `C333382C490CD8095EDCE4BBB5A8068D0B463DA9EE2DE82F4B3676989D313C35`,
  `0498D268B14515EAE1C54A0F43D9CFC047F1FF1B95F5EC20D84E482D6A858F46`
  e
  `FE4351BEBF18B171674E642F267216AC45322CF7B1CA367F3F79625CDD32EDF3`
  retidos separadamente; os três predecessores D7 foram reverificados
  byte a byte depois da execução.
- Validação: build da solução sem warning/erro, focal R1 `8/8`, arquitetura
  focal `1/1`, integração completa final `156/156`, arquitetura completa
  `99/99`, format, documentação de `427` fontes comment-capable, `820` links
  locais em `211` arquivos, secrets e diff aprovados.
- Incidentes não físicos: primeiro focal `5/7` por dois fixtures sintéticos
  sem work-rate, corrigidos antes de qualquer run; primeiro full
  `150/155` pela corrida legada de `Console.Out`, cinco testes aprovados
  isoladamente e passes completos posteriores `155/155` e `156/156`.
  Duas tentativas de remoção foram recusadas pela política antes de executar;
  uma checagem read-only intermediária terminou sem resultado e sem mutação;
  a validação completa foi repetida e o root exato removido via API .NET. Uma
  verificação read-only posterior omitiu espaços no cmdlet de listeners,
  emitiu erro não terminante e foi repetida corretamente sem mutação.
- Cleanup: zero sibling `.tmp`/`.measured.json`, processo, listener ou root
  temporário R1; artefactos D7 e R1 retidos permanecem separados.
- Escopo negativo: zero reparo/substituição D7, alteração V3/D6/algoritmo
  medido D7/threshold, instrumentação intraprocesso, observação externa,
  dependência/restore/download, PostgreSQL, provider/dado operacional, D6,
  controles D5, HM-01–HM-03, Observer, `ActivationState`, lifecycle, deploy,
  push ou pull request.
- Estado resultante: D7-R1 concluído como `D7-R1.NOT_REPRODUCED`; D7, D6,
  PF-OBS-1 e O5 permanecem `BLOQUEADOS`; `STATE-06 INTEGRATION` e
  `ActivationState=None` permanecem inalterados.
- Evidência proprietária:
  [relatório PF-OBS-1-D7-R1](../../docs/STATE-06-MOD-12-PF-OBS-1-D7-R1-Summary-Count-Contract-Rerun-Report.md).
- Próxima condição: qualquer Human Gate, retomada D6/PF-OBS-1 ou novo
  diagnóstico requer autoridade explícita separada.
- Aprovador: execução limitada autorizada explicitamente pelo usuário; nenhum
  Human Gate, lifecycle ou ativação inferido.

## 2026-07-28 — Human Gate PF-OBS-1-D7-R1 aprovado com ressalvas

- Estado anterior: PF-OBS-1-D7-R1 tecnicamente concluído como
  `D7-R1.NOT_REPRODUCED`, com Human Gate pendente; D7, D6, PF-OBS-1 e O5
  `BLOQUEADOS`, `STATE-06 INTEGRATION` e `ActivationState=None`.
- Evidência revista:
  [relatório automático PF-OBS-1-D7-R1](../../docs/STATE-06-MOD-12-PF-OBS-1-D7-R1-Summary-Count-Contract-Rerun-Report.md),
  implementado no commit
  `deef15122cd42989f87debbfdb2a8789bb056279`.
- Resultado automático aceito: três relatórios R1-U admissíveis, `35/35`
  amostras e `1/1` resumo em cada processo, `0/3` falhas exatas e
  `D7-R1.NOT_REPRODUCED`; o braço R1-E permaneceu proibido.
- Amostra humana: nenhuma amostra adicional estava prevista pelo protocolo
  D7-R1; nenhuma amostra física foi inferida ou acrescentada no registro.
- Decisão: Human Gate PF-OBS-1-D7-R1 `APROVADO COM RESSALVAS`.
- Ressalvas reconhecidas: D7 e D6 permanecem historicamente bloqueados;
  nenhuma causa foi atribuída; PF-OBS-1, O5, Observer, `ActivationState` e
  lifecycle não foram aprovados.
- Escopo negativo: a decisão não reclassifica D7/D6, não autoriza novo
  diagnóstico, D6, controles D5, HM-01–HM-03, PostgreSQL, provider, dado
  operacional, Observer, ativação, lifecycle, runtime, deploy, publicação,
  push ou pull request.
- Gates documentais: documentação aprovada para `427` fontes
  comment-capable; `824` links Markdown locais em `212` arquivos aprovados;
  secret scan do worktree não ignorado e do histórico Git disponível e
  `git diff --check` aprovados. Build, testes e runtime de produto não foram
  repetidos porque este registro altera somente documentação.
- Alteração executada: somente documentação; nenhum source, teste, runtime,
  configuração executável, dependência ou evidência retida foi alterado.
- Estado resultante: D7-R1 permanece tecnicamente
  `D7-R1.NOT_REPRODUCED` e está humanamente `APROVADO COM RESSALVAS`; D7, D6,
  PF-OBS-1 e O5 permanecem `BLOQUEADOS`; `STATE-06 INTEGRATION` e
  `ActivationState=None` permanecem inalterados.
- Evidência proprietária:
  [relatório do Human Gate PF-OBS-1-D7-R1](../../docs/STATE-06-MOD-12-PF-OBS-1-D7-R1-Human-Gate-Report.md).
- Próxima condição: qualquer diagnóstico, campanha física, continuação
  PF-OBS-1, ativação Observer ou transição de lifecycle requer autoridade
  explícita separada e os gates aplicáveis.
- Aprovador: Bruno, por decisão explícita em 2026-07-28.

## 2026-07-28 — Proposta documental PF-OBS-1-D8 reconciliada sem execução

- Estado anterior: D7-R1 `D7-R1.NOT_REPRODUCED` e humanamente
  `APROVADO COM RESSALVAS`; D5, D6, D7, PF-OBS-1 e O5 permaneciam
  `BLOQUEADOS`, `STATE-06 INTEGRATION` e `ActivationState=None`.
- Autoridade: exclusivamente reconciliar documentalmente D5, D6, D7 e D7-R1
  e definir pergunta diagnóstica, admissibilidade, stop rules e árvore de
  decisão D8. Implementação, execução física, V3/thresholds,
  dependência/download, PostgreSQL, provider/dado operacional, Observer,
  `ActivationState`, lifecycle, deploy, push e pull request permaneceram
  proibidos.
- Baseline: branch `main`, commit
  `1b6066893296e5e2bd4e37b34bfba4472eed6ede`, worktree limpa, SDK
  `10.0.302` e shutdown preflight com zero processo ou listener próprio.
- Reconciliação: D5 alcançou o prefixo com capturas adicionais, mas não
  reproduziu o excesso; D6 removeu essas capturas, porém parou em
  `FirstByte/Cold` antes de `Cancellation/Cold`; D7 observou três coeficientes
  baixos em evidência contractualmente incompleta; D7-R1 classificou
  validamente `0/3` falhas como `D7-R1.NOT_REPRODUCED`.
- Lacuna isolada: nenhum processo pós-formatação, não observado e
  contractualmente admissível completou o prefixo V3 exato até
  `Cancellation/Cold` measured repetition 13.
- Proposta:
  `pfobs1-d8-exact-prefix-completion-reconciliation-1.0.0`, SHA-256
  `208DA70A8D638E50E2951DECDA83414B9E1F7CF4AFF957D09C1EAA2A8B1B8814`.
- Sequência futura proposta: gates D8-0/D8-1 e exatamente duas tentativas
  D8-U novas, não observadas, com até `158` amostras e quatro resumos V3. Um
  valid-early-stop na primeira tentativa ainda permite a segunda
  pré-registada; um relatório inválido bloqueia a sequência sem substituição.
- Árvore futura: evidência inválida bloqueia interpretação; early stops
  admissíveis distinguem intermittence, repetição ou divergência; somente
  dois resultados target-eligible — prefixo completo sem excesso ou target
  stop exato em `Cancellation/Cold` — classificam working set como
  `0/2 NOT_REPRODUCED`, `1/2 INTERMITTENT` ou `2/2 REPEATED`.
- Preservação: as 15 evidências locais D5/D6/D7/D7-R1 e os seis documentos
  proprietários foram encontrados com tamanhos e hashes exatos. Nenhuma
  evidência histórica foi alterada, renomeada, regenerada, substituída ou
  contada como D8.
- Execução observada nesta atividade: nenhuma implementação, build, teste de
  produto, processo físico, observação externa, controle D5, HM-01–HM-03,
  restore, download, PostgreSQL, provider, runtime, Observer,
  `ActivationState`, lifecycle ou ação remota.
- Gates documentais: digest canônico D8 aprovado; 15 evidências predecessoras
  e seis documentos proprietários conferidos; documentação aprovada para
  `427` fontes comment-capable; `832` links Markdown locais em `213` arquivos,
  secret scan do worktree não ignorado e do histórico Git disponível e
  `git diff --check` aprovados.
- Estado resultante: proposta D8 disponível, mas não autorizada para
  implementação ou execução; D5, D6, D7, PF-OBS-1 e O5 permanecem
  `BLOQUEADOS`; D7-R1 permanece `D7-R1.NOT_REPRODUCED` e humanamente
  `APROVADO COM RESSALVAS`; `STATE-06 INTEGRATION` e
  `ActivationState=None` permanecem inalterados.
- Evidência proprietária:
  [proposta PF-OBS-1-D8](../../docs/STATE-06-MOD-12-PF-OBS-1-D8-Exact-Prefix-Completion-Reconciliation-Proposal.md).
- Próxima condição: implementação, gates estáticos ou processos D8 exigem
  autorização explícita separada que cite a versão e o digest exatos.
- Aprovador: elaboração documental autorizada explicitamente pelo usuário;
  nenhuma aprovação técnica, Human Gate, lifecycle ou ativação inferida.

## 2026-07-28 — PF-OBS-1-D8 concluído com early gate intermitente

- Estado anterior: proposta D8 congelada e autorizada para implementação e
  exatamente duas tentativas; D5, D6, D7, PF-OBS-1 e O5 `BLOQUEADOS`,
  D7-R1 `D7-R1.NOT_REPRODUCED`, `STATE-06 INTEGRATION` e
  `ActivationState=None`.
- Autoridade: implementar e executar exclusivamente a proposta
  `pfobs1-d8-exact-prefix-completion-reconciliation-1.0.0`, SHA-256
  `208DA70A8D638E50E2951DECDA83414B9E1F7CF4AFF957D09C1EAA2A8B1B8814`,
  com gates D8-0/D8-1 e duas tentativas D8-U fixas, sem substituição.
- D8-0: aprovado; proposta, identidades V3/D5/D6/D7/D7-R1, seis documentos
  proprietários e 15 JSONs predecessores conferidos antes de editar; zero
  processo, listener ou root D8.
- Implementação: identidade, envelope, writer atómico, measured child,
  supervisor, validador independente de prefixo/stop e classificadores
  isolados em testes; normal `src/` permaneceu sem D8.
- D8-1: aprovado; build Release sem restore, prefixo `158/4`, quatro early
  stops, target stop, campos JSON reais, falhas de schema, limites, paths,
  argumentos, atomicidade, cleanup e isolamento passaram. Testes focais
  `24/24`, integração completa `168/168`, arquitetura focal `17/17`,
  arquitetura completa `100/100` e documentação de 429 fontes passaram.
- Execução D8-U:
  `858686567bea41298adf05465f17c850`, host SHA-256
  `D603A2B181903B54A928CAAE92F5F4554B8415FFDA195242BD814ECB7D6352C1`.
  Run 1 foi admissível como `TargetStop`, com `150/158` amostras, `4/4`
  resumos e `Cancellation/Cold` measured repetition 5 em `2.797.568` contra
  `786.432 bytes`. Run 2 foi admissível como `EarlyGateStop`, com `35/158`
  amostras, `1/4` resumos e coeficiente `FirstByte/Cold`
  `0,20075118542700426` contra `0,2`.
- Classificação automática: uma tentativa target-eligible e uma falha exata
  pre-target produzem `D8.EARLY_GATE_INTERMITTENT`. A árvore não classifica
  recorrência de working set.
- Evidência: relatórios de `125.856` e `32.546` bytes, SHA-256
  `3052C870635DC511014778F070803E775F28CAD31131DD0FB422BF22122D992A` e
  `377785DF4D703B3C67FC72D8017B20DF2F188866FB1EEF5315D090E842F926D5`,
  retidos separadamente; os 15 predecessores foram reverificados sem mudança.
- Incidente não físico: o preflight embutido antes da segunda tentativa
  contou o próprio comando PowerShell como possível resíduo e parou antes do
  launch. A inspeção separada por PID, caminho e parentagem mostrou zero
  resíduo e destino run 2 ausente; a única tentativa run 2 foi então
  executada. Nenhuma tentativa foi substituída.
- Validação final: format aprovado; documentação de 429 fontes, 839 links
  locais em 216 arquivos, secret scan e diff aprovados. Uma falha de
  compilação e uma asserção focal sintética foram corrigidas antes dos
  processos físicos; os gates completos posteriores passaram.
- Entrega concorrente: o commit JOSE `ef6b28a` consumiu o índice compartilhado
  que já continha os arquivos D8; o índice local obsoleto de `d045864`
  registrou mudanças inversas, e `2655f98` restaurou os mesmos arquivos D8
  validados. Nenhum amend, rebase ou rewrite foi executado; a árvore líquida,
  os testes e a evidência permaneceram corretos.
- Cleanup: root temporário, siblings `.tmp`/`.measured.json`, processos e
  listeners D8 zerados após retenção.
- Escopo negativo: zero alteração V3/D5/D6/D7/D7-R1, workload, failure ou
  threshold; zero instrumentação intraprocesso, observação externa, controle
  D5, HM-01–HM-03, dependência/restore/download, PostgreSQL, provider/dado
  operacional, Observer, `ActivationState`, lifecycle, deploy, push ou pull
  request.
- Estado resultante: D8 tecnicamente
  `D8.EARLY_GATE_INTERMITTENT`; PF-OBS-1 e O5 permanecem `BLOQUEADOS`;
  `STATE-06 INTEGRATION` e `ActivationState=None` permanecem inalterados.
- Evidência proprietária:
  [relatório PF-OBS-1-D8](../../docs/STATE-06-MOD-12-PF-OBS-1-D8-Exact-Prefix-Completion-Reconciliation-Report.md).
- Próxima condição: Human Gate D8 limitado a aceitar ou rejeitar o relatório
  automático com ressalvas explícitas. Qualquer novo diagnóstico físico ou
  causal requer proposta e autoridade separadas.
- Aprovador: implementação e execução limitadas autorizadas explicitamente
  pelo usuário; nenhum Human Gate, lifecycle ou ativação inferido.

## 2026-07-28 — Human Gate PF-OBS-1-D8 aprovado com ressalvas

- Estado anterior: PF-OBS-1-D8 tecnicamente concluído como
  `D8.EARLY_GATE_INTERMITTENT`, com Human Gate pendente; recorrência do
  working set não classificada, PF-OBS-1 e O5 `BLOQUEADOS`,
  `STATE-06 INTEGRATION` e `ActivationState=None`.
- Evidência revista:
  [relatório automático PF-OBS-1-D8](../../docs/STATE-06-MOD-12-PF-OBS-1-D8-Exact-Prefix-Completion-Reconciliation-Report.md),
  commit final `f7fe56196b97dda6b378a1cf56babf7168b480c2`, `10.792` bytes e
  SHA-256
  `64A21CF0CA314C3228632C1AE075C13B1E900C094D537355B1E025A77E024046`.
- Resultado automático aceito: run 1 admissível como `TargetStop` em
  `150/158` amostras e `4/4` resumos; run 2 admissível como
  `EarlyGateStop` em `35/158` amostras e `1/4` resumo; classificação
  `D8.EARLY_GATE_INTERMITTENT`.
- Decisão humana exata: `HUMAN GATE DO PF-OBS-1-D8: APROVADO COM RESSALVAS
  — revisei o relatório automático PF-OBS-1-D8 e aceito a classificação
  D8.EARLY_GATE_INTERMITTENT. Reconheço que a recorrência do working set não
  foi classificada, nenhuma causa foi atribuída e PF-OBS-1, O5, Observer,
  ActivationState e lifecycle não foram aprovados.`
- Decisão: Human Gate PF-OBS-1-D8 `APROVADO COM RESSALVAS`.
- Ressalvas preservadas: a recorrência do working set permanece não
  classificada; nenhuma causa foi atribuída; D5, D6, D7 e D7-R1 não foram
  reclassificados; PF-OBS-1, O5, Observer, `ActivationState` e lifecycle não
  foram aprovados.
- Escopo negativo: a decisão não autoriza novo diagnóstico, execução física,
  observação externa, controle D5, HM-01–HM-03, dependência/restore/download,
  PostgreSQL, provider/dado operacional, runtime, Observer, ativação,
  lifecycle, deploy, publicação, push ou pull request.
- Alteração executada: somente documentação; zero source, teste, runtime,
  configuração executável, dependência ou evidência retida alterada.
- Gates documentais: shutdown preflight, integridade do relatório automático,
  documentação de 429 fontes, 843 links locais em 217 arquivos, secret scan
  e diff aprovados. Build, testes e runtime não foram repetidos porque o
  registro é exclusivamente documental.
- Estado resultante: D8 permanece tecnicamente
  `D8.EARLY_GATE_INTERMITTENT` e está humanamente
  `APROVADO COM RESSALVAS`; PF-OBS-1 e O5 permanecem `BLOQUEADOS`;
  `STATE-06 INTEGRATION` e `ActivationState=None` permanecem inalterados.
- Evidência proprietária:
  [relatório do Human Gate PF-OBS-1-D8](../../docs/STATE-06-MOD-12-PF-OBS-1-D8-Human-Gate-Report.md).
- Próxima condição: se investigação adicional for desejada, elaborar
  documentalmente uma proposta D9 separadamente autorizada, limitada à
  intermitência `FirstByte/Cold` observada em D8, antes de implementação ou
  execução física.
- Aprovador: Bruno, por decisão explícita em 2026-07-28.

## 2026-07-28 — Proposta JOSE revisada para a versão 1.1.0

- Estado anterior: a proposta JOSE inicial e ADR-0008 estavam documentados,
  com o ADR `proposed`, sem lote, dependência, runtime, infraestrutura ou
  homologação JOSE adicional autorizados.
- Autoridade: solicitação explícita do usuário para revisar a proposta; a
  solicitação não aprovou `JOSE-0`, ADR, spike, implementação, IdP,
  chave/certificado, vault/KMS/HSM, migration, serviço externo, deploy,
  lifecycle ou MOD-12.
- Baseline técnica imediatamente anterior:
  `46746c72983888c27d741b25775a21adebc29ccb`.
- Revisão normativa: núcleo/extensões/perfis foram separados; RFCs
  8037/8812/9864/9964, RFC 9700 aplicável, RFC 9068 condicional e snapshots
  IANA JOSE/JWT Claims entraram na cobertura. `AKP`/ML-DSA foram reconhecidos
  como normativos e permanecem `Scheduled + RuntimeDisabled`; material
  private/symmetric, mesmo publicado em RFC, continua proibido em testes,
  repositório e evidência.
- Revisão arquitetural: JOSE foi confinado ao boundary Security/Identity;
  Domain/Application não possuem formato, algoritmo, key descriptor ou tipo
  IdentityModel. Portas externas são semânticas por caso, sem endpoint
  genérico de sign/decrypt. ADRs 0002/0003/0004/0005/0007 e seus módulos
  permanecem autoridades próprias.
- Revisão de identidade, chaves e infraestrutura: identidade humana
  `(issuer, subject)`, BFF/SPA, access token/ID token, RBAC, sessão/revogação,
  CEKs efêmeras, cerimônias separadas de assinatura/criptografia, authority
  denial-dominant de key lifecycle, JWKS bounded e novos egress consumers
  candidatos foram especificados sem criar configuração operacional.
- Revisão de dados/governança: profile snapshot imutável, ativação monotônica,
  matriz de handling, audit MOD-11 canônico, IDs `JOSE-REQ-*`/`JOSE-T*`,
  facts separados de roadmap/implementação/homologação/runtime/suporte e claim
  público somente após `STATE-08` foram incorporados.
- Sequência corrigida:
  `JOSE-0 → JOSE-1 → JOSE-D1 → {JOSE-2, JOSE-3A/3B} → JOSE-4 → JOSE-5 → JOSE-6 → JOSE-7`.
  `JOSE-0` prepara a decisão, `JOSE-1` mede um spike test-only autorizado
  separadamente e somente `JOSE-D1` pode aceitar ADR-0008; esse checkpoint não
  é Human Gate de lifecycle.
- ADR: ADR-0008 foi revisto para revision `1.1` e permanece `proposed`.
- Revisões independentes: segurança, arquitetura e delivery foram verificadas
  em passes somente leitura; os achados foram incorporados sem execução ou
  mutação externa.
- Execução observada: somente Markdown; zero source, configuração executável,
  dependency/restore/download, migration, IdP, chave, vault/KMS/HSM, banco,
  serviço, runtime, deploy, push ou pull request.
- Gates finais: links Markdown, documentação de código, secret scan e diff
  aprovados. Build e testes de produto não foram executados porque o escopo é
  exclusivamente documental.
- Estado resultante: `STATE-06 INTEGRATION` permanece; ADR-0008, `JOSE-0`,
  `JOSE-1`, `JOSE-D1` e todos os lotes de implementação permanecem não
  autorizados; nenhuma capacidade operacional foi inferida e nenhum claim foi
  autorizado.
- Evidência proprietária:
  [proposta JOSE revisada](../../docs/STATE-06-JOSE-Complete-Capability-Proposal.md)
  e
  [ADR-0008 revision 1.1](../../docs/architecture/ADR-0008-JOSE-Cryptographic-Profiles-And-Key-Lifecycle.md).
- Próxima condição: revisão humana desta proposta e, se aceita, autorização
  separada e limitada de `JOSE-0` usando o texto nela fornecido.
- Aprovador: revisão documental autorizada explicitamente pelo usuário;
  nenhuma decisão técnica, Human Gate ou progressão de lifecycle inferida.

## 2026-07-28 — JOSE-0 Architecture, Security and Coverage Design preparado

- Estado anterior: proposta JOSE `1.1.0` e ADR-0008 revision `1.1`
  documentados, com ADR `proposed`; `JOSE-0`, `JOSE-1`, `JOSE-D1`,
  implementação e infraestrutura não autorizados.
- Autoridade: autorização explícita e exclusiva para o lote documental
  `STATE-06 JOSE-0`, limitado a preparar sem aceitar ADR-0008; definir
  profiles, targets e safety caps provisórios; capturar/classificar os
  snapshots IANA JOSE/JWT Claims; produzir maps
  ownership/trust/data/egress, threat model `JOSE-T*`, matriz
  `JOSE-REQ-*` e planos de teste/migração/rollback. Código, dependências,
  migrations, runtime, IdP/login real, chaves, vault/KMS/HSM, serviços
  externos operacionais, deploy, lifecycle, `JOSE-1` e MOD-12 permaneceram
  proibidos.
- Preflight: zero processo, janela, listener, runtime, Dashboard/browser
  dedicado pertencente ao DB-Notifier; worktree limpo e baseline
  `6d0bbdc68c5c080d11fccd7a6fcd540908440903`. Nenhum database engine,
  browser comum, IDE ou processo alheio foi encerrado.
- Proposta/decisão: proposta revista para `1.2.0`; ADR-0008 revision `1.2`
  permanece `proposed`, sem aceite. Targets/caps são hipóteses mensuráveis;
  somente um futuro `JOSE-D1`, depois de `JOSE-1` separadamente autorizado,
  poderá congelar valores.
- Profiles: cinco candidatos — human access token inbound, JWS core, JWS
  detached, JWE core e nested JWT conformance — todos `NotImplemented`,
  `NotTested`, `NotHomologated`, `RuntimeDisabled` e `NotAdvertised`.
- Cobertura: os nove registries IANA JOSE com `Last Updated 2026-05-22`
  produziram `155/155 = 33 Adopted + 44 SafelyAdapted + 78 Rejected`; JWT
  Claims com `Last Updated 2026-07-20` produziu
  `163/163 = 8 + 14 + 141`. Total `318/318 = 41 + 58 + 219`, zero
  `Unreviewed`, duplicado, ausente ou extra.
- Evidência: dez corpos CSV oficiais exatos foram preservados num ZIP
  reversível codificado em Base64, `10.146` bytes, SHA-256
  `f3fcf5a876beb5e06b5d00e7a4affcf74f036d3826539e4f8c5a89e2177d22ad`;
  os dez hashes de entry foram confirmados. Datas/horas de entry são campos
  ZIP/DOS sem offset UTC; os hashes, não uma projeção timezone, definem a
  identidade do archive. A única comunicação externa foi leitura HTTPS das
  fontes públicas IANA/RFC necessária ao lote documental; nenhum consumer ou
  serviço operacional foi registrado ou integrado.
- Arquitetura/segurança: ownership, trust, data e egress maps, key lifecycle,
  cinco profiles, limites, `JOSE-T01`–`JOSE-T17` e
  `JOSE-REQ-001`–`JOSE-REQ-012` foram preparados. Os 14 Test IDs planejados
  ligam-se integralmente à matriz; testes de spike/produto não foram
  executados.
- Lacunas atuais preservadas: o relying party JWT/JWKS não declara
  allowlists explícitas de algoritmo/tipo; identidade e unicidade persistida
  usam somente `subject`; audit histórico usa `ActorId` textual
  subject-derived. A migração candidata não infere issuer, não reescreve
  audit append-only e proíbe fallback subject-only depois do cutover
  composto/multi-issuer.
- Egress: as quatro policies atuais permanecem intactas.
  `human-identity-backchannel`, `server-key-custody` e
  `server-workload-identity` são somente candidatos não compilados,
  configurados ou autorizados; nenhuma identidade, credencial, endpoint ou
  infraestrutura foi selecionada.
- Revisões independentes: inventários JOSE/JWT Claims, arquitetura/autoridade
  e safety caps foram revistos somente leitura. Correções trataram
  classificação `SafelyAdapted`, audit/migração, caps por objeto, credencial
  workload, timestamp ZIP/DOS, egress e rastreabilidade; os rechecks finais
  encontraram zero P0 e zero P1.
- Escopo negativo: zero source, configuração executável, dependency,
  migration, schema, runtime, IdP/login, material de chave/certificado,
  vault/KMS/HSM/STS, serviço operacional, deploy, publicação, lifecycle,
  `JOSE-1`, `JOSE-D1`, decisão ADR ou mudança/ativação MOD-12. Decision
  packets foram excluídos porque a autorização final não os incluiu.
- Gates documentais: archive e 10/10 entry hashes; cobertura mecânica
  `318/318`; 12/12 requisitos, 17/17 threats e 14/14 Test IDs; 886 links
  locais em 221 arquivos; documentação de 429 fontes; secret scan e diff
  aprovados. Build, testes de produto e runtime não foram executados porque
  o lote é exclusivamente documental.
- Estado resultante: `STATE-06 INTEGRATION` e
  `MOD-12 ActivationState=None` permanecem inalterados; pacote `JOSE-0`
  preparado para revisão humana, sem aceitar ADR-0008 nem conceder qualquer
  autoridade posterior.
- Evidência proprietária:
  [relatório JOSE-0](../../docs/STATE-06-JOSE-0-Architecture-Security-And-Coverage-Design-Report.md),
  [profile técnico](../../docs/architecture/JOSE-Security-Profile-And-Key-Lifecycle.md),
  [matriz IANA](../../docs/architecture/JOSE-IANA-Registry-Coverage.md),
  [proposta `1.2.0`](../../docs/STATE-06-JOSE-Complete-Capability-Proposal.md)
  e
  [ADR-0008 revision `1.2`](../../docs/architecture/ADR-0008-JOSE-Cryptographic-Profiles-And-Key-Lifecycle.md).
- Próxima condição: revisão humana do pacote apenas para aceitá-lo como
  preparação documental ou devolvê-lo com ressalvas. `JOSE-1`, `JOSE-D1`,
  qualquer implementação/infraestrutura e qualquer lifecycle continuam a
  exigir autoridade explícita separada.
- Aprovador: elaboração `JOSE-0` autorizada explicitamente pelo usuário;
  nenhuma aprovação técnica, Human Gate, decisão ADR, lifecycle ou ativação
  inferida.

## 2026-07-28 — Pacote JOSE-0 aceito somente como preparação documental

- Estado anterior: pacote `JOSE-0` tecnicamente concluído, gates documentais
  aprovados e revisão humana pendente; ADR-0008 revision `1.2` permanecia
  `proposed`; `JOSE-1`, implementação, infraestrutura, lifecycle e MOD-12
  não autorizados.
- Decisão humana exata:
  `REVISEI o pacote STATE-06 JOSE-0 e ACEITO-O exclusivamente como preparação
  documental. Esta decisão não aceita o ADR-0008, não autoriza JOSE-1, código,
  dependências, migrations, runtime, IdP/login, chaves, custódia,
  infraestrutura, deploy, lifecycle ou ativação MOD-12.`
- Decisão: pacote `JOSE-0` `ACEITO EXCLUSIVAMENTE COMO PREPARAÇÃO
  DOCUMENTAL`. O aceite não é `JOSE-D1`, não aceita proposta/ADR, não torna
  profile, cap, algoritmo, threat, egress candidate, migration ou plano
  normativo e não concede autoridade posterior.
- Preflight do registro: baseline
  `7cbcd0cec4e35715cf9d3e9f4567b2d2c2915f55`, worktree limpa e zero
  processo, janela ou listener pertencente ao DB-Notifier. Nenhum runtime,
  database engine, browser comum, IDE ou processo alheio foi encerrado.
- Alteração executada: somente status e histórico documental do pacote,
  proposta, ADR, profile técnico, índice arquitetural e estado factual.
  Inventários, hashes, classificações, requisitos, threats, caps e planos
  técnicos permaneceram inalterados.
- Gates do registro: 888 links locais em 221 arquivos, documentação de 429
  fontes, secret scan, diff e staged diff aprovados. Build, testes de produto
  e runtime não foram executados porque a alteração é exclusivamente
  documental.
- Escopo negativo: zero source, configuração executável, dependência,
  migration/schema, runtime, IdP/login, material de chave/certificado,
  custódia/vault/KMS/HSM/STS, serviço operacional, infraestrutura, deploy,
  publicação, lifecycle, `JOSE-1`, `JOSE-D1`, aceite ADR ou mudança/ativação
  MOD-12.
- Estado resultante: `STATE-06 INTEGRATION`,
  `MOD-12 ActivationState=None` e ADR-0008 `proposed` permanecem
  inalterados. A revisão humana do pacote preparatório está concluída;
  nenhuma etapa posterior foi iniciada.
- Evidência proprietária:
  [relatório JOSE-0](../../docs/STATE-06-JOSE-0-Architecture-Security-And-Coverage-Design-Report.md)
  e
  [ADR-0008](../../docs/architecture/ADR-0008-JOSE-Cryptographic-Profiles-And-Key-Lifecycle.md).
- Próxima condição: `JOSE-1` permanece sem autorização. Qualquer interesse
  futuro no spike, em `JOSE-D1`, implementação, infraestrutura ou lifecycle
  exige nova autoridade explícita e separada.
- Aprovador: Bruno, por decisão explícita em 2026-07-28, limitada à
  preparação documental.

## 2026-07-29 — Baseline 9512dc1 reconciliada e nova proposta de revalidação preparada

- Estado anterior: `STATE-06 INTEGRATION`, proposta histórica de transição
  `INVALIDADA PARA EXECUÇÃO`, elegibilidade corrente não reavaliada,
  `MOD-12 ActivationState=None` e ADR-0008 `proposed`.
- Autoridade: lote exclusivamente documental autorizado para reconciliar o
  estado factual com `9512dc1`, inventariar mudanças posteriores aos gates e
  elaborar uma nova proposta `STATE-06 → STATE-07` com matriz de revalidação
  proporcional. Build, testes, runtime, downloads, acesso externo, source,
  dependências, `JOSE-1`, `D9`, ativação MOD-12 e transição de lifecycle
  permaneceram proibidos.
- Preflight: zero processo, janela ou listener pertencente ao DB-Notifier;
  branch `main`, worktree limpa e baseline
  `9512dc1de15619eadd9d2e8e6b5476bb77a13abd`. Nenhum database engine,
  browser comum, IDE ou processo alheio foi encerrado.
- Ancestralidade: `84217c6`, `2c1e05f`, `1a27dca`, `96cf248`, `ff0adc7` e
  `3c13d57` foram confirmados como ancestrais de `9512dc1`. Essa cadeia
  preserva proveniência e não revalida a elegibilidade.
- Delta principal: `1a27dca..9512dc1` contém `143` commits, `440` caminhos,
  `90.922` inserções e `2.993` remoções, incluindo `126` caminhos em `src/`,
  `127` em `tests/`, `25` em `scripts/` e três mudanças de schema Server.
- Delta desde a reconciliação anterior: `ff0adc7..9512dc1` contém `24`
  commits, `41` caminhos, `11.507` inserções e `50` remoções. Não há mudança
  em `src/`, migration ou package/lockfile nesse intervalo, mas há `11`
  caminhos C# test-only, atualização do SDK por `global.json`, evolução dos
  harnesses D6–D8, JOSE-0 e governança.
- Famílias inventariadas: R0–R8; R-SEQ; R-EGRESS/R-FENCE; R-NET; migrations;
  Agent/tooling/supply chain; Web/WPF/acessibilidade; MOD-12 O1–D8; JOSE-0 e
  governança. Os gates locais permanecem válidos somente nos seus escopos e
  não se agregam automaticamente como gate de lifecycle.
- Proposta: criado um documento novo e não autorizante com `RV-01`–`RV-13`,
  `RV-H01`, novos IDs humanos `S06-RV-HG-001`–`006`, regras de reutilização,
  ressalvas, condições de parada e seis autoridades futuras independentes. A
  proposta antiga permanece histórica, inalterada e invalidada.
- Escopo documental: nova proposta, snapshot factual, entrada append-only e
  índice documental. A árvore executável `9512dc1` não foi alterada; o commit
  de entrega é somente administrativo e documental.
- Revisão: inventário Git, classificação funcional e desenho da matriz
  receberam passes independentes somente leitura. Links acrescentados tiveram
  a existência dos seus alvos confirmada e a higiene de diff foi aprovada.
- Gates executáveis: validators de documentação, code-doc gate, secret/host
  scan, build, testes e runtime `NÃO EXECUTADOS`, porque não integraram a
  autoridade limitada deste lote. Nenhum resultado técnico foi inferido.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  transição `NÃO AUTORIZADA`, `MOD-12 ActivationState=None` e ADR-0008
  `proposed` permanecem inalterados.
- Evidência proprietária:
  [proposta reconciliada](../../docs/STATE-06-To-STATE-07-Transition-Revalidation-Proposal.md)
  e [estado factual](Current-State.md).
- Próxima condição: revisão humana somente desta proposta. Se ela for aceita,
  a revalidação técnica ainda exigirá nova autoridade explícita, e as amostras
  humanas, o Human Gate de revalidação e a transição exigirão decisões
  posteriores separadas.
- Aprovador: elaboração documental autorizada explicitamente por Bruno;
  nenhuma aprovação técnica, Human Gate, ativação ou progressão de lifecycle
  inferida.

## 2026-07-29 — Fechamentos MOD-12 e JOSE definidos como pré-condições estratégicas

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  proposta reconciliada no commit `04db659`, MOD-12
  `ActivationState=None`, O5/PF-OBS sem aprovação, JOSE-0 apenas documental e
  ADR-0008 `proposed`.
- Autoridade: revisão exclusivamente documental da proposta
  `STATE-06 → STATE-07` para estabelecer como pré-condição estratégica a
  conclusão dos escopos de integração `STATE-06` de MOD-12 e JOSE. Código,
  build, testes, runtime, downloads, acesso externo, dependências, `JOSE-1`,
  `D9`, ativação MOD-12 e transição de lifecycle permaneceram não
  autorizados.
- Preflight: zero processo, listener ou janela pertencente ao DB-Notifier;
  branch `main`, worktree limpa e baseline administrativa
  `04db6594e192dec822fbd326c792eec4f3a37714`. Nenhum database engine,
  browser comum, IDE ou processo alheio foi encerrado.
- Baselines: `9512dc1` permanece a última árvore executável inventariada;
  `04db659` é seu descendente documental direto e a baseline administrativa
  imediatamente anterior a esta revisão. Nenhuma delas é a futura baseline
  técnica depois dos fechamentos.
- Diretriz estratégica: a revalidação consolidada de saída somente poderá ser
  proposta depois de concluídos e aprovados os dois escopos de integração. A
  diretriz não altera o lifecycle canônico e não autoriza os lotes que define;
  sua delimitação detalhada permanece preparada e pendente de revisão.
- MOD-12 no `STATE-06`: o fechamento proposto revalida O1–O4 e um único E2E
  product-owned Agent → Server → MOD-12 → API/UI, com adapters/activation
  guard reais exercidos por harness sintético, sem implementação paralela,
  com zero worker/I/O em `ActivationState=None`, zero LLM, recomendação,
  plano, comando ou automação.
- MOD-12 posterior: PF-OBS, O5, corpus representativo, calibração, célula
  exata, segurança/red team, carga e recuperação permanecem no handoff
  bloqueante de homologação/ativação de `STATE-07`. `D9` permanece diagnóstico
  opcional, não autorizado e não automático. Rollout por modo pertence a
  `STATE-08`.
- JOSE no `STATE-06`: o fechamento proposto percorre `JOSE-1`, `JOSE-D1`,
  `JOSE-2`, `JOSE-3A`, `JOSE-3B` quando aplicável, `JOSE-4` e os sublotes
  `JOSE-5` selecionados. `JOSE-1` mede cinco profiles candidatos e seis
  serializações; `JOSE-D1` congela o conjunto aceito, sem mínimo técnico
  pré-julgado. Cada lote conserva gate proprietário, integração bounded e
  nenhum claim operacional.
- JOSE posterior: `JOSE-6`, provisionamento não produtivo e homologação exata
  pertencem a `STATE-07`; `JOSE-7`, release e claim público pertencem a
  `STATE-08`.
- Matriz: acrescentados `PC-M12-01`–`04` e `PC-JOSE-01`–`05`; `RV-01`,
  `RV-10`, `RV-11`, critérios de elegibilidade, condições de parada,
  autoridades futuras e plano inicial de `STATE-07` foram reconciliados com
  a nova ordem.
- Escopo documental: proposta reconciliada, snapshot factual, entrada
  append-only e índice. A proposta histórica invalidada permaneceu
  inalterada.
- Revisão: boundaries MOD-12, sequência JOSE e coerência da estratégia
  receberam passes independentes somente leitura, sem edição ou execução.
- Gates executáveis: validators de documentação, code-doc gate, secret/host
  scan, build, testes e runtime `NÃO EXECUTADOS`, porque não integraram a
  autoridade limitada deste lote. Nenhum resultado técnico foi inferido.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  transição `NÃO AUTORIZADA`, `MOD-12 ActivationState=None`, O5/PF-OBS sem
  aprovação e ADR-0008 `proposed` permanecem inalterados.
- Evidência proprietária:
  [proposta reconciliada revisada](../../docs/STATE-06-To-STATE-07-Transition-Revalidation-Proposal.md)
  e [estado factual](Current-State.md).
- Próxima condição: revisão humana desta proposta revisada. Seu aceite não
  autorizará execução; um plano de fechamento MOD-12, `JOSE-1`, cada lote
  técnico, a revalidação, as amostras humanas, o Human Gate e a transição
  continuarão a exigir autoridades próprias.
- Aprovador: diretriz estratégica e elaboração documental autorizadas
  explicitamente por Bruno; nenhuma aprovação técnica, ativação, homologação
  ou progressão de lifecycle inferida.

## 2026-07-29 — Delimitação PC-M12/PC-JOSE aceita somente como diretriz documental

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  proposta revisada no commit `db62377`, delimitação detalhada
  `PC-M12`/`PC-JOSE` pendente de revisão, MOD-12 `ActivationState=None`,
  O5/PF-OBS sem aprovação e ADR-0008 `proposed`.
- Decisão humana exata:
  `REVISEI a proposta revisada STATE-06 → STATE-07 e ACEITO a delimitação
  PC-M12 e PC-JOSE exclusivamente como diretriz documental de fechamento da
  integração STATE-06. Esta decisão não autoriza código, build, testes,
  runtime, downloads, acesso externo, dependências, JOSE-1, D9, ativação
  MOD-12 nem transição de lifecycle.`
- Efeito: revisão humana da delimitação documental encerrada como `ACEITA
  EXCLUSIVAMENTE COMO DIRETRIZ DOCUMENTAL DE FECHAMENTO DA INTEGRAÇÃO
  STATE-06`. Nenhuma condição `PC-M12` ou `PC-JOSE` foi classificada como
  tecnicamente satisfeita, aprovada ou executada.
- Preflight do registro: baseline
  `db62377e5d762e7ee334e7bf0e7309a0ab5c4b9b`, branch `main`, worktree limpa
  e zero processo, listener ou janela pertencente ao DB-Notifier. Nenhum
  database engine, browser comum, IDE ou processo alheio foi encerrado.
- Alteração executada: somente status e memória factual da proposta,
  `Current-State`, histórico append-only e índice documental. A proposta
  histórica invalidada e a árvore executável `9512dc1` permaneceram
  inalteradas.
- Escopo negativo: zero source, configuração executável, dependência,
  migration/schema, build, teste, runtime, download, acesso externo,
  `JOSE-1`, `JOSE-D1`, decisão ADR, `D9`, O5/PF-OBS, ativação MOD-12,
  homologação, deploy, publicação ou transição de lifecycle.
- Verificação do registro: somente inspeções locais Git/textuais, conferência
  da whitelist documental e higiene do diff. Validators de documentação,
  secret/host scan, build, testes e runtime não foram executados porque não
  integraram a autoridade deste lote.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  transição `NÃO AUTORIZADA`, MOD-12 `ActivationState=None`, O5/PF-OBS sem
  aprovação e ADR-0008 `proposed` permanecem inalterados. A aceitação não é
  Quality Gate, Human Gate, `JOSE-D1`, ativação ou autorização executiva.
- Evidência proprietária:
  [proposta reconciliada revisada](../../docs/STATE-06-To-STATE-07-Transition-Revalidation-Proposal.md)
  e [estado factual](Current-State.md).
- Próxima condição: qualquer plano de fechamento MOD-12, `JOSE-1`, lote
  técnico, revalidação, amostra humana, Human Gate ou transição requer nova
  autoridade explícita e separada. Pela sequência aceita, o próximo passo
  recomendado é somente um plano documental de fechamento MOD-12
  `STATE-06`, também sob autorização própria.
- Aprovador: Bruno, por decisão explícita em 2026-07-29, limitada à diretriz
  documental `PC-M12`/`PC-JOSE`.

## 2026-07-29 — Plano documental de fechamento MOD-12 STATE-06 preparado

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  delimitação `PC-M12`/`PC-JOSE` aceita somente como diretriz documental,
  MOD-12 `ActivationState=None`, O5/PF-OBS sem aprovação e nenhum lote de
  fechamento autorizado.
- Autoridade humana exata:
  `AUTORIZO exclusivamente um lote documental para elaborar o plano de
  fechamento MOD-12 STATE-06 conforme PC-M12-01–04. Não autorizo código,
  build, testes, runtime, downloads, acesso externo, dependências, D9, O5,
  PF-OBS, ativação MOD-12, JOSE-1 nem transição de lifecycle.`
- Preflight: a primeira leitura limitada pelo sandbox não obteve acesso ao
  inventário CIM e não foi usada como evidência. A inspeção local completa,
  somente leitura e autorizada em seguida confirmou zero processo, listener
  ou janela pertencente ao DB-Notifier, branch `main`, worktree limpa e
  baseline `2a59548788bb6cbe6f88bbf531bd953b718165d8`. Nenhum database engine,
  browser comum, IDE ou processo alheio foi encerrado.
- Inventário factual: os commits revistos de O1, O2-A, O2-B, O3-A, O3-B,
  O4/O4-UI1, O5-R2 e O5-R3 são ancestrais da baseline. Seus relatórios e
  Human Gates permanecem proveniência nos escopos históricos, não aprovação
  corrente. A orquestração O1–O4 continua maioritariamente test-owned e não
  existe ainda o único caminho product-owned exigido por `PC-M12-02`.
- Plano: criado
  [o plano de fechamento MOD-12](../../docs/STATE-06-MOD-12-Integration-Closure-Plan.md),
  com seis lotes futuros `M12-IC1`–`M12-IC6`, ordem técnica
  `PC-M12-02 → PC-M12-03 → PC-M12-01 → PC-M12-04`, matriz de
  rastreabilidade, critérios mensuráveis, stop conditions, rollback, cleanup,
  claims e handoff.
- Autoridades: o plano prevê mínimo oito decisões futuras — revisão do plano,
  cinco autoridades `M12-IC1`–`IC5`, autoridade das amostras `M12-IC6` e
  decisão do fechamento. Remediações acrescentam autoridades; nenhuma é
  concedida por este registro.
- Escopo documental: novo plano, proposta reconciliada, snapshot factual,
  entrada append-only e índice. Nenhum relatório ou Human Gate histórico foi
  reescrito.
- Escopo negativo: zero source, teste, configuração executável, dependency,
  migration/schema, build, runtime, download, acesso externo, D9, O5,
  PF-OBS/HM, JOSE, ativação, homologação, deploy, publicação ou lifecycle.
- Revisão: boundaries O1/O2, O3/O4 e governança/autoridade receberam passes
  independentes somente leitura. Validators de documentação, secret/host
  scan, build, testes e runtime não foram executados porque não integraram a
  autoridade deste lote.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  MOD-12 `ActivationState=None`, O5 `BLOQUEADO`, PF-OBS/HM sem aprovação,
  D9 não autorizado e lifecycle inalterado. O plano está `PREPARADO` e
  `PENDENTE DE REVISÃO`; nenhum `PC-M12` foi classificado como satisfeito.
- Próxima condição: revisão humana somente do plano, para aceitá-lo como
  direção documental, ressalvá-lo, devolvê-lo ou rejeitá-lo. Mesmo um aceite
  sem ressalvas não autorizará `M12-IC1`.
- Aprovador: elaboração documental autorizada explicitamente por Bruno;
  nenhuma aprovação técnica, Quality Gate, Human Gate, ativação ou progressão
  inferida.

## 2026-07-29 — Governança de coordenação de conversas incorporada

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, corpus de instruções `6.1.0` com 14 arquivos
  ativos e nenhum workflow paralelo de escrita autorizado para o lote.
- Autoridade humana exata:
  `APROVO o plano documental apresentado e AUTORIZO exclusivamente a
  incorporação sequencial da política de coordenação de conversas nos 10
  ficheiros enumerados, incluindo os checks locais proporcionais e o commit
  local focal exigido pelo repositório. Não autorizo branches, worktrees,
  merge, rebase, push, código, build, runtime, ações externas, alteração de
  ADR, Human Gate, ActivationState ou lifecycle.`
- Observação do proprietário: prevenir e interromper erro, conflito ou falha no
  fluxo de desenvolvimento sem prometer infalibilidade absoluta.
- Preflight: zero processo e zero listener pertencente ao DB-Notifier. A
  inspeção complementar confirmou zero processo Docker, serviço Docker parado
  e endpoint do contexto ativo inexistente; nenhum processo, serviço, database
  engine, browser, IDE ou recurso alheio foi encerrado ou alterado.
- Baseline: `938c2d22b444b434363d4c87ab13c3a2b2c9d8e3`; a fonte não rastreada foi
  preservada antes da adaptação pelo SHA-256
  `0019950242314908762CAD3E2AEA01C122023E3867885289E04FB3A70CA912D4`.
- Decisão documental: incorporar `Conversation-Coordination-Prompt.md` no
  próprio local como autoridade temática única, revisão `1.0.0`, e evoluir o
  corpus para `6.2.0` com 15 arquivos ativos. `Governance.md` permanece
  proprietário da autoridade, execução controlada e lifecycle.
- Controles: roteamento e paralelismo usam enums fechados; todo handoff contém
  mensagem exata; cada path, artefato lógico e recurso mutável possui um único
  writer; overlap, baseline incerta, dependência instável, decisão pendente ou
  isolamento insuficiente acionam parada e fallback sequencial.
- Git e isolamento: Git existente e commit local final não autorizam branch,
  worktree, merge ou rebase. Sem workflow de escrita paralela especificamente
  autorizado e worktrees isolados, conversas simultâneas permanecem read-only
  e toda escrita ocorre sequencialmente na coordenadora.
- Custódia: integração, estado, histórico, changelog, ADRs, relatórios e
  decisões de gate e apresentação de Human Gates pertencem à coordenadora, sem
  lhe conceder autoridade para decidir, ativar ou promover.
- Escopo documental: `Conversation-Coordination-Prompt.md`, `AGENTS.md`,
  `prompts/Start-Here.md`, `prompts/governance/Governance.md`,
  `prompts/governance/Quality-Gates.md`,
  `prompts/templates/Templates.md`,
  `prompts/system/AI-Software-Engineering-Master-Prompt.md`,
  `prompts/system/Prompt-System-Change-Log.md`,
  `prompts/state/Current-State.md` e este log append-only.
- Gates: whitelist `10/10`, UTF-8/LF/newline final/trailing whitespace `10/10`,
  campos de handoff `14/14`, enums `3 + 3`, autoridade, versão/estado,
  placeholders reais, identificador de host e limites Git aprovados; `935`
  links locais em `224` arquivos, documentação de `429` fontes, secret scan e
  diff aprovados. A revisão semântica independente bloqueou inicialmente duas
  ambiguidades — identificação da coordenadora interna e precedência temática
  —, ambas corrigidas; o recheck confirmou os seis critérios do proprietário
  sem achado bloqueante. O primeiro staged diff continha somente os dez
  arquivos autorizados e passou `git diff --cached --check`; esta atualização
  factual foi incluída na repetição integral do gate antes do commit.
- Escopo negativo: zero branch, worktree, merge, rebase, push, source,
  configuração executável, dependência, build, teste de produto, runtime, ação
  externa, alteração ou decisão de ADR, Human Gate, ativação, homologação,
  deploy, publicação ou transição de lifecycle.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, ADRs, Human Gates, produto e autoridade
  externa permanecem inalterados.
- Evidências proprietárias:
  [autoridade de coordenação](../governance/Conversation-Coordination-Prompt.md),
  [changelog do corpus](../system/Prompt-System-Change-Log.md) e
  [estado factual](Current-State.md).
- Aprovador: Bruno, por autorização explícita limitada à incorporação
  documental sequencial; nenhuma autoridade adjacente inferida.

## 2026-07-29 — Política de idioma incorporada e autoridades movidas para governança

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, corpus de instruções `6.2.0` com 15 arquivos
  ativos, política de coordenação na raiz e política de idioma ainda não
  roteada como autoridade temática.
- Autoridade humana exata:
  `APROVO o plano revisado e AUTORIZO exclusivamente mover Language-Policy.md
  e Conversation-Coordination-Prompt.md para prompts/governance/, preservando
  seus nomes, e realizar a incorporação sequencial nos 11 documentos
  enumerados, com validações documentais locais e sem Git. Não autorizo
  tradução em massa, alteração da interface, código, compilação, execução do
  produto, ações externas, ADR, Human Gate, ActivationState ou lifecycle.`
- Esclarecimento do proprietário: toda informação, orientação, sugestão e
  demais comunicação dirigida ao proprietário deve ser apresentada em
  `pt-BR`; `en-GB` permanece aplicável aos artefatos de projeto e
  desenvolvimento conforme a autoridade temática, sem determinar o idioma da
  interface.
- Preflight: zero processo e zero listener pertencente ao DB-Notifier. A
  inspeção complementar confirmou o serviço Docker parado e o endpoint do
  contexto ativo inexistente; nenhum processo, serviço, database engine,
  browser, IDE ou recurso alheio foi encerrado ou alterado.
- Baseline documental: 12 hashes SHA-256 previamente registrados foram
  reconciliados sem Git. A fonte original `Language-Policy.md` foi preservada
  por proveniência com o SHA-256
  `E6021618DD2DED951FC08BA5305C13F35163E86623C612406CBA2239D8E97224`.
- Decisão documental: mover, preservando os nomes,
  `Language-Policy.md` e `Conversation-Coordination-Prompt.md` para
  `prompts/governance/`; adotar a primeira como autoridade temática única de
  idioma, revisão `1.0.0`; atualizar a segunda para a revisão `1.1.0`; e
  evoluir o corpus para `6.3.0` com 16 arquivos ativos.
- Política adotada: comunicação ao proprietário e mensagens prontas em
  `pt-BR`; novos artefatos autônomos de projeto em `en-GB`; alterações
  limitadas preservam o idioma estabelecido do arquivo; nomes, contratos,
  identificadores e enums externos permanecem estáveis; tradução em massa,
  reescrita histórica e inferência do idioma da interface permanecem
  proibidas sem autoridade própria.
- Contratos de coordenação: o handoff preserva exatamente 14 campos canônicos,
  sua ordem e seus enums fechados, com rótulos visíveis e mensagens padrão em
  `pt-BR`; a mensagem governada de conversa auxiliar preserva exatamente 19
  campos. As chaves técnicas canônicas em inglês permanecem entre parênteses
  ou crases quando necessárias à interoperabilidade.
- Escopo documental: 11 documentos lógicos e 13 endpoints de caminho —
  as duas autoridades movidas, `AGENTS.md`, `prompts/Start-Here.md`,
  `prompts/governance/Governance.md`,
  `prompts/governance/Quality-Gates.md`,
  `prompts/templates/Templates.md`,
  `prompts/system/AI-Software-Engineering-Master-Prompt.md`,
  `prompts/system/Prompt-System-Change-Log.md`,
  `prompts/state/Current-State.md` e este log append-only. A única adaptação em
  evidência histórica anterior foi a correção mecânica do link para a
  autoridade movida; sua prosa permaneceu inalterada.
- Gates anteriores a este registro: escopo `11/11` documentos lógicos e
  `13/13` endpoints, formato e higiene `11/11`, handoff `14/14`, mensagem
  auxiliar `19/19`, enums `3 + 3`, autoridade, roteamento, precedência,
  versão, estado, host real, padrões de segredo e limites negativos
  aprovados; `956` links locais em `225` arquivos aprovados. Uma divergência
  inicial de apresentação dos enums no template foi corrigida antes da
  repetição aprovada do contrato.
- Escopo negativo: nenhum uso de Git; zero tradução em massa, interface,
  source, configuração executável, dependência, compilação, teste ou execução
  do produto, runtime, ação externa, ADR, Human Gate, `ActivationState`,
  homologação, deploy, publicação ou transição de lifecycle.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, ADRs, Human Gates, interface, produto e
  autoridade externa permanecem inalterados.
- Evidências proprietárias:
  [política de idioma](../governance/Language-Policy.md),
  [autoridade de coordenação](../governance/Conversation-Coordination-Prompt.md),
  [changelog do corpus](../system/Prompt-System-Change-Log.md) e
  [estado factual](Current-State.md).
- Aprovador: Bruno, por autorização explícita limitada à movimentação e
  incorporação documental sequencial sem Git; nenhuma autoridade adjacente
  inferida.

## 2026-07-29 — Mensagem pronta para copiar tornada obrigatória em todo handoff

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, corpus de instruções `6.3.0` com 16 arquivos
  ativos, revisão `1.1.0` da autoridade de coordenação e baseline limpa no
  commit `d30b901a63150b9785dae43713a3259302dd5009`.
- Autoridade humana exata:
  `APROVO o plano documental apresentado e AUTORIZO exclusivamente alterar
  sequencialmente AGENTS.md,
  prompts/governance/Conversation-Coordination-Prompt.md,
  prompts/templates/Templates.md, prompts/governance/Quality-Gates.md,
  prompts/system/Prompt-System-Change-Log.md,
  prompts/state/Current-State.md e prompts/state/State-Transition-Log.md para
  tornar obrigatório que todo encerramento forneça uma mensagem completa,
  específica, em pt-BR e pronta para eu copiar e enviar na conversa indicada.
  A mensagem deverá ser fornecida mesmo quando o objetivo estiver concluído,
  parcial ou bloqueado e nunca poderá presumir ou fabricar aprovação, Human
  Gate, ADR, ActivationState, lifecycle, operação Git ou ação externa.
  AUTORIZO o preflight obrigatório, as validações documentais locais
  proporcionais e um commit local focal com a mensagem docs(governance):
  require copy-ready next messages. NÃO AUTORIZO amend, branch, worktree,
  merge, rebase, push, código, build, execução do produto, alteração da
  interface, ação externa, ADR, Human Gate, ActivationState ou lifecycle.`
- Preflight e baseline: zero processo, listener ou janela pertencente ao
  DB-Notifier; serviço Docker parado e endpoint ausente; branch `main`, árvore
  limpa e HEAD
  `d30b901a63150b9785dae43713a3259302dd5009`. Nenhum processo ou recurso alheio
  foi encerrado ou alterado.
- Decisão documental: atualizar a autoridade de coordenação para a revisão
  `1.2.0` e evoluir o corpus para `6.4.0`, mantendo 16 arquivos ativos, a
  Política de Idioma na revisão `1.0.0` e a versão-fonte `2.0.0` do Prompt
  Mestre.
- Política adotada: todo handoff concluído, parcial ou bloqueado contém uma
  única mensagem completa, específica, preenchida, em `pt-BR` e pronta para o
  proprietário copiar e enviar literalmente na conversa indicada. A obrigação
  permanece quando nenhuma ação adicional de projeto for conhecida.
- Contrato preservado: continuam exatamente 14 campos, na mesma ordem, e os
  enums fechados de roteamento e paralelismo. Somente `Exact next message`
  deixa de aceitar valor vazio, placeholder, alternativas ou
  ``Não se aplica (`None`) — nenhuma mensagem é necessária``; os usos
  governados de `None` nos campos de título e paralelismo permanecem
  inalterados.
- Limite decisório: a mensagem pronta não representa decisão antes de ser
  enviada e não presume, fabrica ou amplia aprovação, Human Gate, ADR,
  `ActivationState`, lifecycle, operação Git ou ação externa. Decisão formal
  pendente recebe pedido de apresentação ou revisão do pacote decisório, salvo
  resultado já escolhido inequivocamente pelo proprietário no contexto
  vigente.
- Escopo documental: `AGENTS.md`,
  `prompts/governance/Conversation-Coordination-Prompt.md`,
  `prompts/templates/Templates.md`,
  `prompts/governance/Quality-Gates.md`,
  `prompts/system/Prompt-System-Change-Log.md`,
  `prompts/state/Current-State.md` e este log append-only.
- Gates anteriores a este registro: escopo preliminar `6/6`, `git diff
  --check`, UTF-8/LF/newline final/trailing whitespace, contrato de handoff
  `14/14`, revisão `1.2.0`, corpus `6.4.0` e preservação dos limites decisórios
  aprovados. A validação final inclui este registro e precede o commit focal.
- Autoridade Git limitada: somente o commit local focal
  `docs(governance): require copy-ready next messages` está autorizado.
  `amend`, branch, worktree, merge, rebase e push permanecem proibidos.
- Escopo negativo: zero código, build, execução do produto, interface, ação
  externa, ADR, Human Gate, `ActivationState`, ativação ou transição de
  lifecycle.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, ADRs, Human Gates, produto, interface e
  autoridade externa permanecem inalterados.
- Evidências proprietárias:
  [autoridade de coordenação](../governance/Conversation-Coordination-Prompt.md),
  [changelog do corpus](../system/Prompt-System-Change-Log.md) e
  [estado factual](Current-State.md).
- Aprovador: Bruno, por autorização explícita limitada à alteração documental
  sequencial, validações locais e commit focal; nenhuma autoridade adjacente
  inferida.

## 2026-07-30 — Recomendação de raciocínio do Codex incorporada por conversa

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, corpus de instruções `6.4.0` com 16 arquivos
  ativos, revisão `1.2.0` da autoridade de coordenação e baseline limpa no
  commit `1002fe21e7f3b36efe2f3e7167c8455b467625c0`.
- Autoridade humana exata:
  `Incluir na documentação a orientação, sugestão na de qual raciocinio do
  Codex usar em cada conversa: Leve, Médio, Alto, Extra alto, Máximo ou Ultra.`
- Limite interpretativo: o pedido autoriza exclusivamente a evolução
  documental dessa orientação. Ele não aceita o plano de fechamento MOD-12,
  não autoriza `M12-IC1` e não decide ADR, Human Gate, `ActivationState`,
  lifecycle ou ação externa.
- Preflight e baseline: branch `main`, HEAD
  `1002fe21e7f3b36efe2f3e7167c8455b467625c0`, index, worktree e inventário de
  untracked limpos antes da edição. Zero processo, serviço, listener, janela,
  instância de notification area ou browser dedicado pertencente ao
  DB-Notifier; o serviço Docker estava parado. Nenhum processo, serviço,
  database engine, browser, IDE ou recurso alheio foi encerrado ou alterado.
- Base oficial: a matriz foi reconciliada com a documentação oficial vigente
  de [configuração de modelos do Codex](https://learn.chatgpt.com/docs/models)
  e de
  [subagentes do Codex](https://learn.chatgpt.com/docs/agent-configuration/subagents).
  A disponibilidade permanece dependente da superfície, do modelo e da conta.
- Decisão documental: atualizar
  `Conversation-Coordination-Prompt.md` para a revisão `1.3.0` e o corpus para
  `6.5.0`, mantendo 16 arquivos ativos, a Política de Idioma na revisão
  `1.0.0` e a versão-fonte `2.0.0` do Prompt Mestre. Como os 14 campos do
  handoff, os 19 campos auxiliares e os enums existentes permanecem
  compatíveis, aplica-se evolução `MINOR`, não mudança estrutural `MAJOR`.
- Catálogo adotado: `Leve` (`Light`/`low`), `Médio` (`Medium`/`medium`),
  `Alto` (`High`/`high`), `Extra alto` (`Extra High`/`xhigh`), `Máximo`
  (`Max`/`max`) e `Ultra` (`Ultra`/`ultra`). `Máximo` atende o problema
  excepcionalmente difícil e indivisível; `Ultra` atende a tarefa grande,
  complexa e decomponível em frentes independentes com ganho material.
- Contrato preservado: `Your action now` começa com exatamente uma
  recomendação, identificador técnico e razão específica para a próxima
  interação. Planos paralelos definem a recomendação da coordenadora e de cada
  lane. A mensagem auxiliar usa uma frase de preâmbulo explicitamente não
  canônica e depois mantém seus 19 campos, sem criar um 20º campo.
- Guard rails: usar o menor esforço suficiente e reavaliar a recomendação a
  cada handoff. Nenhum nível comprova disponibilidade, seleção ou aplicação,
  garante melhor resultado, cria paralelismo, permite writer concorrente ou
  amplia escopo, autoridade, ownership, Git, runtime, preflight, Quality Gate,
  revisão humana, ADR, Human Gate, `ActivationState` ou lifecycle.
- Escopo documental: `AGENTS.md`, `prompts/Start-Here.md`,
  `prompts/governance/Governance.md`,
  `prompts/governance/Conversation-Coordination-Prompt.md`,
  `prompts/governance/Quality-Gates.md`,
  `prompts/templates/Templates.md`,
  `prompts/system/AI-Software-Engineering-Master-Prompt.md`,
  `prompts/system/Prompt-System-Change-Log.md`,
  `prompts/state/Current-State.md` e este log append-only.
- Gates preliminares: `git diff --check`, handoff `14/14`, mensagem auxiliar
  `19/19`, catálogo e mappings `6/6`, revisão `1.3.0`, corpus `6.5.0`, `963`
  links locais em `225` arquivos, documentação de `429` fontes e secret scan
  aprovados. A primeira revisão semântica independente bloqueou a possível
  leitura do preâmbulo como 20º campo e a ausência ainda intencional deste
  registro; o primeiro ponto foi corrigido e o recheck confirmou `PASS` sem
  novo bloqueio. Uma segunda revisão independente também confirmou `PASS`.
  A repetição integral dos gates inclui este registro e precede o commit focal.
- Autoridade Git: a instrução permanente do repositório exige um commit local
  focal para esta alteração rastreada. `amend`, branch, worktree, merge,
  rebase, push, publicação e ação remota permanecem sem autoridade.
- Escopo negativo: zero código, configuração executável, dependência, build,
  teste de produto, execução do produto, interface, ação externa, aceitação do
  plano MOD-12, `M12-IC1`, ADR, Human Gate, `ActivationState`, ativação,
  homologação ou transição de lifecycle.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, plano de fechamento MOD-12 `PREPARADO` e
  `PENDENTE DE REVISÃO`, ADRs, Human Gates, produto, interface e autoridade
  externa permanecem inalterados.
- Evidências proprietárias:
  [autoridade de coordenação](../governance/Conversation-Coordination-Prompt.md),
  [changelog do corpus](../system/Prompt-System-Change-Log.md) e
  [estado factual](Current-State.md).
- Aprovador: Bruno, por pedido explícito desta orientação documental; nenhuma
  autoridade adjacente foi inferida.

## 2026-08-27 — Método e fluxo de desenvolvimento governado adotados de forma DB-native

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, corpus de instruções `6.5.0`, autoridade de
  coordenação na revisão `1.3.0` e branch `main` no commit
  `f0f220c539fde685e2c500b4944ebca168aaec7d`.
- Autoridade humana exata:
  `Quero que implemente no DB-Notifier o mesmo método e fluxo de desenvolvimento do RAG-Challenge`.
- Preflight e baseline: zero processo ou listener pertencente ao DB-Notifier
  antes da ação. O index e o worktree rastreado estavam limpos; a árvore
  externa preexistente `mysql-notifier-1.1.8-src/` foi classificada como
  material de referência de terceiro protegido, permaneceu não lida, não
  alterada e não rastreada e foi excluída do inventário clean-room por
  `.gitignore`.
- Fontes congeladas: fluxo técnico público RAG-Challenge em
  `main@2154b311b4ba41d62f462e3cdb37bc360ee32ca4`, corpus governado em
  `codex/pdf1-internal-governance@31ac04ba53f2e94b305d4ca08eb2c6d23aab9f9a`
  e baseline DB-Notifier acima. O worktree público RAG-Challenge possuía
  alterações locais e, por isso, somente conteúdo commitado integrou a fonte.
- Decisão de adaptação: adotar autoridade e baseline antes da execução,
  escopos positivo e negativo, `PLANS.md` vivo, envelope fechado, ownership,
  stop codes, incrementos pequenos, regressão focal, revisão independente,
  integração serial, gate agregado e reconciliação de estado/histórico.
  Permanecem rejeitados lifecycle, corpus, qrels, PDFs/citações, cloud,
  projeção pública/privada e handoff compacto do RAG-Challenge; o orquestrador
  TypeScript/worktree fica `DEFERRED` até ADR e autoridade próprias.
- Sistema de instruções: corpus elevado a `6.6.0` com os mesmos `16` prompts
  ativos e autoridade de coordenação elevada a `1.4.0`. Permanecem exatamente
  `14` campos do handoff, `19` campos auxiliares e os enums fechados de
  roteamento/paralelismo. `PLANS.md`, scripts e testes são materializações não
  autorizantes e não criam lifecycle ou Human Gate.
- Tooling materializado: `.nvmrc`, `scripts/development.ps1` com `Doctor`,
  `Setup`, `Quick`, `Full`, `-Offline` e `-PlanOnly`, agregador
  `scripts/ci.ps1`, configuração NuGet offline, preflight executável,
  verificador de política, regressões determinísticas e workflow CI que delega
  ao mesmo gate em Windows e à etapa Dashboard suplementar em Linux.
- Segurança do runner: os entry points local e direto criam processo-filho sem
  shell e removem da cópia privada do ambiente, sem ler valores, credenciais
  de IA, activators e conexões DB-Notifier, flags de sandbox, configuração de
  endpoints/listeners e overrides ASP.NET. O shell pai permanece inalterado;
  campanhas físicas exigem seus runners e autoridades próprios. Processo
  pertencente ao produto exige evidência de executable path, command line ou
  raiz do projeto, nunca apenas nome.
- Semântica dos checks: `Quick` permanece `NON_GATE`; `Full` delega exatamente
  uma vez ao agregador. `PlanOnly` é determinístico e sem processo/preflight.
  Offline usa restore/cache local, suprime notificação de update de workloads
  .NET, preserva advisories online como `NOT_RUN` e só pode resultar
  `PARTIAL`. Resultado mecânico, classificação do gate automático e decisão
  humana permanecem camadas separadas.
- Hardening durante revisão: findings independentes detectaram envelope
  incompleto, atribuição indevida de autoridade ao plano, taxonomia ambígua,
  descrição incorreta de `PlanOnly`, ambiente herdado capaz de habilitar testes
  físicos ou endpoints/listeners, dispatch case-sensitive de `Stage`,
  identificação por nome de processo, plano `Full` incompleto, notificação de
  workload offline, secret scan tardio, inventário procfs permissivo e timeout
  sem margem para a serialização. Cada ponto foi corrigido no menor owner
  aplicável; nenhum finding foi convertido silenciosamente em aprovação. O
  timeout canônico de `135` minutos deriva dos `125` minutos combinados dos
  antigos envelopes paralelos mais margem de orquestração.
- Evidência mecânica aprovada: preflight; verificador do fluxo `97` assertions;
  regressões do fluxo `64`; sintaxe primária `47` PowerShell e `14` Node;
  sintaxe legada `27` PowerShell com `20` skips declarados; runner `68`
  assertions; regressões de sintaxe `13`; documentação `437` fontes; Markdown
  `977` links em `227` arquivos; validação do bundle; scan de segredos do
  worktree não ignorado e do histórico Git disponível; `git diff --check`; e
  `git fsck --full`, cujos objetos dangling foram informativos sem erro de
  integridade.
- Limitação factual: faltam os pins exatos .NET SDK `10.0.302`, Node.js
  `24.18.0` e npm `11.16.0`. `Doctor` encerrou `DEPENDENCY_UNREADY` no SDK,
  após aprovar layout, lockfiles e dependências restauradas. As tentativas
  diagnósticas Dashboard offline encerraram `DEPENDENCY_UNREADY` no Node; a
  mais recente aprovou preflight e scan completo de segredos antes do bloqueio.
  Nenhuma tentativa gerou ou foi promovida a `PASS`.
- Disposição: implementação estrutural concluída e política testada, mas o
  incremento permanece `PARTIAL`. `Quick`, `Full All`, build, testes de
  produto, cobertura, browser/runtime e freshness online de advisories não
  possuem `PASS` neste ambiente. Remote CI não foi executada.
- Revisão: as três lanes independentes são `Dirac` (equivalência do método),
  `Turing` (PowerShell/runner) e `Schrodinger` (governança/estado), todas
  read-only e sem custody de integração. O recheck final sobre fotografia
  congelada permanece item obrigatório antes do commit focal.
- Autoridade Git: a instrução permanente exige um commit local focal ao fim do
  incremento. `amend`, branch, worktree, merge, rebase, push, publicação,
  release e ação remota permanecem sem autoridade.
- Escopo negativo: zero alteração de comportamento de produto, provider,
  schema, migration, interface, packaging de release, database real, serviço
  externo, navegador comum, deploy, ADR, Human Gate, ativação, homologação ou
  transição de lifecycle.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, providers, ADRs, Human Gates, produto,
  interface e autoridade externa permanecem inalterados.
- Evidências proprietárias: [plano vivo](../../PLANS.md),
  [fluxo de desenvolvimento](../../docs/Development.md),
  [gate de qualidade](../governance/Quality-Gates.md),
  [autoridade de coordenação](../governance/Conversation-Coordination-Prompt.md),
  [changelog do corpus](../system/Prompt-System-Change-Log.md) e
  [estado factual](Current-State.md).
- Aprovador: Bruno, pelo pedido explícito do método e fluxo; nenhuma autoridade
  adjacente foi inferida.
- Recheck independente final: governança/estado e PowerShell/runner encerraram
  `P0=0`, `P1=0`, `P2=0`, `P3=0`; equivalência do método encerrou `P0=0`,
  `P1=0`, `P2=0` e dois `P3` de precisão documental. Os dois `P3` foram
  corrigidos ao distinguir runtime de produto do helper descartável e ao
  documentar o failure outcome `ISOLATION_FAILURE` do sanitizador. A revisão
  permaneceu estática, sem build, teste de produto, runtime ou acesso à árvore
  externa protegida.

## 2026-08-27 — Provisionamento exato I6 interrompido no cleanup isolado

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, plano `DEV-FLOW-01` em `PARTIAL` e branch
  `main` no commit `e6416f6dac3f65d672f0247da88850881bae9120`.
- Autoridade humana exata: provisionar local e isoladamente .NET SDK
  `10.0.302`, Node.js `24.18.0` e npm `11.16.0`, sem alterar os pins ou
  lockfiles; depois executar preflight, `Doctor`, `Quick` e uma única execução
  online de `Full`, preservando o primeiro resultado factual e parando diante
  de qualquer falha. Banco/provider real, navegador comum, deploy, push, Human
  Gate, ativação e transição de `STATE` permaneceram proibidos.
- Preflight: zero processo ou listener pertencente ao DB-Notifier antes da
  ação e novamente antes da reconciliação factual.
- Proveniência: metadata oficial .NET 10 forneceu o arquivo win-x64 do SDK
  `10.0.302` e seu SHA-512; o catálogo oficial Node.js `24.18.0` forneceu o
  arquivo win-x64, seu SHA-256 e a composição com npm `11.16.0`.
- Provisionamento: os dois arquivos corresponderam aos hashes oficiais, foram
  extraídos sob o root ignorado `.dotnet/toolchains/` e os executáveis
  reportaram exatamente .NET SDK `10.0.302`, Node.js `24.18.0` e npm
  `11.16.0`. Nenhum pin, manifesto ou lockfile foi alterado.
- Bloqueio: após a extração, o limite local de execução rejeitou o comando de
  remoção recursiva do staging já verificado antes de criar o processo. O
  diretório ignorado `.dotnet/provisioning-i6/` reteve somente os dois arquivos
  oficiais verificados, totalizando `334721515` bytes. Nenhum mecanismo
  alternativo de exclusão foi tentado.
- Disposição: `I6 BLOCKED` por `ISOLATION_FAILURE`. O stop-on-failure encerrou
  a sequência; o preflight foi `PASS`, enquanto o novo `Doctor`, `Quick` e a
  única execução online de `Full` permaneceram `NOT_RUN`. Nenhum resultado de
  gate canônico foi produzido, repetido, corrigido ou inferido.
- Escopo negativo preservado: nenhum componente de produto, database/provider
  real, browser, deploy, push, Human Gate, ativação, homologação ou transição
  de lifecycle foi executado.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, providers, ADRs, Human Gates, produto,
  interface e autoridade externa permanecem inalterados.
- Evidências proprietárias: [plano vivo](../../PLANS.md),
  [estado factual](Current-State.md),
  [fluxo de desenvolvimento](../../docs/Development.md) e
  [gate de qualidade](../governance/Quality-Gates.md).
- Aprovador: Bruno, exclusivamente para o envelope descrito; nenhuma
  autoridade adjacente foi inferida.

## 2026-08-27 — Clarificação literal das autoridades I6 e I7

- Clarificação append-only: o rótulo “Autoridade humana exata” da entrada I6
  anterior descreveu um resumo semanticamente fiel, mas não uma citação
  literal. A disposição `BLOCKED`/`ISOLATION_FAILURE` e toda a evidência dessa
  entrada permanecem inalteradas.
- Texto literal I6: `AUTORIZO exclusivamente provisionar, de forma local e isolada para o workspace DB-Notifier, o .NET SDK 10.0.302, Node.js 24.18.0 e npm 11.16.0, sem alterar global.json, .nvmrc, package.json ou lockfiles e sem instalar componentes de produto. Depois, execute o shutdown preflight, Doctor, Quick e uma única execução online de Full, preserve o primeiro resultado factual e pare diante de qualquer falha. Permanecem proibidos banco/provider real, navegador comum, deploy, push, Human Gate, ativação e transição de STATE.`
- Texto literal I7: `Não fixar as versões, introduzir um intervalo de versões compatíveis, pois pode acontecer do computador atualizar automaticamente eles`.
- Continuidade interpretada e declarada: I7 substitui somente a exigência de
  pins exatos e mantém a sequência I6 ainda não executada, seu primeiro
  resultado factual e seu stop-on-failure. Essa interpretação não amplia banco,
  provider, navegador comum, deploy, push, Human Gate, ativação ou transição de
  `STATE`.

## 2026-08-27 — I7 bloqueado no primeiro Doctor da política de faixas

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, branch `main` no commit
  `e6416f6dac3f65d672f0247da88850881bae9120`, mais os registos factuais I6
  ainda não commitados e protegidos.
- Autoridade humana literal mais recente: `Não fixar as versões, introduzir um intervalo de versões compatíveis, pois pode acontecer do computador atualizar automaticamente eles`.
  A sequência I6 ainda não executada, o primeiro resultado factual, o
  stop-on-failure e todo o escopo negativo permaneceram herdados.
- Política materializada: .NET SDK `>=10.0.302 <10.1.0` com `latestFeature` e
  prereleases desabilitados; Node.js `>=24.18.0 <25.0.0`; npm
  `>=11.16.0 <12.0.0`. `.nvmrc` seleciona a linha Node 24, os manifests
  Dashboard repetem as faixas e o lockfile muda somente na metadata raiz de
  engines, sem alterar grafo, versões ou integridades de dependência.
- Integração técnica: a política PowerShell compartilhada, os resolvers local
  e CI, o verificador Node, as regressões, o workflow e a documentação foram
  alinhados às faixas. A revisão independente encontrou inicialmente
  `P1=1`, `P2=3`, `P3=1`; o recheck estático final encerrou `P0=0`, `P1=0`,
  `P2=0`, `P3=0` antes da validação executável.
- Preflight inicial: `PASS`, com zero processo correspondente e zero listener
  pertencente ao DB-Notifier. Nenhum runtime de produto, provider, banco ou
  navegador foi iniciado.
- Primeiro resultado executável: `scripts/development.ps1 Doctor` encerrou
  com exit code `1`. Raiz do repositório, lockfiles e dependências restauradas
  aprovaram; a política de toolchain falhou com
  `The provided JSON includes a property whose name is an empty string, this is only supported using the -AsHashTable switch.` ao ler a chave raiz vazia de
  `package-lock.json`.
- Stop factual: nenhuma correção, repetição ou execução alternativa de
  `Doctor` ocorreu. `Quick` e a única execução online de `Full` permaneceram
  `NOT_RUN`; a autorização de uma execução online de `Full` não foi consumida
  e nenhum resultado de gate canônico foi produzido ou inferido.
- Preflight de encerramento: `PASS`, novamente com zero processo
  correspondente e zero listener pertencente ao DB-Notifier.
- Disposição: `I7 BLOCKED` por `GATE_FAILURE`. A implementação de faixas fica
  materializada, porém não validada pelo fluxo canônico; o finding aberto é a
  desserialização do lockfile sem o modo que preserva chaves vazias.
- Escopo negativo preservado: nenhum componente de produto, database/provider
  real, navegador comum, deploy, push, Human Gate, ativação, homologação ou
  transição de lifecycle foi executado. O resíduo ignorado I6 não foi apagado.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, providers, ADRs, Human Gates, produto,
  interface e autoridade externa permanecem inalterados.
- Próxima autoridade necessária: uma correção focal e testada do parser da
  chave raiz vazia do lockfile, seguida de nova sequência preflight,
  `Doctor`, `Quick` e exatamente uma execução online de `Full`, ainda com
  parada no primeiro resultado de falha.

## 2026-08-27 — I7-R1 corrige o parser e bloqueia no primeiro Quick

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, I7 `BLOCKED`/`GATE_FAILURE` e baseline limpa
  `main@fc7001240f87a3ee555b9e53cc98d8c0c57b4ce5`.
- Autoridade humana literal: `AUTORIZO exclusivamente uma nova tentativa do lote I7-R1 no workspace DB-Notifier, começando por localizar de forma somente leitura o entry point canônico existente do shutdown preflight e executá-lo uma única vez. Se o preflight passar, corrija de forma mínima e testada a leitura da chave raiz vazia de package-lock.json no policy helper, sem alterar as faixas, o grafo, as versões ou as integridades das dependências e sem remover o resíduo ignorado de I6. Depois, execute Doctor, Quick e uma única execução online de Full, preserve o primeiro resultado factual de cada etapa e pare diante de qualquer falha. Permanecem proibidos banco/provider real, navegador comum, deploy, push, Human Gate, ativação e transição de STATE.`
- Preflight: a descoberta somente leitura identificou
  `scripts/assert-dbnotifier-shutdown.ps1`; sua única execução inicial aprovou
  com zero processo correspondente e zero listener pertencente ao DB-Notifier.
- Proteção congelada: `global.json`, Dashboard `package.json` e
  `package-lock.json` conservaram, antes e depois da correção, os SHA-256
  `6CD80ED6F7A93E76C20E47164E3BFEDFDDC1B42B33519B2CD2DEE6FEBFF4E836`,
  `5A137255C337AB1A159E797DD7187BCFDBCDB70CA75C73C0CF43FAF5F3A917A8` e
  `ADC835185B3676484274ACC938487EE846599AADCC18E16B02D4B3FBE64EC5F0`.
- Correção: somente `scripts/toolchain-version-policy.ps1` e sua regressão
  focal mudaram. O lockfile é desserializado com `-AsHashtable`; o leitor de
  propriedade aceita o nome vazio exigido pela metadata raiz npm e continua
  falhando quando essa chave está ausente. Faixas, grafo, versões e
  integridades de dependência permaneceram inalterados.
- Evidência focal: `tests/DBNotifier.DevelopmentFlow.Tests.ps1` aprovou em sua
  primeira execução com `94` assertions e exit code `0`.
- `Doctor`: a nova e única execução aprovou com exit code `0`; preflight
  interno, raiz do repositório, toolchains compatíveis, lockfiles e
  dependências restauradas passaram.
- `Quick`: a primeira execução declarou-se `NON_GATE` e encerrou com exit code
  `1`. O build Release aprovou com zero aviso e zero erro; os testes unitários
  aprovaram `528/528`; os testes de arquitetura aprovaram `99/100`. A única
  falha foi
  `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`,
  porque a substring esperada `state06-consolidated-e2e:` não foi encontrada.
- Stop factual: não houve diagnóstico, correção ou repetição de `Quick`.
  Exatamente uma execução online de `Full` permaneceu `NOT_RUN`; sua
  autorização não foi consumida e nenhum gate canônico agregado foi produzido
  ou inferido.
- Disposição: o defeito DF-014 foi `RESOLVED` somente sob a nova autoridade e
  evidência I7-R1; a falha histórica I7 permanece preservada. I7-R1 está
  `BLOCKED` por `GATE_FAILURE`, com o novo finding DF-015 aberto.
- Escopo negativo preservado: nenhum banco/provider real, navegador comum,
  deploy, push, Human Gate, ativação, homologação ou transição de `STATE` foi
  executado; o resíduo ignorado I6 e a fonte externa protegida não foram
  removidos ou lidos.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, providers, ADRs, Human Gates, produto,
  interface e autoridade externa permanecem inalterados.

## 2026-08-27 — AUD-2026-R1 corrige inventários e bloqueia no primeiro Quick

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, I7-R1 `BLOCKED`/`GATE_FAILURE` e baseline
  limpa `main@3762f71c116af206b911a086b836cef11cd1894d`.
- Autoridade humana literal: `AUTORIZO exclusivamente o lote AUD-2026-R1 Gate And Inventory Integrity no DB-Notifier. Parta da baseline main\@3762f71c116af206b911a086b836cef11cd1894d; se houver drift rastreado, pare sem editar. Execute primeiro o shutdown preflight e atualize o PLANS.md antes da implementação. Corrija somente: (1) a regra de ignore que oculta novos arquivos em src/DBNotifier.Persistence.Agent.Sqlite, mantendo arquivos runtime SQLite ignorados; (2) o verificador Markdown para derivar seu corpus do inventário Git e provar por regressão que nunca atravessa mysql-notifier-\*-src nem outra raiz ignorada protegida; e (3) o teste arquitetural obsoleto para validar a topologia consolidada atual do CI, sem restaurar jobs antigos. Adicione regressões focais, preserve en-GB nos artefatos técnicos e não altere versões, lockfiles, dependências ou contratos externos. Depois, execute checks focais, Doctor e Quick; somente se todos passarem, execute uma única vez o Full online. Preserve o primeiro resultado factual de cada etapa e pare diante de qualquer FAIL ou BLOCKED, sem retry ou correção em linha. Faça um commit local focado conforme as instruções do repositório e reconcilie apenas a documentação factual obrigatória. Permanecem proibidos: ler ou modificar o material externo protegido, excluir qualquer resíduo ignorado, alterar backend/migrations, usar banco ou provider real, navegador comum, deploy, push, Human Gate, ativação ou transição de STATE.`
- Preflight e baseline: o shutdown inicial aprovou com zero processo
  correspondente e zero listener pertencente ao DB-Notifier. Branch, commit,
  index e worktree rastreada corresponderam exatamente à baseline autorizada
  antes da atualização do plano e da implementação.
- Correção de ignore: a exceção estreita para a árvore-fonte canônica
  `src/DBNotifier.Persistence.Agent.Sqlite/` restaura sua visibilidade ao
  inventário Git. Arquivos runtime `.db`, `.sqlite`, `.sqlite3`, journal, SHM e
  WAL continuam ignorados, inclusive dentro dessa árvore.
- Correção Markdown: o gate deixou de caminhar a árvore física e passou a
  derivar fontes e destinos admitidos de inventários Git NUL-delimited. Paths
  ignorados rastreados são excluídos; escapes de raiz, drive Windows, UNC,
  barras invertidas e symlinks rastreados ou físicos falham antes de leitura ou
  aceitação de destino. A regressão sintética usa repositórios temporários
  próprios e prova que arquivos novos não ignorados são verificados enquanto
  as duas raízes ignoradas sentinela não são atravessadas.
- Correção arquitetural: o teste obsoleto passou a validar um job
  `canonical-windows`, uma execução `Stage All`, uma raiz recursiva única de
  diagnóstico sanitizado, o upload condicional e o job Dashboard Linux
  suplementar. O workflow CI permaneceu read-only e nenhum job removido foi
  restaurado.
- Revisão independente: a primeira inspeção estática retornou `P0=0`, `P1=1`,
  `P2=3`, `P3=1`; depois das correções pré-validação, a segunda retornou
  `P0=0`, `P1=0`, `P2=1`, `P3=0`; a releitura final encerrou `P0=0`, `P1=0`,
  `P2=0`, `P3=0`. Nenhuma revisão executou código ou leu material protegido.
- Evidência focal: as regressões de inventário aprovaram `2/2`; o verificador
  Markdown real aprovou `981` links em `226` arquivos do corpus Git não
  ignorado; o filtro arquitetural exato aprovou `1/1`. Cada comando foi
  executado uma única vez, com exit code `0`.
- `Doctor`: a única execução aprovou com exit code `0`; seu preflight interno
  encontrou zero processo/listener correspondente, e raiz, toolchains,
  lockfiles e dependências restauradas passaram.
- `Quick`: a primeira execução declarou-se `NON_GATE` e encerrou com exit code
  `1`. Antes da falha, build Release aprovou com zero aviso e zero erro, testes
  unitários `528/528`, arquitetura `100/100`, Node `74/74`, além de assets,
  tipos, documentação e Markdown. O verificador de desenvolvimento então
  falhou porque `PLANS.md` não contém a chave literal obrigatória
  `- Initial baseline:`.
- Stop factual: não houve correção da chave, repetição nem execução alternativa
  de `Quick`. A única execução online autorizada de `Full` permaneceu
  `NOT_RUN`; sua allowance não foi consumida e nenhum gate canônico agregado
  foi produzido ou inferido. O preflight final de encerramento aprovou com zero
  processo correspondente e zero listener próprio.
- Disposição: `AUD-2026-R1 BLOCKED` por `GATE_FAILURE`. As três correções
  candidatas e suas evidências focais permanecem materializadas, mas o finding
  de política do plano fica aberto e o lote não possui disposição canônica
  `Full`.
- Escopo negativo preservado: nenhum arquivo externo protegido foi lido ou
  modificado; nenhum resíduo ignorado foi excluído; backend, migrations,
  banco/provider real, navegador comum, deploy, push, Human Gate, ativação e
  transição de `STATE` não foram executados. Versões, lockfiles, dependências,
  contratos externos e workflow CI permaneceram inalterados.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, providers, ADRs, Human Gates, produto,
  interface e autoridade externa permanecem inalterados.

## 2026-08-27 — AUD-2026-R1-R1 corrige o controle e falha no único Full

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, `AUD-2026-R1 BLOCKED`/`GATE_FAILURE` e
  baseline limpa `main@b60ef4d302e4c4dc3f0e474be27eaa4b8c6beb13`.
- Autoridade humana literal: `AUTORIZO exclusivamente o lote corretivo AUD-2026-R1-R1 Plan Control Integrity no DB-Notifier, partindo da baseline limpa main\@b60ef4d302e4c4dc3f0e474be27eaa4b8c6beb13. Execute primeiro o shutdown preflight e pare sem editar diante de qualquer drift rastreado. Corrija somente o control record de PLANS.md, renomeando “- Frozen baseline:” para a chave literal exigida “- Initial baseline:” e preservando o valor factual main\@3762f71c116af206b911a086b836cef11cd1894d. Não altere a implementação já commitada, testes, workflow, versões, lockfiles, dependências ou contratos externos. Depois, execute uma única vez o check focal da política de desenvolvimento, Doctor e Quick; somente se todos passarem, execute exatamente uma vez o Full online. Preserve o primeiro resultado factual de cada etapa e pare diante de qualquer FAIL ou BLOCKED, sem retry ou correção em linha. Reconcilie somente a documentação factual obrigatória e faça um commit local focado. Permanecem proibidos material externo protegido, exclusão de resíduos ignorados, backend/migrations, banco ou provider real, navegador comum, deploy, push, Human Gate, ativação e transição de STATE.`
- Preflight e baseline: o shutdown inicial aprovou com zero processo
  correspondente e zero listener pertencente ao DB-Notifier. Branch, commit,
  index e worktree não ignorada corresponderam exatamente à baseline autorizada
  antes da edição.
- Correção: uma única linha no control record de `PLANS.md` renomeou
  `- Frozen baseline:` para `- Initial baseline:` e preservou o valor original
  `main@3762f71c116af206b911a086b836cef11cd1894d`. Nenhum arquivo de código,
  teste, workflow, versão, manifesto, lockfile, dependência ou contrato externo
  foi alterado.
- Evidência focal: a única execução de
  `scripts/verify-development-flow.ps1` aprovou `105` assertions e exit code
  `0`.
- `Doctor`: a única execução aprovou com exit code `0`; seu preflight interno
  encontrou zero processo/listener correspondente, e raiz, toolchains,
  lockfiles e dependências restauradas passaram.
- `Quick`: a primeira e única execução declarou-se `NON_GATE` e aprovou com
  exit code `0`. Build Release aprovou com zero aviso/erro; testes unitários
  `528/528`, arquitetura `100/100`, Node `74/74`, política `105`, regressões de
  política `94`, runner `68` e sintaxe `13`, além dos checks auxiliares
  aplicáveis.
- `Full` antes da falha: a única execução online aprovou seus dois shutdown
  preflights, secret scan, política, restore locked de `19` projetos, build com
  zero aviso/erro, arquitetura `100/100`, WPF `10/10`, unitários `528/528`,
  integração `168/168`, cobertura de linhas `83,41%`, branches `56,62%` e `10`
  componentes obrigatórios, vulnerabilidades NuGet nos `19` projetos e o
  runtime audit fail-closed. Esse audit observou resposta live `200`, todos os
  endpoints HTTP protegidos como `426`, workers Agent desabilitados, polling de
  comandos ausente e persistência local não inicializada.
- `Full` — primeira e única disposição: a compatibilidade legada não resolveu
  o SDK .NET `10.0.302` exigido no processo-filho durante o inventário MSBuild
  de `DBNotifier.Domain.csproj`. O teste registrou `Failed=1`, `Pending=0`; o
  gate encerrou com exit code `1` e
  `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`.
- Stop factual: nenhuma repetição, diagnóstico executável, correção, ambiente
  alternativo ou execução adicional de `Full` ocorreu. Os passes parciais não
  substituem nem corrigem a disposição agregada `FAIL`.
- Preflight de encerramento: `PASS`, com zero processo correspondente e zero
  listener pertencente ao DB-Notifier.
- Disposição: `AUD-2026-R1-R1 BLOCKED` por `GATE_FAILURE`. A correção do
  controle foi validada até `Quick`, mas o lote não possui `Full PASS`.
- Escopo negativo preservado: material externo protegido não foi lido ou
  modificado; nenhum resíduo ignorado foi excluído; backend, migrations,
  banco/provider real, navegador comum, deploy, push, Human Gate, ativação e
  transição de `STATE` não foram executados.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, providers, ADRs, Human Gates, produto,
  interface e autoridade externa permanecem inalterados.

## 2026-08-27 — AUD-2026-R1-R3-R1 corrige a identidade do SDK legado e falha no audit Dashboard

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, `AUD-2026-R1-R1 BLOCKED` por seu único
  `Full`, baseline limpa
  `main@6ecc72f7a347a746d0153072580c527d7c679e81`.
- Primeira tentativa preservada: o shutdown inicial de `AUD-2026-R1-R3`
  retornou exit code `1`,
  `BLOCKED|shutdown-preflight|pid=8952|process=pwsh.exe` e
  `ISOLATION_FAILURE`. O lote parou antes de verificar a baseline ou editar;
  nenhum processo foi encerrado, e não houve retry ou validação executável.
- Autoridade humana de recuperação: verificar somente a identidade atual do
  PID `8952` sem ler ambiente/segredos; encerrá-lo apenas se a propriedade pelo
  DB-Notifier fosse comprovada; executar exatamente um novo preflight; e, só
  após baseline limpa e exata, retomar a propagação do SDK legado, checks
  focais, `Doctor`, `Quick` e um único `Full` online, parando no primeiro
  `FAIL` ou `BLOCKED`.
- Recuperação: o PID `8952` já estava ausente na única consulta autorizada;
  nenhum processo foi encerrado. O único novo shutdown preflight aprovou com
  zero processo correspondente e zero listener próprio.
- Baseline: branch `main`, HEAD
  `6ecc72f7a347a746d0153072580c527d7c679e81` e árvore/index rastreados limpos
  foram confirmados antes de atualizar `PLANS.md` e antes da implementação.
- Implementação: `ci.ps1` passou o executável `dotnet` absoluto já validado à
  compatibilidade legada; `run-legacy-tests.ps1` tornou o caminho obrigatório e
  o encaminhou como parâmetro Pester; `DBNotifier.Legacy.Tests.ps1` o forneceu
  às sete chamadas do verificador; e as regressões de desenvolvimento passaram
  a exigir a cadeia completa e a ausência de fallback no caminho canônico.
- Escopo congelado: `development.ps1`, `verify-nuget-vulnerabilities.ps1`,
  `global.json`, produto, workflow, versões, manifests, lockfiles, dependências
  e contratos externos não foram alterados.
- Evidência focal: a única execução de `scripts/verify-development-flow.ps1`
  aprovou `105` assertions. A única execução focal do runner legado, com o host
  absoluto resolvido em `C:\Program Files\dotnet\dotnet.exe`, aprovou `34`
  testes, aceitou `1` skip condicional e atingiu cobertura `35,17%` (`338/961`).
- `Doctor`: a única execução aprovou com exit code `0`; preflight interno,
  raiz, toolchains, lockfiles e dependências restauradas passaram.
- `Quick`: a única execução declarou-se `NON_GATE` e aprovou com exit code `0`.
  Build Release teve zero aviso/erro; unitários `528/528`, arquitetura
  `100/100`, Node `74/74`, política `105`, regressões de política `98`, runner
  `68` e sintaxe `13` passaram, além dos checks auxiliares aplicáveis.
- `Full` antes da falha: a única execução online aprovou seus dois preflights,
  secret scan, políticas, restore locked de `19` projetos, build com zero
  aviso/erro, arquitetura `100/100`, WPF `10/10`, unitários `528/528`,
  integração `168/168`, cobertura de linhas `83,41%`, branches `56,62%` e `10`
  componentes obrigatórios, vulnerabilidades NuGet nos `19` projetos e o
  runtime audit fail-closed. Esse audit observou live `200`, endpoints HTTP
  protegidos `426`, workers Agent desabilitados, polling de comandos ausente e
  persistência local não inicializada.
- Correção comprovada no `Full`: compatibilidade legada aprovou `34` testes,
  `1` skip e cobertura `35,17%` (`338/961`), seguida por validação do bundle.
  Dashboard toolchain, assets, tipos, documentação em `439` fontes, Markdown
  em `981` links de `226` arquivos, `74/74` testes Node e build de produção
  também aprovaram.
- `Full` — primeira e única disposição: o audit online de dependências
  Dashboard reportou uma vulnerabilidade de severidade alta para
  `nanoid <3.3.18`, identificada por `GHSA-2v37-7h3g-55p8`. O gate encerrou com
  exit code `1` e `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`.
- Stop factual: nenhuma repetição, alteração de dependência/lockfile,
  diagnóstico executável, correção em linha ou execução adicional de `Full`
  ocorreu. Os passes parciais e a correção legada comprovada não substituem a
  disposição agregada `FAIL`.
- Preflight de encerramento: `PASS`, com zero processo correspondente e zero
  listener pertencente ao DB-Notifier; não constituiu retry de gate.
- Disposição: `AUD-2026-R1-R3-R1 BLOCKED` por `GATE_FAILURE`. O finding legado
  está resolvido somente neste lote separado; o `Full FAIL` histórico de
  `AUD-2026-R1-R1` permanece inalterado.
- Escopo negativo preservado: material externo protegido não foi lido ou
  modificado; nenhum resíduo ignorado foi excluído; backend, migrations,
  banco/provider real, navegador comum, deploy, push, Human Gate, ativação e
  transição de `STATE` não foram executados.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, providers, ADRs, Human Gates, produto,
  interface e autoridade externa permanecem inalterados.

## 2026-08-27 — AUD-2026-R1-R4 corrige o advisory npm e falha no E2E consolidado

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, `AUD-2026-R1-R3-R1 BLOCKED` por seu único
  `Full`, baseline limpa
  `main@0f59408440dc1c5877f8d3de0de8859ef9bc7fed`.
- Autoridade humana literal: `Quero que implemente no DB-Notifier o mesmo
  método e fluxo de desenvolvimento do RAG-Challenge`.
- Preflight e baseline: o shutdown inicial aprovou com zero processo
  correspondente e zero listener pertencente ao DB-Notifier. Branch, commit,
  index e worktree rastreada estavam limpos antes da abertura do envelope.
  .NET `10.0.400`, Node.js `24.19.0` e npm `11.17.0` satisfizeram as faixas
  estáveis governadas sem alterar qualquer pin ou intervalo.
- Proveniência: a GitHub Advisory Database identifica `nanoid <3.3.18` como
  afetado por `GHSA-2v37-7h3g-55p8` e `3.3.18` como corrigido. Os metadados do
  registro npm forneceram o tarball e a integridade SHA-512 depois gravados no
  lockfile.
- Correção: uma única execução de npm em modo package-lock-only atualizou
  somente `version`, `resolved` e `integrity` do nó transitivo `nanoid`, de
  `3.3.16` para `3.3.18`. O `package.json` Dashboard permaneceu byte-idêntico
  no SHA-256 `5a137255c337ab1a159e797dd7187bcfdbcdb70ca75c73c0cf43faf5f3a917a8`,
  a aresta `postcss -> nanoid ^3.3.16` permaneceu igual e nenhum componente de
  produto foi instalado pela correção.
- Evidência focal: a única auditoria `npm audit --audit-level=high` retornou
  exit code `0` e `found 0 vulnerabilities`.
- `Doctor`: a única execução aprovou com exit code `0`; preflight interno,
  raiz, toolchains, lockfiles e dependências restauradas passaram.
- `Quick`: a única execução declarou-se `NON_GATE` e aprovou com exit code `0`.
  Build Release teve zero aviso/erro; unitários `528/528`, arquitetura
  `100/100`, Node `74/74`, política `105`, regressões de política `98`, runner
  `68` e sintaxe `13` passaram, além dos checks auxiliares aplicáveis.
- `Full` antes da falha: a única execução online aprovou seus dois preflights,
  secret scan, políticas, restore locked de `19` projetos, build com zero
  aviso/erro, arquitetura `100/100`, WPF `10/10`, unitários `528/528`,
  integração `168/168`, cobertura de linhas `83,41%`, branches `56,62%` e `10`
  componentes obrigatórios, vulnerabilidades NuGet nos `19` projetos, runtime
  audit fail-closed, compatibilidade legada com `34` testes e `1` skip, bundle,
  Dashboard toolchain, assets, tipos, documentação em `439` fontes, Markdown
  em `981` links de `226` arquivos, `74/74` testes Node, build de produção e
  o audit npm com zero vulnerabilidades. O audit isolado do Dashboard também
  aprovou `128` amostras de viewport, `96` de forced colours e `24` focais de
  zoom/reflow.
- `Full` — primeira e única disposição: o runner STATE-06 E2E consolidado
  chegou a `scripts/run-state06-consolidated-e2e.ps1:254` e PowerShell reportou
  `The property 'marker' cannot be found on this object.` O gate encerrou com
  exit code `1` e `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`.
- Stop factual: nenhuma repetição, diagnóstico executável, correção em linha
  ou execução adicional de `Full` ocorreu. Os passes parciais e a correção do
  advisory comprovada não substituem a disposição agregada `FAIL`.
- Preflight de encerramento: `PASS`, com zero processo correspondente e zero
  listener pertencente ao DB-Notifier; não constituiu retry de gate.
- Disposição: `AUD-2026-R1-R4 BLOCKED` por `GATE_FAILURE`. O finding npm está
  resolvido somente neste lote separado; os resultados históricos anteriores
  permanecem inalterados.
- Escopo negativo preservado: material externo protegido não foi lido ou
  modificado; nenhum resíduo ignorado foi excluído; banco/provider real,
  navegador comum, deploy, push, Human Gate, ativação e transição de `STATE`
  não foram executados.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, providers, ADRs, Human Gates, produto,
  interface e autoridade externa permanecem inalterados.

## 2026-08-28 — AUD-2026-R1-R5-R1 materializa a guarda e para sem veredicto focal comprovável

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, `AUD-2026-R1-R4 BLOCKED` por seu único `Full`,
  baseline limpa `main@34e5f3358491a1eb52b508c0170d6a9ac3169bc4`.
- Autoridade humana: executar um único shutdown preflight; atualizar `PLANS.md`
  antes da implementação; corrigir somente o parsing de readiness consolidado e
  sua regressão focal; depois executar uma vez teste focal, `Doctor`, `Quick` e,
  apenas após todos aprovarem, um único `Full` online, preservando o primeiro
  resultado e sem retry ou correção em linha.
- Preflight e baseline: o único shutdown inicial aprovou com exit code `0`, zero
  processo correspondente e zero listener próprio. Branch, HEAD, index,
  worktree rastreada e inventário não ignorado corresponderam à baseline limpa.
- Planeamento: `PLANS.md` congelou autoridade, baseline, ownership, escopos
  positivo e negativo, contratos, critérios e stop codes antes da implementação.
- Implementação: o runner usa guarda de nulo, lookup protegido em
  `PSObject.Properties['marker']`, admissão de tipo string e comparação `-ceq`
  com o literal canônico. A regressão focal exige o lookup protegido e proíbe
  `$candidate.marker`. StrictMode herdado, host, literal, runners adjacentes,
  versões, dependências, lockfiles e integridades permaneceram inalterados.
- Revisão estática: `git diff --check` aprovou, o runner contém zero acesso
  direto `$candidate.marker` e nenhum caminho congelado apresentou diff. Essa
  evidência não substitui execução.
- Teste focal: a única invocação compilou o candidato e reportou um arquivo de
  teste correspondente, mas o canal de execução não reteve o veredicto final nem o
  exit code. A recuperação somente leitura da mesma execução não encontrou
  processo correspondente nem artefacto TRX/log durável. O resultado não foi
  inferido como `PASS` nem como falha do teste, e a invocação não foi repetida.
- Stop factual: `AUD-2026-R1-R5-R1 BLOCKED` por `ISOLATION_FAILURE`. `Doctor`,
  `Quick` e o `Full` online condicional ficaram `NOT_RUN`; nenhuma autorização
  posterior foi consumida e nenhuma correção em linha ocorreu.
- Escopo negativo preservado: nenhum banco/provider real, navegador comum,
  deploy, push, Human Gate, ativação ou transição de `STATE` foi executado.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, providers, ADRs, Human Gates, produto,
  interface e autoridade externa permanecem inalterados.

## 2026-08-28 — AUD-2026-R1-R5-R2 para antes de invocar o teste focal

- Estado anterior: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, `AUD-2026-R1-R5-R1 BLOCKED` por resultado focal
  não comprovável, baseline limpa
  `main@dcd3d24064e0709981e8ba0cff223de6a2c35565`.
- Autoridade humana: executar um único shutdown preflight; manter implementação
  e testes inalterados; executar uma vez o teste focal com saída e exit code
  duráveis; somente após `PASS`, executar uma vez `Doctor`, `Quick` e um único
  `Full` online; parar sem retry, diagnóstico executável ou correção em linha no
  primeiro `FAIL` ou `BLOCKED`; reconciliar somente os três documentos factuais.
- Preflight e baseline: o único shutdown inicial aprovou com exit code `0`, zero
  processo correspondente e zero listener próprio. Branch, HEAD, index,
  worktree rastreada e inventário não ignorado corresponderam à baseline limpa.
- Identidade congelada: o runner consolidado conservou SHA-256
  `F88CC8B090967CE666CA57C62BE8949D37B91C1797CF9D1D1B2E1E40F3FFBFF1`; o
  teste focal conservou SHA-256
  `0F2CD49EEB1D87C7986F6F60F793D8E4E8F2603F0793756C00029B0A614857BF`.
- Primeira tentativa executável: o limite local de execução rejeitou o
  invólucro de captura durável antes de `CreateProcess`. Nenhum processo
  PowerShell ou de teste foi criado e o diretório ignorado
  `.dotnet/evidence/AUD-2026-R1-R5-R2/` permaneceu ausente.
- Stop factual: a rejeição foi preservada sem simplificar, corrigir ou repetir a
  invocação. O teste focal, `Doctor`, `Quick` e o `Full` online condicional estão
  `NOT_RUN`; nenhuma dessas autorizações foi consumida.
- Disposição: `AUD-2026-R1-R5-R2 BLOCKED` por `ISOLATION_FAILURE`. Não existe
  novo resultado executável para o candidato commitado.
- Escopo negativo preservado: implementação, testes, scripts, workflows,
  versões, dependências, manifests, lockfiles, integridades, contratos e schemas
  permaneceram inalterados. Nenhum banco/provider real, navegador comum, deploy,
  push, Human Gate, ativação ou transição de `STATE` foi executado.
- Estado resultante: `STATE-06 INTEGRATION`, elegibilidade `NÃO REAVALIADA`,
  `MOD-12 ActivationState=None`, providers, ADRs, Human Gates, produto,
  interface e autoridade externa permanecem inalterados.
