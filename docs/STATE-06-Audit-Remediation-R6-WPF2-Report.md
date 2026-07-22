# STATE-06 — R6-WPF2 Automatic Remediation Report

## Decision summary

- Authorised baseline: `8e595de369a77aa0f534356ab618d0222e71d583`
- Scope: structural and informational visual parity between the secondary WPF shell and the existing Web Dashboard
- Normative visual reference: Web Dashboard at the authorised baseline
- Design System contract: `3.2.0`
- Implementation state: `COMPLETE`
- Automatic result: `APPROVED` within the authorised local R6-WPF2 scope
- Focused commit: this report is committed with the completed increment; the identifier is recorded in the final hand-off
- Human sample `R6-HV-W01`: `REJECTED`; no automatic result may replace that decision
- `R6-HV-W02`: `BLOCKED`
- `R6-HV-P01`: `NOT TESTED`

The authorised offline implementation and automatic evidence are complete. This result does not approve `R6-HV-W01`, accept R6, resume W02/P01 or change lifecycle state. A new visible W01 repetition still requires a separate proposal and explicit authorisation.

## Authority and boundaries

R6-WPF2 may change only the WPF Presentation surface, presentation models needed to expose existing deterministic fixture data, existing generated Design System/localisation inputs where indispensable, directly related tests, the existing WPF auditor and factual documentation. The Web implementation is an immutable reference for this increment; its routes, visual structure, data contracts, fixtures and behaviour must remain unchanged.

The required parity concerns recognisable component structure, information, hierarchy, icons, state semantics and organisation. It does not require pixel-identical rasterisation. WPF retains its native caption, application scrollbars, Per-Monitor V2 DPI handling, work-area placement, focus model and window behaviour.

No new route, product capability, operational source, provider integration, notification, persisted product state or administrative authority is part of this increment. No dependency, restore, installation, download, external access, database, secret, migration, CI execution, push or deployment is authorised.

## Factual causes of the W01 rejection

The [R6-WPF1 report](STATE-06-Audit-Remediation-R6-WPF1-Report.md) addressed the ambiguous destination names and the high-level navigation semantics. The separately authorised [visible repetition](STATE-06-Audit-Remediation-R6-HV-W01-Repetition-Report.md) then established that this narrower interpretation was insufficient. The human decision remains exactly:

`AMOSTRA R6-HV-W01 REPETIDA — REPROVADA: o WPF ainda não reproduz a organização visual do Web; faltam ícones e componentes equivalentes e o gráfico apresenta apenas linhas sem eixos, rótulos e estrutura informativa`

Baseline inspection identifies the corresponding structural causes:

- WPF summary cards exposed labels and values but did not consistently include the equivalent semantic icon treatment used by Web;
- WPF instance, alert and provider regions used reduced or generic presentation instead of the Web field grouping, status pills, semantic icon badges, separators and generated provider identities;
- the WPF provider surface listed counts but omitted the Web distribution-ring composition;
- the WPF performance surface drew simplified lines and partial grid geometry without the percentage axis, time labels, explicit demonstration badge and complete plot margins of the Web component;
- the existing WPF auditor was not yet a route-by-route, locale/theme/size parity matrix, so it could not prevent these omissions.

These are presentation defects only. They do not indicate a data-contract, provider, delivery or operational-authority change.

## Classification method

Each correspondence is assigned one of the classifications required by Design System `3.2.0`:

- **Required equivalent** — structure, information, meaning and accessible outcome must correspond.
- **Justified native adaptation** — native presentation may differ while preserving the complete field set, reading order and outcome.
- **Web-only** — browser/Dashboard presentation with no authorised WPF counterpart.
- **Desktop-only** — Windows/notification-area presentation with no authorised Web counterpart.
- **Divergent — R6-WPF2 correction target** — the authorised baseline is missing or materially reduces a required correspondence. It may become **Divergent — corrected by R6-WPF2** only after the applicable automatic evidence passes.

The final classifications below reflect the completed automatic evidence. Human visual acceptance remains deliberately separate.

## Cross-surface shell inventory

| Component correspondence | Web reference | Required WPF outcome | Baseline classification | R6-WPF2 evidence |
|---|---|---|---|---|
| Product mark and TopBar | Canonical aggregate mark, product name, demonstration truth, language/theme controls | Same identity, hierarchy, truth and code-native global controls | Required equivalent | Passed in both locales and themes across the executed matrix |
| Primary navigation | Eight destinations in stable order, each with icon, localised label and current-page state | Same order, icons, canonical labels, text/background/rail selection and separate focus indication | Required equivalent | Passed; route rail remains visible while keyboard focus is separately outlined |
| Page header | Eyebrow, one page title, description and factual freshness/source context | Same localised hierarchy and source truth | Required equivalent | Passed on all eight routes |
| Scenario selector | Presentation-only deterministic scenario selection | Same safe presentation outcome where exposed by the existing Desktop contract | Required equivalent | Passed without adding an operational source |
| Footer/status truth | No-external-connection and provider-support qualification | Same factual message without implying connectivity or homologation | Required equivalent | Passed on every executed route |
| Responsive shell | Browser breakpoints, reflow and document scrolling | DIP-aware reflow, controlled native scrolling and work-area containment | Justified native adaptation | Passed at `820×620` and `1180×760`; `1920×1080` was not tested because the work area is `1920×1032` |
| TV, Fullscreen, hash routing, tab synchronisation and browser forced-colours emulation | Dashboard/browser-specific behaviour | No WPF counterpart introduced by this increment | Web-only | Not applicable |
| Native caption, taskbar/window behaviour and application scrollbars | Not owned by the Web Dashboard | Windows-owned caption, semantic WPF scrollbars and native window commands | Desktop-only | Preserved as a justified Windows adaptation |
| Tray/flyout and notification-area-first entry | No Web counterpart | Existing secondary Windows entry remains isolated and non-operational | Desktop-only | Preserved; visible Tray/flyout sampling remains outside this automatic lot |

## Eight-route parity matrix

### Overview / Visão geral

| Component correspondence | Web reference outcome | Required WPF outcome | Baseline classification | R6-WPF2 evidence |
|---|---|---|---|---|
| Freshness and disabled summary | Exact timestamp with zone and disabled instances excluded from current health | Same factual text and ordering before the operational panels | Required equivalent | Passed; timestamp, zone and disabled exclusion remain visible |
| Four KPI cards | Total, Healthy, Warning and Critical cards with values and semantic icons | Same meanings, values, complete cards and code-native semantic icon badges | Divergent — corrected by R6-WPF2 | Passed with reusable `KpiCard` and `SemanticIcon` components |
| Fleet status rows | Provider asset/fallback, provider text, instance, status pill, latency and sparkline | Same field grouping, textual status, latency and decorative trend | Divergent — corrected by R6-WPF2 | Passed with registered provider assets, explicit status pills, latency and contained sparklines |
| Recent alert rows | Semantic event icon, instance, rule, exact time and safe Alerts link | Same information, icon meaning, separators and existing safe navigation outcome | Divergent — corrected by R6-WPF2 | Passed with event icon, instance, rule, UTC timestamp and safe existing route action |
| Performance panel | Section header, source badge, percentage/time axes, grid and two series | Shared complete code-native chart contract with contained plot margins | Divergent — corrected by R6-WPF2 | Passed with the shared complete chart component |
| Provider distribution | Ordinal ring, provider identities, visible identifiers and counts | Code-native ring plus equivalent legend, identities and counts | Divergent — corrected by R6-WPF2 | Passed with a code-native ring and generated provider identities |
| Primary/secondary panel layout | Two-column desktop grid that stacks at compact width | DIP-aware paired panels that stack in the same reading order | Justified native adaptation | Passed with DIP-aware stacking and bounded vertical scrolling |
| Authoritative TV trend suppression | TV sandbox hides demonstration graphics and explains absent authoritative trend | No TV mode or operational source added to WPF; WPF must not fabricate authoritative trend | Web-only | Not applicable |

### Instances / Instâncias

| Component correspondence | Web reference outcome | Required WPF outcome | Baseline classification | R6-WPF2 evidence |
|---|---|---|---|---|
| Six summary metrics | Total, Healthy, Degraded, Attention, Stale and Disabled with semantic icons | Same meanings, values and semantic icons | Divergent — corrected by R6-WPF2 | Passed; the `1180×760` layout uses two three-card rows to preserve complete labels |
| Inventory evidence | Instance, provider identity/text, support, environment, status and observed time | Same complete factual field set with disabled health separation | Required equivalent | Passed with bounded table-level horizontal scrolling where native width requires it |
| Provider identity | Registered local assets; neutral fallback only when unmapped, unavailable or High Contrast | Same registry, exact asset policy, visible provider text and neutral fallback | Divergent — corrected by R6-WPF2 | Passed for PostgreSQL, MySQL and MongoDB assets with neutral SQL Server fallback |
| Collection presentation | Responsive Web table/compact cards | Read-only WPF `DataGrid` with native keyboard and one table-level task-order stop | Justified native adaptation | Passed as a native read-only `DataGrid` with bounded scrolling |
| Search/status filtering and compact browser cards | Web route interaction and breakpoint alternative | No new Desktop filtering capability added in this visual-only increment | Web-only | Not applicable |

### Alerts / Alertas

| Component correspondence | Web reference outcome | Required WPF outcome | Baseline classification | R6-WPF2 evidence |
|---|---|---|---|---|
| Alert summary | Critical open, Active and Total visible cards with semantic cues | Same three values, labels and icon-bearing KPI treatment | Divergent — corrected by R6-WPF2 | Passed; Total remains neutral rather than implying health |
| Alert evidence | Severity, state, rule, instance, provider, summary and updated timestamp | Same complete factual fields and fail-closed unknown handling | Required equivalent | Passed with complete fields and dedicated Unknown semantics |
| Alert collection | Semantic cards with explicit headings and definition fields | Read-only WPF `DataGrid`/rows with status pills and equivalent reading order | Justified native adaptation | Passed as a bounded read-only native table |
| Navigation count | Active demonstration count in the Web navigation item | No new Desktop counter required by this increment | Web-only | Not applicable |
| Notification-area entry | No direct Web counterpart | Existing Tray/flyout shortcut remains Windows-specific and non-administrative | Desktop-only | Out of visible scope |

### Performance / Desempenho

| Component correspondence | Web reference outcome | Required WPF outcome | Baseline classification | R6-WPF2 evidence |
|---|---|---|---|---|
| Section truth | Title, description and explicit demonstration badge | Same title, description, accessible name and badge | Divergent — corrected by R6-WPF2 | Passed in both themes and locales |
| Chart structure | Two series, horizontal/vertical grid, `100%`/`50%`/`0%` labels and `09:50`–`10:15` labels | Same information in a contained code-native WPF component | Divergent — corrected by R6-WPF2 | Passed with complete axes, labels, grid and two series |
| Shared component use | The Overview and route use one chart contract | WPF Overview and route use the same component and series | Required equivalent | Passed; both surfaces instantiate `PerformanceChart` |
| Reflow | Responsive SVG/card containment | DPI-aware native measure/arrange without clipping or bidirectional scrolling | Justified native adaptation | Passed at both executable native sizes |
| Authoritative TV suppression | Demonstration chart replaced with factual unavailability in authoritative TV state | No TV or new authoritative source introduced in WPF | Web-only | Not applicable |

### History / Histórico

| Component correspondence | Web reference outcome | Required WPF outcome | Baseline classification | R6-WPF2 evidence |
|---|---|---|---|---|
| Event evidence | UTC-labelled time, severity, event, instance, provider and summary | Same exact fields, source truth and textual severity | Required equivalent | Passed with complete UTC-labelled evidence |
| Provider and severity presentation | Registered identity/fallback plus semantic status cue | Same identity policy and non-colour status meaning | Required equivalent | Passed with generated identities and explicit text/icon status |
| Collection presentation | Semantic ordered timeline | Read-only native `DataGrid` with equivalent order and keyboard semantics | Justified native adaptation | Passed as a bounded native table |
| Search and severity filter | Web-only route controls | No new Desktop filtering capability introduced in this visual-only increment | Web-only | Not applicable |
| Local History/Alerts shortcut | No direct Web counterpart | Existing notification-area shortcut remains Windows-specific | Desktop-only | Out of visible scope |

### Operational configuration / Configuração operacional

| Component correspondence | Web reference outcome | Required WPF outcome | Baseline classification | R6-WPF2 evidence |
|---|---|---|---|---|
| Destination and source truth | Canonical unambiguous title, non-secret fixture and not-persisted context | Same canonical label, page hierarchy and factual state | Required equivalent | Passed without changing route IDs or persistence |
| Non-secret configuration | Field, safe value and description with provider/instance context | Same data and provider identity without secret exposure | Required equivalent | Passed; secret scan remains clean |
| Capability truth | Action, capability ID, unsupported/unavailable/denied state and reason | Same fail-closed outcome; no executable operation | Required equivalent | Passed; no command or executor was introduced |
| Scenario and preview controls | Browser select and native modal dialogue | Native WPF selector, read-only grid and owned preview dialogue | Justified native adaptation | Passed as the existing Windows-native interaction |
| Administrative authority | No Start/Stop/Restart execution | Same explicit non-support and absence of authority | Required equivalent | Passed; execution remains unavailable |

### Providers

| Component correspondence | Web reference outcome | Required WPF outcome | Baseline classification | R6-WPF2 evidence |
|---|---|---|---|---|
| Distribution | Ordinal ring, identity legend and count per provider | Code-native ring with equivalent legend and counts | Divergent — corrected by R6-WPF2 | Passed with code-native ordinal geometry and visible counts |
| Exact identities | Registered PostgreSQL, MySQL and MongoDB assets; neutral SQL Server fallback in the fixture | Same exact assets and fallback decisions from one canonical manifest | Divergent — corrected by R6-WPF2 | Passed using compiled generated resources with safe neutral fallback |
| Catalogue | Provider identifier, explicit support declaration and read-only truth | Same information hierarchy and no support/homologation implication | Required equivalent | Passed without support or homologation claims |
| Catalogue layout | Responsive Web cards | WPF native wrapping/list layout preserving reading order and complete text | Justified native adaptation | Passed as a native wrapping/list composition |
| Unknown provider | Visible identifier beside neutral fallback | Same fail-closed fallback without engine-specific core logic | Required equivalent | Passed through the shared provider identity policy |

### Preferences / Preferências

| Component correspondence | Web reference outcome | Required WPF outcome | Baseline classification | R6-WPF2 evidence |
|---|---|---|---|---|
| Preference cards | Interface-preference and Windows-notification cards with semantic icons | Same two-part hierarchy, icons and factual unavailable integration state | Divergent — corrected by R6-WPF2 | Passed with two icon-bearing factual cards |
| Canonical destination | `Preferências` / `Preferences` in navigation and title | Same unambiguous generated localisation | Required equivalent | Passed in pt-BR and en-GB |
| Theme/language truth | Supported explicit language and Light/Dark preference | Same supported values and safe presentation semantics | Required equivalent | Passed in Light and Dark with byte-for-byte restoration |
| Browser persistence details | Versioned `localStorage` and tab synchronisation | No Desktop counterpart | Web-only | Not applicable |
| Windows preference details | No Web file counterpart | Versioned local preference file, native controls and byte-for-byte restoration during audit | Desktop-only | Passed; before/after SHA-256 values exactly match the authorised hash |

## Authorised implementation shape

The implementation is expected to consolidate only the reusable WPF components needed by the matrix: KPI card, status pill, semantic icon badge, instance/alert/provider rows, provider distribution chart, complete performance chart and section/card header treatment. They must consume existing generated tokens, local provider assets, localisation and deterministic fixtures. No external icon, graph library, package or new harness is permitted.

The Web source must remain unchanged. Stable route IDs, URLs, data contracts, fixture facts, persisted product state and operational behaviour must also remain unchanged.

## Automatic evidence register

| Gate | Required evidence | Current result |
|---|---|---|
| Baseline and scoped diff | Exact baseline, Web unchanged, only authorised WPF/tests/docs/auditor changes | `PASS` — baseline confirmed; Web, project manifests, dependencies and lockfiles have zero diff |
| WPF Release build | Offline `--no-restore`, zero errors and recorded warnings | `PASS` — solution Release build completed with 0 warnings and 0 errors |
| WPF/Presentation tests | Direct component, localisation, accessibility and presentation contracts | `PASS` — WPF 3/3 and focused Presentation 66/66 |
| Architecture tests | Provider-neutral boundaries, no dependency/harness expansion, Web immutability | `PASS (SCOPED)` — WPF contracts 14/14; the full suite is 46/47 with only the unchanged R0 assertion described below |
| Dashboard regression | Existing type-check, tests and build with installed dependencies only | `PASS` — exact Node.js 24.18.0/npm 11.16.0, type-check, 66/66 tests and production build |
| Route parity matrix | Eight routes × pt-BR/en-GB × Light/Dark × authorised dimensions where supported | `PASS` — 64/64 executable samples passed; 32 `1920×1080` samples are `NOT TESTED` for the physical reason below |
| Accessibility | Keyboard, focus, names/states, non-colour meaning, modelled High Contrast/reduced motion | `PASS` — component contracts and the route matrix preserve explicit text/icon/boundary semantics |
| Reflow and containment | `820×620`, `1180×760`, `1920×1080` where supported; bounded scrolling and no clipping/overlap | `PASS WHERE SUPPORTED` — `820×620` and `1180×760` passed; `1920×1080` is `NOT TESTED` because the active work area is `1920×1032` |
| Performance chart | Axes, time labels, grid, two series, badge, accessible name and shared Overview/route use | `PASS` — present and contained on Overview and Performance in every executed combination |
| Provider assets | Registered identities, neutral unknown/error/High Contrast fallback and visible text | `PASS` — deterministic asset gate reports 11 identities and 22 theme variants; WPF exact/fallback rendering passed |
| Generated sources and documentation | Tokens, localisation, provider registries, Design System, comments and links | `PASS` — all deterministic gates passed; 326 comment-capable files and 607 local Markdown links verified |
| Coverage, secrets and diff | Proportional coverage, sanitised secret scan and diff checks | `PASS` — 393 tests, 81.92% lines, 53.56% branches, 10 required components and clean secret scan |
| Preference restoration | Final SHA-256 exactly `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F` | `PASS` — before and after hashes are exact |
| Cleanup | Zero owned process, listener, profile, window, fixture or temporary directory | `PASS` — auditor reports zero process/state residue; all eight exact R6-WPF2 audit directories were removed |
| Focused commit | One local scoped commit after successful evidence | `PASS` — committed with this report; identifier recorded in the final hand-off |

The final WPF auditor report recorded `64` passed executable samples, `0` failed samples, `32` not-tested samples, exact preference restoration and cleanup success. The `32` not-tested entries are the eight routes across two locales and two themes at `1920×1080`; the active Windows work area measured `1920×1032`, and the authorisation prohibited changing host configuration. Automated captures for the executable sizes were inspected before their exact temporary directories were removed.

The ambient terminal initially exposed Node.js 22.23.1/npm 10.9.8. The project gate correctly refused that pair. All Dashboard evidence was rerun with the already-installed, exact Node.js 24.18.0/npm 11.16.0 toolchain by changing only the child-process environment; no installation, download or global toolchain change occurred.

The known global R0 assertion remains the single full-architecture-suite failure: `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources` expects the literal `state05-dashboard-failure.json`. It is not authorised for correction. The R5 NuGet metadata incident also remains recorded; this increment performed no restore, download or metadata access.

## Preserved containment and Human Gate

R2-A, R3, R4-A, R4-B, R5, the automatic R6 phase, R6-G1, R6-FC1 and the naming/navigation result of R6-WPF1 remain outside the behaviour changed by this increment. The approved D01, D02 and D03 human samples remain historically intact.

Normal delivery, Agent Fleet, SignalR, operational providers, dynamic loading, packaging, commands and execution remain disabled or unavailable as previously recorded. R2-B, R7–R8, R7-A0, O1, LLMs, recommendations, automation, mode promotion and lifecycle transition remain unauthorised.

`R6-HV-W01` remains `REJECTED` despite the approved automatic result. A new visible W01 repetition requires a separate proposal and explicit authorisation. W02 remains blocked, every P01 physical condition remains not tested, and R6 human acceptance remains pending.

## Completion disposition

Every authorised divergence is evidenced as corrected or retained as a justified native adaptation. The proportional gates passed, the Web reference and dependency manifests remain unchanged, the preference hash was restored exactly, cleanup proved zero owned residue and the completed increment is carried by one focused local commit. R6-WPF2 is therefore automatically complete within its restricted scope. This disposition is not human approval of W01 or R6 and does not authorise W02, P01, any operational capability or lifecycle progression.
