# Module purpose: Verifies the checked-in DB-Notifier development method, live plan and CI delegation contract.
#Requires -Version 7.0

[CmdletBinding()]
param(
    [string]$RepositoryRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Join-Path $PSScriptRoot '..'
}
if (-not (Test-Path -LiteralPath $RepositoryRoot -PathType Container)) {
    throw 'The development-flow repository root is unavailable.'
}
$resolvedRoot = [System.IO.Path]::GetFullPath(
    (Resolve-Path -LiteralPath $RepositoryRoot).Path)
$assertionCount = 0
. (Join-Path $PSScriptRoot 'toolchain-version-policy.ps1')

function Assert-Condition {
    <#
    .SYNOPSIS
    Fails the policy verification when one required invariant is false.

    .PARAMETER Condition
    Boolean invariant result.

    .PARAMETER Message
    Sanitised explanation of the violated contract.

    .OUTPUTS
    None.
    #>
    param(
        [Parameter(Mandatory)]
        [bool]$Condition,

        [Parameter(Mandatory)]
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
    $script:assertionCount++
}

function Get-RequiredFileText {
    <#
    .SYNOPSIS
    Reads one required project-owned policy file as text.

    .PARAMETER RelativePath
    Repository-relative file path.

    .OUTPUTS
    System.String file content.
    #>
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$RelativePath
    )

    $absolutePath = Join-Path $resolvedRoot $RelativePath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        throw "Required development-flow file '$RelativePath' is missing."
    }
    return Get-Content -LiteralPath $absolutePath -Raw
}

$gitPath = (Get-Command git -CommandType Application -ErrorAction Stop |
        Select-Object -First 1).Source
$reportedRoot = @(& $gitPath -C $resolvedRoot rev-parse --show-toplevel)
if ($LASTEXITCODE -ne 0) {
    throw 'Git could not resolve the development-flow repository root.'
}
$comparison = if ($IsWindows) {
    [System.StringComparison]::OrdinalIgnoreCase
}
else {
    [System.StringComparison]::Ordinal
}
Assert-Condition `
    -Condition ([System.IO.Path]::GetFullPath(($reportedRoot -join '').Trim()).Equals(
            $resolvedRoot,
            $comparison)) `
    -Message 'The development-flow verifier must run against the exact Git worktree root.'

$requiredFiles = @(
    '.nvmrc',
    'AGENTS.md',
    'docs/Legacy-Migration-Plan.md',
    'docs/STATE-06-MySQL-Notifier-Authority-Revocation-Report.md',
    'docs/STATE-06-MySQL-Notifier-Functional-Reference-Restoration-Report.md',
    'docs/design/DB-Notifier-Design-System.md',
    'global.json',
    'PLANS.md',
    'prompts/foundation/Prompt-New-Project.md',
    'prompts/governance/Conversation-Coordination-Prompt.md',
    'prompts/governance/Governance.md',
    'prompts/governance/Language-Policy.md',
    'prompts/governance/Lifecycle.md',
    'prompts/governance/Quality-Gates.md',
    'prompts/operations/Operational-Playbooks.md',
    'prompts/state/Current-State.md',
    'prompts/state/State-Transition-Log.md',
    'prompts/system/AI-Software-Engineering-Master-Prompt.md',
    'prompts/system/Prompt-System-Change-Log.md',
    'prompts/templates/Templates.md',
    'README.md',
    'scripts/assert-dbnotifier-shutdown.ps1',
    'scripts/development-environment.ps1',
    'scripts/development.ps1',
    'scripts/ci.ps1',
    'scripts/NuGet.Offline.config',
    'scripts/toolchain-version-policy.ps1',
    'scripts/verify-development-flow.ps1',
    'scripts/verify-node-toolchain.mjs',
    'src/DBNotifier.Dashboard.Web/package.json',
    'src/DBNotifier.Dashboard.Web/package-lock.json',
    'tests/DBNotifier.DevelopmentFlow.Tests.ps1',
    'tests/fixtures/DBNotifier.DevelopmentFlow.OwnedHelper.ps1')
$inventory = @(& $gitPath -C $resolvedRoot ls-files --cached --others --exclude-standard -- $requiredFiles)
if ($LASTEXITCODE -ne 0) {
    throw 'Git could not enumerate the development-flow files.'
}
$normalisedInventory = @($inventory | ForEach-Object { $_.Replace('\', '/') })
foreach ($requiredFile in $requiredFiles) {
    Assert-Condition `
        -Condition ($normalisedInventory -ccontains $requiredFile) `
        -Message "Development-flow file '$requiredFile' is missing from the project inventory."
}

# The verifier deliberately limits Git inventory to the explicit allowlist
# above. It must never enumerate the repository root or any protected external
# boundary merely to prove that the boundary remains excluded.

$plans = Get-RequiredFileText -RelativePath 'PLANS.md'
foreach ($requiredHeading in @(
        '## Control record',
        '## Task envelopes',
        '### Artefact classification and exclusive writers',
        '### Mutable-resource matrix',
        '### Stop conditions',
        '## Source provenance and adaptation boundary',
        '## Capability disposition',
        '## Positive scope',
        '## Negative scope and protected work',
        '## Definition of Ready',
        '## Definition of Done',
        '## Findings',
        '## Increment plan',
        '## Evidence log',
        '## Blockers and limitations',
        '## Outcome and next action',
        '## Change log')) {
    Assert-Condition `
        -Condition ($plans.Contains($requiredHeading, [System.StringComparison]::Ordinal)) `
        -Message "PLANS.md is missing required heading '$requiredHeading'."
}
foreach ($requiredPlanLiteral in @(
        '- Plan ID:',
        '- Status:',
        '- Initial baseline:',
        '- Authority:',
        '- Execution mode:',
        '- Writer:',
        '- Independent reviewers:',
        '- Rollback strategy:')) {
    Assert-Condition `
        -Condition ($plans.Contains($requiredPlanLiteral, [System.StringComparison]::Ordinal)) `
        -Message "PLANS.md is missing required control '$requiredPlanLiteral'."
}
Assert-Condition `
    -Condition ([regex]::Matches(
            $plans,
            '- Envelope ID and version: `DEV-FLOW-01/v[23]`[.]').Count -eq 2 -and
        $plans.Contains('### Original envelope `DEV-FLOW-01/v1` — closed', [System.StringComparison]::Ordinal)) `
    -Message 'PLANS.md must preserve the three distinct development-flow envelopes.'
foreach ($requiredPlanStopCode in @(
        'AUTHORITY_MISMATCH',
        'BASELINE_DRIFT',
        'SCOPE_OVERLAP',
        'DEPENDENCY_UNREADY',
        'ISOLATION_FAILURE',
        'MUTABLE_RESOURCE_COLLISION',
        'GATE_FAILURE',
        'EXTERNAL_AUTHORITY_REQUIRED',
        'HUMAN_DECISION_REQUIRED')) {
    Assert-Condition `
        -Condition ($plans.Contains($requiredPlanStopCode, [System.StringComparison]::Ordinal)) `
        -Message "PLANS.md is missing stop code '$requiredPlanStopCode'."
}

$canonicalToolchains = Get-DBNotifierToolchainPolicy
$dotNetPolicy = Get-DBNotifierDotNetSdkPolicy `
    -GlobalJsonPath (Join-Path $resolvedRoot 'global.json')
$dashboardPolicy = Get-DBNotifierDashboardToolchainPolicy -RepositoryRoot $resolvedRoot
Assert-Condition `
    -Condition ($dotNetPolicy.Range -ceq $canonicalToolchains.DotNetRange -and
        $dotNetPolicy.RollForward -ceq 'latestFeature') `
    -Message 'global.json must express the bounded stable .NET 10.0 policy.'
Assert-Condition `
    -Condition ($dashboardPolicy.NodeRange -ceq $canonicalToolchains.NodeRange -and
        $dashboardPolicy.NpmRange -ceq $canonicalToolchains.NpmRange -and
        $dashboardPolicy.NvmSelector -ceq $canonicalToolchains.NvmSelector) `
    -Message 'The Dashboard manifests must express the canonical bounded stable ranges.'

$developmentScript = Get-RequiredFileText -RelativePath 'scripts/development.ps1'
$environmentScript = Get-RequiredFileText -RelativePath 'scripts/development-environment.ps1'
$ciScript = Get-RequiredFileText -RelativePath 'scripts/ci.ps1'
foreach ($taskLiteral in @("'Doctor'", "'Setup'", "'Quick'", "'Full'", 'PlanOnly', 'NON_GATE')) {
    Assert-Condition `
        -Condition ($developmentScript.Contains($taskLiteral, [System.StringComparison]::Ordinal)) `
        -Message "The development entry point is missing '$taskLiteral'."
}
Assert-Condition `
    -Condition ($developmentScript.Contains('Resolve-CompatibleDotNetHost', [System.StringComparison]::Ordinal) -and
        $ciScript.Contains('Resolve-CompatibleDotNetHost', [System.StringComparison]::Ordinal) -and
        -not $developmentScript.Contains('Resolve-PinnedDotNetHost', [System.StringComparison]::Ordinal) -and
        -not $ciScript.Contains('Resolve-PinnedDotNetHost', [System.StringComparison]::Ordinal) -and
        -not $developmentScript.Contains("'.dotnet') 'toolchains'", [System.StringComparison]::Ordinal) -and
        -not $ciScript.Contains("'.dotnet') 'toolchains'", [System.StringComparison]::Ordinal)) `
    -Message 'Local and CI .NET resolution must enforce the compatible range rather than an exact pin.'
Assert-Condition `
    -Condition ([regex]::Matches(
            $developmentScript,
            '(?m)^\s*& \$ciEntrypoint @ciArguments\s*$').Count -eq 1) `
    -Message 'Full must delegate exactly once to the canonical CI entry point.'
foreach ($requiredEnvironmentBoundary in @(
        'ASPNETCORE_',
        'CONNECTIONSTRINGS__',
        'DASHBOARDTVSANDBOX__',
        'DASHBOARDTVSIGNALRSANDBOX__',
        'DBN_',
        'DBNOTIFIER_',
        'HUMANAUTHENTICATION__',
        'KESTREL__',
        'RECONCILEDLOCALNOTIFICATIONSANDBOX__',
        'VITE_DB_NOTIFIER_',
        'AZURE_OPENAI_API_KEY',
        'DOTNET_ENVIRONMENT',
        'OPENAI_API_KEY')) {
    Assert-Condition `
        -Condition ($environmentScript.Contains(
                "'$requiredEnvironmentBoundary'",
                [System.StringComparison]::Ordinal)) `
        -Message "The isolated environment boundary is missing '$requiredEnvironmentBoundary'."
}
foreach ($requiredExecutableStopCode in @(
        'DEPENDENCY_UNREADY:',
        'ISOLATION_FAILURE:')) {
    Assert-Condition `
        -Condition ($developmentScript.Contains(
                $requiredExecutableStopCode,
                [System.StringComparison]::Ordinal) -and
            $ciScript.Contains(
                $requiredExecutableStopCode,
                [System.StringComparison]::Ordinal)) `
        -Message "The executable flow is missing stop code '$requiredExecutableStopCode'."
}
Assert-Condition `
    -Condition ($environmentScript -notmatch '(?i)GetEnvironmentVariable|\$env:') `
    -Message 'The environment boundary must remove inherited hazards without reading their values.'
Assert-Condition `
    -Condition ($developmentScript.Contains(
            'Remove-DBNotifierInheritedEnvironment -StartInfo $startInfo',
            [System.StringComparison]::Ordinal) -and
        $ciScript.Contains(
            'Remove-DBNotifierInheritedEnvironment -StartInfo $startInfo',
            [System.StringComparison]::Ordinal)) `
    -Message 'Both local and direct CI entry points must use the isolated environment boundary.'
Assert-Condition `
    -Condition ($developmentScript.Contains(
            'DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE',
            [System.StringComparison]::Ordinal) -and
        $ciScript.Contains(
            'DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE',
            [System.StringComparison]::Ordinal)) `
    -Message 'Both offline entry points must suppress .NET workload update notifications.'
Assert-Condition `
    -Condition ($ciScript -notmatch 'if \(\$Stage -ceq ''Dashboard''\)' -and
        $ciScript -match 'if \(\$Stage -eq ''Dashboard''\)') `
    -Message 'The validated CI stage must dispatch case-insensitively.'
Assert-Condition `
    -Condition ($ciScript.IndexOf(
            'verify-secrets.ps1',
            [System.StringComparison]::Ordinal) -lt
        $ciScript.IndexOf(
            'Invoke-PolicyChecks -NodeExecutable $nodeExecutable',
            [System.StringComparison]::Ordinal)) `
    -Message 'Secret scanning must precede executable repository policy and product gates.'

Assert-Condition `
    -Condition ([regex]::Matches(
            $ciScript,
            [regex]::Escape('DBNotifier.DevelopmentFlow.Tests.ps1')).Count -eq 1) `
    -Message 'The canonical gate must invoke the development-flow policy tests exactly once.'
Assert-Condition `
    -Condition ($ciScript.Contains('NOT_RUN', [System.StringComparison]::Ordinal) -and
        $ciScript.Contains('PARTIAL', [System.StringComparison]::Ordinal) -and
        $ciScript.Contains('DISPOSITION|$failureDisposition', [System.StringComparison]::Ordinal)) `
    -Message 'The canonical gate must preserve explicit offline limitations.'

$workflow = Get-RequiredFileText -RelativePath '.github/workflows/ci.yml'
Assert-Condition `
    -Condition ($workflow.Contains('timeout-minutes: 135', [System.StringComparison]::Ordinal)) `
    -Message 'The sequential canonical CI gate is missing its evidence-based bounded timeout.'
Assert-Condition `
    -Condition ([regex]::Matches(
            $workflow,
            "(?m)^\s*dotnet-version:\s*'10[.]0[.]x'\s*$").Count -eq 1 -and
        [regex]::Matches(
            $workflow,
            "(?m)^\s*dotnet-quality:\s*'ga'\s*$").Count -eq 1 -and
        -not $workflow.Contains('global-json-file:', [System.StringComparison]::Ordinal)) `
    -Message 'CI must install the current stable .NET 10.0 SDK through the pinned setup-dotnet contract.'
Assert-Condition `
    -Condition ([regex]::Matches(
            $workflow,
            '(?m)^\s*run:\s*[.]\\scripts\\ci[.]ps1 -Stage All\b').Count -eq 1) `
    -Message 'CI must invoke the canonical full Windows gate exactly once.'
Assert-Condition `
    -Condition ([regex]::Matches(
            $workflow,
            '(?m)^\s*run:\s*[.]\/scripts\/ci[.]ps1 -Stage Dashboard\b').Count -eq 1) `
    -Message 'CI must retain one supplemental Linux Dashboard invocation through the same entry point.'
Assert-Condition `
    -Condition ([regex]::Matches(
            $workflow,
            '(?m)^\s*check-latest:\s*true\s*$').Count -eq 2) `
    -Message 'Both CI Node.js setup steps must select the current compatible Node 24 release.'
foreach ($forbiddenWorkflowCommand in @(
        'dotnet build',
        'dotnet test',
        'npm ci',
        'npm run build',
        'verify-secrets.ps1')) {
    Assert-Condition `
        -Condition (-not $workflow.Contains(
                $forbiddenWorkflowCommand,
                [System.StringComparison]::OrdinalIgnoreCase)) `
        -Message "CI bypasses the canonical entry point with '$forbiddenWorkflowCommand'."
}

$coordination = Get-RequiredFileText -RelativePath 'prompts/governance/Conversation-Coordination-Prompt.md'
Assert-Condition `
    -Condition ($coordination.Contains('- Revisão: `2.0.0`', [System.StringComparison]::Ordinal)) `
    -Message 'The coordination authority is not at revision 2.0.0.'
foreach ($requiredCoordinationLiteral in @(
        'SAFE_PARALLEL',
        'CONTRACT_FROZEN_PARALLEL',
        'SINGLE_OWNER',
        'BASELINE_DRIFT',
        'GATE_FAILURE',
        'CONTINUE_CURRENT',
        'DELEGATE_SUBAGENT',
        'RETURN_TO_EXISTING',
        'START_NEW_AUTO_DISPATCH',
        'deduplicação',
        'receipt',
        'AGENT_DECIDED',
        'AUTOMATED_GATE_PASS',
        'AUTOMATED_GATE_FAIL',
        'LOCAL_COMPLETE',
        'EXTERNAL_PREREQUISITE',
        'BLOCKED_BY_HIGHER_AUTHORITY')) {
    Assert-Condition `
        -Condition ($coordination.Contains(
                $requiredCoordinationLiteral,
                [System.StringComparison]::Ordinal)) `
        -Message "The coordination authority is missing '$requiredCoordinationLiteral'."
}
Assert-Condition `
    -Condition ($coordination.Contains(
            'Não produzir título sugerido, `Exact next message`, copy box ou instrução para',
            [System.StringComparison]::Ordinal) -and
        $coordination.Contains(
            'A indisponibilidade nunca produz texto para o proprietário copiar.',
            [System.StringComparison]::Ordinal)) `
    -Message 'The coordination authority does not prohibit owner-mediated copy-and-paste dispatch.'
Assert-Condition `
    -Condition ($coordination.Contains(
            '`CONTINUE_CURRENT` usa um registro local factual',
            [System.StringComparison]::Ordinal) -and
        $coordination.Contains(
            'exigem confirmação factual da ferramenta',
            [System.StringComparison]::Ordinal)) `
    -Message 'The coordination authority does not distinguish local continuation from tool-backed dispatch receipts.'

$changelog = Get-RequiredFileText -RelativePath 'prompts/system/Prompt-System-Change-Log.md'
$currentState = Get-RequiredFileText -RelativePath 'prompts/state/Current-State.md'
$stateTransitionLog = Get-RequiredFileText -RelativePath 'prompts/state/State-Transition-Log.md'
$masterPrompt = Get-RequiredFileText -RelativePath 'prompts/system/AI-Software-Engineering-Master-Prompt.md'
$rootInstructions = Get-RequiredFileText -RelativePath 'AGENTS.md'
$governance = Get-RequiredFileText -RelativePath 'prompts/governance/Governance.md'
$qualityGates = Get-RequiredFileText -RelativePath 'prompts/governance/Quality-Gates.md'
$legacyMigrationPlan = Get-RequiredFileText -RelativePath 'docs/Legacy-Migration-Plan.md'
$designSystem = Get-RequiredFileText -RelativePath 'docs/design/DB-Notifier-Design-System.md'
$revocationReport = Get-RequiredFileText -RelativePath 'docs/STATE-06-MySQL-Notifier-Authority-Revocation-Report.md'
$restorationReport = Get-RequiredFileText -RelativePath 'docs/STATE-06-MySQL-Notifier-Functional-Reference-Restoration-Report.md'
$projectReadme = Get-RequiredFileText -RelativePath 'README.md'
$projectVision = Get-RequiredFileText -RelativePath 'prompts/foundation/Prompt-New-Project.md'
$matrixRows = @(
    [regex]::Matches(
        $legacyMigrationPlan,
        '(?m)^\| `MN-(?:\d{3}|Q\d{2})` \|.*$') |
        ForEach-Object { $_.Value.TrimEnd("`r") }
)
$matrixRowBytes = [System.Text.Encoding]::UTF8.GetBytes(($matrixRows -join "`n"))
$matrixRowHash = [Convert]::ToHexString(
    [System.Security.Cryptography.SHA256]::HashData($matrixRowBytes)).ToLowerInvariant()
$changelogEightSectionMatch = [regex]::Match(
    $changelog,
    '(?ms)^## 8[.]0[.]0 — 2026-08-28\r?\n(?<body>.*?)(?=^## )')
$changelogEightSection = $changelogEightSectionMatch.Groups['body'].Value
$changelogNineSectionMatch = [regex]::Match(
    $changelog,
    '(?ms)^## 9[.]0[.]0 — 2026-08-30\r?\n(?<body>.*?)(?=^## )')
$changelogNineSection = $changelogNineSectionMatch.Groups['body'].Value
$changelogSevenSectionMatch = [regex]::Match(
    $changelog,
    '(?ms)^## 7[.]0[.]0 — 2026-08-28\r?\n(?<body>.*?)(?=^## )')
$changelogSevenSection = $changelogSevenSectionMatch.Groups['body'].Value
Assert-Condition `
    -Condition ($changelog -match '(?s)## Versão atual\s+- Versão: `9[.]0[.]0`') `
    -Message 'The instruction-corpus changelog is not at version 9.0.0.'
Assert-Condition `
    -Condition ($changelogNineSectionMatch.Success -and
        $changelogNineSection.Contains('Agent Gates objetivos', [System.StringComparison]::Ordinal) -and
        $changelogNineSection.Contains('`START_NEW_AUTO_DISPATCH`', [System.StringComparison]::Ordinal) -and
        $changelogNineSection.Contains('Preserva integralmente autenticação humana, RBAC', [System.StringComparison]::Ordinal) -and
        $changelogNineSection.Contains('A supersessão é somente prospectiva', [System.StringComparison]::Ordinal)) `
    -Message 'The 9.0.0 changelog entry does not delimit autonomous delivery and preserved product controls.'
Assert-Condition `
    -Condition ($changelogEightSectionMatch.Success -and
        $changelogEightSection.Contains('`GOV-MN-RESTORE-01`', [System.StringComparison]::Ordinal) -and
        $changelogEightSection.Contains('cláusula de inspiração funcional MySQL Notifier de `REQ-047`', [System.StringComparison]::Ordinal) -and
        $changelogEightSection.Contains('matriz funcional ativa', [System.StringComparison]::Ordinal) -and
        $changelogEightSection.Contains('Reuso literal exige proveniência exata', [System.StringComparison]::Ordinal)) `
    -Message 'The 8.0.0 changelog entry does not delimit functional restoration and the expression boundary.'
Assert-Condition `
    -Condition ($changelogSevenSectionMatch.Success -and
        $changelogSevenSection.Contains('`GOV-MN-REV-01`', [System.StringComparison]::Ordinal) -and
        $changelogSevenSection.Contains('cada funcionalidade isolada', [System.StringComparison]::Ordinal) -and
        $changelogSevenSection.Contains('não remove comportamento DB-Notifier já próprio', [System.StringComparison]::Ordinal)) `
    -Message 'The 7.0.0 changelog entry no longer preserves the historical revocation.'
Assert-Condition `
    -Condition ($currentState.Contains('`9.0.0`', [System.StringComparison]::Ordinal) -and
        $currentState.Contains('`2.0.0`', [System.StringComparison]::Ordinal)) `
    -Message 'Current-State.md does not record the adopted workflow versions.'
Assert-Condition `
    -Condition ($rootInstructions.Contains('## Autonomous delivery mandate', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('`START_NEW_AUTO_DISPATCH`', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('`AUTOMATED_GATE_PASS`', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('Product-user authentication, RBAC, administrative confirmation', [System.StringComparison]::Ordinal)) `
    -Message 'The root instructions do not enforce autonomous delivery while preserving product controls.'
Assert-Condition `
    -Condition ($rootInstructions.Contains('existing currently valid credential', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('available capable tool', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('broad path, unresolved variable, substitution, glob or unresolved target', [System.StringComparison]::Ordinal)) `
    -Message 'The root instructions do not preserve external-action and destructive-target safety prerequisites.'
Assert-Condition `
    -Condition ($rootInstructions.Contains('protected-work preservation, recoverable checkpoint, rollback, no safer alternative, objective necessity and independent review', [System.StringComparison]::Ordinal) -and
        $governance -match '(?s)preservação de WIP.*checkpoint recuperável e plano de rollback.*validação de rollback.*alternativa materialmente mais segura.*necessidade objetiva e revisão independente' -and
        $qualityGates -match '(?s)preservação de WIP, checkpoint recuperável,\s+rollback, validação aplicável, inexistência de alternativa mais segura,\s+necessidade objetiva e revisão independente') `
    -Message 'The destructive Safety Gate does not preserve recovery, alternative, necessity and review controls.'
Assert-Condition `
    -Condition ($rootInstructions.Contains('exact account and environment', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('defined cost boundary when applicable', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('objective success criterion and safe verification or reversal', [System.StringComparison]::Ordinal) -and
        $governance -match '(?s)conta, organização, ambiente e\s+alvo exatos.*limite de custo definido.*critério\s+objetivo de sucesso.*verificação segura ou reversão' -and
        $qualityGates -match '(?s)conta e\s+ambiente exatos.*limite de custo.*critério objetivo de sucesso.*verificação segura ou reversão') `
    -Message 'The external-action gate does not preserve account, environment, cost, success and verification or reversal controls.'
Assert-Condition `
    -Condition ($rootInstructions.Contains('`GOV-MN-RESTORE-01`', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('sanitised, observable and non-expressive functional outcomes', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('`MN-001`–`MN-025` and `MN-Q01`–`MN-Q04`', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('read-only source-exposed analyst', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('corresponding implementation and test authors must remain unexposed', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('raw or unsanitised source-derived notes', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('approved sanitised handoff', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('exact component and rightsholder provenance, applicable rights or permissions and a compatible distribution model', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('does not by itself relicense that Oracle/MySQL or third-party material', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('grant trademark rights', [System.StringComparison]::Ordinal)) `
    -Message 'The root instructions do not enforce the corrected functional-reference and expression boundary.'
Assert-Condition `
    -Condition ($legacyMigrationPlan.Contains('## MySQL Notifier functional benchmark', [System.StringComparison]::Ordinal) -and
        $legacyMigrationPlan.Contains('These 29 records are the active independent functional-coverage baseline', [System.StringComparison]::Ordinal) -and
        $legacyMigrationPlan.Contains('they do not themselves authorise implementation', [System.StringComparison]::Ordinal) -and
        $legacyMigrationPlan.Contains('`GOV-MN-REV-01` report', [System.StringComparison]::Ordinal) -and
        -not $legacyMigrationPlan.Contains('These 29 records are retired historical traceability', [System.StringComparison]::Ordinal)) `
    -Message 'The migration plan does not present the restored functional matrix and preserved revocation history.'
Assert-Condition `
    -Condition ($matrixRows.Count -eq 29 -and
        $matrixRowHash -eq '8961a3af4b02de68a2b16b83159c48779e5d26cffaabbb6de387e4a6b61d1449') `
    -Message 'The 29-row MySQL Notifier functional matrix has drifted from its preserved content.'
Assert-Condition `
    -Condition ($designSystem.Contains('| Design System version | `3.4.2` |', [System.StringComparison]::Ordinal) -and
        $designSystem.Contains('Design System `3.4.2` records the owner''s `GOV-MN-RESTORE-01` clarification', [System.StringComparison]::Ordinal) -and
        $designSystem.Contains('sanitised capability inventory MAY inform provider-neutral requirements', [System.StringComparison]::Ordinal)) `
    -Message 'The Design System does not preserve the restored functional-reference boundary.'
Assert-Condition `
    -Condition ($currentState.Contains('A referência funcional de `REQ-047` está restaurada', [System.StringComparison]::Ordinal) -and
        $currentState.Contains('`REQ-048` e `REQ-050` estão `ATIVOS COM LIMITES`', [System.StringComparison]::Ordinal) -and
        $restorationReport.Contains('functional-inspiration clause', [System.StringComparison]::Ordinal) -and
        $currentState.Contains('os 29 registros', [System.StringComparison]::Ordinal) -and
        $restorationReport.Contains('6f7c58b1c36c91dd2c3d7406aba31eb3d8213e6e737dce23be6fc0eaa1b4c676', [System.StringComparison]::Ordinal) -and
        $revocationReport.Contains('every individual MySQL Notifier functionality', [System.StringComparison]::Ordinal)) `
    -Message 'Current functional-reference fact or preserved revocation evidence is incomplete.'
Assert-Condition `
    -Condition ($projectReadme.Contains('sanitised, observable and non-expressive functional outcomes', [System.StringComparison]::Ordinal) -and
        $projectReadme.Contains('independently governed versioned providers/plugins', [System.StringComparison]::Ordinal) -and
        $projectVision.Contains('`GOV-MN-RESTORE-01` registra a intenção corrigida', [System.StringComparison]::Ordinal)) `
    -Message 'The project overview or vision does not preserve the corrected reference boundary.'
Assert-Condition `
    -Condition ($rootInstructions.Contains('MySQL remains a separately governed future database provider', [System.StringComparison]::Ordinal) -and
        $legacyMigrationPlan.Contains('A rejected mechanism cannot be reintroduced', [System.StringComparison]::Ordinal) -and
        $restorationReport.Contains('does not authorise literal source reuse', [System.StringComparison]::Ordinal) -and
        $restorationReport.Contains('All 26', [System.StringComparison]::Ordinal) -and
        $restorationReport.Contains('non-rejected records have an open or bounded exit', [System.StringComparison]::Ordinal) -and
        $restorationReport.Contains('MySQL remains a separately governed database-provider candidate', [System.StringComparison]::Ordinal)) `
    -Message 'The restoration does not protect safety rejections, licensing and the independent MySQL provider boundary.'
Assert-Condition `
    -Condition ($currentState.Contains('Qualquer inspeção futura exige autoridade', [System.StringComparison]::Ordinal) -and
        $restorationReport.Contains('Any future source-code inspection requires separate explicit authority', [System.StringComparison]::Ordinal) -and
        $restorationReport.Contains('sampled licence/notice', [System.StringComparison]::Ordinal) -and
        $restorationReport.Contains('That future lot is not authorised by this report', [System.StringComparison]::Ordinal) -and
        $restorationReport.Contains('`S06-DFR-03 Desktop Fleet Authenticated Runtime', [System.StringComparison]::Ordinal)) `
    -Message 'Source inspection or the S06-DFR-03 successor is implicitly authorised or factually misrepresented.'
Assert-Condition `
    -Condition ($stateTransitionLog.Contains('## 2026-08-28 — GOV-MN-RESTORE-01 restaura a referência funcional MySQL Notifier', [System.StringComparison]::Ordinal) -and
        $stateTransitionLog.Contains('## 2026-08-28 — GOV-MN-REV-01 revoga a autoridade MySQL Notifier', [System.StringComparison]::Ordinal) -and
        $stateTransitionLog.Contains('6f7c58b1c36c91dd2c3d7406aba31eb3d8213e6e737dce23be6fc0eaa1b4c676', [System.StringComparison]::Ordinal) -and
        $stateTransitionLog.Contains('`REQ-048` e `REQ-050` voltam a `ATIVOS COM LIMITES`', [System.StringComparison]::Ordinal) -and
        $stateTransitionLog.Contains('`STATE-06 INTEGRATION` permanece inalterado', [System.StringComparison]::Ordinal)) `
    -Message 'The append-only history does not preserve revocation and corrected restoration.'
Assert-Condition `
    -Condition ($stateTransitionLog.Contains(
            'Método e fluxo de desenvolvimento governado adotados de forma DB-native',
            [System.StringComparison]::Ordinal) -and
        $stateTransitionLog.Contains('corpus de instruções `6.5.0`', [System.StringComparison]::Ordinal) -and
        $stateTransitionLog.Contains('corpus elevado a `6.6.0`', [System.StringComparison]::Ordinal)) `
    -Message 'The append-only history does not preserve the 6.5.0 to 6.6.0 development-flow transition.'
Assert-Condition `
    -Condition ($masterPrompt.Contains('scripts/development.ps1', [System.StringComparison]::Ordinal) -and
        $masterPrompt.Contains('PLANS.md', [System.StringComparison]::Ordinal)) `
    -Message 'The adopted master method does not route the live plan and development entry point.'

Write-Output "Development-flow policy passed with $assertionCount assertions."
