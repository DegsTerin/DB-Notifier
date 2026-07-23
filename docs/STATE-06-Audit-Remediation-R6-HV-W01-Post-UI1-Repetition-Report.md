# STATE-06 — R6-HV-W01 Visible Repetition Report after R6-UI1

## Decision summary

- Authorised baseline: `c7479d853f20ceb45aab70173431e46480b458e9`
- Sample: `R6-HV-W01`
- Automatic prerequisite: R6-UI1 complete within its restricted scope
- Human result: `REJECTED`
- Product implementation during this activity: `NONE`
- R6 acceptance and lifecycle transition: `NOT AUTHORISED`

This activity was authorised only to repeat the visible WPF Desktop, Tray/flyout and Web-reference comparison after R6-UI1. It stopped during the Web comparison before the full sample was completed. It did not authorise or perform source, XAML, CSS, test, harness, Design System, configuration or fixture changes. W02, P01, R6 acceptance and lifecycle progression remained outside the sample.

## Isolated visible execution

The mandatory preflight confirmed the exact authorised baseline, a clean worktree, the authorised preference SHA-256 and zero DB Notifier runtime or matching temporary root. The existing WPF and Dashboard outputs were newer than their owning sources, so no build, restore, installation or download was performed.

The Web reference ran from the existing Dashboard build through an owned loopback-only preview. Chrome used a unique temporary profile, a closed loopback proxy for non-loopback traffic and host-resolution denial outside localhost. The WPF process used the existing complete test-only activation, including `--notifications-quiet`, an intentionally unbound HTTPS loopback endpoint, a temporary state directory and a synthetic subject. The owned connection audit found zero non-loopback connection. No operational source, provider, database, command, external integration or Windows notification was used.

The active Windows work area remained `1920×1032`; therefore `1920×1080` remained `NOT TESTED`, and no Windows setting was changed.

## Human observation

During the compact Web Overview comparison, Bruno identified that the complete disabled-state text did not remain within its status container. The text `Desabilitada · excluída da saúde atual` crossed the pill boundary and entered the horizontal area reserved for the performance sparkline. This is a material containment and reflow defect in the Web presentation.

Bruno supplied a sanitised capture of the dedicated DB Notifier surface in the conversation. No screenshot binary was added to the repository. The material defect stopped the campaign; no positive result was inferred for the remaining routes, locale/theme combinations, compact variants or Tray/flyout details.

After cleanup and the factual summary, Bruno decided exactly:

`AMOSTRA R6-HV-W01 REPETIDA — REPROVADA: na versão Web, o texto “Desabilitada · excluída da saúde atual” ultrapassa o status pill e invade a área do sparkline durante o reflow`

No correction was attempted because the visible-sample authority expressly prohibited implementation.

## Cleanup and preserved state

The first cleanup comparison refused fail-closed because PowerShell had interpreted the persisted ISO timestamp through the current culture. A second normalisation attempt also refused before termination because the automatically converted value was formatted under a different culture. Reading the metadata with `ConvertFrom-Json -DateKind String` preserved the original ISO value; PID, executable path, complete quiet test-only command line and start instant then matched exactly.

Only after that proof, WPF, the dedicated Chrome profile and the preview were terminated by their exact identities without invoking the normal WPF Close path. The original preference bytes were restored before the exact temporary root was removed.

Final verification proved:

- zero WPF, Chrome-profile or preview process residue;
- zero visible DB Notifier top-level window and zero matching notification-area automation element in the Windows shell or overflow host;
- zero owned listener or matching temporary-root residue;
- preference restored byte for byte;
- final preference SHA-256 `ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F`;
- clean Git worktree before this evidence-only documentation change.

No user browser, profile, IDE, service, database or unrelated process was changed.

## Evidence-only gates

The documentation gate passed for `326` comment-capable source files, the Markdown link gate passed for `615` local links in `136` files, the secret scan passed for the non-ignored worktree and available Git history, and `git diff --check` passed. Product builds and tests were deliberately not repeated because this activity changed only factual Markdown evidence after the visible sample.

## Disposition

`R6-HV-W01` remains `REJECTED` after R6-UI1. The automatic R6-UI1 result remains factual for its restricted WPF remediation and automatic matrix, but it does not override the human finding in the unchanged Web reference.

R6 human acceptance remains `PENDING`. W02 remains `BLOCKED`; P01 and each physical condition remain `NOT TESTED`. The R0 global assertion and R5 NuGet metadata incident remain recorded and uncorrected.

The next permissible step is a separate proposal and explicit authorisation for a focal Web remediation of disabled-status containment and responsive allocation between the status and sparkline regions. Any implementation must be followed by another separately authorised visible repetition. No R6 acceptance, W02/P01 resumption, R7/R8 work, O1/AIOps implementation or lifecycle transition is authorised by this report.
