# STATE-06 — Audit Remediation R6 Automatic-Phase Report

- Date: 2026-07-21
- Baseline: `a8d67e35af0be55c9d01a6141677b52fb0f23ed4`
- Lifecycle: `STATE-06 INTEGRATION`, unchanged
- Authorised findings: `AUD-H12`, `AUD-M12`–`AUD-M21`, `AUD-L01`
- Automatic-phase result: `APPROVED`
- Global gate: `NON-GREEN — PRE-EXISTING R0 ASSERTION ONLY`
- Physical Windows evidence: `NOT TESTED`
- Human decision: `PENDING — NOT AUTHORISED BY THIS PHASE`

## Authority and boundaries

R6 used only the separately authorised local automatic phase. The mandatory shutdown preflight observed the exact baseline, a clean initial worktree and zero DB-Notifier-owned process, listener, window or synthetic `ping.exe` residue. Builds and tests used existing local dependencies with `--no-restore` where applicable. The browser matrix used one hidden Chrome process with an isolated temporary profile and loopback-only preview; the runner proved bounded cleanup and deleted its owned profile and temporary directory.

No ordinary user browser/profile, visible WPF sample, Windows notification, Windows preference change, external access, restore, download, package installation, new dependency, database, provider, credential, secret, migration, schema or operational data was used. Normal SignalR, delivery, Agent Fleet, provider loading and packaging remain unavailable. No command, executor, AIOps mode, LLM, recommendation, automation, deployment or lifecycle transition was activated.

The final shutdown audit found a separate older project-dedicated Edge tree using the exact temporary profile `DBNotifier-Dashboard-Runner-28d57827b7d545608d766ac5a70d735b`. It was not the hidden Chrome process used by this R6 matrix and had appeared after the clean preflight. Under the mandatory project shutdown rule, only the 20 processes carrying that exact profile were stopped and only its validated temporary root was removed; no ordinary Edge profile or process was touched. The repeated final audit proved zero project-owned process, `ping.exe`, listener on the two R6 loopback ports or matching temporary directory.

## Finding disposition

| Finding | Implemented disposition | Automatic proof | Residual |
|---|---|---|---|
| `AUD-H12` | Disabled instances remain visible with their own count and text but are excluded from health, freshness, Tray aggregate and transition conclusions in C# and TypeScript. Persisted state is unchanged. | Unit and Dashboard tests cover disabled healthy, critical and stale inputs, filters and all-disabled fleets. | Visible human review is pending. |
| `AUD-M12` | A TV read captures `acceptedAt` with the accepted snapshot; the coordinator publishes both atomically and presentation uses no earlier clock. Future-skew and freshness limits are unchanged. | Reader/coordinator tests prove paired publication, 304 preservation, cancellation and non-overlap. | None within the automatic contract. |
| `AUD-M13` | C# and TypeScript reject empty, over-limit, untrimmed, control-character, Unicode-format/bidi and structurally invalid TV text before presentation. | Hostile-text matrices pass in both technologies. | The inventory remains limited to the authorised TV contract. |
| `AUD-M14` | The normative Design System now defines an authenticated, bounded and coalesced SignalR message only as a change hint that can request an earlier authoritative HTTP read. | Documentation gate and code contract tests pass. | SignalR remains absent from normal composition. |
| `AUD-M15` | Flyout refresh updates the semantic brand image from the same current aggregate as its textual summary. | WPF architecture contract verifies dynamic aggregate use; no visible flyout was opened. | Visible human review is pending. |
| `AUD-M16` | A read-only WPF adapter observes `SystemParameters.ClientAreaAnimation`; reduced motion changes the ComboBox popup to a static transition and never changes Windows configuration. | Build and architecture tests prove ownership, subscription, static fallback and cleanup. | Physical Windows reduced-motion behaviour is `NOT TESTED`. |
| `AUD-M17` | WPF and Tray use one UTC formatter; localised labels explicitly identify UTC. Dashboard and TV retain the existing system-local formatter that exposes the zone. | Localisation generation, system-time tests and WPF contracts pass in both languages. | Visible cross-surface review is pending. |
| `AUD-M18` | Authoritative TV evidence suppresses demonstration sparklines and trend graphics and presents explicit source-truth copy instead. | Dashboard source and behavioural tests pass. | No real authoritative source was used. |
| `AUD-M19` | Unknown health, event severity, alert state and capability state map to localised `Unknown`; raw machine values never become `Resolved`, `Information`, supported or healthy. | TypeScript normaliser tests and WPF source contracts pass. | None within the inventoried presentation enums. |
| `AUD-M20` | Eight textual WPF navigation glyphs were replaced by token-coloured code-native `Path` geometry. | WPF architecture test counts all eight paths and rejects the former glyphs. | Visible optical review is pending. |
| `AUD-M21` | The flyout is constrained to the current monitor working area, observes per-monitor DPI changes, repositions, reflows below 480 DIP and exposes bounded vertical scrolling. | WPF build and static architecture contracts pass without showing a window. | Physical 200% and mixed-DPI movement are `NOT TESTED`. |
| `AUD-L01` | The route owns the localised document title, full instance IDs are exposed with wrapping, and React/WPF provider registries are generated byte-for-byte from the single local canonical manifest. | Dashboard tests, provider-manifest verification and WPF architecture tests pass. | No provider, asset, licence or support claim was added. |

## Shared meanings and source truth

`Enabled=false` means present in inventory but excluded from every current-health conclusion. An all-disabled fleet is `Unknown`, never healthy. This meaning is shared by Application presentation, Dashboard, WPF and Tray. Disabled state is conveyed by text and count as well as neutral geometry, and it does not alter canonical persisted data.

The authoritative TV path continues to read immediately and reconcile serially 30 seconds after each completed read. A SignalR hint can only coalesce an earlier HTTP read. Demonstration charts are not rendered beside an accepted authoritative snapshot, and no real-time, provider-support or operational claim is introduced.

Provider identity paths are now derived from `design-system/provider-icons/manifest.json` by the existing local generator. Verification compares the generated TypeScript and C# registries byte for byte with deterministic expected content and continues to verify all 11 identities and 22 existing theme variants. No asset or licence changed.

## Automatic validation record

- .NET 10 Release solution build with `--no-restore`: 18 projects, zero warnings and zero errors.
- Dashboard type-check, 64/64 tests and Vite build: passed using already installed dependencies.
- R6-focused .NET presentation/TV tests: 63/63 passed.
- Complete unit suite under the coverage runner: 393/393 passed.
- WPF test assembly: 3/3 passed; Dashboard TV integration selection: 4/4 passed.
- WPF presentation architecture contracts: 11/11 passed.
- Coverage: 81.92% lines, 53.56% branches and all ten required components present.
- Hidden isolated Chrome `150.0.7871.129`: 96 viewport samples and 32 forced-colour route samples passed across pt-BR/en-GB and Light/Dark; the accessibility trees exposed 325–326 nodes with no gate failure.
- Provider registries/assets, localisation, Design System tokens and semantic brand deterministic verification: passed.
- Code-documentation gate: 316 source files passed.
- Markdown links: 565 local links in 123 files passed; secret scan passed.
- `dotnet format --verify-no-changes --no-restore`: initially identified only indentation in the changed flyout switch; after correction, the final gate passed.

The complete architecture command remains 43/44 because `State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources` still expects the fixed name `state05-dashboard-failure.json`. This is the exact accepted, pre-existing R0 contradiction. It was neither changed nor excluded from this report. The R6-specific WPF architecture selection is green.

The R5 NuGet metadata incident remains recorded. R6 did not execute the lockfile or live vulnerability gates, did not restore and did not access NuGet or any external source.

## Physical and human evidence boundary

Windows 200%, movement between mixed-DPI monitors, Narrator, physical High Contrast, physical reduced motion and a visible flyout were outside this authority and remain `NOT TESTED`. Modelled High Contrast, forced colours, reduced-motion source contracts, keyboard, focus, accessibility-tree semantics, zoom/reflow and representative viewports passed automatically. Modelled evidence is not substituted for physical evidence.

No Human Gate decision is inferred. A separately authorised visible review must be completed and decided before R6 can be accepted humanly. Until then, the automatic implementation is complete but the R6 human decision remains pending.

## Preserved containments

- R2-A command tombstones and `ExecutionPolicy.Never` remain unchanged.
- Agent Fleet normal and the operational certificate issuer remain unavailable.
- External delivery remains disabled and refused at startup; `UnavailableServerMessagePublisher` and zero real channel adapters remain normal.
- Dynamic provider loading and packaging remain blocked.
- The R5 NuGet incident and the global R0 assertion remain explicit and uncorrected.
- R2-B, R7–R8, R7-A0, O1, all AIOps modes, LLMs, recommendations, commands and automation remain outside this increment.

## Next decision

The next permissible step is not implementation. Bruno may separately authorise the bounded visible R6 samples named in the remediation plan. That review must use only dedicated owned runtimes, must not reuse the ordinary browser profile, must preserve physical limitations as `NOT TESTED` where hardware is unavailable, and must end with full cleanup before an explicit human decision is requested.
