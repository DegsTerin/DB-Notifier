# Playbooks Operacionais

## Uso

Selecionar somente o playbook correspondente ao pedido. Todos dependem do estado atual, da governança, dos gates e das regras de segurança.

## Ajuste focado

Quando usar: correção ou melhoria delimitada.

1. Definir problema, comportamento esperado e fora de escopo.
2. Identificar a camada dona da mudança.
3. Preservar compatibilidade e mudanças preexistentes.
4. Cobrir regressão, segurança e comportamento multi-provider.
5. Atualizar documentação/evidência e encerrar com limitações.

## Auditoria completa

Quando usar: pedido explícito de revisão ponta a ponta.

- Inspecionar arquitetura, código, providers, segurança, dados, UI, testes, empacotamento e documentação.
- Validar local/remoto, stale, reconexão, alerta e notificações.
- Validar Start/Stop/Restart por capability e privilégio.
- Priorizar achados por criticidade com reprodução.
- Diagnóstico não autoriza correção, salvo quando o pedido incluir implementação.

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
