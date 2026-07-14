# Module purpose: Runs repeatable STATE-05 Lighthouse audits against the four local Dashboard views in isolated mobile and desktop Chrome profiles.
[CmdletBinding()]
param(
    [Uri]$DashboardUri = 'http://127.0.0.1:4173/',
    [ValidateRange(1, 10)]
    [int]$Runs = 3,
    [string]$LighthouseVersion = '13.4.0',
    [string]$OutputDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) ("DBNotifier-Lighthouse-{0:yyyyMMdd-HHmmss}" -f [DateTimeOffset]::UtcNow))
)

$ErrorActionPreference = 'Stop'
$chromeProcess = $null
$chromeProfile = $null

# Resolves an installed Chrome binary without attaching to an existing user session.
function Find-Chrome {
    foreach ($candidate in @(
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
    )) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) { return $candidate }
    }
    throw 'Google Chrome was not found for the isolated Lighthouse audit.'
}

# Reserves a loopback TCP port briefly so the external headless Chrome session can expose DevTools.
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

# Waits for a local endpoint using bounded retries and never falls back to an external address.
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

# Returns the arithmetic median so three samples reduce sensitivity to a single local timing outlier.
function Get-Median([double[]]$Values) {
    if ($Values.Count -eq 0) { return $null }
    $ordered = @($Values | Sort-Object)
    $middle = [Math]::Floor($ordered.Count / 2)
    if ($ordered.Count % 2 -eq 1) { return $ordered[$middle] }
    return ($ordered[$middle - 1] + $ordered[$middle]) / 2
}

# Stops only the Chrome process tree created by this audit runner.
function Stop-AuditChrome([System.Diagnostics.Process]$Process) {
    if ($null -eq $Process -or $Process.HasExited) { return }
    & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
}

if (-not $DashboardUri.IsLoopback -or $DashboardUri.Scheme -ne 'http') {
    throw 'The STATE-05 Lighthouse runner accepts only a local HTTP loopback Dashboard URI.'
}

Wait-ForEndpoint $DashboardUri.AbsoluteUri 1
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$resolvedOutput = (Resolve-Path -LiteralPath $OutputDirectory).Path
$chrome = Find-Chrome
$samples = [System.Collections.Generic.List[object]]::new()
$routes = @(
    [pscustomobject]@{ Name = 'inventory'; Fragment = 'inventory' },
    [pscustomobject]@{ Name = 'history'; Fragment = 'history' },
    [pscustomobject]@{ Name = 'alerts'; Fragment = 'alerts' },
    [pscustomobject]@{ Name = 'configuration'; Fragment = 'configuration' }
)
$profiles = @(
    [pscustomobject]@{ Name = 'mobile'; FormFactor = 'mobile'; Width = 390; Height = 844; Mobile = 'true' },
    [pscustomobject]@{ Name = 'desktop'; FormFactor = 'desktop'; Width = 1440; Height = 1000; Mobile = 'false' }
)

try {
    foreach ($profile in $profiles) {
        $port = Get-AvailableLoopbackPort
        $chromeProfile = Join-Path $resolvedOutput ("chrome-profile-{0}-{1}" -f $profile.Name, [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $chromeProfile | Out-Null
        $chromeArguments = @(
            '--headless=new',
            '--disable-gpu',
            '--disable-extensions',
            '--no-first-run',
            '--no-default-browser-check',
            '--remote-debugging-address=127.0.0.1',
            "--remote-debugging-port=$port",
            "--user-data-dir=$chromeProfile",
            "--window-size=$($profile.Width),$($profile.Height)",
            'about:blank'
        )
        $chromeProcess = Start-Process -FilePath $chrome -ArgumentList $chromeArguments -PassThru -WindowStyle Hidden
        Wait-ForEndpoint "http://127.0.0.1:$port/json/version"

        foreach ($route in $routes) {
            for ($run = 1; $run -le $Runs; $run++) {
                $url = [Uri]::new($DashboardUri, "#$($route.Fragment)").AbsoluteUri
                $reportPath = Join-Path $resolvedOutput ("{0}-{1}-run-{2}.json" -f $profile.Name, $route.Name, $run)
                Write-Output ("Lighthouse {0}/{1}, run {2}/{3}" -f $profile.Name, $route.Name, $run, $Runs)
                $profileArguments = if ($profile.Name -eq 'desktop') { @('--preset=desktop') } else { @('--form-factor=mobile') }
                $arguments = @(
                    '--yes',
                    "lighthouse@$LighthouseVersion",
                    $url,
                    "--port=$port",
                    '--quiet',
                    '--output=json',
                    "--output-path=$reportPath",
                    '--only-categories=performance,accessibility,best-practices,seo',
                    "--screenEmulation.mobile=$($profile.Mobile)",
                    "--screenEmulation.width=$($profile.Width)",
                    "--screenEmulation.height=$($profile.Height)",
                    '--screenEmulation.deviceScaleFactor=1'
                ) + $profileArguments
                & npx.cmd @arguments
                if ($LASTEXITCODE -ne 0) { throw "Lighthouse failed for $($profile.Name)/$($route.Name), run $run." }

                $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
                if ($null -ne $report.runtimeError) { throw "Lighthouse reported a runtime error for $($profile.Name)/$($route.Name), run $run." }
                if ($report.configSettings.formFactor -ne $profile.FormFactor -or $report.configSettings.screenEmulation.width -ne $profile.Width -or $report.configSettings.screenEmulation.height -ne $profile.Height) {
                    throw "Lighthouse did not preserve the requested viewport for $($profile.Name)/$($route.Name), run $run."
                }
                $extensionRequests = @($report.audits.'network-requests'.details.items | Where-Object { $_.url -like 'chrome-extension://*' })
                if ($extensionRequests.Count -gt 0) { throw "A Chrome extension request contaminated $($profile.Name)/$($route.Name), run $run." }
                if ($report.audits.'robots-txt'.score -ne 1) { throw "The crawler policy was invalid for $($profile.Name)/$($route.Name), run $run." }

                $samples.Add([pscustomobject]@{
                    Profile = $profile.Name
                    Route = $route.Name
                    Run = $run
                    Performance = [Math]::Round($report.categories.performance.score * 100)
                    Accessibility = [Math]::Round($report.categories.accessibility.score * 100)
                    BestPractices = [Math]::Round($report.categories.'best-practices'.score * 100)
                    Seo = [Math]::Round($report.categories.seo.score * 100)
                    FirstContentfulPaintMs = [Math]::Round($report.audits.'first-contentful-paint'.numericValue)
                    LargestContentfulPaintMs = [Math]::Round($report.audits.'largest-contentful-paint'.numericValue)
                    TotalBlockingTimeMs = [Math]::Round($report.audits.'total-blocking-time'.numericValue)
                    CumulativeLayoutShift = [Math]::Round($report.audits.'cumulative-layout-shift'.numericValue, 4)
                    SpeedIndexMs = [Math]::Round($report.audits.'speed-index'.numericValue)
                    RobotsValid = $report.audits.'robots-txt'.score -eq 1
                    ExtensionRequestCount = $extensionRequests.Count
                    Report = [System.IO.Path]::GetFileName($reportPath)
                })
            }
        }

        Stop-AuditChrome $chromeProcess
        $chromeProcess = $null
        Start-Sleep -Milliseconds 500
        if (Test-Path -LiteralPath $chromeProfile) { Remove-Item -LiteralPath $chromeProfile -Recurse -Force }
        $chromeProfile = $null
    }

    $aggregates = [System.Collections.Generic.List[object]]::new()
    foreach ($group in ($samples | Group-Object Profile, Route)) {
        $first = $group.Group[0]
        $aggregates.Add([pscustomobject]@{
            Profile = $first.Profile
            Route = $first.Route
            Runs = $group.Count
            PerformanceMedian = Get-Median @($group.Group.Performance)
            AccessibilityMedian = Get-Median @($group.Group.Accessibility)
            BestPracticesMedian = Get-Median @($group.Group.BestPractices)
            SeoMedian = Get-Median @($group.Group.Seo)
            FirstContentfulPaintMedianMs = Get-Median @($group.Group.FirstContentfulPaintMs)
            LargestContentfulPaintMedianMs = Get-Median @($group.Group.LargestContentfulPaintMs)
            TotalBlockingTimeMedianMs = Get-Median @($group.Group.TotalBlockingTimeMs)
            CumulativeLayoutShiftMedian = Get-Median @($group.Group.CumulativeLayoutShift)
            SpeedIndexMedianMs = Get-Median @($group.Group.SpeedIndexMs)
        })
    }

    $summary = [ordered]@{
        SchemaVersion = 1
        GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        DashboardUri = $DashboardUri.AbsoluteUri
        LighthouseVersion = $LighthouseVersion
        ChromeVersion = (Get-Item -LiteralPath $chrome).VersionInfo.ProductVersion
        RunsPerRouteProfile = $Runs
        SampleCount = $samples.Count
        Profiles = $profiles
        Categories = @('performance', 'accessibility', 'best-practices', 'seo')
        CrawlerPolicy = 'Internal console intentionally disallows all crawlers; the valid robots.txt audit passes while the SEO crawlability score remains intentionally reduced.'
        Samples = $samples
        Aggregates = $aggregates
    }
    $summaryPath = Join-Path $resolvedOutput 'summary.json'
    $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding utf8
    $aggregates | Sort-Object Profile, Route | Format-Table Profile, Route, Runs, PerformanceMedian, AccessibilityMedian, BestPracticesMedian, SeoMedian, LargestContentfulPaintMedianMs, TotalBlockingTimeMedianMs, CumulativeLayoutShiftMedian -AutoSize
    Write-Output "Lighthouse audit completed: $summaryPath"
}
finally {
    Stop-AuditChrome $chromeProcess
    if ($chromeProfile) {
        $resolvedProfile = [System.IO.Path]::GetFullPath($chromeProfile)
        if ($resolvedProfile.StartsWith($resolvedOutput + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedProfile)) {
            Start-Sleep -Milliseconds 500
            Remove-Item -LiteralPath $resolvedProfile -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
