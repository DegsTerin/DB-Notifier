# Qualidade, Evidências e Gates

## Padrão de evidências

Toda alegação técnica deve registrar, conforme aplicável:

- Comando, diretório, versão, data e exit code
- Escopo e ambiente
- Resultado resumido e artefato correspondente
- Logs e screenshots sanitizados
- Distinção entre observado, inferido, não testado e bloqueado

Banner de sucesso, compilação isolada ou ausência de erro aparente não provam saúde funcional.

## Definition of Done

- Requisitos e critérios atendidos.
- Build, testes e análise estática aplicáveis aprovados.
- Segurança, falhas parciais e compatibilidade avaliadas.
- Logs, observabilidade e mensagens de erro adequados.
- Documentação, migration e rollback atualizados.
- Nenhum secret ou evidência falsa.
- Dívida e cobertura pendente explicitadas.
- Mudanças preexistentes não relacionadas preservadas.

## Auditoria automática comum

1. Confirmar estado e escopo.
2. Conferir diff e entregáveis esperados.
3. Descobrir e executar comandos reais do repositório.
4. Validar build, testes, análise estática, secrets e dependências aplicáveis.
5. Verificar providers, separação monitor/admin e comportamento sem conectividade.
6. Classificar cada gate como APROVADO, REPROVADO, BLOQUEADO ou NÃO APLICÁVEL.
7. Registrar achados com severidade, impacto, reprodução e correção recomendada.

Auditoria não corrige silenciosamente falhas, não inventa evidência e não promove estado.

## Verificações específicas por fase

| Estado | Verificações adicionais |
|---|---|
| STATE-01 | Bootstrap limpo, configuração, dependências e ausência de domínio prematuro |
| STATE-02 | ADRs, boundaries, threat model, offline, atualização e rollback |
| STATE-03 | Constraints, índices, retenção, segredo por referência e migrations |
| STATE-04 | Arquitetura de dependências, autorização, idempotência e testes de providers |
| STATE-05 | Estados de UI, acessibilidade, responsividade e dado stale |
| STATE-06 | Contratos, compatibilidade, reconexão, dedup e E2E em sandbox |
| STATE-07 | Matriz real, segurança, carga, falha, recuperação e cobertura |
| STATE-08 | Artefato, assinatura, SBOM, deploy autorizado, observabilidade e rollback |

## Human Gate

O validador humano deve:

- Revisar relatório automático e repetir amostras críticas.
- Confirmar experiência operacional e mensagens de erro.
- Verificar distinção entre local/remoto e provider suportado/planejado.
- Confirmar autorização e auditoria de ações administrativas.
- Registrar decisão, nome, data, ressalvas e evidência sanitizada.

Decisões possíveis: `PENDENTE`, `APROVADO`, `APROVADO COM RESSALVAS` ou `REPROVADO`.

O gate não pode ser pré-aprovado nem substituir falha técnica sem justificativa formal.

## Amostras humanas por fase

- STATE-01: onboarding de desenvolvedor e execução limpa.
- STATE-02: walkthrough de ameaças e cenários híbridos.
- STATE-03: leitura de modelo/migration e recuperação.
- STATE-04: falhas reais de provider e autorização negativa.
- STATE-05: operação por teclado, leitor de tela e diferentes viewports.
- STATE-06: desconectar/reconectar Agent e duplicar mensagens.
- STATE-07: operar matriz representativa e revisar resultados de carga.
- STATE-08: ensaiar rollout, health check e rollback.

## Retrospectiva arquitetural

Executar quando implementação ou integração revelar pressuposto incorreto:

- Comparar ADRs e solução real.
- Reavaliar limites Agent/API/provider/UI e modelo de ameaças.
- Registrar dívida aceita e ADR substituto.
- Não reescrever o histórico.

## Auditoria dos Human Gates

Comparar checklists, relatórios automáticos e workspace; detectar aprovação pré-preenchida, item não aplicável sem justificativa e cobertura omitida. A auditoria aponta inconsistências, mas não substitui a decisão humana.
