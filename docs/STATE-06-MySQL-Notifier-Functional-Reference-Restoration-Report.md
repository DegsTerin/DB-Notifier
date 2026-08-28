# STATE-06 MySQL Notifier Functional Reference Restoration Report

## Control

- Date: 2026-08-28
- Plan: `GOV-MN-RESTORE-01`
- Baseline: `main@830b20c423b604cc72ec2e1b53423ac53066e067`
- Lifecycle: `STATE-06 INTEGRATION`; unchanged
- Authority: the owner clarified that the intended direction is to recreate
  MySQL Notifier functionality in DB-Notifier and use MySQL Notifier as a
  functional reference for product improvement
- Disposition: corrected functional authority restored with a separated-role,
  non-expressive outcome boundary

## Corrected authority and historical continuity

The owner's current clarification supersedes the prospective prohibition
created by `GOV-MN-REV-01`. It does not erase the earlier literal instruction,
the audit performed under it, commit
`830b20c423b604cc72ec2e1b53423ac53066e067`, the failed and passing policy
executions, the independent reviews or the append-only transition entry. Those
remain factual history.

`GOV-MN-RESTORE-01` restores the MySQL Notifier functional-inspiration clause
of `REQ-047`, together with `REQ-048`, `REQ-050`, `MN-001`–`MN-025` and
`MN-Q01`–`MN-Q04` as active functional coverage. Their dispositions and
remaining exits route potential incremental work; they do not authorise an
implementation lot, import the reference implementation, require all records
in one lot, claim compatibility or advance lifecycle.

## Permitted functional-reference model

MySQL Notifier 1.1.8 may inform only sanitised, observable and non-expressive
functional outcomes. Public behavioural documentation and a sanitised
capability specification may establish what a user can accomplish, the factual
states involved, negative behaviour and acceptance criteria. The reference is
not an implementation base, source-compatibility target or mandate to clone a
distinctive selection, arrangement, interface, text, visual treatment or
interaction expression.

Every adopted or adapted outcome must be implemented through DB-Notifier-owned,
provider-neutral architecture, code, tests, assets and evidence. Existing
security, accessibility, least-privilege, secret-handling, lifecycle and Human
Gate authorities remain controlling. `REJECT/REPLACE` mechanisms remain
rejected even when literal similarity would be easier.

## Source-exposure separation

The already completed `GOV-MN-REV-01` audit supplies a sanitised capability,
provenance and risk inventory. This corrective lot additionally used one
separated read-only provenance review. That reviewer sampled licence/notice
material plus representative source, project and resource-manifest metadata to
classify ownership and asset risk. No target file was executed, built,
decompiled, copied into the workspace or modified, and no source extract entered
project artefacts, the implementation/test handoff or a commit.

Any future source-code inspection requires separate explicit authority and the
following role separation:

1. a source-exposed analyst may inspect only the authorised material and may
   deliver only an approved sanitised provenance record and behavioural
   specification;
2. corresponding implementation and test authors must remain unexposed to the
   source, binaries, assets, decompiled material and raw or unsanitised
   source-derived notes; they may receive only that approved sanitised handoff;
3. no source extract, code signature, identifier, constant, binary content,
   asset or internal design is transferred to implementation or tests; and
4. an independent provenance and similarity review is required before
   integration.

The local tree remains ignored and has zero tracked files. The last recorded
deterministic identity is
`6f7c58b1c36c91dd2c3d7406aba31eb3d8213e6e737dce23be6fc0eaa1b4c676`
for 110 files in ten directories totalling 6,554,849 bytes. This identity
identifies only the local tree; it does not prove upstream authenticity or
chain of custody.

## Rights, licensing and provenance boundary

The local licensing material identifies GPLv2, an additional linking
permission and multiple third-party notices. The additional permission concerns
linking with separately licensed software included with the distribution; it
does not relicense Oracle/MySQL or third-party source under MIT, create one
blanket licence for every file, establish binary provenance or grant trademark
rights.

This restoration does not authorise literal source reuse. No protected
expression or third-party material originating in, bundled with or derived from
the MySQL Notifier reference tree may enter an MIT deliverable unless all of the
following are documented for the exact component:

- component and rightsholder provenance;
- the applicable licence, rights or permissions;
- a distribution model compatible with those obligations and the intended
  DB-Notifier deliverable;
- completed specialist legal review; and
- a separate owner decision authorising that evidenced distribution model.

Owner authority and legal review do not themselves relicense such third-party
material. This report records engineering controls and local observations; it
is not legal advice and makes no conclusion about compatibility, permission or
infringement.

The observable/non-expressive outcome boundary is an engineering control
informed by the distinction between functionality or methods of operation and
protected expression in [17 USC §102(b)](https://uscode.house.gov/view.xhtml?req=%28title%3A17+section%3A102%28b%29+edition%3Aprelim%29).
It does not decide whether a concrete implementation is derivative or resolve
contract, patent, jurisdiction or unfair-competition questions. Trade dress and
confusion remain separate concerns under
[15 USC §1125](https://uscode.house.gov/view.xhtml?req=%28title%3A15+section%3A1125+edition%3Aprelim%29).
The names Oracle and MySQL are used only for accurate referential identification
of the reference product and the separately governed database-engine candidate;
DB-Notifier claims no affiliation, endorsement or trademark right.
Oracle's
[Trademark Guidelines](https://www.oracle.com/legal/trademarks/)
state the owning company's conditions and explain that open-source licensing
does not ordinarily grant trademark rights.

## Active functional-coverage status

The 29 records form one mutually exclusive maturity inventory:

| Observed maturity | Count | Records |
|---|---:|---|
| Normal-composition behaviour still bound to demonstration data | 8 | `MN-001`, `MN-002`, `MN-003`, `MN-009`, `MN-016`, `MN-018`, `MN-019`, `MN-022` |
| Bounded, partial, injectable or test-only DB-Notifier implementation | 12 | `MN-004`, `MN-005`, `MN-006`, `MN-011`, `MN-012`, `MN-023`, `MN-024`, `MN-025`, `MN-Q01`–`MN-Q04` |
| Contracts exist but the end-to-end function does not | 3 | `MN-007`, `MN-008`, `MN-010` |
| Capability is absent | 3 | `MN-013`, `MN-020`, `MN-021` |
| Reference mechanism remains rejected/replaced | 3 | `MN-014`, `MN-015`, `MN-017` |
| **Total** | **29** | |

Twenty records have some DB-Notifier-owned behaviour. The 18 records in the
partial, contract-only and absent categories are not a complete inventory of
open work because the eight demo-bound records also retain exits. All 26
non-rejected records have an open or bounded exit, while three retain a
deliberate `REJECT/REPLACE` disposition. No record currently proves complete
operational or homologated MySQL Notifier parity: ordinary startup remains
demonstrative, no real provider is active and no public-support claim follows
from the matrix.

The later `S06-DFR-01` and `S06-DFR-02` evidence is recorded as a post-matrix
overlay rather than rewriting the original 29 traceability rows. It adds the
single-flight coordinator, bounded validation, last-accepted retention,
freshness ageing, atomic presentation, human `instances.read` projection and
injectable HTTPS adapter while preserving the ordinary demonstration startup.

## Mechanisms that remain rejected

- `MN-014`: automatic SSH-tunnel creation and silent credential fallback;
  use an approved path or outbound Agent, with any managed tunnel requiring its
  own security ADR and Human Gate.
- `MN-015`: remote WMI/DCOM callbacks and automatic firewall mutation;
  authenticated outbound Agent/API communication remains the architectural
  replacement.
- `MN-017`: automatic coupling to or watching of MySQL Workbench files; any
  future importer must be explicit, versioned, consented, reversible and
  secret-safe.

Name-based blind discovery, unconditional administrative commands, TLS
downgrade, plaintext secrets, arbitrary vendor executable launch, colour-only
state and guessed operations for unknown types also remain prohibited by their
own DB-Notifier safety authorities.

## Smallest coherent successor

The prioritised next lot is `S06-DFR-03 Desktop Fleet Authenticated Runtime
Binding`, strictly read-only. `S06-DFR-01` already owns reconciliation and
presentation, and `S06-DFR-02` already owns the endpoint, adapter and injection
seam. The remaining coherent gap is the normal executable's short-lived human
identity and authorised HTTPS composition.

That future lot is not authorised by this report. Its minimum scope would be:

- obtain a short-lived human identity and inject the existing HTTPS adapter;
- require only `instances.read`;
- never reuse Agent mTLS identity or accept a token through arguments, files or
  environment variables;
- enforce exact origin, egress, proxy and redirect policy;
- fail closed to `Denied`, `Offline` or `Unknown` without silently substituting
  demonstration data; and
- retain the demonstration only as an explicitly labelled composition.

It must exclude providers, discovery, catalogue CRUD, operational notification
delivery, administrative commands, packaging and lifecycle transition. Entry
requires this restoration to be committed, a separate security decision for
Desktop identity/egress, a clean baseline, explicit technical authority,
identity/scope/revocation/redirect/offline/secret regressions, independent
security and architecture review, the applicable local gates and an explicit
human sample. `STATE-06 INTEGRATION` remains controlling.

## Non-effects

This corrective governance lot changes no product source, test behaviour,
dependency, lockfile, schema, generated artefact, runtime composition,
provider, database, service, external system, Human Gate, activation or
lifecycle state. MySQL remains a separately governed database-provider candidate;
the functional reference does not implement or homologate it.

The earlier revocation report, Human Gates, ADRs, implementation reports,
commits and historical entries remain unchanged. The correction affects only
prospective authority and routing.

## Validation

- The mandatory shutdown preflight passed before this technical action with no
  matching DB-Notifier process and no owned listener; the frozen baseline was a
  clean `main@830b20c423b604cc72ec2e1b53423ac53066e067`.
- The first development-flow policy execution returned exit code `1` because
  the exact provider-boundary assertion crossed a Markdown line break in this
  report. The boundary was present, but its canonical representation was
  corrected; the successor execution passed all `125` assertions.
- After policy coverage was strengthened for `REQ-047`, exact 29-row identity,
  separately authorised future source inspection and the non-authorisation of
  `S06-DFR-03`, the first strengthened execution returned exit code `1` because
  its count assertion crossed a Markdown line break. The assertion was made
  line-break-safe without weakening the contract; the successor passed all
  `127` assertions.
- The standalone development-flow regression then passed all `98` assertions.
- The code-documentation gate passed `447` comment-capable files.
- The Markdown-link gate passed `995` local links in `231` files.
- The available-history secret scan and `git diff --check` passed.
- The historical revocation report has no diff, and the first 771,775 bytes of
  the append-only transition log retain SHA-256
  `6ebb28113f5e36c9118939e27455f67db096f80974fbf7a3bab83fc8c4598762`.
- All 29 functional-matrix rows retain normalised SHA-256
  `8961a3af4b02de68a2b16b83159c48779e5d26cffaabbb6de387e4a6b61d1449`.
- One draft evidence patch initially matched repeated historical context and
  temporarily changed the append-only prefix. The immediate hash check caught
  it; the uncommitted block was removed, appended at the end and the frozen
  prefix restored before integration.
- The first independent authority review returned `FAIL` with `P0=0`, `P1=3`,
  `P2=3`, `P3=0`; the first licensing review returned `FAIL` with `P0=0`,
  `P1=1`, `P2=2`, `P3=1`. Every source-exposure, authority, count, clean-room,
  third-party-scope, policy and referential-naming finding was corrected. Both
  final frozen-diff re-reviews returned `PASS` with `P0=0`, `P1=0`, `P2=0`,
  `P3=0` and found no new issue.
- The cited official 17 USC §102(b), 15 USC §1125 and Oracle trademark sources
  were refreshed on 2026-08-28. That source check is not specialist legal
  review and does not decide licence compatibility, derivation or infringement.
- `Quick` and `Full` are `NOT_RUN` and not evidence for this result because no
  executable product behaviour, dependency, generated artefact or runtime
  composition changed.

## Result

`GOV-MN-RESTORE-01` is complete. The corrected owner intent is represented as
active functional coverage under a separated-role, non-expressive outcome
model. DB-Notifier may use the reference to decide what safe outcomes to
recreate and improve, while its MIT implementation remains independently
authored and every unsafe mechanism, licensing boundary, gate and lifecycle
condition remains enforceable. This completion grants no implementation
authority for `S06-DFR-03` or any other matrix exit.
