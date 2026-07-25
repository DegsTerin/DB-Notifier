# O5-R5-B — Test-only Physical Campaign Driver Report

## Disposition

- Date: 2026-07-25.
- Authorised baseline: `92044fcbac86ccc67bc6495eeb63a27b7ff7154d`.
- Automatic result: `APPROVED`.
- Physical O5-R5 campaign: not yet executed by this implementation gate.
- Lifecycle: `STATE-06 INTEGRATION` unchanged.
- MOD-12 activation: `ActivationState=None`.
- Restore, installation, download, external access and new dependency: not used.
- Product source and normal composition changes: none.

O5-R5-B supplies the missing physical entrypoint and eight bounded workloads identified by the
blocked O5-R5 repetition. It does not alter the frozen O5-R5-A protocol, its digest, the accepted
runner or any product assembly.

## Isolated entrypoint

The sole physical entrypoint belongs to `DBNotifier.IntegrationTests` and remains inert unless
`DBNOTIFIER_O5_R5_B_PHYSICAL_TEST_ONLY` contains the exact frozen digest:

`266B7A952DF1A46BEE4577894D0A9206D92917AC661E0E17F9052DE1EB415DD7`

Before constructing `O5R5DotNetMeasurementSource`, it validates:

- the exact opt-in digest;
- the project-owned temporary evidence destination;
- the repository-selected .NET SDK declaration;
- the positive installed-memory declaration;
- the unchanged protocol digest.

Ordinary test execution returns before physical-source construction. Architecture tests prove
that there is exactly one construction site and that marker, digest and output checks precede it.

## Bounded workload matrix

The driver creates exactly `560` serial scenarios:

- eight frozen phases;
- `Cold` and `Warm` data-state classifications;
- five retained warm-up samples per phase/temperature;
- thirty measured samples per phase/temperature.

Every scenario declares the unchanged O1 maximum of `100,000` deterministic work units and
`524,288` accounted-memory bytes. Inputs do not exceed `65,536` bytes and item collections do not
exceed `256` items.

| Phase | Bounded physical operation |
|---|---|
| first-byte | reads one byte from the maximum accepted input |
| idle | observes a fixed 10 ms idle interval while retaining the input reservation |
| cancellation | observes an isolated inner cancellation after 10 ms |
| control-update | performs exactly 100,000 in-memory atomic control updates |
| parse | performs exactly 100,000 UTF-8 JSON token reads |
| cryptography | performs exactly 100,000 allocation-free SHA-256 block operations |
| sort | performs exactly 100,000 deterministic compare-and-swap units over 256 items |
| analysis | performs exactly 100,000 finite numeric accumulation units over 256 items |

`Cold` creates fresh bounded input state for each invocation; `Warm` reuses immutable prepared
state. Neither classification changes code, protocol limits or normal composition.

## Fail-closed evidence

The campaign stops on the first:

- refused or incomplete sample;
- sample threshold failure;
- phase/temperature summary failure;
- coefficient-of-variation failure;
- whole-campaign cancellation.

Complete and partial evidence use only phase labels, finite metrics, protocol identity, timestamps
and sanitised environment declarations. Output is atomically committed only inside an exact
project-owned temporary directory. Machine name, user name, paths, payloads, topology and secrets
are excluded. Physical evidence is explicitly non-authorising.

## Automatic validation

| Validation | Observed result |
|---|---|
| Focused O5-R5-A/B integration tests | `11/11` passed |
| Focused O5-R5-A/B architecture tests | `7/7` passed |
| Complete integration suite | `102/102` passed |
| Complete architecture suite | `86/86` passed |
| Complete unit suite | `401/401` passed |
| .NET 10 Release solution build | passed; zero warnings and zero errors |
| .NET format/analyser verification | passed |
| Code-documentation gate | passed for `375` source files |
| Markdown-link gate | passed for `776` links in `175` files |
| Design tokens, localisation and provider assets | passed |
| Focused secret scan | passed |
| Normal-source O5-R5-B references | `0` |
| Dependency and lockfile changes | `0` |

The selected tools were `.NET SDK 10.0.301`, `Node.js 24.18.0` and `npm 11.16.0`. Automatic
workload validation used only deterministic synthetic checkpoint sources; it did not instantiate
the physical counter source.

## Preserved boundaries

- `ActivationState=None` remains the sole activation state.
- Product source, Server composition, API, UI, WPF/Tray and configuration are unchanged.
- No provider, database, corpus, credential, telemetry, LLM, recommendation, command or
  automation was used.
- No package, project file, dependency or lockfile changed.
- The driver cannot evaluate or publish Observer results.
- O5-R6 remains outside this lot.

## Next action

The O5-R5-B implementation gate is automatically complete. Under the same explicit user
authorisation, the next action is the physical O5-R5 campaign using this exact committed driver,
the frozen protocol and the project-owned temporary evidence root. Any failed physical limit must
stop the campaign without tuning or correction.
