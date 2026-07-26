# Controle e Changelog do Sistema de Instruções

## Versão atual

- Versão: `4.1.0`
- Data: 2026-07-26
- Status: corpus consolidado com handoff compacto e autoridade arquitetural factual
- Escopo: 14 arquivos ativos

A versão do corpus é independente da versão do software.

## Política SemVer

- MAJOR: mudança incompatível de autoridade, precedência, estados ou estrutura.
- MINOR: nova capacidade, playbook, módulo ou gate sem quebrar o fluxo.
- PATCH: clareza, correção ou referência sem mudança de autoridade.

Toda alteração atualiza este arquivo e, quando necessário, `../Start-Here.md`.

## 4.1.0 — 2026-07-26

- Substitui o antigo handoff de seis categorias obrigatórias por uma resposta
  compacta que começa pelo status, separa o concluído do restante para o alvo
  solicitado e declara diretamente a ação do proprietário. Contagens de
  lifecycle, ativação, macroetapas e decisões formais continuam disponíveis,
  mas somente aparecem quando pertencem ao alvo ou são solicitadas.
- Alinha o Prompt Mestre a esse contrato de comunicação sem alterar a
  precedência, as autoridades temáticas ou os gates existentes.
- Registra a stack aceita como obrigatória, distingue diretórios de providers
  atuais do catálogo futuro e remove comando administrativo do pseudo-contrato
  de `IDatabaseProvider`, preservando adaptadores, credenciais e identidades
  separados conforme ADR-0006.
- Distingue a declaração estática mínima de capability já implementada do
  descriptor temporal observado ainda contratual. Nenhum timestamp,
  prerequisite, heartbeat claim, suporte ou homologação foi inventado.
- Mantém os 14 arquivos ativos, `STATE-06 INTEGRATION`,
  `ActivationState=None` e todas as fronteiras de runtime, lifecycle e ação
  externa.

## 4.0.2 — 2026-07-26

- Encaminha a regra temática de SSRF do proprietário
  `governance/Security-And-Access.md` para o contrato normativo local de CIDR,
  porta, DNS, pinning, redirects e PKI offline em
  `docs/architecture/Network-Egress-Policy.md`.
- Preserva `Start-Here.md`, a matriz PM-1, a precedência, os 14 arquivos
  ativos e todas as autoridades existentes; o documento arquitetural detalha
  a implementação, mas não concede runtime, conexão externa, lifecycle,
  Human Gate ou ativação.
- Registra a conclusão técnica local de R-NET sem alterar
  `STATE-06 INTEGRATION` ou `ActivationState=None`.

## 4.0.1 — 2026-07-25

- Reduz `state/Current-State.md` de 126.885 para 10.877 bytes e substitui a
  cronologia acumulada por um único snapshot factual: `STATE-06 INTEGRATION`,
  `ActivationState=None`, composição normal, capacidades inativas, limites
  correntes e decisões que ainda exigem autoridade própria.
- Preserva antes da redução todos os fatos retirados: as 102 referências do
  documento anterior resolvem para 87 alvos locais existentes, e a auditoria
  linha a linha confirmou a presença da narrativa restante no log append-only
  ou no relatório proprietário. Nenhum backfill histórico foi necessário.
- Declara no cabeçalho do log que campos de próxima decisão ou ação são
  históricos no contexto da entrada e nunca substituem o estado vigente.
- Distingue auditoria isolada de uma solicitação que já autorize, de forma
  separada e inequívoca, um lote posterior de implementação, sem permitir
  ampliação automática de escopo.
- Torna explícito que `ActivationState=None` precede os modos MOD-12 e que
  `OBSERVER` é o primeiro modo ativável, não o estado atual.
- Subordina localmente a precedência genérica do Prompt Mestre ao
  `Start-Here.md`, alinha seu próximo passo ao handoff obrigatório de
  `AGENTS.md` e renomeia o pseudoestado de tarefa para `WORK_STATUS`, evitando
  colisão com `STATE-00`–`STATE-08`.
- Registra sem retroagir versões que D1–D5 já estavam preservados no
  `State-Transition-Log.md` e nos relatórios proprietários, embora não tivessem
  recebido entradas próprias neste changelog antes da versão `4.0.0`.
- Não altera código, configuração, dependências, runtime, lifecycle,
  `ActivationState`, documentação pública, autorização externa ou estado de
  qualquer gate técnico ou humano.

## 4.0.0 — 2026-07-25

- Incorpora as `30` macroseções completas da fonte
  `PROMPT_MESTRE_CODEX.md` como
  `system/AI-Software-Engineering-Master-Prompt.md`, sem manter cópia paralela
  na raiz, preservando copyright, título, uso recomendado, versão de origem
  `2.0.0` e SHA-256, e registrando explicitamente as adaptações.
- Registra no próprio Prompt Mestre a matriz normativa das 30 macroseções, os
  proprietários temáticos, o mapeamento F0–F12 para `STATE-00`–`STATE-08` e os
  trechos que permanecem informativos ou condicionais.
- Torna explícita a precedência entre plataforma, solicitação atual, limites
  não renunciáveis, instruções de diretório, estado factual, autoridades
  temáticas, Prompt Mestre, playbooks, templates e história; as decisões PM-1
  prevalecem nos conflitos antigos que resolvem, enquanto o proprietário
  temático permanece canônico nos itens delegados pela matriz.
- Adota autorização explícita ou inclusão inequívoca para commit local e
  Conventional Commits, sem ampliar essa autoridade para qualquer ação remota
  ou de entrega.
- Mantém `70%` de linhas e `45%` de branches como pisos automáticos, define
  `80%` de linhas como meta orientativa baseada em risco e proíbe reduzir pisos
  por componente sem decisão, evidência e registro.
- Adapta trabalho multiagente à disponibilidade da plataforma e a ganho
  material, sem expandir o escopo autorizado ou substituir revisão
  independente e Human Gate.
- Atualiza a estrutura ativa de `13` para `14` arquivos. Esta mudança não
  altera código, dependências, estado factual, `STATE-06`,
  `ActivationState=None` ou qualquer autoridade de runtime, release ou
  transição.

## 3.56.34 — 2026-07-25

- Torna obrigatório identificar o objetivo exato usado em toda contagem de
  trabalho restante.
- Separa e nomeia lotes técnicos, decisões formais, etapas macro, estados do
  lifecycle e transições de ativação, sem dupla contagem silenciosa.
- Exige distinguir itens obrigatórios, condicionais e indeterminados e
  recalcular as contagens quando o objetivo ou o escopo mudar.
- Padroniza o próximo passo imediato e exige um bloco exato para copiar e colar
  sempre que uma resposta ou autorização do utilizador for necessária.

## 3.56.33 — 2026-07-25

- Registra a campanha física O5-R5 pós-O5-R5-B como `REPROVADA` no primeiro
  threshold físico excedido.
- Classifica HM-02 `REPROVADO`, HM-01 `BLOQUEADO` sem matriz completa e HM-03
  `NÃO TESTADO`, sem inferir resultados das fases não executadas.
- Preserva o O5-R5-B automático, o protocolo congelado,
  `ActivationState=None`, zero correção pós-resultado e cleanup integral.
- Bloqueia O5-R6 e encaminha somente para proposta separada de remediação do
  working-set headroom.

## 3.56.32 — 2026-07-25

- Registra o O5-R5-B automaticamente `APROVADO` como driver físico test-only
  isolado e inerte sem o digest de opt-in exato.
- Registra a matriz bounded de `560` cenários e a parada no primeiro limite,
  resumo ou variância reprovado.
- Preserva zero referência na composição normal, dependências inalteradas,
  `ActivationState=None` e `HM-01`–`HM-03` ainda `NÃO TESTADOS`.
- Encaminha somente para a campanha física já autorizada; O5-R6, `OBSERVER` e
  transição continuam fora do escopo.

## 3.56.31 — 2026-07-25

- Registra a repetição física O5-R5 como `BLOQUEADA` antes de qualquer medição.
- Distingue a fonte física existente da ausência de entrypoint e workloads
  físicos executáveis.
- Preserva `HM-01`–`HM-03` `NÃO TESTADOS`, `ActivationState=None` e zero
  alteração de código/configuração.
- Encaminha somente para proposta O5-R5-B; O5-R6 e transição continuam
  bloqueados.

## 3.56.30 — 2026-07-25

- Registra a decisão humana exata `HUMAN GATE DO O5-R5-A: APROVADO`.
- Aceita somente protocolo e runner de prontidão, sem converter
  `HM-01`–`HM-03` em resultado físico.
- Mantém `ActivationState=None`, O5 geral bloqueado e O5-R6 não autorizado.
- Encaminha para a repetição O5-R5 autorizada separadamente.

## 3.56.29 — 2026-07-25

- Registra o O5-R5-A automático como `APROVADO` somente para prontidão de
  medição e runner test-only.
- Congela versão, digest, thresholds, headroom, repetições, estatística e
  critérios de parada antes de qualquer resultado físico.
- Preserva O5-R5 `BLOQUEADO`, `HM-01`–`HM-03` `NÃO TESTADOS`,
  `ActivationState=None` e composição normal sem referência O5-R5-A.
- Encaminha somente para Human Gate O5-R5-A; não autoriza campanha física,
  O5-R6, dado/provider real, `OBSERVER` ou transição.

## 3.56.28 — 2026-07-24

- Registra factual e fail-closed o O5-R5 como `BLOQUEADO` antes da campanha
  física, sem converter ausência de runner, thresholds ou profiler em medição.
- Preserva `HM-01`, `HM-02` e `HM-03` como `NÃO TESTADOS`,
  `ActivationState=None` e o bloqueio do O5.
- Encaminha somente para proposta separada de prontidão O5-R5-A; não autoriza
  instrumentação, instalação, O5-R6, ativação ou transição.

## 3.56.27 — 2026-07-24

- Torna obrigatório encerrar cada atividade com uma contagem separada dos lotes
  técnicos, Human Gates ou decisões formais e eventual autorização final de
  transição ainda pendentes.
- Exige declarar a base da contagem e distinguir valores condicionais ou ainda
  não determináveis.
- Mantém inalteradas as autoridades, os gates e o lifecycle do produto.

## 3.56.26 — 2026-07-23

- Registra a decisão humana exata `HUMAN GATE DO O1: APROVADO`.
- Aceita somente o sandbox O1 test-only opt-in e preserva as limitações da evidência sintética local.
- Mantém `STATE-06 INTEGRATION`, `ActivationState=None`, zero referência na composição normal e nenhuma ativação de `OBSERVER`.
- Não autoriza LLM, recomendação, comando, automação, execução operacional, push, deploy ou transição de lifecycle.

## 3.56.25 — 2026-07-23

- Registra a implementação automática local do O1 sobre `c38c494413d16ce1090d31f30d7b2efa87c589dd`, exclusivamente em sandbox test-only opt-in.
- Registra trust bundle assinado, papéis/chaves sintéticos distintos, dual control one-use, checkpoint/heads/audit intent atómicos, crash/recovery, quarentena e admissão de recursos/fencing.
- Rastreia exatamente `24` vetores de confiança, `36` de recursos e `8` de corpus e mantém o resultado humano O1 `PENDENTE`.
- Preserva `ActivationState=None`, zero referência na composição normal e a proibição de `OBSERVER`, dados/providers reais, LLM, recomendação, comando, automação e transição.

## 3.56.24 — 2026-07-23

- Registra a decisão humana exata `HUMAN GATE DO R8: APROVADO COM RESSALVAS`.
- Encerra humanamente somente a remediação da auditoria, preservando 35 achados encerrados, quatro contidos e todas as limitações registradas.
- Mantém `STATE-06 INTEGRATION`, `ActivationState=None` e a ausência de autorização para R2-B, O1, `OBSERVER`, AIOps operacional ou transição.
- Registra somente documentação e o Human Gate próprio do R8, sem código, runtime, push ou deploy.

## 3.56.23 — 2026-07-23

- Corrige a evidência de cleanup do R8 depois de Bruno reportar uma aba genérica do Windows Terminal deixada pelo teste sintético preexistente de `ping.exe`.
- Registra que a primeira declaração de cleanup era prematura e que a aba foi fechada por Bruno antes da revalidação.
- Registra zero janela correspondente, `ping.exe`, processo/listener ou temporário DB-Notifier e a preferência preservada byte a byte.
- Mantém R8 automaticamente aprovado somente após a prova corrigida, com Human Gate pendente e sem código, O1, `OBSERVER`, AIOps operacional ou transição.

## 3.56.22 — 2026-07-23

- Registra a reauditoria consolidada R8 sobre `6e529810aee82446c9f2863363201ac2ef25d151`, sem implementação ou correção.
- Classifica os 39 achados originais como 35 `ENCERRADO`, quatro `CONTIDO`, zero `ABERTO`, zero `BLOQUEADO` e zero achado `NÃO TESTADO`.
- Registra os gates locais proporcionais aprovados e preserva o incidente NuGet R5, as limitações físicas R6 e as demais evidências não testadas.
- Mantém aceitação humana R8 pendente, `STATE-06 INTEGRATION`, `ActivationState=None` e a proibição de R2-B, O1, `OBSERVER`, AIOps operacional ou transição.

## 3.56.21 — 2026-07-23

- Registra a decisão humana exata `ACEITO O R7-A0, SEM AUTORIZAR O1, OBSERVER, R8, AIOPS OPERACIONAL OU TRANSIÇÃO.`
- Encerra R7-A0, `AUD-H13` e `AUD-M11` somente no escopo bounded da fundação MOD-12 inativa, preservando `ActivationState=None`.
- Não autoriza trust host, persistência, corpus/telemetria real, O1, `OBSERVER`, R8, AIOps operacional, execução, promoção ou transição de lifecycle.

## 3.56.20 — 2026-07-23

- Registra a implementação automática local do R7-A0 sobre `ee16f302e703ccad7574c77cb374cd0f44905508`, limitada a `AUD-H13` e `AUD-M11`.
- Separa `Capability=observer-analysis` de `ActivationState=None`, preserva outcomes de probe e torna publicação cancelada, expirada, stale ou parcial vazia e não autorizante.
- Registra materialização offline bounded/cancellation-aware, exceções sanitizadas e corpus `DeclaredOnly`, com 483/483 testes da solução e coverage proporcional aprovados.
- Mantém aceitação humana R7-A0 pendente e não autoriza trust host, persistência, corpus/telemetria real, O1, `OBSERVER`, R8, AIOps operacional ou transição.

## 3.56.19 — 2026-07-23

- Registra a decisão humana exata `ACEITO O R0-F1, SEM AUTORIZAR R7-A0, R8, O1, AIOps OU TRANSIÇÃO.`
- Mantém a correção restrita à asserção arquitetural e preserva runners, workflow, dependências e produto.
- Preserva o incidente NuGet R5 e os resultados R0 históricos nas suas baselines.
- Não autoriza implementação adicional, execução operacional ou transição de lifecycle.

## 3.56.18 — 2026-07-23

- Registra a correção focal R0-F1 da asserção arquitetural que atribuía ao workflow nomes de diagnósticos pertencentes aos runners.
- O gate local corrente passa `52/52` testes de arquitetura e `477/477` testes da solução, preservando workflow, runners, dependências e lockfiles.
- Mantém os resultados R0 anteriores como evidência histórica e preserva o incidente NuGet R5.
- Não autoriza R7-A0, R8, O1, AIOps, execução operacional ou transição de lifecycle.

## 3.56.17 — 2026-07-23

- Registra o Human Gate específico do lote de remediação R6 como `APROVADO COM RESSALVAS`.
- Mantém R6-HV-P01, High Contrast físico, reduced motion físico, Narrator, escala Windows 200% e mixed-DPI como não testados e limitações explícitas.
- Distingue este encerramento do Human Gate histórico do `STATE-06` e preserva a ausência de transição.
- Não autoriza correção R0, alteração do incidente R5, R7-A0, R8, O1, AIOps ou execução operacional.

## 3.56.16 — 2026-07-23

- Registra a execução visível isolada de `R6-HV-W02` e a decisão humana exata `AMOSTRA R6-HV-W02 APROVADA`.
- Preserva o bloqueio anterior como evidência histórica, mantém P01 e suas cinco condições não testadas e mantém a aceitação humana R6 pendente.
- Registra preferência preservada e cleanup sem processo, janela, Tray, listener ou diretório temporário residual.
- Não autoriza implementação, correção R0, alteração do incidente R5, R7–R8, R7-A0/O1, AIOps ou transição de lifecycle.

## 3.56.15 — 2026-07-23

- Registra a implementação automática local do mecanismo W02 test-only, presentation-only e in-memory, sem executar a amostra humana.
- O Design System `3.2.1` documenta marker exato/único, sequência bounded, atualização coerente do Tray/flyout, bloqueio do shell secundário e supressão integral de publishers e fallbacks no modo W02.
- Preserva `R6-HV-W02` bloqueada até autorização visível separada, P01 não testada e a aceitação humana R6 pendente.
- Não autoriza notificação Windows, persistência, integração operacional, comando, acesso externo, correção R0, alteração do incidente R5, R7–R8, R7-A0/O1, AIOps ou transição.

## 3.56.14 — 2026-07-23

- Registra a repetição visível `R6-HV-W01` após R6-WEB1 e a decisão humana exata `AMOSTRA R6-HV-W01 REPETIDA — APROVADA`.
- Preserva as reprovações anteriores como evidência histórica e mantém W02 bloqueada, P01 não testada e a aceitação humana R6 pendente.
- Registra execução local isolada, preferência restaurada e cleanup sem processo, janela, Tray, listener ou diretório temporário residual.
- Não autoriza implementação, correção R0, alteração do incidente R5, R7–R8, R7-A0/O1, AIOps ou transição de lifecycle.

## 3.56.13 — 2026-07-22

- Registra a conclusão automática restrita do R6-WEB1 sobre `1a192a82b3879975e5ecd51a1ebfd156c9db52da`, sem converter o resultado em aprovação humana de W01 ou R6.
- `Current-State.md` e o log append-only registram contenção integral do label desabilitado, reflow por largura real do painel, zero interseção status/latência/sparkline, matriz bilingue Light/Dark com forced-colours, page-scale e zoom/reflow focal, e preservação do ramo TV autoritativo sem tendência demonstrativa.
- Preserva WPF/Tray, dependências, lockfiles, fixtures e contratos sem diff, a única falha global R0 e o incidente NuGet R5 sem correção; registra também o cleanup final do perfil GUID residual do runner.
- A próxima decisão passa a ser proposta separada para repetir visivelmente somente W01; W02, P01, aceitação R6, R7–R8, R7-A0/O1, AIOps e transição continuam não autorizados.

## 3.56.12 — 2026-07-22

- Registra a decisão humana que manteve `R6-HV-W01` reprovada após R6-UI1 porque, no Web compacto, o texto do estado desabilitado ultrapassou o status pill e invadiu a região do sparkline durante o reflow.
- Preserva o resultado automático R6-UI1 somente no seu escopo, sem convertê-lo em aprovação humana; registra a parada antecipada, `1920×1080` não testado, preferência restaurada e cleanup sem resíduo.
- A próxima decisão passa a ser proposta separada de remediação focal Web; W02, P01, aceitação R6, correção R0, incidente R5, R7–R8, R7-A0/O1, AIOps e transição continuam não autorizados ou inalterados.

## 3.56.11 — 2026-07-22

- Registra a conclusão automática restrita do R6-UI1 sobre `321e04d83cdaeab6ad3a4bdedd929c60083788fc`, sem converter o resultado em aprovação humana de W01 ou R6.
- `Current-State.md` e o log append-only registram pills/texto, gráfico de Providers, gutters, tabelas e reflow corrigidos; 64/64 rotas executáveis aprovadas, 32 combinações físicas não testadas, preferência restaurada e cleanup concluído.
- Preserva Web, dependências e lockfiles sem diff, a única falha global R0 e o incidente NuGet R5 sem correção.
- A próxima decisão passa a ser uma proposta separada para repetir visivelmente somente W01; W02, P01, aceitação R6, R7–R8, R7-A0/O1, AIOps e transição continuam não autorizados.

## 3.56.10 — 2026-07-22

- Registra a decisão humana que manteve `R6-HV-W01` reprovada depois do R6-WPF2 por defeitos de acabamento, status containment, gráfico de Providers, reflow e tabelas.
- Preserva o resultado automático R6-WPF2 no seu escopo sem convertê-lo em aprovação humana e mantém `1920×1080` não testado pela área útil física.
- A próxima decisão passa a ser uma proposta separada de remediação; W02, P01, aceitação R6, R7–R8, R7-A0/O1, AIOps e transição continuam não autorizados.

## 3.56.9 — 2026-07-22

- Registra a conclusão automática restrita do R6-WPF2, sem transição de lifecycle e sem substituir a reprovação humana vigente de `R6-HV-W01`.
- O Design System `3.2.0` passa a explicitar paridade estrutural e informacional Web/WPF para KPIs, estados, identidades de provider, alertas, distribuição e gráfico de desempenho, preservando adaptações nativas do Windows.
- `Current-State.md` e o log append-only registram 64/64 amostras executáveis aprovadas, 32 combinações físicas `1920×1080` não testadas, restauração exata da preferência, Web/dependências imutáveis e a falha global R0 e o incidente NuGet R5 sem correção.
- A próxima decisão permanece uma proposta separada para repetir visivelmente somente W01; W02, P01, aceitação R6, R7–R8, R7-A0/O1, AIOps e transição continuam não autorizados.

## 3.56.8 — 2026-07-20

- Bruno adotou explicitamente o `ADR-0007` como decisão arquitetural, sem autorização de implementação.
- O estado corrente e os contratos de arquitetura passam a distinguir a decisão `accepted` da implementação inexistente; os registros anteriores que o classificavam como `proposed` permanecem como evidência histórica datada.
- O1, runtime, persistência, telemetria, provider ou banco real, PostgreSQL, LLM, recomendação, execução, `none → OBSERVER`, promoção e transição continuam não autorizados. `STATE-06 INTEGRATION` permanece inalterado.

## 3.56.7 — 2026-07-19

- Bruno solicitou exclusivamente uma proposta documental para o `Incremento 2 — Authenticated SignalR Change Hint Sandbox`, sem implementação, runtime operacional, acesso externo, promoção ou transição.
- A proposta preserva a API e a reconciliação de 30 segundos como fontes autoritativas, restringe SignalR a um hint mínimo autenticado no sandbox, separa o deadline periódico das leituras antecipadas e define coalescência, budget, fencing, cancelamento e reconnect limitados.
- A inspeção factual encontrou zero implementação SignalR e ausência do cliente oficial `@microsoft/signalr` no manifesto/lockfile; a consulta direcionada ao cache npm local não retornou entrada correspondente. Nenhum pacote foi adquirido; uma implementação futura exige decisão explícita entre aquisição controlada da dependência oficial e adiamento, sem cliente artesanal ou CDN.
- `STATE-06 INTEGRATION` permanece inalterado. Incrementos 2–4, runtime, acesso externo, notificações, comandos, promoção e transição continuam não autorizados.

## 3.56.6 — 2026-07-19

- Bruno aceitou explicitamente o Human Gate próprio do `Incremento 1 — Authoritative Observation Pipeline E2E Sandbox`, commit `3449918`, com todas as limitações registradas depois da revisão orientada do resultado em linguagem simples, da sequência E2E, das limitações residuais e da classificação dos gates.
- A decisão autoriza exclusivamente seu registro factual e não autoriza novo incremento, runtime operacional, SignalR, notificações, comandos, promoção ou transição. `STATE-06 INTEGRATION` permanece inalterado.
- O Quality Gate automático restrito e a evidência técnica permanecem como documentados. Incrementos 2–4 e o Quality/Human Gate final de saída do `STATE-06` continuam não avaliados e não autorizados.

## 3.56.5 — 2026-07-19

- Bruno autorizou separadamente somente o `Incremento 1 — Authoritative Observation Pipeline E2E Sandbox` do plano consolidado, com fonte sintética/provider-neutral, Agent SQLite/outbox, HTTPS/mTLS de teste, ingestão/projeção read-only e runtimes temporários exclusivamente locais.
- A implementação e o relatório registram a cadeia sintética → Agent → outbox → API → snapshot TV, falhas/replay/reorder/staleness, recusa de uma requisição em voo depois da revogação commitada, isolamento da composição normal e cleanup integral. A janela interna pós-consulta `Active`/pré-commit, PostgreSQL real, Agent version N-1/N+1 e identidade operacional permanecem limitações explícitas.
- O Quality Gate automático restrito está aprovado no escopo local/offline. O Human Gate do incremento, Incrementos 2–4, SignalR, notificações, comandos, promoção e transição continuam pendentes ou não autorizados; `STATE-06 INTEGRATION` permanece inalterado.

## 3.56.4 — 2026-07-18

- Bruno autorizou exclusivamente um plano documental consolidado de fechamento do `STATE-06`, sem implementação, runtime, acesso externo, promoção ou transição.
- O plano reconcilia o Lifecycle com as evidências aceitas e organiza quatro incrementos futuros: cadeia autoritativa Agent → API → interfaces, hint SignalR autenticado, notificação Windows local baseada em transição reconciliada e transporte durável de comandos sem executor.
- Quality Gate consolidado, amostras humanas, Human Gate de saída e eventual `STATE-06 → STATE-07` permanecem decisões posteriores e independentes. Nenhum incremento executável foi autorizado.

## 3.56.3 — 2026-07-18

- Bruno aceitou explicitamente o Human Gate próprio de `Dashboard TV Browser Composition and Recovery Evidence Sandbox`, commit `588ffec`, com todas as limitações registradas depois de confirmar concordância com o relatório, a evidência E2E, as limitações e a classificação do gate apresentados.
- A decisão autoriza exclusivamente seu registro factual e não autoriza novo incremento, runtime operacional, SignalR, integração externa, promoção ou transição. `STATE-06 INTEGRATION` permanece inalterado e nenhuma próxima atividade foi inferida.

## 3.56.2 — 2026-07-18

- Bruno aceitou explicitamente o Human Gate próprio de `Provider Identity Icons` no `STATE-06`, com todas as limitações registradas, depois do resumo da evidência automática, da amostra anterior Dashboard/WPF/Tray em Light, Dark e High Contrast, da baseline inalterada de onze identidades/22 variantes e do resultado conservador da pesquisa alternativa.
- A decisão não autoriza provider, banco externo, monitoramento, comando, infraestrutura, runtime, promoção ou transição. Os providers sem asset elegível continuam no fallback neutro, e `STATE-06 INTEGRATION` permanece inalterado.

## 3.56.1 — 2026-07-18

- Bruno ampliou explicitamente a autoridade do incremento `Provider Identity Icons` com a instrução `Os icones que você não conseguiu encontrar busque em outros sites na internet`, limitada à pesquisa de ativos públicos estáticos exatos, sem dispensar revisão de licença/marca nem autorizar provider, runtime operacional, promoção ou transição.
- O Design System `3.1.1` esclarece que uma fonte alternativa só pode entrar no registro quando identidade exata, cópia/redistribuição do arquivo e permissão de marca aplicável ao uso/suporte factual corrente estiverem simultaneamente evidenciadas. Licença de coleção, origem oficial, atribuição ou meta futura isoladas não bastam.
- A pesquisa avaliou Firebird e OpenSearch, além dos nomes ausentes já conhecidos, mas não aceitou novo ativo. A cobertura implementada permanece nas onze identidades/22 variantes Skill Icons; Firebird, OpenSearch, Microsoft SQL Server, Azure SQL, Oracle Database, SAP HANA, IBM Db2, MariaDB, Valkey, CockroachDB, Couchbase, Apache CouchDB, ScyllaDB, InfluxDB, Neo4j e todo ID não mapeado preservam o fallback neutro.
- Nenhum código, manifest, gerador, asset, registro, teste ou saída gerada integra o resultado conservador. Os gates documentais, de links, segredos e escopo foram aprovados; `STATE-06 INTEGRATION`, o Quality Gate automático histórico da baseline e o Human Gate próprio permanecem inalterados.

## 3.56.0 — 2026-07-18

- Bruno autorizou um incremento de identidade visual de providers para que cada banco use seu ícone exato quando disponível, preservando a meta aberta de aceitar providers futuros.
- O Design System `3.1.0` introduz `ProviderIdentityIcon` na fronteira de apresentação: nome textual obrigatório, registro declarativo aberto, fallback neutro para qualquer ID não mapeado ou falha de ativo e fallback por cor de sistema em forced colours/Windows High Contrast.
- Ativos de terceiros devem permanecer locais, fixados por revisão, hash, licença e proveniência, sem download em runtime nem substituição por marca semelhante. Presença do ícone não prova implementação, homologação, saúde, freshness ou suporte público.
- A marca global DB Notifier, seus estados agregados e os tokens categoriais provider-neutral permanecem independentes da identidade visual de qualquer fornecedor.

## 3.55.2 — 2026-07-17

- Bruno determinou que toda entrega final informe explicitamente o próximo passo e detalhe o que ele deve fazer em linguagem acessível a uma pessoa leiga.
- `AGENTS.md` passa a exigir instruções ordenadas, local e forma de execução, resultado esperado, limites de segurança/ciclo de vida e a evidência ou resposta exata necessária para continuidade, com explicação de termos e comandos não familiares.
- Quando nenhuma ação do utilizador for necessária, a entrega deve declarar isso e identificar qual evento ou autorização abriria o próximo passo, em vez de terminar com recomendação vaga.

## 3.55.1 — 2026-07-17

- Bruno corrigiu o escopo do shutdown preflight: o gatilho é cada nova ação técnica autorizada no código, workspace ou produto, não cada conversa ou mensagem.
- Aprovação que libera execução, alteração de fonte/configuração/documentação, ajuste, remediação, modificação, implementação, auditoria executável, build, teste e amostra humana exigem encerramento e verificação antes de começar.
- Pergunta, explicação, planejamento sem execução, status e conversa sem mutação ou runtime não disparam shutdown. Uma amostra visível pode permanecer durante sua ação e deve ser encerrada antes da ação técnica seguinte, inclusive o registro documental da aprovação.
- A correção substitui a regra operacional corrente de `3.55.0` sem apagar seu registro histórico; fronteiras de processos externos, bancos, navegador comum e IDE permanecem inalteradas.

## 3.55.0 — 2026-07-17

- Bruno determinou um shutdown preflight obrigatório no início de toda nova mensagem, inclusive solicitação, aprovação, esclarecimento, ajuste, modificação, implementação, auditoria ou pedido documental.
- `AGENTS.md`, `Start-Here`, governança, playbook e templates passam a exigir encerramento e verificação de WPF/Tray, Dashboard preview/dev server, Agent, API, helpers e navegador dedicado pertencentes ao DB-Notifier antes de continuar. Uma amostra pode permanecer aberta somente até a resposta humana; essa resposta começa pelo shutdown.
- Processos hospedados por runtimes compartilhados devem ser atribuídos por PID, caminho, command line, parentage, listener ou perfil temporário. Banco monitorado, serviço externo, navegador/perfil comum, IDE e processo não atribuído permanecem fora do encerramento automático.
- Ao receber a instrução, o preview Dashboard preexistente PID `29292` foi encerrado; nenhum processo DB Notifier ou listener daquele PID permaneceu. A regra não altera o estado de ciclo de vida nem autoriza `STATE-06`.

## 3.54.0 — 2026-07-16

- Bruno definiu `MOD-12 AIOPS_AI` como o principal diferencial estratégico do DB-Notifier, sem dispensar governança de dados, sequência do ciclo, Quality/Human Gates, homologação, menor privilégio ou separação entre recomendação, aprovação e execução.
- O rascunho avulso `Prompt-IA.md` foi reconciliado com seu proprietário canónico `foundation/AIOps-And-AI-Module.md`, sem criar um 14.º arquivo ativo nem substituir os guardrails mais seguros já consolidados.
- Planos estruturados passam a incluir duração estimada; exclusão de dados fica explicitamente proibida de automação e operações destrutivas preservam owner humano, dupla confirmação, runbook ensaiado e recuperação verificável.
- A exigência original de logs completos permanece adaptada ao contrato de auditoria completa com logs sanitizados, nunca à retenção ou exposição de secrets e dados sensíveis.
- A linguagem arquitetural passa a distinguir a fundação local/in-memory inativa de MOD-12 de um modo operacional promovido: código de evidência, thresholds e capacidade não implica pipeline, integração, dataset, UI, LLM, executor ou aprovação `none → OBSERVER`.

## 3.53.1 — 2026-07-16

- Bruno determinou que toda revisão local do Dashboard deve abrir em um processo de navegador dedicado com perfil temporário isolado, nunca no navegador ou perfil que ele já esteja usando.
- Uma janela separada no navegador ativo deixa de satisfazer o protocolo. Processos, perfis, janelas e abas preexistentes permanecem intocados; somente o navegador dedicado pode ser entregue ou fechado, e seu perfil temporário só pode ser removido depois que o processo terminar.

## 3.53.0 — 2026-07-16

- Bruno autorizou explicitamente uma remediação demonstrativa local no `STATE-05`: uma notificação por mudança individual da fixture, sem notificação inicial, Agent, API, banco externo ou transição.
- Design System `3.0.1` e `AGENTS.md` passam a reconhecer essa exceção estritamente delimitada: baseline silencioso, diff por `InstanceId`/health/freshness, texto local sem dados externos, marca semântica por evento, baseline avançado antes da entrega e fallback legado limitado/serializado por timer monotónico.
- O publicador moderno mantém disponibilidade e mudanças em grupos separados, limita tag/grupo a 16 caracteres, usa tag única, `SetAppLogoOverride` derivado dos novos PNGs canônicos e áudio silenciado; ativação continua restrita a revelar o shell local. Aceitação pelo Windows não prova apresentação visível.
- `MN-004` continua proprietário do requisito amplo. Estado reconciliado, preferência persistida, deduplicação durável, acknowledgement, quiet policy operacional, auditoria e canais pertencem ao `STATE-06`; o Human Gate permanece pendente e nenhuma progressão foi inferida.

## 3.52.1 — 2026-07-16

- A repetição humana limitada de freshness e Tray/flyout/ativação confirmou que o refresh atualiza resumo, ícone e superfícies, mas não solicita uma notificação Windows para cada mudança de estado efetivo.
- O comentário exato de Bruno foi registrado como finding funcional bloqueante da nova amostra de `S05-HG-011`, sem alterar a decisão histórica, aprovar o Human Gate ou avançar o ciclo.
- `MN-004` permanece o requisito proprietário, com entrega factual, opt-in, supressão inicial, identificação por instância, deduplicação e quiet policy em `STATE-06`; qualquer exceção demonstrativa em `STATE-05` exige autorização e especificação explícitas.

## 3.52.0 — 2026-07-16

- O Design System `3.0.0` remove os tokens breaking `palette.provider.*` e `component.provider.*`, introduz cinco categorias ordinais derivadas somente das paletas brand/neutral e proíbe que cor categórica represente identidade de vendor, saúde, freshness, suporte ou homologação.
- O Dashboard passa a usar um único contorno genérico de banco, definido localmente e acompanhado pelo nome textual autoritativo; gráficos de distribuição Web/WPF consomem `colour.data.category.*`, com contraste automatizado e fallback para system text brushes no Windows High Contrast.
- A versão e os ativos independentes de marca permanecem em `2.6.13`: geometria, máscara alfa e cores state-bearing do sino não mudaram. O bump maior reflete exclusivamente a remoção incompatível dos antigos tokens provider-named.

## 3.51.2 — 2026-07-16

- A instrução avulsa `Prompt-Audit.md` foi consolidada no playbook canónico `Auditoria completa`, preservando o corpus em 13 arquivos ativos e sem criar uma autoridade operacional paralela.
- O playbook passa a explicitar inventário técnico, cobertura de código/configuração/dependências/segurança/desempenho, checks reais, classificação reproduzível dos achados e estrutura do relatório final.
- A auditoria continua somente diagnóstica: não autoriza correção, transição de estado nem ação externa e exige aprovação específica antes de qualquer remediação.

## 3.51.1 — 2026-07-16

- `REQ-050` foi re-auditado contra o manual Oracle MySQL Notifier 1.1 e as release notes oficiais até 1.1.8, sem reutilização de código, binário, arte, identidade, texto de produto ou arquitetura proprietária.
- A matriz clean-room passa a enumerar 25 resultados funcionais `MN-*` e quatro invariantes de qualidade `MN-Q*`, com fonte, disposição `ADOPT`/`ADAPT`/`REJECT/REPLACE`, evidência atual, fase proprietária e condição de saída.
- O requisito permanece `PARCIAL`: a reauditoria não implementa provider MySQL, integração, controle administrativo, startup, update ou packaging, não altera o Human Gate de `STATE-05` e não autoriza progressão.

## 3.51.0 — 2026-07-16

- Bruno identificou que a caption nativa permanecia branca e que o scrollbar WPF conservava o template claro durante a revisão Dark de `S05-HG-010`; o finding foi registrado como `REQ-066`, sem inferir aprovação do sample ou do Human Gate.
- O Design System `2.6.14` e `AGENTS.md` passam a exigir que caption e scrollbars WPF acompanhem Light/Dark. Em Windows 11 suportado, a caption padrão recebe background e texto semânticos por DWM depois da criação do handle e em mudanças de tema; a borda ativa/inativa permanece sob controle do Windows, Windows 10 mantém a caption nativa e scrollbars globais consomem tokens e preservam `PART_Track`, orientação e comandos de linha/página.
- Windows High Contrast continua independente: a caption devolve suas cores ao Windows e os scrollbars usam os recursos semânticos já mapeados para system brushes. Build WPF e o décimo teste de arquitetura constituem evidência automática; a comparação humana Light → Dark → Light permanece pendente e exige nova autorização.

## 3.50.1 — 2026-07-16

- Bruno respondeu exatamente `Ícone único APROVADO` após a revisão visível autorizada do Design System `2.6.13`, encerrando somente a comparação perceptual da geometria única registrada em `REQ-065`.
- O protocolo de Human Gate, a matriz de rastreabilidade, o relatório de implementação, o estado factual e o log append-only registram essa aprovação limitada. Nenhum código, ativo, contrato normativo, versão do Design System ou total de testes mudou.
- `S05-HG-010`, o Human Gate de `STATE-05` e a transição para `STATE-06` permanecem pendentes; as amostras `REQ-064/065` não precisam ser repetidas enquanto suas implementações não mudarem materialmente.

## 3.50.0 — 2026-07-15

- O Design System `2.6.13` elimina o microglifo separado de 16/20/24 px e exige que SVG, favicon, ICO Windows e superfícies Web/WPF/Tray usem uma única geometria, proporções e silhueta transparente do banco com sino. Tamanho, amostragem e cor semântica podem variar; costuras, fundo ou modelo alternativo não podem.
- A regra foi consolidada em `AGENTS.md` como orientação permanente e em `REQ-065` como requisito rastreável. O gerador usa a mesma `markGeometry`, cobertura 4× e alfa direto em todos os quadros de 16–256 px, com regressão que rejeita um segundo renderer ou grid.
- A décima sétima revisão de `S05-HG-010` confirmou humanamente, apenas em seu escopo, que a transparência e a repetição das notificações ficaram como solicitadas. A geometria divergente encontrada na mesma revisão não foi aprovada; `S05-HG-010`, o Human Gate de `STATE-05` e a transição para `STATE-06` permanecem pendentes.
- Os gates repetidos após `2.6.13` aprovaram 34 testes Dashboard, 143 unitários, 9 de arquitetura, build Release sem avisos/erros e 96 amostras headless. O publisher moderno, PNG transparente, fallback e política de `CloseRequest` não mudaram.

## 3.49.0 — 2026-07-15

- O Design System `2.6.12` introduz microglifo óptico pixel-aligned para os quadros pequenos e um PNG transparente dedicado à identidade da confirmação de disponibilidade no Windows, preservando a família semântica provider-neutral e o sino verde dessa mensagem não-state-bearing.
- O WPF tenta inicializar o Windows App Runtime e usar `AppNotificationManager` com o display name `DB Notifier`; runtime, plataforma, ativo, registro ou publicação indisponíveis falham de forma segura para o único caminho legado `ShowBalloonTip`, sem impedir a inicialização notification-area-first.
- Cada `CloseRequest` explícito solicita uma nova confirmação local. Minimizar, startup oculto, Show e Exit não solicitam notificação; aceitação pelo publicador não prova exibição, pois política do Windows e Focus Assist permanecem soberanos.
- Provisionamento do Windows App Runtime, identidade/caminho estáveis, packaging, rollback e limpeza de desinstalação continuam em `STATE-08`; notificações factuais por mudança Agent/API permanecem em `STATE-06`. `S05-HG-010` continua pendente e `S05-HG-011` permanece aprovado.

## 3.48.0 — 2026-07-15

- O Design System `2.6.11` reduz o contorno canônico do banco de `4.5` para `3.5` unidades e torna a densidade do quadro nativo de 16 px verificável: pelo menos `150/256` pixels totalmente transparentes e `90/256` com alfa `>=128`; a saída corrente possui `155` e `90`, com cobertura azul ponderada reduzida de `63.09` para `42.84` pixels equivalentes.
- Tray e fonte da notificação usam a métrica pequena nativa. Como WinForms oferece um único slot, a confirmação first-hide ocupa-o temporariamente com a variante verde até `BalloonTipShown` ou fallback de dois segundos; begin, shown, fallback, falha e descarte são governados por política fail-safe testada, sem alterar resumo, flyout ou estado persistido. O callback é melhor esforço e não prova a captura do glyph pelo Windows Shell.
- `REQ-063` e a décima quinta remediação de `S05-HG-010` registram o finding e a correção. Uma notificação nova ainda precisa de revisão humana; o Human Gate continua pendente e `S05-HG-011` permanece aprovado.

## 3.47.0 — 2026-07-15

- O Design System `2.6.10` exige que a máscara AND legada de cada quadro ICO marque exatamente os pixels BGRA totalmente transparentes, preservando o alfa direto nos pixels visíveis ou parcialmente cobertos e evitando um canvas retangular opaco em consumidores Windows de compatibilidade.
- A transparência pertence ao ativo fornecido pelo DB Notifier; o fundo do cartão, a escala/atribuição, o cache e snapshots históricos da notificação continuam controlados pelo Windows Shell e não são apresentados como superfícies transparentes do produto.
- `REQ-062` e a décima quarta remediação de `S05-HG-010` registram o finding e a correção. Uma notificação nova ainda precisa de revisão humana; o Human Gate continua pendente e `S05-HG-011` permanece aprovado.

## 3.46.0 — 2026-07-15

- O Design System `2.6.9` exige que cada notificação selecione a variante canônica pelo significado factual da própria mensagem: disponibilidade/recuperação verde, aviso amarelo, crítico vermelho profundo e informativo/desconhecido cinza; valor inválido falha de forma segura para Unknown.
- A confirmação first-hide usa essa política para disponibilidade, enquanto o ícone do Tray continua representando o agregado independente e é restaurado em `finally`. Texto visível continua obrigatório, pois cor e atribuição controlada pelo Windows não bastam para provar estado ou instância.
- `REQ-061` e a décima terceira clarificação/remediação de `S05-HG-010` registram a seleção tipada. Entrega real permanece em `STATE-06`, o Human Gate continua pendente e `S05-HG-011` permanece aprovado.

## 3.45.0 — 2026-07-15

- O Design System `2.6.8` introduz uma exceção visual limitada para a confirmação inicial não-state-bearing: durante `ShowBalloonTip`, o WPF oferece ao Windows a marca canônica transparente com sino verde fixo para comunicar somente que o aplicativo continua disponível.
- O ícone semântico agregado do Tray é restaurado em `finally`; header, favicon, janela, taskbar, flyout, NotifyIcon e futuras notificações state-bearing continuam obrigados a representar o estado factual aplicável. A apresentação final da atribuição permanece sob controle do Windows Shell.
- `REQ-060` e a décima segunda remediação de `S05-HG-010` registram a solicitação. O Human Gate continua pendente e a aprovação notification-area-first de `S05-HG-011` permanece inalterada.

## 3.44.0 — 2026-07-15

- O Design System `2.6.7` exige que header e favicon Web derivem caminho, revisão e estado da mesma função; candidatos de favicon identificados ou legados são substituídos atomicamente antes da pintura para forçar nova avaliação e reduzir o risco de uma associação antiga do Chrome representar estado diferente do agregado corrente.
- Durante a notificação inicial, o WPF fornece ao Windows um quadro nativo maior da mesma variante semântica e restaura imediatamente o quadro pequeno do NotifyIcon. Atribuição, escala, cache e entradas históricas da Central de Notificações continuam sob autoridade do Windows Shell; o aplicativo não declara controle dinâmico que a plataforma não oferece.
- `REQ-059` e a décima primeira remediação de `S05-HG-010` registram as duas exceções visuais reportadas. A revisão humana permanece pendente, estado/notificações reconciliados continuam em `STATE-06` e a aprovação de `S05-HG-011` permanece inalterada.

## 3.43.0 — 2026-07-15

- O Design System `2.6.6` exige um único contrato geométrico para SVG, favicon, ICO Windows e o cliente PowerShell funcional; todas as superfícies preservam cilindro, costuras, sino, proporções e transparência, variando somente a cor semântica do sino e a densidade de amostragem por resolução.
- Raster 16/20/24 px usa supersampling 2×, 32 px ou maior usa 4× e o BGRA armazena cor com alfa direto para evitar halos escuros/cinzentos; favicons reutilizam os payloads dos quadros Windows equivalentes em 16/20/24/32 px, o balloon WPF deixa de solicitar um glyph Info alheio à marca e hosts WPF selecionam o quadro físico pelo `DpiScale` sem upscale evitável.
- `REQ-058` e a décima remediação de `S05-HG-010` registram o finding de modelos divergentes; a comparação perceptual continua pendente e `S05-HG-011` permanece aprovado.

## 3.42.0 — 2026-07-15

- O Design System `2.6.5` exige seleção explícita do quadro ICO WPF mais próximo em vez de downsampling implícito do quadro de 128 px.
- Header/flyout usam respectivamente 40/32 px sem stretching, o NotifyIcon usa a métrica pequena do Windows e a janela recebe ícones nativos independentes para título e taskbar.
- `REQ-057` e a nona remediação de `S05-HG-010` registram o finding; a comparação perceptual continua pendente e `S05-HG-011` permanece aprovado.

## 3.41.0 — 2026-07-15

- O Design System `2.6.4` substitui a geometria multicamada por um contorno único `#0078D4` e sino/clapper semântico sólido sobre canvas transparente; Critical permanece `#C62828`.
- Entradas ICO de 16/20/24/32 px passam a usar alfa binário e pixels alinhados, enquanto o SVG ativo compartilha a mesma forma compacta; `REQ-055` registra a nova remediação visual pendente de confirmação humana.
- Metadados File Description e Product Name do WPF passam a usar `DB Notifier`, preservando o identificador técnico do assembly; `REQ-056` exige confirmação do nome efetivamente apresentado pelo shell Windows.

## 3.40.0 — 2026-07-15

- O Design System `2.6.3` substitui a simplificação insuficiente de `2.6.2` por raster pixel-alinhado em 16/20 px, cobertura reduzida em 24 px e paleta compacta até 32 px.
- A marca pequena preserva costuras do banco, usa azul Windows `#0078D4`, sino semântico sólido, transparência e Critical `#C62828`; favicon ICO próprio passa a usar a mesma família em quatro resoluções.
- `REQ-054` e a sétima remediação de `S05-HG-010` registram o finding de nitidez; a aceitação humana continua pendente e `S05-HG-011` permanece aprovado.

## 3.39.0 — 2026-07-15

- O Design System `2.6.2` adiciona keyline adaptativa `#0F2940` e peso raster mínimo para preservar a marca transparente sobre superfícies claras e em tamanhos Windows pequenos.
- ICOs de 16/20 px omitem a costura intermediária; 24 px ou mais preservam a geometria completa, sem alterar o vermelho Critical `#C62828` ou a política agregada.
- `REQ-053` e a sexta remediação de `S05-HG-010` registram a decisão; a aceitação humana continua pendente e `S05-HG-011` não foi reaberto.

## 3.38.0 — 2026-07-15

- O Design System `2.6.1` exige interior totalmente transparente no cilindro da marca em SVG e ICO, sem tile ou preenchimento específico de Light/Dark.
- O sino Critical usa vermelho profundo `#C62828` para evitar aparência alaranjada, mantendo texto/forma como evidência não dependente somente de cor.
- `REQ-052` e a quinta remediação de `S05-HG-010` registram a solicitação; a decisão visual humana continua pendente e `S05-HG-011` permanece aprovado em seu escopo notification-area-first.

## 3.37.0 — 2026-07-15

- O Design System `2.6.0` exige que header/favicon Web, header/janela/taskbar/flyout/NotifyIcon WPF usem a mesma cor Healthy/Warning/Critical/Unknown do agregado provider-neutral.
- Superfícies estáticas sem acesso a evidência usam Unknown/cinza em vez de um default verde que poderia declarar saúde inexistente.
- O finding `REQ-051` mantém `S05-HG-010` pendente de nova revisão visual; integração com estado Agent/API real continua pertencendo a `STATE-06`.

## 3.36.0 — 2026-07-14

- O Design System `2.5.0` remove o fundo azul do ícone, amplia o banco em canvas transparente e define variantes de sino Healthy/Warning/Critical/Unknown geradas pelo mesmo algoritmo clean-room.
- Pedidos de paridade funcional ampla passam a exigir matriz explícita de cobertura; adotar, adaptar e rejeitar/substituir preservam proveniência, provider neutrality, least privilege e gates de ciclo de vida.
- Todas as capacidades públicas documentadas do MySQL Notifier possuem destino na matriz, mas itens de integração, administração e release continuam pertencendo respectivamente a `STATE-06`, `STATE-07` e `STATE-08`, sem antecipar suporte ou autorização.

## 3.35.0 — 2026-07-14

- O product owner selecionou explicitamente uma implementação clean-room inspirada apenas no comportamento público documentado do MySQL Notifier, preservando a licença MIT e a identidade independente do DB Notifier.
- Código, binários, arte, logótipos, trade dress, textos de produto e arquitetura específica Oracle/MySQL não podem ser importados, traduzidos ou adaptados.
- O Design System `2.4.1` formaliza o resumo agregado provider-neutral, impede que evidência stale seja saudável e mantém entrega de notificações/ícone dinâmico dependentes da integração autorizada de `STATE-06`.

## 3.34.0 — 2026-07-14

- O cliente Windows passa a ser notification-area-first: inicialização normal mantém o shell WPF completo oculto, um clique no ícone abre o flyout primário e a janela ampla é somente um drill-down secundário.
- O padrão é inspirado conceitualmente no fluxo documentado do Oracle MySQL Notifier, sem reutilizar código, arte, identidade MySQL, WMI/DCOM, mudanças de firewall ou controle administrativo implícito.
- O Design System `2.4.0`, `S05-HG-011`, o plano de migração e a rastreabilidade separam experiência adotada, comportamentos recusados, integração `STATE-06` e homologação administrativa `STATE-07`.

## 3.33.0 — 2026-07-14

- Design System `2.2.0` formaliza Overview operacional compartilhada por Dashboard padrão, modo TV e WPF, sem criar fonte de dados paralela ou declarar telemetria externa.
- O flyout do Tray passa a usar leitura compacta em duas colunas, separando estado da frota e navegação segura; qualquer ação administrativa continua indisponível até implementação e homologação exatas.
- A matriz Dashboard cobre 72 amostras e o Lighthouse cobre cinco rotas/30 relatórios. A confirmação visual permanece no finding humano `S05-HG-010`, sem transição de estado.

## 3.32.5 — 2026-07-14

- O Design System `2.1.3` exige que bandas de resumo usem a largura operacional disponível e tenham o mesmo número de tracks e métricas visíveis em cada breakpoint.
- Regras gerais do Inventário não podem introduzir colunas vazias em resumos menores de Alertas ou outras features.
- A matriz automática passa a incluir Alertas a `960×1040`; a aprovação humana de `390 px` permanece separada das amostras pendentes de `320 px` e da nova correção intermediária.

## 3.32.4 — 2026-07-13

- O contrato responsivo do Design System `2.1.2` exige uma única coluna para coleções de Alertas e capabilities administrativas abaixo de `768` CSS px.
- Identificadores técnicos, timestamps e reason codes devem permanecer dentro do cartão, com quebra segura e sem alterar o valor textual subjacente.
- A matriz automática passa a amostrar Alertas e Configuração também no mínimo suportado de `320` CSS px; isso não substitui a confirmação visual humana.

## 3.32.3 — 2026-07-13

- A instrução de revisão visível no Chrome foi esclarecida: usar uma nova janela independente, não apenas uma nova guia na janela existente.
- Janelas e guias preexistentes permanecem intocadas; somente a janela dedicada de revisão pode ser entregue ou encerrada conforme o fluxo autorizado.
- Esta correção supersede a interpretação de “nova guia dedicada” registrada em `3.32.2` sem ocultar o histórico da instrução anterior.

## 3.32.2 — 2026-07-13

- Revisões humanas explicitamente autorizadas do Dashboard local no Chrome passam a usar uma nova guia dedicada, sem navegar ou reutilizar guias existentes do usuário.
- Guias não relacionadas permanecem intocadas; somente a guia de revisão pode ser entregue ou encerrada conforme o fluxo autorizado da amostra.

## 3.32.1 — 2026-07-13

- Todo handoff passa a terminar com orientação detalhada e acionável ao usuário: próxima ação, ordem, local, resultado esperado, restrições relevantes e evidência ou resposta necessária para continuidade.
- O template de handoff foi ampliado para impedir recomendações finais vagas e reduzir dependência de conhecimento especializado implícito.

## 3.32.0 — 2026-07-13

- Human Gates passam a exigir confirmação inequívoca de um único estado após resumo explícito de relatório, amostras, cobertura e ressalvas; respostas curtas ou agrupadas não autorizam transição.
- Contestação posterior de aprovação informada coloca a progressão em espera e exige ratificação retrospectiva independente, preservando relatórios e histórico originais.
- Template de Human Gate e auditoria retrospectiva ampliado; playbook UI/UX sincronizado com Light/Dark, migração de System retirado e High Contrast independente.
- Aprovações históricas de `STATE-00` a `STATE-04` foram contestadas pelo validador e passam a aguardar ratificação; `STATE-05`, `STATE-06` e laboratório permanecem bloqueados.

## 3.31.0 — 2026-07-13

- Regra permanente do Tray refinada para uma visão compacta e provider-neutral da frota, com verdade de fonte/freshness, navegação segura e separação explícita entre apresentação, integração e controlo homologado.
- Design System `2.1.0` adota a leitura operacional do PgNotifier como referência de experiência sem copiar arte PostgreSQL, identificadores nativos ou capabilities não comprovadas.
- Estado, plano de migração, Human Gate e evidências foram sincronizados; notificações por mudança permanecem no `STATE-06` e operações de serviço dependem de homologação exata no `STATE-07`.

## 3.30.0 — 2026-07-13

- Regra permanente de frontend alterada para preferências explícitas Light/Dark, sem opção System; valores antigos ou inválidos migram para Light e High Contrast permanece independente.
- Design System `2.0.0` substitui o globo ambíguo por símbolo de tradução/idiomas e limita o tema aos ícones Sol/Lua no React e WPF.
- `S05-HG-006`, Lifecycle, Quality Gate, estado e evidências foram sincronizados sem inferir aprovação humana ou transição de `STATE-05`.

## 3.29.1 — 2026-07-13

- Requisito de integração do modo TV atribuído explicitamente a `STATE-06`: leitura autorizada imediata da API na entrada e nova leitura não sobreposta a cada 30 segundos enquanto ativo.
- Hints autenticados do SignalR podem antecipar a leitura sem substituir a reconciliação; falhas preservam snapshot/timestamps e apresentam stale, offline ou erro factual.
- Lifecycle, Quality Gate, arquitetura, plano M6, Design System `1.4.1`, estado pendente e histórico foram sincronizados sem implementar integração em `STATE-05` ou inferir Human Gate.

## 3.29.0 — 2026-07-13

- Nova capacidade permanente de apresentação TV no Dashboard: controle único ampliar/desampliar, estado session-only, Fullscreen opcional, saída persistente e preservação de freshness/unknown/stale e verdade da fonte.
- Design System `1.4.0` define o layout TV de inventário, densidade para distância, relógio UTC e degradação segura quando Fullscreen é negado ou indisponível.
- `S05-HG-005` separa explicitamente apresentação contínua sobre dados de demonstração da futura ingestão externa realmente em tempo real de `STATE-06`; seis amostras TV automáticas foram aprovadas sem inferir aprovação humana.

## 3.28.5 — 2026-07-13

- A regra permanente de frontend passa a exigir um botão code-native de idioma e um de tema no TopBar, com estados atual/próximo localizados por nome acessível e tooltip, sem flags ou grupos permanentemente expandidos.
- Design System `1.3.5` define os ciclos `pt-BR` ↔ `en-GB` e System → Light → Dark com ícones genérico de idioma, monitor, sol e lua em React/WPF.
- `S05-HG-004` registra a solicitação visual e a reauditoria automática dos ciclos, sem inferir aprovação humana ou alterar `STATE-05`.

## 3.28.4 — 2026-07-13

- A convenção permanente distingue `DB-Notifier` em arquitetura, governança e prosa técnica, `DB Notifier` nas superfícies visuais e `DBNotifier` onde identificadores não aceitam espaços ou pontuação.
- Design System `1.3.4` remove o limite fixo da região principal em desktop/ultrawide e adiciona `1920×1080` à matriz; 66 amostras browser e a amostra WPF afetada foram aprovadas automaticamente.
- `S05-HG-003` registra a faixa inativa e o nome visual observados pelo usuário, sem inferir aprovação humana da remediação. O corpus também registra factualmente que não existe modo TV/wallboard/kiosk dedicado.

## 3.28.3 — 2026-07-13

- `S05-HG-002` registra a reprovação humana explícita do primeiro ícone canônico de banco de dados, sem reabrir a hierarquia do shell já aprovada.
- Design System `1.3.3` substitui o cilindro preenchido por uma marca mais leve em contorno branco, traços arredondados e três níveis legíveis, preservando o gerador único SVG/ICO e todas as integrações ativas.
- A substituição foi reauditada automaticamente; sua confirmação humana, teclado, Narrator e o Human Gate completo continuam pendentes.

## 3.28.2 — 2026-07-13

- A segunda remediação visual foi explicitamente aprovada pelo usuário; `S05-HG-001` está encerrado somente quanto à identidade visual, sem inferir aprovação de teclado, Narrator ou do Human Gate completo.
- Design System `1.3.2` formaliza um único ícone provider-neutral de cilindro de banco de dados em Dashboard, WPF, executável, Tray e instalador, com geração SVG/ICO determinística e verificação de drift no CI.
- Próximo checkpoint passa a ser a confirmação humana do ícone, seguida de consentimento explícito antes da continuação de `HG05-01`.

## 3.28.1 — 2026-07-13

- Design System `1.3.1` refina `S05-HG-001` após avaliação humana de melhoria parcial, sem aprovação: faixa única de KPIs, iconografia SVG coerente, navegação selecionada contida e preferências globais menos dominantes.
- Padrões oficiais Carbon UI Shell, Grafana Saga e Microsoft Fluent foram sintetizados sem copiar identidade externa nem alterar a verdade operacional do DB-Notifier.
- Segunda reauditoria afetada aprovada em 60 amostras browser e WPF Light/Dark representativos; a segunda revisão visual humana e todas as porções de Narrator continuam pendentes, sem transição de estado.

## 3.28.0 — 2026-07-13

- Design System atualizado para `1.3.0` com chrome canônico coeso nos temas Light/Dark, hierarquia empresarial refinada e seletores de preferência de baixa ênfase.
- Breakpoints de conteúdo substituem o rail comprimido por navegação horizontal rotulada e a tabela de sete colunas por cartões completos em `1100` CSS px ou menos; a matriz inclui a largura de reprovação `960×1040`.
- `S05-HG-001` implementado e reauditado automaticamente em 60 amostras browser e sete WPF; a reprovação humana original permanece registrada e exige nova validação explícita, sem transição de estado.

## 3.27.0 — 2026-07-13

- Design System atualizado para `1.2.0` com grupos de idioma e tema em botões discretos, estado selecionado acessível e posicionamento oficial na região superior direita do TopBar.
- Contrato compacto usa `pt-BR`/`en-GB` visualmente e nomes nativos completos para tecnologia assistiva; System/Light/Dark permanece localizado, persistente e operável por teclado.
- Terceiro incremento implementado e reauditoria automática bilíngue/temática aprovada; amostras humanas e Human Gate continuam pendentes, sem transição ou autorização externa.

## 3.26.0 — 2026-07-13

- Design System atualizado para `1.1.0` com contrato oficial de interface `pt-BR`/`en-GB`, `pt-BR` como fallback seguro e `LanguageSelector` equivalente em React/WPF.
- Matriz responsiva, acessibilidade e paridade ampliada para exigir os dois locales sem traduzir identificadores técnicos ou alterar fatos operacionais.
- Incremento implementado e verificado em Dashboard, Desktop e Tray; `STATE-05`, Human Gate e limites de integração externa permanecem inalterados.

## 3.25.0 — 2026-07-12

- `AGENTS.md` raiz criado como fonte operacional principal das instruções permanentes e reutilizáveis do repositório.
- Regras transversais de .NET 10 LTS, providers abertos, segurança, comentários en-GB, compatibilidade, Design System, qualidade, Docker e commits consolidadas sem substituir autoridades temáticas.
- `Start-Here.md` e índices atualizados para o novo roteamento; os 13 prompts ativos, ADRs, especificações e relatórios históricos permanecem separados por autoridade e ciclo de vida.
- Nenhum arquivo de instrução removido ou renomeado; nenhuma fase, Human Gate ou autorização externa alterada.

## 3.24.0 — 2026-07-12

- Primeiro incremento do Design System: schema e tokens core/semânticos/componentes canônicos com geração determinística CSS/XAML.
- Contratos System/Light/Dark equivalentes em TypeScript e .NET 10, com valores estáveis de persistência e falha segura para System.
- Gates de drift, paridade e contraste aprovados com 133 testes .NET e 12 Dashboard; aplicação visual React/WPF permanece pendente.

## 3.23.0 — 2026-07-12

- Design System oficial `1.0.0` formalizado antes do Human Gate com identidade moderna, limpa, profissional e sem estética sci-fi/neon.
- Arquitetura canônica de tokens e temas Light/Dark/System, persistência, componentes, WCAG 2.2 AA e paridade React/WPF especificadas.
- Implementação e nova reauditoria adicionadas ao escopo pendente de `STATE-05`; nenhuma transição ou integração externa autorizada.

## 3.22.0 — 2026-07-12

- `S05-AUD-001` remediado com contenção intrínseca do layout; todas as rotas/viewports da reauditoria passaram sem overflow global.
- `S05-AUD-002` remediado com diálogo nativo, foco inicial, Tab/Shift+Tab contidos, Escape e restauração ao acionador.
- Reauditoria automática `APROVADA` com 125 testes .NET, 8 Dashboard e 10 Pester; Human Gate, `STATE-06` e laboratório multi-banco permanecem pendentes.

## 3.21.0 — 2026-07-12

- Auditoria automática de encerramento de `STATE-05` executada com Chrome/CDP, viewports, teclado, árvore acessível, estados/reduced-motion e Windows UI Automation.
- Gate `REPROVADO` por overflow horizontal em 390/320 px e diálogo modal sem gestão de foco/Escape; inventário humano de leitor de tela permanece pendente.
- Estado preservado em `STATE-05`; Human Gate, `STATE-06` e laboratório multi-banco continuam bloqueados até remediação e reauditoria aprovada.

## 3.20.0 — 2026-07-12

- Padrão global de documentação de código em inglês britânico incorporado às regras universais e aos gates de qualidade.
- Cabeçalhos de módulo aplicados aos fontes manuais; exceções permanecem explícitas para formatos estritos, código gerado e migrations já aplicadas.
- Gate `comments:verify` integrado ao Dashboard e ao CI, sem alterar o estado `STATE-05` nem antecipar o Human Gate.

## 3.19.0 — 2026-07-12

- Quarto incremento de `STATE-05`: Tray Windows seguro para a janela DB-Notifier, sem controle de banco/serviço e com saída/descarte explícitos.
- Guards automatizados de contraste WCAG AA, semântica, foco, reduced-motion e política provider-neutral do Tray adicionados.
- 125 testes .NET e 7 testes Dashboard aprovados; próximo passo definido como auditoria automática de encerramento de `STATE-05` antes do Human Gate.

## 3.18.0 — 2026-07-12

- Terceiro incremento de `STATE-05`: contrato `configuration-capabilities.v1` e configuração/capabilities no Dashboard e WPF .NET 10.
- Previews distinguem confirmation/denied/unsupported/unavailable/unknown; nenhuma configuração, secret, mutation ou ação administrativa é executada.
- 114 testes .NET e 5 testes Dashboard aprovados; próximo incremento definido como Tray/notification area e reforço de acessibilidade.

## 3.17.0 — 2026-07-12

- Segundo incremento de `STATE-05`: contrato `history-alerts.v1`, timeline e alertas provider-neutral no Dashboard e WPF .NET 10.
- Busca/filtro de eventos, severidade por texto/símbolo, estados de alerta e manutenção implementados com adapters determinísticos e sem mutations/canais externos.
- 109 testes .NET e 4 testes Dashboard aprovados; próximo incremento definido como configuração e confirmação capability-aware sem execução administrativa.

## 3.16.0 — 2026-07-12

- Primeiro incremento de `STATE-05`: contrato de apresentação `inventory.v1` e inventário/status somente leitura no Dashboard React e Desktop WPF .NET 10.
- Estados ready/loading/empty/offline/error/stale/denied, suporte factual, timestamps, stale, foco/teclado, semântica e responsividade implementados com adapters determinísticos sem integração externa.
- 107 testes .NET e 3 testes Dashboard aprovados; próximo incremento definido como histórico/timeline e alertas provider-neutral, sem mutations ou execução administrativa.

## 3.15.0 — 2026-07-12

- Human Gate de `STATE-04 BACKEND_IMPLEMENTATION` aprovado explicitamente após revisão da reauditoria, das falhas representativas de provider, da autorização negativa e da sanitização do migrador.
- Transição factual para `STATE-05 FRONTEND_IMPLEMENTATION`, autorizando Tray/Desktop e Dashboard sem antecipar integração externa, execução administrativa ou homologação.
- Primeiro incremento de UI definido como fundação visual/contratos de apresentação e vertical slice somente leitura de inventário/status com estados operacionais e acessibilidade.

## 3.14.0 — 2026-07-12

- Oitavo incremento/remediação de `STATE-04`: migrador isolado .NET 10 com dry-run, backups, escrita atômica, relatório/manifesto, idempotência, rollback por hash e bloqueio de secrets.
- Discovery tipado de `pg_isready`, fallback de transporte, expiração de credencial e fixtures negativas implementados sem shell ou controle administrativo.
- Reauditoria automática `APROVADO` com 104 testes .NET; homologação PostgreSQL permanece `None`, suporte público `No` e Human Gate pendente.

## 3.13.0 — 2026-07-12

- Auditoria automática de encerramento de `STATE-04` executada sem transição; build/testes/segurança/dependências aprovados, gate geral `REPROVADO` por incompletude do M4.
- Bloqueadores registrados: migrador seguro PgNotifier → DB-Notifier ausente, discovery PostgreSQL caracterizado incompleto e fixtures negativas M4 insuficientes.
- Drift da matriz de capacidades registrado; próximo passo alterado para incremento de remediação seguido de nova auditoria, antes do Human Gate.

## 3.12.0 — 2026-07-12

- Sétimo incremento de `STATE-04`: polling/ack mTLS, idempotente, versionado e limitado, com inbox durável e sem executor administrativo.
- Discovery de pacotes fail-closed com manifesto `net10.0`, chave pública confiável, assinatura RSA-PSS/SHA-256, hashes, limites e rejeição de paths/links inseguros; nenhum código é carregado automaticamente.
- 86 testes .NET aprovados; nenhum `CommandAttempt`, Start/Stop/Restart, assembly externo, banco, certificado real ou provider homologado foi exercitado.

## 3.11.0 — 2026-07-12

- Sexto incremento de `STATE-04`: retenção Agent/central limitada, opt-in e dry-run por default, sem apagar audit ou dados referenciados/não publicados.
- Server-outbox e notifications com batches, backpressure, retry/backoff e IDs deduplicáveis; nenhum adapter externo registrado automaticamente.
- Consulta humana de auditoria com `audit.read` Global, filtros/paginação limitados, rate limit e auditoria do próprio acesso.
- 80 testes .NET aprovados; nenhuma deleção produtiva, canal externo, PostgreSQL, IdP ou ação administrativa foi exercitada.

## 3.10.0 — 2026-07-12

- Quinto incremento de `STATE-04`: autenticação humana OIDC/JWT externa, fail-closed e rate-limited, separada do certificado de Agent.
- RBAC server-side por usuário ativo, permission code, expiração e escopos Global/Environment/Instance; catálogo mínimo sem credentials/endpoints.
- Criação de comandos idempotentes e auditados somente em `Pending`, exigindo capability/version exatos e sem dispatch, attempt ou execução administrativa.
- 75 testes .NET aprovados; nenhum IdP, token, PostgreSQL, certificado, comando real ou provider homologado foi exercitado.

## 3.9.1 — 2026-07-12

- Auditoria de certificação do quarto incremento com 67 testes .NET aprovados e subset de sincronização 21/21.
- Respostas `2xx` inválidas, timeouts e resultados ausentes permanecem retryable e não tombstonam dados locais prematuramente.
- Cobertura ampliada para ordem monotônica, tombstone, Agent/instance scope, transições canônicas e autorização positiva/negativa.

## 3.9.0 — 2026-07-12

- Quarto incremento de `STATE-04`: outbox dispatch/ack ordenado, retry limitado e sincronização HTTPS/mTLS opt-in.
- Ingestão central idempotente com vínculo Agent/instância, sequência única, eventos canônicos e preparação de alert deliveries atômicos.
- Primeira API operacional protegida por certificado e autorização exata do Agent na rota; negação sem certificado validada em runtime local.
- 56 testes .NET aprovados após hardening de HTTPS, sequência e autorização. Nenhum PostgreSQL, certificado real, vault, alvo monitorado ou canal externo foi exercitado.

## 3.8.0 — 2026-07-11

- Terceiro incremento de `STATE-04`: initializer SQLite controlado, assignment source, worker recorrente e telemetria estruturada.
- Readers reais e read-only para Windows Credential Manager e Linux Secret Service, selecionados por composite sem fallback plaintext.
- Monitoring desabilitado por default; habilitação exige AgentId e pode migrar somente o store SQLite interno configurado.
- 46 testes .NET aprovados; nenhum banco, rede ou credential real executado e nenhum provider homologado.

## 3.7.0 — 2026-07-11

- Segundo incremento de `STATE-04`: retry/backoff limitado, ciclo isolado, credential lease/vault port e health PostgreSQL autenticado.
- Npgsql usa TLS `require`/`verify-full`, pooling desativado, query fixa e timeout; sem banco ou credencial real executados.
- Observation, sequência e outbox SQLite persistidos em uma transação, com rollback/idempotência e payload sem secrets testados.
- 40 testes .NET aprovados; scheduler recorrente, vault real, store initialization, API/RBAC/comandos e homologação permanecem pendentes.

## 3.6.0 — 2026-07-11

- Primeiro incremento de `STATE-04`: Domain/Application neutros, Provider SDK/registro aberto e probe canônico implementados.
- Adapter PostgreSQL de readiness implementado como validação concreta, sem dependência no núcleo e sem homologação pública.
- `pg_isready` executado sem shell, timeout limitado e fallback TCP sempre mapeado como `Degraded` quando alcançável.
- 30 testes aprovados e limites explícitos: sem scheduler, banco real, vault, controle administrativo, API funcional ou UI.

## 3.5.2 — 2026-07-11

- Human Gate de `STATE-03 DATABASE_MODELING` aprovado explicitamente pelo product owner, com as ressalvas técnicas preservadas.
- Ordem consolidada: roadmap universal e SAP HANA/SQLite, conectividade/credenciais e linhagem já formalizados antes do gate.
- Transição factual para `STATE-04 BACKEND_IMPLEMENTATION`, sem iniciar silenciosamente backend, migration produtiva, deploy ou ação real.
- Próximo incremento definido como núcleo provider-neutral e vertical slice PostgreSQL em .NET 10.

## 3.5.1 — 2026-07-11

- Linhagem histórica registrada pelo product owner: MySQL Notifier inspirou conceitualmente o PgNotifier, sucedido pelo DB-Notifier.
- Inspiração separada explicitamente de código reutilizado, compatibilidade técnica, afiliação ou dependência.
- Estado preservado em `STATE-03`; nenhuma mudança de implementação ou gate.

## 3.5.0 — 2026-07-11

- Cobertura de monitoramento formalizada para bancos locais, remotos, Windows, Linux, containers, datacenter, híbridos e cloud.
- Conexão provider-specific atribuída ao Agent, sem transformar API/Dashboard em proxy genérico de banco.
- Identidades separadas para monitoramento, administração do banco, serviço do sistema operacional e plano de controle cloud.
- Cofres Linux e identidade federada/workload cloud incluídos, sem autorizar segredo em claro ou abertura automática de rede.

## 3.4.0 — 2026-07-11

- Objetivo do produto ampliado explicitamente para aceitar qualquer motor de banco por provider/plugin versionado.
- Priorização inicial dos bancos mais utilizados e conhecidos, incluindo SAP HANA e SQLite como futuros alvos monitoráveis.
- Catálogo declarado aberto a engines relacionais, NoSQL, distribuídas, embarcadas, especializadas, cloud-managed e futuras.
- Separação preservada entre presença no roadmap, implementação, homologação e suporte público; estado mantido em `STATE-03`.

## 3.3.0 — 2026-07-11

- Registro factual da modelagem interna SQLite/PostgreSQL concluída e auditada em `STATE-03`.
- Separação explícita dos assemblies de persistência do Agent e Server, sem provider cruzado nos runtimes.
- Inclusão de modelo, constraints, índices, retenção, migrations e recuperação como evidências; nenhuma migration produtiva autorizada.
- Human Gate de `STATE-03` mantido pendente antes de qualquer transição para backend.

## 3.2.2 — 2026-07-11

- Aceitação explícita de ADR-0001 a ADR-0006 e do pacote arquitetural completo.
- Encerramento do Human Gate de `STATE-02 ARCHITECTURE`.
- Transição factual para `STATE-03 DATABASE_MODELING`, sem autorizar migrations produtivas ou backend.

## 3.2.1 — 2026-07-11

- Aceitação explícita do ADR-0001 com baseline única .NET 10 LTS.
- Atualização da visão, arquitetura e estado para proibir targets ativos anteriores sem novo ADR.
- Retarget dos 10 projetos e renovação de restore, build, testes, format, auditoria e liveness em .NET 10.
- Preservação das menções .NET 8 somente como histórico factual supersedido.

## 3.2.0 — 2026-07-11

- Proposta do pacote arquitetural completo de `STATE-02` com seis ADRs.
- Definição de contratos canônicos, protocolo Agent/API, threat model e matriz PostgreSQL.
- Inclusão de guardrails de dados, risco, avaliações e promoção de modos para MOD-12 AIOPS_AI.
- Atualização factual do estado sem pré-aprovar ADRs nem avançar para modelagem.

## 3.1.6 — 2026-07-11

- Registro da aprovação explícita do Human Gate de Project Setup.
- Transição factual de `STATE-01 PROJECT_SETUP` para `STATE-02 ARCHITECTURE`.
- Atualização do próximo gate e dos entregáveis arquiteturais pendentes, sem antecipar implementação.

## 3.1.5 — 2026-07-11

- Registro do commit inicial e da migração canônica PgNotifier → DB-Notifier.
- Preservação explícita de shims, configuração antiga e rollback side-by-side.
- Inclusão do build canônico e das evidências de compatibilidade, sem promover o estado.

## 3.1.4 — 2026-07-11

- Registro da instalação autorizada do SDK .NET 8 isolado no workspace.
- Atualização factual após restore, build, testes, format, auditoria NuGet e liveness aprovados.
- Auditoria automática de `STATE-01` aprovada; Human Gate mantido pendente.

## 3.1.3 — 2026-07-11

- Atualização factual de `STATE-01` após criação do scaffold modular e da CI inicial.
- Registro dos checks aprovados de Dashboard/legado e do bloqueio por ausência do SDK .NET 8.
- Estado preservado em `STATE-01`, com Human Gate de saída pendente.

## 3.1.2 — 2026-07-11

- Registro da aprovação explícita do Human Gate de descoberta.
- Transição factual de `STATE-00 DISCOVERY_MIGRATION` para `STATE-01 PROJECT_SETUP`.
- Registro da autorização para inicialização de Git e das limitações de ambiente ainda existentes.

## 3.1.1 — 2026-07-11

- Atualização factual de `Current-State.md` após inventário e plano incremental do legado.
- Registro do fechamento técnico da descoberta, sem transição e com Human Gate pendente.
- Referência às evidências de migração mantidas em `docs/` sem ampliar o corpus ativo.

## 3.1.0 — 2026-07-11

- Inclusão de `MOD-12 AIOPS_AI` como capacidade de longo prazo.
- Definição de coleta, regras, estatística, correlação, conhecimento, LLM, planejamento, aprovação, executor e feedback.
- Inclusão de governança de modelos, avaliações, prompt injection, modos operacionais e automação baseada em risco.
- Integração do módulo ao índice, arquitetura, lifecycle, segurança e estado corrente.

## 3.0.0 — 2026-07-11

- Consolidação de 77 arquivos em 12 documentos ativos.
- Redução de contexto, segurança e critérios repetidos.
- Unificação das oito fases em `Lifecycle.md`.
- Unificação de auditorias automáticas e Human Gates em `Quality-Gates.md`.
- Separação preservada entre estado corrente e log append-only.
- Criação de documentos temáticos para governança, segurança, playbooks e templates.
- Atualização integral das referências e da hierarquia.

## 2.0.0 — 2026-07-11

- Migração conceitual de 75 prompts herdados para DB-Notifier.
- Remoção do domínio e das evidências do projeto de origem.
- Criação de `STATE-00 DISCOVERY_MIGRATION`.
- Adaptação das fases e módulos para Agent, API, Tray/Desktop, Dashboard e Provider SDK.

## Registro da instrução de migração

O antigo `Prompt-Adapt-Doc.md` foi uma instrução temporária: ler a visão do novo projeto, revisar 75 prompts herdados, remover dependências do projeto anterior, padronizar o conteúdo e emitir relatório. A tarefa foi concluída na versão 2.0.0 e, por isso, não permanece como instrução ativa.

## Auditoria da versão 3.1.0

Critérios obrigatórios:

- Exatamente 13 arquivos Markdown em `prompts/`.
- Todos os links Markdown internos resolvem.
- `Start-Here.md` roteia para todos os documentos.
- Estado e histórico permanecem separados.
- Nenhum nome retirado é usado como dependência ativa.
- Documentos não contêm trailing whitespace.
- Termos do projeto anterior aparecem somente em registro histórico, se necessário.

Auditoria documental não comprova implementação do produto.
