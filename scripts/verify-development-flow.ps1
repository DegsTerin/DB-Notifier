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
    'PLANS.md',
    'scripts/assert-dbnotifier-shutdown.ps1',
    'scripts/development-environment.ps1',
    'scripts/development.ps1',
    'scripts/ci.ps1',
    'scripts/NuGet.Offline.config',
    'scripts/verify-development-flow.ps1',
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

$protectedExternalTree = 'mysql-notifier-1.1.8-src/'
& $gitPath -C $resolvedRoot check-ignore --quiet -- $protectedExternalTree
Assert-Condition `
    -Condition ($LASTEXITCODE -eq 0) `
    -Message 'The protected external reference tree is not excluded from project inventory.'
$projectInventory = @(& $gitPath -C $resolvedRoot ls-files --cached --others --exclude-standard)
if ($LASTEXITCODE -ne 0) {
    throw 'Git could not enumerate the project inventory.'
}
Assert-Condition `
    -Condition (@($projectInventory | Where-Object {
                $_.Replace('\', '/').StartsWith(
                    $protectedExternalTree,
                    [System.StringComparison]::OrdinalIgnoreCase)
            }).Count -eq 0) `
    -Message 'Protected external reference material entered the project inventory.'

$plans = Get-RequiredFileText -RelativePath 'PLANS.md'
foreach ($requiredHeading in @(
        '## Control record',
        '## Closed task envelope',
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
        '- Envelope ID and version:',
        '- Rollback strategy:')) {
    Assert-Condition `
        -Condition ($plans.Contains($requiredPlanLiteral, [System.StringComparison]::Ordinal)) `
        -Message "PLANS.md is missing required control '$requiredPlanLiteral'."
}
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

$package = Get-RequiredFileText -RelativePath 'src/DBNotifier.Dashboard.Web/package.json' |
    ConvertFrom-Json
$nvmVersion = (Get-RequiredFileText -RelativePath '.nvmrc').Trim()
Assert-Condition `
    -Condition ($nvmVersion -ceq [string]$package.engines.node) `
    -Message '.nvmrc must equal the exact Dashboard Node.js engine.'
Assert-Condition `
    -Condition ([string]$package.packageManager -ceq "npm@$([string]$package.engines.npm)") `
    -Message 'The Dashboard packageManager and npm engine must remain identical.'

$developmentScript = Get-RequiredFileText -RelativePath 'scripts/development.ps1'
$environmentScript = Get-RequiredFileText -RelativePath 'scripts/development-environment.ps1'
$ciScript = Get-RequiredFileText -RelativePath 'scripts/ci.ps1'
foreach ($taskLiteral in @("'Doctor'", "'Setup'", "'Quick'", "'Full'", 'PlanOnly', 'NON_GATE')) {
    Assert-Condition `
        -Condition ($developmentScript.Contains($taskLiteral, [System.StringComparison]::Ordinal)) `
        -Message "The development entry point is missing '$taskLiteral'."
}
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
            '(?m)^\s*run:\s*[.]\\scripts\\ci[.]ps1 -Stage All\b').Count -eq 1) `
    -Message 'CI must invoke the canonical full Windows gate exactly once.'
Assert-Condition `
    -Condition ([regex]::Matches(
            $workflow,
            '(?m)^\s*run:\s*[.]\/scripts\/ci[.]ps1 -Stage Dashboard\b').Count -eq 1) `
    -Message 'CI must retain one supplemental Linux Dashboard invocation through the same entry point.'
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
    -Condition ($coordination.Contains('- Revisão: `1.4.0`', [System.StringComparison]::Ordinal)) `
    -Message 'The coordination authority is not at revision 1.4.0.'
foreach ($requiredCoordinationLiteral in @(
        'SAFE_PARALLEL',
        'CONTRACT_FROZEN_PARALLEL',
        'SINGLE_OWNER',
        'BASELINE_DRIFT',
        'GATE_FAILURE')) {
    Assert-Condition `
        -Condition ($coordination.Contains(
                $requiredCoordinationLiteral,
                [System.StringComparison]::Ordinal)) `
        -Message "The coordination authority is missing '$requiredCoordinationLiteral'."
}

$changelog = Get-RequiredFileText -RelativePath 'prompts/system/Prompt-System-Change-Log.md'
$currentState = Get-RequiredFileText -RelativePath 'prompts/state/Current-State.md'
$stateTransitionLog = Get-RequiredFileText -RelativePath 'prompts/state/State-Transition-Log.md'
$masterPrompt = Get-RequiredFileText -RelativePath 'prompts/system/AI-Software-Engineering-Master-Prompt.md'
Assert-Condition `
    -Condition ($changelog -match '(?s)## Versão atual\s+- Versão: `6[.]6[.]0`') `
    -Message 'The instruction-corpus changelog is not at version 6.6.0.'
Assert-Condition `
    -Condition ($currentState.Contains('`6.6.0`', [System.StringComparison]::Ordinal) -and
        $currentState.Contains('`1.4.0`', [System.StringComparison]::Ordinal)) `
    -Message 'Current-State.md does not record the adopted workflow versions.'
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
