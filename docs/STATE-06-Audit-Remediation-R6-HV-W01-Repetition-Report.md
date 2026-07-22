# STATE-06 — R6-HV-W01 Visible Repetition Report

## Decision summary

- Baseline: `288500de9e14212eca29b9b91b3de930fcb0ecb6`
- Sample: `R6-HV-W01`
- Result: `REJECTED`
- Product implementation during this activity: `NONE`
- R6 acceptance and lifecycle transition: `NOT AUTHORISED`

This activity repeated only the visible WPF Desktop and Tray/flyout sample after R6-WPF1. It did not authorise or perform a correction, redesign, new harness, external access, notification, W02, P01 or lifecycle progression.

## Isolated visible execution

The mandatory preflight confirmed the exact baseline, a clean worktree, the authorised preference SHA-256 and zero DB-Notifier runtime or matching temporary root. The existing Release executable was newer than the R6-WPF1 sources, so no build was necessary.

The dedicated process used the existing `--notifications-quiet` test-only composition, an unbound HTTPS loopback endpoint, a temporary ledger directory, a synthetic subject and no owned listener. The original preference bytes were copied before launch. No operational source, provider, database, command, external connection or Windows notification was used.

## Human observation

The visible repetition confirmed that R6-WPF1 removed the naming ambiguity by presenting `Configuração operacional` and `Preferências`. It did not, however, deliver the visual parity with the Dashboard Web that Bruno intended. The supplied visible capture and Bruno's inspection identified these material differences:

- KPI cards do not contain the equivalent semantic icon badges used by the Web Dashboard;
- provider identities are presented with generic database glyphs instead of the equivalent generated provider artwork visible on Web;
- instance, alert and provider regions do not preserve the same row organisation, separators, status pills, icon treatment and compact evidence structure;
- the WPF Performance card renders principally the two series and simple grid lines, without the Web chart's percentage-axis labels, time labels, demonstration badge and complete chart framing;
- card density, alignment and information hierarchy remain visibly different enough to impair cross-surface recognition.

These findings are broader than the original R6-WPF1 interpretation of semantic consistency. Native WPF caption, scrollbar, DPI and window behaviour remain legitimate platform adaptations, but they do not explain the missing shared components or reduced chart information.

After cleanup and a factual summary, Bruno decided exactly:

`AMOSTRA R6-HV-W01 REPETIDA — REPROVADA: o WPF ainda não reproduz a organização visual do Web; faltam ícones e componentes equivalentes e o gráfico apresenta apenas linhas sem eixos, rótulos e estrutura informativa`

The automatic R6-WPF1 implementation result remains historically valid for its narrower naming and navigation-semantic scope. It does not override this human rejection.

## Cleanup and preserved state

The dedicated process PID `48132` was stopped by exact identity without invoking the normal Close flow. The temporary loopback port `26089` had no listener, the exact runner-owned temporary root was removed and the final audit found:

- zero DB-Notifier or W01-owned process;
- zero owned listener;
- zero matching temporary root;
- clean Git worktree before evidence documentation;
- preference restored byte for byte to SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`.

No screenshot binary was added to the repository. No user browser, IDE, service, database or unrelated process was changed.

The evidence-only gates passed: documentation coverage for 316 comment-capable files, 602 local Markdown links in 132 files, secret scanning of the non-ignored worktree and available history, and `git diff --check`. Product build and tests were not repeated after the inspection because this activity changed only factual Markdown evidence.

## Disposition

`R6-HV-W01` remains rejected, now with explicit evidence that full shared-component and chart parity is still missing after R6-WPF1. `R6-HV-W02` remains blocked, `R6-HV-P01` and its physical conditions remain not tested, and R6 human acceptance remains pending. The R0 global assertion and R5 NuGet metadata incident remain recorded and uncorrected.

The next permissible step is a separate proposal and explicit authorisation for a new WPF visual-parity remediation. That proposal must inventory the corresponding Web and WPF components, define the exact shared presentation contract for icons, rows, status indicators, cards and charts, preserve native Windows behaviour, and require a separately authorised visible repetition after automatic evidence passes.
