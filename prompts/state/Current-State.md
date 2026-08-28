# Estado Atual

Este documento é o snapshot factual vigente do workspace em 2026-08-28. Ele
não concede autoridade de execução. A evolução, os resultados substituídos e
as decisões tomadas no seu contexto original permanecem no
[`State-Transition-Log.md`](State-Transition-Log.md) e nos relatórios
proprietários.

## Lifecycle, gates e ativação

- Posição do workspace: `STATE-06 INTEGRATION`.
- Nenhuma transição para `STATE-07 TESTING_HOMOLOGATION` foi executada ou
  autorizada.
- A cadeia anterior permanece válida: `STATE-00` e `STATE-04` foram
  retrospectivamente `APROVADOS`; `STATE-01` a `STATE-03`, `APROVADOS COM
  RESSALVAS`; e o Human Gate de `STATE-05`, `APROVADO` por decisão
  substitutiva. As fontes proprietárias são a
  [ratificação retrospectiva](../../docs/Human-Gate-Retrospective-Ratification.md)
  e o
  [Human Gate de `STATE-05`](../../docs/STATE-05-Human-Gate-Validation.md).
- O resultado registrado do Human Gate final de `STATE-06` permanece
  `APROVADO COM RESSALVAS`, limitado à baseline revista em 2026-07-20. Os
  incrementos posteriores possuem gates próprios, não alteram o lifecycle por
  si mesmos e não autorizam inferir prontidão para transição. A evidência
  proprietária é o
  [relatório do Human Gate](../../docs/STATE-06-Final-Human-Gate-Report.md).
- O
  [ADR-0007](../../docs/architecture/ADR-0007-AIOps-Trust-Distribution-And-Resource-Admission.md)
  está `accepted` somente como decisão arquitetural. A
  [proposta histórica `STATE-06 → STATE-07`](../../docs/STATE-06-To-STATE-07-Transition-Proposal.md)
  permanece `INVALIDADA PARA EXECUÇÃO`. A
  [nova proposta reconciliada](../../docs/STATE-06-To-STATE-07-Transition-Revalidation-Proposal.md)
  foi revisada para incorporar a diretriz estratégica de concluir os escopos
  de integração `STATE-06` de MOD-12 e JOSE antes da revalidação consolidada
  de saída. A delimitação detalhada `PC-M12`/`PC-JOSE` foi aceita em
  2026-07-29 exclusivamente como diretriz documental de fechamento da
  integração `STATE-06`. A proposta continua não autorizante, com todos os
  itens técnicos pendentes de autoridades próprias e elegibilidade `NÃO
  REAVALIADA`.
- O estado de ativação de MOD-12 é `ActivationState=None`. `OBSERVER`,
  `ADVISOR`, `ASSISTANT` e `CONTROLLED_AUTOMATION` permanecem inativos.
- O gate de ativação `None → Observer` e uma transição de lifecycle são
  decisões independentes. Nenhuma delas pode ser inferida da outra.
- A pré-condição estratégica não exige AIOps ou JOSE operacionalmente
  completos antes de `STATE-07`: exige somente os escopos integrados,
  bounded, verificáveis e fail-closed pertencentes ao `STATE-06`. MOD-12 O5,
  homologação JOSE, ativação, suporte público e release conservam fases e
  decisões próprias.

## Sistema de instruções e coordenação

- O corpus vigente é `8.0.0`, com 16 arquivos ativos. A
  [Política de Idioma](../governance/Language-Policy.md), revisão `1.0.0`, é a
  autoridade temática única para comunicação com o proprietário, idioma dos
  artefatos, preservação de conteúdo existente, convenções externas e
  separação do idioma da interface.
- A
  [Coordenação de Conversas e Trabalho Paralelo Seguro](../governance/Conversation-Coordination-Prompt.md),
  revisão `1.4.1`, é a autoridade temática de roteamento, handoff,
  recomendação de raciocínio do Codex, paralelismo, ownership exclusivo e
  integração coordenada. As duas
  autoridades ficam em `prompts/governance/`; `Governance.md` conserva
  autoridade, execução controlada e lifecycle.
- O
  [relatório `GOV-MN-RESTORE-01`](../../docs/STATE-06-MySQL-Notifier-Functional-Reference-Restoration-Report.md)
  registra a intenção corrigida do proprietário: MySQL Notifier 1.1.8 volta a
  ser referência funcional e comportamental para aperfeiçoar DB-Notifier.
  `REQ-048`, `REQ-050`, `MN-001`–`MN-025` e `MN-Q01`–`MN-Q04` recuperam suas
  disposições e saídas incrementais. `GOV-MN-REV-01` e seu commit permanecem
  história factual, mas sua proibição prospectiva está supersedida. A
  autoridade permite somente resultados funcionais observáveis, sanitizados e
  não expressivos, não uma base de implementação ou mandato de clone. Analista
  exposto à fonte entrega apenas proveniência e especificação comportamental
  sanitizada; autores correspondentes de implementação/testes permanecem não
  expostos e uma revisão independente de proveniência/similaridade precede a
  integração. Código, binário, ativo, texto, trade dress ou arquitetura interna
  Oracle/MySQL não entram no projeto MIT sem proveniência/direitos documentados,
  modelo de distribuição compatível, revisão jurídica especializada e decisão
  separada do proprietário. O possível provider independente para o banco
  MySQL conserva autoridade separada.
- Comunicação com o proprietário usa `pt-BR`. Novos artefatos independentes
  pertencentes ao projeto usam `en-GB`; alterações limitadas preservam o
  idioma estabelecido de cada arquivo; convenções externas permanecem
  inalteradas. Nenhuma migração linguística geral ou alteração de locale da
  interface está autorizada.
- Todo handoff, concluído, parcial ou bloqueado, fornece uma única mensagem
  completa, específica, preenchida, em `pt-BR` e pronta para copiar e enviar na
  conversa indicada. Seu valor aparece sozinho em exatamente um bloco de código
  Markdown cercado e rotulado `text`, com rótulo e explicações fora da caixa.
  Todo outro payload apresentado expressamente para copiar, como título
  sugerido, mensagem de lane ou mensagem de retorno, usa um bloco separado.
  `Exact next message` não aceita `None`, placeholder ou alternativas; quando
  não houver ação adicional de projeto, a mensagem confirma ou encerra com
  segurança e declara não autorizar nova ação. Uma mensagem pronta não constitui
  decisão antes de ser enviada nem presume aprovação, Human Gate, ADR,
  `ActivationState`, lifecycle, operação Git ou ação externa.
- Todo handoff preserva os 14 campos existentes e começa `Your action now` com
  exatamente uma recomendação para a próxima interação: `Leve` (`low`),
  `Médio` (`medium`), `Alto` (`high`), `Extra alto` (`xhigh`), `Máximo`
  (`max`) ou `Ultra` (`ultra`), acompanhada de uma razão específica. A escolha
  usa o menor esforço suficiente, é reavaliada por conversa e lane e não
  comprova disponibilidade, seleção ou aplicação. Os planos paralelos indicam
  um nível para a coordenadora e para cada lane; mensagens auxiliares repetem a
  orientação numa frase de preâmbulo não canônica antes dos 19 campos
  preservados, sem criar campo adicional.
- Uma única conversa coordenadora conserva escopo, baseline e integração e
  mantém sob sua custódia estado, histórico, changelog, ADRs, relatórios e
  decisões de gate e apresentação de Human Gates. Essa custódia não transfere
  a decisão humana ou arquitetural nem concede autoridade de lifecycle,
  ativação, operação Git ou ação externa.
- Nenhum workflow de escrita paralela com branches e worktrees isolados está
  autorizado neste snapshot. Conversas simultâneas permanecem read-only e toda
  escrita ocorre sequencialmente na coordenadora; a existência de Git e a
  autorização do commit local final não ampliam esse limite.
- O `PLANS.md` raiz é o ledger vivo não autorizante para trabalho amplo,
  transversal, multi-incremento ou auditoria com remediação. Ele registra
  baseline, autoridade, escopos, trabalho protegido, ownership, DoR/DoD,
  findings, incrementos, evidências, blockers e outcome; estado, histórico,
  ADRs e gates conservam seus owners temáticos.
- O envelope fechado usa as topologias `SAFE_PARALLEL`,
  `CONTRACT_FROZEN_PARALLEL`, `SINGLE_OWNER` ou `SEQUENTIAL_ONLY`, classes de
  artefato e stop codes canônicos. O handoff continua com exatamente 14
  campos e com seus enums anteriores de roteamento e paralelismo.
- `scripts/development.ps1` é o entry point local canônico com `Doctor`,
  `Setup`, `Quick`, `Full`, `-Offline` e `-PlanOnly`. `Quick` é `NON_GATE`;
  `Full` delega uma vez a `scripts/ci.ps1`. A workflow GitHub usa o mesmo
  agregador para o gate Windows e uma execução Dashboard suplementar em Linux.
  Offline conserva advisories online como `NOT_RUN` e resultado completo como
  `PARTIAL`. Tanto o entry point local quanto a chamada direta do gate criam
  processo-filho sem shell e removem da cópia privada do ambiente, sem ler os
  valores, activators, conexões/configurações DB-Notifier, flags de sandbox,
  overrides ASP.NET e credenciais de provider herdados. O shell chamador não é
  alterado e campanhas físicas continuam exigindo seus runners e autoridades
  próprias.
- A adoção desta governança não altera `STATE-06 INTEGRATION`, elegibilidade,
  `MOD-12 ActivationState=None`, ADRs, Human Gates, produto, interface,
  runtime ou autoridade externa.

## Baseline técnica

- A adoção do fluxo de desenvolvimento começou em
  `main@f0f220c539fde685e2c500b4944ebca168aaec7d`, com zero processo ou
  listener DB-Notifier no preflight. A mudança é limitada a método,
  governança, tooling de desenvolvimento, CI e documentação; não revalida a
  baseline executável de produto descrita abaixo. A árvore externa local
  `mysql-notifier-1.1.8-src/` foi lida estaticamente durante
  `GOV-MN-REV-01`; nenhum conteúdo foi executado, incorporado ou rastreado.
  `GOV-MN-RESTORE-01` acrescentou uma lane separada e read-only que amostrou
  licença/avisos e metadados representativos de source, projeto e manifesto de
  recursos para classificar proveniência e risco de ativos. Nenhum arquivo-alvo
  foi copiado ou modificado, e nenhum extract atravessou para artefato,
  implementação, teste ou commit. A árvore permanece excluída do inventário Git
  pelos padrões de `.gitignore`. Qualquer inspeção futura exige autoridade
  explícita separada; autores de implementação/testes recebem somente registro
  de proveniência e especificação comportamental sanitizados e aprovados, nunca
  source, binários, ativos, material decompilado ou notas brutas/não sanitizadas
  derivadas da fonte.
- A última árvore executável inventariada é
  `9512dc1de15619eadd9d2e8e6b5476bb77a13abd`, de 2026-07-28. Ela estava na
  branch `main`, com worktree limpa, e contém como ancestrais `84217c6`,
  `2c1e05f`, `1a27dca`, `96cf248`, `ff0adc7` e `3c13d57`. A baseline
  administrativa da primeira reconciliação é
  `04db6594e192dec822fbd326c792eec4f3a37714`, descendente direto que alterou
  somente os quatro documentos da reconciliação. A revisão estratégica
  aceita somente como diretriz documental está em
  `db62377e5d762e7ee334e7bf0e7309a0ab5c4b9b`, descendente documental direto
  de `04db659`. Nenhuma dessas referências constitui a futura baseline técnica
  depois dos fechamentos MOD-12/JOSE.
- Entre a baseline examinada pelo Human Gate final `1a27dca` e `9512dc1`
  existem `143` commits, `440` caminhos alterados, `90.922` inserções e
  `2.993` remoções. O intervalo inclui `126` caminhos em `src/`, `127` em
  `tests/`, `25` em `scripts/`, três mudanças de schema Server, lotes R0–R8,
  R-SEQ/R-EGRESS/R-FENCE/R-NET, MOD-12 O1–D8, tooling, SDK, dependências e
  governança. Os gates próprios desses lotes não se agregam automaticamente
  como um novo gate de lifecycle.
- Entre a reconciliação anterior `ff0adc7` e `9512dc1` existem `24` commits e
  `41` caminhos, sem mudança em `src/`, migration ou package/lockfile. O
  intervalo contém, porém, `11` caminhos C# test-only, atualização de
  `global.json` para .NET SDK `10.0.302`, evolução dos harnesses D6–D8,
  JOSE-0 e mudanças de governança. Essa ausência focal de source não restaura
  a elegibilidade invalidada.
- A solução contém 19 projetos .NET 10, com targets `net10.0` ou
  `net10.0-windows10.0.22621.0`; os hosts de sandbox em `tests/` não pertencem
  à composição normal. O Dashboard usa React e TypeScript; o cliente Windows
  usa WPF.
- No início da validação local de 2026-08-27, o host não possuía
  simultaneamente os pins exatos `.NET SDK 10.0.302`, `Node.js 24.18.0` e
  `npm 11.16.0`: foram observados SDKs locais `8.0.422`/`10.0.301`, SDKs de
  sistema `10.0.303`/`10.0.400`, Node.js `24.19.0` e npm `11.17.0`.
- Sob autoridade posterior específica, os arquivos oficiais win-x64 tiveram
  SHA-512/SHA-256 conferidos contra os catálogos oficiais e foram extraídos de
  forma isolada sob `.dotnet/toolchains/`. Os executáveis reportaram exatamente
  .NET SDK `10.0.302`, Node.js `24.18.0` e npm `11.16.0`; `global.json`,
  `.nvmrc`, `package.json` e lockfiles permaneceram inalterados. A remoção do
  staging ignorado `.dotnet/provisioning-i6/` foi rejeitada pelo limite local
  de execução antes da criação do processo, deixando somente os dois arquivos
  oficiais verificados (`334721515` bytes). O stop-on-failure exigido encerrou
  o lote como `BLOCKED`/`ISOLATION_FAILURE` antes de novo `Doctor`, `Quick` ou
  `Full`.
- A autoridade mais recente substituiu os pins exatos de toolchain por faixas
  estáveis compatíveis para tolerar atualizações automáticas do host. O
  contrato versionado em implementação é `.NET SDK >=10.0.302 <10.1.0` com
  `latestFeature` e sem prerelease, `Node.js >=24.18.0 <25.0.0` e
  `npm >=11.16.0 <12.0.0`; a CI solicita o SDK GA corrente da linha .NET 10.0,
  enquanto `.nvmrc` e `check-latest` selecionam a linha Node 24 corrente. O
  grafo, as versões e as integridades de
  dependência do lockfile permanecem inalterados; somente sua metadata raiz de
  engines acompanha as faixas.
- O shutdown preflight I7 aprovou com zero processo correspondente e zero
  listener próprio. A primeira execução de `scripts/development.ps1 Doctor`
  encerrou com exit code `1`: raiz do repositório, lockfiles e dependências
  restauradas aprovaram, mas a política de toolchain falhou ao desserializar a
  chave raiz de nome vazio de `package-lock.json` sem `-AsHashtable`. O
  stop-on-failure preservou esse primeiro resultado sem correção nem repetição;
  `Quick` e a única execução online de `Full` ficaram `NOT_RUN`. O preflight de
  encerramento aprovou novamente com zero processo correspondente e zero
  listener próprio.
- Sob a autoridade corretiva I7-R1, a descoberta somente leitura identificou
  `scripts/assert-dbnotifier-shutdown.ps1` e sua única execução inicial
  aprovou com zero processo correspondente e zero listener próprio. A
  desserialização do lockfile passou a usar hashtable e o leitor obrigatório
  passou a aceitar a chave vazia sem deixar de recusar sua ausência. Os hashes
  de `global.json`, Dashboard `package.json` e `package-lock.json` permaneceram
  idênticos; a regressão focal aprovou `94` assertions e o novo `Doctor`
  aprovou preflight interno, raiz, toolchains, lockfiles e dependências.
- A primeira execução I7-R1 de `Quick` encerrou com exit code `1`. O build
  Release aprovou com zero aviso e zero erro, os testes unitários aprovaram
  `528/528`, e os testes de arquitetura aprovaram `99/100`; a falha única foi
  `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`,
  que não encontrou a substring esperada `state06-consolidated-e2e:`. O
  stop-on-failure encerrou o lote sem diagnóstico, correção ou repetição;
  `Full` online ficou `NOT_RUN` e sua única execução autorizada não foi
  consumida.
- O lote corretivo `AUD-2026-R1` partiu da baseline limpa
  `main@3762f71c116af206b911a086b836cef11cd1894d`, após preflight com zero
  processo correspondente e zero listener próprio. Ele materializou somente a
  exceção estreita da árvore-fonte `DBNotifier.Persistence.Agent.Sqlite`, o
  inventário Markdown delimitado pelo Git, suas regressões focais e a
  atualização do teste arquitetural para a topologia CI consolidada. Workflow,
  versões, dependências, lockfiles e contratos externos não mudaram.
- A revisão independente final do candidato `AUD-2026-R1` encerrou
  estaticamente com `P0=0`, `P1=0`, `P2=0` e `P3=0`. Na primeira execução, as
  regressões Git-boundary aprovaram `2/2`, o verificador Markdown aprovou `981`
  links em `226` arquivos do corpus Git não ignorado, e o teste arquitetural
  focal aprovou `1/1`. O `Doctor` também aprovou com preflight interno, raiz,
  toolchains, lockfiles e dependências restauradas válidos.
- A primeira execução `AUD-2026-R1` de `Quick`, explicitamente `NON_GATE`,
  encerrou com exit code `1`. Antes da falha, o build Release aprovou com zero
  aviso e zero erro; testes unitários, arquiteturais e Node aprovaram
  respectivamente `528/528`, `100/100` e `74/74`; verificações de assets,
  tipos, documentação e Markdown também passaram. O verificador de fluxo então
  rejeitou o `PLANS.md` pela ausência da chave literal obrigatória
  `- Initial baseline:`.
- O stop-on-failure de `AUD-2026-R1` preservou esse primeiro resultado sem
  correção, repetição ou execução alternativa. O `Full` online permaneceu
  `NOT_RUN`, sua autorização de uma execução não foi consumida e nenhum gate
  canônico agregado foi produzido ou inferido. O preflight de encerramento
  aprovou com zero processo correspondente e zero listener próprio. A árvore
  externa protegida permaneceu não lida; nenhum resíduo ignorado foi excluído.
- A continuação corretiva `AUD-2026-R1-R1` partiu da baseline limpa
  `main@b60ef4d302e4c4dc3f0e474be27eaa4b8c6beb13`, após shutdown preflight com
  zero processo correspondente e zero listener próprio. A única implementação
  renomeou no control record de `PLANS.md` a chave `- Frozen baseline:` para
  `- Initial baseline:`, preservando o valor original
  `main@3762f71c116af206b911a086b836cef11cd1894d`. Nenhum arquivo de
  implementação/teste, workflow, versão, manifesto, lockfile, dependência ou
  contrato externo mudou.
- O check focal `AUD-2026-R1-R1` aprovou `105` assertions; `Doctor` aprovou
  preflight interno, raiz, toolchains, lockfiles e dependências restauradas; e
  a primeira `Quick`, explicitamente `NON_GATE`, aprovou build Release com zero
  aviso/erro, testes unitários `528/528`, arquitetura `100/100`, Node `74/74`,
  política `105`, regressões de política `94`, runner `68` e sintaxe `13`, além
  dos checks auxiliares aplicáveis.
- A única execução online de `Full` em `AUD-2026-R1-R1` encerrou com exit code
  `1` e `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`. Antes da falha,
  aprovaram secret scan, política, restore locked de `19` projetos, build com
  zero aviso/erro, arquitetura `100/100`, WPF `10/10`, unitários `528/528`,
  integração `168/168`, cobertura de linhas `83,41%` e branches `56,62%` em
  `10` componentes obrigatórios, vulnerabilidades NuGet em `19` projetos e o
  runtime audit fail-closed sem inicialização de persistência local.
- A falha factual do `Full` ocorreu na compatibilidade legada: o processo-filho
  não resolveu o SDK .NET `10.0.302` exigido enquanto inventariava target
  frameworks de `DBNotifier.Domain.csproj`; a suite registrou `Failed=1` e
  `Pending=0`. Não houve retry, diagnóstico executável, correção ou ambiente
  alternativo. O shutdown final aprovou com zero processo correspondente e
  zero listener próprio. Material protegido permaneceu não lido e nenhum
  resíduo ignorado foi excluído.
- A primeira tentativa `AUD-2026-R1-R3` parou antes da baseline e da edição:
  seu shutdown preflight encontrou `pwsh.exe` no PID `8952` e retornou
  `BLOCKED`/`ISOLATION_FAILURE`. Na recuperação separadamente autorizada
  `AUD-2026-R1-R3-R1`, o PID já estava ausente e nenhum processo foi encerrado.
  O único novo preflight aprovou com zero processo/listener, e branch, HEAD e
  árvore rastreada corresponderam à baseline limpa
  `main@6ecc72f7a347a746d0153072580c527d7c679e81`.
- `AUD-2026-R1-R3-R1` propagou o caminho absoluto do executável `dotnet` já
  validado por `ci.ps1`, pelo runner Windows PowerShell, pelos parâmetros do
  Pester e pelas sete chamadas legadas do verificador de vulnerabilidades. Não
  alterou `development.ps1`, o próprio verificador, `global.json`, workflow,
  produto, versões, manifests, lockfiles, dependências ou contratos externos.
- O check focal aprovou `105` assertions. O runner legado focal aprovou `34`
  testes, aceitou `1` skip condicional e atingiu cobertura de `35,17%`
  (`338/961`). `Doctor` aprovou preflight interno, raiz, toolchains, lockfiles e
  dependências restauradas. `Quick`, explicitamente `NON_GATE`, aprovou build
  sem aviso/erro, unitários `528/528`, arquitetura `100/100`, Node `74/74`,
  política `105`, regressões de política `98`, runner `68` e sintaxe `13`.
- A única execução online de `Full` em `AUD-2026-R1-R3-R1` comprovou a correção
  no ponto canônico: a compatibilidade legada aprovou `34` testes, `1` skip e
  cobertura `35,17%`, seguida por validação do bundle. Antes disso, também
  aprovaram os dois preflights, secret scan, políticas, restore locked de `19`
  projetos, build sem aviso/erro, arquitetura `100/100`, WPF `10/10`, unitários
  `528/528`, integração `168/168`, cobertura de linhas `83,41%`, branches
  `56,62%` em `10` componentes, vulnerabilidades NuGet em `19` projetos e o
  runtime audit fail-closed. Dashboard assets, tipos, documentação, Markdown,
  `74/74` testes Node e o build de produção também passaram.
- O mesmo `Full` encerrou depois com exit code `1` e
  `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`: o `npm audit` reportou uma
  vulnerabilidade de severidade alta em `nanoid <3.3.18`, identificada por
  `GHSA-2v37-7h3g-55p8`. Não houve retry, alteração de dependência/lockfile,
  diagnóstico executável ou correção em linha. O shutdown de encerramento
  aprovou com zero processo correspondente e zero listener próprio.
- `AUD-2026-R1-R3-R1` permanece `BLOCKED`; os passes parciais e a correção
  comprovada não convertem o gate agregado em `PASS`. Material externo
  protegido permaneceu não lido, nenhum resíduo ignorado foi excluído e
  backend, migrations, banco/provider real, navegador comum, deploy, push,
  Human Gate, ativação e transição de `STATE` não foram executados.
- `AUD-2026-R1-R4` partiu da baseline limpa
  `main@0f59408440dc1c5877f8d3de0de8859ef9bc7fed` após um shutdown preflight
  com zero processo correspondente e zero listener próprio. As toolchains
  observadas — .NET `10.0.400`, Node.js `24.19.0` e npm `11.17.0` — satisfazem
  as faixas estáveis governadas; nenhum pin ou intervalo mudou.
- npm atualizou em modo package-lock-only somente os campos `version`,
  `resolved` e `integrity` do nó transitivo `nanoid`, de `3.3.16` para a versão
  corrigida compatível `3.3.18`. O `package.json` Dashboard permaneceu
  byte-idêntico, com SHA-256
  `5a137255c337ab1a159e797dd7187bcfdbcdb70ca75c73c0cf43faf5f3a917a8`, e a
  aresta `postcss -> nanoid ^3.3.16` não mudou.
- A auditoria focal online retornou `found 0 vulnerabilities`. `Doctor`
  aprovou preflight interno, raiz, toolchains, lockfiles e dependências
  restauradas. A única `Quick`, explicitamente `NON_GATE`, aprovou build sem
  aviso/erro, unitários `528/528`, arquitetura `100/100`, Node `74/74`,
  política `105`, regressões de política `98`, runner `68` e sintaxe `13`.
- A única execução online de `Full` em `AUD-2026-R1-R4` aprovou os dois
  preflights, secret scan, políticas, restore locked de `19` projetos, build
  sem aviso/erro, arquitetura `100/100`, WPF `10/10`, unitários `528/528`,
  integração `168/168`, cobertura de linhas `83,41%`, branches `56,62%` em
  `10` componentes, vulnerabilidades NuGet nos `19` projetos, runtime audit
  fail-closed, compatibilidade legada com `34` testes e `1` skip, bundle,
  `74/74` testes Node, build Dashboard e o audit npm com zero vulnerabilidades.
  O audit isolado do Dashboard também aprovou `128` amostras de viewport, `96`
  de forced colours e `24` focais de zoom/reflow.
- O mesmo `Full` encerrou depois com exit code `1` e
  `DISPOSITION|FAIL|stage=All|stop=GATE_FAILURE`: em
  `scripts/run-state06-consolidated-e2e.ps1:254`, PowerShell informou que a
  propriedade `marker` não existe em um objeto candidato. Não houve retry,
  diagnóstico executável, correção em linha ou segunda execução. O shutdown
  de encerramento aprovou com zero processo correspondente e zero listener
  próprio.
- `AUD-2026-R1-R4` permanece `BLOCKED`; a correção do advisory está comprovada
  neste lote, mas passes parciais não convertem o gate agregado em `PASS`.
  Material externo protegido permaneceu não lido, nenhum resíduo ignorado foi
  excluído e banco/provider real, navegador comum, deploy, push, Human Gate,
  ativação e transição de `STATE` não foram executados.
- `AUD-2026-R1-R5-R1` partiu da baseline limpa
  `main@34e5f3358491a1eb52b508c0170d6a9ac3169bc4`, após seu único shutdown
  preflight inicial aprovar com zero processo correspondente e zero listener
  próprio. O plano foi atualizado antes da implementação.
- O candidato substitui o acesso direto `$candidate.marker` por lookup
  protegido em `PSObject.Properties['marker']`, guarda nulo e tipo string e usa
  `-ceq` para admitir somente o marker canônico exato. A regressão focal exige
  esse lookup e proíbe o acesso direto. A revisão estática confirmou zero
  alteração no host, no literal canônico, nos runners adjacentes, nas faixas,
  dependências, lockfiles ou integridades.
- A única invocação do teste focal compilou o candidato e reportou um arquivo de
  teste correspondente, mas o canal de execução não reteve o veredicto final nem o
  exit code. A recuperação somente leitura da mesma execução não encontrou
  processo correspondente nem TRX/log durável. O resultado não é inferido como
  `PASS` nem como falha do teste e a execução não foi repetida.
- `AUD-2026-R1-R5-R1` está `BLOCKED` por `ISOLATION_FAILURE`. `Doctor`, `Quick`
  e o `Full` online condicional estão `NOT_RUN`; o candidato possui apenas
  evidência estática. Banco/provider real, navegador comum, deploy, push, Human
  Gate, ativação e transição de `STATE` não foram executados.
- `AUD-2026-R1-R5-R2` partiu da baseline limpa
  `main@dcd3d24064e0709981e8ba0cff223de6a2c35565`. Seu único shutdown preflight
  inicial aprovou com exit code `0`, zero processo correspondente e zero
  listener próprio; branch, HEAD e a árvore não ignorada corresponderam à
  baseline autorizada.
- O runner consolidado e o teste focal permaneceram byte-idênticos. A primeira
  tentativa de iniciar o invólucro de captura durável foi rejeitada pelo limite
  local de execução antes de `CreateProcess`; nenhum processo PowerShell ou de
  teste foi criado e o diretório ignorado de evidência permaneceu ausente. A
  invocação não foi corrigida, simplificada ou repetida.
- `AUD-2026-R1-R5-R2` está `BLOCKED` por `ISOLATION_FAILURE`. O teste focal,
  `Doctor`, `Quick` e o `Full` online condicional estão `NOT_RUN`; nenhuma dessas
  autorizações foi consumida. Banco/provider real, navegador comum, deploy,
  push, Human Gate, ativação e transição de `STATE` não foram executados.
- A evidência executável disponível para o próprio fluxo aprovou `97`
  invariantes estáticas, `64` regressões determinísticas, sintaxe de `47`
  scripts PowerShell e `14` scripts Node, compatibilidade de sintaxe de `27`
  scripts Windows PowerShell com `20` skips declarados, `68` assertions do
  runner e `13` assertions de sintaxe. Documentação (`437` fontes), Markdown
  (`977` links em `227` arquivos), validação de bundle, scan de segredos no
  worktree e histórico disponíveis e integridade Git também aprovaram. Isso
  não substitui build, testes de produto, cobertura, runtime ou gate canônico.
- As tentativas diagnósticas do gate canônico Dashboard offline encerraram
  `BLOCKED` por `DEPENDENCY_UNREADY`; nenhuma produziu ou foi convertida em
  `PASS`. A execução mais recente, posterior ao hardening de isolamento,
  aprovou preflight e scan completo de segredos antes de observar Node.js
  `24.19.0` diante do pin `24.18.0`. Depois do provisionamento exato, a
  sequência I6 não iniciou `Doctor`, `Quick` ou `Full` devido ao bloqueio de
  cleanup. A continuação I7 iniciou um novo `Doctor`, que falhou no parser do
  lockfile; I7-R1 corrigiu e validou somente esse defeito, mas seu `Quick`
  falhou no teste de arquitetura descrito acima. `AUD-2026-R1` corrigiu e
  validou focalmente a topologia atual, mas sua própria `Quick` falhou no
  controle do plano descrito acima. `AUD-2026-R1-R1` corrigiu esse controle e
  aprovou `Quick`, cobertura, advisories NuGet e o runtime audit fail-closed,
  mas seu único `Full All` falhou na compatibilidade legada antes dos estágios
  posteriores. `AUD-2026-R1-R3-R1` corrigiu essa propagação e comprovou a
  compatibilidade legada no `Full`, mas o mesmo gate falhou depois no audit de
  dependências Dashboard. `AUD-2026-R1-R4` comprovou a correção desse advisory
  até dentro do `Full`, que falhou mais tarde no runner E2E consolidado por uma
  propriedade `marker` ausente. O gate agregado permanece sem `PASS` neste
  snapshot.
- Domain e Application permanecem provider-neutral. Provider SDK,
  infraestrutura, persistência, Agent, API, Desktop, Dashboard e testes
  conservam fronteiras próprias e dependências voltadas para dentro.
- Agent usa SQLite somente para estado local autorizado; Server usa PostgreSQL
  somente para persistência central. As 15 migrations atuais são seis do Agent
  SQLite e nove do Server PostgreSQL. Nenhuma migration está aplicada a
  PostgreSQL existente ou operacional.
- O provider PostgreSQL implementa endpoint tipado, discovery de
  `pg_isready`, readiness, fallback TCP degradado, probe Npgsql autenticado e
  normalização canônica. PostgreSQL permanece `Homologation=None` e suporte
  público `No`, conforme a
  [matriz de capabilities](../../docs/architecture/Provider-Capability-Matrix.md).
- O ConfigMigrator permanece em .NET 10, com dry-run, rejeição de secrets e
  campos desconhecidos, backup, journal durável, recuperação determinística,
  rollback e revalidação.
- Os lotes R0, R0-F1, R1, R2-A, R3, R4-A, R4-B, R5, R6, R7-A0 e
  R8 permanecem encerrados somente nos respetivos escopos locais. R5 está
  tecnicamente aprovado, mas sua conformidade com a autoridade original
  permanece `REPROVADA` pelo incidente NuGet não reclassificado. Os Human
  Gates R6 e R8 permanecem `APROVADOS COM RESSALVAS`: as cinco condições
  físicas R6, locked restore, freshness online de advisories, CI remota e a
  repetição PostgreSQL própria de R8 continuam não testados naquele escopo;
  quatro achados R8 permanecem classificados como `CONTIDO`. Laboratórios
  focais posteriores não substituem essas evidências, e os resultados não
  constituem homologação, runtime operacional ou autorização externa. As
  fontes são os relatórios
  [R5](../../docs/STATE-06-Audit-Remediation-R5-Report.md),
  [Human Gate R6](../../docs/STATE-06-Audit-Remediation-R6-Human-Gate-Report.md),
  [R8](../../docs/STATE-06-Audit-Remediation-R8-Report.md) e
  [Human Gate R8](../../docs/STATE-06-Audit-Remediation-R8-Human-Gate-Report.md).
- R-SEQ está automaticamente `APROVADO` somente no escopo local validado. A
  nona migration Server estabelece um corte inclusivo por Agent e o ledger
  `rejected_observation_sequences`, sem criar amostra ou efeito de saúde para
  a rejeição consumida. Posições históricas não são reconstruídas; evidência
  pós-corte ausente ou contraditória falha fechada como retryable, e o Agent
  somente reconhece a rejeição quando o high-water autoritativo cobre sua
  sequência. O laboratório PostgreSQL descartável passou, mas nenhuma
  migration foi aplicada a PostgreSQL existente ou operacional. As fontes são
  o
  [relatório R-SEQ original](../../docs/STATE-06-R-SEQ-Rejected-Observation-Sequence-Remediation-Report.md)
  e o
  [relatório do ledger durável](../../docs/STATE-06-R-SEQ-Durable-Rejection-Ledger-Report.md).
- R-EGRESS foi aceito somente para a corrida entre leitura `Active`, revogação
  principal e commit da ingestão; R-FENCE está automaticamente `APROVADO`
  somente como regressão local do mesmo gate por Agent. A observação que perde
  essa ordem não cria efeito próprio, embora R-SEQ ainda possa projetar uma
  sucessora aceita antes da revogação e retida atrás do gap recusado. A
  campanha PostgreSQL descartável multiprocesso passou, mas não concede
  fairness, prioridade, starvation freedom, timeout próprio, SLO, homologação
  ou suporte operacional. R-EGRESS não designa política de network egress;
  essa fronteira pertence a R-NET. As fontes são os relatórios
  [R-EGRESS](../../docs/STATE-06-R-EGRESS-Observation-Ingestion-Revocation-Linearisation-Report.md),
  [R-FENCE](../../docs/STATE-06-R-FENCE-Agent-Identity-Fence-Lifecycle-Regression-Report.md)
  e da
  [campanha física](../../docs/STATE-06-R-EGRESS-R-FENCE-PostgreSql-Multiprocess-Load-Report.md).
- R-NET está automaticamente `APROVADO` somente no escopo local validado.
  Quatro políticas positivas por consumidor exigem CIDR e porta exatos,
  recusam atomicamente respostas DNS proibidas, conectam somente a IP aprovado
  preservando o hostname TLS e mantêm o legado PowerShell estritamente
  loopback. A campanha local controlada de DNS, PKI, IdP e PostgreSQL TLS
  passou; os construtores produtivos permanecem em trust `System` e o trust
  sintético continua estritamente test-only. DNS/PKI/IdP/PostgreSQL
  operacionais, DNSSEC, trust provisionado, proxy, failover, HA,
  cross-platform, desempenho, provider homologado e suporte público continuam
  não homologados. As fontes são o
  [relatório determinístico R-NET](../../docs/STATE-06-R-NET-Network-Egress-Remediation-Report.md)
  e o
  [relatório da campanha física](../../docs/STATE-06-R-NET-Local-DNS-PKI-IdP-PostgreSql-TLS-Homologation-Report.md).

## Composição normal e limites operacionais

- Monitoring e sincronização continuam desabilitados por padrão. Agent Fleet,
  command polling e notification delivery normais permanecem indisponíveis ou
  recusam startup quando uma configuração incompleta tenta habilitá-los.
- As rotas normais de comando são tombstones fail-closed. Não existe
  `CommandAttempt`, executor administrativo, post-probe ou fallback de
  execução. Start, Stop e Restart permanecem `Unsupported`.
- A API conserva contratos locais de ingestão, identidade, RBAC, auditoria,
  catálogo e sandboxes explicitamente guardados. Não há IdP, PKI, certificado,
  chave, vault, canal de notificação, provider registry operacional, database
  target ou infraestrutura real configurada.
- A
  [proposta de capacidade JOSE completa](../../docs/STATE-06-JOSE-Complete-Capability-Proposal.md)
  e o
  [ADR-0008](../../docs/architecture/ADR-0008-JOSE-Cryptographic-Profiles-And-Key-Lifecycle.md)
  estão revistos como proposta `1.2.0`/ADR revision `1.2`. O ADR permanece
  `proposed`. O lote exclusivamente documental `JOSE-0` foi autorizado e
  produziu o
  [pacote de Architecture, Security and Coverage Design](../../docs/STATE-06-JOSE-0-Architecture-Security-And-Coverage-Design-Report.md):
  cinco profiles e safety caps provisórios, mapas de
  ownership/trust/data/egress, `JOSE-T01`–`JOSE-T17`,
  `JOSE-REQ-001`–`JOSE-REQ-012`, planos de teste/migração/rollback e
  classificação `318/318` das entradas IANA JOSE/JWT Claims, com zero
  `Unreviewed`. Os gates documentais automáticos e os rechecks independentes
  passaram, com zero P0/P1 residual. O proprietário revisou e aceitou o pacote
  exclusivamente como preparação documental. Profiles, caps e decisões
  permanecem provisórios, não normativos, `NotImplemented`, `NotHomologated`,
  `RuntimeDisabled` e `NotAdvertised`; a decisão não aceita a proposta ou
  ADR-0008. `JOSE-1`, `JOSE-D1`, todos os lotes de implementação e todos os
  decision packets permanecem não autorizados ou fora do escopo. Não há JWE,
  emissão JWS própria, ciclo de chaves operacional, IdP, vault/KMS/HSM ou
  capacidade JOSE no runtime além do relying-party JWT/JWKS já descrito.
- A diretriz estratégica de 2026-07-29 tornou o futuro fechamento
  `JOSE STATE-06 INTEGRATION SCOPE COMPLETE` pré-condição da revalidação de
  saída. O escopo proposto percorre `JOSE-1`, `JOSE-D1` e os lotes
  `JOSE-2/3/4/5` aplicáveis, todos sob decisões separadas; não antecipa
  `JOSE-6`/homologação de `STATE-07`, `JOSE-7`/release de `STATE-08` ou
  qualquer autoridade executiva. A delimitação `PC-JOSE-01`–`05` foi aceita
  somente como direção documental; `JOSE-1`, `JOSE-D1` e os lotes posteriores
  continuam não autorizados.
- Não há loader dinâmico nem carregamento ou distribuição operacional de
  provider packages, instalador, assinatura, update channel ou entrega
  operacional. A referência funcional de `REQ-047` está restaurada;
  `REQ-048` e `REQ-050` estão `ATIVOS COM LIMITES`; os 29 registros
  `MN-*`/`MN-Q*` recuperam suas disposições e saídas incrementais. Os lotes
  `S06-DFR-01`/`02` continuam fatos locais concluídos e evidenciam somente seus
  recortes provider-neutral. O possível provider independente para o banco
  MySQL permanece não implementado e não homologado.

## Interfaces atuais

- O contrato normativo vigente é o
  [Design System `3.4.2`](../../docs/design/DB-Notifier-Design-System.md), com
  `pt-BR` e `en-GB`, temas Light e Dark e Windows High Contrast como override
  independente.
- Dashboard Web e WPF expõem oito destinos comuns. O Web é responsivo a partir
  de 320 CSS px e não existe cliente móvel nativo. O WPF é
  notification-area-first: flyout como superfície primária e shell completo
  como destino secundário; ele não possui modo TV.
- As superfícies normais apresentam dados locais determinísticos e
  identificados como demonstração. Existe agora um contrato injetável
  Agent/API para o Tray, validado somente em composição HTTPS local isolada;
  ele permanece indisponível no startup comum porque o Desktop ainda não possui
  fluxo humano próprio de token. Nenhuma composição normal usa provider, banco,
  identidade ou canal externo real.
- O Tray normal adquire esse inventário local através de um contrato
  provider-neutral de snapshot e uma reconciliação Application-owned
  single-flight. Inicialização, atualização manual e timer serial de 30 segundos
  produzem um frame coerente para ícone, tooltip, flyout e inventário WPF;
  leituras offline, negadas, incompatíveis ou falhas retêm a última evidência
  aceita enquanto a freshness continua envelhecendo. O endpoint humano
  `/api/v1/desktop/fleet-snapshot` e seu adapter HTTPS bounded integram as
  observações mais recentes recebidas dos Agents, filtradas por `instances.read`,
  ao mesmo contrato; o startup comum continua compondo somente o adapter
  demonstrativo local. Provider real, identidade Desktop, descoberta e ações
  administrativas não foram ativados.
- O lote `S06-DFR-02` foi validado por regressões focais `10/10`, endpoint HTTPS
  isolado `3/3`, arquitetura `2/2` e build WPF sem avisos. `Doctor` e `Quick`
  aprovaram; um único `Full` canônico concluiu
  `DISPOSITION|PASS|stage=All`, com unitários `543/543`, arquitetura `101/101`,
  integração `171/171`, WPF `10/10` e cobertura `83,39%`/`56,46%`. A evidência
  usa apenas processos e dados locais e não ativa identidade, provider ou
  lifecycle.
- No sandbox TV autorizado, a entrada lê a API imediatamente e a reconciliação
  seguinte ocorre de forma serial 30 segundos após a conclusão da leitura
  anterior. SignalR é apenas um hint autenticado para antecipar uma nova
  leitura; não substitui polling nem prova tempo real. O modo é session-only,
  Fullscreen é opcional e a saída, a freshness e a verdade da fonte permanecem
  visíveis.
- O registro visual atual contém onze identidades e 22 variantes, com fallback
  neutro universal. Identidade visual não implica implementação, homologação
  ou suporte. Web, WPF, Tray, taskbar, favicon e notificações derivam da mesma
  geometria canônica da marca.
- Delivery autoritativo de notificações, ações administrativas operacionais e
  dados de provider real continuam ausentes. Scaling Windows físico a 200%,
  mixed-DPI entre monitores e as demais condições de `R6-HV-P01` permanecem
  sem evidência física.
- A inicialização automática do WPF no logon ainda não existe; a inicialização
  notification-area-first aplica-se somente quando o processo é iniciado.

## MOD-12 e gate `None → Observer`

- A fundação MOD-12 permanece local, in-memory, provider-neutral e inativa. A
  composição normal contém somente o control plane Observer dormente e uma
  autoridade de ativação indisponível; não contém pipeline operacional,
  hosted service, publisher, corpus operacional, LLM, recomendação, plano,
  executor ou automação.
- O1, O2-A, O2-B, O3-A e O3-B estão automática e humanamente aprovados somente
  nos seus sandboxes locais, sintéticos e test-only. O4 está automaticamente
  `APROVADO`, com Human Gate `APROVADO COM RESSALVAS`, somente como
  projeção/API/UI factual, read-only e sintética; forecast operacional,
  representatividade de produção e acessibilidade/DPI físicos não foram
  comprovados.
- O Quality Gate O5 permanece `BLOQUEADO` e nunca foi convertido em Human Gate
  O5. O5-R1 foi aprovado com ressalvas somente como definição documental de
  uma célula candidata; O5-R2 e O5-R3 foram aprovados somente nos seus escopos
  inativo e sintético. O5-R4 foi aprovado sem achados críticos ou altos, mas
  preserva três lacunas médias: owner operacional nominal, autoridade/custódia
  de ativação e continuidade/reconciliação independente.
- O5-R5-A está automática e humanamente `APROVADO` somente como prontidão e
  runner test-only. O5-R5-B está apenas automaticamente `APROVADO` como driver
  test-only; não possui Human Gate próprio. Nenhum deles aprova a campanha. A
  campanha física pós-D3 mais recente foi `REPROVADA` em
  `Cancellation/Cold`, repetição 13, com working set de `2.916.352 bytes`
  contra o limite inclusivo de `786.432 bytes`. Ela parou com `158/560`
  amostras e `4/16` resumos: `HM-01 BLOQUEADO`, `HM-02 REPROVADO` e
  `HM-03 NÃO TESTADO`.
- A metodologia vigente é V3. A correção bounded da alocação gerenciada de
  `Cancellation/Cold` permanece aceita no escopo test-only, mas a campanha
  física posterior continuou reprovada e não comprovou a causa do working set
  residual.
- D4 permanece tecnicamente `BLOQUEADO` e humanamente `ACEITO COMO BLOQUEADO`.
  Seu gate de lockfiles executou internamente um restore bloqueado fora da
  autoridade; dependências e lockfiles não mudaram, mas uma consulta de
  metadados não pode ser descartada. O incidente não foi reclassificado como
  autorizado.
- O resultado temporal mais recente é
  [PF-OBS-1-D5 `BLOQUEADO`](../../docs/STATE-06-MOD-12-PF-OBS-1-D5-Process-History-Diagnostic-Report.md)
  e
  [humanamente `ACEITO COMO BLOQUEADO`](../../docs/STATE-06-MOD-12-PF-OBS-1-D5-Human-Gate-Report.md).
  Duas repetições do prefixo V3 exato e duas de cada controle preservaram
  `704/704` amostras, mas não reproduziram o excesso histórico; nenhuma causa
  foi atribuída e nenhuma correção especulativa foi aplicada.
- O rebaseline pós-formatação
  [PF-OBS-1-D6 está `BLOQUEADO`](../../docs/STATE-06-MOD-12-PF-OBS-1-D6-Post-Format-Non-Intrusive-Rebaseline-Report.md).
  O defeito anterior do writer foi corrigido e coberto por regressão. Na
  retomada explicitamente autorizada, o primeiro processo parou em `35/158`
  amostras e `1/4` resumos: `FirstByte/Cold` excedeu o coeficiente de
  repetibilidade V3 (`0,22923232701994542` contra `0,2`). `Cancellation/Cold`
  não foi alcançado, a segunda execução não começou e nenhum braço externo ou
  controle D5 foi executado. O resultado não reproduz nem refuta o excesso de
  working set.
- O diagnóstico
  [PF-OBS-1-D7 está `BLOQUEADO`](../../docs/STATE-06-MOD-12-PF-OBS-1-D7-FirstByte-Cold-Repeatability-Diagnostic-Report.md).
  Três processos novos preservaram `35/35` amostras e o objeto de resumo V3;
  os coeficientes `0,024460739425192193`, `0,010868664937096893` e
  `0,011026690622714398` ficaram abaixo do limite inalterado de `0,2`.
  Porém, os JSONs omitiram os campos explícitos de contagem de resumo exigidos
  pelo contrato. A implementação e o gate estático foram corrigidos sem
  alterar ou substituir evidência, mas as três execuções são
  contractualmente incompletas: a classificação D7-U não foi admitida e o
  braço externo permaneceu proibido. Nenhuma causa foi atribuída.
- O lote corretivo
  [PF-OBS-1-D7-R1 foi concluído como `D7-R1.NOT_REPRODUCED`](../../docs/STATE-06-MOD-12-PF-OBS-1-D7-R1-Summary-Count-Contract-Rerun-Report.md)
  e o seu
  [Human Gate foi `APROVADO COM RESSALVAS`](../../docs/STATE-06-MOD-12-PF-OBS-1-D7-R1-Human-Gate-Report.md).
  Três envelopes R1-U admissíveis preservaram `35/35` amostras e `1/1`
  resumo; os coeficientes `0,02148452314834982`, `0,0096634992433091` e
  `0,012714703648847817` ficaram abaixo do limite V3 inalterado de `0,2`.
  Com `0/3` falhas, R1-E permaneceu proibido. Os três relatórios D7 históricos
  continuam byte a byte, bloqueados e excluídos da classificação R1. O
  resultado e a aceitação humana comprovam não recorrência somente no lote
  R1; nenhuma amostra humana adicional estava prevista, nenhuma causa foi
  atribuída e nenhum gate adjacente foi aprovado.
- O diagnóstico
  [PF-OBS-1-D8 foi concluído como `D8.EARLY_GATE_INTERMITTENT`](../../docs/STATE-06-MOD-12-PF-OBS-1-D8-Exact-Prefix-Completion-Reconciliation-Report.md).
  Seu
  [Human Gate foi `APROVADO COM RESSALVAS`](../../docs/STATE-06-MOD-12-PF-OBS-1-D8-Human-Gate-Report.md),
  aceitando somente essa classificação automática.
  As duas tentativas novas foram admissíveis e não observadas. A primeira
  reteve um target stop exato em `Cancellation/Cold`, measured repetition 5,
  com `150/158` amostras, `4/4` resumos e working set de `2.797.568 bytes`
  contra o limite de `786.432 bytes`. A segunda parou no resumo
  `FirstByte/Cold`, com `35/158` amostras, `1/4` resumos e coeficiente
  `0,20075118542700426` contra `0,2`. A árvore D8 não entrou numa
  classificação de recorrência do working set porque somente `1/2`
  tentativas foi target-eligible. Nenhuma causa foi atribuída; observação
  externa, controles D5 e HM-01–HM-03 permaneceram proibidos. A aceitação
  humana não aprovou PF-OBS-1, O5, Observer, `ActivationState` ou lifecycle.
- PF-OBS-1 e O5 permanecem sem aprovação. A causa do pico físico residual,
  corpus e ambiente piloto representativos, calibração operacional,
  homologação exata da célula e owners materiais continuam pendentes.
- A diretriz estratégica de 2026-07-29 tornou o futuro fechamento
  `MOD-12 STATE-06 INTEGRATION SCOPE COMPLETE` pré-condição da revalidação de
  saída. A delimitação `PC-M12-01`–`04`, aceita somente como direção
  documental, exige no futuro revalidar O1–O4 num único boundary
  product-owned, com adapters/activation guard reais exercidos por harness
  sintético, sem implementação test-only paralela e com zero worker/I/O em
  `ActivationState=None`. Nenhum lote foi autorizado. PF-OBS, O5 e D9 não são
  reclassificados nem executados: permanecem no handoff de
  homologação/ativação de `STATE-07`, com D9 opcional e dependente de
  autorização própria.
- O
  [plano documental de fechamento MOD-12](../../docs/STATE-06-MOD-12-Integration-Closure-Plan.md)
  foi preparado em 2026-07-29 sobre a baseline administrativa `2a59548` e
  está `PENDENTE DE REVISÃO`. Ele ordena o trabalho futuro como
  `PC-M12-02 → PC-M12-03 → PC-M12-01 → PC-M12-04` e o decompõe em
  `M12-IC1`–`M12-IC6`. Nenhum desses lotes, gate ou amostra humana está
  autorizado; o plano não altera qualquer resultado técnico.

## Decisões que exigem nova autoridade

- Cada incremento que implemente uma saída `MN-*`/`MN-Q*` ainda exige envelope,
  baseline, escopo, checks e autoridade técnica próprios; a matriz ativa não
  autoriza implementação em massa, provider, runtime ou transição de lifecycle.
- Copiar, traduzir, adaptar, linkar ou redistribuir código-fonte, binários,
  ativos, texto, trade dress ou arquitetura interna Oracle/MySQL exige decisão
  explícita e separada de licenciamento/distribuição, proveniência exata do
  componente/rightsholder, direitos ou permissões aplicáveis documentados,
  modelo de distribuição compatível e revisão jurídica especializada;
  `GOV-MN-RESTORE-01` não concede essa autoridade e a decisão do proprietário
  não relicencia esse material Oracle/MySQL ou de terceiros.
- Qualquer novo diagnóstico, mudança metodológica ou campanha física requer
  autorização explícita e separada. `D9` não foi autorizado nem transformado
  em pré-condição automática.
- O plano `M12-IC1`–`M12-IC6` está somente preparado para revisão documental.
  Seu eventual aceite não autorizará código, build, testes, runtime, amostras
  humanas ou classificação de fechamento; cada autoridade permanece
  separada.
- `JOSE-1`, `JOSE-D1`, aceitação do ADR-0008 e qualquer código, dependência,
  migration, login/IdP, chave, custodiante, egress candidate, infraestrutura,
  homologação ou profile JOSE operacional exigem decisões explícitas e
  separadas. A direção documental aceita torna o fechamento integrado
  `JOSE-1`–`JOSE-5` aplicável uma pré-condição futura, mas nem o aceite da
  delimitação nem o aceite humano limitado de `JOSE-0` concedem qualquer
  dessas autoridades.
- Qualquer ativação `None → Observer` exige os gates próprios e uma decisão
  explícita. O fechamento sintético MOD-12 no `STATE-06` não aprova O5, e o
  bloqueio de ativação não pode ser contornado por mudança de lifecycle.
- Qualquer transição para `STATE-07` exige decisão de lifecycle própria. A
  estratégia vigente exige antes dela os fechamentos de integração STATE-06
  de MOD-12 e JOSE, a revalidação consolidada da baseline resultante, novas
  amostras humanas e um Human Gate de revalidação. Cada item exige autoridade
  própria. O Human Gate de 2026-07-20 e os gates posteriores permanecem
  históricos nos seus escopos; a proposta antiga está invalidada, e a nova
  não concede autoridade executiva.
- Produção, PostgreSQL operacional, provider homologado, runtime externo,
  publicação, deploy e ação administrativa real continuam não autorizados.
