# Proposta STATE-06 MOD-12 — Governança de Confiança e Envelope de Recursos

## Status e autoridade

- Data: 2026-07-17
- Posição do ciclo de vida: `STATE-06 INTEGRATION`
- Estado dos modos MOD-12: nenhum modo ativo; `none → OBSERVER` permanece pendente
- Natureza deste artefato: proposta exclusivamente documental e não executiva
- Origem: solicitação de Bruno por uma proposta para o próximo incremento restrito, sem implementação, runtime, ações externas ou promoção para `OBSERVER`
- Baseline aceita: remediação local do commit `6a5f00f`, registrada no [`relatório de proveniência e budget`](STATE-06-MOD-12-Provenance-And-Budget-Remediation-Report.md)

Este documento não autoriza o incremento proposto. Ele organiza uma possível próxima atividade para que Bruno possa revisar seu objetivo, limites, riscos e critérios antes de decidir separadamente se deseja autorizá-la.

## Resumo para não especialistas

O MOD-12 já consegue, em testes locais, verificar políticas assinadas, recusar uma lista de revogação antiga e limitar avaliações offline. Ainda faltam duas definições antes de sequer considerar uma integração operacional:

1. quem teria autoridade para emitir, distribuir, atualizar e revogar essas políticas e chaves sem permitir retorno silencioso a uma versão antiga;
2. quanto trabalho, memória, tempo e concorrência poderiam ser aceitos com segurança quando os dados deixarem de ser pequenas fixtures locais.

A próxima atividade recomendada é desenhar essas regras no papel, revisar ameaças e preparar testes futuros. Ela não ligaria o MOD-12, não criaria serviços e não coletaria dados.

## Nome recomendado do próximo incremento

`MOD-12 — Trust Governance and Resource Envelope Design`

Em linguagem simples: desenho da cadeia de confiança, da revogação durável e dos limites máximos de processamento.

## Objetivo

Produzir uma decisão arquitetural e de segurança revisável para o futuro fornecimento de políticas confiáveis e para a futura ingestão limitada de telemetria, preservando o verificador MOD-12 como componente puro, provider-neutral, sem segredo, sem I/O e sem autoridade operacional.

O incremento proposto terminaria em documentação e vetores de teste especificados. Ele não terminaria em código ou runtime.

## Resultados documentais propostos

### 1. Mapa de responsabilidades e identidades

Definir separadamente, sem criar os componentes:

- autoridade raiz de confiança;
- autoridade de política, responsável por decidir legitimamente finalidade, escopo máximo e período;
- emissor de grants de política;
- emissor de snapshots de revogação;
- publicador de configuração confiável;
- custodiante futuro das chaves privadas em cofre ou serviço de assinatura aprovado;
- consumidor/verificador MOD-12;
- proprietário do checkpoint durável;
- operador autorizado para rotação, revogação e recuperação de compromisso;
- proprietário do corpus e das avaliações offline.

O mapa deve declarar quem pode autorizar, quem pode assinar, quem custodia a chave, quem pode publicar, quem pode apenas verificar e quem não pode substituir uma decisão de segurança. Uma assinatura comprova origem e integridade, não comprova sozinha que a política foi legitimamente autorizada; o desenho deve impor um `scope ceiling` e auditoria próprios da autoridade de política.

### 2. ADR de distribuição de confiança e revogação

Preparar um ADR, ainda sem implementação, que decida:

- raiz de confiança e forma futura de provisionamento;
- separação entre raiz, chaves operacionais de grant e chaves operacionais de revogação;
- contrato versionado do pacote confiável;
- vínculo entre autoridade, série, finalidade, escopo e geração;
- aplicação transacional local entre âncoras, checkpoint e snapshot vigente, sem prometer atomicidade global simultânea entre consumidores;
- sequência monotônica e proteção contra rollback, freeze e avanço indevido;
- rotação normal, sobreposição temporária, retirada de chave e recuperação por comprometimento;
- comportamento offline, expiração, relógio incorreto e indisponibilidade da autoridade;
- política fail-closed e códigos de recusa sanitizados;
- compatibilidade e rollback seguro do próprio contrato.

O ADR deve comparar alternativas e registrar por que a escolhida é mais segura. Nenhuma chave real, certificado ou serviço externo seria criado.

### 3. Contrato conceitual do pacote confiável

Especificar, sem classe C# ou schema persistente, os campos mínimos de um futuro pacote assinado, por exemplo:

- versão do schema;
- identidade da autoridade e da série;
- geração monotônica;
- finalidade e escopo autorizados;
- período de validade em UTC;
- conjunto de chaves públicas ativas e retiradas por função;
- checkpoint de revogação exigido;
- digest da geração anterior para encadeamento;
- algoritmo permitido e política de migração criptográfica;
- limites de tamanho e cardinalidade;
- assinatura e evidência de auditoria.

O desenho deve impedir que evidência não confiável carregue sua própria raiz de confiança e deve distinguir claramente identidade, autorização, integridade, revogação e estado durável.

### 4. Modelo de checkpoint durável

Definir conceitualmente:

- chave lógica do checkpoint, incluindo autoridade, série, finalidade e escopo;
- valor monotônico mínimo aceito e digest associado;
- escrita atômica e proteção contra concorrência;
- recuperação após falha entre validação e persistência;
- comportamento após restauração de backup antigo;
- detecção de corrupção, ausência, divergência e split view;
- quarentena fail-closed após checkpoint ausente, restaurado ou divergente, até reconciliação autenticada;
- regra de nunca diminuir o checkpoint automaticamente;
- auditoria de avanço, rejeição, rotação e recuperação;
- separação entre o verificador puro e o futuro armazenamento pertencente ao host.

A proposta não escolhe nem cria tabela, migration ou banco. Um store restaurável, sozinho, não garante anti-rollback: perda ou restauração do estado monotônico deve bloquear o consumo até uma reconciliação autenticada. Essa decisão só seria materializada em incremento posterior e especificamente autorizado.

### 5. Envelope de recursos e backpressure

Definir uma unidade de admissão futura que não confie somente na contagem declarada pelo produtor. O contrato documental deve abranger:

- número máximo de lotes, casos e amostras;
- tamanho máximo por item e total em bytes codificados e descomprimidos;
- unidades de trabalho determinísticas;
- tempo total e deadlines intermediários;
- cancelamento obrigatório;
- máximo de paralelismo e de trabalho simultâneo;
- quotas globais e por escopo, com fairness para impedir monopolização;
- profundidade de fila, se uma fila vier a ser autorizada no futuro;
- teto de memória e política de descarte seguro;
- validação incremental entre contagens declaradas e observadas;
- rejeição de fonte infinita, mutável, excessiva ou que omita tamanho;
- limites para parsing, ordenação, descompressão e cardinalidade;
- aritmética protegida contra overflow e custo cobrado antes de alocação relevante;
- latência máxima para observar cancelamento;
- semântica explícita para resultado parcial, sempre não autorizante e sem continuação implícita;
- códigos de backpressure sem conteúdo sensível;
- ausência de retry ou aceitação parcial implícita.

O default recomendado para qualquer futura prova local é processamento serial (`maximum parallelism = 1`) até que medições reproduzíveis justifiquem outra decisão. Números de produção não devem ser inventados nesta etapa.

### 6. Extensão do threat model

Adicionar ao desenho futuro, com controle e teste correspondente:

| Ameaça | Exemplo | Controle documental esperado |
|---|---|---|
| Rollback | snapshot antigo ainda assinado volta a ser apresentado | checkpoint monotônico durável e recusa explícita |
| Freeze | distribuidor deixa de avançar a revogação | validade curta, freshness e alarme/auditoria futura |
| Fast-forward/DoS | sequência futura bloqueia a configuração legítima | autenticação, série exata, atualização atômica e recuperação controlada |
| Split view | consumidores recebem gerações diferentes | digest encadeado, reconciliação e evidência de auditoria |
| Replay entre escopos | pacote válido de outro tenant ou ambiente é reapresentado | vínculo assinado de tenant/ambiente/finalidade e recusa cross-scope |
| Compromisso de signer | chave operacional assina política indevida | separação de funções, revogação, rotação e procedimento de recuperação |
| Compromisso da raiz/revogação | autoridade superior ou signer de revogação é tomado | procedimento extraordinário, nova raiz autorizada e quarentena |
| Emissão indevida | signer válido assina algo não aprovado pela autoridade de política | scope ceiling, autorização separada e auditoria verificável |
| Confusão de chave/algoritmo | mesma chave assume duas funções ou algoritmo enfraquece | key IDs/material distintos, allowlist e versionamento criptográfico |
| Relógio manipulado | validade parece futura ou antiga | UTC confiável, tolerância explícita e fail-closed |
| Atualização parcial | âncora avança sem checkpoint correspondente | pacote indivisível e commit atômico futuro |
| TOCTOU/crash | pacote muda ou processo falha entre verificar e gravar | digest fixo, revalidação e transação local idempotente |
| Backup antigo | store restaurado reduz o checkpoint | proteção de restauração e reconciliação antes de aceitar evidência |
| Fonte mentirosa | produtor declara poucas amostras e entrega muitas | contagem observada, limite de bytes e interrupção imediata |
| Exaustão de memória | item ou corpus causa alocação excessiva | limites antes de parsing/materialização e processamento incremental |
| Concorrência descontrolada | múltiplas avaliações ultrapassam o orçamento | limite global, cancelamento e backpressure determinístico |
| Poisoning | corpus autorizado contém evidência adulterada | manifest, digest, proveniência, revisão e fixtures adversariais |
| Auditoria adulterada | evidência de avanço ou revogação é removida | log protegido, separação de função e reconciliação independente |
| Bundle bomb | pacote pequeno expande ou exige parsing excessivo | limites codificado/descomprimido, streaming limitado e custo antes de alocar |

Esta tabela seria incorporada ao threat model normativo somente se o incremento documental for posteriormente autorizado e revisado. Digest encadeado ajuda a detectar inconsistência, mas não prova ausência de split view sem witness, reconciliação autenticada ou auditoria independente; esse risco residual deve permanecer explícito.

### 7. Plano de validação futura

Especificar vetores determinísticos, sem executá-los nesta etapa:

- sequência menor, igual e maior que o checkpoint;
- mesmo número com digest ou série divergente;
- snapshot expirado, ainda não vigente e assinado por função errada;
- rotação normal, sobreposição, chave retirada e chave comprometida;
- atualização interrompida antes e depois do avanço durável;
- restauração de checkpoint antigo;
- fonte cuja contagem muda, mente ou nunca termina;
- item acima do limite e soma acima do limite;
- cancelamento durante parsing, ordenação e análise;
- duas admissões simultâneas competindo pelo mesmo limite global;
- dois escopos competindo por quota e fairness;
- tamanho codificado aceitável que excede o limite após descompressão;
- overflow em somas de bytes, trabalho ou cardinalidade;
- cancelamento cuja observação ultrapassa a latência permitida;
- ausência de memória suficiente e clock fora do intervalo permitido;
- corpus adulterado, duplicado, cross-scope e com distribuição enviesada.

Cada vetor deve indicar pré-condição, entrada, decisão esperada, código fail-closed, trabalho/memória permitidos e evidência necessária.

## Defaults recomendados para a futura decisão

Estes são pontos de partida para revisão, não decisões já aceitas:

- o MOD-12 continua sem possuir chave privada, rede ou persistência;
- a raiz de confiança não é fornecida pelo mesmo pacote que ela valida;
- grant, revogação e publicação usam funções e chaves distintas;
- o host futuro, não o verificador, possui o checkpoint durável;
- âncoras, checkpoint e snapshot são aplicados por transação local indivisível, sem alegação de atomicidade global;
- expiração ou ausência de revogação atual falha de forma fechada;
- nenhum downgrade automático de geração, schema ou algoritmo é permitido;
- qualquer recuperação por comprometimento exige autoridade humana e auditoria próprias;
- budget inclui bytes codificados/descomprimidos, memória, parsing, trabalho e latência de cancelamento, não apenas contagens e tempo;
- quotas globais e por escopo preservam fairness;
- qualquer prova inicial continua serial e local;
- dado de corpus precisa de manifest, digest, classificação, owner, finalidade e expiração;
- nenhuma conclusão documental é tratada como evidência runtime.

## Escopo proposto

| Área | Dentro do próximo incremento documental | Fora do escopo |
|---|---|---|
| Confiança | responsabilidades, ADR, contrato conceitual e rotação | geração de chaves, certificados, vault ou signing service |
| Revogação | sequência, checkpoint, atomicidade e recuperação desenhados | store, migration, API ou distribuição real |
| Recursos | limites conceituais, ordem de admissão e vetores | fila, worker, streaming ou medição de produção |
| Segurança | threat model e matriz ameaça-controle-teste | penetration test, red team ou integração externa |
| Corpus | governança, manifest e plano adversarial | coleta, importação, persistência ou ampliação efetiva |
| Produto | documentação e decisão de arquitetura | DI, Agent, API, UI, provider ou notificação |
| IA | guardrails e ausência de autoridade | LLM, recomendação, planejamento ou executor |
| Ciclo de vida | `STATE-06` preservado | `none → OBSERVER`, `STATE-07` ou release |

## Fora de escopo absoluto

- alteração de código, testes executáveis, migrations ou configuração runtime;
- criação ou uso de segredo, chave privada, certificado ou credencial;
- banco, provider, API, Agent, Dashboard, WPF, serviço, worker ou fila;
- rede, internet, cloud, vault, signing service ou outra ação externa;
- coleta, persistência ou telemetria operacional;
- LLM, recomendação, plano, comando, executor ou ação administrativa;
- calibração, homologação, produção ou anúncio de suporte;
- promoção para `OBSERVER` ou qualquer outro modo;
- transição de ciclo de vida.

## Sequência documental sugerida

1. Confirmar os ativos, autoridades e consumidores futuros sem escolher tecnologia externa.
2. Elaborar alternativas de distribuição e checkpoint, com análise de trade-offs.
3. Escolher uma alternativa em ADR sujeito à revisão de arquitetura e segurança.
4. Especificar pacote confiável, atomicidade, rotação e recuperação.
5. Definir envelope de bytes, memória, trabalho, tempo e concorrência.
6. Atualizar o threat model e a matriz de rastreabilidade.
7. Especificar vetores de teste futuros e critérios de saída.
8. Executar apenas gates documentais e apresentar o pacote para decisão humana.

## Critérios de aceite do incremento proposto

O incremento documental somente poderia ser considerado concluído quando:

- todas as responsabilidades e trust boundaries estiverem inequívocas;
- o ADR comparar ao menos três alternativas e justificar a decisão;
- rollback, freeze, split view, fast-forward, compromisso e restore antigo tiverem controles definidos;
- ausência/restauração de checkpoint conduzir a quarentena até reconciliação autenticada;
- rotação normal e recuperação de compromisso estiverem separadas;
- o checkpoint nunca puder diminuir por comportamento normal;
- o envelope cobrir contagem, bytes codificados/descomprimidos, memória, parsing, overflow, tempo, cancelamento, quotas, fairness, resultado parcial e concorrência;
- toda ameaça possuir controle, vetor de teste futuro e owner;
- nenhuma decisão depender de segredo no repositório ou de provider específico;
- não houver `TBD` crítico escondido ou apresentado como fato resolvido;
- os documentos declararem explicitamente que não existe runtime nem promoção;
- revisão de arquitetura, segurança e dados estiver registrada;
- links, headings, terminologia, diff e secret scan documentais passarem;
- Bruno emitir decisão humana separada sobre o pacote documental.

## Quality Gate aplicável

Classificação proposta para esse futuro incremento:

- build e testes de produto: `NOT APPLICABLE`, pois código seria proibido;
- revisão de arquitetura: `REQUIRED`;
- revisão de segurança/threat model: `REQUIRED`;
- revisão de governança de dados: `REQUIRED`;
- rastreabilidade ameaça → controle → teste: `REQUIRED`;
- documentação, links, headings, termos e secret scan: `REQUIRED`;
- inspeção de diff provando ausência de código/runtime: `REQUIRED`;
- Human Gate do incremento documental: `REQUIRED`;
- gate `none → OBSERVER`: permanece `PENDING` e não faz parte desta proposta.

## Riscos da própria proposta

- **Sobrepromessa:** um desenho aprovado pode parecer implementação. Mitigação: repetir em cada artefato que não existe runtime.
- **Circularidade de confiança:** permitir que o pacote traga sua raiz tornaria a assinatura inútil. Mitigação: raiz provisionada por boundary independente.
- **Assinatura confundida com autorização:** um signer válido pode exceder a decisão de política. Mitigação: authority, signer/custodian e publisher separados, scope ceiling e auditoria.
- **Anti-rollback aparente:** um store restaurável pode voltar no tempo junto com o checkpoint. Mitigação: quarentena e reconciliação autenticada antes de novo consumo.
- **Atomicidade sobreprometida:** transação local não atualiza todos os consumidores ao mesmo tempo. Mitigação: declarar convergência/reconciliação e risco residual de split view.
- **Persistência prematura:** escolher tabela ou banco antes do owner. Mitigação: decidir primeiro responsabilidade e invariantes.
- **Limites inventados:** números arbitrários dariam falsa segurança. Mitigação: definir dimensões e método; números operacionais exigem evidência posterior.
- **Escopo excessivo:** misturar LLM, UI ou executor impediria revisão clara. Mitigação: manter o incremento somente em confiança e recursos pré-runtime.
- **Promoção implícita:** concluir documentação não comprova `OBSERVER`. Mitigação: manter o gate de modo independente e pendente.

## Caminho futuro condicionado

Se este incremento documental vier a ser autorizado, concluído e aceito, ele ainda não liberará implementação. O passo seguinte seria uma nova proposta separada para contratos locais e testes adversariais, também sem runtime. Somente evidências posteriores de implementação, segurança, corpus, carga e integração poderiam alimentar uma proposta independente de `none → OBSERVER`.

## Decisão atualmente solicitada

Nenhuma autorização de execução é inferida deste documento. Bruno poderá:

- aceitar a proposta como base e autorizar separadamente o incremento documental;
- pedir ajustes na proposta;
- rejeitar ou adiar a atividade.

Até uma decisão explícita, a próxima atividade permanece apenas proposta, o workspace continua em `STATE-06 INTEGRATION` e nenhum modo MOD-12 está ativo.
