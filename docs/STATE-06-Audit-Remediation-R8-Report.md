# STATE-06 Audit Remediation — R8 Consolidated Re-audit Report

## Decision summary

- Authorised baseline: `6e529810aee82446c9f2863363201ac2ef25d151`
- Original audit findings reviewed: `39`
- Findings `ENCERRADO`: `35`
- Findings `CONTIDO`: `4`
- Findings `ABERTO`: `0`
- Findings `BLOQUEADO`: `0`
- Findings `NÃO TESTADO`: `0`
- Automatic R8 result: `APPROVED`
- Human acceptance of R8: `APPROVED WITH RESERVATIONS`
- Post-handoff cleanup correction: `REVALIDATED`
- Lifecycle: `STATE-06 INTEGRATION` unchanged
- MOD-12 activation: `ActivationState=None`
- O1, `OBSERVER`, operational AIOps and lifecycle transition: `NOT AUTHORISED`

R8 re-audited the original findings against the accepted R0/R0-F1, R1, R2-A, R3, R4-A, R4-B, R5, R6 and R7-A0 evidence. It made no product, source, test, workflow, dependency or configuration correction. `ENCERRADO` means that the original defect has a bounded accepted correction and current regression evidence. `CONTIDO` means that the unsafe or incomplete capability remains inaccessible in normal composition and requires a separately authorised future gate before activation.

The automatic result did not close the remediation programme or accept R8 on Bruno's behalf. Bruno subsequently
closed the separate [R8 Human Gate](STATE-06-Audit-Remediation-R8-Human-Gate-Report.md) as
`APPROVED WITH RESERVATIONS`. That decision closes only the audit remediation programme; it does not promote the
lifecycle or activate any capability.

## Authority and method

The authorised activity allowed only local, offline re-audit work, documentation and one focused evidence commit. It prohibited restore, download, external access, operational runtime, real providers or databases, CI, push, deploy, corrections, R2-B, O1, `OBSERVER`, LLM, recommendations, commands, automation and lifecycle transition.

The review:

- confirmed the exact baseline and a clean worktree;
- performed the mandatory shutdown preflight;
- inspected the original 39-finding matrix and every accepted remediation report and commit;
- re-ran proportional local build, test, coverage, architecture, frontend, WPF, E2E, security and fail-closed gates;
- used only synthetic fixtures, loopback-only test runtimes and already installed tools;
- retained historical evidence instead of rewriting it;
- distinguished current re-execution from accepted historical evidence;
- removed every owned runtime and temporary root before documentation.

## Consolidated finding matrix

| Finding | R8 classification | Current evidence | Residual or future gate |
|---|---|---|---|
| `AUD-H01` | `ENCERRADO` | The offline NuGet fixture gate accepts the legitimate empty report; R0-F1 architecture responsibility is green. | Live advisory freshness remains `NÃO TESTADA` because network access was prohibited. |
| `AUD-H02` | `ENCERRADO` | R1 durable queued acceptance, monotonic ordering, bounded retry and multiprocess fencing remain covered by current solution tests. | Visible notification delivery was not executed. |
| `AUD-H03` | `CONTIDO` | Normal command creation routes are typed `503` tombstones before persistence; command services and transports are absent from normal composition. | A complete administrative protocol belongs to separately authorised R2-B. |
| `AUD-H04` | `CONTIDO` | Poll/ack routes are unavailable, polling cannot be enabled and no normal Agent command worker is composed. | Durable operational anti-replay and audit require separately authorised R2-B. |
| `AUD-H05` | `ENCERRADO` | Independent Server and Agent non-secret validation and last-known-valid preservation remain covered. | Normal Agent Fleet stays disabled. |
| `AUD-H06` | `ENCERRADO` | Explicit provider-neutral rule-to-channel binding, provenance, event/binding idempotency and fail-closed quarantine remain covered. | External delivery stays disabled. |
| `AUD-H07` | `ENCERRADO` | Accepted R4-B PostgreSQL evidence proves atomic claim, database-clock lease, monotonic fence, reclaim, retry, ambiguity and dead-letter behaviour; current unit, integration and architecture regressions pass. | The PostgreSQL concurrency laboratory was not repeated in R8; external delivery remains disabled. |
| `AUD-H08` | `ENCERRADO` | Principal revocation commits before resumable certificate reconciliation batches. | Operational PKI remains unavailable. |
| `AUD-H09` | `ENCERRADO` | Authenticated pre-replace journal, deterministic recovery, identity revalidation and hostile path/link refusal remain covered by synthetic fault injection tests. | No user configuration was migrated. |
| `AUD-H10` | `ENCERRADO` | PATH-based `secret-tool` resolution is removed and normal Linux vault composition remains `Unavailable`. | A typed or approved absolute-path Linux vault client requires separate authority. |
| `AUD-H11` | `ENCERRADO` | CSR and issued-certificate SPKI comparison is mandatory before commit. | `UnavailableAgentCertificateIssuer` remains normal composition. |
| `AUD-H12` | `ENCERRADO` | Disabled instances are excluded from current health and presented separately in React, WPF and Tray evidence. | Physical accessibility conditions listed below remain `NÃO TESTADAS`. |
| `AUD-H13` | `CONTIDO` | R7-A0 revalidates deadline, cancellation and context revision before complete-only publication; MOD-12 is inactive with `ActivationState=None`. | Durable trust host and checkpoint continuity belong to unauthorised O1. |
| `AUD-M01` | `ENCERRADO` | All 18 solution projects have tracked lockfiles; none are missing or untracked. | Locked restore was prohibited and therefore not re-run. |
| `AUD-M02` | `ENCERRADO` | STATE-05/06 runner cleanup, correlation, deadlines and owned-profile isolation remain covered; R8 browser campaigns cleaned up. | None in the authorised local scope. |
| `AUD-M03` | `ENCERRADO` | The test-only harness uses its isolated bounded rate-limiting contract without weakening production limits. | None in the authorised local scope. |
| `AUD-M04` | `ENCERRADO` | CI workflow integration, global deadlines and concurrency-safe cleanup remain covered by architecture checks; the consolidated E2E passed locally. | Remote CI was prohibited. |
| `AUD-M05` | `ENCERRADO` | Component coverage and architecture tests pass with independently reported line and branch coverage. | Coverage is evidence, not proof of untested external integrations. |
| `AUD-M06` | `ENCERRADO` | Separate live and bounded ready endpoints, including configuration, central database and schema checks, remain covered. | No real central PostgreSQL was contacted. |
| `AUD-M07` | `ENCERRADO` | Inventoried HTTP JSON readers enforce media type, actual byte limit, depth and strict schema. | No external transport was used. |
| `AUD-M08` | `ENCERRADO` | Deadline begins before assignments; future skew is canonical and synthetic `pg_isready` process trees terminate on timeout or cancellation. | No real utility or database was executed. |
| `AUD-M09` | `ENCERRADO` | Package verification returns immutable content-addressed snapshots after exact-tree, aggregate-limit and link/path checks. | Dynamic provider loading remains disabled. |
| `AUD-M10` | `ENCERRADO` | The exact test-only sandbox durably enforces immutable `ExecutionPolicy.Never` in its isolated store. | No executor exists in normal composition. |
| `AUD-M11` | `ENCERRADO` | R7-A0 separates capability from activation, preserves probe outcomes and suppresses partial, stale, expired or cancelled publication. | Corpus authority is `DeclaredOnly`; O1 remains unauthorised. |
| `AUD-M12` | `ENCERRADO` | Accepted snapshot and evaluation instant are atomic in the presentation contract. | None in the synthetic presentation scope. |
| `AUD-M13` | `ENCERRADO` | Equivalent C# and TypeScript validation rejects controls, bidi controls, peripheral whitespace and invalid TV text. | None in the inventoried contract. |
| `AUD-M14` | `ENCERRADO` | The Design System and implementation agree that SignalR is only a test-only hint and HTTP reconciliation is authoritative. | Normal SignalR remains disabled. |
| `AUD-M15` | `ENCERRADO` | The accepted W02 sample proved coherent in-memory mark, text, count and aggregate updates while the flyout remained open. | The mechanism is exact-marker, presentation-only, test-only and disabled normally. |
| `AUD-M16` | `ENCERRADO` | The WPF reduced-motion adapter and modelled tests pass. | Physical Windows reduced motion remains `NÃO TESTADA`. |
| `AUD-M17` | `ENCERRADO` | Dashboard, TV, WPF and Tray identify UTC or the rendered local zone explicitly. | None in the authorised presentation scope. |
| `AUD-M18` | `ENCERRADO` | Demonstration trends are absent when authoritative snapshot evidence is active and the missing trend is explained. | No operational source was used. |
| `AUD-M19` | `ENCERRADO` | Unknown enum values map locally and fail closed to `Unknown` without exposing machine values. | None in the inventoried presentation contract. |
| `AUD-M20` | `ENCERRADO` | WPF navigation uses code-native canonical paths rather than textual glyphs. | None in the authorised UI scope. |
| `AUD-M21` | `ENCERRADO` | Automated and accepted human samples cover bounded positioning, reflow and scroll at available dimensions. | Physical 200%, mixed-DPI and related Windows conditions remain `NÃO TESTADAS`. |
| `AUD-M22` | `ENCERRADO` | All current third-party Action references are full 40-character SHAs with recorded official provenance. | Remote Action execution was prohibited. |
| `AUD-M23` | `CONTIDO` | Compatibility packaging refuses generation and produces no distributable artefact. | Full executable/transitive provenance is a mandatory future packaging gate. |
| `AUD-M24` | `ENCERRADO` | Canonical output omits `pgIsReady`, legacy input stays validated, notifications honour `enabled` and `RESTARTED` is bounded. | Legacy compatibility remains non-administrative. |
| `AUD-L01` | `ENCERRADO` | Route titles, accessible full identifiers and generated provider identity registries remain covered in both frontends. | None in the authorised UI scope. |
| `AUD-L02` | `ENCERRADO` | PowerShell/Pester host compatibility and documentation/anchor architecture checks pass in their declared bounds. | One environment-dependent `pg_isready` legacy case was conditionally skipped and not treated as broader proof. |

The matrix contains all 39 original findings exactly once: 35 are `ENCERRADO`, four are `CONTIDO`, and none are `ABERTO`, `BLOQUEADO` or finding-level `NÃO TESTADO`.

## Verification results

All commands ran locally without restore, download or external access.

| Gate | Result | Evidence |
|---|---|---|
| Baseline and preflight | `PASS` | Exact baseline, clean worktree and zero owned process/listener before execution. |
| Release build | `PASS` | 18-project .NET 10 solution; zero warnings and zero errors. |
| Solution tests | `PASS` | `483/483`: unit `399`, architecture `52`, WPF `10`, integration `22`. |
| Format | `PASS` | `dotnet format --verify-no-changes --no-restore`. |
| Coverage | `PASS` | Lines `81.98%`, branches `53.85%`, ten required components. |
| Dashboard | `PASS` | Node `24.18.0`, npm `11.16.0`; check/build passed; tests `67/67`. |
| Dashboard contracts | `PASS` | Brand, 11 provider identities/22 variants, localisation and generated tokens verified. |
| Consolidated E2E | `PASS` | Exact HTTPS loopback sandbox, synthetic data, isolated Chrome profile and cleanup. |
| Dashboard browser audit | `PASS` | 128 viewport, 96 forced-colours and 24 focal zoom/reflow samples. |
| WPF matrix | `PASS` | 64/64 executable route combinations passed; 32 physical `1920×1080` combinations remained `NÃO TESTADAS` because usable area was `1920×1032`. |
| PowerShell/Pester | `PASS` | 32 tests passed; one declared environment-dependent case skipped; Windows PowerShell 5.1 and PowerShell 7 hosts inventoried. |
| Offline NuGet fixture | `PASS` | All 18 projects independently evaluated; empty and adversarial report fixtures passed without feed access. |
| Lockfile structure | `PASS` | 18 projects, 18 tracked lockfiles, zero missing/untracked. |
| Supply chain | `PASS` | Two Action references, both pinned to full SHAs; zero mutable tags. |
| Secret scan | `PASS` | Current non-ignored worktree and available Git history. |
| Normal composition | `PASS` | Fail-closed audit returned live `200`, protected surfaces `426`, Agent workers disabled, command polling absent and local persistence uninitialised. |
| Packaging | `PASS` | Validation succeeded and normal generation refused fail-closed; no distributable artefact produced. |
| Git integrity | `PASS` | Object verification and remediation-range whitespace checks passed; dependency files unchanged. |
| Documentation and links | `PASS` | Documentation gate covered 329 source files; 642 local links across 144 Markdown files passed. |
| Cleanup | `PASS AFTER CORRECTION` | The first process-based check missed one retained Windows Terminal tab. Bruno closed it; the repeated check proved zero matching window, `ping.exe`, owned process/listener and temporary root, with preferences preserved byte-for-byte. |

### WPF orchestration note

An initial outer command incorrectly invoked the already complete WPF matrix more than once and reached its outer orchestration timeout during a repeated campaign. This was not a product or test failure. The owned child process completed, restored the exact preference bytes and exited. One isolated full matrix was then run successfully with 64/64 executable routes passing and 32 physically unavailable `1920×1080` combinations remaining `NÃO TESTADAS`. Every exact temporary root from both attempts was inspected and removed.

### Post-handoff Windows Terminal cleanup correction

After the first R8 report and commit, Bruno reported one visible Windows Terminal tab titled
`C:\WINDOWS\system32\ping.exe`. The full solution test
`TimeoutAndCancellationTerminateSyntheticReadinessProcessTree` uses a local
`ping.exe 127.0.0.1 -t` child to prove process-tree termination. The displayed `0x800700e8` condition was the
already documented closed-pipe race from the R5 evidence. No `ping.exe` process or external connection remained,
but the generic Windows Terminal tab survived and was not attributable through the repository-path and process
checks used by the first cleanup audit.

This means the original statement of complete cleanup was premature. Bruno closed only that tab and separately
authorised factual correction and cleanup revalidation. The repeated read-only audit then proved:

- zero `ping.exe`;
- zero visible window whose title matched `ping.exe` or `127.0.0.1`;
- zero DB-Notifier-owned process or listener;
- zero matching DB-Notifier temporary root;
- the WPF preference present at the exact expected SHA-256
  `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`;
- a clean worktree before this documentation-only correction.

No source or test correction was authorised or performed. The automatic R8 result remains `APPROVED` only after
this follow-up cleanup proof. Bruno subsequently approved the Human Gate with the report's reservations.

### Later source correction

On 2026-07-24, after the same generic Windows Terminal tab recurred during a later full unit run, Bruno explicitly
authorised its correction. The synthetic readiness fixture no longer uses PowerShell `Start-Process`, which could
delegate the console child to the configured default terminal. Parent and child are now created with
`UseShellExecute=false`, `CreateNoWindow=true`, a hidden window style and redirected streams. The timeout and caller
cancellation cases both passed while proving zero process main-window handles, zero new Windows Terminal process
and zero residual `ping.exe`. This later correction does not rewrite the original R8 incident or its cleanup
evidence.

## Preserved limitations and incidents

The following evidence is deliberately not upgraded to approval:

- R5 NuGet metadata incident: preserved unchanged. The current offline fixture gate passed, but live advisory freshness and an online restore/audit were not executed.
- Locked restore: `NÃO TESTADO` in R8 because restore and dependency resolution were expressly prohibited. Structural lockfile completeness passed.
- R6 physical conditions: High Contrast, reduced motion, Narrator, physical Windows 200% scaling and mixed-DPI movement remain individually `NÃO TESTADAS`.
- R4-B PostgreSQL laboratory: not repeated in R8 because real/provider database runtime was prohibited. The accepted bounded R4-B laboratory evidence remains the concurrency proof.
- Remote CI and Actions: `NÃO TESTADAS`; workflow structure and immutable Action references passed locally.
- Operational providers, PostgreSQL, PKI, vault, delivery, notifications and Agent Fleet: not activated or tested.
- MOD-12 trust host, durable checkpoint, real corpus/telemetry and O1: absent and unauthorised; `ActivationState=None` remains unchanged.

These limitations do not create an uncontained known high finding in the current inactive and fail-closed baseline. They constrain what may be claimed and remain mandatory gates before the related capability can be activated or supported.

## Cleanup and change boundary

- No source, test, workflow, dependency, lockfile or executable configuration was changed.
- No database, provider, delivery adapter, command surface, observer mode or operational runtime was activated.
- The exact WPF preference file remains present with SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`.
- All dedicated Chrome profiles, WPF audit roots, listeners and project-owned processes were removed. The
  initially missed generic Windows Terminal tab was closed by Bruno and its absence was then verified explicitly.
- The initial focused R8 commit and its follow-up correction contain only this report and factual governance/state
  records.

## Automatic conclusion and next decision

R8 is automatically `APPROVED` after the corrected cleanup revalidation: all 39 original findings have a bounded current disposition, the four contained capabilities remain inaccessible in normal composition, proportional gates passed, and no current finding is open or blocked. This is an automatic audit result only.

Bruno decided exactly `HUMAN GATE DO R8: APROVADO COM RESSALVAS — aceito as limitações e contenções registradas no relatório R8. Esta decisão encerra somente a remediação e não autoriza O1, OBSERVER, AIOps operacional ou transição de lifecycle.` The remediation programme is therefore humanly closed with the recorded reservations. R2-B, O1, `OBSERVER`, AIOps operation, deployment, provider homologation and lifecycle transition remain unauthorised.
