# STATE-06 MySQL Notifier Authority Revocation Report

## Control

- Date: 2026-08-28
- Plan: `GOV-MN-REV-01`
- Baseline: `main@0c1da9d9cc11fa0a0678ce1435660e1a833364e1`
- Lifecycle: `STATE-06 INTEGRATION`; unchanged
- Authority: the owner instructed the coordinating conversation to read,
  analyse and verify `mysql-notifier-1.1.8-src`, then revoke every decision or
  permission to incorporate, recreate or use the complete MySQL Notifier 1.1.8
  feature set in DB-Notifier
- Disposition: prospective authority revoked; historical facts preserved

## Audit boundary

The canonical shutdown preflight passed before inspection with zero matching
DB-Notifier processes and zero owned listeners. The target resolved to an
ordinary local directory beneath the repository root. It contained no nested
`AGENTS.md` or override, and the root instructions therefore remained
applicable.

The audit was static and read-only. It did not build, run, load or decompile an
executable or library; render or reuse an asset; contact Oracle, MySQL or any
other network service; validate a credential; mutate the target; or place any
target content in the Git inventory. Source text was inspected only to classify
provenance, architecture, functional families and risk. No source extract is
reproduced in this report.

## Sanitised inventory and integrity

The observed local tree contained:

- 110 files in 10 directories, totalling 6,554,849 bytes;
- 92 files under `Source`, 11 under `Installer` and seven at the root;
- 39 C# files, 30 PNG files, nine RESX files, five DLLs, five ICO files,
  four BMP files, three GIF files, three WiX source files, two solution files,
  one C# project and one WiX project;
- approximately 14,952 C# lines, including generated designer/resource code;
- no observed test project, modern dependency lockfile or self-contained build
  manifest.

The deterministic tree identity is
`6f7c58b1c36c91dd2c3d7406aba31eb3d8213e6e737dce23be6fc0eaa1b4c676`.
It is the SHA-256 of LF-joined, path-sorted
`<relative-path>|<length>|<file-sha256>` records for every observed file. Key
local identities were:

| Artefact | Bytes | SHA-256 |
|---|---:|---|
| `LICENSE` | 86,316 | `40fd96fc2a411e28217739074938c53ec6b67f213e735906a07ade2253ac20a9` |
| `README` | 929 | `d3b8a7a704d62fda0c48a52a3f3acf3b7316ee13a1f8f56a5db6310a4e9d5975` |
| `README.md` | 2,729 | `03af7f544673be90aa71269744ed37a658114aacc7b898e99f615a6d9b0af474` |
| `MySQLNotifier.sln` | 1,728 | `43f720215ffb2d3850e77b2093f76d70f68a4b96f33420b67936a82d29f4fc59` |
| `Installer.sln` | 1,975 | `adf17138856a582f5a6b52bb121e9100ae22207b63b31897997618ff91e89958` |
| `Source/MySql.Notifier.csproj` | 16,733 | `8260f78c207ce3202b816b31a01ce65122f00f3b6179aeba3c048c75444e4850` |

Git confirmed that `.gitignore` excludes `mysql-notifier-*-src/` and that no
file beneath the target is tracked. The audit did not establish upstream
authenticity, signing, original publication timestamps or chain of custody;
the digest identifies only the local tree observed in this execution.

## Provenance and licensing observed locally

The local licensing manual identifies MySQL Notifier 1.1 and later as an
Oracle/MySQL distribution under GPLv2 with an additional linking permission.
It also carries third-party notices. The readme and assembly metadata identify
Oracle/MySQL Notifier 1.1.8 and copyright years extending through 2019.

Five compiled libraries are present, including MySQL, Bouncy Castle and
SSH.NET-related components. The C# project targets .NET Framework 4.5.2 and
declares nominal MySQL library versions that do not match all local binary
metadata observed by the audit. This report records those local facts only; it
does not provide legal advice, declare licence compatibility or prove binary
provenance.

## Functional and architectural families observed

Static documentation, manifests, resource names and source structure expose
the following broad families. They were not runtime-tested:

1. a single-instance WinForms notification-area application with menus,
   balloon notifications and Windows-startup integration;
2. local and remote Windows-service discovery, status watching and
   Start/Stop/Restart control, including WMI-oriented paths and name filters;
3. MySQL connection monitoring, TLS/authentication handling and SSH-tunnel
   support;
4. shared connection and launch integration with MySQL Workbench;
5. update and launch integration with MySQL Installer, scheduled tasks and
   elevated installer operations;
6. preferences, XML/configuration migration, logging and credential-processing
   paths;
7. WiX per-machine packaging, registry startup, shortcuts and process/task
   cleanup;
8. Oracle/MySQL-branded logos, icons, splash/status imagery and installer
   artwork.

The implementation is a vendor- and Windows-specific .NET Framework/WinForms
composition with service, WMI, registry, native API, MySQL, Workbench and
Installer concerns within one product boundary. That is not the DB-Notifier
.NET 10/WPF, provider-neutral, inward-dependency and Agent-mediated
architecture. Several build inputs referenced by the installer were not
present, and no reproducible build or test claim can be made from this tree.

## Findings and disposition

| ID | Severity | Finding | Disposition |
|---|---|---|---|
| `GOV-MN-001` | `P1` | Current DB-Notifier authority still converted MySQL Notifier behaviour into requirements and retained a comprehensive parity backlog. | Resolved prospectively by this revocation and its governing-document updates. Historical records remain evidence only. |
| `GOV-MN-002` | `P1` | The target combines GPLv2 material, third-party binaries, Oracle/MySQL branding and privileged Windows/MySQL mechanisms that cannot be treated as an MIT DB-Notifier implementation blueprint. | Contained: the target and all MySQL Notifier material are prohibited inputs to DB-Notifier requirements, design, code, tests, assets and implementation. No content was copied or executed. |
| `GOV-MN-003` | `P2` | Upstream authenticity, binary provenance, signing and reproducible build identity are unproved; local dependency metadata is partly divergent. | Residual and non-blocking for revocation because no reuse or execution is authorised. No positive integrity claim is made. |
| `GOV-MN-004` | `P2` | The ignored external tree remains physically present in the workspace. | Preserved unmodified because deletion or relocation was not requested. Existing ignore and inventory protections remain mandatory. |

## Revoked authority

Effective with `GOV-MN-REV-01`:

- the 2026-07-14 clean-room inspiration decision is superseded prospectively;
- the prohibition applies to every individual MySQL Notifier functionality,
  every combination and the purported complete feature set;
- the MySQL Notifier inspiration clause in `REQ-047` is revoked while its
  independently established DB-Notifier notification-area behaviour remains;
- `REQ-048` and `REQ-050` are revoked as current or future requirements;
- `MN-001`–`MN-025` and `MN-Q01`–`MN-Q04` remain historical traceability only;
  none of their former dispositions, phases or exit conditions creates work;
- `S06-DFR-01` and `S06-DFR-02` remain factual completed increments but cannot
  be reused as authority for MySQL Notifier parity or further derivation;
- MySQL Notifier source, binaries, assets, product text, public behavioural
  documentation, internal architecture and feature catalogue cannot inform
  DB-Notifier requirements, design, code, tests, assets, acceptance criteria,
  roadmap items or implementation guidance;
- no future request for MySQL Notifier parity or comprehensive feature coverage
  may be executed without a new explicit owner decision that first supersedes
  this revocation and satisfies all independent legal, security, architecture
  and lifecycle gates.

## Preserved boundaries and non-effects

The revocation does not rewrite a Human Gate, ADR, completed report, prior log
entry, Git commit or observed implementation result. Existing DB-Notifier-owned
notification-area behaviour, provider-neutral status semantics, accessibility,
security restrictions and Agent/API boundaries continue on their independent
authority and evidence. Removing those product behaviours would require a
separate technical change.

MySQL as a database engine remains an independently governed candidate in the
open provider catalogue. This report revokes the MySQL Notifier reference
product as a source or parity target; it does not implement, homologate,
advertise or remove a MySQL database provider.

The local external tree remains ignored, untracked and unmodified. Its physical
deletion or relocation is a distinct destructive action and was not inferred
from the authority to revoke functional use.

## Validation

- The development-flow policy verifier passed 123 assertions, including
  bounded checks of the governing `7.0.0` changelog section, project vision,
  README, preserved product behaviour, independent MySQL provider boundary and
  the new append-only history entry.
- The standalone development-flow regression passed 98 assertions.
- The code-documentation gate passed for 447 comment-capable source files, and
  the Markdown-link gate passed for 993 local links in 230 files.
- The secret scan passed for the current non-ignored worktree and available Git
  history; `git diff --check` passed.
- The post-change external-tree verification reproduced 110 files, ten
  directories, 6,554,849 bytes and the same deterministic tree identity. Git
  still reported zero tracked target files and the existing ignore rule.
- The 766,612-byte prefix of the append-only transition log retained SHA-256
  `8aa11f2ceebf3a2ab3094476ec77247d2066b668e14a2ef04dd44c9317167bfc`.
- Initial independent frozen-diff review reported `P0=0`, `P1=0`, `P2=1` and
  `P3=0`. The sole `P2` was the policy verifier's incomplete protection of
  non-effects and documentary boundaries; the assertions listed above close
  that exact gap.
- Final independent re-review closed at `P0=0`, `P1=0`, `P2=0`, `P3=0` and
  independently confirmed the corrected assertions, unchanged historical
  matrix, append-only prefix, external-tree identity and zero tracked target
  files.
- A post-review policy execution then failed because an administrative plan
  edit had renamed the mandatory `- Execution mode:` control. Restoring that
  exact label with `SEQUENTIAL_ONLY` and keeping `SINGLE_OWNER` as the separate
  topology corrected only the control record; the next execution passed all
  123 assertions. The failure is preserved as a failure.
- `Quick` and canonical `Full` were not applicable to this policy/documentation
  revocation because no executable product behaviour, dependency, generated
  artefact or runtime composition changed. They were not used as evidence.

## Result

The local folder was read, analysed and verified within the stated static
boundary. The evidence supports treating it as quarantined historical material,
not as a requirements source or implementation blueprint. The governing
revocation removes the former comprehensive-parity mandate while preserving
historical truth, existing DB-Notifier-owned work and the independent provider
roadmap. Lifecycle, Human Gates, activation and external authority remain
unchanged.
