# DB-Notifier Threat Model

## Scope and assets

This model covers Dashboard, API, Agent, providers, monitored endpoints, OS/service adapters, local/central storage, vaults, update channel, and future AIOps inputs. It is an architecture artifact, not a penetration-test result.

Critical assets:

- Monitoring and administrative credentials.
- Agent private keys and enrollment material.
- Inventory, topology, health history, audit, and user identity.
- Administrative command authorization and results.
- Package signing identity and update manifests.
- AIOps knowledge sources, datasets, prompts, evaluations, and approvals.

## Trust boundaries

1. Browser/Desktop user ↔ API.
2. API ↔ Agent over outbound HTTPS/mTLS.
3. Agent ↔ local vault.
4. Agent ↔ provider driver/native utility.
5. Provider ↔ monitored database.
6. Agent ↔ operating-system/service manager.
7. API ↔ PostgreSQL/notification/secret manager.
8. Release pipeline ↔ signing service/update channel.
9. Sanitized telemetry/knowledge ↔ future AIOps/LLM boundary.

## Threats and required controls

| Boundary/threat | Example | Required controls | Verification |
|---|---|---|---|
| Endpoint SSRF | User config points Agent/server to metadata/internal service | provider endpoint schema, allow/deny CIDR policy, DNS/IP revalidation, no server-side probe to monitored DB | negative endpoint tests, DNS rebinding scenario |
| Native command injection | Host/service/path/extra args become shell text | typed process arguments, no shell, allowlisted executable discovery, provider-owned validation | metacharacter/path tests, static review |
| SQL injection/unsafe probe | Free-form SQL enters provider | fixed/typed provider operations, parameters, no LLM/user SQL executor | provider contract tests |
| Agent impersonation | Stolen token/certificate publishes false health | one-time enrollment, mTLS, non-exportable key, revocation, scope binding, anomaly/audit | cloned Agent/revocation tests |
| Replay/duplicate command | Captured command causes repeated restart | durable command ID/idempotency, expiry, Agent binding, terminal result reuse | duplicate/reorder E2E |
| Privilege escalation | Monitoring credential executes admin action | separate references/sessions, server RBAC, provider capability and local policy | negative credential/RBAC tests |
| Secret exfiltration | Exception/log/API returns connection string | opaque references, centralized redaction, structured logs, bounded native details | secret canary/log scan |
| UI authorization bypass | Hidden button called directly | server-side policy and target scope on every mutation | direct API negative tests |
| Stale/false healthy | Offline Agent's last sample remains green | observed/received/stale semantics, AgentOffline independent state | clock/offline tests |
| Queue exhaustion | API outage fills disk | bounded encrypted/local queue, backpressure, overflow event, reserved disk floor | outage/load tests |
| DoS/cardinality | Agent sends huge batches/tags | auth before cost, body/batch/tag limits, rate limit, quotas | fuzz/load tests |
| Cross-tenant access | ID enumeration leaks another scope | scoped queries, non-sequential public IDs, authorization filters, audit | IDOR tests |
| Audit tampering | Operator deletes evidence | append-only permissions, integrity/retention controls, separation of duty | privileged negative tests |
| Supply-chain compromise | Malicious package/update | lockfiles, dependency audit, SBOM, signed artifacts/manifests, hash verification, staged rollout | signature/tamper tests |
| Downgrade | Attacker installs vulnerable Agent | minimum version policy, signed manifest, schema/protocol compatibility | downgrade test |
| Prompt injection | Retrieved document asks LLM to execute/exfiltrate | treat content as data, sanitization, provenance, structured output, no direct executor | adversarial eval/red team |
| Data poisoning | Fake history biases recommendations | source identity/quality, immutable provenance, approval, offline governed training | poisoned-fixture eval |

## Administrative command abuse cases

- A Viewer crafts a restart request: server returns denied and creates a security audit entry without a command.
- An authorized Operator targets an out-of-scope instance: denied server-side.
- A valid command reaches a revoked Agent: API refuses delivery and Agent certificate fails authentication.
- A command expires after acknowledgement: Agent reports expired without invoking adapter.
- Adapter times out after possible side effect: result is `UnknownOutcome`; post-probe and operator review determine recovery.
- LLM recommends restart: recommendation has no command authority; deterministic risk/RBAC/approval workflow is still required.

## Privacy and data classification

| Class | Examples | Rule |
|---|---|---|
| Secret | passwords, tokens, private keys, full connection strings | never ordinary storage/log/protocol/AI |
| Restricted | host topology, usernames, audit actors, query samples | encrypt, scope, minimize, retain by policy |
| Internal | health samples, versions, tags, incidents | authorized access and retention |
| Public | published docs/release metadata | integrity/provenance still required |

Free-form query text and database data are excluded from the baseline. Any future collection requires a new data-classification decision and redaction/evaluation evidence.

## Residual risks requiring later evidence

- Choice and enterprise integration of vault/signing services.
- Real driver/native utility behavior on supported engine/platform versions.
- Certificate lifecycle under offline/expired conditions.
- WPF/Desktop local privilege boundary and service IPC design.
- Central multi-tenancy model if the product becomes shared SaaS.
- Package/update rollback across schema changes.

## Security acceptance scenarios

- Walk through standalone, on-premises, and hybrid topology.
- Exercise stolen enrollment token, cloned Agent, revoked/expired certificate, and vault outage.
- Test SSRF/DNS rebinding and process-argument metacharacters.
- Prove monitoring credentials and Viewer role cannot issue administrative operations.
- Replay/duplicate/expire/cancel a command.
- Scan logs/events/audit for secret canaries.
- Tamper with update artifact/manifest and verify rejection.
- Run AIOps prompt-injection/data-poisoning fixtures before any mode beyond `OBSERVER`.
