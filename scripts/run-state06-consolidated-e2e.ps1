# Module purpose: Runs the single correlated STATE-06 remediation harness on exact local loopback and removes every owned runtime and browser profile.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Chrome', 'Edge')]
    [string]$BrowserProduct = 'Chrome',

    [ValidateSet('Consolidated', 'FinalHumanSamplesRemediation')]
    [string]$EvidenceMode = 'Consolidated',

    [ValidateRange(60, 900)]
    [int]$NodeTimeoutSeconds = 600,

    [string]$DiagnosticDirectory,

    [string]$DotNetPath = '.dotnet\dotnet.exe'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Import-Module (Join-Path $PSScriptRoot 'DBNotifier.RunnerProcess.psm1') -Force
$dashboardRoot = Join-Path $repositoryRoot 'src\DBNotifier.Dashboard.Web'
$dashboardDist = Join-Path $dashboardRoot 'dist'
$hostProject = Join-Path $repositoryRoot 'tests\DBNotifier.State06.ConsolidatedSandboxHost\DBNotifier.State06.ConsolidatedSandboxHost.csproj'
$hostAssembly = Join-Path $repositoryRoot 'tests\DBNotifier.State06.ConsolidatedSandboxHost\bin\Release\net10.0\DBNotifier.State06.ConsolidatedSandboxHost.dll'
$humanRemediationMode = $EvidenceMode -eq 'FinalHumanSamplesRemediation'
$activationMarker = if ($humanRemediationMode) { 'state06-final-human-samples-remediation' } else { 'state06-consolidated-e2e-sandbox' }
$expectedReadyMode = if ($humanRemediationMode) { 'final-human-samples-remediation' } else { 'consolidated' }
$auditScript = Join-Path $repositoryRoot $(if ($humanRemediationMode) {
    'scripts\audit-state06-final-human-samples-remediation.mjs'
} else {
    'scripts\audit-state06-consolidated-e2e.mjs'
})
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DBNotifier-State06-ConsolidatedE2E-{0}" -f [Guid]::NewGuid().ToString('N'))
$runId = [Guid]::NewGuid()
$hostHandle = $null
$browserHandle = $null
$nodeHandle = $null
$hostProcess = $null
$browserProcess = $null
$nodeProcess = $null
$ready = $null
$stage = 'initialising-runner'
$profilePath = Join-Path $temporaryRoot 'browser-profile'
$previousBuildFlag = [Environment]::GetEnvironmentVariable('VITE_DB_NOTIFIER_TV_SANDBOX', 'Process')
$agentRootPrefix = 'dbnotifier-state06-consolidated-sandbox-'
$ownedAgentRootPrefix = "$agentRootPrefix$($runId.ToString('N'))-"
$hostReadinessBudget = [TimeSpan]::FromSeconds(90)

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

# Stops only a retained process tree and waits for bounded quiescence before cleanup continues.
function Stop-OwnedProcessTree([System.Diagnostics.Process]$Process, [int]$TimeoutMilliseconds = 15000) {
    if ($null -eq $Process) { return }
    if (-not $Process.HasExited) {
        & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
        if (-not $Process.WaitForExit($TimeoutMilliseconds)) {
            throw "Owned process $($Process.Id) did not exit within the cleanup budget."
        }
    }
}

# Stops only Chromium children carrying this run's unique profile and proves they released it.
function Stop-OwnedBrowserResidue([string]$OwnedProfilePath, [int]$TimeoutMilliseconds = 15000) {
    $deadline = [DateTimeOffset]::UtcNow.AddMilliseconds($TimeoutMilliseconds)
    do {
        $residue = @(Get-CimInstance Win32_Process | Where-Object {
            $_.CommandLine -and $_.CommandLine.Contains($OwnedProfilePath, [StringComparison]::OrdinalIgnoreCase)
        })
        foreach ($process in $residue) {
            Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
        }
        if ($residue.Count -eq 0) { return }
        Start-Sleep -Milliseconds 200
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    throw "Owned browser processes did not release the isolated profile within the cleanup budget."
}

# Stops only child helpers whose command line carries this run's exact Agent-root correlation prefix.
function Stop-OwnedAgentProcessResidue([string]$OwnedPrefix, [int]$TimeoutMilliseconds = 15000) {
    $deadline = [DateTimeOffset]::UtcNow.AddMilliseconds($TimeoutMilliseconds)
    do {
        $residue = @(Get-CimInstance Win32_Process | Where-Object {
            $_.CommandLine -and $_.CommandLine.Contains($OwnedPrefix, [StringComparison]::OrdinalIgnoreCase)
        })
        foreach ($process in $residue) {
            Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
        }
        if ($residue.Count -eq 0) { return }
        Start-Sleep -Milliseconds 200
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    throw 'Owned Agent helper processes did not stop within the cleanup budget.'
}

# Removes only this runner's GUID-named child beneath the operating-system temporary directory.
function Remove-OwnedTemporaryRoot([string]$Path, [int]$TimeoutMilliseconds = 15000) {
    $candidate = [System.IO.Path]::GetFullPath($Path)
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($candidate.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $candidate).StartsWith('DBNotifier-State06-ConsolidatedE2E-', [StringComparison]::Ordinal) -and
        (Test-Path -LiteralPath $candidate)) {
        $deadline = [DateTimeOffset]::UtcNow.AddMilliseconds($TimeoutMilliseconds)
        do {
            try {
                [System.IO.Directory]::Delete($candidate, $true)
                return
            }
            catch [System.IO.IOException] { Start-Sleep -Milliseconds 250 }
            catch [System.UnauthorizedAccessException] { Start-Sleep -Milliseconds 250 }
        } while ([DateTimeOffset]::UtcNow -lt $deadline)
        throw 'The isolated runner root remained locked after bounded cleanup retries.'
    }
}

# Removes only Agent roots carrying this runner's exact correlation identifier.
function Remove-OwnedAgentRoots([string]$OwnedPrefix, [int]$TimeoutMilliseconds = 15000) {
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    $ownedRoots = @(Get-ChildItem -Path $systemTemp -Directory -Filter ($OwnedPrefix + '*') -ErrorAction SilentlyContinue)
    foreach ($root in $ownedRoots) {
        $candidate = [System.IO.Path]::GetFullPath($root.FullName)
        if (-not $candidate.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase) -or
            -not $root.Name.StartsWith($OwnedPrefix, [StringComparison]::Ordinal)) {
            throw 'An Agent sandbox root failed the exact per-run cleanup ownership check.'
        }
        $deadline = [DateTimeOffset]::UtcNow.AddMilliseconds($TimeoutMilliseconds)
        do {
            try {
                [System.IO.Directory]::Delete($candidate, $true)
                break
            }
            catch [System.IO.IOException] { Start-Sleep -Milliseconds 250 }
            catch [System.UnauthorizedAccessException] { Start-Sleep -Milliseconds 250 }
        } while ([DateTimeOffset]::UtcNow -lt $deadline)
    }
    $remaining = @(Get-ChildItem -Path $systemTemp -Directory -Filter ($OwnedPrefix + '*') -ErrorAction SilentlyContinue)
    if ($remaining.Count -ne 0) {
        throw "The consolidated runner left $($remaining.Count) exact owned Agent sandbox root(s) after cleanup."
    }
}

$resolvedDotNet = Resolve-DBNotifierRunnerExecutable -Candidate $DotNetPath -BaseDirectory $repositoryRoot
$resolvedNpm = Resolve-DBNotifierRunnerExecutable -Candidate 'npm.cmd' -BaseDirectory $repositoryRoot
$resolvedNode = Resolve-DBNotifierRunnerExecutable -Candidate 'node' -BaseDirectory $repositoryRoot
$browser = Find-Browser $BrowserProduct
$debugPort = Get-AvailableLoopbackPort
New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
$hostStdOut = Join-Path $temporaryRoot 'host.stdout.log'
$hostStdErr = Join-Path $temporaryRoot 'host.stderr.log'
$browserStdOut = Join-Path $temporaryRoot 'browser.stdout.log'
$browserStdErr = Join-Path $temporaryRoot 'browser.stderr.log'
$nodeStdOut = Join-Path $temporaryRoot 'node.stdout.log'
$nodeStdErr = Join-Path $temporaryRoot 'node.stderr.log'

try {
    $stage = 'building-dashboard'
    [Environment]::SetEnvironmentVariable('VITE_DB_NOTIFIER_TV_SANDBOX', 'local-test', 'Process')
    & $resolvedNpm run toolchain:verify --prefix $dashboardRoot
    if ($LASTEXITCODE -ne 0) { throw 'The pinned Dashboard toolchain verification failed.' }
    & $resolvedNpm run build --prefix $dashboardRoot
    if ($LASTEXITCODE -ne 0) { throw 'The existing offline Dashboard build failed.' }
    if (-not (Test-Path -LiteralPath (Join-Path $dashboardDist 'index.html') -PathType Leaf)) {
        throw 'The Dashboard build did not produce dist\index.html.'
    }

    $stage = 'building-host'
    & $resolvedDotNet build $hostProject --configuration Release --no-restore --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw 'The consolidated sandbox host build failed.' }

    & $resolvedDotNet $hostAssembly 2>$null | Out-Null
    if ($LASTEXITCODE -ne 2) { throw 'The host did not reject missing activation arguments.' }
    & $resolvedDotNet $hostAssembly '--activation' 'not-authorised' '--dashboard-root' $dashboardDist 2>$null | Out-Null
    if ($LASTEXITCODE -ne 2) { throw 'The host did not reject an invalid activation value.' }

    $stage = 'starting-host'
    $hostHandle = Start-DBNotifierRunnerProcess `
        -FilePath $resolvedDotNet `
        -ArgumentList @(
            $hostAssembly,
            '--activation', $activationMarker,
            '--dashboard-root', $dashboardDist,
            '--run-id', $runId.ToString('D')
        ) `
        -WorkingDirectory $repositoryRoot `
        -StandardOutputPath $hostStdOut `
        -StandardErrorPath $hostStdErr
    $hostProcess = $hostHandle.Process

    $stage = 'waiting-for-host'
    $readiness = [System.Diagnostics.Stopwatch]::StartNew()
    while ($readiness.Elapsed -lt $hostReadinessBudget) {
        if ($hostProcess.HasExited) {
            $detail = if (Test-Path -LiteralPath $hostStdErr) { Get-Content -LiteralPath $hostStdErr -Tail 30 } else { '' }
            throw "The consolidated host exited before readiness. $detail"
        }
        if (Test-Path -LiteralPath $hostStdOut) {
            foreach ($line in Get-Content -LiteralPath $hostStdOut) {
                try { $candidate = $line | ConvertFrom-Json }
                catch { $candidate = $null }
                # Treat stdout as a mixed diagnostic stream and admit only the exact readiness record.
                $markerProperty = if ($null -eq $candidate) {
                    $null
                }
                else {
                    $candidate.PSObject.Properties['marker']
                }
                if ($null -ne $markerProperty -and
                    $markerProperty.Value -is [string] -and
                    $markerProperty.Value -ceq 'DBNOTIFIER_STATE06_CONSOLIDATED_SANDBOX_READY') {
                    $ready = $candidate
                    break
                }
            }
        }
        if ($null -ne $ready) { break }
        Start-Sleep -Milliseconds 250
    }
    $readiness.Stop()
    if ($null -eq $ready) { throw 'The consolidated host did not publish its bounded readiness record.' }

    $hostUri = [Uri]$ready.baseAddress
    if ($hostUri.Scheme -ne 'https' -or $hostUri.Host -ne '127.0.0.1' -or -not $hostUri.IsLoopback) {
        throw 'The consolidated host attempted to bind outside exact HTTPS loopback.'
    }
    if ([string]::IsNullOrWhiteSpace([string]$ready.spkiPin)) { throw 'The host exposed no ephemeral certificate pin.' }
    if (-not [Guid]::TryParseExact([string]$ready.runId, 'D', [ref]([Guid]::Empty))) { throw 'The host exposed no valid run correlation identifier.' }
    if ([string]$ready.runId -ne $runId.ToString('D')) { throw 'The host did not preserve the runner-owned correlation identifier.' }
    if ([string]$ready.mode -ne $expectedReadyMode) { throw 'The host readiness mode did not match the exact requested evidence mode.' }
    $hostBaseAddress = $hostUri.GetLeftPart([UriPartial]::Authority)
    Write-Output "Consolidated test host ready on exact HTTPS loopback: $hostBaseAddress"

    $stage = 'starting-browser'
    $browserHandle = Start-DBNotifierRunnerProcess `
        -FilePath $browser `
        -ArgumentList @(
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
            '--host-resolver-rules=MAP * ~NOTFOUND, EXCLUDE localhost, EXCLUDE 127.0.0.1',
            "$hostBaseAddress/#overview"
        ) `
        -WorkingDirectory $repositoryRoot `
        -StandardOutputPath $browserStdOut `
        -StandardErrorPath $browserStdErr
    $browserProcess = $browserHandle.Process
    Write-Output "Dedicated $BrowserProduct started with an isolated ephemeral profile."
    $stage = 'waiting-for-browser'
    Wait-ForLoopbackEndpoint "http://127.0.0.1:$debugPort/json/version" $browserProcess $browserStdErr

    $env:DBNOTIFIER_STATE06_CDP_ENDPOINT = "http://127.0.0.1:$debugPort"
    $env:DBNOTIFIER_STATE06_URL = $hostBaseAddress
    $env:DBNOTIFIER_STATE06_RUN_ID = [string]$ready.runId
    $stage = 'running-node-audit'
    $nodeHandle = Start-DBNotifierRunnerProcess `
        -FilePath $resolvedNode `
        -ArgumentList @($auditScript) `
        -WorkingDirectory $repositoryRoot `
        -StandardOutputPath $nodeStdOut `
        -StandardErrorPath $nodeStdErr
    $nodeProcess = $nodeHandle.Process
    if (-not $nodeProcess.WaitForExit($NodeTimeoutSeconds * 1000)) {
        throw 'The consolidated Node/CDP audit exceeded its global deadline.'
    }
    if ($nodeProcess.ExitCode -ne 0) { throw 'The consolidated browser evidence audit failed.' }

    $stage = 'waiting-for-authenticated-host-shutdown'
    if (-not $hostProcess.WaitForExit(20000)) { throw 'The consolidated host did not honour its authenticated shutdown control.' }
    if ($hostProcess.ExitCode -ne 0) { throw "The consolidated host exited with code $($hostProcess.ExitCode)." }
    Write-Output "STATE-06 $EvidenceMode harness passed with local test data only."
}
catch {
    if (-not [string]::IsNullOrWhiteSpace($DiagnosticDirectory)) {
        New-Item -ItemType Directory -Path $DiagnosticDirectory -Force | Out-Null
        [pscustomobject]@{
            schemaVersion = 1
            result = 'failed'
            stage = $stage
            exceptionType = $_.Exception.GetType().Name
            evidenceMode = $EvidenceMode
            runId = $runId.ToString('D')
            hostExitCode = if ($null -ne $hostProcess -and $hostProcess.HasExited) { $hostProcess.ExitCode } else { $null }
            browserExitCode = if ($null -ne $browserProcess -and $browserProcess.HasExited) { $browserProcess.ExitCode } else { $null }
            nodeExitCode = if ($null -ne $nodeProcess -and $nodeProcess.HasExited) { $nodeProcess.ExitCode } else { $null }
        } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $DiagnosticDirectory 'state06-consolidated-failure.json') -Encoding utf8
    }
    throw
}
finally {
    Remove-Item Env:DBNOTIFIER_STATE06_CDP_ENDPOINT, Env:DBNOTIFIER_STATE06_URL, Env:DBNOTIFIER_STATE06_RUN_ID -ErrorAction SilentlyContinue
    [Environment]::SetEnvironmentVariable('VITE_DB_NOTIFIER_TV_SANDBOX', $previousBuildFlag, 'Process')
    $cleanupFailures = [System.Collections.Generic.List[string]]::new()
    foreach ($cleanup in @(
        { Stop-OwnedProcessTree $nodeProcess },
        { Stop-OwnedProcessTree $browserProcess },
        { Stop-OwnedBrowserResidue $profilePath },
        { Stop-OwnedProcessTree $hostProcess },
        { Stop-OwnedAgentProcessResidue $ownedAgentRootPrefix }
    )) {
        try { & $cleanup }
        catch { $cleanupFailures.Add($_.Exception.Message) }
    }

    $ownedResidue = @(Get-CimInstance Win32_Process | Where-Object {
        ($_.CommandLine -and $_.CommandLine.Contains($profilePath, [StringComparison]::OrdinalIgnoreCase)) -or
        ($_.CommandLine -and $_.CommandLine.Contains($hostAssembly, [StringComparison]::OrdinalIgnoreCase)) -or
        ($_.CommandLine -and $_.CommandLine.Contains($ownedAgentRootPrefix, [StringComparison]::OrdinalIgnoreCase))
    })
    if ($ownedResidue.Count -gt 0) {
        $cleanupFailures.Add("The consolidated runner left $($ownedResidue.Count) verified owned process(es) after cleanup.")
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
        if (-not [string]::IsNullOrWhiteSpace($DiagnosticDirectory)) {
            New-Item -ItemType Directory -Path $DiagnosticDirectory -Force | Out-Null
            [pscustomobject]@{
                schemaVersion = 1
                result = 'cleanup-failed'
                stage = 'cleanup'
                evidenceMode = $EvidenceMode
                runId = $runId.ToString('D')
                cleanupFailures = @($cleanupFailures | ForEach-Object {
                    ([string]$_) -replace [regex]::Escape($temporaryRoot), '[temporary]'
                })
            } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $DiagnosticDirectory 'state06-consolidated-cleanup-failure.json') -Encoding utf8
        }
        throw "STATE-06 cleanup failed: $($cleanupFailures -join ' ')"
    }
}
