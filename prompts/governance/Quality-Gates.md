# Qualidade, Evidências e Gates

## Padrão de evidências

Toda alegação técnica deve registrar, conforme aplicável:

- Comando, diretório, versão, data e exit code
- Escopo e ambiente
- Resultado resumido e artefato correspondente
- Logs e screenshots sanitizados
- Distinção entre observado, inferido, não testado e bloqueado

Banner de sucesso, compilação isolada ou ausência de erro aparente não provam saúde funcional.

## Definition of Done

- Requisitos e critérios atendidos.
- Build, testes e análise estática aplicáveis aprovados.
- Segurança, falhas parciais e compatibilidade avaliadas.
- Logs, observabilidade e mensagens de erro adequados.
- Documentação, migration e rollback atualizados.
- Nenhum secret ou evidência falsa.
- Dívida e cobertura pendente explicitadas.
- Mudanças preexistentes não relacionadas preservadas.

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
   canônicas preservadas, mensagens completas em `pt-BR` e ausência de
   placeholders em instâncias reais.
4. Confirmar label da coordenadora fornecido pelo proprietário ou identificador
   canônico fornecido pela plataforma, versão do corpus e commit/hash da
   baseline quando existente; somente label do proprietário habilita
   `RETURN_TO_EXISTING`, e nunca se aceita conversa inventada ou alegadamente
   aberta pelo agente.
5. Comprovar dependências congeladas e acíclicas, inputs compartilhados
   somente leitura e ownership exclusivo de paths, artefatos lógicos e recursos
   mutáveis.
6. Confirmar zero writers sobrepostos. Ownership de diretório exclui writers
   concorrentes em qualquer descendente.
7. Sem workflow Git paralelo especificamente autorizado e worktrees isolados
   próprios, comprovar que todas as conversas simultâneas permaneceram
   read-only e que a escrita foi sequencial na coordenadora.
8. Quando houver escrita paralela autorizada, verificar branch própria e
   worktree isolado próprio por writer, write sets disjuntos e isolamento
   aplicável de portas, processos, bancos, índices, temporários, caches e
   outputs.
9. Confirmar arquivos e ações proibidos, condições objetivas de parada,
   fallback sequencial e preservação da última baseline validada sem descarte
   de trabalho.
10. Confirmar que workers não integraram outras lanes, não atualizaram estado,
    histórico ou changelog, não alteraram ou aceitaram ADR, não promoveram
    lifecycle ou ativação e não solicitaram ou confirmaram Human Gate.
11. Confirmar que cada worker entregou somente um candidato com arquivos,
    artefatos, recursos, checks, evidências, limitações e riscos.
12. Verificar integração determinística de uma entrega por vez, checks locais
    após cada integração e checks transversais sobre o resultado combinado.
13. Confirmar que estado, histórico, changelog, ADRs, relatórios e decisões de
    gate e Human Gates permaneceram sob custódia exclusiva da coordenadora.
14. Confirmar que custódia não foi tratada como autoridade decisória e que
    nenhuma ação externa, operação Git, lifecycle ou ativação foi inferida.
15. Confirmar que eventual Human Gate foi apresentado somente depois da
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
