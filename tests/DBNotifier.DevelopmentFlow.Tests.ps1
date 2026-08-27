# Module purpose: Exercises the development-flow policy without restoring, building, testing product code or using the network.
#Requires -Version 7.0

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$developmentPath = Join-Path (Join-Path $repositoryRoot 'scripts') 'development.ps1'
$ciPath = Join-Path (Join-Path $repositoryRoot 'scripts') 'ci.ps1'
$environmentPath = Join-Path (Join-Path $repositoryRoot 'scripts') 'development-environment.ps1'
$shutdownPreflightPath = Join-Path (Join-Path $repositoryRoot 'scripts') 'assert-dbnotifier-shutdown.ps1'
$ownedHelperPath = Join-Path (Join-Path $repositoryRoot 'tests/fixtures') 'DBNotifier.DevelopmentFlow.OwnedHelper.ps1'
$workflowPath = Join-Path (Join-Path $repositoryRoot '.github/workflows') 'ci.yml'
$assertionCount = 0

function Assert-Condition {
    <#
    .SYNOPSIS
    Fails the standalone policy test when one required invariant is false.

    .PARAMETER Condition
    Boolean invariant result.

    .PARAMETER Message
    Sanitised failure description.

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

function Assert-Plan {
    <#
    .SYNOPSIS
    Requires two PlanOnly executions to match one exact ordered plan.

    .PARAMETER Task
    Development task to inspect.

    .PARAMETER Offline
    Selects the explicitly offline plan variant.

    .PARAMETER Expected
    Exact versioned plan records.

    .OUTPUTS
    None.

    .NOTES
    PlanOnly returns before process inventory, child creation, restore, build, tests or network access.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$Task,

        [switch]$Offline,

        [Parameter(Mandatory)]
        [string[]]$Expected
    )

    $arguments = @{
        Task = $Task
        PlanOnly = $true
    }
    if ($Offline) {
        $arguments['Offline'] = $true
    }
    $first = @(& $developmentPath @arguments)
    $second = @(& $developmentPath @arguments)
    Assert-Condition `
        -Condition ($first.Count -eq $Expected.Count) `
        -Message "$Task PlanOnly returned an unexpected record count."
    Assert-Condition `
        -Condition (-not (Compare-Object -ReferenceObject $Expected -DifferenceObject $first -SyncWindow 0)) `
        -Message "$Task PlanOnly diverged from its exact contract."
    Assert-Condition `
        -Condition (-not (Compare-Object -ReferenceObject $first -DifferenceObject $second -SyncWindow 0)) `
        -Message "$Task PlanOnly is not deterministic."
}

$developmentScript = Get-Content -LiteralPath $developmentPath -Raw
$ciScript = Get-Content -LiteralPath $ciPath -Raw
$shutdownScript = Get-Content -LiteralPath $shutdownPreflightPath -Raw
$workflow = Get-Content -LiteralPath $workflowPath -Raw
. $environmentPath

foreach ($secretName in @('OPENAI_API_KEY', 'AZURE_OPENAI_API_KEY')) {
    Assert-Condition `
        -Condition ($developmentScript -notmatch (
                '(?i)GetEnvironmentVariable\s*\([^)]*' + [regex]::Escape($secretName)) -and
            $developmentScript -notmatch (
                '(?i)\$env:' + [regex]::Escape($secretName))) `
        -Message "The development entry point must not read '$secretName'."
}

$childCredentialRemoval = $developmentScript.IndexOf(
    'Remove-DBNotifierInheritedEnvironment -StartInfo $startInfo',
    [System.StringComparison]::Ordinal)
$childStart = $developmentScript.IndexOf(
    '$process.Start()',
    [System.StringComparison]::Ordinal)
$taskDispatch = $developmentScript.IndexOf(
    'switch ($Task)',
    [System.StringComparison]::Ordinal)
$internalCredentialScrub = $developmentScript.IndexOf(
    '[System.Environment]::SetEnvironmentVariable(',
    $childStart,
    [System.StringComparison]::Ordinal)
Assert-Condition `
    -Condition ($childCredentialRemoval -ge 0 -and
        $childStart -gt $childCredentialRemoval -and
        $internalCredentialScrub -gt $childStart -and
        $taskDispatch -gt $internalCredentialScrub) `
    -Message 'Credential removal must precede child start and internal task dispatch.'
Assert-Condition `
    -Condition ($developmentScript.Contains('ProcessStartInfo', [System.StringComparison]::Ordinal)) `
    -Message 'The development entry point must use an isolated shell-free child process.'
Assert-Condition `
    -Condition ([regex]::Matches(
            $developmentScript,
            '(?m)^\s*& \$ciEntrypoint @ciArguments\s*$').Count -eq 1) `
    -Message 'Full must delegate to the canonical CI entry point exactly once.'

$parentSentinelName = 'DBNOTIFIER_DEVELOPMENT_TEST_SENTINEL'
[System.Environment]::SetEnvironmentVariable(
    $parentSentinelName,
    'parent-preserved',
    [System.EnvironmentVariableTarget]::Process)
try {
    $isolatedStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $isolatedStartInfo.UseShellExecute = $false
    $isolatedStartInfo.Environment['DBNOTIFIER_R4B_POSTGRESQL_LAB'] = 'local-test'
    $isolatedStartInfo.Environment['DBNOTIFIER_R4B_POSTGRESQL_CONNECTION'] = 'must-not-be-read'
    $isolatedStartInfo.Environment['ConnectionStrings__ServerDatabase'] = 'must-not-be-read'
    $isolatedStartInfo.Environment['DashboardTvSandbox__Enabled'] = 'true'
    $isolatedStartInfo.Environment['DashboardTvSignalRSandbox__Enabled'] = 'true'
    $isolatedStartInfo.Environment['DOTNET_ENVIRONMENT'] = 'DashboardTvSandbox'
    $isolatedStartInfo.Environment['Kestrel__Endpoints__Http__Url'] = 'must-not-be-read'
    $isolatedStartInfo.Environment['ReconciledLocalNotificationSandbox__Enabled'] = 'true'
    $isolatedStartInfo.Environment['VITE_DB_NOTIFIER_TV_SANDBOX'] = 'local-test'
    $isolatedStartInfo.Environment['OPENAI_API_KEY'] = 'must-not-be-read'
    $isolatedStartInfo.Environment['DB_NOTIFIER_SAFE_SENTINEL'] = 'preserved'

    Remove-DBNotifierInheritedEnvironment -StartInfo $isolatedStartInfo

    foreach ($removedName in @(
            $parentSentinelName,
            'DBNOTIFIER_R4B_POSTGRESQL_LAB',
            'DBNOTIFIER_R4B_POSTGRESQL_CONNECTION',
            'ConnectionStrings__ServerDatabase',
            'DashboardTvSandbox__Enabled',
            'DashboardTvSignalRSandbox__Enabled',
            'DOTNET_ENVIRONMENT',
            'Kestrel__Endpoints__Http__Url',
            'ReconciledLocalNotificationSandbox__Enabled',
            'VITE_DB_NOTIFIER_TV_SANDBOX',
            'OPENAI_API_KEY')) {
        Assert-Condition `
            -Condition (-not $isolatedStartInfo.Environment.ContainsKey($removedName)) `
            -Message "The isolated environment retained hazardous variable '$removedName'."
    }
    Assert-Condition `
        -Condition ($isolatedStartInfo.Environment['DB_NOTIFIER_SAFE_SENTINEL'] -ceq 'preserved') `
        -Message 'The environment boundary removed an unrelated variable.'
    Assert-Condition `
        -Condition ([System.Environment]::GetEnvironmentVariable(
                $parentSentinelName,
                [System.EnvironmentVariableTarget]::Process) -ceq 'parent-preserved') `
        -Message 'Child-environment sanitisation mutated the parent process environment.'
}
finally {
    [System.Environment]::SetEnvironmentVariable(
        $parentSentinelName,
        $null,
        [System.EnvironmentVariableTarget]::Process)
}

Assert-Plan -Task 'Doctor' -Expected @(
    'PLAN|version=1|task=Doctor|mode=Online|classification=WORKFLOW',
    'STEP|1|Assert the mandatory DB-Notifier shutdown preflight',
    'STEP|2|Validate repository root and required files',
    'STEP|3|Validate PowerShell, Git, .NET, Node.js and npm toolchains',
    'STEP|4|Validate tracked NuGet and npm lock files',
    'STEP|5|Report whether restored dependencies are ready'
)

Assert-Plan -Task 'Setup' -Offline -Expected @(
    'PLAN|version=1|task=Setup|mode=Offline|classification=WORKFLOW',
    'STEP|1|Assert the mandatory DB-Notifier shutdown preflight',
    'STEP|2|Validate repository root, toolchains and lock files',
    'STEP|3|dotnet restore DBNotifier.sln --configfile scripts/NuGet.Offline.config --locked-mode',
    'STEP|4|npm ci --offline --ignore-scripts --no-audit --no-fund',
    'STEP|5|Verify lockfile stability and restored dependency readiness'
)

Assert-Plan -Task 'Quick' -Expected @(
    'PLAN|version=1|task=Quick|mode=Online|classification=NON_GATE',
    'STEP|1|Assert the mandatory DB-Notifier shutdown preflight',
    'STEP|2|Validate repository root, toolchains, lock files and restored dependencies',
    'STEP|3|dotnet format DBNotifier.sln --verify-no-changes --no-restore',
    'STEP|4|dotnet build DBNotifier.sln --configuration Release --no-restore',
    'STEP|5|dotnet test tests/DBNotifier.UnitTests/DBNotifier.UnitTests.csproj --configuration Release --no-build --no-restore',
    'STEP|6|dotnet test tests/DBNotifier.Architecture.Tests/DBNotifier.Architecture.Tests.csproj --configuration Release --no-build --no-restore',
    'STEP|7|Verify Dashboard assets, types, documentation and Markdown',
    'STEP|8|npm test',
    'STEP|9|Verify development-flow and script policies',
    'STEP|10|git diff --check'
)

Assert-Plan -Task 'Full' -Expected @(
    'PLAN|version=1|task=Full|mode=Online|classification=GATE',
    'STEP|1|Assert the mandatory DB-Notifier shutdown preflight',
    'STEP|2|scripts/ci.ps1'
)

Assert-Plan -Task 'Full' -Offline -Expected @(
    'PLAN|version=1|task=Full|mode=Offline|classification=GATE',
    'STEP|1|Assert the mandatory DB-Notifier shutdown preflight',
    'STEP|2|scripts/ci.ps1 -Offline'
)

# Prove that the preflight blocks a disposable, exactly attributable helper
# and returns to a clean result after that owned child has ended.
$ownedHelper = [System.Diagnostics.Process]::new()
$ownedHelperStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
$ownedHelperStartInfo.FileName = (Get-Process -Id $PID).Path
$ownedHelperStartInfo.UseShellExecute = $false
foreach ($argument in @(
        '-NoLogo',
        '-NoProfile',
        '-NonInteractive',
        '-File',
        $ownedHelperPath)) {
    $ownedHelperStartInfo.ArgumentList.Add($argument)
}
$ownedHelper.StartInfo = $ownedHelperStartInfo
try {
    Assert-Condition `
        -Condition $ownedHelper.Start() `
        -Message 'The disposable shutdown-preflight helper did not start.'
    $preflightBlocked = $false
    try {
        & $shutdownPreflightPath -RepositoryRoot $repositoryRoot | Out-Null
    }
    catch {
        $preflightBlocked = $true
    }
    Assert-Condition `
        -Condition $preflightBlocked `
        -Message 'The shutdown preflight did not block an exactly attributable project helper.'
}
finally {
    if (-not $ownedHelper.HasExited) {
        $ownedHelper.Kill($true)
        $ownedHelper.WaitForExit()
    }
    $ownedHelper.Dispose()
}
$cleanPreflight = @(& $shutdownPreflightPath -RepositoryRoot $repositoryRoot)
Assert-Condition `
    -Condition ($cleanPreflight -contains 'PASS|shutdown-preflight|matching-processes=0|owned-listeners=0') `
    -Message 'The shutdown preflight did not return to a clean result after owned-helper cleanup.'
Assert-Condition `
    -Condition ($shutdownScript.Contains(
            "`$executablePathLeaf -match '^(?i:DBNotifier|DB-Notifier)(?:[.]|`$)'",
            [System.StringComparison]::Ordinal) -and
        -not $shutdownScript.Contains(
            "`$processNameLeaf -match '^(?i:DBNotifier|DB-Notifier)(?:[.]|`$)'",
            [System.StringComparison]::Ordinal)) `
    -Message 'Product-process ownership must use verified executable-path evidence rather than a process name alone.'
Assert-Condition `
    -Condition ($shutdownScript.Contains(
            '$unreadableProcessIds.Add($processId)',
            [System.StringComparison]::Ordinal) -and
        $shutdownScript.Contains(
            'Linux procfs identity remained unreadable for live process IDs',
            [System.StringComparison]::Ordinal)) `
    -Message 'Linux process inventory must fail closed for a still-present unreadable identity.'

$quickBody = [regex]::Match(
    $developmentScript,
    '(?s)function Invoke-Quick \{(?<body>.*?)\n\}').Groups['body'].Value
foreach ($forbiddenQuickOperation in @(
        'DBNotifier.IntegrationTests',
        'verify-dotnet-coverage.ps1',
        'verify-nuget-vulnerabilities.ps1',
        'npm audit',
        'run-state05',
        'run-state06')) {
    Assert-Condition `
        -Condition (-not $quickBody.Contains(
                $forbiddenQuickOperation,
                [System.StringComparison]::OrdinalIgnoreCase)) `
        -Message "Quick contains forbidden gate operation '$forbiddenQuickOperation'."
}

Assert-Condition `
    -Condition ([regex]::Matches(
            $ciScript,
            [regex]::Escape('DBNotifier.DevelopmentFlow.Tests.ps1')).Count -eq 1) `
    -Message 'The canonical gate must invoke this development-flow policy test exactly once.'
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
    -Condition ($ciScript.Contains(
            'DISPOSITION|$failureDisposition|stage=$canonicalStage|stop=$stopCode',
            [System.StringComparison]::Ordinal) -and
        $ciScript.Contains("'GATE_FAILURE'", [System.StringComparison]::Ordinal)) `
    -Message 'The canonical gate must preserve a machine-readable blocked or failed disposition.'
Assert-Condition `
    -Condition ([regex]::Matches(
            $workflow,
            '(?m)^\s*run:\s*[.]\\scripts\\ci[.]ps1 -Stage All\b').Count -eq 1) `
    -Message 'The CI workflow must delegate its canonical Windows gate exactly once.'

$approvedActionPins = @{
    'actions/checkout' = '11d5960a326750d5838078e36cf38b85af677262'
    'actions/setup-dotnet' = '67a3573c9a986a3f9c594539f4ab511d57bb3ce9'
    'actions/setup-node' = '49933ea5288caeca8642d1e84afbd3f7d6820020'
    'actions/upload-artifact' = 'ea165f8d65b6e75b540449e92b4886f43607fa02'
}
$actionUses = [regex]::Matches(
    $workflow,
    '(?m)^\s*(?:-\s+)?uses:\s*(?<action>[^@\s]+)@(?<revision>[^\s#]+)')
Assert-Condition `
    -Condition ($actionUses.Count -gt 0 -and
        @($actionUses | Where-Object {
                $action = $_.Groups['action'].Value
                -not $approvedActionPins.ContainsKey($action) -or
                $approvedActionPins[$action] -cne $_.Groups['revision'].Value
            }).Count -eq 0) `
    -Message 'The CI workflow uses an unapproved or mutable Action revision.'
Assert-Condition `
    -Condition ($workflow -match '(?m)^permissions:\s*\r?\n\s*contents:\s*read\s*$' -and
        $workflow -match '(?m)^\s*fetch-depth:\s*0\s*$' -and
        [regex]::Matches(
            $workflow,
            '(?m)^\s*persist-credentials:\s*false\s*$').Count -eq 2 -and
        [regex]::Matches(
            $workflow,
            '(?m)^\s*node-version-file:\s*[.]nvmrc\s*$').Count -eq 2) `
    -Message 'The CI workflow diverged from its least-privilege or pinned-toolchain boundary.'
Assert-Condition `
    -Condition ($workflow -match '(?m)^\s*timeout-minutes:\s*135\s*$') `
    -Message 'The sequential canonical CI job lacks its bounded capacity envelope.'

foreach ($requiredGateReference in @(
        'verify-dotnet-lockfiles.ps1',
        'verify-dotnet-coverage.ps1',
        'verify-nuget-vulnerabilities.ps1',
        'audit-fail-closed-runtime.ps1',
        'run-legacy-tests.ps1',
        'run-state05-dashboard-audit.ps1',
        'run-state06-consolidated-e2e.ps1',
        'verify-secrets.ps1',
        'DBNotifier.RunnerProcess.Tests.ps1',
        'DBNotifier.ScriptSyntax.Tests.ps1')) {
    Assert-Condition `
        -Condition ([regex]::Matches(
                $ciScript,
                [regex]::Escape($requiredGateReference)).Count -eq 1) `
        -Message "The canonical gate must retain '$requiredGateReference' exactly once."
}
Assert-Condition `
    -Condition ($ciScript -notmatch '(?i)GetEnvironmentVariable\s*\([^)]*(?:OPENAI|AZURE_OPENAI)' -and
        $ciScript -notmatch '(?i)\$env:(?:OPENAI|AZURE_OPENAI)' -and
        $ciScript.IndexOf(
            'Remove-DBNotifierInheritedEnvironment -StartInfo $startInfo',
            [System.StringComparison]::Ordinal) -lt
        $ciScript.IndexOf(
            '$process.Start()',
            [System.StringComparison]::Ordinal)) `
    -Message 'The canonical gate must scrub inherited credentials and activators before child execution without reading them.'
Assert-Condition `
    -Condition ($ciScript -notmatch '(?i)Invoke-Expression|Start-Process') `
    -Message 'The canonical gate must preserve shell-free native process boundaries.'

Write-Output "Development-flow policy tests passed with $assertionCount assertions."
