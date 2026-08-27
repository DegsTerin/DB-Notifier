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
  `NOT_RUN`, sem promover lifecycle ou Human Gate implicitamente.

## Política de cobertura

- Os gates automatizados correntes mantêm pisos obrigatórios de `70%` de
  linhas e `45%` de branches para a suíte .NET abrangida.
- `80%` de linhas é meta orientativa baseada em risco. Não substitui os pisos,
  não é um novo gate automático e não prova qualidade isoladamente.
- Componentes críticos podem exigir cobertura superior. Nenhum piso existente
  por componente pode ser reduzido sem decisão explícita, evidência e registro
  de governança.
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

### Resultados mecânicos, gate automático e decisão humana

As disposições do runner são evidência mecânica e permanecem literalmente
`PASS`, `FAIL`, `BLOCKED`, `PARTIAL` ou `NOT_RUN`:

- `PASS`: todos os checks obrigatórios daquele runner foram executados e
  aprovados no escopo declarado;
- `FAIL`: ao menos um check executado produziu falha factual;
- `BLOCKED`: uma pré-condição obrigatória impediu a execução;
- `PARTIAL`: somente parte do conjunto aplicável foi executada ou ficou atual;
- `NOT_RUN`: um check aplicável não foi executado, com motivo explícito.

O gate automático apresenta ao proprietário `APROVADO` apenas quando a
evidência agregada obrigatória é `PASS`; apresenta `REPROVADO` diante de
`FAIL`; e apresenta `BLOQUEADO` diante de `BLOCKED`, `PARTIAL` ou qualquer
`NOT_RUN` obrigatório. A disposição mecânica original continua visível e não
é traduzida nem apagada. `NÃO APLICÁVEL` significa que o check está fora do
escopo provado e nunca é sinónimo de `NOT_RUN`.

Uma decisão humana ou `Human Gate` é uma camada separada: não nasce de um
resultado mecânico e nunca pode convertê-lo, corrigi-lo ou substituí-lo.

## Fluxo local canônico e CI

O entry point local é `../../scripts/development.ps1`:

- `Doctor`: diagnóstico read-only do preflight, layout, toolchains, lockfiles
  e prontidão das dependências;
- `Setup`: restore bloqueado, sem instalar toolchain e sem alterar lockfile;
- `Quick`: feedback focal explicitamente `NON_GATE`, sem integração, cobertura,
  runtime, auditoria online ou Human Gate;
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
produto, revisão semântica ou gate humano.

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

1. Confirmar que perguntas, explicações, atualizações, aprovações, alertas,
   handoffs e mensagens prontas para copiar destinadas ao proprietário usam
   `pt-BR`.
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
10. Registrar escopo revisado, exceções técnicas, revisão humana de vocabulário
    e resultado como `APROVADO`, `REPROVADO`, `BLOQUEADO` ou
    `NÃO APLICÁVEL`.

Automação lexical auxilia a revisão, mas não substitui a avaliação humana de
idioma dominante, ortografia britânica, clareza, nomes externos e contexto de
interface.

## Gate de coordenação de conversas e paralelismo

Aplicar este gate a todo handoff governado e a qualquer plano com múltiplas
conversas, conforme
[`Conversation-Coordination-Prompt.md`](Conversation-Coordination-Prompt.md).

1. Confirmar que `Conversation action` contém somente
   `CONTINUE_CURRENT`, `START_NEW` ou `RETURN_TO_EXISTING`.
2. Confirmar que `Parallel work` contém somente `SEQUENTIAL_ONLY`,
   `PARALLEL_OPTIONAL` ou `PARALLEL_RECOMMENDED`.
3. Validar os 14 campos obrigatórios e sua ordem, rótulos em `pt-BR`, chaves
   canônicas preservadas e ausência de placeholders em instâncias reais.
4. Confirmar que `Your action now` começa com exatamente uma recomendação para
   a próxima interação: `Leve` (`low`), `Médio` (`medium`), `Alto` (`high`),
   `Extra alto` (`xhigh`), `Máximo` (`max`) ou `Ultra` (`ultra`), seguida de
   uma razão concreta. Reprovar valor vazio, `None`, faixa de níveis,
   identificador divergente ou recomendação transportada sem reavaliação.
5. Confirmar escolha do menor esforço suficiente, fallback proporcional quando
   a disponibilidade for incerta e ausência de alegação não comprovada de que
   o nível está disponível, selecionado ou aplicado. A recomendação não pode
   ampliar roteamento, paralelismo, escopo, autoridade, ownership, preflight,
   gate, ADR, Human Gate, `ActivationState` ou lifecycle.
6. Confirmar que `Exact next message` contém sempre uma única mensagem
   completa, específica, preenchida, em `pt-BR` e pronta para copiar e enviar
   literalmente na conversa indicada, inclusive em resultado concluído,
   parcial ou bloqueado e quando nenhuma ação adicional de projeto for
   conhecida.
7. Reprovar valor vazio, placeholder, lista de alternativas, sugestão abstrata
   ou ``Não se aplica (`None`) — nenhuma mensagem é necessária`` em
   `Exact next message`. Confirmar coerência com `Next step`,
   `Your action now`, `Conversation action` e `Conversation target`.
8. Confirmar que a mensagem pronta não presume, fabrica ou amplia aprovação,
   Human Gate, ADR, `ActivationState`, lifecycle, operação Git ou ação externa.
   Decisão formal pendente deve receber pedido de apresentação ou revisão do
   pacote decisório, salvo resultado já escolhido inequivocamente pelo
   proprietário no contexto vigente.
9. Confirmar label da coordenadora fornecido pelo proprietário ou identificador
   canônico fornecido pela plataforma, versão do corpus e commit/hash da
   baseline quando existente; somente label do proprietário habilita
   `RETURN_TO_EXISTING`, e nunca se aceita conversa inventada ou alegadamente
   aberta pelo agente.
10. Comprovar dependências congeladas e acíclicas, inputs compartilhados
    somente leitura e ownership exclusivo de paths, artefatos lógicos e recursos
    mutáveis.
11. Confirmar zero writers sobrepostos. Ownership de diretório exclui writers
    concorrentes em qualquer descendente.
12. Sem workflow Git paralelo especificamente autorizado e worktrees isolados
    próprios, comprovar que todas as conversas simultâneas permaneceram
    read-only e que a escrita foi sequencial na coordenadora.
13. Quando houver escrita paralela autorizada, verificar branch própria e
    worktree isolado próprio por writer, write sets disjuntos e isolamento
    aplicável de portas, processos, bancos, índices, temporários, caches e
    outputs.
14. Confirmar arquivos e ações proibidos, condições objetivas de parada,
    fallback sequencial e preservação da última baseline validada sem descarte
    de trabalho.
15. Confirmar que o plano paralelo declara um nível e uma razão para a
    coordenadora e para cada lane, e que cada mensagem auxiliar repete sua
    recomendação numa frase de preâmbulo não canônica antes dos 19 campos
    existentes, sem criar um 20º campo. `Ultra` não cria
    `PARALLEL_RECOMMENDED`; `PARALLEL_RECOMMENDED` não exige `Ultra`; e
    `SEQUENTIAL_ONLY` continua soberano sobre writers e recursos mutáveis.
16. Confirmar que workers não integraram outras lanes, não atualizaram estado,
    histórico ou changelog, não alteraram ou aceitaram ADR, não promoveram
    lifecycle ou ativação e não solicitaram ou confirmaram Human Gate.
17. Confirmar que cada worker entregou somente um candidato com arquivos,
    artefatos, recursos, checks, evidências, limitações e riscos.
18. Verificar integração determinística de uma entrega por vez, checks locais
    após cada integração e checks transversais sobre o resultado combinado.
19. Confirmar que estado, histórico, changelog, ADRs, relatórios e decisões de
    gate e Human Gates permaneceram sob custódia exclusiva da coordenadora.
20. Confirmar que custódia não foi tratada como autoridade decisória e que
    nenhuma ação externa, operação Git, lifecycle ou ativação foi inferida.
21. Confirmar que eventual Human Gate foi apresentado somente depois da
    integração, auditoria consolidada e amostras humanas aplicáveis.

Qualquer overlap, baseline incerta, isolamento insuficiente, decisão pendente
ou ampliação de autoridade reprova ou bloqueia o plano paralelo e força
`SEQUENTIAL_ONLY`. O gate avalia a coordenação; não aprova ADR, Human Gate,
lifecycle, ativação, produto ou ação externa.

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

## Human Gate

O validador humano deve:

- Revisar relatório automático e repetir amostras críticas.
- Confirmar experiência operacional e mensagens de erro.
- Verificar distinção entre local/remoto e provider suportado/planejado.
- Confirmar autorização e auditoria de ações administrativas.
- Registrar decisão, nome, data, ressalvas e evidência sanitizada.

Decisões possíveis: `PENDENTE`, `APROVADO`, `APROVADO COM RESSALVAS` ou `REPROVADO`.

O gate não pode ser pré-aprovado nem substituir falha técnica sem justificativa formal.

Uma resposta curta como `sim`, `aprovado`, `yes`, `approved` ou `seguir` somente constitui decisão quando responde imediatamente a um resumo explícito de um único estado que identifica o relatório automático revisado, as amostras humanas repetidas, a cobertura pendente, as ressalvas e a decisão solicitada. Resposta ambígua, aprovação agrupada de vários estados ou simples autorização para continuar mantém o gate `PENDENTE`.

Se o validador contestar posteriormente que uma aprovação registrada foi informada, o histórico não é reescrito. A progressão entra em espera e cada estado afetado recebe ratificação retrospectiva independente, com evidência atualizada e nova decisão humana. A ratificação não transforma automaticamente evidência técnica antiga em amostra humana executada.

## Amostras humanas por fase

- STATE-01: onboarding de desenvolvedor e execução limpa.
- STATE-02: walkthrough de ameaças e cenários híbridos.
- STATE-03: leitura de modelo/migration e recuperação.
- STATE-04: falhas reais de provider e autorização negativa.
- STATE-05: operação por teclado, leitor de tela e diferentes viewports.
- STATE-06: desconectar/reconectar Agent e duplicar mensagens.
- STATE-07: operar matriz representativa e revisar resultados de carga.
- STATE-08: ensaiar rollout, health check e rollback.

## Retrospectiva arquitetural

Executar quando implementação ou integração revelar pressuposto incorreto:

- Comparar ADRs e solução real.
- Reavaliar limites Agent/API/provider/UI e modelo de ameaças.
- Registrar dívida aceita e ADR substituto.
- Não reescrever o histórico.

## Auditoria dos Human Gates

Comparar checklists, relatórios automáticos e workspace; detectar aprovação pré-preenchida, item não aplicável sem justificativa e cobertura omitida. A auditoria aponta inconsistências, mas não substitui a decisão humana.

Para ratificação retrospectiva:

1. Identificar exatamente o registro contestado e preservar o relatório histórico.
2. Revalidar automaticamente o que ainda é reproduzível e distinguir evidência histórica da atual.
3. Repetir ou declarar pendente cada amostra humana exigida na fase.
4. Apresentar um estado por vez, sem agrupar decisões.
5. Registrar nova decisão, data, ressalvas e evidência como adendo; somente depois restaurar a cadeia de progressão.
