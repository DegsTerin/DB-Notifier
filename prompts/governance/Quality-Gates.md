# Qualidade, Evidências e Gates

## Padrão de evidências

Toda alegação técnica deve registrar, conforme aplicável:

- Comando, diretório, versão, data e exit code
- Escopo e ambiente
- Resultado resumido e artefato correspondente
- Logs e screenshots sanitizados
- Distinção entre observado, inferido, não testado e bloqueado

Banner de sucesso, compilação isolada ou ausência de erro aparente não provam saúde funcional.

## Definition of Ready

- Baseline, worktree e estado corrente identificados.
- Shutdown preflight aplicável comprovado.
- Autoridade, objetivo, critérios de aceite, escopo positivo, escopo negativo
  e trabalho protegido explícitos.
- Contratos e dependências necessários congelados ou bloqueados factualmente.
- Ownership exclusivo de paths, artefatos lógicos e recursos mutáveis.
- Estratégia de regressão, checks, revisores, evidência e rollback definida.
- `PLANS.md` vivo e sincronizado quando o trabalho for amplo, transversal,
  multi-incremento ou combinar auditoria e remediação.

Falha de prontidão é `BLOCKED`; não deve ser convertida em sucesso por redução
de escopo silenciosa, retry ou substituição de evidência.

## Definition of Done

- Requisitos e critérios atendidos.
- Build, testes e análise estática aplicáveis aprovados.
- Segurança, falhas parciais e compatibilidade avaliadas.
- Logs, observabilidade e mensagens de erro adequados.
- Documentação, migration e rollback atualizados.
- Nenhum secret ou evidência falsa.
- Dívida e cobertura pendente explicitadas.
- Mudanças preexistentes não relacionadas preservadas.
- Causa raiz tratada pelo menor incremento coerente e regressão correspondente.
- Diff, claims, segurança e compatibilidade revisados; revisão independente
  concluída quando exigida pelo risco.
- Plano vivo, estado, histórico, documentação e evidência reconciliados depois
  do fato, sem reescrever registros anteriores.
- Disposição final preservada como `PASS`, `FAIL`, `BLOCKED`, `PARTIAL` ou
  `NOT_RUN`, sem promover lifecycle ou Agent Gate implicitamente.

## Política de cobertura

- Os gates automatizados correntes mantêm pisos obrigatórios de `70%` de
  linhas e `45%` de branches para a suíte .NET abrangida.
- `80%` de linhas é meta orientativa baseada em risco. Não substitui os pisos,
  não é um novo gate automático e não prova qualidade isoladamente.
- Componentes críticos podem exigir cobertura superior. Nenhum piso existente
  por componente pode ser reduzido sem decisão agente explícita, evidência,
  revisão independente e registro de governança.
- Cobertura não substitui testes funcionais, negativos, de integração,
  segurança, acessibilidade, compatibilidade, resiliência ou desempenho
  aplicáveis.
- Exclusões de código gerado ou trivial devem ser estreitas, justificadas e
  verificáveis; não podem ocultar comportamento de produto.

## Auditoria automática comum

1. Confirmar estado e escopo.
2. Conferir diff e entregáveis esperados.
3. Descobrir e executar comandos reais do repositório.
4. Validar build, testes, análise estática, secrets e dependências aplicáveis.
5. Aplicar a
   [`Política de Idioma`](Language-Policy.md), executar
   `npm run comments:verify` quando autorizado e aplicável e revisar
   documentação de código alterado em inglês britânico.
6. Verificar providers, separação monitor/admin e comportamento sem conectividade.
7. Classificar cada gate como APROVADO, REPROVADO, BLOQUEADO ou NÃO APLICÁVEL.
8. Registrar achados com severidade, impacto, reprodução e correção recomendada.

Auditoria não corrige silenciosamente falhas, não inventa evidência e não promove estado.

### Resultados mecânicos e Agent Gate

As disposições do runner são evidência mecânica e permanecem literalmente
`PASS`, `FAIL`, `BLOCKED`, `PARTIAL` ou `NOT_RUN`:

- `PASS`: todos os checks obrigatórios daquele runner foram executados e
  aprovados no escopo declarado;
- `FAIL`: ao menos um check executado produziu falha factual;
- `BLOCKED`: uma pré-condição obrigatória impediu a execução;
- `PARTIAL`: somente parte do conjunto aplicável foi executada ou ficou atual;
- `NOT_RUN`: um check aplicável não foi executado, com motivo explícito.

O gate automático registra `AUTOMATED_GATE_PASS` apenas quando toda evidência
agregada obrigatória é `PASS`; registra `AUTOMATED_GATE_FAIL` diante de `FAIL`,
`BLOCKED`, `PARTIAL` ou qualquer `NOT_RUN` obrigatório. A disposição mecânica
original continua visível e não é traduzida nem apagada. `NÃO APLICÁVEL`
significa que o check está fora do escopo provado e nunca é sinónimo de
`NOT_RUN`.

O `Agent Gate` é uma camada separada: exige baseline exata, todos os checks
obrigatórios em `PASS`, evidência reconciliada, rollback proporcional e revisão
independente com zero `P0` e zero `P1`. Ele nunca converte, corrige ou substitui uma
disposição mecânica. Quando passa, a coordenadora registra `AGENT_DECIDED` e
continua; quando falha, registra `AUTOMATED_GATE_FAIL` e remedia dentro do
escopo seguro.

## Fluxo local canônico e CI

O entry point local é `../../scripts/development.ps1`:

- `Doctor`: diagnóstico read-only do preflight, layout, toolchains, lockfiles
  e prontidão das dependências;
- `Setup`: restore bloqueado, sem instalar toolchain e sem alterar lockfile;
- `Quick`: feedback focal explicitamente `NON_GATE`, sem integração, cobertura,
  runtime, auditoria online ou Agent Gate;
- `Full`: única tarefa que delega exatamente uma vez para
  `../../scripts/ci.ps1`, o agregador canônico;
- `PlanOnly`: plano versionado, ordenado, determinístico e sem preflight,
  processo-filho, restore, build, teste, runtime ou rede.

A política de toolchain aceita somente versões estáveis nas faixas
`.NET SDK >=10.0.302 <10.1.0`, `Node.js >=24.18.0 <25.0.0` e
`npm >=11.16.0 <12.0.0`. O limite inferior preserva a baseline validada; o
limite superior impede que uma atualização automática cruze uma linha ainda
não governada. `global.json`, `.nvmrc`, os manifests Dashboard, a metadata raiz
do lockfile e a CI devem representar esse mesmo contrato sem pin exato de
toolchain. Dependências e GitHub Actions continuam determinísticas em suas
autoridades próprias e não são relaxadas por esta regra.

O gate canônico conserva, conforme plataforma e escopo, as responsabilidades
.NET/WPF, arquitetura, legado/Pester, Dashboard, assets gerados, documentação,
Markdown, cobertura, dependências, segurança, browser sandbox, E2E `STATE-06`,
segredos e integridade Git. A workflow remota delega à mesma implementação;
checks diretos de componente são diagnósticos ou evidência suplementar, não um
segundo gate agregado. Após o shutdown preflight obrigatório, a varredura de
segredos do worktree não ignorado e do histórico Git disponível precede a
execução das políticas e dos gates de produto; higiene do diff e integridade
de objetos Git encerram a sequência.

Somente `Setup` e `Full` aceitam `-Offline`. O modo offline usa caches locais,
desativa notificações de atualização de workloads .NET, marca freshness de
advisories NuGet/npm como `NOT_RUN` e encerra o gate completo como `PARTIAL`,
mesmo quando todos os checks executáveis passam. Não há retry automático nem
correção em linha de uma falha.

`scripts/verify-development-flow.ps1` e
`tests/DBNotifier.DevelopmentFlow.Tests.ps1` verificam o plano vivo, a
delegação de CI, as invariantes `Quick`/`Full`, o isolamento de credenciais e o
plano determinístico. Esses testes de política não substituem build, teste de
produto, revisão semântica independente ou Agent Gate.

## Severidade de achados

- `P0`: risco crítico imediato, perda de dados, segredo exposto, ação externa
  insegura ou violação incontornável de autoridade; interrompe todo trabalho
  afetado.
- `P1`: defeito material de correção, segurança, reprodutibilidade, gate ou
  escopo; impede disposição `PASS` até correção autorizada ou bloqueio factual.
- `P2`: risco relevante não bloqueante, dívida ou lacuna de cobertura que deve
  ser registrada e priorizada.
- `P3`: melhoria de baixo risco, clareza ou manutenção sem impacto material
  demonstrado no objetivo corrente.

Todo achado informa localização, cenário, impacto, evidência, recomendação e
confiança. Ausência de achado não prova ausência absoluta de defeitos.

## Gate de conformidade linguística

Aplicar a
[`Política de Idioma`](Language-Policy.md) em toda mudança documental, de
código, configuração ou comunicação governada:

1. Confirmar que perguntas inevitáveis, explicações, atualizações, alertas,
   relatórios consolidados e dependências externas destinadas ao proprietário
   usam `pt-BR`; payloads internos nunca são apresentados para copiar.
2. Confirmar rótulos visíveis ao proprietário em `pt-BR`. Chaves canônicas,
   comandos, paths e enums podem permanecer em inglês entre crases ou
   parênteses somente quando tecnicamente necessários.
3. Confirmar que cada novo artefato independente pertencente ao projeto usa
   `en-GB`, com ortografia britânica, salvo convenção externa obrigatória.
4. Em alteração limitada de arquivo existente, confirmar que o idioma
   estabelecido foi preservado e que não surgiu mistura linguística
   injustificada.
5. Confirmar que comentários e documentação de código novos ou modificados
   usam `en-GB` e cumprem o
   [`Code-Documentation-Standards.md`](../../docs/Code-Documentation-Standards.md).
6. Verificar nomes impostos por linguagens, frameworks, bibliotecas,
   protocolos, padrões, APIs, terceiros e contratos externos sem tradução ou
   renomeação indevida.
7. Confirmar ausência de tradução em massa, reescrita de evidência histórica
   ou migração linguística sem autorização própria.
8. Quando uma operação Git estiver separadamente autorizada, confirmar
   mensagem de commit em `en-GB` e preservação integral das mensagens
   históricas.
9. Confirmar que idioma de conversa, engenharia ou documentação não alterou
   locales, catálogos, preferências ou comportamento da interface sem decisão
   de produto própria.
10. Registrar escopo revisado, exceções técnicas, revisão semântica agente de
    vocabulário e resultado como `APROVADO`, `REPROVADO`, `BLOQUEADO` ou
    `NÃO APLICÁVEL`.

Automação lexical auxilia a revisão, mas não substitui a avaliação semântica
independente de idioma dominante, ortografia britânica, clareza, nomes externos
e contexto de interface.

## Gate de despacho automático e paralelismo

Aplicar este gate a todo despacho governado e a qualquer plano multiagente,
conforme
[`Conversation-Coordination-Prompt.md`](Conversation-Coordination-Prompt.md).

1. Confirmar que a rota contém somente `CONTINUE_CURRENT`,
   `DELEGATE_SUBAGENT`, `RETURN_TO_EXISTING` ou `START_NEW_AUTO_DISPATCH`.
2. Confirmar que `Parallel work` contém somente `SEQUENTIAL_ONLY`,
   `PARALLEL_OPTIONAL` ou `PARALLEL_RECOMMENDED`.
3. Validar o payload interno completo: projeto, baseline, objetivo, escopos,
   trabalho protegido, ownership, checks, evidência, stop conditions e formato
   de retorno.
4. Para `DELEGATE_SUBAGENT`, `RETURN_TO_EXISTING` e
   `START_NEW_AUTO_DISPATCH`, confirmar execução pelo mecanismo da plataforma e
   receipt factual com origem, destino, rota, chave de deduplicação, resultado
   e estado. Para `CONTINUE_CURRENT`, exigir registro local factual de
   continuação no plano, sem inventar ferramenta ou receipt. Resultado incerto
   de uma rota baseada em ferramenta exige reconciliação antes de retry.
5. Reprovar qualquer título, mensagem exata, bloco pronto ou instrução para o
   proprietário copiar, colar, encaminhar, escolher ou navegar manualmente.
6. Comprovar dependências congeladas e acíclicas, inputs compartilhados somente
   leitura e ownership exclusivo de paths, artefatos lógicos e recursos
   mutáveis.
7. Confirmar zero writers sobrepostos. Ownership de diretório exclui writers
   concorrentes em qualquer descendente.
8. Sem workflow Git paralelo autorizado e worktrees isolados próprios,
   comprovar que todos os agentes simultâneos permaneceram read-only e que a
   escrita foi sequencial na coordenadora.
9. Quando houver escrita paralela autorizada, verificar branch e worktree
   isolados por writer, write sets disjuntos e isolamento de portas, processos,
   bancos, índices, temporários, caches e outputs.
10. Confirmar arquivos e ações proibidos, condições objetivas de parada,
    fallback sequencial e preservação da última baseline validada sem descarte
    de trabalho.
11. Confirmar que workers entregaram somente candidatos e não integraram outras
    lanes, estado, histórico, changelog ou decisões permanentes.
12. Verificar integração determinística, checks locais depois de cada candidato
    e checks transversais sobre o resultado combinado.
13. Confirmar que estado, histórico, changelog, ADRs, relatórios e Agent Gates
    permaneceram sob custódia exclusiva da coordenadora.
14. Confirmar que ação externa, operação Git destrutiva ou autoridade superior
    não foi inferida da autonomia local.
15. Diante de rota indisponível, confirmar fallback nesta ordem: continuar
    localmente, delegar subagente, retornar a tarefa confirmada, manter em fila;
    `EXTERNAL_PREREQUISITE` somente quando nenhuma continuação segura existir.

Overlap, baseline incerta ou isolamento insuficiente registra
`AUTOMATED_GATE_FAIL` e força `SEQUENTIAL_ONLY`. O gate avalia coordenação e
despacho; não concede autoridade externa nem enfraquece segurança.

## Gates de segurança destrutiva e ação externa

Antes de mutação local destrutiva, confirmar alvo exato e resolvido, escopo
positivo, ausência de raiz de workspace/home/caminho amplo/variável/glob ou
identificador não resolvido, preservação de WIP, checkpoint recuperável,
rollback, validação aplicável, inexistência de alternativa mais segura,
necessidade objetiva e revisão independente. Qualquer item ausente produz
`AUTOMATED_GATE_FAIL` antes da ação.

Antes de ação externa, confirmar cumulativamente autoridade aplicável,
credencial existente e atualmente válida por mecanismo seguro, conta e
ambiente exatos, ferramenta disponível e apta, limite de custo quando
aplicável, critério objetivo de sucesso e verificação segura ou reversão. Item
ausente produz `EXTERNAL_PREREQUISITE` depois de concluir o trabalho local
independente; nunca produz receipt presumido ou copy/paste para o proprietário.

## Verificações específicas por fase

| Estado | Verificações adicionais |
|---|---|
| STATE-01 | Bootstrap limpo, configuração, dependências e ausência de domínio prematuro |
| STATE-02 | ADRs, boundaries, threat model, offline, atualização e rollback |
| STATE-03 | Constraints, índices, retenção, segredo por referência e migrations |
| STATE-04 | Arquitetura de dependências, autorização, idempotência e testes de providers |
| STATE-05 | Estados de UI, Design System, Light/Dark, persistência, paridade React/WPF, High Contrast, acessibilidade, responsividade e dado stale |
| STATE-06 | Contratos, compatibilidade, reconexão, dedup, atualização autoritativa TV a cada 30 segundos sem sobreposição e E2E em sandbox |
| STATE-07 | Matriz real, segurança, carga, falha, recuperação e cobertura |
| STATE-08 | Artefato, assinatura, SBOM, deploy autorizado, observabilidade e rollback |

## Agent Gate

O revisor agente independente deve:

- revisar o relatório automático e repetir amostras críticas reproduzíveis;
- confirmar experiência operacional e mensagens de erro por evidência adequada;
- verificar distinção entre local/remoto e provider suportado/planejado;
- confirmar autorização e auditoria das ações administrativas do produto;
- classificar achados `P0`–`P3`, registrar baseline, data, limitações e evidência
  sanitizada;
- devolver `PASS` somente com `P0=0` e `P1=0` e sem check obrigatório ausente.

Decisões possíveis: `AUTOMATED_GATE_PASS` ou `AUTOMATED_GATE_FAIL`. Depois de
`AUTOMATED_GATE_PASS`, a coordenadora registra `AGENT_DECIDED`; ao concluir o
escopo local, registra `LOCAL_COMPLETE` e despacha o próximo lote.

O gate não pode ser pré-aprovado, substituir falha técnica ou transformar
`PARTIAL`/`NOT_RUN` em sucesso. Risco material de arquitetura, segurança,
persistência ou compatibilidade exige pelo menos uma revisão independente; risco
elevado exige uma segunda revisão independente.

## Amostras agentes por fase

- STATE-01: onboarding automatizado e execução limpa.
- STATE-02: threat-model walkthrough e cenários híbridos por revisores agentes.
- STATE-03: modelo/migration, recuperação e rollback reproduzíveis.
- STATE-04: falhas reais ou sandbox de provider e autorização negativa.
- STATE-05: teclado, acessibilidade automatizada, leitor de tela quando
  tecnicamente exercitável e diferentes viewports.
- STATE-06: desconectar/reconectar Agent e duplicar mensagens em sandbox.
- STATE-07: matriz representativa e resultados de carga reproduzíveis.
- STATE-08: rollout, health check e rollback no alvo autorizado.

## Retrospectiva arquitetural

Executar quando implementação ou integração revelar pressuposto incorreto:

- Comparar ADRs e solução real.
- Reavaliar limites Agent/API/provider/UI e modelo de ameaças.
- Registrar dívida aceita e ADR substituto.
- Não reescrever o histórico.

## Preservação dos Human Gates históricos

Comparar checklists, relatórios e workspace para detectar inconsistências, sem
reescrever a decisão ou a evidência histórica. Human Gates anteriores permanecem
fatos do contexto original e não são autoridade prospectiva.

Para revalidação atual:

1. identificar exatamente o registro e preservar o relatório histórico;
2. revalidar o que ainda é reproduzível e distinguir evidência histórica da
   atual;
3. executar as amostras agentes aplicáveis;
4. registrar um novo Agent Gate com baseline, data, achados e limitações;
5. acrescentar o resultado como adendo, nunca como alteração retroativa.
