# Segurança, Identidade e Acesso

## Princípios

- Menor privilégio, deny by default e separação de responsabilidades.
- Credenciais de monitoramento e administração são identidades diferentes.
- O servidor autoriza; a interface nunca é a única barreira.
- Segredos não aparecem em logs, UI, exceptions, testes, commits ou evidências.
- Toda ação sensível possui ator, alvo, motivo, horário e resultado.

## Trust boundaries

- Usuário ↔ Dashboard
- Dashboard ↔ API
- API ↔ Agent
- Agent ↔ provider/driver
- Provider ↔ banco monitorado
- Agent ↔ sistema operacional/utilitário nativo
- Aplicação ↔ cofre de secrets

Avaliar SSRF em endpoints configuráveis, command injection em utilitários, impersonation de Agent, replay de comando, exfiltração de secrets e abuso de controle administrativo.

## Autenticação humana

- Senhas com hashing moderno e parâmetros revisáveis.
- MFA quando exigido pelo ambiente.
- Sessões/tokens com expiração, revogação e rotação.
- Cookies seguros quando usados; proteção CSRF e CORS restritivo.
- Rate limiting e respostas que não facilitem enumeração.
- Recuperação de acesso auditada.

## Identidade de Agent

- Provisionamento explícito e identidade única.
- Credencial rotacionável e revogável.
- TLS obrigatório; mTLS ou mecanismo equivalente conforme ADR.
- Versão e capabilities declaradas, nunca confiadas sem validação.
- Agent revogado não recebe comandos nem publica eventos aceitos.

## Segredos

- Windows: Credential Manager/DPAPI ou cofre corporativo aprovado.
- Linux: cofre do sistema/corporativo ou secret manager aprovado, vinculado à identidade do serviço/workload.
- Cloud: workload/federated identity e referência ao secret manager do provedor quando disponíveis; evitar chaves estáticas de longa duração.
- Servidor: secret manager externo ou mecanismo equivalente.
- Banco central armazena referência opaca, não senha em claro.
- Chave de criptografia fica fora do dado cifrado e do repositório.
- Rotação, backup e recuperação possuem procedimento testado.

## RBAC

Papéis mínimos:

- `Viewer`
- `Operator`
- `InstanceAdministrator`
- `SecurityAdministrator`
- `PlatformAdministrator`

Escopos possíveis: ambiente, servidor, grupo/tag e instância.

Permissões separadas:

- Visualizar status e histórico
- Gerenciar catálogo/configuração
- Reconhecer ou silenciar alerta
- Executar Start, Stop ou Restart individualmente
- Ver ou alterar referência de credencial
- Administrar usuários, papéis, Agents e políticas
- Consultar/exportar auditoria

## Controle administrativo

Start, Stop e Restart exigem:

- Capability declarada pelo provider e Agent
- Permissão específica no escopo do alvo
- Confirmação e motivo
- Idempotency key, timeout e expiração
- Canal autenticado e protegido contra replay
- Resultado verificado por probe posterior
- Auditoria completa

Controle de serviço do sistema operacional é apenas um adaptador possível, nunca pressuposto universal.

Credencial do banco não concede implicitamente controle do serviço Windows, systemd, container/orquestrador ou recurso cloud. Cada plano de controle exige referência, autorização, capability e auditoria próprias.

## Auditoria

Registrar login, logout, falha de autenticação, provisionamento/revogação de Agent, mudança de papel/política, acesso ou rotação de segredo, alteração de instância, silenciamento e comando administrativo.

O log deve ser append-only, pesquisável, retido por política e protegido contra alteração pelo próprio operador auditado.

## Remediação de credenciais

1. Inventariar hardcodes, arquivos, logs, exemplos e histórico.
2. Conter exposição e comunicar owner.
3. Rotacionar material exposto com autorização.
4. Migrar para cofre e referência opaca.
5. Validar acesso negado, rotação e recuperação.
6. Registrar evidência sanitizada.

## Checklist

- Threat model atualizado
- Secrets scan e dependency scan executados
- TLS e validação de certificado configurados
- RBAC negativo testado
- Logs sem dados sensíveis
- SSRF e command injection mitigados
- Rotação/revogação testadas
- Ações administrativas auditadas
- Supply chain, assinatura e atualização avaliadas

## Controles adicionais para IA

- O LLM não acessa banco, secret ou executor diretamente.
- Texto livre nunca é convertido diretamente em SQL ou shell.
- Conteúdo recuperado é não confiável e protegido contra prompt injection.
- Risco e autorização são determinados por política, não pelo modelo.
- Modelo, prompt, fontes, confiança, aprovação e resultado são auditados.
- Há avaliações offline, red teaming, detecção de drift, kill switch e fallback sem IA.
