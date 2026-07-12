# Provider Capability Matrix

## Status vocabulary

- `Legacy observed`: present in the PowerShell compatibility implementation; not DB-Notifier provider support.
- `Planned`: architecture/contract defined but not implemented.
- `Unsupported`: deliberate no-attempt result for the stated scope.
- `Not evaluated`: no implementation or homologation evidence.
- `Homologated`: reserved for `STATE-07` evidence; none exists today.

## PostgreSQL baseline

| Capability | Local Windows legacy | Remote legacy | Target PostgreSQL provider | Homologation |
|---|---|---|---|---|
| Validate non-secret endpoint config | Partial legacy normalization | Partial legacy normalization | Implemented first increment; unit-tested | None |
| Discover Windows PostgreSQL services | Legacy observed via CIM/registry | N/A | Planned Windows discovery adapter | None |
| Provider readiness via `pg_isready` | Legacy observed | Legacy observed | Implemented adapter; no live DB evidence | None |
| TCP reachability fallback | Legacy observed; must be `Degraded` | Legacy observed; must be `Degraded` | Implemented; transport-only maps to `Degraded` | None |
| Authenticated health probe | Not implemented | Not implemented | Implemented Npgsql/TLS adapter; fake-only evidence | None |
| Authentication failure classification | Not reliable | Not reliable | Implemented canonical mapping; fake-only evidence | None |
| Latency measurement | Process duration not canonicalized | Process duration not canonicalized | Implemented for readiness duration | None |
| Version discovery | Not implemented | Not implemented | Planned | None |
| Core metrics | Not implemented | Not implemented | Planned incrementally | None |
| Event/history persistence | Not implemented | Not implemented | Implemented for canonical health transitions and central event/outbox persistence | None |
| Start/Stop/Restart Windows service | Legacy observed in code; not exercised in discovery | Unsupported | Planned separate admin adapter | None |
| SQL/native administrative operations | Unsupported | Unsupported | Unsupported until separate capability ADR/evidence | None |
| Offline Agent outbox | Not implemented | Not implemented | Implemented: transactional sink, ordered dispatch/ack, tombstones and bounded retry | None |

## Administrative platform matrix

| Adapter | Local Windows | Remote Windows | Linux/systemd | Cloud-managed database |
|---|---|---|---|---|
| Windows service control | Planned for explicitly discovered local service | Unsupported without an Agent on that host | N/A | N/A |
| systemd service control | N/A | N/A | Not evaluated/planned later | N/A |
| Vendor/cloud API | N/A | N/A | N/A | Not evaluated; requires provider-specific identity and ADR |
| SQL administrative command | Capability-specific only | Capability-specific only | Capability-specific only | Capability-specific only |

No row implies universal control. UI/API must display `Unsupported`, `Unavailable`, `Unknown`, and `Denied` separately.

## Monitoring connectivity matrix

| Database location | Agent placement | Connection mechanism | Credential/reference |
|---|---|---|---|
| Local Windows | Same Windows host or approved nearby Agent | driver/protocol, local socket where supported, or native provider utility | monitoring credential reference; optional separate Windows service identity |
| Remote from Windows | Windows Agent with authorized network path | provider protocol/driver over TLS where supported | monitoring credential reference |
| Local Linux | Same Linux host, container, or approved nearby Agent | driver/protocol, Unix socket, or native provider utility | monitoring credential reference; optional separate systemd/workload identity |
| Remote from Linux | Linux/container Agent with authorized network path | provider protocol/driver over TLS where supported | monitoring credential reference |
| Private datacenter/hybrid | Agent inside the authorized network segment | LAN, VPN, private link, approved proxy/tunnel | vault-resolved monitoring reference |
| Cloud-managed | Agent/workload with private or policy-approved endpoint access | engine protocol and, when required, separate vendor/cloud API | workload/federated identity or cloud-vault reference; distinct control-plane identity |

The central API and Dashboard never become a generic database proxy. The Agent performs provider access, and the product does not open firewalls, create public endpoints, or weaken TLS automatically.

## Open provider catalog

The product objective is to accept any database engine through a versioned provider/plugin. The catalog is deliberately open: adding an engine must not require an engine conditional or closed enum in Domain/Application.

| Priority wave | Providers | Architecture registration | Implementation | Homologation | Public support claim |
|---|---|---|---|---|---|
| First vertical slice | PostgreSQL | Accepted target | Implemented backend slice: endpoint, typed discovery, readiness/TCP, authenticated health and canonical mapping | None | No |
| Mainstream relational/document | MySQL/MariaDB, SQL Server/Azure SQL, Oracle, MongoDB | Priority roadmap | Not implemented | None | No |
| Enterprise/embedded relational | SAP HANA, SQLite, IBM Db2, Firebird, CockroachDB | Priority roadmap | Not implemented | None | No |
| Distributed/data platforms | Cassandra, ScyllaDB, Redis/Valkey, Couchbase/CouchDB | Open roadmap | Not implemented | None | No |
| Search/time-series/graph | Elasticsearch/OpenSearch, InfluxDB, Neo4j | Open roadmap | Not implemented | None | No |
| Managed/cloud variants | AWS, Azure, Google Cloud, Oracle Cloud, and future vendor services | Engine-compatible or dedicated cloud adapter | Not implemented | None | No |
| Any other engine/fork | Versioned third-party or first-party plugin | Open extension point | Not implemented | None | No |

SQLite internal Agent persistence is separate from a future SQLite monitoring provider and is not evidence of SQLite monitoring support.

An engine moves from roadmap to implementation only with provider contract fixtures, license/environment availability, error/capability mapping, security review, and its own homologation matrix. Homologation is scoped to an engine/version/platform/capability combination; universal catalog acceptance never implies universal administrative control.

## Required PostgreSQL fixtures

- Readiness accepting/rejecting/no-response/timeout/invalid and TCP reachable/unreachable mappings: implemented as deterministic provider fixtures.
- Missing utility with typed TCP fallback plus DNS/refused/timeout/reachable transport outcomes: implemented through discovery/executor fixtures without external network dependency.
- Valid/invalid/expired monitoring credential: implemented through authenticated outcome fixtures and pre-provider expiry enforcement.
- Local service missing/stopped/running/PID changed/access denied: explicitly `Unsupported` in this backend slice; capability-negative tests prove no adapter or attempt is selected.
- Special-character paths/arguments without shell interpretation: implemented with `ProcessStartInfo.ArgumentList` fixture.
- Remote endpoint with administrative control unavailable: implemented through `Unsupported` control capabilities and command-creation denial.
- Provider/Agent version incompatibility and missing capability: implemented in command delivery/authorization tests.
- Post-command probe success/failure/timeout/ambiguous outcome: not applicable while control capabilities are `Unsupported`; execution and `UnknownOutcome` E2E remain gated to authorized integration/homologation.
