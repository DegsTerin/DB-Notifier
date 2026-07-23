# STATE-06 — R6-HV-W01 Visible Repetition Report after R6-WEB1

## Decision summary

- Authorised baseline: `eb14985615dd160500e40e56bd8e910789d98c7d`
- Sample: `R6-HV-W01`
- Automatic prerequisite: R6-WEB1 complete within its restricted scope
- Human result: `APPROVED`
- Product implementation during this activity: `NONE`
- R6 acceptance and lifecycle transition: `NOT AUTHORISED`

This activity repeated only the visible WPF Desktop, Tray/flyout and Web-reference comparison after R6-WEB1. It did not authorise or perform source, XAML, CSS, test, harness, Design System, dependency, configuration or fixture changes. W02, P01, R6 acceptance and lifecycle progression remained outside the sample.

## Isolated visible execution

The mandatory preflight confirmed the exact authorised baseline, a clean worktree, Node.js `24.18.0`, npm `11.16.0`, the authorised preference SHA-256 and zero DB Notifier runtime. Existing WPF and Dashboard outputs were newer than their owning sources, so no build, restore, installation or download was performed.

The Web reference ran from the existing Dashboard build through an owned loopback-only preview. Chrome used a unique temporary profile, a closed loopback proxy and host-resolution denial outside localhost. The WPF process used the complete existing test-only activation: `--show-desktop`, reconciled-notification sandbox, explicit opt-in, `--notifications-quiet`, an intentionally unbound HTTPS loopback endpoint, a temporary state directory, a synthetic certificate thumbprint and a synthetic subject. The exact command line was verified after launch, and the isolation guard found no non-loopback connection.

No operational source, provider, database, command, external integration, persistent product state or Windows notification was used.

## Human observation and decision

The validator received a short checklist covering the remediated disabled-status containment, responsive separation between status, latency and sparkline, Web/WPF visual parity, compact WPF reflow and the optional Tray/flyout surface. Bruno completed the inspection and responded exactly:

`INSPEÇÃO R6-HV-W01 APÓS R6-WEB1 CONCLUÍDA: Sem observações`

After the owned runtimes were closed and cleanup was proved, Bruno decided exactly:

`AMOSTRA R6-HV-W01 REPETIDA — APROVADA`

The decision approves this visible W01 repetition as presented. It does not convert the sample into evidence for unavailable physical conditions, approve W02 or P01, accept R6 as a whole or authorise lifecycle progression.

## Cleanup and preserved state

The WPF process, dedicated Chrome process tree and preview were closed by their recorded PID, executable path and invariant start instant. The ordinary WPF Close path was not used. The original preference bytes were restored before the exact temporary root was removed.

Final verification proved:

- zero WPF, dedicated-Chrome-profile or preview process residue;
- zero visible DB Notifier top-level window and zero matching notification-area automation element;
- zero owned listener;
- zero matching W01 temporary root after three consecutive absence checks;
- preference restored byte for byte;
- final preference SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`;
- clean Git worktree before this evidence-only documentation change.

No screenshot binary was added to the repository. No user browser, profile, IDE, service, database or unrelated process was changed.

## Disposition

`R6-HV-W01` is now `APPROVED` after R6-WEB1. Its earlier blocked and rejected results remain preserved as historical evidence and are not rewritten.

R6 human acceptance remains `PENDING`. W02 remains `BLOCKED`; P01 and each physical condition remain `NOT TESTED`. The R0 global assertion and R5 NuGet metadata incident remain recorded and uncorrected.

Any R6 acceptance decision, W02/P01 activity, R7/R8 work, O1/AIOps implementation or lifecycle transition requires separate explicit authority.
