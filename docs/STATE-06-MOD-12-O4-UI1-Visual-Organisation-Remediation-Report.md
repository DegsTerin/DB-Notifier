# MOD-12 O4-UI1 — Observer Visual and Organisational Remediation Report

## Decision summary

- Authorised baseline: `67e83bff61921387dbbe606499a8569bb4d11dd6`
- Remediation scope: dedicated O4 Web sandbox presentation only
- Automatic result: `APPROVED`
- Previous visible O4 samples: `FAILED`
- Repeated visible O4 samples after O4-UI1: `PENDING SEPARATE AUTHORISATION`
- MOD-12 activation: `ActivationState=None`
- Normal Dashboard build O4 references: `0`
- API or factual contract changes: `0`
- Dependencies or lockfile changes: `0`
- Operational data, recommendations, commands or automation: `0`
- Lifecycle transition: `NOT AUTHORISED`

O4-UI1 remediates the visual hierarchy and layout issues recorded in the first visible O4 samples. It changes only
the dedicated test-only Observer composition, its localised presentation catalogue, directly related tests and its
existing browser auditor. The read-only projection API, accepted synthetic fixture and ordinary Dashboard
composition are unchanged.

## Recorded causes

The previous layout placed the signal panel across two grid rows and kept a two-column composition until a physical
width that was too narrow for its content. At common scaled Windows viewports this produced an uneven page: one
large signal region on the left, compressed forecast and trace panels on the right, and limitations detached below.
Technical digests also competed with the primary operational meaning, while activation, freshness and forecast
availability did not form a clear first reading level.

The first remediated browser audit correctly failed because the short synthetic source label was initially treated
like a long technical digest. The implementation was narrowed so only long content-addressed identifiers are
collapsed behind an accessible disclosure. The source label remains directly visible. Cleanup after the failed
audit removed its exact browser, host and temporary roots before the successful rerun.

## Implemented presentation

The Observer page now provides:

- a balanced heading with a bounded, prominent `ActivationState=None` and read-only status;
- a three-card factual summary for current `Stale` evidence, `Unknown` forecast and inactive activation;
- a balanced two-column desktop composition that stacks before either column becomes compressed;
- consistent section headers with code-native semantic icons, titles and explanatory copy;
- a clearer signal card with a friendly provider-neutral metric label and its exact technical key retained;
- grouped value, threshold, evaluated-at and valid-until facts;
- an explicit uncertainty presentation for the unavailable complete forecast;
- ordered result, policy and corpus trace steps with compact digests and keyboard-accessible complete identifiers;
- a limitations panel integrated into the primary reading order;
- a professional responsive layout that stacks predictably on intermediate and compact viewports;
- forced-colour boundaries that do not rely on product colour alone.

No projection fact was removed. The signal remains `Stale`; the forecast remains `Unknown`; the synthetic,
non-operational and non-authorising boundaries remain visible.

## Automated visual and accessibility evidence

The existing browser auditor now checks 28 combinations:

- pt-BR and en-GB;
- Light and Dark;
- widths representing `1440`, `1180`, `1024`, `820` and `390` CSS pixels;
- effective 200% and 400% zoom widths;
- balanced two-column composition above the breakpoint and ordered single-column composition at and below it;
- three factual summary cards and all four detailed panels;
- complete traceability with full 64-character policy, corpus and O3 result identifiers available;
- zero horizontal page or main-content overflow;
- complete panel bounds, visible activation, freshness, synthetic truth and forecast uncertainty;
- no action control;
- forced colours modelled with retained panel and state boundaries;
- main/heading accessibility semantics and keyboard focus entry;
- authenticated read denial without the exact synthetic subject;
- write-method denial and zero external origin.

All 28 sanitised captures were held only in memory and discarded with the dedicated browser process.

## Validation

All validation ran locally and offline, without restore, installation, download or external access.

| Check | Result |
|---|---|
| Node.js and npm baseline | `24.18.0` / `11.16.0` |
| Dashboard type-check | `PASSED` |
| Dashboard tests | `72/72 PASSED` |
| O4 architecture tests | `4/4 PASSED` |
| complete architecture suite | `75/75 PASSED` |
| focused O4 integration tests | `3/3 PASSED` |
| complete Release solution build | `PASSED`, zero warnings and zero errors |
| proportional .NET coverage | lines `81.98%`, branches `53.85%`, ten required components present |
| O4 browser matrix | `28/28 PASSED` |
| forced colours and accessibility tree | `PASSED` |
| browser external origins | `0` |
| normal Dashboard build O4 references | `0` |
| code documentation and Markdown links | `PASSED` |
| secret scan and diff check | `PASSED` |

## Preserved boundaries

- `ActivationState=None` remains immutable.
- The O4 API remains authenticated, read-only and sandbox-only.
- The accepted O2/O3 result remains synthetic and non-representative of production.
- The composition still displays one `Stale` signal and an `Unknown` forecast.
- There are no suggestions, recommendations, approvals, commands, automation or execution controls.
- Normal Server and Dashboard composition contain no O4 activation or route.
- WPF, Tray, packages, dependency declarations and lockfiles are unchanged.
- No real telemetry, corpus, provider, database, credential or external endpoint was used.

## Cleanup and next gate

The successful browser audit closed its exact Chrome and sandbox host process trees and removed its profile,
listeners and GUID-owned temporary roots. The ordinary Dashboard build was restored afterwards and inspected for
zero O4 reference.

O4-UI1 is automatically approved only as the authorised visual remediation. The earlier human rejection remains
historical evidence and is not overwritten. A new visible human sample requires a separate authorisation and
decision. O4 cannot receive a Human Gate until that repeated sample is completed. This result does not authorise O5,
real data, normal composition, `OBSERVER`, recommendations, execution, deployment or lifecycle transition.
