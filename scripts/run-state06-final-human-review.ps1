# Module purpose: Presents one bounded STATE-06 final human sample in dedicated visible Chrome and removes every owned local resource.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('S06-HG-001', 'S06-HG-006')]
    [string]$Sample,

    [switch]$QualityGateAutomation
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dashboardDist = Join-Path $repositoryRoot 'src\DBNotifier.Dashboard.Web\dist'
$hostAssembly = Join-Path $repositoryRoot 'tests\DBNotifier.State06.ConsolidatedSandboxHost\bin\Release\net10.0\DBNotifier.State06.ConsolidatedSandboxHost.dll'
$presenter = Join-Path $repositoryRoot 'scripts\present-state06-final-human-review.mjs'
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DBNotifier-State06-HumanReview-{0}" -f [Guid]::NewGuid().ToString('N'))
$profilePath = Join-Path $temporaryRoot 'browser-profile'
$agentRootPrefix = 'dbnotifier-state06-consolidated-sandbox-'
$agentRootsBefore = @(Get-ChildItem -Path ([System.IO.Path]::GetTempPath()) -Directory -Filter ($agentRootPrefix + '*') -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
$hostProcess = $null
$browserProcess = $null
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

# Stops only a process tree created and retained by this runner.
function Stop-OwnedProcessTree([System.Diagnostics.Process]$Process) {
    if ($null -eq $Process -or $Process.HasExited) { return }
    & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
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

# Removes only new Agent roots created after this runner captured its immutable baseline.
function Remove-NewOwnedAgentRoots([string[]]$Baseline) {
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    $newRoots = @(Get-ChildItem -Path $systemTemp -Directory -Filter ($agentRootPrefix + '*') -ErrorAction SilentlyContinue | Where-Object {
        $Baseline -notcontains $_.FullName
    })
    foreach ($root in $newRoots) {
        $candidate = [System.IO.Path]::GetFullPath($root.FullName)
        if (-not $candidate.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase) -or
            -not $root.Name.StartsWith($agentRootPrefix, [StringComparison]::Ordinal)) {
            throw 'A new Agent sandbox root failed the exact cleanup ownership check.'
        }
        [System.IO.Directory]::Delete($candidate, $true)
    }
    $remaining = @(Get-ChildItem -Path $systemTemp -Directory -Filter ($agentRootPrefix + '*') -ErrorAction SilentlyContinue | Where-Object {
        $Baseline -notcontains $_.FullName
    })
    if ($remaining.Count -ne 0) { throw "The review runner left $($remaining.Count) new Agent sandbox root(s)." }
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

$chrome = Find-DedicatedChrome
$debugPort = Get-AvailableLoopbackPort
New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
$hostStdOut = Join-Path $temporaryRoot 'host.stdout.log'
$hostStdErr = Join-Path $temporaryRoot 'host.stderr.log'
$browserStdOut = Join-Path $temporaryRoot 'browser.stdout.log'
$browserStdErr = Join-Path $temporaryRoot 'browser.stderr.log'

try {
    $hostProcess = Start-Process -FilePath 'dotnet.exe' -ArgumentList @(
        $hostAssembly,
        '--activation', 'state06-final-human-samples-remediation',
        '--dashboard-root', $dashboardDist,
        '--sample', $Sample
    ) -WorkingDirectory $repositoryRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $hostStdOut -RedirectStandardError $hostStdErr

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
    $hostBaseAddress = $hostUri.GetLeftPart([UriPartial]::Authority)

    $browserProcess = Start-Process -FilePath $chrome -ArgumentList @(
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
        '"--host-resolver-rules=MAP * ~NOTFOUND, EXCLUDE localhost, EXCLUDE 127.0.0.1"',
        "$hostBaseAddress/#overview"
    ) -PassThru -RedirectStandardOutput $browserStdOut -RedirectStandardError $browserStdErr
    Wait-ForLoopbackEndpoint "http://127.0.0.1:$debugPort/json/version" $browserProcess

    $env:DBNOTIFIER_STATE06_CDP_ENDPOINT = "http://127.0.0.1:$debugPort"
    $env:DBNOTIFIER_STATE06_URL = $hostBaseAddress
    $env:DBNOTIFIER_STATE06_RUN_ID = [string]$ready.runId
    $env:DBNOTIFIER_STATE06_REVIEW_SAMPLE = $Sample
    $env:DBNOTIFIER_STATE06_REVIEW_AUTOMATION = if ($QualityGateAutomation) { 'true' } else { 'false' }
    & node $presenter
    if ($LASTEXITCODE -ne 0) { throw 'The versioned visible review presenter failed.' }

    if (-not $hostProcess.WaitForExit(20000)) { throw 'The review host did not honour its authenticated shutdown.' }
    if ($hostProcess.ExitCode -ne 0) { throw "The review host exited with code $($hostProcess.ExitCode)." }
    Write-Output "STATE-06 $Sample review runner completed with local synthetic data; no human decision was inferred."
}
finally {
    Remove-Item Env:DBNOTIFIER_STATE06_CDP_ENDPOINT, Env:DBNOTIFIER_STATE06_URL, Env:DBNOTIFIER_STATE06_RUN_ID, Env:DBNOTIFIER_STATE06_REVIEW_SAMPLE, Env:DBNOTIFIER_STATE06_REVIEW_AUTOMATION -ErrorAction SilentlyContinue
    Stop-OwnedProcessTree $browserProcess
    Stop-OwnedBrowserResidue $profilePath
    Stop-OwnedProcessTree $hostProcess
    Start-Sleep -Milliseconds 250

    $ownedResidue = @(Get-CimInstance Win32_Process | Where-Object {
        ($_.CommandLine -and $_.CommandLine.Contains($profilePath, [StringComparison]::OrdinalIgnoreCase)) -or
        ($_.CommandLine -and $_.CommandLine.Contains($hostAssembly, [StringComparison]::OrdinalIgnoreCase))
    })
    if ($ownedResidue.Count -gt 0) { throw "The review runner left $($ownedResidue.Count) verified owned process(es)." }
    Remove-NewOwnedAgentRoots $agentRootsBefore
    Remove-OwnedTemporaryRoot $temporaryRoot
}
