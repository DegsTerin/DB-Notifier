# STATE-06 Provider Identity Icons Report

## Authority and status

Bruno explicitly requested that database instances use the corresponding database identity, naming PostgreSQL and MongoDB as examples, and asked for an open solution covering every database the platform may accept. After the eleven-identity Skill Icons implementation and its local visual sample, Bruno issued the additional instruction: `Os icones que você não conseguiu encontrar busque em outros sites na internet`. That instruction authorised bounded internet research for exact missing identities. It did not waive licence/trademark review and did not authorise provider implementation, homologation, database access, monitoring, commands, deployment, lifecycle promotion or transition.

Status: alternative-source research `COMPLETE`; no candidate asset accepted and no product/asset expansion produced; the existing eleven-identity/22-variant baseline remains unchanged. Its restricted automatic Quality Gate and the documentary checks for this research record are `APPROVED`. The increment-specific Human Gate is `ACCEPTED WITH THE RECORDED LIMITATIONS`; `STATE-06 INTEGRATION` is unchanged.

## Current implemented outcome

DB-Notifier resolves provider identity from an open presentation-only registry on React and WPF. An exact registered image appears beside the unchanged provider identifier. Any unknown, future, malformed, missing or failed asset uses the neutral database outline while retaining the provider text.

The current demonstration fixture therefore presents:

| Provider identifier | Visual result | Product truth |
|---|---|---|
| `postgresql` | Exact registered PostgreSQL identity | Implemented; not homologated; public support remains No |
| `mysql` | Exact registered MySQL identity | Planned; not implemented |
| `mongodb` | Exact registered MongoDB identity | Planned; not implemented |
| `sql-server` | Neutral database fallback | Planned; not implemented; no approximate Azure identity substituted |

The global DB Notifier product mark, fleet aggregate icon, health glyphs, categorical chart colours and notification identity remain provider-neutral and separate from these decorative images.

## Registered asset coverage

The pinned Skill Icons revision continues to provide the following eleven registered database or data-platform identities and twenty-two Light/Dark variants:

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

No Firebird, OpenSearch or other new identity was added to the manifest, Web/WPF registries, source inventory or generated output by the alternative-source research.

## Alternative-source research result

The review treated three questions independently:

1. Is the artwork the exact database/provider identity rather than a related corporate, foundation or platform logo?
2. May the file itself be copied and redistributed with DB Notifier?
3. Does the mark policy or written permission allow this exact in-product use in DB-Notifier's current factual implementation/support context?

A positive answer about the file licence or download source does not answer the mark-use question. A future objective to support every database also does not create current implementation, support or mark permission.

| Provider identities | Research conclusion and resulting presentation |
|---|---|
| Firebird | Devicon offers an exact Firebird file under the Devicon MIT licence, but the official [Firebird brand FAQ](https://firebirdsql.org/en/firebird-brand-faq/) addresses a small logo beside a database backend the application supports. DB-Notifier does not currently implement, homologate or publicly support a Firebird provider. No asset was accepted; neutral fallback retained. |
| OpenSearch | The OpenSearch Project publishes exact official default/dark marks. Its [trademark and brand policy](https://opensearch.org/trademark-brand-policy/) permits the logo to indicate software or a service that uses OpenSearch and lists separate reference contexts; other logo uses require prior permission. DB-Notifier does not currently implement or integrate an OpenSearch provider, so the review did not establish a sufficiently clear basis for distribution in the current planned/unimplemented fixture context. No asset was accepted; neutral fallback retained. |
| Microsoft SQL Server and Azure SQL | Available Microsoft/Azure product artwork is subject to permission or use restrictions that do not provide a sufficiently clear basis for this in-product identity use; neutral fallback retained. |
| Oracle Database and SAP HANA | The accessible corporate marks are not exact database identities and their published policies require permission for the proposed use; neutral fallback retained. |
| IBM Db2 | Exact official artwork was found, but its published rights are restricted; neutral fallback retained. |
| MariaDB, Valkey, CockroachDB and ScyllaDB | No exact asset with sufficiently clear public permission for this in-product redistribution was verified; neutral fallback retained. |
| Couchbase, InfluxDB and Neo4j | Published mark terms require a separate written licence or permission for this use; neutral fallback retained. |
| Apache CouchDB | Apache artwork is available, but the Apache Software Foundation mark policy requires approval for the reviewed non-link graphical use; neutral fallback retained. |

Redis artwork is not used for Valkey, a general Azure mark is not used for SQL Server/Azure SQL, and a corporate logo is not used as an approximation of an engine identity. Every other unmapped provider remains fully representable through its visible identifier and the neutral database glyph.

## Existing provenance and offline generation

- Source collection: [Skill Icons](https://github.com/tandpfun/skill-icons) and its [public site](https://skillicons.dev/).
- Pinned revision: `7f7e691e71aec64e8354bf697835e009d1ad80f8`.
- Licence: MIT, Copyright (c) 2022 tandpfun; the full text is vendored and copied with Web/WPF distribution output.
- Source inventory: [`design-system/provider-icons/manifest.json`](../design-system/provider-icons/manifest.json), with upstream path plus recalculated Git blob SHA and local SHA-256 for every accepted SVG and the licence.
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
- Browser forced colours and Windows High Contrast hide multicolour provider artwork and use the neutral system-colour outline.
- Light/Dark changes re-resolve only presentation assets. Health, freshness, support, capability and homologation do not change.
- Provider distribution category slots cycle across the five neutral Design System tokens, preventing logo colours from becoming chart or status data.

## Verification evidence

The existing baseline evidence remains the previously recorded result and was not rerun for this documentation-only research conclusion:

| Baseline check | Previously observed result |
|---|---|
| Provider asset generation | `11` identities and `22` Light/Dark outputs generated and verified |
| Provider asset drift/provenance gate | Approved offline for the accepted Skill Icons inventory |
| Dashboard tests | `55/55` approved |
| .NET 10 solution Release build | `15` projects; zero warnings and zero errors |
| Unit tests | `304/304` approved |
| Architecture tests | `17/17` approved |

A preliminary local `node scripts/generate-provider-icon-assets.mjs --generate` invocation occurred before the final rights conclusion. It exited with code `1` before writing outputs because the provisional Firebird working-tree bytes did not match the declared raw upstream Git blob. This demonstrated the generator's fail-closed integrity boundary; the provisional source and all related code/documentation changes were then removed. After the candidates were rejected on their current mark-use conditions, no successful generation, raster, Dashboard, .NET, runtime or visual gate was applicable, and none was run as new evidence.

| Documentary record check | Observed result |
|---|---|
| Mandatory shutdown preflight | No DB-Notifier component or owned listener remained before the technical action; the audit shell itself was the only workspace-command-line match |
| Preliminary generator diagnostic | Failed closed before output with exit code `1` on the provisional Firebird Git-blob mismatch; no generated or product file remained changed |
| Scope/diff confirmation | Approved, exit code `0`; `.gitattributes`, notices, manifest, generated inventory, generator, product source and tests have no diff, and the final scope contains only six Markdown files |
| Temporary research cleanup | Four verified project-owned directories under `C:\tmp` were removed after the review; each contained only reproducible checkouts/downloads and all four absence checks returned `False` |
| Code-documentation and diff gates | `242` comment-capable source files approved; `git diff --check` approved, both exit code `0` |
| Local Markdown links | `342` local links in `83` files approved, exit code `0` |
| Secret scan | Current non-ignored worktree and available Git history approved without exposing values, exit code `0` |

## Limitations and residual review

- No real database, provider runtime, Agent, monitoring channel, credential, command or external infrastructure was used.
- The eleven registered identities are asset coverage, not the complete universe of databases and not a support catalogue. The neutral fallback is the universal compatibility mechanism.
- PostgreSQL remains unhomologated and publicly unsupported; all other displayed fixture providers remain planned and unimplemented.
- A rejected candidate may be reconsidered only after exact file redistribution and mark permission are established for the then-current factual product use. Research availability alone is not approval.
- The prior human comparison of Dashboard and WPF/Tray in Light, Dark and forced colours/High Contrast was the visual basis named in the explicit gate request. The alternative-source research added no visual surface and required no repeat sample.
- Regeneration remains pinned to one renderer version because Chromium antialiasing can change across versions. Ordinary CI verifies checked-in bytes and does not silently regenerate them.

## Human Gate decision

On 2026-07-18, after the gate summary identified the approved automatic evidence, the prior Dashboard/WPF/Tray Light, Dark and High Contrast sample, the unchanged eleven-identity/22-variant baseline and the recorded limitations, Bruno replied exactly: `ACEITO o incremento Provider Identity Icons no STATE-06, com as limitações registradas; não autorizo provider, banco externo nem transição de estado.`

This is an informed acceptance of the `Provider Identity Icons` increment only. It accepts the neutral fallback for every identity that lacks applicable asset/mark permission and preserves the distinction between visual coverage, provider implementation, homologation and public support. It does not authorise a provider, database access, monitoring, commands, infrastructure, runtime integration, deployment, release, lifecycle promotion or state transition.

## Gate classification and next decision

The restricted automatic Quality Gate for the existing eleven-identity implementation remains the previously recorded `APPROVED` result. This research creates no new product Quality Gate; its documentary checks are `APPROVED`. The increment-specific Human Gate is `ACCEPTED WITH THE RECORDED LIMITATIONS`. The Quality/Human Gate for leaving `STATE-06`, provider homologation, `STATE-07`, release and production remain `NOT EVALUATED`.

No further action is required for this increment. A future provider identity may be reconsidered only through a separately authorised increment that proves the exact asset, redistribution rights, applicable mark permission and current provider truth. This acceptance does not open or authorise that future work.
