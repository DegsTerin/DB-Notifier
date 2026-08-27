# Module purpose: Provides one fail-closed entry point for DB-Notifier development preparation and validation.
#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('Doctor', 'Setup', 'Quick', 'Full')]
    [string]$Task = 'Doctor',

    [switch]$Offline,

    [switch]$PlanOnly,

    [Parameter(DontShow)]
    [string]$InternalExecutionToken
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$solutionPath = Join-Path $repositoryRoot 'DBNotifier.sln'
$dashboardRoot = Join-Path $repositoryRoot 'src/DBNotifier.Dashboard.Web'
$offlineNuGetConfigPath = Join-Path $PSScriptRoot 'NuGet.Offline.config'
. (Join-Path $PSScriptRoot 'development-environment.ps1')
$script:gitPath = $null
$script:dotnetPath = $null
$script:nodePath = $null
$script:npmPath = $null

function Get-DevelopmentPlan {
    <#
    .SYNOPSIS
    Produces the exact ordered development plan without executing a technical action.

    .PARAMETER SelectedTask
    Named development workflow task.

    .PARAMETER UseOfflineMode
    Indicates whether the supported task must avoid package-source network access.

    .OUTPUTS
    System.String records forming the deterministic versioned plan.
    #>
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$SelectedTask,

        [Parameter(Mandatory)]
        [bool]$UseOfflineMode
    )

    $mode = if ($UseOfflineMode) { 'Offline' } else { 'Online' }
    $classification = switch ($SelectedTask) {
        'Quick' { 'NON_GATE' }
        'Full' { 'GATE' }
        default { 'WORKFLOW' }
    }
    $steps = @(switch ($SelectedTask) {
            'Doctor' {
                @(
                    'Assert the mandatory DB-Notifier shutdown preflight',
                    'Validate repository root and required files',
                    'Validate PowerShell, Git, .NET, Node.js and npm toolchains',
                    'Validate tracked NuGet and npm lock files',
                    'Report whether restored dependencies are ready'
                )
            }
            'Setup' {
                $dotnetRestore = if ($UseOfflineMode) {
                    'dotnet restore DBNotifier.sln --configfile scripts/NuGet.Offline.config --locked-mode'
                }
                else {
                    'dotnet restore DBNotifier.sln --locked-mode'
                }
                $npmRestore = if ($UseOfflineMode) {
                    'npm ci --offline --ignore-scripts --no-audit --no-fund'
                }
                else {
                    'npm ci --ignore-scripts --no-audit --no-fund'
                }
                @(
                    'Assert the mandatory DB-Notifier shutdown preflight',
                    'Validate repository root, toolchains and lock files',
                    $dotnetRestore,
                    $npmRestore,
                    'Verify lockfile stability and restored dependency readiness'
                )
            }
            'Quick' {
                @(
                    'Assert the mandatory DB-Notifier shutdown preflight',
                    'Validate repository root, toolchains, lock files and restored dependencies',
                    'dotnet format DBNotifier.sln --verify-no-changes --no-restore',
                    'dotnet build DBNotifier.sln --configuration Release --no-restore',
                    'dotnet test tests/DBNotifier.UnitTests/DBNotifier.UnitTests.csproj --configuration Release --no-build --no-restore',
                    'dotnet test tests/DBNotifier.Architecture.Tests/DBNotifier.Architecture.Tests.csproj --configuration Release --no-build --no-restore',
                    'Verify Dashboard assets, types, documentation and Markdown',
                    'npm test',
                    'Verify development-flow and script policies',
                    'git diff --check'
                )
            }
            'Full' {
                $ciCommand = if ($UseOfflineMode) {
                    'scripts/ci.ps1 -Offline'
                }
                else {
                    'scripts/ci.ps1'
                }
                @(
                    'Assert the mandatory DB-Notifier shutdown preflight',
                    $ciCommand)
            }
        })

    @(
        "PLAN|version=1|task=$SelectedTask|mode=$mode|classification=$classification"
        for ($index = 0; $index -lt $steps.Count; $index++) {
            "STEP|$($index + 1)|$($steps[$index])"
        }
    )
}

function Assert-LastExitCode {
    <#
    .SYNOPSIS
    Fails the workflow when the immediately preceding native application failed.

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
        throw "DEPENDENCY_UNREADY: $Operation is unavailable. Install the pinned toolchain and ensure it is on PATH."
    }
    return $application.Source
}

function Invoke-VersionCommand {
    <#
    .SYNOPSIS
    Invokes one read-only native version command and returns its trimmed output.

    .PARAMETER ApplicationPath
    Exact executable path.

    .PARAMETER Arguments
    Argument vector passed without shell interpretation.

    .PARAMETER Operation
    Sanitised operation name used in diagnostics.

    .OUTPUTS
    System.String combined standard output.
    #>
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$ApplicationPath,

        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$Operation
    )

    $output = @(& $ApplicationPath @Arguments)
    Assert-LastExitCode -Operation $Operation
    return ($output -join [System.Environment]::NewLine).Trim()
}

function Get-TrackedProjectPaths {
    <#
    .SYNOPSIS
    Discovers the tracked .NET projects owned by the repository.

    .OUTPUTS
    System.String repository-relative project paths.
    #>
    [OutputType([string])]
    param()

    $projectPaths = @(& $script:gitPath -C $repositoryRoot ls-files -- '*.csproj')
    Assert-LastExitCode -Operation 'Tracked .NET project discovery'
    if ($projectPaths.Count -eq 0) {
        throw 'No tracked .NET projects were found.'
    }
    return $projectPaths
}

function Assert-RepositoryLayout {
    <#
    .SYNOPSIS
    Verifies the exact Git root and files required by the development workflow.

    .OUTPUTS
    None.
    #>
    param()

    foreach ($requiredPath in @(
            $solutionPath,
            (Join-Path $repositoryRoot 'global.json'),
            (Join-Path $repositoryRoot '.nvmrc'),
            (Join-Path $repositoryRoot 'PLANS.md'),
            (Join-Path $dashboardRoot 'package.json'),
            (Join-Path $dashboardRoot 'package-lock.json'),
            (Join-Path $PSScriptRoot 'ci.ps1'),
            (Join-Path $PSScriptRoot 'verify-development-flow.ps1'),
            $offlineNuGetConfigPath)) {
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw 'A required development-workflow file is missing.'
        }
    }

    $script:gitPath = Resolve-RequiredApplication -Candidate 'git' -Operation 'Git'
    $gitRoot = Invoke-VersionCommand `
        -ApplicationPath $script:gitPath `
        -Arguments @('-C', $repositoryRoot, 'rev-parse', '--show-toplevel') `
        -Operation 'Git repository-root discovery'
    $resolvedGitRoot = [System.IO.Path]::GetFullPath($gitRoot)
    $comparison = if ($IsWindows) {
        [System.StringComparison]::OrdinalIgnoreCase
    }
    else {
        [System.StringComparison]::Ordinal
    }
    if (-not $resolvedGitRoot.Equals($repositoryRoot, $comparison)) {
        throw 'The development entry point is not running from its exact Git worktree root.'
    }
}

function Resolve-PinnedDotNetHost {
    <#
    .SYNOPSIS
    Finds a dotnet host that contains the exact SDK pinned by global.json.

    .PARAMETER RequiredVersion
    Exact SDK feature-band version required by the repository.

    .OUTPUTS
    System.String absolute dotnet host path.

    .NOTES
    Discovery uses only --list-sdks and does not change the machine or repository.
    #>
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$RequiredVersion
    )

    $candidates = [System.Collections.Generic.List[string]]::new()
    $workspaceDotNetLeaf = if ($IsWindows) {
        '.dotnet/dotnet.exe'
    }
    else {
        '.dotnet/dotnet'
    }
    $workspaceDotNet = Join-Path $repositoryRoot $workspaceDotNetLeaf
    if (Test-Path -LiteralPath $workspaceDotNet -PathType Leaf) {
        $candidates.Add((Resolve-Path -LiteralPath $workspaceDotNet).Path)
    }
    $pathDotNet = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -ne $pathDotNet -and -not $candidates.Contains($pathDotNet.Source)) {
        $candidates.Add($pathDotNet.Source)
    }

    foreach ($candidate in $candidates) {
        $sdkOutput = @(& $candidate --list-sdks 2>$null)
        if ($LASTEXITCODE -eq 0 -and
            @($sdkOutput | Where-Object { $_ -match "^$([regex]::Escape($RequiredVersion))\s+\[" }).Count -eq 1) {
            return $candidate
        }
    }

    throw (
        "DEPENDENCY_UNREADY: The exact .NET SDK '$RequiredVersion' pinned by global.json is unavailable. Install it without changing the repository baseline.")
}

function Assert-Toolchains {
    <#
    .SYNOPSIS
    Verifies the exact PowerShell, Git, .NET, Node.js and npm toolchain contract.

    .OUTPUTS
    None.

    .NOTES
    Successful resolution stores executable paths for later shell-free invocation in this process.
    #>
    param()

    if ($PSVersionTable.PSVersion.Major -lt 7) {
        throw "DEPENDENCY_UNREADY: PowerShell 7 or later is required; found $($PSVersionTable.PSVersion)."
    }
    if ($null -eq $script:gitPath) {
        $script:gitPath = Resolve-RequiredApplication -Candidate 'git' -Operation 'Git'
    }
    [void](Invoke-VersionCommand `
            -ApplicationPath $script:gitPath `
            -Arguments @('--version') `
            -Operation 'Git version discovery')

    $sdkPolicy = Get-Content -LiteralPath (Join-Path $repositoryRoot 'global.json') -Raw |
        ConvertFrom-Json | Select-Object -ExpandProperty sdk
    if ($sdkPolicy.rollForward -cne 'disable') {
        throw 'global.json must preserve the exact-SDK roll-forward policy.'
    }
    $script:dotnetPath = Resolve-PinnedDotNetHost -RequiredVersion ([string]$sdkPolicy.version)
    $actualDotNet = Invoke-VersionCommand `
        -ApplicationPath $script:dotnetPath `
        -Arguments @('--version') `
        -Operation '.NET SDK version discovery'
    if ($actualDotNet -cne [string]$sdkPolicy.version) {
        throw 'The selected dotnet host did not activate the exact pinned SDK.'
    }

    $script:nodePath = Resolve-RequiredApplication -Candidate 'node' -Operation 'Node.js'
    $script:npmPath = Resolve-RequiredApplication -Candidate 'npm' -Operation 'npm'
    $dashboardPackage = Get-Content -LiteralPath (Join-Path $dashboardRoot 'package.json') -Raw |
        ConvertFrom-Json
    $requiredNode = [string]$dashboardPackage.engines.node
    $requiredNpm = [string]$dashboardPackage.engines.npm
    $nvmVersion = (Get-Content -LiteralPath (Join-Path $repositoryRoot '.nvmrc') -Raw).Trim()
    if ($nvmVersion -cne $requiredNode) {
        throw '.nvmrc and the Dashboard Node.js engine must remain identical.'
    }
    $actualNode = (Invoke-VersionCommand `
            -ApplicationPath $script:nodePath `
            -Arguments @('--version') `
            -Operation 'Node.js version discovery').TrimStart('v')
    $actualNpm = Invoke-VersionCommand `
        -ApplicationPath $script:npmPath `
        -Arguments @('--version') `
        -Operation 'npm version discovery'
    if ($actualNode -cne $requiredNode) {
        throw "DEPENDENCY_UNREADY: Node.js '$actualNode' does not match the pinned version '$requiredNode'."
    }
    if ($actualNpm -cne $requiredNpm) {
        throw "DEPENDENCY_UNREADY: npm '$actualNpm' does not match the pinned version '$requiredNpm'."
    }
    if ([string]$dashboardPackage.packageManager -cne "npm@$requiredNpm") {
        throw 'The Dashboard packageManager field diverges from its npm engine.'
    }
}

function Assert-LockFiles {
    <#
    .SYNOPSIS
    Proves every tracked project and the Dashboard have tracked, parseable lock files.

    .OUTPUTS
    None.
    #>
    param()

    $trackedLockPaths = @(& $script:gitPath -C $repositoryRoot ls-files -- '*packages.lock.json' '*package-lock.json')
    Assert-LastExitCode -Operation 'Tracked lock-file discovery'
    $comparer = if ($IsWindows) {
        [System.StringComparer]::OrdinalIgnoreCase
    }
    else {
        [System.StringComparer]::Ordinal
    }
    $trackedLocks = [System.Collections.Generic.HashSet[string]]::new($comparer)
    foreach ($trackedLockPath in $trackedLockPaths) {
        [void]$trackedLocks.Add($trackedLockPath.Replace('\', '/'))
    }

    $requiredLocks = [System.Collections.Generic.List[string]]::new()
    foreach ($relativeProjectPath in Get-TrackedProjectPaths) {
        $projectDirectory = Split-Path -Parent (Join-Path $repositoryRoot $relativeProjectPath)
        $lockPath = Join-Path $projectDirectory 'packages.lock.json'
        $relativeLockPath = [System.IO.Path]::GetRelativePath(
            $repositoryRoot,
            $lockPath).Replace('\', '/')
        if (-not (Test-Path -LiteralPath $lockPath -PathType Leaf) -or
            -not $trackedLocks.Contains($relativeLockPath)) {
            throw "Project '$relativeProjectPath' has no tracked packages.lock.json."
        }
        $requiredLocks.Add($lockPath)
    }

    $npmLockPath = Join-Path $dashboardRoot 'package-lock.json'
    $relativeNpmLockPath = [System.IO.Path]::GetRelativePath(
        $repositoryRoot,
        $npmLockPath).Replace('\', '/')
    if (-not (Test-Path -LiteralPath $npmLockPath -PathType Leaf) -or
        -not $trackedLocks.Contains($relativeNpmLockPath)) {
        throw 'The Dashboard package-lock.json is missing or untracked.'
    }
    $requiredLocks.Add($npmLockPath)

    foreach ($lockPath in $requiredLocks) {
        $jsonDocument = $null
        try {
            $jsonDocument = [System.Text.Json.JsonDocument]::Parse(
                [System.IO.File]::ReadAllText($lockPath))
        }
        catch {
            throw 'A required dependency lock file contains malformed JSON.'
        }
        finally {
            if ($null -ne $jsonDocument) {
                $jsonDocument.Dispose()
            }
        }
    }
}

function Assert-DependenciesReady {
    <#
    .SYNOPSIS
    Checks whether locked .NET and npm dependencies have already been restored.

    .OUTPUTS
    None.
    #>
    param()

    $missingPaths = [System.Collections.Generic.List[string]]::new()
    foreach ($relativeProjectPath in Get-TrackedProjectPaths) {
        $assetsPath = Join-Path (
            Split-Path -Parent (Join-Path $repositoryRoot $relativeProjectPath)) 'obj/project.assets.json'
        if (-not (Test-Path -LiteralPath $assetsPath -PathType Leaf)) {
            $missingPaths.Add([System.IO.Path]::GetRelativePath($repositoryRoot, $assetsPath))
        }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $dashboardRoot 'node_modules') -PathType Container)) {
        $missingPaths.Add('src/DBNotifier.Dashboard.Web/node_modules')
    }
    if ($missingPaths.Count -gt 0) {
        throw (
            'DEPENDENCY_UNREADY: Development dependencies are not ready. Run ./scripts/development.ps1 Setup first. Missing: ' +
            ($missingPaths -join ', '))
    }
}

function Invoke-Doctor {
    <#
    .SYNOPSIS
    Runs read-only repository, toolchain, lockfile and dependency diagnostics.

    .OUTPUTS
    Human-readable PASS and FAIL records followed by a terminating failure when any check fails.
    #>
    param()

    $failures = [System.Collections.Generic.List[string]]::new()
    foreach ($check in @(
            @{ Name = 'repository root'; Action = { Assert-RepositoryLayout } },
            @{ Name = 'toolchains'; Action = { Assert-Toolchains } },
            @{ Name = 'lock files'; Action = { Assert-LockFiles } },
            @{ Name = 'restored dependencies'; Action = { Assert-DependenciesReady } })) {
        try {
            & $check.Action
            Write-Output "PASS: $($check.Name)"
        }
        catch {
            $message = [string]$_.Exception.Message
            $failures.Add("$($check.Name): $message")
            Write-Output "FAIL: $($check.Name) - $message"
        }
    }
    if ($failures.Count -gt 0) {
        throw "Doctor found $($failures.Count) problem(s). Resolve the FAIL diagnostics and retry."
    }
}

function Invoke-Setup {
    <#
    .SYNOPSIS
    Restores the exact locked dependency graph without installing external toolchains.

    .OUTPUTS
    Native restore output and a final dependency-readiness result.

    .NOTES
    Offline mode clears NuGet package sources and uses the local npm cache; unavailable packages fail closed.
    #>
    param()

    Assert-RepositoryLayout
    Assert-Toolchains
    Assert-LockFiles

    $lockfileGateArguments = @{
        DotNetPath = $script:dotnetPath
        SolutionPath = $solutionPath
    }
    if ($Offline) {
        $lockfileGateArguments['NuGetConfigPath'] = $offlineNuGetConfigPath
    }
    & (Join-Path $PSScriptRoot 'verify-dotnet-lockfiles.ps1') @lockfileGateArguments

    Push-Location $dashboardRoot
    try {
        if ($Offline) {
            & $script:npmPath ci --offline --ignore-scripts --no-audit --no-fund
            Assert-LastExitCode -Operation 'Offline Dashboard locked restore'
        }
        else {
            & $script:npmPath ci --ignore-scripts --no-audit --no-fund
            Assert-LastExitCode -Operation 'Dashboard locked restore'
        }
    }
    finally {
        Pop-Location
    }

    & $script:gitPath -C $repositoryRoot diff --exit-code -- '*packages.lock.json' 'src/DBNotifier.Dashboard.Web/package-lock.json'
    Assert-LastExitCode -Operation 'Dependency lockfile stability verification'
    Assert-LockFiles
    Assert-DependenciesReady
}

function Invoke-Quick {
    <#
    .SYNOPSIS
    Runs bounded local feedback checks that deliberately do not constitute the canonical gate.

    .OUTPUTS
    Native check output prefixed by an explicit NON_GATE disposition.

    .NOTES
    Integration, runtime, coverage and online advisory audits are deliberately excluded.
    #>
    param()

    Write-Output 'NON_GATE: Quick provides development feedback only; it is not the canonical Quality Gate.'
    Assert-RepositoryLayout
    Assert-Toolchains
    Assert-LockFiles
    Assert-DependenciesReady

    & $script:dotnetPath format $solutionPath --verify-no-changes --no-restore
    Assert-LastExitCode -Operation '.NET format verification'
    & $script:dotnetPath build $solutionPath --configuration Release --no-restore
    Assert-LastExitCode -Operation '.NET Release build'

    foreach ($testProject in @(
            'tests/DBNotifier.UnitTests/DBNotifier.UnitTests.csproj',
            'tests/DBNotifier.Architecture.Tests/DBNotifier.Architecture.Tests.csproj')) {
        & $script:dotnetPath test (Join-Path $repositoryRoot $testProject) `
            --configuration Release `
            --no-build `
            --no-restore
        Assert-LastExitCode -Operation "$testProject tests"
    }

    Push-Location $dashboardRoot
    try {
        foreach ($npmScript in @(
                'brand:verify',
                'provider-icons:verify',
                'tokens:verify',
                'localisation:verify',
                'check',
                'comments:verify',
                'markdown:verify',
                'test')) {
            & $script:npmPath run $npmScript
            Assert-LastExitCode -Operation "Dashboard $npmScript"
        }
    }
    finally {
        Pop-Location
    }

    & (Join-Path $PSScriptRoot 'verify-development-flow.ps1')
    & (Join-Path $PSScriptRoot 'verify-script-syntax.ps1') -NodePath $script:nodePath
    & (Join-Path $repositoryRoot 'tests/DBNotifier.DevelopmentFlow.Tests.ps1')
    & (Join-Path $repositoryRoot 'tests/DBNotifier.RunnerProcess.Tests.ps1')
    & (Join-Path $repositoryRoot 'tests/DBNotifier.ScriptSyntax.Tests.ps1')

    & $script:gitPath -C $repositoryRoot diff --check
    Assert-LastExitCode -Operation 'Git diff hygiene'
}

function Invoke-Full {
    <#
    .SYNOPSIS
    Delegates exactly once to the canonical aggregate repository gate.

    .OUTPUTS
    Canonical gate output and disposition.
    #>
    param()

    $ciEntrypoint = Join-Path $PSScriptRoot 'ci.ps1'
    $ciArguments = @{}
    if ($Offline) {
        $ciArguments['Offline'] = $true
    }
    & $ciEntrypoint @ciArguments
}

if ($Offline -and $Task -notin @('Setup', 'Full')) {
    throw '-Offline is supported only by Setup and Full.'
}

if ($PlanOnly) {
    Get-DevelopmentPlan -SelectedTask $Task -UseOfflineMode $Offline.IsPresent
    return
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
            '-Task',
            $Task,
            '-InternalExecutionToken',
            $childToken)) {
        $startInfo.ArgumentList.Add($argument)
    }
    if ($Offline) {
        $startInfo.ArgumentList.Add('-Offline')
    }

    Remove-DBNotifierInheritedEnvironment -StartInfo $startInfo
    $startInfo.Environment['DBNOTIFIER_DEVELOPMENT_CHILD_TOKEN'] = $childToken
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw 'ISOLATION_FAILURE: The isolated development workflow process did not start.'
        }
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) {
            throw "The isolated development workflow failed with exit code $($process.ExitCode)."
        }
    }
    finally {
        $process.Dispose()
    }
    return
}

$expectedChildToken = [System.Environment]::GetEnvironmentVariable(
    'DBNOTIFIER_DEVELOPMENT_CHILD_TOKEN',
    [System.EnvironmentVariableTarget]::Process)
if ([string]::IsNullOrEmpty($expectedChildToken) -or
    -not $expectedChildToken.Equals(
        $InternalExecutionToken,
        [System.StringComparison]::Ordinal)) {
        throw 'ISOLATION_FAILURE: Internal workflow execution requires the isolated child-process token.'
}
[System.Environment]::SetEnvironmentVariable(
    'DBNOTIFIER_DEVELOPMENT_CHILD_TOKEN',
    $null,
    [System.EnvironmentVariableTarget]::Process)

$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
if ($Offline) {
    $env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = '1'
}

Push-Location $repositoryRoot
try {
    & (Join-Path $PSScriptRoot 'assert-dbnotifier-shutdown.ps1') -RepositoryRoot $repositoryRoot
    switch ($Task) {
        'Doctor' { Invoke-Doctor }
        'Setup' { Invoke-Setup }
        'Quick' { Invoke-Quick }
        'Full' { Invoke-Full }
    }
}
finally {
    Pop-Location
}
