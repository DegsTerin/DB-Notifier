# O5-R1 — Escopo piloto e governança de dados do Observer

## Disposição executiva

- Data: 2026-07-24.
- Baseline: `52697b8b3c48fc906e88dd8acfb0f6735ae4c37b`.
- Natureza: elaboração documental local.
- Resultado documental: `CONCLUÍDO COM DECISÕES PENDENTES`.
- Célula piloto: `CANDIDATA`, não escolhida e não homologada.
- Corpus: não criado, não admitido e não acessado.
- Provider/banco: não iniciado e não acessado.
- Código, configuração, testes e runtime: inalterados.
- Estado MOD-12: `ActivationState=None`.
- Human Gate O5-R1: `APROVADO COM RESSALVAS`.

O O5-R1 define uma candidata única e verificável para decisão humana. Ele não escolhe PostgreSQL,
não cria suporte público e não autoriza laboratório, corpus, credencial, implementação ou
ativação. Os campos que exigem nomeação humana ou evidência de runtime permanecem bloqueados em
vez de serem inferidos.

## Célula piloto candidata

Identificador documental: `OBS-PILOT-PG16-LOCAL-001`.

| Dimensão | Valor candidato exato | Estado |
|---|---|---|
| Provider | `postgresql` | `PROPOSTO` |
| Artefacto de banco | imagem local documentada `postgres:16-alpine@sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb` | `PROPOSTO` |
| Versão semântica do servidor | a versão embutida no artefacto imutável; deverá ser lida e vinculada antes do O5-R8 | `BLOQUEADO` até autorização de laboratório |
| Plataforma do banco | Linux/Alpine no artefacto OCI acima | `PROPOSTO` |
| Plataforma candidata do Agent | Microsoft Windows 11 Enterprise `10.0.26200`, x64; .NET 10 | `PROPOSTO` |
| Host candidato para HM | 8 processadores lógicos e 15,8 GiB de memória física | `PROPOSTO` somente para esta máquina |
| Topologia | um Agent no host Windows e uma instância PostgreSQL descartável local, single-primary, sem réplica, acessível somente por TCP loopback em porta efêmera | `PROPOSTO` |
| Rede | loopback-only; sem endpoint externo, túnel, firewall automático ou serviço existente | `PROPOSTO` |
| Credencial futura | referência de monitoramento sintética, efêmera, read-only e separada de qualquer identidade administrativa | `BLOQUEADO` até autorização própria |
| TLS futuro | obrigatório para o probe autenticado; certificado exclusivamente sintético e escopo loopback | `BLOQUEADO` até autorização própria |
| Ambiente | laboratório local descartável; nunca produção | `PROPOSTO` |
| Finalidade | avaliar `OBSERVER` factual e read-only apenas para a célula | `PROPOSTO` |

A imagem foi usada anteriormente apenas no laboratório R4-B de ownership. Essa evidência comprova
a identidade local do artefacto e o isolamento daquele laboratório; não comprova o provider
monitorado, TLS, sinais Observer, corpus representativo ou homologação.

## Sinais incluídos

Somente os sinais já pertencentes ao backend slice PostgreSQL são candidatos:

| Sinal ou resultado | Contrato candidato | Critério factual |
|---|---|---|
| Configuração non-secret | endpoint tipado e validado | entrada inválida é recusada antes de conexão |
| Readiness | resultado tipado de `pg_isready` quando disponível | não substitui probe autenticado |
| TCP fallback | reachability somente de transporte | nunca é apresentado como saudável; resultado máximo `Degraded` |
| Health autenticado | probe Npgsql/TLS read-only | autenticação e resposta válidas exigidas para saúde |
| Falha de autenticação | outcome canônico não saudável | nunca convertido em indisponibilidade genérica ou sucesso |
| Duração | milissegundos bounded do probe | medida factual, não forecast |
| Freshness | instante UTC, accepted-at, stale e future skew | contexto stale/futuro falha fechado |
| Sequência e qualidade | envelope O2 versionado | duplicate/replay/gap/supersession tratados pelo pipeline aceito |
| Estado desconhecido | `Unknown` | obrigatório diante de evidência incompleta ou incompatível |

## Fora da célula

Permanecem explicitamente não suportados ou não avaliados:

- previsão de falha futura, capacity forecast ou qualquer alegação preditiva;
- recomendações, sugestões, diagnóstico gerado por LLM, comandos ou automação;
- core metrics, query text, planos, locks, tabelas, índices ou conteúdo do banco;
- version discovery como sinal de produto;
- Start/Stop/Restart, SQL administrativo ou post-action probe;
- réplica, cluster, HA, failover, managed cloud, Kubernetes ou host remoto;
- Windows service discovery e qualquer controle de serviço;
- PostgreSQL fora do artefacto e topologia exatos;
- MySQL, MariaDB, SQL Server, Oracle, MongoDB e qualquer outro provider;
- produção, dado pessoal, segredo, telemetria real ou suporte público.

Na UI Observer, qualquer área preditiva deverá continuar `Unknown`. Aprovar esta célula não
autoriza mudar esse estado.

## Finalidade e autoridade de dados

| Item | Decisão proposta | Estado |
|---|---|---|
| Finalidade | avaliação e homologação futura do Observer read-only na célula candidata | `PROPOSTA` |
| Origem permitida | envelopes canônicos gerados por cargas controladas no laboratório futuro | `PROPOSTA` |
| Origem proibida | produção, utilizador, banco existente, query, conteúdo de tabela, logs nativos ou telemetria externa | `DEFINIDA` |
| Classificação | sintético, não pessoal, não secreto e restrito ao projeto | `PROPOSTA` |
| Autoridade | corpus project-owned, criado somente por runner futuro especificamente autorizado | `BLOQUEADO` até O5-R6 |
| Data owner | pessoa distinta a nomear | `BLOQUEADO` |
| Data-governance approver | pessoa distinta a nomear | `BLOQUEADO` |
| Corpus attestation owner | papel materialmente distinto a nomear | `BLOQUEADO` |
| Security owner | pessoa a nomear | `BLOQUEADO` |
| Incident owner | pessoa a nomear | `BLOQUEADO` |
| Provider/homologation owner | pessoa a nomear | `BLOQUEADO` |
| Human Gate authority | Bruno, para as decisões explícitas deste programa | `FACTUAL` |

Data owner, data-governance approver e corpus attestation owner não poderão ser a mesma identidade.
O Security owner não poderá aprovar sozinho risco criado por código de sua própria autoria.

## Minimização e campos

O corpus candidato poderá conter somente:

- identificador opaco do caso e da revisão;
- provider ID estável `postgresql`;
- identidade content-addressed do artefacto de banco;
- plataforma e topologia declaradas;
- classe de carga controlada;
- sinal provider-neutral, unidade, valor bounded e outcome esperado;
- timestamps UTC, freshness e quality flags;
- digest de evidência e source-group;
- label, método de label, revisão e incerteza;
- partição, finalidade, retenção, expiry e withdrawal.

São proibidos:

- senha, token, connection string ou private key;
- certificado completo ou assinatura completa;
- hostname, nome de utilizador ou identidade pessoal;
- SQL, query, parâmetro, plano, schema, tabela ou conteúdo do banco;
- stack trace nativo, payload bruto, log completo ou detalhe provider-native não normalizado;
- endereço externo, topologia sensível ou informação de outro processo.

Qualquer campo não previsto exige nova revisão de governança antes da coleta.

## Retenção e retirada propostas

| Classe | Retenção máxima proposta | Disposição |
|---|---:|---|
| Buffer transitório de ingresso | 24 horas | eliminação automática após canonicalização ou recusa |
| Casos canônicos admitidos | 180 dias | expiry obrigatório; nova revisão exige nova autoridade |
| Holdout selado | 180 dias, sem reuso | retirada ou expiry torna a avaliação inelegível |
| Relatórios, manifests e decisões sanitizados | 365 dias | somente metadados e digests necessários à auditoria |
| Material secreto temporário do laboratório | zero persistência | memória/arquivo temporário restrito e cleanup no mesmo lote |

Withdrawal deverá bloquear novas avaliações imediatamente e concluir a remoção dos casos no cleanup
bounded seguinte, em no máximo 24 horas. Digests e decisão de retirada poderão permanecer pelo
prazo de auditoria, sem permitir reconstrução do conteúdo.

Esses prazos são propostas para Human Gate; não são política operacional ativa.

## Segmentos e representatividade

O corpus futuro somente poderá ser chamado representativo da matriz controlada abaixo, nunca de
produção:

| Família | Cenários obrigatórios |
|---|---|
| Availability | healthy autenticado, auth failure, timeout, refused/unavailable e TCP-only degraded |
| Latency | abaixo, no limite e acima do threshold sob carga `low`, `nominal` e `high` |
| Freshness | fresh, stale, future-skewed e evidence absent |
| Continuity | duplicate, replay, reorder, gap, restart e context supersession |
| Resource pressure | dentro do limite, limite exato, limite +1, saturation e cancellation |

Para cada célula cenário × carga aplicável:

- mínimo de 30 casos independentes em Development;
- mínimo de 30 casos independentes em Calibration;
- mínimo de 60 casos independentes no Holdout;
- source-group, run ID e evidence fingerprint disjuntos entre partições;
- nenhum caso transformado ou derivado poderá atravessar partições.

Essa amostragem mede repetibilidade da matriz controlada. Não estima prevalência, sazonalidade,
raridade de incidentes ou comportamento de produção.

## Labels e revisão

| Requisito | Threshold proposto |
|---|---:|
| Outcome controlado vinculado ao caso | 100% |
| Casos com método de label e incerteza declarados | 100% |
| Revisão independente dos labels de holdout | 100% |
| Concordância entre controlador do cenário e revisão | 100% |
| Label conflitante não resolvido | 0 |
| Label inferido de texto livre | 0 |
| Alteração de label após freeze | 0 |

Um conflito remove o caso da revisão corrente; não pode ser resolvido depois de abrir o holdout.

## Partições e integridade

| Requisito | Threshold proposto |
|---|---:|
| Membership autenticada | 100% |
| Conteúdo com digest válido | 100% |
| Segmentos obrigatórios presentes | 100% |
| Campo obrigatório ausente | 0 |
| Duplicidade de case ID, source-group ou evidence fingerprint | 0 |
| Leakage entre partições | 0 |
| Conflito de outcome para o mesmo cenário | 0 |
| Segredo, dado pessoal ou campo proibido | 0 |
| Holdout aberto antes do freeze ou reutilizado | 0 |
| Corpus expirado, retirado ou superseded aceito | 0 |

Qualquer violação falha fechado e impede calibração.

## Métricas e thresholds de qualidade

Os thresholds abaixo são propostos para o comportamento determinístico e deverão ser aprovados
antes do holdout:

| Métrica por segmento | Threshold proposto |
|---|---:|
| Cobertura dos cenários obrigatórios | 100% |
| Resultado canônico correto em casos completos conhecidos | 100% |
| Falso positivo para estado não saudável | 0 no holdout |
| Falso negativo para estado não saudável | 0 no holdout |
| `Unknown` para evidência incompleta/incompatível | 100% |
| Stale/future skew recusado ou marcado corretamente | 100% |
| TCP-only apresentado como saudável | 0 |
| Resultado parcial publicado | 0 |
| Explicação ligada à evidência/política/corpus exatos | 100% |
| Resultado autorizador, recomendação ou ação | 0 |

Com pelo menos 60 casos de holdout por célula e zero erro observado, o limite superior aproximado
de 95% para a taxa real de erro da célula permanece próximo de 5%. Isso não prova produção e não
pode ser agregado para esconder reprovação de um segmento.

Forecast accuracy não possui threshold porque forecast está fora da célula. A saída obrigatória
continua `Unknown`.

## Ambiente candidato para `HM-01`–`HM-03`

| Dimensão | Declaração candidata |
|---|---|
| Host | Windows 11 Enterprise `10.0.26200`, x64 |
| Capacidade observável | 8 processadores lógicos, 15,8 GiB de memória |
| Runtime | .NET 10; versão exata deverá ser capturada no início da campanha |
| Banco | artefacto OCI imutável da célula |
| Isolamento | loopback-only, recursos de container bounded e zero workload externo do projeto |
| Repetições | warm/cold, no mínimo 30 por cenário; distribuição, percentis e pior caso |
| `HM-01` | first-byte, idle, cancellation e control-update latency sob carga máxima admitida |
| `HM-02` | heap, working set e allocation peak versus fórmula contabilizada |
| `HM-03` | CPU/elapsed versus work units em crypto, parse, sort e análise |

Nenhum limite físico numérico foi aprovado neste documento. O5-R3 deverá primeiro definir SLOs
numéricos e O5-R5 deverá medi-los. Inventar números a partir dos sandboxes continua proibido.

## Critérios mensuráveis para O5-R2

O futuro O5-R2 somente poderá ser proposto depois do Human Gate O5-R1 e deverá comprovar:

1. default, ausência ou configuração inválida resultam sempre em `ActivationState=None`;
2. opt-in autenticado e one-use vincula exatamente a célula, ambiente, finalidade e expiry;
3. qualquer ampliação de escopo é recusada;
4. kill switch impede 100% das novas admissões após commit;
5. trabalho corrente é cancelado dentro do deadline posteriormente aprovado;
6. zero publicação ocorre a partir de contexto revogado, superseded ou anterior ao fence;
7. quarantine/recovery/rollback aceitam somente estado antigo ou novo completo;
8. restart, corrupção, restore antigo e falha parcial não reativam o Observer;
9. monitoramento determinístico permanece independente;
10. zero recomendação, comando, executor ou efeito externo existe;
11. composição normal em `None` produz zero avaliação e zero publicação Observer;
12. cleanup termina sem processo, listener, store ou preferência residual.

O5-R2 exigirá autorização separada para código e configuração locais. Este relatório não a concede.

## Critérios para futura admissão de corpus

Antes do O5-R6:

- a célula deverá ser formalmente escolhida;
- todos os owners deverão ser nomeados e aceitar suas responsabilidades;
- a autoridade do laboratório e do corpus deverá ser aprovada;
- retenção, withdrawal, campos e thresholds deverão ser ratificados;
- O5-R2–R4 deverão estar aceitos;
- a fonte deverá permanecer local, sintética e controlada, salvo autorização expressa diferente;
- o runner futuro deverá ser aprovado antes de criar qualquer caso;
- nenhuma coleta poderá começar enquanto um campo estiver `BLOQUEADO`.

## Riscos e critérios de parada

| Risco | Parada obrigatória |
|---|---|
| Célula apresentada como suporte PostgreSQL | qualquer palavra ou UI que implique homologação ou suporte público |
| Imagem não corresponde ao digest | não iniciar laboratório |
| Versão semântica diverge da célula aceita | rejeitar admissão e solicitar nova decisão |
| Owner ausente ou papéis incompatíveis | não autorizar corpus ou control plane |
| Campo secreto/pessoal/provider-native proibido | recusar e eliminar o caso |
| TLS ou credencial não isolados | não conectar |
| Segmento ou partição insuficiente | não abrir holdout |
| Threshold alterado após freeze | invalidar avaliação |
| Resultado preditivo inferido | manter `Unknown` e reprovar o lote |
| Necessidade de rede externa, download ou banco existente | parar e solicitar autoridade separada |
| Qualquer ativação real | parar; O5-R1 não autoriza transição |

## Autorizações adicionais necessárias

| Próxima atividade | Autoridade necessária |
|---|---|
| Decidir a célula e thresholds | Human Gate O5-R1 |
| Nomear owners | decisão explícita dos responsáveis |
| Implementar O5-R2 | código/configuração local, testes e commit próprios |
| Definir/ensaiar SLOs O5-R3 | instrumentação e simulações locais |
| Security review O5-R4 | revisão e red team autorizados |
| Executar `HM-01`–`HM-03` | runtime local, profiler/counters e limites de recursos |
| Criar/admitir corpus O5-R6 | runner, laboratório, corpus e retenção autorizados |
| Executar holdout O5-R7 | corpus aceito e política congelada |
| Homologar O5-R8 | Docker/provider/banco real de laboratório e credencial sintética |

## Conclusão

- O5-R1 documental: `CONCLUÍDO COM DECISÕES PENDENTES`.
- Célula `OBS-PILOT-PG16-LOCAL-001`: proposta para Human Gate, não escolhida.
- PostgreSQL: `Homologation=None`; suporte público `No`.
- Owners de dados, governança, segurança, incidente e homologação: bloqueados até nomeação.
- Corpus e laboratório: não autorizados.
- O5-R2: não autorizado.
- `ActivationState`: `None`.
- `OBSERVER` e lifecycle: sem transição.

Bruno decidiu exatamente `HUMAN GATE O5-R1: APROVADO COM RESSALVAS` em 2026-07-24. A decisão
aceita a célula apenas como escopo candidato exclusivo do futuro laboratório, preserva todos os
limites de homologação, suporte, representatividade e autoridade e mantém os owners pendentes.
O [relatório do Human Gate O5-R1](STATE-06-MOD-12-O5-R1-Human-Gate-Report.md) registra a decisão
integral.

O próximo passo elegível é somente apresentar a proposta do O5-R2. Sem a nomeação dos owners,
O5-R6 e O5-R8 continuam bloqueados. O5-R2, implementação, runtime, laboratório, corpus, provider,
banco, `OBSERVER` e transição permanecem não autorizados.
