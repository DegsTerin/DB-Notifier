# Module purpose: Runs the complete bilingual Light/Dark Dashboard audit in an isolated headless Chrome session.
[CmdletBinding()]
param(
    [string]$DashboardDirectory = (Join-Path $PSScriptRoot '..\src\DBNotifier.Dashboard.Web')
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dashboard = (Resolve-Path $DashboardDirectory).Path
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
    Wait-ForEndpoint 'http://127.0.0.1:4173/' 1 2>$null
    throw 'Port 4173 is already in use; the audit will not attach to or stop an unrelated process.'
}
catch {
    if ($_.Exception.Message -like 'Port 4173*') { throw }
}

try {
    $previewProcess = Start-Process -FilePath 'npm.cmd' -ArgumentList @('run', 'preview', '--', '--host', '127.0.0.1', '--port', '4173', '--strictPort') -WorkingDirectory $dashboard -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $temporaryRoot 'preview.stdout.log') -RedirectStandardError (Join-Path $temporaryRoot 'preview.stderr.log')
    Wait-ForEndpoint 'http://127.0.0.1:4173/'

    $chrome = Find-Chrome
    $profile = Join-Path $temporaryRoot 'chrome-profile'
    $chromeProcess = Start-Process -FilePath $chrome -ArgumentList @('--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check', '--remote-debugging-port=9224', "--user-data-dir=$profile", 'http://127.0.0.1:4173/') -WindowStyle Hidden -PassThru
    Wait-ForEndpoint 'http://127.0.0.1:9224/json/version'

    $summaries = [System.Collections.Generic.List[object]]::new()
    foreach ($locale in @('pt-BR', 'en-GB')) {
        foreach ($theme in @('light', 'dark')) {
            $env:DBNOTIFIER_AUDIT_LOCALE = $locale
            $env:DBNOTIFIER_AUDIT_THEME = $theme
            $env:DBNOTIFIER_AUDIT_CDP_ENDPOINT = 'http://127.0.0.1:9224'
            & node (Join-Path $root 'scripts\audit-state05-dashboard.mjs') *> (Join-Path $temporaryRoot "$locale-$theme.node.log")
            if ($LASTEXITCODE -ne 0) { throw "Dashboard audit execution failed for $locale/$theme." }

            $reportPath = Join-Path ([System.IO.Path]::GetTempPath()) "DBNotifier-State05-Audit\$locale\$theme\dashboard-audit.json"
            $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
            $failures = [System.Collections.Generic.List[string]]::new()
            if (@($report.viewports).Count -ne 11) { $failures.Add('Expected 11 viewport samples.') }
            if (@($report.viewports | Where-Object { $_.layout.horizontalOverflow }).Count -gt 0) { $failures.Add('Horizontal overflow was detected.') }
            $mobileSamples = @($report.viewports | Where-Object { $_.width -le 390 })
            if (@($mobileSamples | Where-Object { -not $_.layout.topbarSingleRow -or -not $_.layout.topbarControlsContained }).Count -gt 0) { $failures.Add('Compact topbar controls wrapped or escaped their header.') }
            if (@($report.accessibilityTree.unnamedInteractive).Count -gt 0) { $failures.Add('Unnamed interactive controls were detected.') }
            if (-not $report.modal.closesWithEscape -or -not $report.modal.focusRestored) { $failures.Add('Modal Escape or focus restoration failed.') }
            if ($report.tvMode.active.tvMode -ne 'true' -or $report.tvMode.active.buttonState -ne 'exit') { $failures.Add('TV mode did not become active.') }
            if ($report.tvMode.active.languageDisplay -eq 'none' -or $report.tvMode.active.themeDisplay -eq 'none') { $failures.Add('TV mode hid a global preference control.') }
            if ([string]::IsNullOrWhiteSpace($report.tvMode.active.clockText) -or [string]::IsNullOrWhiteSpace($report.tvMode.active.systemTimeZone)) { $failures.Add('TV mode did not expose system-time evidence.') }
            if ($null -ne $report.tvMode.restored.tvMode -or $report.tvMode.restored.buttonState -ne 'enter') { $failures.Add('TV mode did not restore the standard layout.') }
            if ($report.tvMode.unavailableFullscreen.tvMode -ne 'true' -or $report.tvMode.unavailableFullscreen.nativeFullscreen) { $failures.Add('TV mode fullscreen fallback failed.') }
            if ($failures.Count -gt 0) { throw "Dashboard audit failed for $locale/${theme}: $($failures -join ' ')" }
            $summaries.Add([pscustomobject]@{ Locale = $locale; Theme = $theme; Viewports = @($report.viewports).Count; AccessibleNodes = $report.accessibilityTree.exposedNodeCount })
        }
    }
    $summaries | Format-Table -AutoSize
    Write-Output 'STATE-05 Dashboard audit passed for 44 viewport samples across pt-BR/en-GB and Light/Dark.'
}
finally {
    Remove-Item Env:DBNOTIFIER_AUDIT_LOCALE, Env:DBNOTIFIER_AUDIT_THEME, Env:DBNOTIFIER_AUDIT_CDP_ENDPOINT -ErrorAction SilentlyContinue
    Stop-ProcessTree $chromeProcess
    Stop-ProcessTree $previewProcess
    $resolvedTemp = [System.IO.Path]::GetFullPath($temporaryRoot)
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($resolvedTemp.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedTemp)) {
        Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
    }
}
