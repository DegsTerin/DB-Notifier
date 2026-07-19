# Module purpose: Builds and runs the exact opt-in Dashboard TV browser E2E on HTTPS loopback with isolated temporary processes and no network restore.
[CmdletBinding()]
param(
    [ValidateSet('Chrome', 'Edge')]
    [string]$BrowserProduct = 'Chrome'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dashboardRoot = Join-Path $repositoryRoot 'src\DBNotifier.Dashboard.Web'
$dashboardDist = Join-Path $dashboardRoot 'dist'
$hostProject = Join-Path $repositoryRoot 'tests\DBNotifier.DashboardTv.BrowserSandboxHost\DBNotifier.DashboardTv.BrowserSandboxHost.csproj'
$hostAssembly = Join-Path $repositoryRoot 'tests\DBNotifier.DashboardTv.BrowserSandboxHost\bin\Release\net10.0\DBNotifier.DashboardTv.BrowserSandboxHost.dll'
$auditScript = Join-Path $repositoryRoot 'scripts\audit-state06-dashboard-tv-browser.mjs'
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DBNotifier-DashboardTv-BrowserE2E-{0}" -f [Guid]::NewGuid().ToString('N'))
$hostProcess = $null
$browserProcess = $null
$debugPort = 0
$hostBaseAddress = $null
$previousBuildFlag = [Environment]::GetEnvironmentVariable('VITE_DB_NOTIFIER_TV_SANDBOX', 'Process')

# Resolves only the explicitly selected installed browser and never downloads or silently substitutes a product.
function Find-Browser([string]$Product) {
    $candidates = if ($Product -eq 'Chrome') {
        @(
            "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
            "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
            "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
        )
    }
    else {
        @(
            "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
            "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
            "$env:LOCALAPPDATA\Microsoft\Edge\Application\msedge.exe"
        )
    }
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) { return $candidate }
    }
    throw "$Product is not installed; the local browser E2E will not download a replacement."
}

# Reserves and releases one loopback port so Chromium never attaches its debugging endpoint to an unrelated listener.
function Get-AvailableLoopbackPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

# Waits for a local endpoint with bounded retries, fails immediately if its owning process exits and never follows an external fallback.
function Wait-ForLoopbackEndpoint(
    [string]$Uri,
    [System.Diagnostics.Process]$OwnedProcess,
    [string]$OwnedProcessErrorLog,
    [int]$Attempts = 80
) {
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        if ($null -ne $OwnedProcess -and $OwnedProcess.HasExited) {
            $errorDetail = if (Test-Path -LiteralPath $OwnedProcessErrorLog) {
                (Get-Content -LiteralPath $OwnedProcessErrorLog -Tail 20) -join [Environment]::NewLine
            }
            else { '' }
            throw "The owned loopback process exited before readiness. $errorDetail"
        }
        try {
            $response = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) { return }
        }
        catch { }
        Start-Sleep -Milliseconds 250
    }
    throw "The loopback endpoint did not become ready: $Uri"
}

# Stops only a process tree created by this runner and tolerates a process that has already exited.
function Stop-OwnedProcessTree([System.Diagnostics.Process]$Process) {
    if ($null -eq $Process -or $Process.HasExited) { return }
    & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
}

# Stops any verified Chromium child that still owns this runner's unique temporary profile after its parent exits.
function Stop-OwnedBrowserResidue([string]$ProfilePath) {
    Get-CimInstance Win32_Process | Where-Object {
        $_.CommandLine -and $_.CommandLine.Contains($ProfilePath, [StringComparison]::OrdinalIgnoreCase)
    } | ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }
}

# Removes only this runner's GUID-named directory after proving it remains beneath the system temporary root.
function Remove-OwnedTemporaryRoot([string]$Path) {
    $resolvedCandidate = [System.IO.Path]::GetFullPath($Path)
    $resolvedSystemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($resolvedCandidate.StartsWith($resolvedSystemTemp, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolvedCandidate).StartsWith('DBNotifier-DashboardTv-BrowserE2E-', [StringComparison]::Ordinal) -and
        (Test-Path -LiteralPath $resolvedCandidate)) {
        Remove-Item -LiteralPath $resolvedCandidate -Recurse -Force
    }
}

$browser = Find-Browser $BrowserProduct
$debugPort = Get-AvailableLoopbackPort
New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
$profilePath = Join-Path $temporaryRoot 'browser-profile'
$hostStdOut = Join-Path $temporaryRoot 'host.stdout.log'
$hostStdErr = Join-Path $temporaryRoot 'host.stderr.log'
$browserStdOut = Join-Path $temporaryRoot 'browser.stdout.log'
$browserStdErr = Join-Path $temporaryRoot 'browser.stderr.log'

try {
    [Environment]::SetEnvironmentVariable('VITE_DB_NOTIFIER_TV_SANDBOX', 'local-test', 'Process')
    & npm.cmd run build --prefix $dashboardRoot
    if ($LASTEXITCODE -ne 0) { throw 'The exact local-test Dashboard build failed.' }
    if (-not (Test-Path -LiteralPath (Join-Path $dashboardDist 'index.html') -PathType Leaf)) {
        throw 'The local-test Dashboard build did not produce dist\index.html.'
    }

    & dotnet build $hostProject --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'The browser sandbox host build failed.' }

    & dotnet $hostAssembly 2>$null | Out-Null
    if ($LASTEXITCODE -ne 2) { throw 'The browser sandbox host did not reject missing activation arguments.' }
    & dotnet $hostAssembly '--activation' 'not-authorised' '--dashboard-root' $dashboardDist 2>$null | Out-Null
    if ($LASTEXITCODE -ne 2) { throw 'The browser sandbox host did not reject an invalid activation value.' }

    $hostProcess = Start-Process -FilePath 'dotnet.exe' -ArgumentList @(
        $hostAssembly,
        '--activation', 'local-test',
        '--dashboard-root', $dashboardDist
    ) -WorkingDirectory $repositoryRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $hostStdOut -RedirectStandardError $hostStdErr

    $ready = $null
    for ($attempt = 1; $attempt -le 80; $attempt++) {
        if ($hostProcess.HasExited) {
            $hostError = if (Test-Path -LiteralPath $hostStdErr) { Get-Content -LiteralPath $hostStdErr -Raw } else { '' }
            throw "The browser sandbox host exited before readiness. $hostError"
        }
        if (Test-Path -LiteralPath $hostStdOut) {
            $line = Get-Content -LiteralPath $hostStdOut | Select-Object -Last 1
            if ($line) {
                try { $candidate = $line | ConvertFrom-Json }
                catch { $candidate = $null }
                if ($candidate.marker -eq 'DBNOTIFIER_DASHBOARD_TV_BROWSER_SANDBOX_READY') {
                    $ready = $candidate
                    break
                }
            }
        }
        Start-Sleep -Milliseconds 250
    }
    if ($null -eq $ready) { throw 'The browser sandbox host did not publish its bounded readiness record.' }

    $hostUri = [Uri]$ready.baseAddress
    if ($hostUri.Scheme -ne 'https' -or $hostUri.Host -ne '127.0.0.1' -or -not $hostUri.IsLoopback) {
        throw 'The browser sandbox host attempted to bind outside exact HTTPS loopback.'
    }
    if ([string]::IsNullOrWhiteSpace([string]$ready.spkiPin)) { throw 'The browser sandbox did not expose its non-secret certificate pin.' }
    $hostBaseAddress = $hostUri.GetLeftPart([UriPartial]::Authority)
    Write-Output "Browser sandbox host ready on exact HTTPS loopback: $hostBaseAddress"

    $browserProcess = Start-Process -FilePath $browser -ArgumentList @(
        '--headless=new',
        '--disable-gpu',
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
    ) -WindowStyle Hidden -PassThru -RedirectStandardOutput $browserStdOut -RedirectStandardError $browserStdErr
    Write-Output "Dedicated $BrowserProduct process started with an isolated ephemeral profile."
    Wait-ForLoopbackEndpoint "http://127.0.0.1:$debugPort/json/version" $browserProcess $browserStdErr

    $env:DBNOTIFIER_DASHBOARD_TV_BROWSER_CDP_ENDPOINT = "http://127.0.0.1:$debugPort"
    $env:DBNOTIFIER_DASHBOARD_TV_BROWSER_URL = $hostBaseAddress
    & node $auditScript
    if ($LASTEXITCODE -ne 0) { throw 'The Dashboard TV browser evidence audit failed.' }

    Write-Output "STATE-06 Dashboard TV browser E2E passed on $BrowserProduct using HTTPS loopback only."
}
finally {
    Remove-Item Env:DBNOTIFIER_DASHBOARD_TV_BROWSER_CDP_ENDPOINT, Env:DBNOTIFIER_DASHBOARD_TV_BROWSER_URL -ErrorAction SilentlyContinue
    [Environment]::SetEnvironmentVariable('VITE_DB_NOTIFIER_TV_SANDBOX', $previousBuildFlag, 'Process')
    Stop-OwnedProcessTree $browserProcess
    Stop-OwnedBrowserResidue $profilePath
    Stop-OwnedProcessTree $hostProcess
    Start-Sleep -Milliseconds 250

    $ownedResidue = @(Get-CimInstance Win32_Process | Where-Object {
        ($_.CommandLine -and $_.CommandLine.Contains($profilePath, [StringComparison]::OrdinalIgnoreCase)) -or
        ($_.CommandLine -and $_.CommandLine.Contains($hostAssembly, [StringComparison]::OrdinalIgnoreCase))
    })
    if ($ownedResidue.Count -gt 0) {
        throw "The local browser E2E left $($ownedResidue.Count) verified project-owned process(es) after cleanup."
    }

    Remove-OwnedTemporaryRoot $temporaryRoot
}
