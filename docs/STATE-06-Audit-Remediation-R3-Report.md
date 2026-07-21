# STATE-06 — Audit Remediation R3 Report

## Disposition

- Increment: `R3 — Agent Fleet, identidade e ingresso non-secret`.
- Findings: `AUD-H05`, `AUD-H08`, `AUD-H11` and `AUD-M07` only.
- Authorised baseline: `ede62bb3617e106803b4109daa5f9df73de2484e`.
- Execution authority: Bruno's explicit local-only authorisation issued on 2026-07-20.
- Local implementation and R3-specific verification: completed.
- Automatic result in the authorised R3 scope: `APROVADO`.
- Independent human decision: `ACEITO COM RESSALVA`, recorded from Bruno's exact decision `ACEITO O R3 COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.` on 2026-07-20.
- Repository-wide qualification: the unfiltered .NET solution test command remains `BLOQUEADO` only by the exact pre-existing R0 architecture assertion reserved after R1 and R2-A; it was not changed under this authority.
- Lifecycle: remains `STATE-06 INTEGRATION`; no transition was requested or performed.
- MOD-12: unchanged and inactive; no `OBSERVER`, O1, LLM, recommendation, command or automation was enabled.

This report proves bounded local trust-boundary remediation. It does not activate the normal Agent Fleet, distribute an operational assignment, integrate PKI or a vault, homologate a provider, or establish production support.

## Authorised boundary

The increment permitted only local source, tests, documentation, synthetic fixtures, ephemeral SQLite, loopback runtimes and one focused commit needed to address the four named findings. It prohibited new dependencies, normal activation, real PKI, vaults, credentials, certificates, providers, databases or services, operational migration, destructive backfill, external access, downloads, remote CI, push, deploy, R2-B, R4–R8, R7-A0/O1, commands, execution, SQL/shell, Start/Stop/Restart, LLM, recommendations, automation, mode promotion and lifecycle transition.

The mandatory shutdown preflight started from the exact clean baseline and proved zero DB-Notifier-owned process, listener or visible product window. The user's IDE was identified and left untouched.

## Implemented remediation

### Independent non-secret assignment admission (`AUD-H05`)

- One provider-neutral validator checks canonical provider identifiers through the open Provider SDK registry and delegates endpoint semantics to the exact registered provider.
- Endpoint JSON permits only primitive provider fields; unknown provider fields and duplicate properties fail closed. Endpoint and arbitrarily nested tag keys are scanned under depth/node bounds for password, token, connection-string, private-key, API-key, access-key and equivalent secret-like names.
- The optional credential reference must be the exact four-field structured document: non-empty GUID reference ID, bounded stable vault-provider identifier, bounded non-secret locator and exact `Monitoring` purpose. Unknown fields and resolved values are refused.
- The Server validates every stored assignment before constructing a transmissible snapshot. The Agent validates the entire snapshot again before deleting or inserting SQLite rows.
- Agent refusal returns a sanitised failure and preserves its last-known-valid assignments and version. The normal Server uses an empty provider registry, and the normal Agent Worker still refuses Agent Fleet client activation.

### Principal-first revocation (`AUD-H08`)

- Revocation now commits the principal Agent state, original monotonic `RevokedAt`, new concurrency token and audit evidence before certificate metadata reconciliation begins.
- Certificate rows reconcile in serialisable transactions of at most 128. Existing non-revoked row state is the durable resumable cursor, so no EF migration or schema change was needed.
- A transient batch failure returns `CertificatesReconciling`/HTTP `202`; the identity remains revoked and authentication is denied. A repeated authorised request resumes batches and returns `AlreadyRevoked` only when reconciliation completes.
- Tests cover 0, 1, 128, 129 and 513 certificates, plus an injected interruption after the principal commit.

### CSR/certificate public-key binding (`AUD-H11`)

- The Server reparses the exact admitted PKCS#10 CSR and independently exports its SubjectPublicKeyInfo.
- Before enrollment commit, the CSR SPKI and issued-certificate SPKI must match through `CryptographicOperations.FixedTimeEquals`, in addition to the pre-existing digest, key algorithm, usage, validity and thumbprint checks.
- A synthetic issuer test proves a certificate with a different P-256 key is rejected even when the issuer returns internally coherent digest and certificate metadata.
- `UnavailableAgentCertificateIssuer` remains the normal composition. No real certificate authority, trust material or private key was introduced.

### Bounded HTTP JSON ingress (`AUD-M07`)

- A shared C# reader admits only exact protocol JSON media types and optional UTF-8, treats `Content-Length` only as an early rejection, streams and counts actual bytes, then applies contract-specific maximum depth and unknown-member refusal.
- The inventory covers Agent Fleet, observation batching, reconciled local notification, the uncomposed legacy command-delivery adapter and the exact test-only command sandbox. An architecture gate scans all product C# source against unbounded response convenience APIs and confines raw response streaming to the shared reader.
- Dashboard TV now streams and counts browser response bytes, rejects invalid JSON content type and malformed declared length, decodes UTF-8 fatally, and retains its exact runtime schema validator.
- C# and Dashboard tests accept exactly `N` streamed bytes and reject `N+1` without relying on `Content-Length`.

## Completion evidence

| R3 criterion | Observed evidence |
|---|---|
| Unsafe assignment content is rejected before Server transmission and Agent persistence | Provider/schema fixtures reject secret-like nested fields, unknown provider/property, malformed credential reference and non-monitoring purpose; Server returns no snapshot; Agent LKG remains unchanged |
| Revocation is independent of certificate cardinality and resumable | 0/1/128/129/513 cases pass; interruption leaves certificate metadata pending but immediately denies the presented identity; repetition completes |
| Issued identity is bound to the exact CSR key | Mismatched-SPKI issuer fixture is refused before enrollment commit despite coherent metadata |
| Every inventoried HTTP JSON reader has real limits | Shared-reader exact `N`/`N+1`, media-type, depth and unknown-schema tests; Dashboard equivalent; repository architecture inventory |
| Enrollment, renewal and revocation remain fail closed | Existing enrollment/replay/audit integration suite passes; repeated revocation preserves the original instant and completes reconciliation; renewal remains unavailable and cannot mutate identity state |
| No operational PKI, credential, provider or database is needed | Only generated synthetic keys/certificates, fixture providers, ephemeral SQLite and HTTPS loopback were exercised |

## Automatic evidence

Environment observed locally on 2026-07-20: Windows, .NET SDK `10.0.301`, Windows PowerShell `5.1 Desktop` with pinned Pester `3.4.0`, Node.js `24.18.0` and npm `11.16.0`; only already installed dependencies were used.

| Gate | Observed result |
|---|---|
| Shutdown preflight | Passed: zero project-owned process, listener or visible product window before work |
| Release build | Passed for 18 solution projects, 0 warnings and 0 errors |
| Focused R3 unit tests | Passed, 89/89 across Agent Fleet, assignment validation, revocation, SPKI and bounded HTTP readers |
| Agent Fleet integration | Passed, 13/13 over local HTTPS loopback and ephemeral SQLite |
| Full .NET solution tests | 418 passed; 1 exact pre-existing R0 architecture assertion failed, so the aggregate command remains non-green |
| Architecture suite excluding the exact baseline contradiction | Passed, 37/37, including the repository-wide response-reader inventory and normal Fleet unavailability proof |
| Coverage | Passed: 80.39% lines, 52.58% branches and 10/10 required components present |
| Dashboard | Passed: 61/61 tests, typecheck and production build |
| .NET format | Passed with no changes required |
| Legacy/Pester | Passed: 29 tests, 1 allow-listed conditional skip, 32.08% command coverage (290/904) |
| Fail-closed runtime audit | Passed: liveness 200, protected HTTP 426, composed Agent workers disabled, command polling absent and persistence not initialised |
| Toolchain and bundle | Passed |
| Code documentation | Passed for 301 comment-capable source files |
| Markdown links | Passed for 541 local links in 119 Markdown files |
| Brand, provider icons, tokens and localisation drift | Passed |
| Secrets | Passed for the current non-ignored worktree and available Git history without printing matched values |
| Final shutdown and cleanup | Passed: zero project-owned process or listener; only the user's Visual Studio Code window remained and was deliberately excluded |

The global failure remains `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources`: it expects fixed artifact filenames while the accepted R0 workflow uses bounded diagnostic directories and run-attempt-qualified names. That contradiction predates R3, is unchanged in this diff and is explicitly outside the authority.

## Schema and external-access result

No EF migration was necessary because the existing `agents` principal state and `agent_certificates` lifecycle rows already provide a durable resumable boundary. Consequently, no SQLite migration, PostgreSQL migration, operational SQL, PostgreSQL SQL generation or database application was performed. No dependency changed, no package was restored or downloaded, and online NuGet/npm advisory checks were not run because external access was prohibited.

## Preserved containment and limitations

- `UnavailableAgentCertificateIssuer` remains registered in the normal Server.
- The normal Server provider registry for Fleet distribution is empty; the ordinary Agent Worker still rejects `AgentFleetClient.Enabled=true` before initialising its store or network path.
- R2-A remains intact: normal command routes are tombstones, normal command stores/workers/transports remain uncomposed, and the exact sandbox keeps durable `ExecutionPolicy.Never`.
- This increment validates configuration evidence; it does not acknowledge or activate assignments, contact a provider, resolve a credential, probe a database, rotate a certificate or supply an operational scheduler.
- Certificate renewal/rotation remains unimplemented and unavailable in normal and sandbox composition; R3 does not claim an operational renewal protocol.
- The C# architecture inventory prevents known unbounded response APIs in product source, but no remote proxy, malformed production peer or production-scale endurance campaign was exercised.
- Remote CI, online advisory feeds, `npm audit`, PostgreSQL/provider runtime, external services and production environments were not exercised.

## Rollback and stop condition

Safe rollback keeps Agent Fleet activation disabled, the issuer unavailable and provider assignment distribution unavailable while preserving last-known-valid Agent rows. Principal or certificate revocation is monotonic and must never be rolled back into an active identity. If a future change requires a real issuer/vault/provider, a new dependency, normal activation, operational migration, schema expansion or modification of the R0/R2-A containment, stop and request separate authority.

## Human decision and next boundary

Bruno accepted R3 on 2026-07-20 with the exact reservation `ACEITO O R3 COM A RESSALVA DO GATE GLOBAL PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.` R3 and `AUD-H05`, `AUD-H08`, `AUD-H11` and `AUD-M07` are therefore accepted and closed only in this bounded local scope. The repository-wide R0 assertion remains explicit, blocking for the aggregate command and unauthorised for correction. This acceptance does not authorise R2-B, R4–R8, R7-A0/O1, normal Agent Fleet activation, real integration, AIOps promotion or lifecycle transition.
