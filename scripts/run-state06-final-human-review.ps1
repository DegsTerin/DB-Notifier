# Module purpose: Presents one bounded STATE-06 final human sample in dedicated visible Chrome and removes every owned local resource.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('S06-HG-001', 'S06-HG-006')]
    [string]$Sample,

    [switch]$QualityGateAutomation,

    [string]$DotNetPath = '.dotnet\dotnet.exe',

    [ValidateRange(780, 1200)]
    [int]$PresenterTimeoutSeconds = 900
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Import-Module (Join-Path $PSScriptRoot 'DBNotifier.RunnerProcess.psm1') -Force
$dashboardRoot = Join-Path $repositoryRoot 'src\DBNotifier.Dashboard.Web'
$dashboardDist = Join-Path $dashboardRoot 'dist'
$hostAssembly = Join-Path $repositoryRoot 'tests\DBNotifier.State06.ConsolidatedSandboxHost\bin\Release\net10.0\DBNotifier.State06.ConsolidatedSandboxHost.dll'
$presenter = Join-Path $repositoryRoot 'scripts\present-state06-final-human-review.mjs'
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DBNotifier-State06-HumanReview-{0}" -f [Guid]::NewGuid().ToString('N'))
$runId = [Guid]::NewGuid()
$profilePath = Join-Path $temporaryRoot 'browser-profile'
$agentRootPrefix = 'dbnotifier-state06-consolidated-sandbox-'
$ownedAgentRootPrefix = "$agentRootPrefix$($runId.ToString('N'))-"
$hostHandle = $null
$browserHandle = $null
$nodeHandle = $null
$hostProcess = $null
$browserProcess = $null
$nodeProcess = $null
$hostReadinessBudget = [TimeSpan]::FromSeconds(90)

# Resolves only an already-installed Chrome and never downloads a substitute.
function Find-DedicatedChrome {
    $candidates = @(
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
    )
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) { return $candidate }
    }
    throw 'Chrome is not installed; this offline runner will not download a replacement.'
}

# Reserves one loopback port for the dedicated Chrome debugging endpoint.
function Get-AvailableLoopbackPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally { $listener.Stop() }
}

# Waits for one owned loopback endpoint and emits only a sanitised failure category.
function Wait-ForLoopbackEndpoint(
    [string]$Uri,
    [System.Diagnostics.Process]$OwnedProcess,
    [int]$Attempts = 80
) {
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        if ($OwnedProcess.HasExited) { throw 'The owned Chrome process exited before its local debugging endpoint became ready.' }
        try {
            $response = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) { return }
        }
        catch { }
        Start-Sleep -Milliseconds 250
    }
    throw 'The owned Chrome debugging endpoint did not become ready within its budget.'
}

# Mirrors newly completed presenter lines while retaining the module-owned evidence capture.
function Write-NewPresenterLines(
    [string]$Path,
    [ref]$NextCharacter,
    [switch]$StandardError,
    [switch]$EndOfStream
) {
    <#
    .SYNOPSIS
    Relays only newly completed lines from one visible-review presenter stream.

    .PARAMETER Path
    Incremental evidence file owned by this runner.

    .PARAMETER NextCharacter
    Caller-owned character cursor advanced only beyond complete emitted lines.

    .PARAMETER StandardError
    Writes new lines to the parent error stream instead of standard output.

    .PARAMETER EndOfStream
    Emits a final unterminated line after the child and capture have completed.

    .OUTPUTS
    None.

    .NOTES
    Sharing races and partial lines are retained for the next bounded poll. The presenter emits only sanitised
    synthetic evidence.
    #>
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return }
    try {
        $stream = [System.IO.FileStream]::new(
            $Path,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::ReadWrite)
        $reader = [System.IO.StreamReader]::new(
            $stream,
            [System.Text.UTF8Encoding]::new($false),
            $true)
        try {
            $capturedText = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    catch [System.IO.IOException] {
        return
    }
    $cursor = [int]$NextCharacter.Value
    if ($cursor -ge $capturedText.Length) { return }
    $remainingText = $capturedText.Substring($cursor)
    $completedLength = if ($EndOfStream) {
        $remainingText.Length
    }
    else {
        $lastLineFeed = $remainingText.LastIndexOf("`n", [StringComparison]::Ordinal)
        if ($lastLineFeed -lt 0) { return }
        $lastLineFeed + 1
    }
    $completedText = $remainingText.Substring(0, $completedLength)
    $completedReader = [System.IO.StringReader]::new($completedText)
    try {
        while ($null -ne ($line = $completedReader.ReadLine())) {
            if ($StandardError) {
                [Console]::Error.WriteLine($line)
            }
            else {
                Write-Output $line
            }
        }
    }
    finally {
        $completedReader.Dispose()
    }
    $NextCharacter.Value = $cursor + $completedLength
}

# Stops only a process tree created by this runner, waits for bounded exit and throws if cleanup cannot be proved.
function Stop-OwnedProcessTree([System.Diagnostics.Process]$Process, [int]$TimeoutMilliseconds = 15000) {
    if ($null -eq $Process -or $Process.HasExited) { return }
    & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
    if (-not $Process.WaitForExit($TimeoutMilliseconds)) {
        throw "Owned process $($Process.Id) did not exit within the cleanup budget."
    }
}

# Stops only Chrome children carrying this run's unique profile path.
function Stop-OwnedBrowserResidue([string]$OwnedProfilePath) {
    Get-CimInstance Win32_Process | Where-Object {
        $_.CommandLine -and $_.CommandLine.Contains($OwnedProfilePath, [StringComparison]::OrdinalIgnoreCase)
    } | ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }
}

# Removes only this runner's exact GUID-named child of the operating-system temporary directory.
function Remove-OwnedTemporaryRoot([string]$Path) {
    $candidate = [System.IO.Path]::GetFullPath($Path)
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($candidate.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $candidate).StartsWith('DBNotifier-State06-HumanReview-', [StringComparison]::Ordinal) -and
        (Test-Path -LiteralPath $candidate)) {
        Remove-Item -LiteralPath $candidate -Recurse -Force
    }
}

# Removes only Agent roots carrying this review runner's exact correlation identifier.
function Remove-OwnedAgentRoots([string]$OwnedPrefix) {
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    $ownedRoots = @(Get-ChildItem -Path $systemTemp -Directory -Filter ($OwnedPrefix + '*') -ErrorAction SilentlyContinue)
    foreach ($root in $ownedRoots) {
        $candidate = [System.IO.Path]::GetFullPath($root.FullName)
        if (-not $candidate.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase) -or
            -not $root.Name.StartsWith($OwnedPrefix, [StringComparison]::Ordinal)) {
            throw 'An Agent sandbox root failed the exact per-run cleanup ownership check.'
        }
        [System.IO.Directory]::Delete($candidate, $true)
    }
    $remaining = @(Get-ChildItem -Path $systemTemp -Directory -Filter ($OwnedPrefix + '*') -ErrorAction SilentlyContinue)
    if ($remaining.Count -ne 0) { throw "The review runner left $($remaining.Count) exact owned Agent sandbox root(s)." }
}

if (-not (Test-Path -LiteralPath $hostAssembly -PathType Leaf)) {
    throw 'The already-built consolidated host is unavailable; build it in a separately authorised technical gate.'
}
if (-not (Test-Path -LiteralPath (Join-Path $dashboardDist 'index.html') -PathType Leaf)) {
    throw 'The already-built Dashboard is unavailable; build it in a separately authorised technical gate.'
}
if (-not (Test-Path -LiteralPath $presenter -PathType Leaf)) {
    throw 'The versioned visible presenter is unavailable.'
}

$resolvedDotNet = Resolve-DBNotifierRunnerExecutable -Candidate $DotNetPath -BaseDirectory $repositoryRoot
$resolvedNpm = Resolve-DBNotifierRunnerExecutable -Candidate 'npm.cmd' -BaseDirectory $repositoryRoot
$resolvedNode = Resolve-DBNotifierRunnerExecutable -Candidate 'node' -BaseDirectory $repositoryRoot
$chrome = Find-DedicatedChrome
$debugPort = Get-AvailableLoopbackPort
New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
$hostStdOut = Join-Path $temporaryRoot 'host.stdout.log'
$hostStdErr = Join-Path $temporaryRoot 'host.stderr.log'
$browserStdOut = Join-Path $temporaryRoot 'browser.stdout.log'
$browserStdErr = Join-Path $temporaryRoot 'browser.stderr.log'
$nodeStdOut = Join-Path $temporaryRoot 'node.stdout.log'
$nodeStdErr = Join-Path $temporaryRoot 'node.stderr.log'

try {
    & $resolvedNpm run toolchain:verify --prefix $dashboardRoot
    if ($LASTEXITCODE -ne 0) { throw 'The pinned Dashboard toolchain verification failed.' }

    $hostHandle = Start-DBNotifierRunnerProcess `
        -FilePath $resolvedDotNet `
        -ArgumentList @(
            $hostAssembly,
            '--activation', 'state06-final-human-samples-remediation',
            '--dashboard-root', $dashboardDist,
            '--run-id', $runId.ToString('D'),
            '--sample', $Sample
        ) `
        -WorkingDirectory $repositoryRoot `
        -StandardOutputPath $hostStdOut `
        -StandardErrorPath $hostStdErr
    $hostProcess = $hostHandle.Process

    $ready = $null
    $readiness = [System.Diagnostics.Stopwatch]::StartNew()
    while ($readiness.Elapsed -lt $hostReadinessBudget) {
        if ($hostProcess.HasExited) { throw 'The test-only review host exited before readiness.' }
        if (Test-Path -LiteralPath $hostStdOut) {
            foreach ($line in Get-Content -LiteralPath $hostStdOut) {
                try { $candidate = $line | ConvertFrom-Json }
                catch { $candidate = $null }
                if ($candidate.marker -eq 'DBNOTIFIER_STATE06_CONSOLIDATED_SANDBOX_READY') {
                    $ready = $candidate
                    break
                }
            }
        }
        if ($null -ne $ready) { break }
        Start-Sleep -Milliseconds 250
    }
    $readiness.Stop()
    if ($null -eq $ready) { throw 'The test-only review host did not publish its bounded readiness record.' }

    $hostUri = [Uri]$ready.baseAddress
    if ($hostUri.Scheme -ne 'https' -or $hostUri.Host -ne '127.0.0.1' -or -not $hostUri.IsLoopback) {
        throw 'The review host attempted to bind outside exact HTTPS loopback.'
    }
    if ([string]$ready.humanReviewSample -ne $Sample) { throw 'The host did not bind the requested single sample.' }
    if ([string]::IsNullOrWhiteSpace([string]$ready.spkiPin)) { throw 'The host exposed no ephemeral certificate pin.' }
    if (-not [Guid]::TryParseExact([string]$ready.runId, 'D', [ref]([Guid]::Empty))) { throw 'The host exposed no valid run identifier.' }
    if ([string]$ready.runId -ne $runId.ToString('D')) { throw 'The review host did not preserve the runner-owned correlation identifier.' }
    $hostBaseAddress = $hostUri.GetLeftPart([UriPartial]::Authority)

    $browserHandle = Start-DBNotifierRunnerProcess `
        -FilePath $chrome `
        -ArgumentList @(
            '--new-window',
            '--disable-background-networking',
            '--disable-component-update',
            '--disable-default-apps',
            '--disable-sync',
            '--metrics-recording-only',
            '--no-first-run',
            '--no-default-browser-check',
            '--no-pings',
            '--no-proxy-server',
            '--remote-debugging-address=127.0.0.1',
            "--remote-debugging-port=$debugPort",
            "--user-data-dir=$profilePath",
            "--ignore-certificate-errors-spki-list=$($ready.spkiPin)",
            '--host-resolver-rules=MAP * ~NOTFOUND, EXCLUDE localhost, EXCLUDE 127.0.0.1',
            "$hostBaseAddress/#overview"
        ) `
        -WorkingDirectory $repositoryRoot `
        -StandardOutputPath $browserStdOut `
        -StandardErrorPath $browserStdErr `
        -Visible
    $browserProcess = $browserHandle.Process
    Wait-ForLoopbackEndpoint "http://127.0.0.1:$debugPort/json/version" $browserProcess

    $env:DBNOTIFIER_STATE06_CDP_ENDPOINT = "http://127.0.0.1:$debugPort"
    $env:DBNOTIFIER_STATE06_URL = $hostBaseAddress
    $env:DBNOTIFIER_STATE06_RUN_ID = [string]$ready.runId
    $env:DBNOTIFIER_STATE06_REVIEW_SAMPLE = $Sample
    $env:DBNOTIFIER_STATE06_REVIEW_AUTOMATION = if ($QualityGateAutomation) { 'true' } else { 'false' }
    $nodeHandle = Start-DBNotifierRunnerProcess `
        -FilePath $resolvedNode `
        -ArgumentList @($presenter) `
        -WorkingDirectory $repositoryRoot `
        -StandardOutputPath $nodeStdOut `
        -StandardErrorPath $nodeStdErr
    $nodeProcess = $nodeHandle.Process
    $nextPresenterOutputCharacter = 0
    $nextPresenterErrorCharacter = 0
    $presenterDeadline = [DateTimeOffset]::UtcNow.AddSeconds($PresenterTimeoutSeconds)
    while (-not $nodeProcess.WaitForExit(250)) {
        Write-NewPresenterLines -Path $nodeStdOut -NextCharacter ([ref]$nextPresenterOutputCharacter)
        Write-NewPresenterLines -Path $nodeStdErr -NextCharacter ([ref]$nextPresenterErrorCharacter) -StandardError
        if ([DateTimeOffset]::UtcNow -ge $presenterDeadline) {
            throw 'The visible review presenter exceeded its global deadline.'
        }
    }
    $nodeExitCode = Complete-DBNotifierRunnerProcess -Handle $nodeHandle
    Write-NewPresenterLines -Path $nodeStdOut -NextCharacter ([ref]$nextPresenterOutputCharacter) -EndOfStream
    Write-NewPresenterLines -Path $nodeStdErr -NextCharacter ([ref]$nextPresenterErrorCharacter) -StandardError -EndOfStream
    if ($nodeExitCode -ne 0) { throw 'The versioned visible review presenter failed.' }

    if (-not $hostProcess.WaitForExit(20000)) { throw 'The review host did not honour its authenticated shutdown.' }
    if ($hostProcess.ExitCode -ne 0) { throw "The review host exited with code $($hostProcess.ExitCode)." }
    Write-Output "STATE-06 $Sample review runner completed with local synthetic data; no human decision was inferred."
}
finally {
    Remove-Item Env:DBNOTIFIER_STATE06_CDP_ENDPOINT, Env:DBNOTIFIER_STATE06_URL, Env:DBNOTIFIER_STATE06_RUN_ID, Env:DBNOTIFIER_STATE06_REVIEW_SAMPLE, Env:DBNOTIFIER_STATE06_REVIEW_AUTOMATION -ErrorAction SilentlyContinue
    $cleanupFailures = [System.Collections.Generic.List[string]]::new()
    foreach ($cleanup in @(
        {
            if ($null -eq $nodeHandle -or -not [bool]$nodeHandle.Completed) {
                Stop-OwnedProcessTree $nodeProcess
            }
        },
        { Stop-OwnedProcessTree $browserProcess },
        { Stop-OwnedBrowserResidue $profilePath },
        { Stop-OwnedProcessTree $hostProcess }
    )) {
        try { & $cleanup }
        catch { $cleanupFailures.Add($_.Exception.Message) }
    }
    Start-Sleep -Milliseconds 250

    $ownedResidue = @(Get-CimInstance Win32_Process | Where-Object {
        ($_.CommandLine -and $_.CommandLine.Contains($profilePath, [StringComparison]::OrdinalIgnoreCase)) -or
        ($_.CommandLine -and $_.CommandLine.Contains($hostAssembly, [StringComparison]::OrdinalIgnoreCase))
    })
    if ($ownedResidue.Count -gt 0) {
        $cleanupFailures.Add("The review runner left $($ownedResidue.Count) verified owned process(es).")
    }
    foreach ($handle in @($nodeHandle, $browserHandle, $hostHandle)) {
        if ($null -eq $handle) { continue }
        try { Complete-DBNotifierRunnerProcess -Handle $handle | Out-Null }
        catch { $cleanupFailures.Add($_.Exception.Message) }
    }
    try { Remove-OwnedAgentRoots $ownedAgentRootPrefix }
    catch { $cleanupFailures.Add($_.Exception.Message) }
    try { Remove-OwnedTemporaryRoot $temporaryRoot }
    catch { $cleanupFailures.Add($_.Exception.Message) }
    if ($cleanupFailures.Count -gt 0) {
        throw "STATE-06 human review cleanup failed: $($cleanupFailures -join ' ')"
    }
}
