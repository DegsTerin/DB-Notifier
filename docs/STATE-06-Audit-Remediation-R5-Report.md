# STATE-06 — Audit Remediation R5 Report

- Date: 2026-07-21
- Baseline: `21dd72dbf5858001a2301f03adf4aa7a82d75798`
- Lifecycle: `STATE-06 INTEGRATION`, unchanged
- Authorised findings: `AUD-H09`, `AUD-H10`, `AUD-M09`, `AUD-M22`, `AUD-M23`, `AUD-M24`
- Technical automatic result: `APPROVED`
- Original-authority compliance: `FAILED — RECORDED NUGET METADATA ACCESS INCIDENT`
- Post-incident conclusion authority: `GRANTED`, exactly and separately on 2026-07-21
- Human decision: `ACCEPTED WITH RECORDED RESERVATIONS`, exactly on 2026-07-21

## Authority and preflight

This local increment used only the authority explicitly granted for R5. The initial repository state was branch `main`, exact authorised baseline and a clean worktree. The mandatory shutdown preflight found zero DB-Notifier-owned process and zero owned listener. No database, service, user configuration, real secret, Secret Service, provider package, loader, installer, release artefact, remote CI, push, deployment, AIOps mode or lifecycle transition was used or enabled.

The intended external boundary was read-only provenance lookup against the four official GitHub repositories already named by the workflow. It used no credential, clone, package or artefact download. During final validation, however, `scripts/verify-dotnet-lockfiles.ps1` was called inadvertently; that script internally executed `dotnet restore --locked-mode`, contrary to the R5 no-restore boundary. The repository and package cache remained unchanged, but the NuGet v3 HTTP cache recorded refreshed `service_index` and vulnerability metadata from `api.nuget.org`. This unauthorised external metadata access initially blocked R5 completion even though it introduced no dependency or package binary; the separate post-incident authority and outcome are recorded below without rewriting the failure.

## Finding disposition

| Finding | Implemented disposition | Proof boundary | Residual |
|---|---|---|---|
| `AUD-H09` | ConfigMigrator creates authenticated backups and pending artefacts, then a durable `Prepared` journal before target replacement. Recovery inspects authenticated old/new state and deterministically aborts old or completes new. Rollback changes the same journal to `RollingBack`, authenticates and revalidates target, backups, manifest and report, and resumes deterministically after interruption. | Synthetic temporary directories, fault injection at four apply and four rollback boundaries, previous-target recovery, hash/identity/concurrency/hardlink tests. | No user configuration or operational migration was touched. macOS is not a supported secure file-identity platform for this implementation; Windows and Linux are explicit. |
| `AUD-H10` | Removed the `secret-tool` process and PATH resolution completely. The reserved Linux adapter now always returns sanitised `CredentialUnavailable`. | Unit test proves normal resolution is unavailable and no hidden `ProcessStartInfo` factory remains. | A typed or separately approved absolute-path Linux adapter remains future work; no real vault was exercised. |
| `AUD-M09` | Provider verification now requires a canonical exact file/directory tree, portable case uniqueness, single-link ordinary files, per-file and aggregate byte limits, stable identities and hashes. It returns a content-addressed immutable in-memory snapshot, never the mutable source path. | Signed synthetic packages cover tamper, extra files, case collision, hardlinks, source mutation after verification and discovery isolation. | Dynamic loading remains absent and disabled; no provider was loaded or activated. |
| `AUD-M22` | Every `actions/*` use in `ci.yml` is pinned to a full commit proven from the official repository's `refs/tags/v4`. | Local Pester assertion rejects mutable major tags and requires forty lowercase hexadecimal characters. Provenance commands and results are recorded below. | Remote Actions execution was prohibited and was not performed. Future upstream ref movement does not rewrite the recorded observation. |
| `AUD-M23` | Compatibility packaging uses manifest schema v2 and requires complete executable/transitive closure with version, SHA-256, role and official repository/commit provenance. R5 still unconditionally refuses generation. | PowerShell 7 and Windows PowerShell 5.1 validate-only runs; Pester proves the checked-in manifest is blocked and no `Invoke-PS2EXE` call remains. | No installer, executable or distributable package was generated. Enabling generation requires separate authority and implementation. |
| `AUD-M24` | Canonical and sample configuration omit `pgIsReady`; the migrator still validates legacy input but does not emit the legacy field. Global `notifications.enabled` gates the startup balloon. Authenticated `RESTARTED` survives only until its bounded deadline; TCP-only evidence cannot create it. | .NET migration tests and Pester compatibility tests on the pinned host. | The explicit legacy reader keeps compatible input handling; no service-control authority was added. |

## ConfigMigrator recovery contract

The journal names every artefact within the exact target directory and records SHA-256 for source, new target, previous target, source backup, previous-target backup, manifest and report. All ordinary-file reads use exclusive handles, byte limits, link-count checks and stable filesystem identity. Parent reparse points, symlinks, junctions, unsafe hardlinks, path aliases, traversal, case-equivalent duplicates and concurrent changes fail closed.

Commit order is: authenticated backups and pending files; durable `Prepared` journal; immediate source/target revalidation; atomic target promotion; report promotion; manifest promotion; journal transition to `Completed`. Recovery treats an authenticated old target as an aborted migration and an authenticated new target as a commit to complete. Any third state is refused. Dry-run remains the default and creates none of these files.

The injected interruption matrix covers journal creation, target commit, report commit and manifest commit for both apply and rollback. Every next start produced a complete authenticated old or new state, including restoration of a previous target. The concurrency fixture changed the target after journal creation and was refused before commit. A Windows hardlink alias was also refused.

## Official GitHub Actions provenance

The following command shape was executed once per repository:

```text
git ls-remote --refs https://github.com/actions/<repository>.git refs/tags/v4
```

Observed official mappings on 2026-07-21:

| Official repository | Ref | Observed commit pinned in CI |
|---|---|---|
| `github.com/actions/checkout` | `refs/tags/v4` | `11d5960a326750d5838078e36cf38b85af677262` |
| `github.com/actions/setup-dotnet` | `refs/tags/v4` | `67a3573c9a986a3f9c594539f4ab511d57bb3ce9` |
| `github.com/actions/setup-node` | `refs/tags/v4` | `49933ea5288caeca8642d1e84afbd3f7d6820020` |
| `github.com/actions/upload-artifact` | `refs/tags/v4` | `ea165f8d65b6e75b540449e92b4886f43607fa02` |

No SHA was inferred. No mirror, community source, authenticated API, checkout, clone, download or Action execution was used.

## Containments preserved

- R2-A command tombstones, `ExecutionPolicy.Never` and normal command-service removal are unchanged.
- R3 Agent Fleet remains disabled and `UnavailableAgentCertificateIssuer` remains in normal composition.
- R4-A external delivery startup refusal remains; R4-B ownership/durability is not activated.
- `UnavailableServerMessagePublisher` remains normal and no real channel adapter exists.
- Linux Secret Service, dynamic provider loading and all compatibility packaging generation are unavailable.
- The known global R0 architecture assertion remains unchanged and is not corrected by this lot.

## Validation record

Observed local evidence before the boundary incident:

- ConfigMigrator Release build: passed with zero warnings/errors.
- Infrastructure Release build: passed with zero warnings/errors.
- R5-focused .NET tests: 29 passed, zero failed/skipped after the final rollback-recovery addition.
- Complete unit suite at the current build: 380 passed, zero failed/skipped.
- Windows PowerShell 5.1/Pester 3.4.0: 32 passed, one authorised environment-dependent skip, 32.38% command coverage (`295/911`).
- PowerShell 7 and Windows PowerShell 5.1 compatibility bundle validation: passed; no package generated.
- Code-documentation gate: 312 files passed.

Additional observed gates:

- Solution Release build: 18 projects, zero warnings/errors.
- Consolidated tests: 380/380 unit, 22/22 integration and 3/3 WPF passed; architecture was 42/43 with only the accepted pre-existing R0 filename assertion failing.
- Architecture excluding the single R0 assertion: 42/42 passed.
- Coverage: 81.88% lines, 52.98% branches, all ten required components present.
- `dotnet format --verify-no-changes --no-restore`: passed.
- Fail-closed runtime audit: passed and cleaned its loopback processes.
- NuGet report-structure gate using the checked-in fixture: passed for 18 projects without live registry evidence.
- Markdown links: 560 links in 122 files passed; secret scan passed.

Boundary incident evidence:

- `scripts/verify-dotnet-lockfiles.ps1` executed a locked restore despite the no-restore instruction.
- `git status` showed no lockfile or generated repository change.
- zero recently written file was found beneath the global NuGet package cache.
- four recently written files were found beneath the NuGet v3 HTTP cache: the service index and vulnerability base/update/index metadata for `api.nuget.org`.
- no attempt was made to delete or rewrite the user's cache, and the gate will not be repeated in this increment.

Because the external-access prohibition was breached, the original execution cannot be reclassified as fully compliant. Bruno subsequently authorised exactly the offline conclusion, final documentation and one focused commit after acknowledging the recorded incident, while prohibiting any further restore, download or external access. That later authority permits the technical result to close as `APPROVED`; it does not erase or retroactively authorise the incident.

The post-incident offline revalidation observed:

- mandatory preflight: zero DB-Notifier runtime/listener, zero `ping.exe`; the user's Visual Studio Code window was identified and preserved;
- Release build with `--no-restore`: 18 projects, zero warnings/errors;
- R5-focused unit tests with `--no-build --no-restore`: 29/29 passed;
- architecture excluding only the recorded R0 contradiction: 42/42 passed;
- bundle validation passed on PowerShell 7.6.3 and Windows PowerShell 5.1 without generation;
- Pester 3.4.0 on Windows PowerShell 5.1: 32 passed, one authorised environment-dependent skip, 32.38% coverage (`295/911`);
- focused .NET coverage: ConfigMigrator 87.43% lines/68.13% branches; package-verifier classes exercised between 66.66% and 100% lines, with the primary verifier at 74.73% lines/54.23% branches; Linux unavailable adapter 71.42% lines/50% branches;
- format with `--no-restore`, documentation, 559 Markdown links, secrets, diff check and Git object integrity passed;
- no restore, external access, download, package generation or full-suite rerun occurred during this post-incident validation.

The earlier full unit run opened two visible Windows Terminal tabs while the pre-existing synthetic PostgreSQL process-tree test raced `ping.exe` termination. Error `0x800700e8` was the expected closed-pipe condition; zero `ping.exe` and zero project runtime remained. The user closed the tabs. The test was not changed because it predates and lies outside R5, and the focused revalidation deliberately avoided reopening it.

## Decision boundary

The technical implementation and offline revalidation are complete under the separate post-incident authority. Bruno subsequently closed the independent human review with the exact decision `ACEITO O R5 COM AS RESSALVAS DO INCIDENTE DE METADADOS NUGET REGISTRADO E DO GATE GLOBAL R0 PREEXISTENTE, SEM AUTORIZAR CORREÇÃO FORA DO ESCOPO.` R5 and its six authorised findings are therefore accepted and closed only within the bounded local scope documented by this report.

The acceptance preserves both reservations without reclassification: original-authority compliance remains failed because of the recorded NuGet metadata incident, and the pre-existing global R0 gate remains non-green and unaltered. This decision does not constitute lifecycle progression, release, operational support or authorisation for R6–R8, R7-A0, O1, LLMs, recommendations, commands, automation, packaging, loader activation, a real vault or mode promotion.
