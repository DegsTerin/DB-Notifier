# DB-Notifier — Permanent Agent Instructions

## Purpose and authority

This file is the primary operational source for permanent, reusable instructions that govern work in this repository. It applies to the whole workspace unless a more specific `AGENTS.md` is deliberately introduced for a subtree.

Before acting, read [`prompts/Start-Here.md`](prompts/Start-Here.md), the current factual state, and the documents routed there for the requested work. This file consolidates cross-cutting behaviour; it does not replace accepted ADRs, lifecycle gates, security policy, the Design System, or factual evidence.

Resolve conflicts using the precedence in `prompts/Start-Here.md`. A current explicit user instruction may refine the requested scope, but it does not silently waive safety, data protection, external-authorisation boundaries, or lifecycle gates. Surface any unresolved material conflict before taking an irreversible or externally impactful action.

## General project conventions

- `DB-Notifier` is the canonical product name in architecture, governance and technical prose. User-facing visual surfaces use the display name `DB Notifier`; use `DBNotifier` only where a technical identifier cannot contain spaces or punctuation.
- DB-Notifier is the independent successor to PgNotifier, which was conceptually inspired by MySQL Notifier. This lineage does not imply code reuse, affiliation, or technical compatibility with MySQL Notifier.
- Build an open, professional, provider-neutral database monitoring and controlled-administration platform, not a PostgreSQL-only product.
- Treat roadmap, implementation, homologation, public support, runtime availability, and authorisation as distinct facts.
- Never invent implementation, support, evidence, test results, credentials, approvals, runtime state, or environment capabilities.
- Inspect the current state before work. Do not advance a lifecycle state automatically: an automatic audit and an explicit Human Gate are both required.
- Execute every authorised increment completely, including its applicable checks and documentation, before recommending progression.
- End every user hand-off with the concrete recommended next step.

## Mandatory technology baseline

- Use .NET 10 LTS for the entire project, from initial development through release and maintenance.
- Active .NET projects target `net10.0` or the appropriate .NET 10 platform-specific target such as `net10.0-windows`.
- Do not introduce, retain, or recommend an earlier .NET target for active DB-Notifier code.
- Keep SDK, CI, build, test, packaging, documentation, and examples aligned with .NET 10.
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
- Run the repository documentation gate after applicable changes and supplement it with human review of en-GB vocabulary, API completeness, and technical value.

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
- `prompts/foundation/`: product vision, solution architecture, and AIOps/AI direction.
- `prompts/governance/`: lifecycle, authority, quality gates, and security/access rules.
- `prompts/state/Current-State.md`: present factual state only.
- `prompts/state/State-Transition-Log.md`: append-only factual transition/increment history.
- `prompts/operations/`: task-specific operational playbooks.
- `prompts/templates/`: evidence templates; templates are not approvals or proof of execution.
- `prompts/system/`: instruction-corpus version and change history.
- `docs/architecture/` and `docs/data/`: accepted decisions and specialised technical contracts.
- `docs/design/DB-Notifier-Design-System.md`: normative frontend specification.
- `docs/STATE-*` and migration reports: historical evidence, not standing authority unless an active governing document explicitly adopts a decision from them.
- Keep current truth out of historical reports, and do not rewrite historical evidence to make it appear current.
- Keep Markdown links valid, headings coherent, terminology canonical, and instruction changes recorded in the corpus changelog.

## Frontend and UX requirements

- Follow the official DB-Notifier Design System for every new or modified React or WPF interface.
- Maintain a modern, clean, calm, professional, restrained enterprise identity. Avoid sci-fi, neon, glassmorphism, excessive glow, decorative animation, and exaggerated effects.
- Support Light, Dark, and System preferences with safe persistence, live system-theme observation, no incorrect-theme flash, and semantic parity between React and WPF.
- Present global language and theme preferences as one discreet code-native icon button each in the upper-right TopBar; expose current and next states through localised accessible names and tooltips rather than flags or permanently expanded button groups.
- Consume canonical generated semantic/component tokens. Do not create parallel token sets, provider-specific themes, or arbitrary raw visual values without a documented narrow exception.
- Meet WCAG 2.2 AA, including contrast, keyboard access, focus visibility/order/restoration, zoom/reflow, reduced motion, screen-reader semantics, non-colour status cues, and High Contrast behaviour where applicable.
- Preserve factual operational distinctions in labels and states. Visual polish must never imply support, freshness, permission, connectivity, or successful execution that has not been proved.
- Keep privileged and destructive actions visually distinct, disabled by default until all policy conditions are satisfied, and accompanied by clear reason and consequence.

## Quality, testing, and evidence

- Discover and run the real repository checks applicable to the change. A banner, isolated compilation, or absence of visible errors is not sufficient evidence.
- Validate in proportion to risk: formatting, build, tests, static analysis, dependencies, secret scanning, architecture boundaries, compatibility, accessibility, packaging, runtime health, and rollback as applicable.
- For runtime work, verify actual process/service state and live health; do not rely only on startup output. Stop validation processes when the task does not authorise leaving them running.
- Distinguish observed, inferred, not tested, and blocked results. Record commands, environment, versions, exit codes, scope, and sanitised artefacts where appropriate.
- Keep automatic audit and human validation separate. Never pre-fill or infer a Human Gate approval.
- Do not transition to a later state while a current gate, required remediation, or explicit human sample remains pending.

## Docker and multi-database laboratory

- Docker Desktop may be used for disposable provider validation when the current lifecycle state and requested increment authorise integration or homologation work.
- Use pinned images, project-scoped containers/networks/volumes, non-production credentials, bounded resources, health checks, repeatable setup/cleanup, and sanitised evidence.
- Start heavy database engines sequentially or in capacity-checked waves rather than all simultaneously, unless the user explicitly authorises otherwise and the host capacity has been verified.
- Do not modify unrelated containers, images, volumes, networks, or host database services.
- A successful container smoke test proves only the tested engine/version/topology/capability matrix entry; it does not grant general provider support.

## Git and delivery workflow

- Inspect `git status` and the relevant diff before editing and again before committing.
- Keep changes scoped. Preserve and exclude unrelated user edits, environment drift, logs, caches, generated build output, installers, binaries, and secrets.
- Use focused commits after each completed, verified increment. The standing project preference is to commit completed work rather than leave it uncommitted.
- Review the staged diff and run `git diff --cached --check` plus applicable validations before committing.
- Do not amend, rebase, force-push, push, publish, or open a pull request unless the user explicitly requests that operation.
- Report the commit identifier and validation outcome in the final hand-off.

## Maintaining these instructions

- When the user provides a new permanent, reusable project instruction, update this `AGENTS.md` instead of duplicating it across unrelated Markdown files.
- If a permanent rule requires a specialised normative specification, keep its concise cross-cutting rule and link here, then maintain the detailed contract in its single owning document.
- Do not create a lowercase `agents.md` duplicate or another general-purpose instruction file.
- Before reorganising instruction files, audit the full active corpus, present a concise plan, preserve every still-valid rule, and distinguish authority from historical evidence.
- Do not remove, rename, or consolidate an instruction file until all relevant rules and inbound references are mapped and preserved, and the proposed strategy has been presented to the user.
- Record changes to the instruction system in `prompts/system/Prompt-System-Change-Log.md`; update current state or transition history only when the change is factual and belongs there.
