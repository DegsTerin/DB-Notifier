# Templates do DB-Notifier

Templates não representam execução nem aprovação até serem preenchidos com evidências reais.

## Handoff de fase

- Estado encerrado:
- Estado recomendado:
- Objetivo e escopo entregues:
- Arquivos/artefatos alterados:
- ADRs e decisões:
- Checks e resultados:
- Interfaces/schemas/protocolos:
- Riscos e dívida:
- Rollback:
- Pré-condições da próxima fase:
- Auditoria automática:
- Human Gate:

## Relatório de execução

- Estado/fase:
- Versão e commit:
- Ambiente, data e executor:
- Escopo e providers cobertos:
- Pré-condições e configuração sanitizada:
- Comandos/testes e resultados:
- Falhas e correções:
- Itens não testados:
- Riscos residuais:
- Decisão do gate:
- Evidências:

Usar para integração, homologação ou release. A fase determina as verificações adicionais em `../governance/Quality-Gates.md`.

## Auditoria automática

- Estado e escopo:
- Entregáveis esperados:
- Checks executados:
- Resultado por gate: APROVADO/REPROVADO/BLOQUEADO/NÃO APLICÁVEL
- Achados por severidade:
- Evidências:
- Limitações do ambiente:
- Recomendação:

## Human Gate

- Fase:
- Validador e data:
- Relatório automático revisado (identificador/commit):
- Amostras críticas repetidas pelo validador:
- Amostras não repetidas e motivo:
- Experiência operacional:
- Segurança/autorização:
- Cobertura pendente:
- Ressalvas aceitas:
- Decisão: PENDENTE/APROVADO/APROVADO COM RESSALVAS/REPROVADO
- Justificativa e evidências:
- Confirmação inequívoca do validador: `Confirmo a decisão acima exclusivamente para <STATE-ID>`

Uma palavra isolada ou autorização para continuar não preenche este template. Cada fase exige decisão separada; gates agrupados permanecem pendentes.

## Ratificação retrospectiva de Human Gate

- Fase original:
- Registro histórico contestado:
- Motivo da contestação:
- Evidência automática histórica:
- Evidência automática repetida agora:
- Amostras humanas repetidas agora:
- Limitações ainda não exercitadas:
- Ressalvas e dívida aceitas:
- Decisão de ratificação: PENDENTE/APROVADO/APROVADO COM RESSALVAS/REPROVADO
- Validador e data:
- Confirmação inequívoca: `Ratifico a decisão acima exclusivamente para <STATE-ID>`

A ratificação é um adendo e não reescreve o relatório ou a decisão histórica contestada.

## ADR

- ID e título:
- Status: proposta/aceita/substituída
- Contexto:
- Decisão:
- Alternativas:
- Consequências:
- Segurança e operação:
- Compatibilidade/migração:
- Data e responsáveis:
