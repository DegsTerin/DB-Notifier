# Controle e Changelog do Sistema de Instruções

## Versão atual

- Versão: `3.25.0`
- Data: 2026-07-12
- Status: corpus DB-Notifier consolidado
- Escopo: 13 arquivos ativos

A versão do corpus é independente da versão do software.

## Política SemVer

- MAJOR: mudança incompatível de autoridade, precedência, estados ou estrutura.
- MINOR: nova capacidade, playbook, módulo ou gate sem quebrar o fluxo.
- PATCH: clareza, correção ou referência sem mudança de autoridade.

Toda alteração atualiza este arquivo e, quando necessário, `../Start-Here.md`.

## 3.25.0 — 2026-07-12

- `AGENTS.md` raiz criado como fonte operacional principal das instruções permanentes e reutilizáveis do repositório.
- Regras transversais de .NET 10 LTS, providers abertos, segurança, comentários en-GB, compatibilidade, Design System, qualidade, Docker e commits consolidadas sem substituir autoridades temáticas.
- `Start-Here.md` e índices atualizados para o novo roteamento; os 13 prompts ativos, ADRs, especificações e relatórios históricos permanecem separados por autoridade e ciclo de vida.
- Nenhum arquivo de instrução removido ou renomeado; nenhuma fase, Human Gate ou autorização externa alterada.

## 3.24.0 — 2026-07-12

- Primeiro incremento do Design System: schema e tokens core/semânticos/componentes canônicos com geração determinística CSS/XAML.
- Contratos System/Light/Dark equivalentes em TypeScript e .NET 10, com valores estáveis de persistência e falha segura para System.
- Gates de drift, paridade e contraste aprovados com 133 testes .NET e 12 Dashboard; aplicação visual React/WPF permanece pendente.

## 3.23.0 — 2026-07-12

- Design System oficial `1.0.0` formalizado antes do Human Gate com identidade moderna, limpa, profissional e sem estética sci-fi/neon.
- Arquitetura canônica de tokens e temas Light/Dark/System, persistência, componentes, WCAG 2.2 AA e paridade React/WPF especificadas.
- Implementação e nova reauditoria adicionadas ao escopo pendente de `STATE-05`; nenhuma transição ou integração externa autorizada.

## 3.22.0 — 2026-07-12

- `S05-AUD-001` remediado com contenção intrínseca do layout; todas as rotas/viewports da reauditoria passaram sem overflow global.
- `S05-AUD-002` remediado com diálogo nativo, foco inicial, Tab/Shift+Tab contidos, Escape e restauração ao acionador.
- Reauditoria automática `APROVADA` com 125 testes .NET, 8 Dashboard e 10 Pester; Human Gate, `STATE-06` e laboratório multi-banco permanecem pendentes.

## 3.21.0 — 2026-07-12

- Auditoria automática de encerramento de `STATE-05` executada com Chrome/CDP, viewports, teclado, árvore acessível, estados/reduced-motion e Windows UI Automation.
- Gate `REPROVADO` por overflow horizontal em 390/320 px e diálogo modal sem gestão de foco/Escape; inventário humano de leitor de tela permanece pendente.
- Estado preservado em `STATE-05`; Human Gate, `STATE-06` e laboratório multi-banco continuam bloqueados até remediação e reauditoria aprovada.

## 3.20.0 — 2026-07-12

- Padrão global de documentação de código em inglês britânico incorporado às regras universais e aos gates de qualidade.
- Cabeçalhos de módulo aplicados aos fontes manuais; exceções permanecem explícitas para formatos estritos, código gerado e migrations já aplicadas.
- Gate `comments:verify` integrado ao Dashboard e ao CI, sem alterar o estado `STATE-05` nem antecipar o Human Gate.

## 3.19.0 — 2026-07-12

- Quarto incremento de `STATE-05`: Tray Windows seguro para a janela DB-Notifier, sem controle de banco/serviço e com saída/descarte explícitos.
- Guards automatizados de contraste WCAG AA, semântica, foco, reduced-motion e política provider-neutral do Tray adicionados.
- 125 testes .NET e 7 testes Dashboard aprovados; próximo passo definido como auditoria automática de encerramento de `STATE-05` antes do Human Gate.

## 3.18.0 — 2026-07-12

- Terceiro incremento de `STATE-05`: contrato `configuration-capabilities.v1` e configuração/capabilities no Dashboard e WPF .NET 10.
- Previews distinguem confirmation/denied/unsupported/unavailable/unknown; nenhuma configuração, secret, mutation ou ação administrativa é executada.
- 114 testes .NET e 5 testes Dashboard aprovados; próximo incremento definido como Tray/notification area e reforço de acessibilidade.

## 3.17.0 — 2026-07-12

- Segundo incremento de `STATE-05`: contrato `history-alerts.v1`, timeline e alertas provider-neutral no Dashboard e WPF .NET 10.
- Busca/filtro de eventos, severidade por texto/símbolo, estados de alerta e manutenção implementados com adapters determinísticos e sem mutations/canais externos.
- 109 testes .NET e 4 testes Dashboard aprovados; próximo incremento definido como configuração e confirmação capability-aware sem execução administrativa.

## 3.16.0 — 2026-07-12

- Primeiro incremento de `STATE-05`: contrato de apresentação `inventory.v1` e inventário/status somente leitura no Dashboard React e Desktop WPF .NET 10.
- Estados ready/loading/empty/offline/error/stale/denied, suporte factual, timestamps, stale, foco/teclado, semântica e responsividade implementados com adapters determinísticos sem integração externa.
- 107 testes .NET e 3 testes Dashboard aprovados; próximo incremento definido como histórico/timeline e alertas provider-neutral, sem mutations ou execução administrativa.

## 3.15.0 — 2026-07-12

- Human Gate de `STATE-04 BACKEND_IMPLEMENTATION` aprovado explicitamente após revisão da reauditoria, das falhas representativas de provider, da autorização negativa e da sanitização do migrador.
- Transição factual para `STATE-05 FRONTEND_IMPLEMENTATION`, autorizando Tray/Desktop e Dashboard sem antecipar integração externa, execução administrativa ou homologação.
- Primeiro incremento de UI definido como fundação visual/contratos de apresentação e vertical slice somente leitura de inventário/status com estados operacionais e acessibilidade.

## 3.14.0 — 2026-07-12

- Oitavo incremento/remediação de `STATE-04`: migrador isolado .NET 10 com dry-run, backups, escrita atômica, relatório/manifesto, idempotência, rollback por hash e bloqueio de secrets.
- Discovery tipado de `pg_isready`, fallback de transporte, expiração de credencial e fixtures negativas implementados sem shell ou controle administrativo.
- Reauditoria automática `APROVADO` com 104 testes .NET; homologação PostgreSQL permanece `None`, suporte público `No` e Human Gate pendente.

## 3.13.0 — 2026-07-12

- Auditoria automática de encerramento de `STATE-04` executada sem transição; build/testes/segurança/dependências aprovados, gate geral `REPROVADO` por incompletude do M4.
- Bloqueadores registrados: migrador seguro PgNotifier → DB-Notifier ausente, discovery PostgreSQL caracterizado incompleto e fixtures negativas M4 insuficientes.
- Drift da matriz de capacidades registrado; próximo passo alterado para incremento de remediação seguido de nova auditoria, antes do Human Gate.

## 3.12.0 — 2026-07-12

- Sétimo incremento de `STATE-04`: polling/ack mTLS, idempotente, versionado e limitado, com inbox durável e sem executor administrativo.
- Discovery de pacotes fail-closed com manifesto `net10.0`, chave pública confiável, assinatura RSA-PSS/SHA-256, hashes, limites e rejeição de paths/links inseguros; nenhum código é carregado automaticamente.
- 86 testes .NET aprovados; nenhum `CommandAttempt`, Start/Stop/Restart, assembly externo, banco, certificado real ou provider homologado foi exercitado.

## 3.11.0 — 2026-07-12

- Sexto incremento de `STATE-04`: retenção Agent/central limitada, opt-in e dry-run por default, sem apagar audit ou dados referenciados/não publicados.
- Server-outbox e notifications com batches, backpressure, retry/backoff e IDs deduplicáveis; nenhum adapter externo registrado automaticamente.
- Consulta humana de auditoria com `audit.read` Global, filtros/paginação limitados, rate limit e auditoria do próprio acesso.
- 80 testes .NET aprovados; nenhuma deleção produtiva, canal externo, PostgreSQL, IdP ou ação administrativa foi exercitada.

## 3.10.0 — 2026-07-12

- Quinto incremento de `STATE-04`: autenticação humana OIDC/JWT externa, fail-closed e rate-limited, separada do certificado de Agent.
- RBAC server-side por usuário ativo, permission code, expiração e escopos Global/Environment/Instance; catálogo mínimo sem credentials/endpoints.
- Criação de comandos idempotentes e auditados somente em `Pending`, exigindo capability/version exatos e sem dispatch, attempt ou execução administrativa.
- 75 testes .NET aprovados; nenhum IdP, token, PostgreSQL, certificado, comando real ou provider homologado foi exercitado.

## 3.9.1 — 2026-07-12

- Auditoria de certificação do quarto incremento com 67 testes .NET aprovados e subset de sincronização 21/21.
- Respostas `2xx` inválidas, timeouts e resultados ausentes permanecem retryable e não tombstonam dados locais prematuramente.
- Cobertura ampliada para ordem monotônica, tombstone, Agent/instance scope, transições canônicas e autorização positiva/negativa.

## 3.9.0 — 2026-07-12

- Quarto incremento de `STATE-04`: outbox dispatch/ack ordenado, retry limitado e sincronização HTTPS/mTLS opt-in.
- Ingestão central idempotente com vínculo Agent/instância, sequência única, eventos canônicos e preparação de alert deliveries atômicos.
- Primeira API operacional protegida por certificado e autorização exata do Agent na rota; negação sem certificado validada em runtime local.
- 56 testes .NET aprovados após hardening de HTTPS, sequência e autorização. Nenhum PostgreSQL, certificado real, vault, alvo monitorado ou canal externo foi exercitado.

## 3.8.0 — 2026-07-11

- Terceiro incremento de `STATE-04`: initializer SQLite controlado, assignment source, worker recorrente e telemetria estruturada.
- Readers reais e read-only para Windows Credential Manager e Linux Secret Service, selecionados por composite sem fallback plaintext.
- Monitoring desabilitado por default; habilitação exige AgentId e pode migrar somente o store SQLite interno configurado.
- 46 testes .NET aprovados; nenhum banco, rede ou credential real executado e nenhum provider homologado.

## 3.7.0 — 2026-07-11

- Segundo incremento de `STATE-04`: retry/backoff limitado, ciclo isolado, credential lease/vault port e health PostgreSQL autenticado.
- Npgsql usa TLS `require`/`verify-full`, pooling desativado, query fixa e timeout; sem banco ou credencial real executados.
- Observation, sequência e outbox SQLite persistidos em uma transação, com rollback/idempotência e payload sem secrets testados.
- 40 testes .NET aprovados; scheduler recorrente, vault real, store initialization, API/RBAC/comandos e homologação permanecem pendentes.

## 3.6.0 — 2026-07-11

- Primeiro incremento de `STATE-04`: Domain/Application neutros, Provider SDK/registro aberto e probe canônico implementados.
- Adapter PostgreSQL de readiness implementado como validação concreta, sem dependência no núcleo e sem homologação pública.
- `pg_isready` executado sem shell, timeout limitado e fallback TCP sempre mapeado como `Degraded` quando alcançável.
- 30 testes aprovados e limites explícitos: sem scheduler, banco real, vault, controle administrativo, API funcional ou UI.

## 3.5.2 — 2026-07-11

- Human Gate de `STATE-03 DATABASE_MODELING` aprovado explicitamente pelo product owner, com as ressalvas técnicas preservadas.
- Ordem consolidada: roadmap universal e SAP HANA/SQLite, conectividade/credenciais e linhagem já formalizados antes do gate.
- Transição factual para `STATE-04 BACKEND_IMPLEMENTATION`, sem iniciar silenciosamente backend, migration produtiva, deploy ou ação real.
- Próximo incremento definido como núcleo provider-neutral e vertical slice PostgreSQL em .NET 10.

## 3.5.1 — 2026-07-11

- Linhagem histórica registrada pelo product owner: MySQL Notifier inspirou conceitualmente o PgNotifier, sucedido pelo DB-Notifier.
- Inspiração separada explicitamente de código reutilizado, compatibilidade técnica, afiliação ou dependência.
- Estado preservado em `STATE-03`; nenhuma mudança de implementação ou gate.

## 3.5.0 — 2026-07-11

- Cobertura de monitoramento formalizada para bancos locais, remotos, Windows, Linux, containers, datacenter, híbridos e cloud.
- Conexão provider-specific atribuída ao Agent, sem transformar API/Dashboard em proxy genérico de banco.
- Identidades separadas para monitoramento, administração do banco, serviço do sistema operacional e plano de controle cloud.
- Cofres Linux e identidade federada/workload cloud incluídos, sem autorizar segredo em claro ou abertura automática de rede.

## 3.4.0 — 2026-07-11

- Objetivo do produto ampliado explicitamente para aceitar qualquer motor de banco por provider/plugin versionado.
- Priorização inicial dos bancos mais utilizados e conhecidos, incluindo SAP HANA e SQLite como futuros alvos monitoráveis.
- Catálogo declarado aberto a engines relacionais, NoSQL, distribuídas, embarcadas, especializadas, cloud-managed e futuras.
- Separação preservada entre presença no roadmap, implementação, homologação e suporte público; estado mantido em `STATE-03`.

## 3.3.0 — 2026-07-11

- Registro factual da modelagem interna SQLite/PostgreSQL concluída e auditada em `STATE-03`.
- Separação explícita dos assemblies de persistência do Agent e Server, sem provider cruzado nos runtimes.
- Inclusão de modelo, constraints, índices, retenção, migrations e recuperação como evidências; nenhuma migration produtiva autorizada.
- Human Gate de `STATE-03` mantido pendente antes de qualquer transição para backend.

## 3.2.2 — 2026-07-11

- Aceitação explícita de ADR-0001 a ADR-0006 e do pacote arquitetural completo.
- Encerramento do Human Gate de `STATE-02 ARCHITECTURE`.
- Transição factual para `STATE-03 DATABASE_MODELING`, sem autorizar migrations produtivas ou backend.

## 3.2.1 — 2026-07-11

- Aceitação explícita do ADR-0001 com baseline única .NET 10 LTS.
- Atualização da visão, arquitetura e estado para proibir targets ativos anteriores sem novo ADR.
- Retarget dos 10 projetos e renovação de restore, build, testes, format, auditoria e liveness em .NET 10.
- Preservação das menções .NET 8 somente como histórico factual supersedido.

## 3.2.0 — 2026-07-11

- Proposta do pacote arquitetural completo de `STATE-02` com seis ADRs.
- Definição de contratos canônicos, protocolo Agent/API, threat model e matriz PostgreSQL.
- Inclusão de guardrails de dados, risco, avaliações e promoção de modos para MOD-12 AIOPS_AI.
- Atualização factual do estado sem pré-aprovar ADRs nem avançar para modelagem.

## 3.1.6 — 2026-07-11

- Registro da aprovação explícita do Human Gate de Project Setup.
- Transição factual de `STATE-01 PROJECT_SETUP` para `STATE-02 ARCHITECTURE`.
- Atualização do próximo gate e dos entregáveis arquiteturais pendentes, sem antecipar implementação.

## 3.1.5 — 2026-07-11

- Registro do commit inicial e da migração canônica PgNotifier → DB-Notifier.
- Preservação explícita de shims, configuração antiga e rollback side-by-side.
- Inclusão do build canônico e das evidências de compatibilidade, sem promover o estado.

## 3.1.4 — 2026-07-11

- Registro da instalação autorizada do SDK .NET 8 isolado no workspace.
- Atualização factual após restore, build, testes, format, auditoria NuGet e liveness aprovados.
- Auditoria automática de `STATE-01` aprovada; Human Gate mantido pendente.

## 3.1.3 — 2026-07-11

- Atualização factual de `STATE-01` após criação do scaffold modular e da CI inicial.
- Registro dos checks aprovados de Dashboard/legado e do bloqueio por ausência do SDK .NET 8.
- Estado preservado em `STATE-01`, com Human Gate de saída pendente.

## 3.1.2 — 2026-07-11

- Registro da aprovação explícita do Human Gate de descoberta.
- Transição factual de `STATE-00 DISCOVERY_MIGRATION` para `STATE-01 PROJECT_SETUP`.
- Registro da autorização para inicialização de Git e das limitações de ambiente ainda existentes.

## 3.1.1 — 2026-07-11

- Atualização factual de `Current-State.md` após inventário e plano incremental do legado.
- Registro do fechamento técnico da descoberta, sem transição e com Human Gate pendente.
- Referência às evidências de migração mantidas em `docs/` sem ampliar o corpus ativo.

## 3.1.0 — 2026-07-11

- Inclusão de `MOD-12 AIOPS_AI` como capacidade de longo prazo.
- Definição de coleta, regras, estatística, correlação, conhecimento, LLM, planejamento, aprovação, executor e feedback.
- Inclusão de governança de modelos, avaliações, prompt injection, modos operacionais e automação baseada em risco.
- Integração do módulo ao índice, arquitetura, lifecycle, segurança e estado corrente.

## 3.0.0 — 2026-07-11

- Consolidação de 77 arquivos em 12 documentos ativos.
- Redução de contexto, segurança e critérios repetidos.
- Unificação das oito fases em `Lifecycle.md`.
- Unificação de auditorias automáticas e Human Gates em `Quality-Gates.md`.
- Separação preservada entre estado corrente e log append-only.
- Criação de documentos temáticos para governança, segurança, playbooks e templates.
- Atualização integral das referências e da hierarquia.

## 2.0.0 — 2026-07-11

- Migração conceitual de 75 prompts herdados para DB-Notifier.
- Remoção do domínio e das evidências do projeto de origem.
- Criação de `STATE-00 DISCOVERY_MIGRATION`.
- Adaptação das fases e módulos para Agent, API, Tray/Desktop, Dashboard e Provider SDK.

## Registro da instrução de migração

O antigo `Prompt-Adapt-Doc.md` foi uma instrução temporária: ler a visão do novo projeto, revisar 75 prompts herdados, remover dependências do projeto anterior, padronizar o conteúdo e emitir relatório. A tarefa foi concluída na versão 2.0.0 e, por isso, não permanece como instrução ativa.

## Auditoria da versão 3.1.0

Critérios obrigatórios:

- Exatamente 13 arquivos Markdown em `prompts/`.
- Todos os links Markdown internos resolvem.
- `Start-Here.md` roteia para todos os documentos.
- Estado e histórico permanecem separados.
- Nenhum nome retirado é usado como dependência ativa.
- Documentos não contêm trailing whitespace.
- Termos do projeto anterior aparecem somente em registro histórico, se necessário.

Auditoria documental não comprova implementação do produto.
