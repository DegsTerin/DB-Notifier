# Module purpose: Runs the canonical DB-Notifier repository gate locally and in CI without deployment or external product actions.
#Requires -Version 7.0

[CmdletBinding()]
param(
    [ValidateSet('All', 'Dashboard')]
    [string]$Stage = 'All',

    [switch]$Offline,

    [string]$DotNetPath,

    [string]$NodePath = 'node',

    [string]$NpmPath = 'npm',

    [string]$DiagnosticRoot,

    [Parameter(DontShow)]
    [string]$InternalExecutionToken
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$solutionPath = Join-Path $repositoryRoot 'DBNotifier.sln'
$dashboardRoot = Join-Path $repositoryRoot 'src/DBNotifier.Dashboard.Web'
$offlineNuGetConfigPath = Join-Path $PSScriptRoot 'NuGet.Offline.config'
. (Join-Path $PSScriptRoot 'toolchain-version-policy.ps1')
. (Join-Path $PSScriptRoot 'development-environment.ps1')
if ([string]::IsNullOrWhiteSpace($DiagnosticRoot)) {
    $DiagnosticRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
        "dbnotifier-ci-$([guid]::NewGuid().ToString('N'))")
}

if ([string]::IsNullOrEmpty($InternalExecutionToken)) {
    $pwshPath = (Get-Process -Id $PID).Path
    $childToken = [guid]::NewGuid().ToString('N')
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $pwshPath
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.UseShellExecute = $false
    foreach ($argument in @(
            '-NoLogo',
            '-NoProfile',
            '-NonInteractive',
            '-File',
            $PSCommandPath,
            '-Stage',
            $Stage,
            '-NodePath',
            $NodePath,
            '-NpmPath',
            $NpmPath,
            '-DiagnosticRoot',
            $DiagnosticRoot,
            '-InternalExecutionToken',
            $childToken)) {
        $startInfo.ArgumentList.Add($argument)
    }
    if (-not [string]::IsNullOrWhiteSpace($DotNetPath)) {
        $startInfo.ArgumentList.Add('-DotNetPath')
        $startInfo.ArgumentList.Add($DotNetPath)
    }
    if ($Offline) {
        $startInfo.ArgumentList.Add('-Offline')
    }

    Remove-DBNotifierInheritedEnvironment -StartInfo $startInfo
    $startInfo.Environment['DBNOTIFIER_CI_CHILD_TOKEN'] = $childToken
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw 'ISOLATION_FAILURE: The isolated canonical-gate process did not start.'
        }
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) {
            throw "The isolated canonical gate failed with exit code $($process.ExitCode)."
        }
    }
    finally {
        $process.Dispose()
    }
    return
}

$expectedChildToken = [System.Environment]::GetEnvironmentVariable(
    'DBNOTIFIER_CI_CHILD_TOKEN',
    [System.EnvironmentVariableTarget]::Process)
if ([string]::IsNullOrEmpty($expectedChildToken) -or
    -not $expectedChildToken.Equals(
        $InternalExecutionToken,
        [System.StringComparison]::Ordinal)) {
    throw 'ISOLATION_FAILURE: Internal canonical-gate execution requires the isolated child-process token.'
}
[System.Environment]::SetEnvironmentVariable(
    'DBNOTIFIER_CI_CHILD_TOKEN',
    $null,
    [System.EnvironmentVariableTarget]::Process)

$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
if ($Offline) {
    $env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = '1'
}

function Assert-LastExitCode {
    <#
    .SYNOPSIS
    Fails the canonical gate when the immediately preceding native application failed.

    .PARAMETER Operation
    Sanitised operation name used in the failure message.

    .OUTPUTS
    None.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$Operation
    )

    if ($LASTEXITCODE -ne 0) {
        throw "$Operation failed with exit code $LASTEXITCODE."
    }
}

function Resolve-RequiredApplication {
    <#
    .SYNOPSIS
    Resolves one required native application without invoking a shell.

    .PARAMETER Candidate
    Executable name or explicit path.

    .PARAMETER Operation
    Sanitised tool name used in diagnostics.

    .OUTPUTS
    System.String absolute executable path.
    #>
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$Candidate,

        [Parameter(Mandatory)]
        [string]$Operation
    )

    if ([System.IO.Path]::IsPathRooted($Candidate) -or
        $Candidate.Contains([System.IO.Path]::DirectorySeparatorChar) -or
        $Candidate.Contains([System.IO.Path]::AltDirectorySeparatorChar)) {
        if (-not (Test-Path -LiteralPath $Candidate -PathType Leaf)) {
            throw "DEPENDENCY_UNREADY: $Operation is unavailable at the configured path."
        }
        return (Resolve-Path -LiteralPath $Candidate).Path
    }
    $application = Get-Command $Candidate -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -eq $application) {
        throw "DEPENDENCY_UNREADY: $Operation is unavailable. Provision a compatible repository toolchain and retry."
    }
    return $application.Source
}

function Resolve-CompatibleDotNetHost {
    <#
    .SYNOPSIS
    Resolves and validates the single dotnet host selected by the caller or PATH.

    .PARAMETER Candidate
    Optional executable name or explicit path supplied by CI or the caller.

    .OUTPUTS
    System.String absolute dotnet host path.

    .NOTES
    The selected host is invoked once with --version from the repository root.
    A failed command, malformed version or incompatible result stops immediately.
    #>
    [OutputType([string])]
    param(
        [string]$Candidate
    )

    $policy = Get-DBNotifierDotNetSdkPolicy `
        -GlobalJsonPath (Join-Path $repositoryRoot 'global.json')
    $candidateName = if ([string]::IsNullOrWhiteSpace($Candidate)) { 'dotnet' } else { $Candidate }
    $dotnetHost = Resolve-RequiredApplication -Candidate $candidateName -Operation '.NET'
    Push-Location $repositoryRoot
    try {
        $versionOutput = @(& $dotnetHost --version)
        Assert-LastExitCode -Operation '.NET SDK version discovery'
        $observedVersion = ($versionOutput -join '').Trim()
        Assert-DBNotifierVersionInRange `
            -Version $observedVersion `
            -Range $policy.Range `
            -ToolName '.NET SDK'
    }
    finally {
        Pop-Location
    }
    return $dotnetHost
}

function Assert-DashboardToolchain {
    <#
    .SYNOPSIS
    Verifies the compatible Node.js and npm versions owned by the Dashboard package contract.

    .PARAMETER NodeExecutable
    Exact Node.js executable path.

    .PARAMETER NpmExecutable
    Exact npm executable path.

    .OUTPUTS
    None.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$NodeExecutable,

        [Parameter(Mandatory)]
        [string]$NpmExecutable
    )

    $policy = Get-DBNotifierDashboardToolchainPolicy -RepositoryRoot $repositoryRoot
    $nodeVersion = ((@(& $NodeExecutable --version)) -join '').Trim().TrimStart('v')
    Assert-LastExitCode -Operation 'Node.js version discovery'
    $npmVersion = ((@(& $NpmExecutable --version)) -join '').Trim()
    Assert-LastExitCode -Operation 'npm version discovery'
    Assert-DBNotifierVersionInRange `
        -Version $nodeVersion `
        -Range $policy.NodeRange `
        -ToolName 'Node.js'
    Assert-DBNotifierVersionInRange `
        -Version $npmVersion `
        -Range $policy.NpmRange `
        -ToolName 'npm'
}

function Invoke-PolicyChecks {
    <#
    .SYNOPSIS
    Runs repository-method, script-syntax and process-boundary policy tests.

    .PARAMETER NodeExecutable
    Compatible Node.js executable used by the dynamic syntax gate.

    .OUTPUTS
    Policy-test output.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$NodeExecutable
    )

    & (Join-Path $PSScriptRoot 'verify-development-flow.ps1') -RepositoryRoot $repositoryRoot
    & (Join-Path $repositoryRoot 'tests/DBNotifier.DevelopmentFlow.Tests.ps1')
    & (Join-Path $PSScriptRoot 'verify-script-syntax.ps1') -NodePath $NodeExecutable
    & (Join-Path $repositoryRoot 'tests/DBNotifier.RunnerProcess.Tests.ps1')
    & (Join-Path $repositoryRoot 'tests/DBNotifier.ScriptSyntax.Tests.ps1')
}

function Invoke-DotNetChecks {
    <#
    .SYNOPSIS
    Runs the locked restore, build, tests, coverage, formatting and .NET audits.

    .PARAMETER DotNetExecutable
    Compatible dotnet host selected under the repository SDK policy.

    .OUTPUTS
    Native .NET and repository-check output.

    .NOTES
    Online vulnerability freshness is explicitly omitted and reported during offline execution.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$DotNetExecutable
    )

    $lockfileArguments = @{
        DotNetPath = $DotNetExecutable
        SolutionPath = $solutionPath
    }
    if ($Offline) {
        $lockfileArguments['NuGetConfigPath'] = $offlineNuGetConfigPath
    }
    & (Join-Path $PSScriptRoot 'verify-dotnet-lockfiles.ps1') @lockfileArguments

    & $DotNetExecutable build $solutionPath --configuration Release --no-restore
    Assert-LastExitCode -Operation '.NET Release build'
    & $DotNetExecutable test $solutionPath --configuration Release --no-build --no-restore
    Assert-LastExitCode -Operation '.NET tests'
    & (Join-Path $PSScriptRoot 'verify-dotnet-coverage.ps1') -DotNetPath $DotNetExecutable
    & $DotNetExecutable format $solutionPath --verify-no-changes --no-restore
    Assert-LastExitCode -Operation '.NET format verification'

    if ($Offline) {
        Write-Output 'NOT_RUN|nuget-vulnerability-audit|reason=online registry metadata is unavailable in offline mode'
    }
    else {
        & (Join-Path $PSScriptRoot 'verify-nuget-vulnerabilities.ps1') -DotNetPath $DotNetExecutable
    }
    & (Join-Path $PSScriptRoot 'audit-fail-closed-runtime.ps1') -DotNetPath $DotNetExecutable
}

function Invoke-LegacyChecks {
    <#
    .SYNOPSIS
    Runs Windows PowerShell compatibility, legacy regression and packaging-entry validation.

    .OUTPUTS
    Legacy policy and regression output.
    #>
    param()

    $windowsPowerShell = Resolve-RequiredApplication -Candidate 'powershell.exe' -Operation 'Windows PowerShell 5.1'
    & $windowsPowerShell -NoProfile -NonInteractive -ExecutionPolicy Bypass `
        -File (Join-Path $PSScriptRoot 'verify-script-syntax.ps1') `
        -PowerShellOnly `
        -LegacyCompatibleOnly
    Assert-LastExitCode -Operation 'Windows PowerShell-compatible script syntax'
    & $windowsPowerShell -NoProfile -NonInteractive -ExecutionPolicy Bypass `
        -File (Join-Path $PSScriptRoot 'run-legacy-tests.ps1')
    Assert-LastExitCode -Operation 'Legacy compatibility tests'
    & (Join-Path $repositoryRoot 'build/build.ps1') -ValidateOnly
}

function Invoke-DashboardChecks {
    <#
    .SYNOPSIS
    Runs the locked Dashboard restore, generated-asset checks, static checks, tests, build and dependency audit.

    .PARAMETER NpmExecutable
    Exact npm executable path.

    .OUTPUTS
    Dashboard check output and explicit offline audit disposition.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$NpmExecutable
    )

    Push-Location $dashboardRoot
    try {
        & $NpmExecutable run toolchain:verify
        Assert-LastExitCode -Operation 'Dashboard toolchain policy'
        if ($Offline) {
            & $NpmExecutable ci --offline --ignore-scripts --no-audit --no-fund
            Assert-LastExitCode -Operation 'Offline Dashboard locked restore'
        }
        else {
            & $NpmExecutable ci --ignore-scripts --no-audit --no-fund
            Assert-LastExitCode -Operation 'Dashboard locked restore'
        }
        foreach ($npmScript in @(
                'brand:verify',
                'provider-icons:verify',
                'tokens:verify',
                'localisation:verify',
                'check',
                'comments:verify',
                'markdown:verify',
                'test',
                'build')) {
            & $NpmExecutable run $npmScript
            Assert-LastExitCode -Operation "Dashboard $npmScript"
        }
        if ($Offline) {
            Write-Output 'NOT_RUN|npm-vulnerability-audit|reason=online registry metadata is unavailable in offline mode'
        }
        else {
            & $NpmExecutable audit --audit-level=high
            Assert-LastExitCode -Operation 'Dashboard dependency audit'
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-RuntimeChecks {
    <#
    .SYNOPSIS
    Runs the existing bounded Dashboard browser and STATE-06 sandbox gates sequentially.

    .PARAMETER DotNetExecutable
    Exact dotnet host used by the STATE-06 sandbox.

    .PARAMETER EvidenceRoot
    Caller-owned root for sanitised failure diagnostics.

    .OUTPUTS
    Existing runtime-gate output.

    .NOTES
    These gates use only their established local sandbox boundaries and must clean up their owned resources.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$DotNetExecutable,

        [Parameter(Mandatory)]
        [string]$EvidenceRoot
    )

    $dashboardEvidence = Join-Path $EvidenceRoot 'state05-dashboard'
    $state06Evidence = Join-Path $EvidenceRoot 'state06-consolidated'
    & (Join-Path $PSScriptRoot 'run-state05-dashboard-audit.ps1') `
        -DiagnosticDirectory $dashboardEvidence
    & (Join-Path $PSScriptRoot 'run-state06-consolidated-e2e.ps1') `
        -DotNetPath $DotNetExecutable `
        -DiagnosticDirectory $state06Evidence
}

function Invoke-RepositoryClosureChecks {
    <#
    .SYNOPSIS
    Runs Git hygiene and object-integrity checks after all executable gates.

    .PARAMETER GitExecutable
    Exact Git executable path.

    .OUTPUTS
    Git hygiene and object-integrity output.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$GitExecutable
    )

    & $GitExecutable -C $repositoryRoot diff --check
    Assert-LastExitCode -Operation 'Git diff hygiene'
    & $GitExecutable -C $repositoryRoot fsck --full
    Assert-LastExitCode -Operation 'Git object integrity'
}

try {
    Push-Location $repositoryRoot
    try {
        & (Join-Path $PSScriptRoot 'assert-dbnotifier-shutdown.ps1') -RepositoryRoot $repositoryRoot

        $gitExecutable = Resolve-RequiredApplication -Candidate 'git' -Operation 'Git'
        & (Join-Path $PSScriptRoot 'verify-secrets.ps1')
        $nodeExecutable = Resolve-RequiredApplication -Candidate $NodePath -Operation 'Node.js'
        $npmExecutable = Resolve-RequiredApplication -Candidate $NpmPath -Operation 'npm'
        Assert-DashboardToolchain -NodeExecutable $nodeExecutable -NpmExecutable $npmExecutable
        Invoke-PolicyChecks -NodeExecutable $nodeExecutable

        if ($Stage -eq 'Dashboard') {
            Invoke-DashboardChecks -NpmExecutable $npmExecutable
            & $gitExecutable -C $repositoryRoot diff --check
            Assert-LastExitCode -Operation 'Git diff hygiene'
            $dashboardDisposition = if ($Offline) { 'PARTIAL' } else { 'PASS' }
            Write-Output "DISPOSITION|$dashboardDisposition|stage=Dashboard|offline=$($Offline.IsPresent.ToString().ToLowerInvariant())"
            return
        }

        if (-not $IsWindows) {
            throw 'DEPENDENCY_UNREADY: The canonical All gate requires Windows for WPF, legacy compatibility and the existing runtime matrices.'
        }
        $dotnetExecutable = Resolve-CompatibleDotNetHost -Candidate $DotNetPath
        Invoke-DotNetChecks -DotNetExecutable $dotnetExecutable
        Invoke-LegacyChecks
        Invoke-DashboardChecks -NpmExecutable $npmExecutable
        Invoke-RuntimeChecks -DotNetExecutable $dotnetExecutable -EvidenceRoot $DiagnosticRoot
        Invoke-RepositoryClosureChecks -GitExecutable $gitExecutable

        if ($Offline) {
            Write-Output 'DISPOSITION|PARTIAL|stage=All|reason=online NuGet and npm advisory freshness checks were NOT_RUN'
        }
        else {
            Write-Output 'DISPOSITION|PASS|stage=All'
        }
    }
    finally {
        Pop-Location
    }
}
catch {
    $failureMessage = [string]$_.Exception.Message
    $stopCodeMatch = [regex]::Match($failureMessage, '^(?<code>[A-Z_]+):')
    $stopCode = if ($stopCodeMatch.Success) {
        $stopCodeMatch.Groups['code'].Value
    }
    else {
        'GATE_FAILURE'
    }
    $failureDisposition = if ($stopCode -eq 'GATE_FAILURE') { 'FAIL' } else { 'BLOCKED' }
    $canonicalStage = if ($Stage -eq 'Dashboard') { 'Dashboard' } else { 'All' }
    Write-Output "DISPOSITION|$failureDisposition|stage=$canonicalStage|stop=$stopCode"
    throw
}
