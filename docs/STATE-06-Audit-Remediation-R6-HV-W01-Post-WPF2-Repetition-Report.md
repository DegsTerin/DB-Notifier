# STATE-06 — R6-HV-W01 Visible Repetition Report after R6-WPF2

## Decision summary

- Authorised baseline: `cf605f7fc775e22d7537ad39e27488e193feac8a`
- Sample: `R6-HV-W01`
- Automatic prerequisite: R6-WPF2 complete within its restricted scope
- Human result: `REJECTED`
- Product implementation during this activity: `NONE`
- R6 acceptance and lifecycle transition: `NOT AUTHORISED`

This activity repeated only the visible WPF Desktop and Tray/flyout comparison with the unchanged Web reference after R6-WPF2. It did not authorise or perform source, XAML, CSS, test, harness, Design System, configuration or fixture changes. W02, P01, R6 acceptance and lifecycle progression remained outside the sample.

## Isolated visible execution

The mandatory preflight confirmed the exact baseline, a clean worktree, the authorised preference SHA-256 and zero DB Notifier runtime or matching temporary root. The existing Release outputs matched the baseline, so no build, restore, installation or download was performed.

The Web reference ran from the existing Dashboard build on loopback port `30298`. Chrome used a unique temporary profile, a closed loopback proxy on port `30299` for non-loopback traffic and host-resolution denial outside localhost. The WPF process used the existing `--notifications-quiet` test-only composition, an intentionally unbound HTTPS loopback endpoint on port `30300`, a temporary state directory and a synthetic subject. The owned connection audit found zero non-loopback connection. No operational source, provider, database, command, external integration or Windows notification was used.

The WPF window opened at its native `1180×760` geometry and retained the authorised `820×620` minimum for compact review. The active work area remained `1920×1032`, so `1920×1080` remained `NOT TESTED`; no Windows setting was changed.

## Human observations

Bruno completed the comparison and supplied sanitised captures of the DB Notifier surfaces. The captures are conversation evidence and are not stored as repository binaries. They establish the following material defects:

| Area | Observed result | Classification |
|---|---|---|
| Instance status pills | The WPF `Stale`/`Desatualizado` container has a visibly different, compressed elliptical treatment from the Web component | `REJECTED` |
| Disabled status containment | The complete “disabled — excluded from current health” text does not remain safely contained in its status field at the reviewed width | `REJECTED` |
| Provider distribution | The WPF provider ring is clipped at the left edge instead of remaining fully contained in its card | `REJECTED` |
| Compact inventory layout | KPI, header and content regions do not reflow uniformly when the WPF window is reduced | `REJECTED` |
| Monitored-instances table | The first column has insufficient internal spacing, fixed columns force broad horizontal scrolling and the resulting composition differs materially from Web | `REJECTED` |
| Alerts | The table truncates summaries and relies on horizontal scrolling rather than a clear compact presentation | `REJECTED` |
| Operational history | Event summaries are truncated and the table does not reorganise adequately at the reviewed width | `REJECTED` |
| Operational configuration | Card density, table organisation, spacing and responsive treatment remain visibly inconsistent with the Web reference | `REJECTED` |
| Cross-route finish | Similar spacing, column, containment and compact-layout problems recur across the reviewed WPF routes | `REJECTED` |

The observations do not invalidate the automatic R6-WPF2 evidence that the authorised components and fields exist. They demonstrate that structural presence and the automatic clipping assertions were insufficient to prove human-acceptable visual finish, responsive composition and cross-surface uniformity.

The human decision is exactly:

`AMOSTRA R6-HV-W01 REPETIDA — REPROVADA: persistem defeitos de acabamento e uniformidade entre WPF e Web, incluindo status pills divergentes, texto fora do campo, gráfico de Providers cortado, reflow inadequado e tabelas com espaçamento, colunas e conteúdo mal ajustados.`

No positive result was inferred for unmentioned locale/theme combinations or for Tray/flyout details. One material failure is sufficient to reject W01. W02 remains blocked and P01 remains not tested.

## Cleanup and preserved state

The first cleanup attempt refused to stop the WPF process because the deserialised timestamp was compared using a culture-dependent string. The refusal occurred before any process termination, preference restoration or temporary-root deletion. A read-only comparison then proved the same PID, executable path and exact UTC instant; the cleanup was repeated with a normalised timestamp comparison.

The WPF process was terminated by exact PID, executable path and start time without invoking normal Close. Chrome was terminated only through the unique profile path, and the preview was terminated through its recorded process tree. The original preference bytes were restored after process termination.

Final verification proved:

- zero WPF, Chrome-profile or preview process residue;
- zero preview, unbound-API or closed-proxy listener residue;
- zero visible DB Notifier window or Tray process;
- zero matching W01 temporary root;
- preference restored byte for byte;
- final preference SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`;
- clean Git worktree before this evidence-only documentation change.

No user browser, profile, IDE, service, database or unrelated process was changed.

## Evidence-only gates

The documentation gate passed for `326` comment-capable source files, the Markdown link gate passed for `609` local links in `134` files, the secret scan passed for the non-ignored worktree and available Git history, and `git diff --check` passed. Product builds and tests were deliberately not repeated because this activity changed only factual Markdown evidence after the visible sample.

## Disposition

`R6-HV-W01` remains `REJECTED` after R6-WPF2. The automatic R6-WPF2 result remains factual for its restricted implementation and automated matrix, but it does not override the human visual result.

The next permissible step is a separate proposal for a bounded remediation of the recorded visual finish, status containment, provider-chart clipping, responsive composition and table-layout defects. That proposal must distinguish the affected Web and WPF owners, preserve native Windows adaptations, add human-relevant containment assertions and require another separately authorised visible W01 repetition. It must not silently resume W02/P01, accept R6, correct R0, alter the R5 incident or advance lifecycle state.
