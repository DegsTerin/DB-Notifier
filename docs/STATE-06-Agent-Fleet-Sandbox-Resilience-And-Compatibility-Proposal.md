# Proposta STATE-06 — Resiliência e Compatibilidade do Agent Fleet em Sandbox

## Status e autoridade

- Data: 2026-07-18
- Posição do ciclo de vida: `STATE-06 INTEGRATION`
- Natureza: proposta exclusivamente documental e não executiva
- Origem: solicitação de Bruno por uma proposta para o próximo incremento restrito de `STATE-06`, sem implementação, promoção ou transição de estado
- Baseline aceita: incrementos server-side `cc2d828`/`c5e3cbd` e Agent-side `beb936b`, com as limitações registradas e respectivas decisões humanas
- Estado operacional: o cliente Agent Fleet permanece indisponível no Worker normal; todos os runtimes estão encerrados

Este documento não autoriza implementação, alteração de código ou configuração, migration, build, teste executável, abertura de runtime ou ação externa. Também não aprova o Quality/Human Gate de saída de `STATE-06`. Ele apresenta uma única opção delimitada para decisão humana posterior.

## Resumo para não especialistas

O laboratório local já demonstrou que um Agent de teste consegue obter uma identidade temporária, avisar ao servidor que continua ativo e guardar uma cópia somente de leitura das suas atribuições. Essa demonstração foi segura, mas executou cada operação de maneira controlada e quase sempre dentro do mesmo processo de teste.

O próximo passo recomendado é testar como essa integração se comporta quando as coisas dão errado: conexão interrompida, resposta repetida, versões incompatíveis, certificado expirado, dois processos concorrendo pelo mesmo arquivo local, encerramento entre duas gravações ou armazenamento temporariamente indisponível. O objetivo não é ligar o Agent real. É descobrir e corrigir, somente num sandbox local e descartável, falhas de continuidade e comportamento ambíguo antes de considerar qualquer runtime operacional.

Em termos simples, este incremento futuro funcionaria como um teste de resistência de uma maquete: desligar e religar, repetir mensagens e simular defeitos controlados para verificar se ela conserva o último estado confiável, não duplica trabalho e falha de forma segura.

## Nome recomendado do próximo incremento

`STATE-06 — Agent Fleet Sandbox Resilience and Protocol Compatibility`

Em linguagem simples: provar que a integração de identidade, heartbeat e assignments read-only se recupera com segurança de falhas locais e incompatibilidades, ainda sem ativá-la como serviço normal.

## Por que este é o próximo passo

Os incrementos aceitos já implementaram o caminho nominal server-side e Agent-side. As limitações aceitas identificam agora um conjunto coeso de riscos de integração:

1. o reinício comprovado foi apenas a reconstrução lógica do coordinator no mesmo processo;
2. não existe evidência de cadência, backoff, jitter, cancelamento ou exclusão de chamadas concorrentes;
3. expiração e incompatibilidade falham fechadas no código, mas não foram dirigidas separadamente no E2E Agent-side;
4. concorrência, encerramento em pontos críticos, arquivo corrompido, `busy/locked` e falta de espaço no SQLite não foram exercitados;
5. a corrida entre leitura de assignments e revogação ainda não possui uma prova integrada de consistência;
6. a matriz obrigatória de `STATE-06` ainda carece de evidência mais ampla sobre reconexão, duplicidade, reorder e compatibilidade `N/N-1`.

Tratar essas lacunas antes de acrescentar provider, monitoramento, UI, notificações ou identidade operacional mantém o risco pequeno e evita que funções novas escondam defeitos do protocolo já existente.

## Objetivo do incremento futuro

Endurecer e validar, exclusivamente em sandbox local, as fronteiras já implementadas de enrollment de teste, heartbeat e reconciliação read-only de assignments para que:

- um reinício real entre processos preserve identidade pública, heartbeat pendente, sequência e último snapshot válido;
- chamadas repetidas ou reordenadas permaneçam idempotentes e factualmente classificadas;
- retries ocorram somente para falhas transitórias, com limites, cancelamento, backoff e jitter determinísticos;
- apenas uma operação de cada tipo possa alterar o store local por vez;
- expiração, revogação, conflito e incompatibilidade parem o fluxo sem reenrollment ou retry infinito;
- falhas controladas de SQLite nunca promovam estado parcial ou apaguem o último snapshot válido;
- a corrida assignment/revogação resulte em recusa ou evidência claramente limitada, sem apresentar configuração como autorizada depois de revogação comprovada;
- o Worker normal continue incapaz de ativar o cliente Agent Fleet.

## Baseline factual que não deve ser reinterpretada

- Enrollment, heartbeat e assignments v1 já existem e possuem cobertura local; esta proposta não cria um protocolo novo.
- A chave privada e a CA existem somente dentro da fixture E2E efêmera.
- O Worker comum não registra identity adapter, transport ou scheduler e recusa `AgentFleetClient.Enabled=true` com `agent_fleet.sandbox_only`.
- O heartbeat pendente já é persistido e repetido exatamente depois de perda simulada de resposta.
- Assignments já são validados por versão, vínculo, ETag, digest e limites, depois substituídos atomicamente no SQLite.
- A evidência de restart atual reconstrói o coordinator sobre o mesmo processo e store vivo; não prova encerramento e abertura de processos independentes.
- Não há key store, issuer, provisionador de token, rotação, recovery, PKI, trust distribution ou identidade operacional.
- Não há provider ativo, probe, monitoramento, acknowledgment operacional de assignment, SignalR, UI, notificação ou comando.

## Referências factuais

- [Relatório aceito do incremento Agent-side](STATE-06-Agent-Side-Identity-And-Assignment-Reconciliation-Report.md)
- [Relatório aceito do incremento server-side](STATE-06-Agent-Identity-And-Fleet-Integration-Report.md)
- [Protocolo Agent/API v1](architecture/Agent-API-Protocol.md)
- [Estado factual corrente](../prompts/state/Current-State.md)
- [Lifecycle e critérios de STATE-06](../prompts/governance/Lifecycle.md)

## Escopo proposto para implementação posterior

### 1. Harness sandbox separado da composição normal

Criar ou ampliar somente o harness de integração local para executar o coordinator Agent-side em processos temporários e controlados. O harness deverá:

- usar um argumento e ambiente de teste inequívocos;
- aceitar somente caminhos, portas e material criados pela própria fixture;
- recusar qualquer endpoint que não seja loopback;
- recusar store, certificado ou token fora da raiz temporária da fixture;
- não reutilizar a composição operacional do Worker;
- encerrar processos filhos, listeners e arquivos ao final, inclusive após falha do teste.

O harness não será serviço, worker instalável, daemon, API pública ou caminho de ativação. Qualquer futura autorização de implementação deverá mencionar expressamente esses runtimes locais temporários.

### 2. Cadência e retry determinísticos

Adicionar ao limite interno adequado um orquestrador de teste que execute heartbeat e reconciliação com:

- `TimeProvider` controlado;
- atraso e jitter injetáveis, sem espera real longa nos testes;
- backoff exponencial limitado e teto de `Retry-After`;
- máximo explícito de tentativas e tempo total;
- uma única chamada heartbeat e uma única reconciliação em voo;
- cancelamento observável antes, durante e depois do transporte;
- classificação tipada entre transitório, terminal, incompatível e revogado;
- zero retry para identidade expirada/revogada, versão inválida, schema inválido, conflito ou payload acima do limite.

O orquestrador continuará exclusivo do sandbox. Não haverá scheduler registrado no Worker normal nem loop contínuo fora dos testes.

### 3. Continuidade entre processos

O E2E futuro deverá encerrar deliberadamente um processo de teste e iniciar outro contra o mesmo SQLite efêmero para provar:

- identidade já matriculada não dispara novo enrollment;
- heartbeat pendente é repetido byte a byte e só avança após recibo compatível;
- uma interrupção depois da aceitação server-side e antes do commit local não duplica efeito;
- a versão e o último snapshot válido são reabertos sem reclassificação otimista;
- estado `RevokedOrDenied`, `Expired`, `Incompatible` ou `Conflict` permanece fail-closed depois do restart;
- nenhum segredo é transferido por argumento, variável de ambiente, arquivo comum ou log.

O bootstrap do processo filho receberá apenas referências efêmeras e não secretas. Para que processos independentes usem a mesma identidade de teste, a fixture poderá manter o material numa fronteira privada exclusiva do E2E, como IPC anônimo controlado ou um key container temporário protegido e criado pelo próprio teste. A escolha deverá ser documentada, ficar encapsulada no adapter E2E e impedir valor bruto em argumentos, variáveis de ambiente, arquivo comum ou log. Isso provará somente a continuidade do estado da aplicação; não provará key store operacional. Todo material será descartado ao final.

### 4. Compatibilidade e falhas de protocolo

Exercitar explicitamente, sem criar suporte fictício:

- Server `N` com Agent `N` no caminho nominal;
- Agent `N-1` somente quando o contrato declarar compatibilidade explícita;
- versão futura, schema desconhecido, header ausente e body/header divergentes;
- resposta duplicada, atrasada, reordenada, parcial e alterada;
- heartbeat com sequência antiga, gap, mesmo ID/conteúdo diferente e mesmo conteúdo/ID diferente;
- ETag fraca, ETag/digest divergentes e `304` incompatível com a versão local;
- relógio antes/depois da validade do certificado, usando tempo controlado;
- revogação antes da chamada, durante uma chamada bloqueada e imediatamente após uma resposta.

Resultados desconhecidos não serão convertidos em revogação comprovada. O Agent preservará o último estado confiável, registrará código sanitizado e exigirá nova autoridade quando recuperação automática não estiver definida.

### 5. Concorrência e exclusão local

Provar que duas instâncias temporárias não conseguem avançar simultaneamente a mesma registration. A implementação futura poderá introduzir somente o mecanismo local mínimo, por exemplo lease/fencing ou transação de aquisição, desde que:

- o owner e a expiração sejam explícitos;
- um processo antigo não possa confirmar trabalho depois de perder o lease;
- o relógio e o timeout sejam controláveis;
- lock abandonado seja recuperável sem apagar evidência;
- a disputa resulte em `Busy`/`Conflict` sanitizado, não em duas operações ativas;
- nenhuma coordenação distribuída ou serviço externo seja criada.

### 6. Falhas controladas do SQLite

Usar somente stores temporários e fault injection determinística para cobrir:

- `busy/locked` e timeout de lock;
- falha antes e depois de cada fronteira transacional relevante;
- encerramento após envio e antes/depois do commit local;
- rollback da substituição completa de assignments;
- arquivo ilegível/corrompido copiado exclusivamente da fixture;
- falta de espaço simulada pelo adapter de persistência ou VFS de teste, sem preencher disco real;
- abertura com schema anterior, futuro ou migration incompleta.

O teste nunca deverá consumir deliberadamente o espaço do computador, corromper banco fora da fixture ou matar processo alheio. Diante de corrupção não recuperável, o comportamento esperado será parar e preservar o artefato sanitizado para diagnóstico; não recriar silenciosamente o store.

### 7. Consistência entre assignment e revogação

O cenário server-side deverá coordenar deterministicamente a corrida já documentada entre leitura do Agent/projeção e revogação. O aceite mínimo será:

- uma revogação confirmada impede a próxima operação autenticada;
- nenhuma resposta iniciada depois da revogação pode ser emitida como autorizada;
- uma resposta iniciada antes e concluída durante a revogação recebe semântica factual documentada;
- o Agent coloca a identidade em quarentena assim que recebe negação comprovada;
- o LKG eventualmente preservado continua marcado como configuração histórica, não como autorização atual.

Se a implementação existente não puder garantir isso sem uma mudança arquitetural ou transação mais ampla, o incremento deverá parar no achado e propor ADR/remediação separada; não poderá ampliar silenciosamente o escopo.

### 8. E2E sandbox proposto

O conjunto deverá permanecer pequeno, determinístico e totalmente local. Cenários mínimos:

1. defaults do Worker comum não criam banco, processo filho ou conexão;
2. processo A faz enrollment de teste e persiste somente metadados permitidos;
3. processo A é encerrado depois de o Server aceitar um heartbeat e antes da confirmação local;
4. processo B reabre o store, repete o mesmo envelope e avança uma única vez;
5. transporte falha repetidamente e o relógio controlado comprova backoff, teto, jitter e cancelamento;
6. duas instâncias disputam o mesmo fluxo e apenas uma obtém o lease/fence válido;
7. respostas duplicadas/reordenadas não alteram indevidamente sequência ou LKG;
8. incompatibilidade de versão/schema e certificado expirado param sem retry contínuo;
9. revogação em três pontos coordenados preserva a semântica fail-closed;
10. falha SQLite antes/depois do commit preserva atomicidade e recuperação definida;
11. corrupção e disk-full simulados recusam avanço sem recriação silenciosa;
12. não são criados probes, observations, events, commands, attempts, notifications ou acknowledgments operacionais;
13. nenhum processo, listener, chave, token, certificado, banco ou diretório temporário permanece ao final.

### 9. Testes proporcionais

O futuro incremento deverá incluir:

- testes unitários da classificação de falhas, backoff, jitter, cancelamento, lease/fencing e estados terminais;
- testes de contrato para `N/N-1`, headers/body, limites máximo-mais-um e respostas fora de ordem;
- testes SQLite de concorrência, rollback, schema incompatível, corrupção e fault injection;
- testes de arquitetura que mantenham o harness fora da composição normal;
- E2E multiprocesso local com PIDs e cleanup verificados;
- canários para token, chave, certificado completo e dados privados em argumentos, ambiente, logs e store;
- repetição suficiente para detectar flakiness sem depender de tempo real ou internet.

### 10. Documentação factual esperada

Se futuramente autorizado e implementado, atualizar somente os documentos proprietários necessários:

- protocolo Agent/API, apenas quanto ao comportamento realmente provado;
- modelo lógico e Migration Runbook, somente se o store mudar;
- threat model, caso surja novo controle ou risco;
- README, sem alegar ativação operacional;
- estado atual, histórico append-only e relatório automático do incremento.

Nenhum ADR será promovido de `proposed` para `accepted` por consequência desse trabalho. Uma descoberta que exija decisão arquitetural material deverá ser apresentada separadamente.

## Fora de escopo absoluto

- ativação do cliente Agent Fleet no Worker normal ou execução contínua fora dos testes;
- key store, issuer, token provisioner, PKI, trust distribution, rotação, renewal ou recovery operacionais;
- PostgreSQL real, IdP, vault, proxy corporativo, cloud, internet, provider externo ou credencial operacional;
- provider, probe, monitoramento, observação, evento, alerta, SignalR ou telemetria externa;
- acknowledgment operacional de assignments ou entrega de assignments ao scheduler;
- Start, Stop, Restart de banco, comando administrativo, command polling, attempt, post-probe ou executor;
- UI, Dashboard, WPF, Tray, modo TV, notificação ou canal externo;
- LLM, recomendação, planejamento, MOD-12 runtime ou promoção `none → OBSERVER`;
- deploy, publicação, instalação, serviço, worker novo, API pública nova ou migration remota;
- teste que preencha disco real, corrompa dados fora da fixture ou encerre processos alheios;
- homologação, produção, release, `STATE-07` ou qualquer transição automática.

## Invariantes de segurança

- Toda composição normal permanece desligada e fail-closed.
- Somente loopback e recursos efêmeros criados pela fixture podem participar.
- Token e chave privada nunca entram em SQLite, configuração comum, argumentos, logs, relatórios ou commits.
- Retry é limitado, cancelável e exclusivo para falha transitória.
- Estado terminal não inicia reenrollment, rotação ou recuperação automática.
- Resposta atrasada ou processo sem fence válido não pode confirmar trabalho.
- Falha de persistência não apaga LKG nem cria store novo silenciosamente.
- LKG não equivale a autorização corrente, saúde de instância ou monitoramento ativo.
- Nenhum assignment ativa provider, scheduler, credencial ou ação.
- Cleanup é parte do resultado, não uma atividade opcional posterior.

## Critérios de aceite do futuro incremento

O incremento futuro somente poderá ser aceito se:

- o Worker normal continuar recusando ativação e sem side effects por padrão;
- reinício entre processos independentes comprovar replay e continuidade durável;
- duplicidade, reorder, expiração, incompatibilidade e revogação forem exercitados E2E;
- backoff/jitter/cancelamento forem determinísticos, limitados e sem espera real longa;
- concorrência local permitir um único owner/fence válido;
- fault injection SQLite provar atomicidade e fail-closed sem risco ao computador;
- a corrida assignment/revogação tiver resultado inequívoco ou achado bloqueante explícito;
- nenhum segredo apareça em store, argumentos, ambiente, logs ou evidência;
- nenhum provider, monitoramento, comando, UI, notificação, LLM ou recurso externo seja chamado;
- todos os processos e listeners temporários sejam identificados e encerrados;
- build, testes, arquitetura, cobertura, format, documentação, links, secret scan e diff passem;
- o relatório diferencie observado, inferido, não testado e bloqueado;
- Bruno emita decisão humana específica, sem promoção ou transição automática.

## Quality Gate proposto

| Gate futuro | Classificação exigida |
|---|---|
| Build Release dos projetos afetados e solução completa | `REQUIRED` |
| Testes unitários, arquitetura, persistência e integração | `REQUIRED` |
| E2E multiprocesso HTTPS/mTLS/SQLite em loopback | `REQUIRED` |
| Matriz negativa de retry, reorder, versão, expiração e revogação | `REQUIRED` |
| Concorrência/fencing e fault injection SQLite | `REQUIRED` |
| Cobertura igual ou superior aos pisos vigentes | `REQUIRED` |
| Format/analyzers e documentação de código en-GB/XML | `REQUIRED` |
| Links Markdown, secret scan, diff e integridade Git | `REQUIRED` |
| Runtime smoke fail-closed e cleanup por PID/listener | `REQUIRED` |
| Recursos externos e auditorias online | `NOT AUTHORISED`, salvo nova autoridade específica |
| Human Gate do incremento | `REQUIRED`, separado da saída de `STATE-06` |
| Quality/Human Gate de saída de `STATE-06` | `NOT EVALUATED` por este incremento |

## Riscos e mitigação proposta

| Risco | Impacto | Mitigação exigida |
|---|---|---|
| Harness parecer runtime operacional | ativação indevida | assembly/argumento sandbox inequívoco e ausência da composição normal |
| Teste multiprocesso vazar segredo | exposição local | somente referências opacas, canários e redaction |
| Retry causar loop ou tempestade | consumo e duplicidade | teto de tentativas/tempo, backoff e single-flight |
| Dois processos confirmarem o mesmo trabalho | sequência ou LKG incoerente | lease/fencing transacional e rejeição do owner antigo |
| Simulação de disk-full afetar o host | perda de dados do usuário | fault injection/VFS; nunca preencher disco real |
| Corrupção ser “corrigida” apagando evidência | perda silenciosa | recusar abertura e preservar artefato sanitizado |
| Revogação competir com resposta antiga | configuração parecer autorizada | semântica coordenada e LKG explicitamente histórica |
| Compatibilidade `N-1` ser presumida | comportamento inseguro | permitir apenas matriz declarada; desconhecido falha fechado |
| Testes baseados em tempo ficarem instáveis | falsa evidência | relógio e delays determinísticos |
| Escopo crescer para identidade operacional | salto de risco | proibição absoluta e proposta posterior independente |

## Limitações que permanecerão mesmo se o futuro incremento passar

Este incremento não provará:

- segurança ou disponibilidade de uma PKI/key store/token service operacional;
- rotação, renewal, recuperação ou distribuição de trust material;
- rede corporativa, proxy, IdP, vault, PostgreSQL real ou instalação como serviço;
- classificação não secreta de todos os futuros schemas de provider;
- ativação de assignment, provider, monitoramento, telemetria ou comando;
- carga de fleet, múltiplas máquinas, segurança ofensiva ou operação prolongada;
- readiness para `OBSERVER`, `STATE-07`, produção ou release.

Esses itens continuarão exigindo propostas, autorizações, evidências e gates independentes.

## Entregáveis documentais deste pedido

Este pedido produz somente:

- esta proposta delimitada;
- atualização factual do estado corrente para indicar que existe uma proposta pendente de decisão;
- entrada factual append-only no histórico.

Não há relatório de implementação, resultado de build/teste, migration, mudança de código ou Human Gate neste pedido.

## Decisão solicitada a Bruno

Bruno pode:

1. aceitar a proposta e autorizar separadamente a implementação restrita;
2. pedir alterações documentais específicas;
3. adiar ou rejeitar a proposta.

Aceitar o desenho não implementa nada. Para permitir a execução futura exatamente como proposta, a autorização deverá mencionar o sandbox multiprocesso local e continuar proibindo ativação operacional. Uma redação segura é:

> AUTORIZO o incremento restrito de STATE-06 — Agent Fleet Sandbox Resilience and Protocol Compatibility, limitado a harness e runtimes temporários exclusivamente locais, reinício E2E entre processos, retry/backoff/cancelamento determinísticos, compatibilidade e falhas de protocolo, concorrência/fencing local, fault injection SQLite e corrida assignment/revogação, mantendo o Worker normal desabilitado e encerrando todos os processos ao final. Permanecem proibidos recursos operacionais ou externos, PKI/key store/token service operacionais, providers, monitoramento, comandos, UI, notificações, LLM, executor, deploy, promoção e transição de estado.

Essa eventual autorização permitiria somente o incremento descrito. A conclusão técnica ainda dependeria de Quality Gate, revisão direta e decisão humana próprias.
