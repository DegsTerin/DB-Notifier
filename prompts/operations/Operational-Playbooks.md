# Playbooks Operacionais

## Uso

Selecionar somente o playbook correspondente ao pedido. Todos dependem do estado atual, da governança, dos gates e das regras de segurança.

## Shutdown preflight obrigatório por ação técnica

Executar antes de cada nova ação técnica autorizada no código, workspace ou produto. São gatilhos: aprovação que libera execução, alteração de fonte/configuração/documentação, ajuste, remediação, modificação, implementação, auditoria executável, build, teste ou amostra humana. Conversa, pergunta, explicação, planejamento sem execução e status sem mutação ou runtime não são gatilhos.

1. Inventariar WPF/Tray, Dashboard preview/dev server, Agent, API, helpers em background, runners de validação e navegador dedicado pertencentes ao DB-Notifier.
2. Identificar processos hospedados por `dotnet`, Node, PowerShell ou navegador através de PID, caminho do executável, command line, parentage, porta e perfil temporário comprovadamente pertencentes ao projeto. Nome genérico de processo não basta.
3. Encerrar primeiro os filhos e depois o processo proprietário, usando encerramento normal quando disponível e força somente para resíduo DB-Notifier confirmado.
4. Verificar zero processo correspondente, zero janela/ícone de notificação do DB Notifier e zero listener pertencente aos PIDs encerrados. Registrar PIDs, resíduos e limitações sem expor dados alheios.
5. Nunca encerrar PostgreSQL ou outro banco monitorado, serviço externo, browser/perfil comum do usuário, IDE, terminal alheio ou processo não atribuído com segurança ao DB-Notifier.
6. Se qualquer componente não puder ser identificado ou encerrado com segurança, interromper o trabalho e informar o resíduo exato. Não continuar sobre um runtime anterior incerto.

Uma amostra humana pode permanecer aberta somente quando a finalidade da ação técnica atual é entregá-la visivelmente ao validador. Antes de qualquer ação técnica posterior — inclusive registrar a aprovação em arquivos — aplicar este protocolo integralmente. Processos de build/teste iniciados na própria tarefa continuam sujeitos ao encerramento normal antes do hand-off, salvo essa entrega visível limitada.

## Fluxo de desenvolvimento governado

Usar para preparação, feedback local e Quality Gate do repositório:

1. Fechar o envelope: autoridade, baseline, estado, escopos positivo e
   negativo, trabalho protegido, contratos, ownership, recursos mutáveis,
   revisores, checks, aceite e stop codes.
2. Em trabalho amplo, transversal ou multi-incremento, criar ou sincronizar o
   `../../PLANS.md` vivo antes da implementação.
3. Executar `../../scripts/development.ps1 Doctor`. O comando é read-only,
   aplica a asserção de shutdown, valida as faixas estáveis compatíveis e não
   instala nem atualiza toolchain.
4. Se as dependências bloqueadas ainda não estiverem prontas e a autoridade
   permitir restore, executar `Setup`; usar `-Offline` somente quando caches
   locais forem a fonte autorizada.
5. Implementar o menor incremento coerente, atualizar regressões e usar
   `Quick` para feedback `NON_GATE` após cada lote útil.
6. Revisar diff, claims, segurança, compatibilidade e evidência; obter revisão
   independente quando o risco exigir e integrar candidatos serialmente.
7. Executar `Full` somente quando todos os preflight e runtimes locais do gate
   estiverem autorizados. `Full` delega uma vez ao agregador
   `../../scripts/ci.ps1` e preserva o primeiro resultado factual.
8. Atualizar plano, estado, histórico e changelog somente depois do fato,
   revisar staged diff, executar checks finais e produzir o commit focal e o
   handoff governado.

`-PlanOnly` permite examinar a sequência determinística sem executar ação
técnica. `Quick` nunca encerra Quality Gate. `Full -Offline` marca verificações
online como `NOT_RUN` e produz evidência `PARTIAL`, não equivalência ao gate
online. Scripts implementam o fluxo, mas não concedem autoridade, lifecycle,
ADR, Human Gate, ativação ou ação externa.

## Ajuste focado

Quando usar: correção ou melhoria delimitada.

1. Definir problema, comportamento esperado e fora de escopo.
2. Identificar a camada dona da mudança.
3. Preservar compatibilidade e mudanças preexistentes.
4. Cobrir regressão, segurança e comportamento multi-provider.
5. Atualizar documentação/evidência e encerrar com limitações.

## Auditoria completa

Quando usar: pedido explícito de revisão ponta a ponta.

1. Mapear a estrutura, as tecnologias, os módulos, as dependências e os processos reais de build, teste e automação antes de concluir sobre o projeto.
2. Inspecionar arquitetura, configuração, código-fonte, scripts, providers, integrações, dados, segurança, logs, tratamento de erros, desempenho, UI, testes, empacotamento, documentação e experiência de desenvolvimento.
3. Avaliar cada linguagem e framework segundo suas convenções oficiais e, quando aplicável, separação de responsabilidades, SOLID, DRY, KISS, OWASP, concorrência, gestão de recursos, compatibilidade e manutenção futura.
4. Validar comportamento local/remoto, stale/unknown, falhas parciais, reconexão, alertas e notificações sem apresentar alcance de transporte como saúde autenticada.
5. Validar Start/Stop/Restart somente contra capability, identidade, privilégio, confirmação, idempotência, auditoria e homologação comprovados; ausência de prova permanece `Unsupported`, `Unavailable`, `Denied` ou não testada conforme o fato.
6. Manter `../../PLANS.md` sincronizado com baseline, escopo negativo,
   findings, incrementos, evidências, decisões, limitações e outcome quando a
   mesma autoridade já incluir remediação.
7. Executar os testes e verificações estáticas reais que sejam seguros e aplicáveis ao estado autorizado, registrando comandos, versões, ambiente, exit codes, limitações e evidência sanitizada.
8. Classificar cada achado como `P0`, `P1`, `P2` ou `P3` e informar arquivo/localização, categoria, descrição técnica, evidência ou reprodução, impacto atual e futuro, causa e correção recomendada.
9. Quando remediação estiver autorizada, corrigir por causa raiz em incrementos
   pequenos, adicionar regressão, validar continuamente e submeter o conjunto
   a revisão independente antes do gate agregado.
10. Produzir resumo executivo, estado geral, lista de achados, riscos, melhorias, prioridade de correção e plano de ação sugerido, distinguindo observado, inferido, não testado e bloqueado.

Auditoria isolada não autoriza correção. Não alterar arquivos durante o
diagnóstico nem avançar o ciclo de vida. Se a solicitação atual já separar e
autorizar inequivocamente um lote de implementação, concluir e apresentar o
diagnóstico antes de executar somente esse lote; caso contrário, aguardar
aprovação específica antes de qualquer remediação.

## Dashboard

- Inventário e saúde em tempo real com timestamp/stale.
- Filtros por provider, host, ambiente, grupo, tag e status.
- Histórico, alertas, incidentes e drill-down.
- Ações administrativas visualmente separadas e auditáveis.
- Desempenho para grandes frotas, responsividade, acessibilidade e i18n.

## Auditoria UI/UX

- Tratar `docs/design/DB-Notifier-Design-System.md` como fonte normativa de tokens, temas, componentes e identidade visual.
- Cobrir vazio, loading, offline, erro, stale, manutenção e permissão negada.
- Verificar muitas instâncias, latência/status sem depender apenas de cor.
- Percorrer alerta, configuração, logs e comando administrativo.
- Testar Desktop Windows e web em viewports representativos.
- Validar teclado, foco, leitor de tela, contraste, escala e overflow.
- Validar Light e Dark, persistência, migração segura do valor System retirado para Light, troca sem perda de estado, High Contrast independente e paridade semântica React/WPF.
- Produzir evidência visual apenas quando materialmente útil.

## Sistematização

- Mapear duplicação, acoplamento e responsabilidades cruzadas.
- Extrair providers e casos de uso do núcleo.
- Uniformizar configuração, erros, telemetria e testes.
- Trabalhar em lotes seguros, com validação após cada lote.
- Continuar enquanto houver cleanup objetivo, seguro e dentro do escopo.

## Reestruturação PgNotifier → DB-Notifier

- Isolar PostgreSQL atrás de `IDatabaseProvider`.
- Separar Core, Application, Infrastructure, Providers, Agent, Desktop, API, Web e Tests.
- Introduzir contratos/versionamento antes da distribuição Agent/API.
- Manter marcos executáveis, migração de configuração e rollback.
- Renomear artefatos PgNotifier com compatibilidade e plano de upgrade.
- Evitar big bang e não anunciar engines antes da homologação.

## Revisão global

- Comparar legado, arquitetura aprovada e implementação.
- Detectar promessas não implementadas e divergências de provider.
- Avaliar monitoramento, alertas, controle, Dashboard, Tray e offline.
- Produzir backlog priorizado sem avançar fase automaticamente.

## Reorganização do sistema de instruções

- Ler entrada, visão, estado, governança e changelog.
- Detectar conflito, duplicação, referência quebrada e instrução obsoleta.
- Preferir consolidação temática a novo arquivo.
- Atualizar entrada, versão, changelog e auditoria em conjunto.
- Validar links, headings, contagem e termos legados.
- Não alterar código do produto quando o escopo for documental.
