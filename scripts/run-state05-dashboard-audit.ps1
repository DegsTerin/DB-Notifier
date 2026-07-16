# Module purpose: Runs the complete bilingual Light/Dark Dashboard and semantic-brand audit in an isolated headless Chrome session.
[CmdletBinding()]
param(
    [string]$DashboardDirectory,
    [ValidateRange(1024, 65535)]
    [int]$PreviewPort = 4173,
    [ValidateRange(1024, 65535)]
    [int]$DebugPort = 9224
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
$dashboardUri = "http://127.0.0.1:$PreviewPort/"
$debugUri = "http://127.0.0.1:$DebugPort"
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DBNotifier-Dashboard-Runner-{0}" -f [Guid]::NewGuid().ToString('N'))
$previewProcess = $null
$chromeProcess = $null

# Stops only the process tree created by this runner and tolerates an already-exited process.
function Stop-ProcessTree([System.Diagnostics.Process]$Process) {
    if ($null -eq $Process -or $Process.HasExited) { return }
    & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
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

# Resolves an installed Chromium browser suitable for a non-interactive CDP audit.
function Find-Chrome {
    foreach ($candidate in @(
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe",
        "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
    )) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) { return $candidate }
    }
    throw 'Chrome or Microsoft Edge was not found for the headless Dashboard audit.'
}

New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
try {
    Wait-ForEndpoint $dashboardUri 1 2>$null
    throw "Port $PreviewPort is already in use; the audit will not attach to or stop an unrelated process."
}
catch {
    if ($_.Exception.Message -like "Port $PreviewPort*") { throw }
}

try {
    $previewProcess = Start-Process -FilePath 'npm.cmd' -ArgumentList @('run', 'preview', '--', '--host', '127.0.0.1', '--port', $PreviewPort, '--strictPort') -WorkingDirectory $dashboard -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $temporaryRoot 'preview.stdout.log') -RedirectStandardError (Join-Path $temporaryRoot 'preview.stderr.log')
    Wait-ForEndpoint $dashboardUri

    $chrome = Find-Chrome
    $profile = Join-Path $temporaryRoot 'chrome-profile'
    $chromeProcess = Start-Process -FilePath $chrome -ArgumentList @('--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check', "--remote-debugging-port=$DebugPort", "--user-data-dir=$profile", $dashboardUri) -WindowStyle Hidden -PassThru
    Wait-ForEndpoint "$debugUri/json/version"

    $summaries = [System.Collections.Generic.List[object]]::new()
    foreach ($locale in @('pt-BR', 'en-GB')) {
        foreach ($theme in @('light', 'dark')) {
            $env:DBNOTIFIER_AUDIT_LOCALE = $locale
            $env:DBNOTIFIER_AUDIT_THEME = $theme
            $env:DBNOTIFIER_AUDIT_CDP_ENDPOINT = $debugUri
            $env:DBNOTIFIER_AUDIT_DASHBOARD_URL = $dashboardUri
            & node (Join-Path $root 'scripts\audit-state05-dashboard.mjs') *> (Join-Path $temporaryRoot "$locale-$theme.node.log")
            if ($LASTEXITCODE -ne 0) { throw "Dashboard audit execution failed for $locale/$theme." }

            $reportPath = Join-Path ([System.IO.Path]::GetTempPath()) "DBNotifier-State05-Audit\$locale\$theme\dashboard-audit.json"
            $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
            $failures = [System.Collections.Generic.List[string]]::new()
            if (@($report.viewports).Count -ne 24) { $failures.Add('Expected 24 viewport samples.') }
            if (@($report.viewports | Where-Object { $_.layout.horizontalOverflow }).Count -gt 0) { $failures.Add('Horizontal overflow was detected.') }
            $mobileSamples = @($report.viewports | Where-Object { $_.width -le 390 })
            if (@($mobileSamples | Where-Object { -not $_.layout.topbarSingleRow -or -not $_.layout.topbarControlsContained }).Count -gt 0) { $failures.Add('Compact topbar controls wrapped or escaped their header.') }
            $compactOperationalSamples = @($report.viewports | Where-Object { $_.width -le 390 -and ($null -ne $_.layout.alertCards -or $null -ne $_.layout.capabilityCards) })
            if ($compactOperationalSamples.Count -ne 4) { $failures.Add('Expected four compact alert and capability samples.') }
            if (@($compactOperationalSamples | Where-Object { ($null -ne $_.layout.alertCards -and ($_.layout.alertCards.columns -ne 1 -or $_.layout.alertCards.contentOverflow)) -or ($null -ne $_.layout.capabilityCards -and ($_.layout.capabilityCards.columns -ne 1 -or $_.layout.capabilityCards.contentOverflow)) }).Count -gt 0) { $failures.Add('Compact alert or capability cards were compressed into multiple columns or overflowed.') }
            $narrowAlertSamples = @($report.viewports | Where-Object { $_.name -eq 'alerts-narrow-desktop-960x1040' })
            if ($narrowAlertSamples.Count -ne 1 -or $narrowAlertSamples[0].layout.alertSummary.columns -ne 3 -or $narrowAlertSamples[0].layout.alertSummary.cardCount -ne 3 -or $narrowAlertSamples[0].layout.alertSummary.parentRightGap -ne 0) { $failures.Add('Narrow-desktop alert summary did not fill its three-column row.') }
            $overviewSamples = @($report.viewports | Where-Object { $_.hash -eq 'overview' })
            if ($overviewSamples.Count -ne 4 -or @($overviewSamples | Where-Object { $_.layout.overview.instanceRows -ne 4 -or $_.layout.overview.alertRows -ne 3 -or $_.layout.overview.contentOverflow }).Count -gt 0) { $failures.Add('Operational overview did not preserve its complete local fixture without overflow.') }
            if (@($overviewSamples | Where-Object { ($_.width -gt 767 -and $_.layout.overview.summaryColumns -ne 4) -or ($_.width -gt 350 -and $_.width -le 767 -and $_.layout.overview.summaryColumns -ne 2) -or ($_.width -le 350 -and $_.layout.overview.summaryColumns -ne 1) }).Count -gt 0) { $failures.Add('Operational overview summary retained empty or unexpected grid tracks.') }
            if (@($report.accessibilityTree.unnamedInteractive).Count -gt 0) { $failures.Add('Unnamed interactive controls were detected.') }
            if ($report.semanticBrand.candidateCount -ne 1 -or $report.semanticBrand.aggregateState -ne 'critical' -or $report.semanticBrand.href -ne '/dbnotifier-favicon.critical.ico?v=2.6.11-critical') { $failures.Add('The runtime favicon did not expose the single current Critical Design System 2.6.11 candidate.') }
            if (-not $report.modal.closesWithEscape -or -not $report.modal.focusRestored) { $failures.Add('Modal Escape or focus restoration failed.') }
            if ($report.tvMode.active.tvMode -ne 'true' -or $report.tvMode.active.buttonState -ne 'exit') { $failures.Add('TV mode did not become active.') }
            if ($report.tvMode.active.overviewDisplay -eq 'none' -or $report.tvMode.active.overviewInstanceRows -ne 4) { $failures.Add('TV mode did not present the complete operational overview.') }
            if ($report.tvMode.active.languageDisplay -eq 'none' -or $report.tvMode.active.themeDisplay -eq 'none') { $failures.Add('TV mode hid a global preference control.') }
            if ([string]::IsNullOrWhiteSpace($report.tvMode.active.clockText) -or [string]::IsNullOrWhiteSpace($report.tvMode.active.systemTimeZone)) { $failures.Add('TV mode did not expose system-time evidence.') }
            if ($null -ne $report.tvMode.restored.tvMode -or $report.tvMode.restored.buttonState -ne 'enter') { $failures.Add('TV mode did not restore the standard layout.') }
            if ($report.tvMode.unavailableFullscreen.tvMode -ne 'true' -or $report.tvMode.unavailableFullscreen.nativeFullscreen) { $failures.Add('TV mode fullscreen fallback failed.') }
            if ($failures.Count -gt 0) { throw "Dashboard audit failed for $locale/${theme}: $($failures -join ' ')" }
            $summaries.Add([pscustomobject]@{ Locale = $locale; Theme = $theme; Viewports = @($report.viewports).Count; AccessibleNodes = $report.accessibilityTree.exposedNodeCount; FaviconState = $report.semanticBrand.aggregateState })
        }
    }
    $summaries | Format-Table -AutoSize
    Write-Output 'STATE-05 Dashboard audit passed for 96 viewport samples across pt-BR/en-GB and Light/Dark.'
}
finally {
    Remove-Item Env:DBNOTIFIER_AUDIT_LOCALE, Env:DBNOTIFIER_AUDIT_THEME, Env:DBNOTIFIER_AUDIT_CDP_ENDPOINT, Env:DBNOTIFIER_AUDIT_DASHBOARD_URL -ErrorAction SilentlyContinue
    Stop-ProcessTree $chromeProcess
    Stop-ProcessTree $previewProcess
    $resolvedTemp = [System.IO.Path]::GetFullPath($temporaryRoot)
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($resolvedTemp.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedTemp)) {
        Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
    }
}
