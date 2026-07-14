# STATE-05 Lighthouse Audit

## Result

`APPROVED — AUTOMATIC EVIDENCE ONLY`

The local Dashboard passed the repeatable Lighthouse matrix after remediation of the findings described below. This result does not approve the `STATE-05` Human Gate, native browser zoom, keyboard operation, screen-reader behaviour, Windows scaling, High Contrast, WPF or any external integration.

## Authority and scope

Bruno explicitly authorised Codex on 2026-07-14 to perform the recommended Lighthouse work. The authorised scope was limited to the local deterministic Dashboard and included:

- Inventory, History, Alerts and Configuration;
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
| Runs | `3` per route/profile; `24` final raw reports |
| Browser isolation | dedicated temporary user-data directories, headless, extensions disabled |
| Raw evidence | `%TEMP%\DBNotifier-Lighthouse-STATE05-20260714-Final` |

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

## Final category results

Every value below is the median of three final runs.

| Profile | Route | Performance | Accessibility | Best Practices | SEO |
|---|---|---:|---:|---:|---:|
| Desktop | Inventory | 100 | 100 | 100 | 66 |
| Desktop | History | 100 | 100 | 100 | 66 |
| Desktop | Alerts | 100 | 100 | 100 | 66 |
| Desktop | Configuration | 100 | 100 | 100 | 66 |
| Mobile | Inventory | 100 | 100 | 100 | 66 |
| Mobile | History | 100 | 100 | 100 | 66 |
| Mobile | Alerts | 100 | 100 | 100 | 66 |
| Mobile | Configuration | 100 | 100 | 100 | 66 |

The SEO score of `66` is expected and accepted for this internal console. Lighthouse passed the syntax/content audit for `robots.txt` in `24/24` reports and reduced the category score only because `is-crawlable` correctly detected `Disallow: /`. Removing that restriction merely to increase the score would contradict the approved crawler policy.

## Final metric ranges

Ranges show the minimum and maximum across the three final runs.

| Profile | Route | FCP | LCP | TBT | CLS | Speed Index |
|---|---|---:|---:|---:|---:|---:|
| Desktop | Inventory | 327–332 ms | 370–378 ms | 0 ms | 0 | 327–332 ms |
| Desktop | History | 329–332 ms | 373–377 ms | 0 ms | 0 | 329–332 ms |
| Desktop | Alerts | 332–367 ms | 378–421 ms | 0 ms | 0 | 332–367 ms |
| Desktop | Configuration | 330–332 ms | 375–378 ms | 0 ms | 0 | 330–332 ms |
| Mobile | Inventory | 1,359–1,362 ms | 1,513–1,518 ms | 17–28 ms | 0 | 1,359–1,362 ms |
| Mobile | History | 1,360–1,406 ms | 1,515–1,561 ms | 0–37 ms | 0 | 1,360–1,406 ms |
| Mobile | Alerts | 1,358–1,362 ms | 1,512–1,517 ms | 7–11 ms | 0 | 1,358–1,362 ms |
| Mobile | Configuration | 1,358–1,360 ms | 1,512–1,515 ms | 0 ms | 0 | 1,358–1,360 ms |

Performance was `100` in `23/24` reports and `99` in one mobile History run; every route/profile median was `100`. Accessibility and Best Practices were `100` in `24/24`. All reports had zero layout shift. The runner observed zero extension requests and removed both isolated Chrome profile directories.

## Limitations

- Lighthouse is laboratory automation and does not prove usability for a human keyboard, screen-reader or zoom user.
- Results describe the local production bundle, host and tool versions above; they are not field telemetry or a production service-level objective.
- The intentional crawler block makes aggregate SEO unsuitable as a quality gate for this internal console. The applicable crawler gate is a syntactically valid, explicit non-crawling policy.
- The raw reports are temporary local evidence and are not committed because twenty-four Lighthouse payloads are large and environment-specific. The pinned runner and this sanitised consolidation are versioned.
- The supplied PDF/HTML/JSON reports were diagnostic input only; the clean post-remediation matrix is the current automatic evidence.

## Lifecycle impact

The automatic Lighthouse increment is complete and approved in its stated scope. `STATE-05 FRONTEND_IMPLEMENTATION` remains the current state, its Human Gate remains `PENDENTE`, and no transition to `STATE-06` is authorised.

## Recommended next step

The human reviewer should now complete the still-pending `STATE-05` samples in [`STATE-05-Human-Gate-Validation.md`](STATE-05-Human-Gate-Validation.md), beginning with the corrected narrow-desktop Alert summary and the `320` CSS px/200% zoom samples. Record each observation separately; do not use the Lighthouse result as a substitute for visual, keyboard, Narrator, scaling, High Contrast, TV or WPF approval.
