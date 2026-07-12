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
| Validate non-secret endpoint config | Partial legacy normalization | Partial legacy normalization | Planned typed validation | None |
| Discover Windows PostgreSQL services | Legacy observed via CIM/registry | N/A | Planned Windows discovery adapter | None |
| Provider readiness via `pg_isready` | Legacy observed | Legacy observed | Planned | None |
| TCP reachability fallback | Legacy observed; must be `Degraded` | Legacy observed; must be `Degraded` | Planned transport evidence only | None |
| Authenticated health probe | Not implemented | Not implemented | Planned with monitoring credential | None |
| Authentication failure classification | Not reliable | Not reliable | Planned | None |
| Latency measurement | Process duration not canonicalized | Process duration not canonicalized | Planned | None |
| Version discovery | Not implemented | Not implemented | Planned | None |
| Core metrics | Not implemented | Not implemented | Planned incrementally | None |
| Event/history persistence | Not implemented | Not implemented | Planned via canonical events | None |
| Start/Stop/Restart Windows service | Legacy observed in code; not exercised in discovery | Unsupported | Planned separate admin adapter | None |
| SQL/native administrative operations | Unsupported | Unsupported | Unsupported until separate capability ADR/evidence | None |
| Offline Agent outbox | Not implemented | Not implemented | Planned | None |

## Administrative platform matrix

| Adapter | Local Windows | Remote Windows | Linux/systemd | Cloud-managed database |
|---|---|---|---|---|
| Windows service control | Planned for explicitly discovered local service | Unsupported without an Agent on that host | N/A | N/A |
| systemd service control | N/A | N/A | Not evaluated/planned later | N/A |
| Vendor/cloud API | N/A | N/A | N/A | Not evaluated; requires provider-specific identity and ADR |
| SQL administrative command | Capability-specific only | Capability-specific only | Capability-specific only | Capability-specific only |

No row implies universal control. UI/API must display `Unsupported`, `Unavailable`, `Unknown`, and `Denied` separately.

## Other engines

| Provider | Architecture registration | Implementation | Homologation | Public support claim |
|---|---|---|---|---|
| MySQL/MariaDB | Roadmap | Not implemented | None | No |
| SQL Server | Roadmap | Not implemented | None | No |
| Oracle | Roadmap | Not implemented | None | No |
| MongoDB | Roadmap | Not implemented | None | No |

An engine moves from roadmap to implementation only with provider contract fixtures, license/environment availability, error/capability mapping, security review, and its own homologation matrix.

## Required PostgreSQL fixtures

- Valid readiness, startup/recovery/rejection, unreachable host, refused port, timeout, DNS failure, and missing utility.
- Valid/invalid/expired monitoring credential for authenticated probe.
- Local service missing/stopped/running/PID changed/access denied.
- Special-character paths/arguments without shell interpretation.
- Remote endpoint with administrative control unavailable.
- Provider/Agent version incompatibility and missing capability.
- Post-command probe success, failure, timeout, and ambiguous outcome.
