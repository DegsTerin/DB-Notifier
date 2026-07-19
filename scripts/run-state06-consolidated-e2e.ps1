# Module purpose: Runs the single correlated STATE-06 remediation harness on exact local loopback and removes every owned runtime and browser profile.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Chrome', 'Edge')]
    [string]$BrowserProduct = 'Chrome'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dashboardRoot = Join-Path $repositoryRoot 'src\DBNotifier.Dashboard.Web'
$dashboardDist = Join-Path $dashboardRoot 'dist'
$hostProject = Join-Path $repositoryRoot 'tests\DBNotifier.State06.ConsolidatedSandboxHost\DBNotifier.State06.ConsolidatedSandboxHost.csproj'
$hostAssembly = Join-Path $repositoryRoot 'tests\DBNotifier.State06.ConsolidatedSandboxHost\bin\Release\net10.0\DBNotifier.State06.ConsolidatedSandboxHost.dll'
$auditScript = Join-Path $repositoryRoot 'scripts\audit-state06-consolidated-e2e.mjs'
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DBNotifier-State06-ConsolidatedE2E-{0}" -f [Guid]::NewGuid().ToString('N'))
$hostProcess = $null
$browserProcess = $null
$profilePath = Join-Path $temporaryRoot 'browser-profile'
$previousBuildFlag = [Environment]::GetEnvironmentVariable('VITE_DB_NOTIFIER_TV_SANDBOX', 'Process')
$agentRootPrefix = 'dbnotifier-state06-consolidated-sandbox-'
$agentRootsBefore = @(Get-ChildItem -Path ([System.IO.Path]::GetTempPath()) -Directory -Filter ($agentRootPrefix + '*') -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })

# Resolves only an already-installed browser and never downloads a substitute.
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
    throw "$Product is not installed; this offline runner will not download a replacement."
}

# Reserves one loopback port for the dedicated browser debugging endpoint.
function Get-AvailableLoopbackPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally { $listener.Stop() }
}

# Waits for one owned loopback endpoint and reports only sanitised tail diagnostics.
function Wait-ForLoopbackEndpoint(
    [string]$Uri,
    [System.Diagnostics.Process]$OwnedProcess,
    [string]$OwnedProcessErrorLog,
    [int]$Attempts = 80
) {
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        if ($OwnedProcess.HasExited) {
            $detail = if (Test-Path -LiteralPath $OwnedProcessErrorLog) {
                (Get-Content -LiteralPath $OwnedProcessErrorLog -Tail 20) -join [Environment]::NewLine
            }
            else { '' }
            throw "The owned loopback process exited before readiness. $detail"
        }
        try {
            $response = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) { return }
        }
        catch { }
        Start-Sleep -Milliseconds 250
    }
    throw "The owned loopback endpoint did not become ready: $Uri"
}

# Stops only a process tree created and retained by this runner.
function Stop-OwnedProcessTree([System.Diagnostics.Process]$Process) {
    if ($null -eq $Process -or $Process.HasExited) { return }
    & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
}

# Stops only Chromium children whose command line contains this run's unique profile path.
function Stop-OwnedBrowserResidue([string]$OwnedProfilePath) {
    Get-CimInstance Win32_Process | Where-Object {
        $_.CommandLine -and $_.CommandLine.Contains($OwnedProfilePath, [StringComparison]::OrdinalIgnoreCase)
    } | ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }
}

# Removes only this runner's GUID-named child beneath the operating-system temporary directory.
function Remove-OwnedTemporaryRoot([string]$Path) {
    $candidate = [System.IO.Path]::GetFullPath($Path)
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($candidate.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $candidate).StartsWith('DBNotifier-State06-ConsolidatedE2E-', [StringComparison]::Ordinal) -and
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
    if ($remaining.Count -ne 0) {
        throw "The consolidated runner left $($remaining.Count) new Agent sandbox root(s) after cleanup."
    }
}

$browser = Find-Browser $BrowserProduct
$debugPort = Get-AvailableLoopbackPort
New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
$hostStdOut = Join-Path $temporaryRoot 'host.stdout.log'
$hostStdErr = Join-Path $temporaryRoot 'host.stderr.log'
$browserStdOut = Join-Path $temporaryRoot 'browser.stdout.log'
$browserStdErr = Join-Path $temporaryRoot 'browser.stderr.log'

try {
    [Environment]::SetEnvironmentVariable('VITE_DB_NOTIFIER_TV_SANDBOX', 'local-test', 'Process')
    & npm.cmd run build --prefix $dashboardRoot
    if ($LASTEXITCODE -ne 0) { throw 'The existing offline Dashboard build failed.' }
    if (-not (Test-Path -LiteralPath (Join-Path $dashboardDist 'index.html') -PathType Leaf)) {
        throw 'The Dashboard build did not produce dist\index.html.'
    }

    & dotnet build $hostProject --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'The consolidated sandbox host build failed.' }

    & dotnet $hostAssembly 2>$null | Out-Null
    if ($LASTEXITCODE -ne 2) { throw 'The host did not reject missing activation arguments.' }
    & dotnet $hostAssembly '--activation' 'not-authorised' '--dashboard-root' $dashboardDist 2>$null | Out-Null
    if ($LASTEXITCODE -ne 2) { throw 'The host did not reject an invalid activation value.' }

    $hostProcess = Start-Process -FilePath 'dotnet.exe' -ArgumentList @(
        $hostAssembly,
        '--activation', 'state06-consolidated-e2e-sandbox',
        '--dashboard-root', $dashboardDist
    ) -WorkingDirectory $repositoryRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $hostStdOut -RedirectStandardError $hostStdErr

    $ready = $null
    for ($attempt = 1; $attempt -le 120; $attempt++) {
        if ($hostProcess.HasExited) {
            $detail = if (Test-Path -LiteralPath $hostStdErr) { Get-Content -LiteralPath $hostStdErr -Tail 30 } else { '' }
            throw "The consolidated host exited before readiness. $detail"
        }
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
    if ($null -eq $ready) { throw 'The consolidated host did not publish its bounded readiness record.' }

    $hostUri = [Uri]$ready.baseAddress
    if ($hostUri.Scheme -ne 'https' -or $hostUri.Host -ne '127.0.0.1' -or -not $hostUri.IsLoopback) {
        throw 'The consolidated host attempted to bind outside exact HTTPS loopback.'
    }
    if ([string]::IsNullOrWhiteSpace([string]$ready.spkiPin)) { throw 'The host exposed no ephemeral certificate pin.' }
    if (-not [Guid]::TryParseExact([string]$ready.runId, 'D', [ref]([Guid]::Empty))) { throw 'The host exposed no valid run correlation identifier.' }
    $hostBaseAddress = $hostUri.GetLeftPart([UriPartial]::Authority)
    Write-Output "Consolidated test host ready on exact HTTPS loopback: $hostBaseAddress"

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
    Write-Output "Dedicated $BrowserProduct started with an isolated ephemeral profile."
    Wait-ForLoopbackEndpoint "http://127.0.0.1:$debugPort/json/version" $browserProcess $browserStdErr

    $env:DBNOTIFIER_STATE06_CDP_ENDPOINT = "http://127.0.0.1:$debugPort"
    $env:DBNOTIFIER_STATE06_URL = $hostBaseAddress
    $env:DBNOTIFIER_STATE06_RUN_ID = [string]$ready.runId
    & node $auditScript
    if ($LASTEXITCODE -ne 0) { throw 'The consolidated browser evidence audit failed.' }

    if (-not $hostProcess.WaitForExit(20000)) { throw 'The consolidated host did not honour its authenticated shutdown control.' }
    if ($hostProcess.ExitCode -ne 0) { throw "The consolidated host exited with code $($hostProcess.ExitCode)." }
    Write-Output 'STATE-06 correlated remediation harness passed with local test data only.'
}
finally {
    Remove-Item Env:DBNOTIFIER_STATE06_CDP_ENDPOINT, Env:DBNOTIFIER_STATE06_URL, Env:DBNOTIFIER_STATE06_RUN_ID -ErrorAction SilentlyContinue
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
        throw "The consolidated runner left $($ownedResidue.Count) verified owned process(es) after cleanup."
    }
    Remove-NewOwnedAgentRoots $agentRootsBefore
    Remove-OwnedTemporaryRoot $temporaryRoot
}
