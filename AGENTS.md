# DB-Notifier — Permanent Agent Instructions

## Purpose and authority

This file is the primary operational source for permanent, reusable instructions that govern work in this repository. It applies to the whole workspace unless a more specific `AGENTS.md` is deliberately introduced for a subtree.

Before acting, read [`prompts/Start-Here.md`](prompts/Start-Here.md), the current factual state, and the documents routed there for the requested work. The adopted [`AI Software Engineering Master Prompt`](prompts/system/AI-Software-Engineering-Master-Prompt.md) owns the general engineering method, role model, proportionality and work modes; its adoption matrix routes each specialised rule to the existing thematic authority. This file consolidates cross-cutting behaviour; it does not replace accepted ADRs, lifecycle gates, security policy, the Design System, or factual evidence.

The governed [`Language Policy`](prompts/governance/Language-Policy.md) is the single thematic authority for owner communication, project-owned artefact language, external naming conventions, preservation of existing content and separation from user-interface localisation.

Resolve conflicts using the precedence in `prompts/Start-Here.md`. A current explicit user instruction may refine the requested scope, but it does not silently waive safety, data protection, external-authorisation boundaries, or lifecycle gates. Surface any unresolved material conflict before taking an irreversible or externally impactful action.

## Autonomous delivery mandate

- The product objectives and requirements recorded in the canonical project sources are established inputs. The coordinating agent owns prospective local engineering decisions and continues safe authorised work without requesting routine owner approval.
- Development `Human Gate` requirements, manual conversation navigation and copy-and-paste hand-offs are prospectively superseded by objective `Agent Gate` decisions and tool-confirmed automatic dispatch. Historical decisions and evidence remain unchanged and retain their original meaning.
- Use only these autonomous lifecycle dispositions: `AGENT_DECIDED`, `AUTOMATED_GATE_PASS`, `AUTOMATED_GATE_FAIL`, `LOCAL_COMPLETE`, `EXTERNAL_PREREQUISITE` and `BLOCKED_BY_HIGHER_AUTHORITY`.
- Local architecture, planning, implementation, review, validation, documentation and lifecycle decisions are delegated to the coordinating agent. Product-user authentication, RBAC, administrative confirmation, legal and licensing constraints, clean-room boundaries, secrets, destructive operations and external systems remain governed by their specialised authorities.
- A local destructive or difficult-to-reverse operation requires the Automated Safety Gate in `Governance.md`. Fail closed on a workspace root, home directory, broad path, unresolved variable, substitution, glob or unresolved target; require protected-work preservation, recoverable checkpoint, rollback, no safer alternative, objective necessity and independent review.
- An external action requires cumulatively an applicable standing authority, an existing currently valid credential through a safe mechanism, the exact account and environment, an available capable tool, a defined cost boundary when applicable, an objective success criterion and safe verification or reversal. Missing evidence is `EXTERNAL_PREREQUISITE`, not a request for routine owner routing.
- Continue automatically while safe in-scope work exists. Notify the owner only with a consolidated final result or when an unavoidable external prerequisite is the sole remaining blocker; never ask the owner to copy, paste or relay a payload that an available agent or tool can dispatch directly.

## General project conventions

- `DB-Notifier` is the canonical product name in architecture, governance and technical prose. User-facing visual surfaces use the display name `DB Notifier`; use `DBNotifier` only where a technical identifier cannot contain spaces or punctuation.
- DB-Notifier is the independent successor to PgNotifier, which was conceptually inspired by MySQL Notifier. The owner's corrected intent in `GOV-MN-RESTORE-01` supersedes the prospective prohibition created by `GOV-MN-REV-01`: it restores the MySQL Notifier functional-inspiration clause of `REQ-047`, while `REQ-048` and `REQ-050` again govern bounded functional coverage. MySQL Notifier 1.1.8 may inform sanitised, observable and non-expressive functional outcomes for improving DB-Notifier. It is not an implementation base, source-compatibility target or mandate to clone distinctive visual, textual or interaction expression. Recreate useful outcomes through independently authored, provider-neutral DB-Notifier requirements, code, tests and assets; preserve the DB-Notifier MIT licence and independent identity.
- Maintain the explicit functional-coverage matrix for `REQ-050`, `MN-001`–`MN-025` and `MN-Q01`–`MN-Q04`. Classify each observed outcome as adopted, safely adapted, deliberately rejected/replaced or scheduled in its authorised lifecycle phase. “All functionality” means outcome coverage, not identical mechanism, selection, arrangement or interface. It never overrides provider neutrality, least privilege, Agent Gates, licensing, secret handling or the prohibitions on automatic firewall, tunnel and infrastructure mutation. MySQL Notifier is the functional reference product; MySQL remains a separately governed future database provider.
- Do not import, copy, translate, adapt, link, redistribute or derive implementation expression from Oracle/MySQL source code, binaries, artwork, logos, trade dress, product copy or vendor-specific internal architecture. When source-tree inspection is specifically authorised, isolate it to a read-only source-exposed analyst who may deliver only an approved sanitised provenance record and behavioural specification. The corresponding implementation and test authors must remain unexposed to source, binaries, assets, decompiled material and raw or unsanitised source-derived notes; they may receive only that approved sanitised handoff. An independent provenance and similarity review is mandatory before integration.
- No protected expression or third-party material originating in, bundled with or derived from the MySQL Notifier reference tree may enter an MIT deliverable unless the exact component and rightsholder provenance, applicable rights or permissions and a compatible distribution model are documented, specialist legal review is complete and the owner separately authorises that model. An owner decision or the local additional linking permission does not by itself relicense that Oracle/MySQL or third-party material, grant trademark rights or establish one licence for the whole reference tree.
- Build an open, professional, provider-neutral database monitoring and controlled-administration platform, not a PostgreSQL-only product.
- Treat `MOD-12 AIOPS_AI` as DB-Notifier's principal strategic product differentiator: evidence-grounded, provider-neutral intelligence that progresses from deterministic `OBSERVER` signals to independently gated recommendations and typed controlled automation. This priority never bypasses data governance, lifecycle states, Quality/Agent Gates, provider homologation, least privilege, or the separation between recommendation, policy decision and execution.
- Treat roadmap, implementation, homologation, public support, runtime availability, and authorisation as distinct facts.
- Apply the adopted engineering framework proportionally to scope and risk. Activate only the roles needed for the current work, preserve independent review where risk requires it, and treat generic tools, stacks, branches and directory layouts in that framework as examples rather than implicit project requirements.
- Never invent implementation, support, evidence, test results, credentials, approvals, runtime state, or environment capabilities.
- Before beginning every newly authorised technical action on the codebase or product, perform the mandatory DB-Notifier shutdown preflight. Triggers include source/configuration/documentation changes, adjustments, remediations, modifications, implementations, executable audits, builds, tests and agent runtime samples. Stop and close every currently running DB-Notifier component and project-owned runtime, including WPF/Tray, Dashboard preview or development servers, Agent, API, project-owned background helpers and any dedicated review browser. Identify shared-host processes through verified PID, executable path, command line or parentage rather than by process name alone. Verify that no matching process, visible window, notification-area instance or owned listener remains before continuing. Never stop a monitored database engine/service, the user's ordinary browser, IDE or any unrelated process under this rule. If complete shutdown cannot be proved, report the exact residue and treat the technical action as blocked. Pure conversation, questions, explanations, status reports and other turns that perform no codebase/product action do not trigger shutdown. A runtime may remain visible only while its current technical action is collecting an authorised agent sample; perform shutdown before any subsequent technical action.
- Inspect the current state before work. Advance a lifecycle state only after its objective Quality Gate and independent Agent Gate pass on the exact baseline; the coordinating agent records the decision and continues automatically.
- For every broad, cross-cutting, multi-increment or audit-plus-remediation activity, create or update the tracked root `PLANS.md` before implementation and keep it synchronised through the final internal dispatch. Record baseline, authority, positive and negative scope, protected work, ownership, Definition of Ready, Definition of Done, findings, increments, evidence, blockers and outcome. `PLANS.md` is an execution ledger only; it never overrides thematic authority, current factual state, append-only history, an ADR, a Quality Gate or an Agent Gate.
- Before implementation, close the task envelope: freeze the baseline and applicable contracts, classify artefacts and mutable resources, assign exactly one writer per boundary, name reviewers and checks, and define objective stop codes. Baseline drift, scope overlap, unready dependency, isolation failure, resource collision, gate failure or a genuine external or higher-authority prerequisite stops the affected work rather than widening it implicitly.
- Use `scripts/development.ps1` as the canonical local development entry point. `Doctor` is read-only, `Setup` performs locked restores without installing toolchains, `Quick` is explicitly `NON_GATE`, and `Full` delegates exactly once to `scripts/ci.ps1`, the canonical aggregate repository gate. `-PlanOnly` is deterministic and side-effect free. Offline execution must identify online-only checks as `NOT_RUN` and a full offline result as partial evidence, never as equivalent to the online gate.
- Execute every authorised increment completely, including its applicable checks and documentation, before recommending progression.
- Apply the governed [`Conversation Coordination and Safe Parallel Work`](prompts/governance/Conversation-Coordination-Prompt.md) contract to every internal dispatch. Use only `CONTINUE_CURRENT`, `DELEGATE_SUBAGENT`, `RETURN_TO_EXISTING` or `START_NEW_AUTO_DISPATCH`; dispatch the complete payload directly through the available platform mechanism, retain a receipt, reconcile uncertain results before retrying and prevent duplicates.
- Apply [`Continuous Improvement`](prompts/governance/Continuous-Improvement.md) to every recurring audit/remediation loop. Consume immutable events, execute at most one bounded next action, reject the same candidate forever, require a new causal delta for any successor, keep implementation/review/integration/observation roles disjoint, and quarantine invalid progress.
- A dispatch payload records repository, baseline, objective, positive and negative scope, protected work, ownership, checks, evidence, stop conditions and expected return. It is internal operational data, not text for the owner to copy or relay.
- When a route or tool is unavailable, continue locally, delegate internally, return to a confirmed task or retain the work in the coordinated queue. Use `EXTERNAL_PREREQUISITE` only when no safe material progress remains.
- Owner-facing completion communication is a concise consolidated factual report in `pt-BR`. It contains results, validations, limitations, residual risks and unavoidable external prerequisites, but no routing choice, suggested title, exact next message or manual forwarding instruction.
- Apply the governed [`Language Policy`](prompts/governance/Language-Policy.md) to every interaction and project artefact. Communicate with the owner in `pt-BR`; write new standalone project-owned artefacts in `en-GB`; preserve the established language of an existing file during a limited amendment; keep mandatory external names unchanged; and never infer user-interface locale from conversation or engineering language.
- Permit only one writer for each path, logical artefact, contract, schema, migration, lockfile, manifest, mutable data set, port, process, runtime, external resource, or other shared mutable boundary. Without a specifically authorised parallel-write Git workflow and isolated worktrees, concurrent conversations remain read-only and all writing is sequential in the coordinating conversation.
- Keep integration, current state, history, instruction changelog, ADR decisions and Agent Gate reports in the single coordinating task. The coordinator may decide local architecture and lifecycle progression after objective gates pass; custody never authorises an unsafe Git operation or an external action lacking its own prerequisites.
- Include separate counts for formal decisions, macro stages, lifecycle `STATE`s or product activation only when one of them is part of the user's requested target or the user explicitly asks for that view. Never mix those categories with technical lots, and never make the user decode a broad roadmap to learn whether the current task finished.
- Calculate every reported remaining-work count after the activity just completed. Name counted items so the number is auditable without inference. Use `minimum N` only when future findings can add work and identify those additions separately. If an exact count is not knowable, write `undetermined`, explain why, and never guess.

## Mandatory technology baseline

- Use .NET 10 LTS for the entire project, from initial development through release and maintenance.
- Active .NET projects target `net10.0` or the appropriate .NET 10 platform-specific target such as `net10.0-windows`.
- Do not introduce, retain, or recommend an earlier .NET target for active DB-Notifier code.
- Keep SDK, CI, build, test, packaging, documentation, and examples aligned with .NET 10.
- Express developer toolchains as bounded stable compatibility ranges rather than exact version pins: .NET SDK `>=10.0.302 <10.1.0`, Node.js `>=24.18.0 <25.0.0`, and npm `>=11.16.0 <12.0.0`. Keep prereleases disabled, retain the validated lower bounds, and require a separate governed update before crossing an exclusive upper bound.
- Use React and TypeScript for the Web Dashboard and WPF on .NET 10 for the Windows Desktop application unless an accepted ADR supersedes that decision.

## Architecture and organisation

- Keep Domain and Application independent of database engine, UI, transport, persistence, and operating system.
- Dependencies point inwards. Provider, infrastructure, persistence, Agent, API, Desktop, Dashboard, and packaging code remain in their owning boundaries.
- Do not add engine-name conditionals, closed engine enums, or provider-specific business rules to the neutral core.
- Extend database behaviour through the versioned Provider SDK, stable provider identifiers, typed non-secret configuration, opaque credential references, declared capabilities, normalised outcomes, and provider-owned fixtures.
- Keep monitoring, administrative commands, operating-system service control, and cloud control-plane actions as separate capabilities and identities.
- Preserve offline Agent operation, versioned contracts, idempotency, outbox/reconciliation semantics, UTC timestamps, freshness, and explicit unknown/stale states.
- Treat SQLite used for authorised Agent persistence separately from a future SQLite monitoring provider.
- Add a new module or instruction file only when it has a genuinely different authority, lifecycle, owner, or audience. Prefer the existing owning module or document otherwise.

## Database providers and connectivity

- The target provider catalogue is open to all database engines. Prioritise widely used engines, including PostgreSQL, MySQL, MariaDB, Microsoft SQL Server/Azure SQL, Oracle, SAP HANA, SQLite, MongoDB, IBM Db2, Redis/Valkey, and other relational, NoSQL, distributed, search, time-series, graph, and managed-cloud platforms.
- Implement and homologate providers incrementally and independently by engine, version, platform, topology, and operation. One working provider must not be presented as proof of general support.
- Represent local, remote, Windows, Linux, container, datacentre, hybrid, and cloud targets without assuming that every capability exists in every topology.
- The Agent connects through an approved provider driver, protocol, socket, native utility, or vendor/cloud API. The Dashboard and central API do not connect directly to monitored databases.
- DB-Notifier must not open firewalls, publish databases, create public endpoints, or establish unapproved tunnels automatically.
- Treat transport reachability as transport evidence only. A successful TCP connection is not proof that a database is authenticated or healthy.
- Return `Unsupported`, `Unavailable`, `Denied`, or the appropriate canonical state when a capability cannot be proved; never improvise an administrative operation.

## Security, credentials, and external actions

- Apply least privilege, deny by default, server-side authorisation, auditable actions, and separation of responsibilities.
- Monitoring credentials, database-administration credentials, operating-system service identities, cloud control-plane identities, human identities, and Agent identities are separate.
- Store secrets only through approved operating-system, corporate, workload, or cloud secret stores. Persist opaque references, never plaintext secret material.
- Never place connection strings, passwords, tokens, private keys, full certificates, or secret values in code, configuration, UI, logs, exceptions, tests, screenshots, reports, commits, or chat output.
- Never place a real computer, host, workstation, or device name in any project file name or file content, including configuration, logs, evidence, caches, screenshots, generated artefacts, and synchronisation-conflict copies. Replace it with a stable sanitised placeholder such as `<host>` before retaining the artefact in the workspace.
- Sanitise evidence while retaining enough information to reproduce the result.
- Administrative Start, Stop, or Restart requires an implemented and homologated capability, exact permission and scope, explicit confirmation and reason, authenticated replay-resistant transport, idempotency, expiry/timeout, audit, and a post-action probe.
- Do not deploy, publish, run a remote migration, install external software, control a real database/service, rotate credentials, or mutate external infrastructure without the required specific authority and lifecycle permission.
- A bare project `Start`, `Stop`, `Restart`, or `Status` request applies to DB-Notifier processes only. It must not control PostgreSQL or any other database service unless database control is explicitly requested and authorised.
- Preserve all pre-existing user work. Never use destructive Git or filesystem operations to discard unrelated changes.

## Code standards

- Prefer clear, cohesive, testable code with one owning responsibility per module or component.
- Use language and framework conventions consistently; prefer typed contracts and explicit failure outcomes over implicit or stringly typed behaviour.
- Validate inputs at trust boundaries, bound retries/timeouts/batches, honour cancellation, and design distributed or repeatable operations for idempotency.
- Avoid shell execution for database utilities when a typed process or library API is available. Treat paths, endpoints, provider packages, and external content as untrusted input.
- Keep secrets and provider-native details out of canonical domain events and user-facing diagnostics.
- Cover changed behaviour with proportionate unit, architecture, integration, accessibility, security, compatibility, or regression tests.
- Do not alter generated or immutable artefacts manually. Change their canonical source and regenerate them deterministically.
- Do not refactor unrelated source merely to satisfy an instruction-maintenance task.

## Mandatory comments and code documentation

- All comments and code-focused documentation in hand-written source or comment-capable configuration files must be written exclusively in British English (`en-GB`). Do not use American English, Portuguese, or mixed-language comments.
- Begin every hand-written, comment-capable module with a concise description of its purpose, responsibilities, architectural relationships, and important boundaries.
- Document every new or modified class with its responsibility, intended use, significant design decisions, and interactions with other components.
- Document every new or modified function or method with its purpose, parameters, return value, possible exceptions or failure outcomes, and important implementation notes.
- Document significant logical blocks when intent is not self-evident, including assumptions, limitations, edge cases, dependencies, performance considerations, and possible side effects.
- For HTML, document significant structural, navigation, form, reusable-component, and accessibility sections when their purpose is not evident.
- For CSS, document the intent of significant style groups, layout choices, responsive behaviour, and non-obvious design decisions; do not restate property names.
- For JavaScript, TypeScript, C#, PowerShell, Python, shell, SQL, and other languages, document asynchronous work, event handling, state transitions, transformations, validation, external communication, and error handling where meaningful.
- Prefer comments that explain why the implementation exists or why a choice was made. Do not narrate obvious syntax or add generic text merely to satisfy a counter.
- Keep documentation concise, technically accurate, valuable, and synchronised. Update or remove stale comments whenever their code changes.
- New or modified public C# APIs require XML documentation. Other new or modified APIs, functions, and classes use the language's native documentation form.
- Follow the exact generated, immutable, strict-format, and syntax exceptions in [`docs/Code-Documentation-Standards.md`](docs/Code-Documentation-Standards.md). Exceptions are narrow and must not be expanded silently.
- Run the repository documentation gate after applicable changes and supplement it with independent semantic agent review of en-GB vocabulary, API completeness and technical value.

## Naming and legacy compatibility

- Migrate PgNotifier artefacts deliberately to canonical DB-Notifier names; never perform an indiscriminate global replacement.
- Preserve required legacy entry points as explicit, documented, tested compatibility shims that warn where practical and delegate to one canonical implementation.
- Compatibility paths must not fork product behaviour, overwrite legacy configuration, expose secrets, or recreate deprecated output names.
- Preserve versioned configuration migration, dry-run defaults, validation, backup, atomic writes, idempotent reruns, rollback, and sanitised reporting.
- Remove a legacy name only after every condition in [`docs/Legacy-Compatibility.md`](docs/Legacy-Compatibility.md) is evidenced and the owning lifecycle gate explicitly approves removal.
- Keep naming consistent across namespaces, assemblies, packages, commands, configuration, documentation, UI, tests, installers, and generated artefacts.

## File and documentation structure

- `AGENTS.md`: primary permanent, reusable instructions for repository agents.
- `prompts/Start-Here.md`: authoritative routing, precedence, and entry to the detailed instruction corpus.
- `prompts/governance/Language-Policy.md`: thematic authority for owner communication, project artefact language, preservation and interface-language separation.
- `prompts/governance/Conversation-Coordination-Prompt.md`: thematic authority for conversation routing, safe parallel work, exclusive ownership and coordinated integration.
- `prompts/governance/Continuous-Improvement.md`: thematic authority for stable findings, bounded event-driven improvement, promotion observation, rollback and anti-gaming metrics.
- `prompts/foundation/`: product vision, solution architecture, and AIOps/AI direction.
- `prompts/governance/`: lifecycle, authority, quality gates, and security/access rules.
- `prompts/state/Current-State.md`: present factual state only.
- `prompts/state/Continuous-Improvement-Backlog.md`: factual improvement-queue snapshot; never standing authority or a mutable execution ledger.
- `prompts/state/State-Transition-Log.md`: append-only factual transition/increment history.
- `prompts/operations/`: task-specific operational playbooks.
- `prompts/templates/`: evidence templates; templates are not approvals or proof of execution.
- `prompts/system/`: adopted cross-cutting engineering framework, instruction-corpus version and change history.
- `docs/architecture/` and `docs/data/`: accepted decisions and specialised technical contracts.
- `docs/design/DB-Notifier-Design-System.md`: normative frontend specification.
- `docs/STATE-*` and migration reports: historical evidence, not standing authority unless an active governing document explicitly adopts a decision from them.
- Keep current truth out of historical reports, and do not rewrite historical evidence to make it appear current.
- Keep Markdown links valid, headings coherent, terminology canonical, and instruction changes recorded in the corpus changelog.

## Frontend and UX requirements

- Follow the official DB-Notifier Design System for every new or modified React or WPF interface.
- Maintain a modern, clean, calm, professional, restrained enterprise identity. Avoid sci-fi, neon, glassmorphism, excessive glow, decorative animation, and exaggerated effects.
- Support only explicit Light and Dark theme preferences with safe persistence, no incorrect-theme flash, and semantic parity between React and WPF; retired System values fail safely to Light, while Windows High Contrast remains an independent accessibility override.
- Keep the native WPF caption and application scrollbars aligned with the effective Light/Dark theme. When Windows High Contrast is active, return native caption colours to Windows and resolve scrollbar colours through the existing system-backed semantic resources instead of forcing product colours.
- Present global language and theme preferences as one discreet code-native icon button each in the upper-right TopBar; use a recognisable translation/languages symbol instead of an ambiguous globe or flags, and expose current and next states through localised accessible names and tooltips rather than permanently expanded button groups.
- Provide Dashboard TV presentation through one expand/collapse icon control, session-only state and optional browser Fullscreen; keep the exit path, freshness, stale/unknown states and demonstration/external-source truth visible, and never describe presentation refresh as a proved real-time feed. During `STATE-06`, load authorised API state on TV entry and re-read it every 30 seconds while TV mode remains active without overlapping requests; authenticated SignalR hints may trigger an earlier re-read but never replace periodic reconciliation.
- Treat the Windows client as notification-area-first: normal startup remains hidden in the Windows notification area, a single icon activation opens the compact provider-neutral fleet flyout, and the full WPF shell is a secondary destination rather than a competing primary dashboard. Every explicit close of that secondary shell requests a fresh local availability confirmation; minimise, hidden startup and explicit Exit do not. Prefer a registered Windows app notification with the display name `DB Notifier` and the generated transparent identity asset, retaining the legacy balloon only as a fail-safe local fallback; platform acceptance is not proof that Windows displayed the card. Preserve factual source/freshness truth and safe Desktop navigation. Authoritative change notifications bind only during authorised integration; a separately authorised `STATE-05` demonstration may emit one clearly labelled local notification per individual transition from its deterministic fixture only, with a silent initial baseline, explicit no-external-data text, event-specific semantic mark, in-memory deduplication and a bounded serialised fallback. Expose service control only after the exact capability, privilege, confirmation, audit and homologation contracts are proved.
- Consume canonical generated semantic/component tokens. Do not create parallel token sets, provider-specific themes, or arbitrary raw visual values without a documented narrow exception.
- Use one canonical database-and-bell product-mark geometry, proportion set and transparent silhouette on every Web, WPF, Tray, taskbar, title, favicon, notification and packaging surface. Rendering may adapt sampling to the target size and the bell may use the factual semantic colour, but no size or consumer may introduce a different micro-glyph, seam layout, backing shape or alternate brand model.
- Meet WCAG 2.2 AA, including contrast, keyboard access, focus visibility/order/restoration, zoom/reflow, reduced motion, screen-reader semantics, non-colour status cues, and High Contrast behaviour where applicable.
- Preserve factual operational distinctions in labels and states. Visual polish must never imply support, freshness, permission, connectivity, or successful execution that has not been proved.
- Keep privileged and destructive actions visually distinct, disabled by default until all policy conditions are satisfied, and accompanied by clear reason and consequence.

## Quality, testing, and evidence

- Discover and run the real repository checks applicable to the change. A banner, isolated compilation, or absence of visible errors is not sufficient evidence.
- Run `scripts/development.ps1 Quick` for bounded feedback when useful and `scripts/development.ps1 Full` for the aggregate gate when the declared compatible prerequisites and authorised runtime scope are available. Direct component checks remain diagnostic or supplemental evidence and must not be presented as the complete canonical gate.
- Validate in proportion to risk: formatting, build, tests, static analysis, dependencies, secret scanning, architecture boundaries, compatibility, accessibility, packaging, runtime health, and rollback as applicable.
- Record actionable review findings as `P0`, `P1`, `P2` or `P3`, with path/location, scenario, impact, evidence and recommendation. Broad or high-risk work requires an independent read-only review before final integration; unresolved `P0` or `P1` findings prevent a passing disposition.
- For runtime work, verify actual process/service state and live health; do not rely only on startup output. Stop validation processes when the task does not authorise leaving them running.
- When an authorised agent review opens the local Dashboard, always launch a dedicated browser process with its own isolated temporary profile. Never open the review in, attach automation to, add a tab to, or reuse the browser/profile the user is already using, even when a separate window would be available there. Leave every unrelated browser process, profile, window and tab untouched; close only the dedicated review browser according to the sample workflow, and remove its temporary profile only after that dedicated process has ended.
- Distinguish observed, inferred, not tested, and blocked results. Record commands, environment, versions, exit codes, scope, and sanitised artefacts where appropriate.
- Preserve the current automated coverage floors of 70% lines and 45% branches. Treat 80% line coverage as a risk-based directional target, not an automatic replacement gate, and never lower an existing component floor without explicit authority, evidence and a governance record.
- Keep mechanical evidence and Agent Gate decisions separate. An Agent Gate is `AUTOMATED_GATE_PASS` only when every required mechanical disposition is `PASS`, the exact baseline is frozen, applicable samples are complete and independent review reports zero unresolved `P0` or `P1` findings.
- A mechanical `FAIL`, `BLOCKED`, `PARTIAL` or required `NOT_RUN` produces `AUTOMATED_GATE_FAIL`; no agent may relabel, suppress or compensate for it with judgement.
- Preserve every historical Human Gate record unchanged. A later dispute creates a new factual addendum and current Agent Gate; it never rewrites or retrospectively fabricates evidence.
- Do not transition to a later state while an Agent Gate, required remediation or required sample remains incomplete.

## Docker and multi-database laboratory

- Docker Desktop may be used for disposable provider validation when the current lifecycle state and requested increment authorise integration or homologation work.
- Use pinned images, project-scoped containers/networks/volumes, non-production credentials, bounded resources, health checks, repeatable setup/cleanup, and sanitised evidence.
- Start heavy database engines sequentially or in capacity-checked waves rather than all simultaneously, unless the user explicitly authorises otherwise and the host capacity has been verified.
- Do not modify unrelated containers, images, volumes, networks, or host database services.
- A successful container smoke test proves only the tested engine/version/topology/capability matrix entry; it does not grant general provider support.

## Git and delivery workflow

- Inspect `git status` and the relevant diff before editing and again before committing.
- Keep changes scoped. Preserve and exclude unrelated user edits, environment drift, logs, caches, generated build output, installers, binaries, and secrets.
- Every user-authorised project modification, task, activity or action that changes tracked repository files must end with a focused local commit before the final hand-off. This applies whether its outcome is complete, partial, blocked or failed. This standing instruction authorises that local commit without requiring a separate request. Use Conventional Commits in the form `<type>(<scope>): <description>`, write the description in British English (`en-GB`) and include only the related increment and its factual evidence.
- Treat the coherent hand-off state of each user-authorised modification, task, activity or action as the commit boundary. Internal commands, inspections, checks and partial implementation steps within that boundary are not separate commit-worthy actions. When one request produces multiple independently reviewable hand-off states, preserve them in separate focused commits.
- A blocked outcome, material incompleteness or failed validation is not by itself an exception to the final commit requirement. Commit the safely isolated state and record the failure or limitation factually; never present failed validation as passed.
- Do not create a commit only when the current user explicitly prohibits it, no tracked change exists, or a safe focused commit cannot be isolated from unrelated work, generated residue or secrets. In the latter case, do not commit unsafe content: report the exact impediment and leave the activity explicitly blocked until a safe commit can be produced.
- Review the staged diff and run `git diff --cached --check` plus applicable validations before committing.
- Standing authorisation for a local final commit does not authorise amend, rebase, force-push, push, publish, merge, release, deployment, or opening a pull request; each requires its corresponding explicit authority.
- Report the commit identifier and validation outcome in the final hand-off.

## Maintaining these instructions

- When the user provides a new permanent, reusable project instruction, update this `AGENTS.md` instead of duplicating it across unrelated Markdown files.
- If a permanent rule requires a specialised normative specification, keep its concise cross-cutting rule and link here, then maintain the detailed contract in its single owning document.
- Do not create a lowercase `agents.md` duplicate or another general-purpose instruction file.
- Before reorganising instruction files, audit the full active corpus, record a concise internal plan, preserve every still-valid rule, and distinguish authority from historical evidence.
- Do not remove, rename, or consolidate an instruction file until all relevant rules and inbound references are mapped and preserved, and the proposed strategy has been presented to the user.
- Record changes to the instruction system in `prompts/system/Prompt-System-Change-Log.md`; update current state or transition history only when the change is factual and belongs there.
