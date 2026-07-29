# DB-Notifier — Sistema de Instruções

## Finalidade

Este é o ponto de entrada obrigatório para trabalhos orientados pelo corpus do DB-Notifier. O sistema separa visão, método geral de engenharia assistida por IA, arquitetura, governança, segurança, ciclo de desenvolvimento, qualidade, playbooks, estado corrente, histórico e templates.

Agentes que operam no repositório começam também por [`../AGENTS.md`](../AGENTS.md), fonte operacional principal das instruções permanentes e reutilizáveis. O `AGENTS.md` consolida regras transversais e encaminha para este corpus; não substitui a autoridade temática detalhada, os ADRs aceitos nem a evidência factual.

## Ordem mínima de leitura

1. [`foundation/Prompt-New-Project.md`](foundation/Prompt-New-Project.md): visão e limites do produto.
2. [`state/Current-State.md`](state/Current-State.md): situação factual do workspace.
3. [`governance/Governance.md`](governance/Governance.md): autoridade, estados e regras de execução.
4. [`system/AI-Software-Engineering-Master-Prompt.md`](system/AI-Software-Engineering-Master-Prompt.md): leitura obrigatória para projeto novo, auditoria ampla, reorganização material ou trabalho transversal; em ajuste focal, aplicar a síntese permanente de `AGENTS.md` e abrir a seção temática roteada quando necessária.
5. [`../Conversation-Coordination-Prompt.md`](../Conversation-Coordination-Prompt.md):
   leitura obrigatória antes de todo handoff governado e antes de avaliar ou
   iniciar trabalho com múltiplas conversas.
6. Abrir somente os demais documentos temáticos necessários à tarefa.

## Roteamento

| Necessidade | Documento |
|---|---|
| Instruções permanentes e comportamento operacional de agentes | `../AGENTS.md` |
| Roteamento de conversas, handoff, paralelismo seguro, ownership e integração coordenada | `../Conversation-Coordination-Prompt.md` |
| Método geral de engenharia, papéis virtuais, proporcionalidade, modos de trabalho e matriz de adoção | `system/AI-Software-Engineering-Master-Prompt.md` |
| Visão, escopo e objetivos | `foundation/Prompt-New-Project.md` |
| Arquitetura, dados, providers e módulos | `foundation/Solution-Architecture-Document.md` |
| MOD-12 AIOPS_AI, modelos estatísticos, LLM e automação controlada | `foundation/AIOps-And-AI-Module.md` |
| Autoridade, estados, bloqueio, rollback e memória | `governance/Governance.md` |
| Fases, entregáveis e critérios por estado | `governance/Lifecycle.md` |
| Evidências, auditoria, DoD e Human Gate | `governance/Quality-Gates.md` |
| Credenciais, autenticação, RBAC e auditoria | `governance/Security-And-Access.md` |
| Auditoria completa, ajustes, UI/UX e reestruturação | `operations/Operational-Playbooks.md` |
| Design System, temas, tokens e componentes React/WPF | `../docs/design/DB-Notifier-Design-System.md` |
| Situação atual | `state/Current-State.md` |
| Ratificação retrospectiva dos Human Gates contestados | `../docs/Human-Gate-Retrospective-Ratification.md` |
| Histórico de transições | `state/State-Transition-Log.md` |
| Handoff, relatórios, auditoria e ADR | `templates/Templates.md` |
| Versão e histórico do corpus | `system/Prompt-System-Change-Log.md` |

## Precedência

Em caso de conflito, aplicar nesta ordem:

1. Instruções da plataforma, sistema e desenvolvedor.
2. Pedido atual e explícito do proprietário.
3. Segurança, proteção de dados, autorização externa e gates de lifecycle que
   uma solicitação comum não pode dispensar silenciosamente.
4. Instruções aplicáveis ao diretório, da mais específica para a mais geral,
   incluindo `AGENTS.override.md` quando existir e `AGENTS.md`.
5. Estado corrente factual.
6. Visão do produto, governança e lifecycle canônico.
7. Decisões explícitas da matriz PM-1, que substituem instruções preexistentes
   nos conflitos que a própria matriz resolve.
8. Autoridade temática específica de coordenação de conversas em
   `../Conversation-Coordination-Prompt.md`, arquitetura, segurança, qualidade,
   dados, Design System ou ADR aceito para itens classificados como
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
- Aplicar `../Conversation-Coordination-Prompt.md` a todo handoff e trabalho com
  múltiplas conversas. Sem workflow Git de escrita paralela especificamente
  autorizado e worktrees isolados, conversas simultâneas permanecem read-only
  e toda escrita ocorre sequencialmente na coordenadora.
- Documentar código e configuração exclusivamente em inglês britânico (`en-GB`), conforme `../docs/Code-Documentation-Standards.md`, mantendo comentários concisos e sincronizados.
- Aplicar `../docs/design/DB-Notifier-Design-System.md` a todo frontend novo ou alterado; não criar temas, tokens ou componentes paralelos fora do contrato oficial.
- Consultar o estado antes de executar uma fase ou playbook.
- Atualizar estado e histórico somente quando houver mudança factual.

## Estrutura ativa

O corpus contém 15 arquivos ativos. A autoridade especializada de coordenação
de conversas possui escopo e público próprios, enquanto `Governance.md`
permanece proprietário da autoridade, execução controlada e lifecycle. O
Prompt Mestre incorporado conserva autoridade transversal distinta: preserva a
baseline geral e encaminha especializações aos documentos proprietários, sem
criar um lifecycle paralelo. Um novo arquivo só deve ser criado quando o
conteúdo tiver autoridade, ciclo de vida ou público diferente dos documentos
existentes. Caso contrário, adicionar uma seção ao documento temático
apropriado.
