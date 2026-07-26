# Module purpose: Builds and audits the exact O4 Observer sandbox with owned loopback processes and deterministic cleanup.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$DotNetPath = '.dotnet\dotnet.exe',

    [ValidateRange(60, 900)]
    [int]$NodeTimeoutSeconds = 600
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Import-Module (Join-Path $PSScriptRoot 'DBNotifier.RunnerProcess.psm1') -Force
$dashboardRoot = Join-Path $repositoryRoot 'src\DBNotifier.Dashboard.Web'
$dashboardDist = Join-Path $dashboardRoot 'dist'
$hostProject = Join-Path $repositoryRoot 'tests\DBNotifier.State06.ConsolidatedSandboxHost\DBNotifier.State06.ConsolidatedSandboxHost.csproj'
$hostAssembly = Join-Path $repositoryRoot 'tests\DBNotifier.State06.ConsolidatedSandboxHost\bin\Release\net10.0\DBNotifier.State06.ConsolidatedSandboxHost.dll'
$auditScript = Join-Path $repositoryRoot 'scripts\audit-o4-observer-sandbox.mjs'
$runId = [Guid]::NewGuid()
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ("DBNotifier-O4-Browser-{0}" -f $runId.ToString('N'))
$hostSandboxRoot = Join-Path ([IO.Path]::GetTempPath()) ("DBNotifier-O4-{0}" -f $runId.ToString('N'))
$previousFlag = [Environment]::GetEnvironmentVariable('VITE_DB_NOTIFIER_OBSERVER_SANDBOX', 'Process')
$hostHandle = $null
$browserHandle = $null
$nodeHandle = $null
$hostProcess = $null
$browserProcess = $null
$nodeProcess = $null
$nodeEvidenceRelayed = $false

# Resolves the already installed Chrome binary without downloading a replacement.
function Find-Chrome {
    $candidates = @(
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
    )
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) { return $candidate }
    }
    throw 'Chrome is not installed; O4 will not download a browser.'
}

# Reserves one loopback port for the owned CDP endpoint.
function Get-LoopbackPort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally { $listener.Stop() }
}

# Relays only the completed, sanitised JSON evidence emitted by the versioned Node auditor.
function Write-CompletedNodeEvidence([string]$StandardOutputPath, [string]$StandardErrorPath) {
    <#
    .SYNOPSIS
    Relays completed O4 auditor evidence before its owned temporary root is removed.

    .PARAMETER StandardOutputPath
    Completed standard-output evidence file.

    .PARAMETER StandardErrorPath
    Completed standard-error diagnostic file.

    .OUTPUTS
    Sanitised auditor JSON on standard output; diagnostics retain their standard-error channel.

    .NOTES
    The caller completes the shared process handle before invoking this function.
    #>
    if (Test-Path -LiteralPath $StandardOutputPath -PathType Leaf) {
        Get-Content -LiteralPath $StandardOutputPath | ForEach-Object { Write-Output $_ }
    }
    if (Test-Path -LiteralPath $StandardErrorPath -PathType Leaf) {
        Get-Content -LiteralPath $StandardErrorPath | ForEach-Object { [Console]::Error.WriteLine($_) }
    }
}

# Stops only a process tree started by this runner.
function Stop-OwnedTree([Diagnostics.Process]$Process) {
    if ($null -ne $Process -and -not $Process.HasExited) {
        & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
        try { $Process.WaitForExit(5000) | Out-Null }
        catch { }
    }
}

# Removes only this GUID-owned O4 browser root beneath the system temporary directory.
function Remove-OwnedRoot([string]$Path) {
    $candidate = [IO.Path]::GetFullPath($Path)
    $temporary = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if ($candidate.StartsWith($temporary, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $candidate).StartsWith('DBNotifier-O4-Browser-', [StringComparison]::Ordinal) -and
        (Test-Path -LiteralPath $candidate)) {
        for ($attempt = 1; $attempt -le 20; $attempt++) {
            try {
                Remove-Item -LiteralPath $candidate -Recurse -Force
                return
            }
            catch {
                if ($attempt -ge 20) { throw }
                Start-Sleep -Milliseconds 100
            }
        }
    }
}

# Removes the exact host-side synthetic O4 root bound to this runner identifier.
function Remove-OwnedHostRoot([string]$Path, [Guid]$ExpectedRunId) {
    $candidate = [IO.Path]::GetFullPath($Path)
    $temporary = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    $expectedLeaf = "DBNotifier-O4-{0}" -f $ExpectedRunId.ToString('N')
    if ($candidate.StartsWith($temporary, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $candidate).Equals($expectedLeaf, [StringComparison]::Ordinal) -and
        (Test-Path -LiteralPath $candidate)) {
        Remove-Item -LiteralPath $candidate -Recurse -Force
    }
}

$resolvedDotNet = Resolve-DBNotifierRunnerExecutable -Candidate $DotNetPath -BaseDirectory $repositoryRoot
$resolvedNpm = Resolve-DBNotifierRunnerExecutable -Candidate 'npm.cmd' -BaseDirectory $repositoryRoot
$resolvedNode = Resolve-DBNotifierRunnerExecutable -Candidate 'node' -BaseDirectory $repositoryRoot
$chrome = Find-Chrome
$debugPort = Get-LoopbackPort
New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
$profile = Join-Path $temporaryRoot 'profile'
$hostOut = Join-Path $temporaryRoot 'host.stdout.log'
$hostError = Join-Path $temporaryRoot 'host.stderr.log'
$browserOut = Join-Path $temporaryRoot 'browser.stdout.log'
$browserError = Join-Path $temporaryRoot 'browser.stderr.log'
$nodeOut = Join-Path $temporaryRoot 'node.stdout.log'
$nodeError = Join-Path $temporaryRoot 'node.stderr.log'

try {
    [Environment]::SetEnvironmentVariable('VITE_DB_NOTIFIER_OBSERVER_SANDBOX', 'local-test', 'Process')
    & $resolvedNpm run toolchain:verify --prefix $dashboardRoot
    if ($LASTEXITCODE -ne 0) { throw 'The pinned Dashboard toolchain verification failed.' }
    & $resolvedNpm run build --prefix $dashboardRoot
    if ($LASTEXITCODE -ne 0) { throw 'The offline O4 Dashboard build failed.' }
    & $resolvedDotNet build $hostProject --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'The offline O4 host build failed.' }

    $hostHandle = Start-DBNotifierRunnerProcess `
        -FilePath $resolvedDotNet `
        -ArgumentList @(
            $hostAssembly,
            '--activation', 'o4-factual-observer-projection-sandbox',
            '--dashboard-root', $dashboardDist,
            '--run-id', $runId.ToString('D')
        ) `
        -WorkingDirectory $repositoryRoot `
        -StandardOutputPath $hostOut `
        -StandardErrorPath $hostError
    $hostProcess = $hostHandle.Process

    $ready = $null
    for ($attempt = 1; $attempt -le 80; $attempt++) {
        if ($hostProcess.HasExited) {
            $detail = if (Test-Path -LiteralPath $hostError) { Get-Content -LiteralPath $hostError -Raw } else { '' }
            throw "The O4 host exited before readiness. $detail"
        }
        if (Test-Path -LiteralPath $hostOut) {
            foreach ($line in Get-Content -LiteralPath $hostOut) {
                try {
                    $candidate = $line | ConvertFrom-Json
                    if ($candidate.marker -eq 'DBNOTIFIER_O4_OBSERVER_SANDBOX_READY') { $ready = $candidate }
                }
                catch { }
            }
        }
        if ($null -ne $ready) { break }
        Start-Sleep -Milliseconds 250
    }
    if ($null -eq $ready) { throw 'The O4 host did not publish readiness within its budget.' }

    $proxy = "http://127.0.0.1:{0}" -f (Get-LoopbackPort)
    $browserHandle = Start-DBNotifierRunnerProcess `
        -FilePath $chrome `
        -ArgumentList @(
            '--headless=new',
            "--user-data-dir=$profile",
            "--remote-debugging-port=$debugPort",
            '--remote-debugging-address=127.0.0.1',
            "--proxy-server=$proxy",
            '--proxy-bypass-list=localhost;127.0.0.1;[::1]',
            "--ignore-certificate-errors-spki-list=$($ready.spkiPin)",
            '--disable-background-networking',
            '--disable-component-update',
            '--disable-default-apps',
            '--disable-extensions',
            '--no-first-run',
            '--no-default-browser-check',
            '--window-size=1440,900',
            $ready.baseAddress
        ) `
        -WorkingDirectory $repositoryRoot `
        -StandardOutputPath $browserOut `
        -StandardErrorPath $browserError
    $browserProcess = $browserHandle.Process

    $cdp = "http://127.0.0.1:$debugPort"
    for ($attempt = 1; $attempt -le 80; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri "$cdp/json/version" -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) { break }
        }
        catch { }
        if ($browserProcess.HasExited) {
            $detail = if (Test-Path -LiteralPath $browserError) {
                (Get-Content -LiteralPath $browserError -Tail 20) -join [Environment]::NewLine
            }
            else { '' }
            throw "The dedicated Chrome process exited before CDP readiness. $detail"
        }
        Start-Sleep -Milliseconds 250
    }
    $env:DBNOTIFIER_O4_CDP_ENDPOINT = $cdp
    $env:DBNOTIFIER_O4_URL = $ready.baseAddress
    $nodeHandle = Start-DBNotifierRunnerProcess `
        -FilePath $resolvedNode `
        -ArgumentList @($auditScript) `
        -WorkingDirectory $repositoryRoot `
        -StandardOutputPath $nodeOut `
        -StandardErrorPath $nodeError
    $nodeProcess = $nodeHandle.Process
    if (-not $nodeProcess.WaitForExit($NodeTimeoutSeconds * 1000)) {
        throw 'The O4 Node/CDP audit exceeded its global deadline.'
    }
    $nodeExitCode = Complete-DBNotifierRunnerProcess -Handle $nodeHandle
    Write-CompletedNodeEvidence -StandardOutputPath $nodeOut -StandardErrorPath $nodeError
    $nodeEvidenceRelayed = $true
    if ($nodeExitCode -ne 0) { throw 'The O4 browser audit failed.' }
}
finally {
    Remove-Item Env:DBNOTIFIER_O4_CDP_ENDPOINT -ErrorAction SilentlyContinue
    Remove-Item Env:DBNOTIFIER_O4_URL -ErrorAction SilentlyContinue
    [Environment]::SetEnvironmentVariable('VITE_DB_NOTIFIER_OBSERVER_SANDBOX', $previousFlag, 'Process')
    if ($null -eq $nodeHandle -or -not [bool]$nodeHandle.Completed) {
        Stop-OwnedTree $nodeProcess
    }
    Stop-OwnedTree $browserProcess
    Get-CimInstance Win32_Process | Where-Object {
        $_.CommandLine -and $_.CommandLine.Contains($profile, [StringComparison]::OrdinalIgnoreCase)
    } | ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        try {
            [Diagnostics.Process]::GetProcessById($_.ProcessId).WaitForExit(5000) | Out-Null
        }
        catch { }
    }
    Stop-OwnedTree $hostProcess
    $cleanupFailures = [System.Collections.Generic.List[string]]::new()
    foreach ($handle in @($nodeHandle, $browserHandle, $hostHandle)) {
        if ($null -eq $handle) { continue }
        try { Complete-DBNotifierRunnerProcess -Handle $handle | Out-Null }
        catch { $cleanupFailures.Add($_.Exception.Message) }
    }
    if ($null -ne $nodeHandle -and -not $nodeEvidenceRelayed) {
        try {
            Write-CompletedNodeEvidence -StandardOutputPath $nodeOut -StandardErrorPath $nodeError
            $nodeEvidenceRelayed = $true
        }
        catch { $cleanupFailures.Add($_.Exception.Message) }
    }
    try { Remove-OwnedHostRoot $hostSandboxRoot $runId }
    catch { $cleanupFailures.Add($_.Exception.Message) }
    try { Remove-OwnedRoot $temporaryRoot }
    catch { $cleanupFailures.Add($_.Exception.Message) }
    if ($cleanupFailures.Count -gt 0) {
        throw "O4 runner cleanup failed: $($cleanupFailures -join ' ')"
    }
}
