# STATE-06 — Plano de Remediação da Auditoria Completa do Projeto

## Estado, autoridade e limites

- Data: 2026-07-20.
- Posição do workspace: `STATE-06 INTEGRATION`.
- Baseline auditada: commit `1179ec88dea1df3f7a1806786f92dca15c29a0e1`.
- Autoridade atual: Bruno declarou exatamente `AUTORIZO APENAS A ELABORAÇÃO DO PLANO DE REMEDIAÇÃO DA AUDITORIA, SEM IMPLEMENTAÇÃO.`
- Disposição deste documento: plano elaborado; aceitação como direção ainda pendente.
- Estado do MOD-12: nenhum modo ativo; `none → OBSERVER` permanece pendente.
- Estado do `ADR-0007`: aceito como decisão arquitetural, sem autorização de implementação.

Esta autoridade permite somente criar, revisar e validar este plano documental e registrar factualmente a sua existência. Não autoriza alterar código, configuração executável, contratos, migrations, packages, lockfiles, runtime, interfaces ou dados; implementar qualquer lote; acessar banco, provider, vault, IdP, PKI, canal ou serviço real; usar LLM; produzir recomendações operacionais; criar plano executável; executar comando; ativar `OBSERVER`; homologar PostgreSQL ou outro provider; avançar o ciclo de vida; publicar, instalar ou acessar recurso externo.

Nenhuma frase deste documento constitui autorização implícita. Cada lote depende de uma decisão posterior, explícita e limitada. Aceitar o plano como direção também não autoriza implementação.

## Objetivo

Organizar os achados da auditoria completa de 2026-07-20 numa sequência segura, verificável e reversível que:

1. restaure primeiro a confiabilidade dos próprios gates e runners;
2. corrija defeitos atuais antes de ampliar funcionalidades;
3. mantenha indisponíveis as capacidades latentes cujos contratos ainda são insuficientes;
4. separe correção, contenção, implementação futura e homologação;
5. preserve provider neutrality, least privilege, fail-closed e ausência de execução automática;
6. não confunda encerramento da auditoria com AIOps operacional, promoção de modo, homologação ou transição de estado.

## Baseline factual e evidência disponível

A auditoria inventariou `552` arquivos versionados, aproximadamente `80 486` linhas de código, testes e scripts, `17` projetos .NET 10 e as superfícies React, WPF, Tray, Agent, API, providers, persistências, MOD-12, CI, packaging e legado.

| Evidência executada sobre a baseline | Resultado observado |
|---|---|
| Shutdown preflight inicial e final | zero processo, listener ou navegador dedicado pertencente ao DB-Notifier |
| Build Release da solução | aprovado; zero warnings e zero erros |
| Testes .NET | `382/382` aprovados: `332` unitários, `31` arquitetura e `19` integração |
| Cobertura .NET | `78,90%` linhas e `49,51%` branches; gate vigente aprovado |
| Dashboard | typecheck, verificadores, `60/60` testes e build Vite aprovados |
| Formatação, documentação e links | aprovados no escopo executado |
| Secret scan e fail-closed runtime | aprovados no worktree/histórico disponível e composição local restrita |
| `npm audit --audit-level=high` | zero entradas reportadas no cache/fonte disponível |
| Gate NuGet do repositório | reprovado por incompatibilidade do próprio gate com o relatório vazio real do SDK |
| `dotnet restore --locked-mode` | retornou sucesso, mas criou o lockfile ausente do host consolidado |
| STATE-06 consolidado | primeira execução falhou em `429/503`; repetição concluiu o fluxo funcional, mas o runner falhou no cleanup |
| STATE-05 browser audit | resultado funcional inconclusivo porque o cleanup falhou ao remover cache ainda aberto |

Esses resultados não provam CI remota, Windows notification visível, PostgreSQL real, migrations reais, PKI/IdP/vault, provider homologado, carga, endurance, disaster recovery, packaging distribuível ou AIOps operacional.

## Regras de classificação e encerramento

Cada achado recebe uma destas disposições:

- `CORRIGIR`: comportamento atual incorreto deve ser alterado.
- `CONTER`: capacidade incompleta deve permanecer inacessível e falhar fechado.
- `DECIDIR`: há uma escolha arquitetural ou de segurança que precisa ser aceita antes do código.
- `ADIAR COM GATE`: implementação não é necessária para a baseline atual, mas é pré-condição obrigatória antes de ativar a capacidade relacionada.
- `ACEITAR COMO RESIDUAL`: somente quando o risco estiver delimitado, não produzir alegação falsa e houver decisão humana informada.

Um achado não é encerrado apenas porque um teste passa. O encerramento exige correção ou contenção comprovada, regressão automatizada, análise de segurança, rollback ou kill switch aplicável, documentação factual e relatório próprio. Limitação ambiental permanece `NÃO TESTADA` ou `BLOQUEADA`; nunca é convertida em aprovação.

## Matriz de rastreabilidade da auditoria

### Achados altos

| ID | Achado | Situação | Disposição proposta | Lote proprietário |
|---|---|---|---|---|
| `AUD-H01` | Gate NuGet rejeita o relatório vazio real embora o próprio script reconheça que o SDK omite `frameworks` sem findings | atual; bloqueia evidência/CI | `CORRIGIR` | `R0` |
| `AUD-H02` | Fallback local retorna `Accepted` ao apenas enfileirar em memória; crash pode perder notificação já deduplicada | atual no sandbox reconciliado | `CORRIGIR` | `R1` |
| `AUD-H03` | Contrato administrativo usa permissão genérica, confirmação insuficiente, parâmetros amplos e snapshot de autorização mutável | superfície normal sem executor | `CONTER` agora; `ADIAR COM GATE` para protocolo completo | `R2` |
| `AUD-H04` | Poll/ack v1 pode oferecer comando após reassignment/disable/archive e não tem anti-replay/auditoria duráveis completos | superfície normal; Worker bloqueado | `CONTER` | `R2` |
| `AUD-H05` | Endpoint e credential reference podem cruzar Server→Agent e ser persistidos antes da validação non-secret canônica | caminho Fleet ainda inativo | `CORRIGIR` antes de ativação | `R3` |
| `AUD-H06` | Uma regra correspondente cria pendências para todos os canais habilitados | pendências podem ser semanticamente erradas; envio bloqueado | `CORRIGIR` | `R4` |
| `AUD-H07` | Outbox/notificações externas não possuem claim, lease e fence duráveis | delivery normal recusado | `ADIAR COM GATE`; preservar contenção | `R4` |
| `AUD-H08` | Mais de `128` certificados fazem rollback da revogação inteira do Agent | endpoint normal | `CORRIGIR` | `R3` |
| `AUD-H09` | ConfigMigrator substitui o target antes de existir journal/report recuperável e não autentica todo backup | utilitário normal | `CORRIGIR` | `R5` |
| `AUD-H10` | Vault Linux resolve `secret-tool` por `PATH` e lê output sem limite | adapter normal Linux | `CORRIGIR` ou manter `Unavailable` | `R5` |
| `AUD-H11` | Server não compara o SPKI do certificado emitido com a chave pública do CSR | issuer normal indisponível | `CORRIGIR` antes de issuer operacional | `R3` |
| `AUD-H12` | `Enabled=false` é transportado, mas ignorado nos agregados e conclusões visuais | apresentação atual/demonstração | `CORRIGIR` | `R6` |
| `AUD-H13` | Fundação MOD-12 não implementa trust host/checkpoint do ADR e pode publicar conclusão após deadline não revalidado | inativa; risco pré-promoção | `CORRIGIR` na fundação e `ADIAR COM GATE` para O1 | `R7` |

### Achados médios e baixos

| ID | Achado agrupado | Disposição proposta | Lote proprietário |
|---|---|---|---|
| `AUD-M01` | Host consolidado é o único projeto sem `packages.lock.json`; locked restore cria um novo | `CORRIGIR` | `R0` |
| `AUD-M02` | Runners STATE-05/06 não aguardam quiescência do browser e deixam processo/profile/cache | `CORRIGIR` | `R0` |
| `AUD-M03` | Controle consolidado compartilha rate limit humano por IP e mostrou falha intermitente `429/503` | `CORRIGIR` sem relaxar limite de produção | `R0` |
| `AUD-M04` | E2E STATE-06 não está no CI; chamadas CDP/Node não têm deadline global; cleanup não é seguro em concorrência | `CORRIGIR` | `R0` |
| `AUD-M05` | Coverage é global/unit-only e gates de arquitetura dependem parcialmente de busca textual | `CORRIGIR` proporcionalmente | `R0` |
| `AUD-M06` | Server possui somente liveness, sem readiness de banco/configuração/schema | `CORRIGIR` | `R4` |
| `AUD-M07` | Respostas HTTP não têm teto real de bytes/profundidade em todos os transportes | `CORRIGIR` | `R3` |
| `AUD-M08` | Deadline de monitoramento começa depois de assignments; timestamp futuro e cancelamento de `pg_isready` são frágeis | `CORRIGIR` | `R4` |
| `AUD-M09` | Verificação de package devolve path mutável, aceita árvore extra e não tem teto agregado | `ADIAR COM GATE` antes de loader dinâmico | `R5` |
| `AUD-M10` | Sandbox de comando perde durablemente `ExecutionPolicy.Never` | `CORRIGIR` antes de qualquer executor | `R2` |
| `AUD-M11` | MOD-12 expõe agregados parciais, proveniência declarativa, status de probe perdido, input não cooperativo e `Mode=OBSERVER` sem ativação | `CORRIGIR` antes de O1/promoção | `R7` |
| `AUD-M12` | Dashboard TV pode classificar snapshot recém-aceito como futuro/Unknown por relógio não atómico | `CORRIGIR` | `R6` |
| `AUD-M13` | Texto TV aceita controls, bidi e whitespace periférico | `CORRIGIR` | `R6` |
| `AUD-M14` | Design System contradiz código/instruções sobre hints SignalR | `CORRIGIR` na fonte normativa | `R6` |
| `AUD-M15` | Marca do flyout não acompanha agregado enquanto a janela permanece aberta | `CORRIGIR` | `R6` |
| `AUD-M16` | Reduced motion WPF não possui adapter comprovado | `CORRIGIR` e validar humanamente | `R6` |
| `AUD-M17` | Timestamps WPF/Tray não rotulam fuso consistentemente | `CORRIGIR` | `R6` |
| `AUD-M18` | Sparklines demonstrativas aparecem junto de snapshot autoritativo sem verdade de origem local | `CORRIGIR` | `R6` |
| `AUD-M19` | Fallbacks de enum desconhecido podem minimizar gravidade ou expor valor machine | `CORRIGIR` fail-closed para `Unknown` | `R6` |
| `AUD-M20` | Navegação WPF usa glyphs de texto proibidos pelo Design System | `CORRIGIR` | `R6` |
| `AUD-M21` | Flyout possui risco mixed-DPI/200% e falta de reflow/scroll comprovados | `CORRIGIR` ou manter limitação explícita até evidência | `R6` |
| `AUD-M22` | Actions de CI usam tags mutáveis | `DECIDIR` e fixar SHA somente com proveniência verificada | `R5` |
| `AUD-M23` | Packaging verifica apenas parte das dependências executáveis futuras | `ADIAR COM GATE`; toolchain continua bloqueada | `R5` |
| `AUD-M24` | Configuração distribuída retém campo legado e monitor legado ignora `notifications.enabled`/duração de `RESTARTED` | `CORRIGIR` preservando compatibilidade | `R5` |
| `AUD-L01` | Título React não acompanha rota; IDs longos truncam; registries de provider são duplicados manualmente | `CORRIGIR` quando R6 tocar as superfícies | `R6` |
| `AUD-L02` | Pester é sensível ao host e gates de comentários/anchors têm cobertura limitada | `CORRIGIR` sem declarar prova além do gate | `R0` |

## Ordem dos lotes

```text
R0  Integridade dos gates e runners
 |
 +--> R1  Verdade e durabilidade da notificação local
 +--> R2  Contenção da superfície de comandos
 +--> R3  Agent Fleet, identidade e ingresso non-secret
 +--> R4  Roteamento, durabilidade e readiness do Server
 +--> R5  Migração, vault, packages, supply chain e legado
 +--> R6  Verdade visual, acessibilidade e contratos frontend
          |
          +--> R7-A0  Correções da fundação inativa do MOD-12
                       |
                       +--> O1 futuro, separado e não autorizado

R0 + todos os lotes que vierem a ser autorizados/concluídos
  --> R8  Reauditoria consolidada e decisão de encerramento da remediação
```

`R0` é pré-condição para confiar na evidência dos demais lotes. Depois de `R0`, lotes sem sobreposição material podem ser planejados em paralelo, mas cada autorização, diff, relatório e aceitação permanecem independentes. A ordem recomendada para reduzir conflito em Application/API/persistência é `R0 → R1 → R2 → R3 → R4 → R5 → R6 → R7-A0 → R8`.

`R2` fecha a auditoria por contenção; construir um protocolo administrativo completo não é requisito para a baseline e continua sendo um programa futuro. `R7-A0` corrige apenas a fundação inativa. O1 continua sendo o incremento definido na proposta AIOps aceita como direção e não é liberado por esta sequência.

## R0 — Integridade dos gates, dependências e runners

### Objetivo

Tornar a evidência automática confiável antes de usar qualquer gate para aprovar remediações posteriores.

### Escopo candidato futuro

- corrigir o verificador NuGet para distinguir relatório vazio válido de cobertura estrutural incompleta;
- obter o inventário de projetos/TFMs de uma fonte independente e comparar o conjunto completo sem exigir uma propriedade que o SDK omite legitimamente;
- cobrir relatórios vazio, vulnerável, transitivo, malformado, fonte divergente, projeto omitido e schema futuro incompatível;
- versionar o lockfile do host consolidado e provar que um clean checkout não cria nem modifica lockfiles em `--locked-mode`;
- encerrar browser por árvore e perfil exclusivo, aguardar quiescência bounded e só então apagar o profile;
- nunca encerrar browser/perfil comum do usuário;
- isolar controles internos do harness de tráfego de Dashboard sem retirar rate limiting da composição normal;
- adicionar deadline global a Node/CDP/host e cleanup idempotente correlacionado ao `runId` exato;
- impedir que uma execução remova roots de outra execução concorrente;
- incluir o E2E consolidado proporcional no CI Windows com timeout e artefato sanitizado em falha;
- fortalecer coverage por componente/código alterado e provas de arquitetura por assembly/DI/route inventory onde necessário;
- documentar o host PowerShell/Pester canônico e falhar cedo em host incompatível;
- decidir fixação de actions por SHA somente depois de verificar a proveniência oficial, sem inventar hash offline.

### Critérios de conclusão

1. Gate NuGet passa com a saída real vazia do SDK e rejeita todos os envelopes incompletos/adversariais.
2. Todos os projetos da solução possuem lockfile rastreado aplicável; restore locked deixa `git status` limpo.
3. STATE-05 e STATE-06 terminam três vezes consecutivas no mesmo ambiente declarado com zero processo, listener, profile, store ou root temporário residual.
4. Rate limit de produção continua ativo; o harness não falha por competir consigo mesmo nem recebe privilégio de produção.
5. Timeout/cancelamento sempre alcança `finally`; cleanup é seguro também quando browser, Node ou host não responde.
6. CI executa os gates dentro de budgets explícitos e preserva logs sanitizados suficientes para diagnóstico.
7. Nenhum gate anuncia mais cobertura do que realmente verifica.

### Rollback e parada

Se a correção exigir acesso online, alteração de feed, instalação ou SHA não verificável, o lote para e solicita autoridade específica. Rollback nunca restaura o gate sabidamente incompatível como fonte de aprovação; na pior hipótese, o gate permanece fail-closed e explicitamente `BLOQUEADO`.

## R1 — Verdade e durabilidade da notificação local

### Objetivo

Garantir que `Accepted` signifique que a fronteira Windows recebeu a tentativa, sem perder itens já deduplicados durante rajada, shutdown ou crash.

### Escopo candidato futuro

- separar `Queued`, `Attempting`, `Accepted`, `Retryable`, `Rejected` e terminalidade factual;
- só persistir `Accepted` depois da chamada efetiva ao publicador moderno ou fallback Windows;
- manter itens aguardando em ledger/fila durável bounded e retomar em ordem após restart;
- preservar baseline silenciosa, deduplicação por transição, cursor, fencing e opt-in;
- limitar fila, tentativas, backoff e tempo; overflow deve gerar decisão explícita, nunca perda silenciosa;
- diferenciar aceitação da API Windows de exibição comprovada pelo Shell;
- manter clique sem execução e sem ação administrativa;
- manter e-mail, SMS, webhook e qualquer canal externo fora do lote.

### Critérios de conclusão

1. Rajada com itens `2..N` não produz `Accepted` antes da fronteira Windows correspondente.
2. Crash em cada fronteira entre enqueue, tentativa, resposta e commit resulta em retomada ou terminalidade factual, sem duplicação.
3. Restart, reconnect, replay, reorder, fallback ocupado e publisher moderno indisponível são cobertos.
4. A composição normal continua desabilitada por padrão e o sandbox exige ativação exata.
5. Teste multiprocess prova ledger/cursor; teste WPF prova o sink; uma amostra Windows visível só ocorre sob autorização humana separada.
6. Cleanup final prova zero Tray, ícone, processo e store temporário residual.

### Rollback

Desativar o consumidor reconciliado e preservar ledger/cursor para diagnóstico. Nunca fazer rollback marcando pendências como entregues ou retornando ao aceite por enqueue.

## R2 — Contenção da superfície de comandos

### Objetivo

Remover da composição normal qualquer impressão de comando operacional enquanto RBAC, confirmação, schema, ownership, replay, auditoria e executor permanecem incompletos.

### Subfase R2-A — contenção obrigatória

- desmapear ou retornar indisponibilidade tipada antes de persistência nas rotas normais de criação/poll/ack v1;
- preservar endpoints sandbox somente sob marker test-only exato;
- impedir configuração de contornar o bloqueio;
- garantir que comandos já persistidos não sejam oferecidos automaticamente;
- persistir `ExecutionPolicy.Never` de modo obrigatório e imutável no sandbox, ou isolar a fixture numa store própria;
- provar por route inventory/DI que não há executor, transport ou inbox operacional ativável.

R2-A encerra os achados atuais por contenção. Não cria protocolo novo.

### Subfase R2-B — programa futuro, fora da baseline

Somente se posteriormente solicitado, um contrato v2 completo exigirá:

- permissão exata por capability e scope;
- confirmação vinculada a actor, alvo, parâmetros tipados, reason e expiry;
- snapshot imutável e hashado da autorização;
- revalidação de RBAC, ownership, enabled/archive, versão e capability no commit, poll e pré-execução;
- cursor monotônico, replay journal, correlação `InReplyToMessageId`, idempotência e fencing;
- auditoria de criação, oferta, receipt, início, resultado, timeout e `UnknownOutcome`;
- credencial administrativa separada e post-probe;
- nenhum SQL, shell, utilitário ou texto de LLM livre.

Transporte receipt-only, executor e provider homologado são três gates distintos. R2-B não está autorizado por este plano.

### Critérios de conclusão de R2-A

1. A composição normal não cria, oferece, recebe nem executa comando.
2. As respostas HTTP e mensagens de startup são estáveis, tipadas e não revelam estado sensível.
3. Toda combinação de flags mantém command polling indisponível.
4. Nenhuma row sandbox se parece com trabalho executável normal.
5. `Start`, `Stop` e `Restart` continuam `Unsupported` no provider PostgreSQL.

### Rollback

Rollback seguro mantém as rotas desativadas. Nunca reativar v1 para restaurar compatibilidade; eventual compatibilidade requer shim explícito que continue não executável.

## R3 — Agent Fleet, identidade e ingresso non-secret

### Objetivo

Fechar as fronteiras Server→Agent e issuer→Server antes de qualquer ativação do Fleet normal.

### Escopo candidato futuro

- validar provider ID, endpoint tipado e credential reference opaca no Server antes de serializar assignment;
- repetir a validação no Agent antes do commit SQLite e preservar last-known-valid após rejeição;
- recusar propriedades desconhecidas, nested secret-like fields, provider desconhecido e propósito de credencial diferente de `Monitoring`;
- impedir que valores rejeitados apareçam em resposta, persistência, log ou relatório;
- tornar revogação da identidade principal independente da cardinalidade de certificados;
- revogar certificados em lotes retomáveis sem nunca reativar Agent;
- comparar SPKI do CSR com o certificado emitido antes do commit;
- limitar respostas HTTP por bytes reais, profundidade, schema e content type;
- manter `UnavailableAgentCertificateIssuer` e cliente Fleet normal desabilitado até PKI/vault/renewal/rotation possuírem autoridade própria.

### Critérios de conclusão

1. Fixtures com password, token, connection string, estruturas aninhadas e referência malformada são rejeitadas antes de transmissão e persistência.
2. Revogação com `0`, `1`, `128`, `129` e cardinalidade alta nega autenticação imediatamente após o commit principal e termina reconciliação de certificados de forma retomável.
3. CSR/certificado com chaves diferentes é recusado mesmo quando o issuer declara digest coerente.
4. Resposta em `N` bytes é aceita e `N+1` é recusada sem alocação não limitada.
5. Enrollment, renewal e revocation permanecem idempotentes e auditados.
6. Nenhuma PKI, chave ou credencial real é necessária para a evidência local; integração real continua separada.

### Rollback

Desabilitar distribuição de assignments, voltar ao issuer indisponível e preservar last-known-valid. Revogação é monotônica; rollback nunca reativa identidade ou certificado.

## R4 — Roteamento, durabilidade e readiness do Server

### Objetivo

Corrigir pendências semanticamente erradas e preparar, sem ativar, os contratos necessários à operação distribuída futura.

### Escopo candidato futuro

- introduzir binding explícito regra→canal com tenant/ambiente/escopo suficiente;
- quarentenar ou cancelar pendências históricas sem binding comprovável; nunca enviá-las por inferência;
- manter delivery externo desligado durante a correção;
- definir claim atômico, lease, fence, reclaim e idempotency key antes de qualquer canal externo;
- filtrar due/enabled/binding no SQL e impedir head-of-line blocking;
- separar `/health/live` de `/health/ready` e verificar configuração, PostgreSQL central e compatibilidade de schema sem expor segredo;
- iniciar deadline antes de assignments, limitar future skew e encerrar `pg_isready` em timeout ou cancelamento do host;
- preservar outbox/assignments em rollback e nunca apagar outcome ambíguo.

### Critérios de conclusão da baseline

1. Uma regra sem binding produz zero delivery enviável.
2. Matrizes regra/canal/ambiente provam ausência de cross-scope fanout.
3. Pendências antigas ambíguas permanecem quarentenadas e delivery continua recusando startup.
4. Readiness retorna `503` para banco/schema/configuração indisponível e liveness continua factual sobre o processo.
5. Fonte de assignments travada não ultrapassa o deadline global; timestamp futuro não suspende probes indefinidamente.
6. Cancelamento deixa zero `pg_isready` residual.

### Gate futuro de delivery

Claim/lease só pode ser considerado provado com concorrência real no provider central proprietário. O teste deve usar PostgreSQL descartável, pinned e autorizado separadamente, com crash antes/depois do side effect, fence antigo, duplicidade e dead-letter. Aprovar o binding não autoriza canal externo.

### Rollback

Delivery permanece desligado. Leases expiram sem confirmar side effect ambíguo. Bindings e quarentena não são removidos para restaurar fanout global.

## R5 — Migração, vault, packages, supply chain, packaging e legado

### Objetivo

Fechar riscos de filesystem/processo e preservar fail-closed das ferramentas que ainda não são operacionais.

### Escopo candidato futuro

- adicionar journal crash-consistent ao ConfigMigrator antes do replace;
- hash de source, target anterior, backup, manifest e report; revalidar imediatamente antes de commit/rollback;
- usar handles exclusivos ou detectar alteração concorrente; recovery determinístico no próximo start;
- manter dry-run default e proteger contra symlink/reparse point;
- substituir `PATH` por cliente Secret Service tipado ou caminho absoluto aprovado; limitar stdout/stderr, timeout e árvore de processo;
- retornar `Unavailable` sem fallback plaintext quando o vault não puder ser provado;
- exigir árvore exata, teto agregado e snapshot content-addressed imutável antes de loader dinâmico de provider;
- manter loader desabilitado até signing keys/rotation e modelo de confiança aprovados;
- fixar actions de CI por SHA somente com proveniência oficial verificada;
- manter packaging bloqueado até todas as dependências executáveis/transitivas possuírem versão/hash/proveniência;
- remover `pgIsReady` da configuração canônica distribuída, preservando-o apenas como input de migração compatível;
- respeitar `notifications.enabled` no legado e preservar `RESTARTED` por sua janela factual;
- ampliar Pester sem aumentar autoridade do legado ou restaurar service control.

### Critérios de conclusão

1. Fault injection em cada fronteira do migrador sempre recupera estado antigo ou novo completo e autenticado.
2. PATH hijack, output excessivo, timeout, link e substituição concorrente do helper falham fechados sem vazar segredo.
3. Package alterado após verificação, arquivo extra, hardlink/symlink, colisão de case e teto agregado são recusados.
4. Toolchain não homologada continua incapaz de produzir instalador.
5. Configuração gerada é canônica; compatibilidade legada permanece explícita, testada e não executa controle.
6. Ações de supply chain não são pinadas a valor inventado nem atualizadas sem autoridade de rede quando necessária.

### Rollback

ConfigMigrator antigo só pode permanecer disponível quando não houver journal incompleto. Vault Linux e loader retornam `Unavailable`. Packaging continua bloqueado. Nenhum fallback grava segredo ou baixa ferramenta.

## R6 — Verdade visual, acessibilidade e contratos frontend

### Objetivo

Garantir que Dashboard, TV, WPF e Tray apresentem somente estado factual, acessível e semanticamente equivalente.

### Escopo candidato futuro

- definir semântica canônica de `Enabled=false`, removê-lo de saúde corrente e apresentá-lo explicitamente;
- publicar snapshot e instante de aceitação atomicamente para evitar `Unknown` transitório por relógio antigo;
- rejeitar controls, bidi, whitespace periférico e texto acima dos limites em C# e TypeScript;
- atualizar o Design System para declarar SignalR como hint autenticado não autoritativo;
- atualizar a marca do flyout junto do agregado enquanto aberto;
- observar reduced motion do Windows e substituir animação indefinida por estado estático/textual;
- usar um adapter único de data/hora com `UTC` ou zona local explícita;
- ocultar ou rotular sparklines demonstrativas quando o snapshot for autoritativo;
- mapear valores desconhecidos para `Unknown`, nunca `Resolved`/`Information` por default;
- substituir glyphs de navegação por paths code-native canônicos;
- corrigir posicionamento/reflow/scroll do flyout por monitor e DPI, incluindo mudança de DPI;
- atualizar título localizado por rota, tornar IDs completos acessíveis e gerar registries de provider a partir do manifest canônico.

### Critérios de conclusão

1. `Enabled=false` não contribui como saúde atual e é distinguível sem depender de cor.
2. Snapshot recém-aceito nunca fica futuro por defasagem do relógio da UI.
3. Estados desconhecidos falham para `Unknown` nas duas tecnologias e idiomas.
4. Light/Dark, High Contrast, reduced motion, teclado, foco, screen reader, zoom/reflow e forced colours passam os gates proporcionais.
5. Mixed-DPI/200% é provado no hardware disponível ou permanece limitação factual explícita, sem aprovação inferida.
6. Nenhuma série demonstrativa parece telemetria autoritativa.
7. Design System, React e WPF usam os mesmos significados e fontes canônicas.

### Evidência humana separada

Depois do relatório automático aprovado, qualquer amostra visível exige autorização específica. Usará runtime e browser/perfil dedicados, nunca o navegador comum do usuário, e será encerrada antes do registro da decisão. A amostra deve cobrir `Enabled=false`, atualização do flyout aberto, reduced motion, High Contrast, timestamp/fuso e reflow disponível.

### Rollback

Reverter por feature/adapters de apresentação sem alterar o dado canônico. Nunca restaurar um fallback que converta estado desconhecido em saudável/resolvido ou esconda a origem demonstrativa.

## R7 — Pré-condições MOD-12 e relação com O1

### Autoridade

R7 é somente uma parte deste plano. Nenhuma alteração da fundação AIOps, O1, runtime ou promoção está autorizada. A [proposta operacional aceita como direção](STATE-06-MOD-12-Operational-AIOps-Programme-And-Restricted-Observer-Proposal.md) e o [ADR-0007 aceito](architecture/ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md) continuam proprietários do programa.

### R7-A0 — correções prévias da fundação inativa

Antes de o código atual poder servir como dependência de O1 ou de um gate futuro:

- separar `Capability=observer-analysis` de `ActivationState=None`; não declarar `Mode=OBSERVER` sem promoção;
- preservar status/evidence semantics ao adaptar duração de probe, ou separar séries por outcome;
- validar deadline absoluto depois de cada fase e imediatamente antes da publicação;
- publicar resultado parcial/cancelado/expirado como não autorizante e sem precision/recall/calibration/agregados de subconjunto;
- materializar apenas fontes imutáveis, bounded e cancellation-aware na admissão;
- transformar exceções de fonte não confiável em resultado tipado sanitizado;
- manter proveniência de corpus declarativa explicitamente não autoritativa até existir manifest/head assinado;
- adicionar vetores independentes de criptografia, cancelamento durante trabalho, input hostil, overflow e publicação stale.

R7-A0 não cria trust host, persistência, corpus real, telemetria, UI ou modo.

### O1 futuro — escopo preservado

Depois de R7-A0 e somente sob outra autorização, O1 poderá implementar o `Durable Trust Continuity and Resource Admission Sandbox` já definido:

1. mapear os `24` vetores de confiança, `36` de recursos e `8` de corpus/manifest ao código, teste e owner;
2. implementar host coordinator, bundle, papéis/chaves materialmente distintos, delegações, dual control e approvals one-use em fixtures públicas de teste;
3. persistir checkpoint/heads/audit intent de sandbox numa transação local atómica, com crash/restart, rollback, gap, divergência, quarentena e reconciliação;
4. aplicar reserva e envelope de bytes, estrutura, memória contabilizada, trabalho, tempo, cancellation, quiescência e fencing antes da análise;
5. manter control plane reservado, paralelismo inicial `1`, fila desabilitada e nenhum claim de fairness/frota;
6. usar somente manifest/head/corpus sintético bounded suficiente para provar confiança; corpus representativo, backtesting e provider real continuam pertencendo a O3/O5;
7. compor tudo apenas em sandbox opt-in exato, sem referência na composição normal;
8. fechar com relatório automático e Human Gate exclusivo do incremento, ainda mantendo modo `none`.

### Critérios de conclusão futuros de R7-A0/O1

- zero resultado completo ou autorizante após deadline, cancelamento, parcialidade ou context revision stale;
- papéis, identidades e chaves cruzadas/repetidas são recusados;
- checkpoint sobrevive crash/restart sem regressão, auto-bootstrap ou aceitação ambígua;
- corpus/manifest sintético possui assinatura, head e membership exata verificáveis;
- verifier puro continua sem rede, chave privada, store, recovery ou publicação;
- zero referência O1 na composição normal e zero acesso a provider, segredo, banco ou executor;
- build, testes unitários/property/fuzz/criptográficos/arquitetura/integration multiprocess, coverage, dependencies, secrets e documentação aprovados;
- cleanup prova zero listener, processo, store e diretório residual.

### Revisões humanas mínimas propostas para uma futura autorização

A futura proposta de implementação deverá decidir se essas revisões serão gates separados ou se integrarão um único Human Gate de O1 com seções e decisões explicitamente distintas. Nenhuma delas está pré-aprovada por este documento.

- `HG-O1-Trust`: bundle, papéis, checkpoint, rollback/recovery e relatório automático.
- `HG-O1-Data`: finalidade, retenção, manifest/corpus sintético e limites do que ele não prova.
- `HG-O1-Closure`: aceitar ou rejeitar somente o incremento restrito, repetindo que `OBSERVER` continua inativo.
- `HG-none→OBSERVER`: gate posterior, separado e fora de O1.

LLM, knowledge retrieval, recomendação, plano, executor, `ADVISOR`, `ASSISTANT` e `CONTROLLED_AUTOMATION` continuam fora de R7 e O1.

## R8 — Reauditoria consolidada e encerramento da remediação

### Objetivo

Reexecutar uma auditoria proporcional depois dos lotes efetivamente autorizados, sem reescrever evidência histórica e sem inferir progressão.

### Evidência mínima

- shutdown preflight e baseline Git limpa;
- revisão integral de cada diff/commit e matriz `AUD-* → teste → resultado → residual`;
- restore locked limpo, build Release, format/analyzers e testes da solução;
- coverage por componente/código alterado sem exclusão oportunista;
- testes Dashboard/WPF/Tray, Pester, migrations/model drift e E2E aplicáveis;
- fault injection, concorrência, crash/restart e rollback onde o lote exige;
- secret scan, dependency/vulnerability audit, supply-chain provenance e documentação/links;
- smoke fail-closed da composição normal;
- zero processo, listener, browser/profile, container, store ou diretório temporário residual;
- relatório automático único que distingue `APROVADO`, `REPROVADO`, `BLOQUEADO` e `NÃO TESTADO`.

### Critérios de encerramento do plano de remediação

O plano só pode ser declarado concluído quando:

1. todo `CORRIGIR` autorizado estiver comprovadamente resolvido;
2. todo `CONTER` estiver inacessível na composição normal e coberto por regressão;
3. todo `DECIDIR` possuir decisão explícita ou permanecer bloqueado sem implementação;
4. todo `ADIAR COM GATE` tiver pré-condição documentada e nenhuma ativação prematura;
5. não houver achado crítico/alto atual conhecido sem correção, contenção ou decisão informada;
6. os gates usados para a conclusão forem eles próprios reproduzíveis e aprovados;
7. rollback/kill switch, limitações e cobertura não testada estiverem registrados;
8. o Human Gate aplicável revisar relatório automático e amostras separadas, sem aprovação pré-preenchida;
9. `STATE-06`, o estado dos providers e o modo MOD-12 forem atualizados somente conforme decisões reais.

Encerrar esta remediação não significa AIOps operacional completa. Essa definição permanece na proposta do programa MOD-12 e exige `O1`–`O5`, depois gates independentes de `ADVISOR`, `ASSISTANT` e `CONTROLLED_AUTOMATION`.

## Estratégia de PostgreSQL

PostgreSQL continua recomendado em duas funções distintas:

1. persistência central do Server, sem tornar Domain/Application provider-specific;
2. primeiro provider de monitoramento candidato à homologação independente.

O plano não autoriza PostgreSQL real. Testes de contrato podem usar stores efêmeros, mas semânticas proprietárias como migration, trigger append-only, transaction isolation, `FOR UPDATE SKIP LOCKED`, lease/claim e concorrência não podem ser declaradas provadas apenas com SQLite. Quando um lote exigir essas garantias, deverá solicitar separadamente um laboratório PostgreSQL descartável, pinned, local, sem dados/credenciais reais, com cleanup e matriz exata de versão/topologia/capability. Um teste aprovado provará somente essa entrada, nunca suporte geral nem preferência arquitetural no núcleo.

## Regras transversais para qualquer autorização futura

Cada lote futuro deve:

1. começar pelo shutdown preflight obrigatório;
2. declarar baseline, arquivos/camadas, achados cobertos, exclusões e condição de parada;
3. preservar trabalho preexistente e usar commit focado sem push/publicação;
4. não adquirir dependência, usar rede ou abrir runtime externo sem autoridade específica;
5. não tocar banco monitorado, serviço, credential, PKI, IdP, vault ou canal real sem autorização própria;
6. manter defaults desabilitados e fail-closed;
7. usar migrations expand-contract, dry-run e rollback testado quando aplicável;
8. produzir relatório factual com comandos, versões, exit codes e artefatos sanitizados;
9. separar Quality Gate automático de Human Gate;
10. encerrar todos os runtimes e provar zero resíduo antes do hand-off;
11. não avançar lifecycle ou modo automaticamente;
12. parar ao encontrar necessidade material fora do lote.

## Modelo de autorização e decisões

A autoridade usada para criar este plano se encerra com sua entrega e validação. Nenhum lote está liberado.

As decisões válidas para revisar este documento são:

- `ACEITO O PLANO COMO DIREÇÃO, SEM IMPLEMENTAÇÃO`
- `ACEITO COM RESSALVAS: <ressalvas>`
- `AJUSTES SOLICITADOS: <ajustes>`
- `REJEITO O PLANO: <motivo>`

Mesmo a primeira decisão apenas aceita a ordem e os critérios. Depois dela, a primeira eventual autorização executiva deverá nomear exatamente um lote — recomenda-se `R0` —, seus achados, arquivos/fronteiras, runtime local permitido, proibições e evidência esperada. Uma autorização de `R0` não libera `R1`, O1 ou qualquer outro lote.

## Entregáveis desta autoridade documental

Esta autoridade produz somente:

- este plano de remediação;
- referência factual no estado corrente;
- validações documentais locais;
- commit focado sem push ou publicação.

Nenhum código de produto/teste, projeto, package, lockfile, configuração executável, migration, runtime, dado, relatório de implementação ou artefato gerado é criado ou alterado.
