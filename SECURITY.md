# Security Policy

<!-- Purpose: Defines safe vulnerability reporting for the DB-Notifier portfolio preview without implying production support. -->

## Project status

DB-Notifier is an evolving portfolio project in `STATE-06 INTEGRATION`. It has no production release, operationally homologated provider or supported hosted monitoring service. The public Dashboard preview uses deterministic synthetic data and does not connect to an Agent, API, identity provider or database.

## Reporting a vulnerability

Please do not disclose a suspected vulnerability in a public issue. Use GitHub's private vulnerability-reporting feature for this repository when it is available. If that feature is unavailable, open a minimal issue requesting a private reporting channel without including vulnerability details, secrets or personal data.

Include:

- a concise description of the affected component;
- the commit or version you reviewed;
- reproducible steps using synthetic data;
- the expected and observed behaviour;
- the likely impact; and
- a suggested mitigation, when known.

Do not include passwords, tokens, connection strings, private keys, certificates, personal data or details of a real database environment.

## Supported versions

No version is currently supported for production use. Security corrections target the latest public portfolio branch and are evaluated against the repository's current lifecycle and quality gates.

## Scope

Good-faith review of the source code and the static portfolio preview is welcome. Real database systems, third-party services, denial-of-service testing, social engineering and attempts to access another person's data are outside scope.

## Disclosure

Reports are assessed privately. A correction and factual advisory may be published after the issue is understood and a safe fix is available; no response-time or release-time guarantee is currently offered.
