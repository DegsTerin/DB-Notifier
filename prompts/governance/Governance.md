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

A autoridade temática de
[`Melhoria Contínua`](Continuous-Improvement.md) especializa fingerprints,
ledger append-only, tentativa, quarentena, promoção, observação, rollback e
métricas. Ela não cria requisito de produto, autoridade externa, transição de
lifecycle ou permissão para ultrapassar escopo, WIP protegido ou clean-room.

## Autonomia prospectiva

Os objetivos e requisitos canônicos do produto são entradas estabelecidas. A
coordenadora decide autonomamente arquitetura, planejamento, implementação,
revisão, documentação, integração local e progressão de lifecycle, desde que o
escopo permaneça local, seguro e compatível com as autoridades temáticas.

Human Gates, aprovações rotineiras e navegação manual deixam de ser dependências
futuras de desenvolvimento. Resultados históricos permanecem fatos imutáveis;
novas decisões usam `Agent Gate` e os estados `AGENT_DECIDED`,
`AUTOMATED_GATE_PASS`, `AUTOMATED_GATE_FAIL`, `LOCAL_COMPLETE`,
`EXTERNAL_PREREQUISITE` ou `BLOCKED_BY_HIGHER_AUTHORITY`.

Essa delegação não substitui autenticação humana do produto, RBAC, confirmação
de ações administrativas, requisitos jurídicos e de licenciamento, clean-room,
proteção de segredos, safety gates destrutivos ou pré-requisitos externos.

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

O fluxo normal é sequencial. Auditoria isolada não promove o estado automaticamente. Uma transição requer entregáveis, Quality Gate, `AUTOMATED_GATE_PASS`, decisão `AGENT_DECIDED` e entrada explícita no log; satisfeitas essas condições, a coordenadora continua sem solicitar aprovação rotineira.

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
2. Antes de cada nova ação técnica, executar o shutdown preflight obrigatório e provar que nenhum componente ou runtime DB-Notifier permanece aberto, ativo ou escutando; turnos exclusivamente conversacionais não acionam este passo.
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
7. Em loop de melhoria contínua, consumir um evento imutável, deduplicar pelo
   fingerprint estável e executar no máximo uma próxima ação bounded conforme
   [`Continuous-Improvement.md`](Continuous-Improvement.md).
8. Implementar somente o escopo autorizado, com um writer por boundary e
   validação focal contínua após cada incremento.
9. Revisar diff, claims, segurança, compatibilidade e evidência; usar revisão
   independente quando o risco ou a amplitude exigirem.
10. Integrar resultados de forma serial e determinística. Workers entregam
   candidatos; a coordenadora preserva a custódia da integração.
11. Executar os checks reais do repositório. `Quick` é somente feedback
    `NON_GATE`; `Full` chama uma vez o gate canônico. Preservar a disposição
    factual `PASS`, `FAIL`, `BLOCKED`, `PARTIAL` ou `NOT_RUN`.
12. Atualizar estado e histórico somente depois da mudança factual e sem
    reescrever evidência anterior.
13. Encerrar o lote com diff/staged diff revisados, commit focal quando exigido,
    limitações, trabalho restante e despacho interno governado para o próximo
    lote seguro.

O fluxo operacional padrão é:

```text
canonical objectives → baseline → scope → preflight → live plan → small increments →
stable finding → causal delta → focused regressions → independent review → serial integration → aggregate
gate → Agent Gate → factual state/history → automatic dispatch
```

Os scripts apenas materializam parte desse protocolo. Resultado mecânico não
substitui decisão arquitetural, Agent Gate, ativação, homologação ou transição
de estado; a coordenadora decide somente depois de satisfazer as condições
objetivas aplicáveis.

## Ações permitidas por estado

| Estado | Permitido | Limite ou pré-requisito separado |
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
- Mudança arquitetural exige ADR decidido e registrado pela coordenadora após
  revisão independente proporcional ao risco.
- Se a resolução ampliar materialmente o escopo, dividir e decidir um novo lote
  local. Se produzir impacto externo ou encontrar autoridade superior, registrar
  `EXTERNAL_PREREQUISITE` ou `BLOCKED_BY_HIGHER_AUTHORITY`.

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

- Definir gatilho, responsável agente, versão-alvo, RTO e RPO.
- Separar rollback de binário, configuração, schema interno e protocolo.
- Preservar dados/auditoria e validar restore.
- Nunca executar rollback em banco monitorado como efeito colateral.
- Preferir forward-fix quando rollback aumentar o risco, com decisão registrada.

## Automated Safety Gate para ação destrutiva local

Uma ação local destrutiva ou de difícil reversão somente pode receber decisão
agente quando o gate comprovar cumulativamente:

- alvo exato, resolvido e estritamente dentro do escopo positivo;
- ausência de raiz de workspace, diretório home, caminho amplo, variável,
  substituição, glob ou identificador ainda não resolvido no alvo;
- preservação de WIP e artefatos protegidos;
- checkpoint recuperável e plano de rollback;
- validação de rollback quando aplicável;
- inexistência de alternativa materialmente mais segura;
- necessidade objetiva e revisão independente.

Qualquer incerteza registra `AUTOMATED_GATE_FAIL` antes da mutação. O gate não
concede acesso clean-room, credencial, autoridade sobre dado externo,
produção, publicação ou infraestrutura.

## Pré-requisitos de ação externa

A coordenadora somente executa ação externa quando todos os itens aplicáveis
estão presentes e atuais: autoridade explícita ou permanente; credencial
existente e válida usada por mecanismo seguro; conta, organização, ambiente e
alvo exatos; ferramenta disponível e apta; limite de custo definido; critério
objetivo de sucesso; e verificação segura ou reversão. A confirmação
administrativa exigida pelo produto continua separada.

Se qualquer condição faltar, concluir todo trabalho local independente e
registrar `EXTERNAL_PREREQUISITE` com uma única dependência factual. Nunca
reduzir gate, fabricar receipt, reutilizar credencial incerta ou transferir ao
proprietário um payload que uma ferramenta disponível possa executar.

## Memória do projeto

- `../../PLANS.md`: plano vivo não autorizante do incremento amplo corrente.
- `../state/Continuous-Improvement-Backlog.md`: fila factual de melhorias, não
  autoridade nem ledger operacional mutável.
- `../state/Current-State.md`: somente presente factual.
- `../state/State-Transition-Log.md`: histórico append-only.
- ADRs: decisões arquiteturais e substituições.
- Relatórios: evidência de uma execução específica.
- `../system/Prompt-System-Change-Log.md`: evolução deste corpus.

## Coordenação de tarefas e trabalho multiagente

Aplicar integralmente
[`Conversation-Coordination-Prompt.md`](Conversation-Coordination-Prompt.md)
sempre que houver despacho entre tarefas ou avaliação de trabalho paralelo.
Esta seção preserva somente os invariantes transversais:

- Uma única tarefa coordenadora mantém escopo, baseline e integração.
- O próximo trabalho é despachado diretamente por ferramenta com payload
  completo, receipt e chave de deduplicação; nunca depende do proprietário
  copiar, colar, encaminhar ou escolher a conversa.
- Nenhum arquivo, artefato lógico ou recurso mutável pode ter writers
  sobrepostos.
- Sem workflow Git de escrita paralela especificamente autorizado e worktrees
  isolados, conversas simultâneas permanecem read-only e toda escrita ocorre
  sequencialmente na coordenadora.
- Estado, histórico, changelog, ADRs, relatórios e decisões de Agent Gate
  permanecem sob custódia exclusiva da coordenadora.
- A coordenadora decide ADR e lifecycle locais depois dos gates objetivos.
  Custódia não concede autoridade sobre ação externa, operação Git insegura ou
  limite de autoridade superior.
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
