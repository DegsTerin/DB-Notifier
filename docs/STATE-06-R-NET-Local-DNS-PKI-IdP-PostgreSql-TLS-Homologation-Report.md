# STATE-06 — Relatório da homologação local R-NET de DNS, PKI, IdP e PostgreSQL TLS

## Decisão

O lote condicional local `R-NET` está automaticamente `APROVADO` somente para
a matriz física controlada descrita neste relatório.

A campanha confirmou o resolvedor de loopback do sistema, as decisões
fail-closed de DNS, a validação TLS pelo mecanismo de cadeia do sistema
operacional com uma raiz sintética restrita ao processo de teste, o fluxo
HTTPS local de discovery/JWKS/JWT e os caminhos produtivos diretamente
afetados contra células PostgreSQL 16 descartáveis com TLS.

Este resultado complementa, sem substituir, o
[relatório determinístico R-NET](STATE-06-R-NET-Network-Egress-Remediation-Report.md).
Ele não homologa infraestrutura ou provider operacional, não concede suporte
público e não altera `STATE-06 INTEGRATION`, Human Gate ou
`ActivationState=None`.

## Autoridade e limites

- Autoridade: homologação local de `R-NET` em fixtures controladas de DNS,
  PKI, IdP e PostgreSQL TLS, incluindo shutdown, implementação, testes,
  cobertura, documentação e commit focal.
- Baseline: commit `e23bb51`, posterior aos lotes locais R-SEQ e
  R-EGRESS/R-FENCE.
- Data local: 2026-07-26.
- Incluído: seams internos de política de cadeia para teste, fixtures
  sintéticas, runner bounded, regressões, campanha física e documentação
  técnica proprietária.
- Proibido e não executado: novas dependências, download, credencial ou dado
  real, infraestrutura remota, migration operacional, runtime normal do
  produto, lifecycle, Human Gate, `ActivationState`, push, pull request ou
  deploy.

O shutdown preflight comprovou zero processo, listener, janela ou recurso
Docker próprio do DB-Notifier antes da ação. Serviços monitorados, navegador
comum, IDE e processos alheios foram preservados.

## Implementação focal

Os três consumidores TLS diretamente exercitados passaram a possuir uma seam
`internal` que recebe uma fábrica de `X509ChainPolicy`:

- `NetworkBoundHttpMessageHandlerFactory`;
- `NetworkBoundServerDbContextFactory`;
- `NpgsqlAuthenticatedExecutor`.

Os construtores públicos e a composição produtiva não mudaram de contrato:
continuam criando uma política nova por uso com `System` trust,
`X509RevocationMode.Offline`, `EntireChain`, `NoFlag`, ServerAuth, downloads
de certificado desabilitados e `CustomTrustStore` vazio. Nenhum callback
permissivo, bypass, variável de ambiente, binding de configuração ou registro
de DI produtivo foi introduzido.

Somente `DBNotifier.IntegrationTests`, via `InternalsVisibleTo`, acessa a seam.
A fábrica test-only:

1. carrega exatamente uma CA pública sintética, sem chave privada;
2. parte da política produtiva offline;
3. altera somente `TrustMode` para `CustomRootTrust`;
4. adiciona exatamente aquela CA ao `CustomTrustStore`;
5. devolve uma política nova por handler ou conexão, conforme o consumidor,
   e descarta o material ao final.

Essa separação permite provar o mecanismo de cadeia e revogação do Windows sem
instalar uma raiz no sistema e sem criar uma opção de trust custom no runtime
normal.

## Topologia e contenção

O runner
[`run-r-net-local-homologation.ps1`](../scripts/run-r-net-local-homologation.ps1)
exige o marcador exato
`DBNOTIFIER_RNET_LOCAL_HOMOLOGATION=local-test`; a suíte de integração comum
permanece inerte sem o marcador.

Para PostgreSQL, o runner usa somente a imagem já presente
`postgres:16-alpine`, fixada por
`sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb`,
com `--pull never`. Cada célula:

- executa sequencialmente, com CPU, memória, PIDs, health check, filesystem
  read-only e `tmpfs` bounded;
- publica uma única porta efêmera em `127.0.0.1`;
- pertence a labels e nomes exclusivos da execução;
- usa uma bridge dedicada com masquerade e comunicação entre contêineres
  desabilitados e binding padrão de host em loopback;
- substitui o resolver efetivo por duas linhas exatas que apontam somente para
  loopback, prova que um nome `.invalid` não resolve e remove as rotas default
  IPv4/IPv6 antes do entrypoint;
- comprova, sem enviar pacote não local, que endereços externos não possuem
  rota, que o PID 1 do PostgreSQL não é root, que suas capacidades efetivas,
  permitidas, herdáveis e ambientes são zero com `NoNewPrivs=1` e que o
  listener Windows existe somente em `127.0.0.1`;
- usa apenas credenciais sintéticas efêmeras, fora de argumentos, ambiente,
  relatório e saída;
- não reutiliza volume nem aplica migration a banco existente.

O runner inspeciona as opções da bridge, a publicação efetiva, o listener do
host, a tabela de rotas, o DNS configurado e o SHA-256 do certificado servido
antes de prosseguir. Uma tentativa com rede Docker `internal` foi descartada
porque o Docker Desktop removeu a publicação de loopback; a alternativa final
manteve somente o segmento local necessário à resposta do PostgreSQL, removeu
a rota default e desabilitou NAT de saída.

Um lock exclusivo impede campanhas concorrentes. O preflight recupera somente
contêineres/redes com labels, nomes e run IDs exatos e somente uma CRL cujo
par CA/CRL preservado tem subject esperado e assinatura RSA/SHA-256 válida. Se
a remoção da CRL falhar em cleanup controlado, o diretório público de
recuperação é preservado para a próxima execução em vez de apagar a evidência.

## Matriz física observada

O resumo fechado final passou `11/11` casos em cinco invocações bounded. As
quatro células PostgreSQL foram iniciadas e removidas sequencialmente.

| Grupo | Caso | Resultado observado |
|---|---|---|
| DNS | `SystemDnsResolver` para `localhost` | somente endereços loopback admitidos |
| DNS | resposta controlada com loopback e metadata | conjunto inteiro recusado; nenhum socket aberto |
| DNS | resposta muda entre admissões | aprovado → recusado → aprovado; somente IP aprovado chegou ao connector |
| PKI/HTTPS | certificado válido, SAN correto, ServerAuth e CRL local | cadeia e HTTPS aceitos |
| PKI/HTTPS | SAN incorreto | cadeia criptográfica válida, handshake HTTPS recusado por identidade |
| PKI/HTTPS | EKU incorreto | `NotValidForUsage`; Kestrel recusou a folha antes de publicar o listener |
| PKI/HTTPS | folha revogada | cadeia e HTTPS recusados por revogação offline |
| PKI/HTTPS | raiz não confiável | cadeia e HTTPS recusados |
| PKI/HTTPS | CRL local ausente | cadeia e HTTPS recusados por revogação desconhecida/offline |
| IdP HTTPS | discovery, JWKS e JWT válidos | recurso protegido retornou `200` |
| IdP HTTPS | redirect ou JWKS cross-origin | `401`; destino e sink receberam zero request |
| IdP HTTPS | issuer, audience ou signing key incorretos | `401` em cada caso |
| PostgreSQL central | caminho público com `System` trust | CA sintética recusada, como esperado |
| PostgreSQL central | seam test-only e certificado válido | `VerifyFull`; `pg_stat_ssl.ssl=true` |
| Provider PostgreSQL | credencial válida | `Healthy` sobre TLS verificado |
| Provider PostgreSQL | senha incorreta | `AuthenticationFailed`, distinto de TLS/rede |
| Central e provider PostgreSQL | hostname incorreto | conexão recusada por `VerifyFull`; provider normalizou para `Unavailable` |
| Central e provider PostgreSQL | raiz não confiável | conexão recusada; provider normalizou para `Unavailable` |
| Central e provider PostgreSQL | CRL ausente | conexão recusada; provider normalizou para `Unavailable` |
| Central e provider PostgreSQL | certificado revogado | conexão recusada; provider normalizou para `Unavailable` |
| Provider PostgreSQL | política de rede nega antes do transporte | `InvalidConfiguration`, secret lido uma vez e zero sessão amostrada |

As linhas da matriz são asserções internas de onze casos agregados; não
representam uma execução independente por linha da tabela.

## PKI e cache de revogação

A raiz sintética usada na campanha permaneceu ausente de `CurrentUser\Root`
antes, durante e depois da execução. Somente a CRL pública exata da CA
sintética foi registrada temporariamente em `CurrentUser\CA` pela API
criptográfica do Windows. O runner comprovou sua presença e removeu exatamente
o contexto criado; a auditoria externa pós-run confirmou zero certificado
DB-Notifier R-NET em `Root`, `CA` e `My`.

Três abordagens preliminares de instalação de raiz foram recusadas e não
integram a evidência de aprovação:

- `X509Store.Add` acionou um aviso protegido do Windows;
- a importação oficial com opção de não exibir UI também acionou o aviso
  protegido;
- `certutil -user -f -silent -addstore Root` terminou com código `1`.

Nenhum diálogo foi aceito ou automatizado. Cada tentativa foi interrompida,
os recursos próprios foram removidos e a verificação retornou a zero raízes
sintéticas instaladas. A campanha final evitou completamente o Root store.

Os artefactos-fonte persistidos PEM/PKCS#8 ficaram somente no diretório
temporário com ACL restrita ao utilizador atual e LocalSystem. Cópias
transitórias existiram no `tmpfs` da célula PostgreSQL e, para compatibilidade
com SChannel/Kestrel, num contexto de chave current-user
`UserKeySet | Exportable`, sem `PersistKeySet`. Contêiner e contexto foram
encerrados no fluxo controlado; diretório e buffers secretos foram removidos
ou zerados. A auditoria externa comprovou zero certificado nos stores, não
uma enumeração separada de key containers.

## Evidência automática

Ambiente: Windows, SDK .NET local `10.0.301`, configuração `Release`, sem
restore online, download ou dependência nova.

| Verificação | Resultado observado |
|---|---|
| Campanha física R-NET | `11/11` casos em cinco invocações bounded |
| Build Release da solução | `19` projetos, `0` warnings, `0` erros |
| Suíte unitária completa | `508/508` |
| Suíte de arquitetura completa | `96/96` |
| Suíte de integração comum | `137/137`, com fixtures R-NET inertes sem marcador |
| Suíte WPF completa | `10/10` |
| Cobertura .NET | `83,37%` linhas e `56,26%` branches; `10/10` componentes presentes |
| `dotnet format --verify-no-changes --no-restore` | aprovado |
| Parser e PSScriptAnalyzer do runner | aprovados |
| Documentação de código | aprovada para `415` arquivos comment-capable |
| Lockfiles | inventário estático de `19/19` projetos, sem falta ou alteração |
| Links Markdown | `792` links locais em `204` arquivos aprovados |
| Secret scan | worktree não ignorado e histórico Git disponível aprovados |

O gate normativo agregado passou os pisos de `70%` de linhas e `45%` de
branches, além da meta orientativa de risco de `80%` de linhas. O piso já
existente de presença por componente permaneceu em `1%` e não foi reduzido.
Uma execução diagnóstica adicional com `70%` por assembly não é o contrato do
repositório e, corretamente, não foi usada para reclassificar o gate.

Locked restore, freshness de advisories e qualquer consulta online não foram
executados: a configuração NuGet aponta para fonte externa, enquanto este
lote proibia download e infraestrutura externa. O inventário estático prova
somente completude dos lockfiles versionados.

## Diagnósticos intermediários e repetição final

Execuções intermediárias identificaram e corrigiram exclusivamente problemas
do harness:

- compilação da fixture após remoção de uma estratégia de Root store;
- uma anotação nullable incompatível com o modo do `Add-Type`, recusada antes
  da criação de qualquer fixture;
- incompatibilidade de `EphemeralKeySet` com o handshake local
  SChannel/Kestrel;
- recusa antecipada, pelo servidor, da folha com EKU incorreto;
- ausência de port binding na rede Docker `internal`.
- nome de variável shell que coincidia com uma assinatura genérica do secret
  scan, sem conter credencial real; o nome foi corrigido e a campanha física
  completa foi repetida.

Cada execução parcial terminou com cleanup comprovado. Nenhuma foi contada
como aprovação. Somente a repetição final limpa, com `11/11`, compõe a decisão
deste relatório.

## Cleanup final da campanha

Ao final da repetição física foram observados:

- processos e listeners próprios: `0`;
- contêineres e redes R-NET: `0`;
- volumes próprios: `0`;
- diretórios temporários e logs de lançamento próprios: `0`;
- certificados DB-Notifier R-NET em `CurrentUser\Root`, `CA` e `My`: `0`;
- CRL sintética própria registrada: `0`.

O shutdown final após todos os gates é registrado no
[State Transition Log](../prompts/state/State-Transition-Log.md).

## Limites e riscos restantes

- A campanha não prova DNSSEC, resolver corporativo, cache ou rebinding
  temporal fora do host local.
- O recovery preflight e o retry exato de CRL foram implementados e revistos,
  mas uma terminação não cooperativa ou crash do host não foi injetado na
  campanha final.
- `CustomRootTrust` pertence somente ao processo de teste. O runtime produtivo
  continua dependente do trust e do cache de revogação do sistema operacional.
- Não houve provisionamento operacional de CA, CRL, OCSP, certificado ou
  rotação.
- O IdP era uma fixture HTTPS local; discovery, JWKS e tokens corporativos
  permanecem não homologados.
- PostgreSQL era uma célula descartável local. Topologia operacional,
  provider/version/platform support, proxy, failover, HA, performance e
  disponibilidade permanecem não homologados.
- O resultado não abre firewall, cria túnel, publica endpoint, instala
  software, carrega credencial real ou autoriza tráfego remoto.
- A homologação de provider permanece `None` e o suporte público permanece
  `No`.

## Estado resultante

- R-NET local DNS/PKI/IdP/PostgreSQL TLS: automaticamente `APROVADO` somente
  para a matriz física controlada.
- `STATE`: permanece `STATE-06 INTEGRATION`.
- `ActivationState`: permanece `None`.
- Human Gate: nenhum executado ou inferido.
- Migration operacional, runtime normal e ação externa: nenhum executado.
