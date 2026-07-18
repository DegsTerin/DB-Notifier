# STATE-06 Provider Identity Icons Report

## Authority and status

Bruno explicitly requested that database instances use the corresponding database identity, naming PostgreSQL and MongoDB as examples, and asked for an open solution covering every database the platform may accept. This authority covers the local presentation increment, acquisition of the requested public static assets, offline generation, tests and factual documentation. It does not authorise provider implementation, homologation, external database access, monitoring, commands, deployment, lifecycle promotion or transition.

Status: `TECHNICALLY COMPLETE`; restricted automatic Quality Gate `APPROVED`; increment-specific Human Gate `PENDING`; `STATE-06 INTEGRATION` unchanged.

## Outcome

DB-Notifier now resolves provider identity from an open presentation-only registry on React and WPF. An exact registered image appears beside the unchanged provider identifier. Any unknown, future, malformed, missing or failed asset uses the neutral database outline while retaining the provider text.

The current demonstration fixture therefore presents:

| Provider identifier | Visual result | Product truth |
|---|---|---|
| `postgresql` | Exact registered PostgreSQL identity | Implemented; not homologated; public support remains No |
| `mysql` | Exact registered MySQL identity | Planned; not implemented |
| `mongodb` | Exact registered MongoDB identity | Planned; not implemented |
| `sql-server` | Neutral database fallback | Planned; not implemented; no approximate Azure identity substituted |

The global DB Notifier product mark, fleet aggregate icon, health glyphs, categorical chart colours and notification identity remain provider-neutral and separate from these decorative images.

## Registered asset coverage

The pinned Skill Icons revision contains exact presentation assets registered for the following eleven database or data-platform identities:

| Provider identifier | Display identity | Theme sources |
|---|---|---|
| `cassandra` | Apache Cassandra | Light and Dark |
| `dynamodb` | Amazon DynamoDB | Light and Dark |
| `elasticsearch` | Elasticsearch | Light and Dark |
| `firebase` | Firebase | Light and Dark |
| `mongodb` | MongoDB | One upstream asset reused in both themes |
| `mysql` | MySQL | Light and Dark |
| `planetscale` | PlanetScale | Light and Dark |
| `postgresql` | PostgreSQL | Light and Dark |
| `redis` | Redis | Light and Dark |
| `sqlite` | SQLite | One upstream asset reused in both themes |
| `supabase` | Supabase | Light and Dark |

Skill Icons does not provide an exact asset in this reviewed catalogue for every target engine named by DB-Notifier. MariaDB, Microsoft SQL Server/Azure SQL, Oracle, SAP HANA, IBM Db2, Valkey and every other unmapped provider remain fully representable through the neutral fallback and visible identifier. Redis artwork is not used for Valkey, and a general Azure mark is not used for SQL Server. Adding an asset never adds a provider implementation or support claim.

## Provenance and offline generation

- Source collection: [Skill Icons](https://github.com/tandpfun/skill-icons) and its [public site](https://skillicons.dev/).
- Pinned revision: `7f7e691e71aec64e8354bf697835e009d1ad80f8`.
- Licence: MIT, Copyright (c) 2022 tandpfun; full text is vendored and copied with Web/WPF distribution output.
- Source inventory: [`design-system/provider-icons/manifest.json`](../design-system/provider-icons/manifest.json), with upstream path plus recalculated Git blob SHA and local SHA-256 for every SVG and the licence.
- Maintenance contract: [`design-system/provider-icons/README.md`](../design-system/provider-icons/README.md) and [`THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md).
- Generator/verifier: `scripts/generate-provider-icon-assets.mjs`; normal builds and product runtime perform no network request.
- Web uses local SVG mirrors. WPF uses checked-in 64×64 PNG derivatives rendered offline by the manifest-pinned Google Chrome Headless `150.0.7871.125` in an isolated temporary profile.

Skill Icons is a third-party collection. This report does not claim that its files are original vendor distributions or that the MIT licence grants trademark rights. Names and marks remain the property of their respective owners; their use identifies provider data only and does not imply affiliation or endorsement.

## Implementation boundaries

- `providerType` remains an open canonical string; no engine enum or provider-specific branch was added to Domain, Application or API.
- Both client registries contain literal local paths. Provider input is never interpolated into a URL, file path or pack URI.
- The asset gate accepts only internal SVG fragments and reviewed embedded PNG data; it rejects scripts, active or external references, style sheets, entities and unexpected derivative files.
- React images use empty alternative text and an inaccessible decorative wrapper; the visible identifier supplies the accessible identity.
- WPF uses a decorative `Image` subclass with no Automation peer and retains the visible identifier in a text element.
- WPF provider cells preserve their original `ProviderType` sorting contract and constrain long identifiers so text trimming remains effective beside the image.
- Browser forced colours and Windows High Contrast hide multicolour provider artwork and use the neutral system-colour outline.
- Light/Dark changes re-resolve only presentation assets. Health, freshness, support, capability and homologation do not change.
- Provider distribution category slots cycle across the five neutral Design System tokens, preventing unstyled sixth-and-later categories without treating logo colours as chart data.

## Verification evidence

| Check | Observed result |
|---|---|
| Mandatory shutdown preflight | No DB-Notifier process, window or owned listener matched before the technical action |
| Provider asset generation | `11` identities and `22` Light/Dark outputs generated and verified |
| Provider asset drift/provenance gate | Approved offline; strict SVG reference policy, exact derivative-directory contents, source/output hashes, PNG signatures/dimensions and licence copy matched |
| Representative raster inspection | PostgreSQL Light/Dark, MongoDB and Firebase showed complete 64×64 artwork after correcting an initial cropped raster attempt |
| Dashboard TypeScript check | Approved |
| Dashboard tests | `55/55` approved |
| Dashboard production build | Approved |
| .NET 10 solution Release build | `15` projects; zero warnings and zero errors |
| Unit tests | `304/304` approved |
| Architecture tests | `17/17` approved, including local assets, exact lookup, fallback, High Contrast and brand/status separation |
| .NET format/analyzers | Approved with no changes required |
| Code documentation | `242` comment-capable source files approved |
| Markdown links | `339` local links in `83` files approved after this report and the append-only entry were added |
| Secret scan | Current non-ignored worktree and available Git history approved without exposing values |

## Limitations and residual review

- No real database, provider runtime, Agent, monitoring channel, credential, command or external infrastructure was used.
- The eleven registered identities are asset coverage, not the complete universe of databases and not a support catalogue. The neutral fallback is the universal compatibility mechanism.
- PostgreSQL remains unhomologated and publicly unsupported; all other displayed fixture providers remain planned and unimplemented.
- Full human comparison of Web and WPF Light, Dark, forced colours/High Contrast and 200% scaling has not yet been accepted for this increment. Automated checks and direct raster inspection do not replace that Human Gate.
- Regeneration is intentionally pinned to one renderer version because Chromium antialiasing can change across versions. Ordinary CI verifies checked-in bytes and does not silently regenerate them.
- Exact trademark and redistribution review may need to be repeated before public packaging or release; this local increment is not release approval.

## Gate classification and next decision

The restricted automatic Quality Gate is `APPROVED` for this local presentation increment only. The increment-specific Human Gate remains `PENDING`; the Quality/Human Gate for leaving `STATE-06`, provider homologation, `STATE-07`, release and production remain `NOT EVALUATED`.

Bruno should review the provider identities in a separately authorised local visual sample and then respond with one explicit decision for this increment: accept with the limitations recorded, request a specifically bounded remediation, or reject. No decision about these icons authorises a new provider, monitoring, external access, lifecycle promotion or transition.
