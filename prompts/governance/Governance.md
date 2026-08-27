# Governança e Execução Controlada

## Autoridade

A precedência global está em `../Start-Here.md`. O Prompt Mestre incorporado em
`../system/AI-Software-Engineering-Master-Prompt.md` fornece o método geral e
sua matriz de adoção; este documento permanece proprietário da autoridade,
execução controlada e estados canônicos. Nenhum documento histórico ou
template altera o estado do projeto.

A
[`Política de Idioma`](Language-Policy.md) é a autoridade temática única para
comunicação com o proprietário, idioma dos artefatos, preservação de conteúdo
existente, convenções externas e separação do idioma da interface. Ela não
altera autoridade, execução, estados ou gates definidos aqui.

A
[`Coordenação de Conversas e Trabalho Paralelo Seguro`](Conversation-Coordination-Prompt.md)
é a autoridade temática especializada de roteamento entre conversas,
recomendação de raciocínio do Codex, paralelismo, ownership e integração. Ela
não altera a autoridade, os estados ou os gates definidos aqui.

## Estados canônicos

1. `STATE-00 DISCOVERY_MIGRATION`
2. `STATE-01 PROJECT_SETUP`
3. `STATE-02 ARCHITECTURE`
4. `STATE-03 DATABASE_MODELING`
5. `STATE-04 BACKEND_IMPLEMENTATION`
6. `STATE-05 FRONTEND_IMPLEMENTATION`
7. `STATE-06 INTEGRATION`
8. `STATE-07 TESTING_HOMOLOGATION`
9. `STATE-08 PRODUCTION_RELEASE`

O fluxo normal é sequencial. Auditoria aprovada não promove o estado automaticamente. Uma transição requer entregáveis, Quality Gate, Human Gate e entrada explícita no log.

## IDs canônicos de módulos

- MOD-01 IDENTITY_ACCESS
- MOD-02 INSTANCE_CATALOG
- MOD-03 AGENT_FLEET
- MOD-04 SERVICE_CONTROL
- MOD-05 PROVIDER_SDK
- MOD-06 HEALTH_MONITORING
- MOD-07 EVENT_HISTORY
- MOD-08 ALERTING
- MOD-09 DASHBOARD
- MOD-10 MONITORING_OPERATIONS
- MOD-11 RBAC_AUDIT
- MOD-12 AIOPS_AI

IDs não podem ser reutilizados com outro significado.

## Protocolo de execução

1. Ler visão, estado corrente e regras aplicáveis.
2. Antes de cada nova ação técnica autorizada, executar o shutdown preflight obrigatório e provar que nenhum componente ou runtime DB-Notifier permanece aberto, ativo ou escutando; turnos exclusivamente conversacionais não acionam este passo.
3. Congelar a baseline por branch/worktree e commit quando existente;
   inventariar ferramentas, mudanças preexistentes, untracked, artefatos
   protegidos e limites clean-room sem absorver trabalho alheio.
4. Confirmar autoridade, estado, objetivo, entregáveis, escopo positivo e
   escopo negativo. Fechar o envelope da tarefa com ownership, recursos
   mutáveis, revisores, checks, critérios de aceite e stop codes.
5. Para trabalho amplo, transversal, multi-incremento ou auditoria com
   remediação já autorizada, criar ou atualizar o [`../../PLANS.md`](../../PLANS.md)
   vivo. O plano registra execução; nunca concede autoridade nem substitui
   estado, histórico, ADR ou gate.
6. Selecionar o menor incremento coerente que ataque a causa raiz e definir a
   regressão capaz de provar o comportamento alterado.
7. Implementar somente o escopo autorizado, com um writer por boundary e
   validação focal contínua após cada incremento.
8. Revisar diff, claims, segurança, compatibilidade e evidência; usar revisão
   independente quando o risco ou a amplitude exigirem.
9. Integrar resultados de forma serial e determinística. Workers entregam
   candidatos; a coordenadora preserva a custódia da integração.
10. Executar os checks reais do repositório. `Quick` é somente feedback
    `NON_GATE`; `Full` chama uma vez o gate canônico. Preservar a disposição
    factual `PASS`, `FAIL`, `BLOCKED`, `PARTIAL` ou `NOT_RUN`.
11. Atualizar estado e histórico somente depois da mudança factual e sem
    reescrever evidência anterior.
12. Encerrar com diff/staged diff revisados, commit focal quando exigido,
    limitações, trabalho restante, próximo passo, próxima etapa e handoff
    governado.

O fluxo operacional padrão é:

```text
authority → baseline → scope → preflight → live plan → small increments →
focused regressions → independent review → serial integration → aggregate
gate → factual state/history → governed hand-off
```

Os scripts apenas materializam parte desse protocolo. Resultado mecânico não
autoriza escopo, ADR, Human Gate, ativação, homologação ou transição de estado.

## Ações permitidas por estado

| Estado | Permitido | Não permitido sem nova autoridade |
|---|---|---|
| STATE-00 | Inspeção, inventário, documentação e testes não mutáveis do legado | Implementar arquitetura-alvo |
| STATE-01 | Scaffold, restore, build, lint e testes de infraestrutura | Regras funcionais do domínio |
| STATE-02 | ADRs, diagramas, contratos e spikes descartáveis | Produto funcional |
| STATE-03 | Modelo e migrations do armazenamento interno | Aplicar migration em produção |
| STATE-04 | Domínio, Application, providers, API e testes | UI funcional completa |
| STATE-05 | Tray/Desktop, Dashboard e testes de interface | Integração externa não autorizada |
| STATE-06 | Dependências locais, contratos e E2E em sandbox | Produção |
| STATE-07 | Segurança, carga e homologação autorizada | Publicação |
| STATE-08 | Empacotar, assinar, migrar e publicar em alvo autorizado | Mudança de produto não registrada |

Comando destrutivo, deploy remoto, alteração de secret ou controle de banco real sempre exige autorização específica, independentemente do estado.

## Resolução de conflitos

- Aplicar a precedência do `Start-Here.md`.
- Preferir fonte atual e factual a registro histórico.
- Não resolver conflito reduzindo segurança ou inventando decisão.
- Mudança arquitetural exige ADR.
- Se a resolução ampliar materialmente escopo ou impacto externo, solicitar direção.

## Estado bloqueado

Registrar:

- Causa e evidência
- Impacto e escopo afetado
- Tentativas seguras realizadas
- Trabalho independente ainda possível
- Responsável ou dependência externa
- Condição objetiva de desbloqueio

Bloqueio não autoriza salto de estado.

## Rollback

- Definir gatilho, owner, versão-alvo, RTO e RPO.
- Separar rollback de binário, configuração, schema interno e protocolo.
- Preservar dados/auditoria e validar restore.
- Nunca executar rollback em banco monitorado como efeito colateral.
- Preferir forward-fix quando rollback aumentar o risco, com decisão registrada.

## Memória do projeto

- `../../PLANS.md`: plano vivo não autorizante do incremento amplo corrente.
- `../state/Current-State.md`: somente presente factual.
- `../state/State-Transition-Log.md`: histórico append-only.
- ADRs: decisões arquiteturais e substituições.
- Relatórios: evidência de uma execução específica.
- `../system/Prompt-System-Change-Log.md`: evolução deste corpus.

## Coordenação de conversas e trabalho multiagente

Aplicar integralmente
[`Conversation-Coordination-Prompt.md`](Conversation-Coordination-Prompt.md)
sempre que houver handoff entre conversas ou avaliação de trabalho paralelo.
Esta seção preserva somente os invariantes transversais:

- Uma única conversa coordenadora mantém escopo, baseline e integração.
- Cada handoff recomenda exatamente um nível de raciocínio do Codex para a
  próxima interação e cada lane recebe recomendação própria. A recomendação é
  consultiva, não comprova a configuração aplicada e não altera autoridade,
  roteamento, paralelismo, ownership, gates ou lifecycle.
- Nenhum arquivo, artefato lógico ou recurso mutável pode ter writers
  sobrepostos.
- Sem workflow Git de escrita paralela especificamente autorizado e worktrees
  isolados, conversas simultâneas permanecem read-only e toda escrita ocorre
  sequencialmente na coordenadora.
- Estado, histórico, changelog, ADRs, relatórios e decisões de gate e Human
  Gates permanecem sob custódia exclusiva da coordenadora.
- Custódia não concede autoridade para decidir ADR, Human Gate, lifecycle,
  ativação, ação externa ou operação Git não autorizada.
- O uso de workers não amplia escopo, runtime, acesso a secrets ou autoridade
  externa; cada resultado é somente candidato sujeito a integração e validação
  central.

## Guard rails absolutos

- Não inventar evidência, ambiente, credencial ou aprovação.
- Não expor secret, token ou connection string.
- Não enfraquecer controles para fazer teste passar.
- Não apresentar dado stale/unknown como saudável.
- Não confundir probe de banco com saúde do Agent/API.
- Não avançar fase implicitamente.
- Não publicar, instalar ou controlar infraestrutura real sem autorização.
