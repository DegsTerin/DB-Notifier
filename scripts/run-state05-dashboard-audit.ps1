# Module purpose: Runs the complete bilingual Light/Dark Dashboard audit in one explicitly selected, isolated Chromium product.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$DashboardDirectory,
    [ValidateRange(1024, 65535)]
    [int]$PreviewPort = 4173,
    [ValidateRange(1024, 65535)]
    [int]$DebugPort = 9224,
    [ValidateSet("Chrome", "Edge")]
    [string]$BrowserProduct = "Chrome",
    [ValidateRange(60, 600)]
    [int]$NodeTimeoutSeconds = 300,
    [string]$DiagnosticDirectory
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$DashboardDirectory = if ([string]::IsNullOrWhiteSpace($DashboardDirectory)) {
    Join-Path $root 'src\DBNotifier.Dashboard.Web'
}
else {
    $DashboardDirectory
}
$dashboard = (Resolve-Path $DashboardDirectory).Path
$semanticBrandSourcePath = Join-Path $dashboard 'src\semanticBrand.ts'
$semanticBrandSource = Get-Content -LiteralPath $semanticBrandSourcePath -Raw -Encoding UTF8
$brandRevisionMatch = [regex]::Match($semanticBrandSource, 'export const designSystemVersion = "(\d+\.\d+\.\d+)";')
if (-not $brandRevisionMatch.Success) { throw 'The canonical semantic brand revision could not be read from semanticBrand.ts.' }
$expectedCriticalFavicon = "/dbnotifier-favicon.critical.ico?v=$($brandRevisionMatch.Groups[1].Value)-critical"
$dashboardUri = "http://127.0.0.1:$PreviewPort/"
$debugUri = "http://127.0.0.1:$DebugPort"
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DBNotifier-Dashboard-Runner-{0}" -f [Guid]::NewGuid().ToString('N'))
$evidenceRoot = Join-Path $temporaryRoot 'evidence'
$profile = Join-Path $temporaryRoot 'chrome-profile'
$previewProcess = $null
$previewListenerProcess = $null
$chromeProcess = $null
$nodeProcess = $null
$stage = 'initialising-runner'
$currentLocale = $null
$currentTheme = $null
$currentReport = $null
$currentFailures = @()
$currentNodeDiagnostics = @()

# Stops only the process tree created by this runner and waits for bounded quiescence.
function Stop-ProcessTree([System.Diagnostics.Process]$Process, [int]$TimeoutMilliseconds = 15000) {
    if ($null -eq $Process) { return }
    if (-not $Process.HasExited) {
        & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
        if (-not $Process.WaitForExit($TimeoutMilliseconds)) {
            throw "Owned process $($Process.Id) did not exit within the cleanup budget."
        }
    }
}

# Stops only browser processes carrying the unique profile owned by this run.
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
    throw 'The dedicated browser did not release its isolated profile within the cleanup budget.'
}

# Removes only this runner's GUID-named temporary root after bounded lock-release retries.
function Remove-OwnedTemporaryRoot([string]$Path, [int]$TimeoutMilliseconds = 15000) {
    $candidate = [System.IO.Path]::GetFullPath($Path)
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if (-not $candidate.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase) -or
        -not (Split-Path -Leaf $candidate).StartsWith('DBNotifier-Dashboard-Runner-', [StringComparison]::Ordinal)) {
        throw 'The STATE-05 temporary root failed its exact ownership check.'
    }
    if (-not (Test-Path -LiteralPath $candidate)) { return }
    $deadline = [DateTimeOffset]::UtcNow.AddMilliseconds($TimeoutMilliseconds)
    do {
        try {
            [System.IO.Directory]::Delete($candidate, $true)
            return
        }
        catch [System.IO.IOException] { Start-Sleep -Milliseconds 250 }
        catch [System.UnauthorizedAccessException] { Start-Sleep -Milliseconds 250 }
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    throw 'The STATE-05 temporary root remained locked after bounded cleanup retries.'
}

# Redacts repository, temporary paths and loopback endpoints from a bounded Node error tail.
function Get-SanitisedNodeErrorTail([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return @() }
    $repositoryPattern = [regex]::Escape($root)
    $temporaryPattern = [regex]::Escape($temporaryRoot)
    return @(Get-Content -LiteralPath $Path -Tail 20 | ForEach-Object {
        $line = ([string]$_) -replace $repositoryPattern, '[repository]' -replace $temporaryPattern, '[temporary]'
        $line = $line -replace 'https?://127\.0\.0\.1:\d+', '[loopback]'
        if ($line.Length -gt 240) { $line.Substring(0, 240) } else { $line }
    })
}

# Waits for an isolated loopback endpoint with bounded retries and no external fallback.
function Wait-ForEndpoint([string]$Uri, [int]$Attempts = 60) {
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) { return }
        }
        catch { }
        Start-Sleep -Milliseconds 250
    }
    throw "Endpoint did not become ready: $Uri"
}

# Verifies that a requested loopback port is free before any audit process is started.
function Assert-LoopbackPortAvailable([int]$Port) {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
    try { $listener.Start() }
    catch { throw "Loopback port $Port is already in use; the audit will not attach to an unrelated process." }
    finally { $listener.Stop() }
}

# Resolves only the explicitly requested Chromium product so evidence cannot be labelled as another browser.
function Find-Browser([string]$Product) {
    $candidates = if ($Product -eq "Chrome") {
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
    throw "$Product was not found for the headless Dashboard audit. Select another product explicitly instead of using a silent fallback."
}

if ($PreviewPort -eq $DebugPort) { throw "PreviewPort and DebugPort must be different." }
Assert-LoopbackPortAvailable $PreviewPort
Assert-LoopbackPortAvailable $DebugPort
New-Item -ItemType Directory -Path $temporaryRoot | Out-Null

try {
    $stage = 'starting-preview'
    $previewProcess = Start-Process -FilePath 'npm.cmd' -ArgumentList @('run', 'preview', '--', '--host', '127.0.0.1', '--port', $PreviewPort, '--strictPort') -WorkingDirectory $dashboard -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $temporaryRoot 'preview.stdout.log') -RedirectStandardError (Join-Path $temporaryRoot 'preview.stderr.log')
    Wait-ForEndpoint $dashboardUri
    $previewListener = @(Get-NetTCPConnection -State Listen -LocalPort $PreviewPort -ErrorAction Stop)
    if ($previewListener.Count -ne 1) { throw 'The isolated Dashboard preview did not expose exactly one owned listener.' }
    $previewListenerProcess = Get-Process -Id $previewListener[0].OwningProcess -ErrorAction Stop

    $chrome = Find-Browser $BrowserProduct
    $stage = 'starting-browser'
    # The disposable browser can reach the loopback audit only; background traffic is suppressed and every other destination uses a closed local proxy.
    $browserArguments = @(
        '--headless=new',
        '--disable-gpu',
        '--disable-background-networking',
        '--disable-component-update',
        '--disable-default-apps',
        '--disable-extensions',
        '--disable-sync',
        '--metrics-recording-only',
        '--no-pings',
        '--no-first-run',
        '--no-default-browser-check',
        '--proxy-server=127.0.0.1:9',
        '--proxy-bypass-list=127.0.0.1',
        "--remote-debugging-port=$DebugPort",
        "--user-data-dir=$profile",
        $dashboardUri
    )
    $chromeProcess = Start-Process -FilePath $chrome -ArgumentList $browserArguments -WindowStyle Hidden -PassThru
    Wait-ForEndpoint "$debugUri/json/version"
    $browserMetadata = Invoke-RestMethod -Uri "$debugUri/json/version" -TimeoutSec 5
    $expectedBrowserPrefix = if ($BrowserProduct -eq "Chrome") { "Chrome/" } else { "Edg/" }
    if ([string]::IsNullOrWhiteSpace([string]$browserMetadata.Browser) -or
        -not ([string]$browserMetadata.Browser).StartsWith($expectedBrowserPrefix, [StringComparison]::Ordinal)) {
        throw "CDP reported '$($browserMetadata.Browser)' while the audit required $BrowserProduct."
    }
    $browserVersion = ([string]$browserMetadata.Browser).Substring($expectedBrowserPrefix.Length)

    $summaries = [System.Collections.Generic.List[object]]::new()
    foreach ($locale in @('pt-BR', 'en-GB')) {
        foreach ($theme in @('light', 'dark')) {
            $currentLocale = $locale
            $currentTheme = $theme
            $currentReport = $null
            $currentFailures = @()
            $currentNodeDiagnostics = @()
            $stage = "running-node-audit-$locale-$theme"
            $env:DBNOTIFIER_AUDIT_LOCALE = $locale
            $env:DBNOTIFIER_AUDIT_THEME = $theme
            $env:DBNOTIFIER_AUDIT_CDP_ENDPOINT = $debugUri
            $env:DBNOTIFIER_AUDIT_DASHBOARD_URL = $dashboardUri
            $env:DBNOTIFIER_AUDIT_BROWSER_PRODUCT = $BrowserProduct
            $env:DBNOTIFIER_AUDIT_BROWSER_VERSION = $browserVersion
            $env:DBNOTIFIER_AUDIT_EVIDENCE_ROOT = $evidenceRoot
            $nodeHost = Get-Command node -CommandType Application -ErrorAction Stop | Select-Object -First 1
            $nodeErrorLog = Join-Path $temporaryRoot "$locale-$theme.node.stderr.log"
            $nodeProcess = Start-Process -FilePath $nodeHost.Source -ArgumentList @((Join-Path $root 'scripts\audit-state05-dashboard.mjs')) -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $temporaryRoot "$locale-$theme.node.stdout.log") -RedirectStandardError $nodeErrorLog
            if (-not $nodeProcess.WaitForExit($NodeTimeoutSeconds * 1000)) {
                throw "Dashboard Node/CDP audit exceeded its global deadline for $locale/$theme."
            }
            if ($nodeProcess.ExitCode -ne 0) {
                $currentNodeDiagnostics = @(Get-SanitisedNodeErrorTail $nodeErrorLog)
                throw "Dashboard audit execution failed for $locale/$theme."
            }

            $reportPath = Join-Path $evidenceRoot "$locale\$theme\dashboard-audit.json"
            $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
            $currentReport = $report
            $failures = [System.Collections.Generic.List[string]]::new()
            if (@($report.viewports).Count -ne 30) { $failures.Add('Expected 30 viewport samples.') }
            if (@($report.viewports | Where-Object { $_.layout.horizontalOverflow }).Count -gt 0) { $failures.Add('Horizontal overflow was detected.') }
            $mobileSamples = @($report.viewports | Where-Object { $_.width -le 390 })
            if (@($mobileSamples | Where-Object { -not $_.layout.topbarSingleRow -or -not $_.layout.topbarControlsContained }).Count -gt 0) { $failures.Add('Compact topbar controls wrapped or escaped their header.') }
            $compactOperationalSamples = @($report.viewports | Where-Object { $_.width -le 390 -and ($null -ne $_.layout.alertCards -or $null -ne $_.layout.capabilityCards) })
            if ($compactOperationalSamples.Count -ne 4) { $failures.Add('Expected four compact alert and capability samples.') }
            if (@($compactOperationalSamples | Where-Object { ($null -ne $_.layout.alertCards -and ($_.layout.alertCards.columns -ne 1 -or $_.layout.alertCards.contentOverflow)) -or ($null -ne $_.layout.capabilityCards -and ($_.layout.capabilityCards.columns -ne 1 -or $_.layout.capabilityCards.contentOverflow)) }).Count -gt 0) { $failures.Add('Compact alert or capability cards were compressed into multiple columns or overflowed.') }
            $narrowAlertSamples = @($report.viewports | Where-Object { $_.name -eq 'alerts-narrow-desktop-960x1040' })
            if ($narrowAlertSamples.Count -ne 1 -or $narrowAlertSamples[0].layout.alertSummary.columns -ne 3 -or $narrowAlertSamples[0].layout.alertSummary.cardCount -ne 3 -or $narrowAlertSamples[0].layout.alertSummary.parentRightGap -ne 0) { $failures.Add('Narrow-desktop alert summary did not fill its three-column row.') }
            $overviewSamples = @($report.viewports | Where-Object { $_.hash -eq 'overview' })
            if ($overviewSamples.Count -ne 7 -or @($overviewSamples | Where-Object { $_.layout.overview.instanceRows -ne 4 -or $_.layout.overview.alertRows -ne 3 -or $_.layout.overview.contentOverflow }).Count -gt 0) { $failures.Add('Operational overview did not preserve its complete local fixture without overflow.') }
            if (@($overviewSamples | Where-Object { ($_.width -gt 767 -and $_.layout.overview.summaryColumns -ne 4) -or ($_.width -gt 350 -and $_.width -le 767 -and $_.layout.overview.summaryColumns -ne 2) -or ($_.width -le 350 -and $_.layout.overview.summaryColumns -ne 1) }).Count -gt 0) { $failures.Add('Operational overview summary retained empty or unexpected grid tracks.') }
            $performanceChartSamples = @($report.viewports | Where-Object { $null -ne $_.layout.performanceChart })
            if ($performanceChartSamples.Count -ne 12 -or @($performanceChartSamples | Where-Object { $_.layout.performanceChart.clipped }).Count -gt 0) { $failures.Add('A performance chart was clipped or overflowed its owning card in the 100/200/400-percent reflow matrix.') }
            if (@($report.accessibilityTree.unnamedInteractive).Count -gt 0) { $failures.Add('Unnamed interactive controls were detected.') }
            if ($report.accessibilityTree.exposedNodeCount -le 0) { $failures.Add('The accessibility tree contained no exposed nodes.') }
            $forcedColours = @($report.forcedColours)
            if ($forcedColours.Count -ne 24) { $failures.Add('Forced-colour coverage did not include all 24 route/zoom samples.') }
            foreach ($zoomPercent in @(100, 200, 400)) {
                $zoomSamples = @($forcedColours | Where-Object { $_.zoomPercent -eq $zoomPercent })
                if ($zoomSamples.Count -ne 8 -or ((@($zoomSamples | ForEach-Object route) -join ',') -ne 'overview,inventory,alerts,performance,history,configuration,providers,settings')) { $failures.Add("Forced-colour coverage at $zoomPercent percent did not include all eight Dashboard destinations in canonical order.") }
            }
            if (@($forcedColours | Where-Object { -not $_.active -or $_.horizontalOverflow -or -not $_.bodyUsesSystemCanvas -or -not $_.bodyUsesSystemText -or -not $_.activeNavigationUsesHighlight -or -not $_.activeNavigationTextUsesHighlightText -or -not $_.activeNavigationResistsRemapping -or -not $_.activeNavigationLabelVisible -or -not $_.activeNavigationCountUsesSystemColours -or -not $_.activeNavigationFocusVisible -or -not $_.focusVisible -or -not $_.statusBoundaryVisible -or [string]::IsNullOrWhiteSpace([string]$_.mainName) -or $_.currentNavigationCount -ne 1 -or $_.accessibilityTree.exposedNodeCount -le 0 -or @($_.accessibilityTree.unnamedInteractive).Count -gt 0 }).Count -gt 0) { $failures.Add('A forced-colour destination lost system colours, visible selected-route text, distinct focus, status boundary, navigation semantics or an accessible control name.') }
            $forcedColourOverview = @($forcedColours | Where-Object { $_.route -eq 'overview' })
            if ($forcedColourOverview.Count -ne 3 -or @($forcedColourOverview | Where-Object { @($_.metricCards).Count -ne 4 -or @($_.metricCards | Where-Object { -not $_.allBoundariesVisible }).Count -gt 0 }).Count -gt 0) { $failures.Add('An Overview metric card lost one or more boundaries in the forced-colour 100/200/400-percent matrix.') }
            if (@($forcedColours | Where-Object { $null -ne $_.performanceChart -and $_.performanceChart.clipped }).Count -gt 0) { $failures.Add('A forced-colour performance chart was clipped or overflowed its owning card.') }
            if ($report.semanticBrand.candidateCount -ne 1 -or $report.semanticBrand.aggregateState -ne 'critical' -or [string]$report.semanticBrand.href -ne $expectedCriticalFavicon) { $failures.Add("The runtime favicon did not expose the canonical brand revision $($brandRevisionMatch.Groups[1].Value) Critical candidate.") }
            if ($report.browser.product -ne $BrowserProduct -or $report.browser.version -ne $browserVersion) { $failures.Add('Browser product/version provenance was not preserved in the report.') }
            if (@($report.keyboard).Count -ne 12 -or @($report.keyboard | Where-Object { -not $_.visible }).Count -gt 0) { $failures.Add('Keyboard traversal did not preserve twelve visible focus targets.') }
            $languageCycle = @($report.preferenceCycles.language)
            $themeCycle = @($report.preferenceCycles.theme)
            if ($languageCycle.Count -ne 3 -or $languageCycle[0].locale -ne $languageCycle[2].locale -or $languageCycle[0].locale -eq $languageCycle[1].locale -or @($languageCycle | Where-Object { [string]::IsNullOrWhiteSpace($_.name) }).Count -gt 0) { $failures.Add('Language preference cycle did not change and restore an accessible state.') }
            if ($themeCycle.Count -ne 3 -or $themeCycle[0].preference -ne $themeCycle[2].preference -or $themeCycle[0].preference -eq $themeCycle[1].preference -or @($themeCycle | Where-Object { [string]::IsNullOrWhiteSpace($_.name) }).Count -gt 0) { $failures.Add('Theme preference cycle did not change and restore an accessible state.') }
            if (-not $report.modal.initial.open -or -not $report.modal.initial.focusInside -or @($report.modal.tabSequence).Count -ne 4 -or @($report.modal.tabSequence | Where-Object { -not $_.focusInside }).Count -gt 0 -or -not $report.modal.reverseTabInside -or -not $report.modal.closesWithEscape -or -not $report.modal.focusRestored) { $failures.Add('Modal opening, focus containment, Escape or focus restoration failed.') }
            $operationalStates = @($report.operationalStates.states.PSObject.Properties)
            if ($operationalStates.Count -ne 6 -or @($operationalStates | Where-Object { [string]::IsNullOrWhiteSpace([string]$_.Value.title) -or $_.Value.role -notin @('status', 'alert') }).Count -gt 0 -or $report.operationalStates.states.loading.ariaBusy -ne 'true' -or -not $report.operationalStates.states.offline.retryVisible -or -not $report.operationalStates.states.error.retryVisible -or $report.operationalStates.reducedMotionAnimation -ne 'none') { $failures.Add('Operational states or reduced-motion behaviour were incomplete.') }
            if ($report.duplicateHistorySearchRegions -ne 1) { $failures.Add('History exposed an unexpected number of search regions.') }
            if ($report.tvMode.active.tvMode -ne 'true' -or $report.tvMode.active.buttonState -ne 'exit') { $failures.Add('TV mode did not become active.') }
            if ($report.tvMode.active.overviewDisplay -eq 'none' -or $report.tvMode.active.overviewInstanceRows -ne 4) { $failures.Add('TV mode did not present the complete operational overview.') }
            if ($report.tvMode.active.languageDisplay -eq 'none' -or $report.tvMode.active.themeDisplay -eq 'none') { $failures.Add('TV mode hid a global preference control.') }
            if ([string]::IsNullOrWhiteSpace($report.tvMode.active.clockText) -or [string]::IsNullOrWhiteSpace($report.tvMode.active.systemTimeZone)) { $failures.Add('TV mode did not expose system-time evidence.') }
            $tvLayouts = @($report.tvMode.layouts)
            if ($tvLayouts.Count -ne 4 -or ((@($tvLayouts | ForEach-Object { $_.width }) -join ',') -ne '320,390,768,1920')) { $failures.Add('TV mode did not record the required 320/390/768/1920 layout matrix.') }
            if (@($tvLayouts | Where-Object { $_.documentScrollWidth -gt $_.documentClientWidth -or $_.appScrollWidth -gt $_.appClientWidth -or $_.horizontalOverflow -or $_.appOverflow -or $_.overviewOverflow }).Count -gt 0) { $failures.Add('TV mode overflowed horizontally at a required layout width.') }
            if (@($tvLayouts | Where-Object { -not $_.topbarControlsContained -or -not $_.topbarControlsWithinViewport }).Count -gt 0) { $failures.Add('TV mode topbar controls escaped their header or viewport.') }
            if (@($tvLayouts | Where-Object { ($_.width -le 1100 -and $_.overviewColumns -ne 1) -or ($_.width -gt 1100 -and $_.overviewColumns -ne 2) }).Count -gt 0) { $failures.Add('TV mode overview did not reflow to the expected compact and wide column counts.') }
            if (@($tvLayouts | Where-Object { $null -eq $_.performanceChart -or $_.performanceChart.clipped }).Count -gt 0) { $failures.Add('TV mode clipped the performance chart at a required layout width.') }
            if ($null -ne $report.tvMode.restored.tvMode -or $report.tvMode.restored.buttonState -ne 'enter') { $failures.Add('TV mode did not restore the standard layout.') }
            if ($report.tvMode.unavailableFullscreen.tvMode -ne 'true' -or $report.tvMode.unavailableFullscreen.nativeFullscreen) { $failures.Add('TV mode fullscreen fallback failed.') }
            if ($failures.Count -gt 0) {
                $currentFailures = @($failures)
                throw "Dashboard audit failed for $locale/${theme}: $($failures -join ' ')"
            }
            $summaries.Add([pscustomobject]@{ Locale = $locale; Theme = $theme; Viewports = @($report.viewports).Count; ForcedColours = $forcedColours.Count; AccessibleNodes = $report.accessibilityTree.exposedNodeCount; FaviconState = $report.semanticBrand.aggregateState; Browser = "$BrowserProduct $browserVersion" })
        }
    }
    $summaries | Format-Table -AutoSize
    Write-Output "STATE-05 Dashboard audit passed for 120 viewport samples and 96 forced-colour route/zoom samples across pt-BR/en-GB and Light/Dark on $BrowserProduct $browserVersion."
}
catch {
    if (-not [string]::IsNullOrWhiteSpace($DiagnosticDirectory)) {
        New-Item -ItemType Directory -Path $DiagnosticDirectory -Force | Out-Null
        $compactOperational = if ($null -ne $currentReport) {
            @($currentReport.viewports | Where-Object {
                $_.width -le 390 -and ($null -ne $_.layout.alertCards -or $null -ne $_.layout.capabilityCards)
            } | ForEach-Object { $_.name })
        }
        else { @() }
        [pscustomobject]@{
            schemaVersion = 1
            result = 'failed'
            stage = $stage
            exceptionType = $_.Exception.GetType().Name
            locale = $currentLocale
            theme = $currentTheme
            failures = @($currentFailures)
            viewportCount = if ($null -ne $currentReport) { @($currentReport.viewports).Count } else { 0 }
            compactOperationalSamples = @($compactOperational)
            nodeErrorTail = @($currentNodeDiagnostics)
        } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $DiagnosticDirectory 'state05-dashboard-failure.json') -Encoding utf8
    }
    throw
}
finally {
    Remove-Item Env:DBNOTIFIER_AUDIT_LOCALE, Env:DBNOTIFIER_AUDIT_THEME, Env:DBNOTIFIER_AUDIT_CDP_ENDPOINT, Env:DBNOTIFIER_AUDIT_DASHBOARD_URL, Env:DBNOTIFIER_AUDIT_BROWSER_PRODUCT, Env:DBNOTIFIER_AUDIT_BROWSER_VERSION, Env:DBNOTIFIER_AUDIT_EVIDENCE_ROOT -ErrorAction SilentlyContinue
    $cleanupFailures = [System.Collections.Generic.List[string]]::new()
    foreach ($cleanup in @(
        { Stop-ProcessTree $nodeProcess },
        { Stop-ProcessTree $chromeProcess },
        { Stop-OwnedBrowserResidue $profile },
        { Stop-ProcessTree $previewListenerProcess },
        { Stop-ProcessTree $previewProcess }
    )) {
        try { & $cleanup }
        catch { $cleanupFailures.Add($_.Exception.Message) }
    }
    try { Remove-OwnedTemporaryRoot $temporaryRoot }
    catch { $cleanupFailures.Add($_.Exception.Message) }
    if ($cleanupFailures.Count -gt 0) {
        if (-not [string]::IsNullOrWhiteSpace($DiagnosticDirectory)) {
            New-Item -ItemType Directory -Path $DiagnosticDirectory -Force | Out-Null
            [pscustomobject]@{
                schemaVersion = 1
                result = 'cleanup-failed'
                stage = 'cleanup'
                cleanupFailures = @($cleanupFailures | ForEach-Object {
                    ([string]$_) -replace [regex]::Escape($temporaryRoot), '[temporary]'
                })
            } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $DiagnosticDirectory 'state05-dashboard-cleanup-failure.json') -Encoding utf8
        }
        throw "STATE-05 cleanup failed: $($cleanupFailures -join ' ')"
    }
}
