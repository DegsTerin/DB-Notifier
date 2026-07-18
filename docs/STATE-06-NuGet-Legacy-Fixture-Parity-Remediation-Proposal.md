# Proposta STATE-06 — Remediação de Paridade da Fixture Legada NuGet

## Status e autoridade

- Data: 2026-07-18
- Posição do ciclo de vida: `STATE-06 INTEGRATION`, sem alteração
- Natureza: proposta exclusivamente documental e não executiva
- Origem: solicitação de Bruno para propor a remediação da fixture legada NuGet de `13` para `15` projetos, sem implementação, promoção ou transição de estado
- Baseline: commit `68b9f4e`, com o incremento Dashboard TV aceito e a ressalva do teste legado registrada
- Estado operacional: nenhum componente DB-Notifier foi iniciado por este pedido

Este documento não autoriza alteração de JSON, PowerShell, teste, solução, projeto, pacote, lockfile, configuração NuGet ou código. Também não autoriza download, consulta online, restore, build, runtime, promoção ou transição.

## Resumo para não especialistas

O projeto possui atualmente `15` projetos .NET na solução. Um arquivo de teste simula a saída do comando de verificação de vulnerabilidades do NuGet, mas esse arquivo ainda relaciona apenas `13` projetos. Por isso, o verificador faz o correto: recusa o relatório incompleto em vez de anunciar uma aprovação falsa.

A correção futura proposta é pequena. Ela acrescentaria ao relatório fictício os dois projetos que já existem na solução:

1. os testes de integração;
2. o executável temporário usado pelo sandbox de testes do Agent Fleet.

Depois disso, o teste positivo voltaria a representar os `15` projetos. Isso não significa que os pacotes foram consultados na internet ou que não possuem vulnerabilidades. Significa apenas que a fixture sintética voltou a cobrir toda a estrutura que pretende simular.

## Diagnóstico factual observado

### Solução corrente

`DBNotifier.sln` contém `15` projetos .NET. Os dois projetos ausentes na fixture são:

| Projeto existente | Target factual | Função |
|---|---|---|
| `tests\DBNotifier.IntegrationTests\DBNotifier.IntegrationTests.csproj` | `net10.0` | testes locais de integração |
| `tests\DBNotifier.AgentFleet.SandboxHost\DBNotifier.AgentFleet.SandboxHost.csproj` | `net10.0` | processo temporário usado somente pelo harness E2E local |

### Fixture positiva corrente

`tests/fixtures/nuget-vulnerability-report.complete-empty.json` contém `13` entradas. Ela possui schema `1`, os parâmetros `--vulnerable --include-transitive`, a fonte configurada e um framework por projeto, mas omite os dois projetos acima.

### Comportamento correto do gate

`scripts/verify-nuget-vulnerabilities.ps1` deriva a lista esperada diretamente da solução e compara caminhos completos sem diferenciar maiúsculas/minúsculas. Como `13` não é igual a `15`, ele encerra com erro antes de afirmar que o relatório está completo.

O teste Pester positivo em `tests/DBNotifier.Legacy.Tests.ps1` espera que a fixture `complete-empty` seja aceita. A fixture desatualizada contradiz essa intenção e causa a única falha registrada no gate legado. O defeito está nos dados sintéticos positivos, não na proteção fail-closed do verificador.

## Objetivo de uma remediação futura

Restabelecer a paridade exata entre a fixture positiva e os projetos de `DBNotifier.sln`, preservando o comportamento fail-closed e sem alterar o significado de nenhum resultado de segurança.

## Escopo proposto para uma autorização futura

### 1. Corrigir somente a fixture positiva

Adicionar a `tests/fixtures/nuget-vulnerability-report.complete-empty.json` estas duas entradas, seguindo a ordem da solução e o formato existente:

- `tests\DBNotifier.IntegrationTests\DBNotifier.IntegrationTests.csproj`, framework `net10.0`;
- `tests\DBNotifier.AgentFleet.SandboxHost\DBNotifier.AgentFleet.SandboxHost.csproj`, framework `net10.0`.

As entradas não devem inventar pacotes, versões, advisories ou resultados. O relatório continuará sendo uma fixture estrutural vazia e explicitamente sintética.

### 2. Preservar os cenários negativos

- `nuget-vulnerability-report.invalid.json` deve continuar inválido.
- `nuget-vulnerability-report.no-frameworks.json` deve continuar provando recusa de um projeto sem evidência de framework.
- Nenhum cenário negativo deve ser enfraquecido para fazer o conjunto passar.
- O script `verify-nuget-vulnerabilities.ps1` não deve ser relaxado, ignorar projetos de teste ou aceitar contagens divergentes.

### 3. Verificação local, offline e proporcional

Uma implementação futura deve executar:

1. o verificador com `-ReportPath` apontando para a fixture positiva e observar a mensagem de aprovação para `15` projetos;
2. os três testes Pester do verificador, confirmando que o positivo passa e os dois negativos continuam recusados;
3. o gate legado completo por `scripts/run-legacy-tests.ps1`, incluindo seu piso de cobertura;
4. validação de JSON, documentação, links, secret scan e `git diff --check`;
5. inspeção do diff para confirmar que apenas a fixture autorizada e a documentação factual correspondente mudaram.

Build .NET, testes de produto, restore e runtime não são necessários para uma alteração exclusivamente em dados sintéticos de teste, salvo se a inspeção do diff revelar expansão de escopo. Nenhuma consulta online NuGet/npm deve ser executada sem autorização externa separada.

### 4. Atualização factual após a execução

Se a correção for autorizada e aprovada:

- registrar o resultado em um relatório curto de remediação;
- atualizar a ressalva no estado atual como resolvida pela correção da fixture, sem reescrever a evidência histórica do commit `70b3960`;
- acrescentar uma entrada append-only no histórico;
- não alterar a classificação ou aceitação humana já registrada do incremento Dashboard TV.

## Fora de escopo absoluto

Permanecem fora desta proposta e de uma eventual autorização limitada:

- alteração de `DBNotifier.sln` ou inclusão/remoção de projeto;
- mudança no verificador para ignorar IntegrationTests, SandboxHost ou qualquer outro projeto;
- alteração de packages, versões, `packages.lock.json`, `NuGet.config` ou fontes;
- restore, download, auditoria online ou afirmação de ausência atual de vulnerabilidades;
- correção de outras fixtures ou refatoração do conjunto Pester sem achado específico;
- mudança de código de produto, API, Dashboard, Agent, provider, persistência ou runtime;
- deploy, publicação, ação externa, LLM, executor, `OBSERVER`, promoção ou transição de estado.

## Critérios de aceite propostos

Uma remediação futura só poderá ser considerada concluída se:

1. a fixture positiva contiver exatamente os mesmos `15` caminhos de projeto da solução;
2. cada projeto possuir o target factual correspondente e nenhuma entrada estiver duplicada;
3. o gate emitir aprovação para `15` projetos ao consumir exclusivamente a fixture positiva;
4. relatórios incompletos e sem frameworks continuarem recusados;
5. o conjunto legado completo passar com seu piso de cobertura;
6. nenhum script de proteção for enfraquecido;
7. nenhum pacote, lockfile, fonte, projeto ou código de produto mudar;
8. nenhum acesso externo ou runtime ocorrer;
9. documentação, links, secrets e diff passarem;
10. o relatório distinguir claramente paridade estrutural sintética de auditoria real de vulnerabilidades.

## Riscos e limitações residuais

- A fixture é estática e poderá ficar desatualizada quando outro projeto for adicionado à solução. A falha atual demonstra que o gate detecta essa divergência de forma segura.
- O teste positivo prova somente que o parser aceita um envelope completo e vazio com todos os projetos; ele não consulta feeds e não prova ausência de CVEs ou advisories.
- O target na fixture deve acompanhar o target real de cada projeto. Alterações futuras de TFM exigirão manutenção factual equivalente.
- O SandboxHost é um projeto temporário de teste, mas pertence à solução e deve ser incluído enquanto permanecer nela; excluí-lo reduziria artificialmente a cobertura do gate.
- Corrigir essa fixture remove a ressalva técnica conhecida, mas não conclui `STATE-06`, não habilita runtime e não satisfaz um gate de release.

## Entregáveis deste pedido

Este pedido produz somente:

- esta proposta documental;
- atualização factual do estado corrente para indicar que existe uma proposta pendente;
- registro append-only da solicitação e do diagnóstico.

Não houve alteração da fixture, script, teste, solução, código, pacote ou configuração. Nenhum build, teste executável, restore, runtime ou auditoria online foi realizado.

## Próxima decisão solicitada

Bruno pode ajustar, adiar, rejeitar ou autorizar separadamente somente a remediação descrita. Uma autorização inequívoca poderá usar:

> AUTORIZO exclusivamente a remediação local da fixture positiva NuGet em STATE-06, limitada a acrescentar os projetos DBNotifier.IntegrationTests e DBNotifier.AgentFleet.SandboxHost com target net10.0 ao relatório sintético complete-empty, executar os testes Pester/offline correspondentes e atualizar a documentação factual. Não autorizo alteração do verificador, solução, projetos, packages, lockfiles, fontes NuGet, código de produto, acesso externo, runtime, promoção ou transição de estado.

Essa autorização futura não deverá ser inferida desta proposta.

## Adendo factual — autorização consumida e remediação executada

Em 2026-07-18, Bruno autorizou separadamente somente a remediação delimitada. A fixture positiva recebeu exclusivamente as entradas `DBNotifier.IntegrationTests` e `DBNotifier.AgentFleet.SandboxHost`, ambas `net10.0`. O verificador permaneceu inalterado, aceitou os `15` projetos, recusou as duas fixtures negativas e o gate legado completo passou.

Nenhum projeto, solução, package, lockfile, fonte NuGet, código de produto, acesso externo, build ou runtime mudou. O resultado e as limitações pertencem ao [relatório da remediação](STATE-06-NuGet-Legacy-Fixture-Parity-Remediation-Report.md). A decisão humana desse resultado permaneceu separada da execução e está registrada no adendo seguinte.

## Adendo factual — decisão humana da remediação

Bruno aceitou em 2026-07-18 a remediação do commit `4732ed7` com as limitações registradas e autorizou exclusivamente o registro factual da decisão. Ele proibiu expressamente novo incremento, acesso externo, promoção e transição de estado.

Essa aceitação encerra somente a revisão desta remediação. Ela não converte a fixture sintética em auditoria real de vulnerabilidades nem cria nova autoridade executiva.
