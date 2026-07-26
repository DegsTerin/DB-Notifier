# STATE-06 — Relatório de isolamento do diagnóstico temporal R-D4

## Decisão

O lote local `R-D4-TIMING` está automaticamente `APROVADO` somente para a
confiabilidade do teste temporal O5/R5 descrito neste relatório.

O deadline congelado de `100 ms`, o agendamento `CancelAfter(10 ms)`, a
observação por `WaitHandle`, a medição por `Stopwatch` e a prova separada de
cancelamento externo permanecem inalterados. Nenhum comportamento produtivo
mudou.

O resultado não altera `STATE-06 INTEGRATION`, não constitui Human Gate ou
decisão de lifecycle e não modifica `ActivationState=None`.

## Autoridade e escopo

- Autoridade: execução sequencial de todos os trabalhos técnicos locais ainda
  obrigatórios, com implementação, regressão, cobertura, documentação
  proprietária e commit local focal.
- Baseline: commit `2d0a658`.
- Data: 2026-07-26.
- Incluído: diagnóstico da flutuação conhecida, isolamento da classe de teste
  diretamente afetada, regressão da assembly e da solução, cobertura e
  documentação factual.
- Excluído: código de produto, dependências, configuração, migration, runtime
  externo ou operacional, lifecycle, Human Gates, `ActivationState`, push,
  pull request e deploy.

O shutdown preflight encerrou os servidores de build pertencentes ao SDK local
do workspace. A inspeção por PID, executável, linha de comando e listener
confirmou zero processo ou listener DB-Notifier; o build host da extensão C#
do VS Code foi preservado por não pertencer ao runtime do projeto.

## Diagnóstico

O histórico já registrava duas ocorrências em que
`WaitHandleCandidateIsTimelyAndCancellationAware` falhou sob carga e passou
isoladamente. A validação deste lote reproduziu a fronteira de forma
determinística:

- duas execuções da solução terminaram com `136/137` integrações;
- uma execução da assembly de integração terminou com `136/137`;
- em todas as três falhas, o `WaitHandle` expirou antes de o
  `CancelAfter(10 ms)` sinalizar o token;
- o mesmo caso filtrado passou `1/1` em `28 ms`.

O teste temporal concorria com as demais coleções da assembly. Essa carga
alheia contaminava a medição isolada do caminho de cancelamento sem demonstrar
violação do candidato. Repetir, ignorar ou aumentar o deadline ocultaria ou
enfraqueceria o contrato e foi rejeitado.

## Alteração

- Adicionada uma coleção xUnit própria com
  `DisableParallelization = true`.
- A coleção foi aplicada somente à classe
  `O5R5CancellationWorkingSetDiagnosticsTests`.
- As quatro verificações de diagnóstico processual da classe agora não
  concorrem com coleções alheias da mesma assembly.
- O algoritmo sob teste, os valores congelados e todas as asserções
  permaneceram intactos.

O arquivo diretamente afetado é
[`O5R5CancellationWorkingSetDiagnosticsTests.cs`](../tests/DBNotifier.IntegrationTests/O5R5CancellationWorkingSetDiagnosticsTests.cs).

## Evidência

Ambiente: Windows, SDK .NET local `10.0.301`, configuração `Release`, sem
restore, download ou runtime externo.

| Verificação | Resultado observado |
|---|---|
| Build Release do projeto de integração | aprovado com `0` warnings e `0` erros |
| Assembly de integração após o isolamento | `137/137` aprovada |
| Solução completa após o isolamento | `771/771` aprovada |
| Formatação .NET | aprovada sem mudança |
| Documentação de código | `420` fontes aprovadas |
| Cobertura .NET de linhas | `83,41%` |
| Cobertura .NET de branches | `56,62%` |
| Componentes obrigatórios presentes na cobertura | `10/10` |

Os pisos obrigatórios de 70% de linhas e 45% de branches permanecem
satisfeitos. A meta orientativa baseada em risco de 80% de linhas também foi
atingida, sem reduzir nenhum piso por componente.

## Limites

- O isolamento xUnit impede concorrência entre coleções dentro da assembly;
  não promete imunidade contra saturação arbitrária causada por outros
  processos ou pelo host.
- A solução completa passou com as quatro assemblies em execução normal, mas
  isso não constitui benchmark ou SLO do host.
- Se uma campanha futura provar interferência entre processos, a evolução
  correta será um processo ou projeto dedicado, sem ampliar o deadline.
- Este lote não reabre nem reclassifica R5, suas decisões de autoridade, a
  campanha física D4 ou qualquer Human Gate.
