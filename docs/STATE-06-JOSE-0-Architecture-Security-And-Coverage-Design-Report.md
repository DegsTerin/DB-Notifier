# Relatório STATE-06 JOSE-0 — Architecture, Security and Coverage Design

## Status e autoridade

- Data local: 2026-07-28
- Estado do projeto: `STATE-06 INTEGRATION`
- Lote: `JOSE-0`
- Natureza: exclusivamente documental
- Status técnico do lote:
  `CONCLUÍDO — PACOTE DOCUMENTAL PREPARADO; REVISÃO HUMANA PENDENTE`
- ADR associado:
  [ADR-0008](architecture/ADR-0008-JOSE-Cryptographic-Profiles-And-Key-Lifecycle.md),
  revision `1.2`, status `proposed`
- Lifecycle: inalterado
- MOD-12: inalterado e `ActivationState=None`

O proprietário autorizou exclusivamente preparar — sem aceitar — ADR-0008,
definir profiles, targets e safety caps provisórios, capturar e classificar
os snapshots IANA JOSE/JWT Claims, produzir ownership/trust/data/egress maps,
threat model `JOSE-T*`, matriz `JOSE-REQ-*` e planos de teste, migração e
rollback.

Permaneceram proibidos código, configuração executável, dependências,
migrations, runtime, IdP/login real, chaves, certificados, vault/KMS/HSM,
bancos ou serviços externos operacionais, deploy, publicação, transição de
lifecycle, `JOSE-1` e ativação MOD-12. A consulta HTTPS somente leitura às
fontes oficiais IANA/RFC foi limitada à captura e fundamentação documental
expressamente necessárias.

Os decision packets mencionados na proposta revisada não foram produzidos:
o texto de autorização atual não os incluiu.

## Shutdown preflight e baseline

Antes da primeira ação técnica:

- processos pertencentes ao DB-Notifier: `0`;
- janelas visíveis pertencentes ao DB-Notifier: `0`;
- listeners pertencentes ao DB-Notifier: `0`;
- runtime, Dashboard de revisão e browser dedicado pertencente ao projeto:
  ausentes;
- worktree: limpo;
- `HEAD` observado:
  `6d0bbdc68c5c080d11fccd7a6fcd540908440903`.

Nenhum database engine, browser comum, IDE ou processo alheio foi encerrado.
Nenhum runtime foi iniciado durante o lote.

## Entregáveis

| Entregável | Resultado |
|---|---|
| ADR preparado | ADR-0008 revision `1.2`, ainda `proposed`; nenhum aceite inferido |
| Contrato técnico | [JOSE Security Profile and Key Lifecycle](architecture/JOSE-Security-Profile-And-Key-Lifecycle.md) |
| Cobertura IANA | [JOSE and JWT IANA Registry Coverage](architecture/JOSE-IANA-Registry-Coverage.md) |
| Evidência reproduzível | [manifest e archive Base64](architecture/evidence/jose-0/README.md) com os dez corpos CSV exatos |
| Profiles provisórios | cinco: human access token inbound, JWS core, JWS detached, JWE core e nested JWT conformance |
| Targets e safety caps | limites numéricos por profile, parâmetros criptográficos candidatos, JWKS/cache, memória, trabalho e tempo |
| Maps | ownership, trust, data e egress no contrato técnico; candidatos de egress separados das quatro políticas atuais |
| Threat model | `JOSE-T01`–`JOSE-T17` na autoridade canónica [Threat Model](architecture/Threat-Model.md) |
| Requisitos | `JOSE-REQ-001`–`JOSE-REQ-012`, rastreados abaixo |
| Planos | testes, migração, compatibilidade e rollback abaixo |
| Estado factual | Current State e State Transition Log atualizados sem mudar lifecycle |

## Descobertas factuais que condicionam o desenho

### Relying party sem profile algorítmico/tipo explícito

O `JwtBearer` atual valida issuer, audience, lifetime e signing key e aplica
um minuto de skew. Ele não configura `ValidAlgorithms`/`AlgorithmValidator`
nem `ValidTypes`/`TypeValidator`. Portanto:

- o caminho atual continua factual como relying party JWT/JWKS agregado;
- nenhuma linha individual de algoritmo ou `typ` foi marcada como
  `Implemented`, `Interoperable` ou `Homologated`;
- o profile `human-access-token-inbound.v0` é alvo de migração, não descrição
  de um profile já implementado; e
- allowlist de algoritmo e tipo é bloqueio obrigatório antes de qualquer
  claim de implementação do profile.

### Identidade atual usa somente `sub`

O resolver atual produz `HumanActor(string SubjectId)` somente a partir de
`sub`, e `users.subject_id` é globalmente único. O alvo seguro da proposta é
`(issuer, subject)`. A incompatibilidade exige migração explícita e
fail-closed:

- issuer não pode ser inferido da configuração vigente;
- nenhum backfill automático é permitido;
- `user_id` e as referências relacionais de roles precisam permanecer
  estáveis;
- o audit atual grava `ActorId` textual derivado de `subject`, sem FK de
  utilizador; os eventos existentes permanecem append-only e nunca recebem
  issuer inferido, enquanto eventos futuros exigem identidade/versionamento
  explícitos;
- cada utilizador existente exige mapping administrativo revisto;
- utilizador ativo sem issuer provado é recusado fail-closed no cutover; um
  estado novo de quarantine exigiria decisão de dados/migration própria; e
- rollback de schema fica bloqueado quando houver subject igual em issuers
  distintos ou dependência do formato composto.

Nenhuma migration ou alteração de código foi executada neste lote.

### Autoridades preservadas

- ADR-0002 continua dono de Agent identity, enrollment e secret custody.
- ADR-0003 e `Agent-API-Protocol.md` continuam donos de protocolo durável,
  offline, sequência e idempotência.
- ADR-0004 e os documentos de dados continuam donos de schema, persistence e
  retention.
- ADR-0005 continua dono de package/platform signing e updates.
- ADR-0007 continua dono de trust, checkpoints, quarantine e ativação MOD-12.
- `Security-And-Access.md`, Lifecycle e Quality Gates não foram alterados
  normativamente antes de `JOSE-D1`.

## Resultado dos snapshots e cobertura

| Escopo | Last Updated | Linhas | Classificadas | Adopted | SafelyAdapted | Rejected | Unreviewed |
|---|---|---:|---:|---:|---:|---:|---:|
| IANA JOSE, nove registries | 2026-05-22 | 155 | 155 | 33 | 44 | 78 | 0 |
| IANA JWT Claims | 2026-07-20 | 163 | 163 | 8 | 14 | 141 | 0 |
| **Total** | — | **318** | **318** | **41** | **58** | **219** | **0** |

Resultado: `318/318 = 100%`, com zero chave duplicada, zero ausente, zero
extra e zero `Unreviewed`.

Os checksums foram calculados sobre os bytes brutos recebidos. Um ZIP
determinístico com os dez corpos CSV exatos foi codificado em Base64 para
preservar os bytes apesar de normalização de line endings do Git. O archive
decodificado possui `10.146` bytes e SHA-256
`f3fcf5a876beb5e06b5d00e7a4affcf74f036d3826539e4f8c5a89e2177d22ad`.
A reconstrução em memória verificou todos os dez hashes de entrada.

IANA é fonte de nomes e estado de registro, não trust ou policy de runtime.
Uma futura diferença de bytes coloca toda entrada nova/alterada em
`Unreviewed + RuntimeDisabled` até nova revisão autorizada.

## Profiles, targets e caps provisórios

Os cinco profiles definidos são:

1. `human-access-token-inbound.v0`;
2. `jws-core-conformance.v0`;
3. `jws-detached-conformance.v0`;
4. `jwe-core-conformance.v0`; e
5. `nested-jwt-conformance.v0`.

Todos estão `NotImplemented`, `NotTested`, `NotHomologated`,
`RuntimeDisabled` e `NotAdvertised`.

Os principais hard caps candidatos para o futuro spike são:

- JWT humano: objeto 16 KiB, header decodificado 2 KiB e claims 8 KiB;
- conformance JWS/JWE: objeto 128 KiB, payload/plaintext 64 KiB, headers
  totais 16 KiB e AAD 8 KiB;
- JSON: depth 8, 64 top-level claims, 256 nós por JWT e 512 por JWS/JWE;
- General: máximo quatro assinaturas ou destinatários e um candidato
  criptográfico por assinatura/destinatário selecionado;
- nesting: máximo duas camadas, somente sign-then-encrypt;
- JWK/JWKS público: 8 KiB por chave, 256 KiB/64 chaves/4.096 nós por set;
- memória contabilizada: 512 KiB para JWT humano e 8 MiB por objeto JOSE
  completo ou documento JWKS; General/nested não multiplicam o envelope por
  assinatura, destinatário ou camada;
- primeira prova: uma operação ativa, zero fila;
- RSA: somente 2.048, 3.072 ou 4.096 bits, expoente `65537`;
- `PS256`: SHA-256/MGF1-SHA-256/salt 32 bytes;
- `ES256`: P-256 e assinatura JOSE `R || S` de 64 bytes;
- `RSA-OAEP-256`: SHA-256/MGF1-SHA-256/label vazio; e
- `A256GCM`: CEK 32 bytes, IV 12 bytes, tag 16 bytes e uma única
  content-encryption invocation por CEK nova.

Esses números são hipóteses locais conservadoras. RFCs não definem os caps de
objeto, memória, cardinalidade, cache ou latência. `JOSE-1` deverá medir
`N-1/N/N+1`; somente `JOSE-D1` poderá aceitar ou congelar valores.

## Maps e arquitetura

### Ownership

Security Architecture possui standards/profiles/threats; MOD-01/MOD-11
possuem identidade/RBAC/session/audit; Server/API/BFF possui composição
confiável e portas semânticas; Platform/SRE/Network Security possuem eventual
custódia e egress; Data Architecture possui classificação, retention,
migration e restore; QA/SDET + Security possuem conformance/negativos; cada
módulo preserva seu ADR; Bruno com arquitetura e segurança decide
`JOSE-D1`.

### Trust

O fluxo proposto é:

```text
RFC/IANA público -> snapshot revisto -> profile provisório
rota/use case confiável -> profile revision
objeto JOSE não confiável -> parser bounded -> trust source preconfigurado
resultado sanitizado -> Application RBAC/idempotência -> estado/audit canónico
```

O objeto não fornece sua própria policy/root. PostgreSQL não cria profile ou
retargeta chave. IdP não cria permissão DB-Notifier. Custodiante não decide
purpose. MOD-12 continua exclusivamente sob ADR-0007.

### Data

Tokens/CEKs/plaintext permanecem fora da persistência comum; JWK/JWKS público
é integrity-critical; profile é security metadata imutável; JWS/JWE herdam a
classificação do payload; ciphertext continua sensível; replay usa somente
digest purpose-bound se o caso exigir; eventos usam o `AuditEntry` MOD-11
sanitizado. Nenhuma tabela ou cache foi criado.

### Egress e infraestrutura

`human-identity` permanece somente para discovery/JWKS atual. Três IDs são
apenas candidatos não compilados:

- `human-identity-backchannel` para eventual token/introspection/revocation;
- `server-key-custody` para eventual data plane de vault/KMS/HSM; e
- `server-workload-identity` para eventual STS/bootstrap.

Nenhum endpoint, CIDR, credencial, SDK, identity, policy ou serviço foi
selecionado. Link-local/metadata continua hard-denied; não há IMDS implícito,
ambient credential chain, redirect, proxy ou egress escolhido por token.

## Matriz de requisitos `JOSE-REQ-*`

### Catálogo de testes planejados

| Test ID | Objetivo futuro |
|---|---|
| `JOSE-TEST-DOC-001` | reconstruir archive, verificar hashes, row counts, chaves compostas e zero delta |
| `JOSE-TEST-DOC-002` | provar 100% de classificação, eixos independentes e zero `Unreviewed` |
| `JOSE-TEST-ARCH-001` | provar Domain/Application/providers sem tipos JOSE/IdentityModel/crypto concretos |
| `JOSE-TEST-PARSE-001` | `N-1/N/N+1`, duplicate JSON, UTF-8/base64url, depth, nodes, headers, claims e `crit` |
| `JOSE-TEST-JWS-001` | Compact/Flattened/General, todas as assinaturas, detached exact bytes e `b64=false` não-JWT |
| `JOSE-TEST-JWE-001` | Compact/Flattened/General, AAD, roster, tamper, CEK/IV/tag, nested e error uniformity |
| `JOSE-TEST-JWK-001` | private-member refusal, alg/kty/use/key_ops/status, collision/ambiguity, refresh/LKG |
| `JOSE-TEST-IDENTITY-001` | access versus ID token, `(issuer,subject)`, issuer collision, RBAC/tenant/offboarding |
| `JOSE-TEST-REPLAY-001` | replay purpose-bound, expiry, cardinalidade, concorrência e commit atómico |
| `JOSE-TEST-EGRESS-001` | zero-hit token URLs, pinned DNS/TLS, redirect/proxy/credential/IMDS refusal e outage |
| `JOSE-TEST-LIFECYCLE-001` | rotation sign/decrypt, denylist, stale cache, compromise, rollback e restore |
| `JOSE-TEST-DATA-001` | canary em logs/audit/stores, retention, backup/restore e ciphertext/key dependency |
| `JOSE-TEST-SUPPLY-001` | licença, provenance, advisories, lockfile, SBOM, .NET 10 Windows/Linux e differential |
| `JOSE-TEST-ROLLBACK-001` | binário/profile/schema/protocolo/session/egress/key lifecycle independentes |

Nenhum `JOSE-TEST-*` de produto ou spike foi executado em `JOSE-0`. Os IDs
definem rastreabilidade futura, não resultados.

### Requirement → owner → threat → teste → gate

| Requirement | Profile/scope e standard | Owner/enforcement | Threats | Testes/evidência | Gate |
|---|---|---|---|---|---|
| `JOSE-REQ-001` — selecionar purpose/profile antes do objeto; manter JOSE fora de Domain/Application | todos; RFC 8725 | Server composition + Security boundary | `JOSE-T01`, `T05`, `T17` | `JOSE-TEST-ARCH-001`, `JOSE-TEST-JWS-001`, cross-profile negatives | `JOSE-D1`, depois lotes próprios |
| `JOSE-REQ-002` — impor bytes, JSON, headers, cardinalidade, candidatos, trabalho, tempo e memória | cinco profiles; RFC 7515/7516/7797 | Security boundary/resource owner | `JOSE-T02`, `T06`, `T07`, `T15` | `JOSE-TEST-PARSE-001`, fuzz, `N-1/N/N+1` | `JOSE-1` + `JOSE-D1` |
| `JOSE-REQ-003` — capturar/preservar/classificar 100% dos snapshots | IANA JOSE/JWT Claims | Security standards owner | `JOSE-T15` | `JOSE-TEST-DOC-001`, `JOSE-TEST-DOC-002`; hashes e 318/318 | `JOSE-0` |
| `JOSE-REQ-004` — ligar alg/kty/key-version/purpose/tenant/environment/direction | todos; RFC 7517/8725/9864/9964 | Profile registry + custodian policy | `JOSE-T01`, `T10`, `T12` | `JOSE-TEST-JWS-001`, `JOSE-TEST-JWK-001`, `JOSE-TEST-LIFECYCLE-001`; wrong-key/cross-purpose negatives | `JOSE-D1`, `JOSE-2/3/4` |
| `JOSE-REQ-005` — autenticar JWE antes de plaintext e controlar CEK/IV/roster/leakage | JWE/nested; RFC 7516/7520 | Security boundary + data owner | `JOSE-T07`, `T08`, `T16` | `JOSE-TEST-JWE-001`, memory/concurrency | `JOSE-1/4` |
| `JOSE-REQ-006` — separar custódia, operações e cerimônias sign/encrypt/compromise | future key operations | Platform/Security/MOD-11 | `JOSE-T05`, `T09`, `T11`, `T13` | `JOSE-TEST-LIFECYCLE-001` e tabletop | `JOSE-3/6`; não autorizado |
| `JOSE-REQ-007` — resolver JWKS/custódia somente por egress preconfigurado | human/JWK/future custody | R-NET + trust resolver | `JOSE-T03`, `T09`, `T14` | `JOSE-TEST-JWK-001`, `JOSE-TEST-EGRESS-001` | `JOSE-3/6`; não autorizado |
| `JOSE-REQ-008` — separar access/ID/session e migrar identidade para `(issuer,subject)` com RBAC server-side | human; RFC 9700/9068 | MOD-01/MOD-09/MOD-11 + Data | `JOSE-T01`, `T04`, `T12` | `JOSE-TEST-IDENTITY-001`; migration rehearsal | `JOSE-5A/B/C`; não autorizado |
| `JOSE-REQ-009` — persistir somente por classificação, replay digest e audit canónico | data/MOD-11; ADR-0004 | Data owner + Application + MOD-11 | `JOSE-T10`, `T12`, `T13`, `T16` | `JOSE-TEST-REPLAY-001`, `JOSE-TEST-DATA-001` | `JOSE-3/5`; não autorizado |
| `JOSE-REQ-010` — preservar ADRs 0002/0003/0005/0007 e autoridade de cada módulo | Agent, updates, MOD-12 | affected-module owners | `JOSE-T17` | `JOSE-TEST-ARCH-001`, `JOSE-TEST-ROLLBACK-001`; compatibility/authority review | sublote/ADR próprio |
| `JOSE-REQ-011` — biblioteca aprovada, zero primitiva própria, provenance/SBOM/interoperabilidade | .NET 10; JWS/JWE/JWK | Platform/Security/CI | `JOSE-T02`, `T15` | `JOSE-TEST-SUPPLY-001`, differential | `JOSE-1` + `JOSE-D1` |
| `JOSE-REQ-012` — separar roadmap, implementação, verificação, homologação, runtime, autorização e claim | governance/todos | lifecycle/release owners | `JOSE-T17` | `JOSE-TEST-DOC-002`, `JOSE-TEST-ROLLBACK-001`; matriz factual e gate/claim review | todos; claim somente `STATE-08` |

`Txx` abreviado na coluna acima sempre significa `JOSE-Txx`. Todos os 12
requirements possuem owner, threat, teste e gate; nenhum threat canónico está
órfão.

## Threat model

O [Threat Model](architecture/Threat-Model.md) é a fonte canónica dos
17 riscos `JOSE-T01`–`JOSE-T17`, com boundary, risco inerente, control owner,
enforcement, vetor futuro e risco residual. Em resumo:

- confusão de algoritmo/tipo/purpose e cross-JWT;
- parser/header/`kid` abuse e DoS;
- SSRF/trust injection/JWKS amplification;
- theft/browser/subject collision/RBAC;
- signing/decryption oracle;
- signature/recipient stripping;
- GCM IV/CEK reuse;
- KMS confused deputy e ambient credential;
- profile/key rollback/split-brain;
- rotation/compromise;
- replay/cross-tenant;
- audit flood/leakage;
- outage/LKG stale;
- supply chain e ML-DSA size/cost;
- metadata/size leakage; e
- substituição indevida de mTLS, protocolo, package signing, ADR-0007 ou
  Human Gate.

Os controles são desenho não implementado. Como todos os novos profiles e
egress candidates estão desabilitados, o lote não cria exposição operacional
JOSE nova. As duas lacunas factuais do relying party/identidade atual
permanecem dívida explícita e bloqueiam claims mais fortes ou expansão
multi-issuer.

## Plano de testes

### `JOSE-0` documental

1. Verificar o shutdown preflight e ausência de mudanças não documentais.
2. Decodificar o archive somente em memória e comparar ZIP/entry hashes.
3. Parsear os CSVs preservados e comparar denominadores, chaves compostas,
   grupos de claims e disposition counts.
4. Verificar traceabilidade bidirecional
   requirement ↔ threat ↔ owner ↔ test ↔ gate.
5. Executar link gate, documentation gate, secret scan e diff checks.
6. Fazer revisão independente de arquitetura, standards/registry e safety
   caps.

### `JOSE-1` test-only futuro — não autorizado

1. Avaliar bibliotecas sem introduzir primitiva criptográfica própria.
2. Usar somente chaves efêmeras únicas por execução e inputs
   públicos/sanitizados.
3. Cobrir os cinco profiles e as seis serializações nucleares
   JWS/JWE Compact, Flattened e General.
4. Executar `N-1/N/N+1`, malformed/duplicate/tamper, fuzz/property,
   differential e interoperabilidade.
5. Medir bytes, nós, operações, memória contabilizada, heap/working set
   empírico, tempo e quiescence.
6. Reconciliar .NET 10 Windows/Linux, licence, provenance, advisories,
   lockfiles e SBOM.
7. Publicar evidência sanitizada para `JOSE-D1`, sem runtime de produto.

### Lotes posteriores — não autorizados

- `JOSE-2`: JWS/JWT hardening e conformance de assinatura.
- `JOSE-3`: JWK/JWKS e, separadamente, custódia/lifecycle.
- `JOSE-4`: JWE e nested.
- `JOSE-5`: um caso de produto por sublote/owner.
- `JOSE-6/7`: homologação externa e release somente nos lifecycle states
  próprios.

## Plano de migração

Nenhuma etapa abaixo está autorizada para execução.

| Ordem | Mudança futura | Pré-condição e prova | Falha/rollback boundary |
|---|---|---|---|
| `M0` | congelar testes e caracterizar relying party atual | issuer/audience/skew/claims/alg/type observados sem registrar token | somente documentação/testes |
| `M1` | introduzir `human-access-token-inbound.v0` desabilitado | ADR aceito, código autorizado, comportamento equivalente provado | remover/desabilitar revision candidata |
| `M2` | adicionar representação explícita `(issuer,subject)` e unicidade composta | decisão de identidade/dados, migration própria, backup e rehearsal | Down bloqueado se houver dependência nova; preferir forward-fix |
| `M3` | mapear cada utilizador legado para issuer revisto | mapping administrativo por linha; zero issuer inferido; preservar `user_id`/roles e o audit subject-only histórico sem rewrite | utilizador sem prova é recusado fail-closed; não apagar legado nem inventar quarantine |
| `M4` | mudar resolver/RBAC para identidade composta e versionar a identidade de novos eventos de audit | todos os consumidores e queries compatíveis; issuer collision negatives; audit legado continua distinto | antes do cutover permanece apenas o caminho atual de issuer único; depois de habilitar identidade composta ou multi-issuer, fallback subject-only é proibido |
| `M5` | impor `typ` e allowlist algorítmica/claims por issuer/profile | IdP exacto e interoperability; `RS256` sunset documentado | restaurar último profile aceito, nunca algoritmo proibido |
| `M6` | integrar BFF/session/login, se selecionado | decisão browser/password/IdP, CSRF/CORS/cookies/logout/MFA/audit | sessão e token store possuem rollback/invalidade próprios |
| `M7` | adicionar producer JWS/JWE por caso | use case, owner, key custody, data/egress/audit e key lifecycle aprovados | desabilitar apenas o profile/caso; preservar decrypt dependency |

### Regras de migração de identidade

- `issuer` é valor canónico validado, não display name, tenant alias ou
  configuração copiada automaticamente.
- `subject` permanece opaco e só tem significado dentro do issuer.
- Não concatenar strings para simular chave composta.
- `user_id` continua a chave interna estável de utilizadores, roles e
  referências relacionais existentes.
- Audit subject-only histórico permanece append-only, sem FK ou issuer
  retroativo; novos eventos usam formato explicitamente versionado e
  issuer-bound somente depois da decisão própria.
- O cutover recusa actor sem identidade composta provada.
- A unicidade global de subject só é removida depois da prova de todas as
  queries/constraints e do mapping integral.
- Login/BFF/session store não entra junto da migration de identidade por
  conveniência.

### Compatibilidade

| Camada | Versiona separadamente | Compatibilidade mínima |
|---|---|---|
| Binário | versão do Server/BFF | entende profile atual e candidato somente na janela autorizada |
| Profile/config | revision monotônica + digest | nunca downgrade, fallback amplo ou mutação em place |
| Dados | migration/provider próprio | identity/replay/metadata não se misturam a store monitorado |
| Protocolo | major/schema ADR-0003 | permanece inalterado salvo decisão do owner |
| Browser/session | cookie/session/token-store schema | access/ID/refresh token mutuamente exclusivos |
| Egress | consumer/topology revision | candidato não vira uma das quatro policies atuais por nome |
| Chave | key version/generation/purpose | overlap de verify/decrypt bounded; compromise separado |

## Plano de rollback

### Rollback deste lote documental

O rollback seguro é reverter somente o commit focal que contém este pacote
documental. Não há binário, config, schema, protocol, key, service ou runtime
para restaurar. O relying party atual permanece inalterado antes, durante e
depois desse rollback.

### Rollback futuro por camada

| Camada | Trigger | Ordem segura e bloqueios |
|---|---|---|
| Profile/config | incompatibilidade, false reject ou budget inadequado | desabilitar nova revision; voltar à última aceita somente se não proibida; nunca reduzir checkpoint |
| Binário | regressão comprovada | parar composição, restaurar binário compatível e profile anterior; schema/protocolo precisam permitir |
| Identity/schema | migration/backfill incorreto | preservar `user_id` e mappings; Down bloqueado com issuer collision/dependência; preferir forward-fix |
| Browser/session | CSRF/session/logout/token-store failure | bloquear novas sessões, invalidar estado novo, restaurar fluxo aprovado sem expor token |
| Egress | destination/SDK/credential boundary não provado | remover somente consumer candidate/config; manter hard deny e políticas existentes |
| Signing lifecycle | producer/key incompatível | parar producer, manter verify overlap necessário; não reativar chave revoked |
| Encryption lifecycle | decrypt/key dependency | parar novos envelopes, manter decrypt-only enquanto ciphertext/queue/backup depender; nunca destruir cedo |
| Agent/protocolo | mudança owner-specific falha | seguir ADR-0003/outbox/idempotency; JOSE flag não pode descartar fila |
| MOD-12 | qualquer tentativa de acoplamento | recusar integração; ADR-0007/checkpoints/activation permanecem intocados |

Nenhum rollback pode ampliar trust, reabilitar algoritmo proibido, reduzir
generation/checkpoint, trocar purpose, apagar audit, perder idempotência,
romper offline Agent ou retirar chave necessária para decrypt.

## Revisões independentes

Três frentes somente leitura foram concluídas:

1. inventário/classificação IANA JOSE: 155/155;
2. inventário/classificação JWT Claims: 163/163; e
3. auditoria de arquitetura/autoridades, que identificou e fez o desenho
   tratar a incompatibilidade `(issuer,subject)`, a ausência de alg/type
   allowlist e a necessidade de manter egress candidates fora das quatro
   policies atuais.

Uma quarta revisão independente dos caps tornou os valores mais estritos por
profile, separou limite enforceable de deadline cooperativo e confirmou os
tamanhos RFC 9964. Nenhum subagente editou o workspace.

Os rechecks finais encontraram zero P0 e zero P1 depois de:

- eliminar toda combinação incoerente `SafelyAdapted + NotPlanned` e
  recontar JOSE como `33/44/78`;
- tratar audit subject-only histórico como append-only, sem issuer inferido,
  e proibir fallback subject-only depois do cutover composto/multi-issuer;
- fixar o envelope de memória por objeto JOSE completo e classificar
  credencial STS/workload;
- corrigir a semântica sem timezone do timestamp ZIP/DOS, sem alterar os
  bytes/hashes retidos;
- preservar o skew atual de 60 segundos, incluir Compact detached e permitir
  somente chave efêmera memory-only num futuro teste autorizado; e
- ligar todos os 14 `JOSE-TEST-*` por ID completo aos 12 requisitos.

## Quality Gate documental

| Gate | Comando/evidência | Resultado |
|---|---|---|
| Archive e entry hashes | reconstrução ZIP em memória | `APROVADO` — 10/10 entries e hashes |
| Cobertura mecânica | comparação CSV ↔ matriz/grupos | `APROVADO` — JOSE 155/155, JWT 163/163, total 318/318 e zero delta |
| Rastreabilidade | IDs requirement/threat/test | `APROVADO` — 12/12 requisitos, 17/17 threats e 14/14 Test IDs ligados |
| Links Markdown | `node scripts/verify-markdown-links.mjs` | `APROVADO` — 886 links locais em 221 arquivos |
| Code documentation gate | `node scripts/verify-code-documentation.mjs` | `APROVADO` — 429 fontes comment-capable |
| Secret scan | `./scripts/verify-secrets.ps1` | `APROVADO` — worktree não ignorado e histórico Git disponível |
| Diff | `git diff --check` e staged diff review | `APROVADO` |
| Build/test/runtime | fora do escopo documental | `NÃO APLICÁVEL`; não executado |

Os gates automáticos documentais passaram. O identificador do commit local
focal será informado no hand-off. Revisão humana do pacote permanece
separada. Nada neste relatório aceita ADR-0008 ou autoriza `JOSE-1`.

## Limitações e riscos residuais

- Os caps ainda não foram medidos por biblioteca/plataforma; são provisórios.
- Nenhuma biblioteca, IdP, browser architecture, custodian ou topology foi
  selecionada.
- A postura de password versus IdP/browser continua decisão futura da
  autoridade de identidade; JOSE não a resolve por inferência.
- O relying party atual ainda não possui o profile alg/type proposto.
- A identidade persistida ainda é subject-only.
- Nenhuma rotação, compromise recovery, outage, HA, cache, migration, backup,
  restore, cross-platform, fuzz, load, pen test ou crypto review operacional
  foi executada.
- Validade criptográfica continua sem provar revogação humana imediata.
- Ciphertext continua revelando tamanho, headers e recipient metadata.

## Estado resultante

- `STATE-06 INTEGRATION`: inalterado.
- ADR-0008: `proposed`.
- `JOSE-0`: pacote documental e Quality Gate automático concluídos; revisão
  humana do pacote preparatório permanece pendente e não aceita o ADR.
- `JOSE-1`: não autorizado.
- `JOSE-D1`: não executado; ADR não aceito.
- Código/dependências/migrations/runtime/IdP/chaves/vault/KMS/HSM/deploy:
  zero alteração.
- MOD-12: inalterado e não ativado.
