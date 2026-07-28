# Proposta STATE-06 — Capacidade JOSE Completa

## Status e autoridade

- Data: 2026-07-28
- Lifecycle atual: `STATE-06 INTEGRATION`
- Status: `PROPOSTA DOCUMENTAL — NÃO AUTORIZADA PARA IMPLEMENTAÇÃO`
- Baseline documental: commit
  `87cb50dd77669cf553e6ecacdf26e2af0b14ee73`
- ADR associado:
  [ADR-0008 — JOSE Cryptographic Profiles and Key Lifecycle](architecture/ADR-0008-JOSE-Cryptographic-Profiles-And-Key-Lifecycle.md),
  com status `proposed`

Esta atividade autoriza somente a elaboração e integração documental desta
proposta. Ela não autoriza código, configuração executável, dependência,
restore/download, migration, IdP, login operacional, chave, certificado,
vault, KMS, HSM, banco, serviço externo, runtime, deploy, publicação,
transição de lifecycle ou alteração do estado de ativação do MOD-12.

## Resumo executivo

Propõe-se transformar a validação JWT/OIDC já existente em uma capacidade
JOSE completa, versionada e orientada por finalidade. A entrega final cobrirá
assinatura (JWS), criptografia (JWE), representação e distribuição de chaves
(JWK/JWKS), algoritmos (JWA), JWT e todas as serializações JOSE aplicáveis.

“Completa” significa que cada formato, serialização, cabeçalho, tipo de chave
e algoritmo aplicável possui decisão, política, teste e evidência. Não
significa habilitar algoritmos inseguros ou todo item registrado pela IANA.
Itens obsoletos ou incompatíveis serão explicitamente rejeitados e continuarão
fazendo parte da matriz de cobertura.

A proposta preserva quatro limites:

1. o DB-Notifier não se torna automaticamente um provedor de identidade ou
   emissor de senhas/tokens humanos;
2. JOSE não substitui HTTPS, mTLS dos Agents, RBAC server-side,
   idempotência, auditoria ou referências opacas de segredo;
3. nenhum segredo ou chave privada entra no repositório ou na persistência
   comum; e
4. nenhuma biblioteca, IdP ou infraestrutura é escolhida apenas por declarar
   suporte genérico a JOSE.

## Estado observado

### Existente

- A Server API usa `JwtBearer` para validar JWT humano emitido externamente.
- O validador exige issuer, audience, lifetime e signing key.
- Discovery OIDC e JWKS usam HTTPS e a política de egress `human-identity`.
- O laboratório R-NET comprovou localmente discovery, JWKS, JWT `RS256` e
  recusas de issuer, audience, signing key, redirect e origem cruzada.
- Os pacotes Microsoft IdentityModel chegam pela dependência
  `Microsoft.AspNetCore.Authentication.JwtBearer`.

### Ausente ou não comprovado

- emissão de JWS pelo produto;
- JWS Flattened/General JSON, múltiplas assinaturas e detached payload;
- JWE Compact/Flattened/General, AAD, múltiplos destinatários e nested JWT;
- perfil de algoritmos por finalidade;
- gestão produtiva de chaves, rotação, revogação e recuperação de
  comprometimento;
- KMS/HSM/vault e workload identity homologados;
- JWKS próprio, caso algum uso futuro realmente o exija;
- login Dashboard integrado a IdP operacional;
- testes de interoperabilidade e segurança JOSE completos; e
- homologação de IdP, chaves, algoritmos, plataformas ou infraestrutura real.

## Objetivo

Entregar uma capacidade JOSE reutilizável, provider-neutral, fail-closed,
interoperável e auditável para casos de uso explicitamente aprovados, sem
levar criptografia, identidade ou infraestrutura para Domain e sem criar um
emissor de tokens genérico.

## Definição de cobertura completa

### Padrões normativos

| Padrão | Cobertura proposta |
|---|---|
| RFC 7515 — JWS | Compact, Flattened JSON, General JSON, múltiplas assinaturas e detached payload |
| RFC 7516 — JWE | Compact, Flattened JSON, General JSON, AAD, múltiplos destinatários e nested content |
| RFC 7517 — JWK | JWK/JWKS público, seleção limitada, `use`, `key_ops`, `kid` e tipos autorizados |
| RFC 7518 — JWA | matriz versionada de algoritmos, key management, content encryption e tipos de chave |
| RFC 7519 — JWT | claims, validação temporal, issuer/audience/subject, `jti` por finalidade e nested JWT |
| RFC 7638 | thumbprint canônico de JWK para identidade pública estável |
| RFC 7797 | `b64=false` somente por opt-in de perfil; desligado na baseline |
| RFC 8725 | baseline obrigatória contra algorithm confusion, substitution e cross-JWT confusion |
| RFC 9864 | identificadores totalmente especificados e tratamento atual de `Deprecated`/`Prohibited` |
| IANA JOSE registries | inventário factual versionado; toda entrada classificada, nenhuma habilitação automática |

Referências primárias:
[JWS](https://www.rfc-editor.org/rfc/rfc7515.html),
[JWE](https://www.rfc-editor.org/rfc/rfc7516.html),
[JWK](https://www.rfc-editor.org/rfc/rfc7517.html),
[JWA](https://www.rfc-editor.org/rfc/rfc7518.html),
[JWT](https://www.rfc-editor.org/rfc/rfc7519.html),
[JWT BCP](https://www.rfc-editor.org/rfc/rfc8725.html),
[algoritmos totalmente especificados](https://www.rfc-editor.org/rfc/rfc9864.html)
e [registros JOSE da IANA](https://www.iana.org/assignments/jose/jose.xhtml).

### Matriz funcional obrigatória

Cada capacidade deverá ser classificada como:

- `Adopted`: implementada, integrada e homologada para finalidade exata;
- `Safely adapted`: limitada a compatibilidade ou perfil estreito documentado;
- `Rejected`: deliberadamente recusada por segurança, arquitetura ou ausência
  de caso de uso; ou
- `Scheduled`: válida, mas dependente de lote, plataforma ou infraestrutura
  ainda não autorizada.

Essa classificação será aplicada a:

- serializações JWS/JWE;
- headers registrados e extensões `crit`;
- algoritmos de assinatura/MAC;
- algoritmos de key management;
- algoritmos de content encryption;
- key types, curvas e parâmetros;
- single/multi-signature e single/multi-recipient;
- payload codificado, detached e nested;
- emissão, consumo, publicação e rotação; e
- cada superfície DB-Notifier que consome ou produz um artefato.

## Casos de uso propostos

| Caso | Resultado esperado | Limite |
|---|---|---|
| Autenticação humana | validar access token JWS de IdP externo com perfil exclusivo | a API não recebe senha nem emite token humano |
| Login Dashboard | Authorization Code + PKCE com IdP externo, em incremento próprio | a UI não autoriza e não guarda chave privada |
| Artefatos de controle assinados | JWS com tipo explícito, finalidade, audience e replay/idempotência adequados | não substituir comando durável nem Human Gate |
| Conteúdo restrito fim a fim | JWE somente após classificação demonstrar necessidade além de TLS/storage encryption | nunca transportar segredo de provider por conveniência |
| Artefactos com dupla assinatura | General JWS com política exata de todos/quórum de signatários | não aceitar “qualquer assinatura válida” |
| Conteúdo para vários destinatários | General JWE com conjunto limitado e auditado | desabilitado até necessidade e custódia comprovadas |
| Manifests/updates futuros | avaliar JWS como uma camada do contrato de proveniência | não substituir assinatura de pacote/plataforma |
| MOD-12 | avaliar adapter separado somente se o contrato de confiança aprovado exigir JOSE | nenhuma ativação ou autoridade implícita |

O primeiro uso produtivo recomendado é somente endurecer a validação JWS
humana existente. Emissão própria, JWE e artefatos de controle entram depois
e de forma independente.

## Arquitetura proposta

```text
External IdP                  Approved KMS/HSM/Vault
    | OIDC/JWKS                         |
    v                                   v
DB-Notifier Server/API -> JOSE Infrastructure Adapter
          |                 |
          |                 +-- immutable purpose profiles
          |                 +-- bounded parser/serialiser
          |                 +-- signing/encryption ports
          |                 +-- verification/decryption ports
          |
          +-> Application use cases and server-side RBAC
          |
          +-> versioned API contracts
                         |
                    Dashboard

Agent -- HTTPS/mTLS --> Server/API
  |
  +-- provider adapters --> monitored databases
```

### Responsabilidades

| Camada/componente | Possui | Não pode possuir |
|---|---|---|
| Domain | nenhuma dependência JOSE | token, algoritmo, chave, IdP ou KMS |
| Application | finalidade, perfil imutável, portas, resultados e códigos canônicos | parser, chave privada ou primitiva criptográfica |
| Infrastructure Security | adapter de biblioteca JOSE, bounded parsing e adapters KMS/vault | decisão de RBAC ou seleção de finalidade pela entrada |
| Server API | composição, perfil por rota/caso, autenticação e auditoria | senha humana, chave em configuração ou token issuer genérico |
| Dashboard | início/retorno OIDC e apresentação de denied/expired | autorização server-side ou chave de assinatura/decriptação |
| Agent | mTLS e contratos duráveis existentes | token humano ou substituição da identidade de certificado |
| Provider | conexão de database e capability própria | identidade humana, JOSE ou material de chave |

### Contratos iniciais

Os nomes finais dependerão da revisão de desenvolvimento, mas as
responsabilidades mínimas serão:

- `JosePurpose`: finalidade fechada e versionada, não string livre externa;
- `JoseProtectionProfile`: algoritmos, serializações, headers, tipos, limites
  e trust source imutáveis;
- `IJoseVerifier` e `IJoseDecryptor`: consumo fail-closed;
- `IJoseSigner` e `IJoseEncryptor`: produção somente por uso autorizado;
- `ICryptographicKeyResolver`: resolve referência opaca e metadados, nunca
  retorna segredo para Domain/Application;
- `JoseValidationOutcome`: resultado tipado com código sanitizado;
- `JoseKeyDescriptor`: metadados públicos, finalidade e lifecycle;
- `JoseAuditEvent`: finalidade, versão, algoritmo, referência pública/opalca,
  resultado e correlação, sem conteúdo protegido.

Uma análise de dependências decidirá se essas implementações permanecem em
`DBNotifier.Infrastructure` ou justificam um assembly
`DBNotifier.Infrastructure.Security.Jose`. Nenhum novo projeto será criado
somente por organização estética.

## Perfil de segurança proposto

### Seleção e parsing

- A rota/caso de uso seleciona o perfil antes de interpretar o objeto.
- `alg`, `enc` e tipo do token nunca escolhem a política.
- JSON duplicado, base64url inválido, header crítico desconhecido, tamanho,
  profundidade, cardinalidade ou nesting excessivo falham antes do trabalho
  criptográfico caro.
- `jku` e `x5u` recebidos não iniciam rede; `jwk`/`x5c` embutidos não viram
  trust anchor.
- `kid` é apenas hint limitado dentro do trust source preconfigurado.
- Cabeçalhos de segurança precisam estar protegidos.

### Assinatura

- baseline de produção: `PS256` ou `ES256`, conforme IdP/custodiante;
- `RS256` somente inbound e por compatibilidade explícita;
- `Ed25519` permanece `Scheduled` até a cadeia completa suportar o
  identificador do RFC 9864;
- `none` sempre rejeitado;
- HMAC entre trust boundaries independentes rejeitado;
- multi-signature exige conjunto/quórum predefinido e signatários distintos;
- conteúdo detached exige binding exato de bytes e content type.

### Criptografia

- baseline candidata: `RSA-OAEP-256` + `A256GCM`;
- ECDH-ES depende de política exata de curva e homologação;
- `RSA1_5`, CBC-HMAC, PBES2 e `zip` rejeitados inicialmente;
- nonce/IV vem de CSPRNG aprovado e não pode repetir por chave;
- plaintext só é liberado depois de tag autenticada válida;
- erros de decriptação são indistinguíveis externamente;
- nested JWT usa sign-then-encrypt, com perfis interno e externo separados.

### Claims e replay

- `iss`, `aud`, `sub`, `exp`, `nbf`, `iat`, `jti`, `typ` e `cty` são
  exigidos conforme a finalidade, com limites próprios;
- access token, ID token, artefato de controle e token de recuperação nunca
  compartilham a mesma regra de validação;
- `jti` não cria automaticamente store global; replay/idempotência pertence
  ao caso de uso;
- clock skew é pequeno, configurado e auditável;
- claims recebidas são não confiáveis até validação completa e RBAC
  server-side posterior.

## Gestão de chaves e infraestrutura

### Identidades e separação

Serão distintas:

- chave do IdP externo;
- chave de assinatura de artefato DB-Notifier;
- chave de decriptação DB-Notifier;
- chave de package/update;
- identidade mTLS do Agent;
- chaves/autoridades futuras do MOD-12; e
- identidade humana, workload e operador de recuperação.

Uma chave não muda de finalidade por configuração. Sign/verify e
encrypt/decrypt possuem referências, políticas, rotação e auditoria próprias.

### Custódia

- desenvolvimento/teste: chaves efémeras, geradas por processo/fixture, sem
  retenção;
- ambientes integrados: vault/KMS/HSM não produtivo e workload identity;
- produção: serviço homologado, política least-privilege, private key
  não exportável onde suportado e auditoria externa;
- persistência DB-Notifier: somente referência opaca, thumbprint público,
  finalidade, versão e estado do lifecycle.

Não haverá chave privada em `appsettings`, variável persistida, PostgreSQL,
SQLite, pacote, arquivo de exemplo, screenshot, relatório ou log.

### Rotação e comprometimento

1. criar/admitir nova versão no custodiante;
2. distribuir a chave pública/JWKS e confirmar disponibilidade;
3. habilitar validação/decriptação sobreposta;
4. mudar produção de novos artefatos para a nova versão;
5. aguardar lifetime máximo, filas e caches;
6. retirar produção antiga;
7. retirar validação/decriptação antiga;
8. provar recusa e encerrar a auditoria.

Comprometimento ignora a cadência comum: revoga, invalida cache, coloca
artefatos/consumidores afetados em quarentena, reemite material e exige
autoridade humana/auditoria próprias.

### JWKS, cache e egress

- JWKS externo continua limitado ao issuer OIDC configurado e à política
  `human-identity`;
- um futuro JWKS próprio publica apenas chaves públicas necessárias;
- documento, quantidade de chaves, headers HTTP, media type e tempo são
  limitados;
- ETag/Cache-Control e overlap de rotação são testados;
- ausência, staleness ou conflito não cai para trust amplo;
- redirect, proxy ambiente, credential forwarding e origem cruzada
  permanecem recusados;
- HA e outage do IdP/KMS/HSM possuem modo fail-closed e alertas.

## Dados e persistência

Se os casos aprovados exigirem metadados duráveis, um incremento de dados
deverá propor entidades equivalentes a:

- `CryptographicKeyReference`;
- `JoseProtectionProfileRevision`;
- `JoseKeyBinding`;
- `JoseReplayRecord`, somente para finalidade que exija uso único; e
- eventos de rotação/revogação no audit append-only.

Não haverá tabela de chave privada, symmetric key, token, plaintext, JWE ou
claims completas. Constraints deverão impor finalidade, estado, janela
temporal, unicidade de referência/versão e transições válidas. Migrations
serão não produtivas até autorização própria.

## Desenvolvimento

### Estratégia de dependências

1. Avaliar a capacidade real de `Microsoft.IdentityModel.JsonWebTokens` e
   `Microsoft.IdentityModel.Tokens` já presentes transitivamente.
2. Comparar suporte a Compact/Flattened/General, multi-signature,
   multi-recipient, detached payload, JWE, RFC 9864, streaming/bounds e
   plataformas alvo.
3. Tornar dependências necessárias diretas e centralmente versionadas; não
   depender acidentalmente de pacote transitivo.
4. Se houver lacuna, comparar bibliotecas JOSE maduras, mantidas e
   licenciadas, com SBOM, advisories, testes vetoriais e compatibilidade .NET
   10.
5. Proibir implementação própria de AES, RSA, ECDSA, EdDSA, KDF, MAC ou
   comparação criptográfica.

Nenhuma biblioteca está escolhida por esta proposta. Qualquer nova
dependência ou download exige autoridade separada e verificação de supply
chain.

### Sequência proposta de oito lotes

| Lote | Objetivo | Saída obrigatória |
|---|---|---|
| `JOSE-0` | arquitetura e segurança | ADR-0008 decidido, perfil normativo, coverage matrix, threat/data classification |
| `JOSE-1` | spike de biblioteca e contratos | compatibilidade .NET 10, vetores oficiais, decisão de dependência e portas Application |
| `JOSE-2` | JWS completo | Compact/Flattened/General, detached, multi-signature, hardening JWT existente |
| `JOSE-3` | JWK/JWKS e ciclo de chave | resolver, thumbprint, cache, rotação, revogação e fixtures sem segredo |
| `JOSE-4` | JWE completo | Compact/Flattened/General, AAD, multi-recipient e nested JWT |
| `JOSE-5` | integração de produto | IdP/login e cada artefato aprovado, separadamente guardados |
| `JOSE-6` | infraestrutura e homologação | KMS/HSM/vault/IdP não produtivos, HA, outage, interoperabilidade, carga e segurança |
| `JOSE-7` | release controlado | runbooks, SBOM, operação, rollback, Human Gate e rollout autorizado |

Cada lote possui autorização, diff, relatório, automatic gate e Human Gate
próprios. Falha num lote não autoriza pular para o seguinte. JWE não precisa
esperar login operacional se usar somente fixtures, mas nenhuma integração
real ocorre antes dos seus pré-requisitos.

## Documentação a incorporar após aprovação

| Documento | Mudança proposta |
|---|---|
| `AGENTS.md` | regra permanente curta somente se a política JOSE for aceita como norma do projeto |
| `prompts/foundation/Solution-Architecture-Document.md` | trust boundaries, componentes e responsabilidades |
| `prompts/governance/Security-And-Access.md` | perfis, claims, headers, chaves, rotação, auditoria e recuperação |
| `prompts/governance/Lifecycle.md` | entregáveis/gates JOSE nas fases proprietárias, sem lifecycle paralelo |
| `prompts/state/Current-State.md` | somente implementação/evidência factual alcançada |
| `docs/architecture/ADR-0008-*` | decisão, alternativas e consequências |
| `docs/architecture/JOSE-Cryptographic-Profile.md` | matriz normativa por finalidade, algoritmo, header e serialização |
| `docs/architecture/JOSE-Key-Lifecycle.md` | custódia, distribuição, cache, rotação, revogação e compromisso |
| `docs/architecture/Threat-Model.md` | ameaças JOSE e control owners |
| `docs/architecture/Network-Egress-Policy.md` | IdP/JWKS/KMS endpoints e proibição de URL recebida |
| `docs/architecture/Canonical-Contracts.md` | contratos públicos somente quando aprovados |
| `docs/data/README.md` | metadados não secretos e migrations, se necessários |
| runbooks de operação/release | provisioning, rotação, outage, compromisso, rollback e disaster recovery |
| relatórios por lote | comandos, versões, matriz, resultados, limitações e evidência sanitizada |

A documentação normativa será atualizada junto do lote dono; não será
preenchida antecipadamente como se a capacidade estivesse implementada.

## Threat model proposto

| Ameaça | Controle mínimo | Evidência |
|---|---|---|
| `alg=none` ou downgrade | allowlist por finalidade, `none` proibido | vetores negativos |
| confusão RSA/HMAC ou key type | binding algoritmo/key type/key ops | matriz adversarial |
| cross-JWT substitution | `typ`, issuer, audience e regras mutuamente exclusivas | tokens cruzados |
| `kid` injection/path abuse | lookup opaco, limitado e não usado como path/SQL | fuzz e injection |
| SSRF por `jku`/`x5u` | URLs recebidas sem autoridade; egress preconfigurado | zero-hit externo |
| JWK/x5c atacante vira trust | trust source fora do objeto | chave embutida recusada |
| duplicate JSON/header smuggling | parser estrito e nomes únicos | corpus duplicado |
| signature stripping | política exata de assinaturas/quórum | assinatura ausente/reordenada |
| padding/decryption oracle | erro uniforme, limites e métricas sem detalhe | timing/disclosure review |
| nonce GCM repetido | CSPRNG e ownership atómico de contador/geração | concorrência/restart |
| zip bomb/side channel | `zip` proibido na baseline | recusa antes de expandir |
| ciphertext/plaintext oversized | budgets antes/depois de decode/decrypt | limites e carga |
| replay | `jti`/nonce/idempotência por finalidade | duplicação/expiração |
| key rotation race | overlap, cache fencing e version binding | rotação concorrente |
| key compromise | revogação, quarantine e reissuance | tabletop/runbook |
| log/telemetry leakage | códigos sanitizados e canary scan | secret scan |
| KMS/IdP outage | fail-closed, LKG público limitado e alerta | falha/recuperação |
| multi-tenant key confusion | tenant/purpose/audience binding | cross-scope negatives |
| library regression | locked dependencies, SBOM, advisories e differential tests | CI e release gate |

## Validação e evidência

### Testes automatizados

- vetores oficiais RFC 7515/7516/7517/7518/7638/7797/8725/9864;
- round-trip de cada serialização autorizada;
- interoperabilidade com ao menos duas implementações independentes ou um
  IdP/custodiante e vetores oficiais, conforme o caso;
- casos negativos de algoritmo, key type, curve, size, issuer, audience,
  lifetime, `crit`, `typ`, `cty`, `kid`, `jku`, `jwk`, `x5u`, `x5c`;
- JSON/base64url malformado, duplicado, profundo, grande e com Unicode
  adversarial;
- multi-signature/quórum e multi-recipient sem aceitação parcial favorável;
- tamper em header, AAD, ciphertext, tag, assinatura e detached payload;
- nesting excessivo, compression refusal e decryption error uniformity;
- rotação, revogação, cache stale, rollback, clock skew e outage;
- concorrência de emissão, nonce/IV, cache refresh e key rotation;
- canary scan em logs, audit, traces, dumps e relatórios;
- fuzz/property tests com budgets explícitos; e
- architecture tests provando ausência de JOSE no Domain/providers e de chave
  em configuração/persistência comum.

### Homologação

- Windows e Linux para Server/Agent aplicável;
- .NET 10 nas arquiteturas de CPU suportadas;
- IdP e fluxo OIDC exatos;
- KMS/HSM/vault por fornecedor, versão, região/topologia e operação;
- algoritmos/curvas/tamanhos exatos;
- proxy/DNS/TLS/cache/HA/outage;
- performance de verify/sign/encrypt/decrypt com payloads máximos;
- rotação normal e recuperação de comprometimento;
- backup/restore e reconciliação com chave externa; e
- penetration test e revisão criptográfica independente antes de produção.

Um caso aprovado não homologa outro algoritmo, IdP, custodiante, plataforma
ou finalidade.

## Observabilidade e auditoria

Podem ser registrados:

- operação, finalidade e profile version;
- algoritmo e serialização permitidos;
- referência pública/opaca sanitizada ou thumbprint público;
- issuer/tenant em forma aprovada e limitada;
- resultado, código estável, latência, cache age e correlation ID; e
- rotação/revogação/compromisso e ator autorizado.

São proibidos em logs/audit/telemetria:

- token completo;
- payload/claims irrestritas;
- plaintext ou ciphertext;
- tag, CEK, KEK, chave privada/simétrica;
- JWK privado, connection string, segredo ou resposta bruta do custodiante; e
- exception que revele material ou permita oracle.

## Compatibilidade e migração

1. Congelar testes do comportamento JWT/OIDC atual.
2. Introduzir o profile humano em modo equivalente, ainda sem emitir token.
3. Fixar issuer, audience, `typ`, claims e algoritmos aceitos por IdP.
4. Manter `RS256` somente se necessário para compatibilidade documentada.
5. Adicionar algoritmo preferido após IdP e custodiante passarem
   interoperabilidade.
6. Observar a janela acordada sem registrar tokens.
7. Remover o algoritmo antigo somente após zero consumidor dependente,
   rollback ensaiado e Human Gate.
8. Adicionar JWS/JWE de produto por caso de uso, nunca por habilitação global.

Rollback desativa o novo producer/consumer por profile version e restaura a
última política aceita, sem reativar algoritmo proibido, sem reintroduzir
chave local e sem aceitar objeto emitido sob finalidade incompatível.

## Critérios de aceite globais

A capacidade JOSE somente poderá ser chamada de completa quando:

- ADR-0008 estiver aceito por segurança/arquitetura;
- coverage matrix tiver 100% das entradas aplicáveis classificadas;
- todas as serializações JWS/JWE estiverem implementadas ou explicitamente
  rejeitadas com razão e teste;
- JWK/JWKS, thumbprint, cache, rotação, revogação e compromisso estiverem
  provados;
- pelo menos um profile JWS e um profile JWE passarem interoperabilidade;
- nenhum algoritmo for selecionado por conteúdo não confiável;
- nenhum segredo/chave privada for encontrado em código, config,
  persistência, logs ou evidência;
- threat model não tiver achado Critical/High aberto no escopo;
- cobertura automática mantiver os pisos do repositório e componentes
  críticos receberem cobertura superior baseada em risco;
- carga, fuzz, negative tests, outage, rollback e cross-platform passarem;
- IdP e KMS/HSM/vault reais forem homologados somente sob autoridade
  específica;
- runbooks e observabilidade estiverem completos; e
- automatic gate e Human Gate do lote final forem aprovados.

“Biblioteca suporta JWE”, “teste unitário passou” ou “JWT funciona” não
satisfazem esse aceite.

## Riscos e mitigação

| Risco | Mitigação |
|---|---|
| escopo excessivo | oito lotes independentes, use cases antes de primitives |
| falsa completude por algoritmo | coverage matrix IANA versionada |
| lock-in de KMS/IdP | portas provider-neutral e homologação por adapter |
| complexidade de General JSON | spike/interoperabilidade antes de arquitetura definitiva |
| chave em aplicação | referência opaca e operação no KMS/HSM quando possível |
| regressão do JWT atual | golden/negative tests e migração por profile |
| custo/DoS criptográfico | admission budgets, tamanhos, rate limit e benchmark |
| criptografia usada para esconder design inseguro | data classification e minimização antes de JWE |
| mistura com mTLS/RBAC | trust boundaries e responsabilidades separadas |
| norma/registro evolui | revisão IANA/RFC em dependency e release gates |

## Decisões humanas necessárias

Antes de `JOSE-0`, o proprietário deverá decidir separadamente:

1. se aceita a definição de “JOSE completo” baseada em cobertura e rejeição
   explícita, não em habilitar todo algoritmo;
2. quais casos de uso além da autenticação humana justificam JWS/JWE;
3. se o Server continuará apenas como relying party humano ou também
   produzirá artefatos JOSE próprios;
4. quais ambientes/tenants entram na primeira homologação;
5. qual classe de custodiante será avaliada primeiro: OS vault, KMS cloud,
   HSM corporativo ou combinação; e
6. se autoriza somente `JOSE-0` documental ou algum spike separado de
   `JOSE-1`.

Nenhuma dessas decisões é inferida desta solicitação de proposta.

## Autorização sugerida para o próximo passo

Caso a proposta seja aceita apenas para decisão arquitetural, a autorização
segura e limitada é:

```text
AUTORIZO exclusivamente o lote documental STATE-06 JOSE-0 — Architecture,
Security and Coverage Design, limitado à decisão do ADR-0008, perfil
criptográfico normativo, matriz completa dos registros JOSE/IANA, mapa de
casos de uso e trust boundaries, data classification, threat model, plano de
chaves/rotação/compromisso, rastreabilidade e plano de testes. Permanecem
proibidos código, dependências, restore/download, migrations, runtime,
IdP/login real, chave/certificado, vault/KMS/HSM, banco/serviço externo,
deploy, publicação, transição de lifecycle e ativação MOD-12.
```

Essa autorização não deve ser usada se o proprietário pretender autorizar
desenvolvimento ou infraestrutura; esses escopos exigem textos separados
depois da decisão `JOSE-0`.
