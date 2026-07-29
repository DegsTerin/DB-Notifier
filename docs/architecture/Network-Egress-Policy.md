# Network egress policy

## Status and boundary

This document defines the local, fail-closed network destination boundary for
DB-Notifier. It applies to explicit outbound connections owned by the Agent,
Server and provider adapters in normal composition.

The policy does not authorise a runtime, provider, monitored database, IdP,
certificate infrastructure or central PostgreSQL deployment. It does not open
firewalls, create tunnels or grant a lifecycle or activation decision.

Isolated test-only loopback sandboxes and dormant transports that normal
composition does not register remain governed by their existing exact
activation and isolation barriers. They are not evidence of operational
egress. Any future normal registration of one of those transports must use
this policy and pass its own authorised gate first.

## Policy model

Trusted host composition selects one of four stable policy identifiers:

| Policy | Consumer |
|---|---|
| `agent-synchronization` | Agent observation synchronisation with its Server |
| `provider-monitoring` | provider-owned monitoring probes |
| `human-identity` | Server OIDC metadata and signing-key retrieval |
| `server-database` | Server access to its central PostgreSQL store |

Each policy is compiled once from `DBNotifier:NetworkEgress:Policies`. A
policy contains:

- one or more canonical allowed CIDRs;
- zero or more canonical denied CIDRs;
- one or more exact allowed ports;
- a DNS timeout from one to ten seconds;
- a maximum of one to 32 resolved addresses.

The complete configuration is bounded to 16 policies, 64 allowed CIDRs, 64
denied CIDRs and 64 ports per policy. Policy identifiers contain only lower
case ASCII letters, digits and hyphens and are at most 64 characters.

Mutable configuration is not an authority after startup. The runtime consumes
only the immutable compiled snapshot. Missing, empty, malformed or oversized
policy input fails closed with a stable code that contains no destination or
resolver detail.

Example structure:

```json
{
  "DBNotifier": {
    "NetworkEgress": {
      "Policies": {
        "agent-synchronization": {
          "AllowedCidrs": ["192.0.2.0/24"],
          "DeniedCidrs": [],
          "AllowedPorts": [443],
          "DnsTimeoutSeconds": 5,
          "MaximumResolvedAddresses": 16
        }
      }
    }
  }
}
```

The documentation address above is illustrative. It is not a working Server
endpoint or an operational recommendation.

## Address admission

Admission follows this order:

1. validate the exact policy identifier, host syntax and port;
2. require the port in the selected policy;
3. parse an IP literal or perform one bounded DNS resolution;
4. normalise IPv4-mapped IPv6 answers to IPv4;
5. reject an empty, null or oversized result;
6. reject the complete destination if any answer is hard-denied, denied by
   policy or absent from the positive allowlist;
7. return only the distinct approved IP addresses for direct connection.

A denial always wins over an allow. Host bits in CIDR declarations must be
zero and duplicate canonical declarations are invalid. IPv6 scope identifiers
are not accepted.

The following address classes are denied independently of configured CIDRs:

- IPv4 unspecified and `0.0.0.0/8`;
- IPv4 and IPv6 link-local;
- IPv4 and IPv6 multicast;
- IPv4 limited broadcast and reserved class-E space;
- known link-local or provider metadata endpoints covered by the
  implementation;
- IPv6 unspecified and scoped addresses.

Loopback, RFC 1918, unique-local IPv6 and other non-public ranges are usable
only when the owning policy explicitly includes them. A DNS answer set is
atomic: one prohibited answer denies all answers. The system resolver result
is evidence, never authority.

## Connection pinning

HTTP clients retain the original URI hostname for the Host header, SNI and
certificate-name validation. Their physical `ConnectCallback` resolves and
authorises every new socket, then connects directly to an approved
`IPEndPoint`. It never reconnects by hostname.

The shared handler disables:

- automatic redirects;
- ambient proxy use;
- cookies;
- ambient credentials and pre-authentication;
- automatic response decompression.

Pooling is bounded. DNS changes are re-evaluated when a new physical
connection is created; they cannot redirect an existing connection. HTTP
consumers that receive a redirect treat it as a refusal or retryable outcome
according to their owning protocol and never forward an identity or payload
to the redirected origin.

PostgreSQL adapters receive only an approved IP literal for
`pg_isready`, TCP and Npgsql connections. Npgsql retains the original hostname
only as `TargetHost` for SNI and `VerifyFull` certificate identity. The central
Server data source pins its approved address for the process lifetime;
legitimate DNS rotation therefore requires a controlled restart.

Unix-domain socket paths remain local and do not enter the CIDR policy.
Support for such a path remains limited to the owning adapter's existing
validated contract.

## Certificate validation

DB-Notifier does not allow certificate validation to create uncontrolled AIA,
CRL or OCSP traffic.

Outbound TLS and configured inbound Agent-certificate validation use:

- operating-system trust unless a narrower owning trust contract is
  configured;
- the exact server-authentication or client-authentication application OID;
- `X509RevocationMode.Offline`;
- `X509VerificationFlags.NoFlag`;
- certificate downloads disabled.

Offline revocation is deliberately fail-closed when the operating system
cannot prove status from local material. Provisioning trust anchors and
revocation material is an operational responsibility outside this local lot.
The implementation never replaces this posture with `NoCheck`.

## Startup and consumer behaviour

- Agent monitoring and synchronisation remain disabled by default.
- Enabling synchronisation requires an `agent-synchronization` policy that
  admits the configured Server port.
- Enabling monitoring requires a compiled `provider-monitoring` policy.
- Configuring an OIDC authority requires a `human-identity` policy and the
  safe backchannel.
- Configuring central PostgreSQL requires a `server-database` policy and a
  connection shape accepted by the pinned data-source factory.
- Leaving a feature disabled or its external configuration absent performs no
  DNS lookup or connection.

The PowerShell compatibility shim is narrower: it admits only the exact
`localhost` alias or a loopback IP literal, converts the result to a pinned
literal and performs no DNS resolution for a remote name. Remote legacy
monitoring is therefore refused before `pg_isready` or TCP execution.

## Stable failures and evidence

Network admission returns stable sanitised codes:

- `network.policy_unavailable`;
- `network.destination_invalid`;
- `network.port_denied`;
- `network.dns_timeout`;
- `network.dns_failed`;
- `network.dns_answer_limit`;
- `network.address_denied`;
- `network.connect_failed`.

These codes describe the failed boundary without disclosing the hostname, IP,
resolver exception, connection string or secret. Transport reachability
remains transport evidence only and never proves database authentication or
health.

## Validation boundary

Local deterministic tests prove parsing, allow/deny precedence, mixed DNS
refusal, rebinding resistance, redirect refusal, pinned socket arguments and
TLS option construction. The separately authorised
[controlled local R-NET campaign](../STATE-06-R-NET-Local-DNS-PKI-IdP-PostgreSql-TLS-Homologation-Report.md)
also proves:

- the Windows system resolver for `localhost`, plus controlled mixed-answer
  refusal and fresh admission after an answer change;
- actual local HTTPS discovery, JWKS retrieval and JWT validation, including
  redirect, cross-origin, issuer, audience and signing-key refusal;
- the operating-system chain engine with exact process-local custom-root
  trust, a temporary exact `CurrentUser\CA` CRL and valid, wrong-name,
  wrong-EKU, revoked, untrusted and missing-CRL cases;
- central and provider paths against sequential disposable PostgreSQL 16 TLS
  cells, including `VerifyFull`, `pg_stat_ssl`, authentication failure,
  certificate failure and pre-transport policy denial. Each cell had no
  default route, used an exact loopback-only effective resolver, ran
  PostgreSQL as non-root with zero active capabilities and `NoNewPrivs=1`,
  and exposed an effective Windows listener only on IPv4 loopback.

Production constructors still use system trust; no custom root or trust
override became runtime configuration. The campaign does not homologate:

- non-loopback or operational DNS, DNSSEC, corporate resolver/cache behaviour
  or temporal rebinding outside the fixture;
- operating-system Root or enterprise-trust provisioning, operational
  CA/CRL/OCSP rotation or externally managed PKI;
- a corporate IdP;
- an operational or provider-homologated PostgreSQL topology;
- proxy, failover, high availability, cross-platform behaviour, production
  availability or performance.

Those stronger claims require separately authorised operational evidence.

## JOSE-0 candidate egress map

The following entries are documentary candidates from `JOSE-0`; they are not
registered policies, configured consumers, endpoints or implementation
authority. Normal composition still recognises only the four policies listed
in [Policy model](#policy-model). No operational or candidate IdP, STS,
vault, KMS, HSM, credential, key or integration service was selected or
contacted. The authorised read-only capture of public IANA/RFC documentary
sources was not a normal-composition identity/custody flow and registered no
egress consumer.

| Candidate consumer | Candidate owner and purpose | Preconfigured target authority | Permitted data shape | Denied behaviour | Decision dependency |
|---|---|---|---|---|---|
| `human-identity-backchannel` | Server Identity/BFF owner; future token exchange, introspection or revocation only if a browser/session design selects them | administratively fixed IdP and exact operation origins admitted through a separately configured pinned HTTPS connector | bounded protocol-specific request/response with exact credential, audience, scope, timeout and retry rules | token/caller URL, redirects, cross-origin escape, proxy, ambient credentials, cookies, decompression or reuse as discovery/JWKS authority | separate candidate; current `human-identity` remains unchanged for discovery and public JWKS |
| `server-key-custody` | Security/platform owner; an artefact-specific private sign, unwrap or decrypt operation through an opaque key reference | one separately homologated custodian adapter, operation endpoint and private network policy per environment | bounded digest/envelope and operation metadata; bounded result with no exportable private or symmetric key | generic cryptographic oracle, caller-selected endpoint/key/profile, key export, ambient SDK transport, fallback endpoint, IMDS and telemetry escape | requires accepted ADR-0008, an authorised use case, `JOSE-3B`, R-NET review and custodian homologation |
| `server-workload-identity` | Platform owner; acquire a narrowly scoped workload credential only when the selected custodian topology requires it | one administratively configured STS/workload-identity endpoint and exact audience/scope | bounded protocol request/response under the owning identity profile | default credential chain, ambient cloud identity, caller-selected scope/audience, cross-environment token and reuse as human or Agent identity | optional; requires a separate identity/R-NET decision and is absent when the custodian does not need it |

These candidates are separate because public identity trust, private key
operations and workload credential bootstrap have different data, scopes,
failure modes and accountable owners. They must not share credentials or
silently reuse the current `human-identity` policy.

Runtime registry refresh from IANA is prohibited. Registry snapshots are
controlled documentary inputs, not an application egress flow. Token
headers, claims, payloads, JWK members, exceptions and custodian responses
cannot introduce a host, scheme, port, proxy, failover target or credential
source.

The complete provisional ownership/trust/data map and numeric response/cache
bounds are in
[JOSE Security Profile and Key Lifecycle](JOSE-Security-Profile-And-Key-Lifecycle.md).
Any future promotion of a candidate requires its own authorised change,
configuration schema, compatibility plan, negative zero-hit evidence and
exact topology homologation. `JOSE-0` grants none of them.
