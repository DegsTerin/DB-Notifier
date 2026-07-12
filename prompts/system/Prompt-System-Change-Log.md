# Controle e Changelog do Sistema de Instruções

## Versão atual

- Versão: `3.2.1`
- Data: 2026-07-11
- Status: corpus DB-Notifier consolidado
- Escopo: 13 arquivos ativos

A versão do corpus é independente da versão do software.

## Política SemVer

- MAJOR: mudança incompatível de autoridade, precedência, estados ou estrutura.
- MINOR: nova capacidade, playbook, módulo ou gate sem quebrar o fluxo.
- PATCH: clareza, correção ou referência sem mudança de autoridade.

Toda alteração atualiza este arquivo e, quando necessário, `../Start-Here.md`.

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
