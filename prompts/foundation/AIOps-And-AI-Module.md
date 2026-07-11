# MOD-12 AIOPS_AI — AIOps e Inteligência Artificial

## Status e objetivo

Capacidade de longo prazo, ainda não implementada. Seu objetivo é permitir que o DB-Notifier monitore, analise, preveja, recomende e, quando autorizado, automatize operações de forma segura, explicável, auditável e escalável.

A IA atua como apoio a DBAs e equipes de infraestrutura. Ela não substitui autorização, responsabilidade humana, providers determinísticos nem controles operacionais.

## Princípios

- A solução não é apenas um LLM.
- Regras determinísticas continuam sendo a fonte para condições objetivas.
- Modelos estatísticos e de ML produzem sinais com incerteza mensurável.
- O LLM recebe contexto estruturado e não acessa bancos diretamente.
- Recomendar, planejar, aprovar e executar são etapas separadas.
- Nenhuma saída probabilística recebe privilégio implícito.
- Baixa confiança, falta de dados e conflito de evidências devem ser apresentados.
- Toda fonte, decisão, aprovação e ação precisa ser rastreável.

## Arquitetura em camadas

```text
Coleta e normalização
        |
Regras determinísticas + séries temporais
        |
Correlação e detecção de anomalias
        |
Base de conhecimento / recuperação
        |
LLM para síntese e recomendação
        |
Planejador estruturado
        |
Motor de risco, política e aprovação
        |
Executor controlado por providers
        |
Auditoria, resultado e feedback
```

Cada camada possui contrato, versão, owner, telemetria e testes independentes.

## 1. Coleta e normalização

Fontes previstas:

- PostgreSQL, MySQL/MariaDB, SQL Server, Oracle, MongoDB e novos providers.
- Windows e Linux.
- CPU, memória, disco, rede, I/O, serviços e processos.
- Futuramente Docker, Kubernetes, AWS, Azure e Google Cloud.

Os dados devem ser normalizados em esquema interno versionado, preservando origem, unidade, timestamp, qualidade, cardinalidade e detalhes nativos necessários ao diagnóstico.

Segredos, conteúdo sensível de consultas e dados pessoais devem ser removidos ou classificados antes de qualquer processamento de IA.

## 2. Motor de regras determinísticas

Condições configuráveis incluem:

- CPU ou memória acima do limite.
- Disco/tablespace próximo do esgotamento.
- Backup ou replicação com falha.
- Deadlocks recorrentes.
- Fragmentação ou crescimento anormal.
- Excesso de conexões, latência ou consultas lentas.

O motor não depende de IA, possui versionamento de regra, janela, debounce, cooldown e evidência reproduzível. Seus eventos podem alimentar correlação e alertas.

## 3. Estatística e previsão

Analisar séries temporais de crescimento, capacidade, latência, throughput, conexões e duração de consultas para estimar:

- Esgotamento de espaço.
- Degradação gradual.
- Necessidade futura de recursos.
- Alteração esperada de carga.
- Anomalias em relação à sazonalidade e baseline.

Cada resultado informa modelo/versão, janela, dados usados, erro histórico, intervalo de confiança e validade temporal. Previsão não é fato observado.

## 4. Correlação inteligente

Correlacionar métricas, eventos do banco e sistema operacional, deploys, mudanças de configuração, alertas, logs e consultas lentas.

O resultado deve apresentar hipóteses ordenadas, evidências favoráveis e contrárias, lacunas e alternativas. Correlação não pode ser declarada automaticamente como causalidade.

## 5. Base de conhecimento

Fontes permitidas:

- Documentação oficial de engines e fornecedores.
- Boletins de segurança e CVEs.
- Release notes e procedimentos aprovados.
- Boas práticas versionadas.
- Incidentes anteriores e histórico interno autorizado.

Cada item possui origem, versão, data, escopo, classificação, validade e permissões. Conteúdo recuperado deve ser citado na recomendação. Material externo é tratado como dado não confiável contra prompt injection.

## 6. LLM

O LLM recebe somente contexto estruturado e sanitizado contendo métricas, eventos, histórico, alertas, correlações, previsões e trechos autorizados da base de conhecimento.

Saída estruturada mínima:

- Diagnóstico e hipóteses.
- Evidências e fontes.
- Nível de confiança e incertezas.
- Riscos e impacto potencial.
- Recomendações.
- Dados ausentes e verificações necessárias.
- Proposta de plano, sem execução.

O modelo não recebe connection string, senha ou canal de execução direta. Resposta sem evidência suficiente deve indicar `INSUFFICIENT_EVIDENCE`.

## 7. Planejamento

Converter recomendações aceitas em plano estruturado:

- Objetivo e justificativa.
- Alvo e escopo.
- Pré-requisitos.
- Evidências utilizadas.
- Impacto esperado.
- Riscos, blast radius e contraindicações.
- Ordem, timeout e janela de execução.
- Validação posterior.
- Rollback ou procedimento de recuperação.

Planejar não concede permissão para executar.

## 8. Risco, política e aprovação

| Classe | Exemplos | Regra mínima |
|---|---|---|
| Informativa | Diagnóstico, relatório | Sem mutação; registro da recomendação |
| Baixa | Atualização de estatísticas em alvo homologado | Política pré-aprovada, limites e rollback quando aplicável |
| Média | Reindexação ou manutenção com impacto | Aprovação configurada e janela de manutenção |
| Alta | Alteração de parâmetro ou indisponibilidade planejada | Aprovação humana explícita e verificação adicional |
| Crítica | Restore, upgrade, patch estrutural ou alteração destrutiva | Dupla confirmação, owner autorizado e runbook ensaiado |

A classe final é determinada por política determinística, não pelo LLM. Automação de baixo risco só pode existir após homologação específica, opt-in do administrador e limites de alvo, horário, frequência e impacto.

## 9. Executor controlado

O executor aceita apenas planos validados, aprovados e convertidos em comandos tipados. Exemplos possíveis:

- Start, Stop e Restart.
- Backup e restore.
- VACUUM, ANALYZE e REINDEX.
- Atualização, patch e configuração.

Toda operação depende de capability do provider, RBAC, idempotency key, expiração, confirmação, auditoria e probe posterior. Texto livre do LLM nunca é executado como SQL, shell ou comando administrativo.

Registrar ator, modelo/versão quando aplicável, motivo, evidências, política, aprovadores, alvo, horário, duração, comando tipado, resultado e logs sanitizados.

## 10. Feedback e aprendizado

Registrar recomendações aceitas/rejeitadas, intervenção humana, sucesso/falha, resultado observado e feedback.

Feedback pode alimentar avaliação e novo treinamento offline. Nunca altera diretamente regra crítica, política de aprovação, permissão ou modelo em produção. Promoção de nova versão exige dataset governado, avaliação, aprovação e rollback.

## Modos de operação

- `OBSERVER`: coleta, detecta e apresenta sinais.
- `ADVISOR`: produz diagnóstico e recomendações.
- `ASSISTANT`: prepara planos e executa somente após aprovação exigida.
- `CONTROLLED_AUTOMATION`: executa apenas operações tipadas, homologadas e previamente autorizadas por política.

O modo inicial obrigatório é `OBSERVER`. A promoção entre modos é explícita, por ambiente e escopo, após Quality Gate e Human Gate.

## Segurança e governança de modelos

- Proteção contra prompt injection, data poisoning e exfiltração.
- Minimização, classificação, retenção e isolamento de dados.
- Catálogo de modelos, versões, prompts, datasets e avaliações.
- Avaliação de groundedness, precisão, falso positivo, falso negativo e calibração.
- Red teaming e testes adversariais antes de produção.
- Kill switch e fallback determinístico.
- Monitoramento de drift, custo, latência e degradação.
- Proibição de ocultar erro, omitir risco ou agir com confiança insuficiente.

## Escalabilidade

Preparar para milhares de instâncias, múltiplos servidores e ambientes híbridos com filas, particionamento por tenant/ambiente, processamento assíncrono, backpressure, deduplicação e alta disponibilidade.

Modelos caros devem ser acionados por sinais relevantes, não por toda amostra. Regras, agregações e modelos estatísticos filtram e enriquecem o contexto antes do LLM.

## Métricas de qualidade

- Precisão e recall de alertas/anomalias.
- Taxa de falsos positivos e negativos.
- Calibração da confiança.
- Tempo até detectar, diagnosticar e recuperar.
- Recomendações aceitas, rejeitadas e revertidas.
- Taxa de sucesso por tipo de ação.
- Incidentes causados ou evitados pela automação.
- Custo e latência por análise.

## Critérios para implementação

- Contratos de telemetria e providers estabilizados.
- Dados históricos suficientes e classificados.
- Threat model e política de dados aprovados.
- Base de conhecimento com proveniência.
- Framework de avaliação offline e conjunto de referência.
- Modos, risco, aprovação e kill switch implementados antes do executor.
- Cenários de falha, rollback e operação sem IA homologados.

## Objetivo de longo prazo

Evoluir o DB-Notifier de monitor de bancos para plataforma AIOps inicialmente especializada em SGBDs e futuramente extensível a servidores, aplicações, containers, clusters e cloud, sem comprometer modularidade, segurança, explicabilidade ou controle humano.
