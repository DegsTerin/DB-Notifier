# ADR-0009 — Web, API, Data, and Linux Delivery Topology

- Status: proposed
- Date: 2026-08-28
- Revision: 1.0
- Owners: product, architecture, Web, API, data, platform and delivery
- Decision authority: explicit product-owner architecture review
- Implementation status: not authorised
- Lifecycle effect: none; `STATE-06 INTEGRATION` remains unchanged

## Context

At the frozen `main@0b09bd62863ad71fb1fcde48c6f47851b2ee0e69`
baseline, the Dashboard is an HTML5/CSS3, TypeScript and React single-page
application built with Vite. Its routing, environment configuration and parts
of its presentation lifecycle depend directly on browser APIs. The current
[Dashboard manifest](../../src/DBNotifier.Dashboard.Web/package.json) contains
React, TypeScript and Vite, but not Next.js.

The Server API is a C#/.NET 10 ASP.NET Core application with versioned REST
routes. PostgreSQL central persistence is already separated into its own
assembly under the accepted
[ADR-0004](ADR-0004-Persistence-And-Retention.md). The tracked baseline has no
internal Redis cache/session adapter and no GraphQL server or client package.

The repository currently contains no application Dockerfile and no Nginx
configuration. Docker is used only by bounded disposable database laboratories.
The tracked [GitHub Actions workflow](../../.github/workflows/ci.yml) delegates
the canonical repository gate to versioned scripts and adds a supplemental
Linux Dashboard lane. It is continuous-integration evidence, not a deployed
Linux topology or an automated delivery implementation.

The product owner requested a coherent revised direction limited to these Web,
API, data, cache/session, container, Linux, reverse-proxy and repository
automation technologies. This proposal must distinguish retained implementation
from conditional future components. Complexity alone is not an adoption
criterion.

## Decision summary

If this ADR is later accepted, the following dispositions will govern separate,
subsequently authorised increments:

| Area | Disposition | Boundary |
|---|---|---|
| HTML5, CSS3, TypeScript and React | Retain | Dashboard presentation and interaction |
| Vite | Retain | Dashboard development and static production build |
| Next.js | Defer | Adopt only after the objective entry criteria below pass |
| C#/.NET 10 and ASP.NET Core | Retain | Sole product-owned Server API and business-composition runtime |
| PostgreSQL | Retain | Sole durable source of truth for central product state |
| Redis cache | Conditional | Reconstructible Server-side read acceleration only |
| Redis session state | Conditional | Explicitly ephemeral Server-side state only |
| REST | Retain | Primary versioned API and durable interaction contract |
| GraphQL | Conditional and additive | Read-oriented Dashboard query composition only |
| Docker and Linux | Conditional delivery target | Central Web/API packaging, not a universal product claim |
| Nginx | Conditional edge component | Static Web serving and reverse proxy to ASP.NET Core |
| Git, GitHub and GitHub Actions | Retain and extend | Version control, review coordination and checked-in automation |

No conditional item becomes required, implemented, supported or operational by
accepting this proposal. Each item retains its own readiness, implementation,
verification and rollback evidence.

## Target central Web topology

```text
Browser
   |
   v
Nginx
   +-- static HTML5/CSS3/TypeScript/React artefacts
   |
   +-- /api/v1 --> ASP.NET Core Server API
                         |
                         +-- PostgreSQL (durable truth)
                         |
                         +-- Redis cache resource (conditional)
                         |
                         +-- Redis session resource (conditional and isolated)
```

The central Web topology is intentionally separable. It has two application
images: one Nginx image containing the Dashboard artefact and one ASP.NET Core
API image. The two images are independently versioned and have distinct health
boundaries; Dashboard artefact validation contributes to the Nginx image's
readiness rather than creating a third image. PostgreSQL and Redis are not
embedded into either application image. Components outside this central Web
topology retain their existing packaging decisions and are not reclassified by
this ADR.

## Frontend decision

The Dashboard retains semantic HTML5, generated and hand-written CSS3,
TypeScript, React and Vite. The production output is static and is served by
Nginx in the proposed Linux topology. `vite preview` remains a local preview
tool and is not a production server.

The existing Design System, accessibility, localisation, no-incorrect-theme-
flash behaviour, factual freshness and browser routing contracts remain
unchanged. A future framework migration must prove parity rather than treating
successful compilation as equivalence.

### Next.js entry criteria

Next.js remains deferred until a separately authorised decision package proves
at least one material requirement that the current React/Vite architecture does
not meet adequately:

1. server rendering or route pre-rendering provides a measured critical-path
   improvement;
2. public indexable pages become a product requirement;
3. route-level static generation or code splitting produces a measured,
   material loading improvement that cannot be achieved more simply in the
   retained stack; or
4. a product-owned server-side Web composition boundary is required and does
   not duplicate ASP.NET Core business rules.

The decision package must choose explicitly between static export and a Web
server runtime, record unsupported features, migrate Vite environment contracts,
preserve URLs and browser history, make browser-only code safe, retain the
pre-paint theme bootstrap, prove accessibility and visual parity, update CI and
provide a tested return to the retained Vite build. Until those conditions pass,
Next.js is neither a dependency nor a delivery requirement.

## Server and API decision

C#/.NET 10 and ASP.NET Core remain the only product-owned backend runtime in
this topology. Business rules, durable transactions and application use cases
remain behind Application ports; neither the Web build nor Nginx duplicates
them.

REST under a versioned route remains the primary contract for durable state,
ingestion, command resources, health and stable Dashboard projections. The
compatibility and idempotency requirements of accepted
[ADR-0003](ADR-0003-Agent-API-Protocol.md) remain intact. REST contract
documentation and contract tests take priority over adding another API surface.

### GraphQL entry criteria

GraphQL may be proposed only when representative measurements show all of the
following:

1. at least two consumers or Dashboard destinations need materially different
   shapes of the same connected data;
2. a critical journey requires at least three dependent REST reads or transfers
   at least 30% unused fields in two representative journeys;
3. a purpose-built REST projection cannot solve the case with lower operational
   and compatibility cost;
4. the use case is read-oriented composition rather than ingestion, commands,
   health or another durable state transition; and
5. one named owner and budget exist for schema evolution, cursor pagination,
   query depth/breadth/cost limits, batching, N+1 prevention, caching,
   observability, deprecation and contract tests.

If admitted, GraphQL is an additive query plane in the ASP.NET Core Server API.
It reuses Application use cases and PostgreSQL-backed projections. REST remains
available and authoritative for its existing responsibilities. GraphQL failure
must not interrupt REST ingestion, commands or health. A feature switch and
route removal must provide rollback without a data migration.

## PostgreSQL and Redis decision

PostgreSQL remains the sole durable source of truth. Redis is never an
authoritative database for DB-Notifier facts and is never part of the durable
commit boundary.

| Data class | Owning store | Redis use |
|---|---|---|
| Catalogue and versioned policy facts | PostgreSQL | Optional read-through copy |
| Observations, events and current accepted state | PostgreSQL | Optional timestamped projection |
| Query aggregates | PostgreSQL-derived | Optional bounded cache |
| Commands, audit, idempotency and outbox | PostgreSQL | Prohibited as authority |
| Server-side session state | None unless justified | Conditional and explicitly ephemeral |

An internal Redis cache/session role does not implement or homologate Redis as
a monitored database. The two responsibilities have separate ownership,
configuration and evidence.

### Cache consistency and adoption

The cache uses cache-aside semantics. A durable PostgreSQL transaction commits
first; the same durable boundary records the version or outbox event required
for later invalidation. A handler must not attempt an uncoordinated PostgreSQL
and Redis dual write.

Each cached value carries a schema version, logical key, source revision and
the factual timestamps needed to preserve freshness. An older invalidation or
refresh cannot replace a newer revision. A hard TTL never extends the product's
staleness boundary. Negative caching is short-lived and invalidated when the
corresponding durable record is created.

Redis cache adoption requires a reproducible benchmark that identifies a real
PostgreSQL/API bottleneck and a pre-declared improvement target. It also
requires bounded single-flight rebuilding, concurrency limits, TTL jitter,
maximum memory, an explicit eviction policy, capacity headroom and load/endurance
evidence. Without those results, the application reads PostgreSQL directly.

### Session-state criteria

Server-side session state is not a default requirement for the current SPA.
It is admitted only when a concrete multi-request flow needs ephemeral state
shared across more than one ASP.NET Core instance and a simpler stateless
contract is inadequate.

Session data must be safe to lose in full. Any value that must survive a store
loss, prove an action or participate in durable consistency belongs in
PostgreSQL. Session entries have independent idle and absolute TTLs, bounded
size and versioned serialisation. Sliding expiry cannot be unbounded.

Cache and session keys use separate namespaces for logical ownership and run on
independent Redis operational resources with separate endpoints, capacity
budgets and eviction policies. An alternative topology is admissible only if it
provides an equally enforceable resource boundary and proves that cache pressure
cannot evict session data silently. If the session store is unavailable, a
dependent flow returns an explicit transient failure. It must not fabricate a
new empty session and report success.

## Docker, Linux and Nginx decision

The first proposed Linux delivery slice contains two independent application
images:

1. a multi-stage ASP.NET Core API image whose final stage contains only the
   required .NET 10 runtime and published application; and
2. a multi-stage Dashboard image whose final stage contains Nginx and only the
   Vite production artefacts plus versioned Nginx configuration.

Base images are pinned by immutable digest through a separately governed update
process. Final images run as non-root, contain no build toolchain, have a
read-only root filesystem where compatible, expose explicit liveness/readiness,
honour graceful shutdown, declare bounded resources and write operational logs
to standard streams. A `.dockerignore` excludes repository-only and local
material from each build context.

Nginx serves immutable hashed assets with long-lived caching, serves HTML with
safe revalidation and proxies only the versioned API paths required by the Web
surface. The configuration preserves the request host and correlation context,
sets explicit payload and header limits, bounds connection/read/write timeouts,
handles compression deliberately and exposes no directory listing or default
site. Reverse-proxy behaviour, SPA fallback, limits, health and failure cases
require automated configuration and integration tests.

For every accepted long-lived transport under `/api/v1`, proxy tests must prove
transparent upgrade or streaming behaviour, transport-appropriate buffering
and timeouts, orderly connection shutdown and the documented fallback path.
This requirement preserves existing transport contracts; it neither reopens
nor expands them.

The existence of an image build does not prove Linux runtime support. That claim
requires repeatable image construction, Linux API/Dashboard checks, central
topology smoke tests, shutdown/cleanup evidence and rollback evidence.

## Git, GitHub and GitHub Actions decision

Git remains the source-history and local integration boundary. GitHub is the
selected future repository collaboration surface; this local baseline does not
prove a configured remote or hosted execution. The checked-in GitHub Actions
workflow orchestrates versioned scripts; business validation logic must not be
duplicated only in workflow YAML.

The current workflow remains CI. Future separately authorised increments may
add:

- Linux ASP.NET Core build and test evidence;
- the retained Dashboard production build;
- deterministic Docker image builds and metadata;
- image policy, vulnerability and SBOM checks;
- Nginx configuration tests;
- a bounded central-topology smoke test; and
- immutable build artefacts with provenance and retention rules.

A green workflow does not by itself perform or approve deployment. Publication,
promotion and environment mutation remain separate operations with their own
authority, gates, observability and rollback.

## Failure behaviour

| Failure | Required behaviour |
|---|---|
| Nginx unavailable | Central Web entry is unavailable; health evidence identifies the edge failure rather than reporting the API as healthy end-to-end |
| Dashboard artefact unavailable or invalid | Nginx readiness fails; no stale or partial asset set is promoted |
| ASP.NET Core unavailable | Nginx returns a bounded upstream failure; the UI preserves factual stale/unknown semantics |
| PostgreSQL unavailable | Durable reads/writes fail explicitly; Redis never fabricates truth or confirms a write |
| Redis cache unavailable | Bounded bypass to PostgreSQL with circuit breaking, backoff and stampede protection |
| Redis session unavailable | Session-dependent flow fails explicitly; no silent empty-session recreation |
| GraphQL query plane unavailable | Existing REST responsibilities remain available and independently observable |
| Docker image/configuration regression | Health/smoke gate stops promotion and retains the last known rollback artefact |
| GitHub Actions gate failure | No later promotion step is eligible; partial stage success is not an aggregate pass |

## Observability

The target topology records metrics, structured logs and traces by component:

- Nginx: request counts, status classes, upstream failures, latency, active
  connections and rejected limit events;
- ASP.NET Core: route duration, error/outcome class, cancellation, dependency
  latency and correlation;
- PostgreSQL: pool saturation, query/transaction duration, timeout, contention
  and durable outbox lag;
- Redis cache: hit/miss, get/set/delete duration, timeout, fallback,
  invalidation lag, rebuild, stale serve, key count, bytes, expiry and eviction;
- Redis session: create/load/commit duration, active/expired/failed sessions and
  store-loss outcomes;
- Docker/Linux: image revision, health transitions, restart count and bounded
  CPU/memory/filesystem use; and
- GitHub Actions: immutable source revision, job/stage disposition, duration and
  sanitised artefact identity.

Telemetry records logical operation and outcome classes, not full cache keys,
session payloads, database values or confidential request bodies. Alerts and
runbooks need explicit owners and thresholds before operational adoption.

## Rollback

- React/Vite remains the working baseline. A future Next.js increment retains a
  proved Vite build and route/visual parity evidence until the new path passes
  its own decision and rollback gate.
- Redis cache is disabled through a Server-side feature switch and falls back
  to bounded PostgreSQL reads. Versioned namespaces allow an incompatible cache
  to be abandoned without data migration.
- Redis sessions may be disabled only with an explicit user-impact decision;
  all affected sessions end safely and no durable fact is lost.
- GraphQL is additive and removable behind a route/feature switch; REST and
  PostgreSQL contracts remain unchanged.
- Docker/Nginx rollback selects the prior immutable image and configuration
  pair, then runs health and smoke checks. It never rolls back PostgreSQL as an
  incidental side effect.
- GitHub Actions changes are reverted as versioned repository changes. A failed
  workflow never rewrites historical evidence or promotes its artefacts.

## Alternatives

### Adopt every named component immediately

Rejected. It adds dependencies and operational surfaces without measured need
and would misstate the current implementation.

### Migrate the current Dashboard to Next.js now

Deferred. The current product is a browser-driven operational SPA, and no
accepted requirement currently offsets the migration and additional runtime
cost.

### Use Redis as a primary or durable store

Rejected. It conflicts with ADR-0004, weakens durable evidence and couples
correctness to cache availability.

### Replace REST with GraphQL

Rejected. Durable interactions already use versioned REST contracts; an
optional query surface must not replace them.

### Package the whole product as one container image

Rejected. It merges release, health, scaling and rollback boundaries and cannot
represent components outside the central Web topology correctly.

### Serve the production Dashboard through the Vite preview server

Rejected. The preview server is a local inspection tool, not the retained
production edge.

## Consequences

- The implemented baseline remains simpler: React/Vite, ASP.NET Core,
  PostgreSQL and REST.
- Redis, GraphQL, Docker/Linux/Nginx delivery and Next.js remain independently
  reversible and evidence-gated.
- The central Web surface gains a coherent Linux target without claiming that
  every DB-Notifier component has the same deployment shape.
- PostgreSQL load remains the baseline until measurements justify caching.
- Adding GraphQL or a server-rendered Web framework later carries explicit
  ownership, observability, compatibility and operational cost.
- More CI evidence is required before Linux packaging or delivery can be
  described as supported.

## Acceptance conditions

This ADR may move from `proposed` to `accepted` only when:

- the product owner explicitly reviews this exact revision and records one
  unambiguous architectural decision;
- architecture, Web, API, data and platform reviews find no unresolved `P0` or
  `P1` issue;
- the proposal is reconciled with accepted ADR-0001, ADR-0003, ADR-0004 and
  ADR-0005 without silently superseding them;
- the retained and conditional dispositions, objective entry criteria, failure
  behaviour, observability and rollback boundaries are accepted explicitly;
- the applicable documentation, link, language, secret and diff checks pass;
  and
- the decision record states that acceptance constrains future increments but
  authorises no implementation, dependency, configuration, migration, runtime,
  external operation or lifecycle change.

Acceptance alone does not satisfy the entry criteria for Next.js, Redis,
GraphQL, Docker/Linux/Nginx delivery or expanded GitHub Actions. Each requires
a separately authorised, bounded increment with its own evidence.

## Negative scope

This documentary proposal does not:

- change source, tests, scripts, workflows, configuration, manifests, lockfiles,
  schemas or migrations;
- add a dependency, image, container, Nginx configuration, Redis instance,
  GraphQL endpoint or Next.js application;
- run a build, test suite, product runtime, database, container or workflow;
- change current routes, persistence, caching, session behaviour, UI behaviour,
  package layout or operational topology;
- alter any accepted ADR, current factual state, historical evidence, Human
  Gate, activation state or lifecycle position;
- claim implementation, homologation, production readiness, operational support
  or public support for a conditional component;
- deploy, publish, promote, push or mutate an external resource; or
- evaluate or select any technology outside the exact set named in this ADR.

## References

- [ADR-0001 — Runtime Stack and Incremental Migration](ADR-0001-Runtime-Stack-And-Migration.md)
- [ADR-0003 — Agent/API Protocol and Compatibility](ADR-0003-Agent-API-Protocol.md)
- [ADR-0004 — Persistence, Migrations, and Retention](ADR-0004-Persistence-And-Retention.md)
- [ADR-0005 — Packaging, Signing, and Updates](ADR-0005-Packaging-Signing-And-Updates.md)
- [Vite static deployment guidance](https://vite.dev/guide/static-deploy.html)
- [Next.js SPA guidance](https://nextjs.org/docs/app/guides/single-page-applications)
- [Next.js deployment modes](https://nextjs.org/docs/app/getting-started/deploying)
- [ASP.NET Core distributed caching](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0)
- [ASP.NET Core session state](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-10.0)
- [GraphQL performance guidance](https://graphql.org/learn/performance/)
- [GraphQL pagination guidance](https://graphql.org/learn/pagination/)
- [Docker multi-stage builds](https://docs.docker.com/build/building/multi-stage/)
- [Docker build best practices](https://docs.docker.com/build/building/best-practices/)
- [Nginx reverse proxy guidance](https://docs.nginx.com/nginx/admin-guide/web-server/reverse-proxy)
- [GitHub Actions continuous integration](https://docs.github.com/en/actions/get-started/continuous-integration)
