# STATE-06 — Relatório de regressão do lifecycle do gate de identidade R-FENCE

## Decisão

O lote local de regressão `R-FENCE` está automaticamente `APROVADO` somente
para a cobertura determinística de lifecycle descrita neste relatório.

Nenhum comportamento produtivo mudou. O lote expõe o fence transacional
interno por Agent somente ao assembly de testes unitários e prova que
cancelamento, unwind excepcional, descarte repetido e identidades de Agents
distintos não corrompem o seu modelo process-local de ownership.

O resultado não altera `STATE-06 INTEGRATION`, não constitui Human Gate ou
decisão de lifecycle e não modifica `ActivationState=None`.

## Autoridade e escopo

- Autoridade: execução sequencial de todos os lotes técnicos locais
  obrigatórios, incluindo implementação, testes, cobertura, documentação
  proprietária, estado factual, histórico append-only e um commit local focal
  por lote.
- Baseline: commit `7f29ebf`.
- Data: 2026-07-26.
- Incluído: implementação diretamente afetada do fence de identidade, acesso
  interno de teste, regressões determinísticas e evidência proprietária.
- Excluído: mudanças de dependência ou migration, runtime externo ou
  operacional, execução PostgreSQL, lifecycle, Human Gates,
  `ActivationState`, push, pull request e deploy.

O shutdown preflight obrigatório observou zero processo, listener ou janela
de produto DB-Notifier. Processos alheios de IDE, navegador, banco e serviço
foram preservados.

## Lacuna residual

A implementação de R-EGRESS retinha cada entrada do gate antes da espera e
liberava essa referência quando a aquisição era cancelada. Um lease liberava
seu semáforo e referência por um descarte interlocked executado no máximo uma
vez. Essas propriedades foram revistas em R-EGRESS, mas a sua suíte de
regressão provava as ordens funcionais de ingestão/revogação, não o lifecycle
do próprio primitivo de fence.

Restavam quatro afirmações de implementação sem regressão determinística
direta:

1. um waiter cancelado não pode remover uma entrada ainda possuída por outra
   transação e permitir que um terceiro waiter adquira um gate diferente;
2. unwind por exceção libera o gate adquirido;
3. descarte repetido do lease não acrescenta um segundo permit ao semáforo;
4. um gate ocupado para um Agent não serializa outro Agent.

A documentação XML também descrevia o owner atual como “bounded”, embora o
primitivo deliberadamente não possua timeout independente. O cancelamento e a
operação proprietária fornecem esse limite.

## Alterações

- Adicionado `InternalsVisibleTo("DBNotifier.UnitTests")` no assembly de
  persistência PostgreSQL, sem ampliar a API do produto.
- Adicionados quatro testes unitários diretos, com identificadores sintéticos
  únicos de Agent e guard de conclusão de cinco segundos restrito ao teste.
- Corrigida a documentação XML inexata de “bounded owner” para “current
  owner”.

Nenhum algoritmo, ordem transacional, contrato de persistência, schema,
migration, pacote ou composição de runtime mudou.

## Evidência

Ambiente: Windows, SDK .NET local `10.0.301`, configuração `Release`, sem
restore e sem runtime externo.

| Verificação | Resultado observado |
|---|---|
| Regressões diretas do lifecycle do fence | `4/4` aprovadas |
| Suíte unitária completa pelo gate de cobertura | `417/417` aprovada |
| Build Release da solução | aprovado com `0` warnings e `0` erros |
| Cobertura .NET de linhas | `82,12%` |
| Cobertura .NET de branches | `54,31%` |
| Componentes obrigatórios presentes na cobertura | `10/10` |

Os pisos obrigatórios de 70% de linhas e 45% de branches permanecem
satisfeitos. A meta orientativa baseada em risco de 80% de linhas também foi
atingida, sem reduzir nenhum piso por componente.

Tentativas focais iniciais foram afetadas somente pelo tempo de materialização
local do OneDrive. Processos pertencentes aos testes foram verificados e
encerrados, e a execução bem-sucedida `4/4` é o resultado registrado acima.

## Limites e riscos restantes

- Os testes provam somente o primitivo process-local. Comportamento do row lock
  PostgreSQL e contenção multiprocesso permanecem fora deste lote.
- O gate continua sem prometer prioridade, fairness ou timeout próprio.
- A revogação ainda entra no fence antes da decisão RBAC final. Efeitos de
  carga e latência permanecem não medidos.
- Os testes não ativam ingestão normal, provider, banco, sincronização do Agent
  ou qualquer caminho de rede externo.

A decisão funcional de R-EGRESS e sua aceitação explícita pelo proprietário
permanecem inalteradas.
