# DB-Notifier provider icon assets

This directory owns third-party visual identity assets used only beside an authoritative provider name. It does not define the provider catalogue, implementation, homologation or public support.

## Coverage and fallback

`manifest.json` maps the exact provider identifiers for which the pinned Skill Icons revision contains a corresponding asset. Every other valid provider identifier remains representable by the presentation contract and receives the DB-Notifier neutral database glyph plus its full text label. A related vendor, foundation or platform logo must never be substituted for a missing exact identity.

The registered baseline contains eleven identities and twenty-two Light/Dark variants: Cassandra, DynamoDB, Elasticsearch, Firebase, MongoDB, MySQL, PlanetScale, PostgreSQL, Redis, SQLite and Supabase. This is an asset inventory, not a closed provider or support catalogue.

The source SVGs are vendored under `skill-icons/icons/`. Web and WPF derivatives are generated offline by `scripts/generate-provider-icon-assets.mjs`; product runtime and ordinary builds never download an icon. The verifier applies a static-SVG allowlist, rejects active or external references and fails on unexpected source or derivative files. A pinned SVG may retain an upstream embedded PNG data reference only when its exact source hashes are approved and the canonical base64 value decodes to a bounded, structurally valid PNG.

## Provenance and licence

The source collection is [Skill Icons](https://github.com/tandpfun/skill-icons), pinned to commit `7f7e691e71aec64e8354bf697835e009d1ad80f8` and retrieved on 2026-07-18. Its MIT licence is preserved verbatim in `skill-icons/LICENSE`. The manifest records and the verifier recalculates the upstream Git blob SHA and local SHA-256 for every SVG and the licence.

Names and logos may also be trademarks of their respective owners. Their presence identifies fixture or provider data only; it does not imply affiliation, endorsement, implementation, homologation, health or public support.

## Alternative-source research and conservative fallback

The authorised internet research found candidate artwork for Firebird and OpenSearch, but neither asset was accepted for distribution. A file licence, an official download location and permission to use a mark in a particular context are distinct evidence. DB-Notifier registers an alternative asset only when all of the following are established for the intended factual use:

1. the artwork is the exact database/provider identity rather than a related corporate, foundation or platform mark;
2. the file may be copied and redistributed with the applications; and
3. the applicable trademark policy permits this product use in DB-Notifier's current implementation/support context, including required attribution and non-endorsement conditions.

The Devicon Firebird file is distributed under the Devicon MIT licence, while the [Firebird brand FAQ](https://firebirdsql.org/en/firebird-brand-faq/) addresses a small Firebird logo beside a database backend the application supports. DB-Notifier does not currently implement, homologate or publicly support a Firebird provider, so that candidate remains outside the asset set.

The OpenSearch Project publishes official default/dark marks. Its [trademark and brand policy](https://opensearch.org/trademark-brand-policy/) permits the logo to indicate software or a service that uses OpenSearch and lists separate reference contexts; other logo uses require prior permission. DB-Notifier does not currently implement or integrate an OpenSearch provider, so the research did not establish a sufficiently clear basis for distributing those marks in the current planned/unimplemented fixture context. OpenSearch therefore also retains the neutral fallback.

Microsoft SQL Server, Azure SQL, Oracle Database, SAP HANA, IBM Db2, MariaDB, Valkey, CockroachDB, Couchbase, Apache CouchDB, ScyllaDB, InfluxDB, Neo4j and every other unmapped provider retain the same fallback because the review did not establish the required combination of exact identity, redistribution and mark permission for this application. This conclusion may be reassessed if the factual implementation/support context or the owner's written permission changes; a future roadmap objective alone is not sufficient.

## Maintenance

1. Verify the exact upstream identity, file redistribution terms and separate trademark conditions for the current factual product use.
2. Pin a reviewed upstream revision and add the source SVG without changing its visual geometry or colours.
3. Extend the presentation-only manifest and both local client registries; do not add engine conditionals to Domain or Application.
4. Run `node scripts/generate-provider-icon-assets.mjs --generate` only with the manifest-pinned rasteriser, then run the same command with `--verify`.
5. Review Light, Dark and High Contrast behaviour. High Contrast must use the neutral system-colour fallback.

A provider icon change never advances a lifecycle state or provider support status by itself.
