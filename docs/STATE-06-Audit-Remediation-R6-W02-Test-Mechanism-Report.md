# R6-W02 — Mecanismo test-only do flyout

## Resultado

- Baseline autorizada: `1b0d90c46bd4a24de38b0636f61f3f195d8afa87`.
- Data: 2026-07-23.
- Escopo automático: `APROVADO`.
- Amostra humana visível `R6-HV-W02`: `BLOQUEADA` até autorização posterior e separada; não executada neste lote.
- Lifecycle: permanece `STATE-06 INTEGRATION`, sem promoção ou transição.

## Implementação

- O argumento exato `--review-flyout-live-update`, aceito somente uma vez, habilita o W02. Ausência, duplicação ou texto semelhante falham fechados.
- A composição W02 permanece tray-first e neutraliza modos concorrentes: não inicia a matriz de notificações, não cria o runtime reconciliado e não mostra o shell secundário no startup.
- Cada abertura do flyout reinicia três frames determinísticos e finitos, separados por quatro segundos:

  1. agregado `Critical`, uma instância desabilitada;
  2. agregado `Warning`, duas instâncias desabilitadas;
  3. agregado `Unknown`, uma instância desabilitada.

- Cada resumo é derivado das mesmas quatro linhas que o flyout apresenta. Um único turno do Dispatcher atualiza o mark do Tray, tooltip, mark do flyout, texto agregado, contagem visível, textos e ícones semânticos das linhas.
- Cada frame exige exatamente as quatro identidades sintéticas estáveis; identidade ausente, repetida ou inventada falha fechada.
- Ao ocultar o flyout, o timer W02 para, o frame ativo é descartado e somente a apresentação normal do Tray/flyout é restaurada. O shell secundário oculto não é atualizado e a sequência não se repete indefinidamente.
- Os antigos glyphs fixos das linhas do flyout foram substituídos pelos `SemanticIcon` code-native já existentes, permitindo que forma, texto e cor mudem juntos sem estado híbrido.

## Isolamento

- O mecanismo de sequência contém somente estado tipado em memória; não referencia arquivo, HTTP, rede, publisher, runtime reconciliado ou comando.
- No modo W02, o controller não cria `WindowsAppNotificationPublisher`, não inicia o refresh normal, não ativa o sandbox reconciliado e recusa também confirmação de fechamento, fila legacy e fallback.
- Os três atalhos do flyout para o shell secundário ficam desabilitados e uma guarda adicional no controller recusa qualquer tentativa de navegação enquanto W02 está ativo. Assim, idioma, tema e demais preferências não podem ser persistidos por essa composição.
- Nenhuma dependência, package, lockfile, schema, migration, fixture operacional ou contrato externo foi alterado.
- Nenhum runtime visível ou amostra humana foi aberto.
- A preferência local permaneceu com SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`.

## Evidência automática

| Verificação | Resultado |
|---|---|
| Build WPF Release `--no-restore` | aprovado, 0 avisos e 0 erros |
| Build da solução Release `--no-restore` | aprovado, 0 avisos e 0 erros |
| `DBNotifier.Desktop.Wpf.Tests` | 10/10 aprovados |
| Arquitetura focal WPF/W02/notificações | 23/23 aprovados |
| Unit tests focados de apresentação | 66/66 aprovados |
| Unit tests completos | 393/393 aprovados |
| Arquitetura global | 51/52; somente a falha R0 preexistente |
| `dotnet format --verify-no-changes --no-restore` | aprovado |
| Documentação, links, tokens, localização e secrets | aprovados |

O primeiro build detectou apenas `CA1859` no tipo declarado da coleção imutável de frames. A declaração foi estreitada para `ReadOnlyCollection<T>` e o mesmo build passou sem avisos ou erros. Nenhum restore ou download ocorreu.

A suíte arquitetural global continuou não verde exclusivamente em `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`, que procura `state05-dashboard-failure.json`. Essa é a falha R0 já registrada na baseline; nenhuma correção, bypass ou alteração fora do W02 foi aplicada.

## Limitações preservadas

- A execução automática não prova aparência, estabilidade de foco ou percepção humana durante a troca com o flyout aberto.
- `R6-HV-W02` ainda requer autorização visível própria, execução isolada, cleanup e decisão humana explícita.
- `R6-HV-P01` e suas condições físicas permanecem `NÃO TESTADAS`.
- O gate global R0 preexistente e o incidente de metadados NuGet R5 permanecem registrados e sem correção.
- W02 não autoriza notificação Windows, persistência, integração operacional, comando, acesso externo, R7/R8, O1, AIOps, promoção ou transição.
