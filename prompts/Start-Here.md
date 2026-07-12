# DB-Notifier — Sistema de Instruções

## Finalidade

Este é o ponto de entrada obrigatório para trabalhos orientados pelo corpus do DB-Notifier. O sistema separa visão, arquitetura, governança, segurança, ciclo de desenvolvimento, qualidade, playbooks, estado corrente, histórico e templates.

## Ordem mínima de leitura

1. [`foundation/Prompt-New-Project.md`](foundation/Prompt-New-Project.md): visão e limites do produto.
2. [`state/Current-State.md`](state/Current-State.md): situação factual do workspace.
3. [`governance/Governance.md`](governance/Governance.md): autoridade, estados e regras de execução.
4. Abrir somente os documentos temáticos necessários à tarefa.

## Roteamento

| Necessidade | Documento |
|---|---|
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
| Histórico de transições | `state/State-Transition-Log.md` |
| Handoff, relatórios, auditoria e ADR | `templates/Templates.md` |
| Versão e histórico do corpus | `system/Prompt-System-Change-Log.md` |

## Precedência

Em caso de conflito, aplicar nesta ordem:

1. Pedido atual e explícito do usuário.
2. Segurança, proteção de dados e limites de autorização.
3. Visão do produto.
4. Estado corrente.
5. Governança e ciclo de desenvolvimento.
6. Arquitetura e segurança específicas.
7. Playbook selecionado.
8. Templates e histórico.

Conflitos que ampliem materialmente o escopo, exijam ação externa irreversível ou reduzam segurança devem ser apresentados ao usuário antes da execução.

## Regras universais

- Não inventar implementação, ambiente, teste, evidência, credencial ou aprovação.
- Não declarar suporte a um banco apenas porque consta do roadmap.
- Não executar deploy, migration remota, instalação, publicação ou controle de banco real sem autorização específica.
- Separar credenciais de monitoramento das credenciais administrativas.
- Preservar mudanças preexistentes e limitar alterações ao escopo autorizado.
- Documentar código e configuração exclusivamente em inglês britânico (`en-GB`), conforme `../docs/Code-Documentation-Standards.md`, mantendo comentários concisos e sincronizados.
- Aplicar `../docs/design/DB-Notifier-Design-System.md` a todo frontend novo ou alterado; não criar temas, tokens ou componentes paralelos fora do contrato oficial.
- Consultar o estado antes de executar uma fase ou playbook.
- Atualizar estado e histórico somente quando houver mudança factual.

## Estrutura ativa

O corpus contém 13 arquivos ativos. Um novo arquivo só deve ser criado quando o conteúdo tiver autoridade, ciclo de vida ou público diferente dos documentos existentes. Caso contrário, adicionar uma seção ao documento temático apropriado.
