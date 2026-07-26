# STATE-06 — Relatório de remediação R-NET da política de network egress

## Decisão

O lote técnico local `R-NET` está automaticamente `APROVADO` somente no
escopo validado neste relatório.

Os caminhos de saída que a composição normal pode habilitar no Agent,
provider e Server agora exigem uma política positiva e imutável por
consumidor. A autorização resolve o destino, avalia atomicamente todas as
respostas, nega classes proibidas e entrega somente IPs aprovados para a
conexão física. O hostname original permanece separado e serve somente à
identidade TLS quando aplicável.

O resultado não autoriza ou comprova um Agent, Server, provider, IdP, PKI,
resolvedor ou PostgreSQL operacional. Ele não altera `STATE-06 INTEGRATION`,
não constitui Human Gate ou decisão de lifecycle e preserva
`ActivationState=None`.

## Autoridade e escopo

- Autoridade: identificação e execução sequencial, sem pausas intermediárias,
  de todos os lotes técnicos locais ainda obrigatórios, com shutdown,
  implementação, testes, cobertura, documentação e commit focal por lote.
- Baseline do lote: commit `3988d3a`.
- Data local: 2026-07-26.
- Incluído: contratos neutros, política local, DNS bounded, pinning de IP,
  HTTP direto, PKI offline, Agent, provider PostgreSQL, Server, persistência
  central, shim PowerShell, regressões e documentação proprietária.
- Proibido e não executado: novas dependências, migrations operacionais,
  runtime de produto, conexão externa, DNS/IdP/PKI/PostgreSQL real, lifecycle,
  Human Gates, `ActivationState`, push, pull request ou deploy.

O shutdown preflight obrigatório observou zero processo, listener ou janela
de produto DB-Notifier. IDE, navegador, database engine, serviço monitorado e
processos alheios foram preservados.

## Diagnóstico final

O threat model já exigia allow/deny CIDR, revalidação de DNS e proteção contra
SSRF, mas essa regra ainda não possuía um proprietário executável comum.
Cada consumidor delegava parte da decisão ao seu mecanismo de transporte:

| Superfície anterior | Lacuna observada |
|---|---|
| sincronização Agent → Server | `HttpClientHandler` podia herdar redirect, proxy, resolução e seleção de endereço |
| `pg_isready` e fallback TCP | o hostname configurado chegava ao processo ou socket, que podia resolver novamente |
| probe autenticado Npgsql | a biblioteca recebia hostname e controlava a conexão física |
| backchannel OIDC | o handler padrão não partilhava uma allowlist de destino com os demais consumidores |
| PostgreSQL central | `UseNpgsql` recebia diretamente a connection string configurada |
| validação TLS | revogação online podia originar AIA, CRL ou OCSP fora de uma política de egress |
| compatibilidade PowerShell | uma entrada remota parseável podia chegar a `pg_isready` ou `TcpClient` |

Essa fragmentação permitia diferenças de política, redirects involuntários,
uso de proxy ou credencial ambiental e uma janela DNS entre validação e
conexão. Transport reachability continuava corretamente sem provar saúde, mas
o destino físico ainda não era uma decisão positiva única.

## Correção implementada

### Autoridade neutra e imutável

Foram introduzidos contratos provider-neutral para quatro políticas exatas:

| Política | Proprietário |
|---|---|
| `agent-synchronization` | sincronização autoritativa do Agent |
| `provider-monitoring` | probes do provider |
| `human-identity` | metadata e chaves públicas OIDC |
| `server-database` | persistência PostgreSQL central do Server |

Cada política é compilada uma vez e exige ao menos um CIDR positivo e uma
porta exata. A configuração é limitada por quantidade de políticas, CIDRs,
portas, prazo DNS e número de respostas. Mutação posterior do objeto de
configuração não altera a autoridade compilada.

Um deny explícito sempre vence. Também são recusados independentemente da
configuração: unspecified, `0.0.0.0/8`, link-local, multicast, broadcast,
espaço IPv4 reservado, endereços conhecidos de metadata e IPv6 com scope. IPs
mapeados são normalizados antes da decisão. Uma única resposta DNS recusada
nega o conjunto completo; não existe seleção silenciosa apenas dos resultados
convenientes.

### DNS, HTTP e pinning

Nomes passam por uma resolução bounded em cada nova admissão. A autoridade
retorna cópias defensivas dos IPs aprovados. O handler HTTP:

- desabilita redirects, proxy, cookies, credenciais ambientais, pre-auth e
  decompression automática;
- reautoriza o destino para cada novo socket físico;
- conecta diretamente a um `IPEndPoint` aprovado, sem segunda resolução;
- preserva o hostname da URI para Host, SNI e verificação do certificado;
- limita conexão, pooling e concorrência.

A sincronização classifica qualquer `3xx` como
`sync.redirect_refused/Retryable`; nenhuma observação é reconhecida ou
removida do outbox por esse resultado. O backchannel OIDC admite somente GET
na origem HTTPS configurada, recusa redirects e encoding e lê JSON pelo
contrato compartilhado de media type e tamanho máximo.

### Provider PostgreSQL e persistência central

`pg_isready`, o fallback TCP e Npgsql recebem somente o IP aprovado. Npgsql
retém o hostname original em `TargetHost`, com `VerifyFull`, para provar SNI e
identidade sem reconectar por nome. Negativa de política resulta em
`Unknown`, erro de configuração sanitizado e retry somente após alteração de
configuração.

Um Unix-domain socket existente continua local e fora da política de rede. A
persistência central aceita somente um host TCP, database não vazio,
`VerifyFull`, revocation check declarado e ausência de trust bypass,
diagnóstico sensível, root custom ou multi-host. Sua resolução é lazy; o
primeiro uso autoriza todas as respostas e fixa o primeiro IP aprovado no
data source pelo restante do processo.

### PKI e autenticação

Cadeias TLS de Server e client usam trust do sistema operacional, EKU exato,
`X509RevocationMode.Offline`, `X509VerificationFlags.NoFlag` e downloads de
certificados desabilitados. O certificado mTLS do Agent precisa ser uma folha
não-CA com ClientAuth e DigitalSignature, além da identidade, enrollment,
estado e revogação já exigidos.

A seleção do certificado do Agent não pede chain building implícito ao
certificate store. Ela exige exatamente um certificado temporalmente válido
com chave privada e constrói o contexto TLS em modo offline.

### Compatibilidade PowerShell

O shim legado admite somente o alias exato `localhost` ou literal loopback,
normaliza IPv4-mapped loopback e passa o literal aprovado a `pg_isready` ou
TCP. Nome remoto, IP remoto, metadata e porta inválida são recusados antes de
DNS, processo ou socket. Entradas antigas continuam parseáveis e migráveis,
mas não recebem autoridade de monitorização remota pelo runtime legado.

### Caminhos dormentes e test-only

A auditoria de usos de `HttpClient`, socket, Npgsql e SignalR também
classificou os caminhos não pertencentes à composição normal:

- Agent Fleet, command transport e command delivery permanecem sandbox-only
  ou sem registro produtivo;
- a reconciliação de notificação WPF exige flags exatas, HTTPS loopback por
  literal e certificado efêmero fixado;
- os browser runners e hosts E2E permanecem ferramentas locais test-only;
- Dashboard TV usa origem relativa e SignalR somente no sandbox explicitamente
  guardado.

Esses caminhos não receberam autoridade adicional nem foram convertidos em
runtime normal. Uma composição futura deverá usar a política R-NET e obter o
gate próprio antes de remover qualquer barreira existente.

## Evidência local

Ambiente: Windows, SDK .NET local `10.0.301`, configuração `Release`, sem
restore, nova dependência ou runtime externo.

| Verificação | Resultado observado |
|---|---|
| Regressões focais R-NET consolidadas | `78/78` aprovadas |
| Build Release da solução | aprovado com `0` warnings e `0` erros |
| Suíte unitária completa | `501/501` aprovada |
| Suíte de integração completa | `124/124` aprovada na repetição isolada |
| Suíte de arquitetura completa | `96/96` aprovada |
| Suíte WPF completa | `10/10` aprovada |
| Cobertura .NET | `82,41%` linhas, `55,89%` branches, `10/10` componentes |
| Pester legado | `34` aprovados, `1` skip previsto, `35,17%` (`338/961`) |
| `dotnet format --verify-no-changes --no-restore` | aprovado na solução completa; nenhuma mudança requerida |
| Documentação de código | aprovada para `404` arquivos comment-capable |
| Links Markdown | `768` links locais em `201` arquivos aprovados |
| Secret scan | worktree não ignorado e histórico Git disponível aprovados |
| JSON e escopo estrutural | três JSON alterados válidos; zero manifest de dependência ou migration alterado |

Os pisos obrigatórios de 70% de linhas e 45% de branches foram satisfeitos. A
meta orientativa baseada em risco de 80% de linhas também foi atingida, sem
reduzir piso por componente. O Pester superou seu piso separado de 25%.

A primeira execução completa de integração produziu `123/124`: o diagnóstico
temporal O5 preexistente
`WaitHandleCandidateIsTimelyAndCancellationAware` falhou sob carga local. O
caso passou isoladamente e a repetição completa passou `124/124`. Nenhuma
mudança R-NET toca esse diagnóstico ou a metodologia O5.

Durante a implementação, a primeira suíte de arquitetura produziu `92/93`
porque o handler OIDC lia o stream HTTP diretamente. O guard cumpriu sua
função: a leitura foi substituída pelo `BoundedHttpJsonReader` compartilhado,
o teste focal passou `3/3`. Três guards finais também fixaram a composição
normal do Agent, Server, provider e legado; a suíte completa final passou
`96/96`. Somente os resultados corrigidos compõem a decisão automática.

## Observado, inferido e não testado

### Observado

- parsing e imutabilidade de políticas, limites, allow/deny e portas;
- negativas de metadata, classes especiais, IPv6 scoped e resposta DNS mista;
- nova admissão após mudança sintética de DNS e conexão somente ao IP
  entregue pela autoridade;
- redirect recusado sem reconhecimento de observação;
- opções HTTP sem proxy, credenciais ambientais, cookies ou redirect;
- configuração TLS offline, sem download e com EKU;
- argumentos de `pg_isready`, TCP e Npgsql contendo IP aprovado;
- OIDC limitado por origem, método, tamanho, media type e encoding;
- PostgreSQL central validado, lazy e pinned sem conexão durante composição;
- legado remoto recusado antes dos mocks de processo e socket.

### Inferido

- quando corretamente configurados em runtime futuro, os consumidores não
  entregam hostname não autorizado ao socket ou processo diretamente afetado;
- a separação entre hostname TLS e IP físico remove a segunda resolução nesses
  consumidores;
- material local de trust e revogação suficiente permitirá validação offline
  sem tráfego PKI originado pelo DB-Notifier.

Essas inferências derivam da composição e dos testes de seams locais; não são
homologação operacional.

### Não testado

- resolvedor real, DNSSEC, rebinding temporal real ou comportamento de cache
  do sistema operacional;
- IdP, metadata/JWKS, PKI, trust store ou revocation cache reais;
- PostgreSQL TLS, `pg_isready`, proxy, failover, high availability ou
  múltiplos endereços sob carga real;
- latência, disponibilidade e rotação operacional de DNS/certificados;
- qualquer provider, topologia ou suporte público.

## Limites e riscos restantes

- A revogação offline falha fechado se o sistema operacional não possuir
  material local atual. Este lote não provisiona CA, CRL ou certificado.
- O trust permanece pertencendo ao sistema operacional; nenhuma CA
  específica do produto foi inventada.
- O data source PostgreSQL central fixa o primeiro IP aprovado. Rotação DNS ou
  failover exige restart controlado e nova evidência operacional.
- HTTP reavalia DNS em cada novo socket físico, mas um socket pooled já
  autorizado pode ser reutilizado por até seu limite configurado.
- Uma recusa de certificado durante o handshake TLS ocorre antes do pipeline
  HTTP e, portanto, não pode produzir o audit record de autenticação HTTP. Os
  resultados posteriores de profile, enrollment e identidade continuam
  auditados.
- O conjunto padrão de políticas é vazio. Monitoring e sincronização
  permanecem desabilitados, e configuração externa parcial falha fechado.
- A política não abre firewall, cria túnel, instala software, descobre
  infraestrutura ou concede credencial.

## Compatibilidade e rollback

Não houve schema, migration, pacote ou formato persistido novo. Contratos
produtivos existentes conservam seus estados canônicos; negativas de rede
usam códigos sanitizados e resultam em evidência `Unknown`, nunca saúde
inventada.

O rollback técnico é a reversão focal do commit R-NET. Ele não exige rollback
de dados, mas também remove as proteções descritas e não deve ser tratado como
operação segura sem nova análise e autoridade.

## Shutdown final

Após os gates, sete nós MSBuild e um `VBCSCompiler` órfãos foram identificados
por PID, caminho sob o SDK local do workspace, linha de comando e parentage.
Os oito helpers de build foram encerrados. A verificação final observou:

- processos pertencentes ao workspace ou produto: `0`;
- listeners pertencentes ao workspace ou produto: `0`;
- janelas de produto DB-Notifier: `0`;
- janela da IDE do utilizador com o workspace no título: `1`, preservada.

Nenhum navegador comum, database engine, serviço monitorado, IDE ou processo
alheio foi encerrado.

## Estado resultante

- R-NET: automaticamente `APROVADO` somente no escopo local validado.
- `STATE`: permanece `STATE-06 INTEGRATION`.
- `ActivationState`: permanece `None`.
- Human Gate: nenhum executado ou inferido.
- Runtime/ação externa: nenhum executado.
