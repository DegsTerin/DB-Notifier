# Proposta STATE-06 — Remediação do Quality Gate Consolidado

## Status e autoridade

- Data: 2026-07-19.
- Estado mantido: `STATE-06 INTEGRATION`.
- Baseline documental: commit `fe479421bb4da936d3e4e48179d78bc916497f68`.
- Quality Gate consolidado corrente: `BLOQUEADO`.
- Autoridade desta atividade: elaborar exclusivamente esta proposta documental.
- Implementação, build, testes de produto, runtime, navegador, acesso externo, promoção e transição: não autorizados e não executados.

Esta proposta não corrige o bloqueio. Ela delimita uma remediação futura que somente poderá começar depois de uma autorização separada e explícita de Bruno.

## Atualização factual posterior

Bruno autorizou separadamente a implementação restrita em 2026-07-19. A remediação foi concluída localmente no commit `ac12791`, está descrita no [relatório factual da remediação](STATE-06-Consolidated-E2E-Evidence-Harness-And-Deterministic-Gate-Remediation-Report.md) e foi aceita por Bruno com as limitações registradas. Essa aceitação não repete nem aprova a Campanha Consolidada, não autoriza amostra humana, não abre o Human Gate final e não promove nem transiciona o estado.

## Resultado em linguagem simples

As quatro partes já construídas passaram isoladamente, mas seus testes não contam uma única história do começo ao fim. Cada laboratório cria seus próprios dados, identidade ou armazenamento. Por isso, a campanha anterior não pôde provar que a mesma observação criada pelo Agent chegou à API, apareceu no Dashboard, originou uma notificação e permaneceu protegida por revogação e por um transporte de comando incapaz de executar ações.

A remediação proposta criará um único laboratório automatizado exclusivamente de teste. Ele usará um identificador de execução, um Agent sintético, uma instância sintética, um SQLite temporário do Agent e um SQLite temporário do Server. O Dashboard e o consumidor de notificação lerão a mesma projeção produzida por esse Agent. O transporte de comando usará a mesma identidade de Agent, mas continuará deliberadamente incapaz de executar qualquer coisa.

Separação de identidades continuará obrigatória: o Agent usará certificado de teste; o Dashboard e a leitura de notificações usarão uma identidade humana sintética, diferente e somente read-only. “Uma execução correlacionada” não significa reutilizar a mesma credencial para responsabilidades diferentes.

A proposta também trata dois problemas menores encontrados pela campanha:

1. duas consultas do Entity Framework receberão uma ordenação estável antes do limite, eliminando warnings sem mudar schema ou regra de autorização;
2. os runners modernos de browser declararão PowerShell 7 como requisito e recusarão PowerShell 5.1 antes de criar qualquer recurso. O runner legado continuará separado em Windows PowerShell 5.1.

Depois dessa eventual remediação, a Campanha Consolidada precisará ser executada novamente sob outra autorização. A conclusão da remediação não aprovará o Quality Gate automaticamente.

## Baseline factual

A [campanha consolidada](STATE-06-Consolidated-Quality-Gate-Campaign-Report.md) observou:

- build Release dos `16` projetos sem erro ou warning;
- `332/332` testes unitários, `25/25` de arquitetura, `14/14` de integração e `60/60` do Dashboard;
- pipeline autoritativo, SignalR/browser, notificação e comando não executável aprovados em harnesses próprios;
- browser host alimentado por `BrowserSignalRSnapshotSource`, baseado em fixture própria;
- notificação alimentada por outro `NotificationSandbox` e outro Server SQLite;
- pipeline e comando alimentados por instâncias independentes de `AgentFleetSandbox`, `AgentFileSandbox` e identidades;
- dois warnings EF `10102` em consultas limitadas sem ordenação;
- falha de cleanup sob Windows PowerShell 5.1 devido à sobrecarga moderna de `String.Contains`, seguida de sucesso integral sob PowerShell `7.6.3`;
- zero model drift e zero resíduo pertencente à campanha ao final.

O achado alto é uma lacuna de evidência, não uma falha funcional reproduzida. Os dois outros achados são de baixa gravidade e ferramenta.

## Objetivo da remediação futura

Produzir os artefatos exclusivamente locais e de teste necessários para que uma nova campanha possa observar uma única sequência correlacionada dos quatro incrementos aceitos, eliminar os warnings EF conhecidos e tornar explícito o contrato de PowerShell dos runners modernos, sem ativar composição normal ou capacidade operacional.

## Decisões propostas

| Tema | Decisão documental proposta |
|---|---|
| Composição | um novo host/orquestrador E2E dedicado, pertencente somente a `tests/` |
| Autoridade de dados | uma única persistência Server SQLite temporária alimenta snapshot, SignalR e transições de notificação |
| Estado Agent | uma única persistência Agent SQLite temporária preserva registration, assignment, outbox, replay e journal entre processos |
| Identidade Agent | um único Agent e certificado exclusivamente de teste durante toda a execução |
| Identidade humana | identidade de teste separada, somente read-only, para Dashboard e notificação |
| Browser | Chrome ou Edge dedicado, perfil efêmero e mesma origem HTTPS loopback |
| SignalR | somente hint; cada hint provoca no máximo uma nova leitura autoritativa da API |
| Notificação | `RecordingSink` de teste; nenhuma publicação Windows ou canal externo |
| Comando | fixture `sandbox.command.*`, política `Never` e zero `CommandAttempt` |
| Evidência | ledger sanitizado correlacionado por um `runId`, sem segredo ou certificado completo |
| PowerShell moderno | requisito mínimo explícito PowerShell 7; Windows PowerShell 5.1 permanece apenas no runner legado |
| EF | ordenação determinística por chaves estáveis antes dos dois `Take` conhecidos |
| Produção | nenhuma alteração na composição normal, provider, banco ou credencial operacional |

## Arquitetura proposta

```text
fonte sintética
      |
Agent de teste -- Agent SQLite/outbox/journal
      |                     |
      | HTTPS/mTLS          | mesma identidade de Agent
      v                     v
host consolidado -- Server SQLite único -- transporte de comando Never
      |              |              |
      |              |              +-- revogação/fencing
      |              +-- transições commitadas -- consumidor -- RecordingSink
      +-- snapshot autoritativo -- API/ETag -- Dashboard no browser
                            |
                            +-- SignalR hint; nunca fonte de dados
```

### Um único orquestrador de teste

O incremento futuro deverá criar um executável de teste dedicado, sugerido como `DBNotifier.State06.ConsolidatedSandboxHost`. Ele deverá:

- existir somente sob `tests/` e nunca ser referenciado por um projeto de `src/`;
- exigir um marcador de ativação exato, por exemplo `--state06-consolidated-e2e-sandbox`;
- recusar ambiente diferente do sandbox exato, endereço não loopback, material não sintético ou diretório temporário fora da raiz de ownership;
- compor classes já existentes de enrollment, assignments, observação, snapshot, SignalR, notificação e comando, sem copiar sua lógica de produto;
- possuir uma única factory de `ServerDbContext` apontada para um arquivo SQLite temporário;
- possuir um único diretório Agent SQLite temporário, reutilizado nos reinícios controlados do processo Agent;
- servir o Dashboard construído e a API na mesma origem HTTPS loopback;
- iniciar apenas filhos locais atribuíveis e registrar PID, porta, diretório e ownership para cleanup;
- expor somente controles e evidências de teste sob um prefixo reservado, autenticados e indisponíveis na composição normal;
- terminar com código diferente de zero se qualquer processo, listener, profile ou store próprio permanecer.

O host é infraestrutura de teste, não um novo serviço do produto. Ele não deverá ser instalado, empacotado, publicado ou habilitado por configuração normal.

### Estado compartilhado e correlação

Uma execução deverá criar exatamente:

- um `runId` público e aleatório, usado apenas para correlação sanitizada;
- um `agentId`, um `instanceId` e uma registration de teste;
- um certificado de Agent efêmero e uma identidade humana read-only separada;
- um Server SQLite e um Agent SQLite sob a raiz temporária exclusiva;
- um ledger de notificação e um perfil de browser sob a mesma raiz de ownership;
- uma sequência monotónica de observações e uma sequência monotónica de mensagens de comando.

O ledger de evidência deverá registrar somente IDs, hashes, sequências, revisões, disposições, timestamps UTC e contagens. Token de enrollment, chave privada, PKCS#12, senha, certificado completo, header de autenticação e payload secreto nunca deverão ser gravados ou apresentados.

### Projeção autoritativa compartilhada

O browser host futuro não deverá usar `BrowserSignalRSnapshotSource` no cenário consolidado. O snapshot deverá vir de `DashboardTvSyntheticObservationSnapshotSource`, alimentado pelo mesmo `ServerDbContext` que recebeu a observação do Agent.

Depois de uma observação commitada, o orquestrador poderá publicar somente a revisão opaca pelo publisher SignalR já existente. O browser continuará obrigado a reler o endpoint de snapshot. A evidência deverá provar:

- leitura imediata ao entrar no modo TV;
- ETag/`304` quando o corpo não mudou;
- hint coalescido seguido por releitura;
- concorrência HTTP máxima `1`;
- reconciliação periódica iniciada 30 segundos após a conclusão da leitura anterior;
- preservação do último snapshot e classificação stale/unknown durante falha.

### Notificação derivada da mesma transição

O consumidor deverá estabelecer baseline depois da primeira observação commitada. Uma segunda observação deverá criar uma transição canônica posterior à baseline no mesmo Server SQLite. `CommittedSyntheticTransitionSource` deverá projetar essa transição, e o `ReconciledNotificationCoordinator` deverá entregá-la uma vez a um `RecordingSink`.

Replay, reconexão e reinício do consumidor deverão reutilizar o mesmo cursor/ledger e não criar nova entrega. A evidência não poderá alegar exactly-once nem apresentação visível pelo Windows.

### Comando e revogação na mesma identidade Agent

O transporte de comando deverá usar o mesmo `agentId`, certificado, Agent SQLite e Server SQLite do pipeline. Somente fixtures deliberadamente não executáveis serão permitidas. O cenário deverá comprovar:

- versão incompatível, expiração, gap, reorder, perda de resposta e replay;
- acknowledgement como recepção/persistência, nunca execução;
- zero `CommandAttempt`, `Running`, `Succeeded`, provider result ou post-probe;
- revogação final da identidade e recusa de novas operações antes de mutação adicional;
- preservação factual do último snapshot durante a falha posterior.

Revogação do Agent não deverá revogar ou reutilizar a identidade humana read-only. As duas responsabilidades continuarão separadas e serão correlacionadas somente pelo `runId` e pelos IDs públicos da fixture.

## Sequência E2E futura

1. executar shutdown preflight e provar zero processo/listener DB-Notifier;
2. criar raiz temporária e manifest de ownership;
3. iniciar o host consolidado em HTTPS loopback com um Server SQLite vazio;
4. criar token e fazer enrollment do único Agent de teste;
5. persistir registration e assignment read-only no único Agent SQLite;
6. estabelecer heartbeat e identidade revogável;
7. iniciar o Dashboard no browser dedicado e observar snapshot inicial vazio ou factual;
8. emitir observação sintética `Degraded` enquanto a API está indisponível;
9. reiniciar o Agent, reconectar e ingerir a mesma mensagem uma vez;
10. observar a mesma instância no snapshot e estabelecer baseline silenciosa da notificação;
11. emitir observação sintética `Unavailable`, simular perda de resposta e fazer replay;
12. observar uma única mutação autoritativa, uma revisão nova e um hint SignalR;
13. observar o browser reler a API sem concorrência e depois reconciliar novamente após 30 segundos;
14. observar uma única entrega da transição `Degraded → Unavailable` ao sink de teste;
15. reiniciar o consumidor e confirmar ausência de nova entrega;
16. transportar fixture de comando expirada ou incompatível e confirmar zero attempt;
17. revogar o Agent e recusar nova observação, heartbeat, assignment e mensagem de comando;
18. induzir indisponibilidade local e confirmar preservação stale/unknown do último snapshot;
19. encerrar filhos, browser, host e listeners;
20. remover somente a raiz temporária pertencente ao run e comprovar cleanup integral.

Cada passo deverá carregar o mesmo `runId`; os passos Agent-side também deverão carregar o mesmo `agentId` e `instanceId` quando aplicável.

## Remediação dos warnings EF

As duas alterações futuras deverão ser estreitas e não exigir migration:

1. na carga de certificados para revogação, ordenar por `AgentCertificateId` antes de `Take(MaximumCertificatesPerAgent + 1)`;
2. na carga de scopes, ordenar a consulta por chaves estáveis da atribuição e das junções antes de `Take(MaximumAuthorisationScopes + 1)`.

Testes deverão capturar o logger EF e provar ausência de `Microsoft.EntityFrameworkCore.Query[10102]`. Também deverão provar que:

- todos os registros são processados abaixo ou no limite;
- acima do limite o comportamento continua fail-closed;
- a ordem física de inserção não muda a decisão;
- nenhuma regra RBAC, schema, índice ou migration foi alterada.

Ordenação existe para determinismo e diagnóstico; não deverá selecionar silenciosamente um subconjunto como autorização quando o limite for excedido.

## Contrato de PowerShell

Os runners modernos de browser e da futura campanha consolidada deverão declarar PowerShell 7 como versão mínima. A remediação futura deverá:

- adicionar uma diretiva `#Requires -Version 7.0` antes de qualquer criação de recurso;
- documentar em `docs/Development.md` que o browser/consolidated runner usa `pwsh` 7 ou superior;
- manter `scripts/run-legacy-tests.ps1` sob Windows PowerShell 5.1/Pester 3.4, sem misturar os dois contratos;
- provar que uma invocação em Windows PowerShell 5.1 recusa o runner moderno antes de criar diretório, processo, listener ou profile;
- provar que PowerShell 7 executa o cleanup normal e o cleanup após falha controlada;
- usar comparações de ownership consistentes e nunca encerrar processo apenas pelo nome.

Esta proposta escolhe um requisito moderno explícito em vez de prometer suporte não testado do runner de browser ao Windows PowerShell 5.1.

## Escopo de arquivos futuro proposto

O diff de implementação deverá permanecer limitado, em princípio, a:

- novo projeto `tests/DBNotifier.State06.ConsolidatedSandboxHost/`, somente com `ProjectReference` existentes;
- inclusão mínima desse projeto em `DBNotifier.sln`, apenas nas configurações `Any CPU` existentes;
- atualização factual da fixture positiva `tests/fixtures/nuget-vulnerability-report.complete-empty.json` para o novo projeto;
- runner PowerShell e auditor Node dedicados sob `scripts/`, sem dependência npm nova;
- testes unitários, de arquitetura e integração estritamente ligados ao harness;
- duas ordenações em `AgentFleetStore.cs` e seus testes de regressão;
- declaração de versão no runner de browser existente e documentação de desenvolvimento;
- relatório factual, estado e histórico do incremento.

O verificador NuGet, packages, package-lock, fontes NuGet, migrations e projetos de produto não deverão mudar. Se a implementação descobrir que outra fronteira é indispensável, deverá parar e solicitar ampliação de escopo antes da alteração.

## Testes futuros obrigatórios

| Área | Evidência mínima |
|---|---|
| ativação fail-closed | marcador, ambiente, loopback, raiz temporária e fixture sintética recusam valores incorretos |
| isolamento arquitetural | nenhum projeto de `src/` referencia o novo host; composição normal não registra endpoints/serviços consolidados |
| correlação | um `runId`, `agentId`, `instanceId`, stores e sequências atravessam todos os passos aplicáveis |
| pipeline | offline, restart de processo, reconnect, response loss, replay, duplicate e reorder |
| Dashboard | mesma projeção Server, leitura inicial, ETag/304, hint, 30 segundos, concorrência `1`, stale/unknown |
| notificação | baseline silenciosa, uma transição commitada, uma entrega, restart e deduplicação |
| comando | incompatibilidade, expiração, replay, revogação e zero attempt/efeito |
| EF | zero warning `10102`, estabilidade de ordenação e fail-closed acima do limite |
| PowerShell | recusa pré-recurso em 5.1 e cleanup completo em 7+ |
| recursos | budgets, cancelamento, fencing, timeout e no máximo um browser/harness por vez |
| cleanup | zero processo, listener, profile, store, certificado ou diretório pertencente ao run |
| regressão | solução completa, Dashboard, Pester, cobertura, format, docs, links, secrets e smoke normal |

O E2E consolidado deverá ser executado serialmente. Paralelizar cenários que compartilham store, portas, identidade ou profile será proibido.

## Critérios de aceite da remediação

O incremento futuro somente poderá ser classificado como `APROVADO` se:

1. um único run correlacionado produzir toda a sequência obrigatória;
2. o snapshot e a notificação vierem do mesmo Server SQLite alimentado pelo Agent;
3. pipeline, comando e revogação usarem o mesmo Agent e a mesma persistência Agent;
4. identidade Agent e identidade humana read-only permanecerem separadas;
5. SignalR continuar somente como hint e a API continuar autoritativa;
6. a notificação ocorrer uma vez depois da baseline e não se repetir após restart/replay;
7. nenhuma fixture de comando criar attempt ou efeito;
8. os dois warnings EF desaparecerem sem reduzir o comportamento fail-closed;
9. o runner moderno declarar e respeitar PowerShell 7 antes de criar recursos;
10. composição normal, workers normais e command polling permanecerem desabilitados;
11. nenhuma nova dependência, download, migration ou acesso externo for necessário;
12. todos os processos e temporários próprios forem encerrados/removidos;
13. os gates aplicáveis passarem e o relatório distinguir observado, inferido, não testado e bloqueado;
14. não houver achado crítico ou alto aberto.

Mesmo com esses critérios aprovados, o Quality Gate consolidado continuará exigindo uma nova campanha separadamente autorizada. Evidência produzida durante a implementação não deverá ser renomeada como campanha final.

## Classificação futura

- `APROVADO`: remediação implementada e validada no escopo, pronta para solicitar nova campanha;
- `REPROVADO`: comportamento implementado viola requisito, isolamento ou segurança;
- `BLOQUEADO`: evidência exige dependência, autoridade, recurso ou mudança fora do escopo;
- `NÃO APLICÁVEL`: somente para item realmente externo ao incremento, com justificativa.

O Human Gate final do `STATE-06` não pertence a esse incremento e não poderá ser aberto enquanto a campanha consolidada permanecer bloqueada.

## Fora de escopo absoluto

- ativação de qualquer host, endpoint, worker ou adapter na composição normal;
- banco, provider, Agent, credencial, certificado, IdP, PKI, vault ou canal operacional;
- PostgreSQL real, database monitorado, serviço permanente ou infraestrutura externa;
- download, restore externo, pacote, dependência, CDN ou registry;
- migration ou alteração de schema;
- comando administrativo, `CommandAttempt`, executor, shell, post-probe, processo ou serviço afetado;
- notificação Windows visível, e-mail, SMS, webhook, Teams, Slack ou outro canal externo;
- nova UI, API administrativa, monitoramento real ou suporte público de provider;
- carga representativa, endurance, HA, disaster recovery ou homologação;
- LLM, recomendação, planejamento, automação ou promoção `none → OBSERVER`;
- deploy, publicação, instalação, amostra humana, Human Gate final, `STATE-07` ou transição.

## Riscos e limitações residuais

- Um harness consolidado pode virar acidentalmente uma segunda composição de produto; o projeto sob `tests/`, o marcador exato e os testes de arquitetura deverão impedir isso.
- Compartilhar o Server SQLite aumenta a fidelidade de correlação, mas não prova PostgreSQL operacional, concorrência distribuída, power-loss ou recuperação de produção.
- Um único host continua sendo laboratório local e não prova rede real, PKI distribuída, IdP ou isolamento entre máquinas.
- O E2E de 30 segundos usa tempo real de browser e pode sofrer ruído do host; tolerâncias deverão preservar a regra “30 segundos depois da conclusão”, sem relaxar concorrência.
- O sink de teste prova solicitação de entrega, não apresentação de notificação pelo Windows.
- O transporte prova persistência e recusa, não execução ou homologação de comando.
- PowerShell 7 explícito melhora previsibilidade, mas exige que o operador use `pwsh`; Windows PowerShell 5.1 continuará reservado ao legado.
- Adicionar um projeto exige atualizar solução e fixture NuGet sintética; o diff da solução deverá ser revisado para impedir configurações x86/x64 ou pastas virtuais acidentais.
- Evidência sanitizada ainda pode perder utilidade se IDs e sequências não forem suficientes; o contrato deverá ser testado sem registrar segredo.
- A remediação não cobre advisories externos atuais porque downloads e acesso externo permanecem proibidos.

## Condições de parada da implementação futura

A implementação deverá parar antes de ampliar o escopo se:

- exigir pacote, download, registry, CDN ou nova fonte NuGet/npm;
- exigir migration, schema novo ou banco externo;
- não puder reutilizar os contratos e stores existentes sem alterar a composição normal;
- alcançar executor, command attempt, shell, serviço, provider ou banco operacional;
- precisar reutilizar a identidade Agent como identidade humana;
- não puder atribuir com segurança processo, listener, profile ou diretório temporário;
- expuser token, chave, certificado completo, senha ou header de autenticação;
- exceder os budgets da campanha anterior ou deixar cleanup incompleto;
- revelar mudança arquitetural material além de um harness de teste dedicado.

O bloqueio deverá ser relatado; não deverá ser contornado por uma nova fixture desconectada da cadeia.

## Entregáveis futuros

- host/orquestrador consolidado exclusivamente de teste;
- runner e auditor local sem dependência nova;
- E2E correlacionado e testes negativos;
- regressões EF e contrato explícito de PowerShell;
- atualização mínima da solução e da fixture NuGet positiva;
- relatório próprio da remediação com diff direto e cleanup;
- atualização factual de estado e histórico;
- commit local focado.

Não haverá deploy, publicação, release, amostra humana ou transição.

## Decisão futura de Bruno

Se Bruno concordar com esta proposta e desejar autorizar somente a implementação restrita da remediação, o texto sugerido é:

> AUTORIZO o incremento restrito de remediação do STATE-06 — Consolidated E2E Evidence Harness and Deterministic Gate Remediation, limitado a um novo host/orquestrador exclusivamente de teste sob `tests/`, runner/auditor local, uma única execução correlacionada com Agent e Server SQLite efêmeros, identidade Agent e identidade humana read-only separadas e exclusivamente de teste, pipeline Agent → API → Dashboard/SignalR → notificação em sink de teste, transporte de comando deliberadamente não executável, revogação, replay, falhas, fencing, budgets e cleanup. Autorizo a inclusão mínima do projeto na solução e na fixture NuGet sintética positiva, sem packages, downloads, lockfiles ou migrations; a ordenação estável das duas consultas EF identificadas; a declaração de PowerShell 7 para os runners modernos; testes e documentação factual correspondentes. Autorizo runtimes temporários exclusivamente locais e navegador dedicado com perfil efêmero, que deverão ser encerrados e removidos ao final. Permanecem proibidos acesso externo, recursos ou credenciais operacionais, provider/banco real, composição normal, comando administrativo, CommandAttempt, executor, shell, serviço/infraestrutura afetados, canal externo, notificação Windows visível, LLM, deploy, nova campanha consolidada, amostra humana, Human Gate final, promoção e transição de estado.

Essa autorização futura liberaria somente a remediação. A repetição da Campanha Consolidada exigiria outra autorização depois da revisão e aceitação do relatório da remediação.

## Próximo passo para Bruno

1. Leia `Resultado em linguagem simples`, `Decisões propostas`, `Arquitetura proposta`, `Critérios de aceite`, `Fora de escopo absoluto` e `Riscos e limitações residuais`.
2. Se desejar mudar algum limite, responda somente com os pontos a alterar.
3. Se concordar e quiser iniciar a remediação, envie exatamente o texto da seção `Decisão futura de Bruno`.
4. Depois da implementação futura, revise o relatório antes de aceitar o incremento.
5. Não autorize ainda a nova campanha, amostra humana, Human Gate ou transição.

Nenhuma ação técnica é necessária enquanto esta proposta estiver apenas em revisão.
