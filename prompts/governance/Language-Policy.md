# DB-Notifier — Language Policy

- Status: normative thematic authority
- Revision: `1.0.0`
- Introduced in instruction corpus: `6.3.0`
- Project: `DB-Notifier`

This document was adapted in place from the source supplied by the owner. Its
SHA-256 before adoption was
`E6021618DD2DED951FC08BA5305C13F35163E86623C612406CBA2239D8E97224`.

## Purpose, authority and boundaries

This document is the single thematic authority for:

- communication with the owner;
- the language of new project-owned artefacts;
- mandatory external naming conventions;
- preservation of existing and historical content;
- the separation between engineering language and user-interface language.

[`Governance.md`](Governance.md) remains the owner of authority, controlled
execution and lifecycle.
[`Conversation-Coordination-Prompt.md`](Conversation-Coordination-Prompt.md)
remains the owner of hand-off semantics, field order, routing and parallelism
enums. [`Templates.md`](../templates/Templates.md) owns reusable formats, and
[`Quality-Gates.md`](Quality-Gates.md) owns verification criteria.
[`Code-Documentation-Standards.md`](../../docs/Code-Documentation-Standards.md)
owns structural requirements, completeness and narrow exceptions for code
documentation. The
[`DB-Notifier Design System`](../../docs/design/DB-Notifier-Design-System.md)
and separately authorised product decisions own supported interface locales
and localisation behaviour.

Where the rules below overlap, apply them in this order:

1. owner-facing communication uses `pt-BR`;
2. mandatory external names and exact technical identifiers remain unchanged;
3. a limited amendment preserves the established language of the existing
   file;
4. a new standalone project-owned artefact uses `en-GB`;
5. user-interface language remains a separate product decision.

## Owner communication

- Always communicate with the owner in Brazilian Portuguese (`pt-BR`).
- Questions, explanations, progress updates, approvals, warnings, hand-offs
  and ready-to-copy messages must use `pt-BR`.
- Present owner-facing labels, reasons, default values and guidance in
  `pt-BR`.
- Canonical field names, commands, paths and enums may remain in English
  inside backticks or parentheses only when technically necessary. The
  surrounding label, value and explanation remain in `pt-BR`.
- Preserve exact quotations in their source language and identify them as
  quotations when needed for evidence.

## Project artefacts

Write new standalone project-owned artefacts in British English (`en-GB`),
including:

- source code and project-owned identifiers;
- comments, docstrings and code documentation;
- technical and public documentation;
- README files;
- API and configuration descriptions;
- test names and descriptions;
- logs and technical error descriptions;
- commit messages when a Git operation has been separately authorised.

Use British spelling in project-owned prose.

New or modified comments and code documentation remain subject to the
mandatory `en-GB` code-documentation standard. Existing comments outside that
standard are not authority to add more non-compliant content and are not
translated globally without a separately authorised migration.

Technical logs and diagnostic descriptions use `en-GB`. Text intentionally
presented by the user interface follows the active, separately governed
localisation catalogue instead.

## External conventions

Preserve mandatory names and spellings imposed by:

- programming languages;
- frameworks and libraries;
- protocols and standards;
- external APIs;
- third-party products.

Do not translate or rename external contracts merely to apply `en-GB`.
Stable keys, reason codes, route identifiers and machine-readable values remain
unchanged unless their owning contract authorises a migration.

## Existing content

- Do not automatically translate or rewrite existing documentation, source
  history or historical evidence.
- Preserve the established language of an existing file when making a limited
  amendment, avoiding mixed-language documents.
- Preserve exact decisions, quotations, identifiers and evidence in their
  original form.
- A full language migration requires a separately authorised and planned
  change.
- Never rewrite Git history to translate previous commit messages.

## User interface

Treat the user-interface language as a separate product decision.

Do not infer the interface language from:

- the conversation language;
- the engineering language;
- the documentation language.

Localisation catalogues, fixtures and user-visible samples may contain the
authorised locale they implement or verify. This does not change the language
required for source comments, technical diagnostics or new engineering
artefacts.

## Governance

This policy defines language conventions only. It does not authorise:

- implementation;
- documentation migration;
- lifecycle transitions;
- Git operations;
- external actions;
- releases or deployments;
- changes to supported interface locales.

Language compliance must be included in the applicable project Quality Gates.
Any change to this authority requires the instruction-maintenance process in
[`Start-Here.md`](../Start-Here.md), factual changelog and state/history
updates only after the authorised change occurs, and preservation of existing
evidence.
