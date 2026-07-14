# STATE-05 Lighthouse Audit

## Result

`APPROVED — AUTOMATIC EVIDENCE ONLY`

The local Dashboard passed the repeatable Lighthouse matrix after remediation of the findings described below. This result does not approve the `STATE-05` Human Gate, native browser zoom, keyboard operation, screen-reader behaviour, Windows scaling, High Contrast, WPF or any external integration.

## Authority and scope

Bruno explicitly authorised Codex on 2026-07-14 to perform the recommended Lighthouse work. The authorised scope was limited to the local deterministic Dashboard and included:

- Overview, Inventory, History, Alerts and Configuration;
- mobile `390×844` and desktop `1440×1000` emulation;
- three independent Lighthouse collections for every route/profile pair;
- a clean, isolated headless Chrome profile with extensions disabled;
- Performance, Accessibility, Best Practices and SEO categories;
- a valid crawler policy for the internal operational console;
- remediation and repetition when the audit found a reproducible product defect.

No visible Chrome window, existing browser profile, WPF process, Narrator, Windows theme, High Contrast or display scaling setting was opened or changed. No Agent, API, monitored database, identity provider, vault, notification channel or administrative executor was contacted.

## Environment and reproducibility

| Item | Observed value |
|---|---|
| Date | 2026-07-14 |
| Dashboard | Vite production preview on `http://127.0.0.1:4173/` |
| Lighthouse | `13.4.0`, pinned through ephemeral `npx` execution |
| Chrome | `150.0.7871.115` |
| Mobile profile | form factor `mobile`, `390×844`, device scale factor `1` |
| Desktop profile | official Lighthouse desktop preset, `1440×1000`, device scale factor `1` |
| Runs | `3` per route/profile; `30` final raw reports |
| Browser isolation | dedicated temporary user-data directories, headless, extensions disabled |
| Raw evidence | `%TEMP%\DBNotifier-Lighthouse-20260714-053200` |

The repository runner is [`scripts/run-state05-lighthouse-audit.ps1`](../scripts/run-state05-lighthouse-audit.ps1). It accepts only a loopback HTTP Dashboard URI, validates the effective viewport, rejects Lighthouse runtime errors, rejects `chrome-extension://` traffic, requires a valid `robots.txt` audit, removes its Chrome profiles and emits `summary.json` plus the raw reports.

After building and starting the local production preview, repeat the matrix from the repository root:

```powershell
.\scripts\run-state05-lighthouse-audit.ps1 -Runs 3
```

## Findings and remediation

### `S05-AUTO-LH-001` — SPA fallback was returned as `robots.txt`

The reports supplied before this controlled run showed SEO `92` because `/robots.txt` returned the application HTML fallback and Lighthouse parsed eighteen invalid directives. The experimental agentic-browsing category also treated the HTML fallback at `/llms.txt` as invalid.

Remediation adds a real `public/robots.txt` response with `text/plain` content and `Disallow: /`. The operational console is intentionally not crawlable. This policy is crawler guidance, not an authentication or authorisation boundary.

No `llms.txt` file was added. The Dashboard is an internal operational interface rather than public content intended for AI discovery, and the experimental agentic-browsing category is outside the accepted `STATE-05` product contract.

### `S05-AUTO-LH-002` — desktop Light sidebar label contrast

The first clean matrix found that the visible desktop Light label `Navigation.Label` used the muted token at `3.85:1` on the sidebar background. Its normal text size requires at least `4.5:1`. Compact mobile samples did not expose the failure because the label is hidden there.

Remediation changes only that label to the existing secondary text token and adds a source regression. A desktop pilot then produced Accessibility `100` and passed the Lighthouse colour-contrast audit. The entire 24-report matrix was rebuilt and repeated after this correction.

### collection configuration correction

The first automated desktop collection used `form-factor=desktop` without the official desktop preset. That retained mobile simulation settings and produced an artificial Performance `90`. This was a runner configuration error, not a product performance defect. The final runner uses the official desktop preset plus explicit `1440×1000` screen emulation; the post-correction pilot measured FCP `329 ms`, LCP `373 ms` and Performance `100`.

### `S05-AUTO-LH-003` — Overview trend-axis contrast

The first five-route matrix measured Accessibility `96` on Overview because its small `100`, `50` and `0 ms` chart-axis labels used the Light muted token at `4.03:1`. Normal text requires at least `4.5:1`.

Remediation changes only those axis labels to the existing semantic secondary-text token. The complete thirty-report matrix was rebuilt and repeated; Accessibility then passed at `100` for every route/profile/run.

### `S05-AUTO-LH-004` — Design System 2.3.0 mobile brand semantics

The Design System `2.3.0` regression expanded the runner from five to all eight current routes: Overview, Instances, Alerts, Performance, History, Configuration, Providers and Settings. Its first current mobile pass found `aria-label="DB Notifier"` on a generic `div`, where that ARIA attribute was prohibited without a semantic role. The same single failure reduced Accessibility to `96`–`97` across the mobile routes even though the visible rendering was unchanged.

The brand lockup now has the semantic image role that corresponds to its accessible name. Dashboard tests, typecheck and build passed, then all sixteen current route/profile reports were rebuilt. Accessibility and Best Practices reached `100` in `16/16`; Performance was `99`–`100`. The raw current regression summary is temporary local evidence under `%TEMP%\DBNotifier-Lighthouse-2.3.0-fixed-003a32b5ac40479d948a7413f4a9a488`.

This single-run regression supplements rather than rewrites the three-run baseline below. It proves that every current route was sampled after the `2.3.0` navigation and visual changes; it does not claim the statistical stability of three repetitions for the new routes.

| Profile | Routes | Reports | Performance | Accessibility | Best Practices | SEO |
|---|---:|---:|---:|---:|---:|---:|
| Desktop | 8 | 8 | 100 | 100 | 100 | 66 |
| Mobile | 8 | 8 | 99–100 | 100 | 100 | 66 |

## Final category results

Every value below is the median of three final runs.

| Profile | Route | Performance | Accessibility | Best Practices | SEO |
|---|---|---:|---:|---:|---:|
| Desktop | Overview | 100 | 100 | 100 | 66 |
| Desktop | Inventory | 100 | 100 | 100 | 66 |
| Desktop | History | 100 | 100 | 100 | 66 |
| Desktop | Alerts | 100 | 100 | 100 | 66 |
| Desktop | Configuration | 100 | 100 | 100 | 66 |
| Mobile | Overview | 100 | 100 | 100 | 66 |
| Mobile | Inventory | 100 | 100 | 100 | 66 |
| Mobile | History | 100 | 100 | 100 | 66 |
| Mobile | Alerts | 100 | 100 | 100 | 66 |
| Mobile | Configuration | 100 | 100 | 100 | 66 |

The SEO score of `66` is expected and accepted for this internal console. Lighthouse passed the syntax/content audit for `robots.txt` in `30/30` reports and reduced the category score only because `is-crawlable` correctly detected `Disallow: /`. Removing that restriction merely to increase the score would contradict the approved crawler policy.

## Final metric ranges

Ranges show the minimum and maximum across the three final runs.

| Profile | Route | FCP | LCP | TBT | CLS | Speed Index |
|---|---|---:|---:|---:|---:|---:|
| Desktop | Overview | 327–331 ms | 411–416 ms | 0 ms | 0 | 327–331 ms |
| Desktop | Inventory | 331–341 ms | 387–417 ms | 0 ms | 0 | 331–341 ms |
| Desktop | History | 328–329 ms | 411–414 ms | 0 ms | 0 | 328–329 ms |
| Desktop | Alerts | 327–328 ms | 411–412 ms | 0 ms | 0 | 327–328 ms |
| Desktop | Configuration | 327–329 ms | 410–413 ms | 0 ms | 0 | 327–329 ms |
| Mobile | Overview | 1,357–1,363 ms | 1,511–1,669 ms | 2–15 ms | 0 | 1,357–1,363 ms |
| Mobile | Inventory | 1,359–1,360 ms | 1,513–1,515 ms | 13–38 ms | 0 | 1,359–1,360 ms |
| Mobile | History | 1,360 ms | 1,515–1,665 ms | 8–23 ms | 0 | 1,360 ms |
| Mobile | Alerts | 1,357–1,358 ms | 1,511–1,662 ms | 0 ms | 0 | 1,357–1,358 ms |
| Mobile | Configuration | 1,358–1,359 ms | 1,513–1,664 ms | 0 ms | 0 | 1,358–1,359 ms |

Every route/profile median was `100` for Performance, Accessibility and Best Practices. Accessibility and Best Practices were `100` in `30/30`; Performance was `100` in `29/30` and `99` in mobile Overview run 3 (`1,669 ms` LCP, `15 ms` TBT). All reports had zero layout shift, a valid crawler policy and no extension requests. The runner removed both isolated Chrome profile directories.

## Limitations

- Lighthouse is laboratory automation and does not prove usability for a human keyboard, screen-reader or zoom user.
- Results describe the local production bundle, host and tool versions above; they are not field telemetry or a production service-level objective.
- The intentional crawler block makes aggregate SEO unsuitable as a quality gate for this internal console. The applicable crawler gate is a syntactically valid, explicit non-crawling policy.
- The raw reports are temporary local evidence and are not committed because thirty Lighthouse payloads are large and environment-specific. The pinned runner and this sanitised consolidation are versioned.
- The supplied PDF/HTML/JSON reports were diagnostic input only; the clean post-remediation matrix is the current automatic evidence.

## Lifecycle impact

The automatic Lighthouse increment is complete and approved in its stated scope. `STATE-05 FRONTEND_IMPLEMENTATION` remains the current state, its Human Gate remains `PENDENTE`, and no transition to `STATE-06` is authorised.

## Recommended next step

The human reviewer should now complete the still-pending `STATE-05` samples in [`STATE-05-Human-Gate-Validation.md`](STATE-05-Human-Gate-Validation.md), beginning with the operational Overview and two-column Tray before returning to the corrected narrow-desktop Alert summary and `320` CSS px/200% zoom samples. Record each observation separately; do not use the Lighthouse result as a substitute for visual, keyboard, Narrator, scaling, High Contrast, TV or WPF approval.
