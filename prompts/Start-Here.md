# DB-Notifier — Sistema de Instruções

## Finalidade

Este é o ponto de entrada obrigatório para trabalhos orientados pelo corpus do DB-Notifier. O sistema separa visão, método geral de engenharia assistida por IA, arquitetura, governança, segurança, ciclo de desenvolvimento, qualidade, playbooks, estado corrente, histórico e templates.

Agentes que operam no repositório começam também por [`../AGENTS.md`](../AGENTS.md), fonte operacional principal das instruções permanentes e reutilizáveis. O `AGENTS.md` consolida regras transversais e encaminha para este corpus; não substitui a autoridade temática detalhada, os ADRs aceitos nem a evidência factual.

## Ordem mínima de leitura

1. [`foundation/Prompt-New-Project.md`](foundation/Prompt-New-Project.md): visão e limites do produto.
2. [`state/Current-State.md`](state/Current-State.md): situação factual do workspace.
3. [`governance/Governance.md`](governance/Governance.md): autoridade, estados e regras de execução.
4. [`system/AI-Software-Engineering-Master-Prompt.md`](system/AI-Software-Engineering-Master-Prompt.md): leitura obrigatória para projeto novo, auditoria ampla, reorganização material ou trabalho transversal; em ajuste focal, aplicar a síntese permanente de `AGENTS.md` e abrir a seção temática roteada quando necessária.
5. [`governance/Language-Policy.md`](governance/Language-Policy.md):
   leitura obrigatória antes de comunicação governada com o proprietário e
   antes de criar ou alterar artefatos pertencentes ao projeto.
6. [`governance/Conversation-Coordination-Prompt.md`](governance/Conversation-Coordination-Prompt.md):
   leitura obrigatória antes de todo despacho automático e antes de avaliar ou
   iniciar trabalho multiagente.
7. Abrir somente os demais documentos temáticos necessários à tarefa.

Em trabalho amplo, transversal, multi-incremento ou que combine auditoria e
remediação, ler também [`../PLANS.md`](../PLANS.md) antes da implementação e
mantê-lo sincronizado. O plano é ledger operacional não autorizante: divergência
com estado ou autoridade interrompe a execução e se resolve pela fonte
proprietária, nunca pelo plano.

## Roteamento

| Necessidade | Documento |
|---|---|
| Instruções permanentes e comportamento operacional de agentes | `../AGENTS.md` |
| Plano vivo do incremento amplo corrente, escopo, progresso, achados e evidências | `../PLANS.md` |
| Comunicação com o proprietário, idioma dos artefatos, conteúdo existente, convenções externas e separação da interface | `governance/Language-Policy.md` |
| Despacho automático, paralelismo seguro, ownership, receipts, deduplicação e integração coordenada | `governance/Conversation-Coordination-Prompt.md` |
| Método geral de engenharia, papéis virtuais, proporcionalidade, modos de trabalho e matriz de adoção | `system/AI-Software-Engineering-Master-Prompt.md` |
| Visão, escopo e objetivos | `foundation/Prompt-New-Project.md` |
| Arquitetura, dados, providers e módulos | `foundation/Solution-Architecture-Document.md` |
| MOD-12 AIOPS_AI, modelos estatísticos, LLM e automação controlada | `foundation/AIOps-And-AI-Module.md` |
| Autoridade, estados, bloqueio, rollback e memória | `governance/Governance.md` |
| Fases, entregáveis e critérios por estado | `governance/Lifecycle.md` |
| Evidências, auditoria, DoD e Agent Gate | `governance/Quality-Gates.md` |
| Credenciais, autenticação, RBAC e auditoria | `governance/Security-And-Access.md` |
| Auditoria completa, ajustes, UI/UX e reestruturação | `operations/Operational-Playbooks.md` |
| Uso de `Doctor`, `Setup`, `Quick`, `Full`, `PlanOnly` e gate local canônico | `operations/Operational-Playbooks.md`, `governance/Quality-Gates.md` e `../docs/Development.md` |
| Design System, temas, tokens e componentes React/WPF | `../docs/design/DB-Notifier-Design-System.md` |
| Situação atual | `state/Current-State.md` |
| Evidência histórica e ratificação retrospectiva de Human Gates anteriores | `../docs/Human-Gate-Retrospective-Ratification.md` |
| Histórico de transições | `state/State-Transition-Log.md` |
| Handoff, relatórios, auditoria e ADR | `templates/Templates.md` |
| Versão e histórico do corpus | `system/Prompt-System-Change-Log.md` |

## Precedência

Em caso de conflito, aplicar nesta ordem:

1. Instruções da plataforma, sistema e desenvolvedor.
2. Pedido atual e explícito do proprietário.
3. Segurança, proteção de dados, clean-room, limites jurídicos e pré-requisitos
   externos ou de autoridade superior que não podem ser dispensados.
4. Instruções aplicáveis ao diretório, da mais específica para a mais geral,
   incluindo `AGENTS.override.md` quando existir e `AGENTS.md`.
5. Estado corrente factual.
6. Visão do produto, governança e lifecycle canônico.
7. Decisões explícitas da matriz PM-1, que substituem instruções preexistentes
   nos conflitos que a própria matriz resolve.
8. Autoridades temáticas específicas de idioma em
   `governance/Language-Policy.md`, coordenação de conversas em
   `governance/Conversation-Coordination-Prompt.md`, arquitetura, segurança,
   qualidade, dados, Design System ou ADR aceito para itens classificados como
   `JÁ GOVERNADO` ou para a especialização descrita em um item `ADAPTADO`.
9. Baseline genérica do Prompt Mestre onde não houver especialização.
10. Playbook selecionado.
11. Templates, evidência histórica e convenções inferidas.

Conflitos que ampliem materialmente o escopo, exijam ação externa irreversível ou reduzam segurança devem ser apresentados ao usuário antes da execução.

## Regras universais

- Não inventar implementação, ambiente, teste, evidência, credencial ou aprovação.
- Não declarar suporte a um banco apenas porque consta do roadmap.
- Não executar deploy, migration remota, instalação, publicação ou controle de banco real sem autorização específica.
- Separar credenciais de monitoramento das credenciais administrativas.
- Preservar mudanças preexistentes e limitar alterações ao escopo autorizado.
- Antes de iniciar cada nova ação técnica autorizada sobre o código, workspace ou produto, aplicar o shutdown preflight obrigatório definido em `../AGENTS.md` e detalhado em `operations/Operational-Playbooks.md`. Conversa, pergunta, explicação ou status sem ação técnica não dispara encerramento.
- Aplicar `governance/Language-Policy.md` a toda comunicação com o
  proprietário e a todo artefato criado ou alterado. Comunicação, rótulos,
  orientações e relatórios finais usam `pt-BR`; novos artefatos independentes
  pertencentes ao projeto usam `en-GB`; alterações limitadas preservam o
  idioma estabelecido do arquivo; convenções externas não são traduzidas; e o
  idioma da interface permanece uma decisão de produto separada.
- Aplicar `governance/Conversation-Coordination-Prompt.md` a todo despacho e
  trabalho multiagente. Sem workflow Git de escrita paralela autorizado e
  worktrees isolados, agentes simultâneos permanecem read-only e toda escrita
  ocorre sequencialmente na coordenadora.
- Despachar diretamente toda continuação por `CONTINUE_CURRENT`,
  `DELEGATE_SUBAGENT`, `RETURN_TO_EXISTING` ou `START_NEW_AUTO_DISPATCH`.
  Registrar receipt, reconciliar resultados incertos antes de retry, impedir
  duplicidade e nunca produzir texto para o proprietário copiar ou encaminhar.
- Aplicar decisões de desenvolvimento prospectivas por Agent Gate objetivo.
  Preservar Human Gates anteriores como fatos históricos, sem mantê-los como
  dependência de execução futura.
- Documentar código e configuração exclusivamente em inglês britânico (`en-GB`), conforme `../docs/Code-Documentation-Standards.md`, mantendo comentários concisos e sincronizados.
- Aplicar `../docs/design/DB-Notifier-Design-System.md` a todo frontend novo ou alterado; não criar temas, tokens ou componentes paralelos fora do contrato oficial.
- Consultar o estado antes de executar uma fase ou playbook.
- Fechar o envelope da tarefa antes de implementar: baseline, autoridade,
  escopos positivo e negativo, trabalho protegido, ownership, recursos
  mutáveis, checks, revisores e stop codes devem estar explícitos.
- Usar `../scripts/development.ps1` como entrada local canônica. `Quick` é
  `NON_GATE`; somente `Full` delega ao agregador `../scripts/ci.ps1`.
- Atualizar estado e histórico somente quando houver mudança factual.

## Estrutura ativa

O corpus contém 16 arquivos ativos. As autoridades especializadas de idioma e
coordenação de conversas ficam em `governance/`, com escopo e público próprios,
enquanto `Governance.md` permanece proprietário da autoridade, execução
controlada e lifecycle. O Prompt Mestre incorporado conserva autoridade
transversal distinta: preserva a baseline geral e encaminha especializações
aos documentos proprietários, sem criar um lifecycle paralelo. Um novo arquivo
só deve ser criado quando o conteúdo tiver autoridade, ciclo de vida ou
público diferente dos documentos existentes. Caso contrário, adicionar uma
seção ao documento temático apropriado. `PLANS.md` e os scripts de execução
ficam fora da contagem dos 16 prompts ativos porque são, respectivamente,
ledger não autorizante e implementação verificável das autoridades temáticas.
