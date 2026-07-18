# Proposta STATE-06 — Identidade Agent-side e Reconciliação Read-only de Assignments

## Status e autoridade

- Data: 2026-07-18
- Posição do ciclo de vida: `STATE-06 INTEGRATION`
- Natureza deste artefato: proposta exclusivamente documental e não executiva
- Origem: solicitação de Bruno por uma proposta para o próximo incremento restrito de `STATE-06`, sem implementação
- Baseline aceita: implementação server-side `cc2d828`, remediação documental `c5e3cbd` e registro de aceitação `27673d8`
- Estado operacional: nenhum Agent Fleet está ativado; monitoramento, command polling e retenção permanecem desabilitados por padrão

Este documento não autoriza o incremento que descreve. Ele não altera código, configuração, migration, runtime, ADR, modo MOD-12 ou posição do ciclo de vida. Seu único objetivo é permitir uma decisão posterior, separada e informada.

> Nota factual posterior: Bruno usou a redação delimitada ao final deste documento para autorizar separadamente a implementação em 2026-07-18. O incremento foi implementado, passou seu Quality Gate automático restrito e foi aceito humanamente com as limitações registradas no commit `beb936b`. O relatório proprietário é [`STATE-06 Agent-side Identity and Assignment Reconciliation Report`](STATE-06-Agent-Side-Identity-And-Assignment-Reconciliation-Report.md). Esta nota não reescreve a natureza não executiva da proposta original nem autoriza trabalho adicional.

## Resumo para não especialistas

O servidor já possui uma porta local de testes pela qual um Agent pode receber uma identidade, informar que continua ativo e consultar quais instâncias lhe foram atribuídas. Entretanto, o programa Agent ainda não usa essa porta: hoje ele depende de um identificador e de um certificado configurados manualmente, não envia o novo heartbeat e não guarda os assignments recebidos do servidor.

O próximo incremento recomendado conectaria essas duas metades somente em um laboratório local. Um Agent de teste criaria sua chave temporária, faria enrollment com um token fornecido apenas pela fixture, enviaria heartbeat e guardaria uma cópia read-only dos assignments em SQLite. “Read-only” significa que o Agent apenas lê a configuração autorizada do servidor: ele não conecta a bancos monitorados, não executa providers e não realiza comandos.

A identidade de produção continuaria indisponível. O fluxo normal permaneceria desligado por padrão e falharia de forma fechada se alguém tentasse ativá-lo sem os componentes seguros que ainda não existem.

## Nome recomendado do próximo incremento

`STATE-06 — Agent-side Test Identity and Read-only Assignment Reconciliation`

Em linguagem simples: completar, somente no sandbox, o lado Agent da matrícula, da comprovação de atividade e da leitura segura de configuração.

## Por que este é o próximo passo

A fronteira server-side aceita já prova enrollment, heartbeat, assignments, catálogo e revogação. O elo seguinte de menor risco é demonstrar que o Agent consegue consumir apenas as três operações destinadas a ele, preservando estado local e comportamento offline sem ativar monitoramento.

Isso reduz quatro lacunas factuais atuais:

1. `AgentId` e thumbprint ainda são fornecidos manualmente ao Worker;
2. não existe coordenador Agent-side de enrollment;
3. não existe entrega Agent-side de heartbeat com replay durável;
4. o snapshot de assignments não é validado nem aplicado ao SQLite local.

PKI e token provisioning operacionais, rotação, provider runtime, telemetria e comandos são problemas posteriores e não precisam ser misturados neste incremento.

## Objetivo do incremento futuro

Implementar e testar localmente uma fronteira Agent-side opt-in que:

- obtenha uma identidade exclusivamente de teste por enrollment v1;
- mantenha a chave privada fora do SQLite, configuração, logs e protocolo;
- persista apenas referência e metadados não secretos da identidade;
- envie heartbeat v1 com sequência e replay duráveis;
- consulte snapshots completos de assignments v1;
- aplique cada snapshot válido atomicamente ao SQLite local;
- preserve o último snapshot válido durante falha, desconexão ou resposta inválida;
- pare de sincronizar de forma fechada após revogação, expiração ou incompatibilidade;
- permaneça incapaz de executar provider, probe, comando, notificação ou ação externa.

## Baseline factual que não deve ser reinterpretada

- O Server possui endpoints v1 de enrollment, heartbeat e assignments, mas o issuer produtivo retorna indisponível.
- O E2E existente chama esses endpoints diretamente; nenhum `DBNotifier.Agent.Worker` participa.
- O Worker possui sincronização de observações opt-in por certificado localizado por thumbprint, mas ela não equivale a enrollment ou Agent Fleet reconciliation.
- O SQLite do Agent já possui registration, assignments e checkpoints, porém não possui um protocolo completo para heartbeat pendente/replay nem aplicação remota de snapshot.
- `MonitoringEnabled`, sincronização, command polling e retenção são flags distintas e continuam `false` por padrão.
- A aplicação local de assignments é persistência de configuração autorizada; não é prova de monitoramento, provider ou suporte a qualquer engine.

## Escopo proposto para implementação posterior

### 1. Fronteiras Agent-side explícitas

Adicionar portas coesas para:

- material de bootstrap de enrollment;
- geração de chave P-256 e CSR;
- armazenamento/reabertura da identidade do Agent;
- transporte v1 de enrollment, heartbeat e assignments;
- persistência da máquina de estados de identidade, heartbeat e reconciliação;
- relógio, atraso e jitter controláveis em testes.

As portas pertencem às camadas internas adequadas e os adaptadores permanecem no Agent/Infrastructure. Domain não recebe HTTP, certificado, SQLite ou regra de provider.

### 2. Enrollment exclusivamente de teste

O coordenador futuro deverá:

- aceitar o token somente em memória a partir da fixture E2E;
- gerar uma chave ECDSA NIST P-256 e CSR localmente;
- enviar somente CSR público e metadados não secretos;
- validar Agent ID, certificado público, curva, uso, validade e vínculo da chave retornada;
- gravar em SQLite apenas Agent ID, installation ID, ambiente, referência da identidade, thumbprint/digest público e tempos necessários;
- apagar a referência ao token imediatamente após a tentativa terminal;
- recusar replay, resposta ambígua, certificado divergente ou identidade local conflitante;
- nunca registrar token, CSR completo, certificado completo ou material privado.

O adaptador de teste para a chave privada será efêmero e existirá somente no projeto E2E. A composição normal continuará sem provisionador, token source e key-store operacional; portanto, enrollment produtivo continuará indisponível.

### 3. Compatibilidade com a configuração manual existente

O incremento não deve migrar silenciosamente a configuração atual por thumbprint. Deve existir uma regra inequívoca:

- identidade local e `AgentId` configurado precisam coincidir exatamente; ou
- o startup recusa a ativação com código sanitizado de conflito.

Não haverá adoção automática de certificado preexistente, troca automática de identidade, fallback para token permanente nem substituição de uma registration válida.

### 4. Heartbeat com entrega durável

Antes de enviar, o Agent deverá persistir um envelope heartbeat pendente com `messageId`, sequência, instantes e digest canônico. Se a resposta for perdida, a repetição usa exatamente o mesmo envelope; um novo heartbeat só recebe a sequência seguinte depois de um resultado compatível.

O fluxo deverá:

- usar `TimeProvider` e sequência monotônica local;
- derivar queue depth/oldest item apenas do store local autorizado, sem inventar saúde de instância;
- aceitar `Accepted`, `Duplicate` e `AcceptedWithGap` somente quando a resposta estiver vinculada ao mesmo Agent/envelope;
- tratar conflito, versão incompatível, expiração e revogação como falhas não retryable;
- limitar timeout, cadência, backoff, jitter e uma única requisição heartbeat em voo;
- observar cancelamento sem perder o envelope pendente.

Heartbeat comprova somente que o processo Agent conseguiu comunicar-se. Ele nunca transforma o status de uma instância em saudável.

### 5. Reconciliação read-only de assignments

O Agent deverá consultar o snapshot completo usando a versão local/ETag e validar antes de qualquer commit:

- schema/protocolo suportados;
- Agent ID e ambiente exatos;
- body `Version`, ETag e digest do snapshot coerentes;
- ordenação, unicidade, cardinalidade e bytes dentro dos limites;
- tipos, intervalos, timeout, retries, tags e endpoint JSON dentro do contrato;
- presença apenas de referência opaca de credencial de monitoramento;
- ausência de qualquer referência administrativa, segredo ou campo desconhecido proibido.

Um snapshot válido substituirá atomicamente o conjunto local e atualizará `ActiveConfigurationVersion` na mesma transação. Um `304` preservará a versão atual. Timeout, desconexão, corpo parcial, limite excedido, digest divergente ou incompatibilidade preservarão o último snapshot válido e registrarão apenas estado/código sanitizado.

Assignments removidos do snapshot deixam de estar ativos localmente, sem apagar observações históricas. Nenhum assignment será entregue ao scheduler de monitoramento neste incremento porque `MonitoringEnabled` continuará desligado e o E2E provará ausência de probes.

### 6. Persistência Agent SQLite

Uma única migration Agent SQLite, revisada e reversível no sandbox, poderá acrescentar somente o estado mínimo necessário para:

- referência e estado da identidade local;
- envelope heartbeat pendente e último recibo confirmado;
- versão, ETag, freshness e último resultado da reconciliação;
- tags e metadados necessários para preservar o snapshot completo;
- concorrência otimista e timestamps UTC.

Chave privada, enrollment token, certificado completo, segredo, connection string e valor resolvido de credencial permanecem proibidos no SQLite. O `Down` não poderá apagar uma identidade ou snapshot ativo sem guard explícito ou procedimento de desativação documentado.

### 7. Estados de falha e offline

O desenho futuro deverá distinguir ao menos:

| Estado | Significado | Comportamento permitido |
|---|---|---|
| `NotEnrolled` | nenhuma identidade local válida | enrollment somente quando a fixture fornece bootstrap de teste |
| `Active` | identidade válida e contrato compatível | heartbeat e leitura read-only de assignments |
| `Offline` | falha transitória de transporte | backoff limitado e retenção do último snapshot válido |
| `Stale` | snapshot conhecido, mas freshness excedida | preservar dados e marcá-los como vencidos; nunca tratá-los como atuais |
| `Incompatible` | protocolo/schema não suportado | parar sem retry infinito e exigir atualização autorizada |
| `Expired` | certificado fora da validade | parar; não auto-renovar nem auto-enroll |
| `RevokedOrDenied` | identidade recusada pelo servidor | quarentena local, sem novas operações Agent Fleet |
| `Conflict` | identidade/configuração/store divergentes | fail-closed e intervenção futura explicitamente autorizada |

Falha de TLS genérica não será apresentada como revogação comprovada. Quando a causa exata não puder ser determinada, o resultado será `IdentityUnavailable` ou equivalente factual.

### 8. E2E sandbox proposto

O cenário futuro deverá usar somente recursos locais e descartáveis:

- Kestrel HTTPS em loopback/porta efêmera;
- SQLite Server e Agent separados e temporários;
- CA, servidor, chave Agent, CSR, certificado e token gerados em memória pela fixture;
- autenticação humana de teste apenas para preparar/revogar o Agent;
- relógio controlado e delays substituídos por coordenadores executados uma vez;
- nenhuma conexão a banco monitorado, internet, IdP, vault, provider ou serviço externo.

O E2E deverá provar, na ordem:

1. startup default não cria store nem faz rede;
2. enrollment de teste cria registration sem persistir token/chave;
3. perda simulada de resposta repete o mesmo heartbeat e não avança indevidamente a sequência;
4. novo heartbeat avança de forma monotônica;
5. snapshot v1 é validado e aplicado atomicamente;
6. `304` preserva a mesma configuração;
7. snapshot v2 remove/adiciona assignments como conjunto completo;
8. snapshot inválido ou truncado preserva o último conhecido válido;
9. reinício lógico do coordenador recupera estado SQLite sem duplicar enrollment ou heartbeat;
10. revogação impede heartbeat/assignments posteriores e não dispara novo enrollment;
11. incompatibilidade v2/v1 falha fechada;
12. nenhum probe, health sample, observation outbox, evento, comando, attempt ou notificação é criado;
13. todos os runtimes, stores e materiais da fixture são encerrados/descartados ao final.

Esse teste não provará key store operacional, restart real entre processos, PostgreSQL, rede corporativa, rotação de certificado ou execução de provider.

### 9. Testes proporcionais

O incremento futuro deverá incluir:

- testes unitários das máquinas de estado, validação, digest, backoff e cancelamento;
- testes negativos de conflito de identidade, certificado divergente, token ausente, segredo em payload e versão incompatível;
- testes SQLite de migration, transação atômica, rollback, concorrência e último snapshot válido;
- testes de arquitetura para preservar dependências e impedir referência do Agent a Server PostgreSQL;
- E2E único ou pequeno conjunto E2E com fluxo completo e cleanup comprovado;
- canários de segredo em configuração, logs, exceções e persistência;
- cobertura dos limites máximo-mais-um e de corpo parcial.

### 10. Documentação factual esperada

Se implementado, o incremento deverá atualizar apenas a documentação proprietária necessária:

- protocolo Agent/API, sem mudar o significado dos endpoints humanos;
- modelo lógico e Migration Runbook Agent SQLite;
- threat model caso a implementação revele novo controle;
- README somente com comportamento realmente disponível;
- estado atual, histórico append-only e relatório automático do incremento.

ADRs aceitos serão corrigidos apenas quando a implementação alterar sua seção factual, sem reabrir ou ampliar a decisão arquitetural.

## Escopo resumido

| Área | Dentro do futuro incremento | Fora do escopo |
|---|---|---|
| Identidade | coordinator e key store exclusivamente de teste | issuer, token service, PKI ou vault operacional |
| Agent/API | enrollment, heartbeat e assignments v1 | observations/events, SignalR e comandos |
| Persistência | SQLite Agent local e migration sandbox | PostgreSQL real ou banco externo |
| Assignments | validação, ETag, aplicação atômica e LKG | provider, probe, credencial resolvida ou scheduler ativo |
| Runtime | coordenadores locais temporários somente no E2E | serviço instalado, deploy ou execução contínua |
| Segurança | fail-closed, revogação, expiração e canários | CRL/OCSP, rotação, IdP ou penetration test |
| Ciclo de vida | `STATE-06` preservado | `OBSERVER`, `STATE-07` ou release |

## Fora de escopo absoluto

- provider operacional, probe, monitoramento, coleta ou telemetria externa;
- Start, Stop, Restart, comando administrativo, command polling ou executor;
- token provisioner, issuer, CA, certificado, chave ou credencial operacional;
- chave privada em SQLite, configuração, arquivo comum, log, relatório ou commit;
- IdP, vault, PostgreSQL, database target, cloud, internet ou canal externo;
- UI, Dashboard, WPF, Tray, SignalR, notificações ou modo TV;
- LLM, recomendação, planejamento, MOD-12 runtime ou promoção para `OBSERVER`;
- deploy, publicação, instalação, serviço, worker permanente ou API nova;
- alteração automática de estado ou entrada em `STATE-07`.

## Invariantes de segurança

- Todo recurso novo permanece opt-in e `false` por padrão.
- Ausência de bootstrap, identidade, trust material, versão ou store válido recusa startup da função específica.
- Token e chave privada nunca entram em persistência comum ou diagnóstico.
- Identidade local não é substituída automaticamente.
- Revogação/expiração interrompe o fluxo Agent Fleet e não inicia recovery automático.
- Resposta parcial ou incompatível nunca substitui o último snapshot válido.
- LKG preservado não é apresentado como atual; freshness e erro permanecem visíveis.
- Assignments não concedem autorização de comando e não contêm credencial administrativa.
- Nenhum dado recebido ativa provider, monitoring worker ou outbox.
- Retry ocorre somente para falha transitória e sempre com limites/cancelamento.

## Critérios de aceite do futuro incremento

O incremento somente poderá ser aceito se:

- os defaults permanecerem totalmente inativos e sem side effect;
- o Agent participar do E2E usando os endpoints reais já implementados;
- enrollment e private-key handling forem exclusivamente de teste e descartáveis;
- nenhum token, chave ou certificado completo aparecer em SQLite, config, log ou evidência;
- heartbeat sobreviver a resposta perdida por replay exato e sequência durável;
- assignments forem validados e aplicados como snapshot completo e atômico;
- falha preservar o último snapshot válido com stale/erro factual;
- revogação, expiração e incompatibilidade pararem o fluxo sem retry infinito;
- conflito entre identidade local e configuração manual falhar fechado;
- nenhuma operação de provider, monitoramento, comando, evento ou notificação ocorrer;
- migration Agent SQLite clean/up/down e falhas forem exercitadas somente em sandbox;
- build, testes, arquitetura, cobertura, format, documentação, links, secret scan e diff passarem;
- todos os processos/listeners temporários forem encerrados;
- o relatório separar evidência observada, limitação e trabalho futuro;
- Bruno emitir decisão humana específica sobre o resultado, sem promoção automática.

## Quality Gate proposto

- build Release dos projetos afetados e solução completa: `REQUIRED`;
- testes unitários, arquitetura e integração: `REQUIRED`;
- migration Agent SQLite em store efêmero: `REQUIRED`;
- E2E local Agent → API com HTTPS/mTLS: `REQUIRED`;
- testes negativos de segredo, identidade, versão, revogação e atomicidade: `REQUIRED`;
- cobertura igual ou superior aos pisos vigentes: `REQUIRED`;
- .NET format/analyzers e documentação en-GB/XML: `REQUIRED`;
- links Markdown, secret scan e inspeção de diff: `REQUIRED`;
- runtime smoke fail-closed com cleanup: `REQUIRED`;
- auditorias online: somente se houver autorização externa específica; ausência deve ser declarada;
- Human Gate deste incremento: `REQUIRED` e separado do gate de saída de `STATE-06`.

## Riscos e mitigação proposta

| Risco | Impacto | Mitigação exigida |
|---|---|---|
| Test adapter parecer key store produtivo | falsa sensação de segurança | nome/test assembly explícitos e produção indisponível |
| Token vazar em config/log | comprometimento do bootstrap | injeção apenas em memória, canário e redaction |
| Resposta perdida duplicar heartbeat | cursor inconsistente | envelope durável e replay exato |
| Snapshot parcial apagar configuração válida | perda ou configuração insegura | validação completa e uma transação atômica |
| LKG vencido parecer atual | estado operacional falso | freshness/stale separado e timestamp preservado |
| Assignment ativar monitoring | conexão indevida a provider | flags separadas e asserção E2E de zero probes |
| Revogação causar auto-enrollment | bypass de decisão humana | quarentena sem recuperação automática |
| Certificado/config manual divergente | impersonation ou cross-scope | binding exato e startup fail-closed |
| Retry storm durante outage | consumo excessivo | uma requisição em voo, backoff/jitter e teto |
| Migration apagar identidade ativa | perda de continuidade | guard de `Down`, backup de sandbox e teste explícito |

## Sequência recomendada se houver autorização posterior

1. Confirmar o escopo e executar o shutdown preflight.
2. Implementar portas/máquinas de estado sem habilitar runtime.
3. Implementar stores SQLite e migration local.
4. Implementar transports v1 e coordenadores executados uma vez.
5. Criar o E2E sandbox com material efêmero de teste.
6. Exercitar revogação, resposta perdida, LKG, incompatibilidade e cleanup.
7. Executar todos os Quality Gates aplicáveis.
8. Fazer revisão direta do diff e corrigir achados dentro do escopo.
9. Atualizar documentação factual e criar commit isolado.
10. Apresentar relatório automático para uma única decisão humana.

## Condição de autorização

Esta proposta termina na documentação. Nenhuma etapa acima pode começar sem uma autorização posterior que cite este incremento e preserve expressamente as exclusões.

Uma autorização inequívoca poderá usar a seguinte redação:

> AUTORIZO o incremento restrito de STATE-06 — Agent-side Test Identity and Read-only Assignment Reconciliation, limitado a enrollment exclusivamente de teste, identidade privada somente em adapter E2E efêmero, heartbeat durável, reconciliação read-only de assignments, persistência/migration Agent SQLite e testes locais sandbox, mantendo todos os runtimes desabilitados por padrão e encerrados após os testes. Permanecem proibidos recursos operacionais ou externos, providers, monitoramento, comandos, UI, notificações, LLM, executor, promoção e transição de estado.

Bruno também poderá pedir ajustes ou adiar/rejeitar a proposta. Nenhuma resposta curta será interpretada como autorização de implementação.

## Resultado posterior da autorização separada

- Implementação: concluída dentro do sandbox local delimitado.
- Runtime normal: permanece desabilitado e recusa ativação porque não existe identity adapter operacional.
- Evidência automática: build Release sem avisos, `283/283` testes unit/model/provider/presentation, `15/15` arquitetura, `2/2` E2E, coverage acima dos pisos, migration SQLite exercitada e smoke fail-closed aprovado.
- Revisão direta: problemas encontrados em digest verificável, canonicalização JSON, classificação da recusa de revogação, guard de rollback e limites foram corrigidos antes da entrega.
- Limitações: key store/issuer/token provisioning, scheduler/retry operacional, restart entre processos, concorrência/crash de SQLite, PostgreSQL e qualquer provider ou integração externa permanecem não implementados.
- Decisão humana: aceita com as limitações registradas no commit `beb936b`; nenhum estado ou modo foi promovido e nenhum novo incremento foi autorizado.
