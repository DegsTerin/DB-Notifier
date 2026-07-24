# MOD-12 O4 — Factual Read-only Observer Projection, API and UI Sandbox Report

## Decision summary

- Authorised baseline: `a7f70fe86e21050eba8b570b5ef0eb1184c91ff1`
- Automatic result: `APPROVED`
- Human acceptance: `PENDING`
- Visible human sample: `NOT AUTHORISED OR EXECUTED`
- Runtime boundary: exact test-only opt-in sandbox
- Inputs: accepted synthetic O2-A/O2-B and O3-A/O3-B paths only
- Production representativeness: `FALSE`
- Result authority: `FALSE`
- MOD-12 activation: `ActivationState=None`
- Normal Dashboard build O4 references: `0`
- Operational telemetry, corpus, provider, database or credential: `0`
- LLM, recommendation, command, automation or execution: `0`
- Lifecycle transition: `NOT AUTHORISED`

O4 adds a factual presentation boundary without activating `OBSERVER`. The sandbox executes the accepted synthetic
O2 pipeline and O3 corpus/policy/evaluation path, creates one in-memory provider-neutral projection, verifies it
before publication, exposes it through an authenticated read-only HTTPS loopback API and renders it in a dedicated
Dashboard composition. Nothing is registered in the normal Server API or normal Dashboard route registry.

The accepted O2 result contains one complete deterministic threshold signal but no complete deterministic forecast.
O4 therefore displays the current signal as `Stale` and the forecast as `Unknown`; it does not infer a prediction
from O3 synthetic holdout approval. This is the intended factual outcome, not a missing UI feature.

## Exact sandbox and composition boundary

The exact process marker is `o4-factual-observer-projection-sandbox`. The existing consolidated sandbox host delegates
only this marker, an exact generated Dashboard `dist` directory and a version-four local run identifier to O4.
The O4 host:

- binds Kestrel to an ephemeral HTTPS listener on `127.0.0.1`;
- uses one short-lived P-256 certificate and exposes only its non-secret SPKI pin to the dedicated browser;
- authenticates the fixed synthetic test subject only on HTTPS loopback;
- permits `GET /api/v1/observer-sandbox/projection`;
- rejects unauthenticated reads and exposes no write endpoint;
- serves only the dedicated build output;
- deletes its exact GUID-owned temporary O2/O4 root on shutdown.

The Dashboard O4 composition requires both the exact build value
`VITE_DB_NOTIFIER_OBSERVER_SANDBOX=local-test` and an HTTPS loopback origin. Its application, API adapter, messages and
CSS are dynamically imported only in that build. The ordinary build was inspected after compilation and contained
zero O4 marker, component, UI copy or CSS reference. `App.tsx`, the normal route registry, normal Server composition,
WPF and Tray remain unchanged.

## Projection and traceability

The projection binds the following content-addressed values:

- accepted O2 envelope digest;
- accepted O3 result digest and evaluation identifier;
- frozen O3 policy digest;
- O3 corpus manifest digest and monotonic revision;
- projection identifier and projection digest;
- complete signal evidence identifiers.

The payload also fixes `Synthetic=true`, `Operational=false`, `ProductionRepresentative=false`,
`IsAuthorising=false`, `Revoked=false`, `Superseded=false` and `ActivationState=None`. The API recomputes the canonical
projection digest and compares the exact expected O2/O3 trace before each response. Publication is refused when the
payload is incomplete, altered, expired, revoked, superseded, future-skewed or bound to another policy or corpus.

The browser accepts only the exact schema, bounded JSON and text, finite numeric values, UTC timestamps, known
limitations, valid evidence identifiers and the explicit non-authority flags. Both `Z` and `+00:00` are accepted as
equivalent UTC encodings and are normalised before presentation.

## Factual UI

The dedicated `Observer` surface presents:

- a persistent synthetic, non-operational and read-only boundary;
- `ActivationState=None`;
- current deterministic signals with state, rule, value, threshold, evaluation time, validity and evidence count;
- forecast availability as explicit `Unknown`;
- result-to-policy-to-corpus traceability;
- source, freshness, uncertainty and known limitations;
- an unavailable state that reveals no transport or integrity detail.

The surface has no recommendation, approval, command, execution or administrative control. It uses a typed
O4-Web-only pt-BR/en-GB catalogue so the prohibited WPF/Tray adapters remain byte-for-byte unchanged.

## Fail-closed evidence

| Scenario | Observed result |
|---|---|
| incomplete processing or empty signals | API/UI refusal |
| altered payload without matching digest | API refusal |
| expired projection | API/UI refusal |
| revoked or superseded projection | API/UI refusal |
| future-skewed projection | API/UI refusal |
| policy or corpus digest divergence | API refusal |
| incompatible schema or forecast state | UI refusal |
| hostile control or bidi text | UI refusal |
| non-finite numeric value | UI refusal |
| missing authentication subject | HTTP `401` |
| attempted write method | no write endpoint |
| non-HTTPS, non-loopback or approximate build activation | composition refusal |
| ordinary Dashboard build | zero O4 UI/module/style reference |

## Automated evidence

All checks ran locally and offline without restore, download, dependency changes or external access.

| Check | Result |
|---|---|
| focused O4 integration tests | `3/3 PASSED` |
| focused O4 architecture tests | `3/3 PASSED` |
| complete integration suite | `75/75 PASSED` |
| complete architecture suite | `74/74 PASSED` |
| complete WPF test suite | `10/10 PASSED` |
| complete unit suite | `399/399 PASSED` after one non-reproduced pre-existing synthetic process timeout |
| complete Release solution build | `PASSED`, zero warnings and zero errors |
| Dashboard tests | `72/72 PASSED` |
| Dashboard type-check and normal/O4 builds | `PASSED` |
| proportional .NET coverage | lines `81.98%`, branches `53.85%`, ten required components present |
| O4 browser matrix | `16/16 PASSED`: pt-BR/en-GB, Light/Dark, desktop/compact, effective 100%/200%/400% reflow |
| browser security | unauthenticated read denied, write denied, external origins `0` |
| accessibility | semantic tree, keyboard focus and forced colours model passed |
| sanitised screenshots | `16`, captured in memory and not retained |
| Node.js/npm baseline | Node.js `24.18.0`, npm `11.16.0` |
| tokens, localisation and provider assets | `PASSED` |
| code-documentation gate | `360` files, `PASSED` |

The first combined .NET run observed one timeout in the pre-existing synthetic
`PostgreSqlProviderTests.TimeoutAndCancellationTerminateSyntheticReadinessProcessTree` case. No O4 code was changed
for it. Both parameterised cases then passed in isolation and the complete `399/399` unit suite passed immediately
afterwards, so the event is recorded as non-reproduced environmental timing rather than hidden or corrected.

## Cleanup and preserved boundaries

- The browser audit used one dedicated Chrome process, isolated temporary profile and closed local proxy.
- A first successful visual matrix found a late Chrome file handle during cleanup and correctly failed the run.
- The runner was hardened to await its exact process tree and retry removal within a bounded two-second window.
- The subsequent full audit passed and removed the profile, host, listeners and GUID-owned temporary roots.
- No screenshot, corpus member, policy, projection, private key or credential remains.
- Package declarations, dependency lockfiles, normal composition, WPF and Tray are unchanged.
- `ObserverActivationState` still contains only `None`.

## Limitations and next gate

O4 proves only a local synthetic read-only presentation protocol. The synthetic O3 corpus is not representative of
production, and its retained availability false positive is not converted into forecast quality. The fixed local
header is sandbox authentication, not an operational identity design. Forced colours, zoom and accessibility were
modelled automatically; no visible human O4 sample, physical assistive technology or physical DPI condition was
authorised or tested.

The automatic O4 result is `APPROVED` only for the authorised sandbox. A separately authorised visible O4 sample and
an informed Human Gate are still required before O4 can be accepted. Acceptance would still not authorise normal
composition, real data, `OBSERVER`, O5, LLM, recommendations, commands, automation or any lifecycle transition.
