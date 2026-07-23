# STATE-06 Audit Remediation — R0-F1 Global Gate Assertion Report

## Decision summary

- Authorised baseline: `c2a966c23c5829e8e2eaf8d2d320fe7579efb390`
- Scope: focal correction of the pre-existing R0 architecture assertion
- Automatic result: `APPROVED`
- Product, runner and workflow behaviour: `UNCHANGED`
- Lifecycle transition: `NOT AUTHORISED`

R0-F1 corrected only the architecture test that had kept the global .NET gate non-green after R0. No product composition, runner, workflow, dependency or operational behaviour changed.

## Confirmed cause

`State06ConsolidatedHarnessIsolationTests.BrowserRunnersBoundWorkAndCleanupExactOwnedResources` required the exact failure filenames to appear in `.github/workflows/ci.yml`.

The actual ownership contract deliberately separates responsibilities:

- `run-state05-dashboard-audit.ps1` writes `state05-dashboard-failure.json`;
- `run-state06-consolidated-e2e.ps1` writes `state06-consolidated-failure.json`;
- the workflow uploads only the corresponding sanitised diagnostic directory through `*.json`, fails when no evidence file exists and retains the artefact for seven days.

The runners and workflow were consistent with each other. The test incorrectly looked for runner-owned filenames in the workflow.

## Focal correction

The architecture assertion now verifies:

- each exact diagnostic filename in its owning runner;
- each exact diagnostic-directory wildcard in the workflow;
- both `if-no-files-found: error` guards;
- both seven-day retention declarations;
- the existing bounded STATE-06 job and timeout assertions.

This preserves fail-closed evidence publication without duplicating filenames into the workflow or weakening the runner contract.

## Verification

All commands ran locally, offline and without restore.

| Check | Result |
|---|---|
| focal corrected assertion | `1/1 PASSED` |
| complete architecture suite | `52/52 PASSED` |
| WPF tests | `10/10 PASSED` |
| unit tests | `393/393 PASSED` |
| integration tests | `22/22 PASSED` |
| complete solution tests | `477/477 PASSED` |
| Release solution build | `PASSED`, zero warnings and zero errors |
| `dotnet format --verify-no-changes` | `PASSED` |

The first focal compilation was stopped by analyser CA1875 because the initial edit used `Regex.Matches(...).Count`. The implementation was mechanically corrected to `Regex.Count`, after which the focal and complete gates passed. No failing result was hidden or excluded.

## Immutability and boundaries

The following preflight SHA-256 values are preserved for final comparison:

- workflow: `E777E3EC83A6E9DC5EF7A8257045A84CA81C471EB0B1EA6BBB33243CB7C16DD5`;
- STATE-05 runner: `CB628EF5FC2CB76374130245DD468425834A55856C4F6A58C9B612D5D4FA92FA`;
- STATE-06 runner: `96EA2DC780038ACB3D1FB6DC739AD006BBBFAE69B573BF5E730F2F4CB08A6A33`.

No dependency, package, lockfile, CI execution, runtime, external access, push, deploy, R7-A0, R8, O1, AIOps capability or lifecycle state was changed or authorised. The R5 NuGet metadata incident remains recorded and unaltered.

Historical reports that recorded the R0 assertion as failing remain factual for their original baselines. R0-F1 changes only the current disposition: the previously blocking global architecture assertion is now resolved and the current local .NET gate is green.
