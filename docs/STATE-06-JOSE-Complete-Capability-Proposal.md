# Proposta Revisada STATE-06 — Capacidade JOSE Completa e Governada

## Status e autoridade

- Data: 2026-07-28
- Versão da proposta: `jose-complete-capability-proposal-1.2.0`
- Substitui: versão inicial registrada no commit
  `ef6b28a12cd5547e64549c1335c7f82420388ced`
- Lifecycle atual: `STATE-06 INTEGRATION`
- Status: `PROPOSTA DOCUMENTAL — NÃO AUTORIZADA PARA IMPLEMENTAÇÃO`
- Baseline técnica imediatamente anterior à revisão `1.1.0`: commit
  `46746c72983888c27d741b25775a21adebc29ccb`
- Baseline observada no início do lote `JOSE-0`: commit
  `6d0bbdc68c5c080d11fccd7a6fcd540908440903`
- ADR associado:
  [ADR-0008 — JOSE Cryptographic Profiles and Key Lifecycle](architecture/ADR-0008-JOSE-Cryptographic-Profiles-And-Key-Lifecycle.md),
  revision `1.2`, com status `proposed`
- `JOSE-0`: pacote aceito exclusivamente como preparação documental; proposta
  e ADR não aceitos

A autorização posterior e mais específica de `JOSE-0` permitiu somente o
desenho documental descrito no
[relatório JOSE-0](STATE-06-JOSE-0-Architecture-Security-And-Coverage-Design-Report.md),
no
[profile técnico](architecture/JOSE-Security-Profile-And-Key-Lifecycle.md) e
na
[matriz IANA](architecture/JOSE-IANA-Registry-Coverage.md).
Ela não aceita esta proposta nem o ADR e não autoriza código, configuração
executável, dependência, migration, IdP, login operacional, chave,
certificado, vault, KMS, HSM, banco, serviço externo, runtime, deploy,
publicação, transição de lifecycle, `JOSE-1` ou ativação do MOD-12.

## Resumo executivo

Propõe-se transformar a validação JWT/OIDC já existente em uma capacidade
JOSE governada, versionada e orientada por finalidade. A entrega final
cobrirá assinatura (JWS), criptografia (JWE), representação e distribuição de
chaves (JWK/JWKS), algoritmos (JWA), JWT e as serializações JOSE definidas
para o escopo.

“Completa” terá três significados separados e auditáveis:

1. **cobertura normativa completa**: toda entrada do snapshot IANA e todo
   recurso dos RFCs adotados possui classificação e justificação;
2. **implementação completa do perfil**: tudo o que o perfil classifica como
   adotado ou adaptado está implementado, limitado e testado; e
3. **suporte operacional completo**: somente os perfis, finalidades,
   plataformas e custodiantes homologados podem ser anunciados como
   suportados.

Uma classificação `Scheduled`, um sandbox ou uma biblioteca capaz não
constitui suporte. Algoritmos inseguros, inadequados ou sem caso de uso podem
ser rejeitados sem criar uma lacuna oculta, mas as serializações nucleares
JWS/JWE Compact, Flattened e General precisam ser comprovadas no boundary
test-only antes de qualquer alegação interna de implementação JOSE completa.

A proposta preserva quatro limites:

1. o DB-Notifier não se torna automaticamente um provedor de identidade ou
   emissor de senhas/tokens humanos;
2. JOSE não substitui HTTPS, mTLS dos Agents, RBAC server-side,
   idempotência, auditoria ou referências opacas de segredo;
3. nenhum segredo ou chave privada entra no repositório ou na persistência
   comum; e
4. nenhuma biblioteca, IdP ou infraestrutura é escolhida apenas por declarar
   suporte genérico a JOSE.

A expressão “JOSE completo” permanece um objetivo interno de engenharia. Ela
não pode aparecer como claim de produto, suporte público ou prontidão
operacional antes de `STATE-08`, homologação da matriz exata e Human Gate de
release específico.

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

### Baseline normativa

A baseline distingue núcleo JOSE, extensões que já alimentam os registros IANA
e perfis de aplicação. Um RFC de perfil só se torna normativo para o caso de
uso que o adotar; ele não amplia silenciosamente a superfície JOSE comum.

| Classe | Padrão | Cobertura proposta |
|---|---|---|
| Núcleo | RFC 7515 — JWS | Compact, Flattened JSON, General JSON, múltiplas assinaturas e detached payload |
| Núcleo | RFC 7516 — JWE | Compact, Flattened JSON, General JSON, AAD, múltiplos destinatários e nested content |
| Núcleo | RFC 7517 — JWK | JWK/JWKS público, seleção limitada, `use`, `key_ops`, `kid` e tipos autorizados |
| Núcleo | RFC 7518 — JWA | algoritmos, key management, content encryption e tipos de chave |
| Núcleo | RFC 7519 — JWT | claims, validação temporal, issuer/audience/subject, `jti` por finalidade e nested JWT |
| Extensão | RFC 7638 | thumbprint de material JWK público; não prova proveniência, finalidade ou versão |
| Extensão | RFC 7797 | `b64=false` test-only por opt-in; sempre proibido para JWT |
| Extensão | RFC 8037 | `OKP`, EdDSA e ECDH com curvas CFRG, todos sujeitos a perfil e homologação |
| Extensão | RFC 8812 | `secp256k1` inventariado, inicialmente `Rejected + RuntimeDisabled` |
| Segurança | RFC 8725 | baseline contra algorithm confusion, substitution e cross-JWT confusion |
| Extensão | RFC 9864 | identificadores totalmente especificados e semântica de `Deprecated`/`Prohibited` |
| Extensão | RFC 9964 | `AKP` e `ML-DSA-44/65/87`, inicialmente `Scheduled + RuntimeDisabled` |
| Vetores | RFC 7520 | somente partes públicas/não secretas e expected outputs sanitizados; exemplos legados/inseguros servem a parsing/recusa, não a habilitação |
| Registro | IANA JOSE registries | inventário factual versionado; toda entrada classificada, nenhuma habilitação automática |
| Registro | IANA JWT Claims registry | inventário de nomes; cada claim é classificada por token type e nunca ganha autoridade só por registro |
| Perfil de aplicação | RFC 9700 | BCP aplicável a todo deployment OAuth 2.0 adotado, inclusive access token humano atual; browser acrescenta requisitos próprios |
| Perfil condicional | RFC 9068 | `typ=at+jwt` e regras próprias se o IdP fornecer JWT access token conforme esse perfil |

Referências primárias:
[JWS](https://www.rfc-editor.org/rfc/rfc7515.html),
[JWE](https://www.rfc-editor.org/rfc/rfc7516.html),
[JWK](https://www.rfc-editor.org/rfc/rfc7517.html),
[JWA](https://www.rfc-editor.org/rfc/rfc7518.html),
[JWT](https://www.rfc-editor.org/rfc/rfc7519.html),
[JOSE Cookbook](https://www.rfc-editor.org/rfc/rfc7520.html),
[algoritmos CFRG](https://www.rfc-editor.org/rfc/rfc8037.html),
[secp256k1](https://www.rfc-editor.org/rfc/rfc8812.html),
[JWT BCP](https://www.rfc-editor.org/rfc/rfc8725.html),
[algoritmos totalmente especificados](https://www.rfc-editor.org/rfc/rfc9864.html),
[ML-DSA para JOSE](https://www.rfc-editor.org/rfc/rfc9964.html),
[OAuth Security BCP](https://www.rfc-editor.org/rfc/rfc9700.html),
[JWT access-token profile](https://www.rfc-editor.org/rfc/rfc9068.html),
[registros JOSE da IANA](https://www.iana.org/assignments/jose/jose.xhtml)
e [registro de claims JWT da IANA](https://www.iana.org/assignments/jwt/jwt.xhtml).

Os snapshots IANA usados por `JOSE-0` registram URL, campo `Last Updated`,
data/hora de aquisição, bytes e SHA-256 na
[matriz de cobertura](architecture/JOSE-IANA-Registry-Coverage.md). As fontes
oficiais capturadas informam `Last Updated: 2026-05-22` para JOSE e
`2026-07-20` para JWT Claims. Uma entrada nova ou alterada depois desses
hashes assume
`Unreviewed + RuntimeDisabled` até nova análise; o runtime nunca consulta a
IANA para decidir política. `AKP`, `ML-DSA-44`, `ML-DSA-65` e `ML-DSA-87`
já pertencem a esse snapshot e não podem ser tratados como drafts. Parâmetros
JWK de lifecycle recebidos, como datas ou estado de revogação, permanecem
dados não confiáveis e não ganham autoridade sobre o lifecycle DB-Notifier
sem um protocolo adotado que defina seu emissor e sua semântica.

Internet-Drafts ativos sobre depreciação de `none`/`RSA1_5`, HPKE, mecanismos
pós-quânticos adicionais e JSON Proof pertencem a uma watchlist não
normativa. O RFC-to-be 10017, sobre aplicações OAuth em browser, encontrava-se
em
[revisão final do RFC Editor](https://queue.rfc-editor.org/final-review/rfc10017/)
nesta revisão e também permanece informativo até publicação. Esta proposta já
rejeita `none` e `RSA1_5` por política própria mais estrita, mas não apresenta
um draft como RFC, não implementa identificador provisório e não promete
compatibilidade futura antes de publicação, caso de uso e nova decisão.

### Matriz funcional e factual obrigatória

Cada linha da matriz terá eixos independentes; nenhum status pode ser inferido
de outro:

| Eixo | Valores mínimos |
|---|---|
| status no registro | `Required`, `Recommended`, `Optional`, `Deprecated`, `Prohibited`, `NotSpecified`, `NotApplicable` ou o valor factual da IANA |
| decisão DB-Notifier | `Adopted`, `SafelyAdapted`, `Rejected`, `Unreviewed` |
| roadmap | `NotPlanned`, `Scheduled(lote)`, `Delivered` |
| implementação | `NotImplemented`, `TestHarnessOnly`, `Implemented` |
| verificação | `NotTested`, `VectorTested`, `Interoperable` |
| homologação | `NotHomologated`, `Homologated(escopo)` |
| runtime | `RuntimeDisabled`, `OperationalCandidate`, `Enabled(ambiente)` |
| autorização | identificador da decisão/gate que permite a condição atual |
| suporte público | `NotAdvertised` ou claim exato aprovado |

`Adopted` significa selecionado para uma finalidade; `SafelyAdapted` limita
compatibilidade inbound ou um perfil estreito, com sunset; `Rejected`
documenta recusa deliberada; `Unreviewed` falha fechado. `Scheduled` existe
somente no eixo roadmap. Somente uma combinação explicitamente autorizada,
homologada e habilitada pode fundamentar suporte público.

A matriz também registra snapshot normativo, finalidade, direção,
serialização, algoritmo, key type/curva, headers permitidos, biblioteca,
custodiante, owner, sunset, última revisão e links de evidência. Thumbprint é
fingerprint de material público, não identificador de proveniência, tenant,
purpose ou versão de lifecycle.

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

Para evitar uma completude nominal, o gate final calculará separadamente:

- `classified/registry-total`;
- `implemented/(Adopted+SafelyAdapted)-total`;
- `verified/implemented-total`;
- `homologated/authorised-operational-combinations-total`;
- `enabled/authorised-and-homologated-combinations-total`;
- `approved-and-accurate-public-claims/public-claims-total` (`N/A` quando
  nenhum claim existir); e
- quantidade e nomes de todos os itens `Unreviewed`, `Scheduled`, `Rejected`,
  vencidos ou temporariamente adaptados.

Nenhum denominador pode omitir uma entrada difícil, desconhecida ou
incompatível.

## Casos de uso, owners e decisões de fronteira

| Caso | Decisão inicial | Owner e fluxo | Trust boundary | Autoridade vigente e mudança necessária | Lote |
|---|---|---|---|---|---|
| JWT access token humano na API | `Adopted`, inbound only | MOD-01/MOD-11; IdP externo → Server API | internet/IdP → API | `Security-And-Access`; endurecer o relying party sem emitir token | `JOSE-2`, `JOSE-5A` |
| Login e sessão do Dashboard | `Scheduled` | MOD-01/MOD-09/MOD-11; browser ↔ BFF/IdP ↔ API | browser, backchannel do Server e IdP | subdecisão de browser identity; reconciliar passwords, sessão e RBAC | `JOSE-5A/B/C` |
| Enrollment token do Agent | `Unreviewed + RuntimeDisabled` | MOD-03; operador/provisionador → Agent → Server | entrega out-of-band e enrollment endpoint | ADR-0002 continua autoridade; classificar formato e replay sem alterar mTLS | `JOSE-5D`, somente se adotado |
| Identidade e transporte do Agent | `Rejected` como substituição | MOD-03; Agent ↔ Server | HTTPS/mTLS | ADR-0002/0003 permanecem inalterados | nenhum |
| Comando administrativo/outbox | `Rejected` por padrão | MOD-04/MOD-11; Server → Agent → resultado | protocolo durável e operação offline | qualquer camada JOSE exige revisão de ADR-0003, `Agent-API-Protocol.md`, schema version, outbox e idempotência | sublote próprio de `JOSE-5D` |
| Artefato de controle assinado | `Scheduled` por artefato concreto | módulo que cria a decisão → consumidor nomeado | definida pelo artefato | JWS prova origem/integridade; não cria autorização, aprovação ou Human Gate | sublote próprio de `JOSE-5` |
| Envelope confidencial | `Scheduled` | data owner → consumidor exato | definida após classificação | somente quando TLS e encryption-at-rest forem insuficientes; store/retention próprios | `JOSE-4` + sublote próprio de `JOSE-5` |
| Evidência multi-signature | `Scheduled` | autoridades independentes → verificador | uma custódia por signatário | quórum é regra criptográfica; dual control/RBAC/decisão continuam externos | `JOSE-2` + sublote próprio |
| Manifests e updates | `Scheduled` como camada adicional | release authority → Agent/Desktop | canal de distribuição | ADR-0005 continua autoridade; JOSE não substitui assinatura de pacote/plataforma | revisão de ADR-0005 antes de `JOSE-5` |
| MOD-12 | `Scheduled` somente como possível codec de infraestrutura | owner futuro do artefato → verificador puro | fronteira aprovada pelo ADR-0007 | ADR-0007 prevalece; JOSE nunca vira root selector, checkpoint, publisher, aprovação, rede ou ativação `None → Observer` | ADR próprio antes de qualquer sublote |
| Credencial de provider | `Rejected` | vault do Agent → provider adapter | Agent → vault/provider | ADR-0002 e referência opaca; token/segredo de database não passa por JOSE | nenhum |

As duas primeiras entregas recomendadas são:

1. endurecer a validação JWS humana existente, ainda como relying party e sem
   emissão própria; e
2. comprovar as seis serializações nucleares JWS/JWE em harness test-only
   interoperável, sem dados, identidades, chaves ou infraestrutura
   operacionais.

Emissão própria, JWE de produto, Agent, packages e MOD-12 entram depois e de
forma independente. Nenhuma linha `Scheduled` constitui autorização.

### Subdecisão obrigatória de identidade humana

Antes de `JOSE-5A`, MOD-01/MOD-11 deverá decidir e atualizar
`Security-And-Access.md` com:

- identidade canônica `(issuer, subject)`, nunca email, UPN ou display name;
- allowlist de issuer/tenant e regras de pre-provisioning ou JIT,
  desativação, offboarding e colisão de identidade;
- mapping server-side de claims para `User`, `RoleAssignment` e escopo; o
  Dashboard não decide autorização a partir do token;
- distinção rígida entre ID token, access token e session identifier; a API
  recusa ID token como access token;
- quando adotado RFC 9068, `typ=at+jwt`, issuer e audience exatos;
- BFF com Authorization Code + PKCE `S256` como opção recomendada para
  avaliação, ou justificativa formal para token-mediating backend/SPA;
- `state`, `nonce`, redirect URI exato, mix-up, CORS, CSRF, cookies
  `HttpOnly`/`Secure`/`SameSite`, rotação e logout;
- MFA e política de `acr`/`amr` quando o risco exigir;
- TTL, refresh/session invalidation, offboarding e a limitação factual de
  revogação de JWT autocontido; e
- decisão explícita sobre o suporte de passwords atualmente descrito na
  autoridade de segurança. JOSE não o remove por inferência.

`localStorage`, `sessionStorage`, query string, fragment persistido, log ou
telemetria não podem armazenar token. Uma SPA autorizada manteria tokens
somente em memória. Logout de sessão não prova revogação do token; TTL curto,
introspection, denylist, continuous access evaluation ou reautenticação
precisam ser escolhidos por perfil.

## Arquitetura proposta

```text
External IdP                    Approved KMS/HSM/Vault
    | OIDC/JWKS                           |
    v                                     v
Server/API composition ---- Security and Identity boundary
    | trusted policy id         | immutable JOSE profiles
    | semantic operation        | bounded parser/serialiser
    |                           | public trust/key-operation adapters
    v                           | canonical security outcome/audit intent
Application use cases <---------+
    | business authorisation, replay/idempotency and canonical audit
    v
versioned API contracts <----> Dashboard/BFF

Agent -- HTTPS/mTLS + durable ADR-0003 protocol --> Server/API
  |
  +-- provider adapters --> monitored databases
```

### Responsabilidades

| Camada/componente | Possui | Não pode possuir |
|---|---|---|
| Domain | regras provider-neutral | token, algoritmo, header, chave, IdP, KMS ou tipo JOSE |
| Application | finalidade de negócio, replay/idempotência, autorização, referência opaca de policy e resultado canônico | `Jose*`, serialização, algoritmo, key descriptor, parser ou tipo IdentityModel |
| Security/Identity Infrastructure | profiles JOSE, formatos, limites, parsing, trust resolution, key-operation e adapters aprovados | regra de negócio, RBAC ou finalidade escolhida pela entrada |
| Server API/BFF | composição confiável rota → policy, sessão, autenticação, quotas e resposta externa uniforme | endpoint genérico de sign/decrypt, senha/chave em configuração ou issuer genérico |
| MOD-11 | `AuditEntry` canônico, RBAC e retenção | trilha JOSE paralela, token, locator/ARN ou conteúdo protegido |
| Dashboard | início/retorno da sessão e apresentação de denied/expired | inspeção de access token para RBAC, token durável ou chave |
| Agent | mTLS e contratos duráveis existentes | token humano ou substituição do ADR-0003 por envelope genérico |
| Provider | conexão de database e capability própria | identidade humana, JOSE ou material de chave |

Architecture tests deverão provar que Domain e Application não referenciam
IdentityModel, tipos criptográficos concretos ou namespaces JOSE.

### Contratos iniciais no boundary de segurança

Os nomes finais dependerão do spike, mas as responsabilidades mínimas serão:

- `ProtectionPolicyId`: referência opaca, estável e versionada fornecida pela
  composição confiável; Domain/Application não conhecem seu conteúdo JOSE;
- `JoseProtectionProfile`: tipo interno ao boundary, com algoritmos,
  serializações, headers, schema, trust source, revision e limites numéricos
  imutáveis;
- portas semânticas por caso, como validação de access token humano ou
  proteção de um artefato tipado; não haverá `IJoseSigner`/`IJoseDecryptor`
  genérico exposto a rotas ou ao núcleo;
- `IJosePublicTrustResolver`: material exclusivamente público e admitido no
  trust source;
- `IKeyOperationProvider`: capabilities separadas `sign`, `unwrap/decrypt`,
  `wrap/encrypt` quando remoto, por referência opaca e policy do custodiante;
- `SecurityOperationOutcome`: resultado canônico e sanitizado; detalhes
  criptográficos ficam restritos ao boundary; e
- intenção de auditoria que estende o `AuditEntry` canônico MOD-11, sem novo
  store e sem token, conteúdo, locator/ARN ou chave.

O servidor escolhe schema, payload canônico, headers, destinatários,
signatários, profile e key binding. O chamador não fornece operação
criptográfica livre. Autorização, rate limit e quota por purpose/key ocorrem
antes do custo; respostas externas de verify/decrypt falham de modo uniforme.
Esse desenho evita signing/decryption oracle e não exporta chaves para
Domain/Application.

Uma análise decidirá se o boundary permanece em `DBNotifier.Infrastructure`
ou justifica `DBNotifier.Infrastructure.Security.Jose`. Nenhum novo projeto
será criado somente por organização estética.

## Perfil de segurança proposto

### Seleção e parsing

- A rota/caso de uso seleciona o perfil antes de interpretar o objeto.
- `alg`, `enc` e tipo do token nunca escolhem a política.
- JSON duplicado, base64url inválido, header crítico desconhecido, tamanho,
  profundidade, cardinalidade ou nesting excessivo falham antes do trabalho
  criptográfico caro.
- `jku` e `x5u` recebidos não iniciam rede; `jwk`/`x5c` embutidos não viram
  trust anchor.
- `kid` é apenas hint limitado dentro do trust source preconfigurado; valor
  ausente só é aceito quando issuer/profile/alg/kty/use/key_ops/status deixam
  uma única chave elegível. Valor duplicado, ambíguo ou acima do limite falha
  sem tentativa ilimitada de todas as chaves.
- nomes repetidos entre protected, shared unprotected e per-recipient/
  per-signature headers são recusados conforme a serialização aplicável.
- em JWS, `alg`, `crit`, `typ`, `cty` e todo valor usado pela policy ficam
  protegidos. `crit` é uma lista limitada, sem duplicatas, e cada extensão
  precisa ser conhecida, permitida pelo profile e processada integralmente.
- em JWE, `enc`, `zip`, `typ`, `cty` e propriedades compartilhadas ficam
  protegidas. Compact e single-recipient exigem `alg` protegido. General JWE
  multi-recipient permanece test-only na baseline e usa um único `alg`
  compartilhado; qualquer futuro profile heterogêneo deverá tratar `alg`,
  `kid`, `epk`, `apu` e `apv` per-recipient conforme RFC 7516, sem permitir
  que valores não autenticados escolham trust ou policy.
- se a identidade, cardinalidade ou ordem do recipient roster tiver semântica
  de autorização, ela será ligada por protected header/AAD ou pelo envelope
  interno assinado e comparada à policy. Conseguir
  unwrap/decrypt/autenticação por um recipient path, isoladamente, não prova
  que o roster original foi preservado.
- `b64=false` é sempre recusado para JWT; em JWS detached não-JWT permanece
  test-only e somente sob profile próprio.
- payload detached é verificado sobre os bytes exatos fornecidos pelo
  protocolo; parse/re-serialização JSON não cria um payload “equivalente”.
- `JOSE-0` define targets conservadores e safety caps provisórios para bytes
  codificados/decodificados, headers, claims, assinaturas, destinatários,
  chaves candidatas, nesting, tempo e memória do spike. `JOSE-1` mede
  `N-1/N/N+1`; `JOSE-D1` revisa e congela os valores aceitos. Nenhum parser de
  produto pode existir antes dessa decisão.

### Assinatura

- baseline candidata de produção: `PS256` com RSA de no mínimo 2048 bits ou
  `ES256` com P-256 e assinatura JOSE `R || S` de 64 bytes, conforme
  IdP/custodiante e política criptográfica aprovada;
- `RS256` somente inbound, RSA de no mínimo 2048 bits, issuer exato e
  compatibilidade com sunset explícito;
- `Ed25519` permanece `Scheduled` até a cadeia completa suportar o
  identificador do RFC 9864;
- `AKP` e `ML-DSA-44/65/87` permanecem `Scheduled + RuntimeDisabled` até
  haver caso, suporte interoperável, validação de parâmetros e budgets que
  considerem suas chaves/assinaturas maiores;
- `none` sempre rejeitado;
- HMAC entre trust boundaries independentes rejeitado;
- `x5t` baseado em SHA-1 rejeitado; `x5t#S256` só pode ser usado como binding
  adicional dentro de trust já estabelecido;
- multi-signature exige conjunto/quórum predefinido e signatários distintos;
- conteúdo detached exige binding exato de bytes e content type.

Cada key version fica ligada a um único algoritmo, finalidade, tenant/ambiente
e direção. Migração `RS256 → PS256` não reutiliza silenciosamente o mesmo par
RSA; qualquer exceção exigiria prova criptográfica e decisão explícita.
`JOSE-0` propõe candidatos para modulus/exponent RSA, parâmetros PSS, curvas,
encoding ECDSA e testes de chave inválida. `JOSE-1`, se vier a ser autorizado,
mede esses candidatos; somente `JOSE-D1` poderá congelar os valores aceitos.

### Criptografia

- baseline candidata: `RSA-OAEP-256` com RSA de no mínimo 2048 bits e
  `A256GCM` com IV de 96 bits e authentication tag de 128 bits;
- ECDH-ES permanece `Scheduled` até haver política exata de curva,
  validação de ponto/chave efémera, KDF, key agreement e homologação;
- `dir`, AES Key Wrap, PBES2 e outros modos simétricos permanecem
  `RuntimeDisabled` até finalidade e custódia próprias;
- `RSA1_5`, CBC-HMAC e `zip` são rejeitados na baseline do projeto;
- quando houver key wrapping, cada JWE recebe uma CEK aleatória nova. IV/nonce
  de 96 bits é produzido pela implementação aprovada e não pode repetir sob a
  mesma CEK/key version;
- o profile fixa limite de invocações e bytes por chave a partir de um budget
  probabilístico aprovado. Esgotamento falha fechado e antecipa rotação.
  Várias instâncias provam como compartilham o budget; contador só é admitido
  com reserva atómica, fencing, restart e rollback, sem alternância implícita
  com geração aleatória;
- plaintext só é liberado depois de tag autenticada válida;
- erros de decriptação são indistinguíveis externamente;
- nested JWT usa sign-then-encrypt, com perfis interno e externo separados.
- JWE não oculta tamanho, recipient metadata ou protected header. Conteúdo
  sensível é minimizado; padding só existe se um protocolo aprovado definir
  buckets e limites que não criem novo oracle.
- replicar `iss`, `sub` ou `aud` em header JWE é proibido por padrão. Uma
  exceção só admite campos classificados, nunca serve a roteamento/autorização
  antes da
  decriptação e exige igualdade com o claim interno validado.

### Claims e replay

- `iss`, `aud`, `sub`, `exp`, `nbf`, `iat`, `jti`, `typ` e `cty` são
  exigidos conforme a finalidade, com limites próprios;
- access token, ID token, artefato de controle e token de recuperação nunca
  compartilham a mesma regra de validação;
- identidade humana usa `(issuer, subject)` após validação completa; tenant e
  roles são resolvidos server-side, sem confiar em email/UPN;
- `jti` não cria automaticamente store global; replay/idempotência pertence
  ao caso de uso;
- clock skew é pequeno, configurado e auditável;
- claims recebidas são não confiáveis até validação completa e RBAC
  server-side posterior.
- validação criptográfica offline não comprova revogação humana em tempo
  real. Cada profile define lifetime máximo e, quando necessário,
  introspection, denylist, continuous access evaluation ou reautenticação;
  ausência desse canal é mostrada como limitação, não como “token revogado”.

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
encrypt/decrypt possuem referências, políticas, cerimônias e auditoria
próprias. O binding mínimo é
`key-version → algorithm → purpose → tenant/environment → direction`.

Permissões RBAC/custodiante são distintas para `read-public`, `verify`,
`sign`, `encrypt/wrap`, `decrypt/unwrap`, `profile-admin`, `rotate`,
`disable`, `recover` e `audit-read`. O runtime comum não possui
`profile-admin`, `rotate`, `disable` ou `recover`. Comprometimento e
destruição exigem dual control e audit durável; multi-signature não substitui
essa separação de deveres.

### Custódia

- desenvolvimento/teste: chaves efémeras, únicas por execução e sem retenção,
  combinadas com inputs públicos/não secretos e expected outputs sanitizados;
- ambientes integrados: vault/KMS/HSM não produtivo e workload identity;
- produção: serviço homologado, política least-privilege, private key
  não exportável onde suportado e auditoria externa;
- persistência DB-Notifier: somente referência opaca, thumbprint público,
  finalidade, versão e estado do lifecycle.

Chaves privadas duráveis, KEKs, seeds e segredos simétricos duráveis não
aparecem em `appsettings`, variável persistida, PostgreSQL, SQLite, pacote,
arquivo de exemplo, screenshot, relatório, teste ou log. CEKs, resultados de
KDF, plaintext e buffers intermediários necessários a JWE podem existir
transitoriamente na memória do processo autorizado. Seu lifetime, tamanho e
ownership são limitados; não entram em cache, dump intencional, log,
telemetria ou persistência, e são limpos/descartados em best effort sem alegar
zeroização perfeita da memória gerenciada.

Infraestrutura como código pode criar identidade, policy e referência, mas
não gera/exporta private key para state, plan, output, pipeline variable ou
artefato. Operações remotas possuem timeout, cancellation, payload máximo,
quota e códigos uniformes; “não exportável” não significa que plaintext ou
CEK nunca entra em memória.

Publicação em RFC não transforma um componente private/symmetric em material
permitido pelo projeto. Somente partes públicas, inputs não secretos e
expected outputs sanitizados podem ser versionados após revisão. Exemplos que
contenham private key, seed, KEK, CEK ou segredo simétrico não são copiados
para teste, repositório ou evidência. Operações secretas usam chaves efémeras
únicas por execução e differential/interoperability tests, registrando que não
reproduzem o vetor privado exato. Se um candidato exigir fixture privada
conhecida, ele permanece bloqueado; esta proposta não cria exceção à política
permanente.

### Cerimônias de rotação e comprometimento

Rotação de assinatura segue esta ordem:

1. criar uma nova key version ligada ao mesmo purpose e ao algoritmo
   autorizado;
2. distribuir/admitir a chave pública e confirmar todos os verificadores;
3. iniciar assinatura com a nova versão;
4. manter a antiga somente para verify durante lifetime, filas e caches
   máximos;
5. retirar verify da antiga, provar recusa e fechar a auditoria.

Rotação de criptografia segue outra ordem:

1. instalar/admitir a nova chave privada de decriptação no destinatário;
2. publicar a nova chave pública/key-encryption version aos produtores;
3. mudar novos envelopes para a nova versão;
4. manter a antiga para decrypt enquanto houver ciphertext, outbox, backup ou
   retenção que dependa dela;
5. reter, re-encriptar ou destruir ciphertext conforme a política de dados;
6. retirar a versão antiga somente depois de provar que não existe dependência
   autorizada.

Comprometimento ignora a cadência comum: revoga, invalida cache, coloca
artefatos/consumidores afetados em quarentena, reemite material e exige
autoridade humana/auditoria próprias. Uma denylist de emergência pode recusar
uma key version antes do fim do cache/LKG.

Rollback de producer, consumer, policy e key lifecycle são planos separados.
Um rollback não diminui geração/checkpoint, não reativa algoritmo proibido,
não muda finalidade e não elimina a chave necessária para decriptar conteúdo
já aceito. Dual-read/overlap só permanece pelo prazo máximo documentado e
nunca autoriza dual-write indefinido.

### JWKS e cache

- JWKS externo continua limitado ao issuer OIDC configurado e à política
  `human-identity`;
- discovery não concede autoridade a uma origem JWKS arbitrária: qualquer
  origem diferente precisa ser preconfigurada administrativamente no mesmo
  trust profile;
- um futuro JWKS próprio publica apenas chaves públicas necessárias;
- documento, quantidade de chaves, headers HTTP, media type e tempo são
  limitados;
- JWKS público com membro privado é recusado; `use`, `key_ops`, `alg`, kty,
  curve/size e status precisam ser coerentes com o profile;
- ETag/Cache-Control e overlap de rotação são testados;
- `kid` desconhecido pode solicitar no máximo um refresh bounded,
  rate-limited e coalesced por issuer/profile; input atacante não causa
  refresh por request, bypass de cache ou fan-out. Negative caching e limite
  de chaves candidatas evitam amplificação;
- refresh possui timeout, backoff, circuit state, single-flight e budget
  separado da validação; uma resposta nova só substitui o LKG depois de
  validação completa e publicação atómica;
- `kid` precisa ser único no trust set ativo; colisão ou duas chaves
  elegíveis falha fechada;
- ausência, staleness ou conflito não cai para trust amplo. LKG possui prazo
  máximo por profile e nunca ignora denylist de comprometimento;
- HA e outage do IdP possuem modo fail-closed e alertas.

### Egress de identidade e custódia

`JOSE-0` produziu uma matriz candidata antes de qualquer SDK ou workload
identity. Os novos consumer IDs abaixo não existem no runtime e só podem ser
criados por revisão própria de R-NET:

| Consumer ID | Finalidade exclusiva | Estado e limite |
|---|---|---|
| `human-identity` | OIDC discovery/JWKS atuais | existente e inalterado; nunca reutilizado para token endpoint, KMS ou STS |
| `human-identity-backchannel` | eventual token exchange/introspection/revocation do BFF | candidato, não autorizado; consumer, origem, credencial e protocolo separados |
| `server-key-custody` | operações data-plane e metadados públicos do KMS/HSM/vault | proposto, não autorizado |
| `server-workload-identity` | bootstrap STS/workload identity quando indispensável | proposto, não autorizado; não implica acesso a IMDS |

Para cada candidato ainda precisarão ser fixados endpoints, CIDRs, portas,
DNS, TLS hostname, private endpoint/failover, redirects, proxy,
credencial/bootstrap, scopes, payloads, timeout, retries, telemetria e owner.
SDK deve aceitar transporte injetado e submetido ao conector pinned do R-NET;
fallback de endpoint, credential chain, proxy, redirect ou telemetria ambiente
é recusado. Um adapter incapaz de provar essa fronteira é rejeitado.

O hard deny atual a metadata/link-local permanece. Qualquer necessidade de
IMDS exige ADR e gate R-NET separados, extremamente limitados, e nunca relaxa
a defesa SSRF geral. URL, tenant, region ou endpoint recebidos de token,
payload ou caller não criam egress. Outage de IdP/KMS/HSM/vault falha fechado
e tem alerta/runbook próprios.

## Dados e persistência

O registry autoritativo de profiles será um snapshot imutável, versionado,
revisado e implantado por um canal de configuração/artefato confiável, com
digest e revision monotônica. PostgreSQL não poderá inventar algoritmo,
header, trust source ou key locator. Se um caso exigir ativação durável, o
banco poderá selecionar apenas uma revision previamente admitida; essa
seleção exige RBAC server-side, optimistic concurrency, ativação atómica,
fencing/checkpoint monotônico e `AuditEntry` append-only. Revision ausente,
desconhecida, divergente ou menor faz startup/ativação falhar fechado.

O estado efetivo de uma key version é a interseção denial-dominant entre o
binding previamente admitido no profile, o checkpoint/generation monotônico,
a denylist de emergência e o estado factual do custodiante; prevalece a
condição mais restritiva. PostgreSQL não pode retargetar uma referência,
reativar chave disabled/revoked ou reduzir geração. Restore, ausência,
divergência ou rollback precisam reconciliar com snapshot/custodiante ou
falhar fechado antes de verify, sign, encrypt ou decrypt.

Se os casos aprovados exigirem metadados duráveis, um incremento de dados
proporá entidades equivalentes a `CryptographicKeyReference`,
`ProtectionProfileActivation`, `KeyBinding` e `ReplayRecord`. Nomes e schema
dependem de ADR-0004 e do modelo lógico; não há migration implícita nesta
proposta.

| Artefato | Classificação e risco | Stores/TTL permitidos | Backup, audit e IA |
|---|---|---|---|
| access/ID/refresh token | secret/bearer e replayable | somente memória ou session/token store cifrado e least-privilege do BFF; no máximo o lifetime necessário | sem backup, log, audit payload ou IA |
| JWK/JWKS público | público, mas integrity-critical | cache limitado por issuer/profile, freshness e denylist | rebuild/reconcile; sem private members ou IA |
| JWS/artefato assinado | classificação do payload; replay conforme finalidade | somente store do caso aprovado, com TTL/retention próprios | audit só metadata/digest sanitizado; sem IA por default |
| JWE/ciphertext | sensível; revela tamanho/headers/recipients | store do caso somente quando durabilidade for requisito, com retenção ligada à chave de decrypt | backup cifrado/reconciliado; nunca log/audit/IA |
| plaintext, CEK, KDF output | secret transitório | memória do processo autorizado e lifetime mínimo | sem backup, persistência, dump intencional, log, audit ou IA |
| referência opaca/profile/key metadata | security metadata interna | PostgreSQL somente sob schema/RBAC admitidos | backup exige reconciliação com custodiante; locator/ARN nunca em audit/IA |
| replay digest | security metadata purpose-bound | digest + issuer/audience + expiry + resultado mínimo; TTL, cardinalidade e cleanup limitados | sem `jti` bruto/token/payload; sem IA |
| evento de segurança | interno e potencialmente sensível | `AuditEntry` canônico MOD-11, append-only e retenção aprovada | campos sanitizados; sem artefato protegido |

Replay protection não é compartilhada entre profiles. Falhas anteriores à
Application usam contadores limitados e sampling para não criar audit flood;
operações administrativas de profile/key, rotação, disable, recovery e
comprometimento de chave exigem audit durável e podem falhar fechado conforme
a decisão MOD-11.

Se comando/outbox adotar JOSE, a mudança precisará revisar ADR-0003,
`Agent-API-Protocol.md`, `Logical-Model.md`, retenção, operação offline,
versionamento, migration e rollback. Sem essa decisão, o protocolo permanece
inalterado.

## Desenvolvimento

### Estratégia de dependências

1. Avaliar a capacidade real de `Microsoft.IdentityModel.JsonWebTokens` e
   `Microsoft.IdentityModel.Tokens` já presentes transitivamente.
2. Comparar suporte a Compact/Flattened/General, multi-signature,
   multi-recipient, detached payload, JWE, RFC 8037/9864/9964,
   streaming/bounds e plataformas alvo.
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

| Lote | Dependência | Owner de fase | Saída obrigatória e condição de saída |
|---|---|---|---|
| `JOSE-0` | autorização documental explícita recebida para preparar a proposta `1.2.0`, sem aceitá-la | correção de arquitetura/segurança/dados pertencente a `STATE-02`, executada sem sair de `STATE-06` | pacote aceito somente como preparação documental; ADR-0008 ainda `proposed`; requisitos, profiles e safety caps provisórios, snapshots/matrizes IANA JOSE/JWT Claims sem `Unreviewed`, ownership, trust/data/egress map e threat model sem lacuna crítica |
| `JOSE-1` | pacote `JOSE-0` aceito apenas como preparação documental e spike autorizado separadamente | correção de feasibility/dependência pertencente a `STATE-01/02/04`, executada sem sair de `STATE-06` | spike test-only .NET 10, corpus público/sanitizado rastreável aos RFCs 7520/8037/9964, chaves efémeras, seis serializações/capacidades medidas, matriz de dependência/SBOM e boundary sem tipo concreto no núcleo |
| `JOSE-2` | `JOSE-D1` aceita ADR-0008 | desenvolvimento corretivo de `STATE-04`, integrado em `STATE-06` | JWS Compact/Flattened/General, detached, multi-signature e JWT inbound hardening em sandbox; nenhum producer operacional |
| `JOSE-3` | `JOSE-D1`; interfaces exigidas por `JOSE-2/4` | `STATE-03/04`, integrado em `STATE-06` | `3A`: JWK/JWKS público, thumbprint, seleção/cache/refresh; `3B`: key operations/custódia, lifecycle e migrations não produtivas somente quando autorizadas |
| `JOSE-4` | `JOSE-D1` + `JOSE-3A`; `JOSE-3B` para chave não efémera; `JOSE-2` para nested JWT | desenvolvimento corretivo de `STATE-04`, integrado em `STATE-06` | JWE Compact/Flattened/General, AAD, multi-recipient e nested JWT em sandbox interoperável |
| `JOSE-5` | lotes técnicos e subdecisão owner de cada caso | `5A` backend MOD-01/MOD-11 (`STATE-04`); `5B` Dashboard/BFF (`STATE-05`); `5C` integração (`STATE-06`); `5D+` um sublote por outro caso | E2E, autorização/RBAC, UX/acessibilidade quando aplicável, rollback e aceite separado por caso |
| `JOSE-6` | `JOSE-5` relevante e transição autorizada | somente `STATE-07` | `6A`: provisionamento não produtivo autorizado; `6B`: IdP/KMS/HSM/vault/plataformas exatos, HA/outage, carga, fuzz, pen test e matriz de homologação |
| `JOSE-7` | `JOSE-6` aprovado e entrada explícita em release | somente `STATE-08` | runbooks, SBOM, observabilidade, backup/recovery, rollout/rollback e Human Gate de release existente |

Depois de `JOSE-1` existe `JOSE-D1`, um checkpoint explícito de decisão de
arquitetura e segurança que pode aceitar, rejeitar ou devolver ADR-0008.
`JOSE-D1` não é lote, não é Human Gate de lifecycle e não revalida
`STATE-06 → STATE-07`. Nenhum código de produto começa antes de ADR-0008
`accepted`.

O grafo mínimo é
`JOSE-0 → JOSE-1 → JOSE-D1 → {JOSE-2, JOSE-3A/3B} → JOSE-4 → JOSE-5 → JOSE-6 → JOSE-7`.
`JOSE-2` e `JOSE-3` só avançam em paralelo quando ownership, interfaces e
arquivos não se sobrepõem.

Cada lote possui autoridade, preflight, diff, evidence owner, Quality Gate
automático e revisão humana proporcional. Esses aceites de lote não são
automaticamente Human Gates formais do lifecycle. Uma correção pertencente a
fase anterior não rebobina o lifecycle, mas satisfaz os checks daquela fase.
`JOSE-6` e `JOSE-7` são inexequíveis no estado atual; citá-los não concede
transição. Falha não autoriza pular ou agrupar aprovações.

### Gates executáveis por lote

| Lote | Checks/evidência mínimos | Revisão/critério de bloqueio | Rollback |
|---|---|---|---|
| `JOSE-0` | snapshots+hash, 100% de linhas classificadas, requisitos/threat/data/egress traceáveis, links e docs gates | owners de security, identity, architecture, data e módulos afetados; qualquer `Unreviewed` bloqueia a saída | retirar somente a revision proposta; runtime permanece inalterado |
| `JOSE-1` | build/test offline Windows+Linux, corpus público/sanitizado com origem+hash, chaves efémeras, capability matrix, licença/provenance/advisories e SBOM reconciliada ao lockfile | `JOSE-D1`; custom primitive, fixture secreta durável, parser sem bounds ou gap nuclear sem estratégia segura bloqueia | remover spike test-only ou manter candidato `RuntimeDisabled` |
| `JOSE-2` | unit/negative/fuzz/differential/interoperability, architecture tests e coverage floors | security review; algoritmo ou assinatura favorável não prevista bloqueia | desabilitar profile e restaurar relying-party profile anterior |
| `JOSE-3` | seleção/JWKS adversarial, cache/outage/rotation, schema/retention checks quando aplicáveis, canary/secret scan | security/platform/data; trust ambíguo, private member ou rollback de version bloqueia | LKG/profile anterior dentro da freshness policy; migration rollback ensaiado |
| `JOSE-4` | corpus público/sanitizado rastreável ao RFC 7520 + casos derivados/differential, tamper/AAD/tag/recipient roster, IV budget, memória e interoperabilidade por serialização | revisão criptográfica independente antes de uso operacional; oracle ou nonce budget não provado bloqueia | profile JWE permanece test-only/desabilitado |
| `JOSE-5` | E2E por caso, RBAC negativo, replay/idempotência, accessibility/browser security quando aplicável e rollback matrix | owner do caso + amostra humana quando houver UI/fluxo; nenhum aceite vale para outro caso | desativar somente policy/caso; preservar protocolo e policy anterior compatível |
| `JOSE-6` | pipeline externo separado, topologia R-NET, rotação/outage/load/fuzz/pen test e scope matrix | decisão de homologação exata; PR CI nunca depende de rede/credencial | revogar workload/profile não produtivo e reconciliar metadados |
| `JOSE-7` | gates completos STATE-08, SBOM, packaging, runbooks, SLO/alerts, backup/recovery e rollout/rollback | Human Gate de release existente; nenhum claim antes dele | rollback de binário/config/schema/protocol/key por owners e RTO/RPO definidos |

### Gate de dependência e biblioteca

`JOSE-1` deverá produzir uma matriz comparativa, não uma escolha implícita.
Para cada candidato:

- serializações JWS/JWE realmente suportadas;
- multi-signature, multi-recipient, detached e `b64=false`;
- enforcement de protected/unprotected headers e JSON duplicado;
- algoritmos/curvas/key types e identificadores dos RFCs 8037/9864/9964;
- KMS/HSM/non-exportable key integration;
- parsing/budgets/cancellation e comportamento de erro;
- .NET 10, Windows/Linux e trimming/AOT quando aplicável;
- manutenção, licença, SBOM, provenance, advisories e locked restore; e
- corpus público/sanitizado, chaves efémeras, differential tests e gaps que
  exigiriam código próprio ou fixture secreta durável.

Qualquer gap que exija implementar primitiva criptográfica bloqueia o
candidato. Wrapper de serialização só poderá existir sobre primitivas
aprovadas, com revisão criptográfica e interoperabilidade; “a dependência já
é transitiva” não constitui aceite.

PR CI deve ser determinística e offline em Windows/Linux para o boundary JOSE.
IdP/custodiante não produtivo usa pipeline separado, autorizado e sem segredo
durável. O corpus público/sanitizado fica versionado com origem/hash; SBOM
SPDX ou CycloneDX é
validada contra dependências/lockfiles. Thresholds de latência, memória,
throughput e falha serão numéricos por profile antes de homologação.

## Documentação preparada e mudanças futuras após aprovação

| Documento | Estado `JOSE-0` ou mudança futura |
|---|---|
| `AGENTS.md` | regra permanente curta somente se a política JOSE for aceita como norma do projeto |
| `prompts/foundation/Solution-Architecture-Document.md` | trust boundaries, componentes e responsabilidades |
| `prompts/governance/Security-And-Access.md` | perfis, claims, headers, chaves, rotação, auditoria e recuperação |
| `prompts/governance/Lifecycle.md` | entregáveis/gates JOSE nas fases proprietárias, sem lifecycle paralelo |
| `prompts/governance/Quality-Gates.md` | checks de segurança, supply chain, interoperabilidade e evidência |
| `prompts/state/Current-State.md` | somente implementação/evidência factual alcançada |
| `prompts/system/Prompt-System-Change-Log.md` | qualquer mudança futura da autoridade/instruction corpus |
| ADR-0002 | somente se enrollment, Agent identity ou custódia do Agent mudar |
| ADR-0003 + `Agent-API-Protocol.md` | somente se comando/outbox/protocolo adotar JOSE |
| ADR-0004 | fonte de policy, metadados, replay, retenção e migrations |
| ADR-0005 | packages/updates; assinatura de plataforma continua prevalente |
| ADR-0007 e documentos AIOps | somente se MOD-12 adotar um codec JOSE sob nova decisão |
| ADR-0008 | decisão guarda-chuva, alternativas, boundaries e consequências |
| `docs/architecture/JOSE-Security-Profile-And-Key-Lifecycle.md` | candidato `JOSE-0` aceito apenas como preparação documental, não como autoridade normativa; só poderá tornar-se autoridade de profile+key lifecycle após a decisão própria |
| `docs/architecture/Threat-Model.md` | `JOSE-T*` e control owners preparados como riscos/controles candidatos, não implementação |
| `docs/architecture/Network-Egress-Policy.md` | candidate appendix sem registrar consumer; endpoints IdP/JWKS/STS/KMS permanecem decisão futura |
| `docs/architecture/Canonical-Contracts.md` | contratos públicos somente quando aprovados |
| `docs/data/README.md`, `Logical-Model.md`, `Retention-And-Deletion.md`, `Migration-Runbook.md` | autoridade do profile, metadata/replay/ciphertext, retenção, migration e rollback |
| matriz `JOSE-REQ-*` | requirement → RFC/IANA → caso/módulo → threat → ADR/doc → lote → teste/evidência → gate |
| runbooks de operação/release | provisioning, rotações separadas, outage, comprometimento de chave, rollback e disaster recovery |
| relatórios por lote | comandos, versões, matriz, resultados, limitações e evidência sanitizada |

A documentação normativa será atualizada junto do lote dono; não será
preenchida antecipadamente como se a capacidade estivesse implementada.
`Security-And-Access.md` permanece a autoridade temática de segurança. O novo
documento técnico detalha somente o candidato JOSE ainda não aceito e não
cria política, lifecycle ou audit paralelos.

## Requisitos e threat model rastreáveis

| Requisito | RFC/IANA e caso/módulo | Threats e autoridade documental | Owner/enforcement | Lote, teste/evidência e gate |
|---|---|---|---|---|
| `JOSE-REQ-001` — selecionar finalidade/policy antes do objeto e manter JOSE fora de Domain/Application | RFC 8725; todos | `T01/T05/T17`; ADR-0008 + Solution Architecture | Server composition + Security boundary | `0/2/4`; architecture/cross-profile negatives; `D1` |
| `JOSE-REQ-002` — parsing, headers, sizes, nesting e custo estritamente limitados | RFC 7515/7516/7797 | `T02/T06/T07/T15`; ADR-0008 + JOSE technical profile | Security boundary | `0/1/2/4`; `N-1/N/N+1`, fuzz/corpus; gates de lote |
| `JOSE-REQ-003` — classificar 100% dos snapshots IANA JOSE e JWT Claims em eixos independentes | IANA; RFC 7517/7518/9964 | `T15`; ADR-0008 + matriz de cobertura | security standards owner | `0`; snapshot+hash+delta review; gate `JOSE-0` |
| `JOSE-REQ-004` — ligar algoritmo/key type/key version/purpose/tenant/ambiente/direção | RFC 8725/9864 | `T01/T10/T12`; ADR-0008 + Security-And-Access | profile registry + custodian policy | `0/2/3`; adversarial/policy inspection; `D1`/lote |
| `JOSE-REQ-005` — JWE autenticar antes de plaintext, controlar CEK/IV/roster/memória e leakage | RFC 7516/7520 | `T07/T08/T16`; ADR-0008 + data authorities | Security boundary + data owner | `1/4`; tamper/budget/concurrency/memory; gates `1/4` |
| `JOSE-REQ-006` — separar custódia, permissões e cerimônias sign/encrypt/compromise | key lifecycle | `T05/T09/T11/T13`; Security-And-Access + ADR-0008 | platform/security/MOD-11 | `0/3/6`; rotation/denylist/tabletop; gates `3/6` |
| `JOSE-REQ-007` — resolver trust/JWKS/KMS somente por R-NET preconfigurado | RFC 7517; identity/custody | `T03/T09/T14`; Network-Egress-Policy + ADR-0008 | egress connector + trust resolver | `0/3/6`; zero-hit/cache/outage/transport; gates `3/6` |
| `JOSE-REQ-008` — separar access/ID token, identidade `(iss,sub)`, sessão, MFA e RBAC | RFC 9700/9068; MOD-01/09/11 | `T01/T04/T12`; Security-And-Access | Server/BFF + MOD-11 | `5A/B/C`; E2E/RBAC/CSRF/CORS/logout; gate do caso |
| `JOSE-REQ-009` — persistir somente por artefato/classificação, replay digest e audit canônico | ADR-0004; MOD-11 | `T10/T12/T13/T16`; data docs + Security-And-Access | data owner + PostgreSQL + audit | `0/3/5`; schema/retention/migration/canary; gates `3/5` |
| `JOSE-REQ-010` — preservar ADRs 0002/0003/0005/0007 e exigir revisão do owner | Agent, updates, MOD-12 | `T17`; ADRs e protocolos owners | module owners | sublote `5D+`; compatibility/authority tests; gate do owner |
| `JOSE-REQ-011` — usar dependência aprovada, sem primitiva própria, com SBOM e interoperabilidade | supply chain/.NET 10 | `T02/T15`; Quality-Gates + ADR-0008 | platform/security/CI | `1/2/4/7`; offline CI/vectors/provenance; `D1`/release |
| `JOSE-REQ-012` — separar roadmap, implementação, verificação, homologação, runtime, autorização e claim | governance | `T17`; Lifecycle + Current-State + ADR-0008 | lifecycle/release owners | todos; matriz factual; gates de lote/lifecycle |

| ID | Ameaça e boundary | Controle/owner e enforcement | Vetor negativo/lote | Risco residual a decidir |
|---|---|---|---|---|
| `JOSE-T01` | downgrade, RSA/HMAC ou cross-JWT substitution na API | `REQ-001/004`; profile selecionado pela composição, allowlists e tipos exclusivos | tokens cruzados/`alg=none`; `2/5` | issuer comprometido |
| `JOSE-T02` | duplicate JSON, header smuggling, `kid` injection e custo de parsing | `REQ-002`; bounded parser e lookup opaco | fuzz, Unicode, N±1; `1/2/4` | DoS dentro do budget |
| `JOSE-T03` | SSRF/JWK atacante/JWKS refresh amplification | `REQ-007`; resolver preconfigurado, single-flight/negative cache/R-NET | `jku/x5u/jwk/x5c`, kid aleatório, zero-hit; `3/6` | outage do issuer |
| `JOSE-T04` | theft no browser, ID-token substitution e mapping de subject/tenant incorreto | `REQ-008`; BFF/session e RBAC server-side | XSS/CSRF/mix-up/cross-tenant; `5A/B/C` | JavaScript comprometido e revogação não imediata |
| `JOSE-T05` | signing/decryption oracle ou signer abuse | `REQ-001/006`; porta semântica, RBAC, quota e custodian policy | payload/profile/key fornecidos pelo caller; `2/4/5` | abuso por principal já autorizado |
| `JOSE-T06` | signature stripping/quórum favorável | `REQ-002/004`; roster de signatários e regra todos/quórum | ausência/reordenação/duplicate signer; `2` | custódias correlacionadas |
| `JOSE-T07` | recipient stripping/header per-recipient adulterado | `REQ-005`; profile/roster binding e General JWE test-only | remover/reordenar/trocar `alg/kid`; `4` | metadata visível |
| `JOSE-T08` | IV GCM repetido em concorrência/restart/rollback | `REQ-005`; CEK nova, budget por chave, fencing ou risco probabilístico | multi-instance/exhaustion/restart; `4/6` | probabilidade residual aprovada |
| `JOSE-T09` | KMS confused deputy, credential bootstrap ou SDK fora de R-NET | `REQ-006/007`; consumer/scopes/transport exatos | endpoint/proxy/credential/IMDS fallback; `3/6` | falha do custodiante |
| `JOSE-T10` | key/profile rollback, split-brain ou policy-store tamper | `REQ-004/006/009`; snapshot imutável, revision monotônica, audit/fencing | DB downgrade/divergence/concurrency; `3/5` | recovery extraordinária |
| `JOSE-T11` | rotação race ou comprometimento de chave | `REQ-006`; cerimônias separadas, overlap e emergency denylist | stale cache/ciphertext antigo/tabletop; `3/6/7` | artefato emitido antes da detecção |
| `JOSE-T12` | replay e confusão cross-tenant/purpose | `REQ-004/008/009`; binding e digest store por finalidade | duplicate/expired/cross-scope; `2/5` | janela entre validação e commit |
| `JOSE-T13` | audit flooding/leakage e locator disclosure | `REQ-006/009`; MOD-11, sampling/limits e campos sanitizados | canary/high-rate/pre-Application failure; `3/5/7` | perda de granularidade sob sampling |
| `JOSE-T14` | IdP/KMS outage ou LKG stale | `REQ-007`; freshness, denylist, fail-closed, alert/runbook | outage/recovery/cache conflict; `3/6` | disponibilidade reduzida por segurança |
| `JOSE-T15` | library regression, supply-chain ou ML-DSA size DoS | `REQ-002/003/011`; locked restore, SBOM, advisories e budgets | differential/oversize/advisory drill; `1/7` | zero-day da dependência |
| `JOSE-T16` | leakage por tamanho/header/recipient metadata | `REQ-005/009`; minimização e padding protocolar opcional | traffic/size analysis; `4/5` | leakage permanece sem protocolo de padding |
| `JOSE-T17` | JOSE substituir mTLS, outbox, package signing, MOD-12 authority ou Human Gate | `REQ-010/012`; ADR/module owner permanece autoridade | architecture/compatibility/authority tests; `0/5` | complexidade de múltiplas camadas |

## Validação e evidência

### Testes automatizados

- partes públicas/não secretas e expected outputs sanitizados dos RFCs
  7520/8037/9964, mais casos efémeros/differential rastreáveis aos requisitos
  dos RFCs 7515/7516/7517/7518/7638/7797/8725/9864; um exemplo legado
  comprova parsing/recusa, não habilitação;
- round-trip e negative/tamper de cada uma das seis serializações nucleares no
  harness test-only;
- interoperabilidade por serialização/capacidade com duas implementações
  independentes quando viável; qualquer fallback para uma implementação +
  corpus público oficial precisa registrar a limitação;
- casos negativos de algoritmo, key type, curve, size, issuer, audience,
  lifetime, `crit`, `typ`, `cty`, `kid`, `jku`, `jwk`, `x5u`, `x5c`;
- JSON/base64url malformado, duplicado, profundo, grande e com Unicode
  adversarial;
- multi-signature/quórum e multi-recipient sem aceitação parcial favorável;
- tamper em header, AAD, ciphertext, tag, assinatura e detached payload;
- nesting excessivo, compression refusal e decryption error uniformity;
- rotação, revogação, cache stale, rollback, clock skew e outage;
- concorrência de emissão, nonce/IV, cache refresh e key rotation;
- browser identity, ID-token substitution, RBAC/tenant e session invalidation;
- R-NET/SDK transport, credential bootstrap e ausência de egress implícito;
- policy/key rollback, split-brain, audit flooding e signer/decrypt oracle;
- canary scan em logs, audit, traces, dumps e relatórios;
- fuzz/property tests com budgets explícitos; e
- architecture tests provando ausência de JOSE no Domain/Application/providers
  e de chave durável em configuração/persistência comum.

### Homologação

- Windows e Linux para Server/Agent aplicável;
- .NET 10 nas arquiteturas de CPU suportadas;
- IdP e fluxo OIDC exatos;
- KMS/HSM/vault por fornecedor, versão, região/topologia e operação;
- algoritmos/curvas/tamanhos exatos;
- proxy/DNS/TLS/cache/HA/outage;
- performance de verify/sign/encrypt/decrypt com payloads máximos;
- rotações separadas de assinatura/criptografia e recuperação de
  comprometimento de chave;
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
- rotação/revogação/comprometimento de chave e ator autorizado.

São proibidos em logs/audit/telemetria:

- token completo;
- payload/claims irrestritas;
- plaintext ou ciphertext;
- tag, CEK, KEK, chave privada/simétrica;
- JWK privado, connection string, segredo ou resposta bruta do custodiante; e
- exception que revele material ou permita oracle.

Métricas terão cardinalidade e retenção limitadas e incluirão, por profile,
recusas sanitizadas, latência/budget criptográfico, refresh/negative cache,
idade do LKG, expiração/denylist de key version, quota do custodiante, replay,
clock health e falha de rotação. Thresholds, SLO, alerta e runbook têm owner
antes de `JOSE-6`; nenhum rótulo contém token, `kid` atacante, locator/ARN ou
claim livre.

Cada finalidade define se uma falha de audit bloqueia a operação. Administração
de profile/key, disable, recovery e comprometimento de chave exigem
`AuditEntry` durável; validações inválidas de alto volume usam métrica
agregada/sampling e não criam uma linha persistente por ataque.

## Compatibilidade e migração

1. Congelar testes do comportamento JWT/OIDC atual.
2. Mapear `HumanAuthentication:Authority/Audience` e demais opções atuais para
   a revision de profile sem mudar semântica por inferência.
3. Introduzir o profile humano em modo equivalente, ainda sem emitir token.
4. Fixar issuer, audience, `typ`, claims e algoritmos aceitos por IdP.
5. Manter `RS256` somente se necessário para compatibilidade documentada.
6. Adicionar algoritmo/key version preferidos após IdP e custodiante passarem
   interoperabilidade.
7. Observar a janela acordada sem registrar tokens.
8. Remover o algoritmo antigo somente após zero consumidor dependente,
   rollback ensaiado, decisão explícita do owner e gate aplicável.
9. Adicionar JWS/JWE de produto por caso de uso, nunca por habilitação global.

Rollback desativa o novo producer/consumer por profile version e restaura a
última política aceita, sem reativar algoritmo proibido, sem reintroduzir
chave local e sem aceitar objeto emitido sob finalidade incompatível.

Cada sublote terá uma matriz separada para binário, configuração/profile,
schema, protocolo e key lifecycle, com owner, trigger, ordem, RTO/RPO,
compatibilidade e evidência. Rollback de schema/protocolo não pode romper
outbox/offline Agent. Rollback de key generation nunca reduz checkpoint nem
retira chave de decrypt enquanto ciphertext, fila ou backup autorizado ainda
depender dela.

## Critérios de aceite globais

Os aceites são separados:

1. **Cobertura normativa completa**:
   `classified/registry-total = 100%` nos snapshots IANA JOSE e JWT Claims;
   todo recurso dos RFCs adotados tem linha, decisão, razão e owner; nenhum
   `Unreviewed` permanece.
2. **Conformance harness completo**: JWS e JWE Compact, Flattened e General
   possuem processamento test-only, limites, negativos/tamper e evidência de
   interoperabilidade por serialização; General JWS cobre múltiplas
   assinaturas e General JWE cobre múltiplos destinatários. Isso não habilita
   um profile de produto.
3. **Implementação completa do profile**: ADR-0008 foi aceito em `JOSE-D1`;
   tudo que a decision matrix marcou `Adopted` ou `SafelyAdapted` para o
   profile está implementado/testado; sunset de adaptação está vigente; JWK,
   trust, key operations, replay, audit e rollback aplicáveis estão provados.
4. **Suporte operacional do profile**: finalidade, IdP/custodiante,
   algoritmo/key type/tamanho, plataforma, topologia, egress, operação,
   lifetime e revision exatos estão autorizados, homologados e habilitados.
5. **Claim público**: somente no `STATE-08`, após gate de release e texto
   aprovado que nomeie o escopo exato. `STATE-07` sozinho não autoriza claim.

Em todos os aceites aplicáveis:

- nenhum algoritmo/trust/policy/key é selecionado por conteúdo não confiável;
- nenhum segredo ou chave durável aparece em código, config, persistência,
  logs ou evidência;
- não existe achado Critical/High aberto no escopo;
- coverage floors, load, fuzz, negatives, outage, rollback e cross-platform
  aplicáveis passam;
- R-NET, data handling, observabilidade, runbooks e audit possuem owner e
  evidência; e
- roadmap, implementação, verificação, homologação, runtime, autorização e
  suporte público permanecem fatos distintos.

Não é obrigatório ativar JWE em produto sem caso de uso; o conformance harness
prova o modelo de processamento. “Biblioteca suporta JWE”, “teste unitário
passou” ou “JWT funciona” não satisfazem nenhum aceite amplo.

## Riscos e mitigação

| Risco | Mitigação |
|---|---|
| escopo excessivo | oito lotes independentes, use cases antes de primitives |
| falsa completude por algoritmo | matriz de cobertura IANA versionada |
| lock-in de KMS/IdP | portas provider-neutral e homologação por adapter |
| complexidade de General JSON | spike/interoperabilidade antes de arquitetura definitiva |
| chave em aplicação | referência opaca e operação no KMS/HSM quando possível |
| regressão do JWT atual | golden/negative tests e migração por profile |
| custo/DoS criptográfico | admission budgets, tamanhos, rate limit e benchmark |
| criptografia usada para esconder design inseguro | data classification e minimização antes de JWE |
| mistura com mTLS/RBAC | trust boundaries e responsabilidades separadas |
| profile store vira autoridade indevida | snapshot imutável, revision monotônica, RBAC/audit e ativação atómica |
| SDK contorna R-NET | consumer/transport/credential bootstrap tipados e adapter rejeitado sem prova |
| token válido mapeado ao humano errado | identidade `(iss,sub)`, tenant allowlist e RBAC server-side |
| retenção de ciphertext perde decrypt key | retenção/key lifecycle reconciliados antes da retirada |
| norma/registro evolui | revisão IANA/RFC em dependency e release gates |

## Decisões humanas necessárias

O proprietário autorizou `JOSE-0` sem aceitar a proposta ou o ADR e depois
aceitou o pacote exclusivamente como preparação documental. Essa decisão não
decide os casos de produto, BFF/SPA, identidade/provisioning/RBAC, ambientes,
custodiantes, topologia externa ou infraestrutura.

Decision packets não fazem parte do lote executado: embora aparecessem no
texto sugerido pela revisão anterior, foram omitidos da autorização final e
permanecem fora do escopo.

`JOSE-1` continua proibido e exige autorização separada para um eventual
spike. Somente depois da evidência desse lote poderá `JOSE-D1` solicitar uma
decisão explícita sobre ADR-0008. Cada caso de produto, infraestrutura
externa, homologação e release continua com autoridade própria. Nenhuma
decisão é inferida desta revisão.

## Autorização recebida para JOSE-0

O escopo efetivamente executado é o texto posterior e mais restrito fornecido
pelo proprietário:

```text
AUTORIZO exclusivamente o lote documental STATE-06 JOSE-0 — Architecture,
Security and Coverage Design, limitado a preparar — sem aceitar — o
ADR-0008; definir profiles, targets e safety caps provisórios; capturar e
classificar os snapshots IANA JOSE/JWT Claims; produzir ownership/trust/data/
egress maps, threat model JOSE-T*, matriz JOSE-REQ-* e planos de teste,
migração e rollback. Permanecem proibidos código, dependências, migrations,
runtime, IdP/login real, chaves, vault/KMS/HSM, serviços externos, deploy,
transição de lifecycle, JOSE-1 e ativação MOD-12.
```

Essa autorização não serve para desenvolvimento, spike ou infraestrutura e
não altera `STATE-06 INTEGRATION`.

## Decisão humana posterior sobre o pacote JOSE-0

```text
REVISEI o pacote STATE-06 JOSE-0 e ACEITO-O exclusivamente como preparação
documental. Esta decisão não aceita o ADR-0008, não autoriza JOSE-1, código,
dependências, migrations, runtime, IdP/login, chaves, custódia,
infraestrutura, deploy, lifecycle ou ativação MOD-12.
```

O aceite fecha somente a revisão humana do pacote preparatório. Todos os
profiles, caps, algoritmos, egress candidates, planos e decisões técnicas
continuam provisórios; ADR-0008 permanece `proposed`; `JOSE-1` e qualquer
ação posterior continuam sem autoridade.
