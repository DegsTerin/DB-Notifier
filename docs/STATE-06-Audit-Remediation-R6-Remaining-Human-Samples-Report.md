# STATE-06 — R6 Remaining Visible Human Samples Report

- Date: 2026-07-21
- Baseline: `2e3b93b4333d22947d21a0baa46b1b05adcaea9d`
- Lifecycle: `STATE-06 INTEGRATION`, unchanged
- Campaign result: `STOPPED — R6-HV-D02 REJECTED`
- R6 human acceptance: `PENDING`

## Authority and boundaries

The campaign used only the separately authorised local, sequential and visible execution of the remaining R6 human samples. It permitted existing test-only fixtures and harnesses, one isolated dedicated browser for each applicable Dashboard sample, factual documentation and one final evidence commit. It did not authorise source, CSS, test, harness, configuration or Design System changes; remediation; dependencies; restore; download; external access; Windows configuration changes; operational data; notifications; or lifecycle progression.

The mandatory preflight observed the exact authorised baseline, a clean worktree and zero DB-Notifier-owned process, listener or human-sample temporary root. The normal Dashboard build already existed, so no build was required. `R6-HV-D02` used one dedicated Chrome profile, one visible DB Notifier page, a loopback-only preview and a closed local proxy for non-loopback destinations. Forced colours were enabled only inside that browser; no Windows High Contrast or other operating-system preference was changed.

The user supplied sanitised captures of DB Notifier surfaces in the review conversation. No screenshot binary was copied into the repository.

## Sample disposition

| Sample | Scope | Decision | Evidence and limitation |
|---|---|---|---|
| `R6-HV-D02` | Visible forced colours in a dedicated Dashboard browser | `REPROVADA` | The partial inspection reached the pt-BR Overview and was sufficient to expose two material visual failures. Bruno decided exactly: `AMOSTRA R6-HV-D02 REPROVADA: o texto da rota selecionada fica invisível e a borda lateral do cartão Crítico desaparece em forced colours`. The remaining routes, language/theme combinations and interactions were not claimed as inspected. |
| `R6-HV-D03` | Visible authoritative TV source truth | `BLOQUEADA` | Not executed. The authorised stop condition required the campaign to end when D02 exposed defects requiring remediation. No authoritative fixture, HTTPS sandbox or SignalR hint was started. |
| `R6-HV-W01` | Visible WPF Desktop and Tray/flyout presentation | `BLOQUEADA` | Not executed after the mandatory stop. No WPF window, Tray flyout, preference or Windows notification was opened or changed. |
| `R6-HV-W02` | In-memory flyout mark update through an existing presentation-only mechanism | `BLOQUEADA` | Not executed after the mandatory stop. No mechanism was invoked and no harness, fixture, marker or event was created or modified. |
| `R6-HV-P01` | Physical Windows accessibility and display conditions | `NÃO TESTADA` | Not executed after the mandatory stop. High Contrast, reduced motion, Narrator, physical 200% scaling and mixed-DPI movement each remain individually `NÃO TESTADA`; no Windows setting was changed by the agent. |

## Observed defects and stop condition

The visible selected navigation item retained its forced-colour selection surface and icon, but its route label became invisible. The Critical metric card retained its text and icon, but its right-hand boundary disappeared. These observations fail the authorised requirements for unequivocal current navigation, visible component boundaries and content that remains perceivable under forced colours.

Bruno completed the preliminary inspection with the exact response:

`INSPEÇÃO R6-HV-D02 CONCLUÍDA: REPROVADA — o texto da rota selecionada fica invisível; a borda lateral do cartão Crítico desaparece em forced colours;`

After exact runtime cleanup and a factual summary, Bruno issued the formal decision quoted in the table above. Diagnosing or correcting either defect would require code or CSS work and validation outside this authority. The campaign therefore stopped without attempting a fix, and no later sample was opened or inferred.

## Cleanup evidence

The dedicated Chrome root and descendants, the preview launcher and its descendants were stopped by exact profile, parentage and listener ownership. The loopback preview and debugging listeners on ports `53483` and `53484` were absent afterwards. The exact temporary root `DBNotifier-R6-HV-D02-35ec83a1f4d44af29102f036ccc3cbed`, including the disposable browser profile and preview logs, was removed only after the owned processes had ended.

An independent final audit observed:

- zero process attributable to DB-Notifier or the D02 profile;
- zero listener on either D02 port;
- zero matching `DBNotifier-R6-HV-D02-*` temporary root;
- the exact authorised Git baseline and a clean worktree before evidence documentation;
- the ordinary browser, IDE, databases, services and unrelated processes left untouched.

## Documentation gates

The evidence-only documentation checks passed:

- the repository Markdown-link verifier resolved `581` local links in `127` files;
- the secret scanner passed for the current non-ignored worktree and available Git history;
- `git diff --check`, trailing-whitespace inspection and the required sample-classification check passed.

No product build or test was applicable because executable code, configuration, tests and generated artefacts were unchanged. The final shutdown audit and staged-diff scope check also passed before commit.

## Preserved boundaries and next decision

- The approved automatic R6 phase, R6-G1 and the approved remediated repetition of `R6-HV-D01` remain unchanged.
- R6 human acceptance remains `PENDING`; this campaign is not a Human Gate decision.
- The R5 NuGet metadata incident and the pre-existing global R0 assertion remain recorded and uncorrected.
- R2-A, R3, R4-A, R4-B and R5 containments remain intact.
- R2-B, R7–R8, R7-A0, O1, AIOps modes, LLMs, recommendations, commands, automation and lifecycle progression remain unauthorised.

The next permissible step is a separate proposal and explicit authority to diagnose and remediate only the two `R6-HV-D02` forced-colour defects, followed by a separately authorised visible repetition. D03, W01, W02 and P01 cannot be resumed automatically. This report does not authorise remediation, accept R6 or transition the lifecycle.
