# STATE-06 — Audit Remediation R6 Visible Human Samples Report

- Date: 2026-07-21
- Baseline: `878a7ea285b332aa9eedc59f4f4a69d5ba001c27`
- Lifecycle: `STATE-06 INTEGRATION`, unchanged
- Authorised activity: visible local human samples only
- Execution result: `STOPPED — REMEDIATION REQUIRES SEPARATE AUTHORITY`
- R6 human acceptance: `PENDING`

## Authority and boundaries

The review used the separately authorised visible-sample phase. It did not authorise implementation, source correction, a new harness, dependencies, restore, download, external access, Windows configuration changes, visible Windows notifications, operational data or a lifecycle transition.

The mandatory preflight observed the exact baseline, a clean worktree and zero DB-Notifier-owned runtime. The Dashboard sample used Chrome with a dedicated temporary profile, a loopback-only preview and no ordinary user browser state. The user supplied only sanitised captures of DB Notifier surfaces in the review conversation; no screenshot binary was copied into the repository.

## Sample disposition

| Sample | Scope | Decision | Evidence and limitation |
|---|---|---|---|
| `R6-HV-D01` | Dashboard pt-BR/en-GB, Light/Dark, routes, TV, disabled-instance truth, demonstration labelling, time-zone truth, keyboard, focus, 200% browser zoom and reflow | `REPROVADA` | Bruno decided exactly: `AMOSTRA R6-HV-D01 REPROVADA: gráfico de desempenho cortado durante o reflow`. The visible captures also showed an explicit disabled-instance exclusion, demonstration labels and `GMT-3`; those partial observations do not override the failed reflow decision. |
| `R6-HV-D02` | Dedicated-browser forced-colours sample | `BLOQUEADA` | Not executed. The authority required work to stop when a visible sample exposed a condition whose correction requires code. Automatic modelled forced-colours evidence remains separate and is not substituted for this human sample. |
| `R6-HV-D03` | Authoritative-snapshot TV source-truth sample | `BLOQUEADA` | Not executed after the mandatory stop. No authoritative or external source was activated. |
| `R6-HV-W01` | Visible WPF Desktop and Tray/flyout truth, keyboard, focus, bounded scroll, work area, reflow and positioning | `BLOQUEADA` | Not executed after the mandatory stop. No WPF window, Tray flyout or Windows notification was opened. |
| `R6-HV-W02` | Flyout semantic-mark refresh through an existing presentation-only/test-only mechanism | `BLOQUEADA` | Not executed after the mandatory stop. No harness was created or modified and no notification was emitted. |
| `R6-HV-P01` | Physical High Contrast, reduced motion, Narrator, Windows 200% scaling and mixed-DPI movement | `NÃO TESTADA` | These conditions were not already available or manually handed to the review before it stopped. The agent did not alter any Windows setting. Absence of physical evidence is not approval. |

## Observed defect and stop condition

The Performance graph is visibly clipped during the authorised reflow review. Correcting chart sizing, overflow or responsive layout would require a source change and new validation. Both are outside this phase. The review therefore stopped immediately after Bruno's explicit rejection; no diagnostic mutation or attempted fix followed.

This report does not determine the defect's code-level cause. Diagnosis and remediation require a separately bounded authorisation. The automatic R6 report remains valid as evidence of its own headless matrix, but that evidence did not detect or disprove the human-observed clipping.

## Cleanup evidence

The owned Chrome tree and preview process were stopped by exact process identity. Loopback listeners `127.0.0.1:43450` and `127.0.0.1:43451` were no longer present. The exact temporary root `DBNotifier-R6-HumanDashboard-ad3883314aea458083bc52a6eec58f5a` was removed after process shutdown.

The final shutdown preflight observed:

- zero process attributable to DB-Notifier or the dedicated review profile;
- zero listener on either review port;
- zero matching `DBNotifier-R6-HumanDashboard-*` temporary root;
- the ordinary browser, IDE and unrelated processes left untouched.

## Preserved boundaries

- No executable source, test, harness, generated artefact or product configuration changed.
- The automatic R6 commit and findings remain unmodified; human acceptance is still pending.
- The R5 NuGet metadata incident and the pre-existing global R0 assertion remain recorded and uncorrected.
- R2-A, R3, R4-A, R4-B and R5 containments remain intact.
- R2-B, R7–R8, R7-A0, O1, AIOps modes, LLMs, recommendations, commands, automation and lifecycle transition remain unauthorised.

## Next decision

R6 cannot be accepted on this evidence. The next permissible step is a separate proposal and explicit authorisation to diagnose and remediate the clipped Performance graph, with a focused regression sample. The blocked visible samples may be resumed only under subsequent explicit authority after the defect is addressed or deliberately dispositioned. This report is not an implementation authorisation or Human Gate acceptance.
